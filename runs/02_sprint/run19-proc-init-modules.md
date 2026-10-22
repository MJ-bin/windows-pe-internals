# 2026-10-23 run19 — `LdrpInitializeProcess` 뒤쪽(`+0x12de ~ 끝`): exe 모듈 항목 → TLS 준비 → kernel32 → 병렬 로딩 켜기 → exe import 올리고 연결 → import DLL 초기화 → exe TLS → 마무리 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일 (sast/14 요청은 2026-10-22).
- 정적: ntdll (`ntdll.dll`, 26100.9457), pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`)
  - analyze·export 는 다시 안 했다. `LdrpInitializeProcess.asm` 은 run18 과 같은 파일 (O1a·O1b 뒤, 전역 변수 주석·`seh:` 줄 있음).
  - 버전 대조: 일치. `_calls.tsv` 의 `+0x1bb6 → ret +0x1bbb LdrpInitializeGraphRecurse` (run7 콜스택 `+0x1bbb`), `+0x199d → ret +0x19a2 LdrpInitializeKernel32Functions` (run7), `+0x1c94 → ret +0x1c99 LdrpCallTlsInitializers` (run7·run8), `_LdrpInitialize +0x167 → ret +0x16c` (run7).
- 근거로 읽은 것
  - `LdrpInitializeProcess` (rva `0x8c404`, 7750바이트) 의 `+0x12de ~ +0x1e46` + 예외 정리 조각 `LdrpInitializeProcess$fin$0` (rva `0x169667`).
  - callee 는 **이름만** (지도 `_edges.tsv`). 본문은 안 읽었다.
  - 전역 변수 참조(`refs`) 5개 → `ms/disassemble/ntdll/_refs/{Kernel32ThreadInitThunkFunction,LdrpImageEntry,g_pfnSE_InstallAfterInit,LdrpProcessInitContextRecord,AvrfAppVerifierMode}.tsv`
  - 구조체 칸 이름은 PDB 타입 (`llvm-pdbutil dump -types`): `_PEB`, `_TEB`, `_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_RTL_USER_PROCESS_PARAMETERS`, `_KUSER_SHARED_DATA`, `_CONTEXT`, 이미지 헤더.

## 목적

- 대기열 **Q9b** (Q9 의 뒤쪽). flow.md 2.4 `LdrpInitializeProcess` 트리의 `[공백: 정적으로 아직 안 봄, Q9b]` 를 채운다. 뒤쪽 호출 75곳을 하는 일별 묶음으로, 관찰된 세 점(`+0x19a2`, `+0x1bbb`, `+0x1c99`)이 어디에 드는지까지.
- 덤으로: exe 모듈 항목은 어디서 만드나, 다른 DLL 을 올리는 일은 어느 호출로 내려가나, 병렬 로딩(일꾼 스레드)은 어디서 켜지나, 간접 호출 4곳, 디버거 때문에 갈리는 길, `LdrInitState` 값과 끝.

## 방법

- 정적: `_workspace/sast/14_proc-init-modules.md` — ntdll, `^LdrpInitializeProcess$` depth 1, 범위 `+0x12de ~ 끝` (+ 전역 refs 5개)
- 결과서: `_workspace/sast/14_proc-init-modules_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntdll/LdrpInitializeProcess.asm`
  - `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_refs/*.tsv`
  - `[RSP+0x60]` 의 출처 한 곳만 `ms/disassemble/ntdll/LdrpInitializeExecutionOptions.asm` (`+0x73d ~ +0x75e`)
- 오프셋 읽는 법 (run9 ~ 18 과 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**.
- 호출 수: 뒤쪽 `_calls.tsv` 75줄 = 본체 74 + 예외 정리 조각 1 (run18 이 센 75 와 같음).

## 결과

한 줄로: **뒤쪽은 "이삿짐 들이고 전원 켜기" 다. 주소록에 exe 이름을 적고(exe 모듈 항목), TLS 칸을 마련하고, kernel32 를 들이고, 일꾼을 부를 준비(병렬 로딩)를 한 뒤, exe 가 필요한 DLL 을 들이고 연결한다. 그다음 DLL 들을 켜고(그래프 초기화), exe 의 TLS 콜백을 부르고, 정리하고 돌아간다.**

- 비유: run18 의 앞쪽이 "공책 펴고 창고 만들기" 였다면, 뒤쪽은 짐 들이기 → 전기 연결 → 전원 켜기 순서다. 일꾼(병렬 로딩 스레드 풀)은 짐이 들어오기 바로 전에 부른다.

### 정적 — 입구 레지스터 (뒤쪽에서 쓰는 앞쪽 값)

| 값 | 어디에 | 뒤쪽에서 쓰는 곳 | 표시 |
|---|---|---|---|
| 입구 `RCX` | `[RSP+0xe0]` | **CONTEXT 레코드** (첫 스레드의 맡긴 문맥). 근거: `+0x18b4 MOV [RBX+0x80],RAX` 에서 `RBX = [RSP+0xe0]`, `_CONTEXT.Rcx` 칸 = 0x80 (PDB). 쓰는 곳 `+0x17f4`(WOW64 길 간접 호출 인자), `+0x18b4`(.NET 길), `+0x1b83`(전역 `LdrpProcessInitContextRecord` 에 저장) | [정적] |
| `RDI` | 앞쪽 `+0x14c` | PEB (뒤쪽에서 안 바뀜) | [정적] |
| `R13` | `+0x1411` | exe NT 헤더 (`[RSP+0xd0]` = `LdrpAppHeaders`) | [정적] |
| `R12B` | `+0x13d2`, `[RSP+0x58]` | .NET IL-only 표시 | [정적] |
| ntdll 시작 주소 | `[RSP+0xd8]` | `+0x1713` 등 | [정적] |

- 주의: 앞쪽 `+0x5bb` 부터 `RSI`·`R14` 는 로그 문자열·로그 문맥 주소라서 ntdll 시작 주소가 아니다 (결과서).
- test.exe (보통 x64 exe) 가정 값: `UseWOW64` = 0, `UseCOR` = 0, `R12B` = 0 `[추측]`. 실제 값은 동적 (Q44·Q46).

### 정적 — 뒤쪽 큰 순서 (호출 75곳, 10묶음)

"잡일" = `LdrpLogInternal`, `LdrpLogDllState`, `NtClose`, 문자열 버퍼 해제(`+0x1563`), `LdrpLogEtwHotPatchStatus`. `memset` 은 뒤쪽에 없다.

| # | 범위 | 요약 | 대표 호출 (call / ret) | 호출 수 (잡일) | 표시 |
|---|---|---|---|---|---|
| 1 | `+0x12de ~ +0x1568` | 병렬 로딩 지원 만들기, 일감 비우기, DLL 검색 경로, **exe 모듈 항목 만들기** (→ 전역 `LdrpImageEntry`), 현재 폴더 초기화 | `LdrpAllocateModuleEntry` `+0x1359 / +0x135e` | 13 (4) | [정적] |
| 2 | `+0x1569 ~ +0x1752` | **AppVerifier 가 켜졌을 때만**: 둘째 모듈 항목(복제) 만들어 올리고, 그래프 초기화 또는 `AVrfInitializeVerifier` | `LdrpAllocateModuleEntry` `+0x1577 / +0x157c` | 9 (2) | [정적] |
| 3 | `+0x1753 ~ +0x1805` | 진행 중 개수 내리기, CPU 기능 표, **WOW64 갈림**: WOW64 면 `LdrpLoadWow64` → 디버거 멈춤 → `LdrInitState=3` → `g_LdrpWow64LdrpInitialize` 호출 → 끝 정리로 점프 | `LdrpLoadWow64` `+0x17c1 / +0x17c6` (WOW64 만) | 8 (3) | [정적] |
| 4 | `+0x1806 ~ +0x195d` | **네이티브 길**: CFG·스케줄러 공유 데이터, (.NET 이면 `LdrpCorInitialize`·검증), **TLS 준비**, (핫패치 ETW 로그), (.NET 그래프) | `LdrpInitializeTls` `+0x18cb / +0x18d0` | 8 (1) | [정적] |
| 5 | `+0x195e ~ +0x1a41` | import 리다이렉션, **kernel32 준비**, 일감 비우기, (shim 데이터 있으면) shim 엔진 | `LdrpInitializeKernel32Functions` `+0x199d / +0x19a2` | 5 (0) | [정적] + [26100.9457 확인] run7 |
| 6 | `+0x1a42 ~ +0x1b23` | **병렬 로딩 켜기**, `LdrInitState=1`, exe 의 import 를 **올리고 연결**, 일감 비우기, 실행 준비. 끝에 `LdrInitState=2` | `LdrpMapAndSnapDependency` `+0x1aa4 / +0x1aa9`, `LdrpPrepareModuleForExecution` `+0x1aed / +0x1af2` | 7 (0) | [정적] |
| 7 | `+0x1b24 ~ +0x1b8b` | 패치 데이터, **디버거면 멈춤**, 진행 중 개수 내리기, **kernel32 스레드 시작 thunk 를 `(1,0,0)` 으로 한 번 호출**, 사용자 Windows 폴더 | 간접 `Kernel32ThreadInitThunkFunction` `+0x1b59 / +0x1b5e` | 5 (0) | [정적] |
| 8 | `+0x1b8c ~ +0x1c24` | 일감 비우기, 로더 락 잡고 **exe 그래프 초기화**, 락 풀기, 로드 문맥·DLL 경로 정리 | `LdrpInitializeGraphRecurse` `+0x1bb6 / +0x1bbb` | 7 (0) | [정적] + [26100.9457 확인] run7·run8 |
| 9 | `+0x1c25 ~ +0x1ca7` + 예외 조각 | SMT 지연 잠 초기화, **exe TLS 콜백** (활성화 컨텍스트 켜고 끔) | `LdrpCallTlsInitializers` `+0x1c94 / +0x1c99` | 4 + 조각 1 (0) | [정적] + [26100.9457 확인] run7·run8 |
| 10 | `+0x1ca8 ~ +0x1e46` | shim 후처리, **`PEB.PostProcessInitRoutine` 호출**, 핸들 닫기, CPU 기능·SymCrypt 정하기, 정상 반환 | 간접 `[RDI+0x230]` `+0x1d16 / +0x1d1b` | 8 (2) | [정적] |
| 합 | | | | **75 (12)** | |

- 잡일 12 = 묶음 1 에 `LdrpLogInternal` 2 + `LdrpLogDllState` 1 + 문자열 해제 1, 묶음 2 에 `LdrpLogDllState` 2, 묶음 3 에 `NtClose` 3, 묶음 4 에 `LdrpLogEtwHotPatchStatus` 1, 묶음 10 에 `NtClose` 2.
- 관찰된 세 점은 묶음 5, 8, 9. 코드 순서 `+0x19a2 → +0x1bbb → +0x1c99` 가 run7·run8 관찰 순서와 같다. [정적] + [관찰]
- 이름 주의 (`_calls.tsv` 의 `alias:` 1곳): `+0x1563` (ret `+0x1568`) = 문자열 버퍼 해제. 같은 주소 이름 3개, 후보: `RtlFreeAnsiString`, `RtlFreeUTF8String`, `RtlFreeUnicodeString`.
- `LdrpInitializeTls`(`+0x18cb`)가 kernel32 준비(`+0x199d`)보다 앞이다. 그때 모듈 목록엔 ntdll 과 exe 뿐이라 exe 의 TLS 칸을 먼저 만들어 두는 것으로 보인다 `[추측]` (callee 본문 안 봄, Q16).

### 정적 — exe 모듈 항목 (묶음 1)

| 자리 | 하는 일 | 표시 |
|---|---|---|
| `+0x1351` | 지역 구조체 `[RSP+0x2b0]` (이하 **로드 문맥**) 를 만듦: `+0x2b0` exe 경로 사본, `+0x2c0` DLL 경로 버퍼, `+0x2d0` 플래그 0x200, `+0x2d8` 상태 칸 주소(`&[RSP+0x50]`) | [정적] |
| `+0x1359 / +0x135e` | `LdrpAllocateModuleEntry(로드 문맥)` — **조건 없음** | [정적] |
| `+0x1374` | 돌려받은 항목을 **전역 `LdrpImageEntry` 에 저장**. `refs`: 46줄 중 쓰기는 이 한 곳뿐 (나머지 45줄은 읽기). 0 이면 `0xc0000017` 반환 | [정적] |
| `+0x1391 ~ +0x13a4` | DDAG 노드 `LoadCount` = -1, 항목 `ObsoleteLoadCount` = 0xffff → "절대 안 내려가는 모듈" | [정적] |
| `+0x13aa`, `+0x1408`, `+0x144c` | `FullDllName`(+0x48) = `ImagePathName`, **`BaseDllName`(+0x58) = 파일 이름 부분 (test.exe)** (아래 정정), `DllBase`(+0x30) = `PEB.ImageBaseAddress` | [정적] |
| `+0x13c1 ~ +0x13f7` | `Flags`: `ImageDll` 켬. 패키지 프로세스면 `PackagedBinary`, .NET 이면 `CorImage`(+IL-only 면 `CorILOnly`), 동적 재배치 이미지면 `OriginalBase` = 0. 비트 이름은 PDB | [정적] |
| `+0x1419 ~ +0x1430` | 헤더 `ImageBase` ≠ `PEB.ImageBaseAddress` 이고 IL-only 아니고 `UseWOW64 == 0` 일 때만 `LdrpProtectAndRelocateImage`. 보통은 커널이 맞춰 둬서 건너뛸 것 `[추측]` | [정적] |
| `+0x1450 / +0x1455` → `+0x14cd / +0x14d2` → `+0x14e9 / +0x14ee` | `LdrpInsertDataTableEntry` → `LdrpInsertModuleToIndex(항목, exe NT 헤더)` → `LdrpProcessMappedModule(항목, 0, UseWOW64==0)`. 목록·색인에 넣는 일로 보임 `[추측: 이름]` | [정적] |
| `+0x1508` | 성공하면 DDAG `State` = 9 (`LdrModulesReadyToRun`, PDB 열거). 뒤 `+0x1a60` 에서 2(`LdrModulesMapped`) 로 다시 씀 | [정적] |
| `+0x1517 / +0x151c` | `RtlpInitCurrentDir` (현재 폴더 `[추측]`). **실패해도 로그만 찍고 계속** (뒤쪽 유일의 "실패해도 계속") | [정적] |

- 둘째 묶음(`+0x1577 ~ +0x16a4`)은 **`AvrfAppVerifierMode != 0` 일 때만** (`+0x1568 CMP ; JZ +0x1753`). `AvrfAppVerifierMode` 를 쓰는 곳은 `AVrfInitializeVerifier +0x414` 한 곳 (`refs`). 빈 항목을 만들어 `[RSP+0x60]` 이 가리키는 다른 항목을 복제한다. `[RSP+0x60]` 은 `LdrpInitializeExecutionOptions` 의 출력 칸 → 그 안에서 `LdrpInitializeApplicationVerifierPackage` 로 넘어간다. 그래서 "verifier 공급자 모듈 항목 복제" 로 보이지만 `[추측]`. WOW64·패치·.NET 과는 관계없다 (조건은 이 값 하나). [정적]
- **test.exe 는 첫째 묶음만** `[정적: 조건상]`. `AvrfAppVerifierMode` = 0 인지는 동적 (Q46).

### 정적 — `LdrpInitializeGraphRecurse` 세 자리 (첫 인자 = 모듈 항목의 `DdagNode`)

| 자리 (call / ret) | 갈림 조건 | 첫 인자 `RCX` | test.exe | 표시 |
|---|---|---|---|---|
| `+0x16e6 / +0x16eb` | 묶음 2 안 (`AvrfAppVerifierMode != 0`) + `NtGlobalFlag & 0x100` 이 0 + `AvrfAppVerifierMode & 2` 가 0 | 복제 항목의 `DdagNode` (`[RBX+0x98]`) | 아니오 | [정적] |
| `+0x193c / +0x1941` | `[RSP+0x178] != 0` — `UseCOR` 일 때 `LdrpCorInitialize` 가 채우는 칸 | .NET 쪽 항목의 `DdagNode` | 아니오 | [정적] |
| **`+0x1bb6 / +0x1bbb`** | 조건 없음 (`+0x1b8c` 일감 비우기, `+0x1b91` 로더 락 뒤) | **exe 항목의 `DdagNode`** (`[LdrpImageEntry+0x98]`, `+0x1baf`) | **예 — 이 길 하나** | [정적] + [26100.9457 확인] run7·run8 (자리) |

- `+0x1bb6` 의 다른 인자: `RDX` = 상태 칸 주소(`[RSP+0x2d8]`), `R8` = 바이트 0 칸 주소(`&[RSP+0x56]`). [정적]
- `+0x1bb6` 앞뒤: `+0x1b83` 전역 `LdrpProcessInitContextRecord` = CONTEXT 레코드 → (그래프 초기화) → `+0x1bc7 LdrpReleaseLoaderLock` → `+0x1be2 LdrpFreeLoadContextOfNode(exe DdagNode, 상태 칸)` → `+0x1bec LdrpProcessInitContextRecord` = 0. 그 전역을 읽는 곳은 `LdrpInitializeNode +0x17a` 하나 (`refs`) → **import DLL 초기화는 이 CONTEXT 를 곁에 두고 불린다** [정적]. 무엇에 쓰는지는 Q15.
- `+0x1aed LdrpPrepareModuleForExecution` 의 callee 에도 `LdrpInitializeGraphRecurse`(`+0xea`, 지도)가 있다. dll1·dll2 의 `LdrpInitializeNode` 가 어느 쪽에서 불리나는 run7 콜스택상 `+0x1bbb` 쪽이지만 `[관찰]`, 두 자리의 역할 나눔은 정적 Q15 · 동적 Q24.

### 정적 — 다른 DLL 을 올리는 호출 후보 (callee 이름만)

앞쪽(`+0x0 ~ +0x12dd`)엔 없다 (run18). 뒤쪽 후보는 전부 **`+0x17c1 ~ +0x1aed`** 에 있고 `+0x1bb6` 은 그 뒤 "켜기" 쪽이다. 그러니 flow.md 2.6 `[추측]` "올리는 일은 `+0x12de` 뒤, `+0x1bb6` 앞" 은 **코드 자리로 맞다** [정적].

| 후보 (call / ret) | 지도의 callee 이름 | 탄다 조건 | test.exe | 표시 |
|---|---|---|---|---|
| `LdrpLoadWow64` `+0x17c1 / +0x17c6` | `LdrpLoadDll`, `LdrGetProcedureAddress`, `LdrpInitializeDllPath` | `UseWOW64 != 0` (`+0x1766`) | 아니오 | [정적] |
| `LdrpCorInitialize` `+0x184d / +0x1852` | `LdrpLoadDll`, `LdrpGetProcedureAddress`, `LdrpBuildSystem32FileName` | `UseCOR != 0` (`+0x1843`) | 아니오 | [정적] |
| `LdrpInitializeImportRedirection` `+0x195e / +0x1963` | `LdrpBuildImportRedirection`, `LdrpLoadDll`, `LdrpInitializeGraphRecurse` | 조건 없음. 실제로 올리는지는 callee 안 | 부름 / 올리는지 모름 | [정적] |
| **`LdrpInitializeKernel32Functions` `+0x199d / +0x19a2`** | `LdrLoadDll` ×2, `LdrGetProcedureAddressForCaller` ×3, `LdrpSnapKernelBaseExtensions` | exe 헤더 `Subsystem` 이 2(GUI) 또는 3(콘솔) (`+0x1980 ~ +0x1993`) | 예 | [정적] + [26100.9457 확인] run7 (`LdrLoadDll` 두 번) |
| `LdrpInitShimEngine` `+0x19ef / +0x19f4` | `LdrpLoadDll`, `LdrpLoadShimEngine` | `PEB.pShimData` 가 있고 `[그 값+0x210] != 0` | 아니오 (shim 없으면) | [정적] |
| **`LdrpMapAndSnapDependency` `+0x1aa4 / +0x1aa9`** | `LdrpGetImportDescriptorForSnap`, `LdrpLoadDependentModuleInternal`, `LdrpSnapModule`, `LdrpQueueWork`, `LdrpPrepareImportAddressTableForSnap` | IL-only 아님 (`+0x1a73 TEST R12B ; JZ +0x1a89`) | 예 — exe import 를 올리고 연결하는 가장 유력한 자리 `[추측: 이름]` | [정적] |
| `LdrpCorProcessImports` `+0x1a7f / +0x1a84` | `AVrfDllLoadNotification`, `LdrpSendDllNotifications` | IL-only .NET 일 때만 (위 대신) | 아니오 | [정적] |
| `LdrpPrepareModuleForExecution` `+0x1aed / +0x1af2` | `LdrpNotifyLoadOfGraph`, `LdrpCondenseGraph`, `LdrpInitializeGraphRecurse`, `LdrpAddNodeServiceTag`, 로더 락 | `+0x1aa4` 길에서 상태 칸이 음수가 아닐 때 (`+0x1ac9 ~ +0x1adc`) | 예. 올리기보다 "연결 끝낸 뒤 실행 준비" 로 보임 `[추측]` | [정적] |

- **`+0x1aa4` 의 첫 인자는 exe 항목이 아니다.** `RCX = &[RSP+0x2b0]` = 묶음 1 의 로드 문맥. 바로 앞 `+0x1a94` 에서 그 `+0x2d0` 플래그에 1 을 더 켠다. exe 항목(`LdrpImageEntry`)은 `+0x1aed`(`RCX`)와 `+0x1c94`(`RDX`)에 직접 간다. 다만 `LdrpMapAndSnapDependency +0x5e2` 가 `LdrpImageEntry` 를 읽고 (`refs`), 항목에 `LoadContext`(+0xb0, PDB) 칸이 있고 뒤에 `LdrpFreeLoadContextOfNode` 가 불리니, 로드 문맥과 항목이 이어져 있을 것 `[추측]` → Q11·Q12·Q14.
- `+0x1aa4` 앞뒤 순서: `+0x1a42` 병렬 로딩 켜기 → `+0x1a4c LdrInitState = 1` → `+0x1a60` exe DDAG `State` = 2 → `+0x1a71` 상태 칸 0 → `+0x1a89 LdrpThreadTokenSetMainThreadToken` → **`+0x1aa4`** → `+0x1aac LdrpDrainWorkQueue(1)` (인자 1 은 여기뿐) → (토큰 있으면) `+0x1abd` 풀기 → `+0x1aed`. [정적]
- run8 `[관찰]` "dll1·dll2 는 `LdrpLoadDll` 을 안 탔다" 와 어긋나지 않는다: `+0x1aa4` 의 callee 에 `LdrpLoadDll` 은 없고 `LdrpLoadDependentModuleInternal` 이 있다. 실제로 어느 호출에서 올라오는지는 동적 (Q23).

### 정적 — 병렬 로딩 (일꾼 스레드와의 연결)

| 순서 | 자리 (call / ret) | 하는 일 | 표시 |
|---|---|---|---|
| 1 | `LdrpInitParallelLoadingSupport` `+0x12de / +0x12e3` | 지도 callee `RtlInitializeCriticalSectionEx`, `LdrpCreateLoaderEvents` → 임계 구역·이벤트. 스레드 만드는 이름 없음. 실패면 반환 | [정적] |
| 2 | `LdrpDrainWorkQueue(0)` `+0x1302 / +0x1307` | 일감 비우기 | [정적] |
| 3 | `LdrpDrainWorkQueue(0)` `+0x19c1 / +0x19c6` | kernel32 준비 뒤 | [정적] |
| 4 | **`LdrpEnableParallelLoading(ECX)` `+0x1a42 / +0x1a47`** | `ECX = ProcessParameters.LoaderThreads` (+0x40c, PDB). `PEB.AppCompatFlags` 비트 28 이면 1 로 고정 (`+0x1a26 ~ +0x1a39`). 지도 callee `TpAllocPoolInternal`, `TpSetPoolWorkerThreadIdleTimeout`, `TpSetPoolMaxThreads`, `TpAllocWork`, `LdrpDetectDetour`, `RtlGetSuiteMask` → **스레드 풀과 일감을 만든다** | [정적] |
| 5 | `LdrpMapAndSnapDependency` `+0x1aa4` → `LdrpDrainWorkQueue(1)` `+0x1aac / +0x1ab1` | 앞의 callee 에 `LdrpQueueWork`, 뒤의 callee 에 `LdrpProcessWork`, `NtWaitForSingleObject` | [정적] |
| 6 | `LdrpDrainWorkQueue(0)` `+0x1b8c / +0x1b91` | 그래프 초기화 직전 | [정적] |

- **일꾼 스레드가 생길 수 있는 가장 이른 자리 (이 함수 안) = `+0x1a42` 뒤.** 그 앞엔 풀을 만드는 호출이 없다 (`+0x12de` 는 이벤트·임계 구역뿐). 풀을 만들 때 바로 스레드가 생기는지, 일감(`+0x1aa4` 안 `LdrpQueueWork`)이 올라올 때 생기는지는 callee 안이라 못 정한다 → 동적 (Q47). [정적]
- 그래서 `+0x195e`·`+0x199d` 가 올리는 DLL(kernel32 등)은 병렬 로딩이 켜지기 **전**이라 이 스레드 혼자 올릴 것 `[추측]` (kernel32 준비는 `LdrLoadDll` 직접 호출, run7 `[관찰]`).
- run8 `[관찰]` "일꾼 2개가 BEFORE-GRAPH(`+0x1bb6`) 직후 유저 쪽에 도착" 과 코드 자리(`+0x1a42` 뒤)가 어긋나지 않는다.

### 정적 — 간접 호출 4곳

| 자리 (call / ret) | 대상을 꺼내는 곳 | 조건 | 인자 | test.exe | 표시 |
|---|---|---|---|---|---|
| `+0x17fc / +0x17fe` (`CALL RAX`, CFG 검사 없음) | 전역 `g_LdrpWow64LdrpInitialize` (`+0x17ed`). 채우는 곳은 `LdrpLoadWow64 +0xe8` (run17) | `UseWOW64 != 0` (`+0x1766 JZ +0x1806`) | `RCX` = CONTEXT 레코드 (`+0x17f4`) | 아니오 | [정적]. 대상 이름 `[추측: wow64 쪽 시작 함수]` |
| `+0x1b59 / +0x1b5e` | 전역 **`Kernel32ThreadInitThunkFunction`** (rva `0x1d2760`). 채우는 곳 `LdrpInitializeKernel32Functions +0xe4` (칸 주소를 넘김), 읽는 곳 여기와 `RtlUserThreadStart +0x7` (`refs`) | 값이 0 이 아닐 때만 (`+0x1b4c`) | `ECX=1, EDX=0, R8=0` | 예 (kernel32 준비가 됐으면) | [정적]. 대상 = `KERNEL32!BaseThreadInitThunk` `[추측]` |
| `+0x1cdf / +0x1ce4` | 전역 `g_pfnSE_InstallAfterInit` (rva `0x1e70c0`) 을 `KUSER_SHARED_DATA.Cookie` 로 푼 값 (`+0x1cb0 ~ +0x1cd1`). 쓰는 곳 `LdrpGetShimEngineInterface +0x4d9` | `g_ShimsEnabled != 0` (`+0x1ca7`) | `RCX=&[RSP+0x98]`, `RDX`=shim 데이터 | 아니오 (shim 없으면) | [정적] |
| `+0x1d16 / +0x1d1b` | **`PEB.PostProcessInitRoutine`** (`[RDI+0x230]`, PDB 오프셋 560) | 값이 0 이 아닐 때만 (`+0x1d0a ~ +0x1d14`) | 따로 안 채움 | 값을 누가 넣는지 이 함수 안에 없음 | [정적] |

- `+0x1b59` 는 **새 스레드가 시작할 때 `RtlUserThreadStart` 가 부르는 그 함수를, 프로세스 초기화 때 `(1,0,0)` 으로 한 번 부르는 것**이다 [정적]. 그 뒤 `+0x1b72 LdrpInitializePerUserWindowsDirectory([RSP+0x188])` — `[RSP+0x188]` 은 `+0x199d` 가 채운 출력 칸.
- `+0x17fc` 를 타면 `+0x1801 JMP` 로 `+0x1d1b` 까지 건너뛴다 (TLS·shim·`PostProcessInitRoutine` 을 안 거침). [정적]

### 정적 — 디버거 때문에 갈리는 길 (CLAUDE.md "따로 적기")

- **`PEB.BeingDebugged`(PEB+0x2)** 를 읽는 곳 2곳, 둘 다 `LdrpDoDebuggerBreak` 로 간다 [정적]:
  - `+0x17d2` → `+0x17d8 / +0x17dd`: **WOW64 길 안**에서만. 그 뒤 `LdrInitState = 3`, `PEB.CrossProcessFlags` 의 `ProcessInitializing` 비트 끄기(`+0x17e7`).
  - **`+0x1b35` → `+0x1b3b / +0x1b40`: 네이티브 길.** `LdrpInitializePatchData`(`+0x1b24`) 성공 뒤. 이름으로 보면 "초기 중단점" (유저 디버거가 붙은 프로세스가 처음 멈추는 곳) `[추측]`.
  - 커널 디버거만 붙었을 때 `BeingDebugged` 가 켜지는지는 PEB 를 만드는 커널 쪽 값이라 못 정한다 `[추측: 안 켜짐]` → 동적 (Q46).
  - 네이티브 길에서 `ProcessInitializing` 비트를 끄는 줄은 이 함수 안에 없다.
- **`KUSER_SHARED_DATA+0x330` 은 디버거 표시가 아니다**: PDB `_KUSER_SHARED_DATA.Cookie`. `+0x188c`(.NET 첫 스레드에서 `LdrpCorExeMainRoutine` 포인터 풀기), `+0x1cb0`(shim 포인터 풀기) — 둘 다 포인터 풀기이고 test.exe 는 안 탈 것 `[추측]`. [정적]
- **`PEB.NtGlobalFlag & 0x100`** (`+0x16b7`): AppVerifier 묶음 안에서만. 켜져 있으면 그래프 초기화 대신 `AVrfInitializeVerifier` (`+0x1713 / +0x1718`). 비트 이름 `FLG_APPLICATION_VERIFIER` `[추측: 공개 문서]`. [정적]

### 정적 — 상태 표시와 끝

- **`LdrInitState` 쓰기 3곳** [정적] (뜻은 공개 문서 기준 `[추측]`):

| 자리 | 값 | 언제 | 뜻 `[추측]` |
|---|---|---|---|
| `+0x17dd` | 3 | WOW64 길만, `g_LdrpWow64LdrpInitialize` 부르기 직전 | 초기화 끝 |
| `+0x1a4c` | 1 | `LdrpEnableParallelLoading` 바로 뒤 | import 올리는 중 |
| `+0x1b1e` | 2 | `+0x1aed` 뒤 상태가 음수 아님을 확인한 다음 | import 올림 끝 |

  - 네이티브 길의 3 은 이 함수 밖 `_LdrpInitialize +0x205` 에서 쓴다 (돌아간 뒤).
  - 이 값을 읽는 함수들 (`LdrpAllocateModuleEntry +0xc2` 값 1, `LdrpLoadDllInternal +0x29b` 값 0, `LdrpLoadShimEngine +0x111` 값 2) 은 상태에 따라 다르게 움직인다 → Q10·Q11.
- **exe TLS 콜백** (묶음 9) [정적]:
  - `+0x1c31`: exe 항목 `TlsIndex`(+0x6e, PDB) ≠ 0 일 때만.
  - `+0x1c85 / +0x1c8a` 항목의 `EntryPointActivationContext`(+0x88) 켜기 → **`+0x1c94 / +0x1c99 LdrpCallTlsInitializers(ECX=1, RDX=LdrpImageEntry)`** → `+0x1ca2 / +0x1ca7` 끄기.
  - 예외 정리 조각 `$fin$0` 은 `RtlDeactivateActivationContextUnsafeFast` 하나만 (ret `$fin$0+0x15`) — TLS 콜백에서 예외가 빠져나올 때 같은 프레임을 끄는 정리 `[추측: 이름·오프셋 일치]`.
  - 같은 틀이 `LdrpInitializeNode +0x145 ~ +0x166` 에도 있다 (그쪽은 모듈마다).
  - → **exe TLS 는 import DLL 초기화(`+0x1bb6`)가 끝난 뒤** [정적]. run7·run8 `[관찰]` 순서와 같다.
- **TLS 뒤 순서** [정적]: `+0x1ca7` `g_ShimsEnabled` 검사 → (켜졌으면) `+0x1cdf` shim 후처리 → 실패면 `+0x1cf9 / +0x1cfe LdrUnloadDll`(shim 엔진) → `+0x1d16` `PostProcessInitRoutine` (있으면) → `+0x1d28`·`+0x1d37` `NtClose` 2 (IFEO 쪽 핸들, 0 이면 건너뜀) → `+0x1d3c ~ +0x1d94` CPUID 로 `x86_cpu_enable_ssse3`·`_simd` → `+0x1d99 / +0x1d9e RtlIsProcessorFeaturePresent(0x29)` → `x86_cpu_enable_avx512` → `+0x1def / +0x1df4 SymCryptDetectCpuFeaturesByCpuid` (`g_SymCryptFlags` 비트 0 이 꺼졌을 때만) → **정상 반환 `+0x1e2d XOR EAX,EAX ; JMP +0xa7b`** (공용 에필로그, 호출한 곳 `_LdrpInitialize +0x16c`).
  - CPU 기능 비트·기능 번호 41 의 이름은 `[추측]`.
- **`+0x1e40 RtlRaiseStatus(0x80000002)`·`+0x1e34 __fastfail(3)` 은 뒤쪽 흐름에서 안 탄다.** 거기로 뛰는 점프는 앞쪽 `+0x784`, `+0xb22`, `+0x1299`, `+0x12c6` 뿐 (run18 "강제 종료 5곳"). 호출 75곳엔 세지만 뒤쪽 길은 아니다. [정적]
- **뒤쪽 실패 길 20개** (모두 에필로그 `+0xa7b` 로 상태 반환) [정적]:
  - 로그 후 반환 7: `+0x12e9`(병렬 로딩 지원), `+0x18d8`(TLS 준비), `+0x1947`(.NET 그래프), `+0x1969`(import 리다이렉션), `+0x19a8`(kernel32 준비), `+0x1b02`(올리기·실행 준비), `+0x1c0e`(**exe 그래프 초기화 실패**).
  - `0xc0000017` 2: `+0x1361`(exe 항목 할당 실패), `+0x1582`(복제 항목, AppVerifier 만).
  - 상태 그대로 11: `+0x143b`, `+0x14f4`, `+0x165a`, `+0x1684`, `+0x174e`, `+0x17cc`(WOW64), `+0x1858`, `+0x1872`(.NET), `+0x1b2f`, `+0x1b64`(kernel32 thunk), `+0x1b7d`.
  - AppVerifier 전용 4, WOW64 1, .NET 3 → test.exe 가 갈 수 있는 건 12. 대표: `+0x1361`, `+0x1b02` (import 올리기 실패 — 예: dll 없음), `+0x1c0e` (DLL 초기화 실패).
  - `+0x1522` 현재 폴더 초기화 실패만 실패 길이 아니다 (로그만, 계속).

### 호출 관계

```
_LdrpInitialize +0x16c
 └─ LdrpInitializeProcess (0x8c404)            ← RCX = CONTEXT 레코드 → [RSP+0xe0], RDX = ntdll 주소 (run18)
      │  앞쪽 +0x0 ~ +0x12dd  [정적] run18 (생략)
      │  뒤쪽 +0x12de ~ 끝    [정적] run19. ①~⑩ = 묶음, ret 오프셋
      ├─ +0x12e3 LdrpInitParallelLoadingSupport      ① 이벤트·임계 구역
      ├─ +0x1318 LdrpInitializeDllPath               ①
      ├─ +0x135e LdrpAllocateModuleEntry             ① exe 항목 → +0x1374 LdrpImageEntry
      ├─ +0x1455 LdrpInsertDataTableEntry            ① (+0x14d2 LdrpInsertModuleToIndex, +0x14ee LdrpProcessMappedModule)
      ├─ +0x157c LdrpAllocateModuleEntry             ② AvrfAppVerifierMode != 0 일 때만 (복제 항목)
      ├─ +0x16eb LdrpInitializeGraphRecurse          ②   〃 + NtGlobalFlag & 0x100 == 0
      ├─ +0x1766 RtlInitializeProcessorFeaturesBitMap ③
      │  +0x1766 UseWOW64 ?  켜짐 → +0x17c6 LdrpLoadWow64 → +0x17dd LdrpDoDebuggerBreak (BeingDebugged)
      │                         → LdrInitState=3 → +0x17fe CALL g_LdrpWow64LdrpInitialize → +0x1d1b 로 점프
      │                     꺼짐 → +0x1806 (네이티브 길, 아래)
      ├─ +0x18d0 LdrpInitializeTls                   ④ (.NET 이면 +0x1852 LdrpCorInitialize, +0x1941 그래프)
      ├─ +0x1963 LdrpInitializeImportRedirection     ⑤
      ├─ +0x19a2 LdrpInitializeKernel32Functions     ⑤ Subsystem 2/3 일 때   [관찰] run7
      ├─ +0x1a47 LdrpEnableParallelLoading           ⑥ 스레드 풀·일감 → +0x1a4c LdrInitState=1
      ├─ +0x1aa9 LdrpMapAndSnapDependency            ⑥ RCX = 로드 문맥 [RSP+0x2b0] (IL-only 아니면)
      ├─ +0x1ab1 LdrpDrainWorkQueue(1)               ⑥
      ├─ +0x1af2 LdrpPrepareModuleForExecution       ⑥ RCX = LdrpImageEntry → +0x1b1e LdrInitState=2
      ├─ +0x1b40 LdrpDoDebuggerBreak                 ⑦ BeingDebugged 일 때만
      ├─ +0x1b5e (간접) Kernel32ThreadInitThunkFunction(1,0,0)  ⑦ 값 있을 때
      ├─ +0x1b96 LdrpAcquireLoaderLock               ⑧ (+0x1b83 LdrpProcessInitContextRecord = CONTEXT)
      ├─ +0x1bbb LdrpInitializeGraphRecurse          ⑧ RCX = exe DdagNode   [관찰] run7·run8
      ├─ +0x1bcc LdrpReleaseLoaderLock               ⑧ (+0x1be7 LdrpFreeLoadContextOfNode, +0x1bec 전역 = 0)
      ├─ +0x1c99 LdrpCallTlsInitializers             ⑨ TlsIndex != 0 일 때, 활성화 컨텍스트 안   [관찰] run7·run8
      ├─ +0x1ce4 (간접) g_pfnSE_InstallAfterInit       ⑩ g_ShimsEnabled 일 때만
      ├─ +0x1d1b (간접) PEB.PostProcessInitRoutine     ⑩ 값 있을 때
      ├─ +0x1d9e RtlIsProcessorFeaturePresent        ⑩
      ├─ +0x1df4 SymCryptDetectCpuFeaturesByCpuid    ⑩
      └─ +0x1e2d 정상 반환 (EAX = 0) → 에필로그 +0xa7b
```

- 트리는 묶음 대표만. 호출 75곳 전부는 `_calls.tsv` 의 `LdrpInitializeProcess` 줄.

## 확인된 것

- **뒤쪽 순서**: 병렬 로딩 지원 → DLL 경로 → **exe 모듈 항목** → (AppVerifier 복제) → CPU 기능 표 → (WOW64 갈림) → CFG·스케줄러 → **TLS 준비** → import 리다이렉션 → **kernel32 준비** → shim → **병렬 로딩 켜기** → **exe import 올리고 연결** → 실행 준비 → 패치 데이터 → (디버거 멈춤) → **kernel32 thunk (1,0,0)** → 로더 락 → **exe 그래프 초기화** → **exe TLS** → shim 후처리 → `PostProcessInitRoutine` → CPU·SymCrypt → 반환. [정적] 결과서 표, `.asm` 대표 줄.
- **관찰된 세 점의 코드 순서가 관찰 순서와 같다** (`+0x19a2` 묶음 5 → `+0x1bbb` 묶음 8 → `+0x1c99` 묶음 9). [정적] + [관찰] run7·run8
- **exe 모듈 항목은 `+0x1359` 에서 조건 없이 만들고 `+0x1374` 에서 `LdrpImageEntry` 에 넣는다** (쓰기는 그 한 곳). `BaseDllName` = 파일 이름, `FullDllName` = `ImagePathName`, `DllBase` = `PEB.ImageBaseAddress`, `LoadCount` = -1. [정적]
- **입구 `RCX` 는 CONTEXT 레코드다** (`+0x18b4` 가 `_CONTEXT.Rcx` 칸에 씀). run16·17 의 "맡긴 문맥" 이 `LdrpInitializeProcess` 까지 그대로 온다는 run17 결론과 맞는다. [정적]
- **`LdrpInitializeGraphRecurse` 세 자리 중 조건 없는 건 `+0x1bb6` 하나이고, 첫 인자는 exe 항목의 `DdagNode` 다.** 나머지 둘은 AppVerifier·.NET 일 때만. [정적]
- **다른 DLL 을 올리는 후보 호출은 모두 `+0x17c1 ~ +0x1aed` 에 있다** → 2.6 `[추측]` "`+0x12de` 뒤, `+0x1bb6` 앞" 이 코드 자리로 맞다. test.exe 조건상 타는 후보는 `+0x195e`(올리는지 모름), `+0x199d`(kernel32, 관찰), `+0x1aa4`·`+0x1aed`. [정적]
- **스레드 풀을 만드는 첫 자리는 `+0x1a42 LdrpEnableParallelLoading`** (callee 이름 `TpAllocPoolInternal` 등). 그 앞의 `+0x12de` 는 이벤트·임계 구역뿐. [정적]
- **exe TLS 콜백은 `TlsIndex ≠ 0` 일 때만, import DLL 초기화 뒤, 활성화 컨텍스트 안에서** 부른다. [정적]
- **`KUSER_SHARED_DATA+0x330` 은 쿠키(`Cookie`)다.** run18 이 "디버거 쪽으로 볼 곳" 으로 넘긴 `+0x188c`·`+0x1cb0` 은 포인터 풀기였다. 디버거로 갈리는 곳은 `BeingDebugged` 두 자리뿐 (네이티브 길은 `+0x1b35`). [정적]

## 정정

- **run18 의 `[RSP+0x130]` "exe 폴더 잘라 두기"** (run18 묶음 10 표, sast/13 질문 4): 틀렸다. `+0x11b0 ~ +0x1224` 는 `ImagePathName` 끝에서 앞으로 `\` 를 찾아 **그 바로 뒤부터 끝까지 = 파일 이름 부분** 을 `UNICODE_STRING` 으로 만든다 (`+0x11f4` 길이 = 끝 − 자리, `+0x1218` 버퍼 = 자리, `\` 가 없으면 `+0x1222` 전체). 그 값을 뒤쪽 `+0x1408` 이 exe 항목 `BaseDllName`(+0x58, PDB) 에 넣는다. 그래서 `[RSP+0x130]` = **exe 의 `BaseDllName` (파일 이름, test.exe)** [정적]. 같은 묶음의 "exe 폴더 종류 검사"(`+0x117e LdrpCheckAppDirType`)는 그대로. run18 문서 맨 위에 정정 한 줄을 넣었다. flow.md 2.4 앞쪽 표 묶음 10 도 고쳤다.
- **run18 의 입구 `RCX` 이름 "로더 문맥"**: 정확히는 **CONTEXT 레코드 (첫 스레드의 맡긴 문맥)** 다 (`+0x18b4`, `_CONTEXT.Rcx` 칸). run17 문서는 "맡긴 문맥" 이라고 맞게 썼고, run18 이 다른 말로 옮긴 것. 뜻이 틀린 건 아니라 run18 문서는 위 한 줄 정정에 같이 적었다. flow.md 2.4 의 "로더 문맥" 은 "CONTEXT 레코드" 로 고쳤다.

## 아직 확인 필요

- 동적 → **Q46** 뒤쪽 실행 값 (주소 = `ntdll 시작 + 0x8c404 + 오프셋`, test.exe 만):
  - `+0x1568` (`ntdll+0x8d96c`) `dd ntdll+0x1e7044 L1` — `AvrfAppVerifierMode` = 0 (둘째 묶음 건너뜀)
  - `+0x1980` (`ntdll+0x8dd84`) `dw @r13+5c L1` — `Subsystem` = 3
  - `+0x1b35` (`ntdll+0x8df39`) `db @$peb+2 L1` — `BeingDebugged` (커널 디버거만 붙었을 때)
  - `+0x1bb6` (`ntdll+0x8dfba`) `r rcx rdx r8` — `rcx` = `poi(poi(ntdll+0x1d2828)+0x98)` 인가
  - `+0x1c31` (`ntdll+0x8e035`) `dw poi(ntdll+0x1d2828)+0x6e L1` — `TlsIndex` ≠ 0
  - `+0x1455` (`ntdll+0x8d859`) `dt ntdll!_LDR_DATA_TABLE_ENTRY poi(ntdll+0x1d2828) BaseDllName FullDllName DllBase Flags` — `BaseDllName` = test.exe (이번 정정 확인), `Flags` 에 0x4
  - `+0x1424` (`ntdll+0x8d828`) `[r13+0x30]` vs `[rdi+0x10]` — 재배치 호출을 건너뛰나
  - `+0x1e2d` (`ntdll+0x8e231`) `r rax` = 0
  - (`UseWOW64` `+0x1766` 는 Q44 쪽)
- 동적 → **Q23** dll1·dll2 가 올라오는 호출: `lm m dll*` 를 `ntdll+0x8dea8`(`+0x1aa4` 직전)·`ntdll+0x8dead`(ret `+0x1aa9`)·`ntdll+0x8def6`(ret `+0x1af2`) 에서 비교, `+0x1aa4` 에서 `r rcx` vs `poi(poi(ntdll+0x1d2828)+0xb0)`.
- 동적 → **Q24** `LdrpInitializeNode` 가 `+0x1aed` 안 `LdrpInitializeGraphRecurse` 에서 먼저 불리나, `+0x1bb6` 에서 불리나 (`k`).
- 동적 → **Q47** 일꾼 스레드 최초 시점: `+0x1a42`(`ntdll+0x8de46`) `r ecx`, 이어 `+0x1a47`·`+0x1aa9`·`+0x1ab1`·`+0x1b91` 에서 스레드 수 (`~`). 필요하면 `TpAllocPoolInternal`·`LdrpQueueWork` (test.exe 조건).
- 동적 → **Q48** 간접 호출 대상: `+0x1b59`(`ntdll+0x8df5d`) `ln @rax`, `+0x1d16`(`ntdll+0x8e11a`) `dq @$peb+0x230 L1`, `+0x1cdf`(`ntdll+0x8e0e3`) `dd ntdll+0x1d23cc L1` (`g_ShimsEnabled` 0 이면 안 탐).
- 정적 → **Q10**: `+0x199d` 출력 칸 `[RSP+0x188]` → `+0x1b72`(ret `+0x1b77`) `LdrpInitializePerUserWindowsDirectory` (인자 싣기 `+0x1b6a`). `Kernel32ThreadInitThunkFunction` 은 `LdrpInitializeKernel32Functions +0xe4` 가 채움. 이 호출은 `LdrInitState` = 0 일 때.
- 정적 → **Q11·Q12**: `+0x1aed LdrpPrepareModuleForExecution(LdrpImageEntry, &상태)` callee (`LdrpInitializeGraphRecurse +0xea`, `LdrpCondenseGraph`, `LdrpNotifyLoadOfGraph`). 로드 문맥 `[RSP+0x2b0]` ↔ 항목 `LoadContext`(+0xb0) ↔ `LdrpFreeLoadContextOfNode`. DDAG `State` 9 → 2 (`+0x1508`, `+0x1a60`).
- 정적 → **Q14**: 올리기 후보 1순위 `+0x1aa4 LdrpMapAndSnapDependency` (첫 인자는 항목이 아니라 로드 문맥).
- 정적 → **Q15**: `+0x1bb6` 인자 (exe DDAG 노드, 상태 칸, 바이트 0). `LdrpProcessInitContextRecord` 를 읽는 `LdrpInitializeNode +0x17a` (`Flags.ProcessStaticImport` 모듈에서 `LdrpCallInitRoutine` 에 넘기는 문맥).
- 정적 → **Q16**: exe 항목 `TlsIndex` 를 누가 쓰나 (`+0x18cb LdrpInitializeTls` 로 보임 `[추측]`).
- 남은 `[추측]`: AppVerifier 둘째 묶음이 복제하는 항목의 정체, `LdrInitState` 값의 뜻, `+0x1391` 이 같은 줄을 `R12`=2 번 반복하는 이유, `PEB.PostProcessInitRoutine` 을 누가 넣나.

## 디버거 메모 / 도구 메모

- 도구 문제 없음 (결과서 "스크립트 문제: 없음"). `alias:` 1곳(`+0x1563`)은 별칭 규칙대로 적었다.
- `CALL RAX` (`+0x17fc`) 는 2바이트라 ret 이 `+0x17fe` 다 (다른 간접 호출 `CALL 0x180172020` 은 5바이트). 멈춤점·라벨에 쓸 때 주의.

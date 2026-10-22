# 14 proc-init-modules — 정적 분석 결과

- 요청서: `_workspace/sast/14_proc-init-modules.md` (Q9b, 02_sprint, 2026-10-22)
- 범위: `ntdll!LdrpInitializeProcess +0x12de ~ 끝(+0x1e46)` + 예외 정리 조각 `LdrpInitializeProcess$fin$0`. callee 본문은 안 읽음 (지도 `_edges.tsv` 의 callee **이름**만 봄).
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), PDB guid_age = `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`).
- analyze·export 다시 안 함 (이미 있음). `LdrpInitializeProcess.asm` 은 O1a·O1b 뒤 파일 (sast/13 에서 확인, 전역 변수 주석·`seh:` 줄 있음).
- 새로 한 일 (전부 몇 초): `refs` 5개 → `ms/disassemble/ntdll/_refs/` (`Kernel32ThreadInitThunkFunction`, `LdrpImageEntry`, `g_pfnSE_InstallAfterInit`, `LdrpProcessInitContextRecord`, `AvrfAppVerifierMode`). PDB 타입 확인은 `llvm-pdbutil dump -types ms/sym/ntdll.dll.pdb` (스크래치 파일, 커밋 안 함): `_PEB`, `_TEB`, `_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_RTL_USER_PROCESS_PARAMETERS`, `_KUSER_SHARED_DATA`, `_CONTEXT`, 이미지 헤더.
- 버전 확인: **일치**. `_calls.tsv`: `LdrpInitializeProcess +0x1bb6 → +0x1bbb LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x167 → +0x16c LdrpInitializeProcess`. 관찰된 `+0x19a2`, `+0x1c99` 도 `_calls.tsv` 에 같은 ret 으로 있음.
- 호출 수: 뒤쪽 `_calls.tsv` 75줄 = 본체 74 + 예외 정리 조각 1 (요청서 75 와 같음).
- 판정: **분석성공** (callee 안쪽은 범위 밖이라 "여기서 내려간다" 까지).

## 스크립트 문제

없음.

## 정정 (sast/13 결과서)

- sast/13 질문 4 는 `+0x11b0 ~ +0x1224` 가 "exe 폴더(마지막 `\` 앞)를 `[RSP+0x130]` 에 잘라 둔다" 고 적었다. **틀림.** 코드는 `ImagePathName` 끝에서 앞으로 `\` 를 찾고, 찾은 자리 **바로 뒤부터 끝까지** (= 파일 이름 부분) 를 `UNICODE_STRING {Length, MaximumLength, Buffer}` 로 만든다 (`+0x11f4 Length = 끝 - RBX`, `+0x1218 Buffer = RBX`; 없으면 전체 복사 `+0x1222`). 이 값을 뒤쪽 `+0x1408` 이 exe 항목 `+0x58` (`BaseDllName`, PDB) 에 넣는다. 즉 `[RSP+0x130]` = exe 의 **BaseDllName(파일 이름, test.exe)** `[정적]`.
- `[RSP+0xf0]` = `ProcessParameters.ImagePathName` (`+0x6c8`, `_RTL_USER_PROCESS_PARAMETERS` +0x60) → `+0x13aa` 가 exe 항목 `+0x48` (`FullDllName`) 에 넣는다.

## 입구 레지스터 정리 (질문 6 의 바탕)

- `RCX`(`[RSP+0xe0]`) = **CONTEXT 레코드** (첫 스레드 문맥) `[정적]`. 근거: `+0x18b4 MOV [RBX+0x80],RAX` 에서 RBX=`[RSP+0xe0]`, `_CONTEXT.Rcx` 오프셋 0x80 (PDB), `+0x1b83` 이 전역 `LdrpProcessInitContextRecord` 에 저장. 요청서가 "로더 문맥"이라 부른 것의 정체.
- 뒤쪽에서 쓰는 레지스터 (앞쪽 값): `RDI`=PEB, `RBX`=TEB 를 `[RSP+0x158]` 에서 다시 읽음(`+0x182c`), `R13`=exe NT 헤더(`+0x1411`, `[RSP+0xd0]`=`LdrpAppHeaders`), `R12B`=`[RSP+0x58]`(.NET IL-only 플래그, `+0x13d2`). **주의**: 앞쪽 `+0x5bb` 부터 `RSI`·`R14` 는 로그 문자열·로그 문맥 주소라서 ntdll 시작 주소가 아니다 (ntdll 시작 주소는 `[RSP+0xd8]`).
- test.exe (일반 x64 네이티브) 가정 값: `UseWOW64`=0, `UseCOR`=0 (`+0x64d~+0x66c` 에서 COM 디렉터리가 없으면 안 켬), `R12B`=0. 실제 값은 동적 (아래).

## 질문별 답 [정적]

### 1. 뒤쪽의 큰 순서 (표)

호출 75곳을 10묶음. "잡일" = `LdrpLogInternal`, `LdrpLogDllState`, `NtClose`, `RtlFreeAnsiString`(별칭), `LdrpLogEtwHotPatchStatus`. `memset` 은 뒤쪽에 없다. 호출 오프셋은 `call / ret`.

| # | 범위 | 요약 | 대표 호출 (call / ret) | 호출 수 (잡일) |
|---|---|---|---|---|
| 1 | `+0x12de ~ +0x1568` | 병렬 로딩 지원 만들기, 일감 비우기, DLL 검색 경로, **exe 모듈 항목 만들기**(`LdrpImageEntry`), 현재 폴더 초기화 | `LdrpAllocateModuleEntry` `+0x1359 / +0x135e` | 13 (4) |
| 2 | `+0x1569 ~ +0x1752` | **AppVerifier 가 켜졌을 때만**: 둘째 모듈 항목(복제) 만들어 올리고 그래프 순회 또는 `AVrfInitializeVerifier` | `LdrpAllocateModuleEntry` `+0x1577 / +0x157c` | 9 (2) |
| 3 | `+0x1753 ~ +0x1805` | 진행 중 개수 내리기, CPU 기능 표, **WOW64 갈림**: WOW64 면 `LdrpLoadWow64` → 디버거 멈춤 → 상태 3 → `g_LdrpWow64LdrpInitialize` 호출 후 끝으로 점프 | `LdrpLoadWow64` `+0x17c1 / +0x17c6` (WOW64 만) | 8 (3) |
| 4 | `+0x1806 ~ +0x195d` | **네이티브 길**: CFG·스케줄러 공유 데이터, (.NET 이면 `LdrpCorInitialize`·검증), **TLS 준비**, (핫패치 ETW 로그), (.NET 그래프) | `LdrpInitializeTls` `+0x18cb / +0x18d0` | 8 (1) |
| 5 | `+0x195e ~ +0x1a41` | import 리다이렉션, **kernel32 준비**, 일감 비우기, (shim 데이터 있으면) shim 엔진 | `LdrpInitializeKernel32Functions` `+0x199d / +0x19a2` **[관찰 run7]** | 5 (0) |
| 6 | `+0x1a42 ~ +0x1b23` | **병렬 로딩 켜기**, `LdrInitState=1`, exe 의 import 를 **올리고 연결(snap)**, 일감 비우기, 실행 준비. 끝에 `LdrInitState=2` | `LdrpMapAndSnapDependency` `+0x1aa4 / +0x1aa9`, `LdrpPrepareModuleForExecution` `+0x1aed / +0x1af2` | 7 (0) |
| 7 | `+0x1b24 ~ +0x1b8b` | 패치 데이터, **디버거면 멈춤**, 진행 중 개수 내리기, **kernel32 스레드 시작 thunk 호출**, 사용자 Windows 폴더 | `indirect:cfg:Kernel32ThreadInitThunkFunction` `+0x1b59 / +0x1b5e` | 5 (0) |
| 8 | `+0x1b8c ~ +0x1c24` | 일감 비우기, 로더 락 잡고 **exe 의 그래프 초기화**, 락 풀기, 로드 문맥·DLL 경로 정리 | `LdrpInitializeGraphRecurse` `+0x1bb6 / +0x1bbb` **[관찰 run7·run8]** | 7 (0) |
| 9 | `+0x1c25 ~ +0x1ca7` + 예외 조각 | SMT 지연 잠 초기화, **exe TLS 콜백** (활성화 컨텍스트 켜고 끔) | `LdrpCallTlsInitializers` `+0x1c94 / +0x1c99` **[관찰 run7·run8]** | 4 + 조각 1 (0) |
| 10 | `+0x1ca8 ~ +0x1e46` | shim 후처리, **`PEB.PostProcessInitRoutine` 호출**, 핸들 닫기, CPU 기능·SymCrypt 정하기, 정상 반환 | `indirect:cfg:[RDI+0x230]` `+0x1d16 / +0x1d1b` | 8 (2) |
| 합 | | | | 75 (12) |

- 묶음 경계는 호출 사이에서 자름. 묶음 1 의 잡일 = `LdrpLogInternal` 2 + `LdrpLogDllState` 1 + `RtlFreeAnsiString` 1, 묶음 2 = `LdrpLogDllState` 2, 묶음 3 = `NtClose` 3, 묶음 4 = `LdrpLogEtwHotPatchStatus` 1, 묶음 10 = `NtClose` 2.
- 관찰된 세 점은 묶음 5, 8, 9. 순서 `+0x19a2 → +0x1bbb → +0x1c99` 는 코드 순서와 같다 `[정적]`.
- 근거: `ms/disassemble/ntdll/LdrpInitializeProcess.asm` 의 위 `+0x..` 줄, `_calls.tsv` 의 `LdrpInitializeProcess` 줄.
- 눈에 띄는 것 (지도 메모에 답): `LdrpInitializeTls`(`+0x18cb`)가 kernel32 준비(`+0x199d`) 앞인 것은, 그 시점 모듈 목록에 ntdll 과 exe 뿐이라 exe 의 TLS 칸을 먼저 만들어 두려는 것으로 보인다 `[추측]` (callee 본문은 안 봄). 일감 비우기(`LdrpDrainWorkQueue`) 4곳 = 질문 5.

### 2. exe 모듈 항목 두 묶음

- **첫째 묶음 (`+0x1359 ~ +0x1517`) = exe 항목 (`LdrpImageEntry`)** `[정적]`. 조건 없이 탄다 (아래 세부 조건만 갈림).
  - `+0x1351` 에서 지역 구조체 `[RSP+0x2b0]` 를 만들어 `+0x1359 LdrpAllocateModuleEntry` 에 넘긴다: `+0x2b0`=`ImagePathName` 사본(16바이트), `+0x2c0`=DLL 경로 버퍼(`[RSP+0x380]`), `+0x2d0`=0x200, `+0x2d8`=상태 칸 주소(`&[RSP+0x50]`). 돌려받은 항목을 **`+0x1374` 에서 전역 `LdrpImageEntry` 에 저장** (`refs LdrpImageEntry`: 쓰기는 이 한 곳). 0 이면 로그(줄 번호 0x1b5a) 후 `0xc0000017` 반환.
  - 항목 채우기 (칸 이름은 PDB `_LDR_DATA_TABLE_ENTRY`/`_LDR_DDAG_NODE`):
    - DDAG 노드 `LoadCount`(+0x18) = -1 (`+0x1391`, 같은 줄을 R12=2 번 반복 — 앞쪽 `+0x11e2` 에서 R12 에 2 를 넣어 둔 것을 반복 횟수로 재사용한 모양, 효과는 한 번과 같음 `[추측]`), 항목 `ObsoleteLoadCount`(+0x6c) = 0xffff (`+0x13a4`, `[DDAG.Modules.Flink - 0x34]`). 즉 "절대 안 내려가는 모듈".
    - `FullDllName`(+0x48) = `ImagePathName`, `BaseDllName`(+0x58) = 파일 이름 부분 (정정 절 참고), `DllBase`(+0x30) = `PEB.ImageBaseAddress` (`+0x144c`).
    - `Flags`(+0x68) 에 `ImageDll`(비트 2) 를 켠다 (`+0x13c1`). 그리고: `PEB.BitField & 0x10` (`IsPackagedProcess`) 이면 `PackagedBinary`(비트 0) 추가 (`+0x13c7`), `UseCOR` 면 `CorImage`(비트 22), 거기에 IL-only(`R12B`) 면 `CorILOnly`(비트 24) (`+0x13d7~+0x13f0`), `PEB.BitField & 0x4` (`IsImageDynamicallyRelocated`) 면 `OriginalBase`(+0xf8) = 0 (`+0x13f7`). 비트 이름은 PDB 에서 맞춘 것.
  - **재배치 갈림** `+0x1419 ~ +0x1430`: 헤더의 `ImageBase`(NT 헤더 +0x30) 가 `PEB.ImageBaseAddress` 와 **다르고**, IL-only 도 아니고, `UseWOW64==0` 이면 `+0x1430 LdrpProtectAndRelocateImage(PEB.ImageBaseAddress)`. 평소엔 커널이 이미 재배치하고 헤더의 `ImageBase` 를 맞춰 둬서 같을 것이라 건너뜀 `[추측]` → 동적.
  - 이어서 `+0x1450 LdrpInsertDataTableEntry` (PEB.Ldr 목록·해시 연결 `[추측]`, 이름 기준), `+0x14be LdrpLogDllState`, `+0x14cd LdrpInsertModuleToIndex(entry, NT 헤더)`, `+0x14e9 LdrpProcessMappedModule(entry, 0, UseWOW64==0)`. 실패(`+0x14f4 JS`)면 바로 반환. 성공하면 DDAG `State`(+0x38) = 9 (`+0x1508`, PDB 열거 `LdrModulesReadyToRun`) — 뒤 `+0x1a60` 에서 2(`LdrModulesMapped`)로 다시 쓴다 (질문 4).
  - `+0x1517 RtlpInitCurrentDir(&[RSP+0xb0])`: 인자는 앞쪽 `+0xf26~+0xf2b` 에서 `ProcessParameters+0x38`(`CurrentDirectory`) 을 복사한 칸 `[추측: 현재 폴더 초기화]`. **음수여도 로그만 찍고 계속** (`+0x1522 JNS`, `+0x1554`); 이 음수는 `+0x1a71` 이 상태 칸을 0 으로 되돌려 지운다.
- **둘째 묶음 (`+0x1577 ~ +0x16a4`) = AppVerifier 용 복제 항목** `[정적 조건, 정체는 추측]`.
  - 갈림 `+0x1568 CMP AvrfAppVerifierMode,0 ; JZ +0x1753`: **0 이면 둘째 묶음 전체를 건너뛴다.** `AvrfAppVerifierMode` 쓰기는 `AVrfInitializeVerifier +0x414` 한 곳 (`refs`).
  - 둘째 묶음은 `LdrpAllocateModuleEntry(NULL)` 로 빈 항목을 만들고 `[RSP+0x60]` 가 가리키는 **다른 모듈 항목**의 `Flags`, `+0xf8`, `FullDllName`, `BaseDllName`, `TimeDateStamp`, `+0x120`, `DllBase`, 나중에 `EntryPoint`(`+0x168f`) 를 복사한다. `TlsIndex`(+0x6e)=0 (`+0x15ce`). `[RSP+0x60]` 는 `LdrpInitializeExecutionOptions` 의 6번째 인자(출력 칸)이고 그 안에서 `LdrpInitializeApplicationVerifierPackage` 의 마지막 인자로 넘어간다 (`LdrpInitializeExecutionOptions.asm +0x73d~+0x75e`). 앞쪽에서 0 으로 시작 (`+0xe8`).
  - 그래서 "verifier 공급자 모듈(PDB 에 `Flags.VerifierProvider` 비트가 있음) 의 항목을 복제" 로 보이지만 `[추측]` — 그 칸에 뭐가 담기는지는 callee 안.
  - WOW64·패치·.NET 이 이 묶음과 상관 있다는 증거는 없다 (이 묶음 조건은 AppVerifier 값 하나).
- **test.exe**: 첫째 묶음만 `[정적: 조건상]`. `AvrfAppVerifierMode` 가 0 인지는 동적 (IFEO/AppVerifier 안 쓰는 일반 프로세스면 0 일 것 `[추측]`).
- 두 번 부르는 함수들의 인자 차이: `LdrpInsertModuleToIndex` 둘째 인자 = 첫째는 `R13`(`LdrpAppHeaders`), 둘째는 `[RSP+0x150]`(`RtlImageNtHeaderEx` 로 새로 구한 헤더). `LdrpProcessMappedModule` 은 첫째 `(entry,0,!UseWOW64)`, 둘째 `(entry,0,1)`.

### 3. `LdrpInitializeGraphRecurse` 세 자리 (모두 호출 규칙 `RCX`=DDAG 노드)

| 자리 (call) | 갈림 조건 | 첫 인자 `RCX` | 다른 인자 | test.exe |
|---|---|---|---|---|
| `+0x16e6` | 묶음 2 안 (`AvrfAppVerifierMode != 0`) **그리고** `PEB.NtGlobalFlag & 0x100` 이 0 **그리고** `AvrfAppVerifierMode & 2` 가 0 (`+0x16b7`, `+0x16c8`) | `[RBX+0x98]` = 둘째(복제) 항목의 `DdagNode` | `RDX`=0, `R8`=`&[RSP+0x54]`(바이트 0) | 아니오 (AppVerifier 일 때만) |
| `+0x193c` | `[RSP+0x178] != 0` (`+0x1925`). 그 칸은 **`UseCOR` 일 때** `LdrpCorInitialize(&[RSP+0x178])`(`+0x184d`)가 채운다 (아니면 앞쪽에서 0) | `[[RSP+0x178]+0x98]` = .NET 쪽 모듈 항목의 `DdagNode` | `RDX`=0, `R8`=`&[RSP+0x55]` | 아니오 (.NET 만) |
| **`+0x1bb6`** | 조건 없음 (`+0x1b8c` 일감 비우기 → `+0x1b91` 로더 락 잡기 뒤) | `[LdrpImageEntry+0x98]` = **exe 항목의 `DdagNode`** | `RDX`=`[RSP+0x2d8]`(상태 칸 주소), `R8`=`&[RSP+0x56]`(바이트 0) | **예 — 이 길 하나** |

- 결론: test.exe 가 `+0x1bb6` 하나만 타는지는 정적으로 "조건상 그렇다" (앞 두 자리는 AppVerifier·.NET 이 아니면 안 감). 값은 동적 (질문 Q46).
- 첫 인자는 항목이 아니라 항목의 **`DdagNode`** 다. `LdrpPrepareModuleForExecution` 의 callee 에도 `LdrpInitializeGraphRecurse`(`+0xea`)가 있다 (지도). 그것과 `+0x1bb6` 의 역할 나눔은 Q15 몫 — 동적으로 두 군데 중 어디서 `LdrpInitializeNode` 가 불리는지 봐야 한다 (Q48).
- `+0x1bb6` 뒤: `+0x1bc7 LdrpReleaseLoaderLock(…, 9, 상태)`, `+0x1be2 LdrpFreeLoadContextOfNode(exe DdagNode, 상태 칸)`, `+0x1be7` 진행 중 개수 내리기, `+0x1bec` `LdrpProcessInitContextRecord = 0`. 상태 음수면 `+0x1c0e` 에서 실패 (질문 8).

### 4. 올리기가 시작되는 자리 — **예**, 후보는 아래 순서

앞쪽(`+0x0 ~ +0x12dd`)엔 없고 (sast/13), 뒤쪽 후보는 이렇다. 갈림 조건은 아래 표. flow.md 2.6 `[추측]` "올리는 일은 `+0x12de` 뒤 `+0x1bb6` 앞" **맞다**: 후보 호출이 전부 `+0x17c1 ~ +0x1aed` 에 있고, `+0x1bb6` 은 그 뒤 초기화 쪽이다.

| 후보 (call 자리) | 지도의 callee 이름 (본문 안 봄) | 탄다 조건 | test.exe (x64 콘솔 가정) |
|---|---|---|---|
| `+0x17c1 LdrpLoadWow64` | `LdrpLoadDll`, `LdrGetProcedureAddress`, `LdrpInitializeDllPath` | `UseWOW64 != 0` (`+0x1766`) | 아니오 |
| `+0x184d LdrpCorInitialize` | `LdrpLoadDll`, `LdrpGetProcedureAddress`, `LdrpBuildSystem32FileName` | `UseCOR != 0` (`+0x1843`) | 아니오 |
| `+0x195e LdrpInitializeImportRedirection` | `LdrpBuildImportRedirection`, `LdrpLoadDll`, `LdrpInitializeGraphRecurse` | 조건 없음 (이 함수 안). 실제로 올리는지는 callee 안 | 부름 / 올리는지는 동적 |
| **`+0x199d LdrpInitializeKernel32Functions`** | `LdrLoadDll` ×2, `LdrGetProcedureAddressForCaller` ×3, `LdrpSnapKernelBaseExtensions` | `NT 헤더 Subsystem` 이 2(GUI) 또는 3(콘솔) (`+0x1980~+0x1993`, `Subsystem` = `OptionalHeader+0x44`, PDB 68) | 예 (콘솔이면 3) **[관찰 run7: `LdrLoadDll` 두 번]** |
| `+0x19ef LdrpInitShimEngine` | `LdrpLoadDll`, `LdrpLoadShimEngine` | `PEB.pShimData`(앞쪽에서 `[RSP+0x180]` 에 저장) 가 0 아니고 `[그 값+0x210] != 0` (`+0x19d7~+0x19e3`) | 아니오 (shim 없으면) |
| **`+0x1aa4 LdrpMapAndSnapDependency`** | `LdrpGetImportDescriptorForSnap`, `LdrpLoadDependentModuleInternal`, `LdrpSnapModule`, `LdrpQueueWork`, `LdrpPrepareImportAddressTableForSnap` | `R12B == 0` = IL-only 가 아님 (`+0x1a73 JZ +0x1a89`) | **예** — exe 의 import 를 올리고 연결하는 가장 유력한 자리 `[추측: 이름·callee 이름 기준]` |
| `+0x1a7f LdrpCorProcessImports` | `AVrfDllLoadNotification`, `LdrpSendDllNotifications` (올리는 이름 없음) | IL-only `.NET` 일 때만 (위 `+0x1aa4` 대신) | 아니오 |
| `+0x1aed LdrpPrepareModuleForExecution` | `LdrpNotifyLoadOfGraph`, `LdrpCondenseGraph`, `LdrpInitializeGraphRecurse`, `LdrpAddNodeServiceTag`, 로더 락 | `+0x1aa4` 길에서 상태(`[RSP+0x50]`)가 음수가 아닐 때 (`+0x1ac9~+0x1adc`) | 예. 올리기가 아니라 "연결 끝낸 뒤 실행 준비" 로 보임 `[추측]` |
| `+0x18cb LdrpInitializeTls` (참고) | `LdrpAllocateTlsEntry`, `LdrpAllocateTls`, `RtlImageDirectoryEntryToData` | 조건 없음 | 올리기 아님 (TLS 칸 준비 `[추측]`) |

- **`+0x1aa4` 의 첫 인자는 exe 항목(`LdrpImageEntry`)이 아니다.** `RCX = &[RSP+0x2b0]` = `+0x1351` 에서 `LdrpAllocateModuleEntry` 에 넘긴 그 지역 구조체 (경로 사본·DLL 경로 버퍼·상태 칸 주소). 이때 `+0x1a94` 에서 `[RSP+0x2d0] |= 1` 로 플래그를 하나 더 켠다. exe 항목은 `LdrpPrepareModuleForExecution`(`+0x1aed`, `RCX=LdrpImageEntry`, `RDX=[RSP+0x2d8]`)과 `LdrpCallTlsInitializers`(`+0x1c94`)에 직접 간다. 그러나 `LdrpMapAndSnapDependency` 안에서 `LdrpImageEntry` 를 읽는 줄이 하나 있다 (`refs`: `+0x5e2`), 그리고 항목 쪽에 `LoadContext`(+0xb0, PDB) 칸이 있고 뒤에서 `+0x1be2 LdrpFreeLoadContextOfNode` 가 불리니, 이 구조체가 항목과 이어져 있을 것이다 `[추측]` → Q12·Q14 (callee 안).
- `+0x1a42 ~ +0x1a67` 사이: `LdrInitState = 1` (`+0x1a4c`), exe 의 DDAG 노드 `State`=2 (`LdrModulesMapped`, `+0x1a60`), 상태 칸 `*[RSP+0x2d8]=0` (`+0x1a71`: 앞의 음수 지움). 그 뒤 `+0x1a89 LdrpThreadTokenSetMainThreadToken` → `+0x1aa4` → `+0x1aac LdrpDrainWorkQueue(1)` (인자 1 은 이 자리뿐) → 토큰이 있으면 `+0x1abd` 풀기 → `+0x1aed`.
- dll1·dll2 가 `LdrpLoadDll` 을 안 탔다 `[관찰 run8]` 와 이 코드는 어긋나지 않는다: `+0x1aa4` 의 callee 에 `LdrpLoadDependentModuleInternal`(import 처리용 이름)이 있고 `LdrpLoadDll` 은 없다. 어느 호출에서 dll1·dll2 가 메모리에 올라오는지는 동적 (Q48).

### 5. 병렬 로딩 (일꾼 스레드 연결)

순서와 조건:

1. `+0x12de LdrpInitParallelLoadingSupport` (지도 callee: `RtlInitializeCriticalSectionEx`, `LdrpCreateLoaderEvents` → 임계 구역·이벤트 만들기, 스레드 생성 이름 없음). 음수면 로그(줄 번호 0x1b45) 후 반환 (`+0x12e9 JNS`).
2. `+0x1302 LdrpDrainWorkQueue(0)`.
3. `+0x19c1 LdrpDrainWorkQueue(0)` (kernel32 준비·import 리다이렉션 뒤).
4. **`+0x1a42 LdrpEnableParallelLoading(ECX)`**: `ECX = [ProcessParameters+0x40c]` = `LoaderThreads` (PDB `_RTL_USER_PROCESS_PARAMETERS` 0x40c; IFEO 값 `MaxLoaderThreads` 를 `LdrpInitializeExecutionOptions` 가 여기에 씀). 앞에서 `PEB.AppCompatFlags`(PEB+0x2c8)의 비트 28 이 켜져 있으면 1 로 고정 (`+0x1a26~+0x1a39`). 지도 callee: `LdrpDetectDetour`, `RtlGetSuiteMask`, `TpAllocPoolInternal`, `TpSetPoolWorkerThreadIdleTimeout`, `TpSetPoolMaxThreads`, `TpAllocWork` → **스레드 풀과 일감(work)을 만든다.**
5. `+0x1a89` (토큰 설정) → `+0x1aa4 LdrpMapAndSnapDependency` (지도 callee 에 **`LdrpQueueWork`**) → `+0x1aac LdrpDrainWorkQueue(1)` (지도 callee: `LdrpProcessWork`, `NtWaitForSingleObject`) → `+0x1abd` 토큰 풀기(`LdrpMainThreadToken != 0` 일 때만).
6. `+0x1b8c LdrpDrainWorkQueue(0)` 그래프 초기화 직전.

- **일꾼 스레드가 생길 수 있는 가장 이른 자리 (이 함수 안)**: `+0x1a42` 앞에는 풀을 만드는 호출이 없다 (`+0x12de` 는 이벤트·임계 구역뿐). 풀을 만드는 건 `+0x1a42`, 일감이 처음 들어갈 수 있는 곳은 `+0x1aa4` 안 `LdrpQueueWork`. 스레드가 풀 만들 때 바로 생기는지, 일감이 올라올 때 생기는지는 callee 안이라 정적으로 못 정한다 → 동적 (Q49). flow.md 3장 `[추측]` "일꾼은 그 뒤에" 는 **`+0x1a42` 뒤** 로 좁혀서 뒷받침된다.
- 일감 비우기가 4곳이고 `+0x1302` 가 `+0x1a42` 보다 앞인 것은, `+0x1a42` 전에는 일감이 없어 "빈 큐 확인" 정도일 것 `[추측]`. `+0x195e`·`+0x199d` 가 올리는 DLL 은 병렬 로딩이 켜지기 전이라 한 스레드로 올라갈 것 `[추측]` (kernel32 준비는 `LdrLoadDll` 직접 호출, 관찰 run7).

### 6. 간접 호출 4곳

- **`+0x17fc` (`CALL RAX`, CFG 검사 없음)**: `+0x17ed RAX = g_LdrpWow64LdrpInitialize` (값은 `LdrpLoadWow64 +0xe8` 이 칸 주소를 `LdrGetProcedureAddress` 쪽에 넘겨 채움 — `refs`: 쓰기 코드 대신 `PARAM`, 읽기 `_LdrpInitialize +0x90` 와 여기), `+0x17f4 RCX = [RSP+0xe0]` = **CONTEXT 레코드**. 그러므로 `RCX` 는 이 호출의 인자 맞다 `[정적]`. **WOW64 일 때만**: `+0x1766 JZ +0x1806` 이 `UseWOW64==0` 이면 건너뛴다. 대상 이름은 `[추측: wow64.dll 의 시작 함수]`, 값은 동적. 이 호출 뒤 `+0x1801 JMP +0x1d1b` 로 TLS·shim·`PostProcessInitRoutine` 을 건너뛰고 끝 정리로 간다.
- **`+0x1b59` `cfg:Kernel32ThreadInitThunkFunction`**: 전역(rva `0x1d2760`)을 읽어 0 이 아닐 때만 부른다 (`+0x1b4c~+0x1b4f`). 인자 `ECX=1, EDX=0, R8=0`. 값 쓰는 곳 (`refs`): **`LdrpInitializeKernel32Functions +0xe4`** (칸 주소를 넘겨 채움, 묶음 5 에서). 읽는 곳: 이 자리와 `RtlUserThreadStart +0x7` (새 스레드 시작 때). 그래서 이 호출은 **스레드 시작 때 부르는 그 함수를 프로세스 초기화 때 `1,0,0` 으로 한 번 부르는 것**이다 `[정적]`. kernel32 의 `BaseThreadInitThunk` 일 것 `[추측: 이름은 callee 안 문자열을 안 봄]`. kernel32 준비가 안 됐으면(Subsystem 이 GUI·콘솔이 아님) 값이 0 이라 건너뛴다. 음수면 반환. 이어서 `+0x1b72 LdrpInitializePerUserWindowsDirectory(RCX=[RSP+0x188])` — `[RSP+0x188]` 는 `+0x199d` 가 채운 출력 칸.
- **`+0x1cdf` `cfg:?`**: `g_ShimsEnabled != 0` 일 때만 (`+0x1ca7`). 대상 = 전역 `g_pfnSE_InstallAfterInit`(rva `0x1e70c0`) 을 `KUSER_SHARED_DATA+0x330` 쿠키로 푼 값 (`+0x1cb0~+0x1cd1`: 쿠키 낮은 6비트만큼 회전 후 XOR). 쓰는 곳 (`refs`): `LdrpGetShimEngineInterface +0x4d9` 한 곳. 인자 `RCX=&[RSP+0x98]`, `RDX=R13`(`+0x19cd` 에서 다시 읽은 shim 데이터). `AL==0` 이면 `g_ShimsEnabled=0`, `LdrUnloadDll(g_pShimEngineModule)`, 모듈 포인터 0 (`+0x1ceb~+0x1cfe`). `g_ShimsEnabled` 는 `+0x19ef LdrpInitShimEngine` 쪽에서 켜질 값 `[추측]` → shim 데이터가 없는 test.exe 는 안 탈 것.
- **`+0x1d16` `cfg:[RDI+0x230]`**: `RDI` = **PEB** (앞쪽 `+0x14c`, 중간에 안 바뀜). `+0x230` = PDB `_PEB.PostProcessInitRoutine` (오프셋 560). 0 이 아닐 때만 부르고 (`+0x1d11~+0x1d14`) 인자는 따로 안 채운다. 값을 누가 넣는지는 이 함수 안에 없음 → 동적.
- **`+0x1cf9` 와 `LdrUnloadDll`**: 위 shim 후처리 안.

### 7. 디버거 때문에 달라지는 길

- `PEB.BeingDebugged`(PEB+0x2):
  - `+0x17d2` → `+0x17d8 LdrpDoDebuggerBreak`: **WOW64 길 안**에서만 (`+0x17c1` 뒤). 그 뒤 `LdrInitState=3`, `PEB.CrossProcessFlags` 비트 1 (`ProcessInitializing`, PDB) 끄기(`+0x17e7`).
  - **`+0x1b35` → `+0x1b3b LdrpDoDebuggerBreak`: 네이티브 길.** `LdrpInitializePatchData`(`+0x1b24`) 성공 뒤, `BeingDebugged != 0` 이면 부른다. 이름으로 보면 "초기 중단점"(유저 디버거가 붙은 프로세스에서 처음 멈추는 곳) `[추측]`.
  - 커널 디버거만 붙었을 때 `BeingDebugged` 가 켜질지는 PEB 를 만드는 커널 쪽 값이라 정적으로 모름 (커널 디버거는 유저 디버거 포트와 다르므로 0 일 것 `[추측]`) → 동적 (Q46).
  - 네이티브 길에서 `ProcessInitializing` 비트를 끄는 줄은 이 함수 안에 없다 (WOW64 길 `+0x17e7` 하나뿐).
- **`KUSER_SHARED_DATA+0x330` 은 디버거 플래그가 아니다**: PDB `_KUSER_SHARED_DATA.Cookie`(오프셋 816=0x330). `+0x188c` (`.NET` 이고 `TEB.SameTebFlags & 0x400`(첫 스레드 `InitialThread`)일 때 `LdrpCorExeMainRoutine` 을 풀어 `CONTEXT.Rcx` 에 넣는 자리), `+0x1cb0` (shim 의 `g_pfnSE_InstallAfterInit` 풀기). 둘 다 포인터 풀기이고 test.exe 는 안 탈 것. (`TEB+0x17ee` = `SameTebFlags`, PDB 6126.)
- `PEB.NtGlobalFlag & 0x100` (`+0x16b7`): AppVerifier 묶음 안 (묶음 2). 이 비트가 있으면 그래프 순회 대신 `+0x1713 AVrfInitializeVerifier(0,0,0,1,ntdll 시작 주소,0)` 를 부른다 (`AvrfAppVerifierMode & 2` 일 때도). 성공하고 `AvrfAppVerifierMode & 2` 면 복제 항목 `State`=9 (`+0x1735`). 실패면 상태 반환(`+0x174a~+0x174e`). 비트 이름은 공개 문서 기준 `FLG_APPLICATION_VERIFIER` `[추측]`.
- 디버거가 붙으면 `PEB.NtGlobalFlag` 에 힙 검사 비트가 켜질 수 있다는 것은 앞쪽(`+0x58e` 등) 이야기라 sast/13 몫.

### 8. 상태 표시와 끝

- **`LdrInitState` 쓰기** (`refs LdrInitState`):
  - `+0x17dd` = **3** (WOW64 길만, `g_LdrpWow64LdrpInitialize` 부르기 직전).
  - `+0x1a4c` = **1** (`LdrpEnableParallelLoading` 바로 뒤).
  - `+0x1b1e` = **2** (`+0x1aed` 뒤 상태 음수 아님 확인한 다음).
  - 네이티브 길의 3 은 이 함수 밖 `_LdrpInitialize +0x205` 에서 쓴다 (돌아간 뒤).
  - 뜻 (공개 문서 기준 `[추측]`): 0 = 시작 전, 1 = import 올리는 중, 2 = import 올림 끝, 3 = 초기화 끝. 값 자체는 `[정적]`.
  - 읽는 곳 중 의미 있는 것: `LdrpAllocateModuleEntry +0xc2` (값 1 비교), `LdrpLoadDllInternal +0x29b` (0 비교), `LdrpLoadShimEngine +0x111` (2 비교) 등 — 상태가 바뀌면 그 함수들이 다르게 동작한다 (Q10·Q11 에 넘김).
- **`+0x1c94` TLS 뒤 순서**: 
  1. `+0x1ca2 RtlDeactivateActivationContextUnsafeFast(&frame)`.
  2. `+0x1ca7 g_ShimsEnabled` 검사 → (켜져 있으면) `+0x1cdf` `SE_InstallAfterInit` → 실패면 `+0x1cf9 LdrUnloadDll(g_pShimEngineModule)`.
  3. `+0x1d0a PEB.PostProcessInitRoutine` 이 있으면 `+0x1d16` 호출.
  4. `+0x1d28`, `+0x1d37` `NtClose` 2번: `[RSP+0x90]`(IFEO 키 핸들, `LdrpInitializeExecutionOptions` 의 출력)과 `[RSP+0x78]`(그 함수의 다른 출력 핸들). 닫기 전 `0` 확인. (WOW64 길에서는 앞 `+0x1780~+0x17b4` 에서 3개를 닫고 여기선 값이 0 이라 건너뜀.)
  5. `+0x1d3c ~ +0x1d94` CPUID(리프 1) 로 `x86_cpu_enable_ssse3`(ECX 비트 9), `x86_cpu_enable_simd`(EDX 비트 26 && ECX 비트 20 && ECX 비트 1 — 비트 이름은 `[추측]`) 정함.
  6. `+0x1d99 RtlIsProcessorFeaturePresent(0x29)` → `x86_cpu_enable_avx512` (AVX-512 기능 번호 41 `[추측]`). 둘 다 켜지면 CPUID 리프 7 의 ECX 비트 10 이 없을 때 도로 0 (`+0x1db4~+0x1de6`).
  7. `+0x1def SymCryptDetectCpuFeaturesByCpuid`: `g_SymCryptFlags` 비트 0 이 안 켜져 있을 때만 (`+0x1de6`). 이어 켜고(`OR.LOCK`), 버전 문자열 한 칸을 복사하고 `g_SymCryptCpuFeaturesPresentCheck = ~g_SymCryptCpuFeaturesNotPresent` (`+0x1dfb~+0x1e27`).
  8. **정상 반환 = `+0x1e2d XOR EAX,EAX ; JMP +0xa7b`** (공용 에필로그: 쿠키 확인, 레지스터 복원, `RET` `+0xaa7`). 호출한 곳은 `_LdrpInitialize +0x16c`.
- `+0x1e40 RtlRaiseStatus(0x80000002)` 와 `+0x1e34 __fastfail(3)` 은 **뒤쪽 어디서도 점프로 안 간다.** 닿는 점프는 앞쪽 `+0x784`, `+0xb22`, `+0x1299`, `+0x12c6` 뿐 (`.asm` 에서 `18008e238/18008e23f` 로 가는 줄). 그래서 이 두 호출은 "뒤쪽 호출 75" 에 세어도 뒤쪽 흐름에선 안 탄다.
- **`$fin$0` (예외 정리 조각, rva `0x169667`)**: `RtlDeactivateActivationContextUnsafeFast(RBP+0x200)` 만 부른다 (`RBP`=정리 때 받은 부모 프레임). 부모 쪽 `+0x1c85` 에서 켠 활성화 컨텍스트 프레임이 `[RSP+0x200]` 이므로, **TLS 콜백(`+0x1c94`)에서 예외가 빠져나올 때 같은 프레임을 끄는 정리** `[추측: 이름·오프셋 일치]`. 정상 길에서는 `+0x1ca2` 가 같은 일을 한다.
- **TLS 콜백을 부르는 조건** (`+0x1c31`): `[LdrpImageEntry+0x6e]`(`TlsIndex`, PDB) 가 0 이 아닐 때만. 인자 `ECX=1`(`DLL_PROCESS_ATTACH`), `RDX=LdrpImageEntry`. 앞서 `+0x1c85` 에서 항목의 `EntryPointActivationContext`(+0x88) 를 켠다. 같은 틀이 `LdrpInitializeNode +0x145~+0x166` 에도 있다 (그쪽은 모듈별). → exe 의 TLS 콜백은 import DLL 초기화(`+0x1bb6`)가 끝난 **뒤** `[정적]`, 관찰 run7·run8 과 같은 순서.
- **뒤쪽 실패 길 20개** (전부 같은 에필로그 `+0xa7b` 로 상태 반환). 대표:
  - 로그 후 반환 7개: `+0x12e9` (병렬 로딩 지원, 줄 0x1b45), `+0x18d8` (`LdrpInitializeTls`, 줄 0x1d0c), `+0x1947` (.NET 그래프), `+0x1969` (import 리다이렉션, 줄 0x1d41), `+0x19a8` (kernel32 준비, 줄 0x1d57), `+0x1b02` (올리기·실행 준비, 줄 0x1e46), `+0x1c0e` (**exe 그래프 초기화 실패**, 줄 0x1eda; 이 앞에 락 풀기·로드 문맥 해제·DLL 경로 해제는 이미 함).
  - `0xc0000017`(메모리 부족) 2개: `+0x1361` (exe 항목 할당 실패, 줄 0x1b5a), `+0x1582` (복제 항목, AppVerifier 만).
  - 상태 그대로 반환 11개: `+0x143b`, `+0x14f4`, `+0x165a`, `+0x1684`, `+0x174e`(AppVerifier 쪽 둘이 모임), `+0x17cc`(WOW64), `+0x1858`, `+0x1872`(.NET), `+0x1b2f`(`LdrpInitializePatchData`), `+0x1b64`(kernel32 thunk), `+0x1b7d`(사용자 Windows 폴더).
  - 이 중 AppVerifier 전용 4, WOW64 전용 1, .NET 전용 3 → test.exe 가 갈 수 있는 것은 12개. 대표 2~3: `+0x1361`(exe 항목 할당 실패), `+0x1b02`(import 올리기 실패 — dll 없음 같은 오류), `+0x1c0e`(DLL 초기화 실패).
  - `+0x1522` 현재 폴더 초기화 실패만 **실패 길이 아니다** (로그만, 계속).
  - 호출한 `_LdrpInitialize +0x172 JS` 쪽 처리는 sast/12 몫.

## 그림 2 에 넣을 `(정적)` 상자 후보 (5개, 관찰 `(6)~(8)` 은 `+0x19a2`, `+0x1bbb`, `+0x1c99` 이므로 그 사이·앞)

1. **exe 모듈 항목 만들기** (`LdrpAllocateModuleEntry +0x1359`, 저장 `+0x1374 LdrpImageEntry`). (6) 앞. 이유: exe 가 모듈 목록에 들어가는 자리이고 항목 칸(`BaseDllName`=파일 이름, `LoadCount=-1`)이 이때 정해진다.
2. **병렬 로딩 켜기 + `LdrInitState=1`** (`+0x1a42 / +0x1a4c`). (6) 뒤. 이유: 일꾼(스레드 풀) 이 처음 만들어질 수 있는 자리, 올리기 시작 표시.
3. **exe import 올리고 연결** (`LdrpMapAndSnapDependency +0x1aa4`, 이어 `LdrpPrepareModuleForExecution +0x1aed`, `LdrInitState=2` `+0x1b1e`). (6)~(7) 사이. 이유: dll1·dll2 가 올라오는 가장 유력한 자리 `[추측]`, 이후 `+0x1bb6` 까지가 "초기화" 쪽.
4. **kernel32 스레드 시작 thunk 를 `(1,0,0)` 으로 호출** (`+0x1b59`). (7) 앞. 이유: 값은 (6) 단계 `+0x199d` 가 채운 전역이고, 새 스레드가 쓰는 그 함수를 프로세스 초기화 때도 한 번 부른다.
5. **`PEB.PostProcessInitRoutine` 호출** (`+0x1d16`). (8) 뒤. 이유: 정상 반환 직전의 마지막 사용자 코드 후보.

(참고 갈림 표시: `+0x1766` WOW64 이면 `+0x17c1 LdrpLoadWow64` 쪽, x64 네이티브는 `+0x1806` 쪽.)

## 동적분석으로 확인해야 할 것

주소는 `ntdll 시작 + 0x8c404 + 오프셋` (test.exe 일 때만 멈춤 조건 필요). 기존 대기 Q44 (앞쪽 실행 값)와 같은 정지 방식이라 한 runbook 으로 묶어도 된다. Q14·Q22·Q24 와는 안 겹친다 (Q14 정적은 이 결과서가 `+0x1aa4` 로 후보를 좁힘, 동적 확인은 아래 Q48). 아래는 **대기열 맨 뒤에 붙일 새 항목 제안** (번호는 오케스트레이터가 정함).

- **Q46 (동적) 갈림 값 한 번에**: 
  - `+0x1568` (`ntdll+0x8d96c`): `dd ntdll+0x1e7044 L1` `AvrfAppVerifierMode`=0 인가 (0 이면 둘째 묶음 건너뜀).
  - `+0x1766` (`ntdll+0x8db6a`): `dd ntdll+0x1d2868 L1` `UseWOW64`=0 인가.
  - `+0x1980` (`ntdll+0x8dd84`): `dw @r13+5c L1` `Subsystem`=3 인가.
  - `+0x1b35` (`ntdll+0x8df39`): `db @$peb+2 L1` `BeingDebugged` (커널 디버거만 붙었을 때) 가 0 인가.
  - `+0x1bb6` (`ntdll+0x8dfba`): `r rcx rdx r8`, `rcx` 가 `poi(ntdll+0x1d2828)+0x98` 와 같은가.
  - `+0x1c31` (`ntdll+0x8e035`): `dw poi(ntdll+0x1d2828)+0x6e L1` `TlsIndex` ≠ 0 인가. (Q44 와 같은 정지 안에서)
- **Q47 (동적) exe 항목 값**: `+0x1455`(ret) 뒤 `ntdll+0x8d859` 에서 `dt ntdll!_LDR_DATA_TABLE_ENTRY poi(ntdll+0x1d2828) BaseDllName FullDllName DllBase Flags` — `BaseDllName`=`test.exe`(정정 확인), `DllBase`=`lm m test` 시작, `Flags` 에 `ImageDll`(0x4). `+0x1424` (`ntdll+0x8d828`) 에서 `[r13+0x30]` 와 `[rdi+0x10]` 가 같은가 (재배치 호출을 건너뛰나).
- **Q48 (동적) dll1·dll2 가 올라오는 호출**: `lm m dll*` 를 `+0x1aa4` 직전(`ntdll+0x8dea8`), `+0x1aa9`(ret, `ntdll+0x8dead`), `+0x1af2`(ret, `ntdll+0x8def6`) 에서 비교. `+0x1aa4` 에서 `r rcx` 와 `poi(poi(ntdll+0x1d2828)+0xb0)` 비교 (`LoadContext` 와 같은 구조체인가). `LdrpInitializeNode` 가 `+0x1aed` 안 `LdrpInitializeGraphRecurse` 에서 먼저 불리는지, `+0x1bb6` 에서 불리는지 (`k`). Q15 와 이어짐.
- **Q49 (동적) 일꾼 스레드 최초 시점**: `+0x1a42`(`ntdll+0x8de46`) `r ecx` (`LoaderThreads`), 이어 `+0x1a47`, `+0x1aa9`, `+0x1ab1`, `+0x1b91` 에서 스레드 목록 (`~` 개수). 필요하면 `ntdll!TpAllocPoolInternal`, `ntdll!LdrpQueueWork` 에 test.exe 조건 멈춤점. 이 구간에서 스레드가 새로 생기는가 / 몇 개인가.
- **Q50 (동적) 간접 호출 대상**: `+0x1b59`(`ntdll+0x8df5d`) `r rax; ln @rax`; `+0x1d16`(`ntdll+0x8e11a`) `dq @$peb+0x230 L1` (0 이면 안 부름); `+0x1cdf`(`ntdll+0x8e0e3`) 는 `dd ntdll+0x1d23cc L1`(`g_ShimsEnabled`)=0 이면 안 탐을 확인.
- 이미 관찰된 `+0x19a2`, `+0x1bbb`, `+0x1c99` 는 다시 안 해도 된다 (run7·run8). 정상 반환(`+0x1e2d`, `ntdll+0x8e231`) 에서 `r rax`=0 확인은 덤.

## Q10 ~ Q16 에 넘기는 연결점

- **Q10**: `+0x199d` 의 `RCX=&[RSP+0x188]` 는 출력 칸이고 나중에 `+0x1b6a` 가 `LdrpInitializePerUserWindowsDirectory` 에 넘긴다. `Kernel32ThreadInitThunkFunction` 칸은 이 함수 `+0xe4` 가 채운다 (`refs`). 이 호출은 `LdrInitState`=0 일 때 (`+0x1a4c` 전). 또 `LdrpAllocateModuleEntry +0xc2`·`LdrpLoadDllInternal +0x29b` 가 `LdrInitState` 를 읽는다.
- **Q11 / Q12**: `+0x1aed LdrpPrepareModuleForExecution(LdrpImageEntry, &상태)` 의 callee 에 `LdrpInitializeGraphRecurse`(`+0xea`), `LdrpCondenseGraph`, `LdrpNotifyLoadOfGraph` 가 있다. `+0x1351` 의 로드 문맥과 `_LDR_DATA_TABLE_ENTRY.LoadContext`(+0xb0), `+0x1be2 LdrpFreeLoadContextOfNode` 의 관계 확인. 항목의 DDAG `State` 가 9 → 2 로 바뀌는 줄(`+0x1508`, `+0x1a60`).
- **Q14**: 올리기 후보 1순위 = `+0x1aa4 LdrpMapAndSnapDependency`(callee `LdrpLoadDependentModuleInternal`, `LdrpSnapModule`, `LdrpQueueWork`). 첫 인자는 항목이 아니라 지역 로드 문맥 구조체.
- **Q15**: `+0x1bb6` 인자 = (exe DDAG 노드, 상태 칸, 바이트 0). `LdrpProcessInitContextRecord`(= CONTEXT) 는 `+0x1b83` 에서 켜고 `+0x1bec` 에서 끄며, 읽는 곳은 `LdrpInitializeNode +0x17a` 하나 (`Flags.ProcessStaticImport`(0x20) 모듈에서 `LdrpCallInitRoutine` 에 넘기는 문맥). 즉 import DLL 초기화는 이 CONTEXT 와 함께 불린다.
- **Q16**: exe TLS 콜백은 `TlsIndex≠0` 일 때만 (`+0x1c31`), `LdrpInitializeNode +0x145~+0x166` 와 같은 틀. `TlsIndex` 를 누가 exe 항목에 쓰는지(`LdrpInitializeTls` `+0x18cb` 로 보임 `[추측]`)가 Q16 의 확인점.
- **Q45**: 이번에 안 건드림.

## 판정

**분석성공** — 질문 1~8 전부 답함. 정정 하나 (sast/13 의 `[RSP+0x130]` 해석). 동적 확인 5건 제안 (Q46~Q50). 스크립트 문제 없음.

# 42 system-dll-boot — 정적 분석 결과

- 요청서: `_workspace/sast/42_system-dll-boot.md` (Q93)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), PDB guid+age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 **같음**)
- 버전 확인 (알려진 사실): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`). **일치** — 스크립트·바이너리 믿을 수 있음.
- 새로 뽑은 것 (depth 0, `ms/decomp/ntoskrnl/` + `ms/disassemble/ntoskrnl/`): `PsLocateSystemDlls` · `PspReferenceSystemDll` · `IoInitSystemPreDrivers` (요청서 대상) + 길을 따라 더한 것: `PsQuerySystemDllInfo`·`PspInitPhase2`·`MmInitSystemDll`·`PsInitSystem`·`Phase1Initialization`·`Phase1InitializationIoReady`·`Phase1InitializationDiscard`·`IoInitSystem`·`InitBootProcessor`·`PspInitPhase3` (부팅 앞뒤 순서 확인), `MiDoesImageContainFunctionOverrideFixups`·`RtlFindExportedRoutineByName`·`VslGetNestedPageProtectionFlags`·`ObInitializeFastReference` (`PspLocateSystemDll` 줄의 뜻), `NtSystemDebugControl`·`KdSystemDebugControl`·`ExpKdPullRemoteFileForUser` (`+0x7e` 의 정체), `PspLocateNtdllAddressesForScpCfg`·`PspCopyNtdllExport`·`PspWow64PickBestNtdll` (`PsQuerySystemDllInfo`/`PspLookupEntryPoint` 다른 쓰는 곳 확인용 — **`PspWow64PickBestNtdll` 은 정규식에 같이 들어갔을 뿐 안 읽음**).
- 이미 있는 것 읽기만: `PspLocateSystemDll`·`PspMapSystemDll`·`PspInitializeSystemDlls` (다시 안 뽑음). **알림: `MiInitializeCreateSectionPacket.c`·`MiSectionControlArea.c` 는 이미 있었는데 확인 없이 다시 뽑아 같은 방식으로 덮어썼다** (내용 달라질 일 없음, 읽기용 비교는 안 함).
- 새 `_refs`: `PspSystemDlls`(0x30)·`PspNativeSystemDll`·`PsLocateSystemDlls`·`PspLocateSystemDll`·`PspMapSystemDll`·`PsInitSystem`·`PspInitializeSystemDlls`·`IoInitSystemPreDrivers`·`PspLocateNtdllAddressesForScpCfg`·`PspCopyNtdllExport`. 새 지도: `ms/disassemble/ntoskrnl/_map/PsLocateSystemDlls_*` (깊이 3, 43개).
- 파일에 박힌 처음 값 (표 칸·문자열·`NtdllExportInformation`) 은 PE 파일 바이트를 직접 읽었다 (임시 파이썬, 스크래치패드). PDB 공개 이름으로 주소를 이름에 맞췄다.
- 표시: `[정적]` 코드·파일 바이트 / `[정적] PDB` PDB 이름·타입 / `[관찰]` 다른 run 의 디버거 값 (인용만) / `[추측]` / `[공백]`. "불린다" 대신 "부르는 코드가 있다".

---

## 0. 맨 앞 요약 — 부팅 준비 호출 사슬

(오프셋 = 함수 시작에서. `ret` 이 WinDbg 콜스택 `caller+0x..` 와 같은 값. 모두 `_calls.tsv`·`.asm` 기준 `[정적]`)

| # | 함수 (caller → callee) | call | ret | 하는 일 | 실패하면 |
|---|---|---|---|---|---|
| 앞 | `InitBootProcessor` → `PsInitSystem(0)` → `PspInitPhase0` | `+0xbad` | `+0xbb2` | (이름만) 프로세스 쪽 0단계. 이 구간 앞 | — |
| 앞 | `Phase1InitializationDiscard` → `PsInitSystem(1)` → `PspInitPhase1` | `+0xc24` | `+0xc29` | (이름만) 인자 1 은 `EBX` (`+0xa8f` 에서 1) `[추측]` | — |
| 1 | `Phase1Initialization` → `IoInitSystem` | `+0x36` | `+0x3b` | 부팅 스레드의 `Phase1` 이 I/O 초기화로. (`Discard` 는 `+0x1e` 에서 먼저 불림) | 실패값이면 `KeBugCheckEx(0x69, IopInitFailCode, …)` (`+0x55`) |
| 2 | `IoInitSystem` → `IoInitSystemPreDrivers` | `+0x12` | `+0x17` | 드라이버 올리기 전 준비 (4821바이트) | — |
| 3 | `IoInitSystemPreDrivers` → `PsLocateSystemDlls` | **`+0x10f5`** | **`+0x10fa`** | 시스템 DLL 찾기 시작. **불리기 직전 `NtGlobalFlag \|= 0x40000` (`+0x10e3~+0x10ef`), 돌아와서 원래 값 복원 (`+0x10fa`)** | `PsLocateSystemDlls` 는 늘 0 을 돌려줘서 `IopInitFailCode=7` 갈래 (`+0x1114`) 는 못 탄다 |
| 4 | `PsLocateSystemDlls` → `ExVerifySuite(6)` | `+0x14` | `+0x19` | 6번 제품군 + `PsEmbeddedNTMask` 비트0 이면 **전체 건너뜀** (`+0x1d~+0x25` → `+0x145`) | — |
| 5 | `PsLocateSystemDlls` → `VslGetNestedPageProtectionFlags(0)` | `+0x2d` | `+0x32` | 비트 `4` 만 남김 (`+0x3b`) | — |
| 6 | `PsLocateSystemDlls` → **`PspLocateSystemDll`** | **`+0x9e`** | **`+0xa3`** | 표 6칸 돌며 (비 NULL·플래그 `0x20` 꺼짐) 칸마다 한 번. 인자 `EDX` = (칸 플래그 `0x10` 켜짐 ? 위 비트4 : 0) | 음수 + 칸 플래그 비트0 → `+0xcf/+0xd4 KeBugCheckEx(0x6b, 상태, 2, 칸번호, 0)`. 비트0 꺼진 칸은 그냥 다음 칸 |
| 6' | `PsLocateSystemDlls` → `PspMapSystemDll` (별칭 칸) | `+0x84` | **`+0x89`** | 플래그 `0x20` 켜진 칸만: 0번 칸 `+0x38` 복사(`+0x70`) 후 System 프로세스에 올림 (인자 3·4 = 0·1). **이 빌드 표엔 그런 칸이 없다** | 음수면 그 칸을 NULL 로 (`+0x8d`), 부팅은 계속 |
| 7a | `PspLocateSystemDll` → `ZwSystemDebugControl` | `+0x79` | `+0x7e` | 명령 `0x26` (파일 끌어오기), 경로 16바이트 입력. `NtGlobalFlag & 0x40000` 일 때만 → **3번의 임시 비트 때문에 늘 켜짐** | 반환값 안 봄 |
| 7b | → `ZwOpenFile` | `+0xc0` | `+0xc5` | 칸의 경로 (`+0x10`) 로 DLL 파일 열기 | 음수면 그대로 돌려줌 (6 에서 판단) |
| 7c | → `MmCreateSpecialImageSection` | `+0xef` | `+0xf4` | 이미지 섹션 만들기 (exe 와 같은 함수·사슬, 인자만 다름 — 2장) | 음수 → `+0x10b/+0x110 KeBugCheckEx(0x6b, 상태, 3, 0, 0)` |
| 7d | → `ObCloseHandle` (파일 핸들) | `+0x117` | `+0x11c` | 파일 핸들 닫기 | — |
| 7e | → `ObReferenceObjectByHandle` | `+0x141` | `+0x146` | 섹션 핸들 → 섹션 객체 (`MmSectionObjectType`, 접근 `0xf001f`, 커널 모드) | 음수 → `+0x15d/+0x162` 사이트 4 |
| 7f | → `ObCloseHandle` (섹션 핸들) | `+0x169` | `+0x16e` | 섹션 핸들 닫기 (객체 참조는 남김) | — |
| 7g | → `MmGetSectionInformation(섹션, 2, …)` | `+0x17e` | `+0x183` | 재배치량 → **칸 `+0x38`** (`+0x1aa`) | 음수 → `+0x19a/+0x19f` 사이트 8 |
| 7h | → `ObInitializeFastReference` | `+0x1ae` | `+0x1b3` | 섹션 객체를 칸 `+0x0` 가 가리키는 빠른 참조에 넣음 (15개 덤으로 참조), 자물쇠 칸 `+0x8`=0 (`+0x1b6`) | — |
| 7i | → `MmGetSectionInformation(섹션, 4, …)` | `+0x1cd` | `+0x1d2` | (칸 플래그 `0x21` 중 하나일 때만) 이미지 정보 + 부가 비트 | 아래 7j~7k 와 함께 사이트 9 |
| 7j | → `MiSectionControlArea` | `+0x1e5` | `+0x1ea` | 섹션 → 컨트롤 영역 | — |
| 7k | → `MiDoesImageContainFunctionOverrideFixups` | `+0x1f5` | `+0x1fa` | 컨트롤 영역의 로드 구성에 "함수 덮어쓰기 고침" 이 있나 | 없음/비트 모자람 → `+0x211/+0x216` 사이트 9 |
| 7l | → **`PspMapSystemDll`** (부팅 갈래) | **`+0x233`** | **`+0x238`** | 현재 프로세스에 올리고 칸 `+0x20/+0xc/+0x28` 채움 (인자 3·4 = 0·**1**) | 음수 → `+0x24f/+0x254` 사이트 5 |
| 8a | `PspMapSystemDll` → `PspReferenceSystemDll` | `+0x3b` | `+0x40` | 칸 `+0x0` 의 빠른 참조로 섹션을 하나 잡음 | NULL → `0xc0000001` |
| 8b | → `MmMapViewOfSectionEx` | `+0x118` | `+0x11d` | 섹션을 프로세스에 올림 | 상태 그대로 |
| 8c | → `ObFastDereferenceObject` | `+0x12d` | `+0x132` | 잡은 참조 돌려줌 (태그 `PsSd`) | — |
| 8d | → `RtlImageNtHeader` | `+0x1ae` | `+0x1b3` | (부팅 갈래만) 올린 뷰의 NT 헤더 찾기 → 칸 쓰기 (2.3) | — |
| 9 | `Phase1Initialization` → `Phase1InitializationIoReady` | `+0x61` | `+0x66` | I/O 끝난 뒤 나머지 (3 이후) | — |
| 10 | `Phase1InitializationIoReady` → `PsInitSystem(2)` → `PspInitPhase2` | `+0x1d5` / `PsInitSystem +0x3b` | `+0x1da` / `+0x40` | 프로세스 쪽 2단계 | 거짓이면 IoReady 가 실패 갈래 (`+0x1dc`) |
| 10a | `PspInitPhase2` → **`PspInitializeSystemDlls`** | `+0x172` | `+0x177` | 칸마다 이름 표를 보고 ntdll 함수 주소를 찾아 **`PsNtdllExports`** 등에 씀 | 아래 사이트 6 |
| 10b | `PspInitializeSystemDlls` → `PsQuerySystemDllInfo` | `+0x2d` | `+0x32` | 칸 `+0x20 ≠ 0` 인 칸만 (아니면 건너뜀) | — |
| 10c | → `PspLookupEntryPoint` (→ `RtlFindExportedRoutineByName` `+0x1b/+0x20`) | `+0x56` | `+0x5b` | 이름 → 주소 | 음수 → `+0xa0/+0xa5 KeBugCheckEx(0x6b, 상태, 6, 0, 0)` |
| 10d | `PspInitializeSystemDlls` 쓰는 줄 | `+0x6b` | — | `MOV [RCX],RAX` — `PsNtdllExports` 칸에 씀 | — |
| 10e | → `PspWow64GetSharedInformation` (칸 1 일 때) | `+0x77` | `+0x7c` | `PsWowX86SharedInformation +0x30` ← WOW ntdll 칸 `+0x20` (`+0x80`) | — |
| 11 | `Phase1InitializationIoReady` → `MmInitSystemDll` | `+0x1e2` | `+0x1e7` | `MiState.Processes.SystemDllBase` ← 0번 칸 `+0x20` (쓰는 줄 `MmInitSystemDll +0x22`) | — |
| 12 | `Phase1InitializationIoReady` → `PsInitSystem(3)` → `PspInitPhase3` | `+0x20a` / `PsInitSystem +0x34` | `+0x20f` / `+0x39` | **`VslVsmEnabled` 일 때만**: `PsSecureSystemProcess` 에 0번·4번 칸 올림 (`PspInitPhase3 +0x6b/+0x70`, `+0xa0/+0xa5`, 인자 3·4 = 0·0) 후 `VslpEnterIumSecureMode` (`+0xea`) | — |

사이트 번호 = `KeBugCheckEx(0x6b, 상태값, 사이트, 칸번호 또는 0, 0)` 의 둘째 인자 (자세한 조건은 2.2 표).

---

## 1. 한눈 결론

1. 시스템 DLL 준비는 **한 번만** (부팅 `Phase1`): `IoInitSystemPreDrivers +0x10f5` → `PsLocateSystemDlls` → (칸마다) `PspLocateSystemDll` → 섹션 만들고 → `PspMapSystemDll` (부팅 갈래, 인자4=1) 로 현재 프로세스 (System 프로세스로 보임 `[추측]`, D3) 에 올리고 칸 `+0x20/+0xc/+0x28` 를 채움. 이 뒤 `PspInitPhase2` 에서 `PsNtdllExports` 를 채움. **프로세스마다 (`PsMapSystemDlls`) 는 같은 섹션을 올리고 `+0x20` 과 같은 주소에 붙었는지 비교만 한다.** 사용자 `[추측]` ("부팅 때 준비되고 프로세스마다는 붙이기만") **맞음** `[정적]` — 단, 프로세스마다 `PspPrepareSystemDllInitBlock` 이 재배치량(`+0x38`) 과 `PsNtdllExports` 를 읽어 init 블록을 새로 만든다 (붙이기 + 읽기).
2. `PspSystemDlls` 는 6칸 포인터 표. 파일에 박힌 처음 값: `[0]` = `PspNativeSystemDllData` (`\SystemRoot\System32\ntdll.dll`), `[1]` = `PspWowX86SystemDllData` (`\SystemRoot\SysWOW64\ntdll.dll`), `[4]` = `PspVsmEnclaveRuntimeDllData` (`\SystemRoot\System32\vertdll.dll`), `[2][3][5]` = 0. 그래서 이 빌드에서 `PspLocateSystemDll` 이 **세 번** (ntdll · WOW ntdll · vertdll) 불릴 코드 경로가 있다 (WOW 도 도는지 → **예, 부르는 코드가 있다** `[정적]`, 표 처음 값 기준).
3. `+0x7e ZwSystemDebugControl` 은 **명령 `0x26` (커널 디버거에서 파일 끌어오기)**. `IoInitSystemPreDrivers` 가 `NtGlobalFlag` 비트 `0x40000` 을 `PsLocateSystemDlls` 동안만 켜 주기 때문에 **조건부가 아니라 사실상 늘 부른다**. 커널 디버깅을 안 켠 부팅이면 `0xc0000354` 로 끝, 켠 부팅이면 파일 이름을 해시해 한 번씩 `KdPullRemoteFile` 까지 간다 — **디버거가 붙어서 달라지는 점**.
4. `PspSystemDlls[0]+0x20` = 올린 뷰의 NT 헤더 `ImageBase` 칸 값 (`RtlImageNtHeader`), `+0x28` = 그 뷰의 시작 주소 (System 프로세스 안), `+0xc` = `SizeOfImage`. 무작위 기본 주소는 이보다 앞서 **섹션 만들 때 MM (`MiRelocateImage → MiSelectImageBase`, `ExGenRandom`)** 이 고른다. 뷰 헤더 `ImageBase` 칸이 새 주소로 바뀌는 줄은 안 따라갔다 `[공백]`.
5. `PsNtdllExports` 를 채우는 줄은 **이 구간(부팅 `PspInitPhase2`)에 있다**: `PspInitializeSystemDlls +0x6b`. 이름 → 주소는 `PspLookupEntryPoint` 가 `RtlFindExportedRoutineByName` (export 표 읽기) 로 찾는다. 이름 19개 (ntdll), WOW 8개, vertdll 0개.

---

## 2. 질문별 답 [정적]

### 2.1 질문 1 — 부르는 자리와 순서

**(a) `PsLocateSystemDlls` 의 caller**
- 답: **예, 맞다.** `IoInitSystemPreDrivers +0x10f5` 가 call 명령 위치이고 **ret 는 `+0x10fa`** (run16 의 `+0x10f5` 는 call 쪽 오프셋 — WinDbg 콜스택은 `+0x10fa`). 다른 caller **없음**.
- 근거: `ms/disassemble/ntoskrnl/IoInitSystemPreDrivers.asm` `+0x10f5` (`CALL 0x14077e070 ; ret=+0x10fa -> PsLocateSystemDlls`). `_refs/PsLocateSystemDlls.tsv`: 코드 호출 1줄 (`IoInitSystemPreDrivers+0x10f5`) + `.pdata` 데이터 참조 1줄 (`함수 밖`), 함수 포인터 표 속 값(`ptr`) 0곳.
- 앞뒤 (같은 `.asm`): 바로 앞 `+0x10d5 RtlIsStateSeparationEnabled` → `+0x10de PpLastGoodDoBootProcessing` (조건), 바로 뒤 `+0x1126 MmInitializeImageViewExtension` → `+0x112d PfSnBeginBootPhase` → `+0x1139 IopReassignSystemRoot`. **부팅 드라이버를 올리는 `+0xf3b IopInitializeBootDrivers` 보다 뒤, `\SystemRoot` 를 NT 이름으로 다시 매기는 `IopReassignSystemRoot` 보다 앞**이다. 그래서 `\SystemRoot` 링크가 이 호출 전에 이미 있어야 한다 (만든 곳은 안 봄 `[공백]`).
- 부팅 안 순서 (정적 호출 순서): `Phase1Initialization` 이 `Discard (+0x1e)` → `IoInitSystem (+0x36)` → `Phase1InitializationIoReady (+0x61)` 순으로 부른다. 그래서 **`PsLocateSystemDlls`(IoInitSystem 안) → `PspInitPhase2`(IoReady `+0x1d5`, 인자 `R12D`=2) → `MmInitSystemDll`(IoReady `+0x1e2`) → `PspInitPhase3`(IoReady `+0x20a`, 인자 `EDI`=3)** 순서다 (IoReady `+0x148 MOV R12D,2`, `+0xfc MOV EDI,3`). `PspInitializeSystemDlls` 는 `PsQuerySystemDllInfo` 가 칸 `+0x20 = 0` 이면 NULL 을 줘서 건너뛰므로 (`+0x38 JZ`), **`PsLocateSystemDlls` 가 먼저 끝나야** 의미 있다 — 코드 순서가 그렇다.

**(b) `PsLocateSystemDlls → PspLocateSystemDll` 몇 번, 어떤 인자**
- 답: 호출 자리는 **한 줄** (`PsLocateSystemDlls +0x9e`, ret `+0xa3`), 6칸 고리 안. 칸마다 (`PspSystemDlls[i]` 가 NULL 이 아니고 칸 플래그 `+0x8` 의 비트 `0x20` 이 꺼져 있으면) 한 번. 인자: `RCX` = 칸 주소, `EDX` = `(칸 플래그 & 0x10 ? VslGetNestedPageProtectionFlags(0) & 4 : 0)` (`+0x93~+0x9c`).
- 파일 처음 값 기준 (아래 2.4 표): 비 NULL 은 `[0]` 플래그 `0x86640013`, `[1]` `0x014c0008`, `[4]` `0x86640012`. 셋 다 `0x20` 꺼짐 → **세 번**. 인자: `[0]`·`[4]` 는 `0x10` 켜짐 → `EDX` = 가상화 쪽 비트4 (VBS 없는 VM 이면 0 일 것 `[추측]`), `[1]` 은 `0x10` 꺼짐 → 0.
- **WOW64 쪽 ntdll 도 도나**: 예, 부르는 코드가 있다 (`[1]` SysWOW64 ntdll, 필수 비트 없음). 못 열면 조용히 넘어간다 (6 의 실패 열). vertdll (`[4]`) 도 같다. 프로세스 종류에 따른 조건은 **여기엔 없다** — 부팅 때는 칸이 있으면 다 찾는다. 프로세스 종류 (WOW 프로세스·보안 프로세스) 조건은 `PsMapSystemDlls` 쪽 (2.5).
- 칸에 쓰는 줄이 `PspSystemDlls` 에 하나뿐 (`PsLocateSystemDlls +0x8d` 에서 NULL 로) → `[2][3][5]` 는 이 빌드에서 안 바뀐다 (`_refs/PspSystemDlls.tsv` 쓰기 1줄) `[정적]`. 레지스터+오프셋으로 표에 쓰는 줄은 refs 가 못 잡으니 값은 D1 로.
- 근거: `PsLocateSystemDlls.asm` `+0x34`(표 주소) `+0x40~+0xb8`(고리), `_refs/PspLocateSystemDll.tsv` (코드 호출 1줄), 처음 값은 PE 바이트 (`PspSystemDlls` rva `0xfd8130`).
- `ExVerifySuite(6)` 가 참이고 `PsEmbeddedNTMask` 비트0 이 켜져 있으면 (`+0x14~+0x25`) 이 고리 전체를 건너뛴다 (EmbeddedNT 제품 `[추측]`, 6번 = SDK 의 EmbeddedNT 로 기억). 일반 Win11 Edu 는 아닐 것.

**(c) `PsLocateSystemDlls +0x89` 는 무엇인가**
- 답: `+0x84` 의 `call PspMapSystemDll` 이 돌아오는 자리. **칸 플래그 `0x20` 이 켜진 "별칭 칸"** 전용 갈래: 0번 칸의 `+0x38` 을 이 칸에 복사 (`+0x6c/+0x70`) 하고 `PspMapSystemDll(현재 프로세스, 이 칸, 0, 1)` 로 올림 (인자: `RCX` = 현재 스레드의 `ApcState.Process` (`GS:[0x188]` `+0xb8`), `RDX` = 칸, `R8D`=0, `R9D`=1). 음수면 그 칸을 NULL (`+0x8d`). **새 파일을 찾지 않는다** (`PspLocateSystemDll` 을 안 부름) — 0번 칸의 섹션을 쓰는 걸로 보이나 `[0]` 칸과 `+0x0` 포인터를 같이 쓰는지는 이 빌드에 이런 칸이 없어 못 봄 `[추측]` (`PspMapSystemDll` 이 이 플래그를 보고 `ImageMachine` 확장 인자를 하나 더 붙이는 것과 짝 — 2.3).
- 이 빌드 표 처음 값에는 이런 칸이 없어서 **이 갈래는 안 탄다** `[정적]` (표 처음 값 기준).

### 2.2 질문 2 — `PspLocateSystemDll` 이 하는 일 (줄마다)

**칸 레이아웃 (칸 하나 = 0x40바이트, `PspNativeSystemDllData` 등)**: 아래 2.4 표. PDB 에 `_PS_SYSTEM_DLL_INFO` 는 함수 서명으로 이름만 있고 칸 정의가 없다 (`pdb_struct.py` "없음") `[정적] PDB` → 오프셋만 쓴다. `PsQuerySystemDllInfo(i)` 가 돌려주는 포인터는 **칸 주소 + 8** (칸 `+0x20 ≠ 0` 일 때만).

- **파일 경로 출처**: 칸 `+0x10` 의 `UNICODE_STRING` (길이 `0x3c`, 버퍼는 `+0x18` 이 가리키는 파일 안 문자열). 같은 16바이트를 `ZwSystemDebugControl` 입력으로도 복사한다 (`+0x59 MOVUPS [RBX+0x10]`). `OBJECT_ATTRIBUTES.ObjectName` = 칸 `+0x10` 주소 (`+0x86~+0x9b`).
- **`+0xc5 ZwOpenFile`** (`+0xc0` call): 접근 `0x100020` (SYNCHRONIZE + 실행 `0x20`), 속성 `0x240` (대소문자 무시 `0x40` + 커널 핸들 `0x200`), 공유 `1` (읽기 공유), 옵션 `0`. 실패하면 상태값을 그대로 돌려줌 (핸들 닫기·버그체크 없음).
- **`+0xf4 MmCreateSpecialImageSection`** (`+0xef` call) — exe 섹션 사슬 (run31·run47, `NtCreateUserProcess +0x937/+0x93c`) 과 **같은 함수 → 같은 안쪽 사슬** (`MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap …`). **다른 점 (인자)**:

  | 인자 | exe (`NtCreateUserProcess +0x937`) | 시스템 DLL (`PspLocateSystemDll +0xef`) |
  |---|---|---|
  | 이름 | (run31) | 섹션 객체 이름 **없음** (`ObjectName` 를 `+0xd8` 에서 NULL) |
  | 토큰 (3번째) | 프로세스 쪽 토큰 | **NULL (`R8D=0`)** |
  | 서명 수준 (4번째) | 부모 쪽에서 구한 값 | **`0x0c`** (`+0xe4 MOV R9B,0xc`) |
  | 플래그 비트0 | 늘 켜짐 → 할당 속성 `SEC_IMAGE 0x01000000` | **꺼짐** → 할당 속성 **`0x02400000`** (`MmCreateSpecialImageSection +0x43~+0x56`) |
  | 플래그 비트2 (`4`) | — | 칸 `0x10` + VSL 비트4 일 때 켜짐 → `MiCreateSection` 플래그 `8` (`+0x4d`, `+0x5c~+0x60`) |
  | 플래그 비트1 (`2`) | — | 켜짐 (`+0xd5 OR ESI,2`). **`MmCreateSpecialImageSection` 이 안 읽는 비트** (`.asm` 에서 비트 0·2·3 만 검사) |

  - 할당 속성 `0x02400000` 의 뜻 (`MiInitializeCreateSectionPacket.asm` `+0x346~+0x35a`, 조각 `FUN_140b23a4f`): `0x02000000` 비트 켜짐 → 패킷 플래그 `0x10` 켬, `0x00400000` 도 켜져 있으면 비트 지우고 **서명 수준 칸 (패킷 `+0x18`) 을 `0x0c` 로 덮음** (꺼져 있으면 `4`), 그리고 할당 속성에서 `0x02000000` 지우고 `SEC_IMAGE (0x01000000)` 를 켬. 그래서 **결국 SEC_IMAGE 이지만 "보호된 이미지" 패킷 플래그 `0x10` + 서명 수준 `0x0c` 가 붙은 섹션** `[정적]` (이름 `SEC_PROTECTED_IMAGE`·`SEC_NO_CHANGE`·`SE_SIGNING_LEVEL_WINDOWS` 는 SDK 에서 기억한 것 `[추측]`).
  - 패킷 플래그 `0x10` 이 `MiCreateNewSection` 에서 쓰이는 곳: 이미지 종류 값을 `2` (또는 `0x800` 켜지면 `8`) 로 정해 `SeGetImageRequiredSigningLevel` 에 넘기고 (`MiCreateNewSection.c` 약 158~172줄), `0x2200` 비트가 켜진 경우의 별도 서명 검사를 건너뛴다 — **`.c` 로만 읽음 `[정적]` (`.asm` 줄 대조 안 함)**. 서명 수준 검사 본체는 안 따라감 `[공백]`.
  - 사슬 뒤쪽은 같다: 끝나면 `MiSectionControlArea`·`MiReferenceControlAreaFile`·`CcZeroEndOfLastPage`·`MiDereferenceControlAreaFile` 후 `ObInsertObjectEx` (접근 `0xf001f`) 로 핸들을 돌려줌 (`MmCreateSpecialImageSection +0xf6~+0x146`, run31).
  - 상태 `0xc0000054` 면 `KeDelayExecutionThread` 로 쉬고 다시 (`+0xdd~+0xea`) — 파일을 못 쓸 때 재시도.
  - 음수면 `+0x10b/+0x110 KeBugCheckEx(0x6b, 상태, 3, 0, 0)`.
- **`+0x7e ZwSystemDebugControl`** (`+0x79` call): 인자 `ECX = 0x26` (`LEA ECX,[RDI+0x16]`, `EDI=0x10`), 입력 = 경로 16바이트 복사본, 입력 길이 `0x10`, 출력 NULL·0·0. 조건: `NtGlobalFlag & 0x40000` (`+0x49`) — **`IoInitSystemPreDrivers +0x10e3~+0x10ef` 가 `PsLocateSystemDlls` 를 부르는 동안만 이 비트를 켜 놓고 (`+0x10fa` 에서 복원) 있어서 이 호출은 늘 켜져 있다** `[정적]`. 명령 `0x26` 은 `NtSystemDebugControl` 에서 디버그 권한 검사를 면제 (`+0xc8 CMP EBX,0x26`) 하고, 입력 길이 `0x10`·출력 길이 `0` 이면 `ExpKdPullRemoteFileForUser` (`NtSystemDebugControl` 조각 `FUN_140b3e0e7 +0x1f8` ret `+0x1fd`) 로 간다. 이 함수는 경로를 소문자로 만들고 SHA-256 해시해 "이미 끌어온 파일 표" (`ExpSysDbgPulledFileTable`) 에 없으면 `KdPullRemoteFile` 을 부른다. 커널 디버깅을 안 켠 부팅 (`KdPitchDebugger` 켜짐, `KdLocalDebugEnabled` 꺼짐) 이면 `NtSystemDebugControl` 앞에서 `0xc0000354` 로 끝 (디버깅을 켠 부팅이면 호스트가 연결 중이 아니어도 이 문턱은 지남 — 그 뒤 `KdPullRemoteFile` 이 성공하는지는 연결 상태에 달림 `[공백]`). **반환값을 `PspLocateSystemDll` 이 안 본다** (`+0x7e` 바로 다음 `+0x81` 로 이어짐).
  - 이름 "KdPullRemoteFile = 명령 0x26" 은 함수 이름과 분기 사슬 (`SUB 0x1e` 에서 7번 `SUB 1` → `CMP EBX,1` → 길이 `0x10`/`0` 검사 → `+0x1f5 MOV RCX,R14; call`) 로 읽은 것 `[정적]`. 번호 이름 표는 안 봄.
- **`+0x146 ObReferenceObjectByHandle`** (`+0x141` call): 섹션 핸들 → 섹션 객체 (`[RBP+0x7f]`). 이 객체 포인터는 칸 `+0x0` 이 가리키는 빠른 참조에 들어간다 (아래 `+0x1b3`). 객체 참조를 **돌려주지 않는다** (이 함수 안에 `ObDereference` 가 없음) → 섹션 객체는 영구히 살아 있음.
- **`+0x183 MmGetSectionInformation(섹션, 2, &x)`** (`+0x17e` call): 클래스 2 = "재배치량" (`MiRelocateAmount` 값, 컨트롤 영역 플래그 `0x20000000` 일 때만 `MiState+0x1000` 값으로 보정; `MmGetSectionInformation.c` 끝 부분). 결과 → **칸 `+0x38`** (`+0x1aa MOV [RBX+0x38],RAX`). 프로세스 생성 때 `PspPrepareSystemDllInitBlock +0xac/+0xd6` 이 읽어 init 블록 `+0x10` (`SystemDllNativeRelocation`) / `+0x8` (`SystemDllWowRelocation`) 에 넣는다 (이름은 run32).
- **`+0x1b3 ObInitializeFastReference`** (`+0x1ae` call): 섹션 객체에 참조 15개를 더 얹고 (`ObpIncrPointerCountEx(…, 0xf)`) 값 `객체 | 0xf` 를 `*칸[+0x0]` (= `PspNativeSystemDll` 등 16바이트 자리, `_EX_FAST_REF` 8 + 푸시 락 8) 에 씀. 이어 `+0x1b6 MOV [[칸]+8], 0` — 락 칸 0.
- **`+0x1d2 / +0x1ea / +0x1fa`** (칸 플래그 `0x21` 중 하나라도 켜졌을 때만 = 필수 칸 = 0번 ntdll): `MmGetSectionInformation(섹션, 4, 0x48바이트)` → `+0x40` 칸 (`SECTION_IMAGE_INFORMATION` 뒤 부가 비트) 비트 `0x200` 확인 (`+0x1d9 TEST [RBP+0x27],0x200`), `MiSectionControlArea(섹션)` → 컨트롤 영역 `+0x38` 비트 `0x20` 확인 (`+0x1ea~+0x1ed`, "Image" 로 짐작 `[추측]`), `MiDoesImageContainFunctionOverrideFixups(컨트롤 영역)` 가 참 (`MiGetControlAreaLoadConfig` 결과 `+0x18 ≠ 0`). 셋 중 하나라도 어긋나면 `+0x211/+0x216 KeBugCheckEx(0x6b, 상태(클래스4 호출), 9, 0, 0)`. **결과를 어디 쓰지는 않는다** — 확인만 한다 (전역·표 칸에 안 씀). 뜻: "필수 시스템 DLL 은 함수 덮어쓰기(DVRT 쪽) 정보가 있는 이미지여야 한다" `[추측]`.
- **핸들 닫기**: 파일 핸들 `ObCloseHandle(…, 0)` `+0x117/+0x11c` (섹션 만든 직후, `ObReference` 전), 섹션 핸들 `+0x169/+0x16e` (`ObReference` 성공 뒤). 둘 다 닫음. **섹션 객체 참조만 남는다** `[정적]`.
- **그 뒤 `+0x233 PspMapSystemDll`** (`+0x233` call, ret `+0x238`): 현재 프로세스 (`GS:[0x188]+0xb8`) 에 올림. 인자 `R8D=0`, `R9D=1`. 음수 → `+0x24f KeBugCheckEx(…, 5, …)`.

**`KeBugCheckEx` 조건 표** (모두 코드 `0x6b`, 넷째 인자 0. "실패하면 부팅이 멈춘다")

| 어디 | 사이트 (2번째) | 1번째 인자 | 조건 |
|---|---|---|---|
| `PsLocateSystemDlls +0xcf/+0xd4` | 2 (3번째 = 칸 번호) | `PspLocateSystemDll` 이 돌려준 음수 (실질 `ZwOpenFile` 실패값) | 칸 플래그 비트0 (필수) 켜진 칸만. 이 빌드에선 0번 ntdll 만 |
| `PspLocateSystemDll +0x10b/+0x110` | 3 | `MmCreateSpecialImageSection` 음수 | 모든 칸 (연 뒤에는 필수 아니어도 멈춤) |
| `+0x15d/+0x162` | 4 | `ObReferenceObjectByHandle` 음수 | 〃 |
| `+0x19a/+0x19f` | 8 | `MmGetSectionInformation` (클래스 2) 음수 | 〃 |
| `+0x211/+0x216` | 9 | 클래스 4 호출의 상태 (성공이면 0) | 칸 플래그 `0x21` 중 하나: 클래스 4 실패 / `+0x40` 비트 `0x200` 꺼짐 / 컨트롤 영역 `+0x38` 비트 `0x20` 꺼짐 / 함수 덮어쓰기 고침 없음 |
| `+0x24f/+0x254` | 5 | `PspMapSystemDll` 음수 | 모든 칸 |
| `PspInitializeSystemDlls +0xa0/+0xa5` | 6 | `PspLookupEntryPoint` 음수 (`0xc000007a` = 이름 없음) | 이름 표의 모든 이름 |

→ **`ZwOpenFile` 에 실패하는 순간 (파일 없음 등) 이후 단계로 못 가고, 필수 칸만 멈춘다**. 필수 아닌 칸 (WOW ntdll·vertdll) 은 파일이 없으면 조용히 빠진다 — 칸 `+0x20` 이 0 으로 남아 `PsQuerySystemDllInfo` 가 NULL 을 준다. 하지만 일단 파일을 연 뒤에는 어느 칸이든 섹션·참조·올리기 실패가 부팅을 멈춘다.

### 2.3 질문 3 — `PspMapSystemDll` 부팅 갈래

시그니처 (`.asm` 서 읽음. `PsMapSystemDlls.c` 는 인자 하나만 보이는데 `.asm` 은 넷): `(프로세스, 칸, 인자3, 인자4)` = (`RCX`, `RDX`, `R8D`, `R9D`).

| 부르는 자리 | 프로세스 | 인자3 | 인자4 |
|---|---|---|---|
| `PspLocateSystemDll +0x233` (ret `+0x238`) | 현재 스레드의 프로세스 (`GS:[0x188]` → `+0xb8`) | 0 | **1** |
| `PsLocateSystemDlls +0x84` (ret `+0x89`) | 〃 | 0 | **1** |
| `PsMapSystemDlls +0xbb` (ret `+0xc0`) — 프로세스마다 | 새 프로세스 | `PsMapSystemDlls` 의 둘째 인자 (`MiMapProcessExecutable +0xd7` 에서 0) | **0** |
| `PspInitPhase3 +0x6b` / `+0xa0` — `VslVsmEnabled` 일 때만 | `PsSecureSystemProcess` | 0 | **0** |

**인자4 가 다르게 하는 일** (`PspMapSystemDll.asm`):
1. 올릴 때 할당 종류에 비트 `0x40000` 을 더한다 (`+0xbb BTS ECX,0x12`, `+0xc2 CMOVZ`): 인자4≠0 일 때만 (SDK 의 `MEM_RESERVE_PLACEHOLDER` 와 같은 비트지만 MM 안 뜻은 안 따라감 `[공백]`). 인자3≠0 이면 `0x20000000` (이 줄을 타는 호출은 위 표엔 없음 — `MiMapProcessExecutable` 쪽 `0x10 << 0x19` 와 같은 비트).
2. 올린 뒤 (`+0x169 TEST R15D`): **인자4≠0 → `+0x1a5` 로 가서 칸을 채움 (돌아오는 값은 0)**, **인자4=0 → `+0x16e~+0x176` 에서 칸 `+0x20` 이 올라간 주소와 같은지 비교, 다르면 `0xc0000001` (`+0x178`)**.
3. `STATUS_IMAGE_NOT_AT_BASE` (`0x40000003`) 는 `R14 == PsInitialSystemProcess` 일 때만 성공 취급 (`+0x132~+0x19c`), 다른 프로세스면 `0xc0000018` (`CONFLICTING_ADDRESSES`). 인자4 와는 무관.

**올리는 호출 `+0x118 MmMapViewOfSectionEx`**: 섹션 = `PspReferenceSystemDll(칸[+0x0])` 으로 잡은 것 (`+0x3b`), 시작 주소 NULL·오프셋 0·크기 0 (섹션 통째), 페이지 보호 `2`, 확장 인자 2개 (칸 플래그 `0x20` 이면 3개): ① 주소 요구 `{하한 0, 상한 = (칸 플래그 & 8) ? 0 : 0x7ffffffeffff, 정렬 0}` (WOW 칸 `0x14c0008` 은 비트 8 켜짐 → 상한 0 = 기본), ② 종류 5 · 값 `0x20` (`MiMapProcessExecutable +0xa3~+0xab` 와 같은 값), ③ (플래그 `0x20` 칸만) 종류 6 `ImageMachine` · 값 = 칸 `+0xa` 의 머신 번호. 나머지 스택 인자 (`[rsp+0x48]=0`, `[rsp+0x50]=0`, `[rsp+0x60]=0x2000000`) 도 `MiMapProcessExecutable` 과 같다. 올린 뒤 `+0x12d ObFastDereferenceObject(칸[+0], 섹션, 'PsSd')` 로 잡은 참조를 돌려줌.

**기본 주소 `+0x20`·`+0x28`·`+0xc` 쓰는 줄 (부팅 갈래)**: `+0x1a5 MOV R14,[올라간 주소]` → `+0x1ae/+0x1b3 RtlImageNtHeader(R14)` → 머신 `Magic` 검사 (`+0x1b8 CMP word [RAX+0x18], 0x10b`):
- PE32 (`0x10b`, WOW ntdll): `+0x1c2 MOV EDX,[RAX+0x34]` (4바이트 `ImageBase`)
- PE32+ (그 밖): `PspMapSystemDll` 의 **떨어진 범위** `+0x207050` (= rva `0xb18c28`) `MOV RDX,[RAX+0x30]`
- 그다음 (떨어진 범위): `+0x207054` (rva `0xb18c2c`) **`MOV [RBX+0x20],RDX`** → `+0x207058 MOV EAX,[RAX+0x50]` (SizeOfImage) → `+0x20705b` (rva `0xb18c33`) **`MOV [RBX+0xc],EAX`** → `+0x20705e` (rva `0xb18c36`) **`MOV [RBX+0x28],R14`** → `+0x207062 JMP +0x13e` (돌려줄 값 0).
- (떨어진 범위는 WinDbg 가 `PspMapSystemDll+0x207…` 로 보일지 다른 이름일지 확인 전 `[추측]`, rva 는 확실.)

**값의 출처 (질문 3 의 "헤더 ImageBase 인가, 따로 고른 주소 인가")**
- 정적으로 읽히는 것: `+0x20` = **System 프로세스에 올린 뷰의 NT 헤더 `ImageBase` 칸 값**, `+0x28` = 그 뷰의 시작 주소. 파일 속 값 `0x180000000` (ntdll, PE 파일에서 확인) 을 그대로 읽는 줄이 **아니다** — 올린 뒤 메모리의 헤더를 읽는다.
- 새 기본 주소는 섹션 만들 때 MM 이 무작위로 고른다: `MiCreateNewSection +0x3da → MiRelocateImage → MiSelectImageBase` (`ExGenRandom`·시스템 이미지 비트맵 사용, `MiSelectImageBase.c` 의 callees). ntdll 은 `DYNAMIC_BASE`·`HIGH_ENTROPY_VA` (`DllCharacteristics 0x4160`, PE 파일) 라 가능. 이 선택은 부팅 때 **한 번**, 모든 프로세스가 같은 섹션을 올리니 같은 주소를 받는다.
- 둘을 잇는 근거: ① 프로세스마다 `+0x172` 에서 `+0x20` == 올라간 주소를 요구 → `+0x20` 은 "모든 프로세스가 받는 기본 주소" 여야 한다, ② `[관찰]` run44: 새 프로세스 첫 트랩 프레임 `Rdx` = ntdll 실제 베이스 `7ffafb660000` (파일 값 `0x180000000` 아님) 이고 `Rdx` 는 `PspSystemDlls[0]+0x20` 에서 온다 (run16 `[정적]`). 합치면 **`+0x20` = 무작위로 고른 실제 기본 주소** (`[정적]` 코드 + `[관찰]` 간접). 단 "뷰 헤더 `ImageBase` 칸이 새 주소로 바뀌는 줄" (`MiRelocateImage` 조각 `FUN_140b24658`? 등) 은 **안 따라갔다 `[공백]`** → D5.
- 부팅 때 System 프로세스에 실제로 올리나: **올리는 코드가 있다** (`MmMapViewOfSectionEx` 의 프로세스 인자 = 현재 스레드의 프로세스, 이 갈래에서 `MmUnmapViewOfSection` 은 없음). 현재 프로세스가 `PsInitialSystemProcess` 라는 건 코드가 가정하는 것 (`+0x195` 비교) — `Phase1Initialization` 스레드가 System 프로세스 소속이어야 한다 `[추측]` → D3 (부팅 후 System 프로세스에 ntdll 뷰가 있나).
- 칸 `+0x28` ≠ `+0x20` 일 수 있는 이유가 코드에 있다: `PspLookupEntryPoint` 는 `RtlFindExportedRoutineByName(칸+0x28 위치의 뷰, 이름)` 으로 찾고 결과에 `(+0x20) − (+0x28)` 을 더해 (`PspLookupEntryPoint +0x2f~+0x3a`) 사용자 주소로 바꾼다. 같다면 더하는 값은 0.

**`+0x28` 칸**: 쓰기 `+0x20705e` 뿐. 읽는 곳: `PspLookupEntryPoint` (via `PsQuerySystemDllInfo` 의 `info+0x20`), `PspLocateNtdllAddressesForScpCfg` (`RtlImageNtHeaderEx` 에 넣음, `.c` 로만 읽음), `PsQuerySystemDllInfo` 가 NULL 판정에 쓰는 건 `+0x20`.

**`PsNtdllExports` 를 채우는 줄**
- 답: **이 구간에 있다 — `PspInitializeSystemDlls +0x6b` (`MOV [RCX],RAX`)**. `PspInitPhase2 +0x172` (ret `+0x177`) → 부팅 `PsLocateSystemDlls` 가 끝난 **뒤** (IoReady `+0x1d5`).
- 이름 → 주소: `PspInitializeSystemDlls +0x56` (ret `+0x5b`) `PspLookupEntryPoint(칸정보, 이름 문자열, &결과)` → 안에서 `+0x1b` (ret `+0x20`) **`RtlFindExportedRoutineByName(뷰 시작, 이름)`** (export 표 읽기, ntdll 이름 → 뷰 안 주소), 못 찾으면 `0xc000007a`, 찾으면 `(+0x20) − (+0x28)` 더함.
- 이름 표 = 파일 안 데이터 `NtdllExportInformation` (rva `0xc7ff90`, 칸마다 16바이트 `{표 포인터, 개수}`): `[0]` ntdll **19개**, `[1]` WOW **8개**, `[2~5]` 0개 (vertdll 도 0 → 이 구간에서 vertdll 함수 주소는 안 찾음). 각 줄 `{이름, 쓸 자리 주소}`.
  - `[0]` → `PsNtdllExports` (`_PS_NTDLL_EXPORTS` 0xa8 바이트, PDB 타입 있음): `+0x0 LdrSystemDllInitBlock`, `+0x8 LdrInitializeThunk`, `+0x10 RtlUserThreadStart`, `+0x18 RtlUserFiberStart`, `+0x20 KiUserExceptionDispatcher`, `+0x28 KiUserApcDispatcher`, `+0x30 KiUserCallbackDispatcher`, `+0x40 KiRaiseUserExceptionDispatcher`, `+0x48/+0x50/+0x58 ExpInterlockedPopEntrySList{End,Fault,Resume}`, `+0x60 RtlpFreezeTimeBias`, `+0x68 KiUserInvertedFunctionTable`, `+0x70 WerReportExceptionWorker`, `+0x78 RtlCallEnclaveReturn`, `+0x80 RtlEnclaveCallDispatch`, `+0x88 RtlEnclaveCallDispatchReturn`, `+0x90 RtlRaiseExceptionForReturnAddressHijack`, `+0xa0 LdrHotPatchNotify` = 19개.
  - **표에 없는 PDB 칸: `+0x38 KiUserCallbackDispatcherReturn`, `+0x98 KiUserEmulationDispatcher`** → 이 구간은 안 채운다 `[정적]` (`_refs/PsNtdllExports.tsv` 의 쓰기 줄이 `+0x6b` 하나뿐). 다른 곳이 채우는지는 `_refs` 에 없음 → 끝까지 0 일 것 `[추측]` (D2).
  - `[1]` → `PsWowX86SharedInformation` (rva `0xfda900`): `+0x0 LdrInitializeThunk`, `+0x8 KiUserExceptionDispatcher`, `+0x10 KiUserApcDispatcher`, `+0x18 KiUserCallbackDispatcher`, `+0x20 RtlUserThreadStart`, `+0x28 RtlpQueryProcessDebugInformationRemote`, `+0x38 LdrSystemDllInitBlock`, `+0x40 RtlpFreezeTimeBias`. 그리고 `+0x30` 은 표가 아니라 `+0x77` 에서 WOW 칸의 `+0x20` 을 복사 (`+0x80 MOV [RAX+0x30],RCX`).

### 2.4 질문 4 — 쓰는 상태 (baggage 양식)

**단계 표시 의견**
- 기존 `⓪` 은 사진 지점 `P0` (`PspUserThreadStartup` 입구) 이름으로 이미 쓰여서 "⓪ 이전" 이라 부르면 사진 지점과 섞인다. 새 단계는 **`Ⓑ` (부팅)** 로 하고 `Ⓑ1` = `Phase1` 안 `PsLocateSystemDlls` 구간, `Ⓑ2` = `PspInitPhase2` (`PspInitializeSystemDlls`), `Ⓑ3` = `MmInitSystemDll`, `Ⓑ4` = `PspInitPhase3` (VBS 일 때만) 로 가른다. 파일에 박힌 처음 값은 `Ⓑ0` (링크 때).
- 사진 구간 말 `K1 이전` 은 그대로 두고 "그중 부팅 때 값이라는 걸 아는 칸" 은 `K1 이전 (Ⓑ1)` 처럼 덧붙인다. 3장 표 "단계" 열에 `Ⓑ1` 등을 쓰고, 2장 세는 표에 "부팅 (Ⓑ)" 열을 하나 더 두는 방식. 놓는 법은 기존 말 (`직접` / `붙이기` / `입력`) 그대로.

| 단계 | 무엇을 | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|
| Ⓑ0 (링크 때) | `PspSystemDlls` 표 처음 값: `[0]`=`PspNativeSystemDllData` (rva `0xfd8980`), `[1]`=`PspWowX86SystemDllData` (`0xfd8940`), `[4]`=`PspVsmEnclaveRuntimeDllData` (`0xfd89c0`), 나머지 0. 칸 처음 값: `+0x0` 섹션 빠른 참조 자리 포인터 (`[0]`→`PspNativeSystemDll` `0xfda548` / `[1]`→`PspWowX86SystemDll` `0xfda568` / `[4]`→`PspVsmEnclaveRuntimeDll` `0xfda558`, 16바이트씩 0 으로 시작), `+0x8` 플래그 WORD (`[0]` `0x0013` / `[1]` `0x0008` / `[4]` `0x0012`) + `+0xa` 머신 WORD (`0x8664` / `0x014c` / `0x8664`), `+0x10` 경로 `UNICODE_STRING` (`\SystemRoot\System32\ntdll.dll` 등), `+0x30` 짧은 이름 포인터 (`ntdll.dll` / `ntdll32.dll` / `vertdll.dll`, `[0]`/`[1]`/`[4]` 순서), 나머지 (`+0xc`·`+0x20`·`+0x28`·`+0x38`) 0 | 입력 (PE 파일 값) | — | (링크) | `[정적]` PE 바이트 (rva `0xfd8130`·`0xfd8940`~`0xfd89c0`) · `[정적] PDB` 이름 · 플래그 뜻 `[추측]` (아래) |
| Ⓑ1 | 섹션 객체 (이미지 섹션, 칸마다) → 칸 `+0x0` 이 가리키는 `_EX_FAST_REF` 자리 (`객체\|0xf`, 참조 +15) + 락 칸 0 | 직접 (객체는 `MmCreateSpecialImageSection` 이 만듦) | nt | `Ⓑ… IoInitSystemPreDrivers +0x10f5 → PsLocateSystemDlls +0x9e → PspLocateSystemDll +0xef · +0x1ae · +0x1b6` | `[정적]` sast42 · 정확 |
| Ⓑ1 | 칸 `+0x38` = 재배치량 (`MmGetSectionInformation` 클래스 2). 별칭 칸은 0번 칸 값 복사 | 직접 | nt | `Ⓑ… → PspLocateSystemDll +0x17e · +0x1aa` (별칭: `PsLocateSystemDlls +0x6c · +0x70`) | `[정적]` sast42 · run32 이름 · 정확 |
| Ⓑ1 | 칸 `+0x20` = 올린 뷰의 NT 헤더 `ImageBase`, `+0xc` = `SizeOfImage`, `+0x28` = 뷰 시작 주소 (System 프로세스) | 직접 (값은 뷰 헤더 = 입력에서 읽음) | nt | `Ⓑ… → PspLocateSystemDll +0x233 → PspMapSystemDll +0x1a5 → … +0x207054 (rva 0xb18c2c) · +0x20705b · +0x20705e` | `[정적]` sast42 · 값 의미 `[관찰]` run44 (`Rdx`) 간접 |
| Ⓑ1 | System 프로세스 주소 공간에 ntdll 뷰 (+ SysWOW64 ntdll, vertdll 뷰) | 붙이기 | nt | `Ⓑ… → PspMapSystemDll +0x118 MmMapViewOfSectionEx` | `[정적]` sast42 · 올라가는지 D3 |
| Ⓑ1 | `NtGlobalFlag` 비트 `0x40000` 임시 켜기·복원 | 직접 | nt | `Ⓑ… IoInitSystemPreDrivers +0x10ef · +0x10fa` | `[정적]` sast42 · 정확 |
| Ⓑ1 | (디버거 붙은 부팅만) `ExpSysDbgPulledFileTable` (풀 `0x4010` 바이트 시작, 해시 8바이트 줄) | 직접 | nt | `Ⓑ… → PspLocateSystemDll +0x79 ZwSystemDebugControl → NtSystemDebugControl 조각 +0x1f8 → ExpKdPullRemoteFileForUser` | `[정적]` sast42 (`.c` 읽음) · 값 D6 |
| Ⓑ2 | `PsNtdllExports` 19칸 (ntdll 안 함수 주소 = 뷰 안 주소 + `(+0x20 − +0x28)`) | 직접 | nt | `Ⓑ… PspInitPhase2 +0x172 → PspInitializeSystemDlls +0x56 → PspLookupEntryPoint +0x1b → RtlFindExportedRoutineByName` → 쓰기 `+0x6b` | `[정적]` sast42 · 정확 (칸 `+0x38`·`+0x98` 은 안 채움) |
| Ⓑ2 | `PsWowX86SharedInformation` 8칸 + `+0x30` (WOW ntdll 기본 주소) | 직접 | nt | `Ⓑ… PspInitializeSystemDlls +0x56 · +0x6b`, `+0x77 PspWow64GetSharedInformation` → `+0x80` | `[정적]` sast42 · 정확 |
| Ⓑ3 | `MiState.Processes.SystemDllBase` (rva `0xe2db80` = `MiState+0x1000`, PDB 타입 `_MI_PROCESS_STATE +0x0`) ← 0번 칸 `+0x20` | 직접 | nt | `Ⓑ… Phase1InitializationIoReady +0x1e2 → MmInitSystemDll +0x22` | `[정적]` sast42 · `[정적] PDB` 이름 · 읽는 곳: `MmGetSectionInformation` (클래스 2·1/4 의 컨트롤 영역 플래그 `0x20000000` 갈래) |
| Ⓑ4 (VBS 일 때) | `PsSecureSystemProcess` 안에 ntdll·vertdll 뷰, 두 섹션 참조를 `VslpEnterIumSecureMode(2, 4, …)` 로 넘김 | 붙이기 + 주소 넘김 | nt | `Ⓑ… PspInitPhase3 +0x6b · +0xa0 · +0xea` | `[정적]` sast42 · `VslVsmEnabled` 값 D8 |

**칸 플래그 `+0x8` 비트 (이 구간에서 읽히는 것만, `[추측]` 이름 없이 동작으로)**: `0x1` 필수 (실패하면 `KeBugCheckEx`, 클래스 4 확인 대상), `0x8` 올릴 주소 상한 풀기 (WOW 칸), `0x10` 섹션 만들 때 VSL 비트4 를 전달, `0x20` 별칭 칸 (찾지 않고 0번 섹션 사용, `ImageMachine` 인자 추가), `0x40` `PsMapSystemDlls` 에서 보안 프로세스(`+0x170` 비트0) 에게도 올릴 칸, `0x2` 는 이 구간 코드가 안 읽음 (다른 곳은 안 봄). 칸 값: `0x...0013`, `0x...0008`, `0x...0012`.

**그 밖에 이 구간이 처음 쓰는 커널 전역** (읽기만 포함): 읽음 `MmSectionObjectType` (`PspLocateSystemDll +0x11c`), `PsInitialSystemProcess` (`PspMapSystemDll +0x195`), `PsEmbeddedNTMask`·`NtGlobalFlag`, `PsSecureSystemProcess`·`VslVsmEnabled` (VBS). 쓰기는 위 표의 것뿐 (`_refs/PspSystemDlls.tsv` 쓰기 1줄 + 위의 `PsNtdllExports`·`PsWowX86SharedInformation`·`MiState`).

### 2.5 프로세스 생성 때 이 짐을 **읽는** 자리 (쓰는 쪽과 짝)

| 쓰는 쪽 (부팅) | 읽는 자리 (프로세스 생성 때, 이미 아는 것 + 이번에 refs 로 찾은 것) |
|---|---|
| 섹션 객체 (칸 `+0x0` 빠른 참조) `Ⓑ1` | `MiMapProcessExecutable +0xd7 (ret +0xdc) → PsMapSystemDlls +0xbb (ret +0xc0) → PspMapSystemDll +0x3b (ret +0x40) PspReferenceSystemDll` → `+0x118 MmMapViewOfSectionEx` (인자4=0). 그 밖: `DbgkCreateThread`·`MmInitSystemDll`·`PspInitPhase3` (`PspReferenceSystemDll` 의 caller 4곳: `PspInitPhase3`·`MmInitSystemDll`·`PspMapSystemDll`·`DbgkCreateThread`). `PsMapSystemDlls` 는 새 프로세스가 `MiMapProcessExecutable` 플래그 `0x10` 이 꺼졌을 때만 불린다 (`+0xce~+0xd0`). 어떤 칸을 올리나: 0번은 늘, WOW 프로세스 (`EPROCESS +0x310` `WoW64Process` ≠ 0) 면 `PsWow64GetProcessNtdllType(process)` 번호와 같은 칸도, 보안 프로세스 (`+0x170` 비트0) 면 칸 플래그 `0x40` 켜진 칸 (`PsMapSystemDlls +0x7c~+0xa4`, `+0x12a`). |
| 칸 `+0x20` (기본 주소) `Ⓑ1` | `PspMapSystemDll +0x172` (비교, 다르면 `0xc0000001`), `PspInitializeThunkContext +0x196 (칸 표) · +0x19d` (새 스레드 컨텍스트 `Rdx`, run16·run18), `PspGetScpCfgFunctions +0x1b · +0x36`, `PsShutdownSystem +0x1ea · +0x1f2 · +0x20d` (용도 안 봄), `PsQuerySystemDllInfo +0x13` (NULL 판정), `MiRelocateImage +0x756 · +0x760` (다른 이미지 재배치 때 `RtlInitializeSystemOverrideDelta` 인자: `+0xc`·`+0x20`) |
| 칸 `+0xc` (`SizeOfImage`) `Ⓑ1` | `MiRelocateImage +0x75d` |
| 칸 `+0x38` (재배치량) `Ⓑ1` | `PspPrepareSystemDllInitBlock +0xa5/+0xac` (→ init 블록 `+0x10`), `+0xcb~+0xd6` (WOW 종류 칸 → init 블록 `+0x8`) |
| 칸 `+0x8` 플래그 `Ⓑ0` | `PsMapSystemDlls +0x12a`, `PspMapSystemDll +0x68` |
| `PsNtdllExports` `Ⓑ2` | `PspInitializeThunkContext +0x1a1 · +0x37f` (`+0x8` `LdrInitializeThunk` → 새 스레드 `Rip`), `PspPrepareSystemDllInitBlock +0x214` (`+0x0` `LdrSystemDllInitBlock`), `NtCreateUserProcess +0xd10`·`NtCreateThreadEx +0x1bf`·`PspWow64InitThread +0x71`·`KiVerifyContextIpForUserCet +0x9d` (`+0x10` `RtlUserThreadStart`), `KiDispatchException +0x58c` (`+0x20`), `KiInitializeUserApc +0x28a` (`+0x28`), `KiCallUserMode +0x1c5·+0x430·+0x467` (`+0x30`), `KeRaiseUserException +0x72` (`+0x40`), `KiDeliverApc`·`KiDispatchInterrupt`·`KiCheckForSListAddress`·`KiRollbackSlistPops` (`+0x48`, `+0x58`) 등 — 전체는 `_refs/PsNtdllExports.tsv` (읽기 약 40줄) |
| `PsWowX86SharedInformation` `Ⓑ2` | WOW 프로세스 생성·스레드 쪽 (이번엔 안 봄). `PspPrepareSystemDllInitBlock +0xc3~+0xef` 가 `PspWow64GetSharedInformation` 이 돌려준 칸의 16개 qword 를 init 블록 `+0x18~` 에 복사하는 것으로 읽힘 `[추측]` (`.asm` 만, run32 와 대조 안 함) |
| `MiState.Processes.SystemDllBase` `Ⓑ3` | `MmGetSectionInformation` (이번 `.c` 확인), 그 밖 안 봄 |
| 이 칸 둘러싼 질문 함수 | `PsQuerySystemDllInfo` 호출자: `PspLocateNtdllAddressesForScpCfg`·`PspWow64PickBestNtdll`·`PspInitializeSystemDlls`·`DbgkCreateThread`·`DbgkSendSystemDllMessages`·`ExpSystemErrorHandler2`·`PsWow64IsMachineSupported`·`MmInitSystemDll` (`PsQuerySystemDllInfo.c` 머리) |

사용자 `[추측]` ("ntdll 섹션·기본 주소·`PsNtdllExports` 는 부팅 때 준비되고 프로세스마다는 붙이기만") 판정: **맞다** `[정적]`. 세 가지 모두 부팅 `Ⓑ` 에서 한 번 쓰고 프로세스마다는 읽기·비교·올리기뿐이다. 다만 프로세스마다 읽은 값으로 **새로 만드는 것**이 있다 (init 블록, 새 스레드 컨텍스트) — 그건 run16·run32 가 이미 다룬 프로세스 생성 짐.

---

## 3. 동적분석으로 확인해야 할 것 (번호)

두 갈래로 가른다.

**A. 부팅이 끝난 뒤 일반 KDNET 세션에서 값만 보면 되는 것** (`dq`·`dt`·`lm`)

- **D1** `dq nt!PspSystemDlls L6` 로 표 확인: `[0][1][4]` 만 비어있지 않고 `[2][3][5]` = 0 인가. 각 칸 `dq <칸> L8`: `+0x8` = `0x86640013` / `0x014c0008` / `0x86640012`, `+0xc` = `0x267000` (x64 ntdll) / `0x1bf000` (SysWOW64) / `0x36000` (vertdll) 기대 (PE 파일 `SizeOfImage`), `+0x20`·`+0x28`·`+0x38` 값. 특히 **`+0x20` vs `+0x28` 이 같은가, `+0x20` vs 사용자 프로세스의 `lm m ntdll` 이 같은가** (Q42 항목). `+0x20 − (파일 ImageBase 0x180000000)` 이 `+0x38` 과 같은가 (재배치량 뜻 확인).
- **D2** `dq nt!PsNtdllExports L15` (0xa8 바이트 = 21 qword): 사용자 프로세스 컨텍스트에서 `x ntdll!LdrInitializeThunk` 등과 같은가, `+0x38`·`+0x98` 칸이 0 인가. `dq nt!PsWowX86SharedInformation L9` (+0x30 = WOW ntdll 기본 주소).
- **D3** System 프로세스 (PID 4) 안에 ntdll 뷰가 있나 (`!process 4 0` → `.process /p /r <EPROCESS>` → `lm m ntdll` 또는 `!vad`): 있으면 시작 주소가 `+0x28` 와 같은가. SysWOW64 ntdll·vertdll 뷰도 있나. (없으면 `PspLookupEntryPoint` 가 읽을 곳이 없으니 부팅 `Phase1` 스레드의 프로세스 가정이 틀린 것)
- **D4** `dq nt!MiState+0x1000 L1` = `poi(nt!PspSystemDlls)+0x20` (`MiState.Processes.SystemDllBase`).
- **D5** 뷰 헤더 `ImageBase` 가 새 주소로 바뀌어 있나: D3 컨텍스트에서 `+0x28` 위치 `!dh` 또는 `dt nt!_IMAGE_NT_HEADERS64 <뷰+e_lfanew> OptionalHeader.ImageBase` 가 `+0x20` 과 같은가, 파일 값 `0x180000000` 인가. (정적으로 못 닫은 "MM 이 헤더 칸을 고치는 줄" 의 확인)
- **D6** 디버거가 붙은 부팅의 흔적: `dd nt!NtGlobalFlag` (비트 `0x40000` 꺼짐 = 복원 확인), `dq nt!ExpSysDbgPulledFileTable L1` (≠ 0 이면 `ExpKdPullRemoteFileForUser` 가 표를 만든 것 = 명령 `0x26` 이 그곳까지 감) 과 `dd poi(nt!ExpSysDbgPulledFileTable) L2` (개수, 용량 `0x800`). 표가 0 이면 해시 표에 안 올렸거나 (`KdPullRemoteFile` 성공 & `ExpSysDbgPullOnce` 0) 안 간 것 — 값만으로 둘을 못 가름 `[공백]`.
- **D7** `PspLocateSystemDll` 이 몇 번 실제로 돌았나: D1 에서 `[1]`·`[4]` 의 `+0x20`·`+0x28` 이 채워져 있으면 그 칸이 찾아 올려진 것 (칸 `+0x20 = 0` 이면 그 칸은 못 찾았거나 안 돌았다).
- **D8** `dd nt!VslVsmEnabled L1`: 0 이면 `Ⓑ4` (`PspInitPhase3`) 는 안 탄다.

**B. 부팅 디버깅이 필요해서 일반 세션으로는 못 서는 것** (`PsLocateSystemDlls` 는 `Phase1` 때라 로그인 후 `bp` 는 이미 지나간 뒤. VM 을 다시 켜며 WinDbg 가 첫 접속에서 멈추게 한 뒤 `bp` — 방법은 dast-mentor 몫)

- **D9** 실제 호출 횟수·순서: `bp nt!PspLocateSystemDll` (rcx = 칸 주소 `PspNativeSystemDllData`·`PspWowX86SystemDllData`·`PspVsmEnclaveRuntimeDllData` 순서인가, `edx` = 0/4) 와 반환 `eax`. `bp nt!PspMapSystemDll` 의 `r9d` (1) · `rcx` = `PsInitialSystemProcess` 와 같은가.
- **D10** `MmMapViewOfSectionEx` 의 반환 상태: `PspMapSystemDll +0x11d` 에서 `eax` 가 `0` 인가 `0x40000003` 인가 (부팅 갈래의 `STATUS_IMAGE_NOT_AT_BASE` 허용이 실제로 타나) — 반환값은 부팅 때만 보임.
- **D11** `ZwSystemDebugControl` 이 돌려주는 값: `PspLocateSystemDll +0x7e` (ret) 에서 `eax` (0 또는 음수), 그리고 `IoInitSystemPreDrivers +0x10ef` 를 지난 직후 `dd nt!NtGlobalFlag` 에 비트 `0x40000` 이 켜져 있나 (정적으로 읽은 임시 비트 확인).
- **D12** `MmCreateSpecialImageSection` (`PspLocateSystemDll +0xef`) 인자 확인: `rcx`·`r8d`(0)·`r9b`(0xc)·`[rsp+0x28]` (`flags` = 2 또는 6) 와 `MiInitializeCreateSectionPacket +0x346` 이후 패킷 `+0x18` = `0xc`. (정적으로 읽은 인자의 실제 값 확인 — 우선순위 낮음)

추천 멈춤점 (일반 세션에서 가능한 것): `nt!PspMapSystemDll+0x172` — 프로세스 생성 때 (인자4=0 갈래) `cmp [rbx+0x20], rax` 줄. 여기서 `dq @rbx L8` (칸 전체, `+0x20` 포함) 과 `rax` (방금 올라간 주소) 를 같이 보면 "칸 `+0x20` == 올라간 주소" 를 test.exe 시작 한 번에 직접 확인한다 (D1 의 `dq` 와 이어짐). 커널 멈춤점은 모든 프로세스에 걸리니 `test.exe` 조건을 붙인다.

---

## 4. 정적으로 못 닫은 것 (`[공백]`)

1. 뷰 헤더 `ImageBase` 칸이 새 주소로 바뀌는 MM 줄 (`MiRelocateImage` 조각·`MiCreateNewSection`) — D5.
2. `\SystemRoot` 링크를 만드는 곳과 그 시점 (`PsLocateSystemDlls` 앞).
3. `SEC_PROTECTED_IMAGE`+`SEC_NO_CHANGE` 비슷한 속성 (패킷 플래그 `0x10`) 이 서명 검사에 주는 영향의 본체 (`SeGetImageRequiredSigningLevel`, 본체 안 따라감). `.c` 로만 읽은 부분임.
4. `MmMapViewOfSectionEx` 안에서 할당 종류 `0x40000` (인자4≠0)·`0x20000000` (인자3≠0) 비트의 MM 내부 뜻.
5. `MiGetControlAreaLoadConfig` 결과 구조의 칸 이름 (`+0x18`, 첫 DWORD 비트) — 사이트 9 조건의 정확한 이름.
6. `PsInitSystem(1)` 의 인자가 정말 1 인지 (`Phase1InitializationDiscard +0xc1b MOV ECX,EBX`, `EBX=1` 은 `+0xa8f` 에서 설정된 것으로 읽음) — 이름만 쓴 부분.
7. `PspInitPhase3` VBS 갈래의 본문과 `VslpEnterIumSecureMode` 가 섹션 둘로 하는 일 (범위 밖).
8. 표 `[2][3][5]` 를 런타임에 쓰는 코드 (레지스터+오프셋 쓰기는 `_refs` 가 못 잡음) — D1.
9. `PsWowX86SharedInformation` 을 프로세스 생성 때 읽는 자리 (이번엔 안 봄).

## 5. `.c` 읽을 때 주의 (이번에 겪은 것)

- `PsMapSystemDlls.c` 는 `PspMapSystemDll(param_1)` 인자 하나만 그린다 — `.asm` 은 `(RCX, RDX, R8D, R9D)` 넷 (`+0xb2~+0xbb`). 또 `PspInitPhase3.c` 에서 `PspSystemDlls` 로 보이는 인자는 `[0x140fd8130]` 의 **값** (0번 칸 주소). 인자 위치는 `.asm` 으로 셌다.
- `MmCreateSpecialImageSection.c` 의 `CONCAT44(…, param_2) | 2` 는 `.asm` 에서 `OR ESI,2` 후 `[RSP+0x28]` 에 쓴 것과 맞다.
- `MiInitializeCreateSectionPacket.c` 는 `0x02000000` 갈래를 `FUN_140b23a4f` 호출로 그려 흐름이 틀어졌다. `.asm` `+0x346~+0x35a` 로 다시 읽었다.

## 6. 스크립트 문제 / 하네스 제안

- 스크립트 오류는 없었다 (모든 `run_ghidra.sh export/refs/map` 정상).
- 제안 (필수 아님): **PE 파일의 초기화된 데이터를 읽는 도구** (`pe_data.py <바이너리> <이름|0xRVA> <바이트수>`: PDB 공개 이름으로 포인터 값을 이름에 맞춰 보여 주는 것). 이번에 `PspSystemDlls` 표·`NtdllExportInformation` 이름 표를 읽으려고 임시 파이썬을 짰다 (스크래치패드). 짐 지도 "입력 (파일 값)" 칸을 닫을 때마다 필요하다.
- 제안: `pdb_struct.py --list` 가 "PDB 에 이름만 있고 칸 정의가 없는 타입" (`_PS_SYSTEM_DLL_INFO`) 을 따로 표시하면 좋겠다 ("없음" 만 나와서 처음엔 이름이 틀린 줄 알았다).

## 판정

**분석성공** — 질문 1~4 모두 `.asm` 줄로 답했다 (부르는 자리·인자·조건·`KeBugCheckEx`·쓰는 칸·`PsNtdllExports` 채우는 줄·읽는 자리 짝). 버전 확인 일치. 못 닫은 것 (`[공백]` 9건) 은 대부분 MM 내부 뜻이거나 부팅 후 값 확인 (D1~D8) 으로 닫힌다. **부팅 디버깅이 필요한 것은 D9~D12** (우선순위 낮음).

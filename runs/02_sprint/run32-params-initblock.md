# 2026-10-23 run32 — 프로세스 매개변수·시작 정보 블록: **PEB `+0x20` 은 `PspCopyAndFixupParameters` 가 쓴다**, `PspPrepareSystemDllInitBlock` 은 **새 프로세스 안 ntdll `LdrSystemDllInitBlock`** 을 채운다 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntoskrnl (`ntoskrnl.exe`), pdb `ntkrnlmp.pdb` GUID+age `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음).
  - analyze 는 다시 안 걸었다 (이미 돼 있음). 지도는 기존 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*` 를 그대로 썼다.
- 버전 대조 (결과서): `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` ret `+0x807` → `PspCallProcessNotifyRoutines` 가 run9·run12 와 같다. 두 대상 함수의 rva·크기가 `_index.tsv` 와 같다.
- 구조체 칸 이름은 PDB 타입 덤프 (`llvm-pdbutil`) 로 맞췄다 (`_RTL_USER_PROCESS_PARAMETERS`, `_PEB`, `_EPROCESS`, `_PS_NTDLL_EXPORTS`, `_ESERVERSILO_GLOBALS`). **`LdrSystemDllInitBlock` 의 타입(`_PS_SYSTEM_DLL_INIT_BLOCK` 류)은 공개 PDB 에 없다.**

| 함수 | rva | 크기 | 이번에 본 것 |
|---|---|---|---|
| `nt!PspCopyAndFixupParameters` | `0xae57e4` | 1340바이트 | 전체 |
| `nt!PspPrepareSystemDllInitBlock` | `0x9395d0` | 812바이트 | 전체 |
| `nt!PspSetupUserProcessAddressSpace` | (run10) | — | `+0x0 ~ +0x26f` 호출 순서·인자 싣는 줄 (이미 있던 파일) |
| `nt!PspCaptureProcessParameters` | — | 104바이트 | 전체 (원본이 어디서 오나) |
| `nt!PspCaptureUserProcessParameters` | — | 2071바이트 | **머리와 호출 이름만** |
| `nt!PspCaptureCreateInfo` | — | 189바이트 | 전체 (원본이 어디서 오나) |
| `nt!Feature_CreateProcessSegmentHeapEnablement__private_IsEnabledDeviceUsageNoInline` | — | 49바이트 | 전체 (갈림길 전역) |
| `nt!PspGetScpCfgFunctions` · `PsWow64GetProcessNtdllType` · `PspWow64GetSharedInformation` | — | 161 · 17 · 16바이트 | 전체 (블록 칸 출처) |
| `nt!PspAllocateProcess` | (run10) | — | `+0x2c8 ~ +0x31f`, `+0x1f3f ~ +0x1f72`, `+0x20d2`, `+0x236c ~ +0x2397`, `+0x2574 ~ +0x25fc` 만 (갈림길) |

## 목적

- Q36: flow.md 2.1 `PspSetupUserProcessAddressSpace` 절의 `[추측]` 두 줄 판정.
  - PEB `+0x20` (`ProcessParameters`) 를 누가 쓰나 (`MmCreatePeb` 는 안 씀, run10).
  - `PspPrepareSystemDllInitBlock` 이 ntdll 로 넘기는 시작 정보를 만드나.
- 커널이 쓴 칸과 ntdll 이 읽는 줄 (run17·run18·run19·run20·run28 에 이미 적힌 것) 을 짝짓기.

## 방법

- 정적: `_workspace/sast/27_params-initblock.md` — ntoskrnl, 두 대상 depth 1 요청. 결과서는 callee 본문을 안 읽는 범위라 **depth 0 으로 9개**를 이름으로 골라 뽑았다 (위 표).
  - 전역 참조 표: `ms/disassemble/ntoskrnl/_refs/{PspNtdllScpFunctions,PspNativeSystemDllData,PsWowX86SharedInformation}.tsv`.
- 결과서: `_workspace/sast/27_params-initblock_result.md` (**분석성공**)
- 이번에 `.asm` 과 다시 맞춰 본 줄 (flow-writer):
  - `PspSetupUserProcessAddressSpace` `+0x8d/+0x92`, `+0xa0 ~ +0xb5`, `+0xb8 ~ +0xc7`, `+0xd0/+0xd5`, `+0xd8/+0xdd`, `+0xe6`·`+0xea`, `+0xf5/+0xfa`, `+0x17f`, `+0x197/+0x19c`, `+0x1b4/+0x1b9`, `+0x1c3`·`+0x1da`, `+0x1e1/+0x1e6`, `+0x1f2/+0x1f7`, `+0x20a/+0x20f`, `+0x214/+0x219`, `+0x222/+0x227`, `+0x22f/+0x234`, `+0x250/+0x255`, `+0x25e/+0x263`, `+0x26a/+0x26f`
  - `PspCopyAndFixupParameters` `+0x2a`, `+0x36 ~ +0x40`, `+0x82`, `+0xb7/+0xbc`, `+0x12b/+0x130`, `+0x19d/+0x1a2`·`+0x1a4`, `+0x324/+0x329`, `+0x347/+0x34c`, `+0x41f/+0x424`, `+0x514`·`+0x52b`
  - `PspPrepareSystemDllInitBlock` `+0x35 ~ +0x50`, `+0x64 ~ +0x92`, `+0x98 ~ +0x9f`, `+0xa5 ~ +0xb0`, `+0xb7/+0xbc`, `+0xc3/+0xc8`, `+0xda`, `+0xef`, `+0xfb/+0x100`, `+0x106`, `+0x120`, `+0x12b`, `+0x138`, `+0x145`, `+0x152`, `+0x15a`, `+0x169`, `+0x196/+0x19b`, `+0x1ee`, `+0x214`, `+0x2dd/+0x2e2`
  - `NtCreateUserProcess +0xac6/+0xacb`, `PspCaptureProcessParameters +0x3a/+0x3f`·`+0x58 ~ +0x5a`, `PspCaptureUserProcessParameters +0x8f`·`+0xa8`, `PspAllocateProcess +0x1eb3/+0x1eb8`·`+0x1f3f`·`+0x1f50`·`+0x20d2`·`+0x236c`·`+0x2397/+0x239c`·`+0x25d7/+0x25dc`·`+0x25f7/+0x25fc`
  - 어긋난 곳 두 군데 (결과서 글자만, 뜻은 같음):
    - 결과서 2-b 표 `+0x8` 줄의 "`+0xc3` call (ret `+0xc8`)" 은 `PspWow64GetSharedInformation` 이다. 종류를 정하는 호출은 그 앞 `+0xb7` (ret `+0xbc`) `PsWow64GetProcessNtdllType`. 이 문서는 둘을 나눠 적었다.
    - 결과서가 `PspGetScpCfgFunctions` 자리로 적은 `+0x18e` 는 조건 검사(`TEST R12D,R12D`) 줄이고, 호출은 call `+0x196` / ret `+0x19b` 다.

## 결과

비유 (관리사무소 = 커널, 관리실 = ntdll, PEB = 가게 안내판):

- 부모가 써 준 **개업 서류**(프로세스 매개변수: 명령줄·exe 경로·환경 변수·표준 입출력 등)가 있다.
- 관리사무소는 그 서류를 먼저 **사무소 서랍에 사본으로** 받아 둔다 (`PspCaptureProcessParameters`).
- 가게 건물이 다 지어지면, 가게 안 빈 방 하나를 잡아(`ZwAllocateVirtualMemory`) **사본을 그 방에 옮겨 놓고**, 서류 안 "몇 쪽 참고" 쪽지들(포인터)을 새 방 주소 기준으로 고쳐 쓴다 (`PspCopyAndFixupParameters`).
- 그리고 **안내판(PEB) `+0x20` 칸에 "개업 서류는 이 방에"** 라고 적는다.
- 이어서 관리실(ntdll) 책상 위 **업무 수첩**(`LdrSystemDllInitBlock`)에 관리사무소가 직접 칸을 채워 둔다: 난수 하나, 안전 규칙(완화 옵션), CFG 출입 명단(비트맵) 주소 같은 것 (`PspPrepareSystemDllInitBlock`).
- 관리실은 나중에 문 열고 들어와서(2.4) 안내판과 수첩을 읽기만 한다.

### 정적 — 호출 지점 (`.asm` CALL 줄, 라벨은 ret)

| caller | call | ret (= 콜스택 오프셋) | callee | 이 고리를 타는 조건 | 표시 |
|---|---|---|---|---|---|
| `nt!PspAllocateProcess` | `+0x2397` | `+0x239c` | `nt!PspSetupUserProcessAddressSpace` | 묶음 플래그 비트 7 켜짐 · 만들기 정보 구조 ≠ NULL · 묶음 플래그 비트 1 꺼짐 (`+0x236c ~ +0x2397`, 아래) | [정적] (run10 자리) |
| `nt!PspSetupUserProcessAddressSpace` | `+0x197` | `+0x19c` | `nt!ZwAllocateVirtualMemory` | 늘 (붙은 뒤). 매개변수 블록 자리 | [정적] |
| `nt!PspSetupUserProcessAddressSpace` | `+0x1b4` | **`+0x1b9`** | `nt!PspCopyAndFixupParameters` | 위 성공 뒤 | [정적] |
| `nt!PspSetupUserProcessAddressSpace` | `+0x1e1` | **`+0x1e6`** | `nt!PspPrepareSystemDllInitBlock` (`ECX=0`, 64비트 ntdll) | 새 프로세스가 보안 프로세스가 **아닐 때** (`+0x1c3 ~ +0x1da`) | [정적] |
| `nt!PspSetupUserProcessAddressSpace` | `+0x214` | `+0x219` | `nt!PspPrepareSystemDllInitBlock` (`ECX` = 종류, 32비트 ntdll) | `EPROCESS.WoW64Process ≠ 0` (32비트 exe 만) | [정적] |
| `nt!PspAllocateProcess` | `+0x25d7` | `+0x25dc` | `nt!PspPrepareSystemDllInitBlock` (`ECX=0`, `RDX=0`) | `PspSetupUserProcessAddressSpace` 를 **안 부른** 갈래 | [정적] |
| `nt!PspAllocateProcess` | `+0x25f7` | `+0x25fc` | `nt!PspPrepareSystemDllInitBlock` (`RDX=0`) | 위 갈래 + WoW64 | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0xb7` | `+0xbc` | `nt!ObDuplicateObject` | 콘솔 서브시스템이고 `ConsoleHandle > 0` | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0x12b` | `+0x130` | `nt!ObDuplicateObject` | 표준 핸들 목록이 있을 때, 세 번 도는 고리 | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0x19d` | `+0x1a2` | `nt!Feature_CreateProcessSegmentHeapEnablement__private_IsEnabledDeviceUsageNoInline` | 늘. **두 갈래를 가르는 기능 스위치** | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0x324` | `+0x329` | `nt!RtlCopyToUser` | 스위치 켜짐 갈래 | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0x347` | **`+0x34c`** | `nt!RtlWriteULong64ToUser` (PEB `+0x20` 쓰기) | 스위치 켜짐 갈래 | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0x41f` | `+0x424` | 메모리 복사 함수 (같은 주소 이름 4개, 후보: `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) | 스위치 꺼짐 갈래 | [정적] |
| `nt!PspPrepareSystemDllInitBlock` | `+0xb7` | `+0xbc` | `nt!PsWow64GetProcessNtdllType` | 64비트 ntdll 블록일 때 | [정적] |
| `nt!PspPrepareSystemDllInitBlock` | `+0xc3` | `+0xc8` | `nt!PspWow64GetSharedInformation` | 위와 같음 | [정적] |
| `nt!PspPrepareSystemDllInitBlock` | `+0xfb` | `+0x100` | `nt!ExGenRandom` | 위와 같음 | [정적] |
| `nt!PspPrepareSystemDllInitBlock` | `+0x196` | `+0x19b` | `nt!PspGetScpCfgFunctions` | `ECX == 0` (64비트 ntdll 블록) 일 때만 | [정적] |
| `nt!PspPrepareSystemDllInitBlock` | `+0x2dd` | `+0x2e2` | `nt!PspWow64GetSharedInformation` | `ECX == 1` (32비트 ntdll 블록) | [정적] |
| `nt!NtCreateUserProcess` | `+0xac6` | `+0xacb` | `nt!PspCaptureProcessParameters` | 매개변수 원본을 커널로 가져옴 | [정적] |

- 요청서와 다른 점 (결과서): `PspCopyAndFixupParameters` 의 호출은 9곳이 아니라 **8곳**, `PspPrepareSystemDllInitBlock` 을 부르는 자리는 3곳이 아니라 **4곳** (`+0x219` 추가, 지도엔 이미 있었음).

### 정적 — `PspSetupUserProcessAddressSpace` 안 순서

| 순서 | call | ret | 하는 일 | 비고 |
|---|---|---|---|---|
| 0 | `+0x8d` | `+0x92` | `PspGetStandardHandleList` | 표준 핸들 상태가 "1" 일 때만 (아래 핸들 절) |
| 0' | — | — | 커널 사본의 `Flags` 비트 15·29 켜기, `LoaderThreads` 기본값 (`+0xa0 ~ +0xea`) | **붙기 전**, 복사 전 |
| 1 | `+0xf5` | `+0xfa` | `KiStackAttachProcess` | 새 프로세스로 붙음 |
| 2 | `+0x197` | `+0x19c` | `ZwAllocateVirtualMemory` | 매개변수 블록 자리 |
| 3 | `+0x1b4` | **`+0x1b9`** | `PspCopyAndFixupParameters` | **PEB `+0x20` 이 여기서 채워짐** |
| 4 | `+0x1e1` | **`+0x1e6`** | `PspPrepareSystemDllInitBlock` (64비트) | 보안 프로세스면 건너뜀 |
| 4' / 4'' | `+0x1f2` / `+0x214` | `+0x1f7` / `+0x219` | `PspWow64SetupUserProcessAddressSpace` / `PspPrepareSystemDllInitBlock` (32비트) | 4'' 는 WoW64 만 |
| 5 | `+0x222` / `+0x22f` | `+0x227` / `+0x234` | `KeCopyXfdMaskToPeb` / `PspUpdatePebForAffinityChange` | |
| 6 | `+0x250` | **`+0x255`** | `MmMapApiSetView` | 보안 프로세스면 건너뜀 |
| 7 | `+0x25e` | **`+0x263`** | `PspMapSiloSharedDataView` | |
| 8 | `+0x26a` | `+0x26f` | `KiUnstackDetachProcess` | 떼어냄 |

- run10 순서 **할당 → 매개변수 복사 → 시작 정보 블록 → ApiSet 뷰** 가 맞다. 사이에 몇 개 더 있을 뿐이다.
- `PspSetupUserProcessAddressSpace` 자신은 PEB 를 건드리는 줄이 없다 (`.asm` 에 `EPROCESS.Peb` `+0x2e0` 읽기가 없음). PEB `+0x20` 은 3번 안에서 쓴다.
- 3번과 4번은 서로의 결과를 안 읽는다. 순서는 코드 순서일 뿐이다.

### 정적 — PEB `+0x20` 쓰기와 매개변수 복사 (`PspCopyAndFixupParameters`)

**누가 쓰나: `PspCopyAndFixupParameters` 다.**

- 새 PEB 를 찾는 길: 현재 스레드(`GS:[0x188]`) → `+0xb8` (지금 붙은 프로세스) → `+0x2e0` (`_EPROCESS.Peb`) → `+0x20` (`_PEB.ProcessParameters`). 스레드가 새 프로세스에 **붙어 있어서** 현재 프로세스 = 새 프로세스다.
- 쓰는 값 = 새 프로세스 안에 복사한 매개변수 블록 시작 주소 (`Dst`).
- 기능 스위치 하나로 두 갈래:

| 갈래 | 조건 | PEB `+0x20` 쓰는 줄 | 복사 방식 |
|---|---|---|---|
| 스위치 꺼짐 | `+0x1a2` 반환 0 → `+0x1a4 JZ` | **`+0x514 ~ +0x52b`** (`MOV [RAX+0x20],RDI`, 그냥 저장) | `+0x41f` (ret `+0x424`) 메모리 복사로 먼저 옮기고 → 복사본 안에서 포인터 고침 |
| 스위치 켜짐 | 반환 ≠ 0 | **`+0x347` (ret `+0x34c`) `RtlWriteULong64ToUser(PEB+0x20, Dst)`** | 원본 앞 0x450 바이트 백업 → **원본 쪽에서** 포인터 고침 → `+0x324` (ret `+0x329`) `RtlCopyToUser` → PEB 에 씀 → 백업 되돌림 |

- 스위치 = 전역 `Feature_CreateProcessSegmentHeapEnablement__private_featureState` (rva `0xfc4110`) 의 비트 4·0. **실행 때 값이라 어느 갈래인지는 동적** (Q35). 이름의 "SegmentHeap" 이 이 함수가 하는 일과 왜 묶였는지는 모른다.
- 두 갈래 모두 **복사가 끝난 뒤** PEB 에 쓴다. 쓰기 결과는 검사하지 않는다 (`+0x34c` 뒤에 `TEST` 없음).

**원본(`Src`)과 도착(`Dst`)**

- `Src` = 만들기 정보 구조 `+0xd8` (`+0x2a`), `Dst` = 같은 구조 `+0xe0` (`+0x40`). 구조 포인터는 셋째 인자.
- `Src` 를 만드는 곳: `NtCreateUserProcess +0xac6` (ret `+0xacb`) `PspCaptureProcessParameters` 가 **부모가 준 9번째 인자**를 받아, 사용자 모드 호출이면 `+0x3a` (ret `+0x3f`) `PspCaptureUserProcessParameters` 로 **커널 풀에 새 사본**을 만든다. 커널 모드 호출이면 받은 포인터를 그대로 쓴다 (`+0x58 ~ +0x5a`).
  - 부모 쪽에서 그 9번째 인자를 만드는 게 `BasepCreateProcessParameters` 라는 건 run14 `[추측]` 그대로다. 이번 코드는 "커널이 유저가 준 포인터를 가져온다" 까지만 `[정적]`.
- `Dst` 를 잡는 곳: `PspSetupUserProcessAddressSpace +0x197` (ret `+0x19c`) `ZwAllocateVirtualMemory(현재 프로세스 = 붙은 새 프로세스, &[구조+0xe0], …, 0x3000, 4)`. 주소 칸을 `+0x17f` 에서 0 으로 비워서 시스템이 자리를 고른다. 종류·보호 값은 `MEM_COMMIT|MEM_RESERVE`·`PAGE_READWRITE` 와 같은 숫자.
  - 크기 = `MaximumLength` + `EnvironmentSize` (+ 스위치 켜짐이면 사용자 속성 목록). `Flags & 0x1000060` 이 0 이고 `0x20000` 보다 작으면 `0x20000` 으로 올린다 (`+0x15c ~ +0x16b`).

**"Fixup" = 안쪽 포인터 12개를 새 주소 기준으로 고치기 (절대 주소)**

- 공식: 칸 = `Dst + (칸 − Src)`, 칸이 0 이면 건너뜀. 오프셋으로 되돌리지 않는다.
- 고치는 칸 (PDB `_RTL_USER_PROCESS_PARAMETERS`): `CurrentDirectory.DosPath.Buffer` (+0x40), `DllPath.Buffer` (+0x58), `ImagePathName.Buffer` (+0x68), `CommandLine.Buffer` (+0x78), `Environment` (+0x80), `WindowTitle.Buffer` (+0xb8), `DesktopInfo.Buffer` (+0xc8), `ShellInfo.Buffer` (+0xd8), `RuntimeData.Buffer` (+0xe8), `RedirectionDllName.Buffer` (+0x418), `HeapPartitionName.Buffer` (+0x428), `DefaultThreadpoolCpuSetMasks` (+0x430).
- 안 고치는 포인터 칸: `PackageDependencyData` (+0x400), `CurrentDirectores[32]` 안쪽. 왜인지는 모른다.
- **정규화 비트(`Flags` 비트 0)는 여기서 안 건드린다.** 이 함수가 `Flags` 를 바꾸는 줄은 `+0x82 BTS [RBX+8],0xe` (비트 14) 하나뿐이다 (만들기 정보 `+0x10` 비트 6 켜짐 · `+0x11` 비트 0 꺼짐일 때).
- 대신 한 단계 위 `PspCaptureUserProcessParameters +0x8f ~ +0xa8` 이 **조건으로 검사**한다: 비트 14 를 지운 뒤 `(Flags ^ 1) & 0xf4010e11 ≠ 0` 이면 `0xC000000D` 로 실패. 곧 들어오는 블록은 **비트 0 이 켜진(정규화된) 꼴**이어야 하고, 마스크의 다른 비트는 꺼져 있어야 한다. 각 비트 이름은 모른다 (Q63 (a)).
  - 그래서 커널 사본은 이미 "포인터가 든 꼴" 이고, 고치는 공식이 절대 주소 기준인 것과 맞다.

**핸들 복제 두 자리 (`ObDuplicateObject`: 부모 → 새 프로세스)**

| 자리 (ret) | 무엇 | 조건 |
|---|---|---|
| `+0xbc` | `ConsoleHandle` (`Src+0x10`) 를 복제해 같은 칸에 되씀 | 이미지 `SubSystemType` 이 3 (콘솔 `[추측: 값 이름]`) 이고 핸들 값 `> 0` |
| `+0x130` | 표준 입력·출력·오류 (`Src+0x20`/`+0x28`/`+0x30`), 고리 세 번 | 복제할 핸들 목록이 있을 때. 목록은 만들기 정보 `+0x11` 비트 2~3 값에 따라 (1: 부모의 표준 핸들 목록 `PspGetStandardHandleList`, 2: 매개변수 안 칸 그대로, 0·3: 없음). 2 일 때만 실패가 전체 실패 |

- `[추측]` 그 값은 공개 문서의 `PS_STD_HANDLE_INFO.StdHandleState` (0 안 함 / 1 요청 시 / 2 늘) 와 모양이 같다. `+0x11` 을 채우는 줄은 안 봤다 (Q63 (c)).

### 정적 — `PspPrepareSystemDllInitBlock` 이 채우는 블록

**어디에 쓰나: 새 프로세스 안 ntdll 의 전역 `LdrSystemDllInitBlock` (0x128 바이트).** 커널 쪽 표가 아니다.

- 주소 얻기 (첫 인자 `ECX`):
  - `ECX == 0` (64비트 ntdll): `+0x214` `MOV RBX,[PsNtdllExports]` — 칸 `+0x0` = PDB `_PS_NTDLL_EXPORTS.LdrSystemDllInitBlock`. 부팅 때 채운 표다 (run16 `PsNtdllExports` 와 같은 표).
  - `ECX == 1` (32비트 ntdll): `+0x2dd` (ret `+0x2e2`) `PspWow64GetSharedInformation(1)` 이 돌려준 표의 `+0x38` 칸 (PDB 열거형 `SharedNtdll32LdrSystemDllInitBlock = 7`).
- 현재 스레드가 붙은 프로세스를 "새 프로세스" 로 쓴다 (`+0x35 ~ +0x3e`). 부르는 쪽이 붙은 뒤에 부른다.
- 크기 확인: 블록 맨 앞 4바이트가 `0x128` 이 아니면 (`+0x98 ~ +0x9f`) `0xC0000059` 로 끝 (`+0x1ee`).

채우는 칸 (블록 오프셋·쓰는 줄·값 출처는 `[정적]`. 칸 이름은 PDB 에 없어서 결과서가 공개 비공식 헤더에서 빌린 후보 = `[추측]`):

| 블록 칸 | 쓰는 줄 | 값 출처 `[정적]` | 이름 후보 `[추측]` |
|---|---|---|---|
| `+0x0` | 안 씀 (읽어서 `0x128` 확인) | ntdll 이미지 쪽 처음 값 | Size |
| `+0x8` | `+0xda` | `PspSystemDlls[종류]` 의 `+0x38` (종류 = `+0xb7` `PsWow64GetProcessNtdllType`, 0 이면 1) | SystemDllWowRelocation |
| `+0x10` | `+0xb0` | `PspNativeSystemDllData +0x38` | SystemDllNativeRelocation |
| `+0x18 ~ +0x97` | 고리 `+0xef` | `+0xc3` `PspWow64GetSharedInformation(종류)` 표 16칸 복사 | Wow64SharedInformation[16] |
| `+0x98` | `+0x100` | **`ExGenRandom(1)`** (`+0xfb`, ret `+0x100`) | RngData |
| `+0x9c` | `+0x106` (0), `+0x120` (비트 0) | 만들기 정보 `+0x11` 비트 1 | Flags |
| **`+0xa0 ~ +0xb7`** (24B) | **`+0x12b`, `+0x138`** | 만들기 정보 `+0x150 ~ +0x167`. 그 칸은 `PspAllocateProcess +0x1f3f ~ +0x1f50` 이 쓴다 (`+0x1eb3` `PspApplyMitigationOptions` 에 넘긴 지역 24바이트) | MitigationOptionsMap |
| **`+0xb8`** | **`+0x15a`** | 새 프로세스 `Vm.Instance.VmWorkingSetList` (`EPROCESS+0x410`, PDB) 가 가리키는 구조의 `+0x4a0` (이름 PDB 에 없음) | CfgBitMap |
| `+0xc0` | `+0x169` | 같은 구조 `+0x4a8` | CfgBitMapSize |
| `+0xc8`, `+0xd0` | `+0x178`, `+0x187` | WoW64 프로세스면 같은 구조 `+0x4c0`·`+0x4c8`, 아니면 0 | Wow64CfgBitMap(Size) |
| `+0xd8 ~ +0xef` (24B) | `+0x145`, `+0x152` | 만들기 정보 `+0x1c0 ~ +0x1d7` (`PspAllocateProcess +0x1f61 ~ +0x1f72`) | MitigationAuditOptionsMap |
| `+0xf0 ~ +0x127` (7칸) | `+0x1a3 ~ +0x1e5` | `+0x196` (ret `+0x19b`) `PspGetScpCfgFunctions` 가 돌려준 전역 표 `PspNtdllScpFunctions` 7칸 (NULL 이면 안 채움) | CFG 검사·분배 함수 주소 류 |

- `PspGetScpCfgFunctions` 는 `ZwQueryVirtualMemory` 결과나 `PsGetScpCfgPageTypeForProcess` 값에 따라 NULL 을 돌려줄 수 있다. 표를 채우는 건 부팅 때 `PspLocateNtdllAddressesForScpCfg` (`+0x1bf`·`+0x1d6`·`+0x1ec`·`+0x202`, 표 앞 4칸) 이고, 그 값이 ntdll 안 어떤 함수인지는 안 봤다 (Q63 (b)). "SCP" 의 뜻은 모른다.
- `+0xb8` 비트맵을 만드는 쪽은 지도상 `MmInitializeProcessAddressSpace +0x1c9 → MiMapProcessExecutable +0xe3 → MiCfgInitializeProcess` 로 보인다. `PspAllocateProcess +0x2095` 가 `+0x239c` 보다 앞이라 시간 순서는 맞다. `MiCfgInitializeProcess` 가 `+0x4a0` 칸을 쓰는지는 안 봤다 (Q63 (b)).
- `RDX = 0` 으로 부르는 `PspAllocateProcess +0x25dc`·`+0x25fc` 갈래에서는 `+0xa0`·`+0xd8` 이 0 이 된다 (`+0x2b5 ~ +0x2d1`).

**test.exe 는 어느 자리에서 부르나 (정적 조건)**

- `PspAllocateProcess +0x236c ~ +0x2397`: 묶음 플래그(스택 `[RSP+0x84]`) 비트 7 켜짐 + 만들기 정보 구조 ≠ NULL + 묶음 플래그 비트 1 꺼짐 → `PspSetupUserProcessAddressSpace` (ret `+0x239c`). 아니면 `+0x2574` 갈래 → `+0x25dc` (+ WoW64 면 `+0x25fc`).
  - 비트 7 은 `+0x20d2 BTS EBX,7` 이 켠다 (`MmInitializeProcessAddressSpace` 성공 뒤, 섹션 있는 보통 실행일 때).
- `NtCreateUserProcess` 는 11번째 인자로 지역 구조를 넘기고 (`+0xbe6`), 7번째 인자로 exe 섹션을 넘긴다. 그래서 **test.exe 는 `PspSetupUserProcessAddressSpace +0x1e6` 한 자리**일 것이다 `[정적 조건 + 추측]`. `+0x219`·`+0x25dc`·`+0x25fc` 는 안 설 것 (64비트, 보통 실행). 실제 플래그 값은 동적 (Q35).
- 반대 갈래(`+0x25d7`)를 타는 프로세스는 매개변수를 복사하지 않으니 **PEB `+0x20` 이 0 으로 남는다** (`PspCopyAndFixupParameters` 를 안 부름).

### 정적 — 커널이 쓰는 칸 ↔ ntdll 이 읽는 줄 (짝 표)

ntdll 쪽은 **이미 문서(flow.md)에 적힌 것만** 짝지었다 (이번에 ntdll 을 새로 읽지 않음).

| 칸 | 커널이 쓰는 줄 `[정적]` run32 | ntdll 이 읽는 줄 (문서) | 짝 |
|---|---|---|---|
| **PEB `+0x20`** (`ProcessParameters`) | `PspCopyAndFixupParameters +0x52b` 또는 `+0x347` (ret `+0x34c`) | `LdrpInitialize` 가 `ProcessParameters.Flags` 맨 윗 비트를 봄 (`+0x5a`, run17). `LdrpInitializeProcess` `+0x6c5 RtlNormalizeProcessParams`, `+0xc3f RtlpInitParameterBlock` (run18) | **맞음.** ntdll 이 읽는 블록은 커널이 복사해 건 것 |
| `Flags` 비트 29 (`0x20000000`) | **`PspSetupUserProcessAddressSpace +0xe6 BTS EBX,0x1d` → `+0xea` 저장.** 조건: `PsGetProcessServerSilo` (ret `+0xd5`) → `PsGetServerSiloGlobals` (ret `+0xdd`) → 결과 `+0x3f8` 바이트 ≠ 0 (PDB `_ESERVERSILO_GLOBALS.OneCoreForwardersEnabled`) | `LdrpInitializeKernel32Functions` "이미 있음" 길 `+0x1ff` 에서 이 비트가 켜졌으면 (4)~(6) 건너뜀 (run20) | **맞음 + 이름 단서.** 서버 silo 의 "OneCore forwarders 켜짐" 을 옮겨 적은 비트로 보인다 `[추측: 이름]` |
| `Flags` 비트 15 (`0x8000`) | `PspSetupUserProcessAddressSpace +0xb1` (조건 `PspGlobalFlags` 비트 0) | 문서에 없음 | 빈 곳 (Q63 (d)) |
| `Flags` 비트 14 (`0x4000`) | `PspCopyAndFixupParameters +0x82` | 문서에 없음 | 빈 곳 (Q63 (d)) |
| `LoaderThreads` (+0x40c) | `PspSetupUserProcessAddressSpace +0xb8 ~ +0xc7`: 0 이면 전역 `PsDefaultLoaderThreads` 로 채움 | `LdrpInitializeProcess +0x1a47 LdrpEnableParallelLoading` 의 `ECX` (run19·run24). IFEO `MaxLoaderThreads` 는 `LdrpInitializeExecutionOptions` (ret `+0x536`) 가 같은 칸에 씀 (run18) | **맞음.** 커널이 먼저 기본값, ntdll 이 IFEO 로 덮을 수 있음. `PsDefaultLoaderThreads` 값은 동적 Q47 |
| **블록 `+0xa0`** (24B) | `PspPrepareSystemDllInitBlock +0x12b`, `+0x138` | `LdrpInitializeProcess` 묶음 4 `+0x4a8 ~ +0x579` (run18 "무결성 설정 비트 두 개") | **맞음 (구간).** 두 비트의 정확한 읽는 줄·뜻은 그대로 빈 곳 |
| **블록 `+0xb8`** | `PspPrepareSystemDllInitBlock +0x15a` | `LdrpDispatchUserCallTarget` `+0x0 ~ +0x2f` (run28) | **맞음.** 값 = CFG 비트맵 주소 |
| 블록 `+0x8`·`+0x10`·`+0x18 ~ +0x97`·`+0x98`·`+0x9c`·`+0xc0 ~ +0xd0`·`+0xd8 ~ +0xef`·`+0xf0 ~` | 위 블록 표 | 문서에 없음. `+0x98` (난수) ↔ run18 `LdrInitializePerThreadRng` 는 이름만 본 후보 `[추측]` | 빈 곳 (Q63 (d)) |

- run20 과 이어지는 이야기: kernel32 준비에서 "이미 올라와 있고 + 비트 29 켜짐" 이면 `BaseThreadInitThunk` 찾기 등 (4)~(6) 을 건너뛴다. 그 비트는 사용자가 줄 수 없고 (위 입력 검사 마스크 `0xf4010e11` 에 비트 29 가 들어 있어서 켜서 넘기면 실패), **커널이 silo 설정을 보고 켠다.** 보통 데스크톱 test.exe 에서 그 silo 값이 켜져 있는지는 실행 때 값이다 (Q35).

### 호출 관계

```
PspAllocateProcess                                   (이 단계 번호는 생략. 그림 1 번호는 +0x2095 = (2), +0x239c = (4))
 ├─ +0x1eb8  PspApplyMitigationOptions          완화 옵션 24B → 만들기 정보 +0x150 (+0x1f3f ~ +0x1f50)
 ├─ +0x2095  MmInitializeProcessAddressSpace    (run10. 그 아래 CFG 비트맵 준비로 보임 — Q63 (b))
 │   +0x20d2 묶음 플래그 비트 7 켬
 └─ +0x239c  PspSetupUserProcessAddressSpace    (갈림길 +0x236c ~ +0x2397. 아니면 +0x25dc / +0x25fc)
       │  +0xa0 ~ +0xea  커널 사본 Flags 비트 15·29, LoaderThreads 기본값   (붙기 전)
       ├─ +0xfa   KiStackAttachProcess
       ├─ +0x19c  ZwAllocateVirtualMemory            Dst 자리
       ├─ (1) +0x1b9  PspCopyAndFixupParameters
       │     ├─ +0xbc / +0x130  ObDuplicateObject     콘솔 핸들 / 표준 핸들 3개
       │     └─ +0x1a2  기능 스위치 → 둘 중 하나:
       │           꺼짐: +0x424 메모리 복사 → 포인터 12개 고침 → +0x52b PEB+0x20 저장
       │           켜짐: 포인터 고침 → +0x329 RtlCopyToUser → +0x34c RtlWriteULong64ToUser(PEB+0x20)
       ├─ (2) +0x1e6  PspPrepareSystemDllInitBlock(0, 만들기 정보)   보안 프로세스 아닐 때
       │     │  블록 = [PsNtdllExports+0x0] = 새 프로세스 안 ntdll LdrSystemDllInitBlock
       │     ├─ +0xbc  PsWow64GetProcessNtdllType
       │     ├─ +0xc8  PspWow64GetSharedInformation
       │     ├─ +0x100 ExGenRandom          → 블록 +0x98
       │     │  +0x12b/+0x138 → 블록 +0xa0 (완화 옵션),  +0x15a → 블록 +0xb8 (CFG 비트맵)
       │     └─ +0x19b PspGetScpCfgFunctions → 블록 +0xf0 ~
       ├─ +0x219  PspPrepareSystemDllInitBlock(종류, …)   WoW64 만
       ├─ +0x255  MmMapApiSetView
       ├─ +0x263  PspMapSiloSharedDataView
       └─ +0x26f  KiUnstackDetachProcess
```

- `(n)` 은 그림 1 에 넣은 두 상자의 번호다. 나머지 호출은 그림 밖 (글에만).

### 그림 반영 (flow.md 그림 1)

- `p8` (`PspSetupUserProcessAddressSpace`) 아래에 **`(정적)` 점선 상자 둘**을 넣었다: `(1) +0x1b9` `PspCopyAndFixupParameters` (PEB `+0x20` 쓰기), `(2) +0x1e6` `PspPrepareSystemDllInitBlock` (ntdll `LdrSystemDllInitBlock` 채우기).
  - 둘 다 ntdll 이 나중에 읽는 칸을 쓰는 **대표 지점**이라 따로 상자로 뒀다 (함수 하나 = 상자 하나). 할당·ApiSet 뷰 등 나머지 호출은 run10 때처럼 글에만 둔다.
- `p8` 상자 글에 "붙기 전 커널 사본 `Flags`·`LoaderThreads` 손봄" 을 더했다.

## 확인된 것

- (`[정적]`) **PEB `+0x20` 은 `PspCopyAndFixupParameters` 가 쓴다.** 기능 스위치에 따라 `+0x52b` (저장) 또는 `+0x347` (ret `+0x34c`, `RtlWriteULong64ToUser`). 부르는 쪽에서 보면 `PspSetupUserProcessAddressSpace +0x1b4 ~ +0x1b9` 사이다.
- (`[정적]`) 매개변수 원본 = `NtCreateUserProcess +0xacb` `PspCaptureProcessParameters` 가 만든 **커널 사본**. 새 프로세스 안 자리 = `+0x19c` `ZwAllocateVirtualMemory`. 복사 뒤 안쪽 포인터 12개를 **절대 주소**로 고친다. 정규화 비트는 안 건드리고, 들어올 때 "정규화된 꼴" 을 검사한다.
- (`[정적]`) 콘솔 핸들·표준 입출력 핸들도 이 함수가 새 프로세스용으로 복제한다.
- (`[정적]`) **`PspPrepareSystemDllInitBlock` 은 새 프로세스 안 ntdll 의 `LdrSystemDllInitBlock` (0x128 바이트) 에 직접 쓴다.** 주소는 `PsNtdllExports+0x0`.
- (`[정적]`) run18 의 `LdrSystemDllInitBlock+0xa0` 과 run28 의 `+0xb8` **둘 다 여기서 채운다** (`+0x12b`/`+0x138`, `+0x15a`).
- (`[정적]`) `ProcessParameters.Flags` **`0x20000000` 비트는 커널 `PspSetupUserProcessAddressSpace +0xe6` 이 켠다.** 조건 = 서버 silo 전역 `OneCoreForwardersEnabled` (PDB 칸 이름). run20 의 kernel32 건너뜀 비트가 이것이다.
- (`[정적]`) `LoaderThreads` 가 0 이면 커널이 `PsDefaultLoaderThreads` 로 먼저 채운다 (`+0xc7`). 그 전역 값은 동적 Q47.
- (`[정적]`) run10 순서 (할당 → 매개변수 복사 → 시작 정보 블록 → ApiSet 뷰) 가 맞다.

### flow.md 판정

| 예전 글 | 판정 | 근거 |
|---|---|---|
| 2.1 `[추측]` "`PspCopyAndFixupParameters` 가 프로세스 매개변수(명령줄 등)를 새 프로세스로 복사하고 PEB `+0x20` 에 잇는 곳" | **맞다 → `[정적]`** | `+0x52b` / `+0x347`(ret `+0x34c`), `+0x424` / `+0x329` |
| 2.1 `[추측]` "`PspPrepareSystemDllInitBlock` 은 ntdll 에 넘길 시작 정보 묶음을 준비한다" | **맞다 → `[정적]`**. 정확히는 새 프로세스 안 ntdll `LdrSystemDllInitBlock` 에 직접 쓴다 | `+0x214`, 블록 표 |
| 2.1 `MmCreatePeb` "PEB `+0x20` 은 … 누가 채우는지는 아직 모른다" | **해결** → `PspCopyAndFixupParameters` | 위 |
| 4장 2.1·2.4 "PEB `+0x20` 누가 쓰나" | **해결됨** | 위 |
| 4장 2.1 "`PspPrepareSystemDllInitBlock` 안쪽 (2.3 공백과 이어질 수 있음)" | **해결됨.** 2.3 과 이어지는 줄은 없다 (`LdrInitializeThunk` 로 가는 `Rdx` 는 `PspSystemDlls[0]+0x20`, run16·run18). 결과서는 `PspNativeSystemDllData +0x20` 이라 부름 — 같은 칸인지 [확인 필요] Q63 (f) | 블록 표 |
| 4장 2.4 "`LdrSystemDllInitBlock+0xa0` 의 무결성 설정 비트 두 개" | **일부** — 커널 출처 (완화 옵션 24B) 까지 | `+0x12b`/`+0x138`, `PspAllocateProcess +0x1f3f ~ +0x1f50` |
| 4장 2.5 "`Flags 0x20000000` 비트 이름" | **일부** — 켜는 줄과 조건까지, 이름은 `[추측]` | `+0xe6` |

## 아직 확인 필요

동적 확인거리는 오케스트레이터가 대기열 **Q35** 에 출처로 붙였다 (`PspAllocateProcess` 한 번 실행 안에서 함께). 커널 멈춤점이라 test.exe 일 때만 서게 조건이 필요하다 (dast-mentor 몫). 붙어 있는 동안 메모리 읽기 문맥은 `.process /r /p <새 EPROCESS>` 가 필요할 수 있다 `[추측]`.

- **Q35** (sast/27 동적 1 ~ 4):
  - `dd nt+0xfc4110 L1` — 기능 스위치 비트 4·0 → `+0x52b` 갈래인가 `+0x347` 갈래인가.
  - `nt+0x9395d0` (`PspPrepareSystemDllInitBlock` 입구) 에서 `[rsp]` = `nt+0xae5f1a` (`PspSetupUserProcessAddressSpace +0x1e6`) 인가. `nt+0xa04b44` (`PspAllocateProcess +0x25dc`), `nt+0xa04b64` (`+0x25fc`), `nt+0xae5f4d` (`+0x219`) 는 안 서야 한다.
  - `nt+0xae5eed` (`+0x1b9`, 복사 직후): 새 PEB `+0x20` 값, 매개변수 `Flags` 의 `0x1`·`0x4000`·`0x8000`·`0x20000000`, `LoaderThreads`, 문자열 `Buffer` 들이 `Dst` 안을 가리키나.
  - `nt+0xae5f1a` (`+0x1e6`, 채운 뒤): 블록 `+0x98` ≠ 0, `+0xa0` 24B, `+0xb8`/`+0xc0`, `+0xf0 ~` 함수 주소를 `ln`.
- **Q63** (정적, 작게):
  - (a) `PspCaptureUserProcessParameters` 의 `Flags` 마스크 `0xf4010e11` 각 비트와 `Environment` 배치.
  - (b) `MiCfgInitializeProcess` 가 작업 집합 구조 `+0x4a0`/`+0x4a8` 을 쓰나, `PspLocateNtdllAddressesForScpCfg` 가 넣는 ntdll 함수 이름.
  - (c) `PspBuildCreateProcessContext` 가 만들기 정보 `+0x11` (표준 핸들 상태) 를 채우는 줄.
  - (d) (ntdll 새로 읽기 — 할지 차례 때 사용자와 정함) 블록의 문서 없는 칸과 `Flags` `0x4000`·`0x8000` 을 ntdll 이 읽는 줄, `RtlNormalizeProcessParams` 가 이미 정규화된 블록에서 하는 일.
  - (e) `ProcessParameters.Flags` 비트 31 (run17 `LdrpIsSecureProcess` 비트) 을 누가 켜나. 입력 마스크 `0xf4010e11` 에 들어 있어 사용자는 못 켜고, 이번에 읽은 커널 줄은 비트 14·15·29 만 켠다.
  - (f) 작은 뜻 찾기: `PackageDependencyData`(+0x400)·`CurrentDirectores[32]` 안쪽을 안 고치는 이유, 기능 스위치 이름의 "SegmentHeap", "SCP" 의 뜻.
- **Q47** (동적): 커널 기본값 `PsDefaultLoaderThreads` 의 실제 값 (IFEO 가 없을 때 ntdll 이 받는 `LoaderThreads`).

## 디버거 메모 / 도구 메모

- (동적 함정 후보) `PspCopyAndFixupParameters` 와 `PspPrepareSystemDllInitBlock` 은 **부모 cmd.exe 스레드가 새 프로세스에 붙어 있는 동안** 돈다. 그 사이 쓰는 주소(PEB, `Dst`, `LdrSystemDllInitBlock`)는 새 프로세스의 유저 주소라, 디버거가 부모 문맥으로 읽으면 엉뚱한 값을 볼 수 있다 `[추측]`. → windbg-kd-runbook pitfalls 에 넣자고 보고함.
- (도구) PDB 구조체 칸 표를 뽑는 명령이 스킬에 없어서 결과서가 이번에도 `llvm-pdbutil` 덤프를 임시 스크립트로 읽었다 (plan 하네스 후보 (40) 과 같은 이야기).
- (도구) `.asm` 전역 주석은 Ghidra 짐작이 섞인다. `PspPrepareSystemDllInitBlock +0xac` 의 `PspNativeSystemDllData+0x38` 은 `refs` 로 맞음을 확인했다 (결과서).

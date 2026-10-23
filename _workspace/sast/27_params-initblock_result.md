# 27 params-initblock — 정적 분석 결과

- 요청서: `_workspace/sast/27_params-initblock.md` (Q36)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`ntoskrnl.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 셋 다 있음). 지도는 기존 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*` 를 그대로 썼다.
- 뽑은 함수: **9개, 전부 depth 0** (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`). 요청서 표는 depth 1 이었지만 "callee 본문은 읽지 않는다" 라서 필요한 것만 이름으로 골랐다.
  - 요청한 둘: `PspCopyAndFixupParameters` (rva `0xae57e4`, 1340B), `PspPrepareSystemDllInitBlock` (rva `0x9395d0`, 812B). `PspSetupUserProcessAddressSpace`·`PspAllocateProcess`·`NtCreateUserProcess` 는 이미 있어서 다시 안 뽑았다.
  - 질문 2 (칸 출처) 때문에 더 뽑은 작은 것 3개: `PsWow64GetProcessNtdllType` (17B), `PspWow64GetSharedInformation` (16B), `PspGetScpCfgFunctions` (161B).
  - 질문 1 (원본이 어디서 오나) 때문에 위쪽으로 올라가 뽑은 3개: `PspCaptureProcessParameters` (104B), `PspCaptureUserProcessParameters` (2071B, **머리와 호출 이름만 봤고 본문 전체는 안 읽었다**), `PspCaptureCreateInfo` (189B).
  - 갈림길 전역을 보려고 1개: `Feature_CreateProcessSegmentHeapEnablement__private_IsEnabledDeviceUsageNoInline` (49B).
- 호출자·전역 표 (`refs`): `PspNtdllScpFunctions`, `PspNativeSystemDllData`, `PsWowX86SharedInformation` → `ms/disassemble/ntoskrnl/_refs/<이름>.tsv`.
- 구조체 칸은 PDB 타입 덤프 (`llvm-pdbutil-14 dump -types`, 임시 스크립트로 읽음)로 맞췄다. **`_PS_SYSTEM_DLL_INIT_BLOCK` 같은 타입은 PDB 에 없다** (아래 질문 2).
- 버전 확인: **일치.**
  - `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess` (`_calls.tsv`, `.asm` 둘 다). `PspInsertThread` call `+0x802` / ret **`+0x807`** → `PspCallProcessNotifyRoutines`.
  - 요청서 지도 값 `PspCopyAndFixupParameters` 0xae57e4/1340, `PspPrepareSystemDllInitBlock` 0x9395d0/812 가 `_index.tsv` 와 같다.

## 요청서와 다른 점 (사실만)

- `PspCopyAndFixupParameters` 의 호출은 **8곳**이다 (요청서는 "호출 9" 라고 썼지만 적어 둔 이름은 8개이고 `.asm` CALL 줄도 8개).
- `PspPrepareSystemDllInitBlock` 을 부르는 자리는 **4곳**이다. 요청서에 없는 넷째: `PspSetupUserProcessAddressSpace +0x214` (ret `+0x219`). 지도 `_edges.tsv` 에는 이미 있었다.

## 스크립트 문제

고칠 버그는 없다. 제안·참고 3건.

- **PDB 구조체 칸을 뽑는 도구가 스킬에 없다** (plan 하네스 후보 (40) 과 같은 이야기). 이번에도 `llvm-pdbutil-14 dump -types` 를 임시 스크립트로 읽었다.
- `_PS_SYSTEM_DLL_INIT_BLOCK`, `_PS_SYSTEM_DLL_DATA` 류는 공개 PDB 에 없다. 칸 이름을 PDB 로 못 정하는 게 정상이다.
- `.asm` 의 전역 변수 주석은 레지스터 기준 칸에서 Ghidra 짐작이다 (예: `PspPrepareSystemDllInitBlock +0xac` 의 `; PspNativeSystemDllData+0x38` 는 `refs` 로 확인해서 맞았다).

## 한눈에

- **PEB `+0x20` 은 `PspCopyAndFixupParameters` 가 쓴다.** `PspSetupUserProcessAddressSpace` 자신은 PEB 를 건드리는 줄이 없다. 쓰는 줄은 갈림길에 따라 `+0x52b` (그냥 저장) 또는 `+0x347` (call, ret `+0x34c`, `RtlWriteULong64ToUser`). 갈림길은 기능 스위치 전역 하나 (`nt+0xfc4110`)라서 **어느 쪽인지는 동적**이다.
- **"Fixup" = 안쪽 포인터 12개를 새 주소 기준으로 고치는 것.** 절대 주소 그대로 (오프셋으로 되돌리지 않는다). `new = Dst + (old − Src)`. `Flags` 의 정규화 비트(`0x1`)는 여기서 건드리지 않는다. 대신 한 단계 위(캡처)에서 "정규화돼 있어야 한다"를 검사한다.
- **원본 = 커널 풀에 새로 만든 복사본.** 부모가 `NtCreateUserProcess` 에 넘긴 9번째 인자를 `PspCaptureProcessParameters` (`NtCreateUserProcess +0xac6`) 가 커널로 가져와 `만들기 정보 구조 +0xd8` 에 둔다. `PspCopyAndFixupParameters` 는 그걸 새 프로세스 안(`ZwAllocateVirtualMemory` 로 잡은 곳)으로 옮긴다.
- **`PspPrepareSystemDllInitBlock` 은 새 프로세스 안 ntdll 의 `LdrSystemDllInitBlock` (0x128 바이트) 를 채운다.** 주소는 커널 전역 `PsNtdllExports` 의 `+0x0` 칸 (PDB 타입 `_PS_NTDLL_EXPORTS.LdrSystemDllInitBlock`) 에서 얻는다. 커널 쪽 전역 표가 아니다.
- **run18 의 `+0xa0` 과 run28 의 `+0xb8` 둘 다 여기서 채운다.** `+0xa0` (24바이트) ← `PspAllocateProcess` 가 만들어 둔 완화 옵션 24바이트. `+0xb8` ← 새 프로세스 작업 집합 구조 `+0x4a0` 의 값 (이름은 PDB 에 없음).
- test.exe 에서 이 함수가 서는 자리는 정적으로 `PspSetupUserProcessAddressSpace +0x1e6` 한 곳이 맞아 보인다 (조건 아래). 실제로 그런지는 동적.
- 덤: 커널이 `ProcessParameters.Flags` 의 **`0x20000000` 비트**를 켜는 줄을 찾았다 (`PspSetupUserProcessAddressSpace +0xe6`). flow.md 가 이름을 못 정한 그 비트다 (질문 4 표).

## 질문별 답 [정적]

### 1. PEB `+0x20` (`ProcessParameters`) 와 매개변수 복사

**1-a. 누가 쓰나: 예, `PspCopyAndFixupParameters` 다.** (`ms/disassemble/ntoskrnl/PspCopyAndFixupParameters.asm`)

- 새 PEB 얻는 방법: `GS:[0x188]` (현재 스레드) → `+0xb8` (`_KTHREAD.ApcState.Process`, PDB `_KAPC_STATE.Process` +0x20) → `+0x2e0` (PDB `_EPROCESS.Peb`) → `+0x20` (PDB `_PEB.ProcessParameters`). 스레드가 새 프로세스에 **붙어 있어서** (`PspSetupUserProcessAddressSpace +0xf5 KiStackAttachProcess`, ret `+0xfa`) 현재 프로세스 = 새 프로세스다.
- 쓰는 값: `RDI` = 새 프로세스 안에 복사해 둔 매개변수 블록 시작 주소 (아래 `Dst`).
- 두 갈래 (`+0x19d` call, ret `+0x1a2` 의 `Feature_CreateProcessSegmentHeapEnablement…` 결과로 갈림):

| 갈래 | 조건 | PEB `+0x20` 을 쓰는 줄 | 복사 방식 |
|---|---|---|---|
| 스위치 꺼짐 | 반환 0 → `+0x1a4 JZ` | **`+0x514 ~ +0x52b`** (`MOV [RAX+0x20],RDI`, 일반 저장) | `+0x41f` call (ret `+0x424`) `RtlCopyMemory` 로 먼저 복사 → 복사본 안에서 포인터 고침 |
| 스위치 켜짐 | 반환 ≠ 0 | **`+0x347` call (ret `+0x34c`) `RtlWriteULong64ToUser(PEB+0x20, Dst)`** (`+0x329 ~ +0x344` 에서 인자 준비) | 원본 첫 0x450 바이트를 스택에 백업(`+0x1bb ~ +0x227`) → **원본 쪽에서** 포인터 고침(`+0x22b ~ +0x314`) → `+0x324` call (ret `+0x329`) `RtlCopyToUser` → PEB 에 씀 → 백업 되돌림(`+0x34d ~ +0x3c3`) |

- 어느 갈래인지는 전역 `Feature_CreateProcessSegmentHeapEnablement__private_featureState` (rva `0xfc4110`) 값에 달렸다: 함수는 이 값의 비트 4 (`0x10`) 가 켜져 있으면 비트 0 을 결과로, 아니면 `IsEnabledFallback` 을 부른다 (`.asm` 의 `Feature_…NoInline` 파일 `+0x14 ~ +0x27`). **런타임 값이라 동적.** 이름이 "SegmentHeap" 이라서 뜻과 이 함수가 하는 일이 맞아 보이지 않는다. 이름 뜻은 모른다 `[공백]`.
- `PspSetupUserProcessAddressSpace` 쪽: `.asm` 전체에서 `0x2e0`(PEB 얻기)·PEB 쓰기가 없다. PEB `+0x20` 은 이 함수가 아니라 부른 함수 안에서 쓴다.
- 두 갈래 모두 PEB 에 쓰는 때는 **복사가 끝난 뒤**다. 쓰기 결과(상태)는 안 검사한다 (`+0x34c` 뒤에 `TEST` 없음).

**1-b. 원본은 어디서 오나: 커널 쪽 복사본 → 새 프로세스 메모리로 복사.**

- 원본(`Src`) = `[만들기 정보 구조 + 0xd8]` (`PspCopyAndFixupParameters +0x2a`, 구조 포인터는 3번째 인자 `R8`). 도착(`Dst`) = `[구조 + 0xe0]` (`+0x40`).
- `Src` 를 만드는 곳: `NtCreateUserProcess +0xac6` (ret `+0xacb`) `PspCaptureProcessParameters(모드, [RBP+0x60] = 유저가 준 9번째 인자, 구조)`. 사용자 모드 호출이면 `+0x3a` (ret `+0x3f`) 에서 `PspCaptureUserProcessParameters` 를 불러 **커널 풀에 새 블록을 만든다** (그쪽 호출 이름: `ExAllocatePool2` `+0x317`, `PspCaptureAndValidateUnicodeString` 여러 번, `PspCopyUnicodeString` 여러 번, `PspValidateEnvironmentBlock` `+0x5bb`, `RtlCopyMemory` 별칭 `+0x583`·`+0x696`). 커널 모드 호출이면 `+0x58 ~ +0x5a` 에서 받은 포인터를 그대로 쓴다.
  - 근거: `ms/disassemble/ntoskrnl/PspCaptureProcessParameters.asm`, `PspCaptureUserProcessParameters.asm` (머리만 읽음).
  - 부모 쪽(`BasepCreateProcessParameters`, run14 `[추측]`)이 이 9번째 인자를 만든다는 건 이번 코드로 확인한 게 아니다. 커널은 "유저가 준 포인터를 가져온다" 까지만 `[정적]`.
- `Dst` 를 잡는 곳: `PspSetupUserProcessAddressSpace +0x197` call (ret `+0x19c`) `ZwAllocateVirtualMemory(-1, &[구조+0xe0], 0, &크기, 0x3000, 4)`.
  - 프로세스 핸들 `-1` = 현재 프로세스 = 붙어 있는 새 프로세스. 주소 칸은 `+0x17f` 에서 0 으로 비우고 부른다 (주소는 시스템이 고름). 종류 `0x3000` (`MEM_COMMIT|MEM_RESERVE` 값), 보호 `4` (`PAGE_READWRITE` 값).
  - 크기: 원본 `MaximumLength` + `EnvironmentSize` (`+0x118 ~ +0x11f`), 스위치 켜짐이면 사용자 속성 목록 크기를 8바이트 올림해서 더함(`+0x124 ~ +0x155`). `Flags & 0x1000060` (비트 5·6·24) 이 모두 0 이고 크기가 `0x20000` 보다 작으면 **`0x20000` 으로 올린다** (`+0x15c ~ +0x16b`).
- 복사 바이트 수 = `MaximumLength` + `EnvironmentSize` (`PspCopyAndFixupParameters +0x36 ~ +0x39`, 크기 칸 이름은 PDB `_RTL_USER_PROCESS_PARAMETERS` +0x0, +0x3f0).

**1-c. "Fixup" 이 무엇인가: 안쪽 포인터를 새 주소로 고친다 (절대 주소). 오프셋으로 두지 않는다. 정규화 비트는 안 건드린다.**

- 고치는 공식: `칸 = Dst + (칸 − Src)`, 칸이 0 이면 건너뜀 (`ADD RAX,RSI` 에서 `RSI = Dst − Src`, `+0x19a`). 스위치 꺼짐은 `Dst` 쪽(`+0x424 ~ +0x50d`), 켜짐은 `Src` 쪽(`+0x22b ~ +0x314`)을 고친다.
- 고치는 칸 12개 (PDB `_RTL_USER_PROCESS_PARAMETERS`; 문자열 칸의 `Buffer` 는 `_UNICODE_STRING +0x8`):

| 오프셋 | 칸 | 오프셋 | 칸 |
|---|---|---|---|
| `+0x40` | `CurrentDirectory.DosPath.Buffer` | `+0xc8` | `DesktopInfo.Buffer` |
| `+0x58` | `DllPath.Buffer` | `+0xd8` | `ShellInfo.Buffer` |
| `+0x68` | `ImagePathName.Buffer` | `+0xe8` | `RuntimeData.Buffer` |
| `+0x78` | `CommandLine.Buffer` | `+0x418` | `RedirectionDllName.Buffer` |
| `+0x80` | `Environment` | `+0x428` | `HeapPartitionName.Buffer` |
| `+0xb8` | `WindowTitle.Buffer` | `+0x430` | `DefaultThreadpoolCpuSetMasks` |

  - 이 함수가 안 고치는 포인터 칸: `PackageDependencyData` (+0x400), `CurrentDirectores[32]` 안쪽. 왜인지는 모른다 `[공백]`.
  - 핸들 칸(`ConsoleHandle` +0x10, `StandardInput/Output/Error` +0x20/+0x28/+0x30)은 포인터가 아니라서 위 표에 없고, 아래 1-d 로 따로 복제한다.
- `Flags`(`+0x8`) 를 이 함수가 바꾸는 곳은 **한 줄뿐**: `+0x82 BTS [RBX+8],0xe` (켜는 조건: 구조 `+0x10` 비트 6 켜짐 **and** `+0x11` 비트 0 꺼짐, `+0x74 ~ +0x80`). 정규화 비트(비트 0)는 읽지도 쓰지도 않는다.
- 정규화는 한 단계 위에서 **조건으로 검사**한다: `PspCaptureUserProcessParameters +0x8f ~ +0xa8` 에서 `Flags` 의 비트 14 를 지운 뒤 `(Flags ^ 1) & 0xf4010e11 ≠ 0` 이면 `0xC000000D` 로 실패. 곧 **비트 0 이 켜져 있어야 하고**, 비트 4·9·10·11·16·26·28~31 은 꺼져 있어야 한다. (`[정적]` 마스크 값과 비교 줄. 각 비트 이름은 모른다.)
  - 그래서 커널 복사본은 이미 "포인터가 든 꼴"이고, 고치는 공식이 절대 포인터 기준인 것과 맞는다. ntdll 의 `LdrpInitializeProcess +0x6c0 RtlNormalizeProcessParams` (run18 표) 가 이미 정규화된 블록에서 무엇을 하는지는 ntdll 본문을 안 읽어 모른다 `[추측: 할 일 없음]`.

**1-d. 핸들 복제 두 줄 (`ObDuplicateObject`, 인자: 원본 프로세스 = 부모, 대상 = 새 프로세스, 옵션 `6` = 접근·속성 그대로 `[추측: 값 이름]`)**

| 줄 | 무엇 | 조건 |
|---|---|---|
| `+0xb7` call (ret **`+0xbc`**) | **`ConsoleHandle`** (`Src+0x10`) 를 새 프로세스용으로 복제해 `Src+0x10` 에 되쓴다 (`+0xda`) | 구조 `+0x58` 이 `3` (이미지 정보 `SubSystemType`, `+0x38` 에 `_SECTION_IMAGE_INFORMATION` 이 있음, 값 3 = 콘솔 `[추측: 값 이름]`) **and** 핸들 값 `> 0` (`+0x87 ~ +0x92`) |
| `+0x12b` call (ret **`+0x130`**) | **표준 입력·출력·오류 핸들** 3개를 한 칸씩 복제 (`Src+0x20`, `+0x28`, `+0x30`. 고리 `+0xf1 ~ +0x178`, 세 번 돎) | 복제할 핸들 목록이 있을 때만 (`+0xe4 TEST RSI,RSI`). 값이 음수면 복제 안 하고 `0xC0000008` 처리 |

- 목록(`RDX`)이 어디서 오나 (`PspSetupUserProcessAddressSpace +0x41 ~ +0x73`): 구조 `+0x11` 의 비트 2~3 값에 따라 — 1: 이미지 서브시스템과 구조 `+0x120` 이 같으면 `PspGetStandardHandleList` (call `+0x8d`, ret `+0x92`) 로 부모에게서 가져온 목록, 2: 매개변수 안 `StandardInput` 칸 자체 (`LEA R15,[RBP+0x20]`), 0·3: 목록 없음.
  - 2번일 때만 복제 실패가 **전체 실패**다 (`PspCopyAndFixupParameters +0x158 ~ +0x160`). 1번이면 실패해도 그 칸은 그냥 둔다.
  - `[추측]` 이 값은 공개 문서의 `PS_STD_HANDLE_INFO.StdHandleState` (0 복제 안 함, 1 요청 시 복제, 2 항상 복제) 와 모양이 같다. 구조 `+0x11` 에 값을 넣는 곳은 이번에 안 봤다 (`PspBuildCreateProcessContext`).

### 2. `PspPrepareSystemDllInitBlock` 이 만드는 것

**2-a. 어디에 쓰나: 새 프로세스 안 ntdll 의 `LdrSystemDllInitBlock`.** (`ms/disassemble/ntoskrnl/PspPrepareSystemDllInitBlock.asm`)

- 주소 얻는 방법 (인자 `ECX` = 어느 ntdll 이냐: 0 = 64비트 원래 ntdll, 1 = WoW64 용 32비트 ntdll):
  - `ECX == 0`: `+0x214 MOV RBX,[PsNtdllExports]` — `PsNtdllExports` 의 `+0x0` 칸 = PDB `_PS_NTDLL_EXPORTS.LdrSystemDllInitBlock` (sast/11 의 표). 부팅 때 채워진 값이라 `refs` 에 코드로 쓰는 줄은 없고 포인터 표(`PTR_DAT_140c80228+0xa0`)만 가리킨다.
  - `ECX == 1`: `+0x2dd` call (ret `+0x2e2`) `PspWow64GetSharedInformation(1)` → 돌려받은 표(`PsWowX86SharedInformation`)의 `+0x38` 칸을 읽어 `RBX` 로 (`+0x2e2`). 표의 7번째 칸 = PDB 열거형 `_WOW64_SHARED_INFORMATION` 의 `SharedNtdll32LdrSystemDllInitBlock = 7`.
- 현재 스레드가 새 프로세스에 붙어 있을 때만 의미가 있다: 함수 자체가 `GS:[0x188]+0xb8` 를 "새 프로세스"로 쓴다 (`+0x35 ~ +0x3e`). 부르는 쪽이 붙은 뒤에 부른다 (`PspSetupUserProcessAddressSpace +0xf5`, `PspAllocateProcess +0x2591 ~ +0x259b`).
- 크기 확인: `+0x98 ~ +0x9f` 에서 블록 맨 앞 4바이트(Size)를 읽어 `0x128` 이 아니면 `0xC0000059` 를 돌려주고 끝낸다 (`+0x1ee`). 값 이름은 `[추측]` REVISION_MISMATCH. 마지막으로 쓰는 칸이 `+0x120` 이라서 끝 `+0x128` 과 맞는다.

**2-b. 채우는 칸** (`RBX` = 블록. 쓰는 줄은 명령 위치, 호출 줄은 call/ret 둘 다)

PDB 에 이 블록 타입이 없어서 칸 이름은 **내가 기억하는 공개 비공식 헤더(`PS_SYSTEM_DLL_INIT_BLOCK`)** 이름을 후보로 단다 `[추측, 확인 못 함]`. 위치와 크기가 코드와 모두 맞는다.

| 블록 칸 | 쓰는 줄 | 값 출처 `[정적]` | 이름 후보 `[추측]` |
|---|---|---|---|
| `+0x0` (4B) | 쓰지 않음, **읽어서 0x128 확인** (`+0x98 ~ +0x9f`) | ntdll 이미지 쪽 처음 값 | Size |
| `+0x8` | `+0xda` | `PspSystemDlls[종류]` 기록의 `+0x38` (종류 = `PsWow64GetProcessNtdllType` 결과, 0 이면 1). `+0xc3` call (ret `+0xc8`) | SystemDllWowRelocation |
| `+0x10` | `+0xb0` | `PspNativeSystemDllData +0x38` (`+0xa5 ~ +0xac`) | SystemDllNativeRelocation |
| `+0x18 ~ +0x97` (8바이트 16개) | `+0xe1 ~ +0xf7` 고리, 쓰는 줄 `+0xef` | `PspWow64GetSharedInformation(종류)` 가 돌려준 표 16칸 복사. 종류 0·1 이면 전역 `PsWowX86SharedInformation` (함수 `+0x5` 에서 그 주소를 돌려줌, 다른 종류는 NULL) | Wow64SharedInformation[16] |
| `+0x98` | `+0x100` | **`ExGenRandom(1)` 반환값** (`+0xfb` call, ret `+0x100`) | RngData |
| `+0x9c` | `+0x106` (0), `+0x120` (`\|= 1`) | 두 번째 인자 `+0x11` 의 비트 1 이 켜져 있으면 비트 0 켬 (`+0x10c ~ +0x120`) | Flags |
| **`+0xa0 ~ +0xb7`** (24B) | **`+0x12b` (16B), `+0x138` (8B)** | 두 번째 인자 `+0x150 ~ +0x167` 복사 (`+0x64 ~ +0x78`). 그 칸들은 `PspAllocateProcess +0x1f3f ~ +0x1f50` 이 쓴다 (지역 `[RSP+0x400]` 24B, `+0x1eb3` call `PspApplyMitigationOptions` ret `+0x1eb8` 에 넘긴 칸) | MitigationOptionsMap |
| **`+0xb8`** | **`+0x15a`** | `R13` = `[[새 프로세스 +0x410] + 0x4a0]` (`+0x214 ~ +0x232`). `EPROCESS+0x410` = PDB `Vm.Instance.VmWorkingSetList`. **그 구조 `+0x4a0` 의 이름은 PDB 에 없다** | CfgBitMap |
| `+0xc0` | `+0x169` | 같은 구조 `+0x4a8` (`+0x239 ~ +0x240`) | CfgBitMapSize |
| `+0xc8`, `+0xd0` | `+0x178`, `+0x187` | **WoW64 프로세스**(`EPROCESS.WoW64Process` +0x310 ≠ 0, `EPROCESS.Machine` +0x6ec 이 `0x14c` 또는 `0x1c4`) 일 때만 같은 구조 `+0x4c0`·`+0x4c8`. 아니면 0 (`+0x248 ~ +0x2a1`) | Wow64CfgBitMap(Size) |
| `+0xd8 ~ +0xef` (24B) | `+0x145` (16B), `+0x152` (8B) | 두 번째 인자 `+0x1c0 ~ +0x1d7` 복사. 쓰는 곳 `PspAllocateProcess +0x1f61 ~ +0x1f72` (`[RSP+0x418]`) | MitigationAuditOptionsMap |
| `+0xf0 ~ +0x127` (7칸) | `+0x1a3 ~ +0x1e5` | **`PspGetScpCfgFunctions(새 프로세스)` 가 돌려준 표** 7칸 (`ECX == 0` 일 때만, `+0x18e`). 표 `+0x0, +0x8, +0x10, +0x18, +0x30, +0x20, +0x28` 순서로 복사 | ScpCfgCheck/Dispatch 류 함수 주소 7개 |

- `ECX == 1` (32비트 ntdll 블록) 일 때는: `+0xb8`·`+0xc0` 에 `+0x4c0`·`+0x4c8` 값이 들어가고(`+0x2fd ~ +0x30b`), `+0xc8`·`+0xd0` 는 0 이고, `+0xf0 ~` 는 안 채운다.
- 두 번째 인자(`RDX`) = **만들기 정보 구조** (`PspAllocateProcess` 가 `NtCreateUserProcess` 에서 받은 지역 구조). `PspAllocateProcess +0x25d7·+0x25f7` 은 이 인자를 `0` 으로 부른다 → 위 `+0xa0`·`+0xd8` 은 0 이 된다 (`+0x2b5 ~ +0x2d1`).
- `PspGetScpCfgFunctions` (`.asm`): 전역 `PspNtdllScpFunctions` (rva `0xfc7640`, 7칸) 의 **주소를 돌려준다.** 단 `ZwQueryVirtualMemory` (call `+0x51`, ret `+0x56`) 가 `0xC00000BB` 이거나 결과가 0 이거나, `PsGetScpCfgPageTypeForProcess(…, 1)` (call `+0x85`, ret `+0x8a`) 가 3 또는 4 이면 NULL 을 돌려준다 (그러면 `+0xf0 ~` 는 안 채워진다).
  - 그 표 칸을 쓰는 곳: 부팅 때 쓰는 INIT 함수 `PspLocateNtdllAddressesForScpCfg` 의 `+0x1bf`·`+0x1d6`·`+0x1ec`·`+0x202` (표 `+0x0`·`+0x8`·`+0x10`·`+0x18`). `+0x20 ~ +0x30` 은 쓰는 줄이 안 나온다 (`refs` 한계일 수 있음). 값이 ntdll 안 함수 주소인지는 이번엔 안 봤다 `[추측]` (CFG 검사·분배 함수일 것).
  - "SCP" 의 뜻은 모른다 `[공백]`.

**2-c. run18 `+0xa0` · run28 `+0xb8` 을 여기서 채우나: 예, 둘 다.**
- `+0xa0`: `PspPrepareSystemDllInitBlock +0x12b ~ +0x138` (위 표). 값은 `PspApplyMitigationOptions` 쪽에서 나온 24바이트. run18 이 "무결성 설정 비트"라고 부른 칸의 커널 쪽 출처가 이것이다 (이름 후보 "완화 옵션"은 `[추측]`).
- `+0xb8`: `+0x15a`. 값은 새 프로세스 작업 집합 구조에서 읽은 주소. CFG 비트맵을 만드는 길은 지도에서 `MmInitializeProcessAddressSpace +0x1c9 → MiMapProcessExecutable +0xe3 → MiCfgInitializeProcess` 이고 `PspAllocateProcess +0x2090` (ret `+0x2095`) `MmInitializeProcessAddressSpace` 가 `+0x239c` 보다 앞이라 시간 순서는 맞다. `MiCfgInitializeProcess` 가 `+0x4a0` 칸을 쓰는지는 이번엔 안 봤다 (새 공백).

**2-d. 호출 자리 넷의 차이 (test.exe 는 어디를 타나, 정적 조건)**

| 자리 (call → ret) | 인자 | 서는 조건 |
|---|---|---|
| **`PspSetupUserProcessAddressSpace +0x1e1` → `+0x1e6`** | `ECX=0`, `RDX=구조` | 새 프로세스 `Pcb.SecureState` 의 `SecureProcess`(`+0x170` 비트 0, PDB) 가 **꺼져 있을 때** (`+0x1c3 ~ +0x1da`). 켜져 있으면 건너뜀 (`+0x1da JNZ`). |
| `PspSetupUserProcessAddressSpace +0x214` → `+0x219` | `ECX=PsWow64GetProcessNtdllType` (call `+0x20a`, ret `+0x20f`), `RDX=구조` | `EPROCESS.WoW64Process` (+0x310) ≠ 0 (`+0x1fd ~ +0x205`) → **32비트 exe 일 때만.** |
| `PspAllocateProcess +0x25d7` → `+0x25dc` | `ECX=0`, `RDX=0` | `PspSetupUserProcessAddressSpace` 를 **안 부른 갈래** (아래). `+0x2574` 에서 "묶음 플래그" 비트 8 켜짐 and 비트 4 꺼짐 → 붙고(`+0x2596` call, ret `+0x259b`) `MmMapApiSetView` (ret `+0x25bf`) → `PspMapSiloSharedDataView` (ret `+0x25cd`) → 이 호출 |
| `PspAllocateProcess +0x25f7` → `+0x25fc` | `ECX=종류`, `RDX=0` | 위와 같은 갈래 + `WoW64Process ≠ 0` (`+0x25e2`) |

- **`PspAllocateProcess` 의 갈림길** (`+0x236c ~ +0x2397`): "묶음 플래그"(스택 `[RSP+0x84]`, 아래 `PF`) 의 **비트 7 이 켜져 있고** (`TEST BL,BL; JNS`), 11번째 인자(만들기 정보 구조)가 NULL 이 아니고, `PF` 비트 1 이 꺼져 있으면 `+0x2397` call (ret `+0x239c`) `PspSetupUserProcessAddressSpace`. 하나라도 아니면 `+0x2574` 로 가서 위 `+0x25d7` 갈래.
  - `PF` 의 낮은 비트는 `PspAllocateProcess +0x2c8 ~ +0x31f` 에서 정한다: 부모 있음 + 섹션 없음 → 4, 부모 없음 + 섹션 없음 → 1, 인자 9 의 비트 11 켜짐 → 2 (비트 13 → 6, 비트 14 → 0xe), **섹션 있음 + 비트 11 꺼짐 → 0**. 비트 7 은 `+0x20d2 BTS EBX,7` 이 켠다 (`MmInitializeProcessAddressSpace` 성공 뒤, `PF` 비트 0·2 가 모두 꺼졌을 때만 → 섹션 있는 일반 실행). 비트 8 은 `PF` 비트 0·1 이 꺼졌을 때 켠다 (decomp `LAB_140a04729` 뒤).
  - **test.exe 쪽 정적 추정**: `NtCreateUserProcess +0xbe6` 가 11번째 인자로 지역 구조(`RBP+0x1a0`)를 넘기므로 NULL 이 아니다. 7번째 인자 `[RBP+0x270]` 은 섹션(`+0x978` 에서 저장, `MmCreateSpecialImageSection` ret `+0x93c` 뒤). 따라서 "섹션 있음 · 최소/복제 플래그 없음 → `PF` 낮은 비트 0 → 비트 7 켜짐" 이면 **`PspSetupUserProcessAddressSpace` 갈래**, 즉 **`+0x1e6` 한 곳**이다. `+0x25dc`·`+0x25fc`·`+0x219` 는 안 선다 (64비트라서). **실제 `PF` 값과 인자 9 의 비트 11 은 런타임이라 동적으로 확인.**
  - 반대 갈래(`+0x25d7`)가 뜻하는 것: 만들기 정보 구조가 없는 호출(`PspCreateProcess` 같은 예전 방식) 이거나 `PF` 비트 1 이 켜진 최소/복제 프로세스. 이때는 **매개변수가 복사되지 않아 PEB `+0x20` 은 0 으로 남는다** (`PspCopyAndFixupParameters` 를 안 부르므로).

### 3. 순서

**답: run10 순서가 맞다. 사이에 몇 개 더 끼어 있다.** `PspSetupUserProcessAddressSpace` (`.asm`, call/ret):

| 순서 | call | ret | 함수 | 비고 |
|---|---|---|---|---|
| 0 | `+0x8d` | `+0x92` | `PspGetStandardHandleList` | 표준 핸들 목록이 "1" 일 때만 |
| 0' | — | — | (`Flags` 에 비트 켜기, `LoaderThreads` 기본값) | `+0xa0 ~ +0xea`, **붙기 전에 커널 복사본에** 한다 (아래) |
| 1 | `+0xf5` | `+0xfa` | `KiStackAttachProcess` | 새 프로세스로 붙음 |
| 1' | `+0x106` | `+0x10b` | `PspLocateInPEManifest` | 구조 `+0x10` 비트 4 일 때 |
| 2 | `+0x197` | **`+0x19c`** | `ZwAllocateVirtualMemory` | 매개변수 블록 자리 |
| 3 | `+0x1b4` | **`+0x1b9`** | `PspCopyAndFixupParameters` | **PEB `+0x20` 이 여기서 채워짐** |
| 4 | `+0x1e1` | **`+0x1e6`** | `PspPrepareSystemDllInitBlock` (64비트 ntdll) | 보안 프로세스면 건너뜀 |
| 4' | `+0x1f2` | `+0x1f7` | `PspWow64SetupUserProcessAddressSpace` | |
| 4'' | `+0x214` | `+0x219` | `PspPrepareSystemDllInitBlock` (32비트 ntdll) | WoW64 일 때만 |
| 5 | `+0x222` | `+0x227` | `KeCopyXfdMaskToPeb` | |
| 5' | `+0x22f` | `+0x234` | `PspUpdatePebForAffinityChange` | |
| 6 | `+0x250` | **`+0x255`** | `MmMapApiSetView` | 보안 프로세스면 건너뜀 |
| 7 | `+0x25e` | **`+0x263`** | `PspMapSiloSharedDataView` | |
| 8 | `+0x26a` | `+0x26f` | `KiUnstackDetachProcess` | 떼어냄 |

- 그러니 **할당 → 매개변수 복사(+PEB `+0x20`) → 시작 정보 블록 → ApiSet 뷰** 순서는 맞다. PEB `+0x20` 을 쓰는 줄은 3번 안(`PspCopyAndFixupParameters +0x52b` 또는 `+0x347`, ret `+0x34c`)이고, 호출한 쪽에서 보면 `+0x1b4` ~ `+0x1b9` 사이다. 4번(블록 채우기)보다 **먼저**다.
- 0' 단계(붙기 전, 복사 전)에서 커널 복사본의 `Flags`·`LoaderThreads` 를 고친다 → 아래 질문 4 표. 이 고쳐진 값이 3번에서 새 프로세스로 복사된다.
- 블록(질문 2) 채우기는 PEB 와 따로다. 블록은 ntdll 이미지 안 변수이고 `PsNtdllExports` 로 찾는다. 3·4번은 서로의 결과를 읽지 않는다 (순서는 코드 순서일 뿐).

### 4. ntdll 과 짝 (이번에 채운 칸 ↔ ntdll 이 읽는 줄)

ntdll 쪽은 **문서에 이미 적힌 것만** 짝지었다 (새로 안 읽음). 출처는 `runs/02_sprint/flow.md`, `_workspace/sast/13`·`23`.

| 칸 | 커널이 쓰는 줄 `[정적]` | ntdll 이 읽는 줄 (문서) | 짝 판정 |
|---|---|---|---|
| **PEB `+0x20`** (`ProcessParameters`) | `PspCopyAndFixupParameters +0x52b` 또는 `+0x347` (ret `+0x34c`) | 문서에 적힌 첫 자리: `LdrpInitialize` (rva `0xe1448`) 가 `ProcessParameters.Flags` 맨 윗 비트를 보고 `LdrpIsSecureProcess = 1` (`+0x5a`, run17). `LdrpInitializeProcess` 는 `+0x6c0 RtlNormalizeProcessParams` (ret `+0x6c5`), `+0xc3a RtlpInitParameterBlock` (ret `+0xc3f`) (run18 묶음 5·8) | **짝 맞음.** run17 의 "누가 채우는지 모름"이 풀렸다. |
| `Flags` 비트 29 (`0x20000000`) | **`PspSetupUserProcessAddressSpace +0xe6 BTS EBX,0x1d` → `+0xea` 저장.** 조건: `PsGetProcessServerSilo` (call `+0xd0`, ret `+0xd5`) → `PsGetServerSiloGlobals` (call `+0xd8`, ret `+0xdd`) → `[결과 + 0x3f8]` 바이트 ≠ 0 (PDB `_ESERVERSILO_GLOBALS.OneCoreForwardersEnabled`) | `LdrpInitializeKernel32Functions` 가 "찾음" 쪽 길에서 이 비트가 켜져 있으면 (4)~(6) 건너뜀 (`+0x1ff` 근처, run20) | **짝 맞음, 이름 단서.** 비트는 "OneCore forwarders 켜짐" 상태를 서버 silo 전역에서 복사한 것으로 보인다 `[추측: 이름]`. 입력 검사 마스크에도 들어 있어(`0xf4010e11`) 사용자가 주는 값이 아니다. |
| `Flags` 비트 15 (`0x8000`) | `PspSetupUserProcessAddressSpace +0xb1 BTS EBX,0xf` → `+0xb5`. 조건 `PspGlobalFlags` 비트 0 (`+0xa0 ~ +0xaf`) | 문서에 읽는 줄 없음 | **빈 곳** (ntdll 쪽) |
| `Flags` 비트 14 (`0x4000`) | `PspCopyAndFixupParameters +0x82` | 문서에 읽는 줄 없음 | **빈 곳** |
| `LoaderThreads` (+0x40c) | `PspSetupUserProcessAddressSpace +0xb8 ~ +0xc7`: 0 이면 전역 `PsDefaultLoaderThreads` 로 채움 (`+0xc1` 읽기, `+0xc7` 쓰기) | `LdrpInitializeProcess +0x1a42 LdrpEnableParallelLoading` 의 `ECX` (run19). 같은 칸에 IFEO `MaxLoaderThreads` 를 쓰는 건 `LdrpInitializeExecutionOptions` (ret `+0x536`, run18) | **짝 맞음.** 커널이 먼저 기본값을 깔고, ntdll 이 IFEO 로 덮을 수 있다. |
| **블록 `+0xa0`** (24B) | `PspPrepareSystemDllInitBlock +0x12b` (16B), `+0x138` (8B) | `LdrpInitializeProcess +0x4a8 ~ +0x579` 구간 (run18, "무결성 설정 비트 두 개 읽기"). 정확한 읽는 줄 번호는 문서에 없음 | **짝 맞음 (구간).** 비트 두 개의 정확한 자리는 빈 곳 |
| **블록 `+0xb8`** | `PspPrepareSystemDllInitBlock +0x15a` | `LdrpDispatchUserCallTarget +0x0 ~ +0x2f` (run28) | **짝 맞음.** 값 = 비트맵 주소(커널 쪽 이름은 PDB 에 없음), `ntdll` 은 비트맵으로 검사 |
| 블록 `+0xc0` (+ `+0xc8`·`+0xd0`) | `+0x169` (+ `+0x178`·`+0x187`) | 문서에 없음 | **빈 곳** |
| 블록 `+0x8`·`+0x10`·`+0x18 ~ +0x97`·`+0x98`·`+0x9c`·`+0xd8 ~ +0xef`·`+0xf0 ~ +0x127` | 위 2-b 표 | 문서에 읽는 줄 없음. 이름만 근거로 한 후보: `+0x98` ↔ run18 묶음 1 의 `LdrInitializePerThreadRng` (난수 준비) `[추측, 이름만]` | **빈 곳** (ntdll 새로 읽기 필요) |

## flow.md 판정

- **2.1 `[추측]` 첫 줄** ("`PspCopyAndFixupParameters`가 프로세스 매개변수(명령줄 등)를 새 프로세스로 복사하고 PEB `+0x20`에 잇는 곳") → **맞다. `[정적]` 로 올릴 수 있다.** 덧붙일 것: 원본은 `PspCaptureProcessParameters` 가 만든 커널 복사본, 안쪽 포인터 12개를 절대 주소로 고침, 쓰는 줄 `+0x52b` / `+0x347`(ret `+0x34c`), 핸들(콘솔·표준 입출력)도 같이 복제.
- **2.1 `[추측]` 둘째 줄** ("`PspPrepareSystemDllInitBlock`은 ntdll 에 넘길 시작 정보 묶음을 준비한다") → **맞다. `[정적]`.** 정확히는 새 프로세스 안 ntdll 의 `LdrSystemDllInitBlock` (0x128 바이트) 에 직접 쓴다.
- **4장 2.1·2.4 첫 줄** (PEB `+0x20` 누가 쓰나, 후보 `PspSetupUserProcessAddressSpace +0x1b9`) → **해결.** `+0x1b9` 로 돌아오기 전에 `PspCopyAndFixupParameters` 안에서 쓴다.
- **4장 2.1 둘째 줄** (`PspPrepareSystemDllInitBlock` 안쪽) → **해결.** "2.3 공백과 이어질 수 있음" 은 이번 코드에선 이어지는 줄을 못 찾았다 (`LdrInitializeThunk` 로 넘기는 값은 여기가 아니라 `PspNativeSystemDllData +0x20`, run18).
- 4장 2.4 의 "`LdrSystemDllInitBlock+0xa0` 의 무결성 설정 비트 두 개" 줄 → **한 칸 진전**: 커널 쪽 출처가 24바이트 완화 옵션이라는 것까지. 비트 두 개의 뜻은 여전히 모름.
- 4장 2.5 의 "`ProcessParameters.Flags` `0x20000000` 비트 이름" 줄 → **커널이 켜는 줄을 찾음** (`PspSetupUserProcessAddressSpace +0xe6`). 이름 단서: 서버 silo 의 `OneCoreForwardersEnabled`.
- `flow.md` 3176 줄 (동적 표, "`+0x239c`에서 PEB `+0x20`이 채워졌는지"): 정적으로는 **채워진다** (위 2-d 의 test.exe 갈래라면). 실제 값은 동적 (Q35).

## 그림 1 상자 후보 (대표 지점만, 둘)

1. `nt!PspCopyAndFixupParameters` `(정적)` — "프로세스 매개변수를 새 프로세스 메모리로 복사 · 안쪽 포인터 12개 고침 · 핸들 복제 · PEB `+0x20` 쓰기". 위치: 그림 1 의 `p8` (`PspSetupUserProcessAddressSpace`) 안쪽, call `+0x1b4` / ret `+0x1b9`.
2. `nt!PspPrepareSystemDllInitBlock` `(정적)` — "새 프로세스 안 ntdll `LdrSystemDllInitBlock` 채우기 (난수·완화 옵션·CFG 비트맵 주소·SCP 함수 7개)". 위치: 그 바로 다음, call `+0x1e1` / ret `+0x1e6`.
- 둘은 `p8` 안 두 줄로 합쳐도 된다. flow-writer 판단.

## 동적분석으로 확인해야 할 것

정적으로 결론 못 낸 것:

1. **기능 스위치 값** (`nt+0xfc4110`): 비트 4·비트 0 → `PspCopyAndFixupParameters` 가 `+0x52b` 로 가는가 `+0x347` 로 가는가, 사용자 속성 목록(`+0x448`) 복사가 있나.
2. **`PF` (묶음 플래그) 와 갈래**: test.exe 가 `PspSetupUserProcessAddressSpace +0x1e6` 로 서는가 (`+0x25dc`·`+0x25fc`·`+0x219` 는 안 서야 한다).
3. **새 PEB `+0x20` 값과 매개변수 블록**: 블록 `Flags` 의 `0x1`·`0x4000`·`0x8000`·`0x20000000` 비트, `LoaderThreads`, 문자열 `Buffer` 가 모두 `Dst` 안(새 프로세스 안 같은 영역)을 가리키는가. → **Q35 와 한 번 실행에 합치면 된다** (Q35 는 `PspAllocateProcess +0x239c` 에서 PEB `+0x20` 값을 이미 묻는다).
4. **`LdrSystemDllInitBlock` 채운 뒤 값**: `+0x98` 난수 ≠ 0, `+0xa0` 24B, `+0xb8`/`+0xc0` (비트맵 주소·크기), `+0xf0 ~` 함수 주소 네 칸이 ntdll 안 어떤 함수인지 (`ln`).
5. 디버거가 붙어서 달라질 점은 이번 질문에선 안 보인다.

추천 멈춤점 (`nt` = ntoskrnl 시작 주소. 붙어 있는 동안이라 CR3 는 새 프로세스지만 WinDbg 가 읽는 문맥은 다를 수 있으니 `.process /r /p <새 EPROCESS>` 를 먼저 `[추측]`; 새 EPROCESS 는 `PspSetupUserProcessAddressSpace` 안에서 `RDI`):

| 멈춤점 | 왜 |
|---|---|
| `dd nt+0xfc4110 L1` (멈춤 아님) | 1번 |
| `nt+0xae57e4` (`PspCopyAndFixupParameters` 입구, test.exe 일 때만) | 인자: `rcx`=부모, `rdx`=핸들 목록, `r8`=구조 → `dq @r8+0xd8 L2` 가 `Src`, `Dst` |
| `nt+0xae5eed` (= `PspSetupUserProcessAddressSpace +0x1b9`, 복사 끝난 직후) | 3번. `Dst` 의 `Flags` 등과 PEB `+0x20` 비교 |
| `nt+0x9395d0` (`PspPrepareSystemDllInitBlock` 입구) | `ecx`, `rdx`, `[rsp]` = 돌아갈 자리: 기대 `nt+0xae5f1a` (2번) |
| `nt+0xae5f1a` (= `PspSetupUserProcessAddressSpace +0x1e6`) | `dq poi(nt+0xfc74b0) L0x25` 로 블록 전체 (4번) |
| 안 서야 하는 자리 | `nt+0xa04b44` (`PspAllocateProcess +0x25dc`), `nt+0xa04b64` (`+0x25fc`), `nt+0xae5f4d` (`+0x219`) |

## 새로 생긴 공백 (대기열 제안, 번호는 오케스트레이터가)

- (동적, Q35 에 합치기) 위 동적 1 ~ 4번.
- (정적, 작게) `PspCaptureUserProcessParameters` 본문 중 `Flags` 마스크 `0xf4010e11` 각 비트와 `Environment` 배치 (패킹) — 이번엔 머리만 읽음.
- (정적, 작게) `MiCfgInitializeProcess` 가 작업 집합 구조 `+0x4a0`·`+0x4a8` 를 쓰는지, `PspLocateNtdllAddressesForScpCfg` 가 `PspNtdllScpFunctions` 에 넣는 ntdll 함수 이름.
- (ntdll 새로 읽기 — 사용자 결정 필요) 블록의 문서 없는 칸(`+0x8`·`+0x10`·`+0x98`·`+0x9c`·`+0xc0`·`+0xd8`·`+0xf0 ~`)을 ntdll 이 읽는 줄, `RtlNormalizeProcessParams` 의 정규화 비트 처리.
- (정적, 작게) `PspBuildCreateProcessContext` 가 만들기 정보 구조 `+0x11` (표준 핸들 상태) 를 채우는 줄.

## 판정

**분석성공.** 질문 1~4 모두 `.asm` 줄 근거로 답했다. 이름이 PDB 에 없는 곳(`LdrSystemDllInitBlock` 칸 이름, 작업 집합 구조 `+0x4a0`, 기능 스위치 이름의 뜻)은 `[추측]`·`[공백]` 으로 구분했다. 갈림길(기능 스위치, 묶음 플래그)은 런타임 값이라 동적으로 넘겼다.

## 메모 (이번에 만든 임시 파일)

- 임시 스크립트·덤프는 세션 임시 폴더에만 있다 (`.claude/` 는 건드리지 않음). 구조체 칸은 PDB `llvm-pdbutil-14 dump -types` 로, 공개 심볼 위치는 `dump --publics` 로 읽었다.
- `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/` 에 위 9개 함수의 `.c`·`.asm` 이 추가되었고 `_calls.tsv` 에 줄이 덧붙었다 (커밋 대상 아님).

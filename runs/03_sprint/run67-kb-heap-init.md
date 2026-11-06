# 2026-11-06 run67 — kernel32·kernelbase 시작 함수가 놓는 힙 칸·명령줄 ANSI 사본·매니페스트 탐침 칸 (Q128, PE_emulator 요청 16 의 급한 셋 + 4번): **`BaseHeap` = `PEB.ProcessHeap` (PEB `+0x30`) 그대로**, `GetCommandLineA` 는 변환이 실패하면 빈 글이 아니라 **NULL**, 그리고 **kernel32 시작 함수가 exe import 올리기보다 앞이라 exe·dll1·dll2 때 탐침 칸 = `kernel32+0x23f90`**

## PE_emulator 쪽에서 쓸 것

### 한 줄 결론 (셋)

1. **`BaseHeap` = `PEB.ProcessHeap` (PEB `+0x30`) 그대로.** 새 힙을 만들지 않는다.
   - 근거 `[정적]` (`ms/disassemble/kernelbase/KernelBaseDllInitializeMemoryManager.asm`): `+0x6 MOV RAX,GS:[0x60]` (PEB) → `+0x17 MOV RCX,[RAX+0x30]` (`ProcessHeap`) → `+0x1b MOV [BaseHeap],RCX`. 조건 없음.
   - 같은 함수가 `SystemRangeStart` (= `0xFFFF800000000000`) 와 핸들 표 `BaseHeapHandleTable` 도 채운다. syscall 은 `NtQuerySystemInformation(0x32)` 하나뿐 (`SystemRangeStart` 가 0 일 때만 — 처음엔 0 이라 탄다).
   - 그래서 막힌 길 `GlobalAlloc +0x7a` (ret `+0x81`) `RtlAllocateHeap(BaseHeap, 플래그, 크기)` 의 첫 인자 = 프로세스 힙. 플래그는 `0x100000 | 옵션` (`KernelBaseGlobalData+0` 이 0 이라서 — 아래 1번).
2. **kernel32 `_BaseDllInitialize` (DllMain) 는 dll1·dll2 의 `LdrpFindDllActivationContext` 보다 앞이다.** 그래서 진짜는 **exe·dll1·dll2 를 올릴 때 `LdrpManifestProberRoutine` = `kernel32+0x23f90`** (`BasepProbeForDllManifest`) 이고, 그 함수를 부른다.
   - PE_emulator 의 0 은 **kernel32·kernelbase 를 올릴 때만** 맞다 (그땐 DllMain 전이라 진짜도 0).
   - 근거 `[정적]`: kernel32 DllMain 은 `LdrpInitializeProcess +0x199d` (ret `+0x19a2`) `LdrpInitializeKernel32Functions` 안에서 돌고, exe import 올리기는 그 뒤 `+0x1aa4` (ret `+0x1aa9`) `LdrpMapAndSnapDependency` 다. `[관찰]` run44 사진: 탐침 칸 P1a 0 → P1b `kernel32+0x23f90`.
   - **요청서 4(b) 의 "뒤라면 PE_emulator 의 0 이 맞다" 쪽 짐작은 틀렸다.** 앞이다.
   - 다행히 sample 셋 (test.exe·dll1·dll2) 은 PE 자원 디렉터리가 RVA 0 / 크기 0 이라 매니페스트가 없다 → 진짜도 `CreateActCtxW` 는 안 타고, `LdrpFindDllActivationContext` 는 0 을 돌려주며 모듈 항목 `+0x88` 을 안 쓴다 `[추측: 실행이 성공한다는 사실에서]`. 결과 칸은 같지만 **부르는 함수 하나가 빠지는 것**이다.
3. **`RtlUnicodeStringToAnsiString` 이 실패하면 `GetCommandLineA` 는 빈 글이 아니라 NULL 을 준다.**
   - 근거 `[정적]`: `_KernelBaseBaseDllInitialize +0x49e JS` → `+0x6b0` `BaseAnsiCommandLine.Buffer` 를 0 으로, `+0x6b7` `Length|MaximumLength` 를 0 으로 → `+0x6bd JMP +0x4a4` (DllMain 은 실패 안 함). `GetCommandLineA +0x0 MOV RAX,[BaseAnsiCommandLine+0x8]` → `+0x7 RET` 이라 NULL 그대로.
   - 성공하면 (test.exe, ACP 949): `BaseAnsiCommandLine = {Length 8, MaximumLength 9, Buffer → 프로세스 힙 새 9바이트 "test.exe\0"}`.

### 작은 표 (결과서 표 그대로, rva 는 모듈 기준)

| 함수 | 인자 | 쓰는 칸 | 값 출처 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|---|
| kernelbase `KernelBaseDllInitializeMemoryManager` (`0xf8344`, 155B) | 없음 | `BaseHeap` (`0x3aefa0`) · `SystemRangeStart` (`0x3b0350`) · `BaseHeapHandleTable` (`0x3aefc0`, `0x30`B) · `KernelBaseGlobalData+0x40/+0x48/+0x50` (`0x3aef00/08/10`) | `PEB.ProcessHeap` · 상수 `0xFFFF800000000000` · `{0xffff, 0x10, 0 …}` · 앞 셋의 사본 | `NtQuerySystemInformation(0x32)` (`SystemRangeStart == 0` 일 때) · `RtlInitializeHandleTable` | 첫 호출만 | 없음 |
| kernelbase `entry` (= `KernelBaseDllInitialize`) `+0xd ~ +0x3f` | — | `SystemRangeStart` · `BaseHeap` · `BaseHeapHandleTablePtr` (`0x3b0348`) | `KernelBaseGlobalData+0x50` / `+0x40` / `+0x48` | — | — | — |
| kernel32 메모리 관리 사본 함수 (`0x2c9d8`) | 없음 | kernel32 `BaseHeap` (`0xc2240`) · `SystemRangeStart` (`0xc2238`, 0 일 때만) | `KernelBaseGlobalData+0x40` / `+0x50` | `KernelBaseGetGlobalData` | 없음 | 없음 |
| kernelbase `_KernelBaseBaseDllInitialize +0x1cf` 반환 → `KernelBaseGlobalData+0` (`0x3aeec0`, 4B) | — | 4B | `RtlCreateTagHeap` = 0 (`NtGlobalFlag` 에 `0x800` 없음) | — | — | — |
| ntdll `RtlUnicodeStringToAnsiString` (`0x4b9f0`, 807B) | `RCX = &ANSI`, `RDX = &UNICODE`, `R8B = 1` | 출력 `Length` (+0) · `MaximumLength` (+2) · `Buffer` (+8) | 입력 길이 · ACP 949 표 (`GlobalRtlNlsState`) · 프로세스 힙 새 블록 | 문자열 버퍼 할당 함수 (= `RtlAllocateHeap(ProcessHeap, 0, 크기+1)`) · (UTF-8 일 때만 `RtlUnicodeToUTF8N`) | 없음 | `0` · `0x80000005` · `0xC0000017` · `0xC00000F0` |
| 같은 호출의 결과 (test.exe) | | `BaseAnsiCommandLine` (`0x3af050`) = `{8, 9, → 힙 9B "test.exe\0"}` · `BaseUnicodeCommandLine` (`0x3af040`) = `{0x10, 0x12, 0x25996f3813e}` (매개변수 안 버퍼를 같이 씀) | | | | |
| ntdll `LdrSetDllManifestProber` (`0x10c7e0`, 22B) | `RCX, RDX, R8` | `LdrpManifestProberRoutine` (`0x1d23c0`) · `LdrpCreateActCtxLanguageW` (`0x1d23a0`) · `LdrpReleaseActCtxW` (`0x1d23a8`) | `kernel32+0x23f90` · `kernel32+0x61520` · `kernelbase+0xefcd0` | 없음 | 없음 | 없음 |
| ntdll `LdrpFindDllActivationContext` (`0x96000`, 495B) | `RCX = 모듈 항목` | 항목 `+0x88` (`EntryPointActivationContext`, 탐침이 핸들을 줄 때만) | 탐침 반환 | 탐침 (CFG 간접) · `RtlReleaseActivationContext` · `LdrpLogInternal` | 없음 | 0 또는 음수 상태 |

- 문자열 버퍼 할당 함수 = ntdll rva `0x21a90`, 몸통 `+0x0 ~ +0x12` 가 `RtlAllocateHeap(PEB.ProcessHeap, 0, 크기)` 로 꼬리 점프. 같은 주소 이름 3개, 후보: `NtdllpAllocateStringRoutine`, `RtlpCtAllocateMemory`, `RtlpAllocateAtom`.
- kernel32 메모리 관리 사본 함수 = kernel32 rva `0x2c9d8`. 같은 주소 이름 2개, 후보: `BaseDllInitializeMemoryManager2`, `BaseDllInitializeMemoryManager`.

오프셋 읽는 법 (run60 ~ run66 과 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값.

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: KernelBase pdb GUID `72CCE506944DE0267C172AD64287F9BD1`, ntdll `C3093720177DA6851519DCFFE8DF53FE1`, kernel32 `AE9E9D8F7AC1467A645320FDC23235D01`, ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` (읽기만 — `ExpQuerySystemInformation.asm`). 넷 다 기준 GUID 와 같다. 새로 analyze 안 함.
- 값 대조: run44 사진의 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 를 다시 읽었다 (결과서 + flow-writer). 새 동적 실행은 없다.
  - **kernelbase·kernel32 `.data` 는 사진에 없다** (run44 는 ntdll `.data` 만 읽음). 그래서 `BaseHeap` 칸 값 자체는 못 봤고, 출처인 `PEB.ProcessHeap` 값만 봤다.

## 목적

- PE_emulator 는 kernelbase·kernel32 시작 함수를 진짜로 돌리지 않고 흉내 낸다. 본문 asm 이 없던 자리 셋 때문에 막혔다:
  - (a) 가장 급함: 은퇴 뒤 `OutputDebugStringA → CreateDBWinMutex → GlobalAlloc → RtlAllocateHeap(BaseHeap = 0)` 에서 멈춤.
  - (b) `BaseAnsiCommandLine` 이 비어 `GetCommandLineA` 가 빈 값을 줄 수 있음 `[추측]`.
  - (c) `LdrSetDllManifestProber` 가 공사 중이라 탐침 칸이 0 → exe import 길이 진짜와 갈릴 수 있음 `[추측]`.
- 알고 싶은 것: (1) `KernelBaseDllInitializeMemoryManager` 가 쓰는 칸과 값 출처, (2) `KernelBaseGlobalData +0x40 ~ +0x57` 을 쓰는 곳, (3) 명령줄 ANSI 사본을 만드는 `RtlUnicodeStringToAnsiString` 갈래, (4) `LdrSetDllManifestProber` 가 쓰는 칸, kernel32 DllMain 과 exe import 의 시간 순서, `LdrpFindDllActivationContext` 가 탐침 반환을 쓰는 법.

## 방법

- 정적: `_workspace/sast/62_kb-heap-init.md` — kernelbase `KernelBaseDllInitializeMemoryManager` depth 1, ntdll `RtlUnicodeStringToAnsiString` depth 1 (→ 문자열 버퍼 할당 함수·`RtlUnicodeToUTF8N`), `LdrSetDllManifestProber`, `LdrpFindDllActivationContext` depth 1 (→ `LdrResFindResourceDirectory`·`RtlRunOnceExecuteOnce`·`RtlReleaseActivationContext`·`LdrpLogInternal`).
  - 범위를 조금 넓힘 (결과서, 모두 depth 0): kernelbase `GlobalAlloc`·`GetCommandLineA` (멈춘 길의 바로 다음 소비자), kernel32 메모리 관리 사본 함수 (`KernelBaseGlobalData+0x40` 을 읽는 곳), kernel32 `BasepProbeForDllManifest` (4(b) 가 "앞" 이라 요청서 약속대로 한 줄), ntdll `RtlInitializeHandleTable`. 탐침 상태값을 보려고 `LdrResSearchResource`·`LdrpResSearchResourceMappedFile`·`LdrpResGetResourceDirectory` 는 열어만 보고 멈춤.
  - 새 `_refs`: kernelbase `KernelBaseGlobalData`·`SystemRangeStart`·`BaseHeapHandleTablePtr`, ntdll `GlobalRtlNlsState`·`Utf8TableInfo`·`LdrpManifestProberRoutine`·`LdrpCreateActCtxLanguageW`·`LdrpReleaseActCtxW`.
- 결과서: `_workspace/sast/62_kb-heap-init_result.md` (**분석성공**).
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `KernelBaseDllInitializeMemoryManager +0x6` · `+0xf` · `+0x17` · `+0x1b` · `+0x22`, `LdrpInitializeProcess +0x199d/+0x19a2` · `+0x1aa4/+0x1aa9` · `+0x1bb6/+0x1bbb`, `LdrSetDllManifestProber` 22B (`+0x15 RET`).
- flow-writer 가 `.asm` 으로 한 번 더 본 줄:
  - `KernelBaseDllInitializeMemoryManager` 전체 (155B), `LdrSetDllManifestProber` 전체, `GetCommandLineA` 전체, `RtlInitializeHandleTable` 전체 (43B), 문자열 버퍼 할당 함수 전체 (`+0x0 ~ +0x12`).
  - kernelbase `entry +0x8/+0xd` · `+0xd` · `+0x20` · `+0x27` · `+0x2e` · `+0x3f`, `KernelBaseBaseDllInitialize +0x28/+0x2d` · `+0x44` (꼬리 `JMP`).
  - `_KernelBaseBaseDllInitialize +0x195` · `+0x1a6` · `+0x1ad/+0x1b2` · `+0x1cf/+0x1d6` · `+0x1db` · `+0x470` · `+0x474` · `+0x47b` · `+0x47e` · `+0x485` · `+0x489` · `+0x490/+0x497` · `+0x49c` · `+0x49e` (→ `+0x6b0`) · `+0x6b0` · `+0x6b7` · `+0x6bd` (→ `+0x4a4`).
  - `GlobalAlloc +0x44` · `+0x63` · `+0x69` · `+0x6e` · `+0x73` · `+0x7a/+0x81` · `+0xd6/+0xdd`, `RtlCreateTagHeap +0x45` (→ `+0x67`) · `+0x67`.
  - `RtlUnicodeStringToAnsiString +0x30` · `+0x38` · `+0x3e` · `+0x46` (→ `+0x26b`) · `+0x4c` · `+0x54` · `+0x68` · `+0x72` · `+0x76` (→ `+0x28d`) · `+0x90` · `+0x96` (→ `+0x2f7`) · `+0x9f` (→ `+0x2c0`) · `+0x245` · `+0x249` · `+0x283/+0x288` · `+0x2c2/+0x2c7` · `+0x2c7` · `+0x2ce` (→ `+0x30d`) · `+0x2d0` · `+0x30d`.
  - kernel32 `entry +0x1d/+0x22`, `_BaseDllInitialize +0x8b` · `+0x92/+0x99` · `+0x9e` · `+0xa5` · `+0xac` · `+0xb3/+0xba` · `+0xbf` · `+0xc2/+0xc7` · `+0x21f/+0x224` · `+0x228` (→ `+0x39f`) · `+0x39f` · `+0x3a2` · `+0x3a8 ~ +0x3ad` · `+0x3af/+0x3b6`, 메모리 관리 사본 함수 전체 (66B), `BasepProbeForDllManifest +0x4a/+0x4f` · `+0x86/+0x8d` · `+0xea/+0xf1`.
  - `LdrpMapAndSnapDependency +0x4f` (→ `+0x96`) · `+0x51` · `+0x59` · `+0x5e/+0x63` · `+0x65 ~ +0x6d`, `LdrpFindDllActivationContext +0x6` · `+0x1a` (→ `+0x114`) · `+0x20` · `+0x24` · `+0x2d` · `+0x34` · `+0x37` (→ `+0x16c`) · `+0x7e/+0x83` · `+0x85` · `+0x8a` (→ `+0x11d`) · `+0xc9` · `+0xd1` (→ `+0x17c`) · `+0x114` · `+0x11b` · `+0x16c` · `+0x17a` · `+0x17c` · `+0x188/+0x18d` · `+0x192`.
  - `LdrpInitializeProcess +0x1980` · `+0x1985` · `+0x1993` · `+0x1a94`, `LdrpInitializeKernel32Functions +0xb6` · `+0xbb/+0xc0`, `LdrpPrepareModuleForExecution +0xbf ~ +0xcf` · `+0xea/+0xef`, `LdrpInitializeGraphRecurse +0x65/+0x6a` · `+0x91/+0x96`, `LdrpInitializeNode +0x197/+0x19c`, `LdrpCallInitRoutine +0x8e/+0x93`, `LdrpCallInitRoutineInternal +0x1d/+0x22`.
  - ntoskrnl `ExpQuerySystemInformation +0x233` · `+0x23c` (→ `+0xe18`) · `+0xe18` · `+0xe1c` (→ `+0x1068`) · `+0x1068` · `+0x1072`.
- 로그: 없음 (정적만). 값 대조만 run44 덤프 읽기 로그.

## 결과

### 정적 — 호출 지점 (`_calls.tsv`, 길 순서)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x199d` | `+0x19a2` | `ntdll!LdrpInitializeKernel32Functions` | exe `Subsystem` 2·3 (`+0x1980 ~ +0x1993`) — test.exe 3 (콘솔, `objdump -p`) | [정적] |
| `ntdll!LdrpInitializeKernel32Functions` | `+0xbb` | `+0xc0` | `ntdll!LdrLoadDll(0x4001, 0, "KERNEL32.DLL")` | 못 찾았을 때 (run20) | [정적] |
| `ntdll!LdrpPrepareModuleForExecution` | `+0xea` | `+0xef` | `ntdll!LdrpInitializeGraphRecurse` | 로드 문맥 `[항목+0xb0]` 이 있고 그 `+0x20` 비트 0 꺼짐 (`+0xbf ~ +0xcf`) | [정적] · run7 [관찰] 콜스택 |
| `ntdll!LdrpCallInitRoutineInternal` | `+0x1d` | `+0x22` | (CFG 간접) KERNELBASE·KERNEL32 시작 함수 | `EntryPoint ≠ 0` (run27·run28) | [정적] |
| `KERNELBASE!KernelBaseDllInitialize` (Ghidra `entry`) | `+0x8` | `+0xd` | `KERNELBASE!KernelBaseBaseDllInitialize` | 모든 이유 | [정적] |
| `KERNELBASE!KernelBaseBaseDllInitialize` | `+0x44` (`JMP`) | — | `KERNELBASE!_KernelBaseBaseDllInitialize` | 꼬리 점프 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x1ad` | `+0x1b2` | `KERNELBASE!KernelBaseDllInitializeMemoryManager` | 이유 1 갈래 (`+0x195` ~) | [정적] |
| `KERNELBASE!KernelBaseDllInitializeMemoryManager` | `+0x3a` | `+0x41` | `ntdll!NtQuerySystemInformation(0x32, &지역, 8, 0)` | `SystemRangeStart == 0` (`+0xf` · `+0x22 JNZ` → `+0x52`) — 첫 호출은 탐 | [정적] |
| `KERNELBASE!KernelBaseDllInitializeMemoryManager` | `+0x66` | `+0x6d` | `ntdll!RtlInitializeHandleTable(0xffff, 0x10, &BaseHeapHandleTable)` | 늘 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x1cf` | `+0x1d6` | `ntdll!RtlCreateTagHeap(PEB.ProcessHeap, 0, …)` | 늘 — 반환을 `+0x1db` 에서 `KernelBaseGlobalData+0` 에 | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x490` | `+0x497` | `ntdll!RtlUnicodeStringToAnsiString(&BaseAnsiCommandLine, &BaseUnicodeCommandLine, 1)` | 늘 (앞 `+0x485`·`+0x489` 에서 `CommandLine` 머리 16B 복사) | [정적] |
| `ntdll!RtlUnicodeStringToAnsiString` | `+0x2c2` | `+0x2c7` | 문자열 버퍼 할당 함수 (rva `0x21a90`, 이름 3개) ⇒ `+0x12` `RtlAllocateHeap(PEB.ProcessHeap, 0, 크기+1)` | `R8B = 1` (`+0x9f JNZ` → `+0x2c0`) | [정적] |
| `ntdll!RtlUnicodeStringToAnsiString` | `+0x283` / `+0x1f0` | `+0x288` / `+0x1f5` | `ntdll!RtlUnicodeToUTF8N` | ACP 또는 OEM 이 `0xfde9` 일 때만 — 이 VM 은 949 라 안 탐 | [정적] · [관찰] 사진 NLS 표 |
| `KERNEL32!BaseDllInitialize` (Ghidra `entry`) | `+0x1d` | `+0x22` | `KERNEL32!_BaseDllInitialize` | 모든 이유 | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0xb3` | `+0xba` | `ntdll!LdrSetDllManifestProber(kernel32+0x23f90, kernel32+0x61520, [__imp_ReleaseActCtx])` | 이유 1 갈래 | [정적] · [관찰] 사진 칸 값 |
| `KERNEL32!_BaseDllInitialize` | `+0xc2` | `+0xc7` | kernel32 메모리 관리 사본 함수 (rva `0x2c9d8`, 이름 2개) | 이유 1 갈래 (`+0xbf MOV ESI,R15D` 바로 뒤) | [정적] |
| kernel32 메모리 관리 사본 함수 | `+0x4` | `+0xb` | `KERNELBASE!KernelBaseGetGlobalData` | 늘 | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x3af` | `+0x3b6` | `ntdll!LdrSetDllManifestProber(0, 0, 0)` | `+0x224` `_Kernel32LegacyLib_BaseDllInitialize` 가 0 (`+0x228 JZ` → `+0x39f`) 이고 `SIL ≠ 0` (attach 만) — 되돌리기, 정상은 안 탐 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1aa4` | `+0x1aa9` | `ntdll!LdrpMapAndSnapDependency` (exe 문맥) | IL-only 아니면 (run19) — **`+0x19a2` 뒤** | [정적] |
| `ntdll!LdrpMapAndSnapDependency` | `+0x5e` | `+0x63` | `ntdll!LdrpFindDllActivationContext(모듈 항목)` | 문맥 `+0x20` 비트 `0x800000` 꺼짐 (`+0x4f JNZ` → `+0x96`), 항목 `LoadReason` (`+0x10c`) ≠ 9 (`+0x59 JZ`) | [정적] |
| `ntdll!LdrpFindDllActivationContext` | `+0x7e` | `+0x83` | (CFG 간접) `LdrpManifestProberRoutine` 칸의 함수 = `KERNEL32!BasepProbeForDllManifest` (exe·dll1·dll2 때) | 칸 ≠ 0 (`+0x6` · `+0x1a JZ` → `+0x114`), exe 항목이면 `PEB.ActivationContextData == 0` (`+0x16c`) | [정적] · [관찰] 사진 칸 값 |
| `ntdll!LdrpFindDllActivationContext` | `+0x188` | `+0x18d` | `ntdll!RtlReleaseActivationContext(옛 값)` | 출력 칸 ≠ 0 이고 항목 `+0x88` 에 옛 값 있을 때 | [정적] |
| `KERNEL32!BasepProbeForDllManifest` | `+0x86` | `+0x8d` | `ntdll!LdrResFindResourceDirectory` (`RT_MANIFEST` `0x18`, ID 2) | `+0x4a/+0x4f IsFusionFullySupported` · PEB 플래그 검사 뒤 | [정적] |
| `KERNEL32!BasepProbeForDllManifest` | `+0xea` | `+0xf1` | `CreateActCtxW` (kernelbase) | 매니페스트를 찾았을 때 — sample 셋은 자원 없음 → 안 탐 | [정적] · [정적+샘플] |
| `KERNELBASE!GlobalAlloc` | `+0x7a` | `+0x81` | `ntdll!RtlAllocateHeap([BaseHeap], 옵션 \| ([KernelBaseGlobalData+0] + 0x100000), 크기)` | 고정 블록 (`+0x44 TEST CL,2` 꺼짐) | [정적] |
| `KERNELBASE!GlobalAlloc` | `+0xd6` | `+0xdd` | `ntdll!RtlAllocateHandle(&BaseHeapHandleTable …)` | 이동식 (`GMEM_MOVEABLE`, `+0x47 JNZ` → `+0xb1`) | [정적] |

### 호출 관계

```
LdrpInitializeProcess
 ├─ (1) +0x19a2  LdrpInitializeKernel32Functions           [관찰] run7 (콜스택)
 │      └─ +0xc0  LdrLoadDll("KERNEL32.DLL") → … → LdrpPrepareModuleForExecution
 │                 └─ +0xef  LdrpInitializeGraphRecurse (KERNEL32 노드)
 │                      ├─ +0x96  LdrpInitializeGraphRecurse (재귀, KERNELBASE 노드)
 │                      │          └─ +0x6a Node → +0x19c CallInitRoutine → +0x93 Internal → +0x22 (CFG)
 │                      │               KERNELBASE!KernelBaseDllInitialize (entry)
 │                      │                ├─ +0xd   KernelBaseBaseDllInitialize ⇒ +0x44 JMP _KernelBaseBaseDllInitialize
 │                      │                │          ├─ (1) +0x1b2  KernelBaseDllInitializeMemoryManager
 │                      │                │          │           ├─ +0x1b   BaseHeap ← [PEB+0x30]
 │                      │                │          │           ├─ +0x41   NtQuerySystemInformation(0x32)  ── SYSCALL ── 커널: 상수 0xFFFF800000000000
 │                      │                │          │           │          (SystemRangeStart == 0 일 때만) → +0x4b SystemRangeStart
 │                      │                │          │           ├─ +0x6d   RtlInitializeHandleTable(0xffff, 0x10, &BaseHeapHandleTable)
 │                      │                │          │           └─ +0x79 · +0x87 · +0x8e  KernelBaseGlobalData+0x40 / +0x50 / +0x48
 │                      │                │          ├─ (2) +0x1d6  RtlCreateTagHeap  → +0x1db KernelBaseGlobalData+0 (= 0)
 │                      │                │          │      … (+0x2d9 CsrClientConnectToServer 등, Q129)
 │                      │                │          └─ (3) +0x497  RtlUnicodeStringToAnsiString(&BaseAnsiCommandLine, &BaseUnicodeCommandLine, 1)
 │                      │                │                      (앞 +0x489 BaseUnicodeCommandLine ← CommandLine 머리 16B)
 │                      │                │                      └─ +0x2c7  문자열 버퍼 할당 (0x21a90) ⇒ RtlAllocateHeap(ProcessHeap, 0, 9)
 │                      │                │                      실패면 +0x49e JS → +0x6b0 · +0x6b7  ANSI 머리 0
 │                      │                └─ +0x20 · +0x2e · +0x3f  SystemRangeStart · BaseHeap · BaseHeapHandleTablePtr  ← KernelBaseGlobalData (이유 상관없이)
 │                      └─ +0x6a Node → +0x19c CallInitRoutine → +0x93 Internal → +0x22 (CFG)
 │                           KERNEL32!BaseDllInitialize (entry)
 │                            └─ +0x22  _BaseDllInitialize
 │                                       ├─ (1) +0xba   ntdll!LdrSetDllManifestProber(k32+0x23f90, k32+0x61520, ReleaseActCtx)
 │                                       │                └─ +0x0 · +0x7 · +0xe  탐침 칸 셋 → +0x15 RET
 │                                       └─ (2) +0xc7   메모리 관리 사본 함수 (0x2c9d8)
 │                                                       └─ +0xb  KernelBaseGetGlobalData → +0x21 k32 BaseHeap · +0x36 k32 SystemRangeStart (0 일 때)
 └─ (2) +0x1aa9  LdrpMapAndSnapDependency (exe 문맥)                       ← 여기서부터 탐침 칸 = kernel32+0x23f90
        ├─ +0x63  LdrpFindDllActivationContext (exe 항목)
        │          └─ +0x83  (CFG 간접) KERNEL32!BasepProbeForDllManifest (DllBase, 경로, &출력)
        │                     ├─ +0x8d  LdrResFindResourceDirectory (RT_MANIFEST, ID 2)
        │                     └─ +0xf1  CreateActCtxW     ← 찾았을 때만 (sample 셋은 안 탐)
        └─ +0x34e LdrpLoadDependentModuleInternal → … dll1 → (같은 함수) dll2 — 둘 다 +0x63 같은 길 [추측: 같은 코드]
```

- 라벨은 ret 자리. `(1) (2) (3)` 은 그린 것끼리의 코드 흐름 순서 (사이에 안 그린 호출이 많다).
- kernel32·kernelbase 자신의 `LdrpMapAndSnapDependency` (`LdrpMapDllWithSectionHandle +0x1c8`, 2.6) 는 위 (1) 의 `LdrLoadDll` 안, 시작 함수보다 **앞** 이라 그때 탐침 칸은 0 → `LdrpFindDllActivationContext +0x1a JZ` → `+0x114` 에서 0 반환.

### 질문별 요약 (근거 줄은 결과서 해당 절)

| # | 질문 | 답 | 닫힘 |
|---|---|---|---|
| 1 | `KernelBaseDllInitializeMemoryManager` 가 쓰는 칸·값 출처 | 인자·반환 없음. `BaseHeap ← PEB.ProcessHeap` (조건 없음) → (`SystemRangeStart == 0` 이면) `NtQuerySystemInformation(0x32)` → `SystemRangeStart` → `RtlInitializeHandleTable` (`BaseHeapHandleTable` `0x30`B: `+0` `0xffff`, `+4` `0x10`, 나머지 0) → `KernelBaseGlobalData` `+0x40` `+0x50` `+0x48` 에 사본. 새 힙 없음 | **정적으로 닫힘** |
| 2 | `KernelBaseGlobalData +0x40 ~ +0x57` 을 쓰는 곳 | 8바이트 칸 셋, 쓰는 곳은 위 함수 `+0x79`·`+0x8e`·`+0x87` **하나씩뿐** (`_refs` 313줄 중 이 구간 WRITE 셋). 읽는 곳: kernelbase `entry +0xd`·`+0x27` (16B), kernel32 메모리 관리 사본 함수 `+0x18`·`+0x1c` | 닫힘 (포인터 경유 읽기는 훑기 한계 — 결과서) |
| 3 | `RtlUnicodeStringToAnsiString` 갈래 | 길이·변환 모두 **인라인** (`RtlUnicodeStringToAnsiSize`·`RtlUnicodeToMultiByteN` 호출 없음). 이 VM 은 ACP 949 · `DBCSCodePage 1` 이라 **DBCS 갈래** (`+0x76 JNZ` → `+0x28d`). 할당은 프로세스 힙 `크기+1`. syscall 없음. test.exe: `{8, 9, "test.exe\0"}`, 반환 0 | **정적으로 닫힘** (갈래는 사진 NLS 표로) |
| 4(a) | `LdrSetDllManifestProber` 가 쓰는 칸 | 셋을 그대로 전역 셋에 (`+0x0`·`+0x7`·`+0xe`) — 호출·검사·syscall 없음. 둘째 호출 `+0x3af` 는 셋 다 0 으로 되돌리는 실패 길 | 닫힘 |
| 4(b) | kernel32 DllMain vs dll1·dll2 `LdrpFindDllActivationContext` | **앞** (`+0x19a2` 안 vs `+0x1aa9`). 그래서 exe·dll1·dll2 때 탐침 칸 = `kernel32+0x23f90` | 코드 길은 닫힘 / 실행 순서는 동적 |
| 4(c) | `LdrpFindDllActivationContext` 가 반환을 쓰는 법 | 쓰는 칸은 항목 `+0x88` 하나 (출력 칸 ≠ 0 일 때만). "없음 취급" 집합 (`0xC000000F` · `0xC0000089 ~ 8B` · `0xC00000BB` · `0xC0000002` · `0xC0000204`) 이면 로그 뒤 0 반환. 그 밖 음수면 로드 실패 | 닫힘, **단 탐침이 test.exe 에 주는 상태값은 안 닫힘** |

## 확인된 것

- `[정적]` **kernelbase 의 `BaseHeap` 은 두 번 쓰인다** (같은 값이라 정상):
  - 첫째 `KernelBaseDllInitializeMemoryManager +0x1b` (이유 1 갈래 안).
  - 둘째 `entry +0x2e` — `KernelBaseBaseDllInitialize` 가 돌아온 뒤 **이유와 상관없이** `KernelBaseGlobalData +0x40` 16B 를 읽어 (`+0x27`) 낮은 8B 를 `BaseHeap` 에, 높은 8B 를 `BaseHeapHandleTablePtr` (`+0x3f`, rva `0x3b0348`) 에. 같은 자리 `+0x20` 이 `SystemRangeStart` 도 `+0x50` 에서 다시 쓴다.
- `[정적]` **kernel32 도 자기 `BaseHeap`·`SystemRangeStart` 를 가진다** (rva `0xc2240`·`0xc2238`). `_BaseDllInitialize +0xc2/+0xc7` → 메모리 관리 사본 함수가 `KernelBaseGetGlobalData` 로 받은 주소 `+0x40` (`BaseHeap`, `+0x21`) · `+0x50` (`SystemRangeStart`, `+0x36` — kernel32 쪽이 0 일 때만) 을 옮긴다. `+0x48` (핸들 표 주소) 은 kernel32 가 안 옮긴다.
- `[정적]` **`SystemRangeStart` 값은 커널이 변수를 읽지 않고 상수로 준다**: `ExpQuerySystemInformation +0x233 CMP EBX,0x32` · `+0x23c JZ` → `+0xe18 CMP R14D,8` · `+0xe1c JZ` → `+0x1068 MOV RAX,-0x800000000000` · `+0x1072 MOV [RDI],RAX`. 출력 길이가 8 이 아니면 다른 갈래.
- `[정적]` **`KernelBaseGlobalData+0` (4B) = 0**: `RtlCreateTagHeap` 은 `PEB+0xbc` (`NtGlobalFlag`) `& 0x800` 이 0 이면 `+0x45 JZ` → `+0x67 XOR EAX,EAX`. `[관찰]` run44 `NtGlobalFlag` = 0 (`ms/logs/04_baggage-read_P1a.txt:1770`, `_P1b.txt:2468`, `_P2.txt:2480`). 그래서 `GlobalAlloc` 플래그 = `0x100000 | 옵션` (`+0x63` · `+0x69 ADD EAX,0x100000` · `+0x6e OR EDX,EAX`).
- `[정적]` **명령줄 유니코드 사본은 새 버퍼가 아니다**: `_KernelBaseBaseDllInitialize +0x470 RAX = [RSI+0x20]` (`ProcessParameters`) → `+0x485 MOVUPS XMM0,[RAX+0x70]` (`CommandLine` 16B) → `+0x489` `BaseUnicodeCommandLine` 에 저장. 머리 (`Length`·`MaximumLength`·`Buffer` 포인터) 만 복사해서 글자는 매개변수 블록 안 그대로 같이 쓴다.
- `[정적]` **명령줄 ANSI 사본만 프로세스 힙에 새 블록 하나**: `RtlUnicodeStringToAnsiString +0x2c2/+0x2c7` → 문자열 버퍼 할당 함수 (`RtlAllocateHeap(PEB.ProcessHeap, 0, 크기+1)` 꼬리 점프). 성공이면 `+0x2c7 [RSI+8] = Buffer`, `+0x2d0 [RSI+2] = 크기+1`, 끝에 `+0x245 Buffer[길이] = 0`, `+0x249 [RSI] = 길이`. NULL 이면 `+0x2ce` → `+0x30d` `0xC0000017`.
  - "ANSI 길이 = 유니코드 길이 / 2" 는 **ASCII 만일 때만** 맞다. 949 표에서 위 바이트가 있는 글자 (한글 등) 는 2바이트라 더 길어진다 (`+0x28d ~ +0x2b1`, 결과서).
- `[정적]` **`LdrSetDllManifestProber` 는 22바이트 칸 쓰기 셋 + `RET`**: `+0x0 [LdrpManifestProberRoutine] ← RCX` · `+0x7 [LdrpCreateActCtxLanguageW] ← RDX` · `+0xe [LdrpReleaseActCtxW] ← R8` · `+0x15 RET`. 셋 다 쓰는 곳은 이 함수 하나 (`_refs`). 이 함수를 import 하는 System32 파일은 kernel32 뿐 (결과서).
  - 읽는 곳: `LdrpManifestProberRoutine` ← `LdrpFindDllActivationContext +0x6`·`+0x4f` 만. 나머지 둘 ← `LdrpQuerySxSMUIFile` (`+0x118`·`+0x1fd` / `+0x21e`) — MUI 쪽, 이름만.
- `[정적]` **`LdrpFindDllActivationContext` 는 PEB 를 안 쓴다.** 쓰는 칸은 모듈 항목 `+0x88` (`EntryPointActivationContext`) 하나 (`+0x192`), 탐침이 출력 칸에 0 아닌 값을 줄 때만. 이 칸은 2.7 이 켜기 때 모듈마다 켜고 끄는 활성화 문맥이다 (run27).
- `[정적+샘플]` sample 셋 (`sample/bin/test.exe`·`dll1.dll`·`dll2.dll`) 은 PE 데이터 디렉터리 2 (자원) 가 RVA 0 / 크기 0 이다 (결과서 `objdump -p`, `ms/bin/` 과 md5 같음) → 매니페스트 없음 → `BasepProbeForDllManifest +0xea CreateActCtxW` 는 안 탄다.

## 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

- **탐침 칸 셋** (`ntdll+0x1d23a0` ~ `+0x1d23c0`): P1a (kernel32 준비 call 자리) 셋 다 0 (`ms/logs/04_baggage-read_P1a.txt:5044` · `:5046`). P1b 부터 P2 까지
  - `0x1d23a0` = `00007ffa`fa691520` = kernel32 `0x7ffafa630000` + `0x61520` (`LdrpCreateActCtxLanguageW`)
  - `0x1d23a8` = `00007ffa`f8a0fcd0` = kernelbase `0x7ffaf8920000` + `0xefcd0` (`LdrpReleaseActCtxW` = kernelbase `ReleaseActCtx`)
  - `0x1d23c0` = `00007ffa`fa653f90` = kernel32 + `0x23f90` (`LdrpManifestProberRoutine`)
  - (`_P1b.txt:6162` · `:6164`, `_P2.txt:6174` · `:6176`). 정적 계산과 같다. 짐 지도 7.3 이 이 셋을 `P1a~P1b` 로 둔 것과도 맞다.
  - 단 P1a 와 P1b 사이에 kernel32 올리기와 exe import 올리기가 **둘 다** 들어 있어서, 사진만으로는 4(b) 순서를 못 가른다.
- **`PEB.ProcessHeap`** = `0x25996f30000` — P1a·P1b·P2 같음 (`_P1a.txt:1739`, `_P1b.txt:2437`, `_P2.txt:2449`). 이 힙은 `LdrpInitializeProcess +0xa4b LdrpInitializeProcessHeap` 가 만든다 (run46). `BaseHeap` 은 이 값이어야 한다 `[정적 + 관찰 값]`. kernelbase 칸 자체는 사진에 없다.
- **명령줄 입력** `CommandLine` = `{Length 0x10, MaximumLength 0x12, Buffer 0x25996f3813e}`, 글 `"test.exe"` — P1a·P1b·P2 같음 (`_P2.txt:3266`, `_P1a.txt:2556`, `_P1b.txt:3254`). 그래서 출력 `{8, 9, …}`.
- **NLS 표** `GlobalRtlNlsState` (`ntdll+0x1ccf90`): `CodePage 0x03b5` (949) · `MaximumCharacterSize 2` · `DBCSCodePage 1` · 앞 바이트 범위 `0x81 ~ 0xfe` (`_P2.txt:4829`, `_P1a.txt:3699`, `_P1b.txt:4817`). UTF-8 갈래 (`0xfde9`) 는 안 탄다. run57 의 `RtlpInitCodePageTables` 가 채운 표와 이어진다.
- **`PEB.ActivationContextData`** = `(null)` — 사진 8장 모두 (예: `_P2.txt:2515`). 그래서 exe 항목 검사 `+0x16c` 는 "0 → 계속" 쪽이다 (exe 도 탐침을 부른다).
- **`PEB.NtGlobalFlag`** = 0 (위 "확인된 것") → `KernelBaseGlobalData+0` = 0.

## 결과서와 같음 / 다르게 적은 곳 · 보탠 것

- 같음: 1번·4(b) 결론, `KernelBaseGlobalData` 세 칸의 쓰는 곳, `RtlUnicodeStringToAnsiString` 갈래·출력, `LdrSetDllManifestProber` 칸 셋, `LdrpFindDllActivationContext` 갈래 — `.asm` 줄이 결과서와 모두 같다.
- 이름 적는 법만 다름: 결과서의 `RtlpCtAllocateMemory` (rva `0x21a90`) 와 `BaseDllInitializeMemoryManager2` (kernel32 rva `0x2c9d8`) 는 PDB 에서 같은 주소 이름이 여럿이라 (`pdb_aliases.py --lookup`, 3개 · 2개) 이 문서에서는 하는 일로 적고 후보를 붙였다 (flow-doc 규칙).
- (보탬) kernelbase `entry +0x8` 이 부르는 것은 `KernelBaseBaseDllInitialize` (rva `0x51d0`) 이고, 그 함수가 `+0x28/+0x2d _DllMainCRTStartupForGS2` 뒤 `+0x44` 에서 `_KernelBaseBaseDllInitialize` 로 꼬리 점프한다 — flow.md 2.5 run25 줄과 같다.
- (보탬) kernel32 `entry +0x1d` (ret `+0x22`) `_BaseDllInitialize` — flow.md 2.5 run25 의 "`+0x22` `_BaseDllInitialize`" 와 같다.
- (보탬) `RtlInitializeHandleTable` 의 `+0x4` 값은 `EDX` 의 비트 31 을 지운 것 (`+0x9 BTR`), `+0x8` 은 그 비트 (`+0x1f SHR EDX,0x1f`). `EDX = 0x10` 이라 `0x10` · `0`. 결과서 값과 같다.
- (보탬) 시작 함수까지의 ntdll 사슬 오프셋 (`LdrpInitializeGraphRecurse +0x65`·`+0x91`, `LdrpInitializeNode +0x197`, `LdrpCallInitRoutine +0x8e`, `LdrpCallInitRoutineInternal +0x1d`) — 짐 지도 경로 줄임 `②D…` 에 씀.

## 정정

- 이전 run 의 정정은 없다.
- 요청서 (`_workspace/sast/62_kb-heap-init.md`) 4(b) 의 "뒤라면 PE_emulator 의 0 이 맞다" 쪽 짐작은 틀렸다 — 코드 길은 **앞**이다. (요청서는 고치지 않는다.)
- PE_emulator 원문 (b) 의 "`GetCommandLineA` 가 빈 값을 줄 수 있음" 은 정확히는 **NULL** 이다 (빈 글 `""` 이 아님).

## 아직 확인 필요

동적 몫은 모두 **Q104 VM 단계**에 얹는다. ntdll·kernelbase 멈춤점은 test.exe 일 때만 걸리게 조건을 붙인다.

1. **4(b) 순서 실행 확인**: `bp ntdll!LdrSetDllManifestProber` (`r rcx, rdx, r8`, 콜스택에 `KERNEL32!_BaseDllInitialize+0xba` 기대) 와 `bp ntdll!LdrpFindDllActivationContext` (매 호출 `dq ntdll+0x1d23c0 L1`, `du poi(@rcx+0x50)`). 기대 순서: kernel32·kernelbase (칸 0) → `LdrSetDllManifestProber` → exe · dll1 · dll2 (칸 `kernel32+0x23f90`). 쓰기 감시 `ba w8 ntdll+0x1d23c0` 도 된다.
2. **4(c) 탐침 반환 상태값 3번**: `ntdll!LdrpFindDllActivationContext+0x83` 에서 `r eax` 를 exe · dll1 · dll2 세 번 (+ `dq @rsp+0x58 L1` 출력 칸). "없음 취급" 집합 안이거나 0 이어야 한다 `[추측]`. 정적으로 닫으려면 `LdrResSearchResource +0x271/+0x276 LdrpResSearchResourceMappedFile → +0x22a LdrpResGetResourceDirectory` 이하 후속 정적.
3. **kernelbase·kernel32 `.data` 칸 읽기** (사진에 없던 것): 멈춤점 `KERNELBASE!_KernelBaseBaseDllInitialize+0x1b2` (ret) 와 그 뒤 `KERNELBASE!KernelBaseDllInitialize+0x47` (entry 의 다시 쓰기 `+0x3f` 바로 뒤) 에서 `dq kernelbase+0x3aefa0 L1` (= `PEB+0x30`?) · `dq kernelbase+0x3aef00 L3` · `dd kernelbase+0x3aefc0 L0xc` · `dq kernelbase+0x3b0348 L2` · `dd kernelbase+0x3aeec0 L1` (0?) · `dq kernel32+0xc2238 L2`.
4. **명령줄 ANSI 사본**: `KERNELBASE!_KernelBaseBaseDllInitialize+0x497` (ret) 에서 `r eax` (0?) · `dq kernelbase+0x3af040 L4` · `da poi(kernelbase+0x3af058)` (`"test.exe"`). `RtlAllocateHeap` 안에서 생길 수 있는 syscall 은 모름.
5. (덤) CRT 가 부르는 `LoadLibraryExW ×3` (run7, Q31) 이 **새 DLL 을 올리면** 그때도 탐침을 부를 것이다 `[추측: 같은 LdrpMapAndSnapDependency 길, run21]`. 1번 멈춤점이 그대로 답한다.

- 다음 바퀴 후보 (정적):
  - 탐침이 "자원 디렉터리 없음" 모듈에 주는 상태값 — `LdrpResGetResourceDirectory` 이하 (위 2번을 정적으로).
  - Q129 (요청 16 의 나머지 — `ConsoleInitialize`·`CsrClientConnectToServer`·`BaseNlsDllInitialize` 등, 이번 범위 밖).
  - `KernelBaseGlobalData` 이웃 칸 (`+0x4`·`+0x8 ~ +0x20`·`+0x58 ~ +0x90`·`+0xa0 ~ +0xb0`) 의 값 출처 — 결과서는 쓰는 자리만 표로 넘김 (짐 지도 7.6.7).

## 도구 메모

- **`pdb_aliases.py`·`pdb_struct.py` 의 kernelbase 모듈 이름은 `KernelBase.dll` (대소문자 정확히).** `ms/sym/<이름>.pdb` 를 이름 그대로 찾기 때문에 `kernelbase.dll` 이라고 쓰면 "PDB 없음" / "표가 없음" 이 난다 (flow-writer 도 `--lookup kernelbase.dll` 로 같은 오류를 봄). `run_ghidra.sh` 는 대소문자 상관없다 (결과서).
- 새 함정 (디버거) 없음.

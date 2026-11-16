# 103 static-import-flags — 정적 분석 결과

- 요청서: `_workspace/sast/103_static-import-flags.md` (Q72, 정적만)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1`. 커널은 안 건드림 (ntoskrnl 은 sast/101 몫). `ms/sym/ntoskrnl.exe.pdb.info.txt` 의 `C29EBFB06B78B3C020DCA66D99713F9E1` 가 기준과 같음만 확인.
- 이미 있던 `.asm`·`.c` 를 그대로 읽음 (다시 안 뽑음): `LdrpAllocateModuleEntry`, `LdrpRecordModuleDependency`, `LdrpInitializeGraphRecurse`, `LdrpLoadDependentModuleInternal`, `LdrpInitializeNtdllDataTableEntry`, `LdrpInitializeProcess`, `LdrpInitializeKernel32Functions`.
- 새로 뽑은 함수 7개 (정규식 `^(LdrpFastpthReloadedDll|LdrpBuildForwarderLink|LdrpMergeNodes|LdrpLoadContextReplaceModule|LdrpSignalModuleMapped|LdrpInitializeShimDllDependencies|LdrpFreeReplacedModule)$`, depth 0, exit 0) → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`. 전수 검색에서 새로 나온 함수들이라 오프셋 대조용으로만 뽑음. `_calls.tsv` 는 누적·중복 제거 (4059 → 4064 줄).
- 새로 만든 `refs` 1개: `ms/disassemble/ntdll/_refs/LdrpDllNotificationLock.tsv` (`run_ghidra.sh refs ntdll.dll LdrpDllNotificationLock 0x28`, 20줄).
- 전수 검색 방법: 스크래치의 `ntdll.dis` (ntdll 코드 구역 8개 전부를 `objdump -d` 로 푼 것, 421,500 줄, sast/100 이 만든 것) 와 `fnmap.py` 를 다시 썼다. 도우미 `grepdis.py`·`scan_flags.py`·`scan_edge.py`·`fn.py`·`ctx.py` 는 이번에 만든 것 (전부 같은 스크래치 폴더, 레포 밖). 스크래치 = `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/`.
- 버전 확인: `LdrpInitializeProcess +0x1bb6` CALL / ret `+0x1bbb` → `LdrpInitializeGraphRecurse` **일치** (objdump 와 `_calls.tsv` 둘 다). `LdrpAllocateModuleEntry` 의 부르는 곳 6곳도 objdump 전수와 `_calls.tsv` 가 **줄마다 같다**. 파일 값(`LdrpDllNotificationLock` 40바이트)이 사진 `[관찰]` (P1a·P1b·P2) 과 **바이트까지 같다** → 바이너리가 타깃 VM 것과 같다고 본다.
- 오프셋 대조: 아래 `+0x` 는 전부 `.asm` (이미 있던 5개 + 새로 뽑은 7개) 줄에서 다시 찾아 맞춰 봤다. objdump 풀이의 `함수 시작 + 오프셋` 이 Ghidra `.asm` 오프셋과 한 줄도 안 어긋났다.
- 스크립트 문제: 없음.

## 한눈에

| # | 한 줄 답 |
|---|---|
| 1 (a) | **네, 켜는 줄은 `LdrpAllocateModuleEntry +0x194` 하나뿐.** ntdll 전체에서 항목 `Flags` 비트 5 (0x20) 를 켜는 명령은 이것 하나 (OR/BTS/마스크/포인터 쓰기/통째 복사 전수). 끄는 줄도 하나: `LdrpFreeReplacedModule +0x15`. 조건 칸 `ntdll+0x1cc920` = `LdrpDllNotificationLock` (rva `0x1cc910`, `RTL_CRITICAL_SECTION` 0x28 바이트) 의 `OwningThread` (`+0x10`). PDB 에 `+0x10` 자체 이름은 없다. `LdrpDllNotificationLock` 을 `EnterCriticalSection` 한 스레드의 ID 가 거기 들어간다. |
| 2 (b) | **빠진 게 있다.** 켜는 곳 4 (`LdrpRecordModuleDependency +0x11a`, `LdrpLoadDependentModuleInternal +0x17d9`, **`LdrpFastpthReloadedDll +0x1a5`, `LdrpBuildForwarderLink +0x11c`**), 지우는 곳 2 (**`LdrpLoadDependentModuleInternal +0xede`** — 요청서엔 함수 이름이 빠져 있었다 — 와 **`LdrpMergeNodes +0xe3`**), 비트 0 을 검사해 건너뛰는 곳 2 (`LdrpInitializeGraphRecurse +0x3a`, **`LdrpInitializeShimDllDependencies +0xa6`**), 포인터로 읽는 곳 3 (`LdrpLoadContextReplaceModule +0x7c`, `LdrpSignalModuleMapped +0x2c`, `LdrpMergeNodes +0x1b4`/`+0x25c`). 같은 `+0x18` 을 쓰는 다른 칸은 노드 `LoadCount` 등 (아래 표 2-F). **test.exe 의 정적 import 간선은 비트 0 이 꺼져 있다** `[정적]` (`+0xede` 가 지우므로). |
| 3 | **네, 하나뿐.** 직접 부름 1곳 `LdrpInitializeNtdllDataTableEntry +0x23e` (ret `+0x243`), 꼬리 점프 0, 주소를 들고 있는 코드·데이터 0 (아래 표 3). 게다가 그 호출은 핫패치 갈래 (`R8 != 0`) 에서만 간다. |

---

## 질문 1 (a) `Flags (+0x68)` 에 `0x20` 을 켜는 곳 전수

### 1-A 비트 5 를 켜는 줄 / 끄는 줄 (ntdll 전체)

| 함수 + 오프셋 | 명령 | 켬/지움 | 조건 |
|---|---|---|---|
| `LdrpAllocateModuleEntry +0x194` | `OR DWORD [RBX+0x68],0x20` | **켬 (유일)** | `+0xc2 CMP [LdrInitState],1` → `+0x105 JZ` (목적지 `+0x17a`). `+0x17a MOV RAX,GS:[0x30]` → `+0x183 MOV RCX,[RAX+0x48]` (TEB `+0x40 ClientId` 의 `UniqueThread`) → `+0x187 CMP [LdrpDllNotificationLock+0x10],RCX` → `+0x18e JZ +0x107` (같으면 건너뜀). 다르면 `+0x194` 를 거쳐 `+0x198 JMP +0x107`. |
| `LdrpFreeReplacedModule +0x15` | `AND DWORD [RBX+0x68],0xffffffdf` | 지움 | 조건 없음. 바꿔치기돼 버려질 항목을 풀기 직전 (`+0x1c ReferenceCount := 1`, `+0x2b JMP LdrpDereferenceModule`). 부르는 곳: `LdrpLoadDllInternal +0x33c`, `LdrpLoadDependentModuleInternal +0x133c`, `LdrpSnapModule +0xfc`, `LdrpFreeLoadContext +0x9d`, `LdrpHandlePendingModuleReplaced +0x27`. |

한 줄로: **조건 = `LdrInitState == 1` 이고, 지금 스레드가 알림 락 주인이 아닐 때.** (plan 2026-10-24 결정문의 괄호와 같다.)

### 1-B 조건 칸 두 개의 정체

- `LdrInitState` (rva `0x1d23c8`): 쓰기 `_LdrpInitialize +0x101` (**0**, EBX 가 `+0x28 XOR EBX,EBX`), `LdrpInitializeProcess +0x1a4c` (**1**), `+0x1b1e` (**2**), `+0x17dd` 와 `_LdrpInitialize +0x205` (**3**) (`_refs/LdrInitState.tsv`). 곧 "1" 은 `LdrpInitializeProcess +0x1a4c` ~ `+0x1b1e` 사이, 즉 exe 의 import 를 붙이는 구간 (`+0x1aa4 LdrpMapAndSnapDependency`).
- `ntdll+0x1cc920`:
  - `--where ntdll.dll 0x1cc920` → "PDB 공개 심볼에 없음". 가까운 앞 이름은 `LdrpDllNotificationLock` (rva `0x1cc910`). 다음 PDB 이름 `LdrpTlsList` 가 `0x1cc938` 이라 이 이름의 덩치는 `0x28` = `RTL_CRITICAL_SECTION` (PDB 크기와 같음), 그 앞 `0x1cc8e0` 에 `LdrpDllNotificationLock_DEBUG`.
  - 그래서 `+0x10` = `_RTL_CRITICAL_SECTION.OwningThread`. (`ntdll.dll` 파일 값: `DebugInfo=0x1801cc8e0, LockCount=-1, RecursionCount=0, OwningThread=0, LockSemaphore=0, SpinCount=0x4000000` — 사진 P1a·P1b·P2 의 `7ffafb82c910` 부터 40바이트와 같다 `[관찰]`.)
  - `_refs/LdrpDllNotificationLock.tsv` (20줄): 칸 주소를 **쓰는 코드는 없다** (`mov [rip+..]` 꼴 0). 쓰는 건 `RtlEnterCriticalSection`/`RtlLeaveCriticalSection` 이 포인터로 한다. 부르는 쪽 (`LEA RCX,[0x1801cc910]` 뒤 `RtlEnterCriticalSection` `+0x...7f0` / `RtlLeaveCriticalSection` `+0x...8f0`): `LdrpSendDllNotifications +0x3b/+0x5e`, `LdrpSendPostSnapNotifications +0x30/+0xeb`, `LdrRegisterDllNotification +0x4f/+0x9b`, `LdrUnregisterDllNotification +0x14/+0x67`, `LdrpUnloadNode +0x69/+0x12c`, `LdrpDynamicShimModule +0x121/+0x14f`, `LdrpLoadShimEngine +0xee/+0x13a`.
  - **읽는 쪽 5줄** (전부 "TEB `+0x48` 과 같나" 비교): `LdrpAllocateModuleEntry +0x187`, `LdrpMapDllWithSectionHandle +0x232` (`+0x239 JE` → `+0x357`, `LdrpCompleteMapModule` 호출 `+0x24a` 를 건너뜀), `LdrpPrepareModuleForExecution +0x24` (`+0x2b JE` → `+0xfe`, **바로 돌려줌** — 그래프 켜기 안 함), `LdrGetProcedureAddressForCaller +0x18e` (`JE` → `+0x82`), `+0x925` (`JE` → `+0x424`).
  - 뜻 `[추측]`: `LdrpSendDllNotifications` 는 락을 쥔 채 등록된 알림 콜백을 부른다 (`+0x3b` 락 잡기 → `+0x4b` 목록 `LdrpDllNotificationList` 머리 읽기 → 비었으면 `+0x5c JNE` 안 타고 `+0x5e` 바로 풀기). 그러니 "락 주인 == 지금 스레드" = "알림 콜백 안에서 부른 로드". 그 경우엔 `0x20` 을 안 켜고 위 둘은 일을 건너뛴다.
  - test.exe: 사진 P1a·P1b·P2 모두 `LdrpDllNotificationList` (`ntdll+0x1cc8c0`) = 자기 자신 (빈 목록), `OwningThread` = 0 `[관찰]`. → 이 구간에서 "같다" 갈래가 설 자리가 없다.

### 1-C `LdrpAllocateModuleEntry` 를 부르는 곳 (objdump 전수 = `_calls.tsv`, 6곳)

| 부르는 자리 (ret) | 만드는 것 | 그때 `LdrInitState` |
|---|---|---|
| `LdrpInitializeNtdllDataTableEntry +0x67` (`+0x6c`) | ntdll 항목 | 0 (`LdrpInitializeProcess +0x119d` 가 부름, `+0x1a4c` 앞) |
| `LdrpInitializeProcess +0x1359` (`+0x135e`) | exe 항목 | 0 |
| `LdrpInitializeProcess +0x1577` (`+0x157c`) | AppVerifier 복제 항목 (`+0x1568 CMP AvrfAppVerifierMode` 가 0 이 아닐 때만) | 0 |
| `LdrpLoadDependentModuleInternal +0x1157` (`+0x115c`) | DLL 항목 | 상태 1 구간이면 1 |
| `LdrpFindOrPrepareLoadingModule +0x475` (`+0x47a`) | DLL 항목 | 〃 |
| `LdrpAllocatePlaceHolder +0xd0` (`+0xd5`) | 자리표시 항목 | 〃 |

주소를 들고 있는 데이터 (간접 호출 대상): `.rdata` 4곳 (`0x1ab2b8`, `0x1ab2ec`, `0x1ab300`, `0x1ab318`) 에 `0x6fab0` 이 있으나 전부 `RUNTIME_FUNCTION { 시작 0x6fab0, 끝 0x6fb06, 풀기정보 0x1ab2a4 }` 꼴의 **풀기(unwind) 사슬 칸**이지 함수 포인터가 아니다. 8바이트 VA 꼴 0곳.

### 1-D test.exe 에서 누가 켜지나 — 정적 추론과 사진 일치 `[정적]` = `[관찰]`

- exe·ntdll: 상태 0 때 만들어짐 → 안 켜짐.
- **kernel32·kernelbase 도 안 켜짐.** 이유: `LdrpInitializeProcess +0x199d` (ret `+0x19a2`) `LdrpInitializeKernel32Functions` 가 `+0xbb` (ret `+0xc0`) `LdrLoadDll` 로 kernel32 를, `+0x151` (ret `+0x156`) `LdrLoadDll` 로 kernelbase 이름을 **상태를 1 로 쓰는 `+0x1a4c` 보다 앞에서** 올린다 (kernel32 → kernelbase 는 그 적재의 import 로 같이 올라옴). 그때 `LdrInitState` 는 0.
- dll1·dll2: `+0x1aa4 LdrpMapAndSnapDependency` (상태 1 구간) 에서 처음 만들어져 켜짐.
- 사진 대조 `[관찰]`: `ms/logs/04_baggage-read_P1b.txt` 의 `ProcessStaticImport`: `test.exe` 0 (줄 3394), `ntdll.dll` 0 (3499), `KERNEL32.DLL` 0 (3604, `LoadReason 4 DynamicLoad`), `KERNELBASE.dll` 0 (3709, `LoadReason 0`), `dll1.dll` **1** (3814), `dll2.dll` **1** (3919). P1a 엔 exe·ntdll 만 있고 0 (줄 2696·2801). P2 도 같은 값. 정적 추론과 **전부 같다.** (baggage 565 줄의 "k32·kb 끝까지 0" 의 이유가 이것이다.)
- AppVerifier 복제 (`LdrpInitializeProcess +0x15b8 OR [RBX+0x68],ECX`, ECX = 원본 항목 `[+0x68]`): 원본에 0x20 이 없으니 안 옮겨진다. test.exe 는 `AvrfAppVerifierMode` 사진 값 0 이라 그 갈래 자체를 안 탄다 (`baggage.md` 958 줄).

### 1-E 항목 `Flags` 를 쓰는 나머지 줄 전부 — 비트 5 와 상관없음 확인

(`scan_flags.py`: 항목 안 연결 고리 7종의 어느 기준으로 접근해도 `Flags` 에 닿는 `or/and/xor/bts/btr/mov` 쓰기·XMM 쓰기·`lock` 꼴 전수. 로더 이름 함수 안의 것을 값까지 따라감.)

| 함수 + 오프셋 | 명령 | 쓰는 값 (PDB 비트 이름) |
|---|---|---|
| `LdrpMapDllSearchPath +0x13a` / `+0x370` | `OR [RSI+0x68],1` / `OR [RAX+0x68],R12D` | 비트 0 `PackagedBinary` / R12D 는 0 아니면 1 (`+0x88`, `+0x160`) |
| `LdrpCompleteMapModule +0x26b` / `+0x27d` / `+0x2ba` | `MOV [R14+0x68],ECX` (앞줄 `BTS ECX,0x16`) / (`BTS ECX,0x18`) / `AND [R14+0x68],0xfffffffb` | 비트 22 `CorImage` / 비트 24 `CorILOnly` / 비트 2 지움 |
| `LdrpProcessMappedModule +0x154` / `+0x183` / `+0x1a9` | `OR [RBX+0x68],EBP` (EBP=`0x2000`, `+0xc2`) / `MOV [RSI],EDX` (앞줄 `BTS EDX,9`) / `BTS [RSI],0x15` (RSI = `LEA RSI,[RBX+0x68]`, `+0x157`) | 비트 13 `LoadConfigProcessed` / 비트 9 `InExceptionTable` / 비트 21 `ScpInExceptionTable` |
| `LdrpInsertDataTableEntry +0x5e`, `LdrpInsertModuleToIndexLockHeld +0xec`, `LdrpRemoveDataTableEntry +0x60` | `OR 0x40`, `BTS 7`, `AND 0xffffffbf` | 비트 6 `InLegacyLists`, 비트 7 `InIndexes`, 비트 6 지움 |
| `LdrpSendPostSnapNotifications +0xce` | `OR [RBX+0x68],8` | 비트 3 `LoadNotificationsSent` |
| `LdrpInitializeNode +0x231` / `+0x29e` | `OR 0x80000` / `OR 0x100000` | 비트 19 `ProcessAttachCalled` / 비트 20 `ProcessAttachFailed` |
| `LdrpGetProcApphelpCheckModule +0x271`, `LdrpInitShimEngine +0xed` | `OR [..+0x68],R13D` / `R14D` (둘 다 `0x100`, `+0x76`, `+0x94`) | 비트 8 `ShimDll` |
| `LdrpLoadShimEngine +0x1d9`, `AVrfpLoadAndInitializeProvider +0x186` | `BTS 8`, `BTS 0xa` | `ShimDll`, `VerifierProvider` |
| `LdrpAllocateModuleEntry +0x10d / +0x11a / +0x129` | `OR 0x10000000 / 0x4 / 0x4000000` | 비트 28 `Redirected`, 비트 2 `ImageDll`, 비트 26 `ChpeEmulatorImage` |
| `LdrpInitializeNtdllDataTableEntry +0x93` | `OR [RBX+0x68],0x204` | 비트 9 + 비트 2 |
| `LdrpInitializeProcess +0x13c4 / +0x13cf / +0x13e4 / +0x13f0` | `MOV [RCX+0x68],EAX` 넷 (`OR EAX,4` → `OR EAX,EBX(=1)` → `BTS EAX,0x16` → `BTS EAX,0x18`) | exe 항목: 비트 2, 비트 0, 비트 22, 비트 24 |
| `LdrpInitializeProcess +0x15b8` | `OR [RBX+0x68],ECX` (ECX = `[원본+0x68]`) | 위 1-D (복사, AppVerifier 때만) |
| `LdrpCfgProcessLoadConfig +0xc5` | `OR [RSI+0x68],0x8000` | 비트 15 `ProtectDelayLoad` |
| `LdrpFindExistingModule +0xaf`, `LdrpProcessMachineMismatch +0xb4`, `LdrpUnloadNode +0x211` (기준 `NodeModuleLink −0x38`) | `OR 1`, `AND 0xfffffffb`, `OR [RDI-0x38],2` | 비트 0, 비트 2 지움, 비트 1 `MarkedForRemoval` |
| `AvrfMiniLoadDll +0x4c8 / +0x654 / +0x880 / +0x89b` | `MOV [R14+0x68],R15D(=0)` / `OR 4` / `BTS ECX,0xd` 뒤 `MOV` / `OR EBX(=0x200)` | 0, 비트 2, 비트 13, 비트 9 (검증기가 만드는 가짜 항목) |

그 밖에 `[reg+0x68]` 에 쓰는 줄 (`Heap`/`Etw`/`Tpp`/`Pss`/`Rtl…` 이름) 은 항목이 아닌 다른 구조체다 (포인터가 `RtlAllocateHeap` 블록·스택·스레드풀 등에서 옴). `LdrpMapAndSnapDependency +0x19d MOV [R9+0x68],EBX` 는 `LDRP_LOAD_CONTEXT` 의 "읽어 들일 import 개수" 칸이다 (`+0x180 [R9+0x58]` 에 `개수*8` 힙 블록을 놓고 `+0x199 [R9+0x6c]` 와 같이 씀). `LdrpCorFixupImage +0x11f MOV QWORD [RBX+0x68],RAX` 는 NT 헤더를 PE32 → PE32+ 모양으로 한 칸씩 밀어 옮겨 쓰는 줄 (`+0x115 ~ +0x126`, `+0x135 MOV WORD [RBX+0x18],0x20b` 로 끝맺음) 이라 항목이 아니다.

### 1-F 읽는 곳 (참고, 요청 밖이지만 뜻을 알려 주려고) — 31줄

- 항목 `[+0x68]` 로 직접 3곳: `LdrpInitializeNode +0x170` (켜져 있으면 `R9 = LdrpProcessInitContextRecord` 값 = DllMain 넷째 인자, 아니면 0, `+0x174 ~ +0x17a`), `LdrpProcessWork +0x114`, `LdrpPrepareModuleForExecution +0x50` (꺼져 있고 `GS:[0x1720] != 0` 이면 `LdrpAddNodeServiceTag`).
- 노드의 첫 모듈 `[RAX-0x38]` (노드 `Modules.Flink` = `NodeModuleLink`, 항목 `+0xa0`) 로 28곳: `LdrAddRefDll +0xab`, `LdrUnloadDll +0xaa`, `LdrpDecrementNodeLoadCountLockHeld +0x19`, `LdrpPinModule +0x2b`, `LdrpPinNodeRecurse +0x15`, `LdrpDependencyExist +0x14`, `LdrpRecordModuleDependency +0x34`, `LdrpBuildForwarderLink +0x9c`, `LdrpFastpthReloadedDll +0xd3`, `LdrpLoadDependentModuleInternal +0xce1 / +0xf22 / +0x130a`, `LdrpFindLoadedDllBy* (5곳)`, `LdrpFindOrPrepareLoadingModule +0x25c / +0x34f`, `LdrGetProcedureAddressForCaller (4곳)`, `LdrpResolveProcedureAddress +0x582`, `LdrResolveDelayLoadedAPI +0x111`, `LdrGetDllFullName +0xae`, `LdrpDereferenceModule +0x19`, `LdrpAddNodeServiceTag +0x1d`. 거의 다 "노드 `LoadCount == -1` 이거나 `0x20` 이 켜진 모듈이면 적재 횟수를 안 건드린다" 꼴 (예: `LdrpRecordModuleDependency +0x25 ~ +0x38`) `[추측: 이름과 꼴로 본 것]`.

### 1-G 검색 방법과 놓칠 수 있는 꼴

- 방법: (1) `objdump` 풀이 전체에서 기준이 `rsp` 아닌 `[reg+0x68]` 와 항목 안 고리 6종의 같은 칸 (`[reg+0x58]`·`+0x48`·`−0x8`·`−0x38`·`−0x60`·`−0x78`) 으로 `Flags` 에 닿는 모든 **쓰기** (크기 1·2·4·8·16, 쓰는 종류 `mov/or/and/xor/add/sub/bts/btr/btc/xchg/cmpxchg/xadd/inc/dec/neg/not/movups/movdqu/…`, `lock` 앞붙이 포함) 를 뽑아 로더 이름 함수는 값 원천까지 따라감. (2) `lea reg,[x+0x68]` 전수 (칸 주소를 들고 다니는 꼴): 로더에서는 `LdrpProcessMappedModule` 4곳과 `LdrpLoadDependentModuleInternal +0xb7c` (읽기만, `+0xfde`·`+0x16be` 가 `TEST [RAX],..`) 뿐. (3) 항목 통째 복사: 크기 `0x138` 상수 전수 → 읽기 쪽 `LdrQueryModuleInfoFromLdrEntry` 와 `AvrfMiniLoadDll` 의 `memset` 뿐. (4) **바이트 패턴으로 한 번 더** (`83/80/81 /1·/4 [reg+0x68]`, `0F BA /5·/6·/7 [..+0x68]`, `C7 /0`, `89`·`09` 등 ModRM `mod=01 disp8=0x68` 꼴): 비트 5 를 켜는 `OR imm` 1개 = `rva 0x6fc44` (= `LdrpAllocateModuleEntry +0x194`), 비트 5 를 끄는 `AND imm` 1개 = `rva 0xd755d` (= `LdrpFreeReplacedModule +0x15`). 둘 다 위 표와 같다.
- 놓칠 수 있는 꼴: ① 이 풀이는 ntdll 코드 구역만이다. kernelbase·apphelp·shimeng·검증기 DLL 이 PEB 로더 목록을 직접 걸어 `Flags` 를 쓰는 건 범위 밖. ② `[reg+idx*N+0x68]` 꼴 (색인 주소). 로더에 그런 쓰기는 못 봤다. ③ `objdump` 가 코드 사이 데이터를 잘못 풀 수 있다 → 바이트 패턴 교차로 그 위험을 줄였다. ④ 이름 없는 함수 (`_index.tsv` 에 안 든 코드) 는 `fnmap` 이 "앞 함수 + 큰 오프셋" 으로 보여 주니 이름이 어긋나 보일 수 있지만 이번 결과에는 없었다.

### 1-H PE_emulator 와

**맞다 (직접 쓰는 줄 없음).** `Src/ntdll/LdrInit.cpp` 가 진짜 `LdrpAllocateModuleEntry` 를 Unicorn 안에서 부르고 (`_CallModuleEntryAllocate`, 3961~3972 줄 — 주석에 "LdrInitState 0 이라 `+0x17a` 길 안 탐"), `LdrInitState ← 1` 도 진짜 `+0x1a4c` 자리에서 쓴다 (5653 줄), DLL 쪽은 진짜 `LdrpMapAndSnapDependency` 를 부른다 (5304 줄~). `ProcessStaticImport`·`+0x68` 에 `0x20` 을 우리가 직접 쓰는 줄은 `Src` 에 없다 (grep). 쓸 수 있는 전제는 TEB `+0x48` 이 0 이 아니라는 것인데 `Src/nt/Process.cpp:1410` 이 진짜 tid 를 넣는다 → 맞다. `확인 필요`로 남는 건 하나: kernel32·kernelbase 를 올리는 순서가 `LdrInitState ← 1` 보다 앞인지 (진짜는 `+0x199d` 가 `+0x1a4c` 앞). 헤더 주석상 같은 순서로 보이나 줄 단위 대조는 안 했다.

---

## 질문 2 (b) 간선 칸 `+0x18` 비트 0 전수

### 2-A 간선이 어떤 상자인가 (판단 근거)

- 크기 0x20 바이트. 만드는 곳은 **5곳** (`RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 태그, 0x20)`, 0x20 바이트 + `NtdllBaseTag` 인 호출을 ntdll 전체에서 찾음): 태그 `0x240000` 4곳 (`LdrpRecordModuleDependency +0x10d`, `LdrpLoadDependentModuleInternal +0x17bd`, `LdrpFastpthReloadedDll +0x198`, `LdrpBuildForwarderLink +0x10b`) + 태그 `0x280000` 1곳 (`LdrpLoadDependentModuleInternal +0x929`, 꼴이 `LEA R8D,[RAX+0x20]` 라 `MOV R8D,0x20` 검색으론 안 잡혔다). **HEAP_ZERO_MEMORY 가 없다 (안 지운 쓰레기)**.
- 칸 배치 `[정적]` (`LdrpRecordModuleDependency +0x96 ~ +0xce` 와 같은 모양이 4곳에 반복): `+0x0` 출발 노드 `Dependencies` (노드 `+0x28`, 꼬리가 가리키는 고리) 에 끼우는 다음 칸 · `+0x8` 도착 노드 포인터 · `+0x10` 도착 노드 `IncomingDependencies` (노드 `+0x30`) 에 끼우는 칸 · `+0x18` 출발 노드 포인터 (8바이트, **하위 비트에 표지**). PDB 에 이 상자 이름은 없다 (`_LDRP_CSLIST` 는 `Tail` 포인터 한 칸짜리).
- `+0x18` 만지는 방식 두 가지: 포인터를 통째로 쓴 다음 **옛 표지를 되살리는** 관용구 (`MOV [edge+0x18],노드(qword)` → `TEST/BT 옛값,1` → 있으면 `(노드 ^ 옛값) & 1 ^ 노드` 를 dword 로 다시 씀) 가 **ntdll 전체에 정확히 4곳** (2-D). (CRT `_raise_exc_ex +0xf0` 에 비슷한 모양이 하나 더 있으나 `[rbx+0x8]` 라 무관.) 포인터를 읽을 땐 `AND r,-8` 로 하위 3비트를 가린다 (2-E).

### 2-B 켜는 곳 (비트 0 := 1, 전수 4)

| 함수 + 오프셋 | 명령 | 조건 (정적) |
|---|---|---|
| `LdrpRecordModuleDependency +0x11a` | `OR DWORD [RAX+0x18],1` (새 상자, 할당 `+0x10d` ret `+0x112`) | `+0x77 TEST R8,R8` / `+0x7a JZ +0xf4` — 호출자가 미리 만든 간선 (R8) 을 안 줬을 때. 할당 실패면 `+0x115 TEST RAX,RAX` / `+0x118 JZ +0x130`. |
| `LdrpFastpthReloadedDll +0x1a5` | `OR DWORD [RAX+0x18],1` (할당 `+0x198` ret `+0x19d`) | 부모(R8→RBP) 가 있고 (`+0x92 TEST RBP,RBP / +0x95 JE +0xf8`), 부모 노드 `LoadCount != 0` (`+0xaa CMP [RBP+0x18],R14D / +0xae JE`), 자식 노드 ≠ 부모 노드 (`+0xbb CMP RBP,RSI / +0xbe JE +0xdd`), "자식이 `State 9` 이고 (`LoadCount == -1` 이거나 `0x20`)" 인 경우가 **아닐 때** (그 경우는 `+0xdd` 로 가서 자식 `LoadCount` 만 1 되돌림, `+0xc0 ~ +0xd7`), 그리고 부모 `Dependencies` 를 돌아 자식이 이미 있나 찾아봐서 없을 때 (`+0x161 ~ +0x17d`, 못 찾으면 `+0x17f` 할당). |
| `LdrpBuildForwarderLink +0x11c` | `OR DWORD [RAX+0x18],1` (할당 `+0x10b` ret `+0x110`) | 첫 인자(부모) 가 NULL 이 아니고 (`+0x17 TEST RCX,RCX / +0x1a JE +0xc5`), 부모 `LoadCount != 0` (`+0x78 CMP [RBP+0x18],EDI / +0x7b JE +0x1a9`), 자식 노드 `State 9` 예외, 이미 같은 간선이 없을 때 (`+0xd8 ~ +0xf0` 로 찾고 `+0xf2` 할당). |
| `LdrpLoadDependentModuleInternal +0x17d9` | `MOV EAX,[RAX+0x18]` (+0x17d6), `OR EAX,1`, `MOV [RCX+0x18],EAX` (+0x17dc) (할당 `+0x17bd` ret `+0x17c2`) | 들어오는 길은 `+0xf4d JZ` (목적지 `+0x179d`, RCX == 0) 하나. **그런데 RCX 는 `+0xed5` 에서 읽어 `+0xed8 MOV EAX,[RCX+0x18]` 로 이미 쓰인 값이라 0 일 수 없다.** `+0xf4a` 로 오는 길 전부 (`+0xf33 JZ`, 순서대로 내려오는 길) 에서 RCX 는 안 바뀐다. → **이 함수 안에서는 안 타는 죽은 길로 보인다** `[정적 추정]` (동적으로 확인할 것 D3). |

호출 쪽 조건 (누가 이 함수들을 부르나, `[정적]`):
- `LdrpFastpthReloadedDll`: `LdrpLoadDllInternal +0xb3` 한 곳. 부모 인자 = `LdrpLoadDllInternal` 의 6번째 인자 (R13) = **`LdrpLoadDll` 이 0 을 넣는다** (`LdrpLoadDll +0x71 XOR R12D,R12D`, `+0xf1/+0x1f8/+0x243 MOV [RSP+0x28],R12`). 그래서 `LoadLibraryExW → LdrLoadDll → LdrpLoadDll` 길에서는 이 줄에 안 간다 (`+0x95 JE`).
- `LdrpBuildForwarderLink`: `LdrpLoadDllInternal +0x290` 한 곳. 같은 부모 인자 (R13) 라 위와 같은 이유로 `LdrLoadDll` 길에선 `+0x1a JE` 로 끝. 부모가 NULL 이 아닌 길은 `LdrpLoadForwardedDll +0x2e1` (포워더 때문에 다른 DLL 을 올릴 때) 와 `LdrpLoadPatchImage +0xbf` 가 `LdrpLoadDllInternal` 을 부르는 길뿐이다.
- `LdrpRecordModuleDependency`: 질문 3.

### 2-C 지우는 곳 (비트 0 := 0)

| 함수 + 오프셋 | 명령 | 어떻게 | 조건 |
|---|---|---|---|
| `LdrpLoadDependentModuleInternal +0xede` (`+0xed8 MOV EAX,[RCX+0x18]`, `+0xedb AND EAX,0xfffffffe`) | `MOV DWORD [RCX+0x18],EAX` | 상자 `*[RSP+0xe0]` (이 함수 6번째 인자가 가리키는 칸의 간선) 의 `+0x18` 하위 dword 의 비트 0 만 지움 | 조건 없음 (`+0xecd` 로 오는 길 전부). 그 상자는 `+0x902 ~ +0x932` 에서 `[칸]` 이 0 이면 `RtlAllocateHeap(.., 태그 0x280000, 0x20)` 으로 만들어 칸에 꽂은 것 (안 지운 쓰레기 → 비트 0 이 아무 값이라 일부러 지움). |
| `LdrpMergeNodes +0xe3` | `MOV QWORD [RAX+0x18],RBX` | 합쳐져 없어질 노드 (`[RDI-0x18]` = 그 노드 `Dependencies` 고리) 의 모든 간선 `+0x18` 을 합친 노드 RBX 로 **덮어쓴다** → 하위 비트 전부 0 (표지 사라짐). 바로 아래 `+0xf6 MOV [RAX-0x8],RBX` 는 반대편 (그 노드 `IncomingDependencies` 의 고리 칸 −8 = 간선 `+0x8` 도착 노드) 을 RBX 로 바꾸는 줄 | `LdrpCondenseGraphRecurse +0xd3` (ret `+0xd8`) 가 부를 때만 (이 함수의 유일한 caller, `_calls.tsv`). 순환 의존이 있어 노드를 합칠 때 `[정적 추정]`. |

(2-D 의 4곳도 `MOV [edge+0x18],노드(qword)` 가 **일시적으로** 비트를 0 으로 만들지만 바로 되살린다 — 순효과 없음.)

### 2-D 되살리는 관용구 (순효과: 비트 0 보존) — 4곳

| 함수 + 오프셋 | 읽음 | 통째 쓰기 | 되살리기 |
|---|---|---|---|
| `LdrpRecordModuleDependency` | `+0xb8 MOV ECX,[R8+0x18]` | `+0xbc MOV [R8+0x18],RDI` | `+0xc0 TEST CL,1 / +0xc3 JZ +0xd2`, `+0xce MOV [R8+0x18],EAX` |
| `LdrpLoadDependentModuleInternal` | `+0xf8f MOV EDX,[RCX+0x18]`, `+0xf92 BT EDX,0` | `+0xf96 MOV [RCX+0x18],R14` | `+0xf9a JNC +0xfa8`, `+0xfa5 MOV [RCX+0x18],EDX` |
| `LdrpFastpthReloadedDll` | `+0x1dd MOV EDX,[RCX+0x18]` | `+0x1e0 MOV [RCX+0x18],RBP` | `+0x1e4 TEST DL,1 / +0x1e7 JE`, `+0x1f6 MOV [RCX+0x18],EAX` |
| `LdrpBuildForwarderLink` | `+0x154 MOV EDX,[RCX+0x18]` | `+0x157 MOV [RCX+0x18],RBP` | `+0x15b TEST DL,1 / +0x15e JE`, `+0x16d MOV [RCX+0x18],EAX` |

여기서 "출발 노드" 는 앞 인자(부모 모듈) 의 `DdagNode` (`[+0x98]`), "도착 노드" 는 뒤 인자의 것 (`LdrpRecordModuleDependency` 는 `RCX`·`RDX`, `LdrpBuildForwarderLink` 는 `RCX`·`RSI`).

### 2-E 읽는 곳

| 함수 + 오프셋 | 명령 | 읽는 방식 | 무엇을 함 |
|---|---|---|---|
| `LdrpInitializeGraphRecurse +0x3a` | `TEST BYTE [RBX+0x18],1` / `+0x3e JNZ +0x56` | 비트 0 | 켜져 있으면 그 간선을 **건너뛴다** (재귀·`State` 확인 안 함). RBX 는 `+0x26 MOV RSI,[RCX+0x28]` (이 노드 `Dependencies`) 를 돌며 얻은 간선. |
| `LdrpInitializeShimDllDependencies +0xa6` | `TEST BYTE [RDI+0x18],1` / `+0xaa JNZ +0x14c` | 비트 0 | 똑같이 건너뜀 (`ShimDll` 비트 `0x100` 인 항목의 `Dependencies` 를 돌며 `LdrpInitializeGraphRecurse` 를 부름, `+0xcd`). |
| `LdrpLoadContextReplaceModule +0x7c` / `+0x80` | `MOV RSI,[R9+0x18]` / `AND RSI,-8` | 포인터 (하위 3비트 가림) | R9 = 들어오는 고리 칸 − 0x10 (`+0x75 LEA R9,[RBX-0x10]`) = 간선 시작. 출발 노드를 얻어 그 `Dependencies` 에서 이 간선을 떼고 `RtlFreeHeap` (`+0xe7`). |
| `LdrpSignalModuleMapped +0x2c` / `+0x30` | `MOV RDX,[RBX+0x8]` / `AND RDX,-8` | 포인터 (하위 3비트 가림) | RBX = 들어오는 고리 칸 (간선 `+0x10`) → `+0x8` 이 간선 `+0x18`. 출발 노드의 `LoadContext` 카운터를 1 줄이고 0 이면 `State := 4` 하고 일감 큐 (`LdrpQueueWork`, `+0x59`). |
| `LdrpMergeNodes +0x1b4` / `+0x25c` | `CMP [R8+0x18],RBX` / `MOV RDX,[R8+0x18]` | 포인터 (가리지 **않음**) | R8 = 들어오는 고리 − 0x10. 합친 뒤 출발 노드가 RBX 인지 확인·옮김 (`+0xe3` 이 이미 RBX 로 덮어서 표지가 없다). |
| 2-D 의 4곳 | `MOV E?X,[간선+0x18]` 뒤 `TEST/BT 비트 0` | 비트 0 | 되살리기용 (순효과 없음). |

`LdrpInitializeGraphRecurse` 를 부르는 곳 (objdump 전수, 11곳): `LdrpInitializeProcess +0x16e6`·`+0x193c`·`+0x1bb6`, `LdrpPrepareModuleForExecution +0xea`, `LdrGetProcedureAddressForCaller +0x945`, 자기 자신 `+0x91`, `LdrpInitializeShimDllDependencies +0xcd`, `LdrpInitializeImportRedirection +0x14b`, `AVrfInitializeVerifier +0x221`·`+0x295`. (`_calls.tsv` 에는 `.asm` 이 있는 함수의 줄만 있어 `LdrpInitializeProcess` 셋만 보인다.)

### 2-F "같은 `+0x18`" 이지만 간선이 아닌 칸 (구분 근거)

| 칸 | 어디서 보이나 | 구분 근거 |
|---|---|---|
| `_LDR_DDAG_NODE +0x18 LoadCount` (dword) | `cmp [x+0x18],-1` 수십 곳, `LdrpInitializeProcess +0x1391`·`+0x159c`, `LdrpInitializeNtdllDataTableEntry +0x7f`, `LdrpInitializeImportRedirection +0x173`, `AvrfMiniLoadDll +0x594` 의 `OR [노드+0x18],0xffffffff` (= LoadCount := -1), `LdrpRecordModuleDependency +0x2b / +0x3a / +0xec`, `LdrpIncrementModuleLoadCount +0x6d / +0xf8`, `LdrpDecrementNodeLoadCountLockHeld +0xc / +0x3a`, `LdrpMergeNodes +0xcb / +0x1d7 / +0x22a / +0x2fd` (`ADD` 로 합산), `LdrpFastpthReloadedDll +0xaa / +0xdd / +0x201`, `LdrpBuildForwarderLink +0x78 / +0xa2 / +0x180`, `LdrpLoadDependentModuleInternal +0xcd8 / +0xf15 / +0x1301 / +0x1294 / +0x1522` | 포인터가 항목 `[+0x98]` (`DdagNode`) 에서 옴. 값이 -1·±1·합산. 비트 0 연산 없음 (-1 로 채우는 `OR` 만 있어 "비트 0 켬" 처럼 **보이지만** 다른 구조체). |
| `IMAGE_EXPORT_DIRECTORY +0x18 NumberOfNames` | `LdrpSnapModule +0x268`, `+0x767`, `LdrpGetProcedureAddress +0x115` | 포인터가 `DllBase + 내보내기 RVA` 에서 옴, 4바이트 읽기뿐. |
| `LDRP_LOAD_CONTEXT +0x18` | `LdrpLoadContextReplaceModule +0xce` 앞뒤, `LdrpQueueWork +0x6c` | 포인터가 항목 `[+0xb0]` (`LoadContext`) 에서 옴. |
| SRW 대기 블록·스택 홈 칸·힙 블록 | `LdrpIncrementModuleLoadCount +0x18b`, `LdrpFindLoadedDllByName +0x8db` (`lock bts [rcx+0x24],..` 옆), `mov [rsp+0x18],rbx` 같은 홈 저장 수십 줄, `RtlDeregisterWaitEx +0xd4 lock or [rbx+0x18],1` (스레드풀) | 포인터 출처가 다르고 이름 접두가 `Rtl`·`Tpp`·`Etw`·`Heap`. |

**판단 요약**: 간선이면 (1) 0x20 바이트 상자이고 `NtdllBaseTag + 0x240000/0x280000` 힙에서 만들어졌거나, (2) 노드 `+0x28`/`+0x30` 고리를 `mov r,[r]` 로 돌다 얻은 값 (간선 시작 = 고리 칸, 또는 고리 칸 − 0x10), (3) `+0x18` 에 노드 포인터를 쓰고 비트 0 을 건드리는 모양. 이 셋으로 위 표를 걸렀다.

### 2-G 검색 방법과 놓칠 수 있는 꼴

- 방법: (1) ntdll 전체의 `[reg+0x18]` 에 대한 `test/or/and/bt/btr/bts/xor/cmp` (크기 1·4·8, 즉치 있는 것) 전수 → 로더 이름 함수만 남겨 눈으로 분류 (수백 줄 중 간선 관련 스무여 줄). (2) `mov r,[x+0x18]` 뒤 8줄 안에서 `and 0xfffffffe`·`or 1`·`bt/btr/bts 0`·`test 1`·`xor` 인 줄. (3) 되살리기 관용구 (`xor; and 1; xor`) 전수 → 4곳. (4) 크기 0x20 + `NtdllBaseTag` 힙 할당 전수 (`mov r8d,0x20` 꼴과 `lea r8d,[rax+0x20]` 꼴 둘) → 5곳. (5) 로더 이름 함수의 `[reg+0x18]` 접근 전부 (포인터·dword·qword) 를 하나씩 위 표에 배정. (6) 들어오는 고리로 읽는 꼴: `[reg+0x8]` 에 대한 비트 0 연산 전수 (간선 관련 0줄. `AVrfpDllLoadNotificationInternal +0x4e/+0x95` 와 `AVrfpDllUnloadNotificationInternal +0x49/+0x91` 의 것은 `0x1801d44f0` 목록 안 32바이트짜리 검증기 칸이라 다른 구조 — Load 쪽을 열어 보니 `shl rbx,5` 로 칸을 센다) 와 마스크 `and r,-2`/`-8`/`btr r,0` 전수 (`-8` 이 로더에 2곳, `-2` 는 간선과 무관 (MUI)).
- 놓칠 수 있는 꼴: ① 간선 시작 주소를 인자로 받아 안에서 쓰는 작은 함수 (로더 이름 함수는 전수로 봤으니 로더 안엔 없음). ② `[reg+idx*8+0x18]` 색인 꼴. ③ 들어오는 고리 쪽 (`+0x10` 기준) 으로 `+0x8` 을 읽되 마스크·비트 연산 없이 비교만 하는 줄 (위 `LdrpMergeNodes` 는 이 꼴이라 잡혔다). 같은 꼴의 다른 함수가 있으면 (6) 이 못 잡는다 — 그래서 `node+0x30` 을 읽어 고리를 도는 함수들을 따로 봤다 (`LdrpSignalModuleMapped`, `LdrpMergeNodes`, `LdrpUnloadNode`, `LdrpDecrementNodeLoadCountLockHeld`, `LdrpFreeLoadContextOfNode`, `LdrpPinNodeRecurse`, `LdrpCondenseGraphRecurse` 등). ④ 다른 모듈의 접근 (범위 밖).

### 2-H test.exe 에서 `[정적]`

- 정적 import 간선 (exe → kernel32·dll1, kernel32 → kernelbase, dll1 → dll2 …) 은 `LdrpLoadDependentModuleInternal` 이 만든 **미리 만든 상자** 를 쓴다 (`+0x929` 할당, `+0xede` 에서 비트 0 지움, `+0xf96 ~ +0xfa5` 에서 연결 — 옛 비트가 0 이라 되살릴 게 없음). 그래서 이 간선들은 **비트 0 = 0 → `LdrpInitializeGraphRecurse` 가 따라간다.** 기존 `plan.md` 2026-10-24 문장 (대비용 할당 `+0x17d9` 가 켠다, 보통 칸 `+0xede` 가 지운다) 은 절반이 맞고 절반이 어긋난다: `+0xede` 가 지운다 (맞음), `+0x17d9` 는 죽은 길로 보이며 (정적 추정) 실제로 켜는 곳은 `LdrpRecordModuleDependency`·`LdrpFastpthReloadedDll`·`LdrpBuildForwarderLink` 다.
- 초기화 동안 비트 0 이 켜진 간선이 생길 길은 (a) 핫패치 (`LdrpRecordModuleDependency`, 질문 3), (b) 포워더로 다른 DLL 을 올리는 길 (`LdrpLoadForwardedDll`), (c) 부모가 있는 `LdrpLoadDllInternal` 호출 — test.exe 의 `LoadLibraryExW` 는 `LdrLoadDll → LdrpLoadDll` 라 부모 NULL → 안 만든다. (b) 가 test.exe 에서 일어나는지는 정적으로 안 따라갔다 (다음 요청 후보 N2).
- `LdrpMergeNodes` 가 지우는 경우: `LdrpCondenseGraphRecurse +0xd3` 이 순환 의존 때만 부름. test.exe (exe → dll1 → dll2, k32 → kb) 는 순환이 없다고 보인다 `[추측]`.

### 2-I PE_emulator 와

**맞다 (직접 쓰는 줄 없음).** 간선을 만드는 `LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal` 과 읽는 `LdrpInitializeGraphRecurse` 를 진짜 코드로 Unicorn 안에서 부르고 (`LdrInit.cpp` 5304 줄~ `_CallMapAndSnapDependency`, 6429 줄~ `LdrpInitializeGraphRecurse` 덫), `Src` 에 간선 칸 `+0x18` 을 우리가 쓰는 줄은 없다 (grep: `Dependencies`·`간선` 쓰기 0). **확인 필요**: `LoadLibraryExW` ×3 (상태 3) 길을 진짜 로더로 돌리는지 — 돌린다면 위 2-B 의 `LdrpLoadDll` 부모 NULL 때문에 비트 0 간선은 안 생긴다.

---

## 질문 3 `LdrpRecordModuleDependency` caller 전수

| 꼴 | 결과 | 근거 |
|---|---|---|
| 직접 `CALL` | **1곳**: `LdrpInitializeNtdllDataTableEntry +0x23e` (ret `+0x243`) | `objdump` 전수 `call 0x1800d8e40`, `_calls.tsv` 한 줄, 지도 `_map/LdrInitializeThunk_edges.tsv` 566 줄 (caller 깊이 6) 도 한 줄 (callee 열이 `LdrpRecordModuleDependency` 인 줄은 그것뿐). `.asm` +0x23e 줄 확인. |
| 꼬리 점프 `JMP`·조건 점프 | 0 | 같은 검색에서 `jmp/jcc 0x1800d8e40` 0줄. |
| 코드에서 주소 꺼내기 (`LEA`/`MOV reg, 0x1800d8e40`) | 0 | 풀이 전체에서 `# 0x1800d8e40` 주석 붙은 줄 0. |
| 데이터에서 주소 들고 있기 | 0 (코드 쪽) | 전 구역 바이트 검색: 8바이트 VA 0곳, 4바이트 RVA `0xd8e40` 은 `.pdata` 한 줄 (`rva 0x1de15c`, `RUNTIME_FUNCTION` 시작) 뿐. ntdll 의 모든 구역 (`.text`, `SCPCFG*`, `RT`, `PAGE`, `fothk`, `.rdata`, `.data`, `.pdata`, `.00cfg`, `.mrdata`, `.rsrc`, `.reloc`) 을 훑었다. CFG 함수 표 (`.rdata`) 에도 없음. 그래서 간접 호출 (`indirect:` 줄) 의 대상이 될 수 없다 `[정적]`. |

그 `+0x23e` 호출이 가는 조건 `[정적]`:
- `+0x140 TEST RBP,RBP` / `+0x143 JNZ` (목적지 `+0x22c`) → `+0x22c ~ +0x23e`: `RCX = [LdrpNtDllDataTableEntry]` (원래 ntdll 항목), `RDX = RBX` (새 항목), `R8 = 0`, `R9 = &[RSP+0x30]`. 즉 **`RBP` (이 함수의 세 번째 인자) 가 0 이 아닐 때** = **핫패치 ntdll 둘째 항목** 일 때만.
- `LdrpInitializeNtdllDataTableEntry` 는 `LdrpInitializeProcess` 에서 두 번 불림: `+0x119d` (ret `+0x11a2`, `R8 = 0` → 이 호출 안 감), `+0x126a` (ret `+0x126f`, `R8 = [LdrpNtDllDataTableEntry]` ≠ 0). 둘째는 `+0x1253 TEST RCX,RCX / +0x1256 JE +0x127b` 로 `[[LdrpNtdllHotPatchContext]]` 가 0 이 아닐 때만 (flow.md 2766 줄과 같은 말).
- 그러므로 **test.exe 에서 `LdrpRecordModuleDependency` 가 불리는지는 핫패치 문맥 값에 달렸다** (동적, Q44 와 같은 값). 핫패치가 꺼진 보통 프로세스는 안 탄다 `[추측]` — 사진에서 `LdrpIsHotPatchingEnabled` 는 끝까지 0 `[관찰]` (`LdrInit.cpp:1858` 주석이 인용).

### PE_emulator 와

**맞다.** 첫 호출 (`R8 = 0`) 만 진짜 `LdrpInitializeNtdllDataTableEntry` 로 부르고 (`LdrInit.cpp:3635`~`_CallNtdllDataTableEntry`, 인자 `{ base, 항목 칸, 0, 시스템 폴더 글 }`), 둘째 항목 갈래는 "문맥 0 이라 안 탄다" 로 줄였다 (7087~7091 줄 주석). `LdrpRecordModuleDependency` 를 직접 부르는 줄은 `Src` 에 없다 (grep).

---

## 동적으로 확인할 것

정적으로 끝난 것: Q1 은 사진 (P1b·P2 의 `ProcessStaticImport` 6개, `LdrpDllNotificationLock`·`LdrpDllNotificationList`) 이 정적 결론과 이미 맞아서 **새 동적 불필요**.

- **D1 (Q2, 사진으로 가능)**: 노드 `Dependencies` (`+0x28`) / `IncomingDependencies` (`+0x30`) 꼬리 포인터는 사진 P1b·P2 에 이미 있다. 각 간선 시작 주소에서 `dq <간선> L4` 로 `+0x18` 의 하위 비트를 본다. 기대 `[정적]`: test.exe 의 정적 import 간선은 모두 비트 0 = 0.
- **D2 (Q2)**: `LdrpLoadDependentModuleInternal +0x179d` (또는 `+0x17d9`) 가 test.exe 에서 한 번도 안 걸리는지 (죽은 길 `[정적 추정]` 확인). `LdrpFastpthReloadedDll +0x1a5`, `LdrpBuildForwarderLink +0x11c`, `LdrpMergeNodes +0xe3` 도 한 번도 안 걸린다고 기대. 한 번에 멈춤점 4개 (모두 `test.exe` 프로세스 조건): `ntdll!LdrpLoadDependentModuleInternal+0x179d`, `ntdll!LdrpFastpthReloadedDll+0x1a5`, `ntdll!LdrpBuildForwarderLink+0x11c`, `ntdll!LdrpMergeNodes+0xe3`.
- **D3 (Q3)**: `[[LdrpNtdllHotPatchContext]]` 의 첫 8바이트 = 0 (`ntdll+0x1d2908` 이 가리키는 지역 칸) — `LdrpInitializeProcess +0x1253` 에서. 기존 Q44 와 같은 값이라 새 항목 필요 없음. 추천 멈춤점: `ntdll!LdrpInitializeProcess+0x126a` (안 걸리면 `LdrpRecordModuleDependency` 안 불림).
- **D4 (Q1, 선택)**: `ntdll!LdrpAllocateModuleEntry+0x194` 가 test.exe 에서 정확히 2번 (dll1·dll2) 걸리는지 — 이미 사진이 같은 결과라 급하지 않음.

## 다음 요청 후보

- **N1 (문서 반영, 오케스트레이터→flow-writer)**: 이번 결과 중 문서에 이미 틀리게/모자라게 적힌 것: (a) plan 2026-10-24 결정문의 간선 `+0x18` 설명 (위 2-H), (b) `plan.md` Q72 줄의 "지움 `+0xede`" 앞에 함수 이름 (`LdrpLoadDependentModuleInternal`) 보태기, (c) `baggage.md` 565 줄 `ProcessStaticImport` 의 "k32·kb 끝까지 0" 에 이유 (`LdrpInitializeKernel32Functions` 가 상태 1 이전에 올림) 보태기, (d) 간선 칸 표 (2-A ~ 2-E) 를 짐 지도 `+0x28 Dependencies`/`+0x30 IncomingDependencies` 줄 옆에 한 줄.
- **N2 (정적, 작음)**: `LdrpLoadForwardedDll` 이 `LdrpLoadDllInternal` 을 부를 때 부모 인자가 무엇이고, test.exe 의 kernel32 → ntdll 포워더 (`kernel32!` 의 `ntdll` 로 가는 내보내기) 가 `LdrpSnapModule` 쪽에서 다른 DLL 을 새로 올리는 길 (= `LdrpBuildForwarderLink` 간선이 생기는 길) 을 타는지. 지금은 "test.exe 에서 비트 0 간선 0개일 듯" 이 `[정적 추정]`.
- **N3 (정적, 중요도 낮음)**: 알림 락 주인일 때 건너뛰는 갈래의 뜻 (`LdrpPrepareModuleForExecution +0x2b → +0xfe`, `LdrpMapDllWithSectionHandle +0x239 → +0x357`, `LdrGetProcedureAddressForCaller +0x195 / +0x92c`). test.exe 에는 알림 등록이 없어 (사진: 목록 빔) 이 갈래를 안 타므로 대표 지점 목표엔 불필요.

## 판정

**분석성공.** 세 질문 모두 답했고, 요청서의 가정 둘이 어긋남 (`LdrpFastpthReloadedDll`·`LdrpBuildForwarderLink` 가 비트 0 을 켠다, `LdrpInitializeShimDllDependencies` 도 읽는다, `+0x17d9` 는 죽은 길로 보임, `+0xede` 의 함수 이름) 을 찾아 고쳤다. PDB·사진과 어긋난 것 없음. 정적 추정으로 남은 것 둘 (`+0x17d9` 죽은 길, `LdrpMergeNodes` 호출 조건) 은 D2 가 확인한다.

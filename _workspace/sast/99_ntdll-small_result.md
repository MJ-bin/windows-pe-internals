# 99 ntdll-small — 정적 분석 결과

- 요청서: `_workspace/sast/99_ntdll-small.md` (Q66 + Q67, 정적만)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`). PDB `ntdll.pdb` guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`ntdll.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 셋 다 있음).
- ntoskrnl 은 **Ghidra 프로젝트를 안 열었다** (sast/98 이 쓰는 중). KUSD 칸은 PDB 파일 (`pdb_struct.py`, llvm-pdbutil) 로, 커널 코드는 `objdump -d` (바이너리 읽기만) + 이미 있는 `_index.tsv` 로 봤다.
- 뽑은 함수 (ntdll):
  - 요청 대상 5개 `^(LdrpLoadPatchedNtdll|CsrClientCallServer|LdrpInitializeProcess|RtlInsertInvertedFunctionTable|LdrpInsertDataTableEntry)$` 를 **한 번에 depth 1 로** 뽑았다 → 121개 (로그 `ms/ghidra/ntdll.export.sast99a.stdout`).
    - **내 실수**: 요청서는 `LdrpInitializeProcess` · `RtlInsertInvertedFunctionTable` · `LdrpInsertDataTableEntry` 를 depth 0 으로 적었는데 한 정규식에 묶어서 depth 1 이 모두에 걸렸다. 이미 있던 파일 ~100개가 같은 스크립트로 **다시 써졌다** (내용은 같거나 최신 형식). 새 파일은 `LdrpLoadPatchedNtdll`·`LdrpQueryCurrentPatch`·`LdrpLoadCustomNtdllWithSectionHandle`·`NpClose`·`NpUnmapViewOfSection` 등 소수.
  - 요청서가 없었지만 길을 따라가려고 depth 0 으로 둘 더: `LdrpInitializeApplicationVerifierPackage` (rva `0x116330`), `AVrfInitializeVerifier` (rva `0x119de4`) — Q67(b) `[RSP+0x60]` 을 채운 줄까지 거슬러 가려고 (로그 `…sast99b·c.stdout`).
  - `_refs` 넷: `CsrClientProcess` · `CsrServerApiRoutine` · `CsrPortHandle` · `CsrPortMemoryRemoteDelta` (`ms/disassemble/ntdll/_refs/`).
- 버전 확인: **일치.** `LdrpInitializeProcess +0x1bb6 / ret +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeInternal +0x55 / ret +0x5a → _LdrpInitialize` (`_calls.tsv`). `LdrInitializeThunk` 줄 등 나머지는 이번에 안 만짐.
- 스크립트 문제: **없음.**

## 한눈에

| 질문 | 한 줄 답 | 지금 PE_emulator |
|---|---|---|
| Q66-① 칸 이름 | `+0x308` = `SystemCall` (4바이트 정수, PDB 에 비트 이름 없음). 스텁은 bit 0 만 본다: 0 → `SYSCALL`, 1 → `INT 0x2e`. 쓰는 커널 코드는 못 찾음. 사진 8장 전부 값 0 | 맞다 (0 으로 둠) |
| Q66-② `CsrClientCallServer` | **기다린다.** ALPC 동기 요청 (`Flags 0x20000`), 받는 칸 = 보낸 칸, 시간 제한 없음. 답 status = 메시지 `+0x34`. 실패해도 `+0x34` 에 넣어 돌려줌 | 해당 없음 (부모·csrss 는 에뮬레이터에 없음, 의도) |
| Q67-a 예외 표 | **맞다.** `+0x415` 가 먼저, `InExceptionTable` 비트는 한참 뒤 `+0x119d` 안 (`LdrpInitializeNtdllDataTableEntry +0x93`) 에서 켜져 `LdrpProcessMappedModule +0x177` 의 둘째 등록을 막는다 | 맞다 |
| Q67-b `+0x1625` | **exe 도 ntdll 도 아니다.** `AvrfpLoaderEntry` (App Verifier 가 먼저 올린 `verifier.dll` 의 임시 항목) 의 복제. `AvrfAppVerifierMode ≠ 0` 일 때만 간다 → test.exe 는 안 탐 | 맞다 (길 안 탐) |
| Q67-c `LdrpLoadPatchedNtdll` | `[ctx+0]` 는 패치 ntdll 이미지의 **매핑 시작 주소** (`NtMapViewOfSection` 이 직접 씀). 켜짐 + 패치 있음 둘 다여야 탄다. test.exe 는 정적으로 못 정함 (사진 쪽 흔적은 "안 탐") | **작게 틀림**: 진짜는 꺼짐이어도 `LdrpNtdllHotPatchContext` 에 스택 주소가 써진다 |

## Q66 — 부모 쪽 작은 정적 둘

### ① `0x7ffe0308` 칸 이름 · bit 0 · 누가 쓰나 · 사진 값

- **칸 이름·크기** `[정적]` (PDB 타입 `_KUSER_SHARED_DATA`, 크기 `0xa80`, 두 PDB 가 같음):
  - `+0x308  SystemCall  unsigned long` (4바이트). 비트 이름 없음.
  - 이웃: `+0x2f8 TestRetInstruction` (8B), `+0x300 QpcFrequency` (8B), `+0x30c Reserved2` (4B), `+0x310 FullNumberOfPhysicalPages`.
  - 근거: `python3 .claude/skills/ghidra-headless-export/scripts/pdb_struct.py ntdll.dll _KUSER_SHARED_DATA` (ntoskrnl PDB 도 같은 칸).
- **bit 0 뜻** `[정적]`: ntdll 의 모든 시스템 콜 스텁이 `TEST byte [0x7ffe0308],1` 만 본다.
  - 근거: `ms/disassemble/ntdll/NtCreateUserProcess.asm` `+0x8 TEST` → `+0x10 JNZ` (목적지 `0x1801627d5` − 시작 `0x1801627c0` = `+0x15`) → bit 0 이 **꺼져 있으면 `+0x12 SYSCALL`**, **켜져 있으면 `+0x15 INT 0x2e`**.
  - `NtAlpcSendWaitReceivePort.asm`·`NtManageHotPatch.asm`·`NpClose.asm` 도 같은 모양 (번호만 `0x8e`·`0x11a`·`0xf`).
  - ntdll 전체 `objdump` 에서 `0x7ffe0308` 을 읽는 곳은 **494곳, 전부 24바이트 스텁** (`_index.tsv` 크기로 확인). kernel32·user32 는 0곳. 스텁 밖에서 이 칸을 읽는 ntdll 코드는 없다.
- **누가 쓰나 (커널 쪽)**: **못 찾았다** `[정적, 한계 있음]`.
  - `ntoskrnl.exe` 를 `objdump -d` 로 전부 훑었다. KUSD 커널 주소 `0xfffff78000000XXX` (`XXX` = `0x000 ~ 0xfff`) 를 `movabs` 로 레지스터에 올리는 곳이 **1026곳**. 각각 뒤 150줄 안에서 "그 레지스터 + 알맞은 오프셋" 이 `+0x308` 이 되는 읽기·쓰기를 찾았더니 **0곳** (한 곳 걸린 `rva 0xb2e63f` 는 `+0x300` 에 8 을 더한 게 아니라 별개 표 순회 (`lea rcx,[rcx+8]`) 라서 가짜). 직접 주소 `0xfffff78000000308` 도 0곳, 유저 주소 `0x7ffe0308` 도 0곳.
  - 같은 방법은 다른 칸 (`+0x300` `QpcFrequency` 읽기, `+0x320` `TickCount` 읽기 등) 에서는 많이 걸린다. 그래서 "방법이 아예 못 보는 칸" 은 아니다.
  - KUSD 페이지를 만들고 매핑하는 `MiInitializeSharedUserData` (rva `0xc5c878`, 590B) 는 풀 할당 (`ecx = 0x112`, 태그 `0x74536d4d`)·PTE 조회·매핑 호출 (`0x140462160`) 만 하고 칸을 채우는 쓰기는 없다 (`objdump` 로 읽음).
  - 한계: 레지스터를 여러 번 옮기거나, 포인터를 인자로 넘겨 다른 함수가 쓰는 길은 못 잡는다. "이 빌드에서 이 칸을 쓰는 곳 없음" 이라고는 못 쓴다. "방법으로는 안 보임" 까지. 이름 후보는 못 댄다.
- **사진 값** `[관찰]` (run44, `ms/logs/04_baggage-read_*.txt`):
  - test.exe 컨텍스트에서 유저 주소 `dt nt!_KUSER_SHARED_DATA 0x7ffe0000`: P1a `:2954`, P1b `:4072`, P2 `:4084` 모두 `+0x308 SystemCall : 0`. (P0·P1·K1~K3 는 유저 쪽이 `??` 라 못 읽음.)
  - 커널 주소 `fffff78000000000` (`13s KUSD_KERNEL`): P0·P1·P1a·P1b·P2·K1·K2·K3 **8장 전부** `+0x308 SystemCall : 0` (`04_baggage-read_supp_<사진>.txt:246`, 각 8파일).
  - 값 0 → `JNZ` 안 탐 → **`SYSCALL` 길** `[정적+관찰]`. 이 사진은 디버거 붙은 상태이고, 이 칸이 디버거 때문에 바뀐다는 증거는 못 봤다.
- 지금 PE_emulator: **맞다.** `Src/nt/AddressSpace.cpp:505` 가 `SystemCall` 0 으로 둔다 (`uc_mem_map` 0 채움). 쓰는 코드를 안 만드는 것도 위 결과와 모순 없음.

### ② `CsrClientCallServer` — 기다리나, 답 status 는 어디서, 실패는

함수: `ntdll!CsrClientCallServer` rva `0xdbdd0`, 449B. 인자는 코드로 읽은 것 (이름은 우리가 붙임): `RCX` = 메시지 칸 (`RBX`), `RDX` = 캡처 버퍼 (`RDI`, 널 가능), `R8D` = API 번호, `R9D` = 데이터 길이.

- **보내고 기다린다 (ALPC 동기 요청)** `[정적]`. 호출 하나: `+0xed → ret +0xf2 NtAlpcSendWaitReceivePort` (syscall `0x8e`). 인자를 `.asm` 에서 센 값:

  | 인자 | 값 | 근거 줄 |
  |---|---|---|
  | 포트 핸들 | `CsrPortHandle` (rva `0x1d4e20`) | `+0xbb MOV RCX,[…]` |
  | Flags | **`0x20000`** | `+0x48 MOV R10D,0x20000` → `+0xdc MOV EDX,R10D` |
  | 보내는 메시지 | 메시지 칸 (`RBX`) | `+0xd4 MOV R8,RBX` |
  | 보내는 속성 | 없음 (0) | `+0xcc XOR R9D,R9D` |
  | **받는 메시지** | **같은 칸 (`RBX`)** | `+0xdf MOV [RSP+0x20],RBX` |
  | 받는 버퍼 크기 칸 | `[RSP+0x40]` = **`0x3b8`** | `+0xc2 LEA RAX,[RSP+0x40]` · `+0xd7` · `+0xe4 MOV [RSP+0x40],0x3b8` |
  | 받는 속성 · 시간 제한 | 0 · 0 (**시간 제한 없음**) | `+0xc7 [RSP+0x38]=0` · `+0xcf [RSP+0x30]=0` |

  - `0x20000` 이 "동기 요청" 이라는 **이름** 은 공개 헤더 자료 (`ALPC_MSGFLG_SYNC_REQUEST`) 에서 빌린 것이다 `[추측: 이름]`. 이 PDB 엔 열거가 없다. **값은 `[정적]`.** 보내는 칸과 받는 칸이 같고 시간 제한이 없으니 "보내기만" 일 수 없다 — 답이 와야 돌아온다.
  - 공개 헤더 기준 "알림 가능 대기" 비트 (`0x200000`) 나 "사용자 모드 대기" 비트 (`0x100000`) 는 값에 없다 → 대기는 알림 가능이 아닌 보통 대기로 보인다 `[추측: 비트 이름]`.
- **답 status 는 메시지 `+0x34`** `[정적]`. 메시지 모양 (`.asm` 에서 읽은 칸): `+0x0` 길이 둘 (`+0x4e~+0x5a`: 데이터 길이 = `R9+0x18`, 전체 = `R9+0x40`), `+0x28` 캡처 버퍼 (원격 주소), `+0x30` API 번호 (`+0x51 MOV [RBX+0x30],R8D`), **`+0x34` 반환 status**.
  - 돌아오는 길: `+0xf2 MOV R8D,EAX` (ALPC 호출 결과) → `+0x12c TEST R8D,R8D` → 음수가 아니면 `+0x12f JNS → +0x135` → **`+0x135 MOV EAX,[RBX+0x34]`** → `+0x147 RET`.
  - **ALPC 호출 자체가 실패하면** (음수) `+0x131 MOV [RBX+0x34],R8D` 로 그 값을 `+0x34` 에 넣고 같은 `+0x135` 로 간다. 그래서 반환값은 "csrss 가 쓴 status" 또는 "ALPC 실패 status" 둘 중 하나, 항상 `+0x34` 안에 있다.
  - csrss 쪽은 이름만 (`csrsrv!CsrApiRequestThread` rva `0x9340`): 메시지가 `[RSP+0xb0]` 에 있고, API 번호로 고른 처리기를 간접 호출 (`+0x4e1 / ret +0x4e6`, `+0x651 / ret +0x656`) 한 뒤 **`+0x656 MOV [RSP+0xe4],EAX`** (= `0xb0 + 0x34`) 로 `+0x34` 에 쓴다. 모르는 번호는 `+0x751` · `+0x790` 에서 `0xc00000af`. 두 쪽이 `+0x34` 로 맞는다.
- **실패 status 를 돌려주나**: 예. 그리고 **부모 (`CreateProcessInternalW`) 는 반환값 `EAX` 를 쓰지 않고 `+0x34` 칸을 직접 다시 읽는다** `[정적]`:
  - `kernelbase!CreateProcessInternalW` `+0x4655 R8D=0x1001d` · `+0x464f R9D=0x218` · `+0x4663 LEA RCX,[RSP+0xbe0]` (메시지) · `+0x466b CALL [0x18029ad00] → ret +0x4672 CsrClientCallServer`.
  - `+0x4677 MOV ECX,[RSP+0xc14]` (= `0xbe0 + 0x34`) → `+0x4680 JNS +0x469a`. 음수면 `+0x4682 → +0x4687 BaseSetLastNTError` 를 부르고 `+0x4695 JMP → +0xb61` (실패 끝맺음). 즉 csrss 알림이 실패하면 `CreateProcess` 가 실패로 끝난다.
- **갈림길 둘 (test.exe 부모에서는 안 탐, 코드에는 있음)** `[정적]`:
  - `+0x14 CMP [LdrpIsSecureProcess],0` → `+0x1e JNZ → +0x1b8 MOV EAX,0xc00000bb` 반환 (보내지 않음).
  - `+0x6f CMP [CsrClientProcess],0` → `+0x76 JZ → +0x163`: **0 이면 ALPC 를 안 쓰고** 자기 프로세스 안 `CsrServerApiRoutine` 을 `indirect:cfg` 로 부른다 (`+0x182 / ret +0x187`, 인자 `TEB+0x40` 의 `ClientId` 16B 를 `[msg+8]` 에 복사). csrss 자신·네이티브 프로세스용.
  - `CsrClientProcess` 는 `CsrClientConnectToServer +0x8c / +0x8f` 가 **1 로 쓴다 (PE 헤더 `Subsystem` ≠ 1 일 때, `+0x81 CMP word [NT+0x5c],1`)**. test.exe `Subsystem = 3` (CUI, `sample/bin/test.exe` 헤더) → 부모가 CUI 면 1 → ALPC 길. (부모 = cmd.exe 등 CUI·GUI 프로세스 `[추측]`, 실제 부모는 우리가 안 봄.)
  - PEB `+0x3` bit 1 (`IsProtectedProcess`) 가 켜진 프로세스는 `+0x69 JNZ → +0x149` 로 간다: API 번호 상위 16비트 (`R8D & 0xffff0000`) 가 `0x20000` (서버 DLL 번호 2) 면 `+0x155 [msg+0x34]=0xc0000022` 로 바로 거절하고, 아니면 (`+0x153 JNZ → +0x19c`) 원래 API 번호의 bit 28 (`+0x2c AND ECX,0x10000000`) 과 PEB `+0x3` bit 6 (`IsProtectedProcessLight`) 를 더 보고 계속 가거나 같은 거절로 간다. 부모가 보호 프로세스일 일은 없다고 본다 `[추측]`.
- 캡처 버퍼 (`RDX ≠ 0`): `+0x81 CsrPortMemoryRemoteDelta` 를 읽어 `+0x8c~+0xb9` 에서 캡처 버퍼 안 포인터들 (`+0x10` 개수, `+0x20` 배열) 을 서버 쪽 주소로 바꾼 뒤 보내고, 돌아와 `+0xfa~+0x12a` 에서 되돌린다. `CreateProcessInternalW` 는 `RDX = [RSP+0x2c0]` 를 줌.
- 이 요청서에 대한 기존 `[추측]` (flow.md 2124줄 "부모는 `CsrClientCallServer` 에서 답을 받을 때까지 기다린다") 은 **`[정적]` 으로 올려도 된다.** 다만 "csrss 등록이 `NtResumeThread` 보다 먼저 끝난다" 는 **한 단계 더 나간 말이라 코드로는 못 정함** (부모 `CreateProcessInternalW` 안 `NtResumeThread` 위치와 `+0x4672` 의 앞뒤는 이 요청서에서 안 봄 `[확인 필요]`).
- 지금 PE_emulator: **해당 없음 (의도).** `Src/main.cpp:268~269` 가 "부모·csrss·스케줄러 없음" 으로 뺀다. 에뮬레이터가 읽는 쪽 (`CsrClientProcess` rva `0x1d4df8` 값) 은 `BaseDllInit.cpp:1139~1140` 이 이미 `CsrClientCallServer +0x6f` 를 근거로 둠 — 이번 결과와 같다.

## Q67 — ntdll 항목 주변 작은 정적 셋

### (a) `LdrpInitializeProcess +0x415/+0x41a RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)` 가 맞나

- **맞다** `[정적]`. `ms/disassemble/ntdll/LdrpInitializeProcess.asm`:
  - `+0x2d MOV RSI,RDX` — `RSI` = 2번째 인자 = 커널이 넘긴 ntdll 시작 (sast/55 가 `+0x684` 에서 `LdrpSystemDllBase` 에 저장되는 걸로 확인).
  - `+0x3f9 MOV RDX,RSI` · `+0x3fc LEA ECX,[R8+3]` (`R8D = 0` 이라 `ECX = 3`) · `+0x400 → +0x405 RtlImageNtHeaderEx(3, RSI, 0, &[RSP+0x170])`.
  - `+0x40d MOV EBX,[RAX+0x50]` — NT 헤더 `+0x50` = `OptionalHeader.SizeOfImage` (ntdll 은 `0x267000`, sast/55).
  - `+0x410 MOV EDX,EBX` · `+0x412 MOV RCX,RSI` · **`+0x415 CALL → ret +0x41a RtlInsertInvertedFunctionTable(RCX = ntdll 시작, EDX = SizeOfImage)`.**
  - `RtlInsertInvertedFunctionTable.asm`: `+0x29 → +0x2e RtlImageDirectoryEntryToData(RCX, 1, 3, …)` (3 = 예외 디렉터리) → `+0x44` 락 → `+0x4b` `LdrProtectMrdata(0)` → `+0x5d → +0x62 RtlpInsertInvertedFunctionTableEntry` → `+0x67` `LdrProtectMrdata(1)` → `+0x87` 꼬리 `RtlReleaseSRWLockExclusive`.
  - 이 구간의 나머지 (`+0x43a Lookup`, `+0x44e~+0x467` 칸 0 에 복사, `+0x470 Remove`) 는 **sast/55 가 이미 표로 닫았다.** 여기서 다시 안 풀고 가리키기만.
- **어느 줄이 먼저인가** `[정적]`: 코드 순서도 실행 순서다 (이 구간은 갈림길 없이 위에서 아래로 내려감).
  1. **먼저 `+0x415`** — 표에 ntdll 을 등록.
  2. **한참 뒤 `+0x119d → ret +0x11a2 LdrpInitializeNtdllDataTableEntry`** (둘째 인자에 `&LdrpNtDllDataTableEntry`, `+0x1195 MOV RCX,[RSP+0xd8]` = ntdll 시작) — ntdll 의 모듈 항목을 만든다. 그 안 `+0x93 OR [RBX+0x68],0x204` 가 **비트 2 (`ImageDll`) + 비트 9 (`InExceptionTable`, 0x200)** 를 켠다 (`_LDR_DATA_TABLE_ENTRY.Flags` PDB 비트 표). `ImageDll` 은 `LdrpAllocateModuleEntry` 가 이미 켜 두므로 새로 켜지는 건 `InExceptionTable` 하나 (sast/30).
  3. 같은 함수 안 `+0x11f → ret +0x124 LdrpProcessMappedModule(entry, 0, 1)` 가 부르는 `LdrpProcessMappedModule +0x15e BT EDX,9` / `+0x162 SETNC CL` → 비트 9 가 켜져 있어 `+0x16f JZ → +0x185` 로 **`+0x177 RtlInsertInvertedFunctionTable` 을 건너뛴다**.
  - 두 줄 사이 거리: `+0x415` 와 `+0x119d` 는 `0xd88` 바이트 떨어져 있다.
- **왜 미리 켜나 (코드로 보이는 만큼)**: 일반 DLL 은 `LdrpProcessMappedModule +0x15e/+0x177` 가 "비트 9 가 꺼져 있을 때만" 표에 등록하고 등록 직후 `+0x17f BTS EDX,9` 로 켠다. ntdll 은 표 등록을 이미 `+0x415` 에서 손으로 했으므로, 항목을 만들 때부터 비트를 켜 둬서 **같은 이미지를 표에 두 번 넣지 않게** 한다. 이건 코드가 직접 보여 준다.
  - "왜 ntdll 만 일찍 손으로 등록하나" 는 코드로 못 정한다 `[추측]`: 그 사이 `+0x43a Lookup`·칸 0 복사가 먼저 필요하고, 이 구간 `LdrInitState` 는 0 이라 `RtlpxLookupFunctionTable` 이 느린 길 (`NtQueryVirtualMemory`) 을 타는 등 loader 가 아직 준비 중이다 (sast/55). ntdll 항목이 만들어지기 전부터 ntdll 안 예외·되감기가 표를 쓸 수 있게 해 두려는 것으로 보이나 증거는 없다.
- 사진 대조 `[관찰]`: sast/55 가 이미 맞춰 봤다 (run44 P1a `Epoch 6 = 4 + 2`, 칸 0 = ntdll).
- 지금 PE_emulator: **맞다.** `Src/ntdll/LdrInit.cpp:1419` 가 `RtlInsertInvertedFunctionTable` 을 `LdrpInitializeProcess +0x415` 근거로 부른다.

### (b) `LdrpInitializeProcess +0x1625 LdrpInsertDataTableEntry` 가 끼우는 항목 — `[RSP+0x60]` 은 무엇인가

- **정체: `AvrfpLoaderEntry` (ntdll `.data` 전역, rva `0x1d4520`) 의 복제본** `[정적]`. exe 항목도 ntdll 항목도 아니다.
  - `AvrfpLoaderEntry` 는 App Verifier 가 `verifier.dll` 을 **loader 가 준비되기 전에** 직접 매핑하면서 (`AvrfMiniLoadDll`) 채우는 임시 항목이다. `AVrfInitializeVerifier +0x18b` 가 `[AvrfpLoaderEntry+0x30]` 을 `DllBase` 로 읽는 것으로도 항목 모양과 맞다.
- **그 칸 (`[RSP+0x60]`, 8바이트 포인터) 을 채운 줄까지 거슬러 올라간 길** `[정적]`:
  1. `LdrpInitializeProcess +0xe8 MOV [RSP+0x60],R14` (`R14 = 0`) — 0 으로 시작.
  2. `+0x507 LEA RAX,[RSP+0x60]` · `+0x50c MOV [RSP+0x28],RAX` → `+0x531 → ret +0x536 LdrpInitializeExecutionOptions` 의 **6번째 인자 (출력 칸)** 로 넘어감. (`+0x51b~+0x529` 로 인자 1~4 도 같이.)
  3. `LdrpInitializeExecutionOptions +0x3f` 가 6번째 인자를 `[RBP-0x58]` 에 저장, **`+0x73d MOV RAX,[RBP-0x58]` → `+0x74c MOV [RSP+0x28],RAX` → `+0x75e → ret +0x763 LdrpInitializeApplicationVerifierPackage`** 로 그대로 넘김.
  4. `LdrpInitializeApplicationVerifierPackage +0xc8 MOV RAX,[RSP+0x88]` → `+0xd3 MOV [RSP+0x28],RAX` → `+0xf2 → ret +0xf7 AVrfInitializeVerifier` 로 그대로 넘김.
  5. `AVrfInitializeVerifier` 는 `+0x40 TEST R9D,R9D / +0x43 JZ → +0x2ac` — 4번째 인자가 0 인 **첫째 호출 (`LdrpInitializeApplicationVerifierPackage +0xe7 XOR R9D,R9D`)** 이 이 길을 탄다 (4번째 인자 1 은 `+0x49 CMP R9D,1` 로 공급자 초기화 길, `LdrpInitializeProcess +0x1708 LEA R9D,[R15+1]` 의 호출). `+0x2ac MOV RSI,[RBP+0x58]` (6번째 인자) · `+0x2bf MOV [RSI],R13` (= 0 으로 비움) → **`+0x3d9 LEA R15,[AvrfpLoaderEntry]`** · `+0x3f2 → ret +0x3f7 AvrfMiniLoadDll(…, [RSP+0x20] = R15)` 가 성공하면 **`+0x3fd MOV [RSI],R15`**. 이 줄이 `[RSP+0x60]` 에 `&AvrfpLoaderEntry` 를 쓴다.
  6. 같은 함수 `+0x414 MOV [AvrfAppVerifierMode],R14D` 가 모드 (1 또는 2) 를 쓰는 유일한 줄 (`_refs/AvrfAppVerifierMode.tsv`).
- **`+0x1625` 앞에서 복제하는 줄** (`LdrpInitializeProcess.asm`):
  - `+0x1577 → +0x157c LdrpAllocateModuleEntry(0)` 로 새 항목 `RBX` 를 만들고, `[RSP+0x60]` 에서 칸을 복사한다: `Flags` (`+0x68`, `OR` 로), `OriginalBase` (`+0xf8`), `FullDllName` (`+0x48`, 16B), `BaseDllName` (`+0x58`, 16B), `TimeDateStamp` (`+0x80`), `CheckSum` (`+0x120`), `DllBase` (`+0x30`) — `+0x15b0 ~ +0x161e`.
  - `+0x1625 → ret +0x162a LdrpInsertDataTableEntry(RBX)` (적재·메모리 순서 목록 + 해시 표, sast/30). 이어서 `+0x166b LdrpInsertModuleToIndex`, `+0x1679 LdrpProcessMappedModule(RBX, 0, 1)`, `+0x168f ~ +0x1693 EntryPoint` (`+0x38`) 를 `[RSP+0x60]` 에서 복사, `+0x16b0 DdagNode.State = 7`.
  - 즉 "임시 항목 → 정식 항목으로 갈아끼우기".
- **test.exe 에서 이 줄을 타나**: 아니다 `[정적]`. `+0x1568 CMP [AvrfAppVerifierMode],R15D` (이 자리 `R15D = 0`, `+0x11a2 XOR R15D,R15D` 가 마지막 대입) → `+0x156f JZ → +0x1753` (목적지 `0x18008db57` − 시작 `0x18008c404`) 로 `+0x1577~+0x1750` 전부 건너뜀.
  - 사진과 맞다 `[관찰]` (run44 P1a·P2): `PEB +0xbc NtGlobalFlag = 0` (`P1a.txt:1770`, `P2.txt:2480`), `IFEOKey (null)` (`P2.txt:2452`). `AvrfAppVerifierMode` 를 쓰는 곳은 `AVrfInitializeVerifier` 하나뿐이라 켜진 흔적이 없다.
  - 참고: exe 쪽 `LdrpInsertDataTableEntry` 는 `+0x1450 → ret +0x1455` (sast/30).
- 지금 PE_emulator: **맞다.** `Src/ntdll/LdrInit.cpp:5845~5846` 이 `AvrfAppVerifierMode` 는 안 읽는다고 `+0x1568 · +0x156f` 근거로 적어 두었다.

### (c) `LdrpLoadPatchedNtdll` — `[ctx+0]` 에 무엇을 쓰나, 패치 항목 · `LdrpNtdllHotPatchContext` 연결

함수: rva `0x8b9ac`, 351B. 인자 (이름은 우리가 붙임): `RCX` = ntdll 시작 (`R15`, `LdrpInitializeInternal` 의 2번째 인자), `RDX` = **ctx** (`R14`).

- **ctx 는 16바이트 지역 칸이고, 전역 `LdrpNtdllHotPatchContext` (rva `0x1d2908`) 는 그 주소를 가리킨다** `[정적]`. 부르는 쪽 `LdrpInitializeInternal.asm`:
  - `+0x9f LEA RAX,[RSP+0x30]` · **`+0xa7 MOV [LdrpNtdllHotPatchContext],RAX`** · `+0xae MOVUPS [RSP+0x30],XMM0` (16바이트 0).
  - **`+0xa7` 은 켜짐·꺼짐과 상관없이 첫 스레드가 항상 쓴다.** (`+0x98 CMP [LdrpIsHotPatchingEnabled],0` 의 결과 플래그를 `+0xa4 XORPS` · `+0xa7 MOV` · `+0xae MOVUPS` 가 안 바꿔서 `+0xb3 JZ → +0xc6` 이 그 `CMP` 를 쓴다.)
  - 켜짐이면 `+0xb5~+0xbd → ret +0xc2 LdrpLoadPatchedNtdll(RCX = RDI, RDX = &ctx)` → `+0xc2 MOV [RSP+0x38],EAX` (ctx `+0x8` = 반환 status).
  - **`+0x5e AND [LdrpNtdllHotPatchContext],0`**: `_LdrpInitialize` (`+0x55 / ret +0x5a`) 가 돌아오면 (`+0x5c TEST EBX,EBX / JNZ +0x66` 은 첫 스레드라 안 탐) 다시 0 으로 비운다. 그러니 이 전역은 **초기화 도중에만** 스택 주소를 가진다.
- **ctx 칸 뜻** `[정적]`:

  | 칸 | 크기 | 쓰는 줄 | 뜻 |
  |---|---|---|---|
  | `+0x0` | 8B | `LdrpLoadPatchedNtdll +0x19` (0 으로) · **`LdrpLoadCustomNtdllWithSectionHandle +0x14` (0 으로) → `+0x49 NpMapViewOfSection` 의 `BaseAddress` 출력 칸으로 `&ctx[0]` (`+0x18 MOV R8,RDX`)** · 실패 때 `LdrpLoadPatchedNtdll +0x157` · `…WithSectionHandle +0x9d` (0 으로) | **패치 ntdll 이미지가 매핑된 시작 주소** |
  | `+0x8` | 4B | `LdrpInitializeInternal +0xc2` | `LdrpLoadPatchedNtdll` 반환 status |
  | `+0xc` | 1B | `LdrpLoadPatchedNtdll +0x57 MOV [R14+0xc],CL` | "패치가 있다" 표시 (`LdrpQueryCurrentPatch` 결과) |

- **`LdrpLoadPatchedNtdll` 이 `[ctx+0]` 에 직접 쓰는 건 0 뿐이다** (`+0x19`, 실패 때 `+0x157`). 진짜 값은 호출한 `LdrpLoadCustomNtdllWithSectionHandle` (rva `0x15e064`) 이 쓴다:
  - `+0x49 NpMapViewOfSection(RCX = 섹션 핸들, RDX = -1 (현재 프로세스), R8 = &ctx[0], R9 = 0, …)` — 스택 인자: 보기 크기 칸, 상속 방식 `1`, 할당 종류 `0`, **보호 `0x80`** (`[RAX-0x10]`). `NpMapViewOfSection` 은 `NtMapViewOfSection` 과 같은 syscall 번호 `0x28` 의 ntdll 자체 스텁 (`NpClose`=`0xf`, `NpUnmapViewOfSection`=`0x2a` 도 같은 짝).
  - 결과가 `0x40000003` (`+0x50 CMP`) 이면 `+0x5a → LdrpProtectAndRelocateImage([ctx])` (다시 배치). 이어 `+0x7a RtlImageNtHeaderEx(1, [ctx], 0, …)` 로 헤더 검사, 실패면 `0xc000007b` 로 `+0x98 → NpUnmapViewOfSection` 후 `+0x9d [ctx] = 0`.
- **`LdrpLoadPatchedNtdll` 전체 순서** `[정적]` (`.asm` 줄):
  1. `+0x19 [ctx+0] = 0`. **`+0x49 → ret +0x4e LdrpQueryCurrentPatch(RCX = ntdll 시작, RDX = &[RBP+0x38])`**. 안에서 `NtManageHotPatch(클래스 8, …, 크기 0x30)` (`LdrpQueryCurrentPatch +0x58 → +0x5d`) 를 불러 결과로 표시를 정한다: status `0xc0000023` → 표시 **1** (`+0xae → +0x71`), status `0` · `0xc0000018` (`LdrpIsImageAddress` 확인 뒤) → 표시 **0**, 그 밖 → 표시 0 + `0xc00004ac`.
  2. `+0x54~+0x57` 표시를 `[ctx+0xc]` 에 복사. **`+0x5b TEST CL,CL` / `+0x5d JNZ → +0x8b`**: 표시가 0 이면 여기서 끝 (패치 없음, `[ctx+0]` 는 0 그대로, status 0).
  3. 표시 1 이면 `+0x8b~+0xc4 NtManageHotPatch(클래스 0xa, 요청 칸 0x28B, …)` (`+0xb5 LEA ECX,[R8-0x1e]`, `R8D = 0x28`) — 요청 칸에 `+0x18 = ntdll 시작 & ~3` (`+0x9b AND RAX,-4`) 를 담아 보내고 출력 `[RBP-0x10]` 에 **섹션 핸들** 을 받는다.
  4. `+0xcf~+0xe4` `TEB +0x28 (ArbitraryUserPointer)` 을 저장 → 0 으로 → `+0xe8 → ret +0xed LdrpLoadCustomNtdllWithSectionHandle(섹션 핸들, ctx)` → `+0xed` 복원 (PDB `_NT_TIB +0x28`).
  5. `+0xfb~+0x12c NtManageHotPatch(클래스 7, 요청 0x20B = {2, 3, -1, 원본 ntdll 시작, 패치 시작 `[ctx+0]`})` (`+0x128 LEA ECX,[R8-0x19]`, `R8D = 0x20`) — 커널에 "이 패치 이미지를 이 원본에 적용" 알림으로 보인다 `[추측: 뜻]`, 숫자·칸은 `[정적]`.
  6. 끝맺음 `+0x5f`: 섹션 핸들이 있으면 `+0x138 NpClose`, status 가 음수면 `+0x142~+0x157` `NpUnmapViewOfSection(-1, [ctx])` 후 `[ctx] = 0`.
- **패치 항목 · `LdrpNtdllHotPatchContext` 와의 연결** `[정적]` (읽는 쪽 셋 — `_refs/LdrpNtdllHotPatchContext.tsv`):
  - `LdrpInitializeProcess +0x1233 MOV RAX,[ctx전역]` · `+0x123a CMP [RAX+8],R15D(0)` · 음수이고 `+0x1240 CMP [LdrpIsHotPatchingEnabled],0` 이 켜짐이면 **`+0x124e INT 0x29`** (코드 `0x45`) — 켜졌는데 패치 로딩이 실패하면 프로세스를 죽인다.
  - 이어서 `+0x1250 MOV RCX,[RAX]` · `+0x1253 TEST` · `+0x1256 JZ → +0x1277` (`[ctx+0] = 0` 이면 건너뜀) → 아니면 **`+0x126a → ret +0x126f LdrpInitializeNtdllDataTableEntry(RCX = 패치 시작, RDX = &[RSP+0x160], R8 = [LdrpNtDllDataTableEntry], R9 = 0)`** 로 **패치 ntdll 의 모듈 항목** 을 만든다 (sast/30 질문 5). 그 안 **`LdrpInitializeNtdllDataTableEntry +0x1b7 MOV RDX,[ctx전역]` → `+0x1dc MOV RDX,[RDX]` → `+0x1df NtQueryVirtualMemory(-1, 패치 시작, 클래스 2, …)`** 로 패치 이미지의 파일 이름을 얻어 `+0x1fe LdrpResolvePatchDllName` 에 넘긴다.
  - `+0x1283~+0x12d7` 초기화 순서 목록 (`PebLdr+0x30`) 에 ntdll 항목의 `+0x20` 링크를 머리에 끼우고 (`[머리]=새 링크`), 패치 항목 (`[RSP+0x160]`) 이 있으면 그 링크도 머리에 끼운다 (그러면 패치 항목이 ntdll 앞) `[정적: 대입 읽음]`.
  - `+0x18ef MOV R9,[ctx전역]` · `+0x18f6 CMP [R9+0xc],DL(0)` · `+0x18fa JZ → +0x191d`: **`[ctx+0xc]` (패치 있음) 가 1 이면** `+0x1904 R9D=[R9+8]` (status) 로 `+0x1916 → ret +0x191b LdrpLogEtwHotPatchStatus` 를 부른다 (ETW 기록).
  - 정리: `[ctx+0]` (패치 시작) → 패치 모듈 항목 (`+0x126a`), `[ctx+8]` (status) → 죽일지 판단 (`+0x123a`) · 기록 (`+0x1900`), `[ctx+0xc]` → 기록 여부 (`+0x18f6`).
- **test.exe 에서 타나 (조건 줄 하나)**: **정적으로 못 정한다.** 조건 줄은 **`LdrpInitializeInternal +0x98 CMP byte [LdrpIsHotPatchingEnabled],0` / `+0xb3 JZ → +0xc6`**. 켜지려면 둘 다 필요:
  1. 이 전역 (rva `0x1d26f0`, 1B) 이 1. 쓰는 곳은 둘:
     - `LdrpInitializeHotPatching +0x55 MOV [flag],BL` (`+0x18 TEST [ProcessParameters+8],0x2000000` 이면 `+0x53 XOR BL,BL`; 아니면 `NtManageHotPatch(클래스 9, 요청 {1}, 크기 8)` (`+0x3d LEA ECX,[RBX+8]`, `+0x39 LEA R8D,[RBX+7]`) 의 결과가 **`0xc00000bb` 또는 `0xc0000002` 이면 0, 그 밖엔 (성공 포함) 1**: `+0x40 → ret +0x45`, `+0x45 CMP 0xc00000bb`, `+0x4c CMP 0xc0000002`).
     - `LdrpInitializeProcess +0x587 MOV [flag],R14B` (= 0): `+0x57f CMP [LdrpImageExpansionMitigation],2` 이면 끈다 (이 줄은 패치 로딩이 끝난 **뒤**).
  2. 켜져도 `LdrpQueryCurrentPatch` 가 표시 1 을 줘야 (`+0x5b`) 다음으로 간다. 즉 **ntdll 에 패치가 있어야** 한다.
  - 커널 쪽 (참고, `objdump` 로만 읽음 — 확인 필요): `nt!NtManageHotPatch` (rva `0xa47e60`, 1701B) 가 입구에서 전역 dword (rva `0xe375f4`) bit 0 을 보고, **꺼져 있으면 모든 클래스에 `0xc00000bb`** 를 돌려준다 (`+0xad~+0xb5 test al,1 / je`). 그 bit 는 부팅 때 `MiInitializeHotPatches +0x82` (rva `0xc64c92`, `or [0xe375f4],1`) 가 켠다 (조건: 위쪽에서 `edx = [rax+0xda8]` 를 페이지 올림한 값이 0 이 아니고, 그리고 (전역 rva `0xfc6ac8` 의 bit 14 가 켜졌거나 전역 rva `0xfc5260` 이 1)). 입력 값들이 무엇이고 test.exe 의 VM 에서 얼마인지는 안 읽었다 → 동적.
  - 사진 쪽 흔적 `[관찰]` (run44 P2, `ms/logs/04_baggage-read_P2.txt`): PEB `CrossProcessFlags.ProcessImagesHotPatched = 0` (`:2461`), EPROCESS `MmHotPatchContext (null)` (`:298`), 모듈 항목 6개 모두 `HotPatchState 0 (LdrHotPatchBaseImage)` (`:3455`, `:3560`, … `:3980`). **패치가 하나도 적용되지 않았다** 는 쪽. 다만 `LdrpIsHotPatchingEnabled` 자체 값은 사진에 없다 (켜졌는데 패치가 없을 수도 있음).
  - 어느 쪽이든 test.exe 에선 `LdrpLoadPatchedNtdll` 이 `[ctx+0]` 를 0 으로 둔 채 끝나거나 아예 안 불린다고 본다 `[정적 + 관찰, 추정]`. `+0x126a` 패치 항목도 안 만들어진다.
- 지금 PE_emulator: **작게 틀림 (동작은 같음).** `Src/ntdll/LdrInit.cpp:7084~7092` 주석은 "`LdrpNtdllHotPatchContext` 안 씀 … `+0x5a ~ +0x5e` 문맥 비우기도 안 함: 위에서 안 써 이미 0" 이라 한다. 실제는 **켜짐 여부와 상관없이 `+0xa7` 이 스택 주소를 쓰고 `+0x5e` 가 비운다.** 읽는 쪽 셋은 그 칸의 16바이트가 0 이라 건너뛰는 것과 결과가 같아서 에뮬레이터 동작은 같다. 달라지는 건 **초기화 도중 그 전역 (rva `0x1d2908`) 의 값 (실제 스택 주소 vs 0)** 뿐이고, 그 사이에 메모리를 찍어 비교할 때만 보인다. 나머지 (`LdrpLoadPatchedNtdll` 안 탐, `LdrpIsHotPatchingEnabled = 0`) 는 맞다.

## 동적으로 확인할 것

1. **`LdrpIsHotPatchingEnabled` 값과 `NtManageHotPatch(9, …)` 결과** (Q67-c, 이미 `LdrInit.cpp` 의 `TODO(VM)` / Q43 (3) 과 같은 질문):
   - 추천 멈춤점: `ntdll!LdrpInitializeHotPatching+0x45` (ret 직후, `EAX` = status), 또 `+0x55` 직후 `db ntdll+0x1d26f0 L1`.
   - 같이: `ntdll!LdrpQueryCurrentPatch+0x5d` 에 오는지·`EAX` (켜짐일 때만 옴).
2. **`LdrpNtdllHotPatchContext` 도중 값** (위 "작게 틀림" 확인): `ntdll!LdrpInitializeProcess+0x1233` 에서 `dq ntdll+0x1d2908 L1` — 비어 있지 않은 스택 주소여야 하고 `dq poi(ntdll+0x1d2908) L2` 가 0 이어야 한다. 읽기 쪽 (`+0x1233`) 이 이미 NtContinue 은퇴 전이라 사진 (P-시리즈) 에 안 잡혔을 수 있다.
3. **`CsrClientCallServer` 가 정말 기다린다 · 돌아온 `+0x34` 값** (Q66-②): 부모 프로세스 쪽에서 `ntdll!CsrClientCallServer+0xed` (호출 직전, `R8D = 0x1001d` 일 때만) 와 `+0xf2` (돌아온 직후, `EAX` = ALPC status, `dd @rbx+0x34 L1` = csrss 의 status). 정적으로 본 `+0x34` 를 값으로 확인.
4. (선택, Q66-①) `nt+0xe375f4` bit 0 · `ntdll+0x1d4df8` (`CsrClientProcess`) · `ntdll+0x1e7044` (`AvrfAppVerifierMode`, 0 이어야) 한 번 읽기. `SystemCall` 이 0 이 아닌 환경 (예: 다른 VM 설정) 이 있는지는 이 요청서로 알 수 없다.

## 새로 생긴 공백 · 다음 요청 후보

- (정적, 작게, **sast/98 이 끝난 뒤**) `nt!NtManageHotPatch` (rva `0xa47e60`) 클래스 8·9 갈래 반환값과 `MiInitializeHotPatches` 가 켜는 전역 (`0xe375f4` bit 0) 의 입력 둘 (`0xfc6ac8` bit 14, `0xfc5260`). Q67-c 에서 test.exe 가 켜짐인지 꺼짐인지가 정적으로 갈리는지. ntoskrnl Ghidra 프로젝트 잠금이 풀린 뒤.
- (정적, 작게) KUSD `+0x308` 을 쓰는 커널 코드가 정말 없는지: Ghidra `refs` 가 `0xfffff78000000308` 직접 참조는 못 잡는 점 (레지스터 기준) 때문에 `objdump` 훑기만 했다. 필요하면 `KiInitializeKernel`·`KiInitializeBootStructures` 같은 부팅 쪽을 읽어 보자. 우선순위 낮음 (8장 전부 0).
- (정적, 작게) `AvrfMiniLoadDll` (rva `0xe094c`) 가 `AvrfpLoaderEntry` 를 채우는 줄 — verifier 를 켠 프로세스만 해당, test.exe 와 무관. 필요 없으면 건너뜀.
- (정적) `CreateProcessInternalW` 에서 `+0x4672` (csrss 응답) 와 `NtResumeThread` 의 앞뒤 순서 — flow.md 2124 의 "csrss 등록이 `NtResumeThread` 보다 먼저" 를 `[정적]` 으로 올리려면 필요 (이번엔 안 봄).

## 판정

**분석성공.** 질문 5개 모두 `.asm` 줄·PDB 타입으로 답했다. Q66-① 의 "쓰는 커널 코드" 는 `objdump` 방법으로 **안 보임** 까지만 (한계 적음). Q67-c 의 test.exe 켜짐 여부는 정적으로 못 정해 동적 항목으로 넘겼다. PE_emulator 와 어긋난 곳은 `LdrpNtdllHotPatchContext` 한 칸 (초기화 도중 값) 하나, 동작 영향 없음.

## 오프셋 대조 메모 (스스로 확인)

- `CsrClientCallServer.asm`: `+0xed → +0xf2`, `+0x182 → +0x187`, `+0x1e JNZ 0x1800dbf88` = `+0x1b8`, `+0x76 JZ 0x1800dbf33` = `+0x163`, `+0x12f JNS 0x1800dbf05` = `+0x135`, `+0x69 JNZ 0x1800dbf19` = `+0x149` 모두 (목적지 − `0x1800dbdd0`) 로 다시 계산.
- `kernelbase!CreateProcessInternalW.asm`: `+0x466b → +0x4672`, `+0x4677 [RSP+0xc14]` = `0xbe0 + 0x34`, 실패 끝맺음 `0x18007e241` − `0x18007d6e0` = `+0xb61`.
- `LdrpInitializeInternal.asm`: `+0x32 JNZ 0x18008beee` = `+0x66`, `+0x45 JZ 0x18008beff` = `+0x77`, `+0xb3 JZ 0x18008bf4e` = `+0xc6`, `+0xdf JMP 0x18008bed7` = `+0x4f`.
- `LdrpInitializeProcess.asm`: `+0x156f JZ 0x18008db57` = `+0x1753`, `+0x18fa JZ 0x18008dd21` = `+0x191d`.
- `LdrpLoadPatchedNtdll.asm`: `+0x5d JNZ 0x18008ba37` = `+0x8b`, `LdrpQueryCurrentPatch.asm`: `+0x62 JZ 0x18008bba4` = `+0x90`, `+0x69 JZ 0x18008bbc2` = `+0xae`, `+0xb0 JMP 0x18008bb85` = `+0x71`, `+0xac JMP 0x18008bb83` = `+0x6f`.

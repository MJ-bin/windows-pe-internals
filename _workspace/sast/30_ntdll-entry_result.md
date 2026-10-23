# 30 ntdll-entry — 정적 분석 결과

- 요청서: `_workspace/sast/30_ntdll-entry.md` (Q45)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`). PDB `ntdll.pdb` guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`ntdll.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 셋 다 있음, 돌고 있는 Ghidra 프로세스 없음). 지도(`_map/`)는 요청서가 이미 읽은 것을 썼고 새로 안 만들었다.
- 뽑은 함수 (전부 depth 0): `LdrpInitializeNtdllDataTableEntry` (rva `0x7015c`, 633B), `LdrpInsertDataTableEntry` (`0xb380`, 274B, 다시 뽑아 덮어씀), `LdrpInsertModuleToIndex` (`0x703dc`, 61B) → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`.
  - 요청서는 본체를 depth 1 로 적었지만, callee 는 이미 파일이 있거나 (`LdrpAllocateModuleEntry`, `LdrpInsertModuleToIndexLockHeld`, `LdrpResolvePatchDllName`, `LdrpRecordModuleDependency`, `LdrpProcessMappedModule`) 이름·인자만 보면 되는 것이라 depth 0 으로 뽑고 그 파일들을 읽었다.
  - 로그: `ms/ghidra/ntdll.export30.stdout`.
- 버전 확인: **일치.** `LdrpInitializeProcess` call `+0x1bb6` / ret **`+0x1bbb`** → `LdrpInitializeGraphRecurse` (`_calls.tsv`). 요청서의 호출 줄 15개도 `_calls.tsv` 와 한 줄도 안 다르다 (call `+0x46 … +0x23e`, ret = call + 5).
- 칸 이름·열거 값은 PDB 타입(`llvm-pdbutil-14 dump -types ms/sym/ntdll.dll.pdb`)으로 읽었다: `_LDR_DATA_TABLE_ENTRY`(크기 `0x138`), `_PEB_LDR_DATA`, `_LDR_DDAG_NODE`(크기 `0x50`), `_LDR_DDAG_STATE`, `_LDR_DLL_LOAD_REASON`, `_LDR_HOT_PATCH_STATE`, `Flags` 비트 위치.

## 스크립트 문제

없음.

## 한눈에

- **질문 2: 예.** 적재 순서(`PebLdr+0x10`)·메모리 순서(`+0x20`) 목록에 넣는 곳은 `+0xf2 LdrpInsertDataTableEntry` 가 맞다. 해시 표(`LdrpHashTable`)에도 끼운다. `[추측]` 딱지 뗀다.
  - **단, "꼬리에 끼운다"는 exe 만 빼고다.** 항목이 `LdrpImageEntry`(exe 항목)면 두 목록의 **맨 앞**에 끼운다. ntdll 은 exe 항목이 생기기 전(`LdrpImageEntry` = 0)이라 꼬리 쪽 분기를 탄다. 비어 있는 목록이라 결과는 "하나뿐"이다.
  - 초기화 순서 목록(`PebLdr+0x30`)은 이 함수가 아예 안 만진다 (caller `LdrpInitializeProcess +0x1283~+0x12d7`).
- **질문 1a: 새로 할당한다.** `LdrpNtDllDataTableEntry` 는 항목 구조체가 아니라 **포인터 칸(8B)** 이다. 둘째 인자(RDX)는 "새 항목 주소를 돌려받을 칸"이고, 함수 끝 `+0x154` 에서 새 항목을 거기에 쓴다. 중간에 실패하면 안 쓴다.
- **질문 1b:** `FullDllName` = `<RtlGetNtSystemRoot()>\SYSTEM32\` 뒤에 `ntdll.dll` 을 붙인 것. `BaseDllName` = 상수 `NtDllName`(`"ntdll.dll"`) 헤더 복사. 요청서 짐작이 맞다. 다만 넷째 인자는 "문자열"이 아니라 **UNICODE_STRING 의 주소**고, 폴더 이름 상수는 대문자 `\SYSTEM32\` 다.
- **질문 3:** 색인은 **붉은-검은 트리 둘**: `LdrpMappingInfoIndex` (키 = TimeDateStamp·SizeOfImage) 와 `LdrpModuleBaseAddressIndex` (키 = DllBase). `LdrpInsertModuleToIndex` 는 락만 잡아 주는 껍데기.
- **질문 4:** ntdll 도 다른 모듈처럼 "맵 끝난 뒤 마무리" 를 거치게 하려는 호출. 안에서 State=2 를 쓰지만 호출이 끝나면 이 함수가 **`+0x150` 에서 9(`LdrModulesReadyToRun`)로 덮는다.**
- **질문 5: 예.** 둘째 호출은 "패치된 ntdll 이미지" 항목을 만들고 원래 ntdll 항목과 의존 간선을 잇는다. 갈래는 `+0x1ab~+0x23e` 가 한 덩어리가 아니라 **서로 다른 네 길**이다 (아래). 요청서의 "함수 끝 뒤 cold 블록" 짐작은 아니다.
- **질문 6: 맞다.** 핫패치 꺼진 보통 프로세스에서 이 함수 직후 적재·메모리 순서 목록엔 ntdll 하나뿐이다. 핫패치가 켜지면 패치 항목이 같은 함수에서 하나 더 들어간다는 `[추측]` 도 코드로 확인됐다. 이후 exe 가 `+0x1450` 에서 맨 앞에 끼어 순서는 "exe, ntdll" 이 된다.

## 질문별 답 [정적]

### 함수의 모양 (질문 1~5 공통)

인자 이름은 우리가 붙인 것이다 `[추측: 이름만, 쓰임은 코드로 확인]`. 호출 규약은 `.asm` 의 `+0x2b~+0x43` 에서 읽었다 (RCX→R14, RDX→R15, R8→RBP, R9→RDI).

| 인자 | 첫째 호출 (`LdrpInitializeProcess +0x119d`) | 둘째 호출 (`+0x126a`, 핫패치) |
|---|---|---|
| RCX = 이미지 베이스 | ntdll 베이스 (`[RSP+0xd8]`) | `[LdrpNtdllHotPatchContext]` 가 가리키는 칸의 `[+0]` (패치 이미지 베이스) |
| RDX = 새 항목을 돌려받을 칸 (`entry**`) | `&LdrpNtDllDataTableEntry` | `&[RSP+0x160]` (caller 지역 칸. `+0xed` 에서 0 으로 비움) |
| R8 = 원래 ntdll 항목 (패치 모드 표시) | 0 | `[LdrpNtDllDataTableEntry]` (값) |
| R9 = 이름 칸 주소 (`UNICODE_STRING*`) | `&[RSP+0xc0]` | 0 |

- 근거: `LdrpInitializeProcess.asm` `+0x118b`~`+0x119d`, `+0x1250`~`+0x126a`.
- R9 가 가리키는 `[RSP+0xc0]` 는 `LdrpInitializeProcess +0xcdc~+0xd4a` 에서 만든다: `RtlGetNtSystemRoot` → `RtlInitUnicodeStringEx([RSP+0x120])` → `RtlAllocateHeap` (크기 = 루트 길이 + `0x28`) → `MaximumLength` = 그 크기, 길이 0 → 루트 문자열을 붙이고 → 상수 `SlashSystem32SlashString` 을 붙임.
  - `.rdata` 의 그 상수를 PE 에서 직접 읽었다: `\SYSTEM32\` (대문자, 20바이트). `NtDllName` 은 `ntdll.dll` (18바이트).
  - 버퍼 여유 `0x28` = `\SYSTEM32\`(20) + `ntdll.dll`(18) + 널(2). 즉 caller 가 **"뒤에 ntdll.dll 이 붙을 자리"까지 미리 잡아 둔다.**
  - 요청서 `이미 아는 것` 의 "R9 = `<NtSystemRoot>\System32\` 문자열" 은 "그 문자열 칸의 **주소**, 폴더 이름은 `\SYSTEM32\` 대문자" 로 고친다.

### 질문 1. 하는 일 (순서)

`ms/disassemble/ntdll/LdrpInitializeNtdllDataTableEntry.asm`. 첫째 호출 (R8=0, R9≠0) 기준. 갈림길은 괄호.

| 자리 (call / ret) | 하는 일 |
|---|---|
| `+0x46` / `+0x4b` `memset` | 지역 `0x218` 바이트를 0 으로 (패치 갈래에서 파일 이름 받는 버퍼. 첫째 호출은 안 씀) |
| `+0x60` / `+0x65` `RtlImageNtHeaderEx(3, base, 0, &[RSP+0x30])` | ntdll 의 NT 헤더 주소를 `[RSP+0x30]` 에 얻음 (뒤의 색인 호출 둘째 인자) |
| **`+0x67` / `+0x6c` `LdrpAllocateModuleEntry(0)`** | **새 항목 만들기.** 0 이면 `+0x72 JZ +0x189` (실패 갈래, 아래 질문 5) |
| `+0x78~+0x93` | 새 항목 손질: `DdagNode.LoadCount`(`+0x18`) = -1, `ObsoleteLoadCount`(`+0x6c`) = 0xffff, `Flags` \|= `0x204` |
| `+0x9a~+0xa6` | (R8 = 0 일 때만) `OriginalBase`(`+0xf8`) 에서 `LdrSystemDllInitBlock+0x10`(`SystemDllNativeRelocation`) 을 뺌 |
| `+0xad~+0xdc` | (R9 ≠ 0) 이름 채움 (질문 1b). `DllBase`(`+0x30`) = base |
| `+0xe0~+0xe9` | `R14D = 9`. (R8 ≠ 0 이면 `+0x220` 에서 `LoadReason`(`+0x10c`) = 9 를 쓰고 되돌아옴) |
| **`+0xf2` / `+0xf7` `LdrpInsertDataTableEntry`** | **적재·메모리 순서 목록 + 해시 표에 끼움** (질문 2) |
| `+0x104` `LdrpLogDllState(…, 0x14a5)` | 기록 (건너뜀) |
| **`+0x111` / `+0x116` `LdrpInsertModuleToIndex(entry, NT 헤더)`** | 색인 둘에 끼움 (질문 3) |
| **`+0x11f` / `+0x124` `LdrpProcessMappedModule(entry, 0, 1)`** | 맵 끝난 뒤 마무리 (질문 4). 음수면 `+0x12c JS` 로 실패 처리 |
| `+0x13b` `LdrpLogDllState(…, 0x14ae)` | 기록 (건너뜀) |
| (R8 ≠ 0 이면 `+0x143 JNZ +0x22c`) | 패치 갈래 (질문 5) |
| **`+0x150`** `[DdagNode+0x38] = R14D (=9)` → **`+0x154`** `[R15] = 새 항목` | **State = `LdrModulesReadyToRun` 로 만들고, 새 항목을 둘째 인자 칸에 저장** |
| `+0x16c` `__security_check_cookie` → `+0x187 RET` | 끝. 반환값 = 상태 코드 (`EDI`) |

- 새 항목 크기: `LdrpAllocateModuleEntry` 가 `LdrpHeap` 에서 `0x138` 바이트(= `_LDR_DATA_TABLE_ENTRY` 크기) 와 `0x50` 바이트(= `_LDR_DDAG_NODE` 크기)를 받는다 (`+0x23`·`+0x40`, 플래그 `|8` = `HEAP_ZERO_MEMORY` 공개 문서 기준 `[추측]`). 그래서 이 함수가 안 쓴 칸은 0 이다.
- `Flags |= 0x204` = 비트 2 `ImageDll` + 비트 9 `InExceptionTable` (PDB 비트 위치). `ImageDll` 은 `LdrpAllocateModuleEntry +0x11a` 가 이미 켜 놓으므로 **새로 켜지는 건 `InExceptionTable` 하나**다.
  - 왜 켜나: `LdrpProcessMappedModule +0x15e BT EDX,9` 가 이 비트가 꺼져 있을 때만 `+0x177 RtlInsertInvertedFunctionTable` 을 부른다. 그래서 ntdll 항목은 그 호출을 건너뛴다. ntdll 의 예외 표는 다른 곳에서 이미 등록한다고 보인다 `[추측: 등록하는 곳은 이 요청서에서 안 봄]`.
- `DdagNode.LoadCount = -1`, `ObsoleteLoadCount = 0xffff`: 내려가지 않는 모듈 표시로 보인다 `[추측]` (`LdrpRecordModuleDependency +0x2b` 도 `LoadCount == -1` 을 따로 본다).
- 덤: ntdll 항목의 `LoadReason` 은 이 함수가 안 건드리므로 0 (`LoadReasonStaticDependency`) 이다 (`LdrpAllocateModuleEntry` 가 `LoadReason` 에 4 를 쓰는 `+0x16b` 는 `LoadContext` 가 있을 때만 닿는다. ntdll 은 인자가 0 이라 `+0x7d JZ +0xc2` 로 건너뜀).

#### 1a. 새로 할당하나, 전역 칸을 채우나

- **새로 할당한다.** `+0x67 LdrpAllocateModuleEntry(0)` 의 반환(RAX)이 `RBX` 가 되고 이후 모든 쓰기는 `[RBX+…]` 다.
- 둘째 인자(R15)에는 **"새 항목 주소"** 가 저장된다: `+0x154 MOV [R15],RBX` (State=9 를 쓴 바로 뒤). 이 줄은 성공 갈래에서만 닿는다 (실패하면 `+0x159 JS` 로 빠짐).
- 첫째 호출의 R15 는 `&LdrpNtDllDataTableEntry` 라서, 그 전역은 **"ntdll 항목을 가리키는 포인터 변수"** 다. caller 가 `+0x127c MOV RAX,[LdrpNtDllDataTableEntry]; ADD RAX,0x20` 으로 포인터처럼 쓰고, 둘째 호출 R8 도 그 값이다.
- 근거: `.asm` `+0x67`, `+0x6c`, `+0x154`, `LdrpInitializeProcess.asm` `+0x118e`, `+0x127c`. `.c`: `*param_2 = lVar5`.

#### 1b. `FullDllName`·`BaseDllName`

- `FullDllName`(`+0x48`):
  1. `+0xb6 MOVUPS XMM0,[RDI]` → `+0xc7 MOVDQU [RSI],XMM0` (RSI = `RBX+0x48`): R9 가 가리키는 UNICODE_STRING 헤더 16바이트를 **값으로 복사**. 글자는 복사하지 않아서 `Buffer` 는 caller 가 `+0xd08` 에서 잡은 **같은 힙 버퍼**를 가리킨다. `Length`·`MaximumLength` 도 같이 온다.
  2. `+0xcb RtlAppendUnicodeStringToString(&FullDllName, &NtDllName)`: 그 버퍼 뒤에 `ntdll.dll` 을 붙임. 여유는 caller 가 미리 잡아 둠 (위).
  - 결과: `<RtlGetNtSystemRoot() 문자열>\SYSTEM32\ntdll.dll`. 앞 루트 문자열의 실제 값은 동적.
  - caller 쪽 `[RSP+0xc0]` 의 `Length` 는 그대로(폴더까지)라서, 항목의 `FullDllName.Length` 만 18바이트 길다.
- `BaseDllName`(`+0x58`): `+0xd0 MOVUPS XMM0,[NtDllName]` → `+0xd7 MOVDQU [RBX+0x58],XMM0`. 상수 `NtDllName`(`"ntdll.dll"`, 길이 18, ntdll 자신의 `.rdata`)의 **헤더 복사**. 글자 버퍼는 `.rdata` 를 그대로 가리킨다.
- (핫패치 갈래는 다름: 질문 5.)

### 질문 2. 목록에 넣는 곳은 `+0xf2 LdrpInsertDataTableEntry` 인가 — **예**

`ms/disassemble/ntdll/LdrpInsertDataTableEntry.asm` (`+0x0~+0x10e`). `PebLdr` 자리는 `_refs/PebLdr.tsv` 와 같다 (`PebLdr` = ntdll `.data` 전역, rva `0x1d2880`, `_PEB_LDR_DATA` 크기 `0x58`).

| 자리 | 하는 일 |
|---|---|
| `+0x6 TEST [RCX+0x68],0x40` / `+0xd JNZ +0x62` | `Flags` 비트 6 `InLegacyLists` 가 이미 켜져 있으면 **아무것도 안 하고 return** |
| `+0xf~+0x22` | `BaseNameHashValue`(`+0x108`) 가 0 이면 `+0x1d LdrpHashUnicodeString(&BaseDllName)` 로 구해 저장 |
| `+0x28~+0x7a` | **해시 표:** 버킷 = `해시 & 0x1f` (32칸), `LdrpHashTable` 은 칸당 16바이트(LIST_ENTRY). `HashLinks`(`+0x70`) 를 그 버킷 목록 **꼬리**에 끼움 (`+0x3d` 꼬리 읽기, `+0x70~+0x7a` 연결) |
| `+0x69 CMP RBX,[LdrpImageEntry]` → `+0x85 JZ +0xbd` | exe 항목인지 가름 |
| (exe 아님) `+0x87~+0x9e`, `+0xa1~+0xaf`, `+0x4d~+0x57` | **적재 순서:** `PebLdr+0x18`(= `InLoadOrderModuleList` 꼬리)에서 읽은 마지막 항목 뒤에 `InLoadOrderLinks`(`+0x0`) 를 끼우고 `PebLdr+0x18` 갱신. **메모리 순서:** 같은 식으로 `InMemoryOrderLinks`(`+0x10`) 를 `PebLdr+0x28` 꼬리 뒤에 끼움. 둘 다 **꼬리** |
| (exe 항목) `+0xbd~+0x10e` | 두 목록의 **첫 번째 자리**(머리 칸 `PebLdr+0x10`·`+0x20` 바로 뒤)에 끼움: `[PebLdr+0x10].Flink = 항목`, `[PebLdr+0x20].Flink = 항목+0x10` 으로 쓰고 옛 첫 항목의 `Blink` 를 항목으로 바꿈. 둘 다 **머리 쪽** |
| `+0x5e OR [RBX+0x68],0x40` | `InLegacyLists` 켬 |
| `+0x46 MOV ECX,3; INT 0x29` | 목록 이웃이 맞지 않으면 `__fastfail(3)` (`FAST_FAIL_CORRUPT_LIST_ENTRY`, 코드 값 3 은 공개 문서 기준) |

- `PebLdr+0x30`(초기화 순서) 칸을 만지는 줄이 이 함수에 없다 (refs 표의 이 함수 줄은 `+0x10/+0x18/+0x20/+0x28` 뿐).
- ntdll 은 `LdrpImageEntry` 가 아직 0 이라 (`LdrpInitializeProcess +0x1374` 에서 처음 쓴다) `+0x69` 비교가 같지 않아 꼬리 쪽 분기를 탄다. 세 목록 머리는 `LdrpInitializeProcess +0x16d~+0x1a5` 에서 **자기 자신을 가리키게(빈 목록)** 만들고 `Peb.Ldr`(`[PEB+0x18]`) = `&PebLdr` 로 쓴 게 `+0x1c1`, 해시 표 32칸 초기화는 `+0x3b8~+0x3d3` (`ECX = 0x20`, `+0x378`) 이다. 전부 ntdll 항목 만들기(`+0x119d`)보다 앞이다.
- 칸 이름 (PDB): `_PEB_LDR_DATA` `+0x10 InLoadOrderModuleList`, `+0x20 InMemoryOrderModuleList`, `+0x30 InInitializationOrderModuleList`. `_LDR_DATA_TABLE_ENTRY` `+0x0 InLoadOrderLinks`, `+0x10 InMemoryOrderLinks`, `+0x20 InInitializationOrderLinks`, `+0x70 HashLinks`, `+0x108 BaseNameHashValue`.

### 질문 3. `+0x111 LdrpInsertModuleToIndex` 의 색인

`ms/disassemble/ntdll/LdrpInsertModuleToIndex.asm`, `LdrpInsertModuleToIndexLockHeld.asm`.

- 껍데기 `LdrpInsertModuleToIndex(entry, NT 헤더)`: `+0x10~+0x17` `LdrpModuleDatatableLock` 을 `RtlAcquireSRWLockExclusive` → `+0x22 LdrpInsertModuleToIndexLockHeld` → `+0x38 JMP RtlReleaseSRWLockExclusive` (꼬리 점프로 락 풂).
- `...LockHeld` = 같은 일을 **락을 이미 쥔 호출자용**으로 쓴 몸통 (호출자: 껍데기, `LdrpMapDllWithSectionHandle`). `LdrpMapDllWithSectionHandle` 은 `+0xd4` 에서 그 락을 잡고 `+0x141 LdrpInsertDataTableEntry`, `+0x14c LdrpInsertModuleToIndexLockHeld` 를 부른 뒤 `+0x158` 에서 푼다. 그래서 껍데기를 안 거치고 이쪽을 바로 부른다.
- `LockHeld` 가 하는 일:
  1. NT 헤더에서 칸을 읽어 항목에 씀: `[NT+0x8]` TimeDateStamp → `+0x80 TimeDateStamp`, `[NT+0x58]` CheckSum → `+0x120 CheckSum`, `[NT+0x50]` SizeOfImage → `+0x40 SizeOfImage` (`+0xa~+0x2e`).
  2. **`+0x91 RtlRbInsertNodeEx(&LdrpMappingInfoIndex, …, 항목+0xe0 = MappingInfoIndexNode)`** — 키 = (TimeDateStamp, SizeOfImage). 비교 줄이 `+0x56 CMP R9D,[RDX-0x60]` (= 항목 `+0x80`), `+0x62 CMP R10D,[RDX-0xa0]` (= 항목 `+0x40`).
  3. **`+0xe7 RtlRbInsertNodeEx(&LdrpModuleBaseAddressIndex, …, 항목+0xc8 = BaseAddressIndexNode)`** — 키 = `DllBase`(`+0x30`). 비교 줄 `+0xba CMP R8,[RDX-0x98]` (= 항목 `+0x30`).
  4. `+0xec BTS [RDI+0x68],7` = `Flags` 비트 7 `InIndexes` 켬.
- 그러니까 질문의 "주소 기준 트리" = `LdrpModuleBaseAddressIndex` 가 맞고, 그 옆에 "이미지 식별(시각·크기) 기준 트리" `LdrpMappingInfoIndex` 가 하나 더 있다. 두 이름 모두 PDB 전역 이름이다 (`.asm` 의 `; LdrpMappingInfoIndex`, `; LdrpModuleBaseAddressIndex`). 두 칸 모두 항목 안의 `_RTL_BALANCED_NODE` 칸이다 (PDB 칸 이름 `MappingInfoIndexNode` `+0xe0`, `BaseAddressIndexNode` `+0xc8`).
- ntdll 에서 둘째 인자는 `+0x109 MOV RDX,[RSP+0x30]` = `+0x60 RtlImageNtHeaderEx` 가 준 NT 헤더.

### 질문 4. `+0x11f LdrpProcessMappedModule` 를 ntdll 항목에 부르는 이유

- 인자: `RCX` = 새 항목, `EDX` = 0, `R8D` = 1 (`+0x116~+0x11b`). 안에서 `EDX` 는 `R13D`(비트 23 `0x800000` 을 봄), `R8D` 는 `R12D` 로 받는다 (`LdrpProcessMappedModule.asm` `+0x23`, `+0x29`).
- 이유 한 줄: **DLL 이 매핑되면 누구나 거치는 마무리(항목 칸 채우기·검증·보안 설정·State 2)를 ntdll 항목도 같은 함수로 하려는 것.** ntdll 은 이미 올라와 있어서 맵 단계만 건너뛰고 이 단계부터 탄다.
- 이 함수가 ntdll 항목에 하는 일 (ntdll 의 `Flags` = ImageDll 켜짐 / CorILOnly 꺼짐 / `LoadReason ≠ 9` 인 첫째 호출 기준. 훑어본 것이고 본문 전체를 읽은 건 아니다):
  - `+0x4a~+0x78` `EntryPoint`(`+0x38`) = `base + AddressOfEntryPoint`. **ntdll 의 `AddressOfEntryPoint` 는 0** (PE 헤더에서 읽음)이라 `+0x68 JZ +0x235` 로 0 이 저장된다. (`LoadReason = 9` 인 패치 항목은 `+0x66 JZ` 로 이 구간을 건너뜀)
  - `+0x83 LdrpValidateEntrySection` (실패 `+0x22e` → `0xc000007b`), `+0x97 OriginalBase`(`+0xf8`) = NT 헤더 `+0x30`(ImageBase), `+0xa6 LoadTime`(`+0x100`).
  - `+0xd9~+0x107` `LdrInitSecurityCookie`, `+0x145 LdrpCfgProcessLoadConfig`.
  - `+0x15e` 위 설명처럼 `InExceptionTable` 이 켜져 있어 `RtlInsertInvertedFunctionTable` 건너뜀.
  - **`+0x1b6~+0x1e9` 락 `LdrpModuleDatatableLock` 안에서 `+0x1c2 [DdagNode+0x38] = 2` (`LdrModulesMapped`)**. 같은 구간의 `+0x1dd LdrpSignalModuleMapped` 는 `LoadContext`(`+0xb0`) ≠ 0 일 때만이라 ntdll(0)은 건너뜀. `+0x1fc LdrpLogDllState(…, 0x14a1)`.
- **State 는 최종 9.** 위 2 를 쓴 뒤 호출이 돌아오면 `+0x150 MOV [RCX+0x38],R14D` (R14D = 9, `+0xe0`) 가 덮는다. 첫째 호출 길에는 State 를 읽는 줄이 이 함수에 없다 (패치 길의 `LdrpRecordModuleDependency +0x25` 는 읽는다). PDB 열거: 2 `LdrModulesMapped`, 9 `LdrModulesReadyToRun`. 그래서 exe·DLL(State 2 로 시작해 그래프 순회에서 올라감, sast/14·18)과 달리 **ntdll 은 처음부터 "다 끝난" 모듈로 목록에 올라간다.** State 9 인 모듈을 적재·순회 쪽이 어떻게 다루는지는 이 요청서 범위 밖이다 (sast/16 은 State 8·9 를 만나면 바로 `return 0`, 22 는 State 9 를 쓰는 쪽).

### 질문 5. 둘째 호출 (`LdrpInitializeProcess +0x126a`) — **예, "패치된 ntdll" 항목**

`+0x1ab~+0x23e` 는 한 갈래가 아니다. 서로 다른 길 넷이다 (실패 로그 하나, 핫패치 둘, 정리 하나).

| 갈래 | 어디서 오나 | 하는 일 |
|---|---|---|
| **실패(메모리 부족)** `+0x189~+0x1b5` (`+0x1ab LdrpLogInternal`) | **`+0x72 JZ +0x189`**: `LdrpAllocateModuleEntry` 가 0 | `.c` 문자열 `"Allocating a data table entry for the system DLL failed\n"`, 줄 번호 `0x13dc` 를 로그. 상태 `0xc0000017` (`STATUS_NO_MEMORY`) → `+0x1b5 JMP +0x15f`. 첫째·둘째 호출 모두 탈 수 있음. **핫패치와 무관.** |
| **패치 이름 갈래** `+0x1b7~+0x20d` (`+0x1df NtQueryVirtualMemory`, `+0x1fe LdrpResolvePatchDllName`) | **`+0xad TEST RDI,RDI` / `+0xb0 JZ +0x1b7`: 넷째 인자(R9)가 0 일 때** (둘째 호출) | 이름을 문자열 상수가 아니라 **매핑된 파일 이름**에서 얻음 (아래) |
| **의존 간선 갈래** `+0x22c~+0x275` (`+0x23e LdrpRecordModuleDependency`) | **`+0x140 TEST RBP,RBP` / `+0x143 JNZ +0x22c`: 셋째 인자(R8)가 0 이 아닐 때** | 원래 ntdll 과 패치 항목을 잇고 두 `HotPatchState` 를 씀 (아래) |
| 정리 `+0x212~+0x21b` (`+0x216 LdrpFreeUnicodeString`) | `+0x159 JS` / `+0x249 JS` (뒤 단계 실패) | `FullDllName`(`+0x48`) 의 버퍼를 풀고 반환 |

- **패치 이름 갈래** (`R9 = 0`):
  1. `+0x1b7~+0x1e0` `NtQueryVirtualMemory(-1 현재 프로세스, [ctx+0] 패치 이미지 베이스, 종류 2, 버퍼 [RSP+0x40], 길이 0x218, &반환길이)`. 종류 2 = `MemoryMappedFilenameInformation` (공개 문서 기준 `[추측: 이름만]`, 숫자 2 는 `+0x1cd MOV R8D,2`). 음수면 `+0x1e8 JS` 로 정리.
  2. `+0x1fe LdrpResolvePatchDllName(RCX=&[RSP+0x50] 파일 이름 글자, RDX=&BaseDllName(항목+0x58), R8=&FullDllName(항목+0x48))`. 안에서: 길이를 세고 → `LdrpAllocateUnicodeString(FullDllName)` → 글자 복사 → `LdrpGetBaseNameFromFullName(FullDllName, BaseDllName)` (`LdrpResolvePatchDllName.asm` `+0x29~+0x64`). 즉 `FullDllName` = 그 파일 이름 전체의 힙 복사, `BaseDllName` = 그 끝 이름.
  3. 성공하면 `+0x207 JNS` → 공통 길 `+0xdc` 로 합류 (`DllBase` = `[ctx+0]` 값).
- **의존 간선 갈래** (`R8 ≠ 0`):
  - `+0xe9 JNZ +0x220` → `+0x220 [RBX+0x10c] = 9`: `LoadReason` = `LoadReasonPatchImage` (PDB 열거 9).
  - `+0x22c~+0x23e` `LdrpRecordModuleDependency(RCX=[LdrpNtDllDataTableEntry] 원래 ntdll 항목, RDX=새 패치 항목, R8=0, R9=&상태)`. 본문(`LdrpRecordModuleDependency.asm` `+0x5d~+0xb4`, 이름·인자만 봄): RCX 쪽 DDAG 의 `Dependencies`(`+0x28`) 목록에 RDX 쪽 노드를 넣고, RDX 쪽 `IncomingDependencies`(`+0x30`) 에도 넣는 간선 하나 (`R8 = 0` 이면 `+0xf4` 에서 `0x20` 바이트를 새로 할당).
  - 성공하면 `+0x24b~+0x275`: 패치 항목 `ParentDllBase`(`+0xb8`) = 원래 항목 `DllBase`; 원래 항목 `HotPatchState`(`+0x130`) = 3 (`LdrHotPatchAppliedForward`), `ActivePatchImageBase`(`+0x128`) = 패치 항목 `DllBase`; 패치 항목 `HotPatchState` = 2 (`LdrHotPatchAppliedReverse`). 이어 `+0x275 JMP +0x149` 로 공통 끝(`+0x150` State = 9, `+0x154` `*R15 = 패치 항목`)에 합류.
- 첫째 호출(보통 ntdll)과 다른 점 한 줄 요약: **이름을 상수에서가 아니라 매핑된 파일에서 얻고, `OriginalBase` 에서 `SystemDllNativeRelocation` 을 안 빼고(`+0x9d JNZ +0xad`), `LoadReason` 을 9 로 쓰고, 원래 ntdll 과 의존 간선을 잇고, 결과를 전역이 아니라 caller 지역 칸(`[RSP+0x160]`)에 돌려준다.** 그 칸은 caller 가 `+0x12b9~+0x12d7` 에서 초기화 순서 목록(`PebLdr+0x30`)의 **맨 앞**에 끼운다.
- 패치 항목도 `+0xf2 LdrpInsertDataTableEntry` 와 `+0x111 LdrpInsertModuleToIndex` 를 똑같이 탄다 (`+0xe9 JNZ` 는 `+0x220` 에서 `+0xef` 로 되돌아옴). 즉 **핫패치가 켜지면 적재·메모리 순서 목록과 해시 표, 색인 둘에 항목이 두 개 들어간다.**
- 둘째 호출을 타는 조건은 caller 쪽 `+0x1250 MOV RCX,[RAX]` / `+0x1256 JZ +0x1277` (`[ctx+0] ≠ 0`) 하나다 (sast/13 와 같음). 이 함수 안의 "패치 모드" 판단은 R8·R9 인자로만 한다 (전역을 다시 읽는 건 `+0x1b7` 한 줄뿐, 값으로 쓰려고).

### 질문 6. 보통 프로세스에서 PEB.Ldr 목록에 ntdll 하나만 — **맞다**

- run18 의 "앞쪽 끝 무렵 모듈 목록에 들어 있는 건 ntdll 뿐 (핫패치 때는 패치 ntdll 하나 더 `[추측]`)" 를 코드로 확인했다.
  - **하나뿐인 이유:** 목록 머리는 `LdrpInitializeProcess +0x16d~+0x1a5` 에서 빈 목록으로 만들어지고, 첫 삽입이 `+0x119d` 에서 부르는 이 함수의 `+0xf2` 다. `LdrpInsertDataTableEntry` 의 다른 호출 자리는 `LdrpInitializeProcess +0x1450`(exe)·`+0x1625`, `LdrpMapDllWithSectionHandle +0x141` 뿐인데 (`_calls.tsv`, `_refs/PebLdr.tsv` 에서 `PebLdr+0x10`·`+0x18` 쓰기도 이 함수 `+0xeb`·`+0xaf` 와 초기화뿐) 모두 `+0x119d` 보다 뒤다.
  - **핫패치 `[추측]` → `[정적]`:** 켜지면 둘째 호출이 같은 길을 타서 패치 항목이 하나 더 들어간다 (질문 5). 이때도 적재·메모리 순서 목록에서 패치 항목은 ntdll 뒤(꼬리), 초기화 순서 목록에서는 ntdll 앞(머리)이다 (`+0x1283~+0x12d7` 둘 다 머리에 끼움: `head.Flink = 새 항목` 으로 읽힘 — `LdrpInitializeProcess.asm` `+0x129f~+0x12aa`, `+0x12cc~+0x12d7`).
- **고쳐 둘 것 (sast/13 → run18):**
  - "초기화 순서 목록에 둘째로 끼움" 의 "둘째"는 **두 번째 호출**이라는 뜻이면 맞지만, 목록 안에서는 **ntdll 앞**이다. 목록 맨 앞에 끼우는 코드다.
  - 이 함수가 끝난 뒤가 아니라 **`LdrpInitializeProcess +0x1450` 뒤**에는 exe 가 적재·메모리 순서 목록 맨 앞에 와서 순서가 "exe, ntdll" 이 된다 (위 질문 2의 `LdrpImageEntry` 분기). "ntdll 뿐" 은 `+0x11a2` 부터 `+0x1450` 까지의 얘기다.

## 요청서 `이미 아는 것` 에서 달라진 것

- R9 는 문자열이 아니라 `UNICODE_STRING` 의 주소고, 폴더 이름 상수는 `\SYSTEM32\`(대문자).
- `+0x1ab~+0x23e` 는 "함수 끝 뒤 cold 블록"이 아니다. 본체와 같은 함수의 연속 범위이고 (`.asm` 에 조각 머리 없음), 위 네 길(실패 로그·패치 이름·의존 간선·정리)이다.
- `[추측]` 였던 것 둘이 닫힌다: (1) 적재·메모리 순서 목록은 `LdrpInsertDataTableEntry`, (2) 둘째 호출은 "패치된 ntdll" 항목.
- 덤으로 눈에 띈 것 하나 (요청 밖, 작게): 첫째 호출은 `+0x9f~+0xa6` 에서 `OriginalBase`(`+0xf8`)에서 `SystemDllNativeRelocation` 을 빼지만, 그 뒤 `+0x11f LdrpProcessMappedModule +0x97` 이 같은 칸을 NT 헤더 ImageBase 로 덮어쓴다. 코드 순서로는 뺀 값이 안 남는 것으로 읽힌다. `[추측]` (동적으로 값 확인 가능, 아래).

## 대표 지점 (그림 2 `(0) 프로세스 준비` 상자에 넣을 것)

1. **`LdrpInitializeNtdllDataTableEntry +0x67`** (ret `+0x6c`) — `LdrpAllocateModuleEntry(0)`: ntdll 의 **로더 항목**이 처음 생김.
2. **`LdrpInitializeNtdllDataTableEntry +0xf2`** (ret `+0xf7`) — `LdrpInsertDataTableEntry`: **PEB.Ldr 적재·메모리 순서 목록과 해시 표에 ntdll 이 들어감** (이 순간부터 목록에 모듈이 하나).
3. **`LdrpInitializeNtdllDataTableEntry +0x150`~`+0x154`** — State = 9, `LdrpNtDllDataTableEntry` 에 새 항목 주소 저장 (값이 바뀌는 순간). 필요하면 `+0x111` (색인 두 개) 를 덧붙임.

(상자 안에 더 넣을 거면 `+0x11f LdrpProcessMappedModule`. 핫패치 갈래 `+0x23e` 는 보통 프로세스에서 안 탄다고 보이므로 점선으로.)

## 동적분석으로 확인해야 할 것

Q44 와 같은 기준점 (test.exe, `LdrpInitializeProcess` 앞쪽) 에서 볼 수 있다. 이 함수는 `LdrpInitializeProcess +0x119d` 가 부르므로 Q44 의 멈춤 뒤에 이어서 건다. 새 번호 없이 **Q44 의 출처로 붙이는 것**을 제안한다 (번호는 오케스트레이터가).

- **dast 요청 필요: Q44 에 합치기** — 첫째 호출이 우리 짐작대로 인자·결과를 갖는가.
  - `ntdll!LdrpInitializeNtdllDataTableEntry` 입구: `r rcx,rdx,r8,r9` → 기대: RCX = ntdll 베이스(`lm` 의 ntdll 시작), RDX = `ntdll!LdrpNtDllDataTableEntry` 주소, R8 = 0, R9 = 스택 주소. `dS @r9` → `Length` = 루트+`\SYSTEM32\`, `Buffer` 문자열. `dq ntdll!LdrpNtDllDataTableEntry L1` → 0.
  - `+0x6c` (ret): `dt ntdll!_LDR_DATA_TABLE_ENTRY @rax` (이 시점 RAX = 새 항목) → `FullDllName` 비어 있음, `DdagNode.State` = 0.
  - `+0xf7` (ret, 목록에 끼운 직후. 이때 새 항목은 RAX 가 아니라 **RBX**): `dt ntdll!_PEB_LDR_DATA ntdll!PebLdr` → `InLoadOrderModuleList`·`InMemoryOrderModuleList` 의 `Flink` = `Blink` = (새 항목) / (새 항목+0x10). `Flags` 에 `0x40`(InLegacyLists) 켜짐. `InInitializationOrderModuleList` 는 아직 자기 자신.
  - `+0x171` (ret, 함수가 끝나기 직전): `dt ntdll!_LDR_DATA_TABLE_ENTRY poi(ntdll!LdrpNtDllDataTableEntry)` → `FullDllName`·`BaseDllName` 문자열, `DdagNode->State` = 9, `Flags` 에 비트 2·6·7·9 켜짐, `EntryPoint` = 0, `LoadReason` = 0, `OriginalBase` (덤 확인: `dq ntdll!LdrSystemDllInitBlock+0x10 L1` 과 비교).
- **dast 요청 필요: 둘째 호출을 안 타나** — `ntdll!LdrpInitializeProcess+0x126a` 에 멈춤점을 걸고 안 걸리는지. 또는 `+0x1250` 에서 `r rcx` = 0 (`+0x1256 JZ` 가 점프). 보통 프로세스(핫패치 꺼짐)에서 안 탄다는 `[추측]`(sast/13·run18)을 닫는다.
- **dast 요청 필요 (선택)** — 해시 버킷 확인: `+0xf7` 에서 `dd @rbx+0x108 L1` 의 값 `& 0x1f` 번째 칸(`ntdll!LdrpHashTable + 16 * 번호`)에 이 항목의 `HashLinks`(`@rbx+0x70`) 가 들어가 있는지. 값은 동적.
- 정적으로 못 정한 것: `RtlGetNtSystemRoot()` 가 돌려주는 루트 문자열, 힙 반환 주소, `LdrpHashUnicodeString` 의 실제 해시 값. 핫패치가 켜진 경우의 파일 이름 값(NT 경로 꼴인지) — 보통 프로세스에선 안 탐.

## 새로 생긴 공백 (대기열 제안)

- (정적, 작게) `RtlInsertInvertedFunctionTable` 로 ntdll 의 예외 표를 등록하는 곳은 어디인가 — 이 함수가 `InExceptionTable` 을 미리 켜 두는 이유와 짝 (`LdrpInitializeProcess +0x45e` 근처 `LdrpInvertedFunctionTables` 쓰기가 후보 `[추측]`).
- (정적, 작게) `LdrpInitializeProcess +0x1625` 가 부르는 `LdrpInsertDataTableEntry` 는 무슨 항목을 끼우나 (`[RSP+0x60]` 에서 `DllBase`·TimeDateStamp·CheckSum 을 복사해 오는 복제 항목으로 보임). 질문 6 의 "다른 호출 자리" 설명을 닫는 데 필요하면.
- (정적, 작게) `LdrpLoadPatchedNtdll` (run17) 이 `[ctx+0]` 에 무엇을 쓰고 이 함수가 만든 패치 항목과 `LdrpNtdllHotPatchContext` 가 어떻게 이어지는지 — 핫패치가 켜진 프로세스를 볼 때만.

## 판정

**분석성공.** 요청 6개 질문 모두 `.asm` 줄·PDB 타입 근거로 답했다. 질문 2 의 `[추측]` (적재·메모리 순서 목록에 넣는 곳) 은 `[정적]` 으로 닫혔고, exe 는 머리에 끼운다는 새 사실이 있다. 질문 5 의 갈래 분해 (요청서 짐작 "cold 블록" 은 아님) 와 `R9`·`R8` 의 뜻을 정정했다. 핫패치가 켜진 실행은 보지 않았으므로 질문 5 는 "코드에 이렇게 쓰여 있다" 까지다.

## 메모

- 임시 파일: 세션 임시 폴더에 PDB 타입 덤프와 PE 상수 읽기 스크립트(`rd.py`)를 두었다 (커밋 대상 아님). 하네스·스크립트는 고치지 않았다.
- `ms/decomp/ntdll/` 와 `ms/disassemble/ntdll/` 에 위 세 함수의 `.c`·`.asm` 이 추가·갱신되었고 `_calls.tsv` 는 `sort -u` 로 합쳐졌다 (커밋 대상 아님).

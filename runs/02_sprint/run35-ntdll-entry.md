# 2026-10-24 run35 — ntdll 의 로더 항목: **`LdrpInitializeNtdllDataTableEntry`** 는 새 항목을 만들어 (`+0x67`) **적재·메모리 순서 목록 + 해시 표** (`+0xf2 LdrpInsertDataTableEntry`) 와 **트리 색인 둘** (`+0x111`) 에 넣고 **State 9** 로 끝낸다 (`+0x150`). 둘째 호출 = 패치 ntdll 항목 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 정적: ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` (다시 analyze 안 함, flow.md 기준과 같은 분석본).
- 버전 대조 (결과서): `LdrpInitializeProcess` call `+0x1bb6` / ret `+0x1bbb` → `LdrpInitializeGraphRecurse` 가 기준과 같다. 요청서의 호출 15줄도 `_calls.tsv` 와 같다.
- 칸 이름·열거 값은 결과서가 PDB 타입(`llvm-pdbutil`)으로 읽었다: `_LDR_DATA_TABLE_ENTRY` (크기 `0x138`), `_PEB_LDR_DATA`, `_LDR_DDAG_NODE` (`0x50`), `_LDR_DDAG_STATE`, `_LDR_DLL_LOAD_REASON`, `_LDR_HOT_PATCH_STATE`, `Flags` 비트 위치.
- 문자열 상수 `\SYSTEM32\`·`ntdll.dll` 은 결과서가 PE `.rdata` 에서 직접 읽었다.

| 함수 | rva | 이번에 본 것 |
|---|---|---|
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `0x7015c` (633B) | 전체 (호출 15줄, 갈래 넷) |
| `ntdll!LdrpInsertDataTableEntry` | `0xb380` (274B) | 전체 |
| `ntdll!LdrpInsertModuleToIndex` / `...LockHeld` | `0x703dc` (61B) / — | 전체 / 트리 넣기 줄 |
| `ntdll!LdrpProcessMappedModule` | — | ntdll 항목에 닿는 줄만 (훑음) |
| `ntdll!LdrpAllocateModuleEntry`, `LdrpResolvePatchDllName`, `LdrpRecordModuleDependency` | — | 이름·인자, 쓰는 칸만 |
| `ntdll!LdrpInitializeProcess` | `0x8c404` | 부르는 자리 `+0x118b ~ +0x119d`, `+0x1250 ~ +0x12d7`, `+0x1374`, `+0x1450`, `+0x1625` |

## 목적

- Q45 (run18 이 남긴 정적 후보): `LdrpInitializeNtdllDataTableEntry` 가 하는 일을 대표 지점 수준으로.
  1. 항목을 새로 만드나, 전역 칸을 채우나.
  2. PEB.Ldr 적재 순서·메모리 순서 목록에 ntdll 을 넣는 곳이 `+0xf2 LdrpInsertDataTableEntry` 인가 (run18 `[추측]`).
  3. `+0x111` 색인은 무엇인가.
  4. `+0x11f LdrpProcessMappedModule` 를 ntdll 에 왜 부르나, State 는 몇이 되나.
  5. 둘째 호출 (`LdrpInitializeProcess +0x126a`, 핫패치 때만) 의 정체.
  6. 보통 프로세스에서 이 함수 뒤 목록에 ntdll 하나뿐인가.

## 방법

- 정적: `_workspace/sast/30_ntdll-entry.md` — ntdll `^LdrpInitializeNtdllDataTableEntry$` (요청 depth 1, 실제는 depth 0 + 있는 callee 파일 읽기), `^LdrpInsertDataTableEntry$`·`^LdrpInsertModuleToIndex$` depth 0
- 결과서: `_workspace/sast/30_ntdll-entry_result.md` (**분석성공**)
- 이번에 `.asm` 과 다시 맞춰 본 줄 (flow-writer):
  - `LdrpInitializeNtdllDataTableEntry` `+0x46`, `+0x60`, `+0x67/+0x6c`, `+0x72`, `+0x93`, `+0x9a ~ +0xa6`, `+0xad/+0xb0`, `+0xb6`, `+0xc7`, `+0xcb`, `+0xd0/+0xd7`, `+0xdc`, `+0xe0`, `+0xe9`, `+0xf2/+0xf7`, `+0x111/+0x116`, `+0x11f/+0x124`, `+0x12c`, `+0x140/+0x143`, `+0x150`, `+0x154`, `+0x159`, `+0x16c`, `+0x187`, `+0x189`, `+0x1ab`, `+0x1b5`, `+0x1b7`, `+0x1cd`, `+0x1df`, `+0x1e8`, `+0x1fe`, `+0x207`, `+0x212/+0x216`, `+0x220`, `+0x22c`, `+0x23e`, `+0x249`, `+0x24b`, `+0x275`
  - `LdrpInsertDataTableEntry` `+0x0 ~ +0x10e` 전체
  - `LdrpInsertModuleToIndex` `+0x10`, `+0x17`, `+0x22`, `+0x38`; `LdrpInsertModuleToIndexLockHeld` `+0x91`, `+0xe7`, `+0xec`
  - `LdrpProcessMappedModule` `+0x4a`, `+0x66`, `+0x68`, `+0x83`, `+0x97`, `+0xa6`, `+0x15e`, `+0x177`, `+0x1c2`, `+0x1dd`, `+0x1fc`
  - `LdrpAllocateModuleEntry` `+0x23`, `+0x40`, `+0x7d`, `+0x11a`, `+0x16b`; `LdrpRecordModuleDependency` `+0x25`, `+0x2b`
  - `LdrpInitializeProcess` `+0x118b`, `+0x118e`, `+0x119d/+0x11a2`, `+0x1250`, `+0x1256`, `+0x126a/+0x126f`, `+0x1271`, `+0x127c`, `+0x1283`, `+0x129f`, `+0x12aa`, `+0x12b9`, `+0x12cc`, `+0x12d7`, `+0x1374`, `+0x1450/+0x1455`, `+0x1625/+0x162a`
  - 결과서와 다른 곳: 없음.

## 결과

비유 (아파트 = 프로세스, 관리실 = ntdll 로더, 입주자 명부 = PEB.Ldr):

- 관리실(ntdll)은 다른 입주자보다 **먼저 와 있다** (커널이 올려 둠). 그래도 명부에는 자기 이름이 있어야 한다.
- 그래서 관리실은 **자기 카드를 새로 한 장 만들어서** (`+0x67`), 이름을 `C:\…\SYSTEM32\ntdll.dll` 로 적고, **명부 두 권(들어온 순서·메모리 순서)과 이름 찾기 서랍(해시 표)** 에 꽂는다 (`+0xf2`).
- 이어서 **번지수로 찾는 색인, 시각·크기로 찾는 색인** 두 곳에도 꽂는다 (`+0x111`).
- 다른 입주자는 "이사 들어옴(State 2)" 부터 시작해서 하나씩 단계를 밟는데, 관리실 카드는 처음부터 **"다 끝남(State 9)"** 도장을 찍는다 (`+0x150`).
- 만든 카드 주소는 전역 칸 `LdrpNtDllDataTableEntry` 에 적어 둔다 (`+0x154`). 이 칸은 카드 자체가 아니라 **카드를 가리키는 쪽지**다.

### 정적 — 호출 지점 (`.asm` CALL 줄, 라벨은 ret)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x119d` | `+0x11a2` | `ntdll!LdrpInitializeNtdllDataTableEntry` (첫째, 조건 없음) | [정적] (run18) |
| `ntdll!LdrpInitializeProcess` | `+0x126a` | `+0x126f` | `ntdll!LdrpInitializeNtdllDataTableEntry` (둘째, `[ctx+0] ≠ 0` 일 때만) | [정적] (run18) |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x60` | `+0x65` | `ntdll!RtlImageNtHeaderEx` (NT 헤더 → `[RSP+0x30]`) | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x67` | `+0x6c` | `ntdll!LdrpAllocateModuleEntry(0)` — 새 항목 | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0xcb` | `+0xd0` | `ntdll!RtlAppendUnicodeStringToString` (`FullDllName` 뒤에 `ntdll.dll`) | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0xf2` | `+0xf7` | `ntdll!LdrpInsertDataTableEntry` — 적재·메모리 순서 목록 + 해시 표 | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x111` | `+0x116` | `ntdll!LdrpInsertModuleToIndex` — 트리 색인 둘 | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x11f` | `+0x124` | `ntdll!LdrpProcessMappedModule(entry, 0, 1)` | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x1ab` | `+0x1b0` | `ntdll!LdrpLogInternal` (항목 할당 실패 로그) | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x1df` | `+0x1e4` | `ntdll!NtQueryVirtualMemory` (패치 이름 갈래, R9 = 0) | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x1fe` | `+0x203` | `ntdll!LdrpResolvePatchDllName` (〃) | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x216` | `+0x21b` | `ntdll!LdrpFreeUnicodeString` (실패 정리) | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x23e` | `+0x243` | `ntdll!LdrpRecordModuleDependency` (의존 간선 갈래, R8 ≠ 0) | [정적] |
| `ntdll!LdrpInsertDataTableEntry` | `+0x1d` | `+0x22` | `ntdll!LdrpHashUnicodeString` (해시 값이 0 일 때만) | [정적] |
| `ntdll!LdrpInsertModuleToIndex` | `+0x17` / `+0x22` | `+0x1c` / `+0x27` | `RtlAcquireSRWLockExclusive` / `LdrpInsertModuleToIndexLockHeld` | [정적] |
| `ntdll!LdrpInsertModuleToIndex` | `+0x38` | — (꼬리 `JMP`) | `RtlReleaseSRWLockExclusive` | [정적] |
| `ntdll!LdrpInsertModuleToIndexLockHeld` | `+0x91` / `+0xe7` | `+0x96` / `+0xec` | `RtlRbInsertNodeEx` (`LdrpMappingInfoIndex` / `LdrpModuleBaseAddressIndex`) | [정적] |
| `ntdll!LdrpProcessMappedModule` | `+0x177` | `+0x17c` | `RtlInsertInvertedFunctionTable` — `Flags` 비트 9 가 꺼졌을 때만 (ntdll 은 건너뜀) | [정적] |

- `+0x46 memset`, `+0x104`·`+0x13b LdrpLogDllState`, `+0x16c __security_check_cookie` 는 잡일이라 뺐다.

### 정적 — 인자 (첫째 호출 / 둘째 호출)

함수 입구 `+0x2b ~ +0x43` 에서 RCX→R14, RDX→R15, R8→RBP, R9→RDI 로 옮긴다. 인자 이름은 결과서가 붙인 것이다 `[추측: 이름만, 쓰임은 코드]`.

| 인자 | 첫째 (`+0x119d`) | 둘째 (`+0x126a`, 핫패치) |
|---|---|---|
| RCX = 이미지 시작 주소 | ntdll 시작 (`[RSP+0xd8]`) | `[LdrpNtdllHotPatchContext]` 칸의 `[+0]` (패치 이미지 시작) |
| RDX = 새 항목을 돌려받을 칸 | **`&LdrpNtDllDataTableEntry`** (`+0x118e`) | caller 지역 칸 `&[RSP+0x160]` |
| R8 = 원래 ntdll 항목 (패치 모드 표시) | 0 (`+0x118b`) | `[LdrpNtDllDataTableEntry]` 값 |
| R9 = 이름 칸 **주소** (`UNICODE_STRING*`) | `&[RSP+0xc0]` | 0 |

- `[RSP+0xc0]` 은 caller `LdrpInitializeProcess +0xcdc ~ +0xd4a` 가 만든다 (결과서): `RtlGetNtSystemRoot` → 힙 버퍼 (크기 = 루트 길이 + `0x28`) → 루트 문자열 + 상수 **`\SYSTEM32\`** (대문자, 20바이트).
  - 여유 `0x28` = `\SYSTEM32\`(20) + `ntdll.dll`(18) + 널(2). caller 가 **`ntdll.dll` 이 붙을 자리까지 미리 잡아 둔다.**

### 정적 — 하는 일 순서 (첫째 호출, R8 = 0, R9 ≠ 0)

```
LdrpInitializeNtdllDataTableEntry (0x7015c)      ← LdrpInitializeProcess +0x11a2 (첫째) / +0x126f (둘째)
 ├─ +0x65   RtlImageNtHeaderEx               NT 헤더 → [RSP+0x30]
 ├─ (1) +0x6c  LdrpAllocateModuleEntry(0)    새 항목 (0x138) + 노드 (0x50). 0 이면 +0x72 JZ → +0x189 실패 로그
 │  +0x78 ~ +0x93  LoadCount = -1, ObsoleteLoadCount = 0xffff, Flags |= 0x204 (ImageDll·InExceptionTable)
 │  +0x9f ~ +0xa6  (R8 = 0 만) OriginalBase -= LdrSystemDllInitBlock+0x10
 │  +0xb6 ~ +0xc7  FullDllName = R9 칸 헤더 복사 (같은 힙 버퍼)
 ├─ +0xd0   RtlAppendUnicodeStringToString   → "<루트>\SYSTEM32\ntdll.dll"
 │  +0xd0 ~ +0xd7  BaseDllName = 상수 NtDllName ("ntdll.dll") 헤더 복사
 │  +0xdc   DllBase = RCX
 ├─ (2) +0xf7  LdrpInsertDataTableEntry      ★ 적재·메모리 순서 목록 (꼬리) + 해시 표
 ├─ (3) +0x116 LdrpInsertModuleToIndex       ★ 트리 색인 둘 (락 안)
 ├─ (4) +0x124 LdrpProcessMappedModule(entry, 0, 1)   안에서 State = 2 (+0x1c2)
 │  +0x143  R8 ≠ 0 이면 → +0x22c 의존 간선 갈래 (둘째 호출만)
 │  +0x150  DdagNode.State = 9 (LdrModulesReadyToRun)    ★ 값이 바뀌는 순간
 │  +0x154  [RDX 칸] = 새 항목  → LdrpNtDllDataTableEntry
 └─ +0x187  RET (EAX = 상태)
```

- **1a. 새로 만든다.** `+0x6c` 에서 받은 RAX 가 RBX 가 되고, 그 뒤 쓰기는 모두 `[RBX+…]` 다. `LdrpNtDllDataTableEntry` 는 **항목을 가리키는 포인터 칸(8B)** 이다. 성공 길 끝 `+0x154` 에서만 쓴다 (실패하면 `+0x159 JS` 로 빠짐).
  - caller 도 포인터로 쓴다: `LdrpInitializeProcess +0x127c` 에서 값을 읽고 `+0x1283 ADD RAX,0x20` (초기화 순서 연결 칸).
- **1b. 이름.**
  - `FullDllName`(`+0x48`): R9 가 가리키는 UNICODE_STRING 헤더 16바이트를 값으로 복사 → **글자 버퍼는 caller 의 힙 버퍼를 같이 쓴다**. 그 뒤에 `ntdll.dll` 을 붙인다. 결과 = `<RtlGetNtSystemRoot() 값>\SYSTEM32\ntdll.dll`. 루트 값은 동적.
  - `BaseDllName`(`+0x58`): ntdll `.rdata` 의 상수 `NtDllName` 헤더 복사. 글자는 `.rdata` 를 그대로 가리킨다.
- **새 항목 손질** `[정적]`:
  - `Flags |= 0x204` = 비트 2 `ImageDll` + 비트 9 `InExceptionTable`. `ImageDll` 은 `LdrpAllocateModuleEntry +0x11a` 가 이미 켜므로 **새로 켜지는 건 `InExceptionTable` 하나**.
  - 그 비트 때문에 `LdrpProcessMappedModule +0x15e BT EDX,9` 에서 `+0x177 RtlInsertInvertedFunctionTable` 을 건너뛴다. ntdll 의 예외 표는 다른 곳에서 이미 등록했다고 보인다 `[추측]`. 후보 둘: 결과서는 `LdrpInitializeProcess +0x45e` 근처 `LdrpInvertedFunctionTables` 쓰기, flow-writer 가 보기엔 run18 이 이미 적은 `+0x415` / `+0x41a RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)` (묶음 3 "ntdll 역함수 표 등록"). 어느 쪽인지는 Q67 (a).
  - `LoadCount = -1`·`ObsoleteLoadCount = 0xffff` = "안 내려가는 모듈" 표시로 보인다 `[추측]` (exe 항목도 같은 값, run19).
  - `LoadReason` 은 이 함수가 안 쓴다 → 0 (`LoadReasonStaticDependency`). `LdrpAllocateModuleEntry +0x16b` 의 `LoadReason = 4` 는 문맥이 있을 때만이고, 인자 0 이라 `+0x7d JZ` 로 건너뛴다.
  - 새 항목의 나머지 칸은 0 이다 — 힙 플래그 `|8` 을 "0 으로 채움" 으로 읽은 데 기댄 `[추측: 공개 문서]`.

### 정적 — 질문 2: 목록에 넣는 곳 = `+0xf2 LdrpInsertDataTableEntry` — **예**

`LdrpInsertDataTableEntry` (rva `0xb380`, 274B) 본문:

| 자리 | 하는 일 |
|---|---|
| `+0x6` / `+0xd JNZ +0x62` | `Flags` 비트 6 `InLegacyLists` 가 이미 켜졌으면 아무것도 안 하고 끝 |
| `+0xf ~ +0x22` | `BaseNameHashValue`(`+0x108`) 가 0 이면 `+0x1d LdrpHashUnicodeString(&BaseDllName)` 로 구해 저장 |
| `+0x28 ~ +0x7a` | **해시 표 `LdrpHashTable`**: 버킷 = 해시 `& 0x1f` (32칸, 칸당 16바이트). `HashLinks`(`+0x70`) 를 그 버킷 **꼬리**에 |
| `+0x69 CMP RBX,[LdrpImageEntry]` → `+0x85 JZ +0xbd` | exe 항목인지 가름 |
| (exe 아님) `+0x87 ~ +0xaf`, `+0x4d ~ +0x57` | `InLoadOrderLinks`(`+0x0`) 를 `PebLdr+0x10` 목록 **꼬리**에 (`+0xaf` 가 `PebLdr+0x18` 갱신), `InMemoryOrderLinks`(`+0x10`) 를 `PebLdr+0x20` 목록 **꼬리**에 (`+0x57` 가 `PebLdr+0x28` 갱신) |
| (exe 항목) `+0xbd ~ +0x10e` | 두 목록의 **맨 앞**에 끼움 (`+0xeb` `PebLdr+0x10` 갱신, `+0x107` `PebLdr+0x20` 갱신) |
| `+0x5e OR [RBX+0x68],0x40` | `InLegacyLists` 켬 |
| `+0x46 MOV ECX,3` / `+0x4b INT 0x29` | 이웃 연결이 안 맞으면 `__fastfail(3)` |

- **초기화 순서 목록(`PebLdr+0x30`)은 이 함수가 안 만진다.** 그건 caller `LdrpInitializeProcess +0x1283 ~ +0x12d7` 이 직접 한다 (run18).
- ntdll 은 **꼬리 쪽 분기**를 탄다. `LdrpImageEntry` 를 처음 쓰는 곳은 `LdrpInitializeProcess +0x1374` (run19) 이고, 그건 `+0x119d` 보다 뒤라서 이때 0 이다.
- 그래서 **exe 는 맨 앞**이다: exe 항목은 `+0x1374` 에서 `LdrpImageEntry` 에 들어간 뒤 `+0x1450` (ret `+0x1455`) 에서 이 함수를 타므로 `+0x69` 비교가 같다 → 머리 쪽. **`+0x1455` 뒤 적재·메모리 순서 = exe, ntdll.**
  - 나머지 DLL(dll1·dll2 등, `LdrpMapDllWithSectionHandle +0x146`) 은 꼬리 쪽이다 `[정적: 코드 모양, 어느 모듈이 타는지는 Q23]`.

### 정적 — 질문 3: 색인 = 붉은-검은 트리 둘

- `LdrpInsertModuleToIndex(entry, NT 헤더)` 는 **락만 잡아 주는 껍데기**: `+0x10 ~ +0x17` `LdrpModuleDatatableLock` 독점 → `+0x22 LdrpInsertModuleToIndexLockHeld` → `+0x38` 꼬리 `JMP RtlReleaseSRWLockExclusive`.
- `...LockHeld` = 락을 이미 쥔 호출자용 몸통. `LdrpMapDllWithSectionHandle` 은 락을 스스로 잡고 이쪽을 바로 부른다 (`+0x151`, run26 그림 `ji2`).
- 몸통이 하는 일:
  1. NT 헤더에서 `TimeDateStamp`·`CheckSum`·`SizeOfImage` 를 항목에 옮김 (`+0xa ~ +0x2e`, 결과서).
  2. **`+0x91 RtlRbInsertNodeEx(&LdrpMappingInfoIndex, …)`** — 키 = (TimeDateStamp, SizeOfImage), 항목 칸 `MappingInfoIndexNode`(`+0xe0`).
  3. **`+0xe7 RtlRbInsertNodeEx(&LdrpModuleBaseAddressIndex, …)`** — 키 = `DllBase`, 항목 칸 `BaseAddressIndexNode`(`+0xc8`).
  4. `+0xec BTS [RDI+0x68],7` = `Flags` 비트 7 `InIndexes` 켬.
- 두 트리 이름은 PDB 전역 이름이다 (`.asm` 주석). ntdll 에서 둘째 인자는 `+0x109` 가 실은 `[RSP+0x30]` (= `+0x60 RtlImageNtHeaderEx` 결과).

### 정적 — 질문 4: `+0x11f LdrpProcessMappedModule` 와 State

- 인자 `(entry, 0, 1)` (`+0x116 ~ +0x11b`).
- 이유 한 줄: **DLL 이 매핑되면 누구나 거치는 마무리를 ntdll 항목도 같은 함수로 하려는 것.** ntdll 은 이미 올라와 있으니 매핑 단계만 건너뛰고 여기부터 탄다 `[추측: 뜻, 줄은 정적]`.
- ntdll 항목에 닿는 줄 (결과서가 훑은 것, 본문 전부는 아님) `[정적]`:
  - `+0x4a ~ +0x78` `EntryPoint`(`+0x38`) = 시작 주소 + `AddressOfEntryPoint`. **ntdll 의 `AddressOfEntryPoint` 는 0** (PE 헤더) → `+0x68` 에서 0 쪽으로 가서 `EntryPoint = 0`.
  - `+0x83 LdrpValidateEntrySection`, `+0x97 OriginalBase`(`+0xf8`) = NT 헤더 `ImageBase`, `+0xa6 LoadTime`.
  - `LdrInitSecurityCookie`·`LdrpCfgProcessLoadConfig` (결과서 `+0xd9 ~ +0x107`, `+0x145`).
  - `+0x15e` 비트 9 켜져 있어 `RtlInsertInvertedFunctionTable` 건너뜀.
  - 락 안 **`+0x1c2 State = 2`** (`LdrModulesMapped`). `+0x1dd LdrpSignalModuleMapped` 는 `LoadContext` ≠ 0 일 때만이라 ntdll 은 건너뜀.
- **최종 State = 9.** 돌아온 뒤 `+0x150 MOV [RCX+0x38],R14D` (R14D = 9, `+0xe0`) 가 2 를 덮는다.
  - 그래서 exe·DLL 이 State 2 로 시작해 그래프 순회로 올라가는 것과 달리, **ntdll 은 처음부터 "다 끝난" 모듈(9 `LdrModulesReadyToRun`) 로 목록에 올라간다.**
  - run7 `[관찰]` "ntdll 노드는 GR 프레임을 안 만들었다" 와 맞는다. GR 은 State 7 자식만 재귀한다 (run27 `[정적]`).

### 정적 — 질문 5: 둘째 호출 = "패치된 ntdll" 항목 — **예**

`+0x1ab ~ +0x23e` 은 한 덩어리가 아니라 **서로 다른 네 길**이다 (`.asm` 에 조각 머리 없음, 같은 함수의 연속 범위).

| 갈래 | 어디서 오나 | 하는 일 |
|---|---|---|
| 실패 로그 `+0x189 ~ +0x1b5` (`+0x1ab LdrpLogInternal`) | `+0x72 JZ` — 항목 할당 실패 | 상태 `0xc0000017` → `+0x1b5 JMP` 끝으로. 첫째·둘째 둘 다 탈 수 있음, **핫패치와 무관** |
| 패치 이름 `+0x1b7 ~ +0x20d` (`+0x1df NtQueryVirtualMemory`, `+0x1fe LdrpResolvePatchDllName`) | `+0xad TEST RDI,RDI` / `+0xb0 JZ` — **R9 = 0** (둘째) | 이름을 상수가 아니라 **매핑된 파일 이름**에서 얻음 → `+0x207 JNS` 로 공통 길 `+0xdc` 에 합류 |
| 의존 간선 `+0x22c ~ +0x275` (`+0x23e LdrpRecordModuleDependency`) | `+0x140 TEST RBP,RBP` / `+0x143 JNZ` — **R8 ≠ 0** (둘째) | 원래 ntdll → 패치 항목 간선, 두 항목 `HotPatchState` 쓰기 → `+0x275 JMP +0x149` 로 공통 끝 |
| 정리 `+0x212 ~ +0x21b` (`+0x216 LdrpFreeUnicodeString`) | `+0x159 JS` / `+0x249 JS` (뒤 단계 실패) | `FullDllName` 버퍼 풀고 반환 |

- 패치 이름 갈래: `NtQueryVirtualMemory(-1, 패치 시작, 종류 2, 버퍼 0x218, …)` — 종류 2 는 `+0x1cd MOV R8D,2`, 이름 `MemoryMappedFilenameInformation` 은 `[추측: 공개 문서]`. 이어 `LdrpResolvePatchDllName` 이 `FullDllName` = 파일 이름 전체의 힙 복사, `BaseDllName` = 끝 이름.
- 의존 간선 갈래:
  - `+0xe9 JNZ` → `+0x220` `LoadReason` = 9 (`LoadReasonPatchImage`, PDB).
  - `+0x23e LdrpRecordModuleDependency(원래 ntdll 항목, 패치 항목, 0, &상태)` — 원래 쪽 `Dependencies` 와 패치 쪽 `IncomingDependencies` 에 간선 하나.
  - 성공하면 (`+0x24b ~ +0x275`) 패치 항목 `ParentDllBase` = 원래 `DllBase`, 원래 항목 `HotPatchState` = 3 (`LdrHotPatchAppliedForward`)·`ActivePatchImageBase` = 패치 `DllBase`, 패치 항목 `HotPatchState` = 2 (`LdrHotPatchAppliedReverse`).
- 첫째와 다른 점: 이름 출처(파일), `OriginalBase` 빼기 안 함(`+0x9d JNZ`), `LoadReason` 9, 의존 간선, 결과를 전역이 아니라 caller 지역 칸에.
- **패치 항목도 `+0xf2`·`+0x111` 을 똑같이 탄다** (`+0x220` 에서 `+0xef` 로 되돌아옴). 핫패치가 켜지면 적재·메모리 순서 목록·해시 표·트리 둘에 항목이 **둘**.
- 둘째 호출을 타는 조건은 caller 쪽 하나: `+0x1250 MOV RCX,[RAX]` / `+0x1256 JZ` (`[ctx+0] ≠ 0`, run18 과 같음).

### 정적 — 질문 6: 보통 프로세스에서 목록에 ntdll 하나 — **맞다**

- 목록 머리는 `LdrpInitializeProcess +0x16d ~ +0x1a5` 에서 빈 목록으로 생기고 (`+0x1c1` 에서 PEB.Ldr 에 걸림, run18), **첫 삽입이 이 함수 `+0xf2`** 다.
- `LdrpInsertDataTableEntry` 의 다른 호출 자리 셋 (`LdrpInitializeProcess +0x1450`·`+0x1625`, `LdrpMapDllWithSectionHandle +0x141`) 은 모두 `+0x119d` 보다 뒤다. `PebLdr+0x10`·`+0x18` 을 쓰는 줄도 이 함수 `+0xeb`·`+0xaf` 와 초기화뿐 (`_refs/PebLdr.tsv`, 결과서).
- 그래서 **"ntdll 뿐" 은 `+0x11a2` 부터 `+0x1450` 까지**다. `+0x1455` 뒤엔 "exe, ntdll".
- 핫패치 때 (`[추측]` → `[정적]`): 패치 항목이 하나 더 들어간다. 적재·메모리 순서에선 ntdll **뒤**(꼬리), 초기화 순서 목록에선 ntdll **앞**이다 — `+0x129f ~ +0x12aa` (ntdll) 과 `+0x12cc ~ +0x12d7` (패치) 둘 다 `PebLdr+0x30` 의 `Flink` 를 새 항목으로 쓰는 **머리 끼우기**이고 패치가 나중이라서.

### 호출 관계

```
ntdll!LdrpInitializeProcess                                   (run18 + run35)
 ├─ (4) +0x11a2  LdrpInitializeNtdllDataTableEntry(ntdll, &LdrpNtDllDataTableEntry, 0, &"<루트>\SYSTEM32\")
 │        ├─ (1) +0x6c   LdrpAllocateModuleEntry(0)
 │        ├─ (2) +0xf7   LdrpInsertDataTableEntry      → PebLdr+0x10·+0x20 꼬리, LdrpHashTable
 │        │                └─ +0x22  LdrpHashUnicodeString   (해시 값 0 일 때)
 │        ├─ (3) +0x116  LdrpInsertModuleToIndex
 │        │                └─ +0x27  LdrpInsertModuleToIndexLockHeld → +0x96 / +0xec RtlRbInsertNodeEx (트리 둘)
 │        ├─ (4) +0x124  LdrpProcessMappedModule(entry, 0, 1)   State = 2 (+0x1c2)
 │        └─ +0x150 State = 9, +0x154 LdrpNtDllDataTableEntry = 항목
 ├─ (5) +0x126f  LdrpInitializeNtdllDataTableEntry(패치 시작, &[RSP+0x160], ntdll 항목, 0)   [ctx+0] ≠ 0 일 때만
 │        ├─ +0x1e4 NtQueryVirtualMemory → +0x203 LdrpResolvePatchDllName   (이름)
 │        ├─ +0xf7 / +0x116 / +0x124  (첫째와 같은 셋)
 │        └─ +0x243 LdrpRecordModuleDependency(ntdll 항목 → 패치 항목)
 │  +0x1283 ~ +0x12d7  초기화 순서 목록 머리에 ntdll, (있으면) 그 앞에 패치 항목
 ├─ (6) +0x135e  LdrpAllocateModuleEntry   exe 항목 → +0x1374 LdrpImageEntry
 └─     +0x1455  LdrpInsertDataTableEntry  exe → 두 목록 **맨 앞** (LdrpImageEntry 와 같아서)
```

### 그림 반영 (flow.md 그림 2)

- `(0) 프로세스 준비` 상자 안 `p4` (ntdll 항목) 아래에 `(정적)` 상자 넷을 넣었다: `(1) +0x6c LdrpAllocateModuleEntry`, `(2) +0xf7 LdrpInsertDataTableEntry`, `(3) +0x116 LdrpInsertModuleToIndex`, `(4) +0x124 LdrpProcessMappedModule`.
  - 대표 지점 셋 (결과서: 항목 생김 · 목록에 들어감 · State 9) 중 셋째는 호출이 아니라 값 쓰기라 `p4` 상자 글에 넣었다.
  - `(3)`·`(4)` 는 dll1·dll2 길 (`LdrpMapDllWithSectionHandle` 아래 `ji2`·`jp1` 등) 과 같은 단계를 ntdll 도 밟는다는 걸 보여 주려고 넣었다. 함수 하나 = 노드 하나 (R7), 같은 함수라도 caller 가 다르면 따로 노드.
  - `LdrpInsertModuleToIndex` 안쪽 (`LockHeld`, 트리 넣기) 과 `LdrpHashUnicodeString` 은 글(2.4)에만 둔다.
- `p5` (둘째 호출) 글: "패치 ntdll 항목 추정" → "(정적) 패치 ntdll 항목 + 원래 ntdll → 패치 의존 간선". 안쪽 호출은 보통 프로세스에서 안 탈 것이라 그림에 안 넣고 글에만.
- `p6` (exe 항목) 글에 "+0x1455 에서 두 목록 맨 앞" 을 붙였다.

## 확인된 것

- (`[정적]`) `LdrpInitializeNtdllDataTableEntry` 는 **항목을 새로 만든다** (`+0x67` call / `+0x6c` ret `LdrpAllocateModuleEntry(0)`). `LdrpNtDllDataTableEntry` 는 그 항목을 가리키는 포인터 칸이고, 성공했을 때 `+0x154` 에서만 쓴다.
- (`[정적]`) ntdll 항목 `FullDllName` = `<RtlGetNtSystemRoot()>\SYSTEM32\ntdll.dll` (caller 의 힙 버퍼를 같이 씀), `BaseDllName` = 상수 `NtDllName`, `DllBase` = ntdll 시작, `EntryPoint` = 0, `LoadReason` = 0.
- (`[정적]`) **PEB.Ldr 적재·메모리 순서 목록에 ntdll 을 넣는 곳 = `+0xf2 LdrpInsertDataTableEntry`** (run18 `[추측]` 닫힘). 해시 표 `LdrpHashTable` 에도 같이 넣는다. 초기화 순서 목록은 안 만진다.
- (`[정적]`) `LdrpInsertDataTableEntry` 는 **exe 항목(`LdrpImageEntry`)만 두 목록 맨 앞, 나머지는 꼬리**에 끼운다. 그래서 `LdrpInitializeProcess +0x1455` 뒤 순서는 exe, ntdll.
- (`[정적]`) `+0x111 LdrpInsertModuleToIndex` = 락 껍데기 → `LockHeld` 가 트리 둘 (`LdrpMappingInfoIndex`, `LdrpModuleBaseAddressIndex`) 에 넣고 `InIndexes` 비트를 켠다.
- (`[정적]`) ntdll 항목은 `+0x11f LdrpProcessMappedModule` 안에서 State 2 를 거쳐 **`+0x150` 에서 State 9** 가 된다. `InExceptionTable` 을 미리 켜 둬서 역함수 표 등록은 건너뛴다.
- (`[정적]`) 둘째 호출 = 패치 ntdll 항목 (이름은 매핑 파일에서, `LoadReason` 9, 원래 ntdll → 패치 간선, `HotPatchState` 3/2). 같은 목록·해시·트리에 들어간다. 타는 조건은 caller `+0x1256` 하나.
- (`[정적]`) 보통 프로세스에서 `+0x11a2 ~ +0x1450` 동안 목록에 있는 건 ntdll 하나 (run18 결론 맞음).

### flow.md 판정

| 예전 글 | 판정 | 근거 |
|---|---|---|
| 2.4 "적재·메모리 순서 목록에 넣는 건 `LdrpInitializeNtdllDataTableEntry` 안(`LdrpInsertDataTableEntry`)으로 보인다 `[추측]`" | **`[정적]`** `+0xf2` | 질문 2 |
| 2.4 둘째 `+0x126a` "패치 ntdll 항목" `[추측]` (그림 `p5` "추정") | **`[정적]`** 무엇을 만드는지는 코드로 확인. 타는지는 그대로 동적 (Q44) | 질문 5 |
| 2.4 exe 항목 "`LdrpInsertDataTableEntry`(`+0x1455`) → `LdrpInsertModuleToIndex`(`+0x14d2`) … 목록·색인에 넣는 일로 보임 `[추측: 이름]`" | **`[정적]`** callee 본문으로 확인 (exe 는 맨 앞). exe 쪽 줄은 그대로 caller 쪽만 | 질문 2·3 |
| 2.4 "앞쪽 끝의 'ntdll 뿐' 다음 상태가 'ntdll + exe'" | **좁힘** — 순서는 exe, ntdll | 질문 2 |
| 2.7 "ntdll 노드 `State` 가 7 이 아니었다 (9 로 짐작 `[추측]`)" | **`[정적]`** `+0x150` State = 9. run7 `[관찰]` 과 맞음 | 질문 4 |
| 2.6 `LdrpAllocateModuleEntry` caller 목록 "(ret) … `LdrpInitializeNtdllDataTableEntry +0x67`" | **정정** — `+0x67` 은 call, ret 는 `+0x6c` | `_calls.tsv` |
| 4장 2.4 "`LdrpInitializeNtdllDataTableEntry` 본문 … (Q45, run18)" | **해결됨** | 위 |

## 정정

- **run18 (sast/13) 첫째 호출 인자 "R9 = `<NtSystemRoot>\System32\` 문자열" 은 정확하지 않다.**
  - R9 는 문자열이 아니라 **`UNICODE_STRING` 칸의 주소** (`&[RSP+0xc0]`) 다. 이 함수는 그 헤더를 값으로 복사해 `FullDllName` 으로 쓴다.
  - 폴더 이름 상수는 대문자 **`\SYSTEM32\`** 다 (`SlashSystem32SlashString`, `.rdata` 에서 직접 읽음).
- **run18 "둘째 항목이 있으면 그것도 (초기화 순서 목록에)" 는 자리가 빠져 있었다.**
  - `+0x1283 ~ +0x12d7` 은 둘 다 **머리 끼우기**라서, 패치 항목은 초기화 순서 목록에서 ntdll **앞**이다. (sast/13 결과서의 "둘째로 끼움" 은 "두 번째 호출로" 라는 뜻으로만 맞다.)
- **run18 "적재·메모리 순서 목록에 넣는 건 첫째 호출 안 `[추측]`", "핫패치 때는 패치 ntdll 하나 더 `[추측]`" 은 `[정적]` 이 됐다** (틀린 건 아님, 태그만).
- **run22 (와 flow.md 2.6) `LdrpAllocateModuleEntry` caller 목록의 `LdrpInitializeNtdllDataTableEntry +0x67` 은 call 자리다.** 다른 caller 는 ret 로 적혀 있어서, 같은 기준이면 **`+0x6c`**.
- run18·run22 문서 맨 위에 정정 한 줄씩 넣었다. flow.md 2.4·2.6 도 고쳤다.

## 아직 확인 필요

동적 확인거리는 오케스트레이터가 대기열 **Q44** 에 출처(sast/30)로 붙였다. 새 정적 공백 셋은 **Q67**.

- **Q44** (a) `ntdll!LdrpInitializeNtdllDataTableEntry` 입구 (`ntdll+0x7015c`, test.exe 만): `r rcx,rdx,r8,r9` — RCX = `lm m ntdll` 시작, RDX = `ntdll!LdrpNtDllDataTableEntry` 주소, R8 = 0, R9 = 스택 주소. `dS @r9` (루트 + `\SYSTEM32\`), `dq ntdll!LdrpNtDllDataTableEntry L1` = 0.
- **Q44** (b) `+0x6c` (ret): `dt ntdll!_LDR_DATA_TABLE_ENTRY @rax` — 이름 비어 있음, `DdagNode.State` = 0.
- **Q44** (c) `+0xf7` (ret, 새 항목은 **RBX**): `dt ntdll!_PEB_LDR_DATA ntdll!PebLdr` — 적재·메모리 순서 목록 `Flink` = `Blink` = 항목 / 항목+0x10, 초기화 순서는 아직 자기 자신. (선택) `@rbx+0x108` 해시 `& 0x1f` 번째 `LdrpHashTable` 칸에 `@rbx+0x70`.
- **Q44** (d) `+0x171` (ret, 끝나기 직전): `dt ntdll!_LDR_DATA_TABLE_ENTRY poi(ntdll!LdrpNtDllDataTableEntry)` — 이름 둘, `DdagNode->State` = 9, `Flags` 비트 2·6·7·9, `EntryPoint` = 0, `LoadReason` = 0, `OriginalBase`.
- **Q44** (e) 둘째 호출을 안 타나: `LdrpInitializeProcess +0x126a` (`ntdll+0x8d66e`) 에 안 서는지, 또는 `+0x1250` 에서 `r rcx` = 0.
- **Q44** (f, 덤) `OriginalBase` 의 두 번 쓰기: 첫째 호출은 `+0x9f ~ +0xa6` 에서 `SystemDllNativeRelocation` 을 빼지만 곧 `LdrpProcessMappedModule +0x97` 이 NT 헤더 `ImageBase` 로 덮는다. 코드 순서로는 뺀 값이 안 남는 것으로 읽힌다 `[추측]` — (d) 의 `OriginalBase` 와 `dq ntdll!LdrSystemDllInitBlock+0x10 L1` 비교.
- **Q67** (새, 정적, 작은 셋):
  - (a) `RtlInsertInvertedFunctionTable` 로 ntdll 예외 표를 등록하는 곳 — `InExceptionTable` 을 미리 켜는 이유와 짝 (후보: 결과서 `LdrpInitializeProcess +0x45e` 근처 `LdrpInvertedFunctionTables` 쓰기 `[추측]`, 또는 run18 의 `+0x41a RtlInsertInvertedFunctionTable(ntdll 시작, …)` — 후자면 run18 로 거의 풀린 셈).
  - (b) `LdrpInitializeProcess +0x1625` (ret `+0x162a`) 의 `LdrpInsertDataTableEntry` 가 끼우는 항목 정체 (AppVerifier 묶음의 복제 항목?).
  - (c) `LdrpLoadPatchedNtdll` (run17) 이 `[ctx+0]` 에 쓰는 것과 패치 항목·`LdrpNtdllHotPatchContext` 의 연결 (핫패치 켜진 경우만).

## 디버거 메모 / 도구 메모

- (다음 동적용) `+0xf7` 시점에 새 항목은 RAX 가 아니라 **RBX** 다 (RAX 는 `LdrpInsertDataTableEntry` 반환값). `+0x6c` 에선 RAX.
- (다음 동적용) ntdll 항목은 `+0x154` 전에는 `LdrpNtDllDataTableEntry` 로 찾을 수 없다 (그 전엔 0).

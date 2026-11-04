# 2026-11-03 run60 — 역함수 표·`.mrdata` 열고 닫기 (Q111, PE_emulator 요청 5): `LdrpInitializeProcess +0x3e6 ~ +0x4a4` 는 **ntdll 을 역함수 표 칸 0 에 놓는 한 묶음** (넣기 → 되읽기 → 칸 0 직접 쓰기 → 빼기). `.mrdata` 는 **파일 값 1** 인 중첩 카운터 `LdrpMrdataUnprotected` 로 열고 닫고, 0 ↔ 1 을 넘을 때만 `NtProtectVirtualMemory` — 이 구간의 유일한 syscall 은 `+0x488` 닫기 (그때 `LdrpLocateMrdata` 가 `LdrpMrdataSize`·`LdrpMrdataBase` 를 처음 씀). CFG 내보내기 억제 확인은 test.exe 에서 0. 덤: `.data 0x1ce268` = 프로세스 속성 `0xa0022` (세그먼트 힙 요청) 값 (정적)

> 정정 (run63, sast/58): 아래 "결과서와 다르게 적은 곳" 의 `LdrpMrdataHeap`·`HeapUnprotected` 두 `[추측]` ("test.exe 에선 쓰기까지 안 간 것", "`LdrEnsureMrdataHeapExists +0xa` 의 CFG 확인도 그 갈래일 것") 은 맞았다 → `[정적]`. 단 갈림 자리는 `RtlAddGrowableFunctionTable` 쪽이 아니다 — `+0x40` (ret `+0x45`) 은 인자 검사 `+0x27`/`+0x3a` 뒤 **늘** `LdrEnsureMrdataHeapExists` 를 부르고, CFG 확인은 그 안 `+0xa/+0xf LdrControlFlowGuardEnforced` · `+0x11 JZ +0x1f` (test.exe 0 → 바로 0 반환) 다. "② 조건부" 의 조건 = 그 안 갈래 → `runs/03_sprint/run63-cfg-func-table.md` "정정"

## PE_emulator 쪽에서 쓸 것

결과서 (`_workspace/sast/55_func-table-mrdata_result.md`) 의 "PE_emulator 가 그대로 쓸 작은 표" 를 1절에 옮겼다. 표 밑 글머리와 2 ~ 4절은 flow-writer 가 `.asm` 의 `CALL … ret=` 줄로 다시 맞춘 것이다. 오프셋 읽는 법:

- `call +0x..` = CALL 명령이 있는 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령이 시작하는 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 오프셋 (예: `LdrpInitializeProcess` 시작 `0x18008c404`, `+0x494 JNZ 0x18008c8a8` → `+0x4a4`).
- `.asm` 은 `ms/disassemble/ntdll/<함수>.asm` (커밋 안 함). 모두 `[정적]`. 값 대조는 run44 사진 (`[관찰]`).

### 1. 함수 × (인자 · 쓰는 칸 · 하위 호출 · syscall · 반환) `[정적]`

| 함수 (rva · 크기) | 인자 | 쓰는 칸 | 하위 호출 (call / ret) | syscall (번호 · 인자) | 반환 |
|---|---|---|---|---|---|
| `LdrProtectMrdata` (`0xa3ec0` · 195) | `ECX`: 0 = 열기 (쓰기 켬), ≠ 0 = 닫기 (쓰기 끔). `+0x1f MOV EDI,ECX` | `LdrpMrdataUnprotected` (`+0x60` 닫을 때 −1 / `+0xbb` 열 때 +1). `LdrpMrdataLock` 비트 0 잡기 (`+0x47 BTS.LOCK`) | `LdrpChangeMrdataProtection` `+0x6d / +0x72` (ECX = 2) · `+0xb4 / +0xb9` (ECX = 4) — 카운터가 0 ↔ 1 일 때만. 락: `RtlpAcquireSRWLockExclusiveContended` `+0xa8 / +0xad` (경합 때), `RtlReleaseSRWLockExclusive` `+0x84` 꼬리 JMP · `+0x99 / +0x9e` (오류 길) | 직접 없음 | 없음 (`void` 모양). 닫을 게 없거나 (카운터 0) 열 때 카운터 −1 이면 `+0x9e ~ +0xa3 INT 0x29` 코드 `0xe` |
| `LdrpChangeMrdataProtection` (`0xa3f90` · 102) | `ECX`: 새 보호값 4 (`PAGE_READWRITE`) 또는 2 (`PAGE_READONLY`), 입구에서 `[RSP+0x40]` 에 저장 | 없음 (`LdrpMrdataBase` 읽기 `+0x8`·`+0x19`, `LdrpMrdataSize` 읽기 `+0x2f`) | `LdrpLocateMrdata` `+0x14 / +0x19` (`LdrpMrdataBase == 0` 일 때만, `+0x12 JNZ → +0x19`) | **`NtProtectVirtualMemory` `0x50`** call `+0x51 / +0x56`: (−1 `+0x36`, `&Base 사본` `RDX`, `&Size 사본` `R8`, 새 보호 `R9D` `+0x3d`, `&옛보호` = 같은 `[RSP+0x40]` `+0x47 ~ +0x4c`) | 없음. 실패 (`+0x58 JNS → +0x61` 못 탐) 면 `+0x5a ~ +0x5f INT 0x29` 코드 5 |
| `LdrpLocateMrdata` (`0xa4000` · 110) | 없음 | **`LdrpMrdataSize` `+0x52`**, **`LdrpMrdataBase` `+0x59`** | `RtlImageNtHeaderEx(3, ntdll 시작, 0, &헤더)` `+0x22 / +0x27` · `RtlSectionTableFromVirtualAddress(헤더, ntdll, LdrSystemDllInitBlock 의 RVA)` `+0x36 / +0x3b` · `LdrpMakePermanentImageCommit(시작, 크기)` `+0x4d / +0x52` | 없음 | 없음. 구역 못 찾으면 (`+0x3e JZ → +0x67`) `INT 0x29` 코드 5 |
| `LdrpMakePermanentImageCommit` (`0xa3e78` · 56) | `RCX` 시작, `RDX` 크기 | 없음 — 걸친 쪽(page)마다 8B 를 읽어 **같은 자리에 되씀** (`+0x24`·`+0x27`), 내용 안 바뀜 | 없음 | 없음 | 없음 |
| `RtlInsertInvertedFunctionTable` (`0xa4074` · 140) | `RCX` 이미지 시작, `EDX` `SizeOfImage` | 표 칸 (아래 Entry), 락 `LdrpInvertedFunctionTableSRWLock` (주소 넘김) | `RtlImageDirectoryEntryToData(시작, 1, 3, &크기)` `+0x29 / +0x2e` · `RtlAcquireSRWLockExclusive` `+0x44 / +0x49` · `LdrProtectMrdata(0)` `+0x4b / +0x50` · `RtlpInsertInvertedFunctionTableEntry` `+0x5d / +0x62` · `LdrProtectMrdata(1)` `+0x67 / +0x6c` · `RtlReleaseSRWLockExclusive` `+0x87` 꼬리 JMP | 없음 | 없음 |
| `RtlpInsertInvertedFunctionTableEntry` (`0xa4110` · 227) | `RDX` 시작, `R8` `FunctionTable`, `R9D` `SizeOfImage`, 스택 `[RSP+0x50]` `SizeOfTable` | `Epoch` +1 두 번 (`+0x34`·`+0xb9`, `INC.LOCK`), 칸 4줄 (`+0x9f`·`+0xa4`·`+0xa9`·`+0xae`), `CurrentSize` +1 (`+0xb3`, LOCK 없음). 가득 차면 (`+0x24 JZ → +0xdb`) `Overflow` = 1 만 | 메모리 옮기기 `+0x90 / +0x95` (끼울 자리가 끝이 아닐 때 — 같은 주소 이름 4개: `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) | 없음 | 없음 |
| `RtlRemoveInvertedFunctionTable` (`0xa7968` · 46) | `RCX` 이미지 시작 | 락만 | `RtlAcquireSRWLockExclusive` `+0x10 / +0x15` · `RtlxRemoveInvertedFunctionTable` `+0x18 / +0x1d` · `RtlReleaseSRWLockExclusive` `+0x29` 꼬리 JMP | 없음 | 없음 |
| `RtlxRemoveInvertedFunctionTable` (`0xa79a0` · 78) | `RDX` 이미지 시작 | 없음 — 칸 `CurrentSize − 1` ~ 1 을 뒤에서 앞으로 `ImageBase` 비교 (`+0x20`), 못 찾으면 조용히 끝 (`+0xf JZ`·`+0x2c` 고리 끝 → `+0x2e`) | `LdrProtectMrdata(0)` `+0x37 / +0x3c` · `RtlpRemoveInvertedFunctionTableEntry(번호)` `+0x3e / +0x43` · `LdrProtectMrdata(1)` `+0x48 / +0x4d` | 없음 | 없음 |
| `RtlpRemoveInvertedFunctionTableEntry` (`0xa87a8` · 105) | `EDX` 칸 번호 | `Epoch` +1 두 번 (`+0x7`·`+0x5d`), `CurrentSize` −1 (`+0x57`). 뒤 칸을 앞으로 당김 — **마지막 칸 바이트는 안 지움** | 메모리 옮기기 `+0x4a / +0x4f` (`CurrentSize == 2` 면 `+0x17 JZ → +0x55` 로 건너뜀. 같은 주소 이름 4개, 위와 같음) | 없음 | 없음 |
| `RtlpxLookupFunctionTable` (`0x14840` · 1339) | `RCX` 찾을 주소, `RDX` 결과 24B | 결과 24B 만 (입구 `+0x23`·`+0x27` 에서 `+0x8`·`+0x10` 비움). 빠른 길에선 락 값 | `LdrInitState == 3` 이면 빠른 길 (표 이진 검색 + 공유 락), 아니면 (`+0x2b JNZ → +0x2a3`) 느린 길: `RtlImageNtHeaderEx(1, …)` `+0x35b / +0x360` (+ 안 타는 `RtlpImageDirectoryEntryToData32` `+0x469`, `RtlAddressInSectionTable` `+0x4a9`) | 느린 길: **`NtQueryVirtualMemory` `0x23`** call `+0x2d0 / +0x2d5`: (−1 `+0x2c7`, 주소, 클래스 6 `+0x2c3`, 버퍼 `[RSP+0x30]`, `0x18` `+0x2b7`, `NULL` `+0x2a5`). 빠른 길 경합 때만 `NtAlertThreadByThreadIdEx` `0x72` (`+0x406`), `NtSetInformationThread` `0xd` (`+0x508`, 클래스 `0x38`) | `RAX` = `FunctionTable` 또는 0 |
| `LdrControlFlowGuardEnforcedWithExportSuppression` (`0xa60c0` · 45) | 없음 | 없음 — `LdrSystemDllInitBlock +0xb8` (`+0x0`) · `+0x9c` (`+0xa`) · `+0xa0` (`+0x13`) 읽기만 | 없음 (잎 함수) | 없음 | `AL` = 1 ⇔ `+0xb8 ≠ 0` 그리고 `+0x9c` 비트 0 = 0 그리고 `(+0xa0 >> 40) & 3 == 3` (`+0x1c ~ +0x26`). 아니면 0 (`+0x2b`). **test.exe = 0** (`+0xb8 = 0`, `+0x8 JZ → +0x2b`) |
| `LdrpHandleUserCopyAttributeList` (`0x15ddb0` · 372) | 없음 (PEB → `ProcessParameters +0x448`) | `.data` `0x1ce268` ← 속성 `0xa0022` 값 0/1 (`+0x82`), 끝에 `ProcessParameters +0x448 = 0` (`+0x15b`) | `LdrpLogInternal` (오류 때) | 없음 | `EAX` = 0 또는 `0xc000000d`. test.exe 는 목록 0 이라 바로 0 |

- syscall 번호 `[정적]`: `NtProtectVirtualMemory.asm` `+0x3 MOV EAX,0x50`, `NtQueryVirtualMemory` `0x23`, `NtSetInformationThread` `0xd`, `NtAlertThreadByThreadIdEx` `0x72` (결과서).
- 공용 부품 (락·메모리 옮기기) 은 이름만. `RtlImageDirectoryEntryToData` → `RtlImageNtHeaderEx`·`RtlAddressInSectionTable` 에서 끝 (syscall 없음, 결과서).
- **`.mrdata` 를 이 구간 뒤에 또 여는 곳** (flow-writer, 지도 `_map/LdrInitializeThunk_edges.tsv` + `LdrpInitializeProcess.asm`): `+0xd9f XOR ECX,ECX` → call `+0xda1` (ret `+0xda6`) `LdrProtectMrdata(0)` → `+0xdb1 LEA RCX,[LdrpKnownDllDirectoryHandle]` → `+0xdb8 NtOpenDirectoryObject` → `+0xdc1 LEA ECX,[RBX−0x2f]` → call `+0xdc4` (ret `+0xdc9`) `LdrProtectMrdata`. 그래서 카운터 0 → 1 (`NtProtectVirtualMemory` 4) → 0 (`NtProtectVirtualMemory` 2) 이 한 번 더 있을 것이다. 둘째 호출의 `ECX` 는 `RBX` 값을 안 따라가서 1 (닫기) 이라는 건 `[추측]`. 지도에 `LdrProtectMrdata` caller 는 21곳 (예: `LdrpInitializeExecutionOptions +0x373·+0x384`, `LdrpInitializeCfgScpHelpers +0x5a·+0x80`).

### 2. `LdrpInvertedFunctionTables` — 머리·칸 표와 구간 뒤 기대 값 `[정적]`

- 전역 `LdrpInvertedFunctionTables` (`.mrdata` rva `0x1e7430`) = PDB 타입 **`_INVERTED_FUNCTION_TABLE_USER_MODE`, `0x3010` B** = 머리 `0x10` + 칸 512 × `0x18` (`pdb_struct.py`, 결과서). 다음 공개 이름 `LdrpGuardCheckIcallNoESFptr` (`0x1ea440`) 까지 거리도 `0x3010`.
  - PE_emulator 가 적은 "0x28B" 는 머리 + 칸 0 (`0x10 + 0x18`) 만 본 것으로 읽힌다 `[추측]` (결과서).
- 커널도 이 표 주소를 안다 `[관찰]`: `PsNtdllExports +0x68 KiUserInvertedFunctionTable = 7ffafb847430` = ntdll + `0x1e7430` (`ms/logs/04_baggage-read_supp_K1.txt:172`).

| 칸 | 이름 (PDB) | 파일 처음 값 | 쓰는 곳 (줄) | `+0x475` 뒤 기대 값 (test.exe) |
|---|---|---|---|---|
| 머리 `+0x0` dword | `CurrentSize` | **1** | `RtlpInsertInvertedFunctionTableEntry +0xb3` (+1) · `RtlpRemoveInvertedFunctionTableEntry +0x57` (−1) | **1** |
| 머리 `+0x4` dword | `MaximumSize` | **`0x200`** | 쓰는 곳 없음 (읽기 `RtlpInsert…Entry +0x18`) | `0x200` |
| 머리 `+0x8` dword | `Epoch` | 0 | `INC.LOCK` — `RtlpInsert…Entry +0x34`·`+0xb9`, `RtlpRemove…Entry +0x7`·`+0x5d` | **4** |
| 머리 `+0xc` byte | `Overflow` | 0 | `RtlpInsert…Entry +0xdb` (= 1, `CurrentSize == MaximumSize` 일 때). 읽기 `RtlpxLookupFunctionTable +0x296` | 0 |
| 칸 0 `+0x10` (`0x18`B) | `FunctionTable` `+0x0` · `ImageBase` `+0x8` · `SizeOfImage` `+0x10` (dword) · `SizeOfTable` `+0x14` (dword) | 전부 0 | **`LdrpInitializeProcess +0x44e · +0x45e · +0x467`** (직접) | `ntdll시작 + 0x1d5000` · `ntdll시작` · `0x267000` · `0x108d8` |
| 칸 1 ~ 511 `+0x28` ~ | 같은 모양, `ImageBase` 오름차순 | 전부 0 | `RtlpInsert…Entry +0x9f · +0xa4 · +0xa9 · +0xae` (칸 하나에 4줄) / `RtlpRemove…Entry` 의 메모리 옮기기 | 칸 1 = 칸 0 과 같은 ntdll 복사본 (빼기가 바이트를 안 지움). 칸 2 ~ = 0 |

- 기대 값의 숫자는 ntdll 파일에서 (예외 디렉터리 RVA `0x1d5000`, 크기 `0x108d8`, `SizeOfImage` `0x267000`, 결과서). 시작 주소는 ASLR 마다 다르다.
- **칸 0 은 정렬 구간 밖**이다: 이진 검색은 칸 1 ~ `CurrentSize − 1` 만 (`RtlpxLookupFunctionTable +0x99 MOV R8D,1` · `+0xa6 DEC R9D`), 끼우기도 칸 1 부터 비교 (`RtlpInsert…Entry +0x4d`). 칸 0 은 따로 직접 읽는 곳 15곳이 본다 (결과서 이름만: `RtlLookupFunctionEntry +0x1d1` 등).
- `[관찰]` run44 P1a (`ms/logs/04_baggage-read_P1a.txt:5814 ~ 5817`, 구획 `17b NTDLL_MRDATA`): `CurrentSize 2` · `MaximumSize 0x200` · `Epoch 6` · `Overflow 0`, 칸 0 = `{7ffafb835000, 7ffafb660000, 0x267000, 0x108d8}`, 칸 1 = `{7ff61a3f5000, 7ff61a370000, 0x8c000, 0x4fbc}` (test.exe). → `Epoch 6 = 4 (ntdll 넣고 뺌) + 2 (exe 넣음)`, `CurrentSize 2 = 1 + exe` 로 **정적 셈과 맞다**. exe 를 넣는 자리는 `LdrpProcessMappedModule +0x177` (run35). P1b `CurrentSize 6`·`Epoch 0xe` (결과서).

### 3. `LdrpMrdataUnprotected` 카운터 규칙 `[정적]`

- 전역 `LdrpMrdataUnprotected` (`.mrdata` rva `0x1e7128`, dword). **파일 처음 값 = 1** (PE 파일 바이트, `.mrdata` 파일 오프셋 `0x1e0128`, 결과서). 구역 특성 `0xc0000040` (읽기·쓰기) 로 매핑되어 처음엔 쓸 수 있다.
- 규칙 (`LdrProtectMrdata`, 락 `LdrpMrdataLock` 안에서):
  - **열기 (`ECX = 0`)**: 카운터 0 이면 `LdrpChangeMrdataProtection(4)` (→ `NtProtectVirtualMemory` `PAGE_READWRITE`) 뒤 +1 (`+0x8f JZ → +0xaf`). 카운터가 0 도 −1 도 아니면 syscall 없이 +1 (`+0x94 JNZ → +0xb9`, **중첩**). −1 이면 `INT 0x29` 코드 `0xe` (`+0x91 CMP`·`+0x96`).
  - **닫기 (`ECX ≠ 0`)**: 카운터 0 이면 `INT 0x29` 코드 `0xe` (`+0x5b JZ → +0x96`). 아니면 −1 (`+0x5d`·`+0x60`), 결과가 0 이면 `LdrpChangeMrdataProtection(2)` (→ `PAGE_READONLY`), 아니면 syscall 없이 끝 (`+0x66 JNZ → +0x72`).
  - **0 ↔ 1 을 넘을 때만 syscall.** `LdrpChangeMrdataProtection` 은 처음 불릴 때 (`LdrpMrdataBase == 0`) `LdrpLocateMrdata` 로 `.mrdata` 시작·크기를 먼저 정한다.
- 이 구간 카운터·표 차례 (결과서 표, 줄은 flow-writer 가 `.asm` 으로 확인):

| 자리 (call / ret) | 하는 일 | 카운터 | `CurrentSize` | `Epoch` | 칸 0 | 칸 1 | syscall |
|---|---|---|---|---|---|---|---|
| 시작 (파일 값) | — | **1** | 1 | 0 | 0 | 0 | — |
| `+0x415 / +0x41a` `RtlInsertInvertedFunctionTable(RSI = ntdll 시작, EBX = SizeOfImage)` | 안에서 열기 (`+0x4b`) 1→2 · 끼우기 · 닫기 (`+0x67`) 2→1. `CurrentSize` 가 1 이라 `RtlpInsert…Entry +0x49 JZ → +0x95` 로 바로 칸 1 | 1 | **2** | **2** | 0 | ntdll | 없음 (0 을 안 넘음) |
| `+0x41a ~ +0x437` | 지역 24B (`[RSP+0x1d0 ~ +0x1e7]`) 비움 | | | | | | |
| `+0x43a / +0x43f` `RtlpxLookupFunctionTable(RSI, &지역)` | `LdrInitState = 0` 이라 느린 길 (아래 4절) — 지역 24B 를 채움. 반환 `RAX` 안 씀 | 1 | 2 | 2 | 0 | ntdll | `NtQueryVirtualMemory` `0x23` |
| `+0x43f XOR ECX,ECX` → `+0x441 / +0x446` `LdrProtectMrdata(0)` | **열기** 1→2 | 2 | | | | | 없음 |
| `+0x44e MOVAPS [0x1e7440]` | 칸 0 `+0x0 ~ +0xf` ← 지역 (`FunctionTable`·`ImageBase`) | | | | 반쯤 | | |
| `+0x45e MOVSD [0x1e7450]` | 칸 0 `+0x10 ~ +0x17` ← 지역 (`SizeOfImage`·`SizeOfTable`) | | | | | | |
| `+0x467 MOV dword [0x1e7450],EBX` | 칸 0 `SizeOfImage` ← 헤더 `SizeOfImage` (`+0x40d` 값) 로 덮음 | | | | **ntdll** | | |
| `+0x470 / +0x475` `RtlRemoveInvertedFunctionTable(RSI)` | 안에서 열기 (`Rtlx… +0x37`) 2→3 · 칸 1 빼기 (`CurrentSize` 2 → 1, `+0x17 JZ` 로 옮기기 없음) · 닫기 (`+0x48`) 3→2 | 2 | **1** | **4** | ntdll | ntdll (남음) | 없음 |
| `+0x475 MOV EBX,1` · `+0x47a MOV ECX,EBX` → `+0x47c / +0x481` `LdrProtectMrdata(1)` | **닫기** 2→1 | 1 | 1 | 4 | | | 없음 |
| `+0x481 / +0x486` `LdrpCaptureCriticalThunks` | 이름만 — 쓰는 칸 `LdrpThunkSignature` (`+0x95`)·`LdrpDetourExist` (`+0xa1`) 는 `.data`. 안에 `NtQueryVirtualMemory` (call `+0x67`, 클래스 4) | 1 | | | | | (`+0x67`) |
| `+0x486 MOV ECX,EBX` → `+0x488 / +0x48d` `LdrProtectMrdata(1)` | **닫기** 1→0 → `LdrpChangeMrdataProtection(2)` → `LdrpMrdataBase == 0` 이라 `+0x14 LdrpLocateMrdata` (**`LdrpMrdataSize` `+0x52`·`LdrpMrdataBase` `+0x59` 처음 씀**) → `NtProtectVirtualMemory(PAGE_READONLY)` | **0** | 1 | 4 | ntdll | ntdll | **`NtProtectVirtualMemory` `0x50`** |
| `+0x48d / +0x492` `LdrControlFlowGuardEnforcedWithExportSuppression` | test.exe 0 → `+0x492 CMP EAX,EBX` · `+0x494 JNZ → +0x4a4` (아래 두 줄 건너뜀) | | | | | | 없음 |
| `+0x496 ~ +0x49d` (반환 1 일 때만) | `LdrpSuppressedExportOverrideListPtr` (`.data` `0x1d2920`) ← `&LdrpSuppressedExportOverrideList` (`.rdata` `0x1740f0`) | | | | | | |

- **카운터가 1 에서 시작해야 맞는다**: 이 구간의 열기는 셋 (`Insert +0x4b`, `+0x441`, `Rtlx… +0x37`), 닫기는 넷 (`Insert +0x67`, `Rtlx… +0x48`, `+0x47c`, `+0x488`) — 순 −1. 0 에서 시작하면 `+0x488` 이 `INT 0x29` 로 죽는다. **ntdll 초기화가 `.mrdata` 를 처음 읽기 전용으로 바꾸는 자리가 `+0x488`** 이다 (결과서. `_LdrpInitialize` 의 `LdrInitializeMrdata` 와 `+0x415` 앞 호출들은 `LdrProtectMrdata` 를 안 부름).
- `[관찰]` run44 P1a: `LdrpMrdataSize = 0x35b8` · `LdrpMrdataBase = 7ffafb847000` (= ntdll `7ffafb660000` + `0x1e7000`) · `LdrpMrdataUnprotected = 0` · `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` = 0 (`04_baggage-read_P1a.txt:5748 ~ 5749`, `:5765`).
- PE_emulator 가 `+0x3e6 ~ +0x4a1` 을 채울 순서 (결과서 요약 그대로): ① 카운터 1. `Insert(ntdll)` → 칸 1·`CurrentSize` 2·`Epoch` 2 (syscall 없음) ② `Lookup(ntdll)` → 지역 24B (`NtQueryVirtualMemory` 1번) ③ 열기 (2). 칸 0 ← 24B, `SizeOfImage` 를 헤더 값으로 덮음 ④ `Remove(ntdll)` → `CurrentSize` 1·`Epoch` 4 ⑤ 닫기 (1), `LdrpCaptureCriticalThunks`, 닫기 (0) → 이때 `LdrpMrdataBase`·`Size` 처음 써지고 `NtProtectVirtualMemory(PAGE_READONLY)` ⑥ CFG 확인 = 0 → `LdrpSuppressedExportOverrideListPtr` 는 0 그대로.

### 4. 함수별 차례 `[정적]` (`.asm` 줄)

```
LdrpInitializeProcess (0x18008c404)                 RSI = 2번째 인자 = ntdll 시작 (+0x2d MOV RSI,RDX)
 │  +0x3d5 TEST DL,DL (DL = LdrpIsSecureProcess, +0x372) → +0x3d7 JNZ +0x3e6
 ├─ call +0x3d9 / ret +0x3de  LdrpHandleUserCopyAttributeList        (보안 프로세스 아닐 때만)
 │     PEB → ProcessParameters +0x448 목록. 비면 +0x23 JZ +0x15b (+0x448 = 0, 반환 0)
 │     항목 속성 == 0xa0022 (+0x47) 이고 값 & 3 == 값 ≤ 1 이면 +0x82 [0x1ce268] = 값
 │  +0x3e0 JS +0xa7b (실패 끝)
 ├─ call +0x400 / ret +0x405  RtlImageNtHeaderEx(3, RSI, 0, &헤더)  → +0x40d EBX = SizeOfImage
 ├─ call +0x415 / ret +0x41a  RtlInsertInvertedFunctionTable(RSI, EBX)
 │     ├─ +0x29  RtlImageDirectoryEntryToData(RSI, 1, 3, &크기)   예외 디렉터리
 │     ├─ +0x44  RtlAcquireSRWLockExclusive(&LdrpInvertedFunctionTableSRWLock)
 │     ├─ +0x4b  LdrProtectMrdata(0)                              카운터 1 → 2
 │     ├─ +0x5d  RtlpInsertInvertedFunctionTableEntry
 │     │     +0x18 CMP CurrentSize,MaximumSize → +0x24 JZ +0xdb (Overflow = 1)
 │     │     +0x34 Epoch++ → +0x49 JZ +0x95 (CurrentSize == 1) → +0x9f ~ +0xae 칸 1 → +0xb3 CurrentSize++ → +0xb9 Epoch++
 │     ├─ +0x67  LdrProtectMrdata(1)                              카운터 2 → 1
 │     └─ +0x87  ⇒ RtlReleaseSRWLockExclusive
 ├─ call +0x43a / ret +0x43f  RtlpxLookupFunctionTable(RSI, &지역24B)
 │     +0x16 CMP [LdrInitState],3 → +0x2b JNZ +0x2a3 (LdrInitState = 0)
 │     ├─ +0x2d0  NtQueryVirtualMemory(−1, RSI, 6, [RSP+0x30], 0x18, NULL)
 │     │     +0x2d7 JS · +0x2e5 JZ · +0x2f2 JNZ · +0x2fa JNZ · +0x303 JC → 모두 +0x487 (0 반환)
 │     │     +0x30d 결과+0x10 = SizeOfImage · +0x310 결과+0x8 = 이미지 시작   (결과 = RBX, +0x13 MOV RBX,RDX)
 │     ├─ +0x35b  RtlImageNtHeaderEx(1, 이미지 시작, 0, &헤더)
 │     │     +0x381 ~ +0x389 PE32+ · +0x38f 디렉터리 > 3 · +0x39c RVA = [헤더+0xa0] · +0x3aa ~ +0x3b0 결과+0x14 = 크기
 │     │     +0x3b3 TEST SIL,SIL (매핑된 이미지) → +0x3bc FunctionTable = RVA + 시작 → +0x3d2 결과+0x0 → +0x3d5 JMP +0x165
 ├─ call +0x441 / ret +0x446  LdrProtectMrdata(0)                   (+0x43f XOR ECX,ECX) 카운터 1 → 2
 │  +0x44e · +0x45e · +0x467  칸 0 = 지역 24B, SizeOfImage = EBX
 ├─ call +0x470 / ret +0x475  RtlRemoveInvertedFunctionTable(RSI)
 │     ├─ +0x10  RtlAcquireSRWLockExclusive
 │     ├─ +0x18  RtlxRemoveInvertedFunctionTable
 │     │     +0x6 EBX = CurrentSize − 1 (= 1) → +0x20 CMP ImageBase → +0x23 JZ +0x35
 │     │     ├─ +0x37  LdrProtectMrdata(0)                        카운터 2 → 3
 │     │     ├─ +0x3e  RtlpRemoveInvertedFunctionTableEntry(1)
 │     │     │     +0x7 Epoch++ → +0x17 JZ +0x55 (CurrentSize == 2) → +0x57 CurrentSize = 1 → +0x5d Epoch++
 │     │     └─ +0x48  LdrProtectMrdata(1)                        카운터 3 → 2
 │     └─ +0x29  ⇒ RtlReleaseSRWLockExclusive
 ├─ call +0x47c / ret +0x481  LdrProtectMrdata(1)                   카운터 2 → 1
 ├─ call +0x481 / ret +0x486  LdrpCaptureCriticalThunks             (이름만)
 ├─ call +0x488 / ret +0x48d  LdrProtectMrdata(1)                   카운터 1 → 0
 │     +0x47 BTS.LOCK [LdrpMrdataLock] → +0x4f 카운터 읽기 → +0x5d −1 → +0x60 저장 → +0x66 (0 이라 안 뜀)
 │     └─ +0x6d  LdrpChangeMrdataProtection(2)
 │           +0x8 Base 읽기 → +0x12 JNZ (0 이라 안 뜀)
 │           ├─ +0x14  LdrpLocateMrdata
 │           │     ├─ +0x22  RtlImageNtHeaderEx(3, ntdll, 0, &헤더)
 │           │     ├─ +0x36  RtlSectionTableFromVirtualAddress(헤더, ntdll, 0x1ea450)   → .mrdata 구역 헤더
 │           │     ├─ +0x4d  LdrpMakePermanentImageCommit(ntdll+0x1e7000, 0x35b8)     4쪽 되쓰기
 │           │     +0x52 LdrpMrdataSize = 0x35b8 · +0x59 LdrpMrdataBase = ntdll+0x1e7000
 │           └─ +0x51  NtProtectVirtualMemory(−1, &Base, &Size, 2, &옛보호)
 │     +0x84 ⇒ RtlReleaseSRWLockExclusive
 ├─ call +0x48d / ret +0x492  LdrControlFlowGuardEnforcedWithExportSuppression   → 0
 │  +0x492 CMP EAX,EBX (= 1) → +0x494 JNZ +0x4a4
 │  (1 일 때만) +0x496 LEA LdrpSuppressedExportOverrideList → +0x49d LdrpSuppressedExportOverrideListPtr
 └─ +0x4a4 MOVAPS XMM1,[LdrSystemDllInitBlock+0xa0]   (묶음 3 끝 → 묶음 4)
```

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 값 대조는 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt` 구획 `17b NTDLL_MRDATA`, `04_baggage-read_supp_K1.txt:172`) 와 ntdll PE 파일 바이트 (결과서). 날짜는 결과서 날짜.
- 03_sprint 열여덟째 바퀴. plan **Q111** — 사용자 결정 2026-11-02 (PE_emulator 요청 5, Q109 다음 · Q104 VM 단계 앞). **짐 지도 바퀴** — `baggage.md` 에 새 쓴 자리 (아래 "짐 지도 반영").
- 정적: ntdll (`ntdll.pdb`) `C3093720177DA6851519DCFFE8DF53FE1` · ntoskrnl (`ntkrnlmp.pdb`) `C29EBFB06B78B3C020DCA66D99713F9E1` (질문 2 속성 풀이 4함수만) — flow.md 기준과 같다 (결과서).
- 버전 대조 (결과서, `_calls.tsv` `ret_off`): `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807`, `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse` (call `+0x1bb6`), `LdrpInitializeNode +0x16b/+0x19c`, `LdrInitializeThunk +0xe`, `_LdrpInitialize +0x16c`, `LdrpLoadDll +0x100` — 모두 일치.

## 목적

- PE_emulator 다음 바퀴 (로드맵 2-B③b, `LdrpInitializeProcess +0x3e6 ~ +0x4a1`) 에서 "공사 중" 빈 함수로 둔 다섯 (`RtlInsertInvertedFunctionTable`·`RtlpxLookupFunctionTable`·`RtlRemoveInvertedFunctionTable`·`LdrProtectMrdata`·`LdrControlFlowGuardEnforcedWithExportSuppression`) 의 **본문·쓰는 칸·syscall** 을 줄기 끝까지 (요청서 1(a) ~ (e)).
- 짐 지도: baggage 3.9 `.mrdata` "쓴 자리 모름" 셋 (`LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataUnprotected`) 과 `LdrpInvertedFunctionTables` 칸 표.
- 덤 (급하지 않음, 요청서 2): `LdrpHandleUserCopyAttributeList +0x82` 가 쓰는 `.data 0x1ce268` 의 뜻, 속성 번호 `0xa0022`.
- 범위 밖 (요청서): 사진 P1a 의 표 실제 값 (VM, Q104 VM 단계), `LdrpCaptureCriticalThunks` 본문, 예외 처리 길, Q112·Q113 몫.

## 방법

- 정적: `_workspace/sast/55_func-table-mrdata.md` — ntdll 대상 8개 depth 1 (→ 22개) + 한 단계 아래 `LdrpLocateMrdata` depth 1·`NtProtectVirtualMemory` depth 0, ntoskrnl depth 0 넷 (`PspProcessUserCopyAttribute`·`PspCopyUserAttributes`·`PspGetFullAttribute`·`PspIsAttributeRemoved`).
  - 이미 analyze 된 프로젝트 — **다시 analyze 안 함**. 이미 있던 `LdrpInitializeProcess`·`LdrpHandleUserCopyAttributeList`·`RtlLookupFunctionEntry`·`LdrpCaptureCriticalThunks`·`RtlpHpOptIntoSegmentHeap`·`PspBuildCreateProcessContext` 는 읽기만.
  - `_refs` 새로 (ntdll 9개 + `0x1ce268`): `LdrpInvertedFunctionTables`·`LdrpInvertedFunctionTableSRWLock`·`LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataUnprotected`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpMrdataLock`·`LdrpSuppressedExportOverrideListPtr`.
  - 그 밖 (결과서): PDB 타입 `pdb_struct.py`, PE 파일 `.mrdata` 처음 값 (`CurrentSize 1`·`MaximumSize 0x200`·`LdrpMrdataUnprotected 1`), ntdll 예외 디렉터리·`SizeOfImage`, 커널 `.rdata` 속성 표 (rva `0x2b750`).
- 결과서: `_workspace/sast/55_func-table-mrdata_result.md` (**분석성공**, PDB 일치, 1(a) ~ (e)·2 모두 답 — 부분 답: Insert 를 거치는 **이유**, 속성 `0x22` 공개 이름).
- 이 문서를 쓰며 다시 맞춘 것:
  - 오케스트레이터: `LdrProtectMrdata +0x60·+0x6d·+0xbb`, `LdrpChangeMrdataProtection +0x14·+0x51`, `LdrpLocateMrdata +0x52·+0x59`, `LdrpInitializeProcess +0x43f XOR ECX,ECX·+0x441·+0x47a·+0x47c·+0x486·+0x488·+0x48d`, `RtlpxLookupFunctionTable +0x2d0`, `LdrControlFlowGuardEnforcedWithExportSuppression +0x0 ~ +0x2d` 전부.
  - flow-writer: 위 1 ~ 4절의 모든 CALL 줄 (`CALL … ret=`) 과 점프 목적지 (`LdrProtectMrdata`·`LdrpChangeMrdataProtection`·`LdrpLocateMrdata`·`LdrpMakePermanentImageCommit`·`RtlInsert…`·`RtlpInsert…Entry`·`RtlRemove…`·`RtlxRemove…`·`RtlpRemove…Entry`·`RtlpxLookupFunctionTable +0x0 ~ +0x3d5`·`LdrpInitializeProcess +0x3d5 ~ +0x4bb`·`+0xd93 ~ +0xdcf`), `_refs` 다섯 (`0x1ce268`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpSuppressedExportOverrideListPtr`·`LdrpInvertedFunctionTableSRWLock`), `LdrpCaptureCriticalThunks +0x67 ~ +0xa1`, `RtlpHpOptIntoSegmentHeap +0x56 ~ +0x62`, ntoskrnl `PspBuildCreateProcessContext +0x1577 ~ +0x15a4`·`PspCopyAndFixupParameters +0x3eb`, 지도 간선 (`RtlAddGrowableFunctionTable +0x40 → LdrEnsureMrdataHeapExists`, `RtlpInsertOrRemoveScpCfgFunctionTable +0xc4 → RtlAddGrowableFunctionTable`, `LdrProtectMrdata` caller 목록), run44 P1a 로그 줄.
- 문서: 이 run 문서, `flow.md` (기준, 2.1 `PspBuildCreateProcessContext`·`PspCopyAndFixupParameters`, 2.4 `LdrpInitializeProcess` 트리·새 절 "묶음 3 뒤", `LdrpInitializeNtdllDataTableEntry` 절, 4장, 5장), `baggage.md` (아래), run35 문서 맨 위 한 줄.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntdll/*.asm` `CALL … ret=`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x3d9` | `+0x3de` | `ntdll!LdrpHandleUserCopyAttributeList` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x400` | `+0x405` | `ntdll!RtlImageNtHeaderEx` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x415` | `+0x41a` | `ntdll!RtlInsertInvertedFunctionTable` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x43a` | `+0x43f` | `ntdll!RtlpxLookupFunctionTable` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x441` | `+0x446` | `ntdll!LdrProtectMrdata` (ECX 0) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x470` | `+0x475` | `ntdll!RtlRemoveInvertedFunctionTable` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x47c` | `+0x481` | `ntdll!LdrProtectMrdata` (ECX 1) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x481` | `+0x486` | `ntdll!LdrpCaptureCriticalThunks` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x488` | `+0x48d` | `ntdll!LdrProtectMrdata` (ECX 1) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x48d` | `+0x492` | `ntdll!LdrControlFlowGuardEnforcedWithExportSuppression` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xda1` | `+0xda6` | `ntdll!LdrProtectMrdata` (ECX 0) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xdc4` | `+0xdc9` | `ntdll!LdrProtectMrdata` (ECX = `RBX − 0x2f`) | [정적] |
| `ntdll!RtlInsertInvertedFunctionTable` | `+0x29` · `+0x44` · `+0x4b` · `+0x5d` · `+0x67` | `+0x2e` · `+0x49` · `+0x50` · `+0x62` · `+0x6c` | `RtlImageDirectoryEntryToData` · `RtlAcquireSRWLockExclusive` · `LdrProtectMrdata` · `RtlpInsertInvertedFunctionTableEntry` · `LdrProtectMrdata` (+ `+0x87` 꼬리 JMP `RtlReleaseSRWLockExclusive`) | [정적] |
| `ntdll!RtlRemoveInvertedFunctionTable` | `+0x10` · `+0x18` | `+0x15` · `+0x1d` | `RtlAcquireSRWLockExclusive` · `RtlxRemoveInvertedFunctionTable` (+ `+0x29` 꼬리 JMP) | [정적] |
| `ntdll!RtlxRemoveInvertedFunctionTable` | `+0x37` · `+0x3e` · `+0x48` | `+0x3c` · `+0x43` · `+0x4d` | `LdrProtectMrdata` · `RtlpRemoveInvertedFunctionTableEntry` · `LdrProtectMrdata` | [정적] |
| `ntdll!LdrProtectMrdata` | `+0x6d` · `+0xb4` | `+0x72` · `+0xb9` | `ntdll!LdrpChangeMrdataProtection` (ECX 2 · 4) | [정적] |
| `ntdll!LdrpChangeMrdataProtection` | `+0x14` · `+0x51` | `+0x19` · `+0x56` | `ntdll!LdrpLocateMrdata` · `ntdll!NtProtectVirtualMemory` | [정적] |
| `ntdll!LdrpLocateMrdata` | `+0x22` · `+0x36` · `+0x4d` | `+0x27` · `+0x3b` · `+0x52` | `RtlImageNtHeaderEx` · `RtlSectionTableFromVirtualAddress` · `LdrpMakePermanentImageCommit` | [정적] |
| `ntdll!RtlpxLookupFunctionTable` | `+0x2d0` · `+0x35b` | `+0x2d5` · `+0x360` | `ntdll!NtQueryVirtualMemory` · `RtlImageNtHeaderEx` | [정적] |
| `nt!PspBuildCreateProcessContext` | `+0x1577` · `+0x159f` | `+0x157c` · `+0x15a4` | `Feature_CreateProcessSegmentHeapEnablement__private_IsEnabledDeviceUsageNoInline` · `nt!PspProcessUserCopyAttribute` | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0x3eb` | `+0x3f0` | `nt!PspCopyUserAttributes` | [정적] |

### 호출 관계

```
LdrpInitializeProcess  (묶음 3 뒤 — 4절 트리가 자세한 판)
 ├─ (1) +0x3de  LdrpHandleUserCopyAttributeList          (보안 프로세스 아닐 때)
 ├─ (2) +0x405  RtlImageNtHeaderEx
 ├─ (3) +0x41a  RtlInsertInvertedFunctionTable
 │              ├─ +0x50  LdrProtectMrdata(0)
 │              ├─ +0x62  RtlpInsertInvertedFunctionTableEntry
 │              └─ +0x6c  LdrProtectMrdata(1)
 ├─ (4) +0x43f  RtlpxLookupFunctionTable  ─ +0x2d5  NtQueryVirtualMemory (클래스 6)
 ├─ (5) +0x446  LdrProtectMrdata(0)
 ├─ (6) +0x475  RtlRemoveInvertedFunctionTable  ─ +0x1d  RtlxRemoveInvertedFunctionTable
 │                                                       ├─ +0x3c  LdrProtectMrdata(0)
 │                                                       ├─ +0x43  RtlpRemoveInvertedFunctionTableEntry
 │                                                       └─ +0x4d  LdrProtectMrdata(1)
 ├─ (7) +0x481  LdrProtectMrdata(1)
 ├─ (8) +0x486  LdrpCaptureCriticalThunks
 ├─ (9) +0x48d  LdrProtectMrdata(1)  ─ +0x72  LdrpChangeMrdataProtection(2)
 │                                           ├─ +0x19  LdrpLocateMrdata  ─ +0x52  LdrpMakePermanentImageCommit
 │                                           └─ +0x56  NtProtectVirtualMemory (PAGE_READONLY)
 └─ (10) +0x492 LdrControlFlowGuardEnforcedWithExportSuppression   → 0
```

## 확인된 것

- **1(a) 칸 이름** `[정적]`: `LdrpInvertedFunctionTables` = `_INVERTED_FUNCTION_TABLE_USER_MODE` (`0x3010` B), 머리 `CurrentSize`·`MaximumSize`·`Epoch`·`Overflow` + 칸 `_INVERTED_FUNCTION_TABLE_ENTRY` 모양 (`FunctionTable`·`ImageBase`·`SizeOfImage`·`SizeOfTable`) × 512 (2절).
- **1(a) "쓴 자리 모름" 셋** `[정적]`: `LdrpMrdataUnprotected` = **`LdrProtectMrdata` 몫** (`+0x60`·`+0xbb`). `LdrpMrdataSize`·`LdrpMrdataBase` = **직접은 아니고** 그 아래 `LdrpLocateMrdata +0x52`·`+0x59` (`LdrProtectMrdata +0x6d → LdrpChangeMrdataProtection +0x14` 길, `Base == 0` 일 때 한 번). 초기화에서 그 길에 처음 닿는 자리 = `LdrpInitializeProcess +0x488` (3절).
  - `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` (덤) 쓰는 줄 = `LdrEnsureMrdataHeapExists +0xef`·`+0xe8` 하나씩 (`_refs`). 부르는 쪽 `RtlAddGrowableFunctionTable` call `+0x40` (지도).
  - `LdrpMrdataLock` (`.data`) = `LdrProtectMrdata` 의 락 — 이름 짐작 (baggage 3.8) 이 코드로 맞다.
- **1(a) 인자·카운터** `[정적]`: 3절. 파일 값 1 (PE 바이트) — 열기 셋·닫기 넷이라 1 에서 시작해야 맞아떨어진다.
- **1(b) syscall** `[정적]`: 칸 표 함수 다섯 (Insert·Remove·Rtlx·두 Entry) 은 syscall 없음. `.mrdata` 보호 바꾸기는 `LdrpChangeMrdataProtection +0x51` 의 `NtProtectVirtualMemory` (`0x50`) 하나. 룩업 느린 길은 `NtQueryVirtualMemory` (`0x23`). 사슬 `RtlRemoveInvertedFunctionTable → RtlxRemoveInvertedFunctionTable → RtlpRemoveInvertedFunctionTableEntry` 는 요청서 짐작대로.
- **1(c) 왜 넣고 빼나** `[정적]`: 이 구간은 ntdll 자기 이야기 (`RSI` = ntdll 시작). 칸 0 은 정렬 구간 밖이라 ntdll 을 칸 0 에 두고 정렬 구간에선 뺀다. 순효과 칸 0 = ntdll, `CurrentSize` 1, `Epoch` +4, 칸 1 바이트는 복사본으로 남음. **왜 칸 0 에 바로 안 쓰고 Insert·Lookup 을 거치는지는 코드로 못 정한다** `[추측: 칸 만들기를 한 곳에 모음]`.
  - 칸 0 값 출처: `+0x44e`·`+0x45e` = `+0x43a` 가 채운 지역 24B, `+0x467` 만 헤더 `SizeOfImage`.
  - 세 `LdrProtectMrdata` 인자: `+0x441` 0 (열기), `+0x47c` 1, `+0x488` 1 (닫기).
- **1(d) CFG 반환** `[정적]` + `[관찰]`: 읽는 칸은 `LdrSystemDllInitBlock +0xb8`·`+0x9c`·`+0xa0` 셋뿐 (PEB·이미지 헤더 안 봄). run44 K1·P1a·P1b·P2 모두 `+0xb8 = 0` (결과서) → **0 반환** → `+0x49d` 안 탐 → `LdrpSuppressedExportOverrideListPtr` 0 그대로 (run44 끝까지 0). PE_emulator 의 `[추측]` ("1 이 아님") 이 맞다.
  - 칸 이름 (`CfgBitMap`·`Flags` 비트 0 `CfgOverride`·완화 옵션 비트 40~41) 은 공개 자료에서 빌린 `[추측]` — 두 PDB 에 블록 타입 없음 (run43).
  - `+0xb8 = 0` 인 이유 `[소스]`+`[추측]`: `sample/build.bat` 에 `/guard:cf` 가 없고 test.exe `DllCharacteristics = 0x8160` (GUARD_CF `0x4000` 없음, 결과서) → 커널이 CFG 비트맵을 안 만든 것으로 보인다.
  - 같은 함수를 부르는 곳 11 (지도, 이름만): `LdrpDoPostSnapWork +0x68`·`RtlUnwindEx +0x5c4`·`RtlValidateUserCallTarget +0x5b` 등.
- **1(e) 룩업 갈래** `[정적]`: `LdrInitState` 는 이때 0 (`_LdrpInitialize +0x101` 이 0, 1·2·3 쓰는 줄 `+0x1a4c`·`+0x1b1e`·`+0x17dd` 는 모두 뒤, `_refs/LdrInitState.tsv`) → `+0x2b JNZ → +0x2a3` 느린 길 → **표를 안 보고** `NtQueryVirtualMemory` + PE 헤더로 24B 를 만든다. 빠른 길 (`LdrInitState == 3`) 은 이름·조건만 (결과서 1(e)).
- **2 `.data 0x1ce268`** `[정적]`: PDB 이름 없음. `LdrpThunkSignature` (`0x1cdfa0`) 짐작 크기 (`0x3fc`) 안이지만 그 칸을 쓰는 `LdrpCaptureCriticalThunks` 는 16B × 최대 5 = `0x50` B 만 쓴다 (`+0x77 CMP EBX,5`·`+0x95`) → **별개 이름 없는 변수**. 쓰기 `LdrpHandleUserCopyAttributeList +0x82` 하나, 읽기 `RtlpHpOptIntoSegmentHeap +0x56` 하나 (`_refs/0x1ce268.tsv`). 읽는 쪽은 `& 3 == 1` 이면 `*출력 = 1` (`+0x5c ~ +0x62`, 세그먼트 힙 쓰기). → **"프로세스 만들기 속성으로 받은 세그먼트 힙 요청 (0/1)"**. 이름은 우리가 붙인 것 `[추측]`.
  - test.exe: `ProcessParameters +0x448 = 0` (run44 `[관찰]` `+0x430 ~ +0x448` 0) → 목록 걷기를 안 타고 `0x1ce268` 은 처음 값 0.
- **2 `0xa0022`** `[정적]`: 번호 `0x22`, 플래그 `0x20000` (입력) + `0x80000`. 커널 속성 표 (`.rdata` rva `0x2b750`, `PspGetFullAttribute`) 에서 `0x80000` 을 가진 번호는 `0x22` 하나 (결과서, PE 바이트). `PspBuildCreateProcessContext +0x1584 TEST …,0x80000` 이 켜진 항목에만 call `+0x159f` (ret `+0x15a4`) `PspProcessUserCopyAttribute` 를 부르고, 그 앞 call `+0x1577` (ret `+0x157c`) 기능 확인이 꺼지면 `+0x157e JZ → +0x17db` 로 건너뛴다. 복사된 항목은 `PspCopyAndFixupParameters` call `+0x3eb` (ret `+0x3f0`) `PspCopyUserAttributes` 가 새 프로세스 쪽에 쓴다 (결과서). → `0x80000` = "유저 복사" 비트로 보인다 (함수 이름 근거 `[추측]`).
  - 값 아래 두 비트: 허용 0 또는 1 (`+0x5f ~ +0x7e`). 읽는 쪽은 `& 3 == 1` 일 때만 "쓰기".

## 결과서와 다르게 적은 곳

- (결과서 2-1) "`LdrpCaptureCriticalThunks` … `+0x7a CMP EBX,5`" → `.asm` 은 **`+0x77 CMP EBX,0x5`**, `+0x7a` 는 `JNC`. 뜻은 같다 (16B × 최대 5).
- (결과서 1(e) 4) "PE32+ (`+0x389` 이 `0x20b`)" → `+0x381 MOV EDX,0x20b` · `+0x386 CMP` · `+0x389 JNZ` (아니면 `+0x47d` — `0xc000000d` 길). 뜻은 같다.
- (결과서 "짐 지도 반영거리" `LdrpMrdataHeap`·`HeapUnprotected` 줄) "**초기화 길 밖** (`RtlAddGrowableFunctionTable +0x40`)" → 이 문서·baggage 는 **"② 조건부"** 로 적었다. 까닭: run51 (flow.md 2.4) 이 초기화 길 안에서 `LdrpInitializeNtdllDataTableEntry → LdrpProcessMappedModule (call +0x19e) → RtlpInsertOrRemoveScpCfgFunctionTable (call +0xc4) → RtlAddGrowableFunctionTable` 길을 이미 적어 두었다 (지도 간선도 있음, "ntdll 안 CFG 갈래 `[공백]`"). `RtlAddGrowableFunctionTable +0x40` 이 `LdrEnsureMrdataHeapExists` 를 부른다 (지도). test.exe 에선 사진 P1a ~ P2 모두 0 이라 쓰기까지 안 간 것이다 `[추측]` (`LdrEnsureMrdataHeapExists +0xa` 가 `LdrControlFlowGuardEnforced` 를 먼저 부르는 것 — 지도 — 도 그 갈래일 것 `[추측]`). 숫자는 어느 쪽이든 "ntdll 이 씀" 이라 2장 셈은 같다.
- (결과서 1(e) 3) "`+0x309 · +0x310` 결과 `SizeOfImage`·`ImageBase` 저장" → `.asm` 은 `+0x309 MOV EAX,[RSP+0x38]` (읽기) · `+0x30d MOV [RBX+0x10],EAX` (`SizeOfImage` 저장) · `+0x310 MOV [RBX+0x8],RDX` (`ImageBase` 저장) — 저장 줄은 **`+0x30d`·`+0x310`**. 뜻은 같다 (4절 트리는 처음부터 `+0x30d`·`+0x310`).
- 나머지 오프셋은 결과서와 `.asm` 이 같다 (오케스트레이터·flow-writer 확인 목록은 "방법").

## 정정

- **run35 Q67 (a)** (run35 문서 맨 위에 한 줄, flow.md 2.4 `LdrpInitializeNtdllDataTableEntry` 절·4장 Q67 줄): "ntdll 예외 표를 등록하는 곳 — 후보 `+0x45e` 근처 쓰기 / `+0x41a Insert`" 의 답은 **둘 다**. `+0x415` (ret `+0x41a`) `RtlInsertInvertedFunctionTable` 이 ntdll 을 칸 1 에 올리고 (`Epoch` +2), `+0x44e ~ +0x467` 이 칸 0 에 놓고, `+0x470` (ret `+0x475`) `RtlRemoveInvertedFunctionTable` 이 칸 1 을 정렬 구간에서 뺀다. 그래서 ntdll 항목은 `InExceptionTable` (비트 9) 를 미리 켜 `LdrpProcessMappedModule +0x15e` 에서 `+0x177` Insert 를 건너뛴다. run35 의 `[추측]` ("다른 곳에서 이미 등록") 이 `[정적]` 으로 맞다 — 틀린 게 아니라 이음이다.
- **baggage 사진 분류 `LdrpMrdataUnprotected`** (run44 2.1·7.3): "`끝까지 0*`" → **`≤P1a`**. P1a ~ P2 값 0 은 그대로지만 파일 처음 값이 **1** 이라 0 은 누군가 쓴 값이다 (`+0x488`). run44 표의 `*` 판정은 "처음 값 0" 을 전제했다.
- **baggage 7.1 "아는 칸 중 끝까지 0 … 전역 16"** 은 옛 숫자였다 (끝까지 0 전역 297 − 그중 모름 271 = 26 이어야 맞음). 이번에 296 − 266 = **30** 으로 고치며 같이 바로잡음 (flow-writer 셈).

## 짐 지도 반영 (`baggage.md`)

- 3장에 쓴 자리를 넣었다 (근거 칸 `sast55`, 줄 끝 **"Q111 로 닫힘"**):
  - 3.8 `.data` **새 줄 4** — `0x1ce268` (목록 밖 자리, 전역 수에 안 셈, 조건부 안 탐) · `LdrpInvertedFunctionTableSRWLock` (주소 넘김, 끝까지 0) · `LdrpSuppressedExportOverrideListPtr` (`+0x49d`, 조건부 안 탐) · `LdrpMrdataLock` 둘째 줄 (`LdrProtectMrdata +0x47` 잡기). 이미 있던 `LdrpMrdataLock` 줄의 "락 `[추측]`" 은 `[정적]` 으로.
  - 3.9 `.mrdata` **새 줄 6** — `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` (② 조건부) · `LdrpMrdataSize`·`LdrpMrdataBase` (`LdrpLocateMrdata`) · `LdrpMrdataUnprotected` (`LdrProtectMrdata`) · `LdrpInvertedFunctionTables` 셋 (`RtlpInsert…Entry` / `LdrpInitializeProcess` 직접 / `RtlpRemove…Entry`). 새 **3.9.1** 칸 표 (머리 4 + 칸 0 + 칸 1 ~ — 전역 하나 안쪽이라 2장 칸 수에 안 셈).
- 전역 8개 닫힘: `.mrdata` 6 (`≤P1a` 3 · 분류 정정 뒤 `≤P1a` 1 · `끝까지 0` 2) · `.data` 2 (`끝까지 0` 2).
- 2장 숫자: 전역 쓴 자리 아는 것 105 → **113** (`.data` 89 → 91 · `.mrdata` 15 → 21) · 모름 388 → **380**. 사진 `끝까지 0` 전역 297 → **296** (`.mrdata` 32 → 31, 정정). 3장 줄 647 → **657**. 구조체 숫자는 그대로.
- 4장 짝 1줄 (`LdrpInvertedFunctionTables` ↔ `PsNtdllExports.KiUserInvertedFunctionTable`), 5장 ② 묶음 3 순서, 6장 공용 부품 1줄 (`LdrProtectMrdata` 열고 닫기), 7.3·7.5 정리, 새 **7.20** (동적 선택 확인 → Q104 VM 단계, 정적 `[공백]` 둘).

## 아직 확인 필요

결과서 "동적분석으로 확인해야 할 것" (정적과 run44 P1a 가 이미 맞아서 **꼭 필요한 것은 없음**) — 대기열: **Q104 VM 단계** (Q111 의 VM 몫이 이미 얹혀 있음). ntdll 멈춤점은 모든 프로세스에 걸리니 test.exe 조건을 붙인다.

1. `ntdll!LdrpInitializeProcess+0x41a` (Insert 복귀): `dd ntdll+1e7430 L4` (→ `2, 0x200, 2, 0`), `dq ntdll+1e7440 L6` (칸 0 = 0, 칸 1 = ntdll), `dd ntdll+1e7128 L1` = 1.
2. `ntdll!RtlpxLookupFunctionTable+0x2d0` (느린 길 syscall 직전): `r rcx, rdx, r8, r9` = `-1`, ntdll 시작, `6`, 버퍼 — 정말 이 길인지 (`LdrInitState != 3`). `+0x2d5` 뒤 `dq @rsp+30 L3`.
3. `ntdll!LdrpInitializeProcess+0x475`: `dd ntdll+1e7430 L4` (→ `1, 0x200, 4, 0`), `dq ntdll+1e7440 L6` (칸 0 = ntdll, 칸 1 = 복사본), `dd ntdll+1e7128 L1` = 2.
4. `+0x486` 과 `+0x48d`: 카운터 1 → 0, `dq ntdll+1e7018 L1` / `dq ntdll+1e7028 L1` 이 `+0x486` 에선 0, `+0x48d` 에선 `0x35b8` / ntdll + `0x1e7000`. `+0x48d` 뒤 `!vprot ntdll+1e7000` = `PAGE_READONLY`.
5. 커널 `nt!NtProtectVirtualMemory` (test.exe 조건): `+0x488` 길 인자 (`R10 = -1`, 크기 `0x35b8` → 쪽 맞춤 `0x4000`, 새 보호 2) 가 그 구간 첫 호출인지. (덤) `+0xda1`·`+0xdc4` 한 쌍이 4 → 2 로 또 부르는지 (`+0xdc4` 의 `ECX` = 1 인지 — `r ecx`).
6. CFG 반환 0: `+0x492` 에서 `r eax` — 사진으로 이미 닫혀 생략 가능.

정적으로 남은 것 — `[공백]` (대기열 번호는 오케스트레이터가 붙임):

- `PspCopyUserAttributes` 가 쓴 목록이 새 프로세스 `ProcessParameters +0x448` 칸에 이어지는 줄 (`PspCopyAndFixupParameters` 쪽 — 이번엔 안 봄).
- `LdrpSuppressedExportOverrideListPtr` 를 **읽는** 곳 (`_refs` 는 쓰기 `+0x49d` 1줄뿐 — 레지스터 상대로 읽으면 안 잡힘).
- (질문 밖, 번호 없음) 속성 번호 `0x22` 공개 이름 (PDB 에 없음), `LdrpMakePermanentImageCommit` 의 목적 (`[추측]` 이미지 쪽을 개인 쪽으로 굳힘), Insert·Lookup 을 거치는 이유, `+0xdc4` 의 `ECX` 값 (`RBX` 따라가기), `LdrEnsureMrdataHeapExists` 가 쓰기까지 가는 조건 (본문 `.asm` 없음).
- test.exe 로는 속성 목록이 비어 `0x1ce268` 이 쓰이는 길을 동적으로 못 본다 (다른 부모가 `0xa0022` 속성을 주는 시험이 따로 필요).

## 도구 메모

- 결과서 "스크립트 문제" 없음. 이번 결과서는 점프 목적지를 처음부터 "목적지 − 시작" 으로 셌고, `.asm` 과 어긋난 건 두 줄 (CMP 를 다음 줄 오프셋에, `0x20b` 를 JNZ 줄에 붙임) 뿐이다 — 둘 다 "명령 하나 옆" 꼴 (run59 와 같은 종류).
- `_calls.tsv` 에 `alias:RtlCopyMemory+3` 으로 나오는 메모리 옮기기 (`0x166500`) 는 같은 주소 이름 4개 (`RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) — 결과서는 `memmove` 로 적음.

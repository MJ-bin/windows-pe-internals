# 55 func-table-mrdata — 정적 분석 결과

- 요청서: `_workspace/sast/55_func-table-mrdata.md` (Q111, PE_emulator 요청 5)
- 모듈 / 바이너리: ntdll (`ms/bin/…/System32/ntdll.dll`), 일부 ntoskrnl (`…/ntoskrnl.exe`, 질문 2 의 속성 풀이용 4개)
- PDB GUID+Age: ntdll `C3093720177DA6851519DCFFE8DF53FE1`, ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` (**기준과 일치**)
- 분석: 둘 다 이미 되어 있음 (`.gpr` + `_index.tsv` + `SetPdb:` 줄). **다시 analyze 안 했다.**
- 뽑은 것 (모두 새 `.asm`·`.c`):
  - ntdll, 요청서 대상 8개 depth 1 → 22개: `RtlInsertInvertedFunctionTable` · `RtlpInsertInvertedFunctionTableEntry` · `RtlRemoveInvertedFunctionTable` · `RtlxRemoveInvertedFunctionTable` · `RtlpRemoveInvertedFunctionTableEntry` · `LdrProtectMrdata` · `LdrControlFlowGuardEnforcedWithExportSuppression` · `RtlpxLookupFunctionTable` + 그 아래 (`LdrpChangeMrdataProtection`, `RtlImageDirectoryEntryToData`, `RtlpImageDirectoryEntryToData32`, `RtlAddressInSectionTable`, `RtlImageNtHeaderEx`, 락 함수들, `NtQueryVirtualMemory`·`NtSetInformationThread`·`NtAlertThreadByThreadIdEx` 스텁 등)
  - 일이 한 단계 아래에서 일어나서 보탬 (지도 `LdrProtectMrdata` helper 라 안 펼쳐져 있었음): `LdrpLocateMrdata` (depth 1 → `LdrpMakePermanentImageCommit`, `RtlSectionTableFromVirtualAddress`), `NtProtectVirtualMemory` (syscall 번호 보려고 depth 0)
  - ntoskrnl (질문 2, depth 0): `PspProcessUserCopyAttribute` · `PspCopyUserAttributes` · `PspGetFullAttribute` · `PspIsAttributeRemoved`
  - `_refs` 새로 (ntdll 9개): `LdrpInvertedFunctionTables`(0x3010) · `LdrpInvertedFunctionTableSRWLock` · `LdrpMrdataBase` · `LdrpMrdataSize` · `LdrpMrdataUnprotected` · `LdrpMrdataHeap` · `LdrpMrdataHeapUnprotected` · `LdrpMrdataLock` · `LdrpSuppressedExportOverrideListPtr`
  - 이미 있던 것은 다시 안 뽑음: `LdrpInitializeProcess`·`LdrpHandleUserCopyAttributeList`·`RtlLookupFunctionEntry`·`LdrpCaptureCriticalThunks`·`RtlpHpOptIntoSegmentHeap`·`PspBuildCreateProcessContext`
- 버전 확인 (`_calls.tsv` `ret_off`, 스킬 "알려진 사실"): **전부 일치**
  - ntoskrnl `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines`
  - ntdll `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse` (call `+0x1bb6`), `LdrpInitializeNode +0x16b/+0x19c`, `LdrInitializeThunk +0xe`, `LdrpInitialize +0x3c` 꼬리호출, `LdrpInitializeInternal +0x5a`, `_LdrpInitialize +0x16c`, `LdrpLoadDll +0x100`
  - **덤으로 정적 ↔ 사진 대조도 맞음** (아래 1(c)): `.mrdata` 크기 `0x35b8`, ntdll 칸 0 (예외 표 `+0x1d5000`·`0x108d8`·`SizeOfImage 0x267000`), `Epoch 6` 가 PE 파일 바이트와 run44 P1a 덤프에서 같이 나옴
- 스크립트 문제: 없음

## 읽는 법 (오프셋 약속)

- `call +0x..` = CALL 명령이 있는 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령이 시작하는 자리.
- **점프 목적지는 전부 (목적지 절대 주소 − 함수 시작 주소) 로 셌다** (주소 끝 세 자리를 오프셋으로 안 씀). 예: `LdrpInitializeProcess +0x494 JNZ 0x18008c8a8` → 목적지 `+0x4a4`.
- 함수 시작 주소: `LdrpInitializeProcess` `0x18008c404`, `RtlpxLookupFunctionTable` `0x180014840`, `LdrProtectMrdata` `0x1800a3ec0`, `LdrpChangeMrdataProtection` `0x1800a3f90`, `LdrpLocateMrdata` `0x1800a4000`, `RtlInsertInvertedFunctionTable` `0x1800a4074`, `RtlpInsertInvertedFunctionTableEntry` `0x1800a4110`, `RtlRemoveInvertedFunctionTable` `0x1800a7968`, `RtlxRemoveInvertedFunctionTable` `0x1800a79a0`, `RtlpRemoveInvertedFunctionTableEntry` `0x1800a87a8`, `LdrControlFlowGuardEnforcedWithExportSuppression` `0x1800a60c0`, `LdrpHandleUserCopyAttributeList` `0x18015ddb0`.
- 요청서 1(c) 에 옮겨 적힌 `LdrpInitializeProcess` 줄은 `.asm` 과 **전부 같다** (`+0x400`·`+0x415`·`+0x43a`·`+0x441`·`+0x44e`·`+0x45e`·`+0x467`·`+0x470`·`+0x47c`·`+0x481`·`+0x488`·`+0x48d`). 모두 명령 시작 자리고, call 의 ret 는 `+0x405 +0x41a +0x43f +0x446 +0x475 +0x481 +0x486 +0x48d +0x492`.
- 이름 표시: `[정적]` 코드·PE 파일 바이트에 있는 것, `[관찰]` run44 덤프 (`ms/logs/04_baggage-read_*.txt`), `[추측]` 코드에 없는 이름·목적.

---

## 한눈에

| 질문 | 한 줄 답 |
|---|---|
| 1(a) 칸 이름 | `LdrpInvertedFunctionTables` = PDB 타입 `_INVERTED_FUNCTION_TABLE_USER_MODE` (**0x3010 B** = 머리 0x10 + 칸 512 × 0x18). PE_emulator 의 "0x28B" 는 머리 + 칸 0 (0x10 + 0x18 = 0x28 바이트) 로 보인다 |
| 1(a) "쓴 자리 모름" 셋 | `LdrpMrdataUnprotected` = **예** (`LdrProtectMrdata +0x60 · +0xbb`). `LdrpMrdataBase`·`LdrpMrdataSize` = **직접은 아니오**, `LdrpLocateMrdata +0x59 · +0x52` 가 씀 (`LdrProtectMrdata` 가 `LdrpChangeMrdataProtection` 을 거쳐 처음 부를 때). 파일 값 `Unprotected` = **1** |
| 1(a) 인자·카운터 | `LdrProtectMrdata(ECX)`: 0 = 열기(쓰기 켬), 0 아님 = 닫기(쓰기 끔). **중첩 카운터** `LdrpMrdataUnprotected` (처음 1). 0 ↔ 1 이 바뀔 때만 syscall |
| 1(b) syscall | 칸 표 함수들은 직접 syscall 없음. 카운터가 바뀔 때 `LdrpChangeMrdataProtection +0x51` 이 `NtProtectVirtualMemory`(0x50) 를 부름. `RtlpxLookupFunctionTable` 은 `+0x2d0` 에서 `NtQueryVirtualMemory`(0x23) |
| 1(c) 왜 넣고 빼나 | 칸 0 에 ntdll 을 놓는 한 묶음: 넣기 → 읽기 → 칸 0 직접 쓰기 → 빼기. 순효과: 칸 0 = ntdll, `CurrentSize` 1, `Epoch` +4. "왜 이 길로" 는 `[추측]` |
| 1(d) CFG 반환 | 읽는 칸 셋 = `LdrSystemDllInitBlock +0xb8 · +0x9c · +0xa0`. test.exe 는 `+0xb8 = 0` 이라 **0 반환**. `LdrpSuppressedExportOverrideListPtr` 가 0 인 것과 맞다. PE_emulator `[추측]` 맞음 |
| 1(e) 룩업 갈래 | `+0x43a` 때는 `LdrInitState = 0` (≠3) → `+0x2b JNZ → +0x2a3` 로 가서 **표를 안 보고** `NtQueryVirtualMemory` + PE 헤더로 24B 를 만든다 |
| 2 `.data 0x1ce268` | 이름 없음. `LdrpThunkSignature` 의 일부 아님. **프로세스 속성 `0xa0022` 값 (0 또는 1) 을 담는 칸**, `RtlpHpOptIntoSegmentHeap +0x56` 이 읽음 (세그먼트 힙 쓰기 요청) |
| 2 `0xa0022` | 속성 번호 `0x22`, 플래그 `0x20000` (입력) + `0x80000` (커널 `PspProcessUserCopyAttribute` 가 복사하는 비트). 값 아래 두 비트는 0 또는 1 만 허용 |

---

## 질문별 답 [정적]

### 1(a). 무엇을 쓰나

**표 머리·칸 (PDB 타입 `_INVERTED_FUNCTION_TABLE_USER_MODE`, `pdb_struct.py` 확인)**

| 칸 | 이름 | 파일 처음 값 (PE 바이트) | 쓰는 곳 (정확) |
|---|---|---|---|
| `+0x0` dword | `CurrentSize` | **1** | `RtlpInsertInvertedFunctionTableEntry +0xb3` (INC, LOCK 없음) · `RtlpRemoveInvertedFunctionTableEntry +0x57` (`DEC` 한 값 저장) |
| `+0x4` dword | `MaximumSize` | **0x200** | 쓰는 곳 없음 (읽기만: `RtlpInsert…Entry +0x18`) |
| `+0x8` dword | `Epoch` | 0 | `INC.LOCK` — `RtlpInsert…Entry +0x34 · +0xb9`, `RtlpRemove…Entry +0x7 · +0x5d` |
| `+0xc` byte | `Overflow` | 0 | `RtlpInsert…Entry +0xdb` (1 로 켬, `CurrentSize == MaximumSize` 일 때), 읽기 `RtlpxLookupFunctionTable +0x296` |
| `+0x10` 칸 0 (0x18B) | `FunctionTable` `+0x0` · `ImageBase` `+0x8` · `SizeOfImage` `+0x10` (dword) · `SizeOfTable` `+0x14` (dword) | 전부 0 | **`LdrpInitializeProcess +0x44e · +0x45e · +0x467`** (직접) |
| `+0x28` 칸 1 ~ | 같은 모양, 오름차순 `ImageBase` | 전부 0 | `RtlpInsert…Entry +0x9f · +0xa4 · +0xa9 · +0xae` (칸 하나에 4줄) |

- 근거: `ms/disassemble/ntdll/_refs/LdrpInvertedFunctionTables.tsv`, `RtlpInsertInvertedFunctionTableEntry.asm`, `RtlpRemoveInvertedFunctionTableEntry.asm`, PE 파일 `.mrdata` (rva `0x1e7430`, 파일 오프셋 `0x1e0000 + 0x430`).
- **크기 0x3010**: PDB 공개 이름에는 크기가 없지만, 다음 이름 `LdrpGuardCheckIcallNoESFptr` (`0x1ea440`) 까지 거리 `0x1ea440 − 0x1e7430 = 0x3010` 이 타입 크기와 **딱 같다**. PE_emulator 의 "0x28B" 는 0x28 바이트 (머리 0x10 + 칸 0) 로 읽힌다 `[추측]` — 모양은 같고 길이만 다르다.
- 칸 0 을 **직접 읽는** 곳 15개 (`MOVAPS [0x1e7440]`, 이름만): `RtlLookupFunctionEntry +0x1d1` · `RtlpWalkFrameChain` · `RtlpLookupFunctionEntryForStackWalks` · `RtlLookupFunctionTable` · `RtlUnwindEx` · `RtlGuardCheckExceptionHandler` · `RtlPcToFileHeader` · `LdrpSnapModule` (2곳) · `EtwpProviderArrivalCallback` · `LdrpResolveProcedureAddress` · `RtlGuardCheckImageBase` · `LdrpHandleProtectedDelayload` · `RtlRestoreContext` · `RtlGuardCheckLongJumpTarget`. 예: `RtlLookupFunctionEntry +0x21 ~ +0x3a` 는 찾는 주소가 칸 0 의 `ImageBase` 이상이면 `+0x1bf` 로 가서 `ImageBase + SizeOfImage` 안인지 보고 맞으면 칸 0 을 그대로 복사하고 (`+0x1d1 ~ +0x1ea`), 아니면 `+0x48` 에서 `RtlpxLookupFunctionTable` 을 부른다.
- 커널도 이 표의 주소를 갖는다 `[관찰]`: `PsNtdllExports +0x68 KiUserInvertedFunctionTable = 7ffafb847430` = ntdll + `0x1e7430` (`ms/logs/04_baggage-read_supp_K1.txt` 172줄).

**`.mrdata` 열고 닫기 — 쓰는 전역 (baggage 3.9 "쓴 자리 모름" 셋)**

| 전역 | 이 함수 몫인가 | 줄 (`_refs`) |
|---|---|---|
| `LdrpMrdataUnprotected` (`0x1e7128`, dword) | **예** | 읽기 `LdrProtectMrdata +0x4f`, 쓰기 **`+0x60`** (닫을 때 `ADD EBX,-1` 한 값) · **`+0xbb`** (열 때 `INC` 한 값). **파일 처음 값 = 1** (PE 파일 바이트) |
| `LdrpMrdataSize` (`0x1e7018`) | **아니오 — 처음 쓰는 곳은 `LdrpLocateMrdata +0x52`** | `LdrProtectMrdata` 는 안 읽고 안 씀. 읽기는 `LdrpChangeMrdataProtection +0x2f` |
| `LdrpMrdataBase` (`0x1e7028`) | **아니오 — `LdrpLocateMrdata +0x59`** | 읽기는 `LdrpChangeMrdataProtection +0x8 · +0x19` |
| `LdrpMrdataLock` (`.data` `0x1d4de8`) | 잡고 풂 (쓰기 한 비트) | `LdrProtectMrdata +0x47 BTS.LOCK [락],0` 로 잡고 (경합이면 `+0xa8` `RtlpAcquireSRWLockExclusiveContended`), 풀 때 `+0x84` 꼬리호출 / `+0x99` 호출 `RtlReleaseSRWLockExclusive`. 값 0 으로 돌아옴 |
| `LdrpMrdataHeap` (`0x1e7010`) · `LdrpMrdataHeapUnprotected` (`0x1e7020`) | 아니오 (덤) | 쓰기는 `LdrEnsureMrdataHeapExists +0xef` · `+0xe8` 하나뿐 (`RtlAddGrowableFunctionTable +0x40` 이 부름 — 초기화 길 밖) |

- `LdrpLocateMrdata` (`ms/disassemble/ntdll/LdrpLocateMrdata.asm`, 110B) 가 하는 일: `+0x22 RtlImageNtHeaderEx(3, ntdll 시작, 0, &헤더)` → `+0x2c ~ +0x36 RtlSectionTableFromVirtualAddress(헤더, LdrSystemDllInitBlock 의 RVA = 0x1ea450)` → 그 구역 헤더에서 `VirtualAddress` (`+0xc`) 와 `VirtualSize` (`+0x8`) 를 읽어 → 시작 = ntdll 시작 + VirtualAddress, 크기 = VirtualSize → `+0x4d LdrpMakePermanentImageCommit(시작, 크기)` → **`+0x52 LdrpMrdataSize = 크기`, `+0x59 LdrpMrdataBase = 시작`**. 구역을 못 찾으면 (`+0x3e JZ → +0x67`) `INT 0x29` 코드 5.
  - 값 `[정적+관찰]`: 구역은 `.mrdata` (PE 구역표: VA `0x1e7000`, VirtualSize `0x35b8`). run44 P1a: `LdrpMrdataSize = 0x35b8`, `LdrpMrdataBase = 7ffafb847000` (ntdll 시작 `7ffafb660000` + `0x1e7000`) 로 같다.
  - `LdrpMakePermanentImageCommit` (`+0x0 ~ +0x37`): 시작·크기가 걸친 쪽(page)마다 8바이트를 읽어 **같은 자리에 다시 쓴다** (내용 안 바뀜). 이 구역은 4쪽. 목적은 이미지 쪽을 프로세스 개인 쪽으로 굳히는 것 `[추측]`.
- **처음 불리는 자리 (초기화 길)**: `LdrpMrdataBase == 0` 일 때만 `LdrpChangeMrdataProtection +0x14` 가 `LdrpLocateMrdata` 를 부른다 (`+0xf ~ +0x12`: 0 이 아니면 `JNZ → +0x19` 로 건너뜀). 그리고 `LdrProtectMrdata` 의 `NtProtectVirtualMemory` 는 카운터가 0 ↔ 1 일 때만 (아래). 초기화에서 이 줄에 **처음** 닿는 자리는 `LdrpInitializeProcess +0x488` (아래 1(c) 카운터 표). 지도 21곳 중 이 앞에 불리는 호출처는 없다 (`LdrpInitializeProcess +0x415` 이전 호출 목록과 `_LdrpInitialize` 의 `LdrInitializeMrdata` 는 `LdrProtectMrdata` 를 안 부름).
  - baggage 분류: `LdrpMrdataUnprotected` 는 "끝까지 0*" 이었지만 **`≤P1a`** 가 맞다 (파일 값 1 → `+0x488` 에서 0). `LdrpMrdataBase`·`Size` 는 `≤P1a` 그대로 (쓴 자리 이제 앎).

**인자 꼴과 카운터 — `LdrProtectMrdata(ECX)` (`ms/disassemble/ntdll/LdrProtectMrdata.asm`, 195B)**

- `+0x1f MOV EDI,ECX`. 락(`LdrpMrdataLock`)을 잡은 뒤 `+0x4f` 에서 카운터를 읽는다.
- **ECX = 0 → 열기 (쓰기 켬)**, `+0x57 JZ → +0x8d`:
  - 카운터 0 이면 `+0x8f JZ → +0xaf` → `ECX = 4` (`PAGE_READWRITE`) 로 `+0xb4 call` (ret `+0xb9`) `LdrpChangeMrdataProtection`, 이어서 `+0xb9 INC` · `+0xbb` 저장.
  - 카운터가 −1 도 0 도 아니면 (`+0x94 JNZ → +0xb9`) syscall 없이 `INC` · 저장 (**중첩**).
  - 카운터 = −1 이면 `+0x96` → 락 풀고 `+0x9e ~ +0xa3` `INT 0x29` 코드 `0xe`.
- **ECX ≠ 0 → 닫기 (쓰기 끔)**:
  - 카운터가 0 이면 (`+0x5b JZ → +0x96`) 위와 같은 `INT 0x29` 코드 `0xe` (닫을 게 없다).
  - 아니면 `+0x5d ADD EBX,-1` · `+0x60` 저장. 값이 0 이 되면 (`+0x66 JNZ → +0x72` 를 안 탐) `ECX = 2` (`PAGE_READONLY`) 로 `+0x6d call` (ret `+0x72`) `LdrpChangeMrdataProtection`. 0 이 아니면 syscall 없이 끝.
- 끝: `+0x84 JMP RtlReleaseSRWLockExclusive` (꼬리호출). 반환값 의미 없음 (`void`처럼 쓰임).
- 다른 갈래 이름만: `+0xf ~ +0x47` 은 TEB `+0x1850` (`SchedulerSharedDataSlot`, PDB 확인) 가 가리키는 8칸 표의 첫 빈 칸에 락 주소를 적는 것으로 보인다 `[추측]`. 이 칸은 `LdrpInitializeProcess +0x182c` 쪽에서 걸리므로 (run45 일 60) `+0x415 ~ +0x488` 때는 비어 있어 건너뛰는 것 같다 `[추측]`.
- 코드 `0xe`·`5` 의 이름은 SDK 의 `FAST_FAIL_INVALID_REFERENCE_COUNT`·`FAST_FAIL_INVALID_ARG` 로 기억 `[추측]`; 코드 번호는 `[정적]`.

---

### 1(b). syscall 을 부르나

| 함수 | 직접 syscall | 하위 호출 (call → ret) |
|---|---|---|
| `LdrProtectMrdata` | 없음 | `+0x6d → +0x72` · `+0xb4 → +0xb9` `LdrpChangeMrdataProtection`, `+0xa8 → +0xad` `RtlpAcquireSRWLockExclusiveContended`, `+0x99 → +0x9e` · `+0x84` 꼬리 `RtlReleaseSRWLockExclusive` |
| `LdrpChangeMrdataProtection` (`0xa3f90`, 102B) | **`+0x51 → +0x56 NtProtectVirtualMemory` (syscall `0x50`)** | `+0x14 → +0x19 LdrpLocateMrdata` (`LdrpMrdataBase == 0` 일 때만) |
| `RtlInsertInvertedFunctionTable` | 없음 | `+0x29 → +0x2e RtlImageDirectoryEntryToData`, `+0x44 → +0x49 RtlAcquireSRWLockExclusive`, `+0x4b → +0x50 LdrProtectMrdata(0)`, `+0x5d → +0x62 RtlpInsertInvertedFunctionTableEntry`, `+0x67 → +0x6c LdrProtectMrdata(1)`, `+0x87` 꼬리 `RtlReleaseSRWLockExclusive` |
| `RtlpInsertInvertedFunctionTableEntry` | 없음 | `+0x90 → +0x95 memmove` (`CurrentSize` 보다 앞에 끼울 때만) |
| `RtlRemoveInvertedFunctionTable` | 없음 | `+0x10 → +0x15 RtlAcquireSRWLockExclusive`, `+0x18 → +0x1d RtlxRemoveInvertedFunctionTable`, `+0x29` 꼬리 `RtlReleaseSRWLockExclusive` |
| `RtlxRemoveInvertedFunctionTable` | 없음 | `+0x37 → +0x3c LdrProtectMrdata(0)`, `+0x3e → +0x43 RtlpRemoveInvertedFunctionTableEntry`, `+0x48 → +0x4d LdrProtectMrdata(1)` — **요청서가 물은 `Remove → Rtlx → Rtlp…Entry` 사슬 맞다** |
| `RtlpRemoveInvertedFunctionTableEntry` | 없음 | `+0x4a → +0x4f memmove` (`CurrentSize == 2` 가 아닐 때만, `+0x17 JZ → +0x55`) |
| `LdrControlFlowGuardEnforcedWithExportSuppression` | 없음 | 없음 (잎 함수) |
| `RtlpxLookupFunctionTable` | **`+0x2d0 → +0x2d5 NtQueryVirtualMemory` (`0x23`, 클래스 6)** · `+0x406 → +0x40b NtAlertThreadByThreadIdEx` (`0x72`) · `+0x508 → +0x50d NtSetInformationThread` (`0xd`, 클래스 `0x38`) | 아래 1(e) |

- `NtProtectVirtualMemory` 인자 (`LdrpChangeMrdataProtection.asm`): 프로세스 = −1 (`+0x36 MOV RCX,-1`), `*BaseAddress` = `LdrpMrdataBase` 사본 (`[RSP+0x50]`, `RDX = &사본`), `*RegionSize` = `LdrpMrdataSize` 사본 (`[RSP+0x48]`, `R8 = &사본`), 새 보호 = 입구에서 저장한 ECX (`+0x3d R9D = [RSP+0x40]`) = **4 (`PAGE_READWRITE`)** 또는 **2 (`PAGE_READONLY`)**, 옛 보호 받는 곳 = 같은 `[RSP+0x40]` (`+0x4c LEA RAX,[RSP+0x40]` → `[RSP+0x20]`). 크기 `0x35b8` 은 쪽 단위로 올림 (4쪽, `0x1e7000 ~ 0x1eafff`). 실패하면 (`+0x58 JNS → +0x61` 못 탐) `+0x5a ~ +0x5f INT 0x29` 코드 5.
- syscall 번호 (`NtProtectVirtualMemory.asm` `+0x3 MOV EAX,0x50`, `NtQueryVirtualMemory.asm` `0x23`, `NtSetInformationThread.asm` `0xd`, `NtAlertThreadByThreadIdEx.asm` `0x72`).
- 줄기 끝까지: `RtlImageDirectoryEntryToData` → `RtlImageNtHeaderEx` · `RtlAddressInSectionTable` 에서 끝 (syscall 없음, `.asm` 에 SYSCALL 0개). `RtlpAcquireSRWLockExclusiveContended` 는 대기 길 (`NtWaitForAlertByThreadId` 등, 락 부품 — 이름만).

---

### 1(c). `LdrpInitializeProcess +0x3e6 ~ +0x4a1` 구간

**입력**: `RSI` = `+0x2d MOV RSI,RDX` = 2번째 인자 = 커널이 넘긴 **ntdll 시작 주소** (run18: `+0x684` 에서 `LdrpSystemDllBase` 에 저장). 이 구간 전체가 **ntdll 자기 이야기**다 (exe 가 아님).

- `+0x3d5 TEST DL,DL` (`DL` = `+0x372` 에서 읽은 `LdrpIsSecureProcess`): 0 이 아니면 `+0x3d7 JNZ → +0x3e6` 로 건너뜀, 0 이면 `+0x3d9 → +0x3de LdrpHandleUserCopyAttributeList` (질문 2). `+0x3e0 JS → +0xa7b` (실패 끝).
- `+0x3e6 ~ +0x40d`: `[RSP+0x170] = R14 (=0)` 으로 비우고 `+0x400 → +0x405 RtlImageNtHeaderEx(3, RSI, 0, &헤더)`. `+0x40d MOV EBX,[헤더+0x50]` = `OptionalHeader.SizeOfImage` (ntdll 은 `0x267000`).

| 줄 | 하는 일 | 카운터 `Unprotected` | `CurrentSize` | `Epoch` | 칸 0 | 칸 1 | syscall |
|---|---|---|---|---|---|---|---|
| 시작 (파일 값) | — | **1** | 1 | 0 | 0 | 0 | — |
| `+0x415 → +0x41a` `RtlInsertInvertedFunctionTable(RSI, EBX)` | 안에서 열기 (`+0x4b`) 1→2, 끼우기, 닫기 (`+0x67`) 2→1. 칸 1 에 ntdll 을 적음 (`CurrentSize` 가 1 이라 `RtlpInsert…Entry +0x49 JZ → +0x95` 로 바로 칸 1) | 1 | **2** | **2** | 0 | ntdll | 없음 (카운터 1↔2, 0 안 지남) |
| `+0x41a ~ +0x437` | 지역 24B (`[RSP+0x1d0 ~ +0x1e7]`) 0 으로 비움 | | | | | | |
| `+0x43a → +0x43f` `RtlpxLookupFunctionTable(RSI, &지역24B)` | 표 안 보고 **`NtQueryVirtualMemory` + PE 헤더로 지역 24B 를 채움** (1(e)). 반환 RAX 는 안 씀 | 1 | 2 | 2 | 0 | ntdll | `NtQueryVirtualMemory` `0x23` |
| `+0x441 → +0x446` `LdrProtectMrdata(0)` (`+0x43f XOR ECX,ECX`) | **열기** 1→2 | 2 | 2 | 2 | | | 없음 |
| `+0x44e MOVAPS [0x1e7440]` | 칸 0 `+0x0 ~ +0xf` ← 지역 `FunctionTable`·`ImageBase` | | | | 반쯤 | | |
| `+0x45e MOVSD [0x1e7450]` | 칸 0 `+0x10 ~ +0x17` ← 지역 `SizeOfImage`·`SizeOfTable` | | | | | | |
| `+0x467 MOV dword [0x1e7450],EBX` | 칸 0 `SizeOfImage` ← **헤더의 `SizeOfImage`** (`+0x40d` 값) 로 덮어씀 | | | | **ntdll** | | |
| `+0x470 → +0x475` `RtlRemoveInvertedFunctionTable(RSI)` | 안에서 열기 (`+0x37`) 2→3, 칸 1 을 뺌 (`CurrentSize` 2→1, 칸 1 바이트는 **지우지 않음**), 닫기 (`+0x48`) 3→2 | 2 | **1** | **4** | ntdll | ntdll (남음) | 없음 |
| `+0x47c → +0x481` `LdrProtectMrdata(1)` (`+0x475 MOV EBX,1`, `+0x47a MOV ECX,EBX`) | **닫기** 2→1 | 1 | 1 | 4 | | | 없음 |
| `+0x481 → +0x486` `LdrpCaptureCriticalThunks` | 이름만 (`.mrdata` 안 건드림: `LdrpThunkSignature`·`LdrpDetourExist` 는 `.data`; `+0x67` 에 `NtQueryVirtualMemory` 클래스 4) | 1 | | | | | (`+0x67`) |
| `+0x488 → +0x48d` `LdrProtectMrdata(1)` (`+0x486 MOV ECX,EBX`) | **닫기** 1→0 → `LdrpChangeMrdataProtection(2)` → `LdrpMrdataBase == 0` 이라 `+0x14` `LdrpLocateMrdata` (**`LdrpMrdataSize`·`Base` 처음 씀**) → `NtProtectVirtualMemory(PAGE_READONLY)` | **0** | 1 | 4 | ntdll | ntdll | **`NtProtectVirtualMemory` `0x50`** (이 구간에서 유일한 쓰기 끔) |
| `+0x48d → +0x492` CFG 확인 | 1(d) | | | | | | |

- 근거 줄: `LdrpInitializeProcess.asm` `+0x3e6 ~ +0x496` (위 오프셋 전부 그 파일), `RtlInsertInvertedFunctionTable.asm`, `RtlxRemoveInvertedFunctionTable.asm`, `RtlpInsert…Entry.asm`, `RtlpRemove…Entry.asm`, `LdrProtectMrdata.asm`.
- **카운터가 1 에서 시작해야 맞아떨어진다**: 이 구간의 열기는 셋 (`Insert +0x4b`, `+0x441`, `Rtlx Remove +0x37`), 닫기는 넷 (`Insert +0x67`, `Rtlx Remove +0x48`, `+0x47c`, `+0x488`) 이라 순 −1 이다. 파일 값 1 에서 시작해야 `+0x488` 에서 0 이 되고, 닫기가 카운터 0 에서 `INT 0x29` 로 죽지 않는다. PE 파일 바이트를 읽어 **`LdrpMrdataUnprotected` 파일 값 = 1** 로 확인했다 (`.mrdata` 파일 오프셋 `0x1e0128`). 구역이 처음부터 읽기·쓰기 (`.mrdata` 특성 `0xc0000040`) 로 매핑되어 있고, **ntdll 초기화가 처음 닫는 자리가 `+0x488`** 이다.
- **왜 Insert 뒤 Remove 인가**: 코드가 보여주는 사실은 다음과 같다.
  1. 칸 0 은 **정렬 구간 밖**이다. `RtlpxLookupFunctionTable` 의 이진 검색은 칸 1 ~ `CurrentSize − 1` 만 본다 (`+0x99 MOV R8D,1`, `+0xa6 DEC R9D`), 삽입도 칸 1 부터 비교한다 (`RtlpInsert…Entry +0x4d` 가 칸 1 의 `ImageBase` 부터). 위 15곳은 칸 0 을 따로 직접 본다.
  2. 그래서 ntdll 을 칸 0 에 놓고 정렬 구간에서는 빼야 한다. `+0x415` Insert 가 칸 1 에 놓고 `Epoch` 를 올리며, `+0x44e ~ +0x467` 이 칸 0 에 복사하고, `+0x470` Remove 가 칸 1 을 정렬 구간에서 뺀다 (`CurrentSize` 2 → 1).
  3. 순효과: `Epoch` +4, 칸 0 = ntdll, `CurrentSize = 1`, 칸 1 바이트는 ntdll 복사본이 남음 (exe 가 `LdrpProcessMappedModule +0x177` 로 칸 1 에 들어올 때 덮임).
  4. 왜 칸 0 에 바로 안 쓰고 Insert 를 거치는지, 왜 Lookup 으로 되읽는지는 코드로는 못 정한다 `[추측]` (칸 만들기를 한 곳에 모으려는 것으로 보인다). run35 Q67(a) 의 답은 **`+0x415` 와 `+0x44e ~ +0x467` 둘 다**: `+0x415 Insert` 가 ntdll 예외 표를 올리고, `+0x44e` 가 칸 0 에 놓는다 (그래서 `LdrpProcessMappedModule +0x15e` 의 `InExceptionTable` 비트 9 가 켜진 ntdll 은 `+0x177` Insert 를 건너뜀).
- **칸 0 에 놓이는 값의 출처**: `+0x44e` · `+0x45e` 는 `+0x43a` 가 채운 지역 24B (`RtlpxLookupFunctionTable` 결과), `+0x467` 만 헤더 `SizeOfImage`. 결과 (아래 1(e)) 에서 `FunctionTable = ntdll 시작 + 예외 디렉터리 RVA`, `ImageBase = NtQueryVirtualMemory 가 준 이미지 시작`, `SizeOfTable = 예외 디렉터리 크기`.
- **끝난 뒤 정적 기대 값 (test.exe, `+0x475` 직후)** — 값은 ntdll 파일에서 읽음 (예외 디렉터리 RVA `0x1d5000`, 크기 `0x108d8`, `SizeOfImage 0x267000`), 시작 주소는 ASLR 마다 다름:
  - 머리: `CurrentSize 1`, `MaximumSize 0x200`, `Epoch 4`, `Overflow 0`
  - 칸 0: `FunctionTable = ntdll시작 + 0x1d5000`, `ImageBase = ntdll시작`, `SizeOfImage 0x267000`, `SizeOfTable 0x108d8`
  - 칸 1: 칸 0 과 같은 복사본 (아직 안 지워짐)
- **[관찰] 대조 (run44 P1a, `ms/logs/04_baggage-read_P1a.txt` 의 `17b NTDLL_MRDATA`)**: `CurrentSize 2`, `MaximumSize 0x200`, `Epoch 6`, `Overflow 0`; 칸 0 = `{7ffafb835000, 7ffafb660000, 0x267000, 0x108d8}` (= ntdll 시작 `7ffafb660000` + `0x1d5000`), 칸 1 = `{7ff61a3f5000, 7ff61a370000, 0x8c000, 0x4fbc}` (test.exe). `Epoch 6 = 4 (ntdll 넣고 뺌) + 2 (exe 넣음)`, `CurrentSize 2 = 1 + exe` 라서 **정적 셈과 맞다**. `LdrpMrdataSize 0x35b8`·`LdrpMrdataBase 7ffafb847000`·`LdrpMrdataUnprotected 0` 도 맞다. P1b 는 `CurrentSize 6`, `Epoch 0xe` (+8 = 4곳 더 넣음).
- 세 `LdrProtectMrdata` 인자: `+0x441` **ECX = 0** (열기), `+0x47c` **ECX = 1** (닫기), `+0x488` **ECX = 1** (닫기). 순서 = **열기 → 닫기 → (LdrpCaptureCriticalThunks) → 닫기** (파일 값 1 이 첫 "열기" 역할).

---

### 1(d). test.exe 에서 `LdrControlFlowGuardEnforcedWithExportSuppression` 반환값

- 읽는 것은 **`LdrSystemDllInitBlock` (rva `0x1ea450`) 세 칸뿐** (PEB·이미지 헤더 안 읽음; 모두 `ms/disassemble/ntdll/LdrControlFlowGuardEnforcedWithExportSuppression.asm`):
  1. `+0x0 CMP [LdrSystemDllInitBlock +0xb8],0` → 0 이면 `+0x8 JZ → +0x2b` → **반환 0**.
  2. `+0xa TEST byte [+0x9c],1` → 1 이면 `+0x11 JNZ → +0x2b` → 반환 0.
  3. `+0x13 MOV RCX,[+0xa0]` · `+0x1c SHR RCX,0x28` · `+0x20 AND CL,3` · `+0x23 CMP CL,3` · `+0x26 SETZ AL` → **`[+0xa0]` 의 비트 40~41 이 3 이면 1**, 아니면 0.
  - 셋 다 통과해야 1: `+0xb8 ≠ 0` **그리고** `+0x9c` 비트 0 = 0 **그리고** `+0xa0 >> 40 & 3 == 3`.
- 칸 이름은 PDB 타입이 없다 (두 PDB 다). 공개 자료(phnt)의 `PS_SYSTEM_DLL_INIT_BLOCK` 이름을 빌리면 `+0x9c` = `Flags` 비트 0 (`CfgOverride`), `+0xa0` = 완화 옵션 첫 칸 (비트 40~41 = CFG 옵션: 3 = 내보내기 억제), `+0xb8` = `CfgBitMap` 포인터 `[추측]`. 이미 있던 기록 (baggage `+0xb8` CFG 비트맵 주소, run59 `+0x9c` 쓰는 줄 `PspPrepareSystemDllInitBlock +0x100 · +0x106`) 과 같다.
- **test.exe: `+0xb8 = 0` 이라 첫 검사에서 0 반환.** `[관찰]` run44 K1·P1a·P1b·P2 모두 `LdrSystemDllInitBlock` `+0xa0 = 0`, `+0xa8 = 0`, `+0xb0 = 0x02002002`, **`+0xb8 = 0`**, `+0x9c = 0` (`+0x98` 난수 `0x8dff4d73`). `[소스]` 이유로 짐작되는 것: `sample/build.bat` 은 `/guard:cf` 가 없고 `test.exe` 의 `DllCharacteristics = 0x8160` (GUARD_CF `0x4000` 비트 없음) → 커널이 CFG 비트맵을 안 만든다 `[추측]`.
- 반환 뒤 갈래 (`LdrpInitializeProcess`): `+0x492 CMP EAX,EBX` (`EBX = 1`) · `+0x494 JNZ → +0x4a4`. **반환 1 이면** `+0x496` 에서 `LdrpSuppressedExportOverrideList` (rva `0x1740f0`, `.rdata`) 의 주소를 `+0x49d` 로 `.data` 전역 `LdrpSuppressedExportOverrideListPtr` (rva `0x1d2920`) 에 저장하고 `+0x4a4` 로 계속. 0 이면 이 두 줄을 건너뛰어 `+0x4a4` 로 바로 간다 (`+0x4a4 MOVAPS XMM1,[LdrSystemDllInitBlock+0xa0]`).
- 이 전역을 쓰는 곳은 `+0x49d` **하나뿐**이고 (`_refs/LdrpSuppressedExportOverrideListPtr.tsv`, 직접 읽는 참조는 없음 — 읽는 쪽은 `[공백]`), 값이 파일 처음 값 0 (`.data`, 파일에 없는 부분) 이다. run44 "끝까지 0" 사진 + 위 `+0xb8 = 0` + 위 갈래 = **PE_emulator `[추측]` ("1 이 아님") 이 맞다.**
- 같은 함수를 부르는 곳 11 (이름만, 지도): `AvrfMiniLoadDll +0x7fc` · `LdrpCfgCheckRoutineCallback +0x7` · `LdrpCfgDispatchRoutineCallback +0x7` · `LdrpDoPostSnapWork +0x68` · `LdrpInitializeProcess +0x48d` · `LdrResolveDelayLoadedAPI +0x19f` · `RtlUnwindEx +0x5c4` · `RtlValidateUserCallTarget +0x5b` 등.

---

### 1(e). `RtlpxLookupFunctionTable` — `+0x43a` 가 타는 갈래

- 인자: `RCX` = 찾을 주소, `RDX` = 결과 24B (`+0x0 FunctionTable` · `+0x8 ImageBase` · `+0x10 SizeOfImage` dword · `+0x14 SizeOfTable` dword, `_INVERTED_FUNCTION_TABLE_ENTRY` 모양). 반환 `RAX` = `FunctionTable` 또는 0. 입구에서 `[RDX+0x8]`·`[RDX+0x10]` 을 0 으로 비운다 (`+0x23 · +0x27`).
- **갈림길**: `+0x16 CMP [LdrInitState],3` 뒤 `+0x2b JNZ → +0x2a3`. `LdrInitState` 는 이 시점에 **0** `[정적]`: `_LdrpInitialize +0x101` 가 0 으로 쓰고 (`EBX = 0`), `LdrpInitializeProcess` 가 1 (`+0x1a4c`) · 2 (`+0x1b1e`) · 3 (`+0x17dd`) 로 쓰는 줄은 모두 `+0x43a` 보다 뒤 (`_refs/LdrInitState.tsv`). → 이 호출은 **3 이 아니라서 느린 길 `+0x2a3`**.
- 느린 길 (`+0x2a3 ~ +0x3d5`, `+0x165` 로 끝):
  1. `+0x2d0 → +0x2d5` `NtQueryVirtualMemory(−1, 찾을 주소, 클래스 6, 버퍼 [RSP+0x30], 0x18, NULL)` — 클래스 6 = 이미지 정보 (공개 자료 이름 `MemoryImageInformation` `[추측]`; 번호·길이 0x18 은 `[정적]`). 버퍼: 이미지 시작 · 이미지 크기 · 플래그.
  2. 실패 (`+0x2d7 JS`), 이미지 시작 0 (`+0x2e5 JZ`), 플래그 비트 1 (`+0x2f2 JNZ`) 또는 비트 0 (`+0x2fa JNZ`) 켜짐, 찾는 주소 < 이미지 시작 (`+0x303 JC`) 이면 전부 `+0x487` (`[RDX+8] = 0`) 로 가서 0 반환.
  3. 성공: `+0x309 · +0x310` 결과 `SizeOfImage`·`ImageBase` 저장. `+0x35b → +0x360 RtlImageNtHeaderEx(1, 이미지 시작, 0, &헤더)`.
  4. PE32+ (`+0x389` 이 `0x20b`): `+0x38f` 디렉터리 개수 > 3, `+0x39c EDX = [헤더+0xa0]` (예외 디렉터리 RVA), `+0x3aa ~ +0x3b0` 크기를 `[RDX+0x14]` 에 저장. 매핑된 이미지로 보면 (`+0x3b3 TEST SIL,SIL` 이 0 이 아님; 시작 주소 하위 2비트가 0 이면 그렇다) **`+0x3bc FunctionTable = RVA + 이미지 시작`**, `+0x3d2 [RDX] = FunctionTable`.
  - 안 타는 길: PE32 (`+0x37b JZ → +0x446` 는 `RtlpImageDirectoryEntryToData32` call `+0x469`), 매핑 아님 (`+0x3b6 JZ → +0x49a` 는 `RtlAddressInSectionTable` call `+0x4a9`), 실패 상태값 `0xc000000d` (`+0x47d`)·`0xc0000002` (`+0x490`).
- 빠른 길 (`LdrInitState == 3`, 이 호출에선 안 탐, 이름만): `+0x31 ~ +0x72` TEB `+0x1850` 표에 락 주소 적기, `+0x7d CMPXCHG.LOCK` 로 `LdrpInvertedFunctionTableSRWLock` 공유로 잡기 (실패 시 `+0x185 → +0x190 RtlpAcquireSRWLockSharedContended`), `+0x8c ~ +0x101` 이진 검색 (칸 1 ~ `CurrentSize − 1`, `ImageBase` 와 `ImageBase + SizeOfImage` 비교), 찾으면 `+0xec ~ +0xfe` 24B 복사, 락 풀기 `+0x104` (대기자가 있으면 `+0x195 ~ +0x41b` 쪽에서 `RtlRaiseStatus 0xc0000264` · `NtAlertThreadByThreadIdEx` · `NtSetInformationThread` 클래스 `0x38`). 못 찾았고 `Overflow` 가 켜져 있으면 (`+0x296 CMP [Overflow]`, `+0x29d JZ → +0x165` 못 탐) 같은 `+0x2a3` 느린 길로 넘어간다.
- **다른 caller (이름만, 지도)**: `RtlLookupFunctionEntry call +0x48` · `RtlPcToFileHeader +0x29` · `RtlUnwindEx +0x4c5` · `RtlGuardCheckImageBase +0x46` · `LdrpSnapModule +0x82a · +0xab5` · `LdrpResolveProcedureAddress +0x356` · `LdrpHandleProtectedDelayload +0x1dd` · `EtwpProviderArrivalCallback +0x52` (지도 `callers_total` 14 중 지도에 보이는 함수는 이 8개 + `LdrpInitializeProcess +0x43a`). 이들은 앞에서 칸 0 을 보거나 `LdrInitState == 3` 라서 표를 쓴다.
- 이 호출이 끝나면 지역 24B 는 (test.exe, ntdll): `FunctionTable = ntdll시작 + 0x1d5000`, `ImageBase = ntdll시작`, `SizeOfImage = NtQuery 가 준 값 (0x267000)`, `SizeOfTable = 0x108d8` — 위 P1a 칸 0 과 같다 `[관찰]`.

---

### 2. 작은 덤 (ⓢ)

**2-1. `LdrpHandleUserCopyAttributeList +0x82` 가 쓰는 `.data` rva `0x1ce268`**

- 하는 일 (`ms/disassemble/ntdll/LdrpHandleUserCopyAttributeList.asm`, 372B): `PEB.ProcessParameters` (`+0x15 [PEB+0x20]`) 의 **`+0x448` 속성 목록**을 걷는다. 목록 = 머리 8B (총 길이) + 항목 `0x20` B 씩 (`+0x0` 속성값, `+0x8` 크기, `+0x10` 값 포인터, `+0x18` 반환 길이) — 항목 수 = `(총길이 − 8) >> 5` (`+0x2f ~ +0x33`).
  - 목록이 비었거나 길이 0 이면 (`+0x23 JZ`, `+0x3a JZ` → 둘 다 `+0x15b`) 아무것도 안 하고 `+0x15b` 에서 `+0x448` 을 0 으로 지우고 반환 (성공).
  - 항목마다 속성값이 **`0xa0022`** 인지 (`+0x47 CMP`, 아니면 `+0x4d JNZ → +0xff` 로 로그 `0x1232` 후 `0xc000000d`), 값 포인터가 0 이 아닌지 (`+0x5a JZ → +0xd6`, 로그 `0x1215`), 값 8바이트의 **아래 두 비트가 0 또는 1 이고** (`+0x5f AND AL,3`, `+0x61 CMP AL,1`, `+0x63 JA → +0x9a`) **그 둘 말고 다른 비트가 없는지** (`+0x71 ~ +0x7e`, 값 & 3 == 값) 확인. 어기면 `+0x9a` 에서 로그 (`0x1224`) 후 `STATUS_INVALID_PARAMETER` (`0xc000000d`).
  - 통과하면 **`+0x82 MOV qword [0x1ce268],R9` — 값 8바이트 (0 또는 1) 를 저장**. `+0x8c ADD RDX,0x20`, `+0x93 JC → +0x44` 로 다음 항목.
- **그 칸의 이름·뜻**:
  - PDB 공개 이름 **없음**.
  - 읽는 곳은 하나뿐: **`RtlpHpOptIntoSegmentHeap +0x56`** (`MOV AL,byte [0x1ce268]`, `+0x5c AND AL,3`, `+0x5e CMP AL,BL(=1)`, 같으면 `+0x62` 에서 `*출력 = 1` 로 "세그먼트 힙 쓰기" 결과 1 반환). 이 함수는 `RtlInitializeHeapManager +0x28` 이 부른다 (지도 6단계). 앞에서 `RtlpLowFragHeapGlobalFlags` 비트 `0x8`/`0x10` 을 먼저 본다 (`+0x28 ~ +0x51`).
  - 그래서 이 칸은 **"프로세스 만들기 속성으로 받은 세그먼트 힙 쓰기 요청 (0 또는 1)"** 이다. 이름은 우리가 붙인 것 `[추측]` (예: `UserCopySegmentHeapRequest`).
- **`LdrpThunkSignature` 안인가: 아니오.** `LdrpThunkSignature` = rva `0x1cdfa0`, 다음 공개 이름 `RtlpCaptureActivationContextActivationStacks` = `0x1ce39c` 라서 "다음 이름까지 거리"는 `0x3fc` 이고 `0x1ce268` 은 그 안이다 (시작에서 `+0x2c8`, 다음 이름 앞 `0x134`). 하지만 `LdrpThunkSignature` 를 쓰는 `LdrpCaptureCriticalThunks +0x95` 는 16B 씩 최대 5번 (`+0x7a CMP EBX,5`) = **0x50 B** 만 쓴다 (`+0x7c ~ +0x9d`). `0x1ce268` 은 그보다 `0x278` 뒤라서 **별개 이름 없는 변수**다.
- 읽는 함수: `RtlpHpOptIntoSegmentHeap` 하나 (`_refs/0x1ce268.tsv` 2줄: 쓰기 1 · 읽기 1).
- test.exe: 매개변수 `+0x448 = 0` (run44 `[관찰]` `+0x430 ~ +0x448` 0) 이라 `+0x23 JZ → +0x15b` — **목록 걷기를 안 타고 `0x1ce268` 은 처음 값 0 그대로**. 호출 조건도 `LdrpIsSecureProcess == 0` 일 때만 (`LdrpInitializeProcess +0x3d5 ~ +0x3d9`).

**2-2. 속성 번호 `0xa0022`**

- 값을 풀면 (공개 `PS_ATTRIBUTE_*` 모양): 번호 = `0xa0022 & 0xffff = 0x22` (10진 34). 플래그: `0x10000` (스레드) **아님**, `0x20000` (입력) **있음**, `0x40000` (더하기 `ADDITIVE`) **아님**, 그리고 `0x80000` **있음**.
- 근거: 커널 `PspGetFullAttribute(번호)` (rva `0x5e2430`) 가 `.rdata` rva `0x2b750` 의 8바이트 표에서 `표[번호]` 를 돌려준다. 표를 PE 에서 읽으면 `[0x22] = 0xa0022`, 번호 0 ~ 0x24 중 `0x80000` 비트를 가진 것은 **이것 하나뿐**이다 (`0x60000`·`0x10003`·`0x20005` 같은 앞쪽 항목들은 공개 자료의 `ADDITIVE`/`INPUT`/`THREAD` 조합과 모양이 맞다).
- `0x80000` 비트 뜻: 커널 `PspBuildCreateProcessContext +0x1584 TEST [RSP+0x138],0x80000` (`[RSP+0x138]` = 항목의 속성값 `+0x1b9`) 가 켜진 항목에만 `+0x159f` (ret `+0x15a4`) `PspProcessUserCopyAttribute` 를 부르고, 그 전에 `+0x1577 → +0x157c Feature_CreateProcessSegmentHeapEnablement__private_IsEnabledDeviceUsageNoInline` 을 먼저 본다 (꺼져 있으면 `+0x157e JZ → +0x17db` 로 건너뜀). 복사된 항목은 `PspCopyUserAttributes` (`PspCopyAndFixupParameters +0x3eb`) 가 새 프로세스 안 목록으로 써 주고, ntdll `LdrpHandleUserCopyAttributeList` 가 그것을 읽는다. 즉 **`0x80000` = "유저 복사" 비트 (새 프로세스의 ntdll 이 읽도록 복사하는 속성)** 로 보이고 (함수 이름 근거), 번호 `0x22` 는 **세그먼트 힙 쓰기 요청**이다 (기능 이름 `CreateProcessSegmentHeapEnablement` + 읽는 쪽 `RtlpHpOptIntoSegmentHeap`).
  - 확인 안 한 것: `PspCopyUserAttributes` 의 쓴 목록이 `ProcessParameters +0x448` 으로 연결되는 줄 (`PspCopyAndFixupParameters` 쪽) — 이번엔 안 봄 `[공백]`. 속성 번호 `0x22` 의 공개 이름도 PDB 에 없다 `[추측]`.
- **값 아래 두 비트**: 허용값은 0 또는 1 (둘째 비트가 켜지거나 다른 비트가 있으면 `0xc000000d`). 읽는 쪽은 `& 3 == 1` 일 때만 "쓰기" (`RtlpHpOptIntoSegmentHeap +0x5c ~ +0x62`). 0 이면 앞뒤의 다른 판단(레지스트리·IFEO 쪽)으로 넘어간다. `& 3` 로 보는데 허용값이 0·1 뿐이라 비트 1 의 뜻 (예: 끔 요청) 은 지금 코드에서 안 쓰인다 `[추측]`.

---

## PE_emulator 가 그대로 쓸 작은 표

| 함수 (rva · 크기) | 인자 | 쓰는 칸 | 하위 호출 | syscall (번호 · 인자) | 반환 |
|---|---|---|---|---|---|
| `LdrProtectMrdata` (`0xa3ec0` · 195) | `ECX`: 0 = 열기, ≠0 = 닫기 | `LdrpMrdataUnprotected` (`+1`/`−1`); `LdrpMrdataLock` 비트 0 잡고 풂 | `LdrpChangeMrdataProtection` (카운터 0 ↔ 1 일 때만), 락 획득·해제 | 직접 없음 | 없음 (`void` 모양). 닫을 게 없거나 −1 이면 `INT 0x29` 코드 `0xe` |
| `LdrpChangeMrdataProtection` (`0xa3f90` · 102) | `ECX`: 새 보호값 4 또는 2 | 없음 (`LdrpMrdataBase`·`Size` 읽기) | `LdrpLocateMrdata` (`Base == 0` 일 때) | **`NtProtectVirtualMemory` `0x50`**: (−1, `&Base`, `&Size`, 4 또는 2, `&옛보호`) | 없음. 실패하면 `INT 0x29` 코드 5 |
| `LdrpLocateMrdata` (`0xa4000` · 110) | 없음 | **`LdrpMrdataSize` (`+0x52`), `LdrpMrdataBase` (`+0x59`)** | `RtlImageNtHeaderEx`, `RtlSectionTableFromVirtualAddress`, `LdrpMakePermanentImageCommit` | 없음 | 없음. 구역 못 찾으면 코드 5 |
| `LdrpMakePermanentImageCommit` (`0xa3e78` · 56) | `RCX` 시작, `RDX` 크기 | 없음 (각 쪽 8B 읽고 같은 값 다시 씀) | 없음 | 없음 | 없음 |
| `RtlInsertInvertedFunctionTable` (`0xa4074` · 140) | `RCX` 이미지 시작, `EDX` `SizeOfImage` | 표 칸 (아래 Entry), 락 `LdrpInvertedFunctionTableSRWLock` | `RtlImageDirectoryEntryToData(시작, 1, 3, &크기)`, 락 잡기, `LdrProtectMrdata(0)`, `RtlpInsert…Entry(…)`, `LdrProtectMrdata(1)`, 락 풀기 | 없음 | 없음 |
| `RtlpInsertInvertedFunctionTableEntry` (`0xa4110` · 227) | `RDX` 시작, `R8` `FunctionTable`, `R9D` `SizeOfImage`, 스택 `SizeOfTable` | `Epoch` +2, 칸 (`FunctionTable`·`ImageBase`·`SizeOfImage`·`SizeOfTable`), `CurrentSize` +1; 가득 차면 `Overflow = 1` 만 | `memmove` (끼울 자리가 끝이 아닐 때) | 없음 | 없음 |
| `RtlRemoveInvertedFunctionTable` (`0xa7968` · 46) | `RCX` 이미지 시작 | 락만 | 락 잡기, `RtlxRemove…`, 락 풀기 | 없음 | 없음 |
| `RtlxRemoveInvertedFunctionTable` (`0xa79a0` · 78) | `RDX` 이미지 시작 | 없음 (칸 1 ~ 끝을 뒤에서 앞으로 `ImageBase` 비교; 못 찾으면 조용히 끝) | `LdrProtectMrdata(0)`, `RtlpRemove…Entry(번호)`, `LdrProtectMrdata(1)` | 없음 | 없음 |
| `RtlpRemoveInvertedFunctionTableEntry` (`0xa87a8` · 105) | `EDX` 칸 번호 | `Epoch` +2, `CurrentSize` −1 (뒤 칸을 앞으로 당김; 마지막 칸 바이트는 안 지움) | `memmove` (`CurrentSize == 2` 가 아닐 때) | 없음 | 없음 |
| `RtlpxLookupFunctionTable` (`0x14840` · 1339) | `RCX` 주소, `RDX` 결과 24B | 결과 24B 만 (표·락은 빠른 길에서 락 값만) | `LdrInitState == 3` → 표 이진 검색 + 락, 아니면 `RtlImageNtHeaderEx`·`RtlpImageDirectoryEntryToData32`·`RtlAddressInSectionTable` | **`NtQueryVirtualMemory` `0x23`**: (−1, 주소, 6, 버퍼, 0x18, NULL) (느린 길). 빠른 길 경합 때만 `NtAlertThreadByThreadIdEx` `0x72`, `NtSetInformationThread` `0xd` (−2, `0x38`, 버퍼, 0x10) | `RAX` = `FunctionTable` 또는 0 |
| `LdrControlFlowGuardEnforcedWithExportSuppression` (`0xa60c0` · 45) | 없음 | 없음 (`LdrSystemDllInitBlock +0xb8 · +0x9c · +0xa0` 읽기만) | 없음 | 없음 | `AL` = 1 (`+0xb8 ≠ 0` 그리고 `+0x9c & 1 == 0` 그리고 `(+0xa0 >> 40) & 3 == 3`), 아니면 0. test.exe = 0 |
| `LdrpHandleUserCopyAttributeList` (`0x15ddb0` · 372) | 없음 (PEB 에서 읽음) | `.data` `0x1ce268` ← 속성 `0xa0022` 값 (0/1), `ProcessParameters +0x448 = 0` | `LdrpLogInternal` (오류 때) | 없음 | `EAX` = 0 또는 `0xc000000d`. test.exe 는 목록 0 이라 바로 0 |

PE_emulator 가 `+0x3e6 ~ +0x4a1` 을 채울 때 순서만 지키면 되는 요약:
1. 카운터 1 (파일 값). `Insert(ntdll)` 로 칸 1 에 ntdll, `CurrentSize` 2, `Epoch` 2 (syscall 없음).
2. `Lookup(ntdll)` 로 지역 24B (`NtQueryVirtualMemory` 1번).
3. 열기 (카운터 2). 칸 0 ← 24B, 그다음 `SizeOfImage` 를 헤더 값으로 덮음.
4. `Remove(ntdll)`: `CurrentSize` 1, `Epoch` 4.
5. 닫기 (카운터 1), `LdrpCaptureCriticalThunks`, 닫기 (카운터 0) → 이때 `LdrpMrdataBase`·`Size` 가 처음 써지고 `NtProtectVirtualMemory(PAGE_READONLY)`.
6. CFG 확인 = 0 → `LdrpSuppressedExportOverrideListPtr` 는 그대로 0.

---

## 짐 지도 반영거리 (flow-writer 몫)

**baggage 3.9 `.mrdata` 칸 쓴 자리**

| 전역 (rva) | 쓴 자리 | 값 | 분류 (run44) |
|---|---|---|---|
| `LdrpMrdataSize` (`0x1e7018`) | `LdrpLocateMrdata +0x52` ← `LdrpInitializeProcess +0x488 → LdrProtectMrdata +0x6d → LdrpChangeMrdataProtection +0x14` | `.mrdata` 구역 `VirtualSize` = `0x35b8` | `≤P1a` (관찰 `0x35b8`) |
| `LdrpMrdataBase` (`0x1e7028`) | `LdrpLocateMrdata +0x59` (같은 길) | ntdll 시작 + `0x1e7000` | `≤P1a` (관찰 `7ffafb847000`) |
| `LdrpMrdataUnprotected` (`0x1e7128`) | `LdrProtectMrdata +0x60 · +0xbb`; 파일 값 **1** | 1 → `+0x488` 에서 0 | `≤P1a` (끝까지 0* 에서 옮김; P1a 0) |
| `LdrpMrdataLock` (`.data` `0x1d4de8`) | `LdrProtectMrdata +0x47` 잡음 / 꼬리호출 풀기 (이미 `LdrInitializeMrdata +0x23` 이 0 씀) | 0 | 끝까지 0 그대로 |
| `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` | `LdrEnsureMrdataHeapExists +0xef · +0xe8` (초기화 길 밖: `RtlAddGrowableFunctionTable +0x40`) | | 끝까지 0 그대로, 쓴 자리 이제 앎 |
| `LdrpSuppressedExportOverrideListPtr` (`.data` `0x1d2920`) | `LdrpInitializeProcess +0x49d` (CFG 내보내기 억제일 때만, test.exe 는 안 탐) | `LdrpSuppressedExportOverrideList` 주소 (`0x1740f0`) | 끝까지 0, 조건부 안 탐 (쓴 자리 이제 앎) |
| `.data` `0x1ce268` (이름 없음, 3.8 "목록 밖 자리") | `LdrpHandleUserCopyAttributeList +0x82` (test.exe 는 안 탐) | 속성 `0xa0022` 값 0/1 | 끝까지 0 (조건부 안 탐) |

**`LdrpInvertedFunctionTables` (`.mrdata` `0x1e7430`, `0x3010`) 칸 표**: 위 1(a) 첫 표와 같다 (머리 4칸 + 칸 0 + 칸 1 ~ 511). 칸별 쓴 자리 = `RtlpInsert…Entry` / `RtlpRemove…Entry` / `LdrpInitializeProcess +0x44e · +0x45e · +0x467`. test.exe 기준: ntdll 은 칸 0 (직접), exe 는 칸 1 (`LdrpProcessMappedModule +0x177`, Insert), 나머지 DLL 은 정렬 삽입. 사진 P1a `CurrentSize 2`·`Epoch 6`, P1b `6`·`0xe`.
- `LdrpInvertedFunctionTableSRWLock` (`.data` `0x1ce418`): 락 값. `Insert +0x44`·`Remove +0x10` 이 잡고 풂, 룩업 빠른 길이 `CMPXCHG.LOCK` 로 공유 잡기. 파일 값 0 (구역 파일 끝 밖), 끝까지 0 그대로 `[관찰]`.

---

## 동적분석으로 확인해야 할 것 (선택 — Q104 VM 단계에 얹을 수 있게)

정적과 run44 사진(P1a)이 이미 맞아서 **꼭 필요한 것은 없다.** 얹을 거면 (멈춤점은 test.exe 만, ntdll 은 모든 프로세스 공통이니 조건 필요):

1. `ntdll!LdrpInitializeProcess+0x41a` (Insert 복귀 직후): `dd ntdll+1e7430 L4` (→ `2, 0x200, 2, 0`), `dq ntdll+1e7440 L6` (칸 0 = 0, 칸 1 = ntdll). `dd ntdll+1e7128 L1` = 1.
2. `ntdll!RtlpxLookupFunctionTable+0x2d0` (느린 길 syscall 직전): `r rcx, rdx, r8, r9` = `-1`, ntdll 시작, `6`, 버퍼 → 정말 이 길을 타는지 (정적 갈래 `LdrInitState != 3`). `+0x2d5` 뒤 `dq @rsp+30 L3`.
3. `ntdll!LdrpInitializeProcess+0x475`: `dd ntdll+1e7430 L4` (→ `1, 0x200, 4, 0`), `dq ntdll+1e7440 L6` (칸 0 = ntdll, 칸 1 = ntdll 복사본), `dd ntdll+1e7128 L1` = 2.
4. `ntdll!LdrpInitializeProcess+0x486` 와 `+0x48d`: 카운터 `1` → `0`, `dq ntdll+1e7018 L1` / `dq ntdll+1e7028 L1` 이 `+0x486` 에선 0, `+0x48d` 에선 `0x35b8` / ntdll+`0x1e7000`. `!vprot ntdll+1e7000` 이 `+0x48d` 뒤에 `PAGE_READONLY`.
5. 커널 쪽: `nt!NtProtectVirtualMemory` (test.exe 조건) 에서 `+0x488` 길의 인자 (`R10 = -1`, 크기 `0x35b8` → 쪽 맞춤 `0x4000`, 새 보호 `2`) 가 이 프로세스에서 그 구간 첫 호출인지.
6. CFG 반환 0: `ntdll!LdrpInitializeProcess+0x492` 에서 `r eax` (→ 0). 이미 사진으로 닫혀 있어서 생략 가능.
- 안 닫힌 것 (정적으로 못 정함): `PspCopyUserAttributes` 로 쓴 목록이 `ProcessParameters +0x448` 에 이어지는 줄, 속성 `0x22` 의 공개 이름, `LdrpSuppressedExportOverrideListPtr` 를 읽는 곳, `LdrpMakePermanentImageCommit` 의 목적. test.exe 로는 속성 목록이 비어서 `0x1ce268` 이 쓰이는 길을 동적으로 못 본다 (다른 부모 프로세스가 `0xa0022` 속성을 주는 시험이 따로 필요).

## 판정

**분석성공.** 요청서 1(a)~(e)·2 모두 답이 있고, 근거는 `.asm` 줄·PE 파일 바이트·run44 P1a 덤프로 서로 맞는다. 덤으로 ntdll 안 `.mrdata` 열고 닫기 카운터가 **파일 값 1 에서 시작**한다는 것과, 이 구간 유일한 `.mrdata` 쓰기 끔 syscall 이 `LdrpInitializeProcess +0x488` 이라는 것이 새로 나왔다. 부분 답: 칸 0 에 Insert 를 거쳐 쓰는 **이유** (`[추측]`), 속성 `0x22` 의 공개 이름 (PDB 에 없음).

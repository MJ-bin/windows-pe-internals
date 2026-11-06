# 62 kb-heap-init — 정적 분석 결과

- 요청서: `_workspace/sast/62_kb-heap-init.md` (Q128, run67 예정)
- 모듈 / 바이너리: kernelbase · ntdll · kernel32 (`ms/bin/.../System32/`, 새로 analyze 안 함). 읽기만 한 ntoskrnl `.asm` 도 있음.
  - pdb guid+age: KernelBase `72CCE506944DE0267C172AD64287F9BD1` · ntdll `C3093720177DA6851519DCFFE8DF53FE1` · kernel32 `AE9E9D8F7AC1467A645320FDC23235D01` · ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID 와 같음)
- 뽑은 함수 (새로, 모두 `ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`):
  - 요청 대상 5개: kernelbase `KernelBaseDllInitializeMemoryManager` · ntdll `RtlUnicodeStringToAnsiString` (depth 1 → `RtlpCtAllocateMemory`·`RtlUnicodeToUTF8N` 딸려 나옴) · `LdrSetDllManifestProber` · `LdrpFindDllActivationContext` (depth 1 → `LdrResFindResourceDirectory`·`RtlRunOnceExecuteOnce`·`RtlReleaseActivationContext`·`LdrpLogInternal`)
  - **범위를 조금 넓힌 것 (이유 있음, 모두 depth 0)**: kernelbase `GlobalAlloc`·`GetCommandLineA` (PE_emulator 가 멈춘 길의 바로 다음 소비자), kernel32 `BaseDllInitializeMemoryManager2` (`KernelBaseGlobalData+0x40` 을 읽는 곳, 이름은 `.asm` 헤더에서 `…MemoryManager2`), kernel32 `BasepProbeForDllManifest` (4(b) 가 "앞" 이라 요청서 약속대로 한 줄), ntdll `RtlInitializeHandleTable`. 그리고 `BasepProbeForDllManifest` 가 어떤 상태값을 주는지 보려고 `LdrResSearchResource`·`LdrpResSearchResourceMappedFile`·`LdrpResGetResourceDirectory` 를 **열어만 보고** 멈췄다 (뜻 못 읽음, 아래 4(c)).
  - 새 `_refs` (읽고 쓰는 곳 표): kernelbase `KernelBaseGlobalData`(0x100바이트)·`SystemRangeStart`·`BaseHeapHandleTablePtr`, ntdll `GlobalRtlNlsState`·`Utf8TableInfo`·`LdrpManifestProberRoutine`·`LdrpCreateActCtxLanguageW`·`LdrpReleaseActCtxW`.
- 버전 확인 (알려진 사실과 대조): `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 일치, `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal` 일치 (`_calls.tsv`). → 일치.
- 스크립트 문제: 없음. 주의 하나 — `pdb_aliases.py`·`pdb_struct.py` 는 `ms/sym/<이름>.pdb` 를 이름 그대로 찾는다. kernelbase 는 `KernelBase.dll` 이라고 써야 한다 (요청서의 `kernelbase.dll` 은 "PDB 없음"). `run_ghidra.sh` 는 대소문자 상관없음.
- 오프셋 대조: 아래 `+0x..` 는 전부 `.asm` 줄에서 다시 찾았다 (call/ret 둘 다, 점프 목적지는 "목적지 − 함수 시작").

## 한 줄 결론 (PE_emulator 용)

1. **`BaseHeap` = `PEB.ProcessHeap` (PEB `+0x30`) 그대로.** 새 힙을 만들지 않는다. 같은 함수가 `SystemRangeStart` (= `0xFFFF800000000000`) 와 핸들 표 `BaseHeapHandleTable` 도 채운다. syscall 은 `NtQuerySystemInformation(0x32)` 하나뿐.
4(b). **kernel32 `_BaseDllInitialize` 는 dll1·dll2 의 `LdrpFindDllActivationContext` 보다 앞** 이다 (`LdrpInitializeProcess +0x19a2` 안 vs `+0x1aa9`). 그래서 진짜는 exe·dll1·dll2 때 `LdrpManifestProberRoutine` 이 **0 이 아니다** (`kernel32+0x23f90`). PE_emulator 의 0 은 kernel32·kernelbase 를 올릴 때만 맞다.

## 질문별 답 [정적]

### 1. kernelbase `KernelBaseDllInitializeMemoryManager` (rva `0xf8344`, 155B)

- 부르는 곳은 하나: `_KernelBaseBaseDllInitialize +0x1ad / +0x1b2` (DLL_PROCESS_ATTACH 갈래, `+0x195` 이후. 바로 앞 `+0x1a6` 은 `BasepIsSecureProcess` 에 1). 인자 없음, 반환 없음 (호출한 쪽도 `+0x1b2` 에서 RAX 를 안 쓴다).
- 하는 일, 코드 순서 (`ms/disassemble/kernelbase/KernelBaseDllInitializeMemoryManager.asm`):

| 줄 | 하는 일 |
|---|---|
| `+0x6` · `+0x17` · `+0x1b` | `GS:[0x60]` (PEB) → `[PEB+0x30]` (`ProcessHeap`) → **`BaseHeap`** (rva `0x3aefa0`) 에 저장. 조건 없음 |
| `+0xf` · `+0x22` | `SystemRangeStart` (rva `0x3b0350`) ≠ 0 이면 `+0x52` 로 건너뜀 (`JNZ 0x1800f8396`) |
| `+0x24 ~ +0x3a` / ret `+0x41` | `NtQuerySystemInformation(ECX=0x32, RDX=&지역, R8D=8, R9D=0)` — 클래스 `0x32` = SystemRangeStart 정보 |
| `+0x46` · `+0x4b` | 지역 값을 **`SystemRangeStart`** 에 저장 |
| `+0x52 ~ +0x66` / ret `+0x6d` | `RtlInitializeHandleTable(ECX=0xffff, EDX=0x10, R8=&BaseHeapHandleTable)` (rva `0x3aefc0`) |
| `+0x72 ~ +0x8e` | `KernelBaseGlobalData+0x40 ← BaseHeap` (`+0x79`), `+0x50 ← SystemRangeStart` (`+0x87`), `+0x48 ← &BaseHeapHandleTable` (`+0x8e`) |
| `+0x9a` | `RET` |

- **값의 출처**:
  - `BaseHeap` = `PEB.ProcessHeap`. `[관찰]` run44 사진에서 `ProcessHeap` = `0x25996f30000` (P1a 부터 P2 까지 같음: `ms/logs/04_baggage-read_P1a.txt:1739`, `_P1b.txt:2437`, `_P2.txt:2449`). 이 힙은 `LdrpInitializeProcess +0xa4b LdrpInitializeProcessHeap` 가 만든다 (flow.md, run46). **kernelbase `BaseHeap` 칸 자체는 사진에 없다** (kernelbase `.data` 는 안 읽음) → 아래 "동적".
  - `SystemRangeStart` = `0xFFFF800000000000`. 커널 `ExpQuerySystemInformation +0x233 CMP EBX,0x32` / `+0x23c JZ` → `+0xe18 CMP R14D,8` / `+0xe1c JZ` → `+0x1068 MOV RAX,-0x800000000000` / `+0x1072 MOV [RDI],RAX` (`ms/disassemble/ntoskrnl/ExpQuerySystemInformation.asm`, 주인 없는 덩어리 2657 ~ 2663줄). 상수를 직접 쓴다 (커널 변수를 읽지 않음). 출력 길이가 8 이 아니면 다른 갈래.
  - 핸들 표: `RtlInitializeHandleTable` (ntdll rva `0x106ee0`, 43B) 는 0x30 바이트를 0 으로 채우고 `[+0] = 0xffff` (`+0x1c`), `[+4] = 0x10` (`+0x26`), `[+8] = 0` (`+0x22`) 만 쓴다. 할당·syscall 없음. 표가 자라는 건 나중에 `RtlAllocateHandle` 몫.
- 하위 호출·syscall: `NtQuerySystemInformation` (`SystemRangeStart == 0` 일 때만; 처음 값이 0 이라 첫 호출에서는 탄다 — kernelbase `.data` 의 파일에 없는 꼬리, `0x3aefa0` 등 모두 0) · `RtlInitializeHandleTable`. 새 힙·`RtlCreateHeap` 없음.
- **같은 값을 다시 쓰는 곳** (`BaseHeap` 이 두 번 쓰이는 게 정상):
  - kernelbase `entry +0x8 / +0xd` (`KernelBaseBaseDllInitialize` 가 돌아온 뒤, **이유와 상관없이**): `+0x20 SystemRangeStart ←`, `+0x2e BaseHeap ←` (`KernelBaseGlobalData+0x40` 앞 8B), `+0x3f BaseHeapHandleTablePtr` (rva `0x3b0348`) `←` (`+0x48` 칸). 래퍼 `KernelBaseBaseDllInitialize` 는 `+0x44` 에서 `_KernelBaseBaseDllInitialize` 로 `JMP` 한다 (꼬리호출).
  - kernel32 `_BaseDllInitialize +0xc2 / +0xc7` → `BaseDllInitializeMemoryManager2` (rva `0x2c9d8`): `+0x4` `KernelBaseGetGlobalData` → `+0x18` `[RAX+0x40]` 16B, `+0x1c` `[RAX+0x50]` → kernel32 자기 `BaseHeap` (rva `0xc2240`, `+0x21`) 와 `SystemRangeStart` (rva `0xc2238`, `+0x36`, 0 일 때만).
- **PE_emulator 가 막힌 길 (`GlobalAlloc`)**: `GlobalAlloc +0x63 ~ +0x7a` 가 `RtlAllocateHeap(RCX=[BaseHeap], EDX=Flags, R8=크기)` 를 부르는데, `Flags = 옵션 비트 | ([KernelBaseGlobalData+0] + 0x100000)` (`+0x63 MOV EAX,[KBGD]`, `+0x69 ADD EAX,0x100000`, `+0x6e OR EDX,EAX`). `KernelBaseGlobalData+0` 은 `_KernelBaseBaseDllInitialize +0x1cf/+0x1d6 RtlCreateTagHeap` 의 반환값을 `+0x1db` 에서 저장한 것인데, `RtlCreateTagHeap` 은 `PEB+0xbc` (NtGlobalFlag) `& 0x800 == 0` 이면 `+0x45 JZ → +0x67` 에서 곧장 0 을 준다 (`ms/disassemble/ntdll/RtlCreateTagHeap.asm`). `[관찰]` NtGlobalFlag = 0 (`_P1a.txt:1770`, `_P1b.txt:2468`, `_P2.txt:2480`). → **`KernelBaseGlobalData+0` = 0, `GlobalAlloc` 플래그는 `0x100000 | 옵션`.** 이동식(`GMEM_MOVEABLE`, `+0x44 TEST CL,2` → `+0xb1`)이면 `BaseHeapHandleTable` 을 쓰는 `RtlAllocateHandle` (`+0xd6 / +0xdd`) 도 탄다.

### 2. `KernelBaseGlobalData +0x40 ~ +0x57` 을 쓰는 곳 (`ms/disassemble/kernelbase/_refs/KernelBaseGlobalData.tsv`)

- `KernelBaseGlobalData` = rva `0x3aeec0`. PDB 공개 이름 사이로 보면 다음 이름 `BaseDllHandle` (`0x3aef98`) 앞까지 최대 `0xd8` 바이트. 이 구간은 8바이트 칸 셋이다.

| 칸 | 의미 | 쓰는 곳 (함수 + 오프셋) | 값 | 읽는 곳 |
|---|---|---|---|---|
| `+0x40` | `BaseHeap` 사본 | `KernelBaseDllInitializeMemoryManager +0x79` **하나뿐** | `PEB.ProcessHeap` | kernelbase `entry +0x27` (16B), kernel32 `BaseDllInitializeMemoryManager2 +0x18` (16B) |
| `+0x48` | `&BaseHeapHandleTable` (kernelbase `+0x3aefc0`) | `…MemoryManager +0x8e` **하나뿐** | 주소 | 위와 같은 16B 읽기 (`entry` 는 `+0x3f` 에 저장, kernel32 는 낮은 8B 만 저장) |
| `+0x50` | `SystemRangeStart` | `…MemoryManager +0x87` **하나뿐** | `0xFFFF800000000000` | kernelbase `entry +0xd`, kernel32 `…MemoryManager2 +0x1c` |

- 1 이 아닌 곳, 다른 쓰기: **없다.** `_refs` 313줄 중 이 구간 `WRITE` 는 위 셋뿐.
- 읽기 쪽 한계: 포인터(`KernelBaseGetGlobalData()` 반환값 + 오프셋)로 읽는 건 `_refs` 가 못 본다. kernel32 의 `KernelBaseGetGlobalData` 호출 146곳을 `objdump` 로 호출 직후 6명령까지 훑어 `[reg+0x40~0x57]` 읽기를 찾았더니 위 둘뿐이었다 (오탐·누락 가능). System32 전체 파일 문자열 검색에서 `KernelBaseGetGlobalData` 를 가진 건 kernel32·kernelbase 둘뿐.
- 이웃 칸 (이번 질문 밖, 짐 지도용으로만 적음):

| 칸 | 쓰는 곳 | 비고 |
|---|---|---|
| `+0x0` (4B) | `_KernelBaseBaseDllInitialize +0x1db` | `RtlCreateTagHeap` 반환 = 0 (위) |
| `+0x4` (1B) | `+0x337` | 읽는 곳 `CreateRemoteThreadEx +0x22e`·`UnhandledExceptionFilter +0x524` |
| `+0x8` (16B) · `+0x10` | `+0x349`, `+0x3d3`·`+0x6a4` | Windows 디렉터리 UNICODE_STRING 꼴 `[추측: 읽는 함수 이름 GetWindowsDirectoryW]` |
| `+0x18` (16B) · `+0x20` | `+0x3df`, `+0x469`·`+0x686` | 시스템 디렉터리 꼴 `[추측]` |
| `+0x58 ~ +0x90` | `GetLocaleFileInfo +0x90`, `SetupMainNlsFiles +0x111 ~ +0x164` | NLS 파일 정보 (Q129 쪽) |
| `+0xa0 ~ +0xb0` | `GetComputerNameExW`, `BasepUnInitComputerNameCache` | 컴퓨터 이름 캐시 |

### 3. ntdll `RtlUnicodeStringToAnsiString` (rva `0x4b9f0`, 807B) — `_KernelBaseBaseDllInitialize +0x490/+0x497`

- 부르기 전 (`.asm` `_KernelBaseBaseDllInitialize`): `+0x470 RAX = [PEB+0x20]` (`ProcessParameters`) → `+0x474 RDX = &BaseUnicodeCommandLine` · `+0x47b R8B = 1` · `+0x47e RCX = &BaseAnsiCommandLine` → `+0x485 MOVUPS XMM0,[RAX+0x70]` (`CommandLine` 16B) → `+0x489` `BaseUnicodeCommandLine` 에 저장. **새 버퍼 없이 머리(Length·MaximumLength·Buffer 포인터)만 복사** — 글자 버퍼는 프로세스 매개변수 안 그대로.
- 인자: `RCX` = 출력 `ANSI_STRING`, `RDX` = 입력 `UNICODE_STRING`, `R8B` = 할당 여부 (`+0x3 MOV [R11+0x18],R8B`, `+0x1a R15D`).
- 갈래 (test.exe: 명령줄 `"test.exe"`, ACP 949 기준), `ms/disassemble/ntdll/RtlUnicodeStringToAnsiString.asm`:

| 단계 | 줄 | 하는 일 |
|---|---|---|
| 길이 셈 (인라인. `RtlUnicodeStringToAnsiSize` 호출 없음) | `+0x2d` · `+0x30` | 입력 `Length`·`Buffer` 읽기 |
| | `+0x38 ~ +0x54` | `R13D = 0xfde9` (UTF-8). `GlobalRtlNlsState+0x40` (OEM `CodePage`) 또는 `GlobalRtlNlsState` (ACP `CodePage`) 가 `0xfde9` 면 `+0x26b` 갈래 → `+0x283 / +0x288 RtlUnicodeToUTF8N(NULL,0,&크기,…)` |
| | `+0x68 ~ +0x76` | 아니면 `GlobalRtlNlsState+0xc` (ACP 표 `DBCSCodePage`) ≠ 0 → `+0x28d ~ +0x2b1`: 글자마다 `WideCharTable[c] >> 8` 이 0 이 아니면 2바이트, 아니면 1바이트를 더한다. 0 이면 글자 수 (`+0x72 SHR EAX,1`) |
| 크기 검사 | `+0x87 ~ +0x96` | 크기 `+1` 이 `0xffff` 를 넘으면 `+0x2f7` → **`0xC00000F0`** |
| 할당 (`R8B=1`) | `+0x9f JNZ → +0x2c0`, `+0x2c2 / +0x2c7` | `ECX = 크기+1` 로 `0x180021a90` 호출 = **`RtlpCtAllocateMemory`** (같은 주소 이름 셋: `NtdllpAllocateStringRoutine`·`RtlpCtAllocateMemory`·`RtlpAllocateAtom`). 몸통은 `RtlAllocateHeap(PEB.ProcessHeap [PEB+0x30], Flags 0, 크기)` 꼬리호출 (`RtlpCtAllocateMemory +0x0 ~ +0x12`). NULL 이면 `+0x2ce → +0x30d` **`0xC0000017`**. 성공이면 `+0x2c7 [RSI+8] = Buffer`, `+0x2d0 [RSI+2] = 크기+1` (`MaximumLength`) |
| 할당 안 함 (`R8B=0`) | `+0xa5 ~ +0xab`, `+0x2dc`, `+0x2e1`, `+0x323` | 호출한 쪽 `MaximumLength` 가 모자라면: 0 이면 `0x80000005`, 아니면 `R12B=1` 로 잘라서 쓰고 끝에서 `0x80000005` |
| 변환 (인라인. `RtlUnicodeToMultiByteN` 호출 없음) | `+0x122 / +0x126 → +0x1d6` | UTF-8 이면 `+0x1f0 / +0x1f5 RtlUnicodeToUTF8N` |
| | `+0x135 JNZ → +0x170 ~ +0x1c4` | DBCS: 표 `[RDI + 글자*2]` 의 위 바이트가 있으면 먼저 쓰고 아래 바이트 (남은 칸이 2 미만이면 그만, `+0x202`) |
| | `+0x156 ~ +0x16e` | SBCS: 글자마다 `byte 표[글자]` |
| 마무리 | `+0x23a ~ +0x258` | `Buffer[길이] = 0` (`+0x245`), `Length = 길이` (`+0x249 MOV [RSI],R10W`), 반환 0 (`R12B` 켜졌으면 `0x80000005`) |

- **NLS 표와의 이음**: `GlobalRtlNlsState` (ntdll rva `0x1ccf90`) = PDB 타입 `_RTL_NLS_STATE` (+0 `DefaultAcpTableInfo` = `_CPTABLEINFO`, +0x40 `DefaultOemTableInfo`). 채우는 곳은 run57 의 `LdrpInitializeNlsInfo +0x5c/+0x61 RtlpInitCodePageTables` (그 안 `+0x46/+0x6c NtGetNlsSectionPtr`, `+0x90/+0xa3 RtlInitCodePageTable`) — **이어진다.** `[관찰]` 사진 P1a·P1b·P2 의 `ntdll+0x1ccf90`: `CodePage 0x03b5` (949), `MaximumCharacterSize 2`, `DBCSCodePage 1`, 앞 바이트 범위 `0x81 ~ 0xfe`, `WideCharTable` = `0x25996d30022` (`_P2.txt:4829`, `_P1a.txt:3699`, `_P1b.txt:4817`). 그래서 이 VM 에서는 **DBCS 갈래**를 탄다. UTF-8 갈래(`0xfde9`)는 안 탄다.
- syscall: **이 함수 안에는 없다.** `RtlAllocateHeap` (공용 부품, 이름만) 안에서 힙이 모자랄 때 생길 수 있는 syscall 은 동적 몫.
- 출력 세 칸 (test.exe 기준):
  - 입력 `[관찰]` `CommandLine` = `{Length 0x10, MaximumLength 0x12, Buffer 0x25996f3813e}`, 문자열 `"test.exe"` (`_P2.txt:3266`, `_P1a.txt:2556`, `_P1b.txt:3254` 모두 같음).
  - 출력 `[정적]`: `Length = 8`, `MaximumLength = 9`, `Buffer` = 프로세스 힙의 9바이트 새 블록, 내용 `"test.exe\0"`. 949 에서도 ASCII 는 표의 위 바이트가 0 이라 글자당 1바이트.
  - "길이 = 유니코드 길이 / 2" 는 **ASCII 만일 때만** 맞다 (한글 같은 2바이트 글자가 있으면 더 크다).
  - 반환 0.
- 실패 때 (`+0x49c TEST EAX,EAX` / `+0x49e JS → +0x6b0`): `BaseAnsiCommandLine` 의 `Buffer` (`+0x6b0`) 와 `Length|MaximumLength` (`+0x6b7`) 를 0 으로 지우고 계속한다 (`+0x6bd JMP +0x4a4`). **DllMain 은 실패하지 않는다.**
- 쓰는 쪽: `GetCommandLineA` (kernelbase rva `0xfd6e0`) = `+0x0 MOV RAX,[BaseAnsiCommandLine+0x8]; RET`. 실패했으면 빈 문자열이 아니라 **NULL** 을 준다.
- 비정상 종료 정리: `RtlUnicodeStringToAnsiString$fin$0` (rva `0x168c20`) 이 할당 버퍼를 해제 (`+0x2b` 해제 함수 호출).

### 4. ntdll `LdrSetDllManifestProber` 와 `LdrpFindDllActivationContext`

**(a) `LdrSetDllManifestProber` (rva `0x10c7e0`, 22B)** — 인자 셋을 전역 셋에 그대로 쓰고 끝낸다. 호출·검사·syscall 없음.

| 줄 | 인자 | 쓰는 전역 | rva | 읽는 곳 |
|---|---|---|---|---|
| `+0x0` | RCX | `LdrpManifestProberRoutine` | `0x1d23c0` | `LdrpFindDllActivationContext +0x6`·`+0x4f` (이것뿐) |
| `+0x7` | RDX | `LdrpCreateActCtxLanguageW` | `0x1d23a0` | `LdrpQuerySxSMUIFile +0x118`·`+0x1fd` |
| `+0xe` | R8 | `LdrpReleaseActCtxW` | `0x1d23a8` | `LdrpQuerySxSMUIFile +0x21e` |

- 셋 다 쓰는 곳은 이 함수 하나 (`_refs`, 포인터 값 0곳). 처음 값은 모두 0 (`.data` 의 파일 밖 꼬리: 구간 `0x1cc000 ~ 0x1d4f58`, 파일에 있는 건 `0x1ce000` 까지). 이 함수를 import 하는 System32 파일은 kernel32 뿐.
- kernel32 `_BaseDllInitialize` 첫 호출 (`+0xb3 / +0xba`) 이 넘기는 값: `RCX = kernel32+0x23f90` (`BasepProbeForDllManifest`, `+0xac`), `RDX = kernel32+0x61520` (`BasepRegenerateActCtxWithLanguage`, `+0xa5`), `R8 = [__imp_ReleaseActCtx]` (`+0x9e`) = kernelbase `ReleaseActCtx` (rva `0xefcd0`).
- `[관찰]` run44 `ntdll` `.data` 읽기 (`NTDLL_DATA`): P1a (`LdrpInitializeKernel32Functions` 부르기 직전) 는 셋 다 0, P1b 부터 P2 까지 `0x7ffafa691520` (= kernel32 `0x7ffafa630000` + `0x61520`), `0x7ffaf8a0fcd0` (= kernelbase `0x7ffaf8920000` + `0xefcd0`), `0x7ffafa653f90` (= kernel32 + `0x23f90`) (`_P1a.txt:5044,5046`, `_P1b.txt:6162,6164`, `_P2.txt:6174,6176`). 정적으로 계산한 값과 같다.
- **둘째 호출 `_BaseDllInitialize +0x3af / +0x3b6`**: `+0x3a8 R8D=0`, `+0x3ab EDX=0`, `+0x3ad ECX=0` → **셋 다 0 으로 되돌리는 길.** 들어오는 길은 `+0x21f / +0x224 _Kernel32LegacyLib_BaseDllInitialize` 가 0 (실패) 을 돌려줄 때 (`+0x228 JZ 0x18002c9af` = `+0x39f`) 이면서 `SIL ≠ 0` (`+0x39f TEST SIL,SIL`; `+0xbf MOV ESI,R15D` 때문에 attach 에서만 켜짐). 즉 DllMain attach 실패의 되돌리기이고, 정상 실행에서는 안 탄다 `[정적]`. 이 함수를 부르는 곳은 kernel32 에서 이 둘뿐이다.

**(b) 시간 순서 — kernel32 DllMain 이 앞이다.**
- test.exe `Subsystem` = 3 (콘솔, `objdump -p`). `LdrpInitializeProcess +0x1980 MOVZX EAX,[R13+0x5c]` → `+0x1985 SUB AX,2` → `+0x1993 JA` : 2 또는 3 이면 `+0x199d / +0x19a2 LdrpInitializeKernel32Functions` 를 부른다 (test.exe 는 탄다).
- 그 안 `+0xb1 ~ +0xbb` (`ECX = 0x4001`) / ret `+0xc0` `LdrLoadDll` (kernel32 를 올림). 사슬은 `LdrLoadDll +0x16b/+0x170 → LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal +0x27f/+0x284 → LdrpPrepareModuleForExecution` 이고, 그 `+0xbf ~ +0xcf` 가 "로드 문맥 `[entry+0xb0]` 이 있고 그 `+0x20` 의 비트 0 이 꺼져 있으면" `+0xea / +0xef LdrpInitializeGraphRecurse` → `LdrpInitializeGraphRecurse +0x65/+0x6a LdrpInitializeNode` → `LdrpInitializeNode +0x197/+0x19c LdrpCallInitRoutine` 로 간다. 이 DllMain 이 도는 동안 kernelbase → kernel32 순서로 시작 함수가 불린다 (`[관찰]` run7 콜스택, `runs/03_sprint/flow.md` 3020·3053줄. 원본 로그는 지금 `ms/logs/` 에 없어 이번에 다시 보지는 못했다).
- exe 쪽 문맥의 비트 0 은 `LdrpInitializeProcess +0x1a94 OR [RSP+0x2d0],R12D` 에서 **그 뒤에** 켜진다 (flow.md 687줄: exe 는 켜기를 건너뜀).
- dll1·dll2 의 `LdrpFindDllActivationContext` 는 `LdrpMapAndSnapDependency +0x5e / +0x63` 이고, exe import 올리기는 `LdrpInitializeProcess +0x1aa4 / +0x1aa9` — `+0x19a2` **뒤**다.
- 그래서: kernel32·kernelbase 를 올리는 `LdrpMapAndSnapDependency` 때는 값이 0 (DllMain 전) → `+0x1a JZ → +0x114` 에서 0 반환. **exe → dll1 → dll2 때는 `kernel32+0x23f90` 이 들어 있다.** PE_emulator 가 0 으로 두면 진짜와 갈린다 (진짜는 그 셋에서 prober 를 부름).
- `[관찰]` 사진도 같은 쪽: P1a 0 → P1b `kernel32+0x23f90` (위 (a)). 단, P1a 와 P1b 사이에 kernel32 올리기와 exe import 올리기가 **둘 다** 있어서 사진만으로는 순서를 못 가른다 → 동적.
- `BasepProbeForDllManifest` (kernel32 rva `0x23f90`, 275B) 한 줄 (본문은 안 읽음): `RCX = DllBase, RDX = 경로, R8 = 출력 칸`. `+0x4a IsFusionFullySupported` → PEB 플래그 검사 → `+0x86 / +0x8d LdrResFindResourceDirectory` (자원 `RT_MANIFEST` `0x18`, ID 2) → 찾으면 `+0xea / +0xf1 CreateActCtxW` 로 핸들을 `*R8` 에, 못 찾으면 그 상태값을 그대로 돌려줌. **직접 syscall 없음.** `CreateActCtxW` 안은 매니페스트가 있을 때만 탄다. **sample 의 `test.exe`·`dll1.dll`·`dll2.dll` 은 PE 데이터 디렉터리 2 (자원) 가 RVA 0 / 크기 0 이다** (`objdump -p sample/bin/*`, `ms/bin/` 과 md5 같음) → 매니페스트 없음 → `CreateActCtxW` 는 안 탄다.

**(c) `LdrpFindDllActivationContext` (rva `0x96000`, 495B)** — 인자 `RCX` = 모듈 항목 (`_LDR_DATA_TABLE_ENTRY`). 부르는 곳은 `LdrpMapAndSnapDependency +0x5e / +0x63` 하나 (그쪽 조건: 문맥 `+0x20` 비트 `0x800000` 이면 건너뜀 `+0x4f JNZ → +0x96`, 항목 `+0x10c` (`LoadReason`) 가 9 면 건너뜀 `+0x59 JZ`, 반환이 음수면 `+0x69 ~ +0x6d` 에서 문맥 `+0x28` 칸에 상태를 쓰고 로드 실패).

| 줄 | 하는 일 |
|---|---|
| `+0x6` · `+0x1a JZ → +0x114` | prober = 0 이면 `+0x114 XOR EAX,EAX` / `+0x11b RET` — **0 반환, 아무 칸도 안 씀** |
| `+0x20` · `+0x24` · `+0x2d` | `R9 = 항목+0x30` (`DllBase`), `RDX = PEB`, `RCX = LdrpImageEntry` |
| `+0x34 / +0x37 JZ → +0x16c` | 항목 = exe 항목이면: `PEB+0x2f8` (`ActivationContextData`) ≠ 0 → `+0x17a JMP +0x114` (0 반환). 0 이면 `+0x3d` 로 계속 |
| `+0x3d` | `RDX = 항목+0x50` (`FullDllName.Buffer`) |
| `+0x46 / +0x49 JZ → +0x19e` | exe 항목이면 경로가 `\??\X:\` 꼴일 때 `RDX += 8` (`\??\` 떼기, `+0x1e8 CMOVZ`) |
| `+0x4f ~ +0x78` | `RAX = prober` 를 `LdrResFindResourceDirectory` (`0x96320`) 와 `RtlRunOnceExecuteOnce` (`0x96200`) 주소와 먼저 비교 → 같으면 `+0x155 / +0x158 (ret +0x15d)`, `+0x162 (ret +0x167)` 에서 직접 부름. 왜 있는지 모름 `[추측: 프로파일 기반 직접 호출 단축]`. kernel32 가 넣는 값은 둘 다 아니라 안 탄다 |
| **`+0x7e / +0x83`** | CFG 간접 호출 `prober(RCX = DllBase, RDX = 경로 UTF-16, R8 = &지역[RSP+0x58])` (지역 칸은 `+0x11` 에서 0). 반환 `EAX` → `EBX` |
| `+0x85 / +0x8a` | `EAX == 0xC000000F` 면 `+0x90` (없음 취급). 아니면 `+0x8a JNZ → +0x11d` |
| `+0x11d ~ +0x150` | `EBX + 0x3fffff77 ≤ 2` (= `0xC0000089 / 8A / 8B`), `0xC00000BB`, `0xC0000002`, `0xC0000204` 면 `+0x90` (없음 취급). 그 밖 (0 포함) 은 `+0xc9` |
| `+0x90 ~ +0xc7` | 로그 (`LdrpLogInternal`, 줄 번호 `0x2e7`) 뒤 `EBX = 0` |
| `+0xc9 ~ +0xd1` | 출력 칸이 0 이 아니면 `+0x17c`: 항목 `+0x88` (`EntryPointActivationContext`) 에 옛 값이 있으면 `+0x188 / +0x18d RtlReleaseActivationContext`, 그리고 **`+0x192 [항목+0x88] = 출력 칸`** |
| `+0xd7 ~ +0x106` | `EBX ≥ 0` 이면 그대로 반환. 음수면 로그 (`0x304`) 뒤 같은 `EBX` 반환 → 로드 실패 |

- 쓰는 칸은 **항목 `+0x88` 하나뿐** (그리고 지역 칸). PEB 는 안 쓴다.
- test.exe 에서: exe·dll1·dll2 에 매니페스트가 없으니 prober 가 "없음" 쪽 상태나 0 을 줄 것이고, 어느 쪽이든 이 함수는 **0 을 반환하고 항목 `+0x88` 을 안 쓴다** (실제 실행이 성공하므로 "없음 취급" 집합 안이다 `[추측: 실행이 성공한다는 사실에서]`). 그 prober 가 내놓는 정확한 상태값은 `LdrResFindResourceDirectory → LdrResSearchResource +0x271/+0x276 LdrpResSearchResourceMappedFile → +0x22a LdrpResGetResourceDirectory` 안쪽이 정하는데 거기는 안 읽었다 (함수가 크고 요청 밖) → 아래 "동적" 또는 후속 정적.
- `[관찰]` `PEB.ActivationContextData` 는 사진 8개 모두 `(null)` (예: `_P2.txt:2515`) → exe 항목 검사 `+0x16c` 는 "0 → 계속" 쪽이다.

## PE_emulator 가 바로 쓸 작은 표

| 함수 | 인자 | 쓰는 칸 (모듈 rva) | 값 출처 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|---|
| kernelbase `KernelBaseDllInitializeMemoryManager` (`0xf8344`) | 없음 | `BaseHeap` (`0x3aefa0`) · `SystemRangeStart` (`0x3b0350`) · `BaseHeapHandleTable` (`0x3aefc0`, 0x30B) · `KernelBaseGlobalData+0x40/+0x48/+0x50` (`0x3aef00/08/10`) | `PEB.ProcessHeap` · 상수 `0xFFFF800000000000` · `{0xffff, 0x10, 0…}` · 앞 셋의 사본 | `NtQuerySystemInformation(0x32)` (`SystemRangeStart==0` 일 때) · `RtlInitializeHandleTable` | 첫째만 | 없음 |
| kernelbase `entry +0xd ~ +0x3f` | — | `SystemRangeStart` · `BaseHeap` · `BaseHeapHandleTablePtr` (`0x3b0348`) | `KernelBaseGlobalData+0x50/+0x40/+0x48` | — | — | — |
| kernel32 `BaseDllInitializeMemoryManager2` (`0x2c9d8`) | 없음 | kernel32 `BaseHeap` (`0xc2240`) · `SystemRangeStart` (`0xc2238`, 0 일 때만) | `KernelBaseGlobalData+0x40/+0x50` | `KernelBaseGetGlobalData` | 없음 | 없음 |
| kernelbase `_KernelBaseBaseDllInitialize +0x1cf` 반환 → `KernelBaseGlobalData+0` (`0x3aeec0`) | — | 4B | `RtlCreateTagHeap` = 0 (NtGlobalFlag 에 `0x800` 없음) | — | — | — |
| ntdll `RtlUnicodeStringToAnsiString` (`0x4b9f0`) | `RCX=&ANSI`, `RDX=&UNICODE`, `R8B=1` | 출력 `Length`(+0) · `MaximumLength`(+2) · `Buffer`(+8) | 입력 길이·ACP 949 표 (`GlobalRtlNlsState`) · 프로세스 힙 새 블록 | `RtlpCtAllocateMemory` (= `RtlAllocateHeap(ProcessHeap,0,크기+1)`) · (UTF-8 일 때만 `RtlUnicodeToUTF8N`) | 없음 | 0 · `0x80000005` · `0xC0000017` · `0xC00000F0` |
| 같은 호출 결과 (`test.exe`) | | `BaseAnsiCommandLine` (`0x3af050`) = `{8, 9, →힙 9B "test.exe\0"}` · `BaseUnicodeCommandLine` (`0x3af040`) = `{0x10, 0x12, 0x25996f3813e…}` (매개변수 안 버퍼 공유) | | | | |
| ntdll `LdrSetDllManifestProber` (`0x10c7e0`) | `RCX, RDX, R8` | `LdrpManifestProberRoutine` (`0x1d23c0`) · `LdrpCreateActCtxLanguageW` (`0x1d23a0`) · `LdrpReleaseActCtxW` (`0x1d23a8`) | `kernel32+0x23f90` · `kernel32+0x61520` · `kernelbase+0xefcd0` | 없음 | 없음 | 없음 |
| ntdll `LdrpFindDllActivationContext` (`0x96000`) | `RCX=모듈 항목` | 항목 `+0x88` (prober 가 핸들을 줄 때만) | prober 반환 | prober (CFG 간접) · `RtlReleaseActivationContext` · `LdrpLogInternal` | 없음 | 0 또는 음수 상태 |

## 짐 지도 반영거리

- kernelbase 전역 (전부 처음 0): `BaseHeap` · `BaseHeapHandleTable` · `BaseHeapHandleTablePtr` · `SystemRangeStart` · `KernelBaseGlobalData` 의 `+0x0 / +0x40 / +0x48 / +0x50` · `BaseUnicodeCommandLine` · `BaseAnsiCommandLine` (위 표 값).
- kernel32 전역: `BaseHeap` (`0xc2240`) · `SystemRangeStart` (`0xc2238`).
- ntdll 전역: `LdrpManifestProberRoutine` · `LdrpCreateActCtxLanguageW` · `LdrpReleaseActCtxW` (kernel32 DllMain 이 채움, `[관찰]` P1a 0 → P1b 값). 읽는 줄기는 `LdrpFindDllActivationContext` 와 `LdrpQuerySxSMUIFile` (MUI 쪽, 이름만).
- 프로세스 힙에 **9바이트 블록 1개** 가 생긴다 (`BaseAnsiCommandLine.Buffer`). run46 의 힙 블록 수 셈에 더할 것.
- 줄기 길이: `_KernelBaseBaseDllInitialize +0x1ad` → `KernelBaseDllInitializeMemoryManager` → (`NtQuerySystemInformation` syscall · `RtlInitializeHandleTable`) 로 **끝까지** 닿는다. `+0x490` → `RtlUnicodeStringToAnsiString` → `RtlpCtAllocateMemory` → `RtlAllocateHeap` (공용 부품, 이름만) 도 끝.

## 닫힘 / 동적 필요

| 질문 | 상태 | 동적 몫 (Q104 VM 단계에 얹을 것) |
|---|---|---|
| 1 | **정적으로 닫힘** | 확인용: `kernelbase+0x3aefa0` 이 `PEB+0x30` 과 같은지, `+0x3aef00 L3`, `+0x3aefc0 L6`, `+0x3b0348`, `+0x3b0350`, `kernel32+0xc2238 / +0xc2240` 읽기. 멈춤점 `kernelbase!_KernelBaseBaseDllInitialize +0x1b2` (ret) |
| 2 | 닫힘 (포인터 경유 읽기는 훑기 한계) | 위 읽기로 값 확인 |
| 3 | **정적으로 닫힘** (갈래는 사진의 NLS 표로 확정) | `kernelbase+0x3af040 L4`, `du/da poi(kernelbase+0x3af058)` 를 `+0x497` (ret) 에서 읽기. `RtlAllocateHeap` 안 syscall 은 모름 |
| 4(a) | 닫힘 | — |
| 4(b) | 코드 길은 **"앞"** 으로 닫힘. 순서 자체는 동적으로 확인 필요 (사진은 P1a~P1b 사이에 둘 다 들어 있어 못 가름, run7 원본 로그는 지금 없음) | 멈춤점 `ntdll!LdrSetDllManifestProber` (`r rcx,rdx,r8`, 콜스택 `kernel32!_BaseDllInitialize +0xba` 기대) 와 `ntdll!LdrpFindDllActivationContext` (매 호출 `dq ntdll+0x1d23c0 L1`, `du poi(@rcx+0x50)`). 기대 순서: kernel32·kernelbase (값 0) → `LdrSetDllManifestProber` → exe·dll1·dll2 (값 `kernel32+0x23f90`). 쓰기 감시 `ba w8 ntdll+0x1d23c0` 도 가능 |
| 4(c) | 닫힘, **단 prober 의 "없음" 상태값은 안 닫힘** | `ntdll!LdrpFindDllActivationContext +0x83` (ret) 에서 `r eax` 를 exe·dll1·dll2 세 번 읽기. 정적으로 닫으려면 `LdrpResGetResourceDirectory` 이하 후속 정적 요청 |

## 판정

**분석성공.** 요청서 질문 1 ~ 4 의 답이 `.asm` 줄과 함께 있고, 가장 급한 1번과 4(b)는 코드 길에서 확정된다. 남은 것은 순서의 동적 확인(4(b))과 prober 의 "없음" 상태값(4(c)) 두 가지다.

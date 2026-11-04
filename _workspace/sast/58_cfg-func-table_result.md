# 58 cfg-func-table — 정적 분석 결과

- 요청서: `_workspace/sast/58_cfg-func-table.md` (Q120a, 질문 1 ~ 6)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`, pdb guid+age `C3093720177DA6851519DCFFE8DF53FE1` — VM 사진 `ms/logs/04_baggage-read_P1a.txt` 59줄의 `lm` 경로와 같음), ntoskrnl (pdb guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준 GUID 와 같음)
- analyze 안 다시 함 (둘 다 `.gpr` + `_index.tsv` + `SetPdb:` 줄 있음). export 만 depth 0.
- 새로 뽑은 것 (`ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`):
  - ntdll: `RtlpInsertOrRemoveScpCfgFunctionTable` `LdrpCfgProcessLoadConfig` `RtlAddGrowableFunctionTable` `LdrEnsureMrdataHeapExists` `RtlInitializeHistoryTable` `RtlpFunctionAddressTableEntry` `RtlpLookupDynamicFunctionEntry` `LdrControlFlowGuardEnforced` `LdrProtectMrdata` `RtlDeleteFunctionTable` (요청서 대상) + 길 따라 더한 것: `LdrpCfgCheckRoutineCallback` `LdrpCfgDispatchRoutineCallback` `LdrpCgRoutineCallback` `LdrpMakePermanentImageCommit` `LdrpIsModuleUnderSystem32` `LdrpChangeMrdataProtection` `NtQuerySystemTime` `RtlQuerySystemTime` `NtSetInformationProcess` `NtQueryVirtualMemory` `LdrpUnmapModule`. 이미 있던 `.asm` (`RtlLookupFunctionEntry` `RtlpxLookupFunctionTable` `LdrpProcessMappedModule` `LdrpInitializeNtdllDataTableEntry` `LdrpInitializeProcess` `_LdrpInitialize`) 은 다시 안 뽑고 읽기만.
  - ntoskrnl: `NtQueryVirtualMemory` `MmQueryVirtualMemory` `MiQueryImageExtensionInformation` (요청서 대상) + 길 따라 더한 것: `MiGetImageExtensionBaseAddress` `MmInitializeImageViewExtension` `MmInitializeImageViewExtensionCfg` `MmGetScpCfgFunctionOffset` `MiMapImageScpCfgPages` `MiMapAllImageScpPages` `MiMapViewOfImageSection` `PsGetScpCfgPageTypeForProcess` `PsInitializeScpCfgPages` `PspFinalizeScpCfgPage` `MiObtainReferencedVadEx` `MiDoesImageContainFunctionOverrideFixups`. 넓힌 이유: 질문 2 의 "값의 출처" (누가 확장 쪽을 만들고 크기를 정하나) 가 한두 단계 아래에서 정해져서 지도로 그 함수만 골랐다.
  - 새 `_refs` (ntoskrnl): `0xe2d8e0` `0xe2d8f0` `0xe375ec` `MmInitializeImageViewExtension`.
- 버전 확인: `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x16b/+0x19c`, `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치 (`_calls.tsv`). 커널 GUID 일치. **일치.**
- 또 하나의 맞춰 보기 (VM 사진 `[관찰]` 과): ntdll 노드 `Flags` 사진값 `0x20a2c4` (`04_baggage-read_P1a.txt` 2795줄) 는 PE_emulator 값 `0xa2c4` 에 비트 21 (`0x200000`, PDB 이름 `ScpInExceptionTable`) 만 더한 값이다. `0xa2c4` 의 비트 하나하나가 아래 정적 줄과 맞는다 (질문 3 표). 즉 두 바이너리 읽기 (스크립트 오프셋 · 파일) 둘 다 믿어도 된다.

## 스크립트 문제

- 없음. 한 가지 참고: `MmQueryVirtualMemory.asm` 에서 클래스 `0xe` · `6` 몸통이 "같은 함수의 떨어진 범위" (`+0x274348 ~`, 주소 `0x140b10c58 ~`) 에 있다. WinDbg 콜스택이 이 자리를 `nt!MmQueryVirtualMemory+0x274764` 로 보여 줄지는 확인 전 `[추측]` — 그래서 아래 멈춤점은 `MiQueryImageExtensionInformation` 입구로 잡았다.

## 한눈에 (먼저 읽을 것)

1. **클래스 `0xe` 의 몸통은 `MiQueryImageExtensionInformation` 이 맞다.** `MmQueryVirtualMemory +0x274764/+0x274769` 에서 부른다.
2. **출력 0x18B = 입력/출력 겸용.** 칸 셋: `+0x0` 종류 (dword, ntdll 은 0 = `ScpCfg`), `+0x4` 예약 (dword, 0 이어야 함), `+0x8` **확장 시작 RVA** (qword, 출력), `+0x10` **확장 크기** (qword, 출력). 확장이 없으면 `+0x8`, `+0x10` 은 0 인 채 **성공(0)** 을 돌려준다.
3. **ntdll 이 읽는 갈래**: `RtlpInsertOrRemoveScpCfgFunctionTable +0x86 ~ +0x9d` 가 `+0x10` (크기) 이 0 이면, 또 확장 쪽 `+0x14` 의 dword 가 0 이면 `0x117` 로 끝낸다. 둘 다 아니면 `RtlAddGrowableFunctionTable` (넣기) 까지 간다.
4. **진짜 VM 은 그 길을 탔다 `[관찰]`**: 사진 P1a 의 ntdll `Flags = 0x20a2c4` (비트 21 켜짐) 와 ntdll 의 VAD 가 이미지보다 5쪽 길다는 것이 그 증거다. PE_emulator 가 준 "출력 칸 안 씀" 은 `+0x10 = 0` 갈래 → `0x117` 이라 길이 갈렸다.
5. **ntdll 에 줄 값 (정적으로 정해짐)**: `+0x8 = 0x267000` (ntdll `SizeOfImage`), `+0x10 = 0x1000`, 돌아온 길이 `0x18`, 상태 `0`. 확장 쪽 내용은 ntdll 안 구역 `SCPCFGNP` (0x2b0 바이트) 를 `0xcc` 로 0x1000 까지 채운 것 (test.exe 는 "Nop" 형 `[정적 추정]`, 칸 표 아래).

## 질문별 답 [정적]

### 질문 1. `RtlpInsertOrRemoveScpCfgFunctionTable` (ntdll rva `0xa7830`, 220B)

근거 파일: `ms/disassemble/ntdll/RtlpInsertOrRemoveScpCfgFunctionTable.asm` (+ `.c`).

**인자·반환**

| 항목 | 값 | 줄 |
|---|---|---|
| `RCX` | 이미지 시작 (`DllBase`). `RBX` 로 보관 | `+0x39 MOV RBX,RCX` · `+0x3c MOV RDX,RCX` |
| `RDX` | 안 씀 (덮어씀) | `+0x3c` |
| `R8B` | **1 = 넣기 / 0 = 빼기** (`DIL` 로 보관) | `+0x31 MOVZX EDI,R8B` · `+0xa3 TEST DIL,DIL` |
| 모듈 노드 | 안 받는다 | |
| 반환 `EAX` | `0` = 넣기/빼기 했음 · **`0x117`** = 할 게 없음 (양수) · 음수 = 실패 상태 그대로 | `+0x64 MOV EAX,0x117` · `+0xcd`/`+0xd9 XOR EAX,EAX` |
| 부르는 곳 | 넣기: `LdrpProcessMappedModule +0x19e/+0x1a3` (`+0x198 MOV R8B,1`). 빼기: `LdrpUnmapModule +0x30/+0x35` (`+0x2d XOR R8D,R8D`; `+0x20 TEST [RBX+0x68],0x200000` · `+0x27 JZ` — 비트 21 이 켜진 모듈만) | `.asm` 두 파일 |

**`NtQueryVirtualMemory` 인자 (`+0x58/+0x5d`)**

| 인자 | 값 | 줄 |
|---|---|---|
| 1 핸들 | `-1` | `+0x51 MOV RCX,-0x1` |
| 2 주소 | 이미지 시작 | `+0x3c MOV RDX,RCX` |
| 3 클래스 | `0xe` | `+0x43 MOV R8D,0xe` |
| 4 출력 버퍼 | 지역 `RSP+0x40` (0x18B: `+0x40`·`+0x48`·`+0x50` 세 qword). 부르기 전에 0 으로 채움 | `+0x1d LEA R9,[R11-0x28]` · `+0x28 MOVUPS [RSP+0x40]` · `+0x2d MOV [R11-0x18]` |
| 5 길이 | `0x18` | `+0x49 MOV [R11-0x48],0x18` |
| 6 돌아온 길이 | 지역 `RSP+0x30` (0 으로 시작) | `+0x24 MOV [R11-0x38],RAX` · `+0x3f MOV [R11-0x40],RAX` |

(`LdrpInitializeNtdllDataTableEntry +0x1df/+0x1e4` 의 `NtQueryVirtualMemory` 는 **다른 호출**이다: 클래스 `2` (`+0x1cd MOV R8D,2`), 인자 `R9 == 0` 일 때만 (`+0xb0 JZ +0x1b7`), `LdrpNtdllHotPatchContext` 안 칸 주소. `+0x119d` 첫 호출은 `R9 = &지역` 이라 안 탄다. PE_emulator 가 본 한 번 = 클래스 `0xe`, 여섯 인자 → `RtlpInsertOrRemoveScpCfgFunctionTable +0x58`, 돌아올 자리 `+0x5d`.)

**출력의 어느 칸을 읽어 어느 갈래로 가나**

| # | 조건 (줄) | 가는 곳 |
|---|---|---|
| 1 | `+0x5d CMP EAX,0xc00000bb` · `+0x62 JNZ +0x82` — 같으면 아래로 | `+0x64 MOV EAX,0x117` → `+0x69` (끝) = **할 것 없음** |
| 2 | `+0x82 TEST EAX,EAX` · `+0x84 JS +0x69` | 그 밖의 실패 상태를 **그대로 반환** |
| 3 | `+0x86 MOV R8,[RSP+0x50]` (**칸 `+0x10` 크기**) · `+0x8b TEST` · `+0x8e JZ +0x64` | 크기 0 → `0x117` |
| 4 | `+0x90 MOV RAX,[RSP+0x48]` (**칸 `+0x8` RVA**) · `+0x95 ADD RAX,RBX` · `+0x98 MOV ECX,[RAX+0x14]` (**확장 쪽 `+0x14` 의 dword**) · `+0x9b TEST` · `+0x9d JZ +0x64` | 0 → `0x117` |
| 5 | `+0x9f LEA RDX,[RAX+RCX]` (= 함수 표 주소) · `+0xa3 TEST DIL,DIL` · `+0xa6 JZ +0xd1` | `R8B == 0` 이면 **빼기** 갈래 (아래 7) |
| 6 | **넣기**: `+0xa8 ~ +0xbf` 인자 채움 · `+0xc4/+0xc9 CALL RtlAddGrowableFunctionTable` · `+0xcb JS +0x69` (실패 반환) · `+0xcd XOR EAX,EAX` · `+0xcf JMP +0x69` | 성공 `0` |
| 7 | **빼기**: `+0xd1 MOV RCX,RDX` · `+0xd4/+0xd9 CALL RtlDeleteFunctionTable` · `+0xd9 XOR EAX,EAX` · `+0xdb JMP +0x69` | `0` |

**`RtlAddGrowableFunctionTable` 에 넘기는 여섯 (넣기 갈래)**

| 인자 | 값 | 줄 | 값의 출처 |
|---|---|---|---|
| 1 표 핸들 칸 | `LEA RCX,[RSP+0x38]` | `+0xba` | 받은 핸들은 **버려진다** (이 함수가 다시 안 읽음) |
| 2 함수 표 | `이미지 시작 + 칸+0x8` 의 위치에서 `+0x14` 의 dword 만큼 더한 주소 (`RDX`) | `+0x98` · `+0x9f` | 클래스 `0xe` 출력 `+0x8` + **확장 쪽 `+0x14`** |
| 3 개수 | `1` (`R8D`) | `+0xb7 MOV R8D,R9D` (R9D = 1, `+0xac`) | 상수 |
| 4 최대 개수 | `1` (`R9D`) | `+0xac MOV R9D,1` | 상수 |
| 5 범위 시작 | `이미지 시작 + 칸+0x8` (`[RSP+0x20]`) | `+0xbf MOV [RSP+0x20],RAX` | 출력 `+0x8` |
| 6 범위 끝 | `범위 시작 + 칸+0x10` (`[RSP+0x28]`) | `+0xa8 LEA RCX,[RAX+R8]` · `+0xb2 MOV [RSP+0x28],RCX` | 출력 `+0x10` |

**상태값 실패 갈래 (호출한 쪽에서 본 모습)** `LdrpProcessMappedModule`:
- `0x117` (양수, 0 아님): `+0x1a3 MOV EDI,EAX` · `+0x1a7 JNZ +0x203` → `+0x203 CMP EAX,0xc0000141` · `+0x208 JZ +0x26e` 아니면 `+0x20a TEST` · `+0x20c JNS +0x1ad` → `+0x1ad XOR EDI,EDI` 성공으로 계속. **`+0x1a9 BTS [RSI],0x15` (비트 21) 는 건너뜀** — 그래서 PE_emulator 의 ntdll `Flags` 에 `0x200000` 이 없다.
- `0xc0000141` (`STATUS_INVALID_ADDRESS`): `+0x26e ~ +0x28b` 에서 `LdrpIsImageAddress` 로 다시 확인. 이미지 주소고 두 번째 값이 0 이면 `+0x28b JMP +0x1ad` (성공 취급), 아니면 `+0x20e` 로 나가 실패.
- 그 밖 음수: `+0x20e` 로 나가 **모듈 적재 실패**.

정적으로 닫힘: 위 전부. 동적 필요: 진짜 VM 에서 이 호출이 실제로 돌려준 버퍼 값 (질문 2 끝에 정리).

### 질문 2. ntoskrnl `NtQueryVirtualMemory` 클래스 `0xe` 길

근거 파일: `ms/disassemble/ntoskrnl/{NtQueryVirtualMemory,MmQueryVirtualMemory,MiQueryImageExtensionInformation}.asm`.

**2-1. 길 (맞다: `MiQueryImageExtensionInformation`)**

| 단계 | 줄 |
|---|---|
| `NtQueryVirtualMemory` (rva `0x89c8d0`, 42B) | `+0x20/+0x25 CALL MmQueryVirtualMemory` (7번째 인자·상태 칸을 스택에 옮겨 줌: `+0x4 ~ +0x1b`) |
| `MmQueryVirtualMemory` 클래스 분기 | `+0x16f CMP R14D,0xe` · `+0x173 JA +0x266` (→ `0xc0000003` 잘못된 클래스) · `+0x180 MOV EAX,[RCX+R14*4+0x89cbe0]` · `+0x18b JMP RAX` (RVA 표 15칸. 클래스 `0xe` 칸 = RVA `0x89cad0` = `+0x1c0`) |
| 클래스 `0xe` 입구 | `+0x1c0 ~ +0x1db` (길이·주소 검사, 아래 2-2) → `+0xce` 공통 |
| 사용자 모드 호출 검사 | `+0xe5 TEST R15B,R15B` · `+0xe8 JNZ +0x287` → `+0x295/+0x29a ProbeForWrite(버퍼, 길이, 8)`, 돌아올 길이 포인터 `[RSP+0x50]` 도 건드려 봄 (`+0x2a2 ~ +0x2b8`) |
| 주소 상한 | `+0xf3 MOV RCX,0x7ffffffeffff` · `+0xfd CMP R12,RCX` · `+0x100 JBE +0x274348` (아니면 `+0x106` `0xc000000d`) |
| 핸들 `-1` 이면 현재 프로세스 | `+0x274430 CMP [RSP+0x80],-1` · `+0x274439 JNZ +0x274459` (아니면 `ObpReferenceObjectByHandleWithTag` `+0x27449c`) |
| **부르는 곳** | `+0x274753 CMP R14D,0xe` · `+0x274759 MOV R9,RDX` (돌아올 길이 포인터) · `+0x27475c MOV R8,R12` (주소) · `+0x27475f MOV RCX,[RSP+0x68]` (사용자 버퍼) · `+0x274764/+0x274769 CALL MiQueryImageExtensionInformation` |

**2-2. 클래스 `0xe` 이름 · 들어오는 검사**

- **이름**: PDB 에 `MEMORY_INFORMATION_CLASS` 열거가 없다 (`llvm-pdbutil` 로 확인). 클래스 `0xe` 의 공개 이름은 `MemoryImageExtensionInformation` 으로 기억한다 `[추측: 외부 이름, PDB 로 못 정함]`. 출력 구조체 이름도 마찬가지 (`MEMORY_IMAGE_EXTENSION_INFORMATION` `[추측]`).
- **PDB 에 있는 이름**: 입력 `+0x0` 칸의 열거 `_MI_IMAGE_VIEW_EXTENSION_TYPE` = `MiImageViewExtensionScpCfg` 0 · `MiImageViewExtensionEmulatedScpCfg` 1 · `MiImageViewExtensionHPAT` 2 · `MiImageViewExtensionMax` 3.
- **길이 검사**: 길이 < `0x18` → `0xc0000004` (`+0x1c8 CMP R13,0x18` · `+0x1cc JC +0x143` → `+0x143 MOV EAX,0xc0000004`). 0x18 이상이면 통과 (쓰는 건 0x18).
- **주소 검사**: 0 → `0xc000000d` (`+0x1d2 TEST R12,R12` · `+0x1d5 JZ +0x106`). `0x7ffffffeffff` 초과 → `0xc000000d` (`+0xfd`/`+0x100` 아래).
- **돌아올 길이 포인터**: NULL 허용 (`MiQueryImageExtensionInformation +0x1ef TEST R14,R14` · `+0x1f2 JZ`). 아니면 `0x18` 을 씀 (`+0x1f4`).
- **종류(`+0x0`) 검사** (`MiQueryImageExtensionInformation`):

| `+0x0` 값 | 결과 | 줄 |
|---|---|---|
| `0` (ScpCfg) | 계속 | `+0xbc TEST EAX,EAX` · `+0xbe JNZ +0x229` 안 탐 |
| `1` (EmulatedScpCfg) | **`0xc00000bb`** (STATUS_NOT_SUPPORTED) | `+0x229 MOV R8,RDX` · `+0x22c CMP EAX,1` · `+0x22f JNZ +0xc9` 안 탐 → `+0x235 MOV EBX,0xc00000bb` |
| 그 밖 (2 `HPAT` 포함) | **`0xc000000d`** | `+0x22f JNZ +0xc9` → `+0xc9 CMP ECX,EDX` · `+0xcb JZ +0x212` → `+0x212 MOV EBX,0xc000000d` |
| `+0x4` 가 0 아님 | `0xc000000d` | `+0x72 MOV RAX,[RSP+0x28]` · `+0x77 SHR RAX,0x20` · `+0x7d JNZ +0x212` |

**2-3. 출력 `0x18`B 칸 표** (PE_emulator 가 nt 몸통을 지을 때 그대로 씀)

| 오프셋 | 크기 | 칸 이름 | 뜻 | 값의 출처 |
|---|---|---|---|---|
| `+0x00` | 4 | `ExtensionType` `[추측: 외부 이름]` (PDB 열거 `_MI_IMAGE_VIEW_EXTENSION_TYPE`) | 입력. ntdll 은 0 (= `MiImageViewExtensionScpCfg`) | 사용자 값을 그대로 되돌려 씀 (`+0x1dc ~ +0x1e1`: 처음 16 바이트를 지역에서 복사) |
| `+0x04` | 4 | `Flags` `[추측: 이름]` | 예약. **0 이어야** 함 | 같이 되돌려 씀 |
| `+0x08` | 8 | `ExtensionImageBaseRva` `[추측: 이름]` | 확장 시작 (이미지 시작 기준 RVA). 없으면 0 | `MiGetImageExtensionBaseAddress` = **VAD 시작 + `Segment.TotalNumberOfPtes << 12`** (`.asm` `+0x0 ~ +0x25`: `[VAD+0x18]`·`[VAD+0x20]` 시작쪽번호 · `[VAD+0x48]`→`[ ]`→`[ ]`→`+0x8`) 에 **표 `DAT[종류]`** (`+0x102 MOV ECX,[RAX+R8*4]`, ScpCfg 는 `R8 = 0`) 를 더하고 VAD 시작을 뺀 값 (`+0x1a1 SHL R8,0xc` · `+0x1a5 SUB R15,R8` · `+0x1a8 MOV [RSP+0x30],R15`) |
| `+0x10` | 8 | `ExtensionSize` `[추측: 이름]` | 확장 크기. 없으면 0 | 상수 **`0x1000`** (`+0x1ad MOV [RSP+0x38],0x1000`) |

- `DAT` = `nt` rva `0xe2d8e0` 의 dword 표 (`_refs/0xe2d8e0.tsv`). **파일에는 값이 없다 (.data 구역 파일 크기 밖)**. 쓰는 곳:
  - `[0]` (ScpCfg 오프셋): 쓰는 줄 하나 = `MmInitializeImageViewExtension +0x14 AND [0xe2d8e0],ECX` (`ECX == 0` 갈래). 그래서 **항상 0** `[정적]`.
  - `[2]` (HPAT 오프셋) = `0x1000` (`+0x2e`, `[0xe375ec] ≠ 0` 일 때). `[3]` = 확장 전체 크기 (`+0xa8` ← `[0xe2d8f0]`, 이 값은 첫 호출 `+0x1a ~ +0x20` 에서 `[0xe375ec] + 0x1000`). 부르는 곳: `MiInitSystem +0x1a6/+0x1a8` (`XOR ECX,ECX` — 첫 호출), `IoInitSystemPreDrivers +0x1123/+0x1126` (`MOV ECX,R13D`, `R13D = R12D + 1` `+0x1db` — 0 아닌 호출 `[추측: R12D 값은 안 봄]`).
- **돌려주는 조건 = 확장이 있을 때만 `+0x8`·`+0x10` 이 채워진다.** 줄 순서:

| 조건 (줄) | 확장 없음 (0, 0, 성공) / 있음 |
|---|---|
| 주소를 덮는 VAD 가 없음 `+0x91/+0x96 MiObtainReferencedVadEx` → `+0x9c JZ +0x219` | 실패: `0xc00000a0` (STATUS_MEMORY_NOT_ALLOCATED, 프로세스가 끝나는 중이면 `0xc000010a`) — `MiObtainReferencedVadEx +0x1e1 ~ +0x1ee`, 호출 뒤 `MOV EBX,[RSP+0x88]` (`+0x219`) |
| VAD 종류가 이미지가 아님 `+0xa2 ~ +0xab` (`([VAD+0x30] & 0x70) != 0x20`) → `+0x222` | 실패: **`0xc0000141`** (STATUS_INVALID_ADDRESS) |
| `[VAD+0x40]` 비트 5 (`_MMVAD_FLAGS2.ImageMappingExtended`) 꺼짐 `+0xdd ~ +0xe2 JZ +0x1c9` | **성공(0), `+0x8 = +0x10 = 0`** |
| 확장 쪽의 PTE (`+0x18a MOV RAX,[RBX]`) 가 0 `+0x190 JZ +0x1b6` | **성공(0), 둘 다 0** |
| 위 모두 통과 | **성공(0), `+0x8 = 확장 RVA`, `+0x10 = 0x1000`** (`+0x1a8`·`+0x1ad`) |

**2-4. test.exe 프로세스에서 ntdll 이미지에 줄 값**

| 칸 | 값 | 닫힘? |
|---|---|---|
| `+0x08` | **`0x267000`** = ntdll `SizeOfImage` (PE 헤더 0x267000, 쪽 수 `0x267` = `Segment.TotalNumberOfPtes` — test.exe 의 사진 `TotalNumberOfPtes 0x8c` = `SizeOfImage 0x8c000` 과 같은 규칙) + `DAT[0] = 0` | 정적으로 정해짐. 값 자체 직접 읽기는 동적 (아래) |
| `+0x10` | `0x1000` | 정적 |
| 돌아온 길이 | `0x18` | 정적 |
| 반환 | `0` | 정적 (확장 있음 갈래) |

- **진짜 VM 이 확장 있음 갈래였다는 `[관찰]` 증거** (`ms/logs/04_baggage-read_P1a.txt`):
  - ntdll 노드 `Flags : 0x20a2c4` + `ScpInExceptionTable : 0y1` (2795 · 2817줄). 이 비트는 `LdrpProcessMappedModule +0x1a9` 가 `RtlpInsertOrRemove…` 가 **정확히 0** 을 돌려줬을 때만 켠다 (`0x117` 이면 `+0x1ad` 로 건너뜀) → 클래스 `0xe` 가 `+0x10 ≠ 0`, `[확장+0x14] ≠ 0` 을 돌려줬고 `RtlAddGrowableFunctionTable` 도 성공했다.
  - ntdll VAD `7ffafb660 ~ 7ffafb8cb` (1551줄, 0x26c쪽) 인데 `lm` 의 이미지 끝은 `7ffa'fb8c7000` (59줄, 0x267쪽) → VAD 가 **5쪽(0x5000) 더 길다** = 확장 영역. `DAT[3] = 0x5000` 으로 보인다 (`= [0xe375ec] 0x4000 + 0x1000`, `[추측: 두 값 직접은 못 봄]`). 확장 영역 안은 앞에서부터 ScpCfg 쪽 0x1000 (`+0x267000`), 그 뒤 HPAT (`DAT[2] = 0x1000`).
  - test.exe 노드는 `Flags 0x22c4`, 비트 21 꺼짐 (2690 · 2712줄), test.exe VAD 는 `7ff61a370 ~ 7ff61a3fb` = 정확히 0x8c쪽 (확장 없음) — 확장이 붙으려면 `ImageExpansionCompatible` 이어야 한다 (아래 조건 중 하나).
- **확장이 붙는 조건 (정적)** `MiMapViewOfImageSection +0x172 ~ +0x1fa` (VAD 를 늘리고 `EDI = 0x20` = `ImageMappingExtended` 를 심음):
  - `[0xe2d8ec] ≠ 0` (`+0x172` · `+0x182 TEST EAX,EAX` · `+0x184 JZ`) — 부팅 때 정해지는 전역 `DAT[3]`.
  - `[이미지 정보 +0x4c] & 0x40` (`+0x186 TEST [R9+0x4c],0x40`) = `_MI_EXTRA_IMAGE_INFORMATION` (+0x40 에 붙은 칸) 의 `+0xc` 비트 6 = **`ImageExpansionCompatible`** (PDB 이름).
  - 뷰가 세그먼트 전체 (`+0x1b4 ~ +0x1c2`), 프로세스 쪽 플래그 둘 (`+0x1cd TEST [R13+0x7dc],4` · `+0x1d7 TEST [RBP+0xf8],0x10`), 시작 `≠ 0x200000` (`+0x199 CMP R8,0x200000`), 비트 10 꺼짐 (`+0x18e BT EBX,0xa`).
  - 그 다음 **확장 쪽의 PTE 를 실제로 놓는 곳** = `MiMapImageScpCfgPages` (`MiMapViewOfImageSection +0xb23 ~ +0xb39` · `MiMapAllImageScpPages`): `+0x3b TEST [[EPROCESS+0x410]+0x425],4` (작업집합 쪽 비트, 이름 PDB 로 못 정함) · `+0x72 PsGetScpCfgPageTypeForProcess` ≠ `4` · `MiDoesImageContainFunctionOverrideFixups` (이미지 로드 구성 사본 `+0x18 ≠ 0`) → `+0xd3 MiGetImageExtensionBaseAddress` 자리의 PTE 에 미리 만든 쪽 (`DAT_140e2d8f8[형]`) 을 놓음.
- **이 중 "`ImageExpansionCompatible` 이 ntdll 이미지에서 왜 1 인가" 와 `[EPROCESS...+0x425]` 비트가 언제 켜지나** 는 이번에 못 닫음 (`MiMapProcessExecutable` 가 부르는 `MiMapAllImageScpPages +0x1c OR byte ptr [RAX+0x425],4` 가 켠다 — 그 호출 조건은 안 따라감). 하지만 VM 결과 (위 `[관찰]`) 가 이미 확장 있음 갈래로 확정해 주므로 **값 결정에는 영향 없음**. 그래도 PE_emulator 가 "다른 이미지 (test.exe, kernel32 …) 에 줄 값" 이 필요해지면 `ImageExpansionCompatible` 이 이미지마다 다르다는 점은 알고 있어야 한다 (사진: kernel32·kernelbase 는 비트 21 켜짐, test.exe 는 꺼짐 — baggage.md 1527줄).

**2-5. 확장 쪽 (0x1000B) 안의 내용 — ntdll 이 읽는 `+0x14`** (PE_emulator 가 메모리를 만들 때)

- 쪽 하나가 ntdll 파일 안 구역 네 개 중 하나를 그대로 복사한 것이다. 복사 + `0xcc` 채움: `PsInitializeScpCfgPages` (`.c` 의 첫 번째 `for` — `RtlCopyMemory` 뒤 `memset(…,0xcc,0x1000 − 길이)`). 네 범위는 ntdll **내보내기 `RtlpScpCfgNtdllExports`** (rva `0x1746f0`, 0x68B) 의 앞 네 (시작, 끝) 쌍. 파일에서 읽은 값:

| 쪽 형 (PDB `_RTL_SCP_CFG_PAGE_TYPE`) | ntdll 구역 (rva, 크기) | 처음 네 부품 (`+0x40`·`+0xc0`·`+0x140`·`+0x1c0`) |
|---|---|---|
| 0 `RtlScpCfgPageTypeNop` | `SCPCFGNP` (`0x16e000`, 0x2b0) | `48 ff e0` (jmp rax) ×2, `c3` (ret) ×2 |
| 1 `…Default` | `SCPCFG` (`0x16c000`, 0x2b0) | `49 bb <8B 자리>` (mov r11, 값) … `48 ba <8B 자리>` … |
| 2 `…ExportSuppression` | `SCPCFGES` (`0x16f000`, 0x2b0) | 형 1 과 같은 꼴 |
| 3 `…Fptr` | `SCPCFGFP` (`0x16d000`, 0x2b0) | `49 bb <8B 자리> 4d 8b 1b 41 ff e3` ×4 |

- 네 구역 **머리 6 dword 와 뒤쪽 `+0x240 ~ +0x2b0` 은 모두 같다**: 머리 dword = `0x40, 0xc0, 0x140, 0x1c0, 0x240, 0x2a4`; **`+0x14` = `0x2a4`**; `+0x2a4` 의 12B 가 `IMAGE_RUNTIME_FUNCTION_ENTRY` = `{BeginAddress 0, EndAddress 0x280, UnwindData 0x298}`; `+0x298` 이 `UNWIND_INFO` (플래그 3, 처리기 RVA `0x280`). 즉 ntdll 이 만드는 함수 표는 **`FunctionTable = 확장+0x2a4`, 개수 1, 범위 `[확장, 확장+0x1000)`**.
- 형 1 · 2 · 3 쪽은 부품 안 8바이트 자리 네 곳 (`+0x42`, `+0xc2`, `+0x142`, `+0x1c2`) 을 `PspFinalizeScpCfgPage` 가 채워 넣는다 (형 0 은 아무것도 안 함: `param_2 == 0 → return 0`).
- **test.exe 의 ntdll 쪽 형은 0 (Nop)** `[정적 + 관찰 기대]`: `PsGetScpCfgPageTypeForProcess` — `R8B == 0` 이면 0 (`+0x1c JZ +0x38`), 작업집합 `+0x4a0 == 0 · == 전역` 이면 `AL = 1` (`+0xe ~ +0x17`·`+0x3c ~ +0x3f`), 프로세스 `MitigationFlags` (`+0x750`) 비트 0 (CFG 켜짐) 이 꺼져 있으면 0 (`+0x28 TEST AL,1` · `+0x2a JZ +0x38`). test.exe `+0x750 MitigationFlags : 0x20` (사진 290줄, 비트 0 꺼짐) 와 `LdrSystemDllInitBlock+0xb8 = 0` (baggage.md 674·742줄) 와 맞는다. 형 0 쪽 `+0x40` 이 `48 ff e0` 인지로 동적에서 가를 수 있다.

**2-6. 이미지가 아닌 주소 · 확장 없음일 때** — 위 2-3 표: 주소가 어느 VAD 도 안 덮음 `0xc00000a0` · 이미지 VAD 아님 `0xc0000141` · 이미지인데 확장 없음 = **성공 + 0, 0**. ntdll 은 이 가운데 "성공 + 0, 0" 과 `0xc00000bb` 를 `0x117` 로, 그 밖 음수는 그대로 실패로 다룬다 (질문 1).

정적으로 닫힘: 길, 이름 (PDB 열거 · 외부 이름 구분), 검사, 칸 표, 값 계산식, 쪽 내용, 형 0. 동적 필요: 실제 버퍼 값 직접 확인, 확장 쪽 `+0x40` 의 형 확인, `DAT[3]` 값.

### 질문 3. `LdrpCfgProcessLoadConfig` (ntdll rva `0xa4a70`, 1813B)

근거 파일: `ms/disassemble/ntdll/LdrpCfgProcessLoadConfig.asm`.

**인자**: `RCX` = 모듈 노드 (`RSI`), `RDX` = NT 헤더 (`R15`, `[RBP-0x61]`), `R8` = **로드 구성 디렉터리 포인터** (`RDI`, 없으면 0). 부르는 곳: `LdrpProcessMappedModule +0x145/+0x14a` (`+0x13a MOV R8,[RSP+0x88]` — `LdrInitSecurityCookie` 가 돌려준 포인터), `AvrfMiniLoadDll +0x869`. 반환 `0` = 성공 (`+0x454 XOR EAX,EAX`), 검증 실패 `0xc000003e` (`+0x567`).

**ntdll 노드가 타는 갈래 (test.exe, CFG 강제 아님) — 위에서 아래로**

| # | 조건 (줄) | 결과 |
|---|---|---|
| 1 | `+0x77 TEST RDI,RDI` · `+0x7a JZ +0x454` | 없으면 바로 `0` 반환. ntdll 은 있음 (`LoadConfig` 크기 `0x148`, PE 데이터 디렉터리 10) |
| 2 | `+0x80 CMP [RDI],0x94` · `+0x86 JC +0x454` | 크기 0x94 미만이면 반환. ntdll 은 통과 |
| 3 | `+0x8c ~ +0xa7`: `[RDI+0x4e]` (`DependentLoadFlags`, ntdll 파일 값 `0x800`) 를 `(0x7b \| (LdrpPolicyBits & 4)) << 8` 마스크로 걸러 `[RSI+0x118]` 에 저장 · `+0xad TEST EDX,ECX` · `+0xaf JNZ +0x67b` | ntdll 은 걸림 → `+0x67e/+0x683 LdrpIsModuleUnderSystem32` (경로 앞부분이 `LdrpSystem32` 로 시작하고 그 뒤가 `\` 이며 디렉터리 부분이 `LdrpSystem32.Length + 2` 보다 길어야 참: `.asm` `+0x22 JA`·`+0x3f`·`+0x5e RtlCompareUnicodeStrings`). **참이면** `+0x685 JNZ` 로 `+0x68b CMP RSI,[LdrpImageEntry]` · `+0x698 JNZ +0x6f1` → `+0x6f1 BTS EAX,8` (exe 면 `+0x69a BTS EAX,9`). **VM 사진 `[관찰]`: ntdll `DependentLoadFlags : 0x800`** (`04_baggage-read_P1a.txt` 2846줄) → 비트 8·9 가 안 켜졌다 = 이 함수는 ntdll 에서 거짓 (`+0x685 JZ +0xb5`). 왜 거짓인지 (`LdrpSystem32` 값 · 길이 비교) 는 이번에 안 따라감 |
| 4 | `+0xb5 TEST [RDI+0x90],0x1000` · `+0xbf JZ +0x16f` — `GuardFlags` 비트 `0x1000` (ntdll `GuardFlags = 0x10417500`, 켜짐) | **`+0xc5 OR [RSI+0x68],0x8000`** |
| 5 | `+0xcc TEST [RDI+0x90],0x2000` · `+0xd6 JZ +0x16f` (ntdll 켜짐) → `+0xe4 ~ +0xec RtlImageDirectoryEntryToData(…, 0xd, …)` (ret `+0xf1`) · `+0xf7 JZ +0x16f` | ntdll 은 **지연 가져오기 디렉터리 (13) 가 없다** (PE 디렉터리: 0·2·3·4·5·6·10 만) → `+0x16f` 로 건너뜀 |
| 6 | `+0x17c BT R15D,0x18` · `+0x181 JC +0x6a9` (`GuardFlags` 비트 24 — ntdll 꺼짐) | CastGuard 갈래 안 탐 |
| 7 | **`+0x187/+0x18c LdrControlFlowGuardEnforced`** · `+0x18e JZ +0x584` | test.exe 는 0 → `+0x584 ~ +0x58a` (`R12D = 0`) → `+0x387` |
| 8 | (CFG 강제일 때만) `+0x198 ~ +0x1b2`: NT 헤더 `DllCharacteristics` 비트 14 (`0x4000`, ntdll `0x4160` 켜짐) **그리고** `GuardFlags` 비트 8 (켜짐) 아니면 `+0x584` | |
| 9 | `+0x39b qsort` (원소 0개) · `+0x3a0 CMP RBX,R14` · `+0x3a9 TEST R12D,R12D` · `+0x3ac JZ +0x454` | **반환 `0`** |

→ **syscall 없음.** 세 `NtProtectVirtualMemory` 자리 각각:

| 자리 | 조건 | ntdll 노드 (test.exe) | 인자 |
|---|---|---|---|
| `+0x16a/+0x16f` | 위 표 4·5 를 모두 통과 + 지연 가져오기 디렉터리 있음. **CFG 강제와 무관** | **안 탐** (디렉터리 없음) | `(-1, &구역 시작, &구역 크기, 2 PAGE_READONLY, &이전)`, 앞에서 `+0x144/+0x149 LdrpMakePermanentImageCommit` 로 그 쪽들을 확정 |
| `+0x4be/+0x4c3` | 표 7 이 참 + 표 8 둘 다 켜짐, 포인터 칸 (로드 구성 `+0x70·+0x78·+0x118·+0x120·+0x128`, CastGuard 면 `+0x130`) 이 이미지 안 | **안 탐** | `(-1, &칸, &8, 4 PAGE_READWRITE, &이전)` |
| `+0x535/+0x53a` | 위 짝의 되돌리기 | **안 탐** | `(-1, &칸, &8, 이전 값, &…)` |

- **PE_emulator 의 "지도 3자리" 는 test.exe 의 ntdll 노드에서 하나도 안 난다.** (대신 `LdrProtectMrdata` 를 거친 `NtProtectVirtualMemory` 가 질문 4·5 에서 별도로 난다 — 아래 syscall 표.)
- 로드 구성에서 ntdll 파일 값 (PE 에서 읽음): `GuardCFCheckFunctionPointer = 0x1e7000`, `GuardCFDispatchFunctionPointer = 0x1e6000`, `+0x118 ~ +0x130` = `0x1e6008 · 0x1e6010 · 0x1e6018 · 0x1e6020`, `GuardFlags = 0x10417500` (켜진 것: `0x100 · 0x400 · 0x1000 · 0x2000 · 0x4000 · 0x10000 · 0x400000` + 앞쪽 크기 필드. 비트 이름은 SDK 기억 `[추측: 이름]`).
- **`Flags` 의 `0x8000` 을 켜는 곳**: **`LdrpCfgProcessLoadConfig +0xc5 OR dword ptr [RSI+0x68],0x8000`**. 이 함수 안. PDB 비트 이름 = **`ProtectDelayLoad`** (비트 15). 조건 = 표 1 ~ 4 (로드 구성 있음 · 크기 ≥ 0x94 · `GuardFlags & 0x1000`).
- **`0xa2c4` 의 비트를 켜는 곳 하나하나**:

| 비트 (PDB 이름) | 켜는 곳 |
|---|---|
| `0x4` ImageDll · `0x200` InExceptionTable | `LdrpInitializeNtdllDataTableEntry +0x93 OR [RBX+0x68],0x204` |
| `0x40` InLegacyLists · `0x80` InIndexes | `LdrpInitializeNtdllDataTableEntry +0xf2/+0xf7 LdrpInsertDataTableEntry` · `+0x111/+0x116 LdrpInsertModuleToIndex` 안으로 `[추측: 본문 안 읽음]` |
| `0x2000` LoadConfigProcessed | `LdrpProcessMappedModule +0x154 OR [RBX+0x68],EBP` (`EBP = 0x2000`, `+0xc2`) — `LdrpCfgProcessLoadConfig` 성공 직후 |
| **`0x8000` ProtectDelayLoad** | **`LdrpCfgProcessLoadConfig +0xc5`** |
| `0x200000` ScpInExceptionTable (VM 만) | `LdrpProcessMappedModule +0x1a9 BTS [RSI],0x15` |

  합 `0x4 + 0x40 + 0x80 + 0x200 + 0x2000 + 0x8000 = 0xa2c4` = PE_emulator 실측 ✓.
- **쓰는 전역** (ntdll rva): `__security_cookie` `0x1ea5a8`, `LdrpPolicyBits` `0x1cd7fc` (읽기, `+0x90`), `LdrpImageEntry` `0x1d2828` (읽기, `+0x68b`), 노드 칸 `+0x118` `DependentLoadFlags`·`+0x68` `Flags` (쓰기). CFG 갈래에서만: `LdrSystemDllInitBlock+0xf0·+0xf8·+0x100·+0x108` (rva `0x1ea540·0x1ea548·0x1ea550·0x1ea558`, 비교 대상) · 로드 구성 안 포인터 칸. run45 의 `LdrpGuardCheckIcallNoESFptr` · `LdrpGuardDispatchIcallNoESFptr` 는 **이 함수가 안 쓴다** — 이 함수가 부르는 `LdrpCfgCheckRoutineCallback` (`+0x0 ~ +0x4c`) · `LdrpCfgDispatchRoutineCallback` 이 로드 구성 포인터 칸에 `LdrSystemDllInitBlock+0xf0` (비었으면 `LdrpValidateUserCallTarget` rva `0x1244e0`) · `+0x100` (비었으면 `0x124580`) 를 넣는다. 그러니 run45 두 칸과는 이름만 닮았다 (그쪽은 `LdrpInitializeCfgScpHelpers`).
- **`+0x70c/+0x711` 간접 호출 후보**: 고리가 `+0x4e5 CMP RAX,R15` (= `LdrpCfgCheckRoutineCallback`) · `+0x571 CMP RAX,R13` (= `LdrpCfgDispatchRoutineCallback`) 를 먼저 가르고 둘 다 아니면 `+0x70c` — 배열에 넣는 세 부류 중 남는 것은 `+0x6d1 LEA RAX,[0x180114e30]` = **`LdrpCgRoutineCallback`** (25B, CastGuard 용: `GuardFlags & 0x1000000` 에 따라 `0x18015ed50` 또는 0 을 칸에 씀) 하나뿐 `[정적 좁힘]`. 이 함수는 CFG 강제 + CastGuard 일 때만 불리므로 test.exe 에선 안 탄다.
- `LdrControlFlowGuardEnforced` (`0xa6740`, 25B) 반환값: `LdrSystemDllInitBlock +0xb8` (qword, CFG 비트맵 주소; 커널이 씀) 이 **0 이면 0** (`+0x0 CMP [0x1ea508],0` · `+0x8 JZ +0x17`), 아니면 `(~dword[LdrSystemDllInitBlock+0x9c]) & 1` (`+0xa ~ +0x15`). test.exe 는 `+0xb8 = 0` (baggage.md 674 · 742줄) → **0**.

정적으로 닫힘: 위 전부 (파일 값 포함). 동적 필요: `LdrSystemDllInitBlock+0xb8` 가 실제로 0 인지 (baggage 는 사진 값으로 0).

### 질문 4. `RtlAddGrowableFunctionTable` (ntdll rva `0xa72e0`, 873B)

근거 파일: `ms/disassemble/ntdll/RtlAddGrowableFunctionTable.asm`, `LdrEnsureMrdataHeapExists.asm`, `LdrControlFlowGuardEnforced.asm`, `LdrProtectMrdata.asm`, `LdrpChangeMrdataProtection.asm`.

**인자·반환**
- 인자: `RCX` 핸들 칸 포인터 (`R15`) · `RDX` 함수 표 (`R14`) · `R8D` 개수 (`EBP`) · `R9D` 최대 개수 · 5번째 `[RSP+0x70]` 범위 시작 (`RDI`) · 6번째 `[RSP+0x78]` 범위 끝 (`RSI`).
- **핸들**: 새 항목 (`_DYNAMIC_FUNCTION_TABLE`, 0x88B) 주소를 `*RCX` 에 돌려줌 (`+0x2a4 MOV [R15],RBX`, 성공 때만, 락 풀고 `.mrdata` 닫은 뒤).
- 반환: `EAX = EDI` = `NtSetInformationProcess` 상태 (성공 `0`) (`+0xbc MOV EAX,EDI`). 항목 할당 실패 `0xc000009a` (`+0x324`). 개수 > 최대 개수 · 범위 시작 ≥ 끝 이면 **`__fastfail(5)`** (`+0x24 CMP R8D,R9D` · `+0x27 JA +0xd8` · `+0x3a JNC +0xd8` → `+0xd8 MOV ECX,5` · `+0xdd INT 0x29`). `LdrEnsureMrdataHeapExists` 실패면 그 값 (`+0x47 JS +0xc3`).

**처음 갈림 (요청서 가정 정정)**
- **`+0x40/+0x45 LdrEnsureMrdataHeapExists` 는 조건 없이 늘 부른다** (인자 검사 바로 뒤). CFG 를 확인하는 곳은 **그 안**이다: `LdrEnsureMrdataHeapExists +0xa/+0xf LdrControlFlowGuardEnforced` · `+0x11 JZ +0x1f` → `+0x1f XOR EAX,EAX` 로 **즉시 0 반환**. 힙이 이미 있어도 (`+0x13 MOV RAX,[LdrpMrdataHeap]` · `+0x1d JZ +0x2d` 안 탐) 같은 `+0x1f`.
  - 그래서 run60 의 `[추측]` ("`RtlAddGrowableFunctionTable` 이 `LdrEnsureMrdataHeapExists` 를 안 타는 갈래") 는 **틀렸다**. 타긴 타는데 안에서 바로 나온다 (test.exe CFG 꺼짐). `LdrpMrdataHeap = 0` 도, `EPROCESS.InvertedFunctionTable ≠ 0` 도 같이 맞는다.
- `+0x4e/+0x53 LdrControlFlowGuardEnforced` · `+0x55 JZ +0xdf` (test.exe 는 0 → `+0xdf`). 참이면 `+0x5b ~ +0x8d`: `LdrpMrdataLock` 잡고 `LdrpMrdataHeapUnprotected` 가 가리키는 카운터를 올림 (0 이면 `RtlProtectHeap(힙, 0)` `+0x349`) — CFG 강제 갈래.
- `+0xdf/+0xe4` 다시 `LdrControlFlowGuardEnforced` · `+0xee JZ +0x20a` → **`+0x20a MOV RCX,GS:[0x60]` · `+0x213 MOV RCX,[RCX+0x30]` (= `PEB.ProcessHeap`) · `+0x217 JMP +0xfb`** — test.exe 는 이쪽. 참이면 `+0xf4 MOV RCX,[LdrpMrdataHeap]`.
- **할당**: `+0xfb/+0x100 RtlAllocateHeap(힙, 0, 0x88)` (`+0xe4 XOR EDX,EDX` · `+0xe6 MOV R8D,0x88`; 플래그 0 이라 **0 으로 안 채움**). 실패 `+0x106 JZ +0x2d6` → `0xc000009a`.

**새 항목 (`_DYNAMIC_FUNCTION_TABLE`, PDB 0x88B) 칸 표 — 이 함수가 채우는 것**

| 칸 | 이름 (PDB) | 값 | 줄 |
|---|---|---|---|
| `+0x00 / +0x08` | `ListEntry.Flink/Blink` | 목록 끝에 달 때 `Flink = &RtlpDynamicFunctionTable`, `Blink = 옛 끝` | `+0x27d MOV [RBX],RDX` · `+0x287 MOV [RBX+8],RAX` |
| `+0x10` | `FunctionTable` | 인자 2 | `+0x110 MOV [RAX+0x10],R14` |
| `+0x18` | `TimeStamp` | `NtQuerySystemTime(&항목+0x18)` — 실제로는 `RtlQuerySystemTime` = `[0x7ffe0014]` (KUSER_SHARED_DATA) 읽기, **syscall 아님** | `+0x10c LEA RCX,[RAX+0x18]` · `+0x117` |
| `+0x20` | `MinimumAddress` | 인자 5 (범위 시작) | `+0x11c` |
| `+0x28` | `MaximumAddress` | 인자 6 (범위 끝) | `+0x125` |
| `+0x30` | `BaseAddress` | 인자 5 (범위 시작) | `+0x12f` |
| `+0x38 / +0x40 / +0x48` | `Callback / Context / OutOfProcessCallbackDll` | **안 씀** (힙 값 그대로) | |
| `+0x50` | `Type` (`_FUNCTION_TABLE_TYPE`) | **3 = `RF_KERNEL_DYNAMIC`** | `+0x13a MOV [RBX+0x50],3` |
| `+0x54` | `EntryCount` | 인자 3 | `+0x114` |
| `+0x58` | `TreeNodeMin` (24B) | 작은 쪽 AVL 나무에 넣음 | `+0x19e LEA R9,[RBX+0x58]` |
| `+0x70` | `TreeNodeMax` (24B) | 큰 쪽 AVL 나무에 넣음 | `+0x1e0 LEA R9,[RBX+0x70]` |

**`NtSetInformationProcess` 앞뒤**
- `+0x146 LEA EDX,[R9+0x25]` (`R9D = 0x10`) → 클래스 **`0x35`**, `+0x133 MOV RCX,-1`, `R8 = RSP+0x20`, 길이 `0x10`. 16B 버퍼: `[RSP+0x20]` = **새 항목 주소** (`+0x141 MOV [RSP+0x20],RBX`), `[RSP+0x28]` 바이트 = **0** (`+0x14a MOV byte [RSP+0x28],0`; 0 = 넣기로 봄 `[추측]`), 나머지 0 (`+0x1f MOVUPS [RSP+0x20],XMM0`). 부름: `+0x14f/+0x154`. **조건 없이 늘 낸다** (항목을 얻은 뒤).
- 실패(`+0x158 JS +0x230`): 항목을 풀고 (`+0x249 RtlFreeHeap` / CFG 면 `LdrpMrdataHeap`) `EDI` 를 반환 — 목록에 안 달림.
- 성공: `+0x15e/+0x165 LdrProtectMrdata(0)` (`.mrdata` 열기) → `+0x16c RtlAcquireSRWLockExclusive(RtlpDynamicFunctionTableLock)` → 나무 둘 → 목록 → `+0x295 RtlReleaseSRWLockExclusive` → `+0x29f/+0x2a4 LdrProtectMrdata(1)` (닫기) → `+0x2a4` 핸들 저장 → `+0x2a7/+0x2ac LdrControlFlowGuardEnforced` · `+0x2ae JZ +0xbc` (test.exe) → 반환.

**쓰는 전역 (ntdll rva)**

| 이름 | rva | 뜻 / 쓰는 줄 |
|---|---|---|
| `RtlpDynamicFunctionTable` | `0x1e7060` (머리 16B: Flink · Blink, Blink 는 `0x1e7068`) | 항목 이중 연결 목록. `_LdrpInitialize +0x10d ~ +0x11b` 가 빈 목록으로 초기화. 여기서 끝에 달기 (`+0x1f0 ~ +0x201`: 끝이 머리를 가리키는지 검사 `CMP [RAX],RDX`, 아니면 `INT 0x29` 코드 3) → `+0x27d ~ +0x28e` |
| `RtlpDynamicFunctionTableTreeMin` · `…TreeMax` | `0x1e7070` · `0x1e7078` (뿌리 포인터) | AVL 나무 둘 (항목 `+0x58`, `+0x70` 의 노드). `+0x1a9/+0x1ae` · `+0x1eb/+0x1f0 RtlAvlInsertNodeEx` |
| `RtlpDynamicFunctionTableLock` | `0x1d4210` (SRW, 8B) | 위 셋을 감싸는 잠금. `_LdrpInitialize +0x122` 가 0 으로 |
| `RtlpDynamicCallbackTableTreeMin/Max` | `0x1e7058` / `0x1e7050` | 콜백형 표의 나무. 여기선 안 건드림, 찾는 쪽 (`RtlpLookupDynamicFunctionEntry`) 만 읽음 |
| `LdrpMrdataLock` · `LdrpMrdataHeap` · `LdrpMrdataHeapUnprotected` | `0x1d4de8` · `0x1e7010` · `0x1e7020` | CFG 강제일 때만 쓰임. test.exe 는 끝까지 0 (run60) |
| `LdrpMrdataUnprotected` (카운터) | `0x1e7128` | `LdrProtectMrdata` 가 쓴다. 파일 값 1 → 이 시점 0 (run60: `LdrpInitializeProcess +0x488` 닫기) |

**`LdrEnsureMrdataHeapExists` (rva `0xa716c`, 360B) — CFG 강제일 때 줄기 끝까지** (test.exe 는 위처럼 `+0x11 JZ` 로 끝)
- `+0x2d MOV RAX,[LdrpAllocationGranularity]` (rva `0x1e7038`) → 크기 칸 `[RSP+0x58]`.
- `+0x61/+0x66 NtAllocateVirtualMemory(-1, &기준(0), 0, &크기, 0x2000 MEM_RESERVE, 4 PAGE_READWRITE)` (`+0x59 MOV [RSP+0x20],0x2000` · `+0x4e MOV [RSP+0x28],EDI` (EDI = 4)).
- `+0x8c/+0x91 RtlpCreateHeap(2 (=`LEA ECX,[RDI-2]`), 예약한 영역, 0, 0, 0, 0, [RSP+0x30]=1)` (본문 이름만, run46 몫).
- `+0xa5/+0xaa RtlAllocateHeap(힙, 0, 4)` (4바이트 카운터 칸) · `+0xbd/+0xc2 RtlProtectHeap(힙, 1)` · `+0xc4/+0xc9 LdrProtectMrdata(0)` · 락 · `LdrpMrdataHeap` 이 아직 0 이면 **`+0xe8 MOV [LdrpMrdataHeapUnprotected],RDI` (카운터 칸) · `+0xef MOV [LdrpMrdataHeap],RBX` (힙)** · 락 풀기 · `+0x100/+0x105 LdrProtectMrdata(1)` → 0 반환.
- 다른 스레드가 먼저 만들었으면 (`+0xe6 JNZ +0x10a`): 풀어서 되돌림 `RtlProtectHeap(힙,0)` `+0x11e` → `RtlFreeHeap` `+0x12b` → `RtlDestroyHeap` `+0x133` → `+0x14c/+0x151 NtFreeVirtualMemory(-1, …, 0x8000 MEM_RELEASE)`; 마지막 `+0x151 ~ +0x15f` 에서 `LdrpMrdataHeap` 이 있으면 0, 없으면 `0xc0000017`.

**ntdll 노드 길에서 이 함수까지 오면 syscall 이 몇 번·어떤 순서** (test.exe, 확장 있음; `LdrpInitializeProcess +0x119d` 시작) — 아래 "syscall 표" 에 한꺼번에.

정적으로 닫힘: 위 전부. 동적 필요: 목록·나무가 실제로 채워졌는지 (아래 멈춤점), `RtlAllocateHeap` 이 힙을 키우는지 (run46).

### 질문 5. `RtlInitializeHistoryTable` · `RtlpFunctionAddressTableEntry` · `RtlLookupFunctionEntry` · `RtlpLookupDynamicFunctionEntry`

근거 파일: 각 `.asm` (`ms/disassemble/ntdll/`).

**`RtlInitializeHistoryTable` (rva `0xf20bc`, 185B, 인자 없음)** — `LdrpInitializeProcess +0x1277/+0x127c` 에서 부름.
- `+0x17/+0x1c LdrProtectMrdata(0)` (열기) → 아래 고리 → `+0xb4` 꼬리 `LdrProtectMrdata(1)` (닫기, `ECX = 1` `+0x9a`).
- 고리 `i = 0 ..`: `+0x29/+0x2e RtlpFunctionAddressTableEntry(i)` → 0 이면 고리 끝 (`+0x2e TEST RAX` · `+0x31 JZ +0x9a`). `+0x33 LEA RCX,[0x18000f700]` · `+0x3a CMP RAX,RCX` (= `RtlRaiseException`) 이면 `+0x3f MOV [0x1e7355],BL` (= `GlobalHint` 칸 = 그 순번). `+0x50/+0x55 RtlLookupFunctionEntry(함수 주소, &이미지 시작 지역, NULL)` — **고리마다 한 번, 이름 6개 = 6번** (아래 표). 칸 채움: `+0x69 MOV [RSI+RDI*8+0x18],RCX` (이미지 시작) · `+0x6e MOV [RSI+RDI*8+0x20],RAX` (함수 항목) · `+0x73 ~ +0x8c` 아래쪽 한계 (`+0x8`) 는 최소, 위쪽 한계 (`+0x10`) 는 최대로 갱신 · `+0x95 CMP EBX,0xc`.
- 끝: `+0x9f MOV [RtlpUnwindHistoryTable],EBX` (= **개수 6**).
- 쓰는 전역 **`RtlpUnwindHistoryTable`** (ntdll rva `0x1e7350`, `.mrdata`, 0xd8B = 머리 0x18 + 칸 12 × 16): `+0x0` 개수 (dword) · `+0x5` `GlobalHint` (byte) · `+0x8` 아래 한계 (파일 값 `0xffffffffffffffff`) · `+0x10` 위 한계 (파일 값 0) · `+0x18 + 16·i` 이미지 시작 · `+0x20 + 16·i` 함수 항목 포인터. (이 표의 `_UNWIND_HISTORY_TABLE` 타입은 PDB 에 없음 — 칸 이름은 위 줄에서 읽은 뜻 `[추측: 이름]`.)
- 반환 없음 (void). syscall: `LdrProtectMrdata` 둘 → `NtProtectVirtualMemory` 둘 (카운터 0 ↔ 1).

**`RtlpFunctionAddressTableEntry` (rva `0xf217c`, 86B)** — 인자 `RCX = 순번`, 반환 `RAX = 함수 주소` (정적 표, 읽는 전역 없음).

| 순번 | 줄 | 함수 (rva) |
|---|---|---|
| 0 | `+0x50` | `RtlUnwindEx` (`0x13380`) |
| 1 | `+0x2c` | `__C_specific_handler` (`0x124670`) |
| 2 | `+0x47` | `RtlpExecuteHandlerForException` (`0x1654f0`) |
| 3 | `+0x35` | `RtlDispatchException` (`0xcb730`) |
| 4 | `+0x3e` | `RtlRaiseException` (`0xf700`) → `GlobalHint = 4` |
| 5 | `+0x23` | `KiUserExceptionDispatcher` (`0x164e10`, 별칭 `KiUserExceptionDispatch`) |
| 6 이상 | `+0x59 XOR EAX,EAX` | 0 (끝) |

**`RtlLookupFunctionEntry` (rva `0x13010`, 863B)** — 인자 `RCX = ControlPc` (`RBX`), `RDX = *ImageBase 출력` (`RSI`), `R8 = 히스토리 표` (`RDI`, NULL 허용). 반환 `RAX = 12B 함수 항목 포인터` (또는 NULL).
- 히스토리 표 NULL (`+0x18 TEST R8,R8` · `+0x1b JNZ +0x13f` 안 탐): `+0x21 MOV RCX,[LdrpInvertedFunctionTables+0x18]` (ntdll 칸 0 의 이미지 시작) · `+0x37 CMP RBX,RCX` · `+0x3a JNC +0x1bf` — **ControlPc 가 ntdll 안이면 `+0x1bf ~ +0x1d1` 로 칸 0 을 그대로 복사해 `+0x50` 으로 바로 감** (`+0x1cb JNC +0x40` 은 ntdll 밖일 때). → **`RtlpxLookupFunctionTable` (`+0x48/+0x4d`) 를 안 부른다**, `NtQueryVirtualMemory` 도 안 낸다 `[정적, 칸 0 이 `LdrpInitializeProcess +0x44e ~ +0x467` 에서 채워져 있다는 run60 전제]`.
  - 여섯 함수 모두 ntdll 안이라 `RtlInitializeHistoryTable` 의 여섯 호출은 이 빠른 길.
  - ntdll 밖이면 `+0x40 ~ +0x4d RtlpxLookupFunctionTable(pc, &지역 24B)` → `+0x53 JZ +0x34c` (못 찾음) → `+0x34f/+0x352 RtlpLookupDynamicFunctionEntry(pc, &이미지 시작)` (ret `+0x357`) → `+0x357 JMP +0x104` 공통 끝.
  - **`RtlpLookupDynamicFunctionEntry` 로 가는 조건 = `RtlpxLookupFunctionTable` 이 못 찾았을 때 (NULL)** 단 하나 (`+0x53 JZ +0x34c`, `+0x34c ~ +0x352`).
- 이진 탐색: `+0x6f MUL RCX` (12B 칸 개수 = 크기/12) … `+0x9c ~ +0xff`.
- 하위: `RtlpxLookupFunctionTable` · `RtlpLookupDynamicFunctionEntry`. syscall 직접 없음.

**`RtlpxLookupFunctionTable` 를 `LdrInitState` 로 가르면** (`+0x16 CMP [LdrInitState],3` · `+0x2b JNZ +0x2a3`):
- `LdrInitState` (rva `0x1d23c8`) 값: `_LdrpInitialize +0x101` 이 **0** (`MOV [..],EBX`, `XOR EBX,EBX` 는 `+0x28`) → `LdrpInitializeProcess +0x1a4c` 가 1 → `+0x1b1e` 가 2 → `_LdrpInitialize +0x205` 가 3 (WOW64 길은 `LdrpInitializeProcess +0x17dd` 가 3). **`+0x1277` 시점 = 0.**
- `≠ 3` 이면 `+0x2a3` 의 **커널에 묻는 길** (클래스 6). `= 3` 이면 `LdrpInvertedFunctionTables` 이진 탐색 (`+0x8c ~ +0x101`).
- 그러나 위처럼 `RtlInitializeHistoryTable` 의 여섯 호출은 `RtlLookupFunctionEntry` 의 칸 0 빠른 길이라 여기를 **안 거친다.** 이 길을 타는 곳은 `LdrpInitializeProcess +0x43a` (ntdll 칸 0 을 처음 채울 때, run60).

**`RtlpLookupDynamicFunctionEntry` (rva `0xe3680`, 660B)** — 인자 `RCX = pc` (`RBX`), `RDX = 이미지 시작 출력` (`R14`). 반환 `RAX = 항목 포인터` 또는 NULL.
- `+0x11 ~ +0x18` `RtlAcquireSRWLockShared(RtlpDynamicFunctionTableLock)`. 찾는 순서: 콜백 나무 Min (`+0x1d`, `0x1e7058`) → 콜백 나무 Max (`+0x56`, `0x1e7050`) → 함수 나무 Min (`+0x7d`, `0x1e7070`) → 함수 나무 Max (`+0xb2`, `0x1e7078`). **연결 목록 (`0x1e7060`) 은 안 걷는다 — 나무 넷만.**
- 찾은 항목의 `Type` (`[항목+0x50]`, `+0xdf`): 0 `RF_SORTED` 또는 **3 `RF_KERNEL_DYNAMIC`** → `+0x18a` 이진 탐색 (`항목+0x54` 개수, `항목+0x10` 표, 기준 `항목+0x30`) ; 1 `RF_UNSORTED` → `+0x249` 선형 ; 2 `RF_CALLBACK` → 락 풀고 `+0x12a` 이미지 시작 저장 · **`+0x133/+0x138` 간접 호출 `CALL RAX` (대상 = `[항목+0x38]` 콜백, 인자 `RCX = pc`, `RDX = [항목+0x40]` 문맥; CFG 거침)**. 대상은 `RtlInstallFunctionTableCallback` 으로 등록한 값이라 동적.
- **네 나무가 모두 비었거나 못 찾으면**: `+0xd6 TEST RAX` · `+0xd9 JZ +0x220` → 락 풀기 (`+0x227`) · `+0x231 XOR EAX,EAX` → **NULL 반환, 이미지 시작 출력은 안 씀**. `_LdrpInitialize` 가 비워 둔 직후라 `+0x119d` 이전엔 나무 넷이 모두 0.
- syscall 없음 (락이 경합할 때의 대기는 `RtlAcquireSRWLockShared` 안쪽이라 이 함수 줄에는 직접 안 보임).

**`RtlpDynamicFunctionTable` 쪽 전역 정리** (4·5 합침)

| 전역 | rva | 읽는 곳 | 쓰는 곳 |
|---|---|---|---|
| `RtlpDynamicFunctionTable` (머리 16B) | `0x1e7060` | (이 요청 범위) | `_LdrpInitialize +0x10d ~ +0x11b` 초기화 · `RtlAddGrowableFunctionTable +0x1f0 ~ +0x28e` 끝에 달기 |
| `…TableTreeMin/Max` | `0x1e7070` / `0x1e7078` | `RtlpLookupDynamicFunctionEntry` | `_LdrpInitialize +0x129 · +0x130` 0 으로 · `RtlAvlInsertNodeEx` |
| `…CallbackTableTreeMin/Max` | `0x1e7058` / `0x1e7050` | 같음 | `_LdrpInitialize +0x137 · +0x13e` 0 으로 |
| `RtlpDynamicFunctionTableLock` | `0x1d4210` | 같음 (공유) | `_LdrpInitialize +0x122` 0 으로 · Add (독점) |

정적으로 닫힘: 위 전부. 동적 필요: 칸 0 이 실제로 채워진 값 (`LdrpInvertedFunctionTables` `0x1e7430`), `RtlpUnwindHistoryTable` 의 최종 칸 (6칸이 정말 차는지).

### 질문 6. `RtlpxLookupFunctionTable +0x2d5 ~ +0x303` — 클래스 6

근거: `ms/disassemble/ntdll/RtlpxLookupFunctionTable.asm`, `MmQueryVirtualMemory.asm`.

- **호출** (`+0x2a3 ~ +0x2d0`): `NtQueryVirtualMemory(-1, pc, 6, &버퍼 RSP+0x30, 0x18, NULL)` — `+0x2c7 LEA RCX,[RAX-1]` (`-1`) · `+0x2c3 LEA R8D,[RAX+6]` (클래스 6) · `+0x2b7 MOV [RSP+0x20],0x18` · `+0x2a5 MOV [RSP+0x28],R12` (돌아올 길이 **NULL**). 버퍼 0x18B 는 `+0x2aa ~ +0x2cb` (`XORPS` · `MOV [RSP+0x40],RAX` · `MOVUPS [RSP+0x30],XMM0`) 에서 0 으로.
- **클래스 6 이름·크기**: 클래스 6 = `MemoryImageInformation` `[추측: 외부 이름]`, 출력 0x18B (입력 길이 < 0x18 이면 `0xc0000004`, `MmQueryVirtualMemory +0x270 ~ +0x27c`). 커널 쪽 몸통: **별도 함수 없이 `MmQueryVirtualMemory` 안** (떨어진 범위 `+0x274a66 CMP R14D,6` ~ `+0x274c09`).
- **ntdll 이 읽는 칸 (줄)**:

| 출력 칸 | 읽는 줄 | 하는 일 |
|---|---|---|
| `+0x00` (qword, **이미지 시작**) | `+0x2dd MOV RDX,[RSP+0x30]` · `+0x2e2 TEST RDX,RDX` · `+0x2e5 JZ +0x487` ; `+0x300 CMP RSI,RDX` · `+0x303 JC +0x487` | 0 이거나 `pc < 이미지 시작` 이면 **못 찾음** (`+0x487 MOV [RBX+8],R12` 로 출력 0) ; 아니면 `+0x310 MOV [RBX+8],RDX` 로 이미지 시작 저장 |
| `+0x08` (**이미지 크기**, 낮은 dword 만) | `+0x309 MOV EAX,[RSP+0x38]` · `+0x30d MOV [RBX+0x10],EAX` | 출력 구조체 `+0x10` (이미지 크기) 에 저장 |
| `+0x10` (**이미지 플래그**) | `+0x2eb MOV RAX,[RSP+0x40]` · `+0x2f0 TEST AL,2` · `+0x2f2 JNZ +0x487` ; `+0x2f8 TEST AL,1` · `+0x2fa JNZ +0x487` | 비트 1 이나 비트 0 이 켜져 있으면 **못 찾음** |
| (반환 상태) | `+0x2d5 TEST EAX,EAX` · `+0x2d7 JS +0x487` | 음수면 못 찾음 |

  이어서 `+0x35b/+0x360 RtlImageNtHeaderEx` → NT 헤더 `+0xa0`·`+0xa4` (예외 디렉터리 RVA·크기, `+0x39c`·`+0x3aa`) 로 **함수 표 주소·크기** 를 만든다 (`[RBX+0x14]` 에 크기).
- **커널이 채우는 칸** (`MmQueryVirtualMemory +0x274a70 ~ +0x274bf0`):
  - 바탕 조건: `+0x274a74 BT EAX,0x15` · `+0x274a78 JC +0x274ba5` (`_MMVAD_FLAGS.PrivateMemory` 면 건너뜀) · `+0x274a7e AND AL,0x70` · `+0x274a80 CMP AL,0x20` · `+0x274a82 JNZ +0x274ba5` (이미지 VAD 아니면 건너뜀). 건너뛰면 **세 칸 모두 0 인 채 성공(0)** — ntdll 은 `+0x2e5 JZ` 로 못 찾음 처리.
  - `+0x00` 이미지 시작 = VAD 시작 (`[RSP+0xd0]`, `+0x274aae`) (큰 이미지 맵이면 `MiVadMapsLargeImage` 가 더함 `+0x274b15 ~ +0x274b39`).
  - `+0x08` 이미지 크기 = `Segment.SizeOfSegment` (`+0x274ac0 CMPXCHG.LOCK [Segment+0x18]` 로 읽음, `[RSP+0xd8]`). ntdll 은 `0x267000`.
  - `+0x10` 플래그 (`[RSP+0xe0]`): 비트 2 ~ 5 = `Segment` 쪽 바이트 `+0xf` 의 아래 4비트 (`+0x274ad1 ~ +0x274ae6` — 서명 수준 `[추측: 이름]`) · **비트 1** = VAD 보호 칸 `([VAD+0x30] & 0xf80) == 0x80` (`+0x274aff/+0x274b06`) · **비트 0** = 뷰가 세그먼트보다 짧음 (`+0x274b79 CMP RDX,RSI` · `+0x274b7e OR R8D,1`) · **비트 6** = `ImageMappingExtended` (`+0x274b87 MiReadVadFlags2` · `+0x274b8c TEST AL,0x20` · `+0x274b90 OR R8D,0x40`) `[추측: 비트 이름 — 외부 `ImagePartialMap` · `ImageNotExecutable` · `ImageExtensionPresent`]`.
  - 돌아온 길이 포인터 NULL 허용, 값은 `0x18` (`+0x274bce`·`+0x274c06`).
- **LdrInitState 로 길**: 위 질문 5 (`+0x1277` 에선 `RtlInitializeHistoryTable` 이 이 길을 안 탐; `LdrpInitializeProcess +0x43a` 의 칸 0 채우기에서만 탐).
- ntdll 이 `+0x2f0`/`+0x2f8` 에서 걸러내는 두 비트는 test.exe 의 ntdll VAD 에서 **꺼진다** (뷰가 세그먼트보다 긺 `0x26c ≥ 0x267`, 보호 칸이 읽기 전용 아님) `[정적 + 사진 1551줄]`.

정적으로 닫힘: 읽는 칸 · 뜻 · 커널 채우기. 동적 필요: 실제 값 (아래 멈춤점 4번).

## syscall 표 — ntdll 노드 길 (`LdrpInitializeProcess +0x119d` → `+0x127c`)

test.exe 의 ntdll 노드, **확장 있음 (진짜 VM 갈래)**. `[정적]` 줄 + 카운터 규칙 (run60) + `[관찰]` (비트 21).

| # | syscall | 클래스·인자 요점 | 부르는 자리 (call/ret) | 조건 | 횟수 |
|---|---|---|---|---|---|
| 1 | `NtQueryVirtualMemory` | 클래스 `0xe`, 길이 `0x18`, 돌아올 길이 포인터 있음 | `RtlpInsertOrRemoveScpCfgFunctionTable +0x58/+0x5d` | `LdrpProcessMappedModule +0x185 ~ +0x196` (비트 21 꺼짐, 인자 비트 23 꺼짐) | 1 |
| 2 | `NtSetInformationProcess` | 클래스 `0x35`, 길이 `0x10`, `{새 항목 주소, 0}` | `RtlAddGrowableFunctionTable +0x14f/+0x154` | #1 이 `+0x10 ≠ 0`, 확장 `+0x14 ≠ 0` ; 개수·범위 검사 통과 ; 항목 할당 성공 | 1 |
| 3 | `NtProtectVirtualMemory` (쓰기 켬) | `.mrdata` 전체, `PAGE_READWRITE` (4) | `RtlAddGrowableFunctionTable +0x15e/+0x165` → `LdrProtectMrdata(0)` → `LdrpChangeMrdataProtection(4)` (`+0xaf` → `+0x51/+0x56`) | #2 가 0 이상, `LdrpMrdataUnprotected == 0` (이 시점 0) | 1 |
| 4 | `NtProtectVirtualMemory` (되돌림) | `.mrdata`, `PAGE_READONLY` (2) | `+0x29f/+0x2a4` → `LdrProtectMrdata(1)` → `LdrpChangeMrdataProtection(2)` (`+0x68`) | 카운터 1 → 0 | 1 |
| 5 | `NtProtectVirtualMemory` (쓰기 켬) | 같음 (4) | `RtlInitializeHistoryTable +0x17/+0x1c` (`LdrpInitializeProcess +0x1277/+0x127c` 안) | 카운터 0 → 1 | 1 |
| 6 | `NtProtectVirtualMemory` (되돌림) | 같음 (2) | `RtlInitializeHistoryTable +0xb4` (꼬리 호출, 돌아올 자리 = `LdrpInitializeProcess +0x127c`) | 1 → 0 | 1 |

- #1 ~ #4 는 `LdrpInitializeProcess +0x119d/+0x11a2` 안, #5 · #6 은 `+0x1277/+0x127c` 안. 순서: #1 → #2 → #3 → #4 → (그 사이 `+0x11a2 ~ +0x1277` 은 이 요청 범위 밖) → #5 → #6.
- **안 나오는 것** (test.exe, 확인한 줄): `LdrpCfgProcessLoadConfig` 의 세 `NtProtectVirtualMemory` (질문 3 표) · `LdrEnsureMrdataHeapExists` 의 `NtAllocateVirtualMemory`·`NtFreeVirtualMemory` · `NtQuerySystemTime` (`RtlQuerySystemTime` 이 `[0x7ffe0014]` 읽기) · `RtlpxLookupFunctionTable` 의 클래스 6 (`RtlInitializeHistoryTable` 의 여섯 호출은 빠른 길) · `RtlAllocateHeap` 이 힙을 키우는 `NtAllocateVirtualMemory` (평소엔 안 남, run46).
- **확장 없음 (PE_emulator 의 지금 대답)**: #1 하나만 나가고 `0x117` 로 끝 — 요청서가 "덫 안 syscall 은 한 개뿐" 이라 한 것과 같은 갈래. 이때 `Flags` 비트 21 이 안 켜지고, `RtlpDynamicFunctionTable` 은 빈 목록, `EPROCESS.InvertedFunctionTable` 도 이 길로는 안 생긴다.
- **PE_emulator R4 (`LdrpMrdataUnprotected` 를 1 로 둠, 진짜 0)** 와의 관계: 진짜는 #3 ~ #6 이 각각 `NtProtectVirtualMemory` 를 낸다 (0 ↔ 1 을 넘으니까). 에뮬레이터가 1 이면 #3 · #5 는 2 로 올라가기만 하고 syscall 이 안 난다 (run60 규칙).
- **F7 (이 길이 `EPROCESS.InvertedFunctionTable` 을 만들었나)** 는 거의 닫힌다 `[정적 + 관찰]`: 비트 21 이 사진 P1a 에서 켜졌다 = #1 이 확장 있음 + `RtlAddGrowableFunctionTable` 이 0 을 반환 = #2 (`NtSetInformationProcess 0x35`) 가 성공했다는 뜻 (`+0xbc MOV EAX,EDI`, 실패면 `+0x230` 으로 빠져 항목이 안 달리고 음수 반환 → `+0x1a9` 도 안 돎). 그러면 nt 가 `RtlInsertDynamicUserFunctionTable +0x20c` 로 `InvertedFunctionTable` 을 만든 것 (run51). kernel32·kernelbase 도 비트 21 이 켜져 (baggage.md 1527줄) **같은 길을 세 번 (ntdll · kernel32 · kernelbase)** 탄 것으로 보인다 `[추측: 개수]`.

## PE_emulator 용 작은 표 — 함수 × (인자 · 쓰는 칸 · 하위 호출 · syscall · 반환)

| 함수 (rva · 크기) | 인자 | 쓰는 칸 (ntdll rva) | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|
| `RtlpInsertOrRemoveScpCfgFunctionTable` (`0xa7830` · 220) | `RCX` 이미지 시작 · `R8B` 1 넣기 / 0 빼기 | 지역 버퍼 0x18B (`RSP+0x40`) | `NtQueryVirtualMemory +0x58` · `__security_check_cookie +0x71` · `RtlAddGrowableFunctionTable +0xc4` · `RtlDeleteFunctionTable +0xd4` | 클래스 `0xe` ×1 | 0 / `0x117` / 음수 |
| `LdrpCfgProcessLoadConfig` (`0xa4a70` · 1813) | `RCX` 노드 · `RDX` NT 헤더 · `R8` 로드 구성 포인터 | 노드 `+0x68` Flags (`0x8000`) · `+0x118` | `memset +0x61` · `RtlImageDirectoryEntryToData +0xec` · `LdrpMakePermanentImageCommit +0x144` · `LdrControlFlowGuardEnforced +0x187` · `qsort +0x39b` · `LdrpIsModuleUnderSystem32 +0x67e` · (CFG 갈래) `LdrpCfgCheckRoutineCallback +0x4ee` · `LdrpCfgDispatchRoutineCallback +0x57a` · 간접 `+0x70c` | ntdll 노드: 없음 | 0 |
| `LdrControlFlowGuardEnforced` (`0xa6740` · 25) | 없음 | 읽기 `LdrSystemDllInitBlock` `0x1ea450`+`0xb8`·`0x9c` | 없음 | 없음 | 0/1 (test.exe 0) |
| `RtlAddGrowableFunctionTable` (`0xa72e0` · 873) | 핸들 칸 · 함수 표 · 개수 · 최대 · 범위 시작 · 범위 끝 | 쓰기 `RtlpDynamicFunctionTable` `0x1e7060` · `TreeMin/Max` `0x1e7070/78` ; 락 `0x1d4210` ; 새 항목 0x88B (프로세스 힙) | `LdrEnsureMrdataHeapExists +0x40` · `LdrControlFlowGuardEnforced` ×6 · `RtlAllocateHeap +0xfb` · `NtQuerySystemTime +0x117` · `NtSetInformationProcess +0x14f` · `LdrProtectMrdata +0x160/+0x29f` · `RtlAvlInsertNodeEx +0x1a9/+0x1eb` · 락 | `0x35` ×1 + `.mrdata` 열고 닫기 `NtProtectVirtualMemory` ×2 | 0 / 실패 상태 |
| `LdrEnsureMrdataHeapExists` (`0xa716c` · 360) | 없음 | (CFG 강제일 때) `LdrpMrdataHeap` `0x1e7010` · `…Unprotected` `0x1e7020` | `LdrControlFlowGuardEnforced +0xa` (test.exe 는 여기서 끝) · `NtAllocateVirtualMemory +0x61` · `RtlpCreateHeap +0x8c` · `RtlAllocateHeap +0xa5` · `RtlProtectHeap` · `LdrProtectMrdata` · `RtlDestroyHeap` · `NtFreeVirtualMemory +0x14c` | test.exe: 없음 | 0 |
| `RtlInitializeHistoryTable` (`0xf20bc` · 185) | 없음 | 쓰기 `RtlpUnwindHistoryTable` `0x1e7350` (개수 6 · `GlobalHint` 4 · 한계 둘 · 칸 6) | `LdrProtectMrdata +0x17/+0xb4` · `RtlpFunctionAddressTableEntry +0x29` ×7 · `RtlLookupFunctionEntry +0x50` ×6 | `NtProtectVirtualMemory` ×2 | void |
| `RtlpFunctionAddressTableEntry` (`0xf217c` · 86) | 순번 | 없음 | 없음 | 없음 | 함수 주소 (0 ~ 5) / 0 |
| `RtlLookupFunctionEntry` (`0x13010` · 863) | `RCX` pc · `RDX` 이미지 시작 출력 · `R8` 히스토리 표 | 읽기 `LdrpInvertedFunctionTables` `0x1e7430` (`+0x10 ~ +0x20` 칸 0) | `RtlpxLookupFunctionTable +0x48` · `RtlpLookupDynamicFunctionEntry +0x352` | ntdll 안 pc: 없음 | 항목 포인터 / NULL |
| `RtlpLookupDynamicFunctionEntry` (`0xe3680` · 660) | `RCX` pc · `RDX` 이미지 시작 출력 | 읽기 `…CallbackTree*` `0x1e7058/50` · `…FunctionTableTree*` `0x1e7070/78` · 락 `0x1d4210` (공유) | `RtlAcquireSRWLockShared +0x18` · 간접 `+0x133` (콜백형만) | 없음 | 항목 포인터 / NULL (나무가 비면 NULL) |
| `RtlpxLookupFunctionTable` (`0x14840` · 1339) | `RCX` pc · `RDX` 24B 출력 (`+0` 표 · `+8` 이미지 시작 · `+0x10` 이미지 크기 · `+0x14` 표 크기) | 읽기 `LdrInitState` `0x1d23c8` · `LdrpInvertedFunctionTables` · 락 `0x1ce418` | `NtQueryVirtualMemory +0x2d0` (`LdrInitState ≠ 3`, 클래스 6) · `RtlImageNtHeaderEx +0x35b` · `RtlAddressInSectionTable +0x4a9` … | 클래스 `6` ×1 | 표 포인터 / NULL |
| 커널 `NtQueryVirtualMemory` 클래스 `0xe` (`MiQueryImageExtensionInformation` `0x48062c` · 541) | `RCX` 사용자 버퍼 · `R8` 주소 · `R9` 돌아올 길이 포인터 | 읽기 `nt` `0xe2d8e0` 표 · VAD · PTE | `MiObtainReferencedVadEx` · `MiGetImageExtensionBaseAddress` · `MiLockWorkingSetShared` · `MiMakeSystemAddressValid` 등 | — | 0 / `0xc000000d` / `0xc00000bb` / `0xc0000141` / `0xc00000a0` |

## 정적으로 닫힘 / 동적 필요 (질문별)

| 질문 | 정적으로 닫힘 | 동적 필요 |
|---|---|---|
| 1 | 인자 · 갈래 · 여섯 인자 출처 · 실패 갈래 · 반환 `0x117` | 진짜 버퍼 값 (`+0x5d` 에서) |
| 2 | 길 · 이름 구분 · 검사 · 칸 표 · 값 식 (`0x267000`, `0x1000`) · 쪽 내용 · 형 0 | 직접 값, 쪽 형 `+0x40`, `DAT[3]`, `ImageExpansionCompatible` 이 ntdll 에서 1 인 까닭 |
| 3 | 갈래 · `0x8000` 줄 · NtProtect 세 자리 (ntdll 은 셋 다 안 탐) | `LdrSystemDllInitBlock+0xb8 = 0` 직접 |
| 4 | `Ensure` 무조건 호출 · 항목 칸 · 목록·나무 · `0x35` 버퍼 · syscall 순서 | 목록·나무가 실제로 3개 항목 (ntdll·kernel32·kernelbase) 인지 |
| 5 | 6개 함수 · 빠른 길 · `LdrInitState` 값 · 동적 조회 조건 · 빈 나무 = NULL | `RtlpUnwindHistoryTable` 최종 값, 칸 0 값 |
| 6 | 읽는 칸 · 커널 채우기 | 실제 값 |

## 동적분석으로 확인해야 할 것 (Q104 VM 단계에 얹을 후보)

추천 멈춤점 (모두 test.exe 일 때만 멈추게 조건을 붙인다):

1. **`ntdll!RtlpInsertOrRemoveScpCfgFunctionTable+0x5d`** (NtQueryVirtualMemory 에서 돌아온 자리). 읽을 것: `r eax` (기대 0), `dq @rsp+0x40 L3` (기대 `0, 0x267000, 0x1000`), `dq @rsp+0x30 L1` (기대 0x18), `r rbx` (이미지 시작). 다음에 `db @rbx+0x267000+0x40 L3` (형 0 이면 `48 ff e0`), `dd @rbx+0x267000+0x14 L1` (기대 `0x2a4`), `dd @rbx+0x267000+0x2a4 L3` (기대 `0, 0x280, 0x298`).
2. **`nt!MiQueryImageExtensionInformation`** 입구와 `+0x1dc` (복사 직전). `dq @rsp+0x28 L3` 과 `r ebx`. 같이 `dd nt+0xe2d8e0 L8` (기대 `[0]=0, [2]=0x1000, [3]=0x5000?`), `dd nt+0xe375ec L1` (기대 `0x4000?`). 조건: 현재 프로세스 이름.
3. **`ntdll!RtlAddGrowableFunctionTable+0x154`** (NtSetInformationProcess 직후, `r edi`) 와 **`+0x2a4`** (핸들). 읽을 것: `dq ntdll+1e7060 L2` (목록 머리), `dq ntdll+1e7070 L2` (나무 둘), `dq ntdll+1e7010 L1` (`LdrpMrdataHeap`, 기대 0), `dq ntdll+1e7020 L1`. 그 뒤 `dt nt!_EPROCESS <프로세스> InvertedFunctionTable` (F7 닫기).
4. **`ntdll!RtlInitializeHistoryTable+0x9f`** (개수 저장 직전) : `dq ntdll+1e7350 L1b` (개수 6, `GlobalHint` 4). 같은 줄기에서 **`ntdll!RtlpxLookupFunctionTable+0x2d5`** 는 `LdrpInitializeProcess +0x127c` 까지 이 호출자 사슬에서 **안 걸려야** 한다 (빠른 길 확인).
5. 클래스 6 직접: `ntdll!RtlpxLookupFunctionTable+0x2d5` 첫 히트 (`LdrpInitializeProcess +0x43f` 자리): `dq @rsp+0x30 L3` (기대 `ntdll 시작, 0x267000, 플래그(비트 0·1 꺼짐)`).
6. 이 칸들이 한 번에 맞는지: `dt nt!_LDR_DATA_TABLE_ENTRY` ntdll 노드 `Flags` 가 `0x20a2c4` (F7 과 같이).

## 짐 지도 반영거리 (baggage.md)

- **3.11 `InvertedFunctionTable`**: 만드는 길 = `LdrpProcessMappedModule +0x19e → RtlpInsertOrRemoveScpCfgFunctionTable +0xc4 → RtlAddGrowableFunctionTable +0x14f NtSetInformationProcess(0x35)` 로 **탄 것으로 정적+관찰 근거가 모임** (비트 21). 앞 단계로 `NtQueryVirtualMemory(0xe)` 한 번이 추가됨.
- **3.9 `.mrdata` · `LdrpMrdataHeap`**: `LdrpMrdataHeap` 이 끝까지 0 인 까닭 = `LdrEnsureMrdataHeapExists +0x11 JZ` (CFG 꺼짐) 이고 `Add` 는 `PEB.ProcessHeap` 갈래 (`+0xee JZ +0x20a`). run60 의 "조건부 ②" 설명을 이쪽으로 바꿀 만하다.
- **새 전역 줄 후보**: `RtlpDynamicFunctionTable` `0x1e7060` (16B) · `…TableTreeMin/Max` `0x1e7070` / `0x1e7078` · `…CallbackTableTreeMin/Max` `0x1e7058` / `0x1e7050` · `RtlpDynamicFunctionTableLock` `0x1d4210` · `RtlpUnwindHistoryTable` `0x1e7350` (0xd8B) · `LdrpInvertedFunctionTables` `0x1e7430` (헤더 0x10 + 칸 0x18B 단위) · 쓰는 곳은 위 질문 4·5 표.
- **새 ntdll 짐 (프로세스 힙 블록)**: `RtlAddGrowableFunctionTable` 이 만든 0x88B 항목 (ntdll · kernel32 · kernelbase 몫, `[추측: 개수]`). `_LdrpInitialize +0x10d ~ +0x13e` 가 목록·나무·락을 비워 둠 (쓰는 줄이 더 늘어남).
- **새 커널 쪽 짐**: ntdll 뷰 뒤 5쪽 (ScpCfg 쪽 1 + HPAT 4) 의 PTE — 커널 전역 `nt` `0xe2d8e0` 표 (`MmInitializeImageViewExtension`) · `DAT_140e2d8f8[형]` (쪽 4개, `MmInitializeImageViewExtensionCfg`) 는 에뮬레이터 바깥 (커널 공간).
- **Flags 비트**: 노드 `+0x68` `Flags` 비트 15 `ProtectDelayLoad` ← `LdrpCfgProcessLoadConfig +0xc5`, 비트 21 `ScpInExceptionTable` ← `LdrpProcessMappedModule +0x1a9`, 비트 13 `LoadConfigProcessed` ← `LdrpProcessMappedModule +0x154`.

## 요청서와 다르게 나온 것 (정정)

1. `LdrEnsureMrdataHeapExists` 는 `RtlAddGrowableFunctionTable` 이 **무조건 부른다** (CFG 조건은 그 안).
2. `LdrpCfgProcessLoadConfig` 의 `NtProtectVirtualMemory` 세 자리는 ntdll 노드에서 모두 안 탄다. 그러나 `.mrdata` 열고 닫기로 `NtProtectVirtualMemory` 가 따로 네 번 (Add 둘 + History 둘) 난다.
3. 진짜 VM 은 클래스 `0xe` 에서 확장 있음 갈래였다. PE_emulator 의 "출력 칸 안 씀" 은 갈래가 달라서 `Flags` 가 `0xa2c4` 로 끝났다 (VM `0x20a2c4`).

## 판정

**분석성공.** 질문 1 · 2 (핵심) 는 코드 줄과 파일 값 (ntdll PE 구역·내보내기·로드 구성) 으로 닫았고 VM 사진 `[관찰]` 과도 맞는다. 질문 3 ~ 6 도 줄로 닫았다. 못 닫은 것은 "`ImageExpansionCompatible` 이 ntdll 이미지에서 왜 1 인가" 와 `EPROCESS` 작업집합 쪽 비트 `+0x425 & 4` 가 켜지는 정확한 조건 (값 결정엔 영향 없음, VM 결과가 이미 확정) 이고, 나머지는 위 "동적 필요" 표.

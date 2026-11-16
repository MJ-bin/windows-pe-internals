# 106 cet-context-small — 정적 분석 결과

- 요청서: `_workspace/sast/106_cet-context-small.md` (Q75, 2.10 `NtContinue` 길 CET 남은 정적)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid+age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음)
- analyze 는 다시 안 함 (`.gpr` + `_index.tsv` + `SetPdb:` 줄 다 있음). ntdll·sample dll 쪽 파일·프로젝트는 안 건드림.
- 이미 있던 `.asm` 은 다시 안 뽑고 그대로 읽음: `KeContextFromKframes`, `PspGetContextThreadInternal`, `PspGetSetContextInternal` + (읽기만) `RtlCopyContext`, `PspGetContext`, `PspGetSetContextSpecialApc`, `PspInitializeThunkContext`, `KiInitializeContextThread`, `KxContextToKframes`, `SwapContext`, `KePopulateContinuationContext`, `KeVerifyContextIpForUserCet`, `KiVerifyContextIpForUserCet`, `KeVerifyContextRecord`, `PspAllocateThread`.
- 새로 뽑은 함수 13개 (모두 depth 0, `^이름$`) → `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`:
  - 요청 대상: `RtlpCopyXStateChunk`, `RtlpCopyKernelCetChunk`, 조각 `FUN_1406f9e12`
  - 질문 3 부모 확인용: `PopFxIdleDevicesFromSx`, `RtlCreateEnclaveReturnFrame`
  - 질문 1 길이 요청서 가정과 달라서 (아래 "중요") 한 걸음 아래: `KiGetAllocatedXSaveArea`, `RtlXSaveS`, `RtlCopyXStateArea`, `PspSetupUserShadowStack`, `PspReserveAndCommitUserShadowStack` (이름만 보려던 것인데 값이 저장되는 줄을 확인하려고 읽음)
  - 질문 2: `KeVerifyContextXStateCetU` (5번째 칸에 값을 쓰는 쪽)
  - 플래그 비트 풀이: `RtlpValidateContextFlags` (`RtlpValidateContextFlags2` 로 가는 얇은 껍데기), `RtlpValidateContextFlags2`
- 버전 확인: `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 일치 (`_calls.tsv`).
- 임시 도구: `.pdata` 를 읽으려고 scratchpad 에 pefile 짧은 스크립트 하나 씀 (`.claude` 아님, 커밋 안 함).
- 스크립트 문제: 없음. 참고 하나: `.asm` 의 `WRUSSD dword ptr [RAX],R8D/R9D` 는 Ghidra 가 크기를 틀리게 보여 준 것이다. 바이트 (`66 4c 0f 38 f5 …`, REX.W) 로 보면 **`WRUSSQ` (8바이트)** 다 (`KePopulateContinuationContext +0x30`, `RtlCreateEnclaveReturnFrame +0x243f90`, objdump 로 확인).

## 중요: 요청서 가정과 다른 점 (먼저 읽기)

1. **시동 문맥 (`PspInitializeThunkContext`) 길에 `KeContextFromKframes` 는 없다.** `KeContextFromKframes` 를 부르는 곳은 `KiDispatchException +0x13d` 와 `KiInitializeUserApc +0x1fb` 둘뿐 (`_calls.tsv`). 시동 길은 `PspGetContextThreadInternal` → `PspGetSetContextSpecialApc` → `PspGetSetContextInternal` → **`PspGetContext`** 이다. 임시 문맥의 CET_U 칸은 `PspGetContext` 가 채운다.
2. **`+0x11c RtlpCopyXStateChunk` · `+0x1b3 RtlpCopyKernelCetChunk` 는 `RtlCopyContext` 의 오프셋**이다 (`PspGetContextThreadInternal` 의 오프셋이 아님). 둘 다 값을 만들지 않고 복사만 한다.
3. **조각 `FUN_1406f9e12` 는 `+0xf4` 에 `WRMSR` 이 없다.** 이 조각은 38바이트짜리 다른 함수 꼬리이고, sast/37 이 본 `nt+0x6f9f06` 은 같은 이름 조각이 아니라 그 뒤쪽 `RtlCreateEnclaveReturnFrame` 의 떨어진 조각이다 (질문 3).

## 질문별 답 [정적]

### 질문 1 (a) 시동 문맥 `0x10005b` 의 XState CET_U 칸 SSP 값은 어디서 오나

- **한 줄 답**: CET_U 칸 (16바이트, `[+0]=1`, `[+8]=SSP`) 의 SSP 는 **시동 스레드가 커널에서 `PspGetContext +0x33d RtlXSaveS` 의 `XSAVES` 로 그 순간 CPU 의 유저 SSP (MSR `0x6a7`) 를 읽어 담은 값 S0** 이다 (`thread+0x198` 이 0 일 때). 스레드 구조 칸에서 읽는 갈래는 `thread+0x198` 이 0 이 아닐 때 하나 더 있다. 그 뒤 `RtlCopyContext` 는 그 칸을 **바이트 그대로 복사만** 한다.
- 이름 없이 읽은 사슬 (처음부터):
  1. **값 S0 가 정해지는 곳 (스택 만들 때).** `PspAllocateThread +0x5b9~+0x5c0` 이 프로세스 `+0x754` bit 14 (`CetUserShadowStacks`) 를 보고 `+0xb3f BTS [thread+0x74],0x14` (`CetUserShadowStack`) → `+0x5ea` `PspSetupUserStack` 뒤 `+0x5fa TEST [thread+0x74],0x100000` / `+0x601 JNZ +0xb49` → `+0xb60` (ret `+0xb65`) **`PspSetupUserShadowStack`** (rva `0x904838`) → `+0xf6` (ret `+0xfb`) **`PspReserveAndCommitUserShadowStack`** (rva `0x904a0c`) → `+0x93` (ret `+0x98`) `MmAllocateUserStack`, `+0xdd`·`+0x11d` `ZwAllocateVirtualMemory`. 돌려주는 SSP 값은 `PspReserveAndCommitUserShadowStack +0x12c LEA RCX,[R14-0x1000]` · `+0x133 ADD RCX,RBX` · `+0x136 MOV [RAX],RCX` = **섀도 스택 시작 + 크기 − 0x1000** (R14 는 `MmAllocateUserStack` 이 돌려준 칸으로 크기로 읽음 `[추측]`). 이 값을 `PspSetupUserShadowStack` 이 스레드의 **초기 문맥** CET_U 칸에 쓴다: `+0x105~+0x111` 플래그 `0x100040` 이면 → `+0x121` (ret `+0x126`) `RtlLocateExtendedFeature(문맥+0x4d0, 0xb)` → `+0x132 BTS [XState 마스크],0xb` → `+0x13c~+0x140 [칸+8] = SSP` → `+0x144 [칸+0] = 1`. 섀도 스택 시작은 `+0x14f MOV [R14+0x40],RAX` 로 따로 저장, `+0x153 OR byte [R15],8`.
  2. **스레드 저장 영역에 옮김.** `PspAllocateThread +0x6f4` (ret `+0x6f9`) `KeInitThread` → `+0x2fc` (ret `+0x301`) `KiInitializeContextThread`. 그 안 `+0x2a8`·`+0x2af` 가 새 스레드 저장 영역 머리 (`[RDI+0x200]`, `[RDI+0x208]`) 에 bit 11 을 켜고, `+0x3a7~+0x3b5` 초기 문맥 플래그 `0x100040` 이면 `+0x3c6` (ret `+0x3cb`) `RtlLocateExtendedFeature` 로 초기 문맥의 칸을 찾고, `+0x3dc` (ret `+0x3e1`) `RtlLocateSupervisorFeature` 로 저장 영역의 CET_U 자리를 찾아 **`+0x3e1 MOVUPS XMM0,[R14]` · `+0x3e5 MOVDQU [RAX],XMM0`** (16바이트 복사). 또 `+0x233~+0x25b` 가 스레드 `+0x250` (`NpxState`) 에 `0x800` 을 켠다 (스레드 bit 20 일 때).
  3. **스레드가 처음 돌 때 MSR 로 올라감.** `SwapContext` 가 나가는 스레드는 `XSAVES [RDI+0x60]` (`+0xcf`), 들어오는 스레드는 `XRSTORS [RSI+0x60]` (조각 `FUN_1406b686f +0x1b1` = `SwapContext +0x4e0`, `nt+0x6b6a20`) 한다. 마스크에 `[RSI+0x250]` 이 들어간다. XSAVES/XRSTORS 가 CET_U 요소로 MSR `0x6a7` (`IA32_PL3_SSP`) 를 읽고 쓰는 것은 코드에 안 보이는 CPU 설명서 지식 `[추측]`.
  4. **시동 길에서 읽어 담기.** `PspInitializeThunkContext +0x168` (ret `+0x16d`) `PspGetContextThreadInternal(현재 스레드, 임시 버퍼 R13, 0, 1, 0)`. 현재 스레드이므로 `PspGetContextThreadInternal +0xe9 CMP R13,RDI` / `+0xec JNZ` 가 안 뛰고 `+0x11e` (ret `+0x123`) 로 `PspGetSetContextSpecialApc` 를 **바로** 부른다 → `PspGetSetContextSpecialApc +0x64` (ret `+0x69`) `PspGetSetContextInternal` → 읽기 갈래 `+0x2ee` (ret `+0x2f3`) `PspGetContext`.
  5. **`PspGetContext` 의 XState 갈래.** `+0x171~+0x17b` 플래그 `0x100040` 이면 `+0x2e6` 로: `+0x2e6 TEST byte [트랩+0x170],1` (유저에서 온 트랩 프레임) → `+0x2ef` GS:`[0x188]` (현재 스레드) → `+0x2fd MOVSXD R14,[문맥+0x4e0]` (`CONTEXT_EX` 의 XState.Offset) → `+0x304` (ret `+0x309`) `KiGetAllocatedXSaveArea` → `+0x309~+0x313` 문맥 안 XSAVE 영역 = `문맥 + 0x2d0 + XState.Offset` → `+0x319 JNZ +0x3e6`.
     - **`KiGetAllocatedXSaveArea` 가 0 (보통)**: `+0x31f~+0x339` 마스크 = (KUSER `+0x708` `EnabledUserVisibleSupervisorFeatures` | KUSER `+0x3d8` `EnabledFeatures`) & `~3` → `+0x33d` (ret `+0x342`) **`RtlXSaveS`**. `RtlXSaveS` 는 `+0x20 OR [영역+0x208],bit63` · `+0x29 XRSTORS [영역]` (RFBM 0) · `+0x2c AND R8,[영역+0x208]` (문맥 압축 마스크와 AND) · **`+0x3d XSAVES [영역]`** 이다 (KUSER `ControlFlags` bit 1 이 꺼져 있으면 `+0x46` `RtlXSave`). 곧 **그 순간의 CPU 상태가 문맥 영역에 담긴다**: CET_U 칸의 `[+8]` = 그 순간 MSR `0x6a7` 값 `[추측: CPU 설명서]`. `RDMSR` 줄은 이 길에 없다.
     - **0 이 아님**: `+0x3e6~+0x3f7` (ret `+0x3fc`) `RtlCopyXStateArea(문맥 영역, 마스크, 스레드의 할당 영역)` — 할당 영역은 `KiGetAllocatedXSaveArea` 가 `thread+0x198` (`_KTHREAD.XStateSave`) 의 `Prev` 사슬 끝에서 `[끝+0x28]` (`XStateContext.Area`) 로 돌려준다 (`KiGetAllocatedXSaveArea +0x0~+0x2e`). 이때 칸의 SSP 는 그 영역에 저장돼 있던 값이다.
  6. **복사.** `PspInitializeThunkContext +0x269` (ret `+0x26e`) 유저 스택 문맥 `RtlInitializeExtendedContext2(…, 0x800)` → `+0x280` (ret `+0x285`) `RtlCopyContext(유저 스택 문맥, 0x10005b, 임시 버퍼)`. `RtlCopyContext +0xdb TEST BPL,2` → `+0x10c`~`+0x11c` (ret `+0x121`) **`RtlpCopyXStateChunk`**. 이 함수 안에는 `RDMSR`·`XSAVE*`·`XRSTOR*`·GS 읽기가 **하나도 없다** (grep 0줄). 하는 일: `+0x18`·`+0x39` 두 쪽 XState 길이 읽기 → 둘 다 `0x40` 이상이어야 함 (`+0x43~+0x4e`, 아니면 `+0x2b3` `0x80000005`) → `+0x54`·`+0x62` 두 쪽 XState 영역 주소 (`Offset` + 기준) → `+0x83`·`+0x89` 원본 마스크 `& ~3` → KUSER 의 켜진 기능에 없는 비트면 `+0x2a9` `0xc000000d` → `+0xa2` 성분 번호 2 부터 63 까지 돌며 (`+0x1b1~+0x20a`) 원본·대상 칸 위치를 KUSER 표 (`+0x3f0`·`+0x3f4` 표준 배치 표, `+0x604` 압축 배치 표 — KUSER `0xfffff780000003f0`·`…3f4`·`…604`) 로 계산 → **`+0x277` (ret `+0x27c`) `RtlCopyMemory`** 로 성분마다 통째 복사 → `+0x28d BTR` 로 비트 지움. CET_U (성분 11) 칸도 다른 성분과 똑같이 복사된다. 값은 안 만들고 안 고친다.
  7. **`RtlpCopyKernelCetChunk` 는 시동 문맥 (`0x10005b`) 에서 안 불린다.** `RtlCopyContext +0xe1 TEST BPL,4` 의 bit 2 는 `RtlpValidateContextFlags2 +0xa1~+0xb9` 에서 **`플래그 & 0x100080 == 0x100080`** (`CONTEXT_KERNEL_CET`) 이고 `KiKernelCetEnabled` 일 때만 켜진다 (`0x10005b & 0x100080 = 0x100000`). 이 함수 자체는 `CONTEXT_EX` 의 `KernelCet` 칸 (`+0x18` 오프셋, `+0x1c` 길이) 이 양쪽 다 `0x18` 이상일 때 24바이트 (`MOVUPS` 16 + `MOVSD` 8, `+0x22~+0x32`) 를 복사하고 (`CL` 이 0 이면 안 복사, `+0x12~+0x18`), 모자라면 `+0x3a` `0x80000005`. **유저 CET_U 칸이 아니라 커널 CET (`KernelCet`) 칸**이다. (`KeContextFromKframes +0x4fb~+0x54d` 가 이 칸을 트랩 프레임 `+0xd8` `ShadowStackFrame` 에서 채우는 곳.)
  8. **그 뒤.** `PspInitializeThunkContext +0x295` (ret `+0x29a`) `KePopulateContinuationContext(원래 Rip)`: `+0xc` 스레드 bit 20 이면 `+0x17~+0x1c RDMSR 0x6a7` → `+0x25 SUB RAX,8` → `+0x30 WRUSSQ [RAX], Rip` → `+0x36 WRMSR`. 이 호출은 `RtlCopyContext` (`+0x280`) **다음**이라, 유저 스택 문맥 CET_U 칸의 SSP 는 **밀어 넣기 전 S0**, 유저 코드가 처음 도는 SSP 는 S0−8 이다 `[정적, 순서]`. `NtContinue` 가 끝나면 `KxContextToKframes +0x211 XRSTORS [R8]` (조건 `+0x193~+0x1a5`) 가 칸 값 S0 를 올린다 `[추측: XRSTORS 가 MSR 로 간다]`. 이전 sast/37 의 "`KiVerifyContextXStateCetUEnabled` 가 칸 SSP ≥ 현재 SSP" 와도 맞는다 (S0 ≥ S0−8).
- `KeContextFromKframes` (시동 길 아님, 예외·APC 길): `+0x186~+0x192` 플래그 `0x100040` → `+0x198 TEST [트랩+0x170],1` (유저) → `+0x1ab~+0x1b9` 영역 주소 → `+0x1bc MOV RCX,[스레드+0x198]` → 0 이면 `+0x1cc~+0x1f3` 마스크 후 `+0x207 OR [영역+0x208],bit63` · `+0x210 XRSTORS` · **`+0x225 XSAVES`** (`RtlXSaveS` 와 같은 일을 직접 풀어 씀; 압축 꺼짐이면 `+0x558` `RtlXSave`), 0 이 아니면 `+0x562~+0x5a1` 사슬 끝 `[+0x28]` 를 `RtlCopyXStateArea` 로 복사. 같은 두 갈래다.
- **지금 우리 (PE_emulator)**: **맞음 (비-CET 갈래).** `Src/nt/Process.cpp:1980` 이 `ContextFlags 0x10001b` 로 고정하고 "CET 갈래 안 탐 `[관찰] run44`", `Process.cpp:2006` 이 `KePopulateContinuationContext` 를 "안 함", `Src/nt/Continue.cpp:17` 이 XState 꼬리를 안 읽는다. 이 VM 시동 문맥은 `0x10001b` 로 관찰돼 (run44 P1) CET_U 칸이 아예 없다. 위 사슬 전체는 CET 스레드일 때만 실행되는 코드다.

### 질문 2 (b) `PspGetSetContextInternal` 이 5번째 칸을 쓰나

- **답: X (안 쓴다).** (`PspGetSetContextInternal` 자신의 인자는 3개 `RCX`·`RDX`·`R8` 뿐이다. 여기서 "5번째 칸" = run42 의 `KiVerifyContextIpForUserCet` 5번째 인자 = 그 칸을 가리키는 포인터, 곧 `KeVerifyContextIpForUserCet` 의 4번째 인자 (`R9`) 로 읽었다 — `KeVerifyContextIpForUserCet +0x5d MOV [RSP+0x20],R9`.)
- 근거 (`PspGetSetContextInternal.asm`, 칸 = `[RBP-0x78]` 지역 변수):
  - 처음 값은 진입 때 `RDX` (읽기=0 / 쓰기=1 인자) `+0x6c`, 읽기·쓰기 갈림길 `+0x2c0 CMP [RBP-0x78],R13` 에서 읽음. 이건 칸을 쓰기 전.
  - 쓰기 갈래: `+0x37d CMP byte [R14+0x58],1` → `+0x388 LEA R8,[RBP-0x78]` · `+0x38f MOV [RBP-0x78],0` · `+0x397` (ret `+0x39c`) `KeVerifyContextXStateCetU(스레드, 문맥, &칸)`. 그 안에서 **칸에 쓰는 것은 `KeVerifyContextXStateCetU +0x242~+0x24b RDMSR 0x6a7 → [RBX]`** (곧 지금 유저 SSP 를 칸에 넣음). 문맥에 CET_U 칸이 없으면 `+0x2c0~+0x2d4` 가 문맥에 칸을 만들고 `[칸]=1`, `[칸+8]=지금 SSP` 를 쓴다.
  - 그다음 `+0x3a4 TEST [R14+0x59],4` → `+0x564`: `+0x568 LEA R9,[RBP-0x78]` · `+0x56f LEA R8,[RBP-0x38]` (+`[RBP-0x38]=3`, `+0x57c`) · `+0x587` (ret `+0x58c`) `KeVerifyContextIpForUserCet`. 그 안 `KiVerifyContextIpForUserCet` 이 칸을 읽고 맞으면 `+0x12c` 에서 `SSP+8` 로 고친다 (sast/37).
  - **`+0x58c` 뒤에 `[RBP-0x78]` 을 읽는 줄이 없다.** `.asm` 에서 `RBP + -0x78` 이 나오는 줄은 `+0x6c`(쓰기, 인자 저장) · `+0x2c0`(읽기, 칸 쓰기 전) · `+0x388`·`+0x568`(주소만 넘김) · `+0x38f`(0 쓰기) · `+0x602`(읽기: `VslGetSetSecureContext` 갈래, `+0xa4 JNZ +0x5e6` 로 가는 별개 길이라 칸을 쓴 뒤에 오지 않음) 뿐. `+0x58e JNS +0x3af` 로 이어져 `PspSetContext (+0x3ea)` 나 조각 `PspSetContextState` 로 가는데 둘 다 이 칸을 인자로 받지 않는다 (`+0x3e0~+0x3e7` 인자 `RDI`·`RSI`·`[R14+0x78]`).
  - `KeVerifyContextRecord` 갈래 `+0x545~+0x552` (ret `+0x557`) 는 `R8=R9=0` 으로 불러서 (`+0x54f XOR R8D,R8D`, `+0x549 XOR R9D,R9D`) 안쪽에서 `KiVerifyContextIpForUserCet` 을 안 부른다 (`KeVerifyContextRecord +0xa8 TEST R14,R14 / +0xab JZ`).
  - 시동 길 (읽기 갈래, `[RBP-0x78]` = 0) 에서는 칸을 만지지 않는다 (`+0x37d` 쓰기 갈래로 안 감).
- **지금 우리 (PE_emulator)**: **맞음.** `Process.cpp:2006` 이 `PspSetContextThreadInternal → … → PspSetContext` 중간 사슬을 줄이고 `Continue.cpp:62` 가 `KiVerifyContextIpForUserCet` 을 "안 함" 이다. 이 칸은 죽은 값이라 넣을 것이 없다.

### 질문 3 (c) 조각 `FUN_1406f9e12` 가 어느 함수의 조각인가

- **답: `PopFxIdleDevicesFromSx` (rva `0x4b580c`) 의 꼬리 조각이다.** 그런데 sast/37 이 본 `WRUSSQ`+`WRMSR 0x6a7` 줄 (`nt+0x6f9f06`) 은 이 조각에 없다. 그 줄은 **`RtlCreateEnclaveReturnFrame` (rva `0x4b5f88`) 의 떨어진 조각 `nt+0x6f9e98~0x6f9f31`** 안에 있다.
- 근거:
  - `pdb_aliases.py --where ntoskrnl.exe 0x6f9e12` · `0x6f9f06` → "PDB 공개 심볼에 없음" (공개 PDB 는 조각에 이름을 안 준다).
  - `.pdata` (`RUNTIME_FUNCTION`) 를 읽음: `0x6f9e12~0x6f9e39` (0x27바이트) 은 `UNW_FLAG_CHAININFO` 로 부모 `0x4b580c~0x4b58e2` = `PopFxIdleDevicesFromSx` (`_index.tsv`). `0x6f9f06` 은 `0x6f9e98~0x6f9f31` (0x99바이트) 에 들고, 이것도 체인으로 부모 `0x4b5f88~0x4b6047` = `RtlCreateEnclaveReturnFrame`.
  - `PopFxIdleDevicesFromSx.asm`: `+0xc2 JZ 0x1406f9e12` 와 `+0xd1 JMP 0x1406f9e12` (`FRAGJMP`, 조각은 `PopFxIdleDevicesFromSx +0x244606~`). 조각 본문은 `KeAbPostRelease` (+0x3/+0x8) · `KeLeaveCriticalRegion` (+0x8/+0xd) 를 부르고 레지스터 복원 `RET` (`FUN_1406f9e12.asm`, 길이 `0x26`). **`RDMSR`/`WRMSR` 없음.** sast/37 의 `+0xf4` 는 Ghidra 가 이 조각을 `FUN_1406f9e12` 이름으로 앞쪽 가까운 함수로 잡은 것에서 바이트 거리만 센 값이다 (`0x6f9e12 + 0xf4 = 0x6f9f06`, 그 사이는 다른 `.pdata` 항목).
  - `RtlCreateEnclaveReturnFrame.asm`: 본체 `+0x1f JNZ 0x1406f9e98` (`+0x18 CMP word [RCX+0x170],DI` — DI=0x33, 곧 트랩 프레임 `SegCs`) 와 `+0x2c JNZ 0x1406f9e98` (`+0x25 CMP [RCX+0x168],R8` — 트랩 `Rip` 가 인자와 다름) 이 `+0x243f10` (= `0x6f9e98 − 0x4b5f88`) 로 뛰어든다. 그 조각이 `+0x243f10~+0x243f69` 에서 트랩 `Rsp` (`[RCX+0x180]`) − 0x50 에 유저 스택 프레임을 만들고 `+0x243f69` 에서 트랩 `Rsp` 를 그 값으로 바꾸고, 스택 인자 `[RSP+0x78]` 가 0 이 아니면 (`+0x243f77 CMP`, `+0x243f7c JZ`) **`+0x243f7e MOV ECX,0x6a7` · `+0x243f83 RDMSR` · `+0x243f8c SUB RAX,8` · `+0x243f90 WRUSSQ [RAX],R9` (R9 = 트랩 `Rip`) · `+0x243f9d WRMSR`** 를 하고 `+0x243f9f JMP 0x1404b5fc4` (본체 `+0x3c`) 로 돌아간다. 곧 엔클레이브 호출용 "돌아올 주소를 그림자 스택에 한 칸 밀어 넣기" 이고 `KePopulateContinuationContext` 와 같은 모양이다.
  - 부르는 곳: `RtlCreateEnclaveReturnFrame` 를 직접 `CALL` 하는 줄은 `_calls.tsv` 에 없다 (간접).
- sast/37·run42 의 "`KeUserModeCallback` 쪽 `[추측]`" 은 틀렸다. `NtContinue` 길·시동 길과 무관하다.
- **지금 우리 (PE_emulator)**: **해당 없음.** 엔클레이브 · 전원 관리 쪽이라 에뮬레이터가 안 가는 길 (`Src` 에 CET 구현 없음).

## 동적분석으로 확인해야 할 것

0. 위 질문 1 사슬은 **CET 스레드일 때만** 실행된다. 이 VM 시동 문맥은 `0x10001b` 로 관찰돼 (run44 P1) CET 스레드가 아니다. 그래서 아래는 **CET 켠 스레드가 있는 환경을 만날 때** 쓰는 목록이고 지금 우리 진행에는 급하지 않다.
1. **(Q57 과 겹침) `rdmsr 6a7` 한 칸 −8**: `PspInitializeThunkContext +0x295`/`+0x29a` (`nt+0x91097d`/`nt+0x910982`) 앞뒤. 이 정적 읽기로는 S0 → S0−8 이고 `RtlCopyContext` 가 그 앞이라 칸 값은 S0.
2. 이 결과로 새로 생기는 확인 (CET 스레드일 때): 같은 `rdmsr 6a7` 을 `PspInitializeThunkContext +0x168` 직전 (`nt+0x910850`) 에서도 보고, 임시 문맥 CET_U 칸 `[칸+8]` 과 같은지 (S0 가 XSAVES 로 읽힌 값인지).
3. `KiGetAllocatedXSaveArea` 반환값: `PspGetContext +0x309` (`nt+0x9b83d9`) 에서 `rax` (0 이면 XSAVES 갈래, 아니면 `RtlCopyXStateArea` 갈래). `thread+0x198` 값도 같이 (`dq @thread+0x198 L1`).
4. 처음 값 S0 의 출처 (CET 스레드일 때): `PspSetupUserShadowStack +0x144` (`nt+0x90497c`) 직후 `[rax]=1`, `[rax+8]` = S0 (`PspReserveAndCommitUserShadowStack +0x136`, `nt+0x904b42` 의 `rcx` 와 같은 값), `KiInitializeContextThread +0x3e5` (`nt+0x3e9f55`) 의 `xmm0` / `[rax]` 16바이트.
5. **WinDbg 이름**: `ln nt+0x6f9f06`, `ln nt+0x6f9e12` 로 디버거가 조각을 어떤 이름으로 보여 주는지 (`RtlCreateEnclaveReturnFrame+0x243f7e` 인지 다른 이름인지). 공개 PDB 에는 조각 이름이 없다. 실행이 필요 없는 확인이다.

## 다음 요청 후보

1. (낮음, CET 스레드 환경에서만) `PspAllocateThread` 의 조각 `+0x2caa12 RtlGetExtendedContextLength`, `+0x2caae1 RtlLocateExtendedFeature` 주변: CET 스레드의 초기 문맥에 CET_U 칸이 늘 있게 누가 보장하나. `KiInitializeContextThread +0x3e1` 은 `+0x3a7~+0x3b5` 에서 `0x100040` 이 없으면 `R14=0` 인 채로 `[R14]` 를 읽는 모양이라 (`R14` 는 `+0x2d` 에서 0) 앞에서 보장돼야 하는데 이번엔 안 읽음.
2. (낮음) `SwapContext` 의 `XRSTORS` 마스크 조건 (`[RSI+0x250]` 의 `0x800`, `BT RBP,0x26`) 을 전부 읽어, 새 스레드 첫 실행에서 CET_U 가 MSR 로 올라간다는 `[추측]` 을 코드로 닫기.
3. (선택) 위 1·2 가 필요 없으면 이 요청으로 Q75 는 닫아도 된다: (a) 답 있음, (b) X, (c) `RtlCreateEnclaveReturnFrame`.

## 판정

분석성공. 질문 3개 모두 코드 줄로 답했다. (a) 는 요청서 가정과 길이 달라서 (`KeContextFromKframes` 가 아니라 `PspGetContext`) 한 걸음 아래 함수들을 더 읽었고, 근거 줄은 위에 있다. 하드웨어 동작 (XSAVES/XRSTORS 가 MSR `0x6a7` 을 읽고 쓰는 것) 은 코드에 안 보이는 CPU 설명서 지식이라 `[추측]` 으로 표시했다. PE_emulator 는 이 VM 의 비-CET 갈래를 쓰므로 영향 없음.

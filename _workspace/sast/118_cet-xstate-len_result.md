# 118 cet-xstate-len — 정적 분석 결과 (Q105)

- 요청서: `_workspace/sast/118_cet-xstate-len.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준값과 같음.
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 있음, 이 모듈 Ghidra 프로세스 없음). ntdll·다른 DLL 은 건드리지 않았다.
- **새로 뽑은 함수: 2개**, 둘 다 depth 0.
  - `^RtlpGetEntireXStateAreaLength2$` (질문 1) → `ms/decomp/ntoskrnl/RtlpGetEntireXStateAreaLength2.c`, `ms/disassemble/ntoskrnl/RtlpGetEntireXStateAreaLength2.asm`
  - `^RtlpRemoveArchDisallowedXStateFeatures$` (58 B, 질문 2 숫자를 닫으려면 CET 비트가 걸러지는지 봐야 해서 추가) → 같은 폴더 `.c`·`.asm`
  - 두 함수 모두 안에서 부르는 함수가 없다 (`_calls.tsv` 줄 안 늘어남).
- **읽기만 한 것** (다시 안 뽑음): `RtlGetExtendedContextLength3.asm`, `RtlpValidateContextFlags2.asm`, `PspInitializeThunkContext.asm` (`+0x53 ~ +0xf0`), `RtlInitializeExtendedContext2.asm` (`+0x129 ~ +0x178`).
- **caller 전수용**: `run_ghidra.sh refs ntoskrnl.exe RtlpGetEntireXStateAreaLength2` → `ms/disassemble/ntoskrnl/_refs/RtlpGetEntireXStateAreaLength2.tsv` (참조 9줄). `.asm` 이 없는 caller 5곳은 `ms/bin` 의 ntoskrnl.exe 를 `objdump -d` 로 call 앞 0x40 바이트만 읽었다 (파일로 안 남김).
- **버전 확인: 일치.** `_calls.tsv` `NtCreateUserProcess call +0xc31 / ret +0xc36 → PspAllocateProcess` (알려진 사실과 같음). `Length3 +0xd1/+0xd6 → RtlpGetEntireXStateAreaLength2` 도 refs 의 `from_rva 0x4ee959 − 0x4ee888 = +0xd1` 과 같음. `_index.tsv` rva `0x4eea6c`, 크기 77 도 새로 뽑은 `.asm` 머리와 같음.

## 스크립트 문제

없다. `export` 2번, `refs` 1번 모두 정상 종료 (exit 0). 참고: `.c` 는 `uint RtlpGetEntireXStateAreaLength2(ulonglong param_1, longlong param_2)` 로 인자 둘을 잘 잡았지만 `.asm` 줄과 같이 봐야 한다 (아래).

## 한 줄 요약 (PE_emulator 가 그대로 옮겨 쓸 것)

> `[정적]` `RtlpGetEntireXStateAreaLength2(rcx = 기능 마스크, rdx = &SharedUserData->XState)` — XState 의 `ControlFlags` bit 1 (`CompactionEnabled`) 이 **꺼져 있으면 `XState.Size`**, **켜져 있으면** `0x240` 에서 시작해 마스크의 각 비트 `i` (2~63) 마다 (`AlignedFeatures` 비트가 켜졌으면 먼저 64 바이트 올림) `AllFeatures[i]` 를 더한 값을 돌려준다. CET 스레드의 맡긴 문맥 길이 = `0x32f + 이 값`. 이 VM 값으로는 **`0x57f`** (비-CET 는 `0x4ff`).

## 질문별 답 [정적]

### 1. `RtlpGetEntireXStateAreaLength2` 가 돌려주는 값의 식

- 답: 인자는 `rcx` = 마스크 (`R10` 에 보관), `rdx` = `_XSTATE_CONFIGURATION` 포인터. 읽는 칸과 계산은 아래와 같다 (`.asm` 77바이트 전부, `+0x0 ~ +0x4d`).

| 줄 | 하는 일 | XState 칸 (`pdb_struct.py _XSTATE_CONFIGURATION`) |
|---|---|---|
| `+0x0` `MOV EAX,[RDX+0x14]` | 제어 플래그 읽음 | `+0x14 ControlFlags` |
| `+0x6 MOV ECX,2`, `+0xb TEST CL,AL`, `+0xd JZ → +0x4a` | bit 1 이 **꺼져 있으면** `+0x4a` 로 | bit 1 = `CompactionEnabled` |
| `+0x4a MOV EAX,[RDX+0x10]`, `+0x4d RET` | **꺼진 갈래**: `XState.Size` 를 그대로 돌려줌 (마스크는 안 봄) | `+0x10 Size` |
| `+0xf MOV R9,[RDX+0x220]` | 정렬 비트 모음을 `R9` 에 | `+0x220 AlignedFeatures` |
| `+0x16 MOV EAX,0x240` | **켜진 갈래** 시작값 `0x240` | (코드 상수) |
| `+0x1b ADD RDX,0x234` | `RDX` 를 `AllFeatures[2]` 칸으로 (`0x22c + 2×4`) | `+0x22c AllFeatures[64]` (칸마다 4바이트) |
| `+0x22 ~ +0x28` | `R8 = 1 << i` (`i` = `ECX`, 2 부터) | |
| `+0x2b TEST R10,R8` → `+0x2e JZ → +0x3d` | 마스크에 `i` 가 없으면 건너뜀 | |
| `+0x30 TEST R9,R8` → `+0x33 JZ → +0x3b` | `AlignedFeatures` 에 `i` 가 있으면 `+0x35 ADD EAX,0x3f`, `+0x38 AND EAX,0xffffffc0` (64 올림) | |
| `+0x3b ADD EAX,[RDX]` | `AllFeatures[i]` 를 더함 | `+0x22c + 4i` |
| `+0x3d INC ECX`, `+0x3f ADD RDX,4`, `+0x43 CMP ECX,0x40`, `+0x46 JC → +0x22` | `i` 를 63 까지 반복 | |

  - 점프 목적지는 `목적지 − 0x1404eea6c` 로 다시 셈: `JZ 0x1404eeab6` → `+0x4a`, `JZ 0x1404eeaa9` → `+0x3d`, `JZ 0x1404eeaa7` → `+0x3b`, `JC 0x1404eea8e` → `+0x22`.
  - 이 함수가 읽는 XState 칸은 넷뿐이다: `Size` (`+0x10`), `ControlFlags` (`+0x14`), `AlignedFeatures` (`+0x220`), `AllFeatures[i]` (`+0x22c + 4i`). `EnabledFeatures`·`EnabledSupervisorFeatures`·`AllFeatureSize` 는 **이 함수가 읽지 않는다** (마스크를 거르는 건 caller, 아래 질문 3).
  - 식 (켜진 갈래): `F = 0x240`, `i = 2..63` 에서 `마스크 bit i` 이면 `{ AlignedFeatures bit i 이면 F = (F+0x3f) & ~0x3f ; F += AllFeatures[i] }`.
  - 근거 파일: `ms/disassemble/ntoskrnl/RtlpGetEntireXStateAreaLength2.asm` (위 줄들), `ms/decomp/ntoskrnl/RtlpGetEntireXStateAreaLength2.c` (같은 모양의 루프 — `0x240`, `0x3f`, `0xffffffc0`, `+0x220`, `+0x234`, `+0x14`, `+0x10`).
  - 이 식이 맞는지 이 VM 값으로 검산: 마스크 `0xe7 | 0x800 = 0x8e7` 를 넣으면 `0x240 + 0x100 + 0x40 + 0x200 + 0x400 + 0x10 = 0x990` 이고, 사진의 `AllFeatureSize (+0x228)` 도 `0x990`. 마스크 `0xe7` 만이면 `0x980` = 사진의 `Size`. (검산 값 둘 다 맞음)

### 2. CET 스레드의 맡긴 문맥 길이 (`0x32f + 값`) 를 이 VM 값으로 계산

입력이 어디서 오는지 (모두 `[정적]`, 줄은 아래):

- `ecx = 0x10005b`, `r8 = 0x800` — CET 스레드일 때. `PspInitializeThunkContext +0x69 MOV ECX,[RSI+0x74]`, `+0x6c AND ECX,0x100000` (`_KTHREAD +0x74` bit 20 = `CetUserShadowStack`, `pdb_struct.py`), `+0x72 ~ +0x79` `R14D = 0x800` (꺼지면 0), `+0x84 ~ +0x8d` `R12D = 0x40 + 0x10001b = 0x10005b` (꺼지면 `0x10001b`), `+0x98 MOV R8D,R14D`, `+0x9f MOV ECX,R12D`, `+0xa2` Length2 호출.
- `RtlpValidateContextFlags2` (`ecx = 0x10005b`): `+0x15 ~ +0x28` 에서 `0x100000` 으로 통과 → `+0x4d ~ +0x8e` `0x10005b & 0x100040 = 0x100040` 이라 `CL = 0` → `+0x90 CMP [RBX],0` (= `XState.EnabledFeatures`, 이 VM `0xe7` ≠ 0) → `+0x96 R10D = 3` → `+0x9c ~ +0xa7` `0x10005b & 0x100080 = 0x100000` ≠ `0x100080` 라 CET 커널 검사 (`+0xa9`) 를 안 탐 → `+0xc2 MOV [R11],R10D` → Length3 의 `[RSP+0x20] = 3`.
- Length3 (`RtlGetExtendedContextLength3.asm`): `+0x4d ~ +0x58` `EBX = 0x4d0`, `+0x78 R11D = 0x10`, `+0x7e EBX = 0x4f0`. `+0x81 TEST [RSP+0x20],2` 켜짐 → XState 블록 (`+0x88 ~ +0xdf`) 을 탐. `+0x88 ~ +0x92` `EBX = (0x4f0 − 1 + 0x10) & ~0xf = 0x4f0`. `+0x94 [RSI+0x14]` bit 1 (압축) 이 이 VM 은 켜짐 → `+0x9b ~ +0xb7` 마스크 `RBP = 0x800 & ([RSI+0x330] | [RSI] | 0x8000000000000000)`, `+0xc1` `RtlpRemoveArchDisallowedXStateFeatures(ecx = 0x10005b, &마스크)`. `+0xcb ~ +0xd1` 마스크를 `rcx`, `RSI` 를 `rdx` 로 Length2 부름. `+0xd6 SUB EBX,R11D`, `+0xd9 ADD EBX,0xfffffe40`, `+0xdf ADD EBX,EAX` → `EBX = 0x4f0 − 0x10 − 0x1c0 + F = 0x320 + F`. `+0xe1 TEST [RSP+0x20],4` 꺼짐 (`3 & 4 = 0`) → `+0xe9 CMOVZ EAX,EBX`, `+0xec DEC`, `+0xee ADD EAX,R11D` → `0x320 + F − 1 + 0x10 = 0x32f + F`. `+0xf1 MOV [R14],EAX`.
- 마스크 거르기 (`RtlpRemoveArchDisallowedXStateFeatures.asm`): `+0x0 BT ECX,0x10` 꺼짐 → `+0x15 BT ECX,0x14` 켜짐 → `+0x1b MOV RAX,0x4000000000060dff` → `+0x25 JMP 0x1402b5b8c` (= `0x1402b5b8c − 0x1402b5b7c = +0x10`) → `+0x10 AND [RDX],RAX`. `0x800 & 0x4000000000060dff = 0x800` (bit 11 은 `0xdff` 안에 있음) → 마스크 그대로 `0x800`.

이 VM 의 `KUSER_SHARED_DATA.XState` 값 (run44 사진 `ms/logs/04_baggage-read_supp_P1.txt` 의 `dq fffff78000000000 L0x150` 에서 `+0x3d8 ~ +0x71f` 를 읽음. K1·K2·K3·P0·P1·P1a·P1b·P2 여덟 사진 모두 840바이트가 **똑같다**):

| XState 칸 | 사진 값 | 읽은 자리 (KUSD 주소) |
|---|---|---|
| `EnabledFeatures` (+0x0) | `0xe7` (비트 0,1,2,5,6,7) | `+0x3d8` |
| `Size` (+0x10) / `ControlFlags` (+0x14) | `0x980` / `3` (bit 0 `OptimizedSave`=1, bit 1 `CompactionEnabled`=1) | `+0x3e8` 한 qword `00000003'00000980` |
| `EnabledSupervisorFeatures` (+0x218) | `0x800` | `+0x5f0` |
| `AlignedFeatures` (+0x220) | `0x0` | `+0x5f8` |
| `AllFeatureSize` (+0x228) | `0x990` | `+0x600` |
| `AllFeatures[2,5,6,7]` | `0x100, 0x40, 0x200, 0x400` | `+0x60c, +0x618, +0x61c, +0x620` |
| **`AllFeatures[11]`** (+0x22c + 0x2c = +0x258) | **`0x10`** (CET_U) | `+0x630` 하위 dword |
| `AllFeatures[12]` | `0x18` (CET_S) | `+0x634` |
| `EnabledUserVisibleSupervisorFeatures` (+0x330) | `0x800` | `+0x708` |
| `AllNonLargeFeatureSize` (+0x340) | `0x990` | `+0x718` |

계산 (CET 스레드, 이 VM):

1. 마스크 = `0x800 & (0x800 | 0xe7 | bit63) = 0x800` → 거르기 후도 `0x800`.
2. `F` = `0x240` (시작) → `i = 11` 에서 마스크 bit 11 켜짐, `AlignedFeatures` bit 11 꺼짐 (값 `0`) 이라 올림 없음 → `+ AllFeatures[11] = 0x10` → **`F = 0x250`**. (다른 `i` 는 마스크에 없어 건너뜀.)
3. 문맥 길이 = `0x32f + 0x250` = **`0x57f`** (= 1407 바이트).

나란히 (이 VM, `[정적]` 계산):

| | 비-CET 스레드 (test.exe 첫 스레드로 보임) | CET 스레드 |
|---|---|---|
| `ecx` / `r8` | `0x10001b` / `0` | `0x10005b` / `0x800` |
| Length3 의 XState 블록 | 건너뜀 (`[RSP+0x20] = 1`, bit 1 꺼짐) | 탐 (`[RSP+0x20] = 3`) |
| Length2 호출 | 없음 | `rcx = 0x800`, `rdx = &XState` → `eax = 0x250` |
| 최종 `*rdx` | **`0x4ff`** (`0x4f0 − 1 + 0x10`) | **`0x57f`** (`0x32f + 0x250`) |

- 차이 `0x57f − 0x4ff = 0x80` (= `F − 0x1d0`).
- 이 숫자가 기대는 가정 (다르면 값이 달라진다):
  - 이 VM 의 XState 가 위 값 (사진 8장 모두 같음). 특히 `ControlFlags` bit 1 켜짐, `AlignedFeatures = 0`, `AllFeatures[11] = 0x10`, `EnabledUserVisibleSupervisorFeatures` 에 bit 11.
  - 마스크에 bit 11 이 안 남으면 (`EnabledUserVisibleSupervisorFeatures` bit 11 꺼짐인 CPU) `F = 0x240` → `0x56f`.
  - `ControlFlags` bit 1 이 꺼져 있으면 마스크와 상관없이 `F = Size` → 이 VM 의 `Size = 0x980` 이면 `0xcaf`. (이 VM 은 켜져 있어서 해당 없음. 다른 CPU 로 옮길 때만 보면 됨.)
- `[관찰]` 은 아직 없다 (CET 스레드의 맡긴 문맥 길이를 디버거로 본 적 없음). `[정적]` 계산이다.

### 3. 이 함수를 부르는 자리 (caller 전수) 와 CET 갈래 조건 한 줄

caller 는 **8곳**, 모두 `UNCONDITIONAL_CALL` (refs 9줄 중 8줄. 나머지 1줄은 `from_rva 0x101264 (함수 밖) DATA` = `.pdata` 의 풀기 표 항목으로 보이는 데이터 참조 `[추측]`). 지도 `_nodes.tsv` 의 `callers_total = 8` 과 같음.

| caller | call_off | ret_off | 넘기는 것 / 돌아와서 | 근거 |
|---|---|---|---|---|
| `RtlGetExtendedContextLength3` | `+0xd1` | `+0xd6` | `rcx` = 걸러낸 마스크, `rdx` = `RSI` (= Length2 가 준 `0xFFFFF780000003D8`). 돌아와서 `−R11D −0x1c0 + eax` | `.asm` (질문 2) |
| `RtlInitializeExtendedContext2` | `+0x15f` | `+0x164` | `rcx` = `RBP` (`+0x12d ~ +0x145` 에서 `[0xfffff78000000708] \| [R8] \| bit63` 와 AND, `+0x14f` 로 거르기. `RBP` 는 호출 인자 `r9`, 시동 길에선 `PspInitializeThunkContext +0xe3 MOV R9,R14` = `0x800`), `rdx` = `R8` (`+0x11f` 에서 `0xfffff780000003d8`). 돌아와서 `+0x169 LEA R14D,[RAX−0x200]` (CET 는 `0x250 − 0x200 = 0x50`) 뒤 `memset`. 이 call 은 `+0xa3 TEST R15B,2` (`RtlpValidateContextFlags2` 가 낸 칸 bit 1) 가 켜진 `+0xa7 JNZ → +0x104` 길에서만 닿는다 | `RtlInitializeExtendedContext2.asm` |
| `RtlpGetEntireXStateAreaLength` | `+0xe` | `+0x13` | `rcx` 그대로, `rdx = 0xfffff780000003d8` 고정. 24바이트 감싸개 | `objdump` (`ms/decomp/…/_index.tsv`: rva `0x48d0c0`, 24 B) |
| `RtlpGetNonLegacyXStateAreaLength` | `+0x3b` | `+0x40` | 압축 켜짐이면 `rcx &= ([0x…708] \| [XState] \| bit63)` 먼저. 돌아와서 `eax − 0x200` | `objdump` (rva `0x3ed0cc`, 74 B) |
| `RtlpWalkFrameChain` | `+0xef6` | `+0xefb` | `rcx = r12`, `rdx = r8 = 0xfffff780000003d8`. 돌아와서 `eax + 0x320` | `objdump` (rva `0x2b6750`) |
| `RtlDispatchException` | `+0x3e7` | `+0x3ec` | `rcx = rsi`, `rdx = r15` (앞에서 정해짐, 안 따라감). 돌아와서 `eax + 0x320` | `objdump` (rva `0x2b4b20`) |
| `RtlUnwind` | `+0x127` | `+0x12c` | `rcx = rsi`, `rdx = 0xfffff780000003d8`. 돌아와서 `eax + 0x320` | `objdump` (rva `0x2b4530`) |
| `RtlUnwindEx` | `+0x630` | `+0x635` | `rcx = r15`, `rdx = 0xfffff780000003d8`. 돌아와서 `eax + 0x320` | `objdump` (rva `0x2b50b0`) |

- `+0x1c0`·`+0x320`: Length3 (`+0xd9 ADD EBX,0xfffffe40` → `0x4f0 − 0x10 − 0x1c0 = 0x320`) 와 위 네 곳의 `+0x320` 은 같은 기본값이다 (문맥 + 확장 문맥 머리를 합친 값으로 보이지만 이름은 `[추측]`).
- `RtlDispatchException`·`RtlUnwind`·`RtlUnwindEx` (확인한 것) 는 `[KUSD+0x3ec]` (= `XState.ControlFlags`) bit 1 이 **켜지면** 먼저 `RtlpRemoveArchDisallowedXStateFeatures` 로 마스크를 거른 뒤 같은 call 자리로 돌아온다. `RtlpWalkFrameChain` 도 압축 켜짐 갈래에서 `[R8] \| [0x…708]` 를 OR 한다 (끝까지는 안 따라감). 즉 **Length2 call 은 압축 켜짐·꺼짐 모두 거친다.**
- 시동 길 (`PspInitializeThunkContext`) 에서 이 함수로 닿는 코드는 둘이다: `RtlGetExtendedContextLength3 +0xd1` (`PspInitializeThunkContext +0xa2` 의 길이 묻기) 와 `RtlInitializeExtendedContext2 +0x15f` (`+0xf0` 의 문맥 채우기). **둘 다 `RtlpValidateContextFlags2` 가 낸 칸의 bit 1 (XState 요청) 이 켜질 때만 닿는다** (Length3 `+0x81/+0x86`, InitializeExtendedContext2 `+0xa3/+0xa7`). 비-CET (`0x10001b`) 는 이 비트가 꺼져서 둘 다 안 닿는다. 나머지 여섯 caller (`RtlpGetEntireXStateAreaLength`·`RtlpGetNonLegacyXStateAreaLength`·`RtlpWalkFrameChain`·`RtlDispatchException`·`RtlUnwind`·`RtlUnwindEx`) 는 문맥 크기 묻기·예외 풀기·스택 걷기 길이고 시동 문맥과 따로다 (누가 부르는지는 이번에 안 따라감).

**CET 갈래 조건 한 줄** `[정적]`: `PspInitializeThunkContext +0x69 ~ +0x6c` 의 `[KTHREAD+0x74] & 0x100000` (`CetUserShadowStack`) 이 켜지면 `ecx = 0x10005b`, `r8 = 0x800` 로 Length2 를 부르는 코드 (`+0xa2`) 가 있고 (`+0x72 ~ +0x8d`), Length3 의 XState 블록 (`+0x81 TEST [RSP+0x20],2`, 이 값은 `RtlpValidateContextFlags2` 가 `ecx & 0x100040 == 0x100040` 이고 `XState.EnabledFeatures ≠ 0` 이면 켬) 을 타서 `RtlpGetEntireXStateAreaLength2` 를 부르는 줄 (`+0xd1`) 에 닿는다. 꺼진 스레드는 이 블록을 건너뛰어 `0x4ff` 로 끝난다.

## 지금 우리 (PE_emulator) 맞나·틀리나

- 비-CET (`0x10001b`, `r8 = 0`) 만 쓰면 `0x4ff` 로 **맞고 해당 없음** (`RtlpGetEntireXStateAreaLength2` 를 아예 안 부른다). run55 와 어긋나지 않음.
- CET 스레드까지 흉내 낸다면 이 VM 값으로 `0x57f` (식은 위). CPU 값이라 PE_emulator 가 `_XSTATE_CONFIGURATION` 을 같은 값으로 채워야 같은 숫자가 나온다.

## 동적분석으로 확인해야 할 것

- **필수는 없다.** 질문 1~3 모두 정적으로 닫힌다.
- 선택 (CET 스레드가 실제로 생길 때만 의미 있음. test.exe 첫 스레드는 비-CET 로 보임):
  - `nt+0x4eea6c` (`RtlpGetEntireXStateAreaLength2` 입구) 에서 `r rcx` (= `0x800`), `r rdx` (= `fffff780`000003d8`) 를 보고, `nt+0x4ee95e` (Length3 `+0xd6`, 돌아온 직후) 에서 `r eax` = `0x250` 인지. CET 스레드가 없으면 이 멈춤점은 안 걸린다.
  - 사진을 새로 찍는다면 `dq fffff780`000003d8 L0x69` 한 줄로 XState 칸 확인 (이 VM 8장이 같으므로 값이 달라진 건 아님).
- "이 caller 들이 실행 때 불린다" 는 정적으로 말할 수 없다 (부르는 코드가 있다는 것만).

## 다음 요청 후보

- 없음 (Q105 닫힘). 굳이 한다면:
  - `RtlpWalkFrameChain`·`RtlDispatchException` 의 `rdx` (`r15`) 가 어디서 오는지 — 이번엔 안 따라감. 예외 길 (run81·82) 에서 필요해지면.
  - `KeVerifyContextXStateCetU` / `KiVerifyContextXStateCetUEnabled` (이미 `.asm` 있음) 와 이번 `AllFeatures[11] = 0x10` 의 짝 맞추기 (문맥에 맡긴 CET_U 칸 크기가 맞는지).

## 판정

**분석성공.** 질문 1 (식) 은 `.asm` 줄과 이 VM 값으로 검산 (`0x990`, `0x980`) 까지 맞았고, 질문 2 는 `0x57f` 를 입력 값 근거 (사진 `+0x3d8 ~ +0x71f`, 8장 동일) 와 함께 계산했고, 질문 3 은 caller 8곳 (+DATA 1곳) 과 CET 갈래 조건을 줄·오프셋으로 적었다. 단, 5곳의 호출 앞 문맥은 `.asm` 이 아니라 `objdump` 로 읽었다 (오프셋은 `refs` 와 `_index.tsv` 로 대조해 맞음).

# 49 context-length2 — 정적 분석 결과 (Q103)

- 요청서: `_workspace/sast/49_context-length2.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준값과 같음.
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 있음, Ghidra 프로세스 없음).
- **새로 뽑은 함수: 1개** — 정규식 `^RtlGetExtendedContextLength2$`, depth 0 → `ms/decomp/ntoskrnl/RtlGetExtendedContextLength2.c`, `ms/disassemble/ntoskrnl/RtlGetExtendedContextLength2.asm`. `_calls.tsv` 는 4,332 → 4,333줄 (`RtlGetExtendedContextLength2  +0xe  +0x13  RtlGetExtendedContextLength3` 한 줄이 늘어남. 지도 `_edges.tsv` 442줄과 같은 값).
- **읽기만 한 것** (다시 안 뽑음, 파일 시각 그대로): `RtlGetExtendedContextLength3.asm` (전체), `PspInitializeThunkContext.asm` (`+0x5c ~ +0xa7`), `RtlpValidateContextFlags2.asm` (+ `.c`, 이미 있던 것. 질문 3의 `[RSP+0x20]` 값 때문에 읽음 — 아래 "요청서 밖에서 본 것").
- **버전 확인: 일치.** `_calls.tsv` 대조: `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess` (알려진 사실과 같음). 커널 PDB GUID 기준값과 같음. 새로 뽑은 asm 의 rva (`0x495440`) 도 `_index.tsv` 와 같음.

## 스크립트 문제

없다. `run_ghidra.sh export` 한 번 정상 종료. Ghidra 시작 때 `Module manifest file error … Extensions/GhidraMCP/Module.manifest` 경고가 나오지만 (사용자 설정의 확장 모듈 문제) 결과에는 영향 없다.

참고 (스크립트 문제 아님): `.c` 는 `void RtlGetExtendedContextLength2(void)` 에 `RtlGetExtendedContextLength3();` 한 줄뿐이다. PDB 에 인자 정보가 없어 Ghidra 가 `rcx/rdx/r8/r9` 를 인자로 못 잡는다. **인자 근거는 `.asm` 만** 믿을 것.

## 한 줄 요약 (PE_emulator 가 그대로 옮겨 쓸 것)

> `[정적]` `RtlGetExtendedContextLength2(ecx, rdx, r8)` = `RtlGetExtendedContextLength3(ecx, rdx, r8, r9 = 0xFFFFF780000003D8)` 을 **CALL** 하고, 돌아온 `eax` 를 **그대로** 돌려준다. `r9` 는 상수 = `KUSER_SHARED_DATA`(커널 매핑 `0xFFFFF78000000000`) `+0x3d8` = `XState` (`_XSTATE_CONFIGURATION`). — `RtlGetExtendedContextLength2.asm` `+0x4`(r9 채움) `+0xe`(CALL) `+0x13 ~ +0x17`(돌아와서 스택만 정리하고 RET).

## 질문별 답 [정적]

### 1. `RtlGetExtendedContextLength2` 의 rva 는 `0x495440` 인가?

- 답: **맞다.** entry `0x140495440` = rva `0x495440`, 24바이트 (`+0x0 ~ +0x17`).
- 근거: `ms/disassemble/ntoskrnl/RtlGetExtendedContextLength2.asm` 머리 `entry=140495440  rva=0x495440`, 마지막 줄 `+0x17 RET` (끝 = 24바이트). `ms/decomp/ntoskrnl/_index.tsv` `RtlGetExtendedContextLength2  140495440  0x495440  24`. `pdb_aliases.py --where ntoskrnl.exe RtlGetExtendedContextLength2` → `rva=0x495440 [function]` (PDB 이름 그대로, Ghidra 가 다른 이름으로 잡은 것 아님).

### 2. Length2 가 Length3 에 무엇을 넘기나

전체 코드는 5줄이다 (`.asm` 전부):

| 줄 | 하는 일 |
|---|---|
| `+0x0 SUB RSP,0x28` | 그림자 공간(0x20) + 정렬(8) 만 잡음 |
| `+0x4 MOV R9,-0x87ffffffc28` | `r9` = 상수. 64비트로 읽으면 `0xFFFFF780000003D8` (10바이트 명령 `+0x4 ~ +0xd`) |
| `+0xe CALL 0x1404ee888` | `RtlGetExtendedContextLength3` (`_calls.tsv`: call `+0xe`, ret `+0x13`) |
| `+0x13 ADD RSP,0x28` | 스택 되돌림만 |
| `+0x17 RET` | `eax` 는 건드리지 않음 |

- **(a) `rcx`(ContextFlags), `rdx`(길이 받을 곳) 를 그대로 넘기나? → 예.** `+0x0 ~ +0xe` 사이에 `rcx`·`rdx` 를 쓰는(바꾸는) 명령이 없다 (스택 포인터와 `r9` 만 건드림).
- **(b) 셋째·넷째 인자**
  - 셋째 `r8` (Length3 의 FeatureMask): **호출자의 `r8` 그대로** — Length2 는 `r8` 에 쓰는 명령이 없다. `PspInitializeThunkContext +0x98 MOV R8D,R14D` 가 위쪽 32비트를 0 으로 채우므로 Length3 가 받는 `r8` 은 `0` 또는 `0x800`.
  - 넷째 `r9`: **상수 `0xFFFFF780000003D8`.** 호출자의 `r9` 는 쓰지 않고 덮어쓴다 (`+0x4`).
    - 이 값이 `&SharedUserData->XState` 인 근거: `KUSER_SHARED_DATA` 커널 매핑 `0xFFFFF78000000000` + `0x3d8`. `pdb_struct.py ntoskrnl.exe _KUSER_SHARED_DATA` → `+0x3d8 XState  _XSTATE_CONFIGURATION` (sizeof `0x348`).
    - Length3 가 이 값을 `RSI` 로 두고 `_XSTATE_CONFIGURATION` 칸으로 읽는다: `Length3 +0x25 MOV RSI,R9`, `+0x94 [RSI+0x14]` (`ControlFlags`), `+0x9b [RSI+0x330]` (`EnabledUserVisibleSupervisorFeatures`), `+0xa7 [RSI]` (`EnabledFeatures`), `+0xcb MOV RDX,RSI` (→ `RtlpGetEntireXStateAreaLength2` 둘째 인자).
- **(c) 꼬리 호출(JMP)인가, CALL 뒤 더 하는 일이 있나? → 보통 CALL 이다 (`+0xe`). 돌아온 뒤 하는 일은 `ADD RSP,0x28` 과 `RET` 뿐.** `eax` 는 Length3 가 낸 값이 그대로 나간다 (길이는 `rdx` 가 가리키는 곳에 Length3 가 직접 씀: `Length3 +0xf1 MOV [R14],EAX`, `R14 = rdx`, `+0x16`).
  - WinDbg 콜스택엔 Length2 프레임이 보인다 (`RtlGetExtendedContextLength2+0x13` 이 Length3 위에). 꼬리호출이 아니라서.

### 3. 이 인자로 Length3 가 `0x4ff` 를 내나? (PE_emulator 셈이 Length2 를 거쳐도 성립하나)

- 답: **예, 비-CET 스레드 (`ecx = 0x10001b`, `r8d = 0`) 에서 `0x4ff`.** Length2 가 바꾸는 건 `r9` 뿐이고, 이 경로에서는 `r9`·`r8` 이 가리키는/담은 값을 **읽지도 않는다** (아래). 그래서 "Length2 가 인자를 그대로 넘긴다" 는 `[추측]` 이 `[정적]` 으로 확정되고, 정확히는 "rcx·rdx·r8 은 그대로, r9 는 상수" 로 고쳐야 한다.
- **CET 스레드 (`ecx = 0x10005b`, `r8d = 0x800`) 는 `0x4ff` 가 아니다.** 다른 줄을 탄다 (아래). 이 경우 값은 CPU 의 XState 설정(런타임 값)에 달려 있어 정적으로 숫자를 못 낸다.
- `ecx`/`r8d` 가 어디서 정해지나: `PspInitializeThunkContext +0x69 MOV ECX,[RSI+0x74]` (`RSI = GS:[0x188]` = 현재 스레드), `+0x6c AND ECX,0x100000`. `_KTHREAD +0x74` bit 20 = `CetUserShadowStack` (`pdb_struct.py _KTHREAD`). 이 한 비트로 둘이 같이 정해진다:
  - 꺼짐: `R14D = 0` (`+0x72 ~ +0x79`), `R12D = 0x10001b` (`+0x84 ~ +0x8d`).
  - 켜짐: `R14D = 0x800` (CET_U 칸 하나), `R12D = 0x10005b`.
  - 호출 직전 `+0x98 MOV R8D,R14D`, `+0x9b LEA RDX,[RBP]` (`[RBP]` 는 `+0x53` 에서 0 으로 비움), `+0x9f MOV ECX,R12D`, `+0xa2 CALL` Length2, `+0xa7` 돌아옴. 돌아와서 `+0xb1 MOV R8D,[RBP]` 로 그 값을 읽는다.

#### 3-1. 비-CET (`ecx = 0x10001b`, `r8 = 0`): `0x4ff`

`RtlGetExtendedContextLength3.asm` 을 따라가면:

| 줄 | 값 |
|---|---|
| `+0x13 MOV RBP,R8`, `+0x16 MOV R14,RDX`, `+0x25 MOV RSI,R9`, `+0x28 MOV EDI,ECX` | 인자 보관 |
| `+0x2a CALL` `RtlpValidateContextFlags2` (`ecx`·`r8 = r9`·`rdx = 지역 칸 [RSP+0x20]`) | 돌려주는 상태 0 (성공), 지역 칸 `[RSP+0x20] = 1` (아래 "요청서 밖에서 본 것") |
| `+0x3a BT EDI,0x10` → `+0x3e JNC +0x4d` | `0x10001b` 에 bit 16 없음 → `+0x4d` |
| `+0x4d BT EDI,0x14` → `+0x51 JNC` 안 탐 → `+0x53 MOV EBX,0x4d0` → `+0x58 JMP +0x78` | bit 20 켜짐 (AMD64) → `EBX = 0x4d0` |
| `+0x78 MOV R11D,0x10` | `R11D = 0x10` |
| `+0x7e ADD EBX,0x20` | `EBX = 0x4f0` |
| `+0x81 TEST [RSP+0x20],0x2` → `+0x86 JZ +0xe1` | 칸 값 1 → bit 1 꺼짐 → XState 블록 (`+0x88 ~ +0xdf`) **건너뜀** |
| `+0xe1 TEST [RSP+0x20],0x4`, `+0xe6 LEA EAX,[RBX+0x20]`, `+0xe9 CMOVZ EAX,EBX` | bit 2 꺼짐 → `EAX = EBX = 0x4f0` |
| `+0xec DEC EAX`, `+0xee ADD EAX,R11D` | `0x4f0 - 1 + 0x10 = 0x4ff` |
| `+0xf1 MOV [R14],EAX`, `+0xf4 XOR EAX,EAX` | `*rdx = 0x4ff`, 반환값 0 (`STATUS_SUCCESS`) |

- 이 경로에서 `r8`(`RBP`) 은 `+0xb7 AND RBP,RCX` (XState 블록 안) 에서만 쓰이고, `r9` 가 가리키는 `XState` 칸은 `+0x94` 이후 (XState 블록 안) 에서만 읽힌다. `RtlpValidateContextFlags2` 도 이 경로에서는 `[RBX]` (= `XState.EnabledFeatures`) 를 안 읽는다 (`+0x8c TEST CL,CL` → `+0x8e JNZ +0x9c` 로 건너뜀). → **`0x4ff` 는 CPU·VM 설정과 무관한 상수.**
- PE_emulator 가 적은 줄 (`+0x4d ~ +0x58`, `+0x7e`, `+0xe1 ~ +0xee`) 은 맞다. 다만 그 사이의 분기 결정 두 줄 `+0x81/+0x86` (bit 1), `+0xe1` (bit 2) 가 읽는 **`[RSP+0x20] = 1`** 이 `RtlpValidateContextFlags2` 에서 온다는 근거 줄을 더 적어 두면 닫힌다 (아래).

#### 3-2. CET (`ecx = 0x10005b`, `r8 = 0x800`): `0x4ff` 아님

- `RtlpValidateContextFlags2` 가 `0x10005b & 0x100040 == 0x100040` 이라 XState 요청으로 보고 `XState.EnabledFeatures` (`[RBX]`, `+0x90 CMP qword ptr [RBX],0`) 를 본다. 0 이면 `0xc00000bb` 를 돌려주고 (Length3 `+0x31 JS` → `+0xf6` 으로 길이를 안 쓰고 끝남), 아니면 `[RSP+0x20] = 3`.
- 그러면 Length3 가 `+0x86` 에서 XState 블록 (`+0x88 ~ +0xdf`) 을 탄다: `EBX` 를 `0x4f0` 으로 맞춘 뒤 `RtlpRemoveArchDisallowedXStateFeatures` (`+0xc1`), `RtlpGetEntireXStateAreaLength2` (`+0xd1`) 를 부르고 `EBX = 0x4f0 - 0x10 - 0x1c0 + (그 길이)` 를 만든다 (`+0xd6 SUB EBX,R11D`, `+0xd9 ADD EBX,0xfffffe40`, `+0xdf ADD EBX,EAX`). `+0xe1 ~ +0xee` 에서 bit 2 꺼짐 → 최종 `*rdx = 0x32f + (RtlpGetEntireXStateAreaLength2 의 반환값)`.
- 그 반환값은 `RSI` (= `SharedUserData->XState`, 런타임 값, CPU 별) 와 `r8` 에 달려 있다. `RtlpGetEntireXStateAreaLength2` 안은 요청서 범위 밖이라 **안 풀었다.**

## 요청서 밖에서 본 것 (질문 3 을 닫으려고 필요했던 만큼만)

- `RtlpValidateContextFlags2.asm` (이미 있던 파일, 다시 안 뽑음) 에서 `ecx = 0x10001b` 를 따라가면:
  - `+0x0 ~ +0x1a`: `0x10001b & 0x27ffff80 = 0x100000` (≠ `0x10000`) → `+0x1e AND EAX,0x7ffff20 = 0x100000` → `+0x23 CMP EAX,0x100000` 같음 → `+0x28 JZ +0x4d` (통과).
  - `+0x4d ~ +0x8e`: XState 관련 비트 조합 (`0x400020`, `0x100040`, `0x10040`) 이 하나도 안 맞음 → `CL = 1` → `+0x8e JNZ +0x9c`, `R10D = 1` 그대로 (`+0x61`).
  - `+0x9c ~ +0xa7`: `0x10001b & 0x100080 = 0x100000` (≠ `0x100080`, CET 커널 검사 안 탐) → `+0xbd` → `+0xc2 MOV [R11],R10D` (`R11 = rdx` = Length3 의 지역 칸) → `+0xc5 XOR EAX,EAX`, RET.
  - 즉 비-CET 에서 `[RSP+0x20] = 1` (bit 1·bit 2 모두 꺼짐).
- run42 에서 이미 정리된 "`0x10001b` = 비-CET, `0x10005b` = CET" 와 같은 결론이다 (`runs/02_sprint/run42-continue-detail.md` 82~83줄).

## 동적분석으로 확인해야 할 것

- **없다.** 질문 1~3 모두 정적으로 닫힌다. run44 사진의 맡긴 문맥 주소가 `0x4ff` 셈과 맞는다는 `[관찰]` 은 비-CET 스레드 (`[thread+0x74]` bit 20 꺼짐) 라는 것과도 앞뒤가 맞는다.
- 굳이 확인한다면 (선택, 없어도 됨):
  - `nt+0x49544e` (Length2 `+0xe`, CALL 직전): `r r9` = `fffff780`000003d8`, `r rcx`·`rdx`·`r8` 이 호출자와 같은지.
  - `nt+0x91078f` (`PspInitializeThunkContext +0xa7`, Length2 에서 돌아온 직후): `r eax` = 0, `dd @rbp L1` = `0x4ff` (비-CET). CET 스레드에서 보고 싶으면 이때 `0x4ff` 가 **아닌** 다른 값이 나와야 한다 — test.exe 의 첫 스레드는 비-CET 로 보인다 (run44).
- CET 스레드의 길이 값이 필요해지면 (지금은 필요 없음): `RtlpGetEntireXStateAreaLength2` 를 새 요청서로 받아야 한다 (`_index.tsv`: rva `0x4eea6c`, 77바이트). 지금 요청서 범위 밖.

## 판정

**분석성공.** 새로 뽑은 함수 1개의 `.asm` 5줄과 이미 있던 Length3·`PspInitializeThunkContext`·`RtlpValidateContextFlags2` 의 줄만으로 질문 1·2·3 이 정적으로 답이 났고 (`r9` 상수 하나가 PE_emulator 가 `[추측]` 으로 달아 둔 "인자 그대로" 와 다른 점), 비-CET 에서 `0x4ff` 가 Length2 를 거쳐도 성립한다.

PE_emulator 쪽 고칠 점 (태그만):
- "Length2 → Length3 인자 그대로 `[추측]`" → `[정적]` 으로 올리고 문구를 "rcx·rdx·r8 그대로, r9 = `0xFFFFF780000003D8` (`&SharedUserData->XState`), CALL 뒤 `eax` 그대로" 로.
- `0x4ff` 는 비-CET (`0x10001b`, `r8 = 0`) 값이라고 적기. CET (`0x10005b`, `r8 = 0x800`) 는 `0x32f + RtlpGetEntireXStateAreaLength2(...)` 로 다른 길이고 CPU 값에 달려 있다.
- 근거 줄에 `RtlpValidateContextFlags2 +0x4d ~ +0xc2` (`[RSP+0x20] = 1`) 추가.

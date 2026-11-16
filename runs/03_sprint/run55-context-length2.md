# 2026-10-31 run55 — 맡긴 문맥 길이 (Q103, PE_emulator 요청 1): `RtlGetExtendedContextLength2` 는 **`rcx`·`rdx`·`r8` 그대로 + `r9` = 상수 `0xFFFFF780000003D8`** 로 `RtlGetExtendedContextLength3` 를 CALL 하고 `eax` 를 그대로 돌려준다. 비-CET 스레드면 길이 **`0x4ff`** (정적)

> **닫힘 (run122)**: 아래 "아직 확인 필요" 둘째 줄 (CET 스레드 길이 `0x32f + RtlpGetEntireXStateAreaLength2(…)` 안 풂) 이 닫혔다 — 그 함수는 `ControlFlags` bit 1 (압축) 꺼짐이면 `XState.Size`, 켜짐이면 `0x240` + 마스크 비트마다 `AllFeatures[i]` (`[정적]`). 이 VM (run44 사진 XState) 으로 CET 스레드 길이 **`0x57f`** (마스크 `0x800` → `0x240 + 0x10`). 이 문서의 다른 내용 (비-CET `0x4ff` 등) 은 그대로 맞다. → `run122-cet-xstate-len.md`

## PE_emulator 용 한 줄 요약

결과서 (`_workspace/sast/49_context-length2_result.md`) "한 줄 요약" 그대로, 근거 태그를 붙였다:

> `[정적] run55` `RtlGetExtendedContextLength2(ecx, rdx, r8)` = `RtlGetExtendedContextLength3(ecx, rdx, r8, r9 = 0xFFFFF780000003D8)` 을 **CALL** 하고, 돌아온 `eax` 를 **그대로** 돌려준다. `r9` 는 상수 = `KUSER_SHARED_DATA`(커널 매핑 `0xFFFFF78000000000`) `+0x3d8` = `XState` (`_XSTATE_CONFIGURATION`). — `ms/disassemble/ntoskrnl/RtlGetExtendedContextLength2.asm` `+0x4`(r9 채움) `+0xe`(CALL) `+0x13 ~ +0x17`(돌아와서 스택만 정리하고 RET).

길이 값 (`PspInitializeThunkContext +0xa2` 가 받는 것):

> `[정적] run55` 비-CET 스레드 (`ecx = 0x10001b`, `r8 = 0`) 에서 `*rdx = 0x4ff`, `eax = 0`. — `ms/disassemble/ntoskrnl/RtlGetExtendedContextLength3.asm` `+0x4d ~ +0x58` (`EBX = 0x4d0`), `+0x78` (`R11D = 0x10`), `+0x7e` (`+0x20`), `+0x81 ~ +0x86` (XState 블록 건너뜀), `+0xe1 ~ +0xee` (`0x4f0 − 1 + 0x10`), `+0xf1` (`*rdx` 에 씀) + `ms/disassemble/ntoskrnl/RtlpValidateContextFlags2.asm` `+0x4d ~ +0xc2` (분기 칸 `[RSP+0x20] = 1`).

PE_emulator 쪽에서 고칠 것 (결과서 "판정" 그대로, 태그만):

- "Length2 → Length3 인자 그대로 `[추측]`" → **`[정적] run55`**. 문구는 "`rcx`·`rdx`·`r8` 그대로, `r9` = `0xFFFFF780000003D8` (`&SharedUserData->XState`), CALL 뒤 `eax` 그대로".
- `0x4ff` 는 **비-CET** (`0x10001b`, `r8 = 0`) 값이라고 적기. CET (`0x10005b`, `r8 = 0x800`) 는 다른 길이 (아래 "아직 확인 필요").
- 근거 줄에 `RtlpValidateContextFlags2 +0x4d ~ +0xc2` (`[RSP+0x20] = 1`) 추가.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 03_sprint 열셋째 바퀴. plan **Q103** (PE_emulator 요청 1, 사용자 결정 2026-10-31 로 Q78d4 앞). 짐 지도 바퀴는 아니다 (`baggage.md` 엔 이 길이를 적은 줄이 없어 안 고쳤다).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음, 결과서 `ms/sym/ntoskrnl.exe.pdb.info.txt` 대조). 이미 analyze 된 프로젝트에서 뽑기만 했다.
- 버전 대조 (결과서): `_calls.tsv` `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess` (알려진 값과 같음). 새로 뽑은 `.asm` 머리 rva `0x495440` 이 `_index.tsv` 와 같다.

## 목적

- `PspInitializeThunkContext +0xa2` 가 부르는 `RtlGetExtendedContextLength2` (run16: 맡긴 문맥 길이) 가 `RtlGetExtendedContextLength3` 에 무엇을 넘기나.
- PE_emulator 가 쓰는 `0x4ff` (Length3 셈) 가 Length2 를 거쳐도 그대로인가.
- 범위 밖 (요청서): Length3 의 XState 길이 계산 전체, Length2 의 다른 caller.

## 방법

- 정적: `_workspace/sast/49_context-length2.md` — ntoskrnl, `^RtlGetExtendedContextLength2$` depth 0 (새로 1개, `.c`·`.asm`). `_calls.tsv` 4,332 → 4,333줄.
  - 읽기만 (이미 있던 파일): `RtlGetExtendedContextLength3.asm` 전체, `PspInitializeThunkContext.asm` `+0x5c ~ +0xa7`, `RtlpValidateContextFlags2.asm` (질문 3 의 분기 칸 값 때문에).
  - `.c` 는 `RtlGetExtendedContextLength3();` 한 줄뿐이다 (PDB 에 인자 정보가 없어 Ghidra 가 인자를 못 잡음). **인자 근거는 `.asm` 만** 썼다.
  - `pdb_aliases.py --where` → PDB 이름 그대로 (`rva=0x495440 [function]`). 같은 주소 다른 이름 아님.
  - `KUSER_SHARED_DATA +0x3d8 = XState` 는 결과서가 `pdb_struct.py ntoskrnl.exe _KUSER_SHARED_DATA` 로 확인.
- 결과서: `_workspace/sast/49_context-length2_result.md` (**분석성공**, 동적 확인 필요 없음)
- 이 문서를 쓰며 다시 맞춘 것 (flow-writer, `.asm`·`_calls.tsv`): Length2 5줄 전부, `MOV R9,-0x87ffffffc28` = `0xFFFFF780000003D8` (64비트 2의 보수), Length3 `+0x13 ~ +0x28`·`+0x3a ~ +0x86`·`+0xe1 ~ +0xf4`, `RtlpValidateContextFlags2` `+0x0 ~ +0xc5` 을 `0x10001b` 로 따라감, `PspInitializeThunkContext` `+0x53`·`+0x69 ~ +0xa7`·`+0xb1`·`+0x17a ~ +0x187`, `_calls.tsv` 의 Length2 caller 줄 (`NtCreateUserProcess +0xc85`·`PspInitializeThunkContext +0xa2`).
- 문서: 이 run 문서, `flow.md` (기준·2.3 `PspInitializeThunkContext` 절 3번·4장·5장). 그림 (mermaid) 은 안 바꿨다 (Length2 는 그림에 없는 글 속 함수).

## 결과

비유 (새 직원 책상 차리기, 이어서):

- 관리사무소가 직원의 원래 쪽지를 사물함에 넣기 전에 "쪽지 크기" 를 잰다. 그 자를 빌려 주는 게 `Length2`, 실제로 재는 건 `Length3`.
- `Length2` 는 자를 그냥 건네주는 게 아니라, **회사 공용 게시판 주소 (`SharedUserData->XState`)** 한 장을 같이 붙여 준다. 그게 `r9` 상수다.
- 직원이 CET (그림자 스택) 를 안 쓰면 `Length3` 는 그 게시판을 **보지도 않고** 정해진 크기 `0x4ff` 를 낸다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!PspInitializeThunkContext` | `+0xa2` | `+0xa7` | `nt!RtlGetExtendedContextLength2` | [정적] |
| `nt!NtCreateUserProcess` | `+0xc85` | `+0xc8a` | `nt!RtlGetExtendedContextLength2` (이번엔 안 읽음, sast/03) | [정적] |
| `nt!RtlGetExtendedContextLength2` | `+0xe` | `+0x13` | `nt!RtlGetExtendedContextLength3` | [정적] |
| `nt!RtlGetExtendedContextLength3` | `+0x2a` | `+0x2f` | `nt!RtlpValidateContextFlags2` | [정적] |
| `nt!RtlGetExtendedContextLength3` | `+0xc1` | `+0xc6` | `nt!RtlpRemoveArchDisallowedXStateFeatures` (XState 블록 안, 비-CET 는 안 탐) | [정적] |
| `nt!RtlGetExtendedContextLength3` | `+0xd1` | `+0xd6` | `nt!RtlpGetEntireXStateAreaLength2` (XState 블록 안, 비-CET 는 안 탐) | [정적] |
| `nt!KiContinuePreviousModeUser` | `+0x133` | `+0x138` | `nt!RtlGetExtendedContextLength3` (Length2 를 안 거침, run29) | [정적] |

### `RtlGetExtendedContextLength2` (rva `0x495440`, 24바이트) — 질문 1·2

| 줄 | 하는 일 | 표시 |
|---|---|---|
| `+0x0` | 스택 `0x28` 잡기 (그림자 공간 + 정렬) | [정적] |
| `+0x4` | `r9` ← 상수 `0xFFFFF780000003D8` (10바이트 명령, `+0x4 ~ +0xd`) | [정적] |
| `+0xe` | **CALL** `RtlGetExtendedContextLength3` (꼬리 JMP 아님) | [정적] |
| `+0x13` | 스택 되돌림 | [정적] |
| `+0x17` | RET — `eax` 안 건드림 | [정적] |

- 질문 1: rva `0x495440` **맞다** (`.asm` 머리 `entry=140495440`, `_index.tsv` 24바이트, PDB 이름 그대로).
- 질문 2 (a): `rcx`·`rdx` **그대로** — `+0x0 ~ +0xe` 사이에 둘을 쓰는 명령이 없다.
- 질문 2 (b): 셋째 `r8` **호출자 값 그대로** (쓰는 명령 없음). 넷째 `r9` **상수로 덮어씀** (`+0x4`).
  - `0xFFFFF780000003D8` = `KUSER_SHARED_DATA` 커널 매핑 + `0x3d8` = `XState` (`_XSTATE_CONFIGURATION`, 결과서 `pdb_struct.py`).
  - Length3 가 이 값을 `RSI` 에 두고 (`+0x25`) `_XSTATE_CONFIGURATION` 칸으로 읽는다: `+0x94 [RSI+0x14]`, `+0x9b [RSI+0x330]`, `+0xa7 [RSI]`, `+0xcb` (→ `RtlpGetEntireXStateAreaLength2` 둘째 인자). 모두 XState 블록 (`+0x88 ~ +0xdf`) 안이다.
- 질문 2 (c): **보통 CALL** (`+0xe`). 돌아와서 하는 일은 스택 정리와 RET 뿐. 그래서 WinDbg 콜스택엔 `RtlGetExtendedContextLength2+0x13` 프레임이 Length3 위에 보일 것이다 `[추측: 꼬리 호출이 아니라서]`.
  - 길이는 반환값이 아니라 **`rdx` 가 가리키는 칸**에 Length3 가 직접 쓴다 (`Length3 +0x16 MOV R14,RDX`, `+0xf1 MOV [R14],EAX`). 반환값 `eax` 는 상태 (`+0xf4 XOR EAX,EAX` = 0).

### `PspInitializeThunkContext` 가 넘기는 값 (`+0x53 ~ +0xa7`)

- 스레드 `_KTHREAD +0x74` bit 20 (`CetUserShadowStack`, 결과서 `pdb_struct.py`) 한 비트로 두 값이 같이 정해진다 [정적]:
  - `+0x5c` `RSI` = 현재 스레드 (`GS:[0x188]`), `+0x69 ~ +0x6c` 그 비트만 남김.
  - `+0x72 ~ +0x79` `R14D` = 꺼짐 `0` / 켜짐 `0x800`.
  - `+0x84 ~ +0x8d` `R12D` = 꺼짐 `0x10001b` / 켜짐 `0x10005b`.
- 호출 직전: `+0x98` `r8d` ← `R14D` (위쪽 32비트 0), `+0x9b` `rdx` ← `&[RBP]` (`+0x53` 에서 0 으로 비움), `+0x9f` `ecx` ← `R12D`, `+0xa2` CALL.
- 돌아와서: `+0xa7 ~ +0xab` 상태가 음수면 실패 길, `+0xb1` `[RBP]` (= 길이) 를 읽어 커널 버퍼 크기로, `+0x17a ~ +0x187` 맡길 자리 `R15 = (원래 Rsp − [RBP]) & ~0xf` (run16 의 "문맥 길이" 가 이 값).

### 비-CET (`ecx = 0x10001b`, `r8 = 0`) 셈 — 질문 3 [정적]

1. `RtlpValidateContextFlags2` (Length3 `+0x2a`, 분기 칸 `[RSP+0x20]` 주소를 `rdx` 로):
   - `+0xa ~ +0x28`: `0x10001b` 의 아키텍처 비트 검사 통과 (`& 0x7ffff20 = 0x100000`) → `+0x4d`.
   - `+0x4d ~ +0x8e`: XState 비트 조합 (`0x400020`·`0x100040`·`0x10040`) 하나도 안 맞음 → `CL = 1` → `+0x9c` 로 건너뜀. `[RBX]` (= `XState.EnabledFeatures`) 를 **안 읽는다**. `R10D = 1` 그대로 (`+0x61`).
   - `+0x9c ~ +0xa7`: `& 0x100080 = 0x100000` → CET 커널 검사 안 탐 → `+0xbd`.
   - `+0xc2` 분기 칸 `= 1`, `+0xc5` 반환 0.
2. Length3:
   - `+0x3a ~ +0x3e` bit 16 없음 → `+0x4d`. bit 20 있음 → `+0x53 EBX = 0x4d0` → `+0x78 R11D = 0x10` → `+0x7e EBX = 0x4f0`.
   - `+0x81 ~ +0x86` 분기 칸 bit 1 꺼짐 → XState 블록 (`+0x88 ~ +0xdf`) **건너뜀**.
   - `+0xe1 ~ +0xe9` 분기 칸 bit 2 꺼짐 → `EAX = EBX = 0x4f0`.
   - `+0xec ~ +0xee` `0x4f0 − 1 + 0x10 = 0x4ff`.
   - `+0xf1` `*rdx = 0x4ff`, `+0xf4` 반환 0.
- 이 길에서 `r8` (Length3 `RBP`) 은 XState 블록 안 `+0xb7` 에서만, `r9` (`RSI`) 가 가리키는 칸은 `+0x94` 이후에서만 쓰인다. 둘 다 건너뛰므로 **`0x4ff` 는 CPU·VM 설정과 무관한 상수**다.
- PE_emulator 가 적은 줄 (`+0x4d ~ +0x58`, `+0x7e`, `+0xe1 ~ +0xee`) 은 맞다. 사이의 분기 두 곳 (`+0x81/+0x86`, `+0xe1`) 이 읽는 칸 값 1 의 출처가 위 1 이다.
- `[추측]` `0x4ff` 의 뜻: `0x4d0` 은 `CONTEXT` 크기 (`PspInitializeThunkContext +0x101 ~ +0x10e` 가 지역 문맥을 `0x4d0` 바이트 `memset`, run44 가 맡긴 `_CONTEXT` 를 `0x4d0` 바이트 비교), `+0x20` 은 확장 문맥 머리, `−1 + 0x10` 은 16바이트 맞춤 여유로 보인다. 칸 이름으로 확인하진 않았다.

### CET (`ecx = 0x10005b`, `r8 = 0x800`) — `0x4ff` 아님 [정적]

- `RtlpValidateContextFlags2`: `0x10005b & 0x100040 = 0x100040` → XState 요청으로 보고 `+0x90` `XState.EnabledFeatures` 를 읽는다.
  - 0 이면 `0xc00000bb` → Length3 `+0x31 JS +0xf6` → 길이를 안 쓰고 끝.
  - 아니면 분기 칸 `= 3`.
- Length3 가 XState 블록을 탄다: `EBX` 를 `0x4f0` 으로 맞춘 뒤 `+0xc1` `RtlpRemoveArchDisallowedXStateFeatures`, `+0xd1` `RtlpGetEntireXStateAreaLength2`, `+0xd6 ~ +0xdf` `EBX = 0x4f0 − 0x10 − 0x1c0 + (그 반환값)`. `+0xe1 ~ +0xee` bit 2 꺼짐 → **`*rdx = 0x32f + RtlpGetEntireXStateAreaLength2(…)`**.
- 그 반환값은 `SharedUserData->XState` (CPU 별 런타임 값) 와 `r8` 에 달렸다. `RtlpGetEntireXStateAreaLength2` 안은 범위 밖이라 **안 풀었다**.

### 호출 관계

```
(test.exe 첫 스레드, 커널)
PspUserThreadStartup
 └─ +0xb3  PspInitializeThunkContext
         │  +0x69 ~ +0x8d  _KTHREAD+0x74 bit20 → R14D (0 / 0x800), R12D (0x10001b / 0x10005b)
         ├─ +0xa7  RtlGetExtendedContextLength2   (ecx = R12D, rdx = &[RBP], r8d = R14D)
         │       │  +0x4   r9 = 0xFFFFF780000003D8 (&SharedUserData->XState)
         │       └─ +0x13  RtlGetExtendedContextLength3   (rcx·rdx·r8 그대로)
         │               ├─ +0x2f  RtlpValidateContextFlags2   분기 칸 [RSP+0x20] (비-CET 1)
         │               │  (XState 블록 +0x88 ~ +0xdf — 비-CET 는 건너뜀)
         │               ├─ +0xc6  RtlpRemoveArchDisallowedXStateFeatures
         │               ├─ +0xd6  RtlpGetEntireXStateAreaLength2
         │               │  +0xf1  *rdx = 길이 (비-CET 0x4ff)
         │  +0xb1  [RBP] 읽음
         │  +0x17a ~ +0x187  R15 = (원래 Rsp − 길이) & ~0xf   (맡길 자리, run16)
         └─ …
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄.

## 확인된 것

- `[정적]` `RtlGetExtendedContextLength2` = rva `0x495440`, 24바이트, PDB 이름 그대로 (`RtlGetExtendedContextLength2.asm` 머리, `_index.tsv`, `pdb_aliases.py`).
- `[정적]` Length2 는 `rcx`·`rdx`·`r8` 을 그대로, `r9` 만 `0xFFFFF780000003D8` 로 채워 Length3 를 **CALL** (`+0x4`, `+0xe`) 하고, `eax` 를 그대로 돌려준다 (`+0x13 ~ +0x17`).
- `[정적]` 비-CET (`0x10001b`, `r8 = 0`) 에서 맡긴 문맥 길이 `0x4ff`, 반환 0 (Length3 `+0x4d ~ +0xf4`, `RtlpValidateContextFlags2 +0x4d ~ +0xc5`). `r9`·`r8` 은 이 길에서 안 읽혀서 Length2 가 `r9` 를 바꿔도 값이 같다.
- `[정적]` CET 와 비-CET 는 스레드 `_KTHREAD +0x74` bit 20 하나로 갈린다 (`PspInitializeThunkContext +0x69 ~ +0x8d`). run42 "`0x10001b` = 비-CET, `0x10005b` = CET" 와 같은 결론.
- `[관찰]` + `[정적]` 앞뒤가 맞는다: run44 의 test.exe 첫 스레드는 맡긴 문맥 `ContextFlags` `0x10001b` (P1, 비-CET 길) 이고, P0 트랩 프레임 `Rsp` `9b2fcfff48` → 맡긴 문맥 `9b2fcffa40` 이다. `(0x9b2fcfff48 − 0x4ff) & ~0xf = 0x9b2fcffa40` 으로 같다.
  - 단 16바이트 맞춤 때문에 길이가 `0x4f9 ~ 0x508` 이면 다 같은 주소가 나온다. 사진은 `0x4ff` 와 **어긋나지 않음**까지만 말한다 (길이 하나를 집어내진 못함).

## 정정

- 우리 문서엔 틀린 곳이 없다. run16 의 "길이 = `RtlGetExtendedContextLength2` (`+0xa2`) 결과" 는 그대로 맞고, 이번에 그 안이 `[정적]` 으로 채워졌다.
- PE_emulator 쪽의 "Length2 → Length3 인자 **그대로**" `[추측]` 은 정확히는 "`rcx`·`rdx`·`r8` 그대로, **`r9` 는 상수**" 다 (맨 위 "PE_emulator 쪽에서 고칠 것").

## 아직 확인 필요

- **`0x4ff` 는 비-CET 일 때만이다.** 스레드 `_KTHREAD +0x74` bit 20 (`CetUserShadowStack`) 이 꺼져 있어야 한다. test.exe 첫 스레드가 비-CET 인 건 run44 `ContextFlags 0x10001b` 로 앞뒤가 맞지만, 그 비트 자체를 읽은 적은 없다 (동적 Q57 의 "스레드 `+0x74` bit20").
- **CET 스레드 길이 = `0x32f + RtlpGetEntireXStateAreaLength2(…)`, 안 풂.** 그 함수 (`_index.tsv` rva `0x4eea6c`, 77바이트) 와 `SharedUserData->XState` 런타임 값에 달렸다. 필요해지면 새 정적 요청서 (지금은 필요 없음, 결과서).
- (선택, 없어도 됨) 동적으로 한 번 보려면 (결과서):
  - `nt+0x49544e` (Length2 `+0xe`, CALL 직전): `r r9` = ``fffff780`000003d8``, `rcx`·`rdx`·`r8` 이 호출자 값과 같은지.
  - `nt+0x91078f` (`PspInitializeThunkContext +0xa7`): `r eax` = 0, `dd @rbp L1` = `0x4ff`.
  - 모든 스레드가 지나니 프로세스 조건 필수.
- 범위 밖 그대로: `NtCreateUserProcess +0xc85` 등 Length2 의 다른 caller, Length3 의 XState 길이 계산 전체.

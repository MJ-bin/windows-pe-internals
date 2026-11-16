# 2026-11-16 (작성일) run122 — CET 스레드의 맡긴 문맥 길이 (Q105, 정적만): `RtlpGetEntireXStateAreaLength2` 식을 풀고 이 VM 값으로 셈 → CET 스레드 **`0x57f`** (비-CET `0x4ff` 와 `0x80` 차이). test.exe 첫 스레드 (비-CET) 는 이 함수를 아예 안 부른다

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 2.3 `PspInitializeThunkContext` 절 3번 (맡긴 자리 = 원래 `Rsp` − 문맥 길이) 의 **CET 갈래 안쪽**이다. test.exe 첫 스레드는 비-CET 라 이 갈래를 안 탄다. 그림에 없는 글 속 함수 (`RtlGetExtendedContextLength2` 아래) 라 **그림은 안 바꿨다.** 글 (flow.md 기준 · 2.3 · 4장 · 5장) 과 짐 지도 기준 줄 한 줄만 고쳤다.

## 요약

- **식** `[정적]` — `RtlpGetEntireXStateAreaLength2(rcx = 기능 마스크, rdx = &XState)`:
  - XState `ControlFlags` bit 1 (`CompactionEnabled`) **꺼짐** → `XState.Size` 를 그대로 돌려준다 (마스크는 안 봄).
  - **켜짐** → `0x240` 에서 시작. 마스크 비트 `i` (2 ~ 63) 마다: `AlignedFeatures` 에 `i` 가 있으면 먼저 64바이트로 올리고, `AllFeatures[i]` 를 더한다.
  - 읽는 XState 칸은 넷뿐: `Size` (`+0x10`) · `ControlFlags` (`+0x14`) · `AlignedFeatures` (`+0x220`) · `AllFeatures[i]` (`+0x22c + 4i`).
- **이 VM 값으로 셈** `[정적 계산 + 관찰 입력]`:
  - 마스크 `0x800` (CET_U 한 비트) → `F = 0x240 + AllFeatures[11] (0x10) = 0x250`.
  - CET 스레드 문맥 길이 = `0x32f + 0x250` = **`0x57f`** (1407 바이트). 비-CET 는 run55 그대로 **`0x4ff`**.
- **caller 8곳** `[정적]` — 시동 길에서 닿는 건 둘 (`RtlGetExtendedContextLength3 +0xd1` · `RtlInitializeExtendedContext2 +0x15f`), 둘 다 XState 요청 비트 (`RtlpValidateContextFlags2` 가 낸 칸 bit 1) 가 켜질 때만. 비-CET 는 둘 다 안 닿는다.
- **PE_emulator** (결과서 "지금 우리 맞나", flow-writer 는 코드 안 읽음): 비-CET (`0x4ff`) 만 쓰면 **맞음 · 해당 없음**. CET 스레드까지 흉내 내면 `_XSTATE_CONFIGURATION` 을 이 VM 값으로 채워야 `0x57f` 가 나온다.
- run55 의 "아직 확인 필요" ② (CET 길이 안 풂) 가 **닫혔다** (run55 맨 위 "닫힘 (run122)").

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든째 바퀴. plan **Q105** (원래 "필요할 때만" — 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례). 짐 지도 바퀴는 아니다 (`baggage.md` 엔 이 길이를 적은 칸 줄이 없다 — 기준 줄 한 줄만).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID+age `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음, 결과서 `ms/sym/ntoskrnl.exe.pdb.info.txt` 대조). 이미 analyze 된 프로젝트에서 뽑기만.
- 버전 대조 (결과서): `_calls.tsv` `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess` (알려진 값과 같음). `Length3 +0xd1/+0xd6 → RtlpGetEntireXStateAreaLength2` = `refs` 의 `from_rva 0x4ee959 − 0x4ee888 = +0xd1`. `_index.tsv` rva `0x4eea6c` · 77바이트 = 새 `.asm` 머리.
- 다른 decompiler 가 같은 때 ntdll (sast/116) · 다른 DLL (sast/117) 을 돌렸다 — 이번엔 둘 다 안 건드림.

## 목적

- run55 (sast/49) 가 남긴 "CET 스레드 길이 = `0x32f + RtlpGetEntireXStateAreaLength2(…)`, 안 풂" 을 닫는다 (Q105):
  1. 그 함수가 돌려주는 값의 식 (읽는 XState 칸 · 계산).
  2. 이 VM 의 KUSD `XState` 값 (run44 사진) 으로 CET 스레드 길이를 숫자로. 비-CET `0x4ff` 와 나란히.
  3. 그 함수를 부르는 자리 전수와 CET 갈래 조건 한 줄.

## 방법

- 정적: `_workspace/sast/118_cet-xstate-len.md` — ntoskrnl.
  - 새로 뽑음 (depth 0, 둘): `^RtlpGetEntireXStateAreaLength2$` (질문 1), `^RtlpRemoveArchDisallowedXStateFeatures$` (58바이트 — 질문 2 숫자를 닫으려면 CET 비트가 걸러지는지 봐야 해서 결과서가 더함). 둘 다 안에서 부르는 함수가 없어 `_calls.tsv` 줄은 안 늘었다.
  - 읽기만 (있던 파일): `RtlGetExtendedContextLength3.asm`, `RtlpValidateContextFlags2.asm`, `PspInitializeThunkContext.asm` (`+0x53 ~ +0xf0`), `RtlInitializeExtendedContext2.asm` (`+0x129 ~ +0x178`).
  - caller 전수: `run_ghidra.sh refs ntoskrnl.exe RtlpGetEntireXStateAreaLength2` → `ms/disassemble/ntoskrnl/_refs/RtlpGetEntireXStateAreaLength2.tsv` (참조 9줄). `.asm` 이 없는 caller 5곳은 결과서가 `objdump -d` 로 call 앞 0x40 바이트만 읽음 (파일로 안 남김).
  - XState 칸 이름: `pdb_struct.py ntoskrnl.exe _XSTATE_CONFIGURATION` (flow-writer 가 다시 돌려 칸 오프셋 맞춤 — `Size +0x10` · `ControlFlags +0x14` (bit 0 `OptimizedSave`, bit 1 `CompactionEnabled`) · `EnabledSupervisorFeatures +0x218` · `AlignedFeatures +0x220` · `AllFeatureSize +0x228` · `AllFeatures +0x22c` (256B) · `EnabledUserVisibleSupervisorFeatures +0x330` · `AllNonLargeFeatureSize +0x340`).
- 입력 값 (사진): run44 덤프 읽기 보충 로그 `ms/logs/04_baggage-read_supp_P1.txt` `:286` `dq fffff78000000000 L0x150` 에서 KUSD `+0x3d8 ~ +0x71f` (XState 840B). 줄 번호는 flow-writer 가 로그로 맞춤 — 아래 "도구 메모".
- 결과서: `_workspace/sast/118_cet-xstate-len_result.md` (**분석성공**, 스크립트 문제 없음, 동적 필수 없음).
- flow-writer 가 `.asm` 으로 다시 본 줄: `RtlpGetEntireXStateAreaLength2` 77바이트 전부 (`+0x0 ~ +0x4d`), `RtlpRemoveArchDisallowedXStateFeatures` 전부, Length3 `+0x81 ~ +0xf4`, `RtlpValidateContextFlags2 +0x4d ~ +0xc5`, `PspInitializeThunkContext +0x53 · +0x5c · +0x69 ~ +0x8d · +0x98 ~ +0xa2 · +0xe3 · +0xf0`, `RtlInitializeExtendedContext2 +0x104 ~ +0x192`, `_refs` 9줄 — 결과서와 같다.
- 앞 바퀴 짝: run55 (sast/49 — Length2 → Length3, 비-CET `0x4ff`), run44 (사진), run111 (sast/106 — 맡긴 문맥 CET_U 칸), run115 (sast/110 — KUSD `XState` 를 놓는 줄 `KiInitializeXSaveConfiguration`).

## 결과

비유 (새 직원 책상 차리기, run55 이어서):

- 관리사무소가 직원의 원래 쪽지를 사물함에 넣기 전에 크기를 잰다. 그림자 스택 (CET) 을 안 쓰는 직원은 늘 같은 크기 `0x4ff` 다.
- CET 직원은 쪽지 뒤에 **CPU 상태 붙임 종이**가 한 장 더 붙는다. 그 종이 크기는 회사 공용 게시판 (`SharedUserData->XState`) 의 "기능별 크기 표" 를 보고 정한다.
- 이번에 그 표 읽는 법 (`RtlpGetEntireXStateAreaLength2`) 을 풀었다. 이 VM 게시판으로 재면 붙임 종이 몫이 `0x80` 더해져 **`0x57f`**.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `_refs/RtlpGetEntireXStateAreaLength2.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!PspInitializeThunkContext` | `+0xa2` | `+0xa7` | `nt!RtlGetExtendedContextLength2` (run55) | [정적] |
| `nt!RtlGetExtendedContextLength2` | `+0xe` | `+0x13` | `nt!RtlGetExtendedContextLength3` (run55) | [정적] |
| `nt!RtlGetExtendedContextLength3` | `+0xc1` | `+0xc6` | `nt!RtlpRemoveArchDisallowedXStateFeatures` (XState 블록, 압축 켜짐일 때만) | [정적] |
| `nt!RtlGetExtendedContextLength3` | `+0xd1` | `+0xd6` | `nt!RtlpGetEntireXStateAreaLength2` (XState 블록) | [정적] |
| `nt!PspInitializeThunkContext` | `+0xf0` | `+0xf5` | `nt!RtlInitializeExtendedContext2` | [정적] |
| `nt!RtlInitializeExtendedContext2` | `+0x14f` | `+0x154` | `nt!RtlpRemoveArchDisallowedXStateFeatures` (압축 켜짐일 때만) | [정적] |
| `nt!RtlInitializeExtendedContext2` | `+0x15f` | `+0x164` | `nt!RtlpGetEntireXStateAreaLength2` (`+0xa7 JNZ` XState 길에서만) | [정적] |
| `nt!RtlInitializeExtendedContext2` | `+0x173` | `+0x178` | `nt!memset` | [정적] |
| `nt!RtlpGetEntireXStateAreaLength` | `+0xe` | `+0x13` | `nt!RtlpGetEntireXStateAreaLength2` (`refs`, 24바이트 감싸개) | [정적] |
| `nt!RtlpGetNonLegacyXStateAreaLength` | `+0x3b` | `+0x40` | `nt!RtlpGetEntireXStateAreaLength2` (`refs`) | [정적] |
| `nt!RtlpWalkFrameChain` | `+0xef6` | `+0xefb` | `nt!RtlpGetEntireXStateAreaLength2` (`refs`) | [정적] |
| `nt!RtlDispatchException` | `+0x3e7` | `+0x3ec` | `nt!RtlpGetEntireXStateAreaLength2` (`refs`) | [정적] |
| `nt!RtlUnwind` | `+0x127` | `+0x12c` | `nt!RtlpGetEntireXStateAreaLength2` (`refs`) | [정적] |
| `nt!RtlUnwindEx` | `+0x630` | `+0x635` | `nt!RtlpGetEntireXStateAreaLength2` (`refs`) | [정적] |

- `refs` 9줄 중 나머지 1줄은 `from_rva 0x101264 (함수 밖) DATA` — `.pdata` 풀기 표 항목으로 보인다 `[추측: 결과서]`. 지도 `_nodes.tsv` `callers_total = 8` 과 같다 (결과서).
- 아래 6 caller (`RtlpGetEntireXStateAreaLength` 부터) 는 `.asm` 이 없다. call 은 `refs` 자리, ret 는 결과서 표 값 (`objdump`).

### 1. `RtlpGetEntireXStateAreaLength2` (rva `0x4eea6c`, 77바이트) — 식 [정적]

| 줄 | 하는 일 | XState 칸 |
|---|---|---|
| `+0x0` | `EAX` ← `[RDX+0x14]` | `ControlFlags` |
| `+0x3` | 마스크 (`rcx`) 를 `R10` 에 | |
| `+0x6 ~ +0xd` | bit 1 꺼짐이면 `+0x4a` 로 | bit 1 = `CompactionEnabled` |
| `+0x4a` · `+0x4d` | **꺼진 갈래**: `EAX` ← `[RDX+0x10]`, RET | `Size` |
| `+0xf` | `R9` ← `[RDX+0x220]` | `AlignedFeatures` |
| `+0x16` | **켜진 갈래** 시작값 `0x240` | (상수) |
| `+0x1b` | `RDX` ← `AllFeatures[2]` 칸 (`+0x234` = `0x22c + 2×4`) | `AllFeatures` |
| `+0x22 ~ +0x2e` | `R8 = 1 << i` (`i` = `ECX`, 2 부터). 마스크에 없으면 `+0x3d` 로 | |
| `+0x30 ~ +0x38` | `AlignedFeatures` 에 `i` 있으면 `(F + 0x3f) & ~0x3f` | |
| `+0x3b` | `F += [RDX]` (= `AllFeatures[i]`) | `AllFeatures[i]` |
| `+0x3d ~ +0x46` | `i++`, `RDX += 4`, `i < 0x40` 이면 `+0x22` 로 | |
| `+0x48` | RET (`EAX = F`) | |

- 점프 목적지 (결과서가 `목적지 − 0x1404eea6c` 로 셈, flow-writer 대조): `+0xd JZ` → `+0x4a`, `+0x2e JZ` → `+0x3d`, `+0x33 JZ` → `+0x3b`, `+0x46 JC` → `+0x22`.
- `EnabledFeatures` · `EnabledSupervisorFeatures` · `AllFeatureSize` 는 **이 함수가 안 읽는다.** 마스크를 거르는 건 caller 몫 (아래 2).
- 검산 (결과서, 이 VM 값): 마스크 `0x8e7` (`0xe7 | 0x800`) → `0x240 + 0x100 + 0x40 + 0x200 + 0x400 + 0x10 = 0x990` = 사진 `AllFeatureSize`. 마스크 `0xe7` → `0x980` = 사진 `Size`. 둘 다 맞다.

### 2. CET 스레드 길이를 이 VM 값으로 [정적 계산]

**입력이 어디서 오나** (모두 `.asm` 줄):

1. `PspInitializeThunkContext`: `+0x5c` 현재 스레드 → `+0x69 ~ +0x6c` `[KTHREAD+0x74] & 0x100000` (`CetUserShadowStack`) → 켜짐이면 `R14D = 0x800` (`+0x72 ~ +0x79`), `R12D = 0x10005b` (`+0x84 ~ +0x8d`). `+0x98` `r8d ← R14D`, `+0x9f` `ecx ← R12D`, `+0xa2` Length2 (run55 와 같은 줄).
2. `RtlpValidateContextFlags2` (`ecx = 0x10005b`):
   - `+0x4d ~ +0x8e` `0x10005b & 0x100040 = 0x100040` → XState 요청으로 봄 (`CL = 0`).
   - `+0x90` `[RBX]` (= `XState.EnabledFeatures`, 이 VM `0xe7` ≠ 0) → `+0x96` `R10D = 3`.
   - `+0x9c ~ +0xa7` `0x10005b & 0x100080 = 0x100000` ≠ `0x100080` → CET 커널 검사 (`+0xa9` `KiKernelCetEnabled`) 안 탐.
   - `+0xc2` Length3 분기 칸 `[RSP+0x20] = 3`.
3. Length3:
   - `EBX = 0x4f0`, `R11D = 0x10` (run55 와 같은 길, `+0x4d ~ +0x7e`).
   - `+0x81` 분기 칸 bit 1 켜짐 → XState 블록 탐. `+0x88 ~ +0x92` `EBX = (0x4f0 − 1 + 0x10) & ~0xf = 0x4f0`.
   - `+0x94 ~ +0x99` `ControlFlags` bit 1 켜짐 (이 VM) → `+0x9b ~ +0xb7` 마스크 `RBP = r8 & (EnabledUserVisibleSupervisorFeatures | EnabledFeatures | bit63)` → `+0xc1` `RtlpRemoveArchDisallowedXStateFeatures(ecx = 0x10005b, &마스크)`.
   - `+0xcb ~ +0xd1` `rdx = RSI` (= `&XState`), `rcx = 마스크` 로 Length2 부름.
   - `+0xd6 ~ +0xdf` `EBX = 0x4f0 − 0x10 − 0x1c0 + F = 0x320 + F`.
   - `+0xe1` 분기 칸 bit 2 꺼짐 (`3 & 4 = 0`) → `+0xe9 CMOVZ` → `+0xec ~ +0xee` `0x320 + F − 1 + 0x10` = **`0x32f + F`** → `+0xf1` `*rdx` 에 씀.
4. 마스크 거르기 (`RtlpRemoveArchDisallowedXStateFeatures`): `+0x0 BT ECX,0x10` 꺼짐 → `+0x15 BT ECX,0x14` 켜짐 → `+0x1b` `RAX = 0x4000000000060dff` → `+0x25 JMP` → `+0x10 AND [RDX],RAX`. `0x800 & 0x4000000000060dff = 0x800` (bit 11 남음).

**이 VM 의 `KUSER_SHARED_DATA.XState` 값** `[관찰]` (run44 사진, `ms/logs/04_baggage-read_supp_P1.txt`, 8장 모두 같은 줄 번호 · 같은 값):

| XState 칸 | 값 | KUSD 주소 | 로그 줄 |
|---|---|---|---|
| `EnabledFeatures` (+0x0) | `0xe7` (비트 0 · 1 · 2 · 5 · 6 · 7) | `+0x3d8` | `:348` 둘째 qword |
| `Size` (+0x10) / `ControlFlags` (+0x14) | `0x980` / `3` (`OptimizedSave` 1 · `CompactionEnabled` 1) | `+0x3e8` | `:349` 둘째 qword `00000003'00000980` |
| `EnabledSupervisorFeatures` (+0x218) | `0x800` | `+0x5f0` | `:382` 첫 qword |
| `AlignedFeatures` (+0x220) | `0` | `+0x5f8` | `:382` 둘째 qword |
| `AllFeatureSize` (+0x228) | `0x990` | `+0x600` | `:383` 첫 qword 아래 dword |
| `AllFeatures[2 · 5 · 6 · 7]` | `0x100 · 0x40 · 0x200 · 0x400` | `+0x60c · +0x618 · +0x61c · +0x620` | `:383` · `:384` · `:385` |
| **`AllFeatures[11]`** (CET_U) | **`0x10`** | `+0x630` | `:386` 첫 qword 아래 dword |
| `AllFeatures[12]` (CET_S) | `0x18` | `+0x634` | `:386` 첫 qword 위 dword |
| `EnabledUserVisibleSupervisorFeatures` (+0x330) | `0x800` | `+0x708` | `:399` 둘째 qword |
| `AllNonLargeFeatureSize` (+0x340) | `0x990` | `+0x718` | `:400` 둘째 qword |

**셈**:

1. 마스크 = `0x800 & (0x800 | 0xe7 | bit63) = 0x800` → 거르기 후도 `0x800`.
2. `F = 0x240` → `i = 11` 만 마스크에 있음, `AlignedFeatures` 0 이라 올림 없음 → `+ 0x10` → **`F = 0x250`**.
3. 길이 = `0x32f + 0x250` = **`0x57f`**.

**나란히** (이 VM):

| | 비-CET (test.exe 첫 스레드로 보임) | CET 스레드 |
|---|---|---|
| `ecx` / `r8` | `0x10001b` / `0` | `0x10005b` / `0x800` |
| 분기 칸 `[RSP+0x20]` | 1 (XState 블록 건너뜀) | 3 (탐) |
| Length2 호출 | 없음 | `rcx = 0x800`, `rdx = &XState` → `0x250` |
| `*rdx` (맡긴 문맥 길이) | **`0x4ff`** (run55) | **`0x57f`** |

- 차이 `0x80` (= `F − 0x1d0`).
- 이 숫자가 기대는 가정 (결과서): `ControlFlags` bit 1 켜짐 · `AlignedFeatures = 0` · `AllFeatures[11] = 0x10` · `EnabledUserVisibleSupervisorFeatures` 에 bit 11. 다른 CPU 면 달라진다:
  - 마스크에 bit 11 이 안 남으면 `F = 0x240` → `0x56f`.
  - `ControlFlags` bit 1 이 꺼져 있으면 `F = Size` → (이 VM `Size` 로 치면) `0xcaf`.
- `[관찰]` 은 입력 값 (사진) 뿐이다. CET 스레드 길이를 디버거로 본 적은 없다.

### 3. caller 와 시동 길 [정적]

- **시동 길에서 닿는 곳 둘** — 둘 다 `RtlpValidateContextFlags2` 가 낸 칸 bit 1 (XState 요청) 이 켜질 때만:
  - `RtlGetExtendedContextLength3 +0xd1` (`PspInitializeThunkContext +0xa2` 의 길이 묻기) — 게이트 `+0x81 / +0x86`.
  - `RtlInitializeExtendedContext2 +0x15f` (`PspInitializeThunkContext +0xf0` 의 문맥 채우기) — 게이트 `+0xa3 TEST R15B,2` / `+0xa7 JNZ → +0x104`.
    - `+0x11f` `rdx` 칸 `R8 = 0xfffff780000003d8`, `+0x129 ~ +0x14f` 압축 켜짐이면 `RBP &= ([0xfffff78000000708] | [R8] | bit63)` 뒤 거르기. `RBP` 는 호출 인자 `r9` — 시동 길에선 `PspInitializeThunkContext +0xe3 MOV R9,R14` (CET 면 `0x800`).
    - 돌아와서 `+0x169 LEA R14D,[RAX−0x200]` → `+0x173 memset(RSI, 0, R14D)`. CET 면 `0x250 − 0x200 = 0x50` 바이트. `RSI` 는 `+0x10d ~ +0x111` `(RBX + 0x5f) & ~0x3f`. 이 `0x50` 이 XSAVE 머리 `0x40` + CET_U `0x10` 이라는 뜻은 `[추측]`.
  - 비-CET (`0x10001b`) 는 bit 1 이 꺼져 둘 다 안 닿는다 → **test.exe 첫 스레드는 이 함수를 안 부른다** (run44 `ContextFlags 0x10001b` 와 맞음).
- **나머지 여섯** (결과서 `objdump`): `RtlpGetEntireXStateAreaLength` (`rdx` 고정 `0xfffff780000003d8`, 24바이트 감싸개) · `RtlpGetNonLegacyXStateAreaLength` (돌아와서 `− 0x200`) · `RtlpWalkFrameChain` · `RtlDispatchException` · `RtlUnwind` · `RtlUnwindEx` (넷은 돌아와서 `+ 0x320`). 문맥 크기 묻기 · 예외 풀기 · 스택 걷기 길이고 시동 문맥과 따로다. 누가 부르는지는 이번에 안 따라감.
  - 넷의 `+ 0x320` 과 Length3 의 `0x4f0 − 0x10 − 0x1c0 = 0x320` 은 같은 기본값이다. "문맥 + 확장 문맥 머리" 라는 이름은 `[추측]`.
  - `RtlDispatchException` · `RtlUnwind` · `RtlUnwindEx` 는 `[KUSD+0x3ec]` (= `ControlFlags`) bit 1 이 켜지면 먼저 거르고 같은 call 자리로 온다 → 압축 켜짐 · 꺼짐 모두 Length2 call 을 거친다.
- **CET 갈래 조건 한 줄**: `PspInitializeThunkContext +0x69 ~ +0x6c` 의 `[KTHREAD+0x74] & 0x100000` (`CetUserShadowStack`) 켜짐 → `ecx = 0x10005b` · `r8 = 0x800` → `RtlpValidateContextFlags2` 가 (`ecx & 0x100040 == 0x100040` 이고 `XState.EnabledFeatures ≠ 0` 이면) 분기 칸 bit 1 을 켬 → Length3 XState 블록 → `+0xd1` 에 닿음. 꺼진 스레드는 블록을 건너뛰어 `0x4ff`.

### 호출 관계

```
(CET 스레드일 때 — test.exe 첫 스레드는 비-CET 라 (*) 줄을 안 탐)
PspUserThreadStartup
 └─ +0xb3  PspInitializeThunkContext
         │  +0x69 ~ +0x8d  _KTHREAD+0x74 bit20 → R14D 0x800 · R12D 0x10005b
         ├─ +0xa7  RtlGetExtendedContextLength2   (run55)
         │       └─ +0x13  RtlGetExtendedContextLength3
         │               ├─ +0x2f  RtlpValidateContextFlags2   분기 칸 3 (비-CET 1)
         │               │  +0x81  bit 1 켜짐 → XState 블록 (*)
         │               ├─ +0xc6  RtlpRemoveArchDisallowedXStateFeatures   (*) 마스크 0x800 그대로
         │               ├─ +0xd6  RtlpGetEntireXStateAreaLength2   (*) F = 0x240 + AllFeatures[11] = 0x250
         │               │  +0xf1  *rdx = 0x32f + F = 0x57f   (비-CET 0x4ff)
         │  +0x17a ~ +0x187  R15 = (원래 Rsp − 길이) & ~0xf
         └─ +0xf5  RtlInitializeExtendedContext2
                 ├─ +0x154  RtlpRemoveArchDisallowedXStateFeatures   (*)
                 ├─ +0x164  RtlpGetEntireXStateAreaLength2   (*) 0x250
                 └─ +0x178  memset   (*) 0x50 바이트
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄.

## 확인된 것

- `[정적]` `RtlpGetEntireXStateAreaLength2` = rva `0x4eea6c`, 77바이트. 식: `ControlFlags` bit 1 꺼짐 → `Size`, 켜짐 → `0x240` + 마스크 비트 (2 ~ 63) 마다 (정렬 비트면 64 올림) `AllFeatures[i]` (`.asm` `+0x0 ~ +0x4d`).
- `[정적]` 읽는 XState 칸은 `Size` · `ControlFlags` · `AlignedFeatures` · `AllFeatures[i]` 넷뿐.
- `[관찰]` 이 VM `XState` 값 (run44 사진 8장 같음 — flow-writer 가 `0x3d0 ~ 0x710` 줄 묶음을 8장에서 같은지 맞춤): 위 표.
- `[정적 계산]` 이 VM CET 스레드 맡긴 문맥 길이 = **`0x57f`**, 비-CET = `0x4ff` (run55). 식 검산 `0x990` · `0x980` 이 사진 `AllFeatureSize` · `Size` 와 같다.
- `[정적]` caller 8곳 (`refs` + `_nodes.tsv` `callers_total`). 시동 길 둘 (`Length3 +0xd1` · `RtlInitializeExtendedContext2 +0x15f`) 은 XState 요청 비트가 켜질 때만 닿는다 → 비-CET 첫 스레드는 이 함수를 안 부른다.
- `[관찰]` + `[정적]` 어긋나지 않음: 사진 `AllFeatures[11] = 0x10` (16바이트) 과 run111 의 맡긴 문맥 CET_U 칸 두 qword (`[+0]` · `[+8]`) 은 크기가 같다. 칸 하나하나를 짝 맞춘 건 아니다 (아래 "아직 확인 필요" 정적 ②).

## 정정

- 우리 문서엔 틀린 곳이 없다. run55 의 "CET 면 `0x32f + RtlpGetEntireXStateAreaLength2(…)`" 은 그대로 맞고, 이번에 그 안이 풀렸다 (run55 맨 위 "닫힘 (run122)").

## 아직 확인 필요

동적 (모두 **선택** — CET 스레드가 실제로 생길 때만 뜻이 있음. test.exe 첫 스레드는 비-CET 로 보임. 결과서 "동적분석으로 확인해야 할 것"):

- `nt+0x4eea6c` (`RtlpGetEntireXStateAreaLength2` 입구) `r rcx` = `0x800`, `r rdx` = ``fffff780`000003d8``, `nt+0x4ee95e` (Length3 `+0xd6`) `r eax` = `0x250`. CET 스레드가 없으면 안 걸린다 (예외 길 caller 여섯이 걸 수 있으니 프로세스 · 스레드 조건 필수).
- 사진을 새로 찍을 일이 있으면 ``dq fffff780`000003d8 L0x69`` 한 줄로 XState 칸 확인 (이 VM 8장은 같아서 값이 바뀐 건 아님).
- "caller 들이 실행 때 불린다" 는 정적으로 말할 수 없다 (부르는 코드가 있다는 것만).

정적 (결과서 "다음 요청 후보" — Q105 는 닫힘, 둘 다 낮음):

- ① `RtlpWalkFrameChain` · `RtlDispatchException` 의 `rdx` (`RtlDispatchException` 은 `r15`) 가 어디서 오나 — 이번엔 안 따라감. 예외 길 (run81 · run82) 에서 필요해지면.
- ② `KeVerifyContextXStateCetU` / `KiVerifyContextXStateCetUEnabled` (이미 `.asm` 있음) 와 `AllFeatures[11] = 0x10` 짝 맞추기 (맡긴 문맥 CET_U 칸 크기).

## 도구 메모

- 결과서는 사진을 **KUSD 주소** 로만 인용했다 (로그 줄 번호 없음). flow-writer 가 로그로 맞춘 줄: `04_baggage-read_supp_P1.txt` `:286` 명령, `:348` (`+0x3d0` 줄) · `:349` (`+0x3e0`) · `:382 ~ :386` (`+0x5f0 ~ +0x630`) · `:399 ~ :400` (`+0x700 ~ +0x710`). 8장 (K1 · K2 · K3 · P0 · P1 · P1a · P1b · P2) 모두 명령 `:286`, `+0x3e0` 줄 `:349`, `+0x630` 줄 `:386`, `+0x700` 줄 `:399` 로 같고, `+0x3d0 ~ +0x710` 줄 묶음 내용도 8장 같다. 칸 이름과 값 짝은 결과서와 모두 맞다 (어긋남 없음).
- 덤으로 본 칸 (셈엔 안 씀): `+0x3e0` `EnabledVolatileFeatures` `0xe7`, `AllFeatures[0] = 0xa0` · `[1] = 0x100` (`:383`), `AllFeatures[9] = 8` (`:385`).

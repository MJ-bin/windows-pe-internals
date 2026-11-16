# 2026-11-16 (작성일) run111 — `NtContinue` 길 CET 남은 정적 셋 (Q75, 정적만): 맡긴 문맥 CET_U 칸의 SSP 는 **시동 스레드가 커널에서 `PspGetContext +0x33d RtlXSaveS` 의 `XSAVES` 로 그 순간 담은 값 S0** 이고, `RtlCopyContext` 는 그 칸을 **바이트 그대로 복사만** 한다 (`RtlpCopyXStateChunk`). S0 의 처음 값은 스택 만들 때 `PspSetupUserShadowStack` 이 초기 문맥에 넣는다. `PspGetSetContextInternal` 은 5번째 칸 값을 **안 쓴다** (X). 조각 `FUN_1406f9e12` 는 `PopFxIdleDevicesFromSx` 꼬리이고, run42 가 본 `WRUSSQ`+`WRMSR 0x6a7` 줄 (`nt+0x6f9f06`) 은 **`RtlCreateEnclaveReturnFrame` 의 떨어진 조각** (엔클레이브용, `NtContinue` 길과 무관 — run42 정정). 이 VM 의 첫 스레드는 비-CET (`0x10001b`) 라 이 사슬은 test.exe 에서 안 돈다 `[관찰]` run44.

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표와 호출 관계 트리.)

> **지도 범위**: 그림은 안 고쳤다. 이번에 나온 길 (`PspSetupUserShadowStack` · `PspGetContext` 의 XState 갈래 · `RtlpCopyXStateChunk`) 은 **CET 스레드일 때만** 타는 곳이고, 이 VM 의 test.exe 는 안 탄다. 글은 flow.md 2.1 · 2.3 · 2.10 · 3장 · 4장에만.

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1`** (기준과 같음 — 결과서).
  - 다시 analyze 안 함. 새로 뽑은 것 13개 (아래 "방법"), 나머지는 이미 있던 `.asm` 읽기만.
- 버전 대조 (결과서): `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다.
- **이 VM 이 비-CET 인 근거** (새로 본 것 아님, 앞 run 값):
  - `[관찰]` run44 P1 맡긴 `_CONTEXT` 의 `ContextFlags` = **`0x10001b`** (XState `0x40` 없음 — flow.md 2.3 `PspInitializeThunkContext`).
  - `[관찰]` 짐 지도 3.14 `_KTHREAD.MiscFlags.CetUserShadowStack` (비트 20) 사진 `끝까지 0` (K2 ~ P2), 프로세스 `MitigationFlags2` K1 `0x40000000` (비트 14 `CetUserShadowStacks` 없음 — sast/44).
- **sast/108 ~ 120 결과는 이 문서에 안 넣었다** (다음 바퀴들 몫).

## 목적

- **Q75** (run42 가 남긴 `NtContinue` 길 CET 작은 정적 셋):
  - (a) 시동 문맥 (CET 스레드면 `0x10005b`) 의 XState **CET_U 칸 SSP 값은 어디서 오나** — 누가 채우고, `RtlCopyContext` 는 그 칸을 어떻게 다루나.
  - (b) `PspGetSetContextInternal` 이 run42 의 "5번째 칸" 값을 쓰나.
  - (c) 이름 없는 조각 `FUN_1406f9e12` 는 어느 함수 조각인가.

## 방법

- 정적: `_workspace/sast/106_cet-context-small.md` — ntoskrnl, 모두 depth 0 `^이름$`.
  - 새로 뽑음 13개: 요청 대상 `RtlpCopyXStateChunk` · `RtlpCopyKernelCetChunk` · 조각 `FUN_1406f9e12`, 질문 3 부모 확인 `PopFxIdleDevicesFromSx` · `RtlCreateEnclaveReturnFrame`, 질문 1 한 걸음 아래 `KiGetAllocatedXSaveArea` · `RtlXSaveS` · `RtlCopyXStateArea` · `PspSetupUserShadowStack` · `PspReserveAndCommitUserShadowStack`, 질문 2 `KeVerifyContextXStateCetU`, 플래그 비트 풀이 `RtlpValidateContextFlags` · `RtlpValidateContextFlags2`.
  - 읽기만: `KeContextFromKframes` · `PspGetContextThreadInternal` · `PspGetSetContextInternal` · `RtlCopyContext` · `PspGetContext` · `PspGetSetContextSpecialApc` · `PspInitializeThunkContext` · `KiInitializeContextThread` · `KxContextToKframes` · `SwapContext` · `KePopulateContinuationContext` · `KeVerifyContextIpForUserCet` · `KiVerifyContextIpForUserCet` · `KeVerifyContextRecord` · `PspAllocateThread`.
  - 조각의 부모: PE `.pdata` (`RUNTIME_FUNCTION`, `UNW_FLAG_CHAININFO`) 를 스크래치 `pefile` 스크립트로 읽음 (커밋 안 함). `pdb_aliases.py --where ntoskrnl.exe 0x6f9e12` · `0x6f9f06` → "PDB 공개 심볼에 없음".
- 결과서: `_workspace/sast/106_cet-context-small_result.md` (**분석성공** — 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `.asm` 줄: `PspAllocateThread +0x5fa` · `+0x601` · `+0xb65` · `+0x9eb` · `+0x9f2` (실행 순서, 아래 "결과서와 다르게 적은 곳"), `PspGetContext +0x304` · `+0x33d` · `+0x3f7`, `RtlXSaveS +0x3d` (`XSAVES [RCX]`), `PspSetupUserShadowStack +0xf6` · `+0x121` · `+0x13c` · `+0x140` · `+0x144`, `PspReserveAndCommitUserShadowStack +0x93` · `+0x12c` · `+0x136`, `RtlCopyContext +0x11c` · `+0x1b3`, `RtlpCopyXStateChunk +0x277`, `KePopulateContinuationContext +0x17` · `+0x1c` · `+0x25` · `+0x30` · `+0x36`, `PspGetSetContextInternal +0x2ee` · `+0x397` · `+0x587`, `KeVerifyContextXStateCetU +0x242` · `+0x24b`, `KiInitializeContextThread +0x3dc` · `+0x3e5`, `PspAllocateThread +0xb3f` · `+0xb60`, `PopFxIdleDevicesFromSx +0xc2` · `+0xd1`, `RtlCreateEnclaveReturnFrame +0x1f` · `+0x2c` · `+0x243f7e` · `+0x243f83` · `+0x243f90` · `+0x243f9d` · `+0x243f9f`.
  - grep: `FUN_1406f9e12.asm` 12줄에 `RDMSR`/`WRMSR` 0줄, `RtlpCopyXStateChunk.asm` 에 `RDMSR`·`WRMSR`·`XSAVE*`·`XRSTOR*` 0줄.
  - `_calls.tsv`: 아래 표의 call · ret 줄 전부. `KeContextFromKframes` 를 부르는 줄은 `KiDispatchException +0x13d` · `KiInitializeUserApc +0x1fb` 둘뿐.
  - `alias:` 줄 하나: `RtlpCopyXStateChunk +0x277` 의 대상 (아래 표 "메모리 복사 함수").

## 결과

비유 (그림자 스택 = 계단 옆 **비상 메모판**):

- CET 스레드는 함수를 부를 때마다 진짜 스택 말고 비상 메모판에도 "돌아올 자리" 를 적는다. 메모판 맨 위 위치가 SSP 다.
- 스레드를 만들 때 관리인 (`PspSetupUserShadowStack`) 이 새 메모판을 걸고, **"여기서 시작"** 위치 S0 을 스레드 첫 쪽지 (초기 문맥) 의 CET 칸에 적는다.
- 출근 직전 (`PspInitializeThunkContext`) 관리사무소가 그 순간 CPU 상태를 사진 찍듯 통째로 받아 적고 (`XSAVES`), 그 쪽지를 사물함 (유저 스택) 에 **글자 그대로 베껴** 넣는다 (`RtlCopyContext`). 그다음에 메모판에 "본업 자리" 한 줄을 더 적는다 (`KePopulateContinuationContext`, S0 → S0−8).
- 그래서 사물함 쪽지의 CET 칸은 **한 줄 더 적기 전 위치 S0** 이다. 이 VM 은 메모판을 아예 안 쓰는 스레드라 이 이야기 전체가 안 일어난다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!PspAllocateThread` | `+0xb60` | `+0xb65` | `PspSetupUserShadowStack` | 프로세스 `+0x754` bit 14 → `+0xb3f BTS [thread+0x74],0x14` 로 스레드 `CetUserShadowStack` 켬 → `+0x5fa TEST [thread+0x74],0x100000` / `+0x601 JNZ +0xb49`. **CET 스레드만.** 끝나면 `+0xb65 JMP +0x9eb` → `+0x60c` 로 돌아와 `KeInitThread` (`+0x6f4`) **앞** | [정적] |
| `nt!PspSetupUserShadowStack` | `+0xd5` / `+0x164` | `+0xda` / `+0x169` | `KiStackAttachProcess` / `KiUnstackDetachProcess` | 새 프로세스 주소 공간에 붙었다 떨어짐 (이름만) | [정적] |
| `nt!PspSetupUserShadowStack` | `+0xf6` | `+0xfb` | `PspReserveAndCommitUserShadowStack` | 섀도 스택 잡기 → SSP 값 돌려받음 | [정적] |
| `nt!PspReserveAndCommitUserShadowStack` | `+0x93` | `+0x98` | `MmAllocateUserStack` | 이름만 | [정적] |
| `nt!PspReserveAndCommitUserShadowStack` | `+0xdd` · `+0x11d` | `+0xe2` · `+0x122` | `ZwAllocateVirtualMemory` | 이름만 | [정적] |
| `nt!PspSetupUserShadowStack` | `+0x121` | `+0x126` | `RtlLocateExtendedFeature(문맥+0x4d0, 0xb)` | 초기 문맥 플래그 `0x100040` 일 때 (`+0x105 ~ +0x111`). 찾은 칸에 `+0x13c ~ +0x140 [칸+8] = SSP`, `+0x144 [칸] = 1` | [정적] |
| `nt!PspAllocateThread` | `+0x6f4` | `+0x6f9` | `KeInitThread` | 모든 스레드 (flow.md 2.1) | [정적] (run13 그대로) |
| `nt!KeInitThread` | `+0x2fc` | `+0x301` | `KiInitializeContextThread` | 모든 스레드 | [정적] (run13 그대로) |
| `nt!KiInitializeContextThread` | `+0x3c6` / `+0x3dc` | `+0x3cb` / `+0x3e1` | `RtlLocateExtendedFeature` / `RtlLocateSupervisorFeature` | 초기 문맥 플래그 `0x100040` 일 때 (`+0x3a7 ~ +0x3b5`). 초기 문맥 칸 → 스레드 저장 영역 CET_U 자리로 16바이트 (`+0x3e1 MOVUPS` · `+0x3e5 MOVDQU`) | [정적] |
| `nt!PspInitializeThunkContext` | `+0x168` | `+0x16d` | `PspGetContextThreadInternal(현재 스레드, 임시 버퍼, 0, 1, 0)` | 모든 스레드 (2.3) | [정적] (run16 그대로) |
| `nt!PspGetContextThreadInternal` | `+0x11e` | `+0x123` | `PspGetSetContextSpecialApc` | 현재 스레드라 `+0xe9 CMP` / `+0xec JNZ` 안 뜀 → APC 없이 바로 | [정적] |
| `nt!PspGetSetContextSpecialApc` | `+0x64` | `+0x69` | `PspGetSetContextInternal` | | [정적] (run16 그대로) |
| `nt!PspGetSetContextInternal` | `+0x2ee` | `+0x2f3` | `PspGetContext` | 읽기 갈래 | [정적] |
| `nt!PspGetContext` | `+0x304` | `+0x309` | `KiGetAllocatedXSaveArea` | 플래그 `0x100040` (`+0x171 ~ +0x17b`) + 유저 트랩 (`+0x2e6 TEST byte [트랩+0x170],1`) 일 때 | [정적] |
| `nt!PspGetContext` | `+0x33d` | `+0x342` | **`RtlXSaveS`** | 위 반환값 0 일 때 (보통). 마스크 = (KUSD `+0x708` \| `+0x3d8`) & `~3` | [정적] |
| `nt!PspGetContext` | `+0x3f7` | `+0x3fc` | `RtlCopyXStateArea` | 반환값 ≠ 0 일 때 (`thread+0x198` 사슬 끝 `[+0x28]` 의 저장 영역을 복사) | [정적] |
| `nt!RtlXSaveS` | `+0x46` | `+0x4b` | `RtlXSave` | KUSD `ControlFlags` bit 1 꺼짐 (압축 저장 안 씀) 일 때 | [정적] |
| `nt!PspInitializeThunkContext` | `+0x269` | `+0x26e` | `RtlInitializeExtendedContext2(…, 0x800)` | 유저 스택 문맥 틀 | [정적] (run42 그대로) |
| `nt!PspInitializeThunkContext` | `+0x280` | `+0x285` | `RtlCopyContext(유저 스택 문맥, 플래그, 임시 버퍼)` | 모든 스레드 (2.3) | [정적] (run16 그대로) |
| `nt!RtlCopyContext` | `+0x11c` | `+0x121` | **`RtlpCopyXStateChunk`** | `+0xdb TEST BPL,2` — XState 비트 | [정적] |
| `nt!RtlpCopyXStateChunk` | `+0x277` | `+0x27c` | 메모리 복사 함수 (성분마다 통째) | 같은 주소 이름 2개, 후보: `memcpy` · `memmove` (Ghidra 는 `RtlCopyMemory+3` 로 보여 줌) | [정적] |
| `nt!RtlCopyContext` | `+0x1b3` | `+0x1b8` | `RtlpCopyKernelCetChunk` | `+0xe1 TEST BPL,4` — `플래그 & 0x100080 == 0x100080` + `KiKernelCetEnabled` 일 때만 (`RtlpValidateContextFlags2 +0xa1 ~ +0xb9`). **시동 문맥에선 안 탐** | [정적] |
| `nt!PspInitializeThunkContext` | `+0x295` | `+0x29a` | `KePopulateContinuationContext(원래 Rip)` | 스레드 bit 20 (`+0xc`) 일 때만 일함: `+0x1c RDMSR 0x6a7` → `+0x25 SUB 8` → `+0x30 WRUSSQ` → `+0x36 WRMSR` | [정적] (run16 · run29 + 이번 순서) |
| `nt!PspGetSetContextInternal` | `+0x397` | `+0x39c` | `KeVerifyContextXStateCetU(스레드, 문맥, &칸)` | 쓰기 갈래 (`+0x37d CMP byte [R14+0x58],1`). 칸 = `[RBP-0x78]`, 앞 `+0x38f` 에서 0 | [정적] |
| `nt!KeVerifyContextXStateCetU` | `+0x273` | `+0x278` | `KiVerifyContextXStateCetUEnabled` | 이름만 (sast/37 이 본 "칸 SSP ≥ 현재 SSP" 검사) | [정적] |
| `nt!PspGetSetContextInternal` | `+0x587` | `+0x58c` | `KeVerifyContextIpForUserCet` | `+0x3a4 TEST [R14+0x59],4` → `+0x564`. `+0x568 LEA R9,[RBP-0x78]` (같은 칸 주소) | [정적] |
| `nt!PspGetSetContextInternal` | `+0x552` | `+0x557` | `KeVerifyContextRecord` | `R8 = R9 = 0` (`+0x549` · `+0x54f XOR`) → 안에서 `KiVerifyContextIpForUserCet` 안 부름 | [정적] |
| `nt!KiDispatchException` · `nt!KiInitializeUserApc` | `+0x13d` · `+0x1fb` | `+0x142` · `+0x200` | `KeContextFromKframes` | **이것만이 부르는 곳** — 예외 · APC 길. 시동 길엔 없음 | [정적] (run42 · run81 자리) |
| `nt!PopFxIdleDevicesFromSx` | `+0xc2` (JZ) · `+0xd1` (JMP) | — | 조각 `FUN_1406f9e12` | 꼬리 조각 (`.pdata` 체인 부모). 조각은 `+0x3` (ret `+0x8`) `KeAbPostRelease` · `+0x8` (ret `+0xd`) `KeLeaveCriticalRegion` 뒤 `RET` | [정적] |
| `nt!RtlCreateEnclaveReturnFrame` | `+0x1f` · `+0x2c` (JNZ) | — | 조각 `0x6f9e98` (= 본체 `+0x243f10`) | 트랩 `SegCs` ≠ `0x33` 이거나 트랩 `Rip` ≠ 인자일 때. 조각 끝 `+0x243f9f JMP` 로 본체 `+0x3c` 복귀 | [정적] |

### 호출 관계

```
[스레드 만들 때 — 부모 cmd.exe 스레드, 2.1]
PspAllocateThread
 ├─ +0x6f9  KeInitThread
 │           └─ +0x301  KiInitializeContextThread
 │                       ├─ +0x3cb  RtlLocateExtendedFeature     초기 문맥 CET_U 칸 찾기     (CET 스레드만)
 │                       └─ +0x3e1  RtlLocateSupervisorFeature   저장 영역 CET_U 자리 → +0x3e5 16바이트 복사
 └─ +0xb65  PspSetupUserShadowStack                               (CET 스레드만, +0x601 JNZ)
             ├─ +0xda   KiStackAttachProcess
             ├─ +0xfb   PspReserveAndCommitUserShadowStack       +0x136 SSP = 시작 + 크기 − 0x1000
             │           ├─ +0x98   MmAllocateUserStack
             │           └─ +0xe2 · +0x122  ZwAllocateVirtualMemory
             ├─ +0x126  RtlLocateExtendedFeature(문맥+0x4d0, 0xb)  → +0x140 [칸+8] = S0 · +0x144 [칸] = 1
             └─ +0x169  KiUnstackDetachProcess

   (실행 순서는 PspSetupUserShadowStack 이 먼저: +0x601 JNZ +0xb49 → +0xb60 → +0xb65 JMP +0x9eb
    → +0x9f2 JMP +0x60c 로 돌아와서 그다음 +0x6f4 KeInitThread — flow-writer .asm)

[첫 실행 — test.exe 스레드, 2.3]
PspInitializeThunkContext
 ├─ (1) +0x16d  PspGetContextThreadInternal(현재 스레드, 임시 버퍼)
 │               └─ +0x123  PspGetSetContextSpecialApc ─ +0x69  PspGetSetContextInternal ─ +0x2f3  PspGetContext
 │                                                         XState 갈래 (0x100040 + 유저 트랩):
 │                                                         ├─ +0x309  KiGetAllocatedXSaveArea   (thread+0x198)
 │                                                         ├─ +0x342  RtlXSaveS   0 일 때: +0x3d XSAVES → 칸 [+8] = 그 순간 SSP (S0)
 │                                                         └─ +0x3fc  RtlCopyXStateArea   ≠ 0 일 때: 저장돼 있던 값
 ├─ (2) +0x26e  RtlInitializeExtendedContext2(…, 0x800)
 ├─ (3) +0x285  RtlCopyContext(유저 스택 문맥, 플래그, 임시 버퍼)
 │               ├─ +0x121  RtlpCopyXStateChunk   성분 2 ~ 63 을 통째 복사 (CET_U 칸도 그대로)
 │               │           └─ +0x27c  메모리 복사 함수
 │               └─ +0x1b8  RtlpCopyKernelCetChunk   (커널 CET 칸 — 0x100080 일 때만, 시동엔 안 탐)
 ├─ (4) +0x29a  KePopulateContinuationContext   RDMSR 0x6a7 → −8 → WRUSSQ Rip → WRMSR   (S0 → S0−8)
 └─ (5) +0x2db  PspSetContextThreadInternal …     (run16 그대로)

[로더 끝 NtContinue, 2.10 — run42 그대로]
KiContinuePreviousModeUser → KeContextToKframes → KxContextToKframes +0x211 XRSTORS   칸 값 S0 을 되돌림 [추측: MSR 로]

[엔클레이브 — 지도 밖]
RtlCreateEnclaveReturnFrame   (부르는 CALL 줄 없음 — 간접)
 └─ +0x1f · +0x2c JNZ → 조각 +0x243f10 ~ : 유저 스택 프레임 · 트랩 Rsp 바꿈 ·
                         +0x243f7e MOV ECX,0x6a7 → +0x243f83 RDMSR → −8 → +0x243f90 WRUSSQ → +0x243f9d WRMSR
                         → +0x243f9f JMP 본체 +0x3c
PopFxIdleDevicesFromSx (전원)
 └─ +0xc2 · +0xd1 → 조각 FUN_1406f9e12 (0x27바이트): KeAbPostRelease · KeLeaveCriticalRegion · RET
```

### (a) CET_U 칸 SSP 는 어디서 오나 (결과서 질문 1)

- **한 줄 답** `[정적]`: 맡긴 문맥의 CET_U 칸 (16바이트: `[+0] = 1`, `[+8] = SSP`) 의 SSP 는 **시동 스레드가 커널에 있는 그 순간 `PspGetContext +0x33d RtlXSaveS` 가 `XSAVES` 로 담은 값 S0** 이다 (스레드 `+0x198` 이 0 일 때). `RtlCopyContext` 는 값을 안 만들고 바이트 그대로 복사만 한다.
  - `XSAVES` 가 CET_U 요소로 MSR `0x6a7` (`IA32_PL3_SSP`) 를 읽는다는 건 코드에 안 보이는 CPU 설명서 지식 `[추측]`. 이 길에 `RDMSR` 줄은 없다.
- 사슬 (CET 스레드일 때):
  1. **처음 값 S0** — 스레드 만들 때 `PspAllocateThread +0xb60 PspSetupUserShadowStack` → `+0xf6 PspReserveAndCommitUserShadowStack` 이 돌려준 값 = **섀도 스택 시작 + 크기 − 0x1000** (`+0x12c LEA RCX,[R14-0x1000]` · `+0x133 ADD RCX,RBX` · `+0x136 MOV [RAX],RCX`; `R14` 를 크기로 읽은 건 `[추측]`). 그 값을 `PspSetupUserShadowStack +0x13c ~ +0x144` 가 초기 문맥 CET_U 칸에 쓴다 (`+0x132 BTS` 로 XState 마스크 bit 11 도). 섀도 스택 시작은 `+0x14f MOV [R14+0x40],RAX` 로 따로 저장, `+0x153 OR byte [R15],8`.
  2. **스레드 저장 영역으로** — `KiInitializeContextThread +0x3e1 ~ +0x3e5` 가 초기 문맥 칸 16바이트를 스레드 저장 영역 CET_U 자리로 복사. 같은 함수 `+0x2a8` · `+0x2af` 가 저장 영역 머리에 bit 11, `+0x233 ~ +0x25b` 가 `NpxState` 에 `0x800` (짐 지도 3.14 `NpxState` 줄과 같은 조건).
  3. **처음 돌 때 MSR 로** — `SwapContext` 들어오는 쪽 `XRSTORS [RSI+0x60]` (조각 `FUN_1406b686f +0x1b1` = `SwapContext +0x4e0`) 가 마스크에 `[RSI+0x250]` 을 넣는다. CET_U 가 MSR 로 올라간다는 건 `[추측: CPU 설명서]` (결과서 "다음 요청 후보" 2).
  4. **시동 길에서 읽어 담기** — `PspInitializeThunkContext +0x168` → (현재 스레드라 바로) `PspGetContextThreadInternal +0x11e` → `PspGetSetContextSpecialApc +0x64` → `PspGetSetContextInternal +0x2ee` → `PspGetContext`. XState 갈래 `+0x2e6 ~ +0x319` 에서 `KiGetAllocatedXSaveArea` 가 0 이면 `+0x33d RtlXSaveS` (`+0x20 OR [영역+0x208],bit63` · `+0x29 XRSTORS` (RFBM 0) · `+0x2c AND` · **`+0x3d XSAVES [영역]`**), 0 이 아니면 `+0x3f7 RtlCopyXStateArea` (스레드 `+0x198` `XStateSave` 사슬 끝 `[+0x28]` 영역 복사).
  5. **복사** — `RtlCopyContext +0x11c RtlpCopyXStateChunk`: 두 쪽 XState 길이가 `0x40` 이상인지 (`+0x43 ~ +0x4e`, 아니면 `0x80000005`), 원본 마스크 `& ~3` 이 KUSD 켜진 기능 안인지 (아니면 `0xc000000d`), 성분 2 ~ 63 을 돌며 KUSD 배치 표 (`0xfffff780000003f0` · `…3f4` · `…604`) 로 칸 자리를 계산해 `+0x277` 에서 통째 복사. **CET_U (성분 11) 도 다른 성분과 똑같이 복사만.**
  6. **그 뒤 −8** — `PspInitializeThunkContext +0x295 KePopulateContinuationContext` 는 `+0x280 RtlCopyContext` **다음**이다. 그래서 맡긴 문맥 칸 = **S0** (밀어 넣기 전), 유저 코드가 처음 도는 SSP = **S0 − 8** `[정적, 순서]`. `NtContinue` 끝 `KxContextToKframes +0x211 XRSTORS` 가 칸 값 S0 을 올린다 `[추측: MSR 로]`. sast/37 의 "칸 SSP ≥ 현재 SSP" 검사와도 맞다 (S0 ≥ S0−8).
- `RtlpCopyKernelCetChunk` 는 **유저 CET_U 칸이 아니라 커널 CET (`KernelCet`) 칸** (`CONTEXT_EX +0x18` 오프셋 · `+0x1c` 길이, 24바이트 복사) 이다. `RtlCopyContext +0xe1` 의 bit 2 는 `플래그 & 0x100080 == 0x100080` + `KiKernelCetEnabled` 일 때만 켜져서 (`RtlpValidateContextFlags2 +0xa1 ~ +0xb9`) 시동 문맥 (`0x10005b & 0x100080 = 0x100000`) 에선 안 탄다. (`KeContextFromKframes +0x4fb ~ +0x54d` 가 이 칸을 트랩 `+0xd8 ShadowStackFrame` 에서 채우는 곳.)
- `KeContextFromKframes` (예외 · APC 길) 도 같은 두 갈래다: 스레드 `+0x198` 이 0 이면 `+0x207 ~ +0x225` 에 `RtlXSaveS` 와 같은 일을 풀어 씀 (`+0x225 XSAVES`), 아니면 `+0x562 ~ +0x5a1` `RtlCopyXStateArea`.
- **test.exe**: 이 VM 첫 스레드는 `0x10001b` 라 (위 "환경") **CET_U 칸이 아예 없다.** 위 사슬은 CET 스레드일 때만이다.

### (b) `PspGetSetContextInternal` 이 5번째 칸을 쓰나 (결과서 질문 2)

- **답: X (안 쓴다)** `[정적]`.
  - "5번째 칸" = run42 의 `KiVerifyContextIpForUserCet` 5번째 인자 = `KeVerifyContextIpForUserCet` 의 4번째 인자 (`R9` → `+0x5d MOV [RSP+0x20],R9`). `PspGetSetContextInternal` 쪽에선 지역 칸 `[RBP-0x78]`.
  - 쓰기 갈래: `+0x38f` 에서 0 → `+0x397 KeVerifyContextXStateCetU` 안 **`+0x242 ~ +0x24b RDMSR 0x6a7 → [RBX]`** (지금 유저 SSP 를 칸에). 문맥에 CET_U 칸이 없으면 `+0x2c0 ~ +0x2d4` 가 문맥에 칸을 만들고 `[칸] = 1`, `[칸+8] = 지금 SSP`.
  - → `+0x587 KeVerifyContextIpForUserCet` 에 같은 칸 주소 (`+0x568 LEA R9`) → 안쪽 `KiVerifyContextIpForUserCet` 이 맞으면 `+0x12c` 에서 SSP+8 로 고침 (run42).
  - **`+0x58c` 뒤에 `[RBP-0x78]` 을 읽는 줄이 없다.** 이 칸이 나오는 줄은 `+0x6c` (인자 저장) · `+0x2c0` (칸 쓰기 전 읽기) · `+0x388` · `+0x568` (주소만) · `+0x38f` (0) · `+0x602` (`VslGetSetSecureContext` 갈래 — `+0xa4 JNZ +0x5e6` 로 가는 별개 길) 뿐. 이어지는 `PspSetContext` (`+0x3ea`) · 조각 `PspSetContextState` 는 이 칸을 인자로 안 받는다 (`+0x3e0 ~ +0x3e7`).
  - `KeVerifyContextRecord` 갈래 (`+0x552`) 는 `R8 = R9 = 0` 으로 불러 안쪽 검사를 안 탄다 (`KeVerifyContextRecord +0xa8 TEST R14,R14` / `+0xab JZ`).
  - 시동 길 (읽기 갈래) 은 쓰기 갈래 `+0x37d` 로 안 가서 칸을 만지지 않는다.
- run42 의 `NtContinue` 길 (`KiContinuePreviousModeUser +0x39e` 뒤 안 읽음) 과 같은 꼴이다: **올린 SSP+8 은 어느 길에서도 죽은 값.**

### (c) 조각 `FUN_1406f9e12` 는 어느 함수 조각인가 (결과서 질문 3)

- **답** `[정적]`: `FUN_1406f9e12` (`0x6f9e12 ~ 0x6f9e39`, 0x27바이트) = **`PopFxIdleDevicesFromSx` (rva `0x4b580c`) 의 꼬리 조각** (`.pdata` 체인 부모, `PopFxIdleDevicesFromSx +0xc2 JZ` · `+0xd1 JMP`). 본문은 `KeAbPostRelease` · `KeLeaveCriticalRegion` 부르고 `RET` — **`RDMSR`/`WRMSR` 없음.**
- run42 (sast/37) 가 본 `nt+0x6f9f06` 의 `RDMSR` → −8 → `WRUSSQ` → `WRMSR 0x6a7` 은 **다른 `.pdata` 항목** `0x6f9e98 ~ 0x6f9f31` (0x99바이트) 안이고, 그 부모는 **`RtlCreateEnclaveReturnFrame`** (rva `0x4b5f88`). 줄 이름은 `RtlCreateEnclaveReturnFrame +0x243f7e` (`MOV ECX,0x6a7`) · `+0x243f83 RDMSR` · `+0x243f8c SUB RAX,8` · `+0x243f90 WRUSSQ [RAX],R9` (R9 = 트랩 `Rip`) · `+0x243f9d WRMSR`.
  - sast/37 의 "`+0xf4`" 는 Ghidra 가 `FUN_1406f9e12` 이름 뒤로 바이트 거리만 센 값이다 (`0x6f9e12 + 0xf4 = 0x6f9f06`).
  - 하는 일: 엔클레이브에서 돌아올 때 유저 스택에 프레임을 만들고, 스택 인자 (`[RSP+0x78]`) 가 0 이 아니면 그림자 스택에 돌아올 주소 한 칸을 밀어 넣는다 — `KePopulateContinuationContext` 와 같은 모양.
  - `RtlCreateEnclaveReturnFrame` 을 직접 `CALL` 하는 줄은 `_calls.tsv` 에 없다 (간접).
  - 공개 PDB 는 조각에 이름을 안 준다 (`pdb_aliases.py --where` 둘 다 "없음").
- → `NtContinue` 길 · 시동 길과 **무관**. run42 의 "`KeUserModeCallback` 쪽 `[추측]`" 은 틀렸다 (아래 "정정").

## 확인된 것

- `[정적]` 맡긴 문맥 CET_U 칸 SSP = `PspGetContext +0x33d RtlXSaveS` (`+0x3d XSAVES`) 가 담은 그 순간 값 (스레드 `+0x198` = 0 일 때) 또는 `+0x3f7 RtlCopyXStateArea` 가 옮긴 저장 값 (≠ 0 일 때). `RtlCopyContext → RtlpCopyXStateChunk` 는 복사만 한다.
- `[정적]` 처음 값 S0 = `PspSetupUserShadowStack` (`+0xb60` 갈래, CET 스레드만) 이 초기 문맥에 쓴 `시작 + 크기 − 0x1000` → `KiInitializeContextThread +0x3e5` 가 스레드 저장 영역으로.
- `[정적, 순서]` 맡긴 문맥 칸 = S0, 첫 유저 SSP = S0 − 8 (`RtlCopyContext +0x280` 이 `KePopulateContinuationContext +0x295` 보다 앞). MSR 로 오가는 건 `[추측: CPU 설명서]`.
- `[정적]` `RtlpCopyKernelCetChunk` 는 커널 CET 칸 복사기이고 시동 문맥에선 안 불린다.
- `[정적]` 시동 문맥 길에 `KeContextFromKframes` 는 없다 (부르는 곳 `KiDispatchException` · `KiInitializeUserApc` 둘뿐).
- `[정적]` `PspGetSetContextInternal` 은 5번째 칸 값을 안 쓴다.
- `[정적]` `FUN_1406f9e12` = `PopFxIdleDevicesFromSx` 꼬리. `WRUSSQ`+`WRMSR 0x6a7` 줄 = `RtlCreateEnclaveReturnFrame` 조각.
- `[관찰]` (앞 run 값) 이 VM 첫 스레드는 비-CET: run44 P1 `ContextFlags 0x10001b`, `_KTHREAD` `CetUserShadowStack` 사진 `끝까지 0`.

## 결과서와 다르게 적은 곳 · 보탠 것

- 결과서는 `.asm` 의 `WRUSSD dword ptr …` 를 바이트 (`66 4c 0f 38 f5`, REX.W) 로 **`WRUSSQ` (8바이트)** 라고 고쳐 읽었다 (`KePopulateContinuationContext +0x30`, `RtlCreateEnclaveReturnFrame +0x243f90`). 이 문서도 `WRUSSQ` 로 적었다. (run56 의 "Ghidra 가 CET 명령을 잘못 읽는다" 와 같은 종류 — 아래 도구 메모.)
- `RtlpCopyXStateChunk +0x277` 의 대상은 Ghidra 가 `alias:RtlCopyMemory+3` 으로 적었다. PDB 공개 이름은 같은 주소 (`0x6c3640`) 에 `memcpy` · `memmove` 둘 (`pdb_aliases.py --lookup ntoskrnl.exe 0x6c3640`). 결과서의 "`RtlCopyMemory`" 대신 "메모리 복사 함수" 로 적었다.
- (flow-writer 보탬) 결과서는 `PspAllocateThread` 의 `+0xb60 PspSetupUserShadowStack` 과 `+0x6f4 KeInitThread` 중 어느 쪽이 먼저 도는지 안 적었다. `.asm` 으로 보면 **`PspSetupUserShadowStack` 이 먼저** `[정적]`: `+0x5fa TEST` / `+0x601 JNZ +0xb49` → `+0xb60` (ret `+0xb65`) → `+0xb65 JMP +0x9eb` (`MOV R14D,EAX` — 돌려받은 상태값) → `+0x9f2 JMP +0x60c` (`TEST R14D,R14D` / `+0x60f JS` 실패 길) → 그 뒤 `+0x6f4 KeInitThread`. 그래서 `KiInitializeContextThread +0x3e1` 이 읽는 초기 문맥 CET_U 칸은 이미 채워져 있다 (앞뒤가 맞음). `+0x9eb` 는 WOW64 갈래 `+0x9e6 PspWow64SetupUserStack` 의 돌아오는 자리와 같은 자리다.

## 정정

- **run42** (`runs/02_sprint/run42-continue-detail.md`) "`WRMSR 0x6a7` 이 있는 곳" 표의 "이름 없는 조각 `FUN_1406f9e12 +0xf4` (`nt+0x6f9f06`) … `KeUserModeCallback` 쪽 `[추측]`" 은 틀렸다. `FUN_1406f9e12` 는 `PopFxIdleDevicesFromSx` 꼬리 조각 (MSR 줄 없음) 이고, `nt+0x6f9f06` 줄은 `RtlCreateEnclaveReturnFrame +0x243f7e` (엔클레이브용) 다. "`NtContinue` 길이 아니다" 라는 결론은 그대로 맞다. → run42 문서 맨 위에 한 줄.
- **run42** "아직 확인 필요" 정적 1 (a) 의 전제 "임시 문맥의 CET_U 칸을 `PspGetContextThreadInternal` / `KeContextFromKframes` 가 채우나" 중 **`KeContextFromKframes` 는 시동 길에 없다** (부르는 곳 `KiDispatchException +0x13d` · `KiInitializeUserApc +0x1fb` 뿐). 채우는 건 `PspGetContextThreadInternal` 아래 `PspGetContext` 다. 같은 전제가 flow.md 4장 2.10 Q75 줄에도 있어 `(해결됨)` 으로 고쳤다. → run42 맨 위 같은 줄에.

## 이전 run 에서 닫은 것

- run42 "아직 확인 필요 — 정적 (대기열 Q75)" 1 · 2 · 3 → 모두 닫힘 (위 (a) · (b) · (c)).
- flow.md 2.10 `[추측]` "시동 문맥의 CET_U 칸은 `RtlCopyContext` 가 `KePopulateContinuationContext` 보다 앞에 복사한다 … 칸 값 출처는 정적 Q75" → 출처 `[정적]` (S0 = `XSAVES`), 순서 `[정적]`, MSR 로 오가는 부분만 `[추측]` 그대로. 실제 값은 동적 Q57 그대로.

## 아직 확인 필요

### 동적 (결과서 "동적분석으로 확인해야 할 것" — **CET 스레드가 있는 환경에서만**, 이 VM 에선 급하지 않음. 대기열 번호는 오케스트레이터가 붙임)

- **1** (Q57 과 겹침) `rdmsr 6a7` 을 `nt+0x91097d` / `nt+0x910982` (`PspInitializeThunkContext +0x295` / `+0x29a`) 앞뒤 — S0 → S0−8.
- **2** 같은 `rdmsr 6a7` 을 `nt+0x910850` (`+0x168` 직전) 에서도, 임시 문맥 CET_U 칸 `[칸+8]` 과 같은지 (XSAVES 가 담은 값인지).
- **3** `nt+0x9b83d9` (`PspGetContext +0x309`) 에서 `rax` (0 이면 XSAVES 갈래) · `dq @$thread+0x198 L1`.
- **4** `nt+0x90497c` (`PspSetupUserShadowStack +0x144`) 직후 `[rax] = 1` · `[rax+8]` = S0 (= `nt+0x904b42` `PspReserveAndCommitUserShadowStack +0x136` 의 `rcx`), `nt+0x3e9f55` (`KiInitializeContextThread +0x3e5`) 의 16바이트.
- **5** (실행 없이) `ln nt+0x6f9f06` · `ln nt+0x6f9e12` — WinDbg 가 조각을 어떤 이름으로 보여 주나.

### 정적 (결과서 "다음 요청 후보")

- ① (낮음, CET 스레드 환경에서만) `PspAllocateThread` 조각 `+0x2caa12 RtlGetExtendedContextLength` · `+0x2caae1 RtlLocateExtendedFeature` 주변 — CET 스레드 초기 문맥에 CET_U 칸이 늘 있게 누가 보장하나 (`KiInitializeContextThread +0x3e1` 은 `0x100040` 이 없으면 `R14 = 0` 인 채로 읽는 모양).
- ② (낮음) `SwapContext` `XRSTORS` 마스크 조건 (`[RSI+0x250]` 의 `0x800`, `BT RBP,0x26`) 전부 — 새 스레드 첫 실행에서 CET_U 가 MSR 로 올라간다는 `[추측]` 을 코드로.
- 결과서 판단: ① · ② 가 필요 없으면 Q75 는 이번으로 닫아도 된다.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- (a) **맞음 (비-CET 갈래)**: `Src/nt/Process.cpp:1980` 이 `ContextFlags 0x10001b` 고정 ("CET 갈래 안 탐 `[관찰]` run44"), `Process.cpp:2006` 이 `KePopulateContinuationContext` 를 안 함, `Src/nt/Continue.cpp:17` 이 XState 꼬리를 안 읽음 — 결과서가 읽은 줄.
- (b) **맞음**: `Process.cpp:2006` 이 `PspSetContextThreadInternal → … → PspSetContext` 중간을 줄이고, `Continue.cpp:62` 가 `KiVerifyContextIpForUserCet` 을 안 함. 그 칸은 죽은 값이라 넣을 것이 없다.
- (c) **해당 없음**: 엔클레이브 · 전원 관리 쪽.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntoskrnl pdb 줄, 근거 로그 run111), 2.1 `PspAllocateThread` · `KeInitThread` 절 (CET 갈래 한 줄씩), 2.3 머리 · `PspInitializeThunkContext` 절 2 · 6 · 7, 2.10 머리 · `KiContinuePreviousModeUser` 6 의 `[추측]` 줄, 3장 "남의 스레드에 문맥 넣기" (CET 칸 X), 4장 Q75 줄 `(해결됨)` · 새 줄 셋 (CET 실행 확인 · 정적 남은 것 · PE_emulator), 5장 두 표. 그림은 안 고침.
- 짐 지도 (`baggage.md`): **오케스트레이터 결정 (run110 넘김) 으로 PEB `NtGlobalFlag2` 를 7.2 `끝까지 0` → 3.1 로 옮김** (2줄: nt `MmCreatePeb +0x4b2` 늘 · ntdll `LdrpInitializeExecutionOptions +0x5a9` IFEO 조건부 — 근거 run110 · sast/107, run70 `MinimumStackCommit` 과 같은 꼴). 2장 `_PEB` nt 41 → 42 · ntdll 24 → 25 · 모름 63 → 62 와 합들 (유저 합 nt 117 → 118 · ntdll 135 → 136 · 모름 195 → 194, 아는 칸 738 → 739 · 모름 301 → 300), 4장 짝 1줄 새로 (커널 전역 `NtGlobalFlag2` ↔ PEB), 7.2 · 7.41 `(해결됨)`. 이번 CET 결과는 3.14 `CetUserShadowStack` 줄 · 4장 맡긴 문맥 짝 줄에 근거만 (칸 수 그대로). 새 7.42.

## 디버거 메모 / 도구 메모

- Ghidra 11.3.2 가 `WRUSSQ` (`66 REX.W 0f 38 f5`) 를 `WRUSSD dword ptr` 로 보여 준다 (결과서). run56 의 `RDSSPQ`·`CLRSSBSY` 오독과 같은 종류 — 그림자 스택 명령 줄은 바이트로 크기를 확인할 것. → flow.md 기준 "Ghidra 가 함수로 안 잡은 곳" 줄에 같이 적음. ghidra-headless-export 쪽 함정 목록에 넣자고 보고.
- Ghidra 는 `.pdata` 상 다른 함수의 조각을 앞쪽 가까운 `FUN_` 이름 + 바이트 거리 (`+0xf4`) 로 보여 줄 수 있다. 조각 오프셋을 문서에 쓰기 전에 `.pdata` 체인 부모를 먼저 볼 것 (run33 방식).

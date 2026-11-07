# 2026-11-07 run72 — 로더 일꾼 풀을 켰을 때의 일감 길 (Q137, PE_emulator 요청 23): `TppWorkPost` 는 **넣기만 하고 돌아온다** (기다림 없음). 첫 넣기만 syscall 1 (`NtReleaseWorkerFactoryWorker`), 둘째부터 0. 재시도 길은 dll1·dll2 맵 결과가 `0x40000003`/`0x40000036` 일 때만 열린다 — 그 값은 정적으로 안 닫힘 (Q104)

> **정정 (run73, sast/68)**: 아래 "`MiCreateNewSection` 줄은 안 읽음 `[공백]`" (한 줄 결론 2 · "dll1·dll2 맵 결과" 절 · 정적 후보 첫 줄) 은 **닫혔다**: `MiCreateNewSection +0x361 MOV ESI,[RBX]` · `+0x38b SHR ESI,0x1b` · `+0x391 AND ESI,2` → `+0x3cc`/`+0x68c` 여섯째 인자라 **비트 0 은 늘 0**. 그래서 dll1·dll2 는 파일마다 다른 비트맵 칸을 받고, "헤더 그대로면 둘째 DLL 은 반드시 `0x40000003`" 갈래는 이 두 DLL 에 해당 없다 (비트 0 을 켜는 곳은 `MmLoadSystemImageEx → MiGetSystemAddressForImage` 뿐). 동적 1 의 기대 `0` 은 `[정적]` 근거가 생겼다 (실제 값은 여전히 Q104). → `run73-image-base-select.md`

## PE_emulator 쪽에서 쓸 것

### 한 줄 결론 (셋)

1. **`TppWorkPost` 는 넣기만 하고 돌아온다** `[정적]`.
   - 안에 `NtWait*` · `NtRemoveIoCompletion` · `NtWaitForWorkViaWorkerFactory` 가 **없다** (`_calls.tsv` 17줄, 깊이 1 에도 없음).
   - **첫 넣기 = syscall 1개** — `+0x4f3 / +0x4f8 NtReleaseWorkerFactoryWorker(풀 +0x38)`.
     - 풀 핸들은 **검사 안 함** (`+0x4ef MOV RCX,[RDI+0x38]` 로 꺼내 넘기기만). 그래서 **핸들 0 이어도 같은 길**이다.
     - 반환값도 **안 봄** (`+0x4f8 JMP` → `+0x4b9`). 아무 상태 (권고 `STATUS_SUCCESS`) 를 줘도 된다.
     - 기본 대답 갈래 (핸들 0 · 일꾼 0) 와 진짜 처음 넣기 (핸들 ≠ 0 · 일꾼 0) 는 **같은 길**이다. 풀의 일꾼 수 칸 (`+0x1a0 AvailableWorkerCount`) 은 아예 안 읽는다.
   - **둘째 넣기부터 = syscall 0개.** 일감 `WorkState` 의 `Insertable` 이 첫 넣기에서 0 이 되어, `+0x156 TEST EDX,EDX` · `+0x158 JZ` (→ `+0x32a`, 끝) 로 큐에도 안 넣고 돌아온다.
2. **재시도 길은 dll1·dll2 맵 결과가 `0x40000003` 또는 `0x40000036` 일 때만** 열린다 `[정적]`. 나머지 문 조건은 test.exe 에서 이미 참으로 정해져 있다 (아래 조건 표).
   - **dll1·dll2 헤더 `ImageBase` 는 둘 다 `0x180000000`** 이다 `[정적+샘플]`.
   - 그런데 커널이 DLL 마다 **다른 기준 주소**를 주는지가 안 닫혔다. 그걸 가르는 게 `MiSelectImageBase` 넷째 인자 비트 0 (= `MiRelocateImage` 여섯째 인자 `& 1`, 이걸 주는 `MiCreateNewSection` 줄은 안 읽음) `[공백]`.
     - (run73 정정) **닫힘**: `MiCreateNewSection +0x391 AND ESI,2` 라 비트 0 은 늘 0 → 아래 "0 이면" 쪽. "1 이면" 갈래는 dll1·dll2 에 해당 없음 (그 갈래는 `MmLoadSystemImageEx` 길뿐) `[정적]`.
     - 0 이면 DLL 마다 비트맵에서 따로 받음 → 보통 `0`.
     - 1 이면 둘 다 `0x180000000` → **둘째로 붙는 DLL 은 반드시 충돌 → `0x40000003` → 재시도 길.**
   - → 동적 Q104 (아래 "아직 확인 필요" 1). PE_emulator 가 dll1·dll2 에 `0` 을 주면 이 길은 안 탄다.
3. **조심**: `LdrpDetourExist` 가 켜지면 `LdrpDetectDetour +0xc4 TpWaitForWork(일감, 1)` 이 불릴 수 있다 (`+0xb3` 일감 ≠ 0 일 때).
   - 에뮬레이터가 `NtQueryInformationThread(-2, 0x2a)` 에 `1` 을 주면 이 길이다. 기본 0 이면 안 탄다.
   - 둘째 인자 1 은 "대기 중 콜백 취소" 쪽이라 일꾼 없이 막히진 않을 것으로 보인다 `[추측]`. `TpWaitForWork` 본문은 안 읽었다 `[공백]`.

### 맞춰 둘 칸 셋 (덫 `LdrpEnableParallelLoading` 을 진짜로 켤 때)

| 칸 | 값 | 누가 이미 그렇게 두나 | 이게 틀리면 |
|---|---|---|---|
| 일감 `_TP_WORK +0xe8 WorkState` 비트 0 `Insertable` | **1** | `TpAllocWork → TppWorkInitialize +0x12b` (sast/64, run69) | 0 이면 첫 넣기부터 `+0x158 JZ` 로 끝 — 큐에 안 넣고 syscall 도 없음 |
| 풀 `TaskQueue[우선순위 1][노드 0]` 큐 머리 `+0x0 Flink` · `+0x8 Blink` | **자기 자신** | `TpAllocPoolInternal +0x16d ~ +0x190` (`+0x17d`·`+0x181`·`+0x185`) | 이웃 검사 (`+0x2d2 ~ +0x2d9`) 실패 → **`+0x2e4 INT 0x29`** (즉시 중단) |
| 같은 큐 `+0x10 Lock` | **0** | `TpAllocPoolInternal +0x17d` | 잠겨 있으면 `+0x2cc JC` → `+0x3d7 RtlpAcquireSRWLockExclusiveContended` → 안에서 `NtWaitForAlertByThreadId` (막힘) |

- 같은 이유로 풀 락 (`풀 +0x48`) · 배리어 락 (`일감 +0x40`) 도 0 이어야 한다. 셋 다 풀·일감을 만들 때 0 이다.
- 우선순위 1 = 일감 `+0xc0 CallbackPriority` (sast/64 의 로더 일감 값). 노드 0 = 일감 `+0xd0 Task.NumaNode` (이 VM 은 노드 1개 `[추측]`).

### syscall 표 — `TppWorkPost` 한 번 (SSN = 이 빌드 ntdll 스텁의 `MOV EAX,imm`)

| 자리 (call / ret) | syscall (SSN) | A. 기본 대답 첫 넣기 (핸들 0 · 일꾼 0) | B. 진짜 처음 넣기 (핸들 ≠ 0 · 일꾼 0) | C. 둘째 이후 |
|---|---|---|---|---|
| `+0x4f3 / +0x4f8` | **`NtReleaseWorkerFactoryWorker`** (`0x17e`), 인자 하나 = 풀 `+0x38` | **1번** (핸들 0) | **1번** (진짜 핸들) | **0번** |
| `+0x224 / +0x229` (조건부 ①) | `NtTraceEvent` (`0x5e`) | 안 탐 — KUSD `+0x386` = 0 `[관찰]` run44 사진 (run62 대조) | 같음 | 안 탐 |
| `+0x127 / +0x12c` (조건부 ②) | `NtAlertThreadByThreadId` (`0x71`) | 안 탐 — 배리어 느린 길 (누가 이 일감을 기다릴 때) 만 | 같음 | 안 탐 |
| `+0x432` → `TppAdjustRunningThreadGoalWithLock` (조건부 ③) | `NtSetInformationWorkerFactory` (`0x1b1`, 클래스 8) + `NtQuerySystemInformationEx` (`0x16e`) | 안 탐 — 풀 `LastProcCount` = CPU 수 (`TpAllocPoolInternal +0x237` 이 채움) → `+0x424 JZ` | 같음 | 안 탐 |
| `+0x3d7` / `+0x4fd` → `RtlpAcquireSRWLockExclusiveContended` (조건부 ④) | `NtWaitForAlertByThreadId` (`0x1e3`) | 안 탐 — 락을 쥔 다른 스레드 없음 | 같음 | 같음 |
| `+0x304` · `+0x50a` · `+0x514` · `+0x3ca` (조건부 ⑤) | 해제 쪽 (`NtClose` 등, 이름만) | 안 탐 — `Refcount` 1 → 3 → 2, `+0x4c2 JNZ` | 같음 | 같음 |

- **합계: 첫 넣기 1개, 이후 0개** `[정적 조건 + 관찰 KUSD]`. 조건부 다섯은 test.exe 초기화에선 안 탄다.
- 반환값을 보는 syscall 이 하나도 없다 (`+0x4f8 JMP`, `+0x12c` 다음 `MOV RBX,RDI`, `+0x229` 다음 `MOV RDI,[RSI+0x90]`).
- 첫 넣기 뒤 일감은 풀 큐에 그대로 남는다 (일꾼이 안 가져가니까). 이후 `LdrpQueueWork` 의 `TpPostWork` 는 셈만 올린다.
  - PE_emulator 가 첫 스레드의 `LdrpDrainWorkQueue(1)` 로 **`LdrpWorkQueue`** 를 비우는 길은 이 풀 큐와 따로라 영향이 없다 (run24).

### syscall 표 — 재시도 길 (맵 결과가 `0x40000003` / `0x40000036` 일 때만)

첫 시도 (`LdrpProcessWork → LdrpMapDllSearchPath → LdrpMapDllNtFileName`, 돌아오는 값 `0xc000022d`):

| # | 자리 (call / ret) | syscall (SSN) | 비고 |
|---|---|---|---|
| 1 | `LdrpMapDllNtFileName +0x132 / +0x137` | `NtOpenFile` (`0x33`) | `0x100021`, 공유 5, 옵션 `0x60` |
| — | `+0x109 / +0x10e` | `NtSystemDebugControl` (`0x1d0`) | **조건부** — `PEB.NtGlobalFlag & 0x40000` 일 때만 (`+0xe8 JZ` → `+0x10e`) |
| 2 | `+0x18f / +0x194` | `NtCreateSection` (`0x4a`) | 이미지 섹션 `0x1000000` |
| 3 | `LdrpMapDllWithSectionHandle +0x49 / +0x4e` → `LdrpMinimalMapModule +0x174 / +0x179` | `NtMapViewOfSection` (`0x28`) | **결과가 이 길의 문** |
| — | `LdrpMinimalMapModule +0x2a9 / +0x2ae` → `LdrpCheckForRetryLoading` | (없음 — 임계 구역만) | 참 → `+0x2b6 MOV EBX,0xc000022d` · `+0x2bb JMP` |
| 4 | `LdrpMinimalMapModule +0x2d4 / +0x2d9` | `NtUnmapViewOfSection` (`0x2a`) | 붙인 뷰를 지움, `항목 +0x30 DllBase = 0` |
| 5·6 | `LdrpMapDllNtFileName +0x29b / +0x2a0` · `+0x2a4 / +0x2a9` | `NtClose` (`0xf`) ×2 | 섹션·파일 핸들 (`ctx+0xb0 = 0` 이라 닫는 쪽) |

- 첫 시도 합계 **6개** (조건부 하나 빼고). 이어 `LdrpProcessWork` 끝에서 `NtSetEvent(LdrpWorkCompleteEvent)` (`+0x1c3` 꼬리, `0xe`) — 재시도와 상관없이 일감 하나 끝날 때마다 같은 조건.
- 되돌리기 (`LdrpDrainWorkQueue(1) +0xeb ~ +0x14b`): **syscall 없음** (임계 구역만).

둘째 시도 (`LdrpProcessWork +0x149 / +0x14e LdrpMapDllRetry`):

| 자리 (call / ret) | syscall |
|---|---|
| `LdrpMapDllRetry +0x9a / +0x9f LdrpGetNtPathFromDosPath` → 그 안 `+0xee / +0xf3` | `NtQueryAttributesFile` (`0x3d`) ×1 — 반환값이 곧 이 syscall 상태 |
| `LdrpMapDllRetry +0xad / +0xb2 LdrpMapDllNtFileName` | `NtOpenFile` · `NtCreateSection` · `NtMapViewOfSection` · `NtClose` ×2 (첫 시도와 같은 함수. ctx 비트 20 이 켜져 있어 재시도 검사는 `+0x31` 에서 바로 거짓) |
| (맵 결과가 `0x40000003` 이면) `LdrpCompleteMapModule +0x31b LdrpRelocateImage` | 재배치 쪽 `NtQueryVirtualMemory` · `NtProtectVirtualMemory` (이름만, 횟수는 run38 / Q126 (5) 몫) |

- 이미 같은 모듈이 올라와 있으면 (`+0x81 / +0x86 LdrpFindExistingModule` 이 찾음) `+0x90 JNZ` → `+0xf2 / +0xf7 LdrpLoadContextReplaceModule` 로 가고 새로 안 맵한다 (syscall 없음, 그 안에서 `LdrpQueueWork` 를 또 부름).
- **재시도는 경로 찾기를 다시 안 한다.** 이미 정해진 `FullDllName` 으로 `NtQueryAttributesFile` 한 번 하고 바로 연다.

### test.exe 가 재시도 길을 타나 — 조건 표

| 조건 | 어디서 | 정해지는 때 |
|---|---|---|
| `LdrpMapAndSnapWork ≠ 0` | `LdrpMinimalMapModule +0x296` | **정해짐** `[정적]` — 병렬 켜기 (`+0x1a47`) 뒤, `TpAllocWork` 성공, 스레드 수 > 1, `LdrpDetourExist` = 0. run44 P1b 에 값 있음 `[관찰]` (run69). kernel32 올릴 때는 0 (run24) |
| `ctx+0xb0 == 0` | `LdrpMinimalMapModule +0x288`, `LdrpCheckForRetryLoading +0x24` | **정해짐 — 늘 0** `[정적]`. 지시서는 `LdrpAllocatePlaceHolder +0x55 RtlAllocateHeap` 의 0 채움 블록이고, ntdll 에서 그 칸에 쓰는 명령을 `objdump` 로 못 찾음 (훑기 한계: `lea` 뒤 쓰기·`rep stos` 류는 못 잡음) |
| ctx `+0x20` 비트 20·26 꺼짐 (첫 시도) | `LdrpCheckForRetryLoading +0x31` | **정해짐** — 새 지시서는 비트 15 만 (`LdrpAllocatePlaceHolder +0x7c`). 비트 26 은 핫패치 길 (`LdrpMapDllPatchImage +0x70`) 만 켬 |
| 맵 결과 `0x40000003` / `0x40000036` | `LdrpMinimalMapModule +0x192 ~ +0x1aa` | **실행 때** — 위 결론 2 |

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: ntdll (pdb GUID `C3093720177DA6851519DCFFE8DF53FE`, age 1) — 새로 analyze 안 함. 커널은 이미 있는 `MiMapViewOfImageSection` · `MiSelectImageBase` · `MiRelocateImage` `.asm`/`.c` 를 읽기만 (결과서).
- 버전 확인 (결과서): 기존 `_calls.tsv` 줄 넷 (`LdrpInitializeProcess +0x1bb6/+0x1bbb` · `LdrInitializeThunk +0x9/+0xe` · `_LdrpInitialize +0x167/+0x16c` · `LdrpInitializeProcess +0x1a42/+0x1a47`) 일치, `TppWorkPost` rva `0x39430` · 1346B 요청서와 같음.
- 값 대조: run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1b.txt`) 의 모듈 항목·VAD 줄을 flow-writer 가 다시 읽었다 (아래 "사진과 맞춰 본 것"). 새 동적 실행은 없다.

## 목적

- PE_emulator 가 진짜 `LdrpEnableParallelLoading` 덫을 켜면 `LdrpQueueWork +0x7d TpPostWork` → `TpPostWork +0x3c` 꼬리 `TppWorkPost` 길이 열린다. 그 길의 syscall 약속과 기다림 유무가 필요했다.
- 일감 개체가 있을 때만 열리는 재시도 길 (run24·run38 이 남긴 빈 곳) 의 syscall 표, 그리고 test.exe 가 그 길을 타는지 (dll1·dll2 맵 결과).

## 방법

- 정적: `_workspace/sast/67_pool-work-post.md`
  - ntdll `^TppWorkPost$` depth 1 → 14개 새로 (`TppWorkPost` + `NtAlertThreadByThreadId` · `NtReleaseWorkerFactoryWorker` · `NtTraceEvent` · SRW 락 셋 · `RtlFreeHeap` · `TppAdjustRunningThreadGoalWithLock` · `TppCleanupGroupMemberDestroy` · `TppFreeWait` · `TppAlpcpFree` · `__security_check_cookie` · CFG 검사 함수). 로그 `ms/ghidra/ntdll.export.sast67a.stdout`.
  - 결과서가 넓힌 것: `LdrpAllocatePlaceHolder` · `LdrpFreeLoadContext` depth 0 (지시서를 만드는 쪽 — `ctx+0xb0` 이 정해지는지 보려고, `ms/ghidra/ntdll.export.sast67b.stdout`), ntdll 전체 `objdump` 로 `[reg+0xb0]` 쓰기 훑기, 샘플 PE 헤더 셋, SSN 은 스텁 바이트.
  - 이미 있는 `.asm` 읽기만: `TpPostWork` · `LdrpQueueWork` · `LdrpCheckForRetryLoading` · `LdrpDrainWorkQueue` · `LdrpProcessWork` · `LdrpMapDllRetry` · `LdrpMinimalMapModule` · `LdrpWorkCallback` · `LdrpMapDllNtFileName` · `LdrpGetNtPathFromDosPath` · `TpAllocPoolInternal` · `LdrpDetectDetour`.
- 결과서: `_workspace/sast/67_pool-work-post_result.md` (**분석성공**). 결과서가 대조 중 자기 초안 오프셋 둘을 고쳤다 (`LdrpMapDllRetry +0xa3 JS` → `+0xb4`, `LdrpMapDllNtFileName +0xe8 JZ` → `+0x10e`). 이 문서는 고친 값을 쓴다.
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `TppWorkPost +0x156 TEST EDX,EDX` · `+0x158 JZ 0x18003975a` (= `+0x32a`) · `+0x15e ADD.LOCK [RSI],2` · `+0x4ef MOV RCX,[RDI+0x38]` · `+0x4f3 / +0x4f8 NtReleaseWorkerFactoryWorker` · `+0x4f8 JMP`; `LdrpMinimalMapModule +0x2b0 JZ` · `+0x2b6 MOV EBX,0xc000022d` · `+0x2bb JMP`.
- flow-writer 가 `.asm` 으로 한 번 더 본 줄: `TppWorkPost +0x14c CMPXCHG.LOCK [RSI+0xe8]` · `+0x1a8 JZ` · `+0x224` · `+0x2c5 BTS.LOCK [RBP],0` · `+0x3d7` · `+0x424 JZ` · `+0x432` · `+0x4a8 CMPXCHG.LOCK [RDI+0x8]` · `+0x4b7 JNZ` · `+0x4b9 XADD.LOCK` · `+0x4c2 JNZ` · `+0x4dd ADD EDX,0x10000`; `TpPostWork +0x37` · `+0x3c` (둘 다 꼬리 JMP); `LdrpQueueWork +0x7d / +0x82`; `LdrpCheckForRetryLoading +0x4e ~ +0x5c` (`LdrpRetryingModuleIndex` 읽기) · `+0x14e / +0x153 RtlRbInsertNodeEx` · `+0x153 BTS [RBP+0x20],0x14` · `+0x158` (`LdrpRetryQueue`); `LdrpMinimalMapModule +0x288` · `+0x296` · `+0x2a9 / +0x2ae` · `+0x2ae TEST AL,AL`; `LdrpDrainWorkQueue +0xeb` · `+0xf2 JZ` · `+0x13b MOVDQU [LdrpRetryingModuleIndex]` · `+0x14b JMP`; `LdrpProcessWork +0x51 BT EAX,0x14` · `+0x55 JC` · `+0x74 CMP EAX,0xc000022d` · `+0x79 JZ` · `+0x149 / +0x14e`; `LdrpMapDllRetry +0x81 / +0x86` · `+0x9a / +0x9f` · `+0xa3 JS 0x1800ebfb0` (= `+0xb4`) · `+0xad / +0xb2` · `+0xf2 / +0xf7`.
- 로그: 없음 (정적만).

## 결과

### 정적 — 호출 지점 (`_calls.tsv`·`.asm`)

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpQueueWork` | `+0x7d` | `+0x82` | `ntdll!TpPostWork(일감)` | `LdrpMapAndSnapWork ≠ 0` (`+0x59`) · `ShutdownInProgress == 0` (`+0x70`) · ctx 상태 음수 아님 | [정적] |
| `ntdll!TpPostWork` | `+0x37` | (꼬리 JMP) | `ntdll!TppRaiseInvalidParameter` | 일감 0 · `Flags +0xa8` 비트 16 / 17 · `VFuncs` 다름 · 종료 중 | [정적] |
| `ntdll!TpPostWork` | `+0x3c` | (꼬리 JMP) | `ntdll!TppWorkPost` | 위 넷 다 통과 (로더 일감은 통과) | [정적] |
| `ntdll!TppWorkPost` | `+0x4f3` | `+0x4f8` | `ntdll!NtReleaseWorkerFactoryWorker(풀 +0x38)` | `PendingReleaseCount` 0 → 1 로 올린 넣기 (= 첫 넣기) | [정적] |
| `ntdll!TppWorkPost` | `+0x224` | `+0x229` | `ntdll!NtTraceEvent` | KUSD `+0x386` ≠ 0 (안 탐) | [정적] |
| `ntdll!TppWorkPost` | `+0x127` | `+0x12c` | `ntdll!NtAlertThreadByThreadId` | 배리어 느린 길 (안 탐) | [정적] |
| `ntdll!TppWorkPost` | `+0x432` | `+0x437` | `ntdll!TppAdjustRunningThreadGoalWithLock` | `LastProcCount` ≠ CPU 수 (안 탐) | [정적] |
| `ntdll!TppWorkPost` | `+0x3d7` | `+0x3dc` | `ntdll!RtlpAcquireSRWLockExclusiveContended` | 큐 락이 이미 잠김 (안 탐) | [정적] |
| `ntdll!TppWorkPost` | `+0x3ca` | — | CFG 간접 `VFuncs->Free` (일감 종류는 `TppWorkpFree`, rva `0x7fb80`) | `Refcount` 가 0 이 될 때 (안 탐) | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x46` | `+0x4b` | `ntdll!LdrpCheckForRetryLoading(ctx, DL = 0)` | 늘 (모든 DLL 이름 열기 앞) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x2a9` | `+0x2ae` | `ntdll!LdrpCheckForRetryLoading(ctx, DL = 1)` | 맵 결과 `0x40000003`/`0x40000036` · `ctx+0xb0 == 0` · `LdrpMapAndSnapWork ≠ 0` | [정적] |
| `ntdll!LdrpCheckForRetryLoading` | `+0x14e` | `+0x153` | `ntdll!RtlRbInsertNodeEx(&LdrpRetryingModuleIndex, …, 항목 +0xe0)` | 트리에 같은 이름 없음 · `DL ≠ 0` | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x2d4` | `+0x2d9` | `ntdll!NtUnmapViewOfSection(-1, …)` | 결과 음수 (`0xc000022d` 포함) · `DllBase ≠ 0` | [정적] |
| `ntdll!LdrpProcessWork` | `+0x149` | `+0x14e` | `ntdll!LdrpMapDllRetry(ctx)` | ctx `+0x20` 비트 20 (`+0x51 BT` · `+0x55 JC`) | [정적] |
| `ntdll!LdrpMapDllRetry` | `+0x81` | `+0x86` | `ntdll!LdrpFindExistingModule` | 늘 | [정적] |
| `ntdll!LdrpMapDllRetry` | `+0xf2` | `+0xf7` | `ntdll!LdrpLoadContextReplaceModule(ctx)` | 같은 모듈을 찾았을 때 (`+0x90 JNZ`) | [정적] |
| `ntdll!LdrpMapDllRetry` | `+0x9a` | `+0x9f` | `ntdll!LdrpGetNtPathFromDosPath` (안에서 `+0xee / +0xf3 NtQueryAttributesFile`) | 못 찾았을 때 | [정적] |
| `ntdll!LdrpMapDllRetry` | `+0xad` | `+0xb2` | `ntdll!LdrpMapDllNtFileName(ctx, NT 경로)` | 위가 음수 아님 (`+0xa3 JS` → `+0xb4` 안 탐) | [정적] |
| `ntdll!LdrpWorkCallback` | `+0xb3` | `+0xb8` | `ntdll!LdrpProcessWork(일감 − 0x40, 0)` | 큐에 일감 있음 | [정적] (sast/19 와 같음) |
| `ntdll!LdrpWorkCallback` | `+0xcb` | `+0xd0` | `ntdll!LdrpThreadTokenSetImpersonationToken(메인 토큰)` | 첫 일감 · `LdrpMainThreadToken ≠ 0` | [정적] |

### 호출 관계

```
LdrpQueueWork(ctx)                                   LdrpWorkQueue 꼬리에 ctx+0x40 (run24)
 └─ +0x7d/+0x82  TpPostWork(LdrpMapAndSnapWork)        [일감 개체 ≠ 0 · 종료 중 아님]
      ⇒ +0x3c 꼬리 JMP  TppWorkPost(일감)
           ├─ 배리어 세기 +1 (+0xed)
           ├─ WorkState: Insertable 끔 · PendingCallbackCount +1 (+0x14c)
           │    └─ 옛 Insertable == 0 → +0x158 JZ → 끝 (둘째 넣기부터, syscall 0)
           ├─ Refcount +2 (+0x15e), WorkOnBehalfTicket ← TEB+0x2b8
           ├─ [KUSD +0x386 ≠ 0]  +0x224 NtTraceEvent                        (안 탐)
           ├─ 큐 = 풀 TaskQueue[1] + 노드*0x18, 락 BTS (+0x2c5)
           │    └─ [잠김] +0x3d7 RtlpAcquireSRWLockExclusiveContended          (안 탐)
           ├─ 이웃 검사 (틀리면 +0x2e4 INT 0x29) → 꼬리 붙이기 → +0x403 락 풂
           ├─ [LastProcCount ≠ CPU 수] +0x432 TppAdjustRunningThreadGoalWithLock (안 탐)
           ├─ 풀 QueueState: QueueLength +1, PendingReleaseCount 0 → 1 (+0x4a8)
           │    └─ 깨우기 표시 → +0x4ef RCX = [풀+0x38] → +0x4f3 NtReleaseWorkerFactoryWorker  (첫 넣기 1번)
           └─ Refcount −1 (+0x4b9) → 1 이 아니면 +0x4c2 JNZ → 끝

LdrpMinimalMapModule(ctx, 섹션)                       (run38)
 ├─ +0x174/+0x179  NtMapViewOfSection
 └─ [0x40000003·0x40000036 · ctx+0xb0 == 0 · LdrpMapAndSnapWork ≠ 0]
      +0x2a9/+0x2ae  LdrpCheckForRetryLoading(ctx, 1)
           ├─ 트리 LdrpRetryingModuleIndex 에 같은 이름 없으면 +0x14e RtlRbInsertNodeEx
           └─ ctx+0x20 비트 20 켬 (+0x153), ctx+0x40 → LdrpRetryQueue 꼬리, 참
      참 → +0x2b6 EBX = 0xc000022d → +0x2d4 NtUnmapViewOfSection
 (위로) LdrpMapDllNtFileName: +0x2a0 · +0x2a9 NtClose ×2 → LdrpProcessWork +0x74 0xc000022d 면 오류 기록 없이 끝

LdrpDrainWorkQueue(1)
 └─ 큐 비고 WorkInProgress == 1 → +0xeb LdrpRetryQueue 가 비었나
      └─ 안 비었음: 줄 통째로 LdrpWorkQueue 로 (+0x100 ~ +0x126), 재시도 줄 비움 (+0x12d·+0x134),
                    +0x13b LdrpRetryingModuleIndex 16B = 0, +0x14b JMP 처음으로
           → +0x187 LdrpProcessWork(ctx)
                └─ 비트 20 → +0x149/+0x14e LdrpMapDllRetry
                     ├─ +0x81 LdrpFindExistingModule  — 찾음 → +0xf2 LdrpLoadContextReplaceModule
                     ├─ +0x9a LdrpGetNtPathFromDosPath → NtQueryAttributesFile
                     └─ +0xad LdrpMapDllNtFileName (같은 길, 재시도 검사는 +0x31 에서 거짓)
```

### `TppWorkPost` 가 첫 넣기에서 쓰는 칸

| 개체 | 칸 | 변화 | 줄 |
|---|---|---|---|
| 일감 `_TP_WORK` | `+0x0 Refcount` | +2 하고 −1 → 순증 +1 (1 → 2) | `+0x15e`, `+0x4b9` |
| | `+0x38 CallbackBarrier.Ptr` | 세기 +1 (넣기마다) | `+0xed` |
| | `+0x80 WorkOnBehalfTicket` | ← `TEB+0x2b8` | `+0x17d` |
| | `+0xd8 Task.ListEntry` | 큐 꼬리에 연결 | `+0x3f2 ~ +0x3ff` |
| | `+0xe8 WorkState` | 1 → 2 (`Insertable` 1 → 0, `PendingCallbackCount` +1. 둘째부터는 셈만 +1) | `+0x14c` |
| 풀 `_TP_POOL` | `+0x8 QueueState` | `QueueLength` +1 · `PendingReleaseCount` 0 → 1. 이 VM: `0x…0002` → `0x0000000100010002` (아래 16비트 `RunningThreadGoal` = CPU 수 2) | `+0x4a8` |
| | `+0x1ac NodeStatus` | `0xfffffffe` → 일감의 노드 번호 (이 VM 0 `[추측: 노드 1개]`) | `+0x36f` |
| 큐 `_TPP_QUEUE` (`TaskQueue[1][노드 0]`) | `+0x0`·`+0x8` | 머리 ↔ 일감 `+0xd8` | `+0x3f2 ~ +0x3ff` |
| | `+0x10 Lock` | 잡았다 풂 | `+0x2c5`, `+0x403` |
| 스레드 TEB | `+0x1850 SchedulerSharedDataSlot` 이 가리키는 8칸 표 | 표가 있을 때만 빈 칸에 락 주소 (`+0x2a7 JZ` 로 표 0 이면 건너뜀) | `+0x29d ~ +0x2c2` |

- 칸 이름은 PDB (`_TP_WORK` · `_TPP_WORK_STATE` · `_TP_POOL` · `_TPP_POOL_QUEUE_STATE` · `_TPP_QUEUE` · `_TP_TASK` · `_TPP_BARRIER`, 결과서 `pdb_struct.py`).
- 풀·일감 안쪽은 run44 사진에 없다 (힙 안, run69). 그래서 이 표는 전부 `[정적]` 이다.

### `LdrpCheckForRetryLoading` (rva `0xdf18`, 494B) — 이름과 달리 "재시도 줄에 넣을지 정하고 넣는다"

- 인자: `RCX` = 지시서 (ctx), `DL` = "트리에 없으면 새로 넣기" 깃발. 돌려주는 값은 `AL` 참/거짓 — **상태 코드가 아니다** `[정적]`.
- 순서:
  1. `+0x24 CMP [ctx+0xb0],0` ≠ 0 → 거짓 (`+0x2b JNZ` → `+0x195`).
  2. `+0x31 TEST [ctx+0x20],0x4100000` — 비트 20 (이미 재시도 줄) 또는 비트 26 켜짐 → 거짓 (`+0x38 JNZ`).
  3. `+0x49 / +0x4e RtlEnterCriticalSection(LdrpWorkQueueLock)`.
  4. 트리 **`LdrpRetryingModuleIndex`** (rva `0x1d25b0`, 루트 8B + `+0x8` 에 `Encoded` 비트) 에서 항목 `BaseNameHashValue` (`+0x108`) 로 찾고, 같으면 `FullDllName` 을 대소문자 무시로 비교 (`+0xa9 / +0xae RtlCompareUnicodeStrings`). 트리 노드 = 항목 `+0xe0` (PDB `MappingInfoIndexNode`).
  5. 같은 이름이 있으면: `+0x153 BTS [ctx+0x20],0x14` · ctx `+0x40` 을 **`LdrpRetryQueue`** (rva `0x1d2630`) 꼬리에 (`+0x158 ~ +0x182`, 이웃 검사 실패면 `+0x176 INT 0x29`) · 참.
  6. 없고 `DL == 0` (`+0xdc JZ` → `+0x189`) 이면 거짓. `DL ≠ 0` 이면 `+0xfb LdrpCompareModuleName` 으로 자리를 찾아 `+0x14e / +0x153 RtlRbInsertNodeEx` 로 트리에 넣고, 5번 길로 합쳐 참.
  7. `+0x190 / +0x195 RtlLeaveCriticalSection`.
- syscall: **없음**.
- `0xc000022d` 는 **부른 쪽**이 만든다: `LdrpMinimalMapModule +0x2b6` (DL = 1 쪽), `LdrpMapDllNtFileName +0x22e` (DL = 0 쪽 — 파일을 열기도 전에 돌아감, `+0x4d JNZ` → `+0x22e`). 이름은 `STATUS_RETRY` 기억 `[추측]`.
- 트리가 비어 있을 때 `LdrpMapDllNtFileName +0x46` 쪽 호출은 임계 구역만 들어갔다 나오고 거짓 (syscall 0). 이 호출은 **모든 DLL 이름 열기 앞**에 있다.

### `LdrpDrainWorkQueue +0xeb ~ +0x14b` — 재시도 줄을 일감 큐로 되돌린다 (`Drain(1)` 만)

- 문: 일감 큐 빔 (`+0x8a`) · `LdrpWorkInProgress == 1` (`+0x90 / +0x96`) → `+0xb4 JNZ` → `+0xeb`.
- `+0xeb CMP [LdrpRetryQueue],R12` 같으면 (빈 줄) `+0xf2 JZ` → `+0xb6` 끝 (책임자 비트 `TEB+0x17ee |= 0x1000`, `+0xd3`).
- 안 비었으면: 잠금 (`+0xf4 / +0xfb`) → 줄 전체를 `LdrpWorkQueue` 머리 뒤로 (`+0x100 ~ +0x126`) → `LdrpRetryQueue` 빈 줄로 (`+0x12d`, `+0x134`) → **`+0x13b` `LdrpRetryingModuleIndex` 16B 를 0 으로** → 잠금 풂 (`+0x143 / +0x148`) → `+0x14b JMP` 처음 (`+0x42`).
- syscall: **없음**. 다음 바퀴에 옮겨진 ctx 가 `+0x182 / +0x187 LdrpProcessWork` 로 간다.
- `LdrpProcessWork` 는 `0xc000022d` 를 받으면 `+0x74 CMP` · `+0x79 JZ` → `+0x15d` — 오류 기록 없이 끝나고 지시서 상태 칸 (`[ctx+0x28]`) 에도 안 쓴다.

### dll1·dll2 맵 결과 — 정적으로 안 닫힘

- 샘플 PE 헤더 (`sample/bin/` = `ms/bin/` 사본, md5 같음 — 결과서) `[정적+샘플]`:

| 파일 | `Characteristics` | `ImageBase` | `DllCharacteristics` | 재배치 디렉터리 |
|---|---|---|---|---|
| `test.exe` | `0x22` | `0x140000000` | `0x8160` (`DYNAMIC_BASE` 등) | rva `0x8b000` · `0x810` |
| `dll1.dll` | `0x2022` (DLL) | **`0x180000000`** | `0x160` (`DYNAMIC_BASE` · `HIGH_ENTROPY_VA` · `NX_COMPAT`) | rva `0x8b000` · `0x800` |
| `dll2.dll` | `0x2022` | **`0x180000000`** | `0x160` | rva `0x8b000` · `0x804` |

- 커널 쪽 (`MiMapViewOfImageSection`, 결과서 `.asm` 읽기) `[정적]`:
  - `0x40000003` 은 **`+0xc0a MOV [RSP+0x60],0x40000003`** 한 자리. 들어오는 길은 `+0x42e JNZ` 하나 — **실제로 붙은 시작 주소 ≠ 섹션 `_SEGMENT.BasedAddress`** (`+0x412 ~ +0x42a`) 일 때.
  - 보통 경우 (`&항목.DllBase` 가 처음 0): 기준 주소부터 `+0x6c8 / +0x6cd MiIsVaRangeAvailable` → 비어 있으면 `+0x6d2 JNZ` → `+0x412` → 상태 `0`. 비어 있지 않으면 다른 자리 (`+0x6d8 JMP` → `+0x932`) → `0x40000003`.
  - `0x40000003` 이 되려면 할당에 `0x800000` 이 있어야 한다 (`+0xc12 TEST BL,2`). `LdrpMinimalMapModule` 기본 길이 `0x800000` 을 준다 (run38).
  - `0x40000036` 은 `+0xc1b TEST BL,8` · `+0xc1e JNZ` → `+0xc82` (이름만, Q126 (6)).
- 섹션 기준 주소는 섹션을 만들 때 정해진다 (`MiCreateNewSection → MiRelocateImage +0x2e6 / +0x2eb → MiSelectImageBase`, 결과서) `[정적]`:
  - `MiSelectImageBase +0xd6 TEST DL,R10B` · `+0xd9 JNZ` → `+0x2b6`: 넷째 인자 비트 0 이 켜지면 **헤더 `ImageBase` 그대로**. 그 비트는 `MiRelocateImage` 여섯째 인자 `& 1` 에서 온다. ~~이걸 주는 `MiCreateNewSection` 줄은 안 읽었다 `[공백]`.~~ → (run73 정정) `MiCreateNewSection +0x361` · `+0x38b SHR ESI,0x1b` · `+0x391 AND ESI,2` → 비트 0 은 늘 0 `[정적]`.
  - 꺼져 있고 DLL 이면 (`+0xfc ~ +0x104`) 전역 비트맵 (`0x7fffffff0000` 에서 아래로 64KB 칸) 에서 **이미지 파일마다 다른 칸**. EXE 는 `ExGenRandom` 쪽.
  - `EPROCESS.Flags3` 비트 13 (`AllImagesAtBasePristineBase`, PDB 이름) 이 `+0xba` 에서 흐름을 바꾼다. 뜻은 안 읽음 `[공백]`.
- 결론: **정적으로 안 닫힘.** 비트맵 갈래라면 헤더 값이 같다는 건 상관없고 자리가 비어 있으면 `0` (기대 `[추측]`). 헤더 그대로 갈래라면 둘째 DLL 은 반드시 `0x40000003`.
  - (run73 정정) 비트맵 갈래로 정해졌다 `[정적]`. 기대 `0` 의 근거가 `[정적]`, 실제 범위 점유는 동적 (Q104). 헤더 그대로 갈래는 이 두 DLL 에 해당 없음.

### `LdrpWorkCallback` (rva `0xe9ef0`, 218B) — sast/19 와 같음

- 인자 셋 (`PTP_CALLBACK_INSTANCE`, 문맥, `PTP_WORK`) 을 **하나도 안 쓴다.** `LdrpWorkQueue` 를 **빌 때까지** 비운다.
- 한 바퀴: `+0x23` `LdrpDetourExist` 켜짐 → `+0x2a JNZ` → `+0x7a` 끝 / 잠금 → 머리 하나 빼기 (이웃 검사 실패 `+0xbd INT 0x29`) → 있으면 `+0x5e LdrpWorkInProgress += 1` · `+0x64 / +0x69 LdrpUpdateStatistics` → 잠금 풂 → 첫 일감 때 `LdrpMainThreadToken ≠ 0` 이면 `+0xcb / +0xd0 LdrpThreadTokenSetImpersonationToken` (안에서 `NtSetInformationThread`, `0xd`) → `+0xb3 / +0xb8 LdrpProcessWork(일감 − 0x40, 0)` → `+0xb8 JMP +0x23`. 끝에서 가장했으면 `+0x81 / +0x86` 로 되돌림.
- 이 함수 자체 syscall: `NtSetInformationThread` (토큰, 조건부) 뿐. 기다림 없음.
- 실제 풀 일꾼은 이 함수 대신 `TppWorkpExecuteCallback` 안 복사본을 돌 수 있다 (run24, `[추측: 동적]`).

## 확인된 것

- `[정적]` `TpPostWork` 는 인자 검사 뒤 `+0x3c` 꼬리 JMP 로 `TppWorkPost` 에 간다. 콜백을 그 자리에서 돌리지 않는다 (run24 `[추측]` "안 돌림" → 이번에 `[정적]`).
- `[정적]` `TppWorkPost` 는 기다림이 없고, test.exe 초기화 조건에서 syscall 은 첫 넣기 `NtReleaseWorkerFactoryWorker` 하나다. 핸들·반환값을 안 본다.
- `[정적]` `LdrpCheckForRetryLoading` 은 참/거짓만 준다. `0xc000022d` 는 부른 쪽 둘이 만든다.
- `[정적]` `LdrpRetryingModuleIndex` = 재시도 중인 모듈 이름 트리 (노드 = 항목 `+0xe0`). 넣는 곳 `LdrpCheckForRetryLoading +0x14e`, 지우는 곳 `LdrpDrainWorkQueue +0x13b`.
- `[정적]` `LdrpRetryQueue` 에 넣는 곳 = `LdrpCheckForRetryLoading +0x158 ~ +0x182` (run26 `[추측: 이름]` → 이번에 길이 정해짐).
- `[정적]` `ctx+0xb0` 은 ntdll 에서 쓰는 줄이 없다 (항상 0, 훑기 한계 있음). run26 이 남긴 "뜻 모름" 은 그대로지만, test.exe 길에선 0 으로 정해진다.
- `[정적]` 재시도 길의 문 조건 넷 중 셋은 test.exe 에서 이미 참, 남은 하나 (맵 결과) 만 실행 때 정해진다.

## 사진과 맞춰 본 것 (run44, 새 실행 없음)

- `[관찰]` P1b 모듈 항목 (`ms/logs/04_baggage-read_P1b.txt`):
  - dll1: `DllBase` `0x7ffae12e0000` (3802줄) · `OriginalBase` `0x7ffae12e0000` (3853줄) · VAD `7ffae12e0 ~ 7ffae136b` (2246줄).
  - dll2: `DllBase` `0x7ffadb950000` (3907줄) · `OriginalBase` `0x7ffadb950000` (3958줄) · VAD `7ffadb950 ~ 7ffadb9db` (2245줄).
  - P2 도 같은 값 (`_P2.txt` 3865 · 3970줄).
- 읽는 법:
  - `OriginalBase` 는 `LdrpProcessMappedModule +0x97` 이 **메모리의 NT 헤더 `ImageBase`** 로 덮는 칸이다 (run35, 짐 지도 3.5). 그러니 그 순간 메모리 헤더 값은 파일 값 `0x180000000` 이 아니라 각자의 `DllBase` 였다.
  - 다만 그 헤더를 커널이 섹션을 만들 때 고쳤는지, ntdll 재배치 (`LdrpCompleteMapModule +0x31b`) 가 고쳤는지는 이 칸으로 못 가린다 `[추측]` — 재배치 쪽이 헤더 `ImageBase` 를 고치는지 안 읽었다.
  - 두 주소 다 비트맵 갈래가 쓰는 꼭대기 구역 (`0x7fffffff0000` 아래) 에 있고, 서로 다르다. 그래서 "DLL 마다 비트맵 칸" 쪽과 어긋나지 않는다 `[추측: 주소 모양으로만]`.
- 재시도 전역 (`LdrpRetryingModuleIndex` · `LdrpRetryQueue`) 은 P1b·P2 사진으로도 못 가린다. 재시도가 일어났어도 `Drain(1) +0x12d ~ +0x13b` 가 빈 꼴로 되돌린 뒤라서다 `[정적: 길]`.

## 아직 확인 필요

동적 — Q104 VM 단계에 얹는다 (결과서 "dast 요청 필요" 1 ~ 5, 멈춤점은 `process test.exe` 조건):

1. **맵 결과 값**: `ntdll!LdrpMinimalMapModule +0x179` (rva `0xa399`) 에서 `r eax` · `du poi(@rdi+0x50)` · `dq @rdi+0x30 L1`. kernel32·kernelbase·dll1·dll2 모두. 기대 dll1·dll2 `0` `[추측]`. `0x40000003`/`0x40000036` 이면 2 를 켠다.
2. **재시도 길 열렸나**: `ntdll!LdrpCheckForRetryLoading` 입구 (rva `0xdf18`) `r dl`·돌아온 `al` · `ntdll!LdrpMapDllRetry` 입구 (rva `0xebefc`) 가 서나 · `ntdll!LdrpDrainWorkQueue +0xeb` (rva `0xad4eb`) 에서 `dq ntdll!LdrpRetryQueue L2`.
3. **첫 넣기 syscall 1번**: `ntdll!TppWorkPost +0x4f3` (rva `0x39923`) 에서 `rcx` = `poi(poi(ntdll!LdrpThreadPool)+0x38)` 인지, 초기화 동안 **딱 1번** 서는지. `ntdll!TppWorkPost` 입구 (rva `0x39430`) 는 넣기 횟수.
   - 첫 넣기 뒤: `dq poi(ntdll!LdrpThreadPool)+8 L1` (기대 `0x0000000100010002`) · `dd poi(ntdll!LdrpThreadPool)+0x1ac L1` (`0`) · `dd poi(ntdll!LdrpMapAndSnapWork)+0xe8 L1` (`2`) · `dq poi(ntdll!LdrpMapAndSnapWork)+0x38 L1` (`1`) · `dd poi(ntdll!LdrpMapAndSnapWork) L1` (`2`).
4. **조건 입력 값**: `db 0x7ffe0386 L1` (`0`) · `dq @$teb+0x1850 L1` · `dq @$teb+0x1778 L1` (메인 스레드 0) · `dd 0x7ffe03c0 L1`.
5. (선택) `ntdll!NtSetEvent` 입구 — `LdrpWorkCompleteEvent` 로 가는 횟수.

정적 — 다음 바퀴 후보 (대기열 번호는 오케스트레이터가):

- ~~커널 `MiCreateNewSection` 이 `MiRelocateImage` 여섯째 인자 (`& 1`) 에 주는 값 — 닫으면 위 동적 1 의 기대값이 정적으로 정해진다.~~ → (해결됨) run73: 늘 0 (`+0x391 AND ESI,2`), 동적 1 기대 `0` 의 근거가 `[정적]` 으로.
- `EPROCESS.Flags3` 비트 13 `AllImagesAtBasePristineBase` 의 뜻, `_SEGMENT.BasedAddress` 를 채우는 줄.
- `TpWaitForWork` 본문 (`LdrpDetectDetour +0xc4` 갈래).
- 커널 `NtReleaseWorkerFactoryWorker` 본문 — 첫 넣기가 일꾼을 새로 만드는 계기인지 `[추측]` (Q134 (b) 와 같이).
- `ctx+0xb0` 을 `lea` 뒤 쓰기·`rep stos` 로 쓰는 곳이 있는지 (이번 훑기 한계).
- (덤프 오프라인, VM 불필요) run44 덤프 (`ms/dump/03_baggage-snap/P1b/`) 에서 dll1·dll2 섹션의 `_SEGMENT.BasedAddress` 를 읽어 `DllBase` 와 비교 — 같으면 맵 결과 `0` (재시도 안 탐) 이 거의 닫힌다 `[추측: 덤프에 그 페이지가 있다면]`.

## 디버거 메모 / 도구 메모

- 없음 (정적만). 결과서 스크립트 문제 없음.

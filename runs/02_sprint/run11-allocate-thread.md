# 2026-10-21 run11 — `PspAllocateThread` 안: 스레드 객체 → ID → 유저 스택 → TEB → `KeInitThread`, `PspInsertProcess` 의 목록 올리기 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntoskrnl (`ntoskrnl.exe`, 26100.9457), pdb `ntkrnlmp.pdb` GUID `C29EBFB06B78B3C020DCA66D99713F9E1`
  - run3(관찰)·run9·run10(정적)와 같은 커널이다.
- 뽑은 함수 2개 (depth 0): `PspAllocateThread`, `PspInsertProcess`
  - 다른 함수 본문은 안 뽑았다. 이름·caller·깊이는 지도(`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_{nodes,edges}.tsv`)로만 봤다.

## 목적

- 대기열 Q3. 그림 1 의 `PspAllocateThread`·`PspInsertProcess` 는 지금까지 `[관찰]` 호출 관계 + `[추측]` 역할뿐이었다. 안에서 무엇을 부르는지 본다.
- `PspAllocateThread` 안에서 아래가 일어나는지, 어떤 순서인지:
  - (가) 스레드 객체 만들기 (나) 스레드 ID (다) 유저 스택 (라) TEB (`MmCreateTeb`, points.md `[확인 필요]` 이름) (마) `KeInitThread`
- 첫 스레드가 `PspUserThreadStartup` 에서 출발하게 되는 곳(01_sprint run4·5 관찰)이 코드에 있나. 유저 쪽 시작 주소(`test!mainCRTStartup`)는 어떤 길로 들어오나.
- `PspInsertProcess` 가 새 프로세스를 목록에 올리는 방법, 핸들을 만드는지.

## 방법

- 정적: `_workspace/sast/06_allocate-thread.md` — ntoskrnl, `^(PspAllocateThread|PspInsertProcess)$` depth 0
- 결과서: `_workspace/sast/06_allocate-thread_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨):
  - `ms/disassemble/ntoskrnl/_calls.tsv`, `PspAllocateThread.asm`, `PspInsertProcess.asm`
  - `ms/decomp/ntoskrnl/PspAllocateThread.c`, `PspInsertProcess.c` (머리말 `callers:` 줄)
  - 지도 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_{nodes,edges}.tsv`
- 오프셋 읽는 법 (run9·10 과 같음)
  - `call` = call 명령이 있는 자리
  - `ret` = call 다음 명령 자리 = **WinDbg 콜스택에 찍히는 오프셋**. flow.md 그림 라벨은 이걸 쓴다.
- 주의: `PspAllocateThread` 안에 **장부(`_calls.tsv`)·`.asm`·지도에 안 잡힌 코드 덩어리**(`+0x849` ~ `+0xbba`)가 있다.
  - 장부 밖 호출 10곳 = 이 덩어리(`+0x849` ~ `+0xbba`) 안 9곳 + 먼 쪽 덩어리 1곳(`+0x2ca86a` `ExRaiseDatatypeMisalignment`). `.c`(디컴파일)와 결과서 표로만 확인했다. 아래에서는 "(장부에 없음, 디컴파일로 확인)" 이라고 붙인다.

## 결과

### 정적 — 큰 줄기

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0xe57` | `+0xe5c` | `nt!PspAllocateThread` | [26100.9457 확인] + [정적] (run9 에서 이미 맞춤) |
| `nt!PspAllocateThread` | `+0x2e2` | `+0x2e7` | `nt!ObCreateObjectEx` (가) 스레드 객체 | [정적] |
| `nt!PspAllocateThread` | `+0x52d` | `+0x532` | `nt!PsAssignThreadId` (나) 스레드 ID | [정적] |
| `nt!PspAllocateThread` | `+0x5ea` | `+0x5ef` | `nt!PspSetupUserStack` (다) 유저 스택 | [정적] |
| `nt!PspAllocateThread` | `+0x62c` | `+0x631` | `nt!MmCreateTeb` (라) TEB | [정적] |
| `nt!PspAllocateThread` | `+0x6f4` | `+0x6f9` | `nt!KeInitThread` (마) 커널 쪽 초기화 | [정적] |
| `nt!NtCreateUserProcess` | `+0xfbd` | `+0xfc2` | `nt!PspInsertProcess` | [26100.9457 확인] + [정적] (run9 에서 이미 맞춤) |
| `nt!PspInsertProcess` | `+0xa1` | `+0xa6` | `nt!PspLockProcessListExclusive` (목록 잠금) | [정적] |
| `nt!PspInsertProcess` | `+0x110` | `+0x115` | `nt!PspUnlockProcessListExclusive` (목록 풀기) | [정적] |
| `nt!PspInsertProcess` | `+0x1de` | `+0x1e3` | `nt!ObInsertObjectEx` (객체 등록) | [정적] |

### 정적 — `PspAllocateThread` 직접 호출, 하는 일 단위로

- 장부 `_calls.tsv` 에 직접 호출 **34줄** (서로 다른 이름 30, 조각 안 9줄 포함) + **장부 밖 10곳** (디컴파일로 확인).
  - 장부에 `tail` 표시 3줄(`+0x7ea`, `+0x2caccb`, `+0x2cad20`)이 더 있는데, 조각으로 갔다가 본체로 돌아오는 점프라서 진짜 꼬리호출이 아니다 (콜스택에 영향 없음).
  - 함수 포인터를 거치는 간접 호출, 별칭(같은 주소 다른 이름) 줄은 없다.
- 이 함수를 부르는 곳 (머리말 `callers:`): `NtCreateUserProcess`, `PspCreateThread`, `PspCreatePicoThread`.
- 주소 순서 = 실행 순서로 보인다 (결과서가 디컴파일 흐름과 맞춰 봄). 숫자는 ret (괄호 안 call).

| # | 하는 일 | 대표 호출 |
|---|---|---|
| 1 | 크기 정하기 (`_ETHREAD` 뒤 덤 공간) | `PoEnergyEstimationEnabled` `+0x228`, `KeQueryMaximumGroupCount` `+0x241` |
| 2 | **(가) 스레드 객체 만들기** | **`ObCreateObjectEx` `+0x2e7`** (`+0x2e2`), `memset` `+0x30c` |
| 3 | 필드 채우기 (리스트 머리, 만든 시각) | `KeInitializeSemaphore` `+0x432`, `KeQuerySystemTimePrecise` `+0x4c4` |
| 4 | 스레드 락 잡기 | `KeAbPreAcquire` `+0x502`, `ExfAcquirePushLockExclusiveEx` `+0x51c` |
| 5 | **(나) 스레드 ID** | **`PsAssignThreadId` `+0x532`** (`+0x52d`) |
| 6 | **(다) 유저 스택** | **`PspSetupUserStack` `+0x5ef`** (`+0x5ea`) |
| 7 | **(라) TEB** | **`MmCreateTeb` `+0x631`** (`+0x62c`) |
| 8 | **(마) 커널 쪽 스레드 초기화** | **`KeInitThread` `+0x6f9`** (`+0x6f4`) |
| 9 | 마무리 | `PsInitializeThreadRngState` `+0x710`, `ObInitializeFastReference` `+0x71f`, `__security_check_cookie` `+0x73c` |
| — | 실패 정리 (조각 안) | `MmDeleteTeb`, `PspDeleteUserStack`, `ExFreePoolWithTag`, `ObfDereferenceObject` 등 |

장부 밖 10곳 (장부에 없음, 디컴파일로 확인. 오프셋은 결과서가 Ghidra 로 직접 읽은 값):

| ret (call) | callee | 언제 |
|---|---|---|
| `+0x87f` (`+0x87a`) | `ObfDereferenceObjectWithTag` | 실패 정리 |
| `+0x8c1` (`+0x8bc`) | `KeGetProcessorNodeNumber` | 이상적 프로세서의 노드 |
| `+0x9ae` (`+0x9a9`) | `PspSetupUserStack` | WOW64(32비트) 길의 두 번째 호출 자리 |
| `+0x9eb` (`+0x9e6`) | `PspWow64SetupUserStack` | WOW64 길 |
| `+0xa9d` (`+0xa98`) | `KeSelectInitialIdealProcessorForThread` | 이상적 프로세서 고르기 |
| `+0xaac` (`+0xaa7`) | `KeGetProcessorNodeNumberByIndex` | 노드 번호 |
| `+0xb39` (`+0xb34`) | `ExfTryToWakePushLock` | 오류 때 락 풀기 |
| `+0xb65` (`+0xb60`) | `PspSetupUserShadowStack` | CET 섀도 스택 (유저 스택 다음) |
| `+0xb7d` (`+0xb78`) | `PspWow64InitThread` | WOW64 길 (TEB 다음) |
| `+0x2ca86a` (`+0x2ca865`) | `ExRaiseDatatypeMisalignment` | 유저 포인터 정렬이 틀릴 때 (먼 덩어리) |

### 정적 — `PspInsertProcess` 직접 호출, 하는 일 단위로

- 장부에 직접 호출 **21줄** (서로 다른 이름 20). 간접·꼬리·별칭·조각 없음.
- 이 함수를 부르는 곳 (머리말 `callers:`): `NtCreateUserProcess`, `PspCreateProcess`, `PsCreateMinimalProcess`.
- 아래 순서는 결과서가 정한 **실행 순서**다 (주소 순서와 다름). 숫자는 ret.

| # | 하는 일 | 호출 |
|---|---|---|
| 1 | 감사 켜졌나 보기 | `SepAuditingForSubCategory` `+0x58` |
| 2 | 사용자 정책 감사 (전역값이 켜졌을 때) | `SeCaptureSubjectContext` `+0x28e`, `SepAdtIncorporatePerUserPolicy` `+0x2b4`, `SeReleaseSubjectContext` `+0x2bd` |
| 3 | 프로세스 생성 감사 | `SeAuditProcessCreation` `+0x78` |
| 4 | 잡(job) 상속 (부모가 잡에 있을 때) | `PspImplicitAssignProcessToJob` `+0xd9` |
| 5 | 시스템 콜 제공자 상속 | `PspInheritSyscallProvider` `+0x94` |
| 6 | **프로세스 목록에 올리기** | **`PspLockProcessListExclusive` `+0xa6`** → (호출 없이 직접 잇기) → **`PspUnlockProcessListExclusive` `+0x115`** |
| 7 | 디버그 포트 | `ObReferenceObjectByHandle` `+0x2f8` (핸들 있을 때), `DbgkCopyProcessDebugPort` `+0x143`, `ObfDereferenceObject` `+0x313` |
| 8 | 접근 상태 + **객체 등록** | `SeCreateAccessStateEx` `+0x1aa`, `ObfReferenceObjectWithTag` `+0x1c1`, **`ObInsertObjectEx` `+0x1e3`**, `ObfDereferenceObjectWithTag` `+0x1f6` |
| 9 | 끝맺음 | `PspValidateJobAffinityState` `+0x263`, `ObCheckRefTraceProcess` `+0x215`, `PspUnlockProcessExclusive` `+0x33a` / `+0x34a` |
| 실패 | | `SeDeleteAccessState` `+0x24d` |

### 호출 관계

```
NtCreateUserProcess
 ├─ (7) +0xe5c  PspAllocateThread                    [관찰] run3 + [정적]   (번호 = 코드 순서)
 │               ├─ (1) +0x2e7  ObCreateObjectEx       [정적] (가) 스레드 객체
 │               ├─ (2) +0x532  PsAssignThreadId       [정적] (나) 스레드 ID
 │               ├─ (3) +0x5ef  PspSetupUserStack      [정적] (다) 유저 스택   ← 유저 스레드 + 보통 길일 때만
 │               ├─ (4) +0x631  MmCreateTeb            [정적] (라) TEB       ← (3) 성공일 때만
 │               └─ (5) +0x6f9  KeInitThread           [정적] (마) 시작점 = PspUserThreadStartup 주소 (+0x681 LEA)
 └─ (9) +0xfc2  PspInsertProcess                     [관찰] run3 + [정적]
                 ├─ (1) +0xa6   PspLockProcessListExclusive    [정적] 목록 잠금
                 │     (호출 없음: +0xa6 ~ +0x109 에서 직접 ActiveProcessLinks 잇기)
                 ├─ (2) +0x115  PspUnlockProcessListExclusive  [정적] 목록 풀기
                 └─ (3) +0x1e3  ObInsertObjectEx               [정적] 객체 등록 (부모용 핸들 아님)
```

## 확인된 것

- **(가)~(마) 가 모두 `PspAllocateThread` 안에 있고, 순서도 (가) → (나) → (다) → (라) → (마).** [정적]
  - ret: `+0x2e7` < `+0x532` < `+0x5ef` < `+0x631` < `+0x6f9`. `.c` 의 흐름 순서도 같다.
  - (다)(라)는 **유저 스레드일 때만**이다 (5번째 인자 = 유저 CONTEXT 가 0이 아닐 때). 0이면 시스템 스레드 길로 가서 스택·TEB 를 건너뛴다.
  - (라)는 (다)가 성공했을 때만 이어진다 (`+0x5f8 JS` 로 실패면 정리로).
- **points.md 의 `MmCreateTeb` 이름이 맞다.** [정적]
  - `_index.tsv` 에 함수로 있고 (rva `0x9318c4`), `PspAllocateThread +0x631` 에서 불린다. 지도에서 caller 는 이 하나뿐 (깊이 2).
  - 받는 것: 프로세스, InitialTeb, 스레드의 Cid 자리, TEB 를 돌려받을 칸. 돌려받은 TEB 는 `KeInitThread` 입력으로 간다. (결과서 요약)
- **`PspUserThreadStartup` 이 시작점으로 넘어가는 곳이 코드에 있다.** [정적]
  - `+0x681 LEA RAX,[0x140910de0]` (rva `0x910de0` = `PspUserThreadStartup`) → `+0x688` 에서 `rsp+0x128` 에 저장 → `+0x6e9 LEA RDX,[RSP+0x120]` 로 이 묶음을 2번째 인자로 → `+0x6f4 CALL KeInitThread`.
  - 즉 `KeInitThread` 에 주는 "시작 정보 묶음" 의 `+0x8` 칸이 `PspUserThreadStartup` 이다.
  - 01_sprint run4·5 관찰("test.exe 의 모든 새 스레드가 `nt!PspUserThreadStartup` 에서 출발")과 맞는다. [관찰] run4·5 + [정적] run11
  - 이건 **호출이 아니라 주소를 값으로 넘기는 것**이라, 호출 지도에는 `PspUserThreadStartup` 이 안 나온다.
  - 시스템 스레드 길은 다른 시작 함수다: `PspSystemThreadStartup` (`+0x949` LEA, 장부에 없음, 디컴파일로 확인), 보안 프로세스 조건이면 `PspSecureThreadStartup` (`+0xb98`, 장부에 없음, 디컴파일로 확인).
- **유저 쪽 시작 주소는 인자로 받은 CONTEXT 에서 옮겨 적는다.** [정적]
  - `+0x640 ~ +0x647`: `_ETHREAD.StartAddress` ← `CONTEXT.Rip`
  - `+0x64e ~ +0x655`: `_ETHREAD.Win32StartAddress` ← `CONTEXT.Rcx`
  - 필드 이름은 결과서가 PDB 타입으로 확인했다 (`_ETHREAD +0x560 Win32StartAddress`, `_CONTEXT +0x80 Rcx`, `+0xf8 Rip`).
  - 그러니 01_sprint 의 "Win32 Start Address = `test!mainCRTStartup`" [관찰] 은 이 함수가 `CONTEXT.Rcx` 를 옮겨 적은 값이다.
  - 그 CONTEXT 를 누가 채웠는지는 이 함수 밖이다. 이 함수는 받아서 옮기기만 한다.
- **`PspInsertProcess` 는 목록에 올리는 일을 호출 없이 직접 한다.** [정적]
  - 잠금(`+0xa6`)과 풀기(`+0x115`) 사이에 호출이 없다.
  - 그 사이: 목록 끝 포인터가 머리(`PsActiveProcessHead`)를 제대로 가리키는지 검사(틀리면 `+0xc5 INT 0x29` 빠른 실패) → 새 프로세스의 `ActiveProcessLinks`(EPROCESS `+0x1d8`)를 맨 뒤에 잇기(`+0xe4 ~ +0xf1`) → 전역 순번을 1 올려 EPROCESS `+0x678` 에 적기(`+0xf8 ~ +0x109`).
  - run3 에서 `PspCallProcessNotifyRoutines` 때 `!process 0 0 test.exe` 가 보인 것과 맞는다 (목록에 올리기가 `NtCreateUserProcess +0xfc2`, 알림이 그 뒤 `+0x101a` 안). [관찰] run3 + [정적] run11
- **`PspInsertProcess` 의 `ObInsertObjectEx`(`+0x1e3`)는 부모에게 줄 핸들을 만드는 호출이 아니다.** [정적]
  - 핸들을 돌려받는 인자 자리에 0을 넣는다 (`+0x1c1`, `+0x1ca`). 객체를 등록만 하는 모양이다.
  - 부모(cmd.exe)가 받는 프로세스 핸들은 run9 의 `NtCreateUserProcess +0x10a7` `PspCreateObjectHandle` 이다. 둘은 다른 일이다.
  - 다만 두 호출은 같은 접근 상태 버퍼(`NtCreateUserProcess` 의 `rbp+0x400`)를 쓴다.
  - `ObInsertObjectEx` 인자 이름은 확인 못 했다 [추측].
- 감사·잡·디버그 포트 [정적]
  - 감사: 켜져 있으면 `SeAuditProcessCreation` (`+0x78`).
  - 잡: 부모가 잡에 있으면 `PspImplicitAssignProcessToJob` (`+0xd9`) 로 같은 잡에 넣는다. **목록 잠금 전**이다.
  - 디버그 포트: `DbgkCopyProcessDebugPort` (`+0x143`) 로 부모의 디버그 포트를 새 프로세스에 복사한다.

## 아직 확인 필요

- **첫 스레드가 `PspAllocateThread` 의 어느 길을 타나** — 정적으로 못 정함.
  - 갈림길은 11번째 인자(스택 정보). 0이 아니면 보통 길(스택 + TEB 새로 만듦), 0이면 함수 먼 쪽 덩어리로 가서 Pico 길 또는 "지금 스레드를 본뜨는" 길.
  - `NtCreateUserProcess` 가 이 인자에 0이 아닌 값을 주는지는 코드로 못 정했다 (`+0xadd` 에서 `rbp+0x720` 이거나 `+0xbb5` 에서 0).
  - 확인: `nt!PspAllocateThread` 입구에서 `poi(@rsp+0x58)` 이 0인지. 먼 덩어리 입구 `nt+0xb0d315` 에 걸어서 안 걸리면 보통 길.
- **`KeInitThread` 시작 정보 묶음의 실제 값**: `PspAllocateThread+0x6f4` (call 직전)에서 `dqs @rdx l0n0xc` — `+0x8` 칸이 `PspUserThreadStartup` 인지, `+0x18` 칸(`StartAddress`)이 무엇인지.
- **Win32 Start Address 가 `CONTEXT.Rcx` 와 같은가**: 입구에서 `dt nt!_CONTEXT poi(@rsp+0x28) Rip Rcx`, `+0x6f9` 에서 `dt nt!_ETHREAD @rsi StartAddress Win32StartAddress`.
  - [추측] `CONTEXT.Rip` 는 유저 첫 위치 쪽 값일 수 있다. 커널 콜스택 유저 자리에 `ntdll!RtlUserThreadStart` 가 보였기 때문(run4). 확인 전엔 모른다.
- **`PspInsertProcess` 목록 올리기 실행**: `+0x115` (풀기 직후)에서 `!process 0 0 test.exe`. `+0x1e3` 앞뒤로 부모 핸들 표가 바뀌는지 (`!handle 0 3 <부모>`).
- **먼 덩어리 오프셋의 WinDbg 표기**: `+0x2caa17` 같은 큰 오프셋 줄은 결과서가 .pdata(예외표) 체인으로 **진짜 이 함수 몸통**임을 확인했다 [정적].
  - 하지만 PDB 공개 심볼이 그 구간에 없어서 WinDbg 가 `nt!PspAllocateThread+0x2caa17` 로 안 보이고 엉뚱한 이름 + 큰 오프셋으로 보일 수 있다 [추측].
  - 확인: `ln nt+0xb0d383`, `ln nt+0xb0d315`.
- 정상 길이면 `PspSetupUserStack` (`+0x5ef`), `MmCreateTeb` (`+0x631`) 의 반환값과 TEB 주소, 그 TEB 가 `KeInitThread` 묶음 `+0x28` 칸과 같은지.
- 추천 멈춤점 (모두 부모 cmd.exe 로 `/p` 필터, run3 방식. `PspAllocateThread` 는 다른 스레드 생성 때도 불린다):
  - `nt!PspAllocateThread`, `+0x6f4`, `+0x6f9`, `nt!PspInsertProcess+0x115`, `nt+0xb0d315`
- 정적으로 더 볼 것
  - 스레드 락(`ThreadLock`)을 성공 길에서 푸는 호출이 이 함수 안에 없다. 호출자 `PspInsertThread` 쪽일 수 있다 [추측] → Q4 에서 같이.
  - `KeInitThread` 가 `PspUserThreadStartup` 주소를 어떻게 쓰나 (커널 스택에 시작점으로 심는 것으로 보임 [추측]) → Q6·Q7.
  - CONTEXT `Rip`/`Rcx` 를 누가 채우나 → Q7 (`PspCreateUserContext`, run9).

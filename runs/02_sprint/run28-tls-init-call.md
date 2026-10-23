# 2026-10-23 run28 — TLS 콜백 사슬: `LdrpCallTlsInitializers` 는 **`LdrpTlsList` 에서 콜백 배열을 꺼내 0 까지 돌고**, `LdrpCallInitRoutine` → `LdrpCallInitRoutineInternal` 이 **한 칸 밀어** 부르고, `ImageTlsCallbackCaller` 가 콜백을 **(DllBase, 이유, 0)** 으로 부른 뒤 늘 TRUE (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntdll 26100.9457, pdb GUID `C3093720177DA6851519DCFFE8DF53FE1` (flow.md 기준과 같음). 이미 analyze 된 프로젝트를 다시 썼다 (analyze 안 돌림).
- 버전 대조 (결과서 + 이번에 `.asm` 다시 봄):
  - `LdrpInitializeNode` call `+0x166`/ret `+0x16b` → `LdrpCallTlsInitializers`, call `+0x197`/ret `+0x19c` → `LdrpCallInitRoutine` (run7·run8 콜스택과 같음).
  - run8 `[관찰]` "`LdrpCallTlsInitializers +0x19b` 에서 `rcx = ntdll!ImageTlsCallbackCaller`" 가 `.asm` `+0x18f LEA RCX,[0x1800e83e0]` → `+0x196` call → ret `+0x19b` 와 같다.
- 이름: 네 함수 모두 `_index.tsv` 에 PDB 이름 그대로 있다. `ImageTlsCallbackCaller` 는 PDB 에 공개 심볼이 아니라 **지역 함수 기호**로 들어 있다 (결과서가 `llvm-pdbutil` 로 확인). 이름은 run8 WinDbg 표시와 같다.

| 함수 | rva | 크기 (`_index.tsv`) | 직접 호출 |
|---|---|---|---|
| `ntdll!LdrpCallTlsInitializers` | `0x10e50` | 963바이트 | 6곳 + 예외 필터 1곳 |
| `ntdll!LdrpCallInitRoutine` | `0x10ba0` | 667바이트 | 8곳 + `__finally` 조각 4곳 |
| `ntdll!LdrpCallInitRoutineInternal` | `0x11eab8` | 47바이트 | 1곳 (CFG 간접) |
| `ntdll!ImageTlsCallbackCaller` | `0xe83e0` | 55바이트 | 2곳 (CFG 간접 1, 직접 1) |

## 목적

- Q16: flow.md 2.7 의 TLS 콜백 길 세 함수(+ 지도에서 찾은 `LdrpCallInitRoutineInternal`) 본문.
  - `LdrpCallTlsInitializers` 가 콜백 목록을 어디서 꺼내나, 콜백마다 도나, 무엇을 넘기나, 언제 하나도 안 부르나, 락·예외는.
  - `LdrpCallInitRoutine` 이 인자를 그대로 넘기나, 무엇을 돌려주나, 로그·ETW·`__finally` 는.
  - DllMain 이 `(DllBase, 1, r9)` 를 받는 "한 칸 밀기" 가 어디서 일어나나 (run27 `[추측]`).
  - `ImageTlsCallbackCaller` 가 진짜 콜백을 어떻게 부르나, 왜 끼우나.
  - 그림 2 의 `LdrpCallInitRoutine` → `?` → `ImageTlsCallbackCaller` 칸을 함수 사슬로 채우기.

## 방법

- 정적: `_workspace/sast/23_tls-init-call.md` — ntdll, `^(LdrpCallTlsInitializers|LdrpCallInitRoutine|LdrpCallInitRoutineInternal|ImageTlsCallbackCaller)$` depth 1 (17개 뽑음). callee 본문은 안 읽음.
  - 범위를 넓힌 것 (depth 0, 호출 자리·칸 오프셋만): `LdrpInitializeThread`·`LdrpProcessDetachNode`·`LdrShutdownProcess`·`LdrShutdownThread` (부르는 자리 비교), `LdrpAllocateTlsEntry`·`LdrpHandleTlsData`·`LdrpInitializeTls`·`LdrpAllocateTls` (TLS 항목을 누가 만드나), `LdrpDispatchUserCallTarget`·`LdrpValidateUserCallTarget`·`_guard_dispatch_icall` (CFG 방식). 전역 참조 `refs`: `LdrpTlsList`, `LdrpTlsLock`, `ImageTlsCallbackCaller`, rva `0x1e6000`.
- 결과서: `_workspace/sast/23_tls-init-call_result.md` (**분석성공**)
- 근거 파일 (레포 밖, 커밋 안 함): `ms/disassemble/ntdll/{LdrpCallTlsInitializers,LdrpCallInitRoutine,LdrpCallInitRoutineInternal,ImageTlsCallbackCaller,LdrpAllocateTlsEntry,LdrpHandleTlsData,LdrpInitializeTls}.asm`, `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_refs/`.
- 예외 범위는 결과서가 PE unwind 표(`pefile`)로, 예외 착지점 5바이트는 원본 바이트로 읽었다. KERNELBASE·kernel32·ntdll 의 TLS 디렉터리 유무도 결과서가 `pefile` 로 PE 파일에서 읽었다.
- 점프 목적지는 `절대 주소 − 함수 시작` 으로 이번에 `.asm` 에서 다시 셈했다 (아래 "도구 메모").

## 결과

### 정적 — 먼저 알아 둘 칸

- `_LDR_DATA_TABLE_ENTRY` (항목, PDB): `+0x30 DllBase`, `+0x38 EntryPoint`, `+0x48 FullDllName`, `+0x58 BaseDllName`, `+0x6e TlsIndex`.
- **TLS 목록 항목** (`LdrpTlsList` 에 매달린 0x48바이트, PDB 타입 없음 — 오프셋+역할):
  - `+0x00` 다음 / `+0x08` 앞 (원형 이중 연결 리스트)
  - `+0x10 ~ +0x37` **TLS 디렉터리 사본** 0x28바이트. PE 규격 `IMAGE_TLS_DIRECTORY64` 배치대로면 `+0x10` 시작, `+0x18` 끝, `+0x20 AddressOfIndex`, **`+0x28 AddressOfCallBacks`** (PE 규격 기준 `[정적]`, PDB 아님)
  - `+0x38` 모듈 항목 포인터, `+0x40` TLS 번호
- 전역 (PDB 이름): `LdrpTlsList` (rva `0x1cc938`, 목록 머리), `LdrpTlsLock` (rva `0x1d2460`, SRW 락).
- 이유 값 (윈도 상수): 0 프로세스 끝, 1 프로세스에 처음 붙음, 2 새 스레드, 3 스레드 끝.

### 정적 — 호출 지점 (`_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 (요약) | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpCallTlsInitializers` | `+0x113` | `+0x118` | `RtlpAcquireSRWLockSharedContended` | `LdrpTlsLock` 빠른 잡기 실패 (경합) | [정적] |
| `ntdll!LdrpCallTlsInitializers` | `+0x180` | `+0x185` | `LdrpLogInternal` | 콜백마다 (찍는지는 callee 몫) | [정적] |
| `ntdll!LdrpCallTlsInitializers` | `+0x196` | `+0x19b` | `LdrpCallInitRoutine` | **콜백마다** | [정적] + [관찰] run8 (`rcx`) |
| `ntdll!LdrpCallTlsInitializers` | `+0x1e8` | `+0x1ed` | `NtSetInformationThread` | 락 추적 칸의 맨 위 비트가 켜졌을 때 | [정적] |
| `ntdll!LdrpCallTlsInitializers` | `+0x1fe` | `+0x203` | `RtlRaiseStatus(0xc0000264)` | 안 잡힌 락을 풀려 할 때 (이상 상태) | [정적] |
| `ntdll!LdrpCallTlsInitializers` | `+0x386` | `+0x38b` | `NtAlertThreadByThreadIdEx` | 락 풀 때 기다리는 스레드가 있으면 | [정적] |
| `LdrpCallTlsInitializers$filt$0` | `+0x2b` | `+0x30` | `RtlReportException` | 콜백이 예외를 던졌을 때 (운영체제가 부름) | [정적] |
| `ntdll!LdrpCallInitRoutine` | `+0x8e` | `+0x93` | `LdrpCallInitRoutineInternal` | 늘 | [정적] |
| `ntdll!LdrpCallInitRoutine` | `+0xfa` | `+0xff` | `__security_check_cookie` | 늘 (끝) | [정적] |
| `ntdll!LdrpCallInitRoutine` | `+0x12f` | `+0x134` | `LdrpLogError(0xc0000142, 0x1496, 이유, 0)` | 결과 `AL == 0` 이고 이유 `== 1` | [정적] |
| `ntdll!LdrpCallInitRoutine` | `+0x1b5` | `+0x1ba` | `memset` | 부르기 전 ETW 이벤트 (아래 세 조건) | [정적] |
| `ntdll!LdrpCallInitRoutine` | `+0x1e0` | `+0x1e5` | `RtlGetCurrentServiceSessionId` | 같은 ETW 갈래 | [정적] |
| `ntdll!LdrpCallInitRoutine` | `+0x213` | `+0x218` | `NtTraceEvent` | 같은 ETW 갈래 | [정적] |
| `ntdll!LdrpCallInitRoutine` | `+0x230` | `+0x235` | `RtlGetCurrentServiceSessionId` | 부른 뒤 ETW 갈래 | [정적] |
| `ntdll!LdrpCallInitRoutine` | `+0x27c` | `+0x281` | `LdrpLogEtwEvent(0x1496, DllBase, 0xff)` | 부른 뒤 ETW 갈래 | [정적] |
| `LdrpCallInitRoutine$fin$0` | `+0xc`·`+0x4c` | `+0x11`·`+0x51` | `RtlGetCurrentServiceSessionId` | `__finally` (예외로 빠져나갈 때도) | [정적] |
| `LdrpCallInitRoutine$fin$0` | `+0x9c` | `+0xa1` | `LdrpLogEtwEvent` | `__finally`, ETW 켜졌을 때 | [정적] |
| `LdrpCallInitRoutine$fin$0` | `+0xcc` | `+0xd1` | `LdrpLogError` | `__finally`, 예외로 끝났거나 (결과 0 · 이유 1) | [정적] |
| `ntdll!LdrpCallInitRoutineInternal` | `+0x1d` | `+0x22` | **CFG 간접 호출** (대상 = 받은 `rcx`) | 늘 | [정적] |
| `ntdll!ImageTlsCallbackCaller` | `+0x16` | `+0x1b` | **CFG 간접 호출** (대상 = 받은 `r8` = 진짜 콜백) | 콜백 ≠ `RtlAcquireSRWLockExclusive` | [정적] |
| `ntdll!ImageTlsCallbackCaller` | `+0x29` | `+0x2e` | `RtlAcquireSRWLockExclusive` | 콜백 == `RtlAcquireSRWLockExclusive` 일 때만 | [정적] |

### 정적 — `LdrpCallTlsInitializers(ECX = 이유, RDX = 모듈 항목)` 순서

1. 입구: `+0x1a RDI = RDX` (모듈 항목), `+0x1d R12D = ECX` (이유).
2. **락 잡기 (공유)**: `+0x6d CMPXCHG.LOCK [LdrpTlsLock], 0x11` (락이 0 이면 "잠김 + 공유 1"). 실패하면 `+0x76 JNZ +0x10d` → `+0x113 RtlpAcquireSRWLockSharedContended` (ret `+0x118` → `+0x7c`).
   - 앞뒤로 TEB `+0x1850` 이 가리키는 8칸 표에 락 주소를 적고 (`+0x26 ~ +0x63`) 나중에 지운다 (`+0xbd ~ +0x1f0`). run23 의 "SRW 락 소유 추적" `[추측]` 과 같은 모양. PDB 는 이 칸을 `SchedulerSharedDataSlot` 이라 부르지만 쓰임새(8칸 표)와 안 맞아 이름은 `[추측]` 으로 둔다.
3. **목록 찾기**: `+0x81` `LdrpTlsList` 를 돌며 `+0x98 CMP [항목+0x38], RDI` — 이 모듈 항목을 가리키는 TLS 항목을 찾는다. 못 찾으면 `+0xa7 CMOVZ` 로 0.
   - 콜백 목록은 **모듈 항목(`_LDR_DATA_TABLE_ENTRY`)의 칸이 아니라 `LdrpTlsList` 항목**에서 꺼낸다.
4. **락 풀기 (인라인)**: `+0xae CMPXCHG.LOCK [LdrpTlsLock], 0` (기대 `0x11`). 안 맞으면 `+0xb7 JNZ +0x1f5` 의 느린 풀기 (같은 함수 안 `+0x1f5 ~ +0x3d1`, 따로 부르는 함수 없음).
   - **락을 쥐는 건 목록 찾기(`+0x66 ~ +0xb7`)뿐이다.** 콜백 고리는 락을 푼 뒤다.
5. 못 찾았으면 `+0xe2 TEST RBX` → `+0xe7` 끝. 찾았으면 `+0x11d RBX = [항목+0x28]` (`AddressOfCallBacks`) → `+0x126`/`+0x129` 0 이면 끝.
6. **콜백 고리** (`+0x132 ~ +0x19b`):
   - `+0x132 RSI = [RBX]` → `+0x138` 0 이면 `+0x19d` → `+0xe7` 끝.
   - `+0x142 RBX += 8` (부르기 **전에** 다음 칸으로)
   - `+0x180 LdrpLogInternal` (ret `+0x185`) — `"Calling TLS callback %p for DLL \"%wZ\" at %p\n"`, 레벨 2, 파일 `minkernel\ldr\ldrtls.c`.
   - `+0x185 R9 = RSI` (**진짜 콜백 주소**), `+0x188 R8D = R12D` (**이유**), `+0x18b RDX = [RDI+0x30]` (**DllBase**), `+0x18f RCX = ImageTlsCallbackCaller`.
   - `+0x196 LdrpCallInitRoutine` (ret `+0x19b`) → `+0x19b JMP +0x132` (고리 머리). 반환 `AL` 은 안 본다.
   - 이유 값은 거르지 않는다 (이 함수 안에 이유 비교가 없다).
7. **콜백을 하나도 안 부르는 조건 셋**: (a) `LdrpTlsList` 에 이 모듈이 없다 (`+0xe2`), (b) `AddressOfCallBacks` 가 0 (`+0x129`), (c) 배열 첫 칸이 0 (`+0x138`).
8. **TLS 데이터를 마련하지 않는다.** 이 함수의 호출은 위 표 7곳뿐이고 할당·복사가 없다. 콜백 부르기만 한다.

### 정적 — 예외 (`LdrpCallTlsInitializers`)

- unwind 표 (`pefile`): 핸들러 `__C_specific_handler`, 범위 1개 = `__try` **`+0x12b ~ +0x1a2`** (끝 미포함) = **콜백 고리 전체**. 배열 포인터 읽기와 락 부분은 밖.
- 필터 `$filt$0` (rva `0x167e00`): `RtlReportException(예외 레코드, 문맥, 0xb)` (`+0x2b`, ret `+0x30`) 뒤 예외 종류를 안 보고 **늘 1** (잡아서 실행).
- 착지점 `+0x1a2` = `JMP +0xe7` 5바이트 (원본 바이트; `.asm` 의 "읽지 않은 틈 `+0x1a2~+0x1a6`"). 호출 없음.
- 그래서 **콜백 하나가 예외를 던지면 보고만 하고, 그 모듈의 남은 콜백은 건너뛰고 끝난다.** 함수는 값을 안 돌려주고 오류를 위로 안 올린다. `LdrpInitializeNode` 는 그대로 진입점 호출로 간다 (run27: TLS 반환값을 버림).
  - run27 은 "TLS 콜백이 예외를 던지면 `LdrpInitializeNode` 의 `__except` 가 잡는다" 고 적었다. 이번 코드로는 **TLS 콜백 예외는 그 안쪽 `LdrpCallTlsInitializers` 의 `__try` 가 먼저 잡는다.** 아래 "정정".

### 정적 — 락 경합 갈래 (`+0x1e8`·`+0x1fe`·`+0x386`)

- 셋 다 **락 풀기 / 추적 칸 정리 쪽**이다. 콜백과는 상관없다.
  - `+0x1e8 NtSetInformationThread(-2, 클래스 0x38, {칸 번호, 0}, 0x10)`: 추적 표에서 `&LdrpTlsLock` 칸을 찾았고(`+0xdc JNZ +0x1a7`) 그 칸 맨 위 비트(`+0x1ab`/`+0x1af`)가 켜졌을 때. 이 함수 안엔 그 비트를 켜는 줄이 없다 (켜는 쪽은 `+0x113` callee 일 것 `[추측]`).
  - `+0x1fe RtlRaiseStatus(0xc0000264)`: 빠른 풀기 실패 + 잠김 비트(bit0) 0 (`+0x1f5 TEST AL,1` / `+0x1f7 JNZ +0x240`).
  - `+0x386 NtAlertThreadByThreadIdEx`: 빠른 풀기 실패 + 기다리는 스레드가 있어 느린 길(`+0x240 ~ +0x3d1`).
- 한 스레드만 이 락을 만지면 셋 다 안 탄다. 로더 일꾼이 같은 때 `LdrpHandleTlsData` (`+0xbc` 에서 같은 락을 **독점**으로 잡음) 를 돌 수 있는지는 정적으로 못 정한다 → 동적 Q25.

### 정적 — `LdrpCallInitRoutine(rcx, rdx, r8d, r9)` (공용 창구)

- **인자 그대로, 순서 그대로** `Internal` 에 넘긴다: 시작 `+0x26 RSI=R9`, `+0x29 R14D=R8D`, `+0x2c R15=RDX`, `+0x2f RDI=RCX` → 직전 `+0x82 R9=RSI`, `+0x85 R8D=R14D`, `+0x88 RDX=R15`, `+0x8b RCX=RDI` → `+0x8e` call (ret `+0x93`).
- **돌려주는 값 = `Internal` 의 `AL` 그대로**: `+0x93 [RSP+0x30]=AL` → `+0xe3 MOVZX EBX` → `+0xec MOVZX EAX,BL`. 가공 없음.
- **ETW 이벤트** (부르기 전 `+0x1b5 ~ +0x213`, 이벤트 `0x14a3` / 부른 뒤 `+0x27c`, 이벤트 `0x1496`) 는 세 조건이 **모두** 일 때만:
  - 로거 바이트 ≠ 0: 공용 데이터 `0x7ffe0384` (`_KUSER_SHARED_DATA.UserModeGlobalLogger[2]`, PDB) 또는 서비스 세션이면 `PEB+0x90` 이 가리키는 `+0x22a` (`_SILO_USER_SHARED_DATA` 같은 칸) (`+0x4c ~ +0x5c`)
  - `PEB.TracingFlags`(`+0x378`) bit 2 = `LibLoaderTracingEnabled` (PDB, `+0x6a TEST [RAX+0x378],4`)
  - 로거 높은 바이트(`0x7ffe0385`) `& 0x20` (`+0x19f`, `+0x250`)
- **오류 로그** `+0x12f LdrpLogError(0xc0000142, 0x1496, 이유, 0)`: **결과 0 이고 이유 1** 일 때만 (`+0xea JZ +0x118` / `+0x118 CMP R14D,1`). 이유가 2·3 이면 FALSE 여도 로그 없음. 상태값 `0xc0000142` 를 실제로 정하는 건 부르는 쪽 (run27 `LdrpInitializeNode +0x298`).
- **`__finally`**: unwind 표 `__try` **`+0x82 ~ +0x97`** (인자 싣기 + call + `AL` 저장), 정리 `$fin$0` (rva `0x167d20`). 본체 `+0x97` 뒤와 같은 일 — ETW 끝 이벤트, 그리고 "예외로 끝났거나 (결과 0 · 이유 1)" 이면 `LdrpLogError`. **예외가 나도 끝 이벤트·오류 로그는 남기고, 예외는 위로 그대로 올라간다.**
- **스택을 바꾸거나 다른 스레드로 넘기지 않는다** (`RSP` 는 프롤로그 `SUB 0x2a0` / 에필로그 `ADD` 뿐, 그런 호출 없음).
- caller 8곳 (`_refs`): `LdrpInitializeNode`, `LdrpCallTlsInitializers`, `LdrpInitializeThread`, `LdrpProcessDetachNode`, `LdrShutdownProcess`, `LdrShutdownThread`, AppVerifier 둘 (`AVrfpLoadAndInitializeProvider`, `AvrfMiniLoadDll`).

### 정적 — `LdrpCallInitRoutineInternal` (47바이트): **한 칸 밀기**

| 줄 | 하는 일 | 결과 |
|---|---|---|
| `+0x4 MOV EAX,R8D` → `+0x12 MOV EDX,EAX` | 받은 `r8d` (이유) | 새 `rdx` |
| `+0xc MOV R10,RDX` → `+0x1a MOV RCX,R10` | 받은 `rdx` | 새 `rcx` |
| `+0x14 MOV R8,R9` | 받은 `r9` | 새 `r8` |
| `+0xf MOV R11,RCX` → `+0x17 MOV RAX,R11` | 받은 `rcx` | `rax` = 부를 함수 |
| `+0x1d CALL` (CFG 중개) → ret `+0x22` | `rax` 를 부름 | |
| `+0x22 [RSP+0x30]=AL` → `+0x26 AL=[RSP+0x30]` → `+0x2e RET` | 결과 `AL` 그대로 | |

- 대상 함수는 **`(받은 rdx, 받은 r8d, 받은 r9)`** 를 받는다. 자기 `__try` 는 없다 (unwind flags 0).

### 정적 — `ImageTlsCallbackCaller` (55바이트): TLS 콜백 어댑터

- 받는 것 `(rcx, rdx, r8) = (DllBase, 이유, 진짜 콜백)`.
- `+0x4 RAX = R8` (콜백 주소) → `+0x13 XOR R8D,R8D` (셋째 인자 = 0) → `+0x16 CALL` CFG 중개 (ret `+0x1b`). `rcx`·`rdx` 는 손 안 댐.
  - 그래서 **콜백은 `(DllHandle = DllBase, Reason = 이유, Reserved = 0)` 을 받는다.**
- 갈림 하나: `+0x7 LEA R8,[RtlAcquireSRWLockExclusive]` / `+0xe CMP` / `+0x11 JZ +0x26` → 콜백이 그 함수와 같을 때만 `+0x29` 직접 호출 (ret `+0x2e`). 컴파일러가 "자주 쓰는 대상" 을 직접 부르게 만든 갈래로 보인다 `[추측]`. sample 콜백은 이 주소가 아니니 `+0x16` 쪽 `[소스]` + `[정적]`.
- **결과는 늘 TRUE**: `+0x1b`·`+0x2e MOV EAX,1`. 콜백의 `rax` 는 버린다.
- 왜 끼우나 (코드에 보이는 것만):
  - TLS 콜백은 반환값이 없는 함수다. 그런데 창구 `LdrpCallInitRoutine` 은 `AL` 이 FALSE 면 오류 로그를 남긴다. 어댑터가 늘 1 을 돌려줘서 그게 안 켜진다 `[정적]`.
  - 창구의 넷째 칸(DllMain 이면 `lpReserved`)에 TLS 는 콜백 주소를 싣는다. 어댑터가 그걸 꺼내 쓰고 `Reserved` 자리는 0 으로 바꾼다 `[정적]`.
  - "TLS 콜백도 같은 창구의 ETW·`__finally`·오류 로그 틀을 쓰게 하려는 것" 이 전부인지는 `[추측]`.
- PE 의 CFG 함수 표에 이 주소가 들어 있다 (`GuardCFFunctionTable +0xc99` 참조, `refs`) = 유효한 간접 호출 대상으로 등록됨.

### 정적 — CFG 중개 (`+0x1d`·`+0x16` 둘 다 같은 곳)

- `CALL 0x180172020` = 링커가 만든 5바이트 중개 `_guard_dispatch_icall$thunk$…` → `JMP _guard_dispatch_icall` (rva `0x166360`) → `JMP [__guard_dispatch_icall_fptr]` (rva `0x1e6000`, `.00cfg` 구역, PE 로드 구성 `GuardCFDispatchFunctionPointer`).
- **전부 `JMP` 라 콜스택에 프레임이 안 생긴다.** 대상 함수의 돌아올 자리는 바로 `Internal +0x22` / `ImageTlsCallbackCaller +0x1b` 다.
- 칸의 **파일 값** = `_guard_dispatch_icall_nop` (rva `0x166480`, `JMP RAX` — 검사 없음). CFG 가 켜진 프로세스에서 이 칸이 `LdrpDispatchUserCallTarget` (rva `0x124580`) 으로 바뀌는지는 정적으로 못 정한다 (ntdll 안에 이 칸에 쓰는 코드가 `refs` 에 없음) → 동적 Q27.
  - `LdrpDispatchUserCallTarget` 를 타면 CFG 비트맵(`LdrSystemDllInitBlock +0xb8`) 한 비트를 보고, 맞으면 `JMP RAX`, 아니면 `+0x47 JMP LdrpHandleInvalidUserCallTarget` (꼬리 점프).

### 정적 — TLS 항목은 누가 만드나 (곁, 호출·칸만)

| 무엇 | 누가 | 근거 |
|---|---|---|
| 항목 하나 (0x48, `LdrpTlsHeap`) + TLS 번호 + 목록 꼬리에 붙이기 | `LdrpAllocateTlsEntry` (rva `0xa6764`) | `+0x2f R8D=0x48`, `+0x54 ~ +0x6b` 디렉터리 0x28바이트를 `+0x10` 으로, `+0xc7 [+0x40]=번호`, `+0xcf [+0x38]=모듈 항목`, `+0x103 ~ +0x10d` `LdrpTlsList` 꼬리 |
| DLL 쪽 | `LdrpHandleTlsData` | `+0x88 RtlImageDirectoryEntryToData(DllBase, 1, 9 = TLS)` → 없으면 건너뜀, `+0xbc` 락 독점, `+0x10b` (ret `+0x110`) `LdrpAllocateTlsEntry`, **`+0x3a8 [항목+0x6e] = 0xffff`** |
| exe 쪽 | `LdrpInitializeTls` (`LdrpInitializeProcess +0x18cb`, ret `+0x18d0`) | `+0x9c` 같은 디렉터리 찾기, `+0xf5` (ret `+0xfa`) `LdrpAllocateTlsEntry`, **`+0x102 [항목+0x6e] \|= 0xffff`**, `+0x15d` (ret `+0x162`) `LdrpAllocateTls` |
| 스레드별 TLS 벡터 | `LdrpAllocateTls` | `LdrpInitializeTls +0x15d`, 새 스레드 `LdrpInitializeThread +0x82`·`+0xc4` (ret `+0x87`·`+0xc9`) |

- 그래서 `LdrpInitializeNode`·`LdrpInitializeProcess` 의 **"`TlsIndex ≠ 0`" = "TLS 디렉터리가 있어서 목록 항목이 만들어졌다"** `[정적]`.

### 정적 — PE 파일의 TLS 디렉터리 (결과서, `pefile`)

| 모듈 | TLS 디렉터리 | 콜백 배열 |
|---|---|---|
| `kernel32.dll` | 없음 | — |
| `KernelBase.dll` | 있음 | 첫 칸이 0 (파일 값, RVA `0x2ab070`) |
| `ntdll.dll` | 없음 | — |

### 정적 — `LdrpCallTlsInitializers` 를 부르는 자리

| 부르는 자리 (call / ret) | `ECX` (이유) | `RDX` | 조건 |
|---|---|---|---|
| `LdrpInitializeNode +0x166` / `+0x16b` (DLL, run27) | 1 | 이 모듈 항목 | `TlsIndex ≠ 0` (`+0x157`) |
| `LdrpInitializeProcess +0x1c94` / `+0x1c99` (exe, run19) | 1 (`EBX`, `+0x1c44`) | `[LdrpImageEntry]` | exe `TlsIndex ≠ 0` (`+0x1c31`) |
| `LdrpInitializeThread +0x28c` / `+0x291` (새 스레드, 모듈마다) | 2 | 순회 중인 모듈 항목 | `TlsIndex ≠ 0` (`+0x27d`). 바로 뒤 `+0x2a0` (ret `+0x2a5`) 이 진입점 `(진입점, DllBase, 2, 0)` |
| `LdrpInitializeThread +0x3bb` / `+0x3c0` (새 스레드, exe) | 2 | `[LdrpImageEntry]` | exe `TlsIndex ≠ 0` + `PebLdr.ShutdownInProgress`(`+0x48`, PDB) == 0 |
| `LdrpProcessDetachNode +0x130` / `+0x135` | 0 | 모듈 항목 | (안 따라감) |
| `LdrShutdownProcess +0x168` / `+0x16d`, `+0x2cc` / `+0x2d1` | 0 | 모듈 항목 / `[LdrpImageEntry]` | 앞쪽은 `[RDI+0x58] ≠ 0` 도 (뜻 모름) |
| `LdrShutdownThread +0x14c` / `+0x151`, `+0x1e0` / `+0x1e5` | 3 | 모듈 항목 / `[LdrpImageEntry]` | |

- `RDX` 는 늘 **모듈 항목**, `ECX` 는 이유. 시동 길 두 자리는 1, 새 스레드는 2.

### 호출 관계

```
TLS 길 (콜백마다 한 바퀴)
LdrpInitializeNode  +0x16b (call +0x166)          [exe 는 LdrpInitializeProcess +0x1c99 (call +0x1c94)]
└─ LdrpCallTlsInitializers(이유, 항목)
    ├─ 락 공유 → LdrpTlsList 에서 [+0x38] == 항목 찾기 → 락 풀기    (경합이면 +0x118 / 느린 풀기)
    ├─ 배열 = [TLS 항목 +0x28]  (0 이면 끝)
    └─ 고리 +0x132 ~ +0x19b  ┐ __try +0x12b ~ +0x1a2 (예외면 남은 콜백 건너뛰고 끝)
        ├─ +0x185  LdrpLogInternal
        └─ +0x19b  LdrpCallInitRoutine(ImageTlsCallbackCaller, DllBase, 이유, 콜백)
             └─ +0x93  LdrpCallInitRoutineInternal                      __try +0x82 ~ +0x97 / __finally
                  └─ +0x22  (CFG 중개 JMP, 프레임 없음) ImageTlsCallbackCaller(DllBase, 이유, 콜백)
                       └─ +0x1b  (CFG 중개 JMP, 프레임 없음) 콜백(DllBase, 이유, 0)   → 늘 TRUE 돌려줌

진입점 길
LdrpInitializeNode  +0x19c (call +0x197)
└─ LdrpCallInitRoutine(EntryPoint, DllBase, 1, r9)        r9 = ProcessStaticImport ? LdrpProcessInitContextRecord 값 : 0 (run27)
    └─ +0x93  LdrpCallInitRoutineInternal
         └─ +0x22  (CFG 중개 JMP, 프레임 없음) _DllMainCRTStartup(DllBase, 1, r9)
```

- 콜백 입구에서 `k` 로 보일 것 (예측, 위에서 아래로 = 안쪽부터): 콜백 ← `ImageTlsCallbackCaller+0x1b` ← `LdrpCallInitRoutineInternal+0x22` ← `LdrpCallInitRoutine+0x93` ← `LdrpCallTlsInitializers+0x19b` ← `LdrpInitializeNode+0x16b` (exe 는 `LdrpInitializeProcess+0x1c99`).

레지스터가 옮겨 가는 모양 (TLS 길):

| 단계 | `rcx` | `rdx` | `r8` | `r9` / `rax` |
|---|---|---|---|---|
| `LdrpCallTlsInitializers +0x196` | `ImageTlsCallbackCaller` | DllBase | 이유 | `r9` = 콜백 |
| `LdrpCallInitRoutine +0x8e` | (그대로) | (그대로) | (그대로) | (그대로) |
| `LdrpCallInitRoutineInternal +0x1d` | DllBase | 이유 | 콜백 | `rax` = `ImageTlsCallbackCaller` |
| `ImageTlsCallbackCaller +0x16` | DllBase | 이유 | **0** | `rax` = 콜백 |
| 콜백 받는 것 | `DllHandle` | `Reason` | `Reserved = 0` | |

진입점 길:

| 단계 | `rcx` | `rdx` | `r8` | `r9` / `rax` |
|---|---|---|---|---|
| `LdrpInitializeNode +0x197` (run27) | EntryPoint | DllBase | 1 | `r9` = CONTEXT 포인터 또는 0 |
| `LdrpCallInitRoutineInternal +0x1d` | DllBase | 1 | `r9` 값 | `rax` = EntryPoint |
| `_DllMainCRTStartup` 받는 것 | `hinstDLL` | `fdwReason = 1` | `lpReserved` | |

## 확인된 것

- (`[정적]`) **TLS 콜백 목록은 모듈 항목이 아니라 전역 `LdrpTlsList` 의 항목 `+0x28` (`AddressOfCallBacks`) 에서 꺼낸다.** `LdrpTlsLock` 은 목록을 찾을 때만 공유로 쥐고, 콜백을 부를 땐 쥐지 않는다.
- (`[정적]`) **콜백 하나 = `LdrpCallInitRoutine` 한 번.** 배열을 0 이 나올 때까지 돈다. 이유 값은 거르지 않는다.
- (`[정적]`) 콜백을 하나도 안 부르는 조건은 셋: 목록에 없음 / `AddressOfCallBacks` 0 / 첫 칸 0.
- (`[정적: PE 파일]` + `[관찰]` run8) run8 의 kernel32 길에서 `LdrpCallTlsInitializers` 가 불렸는데 콜백이 없었던 것과 맞는 후보가 있다: **KernelBase 는 TLS 디렉터리가 있고 콜백 배열 첫 칸이 0**, kernel32 는 TLS 디렉터리가 없다 (그러면 `TlsIndex = 0` 이라 이 함수로 오지도 않음). 그 호출이 KernelBase 였다는 건 `[추측]` → 동적 Q25. 그림 2 의 `[KERNELBASE 추정]` 과 같은 쪽이다.
- (`[정적]`) 콜백 하나가 예외를 던지면 `LdrpCallTlsInitializers` 의 `__try` 가 잡아 보고만 하고 **그 모듈의 남은 콜백을 건너뛴다.** 위로 오류를 안 올리니 그 DLL 의 진입점은 그대로 불린다.
- (`[정적]`) `LdrpCallTlsInitializers` 는 TLS 데이터를 마련하지 않는다. 항목·번호는 `LdrpAllocateTlsEntry` (DLL: `LdrpHandleTlsData`, exe: `LdrpInitializeTls`), 스레드별 벡터는 `LdrpAllocateTls`.
- (`[정적]`) 항목 `TlsIndex`(+0x6e) 를 쓰는 곳 = `LdrpHandleTlsData +0x3a8` (DLL), `LdrpInitializeTls +0x102` (exe). 값은 `0xffff`. 그래서 `TlsIndex ≠ 0` = "TLS 디렉터리가 있어 항목이 만들어졌다".
- (`[정적]`) **`LdrpCallInitRoutine` 은 인자 4개를 그대로 `LdrpCallInitRoutineInternal` 에 넘기고, 그 `AL` 을 그대로 돌려준다.** 앞뒤로 ETW (세 조건), FALSE·이유 1 이면 `0xc0000142` 오류 로그, `__finally` 로 예외 때도 같은 기록.
- (`[정적]`) **한 칸 밀기는 `LdrpCallInitRoutineInternal` 에서 일어난다**: 대상 = 받은 `rcx`, 대상 인자 = (받은 `rdx`, 받은 `r8d`, 받은 `r9`).
  - 진입점 길: `_DllMainCRTStartup(DllBase, 1, r9 값)`. run8 `[관찰]` "`LdrpCallInitRoutine` 의 `r8 = 1` → 바로 뒤 `DllMain` 의 `rdx = 1`" 과 같은 모양 (`_DllMainCRTStartup` → `DllMain` 은 CRT 몫이라 정적으로 안 봄).
- (`[정적]`) **`ImageTlsCallbackCaller` 는 넷째 칸으로 온 콜백을 `(DllBase, 이유, 0)` 으로 부르고 늘 TRUE 를 돌려주는 어댑터다.**
- (`[정적]`) 사이의 CFG 중개는 전부 `JMP` 라 콜스택 프레임이 없다. 콜스택에 보일 자리는 `Internal +0x22`, `ImageTlsCallbackCaller +0x1b`.
- (`[정적]`) 새 스레드 길도 같은 함수를 이유 2 로 부른다 (`LdrpInitializeThread +0x28c`·`+0x3bb`). 01_sprint `[관찰]` "Worker 스레드: 모듈마다 TLS → DllMain(이유 2), 마지막 `ExeTls`(이유 2)" 와 같은 모양.

### flow.md 2.7 `[추측]` 세 줄 판정

| 예전 `[추측]` | 판정 | 근거 |
|---|---|---|
| `ImageTlsCallbackCaller` = 실제 TLS 콜백을 대신 불러 주는 중간 함수 | **맞음** `[정적]`. 덧붙임: `Reserved` 를 0 으로 바꾸고 늘 TRUE 를 돌려준다 | `ImageTlsCallbackCaller +0x4 ~ +0x1b` |
| `LdrpCallInitRoutine` = ntdll 이 유저 코드를 부르는 공용 창구 | **맞음** `[정적]` (caller 8곳: 켜기·TLS·새 스레드·detach·프로세스 끝·스레드 끝·AppVerifier 둘) | `_refs`, 위 절 |
| 한 칸씩 밀어 `DllMain(DllBase, 1, r9)` 로 넘김, 정적 import DLL 의 `lpReserved` = CONTEXT 포인터 | 한 칸 밀기 **맞음** `[정적]` (`Internal +0x4 ~ +0x1a`). `lpReserved` = `LdrpProcessInitContextRecord` 값 (정적 import 일 때) 까지 `[정적]` (run19 + run27 + 이번). 실제 값은 동적 Q27 | 위 표 |

## 정정

- **run27 (그리고 flow.md 2.7·2.8)**: "TLS 콜백이든 DllMain 이든 예외를 던지면 `LdrpInitializeNode` 의 `__except` 가 잡아 `0xc0000142` 가 된다" 에서 **TLS 콜백 쪽은 틀렸다.** TLS 콜백 예외는 그 안쪽 `LdrpCallTlsInitializers` 의 `__try` (`+0x12b ~ +0x1a2`, 필터 늘 1) 가 먼저 잡아 끝내고, 이 함수는 아무 것도 위로 안 올린다. 그래서 `LdrpInitializeNode` 의 `__except` 는 TLS 콜백 예외로는 안 탄다 `[정적]`. DllMain(진입점) 예외는 `LdrpCallInitRoutine` 의 `__finally` 를 거쳐 `LdrpInitializeNode` 의 `__except` 로 가니 run27 말이 맞다.
  - run27 의 Node 코드 읽기 자체(범위 `[+0x10e, +0x1b0)` 가 TLS 호출 자리를 감싼다)는 틀리지 않았다. 안쪽에서 먼저 잡힌다는 걸 그때는 몰랐을 뿐이다.
  - run27 문서 맨 위에 정정 안내 한 줄을 넣었다.

## 아직 확인 필요

동적은 새 번호를 만들지 않고 기존 항목에 붙였다 (오케스트레이터 결정). 오프셋은 `ntdll + rva`, 이번 부팅 값 아님.

- **Q25 (TLS 쪽 인자·배열·KernelBase·락)**:
  - (결과서 D1) 한 칸 밀기 실제로: `ntdll+0x10fe6` (`LdrpCallTlsInitializers +0x196`) 에서 `rcx` = `ntdll+0xe83e0`, `rdx` = DllBase, `r8` = 이유, `ln @r9` (콜백 이름). `ntdll+0x11ead5` (`Internal +0x1d`) 에서 `rax` = `ntdll+0xe83e0`, `rcx` = DllBase, `rdx` = 이유, `ln @r8`.
  - (D3) 배열 고리: `ntdll+0x10f76` (`+0x126`) 에서 `dqs @rbx L3`, 고리 머리 `ntdll+0x10f82` (`+0x132`) 횟수 (sample 은 모듈마다 콜백 1개 `[소스]` → 2번 서고 둘째에 0).
  - (D4) kernel32 길의 "불렸지만 콜백 없음" 이 KernelBase 였나: 입구 `ntdll+0x10e50` 에서 `du poi(@rdx+0x60)` (`BaseDllName` 버퍼) 와 `ecx`, `+0x126` 에서 `dq @rbx L1` (= 0 예측).
  - (D6) 락 경합 길을 안 타나: `ntdll+0x10f63` (`+0x113`), `ntdll+0x11038` (`+0x1e8`), `ntdll+0x111d6` (`+0x386`) 에 안 설 것. 서면 `dq ntdll+0x1d2460 L1` (`LdrpTlsLock`).
- **Q26 (dll1·dll2 콜백) · Q28 (exe 콜백)** (D2): `ntdll+0xe83f6` (`ImageTlsCallbackCaller +0x16`) 에서 `ln @rax` = `dll2!Dll2Tls` → `dll1!Dll1Tls` → `test!ExeTls` 순서, `rcx` = DllBase, `rdx` = 1, `r8` = 0. `+0x11 JZ` 갈래(`ntdll+0xe8406`)에 안 설 것. 콜백 입구에서 `k` 로 위 "호출 관계" 의 예측 프레임.
- **Q27 (진입점 길)**:
  - (D7) `ntdll+0x11ead5` 에서 `rax` 가 진입점일 때 `r8` (= 원래 `r9`) = `poi(ntdll!LdrpProcessInitContextRecord)` 인가, `ntdll+0x10c33` (`LdrpCallInitRoutine +0x93`) 에서 `al`. (기존 `ntdll+0xe767`/`ntdll+0xe76c` 와 같이)
  - (D5) CFG 칸 값: `dq ntdll+0x1e6000 L1`, `ln poi(ntdll+0x1e6000)` (`__guard_dispatch_icall_fptr`) — `_guard_dispatch_icall_nop` (`ntdll+0x166480`) 인가 `LdrpDispatchUserCallTarget` (`ntdll+0x124580`) 인가. 멈춤 없이 아무 때나.
  - (D8) ETW 가 꺼져 있나: `ntdll+0x10d55` (`memset`), `ntdll+0x10db3` (`NtTraceEvent`), `ntdll+0x10e1c` (`LdrpLogEtwEvent`) 에 안 설 것. 값 `db 7ffe0384 L2`, `dd @$peb+0x378 L1` (bit 2).
- **정적 Q56 (TLS 락 세부)**, `[추측]` 그대로: TEB `+0x1850` 8칸 표의 진짜 이름, 추적 칸 맨 위 비트를 켜는 곳 (`RtlpAcquireSRWLockSharedContended` 안일 것), `ImageTlsCallbackCaller` 의 `RtlAcquireSRWLockExclusive` 갈래가 있는 이유, `LdrShutdownProcess +0x168` 의 `[RDI+0x58]` 조건.
- **정적 Q55 (sample dll)**: `_DllMainCRTStartup` → `DllMain` 이 셋째 인자(`lpReserved`)를 그대로 넘기나 (CRT 안. sample 빌드를 정적으로 보면 Q27 전에 답이 나올 수 있음). 실제 값은 그대로 동적 Q27.

## 디버거 메모 / 도구 메모

- **콜스택 예측**: CFG 중개(`_guard_dispatch_icall$thunk$…` → `_guard_dispatch_icall` → 칸)는 전부 `JMP` 라 `k` 에 안 보인다. 콜백 입구의 바로 위 프레임은 `ImageTlsCallbackCaller+0x1b`, 진입점 입구의 바로 위는 `LdrpCallInitRoutineInternal+0x22` 일 것이다. 다른 이름이 끼어 보이면 CFG 칸이 `LdrpDispatchUserCallTarget` 으로 바뀐 경우인데, 그것도 `JMP RAX` 로 끝나 프레임을 안 남긴다 `[정적]`.
- 결과서와 `.asm` 대조 (점프 목적지 `+0x76 → +0x10d`, `+0xb7 → +0x1f5`, `+0xe2 → +0x11d`, `+0x129 → +0xe7`, `+0x138 → +0x19d`, `+0x19b → +0x132`, `+0x1f7 → +0x240`, `+0x11 → +0x26` 등, call·ret 전부): **어긋난 곳 없음.**
  - 작은 것 하나: `ImageTlsCallbackCaller` 크기는 `_index.tsv` 55바이트인데 `.asm` 마지막 `RET` 가 `+0x37` 이라 56바이트로 셈이 된다. 하는 일과는 상관없어 표에는 `_index.tsv` 값을 적었다.
  - `_calls.tsv` 에 `ImageTlsCallbackCaller +0x16` 줄이 두 번 들어 있다 (같은 줄 중복). 호출 수는 2곳으로 셌다.

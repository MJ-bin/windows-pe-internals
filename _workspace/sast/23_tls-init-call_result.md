# 23 tls-init-call — 정적 분석 결과

- 요청서: `_workspace/sast/23_tls-init-call.md` (대기열 Q16, 02_sprint 예정 run28)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), PDB guid_age = `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). 이미 analyze 됨 (`.gpr` + `_index.tsv` + `SetPdb:` 줄 모두 있음) — 다시 안 돌림.
- 뽑은 함수:
  - 요청 네 함수 `^(LdrpCallTlsInitializers|LdrpCallInitRoutine|LdrpCallInitRoutineInternal|ImageTlsCallbackCaller)$` depth 1 → 17개 (callee 본문은 안 읽음).
  - 범위를 넓힌 것 (이유 한 줄씩, 전부 depth 0 또는 지정):
    - `LdrpInitializeThread`·`LdrpProcessDetachNode`·`LdrShutdownProcess`·`LdrShutdownThread`: 질문 5 표 (호출 자리의 `ECX`·`RDX` 만 봄).
    - `LdrpAllocateTlsEntry`·`LdrpHandleTlsData`·`LdrpInitializeTls`·`LdrpAllocateTls`: 질문 1 (TLS 항목 칸이 어디서 채워지나, "마련은 어디" 확인). 호출 목록과 칸 오프셋만 봄.
    - `LdrpDispatchUserCallTarget`·`LdrpValidateUserCallTarget`·`_guard_dispatch_icall` (depth 1): 질문 3 (CFG 검사 방식, 76바이트 안팎).
    - `refs` 4개: `LdrpTlsList`, `LdrpTlsLock`, `ImageTlsCallbackCaller`, rva `0x1e6000` (+ `LdrpCallInitRoutine`·`Internal`) → `ms/disassemble/ntdll/_refs/`.
  - 출력: `ms/decomp/ntdll/`, `ms/disassemble/ntdll/` (`LdrpCallTlsInitializers.asm`, `LdrpCallInitRoutine.asm`, `LdrpCallInitRoutineInternal.asm`, `ImageTlsCallbackCaller.asm` 등), `_calls.tsv`.
- 버전 확인 (알려진 사실 대조): `LdrpInitializeNode` call `+0x166`/ret `+0x16b` → `LdrpCallTlsInitializers`, call `+0x197`/ret `+0x19c` → `LdrpCallInitRoutine` 일치 (표의 `+0x16b`/`+0x19c`). run8 `[관찰]` "`LdrpCallTlsInitializers +0x19b` 에서 `rcx = ImageTlsCallbackCaller`" 도 `.asm` `+0x18f LEA RCX,[ImageTlsCallbackCaller]` / `+0x196` call / ret `+0x19b` 와 일치. **일치.**
- 이름: `ImageTlsCallbackCaller` 는 PDB 에서 **공개 심볼(S_PUB32)이 아니라 지역 함수(S_LPROC32)** 로 들어 있다 (`llvm-pdbutil dump --symbols` 로 확인). 그래서 `pdb_aliases.py --where` 는 "공개 심볼에 없음"이라고 나온다 (아래 "스크립트 문제"). 이름은 run8 WinDbg 관찰과 같다.

## 스크립트 문제

- 막힌 것 없음. 작은 한계 하나:
  - 파일: `.claude/skills/ghidra-headless-export/scripts/pdb_aliases.py` (`--where`).
  - 증상: `ImageTlsCallbackCaller` 를 `--where` 로 찾으면 "PDB 공개 심볼에 없음 (이 빌드에 없거나, 공개 심볼이 아닌 이름)". 실제로는 PDB 에 S_LPROC32 로 있고 Ghidra `_index.tsv` 에도 같은 이름으로 있다.
  - 원인 추정: S_PUB32 만 읽는다.
  - 제안: 메시지에 "지역 함수(S_LPROC32) 이름은 못 본다" 한 줄 더하거나 S_LPROC32 도 읽기. (급하지 않음)

## 질문별 답 [정적]

근거 표기: `.asm` = `ms/disassemble/ntdll/<함수>.asm`. 오프셋은 함수 시작 기준, 라벨은 ret. 아래 오프셋은 다 쓴 뒤 `.asm` 과 다시 맞춰 봤다.

### 1. `LdrpCallTlsInitializers` (rva `0x10e50`, 963바이트; `ECX` = 이유, `RDX` = 모듈 항목)

1. **콜백 목록을 어디서 꺼내나**
   - 답: 모듈 항목(`RDX`, `_LDR_DATA_TABLE_ENTRY`)에 미리 둔 칸을 읽는 게 **아니다**. 전역 TLS 목록 `LdrpTlsList` (PDB 이름, rva `0x1cc938`, 원형 이중 연결 리스트 머리) 를 돌면서 **항목의 `+0x38` 이 이 모듈 항목과 같은 것**을 찾는다. 찾으면 그 항목의 **`+0x28`** 을 읽는다 = TLS 디렉터리 사본의 `AddressOfCallBacks`.
   - 근거 `.asm`:
     - 목록 돌기 `+0x81` (`MOV RAX,[LdrpTlsList]`) ~ `+0xa5`: 항목마다 `+0x98 CMP [RAX+0x38],RDI` (`RDI` = 입력 `RDX`, `+0x1a`), 같으면 `+0xa3 CL=1`. 못 찾으면 `+0xa7 CMOVZ RBX,R13` 로 `RBX=0`.
     - `+0xe2 TEST RBX,RBX` / `JNZ +0x11d` / `+0x11d MOV RBX,[RBX+0x28]` / `+0x121` 에서 `[RSP+0x50]` 에 저장 = 콜백 배열 포인터.
   - 항목 칸은 PDB 타입이 없다 (공개 PDB 에 `_LDRP_TLS_ENTRY`·`_IMAGE_TLS_DIRECTORY64` 없음 — `dump --types` 에서 이름 검색 0건). 오프셋+역할로 확인한 것:
     - `LdrpAllocateTlsEntry` (rva `0xa6764`, 항목 만드는 함수) `.asm`: 항목 크기 `0x48` (`+0x2f R8D=0x48`, `RtlAllocateHeap`, `LdrpTlsHeap`), `+0x54 ~ +0x6b` 에서 입력(TLS 디렉터리) **0x28바이트를 항목 `+0x10` 으로 복사** (16+16+8), `+0xcf MOV [RBX+0x38],모듈 항목`, `+0xc7 MOV [RBX+0x40],TLS 번호`, `+0xab ~ +0xaf` 에서 `[RBX+0x20]` (= 디렉터리 `AddressOfIndex`) 가 가리키는 곳에 번호를 씀, `+0x103 ~ +0x10d` 에서 `LdrpTlsList` 꼬리에 붙임.
     - 디렉터리 사본이 `+0x10` 에서 시작하니, PE 규격(`IMAGE_TLS_DIRECTORY64`: Start `0`·End `8`·Index `0x10`·CallBacks `0x18`)대로 `AddressOfCallBacks` = 항목 `+0x10 + 0x18 = +0x28` (`LdrpAllocateTlsEntry +0x8b ~ +0x93` 이 `+0x10`/`+0x18` 비교, `+0xab` 이 `+0x20` 을 쓰는 것도 같은 배치). 규격 값이라 `[정적]` 이되 PDB 가 아니라 PE 규격 기준.
   - 항목을 누가 만드나: DLL 은 `LdrpHandleTlsData` (`+0x88` `RtlImageDirectoryEntryToData(DllBase, 1, 9 = TLS, &크기)` → 0이면 건너뜀, `+0x10b` ret `+0x110` `LdrpAllocateTlsEntry`), exe 는 `LdrpInitializeTls` (`+0x9c` 같은 함수로 디렉터리 9, `+0xf5` ret `+0xfa` `LdrpAllocateTlsEntry`; 이 함수는 `LdrpInitializeProcess +0x18cb` ret `+0x18d0` 이 부름 — `_calls.tsv`).
   - 덤: 만들기에 성공하면 모듈 항목의 `TlsIndex`(+0x6e) 를 **`0xffff`** 로 쓴다 (`LdrpHandleTlsData +0x3a8`, `LdrpInitializeTls +0x102`). 즉 `LdrpInitializeNode`·`LdrpInitializeProcess` 의 "`TlsIndex ≠ 0`" 은 "TLS 디렉터리가 있어서 목록 항목이 만들어졌다"는 뜻 `[정적]`. (예전 `[추측]` "`TlsIndex` 를 쓰는 곳 = `LdrpInitializeTls` 쪽" 이 맞았고, DLL 쪽은 `LdrpHandleTlsData`.)

2. **콜백 배열을 도는 고리**
   - 답: **있다.** 콜백마다 `+0x196 LdrpCallInitRoutine` (ret `+0x19b`) 을 한 번씩 부르고, `+0x19b JMP +0x132` 로 고리 머리로 돌아간다. 배열 항목이 **0 이면 멈춘다.**
   - 근거 `.asm`: 고리 머리 `+0x132 MOV RSI,[RBX]` / `+0x135 TEST RSI,RSI` / `+0x138 JZ` → `+0x19d JMP +0xe7` (함수 끝 정리). 0 이 아니면 `+0x142 ADD RBX,8` (다음 칸으로 **부르기 전에** 미리 넘김, `[RSP+0x50]` 에 저장) → 로그 `+0x180 LdrpLogInternal` (ret `+0x185`) → `+0x196` call.
   - 이유(`ECX`)는 거르지 않는다: 어떤 이유 값이든 그대로 콜백에 간다 (이 함수 안에 이유 비교 없음).

3. **`+0x196` 에 싣는 네 인자**
   - 답 (이름으로):
     - `rcx` = `ImageTlsCallbackCaller` 주소 (`+0x18f LEA RCX,[0x1800e83e0]`).
     - `rdx` = **DllBase** (`+0x18b MOV RDX,[RDI+0x30]`, `_LDR_DATA_TABLE_ENTRY.DllBase` — PDB 로 `+0x30` 확인).
     - `r8` = **이유** (`+0x188 MOV R8D,R12D`, `R12D` ← 입력 `ECX`, `+0x1d`).
     - `r9` = **진짜 TLS 콜백 주소** (`+0x185 MOV R9,RSI`, `RSI` = 배열 `[RBX]`, `+0x132`).
   - 앞 `+0x180` 로그 호출 인자 (참고): 파일 `"minkernel\ldr\ldrtls.c"`, 줄 `0x4a3`, 함수 `"LdrpCallTlsInitializers"`, 레벨 `2`, 서식 `"Calling TLS callback %p for DLL \"%wZ\" at %p\n"`, 값 = 콜백 주소·`FullDllName`(`+0x48`)·DllBase. 항상 부른다 (찍는지는 `LdrpLogInternal` 안쪽 몫).

4. **콜백을 하나도 안 부르고 돌아가는 조건** (코드에 보이는 세 가지)
   - (a) `LdrpTlsList` 에 이 모듈 항목이 없다 → `+0xe2` 에서 `RBX=0` → 바로 끝.
   - (b) 항목은 있는데 `AddressOfCallBacks`(`+0x28`) 가 0 → `+0x126 TEST RBX` / `+0x129 JZ +0xe7`.
   - (c) 배열 첫 칸이 0 → `+0x138 JZ`.
   - run8 "kernel32 길에서 불렸지만 콜백 호출이 없었다" 와 맞는 후보를 파일에서 찾았다 `[정적: PE 파일, pefile 로 읽음]`:
     - `kernel32.dll` 은 **TLS 디렉터리가 없다** (그러니 `TlsIndex = 0`, 애초에 이 함수로 안 온다).
     - `KernelBase.dll` 은 **TLS 디렉터리가 있고 `AddressOfCallBacks` 가 가리키는 배열의 첫 칸이 0** (파일 값; RVA `0x2ab070`) = 위 (c).
     - `ntdll.dll` 도 TLS 디렉터리 없음.
     - 그 관찰 호출이 KernelBase 항목이었는지는 `[추측]` → 동적 (아래 D4).

5. **`+0x113 RtlpAcquireSRWLockSharedContended` — 무슨 락**
   - 답: 전역 **`LdrpTlsLock`** (PDB 이름, rva `0x1d2460`) 을 **공유(shared)** 로 잡는 인라인 빠른 길이 `+0x6d` `CMPXCHG.LOCK [LdrpTlsLock],0x11` (락이 0 일 때 `0x11` = 잠김 비트 + 공유 1 로 바꿈) 이고, 실패하면(`+0x76 JNZ +0x10d`) `+0x113` 으로 느린 길(경합) 을 부른다 (인자 `rcx=&LdrpTlsLock`, `rdx`=추적 칸, `r8`=본 값; ret `+0x118` → `+0x7c`).
   - **짝 풀기는 인라인**: 빠른 길 `+0xae CMPXCHG.LOCK [LdrpTlsLock],0` (기대 `0x11`→`0`), 안 맞으면(`+0xb7 JNZ +0x1f5`) 느린 푸는 길이 같은 함수 안 `+0x1f5 ~ +0x3d1` 에 풀려 있다 (따로 부르는 함수 없음, 깨울 때만 `+0x386 NtAlertThreadByThreadIdEx`).
   - **락을 잡는 구간은 목록 찾기(`+0x66 ~ +0xb7`)뿐.** 콜백 고리(`+0x132 ~ +0x19b`)는 락을 푼 **뒤**다 (콜백 도는 동안 `LdrpTlsLock` 을 쥐고 있지 않음).
   - 락 앞뒤로 TEB `+0x1850` 이 가리키는 **8칸 표**를 만진다 (`+0x26 ~ +0x57` 빈 칸에 락 주소를 적고, `+0xbd ~ +0x1f0` 에서 지움). sast/18 의 "SRW 락 소유 추적 `[추측]`" 과 같은 패턴. PDB 의 `_TEB` 는 이 칸(`+0x1850`)을 `SchedulerSharedDataSlot` 이라 부르지만 쓰임새(8칸 표)와 안 맞는다 — 이름은 못 믿고 `[추측]` 로 둔다.

6. **`+0x1e8 NtSetInformationThread`, `+0x1fe RtlRaiseStatus`, `+0x386 NtAlertThreadByThreadIdEx` 를 타는 조건**
   - 세 개 다 **락 풀기 / 추적 칸 정리 쪽**이라 콜백과 상관없다. 경합 없는 길에선 안 탄다.
   - `+0x1e8 NtSetInformationThread(-2 = 현재 스레드, 클래스 0x38, {칸 번호, 0}, 0x10)`: 락을 푼 뒤 추적 표에서 `&LdrpTlsLock` 을 담은 칸을 찾았고(`+0xdc JNZ +0x1a7`), 그 칸 값의 **맨 위 비트(바이트 `+7` 의 부호, `+0x1ab ~ +0x1af`)가 켜져 있을 때**만. 이 함수 안에는 그 비트를 켜는 곳이 없다 (쓰는 곳: `+0x63` 락 주소, `+0x1a7 OR 2`, `+0x1ed` 0). 켜는 쪽은 칸 포인터를 받는 느린 획득 함수(`+0x113` callee)일 걸로 보인다 `[추측]`.
   - `+0x1fe RtlRaiseStatus(0xc0000264)`: 풀기 빠른 길이 실패하고(`락 값 ≠ 0x11`) 그 값의 **잠김 비트(bit0)가 0** 일 때(`+0x1f5 TEST AL,1` / `JNZ +0x240`) = 안 잡힌 락을 푸는 이상 상태. 정상 길에선 안 탄다.
   - `+0x386 NtAlertThreadByThreadIdEx`: 풀기 빠른 길이 실패하고, 락에 기다리는 스레드가 있는 값이라 느린 길(`+0x240 ~ +0x3d1`)로 가서 기다림 목록 맨 앞 스레드의 번호(`[RCX+0x18]`)로 깨울 때.
   - test.exe 시동 길에서 안 탈 것인가: 셋 다 "`+0x6d` 에서 락이 0 이 아니었거나(다른 스레드가 쥠), `+0xae` 에서 `0x11` 이 아니었을 때"에만 이어진다. 한 스레드만 이 락을 만지면 안 탄다. 그러나 로더 일꾼 스레드가 같은 시각에 `LdrpHandleTlsData` (`+0xbc` 에서 같은 락을 **독점**으로 잡음) 를 돌 수 있는지는 정적으로 못 정한다 → 동적 (D6).

7. **SEH 필터 (`$filt$0` → `RtlReportException`) 범위**
   - unwind 표(.pdata/.xdata, `pefile` 로 직접 읽음) `LdrpCallTlsInitializers`: 핸들러 `__C_specific_handler`, 범위 표 1개:
     - `__try` = **`+0x12b ~ +0x1a2`** (끝 미포함) = **콜백 고리 전체**(로그 + `LdrpCallInitRoutine` + `JMP` 들). 배열 포인터 읽기(`+0x11d ~ +0x129`)와 락 부분은 밖.
     - 필터 = `LdrpCallTlsInitializers$filt$0` (rva `0x167e00`): 예외 정보를 받아 `RtlReportException(ExceptionRecord, ContextRecord, 0xb)` (필터 `+0x2b`, ret `+0x30`) 을 부른 뒤 **무조건 `EAX = 1` (EXCEPTION_EXECUTE_HANDLER)** 을 돌려준다 (예외 종류를 검사하지 않음).
     - 착지점 = `+0x1a2` = `JMP +0xe7` (rva `0x10ff2` 의 5바이트 `e9 40 ff ff ff` 를 직접 읽음; `.asm` 의 "읽지 않은 틈 `+0x1a2~+0x1a6`" 이 이것) → 함수 끝 정리로 가서 **그냥 돌아간다.** `_calls.tsv` 가 빠뜨린 호출 없음 (착지점에 호출 없음).
   - 답: 콜백 하나가 예외를 던지면 **잡고(보고만 하고) 이 모듈의 남은 콜백을 건너뛴 채 끝낸다.** 다음 콜백으로 안 간다. 고리 전체가 한 `__try` 라서. 함수는 `void` 이고 오류를 위로 안 올려서 `LdrpInitializeNode` 는 그대로 진입점 호출로 간다 (반환값을 안 씀, sast/22). `RtlReportException` 본문은 범위 밖.

8. **TLS 데이터(스레드별 칸)를 마련하는 일도 하나**
   - 답: **아니오, 콜백 부르기만 한다.** 이 함수의 호출은 위 `+0x113`·`+0x180`·`+0x196`·`+0x1e8`·`+0x1fe`·`+0x386`·필터 `+0x2b` 뿐이고, 할당·복사 호출이 없다.
   - 마련하는 쪽 (`_calls.tsv`): 항목(디렉터리 사본)과 TLS 번호 = `LdrpAllocateTlsEntry` (DLL: `LdrpHandleTlsData +0x10b`, exe: `LdrpInitializeTls +0xf5`). 스레드별 벡터 = `LdrpAllocateTls` (`LdrpInitializeTls +0x15d` ret `+0x162`, 새 스레드: `LdrpInitializeThread +0x82` ret `+0x87`·`+0xc4` ret `+0xc9`; `LdrpAllocateTls +0x31` 에서 같은 `LdrpTlsLock` 을 공유로 잡고 `LdrpTlsList` 를 읽음). 예전 `[추측]` "마련은 `LdrpInitializeTls` 쪽" 은 **exe(와 메인 스레드) 쪽만 맞고, DLL 쪽은 `LdrpHandleTlsData`.**

### 2. `LdrpCallInitRoutine` (rva `0x10ba0`, 667바이트)

1. **`+0x8e LdrpCallInitRoutineInternal` (ret `+0x93`) 에 넘기는 네 칸**
   - 답: **받은 인자를 그대로, 순서 그대로** 넘긴다. 시작에서 `+0x26 RSI=R9`, `+0x29 R14D=R8D`, `+0x2c R15=RDX`, `+0x2f RDI=RCX` 로 챙기고, 부르기 직전 `+0x82 R9=RSI`, `+0x85 R8D=R14D`, `+0x88 RDX=R15`, `+0x8b RCX=RDI`. 즉 (rcx, rdx, r8d, r9) 그대로. (`r8` 은 32비트만: 이유.)

2. **돌려주는 값**
   - 답: `LdrpCallInitRoutineInternal` 의 **`AL` 를 그대로** 돌려준다. `+0x93 MOV [RSP+0x30],AL` → 뒤 처리 → `+0xe3 MOVZX EBX,[RSP+0x30]` → `+0xec MOVZX EAX,BL` (0 확장만). 가공 없음. (함수 이름처럼 `BOOLEAN` 으로 쓰는 호출 규약.)

3. **로그·ETW 켜지는 조건, 오류 로그**
   - 부르기 **전** 이벤트 (`+0x1b5 memset(0x240)`, `+0x213 NtTraceEvent(로거, 0x402, 0xa, 버퍼)`, 이벤트 칸에 `0x14a3`, DllBase, 이유): 아래 세 조건이 **모두**일 때.
     - ETW 로거 바이트 ≠ 0: `UserModeGlobalLogger[2]` 의 낮은 바이트. 읽는 곳은 `PEB.SharedData`(`+0x90`, 서비스 세션 ≠ 0 일 때)가 가리키는 `+0x22a` 또는 공용 데이터 `0x7ffe0384` (`+0x4c ~ +0x5c`). PDB: `_KUSER_SHARED_DATA.UserModeGlobalLogger` offset `0x380`(배열 USHORT[16], 인덱스 2 = `0x384`), `_SILO_USER_SHARED_DATA.UserModeGlobalLogger` offset `0x226`(인덱스 2 = `0x22a`).
     - `PEB.TracingFlags` (`+0x378`, PDB) 의 **bit 2 = `LibLoaderTracingEnabled`** (PDB 비트 필드 확인; `+0x6a TEST [RAX+0x378],4`).
     - 로거 USHORT 의 높은 바이트(`0x385`/`0x22b`) `& 0x20` (`+0x19f TEST [RCX],0x20`).
   - 부른 **후** 이벤트 `+0x27c LdrpLogEtwEvent(0x1496, DllBase, 0xff)` (ret `+0x281`): 같은 세 조건 (`+0x97 ~ +0xdd`, 켜짐일 때 `+0x230 ~ +0x255`).
   - 오류 로그 `+0x12f LdrpLogError(0xc0000142, 0x1496, 이유, 0)` (ret `+0x134`): **결과 `AL == 0` 이고 이유 `== 1`** (`+0xe8 TEST BL,BL` / `+0xea JZ +0x118` / `+0x118 CMP R14D,1`). 즉 "프로세스(DLL) 붙이기에서 FALSE". 이유가 1이 아니면(스레드 붙이기 등) FALSE 여도 로그 없음. 로그 뒤 상태값 `0xc0000142` 을 만드는 쪽은 부르는 쪽(`LdrpInitializeNode`, sast/22).
   - TLS 콜백은 늘 TRUE 를 받아서(아래 4번) 오류 로그가 안 켜진다.
   - 평소 test.exe 에서 이 로그·이벤트가 꺼져 있을 거라는 건 `[추측]` — 레지스트리·ETW 세션에 달렸다 (D7).

4. **`__finally` (`$fin$0`) 에서 하는 일**
   - unwind 표: 핸들러 `__GSHandlerCheck_SEH`, 범위 표 1개 = `__try` **`+0x82 ~ +0x97`** (인자 챙기기 + `+0x8e` call + `+0x93` `AL` 저장), 정리 함수 `LdrpCallInitRoutine$fin$0` (rva `0x167d20`, 착지점 없음 = 그냥 finally).
   - `$fin$0` 은 **본체 `+0x97` 뒤와 같은 일**을 한다: ETW 켜짐 검사(`RtlGetCurrentServiceSessionId`) 후 `LdrpLogEtwEvent(0x1496, ...)` (`$fin$0 +0x9c`, ret `+0xa1`), 그리고 "예외로 끝났거나(`[RBP+0x31]` = AbnormalTermination ≠ 0), 결과 0 이고 이유 1" 이면 `LdrpLogError(0xc0000142, 0x1496, 이유, 0)` (`$fin$0 +0xcc`, ret `+0xd1`).
   - 즉 **콜백/진입점에서 예외가 나도 끝 이벤트와 오류 로그는 남기고**, 예외는 위(`LdrpCallTlsInitializers` 의 `__except`, `LdrpInitializeNode` 의 `__except`)로 계속 올라간다.

5. **스택을 바꾸거나 다른 스레드로 넘기나: 아니오.**
   - `RSP` 는 프롤로그 `SUB 0x2a0` / 에필로그 `ADD` 로만 바뀐다. 호출 목록(`_calls.tsv`)은 `Internal`·`__security_check_cookie`·`LdrpLogError`·`memset`·`RtlGetCurrentServiceSessionId`·`NtTraceEvent`·`LdrpLogEtwEvent` 와 `$fin$0` 안 것뿐이고, 스레드·스택 바꾸는 호출 없음. 쿠키 검사 `+0xfa` (ret `+0xff`) 있음.

### 3. `LdrpCallInitRoutineInternal` (rva `0x11eab8`, 47바이트)

1. **한 칸씩 미나: 예.** 대상 함수가 받는 것은 `(rcx, rdx, r8) = (받은 rdx, 받은 r8d, 받은 r9)`, 대상 주소는 받은 `rcx`.
   - 줄 근거 `.asm`:
     - `+0x4 MOV EAX,R8D` (이유, 32비트) → `+0x12 MOV EDX,EAX` : 새 `rdx` = 받은 `r8d`.
     - `+0xc MOV R10,RDX` → `+0x1a MOV RCX,R10` : 새 `rcx` = 받은 `rdx`.
     - `+0x14 MOV R8,R9` : 새 `r8` = 받은 `r9`.
     - `+0xf MOV R11,RCX` → `+0x17 MOV RAX,R11` : `rax` = 받은 `rcx` = 부를 함수.
     - `+0x1d CALL`(간접, 아래) → ret `+0x22`, 결과 `+0x22 MOV [RSP+0x30],AL` / `+0x26 MOV AL,[RSP+0x30]` → `+0x2e RET` (결과 `AL` 그대로).
2. **DllMain 은 `(hinstDLL = DllBase, fdwReason = 1, lpReserved = r9값)` 을 받나: 예, `run27` `[추측]` 이 맞다.** `LdrpInitializeNode +0x197` 이 `rcx=EntryPoint, rdx=DllBase, r8=1, r9=(ProcessStaticImport ? LdrpProcessInitContextRecord 값 : 0)` 으로 부르므로 (sast/22) 진입점 함수는 `(DllBase, 1, r9값)`. 정적 import DLL 일 때 `lpReserved` 가 **`LdrpProcessInitContextRecord` 값**이라는 것까지는 `[정적]`, 그 값이 실제 CONTEXT 인지는 동적 (Q27, `run27` 이 이미 대기).
3. **CFG 검사 방식**
   - `+0x1d` 는 `CALL 0x180172020` = 링커가 만든 짧은 중개 `_guard_dispatch_icall$thunk$10345483385596137414` (`fothk` 구역, 5바이트 `JMP`) → `_guard_dispatch_icall` (rva `0x166360`, `JMP [__guard_dispatch_icall_fptr]`) → 포인터 칸 `__guard_dispatch_icall_fptr` (PDB 공개 심볼, rva `0x1e6000`, `.00cfg` 구역; PE 로드 구성 `_load_config_used +0x78` 의 `GuardCFDispatchFunctionPointer`). **전부 `JMP` 라서 콜스택에 프레임이 안 생긴다** (대상의 돌아올 주소 = `LdrpCallInitRoutineInternal +0x22`).
   - 칸의 **파일에 든 값**은 `_guard_dispatch_icall_nop` (rva `0x166480`, `JMP RAX` 2바이트: 검사 없음). CFG 가 켜진 프로세스에서 이 칸이 `LdrpDispatchUserCallTarget` (rva `0x124580`, 76바이트) 로 바뀌는지는 정적으로 못 정함 (ntdll 코드 중 이 칸에 쓰는 곳은 `refs` 에 없음) → 동적 (D5).
   - `LdrpDispatchUserCallTarget` 를 타면: `RAX` 주소로 CFG 비트맵(`LdrSystemDllInitBlock +0xb8`) 한 비트를 보고(`+0x0 ~ +0x2f`), 맞으면 `+0x23`/`+0x3b JMP RAX`, 아니면 `+0x47 JMP LdrpHandleInvalidUserCallTarget` (꼬리호출, `_calls.tsv` `tail`). `LdrpValidateUserCallTarget` 도 같은 실패 길(`+0x44`).
   - `ImageTlsCallbackCaller` 는 PE 의 CFG 함수 표에 든 주소다 (`GuardCFFunctionTable +0xc99` 에서 참조, `refs`) = 유효한 호출 대상으로 등록되어 있다.

### 4. `ImageTlsCallbackCaller` (rva `0xe83e0`, 55바이트)

1. **받는 인자와 하는 일**
   - 답: `(rcx, rdx, r8) = (DllBase, 이유, 진짜 콜백 주소)` 를 받아 **진짜 콜백을 `(DllHandle = DllBase, Reason = 이유, Reserved = 0)` 로 부른다.**
   - 근거 `.asm`: `+0x4 MOV RAX,R8` (콜백 주소를 `rax` 로), `+0x13 XOR R8D,R8D` (3번째 인자를 0 으로), `rcx`·`rdx` 는 손 안 댐, `+0x16 CALL 0x180172020` (위 3번의 같은 CFG 중개; ret `+0x1b`) 로 `rax` 를 부름.
   - 갈림길 하나: `+0x7 LEA R8,[RtlAcquireSRWLockExclusive]` / `+0xe CMP RAX,R8` / `+0x11 JZ +0x26` → 콜백 주소가 `RtlAcquireSRWLockExclusive` 와 같을 때만 `+0x29 CALL RtlAcquireSRWLockExclusive` (직접 호출, ret `+0x2e`, `r8=0`). 컴파일러가 만든 "자주 쓰는 대상 직접 호출" 갈래로 보인다 `[추측]`. sample 의 콜백(`ExeTls`·`Dll1Tls`·`Dll2Tls`)은 이 주소가 아니니 `+0x16` 쪽을 탄다 (`[소스]` + `[정적]`; 동적 확인 D2).
2. **왜 중간 함수를 끼우나 (코드에 보이는 이유만)**
   - TLS 콜백(`PIMAGE_TLS_CALLBACK`)은 반환값이 없는 3인자 함수인데, `LdrpCallInitRoutine` 은 `AL` 가 FALSE 면 오류를 기록하는 창구다. 이 함수가 **`+0x1b`·`+0x2e` 에서 `MOV EAX,1` 로 늘 TRUE 를 돌려줘서** 콜백이 `AL` 을 안 맞춰도 오류 로그(위 2-3)가 안 켜진다 `[정적: 늘 1 을 돌려줌]`.
   - 창구가 넘기는 3칸 `(DllBase, 이유, 4번째 인자)` 에서 4번째 인자는 DllMain 에선 `lpReserved` 인데, TLS 는 여기에 콜백 주소를 싣고 이 함수가 3번째 인자를 0 으로 바꿔 `Reserved = 0` 으로 보낸다 `[정적]`.
   - 이게 "왜 만들었나"의 전부인지는 `[추측]` (TLS 콜백도 같은 창구의 ETW·`__finally`·오류 로그 틀을 쓰게 하려는 어댑터로 보인다).
3. **결과: 늘 TRUE(1).** `+0x1b MOV EAX,1` / `+0x2e MOV EAX,1`. 콜백의 `rax` 는 버린다. 부르는 쪽 `LdrpCallTlsInitializers` 도 `+0x19b` 뒤에 `AL` 을 안 본다.
4. unwind 표: 핸들러 없음 (flags 0) — 이 함수와 `LdrpCallInitRoutineInternal` 에는 자기 `__try` 가 없다.

### 5. 세 자리 비교 (`LdrpCallTlsInitializers` 에 넘기는 값)

`ECX` 이유 값: 1 = `DLL_PROCESS_ATTACH`, 2 = `DLL_THREAD_ATTACH`, 0 = `DLL_PROCESS_DETACH`, 3 = `DLL_THREAD_DETACH` (윈도 상수).

| 부르는 자리 (call / ret) | `ECX` (이유) | `RDX` | 부르는 조건 | 근거 줄 |
|---|---|---|---|---|
| `LdrpInitializeNode +0x166` / `+0x16b` (Node 길, run27) | `1` | `RDI` = 이 모듈 항목 | 항목 `TlsIndex(+0x6e) ≠ 0` | `+0x157 CMP word [RDI+0x6e],0` / `+0x15c JZ` / `+0x15e MOV RDX,RDI` / `+0x161 MOV ECX,1` |
| `LdrpInitializeProcess +0x1c94` / `+0x1c99` (exe 길, run19) | `1` (`EBX=1`, `+0x1c44`) | `[LdrpImageEntry]` (`+0x1c8b`) | `LdrpImageEntry.TlsIndex ≠ 0` (`+0x1c31 CMP [RDX+0x6e],R12W`, `R12` 는 `+0x1ab1` 에서 0 으로 만든 뒤 안 바뀜) | `+0x1c92 MOV ECX,EBX` |
| `LdrpInitializeThread +0x28c` / `+0x291` (새 스레드, 모듈 순회 안) | `2` | `RDI` = 순회 중인 모듈 항목 (`[RSP+0x28]`) | `TlsIndex ≠ 0` (`+0x27d`) | `+0x284 MOV RDX,RDI` / `+0x287 MOV ECX,2`. 바로 뒤 `+0x2a0` 이 `LdrpCallInitRoutine(진입점, DllBase, r8=2, r9=0)` (스레드 붙이기 DllMain) |
| `LdrpInitializeThread +0x3bb` / `+0x3c0` (새 스레드, exe) | `2` | `[LdrpImageEntry]` | `LdrpImageEntry.TlsIndex ≠ 0` **그리고** `PebLdr.ShutdownInProgress`(`_PEB_LDR_DATA +0x48`, PDB) 가 0 | `+0x33e ~ +0x357`, `+0x3af ~ +0x3b6` |

참고 (요청 밖, 같은 방식으로 읽음):

| 자리 | `ECX` | `RDX` | 비고 |
|---|---|---|---|
| `LdrpProcessDetachNode +0x130` / `+0x135` | `0` (`XOR ECX,ECX`) | `RBX` = 모듈 항목 | `TlsIndex` 비교 상대 `SI` 는 `+0x3c` 에서 다시 채워짐, 안 따라감 |
| `LdrShutdownProcess +0x168` / `+0x16d` | `0` | `RBX` | 추가 조건 `[RDI+0x58] ≠ 0` (뜻 모름) |
| `LdrShutdownProcess +0x2cc` / `+0x2d1` | `0` | `[LdrpImageEntry]` | |
| `LdrShutdownThread +0x14c` / `+0x151` | `3` (`LEA ECX,[RAX+3]`, `RAX=0`) | `RDI` | |
| `LdrShutdownThread +0x1e0` / `+0x1e5` | `3` (`LEA ECX,[RDI+3]`, `RDI=0` `+0x185`) | `[LdrpImageEntry]` | |

- 한 줄 요약: **`RDX` 는 항상 "모듈 항목"(`_LDR_DATA_TABLE_ENTRY *`), `ECX` 는 이유 번호.** 시동 길 두 자리(Node·exe)는 둘 다 `ECX = 1`, 새 스레드 길은 `2`.

### 6. 그림·관찰과 맞추기

1. **TLS 길 한 사슬** (부르는 쪽 → 콜백). 괄호는 그 줄의 ret (WinDbg `k` 에서 보일 `함수+0x..`):

   ```
   LdrpInitializeNode  +0x166 call (ret +0x16b)      [exe 는 LdrpInitializeProcess +0x1c94 (ret +0x1c99)]
     -> LdrpCallTlsInitializers   ...  +0x196 call (ret +0x19b)   [콜백마다]
       -> LdrpCallInitRoutine     ...  +0x8e  call (ret +0x93)
         -> LdrpCallInitRoutineInternal ... +0x1d call (ret +0x22)   [CFG 중개 JMP 들은 프레임 없음]
           -> ImageTlsCallbackCaller ... +0x16 call (ret +0x1b)      [CFG 중개 JMP 들은 프레임 없음]
             -> 콜백 (Dll2Tls / Dll1Tls / ExeTls)
   ```

   콜백 입구에서 `k` 로 보이는 순서: `콜백` ← `ImageTlsCallbackCaller+0x1b` ← `LdrpCallInitRoutineInternal+0x22` ← `LdrpCallInitRoutine+0x93` ← `LdrpCallTlsInitializers+0x19b` ← `LdrpInitializeNode+0x16b` (exe 는 `LdrpInitializeProcess+0x1c99`). 그림 2 의 `?` = `LdrpCallInitRoutine +0x93` 과 `LdrpCallInitRoutineInternal +0x22` 두 줄.

   레지스터가 옮겨 가는 모양:

   | 단계 | `rcx` | `rdx` | `r8` | `r9` / `rax` |
   |---|---|---|---|---|
   | `LdrpCallTlsInitializers +0x196` | `ImageTlsCallbackCaller` | DllBase | 이유 | `r9` = 콜백 주소 |
   | `LdrpCallInitRoutine +0x8e` | (그대로) | (그대로) | (그대로) | (그대로) |
   | `LdrpCallInitRoutineInternal +0x1d` | DllBase | 이유 | 콜백 주소 | `rax` = `ImageTlsCallbackCaller` |
   | `ImageTlsCallbackCaller +0x16` | DllBase | 이유 | **0** | `rax` = 콜백 주소 |
   | 콜백 | `DllHandle` | `Reason` | `Reserved = 0` | |

2. **진입점 길 한 사슬**:

   ```
   LdrpInitializeNode  +0x197 call (ret +0x19c)
     -> LdrpCallInitRoutine ... +0x8e call (ret +0x93)
       -> LdrpCallInitRoutineInternal ... +0x1d call (ret +0x22)   [CFG 중개 JMP 들은 프레임 없음]
         -> _DllMainCRTStartup (진입점)
   ```

   | 단계 | `rcx` | `rdx` | `r8` | `r9` / `rax` |
   |---|---|---|---|---|
   | `LdrpInitializeNode +0x197` (run27) | EntryPoint | DllBase | 1 | `r9` = `ProcessStaticImport` ? `LdrpProcessInitContextRecord` 값 : 0 |
   | `LdrpCallInitRoutineInternal +0x1d` | DllBase | 1 | `r9` 값 | `rax` = EntryPoint |
   | `_DllMainCRTStartup` | `hinstDLL` | `fdwReason = 1` | `lpReserved` | |

   `_DllMainCRTStartup` → `DllMain` 은 CRT 몫이라 정적으로 안 봄 (Q27 동적).
3. **run8 `[관찰]` "`r8=1` 이 바로 뒤 `dll2!DllMain` 의 `rdx=1`" 과 맞나: 맞는다.** `LdrpCallInitRoutine` 입구의 `r8d=1` 이 `Internal +0x4 MOV EAX,R8D` / `+0x12 MOV EDX,EAX` 로 한 칸 밀려 진입점 함수의 `rdx` 가 된다 `[정적]`. (`_DllMainCRTStartup` 가 그 `rdx` 를 `DllMain` 에 그대로 넘기는 건 CRT 쪽이라 run8 관찰 몫.)

## flow.md 2.7 `[추측]` 3줄 판정

| `[추측]` | 판정 | 근거 |
|---|---|---|
| `ImageTlsCallbackCaller` = 실제 TLS 콜백을 대신 불러 주는 중간 함수 | **맞음** `[정적]` (콜백은 `LdrpCallTlsInitializers` 가 `r9` 로 넘기고, 이 함수 `+0x16` 이 CFG 간접 호출). 덧붙일 것: 늘 TRUE 를 돌려주고 `Reserved` 를 0 으로 바꾸는 어댑터 | 4번 |
| `LdrpCallInitRoutine` = ntdll 이 유저 코드를 부르는 공용 창구 | **맞음** `[정적]` (caller 8곳: Node·TLS·스레드 시작·프로세스 끝·스레드 끝·detach·AVrf 둘; 앞뒤 ETW·`__finally`·실패 로그) | 2번, `_refs/LdrpCallInitRoutine.tsv` |
| 한 칸씩 밀어 `DllMain(DllBase, 1, r9)` 로 넘김, 정적 import DLL 의 `lpReserved` = CONTEXT 포인터 | 앞부분 **맞음** `[정적]` (`Internal +0x4 ~ +0x1a`). `lpReserved` = `LdrpProcessInitContextRecord` 값 (정적 import 일 때) 까지는 `[정적]`, **그 값이 CONTEXT 포인터라는 것은 여전히 동적** (Q27) | 3번 |

## 그림 2 `(정적)` 상자 후보

1. **`LdrpCallTlsInitializers`**: "`LdrpTlsList` 에서 이 모듈 항목을 찾아(락 잡는 건 이때뿐) `AddressOfCallBacks` 배열을 0 까지 돈다. 콜백마다 `LdrpCallInitRoutine(ImageTlsCallbackCaller, DllBase, 이유, 콜백)`. 예외가 나면 남은 콜백을 건너뛰고 끝 (`+0x12b ~ +0x1a2` 한 `__try`)". call `+0x196` / ret `+0x19b`.
2. **`LdrpCallInitRoutine` → `LdrpCallInitRoutineInternal`**: "유저 코드를 부르는 공용 창구. 인자 4개를 그대로 `Internal` 로 (`+0x8e`/`+0x93`), 앞뒤로 ETW 이벤트·`__finally`, FALSE 이고 이유 1 이면 `0xc0000142` 로그." 이 두 줄(`+0x93`, `+0x22`)이 그림 2 의 `?` 를 채운다.
3. **`LdrpCallInitRoutineInternal`**: 그림에 **넣을 가치 있음** — 47바이트라도 TLS 길·진입점 길 **둘 다** 지나가고, 레지스터 "한 칸 밀기"(`rdx←r8d`, `r8←r9`, `rcx←rdx`, 대상←`rcx`)가 일어나는 유일한 곳이라 이 박스가 없으면 "r8=1 이 DllMain rdx=1 이 된다"가 안 설명된다. WinDbg `k` 에도 `+0x22` 로 보인다. 별도 작은 상자로 두거나, 2번 상자 안에 "한 칸 밀기" 한 줄로 합쳐도 됨 (결정은 flow-writer).
4. **`ImageTlsCallbackCaller`**: "콜백을 `(DllBase, 이유, 0)` 으로 부르고 늘 TRUE(1). 콜백 직전 프레임 (`+0x1b`)." (선택) CFG 중개(`_guard_dispatch_icall` → `__guard_dispatch_icall_fptr` 칸)는 전부 `JMP` 라 프레임이 없으니 상자는 안 만들고 주석 한 줄이면 충분.

## 동적분석으로 확인해야 할 것

대기 중인 번호 (Q25~Q28) 에 붙일 수 있는 것은 번호를 적는다. `ntdll+rva` 는 `ntdll` 시작 주소 기준 (test.exe 일 때만 멈추게 조건은 dast-mentor 몫).

| # | 확인할 것 | 멈춤점 | 볼 것 | 대기 번호 |
|---|---|---|---|---|
| D1 | TLS 길에서 한 칸 밀기가 실제로 일어나나, 콜백이 주소 그대로 `r9` 로 오나 | `ntdll+0x10fe6` (`LdrpCallTlsInitializers +0x196`), `ntdll+0x11ead5` (`LdrpCallInitRoutineInternal +0x1d`) | 앞: `rcx`=`ntdll+0xe83e0`, `rdx`=DllBase, `r8`=이유, `ln @r9`(콜백 이름). 뒤: `rax`=`ntdll+0xe83e0`, `rcx`=DllBase, `rdx`=이유, `ln @r8` | Q25 |
| D2 | `ImageTlsCallbackCaller` 가 콜백을 `(DllBase, 이유, 0)` 으로 부르나, `+0x11 JZ` 안 타나 | `ntdll+0xe83f6` (`+0x16`), ret `ntdll+0xe83fb` (`+0x1b`) | `ln @rax` = `dll2!Dll2Tls` → `dll1!Dll1Tls` → `test!ExeTls` (순서 dll2→dll1→exe, run7·8), `rcx`=DllBase, `rdx`=1, `r8`=0 | Q26, Q28 |
| D3 | 콜백 배열 고리: 배열 안 콜백 수, 0 에서 멈춤 | `ntdll+0x10f76` (`+0x126`), `ntdll+0x10f82` (`+0x132`, 고리 머리) | `+0x126` 에서 `rbx` = `AddressOfCallBacks`, `dqs @rbx L3`; 고리 머리 횟수 | Q25 |
| D4 | kernel32 길에서 "불렸지만 콜백 호출 없음" 이 KernelBase(배열 첫 칸 0)였나 | `ntdll+0x10e50` (입구) | 입구에서 `du poi(@rdx+0x60)` (= `BaseDllName`, `_LDR_DATA_TABLE_ENTRY +0x58` 의 버퍼) 와 `ecx`; `+0x126` 에서 `dq @rbx L1` | Q25 에 더함 |
| D5 | CFG 칸 값 = `_guard_dispatch_icall_nop` (`ntdll+0x166480`) 인가 `LdrpDispatchUserCallTarget` (`ntdll+0x124580`) 인가 | (멈춤 없이) 아무 때나 | `dq ntdll+0x1e6000 L1`, `ln poi(ntdll+0x1e6000)` (`ntdll!__guard_dispatch_icall_fptr`) | 새 항목 후보 (작음) 또는 Q25 에 더함 |
| D6 | `LdrpTlsLock` 경합 길이 시동 길에서 안 타나 | `ntdll+0x10f63` (`+0x113`), `ntdll+0x11038` (`+0x1e8`), `ntdll+0x111d6` (`+0x386`) | 한 번도 안 걸리면 "안 탐". 걸리면 `rdx`(칸), `dq ntdll+0x1d2460 L1` (`LdrpTlsLock`) | Q25 에 더함 |
| D7 | 진입점 길: `rax`=`_DllMainCRTStartup`, `r8`(=r9원본)=`poi(ntdll!LdrpProcessInitContextRecord)`, 결과 `al` | `ntdll+0x11ead5` (`rax` 가 진입점일 때만), `ntdll+0x10c33` (`LdrpCallInitRoutine +0x93`, `al`) | `r8` = `poi(ntdll+0x1d2950)`, `al` | Q27 (이미 있음: `ntdll+0xe767`/`ntdll+0xe76c`) |
| D8 | ETW 이벤트·로그가 꺼져 있나 (`+0x1b5 memset`, `+0x213 NtTraceEvent`, `+0x27c LdrpLogEtwEvent` 안 타는지) | `ntdll+0x10d55`, `ntdll+0x10db3`, `ntdll+0x10e1c` | 안 걸리면 꺼짐. 값: `db 7ffe0384 L2`, `dd @$peb+0x378` (bit 2) | Q25 에 더함 |

- 위 오프셋은 `ntdll+rva` = 함수 rva + `+0x..` : `LdrpCallTlsInitializers` `0x10e50`, `LdrpCallInitRoutine` `0x10ba0`, `LdrpCallInitRoutineInternal` `0x11eab8`, `ImageTlsCallbackCaller` `0xe83e0` (`_index.tsv`).
- 정적으로 못 정한 것: CFG 칸의 실제 값(D5), 콜백이 `RtlAcquireSRWLockExclusive` 갈래를 안 타는 것(D2), 락 경합(D6), ETW 켜짐(D8), KernelBase 가 그 관찰 호출이었는지(D4), `lpReserved` 가 진짜 CONTEXT 인지(D7).
- 디버거가 붙어서 달라질 점: 이 경로에서는 보이는 것 없음 (`NtGlobalFlag`·`BeingDebugged` 를 읽는 곳이 이 네 함수 안에는 없음).

## 정리: 새로 알게 된 것 (flow.md / run 문서용 후보)

- TLS 콜백 목록은 모듈 항목이 아니라 `LdrpTlsList` 항목(`+0x28`)에서 꺼낸다. 락은 목록 찾을 때만.
- 콜백 한 개 = `LdrpCallInitRoutine` 한 번. 예외 한 번이면 그 모듈 남은 콜백 포기.
- `TlsIndex(+0x6e) = 0xffff` 는 "TLS 항목이 만들어졌다" 표시 (`LdrpHandleTlsData +0x3a8`, `LdrpInitializeTls +0x102`).
- 한 칸 밀기는 `LdrpCallInitRoutineInternal` 에서 일어난다. TLS 콜백은 `Reserved = 0`, 진입점은 `lpReserved = r9 값`.
- `KernelBase` 는 TLS 디렉터리가 있고 콜백 배열이 비어 있다. `kernel32`·`ntdll` 은 TLS 디렉터리가 없다 (PE 파일).

## 판정

**분석성공.** 요청서 질문 1~6 모두 `.asm`·PE 파일·PDB 로 답했고, 동적으로 넘길 것은 D1~D8 로 나눠 Q25~Q28 에 붙였다. 알려진 사실 대조 일치. 스크립트 막힌 곳 없음 (작은 한계 하나는 위 "스크립트 문제").

# 56 random-ex-init — 정적 분석 결과

- 요청서: `_workspace/sast/56_random-ex-init.md` (Q116, PE_emulator 요청 9)
- 모듈 / 바이너리: ntdll (`ms/bin/…/System32/ntdll.dll`), PDB GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (run44 덤프의 ntdll PDB 와 **같음**)
- 분석: 이미 되어 있음 (`.gpr` + `_index.tsv` + `SetPdb:` 줄). **다시 analyze 안 했다.**
- 뽑은 것 (모두 depth 0):
  - `RtlpInitRandomExVector` · `RtlRunOnceComplete` · `RtlpRunOnceWaitForInit` · `RtlHeapTrkInitialize` (`ms/ghidra/ntdll.export.sast56a.stdout`)
  - `LdrQueryImageFileExecutionOptionsEx` · `LdrQueryImageFileExecutionOptions` · `LdrpQueryAndUpdateVerifierLaunchCounter` (`…sast56b.stdout`, IFEO 길 확인용으로 보탬)
  - 이미 있던 것은 다시 안 뽑음: `RtlRandom`(= PDB `RtlRandomEx`) · `RtlHpGlobalsInitialize` · `RtlInitializeHeapManager` · `RtlpOpenImageFileOptionsKeyEx` · `RtlpCreateIFEOKeyFilterKey` · `LdrpInitializeExecutionOptions` · `LdrpInitializeProcess` · `RtlpGetModifiedProcessCookie`
  - `_refs` 새로 (7개): `RtlpRandomExInit` · `RtlRandom` · `RtlpInitRandomExVector` · `RtlpRandomExConstantVector`(0x200B) · `RtlpRandomExAuxVarY` · `RtlpOpenImageFileOptionsKeyEx` · `RtlHeapTrkInitialize`
- 버전 확인: **일치** — `LdrInitializeThunk +0x9/+0xe → LdrpInitialize`, `LdrpInitializeInternal +0x55/+0x5a → _LdrpInitialize`, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess`, `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` (`_calls.tsv`). PE_emulator 가 본 돌아올 자리 ntdll `+0x95f45` = `RtlpInitRandomExVector +0x29` 도 정적과 같음.
- 스크립트 문제: 없음
- 오프셋 약속: `call +0x..` = CALL 자리, `ret +0x..` = 다음 줄 (콜스택 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리. 점프 목적지는 (목적지 절대 주소 − 함수 시작) 으로 셌다.

## 한 줄 결론

**run46 이 빠뜨렸다. 힙 전에 누가 켜 둔 게 아니다.** `RtlpRandomExInit` 은 파일 값 0 이고, 이 프로세스에서 처음 켜는 곳이 `RtlInitializeHeapManager +0xd → RtlHpGlobalsInitialize +0x4c` 의 첫 `RtlRandom` 이다 (힙 구간 맨 앞). 그래서 힙 구간 `NtQueryInformationProcess` 는 **2번**, 구간 시스템 콜은 12 → **13** (n=0 이면 12). `[정적]`

## 질문별 답 [정적]

### 1. `RtlpRandomExInit` (rva `0x1ce878`, 8바이트) 을 쓰는 곳

- 답: ntdll 전체에서 이 주소를 쓰거나 넘기는 곳은 **`RtlRandom` 안 5곳뿐**이다. `refs` 5줄이 전부 `RtlRandom` 이고, 8바이트 함수 포인터 표로 가리키는 곳은 0곳 (`ptr` 0). 값은 **run-once 꼴** (`RTL_RUN_ONCE` 의 아래 두 비트 = 상태: 0 안 함 / 1 하는 중 / 2 끝 / 3 비동기):
  - 파일 값 0 (`.data` 디스크 크기 `0x2000`, 이 칸 `0x2878` 은 영 채움 쪽). `[정적]` PE 헤더
  - `RtlRandom +0xa` 읽기 → 아래 두 비트가 2 면 곧장 난수 계산 (`+0x27 JZ → +0x9a`)
  - **`+0x34` `CMPXCHG.LOCK [RtlpRandomExInit],RDI` (RDI=1): 0 → 1 ("하는 중" 잡기)**. 이긴 쪽만 아래로.
  - `+0x45 LEA RCX,[…]` (call `+0x4e` / ret `+0x53` `RtlpInitRandomExVector` 의 1번째 인자, 함수는 이 인자를 안 씀) · `+0x56 LEA RCX,[…]` (call `+0x67` / ret `+0x6c` `RtlRunOnceComplete(&RtlpRandomExInit, 0, 0)` 의 1번째 인자) — 주소를 **넘기는** 줄
  - **끝 값은 `RtlRunOnceComplete +0x67` `XCHG [R11],R8`** (R11 = 넘겨받은 `&RtlpRandomExInit`): flags 0 이라 `2 | (Context 0)` = **2**. (`RtlpInitRandomExVector` 가 1(성공)을 돌려주면 이 길. 0 을 돌려주면 `+0x136` 에서 flags 4 로 불러 값을 0 으로 되돌림. 실제로는 실패하면 `RtlRaiseStatus` 라 0 은 안 돌려줌.)
  - `+0x152 LEA RDX,[…]` (call `+0x15c` / ret `+0x161` `RtlpRunOnceWaitForInit(값, &RtlpRandomExInit)`): 다른 스레드가 "하는 중"(1) 일 때만. 이 안 `+0x4a CMPXCHG.LOCK [RBX],RDX` 가 값에 "기다리는 줄 포인터 | 1" 을 끼워 넣음 (여럿이 동시에 들어올 때만; test.exe 첫 스레드 혼자는 안 탐 `[정적]`)
- 근거: `ms/disassemble/ntdll/_refs/RtlpRandomExInit.tsv` (5줄: `+0xa` READ · `+0x34` READ_WRITE · `+0x45`·`+0x56`·`+0x152` DATA), `RtlRandom.asm`, `RtlRunOnceComplete.asm` (`+0x67 XCHG`), `RtlpRunOnceWaitForInit.asm` (`+0x4a`), `RtlRandom.c`
- 같이 닫히는 전역 (baggage 7.3 `≤P1a` 목록 안 두 개): `RtlpRandomExConstantVector` (rva `0x1d39c0`, **0x200B = 128 dword**) 와 `RtlpRandomExAuxVarY` (rva `0x1d3bc0`, dword). 쓰는 줄은 아래 3번 + `RtlRandom +0xe6` (`XCHG [RCX+RAX*4],R8D`, 벡터 한 칸 바꿈, 매 호출) · `RtlRandom +0x11f` (`ADD.LOCK [AuxVarY],R8D`, 매 호출). `_refs/RtlpRandomExConstantVector.tsv`·`RtlpRandomExAuxVarY.tsv`

### 2. `RtlRandom`(= `RtlRandomEx`) 을 부르는 곳, 그중 `+0x9ed` 보다 앞에 도는 것

- 답: **아니오. `LdrpInitializeProcess +0x9ed` (`RtlInitializeHeapManager`) 보다 앞에서 `RtlRandom` 에 닿는 길은 없다.** 정적으로 `RtlRandom` 에 닿는 길 하나뿐인 앞쪽 후보 (IFEO 필터 키 만들기) 는 열리지 않는다.
- `RtlRandom` 부르는 곳 전부 (`_refs/RtlRandom.tsv`, 호출 4줄 = 함수 3개):
  | 호출 자리 (call / ret) | 앞쪽(힙 전)에 닿나 |
  |---|---|
  | `RtlHpGlobalsInitialize +0x4c / +0x51` · `+0x5c / +0x61` | 닿는 건 `+0x9ed` 이후뿐 (이게 **첫 호출**) |
  | `RtlpCreateIFEOKeyFilterKey +0x7e / +0x83` | 아래 (a) 참고. 길은 있으나 **열리지 않음** |
  | `RtlHeapTrkInitialize +0x1a0 / +0x1a5` | 아니오. 이 함수는 **내보내기만** 있고 내부에서 부르는 곳이 0 (`_refs/RtlHeapTrkInitialize.tsv`: 내보내기 표 `0x1b9314` + `.pdata` + CFG 표). 지도에도 없음. (요청서 caller 3 에는 이 함수가 빠져 있었음) |
- 간접·꼬리 호출 없음: `RtlRandom` 주소를 가리키는 `refs` 5줄 중 함수 안 호출은 전부 `UNCONDITIONAL_CALL`. 나머지 5개는 데이터: 내보내기 표 슬롯 `0x1b973c`(서수 1421 `RtlRandom`)·`0x1b9740`(1422 `RtlRandomEx`) — 둘 다 `0x95db0` 로 가는 것을 PE 파일에서 직접 확인 · `.pdata` `0x1db0b4` · CFG 함수 표 `GuardCFFunctionTable +0x5b4` · 외부 표시 1줄. 메모리 속 8바이트 포인터 표로 가리키는 곳은 0곳. `RtlpInitRandomExVector` 도 호출 1곳 (`RtlRandom +0x4e`) + `.pdata`(`0x1db0c0`) 뿐.
- 앞 묶음에서 닿는 길 찾기 (지도 `_map/LdrInitializeThunk_edges.tsv` 로 `LdrpInitialize` 전부 · `LdrpInitializeInternal` 전부 · `_LdrpInitialize` `+0x167` 까지 · `LdrpInitializeProcess` `+0x9ed` 앞 호출 전부의 아래를 따라가 봄; 일부러 넓게 잡음, 1080개 함수):
  - **`RtlRandom` 에 닿는 길 = 하나**: `LdrpInitializeProcess +0x531/+0x536 → LdrpInitializeExecutionOptions +0xc5/+0xca → RtlpOpenImageFileOptionsKeyEx +0x184/+0x189 → RtlpCreateIFEOKeyFilterKey +0x7e/+0x83 → RtlRandom`.
  - `RtlpHpGlobalsInitialize`·`RtlInitializeHeapManager` 는 이 묶음에서 안 닿음 (당연히, `+0x9ed` 자체).
  - run59 쿠키 사슬: `InitSecurityCookie`(`LdrpGenRandom` `+0x18/+0x1d`, `LdrInitSecurityCookie` `+0x38/+0x3d`) · `LdrInitializePerThreadRng`(꼬리호출 `RtlInitializeTRng`) · `RtlEncodePointer`(`NtQueryInformationProcess` `+0x43/+0x48`) · `LdrInitializeMrdata` — 이 중 `RtlRandom`·`RtlpInitRandomExVector`·`RtlpRandomExInit` 에 닿는 것 **없음** (`_calls.tsv` 호출 목록 전부 확인).
- 근거: `_refs/RtlRandom.tsv`, `_refs/RtlpInitRandomExVector.tsv`, `_map/LdrInitializeThunk_edges.tsv`, `_calls.tsv`

### 3. `RtlpInitRandomExVector` 본문 (rva `0x95f1c`, 218B)

- 답: **씨앗 질의 syscall 1번 + 난수 벡터 채우기. `RtlpRandomExInit` 은 안 건드린다 (켜는 건 `RtlRandom` 이 `RtlRunOnceComplete` 로).**
  - 인자 3개 (RunOnce, Parameter, Context) 는 **안 쓴다** (RCX 를 `+0x1c` 에서 덮어씀). 반환 1 = 성공.
  - `+0x1c ~ +0x24`: `NtQueryInformationProcess(-1, 0x24, &지역4바이트, 4, NULL)` — call `+0x24` / ret `+0x29` (RCX=`-1` `+0x1c`, EDX=`R9+0x20`=`0x24` `+0x20`, R8=`&[RSP+0x58]` `+0xb`, R9D=4 `+0x16`, 5번째 `[RSP+0x20]`=0 `+0x10`). 클래스 `0x24` = 프로세스 쿠키 (run46·sast/54 와 같은 호출). **syscall 이 이 함수에 하나뿐.**
  - 실패 (`+0x2b JS → +0xd3`): `RtlRaiseStatus` call `+0xd5` / ret `+0xda`.
  - 성공: 씨앗 = 질의 값 XOR `LdrSystemDllInitBlock +0x98` (`+0x3d`, 커널이 쓴 난수 4바이트; run59 가 "읽는 곳 다섯" 중 하나로 이미 셈).
  - 루프 `+0x5a ~ +0x92` (카운트 `0x80` = `+0x4e MOV R10D`, `+0x8e SUB R10,1` / `+0x92 JNZ → +0x5a`): `x = (x × 0x7fffffed + 0x7fffffc3) mod 0x7fffffff`, 한 번 돌 때마다 `RtlpRandomExConstantVector[i]` 에 dword 로 씀 (`+0x87`) → **128 dword = 0x200B** (`0x1d39c0 ~ 0x1d3bc0`). 한 번 더 돌린 값을 `RtlpRandomExAuxVarY` 에 씀 (`+0xc5`). 반환 `EAX = R10+1 = 1` (`+0xaa`).
  - 쓰는 전역: `RtlpRandomExConstantVector` (0x200B) · `RtlpRandomExAuxVarY` (4B). **읽는 전역: `LdrSystemDllInitBlock +0x98`.**
- 근거: `RtlpInitRandomExVector.asm` (`+0x24/+0x29`, `+0x3d`, `+0x87`, `+0xc5`, `+0xaa`), `.c`, `_refs/RtlpRandomExConstantVector.tsv`·`RtlpRandomExAuxVarY.tsv`, 사진 대조 아래 5(b)

### 4. `RtlRandom` 의 `RtlpRandomExInit` 갈래 (`+0xa ~ +0x56`, 대기 길 `+0x152`)

- 답: 값의 **아래 두 비트**로 가른다.
  | `RtlpRandomExInit & 3` | 코드 | 하는 일 |
  |---|---|---|
  | **2** (끝) | `+0x24 CMP DL,2` / `+0x27 JZ → +0x9a` | 초기화 건너뛰고 곧장 난수 계산. **이후 모든 호출** (두 번째 `RtlHpGlobalsInitialize +0x5c` 호출도 이 길, syscall 없음) |
  | **0** (안 함) | `+0x2f JNZ` 안 탐 → `+0x34 CMPXCHG 0→1` | 이기면 `RtlpInitRandomExVector` → `RtlRunOnceComplete(…, 0, 0)` → 값 2. 지면 (값이 바뀜) `+0x40 JNZ → +0x29` 로 다시 읽음 |
  | **1** (하는 중) | `+0x2f JNZ → +0x77`, `+0x7a JZ → +0x152` | `RtlpRunOnceWaitForInit` (`NtWaitForAlertByThreadId` call `+0x73` / ret `+0x78`) 으로 기다린 뒤 `+0x161 JMP → +0x29` 로 다시 읽음 |
  | **3** (비동기) | `+0x80 CMP RCX,3` / `+0x86 MOV EAX,0xc00000f0` | `RtlReportCriticalFailure(0xC00000F0, …)` call `+0x95` / ret `+0x9a` (치명 실패). 정상에선 안 옴 |
  - `RtlpInitRandomExVector` 가 0 을 돌려주는 갈래 (`+0x5f JZ → +0x136`) 는 `RtlRunOnceComplete(…, 4, 0)` (되돌리기) call `+0x13b` / ret `+0x140`. 이 함수는 실패를 0 으로 돌려주지 않아서 (실패는 `RtlRaiseStatus`) 안 탐 `[정적]`.
  - 난수 계산 자체 (뒷부분, `+0x9a ~ +0x134`): `*param` 을 한 칸 돌려 쓰고, `RtlpRandomExConstantVector[AuxVarY & 0x7f]` 와 맞바꿔 꺼낸 값에 (`KUSD +0x290` 이 0 이 아니면 `RDRAND` 최대 10번) XOR, 결과를 `AuxVarY` 에 더함. 요청서 범위 밖이라 이름·칸만. `KUSD +0x290` = `ProcessorFeatures` (`+0x274`) 의 28번째 칸 (`[추측]` 이름 `PF_RDRAND_INSTRUCTION_AVAILABLE`).
- 근거: `RtlRandom.asm`, `RtlRandom.c`, `RtlRunOnceComplete.asm` (`+0x67 XCHG`, flags 4 → 값 0), `RtlpRunOnceWaitForInit.asm`

### 5. run46 #1 ~ #8 다시 보기

**(a) 길이 늘 타는가: 예.** `[정적]`
  - `LdrpInitializeProcess +0x9ed / +0x9f2` `RtlInitializeHeapManager`. 이 자리를 건너뛰는 점프 없음 — `+0x9ed` 앞에서 이 자리 너머로 뛰는 점프는 모두 실패 출구 `+0xa7b` (`+0x3b3`·`+0x3e0`·`+0x575`·`+0x630`·`+0x9d4`) 와 `+0x1e3b` (`RtlRaiseStatus`, `+0x784 JNZ`) 뿐. `+0x9e5 ~ +0x9fa` 안으로 뛰어드는 점프도 0. (`+0x9e5` = 인자 `LEA RCX,[RSP+0x98]`, call `+0x9ed`, ret `+0x9f2`, 실패 검사 `+0x9f2 TEST` / `+0x9f4 JS → +0xa7b`)
  - `RtlInitializeHeapManager +0xd / +0x12` `RtlHpGlobalsInitialize` — 함수 맨 처음 call, 조건 없음.
  - `RtlHpGlobalsInitialize +0x4c / +0x51` · `+0x5c / +0x61` `RtlRandom` — `+0x0 ~ +0x5c` 사이 분기 0, 조건 없음. 씨앗 = `[RSP+0x30]` ← `TEB +0x48 (ClientId.UniqueThread) XOR (KUSD +0x320 틱 × +0x4 곱 >> 0x18)`.
  - `RtlRandom` 안: 첫 호출은 상태 0 이라 `+0x34 → +0x4e` 로 들어감 (위 4번). 두 번째 호출은 상태 2.
  - test.exe 조건 (IFEO 필터 키 길): `RtlpOpenImageFileOptionsKeyEx +0x184` 는 세 번째 인자 (R8B, "만들기" 표시, `+0x26 MOV R14B,R8B`; 0 이면 `NtOpenKey` `+0xe7`, 아니면 `NtCreateKey` `+0x14a`) 가 0 이 아니어야 닿는다: `+0x29f TEST R14B,R14B` / `+0x2a2 JZ → +0x265` (0 이면 건너뜀), 0 이 아니면 `+0x2a4 JMP → +0x175` → `+0x184` (그 앞에 `+0x295 CMP RDI,RDX` / `+0x298 JNZ`, `+0x29a TEST R12B,R12B` 조건도 있음). **앞 `+0x531` 길의 호출 `LdrpInitializeExecutionOptions +0xbd XOR R8D,R8D` (call `+0xc5`) 는 R8=0** → `+0x2a2 JZ` 로 건너뜀. 게다가 run46 말대로 test.exe 는 `LdrpInitializeExecutionOptions +0xa6 TEST dword ptr [RAX+0x8],0x4000` / `+0xad JNZ` (목적지 `0x18011aae4`) 로 이 call 자체를 건너뜀 `[정적, run46]` (분기 방향은 run46 의 `Flags=0x4001` 사진 P1 을 믿음, 이번엔 다시 안 셈). `RtlpOpenImageFileOptionsKeyEx` 를 부르는 곳 9줄 (함수 8개) 중 지도에 있는 것 (`LdrQueryImageFileExecutionOptionsEx +0x90`, `LdrpQueryAndUpdateVerifierLaunchCounter +0x9c`·`+0xb4`, `RtlpHpQueryGCTimerInterval +0x27`) 도 모두 직전에 `XOR R8D,R8D` (R8=0). 지도에 없는 4곳 (`RtlQueryImageMitigationPolicy`·`RtlSetImageMitigationPolicy`·`RtlpDeleteEmptyImageFileOptionsKey`·`LdrOpenImageFileOptionsKey`) 은 `LdrInitializeThunk` 뿌리에서 안 닿음.

**(b) 사진 (run44 덤프 로그, 서버 `ms/logs/04_baggage-read_P*.txt`):** rva `0x1ce878` = 실제 주소 ntdll 베이스 `0x7ffafb660000` + `0x1ce878` = `7ffa'fb82e878`.
  | 사진 | 자리 | 값 | 줄 |
  |---|---|---|---|
  | P0 (`PspUserThreadStartup` 입구) | `7ffa'fb82e870` | `????????'????????` 둘 다 (읽기 불가) | `04_baggage-read_P0.txt:3706` |
  | P1 (`LdrInitializeThunk` 입구) | 같음 | **읽기 불가** `????` | `04_baggage-read_P1.txt:3705` |
  | P1a (`LdrpInitializeProcess +0x199d`) | 같음 | 칸 `fb82e878` = **`0x2`** (앞 칸 `fb82e870` = 0) | `…P1a.txt:4097` |
  | P1b (`+0x1bb6`) | 같음 | `0x2` | `…P1b.txt:5215` |
  | P2 (`NtContinue` 입구) | 같음 | `0x2` | `…P2.txt:5227` |
  - 읽기 불가의 폭: P0·P1 둘 다 ntdll `.data` **9쪽 전부** (`0x1cc000 ~ 0x1d4000`) 가 `????`, P1a 에서 9쪽 전부 읽힘. 그래서 사진만으로는 "P1 에 0" 을 못 보인다 (요청서 가르는 기준 "P1 에 0 이고 P1a 에 0 아니면" 의 앞 반이 안 채워짐). 가르는 건 (a) + 2번의 정적이다.
  - 값 `2` = 위 1번 run-once 의 **끝 값** (`2 | Context 0`). 딱 맞음. 힙 구간 (`+0x9ed`) 은 P1 ~ P1a 사이라 구간 안.
  - 같이 맞는 것: `RtlpRandomExConstantVector` (`7ffa'fb8339c0 ~`) 가 P1a 에서 이미 채워져 있음 (`619336c3`, `7e5153be`, … 0 아님; `…P1a.txt:5398`). `RtlpRandomExAuxVarY` (`7ffa'fb833bc0`) = P1a `0xc899afee` (`:5430`) → P1b·P2 `0xb6cd45cc` (`…P1b.txt:6548`, `…P2.txt:6560`). **P1a 와 P1b 사이에 `RtlRandom` 이 또 불렸다는 뜻** (이 칸을 쓰는 곳은 `InitVector +0xc5` 와 `RtlRandom +0x11f` 뿐). 누가 불렀는지는 이번엔 안 봄 (ntdll 안 후보는 위 표 밖에 없음 → 다른 모듈이 내보내기 `RtlRandomEx` 를 부른 것 `[추측]`). 아래 동적 3.

**(c) 결론: run46 정정이 필요하다.** `[정적]`
  - run46 은 `RtlHpGlobalsInitialize` 의 `RtlRandom` 두 번을 "시스템 콜 없이" 로 적었다 (`run46-process-heap.md` "힙 주소 무작위" 줄, 해설 356 줄, `baggage.md` 6장 `RtlRandom · xorshift64*` 줄 "시스템 콜 없음"). `RtlRandom` 의 **첫 호출에서만** 도는 `RtlpInitRandomExVector` 안 syscall 을 못 봤다.
  - 정정할 줄:
    1. 맨 앞 표에 **새 #1** 을 넣는다: `RtlpInitRandomExVector +0x29` (← `RtlRandom +0x4e/+0x53` ← `RtlHpGlobalsInitialize +0x4c/+0x51` ← `RtlInitializeHeapManager +0xd/+0x12`) · `NtQueryInformationProcess` (스텁 번호는 #3 과 같은 스텁) · 프로세스 `-1`, 클래스 `0x24`, 반환 길이 `NULL` · 출력 4바이트 · 횟수 1 (`RtlRandom` 첫 호출만. 두 번째 호출은 상태 2 라 안 탐) · 조건 "항상 (`RtlpRandomExInit`=0 으로 시작)" · `[정적]` + 사진 `0x1ce878 = 2`.
    2. 기존 #1 ~ #12 → **#2 ~ #13** (순서: `RtlHpGlobalsInitialize` 가 `RtlInitializeHeapManager` 의 첫 call 이라 `RtlpHpQueryGCTimerInterval +0x7d` (NtOpenKey 둘) 보다 먼저). 기존 #3 은 **#4**.
    3. 합계: 구간 시스템 콜 12 → **13** (무작위 몫 n=0 이면 #12 (옛 #11) 가 빠져 12). `NtQueryInformationProcess` **1 → 2**. 요약 줄 "`NtOpenKey` 2 · `NtQueryInformationProcess` 1 · …" → "… `NtQueryInformationProcess` 2 · …".
    4. `RtlRandom` 은 "시스템 콜 없음" 이 아님: 첫 호출만 `NtQueryInformationProcess(0x24)` 1번 (`baggage.md` 6장 줄, `run46` "힙 주소 무작위" 줄).
    5. 호출 관계 그림에 `+0x12 RtlHpGlobalsInitialize` 아래 `└─ +0x4c RtlRandom → +0x4e RtlpInitRandomExVector (#1)` 추가.
  - 이미 쓴 다른 문서의 "NtQueryInformationProcess 0x24" 횟수: 프로세스 전체로는 **3번** (`LdrpInitializeProcess +0x12f → RtlEncodePointer +0x43/+0x48` sast/54 가 처음, 힙 구간에서 `RtlpInitRandomExVector +0x24/+0x29` 와 `RtlpGetModifiedProcessCookie +0x22/+0x27`). 셋 다 같은 커널 쿠키를 읽는다 `[추측]` (같은 값인지는 동적).
  - baggage 7.3: `RtlpRandomExInit`·`RtlpRandomExConstantVector*`·`RtlpRandomExAuxVarY*` 세 개를 `≤P1a` 모름 목록에서 3.8 로 옮김 (구간 ② 힙 구간 P1 ~ P1a; 쓰는 줄: 위 1·3번). 3.8 ② "ntdll 직접".

**(d) PE_emulator 에 줄 답: 힙 안에서 켜는 게 맞다. `RtlpRandomExInit` 은 파일 값 0 그대로 두고, 진짜 `RtlRandom` 이 `RtlHpGlobalsInitialize +0x4c` 에서 처음 켜게 둔다. 힙 구간 `NtQueryInformationProcess` 약속 횟수를 2번으로 (첫째 = `RtlpInitRandomExVector +0x29` 클래스 `0x24`, 둘째 = `RtlpGetModifiedProcessCookie +0x27`).** 미리 2 로 켜 두면 syscall 이 한 번 빠지고 `RtlpRandomExConstantVector`(0x200B)·`AuxVarY` 가 진짜와 달리 0 으로 남는다.
  - 에뮬레이터가 맞춰 줄 입력 (정적): (1) 이 syscall 은 앞서 `RtlEncodePointer +0x48` 이 받은 것과 **같은 프로세스 쿠키 4바이트** 를 돌려주는 게 자연스럽다 `[추측]`. (2) `LdrSystemDllInitBlock +0x98` (커널이 쓰는 난수) 가 이미 채워져 있어야 한다 (`RtlpInitRandomExVector +0x3d` 읽기). (3) `KUSD +0x290` 이 0 이 아니면 `RtlRandom +0x103` `RDRAND` 가 돈다 (Unicorn 에서 `RDRAND` 처리).

## 동적분석으로 확인해야 할 것 (Q104 VM 단계에 얹기)

정적으로 근거는 충분해서 **급하지 않다.** 얹을 수 있으면:
1. `RtlRandom` 첫 호출의 돌아올 자리와 값 변화: `bp ntdll+0x95db0` 첫 히트의 `[rsp]` = `ntdll+0x95641` (`RtlHpGlobalsInitialize +0x51`) 인지. 그 시점 `dq ntdll+0x1ce878 L1` = 0 (더 앞 `ntdll+0x9c334` `RtlInitializeHeapManager` 입구에서도 0). `ntdll+0x95e03` (`RtlRandom +0x53`, `RtlpInitRandomExVector` 가 돌아온 직후) 에서 값 **1**, `ntdll+0x95e1c` (`RtlRandom +0x6c`, `RtlRunOnceComplete` 돌아온 직후) 에서 값 **2**.
2. `ntdll+0x95f45` (`RtlpInitRandomExVector +0x29`) 에서 `r eax`, `dd @rsp+0x58 L1` = 쿠키. `RtlEncodePointer +0x48` 과 `RtlpGetModifiedProcessCookie +0x27` 에서 본 값과 같은지.
3. (보너스) P1a ~ P1b 사이 `RtlRandom` 을 또 부른 곳: `bp ntdll!RtlRandom` 두 번째 이후 히트의 `k`. `RtlpRandomExAuxVarY` (ntdll+`0x1d3bc0`) 가 바뀌는 쪽이니 그 값이 P1a `0xc899afee` 에서 P1b `0xb6cd45cc` 로 가는 중 `ba w4` 로 걸 수도 있음. 필수 아님.
4. 추천 멈춤점 정리: `ntdll!RtlInitializeHeapManager` 입구 (`+0x9c334`, 값 0 확인) · `RtlRandom +0x53/+0x6c` · `RtlpInitRandomExVector +0x29`. test.exe 조건 (프로세스 이름) 은 기존 멈춤점 약속 그대로.

## 판정

**분석성공** — 요청서 5(a)~(d) 모두 답했다. 쓰는 곳 전부(1), 앞쪽 길 없음(2), 본문(3), 갈래(4), run46 정정(5). 사진 (b) 는 P0·P1 이 읽기 불가라 단독으로 못 가르지만 정적 근거(쓰는 곳이 `RtlRandom` 뿐, 첫 도달이 `+0x9ed` 안)가 결론을 닫고, P1a 값 `0x2` 가 run-once 끝 값과 정확히 맞아 서로 확인된다.

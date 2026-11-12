# 77 thread-leftovers — 정적 분석 결과

- 요청서: `_workspace/sast/77_thread-leftovers.md` (Q142, run74 남은 정적 ①~⑤)
- 앞 결과서 먼저 읽음: `_workspace/sast/69_thread-create_result.md`, `_workspace/sast/70_thread-stack_result.md`
- 모듈 / 바이너리 (셋 다 `ms/bin/.../System32/`):
  - ntoskrnl — pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음)
  - kernelbase — `72CCE506944DE0267C172AD64287F9BD1`, ntdll — `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/*.pdb.info.txt`)
- 분석: **다시 안 함** (셋 다 `.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 있음). 실행 중인 Ghidra 작업 없음 (`pgrep` 확인) — 모듈 셋 다 잠금 없이 썼다.
- 지도: 새로 안 만듦 (이번 길은 지도 없이 `_calls.tsv`·`refs`·asm 으로 찾았다).
- 뽑은 함수 (모두 `export`, SCRIPT ERROR 0건 — `ms/ghidra/ntdll.export.log` 의 1건은 2026-10-19 옛 줄):
  - kernelbase `^CreateRemoteThreadEx$` depth 1 → 10개 (`ms/decomp/kernelbase/`, `ms/disassemble/kernelbase/`)
  - kernel32 `^CreateThread$`, kernelbase `^CreateThread$` depth 0 (요청서가 "이름만" 이라 했지만 각 64~71바이트 껍데기라 같이 뽑음)
  - ntdll `^RtlCreateUserStack$` depth 0
  - ntoskrnl `^MmAllocateUserStack$` depth 0 (= `NtSetInformationProcess` 클래스 `0x29` 갈래가 부르는 함수), 그 아래 `^(MiAllocateVirtualMemoryPrepare|MiAllocateVirtualMemory)$` depth 0 (이유는 아래 "요청서와 다르게 한 것")
  - ntoskrnl `NtSetInformationProcess`·`PspBuildCreateProcessContext`·`NtCreateThreadEx`·`PspCreateThread`·`PspAllocateThread`·`PspInsertThread`·`PspDeleteCreateProcessContext`·`PspCallProcessNotifyRoutines` 는 **이미 있던 `.asm`** 읽기만.
  - `refs` 5번 (`ms/disassemble/{ntdll,kernelbase}/_refs/`): ntdll `RtlCreateUserStack`, kernelbase `RtlCreateUserStack`·`0x268d80`·`0x29ba60`(진짜 IAT 칸), `KernelBaseGlobalData`.
- 버전 확인 (알려진 사실 대조): **일치**
  - ntoskrnl `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines`
  - ntdll `LdrpInitializeProcess +0x1bb6 / ret +0x1bbb → LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x167 / ret +0x16c → LdrpInitializeProcess`
  - 요청서가 준 rva·크기도 맞음: kernelbase `CreateRemoteThreadEx` `0xca3f0` 2126B, ntoskrnl `NtSetInformationProcess` `0x9329b0` 19144B, ntdll `RtlCreateUserStack` `0xf5130` 709B.

## 스크립트 문제

막힌 것은 없다. 참고만 (고치지 않음):

1. `.asm` 에 **점프 표 목적지가 안 나온다.** `NtSetInformationProcess +0x25b ~ +0x284` 는 "클래스 − 1" 로 바이트 표 (`rva 0x935914`) → 4바이트 표 (`rva 0x935800`) → `JMP RDX` 인데, 어느 클래스가 어디로 가는지는 `.asm` 만으로 안 보인다. 클래스 `0x29` 갈래를 찾으려고 읽기 전용 임시 스크립트로 PE 에서 직접 풀었다 (`.claude/`·`ms/` 밖, 세션 임시 폴더 `.../scratchpad/tools/jt.py`, `python3 -I`). 결과: 클래스 `0x29` → `+0x485`. 제안: `.asm` 머리에 "점프 표 → 목적지" 줄을 붙이는 기능 (sast/69 의 try 범위 제안과 같이 사용자 검토 뒤에).
2. `refs <바이너리> <이름>` 이 **import 이름에는 엉뚱한 칸을 돌려준다.** `refs KernelBase.dll RtlCreateUserStack` 은 외부 위치 (`rva=0xfffffffe800002b1`) 로 풀고, `.rdata` 의 8바이트 값 하나 (`0x268d80`, 이름 표 쪽) 를 "포인터 값" 으로 보고했다 — 진짜 IAT 칸 (`0x29ba60`) 이 아니다. IAT 칸은 PE import 표를 직접 읽어 (`.../tools/imps.py`) 찾았고, `refs KernelBase.dll 0x29ba60` 로 호출 줄을 얻었다. 제안: `refs` 가 이름이 import 일 때 IAT 칸을 따로 찾아 주면 좋겠다.

## 질문별 답 [정적]

### 1. ① `KERNELBASE!CreateRemoteThreadEx` (rva `0xca3f0`)

**읽는 법.** 함수 안에서 `RSP` 는 머리 부분 (저장 7개 + `SUB RSP,0x6d0`) 뒤 값이라 진입 때보다 `0x708` 작다. 그래서 진입 스택 인자 5·6·7·8번째 = `[RSP+0x730]`·`[RSP+0x738]`·`[RSP+0x740]`·`[RSP+0x748]`. 인자 이름은 공개 헤더 순서에 asm 자리를 맞춘 것 `[추측: 이름]`, 자리·상수는 `[정적]`.

| 자리 | 이름 | 줄 |
|---|---|---|
| `RCX` → `R12` | hProcess | `+0x38` |
| `RDX` → `RDI` | lpThreadAttributes | `+0x35` |
| `R8` → `[RSP+0x178]` | dwStackSize | `+0x2d` |
| `R9` → `[RSP+0x140]` | lpStartAddress | `+0x25` |
| `[RSP+0x730]` → `[RSP+0x180]` | lpParameter | `+0x3b`, `+0x43` |
| `[RSP+0x738]` → `R14D` | dwCreationFlags | `+0x118` |
| `[RSP+0x740]` → `RSI` | lpAttributeList | `+0x4b` |
| `[RSP+0x748]` → `R13` | lpThreadId | `+0x53` |

**`kernel32!CreateThread` 가 여기로 오나: 예.** `kernel32!CreateThread` (rva `0x315c0`, 71B) `+0x36` / ret `+0x3d` 가 `CreateRemoteThreadEx` 를 부른다 (IAT 칸 `[0x18008ba50]`, 임포트 이름 `api-ms-win-core-processthreads-l1-1-0` — 실행 때 KernelBase 로 풀림은 `[추측]`). `KERNELBASE!CreateThread` (rva `0x18b3d0`, 64B) 도 몸이 같고 `+0x36` / ret `+0x3b` 에서 직접 부른다. 둘 다 이렇게 넘긴다: `hProcess = -1` (`OR RCX,-1` `+0x32`), `dwCreationFlags & 0x10004` (`+0x11 AND R10D,0x10004` — 남기는 비트는 `0x4` CREATE_SUSPENDED 와 `0x10000` STACK_SIZE_PARAM_IS_A_RESERVATION 둘뿐), `lpAttributeList = NULL` (`+0x1c AND [R11-0x18],0`). 이 래퍼 둘은 `ms/disassemble/kernel32/CreateThread.asm`, `ms/disassemble/kernelbase/CreateThread.asm`.

**(a) `NtCreateThreadEx` 를 부르는 곳: `+0x2e2` (call) / ret `+0x2e9`.** `CALL qword ptr [0x18029b990]` (IAT). 인자 (`_calls.tsv` + asm `+0x2b5 ~ +0x2da`):

| NtCreateThreadEx 인자 | 값 | 줄 |
|---|---|---|
| 1 ThreadHandle | `&[RSP+0x100]` | `+0x2da` |
| 2 DesiredAccess | `0x1fffff` (THREAD_ALL_ACCESS) | `+0x2d5` |
| 3 ObjectAttributes | `R15` — `lpThreadAttributes == NULL` 이면 0 (`+0x136 MOV R15D,EBX`), 아니면 `&[RSP+0x148]` (`+0x535` 갈래가 만든 OBJECT_ATTRIBUTES, 크기 `0x30`) | `+0x2d2`, `+0x130`, `+0x535~+0x596` |
| 4 ProcessHandle | `R12` (= hProcess, `CreateThread` 면 -1) | `+0x2cf` |
| 5 StartRoutine | `[RSP+0x140]` | `+0x2c2`, `+0x2ca` |
| 6 Argument | `[RSP+0x180]` | `+0x2b5`, `+0x2bd` |
| 7 CreateFlags | `ECX` (아래 (b)) | `+0x2b1` |
| 8 ZeroBits | `RBX` = **0** | `+0x2ac` |
| 9 StackSize | `RDX` (아래 (c)) | `+0x2a7` |
| 10 MaximumStackSize | `RAX` (아래 (c)) | `+0x2a2` |
| 11 AttributeList | `&[RSP+0x210]` | `+0x295`, `+0x29d` |

**(b) CreateFlags: `dwCreationFlags` 에서 바꾸는 줄은 하나뿐 — `CREATE_SUSPENDED (0x4) → 0x1`.**
- 거르기: `+0x120 TEST R14D,0xfffefffb` / `+0x127 JNZ` → 목적지 `+0x7d8` `MOV ECX,0xc000000d` (`+0x7dd` 에서 `FUN_1801a5026` 조각으로). 받는 비트는 `0x4` 와 `0x10000` 둘뿐.
- 바꾸기: `+0x25b TEST R14B,0x4` / `+0x25f JNZ` → 목적지 `+0x274 MOV ECX,1` (`+0x279` 에서 `[RSP+0x10c]` 에 저장, `+0x2b1` 에서 인자로). 안 켜졌으면 `+0x261 MOV ECX,EBX` (0).
- **0 이 늘 0 인 건 아니다.** 아래 셋 중 하나라도 참이면 `dwCreationFlags` 와 상관없이 `ECX = 1` (`+0x26c MOV byte [RSP+0xf0],1` 다음 `+0x274 MOV ECX,1`) — 일부러 멈춘 채 만들어서 kernelbase 가 새 스레드 TEB 를 손보고 직접 깨운다:
  1. `KernelBaseGlobalData+0x4` (`[0x1803aeec4]`, kernelbase rva `0x3aeec4`) ≠ 0 (`+0x22e CMP byte [0x1803aeec4],0` / `+0x235 JNZ` → `+0x26c`). 이 칸은 `_KernelBaseBaseDllInitialize +0x337 SETNZ` 한 곳만 쓰고 (`refs`), 그 값은 바로 앞 `+0x2d2 CALL CsrClientConnectToServer` 의 5번째 인자 `&[RSP+0x30]` (바이트 하나) 에서 온다. 이 인자는 "서버끼리 호출" 표시 `[추측: ServerToServerCall]` 이라 csrss 같은 서버 프로세스가 아니면 0 일 것 `[추측]`.
  2. `TEB+0x1720` (`GS:[0x1720]`, PDB 이름 `SubProcessTag`) ≠ 0 (`+0x237 MOV RAX,GS:[0x1720]` / `+0x243 JNZ` → `+0x26c`). 서비스 호스트가 붙이는 칸 `[추측]`.
  3. 현재 활성화 문맥 핸들 (`RtlQueryInformationActivationContext` 결과 `[RSP+0x118]`) ≠ 0 이고 그 `dwFlags` (`[RSP+0x120]`) 비트0 (NO_INHERIT 쯤 `[추측]`) 이 꺼져 있을 때 (`+0x245 CMP [RSP+0x118],RAX` / `+0x24d JNZ` → `+0x4aa TEST byte [RSP+0x120],1` / `+0x4b2 JNZ` → `+0x253`, 안 걸리면 `+0x4b8 JMP` → `+0x26c`).
- 셋 다 거짓이고 `dwCreationFlags = 0` 이면 `ECX = 0` → **CreateFlags = 0.** 위 1·2·3 의 실제 값은 정적으로 못 정함 → 동적.
- `0x10000` 비트는 CreateFlags 에 안 들어간다 (스택 크기 쪽으로, (c)).

**(c) 스택 인자 (9·10번째): `dwStackSize = 0` 이면 둘 다 0 — 커널에 (0, 0) 이 간다: 예.**
- `+0x1c5 MOV ESI,R14D` / `+0x1c8 AND ESI,0x10000` → `ESI` = RESERVATION 비트.
- `+0x280 MOV RAX,RBX` (0) / `+0x283 TEST ESI,ESI` / `+0x285 MOV RDX,[RSP+0x178]` (dwStackSize) / **`+0x28d CMOVNZ RAX,RDX`** / **`+0x291 CMOVNZ RDX,RBX`**.

| `dwCreationFlags` 의 `0x10000` | 9번째 StackSize (`RDX`, `+0x2a7`) | 10번째 MaximumStackSize (`RAX`, `+0x2a2`) |
|---|---|---|
| 없음 | `dwStackSize` | **0** |
| 있음 (RESERVATION) | **0** | `dwStackSize` |

- `dwStackSize = 0` 이면 두 칸이 어느 쪽이든 **(0, 0)**. 8번째 ZeroBits 도 `+0x2ac` 에서 늘 0. 커널 쪽 `NtCreateThreadEx +0x15f/+0x16a` 는 0 검사·치환 없이 StackInfo 로 복사 (sast/69), 그다음 `RtlCreateUserStack` 이 0 인 쪽마다 exe 헤더 값으로 바꾼다 (sast/70). 따라서 test.exe 는 sast/70 의 "(0, 0) 이면" 줄이 그대로 해당.
- `dwStackSize ≠ 0` 이고 RESERVATION 없음 이면 (커밋 = 크기, reserve = 0) 으로 간다 → 커널이 reserve 를 헤더 값으로 채운 뒤 "커밋 ≥ reserve 면 1MB 올림" 규칙 (sast/70 3번 ②). 이 길은 test.exe 에 해당 없음.

**(d) AttributeList: NULL 이 아니다. 항목 2개 (CLIENT_ID, TEB_ADDRESS), `TotalLength = 0x48`** (`lpAttributeList == NULL` 일 때, test.exe 가 그렇다).
- `+0x140 [RSP+0x218] = 0x10003`, `+0x14c [RSP+0x220] = 0x10` (Size), `+0x160~+0x168 [RSP+0x228] = &[RSP+0x188]` (Value, 16바이트 ClientId 칸), `+0x158 [RSP+0x230] = 0` (ReturnLength 없음). 둘째: `+0x170 [RSP+0x238] = 0x10004`, `+0x17c [RSP+0x240] = 8`, `+0x190 [RSP+0x248] = &[RSP+0x128]` (Value, 새 스레드 TEB 주소를 받는 칸), `+0x188 [RSP+0x250] = 0`.
- `+0x1a0 MOV EAX,2` / `+0x1b5 SHL RAX,5` / `+0x1b9 ADD RAX,8` / `+0x1bd [RSP+0x210] = RAX` → `TotalLength = 2×0x20 + 8 = 0x48`. 목록 시작 = `&[RSP+0x210]` (`+0x295`).
- 커널 검사와 맞다 (sast/69 4b): `TotalLength ≥ 0x28`, `(0x48−8) & 0x1f == 0`, 항목 `Size` CLIENT_ID `0x10` · TEB_ADDRESS `8`, 둘 다 THREAD 비트 (`0x10000`) 있음, ReturnLength NULL (INPUT 비트 없음).
- `lpAttributeList ≠ NULL` (test.exe 는 아님) 이면 `+0x1af JNZ` → `+0x59b` 에서 `BasepConvertWin32AttributeList` (+0x662 / ret +0x667) 가 같은 칸에 항목을 더 넣고 (최대 `0x25` 칸, `+0x59b MOV [RSP+0xe0],0x25`), 돌아와 `+0x67d JMP +0x1b5` 로 `TotalLength` 를 다시 센다. 본문은 안 읽음 (이름만).
- 반환 쪽 쓰임: 부른 뒤 `lpThreadId ≠ NULL` 이면 `ClientId.UniqueThread` 하위 4바이트 (`[RSP+0x190]`) 를 복사 (`+0x30e`, `+0x315`). test.exe 는 `lpThreadId = NULL` 이라 이 줄을 안 탄다 (`+0x309 TEST R13,R13` / `+0x30c JZ`).

**(e) 부른 뒤 하는 일 (이름과 오프셋만).** 정상 끝길 (`+0x2f7 TEST EAX,EAX` / `+0x2f9 JS` 로 실패는 따로):

| 갈래 | 줄 | 부르는 것 |
|---|---|---|
| **test.exe 가 탈 길** (아래 조건이 다 거짓) | `+0x2ff CMP [RSP+0xf0],0` / `+0x307 JNZ`(안 탐) → `+0x309 TEST R13,R13` → `+0x319 CMP [RSP+0xf0],0` / `+0x321 JZ` → `+0x4d4` | 활성화 문맥 핸들이 있으면 `RtlReleaseActivationContext` (`+0x4e1` / ret `+0x4e8`), 원격 복제 핸들이 있으면 `NtClose` (`+0x7e2`), 반환 = 스레드 핸들 (`+0x506 MOV RBX,[RSP+0x100]`). **`NtResumeThread` 없음, CSR 알림 없음, 활성화 안 함.** |
| "멈춘 채 만들었다" (`[RSP+0xf0] ≠ 0`, 위 (b) 의 1·2·3) | `+0x307 JNZ` → `+0x36d` | `TEB+0x1720` 값을 새 TEB 에 복사 (`+0x37b~+0x38c`). 활성화 문맥이 있고 NO_INHERIT 아니면 `RtlAllocateActivationContextStack` (`+0x41b` / ret `+0x422`) → 새 TEB `+0x2c8` 에 저장 (`+0x448`) → `RtlActivateActivationContextEx` (`+0x46c` / ret `+0x473`). 그리고 `dwCreationFlags` 에 CREATE_SUSPENDED 가 **없으면** `NtResumeThread` (`+0x341` / ret `+0x348`, 가드 `+0x319~+0x327`) |
| `KernelBaseGlobalData+4 ≠ 0` (서버 프로세스) | `+0x3a5 CMP byte [0x1803aeec4],0` | `GetModuleHandleA` (`+0x3b9` / ret `+0x3be`, 문자열 `"csrsrv"`) → `GetProcAddress` (`+0x3c8` / ret `+0x3cd`, `"CsrCreateRemoteThread"`) → 간접 호출 (`+0x3e6` / ret `+0x3eb`, CFG). 문자열은 PE `.rdata` `0x2e47c0`·`0x2e47a8` 에서 읽음 |
| 실패 | `+0x790` 이하 | `RtlFreeActivationContextStack` (`+0x80e`), `NtTerminateThread` (`+0x81e`), `NtClose` (`+0x832`), `BaseSetLastNTError` (`+0x7b6`) |
| 시작 앞 (참고) | `+0x211` / ret `+0x218` | `RtlQueryInformationActivationContext` (현재 프로세스일 때만: `+0x1d9 CMP R12,-1`) |

- 원격 프로세스 (`hProcess ≠ -1`) 길은 `+0x682` (`NtDuplicateObject` `+0x6a7`, `NtQueryInformationProcess` `+0x6d6`·`+0x72c`). test.exe 는 해당 없음.

**한 줄 답 — test.exe `CreateThread` → 커널 `NtCreateThreadEx` 인자:**

| 인자 | 값 | 닫힘 |
|---|---|---|
| CreateFlags (7) | **0** (위 (b) 의 세 조건이 다 거짓일 때). 하나라도 참이면 **1** (그때 kernelbase 가 `NtResumeThread` 를 직접 부름) | 조건 값만 동적 |
| ZeroBits (8) | 0 | 닫힘 |
| StackSize (9) | **0** | 닫힘 |
| MaximumStackSize (10) | **0** | 닫힘 |
| AttributeList (11) | **NULL 아님**, `TotalLength 0x48`, 항목 2개 (`0x10003` CLIENT_ID Size `0x10`, `0x10004` TEB_ADDRESS Size `8`) | 닫힘 |
| (참고) ProcessHandle (4) / DesiredAccess (2) / ObjectAttributes (3) | -1 / `0x1fffff` / NULL | 닫힘 |

→ **Q42 동적 몫 "CreateThread 가 커널에 정말 (0, 0) 을 넘기나" 는 정적으로 닫힌다 (예).** 남는 것은 CreateFlags 가 0 이냐 1 이냐 (조건 셋의 실제 값) 뿐이고, 이는 동적 몫으로 넘긴다 (아래).

### 2. ② `NtSetInformationProcess` 클래스 `0x29`

**PDB 이름: PDB 에 없다.** ntoskrnl·ntdll·kernelbase·kernel32 PDB 를 모두 뒤졌는데 `PROCESSINFOCLASS` 열거형도 `ProcessThreadStackAllocation` 글자도 없다 (`llvm-pdbutil dump -types` 에서 `ENUM` 4377개 중 해당 없음). 공개 헤더 이름 `ProcessThreadStackAllocation` (= 41 = `0x29`) 은 sast/70 의 `[추측: 이름]` 그대로 (정보 칸 모양이 맞는다 — 아래).

**클래스 `0x29` 갈래 찾기.**
- 점프 표: `+0x25b LEA EAX,[RDX-1]` / `+0x25e CMP EAX,0x70` / `+0x270 MOVZX EAX,byte [R8+RAX+0x935914]` / `+0x279 MOV EDX,[R8+RAX*4+0x935800]` / `+0x284 JMP RDX`. 표를 PE 에서 풀면 **클래스 `0x29` → `+0x485`** (바이트 표 칸 `0x28` = 27번째 → 4바이트 표 → rva `0x932e35`). 클래스 `0x29` 는 다른 클래스와 같은 목적지를 안 쓴다 (`+0x485` 로 오는 것은 `0x29` 뿐).
- 사용자 모드 호출이면 그 앞에 `+0x2f6` 이하 사다리 (`+0x359 CMP EBX,0x29` / `+0x35c JZ` → `+0x20e8 MOV ECX,8` → `+0x429` 검사): 정보 칸이 **8바이트 정렬** 이어야 하고 (`+0x42e~+0x434`, 아니면 `ExRaiseDatatypeMisalignment` `+0x461`), `칸 + 길이` 가 `0x7fffffff0000` 이하이고 넘치지 않아야 한다 (`+0x436~+0x44c`, 아니면 `+0x466` 에서 일부러 접근해 예외).

**갈래 본문 (`+0x485 ~ +0x528`, 줄은 `.asm` 249~286행):**

| 줄 | 하는 일 | 어기면 |
|---|---|---|
| `+0x485~+0x497` | 지역 `[RSP+0x1a8..0x1d0)` 40바이트를 0 으로 | |
| `+0x49f CMP R10,-1` / `+0x4a3 JNZ` → `+0xd05` | `ProcessHandle` 은 **-1 (현재 프로세스)** 만 | `0xC000000D` |
| `+0x4ac CMP R13D,0x28` / `+0x4b0 JNZ` → `+0xe0b` | 길이 **`0x28`**. 아니고 `0x18` 이면 구형 꼴 (아래), 그 밖은 | `0xC0000004` (`+0xe0f JNZ` → `+0xcc9`) |
| `+0x4b6 TEST DIL,DIL` / `+0x4b9 JNZ` → `+0x116d` | 사용자 모드 (`DIL` = 이전 모드) 이면 정보 칸 40바이트를 **지역으로 복사** (`+0x116d~+0x1195`) 하고, 나중에 쓸 사용자 칸 주소 `칸+0x20` 을 `R14` 에 (`+0x119c LEA R14,[R15+0x20]`), `R15` 를 지역으로 (`+0x11a0`) 바꿔 `+0x4bf` 로 | |
| `+0x4bf~+0x4c6` | `R9D = [칸+0]` **노드**, `> 0x40` 이면 | `0xC000000D` |
| `+0x4cc~+0x4d8` | `[칸+4]`·`[칸+8]`·`[칸+0xc]` (예약 12바이트) 는 **전부 0** | `0xC000000D` |
| `+0x4de ADD R15,0x10` | 이제 `R15` = 칸 + `0x10` | |
| `+0x4e2~+0x4e8` | `[칸+0x10]` **예약 크기** ≠ 0 | `0xC000000D` |
| `+0x4ee`, `+0x4f6`, `+0x4fa` | 크기를 지역 `[RSP+0x200]` 에 복사, `[칸+0x20]` (출력) 을 0 으로, 5번째 인자 `[RSP+0x20] = 0` | |
| `+0x4fe~+0x50a` | `R8 = &[RSP+0x200]` (크기), `RDX = [칸+0x18]` (**ZeroBits**), `RCX = 칸+0x20` (**출력 주소 칸**) | |
| **`+0x50e CALL`** / ret **`+0x513`** | **`MmAllocateUserStack`** (`R9D` = 노드) | 실패면 `+0x517 JS` → 그대로 반환 |
| `+0x51d TEST DIL,DIL` / `+0x520 JNZ` → `+0xff1` | 사용자 모드면 `[칸+0x20]` (출력) 를 **사용자 칸에 복사** (`+0xff1 MOV RAX,[R15+0x10]` / `+0xff5 MOV [R14],RAX`). 커널 모드면 `MmAllocateUserStack` 이 칸에 직접 씀 | |

- **정보 칸 해석**: `+0` 노드 (4바이트), `+4~+0xf` 예약 0, **`+0x10` 예약 크기**, **`+0x18` ZeroBits**, **`+0x20` 출력 = 예약된 시작 주소**. 요청서가 적은 "`+8` 크기" 는 아니다 — sast/70 의 `+0x10`/`+0x18`/`+0x20` 과 같고, 이 코드가 확인해 준다. 공개 헤더의 `{PreferredNode, Reserved[3], ReserveSize, ZeroBits, StackBase}` 꼴과 모든 칸이 맞는다 `[추측: 이름]`.
- **길이 `0x18` 구형 꼴** (`+0xe0b CMP R13D,0x18` / `+0xe0f JNZ` → `+0xcc9`): `{+0 크기, +8 ZeroBits, +0x10 출력}`, 노드 0 (`+0xe15 MOV R9D,ESI`). 커널 모드면 `+0xe1b JZ` → `+0x4e2` 로, 사용자 모드면 조각 `FUN_140935286` (`+0x28d6~+0x2900`) 이 16바이트를 지역에 복사하고 `R14 = 칸+0x10` 으로 둔 뒤 `+0x4e2` 로 간다. 출력 칸 위치만 다르고 이후는 같다.
- **권한 검사는 이 갈래에 없다** (SeDebug·SeTcb 같은 줄 없음). 호출자는 자기 프로세스 (`-1`) 만 건드린다.
- 이 갈래는 **유저가 직접 부를 수도, 커널이 `ZwSetInformationProcess` 로 부를 수도** 있다. 커널 `RtlCreateUserStack +0x104` 는 후자 (이전 모드 = 커널이라 `DIL = 0`: 복사·검사 건너뜀), ntdll `RtlCreateUserStack +0x16f` 는 `NtSetInformationProcess` 를 syscall 로 (전자, 아래 5번).

**부르는 함수: `MmAllocateUserStack`** (rva `0x904d94`, 536B). 인자: `RCX` = 출력 주소 칸 포인터 (`R15`), `RDX` = ZeroBits (`R12`), `R8` = 크기 포인터 (`R14`), `R9D` = 노드 (`EBX`), 5번째 `[RBP+0xa0]` = 파티션 핸들 (이 길은 0 — `+0x4fa` 가 넣음) `[추측: 이름]`.

**그 함수가 정말 예약 (`MEM_RESERVE`) 을 하나: 예. 커밋은 안 한다.**
- `MmAllocateUserStack +0xcb MOV dword [RSP+0x20],0x2000` — `MiAllocateVirtualMemoryPrepare` 의 5번째 인자 (할당 종류) 가 **`0x2000` = MEM_RESERVE** 하나뿐 (`0x1000` MEM_COMMIT 비트 없음). 같이 넘기는 값: `RCX = -1` (현재 프로세스, `+0x9a OR RCX,-1`), `RDX = [R15]` (원하는 시작 주소 = 0), `R8 = R12` (ZeroBits), `R9 = [R14]` (크기), `[RSP+0x28] = ESI` (보호값 `4` = PAGE_READWRITE — 파티션 핸들이 0 이면. `+0x48~+0x57` 에서 `NEG`/`SBB` 로 정함 `[추측: 이름]`), `[RSP+0x30] = &[RBP+0x10]` (0x48 바이트 칸, `+0x4a` 에서 `[RBP+0x30] = EBX` 로 노드를 칸 `+0x20` 에 넣음), `[RSP+0x40] = EBX` (플래그 `0x80000000`, 파티션이 있으면 `0xC0000000`), 출력 칸 둘.
- `MiAllocateVirtualMemoryPrepare` (`+0xd8` / ret `+0xdd`) 가 이 값들을 0x80 바이트 칸 (`[RBP-0x70]`) 으로 정리하고, `MiAllocateVirtualMemory` (`+0x12f` / ret `+0x134`) 가 실행한다. 그 안: `MiAllocateVirtualMemory +0x125 MOV EAX,[RDI+0x28]` (= Prepare 가 `+0x5e6` 에 쓴 할당 종류) / `+0x11c MOV R13D,0x2000` / `+0x12c TEST R13D,EAX` / **`+0x12f JNZ` → `+0x31e`** → **`+0x334 CALL MiReserveUserMemory`** (ret `+0x339`). 즉 `MEM_RESERVE` 비트가 켜지면 VAD 만 예약하는 `MiReserveUserMemory` 로 가고, 커밋 쪽 (`MiCommitExistingVad` 등) 으로는 안 간다. `MiReserveUserMemory` 본문은 안 읽었다 (이름만).
- 성공하면 `MmAllocateUserStack +0x157 MOV RAX,[RSP+0x78]` / `+0x15c MOV [R15],RAX` (시작 주소 → 출력 칸), `+0x15f MOV RAX,[RBP-0x58]` / `+0x163 MOV [R14],RAX` (실제 크기 → 크기 칸).

**정렬 (`0x10000`?): 시작 주소는 64KB 정렬, 크기는 4KB 쪽 단위로만 올림.**
- `MiAllocateVirtualMemoryPrepare +0x10d MOV R8D,0x10000` / `+0x118 MOV [RSP+0xa8],R8` — 기본 정렬값. 시작 주소 0 (바꿀 주소 없음)·`MEM_RESERVE` 길에서 `+0x1ac MOV R10D,0x1000` 갈래가 아니라 `+0x245~+0x24f` (`MOV R10,[RSP+0xa8]`) 를 거쳐 **`+0x1bf MOV [R13+0x10],R10`** 으로 칸 (`MmAllocateUserStack` 의 0x48 바이트 칸) `+0x10` 에 `0x10000` 이 들어간다. 이어 `+0x4d9~+0x4e7` 이 `[R13+0x10]-1` 과 `[R13]` (요구 시작 주소) 를 `TEST` 해 맞는지 본다. 이 값은 `+0x57b~+0x585` 에서 0x80 바이트 칸 `+0x10` 으로 가서 `MiAllocateVirtualMemory` 로 간다 (실제 시작 주소 고르는 곳 `MiReserveUserMemory` 는 안 읽음).
- 크기는 `+0x4ba ADD R8,0xfff` / `+0x4c1 AND R8,-0x1000` 로 **4KB 올림** (`+0x490 NEG R10` / `+0x495 AND R15,R10` 으로 시작 주소는 64KB 내림). 이 함수에서 크기를 64KB 로 올리는 줄은 없다 — 그건 이미 `RtlCreateUserStack` 이 `M` 을 64KB 로 올려서 온다 (sast/70 3번 ④).
- **ZeroBits**: 0 이면 위 상한 = `min(EPROCESS.HighestUserAddress − 1, 0x7ffffffeffff)` (`+0x517 MOV RCX,[R14+0x368]` — PDB: `_EPROCESS +0x368 HighestUserAddress` — / `+0x51e DEC RCX` / `+0x521 CMP RCX,R11` / `+0x524 CMOVNC RCX,R11` / `+0x528 [R13+8] = RCX`). 0 이 아니면 `MiValidateZeroBits` (`+0x374` / ret `+0x379`) 로 검사한 뒤 상한 = `(−1 >> ZeroBits)` (`+0x693~+0x6a4`, `+0x3a0`~`+0x3bd`) — 보통의 "윗 비트 몇 개는 0" 뜻.
- **노드**: `+0x560 MOV R11D,[R13+0x20]` / `+0x564 CMP R11D,EAX` (`EAX = KeNumberNodes`) / `+0x567 JA` → `+0x1cc` `0xC000000D`. 노드가 `KeNumberNodes` 보다 크면 거절. 이 노드가 시작 주소 고르기에 어떻게 쓰이는지는 `MiReserveUserMemory` 안이라 안 읽음.
- 지금 우리 (PE_emulator): 클래스 `0x29` = "현재 프로세스의 유저 공간에 `[A, A+M)` 을 `MEM_RESERVE` 로 잡고 `A` 를 칸 `+0x20` 에 돌려준다. `A` 는 64KB 정렬, 상한 `0x7ffffffeffff` 안, ZeroBits 지키기" 로 구현하면 된다 (커밋·가드는 `RtlCreateUserStack` 이 `NtAllocateVirtualMemory` 로 따로, sast/70).

### 3. ③ `PspBuildCreateProcessContext +0x1876 ~ +0x1882` 의 `ctx+0x10` 비트 0 을 누가 읽나

**답: 요청서가 정한 네 함수 안에는 읽는 줄이 없다. "훑기 한계" 로 못 찾음.**

- 쓰는 줄 (다시 확인): `+0x186f CMP [RSP+0x90],ESI` / `+0x1876 SETNZ DL` (셋째 인자 ≠ 0 = 스레드 모드) / `+0x1879 MOV AL,[R12+0x10]` / `+0x187e AND AL,0xfe` / `+0x1880 OR DL,AL` / `+0x1882 MOV [R12+0x10],DL`. `R12` = `ctx` (넷째 인자, `+0x45 MOV R12,R9`). 이 함수 안에서 `[R12+0x10]` 는 이 두 줄뿐이다.
- 칸 `+0x10` 은 **바이트 하나에 여러 비트** 가 있다 (아래). 읽는 쪽을 함수마다 보면:

| 함수 | `ctx` 를 어디서 읽나 | `ctx+0x10` 읽는 줄 |
|---|---|---|
| `NtCreateThreadEx` | `ctx = [RBP+0x48]` (`+0xb4`, `+0xd1`, `+0x2b1`, `+0x2d4` 에서 주소만 넘김. 직접 읽는 곳은 `[RBP+0x60]` = `ctx+0x18` 한 곳, `+0x2a8`) | **없음** |
| `PspCreateThread` | 6번째 인자 `[RBP+0x1d8]` → `[RBP-0x80]` (`+0x2b`, `+0x43`). 쓰임: `+0x15f CMP [RBP-0x80],0` (NULL 검사), `+0x277 MOV R12,[RBP-0x80]` → `PspAllocateThread` 4번째 (`+0x27b MOV R9,R12`), `PspInsertThread` 7번째 (`+0x2d3 [RSP+0x30]=R12`). 값을 읽지 않고 넘기기만 | **없음** |
| `PspAllocateThread` | 4번째 `R9` → `RBX` (`+0x29`), `+0x1e2 MOV EBX,EDI` 앞까지가 `ctx` 몫. 읽는 칸: `+8` (`+0x170`, 조각 `+0x894`·`+0xaa3`), `+0x14` (`+0xa03`, `+0xaa0`), `+0x140`·`+0x148` (`+0x9f7`, `+0x89f`, `+0x8b5`), `+0x1e0` (`+0x2ca839`). `[RBX+0x10]` 은 파일 전체에서 한 줄도 없음 | **없음** |
| `PspInsertThread` | 7번째 `[RSP+0x100]` → `[RSP+0x80]` (`+0x2e`, `+0x36`), `+0xc6 MOV RBX,[RSP+0x100]`. 읽는 칸: `+0x20` (`+0x1cb`), `+8` (`+0x1d9`, `+0x1e7`), `+0x14` (`+0x1f0 LEA`), `+0x170` (`+0x54b`). 그리고 `+0x7f7 MOV RDX,[RSP+0x100]` → **`PspCallProcessNotifyRoutines`(ctx 를 인자로 넘김)** (프로세스의 첫 스레드일 때만: `+0x4b4 TEST DIL,0x2` / `+0x4b8 JNZ` → `+0x7bb`) | **없음** (아래 칸 `PspCallProcessNotifyRoutines` 참고) |

- 요청 밖이지만 이미 있던 `.asm` 로 같이 훑어 본 곳:
  - `PspDeleteCreateProcessContext` (`NtCreateThreadEx +0x2d8`, `PspBuildCreateProcessContext +0x188e` 에서 부름): **`+0x7d TEST byte [RBX+0x10],0x4`** / `+0x81 JNZ` → `+0x183` — 읽는 건 **비트 2** 지 비트 0 이 아니다.
  - `NtCreateUserProcess` (프로세스 길, `ctx = [RBP+0x1a0]` → `+0x10` = `[RBP+0x1b0]`): `+0xaa1 OR byte [..],0x8`, `+0xad1 AND byte [..],0xfb`, `+0xb47 MOV AL,[..]` / `+0xb4d TEST AL,0x10` / `+0xb55 AND AL,0xfb` / `+0xb57` 쓰기 — **비트 3 켜기, 비트 4 읽기, 비트 2 끄기**. 비트 0 은 안 만진다.
  - `PspCallProcessNotifyRoutines` (`ctx` 를 `RBX = RDX` 로 받음, `+0x21`): `[RBX+0x40]`·`+0xb8`·`+8`·`+0xf0`·`+0xd8`·`+0x1a0` 만 읽고 `[RBX+0x10]` 는 없다.
- 요약: 칸 `ctx+0x10` 의 비트 0 은 `PspBuildCreateProcessContext` 가 "스레드 모드" 로 켜 두기만 하고, **내가 훑은 어디에서도 읽지 않는다.** 훑기 한계: `ctx` 를 받아 가는 `KeInitThread`·`KeStartThread`·`MmCreateTeb` 등 callee 안은 안 읽었다 (넘기는 값이 `ctx` 전체가 아니라 `+0x140`·`+0x1e0` 같은 부분 포인터라서 이 비트는 안 보일 것 `[추측]`).

### 4. ④ `PspAllocateThread +0x8c7` 노드 갈래

**답: "노드 ≠ 0 일 때" 갈래가 아니다. 이상적 프로세서 속성 (`0x3000e`) 이 있을 때 타는 곳이고, 노드 번호에 `+1` 을 해서 스택 노드 선호값으로 쓴다.**

- 들어가는 길 (오프셋 순, 이 근처는 `.asm` 에서 "주인 없는 덩어리" 로 떨어져 있어 실행 순서로 적음):
  1. `+0x165 TEST RBX,RBX` / `+0x168 JZ` → `+0x1cd` (`ctx == NULL` 이면 통째로 건너뜀. `NtCreateThreadEx` 는 늘 `&ctx` 를 주므로 NULL 아님).
  2. `+0x170 MOV R8,[RBX+8]` (`ctx+8` = 받은 속성 번호 비트). `+0x174 BT R8,0xc` (그룹 친화도 `0x3000c`) → 켜졌으면 `+0x179 JC` → `+0x9f7 LEA RDX,[RBX+0x140]` (RDX = 친화도 칸 포인터). `+0x17f MOV ESI,0x4000` / `+0x18b TEST RSI,R8` (이상적 프로세서 `0x3000e`) → 켜졌으면 `+0x18e JNZ` → `+0xa03 MOV EAX,[RBX+0x14]` (프로세서 번호, sast/69 의 `ctx+0x14`) / **`+0xa06 MOV RCX,[R10+RAX*8+0xfc8c80]`** — 이 표는 PDB 이름 **`KiProcessorBlock`** (`+0x0`, PDB 로 확인: 프로세서 번호 → `PRCB` 포인터). 그래서 `RCX` = 그 프로세서의 `PRCB`.
  3. `+0x1b6 TEST RCX,RCX` / **`+0x1b9 JNZ` → `+0x886`** 이 노드 갈래 입구. 즉 **이상적 프로세서 속성이 있을 때만** 들어간다.
- 갈래 안 (`+0x886 ~ +0x8cb`):
  - **친화도 속성은 없고 (`RDX = 0`)** 이상적 프로세서만 있으면 (`+0x886 TEST RDX,RDX` / `+0x889 JNZ` 안 탐): `+0x88f BTS R8,0xc` / `+0x894 MOV [RBX+8],R8` (속성 번호 비트에 친화도 비트를 **지어 넣음**), `+0x898~+0x89f` 그 `PRCB.Group` (`+0xd0`, PDB `Group`) 을 `[RBX+0x148]` (친화도의 그룹) 에, `+0x8a6~+0x8b5` 그 그룹의 활성 프로세서 마스크 (`[R10+RAX*8+0xfc7168]` = PDB `KeActiveProcessors+8` + 그룹×8 = `_KAFFINITY_EX.Bitmap[그룹]`) 를 `[RBX+0x140]` (친화도의 마스크) 에 쓴다. 즉 "이상적 프로세서가 있는 그룹 전체" 로 친화도를 채운다.
  - **친화도 속성도 같이 있으면** (`+0x889 JNZ` → `+0xa5d`): `+0xa5d MOVZX EAX,byte [RCX+0xd0]` / `+0xa64 CMP AX,[RDX+8]` (프로세서의 그룹 ≠ 친화도의 그룹이면 `+0xa68 JNZ` → 조각 `FUN_140b0d1c1` 에서 `0xC0000030`), `+0xa6e MOV R8,[RDX]` (마스크) — 0 이면 통과 (`+0xa74 JZ`), 아니면 `+0xa7a TEST [RCX+0xc8],R8` (PDB `GroupSetMember`) 이 겹쳐야 통과 (`+0xa81 JNZ`), 아니면 같은 `0xC0000030`. 통과하면 `+0x8bc` 로 합류.
  - 합류점 (**여기가 요청서의 노드 갈래**): `+0x8bc CALL KeGetProcessorNodeNumber` / ret `+0x8c1` (인자는 `RCX` = 앞에서 읽은 `PRCB`, 이 사이에 `RCX` 를 바꾸는 줄 없음 `[추측: 인자 모양]`) → `+0x8c1 MOVZX EDX,AX` / `+0x8c4 ADD EDX,R14D` (**`R14D = 1`**: `+0x13b LEA R14D,[RDI+1]`, `+0x245 MOV R14D,EDI` 는 이 갈래 **뒤** 에서 실행) → **`+0x8c7 MOV [RSP+0x5c],EDX`** = **노드 + 1** → `+0x8cb JMP` → `+0x1c8`.
- **그 값이 가는 곳**: `+0x11d MOV [RSP+0x5c],EDI` (0 으로 시작) → `+0x5c6 MOV EBX,[RSP+0x5c]` / `+0x5d2 MOV [RSP+0x20],EBX` (`PspSetupUserStack` 5번째 인자) → `PspSetupUserStack` 이 0 이 아니면 `R9 = 0x1000 | (값 << 56)` (sast/70, `PspSetupUserStack +0x62 SHL RAX,0x38`, `+0x66 OR RDI,RAX`) → `RtlCreateUserStack` `R14 = R9 >> 0x38` → 정보 칸 `+0` 노드 (`+0xd4`) → `MmAllocateUserStack` `R9D` → 0x48 칸 `+0x20` → `MiAllocateVirtualMemoryPrepare +0x560~+0x567` 에서 `KeNumberNodes` 이하만 받음. **값은 `노드+1` 이라 1부터 시작하고, 0 은 "선호 없음".**
- test.exe: `AttributeList` 의 두 항목 (`0x10003`, `0x10004`) 에는 이상적 프로세서 비트가 없다 → `ctx+8` 에 비트 14 없음 → `RCX = 0` → 이 갈래 안 탐 → `[RSP+0x5c] = 0` → `PspSetupUserStack` 의 `R9 = 0x1000` (노드 없음). sast/70 의 "보통 0 `[추측]`" 이 정적으로 닫힌다 (`AttributeList` 가 위 1번 (d) 와 같다면).
- 근거: `ms/disassemble/ntoskrnl/PspAllocateThread.asm` (`+0x165~+0x1c8`, 조각 `+0x886~+0x8cb`, `+0x9f7~+0xa0e`, `+0xa5d~+0xa87`, `+0x5c6~+0x5d2`).

### 5. ⑤ ntdll `RtlCreateUserStack` (rva `0xf5130`, 709B)

**같은 꼴인가: 예, 거의 한 몸이다.** 크기 0 → exe 헤더 (0 인 쪽마다 `CMOVZ`), 다듬기 ①~⑤, `0x28` 바이트 정보 칸으로 `NtSetInformationProcess(-1, 0x29, …, 0x28)` 예약, 커밋 `4`, 가드 `0x104` 3쪽, InitialTeb 채우기, 실패 시 `MEM_RELEASE` — 모두 커널 쪽과 같은 순서·같은 상수다. 다른 점만 표로.

**ntdll 인자 (`RSP = 진입 − 0xb8`):** `RCX` 커밋 (`+0xb` 보관 → `RDI`), `RDX` reserve (`+0x7` → `RBX`), `R8` ZeroBits (`+0x3` 보관 → `[RSP+0xd0]`, `+0xd5 MOV R8,[RSP+0xd0]`), `R9` = `P | (노드 << 56)` (`+0x21 MOV RSI,R9`, `+0x2f~+0x40`), **5번째 `[RSP+0xe0]` → `R14` (`+0x56`)**, 6번째 `[RSP+0xe8]` = InitialTeb (`+0x17f MOV RSI,[RSP+0xe8]`).

| 항목 | 커널 `RtlCreateUserStack` (sast/70) | ntdll `RtlCreateUserStack` |
|---|---|---|
| 헤더 찾기 | 붙은 프로세스의 **`EPROCESS.SectionBaseAddress`** (`+0x1eb~+0x1fb`) → `RtlImageNtHeader` `+0x202` | **PEB `ImageBaseAddress`**: `+0x74 MOV R13,GS:[0x60]` (PEB) / `+0x9b MOV RDX,[R13+0x10]` (PDB: `_PEB +0x10 ImageBaseAddress`) → **`RtlImageNtHeaderEx(1, 기준, 0, &out)`** `+0xa3` / ret `+0xa8` |
| PE32 갈래 | 있음 (`+0x214 CMP word [RAX+0x18],0x20b`, 아니면 `+0x255`·`+0x258` 4바이트) | **없음.** `+0xb8 CMOVZ RDI,[RAX+0x68]` / `+0xc8 CMOVZ RBX,[RAX+0x60]` (8바이트 칸) 만. Magic 검사 없음 |
| 헤더 NULL | `0xC000007B` (`+0x25d`) | 같음 (`+0xdf MOV EAX,0xc000007b`) |
| 0 이면 헤더로 가는 갈림 | `+0x6e`·`+0x7b` | `+0x85 TEST RDI,RDI` / `+0x88 JNZ` → `+0x248 TEST RBX,RBX` / `+0x24b JNZ` → `+0xee` (헤더 블록을 건너뜀) / `+0x251 JMP` → `+0x8e` (헤더 블록). 둘 중 하나라도 0 이면 헤더 블록. **같은 규칙** |
| 5번째 인자 | 안 읽음 (`+0x26` 에서 0 으로 덮어 씀) | **할당 단위 `g`**: 0 이면 `0xC000000D` (`+0x5e TEST R14,R14` / `+0x61 JZ`), `g < P` 면 `0xC000000D` (`+0x67 CMP R14,RSI` / `+0x6a JC`). reserve 올림에 `g` 를 씀 (`+0x113~+0x120`) |
| reserve 올림 단위 | **상수 `0x10000`** (`+0xa7 ADD RBX,0xffff`, `+0xb5 AND RBX,RDX`) | **`g`** (`+0x113 LEA RDX,[R14-1]` / `+0x117 NOT RDX` / `+0x11d ADD RBX,R14` / `+0x120 AND RBX,RDX`) — 호출자가 정함 |
| 입력 검사 | 노드 `> 0x40` 만 (`+0x42~+0x46`) | 노드 `> 0x40` (`+0x43`/`+0x47`) + **`P == 0` 거절** (`+0x4d`/`+0x50`) + 위 `g` 두 검사 → 모두 `0xC000000D` (`+0x2a6`) |
| 커밋 0 → `0x4000`, 커밋 ≥ reserve → 1MB 올림, 페이지 올림 `C`, `PEB.MinimumStackCommit` (`+0x318`) 보정 | `+0x81~+0x89`, `+0x8d`/`+0x90`→`+0x2c0`, `+0x96~+0xa4`, `+0xb8~+0xce`, `+0x299~+0x2bb` | `+0xee~+0xf6`, `+0xfa`/`+0xfd`→`+0x2ad`, `+0x103~+0x110`, `+0x123~+0x139` (`MOV RAX,[R13+0x318]`, PDB `_PEB +0x318 MinimumStackCommit`), `+0x280~+0x2a1`, `+0x2c0~+0x2ce`. **같은 규칙** |
| 어느 PEB | 대상 프로세스 `EPROCESS.Peb` (`+0x60 MOV R13,[RCX+0x2e0]`) | **현재** 프로세스 `GS:[0x60]` |
| 프로세스에 붙기 | 부른 쪽 `PspSetupUserStack` 이 `KiStackAttachProcess` 로 붙음 | 없음 (자기 프로세스, 핸들 -1: `+0x168 OR R15,-1`) |
| 예약 호출 | `ZwSetInformationProcess` `+0x104` / ret `+0x109` | **`NtSetInformationProcess`** `+0x16f` / ret `+0x174` (syscall). 인자 `RCX=-1`, `EDX=0x29` (`+0x164 LEA EDX,[R9+1]`), `R8=&[RSP+0x58]`, `R9D=0x28` (`+0x159`) |
| 정보 칸 | `+0` 노드, `+0x10` `M`, `+0x18` ZeroBits, `+0x20` 출력 | **같음**: `[RSP+0x58]=R15D` 노드 (`+0x13f`), `[RSP+0x5c]` 8바이트 0 (`+0x144`), `[RSP+0x64]=R9D` 0 (`+0x14a`), `[RSP+0x68]=RBX` `M` (`+0x14f`), `[RSP+0x70]=R8` ZeroBits (`+0x154`), `[RSP+0x78]` 출력 (0 으로 `+0x2b`) |
| 커밋 | `ZwAllocateVirtualMemory` `+0x170` (`0x1000`, `4`) | `NtAllocateVirtualMemory` `+0x1db` / ret `+0x1e0` (`+0x1b5 [RSP+0x28]=4`, `+0x1c3 [RSP+0x20]=0x1000`) |
| 가드 (`M − C ≥ 3P`) | `+0x18b`/`+0x18e`, `+0x1c6` (`0x104`) | `+0x1f2 CMP RBX,R12` / `+0x1f5 JC`, `+0x227` / ret `+0x22c` (`+0x207 [RSP+0x28]=0x104`) |
| 실패 시 풀기 | `ZwFreeVirtualMemory` `+0x28d` (`0x8000`) | `NtFreeVirtualMemory` `+0x277` / ret `+0x27c` (`+0x264 MOV R9D,0x8000`) |
| InitialTeb | `+0x00`·`+0x08` = 0, `+0x20` = `A`, `+0x10` = `A + M`, `+0x18` = `A + M − C` | **같음** (`+0x187`·`+0x18a` 0, `+0x193` `A`, `+0x19b` `A+M`, `+0x1ee` `StackLimit`) |

- 요약: **다른 점은 (1) 헤더를 PEB `ImageBaseAddress` + `RtlImageNtHeaderEx` 로 찾고 PE32 갈래가 없음, (2) 5번째 인자 `g` 로 reserve 올림 단위를 호출자가 정함 (커널은 `0x10000` 고정), (3) 입력 검사 추가 (`P ≠ 0`, `g ≠ 0`, `g ≥ P`), (4) Nt\* (syscall) 를 부름, 프로세스에 안 붙음.** 값 계산은 같다.
- **누가 부르나 (callers 이름만).**
  - ntdll 안: **직접 부르는 코드 없음.** `refs ntdll.dll RtlCreateUserStack` = 데이터 참조 3줄 (내보내기 표 쪽 두 곳 `rva 0x1b8ef4`·`0x1df548`, `GuardCFFunctionTable +0xeec` `rva 0x17620c`) + 외부 1줄. 이 목록에 `CALL` 은 없다. 이 함수를 쓰는 ntdll 함수는 (이 빌드에서) 없다. → `RtlCreateUserThread` 도 안 부른다.
  - kernelbase: **`CreateFiberEx` 한 곳.** `refs KernelBase.dll 0x29ba60` (IAT 칸) = `CreateFiberEx +0xce` (rva `0xf0bbe`, `CALL qword ptr [0x18029ba60]`), `ret = +0xd5` (rva `0xf0bc5`; `objdump` 로 7바이트 명령 확인). 부르기 앞 `+0xbe`·`+0xc9` 에서 `[RSP+0x28]`·`[RSP+0x20]` (5·6번째 인자 칸) 를 채운다 (`objdump` 로 확인). `CreateFiberEx` 는 test.exe `sample/test.c` 에서 안 쓰는 길 (`CreateThread` 는 위 1번).
  - 그 밖: `ms/bin/.../System32/*.dll`·`*.exe` 중 `RtlCreateUserStack` 글자가 들어 있는 것은 `KernelBase.dll`·`ntdll.dll` 둘뿐이다 (`grep -l -a`). 다른 모듈은 이 로컬 빌드 기준으로 안 부른다.
- **유저 쪽에서 스택을 직접 만드는 길이 있나: 있다 — 파이버 (`kernelbase!CreateFiberEx`).** `CreateThread` → `CreateRemoteThreadEx` 길은 스택을 직접 안 만들고 `NtCreateThreadEx` 에 크기 둘만 넘겨 커널 `RtlCreateUserStack` 이 만든다 (위 1번 (c)).
- 지금 우리: 요청서에 가정 없음. 둘째 스레드 길엔 ntdll 쪽 함수가 안 쓰인다 (커널 것만). ntdll 쪽 구현이 필요한 것은 `CreateFiberEx` 를 지원할 때.

## 동적분석으로 확인해야 할 것

dast 요청 필요: **있음 — Q42·Q104 에 붙일 후보** (이번 묶음엔 VM 안 함). 테스트 대상은 test.exe 의 `CreateThread` (`sample/test.c:34`) 로 만든 둘째 스레드.

정적으로 못 닫은 것:
1. **`CreateRemoteThreadEx` 가 CreateFlags 를 0 으로 넘기나, 1 로 넘기나.** 1번 (b) 의 세 조건 값: (가) `KernelBaseGlobalData+4` (kernelbase rva `0x3aeec4`), (나) `TEB+0x1720` `SubProcessTag`, (다) 활성화 문맥 핸들 `[RSP+0x118]` 과 플래그 `[RSP+0x120]`. test.exe 는 일반 클라이언트라 (가) 0, (나) 0 일 것 `[추측]`; (다) 는 exe 매니페스트 유무와 `RtlQueryInformationActivationContext` 가 돌려주는 값에 달렸다 (정적으로 못 정함).
2. `NtCreateThreadEx` 에 실제로 가는 인자 전부 (CreateFlags·StackSize·MaximumStackSize·AttributeList 의 항목 수와 번호). 정적 답 (위 표) 과 일치하는지.
3. `MmAllocateUserStack` → `MiReserveUserMemory` 가 돌려주는 시작 주소 `A` 의 모양 (64KB 정렬인가, 크기가 `M` 인가). `MiReserveUserMemory` 본문은 안 읽었다. (sast/70 의 동적 4번·5번과 같은 자리.)
4. test.exe 의 `AttributeList` 에 이상적 프로세서 속성이 없어 `PspAllocateThread +0x886` 갈래를 안 타는지 (타면 `PspSetupUserStack +0x89` 의 `r9 >> 0x38` 이 0 이 아님).

추천 멈춤점 (call 명령 위치; 공통 조건: 현재 프로세스 = test.exe, 그리고 sast/69 처럼 StartRoutine = test 의 스레드 함수로 둘째 스레드만 거름):

| 멈춤점 | 볼 것 |
|---|---|
| `KERNELBASE!CreateRemoteThreadEx+0x211` (call) / `+0x218` (ret) | ret 에서 `dq @rsp+0x118 L2` (`[0x118]` 활성화 문맥 핸들, `[0x120]` 플래그), `eax` 상태 |
| `KERNELBASE!CreateRemoteThreadEx+0x22e` | `db kernelbase+0x3aeec4 L1` (서버 표시), `dq gs:[1720] L1` (`SubProcessTag`) |
| `KERNELBASE!CreateRemoteThreadEx+0x2e2` (call) | `ecx` = CreateFlags(7번째), `dq @rsp+0x20 L7` = 인자 5~11 (StartRoutine, Argument, CreateFlags, ZeroBits, StackSize, MaxStackSize, AttributeList), `dd poi(@rsp+0x50) L1` = TotalLength (`0x48`), `dq poi(@rsp+0x50)+8 L8` = 항목 두 개 |
| `nt!NtCreateThreadEx+0x70` (sast/69 와 같은 지점) | `r13d` CreateFlags, `rdi` AttributeList, `poi(@rbp+0x340)`·`+0x348` = 9·10번째. 위 `+0x2e2` 값과 맞는지 |
| `nt!NtSetInformationProcess+0x485` (class 0x29 갈래 입구, 걸리면 `ebx == 0x29`) | `r10` (= -1), `r13d` (= 0x28), `dq @r15 L5` (정보 칸) |
| `nt!MmAllocateUserStack+0xd8` (call) | 스택 인자 `dd @rsp+0x20 L1` = `0x2000`, `dd @rsp+0x28 L1` = 보호값 |
| `nt!MmAllocateUserStack+0x134` (ret) | `dq @r15 L1` = `A`, `dq @r14 L1` = 실제 크기 (`!address` 로 예약 모양) |
| `nt!PspAllocateThread+0x886` (노드 갈래 입구, 조각 안) | **걸리면 이상적 프로세서 속성이 있다는 뜻.** test.exe 의 둘째 스레드에선 안 걸려야 함. 걸리면 `rcx` = `PRCB`, `dq @rbx+8 L1` = 속성 번호 비트 |

## 후속 제안 (정적)

- **새로 필요한 정적 요청은 없다.** 선택: `MiReserveUserMemory` 본문 (시작 주소를 어떻게 고르나, 64KB 정렬·노드 선호) — 동적 3번이 이상하게 나오면 한 건으로.
- (선택) `KernelBaseGlobalData+4` 가 `CsrClientConnectToServer` 의 5번째 인자 (서버끼리 호출 표시) 라는 해석을 확정하려면 `CsrClientConnectToServer` 본문을 읽어야 한다. 지금은 `[추측]`. test.exe 에서 값이 0 인지는 동적 한 줄이면 닫힌다.

## 요청서와 다르게 한 것 (이유)

- **요청서 범위 밖으로 뽑은 것**:
  - ntoskrnl `MiAllocateVirtualMemoryPrepare` (rva `0x905490`, 2435B)·`MiAllocateVirtualMemory` (rva `0x905db0`, 2318B) depth 0. 이유: 질문 2 의 "정말 `MEM_RESERVE` 를 하나, 정렬" 이 `MmAllocateUserStack` 한 단계 아래에서 갈린다 (Prepare 가 정렬값을 정하고, `MiAllocateVirtualMemory` 가 `MEM_RESERVE` 로 `MiReserveUserMemory` 를 부름). 지도 대신 `_calls.tsv` 로 길을 찾고 이 둘만 뽑았다. `MiReserveUserMemory` 는 이름만.
  - kernel32·kernelbase `CreateThread` (각 64~71B). 이유: 요청서가 "한 줄, 이름만" 이라 했지만 오프셋 근거가 필요해서.
  - `refs` (ntdll `RtlCreateUserStack`, kernelbase 쪽 4개, `KernelBaseGlobalData`): ⑤ 의 callers 와 (b) 의 조건 칸이 어디서 정해지는지 찾으려고.
- **읽기 전용 임시 스크립트** (`.claude/`·`ms/` 밖, 세션 임시 폴더, `python3 -I`): 점프 표 풀기 (`jt.py`), PE import 표 읽기 (`imps.py`), `.text` 에서 rip 상대 참조 찾기 (`ripscan.py`), `.rdata` 문자열 읽기 (`strat.py`). PDB 에서 `KiProcessorBlock`·`KeActiveProcessors` 이름 찾기는 `llvm-pdbutil-14 dump -publics` + 섹션 표.
- 요청서의 `NtSetInformationProcess` "정보 칸 `+8` 크기" 는 asm 과 다르다 (`+0x10`). 위 2번에 바로잡아 적었다.
- 요청서 "② PDB 의 `PROCESSINFOCLASS` 값 `0x29` 이름": PDB 에 그 열거형이 없다 (위 2번). 공개 헤더 이름은 `[추측: 이름]` 으로만.
- 새로 쓴 함수·전역 이름은 모두 `_index.tsv` 에서 확인했다 (`pdb_aliases --where` 는 필요 없었음). 데이터 이름 (`KiProcessorBlock`, `KeActiveProcessors`, `MmRegistryState` 등) 은 PDB `-publics` 와 `_refs` 로.

## 판정

**분석성공.** 질문 ①~⑤ 모두 `.asm` 줄과 함께 답했다. ① Q42 동적 몫 "(0, 0)" 은 정적으로 닫힘 (CreateFlags 0/1 만 조건부로 동적). ② 클래스 `0x29` 는 `MmAllocateUserStack` → `MEM_RESERVE` (`MiReserveUserMemory`) 로 확정, PDB 에 이름 없음 (공개 헤더 이름 `[추측]`). ③ 비트 0 읽는 줄 못 찾음 (훑기 한계). ④ 노드 갈래 = 이상적 프로세서 속성 (노드+1), test.exe 는 안 탐. ⑤ ntdll 쪽은 커널 쪽과 같은 꼴 (다른 점 표), callers 는 `CreateFiberEx` 하나. 정적으로 못 닫은 것: CreateFlags 조건 셋의 실제 값, `MiReserveUserMemory` 가 돌려주는 주소 모양 — 동적 (Q42·Q104). 버전 대조 일치.

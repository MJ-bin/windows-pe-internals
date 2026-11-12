# 70 thread-stack — 정적 분석 결과

- 요청서: `_workspace/sast/70_thread-stack.md` (Q141a 이어서, PE_emulator 요청 25 의 ㄱ-2)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있음).
- 뽑은 함수: **새로 0개.** `^RtlCreateUserStack$` (rva `0x938db0`, 738B) 의 `.asm`·`.c` 가 이미 있었다 (10월 30일에 뽑은 것). 다시 안 뽑은 이유: `_calls.tsv` 5줄이 `.asm` 과 맞고, 바이트로도 맞았다 (`objdump` 로 `+0x202` call 목적지 = `0x140444de0` = `RtlImageNtHeader` rva `0x444de0`, `+0x255`·`+0x258` 명령, 안 닿는 틈 2곳).
  - 파일: `ms/decomp/ntoskrnl/RtlCreateUserStack.c`, `ms/disassemble/ntoskrnl/RtlCreateUserStack.asm`
  - 같이 읽은 것 (이미 있던 파일): `PspSetupUserStack.asm` (+0x28~+0x89), `NtCreateThreadEx.asm` (+0x154~+0x175), `NtCreateUserProcess.asm` (+0xd92~+0xdbc), `PspAllocateThread.asm` (+0x11d, +0x5c6~+0x5ea, +0x8c7).
  - callee 본문 (`ZwAllocateVirtualMemory`, `ZwSetInformationProcess`, `RtlImageNtHeader`, `ZwFreeVirtualMemory`) 은 **안 읽었다.** 이름·인자만.
- 버전 확인 (알려진 사실 대조, **일치**): `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines` 둘 다 `_calls.tsv` 에서 다시 확인. `PspSetupUserStack +0x89 / ret +0x8e → RtlCreateUserStack` 도 sast/69 와 같다.

## 스크립트 문제

없다. 참고만 (고치지 않음):

- `.asm` 맨 끝 "읽지 않은 틈" 2곳 (`+0xc6~+0xca`, `+0x267~+0x26b`) 을 `objdump` 로 봤다. 둘 다 `E9 rel32` (JMP) 한 개로, 목적지가 `+0x1d7` (함수 끝 정리). 앞 명령이 `JMP`/`Jcc` 라서 아무도 안 닿는 **죽은 코드**다. 읽을 것 없음.

## 읽는 법 (표기)

- 커널 PDB 는 공개 심볼이라 함수 원형이 없다. 인자·구조체 칸 **이름**은 널리 알려진 원형에 asm 자리를 맞춘 것이라 `[추측: 이름]`. 자리·오프셋·상수는 `[정적]`.
- 함수 안에서 `RSP` 는 머리 부분(저장 7개 + `SUB RSP,0x80`) 뒤 값이다. 진입 때 `RSP` 보다 `0xb8` 작다. 그래서 인자 보관칸 `[진입RSP+8]` = `[RSP+0xc0]`, `[진입RSP+0x10]` = `[RSP+0xc8]`.
- **주의**: `_calls.tsv`·지도는 오프셋 순서로 정렬돼 있다. `RtlImageNtHeader (+0x202)` 가 맨 뒤에 있지만, 그 블록 (`+0x1eb ~ +0x250`) 은 **실행으로는 맨 앞** (할당보다 먼저)에서 탄다. 코드 모양이 "잘 안 쓰는 길을 뒤로 뺀" 것이다.

인자 (`RtlCreateUserStack`, 진입 때):

| # | 자리 | 이름 `[추측]` | 어디서 쓰이나 |
|---|---|---|---|
| 1 | `RCX` → `[RSP+0xc0]` (`+0x7`) | CommittedStackSize (커밋 크기) | 0 이면 헤더 `SizeOfStackCommit` 으로 (`+0x22d`) |
| 2 | `RDX` → `[RSP+0xc8]` (`+0x3`) | MaximumStackSize (reserve 크기) | 0 이면 헤더 `SizeOfStackReserve` 로 (`+0x244`) |
| 3 | `R8` → `R12` (`+0x20`) | ZeroBits | `ZwSetInformationProcess` 정보 칸 `+0x18` 에 그대로 (`+0xe9`) |
| 4 | `R9` → 아래 56비트 `RSI` (`+0x3f`), 위 8비트 `R14` (`+0x2e`/`+0x31`) | PageSize \| (노드 << 56) | 아래 56비트 = 페이지 크기 `P`, 위 8비트 = 선호 노드 |
| 5 | `[진입RSP+0x28]` = `[RSP+0xe0]` | (안 읽음) | `+0x26` 에서 0 으로 덮어 쓰고 가드 크기 지역 변수로 재사용 (`+0x19b`) |
| 6 | `[진입RSP+0x30]` = `[RSP+0xe8]` | InitialTeb (출력) | `+0x114` 에서 읽음 |

(`PspSetupUserStack` 쪽: `+0x75 R8=[StackInfo+8]`, `+0x7c RDX=[StackInfo+0x18]`, `+0x80 RCX=[StackInfo+0x10]`, `+0x79 R9=RDI`, `+0x84 [RSP+0x28]=R14` (InitialTeb). 5번째 칸 `[RSP+0x20]` 은 안 채운다.)

## 질문별 답 [정적]

### 1. `RCX`·`RDX` = 커밋·reserve 인가: **예, sast/69 표기가 맞다.**

- 값의 출처: `NtCreateThreadEx +0x158/+0x15f` 9번째 인자(`[RBP+0x340]`, StackSize) → StackInfo `+0x10`, `+0x163/+0x16a` 10번째 인자(`[RBP+0x348]`, MaximumStackSize) → StackInfo `+0x18`, `+0x16e/+0x175` 8번째 인자(ZeroBits) → StackInfo `+8`. `PspSetupUserStack +0x80` 이 `+0x10` 을 `RCX` 로, `+0x7c` 가 `+0x18` 을 `RDX` 로, `+0x75` 가 `+8` 을 `R8` 로 읽는다.
- 쓰임이 증명: `RCX` 값은 커밋 크기 `C` 가 되어 첫 `ZwAllocateVirtualMemory` 의 RegionSize (`+0x145 [RSP+0x30]=RDI`) 로, `RDX` 값은 reserve 크기 `M` 이 되어 `ZwSetInformationProcess` 정보 칸 `+0x10` (`+0xe4 [RSP+0x60]=RBX`) 으로 간다. 헤더 치환도 짝이 맞다: `RCX` ↔ 헤더 `SizeOfStackCommit` (`+0x21a`~`+0x231`), `RDX` ↔ `SizeOfStackReserve` (`+0x21e`~`+0x248`).
- 지금 우리: 요청서 표기 그대로 **맞음.**

### 2. 하나가 0 이면 다른 값으로 바꾸는 줄: **있다. 예 (`RtlCreateUserStack` 안).**

**(a) 갈림길**

- `+0x67 MOV RAX,[R11+8]` (`RCX` 보관칸) / `+0x6b TEST RAX,RAX` / `+0x6e JZ` → 목적지 `0x140938f9b` = **`+0x1eb`**.
- `+0x74 MOV RBX,[R11+0x10]` (`RDX` 보관칸) / `+0x78 TEST RBX,RBX` / `+0x7b JZ` → 같은 `+0x1eb`.
- 즉 **둘 중 하나라도 0 이면** 헤더 블록 (`+0x1eb ~ +0x250`) 으로 간다. 둘 다 0 이 아니면 안 간다. (함수 안에서 `RtlImageNtHeader` 를 부르는 곳은 이 블록 한 곳뿐.)

**(b) 바꾸는 줄: 각각 따로, 자기가 0 일 때만 (`CMOVZ`)**

| 줄 | 하는 일 |
|---|---|
| `+0x222 MOV RAX,[RSP+0xc0]` / `+0x22a TEST RAX,RAX` / **`+0x22d CMOVZ RAX,RCX`** / `+0x231 MOV [RSP+0xc0],RAX` | 커밋이 0 이면 헤더 커밋 값 (`RCX`) 으로. 아니면 그대로 |
| `+0x239 MOV RBX,[RSP+0xc8]` / `+0x241 TEST RBX,RBX` / **`+0x244 CMOVZ RBX,RDX`** / `+0x248 MOV [RSP+0xc8],RBX` | reserve 가 0 이면 헤더 reserve 값 (`RDX`) 으로. 아니면 그대로 |
| `+0x250 JMP` → `+0x81` | 본 계산으로 복귀 |

- 예: 커밋만 0 이고 reserve 가 `0x200000` 이면 → 커밋은 헤더 값, reserve 는 `0x200000` 그대로.

**(c) 값을 가져오는 곳: exe 헤더. 칸이 요청서 `[추측]` 과 정확히 같다.**

| 줄 | 하는 일 |
|---|---|
| `+0x20f MOV ECX,0x20b` / `+0x214 CMP word [RAX+0x18],CX` / `+0x218 JNZ` → `+0x255` | NT 헤더 `+0x18` = OptionalHeader `Magic` 이 `0x20b` (PE32+) 인지 |
| PE32+ (같음): `+0x21a MOV RCX,[RAX+0x68]` / `+0x21e MOV RDX,[RAX+0x60]` | NT `+0x68` = OptionalHeader `+0x50` **SizeOfStackCommit** (8바이트), NT `+0x60` = OptionalHeader `+0x48` **SizeOfStackReserve** (8바이트) |
| 그 밖 (PE32 로 가정): `+0x255 MOV ECX,[RAX+0x64]` / `+0x258 MOV EDX,[RAX+0x60]` / `+0x25b JMP` → `+0x222` | 4바이트 값. 32비트 헤더의 칸이다 (`Magic` 이 `0x20b` 가 아니면 모두 이쪽) |
| `+0x20a TEST RAX,RAX` / `+0x20d JZ` → `+0x25d` `MOV EAX,0xc000007b` / `+0x262 JMP` → `+0x1d7` | `RtlImageNtHeader` 가 NULL 을 돌려주면 **`0xC000007B`** 로 끝 (스택 안 만듦) |

**(d) 헤더를 찾는 법: 대상 프로세스의 `EPROCESS.SectionBaseAddress` 를 `RtlImageNtHeader` 에 넘긴다.**

- `+0x1eb MOV RAX,GS:[0x188]` (현재 스레드 = `_KPRCB +0x8 CurrentThread`) / `+0x1f4 MOV RCX,[RAX+0xb8]` (**`_KTHREAD.ApcState.Process`**: `ApcState` `+0x98` + `Process` `+0x20`, PDB) / `+0x1fb MOV RCX,[RCX+0x2b0]` (**`_EPROCESS.SectionBaseAddress`**, PDB) / **`+0x202 CALL RtlImageNtHeader` (ret `+0x207`)**.
- PEB 의 `ImageBaseAddress` 도, 섹션 정보도 아니다. **프로세스 객체 칸** `+0x2b0` (exe 가 유저 주소 공간에 매핑된 시작 주소) 이다.
- "어느 프로세스" = `ApcState.Process`, 즉 **붙은 (attached) 프로세스.** 부른 쪽 `PspSetupUserStack +0x70 / ret +0x75` 가 대상 프로세스에 `KiStackAttachProcess` 로 붙은 뒤에 이 함수를 부르고 (`+0x89`), 끝나면 `+0xe3 / ret +0xe8` `KiUnstackDetachProcess` 로 뗀다. 그래서 **원격 스레드 (`ProcessHandle` 이 다른 프로세스) 도 그 프로세스의 exe 헤더**를 읽는다. 이 읽기가 대상 프로세스의 유저 메모리를 직접 읽는 것이라 붙어 있어야 한다 `[추측: 이유]`.
- `RtlImageNtHeader` 의 본문 (82B) 은 안 봤다 (요청서 경계).
- WoW64 프로세스는 `PspAllocateThread +0x5d6 CMP [R15+0x310],0` (`EPROCESS.WoW64Process`) / `+0x5de JNZ` 에서 다른 길로 가서 이 함수를 이 자리에서 안 부를 수 있다 (sast/69 에서 x64 길만 읽음). PE32 갈래 (`+0x255`) 가 쓰이는 때는 그쪽 `[추측]`.

- 지금 우리: "스택 크기 0 → exe 헤더 (`SizeOfStackReserve`·`SizeOfStackCommit`)" **맞음** (칸·오프셋까지). 단 아래 3번 다듬기 규칙이 뒤에 붙고, **"둘 다 0 일 때만" 이 아니라 "0 인 쪽만 각각"** 이다.

### 3. 크기 다듬는 규칙

기호: `c`=커밋(헤더 치환 뒤), `m`=reserve(헤더 치환 뒤), `P`=페이지 크기 (`R9` 아래 56비트), `C`=최종 커밋, `M`=최종 reserve.

| 순서 | 규칙 | 줄 (오프셋·상수) |
|---|---|---|
| ① | 커밋이 아직 0 이면 **`0x4000` (16KB)** | `+0x81 MOV ECX,0x4000` / `+0x86 TEST RAX,RAX` / `+0x89 CMOVZ RAX,RCX`. 이 줄이 실제로 쓰이려면 헤더 커밋도 0 이어야 한다 (커밋 0 은 늘 헤더 블록을 거쳐 오니까) |
| ② | **커밋 ≥ reserve 이면 reserve = 커밋을 `0x100000` (1MB) 단위로 올림** | `+0x8d CMP RAX,RBX` / `+0x90 JNC` → `+0x2c0 LEA RBX,[RAX+0xfffff]` / `+0x2c7 AND RBX,-0x100000` / `+0x2ce JMP` → `+0x96`. (같을 때도 탄다. 헤더 reserve 가 0 이어도 탄다) |
| ③ | **페이지 올림**: `C = (c + P−1) & ~(P−1)` | `+0x96 LEA RCX,[RSI−1]` / `+0x9a NOT RCX` / `+0x9d LEA RDI,[RSI−1]` / `+0xa1 ADD RDI,RAX` / `+0xa4 AND RDI,RCX`. `P` 가 2의 거듭제곱인지는 **검사 안 함** |
| ④ | **할당 단위 올림**: `M = (m + 0xffff) & −0x10000` (64KB) | `+0xa7 ADD RBX,0xffff` / `+0xae MOV RDX,-0x10000` / `+0xb5 AND RBX,RDX` |
| ⑤ | **프로세스 최소 커밋**: `X = PEB.MinimumStackCommit` 이 0 이 아니고 `C < X` 이면 `C = X 를 P 단위로 올림`, 그래도 `C ≥ M` 이면 `M = C 를 1MB 단위로 올림` | `+0xb8 MOV RAX,[R13+0x318]` (`R13` = `+0x60` 에서 읽은 `EPROCESS.Peb` `+0x2e0`, `_PEB +0x318 MinimumStackCommit` 둘 다 PDB) / `+0xcb TEST RAX,RAX` / `+0xce JNZ` → `+0x299 CMP RDI,RAX` / `+0x29c JNC` → `+0xd4` (이미 크면 건너뜀) / `+0x2a2~+0x2a9` 올림 / `+0x2ac CMP RDI,RBX` / `+0x2af JNC` → `+0x2dd LEA RBX,[RDI+0xfffff]` / `+0x2e4 AND RBX,-0x100000` / `+0x2b1 ADD RBX,0xffff` / `+0x2b8 AND RBX,RDX` / `+0x2bb JMP` → `+0xd4` |
| 없음 | **reserve 의 따로 정한 최소값은 이 함수에 없다** (`0x40000` 최소는 첫 스레드의 `NtCreateUserProcess +0xd99` 에만 있음). **위쪽 한도도 없다** (너무 크면 Mm 가 실패시킬 뿐) | — |

- `X` (`PEB.MinimumStackCommit`) 의 출처:
  - 커널: `MmCreatePeb +0x4c6 MOV EAX,[0x140fc5218]` (`MmRegistryState+0x28`) / `+0x4cc MOV [RBX+0x318],RAX`. 이 칸 `0x140fc5218` 는 레지스트리 표 한 항목 (rva `0xc81a60 ~ 0xc81a70`: 키 `Session Manager\Memory Management`, 값 이름 **`MinimumStackCommitInBytes`**, 담는 곳 `0x140fc5218`) 의 담는 곳이다 (`ms/disassemble/ntoskrnl/_refs/MmRegistryState.tsv` 의 `PTR_u_Session_Manager_140c80d40+0xd30 → +0x28` 줄, 표 글자는 PE 파일에서 읽음). → 레지스트리에 값이 없으면 0 일 것 `[추측]`.
  - 유저: `ntdll!LdrpInitializeExecutionOptions +0x406 ~ +0x413` 이 이미지 실행 옵션(IFEO)의 `MinimumStackCommitInBytes` 가 더 크면 `[PEB+0x318]` 를 키운다 (`+0x40a CMP`, `+0x413 MOV`). 즉 **첫 스레드는 레지스트리 값만, 둘째 이후는 IFEO 값까지** 볼 수 있다 `[추측]`. (유저 쪽은 이번에 이 줄만 봄.)
- 지금 우리: 요청서에 가정 없음. 이 다듬기 순서 (①→②→③④→⑤) 가 구현 대상이다. `X = 0` 으로 두면 ⑤ 는 빠진다.

### 4. `+0x202` `RtlImageNtHeader` 의 쓰임: **질문 2 의 헤더 읽기가 맞다.** 할당 뒤의 다른 용도가 아니다.

- 오프셋이 할당 (`+0x170`, `+0x1c6`) 보다 큰 건 **코드 배치** 때문이다. 실행 순서는 `+0x6e/+0x7b` (0 검사) → `+0x1eb ~ +0x250` (헤더 읽기 + 치환) → `+0x81` (본 계산) → `+0x104` → `+0x170` → `+0x1c6`. `RtlImageNtHeader` 는 **할당보다 먼저** 불린다.
- 이 함수가 헤더를 읽는 곳은 이 한 군데뿐이고, 결과 (`RAX`) 는 `+0x20a` NULL 검사, `+0x214` Magic 검사, `+0x21a`/`+0x21e`/`+0x255`/`+0x258` 크기 읽기에만 쓰인다.
- 지금 우리: 요청서에 가정 없음.

### 5. 할당 꼴

**"`ZwAllocateVirtualMemory` 두 번 = reserve 한 번 + 커밋 한 번" 이 아니다.** 둘 다 커밋이다 (`MEM_COMMIT`). **reserve 는 `ZwSetInformationProcess` 가 한다.**

| 순서 | call / ret | 부르는 것 | 인자 (asm 줄) | 뜻 |
|---|---|---|---|---|
| 1 | **`+0x104` / `+0x109`** | `ZwSetInformationProcess` | `RCX`=−1 (`+0xfd OR R12,-1`, `+0x101`), `EDX`=**`0x29`** (`+0xf9 LEA EDX,[R9+1]`, `R9D=0x28`), `R8`=`&[RSP+0x50]`, `R9D`=**`0x28`** (`+0xee`) | **정보 클래스 번호 `0x29` (41), 길이 `0x28`.** 정보 칸 40바이트: `+0` 노드 (`+0xd4`), `+4 ~ +0xf` 0 (`+0xd9`, `+0xdf`), `+0x10` reserve 크기 `M` (`+0xe4`), `+0x18` ZeroBits (`+0xe9`), `+0x20` **출력: 예약된 시작 주소 `A`** (`+0x2a` 에서 0 으로 비워 둠, `+0x123` 에서 읽음) |
| 2 | `+0x170` / `+0x175` | `ZwAllocateVirtualMemory` | `RCX`=−1, `RDX`=`&[RSP+0xd8]` (= `A+M−C`), `R8`=0 (`+0x162`), `R9`=`&[RSP+0x30]` (크기 = `C`), `[RSP+0x20]`=`0x1000` (`+0x158`, `MEM_COMMIT`), `[RSP+0x28]`=**`4`** (`+0x14a`, `PAGE_READWRITE`) | **커밋 한 번**: 예약 범위의 **맨 위쪽** `C` 바이트를 읽기/쓰기로 |
| 3 | `+0x1c6` / `+0x1cb` | `ZwAllocateVirtualMemory` | `RDX`=`&[RSP+0xd8]` (= 커밋 바닥 − `3P`), `R9`=`&[RSP+0xe0]` (크기 = `3P`), `[RSP+0x20]`=`0x1000` (`+0x1ab`), `[RSP+0x28]`=**`0x104`** (`+0x1a3`, `PAGE_READWRITE`\|`PAGE_GUARD`) | **가드 영역**: 커밋 바로 아래 **`3 × P` 바이트 (`P=0x1000` 이면 3쪽 = `0x3000`)** 를 한 번에 `0x104` 로 |
| (실패) | `+0x28d` / `+0x292` | `ZwFreeVirtualMemory` | `RDX`=`&[RSP+0x40]` (= `AllocationBase`, `+0x26c`), `R8`=`&[RSP+0x38]` (크기 0), `R9D`=**`0x8000`** (`+0x27a`, `MEM_RELEASE`) | 2번이나 3번이 실패하면 **예약한 범위 통째로 풀고** (크기 0 + RELEASE) 실패 상태 (`EDI`) 를 돌려줌 |

- **reserve 의 증거**: 1번 뒤에는 `MEM_COMMIT` 만 있고 `MEM_RESERVE` 가 어디에도 없다. 그런데 2번이 `A+M−C` 를 쓰고, 실패 때 `A` 에서 `MEM_RELEASE` 한다 → 1번이 `[A, A+M)` 를 예약한다고 읽을 수 있다. `ZwSetInformationProcess` 본문은 안 봐서 "예약" 자체는 `[추측]`, 동적으로 확인 (아래 멈춤점).
- **정보 클래스 `0x29`**: 공개 헤더의 `ProcessThreadStackAllocation` (41) 과 값이 같고, 정보 칸 (`{PreferredNode, Reserved x3, ReserveSize, ZeroBits, StackBase}`, 40바이트) 이 asm 의 칸 배치와 정확히 맞는다 `[추측: 이름]`. **이유**: 정적으로 확정 못 함. 정보 칸이 {노드, reserve 크기, ZeroBits} 를 넘기고 {시작 주소} 를 돌려받는 꼴이라, "노드를 지정해서 예약하고 기준 주소를 받으려는 것" 으로 보인다 (`ZwAllocateVirtualMemory` 인자엔 노드 칸이 없다) `[추측]`.
- **가드 조건**: `+0x142 SUB RBX,RDI` (RBX = `M−C`) 뒤 `+0x18b CMP RBX,R15` (`R15` = `3P`, `+0x4c LEA R15,[RSI+RSI*2]`) / `+0x18e JC` → `+0x1d5` (성공으로 끝). 즉 **`M − C ≥ 3P` 일 때만** 가드를 놓는다. 아니면 가드 없이 성공.
- 반환: 성공 `+0x1d5 XOR EAX,EAX` → `+0x1d7` 끝 정리. 1번 실패 (`+0x10e JS`) 는 InitialTeb 를 **건드리기 전에** 바로 반환. 2·3번 실패 (`+0x179 JS`, `+0x1cf JS`) 는 `+0x26c` 로 가서 풀고 반환 (이때 InitialTeb 의 이미 쓴 칸은 지우지 않는다).
- 지금 우리: 요청서에 가정 없음. "reserve 한 번 + 커밋 한 번 + 가드 한 번" 으로 만들되, **reserve 는 `ZwSetInformationProcess(0x29)` 로, 커밋·가드는 `ZwAllocateVirtualMemory(MEM_COMMIT)` 로** 해야 호출 이름이 맞다.

### 6. 출력 InitialTeb 를 채우는 줄과 계산식

`RSI` = InitialTeb (`+0x114 MOV RSI,[RSP+0xe8]`). `A` = 정보 칸 `+0x20` 의 출력 (`[RSP+0x70]`), `M`, `C` 는 위 3번 결과.

| 칸 | 줄 | 값 |
|---|---|---|
| `+0x00` OldStackBase `[추측: 이름]` | `+0x11c MOV [RSI],R14` | 0 (`+0x109` 에서 `R14D=0`) |
| `+0x08` OldStackLimit | `+0x11f MOV [RSI+8],R14` | 0 |
| **`+0x20` AllocationBase** | `+0x123 MOV RCX,[RSP+0x70]` / `+0x128 MOV [RSI+0x20],RCX` | **`A`** (예약 시작) |
| **`+0x10` StackBase** | `+0x12c LEA RAX,[RCX+RBX]` / `+0x130 MOV [RSI+0x10],RAX` | **`A + M`** (예약 끝 = 스택 꼭대기) |
| **`+0x18` StackLimit** | `+0x17f MOV RAX,[RSP+0xd8]` / `+0x187 MOV [RSI+0x18],RAX` | **첫 커밋이 돌려준 시작 주소 = `A + M − C`** (커밋 바닥). 가드는 이보다 **아래** |

- 커밋 시작 주소 `A + M − C` 는 `+0x134 SUB RCX,RDI` / `+0x137 ADD RCX,RBX` / `+0x13a MOV [RSP+0xd8],RCX` 에서 만든다.
- 쓰는 순서: `+0x11c ~ +0x130` (`AllocationBase`·`StackBase`) 는 예약이 **성공한 직후**, `StackLimit` (`+0x187`) 는 **첫 커밋이 성공한 뒤**. 가드 (3번) 는 InitialTeb 를 안 바꾼다.
- 그림 (위가 높은 주소): `StackBase = A+M` ┬ 커밋 `C` ┬ `StackLimit = A+M−C` ┬ 가드 `3P` (있을 때만) ┬ 나머지 예약 ┬ `AllocationBase = A`.
- 이어지는 곳 (sast/69): `MmCreateTeb +0x12a~+0x13a` 가 이 세 칸을 TEB `+8` (StackBase), `+0x10` (StackLimit), `+0x1478` (DeallocationStack) 로 복사. `CONTEXT.Rsp` = `StackBase − 랜덤 − 0x28` (`PspSetupUserStack +0xdc`).
- 지금 우리: 요청서에 가정 없음.

### 7. 넷째 인자 `R9` 와 셋째 인자 `R8`

**`R9` = `P | (노드 << 56)`** — `ZwAllocateVirtualMemory` 로 가는 인자가 **아니다.**

- 부른 쪽: `PspSetupUserStack +0x4d MOV EDI,0x1000` / `+0x52 CMP [RSP+0xf0],EBX` (노드, 5번째 인자) / `+0x59 JZ` / `+0x62 SHL RAX,0x38` / `+0x66 OR RDI,RAX` / `+0x79 MOV R9,RDI`. 노드가 0 이면 `R9 = 0x1000`.
- **아래 56비트 = 페이지 크기 `P`**: `+0x3f AND RSI,RAX` (`RAX=0x00ffffffffffffff`, `+0x35`). 쓰임: 커밋 올림 (`+0x96~+0xa4`, 3번 ③), 5번 `R15 = 3P` (`+0x4c`, 가드 크기·조건). 값 `0x1000` 은 `MEM_COMMIT = 0x1000` 과 **우연히 같은 숫자**이고 서로 무관하다 (`MEM_COMMIT` 은 `+0x152 MOV R13D,0x1000` 으로 따로 만든다).
- **위 8비트 = 선호 노드**: `+0x2e MOV R14,R9` / `+0x31 SHR R14,0x38`. 쓰임: `+0x42 CMP R14B,0x40` / `+0x46 JA` → `+0x2d3 MOV EAX,0xc000000d` (**노드 > 0x40 이면 `0xC000000D`** 로 끝, 가장 먼저 하는 검사), 통과하면 `ZwSetInformationProcess` 정보 칸 `+0` (`+0xd4 MOV [RSP+0x50],R14D`).
- 노드 값의 출처: `PspSetupUserStack` 5번째 인자 ← `PspAllocateThread +0x5c6/+0x5d2` (`[RSP+0x5c]`). 이 칸은 `+0x11d` 에서 0 으로 시작하고 `+0x8c7` 한 곳에서만 바뀐다 (`+0x8bc KeGetProcessorNodeNumber` 뒤). 그 갈래의 조건은 이번에 안 읽음 → 보통 0 `[추측]`, 동적 확인.

**`R8` = ZeroBits (`[추측: 이름]`)** — 예약에만 쓰인다.

- `+0x20 MOV R12,R8` / `+0xe9 MOV [RSP+0x68],R12` → 정보 칸 `+0x18` 에 **그대로** (검사·치환 없음). 두 `ZwAllocateVirtualMemory` 의 ZeroBits 는 따로 0 (`+0x162 XOR R8D,R8D`, `+0x1b8 XOR R8D,R8D`).
- 첫 스레드: StackInfo `+8` ← 이미지 정보 `ZeroBits` (`NtCreateUserProcess +0xdb6`), 둘째 스레드: StackInfo `+8` ← `NtCreateThreadEx` 8번째 인자 (`+0x16e`).
- 지금 우리: 요청서에 가정 없음.

## 첫 스레드 길과 비교

| 항목 | 첫 스레드 (`NtCreateUserProcess`) | 둘째 스레드 (`NtCreateThreadEx`) |
|---|---|---|
| StackInfo `+0x10` (커밋) | 이미지 정보 `+0x1f0` `CommittedStackSize` (`+0xdaa/+0xdb1`) | 9번째 인자 그대로 (`+0x158/+0x15f`) — 0 이 올 수 있음 |
| StackInfo `+0x18` (reserve) | 이미지 정보 `+0x1e8` `MaximumStackSize`, **최소 `0x40000`** (`+0xd92~+0xda5`) | 10번째 인자 그대로 (`+0x163/+0x16a`) — 0 이 올 수 있음 |
| `RtlCreateUserStack` 의 헤더 갈래 (`+0x1eb`) | 이미지 정보 값이 0 이 아니면 **안 탄다** | 0 인 쪽이 있으면 **탄다** |
| reserve 최소 `0x40000` | 있음 (`NtCreateUserProcess`) | **없음** |
| 그 뒤 (3번 ①~⑤, 5·6번) | 같다 (같은 함수) | 같다 |

- 이미지 정보의 `MaximumStackSize`·`CommittedStackSize` 가 헤더의 `SizeOfStackReserve`·`SizeOfStackCommit` 와 같은 값이라는 건 **이번에 안 확인** (그 칸을 채우는 Mm 함수는 안 봄) `[추측]`.

## 동적분석으로 확인해야 할 것

dast 요청 필요: **있음** (대기열 Q42 에 붙일 후보). 테스트 대상은 sast/69 와 같은 test.exe 의 **둘째 스레드** (`CreateThread(NULL, 0, Worker, NULL, 0, NULL)`, `sample/test.c:34`).

정적으로 못 닫은 것:

1. **`CreateThread(…, 0, …)` 가 커널에 실제로 (0, 0) 을 넘기나.** 유저 쪽 (`KERNELBASE!CreateRemoteThreadEx`) 은 안 읽었다. 보통은 `dwStackSize = 0` 이면 둘 다 0 일 것 `[추측]`.
2. **헤더 갈래를 탔나** (`+0x1eb` 걸림 여부), 읽은 헤더 값이 `0x100000` / `0x1000` 인가. 로컬 `sample/bin/test.exe` 의 헤더는 `SizeOfStackReserve = 0x100000`, `SizeOfStackCommit = 0x1000` (PE32+, `NT+0x60` / `NT+0x68`, 파일을 직접 읽음). **타깃 VM 의 test.exe 와 같은 빌드인지** 는 확인 필요.
3. **`PEB.MinimumStackCommit` (`+0x318`) 실제 값** (0 인가).
4. **`ZwSetInformationProcess(0x29)` 가 실제로 하는 일**: 예약인가, 반환 주소 `A` 의 모양 (64KB 정렬?), 예약 크기가 `M` 인가.
5. **가드 영역 보호값**: 3쪽 모두 `PAGE_GUARD` 인지 (`!address` 또는 `!vad`).
6. 노드 값이 0 인지 (`R9`).

추천 멈춤점 (`nt!RtlCreateUserStack` 은 `PAGE` 구역이라 안 올라와 있으면 `bp` 가 미해결일 수 있으나, 첫 스레드가 이미 거쳐서 올라와 있을 것 `[추측]`). **공통 조건**: 현재 프로세스 `ImageFileName` = test.exe. 로더 워커·풀 스레드까지 걸리니 sast/69 의 `NtCreateThreadEx +0x70` 조건 (`r13d == 0` 그리고 StartRoutine = test 의 스레드 함수) 을 같이 쓰거나, 아래 입구에서 `@rcx == 0 and @rdx == 0` 로 거른다.

| 멈춤점 | 볼 것 |
|---|---|
| `nt!PspSetupUserStack+0x89` (call 위치) | `rcx` 커밋 입력, `rdx` reserve 입력, `r8` ZeroBits, `r9` (`& 0x00ffffffffffffff` = `P`, `>> 0x38` = 노드) |
| **`nt!RtlCreateUserStack+0x1eb`** | **걸리면 헤더 갈래를 탔다** (커밋 또는 reserve 입력이 0). `dq @rsp+0xc0 L2` = (커밋 입력, reserve 입력) |
| `nt!RtlCreateUserStack+0x207` (ret) | `rax` = 헤더 포인터. `dq @rax+0x60 L2` = (`SizeOfStackReserve`, `SizeOfStackCommit`), `dw @rax+0x18 L1` = `0x20b` |
| `nt!RtlCreateUserStack+0x81` | 치환 뒤 값: `rax` = 커밋, `rbx` = reserve (그리고 `dq @rsp+0xc0 L2`) |
| `nt!RtlCreateUserStack+0x104` (call 위치) | `dq @rsp+0x50 L5` (정보 칸: `+0` 노드, `+0x10` **reserve `M`**, `+0x18` ZeroBits), `rdi` = 올림한 커밋 `C`, `dq @r13+0x318 L1` = `MinimumStackCommit` (`r13` = PEB) |
| `nt!RtlCreateUserStack+0x109` (ret) | `eax` 상태, `dq @rsp+0x70 L1` = 예약 시작 `A` (그리고 `!address` 로 모양 확인) |
| `nt!RtlCreateUserStack+0x170` | 첫 커밋: `dq @rsp+0xd8 L1` (시작), `dq @rsp+0x30 L1` (크기), `dd @rsp+0x20 L2` (`0x1000`, `4`) |
| `nt!RtlCreateUserStack+0x1c6` | 가드: `dq @rsp+0xd8 L1`, `dq @rsp+0xe0 L1` (`3P`), `dd @rsp+0x20 L2` (`0x1000`, `0x104`) |
| `nt!RtlCreateUserStack+0x1d7` (모든 반환 합류점) | `eax` 반환값, `dq poi(@rsp+0xe8) L5` = InitialTeb 다섯 칸 (이 줄에서 `rsp` 는 아직 본체 값) |
| `nt!PspSetupUserStack+0x8e` (ret) | `dq @r14 L5` — 위와 같은 InitialTeb (sast/69 와 같은 지점) |

## 후속 제안 (정적)

- 필요한 새 정적 요청은 없다. 동적으로 4번 (`ZwSetInformationProcess` 가 실제로 어떻게 예약하나) 이 이상하게 나오면 그때 `NtSetInformationProcess` 의 `0x29` 갈래 (Mm 쪽) 를 한 건으로 읽는다.
- (선택) 유저 쪽 `KERNELBASE!CreateRemoteThreadEx` 가 `dwStackSize`·`STACK_SIZE_PARAM_IS_A_RESERVATION` 으로 `NtCreateThreadEx` 의 9·10번째 인자를 정하는 줄은 안 읽었다. 동적 멈춤점 `NtCreateThreadEx +0x70` 의 값 (sast/69) 으로도 닫힌다.

## 요청서와 다르게 한 것 (이유)

- 요청서 범위 밖 읽기: `ntdll!LdrpInitializeExecutionOptions` 의 `+0x406 ~ +0x413` (`PEB+0x318` 쓰는 줄), `MmCreatePeb +0x4c6 / +0x4cc` (같은 칸), `MmRegistryState.tsv` (`_refs`, 이미 있던 파일), PE 데이터에서 레지스트리 표 한 항목 (`rva 0xc81a40 ~ 0xc81a70` 만, 읽기 전용). 이유: 3번 ⑤ 의 `X` 가 어디서 오는지 모르면 "둘째 스레드 실제 크기" 를 못 말해서. 이 함수의 callee 는 아니다.
- 로컬 `sample/bin/test.exe` 헤더 읽기 (python `-I`, 읽기 전용): 최종 답의 숫자를 대려고.
- ntdll 같은 이름 함수: `ntdll!RtlCreateUserStack` (rva `0xf5130`, 709B, `ms/decomp/ntdll/_index.tsv`) 이 있다. 이름이 같고 크기도 비슷해 (커널 738B) **같은 모양일 것 같음 `[추측]`.** 본문은 안 읽었다 (ntdll 쪽은 아직 안 뽑음: `.asm` 없음).
- 새로 쓴 함수 이름은 모두 `_index.tsv` 에서 확인했다 (`pdb_aliases --where` 로 `RtlImageNtHeader` 도 확인).

## 판정

**분석성공.** 질문 1~7 을 `.asm` 줄과 함께 답했다. 정적으로 못 닫은 것: (a) 유저 쪽이 커널에 0 을 넘기는지, (b) `ZwSetInformationProcess(0x29)` 가 실제로 예약을 하는지(본문 안 봄), (c) `PEB.MinimumStackCommit` 실제 값 — 모두 동적 확인. 버전 대조는 일치, 요청서 후속 제안 (sast/69) 대로 `RtlCreateUserStack` 하나로 "크기 0 → exe 헤더" 가 닫혔다.

## PE_emulator ㄱ-2 최종 답

**둘째 스레드 (`NtCreateThreadEx`) 가 StackSize(커밋)=0, MaximumStackSize(reserve)=0 을 받으면**, 커널은 0 을 그대로 `PspSetupUserStack +0x89` → `RtlCreateUserStack` 까지 내려보내고 (`NtCreateThreadEx +0x15f/+0x16a` 에 0 검사·치환 없음), **그 함수 안에서 0 인 쪽마다 따로** exe 헤더 값으로 바꾼다: 커밋 0 → `SizeOfStackCommit`, reserve 0 → `SizeOfStackReserve` (PE32+ 는 NT 헤더 `+0x68` / `+0x60` 8바이트, 그 밖은 `+0x64` / `+0x60` 4바이트). 줄은 `RtlCreateUserStack +0x6e/+0x7b` (0 이면 `+0x1eb` 로 점프) → `+0x22d CMOVZ` (커밋), `+0x244 CMOVZ` (reserve). 헤더는 **대상 프로세스의 `EPROCESS.SectionBaseAddress`** (`+0x1f4`·`+0x1fb`, `ApcState.Process` + `0x2b0`) 를 `RtlImageNtHeader` 에 넘겨 찾고 (`+0x202`), PEB `ImageBaseAddress` 가 아니다. 그러니 PE_emulator 의 "스택 크기 0 → exe 헤더 (`SizeOfStackReserve`·`SizeOfStackCommit`)" `[추측]` 은 **맞다.** 다만 치환 뒤에 규칙이 붙는다: 커밋이 그래도 0 이면 `0x4000` (`+0x81~+0x89`), 커밋 ≥ reserve 면 reserve = 커밋을 1MB 로 올림 (`+0x8d/+0x90 → +0x2c0`), 커밋은 페이지(`P`=`0x1000`) 로, reserve 는 `0x10000` 로 올림 (`+0x96~+0xb5`), `PEB.MinimumStackCommit` (레지스트리 `MinimumStackCommitInBytes`, 기본 0 `[추측]`) 이 더 크면 커밋을 그 값으로 (`+0xb8~+0xce`, `+0x299~+0x2bb`). 결과는 `ZwSetInformationProcess(클래스 0x29, 길이 0x28)` 가 reserve `M` 만큼 예약하고 (`+0x104`), 그 맨 위쪽 `C` 바이트를 `MEM_COMMIT`·`PAGE_READWRITE` (`+0x170`), 그 바로 아래 `3×P` 바이트를 `PAGE_READWRITE\|PAGE_GUARD` (`0x104`) 로 커밋하는 것 (`M−C ≥ 3P` 일 때, `+0x1c6`). InitialTeb 는 `StackBase = A+M`, `StackLimit = A+M−C`, `AllocationBase = A` (`+0x130`, `+0x187`, `+0x128`). **test.exe** 는 헤더가 `SizeOfStackReserve = 0x100000`, `SizeOfStackCommit = 0x1000` (로컬 `sample/bin/test.exe`) 이므로 `CreateThread(NULL, 0, …)` 가 (0, 0) 을 넘기면 `M = 0x100000` (1MB), `C = 0x1000` (1쪽), 가드 `0x3000` 이 된다 (`PEB.MinimumStackCommit = 0` 일 때). **첫 스레드와 같은 값인가: test.exe 는 같다.** 첫 스레드는 `NtCreateUserProcess +0xd92~+0xdbc` 가 이미지 정보 값으로 StackInfo 를 미리 채워 (reserve 는 최소 `0x40000`) `RtlCreateUserStack` 의 헤더 갈래를 안 타지만 (이미지 정보가 헤더 값과 같다면 `[추측]`), 같은 다듬기를 거쳐 같은 `M=0x100000`, `C=0x1000` 이 된다. **다른 경우**: 헤더 reserve 가 `0x40000` 보다 작으면 첫 스레드만 `0x40000` 으로 올라가고 둘째는 헤더 값 그대로 (`0x10000` 단위 올림만). 또 `MinimumStackCommit` 는 첫 스레드엔 레지스트리 값만, 둘째 이후엔 IFEO 값까지 쓰일 수 있다. 동적으로 닫을 것: `CreateThread` 가 (0, 0) 을 넘기는지, `nt!RtlCreateUserStack+0x1eb` 가 걸리는지, `+0x104` 의 `M` 과 `+0x1d7` 의 InitialTeb 값 (위 "동적분석" 표).

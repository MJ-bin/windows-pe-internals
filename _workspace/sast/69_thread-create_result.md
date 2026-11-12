# 69 thread-create — 정적 분석 결과

- 요청서: `_workspace/sast/69_thread-create.md` (Q141a, PE_emulator 요청 25 의 ㄱ)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄이 다 있음).
- 지도 새로 만듦: `run_ghidra.sh map ntoskrnl.exe NtCreateThreadEx` → `ms/disassemble/ntoskrnl/_map/NtCreateThreadEx_{edges,nodes}.tsv`, `_summary.txt` (함수 2620개 = expanded 1752 / helper 362 / depth 506, 5초, 로그 `ms/ghidra/ntoskrnl.map.log`).
- 뽑은 함수:
  - ① `^(NtCreateThreadEx|PspCreateThread)$` depth 1 → 20개 (새로 뽑음, 이 둘은 `.asm`·`.c` 가 처음 생김)
  - ② `^(PspSetupUserStack|MmCreateTeb)$` depth 0 → 2개 (요청서: 스택을 잡는 쪽이 다른 함수로 이어지면 "그 함수 하나까지만")
  - 이미 있던 `PspBuildCreateProcessContext`·`PspAllocateThread`·`PspCreateUserContext` 는 다시 안 뽑고 읽기만 했다. `PspInsertThread`·`NtCreateUserProcess` 는 ① 의 depth 1 (PspInsertThread) 과 옛 파일 (NtCreateUserProcess, 비교용) 로 읽었다.
  - 파일: `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/` (`_calls.tsv` 는 덧붙여 쌓임). export 로그 `ms/ghidra/ntoskrnl.export.log`, SCRIPT ERROR 0건.
- 버전 확인 (알려진 사실 대조, **일치**):
  - `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess` 일치
  - `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines` 일치
  - 요청서 `[정적]` 줄도 다시 맞음: `NtCreateUserProcess +0x295/+0x29a` PspBuildCreateProcessContext, `+0xd1d/+0xd22` PspCreateUserContext, `+0x1015/+0x101a` PspInsertThread. run11·run54 의 `PspAllocateThread +0x640~+0x655`, `+0x54a~+0x5a5`, `+0x793` 도 `.asm` 줄이 그대로.

## 스크립트 문제

막힌 것은 없다. 참고만 (고치지 않음).

- 질문 4(d) 는 "어느 쓰기가 예외로 보호되나" 를 알아야 했는데, 스크립트가 `.pdata` 의 예외 표 (try 범위) 를 안 보여 준다. 그래서 읽기 전용으로 직접 봤다 (파일은 `.claude/`·`ms/` 밖):
  - 표 읽기: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/14c1dff7-ed00-4e6d-a07a-32fef705671a/scratchpad/tools/scopes2.py` (`python3 -I`), 착지점 명령은 `objdump -d` 로 확인.
  - 제안: 이런 질문이 또 나오면 `.asm` 머리에 "try 범위 → 착지점" 줄을 붙이는 기능을 사용자 검토 뒤에 검토. (`.asm` 의 "읽지 않은 틈" 이 이미 착지점 자리를 가리키고 있다: `NtCreateThreadEx +0xa7~+0xab`, `PspInsertThread +0x411~+0x431` 등.)

## 읽는 법 (표기)

- 커널 PDB 는 공개 심볼이라 **함수 원형(인자 이름)이 없다.** 인자 이름은 널리 알려진 원형 순서에 asm 의 스택 자리를 맞춘 것이라 `[추측: 이름]`. 자리·오프셋은 `[정적]`.
- `NtCreateThreadEx` 인자 자리 (진입 때 RSP 기준 5번째 = `RSP+0x28`, 본문에서는 `RBP+0x320`):

| # | 자리 | 이름 `[추측]` |
|---|---|---|
| 1 | `RCX` → `RSI` (+0x34) | ThreadHandle (출력 포인터) |
| 2 | `EDX` → `[RBP+0x10]` (+0x31) | DesiredAccess |
| 3 | `R8` → `[RBP+0x40]` (+0x2d) | ObjectAttributes |
| 4 | `R9` → `R15` (+0x2a) | ProcessHandle |
| 5 | `[RBP+0x320]` → `[RBP+0x20]` (+0x37~+0x3e) | StartRoutine |
| 6 | `[RBP+0x328]` → `[RBP+0x18]` (+0x42~+0x49) | Argument |
| 7 | `[RBP+0x330]` → `R13D` (+0x69) | CreateFlags |
| 8 | `[RBP+0x338]` | ZeroBits |
| 9 | `[RBP+0x340]` | StackSize |
| 10 | `[RBP+0x348]` | MaximumStackSize |
| 11 | `[RBP+0x350]` → `RDI` (+0x4d) | AttributeList |

- `PspCreateThread` 는 인자 13개: 1 ThreadHandle, 2 DesiredAccess, 3 ObjectAttributes, 4 ProcessHandle, 5 (프로세스 객체, 핸들이 0 일 때. 여기선 0), 6 `ctx` (NtCreateThreadEx 지역 0x260 바이트 칸), 7 ClientId 출력 포인터 (= `ctx+0x18`), 8 CONTEXT 포인터, 9 InitialTeb 칸 (0x48 바이트), 10 CreateFlags, 11 StartRoutine, 12 Argument, 13 StackInfo (0x20 바이트: `+0` 바이트 표시, `+8` ZeroBits, `+0x10` StackSize, `+0x18` MaximumStackSize).
- 같은 일을 하는 첫 스레드 길은 `NtCreateUserProcess` (run9). 아래 질문 끝에 비교표를 붙였다.

## 질문별 답 [정적]

### 1. 부르는 순서

**`NtCreateThreadEx`** (`_calls.tsv`, call 위치 / ret 위치, 오프셋 순서 = 실행 순서):

| # | call / ret | 부르는 것 | 조건·뜻 |
|---|---|---|---|
| 1 | `+0xb8` / `+0xbd` | `memset` | `ctx` 0x260 바이트 0 으로 비움 (그 앞 `+0x70` CreateFlags 검사, `+0x8e~+0xa5` ThreadHandle 쓰기 시험은 호출 없음) |
| 2 | `+0xde` / `+0xe3` | `PspBuildCreateProcessContext` | `AttributeList` 가 NULL 이 아닐 때만 (`+0xbd TEST RDI,RDI`, `+0xc0 JZ`). 음수면 `+0xe5 JS` 로 바로 끝 |
| 3 | `+0x127` / `+0x12c` | `ObpReferenceObjectByHandleWithTag` | ProcessHandle, 접근 `2`, `PsProcessType`, 태그 `0x72437350` |
| 4 | `+0x191` / `+0x196` | `memset` | InitialTeb 칸 0x48 바이트 (뒤에 `R12`) 비움 — 프로세스 `SecureProcess` 비트가 0 일 때만 (`+0x138 TEST [RDI+0x170],1`, `+0x13f JNZ +0x275`) |
| 5 | `+0x1d3` / `+0x1d8` | `RtlGetExtendedContextLength2` | CONTEXT 길이 |
| 6 | `+0x1f3` / `+0x1f8` | `__chkstk` (= `_alloca_probe`, 같은 주소) | 스택에 CONTEXT 자리 잡기 |
| 7 | `+0x205` / `+0x20a` | `memset` | CONTEXT 칸 0 으로 |
| 8 | `+0x217` / `+0x21c` | `RtlInitializeExtendedContext2` | `ContextFlags` 채우기 |
| 9 | `+0x27d` / `+0x282` | `ObfDereferenceObjectWithTag` | 3번의 프로세스 참조 되돌림 (→ `PspCreateThread` 가 프로세스를 따로 다시 잡음) |
| **10** | **`+0x2cd` / `+0x2d2`** | **`PspCreateThread`** | 질문 1 의 답 |
| 11 | `+0x2d8` / `+0x2dd` | `PspDeleteCreateProcessContext` | `ctx` 안에 잡은 풀 정리 |
| 12 | `+0x2e9` / `+0x2ee` | `__security_check_cookie` | 끝 |

**`PspCreateThread`** (정상 길 실행 순서. 오프셋 순서와 같음):

| # | call / ret | 부르는 것 | 조건·뜻 |
|---|---|---|---|
| 1 | `+0x97` / `+0x9c` | `memset` | 지역 0x190 바이트 비움 (뒤에 접근 상태로 쓰임) |
| 2 | `+0x102` / `+0x107` | `ObpReferenceObjectByHandleWithTag` | ProcessHandle 이 0 이 아닐 때. 0 이면 `+0x380/+0x385 ObfReferenceObjectWithTag` |
| – | `+0x349` / `+0x34e` | `PspIsProcessReadyForRemoteThread` | 대상 프로세스가 **지금 프로세스가 아닐 때만** (`+0x135 CMP RDI,RSI`, `+0x138 JNZ`) |
| 3 | `+0x204` / `+0x209` | `ExAcquireRundownProtection` | 실패 시 `0xC000010A` (`+0x332`) |
| 4 | `+0x219` / `+0x21e` | `ObfReferenceObjectWithTag` | 프로세스 몫 하나 더 |
| **5** | **`+0x27e` / `+0x283`** | **`PspAllocateThread`** | 스레드 개체·스택·TEB·`KeInitThread`. 음수면 `+0x287 JS` → `+0x438` 정리 |
| 6 | `+0x295` / `+0x29a` | `ObfDereferenceObjectWithTag` | |
| **7** | **`+0x2e5` / `+0x2ea`** | **`PspInsertThread`** | 목록·객체 등록·핸들·`KeReadyThread` |
| 8~11 | `+0x2ef`, `+0x2f7`, `+0x301`, `+0x312` | `ExReleaseRundownProtection`, `KiLeaveCriticalRegionUnsafe`, `ObfDereferenceObject`, `__security_check_cookie` | 정리 후 끝 |

- 호출 전 `+0x1f1 DEC word [R15+0x1e4]` 는 `KeEnterCriticalRegion` 을 인라인한 것 (`KiLeaveCriticalRegionUnsafe` 짝).
- 에러 길 호출: `KiLeaveCriticalRegionUnsafe` `+0x32d`, `+0x443`, `ObfDereferenceObjectWithTag` `+0x33f`, `+0x392`, `ExReleaseRundownProtection` `+0x43b`.
- **몇 번째인가**: 직접 호출 11개 중 `PspAllocateThread` **5번째**, `PspInsertThread` **7번째**. 둘 다 정상 길에서 **각각 한 번씩**, 이 순서로만 불린다 (첫 스레드 길도 같은 두 함수, run9·run12).
- 근거: `NtCreateThreadEx.asm`, `PspCreateThread.asm`, `_calls.tsv`, 지도 `_map/NtCreateThreadEx_edges.tsv` (깊이 1: `PspCreateThread`).
- 지금 우리: 요청서에 가정 없음 (해당 없음).

### 2. 새 스레드 첫 문맥

**(a) 시작 주소·인자 → 레지스터. 예, `Rcx`·`Rdx` 가 맞다.** `NtCreateThreadEx` 가 스택에 CONTEXT 를 직접 만들고 이렇게 쓴다 (`RBX` = CONTEXT):

| CONTEXT 칸 | 값 | 줄 |
|---|---|---|
| `Rcx` (+0x80) | StartRoutine (인자 5, `[RBP+0x20]`) | **`NtCreateThreadEx +0x22b`** (`MOV [RBX+0x80],RAX`) |
| `Rdx` (+0x88) | Argument (인자 6, `[RBP+0x18]`) | **`NtCreateThreadEx +0x236`** (`MOV [RBX+0x88],RAX`) |
| `Rip` (+0xf8) | `PsNtdllExports+0x10` 칸의 값 | **`NtCreateThreadEx +0x220`** (읽기는 `+0x1bf`, `[0x140fc74c0]`) |

- 이어서 커널이 이 CONTEXT 를 읽는 줄: `PspAllocateThread +0x640/+0x647` (`StartAddress` ← `Rip`), `+0x64e/+0x655` (`Win32StartAddress` ← `Rcx`) — run11 과 같음. 그래서 둘째 스레드는 `StartAddress` = ntdll 쪽 고정 주소, `Win32StartAddress` = 사용자 시작 함수.
- 이 CONTEXT 는 프로세스 `EPROCESS.Pcb.SecureState.SecureProcess` (+0x170 비트0, PDB) 가 **0 일 때만** 만든다. 1 이면 CONTEXT 없이 `PspCreateThread +0x366` 이 플래그 `0x400` 을 켜고 비밀 스레드 길로 간다. test.exe 는 일반 프로세스라 앞쪽 길 `[추측]` (동적으로 한 번 확인).
- 지금 우리: `Rcx`=시작 주소, `Rdx`=인자 → **맞음**.

**(b) `PspCreateUserContext` 를 부르나: 아니오.**
- `NtCreateThreadEx` 의 호출 12줄 어디에도 없다. `PspCreateThread` 에도 없다. 지도 `NtCreateThreadEx_{nodes,edges}.tsv` (깊이 10까지, 2620개) 에 이 함수가 아예 없고, `_nodes` 의 `callers_total` 도 1 (`NtCreateUserProcess +0xd1d`) 이다.
- 문맥을 만드는 것은 `NtCreateThreadEx` **자기 자신** (인라인): `+0x1d3 RtlGetExtendedContextLength2` → `+0x1f3` 스택 자리 → `+0x205 memset` → `+0x217 RtlInitializeExtendedContext2` → `+0x220~+0x26b` 레지스터 쓰기.
- 쓰는 값 세트가 `PspCreateUserContext` (91바이트) 와 **똑같다**: `SegCs=0x33` (+0x254), `SegDs=SegEs=0x2b` (+0x242), `SegFs=0x53`·`SegGs=0x2b` (+0x249), `SegSs=0x2b` (+0x250), `MxCsr=0x1f80` (+0x25f), `FltSave.ControlWord=0x27f` (+0x262), `FltSave.MxCsr=0x1f80` (+0x26b). `ContextFlags` 는 `0x10000b` (CONTROL|INTEGER|FLOATING_POINT), 프로세스 `MitigationFlags2` (+0x754) 비트 14 = `CetUserShadowStacks` 가 켜져 있으면 `0x40` (XSTATE) 이 더해지고 확장 마스크 `0x800` 이 붙는다 (`+0x196~+0x1bc`). 차이는 이것뿐 (`PspCreateUserContext` 는 `0x10000b` 고정).
- 지금 우리: 요청서에 가정 없음. `PspCreateUserContext` 를 재사용해도 **레지스터 값은 같게 나온다**, 다만 커널 이름·호출관계는 다르다 (이 길에선 `NtCreateThreadEx` 안).

**(c) `CONTEXT.Rip`: `ntdll!RtlUserThreadStart` 쪽 고정 주소.** 사용자 시작 주소가 아니다.
- 값은 `PsNtdllExports+0x10` 칸 (`NtCreateThreadEx +0x1bf`). 이 칸 = `RtlUserThreadStart` 는 run16 (표 `NtdllExports` 둘째 항목, 부팅 때 채움) 에서 `[정적]` 확정. 첫 스레드도 같은 칸 (`NtCreateUserProcess +0xd10`).
- 사용자 시작 주소는 `Rcx` 로 가서 `RtlUserThreadStart(Rcx, Rdx)` 가 부르게 된다 (유저 쪽은 이번에 안 봄).
- 지금 우리: "Rip=시작 주소 그대로" 로 하고 있다면 **틀림** (요청서엔 가정이 적혀 있지 않아 이 한 줄은 확인용).

**(d) 스택: 커널이 잡는다. 크기 0 일 때 exe 헤더를 쓰는 줄은 이 길 (읽은 4개 함수) 엔 없다.**
- 유저가 미리 잡아 넘기는 칸이 이 길엔 없다. StackInfo 첫 바이트의 bit0 ("이미 있음") 은 `NtCreateThreadEx +0x154 MOV byte [R14],0` 이 늘 0 으로 둔다. `PspSetupUserStack +0x28 TEST [R9],1` 이 0 이니 만든다.
- 사슬 (커널이 `StackSize`·`MaximumStackSize`·`ZeroBits` 를 **그대로** 쓴다):
  1. `NtCreateThreadEx +0x15f/+0x16a/+0x175` — 인자 9·10·8 을 StackInfo `+0x10`/`+0x18`/`+0x8` 에 복사. **0 검사·치환 없음.**
  2. `PspCreateThread` 는 StackInfo 포인터를 `PspAllocateThread` 11번째 인자로 그대로 넘김 (`+0x27e` 앞 `[RSP+0x50]`).
  3. `PspAllocateThread +0x5ea` / ret `+0x5ef` `PspSetupUserStack` (프로세스가 WoW64 면 `+0x9a9`, 이번엔 x64 길만 읽음).
  4. `PspSetupUserStack +0x89` / ret `+0x8e` **`RtlCreateUserStack`** (ntoskrnl 안 함수, rva `0x938db0`): `RCX=[StackInfo+0x10]`, `RDX=[+0x18]`, `R8=[+0x8]`, `R9=0x1000(|노드<<56)`, 출력 InitialTeb = `[RSP+0x28]`. 앞뒤로 `KiStackAttachProcess` (+0x70/+0x75), `KiUnstackDetachProcess` (+0xe3/+0xe8).
  5. `PspAllocateThread +0x62c` / ret `+0x631` `MmCreateTeb` (InitialTeb 의 `+0x10/+0x18/+0x20` = StackBase/Limit/AllocationBase 를 TEB `+8/+0x10/+0x1478` 로 복사. `MmCreateTeb +0x12a~+0x13a`).
- **`CONTEXT.Rsp`** 는 `PspSetupUserStack +0xdc` (`MOV [R13+0x98],RAX`): `StackBase − ((ExGenRandom(1) & 0x7f) << 4) − 0x28`. `EPROCESS.MitigationFlags` (+0x750) 비트 6 = `StackRandomizationDisabled` 가 켜져 있으면 랜덤 몫이 0 (`+0x9f/+0xa1`).
- **exe 헤더 (`SizeOfStackReserve`·`SizeOfStackCommit`) 를 쓰는 줄**:
  - 읽은 4개 함수 (`NtCreateThreadEx`, `PspCreateThread`, `PspAllocateThread`, `PspSetupUserStack`) 에는 **없다.**
  - **첫 스레드 길엔 있다**: `NtCreateUserProcess +0xd92~+0xdbc` 가 이미지 정보 (`[RBP+0x1d8]` = `_SECTION_IMAGE_INFORMATION`, PDB 로 칸 맞춤: `+0x1e0` ZeroBits, `+0x1e8` MaximumStackSize, `+0x1f0` CommittedStackSize) 로 StackInfo 를 채운다. 이때 reserve 는 **최소 0x40000** 으로 올린다 (`+0xd99 MOV ECX,0x40000`, `+0xd9e CMP`, `+0xda1 CMOVC`, `+0xda5` 쓰기). 그리고 같은 이미지 정보의 `TransferAddress` (`[RBP+0x1d8]`, `+0xd09`) 가 첫 스레드 `PspCreateUserContext` 의 `Rcx` 로 간다 (그래서 첫 스레드 Win32 Start Address = 진입점, 01_sprint `[관찰]` 과 맞음).
  - 둘째 스레드에서 0 → 헤더 값 치환이 있다면 **`RtlCreateUserStack` 안**일 가능성이 높다 `[추측]`: 지도 `_edges` 에 이 함수가 `RtlImageNtHeader` (`+0x202` / ret `+0x207`) 를 부르는 줄이 있다 (그 밖에 `ZwAllocateVirtualMemory` `+0x170`, `+0x1c6`, `ZwSetInformationProcess` `+0x104`, `ZwFreeVirtualMemory` `+0x28d`). **본문은 안 읽었다** (요청서 경계: "그 함수 하나까지만"). → "정적으로는 못 닫음", 아래 "후속" 참고.
- 지금 우리: "첫 스레드 사슬 (`MmCreateTeb`·`RtlCreateUserStack`) 재사용" = **맞음** (둘 다 같은 함수 `PspAllocateThread` 안 같은 자리 `+0x5ea`·`+0x62c`, 이름도 같다). "스택 크기 0 → exe 헤더" = **아직 `[추측]`**: 커널의 `NtCreateThreadEx` 쪽엔 그 줄이 없고, 첫 스레드는 `NtCreateUserProcess` 가 따로 하니 "둘째 스레드도 같다" 는 근거가 안 된다.

**첫 스레드 길과 비교** (질문 2 전체 요약):

| 항목 | 첫 스레드 (`NtCreateUserProcess`) | 둘째 스레드 (`NtCreateThreadEx`) |
|---|---|---|
| CONTEXT 만드는 곳 | `PspCreateUserContext` (+0xd1d/+0xd22) | `NtCreateThreadEx` 인라인 (+0x1d3~+0x271), **같은 값 세트** |
| `Rip` | `PsNtdllExports+0x10` (+0xd10) | 같은 칸 (+0x1bf) |
| `Rcx` | 이미지 `TransferAddress` (+0xd09) | StartRoutine (인자 5) |
| 스택 크기 | 이미지 정보 (reserve 최소 0x40000) | 사용자 값 그대로 |
| 스택·TEB 잡기 | `PspAllocateThread +0x5ea/+0x62c` | **같음** |
| `PspAllocateThread` 부르는 곳 | `NtCreateUserProcess +0xe57/+0xe5c` | `PspCreateThread +0x27e/+0x283` |
| `PspInsertThread` 부르는 곳 | `NtCreateUserProcess +0x1015/+0x101a` | `PspCreateThread +0x2e5/+0x2ea` |
| 만들기 플래그 `0x20`·`0x40` | `NtCreateUserProcess +0xdfd` 가 늘 켬 (0x60) | `PspCreateThread` 가 **안 만든다** |
| 프로세스 락 (프로세스+0x1c8) | 호출자가 잡고 들어옴 (flags `0x20`) | `PspInsertThread +0x8e` (`KeAbPreAcquire`) 가 **직접 잡음** |
| `PspBuildCreateProcessContext` 셋째 인자 | 0 (+0x28c) | 1 (+0xd5) |

### 3. `CreateFlags`

**(a) 허용 마스크 `0x7f` (비트 0~6), 나머지는 거절.**
- `NtCreateThreadEx +0x70 TEST R13D,0xffffff80` → `+0x77 JNZ` → 목적지 `+0x303` (`MOV EAX,0xc00000f5`, `+0x308 JMP +0x2df` 로 끝). `0xC00000F5` 는 "일곱째 인자가 나쁨" (`STATUS_INVALID_PARAMETER_7` 번호 순서와 맞음 `[추측: 이름]`).
- 맨 처음 검사다 (`ThreadHandle` 쓰기 시험 `+0x7d` 보다, 속성 목록 `+0xde` 보다 앞).
- 비트 이름 (`THREAD_CREATE_FLAGS_*`) 은 공개 헤더 값에 맞춘 `[추측]`: `0x1` CREATE_SUSPENDED, `0x2` SKIP_THREAD_ATTACH, `0x4` HIDE_FROM_DEBUGGER, `0x8` HAS_SECURITY_DESCRIPTOR, `0x10` LOADER_WORKER, `0x20` SKIP_LOADER_INIT, `0x40` BYPASS_PROCESS_FREEZE. **`0x8` 은 이 길에서 읽는 줄이 없다** (통과만 하고 `PspCreateThread` 가 안 본다).
- 지금 우리: 요청서에 가정 없음.

**(b) 비트 대응표 (`NtCreateThreadEx` CreateFlags → `PspCreateThread` 의 만들기 플래그 → 쓰임). `PspCreateThread +0x197~+0x47c`:**

| CreateFlags | → 만들기 플래그 | 줄 (`PspCreateThread`) | 그 뒤 쓰이는 곳 |
|---|---|---|---|
| `0x1` | `0x1` | `+0x1a5 TEST R8B,DL` → `+0x1b0` | `PspInsertThread +0x2d4` → **`KeSuspendThread +0x2e0`** (3d) |
| `0x2` | `0x2` | `+0x1b4` → `+0x44d` (`OR EAX,2`) | `PspAllocateThread +0x56e~+0x572` → `SameTebFlags 0x8`. 또 `PspInsertThread +0x9c4~+0x9d6`: 프로세스 +0x1f4 비트30 (`ProcessSelfDelete`) 이 켜진 프로세스에서만 `ETHREAD+0x5a4 \|= 0x40` (`SameThreadPassiveFlags` 비트6 `SelfTerminate`) |
| `0x4` | `0x4` | `+0x1bd` → `+0x45d` (`OR EAX,4`) | `PspAllocateThread +0x3eb/+0x3ed` → `+0xb04`: `CrossThreadFlags`(+0x5a0) 비트2 `HideFromDebugger` |
| `0x10` | `0x80` | `+0x1c6` → `+0x1cd BTS EAX,7` | `PspAllocateThread +0x576~+0x587` → `SameTebFlags 0x2000` (LoaderWorker) |
| `0x20` | `0x100` | `+0x1d7` → `+0x46d BTS EAX,8` | `PspAllocateThread +0x590~+0x5a0` → `SameTebFlags 0x4000` (SkipLoaderInit) |
| `0x40` | `0x200` | `+0x1e0` → `+0x47c BTS EAX,9` | `PspAllocateThread +0x3f3/+0x3f9` → `+0xb10`: `KTHREAD.MiscFlags`(+0x74) 비트21 `BypassProcessFreeze` |
| (SecureProcess, 비트 없음) | `0x400` | `+0x1e9` → `+0x361~+0x366 BTS EAX,0xa` (CONTEXT 가 이미 있으면 `+0x3b8` `0xC0000022`) | `PspAllocateThread +0x960` (CONTEXT 없는 갈래). test.exe 는 안 탐 `[추측]` |
| 안 만듦 | `0x20`, `0x40` | — | `0x20`: `PspInsertThread +0x77` (프로세스 락 새로 안 잡음). `0x40`: `PspAllocateThread +0x54a~+0x563` → `SameTebFlags 0x400` (InitialThread), `PspInsertThread +0x2ae` → `PspChangeProcessExecutionState`. **`NtCreateUserProcess +0xdfd` 만 켠다** |

- run54 사슬 완성: `+0x54a~+0x5a5` 의 입력 `0x40`→`0x400`, `0x2`→`0x8`, `0x80`→`0x2000`, `0x100`→`0x4000` 중 **`0x40` 은 `NtCreateThreadEx` 길에서 절대 안 켜진다.** `0x2` ← CreateFlags `0x2`, `0x80` ← `0x10`, `0x100` ← `0x20`.
- 지금 우리: 요청서에 가정 없음.

**(c) 둘째 스레드 (`CreateFlags = 0`) 의 `SameTebFlags`: 정적으로는 "커널이 쓰는 줄이 없다" 까지만 정해진다. 0x400 이 아니다.**
- 흐름: CreateFlags `0` → `PspCreateThread` 만들기 플래그 `0` (`+0x1a1 MOV [RSP+0x60],0`, 켜는 줄 전부 안 탐) → `PspAllocateThread +0x54a~+0x5a5` 에서 `R12W = 0` (+0x5a5 `[RSP+0x58]` 에 저장) → **`+0x66f TEST R12W,R12W` / `+0x673 JNZ +0x780` 안 탐** → `+0x793` (`MOV [RBX+0x17ee],R12W`, TEB `SameTebFlags` 통째 쓰기) **건너뜀**. WoW64 쪽 `+0x7c9`·`+0x7db` 도 안 감.
- `MmCreateTeb` 도 `+0x17ee` 를 안 쓴다 (`MmCreateTeb.asm` 에 `0x17ee` 줄 없음; 쓰는 칸은 `+0xc1~+0x13e` 의 `+0x20`, `+0x30`, `+0x40/+0x48`, `+0x2e8`, `+0x7d8/+0x7e0`, `+0x125a/+0x1260`, `+0x60`, `+8/+0x10/+0x1478` 등).
- 그래서 값 = 새 TEB 메모리의 첫 모양. `0` 일 것으로 본다 `[추측]` (`MiAllocateFromSubAllocatedRegion` 이 준 새 페이지가 0 이라는 가정, 한 TEB 는 `0x2000` 칸, `MmCreateTeb +0x85~+0xa7`). 이 `0` 은 **동적으로 확인** (아래 멈춤점 4).
- 첫 스레드는 flags `0x60` → `0x40` → `0x400` 이라 `+0x793` 이 타서 늘 `0x400` (run54 와 같음).
- 지금 우리: "첫 스레드 값 `0x400` 을 둘째에도 쓴다" 면 **틀림**. 둘째 스레드는 커널이 안 쓰므로 `CreateFlags` 에 `0x2`·`0x10`·`0x20` 이 없으면 `0` `[추측]`.

**(d) `CREATE_SUSPENDED` (`0x1`) 는 `PspInsertThread +0x2e0` 에서 멈춰 둔다.**
- 근거: `PspInsertThread +0x2d4 MOV EAX,[R12]` (R12 = 만들기 플래그 포인터) / `TEST R13B,AL` / `+0x2db JZ` → 비트0 이 켜지면 `+0x2e0` call, ret `+0x2e5` **`KeSuspendThread`**. 이 호출은 try 범위 `[+0x2dd,+0x2e7)` 안이고 필터 `PspInsertThread$filt$0` 는 `0xC000004A` (`STATUS_SUSPEND_COUNT_EXCEEDED`) 만 잡는다.
- 순서: `KeStartThread +0x5dd` → (목록·락) → **`KeSuspendThread +0x2e0`** → `ObInsertObjectEx +0x3e5` → … → `KeReadyThread +0x5a1`. 즉 멈춤 표시는 핸들·준비보다 **먼저**.
- **안 켜면**: 실행 가능 상태가 되는 곳은 **`PspInsertThread +0x5a1` / ret `+0x5a6` `KeReadyThread`** (정상 길 맨 끝, TEB 주소·ClientId·핸들을 호출자에게 다 써 준 **뒤**). sast/07 에서 이미 본 대로 **켜도 `KeReadyThread` 는 똑같이 불린다** (멈춤 표시를 먼저 해 둔 채 준비). 그래서 "CREATE_SUSPENDED 면 ready 를 안 한다" 가 아니라 "멈춤 표시를 먼저 한다" 다. `KeSuspendThread` 직후 스레드가 이미 `Terminated` 면 `+0x9fc KeForceResumeThread` 로 푸는 길도 있다.
- 출력이 쓰이는 순서 (4d 와 이어짐): `ObInsertObjectEx +0x3e5/+0x3ea` → TEB 주소 쓰기 `+0x40c` → ClientId 쓰기 `+0x450` → `ThreadInserted` 켜기 `+0x490 (OR.LOCK [R14+0x5a0],2)` → `PspCallThreadNotifyRoutines +0x4f5/+0x4fa` → `PspCreateObjectHandle +0x520/+0x525` → 핸들 쓰기 `+0x573` → `KeReadyThread +0x5a1/+0x5a6`.
- 지금 우리: 요청서에 가정 없음. (`THREAD_CREATE_FLAGS_CREATE_SUSPENDED = 1` `[추측]` → 비트값은 asm 로 맞다: `TEST ...,1`.)

### 4. 속성 목록 (`PS_ATTRIBUTE_LIST`) 검사

**(a) 누가, 언제: `PspBuildCreateProcessContext` 가, 스레드를 만들기 "전"에.**
- `NtCreateThreadEx +0xde` 에서 부른다 (`PspCreateThread +0x2cd` 보다 훨씬 앞, 프로세스 참조 `+0x127` 보다도 앞). `AttributeList` 가 NULL 이면 안 부른다.
- `NtCreateThreadEx` 가 직접 하는 검사는 두 개: CreateFlags 마스크 (`+0x70`), `ThreadHandle` 쓰기 시험 (`+0x7d~+0xa5`: 사용자 모드 호출일 때만 `[포인터]` 를 읽고 다시 씀. try 범위 `[+0x8e,+0xa7)`, 실패하면 착지점 `+0xa7` 의 `JMP +0x2df` 로 **예외 코드를 그대로 돌려주고 끝**).
- 속성 목록 자체의 검사는 전부 `PspBuildCreateProcessContext` 안. 그 결과(`ctx`)를 **스레드를 만드는 중에** 두 곳이 쓴다:
  - `PspAllocateThread +0x165~+0x1c8`: `ctx+0x8` 의 "받은 속성 번호 비트" 중 비트 12 (그룹 친화도), 14 (이상적 프로세서), 30 (XState) 를 본다. 30 이면 WoW64 는 `0xC000000D`, 시스템 프로세스 (`Flags3` 비트12) 는 `0xC00000BB`, 아니면 `ctx+0x1e0` 를 `KeInitThread` 로 넘김.
  - `PspInsertThread +0x1cb~+0x202`: `ctx+0x20` (TEB 주소 출력 포인터), 친화도·이상적 프로세서를 `KeStartThread` (`+0x5dd`) 에 넘김.

**(b) 크기 규칙 (전부 `0xC000000D` = 일반 "인자 나쁨", 사용자 모드일 때만 포인터 검사):**

| 검사 | 줄 (`PspBuildCreateProcessContext`) | 어기면 |
|---|---|---|
| 목록 포인터 4바이트 정렬 | `+0xf6 TEST R14B,CL` (R14=3) / `+0xf9 JNZ` | `ExRaiseDatatypeMisalignment` 예외 (+0x18b9) → 예외 코드가 상태값 |
| `TotalLength` (첫 8바이트) **≥ 0x28** | `+0x12a CMP RAX,0x28` / `+0x12e JNC +0x13a` (통과), 아니면 `+0x130 MOV EAX,0xc000000d` | `0xC000000D`. 빈 목록 (8바이트) 도 거절, 최소 항목 1개 |
| 전체 범위가 유저 영역 안 | `+0x13a~+0x168` (`TotalLength != 0x28` 일 때 정렬 + `RCX+TotalLength ≤ 0x7fffffff0000`, 넘으면 일부러 `MOV AL,[R13]` 로 접근) | 예외 (접근 위반 쯤 `[추측]`) |
| `(TotalLength − 8) & 0x1f == 0` | `+0x171 SUB RAX,8` / `+0x17a TEST AL,0x1f` / `+0x17c JZ`, 아니면 `+0x17e` | `0xC000000D`. 항목 하나 = **0x20 바이트**, 개수 = `(TotalLength−8)/0x20` |
| 항목 `Size` (`[항목+8]`) 가 속성별 정해진 값 | CLIENT_ID `0x10` (`+0x5bb`), TEB_ADDRESS `8` (`+0x52c`), GROUP_AFFINITY `0x10` (`+0xd68`), IDEAL_PROCESSOR `4` (`+0xd07`), XSTATE `8` (`+0x15c5`) | `0xC000000D` (`JNZ +0x...aed`) |
| 같은 속성 두 번 | `+0x1f8~+0x200` (`ctx+8` 에 번호 비트 겹치면) | `0xC000000D` |
| INPUT 비트 (`0x20000`) 가 있는 항목은 `ReturnLength` 칸 (`[항목+0x18]`) 이 NULL | `+0x1d9~+0x1e4` | `0xC000000D` |
| **스레드 모드 (셋째 인자 ≠ 0) 면 항목마다 THREAD 비트 (`0x10000`)** | `+0x1c1 TEST R8D,R8D` / `+0x1c6 BT RAX,0x10` / `+0x1cb JC`, 아니면 `+0x1cd MOV EDI,0xc000000d` | `0xC000000D` |

- 풀 할당 실패는 `0xC000009A` (일부 속성 갈래에서만, 스레드 속성엔 없음).
- 근거: `PspBuildCreateProcessContext.asm` 위 줄들. 속성 값 비트 (`0x10000` THREAD, `0x20000` INPUT) 이름은 공개 헤더 값에 맞춘 `[추측: 이름]`, 비트 자리는 asm 로 확인.

**(c) 모르는 속성 번호: "거절"** (무시 아님).
- 값을 상수 비교 트리 (`+0x20a`, `+0x21c`, `+0xc9c`, `+0x164d` …) 로 갈라 아는 값만 처리하고, 못 맞추면 `+0x1690` 기본 갈래로 간다:
  - `+0x1690 / +0x1695` `Feature_CreateProcessSegmentHeapEnablement__private_IsEnabledDeviceUsageNoInline` 가 0 이면 → `JZ` → `0xC000000D`.
  - 켜져 있어도 번호 `< 0x25` (`+0x16ad CMP R8W,0x25`), 값 비트 `0x80000` (`+0x16b8 BT R8,0x13`) 가 켜져 있고, `PspIsAttributeRemoved` (+0x16a8) 가 거짓이고, `PspGetAttributeValueSize`·`PspGetFullAttribute` (+0x16ce/+0x16da) 와 `Size`·전체 값이 맞아야 받는다. 하나라도 어기면 `0xC000000D`. 받으면 `+0x159f PspProcessUserCopyAttribute` 로 복사.
- 이 기능 게이트의 실제 값은 **동적** (아래). 스레드 모드에선 위 THREAD 비트 검사 (`+0x1c1`) 가 먼저라서, THREAD 비트 없는 모르는 값은 기본 갈래까지 가지도 못하고 거절된다.
- 지금 우리: "그 밖은 멈춤" = 방향은 **맞음** (커널도 거절). 단 "CLIENT_ID·TEB_ADDRESS 둘만" 은 **좁다**: 커널은 그룹 친화도·이상적 프로세서·XState 도 정식으로 받는다 (아래 5번 표). 어디까지 구현할지는 사용자 결정.

**(d) 출력 속성 (`CLIENT_ID`·`TEB_ADDRESS`) 포인터가 나쁠 때: 보통은 스레드가 만들어지기 "전"에 실패한다.**
- 시점 ①, **속성 읽을 때** (정상적인 "나쁜 포인터" 는 여기서 잡힘): `PspBuildCreateProcessContext` 가 항목을 읽을 때 사용자 모드면 출력 포인터에 **읽고 다시 쓰기** (CLIENT_ID `+0x5e6~+0x5ed` 첫·끝 바이트, TEB_ADDRESS `+0x558~+0x55f`) 와 `ReturnLength` 칸에 직접 쓰기 (`+0x5af` 에 `8`, `+0x63d` 에 `0x10` — **스레드를 만들기 전에 이미 써 버림**) 를 한다. 함수 전체가 try 범위 `[+0xee,+0x181b)` 이고 착지점 `+0x181b` (`MOV EBX,EAX` — 예외 코드를 상태로) → `+0x188e PspDeleteCreateProcessContext` 로 풀 정리 → 반환. `NtCreateThreadEx +0xe3/+0xe5 JS` 가 받아 바로 끝낸다 → **스레드 없음, 핸들 없음.** 돌려주는 값 = 예외 코드 (접근 위반 `0xC0000005` 쯤 `[추측]`, 정렬 오류 `0x80000002` 쯤 — 상위 비트가 켜져 있어 음수로 취급).
- 시점 ②, 시험 뒤에 포인터가 나빠진 경우 (경쟁): 실제 값을 쓰는 `PspInsertThread` 가 이미 스레드를 만든 **뒤**다. 각 쓰기가 try 로 싸여 있고 실패하면 예외 코드가 `R12D` 에 들어간다 (착지점에서 `MOV R12D,EAX`, `objdump` 로 확인):

| 쓰기 | 줄 | try 범위 / 필터 |
|---|---|---|
| TEB 주소 → `ctx+0x20` 포인터 | `+0x40c` | `[+0x407,+0x411)`, 항상 잡음 (필터 값 `1`) |
| ClientId (16바이트) | `+0x450` | `[+0x448,+0x456)`, 항상 잡음 |
| 스레드 핸들 → ThreadHandle | `+0x573` | `[+0x564,+0x578)`, 필터 `PspInsertThread$filt$1` = **사용자 모드 호출일 때만** 잡음 (필터 함수 `+0x12 CMP byte [RCX+0x184],1`) |

  - TEB·ClientId 실패 (`+0x435`/`+0x47a JS` → `+0x79e SeDeleteAccessState` → `+0x480` → `+0x48a JS +0xa13`): `ThreadInserted` 켜기 (`+0x490`) 를 **건너뛰고**, 락 풀기·`KeForceResumeThread`·`PspDeleteUserStack` (조각 `FUN_140b0d692`) → **`KeReadyThread +0x5a1` 은 그래도 부른다.** 이 스레드가 처음 돌 때 `PspUserThreadStartup` 이 `ThreadInserted` = 0 을 보고 `PspTerminateThreadByPointer(…,0xC000004B,TRUE)` 로 **스스로 끝낸다** (sast/10 `+0x4b`/`+0xe5`). 호출자는 실패 상태값을 받고 핸들은 없다.
  - 핸들 쓰기 실패 (`+0x55e`/`+0x598 JS +0xa4a`): 방금 만든 핸들을 `ObCloseHandle` (조각 `FUN_140b0d692+0x5c`) 로 닫고, `Terminated` 비트 켜기 + `ExitStatus` 저장 + **`KeRequestTerminationThread`** (조각 `FUN_140b0d71b+0x19`) → `KeReadyThread`. 호출자는 실패 상태값.
- 요약: 스레드는 "이미 만들어졌다가 끝나는" 길도 있지만, **정상적으로 나쁜 포인터는 만들기 전에** 막힌다. 어느 쪽이든 호출자에게는 실패가 돌아가고 핸들은 안 남는다.
- 지금 우리: 요청서에 가정 없음. 에뮬레이터에서 나쁜 포인터를 흉내 낼 땐 "만들기 전 실패 (속성 읽을 때)" 만 두면 거의 충분하다.

### 5. `PspBuildCreateProcessContext`

**스레드 쪽에서 부른다: 예.** `NtCreateThreadEx +0xde` / ret `+0xe3`, `AttributeList` 가 NULL 이 아닐 때만.

| 인자 | `NtCreateThreadEx` (스레드) | `NtCreateUserProcess +0x295` (프로세스) |
|---|---|---|
| 1 `RCX` | AttributeList (`+0xdb MOV RCX,RDI`) | 프로세스 속성 목록 (`+0x292 MOV RCX,RSI`) |
| 2 `DL` | PreviousMode (`+0xcb MOV DL,[RAX+0x232]`) | PreviousMode (`+0x28f MOV DL,R12B`) |
| 3 `R8D` | **`1`** (`+0xd5`) | **`0`** (`+0x28c XOR R8D,R8D`) |
| 4 `R9` | `&ctx` = `RBP+0x48` (`+0xd1`) | `&ctx` = `RBP+0x1a0` (`+0x285`) |

- 즉 다른 값은 **셋째 인자 하나** (스레드 1 / 프로세스 0). 둘째·넷째는 같은 종류 (넷째 `ctx` 는 둘 다 0x260 바이트 0 으로 비운 칸).
- 셋째 인자가 바꾸는 것 (`PspBuildCreateProcessContext` 가 `[RSP+0x90]` 에 저장해 쓴다):
  1. 항목마다 THREAD 비트 필수 (`+0x1c1~+0x1cd`) — **프로세스 쪽 속성 (`PARENT_PROCESS` 등) 은 이 비트가 없어서 스레드 모드에서 전부 거절.**
  2. GROUP_AFFINITY (`0x3000c`, `+0xd5f`), IDEAL_PROCESSOR (`0x3000e`, `+0xcfe`), XSTATE (`0x3001e`, `+0x15bc`) 는 이 값이 0 이 아니어야 받는다 — **스레드 전용**.
  3. 끝에서 `ctx+0x10` 의 비트0 ← (셋째 ≠ 0) (`+0x1876~+0x1882`). 이 칸을 누가 읽는지는 안 따라갔다.
- **스레드 쪽에서 실제로 탈 수 있는 속성 갈래** (값은 asm 상수. 이름은 공개 헤더 맞춤 `[추측]`):

| 값 | 이름 `[추측]` | 갈래 위치 | 하는 일 |
|---|---|---|---|
| `0x10003` | CLIENT_ID | `+0x5bb` | Size `0x10`. 출력 포인터 → `ctx+0x18` (이후 `PspCreateThread` 7번째 인자 → `PspInsertThread` 가 `+0x450` 에서 씀). `ReturnLength` 있으면 `0x10` 써 줌 |
| `0x10004` | TEB_ADDRESS | `+0x52c` | Size `8`. 출력 포인터 → `ctx+0x20` (`PspInsertThread +0x40c` 에서 씀). `ReturnLength` 에 `8` |
| `0x3000c` | GROUP_AFFINITY | `+0xd5f` | Size `0x10`, `ctx+0x140` 에 복사, `KeVerifyGroupAffinity` (+0xdb4/+0xdb9) 통과해야 함. 스레드 전용 |
| `0x3000e` | IDEAL_PROCESSOR | `+0xcfe` | Size `4` (프로세서 번호), `KeGetProcessorIndexFromNumber` (+0xd4d/+0xd52) → `ctx+0x14`, `-1` 이면 거절. 스레드 전용 |
| `0x3001e` | ENABLE_OPTIONAL_XSTATE_FEATURES | `+0x15bc` | Size `8`, 값이 공유 사용자 데이터 두 칸 (`0xFFFFF780_000003D8`, `…0708`) OR 의 부분집합이어야 함, `& 0x40000` 만 남겨 `ctx+0x1e0`. 스레드 전용 |
| 그 밖의 THREAD 비트 값 | — | `+0x1690` | 기본 갈래 (4c). 기능 게이트·번호·크기·전체 값이 다 맞아야 받음 |

- 나머지 프로세스용 갈래 (`PARENT_PROCESS`, `TOKEN`, `IMAGE_NAME`, `HANDLE_LIST` 등) 는 값에 THREAD 비트가 없어서 스레드 모드에서 **타지 않는다.**
- `ctx` 가 이후 하는 일: `PspAllocateThread`·`PspInsertThread` (4a 에 정리).
- 지금 우리: 요청서에 가정 없음. 위 표가 "스레드 길에서 받아야 하는 속성" 전체 목록이다 (구현 범위는 사용자 결정).

## 동적분석으로 확인해야 할 것

dast 요청 필요: **있음** (대기열 Q42 에 붙일 후보). 테스트 대상은 test.exe 의 `CreateThread` 로 만든 **둘째 스레드** 하나.

정적으로 못 닫은 것:
1. 둘째 스레드의 새 TEB `SameTebFlags` 실제 값 (커널이 안 쓰는 건 정적으로 확정, 값 0 은 `[추측]`) — 3c.
2. 둘째 스레드 첫 CONTEXT 실제 값 (`Rip`·`Rcx`·`Rdx`·`Rsp`·`ContextFlags`·세그먼트) — 2a, 2c, 2d.
3. **스택 크기 0 → exe 헤더** 치환이 실제로 일어나는지, 일어나면 어디서인지. `RtlCreateUserStack` 입구 인자 (`rcx`, `rdx`) 와 InitialTeb 결과를 비교 — 2d.
4. `AttributeList` 가 둘째 스레드에서 실제로 NULL 인지 아닌지, 몇 개인지 (유저 쪽 `CreateRemoteThreadEx` 가 넣는 것, 이번엔 안 봄), `Feature_CreateProcessSegmentHeapEnablement` 값.
5. test.exe 프로세스의 `SecureProcess` (`+0x170` 비트0) 가 0 인지 (CONTEXT 를 만드는 길을 타는지).
6. 보통 길에서 `KeSuspendThread +0x2e0` 가 안 걸리는지, `KeReadyThread +0x5a1` 시점.

추천 멈춤점 (call 명령 위치, 콜스택엔 ret 가 보임). **공통 조건**: 현재 프로세스 `ImageFileName` = test.exe. `NtCreateThreadEx` 는 ntdll 이 만드는 로더 워커·풀 스레드도 거치니 **둘째 스레드만** 거르려면 `r13d == 0` (CreateFlags `0`) **그리고** `poi(@rbp+0x320)` (StartRoutine) = test 의 스레드 함수 주소 (`test!<함수>` 를 심볼로 풀어 비교). 이 둘로 로더 워커 (CreateFlags 에 `0x10` 등 켜짐) 와 풀 스레드 (시작 함수가 다름) 가 걸러진다 `[추측]`.

| 멈춤점 | 볼 것 |
|---|---|
| `nt!NtCreateThreadEx+0x70` | `r13d` CreateFlags, `rsi` ThreadHandle, `r15` ProcessHandle, `rdi` AttributeList (`poi(@rdi)` = TotalLength), `poi(@rbp+0x320)`·`+0x328`·`+0x340`·`+0x348` = StartRoutine·Argument·StackSize·MaximumStackSize |
| `nt!NtCreateThreadEx+0x2cd` | `rbx` = CONTEXT (`ContextFlags`, `SegCs`, `Rip`, `Rcx`, `Rdx` — `Rsp` 는 아직 0), `r14` = StackInfo (`dq @r14+8 L3`) |
| `nt!PspSetupUserStack+0x89` / `+0x8e` | `+0x89`: `rcx` 커밋, `rdx` 최대, `r8` ZeroBits. `+0x8e`: `dq @r14 L5` (InitialTeb: `+0x10` StackBase, `+0x18` StackLimit, `+0x20` AllocationBase). → 크기 0 일 때 실제 할당 크기 |
| `nt!PspSetupUserStack+0xe3` | `dq @r13+0x98 L1` = `CONTEXT.Rsp` |
| `nt!PspAllocateThread+0x661` (MmCreateTeb 뒤, `rbx` = TEB) | `dx ((nt!_TEB*)@rbx)->SameTebFlags` (커널이 쓰기 전 값) |
| `nt!PspAllocateThread+0x66f` | `r12w` (쓸 값. `0` 이면 `+0x793` 안 탐) |
| `nt!PspAllocateThread+0x6f4` (KeInitThread 호출 직전) | 같은 TEB 의 `SameTebFlags` 다시 읽기, `dq @r13+0xf8 L1` / `@r13+0x80 L1` |
| `nt!PspInsertThread+0x8e` | **걸리면 `flags & 0x20 == 0` = 첫 스레드 길이 아님** (프로세스 락을 직접 잡음). `rdx` 프로세스 이미지 이름으로 test.exe 거르기 |
| `nt!PspInsertThread+0x2e0` | 걸리면 CREATE_SUSPENDED (보통 안 걸려야 함) |
| `nt!PspInsertThread+0x5a1` | `KeReadyThread` 직전, 실행 가능 상태가 되는 시점 |

## 후속 제안 (정적)

- **`RtlCreateUserStack` (ntoskrnl, rva `0x938db0`, 738바이트) 한 개를 depth 0 으로 읽는 요청**이 필요하다. 이유: 질문 2(d) 의 "크기 0 → exe 헤더" 가 거기서 갈린다 (`RtlImageNtHeader` `+0x202`). 이번엔 요청서 경계 ("그 함수 하나까지만") 때문에 이름·지도 줄만 적고 멈췄다. 작아서 다음 번호 한 건으로 충분.
- 같이 볼 질문: 크기 0 일 때 헤더 값이 어느 칸에서 오는지 (`SizeOfStackReserve` / `SizeOfStackCommit`), 최소값·정렬 (첫 스레드 길의 `0x40000` 최소와 같은지), `R9` 의 `0x1000` 이 `ZwAllocateVirtualMemory` 의 무엇인지.

## 요청서와 다르게 한 것 (이유)

- `PspBuildCreateProcessContext` 는 요청대로 **스레드 쪽 인자로 타는 갈래만** 읽었다 (인자 `R8D` 가 쓰이는 줄, 속성 값 상수 비교 트리, 스레드 속성 갈래 5개, 기본 갈래, 끝 정리). 다른 속성 갈래 본문은 안 읽었다.
- 요청서에 없는 읽기: `PspInsertThread.asm` (depth 1 로 새로 생김, 출력 쓰기 순서·실패 길), `NtCreateUserProcess.asm` (비교용), `.pdata` 예외 표·착지점 명령 (읽기 전용). `RtlCreateUserStack`·`PspUserThreadStartup` 은 본문 안 읽음 (지도·sast/10 결과만).
- 새로 쓴 함수 이름은 모두 `_index.tsv` 에서 확인했다 (`pdb_aliases --where` 필요 없었음).

## 판정

**분석성공.** 질문 1~5 를 `.asm` 줄과 함께 답했다. 정적으로 못 닫은 것은 둘: ① 크기 0 → exe 헤더 치환 위치 (`RtlCreateUserStack` 본문, 요청서 경계라 이름까지만 — 후속 한 건 제안), ② 새 TEB `SameTebFlags` 의 값 `0` (커널이 안 쓴다까지만 정적 확정 — 동적 확인). 버전 대조는 일치.

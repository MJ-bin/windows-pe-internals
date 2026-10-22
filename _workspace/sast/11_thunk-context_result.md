# 11 thunk-context — 정적 분석 결과

- 요청서: `_workspace/sast/11_thunk-context.md` (Q7)
- 날짜: 2026-10-22 (02_sprint)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`, sha256 `d90c69cfa925d420…`), pdb guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID와 같음)
- ntoskrnl analyze: **다시 하지 않음** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 모두 있음). export 만 했다.
- 뽑은 함수 (모두 depth 0, 로그 `ms/ghidra/ntoskrnl.export.sast11{,b,c,d,e,f,g}.stdout`, SCRIPT ERROR 0)
  - 요청서 정규식 `^(PspInitializeThunkContext|PspCreateUserContext|KxContextToKframes)$` → 3개
  - 넓힌 것 13개 (이유는 맨 아래 "범위를 넓힌 이유")
  - 위치: `ms/decomp/ntoskrnl/<함수>.c`, `ms/disassemble/ntoskrnl/<함수>.asm`, 호출표 `ms/disassemble/ntoskrnl/_calls.tsv` (이번에 122줄 늘고 지워진 줄 0)
- 버전 확인: **일치**
  - `NtCreateUserProcess +0xc31 (ret +0xc36) → PspAllocateProcess`, `PspInsertThread +0x802 (ret +0x807) → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 에 있다.
  - `NtCreateUserProcess +0xd1d (ret +0xd22) → PspCreateUserContext`, `PspUserThreadStartup +0xae (ret +0xb3) → PspInitializeThunkContext` 도 요청서(run9·run15)와 같다.
  - 데이터 이름 5개 (`PsNtdllExports` 0xfc74b0, `PspSystemDlls` 0xfd8130, `NtdllExports` 0xc80220, `PspNativeSystemDllData` 0xfd8980, `NtdllExportInformation` 0xc7ff90) 의 rva 를 내가 PE 구역표로 계산한 값과 `pdb_aliases.py --where` 값이 모두 같았다.
  - PDB 칸 이름과 코드 오프셋이 서로 맞는다 (아래 "PDB 자료형으로 확인한 칸").

## 스크립트 문제

실행을 막는 문제는 없다. 읽기를 어렵게 한 점만 제안으로 적는다 (`.claude/` 는 안 고쳤다).

1. **`.asm` 에 "읽지 않은 틈" 표시가 없다** (sast/10 과 같은 증상).
   - `PspInitializeThunkContext.asm` 의 `+0x2a9 JMP` 다음이 바로 `+0x2ba` 이다. 빠진 `+0x2ab~+0x2b9` (15바이트)는 SEH `__except` 착지점이다 (`MOV EBX,EAX` / `MOV [RBP+4],EAX` / `MOV R14D,1` / `MOV RSI,[RBP+0x28]`, 그다음 `+0x2ba` 로 이어짐). 앞에서 점프해 오는 코드가 없어서 Ghidra 가 코드로 안 잡았다. 원본 바이트로 읽었다.
   - 제안: 함수 범위 안에 명령이 안 찍힌 틈을 `; GAP +0x2ab..+0x2b9 (명령 없음 — 원본 바이트로 읽을 것)` 한 줄로 표시.
   - (나머지 틈은 `INT3` 패딩뿐이었다. 이번에 읽은 함수 9개를 `objdump` 선형 훑기와 줄마다 맞춰 봤다.)
2. **`.asm` 의 전역 변수 참조가 주소 숫자뿐이다.** `[0x140fc74b8]`, `[0x140fd8130]`, `[0x140fd9a88]` 같은 줄이 이름 없이 나와서, 이번 질문의 핵심(`PsNtdllExports`, `PspSystemDlls`, `PspNotifyEnableMask`)을 알아내려면 PDB 공개 심볼 덤프(`llvm-pdbutil-14 dump -publics`)와 PE 구역표로 직접 이름을 매겨야 했다.
   - 제안: `.asm` 의 RIP 상대 메모리 참조 줄 끝에 `; PsNtdllExports+0x8` 처럼 PDB 공개 이름(+오프셋)을 붙이기.
   - `.c` 쪽도 `SystemReserved1[0xf]` (= 현재 스레드 `GS:[0x188]`, `KePopulateContinuationContext.c`) 같은 엉뚱한 이름이 나온다 (sast/10 과 같음). 흐름은 `.asm` 만 믿었다.
3. **"이 전역을 누가 읽고 쓰나" 를 찾는 도구가 없다.** `PsNtdllExports` 를 **쓰는** 곳은 코드 참조로는 안 나온다. 부팅 때 `PspInitializeSystemDlls` 가 **표(`NtdllExports`) 안의 8바이트 포인터**로 간접 기록하기 때문이다. 그래서 원본 바이트로 (가) RIP 상대 참조, (나) `CALL` 목적지, (다) 8바이트 절대 주소를 훑는 임시 스크립트를 직접 썼다 (레포 밖 임시 폴더, 아래 "원본 바이트로 읽은 곳").
   - 제안: `run_ghidra.sh refs <모듈> <이름 또는 rva 범위>` — 코드 참조와 데이터→데이터 포인터 참조를 함수 이름+오프셋과 같이 뽑기.
4. **`_calls.tsv` 의 `PspInitializeThunkContext +0x35b → indirect:cfg:?`.** 대상은 `[RAX+8]` (알림 블록의 함수 칸)인데 `?` 로만 나온다. 틀린 건 아니지만 `indirect:cfg:[rax+0x8]` 처럼 레지스터+오프셋을 적으면 더 읽기 쉽다.

## 원본 바이트로 읽은 곳과 출처 (Ghidra 함수가 아니거나 `.asm` 에 빠진 곳)

모두 `ms/bin/.../System32/ntoskrnl.exe` 에서 rva 범위를 잘라 읽었다 (바이너리를 고치지 않음). 임시 스크립트는 레포에 안 넣었다 (위치: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/3f4d15aa-16f7-4279-8177-858e012325c7/scratchpad/s11/`).

| 읽은 곳 | rva | 어떻게 | 이유 |
|---|---|---|---|
| `PspInitializeThunkContext` 의 SEH 착지점 | `0x910980~0x9109b0` | `objdump -D -b binary -m i386:x86-64 -M intel` | `.asm` 에 빠진 `+0x2ab~+0x2b9` |
| `PsQuerySystemDllInfo` (40바이트) | `0x90d3c8` | 같은 방식 (바이트 `48 63 c1 48 8d 0d … c3`) | 반환값이 `PspSystemDlls[k]+8` 인지 (Ghidra 함수이긴 하나 `.asm` 안 뽑음) |
| `NtdllExportInformation` (8바이트 포인터·카운트 표) | `0xc7ff90~` | 파이썬으로 qword 읽기 | 종류(0..5)별 {표 주소, 항목 수} |
| `NtdllExports` (항목 19개: {이름 문자열 주소, 기록할 칸 주소}) | `0xc80220~0xc80350` | 같은 방식 + 이름 문자열 읽기 (`LdrInitializeThunk` 문자열 rva `0xc87998`) | 커널이 ntdll 어느 이름을 찾는지 |
| `NtdllWowX86Exports` (항목 8개) | `0xc801a0~` | 같은 방식 | WoW64 용 표가 따로 있는지 |
| `PspSystemDlls`(포인터 6칸), `PspNativeSystemDllData` | `0xfd8130`, `0xfd8980` | 같은 방식 | 파일 속 처음 값 (`PspSystemDlls[0]` = `PspNativeSystemDllData`) |
| 문자열 `\SystemRoot\System32\ntdll.dll` (UTF-16) | `0xb53370` | 같은 방식 | `PspNativeSystemDllData` 의 경로 칸 |
| 데이터 참조 훑기 | 실행 구역 전부 | RIP 상대 `disp32` 와 8바이트 절대 주소 검색 | `PsNtdllExports`/`PspSystemDlls`/`NtdllExportInformation` 의 읽는 곳·쓰는 곳 |
| `CALL`(E8) 목적지 훑기 | 실행 구역 전부 | 같은 방식 | `PspInitializeSystemDlls` 의 부르는 곳 등 |

- 이름 붙이기: PDB 공개 심볼 덤프(`llvm-pdbutil-14 dump -publics ms/sym/ntoskrnl.exe.pdb`, 오늘 임시 폴더에 받아 둔 것)의 `구역:오프셋` 을 PE 구역표 가상 주소에 더해 rva 로 바꿨다.
- 이름↔자료형 연결 근거: 공개 PDB 에는 전역 변수의 자료형이 없다. 그래서 `PsNtdllExports` ↔ `_PS_NTDLL_EXPORTS` 는 (가) 크기(공개 심볼 간격 `0xfc74b0~0xfc7558` = 168바이트 = `sizeof(_PS_NTDLL_EXPORTS)`), (나) `NtdllExports` 표 19개 항목의 {이름 문자열 ↔ 기록 칸 오프셋} 이 `_PS_NTDLL_EXPORTS` 의 멤버 이름·오프셋과 **19개 모두 일치**하는 것으로 이었다 `[정적]`.
- `.asm` 명령 이름을 같은 주소의 `objdump` 와 맞춰 봤다: `PspInitializeThunkContext` 222/224, `PspCreateUserContext` 17/17, `KxContextToKframes` 275/279, `PspSetContext` 230/234, `PspInitializeSystemDlls` 58/58, `PspLookupEntryPoint` 24/24, `KiInitializeContextThread` 246/250, `PspGetContext` 210/213, `PspSetupUserStack` 100/103 (모자란 줄은 `INT3` 패딩과 위 SEH 착지점뿐).

### PDB 자료형으로 확인한 칸 (`[정적]`, `llvm-pdbutil-14 dump -types ms/sym/ntoskrnl.exe.pdb`)

- `_CONTEXT` (크기 0x4d0): `ContextFlags +0x30`, `MxCsr +0x34`, `SegCs +0x38`, `SegDs +0x3a`, `SegEs +0x3c`, `SegFs +0x3e`, `SegGs +0x40`, `SegSs +0x42`, `EFlags +0x44`, `Rax +0x78`, **`Rcx +0x80`**, **`Rdx +0x88`**, `Rbx +0x90`, **`Rsp +0x98`**, `Rbp +0xa0`, `R8 +0xb8` … `R10 +0xc8`, **`Rip +0xf8`**, `FltSave +0x100` (`_XSAVE_FORMAT`: `ControlWord +0`, `MxCsr +0x18` → CONTEXT `+0x100`, `+0x118`), `Xmm0 +0x1a0`.
- `_KTRAP_FRAME` (0x190): `MxCsr +0x2c`, `Rax +0x30`, **`Rcx +0x38`**, **`Rdx +0x40`**, `R8 +0x48` … `R11 +0x60`, `Rbp +0x158`, **`Rip +0x168`**, `SegCs +0x170`, `EFlags +0x178`, **`Rsp +0x180`**, `SegSs +0x188`. `_KEXCEPTION_FRAME` (0x140): `Rbx +0x100`, `Rdi +0x108`, `Rsi +0x110`, `R12~R15 +0x118~+0x130`.
- **`_PS_NTDLL_EXPORTS` (168바이트)**: `LdrSystemDllInitBlock +0x0`, **`LdrInitializeThunk +0x8`**, **`RtlUserThreadStart +0x10`**, `RtlUserFiberStart +0x18`, `KiUserExceptionDispatcher +0x20`, `KiUserApcDispatcher +0x28`, `KiUserCallbackDispatcher +0x30`, `KiRaiseUserExceptionDispatcher +0x40`, … `LdrHotPatchNotify +0xa0`.
- `_SECTION_IMAGE_INFORMATION`: **`TransferAddress +0x0`**, `ImageFlags +0x33`, 크기 0x40.
- `_KTHREAD`: `InitialStack +0x28`, `TrapFrame +0x90`, `ApcState +0x98` (`Process +0x20` → +0xb8), `Process +0x220`, `SpecialApcDisable +0x1e6`, 플래그 `+0x74` 의 bit20 = `CetUserShadowStack`.
- `_KPROCESS` (= `_EPROCESS` 앞부분): **`InstrumentationCallback +0x168`**. `_EPROCESS`: `UniqueProcessId +0x1d0`, `SectionBaseAddress +0x2b0`, **`Peb +0x2e0`**, **`WoW64Process +0x310`**, `MitigationFlags2 +0x754` 의 bit14 = `CetUserShadowStacks`.
- `_ETHREAD`: `Cid +0x508` (`UniqueThread` = +0x510), `StartAddress +0x4e0`, `Win32StartAddress +0x560`.
- **PDB 에 없는 이름** (칸 오프셋만 쓴다): `PspSystemDlls[k]` 가 가리키는 구조체 (공개 심볼로 `PspNativeSystemDllData` 만 있음), `_INITIAL_TEB`. `CONTEXT_*` 비트 값은 SDK 매크로라 PDB 에 없다 → 비트가 하는 일은 코드(`PspSetContext`)로 확인했다.

## 질문별 답 [정적]

### 질문 1. 트랩 프레임 `Rip` 에 `LdrInitializeThunk` 를 넣는 곳

- **답: `PspInitializeThunkContext` 는 트랩 프레임에 직접 쓰지 않는다.** 새 `CONTEXT`(로컬 `ctx2`)를 만들어 `Rip = LdrInitializeThunk` 를 적고 `PspSetContextThreadInternal` 에 넘긴다. 그 안쪽 **`PspSetContext +0x61~+0x68`** (`MOV RCX,[RBX+0xf8]` → `MOV [RDI+0x168],RCX`) 가 `CONTEXT.Rip` → `_KTRAP_FRAME.Rip`(+0x168) 로 옮긴다.
- **값의 출처:** `+0x1a1 MOV R8,[0x140fc74b8]` = 전역 **`PsNtdllExports`(rva `0xfc74b0`, 형 `_PS_NTDLL_EXPORTS`) 의 `LdrInitializeThunk` 칸 (+0x8)**. `+0x1b2 MOV [RBP+0x1c8],R8` 로 `ctx2.Rip`(`ctx2` = `RBP+0xd0`, 크기 0x4d0, `+0xff~+0x113` 에서 0 으로 채움) 에 적는다.
- **트랩 프레임으로 가는 길 (`ctx2` → 트랩 프레임, 현재 스레드):**
  - `PspInitializeThunkContext +0x2d6` (ret `+0x2db`) `PspSetContextThreadInternal(스레드, ctx2, 0, 1, 2)`
  - → `PspSetContextThreadInternal +0x131` (ret `+0x136`) `PspGetSetContextSpecialApc` — 대상이 **현재 스레드**(`+0xf7 CMP R13,R14`, 같으면 `+0xfa` 에서 안 뛰고 계속)면 APC 를 만들지 않고 이 루틴을 직접 부른다. 다른 스레드면 `+0x2a9 KeInitializeApc`/`+0x2bf KeInsertQueueApc` 로 APC 를 걸고 `+0x2dd` 에서 기다린다.
  - → `PspGetSetContextSpecialApc +0x64` (ret `+0x69`) `PspGetSetContextInternal` — `SystemArgument1` 이 1(쓰기)이면 `+0x370` 갈래로 가서 **`+0x3ea` (ret `+0x3ef`) `PspSetContext(트랩 프레임, 비휘발 레지스터 포인터 표, ctx2, 모드)`**. 0(읽기)이면 `+0x2ee` `PspGetContext`.
  - 트랩 프레임 = `[현재 스레드 +0x28 (InitialStack)] - 0x190` (`PspGetSetContextInternal +0xa4 MOV RDI,[R15+0x28]`, `+0xb2 ADD RDI,-0x190`). run15 의 "트랩 프레임 = InitialStack-0x190" 과 같다.
- **같이 바꾸는 칸** (모두 `ctx2` 에 적는 줄 → `PspSetContext` 에서 트랩 프레임으로 옮기는 줄):

  | `ctx2` 칸 | 넣는 값 | `PspInitializeThunkContext` 줄 | 트랩 프레임 칸 (`PspSetContext` 줄) |
  |---|---|---|---|
  | `Rip +0xf8` | `LdrInitializeThunk` (`PsNtdllExports+0x8`) | `+0x1a1`, `+0x1b2` | `Rip +0x168` (`+0x61~+0x68`) |
  | `Rsp +0x98` | `R15 - 0x28` (저장본 바로 아래, 그림자 공간 0x20 + 돌아올 주소 칸 8) | `+0x187 AND R15,-0x10`, `+0x18b LEA RAX,[R15-0x28]`, `+0x18f MOV [RBP+0x168],RAX` | `Rsp +0x180` (`+0x6f~+0x76`) |
  | `Rcx +0x80` | `R15` = **유저 스택에 저장해 둔 원래 CONTEXT 의 주소** | `+0x1b9 MOV [RBP+0x150],R15` | `Rcx +0x38` (`+0xdc~+0xe3`) |
  | `Rdx +0x88` | `[ [PspSystemDlls] + 0x20 ]` — `PspSystemDlls[0]`(= `PspNativeSystemDllData`) 칸 `+0x20` (질문 4 참고) | `+0x196 MOV RAX,[0x140fd8130]`, `+0x19d MOV RCX,[RAX+0x20]`, `+0x1c0 MOV [RBP+0x158],RCX` | `Rdx +0x40` (`+0xe7~+0xee`) |
  | `ContextFlags +0x30` | `0x10000b` | `+0x1a8` | 어느 칸을 옮길지 정함 (아래) |
  | `SegCs +0x38` / `SegSs +0x42` | `0x33` / `0x2b` | `+0x1e7 LEA EAX,[RCX+8]`+`+0x1ea`, `+0x1e0` | `SegCs +0x170` = `0x33`(ctx 의 CS 가 `0x33` 이면, `+0x95~+0xa2`), `SegSs +0x188` = `0x2b` (`+0x8c`) |
  | `SegDs/Es/Fs/Gs` | `0x2b/0x2b/0x53/0x2b` | `+0x1cc`, `+0x1d6` | (이 `ctx2` 플래그엔 SEGMENTS 가 없어 트랩 프레임으론 안 감) |
  | `MxCsr +0x34`, `FltSave.ControlWord +0x100`, `FltSave.MxCsr +0x118` | `0x1f80`, `0x27f`, `0x1f80` | `+0x1f6`, `+0x1fc~+0x201`, `+0x208` | FP 레지스터 xmm0~15 는 0 으로, MxCsr 는 `PspSetContext +0x2a7` 이후 |
  | `EFlags +0x44` | 안 적음 (0) | - | 사용자 모드면 `(EFlags & 0x210dd5) | 0x200` → `0x200` (`+0x3b~+0x5a`). IF 만 켜짐 |
  | 나머지 (`Rax`, `R8`~`R15`, `Rbx`, `Rbp`, `Rsi`, `Rdi`) | 0 | `+0xff` memset | 비휘발은 `PspGetSetContextInternal` 이 커널 스택을 `RtlVirtualUnwind2`(`+0x298`)로 풀어 가며 찾아 둔 포인터 표(`+0xcc~+0x2b3`)를 통해 들어감 |

  - `0x10000b` 가 하는 일은 `PspSetContext` 의 비트 검사로 읽었다: bit0(`0x100001`)이면 Rip·Rsp·EFlags·CS·SS (`+0x28~+0xc0`), bit1(`0x100002`)이면 Rax~R11 + 비휘발 (`+0xc2~+0x1a3`), bit3(`0x100008`)이면 xmm0~15 (`+0x1b8~+0x2b3`). 이름(CONTROL/INTEGER/FLOATING_POINT)은 알려진 SDK 값이라 `[추측]`.
- **다른 곁가지 (질문 범위 밖이지만 같은 함수 안):**
  - **계측 콜백**: `+0x20e MOV RAX,[RSI+0xb8]` (현재 프로세스) / `+0x215 MOV RCX,[RAX+0x168]` = `_KPROCESS.InstrumentationCallback`. 0 이 아니면 `ctx2.Rip` 를 이 값으로 바꾸고 `ctx2.R10`(+0xc8)에 `LdrInitializeThunk` 를 넣는다 (`+0x221`, `+0x228`). 보통 0 이라 위 표대로 간다 `[정적: 조건]`, 실제 값은 동적.
  - **CET**: `+0x69~+0x80` 에서 현재 스레드 `+0x74` 의 bit20 (`CetUserShadowStack`)이 켜져 있으면 저장 CONTEXT 에 XSTATE 비트(`+0x40`)와 기능 마스크 `0x800` 을 더한다. 그리고 `+0x295` 에서 `KePopulateContinuationContext(원래 Rip)` 를 불러, 같은 비트가 켜진 스레드면 **원래 Rip 를 유저 그림자 스택에 한 칸 밀어 넣는다** (본문: `RDMSR 0x6a7` / `WRUSSD` / `WRMSR`). 목적은 `NtContinue` 로 되돌아갈 때 그림자 스택 검사를 맞추려는 것으로 보이나 `[추측]`.
  - **스레드 만들기 알림 콜백**: `+0x11b~+0x153` — `[PspNotifyEnableMask] & 0x10` 이면 `PspCreateThreadNotifyRoutine`(rva `0xf05740`) 64칸을 돌며 `ExReferenceCallBackBlock` 후 블록 `+0x10` 의 bit0 가 켜진 칸의 함수(`[블록+8]`)를 `(프로세스 ID, 스레드 ID, TRUE)` 로 부른다 (`+0x337~+0x360`, CFG 간접 호출). 어느 종류의 알림인지는 `[추측]` (새 스레드 자기 문맥에서 부르는 형), `PspNotifyThreadCreation`(`PspUserThreadStartup +0x9c`)과의 분담은 안 읽음.
  - **특수 APC 보류 구간**: `+0x113 DEC word [RSI+0x1e6]` (`_KTHREAD.SpecialApcDisable` 1 줄임) ~ `+0x2de ADD word [RSI+0x1e6],1`. 0 이 되고 커널 APC 목록(`+0x2e9 LEA RAX,[RSI+0x98]`, `CMP [RAX],RAX`)이 비어 있지 않으면 `+0x2f5` (ret `+0x2fa`) **`KiCheckForKernelApcDelivery`**. → 멈춤 APC 가 이 함수 안에서 막혀 있다가 **함수 끝에서** 배달될 수 있다 (질문 3 뒤 "run13·Q19·Q42 와 이어지는 것").

### 질문 2. 그 전의 값

#### 2-1. `PspCreateUserContext` 가 CONTEXT 에 넣는 것

- `PspCreateUserContext` 는 91바이트, `.asm` 17줄, 호출 0개. 인자는 `NtCreateUserProcess +0xd1d` (ret `+0xd22`) 앞에서 이렇게 채운다 (`NtCreateUserProcess.asm`).

  | 인자 | 값 | 줄 | 들어가는 CONTEXT 칸 (`PspCreateUserContext` 줄) |
  |---|---|---|---|
  | RCX | 확장 CONTEXT 버퍼 (스택에 `+0xcba LEA R13,[RSP+0x80]` 로 잡고 `+0xcdb RtlInitializeExtendedContext2` 로 초기화) | `+0xd1a` | - |
  | EDX | 1 (`R14D`, `+0x320 MOV R14D,1` 이후 안 바뀜) | `+0xd17` | bit0 이 **0 일 때만** `ContextFlags +0x30 = 0x10000b` (`+0x0~+0x5`). 지금은 1 이라 안 씀 |
  | R8 | **`[0x140fc74c0]` = `PsNtdllExports` 의 `RtlUserThreadStart` 칸 (+0x10)** | `+0xd10` | **`Rip +0xf8`** (`+0x28`) |
  | R9 | `[RBP+0x1d8]` = 로컬 구조체(`RBP+0x1a0`)의 `+0x38` = **`_SECTION_IMAGE_INFORMATION.TransferAddress`** (exe 진입점) | `+0xd09` | **`Rcx +0x80`** (`+0x2f`) |
  | 5번째 (`[RSP+0x20]`) | WoW64 가 아니면 **`EPROCESS.Peb`(+0x2e0)**, WoW64 면 `[RBP+0x28c]` (32비트 값) | `+0xce9~+0xd04` (`[RCX+0x310]` = `WoW64Process` 검사) | **`Rdx +0x88`** (`+0x36`) |

  - 그 밖에 이 함수가 적는 칸: `SegCs +0x38 = 0x33`, `SegSs +0x42 = 0x2b`, `SegDs/Es +0x3a = 0x2b002b`, `SegFs/Gs +0x3e = 0x2b0053`, `MxCsr +0x34 = 0x1f80`, `FltSave.MxCsr +0x118 = 0x1f80`, `FltSave.ControlWord +0x100 = 0x27f`.
  - **`Rsp` 는 이 함수가 안 쓴다.** (CONTEXT 는 `+0xcc7 memset` 으로 0 이다.) `Rsp` 는 `PspAllocateThread +0x5ea` (ret `+0x5ef`) `PspSetupUserStack(프로세스, CONTEXT, …)` 가 적는다: `PspSetupUserStack +0xdc MOV [R13+0x98],RAX`, 값 = `스택 정보 칸 +0x10 - 임의 오프셋 - 0x28` (임의 오프셋 = `ExGenRandom` & 0x7f ×16, 프로세스 `MitigationFlags` bit6 `StackRandomizationDisabled` 가 꺼졌을 때만, `+0xa7~+0xb7`). 칸 이름은 PDB 에 없다 (`_INITIAL_TEB` 없음) `[추측: StackBase]`.
  - `ContextFlags` 최종값: `NtCreateUserProcess +0xc47~+0xc4f` — 섹션 참조(`[RBP+0x270]`)가 있으면 `0x10000b`, 없으면 `0x10001b` (이 길은 `PspCreateUserContext` 가 아니라 `PspGetContextThreadInternal` 쪽이라 지금 길은 `0x10000b`). `EPROCESS.MitigationFlags2` bit14 (`CetUserShadowStacks`)가 켜져 있으면 `+0x100040` 을 더한다 (`+0xc5f~+0xc76`).
- **`Rcx` 값(진입점)이 오는 길 `[정적]`:**
  1. `PspAllocateProcess +0x6c` 에서 11번째 인자(= `NtCreateUserProcess` 의 `RBP+0x1a0` 로컬)를 `R15` 로 받는다 (`NtCreateUserProcess +0xbe6~+0xbed` 가 `[RSP+0x50]` 에 넣는 줄과 `PspAllocateProcess +0x6c MOV R15,[RSP+0x8c0]` 가 서로 맞는다: 7번 push + `SUB RSP,0x830` + 반환 주소 8 = 0x870, `0x870 + 0x50 = 0x8c0`).
  2. `PspAllocateProcess +0x986~+0x98a` (ret `+0x991`) `MmGetSectionInformation(섹션 객체, 4, R15+0x38)`. 이 함수는 섹션의 컨트롤 영역에 저장된 이미지 정보 0x40바이트를 호출자 버퍼로 복사한다 (`MmGetSectionInformation.asm +0x81~+0xbe`, `+0x14d~+0x158`; 마지막 8바이트는 따로). 그 첫 8바이트가 `TransferAddress`. → 호출자 `+0x38` 에 들어감.
  3. 이미지가 선호 주소와 다른 곳에 올라가면 차이만큼 더한다: `PspAllocateProcess +0x20b3~+0x20ce` (`MmInitializeProcessAddressSpace` 뒤, `[R14+0x2b0]` = `EPROCESS.SectionBaseAddress` - 컨트롤 영역 `+0x20` 칸을 `[R15+0x38]` 에 더함. 컨트롤 영역 `+0x20` 이 선호 주소라는 이름은 `[추측]`).
  4. 그 값이 `NtCreateUserProcess +0xd09` 에서 `R9` 로 `PspCreateUserContext` 에 간다.
  - 그러므로 `CONTEXT.Rcx` = exe 진입점. `PspAllocateThread +0x64e~+0x655` (`[ETHREAD+0x560] = [CONTEXT+0x80]`)가 옮겨 적은 `Win32StartAddress` 가 01_sprint 에서 본 `test!mainCRTStartup` 인 것과 맞는다.
- 반대쪽 길: `NtCreateUserProcess +0xce0 CMP [RBP+0x270],RDI` 가 0 이면 (`+0xd24`) `PspGetContextThreadInternal(+0xd39)` 로 부모 스레드 CONTEXT 를 복사한다. 이쪽은 `PspCreateUserContext` 를 안 부른다. 어느 쪽을 타는지는 동적 (Q34).
- 다른 스레드: `NtCreateThreadEx +0x1c1`, `PspWow64InitThread +0x73` 도 같은 `PsNtdllExports+0x10` 칸을 읽는다 (RIP 상대 참조 훑기 결과, 코드는 안 읽음). 그러니 `CreateThread` 로 만든 스레드도 CONTEXT.Rip = `RtlUserThreadStart` 로 시작한다고 볼 수 있다 `[추측]`.

#### 2-2. 그 CONTEXT 가 트랩 프레임으로 처음 옮겨지는 길 — run15 `[추측]` 이 맞다 `[정적]`

1. `PspAllocateThread +0x6af MOV [RSP+0x140],R13` — 시작 정보 묶음(`RSP+0x120`)의 `+0x20` 칸에 **CONTEXT 포인터**. (`+0x640` 에서 `[R13+0xf8]`→`ETHREAD.StartAddress`, `+0x64e` 에서 `[R13+0x80]`→`Win32StartAddress`. 이 묶음 `+0x18` 의 `StartContext` 도 `StartAddress` = `RtlUserThreadStart`.)
2. 이 묶음이 `KeInitThread +0x2fc` (ret `+0x301`) 로 `KiInitializeContextThread` 에 둘째 인자로 간다 (sast/10).
3. `KiInitializeContextThread +0xb5 MOV R12,[R13+0x20]` (CONTEXT) → `+0xc7~+0x14c` 로컬(`RSP+0x30`)로 0x4d0 복사 → `+0x150~+0x15f` `ContextFlags = (원래 & 6) | 0x100001` (INTEGER 비트는 남기고 CONTROL 켬, FLOATING_POINT 는 뺌) → `+0x163~+0x16e` `SegCs = 0x33`, `SegSs = 0x2b` → **`+0x1ac` (ret `+0x1b1`) `KxContextToKframes(트랩 프레임 = R15-0x190, 예외 프레임 = R15-0x2d0, 로컬 CONTEXT, 플래그, 사용자 모드=1)`**.
4. `KxContextToKframes.asm` 에서 칸이 옮겨지는 줄:

   | CONTEXT 칸 | 트랩 프레임 칸 | 줄 |
   |---|---|---|
   | `Rip +0xf8` | `Rip +0x168` | `+0x66`, `+0x6d` |
   | `Rsp +0x98` | `Rsp +0x180` | `+0x74`, `+0x7b` |
   | `Rcx +0x80` | `Rcx +0x38` | `+0xe1`, `+0xe8` |
   | `Rdx +0x88` | `Rdx +0x40` | `+0xec`, `+0xf3` |
   | `SegCs +0x38` (0x33 → 0x33, 아니면 0x23) / (SS 는 상수) | `SegCs +0x170` / `SegSs +0x188` = 0x2b | `+0x9a~+0xa7`, `+0x91` |
   | `EFlags +0x44` | `EFlags +0x178` (사용자 모드는 `(& 0x210dd5) \| 0x200`) | `+0x40~+0x5f` |

   (`PspSetContext` 와 칸 대응이 같다: 같은 일을 하는 쌍둥이 코드.)
- 따라서 **스레드가 처음 만들어진 직후 트랩 프레임은 `Rip = RtlUserThreadStart`, `Rcx = exe 진입점`, `Rdx = PEB`, `Rsp = 새 유저 스택 위 - 0x28 쯤`** 이다. `ETHREAD.StartAddress`(= `RtlUserThreadStart`) / `Win32StartAddress`(= 진입점) 와 같은 값이고, `PspInitializeThunkContext` 는 이 둘을 건드리지 않는다.
- `KxContextToKframes` 의 부르는 곳은 `KiInitializeContextThread +0x1ac` 와 `KeContextToKframes +0x78` (ret `+0x7d`) 둘 (`_calls.tsv`, `.c` 의 `// callers:`). `KeContextToKframes` 는 원본 바이트 훑기로 `KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0`, `KyRaiseException +0x18d`, `KiDispatchException +0x1b8` 등 6곳이 부른다. `KiContinue*` 두 곳은 `NtContinue` 쪽에서 CONTEXT 를 트랩 프레임으로 되돌리는 데 쓰일 수 있다 `[추측]` (이름만, 본문 안 읽음 — Q17 에서).

### 질문 3. `RtlUserThreadStart` 와의 관계 (flow.md 2.3·2.10 `[추측]` 확인)

- **답: 그렇다. 커널은 원래 CONTEXT(Rip = `RtlUserThreadStart`, Rcx = exe 진입점, Rdx = PEB, Rsp = 원래 유저 스택)를 읽어 유저 스택에 통째로 저장해 두고, 트랩 프레임은 `LdrInitializeThunk` 로 바꾼다. 저장본의 주소는 `Rcx` 로 넘긴다.**
  1. 읽기: `PspInitializeThunkContext +0x168` (ret `+0x16d`) `PspGetContextThreadInternal(스레드, R13 = 커널 버퍼, 0, 1, 0)` → `PspGetSetContextInternal +0x2ee` → `PspGetContext`. `PspGetContext +0x2f~+0x36` 이 `[트랩 프레임+0x168]` → `CONTEXT.Rip(+0xf8)`, `+0x55~+0x5c` 이 `Rsp`, `+0x9d~` 이 `Rax~R11` 을 읽어 온다. 플래그는 `0x10001b` (CONTROL+INTEGER+FLOATING_POINT+DEBUG_REGISTERS, 켜지면 +XSTATE) — `+0x8d ADD R12D,0x10001b`.
  2. 저장 위치: 원래 `Rsp` 바로 아래. `R15 = ([R13+0x98] - 길이) & ~0xf` (`+0x17a~+0x187`). 길이 = `RtlGetExtendedContextLength2` (`+0xa2`) 결과.
  3. 저장: `+0x23e~+0x25b` 로 `R15` 첫 바이트와 끝 바이트를 읽고 되쓰기(길이가 4095 이하일 때, 아니면 `+0x2a2` `ProbeForWrite`) → `+0x269 RtlInitializeExtendedContext2(R15 …)` → **`+0x280 RtlCopyContext(R15, 플래그, R13)`**. 유저 메모리 쓰기가 틀어지면 SEH 착지점(`+0x2ab`, 원본 바이트)이 상태 코드를 `EBX` 에 담아 `+0x37b` 오류 길로 간다 (`KiDispatchException` 으로 유저 예외를 올림, 예외 주소는 `LdrInitializeThunk`).
  4. 덮어쓰기: 질문 1 표. `Rip = LdrInitializeThunk`, `Rcx = R15`(저장본 주소), `Rdx = ntdll 칸`, `Rsp = R15 - 0x28`.
  5. **돌아올 주소는 안 쓴다**: 이 함수 안에 `Rsp`(= `R15 - 0x28`)가 가리키는 유저 스택 칸에 쓰는 줄이 없다 (유저 메모리를 쓰는 건 저장본 `[R15, R15+길이)` 뿐). 그래서 `LdrInitializeThunk` 시작 때 `[Rsp]` 는 "새로 받은 스택의 0" 이라고 볼 수 있다 `[추측: 값은 동적]`. → 01_sprint 의 "`LdrInitializeThunk` 는 혼자(부른 곳 없음)" 와 맞는다.
- **`LdrInitializeThunk` 가 저장본을 받는 쪽 `[정적]` (기존 export, `ms/disassemble/ntdll/LdrInitializeThunk.asm`):**
  - `+0x6 MOV RBX,RCX` (저장본 주소를 `RBX` 에 보관) → `+0x9` (ret `+0xe`) `LdrpInitialize` 호출 → `+0xe MOV DL,1` / `+0x10 MOV RCX,RBX` / **`+0x13` (ret `+0x18`) `NtContinue(저장본, TRUE)`** → 돌아오면 `+0x1a RtlRaiseStatus`.
  - `RDX`(ntdll 칸)는 `+0x0~+0x9` 에서 안 건드리고 `LdrpInitialize` 로 넘어간다 (`LdrpInitialize` 본문은 Q8).
  - 즉 **로더 일이 끝나면 `NtContinue` 가 저장본으로 실행을 옮긴다** — 저장본의 `Rip` 가 `RtlUserThreadStart` 이므로 그리로 간다. `NtContinue` 가 CONTEXT 의 `Rip` 로 실제로 돌아가는 부분(커널 `NtContinue` 본문)은 안 읽었다 `[공백]` (Q17·Q29).
- **01_sprint `[관찰]` 두 가지가 코드로 맞아떨어지는 한 문단:**
  - 커널 콜스택의 `ntdll!RtlUserThreadStart` 는 `nt!PspUserThreadStartup` **입구**에서 본 것이다 (run4: `bp nt!PspUserThreadStartup`). 그 시각 트랩 프레임은 `KiInitializeContextThread +0x1ac` 의 `KxContextToKframes` 가 적은 그대로라 `Rip = RtlUserThreadStart` 이다. `KiStartUserThreadReturn` 프레임을 풀 때(sast/10) 이 칸이 유저 쪽 자리로 읽힌다.
  - 첫 유저 멈춤이 `LdrInitializeThunk` 인 것은 시작 루틴이 `+0xae PspInitializeThunkContext` 를 부른 **뒤**에 `Rip` 칸이 `LdrInitializeThunk` 로 바뀌었기 때문이다. 원래 CONTEXT 는 버려진 게 아니라 유저 스택에 보관되어 `LdrInitializeThunk` 의 `Rcx` 로 넘어가고, 끝에 `NtContinue` 가 그걸로 다시 `RtlUserThreadStart` 로 보낸다.
  - 그래서 `ETHREAD.StartAddress`(= `RtlUserThreadStart`), `Win32StartAddress`(= `mainCRTStartup`) 는 그대로고 `Rip` 만 바뀐다. 이 함수에 "첫 스레드인지" 를 가르는 조건은 없다 (조건은 계측 콜백·CET·알림 마스크뿐, 모두 위 표에서 다룸).

### 질문 4. 커널이 `LdrInitializeThunk` 주소를 아는 방법

- **답: 부팅 때 `PspInitializeSystemDlls` 가 ntdll 의 내보내기 표에서 이름으로 찾아 전역 `PsNtdllExports` 에 채워 둔다.** 이름 문자열 `"LdrInitializeThunk"` 를 쓴다. 채운 뒤엔 모든 프로세스가 같은 값을 쓴다.
- 순서 `[정적]`:
  1. 표 정의 (INIT 구역 데이터): **`NtdllExportInformation`**(rva `0xc7ff90`, 16바이트 칸 {표 주소, 항목 수}) 의 0번 = {`NtdllExports`(`0xc80220`), 19}, 1번 = {`NtdllWowX86Exports`(`0xc801a0`), 8}, 2~5번 = 비어 있음.
  2. `NtdllExports` 의 항목 = {이름 문자열 주소, 기록할 칸 주소} 쌍 19개 (16바이트 간격). 첫 항목은 {`"LdrInitializeThunk"`(문자열 rva `0xc87998`), `PsNtdllExports+0x8`}, 둘째는 {`"RtlUserThreadStart"`, `+0x10`}. 19개 이름·칸 오프셋이 `_PS_NTDLL_EXPORTS` 멤버와 모두 일치.
  3. `PspInitializeSystemDlls`(rva `0xc43c40`, INIT 구역 190바이트) — 종류 `EBX = 0..5` 반복 (`+0x26 CMP EBX,6`): `+0x2d` (ret `+0x32`) `PsQuerySystemDllInfo(EBX)` (없으면 0, 건너뜀) → 그 종류의 표를 항목마다 (`+0x40 CMP ESI,[RDI]`) `+0x56` (ret `+0x5b`) **`PspLookupEntryPoint(시스템 DLL 정보, 이름, &결과)`** → 실패면 `+0xa0` (ret `+0xa5`) `KeBugCheckEx(0x6b, …)` → 성공이면 `+0x6b MOV [RCX],RAX` (RCX = 항목의 기록할 칸 주소). 종류 1 이면 `+0x77` `PspWow64GetSharedInformation(1)` 로 한 칸 더 채운다 (`+0x80`).
  4. `PspLookupEntryPoint`(rva `0xc43d08`, 79바이트): `+0x1b` (ret `+0x20`) **`RtlFindExportedRoutineByName`**(커널에서 본 ntdll 매핑의 내보내기 표에서 이름 찾기) → 못 찾으면 `0xc000007a` → 찾으면 `결과 = 찾은 주소 + [정보+0x18] - [정보+0x20]` (커널 쪽 매핑 주소를 유저 주소로 환산: `+0x2f~+0x3a`).
  5. 호출 위치 (원본 바이트 훑기): `PspInitializeSystemDlls` 는 `PspInitPhase2 +0x172` (ret `+0x177`) 에서 부른다. `PspLookupEntryPoint` 는 이 함수(`+0x56`)와 `PspCopyNtdllExport +0x31` 두 곳.
- **정보 칸(`PspSystemDlls`)이 채워지는 길 `[정적]`:**
  - `PspSystemDlls`(rva `0xfd8130`, 포인터 6칸)의 파일 속 처음 값은 `[0]` = `PspNativeSystemDllData`(`0xfd8980`), `[1]` = `PspWowX86SystemDllData`(`0xfd8940`), `[4]` = `PspVsmEnclaveRuntimeDllData`. 칸 구조체는 `+0x0` = 섹션 빠른 참조(`PspNativeSystemDll`), `+0x10~+0x18` = 경로 문자열 (`\SystemRoot\System32\ntdll.dll`), `+0x20` / `+0x28` 은 파일 속에선 0.
  - `PsQuerySystemDllInfo`(40바이트, 원본 바이트): `[PspSystemDlls + k*8]` 를 읽어 `+0x20` 이 0 이 아니면 `칸 + 8` 을 돌려주고 아니면 0.
  - `+0x20` / `+0x28` 은 **`PspMapSystemDll`** 이 채운다: `+0x207054 MOV [RBX+0x20],RDX` (RDX = 매핑된 ntdll 의 PE 헤더 `ImageBase`, `+0x207050` 또는 PE32 면 `[RAX+0x34]`), `+0x20705e MOV [RBX+0x28],R14` (매핑된 기준 주소). 이 갈래는 네 번째 인자(`R9D`)가 1 일 때다 (`+0x169 TEST R15D,R15D; JNZ +0x1a5`). 인자 1 로 부르는 곳은 두 곳이다 (원본 바이트로 확인): `PsLocateSystemDlls +0x84` (ret `+0x89`, 정보 `+8` 의 bit5 가 켜진 종류) 와 `PspLocateSystemDll +0x233` (ret `+0x238`, 부팅 때 파일 열기 `+0xc0 ZwOpenFile` → `+0xef MmCreateSpecialImageSection` → `+0x17e MmGetSectionInformation` 뒤). `PspInitPhase3 +0x6b`, `+0xa0` 의 두 호출은 인자 0 이다 (`PspSystemDlls[0]`, `[4]` 를 `[0x140fc6cf8]` 프로세스에 매핑). 프로세스마다 매핑할 땐 (`PsMapSystemDlls +0xb2~+0xbb`, 인자 0) 같은 기준 주소에 붙었는지만 검사한다 (`PspMapSystemDll +0x172 CMP [RBX+0x20],RAX`, 틀리면 `0xc0000001`).
  - `PspLocateSystemDll` 은 `PsLocateSystemDlls +0x9e` (ret `+0xa3`) 가 부르고, `PsLocateSystemDlls` 는 `IoInitSystemPreDrivers +0x10f5` 에서 부른다 (원본 바이트 훑기). 부팅 안에서의 앞뒤 순서는 안 따졌다 `[공백]` (이번 질문 밖).
- 그러므로 `PspInitializeThunkContext` 가 쓰는 두 값은 둘 다 부팅 때 정해진 전역이다: `Rip` ← `PsNtdllExports.LdrInitializeThunk`, `Rdx` ← `PspNativeSystemDllData` 의 `+0x20`(ntdll 헤더 `ImageBase`, 즉 ntdll 이 모든 프로세스에서 올라가는 유저 주소 `[추측: 이름]`; 이 값이 실제 `lm m ntdll` 시작과 같은지는 동적).
- INIT 구역은 부팅 뒤 버려지는 구역이라 `NtdllExports` 표와 `"LdrInitializeThunk"` 문자열은 나중엔 메모리에 없을 수 있다 `[추측]`. 런타임에 남는 건 `PsNtdllExports` 안 주소뿐이다.

### 질문 5. 표기 규칙

- `indirect:cfg:` — `PspInitializeThunkContext +0x35b` (알림 콜백, 대상은 `[블록+8]`), `KxStartUserThread` 와 달리 이번 세 함수엔 그 외 없음.
- `alias:` — `PspInitializeThunkContext +0xcc` = `alias:__chkstk+1` (`ALIASES(1): _alloca_probe`), 문서엔 `__chkstk`(스택 확장 검사). `PspSetContextThreadInternal +0x1a2` 는 `alias:IoThreadToProcess+1` (= `PsGetThreadProcess`), `+0x2dd` 는 `alias:KeWaitForMutexObject+1` (PDB 이름 `KeWaitForSingleObject`).
- `tail`/`frag:` — 이번에 읽은 함수 중 `PspMapSystemDll` 만 "같은 함수의 떨어진 범위"(`+0x207050~+0x20705b`, 크게 떨어진 `0x140b18c28`)를 갖는다. 오프셋 `+0x207054` 는 그림 라벨로 안 쓴다.
- `.c` 는 흐름 판단에 쓰지 않았다 (칸 오프셋·흐름은 `.asm` 과 PDB 자료형). 맨 위 `// callees:` 줄은 `.asm`/`_calls.tsv` 와 대조했다.

## 동적분석으로 확인해야 할 것

정적으로 못 정한 것 (Q42 와 같이 볼 것은 `[Q42]`):

1. `[Q42]` **`PspUserThreadStartup +0xae` 앞/뒤 트랩 프레임 `Rip`/`Rcx`/`Rdx`/`Rsp`**. 앞 = `RtlUserThreadStart`/진입점/PEB/원래 `Rsp`, 뒤 = `LdrInitializeThunk`/저장본 주소/ntdll 칸/원래 `Rsp-길이-0x28` 인지.
2. **저장본**: `PspInitializeThunkContext +0x16d` 에서 `R13`(커널 버퍼)의 `Rip`·`Rcx`·`Rdx`·`Rsp`·`ContextFlags`, 그리고 `+0x2d6` 에서 `RDX`(= `ctx2`)의 같은 칸. `R15`(저장 위치)와 유저에 첫 진입했을 때 `RCX` 가 같은지.
3. **`LdrInitializeThunk` 진입 때** `RCX`(저장본), `RDX`(ntdll 칸), `RSP`, `[RSP]`(돌아올 주소 칸이 0 인지), `dt ntdll!_CONTEXT @rcx` 의 `Rip`·`Rcx`.
4. `PsNtdllExports` 값: `[+0x8]` = 첫 유저 멈춤 주소 `ntdll!LdrInitializeThunk`, `[+0x10]` = `ntdll!RtlUserThreadStart`. `poi(PspSystemDlls)+0x20` 이 ntdll 시작 주소와 같은지.
5. `[Q42]` **`+0x2f5 KiCheckForKernelApcDelivery`** 가 test.exe 스레드에서 불리는지 (멈춰 둔 채 시작한 스레드가 이 함수 끝에서 잠드는지, 아니면 앞서 `PspUserThreadStartup +0x26` 에서 이미 잠들었는지 — run13·sast/10 의 마지막 `[추측]`과 이어진다). `SpecialApcDisable`(`+0x1e6`) 이 `+0x113` 앞뒤로 어떻게 바뀌는지.
6. 갈림길 값: `_KPROCESS.InstrumentationCallback`(`+0x168`, 보통 0), `_KTHREAD` `+0x74` bit20 (`CetUserShadowStack`; 켜지면 `+0x295` 가 그림자 스택에 씀, 이 VM 에서 `PspSetupUserShadowStack +0xb65`(Q38)와 같이), `PspNotifyEnableMask` bit4 (알림 콜백 루프에 들어가는지).
7. `ETHREAD.StartAddress`/`Win32StartAddress` 가 `+0xae` 앞뒤로 같은지 (정적으론 안 건드린다).
8. 이 길이 **모든 스레드**에서 같은지 (메인·`TppWorkerThread` 2개·`CreateThread` 스레드): 각 스레드의 저장본 `Rcx`(= 그 스레드의 `Win32StartAddress`).

추천 멈춤점 (모두 test.exe 일 때만 서게. 조건식은 dast 가 정한다. 커널 이름은 WinDbg 가 `PspInitializeThunkContext+0x…` 로 보일 것):

- `nt!PspInitializeThunkContext+0x16d` (`PspGetContextThreadInternal` 직후, 원래 CONTEXT 읽은 직후): `dq @r13+0xf8 L1` (Rip), `dq @r13+0x80 L2` (Rcx, Rdx), `dq @r13+0x98 L1` (Rsp), `dd @r13+0x30 L1` (ContextFlags). `dt nt!_CONTEXT @r13 Rip Rcx Rdx Rsp`.
- `nt!PspInitializeThunkContext+0x2d6` (`PspSetContextThreadInternal` 호출 직전): `RDX` = `ctx2`. `dt nt!_CONTEXT @rdx Rip Rcx Rdx Rsp R10`. 저장본 위치는 `@r15`.
- `nt!PspInitializeThunkContext+0x2db` (호출 직후): `dq (poi(@rsi+0x28)-0x190)+0x168 L1` (트랩 프레임 Rip), `+0x38 L2` (Rcx, Rdx), `+0x180 L1` (Rsp). `@rsi` = 현재 스레드.
- `nt!PspInitializeThunkContext+0x2f5` (호출될 때만 선다): 거기서 `k`.
- `nt!PspUserThreadStartup+0xae` 와 `+0xb3` (run15·sast/10 추천과 같음): 트랩 프레임 `Rip` 앞뒤. `[Q42]`
- 유저(KD 로 유저 주소 멈춤): `ntdll!LdrInitializeThunk` 입구 (`r rcx rdx rsp`, `dq @rsp L2`, `dt ntdll!_CONTEXT @rcx`), 그리고 `ntdll!LdrInitializeThunk+0x13` (`NtContinue` 호출 직전, `@rbx` 가 처음 `@rcx` 와 같은지, `dt ntdll!_CONTEXT @rbx Rip`).
- 값만 읽기: `dq nt!PsNtdllExports L3`, `ln poi(nt!PsNtdllExports+8)`, `dq poi(nt!PspSystemDlls)+0x20 L1`, `lm m ntdll`, `dd nt!PspNotifyEnableMask L1`.
- 멈춤점 주의: 이 함수는 모든 프로세스의 모든 유저 스레드가 거치므로 반드시 프로세스 조건을 붙인다 (run4 에서 쓴 `bp /p <test>` 방식).

dast 요청 필요: 위 1·2·3·5 를 Q42 와 한 번에 보는 요청서 (2.3 `[공백]` 중 "유저 첫 RIP 가 어떻게 바뀌나" 를 닫으려면 필요). 4·6·7·8 은 같은 멈춤점에서 값만 더 읽으면 된다.

## 범위를 넓힌 이유

요청서 대상 3개는 depth 0 으로 뽑았고, 아래는 질문에 답하려고 **한 함수씩** 더 뽑은 것이다 (모두 depth 0). 지도로는 안 닿는 곳이라 `_index.tsv` 와 `pdb_aliases.py --where`, `_calls.tsv`, 원본 바이트 훑기로 찾았다.

| 함수 (rva, 바이트) | 질문 | 이유 |
|---|---|---|
| `PspSetContextThreadInternal` (`0x90f6f0`, 834) | 1 | `PspInitializeThunkContext +0x2d6` 이 부름. 현재 스레드면 APC 없이 직접 처리 |
| `PspGetSetContextSpecialApc` (`0x6b7180`, 229) | 1 | 위가 부름. `PspGetSetContextInternal` 로 넘김 |
| `PspGetSetContextInternal` (`0x9b7a20`, 1712) | 1·3 | 읽기/쓰기 갈림, 트랩 프레임 위치(`InitialStack-0x190`) |
| `PspSetContext` (`0x9b84e0`, 1126) | 1 | 트랩 프레임 `Rip` 칸에 쓰는 줄이 여기 |
| `PspGetContext` (`0x9b80d0`, 1024), `PspGetContextThreadInternal` (`0x90fa40`, 736) | 3 | 원래 CONTEXT 를 읽는 쪽 (`Rip`/`Rsp` 줄만 확인) |
| `KePopulateContinuationContext` (`0x3ecab0`, 56) | 1 | `+0x295` 호출 대상 (CET 그림자 스택) |
| `KeContextToKframes` (`0x4fd260`, 236) | 2-2 | `KxContextToKframes` 의 다른 부르는 곳. 본문은 안 읽고 호출표만 봄 |
| `PspSetupUserStack` (`0x938c3c`, 363) | 2-1 | 질문이 `Rsp` 를 물음. `PspCreateUserContext` 는 `Rsp` 를 안 써서 어디서 쓰는지 |
| `MmGetSectionInformation` (`0x8a51c0`, 792) | 2-1 | `Rcx`(진입점) 값의 출처. `TransferAddress` 첫 8바이트 확인 줄만 읽음 |
| `PspInitializeSystemDlls` (`0xc43c40`, 190), `PspLookupEntryPoint` (`0xc43d08`, 79) | 4 | `PsNtdllExports` 를 채우는 곳, 이름으로 찾는 곳 |
| `PspLocateSystemDll` (`0x77e3b0`, 608) | 4 | 요청서가 이름을 적음. `PspMapSystemDll` 을 인자 1 로 부르는 곳 확인용 |

- 안 뽑은 것(이름만 적음): `PspPrepareSystemDllInitBlock`(`PsNtdllExports+0`=`LdrSystemDllInitBlock` 읽는 곳, 요청서 질문 밖), `KiInitiateUserApc`, `NtContinue`(커널), `KiContinueEx`, `KiContinuePreviousModeUser`, `PspCopyNtdllExport`, `PsLocateSystemDlls`, `IoInitSystemPreDrivers`, `PspInitPhase2/3`.
- 이미 있던 export 를 다시 읽은 것: `PspMapSystemDll`·`PsMapSystemDlls`·`KiInitializeContextThread`·`PspAllocateThread`·`PspAllocateProcess`·`NtCreateUserProcess`·`PspUserThreadStartup` (이번엔 안 다시 뽑음), ntdll 쪽 `LdrInitializeThunk.asm` (기존 export, 질문 3 의 받는 쪽 근거).

## 판정

**분석성공.** 질문 1~5 에 모두 답이 있고 근거 줄(`.asm` `+0x…`, 원본 바이트 rva, PDB 자료형)이 붙어 있다. 핵심은 다섯 가지다.

1. 트랩 프레임 `Rip`(+0x168)를 `LdrInitializeThunk` 로 쓰는 줄은 `PspInitializeThunkContext` 가 아니라 **`PspSetContext +0x61~+0x68`** 이다. 이 함수는 로컬 `ctx2` 에 `Rip = PsNtdllExports.LdrInitializeThunk(+0x8)`, `Rsp = 저장본-0x28`, `Rcx = 저장본 주소`, `Rdx = PspSystemDlls[0]+0x20` 을 채워 `PspSetContextThreadInternal` 로 넘긴다.
2. 처음 트랩 프레임 `Rip` 는 `RtlUserThreadStart` 였다: `PspCreateUserContext` 가 `CONTEXT.Rip = PsNtdllExports.RtlUserThreadStart(+0x10)`, `Rcx = SECTION_IMAGE_INFORMATION.TransferAddress`(exe 진입점), `Rdx = EPROCESS.Peb` 를 넣고 (`Rsp` 는 `PspSetupUserStack` 이 따로), `KiInitializeContextThread +0x1ac` 의 `KxContextToKframes` 가 트랩 프레임으로 옮긴다. run15 의 `[추측]` 이 코드로 확인됐다.
3. `RtlUserThreadStart` 와의 관계: 원래 CONTEXT 는 유저 스택에 저장되고 (`+0x280 RtlCopyContext`), `LdrInitializeThunk` 가 그 주소를 `RCX` 로 받아 `RBX` 에 쥐고 있다가 끝에 `NtContinue(저장본)` 를 부른다 (`ntdll!LdrInitializeThunk +0x13`). 커널 `NtContinue` 가 실제로 그 `Rip` 로 돌아가는 부분만 `[공백]`.
4. 커널은 부팅 때 `PspInitializeSystemDlls` → `PspLookupEntryPoint` 로 ntdll 내보내기 표에서 이름(`"LdrInitializeThunk"` 등 19개)으로 찾아 `PsNtdllExports` 에 채운다.
5. 이 길은 "첫 스레드만" 이 아니다 (조건 없음). 새로 보인 점: 같은 함수가 특수 APC 보류 구간 안에서 돌고, 끝(`+0x2f5`)에서 `KiCheckForKernelApcDelivery` 를 부를 수 있다 (Q19·Q42 의 "멈춤 APC 가 어디서 들어오나" 후보 하나 추가).

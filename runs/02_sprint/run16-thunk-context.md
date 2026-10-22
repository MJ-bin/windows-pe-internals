# 2026-10-22 run16 — 유저 첫 위치가 `LdrInitializeThunk` 로 바뀌는 곳: `PspCreateUserContext` 가 `RtlUserThreadStart` 로 채우고, `PspInitializeThunkContext` 가 원래 문맥을 유저 스택에 맡긴 뒤 바꿔 끼운다 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntoskrnl (`ntoskrnl.exe`, 26100.9457), pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음)
  - analyze 는 다시 안 했다. 기존 프로젝트에서 export 만.
  - 버전 대조: 일치. `NtCreateUserProcess +0xc36`, `PspInsertThread +0x807`, `NtCreateUserProcess +0xd22`, `PspUserThreadStartup +0xb3` 이 `_calls.tsv` 와 같다 (결과서).
- 받는 쪽(ntdll)은 기존 export `ms/disassemble/ntdll/LdrInitializeThunk.asm` 을 근거로 썼다 (새로 뽑지 않음).
- 뽑은 함수 (모두 depth 0)
  - 요청서 3개: `PspInitializeThunkContext` (rva `0x9106e8`), `PspCreateUserContext` (rva `0xa2e790`, 91바이트), `KxContextToKframes` (rva `0x3e9fc0`)
  - 넓힌 것 13개: `PspSetContextThreadInternal`, `PspGetSetContextSpecialApc`, `PspGetSetContextInternal`, `PspSetContext`, `PspGetContext`, `PspGetContextThreadInternal`, `KePopulateContinuationContext`, `KeContextToKframes`, `PspSetupUserStack`, `MmGetSectionInformation`, `PspInitializeSystemDlls`, `PspLookupEntryPoint`, `PspLocateSystemDll` (이유는 결과서 "범위를 넓힌 이유")
  - 이미 있던 것도 근거로 씀: `NtCreateUserProcess.asm`, `PspAllocateThread.asm`, `PspAllocateProcess.asm`, `KiInitializeContextThread.asm`, `PspUserThreadStartup.asm`, `PspMapSystemDll.asm`

## 목적

- 대기열 **Q7** (단계 1 의 마지막 항목). flow.md 2.3 `[공백]` "유저 첫 `RIP` 가 `LdrInitializeThunk` 로 정해지는 곳" 을 채운다.
  1. 트랩 프레임 `Rip`(+0x168) 에 `LdrInitializeThunk` 를 넣는 줄과 그 값의 출처.
  2. 그 전에 `Rip` 는 무엇이었나 (`PspCreateUserContext` 가 CONTEXT 에 넣는 값, `KxContextToKframes` 로 처음 옮겨지는 길 — run15 `[추측]`).
  3. `RtlUserThreadStart` 와의 관계 (2.3·2.10 `[추측]`), 01_sprint `[관찰]` 두 개가 코드로 어떻게 맞아떨어지나.
  4. 커널이 `LdrInitializeThunk` 주소를 어떻게 아나.

## 방법

- 정적: `_workspace/sast/11_thunk-context.md` — ntoskrnl, `^(PspInitializeThunkContext|PspCreateUserContext|KxContextToKframes)$` depth 0 (+ 한 함수씩 13개)
- 결과서: `_workspace/sast/11_thunk-context_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntoskrnl/{PspInitializeThunkContext,PspCreateUserContext,KxContextToKframes,PspSetContextThreadInternal,PspGetSetContextSpecialApc,PspGetSetContextInternal,PspSetContext,PspGetContext,PspGetContextThreadInternal,PspInitializeSystemDlls,PspLookupEntryPoint,KiInitializeContextThread,NtCreateUserProcess}.asm`, `ms/disassemble/ntoskrnl/_calls.tsv`
  - `ms/disassemble/ntdll/LdrInitializeThunk.asm`, `ms/disassemble/ntdll/_calls.tsv`
- `.asm` 에 없는 곳은 결과서가 **원본 바이트**로 읽었다 (`ms/bin/.../System32/ntoskrnl.exe` 의 rva 범위를 `objdump`/파이썬으로). 이 문서에서 "(원본 바이트)" 라고 붙인 곳이 그것이다.
  - `PspInitializeThunkContext` 의 SEH 착지점 `+0x2ab~+0x2b9`, `PsQuerySystemDllInfo`, 표 `NtdllExportInformation`·`NtdllExports`·`PspSystemDlls`·`PspNativeSystemDllData`, 부르는 곳 훑기 (`PspInitPhase2 +0x172` 등).
- 전역 변수 이름은 PDB 공개 심볼 + PE 구역표로 맞췄다 (`PsNtdllExports` rva `0xfc74b0`, `PspSystemDlls` `0xfd8130`, `NtdllExports` `0xc80220` 등). `pdb_aliases.py --where` 값과 같았다 (결과서).
  - `PsNtdllExports` 가 `_PS_NTDLL_EXPORTS` 형이라는 건 크기(168바이트)와 `NtdllExports` 표 19개 {이름 ↔ 칸 오프셋} 이 멤버와 모두 맞는 것으로 이었다 (공개 PDB 엔 전역 변수 형이 없음).
- 구조체 칸 이름은 **PDB 타입 레코드**로 맞췄다 (`llvm-pdbutil-14 dump -types`, 결과서). 자주 나오는 것:
  - `_CONTEXT` (0x4d0): `ContextFlags +0x30`, `Rcx +0x80`, `Rdx +0x88`, `Rsp +0x98`, **`Rip +0xf8`**
  - `_KTRAP_FRAME` (0x190): `Rcx +0x38`, `Rdx +0x40`, **`Rip +0x168`**, `SegCs +0x170`, `EFlags +0x178`, `Rsp +0x180`, `SegSs +0x188`
  - `_PS_NTDLL_EXPORTS` (168바이트): `LdrSystemDllInitBlock +0x0`, **`LdrInitializeThunk +0x8`**, **`RtlUserThreadStart +0x10`**, `RtlUserFiberStart +0x18`, `KiUserExceptionDispatcher +0x20` …
  - `_SECTION_IMAGE_INFORMATION.TransferAddress +0x0`, `_EPROCESS.Peb +0x2e0`, `_KTHREAD.InitialStack +0x28`, `_KPROCESS.InstrumentationCallback +0x168`
- 오프셋 읽는 법 (run9 ~ 15 와 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**.

## 결과

한 줄로: **처음엔 `RtlUserThreadStart` 로 채워 두고, 출발 직전에 원래 문맥을 유저 스택에 맡긴 뒤 `LdrInitializeThunk` 로 바꿔 끼운다. 로더는 끝에서 맡긴 문맥을 `NtContinue` 로 되찾는다.**

- 비유: 새 직원 책상 쪽지에 처음엔 "본업 자리(`RtlUserThreadStart`)" 가 적혀 있다. 첫 출근 직전에 관리실이 그 쪽지를 **사물함(유저 스택)에 넣어 두고**, 책상엔 "먼저 오리엔테이션실(`LdrInitializeThunk`)로" 라는 새 쪽지와 사물함 번호(`Rcx`)를 붙인다. 오리엔테이션이 끝나면 사물함 쪽지를 꺼내 본업 자리로 간다(`NtContinue`).

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv`, `ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0xd1d` | `+0xd22` | `nt!PspCreateUserContext` | [정적] (run9 과 같음) |
| `nt!PspAllocateThread` | `+0x5ea` | `+0x5ef` | `nt!PspSetupUserStack` (CONTEXT `Rsp` 를 씀) | [정적] (run11 과 같음) |
| `nt!KiInitializeContextThread` | `+0x1ac` | `+0x1b1` | `nt!KxContextToKframes` (CONTEXT → 트랩 프레임, 처음) | [정적] |
| `nt!PspUserThreadStartup` | `+0xae` | `+0xb3` | `nt!PspInitializeThunkContext` | [정적] (run15 와 같음) |
| `nt!PspInitializeThunkContext` | `+0x168` | `+0x16d` | `nt!PspGetContextThreadInternal` (원래 문맥 읽기) | [정적] |
| `nt!PspInitializeThunkContext` | `+0x280` | `+0x285` | `nt!RtlCopyContext` (유저 스택에 저장) | [정적] |
| `nt!PspInitializeThunkContext` | `+0x295` | `+0x29a` | `nt!KePopulateContinuationContext` (CET 일 때만 일함) | [정적] |
| `nt!PspInitializeThunkContext` | `+0x2d6` | `+0x2db` | `nt!PspSetContextThreadInternal` (새 문맥 쓰기) | [정적] |
| `nt!PspInitializeThunkContext` | `+0x2f5` | `+0x2fa` | `nt!KiCheckForKernelApcDelivery` (조건부) | [정적] |
| `nt!PspSetContextThreadInternal` | `+0x131` | `+0x136` | `nt!PspGetSetContextSpecialApc` (현재 스레드면 직접) | [정적] |
| `nt!PspGetSetContextSpecialApc` | `+0x64` | `+0x69` | `nt!PspGetSetContextInternal` | [정적] |
| `nt!PspGetSetContextInternal` | `+0x3ea` | `+0x3ef` | `nt!PspSetContext` (트랩 프레임에 씀) | [정적] |
| `nt!PspGetSetContextInternal` | `+0x2ee` | `+0x2f3` | `nt!PspGetContext` (읽기 쪽) | [정적] |
| `ntdll!LdrInitializeThunk` | `+0x9` | `+0xe` | Ghidra 이름 `LdrpInitialize` (rva `0xe1448`) | [정적] (아래 "정적과 동적이 다른 곳") |
| `ntdll!LdrInitializeThunk` | `+0x13` | **`+0x18`** | `ntdll!NtContinue` | [정적] |
| `ntdll!LdrInitializeThunk` | `+0x1a` | `+0x1f` | `ntdll!RtlRaiseStatus` (`NtContinue` 가 돌아왔을 때만) | [정적] |
| `nt!PspInitPhase2` | `+0x172` | `+0x177` | `nt!PspInitializeSystemDlls` (부팅 때) | [정적] (원본 바이트 훑기) |
| `nt!PspInitializeSystemDlls` | `+0x56` | `+0x5b` | `nt!PspLookupEntryPoint` | [정적] |
| `nt!PspLookupEntryPoint` | `+0x1b` | `+0x20` | `nt!RtlFindExportedRoutineByName` | [정적] |

### 정적 — ① 처음 문맥: `PspCreateUserContext` 가 채우는 값

`NtCreateUserProcess +0xd1d` 앞에서 인자를 채우고, `PspCreateUserContext`(91바이트, 호출 없음) 가 CONTEXT 칸에 그대로 적는다.

| CONTEXT 칸 | 값 | 값의 출처 (`NtCreateUserProcess` 줄) | 적는 줄 (`PspCreateUserContext`) |
|---|---|---|---|
| **`Rip +0xf8`** | **`RtlUserThreadStart`** | `+0xd10 MOV R8,[0x140fc74c0]` = `PsNtdllExports+0x10` | `+0x28` |
| **`Rcx +0x80`** | **exe 진입점** (`_SECTION_IMAGE_INFORMATION.TransferAddress`) | `+0xd09 MOV R9,[RBP+0x1d8]` | `+0x2f` |
| **`Rdx +0x88`** | **PEB** (`EPROCESS.Peb +0x2e0`, WoW64 면 32비트 값) | `+0xce9 ~ +0xd04` | `+0x36` |
| `SegCs`/`SegSs` 등 | `0x33` / `0x2b`, `MxCsr` `0x1f80` | — | `+0x11 ~ +0x51` |

- `Rsp` 는 이 함수가 안 쓴다. `PspSetupUserStack +0xdc` 가 적는다 (`PspAllocateThread +0x5ef` 에서 부름). 값은 새 유저 스택 위쪽 − 임의 오프셋 − 0x28 (칸 이름은 PDB 에 없어 `[추측: StackBase]`).
- `Rcx`(진입점) 값의 길: `PspAllocateProcess +0x98c` (ret `+0x991`) `MmGetSectionInformation` 이 섹션의 이미지 정보(첫 8바이트 = `TransferAddress`)를 복사 → 이미지가 다른 곳에 올라가면 `+0x20b3 ~ +0x20ce` 에서 차이만큼 더함 → `NtCreateUserProcess +0xd09` (결과서 2-1).
- 그래서 `PspAllocateThread` 가 옮겨 적는 값(run11)은 `_ETHREAD.StartAddress` = `RtlUserThreadStart`, `Win32StartAddress` = exe 진입점이다 `[정적]`. run5 `[관찰]` "Win32 Start Address = `test!mainCRTStartup`" 과 맞는다.
- 갈림길: `NtCreateUserProcess +0xce0` 에서 섹션 참조가 0 이면 `PspCreateUserContext` 대신 `PspGetContextThreadInternal` (ret `+0xd3e`) 로 부모 스레드 문맥을 복사한다. 어느 쪽을 타는지는 동적 (Q34).

### 정적 — ② 처음 트랩 프레임에 옮기기: `KxContextToKframes` (run15 `[추측]` → `[정적]`)

- `PspAllocateThread +0x6af` 가 시작 정보 묶음 `+0x20` 칸에 CONTEXT 포인터를 넣고, 그 묶음이 `KeInitThread +0x2fc` → `KiInitializeContextThread` 로 간다.
- `KiInitializeContextThread +0xb5` 가 CONTEXT 를 꺼내 로컬로 복사, `ContextFlags`·`SegCs`·`SegSs` 를 손본 뒤 **`+0x1ac` (ret `+0x1b1`) `KxContextToKframes(트랩 프레임 = InitialStack − 0x190, …, 사용자 모드)`**.
- `KxContextToKframes` 가 옮기는 칸: `Rip +0xf8 → +0x168` (`+0x66`/`+0x6d`), `Rsp +0x98 → +0x180` (`+0x74`/`+0x7b`), `Rcx +0x80 → +0x38` (`+0xe1`/`+0xe8`), `Rdx +0x88 → +0x40` (`+0xec`/`+0xf3`).
- 그래서 **스레드가 만들어진 직후 트랩 프레임은 `Rip = RtlUserThreadStart`, `Rcx = exe 진입점`, `Rdx = PEB`** 다 `[정적]`.

### 정적 — ③ 바꿔 끼우기: `PspInitializeThunkContext` (`PspUserThreadStartup +0xb3`)

코드 순서대로 (자리는 `PspInitializeThunkContext` 기준):

| # | 자리 | 하는 일 |
|---|---|---|
| 1 | `+0x113` ~ `+0x2de` | 특수 APC 를 잠깐 막는 구간 (`_KTHREAD.SpecialApcDisable` −1 … +1) |
| 2 | `+0x168` (ret `+0x16d`) | `PspGetContextThreadInternal` 로 **지금 트랩 프레임의 원래 문맥**(Rip = `RtlUserThreadStart`, Rcx = 진입점 …)을 커널 버퍼에 읽어 옴. 안쪽 `PspGetContext +0x2f~+0x36` 이 트랩 프레임 `+0x168` → CONTEXT `+0xf8` |
| 3 | `+0x17a ~ +0x187` | 맡길 자리 `R15` = (원래 `Rsp` − 문맥 길이) 를 16바이트로 맞춤 — **원래 유저 스택 바로 아래** |
| 4 | `+0x18b ~ +0x208` | 새 문맥 `ctx2` 채우기 (아래 표) |
| 5 | `+0x20e ~ +0x228` | 계측 콜백(`_KPROCESS.InstrumentationCallback`)이 0 이 아니면 `ctx2.Rip` 를 그 값으로, `R10` 에 `LdrInitializeThunk` (보통 0 `[추측]`, 값은 동적) |
| 6 | `+0x23e ~ +0x280` (ret `+0x285`) | 유저 메모리 검사 → **`RtlCopyContext` 로 원래 문맥을 유저 스택 `R15` 에 맡김** (쓰기 실패면 SEH 착지점 `+0x2ab` (원본 바이트) → 오류 길) |
| 7 | `+0x295` (ret `+0x29a`) | `KePopulateContinuationContext(원래 Rip)` — CET 그림자 스택이 켜진 스레드면 원래 `Rip` 를 그림자 스택에 한 칸 넣음. 목적은 `[추측]` (`NtContinue` 로 돌아갈 때 검사 맞추기) |
| 8 | `+0x2d6` (ret `+0x2db`) | **`PspSetContextThreadInternal(현재 스레드, ctx2, …)`** → 트랩 프레임에 씀 (아래 사슬) |
| 9 | `+0x2f5` (ret `+0x2fa`) | APC 막기를 풀었을 때 커널 APC 가 걸려 있으면 `KiCheckForKernelApcDelivery` (조건부) |

- 같은 함수 `+0x11b ~ +0x153`, `+0x337 ~ +0x360` 에 스레드 만들기 알림 콜백 루프(`PspNotifyEnableMask` bit4 일 때, CFG 간접 호출)도 있다. 어느 알림인지·`PspNotifyThreadCreation`(`PspUserThreadStartup +0xa1`)과 나누는 방식은 안 읽었다 `[추측]`.

새 문맥 `ctx2` (로컬 `RBP+0xd0`, 0 으로 채운 뒤):

| `ctx2` 칸 | 넣는 값 | 줄 | → 트랩 프레임 칸 (`PspSetContext` 줄) |
|---|---|---|---|
| **`Rip`** | **`LdrInitializeThunk`** = `PsNtdllExports+0x8` | `+0x1a1 MOV R8,[0x140fc74b8]`, `+0x1b2` | `+0x168` (`+0x61`/**`+0x68`**) |
| **`Rcx`** | **`R15` = 유저 스택에 맡긴 원래 문맥의 주소** | `+0x1b9` | `+0x38` (`+0xdc`/`+0xe3`) |
| `Rsp` | `R15 − 0x28` (맡긴 문맥 바로 아래) | `+0x18b`, `+0x18f` | `+0x180` (`+0x6f`/`+0x76`) |
| `Rdx` | `PspSystemDlls[0]` 칸 `+0x20` (ntdll 헤더 `ImageBase` `[추측: 이름]`) | `+0x196`, `+0x19d`, `+0x1c0` | `+0x40` (`+0xe7`/`+0xee`) |
| `ContextFlags` | `0x10000b` (어느 칸을 옮길지) | `+0x1a8` | — |
| `EFlags` | 안 적음 → 사용자 모드라 `0x200` (IF 만) | — | `+0x178` |

- 트랩 프레임까지의 사슬 (현재 스레드라 APC 없이 바로):
  - `PspSetContextThreadInternal +0xf7 CMP R13,R14` — 대상이 현재 스레드면 `+0x131` (ret `+0x136`) `PspGetSetContextSpecialApc` 를 직접 부름 (다른 스레드면 APC 를 걸고 기다림)
  - → `+0x64` (ret `+0x69`) `PspGetSetContextInternal` — 트랩 프레임 = `[스레드 +0x28] − 0x190` (`+0xa4`, `+0xb2`), 쓰기면 `+0x3ea` (ret `+0x3ef`) `PspSetContext`
  - → **`PspSetContext +0x61 ~ +0x68` 이 `ctx2.Rip` → `_KTRAP_FRAME.Rip`(+0x168)**. 즉 `LdrInitializeThunk` 를 실제로 칸에 쓰는 줄은 `PspInitializeThunkContext` 가 아니라 여기다.
- **"첫 스레드만" 을 가르는 줄이 없다** `[정적]`. 갈림 조건은 계측 콜백·CET·알림 마스크뿐이다. run15 "`PspInitializeThunkContext` 는 모든 스레드" 와 합치면 **모든 유저 스레드가 같은 바꿔 끼우기를 거친다** `[정적]`.
- **돌아올 주소 칸은 안 쓴다**: 유저 메모리에 쓰는 건 맡긴 문맥 `[R15, R15+길이)` 뿐이고, 새 `Rsp`(`R15 − 0x28`)가 가리키는 칸엔 쓰는 줄이 없다 `[정적]`. 그 칸 값은 `[추측: 새 스택이라 0]`, 동적.
- `ETHREAD.StartAddress`·`Win32StartAddress` 는 건드리지 않는다 `[정적]` — 트랩 프레임 `Rip` 만 바뀐다.

### 정적 — ④ 받기: `ntdll!LdrInitializeThunk` (`LdrInitializeThunk.asm`)

| 자리 | 명령 | 뜻 |
|---|---|---|
| `+0x6` | `MOV RBX,RCX` | 맡긴 문맥 주소(커널이 `Rcx` 로 넘긴 것)를 `RBX` 에 쥐고 있음 |
| `+0x9` (ret `+0xe`) | `CALL` | 로더 본체 (Ghidra 이름 `LdrpInitialize`, run7 관찰 이름은 `LdrpInitializeInternal` — 아래) |
| `+0xe`, `+0x10` | `MOV DL,1` / `MOV RCX,RBX` | 인자 = (맡긴 문맥, TRUE) |
| `+0x13` (ret **`+0x18`**) | `CALL NtContinue` | **맡긴 문맥으로 실행을 옮겨 달라고 함** |
| `+0x1a` (ret `+0x1f`) | `CALL RtlRaiseStatus` | `NtContinue` 가 돌아오면 (실패) 예외 |

- `RDX`(ntdll 칸)는 `+0x0 ~ +0x9` 에서 안 건드리고 로더 본체로 넘어간다 (본체는 Q8).
- 맡긴 문맥의 `Rip` 가 `RtlUserThreadStart`, `Rcx` 가 exe 진입점이니, **`NtContinue` 가 문맥대로 돌려준다면** `RtlUserThreadStart(진입점, PEB)` 모양으로 이어진다 `[추측]`. 커널 `NtContinue` 가 문맥의 `Rip` 로 실제로 보내는 부분은 안 읽었다 `[공백]` (Q17·Q29). 이름만 본 후보: `KeContextToKframes` 를 부르는 곳에 `KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0` 이 있다 (원본 바이트 훑기, 본문 안 읽음) `[추측]`.

### 정적 — ⑤ 커널이 `LdrInitializeThunk` 주소를 아는 법 (부팅 때)

```
PspInitPhase2 +0x177                                        (원본 바이트 훑기)
 └─ PspInitializeSystemDlls      종류 0..5 반복 (+0x26)
     ├─ (1) +0x32  PsQuerySystemDllInfo   PspSystemDlls[k] 칸 (+0x20 ≠ 0 일 때만)
     └─ (2) +0x5b  PspLookupEntryPoint    표의 이름마다
          └─ +0x20  RtlFindExportedRoutineByName   ntdll 내보내기 표에서 이름 찾기
         → +0x6b  표가 가리키는 칸에 주소 기록 (실패면 KeBugCheckEx 0x6b)
```

- 표 `NtdllExportInformation` 0번 = {`NtdllExports`, 19개}. `NtdllExports` 항목 = {이름 문자열, 기록할 칸}. 첫 항목 = {`"LdrInitializeThunk"`, `PsNtdllExports+0x8`}, 둘째 = {`"RtlUserThreadStart"`, `+0x10`} (원본 바이트).
- `PspLookupEntryPoint` 는 찾은 주소를 **커널에서 본 ntdll 매핑 → 유저 주소로 환산**한다 (`+0x2f ~ +0x3a`, 정보 칸 `+0x18`·`+0x20` 차이).
- 정보 칸 `+0x20`·`+0x28` 은 부팅 때 `PspMapSystemDll`(인자 1 갈래)이 채운다 (`PspLocateSystemDll +0x238`, `PsLocateSystemDlls +0x89` 에서). 프로세스마다 붙일 땐(`PsMapSystemDlls`, 인자 0) 같은 주소에 붙었는지만 본다.
- 그래서 `PspInitializeThunkContext` 가 쓰는 두 값(`Rip` ← `PsNtdllExports.LdrInitializeThunk`, `Rdx` ← `PspSystemDlls[0]+0x20`)은 **부팅 때 한 번 정해진 전역**이다 `[정적]`. 프로세스마다 다시 찾지 않는다.
- 부팅 안에서 `PsLocateSystemDlls`(← `IoInitSystemPreDrivers +0x10f5`)와 `PspInitPhase2` 의 앞뒤 순서는 안 따졌다 `[공백]` (범위 밖).

### 01_sprint `[관찰]` 두 개가 맞아떨어지는 방법

| `[관찰]` | 코드로 본 이유 | 표시 |
|---|---|---|
| (run4) 커널 콜스택의 유저 자리에 `ntdll!RtlUserThreadStart` | run4 는 `bp nt!PspUserThreadStartup` **입구**에서 찍었다. 그때 트랩 프레임 `Rip` 는 `KxContextToKframes` 가 적은 그대로 = `RtlUserThreadStart` (바꿔 끼우기는 `+0xae` 에서, 그보다 뒤) | 값은 [정적]. WinDbg 가 그 칸을 유저 자리로 읽는다는 건 [추측] (run15 `KiStartUserThreadReturn` 풀어 보기) |
| (run4·run7) 첫 유저 멈춤은 `ntdll!LdrInitializeThunk`, 부른 곳 없음 | `PspInitializeThunkContext` 가 `Rip` 를 `LdrInitializeThunk` 로 바꾼 뒤 `IRETQ`(run15)로 가고, 새 `Rsp` 칸에 돌아올 주소를 안 쓴다 | 바꿔 끼우기·안 씀은 [정적]. 그 칸 값(0?)은 [추측], 동적 (Q42) |

- 둘은 서로 모순이 아니라 **같은 칸(`_KTRAP_FRAME.Rip`)의 바꾸기 전·후** 를 본 것이다 `[정적]+[추측]` (앞 문장의 시각 차이는 정적, "그래서 그렇게 보였다" 는 연결은 추측 — 동적으로 `+0xae`/`+0xb3` 앞뒤 값을 찍으면 닫힌다).
- 원래 문맥은 버려지지 않고 유저 스택에 맡겨져, 로더가 끝난 뒤 `NtContinue` 로 다시 쓰인다 (`NtContinue` 안쪽만 `[공백]`).

### 호출 관계

```
(부모 cmd.exe 스레드, 커널)
NtCreateUserProcess
 ├─ (5) +0xd22  PspCreateUserContext      CONTEXT: Rip = PsNtdllExports.RtlUserThreadStart, Rcx = 진입점, Rdx = PEB   [정적]
 └─ (7) +0xe5c  PspAllocateThread
                 ├─ +0x5ef  PspSetupUserStack     CONTEXT.Rsp                        [정적]
                 └─ +0x6f9  KeInitThread
                             └─ +0x301  KiInitializeContextThread
                                         └─ +0x1b1  KxContextToKframes   CONTEXT → 트랩 프레임 (처음)   [정적]

(test.exe 스레드, 커널)
KiStartUserThread
 ├─ (1) +0x28  PspUserThreadStartup
 │       └─ (5) +0xb3  PspInitializeThunkContext                                     [정적]
 │               ├─ (1) +0x16d  PspGetContextThreadInternal   원래 문맥 읽기
 │               ├─ (2) +0x285  RtlCopyContext                원래 문맥 → 유저 스택 (R15)
 │               ├─ (3) +0x29a  KePopulateContinuationContext (CET 일 때만 일함)
 │               ├─ (4) +0x2db  PspSetContextThreadInternal   새 문맥 (Rip = LdrInitializeThunk, Rcx = R15)
 │               │       └─ +0x136  PspGetSetContextSpecialApc
 │               │           └─ +0x69  PspGetSetContextInternal
 │               │               └─ +0x3ef  PspSetContext     +0x68 트랩 프레임 Rip 에 씀
 │               └─ (5) +0x2fa  KiCheckForKernelApcDelivery   (조건부)
 └─ (2) ═▶ 유저  IRETQ  (run15)
          ─▶ ntdll!LdrInitializeThunk  (Rcx = 맡긴 문맥)                              [관찰] run4 + [정적]
              ├─ (1) +0xe   로더 본체                                               [관찰] run7
              └─ (2) +0x18  NtContinue(맡긴 문맥, TRUE)                             [정적]
                      ═▶ (커널 NtContinue 안 [공백]) ═▶ RtlUserThreadStart (맡긴 문맥 Rip)   [추측]
```

## 확인된 것

- **처음 문맥의 `Rip` 는 `RtlUserThreadStart` 다.** [정적] `PspCreateUserContext +0x28` 이 `PsNtdllExports+0x10` 값(`NtCreateUserProcess +0xd10`)을 `CONTEXT.Rip` 에 적는다. 같은 함수가 `Rcx` = exe 진입점(`TransferAddress`), `Rdx` = PEB 를 적는다. run11 `[추측]` "`StartAddress` 가 `RtlUserThreadStart` 쪽" 이 맞았다.
- **그 문맥은 `KiInitializeContextThread +0x1b1` 의 `KxContextToKframes` 가 트랩 프레임에 처음 옮긴다.** [정적] run15 `[추측]` 이 맞았다.
- **출발 직전 `PspInitializeThunkContext` 가 원래 문맥을 유저 스택에 맡기고(`+0x285 RtlCopyContext`), 트랩 프레임을 `Rip = LdrInitializeThunk`, `Rcx = 맡긴 문맥 주소`, `Rsp = 맡긴 문맥 − 0x28` 로 바꾼다.** [정적] 칸에 실제로 쓰는 줄은 `PspSetContext +0x68`.
- **`LdrInitializeThunk` 는 `Rcx` 를 `RBX` 에 쥐고 있다가 `+0x18` 에서 `NtContinue(맡긴 문맥, TRUE)` 를 부른다.** [정적] 그림 2 의 `(2) ?` 가 `+0x18` 로 바뀐다. 실행은 안 봤다.
- **커널은 부팅 때 ntdll 내보내기 표에서 이름(`"LdrInitializeThunk"` 등 19개)으로 찾아 `PsNtdllExports` 에 적어 둔다.** [정적] (`PspInitPhase2 +0x177` → `PspInitializeSystemDlls +0x5b` → `PspLookupEntryPoint +0x20`)
- **바꿔 끼우기에 "첫 스레드만" 조건은 없다.** [정적] run4 `[관찰]` "스레드 4개 모두 `LdrInitializeThunk` 에서 출발" 과 모양이 같다.

## 정적과 동적이 다른 곳

- **`LdrInitializeThunk +0xe` 가 부르는 함수 이름.** run7 `[관찰]` 콜스택은 `LdrInitializeThunk+0xe` → `LdrpInitializeInternal`. 정적 `.asm` 의 `+0x9` (ret `+0xe`) 대상은 Ghidra 이름 **`LdrpInitialize`** (rva `0xe1448`) 이고, 그 함수가 `+0x3c` 에서 `LdrpInitializeInternal` 로 **꼬리 점프**한다 (`_calls.tsv`: `LdrpInitialize +0x3c tail LdrpInitializeInternal`).
  - `[추측]` 꼬리 점프는 자기 프레임을 안 남기니, 콜스택엔 `LdrpInitializeInternal` 이 `LdrInitializeThunk+0xe` 바로 위로 보였을 수 있다. 이번 질문 밖이라 고치지 않고 둘 다 적는다. 대기열 Q8 에 이미 있는 이름 정정 건이다 (sast/02).

## 아직 확인 필요

- 동적 → 대기열 **Q42** (조건은 모두 test.exe 일 때만, 결과서 "동적분석으로 확인해야 할 것"):
  1. `PspUserThreadStartup+0xae` / `+0xb3` 앞뒤 트랩 프레임 `Rip`·`Rcx`·`Rdx`·`Rsp` — 앞 = `RtlUserThreadStart`/진입점/PEB/원래 `Rsp`, 뒤 = `LdrInitializeThunk`/맡긴 문맥 주소/ntdll 칸/맡긴 문맥 − 0x28 인지.
  2. `PspInitializeThunkContext+0x16d` 에서 `dt nt!_CONTEXT @r13 Rip Rcx Rdx Rsp`, `+0x2d6` 에서 `dt nt!_CONTEXT @rdx Rip Rcx Rdx Rsp R10` 과 `@r15`, `+0x2db` 뒤 트랩 프레임 값.
  3. `ntdll!LdrInitializeThunk` 입구 `r rcx rdx rsp`, `dq @rsp L2` (돌아올 주소 칸이 0 인지), `dt ntdll!_CONTEXT @rcx Rip Rcx`. `+0x13` 에서 `@rbx` 가 처음 `@rcx` 와 같은지.
  4. `dq nt!PsNtdllExports L3`, `ln poi(nt!PsNtdllExports+8)`, `dq poi(nt!PspSystemDlls)+0x20 L1` vs `lm m ntdll`.
  5. `PspInitializeThunkContext+0x2f5` `KiCheckForKernelApcDelivery` 가 test.exe 스레드에서 불리나 — 멈춤 APC 가 여기서 들어올 수 있는 후보 하나가 더 생겼다 (run13·run15 "`+0x26` 에서" 와 비교, Q19 와 같이).
  6. 갈림 값: `InstrumentationCallback`(`_KPROCESS +0x168`), `_KTHREAD +0x74` bit20 (CET), `PspNotifyEnableMask` bit4.
  7. `StartAddress`/`Win32StartAddress` 가 `+0xae` 앞뒤로 같은지, 네 스레드 모두 같은 길인지 (맡긴 문맥 `Rcx` = 그 스레드의 `Win32StartAddress`).
  - 멈춤점 주의: 이 함수는 모든 프로세스의 모든 유저 스레드가 거친다. 반드시 프로세스 조건을 붙인다.
- 동적 → **Q34**: test.exe 때 `PspCreateUserContext` 길인지 `PspGetContextThreadInternal` 길인지.
- 정적 → **Q17** (ntdll) / 동적 → **Q29**: 커널 `NtContinue` 가 맡긴 문맥의 `Rip` 로 돌려보내는 부분, `RtlUserThreadStart` 에서 `k`.
- 정적 → **Q8**: `LdrInitializeThunk +0xe` 의 대상 이름 (`LdrpInitialize` 꼬리 점프 → `LdrpInitializeInternal`), 로더 본체가 `RDX`(ntdll 칸)를 어떻게 쓰나.

## 단계 1 (nt 지도) 정리

이번 바퀴로 대기열 단계 1 의 처음 항목 Q1 ~ Q7 이 끝났다. 새 사실은 없고 어디를 채웠는지만 모은다.

| 대기열 | run | 채운 flow.md 절 |
|---|---|---|
| Q1 | run9 | 2.1 `NtCreateUserProcess` 아래 대표 12개, 2.2 멈춤 갈림길 |
| Q2 | run10 | 2.1 `PspAllocateProcess` 아래 (주소 공간 → exe → ntdll → PEB), `MmCreatePeb` PEB 오프셋 |
| Q3 | run11 | 2.1 `PspAllocateThread` 아래 (객체 → ID → 스택 → TEB → `KeInitThread`), `PspInsertProcess` |
| Q4 | run12 | 2.1 `PspInsertThread` 아래, 알림 (첫 스레드만 / 모든 스레드) |
| Q5a | run13 | 2.2 멈춰 두기(멈춤 APC + `SuspendEvent`), 풀어 주는 nt 사슬 |
| Q5b | run14 | 2.2 부모 KERNELBASE `CreateProcessInternalW` (늘 멈춘 채로 → csrss 알림 → `NtResumeThread`) |
| Q6 | run15 | 2.3 `KiStartUserThread` → `PspUserThreadStartup` → `IRETQ` |
| Q7 | run16 | 2.1 `PspCreateUserContext`, 2.3 `PspInitializeThunkContext` 바꿔 끼우기, 2.4·2.10 `NtContinue` 앞부분 |

- 단계 1 은 모두 `[정적]` 이다. 실행으로 맞춰 볼 동적 항목: **Q19, Q34, Q35, Q38, Q39, Q42** (nt 쪽), **Q29** (`NtContinue` → `RtlUserThreadStart`).

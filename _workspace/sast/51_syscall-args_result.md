# 51 syscall-args — 정적 분석 결과 (Q106)

- 요청서: `_workspace/sast/51_syscall-args.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), ntdll (`.../System32/ntdll.dll`).
  - 커널 PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준값과 같음.
  - ntdll PDB guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`).
- 분석: 두 모듈 다 이미 돼 있어서 다시 안 걸었다.
- 이번에 쓴 뽑기 (앞선 decompiler 가 03:19~03:25 에 만든 것을 그대로 씀, 다시 안 뽑음):
  - export depth 0 (로그 `ms/ghidra/ntoskrnl.export.sast51.stdout`, `…sast51b.stdout`):
    - `KiSystemService`, `KiSystemServiceUser`, `KiSystemServiceStart`, `KiSystemServiceExitPico`, `KiKernelSysretExit`
    - `KeCompactServiceTable`, `KiInitSystem`, `KeAddSystemServiceTable`, `PsInitializeWin32kServiceTable`, `KiConvertToGuiThread`
  - refs → `ms/disassemble/ntoskrnl/_refs/`: `KiServiceTable.tsv`, `KiServiceLimit.tsv`, `KeServiceDescriptorTable.tsv`, `KeServiceDescriptorTableShadow.tsv`, `KiSystemCall64.tsv`, `KiSystemCall64Shadow.tsv`, `KiKernelSysretExit.tsv` (뒤 셋도 tsv 가 다 써져 있었다. 다시 안 돌림).
  - 이미 있던 `KiInitializeKernel.asm` 은 읽기만 했다.
- `.asm` 이 없는 곳은 objdump 로 봤다 (아래 "스크립트 문제"·"도구 메모"):
  - `KiSystemCall64` (rva `0x6c1840`): Ghidra 가 함수로 안 잡았다.
  - `KiSystemServiceStart` 끝 조각의 CET 명령: Ghidra 가 잘못 읽었다.
- **버전 확인: 일치.**
  - `_calls.tsv` `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`.
  - ntdll `LdrpInitializeProcess` call `+0x1bb6` / ret **`+0x1bbb`** → `LdrpInitializeGraphRecurse`.
  - 둘 다 "알려진 사실" 표와 같음.
  - 덤으로 run3·run44 의 `[관찰]` `KiSystemServiceCopyEnd+0x28` 과 대조했다. 파일 바이트로는 `+0x25` 라서 3바이트 어긋난다. 이유는 질문 1 끝 "관찰과 3바이트 차이" 에.

## 스크립트 문제

고칠 것은 아니고 알고 쓸 것 둘.

1. **`KiSystemCall64` 가 Ghidra 함수가 아니다.**
   - `pdb_aliases.py --where ntoskrnl.exe KiSystemCall64` 결과: `rva=0x6c1840 [function]` 인데 `_index.tsv` 에 없고, 어느 함수 범위에도 안 든다. `KiSystemCall64Shadow` (`0xbcb200`) 도 같다.
   - 그래서 `run_ghidra.sh export '^KiSystemCall64$'` 로는 안 나온다. 같은 자리 둘레엔 `FUN_1406c196c` 같은 9바이트짜리 가짜 함수가 31개 잡혀 있다 (RSB 채우기 사다리).
   - 이번엔 `objdump -d -M intel --start-address=0x1406c1840 --stop-address=0x1406c1bc0` 로 봤다 (출력은 스크래치에만).
   - 제안: `ExportFunctions` 가 PDB 에 `[function]` 인데 Ghidra 함수가 없는 이름이면, 그 rva 부터 다음 `_index.tsv` 함수 시작까지를 선형 디스어셈블로 `.asm` 에 남기는 갈래. 또는 analyze 뒤 `createFunction` 을 한 번 더 시도.
2. **Ghidra 11.3.2 가 F3 접두 CET 명령을 잘못 읽는다.**
   - `KiSystemServiceStart.asm` 조각 `FUN_1406c20cc+0x84` 가 `RDSSPD ECX` 로 나온다. 바이트(`f3 49 0f 1e c9`)는 **`RDSSPQ R9`** 이다.
   - `+0x8e` 의 `XSAVEOPT [R9]` 도 바이트(`f3 41 0f ae 31`)는 **`CLRSSBSY [R9]`** 이다.
   - `KiKernelSysretExit.asm` `+0x59`/`+0x63` 도 같은 꼴이다 (`RDSSPQ RSP` / `CLRSSBSY [RSP]`).
   - 그대로 읽으면 "`RCX`(유저 RIP) 를 덮어쓴다" 로 잘못 읽게 된다. objdump(binutils) 는 바르게 읽는다.
   - 제안: `.asm` 머리 설명이나 reference.md 에 "F3 0F 1E / F3 0F AE 는 objdump 로 다시 볼 것" 한 줄.

## 질문별 답 [정적]

### 1. 칸 모양 — `KiServiceTable` 칸 하나에서 함수 주소·스택 인자 수 뽑기

**한 줄 답**
- 파일 속 칸 = 그냥 **함수 rva** (32비트, 아래 4비트 = 0).
- 부팅 때 커널이 칸을 **고쳐 쓴다.**
- 메모리 칸 = `((함수 rva − 0xdadc0) << 4) | 스택 인자 수`.
- 커널은 **부호 있게 읽고 `SAR 4`** 해서 표 시작에 더하면 함수 주소, **`AND 0xf`** 하면 스택 인자 수.
- 스택 인자 수 = **5번째부터의 8바이트 칸 수** (레지스터 4개 몫은 빠짐).
- PE_emulator 의 `[추측]` 모양은 **메모리 칸 기준으로는 맞다.** 다만 PE 파일에서 바로 읽으면 그 꼴이 아니다.

#### (1) 파일 속 값 = 함수 rva 그대로

- `KiServiceTable` rva `0xdadc0` (`.rdata`), 4바이트 칸 489개 (`KiServiceLimit` rva `0xdb564` = 489).
- 489칸 전부 아래 4비트가 0이고, 값이 그대로 `_index.tsv` 함수 시작 rva 와 맞는다.
  - 예: `[0x43]` 파일 값 `0x006adde0` = `NtContinue` rva.
- 칸이 가리키는 rva 범위: `0x20a9f0` ~ `0xb6e8b0`. 전부 표(`0xdadc0`)보다 뒤라서, 고쳐 쓴 뒤 오프셋이 음수가 되는 칸은 없다.
- **sast/24·29(run29·run34) 가 칸을 푼 방법**: `>>4` 없이 파일 값을 그대로 rva 로 읽었다 (sast/24 결과서 "압축 없는 rva 로 읽힘").
  - **파일에 대해서는 맞는 방법이었다.** 그래서 489/489 맞았다.
  - 다만 "메모리에서도 그 꼴" 은 아니다 — 아래 (2).

#### (2) 부팅 때 고쳐 쓰는 코드 = `KeCompactServiceTable`

- 누가 언제 부르나:
  - `KiInitializeKernel.asm` `+0x257` CALL → `KeCompactServiceTable` (ret `+0x25c`, `_calls.tsv` 1100줄).
  - 넘기는 인자 (`+0x233 ~ +0x250`):
    - `RCX` = `KiServiceTable`
    - `RDX` = **`KiArgumentTable`** (rva `0xdb568`, PDB 이름, 1바이트 칸 489개)
    - `R8D` = `KiServiceLimit`
    - `R9D` = 0
    - `[RSP+0x20]` = 이미지 시작 (`LEA R13,[0x140000000]`)
  - 이 CALL 은 `+0xf7 TEST R15D,R15D` / `+0xfa JNZ` 갈림의 0 쪽에만 있다.
    - `R15D` 는 `+0x5a` 에서 스택 인자(`[RSP+0x300]`)로 받는다. 첫 CPU 번호로 보인다 `[추측]`.
    - 실제로 부팅 때 지나갔는지는 정적으로 못 정한다.
- 하는 일 (`KeCompactServiceTable.asm` 루프 `+0x17 ~ +0x3d`, 칸마다 한 번):
  - `+0x17 MOVZX EAX,byte [RBX]` — 인자표 바이트 하나.
  - `+0x21 ~ +0x2a` — `EDX = 이미지시작 − 표주소 + 칸값`. 칸값이 rva 라서 이것이 **(함수 주소 − 표 주소)**.
  - `+0x27 SHR EAX,2` — 인자표 바이트 ÷ 4.
  - `+0x2d SHL EDX,4` / `+0x30 OR EDX,EAX` / `+0x32 MOV [R11],EDX` — 그 칸에 다시 씀.
  - 루프 뒤 `+0x42` CALL `KiLockServiceTable` (이름만. 표를 잠그는 쪽으로 보임 `[추측]`).
- **인자표 바이트는 "스택 인자 바이트 수 (4바이트 단위)" 꼴**이다. `÷4` 하면 8바이트 칸 수가 된다.
  - 489바이트 값의 종류: `0, 4, 8, 12, …, 48, 52`. 전부 4의 배수라서 `÷4` 로 잃는 것이 없다.
  - 예: `NtAccessCheck` (8인자) = `0x10` → 4.
- **`KiServiceTable` 을 쓰는 곳은 둘뿐**이다 (`_refs/KiServiceTable.tsv`):
  - `KiInitializeKernel+0x250` (위 CALL 의 인자)
  - `KiInitSystem+0xfa` (서술자에 주소만 넣음)
  - 다른 곳에서 고쳐 쓰는 코드는 없다.
- **서술자 채우기** (`KiInitSystem.asm`):
  - `+0x101` `KeServiceDescriptorTable+0x0` = `KiServiceTable`
  - `+0x115` `+0x10` = `KiServiceLimit`
  - `+0x122` `+0x18` = `KiArgumentTable`
  - `+0x1af ~ +0x1e0` 같은 0x40바이트를 `KeServiceDescriptorTableShadow`·`KeServiceDescriptorTableFilter` 앞에 복사.
  - 서술자 `+0x18`(인자표 주소) 은 남아 있다. 하지만 아래 디스패치 길은 그것을 안 읽고 칸의 아래 4비트만 쓴다.
- win32k 표(서술자 `[1]`)는 `PsInitializeWin32kServiceTable+0x8e` → `MmCompactServiceTable` 이 따로 누른다 (이름만, 범위 밖).

메모리 칸 예 (스크립트로 계산한 값. 메모리에서 직접 본 값은 아님):

| 번호 | 함수 | 파일 값 | 인자표 바이트 | 메모리 값 (계산) | `SAR 4` + 표 | `AND 0xf` |
|---|---|---|---|---|---|---|
| `0x0` | `NtAccessCheck` | `0x0039d590` | `0x10` | `0x02c27d04` | `0x39d590` | 4 |
| `0x43` | `NtContinue` | `0x006adde0` | `0x00` | `0x05d30200` | `0x6adde0` | 0 |
| `0x52` | `NtResumeThread` | `0x00a2d600` | `0x00` | `0x09528400` | `0xa2d600` | 0 |
| `0xd1` | `NtCreateUserProcess` | `0x00a01340` | `0x1c` | `0x09265807` | `0xa01340` | 7 |

#### (3) 칸을 읽는 줄 (`KiSystemServiceStart.asm`)

PDB 이름표: `KiSystemServiceRepeat` = `+0x14`, `KiSystemServiceCopyStart` = `+0x100`, `KiSystemServiceCopyEnd` = `+0x170`, `KiSystemServiceExit` = `+0x1a0` (`pdb_aliases.py --where`, 넷 다 `[none]` 이름표).

- 번호 풀기 (`EAX` = 유저가 준 번호):
  - `+0x7 ~ +0xc` — `EDI = (EAX >> 7) & 0x20`. 번호의 비트 12 (`0x1000`) 가 서 있으면 서술자 `[1]`(win32k), 아니면 `[0]`.
  - `+0xf AND EAX,0xfff` — 표 안 번호.
- 표 고르기:
  - `+0x14` 기본 = `KeServiceDescriptorTable`.
  - `+0x22 ~ +0x3b` GUI 스레드(`KTHREAD+0x78` bit 7 `GuiThread`) 면 `…Shadow`, `RestrictedGuiThread`(bit 21) 면 `…Filter`.
- 범위 검사:
  - `+0x3e CMP EAX,[R10+RDI+0x10]` (Limit) / `+0x43 JNC +0x907`.
  - 넘치면 `+0x907`: `0x1000` 대이고 아직 GUI 스레드가 아니면 `KiConvertToGuiThread` (`+0x91f`) 뒤 `+0x940` 에서 다시 `+0x14` 로. 아니면 `+0x967` `EAX = 0xc000001c` (STATUS_INVALID_SYSTEM_SERVICE) 로 바로 나감.
- **(a) 함수 주소**:
  - `+0x49 MOV R10,[R10+RDI]` — 표 시작.
  - **`+0x4d MOVSXD R11,dword [R10+RAX*4]`** — 칸을 부호 확장해서 읽음.
  - `+0x51 MOV RAX,R11` — 아래 4비트용으로 복사.
  - **`+0x54 SAR R11,0x4`** — 산술 오른쪽 4.
  - **`+0x58 ADD R10,R11`** → `R10` = 함수 주소.
- **(b) 스택 인자 수**:
  - **`+0xb0 AND EAX,0xf`** → 개수.
  - `+0xb3 JZ +0x170` (`CopyEnd`) — 0 이면 복사 안 함.
  - **`+0xb9 SHL EAX,3`** — 개수 × 8.
- **복사 사다리** (`+0xbc ~ +0x16c`):
  - `+0xbc LEA RSP,[RSP-0x70]` / `+0xc1 LEA RDI,[RSP+0x18]` — 커널 쪽 받는 곳.
  - `+0xc6 MOV RSI,[RBP+0x100]` — `KTRAP_FRAME.Rsp`(`RBP` = 트랩 프레임 `+0x80`, `pdb_struct.py`) = 유저 RSP.
  - `+0xcd LEA RSI,[RSI+0x20]` — 그림자 공간 0x20 건너뜀.
  - `+0xd1 ~ +0xe1` 유저 모드에서 왔으면 `MmUserProbeAddress` 로 자름.
  - `+0xf0 LEA R11,[CopyEnd]` / `+0xf7 SUB R11,RAX` / `+0xfa JMP R11` — 끝에서 `개수×8` 바이트 앞으로 뛰어듦.
  - `+0x100 ~ +0x16c`: `MOV RAX,[RSI+k]` / `MOV [RDI+k],RAX` 한 쌍이 8바이트. `k` 는 `0x70` 부터 `0x8` 까지 내려가는 14쌍.
  - 그래서 개수 `n` 이면 마지막 `n` 쌍만 돈다. `k = 8, 16, …, 8n`.
  - 유저 `[RSP+0x28]`(5번째 인자) ~ `[RSP+0x20+8n]`(4+n번째) 를 커널 새 `RSP+0x20 …` 에 복사한다 (커널 함수가 보는 5번째 인자 자리).
- **결론**: 스택 인자 수 = **"레지스터 4개(`RCX`(=R10)·`RDX`·`R8`·`R9`) 다음, 유저 스택 `RSP+0x28` 부터 복사할 8바이트 칸 수"**.
  - 사다리가 14쌍이라 코드상 최대 14. 4비트 칸은 15까지 담을 수 있지만, 이 빌드 표의 최대값은 13이다.
- 함수 부르기:
  - `+0x190 MOV RAX,R10` / `+0x193 CALL RAX` (`+0x195 NOP dword [RAX]`).
  - 트레이스 갈래 `+0x17a`(`KiDynamicTraceMask`)→`+0x9c6` (`KiTrackSystemCallEntry`/`Exit`), `+0x18a`(`PerfGlobalGroupMask`)→`+0xa3a` (`PerfInfoLogSysCallEntry`/`Exit`). 이름만.

#### 관찰과 3바이트 차이 (`KiSystemServiceCopyEnd+0x28`)

- 파일 바이트 `0x6c1d50`: `49 8b c2 | ff d0 | 0f 1f 00` = `MOV RAX,R10` / `CALL RAX`(2바이트) / `NOP dword [RAX]`(3바이트).
  - 이대로면 돌아올 곳 = `+0x195` = **`CopyEnd+0x25`**.
- run3·run44 `[관찰]` 은 **`CopyEnd+0x28`** = NOP 끝(`+0x198`).
- `CALL RAX` + 3바이트 NOP = 5바이트 자리가 메모리에서 5바이트 직접 CALL(retpoline 류) 로 바뀌었다고 보면 맞아떨어진다 `[추측]`.
  - 이 빌드에서 그렇게 바꾸는 코드(동적 재배치)는 이번에 안 찾았다.
- 칸 풀기 답에는 영향 없다. 오프셋을 맞출 때 `+0x28` 이 맞는 값이다.


### 2. 번호표 (기계용 파일)

- 파일: **`_workspace/sast/51_syscall-args_table.tsv`**
  - 머리 1줄 + 489줄 (`0x0` ~ `0x1e8`). 탭 구분.
  - 머리줄은 `#` 없는 보통 TSV 머리다.
- 열:
  - `number` — 16진 (`0x43` 꼴).
  - `nt_function` — 커널 함수 PDB 이름.
  - `stack_args` — 메모리 칸 아래 4비트 = `KiArgumentTable[번호] >> 2`.
  - `total_args` — `4 + stack_args`.
  - `ntdll_stub` — 같은 번호를 쓰는 ntdll `Nt*` export 이름. 없으면 빈칸.
- MS 코드가 아니라 번호·이름·숫자만 들어 있어서 그대로 커밋해도 된다고 본다.
- 만든 방법: `pefile` 로 ntoskrnl `KiServiceTable`(rva `0xdadc0`)·`KiArgumentTable`(rva `0xdb568`) 489칸을 읽어 `KeCompactServiceTable` 식으로 풀고, PDB 공개 심볼(`llvm-pdbutil-14 -publics`)·ntdll 스텁 스캔(질문 3)과 짝지은 임시 스크립트 (스크래치, 레포 밖).

**`total_args` 읽는 법 (중요)**

- `stack_args > 0` 이면 `total_args` 가 정확한 인자 수다 (레지스터 4 + 스택 n).
- `stack_args = 0` 이면 실제 인자는 **0~4개**다. 표만으로는 몇 개인지 못 정한다.
  - 이때 `total_args` 는 "레지스터 4칸" 이라는 뜻의 4다.
  - 예: `NtClose` 는 1개, `NtContinue` 는 2개인데 둘 다 `4` 로 적혀 있다.
  - 로그용으로는 "레지스터 4개 + 스택 0개" 라서 문제 없다.
- 489칸 분포 (`stack_args`: 칸 수):
  - 0: 302 / 1: 72 / 2: 44 / 3: 13 / 4: 12 / 5: 16 / 6: 11 / 7: 9 / 8: 2 / 9: 1 / 10: 2 / 12: 3 / 13: 2
  - 최대 13. 11 과 14 는 없다.

**이름 고르기**
- 21칸은 한 커널 주소에 PDB 함수 이름이 여럿 붙어 있다 (ICF, 링커가 같은 내용 함수를 하나로 합침).
  - 예: `0x705290` 한 곳을 12번호(`0x6d`, `0xb0`, `0xe2`, `0xee`, `0x128`, `0x1a2`, `0x1a3`, `0x1b6`, `0x1b7`, `0x1e2`, `0x1e7`, `0x1e8`)가 같이 쓴다. 이름은 `NtCreateEventPair`·`NtVdmControl` 등 19개.
  - `0xa9b6e0` = `0xa2`·`0xf1`·`0xf5`.
  - `0x74b750` = `0x54`(`NtReadRequestData`)·`0x57`(`NtWriteRequestData`).
- 그런 칸은 **ntdll 스텁 이름을 골랐다.** 그 이름이 그 주소의 PDB 이름 목록 안에 있는지 489칸 전부 확인했고 어긋난 칸은 0개다.
- 스텁이 없는 `0x5a` 는 PDB 함수 이름이 `NtQuerySystemTime` 하나뿐이라 그것을 썼다.
- 결과적으로 `nt_function` 489개가 전부 서로 다르다.

**손 대조** (`[정적]`)

| 번호 | 함수 | 표 `stack_args` | 인자 수 근거 | 맞나 |
|---|---|---|---|---|
| `0x43` | `NtContinue` | 0 | `NtContinue.c` 매개변수 2개 | 맞음 (2 ≤ 4) |
| `0x52` | `NtResumeThread` | 0 | `NtResumeThread.c` 2개 | 맞음 |
| `0xd1` | `NtCreateUserProcess` | 7 | `NtCreateUserProcess.c` 11개 (`param_1 ~ param_11`) | 맞음 (4 + 7) |
| `0x19` | `NtQueryInformationProcess` | 1 | `.c` 5개 | 맞음 |
| `0x1d0` | `NtSystemDebugControl` | 2 | `.c` 6개 | 맞음 |
| `0x4a` | `NtCreateSection` | 3 | `.c` 는 1개로 나옴 → **`.asm` 으로 봄**: `+0xa [RSP+0xa8]`, `+0x3a [RSP+0xb0]`, `+0x60 [RSP+0xa0]` = 들어올 때 `RSP+0x28/0x30/0x38` (5·6·7번째) | 맞음 (7개) |

- 이미 있던 `ms/decomp/ntoskrnl/Nt*.c` 12개를 전부 대조했다. 위 표 밖 6개(`NtAlertResumeThread` 2, `NtClose` 1, `NtContinueEx` 2, `NtSetInformationProcess` 4, `NtSetInformationThread` 4, `NtSetWnfProcessNotificationEvent` 1)도 전부 `stack_args = 0` 과 어긋나지 않는다.
- `NtCreateSection` 처럼 얇은 감싸개 함수는 Ghidra `.c` 의 매개변수 수가 모자라게 나온다. 인자 수는 `.asm` 의 `[RSP+0x28 이상]` 읽기로 본다.

### 3. ntdll 스텁과 1:1 인가

- 스캔: ntdll 실행 섹션 전체에서 `4c 8b d1 b8` (`MOV R10,RCX` / `MOV EAX,imm32`) 를 찾았다. **494곳.**
- **모양은 494곳 전부 같다**: 그 뒤 16바이트가 `f6 04 25 08 03 fe 7f 01 | 75 03 | 0f 05 | c3 | cd 2e | c3`.
  - 뜻: `TEST byte [0x7ffe0308],1` / `JNZ +3` / **`+0x12 SYSCALL`** / `+0x14 RET` / `+0x15 INT 0x2e` / `+0x17 RET`.
  - 뒤 채움만 다르다: 493곳 NOP, 1곳 `CC`.
  - "앞에 다른 명령이 붙은 스텁" 은 없다.
- **번호: 488개, `0x0` ~ `0x1e8`.** `0x1000` 이상(win32k 표)은 **0개.** `0x1e9`(489) 이상도 0개.
- **489칸과 견주면:**
  - **빠진 번호 1개: `0x5a` `NtQuerySystemTime`.**
    - ntdll 의 `NtQuerySystemTime`/`ZwQuerySystemTime` export 는 스텁이 아니다. `E9` JMP 로 `RtlQuerySystemTime` (rva `0xed060`) 에 넘긴다 — 유저에서 공유 데이터 시계를 읽고 커널에 안 들어가는 길로 보인다 `[추측]`.
    - 그래서 ntdll 로는 `0x5a` 를 부를 길이 없다.
  - **번호 하나에 스텁이 둘인 곳 6개.** export 가 아니라 PDB 에만 이름이 있는 내부 스텁 `Np*`:

    | 번호 | 내부 스텁 (rva) | 짝 export 스텁 (rva) |
    |---|---|---|
    | `0xf` | `NpClose` (`0x160920`) | `NtClose` (`0x160f90`) |
    | `0x23` | `NpQueryVirtualMemory` (`0x160940`) | `NtQueryVirtualMemory` (`0x161210`) |
    | `0x28` | `NpMapViewOfSection` (`0x160960`) | `NtMapViewOfSection` (`0x1612b0`) |
    | `0x2a` | `NpUnmapViewOfSection` (`0x160980`) | `NtUnmapViewOfSection` (`0x1612f0`) |
    | `0x33` | `NpOpenFile` (`0x1609a0`) | `NtOpenFile` (`0x161410`) |
    | `0x4a` | `NpCreateSection` (`0x1609c0`) | `NtCreateSection` (`0x1616f0`) |

    - 바이트 모양은 export 스텁과 같고 번호도 같다. 누가 부르는지는 이번에 안 봤다 (범위 밖).
  - **한 주소에 `Nt*`/`Zw*` 말고 이름이 더 붙은 곳 1개:** `0x36` 스텁 (rva `0x161470`) = `NtQuerySystemInformation` + `ZwQuerySystemInformation` + **`RtlGetNativeSystemInformation`**.
- **export 이름 수:**
  - export 스텁 488곳에 `Nt*` 488개 + `Zw*` 488개. 전부 `Nt`/`Zw` 짝이 있다.
  - `Nt`/`Zw` 로 시작하는 export 이름은 모두 983개. 나머지 7개는 스텁이 아니다:
    - `NtQuerySystemTime`·`ZwQuerySystemTime` (위)
    - `NtGetTickCount` (`E9` JMP → `RtlGetTickCount`, rva `0x145570`. 커널 표에 같은 이름 없음)
    - `NtdllDefWindowProc_A/_W`·`NtdllDialogWndProc_A/_W` (`FF 25` 간접 JMP, 이름만 `Ntdll…`)
- **정리**:
  - ntdll export 스텁 488개 ↔ `KiServiceTable` 489칸 중 488칸이 1:1이다 (번호 같고, 스텁 `Nt` 이름이 그 칸 커널 주소의 PDB 이름에 들어 있음 — 488/488).
  - 남는 1칸은 `0x5a`.

### 4. 유저로 돌아갈 때 휘발성 레지스터

**들어갈 때** (`KiSystemCall64`, rva `0x6c1840`, objdump. 오프셋은 `KiSystemCall64` 기준):
- `+0x0 SWAPGS` → 유저 RSP 를 `GS:[0x10]` 에 둠.
- `+0x15 ~ +0x23` 트랩 프레임에 `SS 0x2b`, 유저 RSP, `R11`(=RFLAGS), `CS 0x33`, `RCX`(=돌아갈 RIP) 를 쌓음.
- `+0x3e MOV RCX,R10` — 첫 인자를 `RCX` 로 되돌림.
- `+0x55 ~ +0x63` `RBX`·`RDI`·`RSI` 저장, `+0x7f ~ +0x87` `RAX`(번호)·`RCX`·`RDX` 저장.
- `KiSystemServiceUser` (`0x6c1ae2`, objdump `0x6c1b01 ~ 0x6c1b32`) 에서 MXCSR, `R8`·`R9`·`R10`, `XMM0 ~ XMM5` 를 트랩 프레임에 저장.
- 그 뒤 `KiSystemServiceStart` 로 넘어간다.

**보통 반환 길** (`KiSystemServiceStart.asm`, `KiSystemServiceExit` = `+0x1a0`, 유저 모드에서 온 경우):

| 줄 | 하는 일 |
|---|---|
| `+0x1a0 ~ +0x1ae` | `RBX`·`RDI`·`RSI` ← 트랩 프레임 (들어올 때 값) |
| `+0x1be` | `SegCs` bit 0 = 0 (커널에서 부름) 이면 `+0x5be` 의 `RET` 길로 (이름만) |
| `+0x1fb ~ +0x25b` | 유저 APC 가 있으면 트랩 프레임 휘발 칸을 0 으로 지우고 `KiInitiateUserApc` 후 다시 검사 (이름만) |
| `+0x261`, `+0x27f`, `+0x2d3`, `+0x2f3` | `KiSynchronizeUserIsolationDomainEntry`, `KiUpdateStibpPairing`, `KiRestoreSetContextState`, `KiCopyCounters` (이름만) |
| `+0x305 LDMXCSR [RBP-0x54]` | MXCSR = 들어올 때 값 |
| **`+0x309 XOR R10,R10`** | **`R10 = 0`** |
| `+0x30c ~ +0x34c` | `Dr7` 칸(트랩 프레임 `+0x100`) ≠ 0 일 때만: `KiRestoreDebugRegisterState`. 그리고 `KPROCESS.InstrumentationCallback`(`+0x168`) ≠ 0 이고 `CS = 0x33` 이면 **`R10` = 원래 돌아갈 RIP, 돌아갈 RIP = 콜백 주소** |
| `+0x357 ~ +0x38d` | 그림자 스택(CET) 갈래 (이름만) |
| `+0x391` | 반환값 `RAX` 를 트랩 프레임 `Rax` 칸에 둠 |
| `+0x395 ~ +0x3f0` | 투기 실행 대책 MSR·RSB 채우기 (이름만) |
| 조각 `+0x34` (`0x6c2100`) | **`RAX`** ← 트랩 프레임 `Rax` = **NTSTATUS** |
| 조각 `+0x38` | **`R8`** ← 트랩 프레임 `Rsp` = **유저 RSP** |
| 조각 `+0x3f` | `R9` ← 트랩 프레임 `Rbp` |
| 조각 `+0x46 XOR EDX,EDX` | **`RDX = 0`** |
| 조각 `+0x48 ~ +0x5c` | **`XMM0 ~ XMM5 = 0`** (`PXOR`) |
| 조각 `+0x60` | **`RCX`** ← 트랩 프레임 `Rip` = **돌아갈 유저 RIP** (= 스텁 `+0x14 RET`) |
| 조각 `+0x67` | **`R11`** ← 트랩 프레임 `EFlags` = **유저 RFLAGS** |
| 조각 `+0x6e / +0x75` | `KiKvaShadow & 1` 이면 `KiKernelSysretExit` 로 |
| 조각 `+0x7b ~ +0x81` | `RBP = R9` (유저 RBP), `RSP = R8` (유저 RSP), **`R9 = 0`** |
| 조각 `+0x84 ~ +0x93` | `RDSSPQ R9` / 0 아니면 `CLRSSBSY [R9]` 뒤 `XOR R9,R9` → **`R9` 은 어느 쪽이든 0** (Ghidra 표기 `RDSSPD ECX`·`XSAVEOPT` 는 오독, 위 "스크립트 문제" 2) |
| 조각 `+0xac / +0xaf` | `SWAPGS` / `SYSRET` |

- 오프셋 기준: "조각" = `KiSystemServiceStart.asm` 의 `FUN_1406c20cc` 조각 기준. 본문 기준은 `+0x50c` 를 더함 (예: `SYSRET` = 본문 `+0x5bb`).
- 조각 `+0x1b ~ +0x31` (`GS:[0x858] & 0x100` 이면 `DIV RCX`) 는 `RAX`·`RDX`·`RCX` 를 건드리지만, 바로 뒤에서 셋 다 다시 채운다.

**`KiKvaShadow` 갈래** (`KiKernelSysretExit.asm`, rva `0xbcadc0`):
- `+0x0 ~ +0x41` CR3 를 `KPROCESS.UserDirectoryTableBase`(`+0x158`) 쪽으로 바꿈 (조건 갈래 있음).
- `+0x44 MOV RBP,R9` — 유저 RBP.
- **`R9` 을 지우지 않는다.** → 이 갈래에서는 **`R9` = 유저 RBP** 가 남는다.
- `+0x56 ~ +0x68` `RSP` 로 `RDSSPQ`/`CLRSSBSY` (Ghidra 표기 `RDSSPD ESP`/`XSAVEOPT`).
- `+0x6b MOV RSP,R8` / `+0x6e SWAPGS` / `+0x71 SYSRET`.
- 나머지(`RAX`·`RCX`·`RDX`·`R8`·`R10`·`R11`·`XMM0 ~ 5`) 는 위 조각에서 채운 그대로다.
- `KiKvaShadow` 비트가 켜지는 조건은 sast/50 #10 과 같은 전역·같은 판단이다 (`KiEnableKvaShadowing +0x131`, CPU·VM 따라 다름 → 정적으로 못 정함).
  - sast/50 의 첫 스레드 길은 `KiKernelExit`(IRETQ) 이고, 이번 syscall 길은 `KiKernelSysretExit`(SYSRET) 이다. **함수는 다르고 갈림 변수는 같다.**

**정리 — SYSRET 직후 유저가 보는 값** (보통 반환, 인스트루먼테이션 콜백 없음):

| 레지스터 | 값 |
|---|---|
| `RAX` | 커널 함수가 돌려준 NTSTATUS (번호가 범위 밖이면 `0xC000001C`) |
| `RCX` | 돌아갈 유저 RIP (SYSCALL 다음 명령) |
| `R11` | 유저 RFLAGS (SYSCALL 때 값. 트랩 프레임에서 다시 읽음) |
| `RDX` | 0 |
| `R8` | 유저 RSP (SYSCALL 때 값) |
| `R9` | 0 (`KiKvaShadow` 꺼짐) / 유저 RBP (켜짐) |
| `R10` | 0 (콜백 있으면 원래 RIP) |
| `XMM0 ~ XMM5` | 0 |
| `RBX`·`RSI`·`RDI`·`RBP`·`RSP`·MXCSR | 들어올 때 값 그대로 |
| `R12 ~ R15`, `XMM6 ~ XMM15` | 이 길에는 쓰는 줄이 없다. 커널 함수가 x64 호출 규약대로 지켜 준다고 본다 `[추측]` |

- **APC 배달·IRETQ 길 (이름만)**:
  - `KiInitiateUserApc` (`+0x248`)
  - `KiRestoreSetContextState` (`+0x2d3`, 스레드 플래그 `0x8000000` — `NtContinue`/`NtSetContextThread` 처럼 문맥을 통째로 바꾸는 쪽으로 보임 `[추측]`)
  - `KiSystemServiceExitPico` (`0x6c21ab`, 피코 프로세스)
  - `+0x5be` 커널 모드 호출자 `RET`
  - 이 길들에서는 위 표와 다른 값이 들어갈 수 있다.
  - **특히 `NtContinue` 는 test.exe 초기화 끝 길이라 이 표를 그대로 쓰면 안 될 수 있다** → 동적 확인 거리.

## 동적으로 확인할 것

1. **메모리 칸 꼴**: `dd nt!KiServiceTable+0x43*4 L1` → `05d30200` 이 나오나 (계산값). `+0xd1*4` → `09265807`.
   - 같으면 "부팅 때 고쳐 씀" 이 확인된다.
   - 다르면 `KiInitializeKernel +0xfa` 갈림을 다시 본다.
2. **`CopyEnd+0x28` 3바이트 차이**: `u nt!KiSystemServiceCopyEnd+0x20 L3`.
   - `CALL RAX; NOP` 대신 5바이트 `CALL` 이 보이면 retpoline 류로 고쳐 쓴 것 `[추측]` 확인.
3. **`KiKvaShadow` 값**: `db nt!KiKvaShadow L1` (sast/50 #10 과 같은 질문. 이미 dast 요청이 있으면 합침). `R9` 값이 이것에 달려 있다.
4. **돌아온 직후 레지스터**: test.exe 의 아무 `Nt*` 스텁 `+0x14` (`RET`) 에 멈추고 `r` 한 번.
   - `rdx = 0`, `r8 = rsp`, `r10 = 0`, `r11 = efl` 인지 본다.
   - 커널 멈춤점은 조건(test.exe 만) 필요.
5. `NtContinue` 로 나갈 때(`KiRestoreSetContextState` 갈래) 레지스터가 위 표와 다른지 — run29 `NtContinueEx` 길과 합쳐 볼 것.

추천 멈춤점 (dast-mentor 참고):
- `nt!KiSystemServiceCopyEnd+0x20` (`MOV RAX,R10`, 함수 주소 = `R10`, 스택 복사 끝난 뒤)
- `nt!KiSystemServiceStart+0x58` (`ADD R10,R11` 바로 뒤가 `+0x5b`)
- ntdll `Nt*` 스텁 `+0x14`

## 요청서 밖에서 본 것

- `KiInitializeKernel.asm` (`+0xf7 ~ +0x25c`) — 고쳐 쓰는 함수의 호출자라 읽음 (이미 있던 파일).
- `pdb_struct.py` 로 `_KTRAP_FRAME`, `_KTHREAD`(`+0x78`, `+0x98` ApcState, `+0x220`), `_KPROCESS`(`+0x158`, `+0x168`) 칸 이름 확인.
- `NtCreateSection.asm` — 손 대조에서 `.c` 매개변수 수가 틀려서 읽음 (이미 있던 파일).

## 도구 메모 (하네스는 안 고침)

- 이번에도 "함수 포인터 표를 읽는 명령이 없다" (sast/29 제안과 같음).
  - `KiServiceTable`·`KiArgumentTable` 을 `pefile` 임시 스크립트로 읽었다.
  - 이번엔 "칸 꼴이 `rva` / `(off<<4)|n` 중 무엇인가" 까지 있어서, 제안하는 `table` 명령에 칸 풀기 방식 인자(`rva` / `abs` / `compact4`)가 있으면 좋겠다.
- 디스어셈블 도구:
  - Ghidra 가 함수로 안 잡은 PDB 함수(`KiSystemCall64`·`KiSystemCall64Shadow`)를 읽는 길이 없다.
  - CET 명령을 Ghidra 가 잘못 읽는다.
  - 둘 다 objdump 로 메웠다. `run_ghidra.sh` 에 `rawdis <바이너리> <rva> <len>` (objdump 감싸기) 같은 명령이 있으면 좋겠다. 제안만.

## 판정

**분석성공.**
- 질문 1~4 모두 `[정적]` 답이 있다.
- 메모리 칸 값·`KiKvaShadow`·`NtContinue` 길 레지스터는 동적 확인 거리로 넘김.

## PE_emulator 쪽에서 고칠 것

1. **로그에 남길 인자 수**:
   - `_workspace/sast/51_syscall-args_table.tsv` 를 읽어 `number → stack_args` 를 쓴다.
   - `stack_args = n` 이면 유저 `RSP+0x28, +0x30, …, +0x20+8n` 의 8바이트 n개를 레지스터 4개(`R10`·`RDX`·`R8`·`R9`) 뒤에 붙여 남긴다 (`KiSystemServiceStart +0xc6 ~ +0xcd`, 사다리 `+0x100 ~ +0x16c`).
   - **PE 파일의 `KiServiceTable` 을 직접 읽어 `& 0xf` 하면 안 된다** — 파일 칸은 그냥 rva 라 아래 4비트가 늘 0. 인자 수는 `KiArgumentTable[번호] >> 2` 에 있다 (`KeCompactServiceTable +0x17 ~ +0x32`).
   - `stack_args = 0` 인 302칸은 실제 인자가 0~4개라 레지스터 4개를 다 남겨도 된다.
2. **번호 풀기**:
   - `EAX & 0xfff` = 표 안 번호, `EAX & 0x1000` = win32k 표 (`KiSystemServiceStart +0x7 ~ +0xf`).
   - 범위 밖이면 `RAX = 0xC000001C` (`+0x967`).
   - ntdll 은 `0x1000` 이상을 안 쓴다 (질문 3).
3. **돌아갈 때 레지스터**:
   - `RAX` = NTSTATUS, `RCX` = 돌아갈 RIP, `R11` = RFLAGS, `RDX = 0`, `R8` = 유저 RSP, `R10 = 0`, `XMM0 ~ XMM5 = 0`.
   - `R9 = 0` (KVA 섀도 꺼짐 기준. 켜진 VM 이면 유저 RBP).
   - 나머지는 그대로.
   - 근거: `KiSystemServiceStart` 조각 `+0x34 ~ +0x81`, 본문 `+0x309`.

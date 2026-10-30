# 2026-10-31 run56 — 시스템 콜 인자 수·출구 레지스터 (Q106, PE_emulator 요청 3): PE 파일의 `KiServiceTable` 칸은 **그냥 함수 rva** 이고, 부팅 때 `KeCompactServiceTable` 이 **`((rva − 표) << 4) | 스택 인자 수`** 로 고쳐 쓴다. 커널은 `SAR 4` 로 함수, `AND 0xf` 로 5번째부터의 8바이트 칸 수를 뽑는다. 보통 출구는 `SYSRET` 직전 `RDX`·`R10`·`XMM0~5` 를 0 으로 지운다 (정적)

## PE_emulator 쪽에서 고칠 것

결과서 (`_workspace/sast/51_syscall-args_result.md`) "PE_emulator 쪽에서 고칠 것" 에 근거 태그·오프셋을 붙였다. 오프셋은 모두 `ms/disassemble/ntoskrnl/<함수>.asm` 의 `+0x` 줄이다 (이 폴더는 커밋 안 함).

### 1. 스택 인자 수 — 어느 칸에서 읽나

- `[정적] run56` **PE 파일의 `KiServiceTable` 칸을 `& 0xf` 하면 늘 0 이다.** 파일 칸은 압축 안 된 함수 rva 그대로다 (489칸 모두 아래 4비트 0).
- `[정적] run56` 에뮬레이터가 **PE 파일에서** 읽는다면 인자 수는 **`KiArgumentTable[번호] >> 2`** 다.
  - `KiArgumentTable` = ntoskrnl rva `0xdb568` (PDB 이름), 1바이트 칸 489개. 값은 "스택 인자 바이트 수 (4바이트 단위)" 꼴이라 `>> 2` 하면 8바이트 칸 수가 된다.
  - 근거: `KeCompactServiceTable.asm` 루프 `+0x1b ~ +0x3d` — `+0x1b MOVZX EAX,byte [RBX]` (인자표 한 바이트), `+0x27 SHR EAX,2`, `+0x21 ~ +0x2a` `EDX = 이미지시작 − 표 + 칸값`, `+0x2d SHL EDX,4`, `+0x30 OR EDX,EAX`, `+0x32 MOV [R11],EDX` (그 칸에 되씀). 부르는 곳 `KiInitializeKernel.asm +0x257` (ret `+0x25c`), 인자 `+0x233 ~ +0x250` (`RCX` = `KiServiceTable`, `RDX` = `KiArgumentTable`, `R8D` = `KiServiceLimit`, `[RSP+0x20]` = 이미지 시작).
- `[정적] run56` 에뮬레이터가 **부팅 뒤 메모리 꼴** (압축 칸) 을 흉내 낸다면 PE_emulator 가 알던 모양 `(오프셋 << 4) | 개수` 가 맞다. 커널이 그 칸을 읽는 줄:
  - `KiSystemServiceStart.asm +0x4d MOVSXD R11,dword [R10+RAX*4]` (부호 있게) → `+0x54 SAR R11,4` → `+0x58 ADD R10,R11` = 함수 주소.
  - `+0xb0 AND EAX,0xf` = 스택 인자 수, `+0xb3 JZ` (0 이면 복사 안 함), `+0xb9 SHL EAX,3` (× 8).
- `[정적] run56` 스택 인자 수 `n` = **레지스터 4개 (`R10`(→`RCX`)·`RDX`·`R8`·`R9`) 다음, 유저 `RSP+0x28` 부터 `RSP+0x20+8n` 까지** 8바이트 `n` 개.
  - 근거: `+0xc6 MOV RSI,[RBP+0x100]` (트랩 프레임 `Rsp` = 유저 RSP), `+0xcd LEA RSI,[RSI+0x20]` (그림자 공간 건너뜀), 복사 사다리 `+0x100 ~ +0x16c` (`MOV RAX,[RSI+k]` / `MOV [RDI+k],RAX` 14쌍, `k` = `0x70` → `0x8`), `+0xf0 ~ +0xfa` 끝에서 `n × 8` 바이트 앞으로 뛰어듦.
  - 로그에 남길 때: `R10, RDX, R8, R9` 뒤에 `[유저 RSP + 0x28 + 8·i]` (`i = 0 … n−1`).

### 2. 번호표 — `_workspace/sast/51_syscall-args_table.tsv`

- `[정적] run56` 머리 1줄 + 489줄 (`0x0` ~ `0x1e8`), 탭 구분, 머리줄은 `#` 없음.
- 열: `number` (16진 `0x43` 꼴) · `nt_function` (커널 PDB 이름) · `stack_args` (= `KiArgumentTable[번호] >> 2`) · `total_args` (= `4 + stack_args`) · `ntdll_stub` (같은 번호를 쓰는 ntdll `Nt*` export, 없으면 빈칸).
- **`stack_args = 0` 인 302칸은 실제 인자가 0 ~ 4개다. 표로는 몇 개인지 못 정한다.**
  - 그 줄의 `total_args` 4 는 "레지스터 4칸" 이라는 뜻이다. 예: `NtClose` 는 1개, `NtContinue` 는 2개인데 둘 다 4.
  - 로그용으로는 레지스터 4개를 다 남기면 된다 (스택 0개).
  - `stack_args > 0` 이면 `total_args` 가 정확한 인자 수다.
- 분포 (`stack_args`: 칸 수): 0: 302 / 1: 72 / 2: 44 / 3: 13 / 4: 12 / 5: 16 / 6: 11 / 7: 9 / 8: 2 / 9: 1 / 10: 2 / 12: 3 / 13: 2. 최대 13 (11·14 없음). 사다리는 14쌍이라 코드상 최대 14.
- 한 커널 주소에 PDB 이름이 여럿인 21칸 (링커가 같은 내용 함수를 하나로 합친 것) 은 ntdll 스텁 이름을 골랐다. 그래서 `nt_function` 489개가 서로 다르다.
  - 예: `0x705290` 한 곳을 12번호가 같이 쓴다 (`0x6d`·`0xb0`·`0xe2`·`0xee`·`0x128`·`0x1a2`·`0x1a3`·`0x1b6`·`0x1b7`·`0x1e2`·`0x1e7`·`0x1e8`). 이 칸들의 이름으로 함수 본문을 가리면 안 된다 (본문은 같음).

### 3. 번호 풀기·빠진 번호

- `[정적] run56` 표 고르기: `KiSystemServiceStart +0x7 ~ +0xc` `EDI = (EAX >> 7) & 0x20` (번호 비트 12 = `0x1000` → 서술자 `[1]` win32k), `+0xf AND EAX,0xfff` (표 안 번호).
- `[정적] run56` 범위 검사 `+0x3e CMP EAX,[R10+RDI+0x10]` (Limit) → `+0x43 JNC +0x907`. win32k 번호가 아니면 `+0x967 MOV EAX,0xc000001c` (STATUS_INVALID_SYSTEM_SERVICE) 로 돌아간다.
- `[정적] run56` ntdll 스텁은 **`0x1000` 이상 번호를 하나도 안 쓴다.** 488개 번호, `0x0` ~ `0x1e8`.
- `[정적] run56` **`0x5a` `NtQuerySystemTime` 는 ntdll 스텁이 없다.** ntdll 의 `NtQuerySystemTime`·`ZwQuerySystemTime` export 는 `E9` JMP 로 `RtlQuerySystemTime` (ntdll rva `0xed060`) 에 넘긴다. 그래서 test.exe 가 ntdll 로 `0x5a` 를 부를 길은 없다. 유저에서 공유 데이터 시계를 읽고 커널에 안 들어가는 길로 보인다 `[추측]`.
  - `NtGetTickCount` 도 같은 꼴 (`E9` → `RtlGetTickCount`, rva `0x145570`) 이고 커널 표에 같은 이름이 없다.
- `[정적] run56` 같은 번호를 쓰는 **내부 스텁 `Np*` 6개** (PDB 이름만, export 아님): `0xf` `NpClose` · `0x23` `NpQueryVirtualMemory` · `0x28` `NpMapViewOfSection` · `0x2a` `NpUnmapViewOfSection` · `0x33` `NpOpenFile` · `0x4a` `NpCreateSection`. 바이트 모양·번호는 export 스텁과 같다. 누가 부르는지는 안 봤다.
  - 에뮬레이터가 "돌아갈 주소 → 스텁 이름" 으로 로그를 남긴다면 이 6개 주소 (결과서 3번 표) 도 이름표에 넣어야 같은 번호가 다른 이름으로 안 찍힌다 `[추측]`.

### 4. 유저로 돌아갈 때 레지스터 (보통 출구 `SYSRET`)

- `[정적] run56` 근거 = `KiSystemServiceStart.asm` 본문 `+0x309`, 조각 `FUN_1406c20cc +0x34 ~ +0xaf` (조각 오프셋 + `0x50c` = 본문 오프셋, 예 `SYSRET` = 본문 `+0x5bb`). `KiKvaShadow` 켜짐 갈래는 `KiKernelSysretExit.asm +0x44 ~ +0x71`.

| 레지스터 | 값 | 근거 (`KiSystemServiceStart`) |
|---|---|---|
| `RAX` | 커널 함수가 돌려준 NTSTATUS (번호가 범위 밖이면 `0xC000001C`) | `+0x391` 트랩 `Rax` 칸에 둠 → 조각 `+0x34 MOV RAX,[RBP-0x50]` |
| `RCX` | 돌아갈 유저 RIP = SYSCALL 다음 명령 (스텁 `+0x14 RET`) | 조각 `+0x60 MOV RCX,[RBP+0xe8]` (트랩 `Rip`) |
| `R11` | 유저 RFLAGS (SYSCALL 때 값) | 조각 `+0x67 MOV R11,[RBP+0xf8]` (트랩 `EFlags`) |
| `RDX` | **0** | 조각 `+0x46 XOR EDX,EDX` |
| `R8` | 유저 RSP (SYSCALL 때 값) | 조각 `+0x38 MOV R8,[RBP+0x100]` (트랩 `Rsp`), `+0x7e MOV RSP,R8` |
| `R9` | **0** (`KiKvaShadow` 꺼짐) / **유저 RBP** (켜짐) | 꺼짐: 조각 `+0x81 XOR R9,R9`, `+0x84 ~ +0x93` 뒤에도 0. 켜짐: 조각 `+0x6e/+0x75` → `KiKernelSysretExit` 는 `R9` 를 안 지움 (`+0x44 MOV RBP,R9` 만) |
| `R10` | **0** (인스트루먼테이션 콜백이 있으면 원래 돌아갈 RIP) | 본문 `+0x309 XOR R10,R10`, 콜백 갈래 `+0x30c ~ +0x34c` |
| `XMM0 ~ XMM5` | **0** | 조각 `+0x48 ~ +0x5c PXOR` |
| `RBX`·`RSI`·`RDI`·`RBP`·`RSP`·MXCSR | 들어올 때 값 그대로 | `+0x1a0 ~ +0x1ae`, `+0x305 LDMXCSR`, 조각 `+0x7b`·`+0x7e` |
| `R12 ~ R15`, `XMM6 ~ XMM15` | 이 길에 쓰는 줄 없음 → 커널 함수가 x64 호출 규약대로 지켜 준다고 봄 | `[추측]` |

- `[정적] run56` 조각 `+0x1b ~ +0x31` (`GS:[0x858] & 0x100` 이면 `DIV RCX`) 은 `RAX`·`RDX`·`RCX` 를 건드리지만 바로 뒤에서 셋 다 다시 채운다.
- `[정적] run56` 인스트루먼테이션 콜백: `KPROCESS +0x168` (`InstrumentationCallback`) ≠ 0 이고 `CS = 0x33` 이면 `R10` = 원래 RIP, 돌아갈 RIP = 콜백 주소 (`+0x30c ~ +0x34c`). test.exe 에서 이 칸 값은 안 봤다.
- **`NtContinue` 는 이 표를 안 쓴다.** `[정적]` run29: 성공하면 `NtContinueEx` 가 반환하지 않고 트랩 프레임에서 휘발성 레지스터를 되살려 (`NtContinueEx +0x3be ~ +0x3ee`) **`IRETQ`** 로 바로 유저에 간다 (flow.md 2.10). 그래서 로더 끝 `NtContinue` 뒤 레지스터는 **맡긴 문맥 값** (`Rcx` = 진입점, `Rdx` = PEB 등) 이다. 실패 (`NtContinueEx +0x47a RET`) 일 때만 위 표 길로 온다 `[정적]` run29 + run56.
  - 위 표의 `+0x2d3 KiRestoreSetContextState` (스레드 플래그 `0x8000000`) 갈래도 문맥을 통째로 바꾸는 쪽으로 보인다 `[추측]`. 실제로 어느 쪽인지 → Q107 (5).
- 이 표에 안 든 길 (이름만): 유저 APC (`+0x248 KiInitiateUserApc`), `+0x2d3 KiRestoreSetContextState`, 피코 프로세스 `KiSystemServiceExitPico` (rva `0x6c21ab`), 커널 모드 호출자 `+0x5be RET`, `INT 0x2e` 갈래. 이 길들에선 값이 다를 수 있다.

### 5. 콜스택 흉내 (참고)

- `[관찰]` run3·run44: 커널 `Nt*` 함수 바로 위 프레임 = **`nt!KiSystemServiceCopyEnd+0x28`**.
- `[정적] run56` 파일 바이트로는 `+0x190 MOV RAX,R10` / `+0x193 CALL RAX` (2바이트) / `+0x195 NOP dword [RAX]` (3바이트) → 돌아올 자리 = `+0x195` = **`KiSystemServiceCopyEnd+0x25`** (이름표 `CopyEnd` = `+0x170`).
- 3바이트 차이는 아직 못 풀었다 (아래 "정적과 동적이 다른 곳"). 에뮬레이터가 실제 콜스택과 맞추려면 **관찰값 `+0x28`** 을 쓰는 게 맞다 (결과서 판단, 이유는 `[추측]`).

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 03_sprint 열넷째 바퀴. plan **Q106** (PE_emulator 요청 3, 사용자 결정 2026-10-31 로 Q104 VM 단계 전에). 짐 지도 바퀴는 아니다.
  - `baggage.md` 엔 `KiServiceTable` 칸 값이나 시스템 콜 출구 레지스터를 적은 줄이 없어 **안 고쳤다** (`_KTHREAD.PreviousMode` 줄의 `KiSystemServiceUser +0x11` 은 다른 이야기).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID+age `C29EBFB06B78B3C020DCA66D99713F9E1`, ntdll (`ntdll.pdb`) `C3093720177DA6851519DCFFE8DF53FE1` — 둘 다 flow.md 기준과 같다 (결과서 `ms/sym/*.pdb.info.txt` 대조).
- 버전 대조 (결과서): `_calls.tsv` `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`, ntdll `LdrpInitializeProcess` call `+0x1bb6` / ret `+0x1bbb` → `LdrpInitializeGraphRecurse`. 둘 다 알려진 값과 같다.

## 목적

- PE_emulator 가 syscall 로그에 레지스터 4개 다음 **스택 인자** 를 몇 개 남겨야 하는지 — `KiServiceTable` 칸 모양 (함수 주소·스택 인자 수 뽑는 법) 과 번호표.
- ntdll 스텁 (`4c 8b d1 b8 imm32`) 이 489칸과 1:1 인가.
- 보통 출구 (`SYSRET`) 에서 휘발성 레지스터에 무엇이 남나 (run29 는 "처리기 본문 안 읽음" 이었다).
- 범위 밖 (요청서): 각 `Nt*` 본문, win32k 표 칸 풀기, 입구의 보안 검사·감사·트레이스 갈래 전체.

## 방법

- 정적: `_workspace/sast/51_syscall-args.md` — ntoskrnl·ntdll. 두 모듈 다 이미 analyze 돼 있어서 다시 안 걸었다.
  - depth 0 export (`ms/disassemble/ntoskrnl/`): `KiSystemService`, `KiSystemServiceUser`, `KiSystemServiceStart`, `KiSystemServiceExitPico`, `KiKernelSysretExit`, `KeCompactServiceTable`, `KiInitSystem`, `KeAddSystemServiceTable`, `PsInitializeWin32kServiceTable`, `KiConvertToGuiThread`. (`KiSystemService` 는 뽑았지만 결과서 답에 안 썼다.)
  - refs (`ms/disassemble/ntoskrnl/_refs/`): `KiServiceTable`, `KiServiceLimit`, `KeServiceDescriptorTable`, `KeServiceDescriptorTableShadow`, `KiSystemCall64`, `KiSystemCall64Shadow`, `KiKernelSysretExit`.
  - 읽기만: `KiInitializeKernel.asm` (`+0x5a`, `+0xf7 ~ +0x25c`), `NtCreateSection.asm` (손 대조), `pdb_struct.py` (`_KTRAP_FRAME`, `_KTHREAD` `+0x78`·`+0x98`·`+0x220`, `_KPROCESS` `+0x158`·`+0x168`).
  - **`KiSystemCall64` (rva `0x6c1840`) 는 Ghidra 함수가 아니라 `.asm` 이 없다.** 결과서가 `objdump -d -M intel` (rva `0x6c1840 ~ 0x6c1bc0`, 출력은 스크래치) 로 봤다. 이 문서의 `KiSystemCall64 +0x..` 는 그 결과서 근거 그대로다.
  - 번호표는 `pefile` 로 `KiServiceTable` (rva `0xdadc0`)·`KiArgumentTable` (rva `0xdb568`) 489칸을 읽어 `KeCompactServiceTable` 식으로 풀고, PDB 공개 심볼 (`llvm-pdbutil-14 -publics`) 과 ntdll 스텁 스캔을 짝지은 임시 스크립트로 만들었다 (스크래치, 레포 밖).
- 결과서: `_workspace/sast/51_syscall-args_result.md` (**분석성공**). 번호표: `_workspace/sast/51_syscall-args_table.tsv`.
- 이 문서를 쓰며 다시 맞춘 것 (flow-writer, `.asm`·tsv):
  - `KiSystemServiceStart.asm` `+0x7 ~ +0x58`, `+0xb0 ~ +0x16c`, `+0x170`, `+0x190 ~ +0x1a0`, `+0x1be`, `+0x248`·`+0x261`·`+0x27f`·`+0x2d3`·`+0x2f3` (call 줄), `+0x305 ~ +0x30c`, `+0x34c`, `+0x391`, `+0x907`·`+0x91f`·`+0x940`·`+0x967`, 조각 `FUN_1406c20cc +0x0 ~ +0xaf`.
  - `KeCompactServiceTable.asm` 전체 (82바이트) — **루프 시작은 `+0x1b`** 다 (결과서의 `+0x17` 은 주소 끝자리 `…a17` 을 오프셋으로 적은 것. 아래 "정정").
  - `KiInitializeKernel.asm +0x233 ~ +0x25c`, `KiInitSystem.asm +0xfa ~ +0x122`, `KiKernelSysretExit.asm +0x44 ~ +0x71`, `KiSystemServiceUser.asm` 끝 (`+0xd8`, rva `0x6c1ae2` + 222바이트 = `0x6c1bc0` = `KiSystemServiceStart` 시작).
  - tsv: 489줄, `stack_args` 분포, `ntdll_stub` 빈칸이 `0x5a` 하나뿐, `0x0`·`0x43`·`0x4a`·`0x52`·`0xd1` 줄.
- 문서: 이 run 문서, `flow.md` (기준·1장 그림 1·그림 2 의 시스템 콜 진입 상자 글·그림 1 노트·2.1 `NtCreateUserProcess`·2.2 새 절·2.10·4장·4.1·5장).

## 결과

비유 (관리사무소 전화, run34 이어서):

- 관리실 (ntdll) 전화기의 단축 번호를 누르면 관리사무소 (커널) 번호부를 편다.
- 번호부 **인쇄본** (PE 파일) 에는 담당자 자리 번호만 적혀 있다.
- 관리사무소가 문을 열 때 (부팅) 따로 있던 "서류 몇 장 가져와야 하나" 표 (`KiArgumentTable`) 를 보고, 번호부 칸마다 **"자리까지 거리 + 서류 장수"** 를 한 칸에 겹쳐 다시 적는다.
- 전화를 받으면 그 칸에서 거리로 담당자를 찾고, 장수만큼 서류를 손님 가방 (유저 스택) 에서 옮겨 담는다.
- 끝나면 답 (`RAX`) 과 돌아갈 문 (`RCX`) 만 쥐여 주고, 쓰던 메모지 몇 장 (`RDX`·`R10`·`XMM0~5`) 은 지워서 돌려보낸다.

### 1. 칸 모양 — 파일 칸과 메모리 칸 [정적]

| | 파일 (PE) | 부팅 뒤 메모리 (계산) |
|---|---|---|
| 칸 크기 | 32비트 | 32비트 |
| 값 | 함수 rva (아래 4비트 0) | `((함수 rva − 0xdadc0) << 4) \| (KiArgumentTable[i] >> 2)` |
| 함수 주소 | 이미지 시작 + 칸 | 표 시작 + (칸 `SAR 4`, 부호 있게) |
| 스택 인자 수 | 칸에 없음 → `KiArgumentTable[i] >> 2` | 칸 `& 0xf` |
| 근거 | 489칸 모두 `_index.tsv` 함수 시작과 같음 (run29·run34 의 방법) | `KeCompactServiceTable +0x1b ~ +0x3d`, 읽기 `KiSystemServiceStart +0x4d ~ +0x58`·`+0xb0` |

- 칸이 가리키는 rva 범위 `0x20a9f0 ~ 0xb6e8b0` — 전부 표 (`0xdadc0`) 보다 뒤라서 고쳐 쓴 뒤 오프셋이 음수인 칸은 없다 (결과서).
- 메모리 값 예 (결과서가 계산. 메모리에서 직접 본 값 아님 → Q107 (1)):

| 번호 | 함수 | 파일 값 | 인자표 바이트 | 메모리 값 (계산) | `& 0xf` |
|---|---|---|---|---|---|
| `0x0` | `NtAccessCheck` | `0x0039d590` | `0x10` | `0x02c27d04` | 4 |
| `0x43` | `NtContinue` | `0x006adde0` | `0x00` | `0x05d30200` | 0 |
| `0x52` | `NtResumeThread` | `0x00a2d600` | `0x00` | `0x09528400` | 0 |
| `0xd1` | `NtCreateUserProcess` | `0x00a01340` | `0x1c` | `0x09265807` | 7 |

- 누가 고쳐 쓰나 [정적]:
  - `KiInitializeKernel +0x257` CALL `KeCompactServiceTable` (ret `+0x25c`). 이 CALL 은 `+0xf7 TEST R15D,R15D` / `+0xfa JNZ` 의 0 쪽에만 있다. `R15D` 는 `+0x5a` 에서 스택 인자로 받는다 — 첫 CPU 번호로 보인다 `[추측]`. 부팅 때 정말 지나갔는지는 정적으로 못 정한다.
  - `KiServiceTable` 을 쓰는 곳은 둘뿐 (`_refs/KiServiceTable.tsv`): `KiInitializeKernel +0x250` (위 CALL 인자), `KiInitSystem +0xfa` (서술자에 주소만).
  - 서술자 채우기 `KiInitSystem`: `+0x101` `KeServiceDescriptorTable+0x0` = 표, `+0x115` `+0x10` = Limit, `+0x122` `+0x18` = `KiArgumentTable` (`+0x11b LEA`). 디스패치 길은 `+0x18` 을 안 읽고 칸 아래 4비트만 쓴다.
  - 루프 뒤 `+0x42` CALL `KiLockServiceTable` (이름만, 표를 잠그는 쪽으로 보임 `[추측]`). win32k 표는 `PsInitializeWin32kServiceTable +0x8e` → `MmCompactServiceTable` 이 따로 누른다 (이름만, 범위 밖).

### 2. 칸을 읽는 길 — `KiSystemServiceStart` (rva `0x6c1bc0`, 1364바이트) [정적]

PDB 이름표 (`pdb_aliases.py --where`, 넷 다 `[none]` 이름표): `KiSystemServiceRepeat` = `+0x14`, `KiSystemServiceCopyStart` = `+0x100`, `KiSystemServiceCopyEnd` = `+0x170`, `KiSystemServiceExit` = `+0x1a0`.

| 줄 | 하는 일 |
|---|---|
| `+0x7 ~ +0xf` | 번호 풀기 (`0x1000` 비트 → 서술자 `[0]`/`[1]`, `& 0xfff`) |
| `+0x14` (`Repeat`) | 기본 서술자 `KeServiceDescriptorTable`. `+0x22 ~ +0x3b` GUI 스레드 (`KTHREAD+0x78` bit 7) 면 `…Shadow`, bit 21 이면 `…Filter` |
| `+0x3e` / `+0x43` | Limit 검사 → 넘치면 `+0x907` (win32k 번호면 `+0x91f KiConvertToGuiThread` 뒤 `+0x940` 에서 `+0x14` 로 다시, 아니면 `+0x967` `0xc000001c`) |
| `+0x49 ~ +0x58` | 표 시작, 칸 `MOVSXD`, `SAR 4`, `ADD` → `R10` = 함수 주소 |
| `+0xb0 ~ +0xb9` | `AND 0xf` → 0 이면 `CopyEnd` 로, 아니면 `× 8` |
| `+0xbc ~ +0xfa` | 커널 받는 곳 `RSP+0x18`(새 `RSP` 기준), 유저 `RSP+0x20` 부터, `MmUserProbeAddress` 로 자름 (`+0xd1 ~ +0xe1`), 사다리 안으로 뛰어듦 |
| `+0x100 ~ +0x16c` (`CopyStart`) | 8바이트 복사 14쌍 |
| `+0x170` (`CopyEnd`) | 트레이스 갈래 `+0x17a` (`KiDynamicTraceMask`) · `+0x18a` (`PerfGlobalGroupMask`) — 이름만 |
| `+0x190 ~ +0x195` | `MOV RAX,R10` / **`CALL RAX`** / `NOP` — 커널 `Nt*` 함수 부르기 |
| `+0x1a0` (`Exit`) ~ 조각 `+0xaf` | 보통 출구 (위 "PE_emulator 쪽에서 고칠 것" 4) |

- 손 대조 [정적] (결과서): `0x43 NtContinue` 0 (`.c` 매개변수 2), `0x52 NtResumeThread` 0 (2), `0xd1 NtCreateUserProcess` 7 (11), `0x19 NtQueryInformationProcess` 1 (5), `0x1d0 NtSystemDebugControl` 2 (6), `0x4a NtCreateSection` 3 (`.c` 는 1개로 잘못 나와 `.asm` 의 `[RSP+0x28/0x30/0x38]` 읽기로 7). 이미 있던 `Nt*.c` 12개 모두 어긋남 없음.
  - 얇은 감싸개 함수는 Ghidra `.c` 매개변수 수가 모자라게 나온다. 인자 수 대조는 `.asm` 의 `[RSP+0x28 이상]` 읽기로 한다.

### 3. 들어가는 길 — `KiSystemCall64` (rva `0x6c1840`, objdump) → `KiSystemServiceUser` → `KiSystemServiceStart` [정적]

- `KiSystemCall64` (오프셋은 결과서 objdump 기준):
  - `+0x0 SWAPGS`, 유저 RSP 를 `GS:[0x10]` 에.
  - `+0x15 ~ +0x23` 트랩 프레임에 `SS 0x2b`, 유저 RSP, `R11` (= RFLAGS), `CS 0x33`, `RCX` (= 돌아갈 RIP).
  - **`+0x3e MOV RCX,R10`** — 첫 인자를 `RCX` 로 되돌림 (스텁 `+0x0 MOV R10,RCX` 의 짝).
  - `+0x55 ~ +0x63` `RBX`·`RDI`·`RSI`, `+0x7f ~ +0x87` `RAX` (번호)·`RCX`·`RDX` 저장.
- `KiSystemServiceUser` (rva `0x6c1ae2`, 222바이트): MXCSR, `R8`·`R9`·`R10`, `XMM0 ~ XMM5` 를 트랩 프레임에 저장 (결과서 objdump `0x6c1b01 ~ 0x6c1b32`). 끝 (`+0xd8 NOP`) 다음 바이트가 바로 `KiSystemServiceStart` (`0x6c1bc0`) 라 그대로 내려간다.
- `[추측]` SYSCALL 이 `KiSystemCall64` 로 오는 것 (MSR 에 이 주소를 넣는 줄) 은 안 봤다. `_refs/KiSystemCall64.tsv` 에 주소를 집는 줄이 셋 있다 (`KiInitializeBootStructures +0x391 LEA RSI` 등). `KiKvaShadow` 가 켜진 기계면 입구가 `KiSystemCall64Shadow` (rva `0xbcb200`, 역시 Ghidra 함수 아님) 일 수 있다 `[추측]` — 안 읽음.

### 4. ntdll 스텁 — 488개, 489칸 중 488칸과 1:1 [정적]

- 실행 섹션 전체에서 `4c 8b d1 b8` 494곳. 뒤 16바이트가 **모두 같다**: `TEST byte [0x7ffe0308],1` / `JNZ +3` / `+0x12 SYSCALL` / `+0x14 RET` / `+0x15 INT 0x2e` / `+0x17 RET`. 뒤 채움만 다름 (493곳 NOP, 1곳 `CC`). 앞에 다른 명령이 붙은 스텁은 없다.
- 494곳 = export 스텁 488 + 내부 `Np*` 6 (위 "고칠 것" 3).
- export: `Nt*` 488 + `Zw*` 488, 전부 짝. `Nt`/`Zw` 로 시작하는 export 983개 중 스텁 아닌 7개: `NtQuerySystemTime`·`ZwQuerySystemTime`·`NtGetTickCount` (`E9` JMP), `NtdllDefWindowProc_A/_W`·`NtdllDialogWndProc_A/_W` (`FF 25` 간접 JMP).
- 한 스텁 주소에 이름 셋: `0x36` (rva `0x161470`) = `NtQuerySystemInformation` + `ZwQuerySystemInformation` + `RtlGetNativeSystemInformation`.
- 남는 1칸 = `0x5a` `NtQuerySystemTime`.

### 호출 관계

```
(아무 스레드, 유저 → 커널 → 유저 — 보통 시스템 콜 한 번)
ntdll!Nt* 스텁   +0x0 MOV R10,RCX · +0x3 MOV EAX,번호
 ══▶ +0x12 SYSCALL                          (0x7ffe0308 bit0 면 +0x15 INT 0x2e — 이번 범위 밖)
nt!KiSystemCall64   (objdump, Ghidra 함수 아님. 여기로 오는 것은 [추측])
 │  +0x0 SWAPGS · +0x15 ~ +0x23 트랩 프레임 · +0x3e RCX ← R10
 ══▶ (그대로 내려감)
nt!KiSystemServiceUser   R8·R9·R10·XMM0~5 저장
 ══▶ (그대로 내려감)
nt!KiSystemServiceStart
 │  +0xf  번호 & 0xfff
 │  +0x14 (Repeat)  서술자 고르기
 │  +0x4d ~ +0x58   칸 SAR 4 → R10 = 함수
 │  +0xb0           칸 & 0xf → 스택 인자 수
 │  +0x100 (CopyStart) ~ +0x16c  유저 RSP+0x28 … 를 커널 스택으로
 └─ +0x195 (= CopyEnd+0x25, 관찰은 +0x28)  CALL RAX → nt!Nt* (표 칸)
 │  +0x1a0 (Exit)   RBX·RDI·RSI 되살림 …
 │  +0x309          R10 = 0
 │  조각 +0x34 ~ +0x67  RAX·R8·R9·RDX=0·XMM0~5=0·RCX·R11
 ├─ ══▶ 조각 +0xaf SYSRET                   KiKvaShadow 꺼짐 (R9 = 0)
 └─ ══▶ 조각 +0x75 JNZ KiKernelSysretExit ══▶ +0x71 SYSRET   켜짐 (R9 = 유저 RBP)

(부팅 때, 흐름도 범위 앞)
KiInitializeKernel   (+0xfa 갈림의 0 쪽만)
 └─ +0x25c  KeCompactServiceTable(KiServiceTable, KiArgumentTable, KiServiceLimit, 0, 이미지 시작)
         │  +0x1b ~ +0x3d  칸 = ((rva − 표) << 4) | (인자표 >> 2)
         └─ +0x47  KiLockServiceTable
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄. `══▶` 는 호출이 아닌 실행 넘어감.

## 확인된 것

- `[정적]` PE 파일 `KiServiceTable` 489칸 = 압축 안 된 함수 rva (run29·run34 가 읽은 방법 그대로 맞음).
- `[정적]` 부팅 때 `KiInitializeKernel +0x257` → `KeCompactServiceTable` 이 칸을 `((rva − 0xdadc0) << 4) | (KiArgumentTable[i] >> 2)` 로 되쓴다 (`+0x1b ~ +0x3d`). 다른 곳에서 표를 고쳐 쓰는 줄은 없다 (`_refs/KiServiceTable.tsv` 2곳).
- `[정적]` 커널은 칸을 부호 있게 읽어 `SAR 4` → 함수, `AND 0xf` → 스택 인자 수 (`KiSystemServiceStart +0x4d ~ +0x58`, `+0xb0`).
- `[정적]` 스택 인자 수 = 레지스터 4개 다음, 유저 `RSP+0x28` 부터의 8바이트 칸 수 (`+0xc6 ~ +0xcd`, `+0x100 ~ +0x16c`). 이 빌드 최대 13, 코드상 최대 14.
- `[정적]` 번호표 489줄 (`_workspace/sast/51_syscall-args_table.tsv`). ntdll export 스텁 488개가 488칸과 1:1, 빠진 번호 `0x5a` 하나, `0x1000` 이상 0개.
- `[정적]` 보통 출구 `SYSRET` 직전: `RAX` NTSTATUS, `RCX` 유저 RIP, `R11` 유저 RFLAGS, `R8` 유저 RSP, `RDX`·`R10`·`XMM0~5` = 0, `R9` = 0 (`KiKvaShadow` 꺼짐) / 유저 RBP (켜짐).
- `[정적]` `KiKvaShadow` 갈림: 시스템 콜 출구는 `KiKernelSysretExit` (`SYSRET`), 첫 스레드 출구 (2.3) 와 `NtContinueEx` 출구 (2.10) 는 `KiKernelExit` (`IRETQ`). **함수는 다르고 갈림 변수는 같다** (결과서 + sast/50 #10).
- `[관찰]` run44 + `[정적]` run56: run44 K1 콜스택의 유저 프레임 `ntdll!NtCreateUserProcess+0x14` = 스텁 `+0x12 SYSCALL` 다음 `+0x14 RET` 자리다. 출구 `RCX` (트랩 `Rip`) 가 가리키는 자리와 같다.
- `[관찰]` run3·run44 + `[정적]` run56: 커널 `Nt*` 위 프레임 이름 `KiSystemServiceCopyEnd` 는 `KiSystemServiceStart` 안 PDB 이름표 (`+0x170`) 이고, 그 뒤 `+0x193 CALL RAX` 가 부르는 자리다. 오프셋은 다르다 (아래).

## 정적과 동적이 다른 곳

- **`KiSystemServiceCopyEnd+0x28` (관찰) vs `+0x25` (파일)** — 3바이트.
  - `[관찰]` run3 (`NtCreateUserProcess` 위), run44 (K1 콜스택 `PspAllocateThread` ← `NtCreateUserProcess+0xe5c` ← `KiSystemServiceCopyEnd+0x28` ← `ntdll!NtCreateUserProcess+0x14`): 커널 `Nt*` 바로 위 = `CopyEnd+0x28`.
  - `[정적]` run56: 파일 바이트 `0x6c1d50` = `49 8b c2 | ff d0 | 0f 1f 00` → `CALL RAX` 다음 = `+0x195` = `CopyEnd+0x25`. `+0x28` 은 `NOP` 끝 (`+0x198`).
  - `[추측]` 메모리에서 `CALL RAX` (2바이트) + 3바이트 `NOP` = 5바이트 자리가 5바이트 직접 CALL (retpoline 류) 로 바뀌었다고 보면 맞아떨어진다. 이 빌드에서 그렇게 바꾸는 코드는 이번에 안 찾았다.
  - 고르지 않고 둘 다 둔다. 확인 → Q107 (2) `u nt!KiSystemServiceCopyEnd+0x20 L3`.
  - 칸 풀기·인자 수 답에는 영향 없다.

## 정정

- **틀린 것은 아니고 덧붙임** — run29·run34 의 "`KiServiceTable` 489칸 모두 `_index.tsv` 함수 시작과 맞음":
  - **PE 파일 기준으로는 맞다** (`>> 4` 없이 rva 로 읽은 게 파일 꼴에 맞는 방법).
  - 다만 부팅 뒤 메모리 칸은 `(오프셋 << 4) | 개수` 압축 꼴이다 (run56). 메모리에서 `dd nt!KiServiceTable` 로 볼 땐 파일 값과 다르게 나올 것이다.
  - 이전 문서 (`runs/02_sprint/run29-*.md`, `run34-*.md`) 는 틀린 게 아니라서 손대지 않았다. flow.md 의 같은 문장 셋 (기준 메모, 2.1 `NtCreateUserProcess`, 2.2 스텁 절, 2.10 `ntdll!NtContinue`) 에 "파일 기준" 과 run56 을 붙였다.
- **결과서 오프셋 하나**: `KeCompactServiceTable` 루프 첫 줄 `MOVZX EAX,byte [RBX]` 는 `+0x17` 이 아니라 **`+0x1b`** 다 (`.asm` `+0x1b 1405b5a17`, `+0x3d JNZ 0x1405b5a17`). 결과서는 주소 끝자리를 적은 것으로 보인다. 이 문서·flow.md 는 `+0x1b ~ +0x3d` 로 썼다. 뜻은 같다.
- flow.md 에서 "시스템 호출 처리기 — 본문 안 읽음, `KiSystemServiceCopyEnd` 는 추측" (그림 1 노트, 그림 2 `nc0` 상자, 2.10 트리, 4.1) → 처리기 본문을 읽었다 (`[정적]` run56). 그림 2 의 `NtContinue` 길에서 `CopyEnd` 를 거친다는 건 이제 코드로 맞다. 다만 그 시스템 콜에서 실제로 거친 걸 본 적은 없어 화살표 `?` 는 그대로 둔다 (아래).

## 아직 확인 필요

→ plan **Q107** (동적, 전역 읽기 위주. 결과서 "동적으로 확인할 것" 번호 그대로):

1. **메모리 칸 꼴**: `dd nt!KiServiceTable+0x43*4 L1` = `05d30200`, `+0xd1*4` = `09265807` 인가. 같으면 "부팅 때 고쳐 씀" 확인. 다르면 `KiInitializeKernel +0xfa` 갈림을 다시 본다.
2. **`CopyEnd+0x28` 3바이트 차이**: `u nt!KiSystemServiceCopyEnd+0x20 L3`. `CALL RAX; NOP` 대신 5바이트 `CALL` 이 보이면 `[추측]` 확인.
3. **`KiKvaShadow` 값**: `db nt!KiKvaShadow L1`. 출구 `R9` 가 여기에 달렸다. Q42 (첫 스레드 `IRETQ` 갈래) 와 같은 변수라 한 번에.
4. **돌아온 직후 레지스터**: test.exe 의 아무 `Nt*` 스텁 `+0x14` (`RET`) 에서 `r` — `rdx = 0`, `r8 = rsp`, `r10 = 0`, `r11 = efl` 인가. 유저 자리 멈춤점이라 프로세스 조건 필요.
5. **`NtContinue` 로 나갈 때** 레지스터가 위 표와 다른가 (`KiRestoreSetContextState` 갈래 / run29 `NtContinueEx` 직접 `IRETQ`). Q29·Q57 과 합쳐 볼 것.

- 추천 멈춤점 (결과서, dast-mentor 참고): `nt!KiSystemServiceCopyEnd+0x20` (`MOV RAX,R10`, `R10` = 함수 주소, 복사 끝난 뒤), `nt!KiSystemServiceStart+0x5b` (`ADD R10,R11` 바로 뒤), ntdll `Nt*` 스텁 `+0x14`.
- 정적으로 남은 것 (지금은 안 함): `KiSystemCall64Shadow` 본문, SYSCALL 이 `KiSystemCall64` 로 오는 MSR 설정 줄 (`KiInitializeBootStructures +0x391` 후보), `INT 0x2e` 갈래 (`KiSystemService`), `Np*` 내부 스텁을 누가 부르나.

## 도구 메모

결과서 "스크립트 문제"·"도구 메모" 그대로 (하네스는 안 고침, 제안은 오케스트레이터 보고에만):

- **`KiSystemCall64` 가 Ghidra 함수가 아니다.** `pdb_aliases.py --where ntoskrnl.exe KiSystemCall64` = `rva=0x6c1840 [function]` 인데 `_index.tsv` 에 없고 어느 함수 범위에도 안 든다. `KiSystemCall64Shadow` (`0xbcb200`) 도 같다. 둘레엔 9바이트짜리 가짜 함수 (`FUN_1406c196c` 등) 31개 (RSB 채우기 사다리). 그래서 `run_ghidra.sh export '^KiSystemCall64$'` 로 안 나오고, 이번엔 `objdump` 로 봤다.
- **Ghidra 11.3.2 가 F3 접두 CET 명령을 잘못 읽는다.**
  - `KiSystemServiceStart.asm` 조각 `+0x84` `RDSSPD ECX` → 바이트 `f3 49 0f 1e c9` = **`RDSSPQ R9`**. `+0x8e` `XSAVEOPT [R9]` → `f3 41 0f ae 31` = **`CLRSSBSY [R9]`**.
  - `KiKernelSysretExit.asm +0x59` / `+0x63` 도 같은 꼴 (`RDSSPQ RSP` / `CLRSSBSY [RSP]`).
  - 그대로 읽으면 "`RCX` (유저 RIP) 를 덮어쓴다" 로 잘못 읽게 된다. 출구 레지스터 표의 `R9` 는 objdump 기준이다.
  - 이 문서·flow.md 에 그 두 줄을 인용할 땐 바이트 기준 이름 (`RDSSPQ`·`CLRSSBSY`) 을 쓰고 Ghidra 표기를 괄호로 붙였다.
- 함수 포인터 표 (`KiServiceTable`·`KiArgumentTable`) 를 읽는 하네스 명령이 없어 이번에도 `pefile` 임시 스크립트 (sast/29 제안과 같음).

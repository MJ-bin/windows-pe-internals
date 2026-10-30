# 51 syscall-args — 정적 분석 요청

- 날짜: 2026-10-31
- 스프린트: 03_sprint
- 대기열: **Q106** (PE_emulator 요청 3. 사용자 결정 2026-10-31: Q104 VM 단계 전에 먼저)
- 관련: run29 (`KiServiceTable` rva `0xdadc0`, 489칸, `[0x43]` = `NtContinue`), run34 (`[0xd1]` `NtCreateUserProcess`, `[0x52]` `NtResumeThread`, ntdll 스텁 `+0x12 SYSCALL`), run3·run44 (`KiSystemServiceCopyEnd+0x28` 에서 커널 함수로 들어감 `[관찰]`)

## 배경

- `../PE_emulator`가 syscall 입구(`KiSystemCall64` 자리)를 만들고 있다.
  - 지금은 이벤트 로그에 레지스터 인자 4개(`R10`·`RDX`·`R8`·`R9`)만 남긴다.
  - 5번째부터는 유저 스택에 있는데, syscall마다 몇 개인지 몰라서 못 남긴다.
- PE_emulator가 아는 "일반적인 모양"은 다음과 같다. 우리 쪽에서 확인한 적은 없다 → `[추측]`
  - 칸 = 32비트
  - 위 비트 = 함수까지 상대 오프셋 (`>> 4`)
  - 아래 4비트 = 스택 인자 수
- 주의: run29 결과서는 "489칸 모두 `_index.tsv` 함수 시작과 맞음"이라고 적었다. 그때 칸을 어떻게 풀었는지(`>>4` 를 했나, 파일에 든 값이 다른 꼴인가)부터 다시 확인할 것.
  - 파일 속 값과 부팅 뒤 메모리 값이 다를 수 있다 (부팅 때 커널이 표를 고쳐 쓰는 경우). 이 빌드에서 그런 코드가 있는지도 본다.

## 알고 싶은 것

1. **칸 모양** (본론): 26100.9457 `KiServiceTable` 칸 하나에서 (a) 함수 주소, (b) 스택 인자 수를 어떻게 뽑나?
   - 근거 = 커널이 그 칸을 읽는 asm 줄. `KiSystemServiceStart`·`KiSystemServiceRepeat`·`KiSystemServiceCopyStart`/`CopyEnd` 쪽에서 `SAR`/`SHR`/`AND 0xf` 같은 줄의 오프셋.
   - 파일 속 값을 부팅 때 고쳐 쓰는 코드(예: `KiInitializeKernel`·`KeAddSystemServiceTable` 류)가 있나? 있으면 메모리 값 꼴도.
   - 스택 인자 수가 "스택으로 복사할 8바이트 칸 수" 인지 (4개 레지스터 인자 빼고) 확인. 복사 루프(`KiSystemServiceCopyStart` 의 펼친 MOV 줄)에서 칸 수를 어떻게 쓰나.
2. **번호표 (기계용 파일)**: 번호 → (커널 함수 이름, 스택 인자 수, 전체 인자 수 = 4 + 스택 인자 수(?)) 489줄 tsv.
   - 위치 제안: `_workspace/sast/51_syscall-args_table.tsv` (MS 코드가 아니라 번호·이름·숫자 표라 커밋 가능. 문제 있으면 다른 자리를 제안).
   - 열: `number(hex)`, `nt_function`, `stack_args`, `total_args`, `ntdll_stub`(질문 3에서 짝지은 이름, 없으면 빈칸).
   - 몇 줄은 손으로 대조: `0x43 NtContinue` (인자 2 → 스택 0?), `0xd1 NtCreateUserProcess` (인자 11 → 스택 7?), `0x52 NtResumeThread` (인자 2). 함수 원형 인자 수는 디컴파일 C 의 매개변수 수로 대조 (`[정적]`).
3. **ntdll 스텁과 1:1 인가**: ntdll `Nt*`(그리고 `Zw*`) export 중 스텁 모양 `4c 8b d1 b8 imm32` (`MOV R10,RCX` / `MOV EAX,imm32`) 인 것.
   - 개수, 489칸과 1:1인지. 빠진 번호 (ntdll 에 스텁이 없는 번호), 한 번호에 이름 여럿 (`Nt*`/`Zw*` 짝 말고), 모양이 다른 스텁 (예: 앞에 다른 명령) 이 있으면 목록.
   - `0x1000` 이상 (win32k 표) 번호가 ntdll 에 있는지 — 있으면 따로 센다 (`KiServiceTable` 밖).
4. **유저로 돌아갈 때 휘발성 레지스터**: `KiSystemCall64` (이 이름이 `_index.tsv` 에 없으면 `pdb_aliases.py --where ntoskrnl KiSystemCall64` / `KiSystemCall64Shadow`) → `KiSystemServiceExit` 길 (`SYSRET` 직전) 에서 `RCX`·`R11`·`RAX`·`RDX`·`R8`·`R9`·`R10`·XMM 에 무슨 값을 넣나 (그대로 둠 / 0 으로 지움 / 유저 RIP·RFLAGS 등).
   - `KiKvaShadow` 갈래 (`KiKernelExit` 쪽) 가 따로 있으면 둘 다. sast/50 #10 과 같은 갈림이면 그 근거만 가리킨다.
   - 보통 반환만. APC 배달·`IRETQ` 로 가는 길은 이름만 적는다.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^KiSystemCall64$`, `^KiSystemCall64Shadow$` (없으면 PDB 이름 찾기) | 0 | 질문 1·4 — 입구 |
| ntoskrnl | `^KiSystemServiceUser$`, `^KiSystemServiceStart$` (rva `0x6c1bc0`, 1364바이트 — 안에 `Repeat`·`CopyStart`·`CopyEnd`·`Exit` 이름표가 있을 수 있음) | 0 | 질문 1·4 |
| ntoskrnl | `KiSystemServiceExit`·`KiSystemServiceCopyStart`·`KiSystemServiceCopyEnd`·`KiSystemServiceRepeat` (PDB 이름표) | 0 | 위 함수 안 이름표면 오프셋만 |
| ntoskrnl | `KiServiceTable`·`KiServiceLimit`·`KeServiceDescriptorTable` 를 쓰는 곳 (`run_ghidra.sh refs`) | — | 질문 1 — 부팅 때 고쳐 쓰나 |
| ntoskrnl | PE 파일 `KiServiceTable` (rva `0xdadc0`) 489칸 | — | 질문 2 — run29·run34 처럼 임시 스크립트 (스크래치, 레포 밖) |
| ntdll | export 표 + 스텁 바이트 | — | 질문 3 — 임시 스크립트 |

## 지도에서 본 것

- ntoskrnl `_map` 에 `KiSystemCall64` 줄기는 없다 (지도는 `NtCreateUserProcess`·`PsLocateSystemDlls` 뿌리만). 새 지도는 필요 없고 depth 0 export 로 충분.
- `_index.tsv`: `KiSystemServiceUser` `0x6c1ae2` (222), `KiSystemServiceStart` `0x6c1bc0` (1364), `KiSystemServiceExitPico` `0x6c21ab` (419). `KiSystemCall64` 이름은 `_index.tsv` 에 바로 안 보임.

## 이미 아는 것

- `[정적]` run29: `KiServiceTable` rva `0xdadc0`, `KiServiceLimit` = 489, `[0x43]` = `NtContinue`.
- `[정적]` run34: ntdll `NtCreateUserProcess` = `SYSCALL 0xd1`, `NtResumeThread` = `SYSCALL 0x52`. 스텁은 `0x7ffe0308` bit 0 이면 `INT 0x2e` 로 가는 갈래가 있음.
- `[관찰]` run3·run44: 커널 함수 바로 위 프레임 = `nt!KiSystemServiceCopyEnd+0x28`.
- `[추측]` PE_emulator: 칸 = `(오프셋 << 4) | 스택 인자 수`.
- `[정적]` sast/50 #10: `KiKvaShadow` 비트 0 → `KiKernelExit+0xa5` / `KxStartUserThread+0x3e1` (첫 스레드 길).

## 받고 싶은 것

- 질문별 답 + 근거 (asm 경로 + `+0x` 줄).
- 기계용 tsv (질문 2) + 그 파일을 만든 방법 한 줄 (스크립트는 레포에 넣지 않음).
- 맨 끝에 **"PE_emulator 쪽에서 고칠 것"** 절 (run55 처럼): 로그에 남길 인자 수를 어떻게 읽으면 되는지, 휘발성 레지스터에 무엇을 넣어야 하는지 한두 줄씩.
- 범위 밖 (하지 말 것): 각 `Nt*` 함수 본문 분석, win32k 표 (`W32pServiceTable`) 칸 풀기 (번호가 ntdll 에 있는지만 셈), syscall 입구의 보안 검사·감사·트레이스 갈래 전체 (이름만).
- 하네스 메모: run34 에서 "함수 포인터 표를 읽는 명령이 없다" 제안이 있었다. 이번에도 임시 스크립트를 쓰면 결과서 "도구 메모" 에 한 줄 (하네스는 안 고침).

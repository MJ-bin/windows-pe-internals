# 107 tls-lock-small — 정적 분석 결과

- 요청서: `_workspace/sast/107_tls-lock-small.md` (Q74, 정적만. 판정 **분석성공**)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`, pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1`), 질문 1·4 에서 ntoskrnl (`ntoskrnl.exe`, pdb guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` = 기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`).
- analyze 다시 안 함 (ntdll `.gpr` + `_index.tsv` + `SetPdb:` 줄 다 있음). **ntoskrnl Ghidra 프로젝트는 안 열었다** (요청대로). 커널 쪽은 이미 있던 `.asm` 과 `ms/sym` 의 PDB (`llvm-pdbutil-14` 로 파일 읽기), 스크래치 objdump 풀이 (읽기만).
- 요청서 네 함수 (`RtlAbPreAcquire`, `RtlpAcquireSRWLockSharedContended`, `LdrpAcquireSchedulerSharedDataSlot`, `LdrpAllocateSchedulerSharedData`) 는 `.asm`·`.c` 가 **이미 있어서 다시 안 뽑음**.
- 새로 뽑은 것 (ntdll, depth 0): `^(RtlTryAcquireSRWLockShared|RtlAbPostRelease|RtlpAbFreeKernelEntry|RtlCompleteProcessCloning)$` 4개 (4.5초, `ms/ghidra/ntdll.export.log`). `RtlAbPreAcquire` 의 다른 부르는 쪽과 "목록을 다시 만드는 곳" 줄을 `.asm` 으로 대조하려고. `_calls.tsv` 는 줄만 더해짐 (다른 줄 안 바뀜, 백업 `…/scratchpad/_calls107.bak`).
- 버전 대조 (`_calls.tsv`): `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x16b → LdrpCallTlsInitializers` / `+0x19c → LdrpCallInitRoutine`, `NtCreateUserProcess +0xc36 → PspAllocateProcess` **일치**. 커널 pdb GUID 일치.
- 스크립트 문제: 없음.
- 숫자 "N곳" 은 **검색한 꼴 기준**이다 (objdump 풀이·바이트 꼴 검색). 꼴 밖은 못 셈.

## 한눈에

| # | 한 줄 답 (정적) | 지금 우리 (PE_emulator) |
|---|---|---|
| 1 (a) bit 0 | **ntdll 에 켜는 줄 0곳.** 켜는 곳은 **커널 `KeWaitForAlertByThreadId +0x2db`** (`NtWaitForAlertByThreadId` 몸). 유저 모드 대기에 들어갈 때, 기다리는 주소와 같은 락 주소가 내 칸에 있으면 그 칸에 bit 0 을 넣는다. 끄는 곳 `KiAbUmPostWait +0x88`. run41 의 `[공백]` 이 풀림. | 영향 없음 (맞음). 우리는 TEB `+0x1850` = 0 이라 칸 쓰기가 없고, 진짜도 시동 중 대기가 없으면 bit 0 은 0. |
| 2 (b) `+0x126` | `RtlAbPreAcquire` 는 **전역을 안 본다.** 둘째 인자 `RDX` 만 본다: `≠ 0` 이면 그대로 돌려주고 끝 (`+0x3 TEST` / `+0x6 JNZ` → `+0x43`). 부르는 쪽은 `R14 ≠ 0` 일 때만 부르고 (`+0x11b`), **반환값을 안 쓴다** (`+0x12b` 에서 `EAX` 덮음). 그래서 늘 효과 없음. 왜 짰는지는 코드로 못 정함 `[추측]`. | 영향 없음 (맞음). 우리는 칸 = 0 이라 `R14 = 0` → 호출 자체가 안 생김 (진짜는 `≠ 0` 이면 부르지만 효과 없음). |
| 3 (c) 두 조건 | ① 목록이 비어 있지 않음 = `LdrpAllocateSchedulerSharedData` 가 노드 하나 (`0x18`B, `+0x10` = **스케줄러 공유 데이터 섹션 핸들**) 를 끼움. 그 안 syscall 은 `NtSetInformationProcess(-1, 클래스 0x70, &핸들, 8)` (실패 땐 `NtClose`). ② PEB `NtGlobalFlag2 & 0x10` 꺼짐 = "이 기능 안 끈다". **test.exe 는 둘 다 통과** (run44 P1a 사진: 목록 노드 1개, `NtGlobalFlag2 = 0`, TEB `+0x1850 = 0x25996cb0000`). | 부분 틀림 (이미 기록된 갈라짐). 우리는 Allocate 가 노드를 끼우지만 핸들 0, TEB `+0x1850` = 0 (진짜 `≠ 0`). 조건 둘 통과 길은 같음. |
| 4 (d) 클래스 이름 | **PDB 에 없음.** `ntdll.dll.pdb`·`ntoskrnl.exe.pdb` 에 `THREADINFOCLASS` enum 없음 (다른 PDB 27개 포함 문자열 검색도 0). 커널 `NtSetInformationThread` 점프표로: **0x38** = "내 AutoBoost 칸 N 의 커널 항목을 반납하고 칸 bit 63 끄기", **0x39** = "이 스레드에 공유 데이터 칸 표를 배정하고 표의 유저 주소를 돌려주기". 0x3a 이상은 `0xC0000003`. | 이름만의 문제 (코드 영향 없음). `[추측]` 표시 유지. |

## 질문별 답 [정적]

### 1 (a) 칸 bit 0 을 켜는 곳

근거 파일: ntdll 은 `RtlAcquireSRWLockShared.asm`·`RtlAcquireSRWLockExclusive.asm`·`RtlEnterCriticalSection.asm`·`LdrpCallTlsInitializers.asm` 등 `.asm` 과 스크래치 objdump 전수 (아래), 커널 `ms/disassemble/ntoskrnl/KeWaitForAlertByThreadId.asm`, `KiAbUmPostWait.asm`, `KiAbProcessPreContextSwitch.asm`.

**대상 칸**: run41 그대로. TEB `+0x1850` (`SchedulerSharedDataSlot`) 이 가리키는 8칸 표의 한 칸 (8바이트). 칸 = 락 주소 (`& 0x7ffffffffffffffc`) + 아래 bit 0·1 + 맨 위 bit 63.

**ntdll 전수 — 켜는 줄 0곳.**
- 이 칸을 건드리는 함수 = TEB `+0x1850` 을 읽는 함수 **56개** (objdump 풀이에서 `0x1850` 이 든 줄 92 = 읽기 91 (`mov` 72 + `sub` 19) + 쓰기 1 `LdrpAcquireSchedulerSharedDataSlot +0x92`). 표 주소를 받는 길은 이것뿐이다 (인자로 칸 포인터를 받는 `RtlpAcquireSRWLockSharedContended`·`RtlpAcquireSRWLockExclusiveContended` 는 아래).
- 칸에 쓰는 모양은 셋뿐이다:
  1. 빈 칸 (값 0) 을 찾아 **락 주소 그대로** 적기 (`mov [칸], 락`). 예 `RtlAcquireSRWLockShared +0x39`, `RtlAcquireSRWLockExclusive +0x32`, `RtlEnterCriticalSection +0x4a`, `RtlAbPreAcquire +0x3a`, `LdrpCallTlsInitializers +0x63` (= `&LdrpTlsLock`). 락 주소는 정렬돼 있어서 bit 0 = 0. 56개 함수의 저장 줄을 하나씩 훑어, 저장하는 레지스터가 락 포인터 (함수 인자·`lea` 로 만든 `&락`) 임을 확인함. 비트를 얹은 값을 저장하는 줄은 없다. (스크래치 `…/scratchpad/bit0scan2.py` 출력)
  2. **bit 1 켜기**: `or byte [칸], 2` — **22곳**, 즉시값은 전부 `0x2` (`LdrpCallTlsInitializers +0x1a7`, `RtlAbPostRelease +0x3c`, `RtlEnterCriticalSection +0x68`, `RtlReleaseSRWLockShared +0x1c0` 등).
  3. 비우기 `mov [칸], 0` (`LdrpCallTlsInitializers +0x1ed` 등).
- 즉시값 `0x1` 로 `or`/`bts` 하는 줄은 이 56개 함수 안에 칸 대상으로는 **0곳**. `lock bts [락], 0` 이 여럿 있지만 락 값 자체의 bit 0 (잡기 비트) 이다 (run41 과 같음). `or BYTE [..],1` 이 보이는 `TppWorkCallbackPrologRelease +0x618`, `LdrpBuildForwarderLink +0x11c` 등은 힙에 할당한 노드 플래그라 칸이 아니다.
- 넘겨받는 칸 포인터: `RtlpAcquireSRWLockSharedContended` 는 `RDX` → `R14` 를 `+0x21` 에서 한 번 받고 `+0x11b ~ +0x120` 에서 `RtlAbPreAcquire` 에만 넘긴다 (칸을 안 씀). `RtlpAcquireSRWLockExclusiveContended` 는 `RDX` 를 홈 칸에 저장만 하고 (`+0x7`) 안 쓴다 (`+0x2cf` 의 `[RSP+0x88]` 은 `RtlBackoff` 류 스핀 변수 주소).
- **다른 모듈**: `ms/bin/.../System32` 의 PE 4119개에서 "GS 로 TEB 를 읽고 `[reg+0x1850]` 을 읽는" 꼴을 바이트 검색 → **ntdll.dll 만** (91곳). kernel32·KernelBase·ucrtbase·test.exe·dll1·dll2 는 0 (objdump 로 따로 봄; KernelBase 의 `0x1850` 7줄은 RIP 상대 오프셋이라 무관). 한계: `r12`·`rsp` 기반 SIB 꼴은 이 검색이 못 잡는다.

**켜는 곳 = 커널 `nt!KeWaitForAlertByThreadId +0x2db`.** (run41 은 `KiAb*`·`KeAb*` 만 봐서 못 찾았다.)
- 부르는 길: ntdll `NtWaitForAlertByThreadId` syscall → 커널 `NtWaitForAlertByThreadId` (rva `0x9fc370`, `.asm` 없음 — 스크래치 objdump: `+0x9 MOV R8,RCX` 주소, `+0x1e MOVZX ECX,byte [RCX+0x232]` 이전 모드, `+0x2f CALL KeWaitForAlertByThreadId`) → `KeWaitForAlertByThreadId(모드, 시간, 주소)`.
  - `.asm`: `+0x2c MOV R15,R8` (주소) · `+0x36 MOV R13B,CL` (모드).
  - ntdll 쪽 부르는 곳 예: `RtlpAcquireSRWLockSharedContended +0x113 MOV RCX,RSI` (RSI = 락) → `+0x116 CALL NtWaitForAlertByThreadId`.
- 켜는 조건 (`KeWaitForAlertByThreadId.asm`):
  1. `+0x57 TEST R15,R15` / `+0x5a JZ` — 주소가 0 아님.
  2. `+0x5c CMP R13B,SIL` / `+0x5f JNZ` — 모드가 1 (유저 모드). `SIL` = 1 (`+0x52 MOV ESI,1`).
  3. `+0x62 MOV RDX,[RDI+0x320]` / `+0x69 TEST RDX,RDX` / `+0x6c JNZ +0x29b` — 이 스레드에 표 (`_KTHREAD +0x320 SchedulerSharedSystemSlot`) 가 있음.
  4. `+0x29b ~ +0x2c6` 칸 0~7 을 돌며 `+0x2b4 MOV RAX,[RDX+R8*8]` → `+0x2b8` 복사 → `+0x2bc AND RAX,R9` (`R9 = 0x7ffffffffffffffc`) → `+0x2bf CMP RAX,R15` / `+0x2c2 JZ +0x2db` — **칸의 락 주소 == 기다리는 주소**.
  5. 맞는 칸에서: `+0x2db OR byte [RBP-0x20],SIL` (복사본 bit 0 켜기) → `+0x2df MOV RAX,[RBP-0x20]` → `+0x2e3 MOV [RDX+R8*8],RAX` (칸에 도로 씀).
- 끄는 곳: 깨어난 뒤 `KeWaitForAlertByThreadId +0x1de CALL KiAbUmPostWait(스레드, 주소)` (`+0x1d8 MOV RDX,R15`) → `KiAbUmPostWait +0x70 ~ +0x7e` 같은 칸 찾기 → `+0x88 AND byte [RSP+0x50],0xfe` (**bit 0 지움**) → `+0xd2 AND byte [RSP+0x57],0x7f` (bit 63 지움) → `+0xdc MOV [R14+RBP*8],RAX` 도로 씀. **락 주소 부분은 그대로 둔다.**
- 켜진 bit 0 을 커널이 쓰는 곳: `KiAbProcessPreContextSwitch +0x25f TEST byte [RSP+0x98],1` / `+0x269 MOV byte [R15+0x19],1` (커널 항목 `+0x19` 에 복사) 와 `+0x29f TEST DIL,1` (이미 bit 63 인 칸을 다시 볼지). `KiAbUmPostWait +0x9d CMP byte [RBX+0x19],0` / `+0xa1 JZ` — 항목 `+0x19` 가 켜진 것만 깨어난 뒤에 풀어 준다.
- **뜻** `[추측: 이름]`: bit 0 = "이 스레드가 이 락 때문에 **유저 모드로 대기에 들어간다**" 표시. 커널이 대기 직전에 켜고, 컨텍스트 스위치 때 항목 `+0x19` 로 옮기고, 깨어나면 지운다. 이름은 PDB 에 없다.
- 커널에서 이 표를 해석하는 함수 (락 주소 마스크 + `+0x320` 동시 사용): `KiAbProcessPreContextSwitch`, `KiAbProcessPostContextSwitch`, `KeWaitForAlertByThreadId`, `KiAbUmPostWait` **넷** (+ `KeAbUserModeEntryFree` 는 `+0x320` 만). 칸 비트를 바꾸는 줄은: bit 63 켜기 `PreContextSwitch +0x26e`, bit 63 끄기 `PostContextSwitch +0x329`·`KiAbUmPostWait +0xd2`·`KeAbUserModeEntryFree +0x96`, **bit 0 켜기 `KeWaitForAlertByThreadId +0x2db`**, bit 0 끄기 `KiAbUmPostWait +0x88`. (이 넷은 커널 전체 objdump 에서 마스크 상수 `0x7ffffffffffffffc` 를 찾은 줄의 함수 목록과 `[…+0x320]` 읽는 함수의 교집합이다.)
- **test.exe 에서는**: 시동 중 TLS 락을 기다리는 일은 없을 것 `[추측]` (`LdrpCallTlsInitializers +0x6d CMPXCHG` 가 바로 성공). 그러면 bit 0 은 한 번도 안 켜진다. run44 P1a 사진에서도 8칸이 전부 0 (`ms/logs/04_baggage-read_P1a.txt` 3441 ~ 3445줄).

**표 칸 모양 (run41 표에 채울 것)**

| 비트 | 뜻 | 켜는 곳 | 끄는 곳 | 표시 |
|---|---|---|---|---|
| bit 0 | 유저 모드 대기 중 (이름 `[추측]`) | **커널 `KeWaitForAlertByThreadId +0x2db`** | 커널 `KiAbUmPostWait +0x88` | 위치 `[정적]`, 뜻 `[추측]` |

### 2 (b) `RtlpAcquireSRWLockSharedContended +0x126` 의 `RtlAbPreAcquire`

근거 파일: `ms/disassemble/ntdll/RtlAbPreAcquire.asm` (`+0x0 ~ +0x46`), `RtlpAcquireSRWLockSharedContended.asm`, `RtlTryAcquireSRWLockShared.asm`, `RtlAcquireSRWLockShared.asm`.

**`RtlAbPreAcquire(락 RCX, 칸 RDX)` 가 보는 것 — 인자 하나뿐이다. 전역 칸·플래그는 없다.**
- `+0x0 MOV R10,RCX` (락 보관) → `+0x3 TEST RDX,RDX` → `+0x6 JNZ +0x43` → `+0x43 MOV RAX,RDX` / `+0x46 RET`.
  - 즉 **칸 (`RDX`) 이 0 이 아니면 그것을 그대로 돌려주고 끝** (읽기만, 쓰기 0).
- `RDX == 0` 일 때만 본격 일을 한다: `+0x8 MOV RDX,GS:[0x30]` → `+0x13 MOV R8,[RDX+0x1850]` (표) → `+0x1a TEST R8,R8` / `+0x1d JZ +0x35` (표 없으면 0 돌려줌) → `+0x21 ~ +0x30` 칸 0~7 중 **값 0 인 빈 칸** 찾기 (`+0x28 CMP [R8+RCX*8],RAX(0)`) → `+0x3a MOV [RAX],R10` (**락 주소 적기**) → 그 칸 주소를 돌려줌. 8칸이 다 차면 0.
- 그래서 이 함수는 "칸이 없으면 새로 잡고, 이미 있으면 그대로" 라는 모양이다. 다른 부르는 쪽 `RtlTryAcquireSRWLockShared +0x8 XOR EDX,EDX` → `+0x11 CALL` (칸 = 0 → 진짜 일을 함) → `+0x16 MOV R9,RAX` → 실패하면 `+0x4e CALL RtlAbPostRelease(락, R9)` 로 칸을 되돌림.

**`+0x126` 호출은 늘 "그대로 돌려주는" 쪽이다.**
- `RtlpAcquireSRWLockSharedContended.asm`: `+0x21 MOV R14,RDX` (칸, 이 함수 안에서 **여기서 한 번만 쓰임**; 이후 `R14` 읽기는 `+0x11b`·`+0x120` 둘뿐) · `+0x28 MOV RSI,RCX` (락) · `+0x113 MOV RCX,RSI` / `+0x116 CALL NtWaitForAlertByThreadId` · **`+0x11b TEST R14,R14` / `+0x11e JZ +0x12b`** (칸이 0 이면 호출 건너뜀) · `+0x120 MOV RDX,R14` / `+0x123 MOV RCX,RSI` / `+0x126 CALL RtlAbPreAcquire` · `+0x12b MOV EAX,[RBP-0xc]`.
- 따라서 부를 때 `RDX = R14 ≠ 0` → `RtlAbPreAcquire` 는 `+0x43` 으로 가서 `RAX = RDX` 만 하고 나온다. 그리고 부르는 쪽은 **`RAX` 를 안 쓰고** (`+0x12b` 가 `EAX` 를 덮어씀), `R14` 도 안 바꾼다. → **늘 효과 없음. 칸·락·전역 어느 것도 안 바뀐다.**
- 칸을 어디서 받나: `RtlAcquireSRWLockShared +0xd XOR EDX,EDX` → `+0xf ~ +0x31` 빈 칸 찾기 → `+0x39 MOV [R9],RCX` (락 주소 적기) → `+0x56 MOV R8,RAX` / `+0x59 CALL RtlpAcquireSRWLockSharedContended` (`RDX` = 찾은 칸, 못 찾으면 0). `LdrpCallTlsInitializers +0x26 ~ +0x63` 도 같은 꼴로 `RDX` = 빈 칸을 만들어 `+0x113` 에서 넘김.
- **왜 이렇게 짰나는 코드만으로 못 정한다.** 정황 둘:
  1. 깨어난 뒤에도 칸의 락 주소는 그대로다: 커널 `KiAbUmPostWait` 는 bit 0·bit 63 만 지우고 락 주소 부분은 안 건드린다 (`+0x88`, `+0xd2`, `+0xdc`). 그래서 "깨어난 뒤 칸을 다시 적을" 필요가 없다.
  2. 배타 쪽 `RtlpAcquireSRWLockExclusiveContended` 에는 같은 호출이 **없다** (`NtWaitForAlertByThreadId` 는 `+0x146` 에서 부르지만 `RtlAbPreAcquire` 호출 0). 공유 쪽만 남은 흔적이라 `[추측]`: "칸이 있으면 그대로, 없으면 새로" 헬퍼를 방어적으로 부른 코드가 컴파일 뒤에도 남았다. 증명은 못 함.
- **test.exe 에서**: 이 호출은 칸 값에 달린 게 아니라 **인자 `R14 ≠ 0` 에 달렸다.** test.exe 는 TEB `+0x1850 ≠ 0` (P1a 사진) 이라 TLS 락 칸 `RDX` 는 `≠ 0` → 락을 기다려서 깨어나는 일이 있으면 호출은 생기지만 효과는 0. 시동 중엔 TLS 락 경쟁이 없을 것 `[추측]` (위 1 (a) 와 같은 이유) → `RtlpAcquireSRWLockSharedContended` 자체가 안 불릴 것.

### 3 (c) `LdrpAcquireSchedulerSharedDataSlot` 진입 조건 둘 + `LdrpAllocateSchedulerSharedData` 본문

근거 파일: `ms/disassemble/ntdll/LdrpAcquireSchedulerSharedDataSlot.asm`, `LdrpAllocateSchedulerSharedData.asm`, `LdrpInitializeProcess.asm`, `RtlCompleteProcessCloning.asm`, 커널 `NtSetInformationProcess.asm`·`MmCreatePeb.asm`·`InitBootProcessor.asm`, 사진 `ms/logs/04_baggage-read_P1a.txt`.

**`LdrpAcquireSchedulerSharedDataSlot(TEB)` 흐름** (`.asm`):
- `+0xf ~ +0x1a` `RtlAcquireSRWLockShared(&LdrpSchedulerSharedDataListHeadLock)` → `+0x1f MOV RBX,[LdrpSchedulerSharedDataListHead]` (첫 노드).
- 조건 ①: `+0x2d CMP RBX,RAX` (`RAX` = `&머리`) / `+0x30 JZ +0xa5` (**목록이 빔**) · `+0x32 TEST RBX,RBX` / `+0x35 JZ +0xa5` (머리가 0 = 한 번도 안 만듦).
- 조건 ②: `+0x37 MOV RAX,GS:[0x60]` (PEB) / `+0x40 TEST byte [RAX+0x7c4],0x10` / `+0x47 JNZ +0xa5` (**`NtGlobalFlag2` bit 0x10 켜짐**).
- 둘 다 통과하면: `+0x50 RtlReleaseSRWLockShared` → 버퍼 만들기 (`+0x5a ~ +0x7f`; 첫 8바이트 0, **`+0x77 MOV RAX,[RBX+0x10]` → `[RSP+0x28]` = 섹션 핸들**, `[RSP+0x30]` = 0 출력 칸) → **`+0x84 NtSetInformationThread(-2, 0x39, 버퍼, 0x18)`** (`+0x80 LEA EDX,[R9+0x21]` = 0x39) → `+0x8b JS +0x99` (실패면 TEB 안 씀) → `+0x92 MOV [RDI+0x1850],RAX` (돌려받은 표 주소).
- 하나라도 안 맞으면 `+0xa5 ~ +0xb1` 락만 풀고 나온다 → **TEB `+0x1850` 은 0 으로 남는다** (run41 그대로).
- 참고: `RtlAcquireSRWLockShared` 자체가 TEB `+0x1850` 을 읽는데 (`+0xf`), 이 시점엔 아직 0 이라 칸을 안 쓴다 (자기 자신 락에 칸을 적는 되풀이 없음).

**조건 ① "목록이 안 비었다" = `LdrpAllocateSchedulerSharedData` 가 성공했다.**
- 목록 머리 `LdrpSchedulerSharedDataListHead` (rva `0x1d2930`) 는 `.data` 의 **파일에 없는 0 구간** (섹션 raw 크기 `0x2000` < 오프셋 `0x6930`) 이라 처음엔 0. 그래서 부르기 직전에 빈 목록으로 만든다: `LdrpInitializeProcess +0x180b` (락 = 0) · `+0x1812 ~ +0x1820` (`Flink = Blink = &머리`). 바로 뒤 `+0x1827 CALL LdrpAllocateSchedulerSharedData` (ret `+0x182c`) · `+0x1837 CALL LdrpAcquireSchedulerSharedDataSlot` (ret `+0x183c`).
  - **부르는 쪽은 반환값을 안 본다** (`+0x182c MOV RBX,[RSP+0x158]` 로 곧장 넘어감). Allocate 가 실패하면 목록이 빈 채로 → ① 불통과 → TEB 칸 0.
- 같은 모양이 프로세스 복제 길에도 있다: `RtlCompleteProcessCloning +0xec ~ +0x102` (목록 비우기) → `+0x109 CALL Allocate` → `+0x117 CALL AcquireSlot` (`.asm`). `RtlCloneUserProcess +0x2ab ~ +0x2c0` → `+0x2c7` → `+0x2d5` (`.asm` 없음, 스크래치 objdump 와 `_calls.tsv`). 새 스레드는 `LdrpInitializeThread +0x50 CALL AcquireSlot` (ret `+0x55`) 만 (Allocate 안 부름).
- 목록에 노드를 끼우는 줄은 ntdll 전체에 **`LdrpAllocateSchedulerSharedData +0xa6` 하나**뿐이고, 빼는 줄은 없다 (이 목록 주소를 쓰는 줄 전수: `LdrpInitializeProcess`·`RtlCloneUserProcess`·`RtlCompleteProcessCloning` 의 비우기, Allocate 의 끼우기, AcquireSlot 의 읽기).

**`LdrpAllocateSchedulerSharedData` 본문** (`.asm`, 이름과 syscall):
1. `+0xc CMP byte [LdrpIsSecureProcess],0` / `+0x13 JNZ +0xbd` → `0xC00000BB` (`STATUS_NOT_SUPPORTED`) 로 끝, syscall 없음. (test.exe 는 0, P1a 사진 `0x7ffafb8328e0` 첫 8바이트 0.)
2. **`+0x2c CALL NtSetInformationProcess(-1, 0x70, &출력, 8)`** (`+0x19 MOV R9D,8` · `+0x24 OR RCX,-1` · `+0x28 LEA EDX,[R9+0x68]` = 0x70; ntdll 스텁 syscall 번호 `0x1c`). 출력 8바이트 = **섹션 핸들**. 실패하면 상태를 들고 `+0xc9` 로 가서 (핸들이 있으면) 정리.
   - 커널 `NtSetInformationProcess.asm`: 클래스 `0x70` 갈래 `+0xec4 CMP R13D,8` (길이 정확히 8) · `+0xeda MOV EDX,0x200` (프로세스 접근 권한) · `+0xf17 CALL PspSchedulerSharedDataRegionCreate` · `+0x13ea ~ +0x13ef` 핸들을 사용자 버퍼에 (run65 와 같음).
3. `+0x3b ~ +0x57 RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 0x40000 | 8, 0x18)` — `0x18` 바이트, 0 채움. 실패면 `+0xc4 MOV EBX,0xc000009a` → `+0xd3 CALL NtClose(핸들)` (syscall 번호 `0x0f`).
4. `+0x70 MOV [노드+0x10],핸들` → `+0x74 RtlAcquireSRWLockExclusive(&목록락)` → 머리 앞에 끼움 (`+0x87 CMP [첫노드+8],&머리` 어긋나면 `+0x92 INT 0x29` 코드 3 · `+0x94 ~ +0xa6` 링크 4개 적기) → `+0xad RtlReleaseSRWLockExclusive`.
- 한 줄: **목록에 `0x18`B 노드 (Flink·Blink·`+0x10` 섹션 핸들) 를 넣는다.** syscall 은 `NtSetInformationProcess` (클래스 `0x70`) 하나, 실패 정리에 `NtClose`. 핸들은 AcquireSlot 이 클래스 `0x39` 에서 "어느 섹션의 칸을 달라" 고 가리키는 데 쓴다 (`LdrpAcquireSchedulerSharedDataSlot +0x77` → 커널 `NtSetInformationThread +0xbd5 PspSchedulerSharedDataType` 로 객체 찾기).

**조건 ② `PEB.NtGlobalFlag2 & 0x10` 의 뜻**
- `PEB +0x7c4 NtGlobalFlag2` (PDB `_PEB`) 는 `NtGlobalFlag` (`+0xbc`) 의 짝인 둘째 전역 플래그. 값이 정해지는 길:
  1. 커널 `MmCreatePeb +0x4ac MOV EAX,[NtGlobalFlag2]` → `+0x4b2 MOV [RBX+0x7c4],EAX` (커널 전역을 PEB 에 복사).
  2. 그 커널 전역은 부팅 때 `InitBootProcessor +0x3e2 AND [CmNtGlobalFlag2],0x201f` → `+0x494 MOV EAX,[CmNtGlobalFlag2]` → `+0x49a OR [NtGlobalFlag2],EAX` (레지스트리에서 읽은 값 `CmNtGlobalFlag2` 의 허용 비트 `0x201f` 안만 받음; 0x10 은 허용 비트). `NtSetSystemInformation +0x1a28` 도 이 전역에 쓴다 (실행 중, 클래스는 안 봄).
  3. 그 뒤 ntdll `LdrpInitializeExecutionOptions +0x587 LEA R8,[L"GlobalFlag2"]` → `+0x59c CALL RtlQueryApplicationKeyOption` → 성공하면 `+0x5a9 MOV [RBX+0x7c4],EAX` — **IFEO (이미지 파일 실행 옵션) 의 `GlobalFlag2` 값이 있으면 덮어쓴다.**
- 비트 0x10 의 **이름은 PDB 에 없다** (`FLG2_*` 류 상수·enum 없음). 코드로 아는 건 **효과뿐**: 켜지면 `LdrpAcquireSchedulerSharedDataSlot` 이 칸 표를 안 받는다 (`+0x47 JNZ`). → "스케줄러 공유 데이터 (락 소유 칸 표) 기능 **끄는 스위치**" 로 읽힌다 `[추측: 이름 풀이]`. 같은 PEB 칸의 다른 비트 읽는 곳: bit 0x1 `LdrpInitializeExecutionOptions +0x71c`, bit 0x2 `LdrpIncludeAlternateForwarders +0x16`.

**test.exe 는 어느 쪽인가 — 둘 다 통과 (표를 받음).** `[관찰]` run44 P1a (`LdrpInitializeProcess +0x199d`, 곧 `+0x183c` 뒤) 사진, `ms/logs/04_baggage-read_P1a.txt`:
- 5133줄: 목록 머리 `0x7ffafb832930` = `0x25996f33940` / `0x25996f33940` (Flink = Blink, **노드 1개**; `LdrpHeap` = 5120줄 `0x25996f30000` 안). → ① 통과.
- 1842줄: `PEB +0x7c4 NtGlobalFlag2 : 0` → ② 통과 (`& 0x10` 꺼짐).
- 2108줄: `TEB +0x1850 SchedulerSharedDataSlot : 0x259`96cb0000` ≠ 0. 3441 ~ 3445줄: 그 표 8칸 전부 0 (TLS 락 적기 전).
- 그러므로 test.exe 에서 `LdrpCallTlsInitializers +0x36 TEST R8,R8` / `+0x39 JZ` 는 **안 건너뛰고** 칸 적기 (`+0x63`) 를 한다 (run41 "표가 0 이면 건너뜀" 쪽 길이 아님).

### 4 (d) `THREADINFOCLASS` 0x38·0x39 이름

근거: `llvm-pdbutil-14 dump -types/-publics` 와 문자열 검색 (스크래치 `…/scratchpad/types_ntdll.dll.pdb.txt`·`types_ntoskrnl.exe.pdb.txt`·`pubs_*.txt`), 커널 `ms/disassemble/ntoskrnl/NtSetInformationThread.asm`, `KeAbUserModeEntryFree.asm`.

**PDB 에 없다.**
- `ntdll.dll.pdb` enum 138개·`ntoskrnl.exe.pdb` enum 417개 (`LF_ENUM` 레코드) 이름 중 `THREADINFOCLASS`·`_THREAD_INFORMATION_CLASS`·`_PROCESSINFOCLASS` 없음. 열거자 중 `Thread…` 로 시작하는 것은 `ThreadPpm*`·`ThreadWorkloadClass*`·`ThreadObject`·`ThreadedDpcObject` 뿐 (다른 enum). 값 56·57 열거자는 모두 다른 enum (`FileRenameInformationBypassAccessCheck=56` 등).
- 공개 심볼 (`S_PUB32`) 에서 `InformationThread`·`LockOwnership`·`SchedulerShared` 를 찾으면 **함수·변수 이름만**: `PspSchedulerSharedDataRegionCreate`·`…Delete`·`…SlotAllocate`·`…SlotFree`·`…SlotRetrieve`, `PspSchedulerSharedDataType`·`…Mapping`, ntdll `LdrpAcquireSchedulerSharedDataSlot`·`LdrpAllocateSchedulerSharedData`·`LdrpSchedulerSharedDataListHead(Lock)`. 클래스 이름은 없다.
- `ms/sym/*.pdb` **29개 전부**에서 `THREADINFOCLASS|ThreadSchedulerSharedData|ThreadUpdateLockOwnership|ThreadInformationClass` 문자열 검색 = 0. `ThreadBasicInformation`·`ThreadHideFromDebugger` 같은 열거자 문자열도 6개 PDB (ntdll·ntoskrnl·KernelBase·kernel32·win32kbase·rpcrt4) 에서 0 (enum 이 있으면 있어야 할 이름).
- → **공식 이름은 이 PDB 들로는 못 얻는다.** MS 공개 헤더 (`winternl.h`·`wdm.h`) 에도 이 번호는 없다고 기억한다 `[추측: 기억]`. 외부 비공식 헤더 (phnt) 기억은 `0x38 = ThreadUpdateLockOwnership`, `0x39 = ThreadSchedulerSharedDataSlot` `[추측]` — 아래 커널 동작과 뜻이 맞는다 (run41 과 같음).

**커널이 하는 일** (`NtSetInformationThread.asm`, 점프표로 확인):
- 갈림길: `+0x1ee ADD EBX,-0x3` · `+0x1f1 CMP EBX,0x36` · `+0x1f4 JA +0x1617` (`MOV EAX,0xc0000003` = `STATUS_INVALID_INFO_CLASS`) · `+0x1fd MOVZX EAX,byte [RSI+RAX+0x937b8c]` · `+0x205 MOV ECX,[RSI+RAX*4+0x937b10]` · `+0x20f JMP RCX`. 표 칸을 PE 에서 직접 읽어 풀면 **클래스 `0x38` → `+0x5a2`**, **`0x39` → `+0x58e`**, 클래스 `0x3a` 부터는 `JA` 로 `0xC0000003` (이 표의 마지막 클래스 = `0x39`).
- **0x38** (`+0x5a2`): `+0x5a2 CMP R15D,0x10` (길이 정확히 16, 아니면 `0xC0000004`) · `+0x5a8 MOVUPS XMM0,[R12]` (16바이트 복사) · `+0x5c0 CMP dword [RSP+0x160],0` (둘째 dword 가 0 이어야) · `+0x5fb` 스레드 핸들 (`PsThreadType`, 권한 `0x20`) · `+0x61c CMP RBX,RAX` (RAX = `GS:[0x188]` **현재 스레드** 여야, 아니면 `+0x1554` `0xC00000BB`) · `+0x625 MOV RDX,[RSP+0x158]` (첫 8바이트 = **칸 번호**) · `+0x630 CALL KeAbUserModeEntryFree`. 그 안: `+0x14 MOV RDI,[RCX+0x478]` (`UserAbEntries`) · `+0x2e CMP EBP,EAX` (칸 번호 범위) · `+0x78 CALL KiAbEntryFreeAndEnableInterrupts` (커널 항목 반납) · `+0x92 SHR RAX,0x38` / `+0x96 AND AL,0x7f` / `+0xa1 MOV [RCX+RBP*8],RAX` (**칸 bit 63 끄기**). 한 줄: **"내 AutoBoost 칸 N 의 커널 항목을 돌려주고 칸의 bit 63 을 끈다."**
- **0x39** (`+0x58e`): `+0x58e CMP R15D,0x18` (길이 정확히 24) / `+0x592 JZ +0xb5c` · 입력 `+0x199 CMP EBX,0x39` → `+0x1a9 ProbeForWrite` (**출력도 있는 클래스**) · `+0xb5c ~ +0xb71` 24바이트 복사 · 스레드 핸들 (`+0xbba`) · `+0xbc9 MOVD ECX,XMM6` / `+0xbcf JNZ` (첫 dword 0) · `+0xbd5 MOV R8,[PspSchedulerSharedDataType]` + `+0xc0c` 버퍼 둘째 8바이트 (섹션 핸들) 로 **스케줄러 공유 데이터 객체** 찾기 · `+0xc2c CALL PspSchedulerSharedDataRegionSlotAllocate(스레드, 객체, &출력)` · `+0xc44 ~ +0xc49` 버퍼 되쓰기 (셋째 8바이트 = 표의 유저 주소). 한 줄: **"이 스레드에 스케줄러 공유 데이터 칸 (8칸 표) 을 배정하고 그 유저 주소를 돌려준다."** 입력 버퍼 모양 `{ 0 (8B), 섹션 핸들 (8B), 출력 표 주소 (8B) }` 는 `LdrpAcquireSchedulerSharedDataSlot +0x5a ~ +0x7f` 와 맞는다.
- 문서에는 계속 "클래스 0x38·0x39 (`[추측]` 외부 이름 …)" 으로만 쓴다.

## 정정·영향 (오케스트레이터 → flow-writer)

- **run41 "bit 0 = `[공백]`, 켜는 ntdll 줄 못 찾음"** → 켜는 곳은 **커널 `KeWaitForAlertByThreadId +0x2db`** (대기 직전, 유저 모드 · 주소 일치). run41 이 `KiAb*`·`KeAb*` 만 봐서 놓친 것이고, ntdll 에 없다는 결론은 그대로다. flow.md 2.7 "TLS 락 소유 칸 표" 의 칸 모양 표 **bit 0 줄**과 "정적으로 남은 것 (run41) Q74 (a)" 를 고친다.
- run41 "(b) 효과 없는 호출" → 이유를 둘로 좁힘: 전역 칸 없음, 인자 `≠ 0` 이면 그대로. test.exe 는 TEB 칸 `≠ 0` 이라 호출이 생길 수는 있지만 효과는 같다 (없음). "왜 짰나" 는 여전히 `[추측]`.
- run41 "(c) 조건 둘이 언제 차나" → 채움: 목록 = `LdrpInitializeProcess +0x180b ~ +0x1820` 가 빈 목록으로 만들고 `LdrpAllocateSchedulerSharedData` 가 끼움 (반환값은 아무도 안 봄). `NtGlobalFlag2` = 커널 `MmCreatePeb` 가 복사, IFEO 가 덮어쓸 수 있음.
- run41 "(d) THREADINFOCLASS" → "PDB 에 없음" 확정 (다른 27개 PDB 포함). 클래스 한 줄 동작 위에.
- `ms/decomp`·`ms/disassemble` 새 파일 4개 (위). baggage.md 쪽 고칠 곳은 없다 (이번 답이 바꾸는 짐 없음; 2512줄의 "빈 칸에 큐 락 주소 → 풀 때 지움" 도 그대로).

## 동적으로 확인할 것 (dast 요청 필요: 작음, 새 번호 없이 Q25 쪽에 붙이면 됨)

1. test.exe 시동 중 `ntdll!RtlpAcquireSRWLockSharedContended` (rva `0x15f0c`) 입구에 **서는지**. 안 서면 (b) 호출은 시동 길에서 아예 없는 것.
2. `ntdll!LdrpCallTlsInitializers+0x6d` 직후 `dq @$teb+0x1850 L8` — 칸 0 에 `LdrpTlsLock` 주소 (rva `0x1d2460`) 가 적히고 bit 0·1·63 이 다 0 인지 (0x1ab 에서 `dq @rsi L1`; run41 의 Q25 확인과 같은 자리).
3. (선택) `nt!KeWaitForAlertByThreadId+0x2db` 에서 bit 0 이 켜지는 순간 — **모든 대기에 걸려서 비추천.** 꼭 보려면 `+0x2db` 에 조건 `@r13b == 1 && @r15 != 0` + 프로세스 조건.
- 이미 사진으로 확인된 것이라 새로 안 볼 것: 목록 비지 않음·`NtGlobalFlag2 = 0`·TEB `+0x1850 ≠ 0` (run44 P1a).

## 다음 요청 후보

1. (정적, 중간) 커널 `PspSchedulerSharedDataRegionSlotAllocate`·`PspSchedulerSharedDataRegionSlotFree` 본문 — 표 한 칸의 크기와 스레드마다 다른 표를 어떻게 나누는지, `_KTHREAD +0x320`·`+0x464 SchedulerSharedOffset` 채우기. PE_emulator 가 클래스 `0x39` 를 기본 대답에서 진짜 값으로 바꿀 때 필요. (ntoskrnl Ghidra 가 풀린 뒤.)
2. (정적, 작음, 급하지 않음) 커널 `CmNtGlobalFlag2` 가 읽는 레지스트리 값 이름과 `NtGlobalFlag2` bit `0x1`·`0x2`·`0x10` 이름 후보.
3. (정적, 작음) `NtSetSystemInformation +0x1a17 ~ +0x1a28` 이 어떤 클래스로 `NtGlobalFlag2` 를 바꾸는지 (이번엔 클래스 안 봄).

## 방법·도구 메모

- 칸 쓰기 전수: 스크래치 `…/scratchpad/ntdll.dis` (objdump 풀이) 와 파이썬 `…/scratchpad/slotscan2.py`·`bit0scan.py`·`bit0scan2.py`·`fnmap.py`·`grepdis.py` (결과서에 임시 스크립트를 따로 짠 것 — 레포 밖, 커밋 안 함).
- System32 바이트 검색: 같은 스크래치에서 한 번 돌린 파이썬 (4119 파일, 약 30초). 정규식 `[REX.W] 8B/2B [mod=10] 50 18 00 00` + 앞 80바이트 안 `GS:` 읽기.
- 커널 함수 위치 변환: `ms/decomp/ntoskrnl/_index.tsv` (두 번째 열이 절대 주소, 네 번째가 크기).
- 새 `.asm` 4개 (`RtlTryAcquireSRWLockShared`, `RtlAbPostRelease`, `RtlpAbFreeKernelEntry`, `RtlCompleteProcessCloning`) 는 `RtlAbPreAcquire` 의 다른 부르는 쪽과 목록 재설정 줄을 확인하는 데만 썼다. 이 함수들의 본문 분석은 안 했다.
- 근거 `+0x` 는 위에 쓴 `.asm` 줄에서 다시 찾아 대조했다 (call 은 ret 오프셋·call 오프셋 둘 다 `_calls.tsv` 와, 점프 목적지는 "목적지 − 함수 시작" 으로).
- `PE_emulator` 쪽 "우리" 판단은 `_workspace/orchestrator/74_scheduler-slot.md`·`98_scheduler-alloc.md` 와 `Src/ntdll/LdrInit.cpp` 주석 (4437·4449 ~ 4451줄) 을 읽은 것 (코드를 돌리진 않음).

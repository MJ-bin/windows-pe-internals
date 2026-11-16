# 2026-11-16 (작성일) run110 — TLS 락 소유 칸 표 남은 넷 (Q74, 정적만): 칸 **bit 0 은 ntdll 이 아니라 커널 `KeWaitForAlertByThreadId +0x2db` 가 켠다** (유저 모드로 그 락을 기다리러 들어갈 때), 끄는 곳 `KiAbUmPostWait +0x88` `[정적]`. `RtlAbPreAcquire` 는 **둘째 인자 (칸) 하나만 보고**, 칸이 주어지면 그대로 돌려준다 — `RtlpAcquireSRWLockSharedContended +0x126` 호출은 반환값도 안 써서 늘 효과 없음 `[정적]`. 표를 받는 조건 둘 = "`LdrpAllocateSchedulerSharedData` 가 노드를 끼움" · "`PEB.NtGlobalFlag2 & 0x10` (끄는 스위치 `[추측: 이름]`) 꺼짐" — **test.exe 는 둘 다 통과** `[관찰]` run44 P1a. `THREADINFOCLASS` `0x38` · `0x39` 이름은 **PDB 29개 어디에도 없다** (확정).

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표와 호출 관계 트리.)

> **지도 범위**: 그림은 안 고쳤다. 스케줄러 공유 데이터 두 호출 (`LdrpInitializeProcess +0x182c` · `+0x183c`) 은 run65 가 "상자로 안 넣음" 으로 정한 자리이고, 이번에 나온 커널 길 (`NtWaitForAlertByThreadId` → `KeWaitForAlertByThreadId`) 은 **락을 기다릴 때만** 타는 길이라 test.exe 시동 길에 없을 것 `[추측]` 이다. 글은 flow.md 2.1 · 2.4 · 2.7 · 4장에만.

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 P1a 사진 덤프 읽기 로그를 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`**, **ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1`** (둘 다 기준과 같음 — `ms/sym/ntoskrnl.exe.pdb.info.txt`, 결과서).
  - ntdll: 다시 analyze 안 함. 새로 뽑은 것 4개 (아래 "방법").
  - ntoskrnl: **Ghidra 프로젝트를 안 열었다** (그때 다른 decompiler 가 쓰고 있어서 — 요청서). 이미 있던 `.asm` 과 PDB 파일 읽기 (`llvm-pdbutil-14`), 스크래치 `objdump` 풀이만.
- 버전 대조 (결과서): `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x16b → LdrpCallTlsInitializers` · `+0x19c → LdrpCallInitRoutine`, 커널 `NtCreateUserProcess +0xc36 → PspAllocateProcess` 가 `_calls.tsv` 와 같다.
- "ntdll 전체에서 그 줄 찾기" 는 앞 바퀴들이 만든 스크래치 `objdump -d` 풀이 (`ntdll.dis`, 커밋 안 함) 를 다시 썼다. 숫자 "N곳" 은 **검색한 꼴 기준**이다 (꼴 밖은 못 셈 — 결과서).
- **sast/106 · 108 ~ 120 결과는 이 문서에 안 넣었다** (다음 바퀴들 몫).

## 목적

- **Q74** (run41 이 남긴 TLS 락 소유 칸 표 작은 정적 넷):
  - (a) 칸 **bit 0** 을 켜는 곳 (run41 은 ntdll 에서 못 찾음 `[공백]`).
  - (b) `RtlpAcquireSRWLockSharedContended +0x126` 의 `RtlAbPreAcquire(락, 칸)` 이 왜 효과 없는 호출인가.
  - (c) `LdrpAcquireSchedulerSharedDataSlot` 의 진입 조건 둘 (`LdrpSchedulerSharedDataListHead` 가 비지 않음 · `PEB.NtGlobalFlag2 & 0x10` 꺼짐) 의 뜻, `LdrpAllocateSchedulerSharedData` 본문, test.exe 는 어느 쪽인가.
  - (d) `THREADINFOCLASS` `0x38` · `0x39` 의 공식 이름.
- 앞 바퀴 짝: run41 (`runs/02_sprint/run41-tls-lock-detail.md`, sast/36) — 표 이름 · bit 63 · bit 1 · 클래스 0x38 을 닫고 위 넷을 남김. run65 (sast/59) — `LdrpAllocateSchedulerSharedData` 의 syscall 과 노드를 처음 적음.

## 방법

- 정적: `_workspace/sast/107_tls-lock-small.md` — ntdll (질문 1 ~ 3) · ntoskrnl `.asm` 읽기 (질문 1 · 3 · 4) · PDB 파일 (질문 4)
  - 이미 있던 `.asm` · `.c` 그대로 (다시 안 뽑음): ntdll `RtlAbPreAcquire`, `RtlpAcquireSRWLockSharedContended`, `LdrpAcquireSchedulerSharedDataSlot`, `LdrpAllocateSchedulerSharedData`, `RtlAcquireSRWLockShared`, `LdrpCallTlsInitializers`, `LdrpInitializeProcess`, `LdrpInitializeExecutionOptions`. ntoskrnl `KeWaitForAlertByThreadId`, `KiAbUmPostWait`, `KiAbProcessPreContextSwitch`, `NtSetInformationThread`, `KeAbUserModeEntryFree`, `NtSetInformationProcess`, `MmCreatePeb`, `InitBootProcessor`.
  - 새로 뽑음 (ntdll, depth 0): `^(RtlTryAcquireSRWLockShared|RtlAbPostRelease|RtlpAbFreeKernelEntry|RtlCompleteProcessCloning)$` (4개). `RtlAbPreAcquire` 의 다른 부르는 쪽과 "목록을 다시 비우는 곳" 줄을 맞춰 보려고만 썼다 (본문 분석은 안 함).
  - 커널 `NtWaitForAlertByThreadId` (rva `0x9fc370`) 는 `.asm` 이 없어 스크래치 `objdump` 로만 봤다 (결과서).
  - 전수 검색: ntdll `objdump` 풀이에서 `0x1850` 이 든 줄 92 (읽기 91 · 쓰기 1) → 그 표를 읽는 함수 56개의 칸 쓰기 줄 전부. System32 PE 4119개에서 "`GS:` 로 TEB 를 읽고 `[reg+0x1850]` 을 읽는" 바이트 꼴. 커널 `objdump` 에서 마스크 상수 `0x7ffffffffffffffc` 를 쓰는 함수와 `[스레드+0x320]` 을 읽는 함수의 교집합.
  - PDB: `ntdll.dll.pdb` · `ntoskrnl.exe.pdb` 형 레코드 · 공개 이름, `ms/sym/*.pdb` 29개 문자열 검색.
- 결과서: `_workspace/sast/107_tls-lock-small_result.md` (**분석성공** — 스크립트 문제 없음).
- flow-writer 가 다시 본 것:
  - `.asm` 줄: `KeWaitForAlertByThreadId +0x2db` · `+0x2df` · `+0x2e3`, `KiAbUmPostWait +0x88`, `RtlAbPreAcquire +0x3` · `+0x6` · `+0x43`, `LdrpAcquireSchedulerSharedDataSlot +0x40` · `+0x47` · `+0x84` · `+0x92`, `LdrpAllocateSchedulerSharedData +0xc`, `MmCreatePeb +0x4ac` · `+0x4b2`, `LdrpInitializeExecutionOptions +0x587` · `+0x5a9`.
  - `_calls.tsv` (ntdll · ntoskrnl): 아래 표의 call · ret 줄 전부. `alias:` 줄은 이번 문서에 없다.
  - 사진 로그: `ms/logs/04_baggage-read_P1a.txt` 1842 · 2108 · 3441 ~ 3445 · 5120 · 5128 · 5133줄 (아래 "사진 로그 대조").

## 결과

비유 (도서관 대출 카드 — run41 과 같은 비유):

- 스레드마다 **대출 카드 한 장 (8칸)**. 책 (락) 을 빌리면 빈 칸에 책 이름 (락 주소) 을 적고, 돌려주면 지운다.
- run41 까지 본 도장: **bit 63** = 사서 (커널) 가 "이 사람 책 들고 자리 비웠음" 이라고 찍는 도장, **bit 1** = 내가 "지금 돌려주는 중" 이라고 찍는 표시.
- 이번에 찾은 도장: **bit 0** = 사서가 "이 사람이 **이 책을 기다리며 대기실에 들어감**" 이라고 찍는 도장. 대기실에 들어갈 때 찍고, 나올 때 지운다. 그래서 ntdll 코드에는 안 보였다.
- 카드를 받으려면 두 가지가 맞아야 한다: 도서관에 **카드 발급대 (스케줄러 공유 데이터 섹션)** 가 열려 있고, 프로세스 규칙판 (`NtGlobalFlag2`) 에 "카드 안 씀" 표시가 없어야 한다. test.exe 는 둘 다 맞아서 카드를 받았다 (사진).

### 정적 — 호출 지점 (`ms/disassemble/<모듈>/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x1827` | `+0x182c` | `LdrpAllocateSchedulerSharedData` | 앞 `+0x180b ~ +0x1820` 에서 목록 락 0 · 목록 비움. **반환값 안 봄** (`+0x182c MOV RBX,[RSP+0x158]` 로 곧장) | [정적] (run65 + 이번) |
| `ntdll!LdrpAllocateSchedulerSharedData` | `+0x2c` | `+0x31` | `NtSetInformationProcess(-1, 0x70, &출력, 8)` | `LdrpIsSecureProcess == 0` 일 때만 (`+0xc` · `+0x13 JNZ +0xbd` → `0xC00000BB`). syscall `0x1c` | [정적] (run65 그대로) |
| `ntdll!LdrpAllocateSchedulerSharedData` | `+0x57` | `+0x5c` | `RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 0x40000 \| 8, 0x18)` | syscall 성공 뒤. 실패면 `0xc000009a` | [정적] (run65 그대로) |
| `ntdll!LdrpAllocateSchedulerSharedData` | `+0x74` · `+0xad` | `+0x79` · `+0xb2` | `RtlAcquireSRWLockExclusive` · `RtlReleaseSRWLockExclusive` (`&LdrpSchedulerSharedDataListHeadLock`) | 그 사이 `+0x94 ~ +0xa6` 링크 4개 (끼우기). `+0x87` 검사 틀리면 `+0x92 INT 0x29` (코드 3) | [정적] |
| `ntdll!LdrpAllocateSchedulerSharedData` | `+0xd3` | `+0xd8` | `NtClose(핸들)` | 힙 할당 실패 정리 (핸들이 있으면). syscall `0x0f` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1837` | `+0x183c` | `LdrpAcquireSchedulerSharedDataSlot(TEB)` | 첫 스레드 | [정적] (run41 그대로) |
| `ntdll!LdrpAcquireSchedulerSharedDataSlot` | `+0x1a` | `+0x1f` | `RtlAcquireSRWLockShared(&LdrpSchedulerSharedDataListHeadLock)` | 이때 TEB `+0x1850` 은 아직 0 → 이 락에는 칸을 안 적음 | [정적] |
| `ntdll!LdrpAcquireSchedulerSharedDataSlot` | `+0x84` | `+0x89` | `NtSetInformationThread(-2, 0x39, 버퍼 {0, 섹션 핸들, 0}, 0x18)` | 조건 ① `+0x2d ~ +0x35` 목록 비지 않음 · ② `+0x40 TEST byte [PEB+0x7c4],0x10` / `+0x47 JNZ` 꺼짐. 성공이면 `+0x92` TEB `+0x1850` | [정적] |
| `ntdll!LdrpAcquireSchedulerSharedDataSlot` | `+0x50` / `+0xac` | `+0x55` / `+0xb1` | `RtlReleaseSRWLockShared` | 조건 통과 길 / 안 맞은 길 | [정적] |
| `ntdll!LdrpInitializeThread` | `+0x50` | `+0x55` | `LdrpAcquireSchedulerSharedDataSlot` | 새 스레드 (Allocate 는 안 부름) | [정적] (run41 그대로) |
| `ntdll!RtlCompleteProcessCloning` | `+0x109` / `+0x117` | `+0x10e` / `+0x11c` | `LdrpAllocateSchedulerSharedData` / `LdrpAcquireSchedulerSharedDataSlot` | 프로세스 복제 길 — 앞 `+0xec ~ +0x102` 목록 비우기. test.exe 길 아님 | [정적] |
| `ntdll!RtlCloneUserProcess` | `+0x2c7` / `+0x2d5` | — | 같은 둘 | 복제 길 — 앞 `+0x2ab ~ +0x2c0` 목록 비우기 (`.asm` 없음, 결과서가 스크래치 `objdump` 로) | [정적] |
| `ntdll!LdrpCallTlsInitializers` | `+0x113` | `+0x118` | `RtlpAcquireSRWLockSharedContended(&LdrpTlsLock, 칸, 값)` | 빠른 잡기 `+0x6d CMPXCHG` 실패 때만. 칸 = `+0x26 ~ +0x63` 에서 찾은 빈 칸 (표 없으면 0) | [정적] (run28 그대로) |
| `ntdll!RtlAcquireSRWLockShared` | `+0x59` | `+0x5e` | `RtlpAcquireSRWLockSharedContended` | 같은 꼴 — `+0xf ~ +0x39` 빈 칸에 락 주소, `RDX` = 그 칸 (없으면 0) | [정적] |
| `ntdll!RtlpAcquireSRWLockSharedContended` | `+0x116` | `+0x11b` | `NtWaitForAlertByThreadId(락)` | `+0x113 MOV RCX,RSI` (락) — 여기서 커널 대기로 | [정적] |
| `ntdll!RtlpAcquireSRWLockSharedContended` | `+0x126` | `+0x12b` | `RtlAbPreAcquire(락, 칸)` | `+0x11b TEST R14,R14` / `+0x11e JZ` — 칸 ≠ 0 일 때만. 반환값은 `+0x12b` 가 `EAX` 를 덮어 안 씀 | [정적] (run41 + 이번) |
| `ntdll!RtlTryAcquireSRWLockShared` | `+0x11` | `+0x16` | `RtlAbPreAcquire(락, 0)` | `+0x8 XOR EDX,EDX` — 칸 0 이라 **진짜로 빈 칸을 잡는** 쪽 | [정적] |
| `ntdll!RtlTryAcquireSRWLockShared` | `+0x4e` | `+0x53` | `RtlAbPostRelease(락, 칸)` | 잡기 실패면 칸 되돌림 | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x59c` | `+0x5a1` | `RtlQueryApplicationKeyOption(…, L"GlobalFlag2", …)` | 성공이면 `+0x5a9` PEB `+0x7c4` 덮어씀 (IFEO) | [정적] |
| `nt!NtWaitForAlertByThreadId` | `+0x2f` | — | `KeWaitForAlertByThreadId(이전 모드, 시간, 주소)` | `+0x9` 주소 · `+0x1e` 이전 모드 `[스레드+0x232]` (`.asm` 없음 — 결과서 스크래치 `objdump`) | [정적] |
| `nt!KeWaitForAlertByThreadId` | `+0x1de` | `+0x1e3` | `KiAbUmPostWait(스레드, 주소)` | 깨어난 뒤 (`+0x1d8 MOV RDX,R15`) | [정적] (run41 자리 + 이번 본문) |
| `nt!NtSetInformationThread` | `+0x630` | `+0x635` | `KeAbUserModeEntryFree(스레드, 칸 번호)` | 클래스 `0x38`, 길이 16, 둘째 dword 0, 현재 스레드 | [정적] (run41 그대로) |
| `nt!KeAbUserModeEntryFree` | `+0x78` | `+0x7d` | `KiAbEntryFreeAndEnableInterrupts` | 커널 항목 반납 | [정적] |
| `nt!NtSetInformationThread` | `+0xc2c` | `+0xc31` | `PspSchedulerSharedDataRegionSlotAllocate(스레드, 객체, &출력)` | 클래스 `0x39`, 길이 24, 첫 dword 0, 둘째 8바이트 = 섹션 핸들 → `+0xbd5` `PspSchedulerSharedDataType` 로 객체 찾기 | [정적] (run41 자리 + 이번 갈래) |
| `nt!NtSetInformationProcess` | `+0xf17` | `+0xf1c` | `PspSchedulerSharedDataRegionCreate` | 클래스 `0x70`, 길이 정확히 8 (`+0xec4`), 프로세스 접근 `0x200` (`+0xeda`) | [정적] (run65 그대로) |

### 호출 관계

```
첫 스레드 (test.exe 메인)
LdrpInitializeProcess
 │  +0x180b ~ +0x1820  목록 락 = 0, LdrpSchedulerSharedDataListHead = 빈 목록 (파일엔 0 구간)
 ├─ +0x182c  LdrpAllocateSchedulerSharedData                     반환값 안 봄
 │            ├─ +0xc   LdrpIsSecureProcess ≠ 0 이면 0xC00000BB 로 끝 (syscall 없음)
 │            ├─ +0x31  NtSetInformationProcess(-1, 0x70, &핸들, 8)   ── 커널 +0xf1c PspSchedulerSharedDataRegionCreate
 │            ├─ +0x5c  RtlAllocateHeap(LdrpHeap, …, 0x18)            노드 +0x10 = 핸들
 │            ├─ +0x79 … +0x94 ~ +0xa6 끼우기 … +0xb2  (목록 락 독점)   ← ntdll 에서 목록에 끼우는 유일한 줄
 │            └─ +0xd8  NtClose(핸들)                                  힙 실패 정리
 └─ +0x183c  LdrpAcquireSchedulerSharedDataSlot(TEB)
              ├─ +0x1f  RtlAcquireSRWLockShared(목록 락)
              │   ① 목록 빔? (+0x2d/+0x30, +0x32/+0x35)  ② PEB+0x7c4 & 0x10? (+0x40/+0x47)  → 하나라도면 +0xb1 풀고 끝 (TEB 칸 0)
              ├─ +0x55  RtlReleaseSRWLockShared
              ├─ +0x89  NtSetInformationThread(-2, 0x39, {0, 핸들, 0}, 0x18)  ── 커널 +0xc31 PspSchedulerSharedDataRegionSlotAllocate
              └─ +0x92  TEB+0x1850 = 돌려받은 표 주소                       test.exe: 0x25996cb0000 [관찰] P1a

락을 기다릴 때 (시동 길엔 없을 것 [추측])
LdrpCallTlsInitializers (또는 RtlAcquireSRWLockShared +0x5e)
 └─ +0x118  RtlpAcquireSRWLockSharedContended(락, 칸, 값)
             ├─ +0x11b  NtWaitForAlertByThreadId(락)  ==> 커널
             │            nt!NtWaitForAlertByThreadId → KeWaitForAlertByThreadId
             │              ├─ +0x57 ~ +0x6c  주소 ≠ 0 · 유저 모드 · 스레드 +0x320 표 있음
             │              ├─ +0x29b ~ +0x2c2  칸 0 ~ 7 중 (칸 & 0x7ffffffffffffffc) == 주소 인 칸
             │              ├─ +0x2db  그 칸 bit 0 켬 → +0x2e3 칸에 씀
             │              │    (대기 중 CPU 에서 내려가면 KiAbProcessPreContextSwitch +0x25f/+0x269 가 bit 0 을 항목 +0x19 로)
             │              └─ +0x1e3  KiAbUmPostWait(스레드, 주소)   +0x88 bit 0 끔 · +0xd2 bit 63 끔 · +0xdc 칸에 씀 (락 주소는 그대로)
             └─ +0x12b  RtlAbPreAcquire(락, 칸)     칸 ≠ 0 이라 +0x6 JNZ → +0x43 RAX = 칸 → 끝. 부르는 쪽은 RAX 안 씀
```

### (a) 칸 bit 0 — ntdll 엔 없고, 커널이 대기 직전에 켠다 (결과서 1)

- **ntdll 에서 켜는 줄 0곳** `[정적]` (검색한 꼴 기준).
  - 표 주소를 받는 길은 TEB `+0x1850` 읽기뿐이다. 그 줄 92 = 읽기 91 + 쓰기 1 (`LdrpAcquireSchedulerSharedDataSlot +0x92`). 읽는 함수 56개.
  - 칸에 쓰는 꼴은 셋뿐: ① 빈 칸에 **락 주소 그대로** 적기 (`RtlAcquireSRWLockShared +0x39`, `RtlAcquireSRWLockExclusive +0x32`, `RtlEnterCriticalSection +0x4a`, `RtlAbPreAcquire +0x3a`, `LdrpCallTlsInitializers +0x63` 등. 락 주소는 정렬돼 bit 0 = 0) ② **bit 1 켜기** `or byte [칸],2` 22곳 (즉시값은 모두 `0x2`) ③ 비우기 `mov [칸],0`.
  - 즉시값 1 로 칸에 `or` / `bts` 하는 줄은 0. `lock bts [락],0` 은 락 값 자체의 bit 0 이다 (run41 과 같음).
  - 칸 포인터를 인자로 받는 둘: `RtlpAcquireSRWLockSharedContended` 는 `RtlAbPreAcquire` 에만 넘기고, `RtlpAcquireSRWLockExclusiveContended` 는 받은 `RDX` 를 홈 칸에 두기만 한다 (`+0x7`).
  - 다른 모듈: System32 PE 4119개 중 TEB `+0x1850` 을 읽는 꼴은 **ntdll.dll 만**. kernel32 · KernelBase · ucrtbase · test.exe · dll1 · dll2 는 0 (한계: `r12` · `rsp` 기반 꼴은 못 잡음 — 결과서).
- **켜는 곳 = 커널 `nt!KeWaitForAlertByThreadId +0x2db`** `[정적]` (`OR byte [RBP-0x20],SIL` — 칸 값 복사본 bit 0 → `+0x2df` · `+0x2e3 MOV [RDX+R8*8],RAX` 칸에 도로 씀).
  - 오는 길: ntdll `NtWaitForAlertByThreadId` syscall → 커널 `NtWaitForAlertByThreadId` (`+0x2f`) → `KeWaitForAlertByThreadId(모드, 시간, 주소)`. ntdll 쪽 예: `RtlpAcquireSRWLockSharedContended +0x116` (락 = `RSI`).
  - 켜는 조건 넷 (`.asm`): 기다리는 주소 ≠ 0 (`+0x57`/`+0x5a`) · **유저 모드** (`+0x5c CMP R13B,SIL` — `SIL = 1`) · 이 스레드에 표가 있음 (`+0x62 [RDI+0x320]` = `_KTHREAD.SchedulerSharedSystemSlot`) · 칸의 락 주소 == 기다리는 주소 (`+0x2b4 ~ +0x2c2`).
  - 끄는 곳: 깨어난 뒤 `+0x1de KiAbUmPostWait` → `+0x88 AND byte [RSP+0x50],0xfe`. 같은 함수가 bit 63 도 끈다 (`+0xd2`). **락 주소 부분은 그대로 둔다.**
  - 켜진 bit 0 을 쓰는 곳: `KiAbProcessPreContextSwitch +0x25f` / `+0x269` (커널 항목 `+0x19` = 1), `KiAbUmPostWait +0x9d` (항목 `+0x19` 켜진 것만 풀어 줌).
  - 이 표를 해석하는 커널 함수는 넷 (`KiAbProcessPreContextSwitch` · `KiAbProcessPostContextSwitch` · `KeWaitForAlertByThreadId` · `KiAbUmPostWait`) + `+0x320` 만 보는 `KeAbUserModeEntryFree` (결과서 — 마스크 상수 줄과 `+0x320` 읽기의 교집합). run41 은 `KiAb*` · `KeAb*` 이름만 훑어서 `KeWaitForAlertByThreadId` 의 이 줄을 놓쳤다.
- 뜻 `[추측: 이름]`: bit 0 = "이 스레드가 **이 락 때문에 유저 모드 대기에 들어갔다**". 이름은 PDB 에 없다.
- test.exe: 시동 중 TLS 락을 기다릴 일은 없을 것 `[추측]` (`LdrpCallTlsInitializers +0x6d CMPXCHG` 가 바로 성공할 것) → bit 0 은 한 번도 안 켜질 것. 사진 P1a 의 8칸은 모두 0 (TLS 콜백 전이라 아직 아무 칸도 안 적힘 — 아래 대조).

| 비트 | 뜻 | 켜는 곳 | 끄는 곳 | 표시 |
|---|---|---|---|---|
| bit 0 | 유저 모드 대기 중 (이름 `[추측]`) | **커널 `KeWaitForAlertByThreadId +0x2db`** | 커널 `KiAbUmPostWait +0x88` | 위치 [정적], 뜻 [추측] |

### (b) `RtlpAcquireSRWLockSharedContended +0x126` 의 `RtlAbPreAcquire` — 늘 효과 없음 (결과서 2)

- **`RtlAbPreAcquire(락 RCX, 칸 RDX)` 는 인자만 본다.** 전역 칸 · 플래그는 없다 `[정적]`.
  - `+0x3 TEST RDX,RDX` → `+0x6 JNZ +0x43` → `+0x43 MOV RAX,RDX` / `+0x46 RET`: **칸이 주어지면 그대로 돌려주고 끝** (읽기만).
  - 칸이 0 일 때만 진짜 일: `+0x13` TEB `+0x1850` 표 → 없으면 0 → 칸 0 ~ 7 중 **값 0 인 빈 칸** (`+0x28`) → `+0x3a` 락 주소 적기 → 그 칸 주소 돌려줌. 8칸이 다 차면 0.
  - 곧 "칸이 없으면 새로 잡고, 있으면 그대로" 헬퍼다. 칸 0 으로 부르는 예: `RtlTryAcquireSRWLockShared +0x8 XOR EDX,EDX` → `+0x11` (잡기 실패면 `+0x4e RtlAbPostRelease` 로 되돌림).
- **`+0x126` 은 늘 "그대로" 쪽이다** `[정적]`: `+0x21 MOV R14,RDX` (칸, 함수 안에서 이후 `R14` 읽기는 `+0x11b` · `+0x120` 둘뿐) → `+0x11b TEST R14,R14` / `+0x11e JZ +0x12b` → `+0x120 RDX = R14` → `+0x126` → `+0x12b MOV EAX,[RBP-0xc]` (반환값 덮음). 칸 · 락 · 전역 어느 것도 안 바뀐다.
- 왜 이렇게 짰나는 **코드로 못 정한다** `[추측]`. 정황 둘 (결과서):
  1. 깨어난 뒤에도 칸의 락 주소는 그대로다 (`KiAbUmPostWait` 는 bit 0 · 63 만 지움) — 다시 적을 필요가 없다.
  2. 배타 쪽 `RtlpAcquireSRWLockExclusiveContended` 에는 같은 호출이 없다 (`NtWaitForAlertByThreadId` 는 `+0x146` 에서 부르지만 `RtlAbPreAcquire` 0). 방어적으로 부른 코드가 남은 것으로 보인다 `[추측]`.
- test.exe: 이 호출이 생기는지는 **칸 값이 아니라 인자 `R14 ≠ 0`** 에 달렸다. test.exe 는 표가 있어 (사진) TLS 락 칸 `RDX ≠ 0` → 기다렸다 깨면 호출은 생기지만 효과 0. 시동 중엔 경합이 없을 것 `[추측]` → `RtlpAcquireSRWLockSharedContended` 자체가 안 불릴 것.

### (c) 표를 받는 조건 둘 + `LdrpAllocateSchedulerSharedData` 본문 (결과서 3)

- **조건 ① "목록이 비지 않음" = `LdrpAllocateSchedulerSharedData` 가 성공했다** `[정적]`.
  - 목록 머리 `LdrpSchedulerSharedDataListHead` (rva `0x1d2930`) 는 `.data` 에서 **파일에 없는 0 구간** (raw 크기 `0x2000` < 오프셋 `0x6930`) 이라 처음엔 0. 그래서 `+0x180b ~ +0x1820` 이 빈 목록 (`Flink = Blink = &머리`) 으로 만든 뒤 Allocate 를 부른다. AcquireSlot 은 `+0x2d` (머리 = 자기) 와 `+0x32` (머리 = 0) 둘 다 본다.
  - 부르는 쪽은 Allocate 반환값을 안 본다. Allocate 가 실패하면 목록이 빈 채 → ① 불통과 → TEB 칸 0.
  - 목록에 **끼우는 줄은 ntdll 전체에 `LdrpAllocateSchedulerSharedData +0xa6` 하나**, 빼는 줄은 없다. 이 목록 주소를 쓰는 줄 전수: 비우기 셋 (`LdrpInitializeProcess` · `RtlCloneUserProcess` · `RtlCompleteProcessCloning`), 끼우기 하나, 읽기 하나 (AcquireSlot).
  - Allocate 본문 순서: ① `+0xc` `LdrpIsSecureProcess ≠ 0` 이면 `0xC00000BB` (`STATUS_NOT_SUPPORTED`) 로 끝, syscall 없음 ② `+0x2c NtSetInformationProcess(-1, 0x70, &출력, 8)` — 출력 = **섹션 핸들** (커널 `+0xf17 PspSchedulerSharedDataRegionCreate`, run65) ③ `+0x57 RtlAllocateHeap(LdrpHeap, …, 0x18)` 0 채움, 실패면 `0xc000009a` + `+0xd3 NtClose` ④ `+0x70` 노드 `+0x10` = 핸들 → 목록 락 독점 안에서 머리 앞에 끼움.
  - 한 줄: **목록에 `0x18`B 노드 (Flink · Blink · `+0x10` 섹션 핸들) 를 넣는다. syscall 은 `NtSetInformationProcess` 0x70 하나 (+ 실패 정리 `NtClose`).**
- **조건 ② `PEB.NtGlobalFlag2 & 0x10` 꺼짐 = "이 기능을 끄지 않았다"** — 값이 정해지는 길 `[정적]`:
  1. 커널 `MmCreatePeb +0x4ac MOV EAX,[NtGlobalFlag2]` → `+0x4b2 MOV [RBX+0x7c4],EAX` (커널 전역 복사).
  2. 그 커널 전역: 부팅 때 `InitBootProcessor +0x3e2 AND [CmNtGlobalFlag2],0x201f` → `+0x494` · `+0x49a OR [NtGlobalFlag2],EAX` (레지스트리 값의 허용 비트 `0x201f` 안만, `0x10` 은 허용 비트). 실행 중엔 `NtSetSystemInformation +0x1a28` 도 쓴다 (클래스는 안 봄).
  3. ntdll `LdrpInitializeExecutionOptions +0x587 LEA R8,[L"GlobalFlag2"]` → `+0x59c RtlQueryApplicationKeyOption` → 성공이면 `+0x5a9 MOV [RBX+0x7c4],EAX` — **IFEO 에 `GlobalFlag2` 값이 있으면 덮어씀.**
  - 비트 `0x10` 이름은 PDB 에 없다. 코드로 아는 건 효과뿐 (켜지면 표를 안 받음) → "스케줄러 공유 데이터 (락 소유 칸 표) **끄는 스위치**" 로 읽힌다 `[추측: 이름 풀이]`. 같은 칸 다른 비트: `0x1` `LdrpInitializeExecutionOptions +0x71c`, `0x2` `LdrpIncludeAlternateForwarders +0x16` 이 읽음.
- 첫 스레드만 Allocate 를 부른다. 새 스레드는 `LdrpInitializeThread +0x50` 에서 AcquireSlot 만 (같은 노드의 핸들을 씀). 복제 길 둘 (`RtlCompleteProcessCloning` · `RtlCloneUserProcess`) 도 "비우기 → Allocate → AcquireSlot" 같은 꼴이다.
- **test.exe 는 둘 다 통과 → 표를 받는다** `[관찰]` run44 P1a (아래 대조). 그래서 `LdrpCallTlsInitializers +0x36 TEST R8,R8` / `+0x39 JZ` 는 안 건너뛰고 칸 적기 (`+0x63`) 를 하는 길이다 `[정적 + 조건 값 관찰]` (run41 "표가 0 이면 건너뜀" 쪽 아님).

### (d) `THREADINFOCLASS` `0x38` · `0x39` — PDB 에 없음 (결과서 4)

- `ntdll.dll.pdb` (enum 138개) · `ntoskrnl.exe.pdb` (enum 417개) 어디에도 `THREADINFOCLASS` · `_THREAD_INFORMATION_CLASS` · `_PROCESSINFOCLASS` 가 없다. `ms/sym/*.pdb` **29개 전부** 문자열 검색도 0. `ThreadBasicInformation` · `ThreadHideFromDebugger` 같은 다른 열거자도 6개 PDB 에서 0 → 이 enum 이 공개 PDB 에 통째로 빠져 있다 `[정적]`.
- 그래서 이름은 계속 `[추측]` (외부 비공식 헤더 기억: `0x38 = ThreadUpdateLockOwnership`, `0x39 = ThreadSchedulerSharedDataSlot`). MS 공개 헤더에도 이 번호는 없다고 기억한다 `[추측: 기억]`.
- 커널이 하는 일 `[정적]` (`NtSetInformationThread` 점프표 — `+0x1ee ADD EBX,-3` · `+0x1f1 CMP EBX,0x36` · `+0x1f4 JA` → `0xC0000003`):
  - **`0x38`** (`+0x5a2`): 길이 정확히 16 · 둘째 dword 0 · **현재 스레드만** (`+0x61c`, 아니면 `0xC00000BB`) · 첫 8바이트 = 칸 번호 → `+0x630 KeAbUserModeEntryFree` (`+0x14 [스레드+0x478] UserAbEntries` · `+0x78` 커널 항목 반납 · `+0xa1` 칸 bit 63 끔). 한 줄: **"내 칸 N 의 커널 항목을 돌려주고 칸 bit 63 을 끈다."**
  - **`0x39`** (`+0x58e`): 길이 정확히 24 · 출력도 있는 클래스 (`+0x199` → `+0x1a9 ProbeForWrite`) · 첫 dword 0 · 둘째 8바이트 = 섹션 핸들 → `+0xbd5 PspSchedulerSharedDataType` 으로 객체 → `+0xc2c PspSchedulerSharedDataRegionSlotAllocate` → `+0xc44 ~ +0xc49` 셋째 8바이트 = 표의 유저 주소. 한 줄: **"이 스레드에 8칸 표를 배정하고 그 유저 주소를 돌려준다."** 버퍼 모양 `{0, 핸들, 출력}` 은 ntdll `LdrpAcquireSchedulerSharedDataSlot +0x5a ~ +0x7f` 와 맞다.
  - 이 점프표의 마지막 클래스가 `0x39` 다. `0x3a` 부터는 `0xC0000003` (`STATUS_INVALID_INFO_CLASS`).

### 사진 로그 대조 (run44, 새로 돌린 것 아님 — 결과서 + flow-writer)

P1a = `LdrpInitializeProcess +0x199d` 직전 (곧 `+0x183c` 뒤, TLS 콜백 전). `ms/logs/04_baggage-read_P1a.txt`:

| 줄 | 값 | 무엇과 맞나 |
|---|---|---|
| 5133 | `LdrpSchedulerSharedDataListHead` (`0x7ffafb832930`) = `0x25996f33940` / `0x25996f33940` | Flink = Blink → **노드 1개** → 조건 ① 통과 |
| 5120 | `LdrpHeap` 칸 = `0x25996f30000` | 노드 `0x25996f33940` 이 그 힙 안 (Allocate 의 `RtlAllocateHeap(LdrpHeap)`) |
| 5128 | `0x7ffafb8328e0` 첫 8바이트 0 (`LdrpIsSecureProcess`, `LdrpAllocateSchedulerSharedData +0xc` 가 읽는 칸) | 보안 프로세스 아님 → syscall 길 |
| 1842 | `PEB +0x7c4 NtGlobalFlag2 : 0` | `& 0x10` 꺼짐 → 조건 ② 통과 |
| 2108 | `TEB +0x1850 SchedulerSharedDataSlot : 0x00000259'96cb0000` | 표를 받았다 (`+0x92` 를 탔다) |
| 3441 ~ 3445 | 그 표 8칸 모두 0 | TLS 콜백 전이라 아직 칸 적기 전. bit 0 · 1 · 63 도 0 |

- `[관찰]` 은 "그 순간 메모리에 그 값이 있었다" 이다. `+0x92` 를 타는 순간을 본 것은 아니다.
- P1a 보다 뒤 (TLS 콜백이 칸을 적고 지운 뒤) 의 표 값은 이 바퀴에 안 읽었다.

## 확인된 것

- `[정적]` 칸 bit 0 을 켜는 곳은 커널 `KeWaitForAlertByThreadId +0x2db` (유저 모드 대기 · 주소 일치 · 표 있음), 끄는 곳은 `KiAbUmPostWait +0x88`. ntdll 에는 켜는 줄이 없다 (꼴 기준). 커널은 컨텍스트 스위치 때 이 비트를 항목 `+0x19` 로 옮긴다 (`KiAbProcessPreContextSwitch +0x25f` / `+0x269`).
- `[정적]` ntdll 이 칸에 쓰는 꼴은 셋뿐 (락 주소 · bit 1 · 0). bit 1 켜기 22곳.
- `[정적]` `RtlAbPreAcquire` 는 둘째 인자만 본다. 칸 ≠ 0 이면 그대로 돌려줌. `RtlpAcquireSRWLockSharedContended +0x126` 은 칸 ≠ 0 일 때만 부르고 반환값을 안 쓴다 → 늘 효과 없음. 배타 쪽엔 같은 호출이 없다.
- `[정적]` 조건 ① = Allocate 가 노드를 끼움 (유일한 끼우기 `+0xa6`, 빼기 없음, 반환값 안 봄). 조건 ② = `PEB +0x7c4 & 0x10` — 커널 `MmCreatePeb +0x4b2` 가 커널 전역 `NtGlobalFlag2` 를 복사하고, IFEO `GlobalFlag2` 가 있으면 `LdrpInitializeExecutionOptions +0x5a9` 가 덮어씀.
- `[관찰]` run44 P1a: 목록 노드 1개 · `NtGlobalFlag2 = 0` · `LdrpIsSecureProcess = 0` · TEB `+0x1850 = 0x25996cb0000` · 표 8칸 0. → test.exe 는 표를 받는 길.
- `[정적]` `THREADINFOCLASS` enum 은 공개 PDB 29개에 없다. 클래스 `0x38` = 칸 N 커널 항목 반납 + bit 63 끔, `0x39` = 표 배정 + 유저 주소 돌려줌, `0x3a` 부터 `0xC0000003`.

## 결과서와 다르게 적은 곳 · 보탠 것

- 결과서 "동적으로 확인할 것" 2 의 `dq @$teb+0x1850 L8` 은 **TEB 칸 자체부터 8칸**을 찍는다. 표를 보려면 `dq poi(@$teb+0x1850) L8` 이다 (run44 덤프 읽기도 `poi(…+0x1850)` 로 읽음 — P1a 3441줄). 이 문서 · flow.md 4장은 `poi` 꼴로 적었다.
- flow-writer 보탬: 사진 P1a 5120줄 (`LdrpHeap`) · 5128줄 (`LdrpIsSecureProcess`) 을 결과서 줄 번호와 같이 대조 표에 넣었다 (결과서는 `0x7ffafb8328e0` 주소만).
- 결과서의 `RtlAllocateHeap` 자리 "`+0x3b ~ +0x57`" 은 인자 싣기부터 call 까지다. 표에는 `_calls.tsv` 대로 call `+0x57` / ret `+0x5c` 로 적었다.

## 정정

- 없음. run41 의 "bit 0 = `[공백]`, ntdll 에서 켜는 줄 못 찾음" 은 **틀린 게 아니라 비어 있던 것**이다 (ntdll 에 없다는 결론은 그대로 맞음). 그래서 run41 문서 맨 위엔 안내를 안 넣었다. flow.md 2.7 표의 bit 0 줄만 `[공백]` → 위치 `[정적]` 으로 바꿨다.

## 이전 run 에서 닫은 것

- run41 "아직 확인 필요 — 정적 (대기열 Q74)" 1 ~ 4 → 모두 닫힘 (위 (a) ~ (d)). 4 는 "PDB 에 없음" 확정이라 이름은 `[추측]` 그대로.
- run41 "아직 확인 필요 — 동적" 둘째 줄 "`[TEB+0x1850]` 이 0 이 아닌지" → **사진으로 이미 답** (`0x25996cb0000`, P1a 2108줄). "몇 번 칸에 락 주소가 적히나" 는 남음 (아래).

## 아직 확인 필요

### 동적 (결과서 "동적으로 확인할 것" — 사진으로 닫힌 것은 뺌. 대기열 번호는 오케스트레이터가 붙임 — 결과서는 Q25 쪽에 붙이자고 함)

- **1** test.exe 시동 중 `ntdll!RtlpAcquireSRWLockSharedContended` (rva `0x15f0c`) 입구에 **서는지** (test.exe 조건). 안 서면 `+0x126` 호출은 시동 길에서 아예 없다.
- **2** `ntdll!LdrpCallTlsInitializers+0x6d` 직후 `dq poi(@$teb+0x1850) L8` — 칸 0 에 `LdrpTlsLock` 주소 (`ntdll+0x1d2460`) 가 적히고 bit 0 · 1 · 63 이 모두 0 인지 (기대: 칸 0 — 사진 P1a 에 8칸이 다 비어 있었다). 풀 때 `+0x1ab` 에서 `dq @rsi L1` (run41 의 Q25 확인과 같은 자리).
- **3** (선택, 비추천) `nt!KeWaitForAlertByThreadId+0x2db` — 모든 대기에 걸린다. 꼭 보려면 `@r13b == 1 && @r15 != 0` 조건 + test.exe 프로세스 조건.

### 정적 (결과서 "다음 요청 후보")

- ① (중간) 커널 `PspSchedulerSharedDataRegionSlotAllocate` · `PspSchedulerSharedDataRegionSlotFree` 본문 — 칸 표 한 개 크기, 스레드마다 표를 어떻게 나누나, `_KTHREAD +0x320` · `+0x464 SchedulerSharedOffset` 채우기. PE_emulator 가 클래스 `0x39` 를 기본 대답에서 진짜 값으로 바꿀 때 필요.
- ② (작음, 급하지 않음) 커널 `CmNtGlobalFlag2` 가 읽는 레지스트리 값 이름, `NtGlobalFlag2` 비트 `0x1` · `0x2` · `0x10` 이름 후보.
- ③ (작음) `NtSetSystemInformation +0x1a17 ~ +0x1a28` 이 어느 클래스로 `NtGlobalFlag2` 를 바꾸나.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- 질문 1 (bit 0) · 2 (`+0x126`) · 4 (클래스 이름): **영향 없음 (맞음).** 우리는 TEB `+0x1850` = 0 이라 칸 쓰기가 없고 `R14 = 0` 이라 `+0x126` 호출 자체가 안 생긴다. 이름은 코드에 영향 없음.
- 질문 3 (조건 둘): **부분 틀림 — 이미 기록된 갈라짐.** 우리는 Allocate 가 노드를 끼우지만 핸들이 0 이고, TEB `+0x1850` = 0 이다 (진짜는 `≠ 0` `[관찰]` P1a). 조건 둘 통과 길은 같다. 기록 자리: `../PE_emulator/_workspace/orchestrator/98_scheduler-alloc.md` (와 `74_scheduler-slot.md`) — 결과서가 읽은 것.
  - 그 결과 진짜 Windows 는 TLS 콜백 둘레에서 칸 적기 (`+0x63`) · 지우기 (`+0x1ed`) 를 하고, 우리는 `+0x36` / `+0x39` 에서 건너뛴다 `[정적]`. 칸 적기 · 지우기 자체는 메모리 쓰기뿐이라 syscall 수는 안 달라질 것 `[추측]` (bit 63 이 켜져야 `+0x1e8` syscall 이 생김 — 시동 땐 안 켜질 것, run41).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntoskrnl · ntdll pdb 줄, 근거 로그 run110), 2.1 `MmCreatePeb` 표 `+0x7c4` 줄, 2.4 run65 절 "스케줄러 공유 데이터" 줄, 2.7 머리 · "TLS 락 소유 칸 표" 절 (조건 둘 · bit 0 줄 · `RtlAbPreAcquire` · 클래스 이름), 4장 Q74 줄 `(해결됨)` · Q25 TLS 길 줄 · 새 줄 셋 (실행 확인 · 정적 남은 것 · PE_emulator), 5장 두 표. 그림은 안 고침.
- 짐 지도 (`baggage.md`): 3.3 `SchedulerSharedDataSlot` 줄 · 3.8 `LdrpSchedulerSharedDataListHead` 두 줄 · 7.6.5 노드 줄 근거 · 7.2 `_PEB` `끝까지 0` 에 `NtGlobalFlag2` 근거 (칸 수 그대로 — 3장으로 옮길지는 오케스트레이터 결정) · 새 7.41 · 기준 · 8장. **칸 · 줄 · 2장 숫자 그대로.**

## 디버거 메모 / 도구 메모

- 표 칸을 볼 때는 `@$teb+0x1850` 이 아니라 `poi(@$teb+0x1850)` 이다 (TEB 칸은 표의 **주소**). → windbg-kd-runbook pitfalls 에 넣자고 보고.
- 커널 "칸 비트를 바꾸는 줄" 은 함수 이름 (`KiAb*` · `KeAb*`) 으로 찾으면 빠진다 (run41). 마스크 상수 `0x7ffffffffffffffc` 와 `[스레드+0x320]` 읽기의 교집합으로 찾아야 `KeWaitForAlertByThreadId` 가 나온다 (결과서 방법).
- 공개 PDB 에 없는 enum 은 `llvm-pdbutil dump -types` 의 `LF_ENUM` 이름과 다른 열거자 문자열 (같은 enum 이면 있어야 할 이름) 둘로 "없다" 를 확인했다 (결과서).

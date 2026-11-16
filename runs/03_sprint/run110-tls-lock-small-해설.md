# run110 tls-lock-small — 해설

> 이 글은 [run110 문서](run110-tls-lock-small.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q74** — 2.7 TLS 락 소유 칸 표에 남은 작은 정적 넷 (a) · (b) · (c) · (d) (`_workspace/plan.md`, 이번에 **끝**, 한 바퀴로)
  - Q74 는 [run41](../02_sprint/run41-tls-lock-detail-해설.md) (sast/36) 이 남긴 "새 정적 공백" 넷이다.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q25** (run110 몫이 덧붙음, 새 번호 없음)
    - 정적 → 새 **Q198 ① ~ ③**
    - 짐 지도 `NtGlobalFlag2` 를 7.2 → 3.1 로 옮기는 일 → **다음 바퀴** (run111 flow-writer 몫)
- 근거:
  - run 문서 [`run110-tls-lock-small.md`](run110-tls-lock-small.md) (가장 중요)
  - 요청서 [`_workspace/sast/107_tls-lock-small.md`](../../_workspace/sast/107_tls-lock-small.md) · 결과서 [`107_tls-lock-small_result.md`](../../_workspace/sast/107_tls-lock-small_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다.** 글은 기준 · 2.1 · 2.4 · 2.7 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): **새 짐 칸 없음, 칸 · 줄 · 2장 숫자 그대로.** 고친 줄 · 근거만 (3절).
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 커널(ntoskrnl) 은 **Ghidra 프로젝트를 안 열었다.** 그때 다른 decompiler 가 쓰고 있어서다. 이미 있던 `.asm`, PDB 파일 읽기, 스크래치 `objdump` 풀이만 썼다.
  - 이 글의 `[관찰]` 은 모두 **run44 사진 P1a 로그를 다시 읽은 것** 이다 (2.7). 새로 찍은 사진이 아니다.
  - "N곳" 같은 숫자는 run41 때처럼 **검색한 꼴 기준** 이다. 모양이 조금 다른 줄은 빠질 수 있다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **유저 모드로 기다리기** (`NtWaitForAlertByThreadId`) → 2.3
- **파일에 없는 0 구간** → 2.5
- 이미 푼 말은 링크만 건다:
  - **SRW 락** (유저 쪽 잠금 = **열쇠**), 독점 / 공유 잠금 (명부실 열쇠) → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md) · [run23 해설 2.3](../02_sprint/run23-load-dependent-해설.md#23-핵심--줄간선은-여기서-긋는다)
  - 빠른 잡기와 **경합 갈래** (`…Contended`) → [run28 해설 2.5](../02_sprint/run28-tls-init-call-해설.md)
  - TEB `+0x1850` = 직원마다 받는 **열쇠 대출 카드** (8칸), 칸 모양 (락 주소 + 표시 셋), 거름망(마스크) `0x7ffffffffffffffc` → [run41 해설](../02_sprint/run41-tls-lock-detail-해설.md) 2.3 · 2.4
  - **AutoBoost** → [run41 해설 2.5](../02_sprint/run41-tls-lock-detail-해설.md). 한 줄로만 다시: 락을 쥔 스레드가, 그 락을 기다리는 스레드 때문에 **잠깐 우선순위를 빌리는** 장치로 알려져 있다 `[추측: 일반적으로 알려진 설명]`. 이 표를 커널 쪽에서 쓰는 함수 이름이 `KiAb*` · `KeAb*` 인 까닭이다 (`Ab` = AutoBoost 풀이도 `[추측]`).
  - **컨텍스트 스위치** (CPU 에서 내려가기 = 계산대 **교대**) → [run41 해설 2.6](../02_sprint/run41-tls-lock-detail-해설.md)
  - **정보 클래스** (창구 하나에 내는 **서류 번호**) → [run41 해설 2.2](../02_sprint/run41-tls-lock-detail-해설.md#22-한-줄씩-읽는-법)
  - **스케줄러 공유 데이터** (관리사무소가 가게 안에 갖다 놓는 **카드 묶음 상자**) → [run65 해설 2.9](run65-ldr-init-bodies-해설.md)
  - **이전 모드** (이 시스템 콜을 유저가 냈나 커널이 냈나) → [run29 해설](../02_sprint/run29-continue-start-해설.md) "새 말 — 이전 모드"
  - **IFEO** = **가게 이름별 특별 지시** → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 3.4
  - **복제 길** (exe 없이 있는 프로세스를 본떠 짓는 길) → [run10 해설](../02_sprint/run10-allocate-process-해설.md) "보통 길과 복제 길"
  - 사진 P1a → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)
  - 빠른 실패 `INT 0x29` → [run24 해설](../02_sprint/run24-work-queue-해설.md) 2.3

### 이번 바퀴에서 조심할 곳 셋

1. **이번에 찾은 커널 길은 test.exe 시동 길에 없을 것이다** `[추측]`.
   - bit 0 을 켜는 곳은 **락을 기다릴 때만** 탄다. 시동 중엔 TLS 락을 다툴 다른 스레드가 없을 것이다.
   - 그래서 지도 그림은 그대로다. 뜻만 글로 붙였다.
2. **"위치" 와 "뜻" 의 태그가 다르다.**
   - bit 0 을 **켜고 끄는 줄** 은 `[정적]` 이다.
   - bit 0 이 **무슨 뜻인지** ("이 락 때문에 유저 모드 대기에 들어감") 는 `[추측]` 이다. 이름이 PDB 에 없다.
3. **오프셋은 call 자리가 기본이다.**
   - 다만 2.1 트리는 run 문서 "호출 관계" 처럼 **ret 자리** 로 적었다. 숫자가 5 쯤 다르면 call / ret 차이다 (예: `LdrpAcquireSchedulerSharedDataSlot` call `+0x1837` ↔ ret `+0x183c`).

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = **신청 창구**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**
- TEB = 직원 **개인 안내판**, 프로세스 힙 = 가게 **창고**
- run41 해설에서 이어 쓰는 카드 그림 (run 문서의 "도서관 · 사서 · 책" 을 **관리사무소 · 자물쇠** 로 바꿔 부른 것):
  - 락 소유 칸 표 (8칸) = 직원마다 받는 **열쇠 대출 카드**
  - 칸에 락 주소 = "**○○ 자물쇠 쥐고 있음**"
  - bit 63 = 관리사무소 **도장** ("우리 장부에도 적어 뒀음")
  - bit 1 = 직원이 적는 "**지금 반납 중**"
  - 클래스 `0x38` = "**장부에서도 지워 주세요**" 신청서
- run65 해설에서 이어 쓰는 상자 그림:
  - `LdrpAllocateSchedulerSharedData` = **상자 신청** (관리사무소가 카드 묶음 상자를 가게 안에 갖다 놓아 줌)
  - `LdrpAcquireSchedulerSharedDataSlot` = **카드 받기** (상자에서 내 카드 한 장)
- **이번에 더하는 것** (run 문서 그대로 옮긴 것):
  - bit 0 = 관리사무소가 찍는 "**대기실 들어감**" 표시. 직원이 그 자물쇠가 풀리길 기다리며 관리사무소 **대기실** 에 들어갈 때 찍고, 나올 때 지운다.
  - `NtGlobalFlag2` = 가게 **규칙판**. `0x10` 칸 = "카드 안 씀" 표시 (이름은 `[추측]`).

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) | 표시 |
|---|---|---|
| (a) 칸 bit 0 은 누가 켜나? | ntdll 이 아니라 **커널 `KeWaitForAlertByThreadId +0x2db`**. 끄는 곳 `KiAbUmPostWait +0x88`. ntdll 엔 켜는 줄 0곳 (꼴 기준) | 위치 `[정적]`, 뜻 `[추측]` |
| (b) `RtlpAcquireSRWLockSharedContended +0x126` 의 `RtlAbPreAcquire` 는 왜 효과가 없나? | `RtlAbPreAcquire` 는 **둘째 인자 (칸) 하나만** 본다. 칸이 주어지면 그대로 돌려준다. `+0x126` 은 칸 ≠ 0 일 때만 부르고 **반환값도 안 쓴다** → 늘 효과 없음 | `[정적]` (짠 까닭은 `[추측]`) |
| (c) 카드 받기 조건 둘은 무슨 뜻인가? | ① 목록이 차 있음 = `LdrpAllocateSchedulerSharedData` 가 노드를 끼웠음 · ② `PEB.NtGlobalFlag2 & 0x10` 꺼짐 = 끄는 스위치가 안 켜짐 | `[정적]` (`0x10` 이름은 `[추측]`) |
| (c) test.exe 는? | **둘 다 통과 → 카드를 받는다** (TEB `+0x1850` = `0x25996cb0000`) | `[관찰]` run44 P1a |
| (d) 클래스 `0x38` · `0x39` 공식 이름은? | **PDB 29개 어디에도 없다** (확정). 이름은 `[추측]` 그대로. 커널이 하는 일은 코드로 봤다 | `[정적]` |
| 앞 run 과 어긋난 곳은? | **없다.** 정정 없음. run41 의 bit 0 `[공백]` 은 틀린 게 아니라 **비어 있던 것** | — |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 **돌아올 자리(ret)** 다. 숫자는 run 문서·앞 run 들 그대로.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): … NtCreateUserProcess
      └─ 커널 MmCreatePeb +0x4b2  PEB+0x7c4 NtGlobalFlag2 = 커널 전역 사본       ← 이번 (c) ②

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ LdrpInitializeProcess
        ├─ … LdrpInitializeExecutionOptions +0x5a9  IFEO GlobalFlag2 면 덮어씀    ← 이번 (c) ②
        ├─ +0x182c  LdrpAllocateSchedulerSharedData   (상자 신청)                ← 이번 (c) ①
        ├─ +0x183c  LdrpAcquireSchedulerSharedDataSlot (카드 받기)               ← 이번 (c), (d) 0x39
        │      ⋯ 사진 P1a (+0x199d 직전) — 여기서 찍힌 값으로 test.exe 를 맞춰 봄 ← 이번 (c)
        └─ +0x1bbb  … LdrpCallTlsInitializers   (TLS 콜백 앞뒤로 카드 칸 적기·지우기)
                     └─ 락이 막혔을 때만: RtlpAcquireSRWLockSharedContended      ← 이번 (b)
                          └─ NtWaitForAlertByThreadId ══▶ 커널 KeWaitForAlertByThreadId ← 이번 (a)
   └─ NtContinue ═▶ … test.exe 진입점 → main
```

- 지도의 **큰 길은 그대로** 다.
- 이번 결과는 run41 이 붙여 둔 2.7 "TLS 락 소유 칸 표" 절의 **물음표 넷** 을 지운 것이다.
- (a) 와 (b) 는 **락이 막혔을 때만** 타는 곁길이다. test.exe 시동 길엔 없을 것이다 `[추측]`.
- (c) 는 시동 길 위에 있다. 사진으로 test.exe 값까지 맞췄다.

### 무엇이 궁금했나

run41 이 남긴 작은 질문 넷이다.

1. **(a)** 카드 칸의 **bit 0** 은 누가 켜나? run41 은 ntdll 을 다 뒤져도 못 찾았다.
2. **(b)** `RtlpAcquireSRWLockSharedContended +0x126` 의 `RtlAbPreAcquire(락, 칸)` 은 칸을 안 바꾸는 "효과 없는 호출" 로 보였다. **왜 그런가?**
3. **(c)** 카드 받기 (`LdrpAcquireSchedulerSharedDataSlot`) 의 조건 둘은 **무슨 뜻** 인가? 목록은 누가 채우나? test.exe 는 어느 쪽인가?
4. **(d)** `NtSetInformationThread` 클래스 `0x38` · `0x39` 의 **공식 이름** 은?

### 바퀴 전에 알던 것

- `[정적]` (run41) 칸 bit 0 은 커널이 **읽고** (`KiAbProcessPreContextSwitch +0x25f` → 항목 `+0x19`), 대기가 끝나면 **지운다** (`KiAbUmPostWait +0x88`). 켜는 줄은 ntdll 에서 못 찾았다 → `[공백]`.
- `[정적]` (run41) `RtlAbPreAcquire` 는 칸이 0 이 아니면 그대로 돌려준다 (`+0x3` · `+0x6` → `+0x43`). 그래서 `+0x126` 호출은 칸을 안 바꾼다. **왜 부르나** 는 몰랐다.
- `[정적]` (run41) 카드 받기 조건 둘: `LdrpSchedulerSharedDataListHead` 가 비지 않음, PEB `NtGlobalFlag2` (`+0x7c4`) 의 `0x10` 꺼짐. 안 맞으면 TEB `+0x1850` 은 0 으로 남고, `LdrpCallTlsInitializers` 는 칸 적기를 **건너뛴다** (`+0x36` / `+0x39`).
- `[정적]` (run65) `LdrpAllocateSchedulerSharedData` 는 `NtSetInformationProcess(-1, 0x70, …)` 로 섹션 핸들을 받아 노드에 넣는다.
- `[관찰]` (run44 P1a) TEB `+0x1850 = 0x25996cb0000`. 다만 run41 은 이걸 test.exe 조건과 **맞춰 보지 않았다.**
- `[추측]` (run41) 클래스 이름 `0x38 = ThreadUpdateLockOwnership`, `0x39 = ThreadSchedulerSharedDataSlot` (외부 헤더 기억). PDB 에 enum 이 없었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 카드 받기와 대기실

run 문서 "호출 관계" 를 줄였다. 숫자는 run 문서 그대로.

```
첫 스레드 (test.exe 메인)
LdrpInitializeProcess
 │  +0x180b ~ +0x1820  목록 락 = 0, 목록 머리 = 빈 목록
 ├─ +0x182c  LdrpAllocateSchedulerSharedData              (상자 신청)   반환값 안 봄
 │            ├─ +0xc   LdrpIsSecureProcess ≠ 0 이면 0xC00000BB 로 끝 (syscall 없음)
 │            ├─ +0x31  NtSetInformationProcess(-1, 0x70, &핸들, 8)
 │            ├─ +0x5c  RtlAllocateHeap(LdrpHeap, …, 0x18)     노드 +0x10 = 핸들
 │            └─ +0x94 ~ +0xa6  목록에 끼우기 (목록 락 독점 안)   ← ntdll 에서 끼우는 유일한 줄
 └─ +0x183c  LdrpAcquireSchedulerSharedDataSlot(TEB)      (카드 받기)
              ├─ ① 목록 빔? (+0x2d/+0x30, +0x32/+0x35)  ② PEB+0x7c4 & 0x10? (+0x40/+0x47)
              │     → 하나라도면 풀고 끝 (TEB 칸 0)
              ├─ +0x89  NtSetInformationThread(-2, 0x39, {0, 핸들, 0}, 0x18)
              └─ +0x92  TEB+0x1850 = 받은 표 주소        test.exe: 0x25996cb0000 [관찰] P1a

락이 막혔을 때 (시동 길엔 없을 것 [추측])
LdrpCallTlsInitializers (또는 RtlAcquireSRWLockShared)
 └─ RtlpAcquireSRWLockSharedContended(락, 칸, 값)
      ├─ +0x11b  NtWaitForAlertByThreadId(락)  ══▶ 커널 KeWaitForAlertByThreadId
      │            ├─ +0x2db  칸의 락 주소 == 기다리는 주소 인 칸에 bit 0 켬   (대기실 들어감)
      │            └─ +0x1e3  KiAbUmPostWait  +0x88 bit 0 끔 · +0xd2 bit 63 끔  (대기실 나옴)
      └─ +0x12b  RtlAbPreAcquire(락, 칸)   칸 ≠ 0 → 그대로 돌려줌. 부르는 쪽은 안 씀
```

- 위 덩어리 (카드 받기) 는 `[정적]` + test.exe 값 `[관찰]`.
- 아래 덩어리 (대기실) 는 줄마다 `[정적]`. test.exe 에서 타는지는 `[추측]` (안 탈 것).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — bit 0 을 켜는 커널 줄

```
      │            ├─ +0x2db  칸의 락 주소 == 기다리는 주소 인 칸에 bit 0 켬
```

- **누가**: 커널 `nt!KeWaitForAlertByThreadId`. ntdll 이 `NtWaitForAlertByThreadId` 시스템 콜을 내면, 커널 `NtWaitForAlertByThreadId` 가 `+0x2f` 에서 이 함수를 부른다.
- **어디서**: `+0x2db` — `OR byte [RBP-0x20],SIL`. `[RBP-0x20]` 은 칸 값을 스택에 **복사해 둔 것** 이고, `SIL` 은 1 이다. 그러니 복사본의 bit 0 을 켠다.
  - 이어서 `+0x2df` 에서 그 값을 다시 꺼내고, `+0x2e3 MOV [RDX+R8*8],RAX` 로 **칸에 도로 쓴다.** (`RDX` = 표, `R8` = 칸 번호)
- **누구를**: 부르기가 아니라 **쓰기** 다. 대상은 이 스레드의 카드 칸 하나.
- **무엇을 위해**: "이 직원은 **이 자물쇠 때문에** 대기실에 들어갔다" 를 카드에 표시하려고 — 이렇게 읽힌다 `[추측: 이름]`.
- 태그: 줄 위치 `[정적]` (`.asm`), 뜻 `[추측]`.

#### 한 줄 읽기 ② — 카드 받기의 둘째 조건

```
              ├─ ① …  ② PEB+0x7c4 & 0x10? (+0x40/+0x47)
```

- **누가**: ntdll `LdrpAcquireSchedulerSharedDataSlot`. 첫 스레드에선 `LdrpInitializeProcess` 가 call `+0x1837` / ret `+0x183c` 로 부른다.
- **어디서**: `+0x40 TEST byte [PEB+0x7c4],0x10` → `+0x47 JNZ`. 켜져 있으면 카드 신청을 **건너뛴다.**
- **누구를**: 부르기가 아니라 PEB 칸 **읽기** 다. `+0x7c4` = `NtGlobalFlag2` (PDB `_PEB` 이름).
- **무엇을 위해**: 가게 규칙판에 "카드 안 씀" 이 켜져 있으면 카드를 안 받으려고. `0x10` 의 이름은 PDB 에 없어서 "끄는 스위치" 는 **효과로 붙인 별명** 이다 `[추측: 이름]`.
- 태그: 줄 `[정적]`, test.exe 의 값 0 `[관찰]` run44 P1a (2.7).

### 2.3 (a) bit 0 — ntdll 엔 없고, 커널이 대기 직전에 켠다

천천히 간다. "ntdll 엔 없다" → "그럼 어디서" → "언제 켜고 끄나" → "무슨 뜻인가".

#### 1단계 — ntdll 에 켜는 줄이 **0곳** 이라고 어떻게 말하나 `[정적]`

- 카드를 찾는 길은 TEB `+0x1850` 을 읽는 것뿐이다.
  - ntdll 에서 `0x1850` 이 든 줄은 **92** = 읽기 91 + 쓰기 1 (`LdrpAcquireSchedulerSharedDataSlot +0x92`).
  - 읽는 함수는 **56개** (run41 과 같은 숫자).
- 그 56개가 칸에 **쓰는 꼴** 은 딱 셋뿐이었다:
  1. 빈 칸에 **락 주소 그대로** 적기 — 예 `RtlAcquireSRWLockShared +0x39`, `RtlEnterCriticalSection +0x4a`, `RtlAbPreAcquire +0x3a`, `LdrpCallTlsInitializers +0x63`.
     - 락 주소는 정렬돼 있어서 **bit 0 은 원래 0** 이다.
  2. **bit 1 켜기** `or byte [칸],2` — **22곳**. 즉시값은 모두 `0x2`.
  3. 비우기 `mov [칸],0`.
- 즉시값 1 로 칸에 `or` / `bts` 하는 줄은 **0** 이다.
  - `lock bts [락],0` 은 여럿 있지만, 이건 **락 값 자체** 의 bit 0 이다 (run41 2.4 "헷갈리기 쉬운 것" 과 같다).
- 칸 포인터를 인자로 받는 함수 둘도 봤다:
  - `RtlpAcquireSRWLockSharedContended` → `RtlAbPreAcquire` 에 넘기기만 한다.
  - `RtlpAcquireSRWLockExclusiveContended` → 받은 `RDX` 를 홈 칸에 두기만 한다 (`+0x7`).
- 다른 모듈도 봤다: System32 PE **4119개** 중 TEB `+0x1850` 을 읽는 꼴은 **ntdll.dll 만**. kernel32 · KernelBase · ucrtbase · test.exe · dll1 · dll2 는 0.
  - 한계: `r12` · `rsp` 기반 꼴은 이 검색이 못 잡는다 (결과서).
- 비유: 가게 안 모든 직원의 손글씨를 다 봤는데, 카드에 "대기실" 표시를 쓰는 사람이 **없었다.**

#### 2단계 — 그럼 어디서? 이번엔 **찾는 법** 을 바꿨다

- run41 은 커널을 **함수 이름** (`KiAb*` · `KeAb*`) 으로 훑었다. 그래서 이 줄을 놓쳤다.
- 이번엔 **칸 모양** 으로 찾았다 `[정적]`:
  - 카드 칸을 해석하는 커널 함수라면 거름망 `0x7ffffffffffffffc` 를 쓴다 (run41 2.4).
  - 그리고 스레드의 표 자리 `[스레드+0x320]` (`_KTHREAD.SchedulerSharedSystemSlot`) 를 읽는다.
  - 둘 다 하는 함수 = 교집합 → `KiAbProcessPreContextSwitch` · `KiAbProcessPostContextSwitch` · **`KeWaitForAlertByThreadId`** · `KiAbUmPostWait` **넷** (+ `+0x320` 만 보는 `KeAbUserModeEntryFree`).
- 넷 중 이름에 `Ab` 가 없는 하나가 **`KeWaitForAlertByThreadId`** 다. 거기에 켜는 줄이 있었다.
- 비유: 서명 (함수 이름) 으로 찾으면 빠지는 사람이 있다. **쓰는 도장 모양** (거름망) 과 **여는 서랍** (`+0x320`) 으로 찾으니 나왔다.

#### 새 말 — 유저 모드로 기다리기 (`NtWaitForAlertByThreadId`)

- 락이 막혔을 때 유저 코드는 계속 돌며 기다리지 않고, 이 시스템 콜로 **커널에 가서 잠든다.** 그러다 깨어나면 돌아온다 (`KiAbUmPostWait` 가 "깨어난 뒤" 에 불린다 — run 문서).
  - 누가 어떻게 깨우는지는 이번 근거에 없다. 이름으로 보면 "스레드 번호로 깨워 주길 기다린다" 는 뜻으로 읽힌다 `[추측: 이름 풀이]`.
- 비유: 직원이 자물쇠가 풀릴 때까지 문 앞에서 서성이지 않고, 관리사무소 **대기실** 에 들어가 앉아 있는 것.
- ntdll 쪽 예: `RtlpAcquireSRWLockSharedContended` call `+0x116` / ret `+0x11b` (`+0x113 MOV RCX,RSI` — 인자 = 락 주소).

#### 3단계 — 켜는 조건 넷 `[정적]` (`KeWaitForAlertByThreadId.asm`)

넷 다 맞아야 켠다.

1. 기다리는 주소가 0 이 아님 (`+0x57` / `+0x5a`)
2. **유저 모드** 에서 온 대기 (`+0x5c CMP R13B,SIL` — `SIL = 1`). `R13B` 는 [이전 모드](../02_sprint/run29-continue-start-해설.md) 다 (커널 `NtWaitForAlertByThreadId +0x1e` 가 `[스레드+0x232]` 에서 꺼냄).
3. 이 스레드에 표가 있음 (`+0x62 [RDI+0x320]`)
4. 칸 0 ~ 7 중 **(칸 & 거름망) == 기다리는 주소** 인 칸이 있음 (`+0x29b ~ +0x2c2`, 비교는 `+0x2b4 ~ +0x2c2`)

- 비유: "대기실에 들어온 직원이 **카드를 가지고 있고**, 카드에 **바로 이 자물쇠** 가 적혀 있으면" 그 칸에 표시를 찍는다.
- 4번이 재미있다. 기다리는 대상은 **락** 이고, 카드에 적힌 것도 **락 주소** 다.
  - 공유 잠금 길에선 빈 칸에 락 주소를 적는 줄 (`RtlAcquireSRWLockShared +0x39`, `LdrpCallTlsInitializers +0x63`) 이 경합 함수를 부르는 줄 (`+0x59`, `+0x113`) 보다 **앞** 에 있다 `[정적]`.
  - 그래서 대기실에 들어갈 땐 이미 카드에 그 락이 적혀 있고, 4번이 맞아떨어진다 — 줄 순서에서 읽은 내 정리다 `[추측]` (실행으로 본 건 아님).

#### 4단계 — 끄는 곳, 그리고 켜진 bit 0 을 쓰는 곳 `[정적]`

- **끄는 곳**: 깨어난 뒤 `KeWaitForAlertByThreadId +0x1de` → `KiAbUmPostWait` → **`+0x88 AND byte [RSP+0x50],0xfe`** (bit 0 끔).
  - 같은 함수가 `+0xd2` 에서 bit 63 도 끄고, `+0xdc` 에서 칸에 도로 쓴다.
  - **락 주소 부분은 그대로 둔다.** (이게 2.4 의 (b) 와 이어진다.)
- **쓰는 곳**:
  - `KiAbProcessPreContextSwitch +0x25f` / `+0x269` — 대기 중 CPU 에서 내려갈 때 (교대), bit 0 이 켜져 있으면 커널 장부 항목 `+0x19` = 1.
  - `KiAbUmPostWait +0x9d` — 깨어난 뒤, 항목 `+0x19` 가 켜진 것만 풀어 준다.
- 비유: 대기실에 들어간 직원이 교대 시간에 걸리면, 관리사무소가 장부에도 "이 사람 **대기실에서** 교대됨" 을 적는다. 나올 때 그 표시만 골라 정리한다.

#### 5단계 — 뜻 `[추측: 이름]`

- 근거: 켜는 곳이 **유저 모드 대기 직전**, 끄는 곳이 **깨어난 직후** 다 `[정적]`.
- 그래서: bit 0 = "이 스레드가 **이 락 때문에 유저 모드 대기에 들어갔다**" 로 읽힌다.
- 아직 확인 아님: 이름이 PDB 에 없다. 그래서 뜻은 `[추측]` 으로 남는다.

#### 카드 칸 표 — bit 0 줄만 바뀌었다

run41 해설 2.4 표에서 바뀐 줄 하나 (나머지 bit 1 · bit 63 · 락 주소 줄은 그대로).

| 비트 | 뜻 | 켜는 곳 | 끄는 곳 | 비유 | 표시 |
|---|---|---|---|---|---|
| bit 0 | 유저 모드 대기 중 (이름 `[추측]`) | **커널 `KeWaitForAlertByThreadId +0x2db`** | 커널 `KiAbUmPostWait +0x88` | "대기실 들어감" | 위치 `[정적]`, 뜻 `[추측]` (전: `[공백]`) |

- 이렇게 보면 카드 표시 셋 중 **둘 (bit 0 · bit 63) 은 관리사무소만** 찍고 지운다. 직원 (ntdll) 이 직접 쓰는 건 **bit 1 ("반납 중")** 하나뿐이다 `[정적]`.

#### test.exe 에서는

- 시동 중 TLS 락을 기다릴 일은 없을 것 `[추측]` — `LdrpCallTlsInitializers +0x6d CMPXCHG` (빠른 잡기) 가 바로 성공할 것이다.
- 그러면 bit 0 은 **한 번도 안 켜질 것** `[추측]`.
- 사진 P1a 의 8칸은 모두 0 이다 `[관찰]`. 다만 P1a 는 TLS 콜백 **전** 이라 아직 아무 칸도 안 적힌 때다 (2.7). 그러니 이 사진은 "bit 0 이 안 켜졌다" 의 증거는 **아니다.**

### 2.4 (b) `RtlAbPreAcquire` — 왜 늘 효과가 없나

#### `RtlAbPreAcquire(락, 칸)` 이 하는 일 `[정적]`

- **인자만 본다.** 전역 칸 · 플래그는 없다.
- 갈림은 첫머리 한 번이다:
  - `+0x3 TEST RDX,RDX` → `+0x6 JNZ +0x43` → `+0x43 MOV RAX,RDX` / `+0x46 RET`
  - 곧 **칸이 주어지면 그대로 돌려주고 끝** (읽기만).
- 칸이 0 일 때만 진짜 일을 한다:
  - `+0x13` TEB `+0x1850` 으로 표를 찾고 → 없으면 0
  - 칸 0 ~ 7 중 **값 0 인 빈 칸** 을 찾고 (`+0x28`) → `+0x3a` 락 주소 적기 → 그 칸 주소를 돌려줌
  - 8칸이 다 차면 0
- 한 줄: "**칸이 없으면 새로 잡고, 있으면 그대로**" 헬퍼다.
- 칸 0 으로 부르는 진짜 예: `RtlTryAcquireSRWLockShared +0x8 XOR EDX,EDX` → `+0x11`. 잡기에 실패하면 `+0x4e RtlAbPostRelease` 로 칸을 되돌린다.
- 비유: 창구 직원이 "카드 칸 번호 들고 왔어요?" 하고 묻는다. 들고 왔으면 **그 번호를 그대로 돌려준다.** 빈손이면 빈 칸을 찾아 적어 준다.

#### `+0x126` 은 늘 "그대로" 쪽이다 — 단계별로 `[정적]`

1. `+0x21 MOV R14,RDX` — 함수 첫머리에서 칸을 `R14` 에 받아 둔다. 이 뒤 `R14` 를 읽는 곳은 `+0x11b` · `+0x120` **둘뿐** 이다.
2. `+0x11b TEST R14,R14` / `+0x11e JZ +0x12b` — 칸이 0 이면 호출을 **건너뛴다.**
3. 그러니 `+0x126` 에 오면 늘 **칸 ≠ 0** 이다 → `RtlAbPreAcquire` 는 `+0x43` 으로 가서 칸을 그대로 돌려준다.
4. `+0x12b MOV EAX,[RBP-0xc]` — 돌아오자마자 `EAX` (반환값) 를 **덮어쓴다.** 받은 값을 안 쓴다.
5. 그래서: 칸 · 락 · 전역 **어느 것도 안 바뀐다.** "늘 효과 없음" 이 **확정** 됐다.

- 비유: 칸 번호를 들고 창구에 가서, 돌려받은 번호를 그 자리에서 **버리고** 나온다.

#### 왜 이렇게 짰나 — 코드로는 못 정한다 `[추측]`

정황 둘 (결과서):

1. 깨어난 뒤에도 칸의 락 주소는 **그대로** 다 — `KiAbUmPostWait` 는 bit 0 · 63 만 지운다 (2.3 4단계). 그러니 "깨어나서 칸을 다시 적을" 필요가 없다.
2. 짝인 독점 쪽 `RtlpAcquireSRWLockExclusiveContended` 엔 **같은 호출이 없다** (`NtWaitForAlertByThreadId` 는 `+0x146` 에서 부르지만 `RtlAbPreAcquire` 는 0).

- 그래서: "혹시 몰라 부른 (방어적인) 코드가 공유 쪽에만 남은 것" 으로 보인다 `[추측]`.
- 아직 확인 아님: 바이너리만으로는 **짠 사람의 의도** 를 증명할 수 없다.

#### test.exe 에서는

- 이 호출이 생기느냐는 칸 **값** 이 아니라 인자 **`R14 ≠ 0`** 에 달렸다.
- test.exe 는 카드가 있다 (2.7) → TLS 락 칸 `RDX ≠ 0` → 기다렸다 깨면 호출은 **생기지만 효과는 0** 이다 `[정적]`.
- 다만 시동 중엔 경합이 없을 것이라 `RtlpAcquireSRWLockSharedContended` 자체가 안 불릴 것이다 `[추측]` → 동적 Q25 (4절).

### 2.5 (c) 조건 ① "목록이 차 있음" = 상자 신청이 성공했다

#### 목록 머리는 처음에 0 이다 — 그래서 먼저 비운다 `[정적]`

- **새 말 — 파일에 없는 0 구간**: 섹션의 메모리 크기가 파일에 든 크기보다 크면, 남는 뒷부분은 파일에 내용이 없고 올릴 때 0 으로 채워진다. [run103 해설 2.8](run103-image-share-해설.md) 의 "**빈 방으로** 지을 쪽" (`VirtualSize > SizeOfRawData` 구간) 이 이것이다.
- 목록 머리 `LdrpSchedulerSharedDataListHead` (rva `0x1d2930`) 는 ntdll `.data` 의 그 구간에 있다 (raw 크기 `0x2000` < 오프셋 `0x6930`). 그래서 처음엔 **그냥 0** 이다.
- 0 은 "빈 목록" 모양이 아니다. 빈 목록은 `Flink = Blink = &머리` (자기 자신을 가리킴) 다.
  - 그래서 `LdrpInitializeProcess +0x180b ~ +0x1820` 이 먼저 **빈 목록 모양으로 만든 뒤** 상자 신청을 부른다.
  - 카드 받기는 둘 다 본다: `+0x2d` (머리 = 자기 → 빔) 와 `+0x32` (머리 = 0 → 한 번도 안 만듦).
- 비유: 관리실 칠판의 "상자 번호표 거는 줄" 은 처음엔 **아무것도 안 그려진 빈 칠판** 이다. 먼저 줄부터 그어 두고, 번호표를 건다.

#### 상자 신청 (`LdrpAllocateSchedulerSharedData`) 본문 — 넷 `[정적]`

| 순서 | 자리 | 하는 일 | 안 되면 |
|---|---|---|---|
| ① | `+0xc` | `LdrpIsSecureProcess ≠ 0` 인지 본다 | 보안 프로세스면 `0xC00000BB` (`STATUS_NOT_SUPPORTED`) 로 끝, **syscall 없음** |
| ② | call `+0x2c` / ret `+0x31` | `NtSetInformationProcess(-1, 0x70, &출력, 8)` — 출력 = **섹션 핸들** (커널 `+0xf17 PspSchedulerSharedDataRegionCreate`, run65) | 실패 상태를 들고 정리 |
| ③ | call `+0x57` / ret `+0x5c` | `RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 0x40000 \| 8, 0x18)` — 0 채운 `0x18`B 노드 | `0xc000009a` + `+0xd3 NtClose(핸들)` |
| ④ | `+0x70` → `+0x74 ~ +0xb2` | 노드 `+0x10` = 핸들 → 목록 락 **독점** 안에서 머리 앞에 끼움 (`+0x94 ~ +0xa6` 링크 4개) | `+0x87` 연결 검사가 틀리면 `+0x92 INT 0x29` (코드 3) |

- 한 줄: **목록에 `0x18`B 노드 (Flink · Blink · `+0x10` 섹션 핸들) 를 하나 넣는다. syscall 은 `NtSetInformationProcess` 0x70 하나 (+ 실패 정리 `NtClose`).**
- 비유: 관리사무소에 상자를 신청해 **상자 번호표** (섹션 핸들) 를 받고, 창고에서 쪽지 한 장 (노드) 을 꺼내 번호표를 붙인 뒤, 칠판 줄에 건다.

#### 목록을 만지는 줄 — 전부 셌다 `[정적]`

- **끼우는 줄은 ntdll 전체에 `LdrpAllocateSchedulerSharedData +0xa6` 하나.** 빼는 줄은 **없다.**
- 이 목록 주소를 쓰는 줄 전수: 비우기 셋 (`LdrpInitializeProcess` · `RtlCloneUserProcess` · `RtlCompleteProcessCloning`), 끼우기 하나, 읽기 하나 (카드 받기).
- 그래서 조건 ① 의 뜻이 또렷해진다: **"목록이 차 있음" = "상자 신청이 끝까지 성공했음".**

#### 상자 신청이 실패해도 아무도 모른다 `[정적]`

- `LdrpInitializeProcess` 는 상자 신청 반환값을 **안 본다.** `+0x182c` 뒤 곧장 `MOV RBX,[RSP+0x158]` 로 간다.
- 실패하면 → 목록이 빈 채 → 조건 ① 불통과 → TEB `+0x1850` 0 → TLS 콜백 둘레 칸 적기를 건너뜀 (run41).
- 실패 길은 셋: 보안 프로세스 (`+0xc`) / syscall 실패 / 힙 실패.
- 비유: 상자를 못 받아도 가게는 문을 연다. 직원들이 **카드 없이** 일할 뿐이다.

#### 누가 상자 신청을 부르나 `[정적]`

- 첫 스레드만: `LdrpInitializeProcess` call `+0x1827` / ret `+0x182c`.
- 새 스레드는 카드 받기만 한다: `LdrpInitializeThread` call `+0x50` / ret `+0x55` (같은 노드의 핸들을 씀).
- [복제 길](../02_sprint/run10-allocate-process-해설.md) 둘 (`RtlCompleteProcessCloning` `+0xec ~ +0x11c` · `RtlCloneUserProcess` `+0x2ab ~ +0x2d5`) 도 "비우기 → 상자 신청 → 카드 받기" 같은 꼴이다. test.exe 길은 아니다.

### 2.6 (c) 조건 ② "`NtGlobalFlag2 & 0x10` 꺼짐" = 끄는 스위치가 안 켜짐

#### 값이 정해지는 길 — 세 걸음 `[정적]`

```
부팅 때 (커널)
  InitBootProcessor +0x3e2  AND [CmNtGlobalFlag2],0x201f     레지스트리 값의 허용 비트만
                    +0x49a  OR  [NtGlobalFlag2],EAX           커널 전역에 넣음
test.exe PEB 만들 때 (커널)
  MmCreatePeb +0x4ac / +0x4b2   PEB+0x7c4 = 커널 전역 NtGlobalFlag2 (복사)
test.exe 로더 (ntdll)
  LdrpInitializeExecutionOptions +0x59c RtlQueryApplicationKeyOption(…, L"GlobalFlag2", …)
                                 +0x5a9 성공이면 PEB+0x7c4 덮어씀   (IFEO 에 값이 있을 때)
```

1. **관리사무소 전체 규칙** (커널 전역 `NtGlobalFlag2`) 은 부팅 때 레지스트리에서 정해진다. 허용 비트 `0x201f` 안만 받고, `0x10` 은 허용 비트다.
   - 실행 중엔 `NtSetSystemInformation +0x1a28` 도 이 전역에 쓴다. 어느 클래스인지는 이번에 안 봤다 → Q198 ③.
2. 가게를 열 때 관리사무소가 그 규칙을 **가게 규칙판** (PEB `+0x7c4`) 에 **베껴 적는다.**
3. 그다음 관리실이 [IFEO](../02_sprint/run10-allocate-process-해설.md) (**가게 이름별 특별 지시**) 를 보고, `GlobalFlag2` 값이 있으면 **덮어쓴다.**

- 비유: 본사 규칙을 가게 규칙판에 베껴 붙이고, 그 가게 이름 앞으로 따로 온 지시가 있으면 그걸로 고쳐 쓴다.

#### `0x10` 은 무슨 스위치인가 `[추측: 이름 풀이]`

- 근거: 이름은 PDB 에 없다. 코드로 아는 건 **효과뿐** — 켜지면 카드를 안 받는다 (`+0x47 JNZ`) `[정적]`.
- 그래서: "스케줄러 공유 데이터 (락 소유 칸 표) **끄는 스위치**" 로 읽힌다.
- 아직 확인 아님: 이름 · 레지스트리 값 이름은 Q198 ② 에서 찾아본다.
- 덤: 같은 칸의 다른 비트도 읽힌다 — `0x1` 은 `LdrpInitializeExecutionOptions +0x71c`, `0x2` 는 `LdrpIncludeAlternateForwarders +0x16` `[정적]`.

### 2.7 test.exe 는 둘 다 통과 — 사진으로 맞춰 보기 `[관찰]`

사진 P1a = `LdrpInitializeProcess +0x199d` 직전. 곧 카드 받기 (`+0x183c`) **뒤**, TLS 콜백 **전** 이다. 로그 `ms/logs/04_baggage-read_P1a.txt` (run 문서 "사진 로그 대조" 그대로).

| 줄 | 값 | 무엇과 맞나 |
|---|---|---|
| 5133 | 목록 머리 (`0x7ffafb832930`) = `0x25996f33940` / `0x25996f33940` | Flink = Blink → **노드 1개** → 조건 ① 통과 |
| 5120 | `LdrpHeap` 칸 = `0x25996f30000` | 노드가 그 힙 안 (상자 신청 ③) |
| 5128 | `LdrpIsSecureProcess` 칸 (`0x7ffafb8328e0`) 첫 8바이트 0 | 보안 프로세스 아님 → syscall 길 (상자 신청 ①) |
| 1842 | `PEB +0x7c4 NtGlobalFlag2 : 0` | `& 0x10` 꺼짐 → 조건 ② 통과 |
| 2108 | `TEB +0x1850 SchedulerSharedDataSlot : 0x00000259'96cb0000` | 카드를 받았다 (`+0x92` 를 탔다) |
| 3441 ~ 3445 | 그 표 8칸 모두 0 | TLS 콜백 전이라 아직 칸 적기 전. bit 0 · 1 · 63 도 0 |

#### 한 줄 읽기 — 5133줄

- **무엇**: 칠판 줄 머리의 앞 · 뒤 연결이 **같은 쪽지** (`0x25996f33940`) 를 가리킨다.
- **그래서**: 줄에 쪽지가 **딱 하나** 걸려 있다 → 상자 신청이 성공했다 → 조건 ① 통과.
- 쪽지 주소가 `LdrpHeap` (`0x25996f30000`, 5120줄) 근처라는 것도 "창고에서 꺼낸 쪽지" 와 맞는다.

#### 조심할 것 둘 (run 문서 그대로)

- `[관찰]` 은 "**그 순간 메모리에 그 값이 있었다**" 이다. `+0x92` 를 **타는 순간** 을 본 건 아니다.
- P1a 보다 뒤 (TLS 콜백이 칸을 적고 지운 뒤) 의 표 값은 이 바퀴에 안 읽었다.

#### 그래서 바뀌는 것 하나

- run41 은 `LdrpCallTlsInitializers +0x36 TEST R8,R8` / `+0x39 JZ` 에서 "표가 0 이면 건너뜀" 길도 있다고 적었다.
- test.exe 는 표가 있으니 **건너뛰지 않고** `+0x63` 칸 적기를 하는 길이다 `[정적 + 조건 값 관찰]`.
- 비유: test.exe 직원은 카드가 **있다.** 그러니 TLS 서류철 자물쇠를 쥘 때 카드에 적는다.

### 2.8 (d) 클래스 `0x38` · `0x39` — PDB 에 없음 확정

#### "없다" 를 어떻게 확인했나 `[정적]`

- `ntdll.dll.pdb` (enum 138개) · `ntoskrnl.exe.pdb` (enum 417개) 어디에도 `THREADINFOCLASS` · `_THREAD_INFORMATION_CLASS` · `_PROCESSINFOCLASS` 가 없다.
- `ms/sym/*.pdb` **29개 전부** 문자열 검색도 0.
- 같은 enum 이라면 있어야 할 **다른 이름** (`ThreadBasicInformation` · `ThreadHideFromDebugger`) 도 6개 PDB 에서 0.
- 그래서: 이 enum 은 공개 PDB 에 **통째로 빠져 있다.** 이름은 계속 `[추측]` (외부 비공식 헤더 기억: `0x38 = ThreadUpdateLockOwnership`, `0x39 = ThreadSchedulerSharedDataSlot`).
- 비유: 서류 번호표는 받았는데, 번호마다 붙은 **서류 이름 목록** 은 공개본에서 아예 찢겨 나가 있다.

#### 이름 대신 — 커널이 하는 일 `[정적]`

`NtSetInformationThread` 는 클래스 번호로 갈 자리를 표에서 찾는다 (점프표 = 서류 번호별 **담당 책상 배치표**). `+0x1ee ADD EBX,-3` · `+0x1f1 CMP EBX,0x36` · `+0x1f4 JA` → 범위 밖이면 `0xC0000003`.

| 클래스 | 담당 자리 | 받는 것 | 하는 일 (한 줄) | 비유 |
|---|---|---|---|---|
| `0x38` | `+0x5a2` | 길이 정확히 16 · 둘째 dword 0 · **현재 스레드만** (`+0x61c`) · 첫 8바이트 = 칸 번호 | `+0x630 KeAbUserModeEntryFree` → **내 칸 N 의 커널 항목을 돌려주고 칸 bit 63 을 끈다** | "장부에서도 지워 주세요" (run41) |
| `0x39` | `+0x58e` | 길이 정확히 24 · 출력 있음 · 첫 dword 0 · 둘째 8바이트 = 섹션 핸들 | `+0xbd5 PspSchedulerSharedDataType` 으로 상자 찾기 → `+0xc2c PspSchedulerSharedDataRegionSlotAllocate` → 셋째 8바이트 = **표의 유저 주소** | "카드 주세요" |

- `0x39` 버퍼 모양 `{0, 핸들, 출력}` 은 ntdll 카드 받기 `+0x5a ~ +0x7f` 가 만드는 모양과 **맞는다** `[정적]`.
- 이 점프표의 **마지막 클래스가 `0x39`** 다. `0x3a` 부터는 `0xC0000003` (`STATUS_INVALID_INFO_CLASS`).

### 2.9 PE_emulator 쪽 — 무엇이 맞고 무엇이 갈렸나

결과서 "지금 우리 맞나" 요약을 run 문서대로 옮긴다. `../PE_emulator` 코드는 flow-writer 도, 이 글도 직접 안 읽었다. 고칠지는 사용자 결정이다.

- **맞음 (영향 없음)** — (a) bit 0 · (b) `+0x126` · (d) 클래스 이름
  - PE_emulator 는 TEB `+0x1850` = 0 이라 칸 쓰기가 없다. 그러니 `R14 = 0` 이고 `+0x126` 호출 자체가 안 생긴다.
- **부분 틀림 (이미 기록된 갈라짐)** — (c)
  - PE_emulator 는 상자 신청이 노드를 끼우지만 **핸들이 0** 이고, TEB `+0x1850` 도 0 이다. 진짜는 `≠ 0` `[관찰]` P1a.
  - 조건 둘을 통과하는 길은 같다.
  - 결과: 진짜는 TLS 콜백 둘레에서 칸 적기 (`+0x63`) · 지우기 (`+0x1ed`) 를 하고, PE_emulator 는 `+0x36` / `+0x39` 에서 건너뛴다 `[정적]`.
  - 칸 적기 · 지우기는 메모리 쓰기뿐이라 syscall 수는 안 달라질 것 `[추측]` (bit 63 이 켜져야 `+0x1e8` syscall 이 생기는데, 시동 땐 안 켜질 것 — run41).

### 2.10 정리 — 비유로 한 번 더

- 개업 준비 때 관리실은 관리사무소에 **상자 신청** 을 한다. 받은 **상자 번호표** 를 쪽지에 붙여 칠판 줄에 건다. 칠판 줄은 원래 **빈 칠판** 이라 먼저 줄부터 긋는다.
- 직원은 **카드 받기** 를 한다. 칠판 줄에 쪽지가 있고, 가게 규칙판에 "카드 안 씀" 이 꺼져 있으면, 번호표를 내고 카드를 받아 개인 안내판에 꽂는다. test.exe 는 둘 다 맞아서 **카드를 받았다** (사진).
- 상자 신청이 실패해도 아무도 모른다. 그땐 직원들이 카드 없이 일한다.
- 직원이 자물쇠가 막혀 관리사무소 **대기실** 에 들어가면, 관리사무소가 카드의 그 자물쇠 칸에 "**대기실 들어감**" (bit 0) 을 찍는다. 나올 때 지운다. 자물쇠 이름은 그대로 둔다.
  - 그래서 대기실에서 나온 직원이 카드를 **다시 적을 필요가 없다.** 그런데도 나오자마자 창구에 칸 번호를 들고 가서 (`RtlAbPreAcquire`) 돌려받은 번호를 버리고 온다 — 효과 없는 들름이다.
- 카드 표시 셋 중 "대기실" · "도장" 은 관리사무소만, "반납 중" 은 직원만 쓴다.
- 서류 번호 `0x38` · `0x39` 의 공식 이름은 공개본에 없다. 하는 일은 코드로 알았다.

### 2.11 곁가지 — 도구 메모

run 문서 "디버거 메모 / 도구 메모" 를 옮긴다. 분석 결과엔 영향이 없다.

- **표 칸을 볼 때는 `poi` 를 붙인다.** 결과서는 `dq @$teb+0x1850 L8` 이라 적었는데, 이건 **TEB 칸 자체부터** 8칸을 찍는다. TEB 칸엔 표의 **주소** 가 들어 있으니, 표를 보려면 한 번 따라가야 한다:

  ```
  dq poi(@$teb+0x1850) L8
  ```

  - run 문서 · flow.md 4장은 이 `poi` 꼴로 적었다 (run44 덤프 읽기도 `poi(…+0x1850)` 로 읽음 — P1a 3441줄). windbg-kd-runbook 에 넣자는 제안이 보고됐다.
  - 비유: 개인 안내판에 꽂힌 건 **카드가 어디 있는지 적은 쪽지** 다. 쪽지를 보고 카드 자리로 가야 카드가 보인다.
- 커널에서 "칸 비트를 바꾸는 줄" 은 **함수 이름으로 찾으면 빠진다** (run41). 거름망 상수와 `[스레드+0x320]` 읽기의 교집합으로 찾아야 `KeWaitForAlertByThreadId` 가 나온다 (2.3 2단계).
- 공개 PDB 에 없는 enum 은 두 가지로 "없다" 를 확인했다: `LF_ENUM` 이름, 그리고 같은 enum 이면 있어야 할 **다른 열거자 이름** (2.8).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp · ntdll pdb 줄에 run110 없음 | **run110** 더함 + 근거 로그 줄 (run44 P1a 를 다시 읽은 줄 번호들, "새 동적 실행은 없다") |
| 2.1 `MmCreatePeb` 표 | `+0x7c4` 줄 없음 | 새 줄 `+0x7c4 NtGlobalFlag2` — 커널 전역 사본 (`+0x4ac` → `+0x4b2`), 그 전역의 부팅 길, IFEO 덮어쓰기 `[정적]`, 사진 값 0 `[관찰]` |
| 2.4 run65 절 "스케줄러 공유 데이터" | Allocate · AcquireSlot 사슬 (run65) | (run110 `[정적]`) **반환값 안 봄 → 실패해도 아무도 모름**, 실패 길 셋, 목록 머리 = 파일에 없는 0 구간, 끼우는 줄 하나, 복제 길 같은 꼴, 조건 ② 출처 + 사진 값 `[관찰]` |
| 2.7 "TLS 락 소유 칸 표" 머리 | run41 `[정적]` | run41 `[정적]`, **run110 `[정적]` + 조건 값 `[관찰]`** |
| 2.7 조건 둘 | 조건만 적힘 | 뜻 (① = 노드 끼움, ② = 끄는 스위치 `[추측: 이름]`) + **test.exe 둘 다 통과** `[관찰]` → `+0x39 JZ` 안 타고 `+0x63` 칸 적기 길 |
| 2.7 칸 표 bit 0 줄 | 모름 · 켜는 줄 못 찾음 · **`[공백]`** | 취소선 → **커널 `KeWaitForAlertByThreadId +0x2db` 켬, `KiAbUmPostWait +0x88` 끔** · 위치 **`[정적]`**, 뜻 `[추측]` |
| 2.7 클래스 이름 | PDB 에 enum 없음 (run41) | **"PDB 에 없음" 확정** (29개) + 커널 점프표로 본 `0x38` · `0x39` 일 `[정적]` |
| 2.7 `RtlAbPreAcquire` | 효과 없음, 까닭 모름 | 둘째 인자만 봄 · 반환값 안 씀 → **늘 효과 없음 확정** `[정적]`, 짠 까닭 `[추측]` |
| 4장 Q74 줄 | 정적 남은 넷 | **(해결됨, 정적 → run110)** |
| 4장 Q25 TLS 길 줄 | (run41) 메인 스레드 표 주소가 생기나 | 취소선 → **사진으로 답함** `0x25996cb0000`. 남은 것: `RtlpAcquireSRWLockSharedContended` 입구에 서나, `dq poi(@$teb+0x1850) L8`, (비추천) `+0x2db` |
| 4장 새 줄 셋 | — | 실행 확인 1 ~ 3 · 정적 남은 것 ① ~ ③ · PE_emulator 쪽 |
| 5장 두 표 | — | run110 줄 |

- 태그 변화를 한 줄씩:
  - bit 0 켜는 곳: `[공백]` → **`[정적]`** (커널 `+0x2db`). 뜻은 `[추측]`.
  - `+0x126` 효과 없음: `[정적]` (칸 안 바뀜) → `[정적]` **확정** (반환값도 안 씀). 짠 까닭은 `[추측]` 그대로.
  - 조건 ① 뜻: 모름 → **`[정적]`** (상자 신청 성공).
  - 조건 ② 출처: 모름 → **`[정적]`** (`MmCreatePeb +0x4b2` · IFEO `+0x5a9`). `0x10` 이름은 `[추측]`.
  - test.exe 가 카드를 받나: 사진 값만 있음 → **조건과 맞춰 봄** `[관찰]`.
  - 클래스 이름: `[추측]` → `[추측]` 그대로 (PDB 에 없음은 `[정적]` 확정).

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **새 짐 칸 없음. 칸 · 줄 · 2장 숫자 그대로.** 고친 줄 · 근거만이다.
  - 3.3 TEB `SchedulerSharedDataSlot` 줄 — 조건 둘의 뜻, test.exe 조건 값, 표 8칸 사진
  - 3.8 `LdrpSchedulerSharedDataListHead` 두 줄 — 처음 0 의 까닭 · 복제 길 비우기 / 끼우는 줄 하나 · 실패 길 · 사진 노드 1개
  - 7.6.5 노드 줄 근거, 7.2 `_PEB` `끝까지 0` 의 `NtGlobalFlag2` 에 쓰는 줄 둘 (칸 수 그대로)
  - 새 7.41 (Q74 가 넘긴 질문)
- **다음 바퀴 몫**: `NtGlobalFlag2` 를 7.2 → 3.1 로 옮긴다 (2장 숫자가 바뀜). 오케스트레이터 기본값으로 run111 flow-writer 가 한다.
  - 왜 옮기나: 늘 쓰는 줄 (`MmCreatePeb +0x4b2`) 이 나왔기 때문이다. plan 결정 기록은 이걸 run70 `MinimumStackCommit` 과 같은 꼴이라고 적었다 (2장 `_PEB` 숫자가 바뀌는 일이라 이번엔 근거만).

### 큰 그림에서 어떻게 이어지나

- 이번 바퀴도 run41 처럼 **잔가지 정리** 다. 지도의 화살표는 하나도 안 바뀌었다.
- 그래도 두 가지가 또렷해졌다:
  1. **카드 표시 셋의 주인이 다 정해졌다.** bit 0 · bit 63 은 관리사무소, bit 1 은 직원. ntdll 은 카드 칸에 "락 주소 · bit 1 · 0" 말고는 아무것도 안 쓴다 `[정적]`.
  2. **test.exe 가 카드를 받는 길이 조건까지 맞춰졌다** `[관찰]`. 그래서 TLS 콜백 둘레에서 칸 적기 · 지우기를 **한다.** PE_emulator 가 진짜와 갈리는 자리도 이것으로 짚였다 (2.9).
- 동적 (Q25) 에 들고 갈 예측이 늘었다: 시동 중 `RtlpAcquireSRWLockSharedContended` 에 **안 설 것**, TLS 락을 쥔 순간 카드 칸 0 에 `LdrpTlsLock` 이 적히고 bit 0 · 1 · 63 은 모두 0 일 것 `[추측]`.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 시동 중 `ntdll!RtlpAcquireSRWLockSharedContended` (rva `0x15f0c`) 에 **서나** (안 서면 `+0x126` 호출은 시동 길에 아예 없음) | 락 경합은 실행 때 일이다 | 동적 **Q25** — 입구 멈춤점, test.exe 조건 |
| TLS 락을 쥔 순간 카드 칸 값 — 칸 0 에 `LdrpTlsLock` (`ntdll+0x1d2460`) 이 적히고 bit 0 · 1 · 63 이 0 인가 | 사진 P1a 는 TLS 콜백 **전** 이라 칸이 비어 있었다. 락을 쥔 짧은 동안이라 사진 덤프로는 못 볼 것 `[추측]` | 동적 **Q25** — `ntdll!LdrpCallTlsInitializers+0x6d` 직후 `dq poi(@$teb+0x1850) L8`, 풀 때 `+0x1ab` 에서 `dq @rsi L1` |
| bit 0 이 켜지는 순간 | `KeWaitForAlertByThreadId +0x2db` 는 **모든 대기** 에 걸린다 | 동적 **Q25** (선택, 비추천) — `@r13b == 1 && @r15 != 0` + 프로세스 조건 |
| 커널이 표를 **어떻게 나눠 주나** — 표 한 개 크기, 스레드마다 표 나누기, `_KTHREAD +0x320` · `+0x464 SchedulerSharedOffset` 채우기 | 커널 `PspSchedulerSharedDataRegionSlotAllocate` · `…SlotFree` 본문을 안 봤다 (ntoskrnl Ghidra 를 못 열었다). PE_emulator 가 클래스 `0x39` 를 진짜 값으로 바꿀 때 필요 | 정적 **Q198 ①** (중간) |
| `CmNtGlobalFlag2` 가 읽는 **레지스트리 값 이름**, `NtGlobalFlag2` 비트 `0x1` · `0x2` · `0x10` 이름 후보 | 이번엔 값이 흐르는 줄만 봤다 | 정적 **Q198 ②** (작음, 급하지 않음) |
| `NtSetSystemInformation +0x1a17 ~ +0x1a28` 이 **어느 클래스** 로 `NtGlobalFlag2` 를 바꾸나 | 이번엔 쓰는 줄만 보고 클래스는 안 봤다 | 정적 **Q198 ③** (작음) |
| 이름 셋 — bit 0 · `NtGlobalFlag2 0x10` · 클래스 `0x38` / `0x39` | PDB 에 없다. 코드로는 못 닫는다 | `[추측]` 그대로 (Q198 머리에 적힘) |
| `+0x126` 을 **왜 짰나** | 바이너리로 짠 사람의 의도를 증명할 수 없다 | `[추측]` 그대로 |
| P1a 뒤 (TLS 콜백이 칸을 적고 지운 뒤) 의 표 값 | 이번 바퀴에 그 사진을 안 읽었다 | run 문서에 번호 없이 적힘 |
| 짐 지도 `NtGlobalFlag2` 를 3.1 로 옮기기 | 2장 숫자가 바뀌는 일이라 이번 바퀴엔 근거만 넣었다 | **다음 바퀴** (run111 flow-writer) |

---

## 5. 더 읽기

- run 문서: [`run110-tls-lock-small.md`](run110-tls-lock-small.md) — 호출 지점 표 전체, 호출 관계 트리, (a) ~ (d) 근거 줄, 사진 로그 대조, 결과서와 다르게 적은 곳, PE_emulator 쪽
- 정적 결과서: [`_workspace/sast/107_tls-lock-small_result.md`](../../_workspace/sast/107_tls-lock-small_result.md) — 질문별 답, 전수 검색 방법 (ntdll `objdump` · System32 4119개 · 커널 교집합), 동적으로 확인할 것, 다음 요청 후보
- 요청서: [`_workspace/sast/107_tls-lock-small.md`](../../_workspace/sast/107_tls-lock-small.md)
- flow.md: [`flow.md`](flow.md)
  - 2.7 "TLS 락 소유 칸 표 (TEB `+0x1850`)" 절 — 조건 둘, 칸 표 bit 0 줄, 클래스, `RtlAbPreAcquire`
  - 2.4 run65 절 "스케줄러 공유 데이터" 줄, 2.1 `MmCreatePeb` 표 `+0x7c4` 줄
  - 4장 Q74 · Q25 TLS 길 줄 · run110 새 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.3 · 3.8 · 7.2 · 7.6.5 · 7.41
- 앞 해설:
  - [run41 해설](../02_sprint/run41-tls-lock-detail-해설.md) — 카드 · 도장 · 반납 중 (2.1 ~ 2.7), 이번에 닫은 Q74 넷 (4절). 맨 위에 "고쳐 읽기 (run110)" 한 줄이 붙었다.
  - [run65 해설 2.9](run65-ldr-init-bodies-해설.md) — 상자 신청 · 카드 받기 사슬
  - [run44 해설](run44-baggage-snap-해설.md) — 사진 P1a
  - [run28 해설 2.5](../02_sprint/run28-tls-init-call-해설.md) — TLS 서류철 자물쇠, 경합 갈래

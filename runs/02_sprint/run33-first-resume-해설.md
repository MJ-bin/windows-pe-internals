# run33 first-resume — 해설

> 이 글은 [run33 문서](run33-first-resume.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.
>
> **정정 (run34)**: 2.6 의 "일반 생성에선 이 비교가 맞기 어려워 보인다" 는 반대다 — bit 9 는 `CREATE_SUSPENDED` 가 없을 때 켜져, 보통 생성은 `OwnerProcessId` = 부모 PID 그대로. → [run34 해설 2.7](run34-parent-leftovers-해설.md)

- 대기열: **Q40** — 2.2·2.3 작은 정적 확인 둘: `EPROCESS.Flags3.ProcessFirstResume` (`+0x5fc` bit 15, `PsMultiResumeThread` 가 켬) 를 **읽는 곳**, 조각 `FUN_1406b2db3` 의 **정체** (`KiApcInterrupt` 추정) (`_workspace/plan.md`, 이번에 **끝**)
  - **단계 3 "작은 정적부터 몰아서"** 의 바퀴다 (사용자 결정, 2026-10-23). run32(Q36) 다음이다.
  - 새 항목: **Q64** (동적), **Q65** (정적). 출처를 더한 항목: **Q19**, **Q42** (동적).
  - 다음 항목: **Q41** (plan.md).
- 근거:
  - run 문서 [`run33-first-resume.md`](run33-first-resume.md) (가장 중요)
  - 요청서 [`_workspace/sast/28_first-resume.md`](../../_workspace/sast/28_first-resume.md)
  - 결과서 [`_workspace/sast/28_first-resume_result.md`](../../_workspace/sast/28_first-resume_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 연 바이너리는 ntoskrnl 하나 (`ntkrnlmp.pdb`, flow.md 기준과 같은 GUID).
  - 이번엔 하네스에 없는 **임시 도구**를 썼다: 커널 파일 전체를 훑는 Ghidra 스크립트 둘, `.pdata` 를 읽는 작은 파서 (커밋 안 함, 2.9·2.10).

이 글은 run13 해설의 **뒷정리 편**이다.

- [run13 해설](run13-resume-thread-해설.md) 끝에 질문 두 개가 "Q40 (정적)" 으로 남아 있었다.
  - "첫 출근 허가 도장" (`ProcessFirstResume`) 을 **누가 보나**? ([run13 해설 2.7](run13-resume-thread-해설.md#27-풀어-주는-사슬--그리고-끼지-않는-psresumethread))
  - 쪽지를 펼치게 하는 **초인종 함수**의 이름이 정말 `KiApcInterrupt` 인가? ([run13 해설 2.6](run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리))
- 이번에 둘 다 코드로 답이 나왔다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 비트·플래그 → [run9 해설](run9-create-user-process-해설.md) 2절
- 핸들(**손잡이**) → [run9 해설 "핸들"](run9-create-user-process-해설.md#핸들)
- 직접 호출·간접 호출 → [run9 해설](run9-create-user-process-해설.md#직접-호출과-간접-호출)
- CFG(**교환원**) → [run28 해설 2.9](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)
- ETW(커널의 사건 기록 장치) → [run12 해설 2.12](run12-insert-thread-해설.md#212-그-밖에-확인된-것)
- `PsTerminateProcess` (가게 강제로 닫기) → [run12 해설 2.8](run12-insert-thread-해설.md#28-알림-받은-쪽이-안-돼-하면--psterminateprocess-로-가는-길)
- 멈춤 쪽지·신호등·어깨 두드리기 → [run13 해설 2.3](run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- IRQL("방해 금지" 표지판), 소프트웨어 인터럽트(**초인종** 예약) → [run13 해설 2.6](run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리)
- 부모가 `NtResumeThread` 를 쓴다 → [run14 해설 2.9](run14-create-process-internal-해설.md#29-ntresumethread-다-ntalertresumethread-가-아니다--run13-사슬과-이어진다)
- 표지판 1 → 0 은 `PspUserThreadStartup +0x22` 한 곳 → [run15 해설 2.9](run15-user-thread-startup-해설.md#29-run13-의-쪽지는-0x22-직후-펼쳐진다--고리-3개가-굳었다)

비유도 그대로 이어 쓴다.

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**
- 프로세스 = **가게**, 스레드 = **직원**, `_EPROCESS` = 관리사무소 쪽 가게 **장부**
- 멈춤 APC = 출근하자마자 펼쳐 볼 **쪽지**, `SuspendEvent` = **신호등**, APC 인터럽트 = **초인종** (run13)
- `ProcessFirstResume` = 장부에 찍는 "**이 가게 첫 직원 출근 허가 났음**" **도장** (run13·run14 해설)
  - run33 문서는 같은 것을 "첫 개업 허락 도장" 이라고 적었다. **같은 도장**이다.
  - 주의: [run15 해설](run15-user-thread-startup-해설.md#28-pspuserthreadstartup-안--출근-도장-다섯-칸) 의 "출근 도장 다섯 칸" 은 `PspUserThreadStartup` 창구 이야기라 **다른 것**이다. 이번 "도장" 은 장부에 찍히는 표시 하나다.
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- **이번에 더하는 비유**:
  - 커널 파일 전체 훑기 = **도서관 한 동을 처음부터 끝까지 뒤지기** (2.9)
  - `.pdata` = 책의 **목차**, PDB 공개 이름 = 장 **제목** (2.10)
  - 객체 형식 = 관리사무소의 **물건 종류별 규정 카드**, 닫기 콜백 = 그 카드에 적힌 **손잡이 반납 창구** (2.4)

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 셋**:

1. **도장을 보는 곳은 둘, 찍는 곳은 하나, 지우는 곳은 없다** `[정적]`.
   - 보는 곳 ① `PsMultiResumeThread +0x45` — 찍는 사람 자신이 "벌써 찍혀 있나?" 확인 → 2.3
   - 보는 곳 ② `PspProcessClose +0xf` — 프로세스 손잡이를 **반납받는 창구**. 도장이 없으면 "주인이 떠나는 중이면 이 가게도 같이 닫기" 를 검사한다 → 2.4 ~ 2.7
   - 찍는 곳은 `PsMultiResumeThread +0x4e` 하나. 관리실(ntdll)·ETW 로 **안 나간다** → 2.8
2. 그 "둘" 은 **커널 파일 하나(ntoskrnl.exe) 안에서, 네 겹 방법으로 잡히는 꼴 안에서** 의 결론이다. 도서관 **한 동**을 다 뒤진 것이지, 도서관 전체는 아니다 → 2.9
3. **`FUN_1406b2db3` = `KiApcInterrupt +0x273`** `[정적]`. 떨어진 조각이 아니라, Ghidra 가 **한 함수를 토막 내서 보여 준 것**이었다. 목차(`.pdata`)를 보고 확인했다. 그림 2 `h0` 상자 이름이 확정됐고, `KiDeliverApc` 를 부르는 자리는 **call `+0x3a6` / ret `+0x3ab`** 다 → 2.10 · 2.11

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

시간 순서로 놓았다. 오프셋은 **돌아올 자리(ret)** 다. 왼쪽 표시는 어느 직원(스레드)인지.

```
[부모 cmd.exe]   CreateProcessInternalW +0x30ee → NtCreateUserProcess            (run14·run9)
                  └ 커널: PspInsertThread +0x2e5 KeSuspendThread                  (run12·run13)
                          → 쪽지 넣기 + 신호등 빨강

[test.exe 첫]    KiStartUserThread +0x28 → PspUserThreadStartup                   (run4·run15)
                   +0x22  IRQL 1 → 0
                   ═▶ 초인종 = KiApcInterrupt                                     ← 이번 ③ 이름 확정
                        └ +0x3ab KiDeliverApc → +0x220 KiSchedulerApc → +0xf7 잠듦 (run13)

[부모 cmd.exe]   CreateProcessInternalW +0x5bd3 → NtResumeThread                  (run14)
                  └ 커널: NtResumeThread +0x9b PsMultiResumeThread
                            +0x45  도장 이미 있나?                               ← 이번 ① 보는 곳
                            +0x4e  도장 찍기                                      (run13)
                            → … 초록불 + 어깨 두드리기                           (run13)

[test.exe 첫]    깨어남 → … → ntdll → exe 진입점

[누가·언제인지 모름]  프로세스 손잡이 닫기
                  NtClose +0xe9 ObCloseHandleTableEntry +0x7ae (간접)
                    → PspProcessClose  +0xf 도장 있나?                           ← 이번 ② 보는 곳
```

- ①·② 는 **flow.md 2.2** (부모 쪽 마무리 ~ 첫 스레드 출발) 이야기다.
- ③ 은 **그림 2** `nt` 상자 안 `(1) 멈춤 APC 처리` 의 첫 상자 `h0` 이야기다.
- 마지막 줄(②)은 **언제 일어나는지 모른다.** `main` 전일 수도, 훨씬 뒤일 수도 있다 (2.7, Q64).

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. `PsMultiResumeThread +0x4e` 가 켜는 `_EPROCESS +0x5fc` bit 15 `Flags3.ProcessFirstResume` 를 **누가 읽나?**
   - flow.md 2.2 에 `[공백]` 으로 남아 있었다: "`ProcessFirstResume` 를 읽는 곳은 안 봤다."
2. `KiDeliverApc` 를 부르는 조각 `FUN_1406b2db3` 이 **정말 `KiApcInterrupt` 인가?**
   - flow.md 2.2 "멈춘 첫 스레드가 서는 곳" 6번이 `[추측]` 이었다: "`KiApcInterrupt` 의 떨어진 조각으로 보임."

### 바퀴 전에 알던 것

- `[정적]` (run13) `PsMultiResumeThread` (`NtResumeThread +0x9b`) 는 `KeResumeThread` 결과가 "이번에 완전히 풀림" 이면 bit 15 를 켠다 (`+0x4e` `OR.LOCK`, 이미 켜져 있으면 건너뜀).
- `[정적]` (run13) `NtAlertResumeThread` 길은 `PsMultiResumeThread` 를 안 거쳐 이 표시를 안 한다. (run14) 부모 KERNELBASE 는 `NtResumeThread` 쪽을 쓴다.
- `[정적]` (run13) `KiDeliverApc.c` 머리 `callers:` 에 `FUN_1406b2db3` 이 있다. `[추측]` `KiApcInterrupt` 의 떨어진 조각.
- `[정적]` (run15) 첫 스레드의 IRQL 이 1 → 0 으로 내려가는 자리는 `PspUserThreadStartup +0x22` 하나다. 초인종이 실제로 `+0x26` 에서 울리는지는 `[추측]`.

---

## 2. 찾은 것

### 2.1 한눈에 — 세 장면

run 문서 "호출 관계" 를 줄였다. 오프셋은 ret.

```
(장면 1) 부모 cmd.exe 스레드 — 풀어 주기                              (run13 + run33)
NtResumeThread
 └─ +0x9b  PsMultiResumeThread
      ├─ +0x25  KeResumeThread
      │  +0x32/+0x36  "이번에 완전히 풀림" 일 때만 ↓
      │  +0x45  Flags3 bit 15 이미 켜졌나              (보는 곳 ①)
      │  +0x4e  안 켜졌으면 OR.LOCK 으로 켬            (찍는 곳, 하나뿐)
      └─ +0x6f  EtwTiLogSuspendResumeThread            (bit 20 일 때)

(장면 2) 누가·언제인지 모름 — 프로세스 손잡이 닫기                    (run33, 대상은 간접)
NtClose
 └─ +0xe9  ObCloseHandleTableEntry
      └─ +0x7ae  (CFG 간접) 객체 형식 CloseProcedure = PspProcessClose   [추측: 대상]
            │  +0xf  Flags3 bit 15 켜졌나                (보는 곳 ②) → 켜짐: +0x1b return
            └─ +0x4c  PsTerminateProcess(0xc000010a)     (꺼짐 + 주인 PID 일치 + 주인 지워지는 중)

(장면 3) test.exe 첫 스레드 — 초인종                                   (run13 + run15 + run33)
PspUserThreadStartup +0x22 IRQL 0
 ═▶ KiApcInterrupt                                       [추측] 들어오는 자리 +0x26
      │  +0x38f HalPerformEndOfInterrupt 뒤 STI,  +0x390 GS:[0x187] 검사
      └─ +0x3ab  KiDeliverApc
            └─ +0x220  KiSchedulerApc
                  └─ +0xf7  KeWaitForSingleObject(SuspendEvent)   ← 여기서 잠듦
```

- 장면 1·2 의 이름과 오프셋은 `[정적]` 이다. 장면 2 의 간접 호출 대상이 `PspProcessClose` 라는 것만 `[추측]` 이다 (2.4).
- 장면 3 은 `KiApcInterrupt` 이름과 `+0x3ab` 가 이번에 `[정적]` 이 됐다. `═▶` (끼어들기) 가 `+0x26` 에서 들어오는지는 여전히 `[추측]` 이다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 반납 창구에서 도장 보기

장면 2 의 `+0xf  Flags3 bit 15 켜졌나` 줄.

- **누가**: `PspProcessClose` (rva `0x9b3b50`, `+0xf3` 까지 짧은 함수)
- **어디서**: `+0xf` (rva `0x9b3b5f`). 명령은 `TEST [RDX+0x5fc],0x8000` → `+0x19 JZ +0x22`.
- **누구를**: 부르는 게 아니라 **읽는** 줄이다. `RDX` = 닫히는 프로세스, `+0x5fc` = 그 장부의 `Flags3` 칸, `0x8000` = bit 15.
- **무엇을 위해**: 도장이 있으면 `+0x1b` 로 **바로 돌아가고**, 없으면 `+0x22` 아래 "주인이 떠나는 중인가" 검사로 내려가려고. `[정적]`

#### 한 줄 읽기 ② — 초인종 함수가 쪽지 배달을 부르는 자리

장면 3 의 `└─ +0x3ab  KiDeliverApc` 줄.

- **누가**: `KiApcInterrupt` (rva `0x6b2b40`)
- **어디서**: `+0x3a6` 에서 부르고, 돌아올 자리는 **`+0x3ab`** (절대 주소 `0x1406b2ee6` / `0x1406b2eeb`).
- **누구를**: `KiDeliverApc(0, 0, 트랩 프레임 추정)` — 셋째 인자가 트랩 프레임이라는 건 `[추측]`.
- **무엇을 위해**: 초인종이 울렸으니 **쌓인 쪽지(APC)를 배달**하려고. 멈춘 첫 직원이라면 이 배달 끝에 빨간불 앞에서 잔다 (run13). `[정적]`
- 콜스택으로 보면 `KiDeliverApc+0x220` **바로 아래 칸**이 이 자리일 것이다. 실제로 그렇게 보이는지는 Q42.

---

### 2.3 보는 곳 ① — 도장 찍는 사람 자신 (`PsMultiResumeThread +0x45`)

run13 에서 "도장을 찍는다(`+0x4e`)" 까지는 알았다. 이번에 보니 **찍기 바로 앞에서 한 번 본다.**

**순서** `[정적]`

1. `+0x38`: `RAX` = 그 스레드의 프로세스 (`[스레드+0x220]`).
2. `+0x3f MOV R8D,0x8000` — bit 15 마스크를 준비.
3. `+0x45 TEST [RAX+0x5fc],R8D` — 장부 `Flags3` 칸의 bit 15 를 본다.
4. `+0x4c JNZ +0x56` — **이미 켜져 있으면** `+0x4e` 를 건너뛴다.
5. 꺼져 있으면 `+0x4e OR.LOCK` 으로 켠다.

- 비유 (태그 없음): 도장 찍는 직원이 "벌써 찍혀 있나?" 먼저 보고, 있으면 그냥 넘어간다. **두 번 찍지 않는다.**
- 그래서 이 줄은 "도장을 **판단에 쓰는**" 곳이라기보다 **찍는 일의 일부**다. 진짜로 도장 값에 따라 **다른 일을 하는** 곳은 ② 하나다 (2.5).

**test.exe 시동 때 이 줄을 지나나** `[정적]`

- 부모의 `NtResumeThread` (call `+0x96` / ret `+0x9b`, run13·run14) 가 `PsMultiResumeThread` 를 부른다.
- `+0x45` 까지 오려면:
  - `KeResumeThread` 가 돌려준 **이전 멈춤 횟수가 0 이 아니고** (`+0x32 JZ`)
  - **요청 횟수(1) 이하**여야 한다 (`+0x36 JA`).
- 새 첫 스레드는 `KeSuspendThread` 로 멈춤 횟수가 1 이다. 이전 횟수 1 → **통과한다.**
- 그러니 **코드상 지나는 줄**이다. 실제로 지났는지, 그때 bit 15 가 꺼져 있었는지는 동적 (Q19).

---

### 2.4 새 말 둘 — 객체 형식과 닫기 콜백 (`PspProcessClose` 는 어떻게 불리나)

보는 곳 ② `PspProcessClose` 는 **아무도 직접 `CALL` 하지 않는다** (`.c` 머리 `callers:` 가 비어 있다) `[정적]`. 그럼 어떻게 불리나? 이걸 읽으려면 새 말 둘이 필요하다.

**새 말**

- **객체 형식(object type)** = 커널이 다루는 물건(프로세스, 스레드, 파일 …)의 **종류 하나하나를 적은 기록**. 종류마다 "열 때·닫을 때·지울 때 부를 함수" 를 같이 적어 둔다 `[정적]` (아래 등록 줄로 확인).
  - 비유: 관리사무소의 **물건 종류별 규정 카드**. "가게 손잡이" 카드엔 "반납받을 땐 이 창구로" 가 적혀 있다.
  - 프로세스 종류의 카드는 `PsProcessType` 이다 (등록 때 `R9 = &PsProcessType`).
- **닫기 콜백(`CloseProcedure`)** = 그 카드에 적힌 "**손잡이가 하나 닫힐 때 불러 줄 함수**". 미리 이름을 적어 두면, 손잡이를 닫는 쪽(객체 관리 코드)이 **간접 호출**로 불러 준다.
  - 비유: 카드에 적힌 **손잡이 반납 창구**. 손님이 아무 창구에나 손잡이를 내도, 직원이 카드를 보고 그 창구로 넘긴다.
  - run33 문서 비유로는 "가게 열쇠(프로세스 핸들)를 반납받는 창구" 다.

**등록 — 부팅 때 한 번** `[정적]`

- `PspInitPhase0` 가 프로세스 객체 형식을 만들면서 카드(`OBJECT_TYPE_INITIALIZER`)를 스택에 채운다.
  - `+0x47e` `PspProcessDelete` (지울 때)
  - `+0x490` `PspProcessOpen` (열 때)
  - **`+0x4a8 LEA RAX,[PspProcessClose]` → `+0x4bb` 저장** (닫을 때)
- 세 칸의 간격이 PDB 의 `_OBJECT_TYPE_INITIALIZER` 칸 `OpenProcedure +0x38` / `CloseProcedure +0x40` / `DeleteProcedure +0x48` 과 같다. 그래서 `PspProcessClose` 가 **닫기 칸**에 들어간다고 읽었다.
- 그리고 `ObCreateObjectType` (call `+0x4e6` / ret `+0x4eb`) 로 카드를 등록한다.

**부름 — 손잡이를 닫을 때마다** `[정적]` (대상은 `[추측]`)

1. `NtClose` call `+0xe4` / ret `+0xe9` → `ObCloseHandleTableEntry`
2. 그 안에서 객체 형식의 `+0x80` (= `TypeInfo.CloseProcedure`) 을 읽는다 (`+0x2b0` `CMP`, `+0x30c` `MOV RAX,[R14+0x80]`).
3. 알려진 몇 개(`CmpCloseKeyObject`, `IopCloseFile` 등)와 비교해 맞으면 직접 부르고,
4. **아니면 CFG 간접 호출 `+0x7a9` (ret `+0x7ae`).**

- 간접 호출이라 "그 대상이 `PspProcessClose` 다" 는 코드만으론 못 정한다 → `[추측]`. 교환원(CFG)을 거친 전화라 통화 기록에 상대 이름이 안 남는 것과 같다.
- 그래서 결론은: **어느 프로세스든 그 손잡이가 하나 닫힐 때마다** 이 함수가 돈다 `[정적 + 간접 호출 대상은 추측]`.

---

### 2.5 보는 곳 ② — 반납 창구 안 (`PspProcessClose`)

함수 전체가 `+0xf3` 까지로 짧다. 그중 **bit 15 앞뒤만** 따라가 보자. 모두 `[정적]`, 인자 이름만 `[추측]`.

**먼저 인자** `[추측]`

- `RCX` = 손잡이를 **닫는** 프로세스
- `RDX` = 손잡이가 가리키던, **닫히는** 프로세스
- `R9` = 닫는 쪽이 이 객체에 가진 손잡이 수
- 공개 문서의 닫기 콜백 모양에 맞춘 이름이다. 확인은 Q65.

**갈림길 1 — `+0xf` 까지 오나**

- `+0x9 CMP R9,1` / `+0xd JBE +0x4e` → 손잡이가 **하나뿐**이면 `+0x4e` 로 간다.
- `+0x4e` 에서 닫히는 프로세스의 `ActiveThreads` (`+0x380`, 살아 있는 스레드 수) 를 본다. 0 이 아니면 `+0x56 JNZ +0xf`.
- 곧 **`+0xf` 로 오는 조건 = 손잡이가 둘 이상이거나, 스레드가 하나라도 살아 있을 때.**
- 손잡이 하나 + 스레드 0 이면 `+0x58` 아래 "**마지막 손잡이**" 길로 간다:
  - `PspLockProcessExclusive` (ret `+0x7b`) → `Flags` bit 3 을 cmpxchg 로 켜기 → `PspUnlockProcessExclusive` (ret `+0xae`) → `PspRundownProcess` (ret `+0xd4`)
  - 이 길엔 **bit 15 검사가 없다.**

**갈림길 2 — `+0xf` 의 도장 검사**

- 도장 **있음** (bit 15 켜짐) → `+0x1b` 로 return. **아무것도 안 한다.**
- 도장 **없음** (bit 15 꺼짐) → `+0x22 ~ +0x3d` 에서 두 가지를 본다:
  1. 닫히는 프로세스의 **`OwnerProcessId`** (`+0x2d8`) 가 닫는 프로세스의 `UniqueProcessId` (`+0x1d0`) 와 같은가
     - `OwnerProcessId` = 이름 그대로 "이 프로세스의 **주인** 프로세스 번호" 칸. 장부의 "주인 이름" 란.
  2. 닫는 프로세스의 `Flags` (`+0x1f4`) 에 **bit 3 `ProcessDelete` 또는 bit 30 `ProcessSelfDelete`** (`0x40000008`) 가 있나 — 곧 "닫는 쪽이 **지워지는 중**인가"
- 둘 다 맞으면 → **`PsTerminateProcess(닫히는 프로세스, 0xc000010a)`** (call `+0x47` / ret `+0x4c`). 아니면 return.

비유로 한 번 더 (태그 없음):

- 손님이 가게 손잡이를 반납 창구에 낸다.
- 창구 직원이 장부를 펼쳐 "첫 직원 출근 허가 도장" 을 본다.
  - 도장이 **있으면**: "네, 반납 받았습니다." 끝.
  - 도장이 **없으면**: "반납하는 분이 이 가게 **주인**이세요? 그리고 지금 **문 닫고 떠나는 중**이세요?" 둘 다 그렇다면 **한 번도 문 안 연 이 가게도 같이 닫는다.**

---

### 2.6 이 검사는 무엇을 지키나 — 짐작의 고리 `[추측]`

run 문서는 뜻을 `[추측]` 으로 적었다. 왜 그렇게 짐작하는지 하나씩.

**근거** (모두 `[정적]`)

1. bit 15 는 `PsMultiResumeThread` 가 "**이번에 완전히 풀림**" 일 때만 켠다 (2.3). 끄는 곳은 없다 (2.8).
   - → 켜져 있다 = "이 프로세스의 스레드가 **한 번이라도** 풀린 적 있다".
2. 꺼져 있을 때만 `PsTerminateProcess` 쪽으로 간다.
3. 그 조건은 "닫는 쪽 = 주인" 이고 "닫는 쪽이 지워지는 중" 이다.

**그래서** `[추측]`

- "**한 번도 안 풀린 자식 프로세스를, 그 주인이 죽으면서 손잡이를 닫을 때 같이 끝낸다**" 는 지름길로 보인다.
- 멈춘 채 만들어 놓고 풀어 주기 전에 주인이 죽으면, 그 자식은 아무도 깨워 줄 사람이 없다. 그런 가게를 치워 주는 장치라고 읽힌다.
- bit 15 가 켜졌다 = "이미 한 번 풀렸음" → 그 지름길이 **막힌다.** 한 번이라도 일을 시작한 가게는 주인이 떠나도 같이 닫지 않는다.

**다만 일반 생성에선 이 비교가 맞기 어려워 보인다** `[정적 읽기 + 추측]` (→ run34 정정)

- `OwnerProcessId` 를 쓰는 줄은 `PspAllocateProcess +0x6f1 ~ +0x710` 이다 (run10 의 가게 짓기 안).
  - 값 = **부모 PID 에 `|2`** 를 붙인 것 (`+0x703 OR RAX,2`).
  - 붙이지 않는 건 `PspAllocateProcess` 인자 `[RSP+0x90]` 의 **bit 9 가 켜졌을 때뿐**이다 (`+0x707 BT R8D,9`, `+0x70c CMOVC`).
- PID 는 4 의 배수다. `|2` 가 붙으면 닫는 쪽 PID 와 **같을 수가 없다.**
- → test.exe 같은 보통 생성이라면 갈림길 2 의 첫 조건이 안 맞아, 도장이 없어도 그냥 return 할 것으로 보인다.

**아직 확인 아님**

- 그 인자 bit 9 가 **무슨 플래그인지** 안 봤다. `OwnerProcessId` 를 쓰는 다른 줄(`NtSetInformationProcess +0x1ecd`)도 어느 정보 종류인지 안 봤다 → Q65 (정적).
- 인자 이름(`R9` = 손잡이 수 등)도 `[추측]` → Q65.
- 비유 (태그 없음): 장부 "주인" 란에 부모 번호를 **살짝 바꿔서**(`|2`) 적어 두는 모양이다. 그러면 창구의 "주인이세요?" 질문에 진짜 부모도 "예" 가 안 나온다. 어떤 경우에 안 바꾸고 적는지(bit 9)는 아직 모른다.

---

### 2.7 test.exe 시동 중엔 어떻게 되나 — 정적으론 모른다

- `PsMultiResumeThread +0x45` (①) 는 **코드상 지나는 줄**이다 `[정적]` (2.3). 실제 값은 Q19.
- `PspProcessClose` (②) 는 **정적으로는 불리는지 모른다.**
  - "**누가 test.exe 프로세스 손잡이를 언제 닫나**" 에 달렸다.
  - 부모 cmd.exe 가 `CreateProcess` 끝에 `CloseHandle(hProcess)` 하는 게 `main` 전인지 후인지 코드로 못 정한다.

**만약 `main` 전에 닫는다면** `[정적 읽기 + 추측: 순서]`

1. 코드 순서상 부모는 `NtResumeThread` → `CreateProcess` 반환 → 손잡이 닫기 순이다.
2. 그러면 닫을 때 이미 `+0x4e` 를 지났으니 **도장은 찍혀 있어야** 한다.
3. 첫 스레드는 살아 있으니 (`ActiveThreads` ≠ 0) `+0xf` 로 온다.
4. 도장이 있으니 `+0x1b` 로 바로 return → **아무 일도 안 한다.**

- 그래서 flow-writer 는 **`PspProcessClose` 를 그림에 안 넣었다** (3절).
  - 누가(어느 스레드가) 언제 부르는지 모르고, 간접 호출이라 caller 도 정적으로 못 정한다.
  - `main` 전에 서도 도장이 있으면 바로 돌아가는 자리라 "시동에서 무엇이 일어나나" 의 대표 지점이 아니다.
- 실제로 서는지는 동적 **Q64** — 서면 누가 닫았는지(`k`)와 그때 도장 값을 본다. **안 서면 그것 자체가 답**이다 (`main` 까지 test.exe 프로세스 손잡이를 닫는 곳이 없다).

---

### 2.8 찍는 곳·지우는 곳·밖으로 나가는 길

"보는 곳은 둘" 만큼 중요한 게 "**찍고 지우는 곳**" 이다. 도장 값이 언제 바뀌는지 알아야 보는 곳의 뜻이 선다.

**찍는 곳 하나, 지우는 곳 없음** `[정적]`

- bit 15 를 켜는 곳은 **`PsMultiResumeThread +0x4e` 하나뿐**이다. 끄는 줄은 **없다.**
- `_EPROCESS +0x5fc` 에 **쓰는** 명령 40곳을 다 봤다.
  - 그중 마스크가 상수가 아닌 4곳도 값을 따라가 bit 15 가 아님을 확인했다 (결과서): `PspAllocateProcess +0x640` (bit 5), `NtSetInformationProcess +0x203` (bit 19·20·28·29·31), `PspSetProcessForegroundBackgroundRequest +0xd7`·`+0x125` (bit 16·17), `ExUpdateTimerResolutionPolicy +0x62` (bit 26).
- 새 가게를 지을 때 `PspAllocateProcess` 가 `Flags3` 에 쓰는 비트는 0·5·7·12·23·27·30. **bit 15 없음.**
  - 처음 값은 0 일 것이다 `[추측: 객체가 0 으로 채워져 나옴]`.
- 부팅 코드 한 곳(`FUN_140bef6d0 +0x3b57`, INIT 구역)이 4바이트를 **통째로** 쓴다. 처음 만드는 프로세스용으로 보이고 test.exe 길과 상관없다 `[추측]`.

**밖으로 안 나간다** `[정적]`

- `NtQueryInformationProcess` (관리실이 장부를 물어보는 창구) 가 `Flags3` 를 읽는 곳 셋 — `+0x251a`, `+0x2ecd`, `+0x3ff3` — 은 bit 19·20·28·29 / 2 / 9 만 본다. **bit 15 없음.**
- ETW 쪽 (`EtwpPsProvProcessEnumCallback` 등 5개) 과 `ExpGetProcessInformation` 도 bit 15 를 안 읽는다.
- `EtwTiLogSuspendResumeThread` (`PsMultiResumeThread +0x6f`) 는 `Flags3` 칸을 **아예** 안 읽는다.
- → 도장은 관리사무소 안 두 곳에서만 쓰이고, **관리실(ntdll)이나 바깥 기록(ETW)으로 안 나간다** (2.9 의 범위 안에서).

**헷갈리지 않게 — bit 15 와 상관없는 `Flags3` 읽기들** `[정적]`

- 칸을 **통째로** 복사·비교하는 곳은 있다 (`PsSetVmProcessorHostProcess` 등). 비트별 판단이 아니라 bit 15 를 보는 곳으로 안 센다.
- test.exe 첫 스레드가 도는 `PspUserThreadStartup` 도 `+0x5a` 에서 `&Flags3` 를 만들지만, 읽는 곳 `+0x66`·`+0xa8` 은 **bit 0 만** 본다.

---

### 2.9 "둘뿐" 은 어느 범위의 결론인가 — 도서관 한 동을 다 뒤졌다

"보는 곳은 **둘뿐**" 이라는 말은 세다. 어디까지 뒤져서 한 말인지 알아야 그 무게를 안다.

**비유부터** (태그 없음)

- 커널 쪽 코드 전체를 **도서관**이라고 하자. `ntoskrnl.exe` 는 그중 **본관 한 동**이다. `hal`, `win32k*`, 드라이버는 **다른 동**이다.
- 우리가 찾는 건 "`0x5fc` 쪽, bit 15" 라는 **한 줄 문구**다.
- 이번엔 본관을 **처음부터 끝까지** 뒤졌다. 다른 동은 안 갔다.

**어떻게 뒤졌나 — 네 겹** (run 문서 "찾는 법과 한계", 결과서 1-g)

1. **모든 명령 훑기** — 본관 책 전부의 모든 줄.
   - Ghidra 가 읽은 ntoskrnl 명령 **2,645,299개** 전부에서, 피연산자 값이 `0x5f8 ~ 0x5ff` 인 것을 뽑았다 (`[reg+0x5fc]` 꼴, 즉값 둘 다).
   - `+0x5fc` 는 **226개 / 149개 함수.**
   - 원래 하네스의 `refs` 도구는 "**한 주소를 가리키는 곳**" 만 찾는다. 이번처럼 "레지스터 + `0x5fc`" 로 읽는 **구조체 칸**은 못 찾아서, 임시 스크립트를 따로 썼다.
2. **원본 바이트로 다시** — Ghidra 의 "책 목록" 에 빠진 쪽이 없나, **종이 원본**에서 직접 찾기.
   - 실행 구역 18개 전부에서 `fc 05 00 00` (변위 `0x5fc` 의 바이트) 를 찾았다: **233곳.**
   - 226곳은 1번과 같고, 나머지 7곳은 명령이 아니다 (분기 변위 6곳, 표 데이터 1곳).
   - → **Ghidra 가 못 읽은 코드에 숨은 `[reg+0x5fc]` 는 없다.**
   - bit 15 가 든 바이트 `+0x5fd` 를 직접 건드리는 명령은 0개.
3. **돌려 말한 곳** — 쪽 번호를 바로 안 쓰고 "책갈피(포인터)를 먼저 꽂아 두고" 읽는 꼴.
   - `&Flags3` 를 `LEA`/`ADD 상수` 로 만든 곳 6곳을 따라가 읽는 줄을 봤다.
   - 더 일반적인 추적은 그 6곳 말고 **0건.** (추적이 제대로 도는지는 `+0x5f8` 로 시험해 18건 나오는 걸로 확인.)
4. **읽어 간 값 따라가기** — 문구를 베껴 간 뒤 어떻게 쓰나.
   - `[reg+0x5fc]` 를 레지스터로 읽는 `MOV` 103개마다, 뒤따르는 직선 코드(최대 120명령)에서 어떤 마스크·비트로 쓰는지 봤다.
   - bit 15 를 보는 것은 **0건.**

**못 보는 꼴 — 한계**

- **범위는 이 `ntoskrnl.exe` 하나.** `hal`, `win32k*`, 드라이버, 실행 중에 고쳐지는 코드(핫패치)는 안 봤다. → 다른 동은 안 갔다.
- 책갈피를 **다른 함수에 넘겨** 거기서 읽는 꼴은 못 따라간다.
  - 예: `PsSetVmProcessorHostProcess +0x12a` → `ExBlockOnAddressPushLock`. 4바이트 통째 비교라 bit 15 판단은 아닐 것 `[추측]`.
  - 비유: 책을 빌려 **다른 사람에게 넘긴 뒤** 그 사람이 뭘 읽었는지는 모른다.
- 책갈피를 **메모리(스택·구조체)에 저장했다 다시 꺼내** 쓰는 꼴은 안 따라간다.
  - 예외 하나: `PspUserThreadStartup +0x61` 은 원본 바이트로 직접 봤고, bit 0 만 읽었다.
  - 비유: 쪽 번호를 **메모지에 적어 두었다가** 한참 뒤에 꺼내 읽는 꼴.
- 기준 주소를 `LEA`/`ADD 상수` 가 아닌 식(곱셈, `SUB`, 레지스터 둘 더하기)으로 만든 꼴, 분기·함수 경계를 넘는 추적, 실행 중에 오프셋을 계산하는 접근도 못 찾는다.
- 같은 `+0x5fc` 를 쓰는 **다른 구조체**는 이름·쓰임으로 걸렀다 (`_EPROCESS` 아님 `[추측]`). 그것들은 bit 15 판단이 없어 결론엔 영향이 없다.

**그래서 결론의 정확한 모양**

- "보는 곳은 둘뿐" = **ntoskrnl 한 파일 안에서, 위 네 겹으로 잡히는 꼴 안에서는** 둘뿐이다.
- 비유로: "**본관은 책장 하나 빠짐없이 뒤졌고, 거기엔 두 군데뿐.** 다른 동, 그리고 빌려 나간 책은 모른다."
- 다른 동(hal·win32k·드라이버)까지 갈지는 시동 지도 기준으로 우선순위가 낮아 **대기열에 안 넣었다** (plan.md 결정 기록). run33 의 한계로만 적어 둔다.

---

### 2.10 `FUN_1406b2db3` = `KiApcInterrupt +0x273` — 목차로 확인했다

이제 두 번째 질문. run13 에서 "`KiDeliverApc` 를 부르는 조각 `FUN_1406b2db3` 은 `KiApcInterrupt` 의 **떨어진 조각**으로 보임 `[추측]`" 이라고 했다.

**새 말**

- **`.pdata` (함수 범위 표)** = exe·dll 파일 안에 들어 있는 표. 함수마다 "**여기부터 여기까지가 한 함수**" 와 그 함수의 스택 설명서(unwind 정보)를 적어 둔다. 예외 처리나 디버거가 스택을 거슬러 읽을 때 쓴다 ([run15 해설 2.4](run15-user-thread-startup-해설.md#24-새-스레드는-불려서-가-아니라-교대의-ret-로-떨어져서-출발한다) 의 "풀어 보기", [run11 해설](run11-allocate-thread-해설.md) 의 "예외표" 와 같은 표).
  - 비유: 책의 **목차**. "3장은 40쪽부터 58쪽까지."
  - Ghidra 가 보여 주는 함수 경계는 **Ghidra 가 읽으며 짐작한 것**이다. 목차는 **책을 만든 쪽(컴파일러)이 적어 둔 것**이다.
- **PDB 공개 이름** = 그 범위에 붙은 함수 이름. 비유: 장 **제목**.

**목차를 펼쳐 보니** `[정적]`

| 항목 | 값 |
|---|---|
| `0x6b2db3` 를 감싸는 `.pdata` 범위 | `[0x6b2b40, 0x6b3259)`, **1817바이트** |
| unwind 정보 | `UNW_FLAG_CHAININFO` **없음** → 부모 함수 없음. 예외 처리기 `KiFatalExceptionHandler` |
| 이 범위로 체인되는 다른 범위 | 없음 |
| PDB 공개 이름 | 이 범위 안에 `KiApcInterrupt` (`0x6b2b40`) **하나**. 다음 이름 `KiInitiateUserApc` (`0x6b3260`) |
| `pdb_aliases.py --lookup ntoskrnl.exe 0x6b2db3` | 별칭 무리 아님. 앞 이름 `KiApcInterrupt` 에서 `+0x273` |

- `UNW_FLAG_CHAININFO` = "나는 다른 함수에서 떨어져 나온 조각이고, 부모는 저기" 라고 이어 주는 표시다 (요청서·결과서가 "부모 함수" 를 찾는 표시로 쓴 것). 이게 **없다** = 이 범위는 **혼자서 온전한 한 함수**다.
- 비유: 목차를 보니 **40~58쪽 전체가 한 장**("`KiApcInterrupt`")이고, 그 장 제목 말고는 제목이 없다. 우리가 "따로 떨어진 부록" 이라 여겼던 쪽은 사실 **그 장의 한가운데 쪽**(`+0x273`)이었다.

**그럼 Ghidra 는 왜 토막을 냈나** `[정적]`

- Ghidra 는 이 1817바이트를 **다섯 토막**으로 보여 줬다:

  ```
  KiApcInterrupt 348B + 사다리 279B + FUN_1406b2db3 753B + 사다리 279B + FUN_1406b3170 158B = 1817B
  ```

  - 다섯을 더하면 `.pdata` 범위와 **딱 맞는다.**
  - `FUN_1406b3170` = `KiApcInterrupt +0x630` (끝 갈래, `+0x6b2` 에서 `JMP KiKernelExit`) (결과서).
- **사다리** = `ADD RSP,8; CALL` 짝을 **31칸** 늘어놓은 것 (`KiApcInterrupt +0x15c ~ +0x272`, `+0x519 ~ +0x62f`). 읽을 코드가 아니다.
  - **RSB 채우기 사다리** 로 보인다 `[추측]` — CPU 가 "돌아갈 곳" 을 미리 짐작해 두는 작은 메모(RSB)를 가짜 호출로 가득 채워, 엉뚱한 짐작(추측 실행)을 막는 보안 장치 (run29 해설 "나가는 순서" 4번에도 나왔다).
  - 비유: 장 한가운데 끼워 둔 **빈 칸 채우기용 종이 31장**.
- 그래서 `KiApcInterrupt +0x157` 의 `CALL` 도 **함수 호출이 아니다** (`.asm` 이 FAKECALL 로 표시).
- flow.md 는 "Ghidra 가 사이의 사다리 때문에 토막을 낸 것" 이라고 적었다.
  - `[추측]` (이 해설의 짐작) 사다리에는 `CALL` 이 31개 있고, Ghidra 는 보통 `CALL` 이 가리키는 곳을 "새 함수 입구" 로 잡는다. 그래서 사다리 뒤에서 이름(`FUN_…`)을 새로 붙였을 수 있다. 근거 문서는 "사다리 때문" 까지만 적었다.

**run13 의 짐작은 반만 맞았다**

| | run13 | run33 |
|---|---|---|
| 이름 | `KiApcInterrupt` `[추측]` | **`KiApcInterrupt`** `[정적]` (`.pdata`, PDB 공개 이름) |
| 모양 | "떨어진 조각" `[추측]` | **떨어진 조각 아님** — 같은 범위 한가운데 `+0x273` `[정적]` |

- 비유 (태그 없음): "저 부록은 3장에서 떨어져 나온 것 같다" 고 했는데, 목차를 보니 **떨어진 게 아니라 3장 본문 한가운데**였다. 제목(3장)은 맞혔고, "떨어졌다" 는 틀렸다.

---

### 2.11 초인종 함수 안 — `KiDeliverApc` 를 부르는 자리

이름이 확정됐으니, run13·run15 에서 이어 온 **초인종 고리**에 오프셋 하나가 더 붙었다.

**`KiApcInterrupt` 안, `KiDeliverApc` 앞뒤** `[정적]`

1. `HalPerformEndOfInterrupt` (call `+0x38a` / ret `+0x38f`)
2. `STI` (`+0x38f`)
3. **`+0x390 CMP byte GS:[0x187],0`** — 이 검사에 걸리면 `KiDeliverApc` 를 **건너뛴다.**
   - `GS:[...]` 는 커널에서 CPU 마다 하나씩 있는 정보 묶음을 가리킨다 `[추측]` (일반적으로 알려진 설명). 비유: CPU 마다 붙은 **작은 메모판**의 한 칸.
   - 그 바이트가 **무슨 뜻인지는 안 봤다.** 같은 실행에서 값만 보기로 했다 (Q42).
4. **`KiDeliverApc(0, 0, 트랩 프레임 추정)`** — call **`+0x3a6`** / ret **`+0x3ab`**.

**같은 함수의 다른 눈에 띄는 호출** (참고, `[정적]`)

- `KiInitiateUserApc` call `+0x401` / ret `+0x406`
- 끝에 `KiKernelExit` 꼬리 점프 `+0x6b2`

**장부 이름과 오프셋 맞추기**

- `_calls.tsv` 는 이 줄을 **조각 기준**으로 적었다: `frag:FUN_1406b2db3+0x133` / `+0x138`.
- `.asm` 은 `(= KiApcInterrupt +0x3a6)` 처럼 **본래 함수 기준**으로도 보여 준다.
- 둘은 **같은 줄**이다 (절대 `0x1406b2ee6` / `0x1406b2eeb`). 조각 시작이 `+0x273` 이라서 `+0x133` 을 더하면 `+0x3a6` 이 된다.
- 앞으로 장부(`KiDeliverApc.c` 머리 `callers:` 등)에서 `FUN_1406b2db3` 을 보면 **`KiApcInterrupt`** 로 읽으면 된다.

**고리가 이렇게 이어진다**

```
PspUserThreadStartup +0x22   IRQL 1 → 0                        [정적] (run15)
 ═▶ KiApcInterrupt                                              [추측] 들어오는 자리 +0x26
      +0x390  GS:[0x187] 검사 (걸리면 건너뜀)                  [정적] (run33)
      └─ +0x3ab  KiDeliverApc                                   [정적] (run33)
            └─ +0x220  KiSchedulerApc                           [정적] (run13)
                  └─ +0xf7  KeWaitForSingleObject  ← 잠듦      [정적] (run13)
```

- 남은 `[추측]` 은 **굵은 화살표 하나** — 초인종이 정말 `PspUserThreadStartup +0x26` 에서 울리나 (Q42).
- WinDbg 가 콜스택에서 이 칸을 `nt!KiApcInterrupt+0x3ab` 로 보여 줄지는, PDB 공개 이름이 그 범위에 하나뿐이라 **그럴 것** `[추측]` (Q42 에서 확인).
- 비유 (태그 없음): 직원이 "방해 금지" 표지판을 0 으로 내리면 초인종이 울린다. 초인종을 받는 담당은 이제 이름표까지 확인됐다(`KiApcInterrupt`). 담당은 "벨 소리 끝" 처리를 하고(`HalPerformEndOfInterrupt`), CPU 메모판의 한 칸을 확인한 뒤(`GS:[0x187]`), 쪽지 배달 담당(`KiDeliverApc`)을 부른다. 그 배달 끝에 직원은 빨간불 앞에서 잔다.

---

### 2.12 덤 — 도구 이야기 두 줄

run 문서 "도구 메모" 에 적힌, **하네스 제안** 두 개다 (고치는 건 오케스트레이터·사용자 몫).

- "**구조체 칸을 누가 읽고 쓰나**" 를 묻는 명령이 스킬에 없다.
  - 이번엔 임시 Ghidra 스크립트로 명령 264만 개를 15초에 훑었다.
  - `scan <바이너리> <변위>` 같은 명령이 있으면 Q36(PEB)·이번(`Flags3`) 같은 질문을 바로 할 수 있다.
- **조각 이름이 장부에 그대로 남는다.**
  - `KiDeliverApc.c` 머리 `callers:` 의 `FUN_1406b2db3`, `_calls.tsv` 의 조각 기준 오프셋.
  - 같은 `.pdata` 범위면 본래 함수 이름·오프셋으로 적으면 좋다는 제안이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 2.2 `PsMultiResumeThread` 절 | `[공백]` "`ProcessFirstResume` 를 읽는 곳은 안 봤다" | **(해결됨) `[정적]`** 읽고 판단하는 곳 둘 — `PsMultiResumeThread +0x45`, `PspProcessClose +0xf`. 켜는 곳 `+0x4e` 하나, 끄는 곳 없음, 유저·ETW 로 안 나감. 범위는 ntoskrnl 하나 |
| 2.2 새 절 `nt!PspProcessClose` | 없음 | **`[정적]`** 프로세스 객체 형식의 닫기 콜백 (등록 `PspInitPhase0 +0x4a8`, 부름 `ObCloseHandleTableEntry +0x7a9` 간접, 대상 `[추측]`). bit 15 꺼짐 + 주인 PID + 주인 지워지는 중 → `PsTerminateProcess` (ret `+0x4c`). 뜻 `[추측]`, 일반 생성에서 맞기 어려움 `[정적 읽기 + 추측]` |
| 2.2 "멈춘 첫 스레드가 서는 곳" 6번 | `[추측]` "`FUN_1406b2db3` … `KiApcInterrupt` 의 떨어진 조각" | **`[정적]`** `KiApcInterrupt +0x273` (같은 `.pdata` 범위 `[0x6b2b40, 0x6b3259)`, 체인 없음, PDB 이름 하나). `KiDeliverApc` call `+0x3a6` / ret `+0x3ab` |
| 2.2 스택 모양 한 줄 | `KeWaitForSingleObject` ← `KiSchedulerApc+0xf7` ← `KiDeliverApc+0x220` ← … | … ← `KiDeliverApc+0x220` ← **`KiApcInterrupt+0x3ab`** ← … (WinDbg 표시는 `[추측]`) |
| 그림 2 `h0` 상자 | "nt!KiApcInterrupt **추정** (장부: 조각 FUN_1406b2db3)" | "**nt!KiApcInterrupt** (정적: .pdata·PDB 로 확정 …)". 상자는 그대로 `(정적)` 점선 (실행으로 본 적 없음) |
| 그림 2 `h0 → h1` 화살표 | `?` | **`+0x3ab`** |
| 그림 2 묶음 제목 | "(추측 연결)" | "(들어오는 자리는 추측)" — 남은 추측이 굵은 화살표 하나뿐이라서 |
| 1장 메모 | — | `h0` 이름 확정 설명 + "`PspProcessClose` 는 그림에 안 넣었다" 이유 (2.7) |
| 4장 공백 표 2.2 | 두 줄 (`FUN_1406b2db3` 정체, `ProcessFirstResume` 읽는 곳) | 둘 다 **(해결됨, 정적)**. 새 줄 셋: Q19 에 합친 bit 15 값 보기, **Q64**, **Q65** |
| 4.1 표 | 그림 1 (3) 풀어 주기, 그림 2 nt 줄 | bit 15 0→1 보는 자리 (`nt+2c9ae5`·`nt+2c9af6`) 추가. 그림 2 nt 줄의 "인터럽트 조각 이름 `[추측]`·`?` 한 칸" 이 해결됨, `KiApcInterrupt+0x3ab`·`ln nt+6b2db3`·`GS:[0x187]` 보기 추가 |
| 기준 | 정적 근거 run9 ~ run32 | run9 ~ run33. 그리고 두 줄 추가: 조각을 `.pdata`+PDB 로 맞춘 방법, 임시 스크립트로 전체 훑기 (범위·한계는 run33) |

### 큰 그림에서 어떻게 이어지나

- **2.2 (부모가 첫 직원을 풀어 주는 구간)** 의 정적 그림이 한 칸 더 닫혔다.
  - run13 에서 "도장을 찍는다" 까지 알았고, 이번에 "**그 도장은 시동 길에서 사실상 아무것도 안 바꾼다**" 쪽으로 기울었다.
    - 보는 곳 ① 은 "두 번 찍지 않기" 일 뿐이다.
    - 보는 곳 ② 는 손잡이 반납 때만 돌고, 시동 중이라면 도장이 이미 찍혀 있어 바로 돌아갈 것이다 `[추측: 순서]`.
  - 그래서 도장은 **대표 지점 목록(그림)에 새 상자를 만들지 않았다.** 대신 Q19 실행 때 값 하나(0 → 1)만 같이 보기로 했다.
- **그림 2 `(1) 멈춤 APC 처리`** 의 상자 이름이 다 정해졌다.
  - `KiApcInterrupt` → `KiDeliverApc` → `KiSchedulerApc` → `KeWaitForSingleObject` 네 상자가 모두 이름·오프셋 `[정적]`.
  - 이제 남은 건 "**정말 그 순간 그 자리로 들어오나**" 하나 — 동적 Q42 에서 `k` 한 번으로 볼 수 있는 질문이다.
- 다음 바퀴는 **Q41** (부모 유저 쪽 남은 정적 확인, 작음). 정적 묶음이 끝나면 동적은 **Q19** 부터다. 그때 이번 Q64 를 **같은 실행**에서 같이 본다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| bit 15 가 test.exe 때 **풀기 전 0 → 풀 때 1** 로 정말 바뀌나. `+0x45` 를 실제로 지났나 | 실행 때 값이다. 정적으론 "코드상 지나는 줄" 까지만 | **Q19** (동적): `nt+2c9ae5` (`PsMultiResumeThread +0x45`) 에서 `dd poi(@rsi+0x220)+0x5fc L1` → bit 15 꺼짐, `nt+2c9af6` (`+0x56`) 에서 켜짐 기대 |
| `PspProcessClose` 가 test.exe 프로세스로 **`main` 전에 서나**. 서면 누가 닫았고 도장 값은 무엇인가 | 누가 언제 손잡이를 닫는지는 코드로 못 정한다. 간접 호출이라 대상도 `[추측]` | **Q64** (동적, Q19 와 같은 실행): `nt+9b3b50` 입구, 조건 `@rdx == <test.exe EPROCESS>`. 서면 `k` 와 `dd @rdx+0x5fc L1` (bit 15 켜짐 → `nt+9b3b6b` return 기대). **안 서면 그것이 답** |
| `PspAllocateProcess` 인자 `[RSP+0x90]` **bit 9** 의 뜻 (`OwnerProcessId` 에 `|2` 를 안 붙이는 조건). `NtSetInformationProcess +0x1ecd` 의 다른 쓰기는 어느 정보 종류인가. `PspProcessClose` 인자 이름 | 이번 질문(읽는 곳)의 바깥이라 안 봤다. 인자 이름은 공개 문서 모양에 맞춘 `[추측]` | **Q65** (정적, 대표 지점 목표엔 덜 중요): `PspAllocateProcess +0x707`, `NtSetInformationProcess +0x1ecd`, `ObCloseHandleTableEntry +0x7a9` 앞 인자 채움 (→ run34 정정) |
| 콜스택에 정말 `nt!KiApcInterrupt+0x3ab` 로 보이나. 초인종이 `PspUserThreadStartup +0x26` 에서 들어오나. `+0x390` 의 `GS:[0x187]` 값과 뜻 | 실행으로 본 적 없다. `GS:[0x187]` 바이트의 뜻은 안 읽었다 | **Q42** (동적): `KiSchedulerApc +0xf2` 에서 `k`, `ln nt+6b2db3` → `nt!KiApcInterrupt+0x273` 기대, 멈춤점 후보 `nt+6b2ee6` (call) / `nt+6b2eeb` (ret), 같은 자리에서 `db gs:[0x187] L1` (0 이어야 `KiDeliverApc` 를 부름) |
| bit 15 를 ntoskrnl **밖**(hal·win32k·드라이버)에서 읽는 곳이 있나 | 이번 훑기 범위가 ntoskrnl 한 파일이었다 (2.9) | 대기열에 **안 넣었다** (시동 지도 기준 우선순위 낮음, plan.md 결정 기록). run33 한계로만 둠 |

---

## 5. 더 읽기

- run 문서: [`run33-first-resume.md`](run33-first-resume.md) — 특히 "찾는 법과 한계", "정적 — 질문 1", "정적 — 질문 2", "그림 반영"
- 결과서: [`_workspace/sast/28_first-resume_result.md`](../../_workspace/sast/28_first-resume_result.md) — 1-a ~ 1-g (읽는 곳·쓰는 곳·밖으로 나가는 길·찾는 법과 한계), 질문 2 표 (`.pdata`, PDB), "스크립트 문제"
- 요청서: [`_workspace/sast/28_first-resume.md`](../../_workspace/sast/28_first-resume.md)
- flow.md:
  - [2.2 부모 쪽 마무리 ~ 첫 스레드 출발](flow.md) — `nt!PsMultiResumeThread` 절의 (해결됨) 줄, 새 `nt!PspProcessClose` 절, "멈춘 첫 스레드가 서는 곳" 6번
  - 그림 2 `(1) 멈춤 APC 처리` (`h0` 상자, `h0 → h1` `+0x3ab`), 1장 메모
  - 4장 공백 표 2.2 줄, 4.1 표 "그림 1 (3) 풀어 주기"·"그림 2 nt"
- 앞 해설:
  - [run13 해설 2.6](run13-resume-thread-해설.md#26-쪽지는-언제-펼쳐지나--추측의-고리) (초인종 고리), [2.7](run13-resume-thread-해설.md#27-풀어-주는-사슬--그리고-끼지-않는-psresumethread) (도장 찍기)
  - [run14 해설 2.9](run14-create-process-internal-해설.md#29-ntresumethread-다-ntalertresumethread-가-아니다--run13-사슬과-이어진다) (부모가 `NtResumeThread` 를 씀), [2.11](run14-create-process-internal-해설.md#211-한-장의-시간표--부모--커널--자식-잠듦--부모-풀어-줌--자식-깨어남) (한 장의 시간표)
  - [run15 해설 2.9](run15-user-thread-startup-해설.md#29-run13-의-쪽지는-0x22-직후-펼쳐진다--고리-3개가-굳었다) (IRQL 1 → 0 고리)

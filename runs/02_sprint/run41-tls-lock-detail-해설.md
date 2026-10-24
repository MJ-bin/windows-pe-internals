# run41 tls-lock-detail — 해설

> 이 글은 [run41 문서](run41-tls-lock-detail.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q56** — 2.7 TLS 락 세부 (작은 정적): TEB `+0x1850` 8칸 표 이름, 추적 칸 맨 위 비트를 켜는 곳, `ImageTlsCallbackCaller` 의 `RtlAcquireSRWLockExclusive` 직접 호출 갈래, `LdrShutdownProcess +0x168` 앞 `[RDI+0x58]` 조건 (`_workspace/plan.md`, 이번에 **끝**)
  - 실행 때 확인할 거리는 새 번호 없이 **Q25** (락 소유 칸, `+0x1e8`) 와 **Q30** (`RtlUserThreadStart +0x23`) 에 출처로 붙었다.
  - 새 정적 공백 넷은 **Q74** (a)·(b)·(c)·(d) 로 맨 아래에 붙었다.
- 근거:
  - run 문서 [`run41-tls-lock-detail.md`](run41-tls-lock-detail.md) (가장 중요)
  - 요청서 [`_workspace/sast/36_tls-lock-detail.md`](../../_workspace/sast/36_tls-lock-detail.md)
  - 결과서 [`_workspace/sast/36_tls-lock-detail_result.md`](../../_workspace/sast/36_tls-lock-detail_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll 은 **새로 안 뽑았다.** 이미 있던 `.asm`·`.c` 만 다시 읽었다.
  - 대신 ntoskrnl(커널) 함수 **6개를 새로 뽑았다.** 질문 2 의 답이 ntdll 이 아니라 커널 안에 있어서다 (2.6 에서 자세히).
  - 구조체 칸 이름(`_TEB`, `_LDR_DATA_TABLE_ENTRY`, `_PEB_LDR_DATA`, 커널 `_KTHREAD`)은 **PDB 타입 레코드**로 읽었다.
  - "몇 곳에 있나" 같은 숫자는 ntdll.dll 전체를 `objdump` 로 풀어 **명령 꼴로 검색**한 것이다. 그래서 **"검색한 꼴 기준"** 이고, 모양이 조금 다른 곳은 빠질 수 있다 (하한).

이 글은 [run28 해설](run28-tls-init-call-해설.md) 의 **곁가지 정리판**이다.

- run28 은 TLS 콜백이 불리기까지의 큰 길을 봤다.
- 그때 "작은 거라 나중에" 하고 남긴 질문 넷이 이번 바퀴다. 큰 길(지도의 화살표)은 안 바뀐다. **자물쇠 둘레의 잔가지**를 정리한 바퀴다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](run9-create-user-process-해설.md) 2절 · rva, PEB → [run10 해설](run10-allocate-process-해설.md) 2절
- TEB (직원 **개인 안내판**) → [run11 해설 2.2](run11-allocate-thread-해설.md#22-한-줄씩-읽는-법)
- `GS:[0x30]` 으로 TEB 를 꺼내는 것 → [run18 해설](run18-proc-init-setup-해설.md) "`+0x13b R13 = GS:[0x30]` (TEB)"
- 독점 잠금 / 공유 잠금 (명부실 열쇠) → [run23 해설 2.3](run23-load-dependent-해설.md#23-핵심--줄간선은-여기서-긋는다)
- TLS (직원 **개인 사물함**), TLS 디렉터리, 콜백 배열 → [run28 해설](run28-tls-init-call-해설.md) 2.3 "새 말 — TLS"
- TLS 서류철 자물쇠 `LdrpTlsLock` (찾을 때만 잠근다), 경합 갈래 셋 → [run28 해설](run28-tls-init-call-해설.md) 2.5
- `ImageTlsCallbackCaller` (**TLS 전용 안내원**), 직접 호출 갈래 → [run28 해설](run28-tls-init-call-해설.md) 2.8
- CFG 중개 (**교환원**) → [run28 해설](run28-tls-init-call-해설.md) 2.9
- 직접 호출과 간접 호출 (냉장고 메모) → [run9 해설](run9-create-user-process-해설.md) 2절 "직접 호출과 간접 호출"

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- TEB = 직원 **개인 안내판**, TLS = 직원 **개인 사물함**
- `LdrpTlsList` = 관리실 **TLS 서류철**, `LdrpTlsLock` = 그 서류철의 **자물쇠** (run28)
- **이번에 더하는 비유** — run 문서·flow.md 는 "도서관 대출 카드·사서·도장" 비유를 썼다. 이 글은 앞 해설들과 맞추려고 **사서 → 관리사무소, 책 → 자물쇠** 로만 바꿔 부른다. 그림은 같다.
  - 락 소유 칸 표(8칸) = 직원마다 받는 **열쇠 대출 카드** (8칸)
  - 칸에 락 주소 적기 = 카드 빈 칸에 "**○○ 자물쇠 쥐고 있음**" 적기
  - bit 63 = 관리사무소가 찍는 **도장** ("이 칸은 우리 장부에도 적어 뒀음")
  - bit 1 = 직원이 적는 "**지금 반납 중**" 표시
  - 커널 쪽 항목 `UserAbEntries` = 관리사무소 **장부**
  - `NtSetInformationThread` 클래스 0x38 = "**장부에서도 지워 주세요**" 신청서

### 정정 둘 — 먼저 한눈에

이번 바퀴의 핵심은 **요청서(sast/36)의 짐작 둘이 틀렸다**는 것이다. 자세한 건 2.6 · 2.8.

| | 요청서의 짐작 `[추측]` | 실제 `[정적]` | 결정적인 줄 |
|---|---|---|---|
| 질문 2 | 칸의 bit 63 을 켜는 건 **ntdll** `RtlpAcquireSRWLockSharedContended` 안 | 켜는 건 **커널** `KiAbProcessPreContextSwitch +0x26e`. ntdll 엔 켜는 줄이 **0곳** | 커널 `+0x26e OR byte [RSP+0x9f],0x80` |
| 질문 4 | `LdrShutdownProcess` 의 `RDI` = **모듈 항목**, `+0x58` = `BaseDllName` | `RDI` = **TEB**, `+0x58` = `ThreadLocalStoragePointer`. 모듈 항목은 `RBX` | `+0x28 MOV RDI,GS:[0x30]` |

- 질문 2 정정은 run28 의 짐작("켜는 쪽은 `+0x113` callee 일 것")도 같이 고친다. run28 문서 맨 위에 정정 한 줄이 들어갔다.

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 다섯**:

1. TEB `+0x1850` 의 PDB 이름 **`SchedulerSharedDataSlot` 이 맞다.** 칸에는 커널이 준 "락 소유 칸 표" 의 **주소**가 들어간다. 넣는 곳은 ntdll 에 **하나뿐** `[정적]` → 2.3
2. 그 표는 SRW 락 전용이 아니라 **임계영역·리소스도 같이 쓰는** 표다. 커널 쪽 이름은 **AutoBoost** `[정적]` (`Ab` 풀이는 `[추측]`) → 2.5
3. **정정 ①**: 칸의 bit 63 은 ntdll 이 아니라 **커널**이 켠다. 스레드가 락을 쥔 채 CPU 에서 내려갈 때다 `[정적]` → 2.6
4. **정정 ②**: `LdrShutdownProcess` 의 `RDI` 는 **TEB** 다. 조건은 "TLS 있는 모듈이고, 이 스레드에 TLS 배열이 있으면" `[정적]` → 2.8
5. `ImageTlsCallbackCaller` 의 직접 호출 갈래와 같은 꼴이 ntdll 에 **최소 23곳** 더 있다. 컴파일러 꼴이라는 짐작은 `[추측]` 그대로지만 근거가 늘었다. sample 콜백은 이 갈래를 **안 탄다** → 2.9

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 **돌아올 자리(ret)** 다. 숫자는 run 문서·앞 run 들 그대로.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessW … NtCreateUserProcess … NtResumeThread         (run9 ~ run14, run34)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ LdrpInitializeProcess   (가게 전체 세팅)                                    (run17 ~ run26, run35 ~ run39)
        ├─ +0x183c  LdrpAcquireSchedulerSharedDataSlot  (열쇠 대출 카드 받기)       ← 이번
        ├─ +0x18d0  LdrpInitializeTls
        ├─ +0x1bbb  … LdrpInitializeNode +0x16b  LdrpCallTlsInitializers            (run27·run28)
        │                └─ 서류철 자물쇠 잡기·풀기 둘레 (카드 칸 적기·지우기)       ← 이번
        └─ +0x1c99  LdrpCallTlsInitializers (exe TLS)                              ← 이번 (같은 함수)
   └─ NtContinue ═▶ RtlUserThreadStart → … → test.exe 진입점 → main               (run29·run30)
                       └─ +0x13 ~ +0x30 직접 호출 꼴 (곁가지)                       ← 이번

 새 스레드:  LdrpInitializeThread +0x55  LdrpAcquireSchedulerSharedDataSlot      ← 이번 (자기 몫 카드)

 커널 (아무 때나 — 스레드가 CPU 에서 내려갈 때마다)
  KiSwapThread / KiQuantumEnd / … → KiAbProcessPreContextSwitch (도장 찍기)       ← 이번

 프로세스 끝 (지금 목표 범위 밖, 뜻만)
  LdrShutdownProcess +0x16d / +0x2d1  LdrpCallTlsInitializers(0, …)              ← 이번
```

- 지도의 **큰 길은 그대로**다. 이번 결과는 2.7 `LdrpCallTlsInitializers` 의 자물쇠 줄 둘레에 붙는 **설명**이다.
- 다만 "카드를 언제 받나" 가 2.4 (`+0x183c`) 와 3장(새 스레드)에 한 줄씩 생겼다.
- flow.md 로는 **2.7** 이 중심이다. 2.4, 2.10, 3장, 4장에도 줄이 더해졌다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면 작은 질문 넷이다.

1. TEB `+0x1850` 이 가리키는 **8칸 표는 무엇**인가? 이름은? 누가 넣나? "SRW 락 소유 추적" 짐작이 맞나?
2. 칸 값의 **맨 위 비트(bit 63)는 누가 켜나**? 후보는 `RtlpAcquireSRWLockSharedContended` 였다.
3. `ImageTlsCallbackCaller` 의 `RtlAcquireSRWLockExclusive` **직접 호출 갈래는 왜** 있나? sample 콜백은 안 타나?
4. `LdrShutdownProcess +0x168` 앞의 **`[RDI+0x58] ≠ 0`** 은 무슨 뜻인가?

### 바퀴 전에 알던 것

- `[정적]` (run28) `LdrpCallTlsInitializers` 는 자물쇠 앞뒤로 TEB `+0x1850` 이 가리키는 **8칸 표에 락 주소를 적었다 지운다.**
  - 풀 때 그 칸의 **맨 위 비트가 켜져 있을 때만** `+0x1e8` `NtSetInformationThread(-2, 0x38, {칸 번호, 0}, 0x10)` 를 부른다.
  - 그런데 **이 함수 안엔 그 비트를 켜는 줄이 없었다.**
- `[정적]` (run23) `LdrpLoadDependentModuleInternal` 에도 같은 "8칸 표에 락 주소 적기" 덩어리가 있다.
- `[추측]` (run23) 이 표는 "**SRW 락 소유 추적**" 용.
- `[추측]` (run28) PDB 이름 `SchedulerSharedDataSlot` 은 쓰임새(8칸 표)와 **안 맞아 보인다.**
- `[추측]` (run28, 요청서 질문 2) 맨 위 비트를 켜는 곳 = `+0x113` 에서 부르는 **`RtlpAcquireSRWLockSharedContended` 안**.
- `[추측]` (요청서 질문 4) `LdrShutdownProcess` 의 `RDI` = **모듈 항목**, 그러면 `+0x58` = `BaseDllName`.
- `[추측]` (run28) `ImageTlsCallbackCaller` 의 직접 호출 갈래 = 컴파일러가 만든 "자주 쓰는 대상" 갈래.
- `[소스]` sample TLS 콜백은 `ExeTls`·`Dll1Tls`·`Dll2Tls` 셋, 모듈마다 하나씩.

---

## 2. 찾은 것

### 2.1 한눈에 — 카드 받기, 칸 적기, 도장, 반납

run 문서 "호출 관계" 를 줄였다. 숫자는 run 문서 그대로. 모두 `[정적]` 이다.

```
첫 스레드 (test.exe 메인)
LdrpInitializeProcess
 ├─ +0x183c  LdrpAcquireSchedulerSharedDataSlot(TEB)            ← 카드 받기
 │            ├─ +0x89  NtSetInformationThread(-2, 0x39, 버퍼, 0x18)  ── 커널 PspSchedulerSharedDataRegionSlotAllocate
 │            └─ +0x92  TEB+0x1850 = 받은 표 주소   (조건 안 맞으면 0 으로 남음)
 └─ … LdrpCallTlsInitializers (2.7)
        ├─ +0x26 ~ +0x63  빈 칸 찾아 &LdrpTlsLock 적기 (표가 0 이면 건너뜀)   ← "자물쇠 쥐고 있음"
        ├─ +0x6d 빠른 잡기 → 실패면 +0x118 RtlpAcquireSRWLockSharedContended
        │                         └─ +0x12b RtlAbPreAcquire(락, 칸)  칸 ≠ 0 이면 그대로 돌려줌
        └─ 풀기: +0x1a7 bit 1 켬 → +0x1ab bit 63?                          ← 반납
                   ├─ 켜짐 → +0x1ed NtSetInformationThread(-2, 0x38, {칸 번호, 0}, 0x10)
                   │           ── 커널 KeAbUserModeEntryFree (bit 63 끔)
                   └─ +0x1ed 칸 = 0

커널 (스레드가 CPU 에서 내려갈 때마다)
 └─ KiAbProcessPreContextSwitch
      └─ 칸마다: 0 아님 · bit 1 꺼짐 · bit 63 꺼짐
           → 장부에 락 주소 복사 (+0x252) → +0x26e bit 63 켬 → +0x280 칸에 씀   ← 도장
```

- 비유로: 직원은 출근 준비 때 **카드**를 받는다. 자물쇠를 쥘 때 카드에 적고, 놓을 때 지운다.
- 관리사무소는 직원이 **자리를 비울 때마다** 카드를 훑고, 자물쇠를 쥔 채 비운 칸에 **도장**을 찍는다.
- 직원이 반납할 때 도장이 있으면 "장부에서도 지워 주세요" 신청서를 낸다. 없으면 그냥 지운다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 카드를 TEB 에 붙이는 줄

```
 │            └─ +0x92  TEB+0x1850 = 받은 표 주소   (조건 안 맞으면 0 으로 남음)
```

- **누가**: `ntdll!LdrpAcquireSchedulerSharedDataSlot` (rva `0xf0ea4`)
- **어디서**: `+0x92` — `MOV [RDI+0x1850],RAX`. 여기서 `RDI` 는 인자로 받은 TEB 다.
- **누구를**: 호출이 아니라 **쓰기** 한 줄이다. 바로 앞 `+0x84` (ret `+0x89`) 에서 커널에 받은 표 주소(`RAX`)를 TEB `+0x1850` 칸에 적는다.
- **무엇을 위해**: 이 스레드가 쓸 **8칸 표의 위치**를 개인 안내판에 붙여 둔다. 그래야 뒤에 락 함수들이 TEB 만 보고 표를 찾는다.
- 비유: 관리사무소에서 받은 **열쇠 대출 카드**를 직원 개인 안내판에 꽂아 두는 줄.
- `[정적]` ntdll 전체에서 이 칸에 **쓰는** 줄은 이것 **하나뿐**이다. 나머지 91곳은 읽기다 (`objdump` 꼴 검색 기준).

#### 한 줄 읽기 ② — 반납할 때 관리사무소에 알리는 줄

```
                   ├─ 켜짐 → +0x1ed NtSetInformationThread(-2, 0x38, {칸 번호, 0}, 0x10)
```

- **누가**: `ntdll!LdrpCallTlsInitializers` (rva `0x10e50`)
- **어디서**: call `+0x1e8`, ret `+0x1ed`. 이 줄로 오는 조건은 `+0x1ab` 에서 본 칸의 **bit 63 이 켜져 있을 때**뿐이다.
- **누구를**: 시스템 콜 `NtSetInformationThread`. 인자는 (`-2`, 클래스 `0x38`, `{칸 번호, 0}`, 크기 `0x10`).
  - **새 말 — 정보 클래스(`THREADINFOCLASS`)**: `NtSetInformationThread` 는 "스레드 정보 바꾸기" 창구 하나로 여러 일을 받는다. 둘째 인자 번호가 **신청서 종류**다. 비유: 관리사무소 창구는 하나인데, 내는 **서류 번호**에 따라 하는 일이 다르다.
  - 커널 쪽에선 `NtSetInformationThread +0x630` (ret `+0x635`) 이 `KeAbUserModeEntryFree(스레드, 칸 번호)` 로 넘긴다. 입력 크기 0x10, 둘째 dword 0, 핸들이 **현재 스레드**일 때만이다 `[정적]`.
- **무엇을 위해**: 관리사무소 장부에 적힌 이 칸의 항목을 풀고 bit 63 을 꺼 달라는 신청이다.
- 비유: 반납하려는데 카드에 **도장**이 있다 → "장부에서도 지워 주세요" 서류를 낸다.
- 칸 번호는 `(칸 주소 − [TEB+0x1850]) >> 3` 으로 계산한다 (`+0x1c2 SUB`, `+0x1c9 SAR`). 칸 하나가 8바이트라 "몇 번째 칸" 이 된다.

### 2.3 핵심 ① TEB `+0x1850` = `SchedulerSharedDataSlot` — PDB 이름이 맞았다

#### 이름

- `[정적: PDB 타입]` `_TEB` 의 `+0x1850` 칸 이름은 **`SchedulerSharedDataSlot`**, 타입 `void*` (주소 하나).
  - 같은 오프셋에 겹친(union) 다른 이름은 **없다**. (union = 한 칸에 이름을 둘 이상 붙여 같이 쓰는 것. 비유: 한 칸짜리 서랍에 붙은 이름표가 둘.) 그러니 다른 이름 후보도 없다.
  - 이웃 칸: `+0x1848 ExtendedFeatureDisableMask`, `+0x1858 HeapWalkContext`.

#### run28 은 왜 "안 맞는다" 고 했나 → 왜 사실은 맞나

1. run28 이 본 것: 칸 쓰임새는 "8칸 표에 락 주소 적기". 이름은 "스케줄러 공유 데이터 칸". **락**과 **스케줄러**가 따로 놀아 보였다.
2. 이번에 본 것 `[정적]`:
   - TEB 칸에 들어가는 건 표 **자체**가 아니라 표의 **주소**다 (`+0x92`).
   - 그 표는 **커널이 준다**. `+0x84` 에서 클래스 `0x39` 로 달라고 하고, 커널 쪽 처리 함수 이름이 `PspSchedulerSharedDataRegionSlotAllocate` 다 (`NtSetInformationThread +0xc2c`, ret `+0xc31`).
   - 그리고 이 표는 커널 스케줄러(AutoBoost, 2.5)가 **같이 읽고 쓴다** (2.6).
3. 그래서: "스케줄러와 **나눠 쓰는**(shared) 데이터 칸(slot)" 이라는 이름이 쓰임새와 **어긋나지 않는다.** run28 의 `[추측]` 은 틀렸고, PDB 이름 그대로가 맞다 `[정적]`.

#### 누가 언제 넣나

- `[정적]` 넣는 함수는 `LdrpAcquireSchedulerSharedDataSlot` 하나. 부르는 자리는:
  - 첫 스레드: `LdrpInitializeProcess +0x1837` (ret `+0x183c`). 바로 앞이 `+0x1827 LdrpAllocateSchedulerSharedData` (ret `+0x182c`, 본문은 이번에 안 봄).
  - 새 스레드: `LdrpInitializeThread +0x50` (ret `+0x55`). **스레드마다 자기 몫**을 따로 받는다.
  - `.c` 머리 caller 목록엔 `RtlCloneUserProcess`, `RtlCompleteProcessCloning` 도 있다 (오프셋은 안 봄).
- `[정적]` 오프셋 순서로 보면 test.exe 메인 스레드는 **TLS 준비(`+0x18d0`) · 모듈 초기화(`+0x1bbb`) · TLS 콜백보다 앞**에 카드를 받는다.
  - 비유: 직원이 처음 자물쇠를 만지기 **전에** 카드부터 받아 둔다.

#### 카드를 못 받는 때 — 조건 둘

- `[정적]` `+0x84` 의 신청은 두 조건이 맞을 때만 한다:
  - 전역 목록 `LdrpSchedulerSharedDataListHead` 가 **비어 있지 않음**
  - PEB `NtGlobalFlag2` (`+0x7c4`) 의 `0x10` 비트가 **꺼져 있음**
- 안 맞으면 TEB `+0x1850` 은 **0 으로 남는다.**
- 그래도 괜찮다. `LdrpCallTlsInitializers` 는 표가 0 일 때 칸 적기를 **건너뛴다** (`+0x36 TEST R8,R8` / `+0x39 JZ`) `[정적]`.
  - 비유: 카드가 없는 직원은 그냥 적지 않고 자물쇠만 쓴다.
- 두 조건이 **무슨 뜻**인지, 목록을 누가 채우는지는 이번에 안 봤다 → 4절 (Q74 (c)).

### 2.4 카드 칸 하나의 모양 — 8바이트에 주소 + 표시 셋

- 표 = **8칸 × 8바이트**. 첫 칸 주소 = `[TEB+0x1850]`. 빈 칸 = 0.
- 한 칸 안에 **락 주소**와 **작은 표시(비트) 셋**이 같이 들어 있다.

| 비트 | 뜻 | 누가 켜나 / 끄나 | 비유 | 표시 |
|---|---|---|---|---|
| bit 2 ~ 62 | **락 주소** | ntdll 이 적음 (`LdrpCallTlsInitializers +0x63`, `RtlAbPreAcquire +0x3a`). 풀고 나서 칸 전체 0 (`+0x1ed`) | "○○ 자물쇠 쥐고 있음" | [정적] |
| bit 63 | 커널이 이 칸의 자기 항목을 만들어 둠. 켜져 있으면 풀 때 커널에 알림 | **커널이 켬** `KiAbProcessPreContextSwitch +0x26e`. **끄기도 커널** (`KeAbUserModeEntryFree`, `KiAbUmPostWait +0xd2`, `KiAbProcessPostContextSwitch +0x329`). ntdll 은 **읽기만** (`+0x1ab`) | 관리사무소 **도장** | 위치는 [정적], 뜻은 [추측] |
| bit 1 | "푸는 중". 커널이 이 칸을 건너뜀 | **ntdll 이 켬** `LdrpCallTlsInitializers +0x1a7` | "지금 반납 중" | [정적] |
| bit 0 | 모름. 커널이 읽어 항목 `+0x19` 로 옮기고 (`PreContextSwitch +0x25f`), 대기가 끝나면 지움 (`KiAbUmPostWait +0x88`) | 켜는 ntdll 줄은 못 찾음 | — | [공백] |

#### 주소와 표시를 어떻게 갈라 읽나 — 마스크

- **새 말 — 마스크**: 값에서 필요한 비트만 남기고 나머지를 지우는 **거름망** 숫자. 비유: 카드 칸에서 도장·표시를 가리고 **자물쇠 이름만** 보이게 대는 종이틀.
- `[정적]` 거름망은 `0x7ffffffffffffffc` 다. 맨 위 1비트(bit 63)와 아래 2비트(bit 0·1)를 지운다.
- `[정적]` 이 **같은 숫자**를 커널과 ntdll 이 둘 다 쓴다:
  - 커널 `KiAbUmPostWait +0x79 AND` / `+0x7e CMP RAX,R15` (칸 값 & 마스크 == 락 주소 인 칸 찾기)
  - ntdll `LdrpCallTlsInitializers +0x204 MOV R9,0x7ffffffffffffffc`
- 그래서: **ntdll 과 커널이 같은 칸 모양을 약속하고 같이 쓴다**는 게 코드로 보인다.

#### 헷갈리기 쉬운 것 하나

- ntdll 에 `lock bts [reg],0` 줄이 여럿 있다 (예: `RtlpAddDebugInfoToCriticalSection +0xf2`).
- 이건 **락 값 자체**의 bit 0 이지, 이 카드 칸의 bit 0 이 아니다 `[정적]`. 그래서 bit 0 은 여전히 `[공백]` 이다.

### 2.5 핵심 ② SRW 전용이 아니다 — 커널 쪽 이름은 AutoBoost

#### run23 의 "SRW 락 소유 추적" — 방향은 맞고, 범위가 좁았다

- `[정적]` 칸에 **락 주소**가 들어간다는 건 맞다 (2.4). 그러니 "락 소유를 적는 표" 라는 방향은 맞다.
- `[정적]` 그런데 TEB `+0x1850` 을 **읽는** ntdll 함수 56개를 보면:
  - SRW 락 함수: `RtlAcquireSRWLockShared/Exclusive`, `RtlReleaseSRWLock*`, `RtlSleepConditionVariableSRW` 등
  - **임계영역** 함수: `RtlEnterCriticalSection`, `RtlLeaveCriticalSection`, `RtlTryEnterCriticalSection`, `RtlpEnterCriticalSectionContended`
  - `RtlInitializeResource`
- 그래서 SRW 락 **전용이 아니다.** 여러 종류 자물쇠가 같은 카드를 쓴다.
  - (임계영역·리소스는 SRW 와 다른 종류의 락이다. 여기서는 "다른 모양의 자물쇠" 정도로만 보면 된다.)
- run23 짐작은 "틀렸다" 가 아니라 "**좁았다**". run23 문서는 안 고치고 flow.md 4장 run23 줄에만 반영했다.

#### 커널 쪽에서 본 같은 표

- `[정적: PDB 이름]` 커널 `_KTHREAD`(커널 쪽 스레드 정보) 에 짝이 되는 칸들이 있다:
  - `+0x320 SchedulerSharedSystemSlot` — 같은 표를 **커널 주소로** 본 것
  - `+0x478 UserAbEntries` — 칸마다 0x58바이트 커널 항목 (= 관리사무소 **장부**)
  - `+0x470 KernelAbEntries`
- 함수 이름 `KiAb*`/`KeAb*`, 변수 `AutoBoostActive`·`AutoBoostLock`, 기능 이름 `Feature_UserModeAutoBoost`.
- **새 말 — AutoBoost**: `Ab` = AutoBoost 로 읽힌다 `[추측: 이름 풀이. PDB 이름들이 AutoBoost 를 같이 쓴다]`. "락을 쥔 스레드의 우선순위를 올려, 기다리는 쪽을 빨리 풀어 주는 기능" 이라는 일반 설명이 있다 `[추측: 일반 지식]`.
  - 비유: 열쇠를 들고 자리를 비운 직원이 있으면, 관리사무소가 "**그 사람 먼저 일 끝내게 해 주자**" 하고 순서를 당겨 주는 것.
- 그래서 이 표의 이름을 이제 **"락 소유 칸 표 (AutoBoost 용, 스레드마다 8칸)"** 로 부른다 (run 문서·flow.md).

### 2.6 정정 ① bit 63 은 ntdll 이 아니라 커널이 켠다

이번 바퀴에서 제일 큰 정정이다. 짐작 → 근거 → 결론 순서로 천천히 간다.

#### 짐작은 이랬다 `[추측]` (run28, 요청서 질문 2)

- `LdrpCallTlsInitializers` 는 bit 63 을 **읽기만** 하고 켜지 않는다 (run28 `[정적]`, 이건 맞다).
- 그러면 켜는 건 이 함수가 부르는 누군가일 것. 자물쇠 쪽 callee 는 `+0x113` 의 `RtlpAcquireSRWLockSharedContended` (자물쇠가 막혔을 때 기다리는 함수) 다.
- "기다려야 했을 때 그 함수가 표시를 켜겠지" — 이게 짐작이었다.

#### 근거 1 — 후보 함수는 표를 아예 안 만진다 `[정적]`

- `RtlpAcquireSRWLockSharedContended` 안에 TEB `+0x1850` 접근이 **0줄**이다.
- 칸 값을 바꾸는 줄도 없다. 칸 포인터는 둘째 인자(`RDX` → `R14`)로 **받기만** 한다.
- 이 함수가 부르는 다른 것: `RtlpWaitCouldDeadlock`(`+0x57`), `NtTerminateProcess`(`+0x239`), `RtlpOptimizeSRWLockList`(`+0x246`), `RtlBackoff`(`+0x168`), `NtWaitForAlertByThreadId`(`+0x116`). 여기에도 표를 만지는 줄은 없다 (결과서).

#### 근거 2 — 하나 남은 `RtlAbPreAcquire` 호출도 칸을 안 바꾼다 `[정적]`

- 이 함수는 칸이 0 이 **아닐** 때만 (`+0x11b TEST R14,R14`) `+0x126 RtlAbPreAcquire(락, 칸)` 을 부른다 (ret `+0x12b`).
- 그런데 `RtlAbPreAcquire` 는 둘째 인자(칸)가 0 이 아니면 **그걸 그대로 돌려주고 끝난다** (`+0x3 TEST RDX,RDX` / `+0x6 JNZ` → `+0x43 MOV RAX,RDX`).
  - 빈 칸을 찾아 락 주소를 적는 건(`+0x3a`) 칸이 **0 으로 들어왔을 때**의 길이다.
- 그래서 이 호출로는 칸 값이 **안 바뀐다.**
- 왜 굳이 "효과 없는 호출" 을 하게 짰는지는 모른다 → 4절 (Q74 (b)).

#### 근거 3 — ntdll 어디에도 켜는 줄이 없다 `[정적]`

- 짝꿍 `RtlpAcquireSRWLockExclusiveContended` (독점 잠금 쪽) 도 TEB `+0x1850` 접근·`RtlAbPreAcquire` 호출이 없다 (결과서가 호출·`bts` 만 훑음).
- ntdll **전체**에서 칸 맨 위 바이트를 켜는 꼴(`bts …,0x3f`, `or byte [..+7],0x80`)을 찾으니 **0곳**이다 (검색한 꼴 기준).

#### 그래서 — 커널을 찾아봤다

- ntdll 이 안 켠다면 남은 쪽은 커널이다. 커널은 `_KTHREAD +0x320` 으로 **같은 표를 볼 수 있다** (2.5).
- 이 길은 호출 지도(`_map`)에 없다. 아래에서 보듯 이 커널 함수를 부르는 건 `KiSwapThread` 같은 **커널 스케줄러 함수들**이지, ntdll 쪽에서 오는 호출이 아니다 `[정적]`. 비유: 직원이 관리사무소에 신청하는 게 아니라, 관리사무소가 **제 순찰 중에** 카드를 들여다본다.
- 그래서 찾은 방법은 이름과 칸이다: PDB 이름 목록에서 `KiAb*`·`KeAb*` 를 뽑고, "`_KTHREAD +0x320` 을 읽는 곳" 을 검색했다. 그렇게 커널 함수 6개를 새로 뽑았다.

#### 결론 — 켜는 곳은 `nt!KiAbProcessPreContextSwitch +0x26e` `[정적]`

- `+0x26e OR byte [RSP+0x9f],0x80` — 스택에 복사해 둔 칸 값(`[RSP+0x98]`)의 바이트 `+7` 의 bit 7, 곧 **bit 63** 을 켠다.
- `+0x280 MOV [R14+RCX*8],RAX` — 그 값을 칸에 **도로 쓴다** (`R14` = `[스레드+0x320]` 표, `RCX` = 칸 번호).
- 켜는 조건 (칸마다, 넷 다 맞아야):
  1. 칸 값이 0 이 아님 (`+0x7e TEST RDI,RDI` / `+0x81 JNZ`) — 자물쇠를 쥐고 있다
  2. **bit 1 이 꺼져 있음** (`+0x202 TEST DIL,2`) — "반납 중" 이 아니다
  3. bit 63 이 아직 꺼져 있음 (`+0x216 SHR RAX,0x38` / `+0x21c JS`) — 도장이 아직 없다. 이미 있으면 `+0x294` 쪽으로
  4. 그러면 칸의 락 주소를 장부 `UserAbEntries[칸]` 의 `+0x10` 에 복사하고 (`+0x252`), 칸 bit 0 이 켜져 있으면 항목 `+0x19` = 1 (`+0x25f`, `+0x269`), 그리고 **도장(bit 63)** 을 찍는다 (`+0x26e`)
- 비유: 관리사무소가 카드를 훑다가 "자물쇠 쥐고 있음" 이 적혀 있고, "반납 중" 도 아니고, 도장도 아직 없으면 → **장부에 옮겨 적고 도장을 찍는다.**

#### 언제 이 함수가 도나 — "CPU 에서 내려갈 때"

- **새 말 — 컨텍스트 스위치 (CPU 에서 내려가기)**: CPU 한 개가 지금 스레드를 멈추고 다른 스레드로 **바꿔 끼우는** 것 `[추측: 일반 지식]`. 비유: 계산대 하나에 직원이 **교대**하는 것. 앞 직원은 자리를 비운다.
- `[정적]` `KiAbProcessPreContextSwitch` 를 부르는 곳 (`.c` 머리): `KiSwapThread`, `KiQuantumEnd`, `KiDispatchInterrupt`, `KiExitDispatcher`, `KiCheckForThreadDispatch`, `KiDirectSwitchThread`, `KiHandleDeferredPreemption`.
  - 이름으로 보면 모두 **스레드가 CPU 에서 내려가는 때**다 — 기다리러 갈 때, 받은 시간을 다 썼을 때, 다른 스레드에 밀려날 때(선점).
- 해석 `[추측]`: bit 63 = "커널이 이 칸에 대해 **자기 항목을 만들어 뒀다**". 그러니 **락을 쥔 채 CPU 에서 내려간 스레드만** 켜진다.

#### 끄는 곳도 전부 커널 `[정적]`

- `KeAbUserModeEntryFree +0x92 ~ +0xa1` — 클래스 0x38 신청을 받았을 때 (2.2 ②, 2.7)
- `KiAbUmPostWait +0xd2` (`AND byte [RSP+0x57],0x7f`) — `KeWaitForAlertByThreadId +0x1de` (ret `+0x1e3`) 가 부름
- `KiAbProcessPostContextSwitch +0x329` (`AND CL,0x7f`)
- ntdll 은 bit 63 을 **읽기만** 한다 (`LdrpCallTlsInitializers +0x1ab`).
- 비유: 도장은 관리사무소만 찍고, 관리사무소만 지운다. 직원은 **보기만** 한다.

### 2.7 반납 순서 — 왜 "반납 중" 표시부터 하나

`LdrpCallTlsInitializers` 가 자물쇠를 놓을 때의 순서 `[정적]`:

1. `+0x1a7 OR byte [RSI],2` — **bit 1 켜기** ("지금 반납 중")
2. `+0x1ab CMP byte [RSI+7],R13B(0)` / `+0x1af JGE` — bit 63 이 **꺼져 있으면** 3 을 건너뛴다
3. 켜져 있으면 칸 번호 계산 → `+0x1e8 NtSetInformationThread(-2, 0x38, {칸 번호, 0}, 0x10)` (2.2 ②)
4. `+0x1ed MOV [RSI],R13(0)` — 칸 지우기

왜 1 이 먼저인가 — 코드에 보이는 것만:

- 2.6 의 켜는 조건 2번: 커널은 bit 1 이 켜진 칸을 **건너뛴다** (`+0x202`) `[정적]`.
- 그래서 1 을 먼저 하면, 2 를 읽은 뒤에 관리사무소가 끼어들어 **새로 도장을 찍는 일이 막힌다** — 이렇게 읽힌다 `[추측]` (코드 순서와 조건에서 짐작. 이유가 코드에 쓰여 있진 않다).
- 비유: 반납하러 가면서 카드에 "반납 중" 을 먼저 써 두면, 관리사무소가 그 칸은 안 건드린다. 그다음 도장이 있나 보고, 있으면 신청서, 마지막에 칸을 지운다.

클래스 0x38 이름:

- `[정적]` **PDB 에 없다.** `ntdll.pdb`·`ntoskrnl.exe.pdb` 타입 목록에 `THREADINFOCLASS` enum 자체가 없다 (공개 심볼이라 빠진 것).
- 외부 공개 헤더 기억으로는 `0x38` = `ThreadUpdateLockOwnership`, `0x39` = `ThreadSchedulerSharedDataSlot` `[추측]`.
  - 정황: 0x39 처리 함수가 `PspSchedulerSharedDataRegionSlotAllocate`, 0x38 처리 함수가 `KeAbUserModeEntryFree` 라 이름 뜻이 맞는다.
- 문서에는 "클래스 0x38 (`[추측]` `ThreadUpdateLockOwnership`)" 으로만 쓴다.

그래서 sample 시동에서는:

- 도장이 찍히려면 **TLS 서류철 자물쇠를 쥔 채** CPU 에서 내려가야 한다. run28 에서 봤듯 이 자물쇠는 **서류철을 찾는 짧은 동안만** 쥔다.
- 그러니 시동 때 도장이 찍힐 일은 거의 없고, `+0x1e8` 도 안 불릴 것 `[추측]` → 동적 **Q25** 에서 확인.

### 2.8 정정 ② `LdrShutdownProcess` 의 `RDI` 는 모듈 항목이 아니라 TEB

두 번째 정정이다. 이것도 짐작 → 근거 → 결론으로.

#### 짐작은 이랬다 `[추측]` (요청서 질문 4)

- `+0x168` 은 모듈마다 `LdrpCallTlsInitializers(0, 모듈 항목)` 을 부르는 자리다 (run28).
- 그 앞에 `[RDI+0x58] ≠ 0` 조건이 있다.
- 모듈마다 도는 자리니까 `RDI` 도 **모듈 항목**(`_LDR_DATA_TABLE_ENTRY`, 명부 한 줄)이겠지. 그러면 `+0x58` = `BaseDllName` (모듈 이름) — 이게 짐작이었다.
- 짐작이 그럴듯했던 이유: **모듈 항목의 `+0x58` 은 정말 `BaseDllName` 이다** (결과서). 오프셋 하나는 맞았다. 틀린 건 "`RDI` 가 무엇을 가리키나" 였다.

#### 근거 — `RDI` 는 함수 첫머리에서 TEB 로 정해지고 안 바뀐다 `[정적]`

- `+0x28 MOV RDI,GS:[0x30]` — `GS:[0x30]` 은 지금 스레드의 TEB 를 꺼내는 자리다 (run18 해설에서 본 `R13 = GS:[0x30]` 과 같은 것).
- 그 뒤로 함수 안에서 `RDI` 는 **안 바뀐다.**
- 그럼 모듈 항목은 누구인가 — **`RBX`** 다:
  - `+0xf9 LEA RBX,[RSI-0x20]`. `RSI` 는 `PebLdr+0x30` (`InInitializationOrderModuleList`, 초기화 순서 명부 고리)를 도는 포인터다.
  - 모듈 항목 `+0x20` 이 그 고리 연결 칸(`InInitializationOrderLinks`)이라, 고리 칸에서 `0x20` 을 빼면 **명부 한 줄의 시작**이 된다.
  - 실제로 `+0x163 MOV RDX,RBX` 로 `RBX` 를 둘째 인자(모듈 항목)로 넘긴다.

#### 결론 — `[RDI+0x58]` = `_TEB.ThreadLocalStoragePointer` `[정적]`

- PDB 로 `_TEB +0x58` 은 **`ThreadLocalStoragePointer`** 다. 이 스레드의 **TLS 배열** 포인터.
  - 비유: 직원 개인 안내판에 붙은 "**내 개인 사물함 열쇠꾸러미**" 칸.
- 그래서 `+0x168` 앞 조건은 둘이다:
  - `+0x156 CMP word [RBX+0x6e],R12W(0)` / `+0x15b JZ` — 모듈 항목 `+0x6e` = **`TlsIndex`**. TLS 디렉터리가 있는 모듈은 `LdrpInitializeTls +0x102`·`LdrpHandleTlsData +0x3a8` 에서 `0xffff` 가 된다 (run28). 그래서 `≠ 0` = "**TLS 있는 모듈**".
  - `+0x15d CMP qword [RDI+0x58],R12(0)` / `+0x161 JZ` — "**이 스레드에 TLS 배열이 있음**".
  - 둘 다 맞으면 `+0x163 MOV RDX,RBX` / `+0x166 XOR ECX,ECX` / `+0x168 CALL LdrpCallTlsInitializers` (ret `+0x16d`).
- 한 줄로: **"이 모듈에 TLS 가 있고, 지금 끝내는 이 스레드에 TLS 배열이 있으면" 그때만 종료 콜백(이유 0)을 부른다.**
- exe 몫도 같은 모양이다: `+0x18f` `[LdrpImageEntry]` → `+0x196 CMP word [RDX+0x6e],R12W` / `+0x19b JNZ` → `+0x267 CMP qword [RDI+0x58],R12` → `+0x2cc CALL LdrpCallTlsInitializers` (ret `+0x2d1`).
- 프로세스 **끝** 길이라 지금 목표(진입점까지) 범위 밖이다. 이름·뜻만 적었다.

#### 정정 둘에서 배울 점 (내 정리)

- 둘 다 "**레지스터·칸이 무엇을 가리키나**" 를 근처 모양만 보고 짐작한 데서 틀렸다.
  - 질문 2: "켜는 줄이 이 함수에 없다 → 이 함수가 부르는 쪽이겠지" → 실제론 **부르는 관계가 아예 없는** 커널이었다.
  - 질문 4: "모듈마다 도는 자리 → `RDI` 도 모듈이겠지" → 실제론 **함수 첫머리에서 정해진** TEB 였다.
- 이번엔 둘 다 "값이 처음 정해지는 줄" (`+0x28`, `+0x26e`) 을 찾아서 풀었다.

### 2.9 핵심 ③ 직접 호출 갈래 — 같은 꼴이 ntdll 에 23곳 더

#### 꼴 다시 보기 `[정적]` (run28)

- `ImageTlsCallbackCaller` `+0x7 LEA R8,[RtlAcquireSRWLockExclusive]` → `+0xe CMP RAX,R8` → `+0x11 JZ` (→ `+0x26`)
  - 같으면 `+0x29 CALL RtlAcquireSRWLockExclusive` (직접)
  - 다르면 `+0x16` CFG 간접 호출 (교환원을 거침)
- 두 갈래 다 셋째 인자 `R8D = 0` 이다 (`+0x13`, `+0x26`).

#### 이번에 본 것 — 이 함수만의 갈래가 아니다 `[정적]`

- ntdll 전체에서 "`LEA 함수 주소` → 4명령 안 `CMP` → 바로 `JE/JZ` → 목적지 8명령 안 **같은 주소로 직접 CALL**" 꼴을 세니 **23곳 더** 있다. 모양이 조금 다른 건 못 셌으니 **하한**이다.
- 예:
  - `RtlpTpWorkCallback` — `+0x208 LEA RtlAcquireSRWLockShared` → `+0x234` 직접 CALL, `+0x217 LEA RtlAcquireSRWLockExclusive` → `+0x22d` 직접 CALL (**같은 대상 함수**)
  - `LdrQueryProcessModuleInformationEx +0x33f LEA LdrQueryModuleInfoLocalLoaderUnlock` → `+0x3a2` 직접 CALL
  - `TpSetTimerEx +0x2a6 LEA` → `+0x2eb` 직접 CALL (대상 rva `0x829a0`, `_index` 이름 `TppFreeWait`, PDB 이름 하나뿐)
  - **`RtlUserThreadStart +0x13 LEA RCX,[rva 0x128f0]` / `+0x20 CMP RAX,RCX` / `+0x23 JZ` → `+0x30` 직접 CALL** (대상 = `RtlLeaveCriticalSection`, PDB 이름 하나뿐) — 우리 지도 2.10 에 있는 함수다. 아래에서 따로.

#### 그래서 — 컴파일러 꼴이라는 짐작 `[추측]` (그대로, 근거만 늘었다)

- **새 말 — PGO 간접 호출 승격**: 프로그램을 미리 돌려 본 기록(PGO)으로 "이 간접 호출은 대개 ○○ 로 간다" 를 알면, 컴파일러가 "**먼저 ○○ 인지 비교해서 맞으면 직접 부르기**" 로 바꿔 둔다는 꼴. 비유: 전화 교환원(CFG)을 거치기 전에 "혹시 늘 거는 그 번호야?" 하고 먼저 **단축 번호**로 거는 것.
- 단계별로:
  1. 근거: 같은 꼴이 ntdll 곳곳에 흔하다 (23곳+). `ImageTlsCallbackCaller` 만의 특별한 뜻이 있는 갈래라면 이렇게 흔할 이유가 없다.
  2. 그래서: 컴파일러가 기계적으로 만든 꼴로 보인다.
  3. 아직 확인 아님: 바이너리만으로 **컴파일러 동작을 증명할 순 없다.** 그래서 `[추측]` 그대로다.

#### sample 콜백은 이 갈래를 안 탄다 `[소스]` + `[정적+샘플]`

1. 콜백은 `ExeTls` (`sample/test.c:8`), `Dll1Tls` (`dll1.c:8`), `Dll2Tls` (`dll2.c:5`). 각 이미지의 `.CRT$XLB` 칸 하나로 등록 `[소스]`.
2. PE TLS 디렉터리 콜백 배열을 직접 읽었다 (`sample/bin/*`): test.exe `0x1400012a0`, dll1 `0x1800012a0`, dll2 `0x1800012a0` — 모두 rva `0x12a0`, 배열마다 콜백 **하나** + 끝 0.
3. 비교 대상 `RtlAcquireSRWLockExclusive` 는 **ntdll 안** (rva `0x393d0`).
4. 콜백은 각자 **다른 이미지** 안이다. 실행 때 서로 다른 주소에 올라가니 같을 수 없다 → `CMP` 는 늘 "다름" → `+0x16` 쪽.
   - dll1·dll2 선호 `ImageBase` 가 ntdll 선호 값과 같은 `0x180000000` 이지만, 실행 땐 모듈마다 다른 주소에 올라가니 결론은 같다.
- 비유: 안내원의 단축 번호는 **관리실 내선**(ntdll 안)이다. 우리 건물 사람들(sample 콜백)은 다른 건물에 있으니 단축 번호에 걸릴 일이 없다.

#### 곁가지 — `RtlUserThreadStart` 의 같은 꼴

- `[정적]` `+0x13 ~ +0x30`: 칸 `Kernel32ThreadInitThunkFunction` 값(kernel32 의 시작 함수)을 `RtlLeaveCriticalSection` 주소와 비교해, 같으면 직접 부른다.
- 두 값이 같아질 일은 없어 보인다 `[추측]`.
- `[관찰]` + `[정적]` 01_sprint Worker 종료 콜스택의 `RtlUserThreadStart+0x2c` 는 **간접 갈래**의 ret 다. 그래서 **Worker 에선 이 직접 갈래를 안 탔다.**
- 메인 스레드는 아직 안 봤다 → 동적 **Q30** (`+0x23 JZ`, `ntdll+0xacae3`).

### 2.10 앞 run 짐작들의 판정

| 어디 | 짐작 | 판정 | 어떻게 고쳤나 |
|---|---|---|---|
| run23 | TEB `+0x1850` 표 = "SRW 락 소유 추적" `[추측]` | 방향은 맞고 **좁았다** (임계영역·리소스도 씀) | run23 문서는 그대로, flow.md 4장 run23 줄에 "(해결됨, 정적)" |
| run28 | PDB 이름 `SchedulerSharedDataSlot` 은 쓰임새와 안 맞음 `[추측]` | **틀렸다.** PDB 이름이 맞다 (2.3) | run28 맨 위 정정 한 줄, flow.md 2.7 취소선 + 정정 |
| run28 | `+0x1e8` 앞 비트를 켜는 쪽 = `+0x113` callee `[추측]` | "이 함수 안엔 켜는 줄 없음" 은 맞고, **뒷 짐작은 틀렸다** (커널이 켬, 2.6) | 같음 |
| run28 | 직접 호출 갈래 = 컴파일러 갈래 `[추측]` | 그대로 `[추측]`, 근거 늘어남 (23곳+, 2.9) | flow.md 2.7 줄 더함 |
| 요청서 sast/36 | 질문 2 후보 = `RtlpAcquireSRWLockSharedContended` | **틀렸다** (2.6) | run 문서 "정정" |
| 요청서 sast/36 | 질문 4 `RDI` = 모듈 항목, `+0x58` = `BaseDllName` | **틀렸다** (2.8) | run 문서 "정정" |

### 2.11 정리 — 비유로 한 번 더

- 직원(스레드)은 출근 준비 때 관리사무소(커널)에서 **열쇠 대출 카드**(8칸 표)를 받아 개인 안내판(TEB `+0x1850`)에 꽂는다. 조건이 안 맞으면 카드 없이 일한다.
- 서류철 자물쇠를 쥘 때, 카드 빈 칸에 "**○○ 자물쇠 쥐고 있음**" 을 적는다. 이 카드는 서류철 자물쇠뿐 아니라 **다른 모양의 자물쇠**(임계영역 등)에도 같이 쓴다.
- 관리사무소는 직원이 **자리를 비울 때마다**(CPU 에서 내려갈 때) 카드를 훑는다. 자물쇠를 쥔 채 비운 칸이 있으면 **장부에 옮겨 적고 도장**(bit 63)을 찍는다. 그 직원을 먼저 일 끝내게 도와주려는 것으로 보인다 (AutoBoost `[추측]`).
- 직원은 반납할 때 먼저 "**반납 중**" (bit 1) 을 써 둔다. 관리사무소는 그 칸을 건드리지 않는다.
- 그다음 도장을 본다. 도장이 있으면 "**장부에서도 지워 주세요**" 신청서(클래스 0x38)를 내고, 없으면 그냥 칸을 지운다.
- 도장은 **관리사무소만 찍고 지운다.** 직원은 보기만 한다. (요청서는 직원 쪽 함수가 찍는다고 짐작했다 — 틀렸다.)
- 가게 문을 닫을 때(`LdrShutdownProcess`)는 "이 건물에 사물함 규칙(TLS)이 있고, **이 직원에게 사물함 열쇠꾸러미**(TEB `+0x58`)가 있으면" 마지막 인사(이유 0)를 부른다. (요청서는 `RDI` 가 건물 명부 줄이라고 짐작했다 — 틀렸다. 직원 안내판이었다.)

### 2.12 곁가지 — 도구 메모

run 문서 "도구 메모" 를 옮긴다. 분석 결과엔 영향이 없다.

- 결과서 질문 4 의 "`+0x19b CMP word [RDX+0x6e],0`" 은 `.asm` 으로 보면 **`+0x196 CMP word [RDX+0x6e],R12W` / `+0x19b JNZ`** 다. 뜻은 같다. run 문서와 이 글은 `.asm` 을 따랐다.
- 결과서가 `RtlpAcquireSRWLockSharedContended` 아래에 "부르는 함수는 …" 이라 적은 목록은 **이 함수가 부르는 것**(callee) 이다. run 문서는 그렇게 고쳐 적었다.
- `pdb_aliases.py --lookup ntdll.dll` 로 rva `0x128f0`·`0x393d0`·`0x829a0` 을 보니 셋 다 이름이 하나뿐이었다 (별칭 무리 아님). 모듈 이름은 `ntdll` 이 아니라 **`ntdll.dll`** 로 줘야 표를 찾는다 (plan 하네스 후보 (55)).
- 칸 이름·enum 이 "PDB 에 있나 없나" 는 `llvm-pdbutil dump -types` 로 봤다. 공개 PDB 엔 `THREADINFOCLASS` 같은 enum 이 빠져 있을 수 있다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 머리 (GUID 목록·정적 근거) | run40 까지 | **run41** (ntdll·ntoskrnl) 더함. `objdump` 꼴 검색 숫자는 "검색한 꼴 기준, 하한" 이라는 메모 한 줄 |
| 2.4 `LdrpInitializeProcess` 트리 | 없음 | **`+0x183c LdrpAcquireSchedulerSharedDataSlot`** 줄 `[정적]` (TEB `+0x1850` = 락 소유 칸 표 주소, 조건 맞을 때만). 앞 `+0x182c LdrpAllocateSchedulerSharedData` 는 본문 안 봄 |
| 2.7 자물쇠 줄 — PDB 이름 | "PDB 이름은 쓰임새와 안 맞아 `[추측]`" | **취소선** → (정정, run41 `[정적]`) **PDB 이름이 맞다** |
| 2.7 자물쇠 줄 — `+0x1e8` | "켜는 쪽은 `+0x113` callee 일 것 `[추측]`" | **취소선** → (정정, run41 `[정적]`) 켜는 건 **커널** `KiAbProcessPreContextSwitch +0x26e`. 시동 때 `+0x1e8` 은 안 탈 것 `[추측]` (동적 Q25) |
| 2.7 `LdrShutdownProcess` 줄 | `[RDI+0x58]` 뜻 모름 | `[정적]` `RDI` = **TEB**, `+0x58` = `ThreadLocalStoragePointer`. 조건 = `TlsIndex ≠ 0` **그리고** TLS 배열 있음 |
| 2.7 새 소절 | 없음 | **"TLS 락 소유 칸 표 (TEB `+0x1850`)"** — 도서관 비유, 이름·넣는 곳·조건, 칸 비트 표, 클래스 0x38, `RtlpAcquireSRWLockSharedContended` 는 표를 안 건드림 |
| 2.7 `ImageTlsCallbackCaller` 갈림 줄 | 컴파일러 갈래 `[추측]` | `[추측]` 그대로 + `[정적]` **같은 꼴 최소 23곳** (예 넷), sample 콜백 rva `0x12a0` vs ntdll rva `0x393d0` |
| 2.10 `RtlUserThreadStart` | — | `[정적]` 같은 꼴, 대상 rva `0x128f0` = `RtlLeaveCriticalSection`. Worker 는 직접 갈래 안 탐 `[관찰]` + `[정적]` |
| 3장 새 스레드 | — | `[정적]` `LdrpInitializeThread +0x50` (ret `+0x55`) 에서 **자기 몫 표**를 받음 |
| 4장 2.6 run23 줄 | TEB `+0x1850` 8칸 표 (`[추측]` SRW 락 소유 추적) | **(해결됨, 정적)** → run41: 방향은 맞고 SRW 전용 아님 |
| 4장 2.7 "이름·뜻 못 정한 것 (run28)" | 다섯 가지 | 뒤 넷 **(해결됨, 정적) run41**. TLS 목록 항목 칸 이름만 남음 |
| 4장 2.7 새 줄 | 없음 | 정적 **Q74** (a) bit 0, (b) 효과 없는 `RtlAbPreAcquire`, (c) 카드 받기 조건 둘, (d) 클래스 0x38·0x39 이름 |
| 4장 2.7 Q25 줄 / 2.10 Q30 줄 | — | (run41 더함) 락 소유 칸 실행 값, `RtlUserThreadStart +0x23` |
| 5장 run 목록·요청서 표 | — | run41 줄 둘 |

- 태그 변화를 한 줄씩:
  - TEB `+0x1850` 이름: `[추측]` (안 맞음) → **`[정적: PDB 타입]`** (맞음)
  - bit 63 켜는 곳: `[추측]` (ntdll callee) → **`[정적]`** (커널 `+0x26e`). 뜻은 여전히 `[추측]`
  - `[RDI+0x58]`: 뜻 모름 → **`[정적]`** (`ThreadLocalStoragePointer`)
  - 직접 호출 갈래 이유: `[추측]` → `[추측]` (근거만 늘어남)
  - SRW 락 소유 추적: `[추측]` → `[정적]` (방향 맞음, 범위 넓힘)

### 큰 그림에서 어떻게 이어지나

- 이번 바퀴는 지도의 **화살표를 하나도 안 바꿨다.** 대신 2.7 자물쇠 줄 옆에 붙어 있던 물음표 넷을 지웠다.
  - 지금 목표(프로세스 생성 → 진입점)에서 보면 **잔가지 정리**다. plan 에도 "대표 지점 목표엔 덜 중요한 작은 정적" 이라고 적혀 있다.
- 그래도 눈여겨볼 점이 하나 있다: 이 표는 ntdll 과 커널이 **같은 칸 모양(같은 마스크)을 약속하고 같이 쓰는** 칸이다 `[정적]` (2.4).
  - 표를 **받을 때**는 시스템 콜(신청 창구, 클래스 0x39)을 거친다.
  - 하지만 **도장을 찍을 때**는 신청이 없다. 커널이 CPU 교대 때 스스로 들여다본다 (2.6). 그래서 이 길은 호출 지도(`_map`)로는 안 보였고, 이름(`KiAb*`)과 칸(`_KTHREAD +0x320`) 검색으로 찾았다.
- 동적으로 갈 때(**Q25**) 들고 갈 예측이 하나 늘었다: 시동 때 TLS 자물쇠 칸의 **bit 63 은 꺼져 있고**, `+0x1e8` (`ntdll+0x11038`) 에는 **안 설 것** `[추측]`.
- 다음 항목은 **Q58** (plan 머리).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 칸 **bit 0** 은 누가 켜나 | 커널은 읽고(`+0x25f`) 지우기만(`KiAbUmPostWait +0x88`) 한다. ntdll 쪽 켜는 줄은 검색한 꼴로 못 찾았다 | 정적 **Q74** (a) |
| `RtlpAcquireSRWLockSharedContended +0x11b ~ +0x126` 의 `RtlAbPreAcquire(락, 칸)` 은 왜 부르나 | 칸 ≠ 0 일 때만 부르는데, 그때 `RtlAbPreAcquire` 는 아무것도 안 한다. 정적으로는 "효과 없는 호출" | 정적 **Q74** (b) |
| 카드 받기 조건 둘의 뜻 — `LdrpSchedulerSharedDataListHead` 를 누가 언제 채우나, PEB `NtGlobalFlag2 & 0x10` 은 무엇인가 | `LdrpAllocateSchedulerSharedData` (`+0x182c`) 본문을 이번엔 안 봤다. 안 맞으면 TEB `+0x1850` 이 0 → 칸 적기 건너뜀 | 정적 **Q74** (c) |
| 클래스 0x38·0x39 의 공식 이름 | 공개 PDB 에 `THREADINFOCLASS` enum 이 없다. 지금은 외부 헤더 기억 `[추측]` | 정적 **Q74** (d) (WinDbg `dt nt!_THREADINFOCLASS` 로 한 번 볼 수는 있지만 공개 심볼이면 안 나올 것) |
| 메인 스레드 TEB `+0x1850` 에 **실제로 표가 생기나** (0 이 아닌가), 몇 번 칸에 락 주소가 적히나 | 조건 둘이 실행 때 값이다 | 동적 **Q25** — `ntdll!LdrpAcquireSchedulerSharedDataSlot+0x92` 의 `rax`. 메인 스레드 몫은 `LdrpInitializeProcess+0x183c` 반환. test.exe 조건 필요 |
| 시동 때 TLS 자물쇠 칸의 bit 63 이 정말 꺼져 있나, `+0x1e8` 에 안 서나 | 켜지려면 락을 쥔 채 CPU 에서 내려가야 한다. 그건 실행 때 일이다 | 동적 **Q25** — `ntdll+0x10ffb` (`+0x1ab`) 에서 `dq @rsi L1`, `ntdll+0x11038` (`+0x1e8`) 에 안 설 것. 커널 `KiAbProcessPreContextSwitch+0x26e` 는 컨텍스트 스위치마다 걸려 **시동 길엔 비추천** |
| `ImageTlsCallbackCaller +0x11` 이 늘 "다름" 인가 | 정적으로는 "같을 수 없다" 까지. 실제로 안 타는지는 실행 때 | 동적 **Q25**·**Q28** — run28 이 적은 `ntdll+0xe8406` 에 안 설 것 |
| 메인 스레드도 `RtlUserThreadStart +0x23` 직접 갈래를 안 타나 | Worker 는 관찰로 안 탔다. 메인 스레드는 아직 안 봤다 | 동적 **Q30** — `ntdll+0xacae3` (`+0x23 JZ`) |
| bit 63 의 **뜻** ("커널이 자기 항목을 만들어 뒀다"), AutoBoost 의 뜻 | 켜고 끄는 위치는 `[정적]` 이지만 "왜" 는 코드에 안 쓰여 있다 | `[추측]` 그대로 |
| 직접 호출 갈래가 정말 컴파일러(PGO) 꼴인가 | 바이너리만으로 컴파일러 동작을 증명할 수 없다 | `[추측]` 그대로 |
| TLS 목록 항목(0x48바이트) 칸의 진짜 이름 | PDB 에 타입이 없다 (run28 부터 남은 것) | flow.md 4장 2.7 "이름·뜻 못 정한 것 (run28)" 줄 (번호 없음) |

---

## 5. 더 읽기

- run 문서: [`run41-tls-lock-detail.md`](run41-tls-lock-detail.md) — 호출 지점 표 전체, 질문 넷 근거 줄, 칸 비트 표, 호출 관계 트리, 정정, 동적·정적 남은 것, 도구 메모
- 정적 결과서: [`_workspace/sast/36_tls-lock-detail_result.md`](../../_workspace/sast/36_tls-lock-detail_result.md) — 질문별 답 (1-a ~ 4), `objdump` 꼴 검색 조건, 23곳 예, 추천 멈춤점, 새 정적 공백
- 요청서: [`_workspace/sast/36_tls-lock-detail.md`](../../_workspace/sast/36_tls-lock-detail.md) — 정정된 `[추측]` 둘의 원문 (질문 2·4)
- flow.md: [`flow.md`](flow.md)
  - 2.7 "TLS 락 소유 칸 표 (TEB `+0x1850`, run41 `[정적]`)" 소절, 자물쇠 줄의 정정 둘, `LdrShutdownProcess` 줄, `ImageTlsCallbackCaller` 갈림 줄
  - 2.4 `LdrpInitializeProcess` 트리 `+0x183c` 줄, 2.10 `RtlUserThreadStart` 줄, 3장 새 스레드
  - 4장 공백 표 2.6 run23 줄, 2.7 run28·run41 줄, Q25·Q30 줄
- 앞 해설:
  - [run28 해설](run28-tls-init-call-해설.md) 2.5 (자물쇠 — 서류철을 찾는 동안만), 2.8 (TLS 전용 안내원, 직접 호출 갈래), 4절 (이번에 닫은 Q56 세 줄)
  - [run23 해설 2.3](run23-load-dependent-해설.md#23-핵심--줄간선은-여기서-긋는다) (공유·독점 잠금)
  - [run11 해설 2.2](run11-allocate-thread-해설.md#22-한-줄씩-읽는-법) (TEB = 직원 개인 안내판)

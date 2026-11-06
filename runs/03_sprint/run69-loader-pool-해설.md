# run69 loader-pool — 해설

> 이 글은 [run69 문서](run69-loader-pool.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q132** — PE_emulator 요청 12: "로더 전용 풀 덫 안, 본문을 아직 안 본 함수 셋" (`_workspace/plan.md`, 이번에 **끝**)
  - 새로 생긴 할 일: **Q134** (정적, 맨 뒤) — 4절.
  - 동적 다섯 건은 **Q104 VM 단계** 에 얹었다.
  - 다음 항목: **Q133** (sast/65 cpu-features, 이미 도는 중) → 그다음 **Q104**.
- 근거:
  - run 문서 [`run69-loader-pool.md`](run69-loader-pool.md) (가장 중요)
  - 요청서 [`_workspace/sast/64_loader-pool.md`](../../_workspace/sast/64_loader-pool.md)
  - 결과서 [`_workspace/sast/64_loader-pool_result.md`](../../_workspace/sast/64_loader-pool_result.md) (판정: **분석성공**)
  - flow.md 변경: 그림 2 (`_LdrpInitialize` 아래 상자 하나 + `j1` 상자 글) · 2.4 (새 절 "로더 전용 스레드 풀", run62 `[공백]` 닫음) · 3장 일꾼 줄 · 4장 · 5장
- 이번 바퀴는 **정적만** 했다.
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 를 **다시 읽은 것** 이다. 새 실행이 아니다.

이미 푼 말은 **링크만** 건다.

- 스레드 풀 부품 (기본 풀 = 공용 **일꾼 사무실**, 일꾼 공장 = **파견소**, IO 완료 포트 = **우편함**, 타이머 큐의 `NtCreateTimer2` = **알람 시계**) → [run62 해설 2.2](run62-tp-etw-init-해설.md#22-새-말--스레드-풀-한-벌의-부품)
- 일감 개체 `LdrpMapAndSnapWork` = 대기실 벽 **호출 버튼** → [run24 해설 2.3](../02_sprint/run24-work-queue-해설.md)
- 덫 = PE_emulator 가 진짜 ntdll 함수를 Unicorn 안에서 돌리고, syscall 이 오면 대신 답해 주는 것 → [run58 해설](run58-registry-strings-해설.md) · [run62 해설 1절](run62-tp-etw-init-해설.md)
- 기본 대답 = "syscall 성공 (0), 출력 칸은 안 건드림" → [run66 해설 2.10](run66-apphelp-cache-해설.md)
- 프로세스 힙 = **창고**, GC 간격 전역 `0x1ce278` → [run46 해설](run46-process-heap-해설.md)
- 사진 P1a · P1b · P2 → [run44 해설 2.8](run44-baggage-snap-해설.md)

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**, 커널 = **관리사무소**.
- 스레드 풀 = **일꾼 사무실**. 이 가게엔 사무실이 둘이다 (run62):
  - 공용 사무실 (기본 풀, `+0xbe2` 에서 차림)
  - **짐 들이기 팀 전용 사무실** (로더 풀, `+0x1a47` 에서 차림) ← 이번 바퀴
- 일꾼 공장 = 사무실에 딸린 **파견소**. 진짜 파견소는 관리사무소 (커널) 안에 있고, 사무실은 그 **번호표 (핸들)** 만 들고 있다.

### 한 줄로 먼저 — 무엇을 얻었나

| 물음 | 답 |
|---|---|
| PE_emulator 가 본 "핸들 0" 은 진짜로 나올 수 있나? | **못 나온다** `[정적]`. 파견소를 못 차리면 사무실을 통째로 접고, 이 단계에 아예 안 온다 (2.3) |
| 그럼 왜 0 이었나? | PE_emulator 기본 대답이 핸들 칸을 안 써 줘서 `[정적]` (2.3) |
| 팀 사무실에 무엇을 정하나? | 쉬는 시간 30초 · 최대 3명 (2.4), 호출 버튼 하나 (2.5) |
| 덫 안 syscall 은 몇 번, 쉬는 시간 정하기는 몇 번째? | **14번**, 쉬는 시간은 **13번째** `[정적]` (2.6) |
| 은퇴 앞 `RtlpHpGCTimerEnable` 은? | 힙 청소 타이머를 **만들기만** 하고 켜지 않는다. syscall 1번 `[정적]` (2.7) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 출발
 └─ ntdll LdrInitializeThunk → _LdrpInitialize
      ├─ LdrpInitializeProcess
      │    ├─ … +0xbe2  공용 일꾼 사무실 (run62)
      │    ├─ … (사진 P1a)
      │    ├─ +0x1a47 LdrpEnableParallelLoading   짐 들이기 팀 전용 사무실   ← 이번 바퀴 ①
      │    ├─ … (사진 P1b)
      │    └─ … DLL 올리기 · 켜기
      ├─ +0x282 RtlpHpGCTimerEnable              힙 청소 타이머 만들기     ← 이번 바퀴 ②
      └─ +0x252 LdrpInitializationComplete → … NtContinue 은퇴 (사진 P2) → main
```

### 무엇이 궁금했나

- PE_emulator 81 이 진짜 `LdrpEnableParallelLoading` 을 덫 안에서 돌렸다.
- 그랬더니 14번째 syscall 이 `NtSetInformationWorkerFactory(핸들 0, 클래스 2, &buf, 8)` 였다 (돌아갈 곳 `ntdll+0xe655e` = `TpSetPoolWorkerThreadIdleTimeout +0x3e`).
- 물음: **핸들 0 이 진짜로 가능한 길인가?** 그리고 그 근처 함수 셋 (`TpSetPoolWorkerThreadIdleTimeout` · `TpAllocWork` · `RtlpHpGCTimerEnable`) 은 무엇을 하나?

### 바퀴 전에 알던 것

- `[정적]` (run19 · run24) `+0x1a47 LdrpEnableParallelLoading` 이 로더 전용 풀 `LdrpThreadPool` 과 일감 개체 `LdrpMapAndSnapWork` 를 만든다. 최대 스레드 = 스레드 수 − 1.
- `[정적]` (run62) 공용 풀은 더 앞 `+0xbe2` 에서 `TpAllocPoolInternal` 로 만든다. syscall 30. 단 `TpAllocPoolInternal +0x443` (클래스 `0xd`) 갈래를 타면 31 — 이건 `[공백]` 이었다.
- `[관찰]` (run44) 사진에 `LdrpThreadPool` 등 칠판 칸 값이 있다 (이번에 다시 읽음).

---

## 2. 찾은 것

### 2.1 한눈에

run 문서 호출 관계를 줄였다. 라벨은 ret 자리, `[#n]` 은 2.6 의 syscall 번호 `[정적]`.

```
LdrpEnableParallelLoading (ECX = LoaderThreads = 0)
 ├─ (1) +0x21  LdrpDetectDetour            NtQueryInformationThread(-2, 0x2a)       [#1]
 ├─ (2) +0x2e  RtlGetSuiteMask             bit 16 꺼짐 → 스레드 수 4
 ├─ (3) +0x49  TpAllocPoolInternal(&LdrpThreadPool, 1)    팀 사무실 차리기          [#2 ~ #12]
 │       ├─ +0x308 NtCreateWorkerFactory   → 풀 +0x38   (실패면 풀 폐기)
 │       ├─ +0x448 NtSetInformationWorkerFactory(0xd, 1)   ← 로더 풀만
 │       └─ +0x4be LdrpThreadPool ← 풀
 ├─ (4) +0x66  TpSetPoolWorkerThreadIdleTimeout(풀, −300000000)   쉬는 시간    [#13]
 ├─ (5) +0x7f  TpSetPoolMaxThreads(풀, 3)                         최대 3명     [#14]
 └─ (6) +0xe2  TpAllocWork(&LdrpMapAndSnapWork, LdrpWorkCallback, 0, &환경)   호출 버튼 (syscall 없음)
_LdrpInitialize (LdrpInitializeProcess 뒤)
 └─ +0x282 RtlpHpGCTimerEnable(RDX = LdrpThreadPool)   힙 청소 타이머 만들기   [(15)]
```

- 비유로 한 번 더: 짐 들이기 직전, 관리실이 **팀 전용 사무실** 을 차린다 → 파견소 등록 → "쉬는 시간 30초, 최대 3명" 규칙을 붙인다 → 벽에 **호출 버튼** 을 단다. 가게 문 열기 직전엔 창고 **청소 알람** 을 하나 만들어 둔다 (아직 안 맞춤).

### 2.2 팀 사무실 차리기 — 공용 사무실과 같은 업체, 주문서 한 칸만 다르다

- `[정적]` 로더 풀과 공용 풀은 **같은 `TpAllocPoolInternal`** 로 만든다. 다른 건 **둘째 인자** 하나다.
  - 공용 풀: `TppPoolpReferenceGlobalPool +0xbb XOR EDX,EDX` → 0
  - 로더 풀: `LdrpEnableParallelLoading +0x38 MOV EDX,1` → 1
- `[정적]` 둘째 인자가 1 이면 `+0x316` · `+0x31e JNZ +0x42e` 갈래로 가서 `+0x443` (ret `+0x448`) `NtSetInformationWorkerFactory` **클래스 `0xd`** (값 1, 4B) 를 한 번 더 부른다.
  - 성공하면 `+0x459` 에서 풀 `+0x17a` (`PoolFlags`) = 1 → `+0x460 JMP +0x324` 로 본 줄기에 합류.
  - 클래스 `0xd` 는 커널 일꾼 공장 `+0x198` 의 bit 11 에 값 bit0 를 복사한다 `[정적]`. 그 bit 의 **뜻은** `[공백]`.
- `[정적]` 로더 풀도 일꾼 공장을 만든다 (`+0x303/+0x308 NtCreateWorkerFactory`). 그래서 이 프로세스엔 **일꾼 공장이 둘** (공용 · 로더) 이다.
- 비유: 같은 건축 업체에 사무실을 두 번 주문했다. 두 번째 주문서엔 "짐 들이기 팀용" 칸에 체크가 돼 있어서, 업체가 파견소에 **표시 하나** (클래스 `0xd`) 를 더 붙여 준다. 그 표시가 무슨 뜻인지는 아직 모른다.

#### run62 정정 (짧게)

- run62: "`+0x443` 갈래를 test.exe 가 타나?" `[공백]` → 공용 풀 syscall 30 일 수도, 31 일 수도.
- run69: **공용 풀은 안 탄다** (둘째 인자 0) → **30 그대로** `[정적]`. 그 갈래는 로더 풀만 탄다.

### 2.3 ★ "핸들 0" 은 진짜에선 못 나온다 — 단계별로

먼저 무엇이 "핸들" 인지:

- 풀 개체 `+0x38` (PDB `_TP_POOL.WorkerFactory`) = 파견소 **번호표** 를 적는 칸 `[정적]`.
- `TpSetPoolWorkerThreadIdleTimeout` 은 `+0x26` 에서 이 칸을 그대로 읽어 syscall 에 넘긴다. 0 검사는 안 한다 `[정적]`.

그럼 이 칸은 누가 채우나 — 단계별로 `[정적]`:

1. `TpAllocPoolInternal` 은 `+0x28` 에서 출력 칸 (`LdrpThreadPool`) 을 **먼저 0 으로 지운다**.
2. `+0x2cb LEA RSI,[RDI+0x38]` → `+0x300 MOV RCX,RSI` → `+0x303/+0x308 NtCreateWorkerFactory`. 즉 커널이 **번호표를 풀 `+0x38` 에 직접 써 준다**.
3. 이 syscall 이 실패 (음수) 하면 `+0x310 JS` → `+0x465` → **풀을 통째로 버리고** 음수를 돌려준다.
4. `LdrpThreadPool` 에 풀 주소를 쓰는 건 성공이 확정된 `+0x4be` 한 곳뿐이다. 실패면 1번의 0 그대로.
5. 돌아온 `LdrpEnableParallelLoading` 은 `+0x4d JS +0xe4` 로 뛰어서 쉬는 시간·최대 인원·호출 버튼을 **다 건너뛴다**.

그래서:

- 쉬는 시간 정하기 (`TpSetPoolWorkerThreadIdleTimeout`) 까지 오는 풀은 **번호표가 방금 진짜로 생긴 풀뿐** 이다.
- 비유: 관리사무소가 파견소를 못 차려 주면, 관리실은 반쯤 지은 사무실을 **허물고** 칠판엔 "사무실 없음 (0)" 으로 둔다. 그러니 "사무실은 있는데 번호표가 빈칸" 인 상태로 다음 단계에 갈 수가 없다.

#### 그럼 PE_emulator 에선 왜 0 이었나

- 기본 대답이 `NtCreateWorkerFactory` 에 "성공" 만 주고 **번호표 칸을 안 써 줬다** `[정적]`.
- 풀은 힙에서 0 으로 채워 받은 블록 (`HEAP_ZERO_MEMORY`) 이라, `+0x38` 이 0 그대로 남았다.
- 비유: 관리사무소 역할을 하는 PE_emulator 가 "네, 파견소 차렸어요" 라고만 하고 번호표를 안 건넸다. 사무실 칸은 처음 그대로 빈칸.

#### PE_emulator 가 고칠 것 (run 문서 결론 2)

| syscall | 줄 대답 | 왜 |
|---|---|---|
| `TpAllocPoolInternal +0x303/+0x308` `NtCreateWorkerFactory` | 핸들 칸 (`RCX` = `&풀+0x38`) 에 **0 아닌 값** | 진짜와 같게 하려고 |
| `TpAllocPoolInternal +0x443/+0x448` `NtSetInformationWorkerFactory` 클래스 `0xd` | **0 이상** 상태 | 이 하나만 상태를 본다 (`+0x44e TEST` · `+0x450 JS +0x465`). 음수면 풀 폐기 → `LdrpThreadPool` = 0 → 뒤 길 전체가 달라짐 |
| 클래스 2 · 5 · 3 | 아무 값이나 (권고 `STATUS_SUCCESS`, 출력 쓰기 없음) | 상태를 아무도 안 본다 |

- 그러니 핸들 0 으로 가도 **이번 로더 길 자체는 안 깨진다** `[정적]`. 번호표를 채우는 건 "진짜와 같게" 하려는 것이다.

### 2.4 쉬는 시간 30초, 최대 3명

#### 한 줄 읽기 — `LdrpEnableParallelLoading +0x61` (call) / `+0x66` (ret)

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpEnableParallelLoading` | `+0x61` | `+0x66` | `ntdll!TpSetPoolWorkerThreadIdleTimeout(풀, −300000000)` | `+0x4d JS +0xe4` 안 탐 | [정적] |

- **누가**: 병렬 로딩 준비 함수 `LdrpEnableParallelLoading`.
- **어디서**: `+0x61` 에서 부르고 `+0x66` 으로 돌아온다 (WinDbg 콜스택엔 `+0x66` 으로 보인다).
- **누구를**: `TpSetPoolWorkerThreadIdleTimeout` — 이름 그대로 "풀 일꾼의 **쉬는 시간 제한**".
- **무엇을 위해**: 일감 없이 노는 일꾼을 얼마 뒤에 돌려보낼지 정하려고 `[추측: 함수 이름]`.
  - 넘기는 값 −300000000 (`+0x5a MOV RDX,-0x11e1a300`) 은 상수다 `[정적]`. 상대 30초로 읽는다 `[추측: NT 관례]`.
  - 안에서 `+0x39/+0x3e` `NtSetInformationWorkerFactory(풀+0x38, 클래스 2, &값, 8)` 한 번 `[정적]`.
  - 풀이 0 · 값 ≥ 0 · 종료 중이면 `TppRaiseInvalidParameter` 로 가지만, 로더 값으론 안 탄다 `[정적]`.

#### 최대 3명

- `[정적]` `+0x7a/+0x7f` `TpSetPoolMaxThreads(풀, 3)` → 안에서 `NtSetInformationWorkerFactory(풀+0x38, 클래스 5, &개수, 4)` 한 번.
- 계산 `[정적]` (`+0x66 ~ +0x70`): 스레드 수가 1 보다 크면 "스레드 수 − 1", 아니면 1. test.exe 는 스레드 수 4 → **3**.
- 일꾼 공장을 만들 때 넣은 최대는 `0x200` 이었고, 여기서 3 으로 줄인다 `[정적]`.

#### 두 값은 어디에 남나

- `[정적]` 두 함수 다 풀 개체엔 **아무것도 안 쓴다**. 값은 커널 일꾼 공장 안에만 있다.
- 비유: 쉬는 시간·정원 규칙은 사무실 칠판이 아니라 **관리사무소 파견소 장부** 에만 적힌다. 가게 안에서 보이는 사본이 없다 → 에뮬레이터 바깥 짐.
- 클래스 번호의 이름은 **PDB 에 없다** (`_WORKERFACTORYINFOCLASS` 열거가 ntdll·커널 어디에도 없음). 2 = 쉬는 시간, 5 = 최대 스레드 는 함수 이름으로 붙인 것이다 `[추측]`.

### 2.5 호출 버튼 하나 — `TpAllocWork` → `LdrpMapAndSnapWork`

- `[정적]` `+0xdd/+0xe2` `TpAllocWork(&LdrpMapAndSnapWork, LdrpWorkCallback, 0, &환경)`.
  - 조건: 스레드 수 > 1 (`+0x82 JBE`) · `LdrpDetourExist` 0 (`+0x8b JNZ`).
  - 힙에서 `0xf0`B (`_TP_WORK`) 를 받아 → `TppWorkInitialize` (풀 `Refcount` +1, 풀 `PoolObjectList` 꼬리에 걸기) → 출력 칸 = `LdrpMapAndSnapWork`.
  - 버튼이 눌리면 돌 함수 = `LdrpWorkCallback` (`0xe9ef0`). 그 본문은 아직 안 봤다 (Q134 (e)).
- `[정적]` **syscall 은 없다.** 관리사무소에 안 가고 관리실 안에서 끝난다.
- 요청서는 결과 전역을 `LdrpWorkQueue`·`LdrpLoaderWork` 로 짐작했는데 틀렸다 → run24 에 이미 있던 이름 `LdrpMapAndSnapWork` 다.
- 비유: run24 의 그 **호출 버튼** 을 다는 순간이 바로 여기다. 버튼은 가게 안 물건이라 관리사무소 신청이 필요 없다.

### 2.6 syscall 14번 — 쉬는 시간은 13번째

run 문서 syscall 표를 줄였다 `[정적]`. 기본 길 가정 (`LoaderThreads` 0 · SuiteMask bit 16 꺼짐 · `LdrpDetourExist` 0 등) 은 run 문서 표 위.

| # | 어디서 | syscall | 무엇 |
|---|---|---|---|
| 1 | `LdrpDetectDetour +0x9c/+0xa1` | `NtQueryInformationThread` | 클래스 `0x2a` |
| 2 ~ 3 | `TppPoolUpdateNodeRelation +0x4b/+0x50` | `NtQuerySystemInformationEx` | 클래스 `0x6b`, 첫 대답 `0xc0000004` 면 한 번 더 |
| 4 | `TpAllocPoolInternal +0x292/+0x297` | `NtCreateIoCompletion` | 우편함 → 풀 `+0x40` |
| 5 | `TpAllocPoolInternal +0x303/+0x308` | `NtCreateWorkerFactory` | 파견소 → 풀 `+0x38` |
| 6 | `TpAllocPoolInternal +0x443/+0x448` | `NtSetInformationWorkerFactory` | **클래스 `0xd`** — 로더 풀만 |
| 7 ~ 12 | 타이머 큐 둘 | `NtCreateTimer2` · `NtCreateWaitCompletionPacket` · `NtAssociateWaitCompletionPacket` | 큐마다 셋 |
| **13** | `TpSetPoolWorkerThreadIdleTimeout +0x39/+0x3e` | `NtSetInformationWorkerFactory` | **클래스 2**, 쉬는 시간 |
| 14 | `TpSetPoolMaxThreads +0x3b/+0x40` | `NtSetInformationWorkerFactory` | **클래스 5**, 최대 3 |

- 덫 안 합계 **14** `[정적]`. 그 밖에 힙이 자랄 때의 `NtAllocateVirtualMemory` 가 끼일 수 있다 (공용 부품 `RtlAllocateHeap` 안, 표 밖).

#### PE_emulator 는 왜 14번째로 봤나 — 짐작

1. 근거 `[정적]`: asm 순서로는 쉬는 시간이 **13번째** 다.
2. 그래서: PE_emulator 가 14번째로 봤다면 **앞에 표 밖 syscall 하나가 끼었다** 는 뜻이다.
3. 후보 (아직 확인 아님):
   - 1순위: 힙 확장 `NtAllocateVirtualMemory`.
   - 2: `NtQuerySystemInformationEx` 를 3번 부름 (`TppPoolUpdateNodeRelation +0x56 CMP EAX,0xc0000004` 가 또 맞으면 `+0xbd JMP` → `+0x23` 으로 되돌아감).
4. 가리는 법: PE_emulator 가 앞 13개 이름을 위 표 1 ~ 12 와 대조하면 된다.

#### 덤 — 1번 `LdrpDetectDetour` (flow-writer 보탬)

- `[정적]` `LdrpDetectDetour` 는 `+0x1a47` 이 처음 부르는 곳이 아니다. 더 앞 kernel32 준비 길 `LdrpLoadDllInternal +0x1bb/+0x1c0` 에서도 부른다. 거기 들어가는 조건 (`+0x1a1 ~ +0x1b5`) 은 안 봤다 (Q134 (d)).
- `[정적]` `LdrpDetourExist` 를 1 로 만드는 길은 둘: 핵심 로더 함수 머리 비교가 어긋남 (`+0x7a`), 또는 `NtQueryInformationThread(0x2a)` 답이 1 (`+0xac`). 사진은 끝까지 0 `[관찰]` (2.8).

### 2.7 은퇴 앞 — 힙 청소 타이머를 "만들기만" 한다

새 말 둘:

- **타이머** (TP 타이머, `_TP_TIMER`): 정한 때가 되면 일꾼에게 일을 시키는 TP 개체 `[추측: 일반적으로 알려진 설명]`. run62 에선 같은 종류가 WNF 쪽에 나왔다 ([run62 해설 2.2](run62-tp-etw-init-해설.md#22-새-말--스레드-풀-한-벌의-부품)).
  - 비유: 사무실 벽에 거는 **알람**. 시각을 맞춰야 울린다.
- **힙 GC**: GC 는 보통 "쓰레기 정리 (garbage collection)" 의 줄임으로 알려져 있다 `[추측: 일반적으로 알려진 설명]`. 여기선 이름대로 힙 (창고) 정리 일 `[추측: 이름]`. GC 간격 전역 `0x1ce278` 은 run46 에서 이미 봤다.
  - 비유: **창고 청소 알람**.

무엇을 하나 — 단계별로 `[정적]`:

1. `LdrpInitializeProcess` 가 0 이상을 주면 `_LdrpInitialize +0x215 MOV RDX,[LdrpThreadPool]` · `+0x21f JNZ` → `+0x27d` (ret `+0x282`) `RtlpHpGCTimerEnable`.
   - 즉 **팀 사무실이 있을 때만** (`LdrpThreadPool ≠ 0`) 탄다.
   - 주소로는 `+0x282` 가 `+0x252 LdrpInitializationComplete` 보다 뒤지만, `+0x282 JMP +0x221` 로 돌아와서 **실행은 그 앞** 이다.
2. 이미 타이머가 있으면 (`+0x24`) `0xff` 를 돌려주고 끝.
3. 아니면 힙에서 `0x168`B (`_TP_TIMER`) 를 받아 `TppInitializeTimer` 로 채운다. 콜백 칸 `+0x50` = `RtlpHpGCTimerCallback`.
4. `TppInitializeTimer +0xcb/+0xd0` 에서 **syscall 1번**: `NtSetInformationWorkerFactory(풀+0x38, 클래스 3, &1, 4)`.
   - 왜 부르나: 풀 `BindingCount` (`+0x1b0`) 옛 값이 0 이라서 (0 → 1). `TpAllocWork` 는 이 칸을 안 올리고 **타이머만** 올린다. 그래서 로더 풀의 클래스 3 은 이때 한 번이다.
5. `+0x109 CMPXCHG` 로 칠판 칸 `0x1ce270` 에 타이머를 꽂는다.
6. **켜지는 않는다.** `NtSetTimer2` 류가 없다.

그럼 언제 켜나:

- `[정적]` 켜는 곳은 따로 있다: `RtlpHpGCTimerSchedule +0x52 TpSetTimerEx(타이머, &0x1ce278, 0, 1000)`. 힙 코드 (`RtlAllocateHeap +0x9ef` 등) 가 조건을 맞출 때 부른다.
- **은퇴 전에 실제로 켜지는지는 동적** 이다 (4절).
- 비유: 문 열기 직전에 창고 청소 알람을 벽에 **걸어만** 둔다. 시각은 창고를 쓰다가 "이제 맞춰야겠다" 싶을 때 따로 맞춘다.
- 반환값은 `_LdrpInitialize` 가 안 본다 (`+0x221 MOV EDI,[RSP+0x30]`) `[정적]`.

### 2.8 사진이 받쳐 준다 `[관찰]`

run44 사진의 ntdll 칠판 칸을 flow-writer 가 다시 읽었다 (새 실행 아님).

| 칸 | P1a | P1b | P2 | 뜻 |
|---|---|---|---|---|
| `LdrpThreadPool` (`0x1d25c0`) | 0 | **`0x25996f387d0`** | 같음 | 팀 사무실이 **P1a ~ P1b 사이** 에 생겼다 = 만들기 성공 |
| `LdrpMapAndSnapWork` (`0x1d2640`) | 0 | **`0x25996f38450`** | 같음 | 호출 버튼도 달렸다 |
| `LdrpDetourExist` (`0x1d2610`) | 0 | 0 | 0 | detour 없음 |
| `TppPoolpList` `Blink` (`0x1cc728`) | `0x25996f31c00` (= 공용 풀 + `0x180`) | **`0x25996f38950`** (= 로더 풀 + `0x180`) | 같음 | 사무실 목록 꼬리에 로더 풀이 붙었다 |
| GC 타이머 `0x1ce270` | 0 | 0 | **`0x25996f43210`** | 청소 알람이 **P1b ~ P2 사이** 에 걸렸다 = `+0x27d` 구간 |
| "예약됨" 깃발 `0x1ce280` | 0 | 0 | 0 | 은퇴 순간엔 예약 표시 없음 |

- 사무실이 성공적으로 생겼으니, 2.3 결론대로 번호표 칸도 0 이 아니었을 것이다 `[추측: 핸들 값 자체는 사진에 없음 — 풀이 힙 안이라 안 읽음]`.
- 깃발이 P2 에서 0 이라고 "은퇴 전에 알람을 안 맞췄다" 고는 못 한다. 맞췄다가 콜백이 지웠을 수도 있다 `[추측]` → 동적 4번.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 그림 2 `_LdrpInitialize` 아래 | (2) `LdrpInitializeProcess` → (3) `+0x252 LdrpInitializationComplete` | (3) **`+0x282 RtlpHpGCTimerEnable`** 상자 새로 `[정적]` → (4) `+0x252` |
| 그림 2 `j1` 상자 | 로더 풀 + 일감 개체 (조건) | + "syscall 14 — 둘째 일꾼 공장 · 클래스 `0xd` · 유휴 시간 · 최대 3" |
| 2.4 새 절 | — | "로더 전용 스레드 풀" (2.1 트리 + 사실 7줄 + `[관찰]` 사진 줄) |
| 2.4 run62 syscall 30 줄 | `+0x443` 갈래 타는지 `[공백]` | 해결 → 안 탄다, **30 그대로** `[정적]` |
| 3장 일꾼 | — | 로더 공장 최대 3, 유휴 30초 `[추측: 단위]`. 어느 공장 일꾼인지는 여전히 동적 |
| 4장 | — | 해결 줄 하나 + 새 동적·정적·보탬 줄 |

- 짐 지도 `baggage.md` 는 3.8 (칠판 칸) 과 새 7.6.9 (풀·일감·타이머 칸, 커널 개체) 가 늘었다. 7.3 "쓴 자리 모르는 전역" 은 311 → **308**.

### 큰 그림에서

- 이제 `LdrpInitializeProcess +0x1a47` 부터 은퇴 앞 `+0x27d` 까지, **팀 사무실 한 벌이 어떻게 차려지는지** syscall 단위로 다 셌다.
- PE_emulator 는 이 표로 덫 안 syscall 을 하나씩 맞춰 볼 수 있다. 바꿀 곳은 둘 (번호표 채우기, 클래스 `0xd` 성공).
- 커널 쪽 짐이 또렷해졌다: 이 프로세스엔 **일꾼 공장 둘**, 로더 공장엔 쉬는 시간·최대 3·바인딩·bit 11 이 커널 안에만 있다 → 에뮬레이터 바깥 짐.
- 다음 이어짐: 청소 알람이 은퇴 전에 켜지면 (`TpSetTimerEx`) syscall 이 더 생길 수 있다. 그건 Q134 (a) 정적 + Q104 동적.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `TpSetTimerEx` (rva `0x7eb60`) 아래 syscall — 알람을 맞출 때 무엇이 더 생기나 | 이번 범위 밖 (켜는 곳만 찾음) | **Q134 (a)** 정적 |
| 커널 `NtSetInformationWorkerFactory` 클래스 2·3·5·`0xd` 갈래, `+0x198` bit 11 의 뜻, 클래스 이름 | Ghidra 가 점프표를 못 풀었고, PDB 에 열거가 없다 | **Q134 (b)** 정적 |
| 공개 `TpAllocPool +0x27` 의 `JMP` 가 넘기는 둘째 인자 | `[공백]` 그대로 | **Q134 (c)** 정적 |
| `LdrpLoadDllInternal +0x1a1 ~ +0x1b5` — kernel32 준비 때 `LdrpDetectDetour` 를 부르는 조건 (부르면 `0x2a` 가 덫 밖에서 한 번 더) | flow-writer 가 `_calls.tsv` 에서 찾았지만 조건은 안 봤다 | **Q134 (d)** 정적 |
| `LdrpWorkCallback` (`0xe9ef0`) 본문 — 버튼이 눌리면 일꾼이 하는 일 | 요청 밖 | **Q134 (e)** 정적 |
| 번호표가 정말 0 이 아닌지, 풀·일감 칸 값 | 풀은 힙 안이라 사진에 없다 | Q104 동적 1 (`ntdll+0x8de4b` 에서 `dt ntdll!_TP_POOL`) |
| syscall 순서·개수, 같은 번호표인지, 표 밖 `NtAllocateVirtualMemory` 수 | 실행 때 값 | Q104 동적 2 |
| 입력 값 (`LoaderThreads`, SuiteMask bit 16, KUSD 칸, `TppHeapTag` 등) | 실행 때 값 (일부는 sast/57 `[관찰]`) | Q104 동적 3 |
| 청소 알람이 은퇴 전에 켜지나 (`RtlpHpGCTimerSchedule`·`TpSetTimerEx` 입구) | 힙 코드가 언제 부르는지는 실행을 봐야 함 | Q104 동적 4 |
| 커널 일꾼 공장 값 (−30초 · 3 · 바인딩 1 · bit 11) | 읽을 타입 이름이 PDB 에 있는지부터 `[공백]` | Q104 동적 5 |

---

## 5. 더 읽기

- run 문서: [`run69-loader-pool.md`](run69-loader-pool.md) — syscall 표 전체, 작은 표, 추천 멈춤점
- 결과서: [`_workspace/sast/64_loader-pool_result.md`](../../_workspace/sast/64_loader-pool_result.md) — 1-7 (PE_emulator 가 줄 대답), 3-6 (GC 타이머 전역 세 칸)
- 요청서: [`_workspace/sast/64_loader-pool.md`](../../_workspace/sast/64_loader-pool.md)
- flow.md: [2.4 "로더 전용 스레드 풀"](flow.md) 절, 그림 2 `_LdrpInitialize` 아래
- 짐 지도: [`baggage.md`](baggage.md) 3.8 · 7.6.9
- 앞 이야기: [run62 해설](run62-tp-etw-init-해설.md) (공용 사무실, syscall 30) · [run24 해설](../02_sprint/run24-work-queue-해설.md) (호출 버튼) · [run46 해설](run46-process-heap-해설.md) (창고, GC 간격)

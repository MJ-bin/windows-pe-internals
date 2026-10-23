# run24 work-queue — 해설

> 이 글은 [run24 문서](run24-work-queue.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q12c** — 2.5·2.6·3장 일감 흐름(`LdrpQueueWork`·`LdrpProcessWork`·`LdrpDrainWorkQueue`)과 그래프 정리(`LdrpCondenseGraph` → `LdrpCondenseGraphRecurse` → `LdrpMergeNodes`) (`_workspace/plan.md`, 이번에 **끝**)
  - 이걸로 **Q12 (a·b·c) 가 모두 끝났다** (run22 ~ run24).
  - 새 대기열 항목은 **없다.** 동적으로 볼 것은 이미 있던 **Q23·Q47**, 정적으로 남은 주소 연결은 **Q50** 에 출처를 더했다.
  - 다음 항목: Q13 (단계 2). 그 전에 Q14 를 합칠지 사용자 결정, 하네스 묶음 검토 (plan "다음 항목").
- 근거:
  - run 문서 [`run24-work-queue.md`](run24-work-queue.md) (가장 중요)
  - 요청서 [`_workspace/sast/19_work-queue.md`](../../_workspace/sast/19_work-queue.md)
  - 결과서 [`_workspace/sast/19_work-queue_result.md`](../../_workspace/sast/19_work-queue_result.md) (판정: **분석성공**)
    - 결과서와 run 문서가 다른 곳은 run 문서를 따른다 (2.11).
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md`
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll (`ntdll.dll`, 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`). run17 ~ run23 과 같은 파일이다.
  - 본문을 읽은 건 **여덟 함수**다:
    - 일감 흐름 다섯: `LdrpQueueWork`, `LdrpWorkCallback`, `LdrpDrainWorkQueue`, `LdrpProcessWork`, `LdrpSignalModuleMapped`
    - 그래프 정리 셋: `LdrpCondenseGraph`, `LdrpCondenseGraphRecurse`, `LdrpMergeNodes`
  - 곁에서 "부르는 조건만" 본 함수: `LdrpEnableParallelLoading`, `LdrpProcessMappedModule`, `TppWorkpExecuteCallback` 등.
  - 그 아래 함수(`LdrpMapDllSearchPath`, `LdrpSnapModule`, `TpPostWork`, `TpAllocWork`, `LdrpDestroyNode` 등)는 **본문을 안 읽었다.** 이름·인자·결과만.

이 글은 [run23 해설](run23-load-dependent-해설.md) 의 **다음 편**이고, **Q12 의 마지막 편**이다.

- run22 에서 **건물 담당**(`LdrpMapAndSnapDependency`)이 "기대는 건물 목록" 을 한 줄씩 읽고 줄마다 배달 기사에게 맡기는 걸 봤다.
- run23 에서 **배달 기사**(`LdrpLoadDependentModuleInternal`)가 연결 카드 사이에 줄을 긋고, 새 건물이면 **게시판에 "가져와 주세요" 쪽지**를 붙이고 끝내는 걸 봤다.
- 그때 남긴 질문이 셋이었다:
  1. 게시판 쪽지를 **누가 떼어 가서** 건물을 실제로 가져오나?
  2. 늦게 도착한 자식 건물이 있을 때, 부모 **체크판을 지우고 전선 꽂기로 잇는 사람**은 누구인가?
  3. 그어 둔 줄을 **읽고 정리하는** 쪽(그래프 정리)은 무엇을 하나?
- 이번엔 **게시판 자체와, 쪽지를 떼어 가는 사람들**의 본문을 처음 펼쳤다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출, 비트와 플래그 → [run9 해설](run9-create-user-process-해설.md) 2절
  - 오프셋 순서 ≠ 실행 순서 → [run9 해설](run9-create-user-process-해설.md) 3.7
- PEB, 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
- 잠금 → [run11 해설 2.8](run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다)
- 함수 주소를 "부르지 않고 건네기" (지도에 화살표가 안 생기는 이유) → [run11 해설](run11-allocate-thread-해설.md) 2.5
- 이벤트(**신호등**) → [run13 해설](run13-resume-thread-해설.md) 2.3
- 꼬리 점프 (콜스택에 안 쌓이는 넘어가기) → [run17 해설](run17-ldr-init-entry-해설.md) 2.4
- 임계 구역(유저 쪽 잠금, **열쇠**) → [run18 해설](run18-proc-init-setup-해설.md) 2.4
- 로드 문맥(**작업 지시서**), DDAG 노드(**연결 카드**) → [run19 해설](run19-proc-init-modules-해설.md) 2.6
- 병렬 로딩, 스레드 풀과 일감, 게시판(`LdrpQueueWork`·`LdrpDrainWorkQueue`), 일꾼이 생길 수 있는 첫 자리 `+0x1a42` → [run19 해설](run19-proc-init-modules-해설.md) 2.9
- `LoadOwner`(짐 들이기 주인 표시), 메인 직원 표, 우회 검사(`LdrpDetectDetour`) → [run21 해설](run21-load-dll-internal-해설.md) 2.11
- 지도 이름 사슬, 설치 담당 `LdrpMapDllWithSectionHandle` → [run21 해설](run21-load-dll-internal-해설.md) 2.4
- 노드 상태 번호(**진행 도장**) → [run21 해설](run21-load-dll-internal-해설.md) 2.6, [run22 해설](run22-map-snap-dep-해설.md) 2.6
- 간선(카드 사이 줄), 부모 / 자식 → [run22 해설](run22-map-snap-dep-해설.md) 2.4
- 남은 자식 수 `+0x6c` (**체크판**, "자식 수 + 1"), 끝맺음, 두 번째 쪽지 `+0x4f7` → [run22 해설](run22-map-snap-dep-해설.md) 2.6
- 끼워 넣기(인라인, 본문을 통째로 복사해 붙임) → [run23 해설](run23-load-dependent-해설.md) 2.1
- 쪽지 두 종류 ("가져와 주세요" `+0x182c` / "전선 꽂아 주세요" `+0x4f7`) → [run23 해설](run23-load-dependent-해설.md) 2.6

비유도 그대로 이어 쓴다.

- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- DLL = **건물**, 모듈 항목 = **명부 한 줄**, DDAG 노드 = 명부 줄마다 붙은 **연결 카드**, 간선 = 카드 사이 **줄**
- 로드 문맥 = **작업 지시서**, 부모 지시서 `+0x6c` = **체크판**, 노드 `State` = 카드의 **진행 도장**
- snap = 콘센트에 **전선 꽂기**, 올리기(매핑) = 건물을 **가져와 설치하기**
- `LdrpMapAndSnapDependency` = **건물 담당**, `LdrpLoadDependentModuleInternal` = **배달 기사**, `LdrpMapDllWithSectionHandle` = **설치 담당**
- `LdrpQueueWork` = **게시판에 쪽지 붙이기**, `LdrpDrainWorkQueue` = **게시판 비우기**, 스레드 풀 일꾼 = **일꾼**
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- run 문서는 이번에 "**이삿짐 센터의 일감 상자**" 비유를 썼다. 이 글은 그걸 가게 그림으로 옮긴다 (가리키는 건 같다).
  - 일감 상자 = **게시판**
  - 상자에 넣는 쪽지 = **작업 지시서 한 장** (이번에 알게 된 것: 쪽지가 따로 있는 게 아니라 지시서 자체를 붙인다, 2.3)
  - 아르바이트생 = **일꾼** (스레드 풀)
  - 현장 반장 = **짐 들이기 책임자** (`LoadOwner` 를 단 직원)
  - "끝났다" 벨 = **신호등** (이벤트)
  - 짐 가져오기 = **올리기**, 전기·수도 잇기 = **전선 꽂기** (snap)
  - 남은 짐 수 = **체크판**

한 줄로 먼저 말하면:

- **일감 = 작업 지시서 한 장.** `LdrpQueueWork` 는 지시서를 게시판 **맨 끝에 붙이기만** 한다 (일꾼이 등록돼 있으면 "일 왔어요" 버튼만 누른다).
- 떼어 가는 쪽은 둘이다: **짐 들이기 책임자 직원**(`LdrpDrainWorkQueue` 안에서 직접), 또는 **일꾼**.
- 뗀 지시서는 `LdrpProcessWork` 가 카드 도장을 보고 처리한다: 도장 0 이면 **가져오기**, 아니면 **전선 꽂기**.
- 자식 건물이 도착하는(Mapped) 순간 `LdrpSignalModuleMapped` 가 **부모 체크판을 하나 지우고**, 0 이 되면 부모의 "전선 꽂아 주세요" 지시서를 다시 게시판에 붙인다.

이번 바퀴의 **핵심 넷**:

1. **게시판** — `LdrpQueueWork` 는 넣기만 한다. 일꾼을 깨우는 건 전역 `LdrpMapAndSnapWork` 가 **0 이 아닐 때만**이다. "병렬 로딩이 켜졌나" 깃발은 따로 없다 → 2.3
2. **`LdrpDrainWorkQueue(0/1)`** — 이 스레드가 "**짐 들이기 책임자**" 가 되어 게시판을 **직접 비우거나 기다린다.** 그래서 kernel32 준비 때(일꾼을 아직 안 부른 때)는 **일꾼 없이 혼자 비운다** `[정적: 코드상]` → 2.4
3. **`LdrpSignalModuleMapped`** — 자식이 올라오면 부모 카운터를 줄이고, 0 이면 부모 snap 일감을 넣는다. run22 부터 비어 있던 `?` 칸이 이것이다 → 2.7
4. **그래프 정리** = 서로 기대는 **고리를 하나로 합치기.** test.exe → dll1 → dll2 에는 고리가 없어서 **아무것도 안 합친다** → 2.9

그리고 하나 더: **run23 해설의 `[추측]` 하나가 틀렸다** ("`+0x12df` 에서 일감이 그 자리에서 올라왔나에 따라 갈린다"). 왜 틀렸는지 2.8 에서 천천히 푼다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)              (run17 ~ run19)
        ├─ +0x12e3  LdrpInitParallelLoadingSupport  (게시판·열쇠·신호등 만들기)       (run19, 이번에 내용)
        ├─ +0x19a2  LdrpInitializeKernel32Functions → … LdrpLoadDllInternal          (run20·run21)
        │     └─ … +0x21a LdrpDrainWorkQueue(1)                                     ← 이번 (kernel32 때 게시판)
        ├─ +0x1a47  LdrpEnableParallelLoading  (일꾼 풀 + 일감 개체)                 (run19, 이번에 조건)
        ├─ +0x1aa9  LdrpMapAndSnapDependency (exe 지시서)                            (run22)
        │     └─ +0x34e LdrpLoadDependentModuleInternal [dll1 추정]                 (run23)
        │           └─ +0x182c LdrpQueueWork                                       ← 이번 (게시판에 붙이기)
        ├─ +0x1ab1  LdrpDrainWorkQueue(1)                                           ← 이번 (게시판 비우기)
        │     └─ +0x187 LdrpProcessWork → 올리기 / snap                             ← 이번
        │           └─ … LdrpProcessMappedModule +0x1e2 LdrpSignalModuleMapped      ← 이번 (체크판 지우기)
        ├─ +0x1af2  LdrpPrepareModuleForExecution (exe 항목)                         (run21)
        │     └─ +0x12f LdrpCondenseGraph                                           ← 이번 (그래프 정리)
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16, 뒤는 추측)
```

- 이번 자리도 여전히 **짐 들이기**(`+0x1aa9`)와 **전원 켜기**(`+0x1bbb`) 사이다.
- run22 가 "목록을 도는 바깥 고리", run23 이 "한 줄을 맡은 배달 기사" 였다면, 이번은 **배달 기사가 붙인 쪽지가 그다음 어디로 가나**다.
- flow.md 로는 **2.6** (`[공백]` import DLL 이 메모리에 올라오는 순간) 이 중심이다. **2.5** (kernel32 준비 때 게시판), **2.7** (그래프 정리), **3장** (일꾼) 에도 더해졌다.

### 무엇이 궁금했나

run 문서 "목적" 그대로. run22·23 이 남긴 `?` 셋을 일감 함수 본문으로 푼다.

1. 일감을 **어디에 넣고, 누가, 언제 꺼내 올리나** (`LdrpQueueWork`·`LdrpWorkCallback`·`LdrpDrainWorkQueue`·`LdrpProcessWork`).
2. 자식이 다 올라온 뒤 **부모 카운터(`ctx+0x6c`)를 줄이고 snap 으로 잇는 코드**는 어디인가 (`LdrpSignalModuleMapped`).
3. 그래프 **정리**(`LdrpCondenseGraph` → `Recurse` → `LdrpMergeNodes`)는 무엇을 하고, test.exe → dll1 → dll2 에서는 무엇을 하나.

### 바퀴 전에 알던 것

- `[정적]` (run19) `LdrpInitializeProcess +0x12e3 LdrpInitParallelLoadingSupport` 는 이벤트·임계 구역만 만든다. `+0x1a47 LdrpEnableParallelLoading` 의 callee 에 `TpAllocPoolInternal`·`TpAllocWork` 가 있다 → 일꾼이 생길 수 있는 가장 이른 자리.
- `[추측]` (run19) kernel32·kernelbase 는 일꾼 없이 메인 직원 혼자 들일 것 (kernel32 준비가 `+0x1a47` 앞이라).
- `[정적]` (run22) 건물 담당의 끝맺음: 체크판 자기 몫 −1 → 0 이면 부모 없음 `+0x4c2 LdrpSnapModule` 직접 / 부모 있음 `+0x4f7 LdrpQueueWork`.
- `[정적]` (run22) 그런데 kernelbase 같은 "부모 있는" 건물은 일꾼 풀이 없을 때도 `+0x4f7` 쪽지를 붙이는 코드 길이 있다 → "그 쪽지를 누가 처리하나" 는 모름.
- `[정적]` (run23) 배달 기사는 잠금(`LdrpModuleDatatableLock`) 안에서 줄을 긋고 `+0x182c LdrpQueueWork` 로 "가져와 주세요" 쪽지를 붙인다. 병렬 로딩이 켜졌는지는 **안 본다.**
- `[정적]` (run23) 배달 기사의 `+0x12f7` 은 "새 줄이 안 생겼거나 자식 `State ≥ 2`" 일 때만 부모 체크판을 1 줄인다. 새로 이은 자식이 **나중에** 올라왔을 때 지우는 코드는 **배달 기사에도 건물 담당에도 없었다.**
- `[추측]` (run23) `LdrpQueueWork` 가 그 자리에서 다 올렸으면 `+0x12df` 에서 자식 `State ≥ 2`, 미뤘으면 `< 2` → 병렬 켜기 앞뒤 차이가 여기 나타날 수 있다. (← 이번에 **틀린 걸로** 밝혀졌다, 2.8)
- `[관찰]` (run5) test.exe 에 ntdll 일꾼(`TppWorkerThread`) 2개가 있다. (run8) `+0x1bb6` 에서 멈췄을 땐 dll1·dll2 가 **이미** 올라와 있었다.
- `[소스]` test.exe 가 직접 import 하는 건 dll1 뿐, dll2 는 dll1 을 거쳐 붙는다.

---

## 2. 찾은 것

### 2.1 한눈에 — 일감 한 바퀴

run 문서 "일감 한 바퀴 그림" 을 줄였다. 숫자는 run 문서 그대로. P = 부모, C = 자식.

```
[배달 기사]  LdrpLoadDependentModuleInternal(C 이름, P 지시서)                     (run23)
   새 C 지시서·명부 줄·카드 (도장 0) → 락 안에서 줄 P → C → +0x182c LdrpQueueWork(C 지시서)
     └ 게시판 LdrpWorkQueue 맨 끝에 C 지시서를 붙임
       LdrpMapAndSnapWork ≠ 0 · 문 닫는 중 아님 → +0x82 TpPostWork  ── 일꾼 깨움
   P 체크판 +0x6c : C 는 아직 도장 0 → 안 줄임

        ┌────────── 누가 떼어 가나 ──────────┐
 (a) 책임자 직원                         (b) 일꾼 (LdrpMapAndSnapWork ≠ 0 일 때만 있음)
     LdrpDrainWorkQueue(1)                   TppWorkpExecuteCallback (복사본 반복)
       있음 → +0x187 LdrpProcessWork          또는 LdrpWorkCallback 반복
       없음·일하는 중 → +0x199 기다림          있음 → +0x2e0 / +0xb8 LdrpProcessWork(.., 0)
        └───────────────┬─────────────────────┘
                        ▼
  LdrpProcessWork(C 지시서)   C 도장 0 → 올리기
    +0x6a LdrpMapDllSearchPath → (지도) … → LdrpMapDllWithSectionHandle
       +0x26c LdrpProcessMappedModule: 락 안에서 C 도장 = 2 (Mapped)
          +0x1e2 LdrpSignalModuleMapped(C)
             P 마다: P 체크판 −1 → 0 이면 P 도장 = 4, +0x5e LdrpQueueWork(P 지시서)   ◀ P snap 일감
       +0x1c8 LdrpMapAndSnapDependency(C 지시서)   ← C 의 import 로 같은 바퀴 (손자)
    끝: WorkInProgress −1, 게시판 비고 값 1 이면 NtSetEvent(LdrpWorkCompleteEvent) → (a) 깨어남

  나중에 뗀 P 지시서:  LdrpProcessWork(P)   P 도장 4 ≠ 0 → +0x131 LdrpSnapModule(P)   (본문 Q50)
  [snap 뒤] LdrpCondenseGraph(노드): 고리 없으면 도장 5 → 6 만
```

- 모든 줄이 `[정적]` (코드 길) 이다.
- **실제로 어느 직원이 어느 지시서를 처리했는지는 아무도 안 봤다** (동적 Q47·Q23).
- `?` 로 남는 것: `LdrpMapDllSearchPath` → `LdrpMapDllWithSectionHandle` 사이 (지도 이름 사슬만, 본문 안 봄), `LdrpSnapModule` 안 (Q50), `LdrpDestroyNode` 본문.

### 2.2 한 줄씩 읽는 법

run 문서 "호출 지점" 표에서 셋을 골랐다. "누가 / 어디서 / 누구를 / 무엇을 위해" 로 읽는다.

#### 한 줄 읽기 ① — 일꾼 깨우기

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpQueueWork` | `+0x7d` | `+0x82` | **`TpPostWork(LdrpMapAndSnapWork)`** — `LdrpMapAndSnapWork ≠ 0` (`+0x59`) 이고 `PEB.Ldr.ShutdownInProgress == 0` (`+0x63 ~ +0x70`) 일 때만 | [정적] |

- **누가**: 게시판에 지시서를 붙이는 함수 `LdrpQueueWork`.
- **어디서**: 자기 시작에서 `+0x7d` 에서 부르고, 돌아오면 `+0x82`. 콜스택엔 `LdrpQueueWork+0x82` 로 찍힌다.
- **누구를**: 스레드 풀의 `TpPostWork`. 넘기는 값은 전역 `LdrpMapAndSnapWork` (일감 개체).
- **무엇을 위해**: 일꾼에게 "**일 왔어요**" 버튼을 누른다.
- **조건**: 두 줄을 먼저 본다. 일감 개체가 있어야 하고(`+0x59`), 가게가 문 닫는 중이 아니어야 한다(`+0x63 ~ +0x70`). 하나라도 아니면 버튼을 안 누르고 그냥 끝난다.

#### 한 줄 읽기 ② — 책임자가 직접 처리

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpDrainWorkQueue` | `+0x182` | `+0x187` | **`LdrpProcessWork(일감, DIL)`** — 큐에 일감이 있을 때. **이 스레드가 직접 처리**, 끝나면 `JMP +0x42` | [정적] |

- **누가**: 게시판 비우기 함수 `LdrpDrainWorkQueue`. 지금 이 함수를 돌고 있는 **그 직원**.
- **어디서**: `+0x182` 에서 부르고 `+0x187` 로 돌아온다.
- **누구를**: `LdrpProcessWork`. 인자 1 = 방금 뗀 지시서, 인자 2 = `DIL` (평소 0, 2.6).
- **무엇을 위해**: 게시판에서 뗀 지시서를 **자기 손으로** 처리한다. 다 하면 `JMP +0x42` 로 반복문 맨 앞에 돌아가 다음 지시서를 본다.
- 이 한 줄이 "일꾼이 없어도 일이 된다" 의 근거다 (2.4).

#### 한 줄 읽기 ③ — 호출이 아닌 줄과 짝으로

`LdrpSignalModuleMapped` 는 호출 바로 앞에 **메모리에 쓰는 줄**이 있다. 둘을 같이 읽는다.

| 자리 | 하는 일 | 표시 |
|---|---|---|
| `+0x40 ~ +0x45` | 부모 지시서 `+0x6c` 를 1 줄인다 | [정적] |
| `+0x52` | 부모 노드 `State = 4` (Snapping) | [정적] |
| `+0x59` / `+0x5e` | **`LdrpQueueWork(부모 문맥)`** — 줄인 값이 0 이고 부모 노드 `State ≠ 5` 일 때 | [정적] |

- **누가**: 방금 올라온 자식 건물의 "도착 알림" 함수.
- **어디서**: `+0x45` 에서 부모 체크판에 줄인 값을 쓰고, `+0x52` 에서 부모 카드에 "꽂는 중"(4) 도장을 찍고, `+0x59` 에서 부르고 `+0x5e` 로 돌아온다.
- **누구를**: 다시 `LdrpQueueWork`. 이번엔 **부모의** 지시서를 붙인다.
- **무엇을 위해**: 부모의 "전선 꽂아 주세요" 일감을 만든다.
- 쓰는 줄(`+0x45`, `+0x52`)은 call 이 아니라서 ret 가 없다. 그 명령 자리 하나만 적는다 (run 문서 "오프셋 읽는 법").

### 2.3 핵심 ① 게시판 — `LdrpQueueWork` 는 붙이기만 한다

#### 새 말 — 큐 (먼저 온 것부터 꺼내는 줄)

- **큐** = 맨 끝에 넣고 맨 앞에서 꺼내는 목록. 그래서 **먼저 넣은 것부터** 나온다 `[추측]` (일반적으로 알려진 설명).
- 비유: 게시판에 쪽지를 **오른쪽 끝에 붙이고**, 떼어 가는 사람은 **왼쪽 끝부터** 뗀다.

#### 게시판이 어디 있나 `[정적]`

- 게시판 = 전역 **`LdrpWorkQueue`** (`ntdll+0x1d26b0`). 목록 머리다.
- 게시판 열쇠 = 전역 **`LdrpWorkQueueLock`** (`ntdll+0x1d2680`, 임계 구역 0x28바이트).
- **붙이는 건 작업 지시서 자체**다. 지시서 `+0x40` 칸이 게시판에 거는 고리다.
  - 비유: 따로 쪽지를 쓰는 게 아니라 **지시서에 압정 구멍이 하나 있어서** 그대로 게시판에 꽂는다.
  - `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 이름이 없다. 그래서 "+0x40 = 큐 고리" 처럼 **오프셋 + 역할**로만 적는다 (run22·23 과 같다).

#### 하는 일 — 순서대로 `[정적]`

1. 지시서 상태(`*(ctx+0x28)`)가 이미 음수(실패)면 **아무것도 안 하고** 끝 (`+0x10 JL`).
2. 열쇠를 잡는다 (`+0x19` / `+0x1e`).
3. 지시서 `+0x40` 을 게시판 **꼬리**에 끼운다 (`+0x1e ~ +0x4d`).
   - 꼬리 이웃이 깨져 있으면 `+0x3a INT 0x29` (빠른 실패). 목록이 망가졌으면 그 자리에서 프로세스를 멈춰 버리는 안전장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
4. 열쇠를 놓는다 (`+0x54` / `+0x59`).
5. 조건 둘이 맞으면 일꾼 버튼 `+0x82 TpPostWork` (2.2 ①).

- 떼어 가는 쪽(`LdrpWorkCallback`·`Drain`)은 **머리**에서 뺀다 → **먼저 넣은 것부터** 처리된다 `[정적]`.
- 이 함수 안의 호출은 **셋뿐**이다 (열쇠 잡기·놓기·`TpPostWork`). `LdrpProcessWork` 를 부르지 않는다.
  - → **일감이 이 자리에서 바로 처리되는 일은 없다** `[정적]`.
  - `TpPostWork` 가 그 자리에서 콜백을 돌리지 않는다는 건 `[추측]` 이다 (본문 안 봄).
- 비유: 쪽지를 붙이고 버튼을 누르면 **할 일 끝.** 붙인 사람이 건물을 가지러 가지 않는다.

#### "병렬 로딩이 켜졌나" 깃발은 따로 없다 `[정적]`

여기가 이번 바퀴의 첫째 열쇠다.

- 일꾼을 깨울지는 **전역 `LdrpMapAndSnapWork`** (`ntdll+0x1d2640`) 가 0 이냐 하나로 정한다.
- **새 말 — 일감 개체 (`LdrpMapAndSnapWork`)**: 스레드 풀에 "이런 일이 오면 이 함수를 돌려 주세요" 하고 **미리 등록해 둔 것**의 손잡이다. `TpAllocWork` 가 만들고 `TpPostWork` 가 "지금 하나 왔다" 고 알린다 `[추측]` (일반적으로 알려진 설명 + 이름).
  - 비유: 일꾼 대기실 벽에 달아 둔 **호출 버튼.** 버튼이 아예 안 달려 있으면(0) 누를 수가 없다.
- 그러니 "일꾼이 있나" = "**호출 버튼이 달려 있나**" 다.

#### 호출 버튼은 누가 다나 `[정적]`

`refs` (`_refs/LdrpMapAndSnapWork.tsv`, 8줄) 에서:

| 하는 일 | 자리 |
|---|---|
| **단다** | `LdrpEnableParallelLoading +0xa9` (`TpAllocWork` 의 출력 자리). 이때 넘기는 콜백 `+0x9d` = **`LdrpWorkCallback`** 주소 |
| 뗀다 | `LdrpDetectDetour +0xd5` (`LdrpDetourExist` 가 켜지면 개체를 기다렸다 놓고 0), `LdrpCompleteProcessCloning +0x78` |
| 읽는다 | `LdrpQueueWork +0x59`·`+0x76`, `LdrpMinimalMapModule +0x296` (뒤의 것은 이번에 안 봄) |

`LdrpEnableParallelLoading` 이 **버튼을 안 다는 경우** `[정적]`:

1. 스레드 수를 정한다.
   - 인자(`ProcessParameters.LoaderThreads`, run19) = 0 이면 스레드 수 = **4**. 다만 `RtlGetSuiteMask` 비트 16 이 켜졌으면 0.
   - 인자 ≠ 0 이면 `min(인자, 16)`.
2. 풀(대기실)은 **늘** 만든다 (`+0x49 TpAllocPoolInternal`). 최대 스레드 = 스레드 수 − 1 (1 이하면 1, `+0x7f TpSetPoolMaxThreads`).
3. **스레드 수 ≤ 1 이거나 `LdrpDetourExist ≠ 0` 이면 `TpAllocWork` 를 안 부른다.**
   - → `LdrpMapAndSnapWork` 는 0 그대로 → 그 뒤 `LdrpQueueWork` 도 일꾼을 안 깨운다.

- 비유: 대기실은 늘 짓는다. 하지만 일꾼 정원이 1 명 이하이거나 "우회 장치" 가 발견됐으면 **호출 버튼은 안 단다.**
- test.exe 에서는? `[추측]` IFEO 를 따로 안 줬으면 `LoaderThreads` 는 0 일 것이다 → 스레드 수 4 → 최대 3 → 버튼이 달린다.
  - run5 `[관찰]` 의 ntdll 일꾼 2개와 어긋나지 않는다.
  - 실제 값은 동적 **Q47** (`ntdll+0xe572c` 에서 `r ecx`).

#### 그래서 — kernel32 준비 때는 일꾼을 안 깨운다 `[정적]`

1. 버튼을 다는 자리는 `LdrpInitializeProcess +0x1a47 LdrpEnableParallelLoading` 안이다.
2. kernel32 준비(`+0x19a2`)는 그보다 **앞**이다.
3. 버튼은 그 전엔 0 이다 (다는 곳이 거기뿐).
4. → kernel32 준비 때 붙는 쪽지에는 `LdrpQueueWork` 가 **버튼을 안 누른다.**
5. 그럼 그 쪽지는 누가 떼나? → 2.4 에서 답한다.

#### 게시판·열쇠·신호등을 처음 만드는 곳

- `LdrpInitializeProcess +0x12e3 LdrpInitParallelLoadingSupport` (결과서) `[정적]`:
  - 빈 게시판 (머리가 자기를 가리킴), `+0x3f RtlInitializeCriticalSectionEx` (열쇠),
  - `+0x43` 꼬리 점프 → `LdrpCreateLoaderEvents` → `NtCreateEvent` 두 번 (`+0x23`, `+0x46`).
- 이벤트 둘 다 **자동 리셋**(Synchronization), 처음엔 꺼짐 (결과서) `[정적]`.
  - **새 말 — 자동 리셋 이벤트**: 기다리던 직원 하나를 깨우면 **저절로 다시 꺼지는** 신호등으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- run19 해설 2.9 의 "연락망(신호등·열쇠)" 이 바로 이 게시판용 열쇠와 신호등 둘이었다.

### 2.4 핵심 ② 짐 들이기 책임자 — `LdrpDrainWorkQueue(0/1)`

#### 새 말 둘

- **짐 들이기 책임자** = TEB `SameTebFlags` 의 **`LoadOwner`** 비트(`0x1000`)를 켠 직원. 이 비트는 [run21 해설 2.11](run21-load-dll-internal-해설.md) 에서 "짐 들이기 주인 표시" 로 봤다. 이번에 **켜는 자리**가 이 함수 안(`+0xd3`)이란 걸 알았다 `[정적]`.
  - run 문서는 "로드 책임자" 라고 부른다. 같은 말이다.
- **`LdrpWorkInProgress`** = 지금 **손에 들고 처리 중인 지시서 수**를 세는 전역 칸 `[정적: 줄]`. 떼어 갈 때 +1 하고, `LdrpProcessWork` 끝에서 −1 한다. 책임자 자신의 몫 1 도 여기 들어간다 (아래).
  - 비유: 게시판 옆 **"작업 중" 카운터.**

#### 반복문 한 바퀴 `[정적]`

맨 앞 `+0x42`. 열쇠를 잡고 게시판 **머리**에서 하나 뗀다. 그다음 셋 중 하나:

| 상황 | 하는 일 |
|---|---|
| 지시서가 **있다** | `WorkInProgress++` → 열쇠 놓음 → **`+0x187 LdrpProcessWork(일감, DIL)`** → 맨 앞으로. **이 직원이 직접 처리** |
| 없고, `WorkInProgress ≠ 인자` | 열쇠 놓음 → **`+0x199 NtWaitForSingleObject(이벤트)`** (시간 제한 없음) → 맨 앞으로. **일꾼이 끝내길 기다림** |
| 없고, `WorkInProgress == 인자` | 끝낼 때 (아래 표) |

- **갈리는 조건은 "게시판에 지시서가 있나" 하나**다 `[정적]`.
- 비유: 책임자는 게시판에 쪽지가 있으면 **직접 떼서 한다.** 비었는데 일꾼이 아직 들고 일하는 중이면, "끝났다" **신호등**이 켜질 때까지 앞에서 기다린다.

#### 인자 0 과 1 은 두 가지만 다르다 `[정적]` (`+0x2f TEST`, `+0x3a CMOVZ`)

| | `Drain(0)` | `Drain(1)` |
|---|---|---|
| 기다리는 신호등 | `LdrpLoadCompleteEvent` (`ntdll+0x1d2620`) | `LdrpWorkCompleteEvent` (`ntdll+0x1d2660`) |
| 끝나는 조건 (게시판이 빈 상태에서, `+0x90`) | `LdrpWorkInProgress == 0` | `LdrpWorkInProgress == 1` 그리고 `LdrpRetryQueue` 가 빔 (`+0xeb`) |
| 끝낼 때 | `WorkInProgress = 1` (`+0xa3`), TEB `SameTebFlags |= 0x1000` (**`LoadOwner`**, `+0xd3`) | 같은 줄 |
| 뜻 `[추측]` | "다른 로드가 다 끝날 때까지 기다렸다가 내가 **책임자**가 된다" | "책임자인 내가 큐를 다 비우고, 일꾼도 다 끝날 때까지 돕는다" |

표를 단계로 풀면:

- **`Drain(0)`** — "책임자 자리 받기".
  1. "작업 중" 카운터가 **0** 이 될 때까지 (아무도 짐을 안 들이고 있을 때까지) 기다린다.
  2. 0 이 되면 카운터를 **1** 로 올린다. 이 1 이 **책임자 자신의 몫**이다.
  3. 자기 TEB 에 `LoadOwner` 를 켠다.
  4. 그 사이 게시판에 지시서가 있으면 그것도 직접 처리한다 (반복문은 같으니까).
- **`Drain(1)`** — "책임자로서 다 끝내기".
  1. 이미 책임자다. 카운터엔 내 몫 1 이 있다.
  2. 게시판의 지시서를 다 직접 처리한다.
  3. 게시판이 비었는데 카운터가 1 보다 크면 = **일꾼이 아직 들고 있다** → `LdrpWorkCompleteEvent` 신호등을 기다린다.
  4. 카운터가 **1** (나만 남음) 이고 재시도 게시판도 비었으면 끝.
- 뜻 칸은 run 문서도 `[추측]` 이다. 줄은 `[정적]` 이고, 그 줄에서 읽은 뜻이다.

#### 곁가지 셋 `[정적]`

- **재시도 게시판**: `Drain(1)` 끝 직전, `LdrpRetryQueue` 가 비어 있지 않으면 그걸 통째로 `LdrpWorkQueue` 로 옮기고 (`+0x100 ~ +0x134`) 다시 돈다. 재시도 게시판에 **넣는 쪽**은 이번에 안 봤다.
- **우회 장치가 있으면** (`LdrpDetourExist ≠ 0`, `+0x58`): `Drain(1)` 은 카운터를 세지 않고(`DIL = 1`) 처리, `Drain(0)` 은 게시판을 안 건드리고 책임자만 된다.
- **책임자 내려놓기** = `LdrpDropLastInProgressCount` (결과서): `LoadOwner` 를 끄고, 카운터 = 0, `+0x45` 꼬리 점프 `NtSetEvent(LdrpLoadCompleteEvent)`.
  - 이 신호등이 다른 직원의 `Drain(0)` 기다림을 깨운다. 둘이 짝이다.
- 신호등을 켜는 쪽 정리:

| 신호등 | 켜는 곳 | 깨우는 곳 |
|---|---|---|
| `LdrpWorkCompleteEvent` | `LdrpProcessWork +0x1c3` (꼬리 점프 `NtSetEvent`) | `Drain(1)` 의 `+0x199` 기다림 |
| `LdrpLoadCompleteEvent` | `LdrpDropLastInProgressCount +0x45` (꼬리 점프 `NtSetEvent`) | `Drain(0)` 의 기다림 |

#### 누가 0 / 1 로 부르나 (run 문서 표에서 대표)

| caller | call / ret | 인자 | 비고 |
|---|---|---|---|
| `LdrpInitializeProcess` | `+0x1302` / `+0x1307` | 0 | `+0x12e3 LdrpInitParallelLoadingSupport` 바로 뒤 |
| 〃 | `+0x19c1` / `+0x19c6` | 0 | kernel32 준비 `+0x19a2` 뒤, 병렬 켜기 `+0x1a47` 앞 |
| 〃 | `+0x1aac` / `+0x1ab1` | **1** | exe import `+0x1aa9` 바로 뒤 |
| 〃 | `+0x1b8c` / `+0x1b91` | 0 | `LdrpProcessInitContextRecord` 저장 뒤 |
| `LdrpLoadDllInternal` | `+0x185` / `+0x18a` | 0 | `LoadOwner` 가 이미 켜져 있으면 건너뜀 (`+0x16d ~ +0x174`) |
| 〃 | `+0x215` / `+0x21a` | **1** | `+0x210 LdrpProcessWork(ctx, 1)` 바로 뒤 |

- 모양이 보인다: **`(0)` 으로 책임자가 되고 → 짐을 들이고 → `(1)` 로 마무리** `[추측: 뜻]`.
- `LdrpLoadDllInternal +0x18a` 가 "이미 책임자면 건너뜀" 인 것도 이 그림과 맞는다. 책임자가 안쪽에서 또 책임자가 될 필요는 없다.
- `LdrpInitializeProcess` 안에서 "책임자 기간" 은 **세 번** 열리고 닫힌다 `[정적: 위치만]`:
  - `[+0x1307 ~ +0x1753]`, `[+0x19c6 ~ +0x1b40]`, `[+0x1b91 ~ +0x1be7]` (닫는 쪽 = `LdrpDropLastInProgressCount`, 지도).
  - 둘째 기간 안에 병렬 켜기 `+0x1a47`, exe import `+0x1aa9`, `Drain(1)` `+0x1ab1` 이 **다 들어 있다.**
- 나머지 caller 5곳 (`LdrpFindLoadedDll`, `LdrpInitializeImportRedirection`, `LdrpInitializeThread`, `LdrUnloadDll`, `RtlQueryInformationActivationContext`) 은 `.asm` 이 없어 인자를 못 봤다.

#### 그래서 — kernel32 준비 때는 혼자 비운다 `[정적: 코드상]`

2.3 끝의 질문 ("버튼이 없을 때 붙은 쪽지는 누가 떼나") 의 답이다. 단계로:

1. kernel32 준비는 병렬 켜기(`+0x1a47`) **앞**이다 → 호출 버튼 `LdrpMapAndSnapWork == 0` (2.3).
2. → 그때 `LdrpQueueWork` 는 쪽지를 붙이기만 하고 **아무도 안 부른다.**
3. kernel32 를 들이는 길에 `LdrpLoadDllInternal +0x21a Drain(1)` 이 **늘** 있다 (run21).
4. `Drain(1)` 은 게시판에 쪽지가 있으면 **자기 손으로** 뗀다 (`+0x187`).
5. 일꾼이 없으니 게시판이 비면 바로 끝날 것이다. 기다림(`+0x199`)까지 갈 일은 없어 보인다 `[추측]` — 카운터를 올리는 쪽(게시판에서 떼는 쪽)이 이 직원뿐이라서. 실제로 `ntdll+0xad594` 가 안 서는지는 동적 Q47.
6. → kernel32 준비 때 쌓인 쪽지(예: kernelbase snap 쪽지, run22)는 **같은 직원의 `Drain(1)` 이 혼자 비운다.**

- run19 의 `[추측]` "kernel32·kernelbase 는 일꾼 없이 메인 직원 혼자 들일 것" 이 **코드로 이어졌다.**
- 다만 kernelbase 가 실제로 그 쪽지 길을 탔는지는 그대로 `[추측]` 이다 (kernel32 import 표·KnownDll 여부는 실행 값, Q20).
- kernel32 지시서 자신은 `+0x30 == 0` (부모 없음) 이라 자기 끝맺음에서 `LdrpSnapModule` 을 **직접** 부른다 (run22).
- 결과서 말로: kernel32 준비 때와 exe import 때 **코드는 같다.** 다른 건 `LdrpMapAndSnapWork` 뿐이다. 켜기 앞이면 `Drain` 하는 직원 혼자, 켠 뒤면 일꾼과 나눠서 처리한다.

#### 비유로 정리

- 짐 들이기를 시작하는 직원은 먼저 **책임자 완장**을 찬다 (`Drain(0)`, 다른 사람이 들이는 중이면 끝날 때까지 기다린다).
- 짐을 들이고 나면 **게시판을 끝까지 비운다** (`Drain(1)`). 쪽지가 있으면 직접 하고, 비었는데 일꾼이 아직 들고 있으면 신호등 앞에서 기다린다.
- 일꾼 호출 버튼이 아직 안 달린 때(kernel32 준비)는 **혼자서** 비운다.

### 2.5 일꾼 쪽 — `LdrpWorkCallback` 과 그 복사본

#### `LdrpWorkCallback` 은 지도에 없다 — 주소로 건네지는 함수 `[정적]`

- 호출 참조가 0 이다. 누가 "부르는지" 지도에 화살표가 없다.
- `refs` 로 찾은 4곳:
  - `LdrpEnableParallelLoading +0x9d` — 주소를 `TpAllocWork` 에 넘김 (2.3 의 "버튼 달기" 때 "이 함수를 돌려 주세요" 로 적어 둔 것).
  - `TppWorkpExecuteCallback +0x19d` — 비교하려고 주소를 읽음.
  - 함수 밖 2곳 (`GuardCFFunctionTable` 등) — CFG 허용 목록으로 보인다 `[추측]`.
- → **주소로 넘겨지는 함수가 맞다** `[정적]`. [run11 해설](run11-allocate-thread-해설.md) 2.5 의 `PspUserThreadStartup` 과 같은 모양이다: 부르지 않고 **주소를 건넨다.** 지도가 이런 길을 못 잇는 건 이미 알려진 한계다 (결과서 "스크립트 문제").

#### 본문 = 반복문 `[정적]`

1. `+0x23` `LdrpDetourExist ≠ 0` 이면 끝.
2. 열쇠 → 게시판 **머리** 하나 떼기 → 있으면 카운터 +1, `+0x69 LdrpUpdateStatistics` → 열쇠 놓음.
3. 없으면 빌렸던 출입증을 돌려놓고(`+0x86`) **반환.**
4. 있으면 처음 한 번 **메인 직원 출입증**을 빌려 달고(`+0xd0`, `LdrpMainThreadToken ≠ 0` 일 때) **`+0xb8 LdrpProcessWork(일감, 0)`** → 1 로.

- 한 번에 하나씩, **게시판이 빌 때까지** 돈다. 기다림(`NtWait*`)은 **없다.**
- 비유: 일꾼은 버튼 소리에 나와서 게시판이 빌 때까지 하나씩 떼어 처리하고, 비면 **그냥 대기실로 돌아간다.** 신호등 앞에서 기다리는 건 책임자만 한다.
- **새 말 — 토큰 가장**: 다른 스레드의 보안 신분(토큰)을 잠시 빌려 쓰는 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 여기선 `LdrpThreadTokenSetImpersonationToken(LdrpMainThreadToken)` 이다.
  - 비유: 일꾼이 **메인 직원 출입증**을 빌려 달고 일한다. 그래야 메인 직원이 들어갈 수 있는 곳에 똑같이 들어갈 것이다 `[추측]`.
  - run21 해설 2.11 의 "메인 직원 표 달기"(`LdrpThreadTokenSetMainThreadToken`) 가 그 출입증을 걸어 두는 쪽으로 보인다 `[추측: 이름]`. 이유: run21 해설이 그 확인에 쓰자고 한 주소 `ntdll+0x1d2618` 이, 이번 결과서의 `LdrpMainThreadToken` 자리와 같다.

#### 실제 일꾼은 이 함수를 안 거칠 수 있다 `[정적: 코드 줄]`

여기는 헷갈리기 쉬워서 단계로 간다.

1. 일꾼 스레드는 풀 쪽 함수 `TppWorkpExecuteCallback` 에서 등록된 콜백을 돌린다.
2. 그런데 그 함수 `+0x1b4` 에 **비교 한 줄**이 있다: "등록된 콜백이 `LdrpWorkCallback` 주소와 같은가?"
3. **같으면** `LdrpWorkCallback` 을 부르지 않는다. 대신 **같은 반복문의 복사본**(`+0x1bd ~ +0x2db`)을 자기 안에서 돈다.
   - 복사본도 똑같다: `LdrpDetourExist` 확인, 열쇠, 떼기, 카운터 +1, `+0x212 LdrpUpdateStatistics`, `+0x233` 출입증 빌리기.
   - 그리고 **`+0x2e0 LdrpProcessWork`** 를 직접 부른다.
4. 다르면 `+0x4d0` 간접 호출 (보통의 콜백 부르기).

- [run23 해설 2.1](run23-load-dependent-해설.md) 의 **끼워 넣기**와 비슷한 모양이다. 매뉴얼을 따로 부르는 대신 **같은 쪽을 오려서 풀 담당 수첩에 붙여 둔** 셈이다.
- 그래서 `LdrpWorkCallback` 함수 자체는 실행 때 **안 불릴 수도 있다** `[추측]`.
- 일꾼 콜스택엔 `TppWorkpExecuteCallback+0x2e0` 이 보일 것이다 `[추측: 확인은 동적]`.
- 버스 탈 때 주의 (plan 하네스 후보 (24)): `LdrpWorkCallback` 입구(`ntdll+0xe9ef0`)에 멈춤점을 걸어도 **안 설 수 있다.** 복사본 쪽 `ntdll+0x19050` (`TppWorkpExecuteCallback +0x2e0`) 도 같이 건다.

### 2.6 쪽지 처리 — `LdrpProcessWork` 는 도장을 보고 하나만 한다

#### 한 일감 = 올리기 **또는** snap `[정적]`

`ctx+0x38`(명부 줄) → `DdagNode(+0x98)` → **`State(+0x38)`** 를 본다 (`+0x29 ~ +0x38`).

```
LdrpProcessWork(지시서, 센적없음)                     상태 *(ctx+0x28) 가 음수면 바로 끝
├─ 카드 도장 ≠ 0 → +0x131 LdrpSnapModule(ctx)          ← snap 일감 (부모 snap 등)
└─ 카드 도장 == 0 → 올리기 (위에서부터 고른다)
     ├─ LoadReason == 9   → +0x158 LdrpMapDllPatchImage
     ├─ ctx+0x20 비트 20  → +0x14e LdrpMapDllRetry
     ├─ ctx+0x20 비트 9   → +0x144 LdrpMapDllFullPath
     └─ 기본              → +0x6a  LdrpMapDllSearchPath
끝 (센적없음 == 0 일 때): 열쇠, WorkInProgress −1, 게시판 비고 값 1 이면 NtSetEvent(LdrpWorkCompleteEvent)
```

- 도장 **0** (`PlaceHolder`) 이면 **가져오기**. 이름으로는 "명부 줄·카드만 만들어 둔 자리표" 로 읽힌다 `[추측: 이름]`.
- 도장이 **0 이 아니면** `LdrpSnapModule` 만. **가져오지 않는다.**
- **한 호출 안에서 둘이 같이 일어나지 않는다** `[정적]`. 가져온 뒤의 전선 꽂기는 **다른 지시서**로 다시 온다 (2.7 의 `Signal`, run22 의 `MapAndSnap +0x4f7`).
- dll1·dll2 같은 맨 이름은 기본 길 `+0x6a LdrpMapDllSearchPath` 일 것이다 `[추측]` (flow.md).
- 비유: 지시서 한 장을 받으면 카드 도장부터 본다. "**아직 없음**"(0) 이면 건물을 가지러 가고, 그 밖이면(보통 "꽂는 중" 4) 전선을 꽂는다. 한 번에 둘 다 하지 않는다.

#### 같은 지시서가 두 번 게시판에 붙는 이유

여기서 그림 하나가 맞춰진다. dll1·dll2 처럼 **게시판으로 가져온 건물**의 지시서 얘기다 (2.10 이야기 그림).

- 첫 번째: 배달 기사가 "**가져와 주세요**" 로 붙인다 (`+0x182c`, 도장 0).
- 두 번째: 자식들이 다 왔을 때 누군가 도장을 4 로 바꾸고 "**전선 꽂아 주세요**" 로 다시 붙인다 (`Signal +0x5e` 또는 `MapAndSnap +0x4f7`).
- 게시판도, 처리 함수도 같다. **도장만 다르다.** 그래서 run23 해설 2.6 의 "쪽지 두 종류" 는 사실 **같은 지시서, 다른 도장**이었다.

#### 결과와 끝 `[정적]`

- 결과가 0 이상이거나 `0xc000022d` (이름 `STATUS_RETRY` 로 알려진 값 `[추측]`) 면 그냥 끝.
- 그 밖의 음수면 지시서 상태 칸에 쓴다 (`+0x13b`). `0xc0000135` 일 때만 로그·ETW 3개, 정적 import(`ProcessStaticImport`) 면 `+0x127 LdrpReportError`.
- **끝** (인자 2 == 0 일 때): 열쇠 → 카운터 −1 → **게시판이 비었고 줄인 값이 1 이면** (`+0x192`, `+0x1c8`) 열쇠 놓고 `NtSetEvent(LdrpWorkCompleteEvent)` (꼬리 점프 `+0x1c3`).
  - 남은 1 = **책임자 몫**이다 → "이제 나머지는 책임자 혼자" 라는 뜻으로 `Drain(1)` 의 기다림(`+0x199`)을 깨운다 `[정적]`.
  - 꼬리 점프라 ret 가 없다. 그래서 `NtSetEvent` 안에서 멈추면 콜스택에 `LdrpProcessWork` 가 안 보일 것이다 `[추측]` ([run17 해설](run17-ldr-init-entry-해설.md) 2.4 와 같은 원리).

#### 인자 2 — "센 적 없음" `[추측: 이름 없음]`

- 인자 2 = 1 이면 끝에서 카운터를 안 줄이고 신호도 안 한다 (`+0x15d`).
- 뜻은 "이 일감을 `WorkInProgress` 에 센 적 없다" 로 읽힌다 `[추측]`.
- 누가 무엇을 넘기나 (run 문서):

| caller (ret) | 인자 2 |
|---|---|
| `LdrpLoadDllInternal +0x210` | 1 |
| `LdrpLoadDependentModuleInternal +0x1cae` (run23) | 1 |
| `LdrpWorkCallback +0xb8` | 0 |
| `TppWorkpExecuteCallback +0x2e0` | 0 |
| `LdrpDrainWorkQueue +0x187` | `DIL` (= `LdrpDetourExist`, 평소 0) |

- 맞아떨어지는 그림 `[추측]`: 게시판에서 **뗄 때 카운터를 +1 한** 쪽(일꾼·`Drain`)은 0 을 넘겨 끝에서 −1 한다. 게시판을 안 거치고 **바로** 부르는 쪽(`LdrpLoadDllInternal`, 배달 기사 `+0x1cae`)은 센 적이 없으니 1 을 넘긴다. (줄은 `[정적]`, 이 짝 맞추기는 내 정리다. 결과서도 `LdrpLoadDllInternal` 쪽을 "자기 스레드가 책임자라서 센 적 없음" 으로 읽었다.)

### 2.7 핵심 ③ `LdrpSignalModuleMapped` — 체크판을 지우는 셋째 손

#### run22 부터 비어 있던 칸

- run22: 건물 담당은 자기 몫 1 만 지운다. 남은 수가 0 이 아니면 **아무것도 안 하고** 끝난다. "그 몫을 누가 이어받나" `?`.
- run23: 배달 기사는 "새 줄이 안 생겼거나 자식이 이미 `State ≥ 2`" 일 때만 지운다. **새로 이은, 아직 안 온 자식**은 지우지 않고 남긴다. "도착했을 때 누가 지우나" `?`.
- **답: 이 함수다** (`+0x40 ~ +0x59`) `[정적]`.

#### 언제 불리나 `[정적]`

- 설치 담당 쪽 `LdrpProcessMappedModule` 이 `LdrpModuleDatatableLock` 독점 안에서:
  1. `+0x1bb` 열쇠 잡음
  2. **`+0x1c2` 카드 도장 = 2 (Mapped — 건물이 땅에 놓였다)**
  3. 바로 뒤 **`+0x1e2 LdrpSignalModuleMapped(명부 줄)`**
  4. `+0x1ee` 열쇠 놓음
- 조건 둘: 인자 2 비트 23(`0x800000`) 꺼짐, 명부 줄 `LoadContext(+0xb0) ≠ 0`.
  - 비트 23 이 `LdrpMapAndSnapDependency` 가 보는 `ctx+0x20 & 0x800000` 과 같은 것인지는 `[추측]`.
- `LdrpProcessMappedModule` 의 caller: `LdrpMapDllWithSectionHandle +0x26c`, `LdrpInitializeProcess +0x14ee`·`+0x167e` (`_calls.tsv`).
- 이름 그대로 "**Mapped 가 됐다고 부모들에게 알린다**" (결과서).

#### 하는 일 — 단계별로 `[정적]`

인자 = **방금 도장 2 를 받은 자식**의 명부 줄.

1. 자식 카드 뒷면 `IncomingDependencies(+0x30)` 가 비었으면 (나를 기다리는 부모가 없으면) 바로 끝 (`+0x18`, `+0x24 RET`).
2. 뒷면 목록을 한 바퀴 돈다 (`+0x26 ~ +0x63`). 칸마다:
   - 칸 `+0x18` (run23 의 줄 칸에 적힌 부모 노드, 아래 비트 지움) = **부모 카드**
   - 부모 카드의 첫 모듈 → 그 명부 줄의 **`LoadContext`** = 부모 지시서 (`+0x37`). 0 이면 이 부모는 건너뜀.
3. **`+0x40 ~ +0x45` 부모 지시서 `+0x6c` 를 1 줄인다.**
4. 부모 카드가 이미 도장 5(Snapped) 면 건너뜀. 줄인 값이 0 이 아니어도 건너뜀.
5. 0 이 됐으면 **`+0x52` 부모 도장 = 4 (Snapping)**, **`+0x5e LdrpQueueWork(부모 지시서)`**.

- 넣은 부모 지시서는 도장이 4 라서 `LdrpProcessWork` 에서 **snap 일감**이 된다 (2.6).
- 비유: 건물이 땅에 놓이자마자, 그 건물에 **줄로 이어진 부모 카드들을 하나씩 찾아가** 체크판을 하나씩 지운다. 체크판이 0 이 된 부모에겐 "꽂는 중" 도장을 찍고 "**전선 꽂아 주세요**" 지시서를 게시판에 붙인다.
- run23 이 그은 줄(간선)이 **여기서 처음 "읽힌다."** 줄은 부모 → 자식 방향으로 그었지만, 자식 쪽 뒷면 목록으로 거꾸로 따라가 부모를 찾는다.

#### 체크판을 줄이는 곳은 이제 셋 `[정적]`

| 자리 | 언제 | 근거 |
|---|---|---|
| `LdrpMapAndSnapDependency +0x461 ~ +0x47d` | 고리 끝, **자기 몫 1**. 0 이면 부모 없음 → `+0x4c2 LdrpSnapModule` 직접 / 부모 있음 → `+0x4f7 LdrpQueueWork` | run22 |
| `LdrpLoadDependentModuleInternal +0x12f7` | 새 줄이 안 생겼거나 자식이 이미 `State ≥ 2` | run23 |
| **`LdrpSignalModuleMapped +0x45`** | 새로 이은 자식이 **나중에** Mapped 가 될 때 | **run24** |

- 셋이 합쳐서 "자식 수 + 1" 을 0 으로 만든다.
- **마지막으로 줄이는 쪽이 부모 snap 을 시작한다** `[정적]`. 누가 마지막이 되든 snap 은 **한 번**이다.
- 이걸 보면 run22 해설 2.6 의 `[추측]` "**+1** 을 두는 건 도중에 0 이 되어 '끝' 으로 보이는 걸 막으려고" 와도 맞아 보인다. 자식들이 아무리 빨리 와도, 건물 담당이 자기 몫 1 을 놓기 전엔 0 이 안 된다. (이유가 코드에 쓰인 건 아니어서 여전히 `[추측]`.)

#### 헷갈리기 쉬운 것 둘

**(가) "자식이 다 끝나면" 이 아니라 "자식이 Mapped 가 되면" 이다** `[정적]`

- 부모 체크판은 자식이 **땅에 놓이는 순간**(도장 2) 지운다. 자식의 전선 꽂기(snap)까지 기다리지 않는다.
- 손자(자식의 import)는? 자식 자신의 `LdrpMapAndSnapDependency` 가 **자식 체크판**으로 따로 센다.
- 비유: 부모 체크판은 "**짐이 집 앞에 도착했나**" 만 센다. 그 짐의 전선이 다 꽂혔는지는 그 짐 자신의 체크판 몫이다.

**(나) 열쇠 두 개가 겹친다** `[정적]`

- 이 함수는 열쇠를 따로 안 잡는다. caller 가 `LdrpModuleDatatableLock` 독점을 잡은 채라 그 안이다.
- 그 안에서 `LdrpQueueWork` 가 게시판 열쇠 `LdrpWorkQueueLock` 을 잡는다.
- 순서 = **명부실 열쇠 → 게시판 열쇠.** 이 순서가 2.8 정정의 근거가 된다.

### 2.8 정정 — run23 해설의 `[추측]` 하나가 틀렸다

#### 무엇이었나

[run23 해설 2.6](run23-load-dependent-해설.md) 끝 "병렬 로딩이 켜졌는지는 안 본다" 의 2번:

- "배달 기사는 쪽지를 붙인 **뒤** 자식 `State` 를 `+0x12df ~` 에서 읽는다. 그 자리에서 다 올렸으면 `State ≥ 2`, 미뤘으면 `< 2` → 부모 체크판이 달라진다. 병렬 켜기 앞뒤 차이가 여기 나타날 수 있다 `[추측]`."
- run23 해설 2.8 의 표 "`State ≥ 2` (그 자리에서 올라옴) → exe 체크판 1 → 끝맺음 0 → `+0x4c2`" 도 같은 짐작 위에 있었다.

이게 **틀렸다** (run 문서 "정정"). run23 해설 맨 위에도 안내 한 줄을 넣었다 (본문은 그대로 둠).

#### 왜 틀렸나 — 단계별로

1. **`LdrpQueueWork` 는 붙이기만 한다** `[정적]` (2.3). 안에서 `LdrpProcessWork` 를 안 부른다. 그러니 "그 자리에서 다 올리는" 길이 **이 함수 안엔 없다.**
2. "그럼 그 사이에 **일꾼이** 재빨리 올려 버리면?" — 이것도 안 된다. 이유:
   - 자식 도장 = 2 를 쓰는 곳은 `LdrpProcessMappedModule +0x1c2` 다. 그 줄은 **`LdrpModuleDatatableLock` 독점 안**이다 (2.7).
   - 배달 기사는 **같은 열쇠**를 `+0x182c` (쪽지 붙이기) 부터 `+0x12df ~ +0x12f7` (도장 읽기·체크판) 까지 **계속 쥐고 있다** (run23).
   - 열쇠가 하나뿐이니, 배달 기사가 쥐고 있는 동안 일꾼은 도장 2 를 **쓸 수가 없다** `[정적: 락 순서]`.
3. → 새로 쪽지에 넣은 자식은 `+0x12df` 에서 **늘 `State < 2`** 다.
4. → 부모 체크판은 그 자리에서 **안 줄어든다.** 병렬 켜기 앞이든 뒤든 **다르지 않다.**
5. 그 몫은 나중에 자식이 도착할 때 `LdrpSignalModuleMapped` 가 줄인다 (2.7).

- 비유: 배달 기사가 **명부실 열쇠를 쥔 채** 쪽지를 붙이고, 같은 열쇠를 쥔 채 카드 도장을 본다. 일꾼이 아무리 빨라도 "도착" 도장은 명부실 안에서만 찍을 수 있으니, 기사가 열쇠를 놓기 전엔 찍을 수 없다.

#### 그럼 run23 의 `+0x12f7` "`State ≥ 2` 면 −1" 은 언제 타나

- run 문서 이야기 그림 표시: "run23 '≥ 2 면 −1' 은 **이 길엔 안 나온다.**"
- "이 길" = 새 지시서를 만들어 게시판에 붙인 자식의 길이다. 그 자식은 늘 `< 2` 이니 이 조건에 안 걸린다.
- `State ≥ 2` 조건이 실제로 쓰이는 건 다른 길(예: 이미 올라와 있던 건물에 줄을 그을 때)일 것이다 `[추측]`. run 문서가 따로 적지 않아서 여기선 더 말하지 않는다.

#### 남는 것

- "병렬 켜기 앞뒤 차이는 어디서 나나" 에 대한 답은 이제 하나다: `LdrpQueueWork +0x59` 의 `LdrpMapAndSnapWork ≠ 0` — **일꾼을 깨우나** (flow.md 2.6 "맞았다").
- flow.md 3장의 같은 `[추측]` 에도 `(틀림 → run24)` 가 붙었다.

### 2.9 핵심 ④ 그래프 정리 — 서로 기대는 고리를 하나로

#### 새 말 — 고리 (서로 기대는 묶음)

- 연결 카드 사이 줄을 따라가다 보면 "A 는 B 에 기대고, B 는 다시 A 에 기대는" **동그라미**가 생길 수 있다.
- **정리 = 그 동그라미를 카드 하나로 합치기** `[정적: 코드 모양]`.
- 비유 (run 문서): 가계도를 그리다가 동그라미가 보이면 둘을 **한 칸("한 집")** 으로 묶는다. 그래야 "누구부터 켤까" 순서를 정할 수 있다.
- 번호 매기기 + 가장 낮은 번호 + 스택을 쓰는 모양이 **Tarjan 알고리즘**과 같다 (알고리즘 이름은 `[추측]`).
  - Tarjan 알고리즘 = 그래프에서 "서로 오갈 수 있는 묶음" 을 한 번 훑어서 찾는 방법으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

#### 쓰는 카드 칸 (PDB `_LDR_DDAG_NODE`) `[정적]`

| 칸 | 이름 | 이번에 하는 일 |
|---|---|---|
| `+0x48` | `PreorderNumber` | 방문 순서 번호 |
| `+0x20` | `LowestLink` | 내가 닿을 수 있는 가장 낮은 번호 |
| `+0x40` | `CondenseLink` | 스택에 거는 고리 |
| `+0x28` | `Dependencies` | 카드 앞면 (내가 기대는 쪽) |
| `+0x38` | `State` | 진행 도장: 5 Snapped → **6 Condensed**, 합쳐지면 **−5 Merged**, 오류 **−3 SnapError** |

#### 순서 — 단계별로 `[정적]`

```
LdrpCondenseGraph(노드)                              도장 < 6 일 때만 (+0xb)
└ +0x26 LdrpCondenseGraphRecurse(노드, &번호, &스택)
    ├ 번호 +1 → PreorderNumber·LowestLink 에 씀, 스택에 올림
    ├ 앞면 줄마다 자식 (도장 > 5 면 건너뜀)
    │    ├ 번호 없는 자식 → +0x13d 자기 재귀 → LowestLink = min
    │    ├ 이미 스택에 있는 자식 → LowestLink = min(내 것, 자식 번호)
    │    └ 자식 도장 −3 → 나도 −3 (오류 전파)
    └ PreorderNumber == LowestLink (묶음의 뿌리) → 스택에서 내 것까지 꺼냄
         ├ 나 혼자 (+0x65): 합치지 않음. 도장 5 면 → 6
         └ 여럿: 명부실 열쇠 독점 (+0xcb) → +0xd8 LdrpMergeNodes(뿌리, 목록) → 열쇠 놓음 (+0xe4)
                 → 남은 카드마다 +0x102 LdrpDestroyNode → 뿌리 도장 5 → 6
```

말로 풀면:

1. 카드에 방문 번호를 붙이고, "내가 닿는 가장 낮은 번호" 칸에도 같은 번호를 적는다. 그리고 쌓아 두는 더미(스택)에 올린다.
2. 앞면 줄을 따라 자식에게 간다. 처음 보는 자식이면 **같은 일을 자식에게** 한다 (재귀).
3. 돌아와서 "가장 낮은 번호" 를 자식 것과 비교해 작은 쪽을 갖는다.
   - 자식이 더 낮은 번호에 닿는다 = 자식이 나보다 먼저 방문한 카드로 **되돌아오는 줄**이 있다 = 동그라미.
4. 다 보고 나서 "내 번호 == 가장 낮은 번호" 면, 나는 **묶음의 뿌리**다. 더미에서 나까지 꺼낸다.
   - 꺼낸 게 **나 하나**면 동그라미가 없다 → 도장만 5 → 6.
   - **여럿**이면 동그라미 → 합친다.

#### `LdrpMergeNodes` — 합치기 (본문 요약 `[정적]`)

- 합쳐지는 카드의 첫 모듈을 뿌리 카드 `Modules` 꼬리로 옮기고, 명부 줄의 `DdagNode` 를 뿌리로 바꾼다.
- `LoadCount` 를 뿌리에 더한다. 합쳐진 카드는 도장 **−5 (Merged)**.
- 줄 칸의 끝점을 전부 뿌리로 바꾼 뒤, 뿌리 → 뿌리 칸(자기 고리)과 중복 칸을 빼서 `RtlFreeHeap` (`+0x1e9`·`+0x23c`·`+0x312`).
- 카드마다 모듈이 하나라고 가정한 모양이다 `[추측]`.
- `LdrpDestroyNode` 본문은 **안 봤다.**
- 비유: 동그라미 안 카드들의 명부 줄을 뿌리 카드 한 장 밑으로 모으고, 빈 카드엔 "**합쳐짐**" 도장을 찍는다. 카드끼리 이어 둔 줄은 뿌리로 옮겨 달고, 뿌리에서 뿌리로 가는 줄(이제 의미 없음)은 버린다.

#### 언제 불리나 `[정적]`

- `LdrpPrepareModuleForExecution +0x12f` — 노드 도장 5 일 때 (run21).
- `LdrpLoadDllInternal +0x262` — 명부 줄 `LoadContext ≠ 0` 일 때 (run21).
- 정리가 끝난 카드는 도장 6 이다. 그래서 다음에 `Prepare` 가 오면 정리를 건너뛴다 ([run21 해설](run21-load-dll-internal-해설.md) 2.6 표: 6 → "다음 단계로") `[정적]`.

#### test.exe → dll1 → dll2 — 아무것도 안 합친다 `[정적: 코드 모양]`

1. 줄은 exe → dll1, dll1 → dll2 뿐이다 `[소스]`. 되돌아오는 줄이 없다.
2. 그러니 카드마다 "내 번호 == 가장 낮은 번호" 이고, 더미 맨 위가 자기다.
3. → 전부 "**나 혼자**" 길 (`+0x65`).
4. → **`LdrpMergeNodes` 도 `LdrpDestroyNode` 도 안 부른다.** 카드마다 도장 5 → 6 만 한다.

- exe 쪽 자리는 `LdrpInitializeProcess +0x1af2 LdrpPrepareModuleForExecution(exe)` 의 `+0x12f` 일 것이다 (그때 exe 카드가 도장 5 라면, `[추측]`).
- **시스템 DLL 끼리** 고리가 있는지는 모른다 → 동적 (`ntdll+0xed9f4` 에 안 서는지).

### 2.10 test.exe → dll1 → dll2 이야기 — run22·23 그림의 `?` 칸 채우기

#### 먼저 — 전제 하나

- 아래는 **일꾼이 없다고 칠 때** (메인 직원 혼자, 게시판은 먼저 붙인 것부터) 코드대로 따라간 순서다 `[추측: 일꾼 없음]`.
- 일꾼이 있으면 (b) 쪽이 중간 지시서를 가져가 **순서가 섞일 수 있다.**
- 모듈 대응(`dll1`·`dll2`)은 소스 의존 관계에서 짐작 `[소스]`+`[추측]`.
- 오른쪽 `큐:` 는 그 줄 뒤 게시판에 붙어 있는 지시서들이다.

```
LdrpInitializeProcess
 +0x1aa9 LdrpMapAndSnapDependency(exe)            exe 체크판 = 1 + 1
   +0x34e 배달 기사("dll1.dll") → 줄 exe → dll1 → +0x182c QueueWork(dll1)     큐: [dll1 가져오기]
          dll1 도장 < 2 → exe 체크판 그대로 (2)
   자기 몫 −1 → 1 → snap 안 함 (+0x4c2 안 탐)
 +0x1ab1 LdrpDrainWorkQueue(1)
   (1) +0x187 ProcessWork(dll1)  도장 0 → +0x6a 가져오기 → … 설치 담당
         +0x26c ProcessMappedModule: dll1 도장 = 2
           +0x1e2 Signal(dll1) → exe 체크판 1 → 0 → exe 도장 = 4 → +0x5e QueueWork(exe)   큐: [exe 꽂기]
         +0x1c8 MapAndSnap(dll1)  dll1 체크판 = 1 + 1
           +0x34e 배달 기사("dll2.dll") → 줄 dll1 → dll2 → +0x182c QueueWork(dll2)   큐: [exe 꽂기, dll2 가져오기]
           자기 몫 −1 → 1 → 아무것도 안 함
   (2) +0x187 ProcessWork(exe)   도장 4 → +0x131 LdrpSnapModule(exe)
   (3) +0x187 ProcessWork(dll2)  도장 0 → 가져오기 → … 설치 담당
         +0x26c → +0x1e2 Signal(dll2) → dll1 체크판 1 → 0 → dll1 도장 = 4 → QueueWork(dll1)   큐: [dll1 꽂기]
         +0x1c8 MapAndSnap(dll2)  (dll2 의 import = ? 이미 올라온 것뿐이라면) → 0 → 도장 4
           +0x4f7 QueueWork(dll2)                                              큐: [dll1 꽂기, dll2 꽂기]
   (4) +0x187 ProcessWork(dll1)  도장 4 → +0x131 LdrpSnapModule(dll1)
   (5) +0x187 ProcessWork(dll2)  도장 4 → +0x131 LdrpSnapModule(dll2)
   게시판 빔, 카운터 == 1 → 반환
 +0x1af2 LdrpPrepareModuleForExecution(exe) → (exe 도장 5 라면) +0x12f LdrpCondenseGraph → 셋 다 5 → 6, 합치기 없음
 +0x1bbb LdrpInitializeGraphRecurse …  (2.7, run7·run8 [관찰])
```

#### 한 단락씩 풀면

1. **exe 건물 담당** — exe 체크판에 "자식 1 + 나 1 = 2". dll1 줄을 배달 기사에게 맡긴다.
2. **배달 기사(dll1)** — 줄을 긋고 "dll1 가져와 주세요" 지시서를 붙인다. dll1 은 아직 도장 0 이니 exe 체크판은 **그대로 2** (2.8).
3. **exe 건물 담당 끝맺음** — 자기 몫을 지워 1. 0 이 아니니 **전선 안 꽂고 끝.** 여기서 run22 그림의 `+0x4c2` 직접 꽂기는 **안 탄다** `[추측: 일꾼 없을 때]`.
4. **책임자가 게시판을 비운다** (`Drain(1)`):
   - (1) dll1 지시서: 도장 0 → 가져오기. 땅에 놓이자(도장 2) **도착 알림** → exe 체크판 1 → 0 → exe 도장 4, "exe 전선 꽂아 주세요" 붙임. 이어서 dll1 자신의 목록 → dll2 줄 → "dll2 가져와 주세요" 붙임. dll1 체크판은 1 남음.
   - (2) exe 지시서: 도장 4 → **exe 전선 꽂기.**
   - (3) dll2 지시서: 도장 0 → 가져오기. 도착 알림 → dll1 체크판 1 → 0 → "dll1 전선 꽂아 주세요". dll2 자신의 목록이 (이미 올라온 것뿐이라면) 금방 0 → 부모가 있으니 `+0x4f7` 로 "dll2 전선 꽂아 주세요".
   - (4) dll1 전선 꽂기, (5) dll2 전선 꽂기.
   - 게시판이 비고 카운터가 내 몫 1 뿐 → 돌아온다.
5. **exe 실행 준비** — (exe 도장이 5 라면) 그래프 정리. 셋 다 5 → 6, 합치기 없음.

#### 채운 `?` (run 문서 목록)

1. **일감을 누가 꺼내 올리나**: 책임자 직원이 `Drain` 안에서 직접(`+0x187`), 또는 `TpPostWork` 로 깬 일꾼 (`TppWorkpExecuteCallback +0x2e0` / `LdrpWorkCallback +0xb8`). `[정적]` — 실제 어느 쪽인지는 동적.
2. **자식이 올라온 뒤 부모 카운터를 줄이고 snap 으로 잇는 코드**: `LdrpSignalModuleMapped +0x40 ~ +0x5e`. `[정적]`
3. **`LdrpQueueWork` 가 돌아왔을 때 dll1 `State`**: 붙이기만 하니 아직 0 (`< 2`). 그래서 exe 카운터는 그 자리에서 안 줄어든다. `[정적: 락 순서]`
4. **올리기와 snap 의 연결**: 한 일감 = 둘 중 하나, `State` 로 갈림. `[정적]`
5. **kernel32·kernelbase (병렬 켜기 앞)**: 버튼이 0 이라 일꾼을 안 깨운다. 쌓인 지시서는 `LdrpLoadDllInternal +0x21a Drain(1)` 이 혼자 비운다. `[정적: 코드상]`
6. **그래프 정리**: 고리가 없으니 합치지 않는다. `[정적: 코드 모양]`

#### exe 의 전선 꽂기는 어디서? — 직접이 아니라 일감 (2) 일 수 있다 `[추측: 일꾼 없을 때]`

- run22 그림은 exe 를 `+0x4c2 LdrpSnapModule` **직접**으로 그렸다. 그건 "고리 끝에서 체크판이 0 일 때" 다.
- 일꾼이 없으면 그때 dll1 은 아직 안 왔으니 체크판은 1 → **직접이 아니라 일감 (2)** 로 간다.
- 일꾼이 있으면, 일꾼이 dll1 을 재빨리 올려 exe 의 고리 끝 자기 몫 줄이기(`+0x474`) **전에** dll1 이 Mapped 가 될 수도 있다. 그러면 `+0x4c2` 가 탈 수도 있다.
  - 2.8 과 헷갈리지 말 것 `[추측: 내 정리]`: 2.8 의 "끼어들 수 없다" 는 배달 기사가 열쇠를 쥔 **그 사이** 얘기다. 기사가 열쇠를 놓은 **뒤**, 건물 담당이 고리 끝(`+0x474`)에서 자기 몫을 지우기 전까지는 일꾼이 dll1 을 올릴 틈이 남는다. 그래서 두 문장은 서로 어긋나지 않는다.
- **어느 쪽이든 snap 은 딱 한 번**이다. 마지막으로 체크판을 지우는 쪽이 시작한다.
- 확인은 동적 Q23 (`MapAndSnap +0x4c2` = `ntdll+0x2155d` 가 서나).

#### run8 관찰과 맞나

- `[관찰]` run8: "`+0x1bb6` 에서 dll1·dll2 가 **이미** 올라와 있었다."
- 올리기는 전부 `+0x1ab1 Drain(1)` 이 돌아오기 **전에** 끝난다. `Drain(1)` 은 게시판이 비고 일꾼도 다 끝나야 돌아온다 `[정적]`.
- → **어긋나지 않는다.**

### 2.11 곁가지 — 짧게

#### 결과서와 run 문서가 다른 곳 — run 문서를 따랐다

- `LdrpCondenseGraphRecurse` 의 열쇠 잡기: 결과서는 "`+0xbf` / `+0xc6`" 로 적었다. run 문서가 `.asm` 으로 다시 봤다:
  - `+0xbf` 는 `LEA RCX,LdrpModuleDatatableLock` (인자 준비) 이고, call 은 `+0xc6`, ret 은 `+0xcb` 다 (`_calls.tsv` 137줄).
  - 이 글·flow.md 는 `+0xc6` / `+0xcb`.
- 결과서 질문 1 의 "`TpPostWork` 를 부르는 조건 (**세 개** 다 참일 때)" 는 목록에 둘만 있다. run 문서는 셋째를 `+0x10` 의 "지시서 상태가 음수가 아님" (아니면 게시판에도 안 붙이고 끝) 으로 읽었다. 이 글은 2.3 에서 그 줄을 "붙이기 전 검사" 로, 버튼 조건은 둘로 적었다.
- 결과서는 같은 호출을 call 자리로 적은 곳이 많다 (예: `Drain +0x182`, `Tpp +0x2db`). 이 글은 run 문서처럼 라벨에 ret (`+0x187`, `+0x2e0`) 를 쓴다.

#### 오프셋 대조 — 이번엔 스크립트로 `[정적]`

- 요청서가 "점프 목적지를 함수 오프셋으로 바꿀 때 다시 확인" 을 따로 부탁했다 (sast/16·18 에서 한 줄씩 어긋난 적이 있음).
- 결과서는 8개 함수의 점프 128개를 "절대 주소 − 함수 시작" 으로 스크립트 대조했다. 127개가 함수 안 명령과 맞고, 나머지 1개는 `LdrpProcessWork +0x1c3` 의 `NtSetEvent` 꼬리 점프(밖으로 나감)다.

#### 이번 8개 + 곁 함수 호출 줄에 `alias:` 로 나온 이름은 없다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만.

| 자리 | 전 (run23 뒤) | 후 (run24) |
|---|---|---|
| 기준 | ntdll.pdb (run17 ~ run23), 정적 근거 run9 ~ run23 | + **run24** |
| 두 그림 사이 설명 | run23 문단: 매핑 함수는 일감 상자 뒤 `?` | + "코드 길은 풀림, 스레드는 Q47" + 새 run24 문단: `?` 화살표를 지우고 `Drain(1)` → `LdrpProcessWork` 로 그림, `Signal` 상자 넣음, (1)~(5) 는 일꾼 없을 때 짐작, `CondenseGraph` 넣음, 호출 번호 `(10)` 부터 밀림 |
| 그림 2 상자 글 | `j1` 풀·일감, `jd4` "누가 도는지 모름", `jb2`·`jc1` "누가·어느 스레드 모름" | `j1` "일감 개체는 스레드 수 > 1 · Detour 없음일 때만", `jd4` "**꼬리에 넣기만**", `ja4`·`jb5` "일꾼이 없으면 여기선 안 탈 것" 메모 |
| 그림 2 새 `(정적)` 상자 | 없음 | `tp1` `TpPostWork`, `w2` 노란 메모(다른 스레드의 `TppWorkpExecuteCallback +0x2e0`), dll1·dll2 층 `ProcessMappedModule` → `SignalModuleMapped` → `QueueWork`(부모 snap), `jw1` `Drain(1)` 과 `jw2`~`jw11` (`ProcessWork` 다섯 번 + `MapDllSearchPath`·`SnapModule`), `cg1`~`cg4` (`CondenseGraph` → `Recurse` 재귀 둘) |
| 그림 2 화살표 | `jd4 → ? → jb2`, `jb2 → +0x1c8`, `c4 (10) +0x1af2 → j3` | `jd4 +0x82 → tp1`, `jb2 (1) +0x26c → … +0x5e`, `jb2 (2) +0x1c8`, **`c4 (10) +0x1ab1 → jw1`** 과 그 아래 (1)~(5), `c4 (11) +0x1af2 → j3 +0x12f → cg1 …`, 뒤 번호 (12)~(15) |
| 2.4 `LdrpInitializeProcess` 트리 | `+0x1a47`·`+0x1ab1` 한 줄 | `+0x1a47` "일감 개체는 스레드 수 > 1 일 때만", `+0x1ab1` "큐의 일감을 직접 처리, 비면 일꾼을 기다림" |
| 2.5 | run23 까지 | + `[정적]` 넷: `Drain(0)` 책임자 되기, `LdrpProcessWork` 갈림, **`Drain(1)` 이 kernel32 때 혼자 비움**, `CondenseGraph` 한 줄. kernel32 쪽 `?` 두 곳에 "**풀렸다 (코드상)**" |
| 2.6 제목 | "… 안쪽 한 바퀴 run23" | + "**일감 흐름 run24**". 머리 `[공백]` 그대로 |
| 2.6 본문 | `?` 칸 셋 | 새 소절 다섯 (`LdrpQueueWork`, `LdrpDrainWorkQueue`, 풀 일꾼, `LdrpProcessWork`, `LdrpSignalModuleMapped`) + "일감 한 바퀴" 그림, 바깥 고리 소절 "**찾았다: `LdrpSignalModuleMapped`**", 배달 기사 소절 "맞았다 … 넣기만", 이야기 그림을 `Drain` (1)~(5) 로 갱신, `?` 셋 → **(해결됨, 정적)**, exe snap 이 일감 (2) `[추측]` |
| 2.7 | 켜기의 틀 | + 새 소절 "켜기 전 그래프 정리" (`CondenseGraph` 트리, 고리 없으면 5 → 6) |
| 3장 일꾼 | run23 줄 | run23 의 `[추측]` 에 **(틀림 → run24)**, + 일꾼이 하는 일 `[정적]`, 버튼 조건, `LoaderThreads` 0 → 4 `[추측]`, kernel32 때 안 깨움 |
| 4장 공백 표 | 2.6 일감 줄 (Q12c) | 그 줄들 → **(해결됨, 정적)**. 새 줄 셋: 일감 흐름 실행 값 (Q47·Q23 멈춤점), 정적으로 남은 것 run24, 이름·뜻 못 정한 것 run24. `LdrpSnapModule` 줄(Q50)에 "일감 쪽 caller = `ProcessWork +0x131`" |
| 4.1 | `LdrpQueueWork → ? LdrpMapDllWithSectionHandle` | 그 줄 **(해결됨)**, 새 줄 여섯 (`TpPostWork`, `Drain(1)` (1)~(5), `MapDllSearchPath → ?`, `ProcessMappedModule → Signal → QueueWork`, `ProcessWork → SnapModule`, `Prepare → CondenseGraph`), 번호 (11)·(14) → (12)·(15) |
| 5장 | run23 까지 | run24 줄, 요청서 번호 표 run24 줄 |

태그로 보면 핵심 변화는 여섯이다:

- **"일감을 누가 꺼내 올리나"**: 지도 이름만 (`?`) → **`[정적]` 코드 길** (책임자 `Drain +0x187` / 일꾼). 어느 직원인지는 그대로 동적.
- **"자식이 올라온 뒤 부모 카운터를 줄이는 곳"**: "두 함수 어디에도 없다" (`?`) → **`[정적]` `LdrpSignalModuleMapped +0x45`**.
- **"`LdrpQueueWork` 가 돌아왔을 때 자식 `State`"**: `[추측]` "그 자리에서 올렸나에 따라 갈린다" (run23) → **`[정적: 락 순서]` 늘 `< 2`**. 짐작이 **틀림**.
- **"kernel32 준비 때 쪽지는 어디서 도나"**: `[추측]` "메인 혼자" (run19) / 모름 (run22) → **`[정적: 코드상]` 같은 직원의 `LdrpLoadDllInternal +0x21a Drain(1)`**.
- **"`+0x1ab1 Drain(1)`"**: `[추측]` "일감이 끝나길 기다림" → **`[정적]` 직접 처리 + 비면 기다림.**
- **"그래프 정리"**: callee 이름만 → **`[정적]` 고리 합치기**, test.exe 는 **`[정적: 코드 모양]` 안 합침**.

2.6 제목 머리 `[공백]` 은 **그대로**다. 일감 흐름 **코드**가 이어졌을 뿐, dll1·dll2 가 실제로 올라오는 순간은 아직 아무도 안 봤다.

그림 2 를 볼 때 주의:

- 새 상자는 **전부 `(정적)`** 이다. 관찰한 상자는 없다.
- `Drain(1)` 아래 **(1)~(5) 의 순서와 모듈 대응은 "일꾼이 없을 때" 의 짐작**이다 `[추측]`. 일꾼이 있으면 일부는 노란 메모(`w2`)의 다른 스레드가 돈다.
- `TpPostWork` 화살표는 첫 `LdrpQueueWork` 에만 그렸다. 다른 `LdrpQueueWork` 상자도 같은 조건으로 부른다.

### 큰 그림에서 어떻게 이어지나

- run20 = kernel32 주문서, run21 = 배달 창구, run22 = 목록을 도는 건물 담당, run23 = 한 줄을 맡은 배달 기사, **run24 = 게시판과 쪽지를 떼어 가는 사람들.**
- 이걸로 "**exe import → dll1·dll2 가 올라오고 전선이 꽂히기까지**" 가 코드로 **한 바퀴 닫혔다** `[정적]` (아래 요약 그림).
  - 남은 `?` 는 셋: `LdrpMapDllSearchPath` → 설치 담당 사이 (지도 이름만), `LdrpSnapModule` 안 (Q50), `LdrpDestroyNode`.
- 그다음은 run7·run8 에서 **버스를 타 본** 구간이다: `+0x1bbb LdrpInitializeGraphRecurse` 로 dll2 → dll1 을 켠다 (flow.md 2.7).
  - 도장으로 보면 이번에 **6 (Condensed)** 까지 왔다. 그 뒤 도장을 누가 어떻게 바꾸는지는 이번 범위 밖이다.
- **Q14** ("dll1·dll2 를 올리는 쪽") 는 **일부**다 (결과서 판단):
  - 코드 길은 끝까지 이어졌다: `MapAndSnap +0x34e` → `Internal +0x182c LdrpQueueWork` → `Drain +0x187`/일꾼 → `LdrpProcessWork +0x6a LdrpMapDllSearchPath` → … → `LdrpMapDllWithSectionHandle`.
  - **어느 직원이 어느 줄에서 올렸는지**는 실행 값(`LoaderThreads`, `LdrpMapAndSnapWork`)에 달려 동적 (Q23·Q47).
  - 합칠지는 사용자에게 묻기로 했다 (plan).
- **다음 구간** (`plan.md` "다음 항목"): **Q13 (단계 2)**. 그 전에 Q14 합칠지 사용자 결정, 하네스 묶음 검토.
- 버스를 탈 때(동적) 볼 것은 새 번호 없이 **Q47**(일꾼이 있나, 누가 `LdrpProcessWork` 를 도나, `Drain` 이 기다리나), **Q23**(지시서마다 가져오기·꽂기, `Signal` 체크판) 에 붙었다.

### Q12 세 바퀴 한 장으로 — run22 ~ run24

Q12 를 셋으로 나눠 봤으니, 마지막으로 **한 장**에 모은다. 위에서 아래가 코드 흐름 순서다. 칸마다 오른쪽이 근거 바퀴다. 오프셋은 ret (메모리에 쓰는 줄은 그 자리). 숫자는 각 run 문서 그대로.

```
┌─ ① 바깥 고리 ── 건물 담당 LdrpMapAndSnapDependency(부모 지시서) ──────────────── run22
│    import 목록을 세고, 체크판 +0x6c = 자식 수 + 1
│    import 한 줄마다 → +0x34e 배달 기사 (조건 없음)
│    고리 끝: 자기 몫 −1 (+0x474) → 0 이면 도장 4 (+0x4ac)
│              부모 없음 (+0x30 == 0) → +0x4c2 LdrpSnapModule 직접
│              부모 있음               → +0x4f7 LdrpQueueWork (자기 꽂기 일감)
│
├─ ② 안쪽 처리·간선 ── 배달 기사 LdrpLoadDependentModuleInternal(자식 이름, 부모 지시서) ── run23
│    이름 다듬기 (api-set +0x2ff 등) → 이미 있나 (+0x16ee)
│      ├ 있음 → +0x1513 LdrpIncrementModuleLoadCount
│      └ 없음 → 새 지시서·명부 줄·카드 (+0x115c, 도장 0) → KnownDll 시도 (+0x1792)
│    명부실 열쇠 독점 안:
│      줄 긋기 부모 → 자식 (+0xf69 / +0xf8b)
│      새 건물·KnownDll 아님·경로 직접 아님 → +0x182c LdrpQueueWork ("가져와 주세요")
│      새 줄 없음 또는 자식 도장 ≥ 2 → 부모 체크판 −1 (+0x12f7)
│
├─ ③ 일감 ── 게시판 LdrpWorkQueue ─────────────────────────────────────── run24
│    LdrpQueueWork: 꼬리에 붙이기만. 버튼 LdrpMapAndSnapWork ≠ 0 이면 +0x82 TpPostWork
│    떼어 가는 쪽: (a) 책임자 LdrpDrainWorkQueue +0x187   (b) 일꾼 TppWorkpExecuteCallback +0x2e0
│                                                            / LdrpWorkCallback +0xb8
│    LdrpProcessWork: 카드 도장 0 → 올리기 / ≠ 0 → snap  (한 번에 하나)
│
├─ ④ 올리기 ── 도장 0 ───────────────────────────────────── run24 (+ run21 지도, run22)
│    +0x6a LdrpMapDllSearchPath → ? (지도 이름 사슬) → 설치 담당 LdrpMapDllWithSectionHandle
│      +0x26c LdrpProcessMappedModule: 열쇠 안에서 도장 = 2 (+0x1c2)
│      +0x1c8 LdrpMapAndSnapDependency(자식 지시서) → 손자에 대해 ① 부터 다시
│
├─ ⑤ 신호 ── 도착 알림 ──────────────────────────────────────────────── run24
│    +0x1e2 LdrpSignalModuleMapped(자식): 부모마다 체크판 −1 (+0x45)
│      → 0 이면 부모 도장 4 (+0x52), +0x5e LdrpQueueWork(부모) ("전선 꽂아 주세요")  → ③ 으로
│    ※ 체크판을 줄이는 곳은 셋 (+0x474 / +0x12f7 / +0x45). 마지막 쪽이 snap 을 시작
│
├─ ⑥ snap ── 도장 ≠ 0 ──────────────────────────────────────────── run24 (본문 Q50)
│    LdrpProcessWork +0x131 LdrpSnapModule(지시서)   (IAT 쓰기·도장 5 쓰는 곳은 모름)
│    끝: 카운터 −1, 게시판 비고 1 이면 NtSetEvent → 책임자 Drain(1) 깨어남 → 다 비면 반환
│
└─ ⑦ 정리 ── LdrpPrepareModuleForExecution +0x12f → LdrpCondenseGraph ───── run21·run24
     고리가 있으면 +0xd8 LdrpMergeNodes 로 합치기, 없으면 도장 5 → 6 만
     test.exe → dll1 → dll2: 고리 없음 → 합치기 없음
                                      ↓
                  +0x1bbb LdrpInitializeGraphRecurse (dll2 → dll1 켜기)   run7·run8 [관찰]
```

이 그림을 한 문단으로 말하면:

- 건물 담당(①)이 목록을 돌며 줄마다 배달 기사(②)를 보낸다.
- 배달 기사는 줄을 긋고, 새 건물이면 게시판(③)에 "가져와 주세요" 지시서를 붙인다.
- 책임자나 일꾼이 지시서를 떼어 도장을 보고 가져온다(④). 가져온 건물은 **자기 목록으로 ① 부터 다시** 돈다.
- 건물이 땅에 놓이는 순간 부모 체크판을 지운다(⑤). 0 이 된 부모는 "전선 꽂아 주세요" 로 다시 게시판에.
- 그 지시서를 떼면 전선을 꽂는다(⑥).
- 다 꽂으면 카드 그림을 정리하고(⑦), 그다음 켜기로 간다.

칸별 태그: ① ② `[정적]` (run22·23), ③ ~ ⑥ `[정적]` (run24), ④ 의 `?` 칸은 지도 이름만, ⑦ test.exe 는 `[정적: 코드 모양]`. **실행으로 확인한 칸은 없다** (마지막 켜기 줄만 run7·run8 `[관찰]`).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 일꾼이 **실제로 있나** — `LoaderThreads` 값, 켠 뒤 `LdrpMapAndSnapWork` 가 0 이 아닌가 | 실행 때 값이다 (IFEO·스위트 비트·`LdrpDetourExist`) | 동적 **Q47**: `ntdll+0xe572c` (`LdrpEnableParallelLoading` 입구) `r ecx`, `ntdll+0xe580e` (`+0xe2` ret) 뒤 `dq ntdll+0x1d2640 L1`. `TpAllocWork` 를 안 타면 `ntdll+0xe5809` 가 안 선다 |
| 일꾼을 **깨우나**, 누가 `LdrpProcessWork` 를 도나, `Drain(1)` 이 기다리나 | 실행 때 갈림 | 동적 **Q47**: `ntdll+0xe24c9` (`LdrpQueueWork +0x7d` call, 서면 일꾼을 깨우는 것), `ntdll+0x17140` (`LdrpProcessWork` 입구) 에서 `~`·`k`·`r dl`, `ntdll+0xad400` (`Drain` 입구) `r ecx`, `ntdll+0xad594` (`+0x194` call, 서면 메인이 기다린 것) |
| `LdrpWorkCallback` 이 실제로 불리나 | 복사본 길이 있어 정적으로 못 정한다 | 동적 **Q47**: `ntdll+0xe9ef0` 은 **안 서도 이상하지 않다.** 대신 `ntdll+0x19050` (`TppWorkpExecuteCallback +0x2e0`) |
| dll1·dll2 지시서마다 가져오기·꽂기 어느 쪽이었나, 부모 체크판 값, exe snap 이 직접(`+0x4c2`)인가 일감인가 | 실행 때 값 (일꾼 유무로 순서가 바뀜) | 동적 **Q23**: `ntdll+0x171a5` (`ProcessWork +0x65`, 가져오기 call) vs `ntdll+0x1726c` (`+0x12c`, snap call), 그때 `dS poi(@rbx+0x38)+0x58`. `ntdll+0xe2420` (`Signal +0x40`) `dd @rcx+0x6c L1`, `ntdll+0xe2439` (`+0x59` call). exe snap `MapAndSnap +0x4c2` (`ntdll+0x2155d`, call `+0x4bd`) |
| 시스템 DLL 끼리 고리가 있어 합치기가 일어나나 | 실행 때 그래프 | 동적 **Q23**: `ntdll+0xed9f4` (`LdrpMergeNodes` 입구) — 안 서는 게 정상일 것 |
| exe 카드가 `Prepare +0x12f` 때 정말 도장 5 인가 | 실행 때 값 | flow.md 4.1 (`ntdll+0xe9164` 입구에서 `dd @rcx+0x38 L1`) |
| `LdrpSnapModule` 안 — IAT 에 주소를 쓰는 곳, 도장 5 를 쓰는 곳, `+0x5df` 갈래 | 본문을 안 읽었다 | 정적 **Q50** |
| `LdrpMapDllSearchPath` → `LdrpMapDllWithSectionHandle` 사이 | 지도 이름 사슬(`LdrpMapDllNtFileName`)만, 본문 안 봄 | 정적, 대기열 줄 없음 (flow.md 4장 "정적으로 남은 것 run24") |
| `LdrpDestroyNode` 본문, 재시도 게시판(`LdrpRetryQueue`)에 넣는 쪽, `Drain` caller 5곳의 인자, `LdrpMinimalMapModule +0x296` 이 버튼을 읽는 이유, `TpPostWork`·`TppWorkpExecuteCallback` 이 콜백을 어느 스레드에서 돌리나 | 이번 범위 밖 (callee 본문·`.asm` 없음) | 정적, 대기열 줄 없음 (같은 4장 줄) |
| dll2 의 import | dll2 import 표를 안 봤다 | 동적 **Q23** (flow.md 2.6 모르는 것) |
| 이름·뜻 못 정한 것: `LdrpProcessWork` 인자 2 ("센 적 없음" `[추측]`), 지시서 `+0x20` 비트 9·20, `LdrpProcessMappedModule` 인자 2 비트 23, `LdrpDetourExist` 가 켜지는 조건 (`LdrpDetectDetour` 본문), `RtlGetSuiteMask` 비트 16, `Drain(0)`/`Drain(1)` 의 뜻 (`[추측]`), 큐 칸 `ctx+0x40` 의 이름 | PDB 에 이름이 없거나(`_LDRP_LOAD_CONTEXT`), 줄에서 읽은 뜻이다 | flow.md 4장 "이름·뜻 못 정한 것 (run24)" (대기열 줄 없음) |
| Q14 를 이번에 합칠까 | 코드 길은 끝까지 이어졌지만 "어느 직원" 은 동적 → 판단이 "일부" | 사용자 결정 (plan "다음 항목") |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run24-work-queue.md`](run24-work-queue.md) — 특히 "호출 지점" 표, "일감 큐", "꺼내는 쪽 ①·②", "일감 처리", "부모 카운터를 줄이고 snap 으로 잇는 곳", "그래프 정리", "일감 한 바퀴 그림", "test.exe → dll1 → dll2 이야기", "정정", "아직 확인 필요"(멈춤점 목록), "도구 메모"
- 정적 요청서: [`_workspace/sast/19_work-queue.md`](../../_workspace/sast/19_work-queue.md) — "오프셋은 call·ret 둘 다, 점프 목적지 다시 확인" 부탁
- 정적 결과서 (질문 1 ~ 7, 한 바퀴 그림, 그림 2 상자 후보, Q14 판단): [`_workspace/sast/19_work-queue_result.md`](../../_workspace/sast/19_work-queue_result.md)
- 앞 바퀴 해설:
  - [`run23-load-dependent-해설.md`](run23-load-dependent-해설.md) — 2.3 줄(간선) 긋기, 2.6 쪽지 두 종류와 **이번에 틀린 `[추측]`**, 2.7 체크판, 2.8 이야기 그림
  - [`run22-map-snap-dep-해설.md`](run22-map-snap-dep-해설.md) — 2.4 부모/자식·간선, 2.5 같은 함수가 DLL 마다 다시, 2.6 체크판 "+1"·끝맺음 세 갈래·`+0x4f7`
  - [`run21-load-dll-internal-해설.md`](run21-load-dll-internal-해설.md) — 2.4 지도 이름 사슬·설치 담당, 2.6 진행 도장 표, 2.11 `LoadOwner`·게시판 앞뒤 세트
  - [`run19-proc-init-modules-해설.md`](run19-proc-init-modules-해설.md) — 2.9 일꾼·게시판·연락망, 첫 자리 `+0x1a42`
  - [`run13-resume-thread-해설.md`](run13-resume-thread-해설.md) — 2.3 이벤트(신호등)
  - [`run11-allocate-thread-해설.md`](run11-allocate-thread-해설.md) — 2.5 주소를 건네는 함수
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "두 그림 사이 설명" run24 문단
  - "그림 2" `tp1`·`w2`, `jp1`·`js1`·`jq1`, `jp2`·`js2`·`jq2`, `jw1` ~ `jw11`, `cg1` ~ `cg4`
  - "2.5" run24 네 덩어리, "풀렸다 (코드상)"
  - "2.6" 새 소절 다섯 + "일감 한 바퀴", 이야기 그림, 모르는 것 (해결됨 셋)
  - "2.7" "켜기 전 그래프 정리"
  - "3" 일꾼 run24 줄들, "(틀림 → run24)"
  - "4. 공백 목록" 2.6 줄들, "4.1" run24 줄들
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q12c(끝, Q12 a·b·c 모두 끝), Q23·Q47·Q50 출처, Q14 "일부", 하네스 후보 (24)

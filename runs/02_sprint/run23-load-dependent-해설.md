# run23 load-dependent — 해설

> 이 글은 [run23 문서](run23-load-dependent.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **정정 (run24)**: 2.6 "병렬 로딩이 켜졌는지는 안 본다" 2번의 `[추측]` ("`LdrpQueueWork` 가 그 자리에서 다 올렸으면 `+0x12df` 에서 자식 `State ≥ 2` → 병렬 켜기 앞뒤 차이") 과 2.8 표의 "`State ≥ 2` (그 자리에서 올라옴)" 줄은 틀렸다. `LdrpQueueWork` 는 넣기만 하고 자식 `State = 2` 는 같은 잠금 안에서만 써지므로, 새로 일감에 넣은 자식은 `+0x12df` 에서 늘 `State < 2` 다 → [run24 해설](run24-work-queue-해설.md) 2.8. (본문은 그대로 둠)

> **정정 (run36)**: 2.7 "`0x103` 길은 다른 caller 몫으로 보인다 `[추측]`" 의 `LdrpSnapModule +0x5df` 쪽은 맞다 `[정적]` — snap 이 부를 땐 체크판이 이미 0 이라, forwarder 대상이 아직 `State < 2` 면 이 갈래가 타고 snap 은 0 으로 일단 끝난 뒤 같은 thunk 부터 다시 한다. `LdrpLoadDependentModuleW +0xae` 쪽은 그대로 `[추측]`. 본문은 그대로 두었다 → [run36 해설 2.7](run36-snap-module-해설.md#27-안내문-대상이-아직-안-왔으면--잠깐-멈췄다-이어서)

- 대기열: **Q12b** — 2.5·2.6 의존 DLL 하나를 처리하는 `ntdll!LdrpLoadDependentModuleInternal` (9250바이트, 호출 75곳): 간선(의존 관계)을 만드는 곳, 이미 있는 DLL·KnownDll·새로 올리기 갈림 (`_workspace/plan.md`, 이번에 **끝**)
- 근거:
  - run 문서 [`run23-load-dependent.md`](run23-load-dependent.md) (가장 중요)
  - 요청서 [`_workspace/sast/18_load-dependent.md`](../../_workspace/sast/18_load-dependent.md)
  - 결과서 [`_workspace/sast/18_load-dependent_result.md`](../../_workspace/sast/18_load-dependent_result.md) (판정: **분석성공**)
    - 결과서 오프셋 두 곳은 run 문서가 `.asm` 으로 고쳐 읽었다 (`+0x1c8e` → `+0x1de`, `+0x1345` → `+0x1341`). 이 글은 run 문서 값을 쓴다 (2.9).
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md`
    - Q12b 가 **끝났다.**
    - **새 항목 둘**:
      - **Q12c** — 일감 흐름(`LdrpQueueWork`·`LdrpProcessWork`·`LdrpDrainWorkQueue`) + 그래프 정리(`LdrpCondenseGraph` → `LdrpCondenseGraphRecurse` → `LdrpMergeNodes`). **다음 항목.**
      - **Q50** — 주소 연결 `LdrpSnapModule` 본문.
    - 출처를 더한 곳: **Q14·Q20·Q23·Q47**.
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll (`ntdll.dll`, 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`). run17 ~ run22 와 같은 파일이다.
  - 본문을 읽은 건 **한 함수**다: `LdrpLoadDependentModuleInternal` (rva `0x21ab0`, 9250바이트, 직접 호출 75줄 = 본체 74 + 예외 조각 `$fin$0` 1).
  - 그 안에서 부르는 함수(`LdrpLoadKnownDll`, `LdrpQueueWork`, `LdrpProcessWork`, `LdrpFindLoadedDllByName`, `ApiSetp*` 등)는 **본문을 안 읽었다.** 이름·인자·결과만.

이 글은 [run22 해설](run22-map-snap-dep-해설.md) 의 **다음 편**이다.

- run22 에서 건물 담당(`LdrpMapAndSnapDependency`)이 "기대는 건물 목록" 을 한 줄씩 읽고 **줄마다 배달 기사에게 맡긴다**는 걸 봤다.
- 그때 남긴 짐작이 하나 있었다: "연결 카드 사이에 **줄을 긋는 건 배달 기사 안**일 것 `[추측]`" ([run22 해설 2.4](run22-map-snap-dep-해설.md) 단계 4 ~ "그래서").
- 이번엔 그 **배달 기사의 본문을 처음 펼쳤다.**

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출, 비트와 플래그 → [run9 해설](run9-create-user-process-해설.md) 2절
  - 부르는 자리와 돌아올 자리 → [run9 해설](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)
  - 오프셋 순서 ≠ 실행 순서 → [run9 해설](run9-create-user-process-해설.md) 3.7
- PEB, 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
- 잠금 → [run11 해설 2.8](run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다)
- `\KnownDlls`(동네 공용 창고) → [run18 해설](run18-proc-init-setup-해설.md) 2.4 묶음 9
- 모듈 목록(PEB.Ldr)·모듈 항목(명부 줄) → [run18 해설 2.6](run18-proc-init-setup-해설.md#26-pebldr--건물-명부가-생기는-순간-0x1c1)
- 로드 문맥(작업 지시서), DDAG 노드(연결 카드) → [run19 해설 2.6](run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1)
- 활성화 컨텍스트 → [run19 해설](run19-proc-init-modules-해설.md) 2.11, [run22 해설](run22-map-snap-dep-해설.md) 2.1
- 병렬 로딩, 일감·게시판(`LdrpQueueWork`, `LdrpDrainWorkQueue`) → [run19 해설](run19-proc-init-modules-해설.md) 2.9
- 해시 표(이름 색인 카드함) → [run20 해설](run20-kernel32-load-해설.md) 2.3
- 두 `LdrLoadDll` 과 "순서 문제" (KERNEL32 를 먼저 주문했는데 KERNELBASE 가 따라 들어온다) → [run20 해설](run20-kernel32-load-해설.md) 2.4, [run21 해설](run21-load-dll-internal-해설.md) 2.5
- `0xc0000135` (`STATUS_DLL_NOT_FOUND`) → [run20 해설](run20-kernel32-load-해설.md) 2.12
- KnownDll 길·파일 길, 지도 이름 사슬, 설치 담당 `LdrpMapDllWithSectionHandle` → [run21 해설](run21-load-dll-internal-해설.md) 2.4
- 노드 상태 번호(진행 도장) → [run21 해설](run21-load-dll-internal-해설.md) 2.6, [run22 해설](run22-map-snap-dep-해설.md) 2.6
- 적재 이유 `LoadReason`(주문 사유), 패치 주문(이유 9) → [run21 해설](run21-load-dll-internal-해설.md) 2.10
- **간선**(연결 카드 사이 줄), 카드 앞면 `Dependencies`·뒷면 `IncomingDependencies` → [run22 해설](run22-map-snap-dep-해설.md) 2.4 단계 4
- 부모 / 자식 (import 하는 쪽 / 되는 쪽) → [run22 해설](run22-map-snap-dep-해설.md) 2.4 단계 5
- 남은 자식 수 `+0x6c` (체크판, "올릴 수 + 1") → [run22 해설](run22-map-snap-dep-해설.md) 2.6
- 명부 줄 + 연결 카드를 한 쌍으로 만드는 `LdrpAllocateModuleEntry` → [run22 해설](run22-map-snap-dep-해설.md) 2.7

비유도 그대로 이어 쓴다.

- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- DLL = **건물**, 모듈 항목 = **명부 한 줄**, `LdrpHashTable` = **이름 색인 카드함**
- DDAG 노드 = 명부 줄마다 붙은 **연결 카드**, 간선 = 카드 사이 **줄**, 로드 문맥 = **작업 지시서**
- import 목록 = 건물의 **"기대는 건물 목록"**, snap = 콘센트에 **전선 꽂기**
- `LdrpMapAndSnapDependency` = **건물 담당**, `LdrpLoadDependentModuleInternal` = **배달 기사**, `LdrpMapDllWithSectionHandle` = **설치 담당**
- `\KnownDlls` = **동네 공용 창고**, `LdrpQueueWork` = **게시판에 일감 쪽지 붙이기**
- 노드 `State` = 연결 카드의 **진행 도장**, 부모 지시서 `+0x6c` = **체크판**
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- run 문서는 이번에도 "**이삿짐 심부름꾼**" 비유를 썼다. 이 글은 그걸 가게 그림으로 옮긴다 (가리키는 건 같다).
  - 심부름꾼 = 배달 기사
  - 별명 → 진짜 이름 = ApiSet 풀이 (2.5)
  - 공용 창고 = `\KnownDlls`
  - "가져와 주세요" 쪽지 = 게시판 일감 쪽지
  - 장부에 "이 집은 이 짐에 기댄다" 한 줄 = 연결 카드 사이 줄 (간선)

한 줄로 먼저 말하면:

- `LdrpLoadDependentModuleInternal` 은 이런 함수다 `[정적]`:
  - import 이름 하나를 받아 → **진짜 이름으로 다듬고** → **이미 올라와 있으면** 그걸 쓰고 → 없으면 **새 명부 줄을 만들어 KnownDll 로 올려 보고** → 그래도 안 되면 **일감 쪽지**로 넘긴다.
  - 어느 쪽이든 끝에 **잠금을 잡고 부모 → 자식 줄을 한 가닥** 긋는다.
- 이번 바퀴의 **핵심 넷**:
  1. **줄(간선)을 긋는 곳을 찾았다.** `+0xf69` 부모 카드 `Dependencies`, `+0xf8b` 자식 카드 `IncomingDependencies`. 0x20바이트 칸 하나가 두 목록에 같이 걸린다. 잠금 안이다. run22 의 "바깥 고리엔 없다" 와 그대로 이어진다 → 2.3
  2. **갈림은 "이미 있음 vs 새로" 둘이다.** KnownDll 은 따로 있는 셋째 갈래가 아니라 **"새로" 안에서 먼저 시도**한다. 요청서와 run22 해설이 세운 틀이 여기서 틀렸다 → 2.4
  3. **ApiSet** — `api-ms-win-…` 같은 가상 이름을 진짜 이름으로 바꾸는 자리가 **이 함수 안**에 있다. run20·run21 의 "kernelbase 가 kernel32 를 따라 들어온다" 가 코드로 어느 길인지 한 칸 더 보인다 → 2.5
  4. **일감으로 넘기는 조건**: KnownDll 도 아니고 경로를 직접 준 이름(flags `0x400`)도 아닐 때. 병렬 로딩이 켜졌는지는 **안 본다.** 쪽지를 **누가 꺼내는지**는 다음 항목 Q12c → 2.6

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
        ├─ +0x19a2  LdrpInitializeKernel32Functions                                (run20)
        │     └─ +0xc0 LdrLoadDll("KERNEL32.DLL") → … LdrpLoadDllInternal           (run21)
        │           └─ … LdrpMapDllWithSectionHandle +0x1c8
        │                 └─ LdrpMapAndSnapDependency (kernel32 지시서)              (run22)
        │                       └─ +0x34e LdrpLoadDependentModuleInternal           ← 이번 (같은 함수, 2.5)
        ├─ +0x1a47  LdrpEnableParallelLoading                                       (run19)
        ├─ +0x1aa9  LdrpMapAndSnapDependency (exe 지시서)                            (run22)
        │     └─ +0x34e LdrpLoadDependentModuleInternal [dll1 추정]                 ← 이번 (run23)
        │           └─ +0x182c LdrpQueueWork → ? (누가 꺼내나)                       ← 다음 (Q12c)
        ├─ +0x1ab1  LdrpDrainWorkQueue(1)                                           (run19)
        ├─ +0x1af2  LdrpPrepareModuleForExecution (exe 항목)                         (run21)
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16, 뒤는 추측)
```

- 이번 자리는 여전히 **짐 들이기**와 **전원 켜기** 사이다.
- run22 가 "목록을 한 줄씩 도는 바깥 고리" 였다면, 이번은 그 고리가 **한 줄마다 부르는 안쪽 한 바퀴**다.
- 같은 함수가 kernel32 준비 때(위쪽 가지)도, exe import 때(아래쪽 가지)도 불린다 (run22). 이번에 읽은 코드는 **두 자리에 똑같이** 쓰인다 (2.6).
- flow.md 로는 **2.6** (`[공백]` import DLL 이 메모리에 올라오는 순간) 이 중심이고, **2.5** (kernelbase 가 따라 올라오는 길) 와 **3장** (일꾼) 에도 한 줄씩 더해졌다.

### 무엇이 궁금했나

run 문서 "목적" 그대로. run22 가 넘긴 질문 다섯 개다.

1. 연결 카드 사이 **줄(간선)** 을 긋는 줄이 여기 있나.
2. "이미 올라와 있음 / KnownDll / 새로 올리기" 가 **어떻게 갈리나**, 인자 5 칸에 무엇이 들어가나.
3. `LdrpQueueWork`(일감) 와 `LdrpProcessWork`(바로) 를 부르는 **조건**.
4. 부모 지시서 `+0x6c`(체크판) 를 줄이는 조건, `State = 3`·반환 `0x103` 의 뜻.
5. 인자 4(`LoadReason`)·인자 6(임시 포인터) 이 하는 일, 반환값.
6. 덤: 2.5 의 "kernel32 의 import 가 `api-ms-win-…` 같은 가상 이름이면 진짜 이름(KERNELBASE)으로 바뀐다" `[추측]` 의 자리.

### 바퀴 전에 알던 것

- `[정적]` (run22) 바깥 고리 `LdrpMapAndSnapDependency` 와 `LdrpAllocateModuleEntry` 엔 줄(간선)을 긋는 코드가 **없다.**
- `[추측]` (run22) 그러니 줄은 배달 기사 `LdrpLoadDependentModuleInternal` 안에서 그을 것.
- `[정적]` (run22) 배달 기사는 인자 6개를 받는다: 1 자식 이름, 2 **부모 지시서**, 3 **부모 명부 줄**, 4 이유 0, 5 자식 명부 줄을 돌려받는 칸 `&지시서.58[i]`, 6 임시 포인터 (`+0x451` 에서 놓음, 뜻은 `[추측]`).
- `[정적]` (run22) 배달 기사 `+0x1100 ~ +0x1108` 이 새 자식 지시서 `+0x30` 에 부모 명부 줄을 넣고, `+0x115c` 에서 `LdrpAllocateModuleEntry` 를 부른다. 그 갈래가 "**아직 안 올라온 DLL 일 때만**" 인지는 `[추측]`.
- `[정적: 지도]` (run21) 배달 기사 아래에 두 사슬이 있다: KnownDll 길 `+0x1792 LdrpLoadKnownDll → …`, 파일 길 `+0x1cae LdrpProcessWork → …`.
- `[관찰]` (run8) `+0x1bb6` 에서 멈췄을 땐 dll1·dll2 가 **이미** 올라와 있었다. 켜는 순서는 dll2 → dll1.
- `[소스]` test.exe 가 직접 import 하는 건 dll1 뿐, dll2 는 dll1 을 거쳐 붙는다.

---

## 2. 찾은 것

### 2.1 한눈에 — 갈림길 트리

이번 함수는 크다 (호출 75곳). 그래서 결과서와 run 문서는 **"어느 길로 끝나나"** 를 먼저 그렸다. 아래는 run 문서 트리에서 잡일을 줄인 것이다. 숫자는 run 문서 그대로, 오프셋은 **ret** (메모리에 쓰는 `MOV` 줄은 그 명령 자리).

먼저 알아 둘 것 하나 — **주소 순서가 뒤죽박죽이다.**

- 이 함수 안에는 이름 다듬기(`LdrpPreprocessDllName`)와 찾기·새 항목(`LdrpFindOrPrepareLoadingModule`) **모양의 코드가 끼워져 있다** `[정적]`. 로그 문자열이 그 두 이름으로 나온다 (`+0x8c1`, `+0x15aa`, `+0xde8` 의 `LdrpLogInternal` 인자).
  - `LdrpFindOrPrepareLoadingModule` 은 [run21 해설](run21-load-dll-internal-해설.md) 2.3 의 "찾거나 준비" 상자(kernel32 길)와 같은 이름이다.
- **새 말 — 끼워 넣기(인라인)**: 다른 함수를 부르는 대신, 그 함수 본문을 **통째로 복사해 붙여 넣는** 컴파일러 기법으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 그래서 호출 줄은 없고 로그 이름만 남는다.
  - 비유: 업무 매뉴얼 두 권을 따로 들고 다니는 대신, 필요한 쪽을 **오려서 배달 기사 수첩에 붙여 둔** 것.
- 붙인 쪽이 주소 여기저기에 흩어져 있어서, 아래 트리는 결과서가 **실행 순서로 다시 맞춘** 것이다 ([run9 해설](run9-create-user-process-해설.md) 3.7, 오프셋 순서 ≠ 실행 순서).

```
LdrpLoadDependentModuleInternal(이름, 부모문맥, 부모항목, 이유, &결과항목, &임시칸)
│
├─ [항상] 부모 항목의 활성화 컨텍스트를 이 스레드에 올림 (+0x71 ~ +0x1d3, 호출 없음)
├─ 이름 다듬기 (이유 9·enclave 면 건너뜀)
│     (a) ApiSet 풀이 +0x2ff  (b) SxS 리다이렉트 +0x142b  (c) 경로·확장자
│     └ 어느 단계든 음수 → 로그 → 맨 끝 (간선 없음)
├─ *임시칸 이 비었으면 0x20바이트 간선 칸 할당 (+0x92e), *결과항목 = 0
│
├─ 찾기
│    ├ 이유 == 9              : 안 찾음 → 상태 0xc0000135
│    ├ flags 0x20 (맨 이름)   : +0x16ee LdrpFindLoadedDllByName
│    ├ flags 0x200 (전체 이름) : 해시표 훑기 (락 공유 +0xb32 ~ +0xd1f)
│    └ 둘 다 아님             : 안 찾음 → 상태 0xc0000135
│
├─ ① 찾았다
│     노드 State < 0 → "순환 / 앞서 실패", 상태 0xc00000e5, 결과항목 = 0 → 맨 끝
│     아니면 +0x1513 LdrpIncrementModuleLoadCount, 상태 0 ───────────────┐
│                                                                        │
├─ ③ 못 찾았다 (0xc0000135)                                               │
│     새 로드 문맥 (+0x10db) → +0x115c LdrpAllocateModuleEntry             │
│     → *결과항목 = 새 항목 (+0x1164), LoadReason = 이유 (+0x1179)         │
│     ├ ② 이유 ≠ 9 : +0x1792 LdrpLoadKnownDll                              │
│     └ flags 0x400 이고 아직 0xc0000135 : +0x1cae LdrpProcessWork(문맥, 1) │
│                                                                        │
├─ [공통 꼬리: *결과항목 ≠ 0 이면 늘] ◀──────────────────────────────────┘
│     LdrpModuleDatatableLock 독점 (+0xe94 / 막히면 +0x1772)
│     ★ 간선 쓰기 (+0xf2c ~ +0xfbb)
│     ★ 상태 == 0xc0000135 이고 flags 0x400 아님 → +0x182c LdrpQueueWork   (락 안)
│     상태 ≥ 0 이면 부모 문맥 +0x6c 줄이기 / 기다림 표시
│     참조 하나 내려놓기 (+0x1310) → 마지막이었으면 정리
│     락 풀기 (+0x132f)
└─ 활성화 컨텍스트 내림 (+0x2357), 반환 (+0x2395 RET, eax = 상태)
```

- 트리 전체가 `[정적]` 이다. 실행 때 test.exe 가 **어느 갈래를 탔는지는 아무도 안 봤다.**
- 빠진 것: enclave 갈래(`+0x1be7 LdrpFindOrPrepareEnclaveModule`), `RtlRaiseException`(`+0x23f5`), 로그·이름 버퍼 잡일, 마지막 참조 정리 안쪽 호출들. 전체는 run 문서 "호출 지점" 표.

### 2.2 트리 한 줄씩 읽는 법

#### 기호부터

| 기호 | 뜻 |
|---|---|
| `├─` / `└─` | 다음 단계. 위에서 아래가 **실행 순서**. `└─` 는 그 층의 마지막 |
| `│` | 같은 층이 아래로 이어진다는 세로줄 (내용은 없다) |
| 들여쓰기 | 바로 위 줄 **안쪽**의 갈래 |
| `[항상]` | 조건 없이 탄다 |
| `①` `③` `②` | 갈래 이름표. 번호는 **요청서가 붙인 이름**이라 순서와 다르다. 실제 순서는 ① → ③ → ② (2.4) |
| `─┐` … `◀─┘` | 그 갈래가 끝나고 **공통 꼬리로 합류**한다 |
| `→ 맨 끝 (간선 없음)` | 공통 꼬리를 **건너뛰고** 바로 끝으로 간다 |
| `★` | 이번 바퀴의 핵심 줄 |
| `flags 0x20` 등 | 이 함수가 들고 다니는 지역 플래그. 비트 이름은 못 정했다. 역할만 적는다 (2.4) |

#### 한 줄 읽기 ① — 호출 줄

**`③ … +0x115c LdrpAllocateModuleEntry`** `[정적]`

- 누가: `LdrpLoadDependentModuleInternal` (배달 기사)
- 어디서: 부르는 자리 `+0x1157`, 돌아올 자리 `+0x115c`
- 누구를: `LdrpAllocateModuleEntry(새 문맥)` — 바로 앞 `+0x10db` 에서 힙에 만든 **새 작업 지시서**를 넘긴다.
- 무엇을 위해: 새 **명부 줄 + 연결 카드** 한 쌍을 만든다 ([run22 해설](run22-map-snap-dep-해설.md) 2.7). 돌려받은 명부 줄은 `+0x1164` 에서 인자 5 칸(결과항목)에 넣는다.
- 언제: 찾기 결과가 `0xc0000135`(못 찾음) 일 **때만** (`+0xd92 CMP → +0x10a3`).
- 비유: 색인 카드함에 없는 건물이다 → 새 지시서를 쓰고, 명부에 새 줄과 연결 카드를 만든다.

#### 한 줄 읽기 ② — 호출 줄, 잠금 안

**`★ … +0x182c LdrpQueueWork`** `[정적]`

- 누가: 배달 기사
- 어디서: 부르는 자리 `+0x1827`, 돌아올 자리 `+0x182c`. 공통 꼬리 안, **잠금(`LdrpModuleDatatableLock` 독점)을 잡은 채**.
- 누구를: `LdrpQueueWork(항목.LoadContext)` — 자식 명부 줄에 묶인 작업 지시서.
- 무엇을 위해: 이 자식 건물을 **"가져와 주세요" 쪽지로 게시판에** 넘긴다.
- 언제: 상태가 아직 `0xc0000135`(새로 만들었는데 KnownDll 도 아님) 이고 flags `0x400` 이 꺼졌을 때 (2.6).

#### 한 줄 읽기 ③ — 호출이 아닌 줄 (메모리에 쓰기)

**`★ 간선 쓰기`** 안의 한 줄, run 문서 표에서:

```
+0xf69  MOV [부모노드+0x28],칸
```

- 이건 호출이 아니다. **메모리 한 칸에 값을 쓰는 명령**이다. 자리도 하나뿐이다 (call/ret 이 없다).
- `MOV [A],B` = "A 가 가리키는 칸에 B 를 쓴다".
- 풀어 읽으면: "부모 연결 카드의 `+0x28` 칸(PDB 이름 `Dependencies`)에 **간선 칸 주소**를 쓴다".
- 무엇을 위해: 부모 카드 앞면 "**내가 기대는 건물**" 목록에 줄 한 가닥을 매단다 (2.3).

### 2.3 핵심 ① 줄(간선)은 여기서 긋는다

#### run22 에서 이어지는 이야기

[run22 해설](run22-map-snap-dep-해설.md) 2.4 의 단계를 다시 쓰면:

1. 바깥 고리는 줄마다 **무조건** 배달 기사를 부른다 `[정적]`.
2. 바깥 고리와 명부 줄 만들기엔 **줄을 긋는 코드가 없다** `[정적]`.
3. 부모 정보는 배달 기사에게 넘어간다 `[정적]`.
4. → 줄은 배달 기사 안에서 그을 것이다 `[추측]`.

이번에 4번이 **`[정적]` 이 됐다.** 그리고 run22 의 2번("두 함수엔 없다")도 그대로 맞다.

#### 어디서

- 공통 꼬리 안, **잠금(`LdrpModuleDatatableLock` 독점) 안**, 새 항목을 만든 **뒤**, `LdrpQueueWork` **앞** `[정적]`.
- 호출이 아니라 **함수 안에서 직접** 쓴다. 그래서 `_calls.tsv` 같은 호출 목록만 봐서는 안 보이던 자리다.

**새 말 — 독점 잠금 / 공유 잠금**: 이 함수는 같은 잠금 종류(SRW)를 두 방식으로 잡는다 — `RtlAcquireSRWLockShared`(공유) 와 `RtlAcquireSRWLockExclusive`(독점, 빠른 길은 `+0xe94` 의 `BTS.LOCK`). 일반적으로 공유 = **여럿이 같이 읽기만**, 독점 = **혼자 고치기**로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

- 비유: 명부실 열쇠. 공유는 "구경만 하는 사람들은 같이 들어와도 됨", 독점은 "고치는 동안 **나 혼자만**".
- 이 함수에서: 해시표를 **훑을** 땐 공유(`+0xb32 ~ +0xd1f`), 줄을 **그을** 땐 독점(`+0xe94 ~ +0x132f`) `[정적]`.

#### 어느 카드끼리 — 부모 카드는 인자 3 이 아니다

- **부모 카드** = 인자 2(**부모 지시서**) 의 `+0x38` 이 가리키는 명부 줄 → 그 줄의 `DdagNode` (`+0xeec ~ +0xeff`) `[정적]`.
  - 인자 3(부모 명부 줄)에서 꺼내지 **않는다.** 인자 3 은 활성화 컨텍스트, ApiSet 호스트 고르기, 새 문맥 `+0x30` 에만 쓰인다 `[정적]`.
  - 바깥 고리(`LdrpMapAndSnapDependency +0x34e`)에서는 인자 3 이 바로 `지시서 +0x38` 이라 두 값이 같은 건물이다 `[정적]` ([run22 해설](run22-map-snap-dep-해설.md) 2.4 단계 5 표, `R8` = `R13` = 지시서 `+0x38`). 그러니 test.exe 길에선 결과가 같다. 다만 "줄을 긋는 코드가 **어느 쪽에서 꺼내나**" 를 정확히 적어 두는 것이다 (다른 caller 에서는 다를 수도 있다 `[추측]`).
- **자식 카드** = 인자 5 칸에 들어 있는 명부 줄(찾은 것이든 새로 만든 것이든)의 `DdagNode` (`+0xea1`, `+0xef8`) `[정적]`.

#### 줄 한 가닥 = 0x20 바이트 칸 하나, 두 목록에 같이 걸린다

**새 말 — `_LDRP_CSLIST`(꼬리 하나짜리 목록)**: 카드의 `Dependencies`·`IncomingDependencies` 칸 타입이다 (PDB). 안에 **칸이 `Tail` 하나뿐**이다 `[정적]`. 새 칸은 **꼬리에** 끼우고, 목록이 비어 있으면 새 칸이 **자기 자신을 가리키게** 한다 `[정적]`. 끝이 처음으로 돌아오는 고리 모양 목록(원형 목록)으로 읽힌다 `[추측]` (일반적으로 알려진 설명).

- 비유: 카드에 달린 **열쇠고리**. 고리에 마지막으로 꿴 열쇠 하나만 기억하고, 열쇠끼리는 서로 이어져 한 바퀴 돈다.

이 칸의 **구조체 이름은 PDB 에 없다** `[정적]`. 그래서 run 문서처럼 오프셋 + 역할로만 쓴다.

| 칸 안 | 무엇 | 표시 |
|---|---|---|
| `+0x00` | 부모 `Dependencies` 목록 고리 | `[정적]` |
| `+0x08` | 자식 카드 | `[정적]` |
| `+0x10` | 자식 `IncomingDependencies` 목록 고리 | `[정적]` |
| `+0x18` | 부모 카드 (맨 아래 비트 0 은 원래 값 유지) | `[정적]` (비트 0 의 뜻은 `[추측]` "대비용 칸 표시") |

쓰는 줄 넷 (run 문서 표):

| 줄 | 하는 일 | 표시 |
|---|---|---|
| **`+0xf69`** | 칸을 **부모 카드 `Dependencies`(+0x28)** 꼬리에 끼움 (비었으면 `+0x17e4`) | `[정적]` |
| `+0xf6d` | 칸 `+0x08` = 자식 카드 | `[정적]` |
| **`+0xf8b`** | 칸 `+0x10` 을 **자식 카드 `IncomingDependencies`(+0x30)** 꼬리에 끼움 (비었으면 `+0x17ec`) | `[정적]` |
| `+0xf96` | 칸 `+0x18` = 부모 카드 | `[정적]` |

- **한 칸이 두 목록에 동시에** 걸린다 `[정적]`.
  - 부모 쪽에서 보면 "내가 기대는 자식".
  - 자식 쪽에서 보면 "나에게 기대는 부모".
- 비유: **끈 한 가닥**의 양 끝에 고리가 있다. 한쪽 고리는 부모 카드 **앞면** 열쇠고리에, 다른 쪽 고리는 자식 카드 **뒷면** 열쇠고리에 건다. 끈 가운데엔 "이쪽 끝은 누구, 저쪽 끝은 누구" 이름표 두 장(`+0x08`, `+0x18`).

#### 끈은 어디서 오나 — 인자 6 의 정체

- 보통은 **`+0x92e`** 에서 잡는다: `RtlAllocateHeap(LdrpHeap, .., 0x20)` → `*인자6`. `*인자6` 이 0 일 때만 `[정적]`.
- 끈을 쓰면 `*인자6 = 0` (`+0x12ab`). 안 쓰면 남는다 `[정적]`.
- 남은 끈은 바깥 고리 `LdrpMapAndSnapDependency +0x451 RtlFreeHeap(임시)` 가 놓는다 (run22).
- → run22 의 "**임시 포인터**" `[추측]` 이 **"간선 칸 보관 포인터"** 로 정해졌다 `[정적]`.
- 다른 두 할당:
  - `+0x17c2` 는 **대비용** 끈 (`+0xf4a` 에서 `RCX == 0` 일 때만). 실패하면 `0xc0000017`. 거의 안 오는 길로 보인다 `[추측]`.
  - `+0x10db` 는 끈이 **아니다.** 새 작업 지시서다 (크기 = 이름 길이 + `0xd2`).
- 비유: 배달 기사가 **빈 끈 한 가닥을 미리 챙겨** 나간다. 쓸 일이 없으면 들고 돌아오고, 건물 담당이 그걸 버린다.

#### 줄을 안 긋는 경우 셋 `[정적]`

1. 부모 카드 == 자식 카드
2. 자식 `State == 9`(ReadyToRun) 이고 (`LoadCount == -1` 이거나 자식 명부 줄 `Flags` 의 `ProcessStaticImport` 비트가 켜짐)
3. 부모 `Dependencies` 를 한 바퀴 돌아 보니 **이미 같은 자식이 있음**

- 이때는 `+0x1294` 로 가서 자식 카드 `LoadCount` 를 1 줄인다 (2 이상이고 -1 아닐 때) `[정적]`.
- 왜 줄이나 — 짐작 `[추측]` (run 문서):
  - 근거: 찾았을 때 `+0x1513 LdrpIncrementModuleLoadCount` 로 올렸다 `[정적]`.
  - 그래서: `LoadCount` 가 "**들어오는 줄 수**" 를 대신 세는 값이라면, 새 줄이 안 생겼을 때 올린 걸 되돌려야 맞다.
  - 아직 확인 아님: `LoadCount` 의 뜻은 이름과 이 짝 맞춤에서 읽은 것이다.
- 비유: 같은 두 카드 사이에 **끈을 두 번 매지 않는다.** 그리고 "끈 수" 를 미리 하나 올려 둔 걸 다시 내린다.

#### 이미 있던 건물에도, 상태가 나빠도 줄은 긋는다 `[정적]`

- 꼬리로 들어가는 조건은 **인자 5 칸 ≠ 0** 하나다 (`+0xe0e ~ +0xe15`).
- 그래서 ① 이미 올라와 있던 건물이어도 줄이 생긴다 (위 셋만 빼고).
- 상태가 음수여도 인자 5 칸이 차 있으면 줄을 긋는다.
- 예외: ① 에서 "순환 / 앞서 실패" 면 인자 5 칸을 0 으로 비우고 끝낸다 → 그땐 꼬리에 안 간다.

#### 이 함수가 카드에 쓰는 다른 칸

- 노드 `State` 에 쓰는 곳은 `+0x162d`(부모 카드 `State = 3`) **하나** (2.7). `Modules` 에는 안 쓴다 `[정적]`.

#### 비유로 정리

- 배달 기사는 끝에 늘 **명부실 열쇠를 혼자 잡고** 들어간다.
- 부모 카드(부모 지시서가 가리키는 건물) 앞면 고리와 자식 카드 뒷면 고리에 **끈 한 가닥**을 같이 건다.
- 끈은 출발할 때 미리 챙긴 것. 안 쓰면 그대로 들고 나온다.
- 같은 끈이 이미 있거나, 자기 자신이거나, 다 켜진 고정 건물이면 끈을 안 매고 "끈 수" 만 되돌린다.

### 2.4 핵심 ② 갈림은 "이미 있음 vs 새로" 둘, KnownDll 은 "새로" 안에서

#### 솔직히 먼저 — 우리가 세운 틀이 틀렸다

- 요청서 질문 3 은 "① 이미 올라와 있음 / ② KnownDll / ③ 새로 올리기" 를 **나란한 세 갈래**로 놓고 물었다.
- run22 해설 2.5 의 `?` 목록도 같은 틀이었다: "배달 기사가 **어느 길**(이미 있음 / KnownDll / 새로 올리기)로 설치 담당에 닿나".
- 실제 코드는 다르다 `[정적]`:
  - **KnownDll(②) 은 새로 올리기(③) 안에서** 새 명부 줄을 만든 **뒤** 시도한다.
  - 순서: ① 찾기 → 못 찾으면 ③ 새 지시서·명부 줄 → 그 명부 줄로 ② KnownDll 시도.
- run22 문서는 이걸 질문으로만 적었고 사실로 쓰지 않아서, run22 문서 맨 위 정정 안내는 넣지 않았다 (run 문서 "정정"). flow.md 2.6 의 같은 틀은 이번에 고쳤다.
- 비유: "창고에 있나 / 공용 창고에 있나 / 새로 주문하나" 셋 중 하나를 고르는 게 아니었다. "**이미 있나?**" 하나만 묻고, 없으면 **먼저 새 짐표를 쓰고**, 그 짐표를 들고 공용 창고부터 가 본다.

#### 새 말 둘 — 맨 이름 / 전체 이름

찾기 전에 이름 다듬기가 이름 모양을 보고 플래그를 켠다. 비트 이름은 PDB 에 없어 **못 정했다** (`[추측]` 이름). 아래는 코드에서 켜는 자리로 읽은 역할이다.

- **맨 이름** (flags `0x20`): 이름에 `\`·`/` 가 **없을** 때 켠다 (`+0x6fa`) `[정적]`. 예: `dll1.dll`.
- **전체 이름** (flags `0x200`): 이름에 전체 경로가 있다. api-set 을 풀어 만든 `System32` 경로도 여기에 켠다 (2.5) `[정적]`.
- **경로를 직접 줌** (flags `0x400`): `LdrpGetFullPath`(`+0x169a`) 가 성공하면 `0x600` 이 켜진다 (`+0x16af`) `[정적]` (2.6 에서 다시).
- 비유: 짐표에 "dll1" 만 적혔나(맨 이름), "OO동 OO번지 dll1" 처럼 주소까지 적혔나(전체 이름).

#### ① 이미 올라와 있나 — 찾는 법 셋 `[정적]`

| 경우 | 하는 일 | 자리 |
|---|---|---|
| 이유 == 9 (`PatchImage`) | 안 찾는다. 자리표시 이름 `ModuleNamePlaceholderBuffer`, 상태 `0xc0000135` | `+0x995` |
| 맨 이름 (`0x20`) | `LdrpFindLoadedDllByName(이름, 0, flags, 인자5, &모듈상태)` | `+0x16ee` |
| 전체 이름 (`0x200`) | **함수 안에서** 해시표를 훑는다 (호출 아님): 이름 마지막 조각을 대문자로 → 해시 → `LdrpHashTable[hash & 0x1f]` → 항목 `+0x108` 해시와 `FullDllName` 비교 | `+0xb32 ~ +0xd1f` (공유 잠금) |
| 둘 다 아님 | 안 찾는다. 상태 `0xc0000135` | — |

- 해시 길에서 찾으면: `+0xce7` 명부 줄 `ReferenceCount++`, `+0xcf6` 인자 5 칸에 그 명부 줄 `[정적]`.
- 맨 이름 길: 찾은 명부 줄은 callee 가 인자 5 칸에 쓴다 (인자로 넘기는 것까지만 봤다).
- 비유: 이름만 적힌 짐표면 **색인 담당에게** 물어보고, 주소까지 적힌 짐표면 기사가 **직접 색인 카드함을 연다.**

찾았을 때 `[정적]`:

- 그 건물 상태가 **음수**(앞서 실패) → 로그 "Found circular dependent DLL … failed to load previously" (`+0xded`), 상태 `0xc00000e5`, 인자 5 칸 = 0 → **줄 없이 끝.**
- 아니면 → `+0x1513 LdrpIncrementModuleLoadCount`, 상태 0.
  - 줄은 **생긴다** (2.3 의 세 예외만 빼고).
  - 상태가 0 이라 **일감 쪽지는 안 붙인다** (2.6).

#### ③ 못 찾았다 → 새로 `[정적]`

- 들어오는 조건: 찾기 결과가 **`0xc0000135`(못 찾음)** 일 때만 (`+0xd92 CMP → +0x10a3`).
  - → run22 의 "이 갈래가 '아직 안 올라온 DLL 일 때만' 인지는 `[추측]`" 이 **정해졌다** `[정적]`.
- 순서:
  1. 인자 5 칸 = 0
  2. `+0x10db` 새 작업 지시서 (힙)
  3. 지시서 칸 채우기 (아래 표)
  4. `+0x115c LdrpAllocateModuleEntry(새 지시서)` → 새 명부 줄 + 카드
  5. **`+0x1164` 인자 5 칸 = 새 명부 줄**
  6. **`+0x1179` 새 명부 줄 `LoadReason`(+0x10c) = 인자 4**. 9 면 `+0x118a` `HotPatchState = 1`.
  - `LdrpAllocateModuleEntry` 가 0 을 돌려주면 `+0x1805 RtlFreeHeap(새 문맥)` 하고 `0xc0000017`.

새 지시서에 채우는 칸 (run 문서):

| 칸 | 값 |
|---|---|
| `+0x20` | 지역 flags `\| 0x8000` |
| `+0x10` | 부모 지시서 `+0x10` (경로 구조체) |
| `+0x28` | 부모 지시서 `+0x28` (상태를 쓸 곳 = 결과 메모지) |
| **`+0x30`** | **인자 3 (부모 명부 줄)** |
| `+0xb8` | -1 |
| `+0x00` | 이름 (`+0xd0` 에 복사) |

- 결과 메모지 칸(`+0x28`)을 **부모와 같은 곳**으로 둔다는 게 눈에 띈다. 자식 쪽 결과가 부모 메모지에 적힐 수 있는 모양이다 `[추측]`.
  - 이유: 2.6 의 `+0x1cae LdrpProcessWork` 뒤에 상태를 `*(문맥+0x28)` 에서 읽는다 `[정적]`. 메모지를 같이 쓰는 모양과 맞는다.
  - 아직 확인 아님: run 문서는 칸 값까지만 적었고, 그 칸에 쓰는 callee 본문은 안 읽었다.

#### ② KnownDll — 새 명부 줄을 들고 공용 창고로 `[정적]`

- `+0x11e6` 에서 이유 ≠ 9 이면, 새 명부 줄의 `LoadContext`(`+0xb0`, 곧 새 지시서) 로 **`+0x1792 LdrpLoadKnownDll`**.
- **어떤 이름이 KnownDll 인지는 callee 가 정한다.** 이 함수는 조건 없이 부른다.
- 돌려받은 값:
  - **0** → callee 안에서 올림이 끝난 것 (지도: `LdrpLoadKnownDll +0xf1 → LdrpMapDllWithSectionHandle`) → 일감 안 넣음.
  - **`0xc0000135`** → KnownDll 아님 → 2.6 의 일감 / 바로.

왜 새 명부 줄을 **먼저** 만드나 — 짐작 `[추측]`:

- 근거: `LdrpLoadKnownDll` 에 넘기는 인자가 **새 명부 줄의 지시서**다 `[정적]`.
- 그래서: 공용 창고에서 꺼내 오든 파일에서 새로 짜든, 받아 둘 **짐표(지시서)와 명부 줄이 먼저 있어야** 한다.
- 아직 확인 아님: `LdrpLoadKnownDll` 본문은 안 읽었다. 코드에 이유가 쓰여 있는 건 아니다.

#### 인자 5 칸에 무엇이 들어가나 — 한 표로

| 길 | 인자 5 칸 | 자리 | 표시 |
|---|---|---|---|
| ① 해시 길 | 찾은 명부 줄 | `+0xcf6` | `[정적]` |
| ① 맨 이름 길 | callee 가 쓸 것 | `+0x16ee` | 칸을 넘기는 것은 `[정적]`, callee 가 쓰는 것은 `[추측]` (본문 안 봄) |
| ③ 새로 | 새 명부 줄 | `+0x1164` | `[정적]` |
| 교체됐을 때 | 문맥 쪽 명부 줄 | `+0x1341` (`LdrpFreeReplacedModule` 바로 뒤) | `[정적]` |
| 순환·앞서 실패 | 0 | — | `[정적]` |

#### 비유로 정리

1. 짐표를 받으면 "**이미 우리 가게에 있나?**" 부터 본다.
2. 있으면 "하나 더 씀" 표시만 하고(로드 수 +1) 끈 매러 간다.
3. 없으면 **새 짐표**를 쓰고 명부에 새 줄·카드를 만든다.
4. 그 짐표를 들고 **동네 공용 창고**부터 가 본다. 거기 있으면 꺼내 와서 끝.
5. 공용 창고에도 없으면 → 2.6 (쪽지 또는 직접).

### 2.5 핵심 ③ ApiSet — 가상 이름을 진짜 이름으로 바꾸는 자리

#### 새 말 — api-set (가상 DLL 이름)

- `api-ms-win-…`·`ext-…` 같은 이름은 **실제 파일 이름이 아니라 "기능 묶음 이름"** 이고, 실행 때 진짜 DLL 이름으로 바뀌는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 바꿀 때 보는 표는 **`PEB.ApiSetMap`** 이다. 이 함수가 그 칸을 읽는다 `[정적]`.
- 비유: 짐표에 "**OO 가구점**" 같은 **별명**이 적혀 있다. 가게 안내판(PEB)에 붙은 **별명 대조표**를 보고 "아, 그건 **KERNELBASE 건물**이야" 로 바꾼다.

#### 이 함수 안에 그 자리가 있다 `[정적]`

맵 모양에 따라 두 길이다 (run 문서).

| 맵 모양 | 하는 일 | 자리 (ret) |
|---|---|---|
| 새 쪽 (`ApiSetMap` 첫 바이트 7, 또는 버전 6 이고 `+0x10 > 8`·`+0x1c == 7`·`+0x1e & 4 == 0`) | `ApiSetpResolveHost(PEB.ApiSetMap, 이름, 글자 수, 1, 부모 BaseDllName, &api-set이었나, &진짜이름)` | `+0x2ff` |
| 옛 쪽 | 이름 첫 8바이트가 `api-`/`ext-` 면 마지막 `-` 뒤를 떼고 `ApiSetpSearchForApiSet` | `+0x22b5` |
| 〃 | 후보가 둘 이상이고 부모 이름이 있으면 `ApiSetpSearchForApiSetHost` | `+0x22e0` |

- **부모 `BaseDllName` 도 넘긴다.** 부모마다 다른 진짜 건물을 고를 수 있게 하는 것으로 보인다 `[추측]`.
- api-set 이었으면 `[정적]`:
  - 진짜 이름 길이가 0 → `0xc0000481` 로 실패.
  - 아니면 **`NtSystemRoot` + `\System32\` + 진짜 이름** 전체 경로를 지역 버퍼에 만든다 (`+0x3ca ~ +0x5ff`).
  - 로그 "… was redirected to … by API set" (`+0x15af`), flags `|= 0x200`(전체 이름).

#### 바뀐 이름도 같은 길을 탄다 `[정적]`

- 다른 점은 하나: flags 에 `0x20`(맨 이름)이 안 켜지고 `0x200`(전체 이름)이 켜진다.
- 그래서 `LdrpFindLoadedDllByName` 대신 **해시표 훑기 + 전체 이름 비교**로 찾는다 (2.4 ① 표 셋째 줄).
- 못 찾으면 ③ 새 명부 줄 → ② KnownDll — 2.4 와 같다.
- 비유: 별명을 진짜 주소로 바꾼 짐표는, 그 뒤로는 **주소가 적힌 보통 짐표와 똑같이** 처리된다.

#### run20·run21 의 "kernelbase 가 kernel32 를 따라 들어온다" — 어느 길인가

[run20 해설](run20-kernel32-load-해설.md) 2.4 에서 본 "순서 문제": 관리실은 KERNEL32 를 먼저 주문했는데, KERNELBASE 가 **그 주문 안에서** 따라 올라올 것 `[추측]`. [run21 해설](run21-load-dll-internal-해설.md) 2.5 는 그 길이 지도에 **있다**까지 봤다.

이번에 flow.md 2.5 가 코드 순서로 한 줄 더 썼다 `[정적: 길]`:

```
kernel32 매핑 → LdrpMapDllWithSectionHandle +0x1c8 LdrpMapAndSnapDependency(kernel32 문맥)
 → +0x34e LdrpLoadDependentModuleInternal(api-set 이름 ?, ..) → +0x2ff 등에서 KERNELBASE.dll 로 바뀜
 → KnownDll 이면 +0x1792 안에서 올라옴
```

한 단계씩:

1. kernel32 가 갖다 놓이면 설치 담당이 건물 담당을 부른다 (run22) `[정적]`.
2. 건물 담당은 kernel32 의 "기대는 건물 목록" 을 한 줄씩 배달 기사에게 준다 (run22) `[정적]`.
3. 배달 기사 안에 **별명 → 진짜 이름** 자리가 있다 (이번) `[정적]`.
4. 바뀐 이름도 ① → ③ → ② 를 탄다 (이번) `[정적]`.
5. → 그러니 **"kernel32 의 목록에 별명이 있으면 여기서 KERNELBASE 로 바뀌어 같은 호출 안에서 올라온다"** 는 코드 길이 이어졌다.

남은 `[추측]` 셋 (flow.md 2.5):

- kernel32 의 import 가 **정말** api-set 이름인가 → kernel32 import 표를 안 봤다.
- 그게 `KERNELBASE.dll` 로 풀리나.
- kernelbase 가 KnownDll 이라 `+0x1792` 안에서 끝나나. 그렇다면 이 함수의 `+0x182c LdrpQueueWork` 는 안 타고, kernelbase 문맥 끝맺음 `MapAndSnap +0x4f7` 만 탄다.
- → 확인은 동적 **Q20** (`ntdll+0x21daf` 에서 `dS @rsp+0x108`, `ntdll+0x23242` 에서 `r eax`).

그래서 "이 함수가 바꿔 주는 **자리가 있다**" 까지만 `[정적]` 이다.

#### test.exe 쪽은

- `dll1.dll`·`dll2.dll` 은 일반 이름이다 `[소스]` → "api-set 아님" 쪽으로 지나갈 것 `[추측]`.

#### 곁가지 — SxS 리다이렉트

- **새 말 — SxS 리다이렉트**: 매니페스트를 보고 DLL 이름을 **다른 버전 파일로 돌리는** 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이 함수에선 `+0x142b RtlDosApplyFileIsolationRedirection_Ustr` 다 `[정적]`.
- 언제 시도하나 `[정적]`: api-set 길에서는 `ProcessParameters.Flags & 0x1000` 일 때만, 일반 이름은 `LdrpIsSecureProcess == 0` 이면 늘 (`+0x13e7`).
- 이 함수는 **부모 명부 줄의 활성화 컨텍스트**(`+0x88`)를 켠 채 돈다 (`+0xf1`) `[정적]`. 그래서 SxS 리다이렉트가 **부모의 매니페스트 기준**으로 이름을 바꿀 것 `[추측]`.
- 비유: 부모 건물의 "어떤 버전 부품을 쓸지" 설정표를 펴 놓고 일한다.

### 2.6 핵심 ④ 일감 쪽지로 넘기나, 직접 가져오나

#### 두 호출 나란히 `[정적]`

| 호출 (call / ret) | 조건 (모두) | 잠금 | 뒤에 |
|---|---|---|---|
| `+0x1827` / **`+0x182c`** `LdrpQueueWork(항목.LoadContext)` | 인자 5 칸 ≠ 0, 상태 == `0xc0000135` (`+0x12b2`), flags `0x400` **꺼짐** (`+0x1812`) | **`LdrpModuleDatatableLock` 독점을 잡은 채** (`+0xe94` ~ `+0x132f` 사이) | 상태 = 0 |
| `+0x1ca9` / **`+0x1cae`** `LdrpProcessWork(항목.LoadContext, 1)` | flags `0x400` **켜짐** (경로를 직접 줌, `LdrpGetFullPath` 성공) 이고 상태 == `0xc0000135` | 잠금 잡기 **전** | 문맥 `+0x20` 비트 26 켬 (`+0x1c9d`), 상태 = `*(문맥+0x28)` |

표를 한 줄씩 말로 바꾸면:

- **상태가 `0xc0000135` 인 길** = "새로 만들었고 KnownDll 도 아니었다" (또는 이유 9).
  - **이미 올라와 있던 건물은 상태 0 이라 일감에 안 들어간다** `[정적]`.
  - KnownDll 로 올라온 건물도 상태 0 이라 안 들어간다.
- 그중 **경로를 직접 준 이름**(`0x400`)이면 → 잠금 잡기 전에 **직접** 가져온다 (`LdrpProcessWork`).
- 아니면(맨 이름 등) → 잠금 안에서 **게시판에 쪽지**를 붙인다 (`LdrpQueueWork`).
- 상태 `0xc000022d` 는 0 으로 바꿔서 그냥 지나간다 (`+0x12be`). 이 값 이름은 `STATUS_RETRY` 로 알려져 있다 `[추측]`.
- 비유: 주소까지 정확히 적힌 짐표면 기사가 **직접 가서 가져온다.** 이름만 있는 짐표면 "**가져와 주세요**" 쪽지를 게시판에 붙이고 끝낸다.

#### dll1·dll2 는 어느 쪽인가

- `dll1.dll`·`dll2.dll` 은 맨 이름이다 `[소스]`.
- 그러니 KnownDll 이 아니면 `+0x182c LdrpQueueWork` 쪽이다 `[추측]` (KnownDll 아님은 실행 값).
- `+0x1cae LdrpProcessWork` 는 안 탈 것 `[추측]`.

#### 정정 — run21·run22 의 "파일 길" 사슬에 조건이 붙었다

- run21·run22 해설에서 "파일 길" 로 적은 지도 이름 사슬: `LdrpLoadDependentModuleInternal +0x1cae LdrpProcessWork → +0x6a LdrpMapDllSearchPath → …` ([run22 해설](run22-map-snap-dep-해설.md) 2.5).
- 사슬 자체는 맞다. 다만 이 호출은 **경로를 직접 준 이름(flags `0x400`)일 때만** 탄다 `[정적]`.
- 맨 이름은 KnownDll 이 아니면 `+0x182c LdrpQueueWork` 로 간다. flow.md 2.6 의 그 문장에 조건을 덧붙였다.

#### 쪽지가 두 종류다 — 헷갈리지 않게

run22 에서도 `LdrpQueueWork` 가 나왔다. **다른 자리, 다른 쪽지**다.

| 자리 | 쪽지 내용 | 언제 | 근거 |
|---|---|---|---|
| `LdrpLoadDependentModuleInternal +0x182c` (이번) | 이 **자식 건물을 가져와 주세요** | 새로 만들었고 KnownDll 아님, 경로 직접 아님 | run23 `[정적]` |
| `LdrpMapAndSnapDependency +0x4f7` (run22) | 이 건물 **자기 전선 꽂기**(snap)를 해 주세요 | 남은 자식 0, 부모 있음 | run22 `[정적]` |

- flow.md 3장이 이번에 이렇게 이었다: exe import 길의 **첫 일감은 dll1 의 "올려 주세요"** 일 것이다 (dll1 이 KnownDll 이 아니라면 `[추측]`). dll1 자신의 snap 쪽지(`MapAndSnap +0x4f7`)는 그 뒤.

#### 병렬 로딩이 켜졌는지는 안 본다 `[정적]`

- 이 함수가 읽는 전역은 `__security_cookie`, `NtdllBaseTag`, `LdrpHeap`, `LdrpModuleDatatableLock`, `LdrpHashTable`, `GlobalRtlNlsState`, `LdrpIsSecureProcess`, `LdrpDefaultExtension`, `LdrpIllegalCWDDevices`, `SlashSystem32SlashString`, `ModuleNamePlaceholderBuffer` 뿐이다.
- `LdrInitState`·`LdrpEnableParallelLoading` 결과를 읽는 줄이 **없다.**
- → kernel32 준비 때(병렬 켜기 **앞**)와 exe import 때(켜기 **뒤**)에 **이 함수 코드는 똑같이** 돈다 `[정적]`. run22 의 바깥 고리와 같은 결론이 **안쪽까지** 이어진 것이다.

그럼 어디서 달라질 수 있나 — 둘 (run 문서):

1. 부모 지시서 `+0x20` 에서 물려받는 비트 (`0x800`·`0x800000`·`0x1000000`). 켜는 곳은 이 함수 밖.
2. `LdrpQueueWork` **본문**. 이 함수는 쪽지를 붙인 **뒤** 자식 `State` 를 `+0x12df ~` 에서 읽는다.
   - 그 자리에서 다 올렸으면 `State ≥ 2`, 미뤘으면 `< 2` → 부모 체크판이 달라진다 (2.7).
   - 병렬 켜기 앞뒤 차이가 여기 나타날 수 있다 `[추측]`.

#### 누가 쪽지를 꺼내나 — 모른다, Q12c

- 이 함수는 쪽지를 붙이고 끝이다. 그 뒤 dll1 지시서로 `LdrpMapDllWithSectionHandle` 에 닿는 길은 **지도만** 있다.
  - 지도: `LdrpQueueWork +0x82 → TpPostWork`(일꾼 쪽), `LdrpProcessWork +0x6a → LdrpMapDllSearchPath → … → LdrpMapDllWithSectionHandle`, `LdrpInitializeProcess +0x1ab1 LdrpDrainWorkQueue(1)`.
- 일꾼이 돌리면 이 줄은 **다른 직원**의 일이 되어 "호출" 이 아니게 된다. 그래서 flow.md 그림 2 에서 `LdrpQueueWork` → `LdrpMapDllWithSectionHandle` 화살표가 `?` 다.
- 본문은 정적 **Q12c**, 어느 직원인지는 동적 **Q47**.
- 하나 더 볼 것: **잠금을 잡은 채** 쪽지를 붙인다는 게 실행 때도 맞나 → Q47 (call `ntdll+0x232d7` 에서 `dq ntdll!LdrpModuleDatatableLock L1` 비트 0).

### 2.7 체크판(`+0x6c`)과 기다림 도장

[run22 해설](run22-map-snap-dep-해설.md) 2.6 의 체크판: 건물 담당이 "**아직 안 온 짐 수 + 나**" 를 적어 두고, 0 이 되면 전선을 꽂는다. run22 는 "배달 기사 쪽에 이 칸을 줄이는 줄이 있다" 까지만 봤다.

근거 `+0x12ce ~ +0x12fa`, `+0x1606 ~ +0x163d`, `+0x1700 ~ +0x1711`. 상태가 0 이상(또는 `0xc000022d`·일감으로 0 이 된 것)일 때만 돈다 `[정적]`.

#### 체크판이 0 이 아닐 때 (보통, 바깥 고리에서)

- **줄인다** (`+0x12f7` `+0x6c` = 값 − 1) `[정적]`:
  - 끈이 남았다(`*인자6 ≠ 0`, 곧 새 줄이 안 생김) **이거나**
  - 자식 `State ≥ 2`(Mapped 이상)
- **안 줄인다** (`+0x1711 → +0x12fa`) `[정적]`:
  - 새 줄을 그었는데 자식 `State < 2`
- 뜻 `[추측]`: "이미 끝난 자식, 새 줄이 필요 없는 자식은 바로 지우고, 새로 이었는데 아직 안 올라온 자식은 **부모 몫으로 남긴다**".
- 그 자식이 **나중에** 다 올라왔을 때 부모 체크판을 지우는 코드는 **이 함수에 없다** (`MapAndSnap` 에도 없다) → Q12c.
- 비유: 짐이 이미 와 있으면 체크판에서 바로 지운다. 아직 길 위에 있으면 **지우지 않고 남겨 둔다.** 도착했을 때 누가 지우는지는 아직 모른다.

#### 체크판이 이미 0 일 때 (`+0x12e4 → +0x1606`), 자식 `State < 2` 면

| 자리 | 하는 일 | 표시 |
|---|---|---|
| `+0x1617` | 문맥 `+0x50` = 자식 명부 줄 | `[정적]` |
| `+0x161b` | 문맥 `+0x6c` = 1 | `[정적]` |
| `+0x162d` | **부모 카드 `State = 3`**(`WaitingForDependencies`) | `[정적]` |
| `+0x1634` | **반환 상태 `0x103`** (`STATUS_PENDING` 값) | `[정적: 줄]` |

- 뜻 `[추측]`: "체크판이 이미 0 인데 안 끝난 자식이 새로 생겼다 → 그 하나를 기다리는 걸로 되돌리고 부모에게 **기다림 도장**을 찍는다".
- 비유: 체크판이 0 이라 "다 왔다" 로 보였는데, 새 짐이 하나 더 생겼다 → 체크판에 1 을 다시 적고, 부모 카드에 "**짐 기다리는 중**"(3) 도장.

#### 바깥 고리에서는 이 길이 안 나온다 `[정적: 카운터 줄]`

- 체크판은 "자식 수 **+ 1**" 로 시작하고, 그 +1 은 고리 뒤 끝맺음(`MapAndSnap +0x47d`)에서만 놓는다 (run22).
- 그러니 고리 도중엔 0 이 될 수 없다.
- → `0x103` 길은 **다른 caller** 몫으로 보인다 `[추측]`: `LdrpSnapModule +0x5df`, `LdrpLoadDependentModuleW +0xae`. (`LdrpSnapModule` 쪽은 **Q50**.)
- run22 해설 2.6 에서 "+1 을 왜 두나" 를 `[추측]` 으로 남겼다 ("도중에 0 이 되어 '끝' 으로 보이는 걸 막으려고"). 이번 줄들과 **맞아 보인다.** 다만 이유가 코드에 쓰여 있는 건 아니어서 여전히 `[추측]` 이다.

#### caller 는 `0x103` 을 성공처럼 받는다 `[정적]`

- `LdrpMapAndSnapDependency +0x34e ~ +0x352` 는 **음수일 때만** 실패로 본다. `0x103` 은 양수라 그냥 지나간다.

### 2.8 test.exe → dll1 → dll2 이야기 — run22 그림의 `?` 칸 채우기

run 문서 그림 그대로. 오른쪽 `←` 는 run22 의 `?` 칸이 어디로 채워졌는지다.

```
LdrpLoadDependentModuleInternal("dll1.dll", exe문맥, exe 항목, 0, &exe문맥.58[i], &임시)   ← MapAndSnap +0x34e
  부모(exe 항목) 활성화 컨텍스트 올림
  이름 다듬기: api-set 아님 → SxS 시도 → 맨 이름(0x20) → ".dll" 확인          [정적: 길] / 실제 값 [추측]
  *임시 에 0x20바이트 간선 칸 (+0x92e)
  +0x16ee LdrpFindLoadedDllByName → 0xc0000135 (아직 없음)                       [추측]
  새 dll1문맥 (+0x30 = exe 항목) → +0x115c LdrpAllocateModuleEntry
     → dll1 항목 + 노드, *인자5 = dll1 항목, LoadReason 0 (+0x1179)            [정적]
  +0x1792 LdrpLoadKnownDll(dll1문맥) → 0xc0000135 (KnownDll 아님)                [추측]
  락 독점
    ★ 간선: exe 노드.Dependencies ← 칸 → dll1 노드.IncomingDependencies (+0xf69, +0xf8b)   ← run22 "? 간선"
    +0x182c LdrpQueueWork(dll1문맥)  (락 안)                                      ← run22 "? … 올리기"
    exe문맥 +0x6c : dll1 State < 2 면 그대로, ≥ 2 면 −1                            ← run22 "남은 수 = ?"
  참조 −1, 락 풀기, 반환 0
```

한 줄씩 풀면:

1. 건물 담당(exe)이 "dll1 가져와" 짐표를 준다. 부모 = exe.
2. 기사가 **exe 의 설정표**(활성화 컨텍스트)를 편다.
3. 이름을 다듬는다. dll1 은 별명이 아니고 맨 이름이다 (길은 `[정적]`, dll1 의 실제 값은 `[추측]`).
4. 빈 끈 한 가닥을 챙긴다.
5. 색인 담당에게 "dll1 있어?" → 아직 없을 것 `[추측]`.
6. 새 짐표(`+0x30` = exe 명부 줄) + 새 명부 줄·카드. 주문 사유 0.
7. 공용 창고에 가 본다 → dll1 은 없을 것 `[추측]`.
8. 명부실 열쇠를 혼자 잡고:
   - exe 카드 앞면 ↔ dll1 카드 뒷면에 끈을 건다.
   - "dll1 가져와 주세요" 쪽지를 게시판에 붙인다.
   - exe 체크판: dll1 이 쪽지 뒤에도 `State < 2` 면 그대로, `≥ 2` 면 하나 지운다.
9. 손에 쥔 참조 하나를 놓고, 열쇠를 돌려주고 0 을 들고 돌아간다.

dll2 바퀴는 **같은 코드**다 (flow.md): 찾기 → 새 dll2 지시서·명부 줄 → KnownDll → 끈 dll1 → dll2 → `+0x182c` 쪽지.

#### 정적으로 한 칸 더 읽히는 것 — exe 의 체크판

run22 그림의 "exe 남은 수 = ?" 는 두 경우로 갈린다 (`MapAndSnap +0x47d ~ +0x49a`):

| dll1 이 쪽지 뒤에 | exe 체크판 | 끝맺음에서 | 결과 |
|---|---|---|---|
| `State < 2` (아직 안 올라옴) | 2 (자식 1 + 1) | 1 | 0 이 아니라 **snap 안 함** |
| `State ≥ 2` (그 자리에서 올라옴) | 1 | 0 | `+0x4c2 LdrpSnapModule` |

- 어느 쪽인지는 `[추측]` → **Q47** (쪽지를 그 자리에서 처리하나, 일꾼에게 미루나).

#### 아직 `?` 인 칸

- `LdrpQueueWork` 가 돌아왔을 때 dll1 `State` → Q12c (본문), Q47 (실행).
- dll1 지시서로 `LdrpMapDllWithSectionHandle` → `LdrpMapAndSnapDependency` 를 **누가, 어느 직원이** 부르나 → Q12c·Q47.
- 나중에 exe 체크판을 지우고 exe snap 으로 잇는 쪽 → Q12c.

#### run8 관찰과 맞나

- `[관찰]` run8: "`+0x1bb6` 에서 dll1·dll2 가 **이미** 올라와 있었고 켜는 순서 dll2 → dll1".
- 쪽지를 붙이는 건 여기, 쪽지가 끝나길 기다리는 건 `LdrpInitializeProcess +0x1ab1 LdrpDrainWorkQueue(1)` (run19) `[정적]`.
- → **어긋나지 않는다.**

### 2.9 곁가지 — 짧게

#### `+0x1f03 ~` 은 실패 정리가 아니라 "마지막 참조 정리" `[정적: 조건 줄]`

**새 말 — 참조 수 (`ReferenceCount`, 명부 줄 `+0x114`)**: 이름으로는 "이 명부 줄을 지금 **손에 쥐고 있는 쪽의 수**" `[추측: 이름]`. 0 이 되면 치운다.

- `+0x1310 XADD.LOCK [항목+0x114], -1` 로 하나 내린다 (자식 카드 `LoadCount ≠ -1` 이고 `ProcessStaticImport` 꺼졌을 때).
- 옛 값이 1 이었으면(= 이게 마지막) `+0x1319` → `+0x187a` 정리 길: 목록에서 빼기, `LdrpReleaseTlsEntry`, `LdrpUnmapModule`, `RtlReleaseActivationContext`, 이름·항목 놓기, 카드가 비면 `LdrpDestroyNode`.
- 새 명부 줄은 참조 2 로 시작하고 (run22), ① 해시 길은 `+0xce7` 에서 +1. 이 함수는 −1 만 한다 → **정상으로 올라온 DLL 은 이 길을 안 탄다** `[추측]`. 다른 쪽이 먼저 놓았을 때만.
- 비유: 명부 줄을 쥔 손이 둘 이상이면, 기사가 자기 손을 놓아도 줄은 그대로다. **마지막 손**이 놓을 때만 치운다.

#### `RtlRaiseException(0xc0150014)` `[정적]`

- 활성화 컨텍스트 스택 꼭대기 프레임의 `Flags & 0x70 ≠ 0x20` 일 때 (`+0x23f5`). 끝나면 `+0x1de` 로 돌아와 계속 간다.
- test.exe 길에선 안 탈 것 `[추측]`.

#### 반환값 (run 문서 표에서 대표)

| 값 | 언제 | 표시 |
|---|---|---|
| 0 | ① 찾아서 로드 수만 올림, ② KnownDll 로 올림 끝, 일감 넣고 돌아옴 | `[정적]` |
| `0x103` | 부모 `+0x6c == 0` 이고 자식 `State < 2` (`+0x1634`) | `[정적]` |
| `0xc0000135` | flags `0x400` 로 바로 올렸는데 못 찾음 | `[정적]` |
| `0xc00000e5` | 이미 있는 항목의 상태가 음수 | `[정적]` |
| `0xc0000481` | api-set 으로 풀었는데 진짜 이름 길이 0 | `[정적]` |
| `0xc0000017` / `0xc0000106` | 힙 할당 실패 / 이름·경로가 `0xfffe` 넘음 | `[정적]` |

- dll1·dll2 처럼 쪽지로 넘긴 길은 **0** 이다. "아직 안 올라왔는데 0(성공)" 이라는 점이 2.7 의 체크판 이야기와 이어진다.

#### 인자 4 (`LoadReason`)

- `+0x1ec`·`+0x995`·`+0x11e6` 에서 9(`PatchImage`) 와 비교 → 이름 다듬기·찾기·KnownDll 을 건너뛴다 `[정적]`.
- **새 명부 줄**에만 쓴다 (`+0x1179`). ① 찾은 명부 줄에는 안 쓴다 `[정적]`.
- 바깥 고리는 0 을 넘긴다 (run22).

#### 결과서 오프셋 두 곳 — run 문서가 고쳐 읽음

| 결과서 | run 문서·이 글 | 이유 |
|---|---|---|
| `RtlRaiseException` 뒤 돌아가는 자리 `+0x1c8e` | `+0x1de` | `.asm` 의 `+0x23f5 JMP 0x180021c8e` 에서 **절대 주소 끝자리**를 적은 것 |
| 교체된 항목을 인자 5 칸에 쓰는 줄 `+0x1345` | `+0x1341` | `+0x1345` 는 그다음 `JMP` 줄 |

- 첫째가 헷갈리기 쉽다: `0x180021c8e` 는 **절대 주소**(`.asm` 에 찍힌 주소)이고, 함수 시작에서 센 오프셋으로는 `+0x1de` 다 (`.asm` 81줄, run 문서).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만.

| 자리 | 전 (run22 뒤) | 후 (run23) |
|---|---|---|
| 기준 | ntdll.pdb (run17 ~ run22), 정적 근거 run9 ~ run22 | + **run23** |
| 두 그림 사이 설명 | run22 문단까지 | + run23 문단: `LdrpLoadDependentModuleInternal` 아래를 코드 순서로 채움, 간선은 호출이 아니라 상자 글에, `LdrpMapDllWithSectionHandle` 은 일감 상자 뒤 `?` |
| 그림 2 `j2` | "간선 적는 줄 없음" | "이 함수엔 간선 줄 없음 — **간선은 자식 쪽 Internal 이 씀**" |
| 그림 2 `ja3` [dll1 추정] | 인자만 | + 순서(이름 다듬기 → 이미 있나 → 새 항목 → KnownDll → 일감), 잠금 안 간선 `+0xf69`/`+0xf8b`, 부모 남은 수 `+0x12f7` |
| 그림 2 새 `(정적)` 상자 | 없음 | dll1 층 `jd1`~`jd4` (`ApiSetpResolveHost`, `LdrpFindLoadedDllByName`, `LdrpLoadKnownDll`, `LdrpQueueWork`), dll2 층 `je1`~`je4` (같은 넷) |
| 그림 2 화살표 | `(1) +0x115c` → Allocate, `(2) ?` → 설치 담당 | `(1) +0x2ff` / `(2) +0x16ee` / `(3) +0x115c` / `(4) +0x1792` / `(5) +0x182c`, 그리고 `LdrpQueueWork` → `?` → 설치 담당 (dll1·dll2 층 둘 다) |
| 2.5 | run22 의 "고리 기준으로 한 칸 더" | + KnownDll·일감 조건, **api-set → 진짜 이름 자리** (`+0x2ff` 등), "kernel32 를 들이면 kernelbase 가 따라 들어온다" 를 코드 순서로 `[정적: 길]`, 간선 "**맞았다**" |
| 2.6 제목 | "바깥 고리 코드 `[정적]` run19·run21·run22" | + "**안쪽 한 바퀴 run23**". 머리 `[공백]` 은 그대로 |
| 2.6 본문 | `?` 칸 | + 새 소절 `LdrpLoadDependentModuleInternal`, 바깥 고리 소절에 체크판 조건·"음수만 실패", 문맥 `+0x50`, (해결됨) ③ 갈래 조건, (정정) `+0x1cae` 조건, 이야기 그림 갱신 |
| 3장 일꾼 | run22 두 줄 | + `+0x182c` 조건·잠금 안, "첫 일감은 dll1 '올려 주세요'" `[추측]`, kernel32 자식이 KnownDll 이면 `+0x182c` 안 탐 |
| 4장 공백 표 | run22 줄들 | 2.6 간선·갈림 줄 → **(해결됨, 정적)**, 남은 자식 수 → **(일부 해결)**, 새 줄 넷 (실행 값 Q23·Q47·Q20, 일감 뒤 누가 Q12c, `LdrpSnapModule` Q50, 이름·뜻 못 정한 것 run23). 3 일꾼·2.5 줄에 run23 덧붙임 |
| 4.1 | `Internal → (1) Allocate / (2) ?` 줄 | 그 줄을 (1) ~ (5) 다섯 화살표로 바꿈 (번호 = 실행 순서, 조건 같이) + 새 줄 `LdrpQueueWork → ? LdrpMapDllWithSectionHandle` (일꾼이 돌리면 다른 스레드라 `?`) |
| 5장 | run22 까지 | run23 줄, 요청서 번호 표 run23 줄 |

태그로 보면 핵심 변화는 다섯이다:

- **"간선을 어디서 긋나"**: `[추측]` "안쪽일 것" (run22) → **`[정적]`** `+0xf69`/`+0xf8b`, 잠금 안.
- **"이미 있음 / KnownDll / 새로 갈림"**: 질문(나란한 셋) → **`[정적]` ① → ③ → ②**, KnownDll 은 "새로" 안. run22 의 "③ 은 못 찾았을 때만?" `[추측]` → `[정적]`.
- **"인자 6 임시 포인터"**: `[추측]` → **`[정적]` 간선 칸 보관 포인터**.
- **"일감 vs 바로"**: 지도 이름 사슬만 → **`[정적]` 조건** (`0xc0000135` + `0x400` 여부). run21·run22 의 "파일 길" 에 조건이 붙음 (정정).
- **"api-set 이름이 바뀌는 자리"**: `[추측]` → **자리는 `[정적]`** (`+0x2ff` 등). kernel32 import 가 실제로 그 길을 타나는 여전히 `[추측]` (Q20).

2.6 제목 머리 `[공백]` 은 **그대로**다. 안쪽 한 바퀴 **코드**가 채워졌을 뿐, dll1·dll2 가 실제로 올라오는 순간은 아직 아무도 안 봤다.

그림 2 를 볼 때 주의:

- 새 상자는 **전부 `(정적)`** 이다. 관찰한 상자는 없다.
- 화살표 번호 (1) ~ (5) 는 **실행 순서**다. 주소 순서와 다르다 (함수 안에 끼워 넣은 코드 때문, 2.1).
- 간선은 호출이 아니라서 **화살표가 아니라 `ja3`·`jb4` 상자 글**에 적혀 있다.

### 큰 그림에서 어떻게 이어지나

- run20 = kernel32 주문서, run21 = 배달 창구, run22 = 목록을 도는 건물 담당, **run23 = 목록 한 줄을 맡은 배달 기사.**
- 이번으로 "**연결 카드 사이 줄이 언제 생기나**" 가 코드로 이어졌다:
  - `LdrpAllocateModuleEntry` 가 카드를 만들고 (run22) → 배달 기사가 잠금 안에서 **줄을 긋는다** (run23).
  - 그 줄은 나중에 전원 켜기 순서(`+0x1bbb`, dll2 → dll1, run8 `[관찰]`)를 정할 때 쓰일 것으로 보인다 `[추측]`.
    - 이유: 켜기 함수 이름이 `LdrpInitializeGraphRecurse`(그래프를 따라 내려감) 이고, 켜는 순서가 "기대는 쪽(dll2) 먼저" 다. 이름과 순서에서 읽은 것이라 코드 근거는 아니다.
  - 그 줄을 **읽는 쪽**(그래프 정리 `LdrpCondenseGraph` 계열)은 Q12c.
- "**dll1 은 실제로 어디서 올라오나**" 는 한 칸 더 좁혀졌다:
  - 배달 기사는 dll1 을 직접 올리지 않고 **쪽지**로 넘긴다 (KnownDll 이 아니라면).
  - 그러니 실제 매핑은 쪽지를 꺼내는 쪽이다 → **Q12c** (정적, 다음 항목).
- **다음 구간** (`plan.md` "다음 항목"): **Q12c — 일감 흐름 + 그래프 정리** (정적).
  - 순서는 (1) `LdrpQueueWork`·`LdrpProcessWork`·`LdrpDrainWorkQueue` 본문 → (2) Condense 쪽이 자연스럽다 `[추측]` (결과서 판단).
  - 왜 같이 보나: 체크판을 "다 올라온 뒤" 지우고 부모 snap 으로 잇는 코드가 **이 함수에도 바깥 고리에도 없어서** 쪽지 쪽이 이어받을 것으로 보이고, 줄은 이미 여기서 그어지니 그 줄을 읽는 쪽과 짝으로 보기 좋다 (결과서 판단).
- **Q50 — `LdrpSnapModule`** 은 주제가 달라 따로 뺐다 (IAT 에 주소 적기, `+0x5df` 에서 이 함수를 부르는 갈래 = `0x103` 을 받을 수 있는 caller 후보).
- **Q14** ("dll1·dll2 를 올리는 쪽") 는 **여전히 일부**다. 이름 처리·새 항목·간선·"KnownDll 아니면 쪽지" 까지 `[정적]`. 실제로 맵하는 쪽과 직원은 Q12c·Q47 뒤.
- 버스를 탈 때(동적) 볼 것은 새 번호 없이 **Q20**(kernel32 import 의 ApiSet·KnownDll·잠금), **Q23**(dll1·dll2 갈래·간선·체크판), **Q47**(쪽지 직원·잠금) 에 붙었다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 쪽지(`LdrpQueueWork`)를 **누가, 언제** 꺼내 실제로 올리나. 쪽지가 돌아왔을 때 자식 `State` (그 자리에서 올리나, 미루나) | `LdrpQueueWork`·`LdrpProcessWork`·`LdrpDrainWorkQueue` 본문은 이번 범위 밖 | 정적 **Q12c** (다음 항목) |
| 안 끝난 자식이 **나중에** 올라왔을 때 부모 체크판(`+0x6c`)을 지우고 snap 으로 잇는 코드 | 이 함수에도 `MapAndSnap` 에도 없다 | 정적 **Q12c** |
| 그어 둔 줄을 **읽는 쪽** — 그래프 정리 (`LdrpCondenseGraph` → `LdrpCondenseGraphRecurse` → `LdrpMergeNodes`) | 이번엔 줄을 **긋는** 쪽만 봤다 | 정적 **Q12c** |
| `0x103` 을 받는 caller 가 정말 `LdrpSnapModule +0x5df` 인가, IAT 에 주소를 적는 곳 | `LdrpSnapModule` 본문을 안 읽었다 | 정적 **Q50** |
| dll1·dll2 가 실제로 어느 갈래를 탔나 (못 찾음 `0xc0000135`? KnownDll 아님?), 줄이 정말 그어졌나, 체크판 값 | 실행 때 값이다 | 동적 **Q23**: 입구 `ntdll+0x21ab0`, `FindLoaded` ret `ntdll+0x2319e` 에서 `r eax`, 간선 직후 `ntdll+0x22a58`, 부모 카운터 `ntdll+0x22d8f`. 안 서야 할 곳 `0x103` 길 `ntdll+0x230c7`, 마지막 참조 `ntdll+0x2332d`, 바로 올리기 `ntdll+0x23759` |
| kernel32 import 가 api-set 인가, `KERNELBASE.dll` 로 풀리나, kernelbase 가 KnownDll 로 올라오나 | kernel32 import 표를 안 봤다. 실행 때 값 | 동적 **Q20**: `ApiSetpResolveHost` ret `ntdll+0x21daf` 에서 `r eax`·`db @rsp+0x50 L1`·`dS @rsp+0x108`, KnownDll ret `ntdll+0x23242` 에서 `r eax` |
| 쪽지를 붙일 때 **정말 잠금을 잡은 채**인가, 어느 직원인가 | 실행 때 값 | 동적 **Q47**: call `ntdll+0x232d7` 에서 `dq ntdll!LdrpModuleDatatableLock L1`(비트 0), `~`, `kb`. ret `ntdll+0x232dc` 에서 자식 `State` |
| 왜 KnownDll 을 "새로" 안에서 새 명부 줄을 만든 **뒤** 시도하나 | 코드에 이유가 쓰여 있지 않다. 2.4 의 짐작은 인자 모양에서 읽은 `[추측]` | `LdrpLoadKnownDll` 본문 (대기열 줄은 따로 없다) |
| 이름·뜻 못 정한 것: 지역 flags 비트 `0x4`·`0x20`·`0x200`·`0x400`·`0x8000`·`0x800000`·`0x4000000` 의 이름, 간선 칸 구조체 이름과 비트 0, `ApiSetMap` 버전 갈래, `0xc000022d`·`0xc0150014` 이름, TEB `+0x1850` 8칸 표 (SRW 락 소유 추적 `[추측]`), 문맥 `+0xb8`(-1) | PDB 에 이름이 없거나(간선 칸, `_LDRP_LOAD_CONTEXT`), 공개 상수 이름에 기댄다 | flow.md 4장 "이름·뜻 못 정한 것 (run23)" 줄 (대기열 줄은 따로 없다) |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run23-load-dependent.md`](run23-load-dependent.md) — 특히 "갈림길", "호출 지점", "간선(의존 관계)을 적는 줄", "세 갈림", "일감 vs 바로", "남은 자식 수와 기다림", "ApiSet", "test.exe → dll1 → dll2 이야기", "정정", "아직 확인 필요"(멈춤점 목록)
- 정적 요청서: [`_workspace/sast/18_load-dependent.md`](../../_workspace/sast/18_load-dependent.md) — 질문 3 이 "나란한 세 갈래" 틀로 물은 곳
- 정적 결과서 (질문 1 ~ 8, 3절 "중요한 정정", 판단 Q12c·Q14): [`_workspace/sast/18_load-dependent_result.md`](../../_workspace/sast/18_load-dependent_result.md)
- 앞 바퀴 해설:
  - [`run22-map-snap-dep-해설.md`](run22-map-snap-dep-해설.md) — 2.4 간선·부모/자식·인자 6개, 2.5 이야기 그림의 `?` 칸, 2.6 체크판·두 번째 쪽지(`+0x4f7`), 2.7 명부 줄 + 카드 한 쌍
  - [`run21-load-dll-internal-해설.md`](run21-load-dll-internal-해설.md) — 2.3 "찾거나 준비", 2.4 KnownDll 길·파일 길, 2.5 의존 DLL 길, 2.6 진행 도장, 2.10 적재 이유
  - [`run20-kernel32-load-해설.md`](run20-kernel32-load-해설.md) — 2.3 해시 표(이름 색인 카드함), 2.4 순서 문제, 2.12 `0xc0000135`
  - [`run19-proc-init-modules-해설.md`](run19-proc-init-modules-해설.md) — 2.6 작업 지시서·연결 카드, 2.9 일꾼·게시판, 2.11 활성화 컨텍스트
  - [`run18-proc-init-setup-해설.md`](run18-proc-init-setup-해설.md) — 2.4 묶음 9 동네 공용 창고(`\KnownDlls`)
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "두 그림 사이 설명" run23 문단
  - "그림 2" `ja3`·`jd1`~`jd4`, `jb4`·`je1`~`je4`, `jb2`·`jc1` 앞 `?` 화살표
  - "2.5" run23 세 덩어리 (KnownDll·일감 조건, api-set 자리, 간선 "맞았다")
  - "2.6" 새 소절 `ntdll!LdrpLoadDependentModuleInternal`, 바깥 고리 소절의 체크판 줄, 이야기 그림
  - "3" 일꾼 줄의 run23 세 줄
  - "4. 공백 목록" 2.6 줄들, "4.1" `LdrpLoadDependentModuleInternal` → (1) ~ (5) 줄, `LdrpQueueWork → ?` 줄
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q12b(끝), **Q12c(다음)**, Q50(새), Q14·Q20·Q23·Q47 출처

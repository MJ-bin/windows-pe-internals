# run22 map-snap-dep — 해설

> 이 글은 [run22 문서](run22-map-snap-dep.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q12a** — 2.5·2.6 import 를 하나씩 도는 바깥 고리 `ntdll!LdrpMapAndSnapDependency` (호출 23곳) + 노드·항목을 만드는 `LdrpAllocateModuleEntry` (5곳) (`_workspace/plan.md`, 이번에 **끝**)
- 근거:
  - run 문서 [`run22-map-snap-dep.md`](run22-map-snap-dep.md) (가장 중요)
  - 요청서 [`_workspace/sast/17_map-snap-dep.md`](../../_workspace/sast/17_map-snap-dep.md)
  - 결과서 [`_workspace/sast/17_map-snap-dep_result.md`](../../_workspace/sast/17_map-snap-dep_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md`
    - Q12 를 **Q12a·Q12b 로 나눴고**, 이번에 Q12a 가 끝났다.
    - **새 항목은 없다.** 출처를 더한 곳은 Q12b·Q14·Q23·Q46·Q47.
    - 다음 항목은 **Q12b**.
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll (`ntdll.dll`, 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`). run17 ~ run21 과 같은 파일이다.
  - 본문을 읽은 건 두 함수다.
    - `LdrpMapAndSnapDependency` (rva `0x210a0`, 1544바이트, 직접 호출 23줄)
    - `LdrpAllocateModuleEntry` (rva `0x6fab0`, 412바이트, 직접 호출 5줄)
  - 확인용으로 `LdrpMapDllWithSectionHandle` 의 `.asm` 도 봤다 ("누가 이 함수를 다시 부르나" 를 보려고).
  - 안쪽 `LdrpLoadDependentModuleInternal` 은 **본문을 안 읽었다.** 로드 문맥 `+0x30` 칸이 무엇인지 정하려고 **몇 줄만** 봤다.

이 글은 [run21 해설](run21-load-dll-internal-해설.md) 의 **다음 편**이다.

- run21 에서 배달 창구(`LdrpLoadDllInternal`) 아래를 노선도로 따라가다가, 배달 기사의 업무 매뉴얼에 "**건물을 갖다 놓은 뒤, 그 건물이 기대는 건물 목록을 보고 따라 가져온다**" 는 절차가 있다는 걸 봤다 ([run21 해설 2.5](run21-load-dll-internal-해설.md)). 그때는 이름만 봤다.
- 이번엔 그 절차를 맡은 **`LdrpMapAndSnapDependency` 의 본문을 처음 펼쳤다.**

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출, 비트와 플래그 → [run9 해설](run9-create-user-process-해설.md) 2절
  - 부르는 자리와 돌아올 자리 → [run9 해설](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)
  - 오프셋 순서 ≠ 실행 순서 → [run9 해설](run9-create-user-process-해설.md) 3.7
- PEB, 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
- 잠금 → [run11 해설 2.8](run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다)
- 모듈 목록(PEB.Ldr)·모듈 항목(명부 줄) → [run18 해설 2.6](run18-proc-init-setup-해설.md#26-pebldr--건물-명부가-생기는-순간-0x1c1)
- 로드 문맥(작업 지시서), DDAG 노드(연결 카드) → [run19 해설 2.6](run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1)
- snap(전기 연결), IAT(벽 콘센트) → [run19 해설](run19-proc-init-modules-해설.md) 2.7
- 병렬 로딩, 일감·게시판(`LdrpQueueWork`, `LdrpDrainWorkQueue`) → [run19 해설](run19-proc-init-modules-해설.md) 2.9
- 팻말 `LdrInitState` → [run19 해설 2.12](run19-proc-init-modules-해설.md#212-팻말--ldrinitstate-1--2--3)
- 지도 이름 사슬, KnownDll 길·파일 길, 설치 담당 `LdrpMapDllWithSectionHandle`, 의존 DLL 처리 → [run21 해설](run21-load-dll-internal-해설.md) 2.4·2.5
- 노드 상태 번호(진행 도장) → [run21 해설](run21-load-dll-internal-해설.md) 2.6
- 적재 이유 `LoadReason`(주문 사유), 패치 주문(이유 9) → [run21 해설](run21-load-dll-internal-해설.md) 2.10

비유도 그대로 이어 쓴다.

- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- DLL = **건물**, 모듈 항목 = **명부 한 줄**, DDAG 노드 = 명부 줄마다 붙은 **연결 카드**, 로드 문맥 = **작업 지시서** (run18·run19)
- import 목록 = 건물의 **"기대는 건물 목록"**, IAT = 벽 **콘센트**, snap = 콘센트에 **전선 꽂기** (run19·run21)
- 아래에서 실제로 올리는 함수들 = **배달 기사**, `LdrpMapDllWithSectionHandle` = **설치 담당** (run20·run21)
- `LdrpQueueWork` = 일꾼 **게시판에 일감 쪽지 붙이기** (run19)
- 노드 `State` = 연결 카드의 **진행 도장** (run21)
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- run 문서는 이번에 "**이삿짐 목록표**" 비유를 썼다. 이 글은 그걸 가게 그림으로 옮긴다 (가리키는 건 같다).
  - 이삿짐 목록표 = 건물의 "기대는 건물 목록" (import 표)
  - "이 짐 가져와" 심부름꾼 = 배달 기사 (`LdrpLoadDependentModuleInternal`)
  - 마지막에 전기·수도 잇기 = 콘센트에 전선 꽂기 (snap)
  - "이어 달라" 쪽지를 넣는 일감 상자 = 게시판

한 줄로 먼저 말하면:

- `LdrpMapAndSnapDependency` 는 이런 함수다 `[정적]`:
  - 건물 하나의 **기대는 건물 목록을 한 줄씩** 읽는다 → 줄마다 **배달 기사에게 맡긴다** → 맡긴 게 다 끝났으면 **마지막에 한 번** 전선을 꽂는다 (snap).
- 이번 바퀴의 **핵심 셋**:
  1. 목록 한 줄마다 도는 고리(`+0x1d0 ~ +0x3a3`)가 **갈림 없이** `LdrpLoadDependentModuleInternal` 을 부른다. 이 함수엔 "누가 누구에 기대나" 를 **적는 줄이 없다.** 대신 **부모 정보**(부모 작업 지시서·부모 명부 줄)를 안쪽에 넘긴다 → 적는 일은 안쪽일 것 `[추측]` (Q12b) → 2.4
  2. 같은 함수가 **올라온 DLL 마다 다시 불린다.** 그래서 test.exe → dll1 → dll2 가 줄줄이 내려가는 코드 모양이다 → 2.5
  3. 끝맺음 갈림. **부모가 없으면**(exe, kernel32) 바로 snap, **있으면**(kernelbase, dll1, dll2) 일감 쪽지로 넘긴다. 병렬 로딩이 켜졌는지는 **이 함수가 안 본다** → 2.6
- 덤: `LdrpAllocateModuleEntry` 는 **명부 줄과 연결 카드를 한 쌍으로** 만든다. 카드엔 아직 선이 없다 → 2.7

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다. run19·run21 해설에서 `+0x1aa4`·`+0x1aed` 로 쓴 건 같은 호출의 **부르는 자리(call)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)              (run17 ~ run19)
        ├─ +0x135e  LdrpAllocateModuleEntry (exe 명부 줄)                            ← 이번 (안쪽)
        ├─ +0x19a2  LdrpInitializeKernel32Functions                                (run20)
        │     └─ +0xc0 LdrLoadDll("KERNEL32.DLL") → … LdrpLoadDllInternal           (run21)
        │           └─ … LdrpMapDllWithSectionHandle +0x1c8
        │                 └─ LdrpMapAndSnapDependency (kernel32 지시서)              ← 이번 (같은 함수)
        ├─ +0x1a47  LdrpEnableParallelLoading                                       (run19)
        ├─ +0x1aa9  LdrpMapAndSnapDependency (exe 지시서)                            ← 이번 (run22)
        │     └─ +0x34e LdrpLoadDependentModuleInternal                             ← 다음 (Q12b)
        ├─ +0x1ab1  LdrpDrainWorkQueue(1)                                           (run19)
        ├─ +0x1af2  LdrpPrepareModuleForExecution (exe 항목)                         (run21)
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16, 뒤는 추측)
```

- 이번 자리는 **짐 들이기**와 **전원 켜기** 사이다.
  - 앞: kernel32 준비 (run20·run21)
  - 이번: exe 가 기대는 DLL(dll1·dll2)을 들여오고 전선을 꽂는 바깥 고리
  - 뒤: 전원 켜기 `+0x1bbb` (run19·run21)
- flow.md 로는 **2.6** (`[공백]` import DLL 이 메모리에 올라오는 순간) 이 중심이고, 2.5 (kernelbase 가 따라 올라오는 길) 에도 한 줄 더해졌다.

### 무엇이 궁금했나

run 문서 "목적" 그대로.

1. exe 의 import 를 **어떤 순서로** 도나, 하나마다 **무엇을** 부르나.
2. "누가 누구에 기대나"(그래프 간선)를 **적는 자리가 여기인가.**
3. **같이 올리기 vs 일감으로 넘기기**가 어디서 갈리나.
4. 그래프 노드는 **어디서 태어나나** (`LdrpAllocateModuleEntry`).
5. 덤: 2.5 kernelbase 가 kernel32 와 **같은 호출 안에서** 따라 올라오는 길을 이 함수 기준으로 다시 본다.

### 먼저 — 왜 Q12 를 둘로 나눴나

원래 대기열 Q12 는 한 줄이었다: "**올리면서 의존 관계 그래프를 만드는 함수**". run21 이 넘긴 후보 이름은 여럿이었다 (`LdrpAllocateModuleEntry`, `LdrpMapAndSnapDependency`/`LdrpSnapModule`/`LdrpLoadDependentModuleInternal`, `LdrpCondenseGraph` → `LdrpCondenseGraphRecurse` → `LdrpMergeNodes`).

나눈 이유는 둘이다 (`plan.md` 결정 기록, 2026-10-23).

1. **안쪽 함수 하나가 너무 크다.**
   - `LdrpLoadDependentModuleInternal` 혼자 **9250바이트, 호출 75곳**이다.
   - 바깥 두 함수는 합쳐서 23 + 5 곳이다.
   - 한 바퀴에 같이 보면 이해가 따라오기 어렵다. 그래서 그 자리에서 나눴다.
2. **"의존 관계를 더한다" 는 이름의 함수가 따로 없다.**
   - `_index.tsv` 에 "AddDependency" 같은 이름이 없다.
   - 비슷한 이름은 `LdrpRecordModuleDependency`·`LdrpDependencyExist` 뿐이다 (결과서가 `_index.tsv` 로 다시 맞춤). 그런데 `LdrpRecordModuleDependency` 의 caller 는 `LdrpInitializeNtdllDataTableEntry` 하나뿐이다.
   - → 이름으로 "여기서 선을 긋는다" 를 고를 수 없다. 어떤 함수의 **본문 안**에서 그을 것 `[추측]`.
   - 그러니 바깥부터 차례로 열어 보기로 했다.

그래서:

- **Q12a (이번)** = 바깥 고리 `LdrpMapAndSnapDependency` + 명부 줄·연결 카드를 만드는 `LdrpAllocateModuleEntry`
- **Q12b (다음)** = 안쪽 배달 기사 `LdrpLoadDependentModuleInternal` 하나
- 비유: 큰 창고 정리를 할 때, 바깥 선반부터 무엇이 어디로 가는지 보고 나서 제일 큰 상자를 연다.

### 바퀴 전에 알던 것

- `[정적]` (run19) `LdrpInitializeProcess +0x1aa9` 가 `LdrpMapAndSnapDependency` 를 부른다. IL-only 가 아닐 때. 첫 인자는 exe 명부 줄이 아니라 **작업 지시서** `[RSP+0x2b0]`.
  - callee 이름(`LdrpGetImportDescriptorForSnap`, `LdrpLoadDependentModuleInternal`, `LdrpSnapModule`, `LdrpQueueWork`, `LdrpPrepareImportAddressTableForSnap`)만 봤다. 본문은 안 봤다.
- `[추측]` (run19·run21) 그 작업 지시서와 exe 명부 줄이 **서로 묶여 있을 것**이다.
- `[정적: 지도]` (run21) 같은 `LdrpMapAndSnapDependency` 가 kernel32 를 올리는 길 안(`LdrpMapDllWithSectionHandle +0x1c8`)에도 있다. 그 아래에서 다시 올리기 함수로 되돌아가는 **재귀 모양**이 지도에 있다.
- `[정적]` (run21) 노드 상태 번호 5 ~ 9 의 PDB 이름과, 켜기 준비 담당이 그 번호로 갈리는 모양.
- `[관찰]` (run8) `+0x1bb6` 에서 멈췄을 땐 dll1·dll2 가 **이미 올라와 있었다.** `LdrpLoadDll` 은 안 탔다. 켜는 순서는 dll2 → dll1.
- `[소스]` test.exe 가 직접 import 하는 건 dll1 뿐이고, dll2 는 dll1 을 거쳐 **간접으로** 붙는다.

---

## 2. 찾은 것

### 2.1 한눈에 — `LdrpMapAndSnapDependency` 트리

run 문서 "순서" 트리에서 잡일을 줄였다. 오프셋은 **ret** 다. 위에서 아래가 **실행 순서**다 (결과서 순서 1 ~ 13).

```
LdrpMapAndSnapDependency(지시서)                     E = 지시서+0x38 (이번에 맡은 명부 줄)
├─ +0x63   LdrpFindDllActivationContext(E)            [지시서+0x20 & 0x800000 꺼짐, E.LoadReason ≠ 9 일 때]
├─ +0x9e   LdrpPrepareImportAddressTableForSnap(지시서) IAT 를 찾아 쓰기 가능으로
├─ +0x10e  LdrpQueryCurrentPatch(..)                  [LdrpIsHotPatchingEnabled 켜졌을 때만]
├─ +0xd2   LdrpShouldModuleImportBeRedirected(E)
├─ +0xe5   LdrpGetImportDescriptorForSnap(지시서)       import 서술자 배열
├─ 세는 고리 +0x122 ~ +0x14d (호출 없음)
├─ +0x17b  RtlAllocateHeap → 지시서+0x58 (자식 칸 배열)  지시서+0x6c = 올릴 수 + 1
├─ ★ import 마다 도는 고리 +0x1d0 ~ +0x3a3
│    └─ +0x34e  LdrpLoadDependentModuleInternal(..)    ← 매 바퀴, 갈림 없음
├─ +0x5d8  LdrpLoadDependentModuleW(..)               [핫패치 바이트 ≠ 0 일 때만]
├─ +0x461 ~ +0x47d  잠금 안에서 지시서+0x6c 를 1 줄임
└─ 끝맺음  남은 수 == 0 → State = 4 → 지시서+0x30 == 0 ? +0x4c2 LdrpSnapModule : +0x4f7 LdrpQueueWork
```

읽을 때 주의:

- **전부 `[정적]`** 이다. 실행 때 이 순서로 탔는지는 아무도 안 봤다.
- `+0x10e` 가 `+0xd2` 보다 **위**에 있다. 오프셋은 큰데 먼저 실행되는 줄이다 ([run9 해설](run9-create-user-process-해설.md) 3.7, 오프셋 순서 ≠ 실행 순서). 트리는 결과서의 실행 순서를 따랐다.
- 23줄 중 빠진 건: 이름 문자열 잡일 8곳, `RtlFreeHeap` (`+0x451`), 실패 로그 `LdrpLogEtwHotPatchStatus` (`+0x604`), 쿠키 검사 (`+0x7e`). 전체는 run 문서 표.
- 이 함수는 **반환값이 없다.** 결과 상태는 지시서 `+0x28` 이 가리키는 칸에 적는다 `[정적]`. run20 의 **결과 메모지**와 같은 방식이다.

트리에 처음 나온 말 넷. 앞의 둘이 이번 이야기의 바탕이고, 뒤의 둘은 곁가지(2.8)다.

- **새 말 — import 서술자**: exe·DLL 안에 있는 import 표의 **한 줄**. 기대는 DLL **하나**마다 한 줄이다. 한 줄은 0x14 바이트이고, 그 안에 DLL 이름 위치(`+0xc`)와 그 DLL 몫 IAT 칸 위치(`+0x10`, FirstThunk)가 있다 `[정적]` (결과서).
  - 비유: "기대는 건물 목록" 의 **한 줄** — "옆 건물 이름 / 그 건물 전선을 꽂을 콘센트 줄은 여기".
- **새 말 — IAT 를 쓰기 가능으로**: snap 은 IAT 칸에 주소를 **써 넣는** 일이다 ([run19 해설](run19-proc-init-modules-해설.md) 2.7). 그런데 그 칸이 든 페이지가 쓰기 금지일 수 있다. 그래서 **먼저 쓰기 가능으로 바꿔 둔다** (2.8) `[정적, .c]`.
  - 비유: 전선을 꽂기 전에 콘센트 **덮개부터 연다.**
- **새 말 — 활성화 컨텍스트**: 어떤 DLL 버전을 쓸지 정하는 **매니페스트 설정 묶음**으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). `LdrpFindDllActivationContext` 본문은 "매니페스트 찾기 루틴을 부르고 명부 줄 `+0x88` 에 넣는다" 로 읽혔다 `[정적, .c]`.
- **새 말 — 핫패치 / 리다이렉션**: 핫패치는 **돌아가는 중에 고친 판(패치)을 끼우는** 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 리다이렉션은 이름으로는 import 를 **다른 곳으로 돌리는** 일이다 `[추측: 이름]`. 둘 다 전역 값이 0 이면 안 탄다 (2.8).

### 2.2 한 줄씩 읽는 법 — 대표 셋

"누가 / 어디서(+0x) / 누구를 / 무엇을 위해" 로 읽는다. **누가**는 모두 `LdrpMapAndSnapDependency` 다.

**`+0x9e LdrpPrepareImportAddressTableForSnap`** `[정적]`

- 어디서: 부르는 자리 `+0x99`, 돌아올 자리 `+0x9e`.
- 누구를: `LdrpPrepareImportAddressTableForSnap(지시서)`.
- 무엇을 위해: 이 건물의 IAT 를 찾아 **지시서 `+0x70`(시작)·`+0x78`(크기)** 에 적고, 쓰기 가능으로 바꾼다 (`.c`).
- 갈림: 결과가 음수면 결과 메모지에 적고 끝낸다.
- 비유: 콘센트 줄이 어디부터 어디까지인지 지시서에 적고, 덮개를 연다.

**`+0x34e LdrpLoadDependentModuleInternal`** `[정적]` — 이번 바퀴의 주인공

- 어디서: 부르는 자리 `+0x349`, 돌아올 자리 `+0x34e`. import 고리 `+0x1d0 ~ +0x3a3` **안**이다.
- 누구를: `LdrpLoadDependentModuleInternal(자식 이름, 지시서, E, 0, &지시서.58[i], &임시)` — 인자 6개 (2.4 표).
- 무엇을 위해: 목록 한 줄(기대는 DLL 하나)을 **배달 기사에게 맡긴다.**
- 갈림: **없다.** 서술자마다 부른다. 단, 그 서술자의 IAT 첫 칸이 0 이면 그 바퀴는 건너뛴다.
- 결과가 음수면 자식 칸을 0 으로, 결과 메모지에 상태를 적고 **고리를 나간다.**

**`+0x4c2 LdrpSnapModule` / `+0x4f7 LdrpQueueWork`** `[정적]` — 같은 자리의 두 갈래

- 어디서: 부르는 자리 `+0x4bd` / `+0x4f2`, 돌아올 자리 `+0x4c2` / `+0x4f7`. 고리 **밖**, 함수 끝맺음이다.
- 누구를: 둘 다 인자는 지시서 하나.
- 무엇을 위해: 이 건물 **자기 자신의** 전선 꽂기를 **지금 직접 하나** (`LdrpSnapModule`), **게시판에 쪽지로 넘기나** (`LdrpQueueWork`).
- 갈림: 남은 자식 수가 0 일 때만 둘 중 하나. 지시서 `+0x30` 이 0 이면 앞쪽, 아니면 뒤쪽 (2.6).

#### 비유로 다시 한 번 — 트리 전체

1. 매니페스트 설정을 찾아 둔다 (필요할 때만)
2. 콘센트 줄을 찾아 **덮개를 연다** (`+0x9e`)
3. 수리판(핫패치)·돌림(리다이렉션) 표시를 본다 — 보통은 해당 없음
4. 기대는 건물 목록(import 서술자)을 꺼낸다 (`+0xe5`)
5. 목록을 한 번 **세기만** 한다 — 몇 줄인가, 실제로 가져올 건 몇 개인가
6. 자식 칸 배열을 만들고, "아직 안 끝난 수" 를 적는다
7. 목록을 한 줄씩 읽으며 **배달 기사에게 맡긴다** (`+0x34e`)
8. 내 몫 1 을 뺀다 (잠금 안에서)
9. 다 끝났으면 진행 도장 "**꽂는 중**"(4) 을 찍고, 내가 직접 꽂거나(`+0x4c2`) 게시판에 넘긴다(`+0x4f7`)

### 2.3 작업 지시서의 칸 — 이름 없이 오프셋 + 역할로만

2.4 ~ 2.6 을 읽으려면 작업 지시서(로드 문맥)의 칸 몇 개를 알아야 한다.

**먼저, 왜 칸 이름이 없나**

- 지시서 구조체 `_LDRP_LOAD_CONTEXT` 는 PDB 에 **이름만 있고 칸 정보가 없다** (앞선 선언만) `[정적]`.
- 그래서 칸 이름을 **지어 붙이지 않는다.** 아래는 코드가 그 칸을 **쓰고 읽는 줄**에서 역할을 읽은 것이다.
- 비유: 서식은 있는데 칸 머리글이 지워진 지시서. "세 번째 칸엔 늘 부모 건물 명부 줄이 적힌다" 는 식으로만 말할 수 있다.

**이번에 필요한 칸** (전체 표는 run 문서)

| 칸 | 역할 | 표시 |
|---|---|---|
| `+0x20` | 플래그. exe 처음 값 `0x200` | `[정적]` (비트 뜻은 `[추측]`) |
| `+0x28` | 결과 상태를 써 넣을 곳(결과 메모지)의 포인터 | `[정적]` |
| **`+0x30`** | **부모 명부 줄** = 이 DLL 을 import 한 쪽의 `_LDR_DATA_TABLE_ENTRY*`. exe 길·`LdrLoadDll` 길은 0 | `[정적]` (2.4) |
| `+0x38` | 이 지시서의 명부 줄 (이번에 맡은 건물) | `[정적]` |
| `+0x58` | 자식 명부 줄을 받을 칸 배열 (서술자 수 × 8바이트) | `[정적]` |
| `+0x68` | import 서술자 수 | `[정적]` |
| **`+0x6c`** | **아직 안 끝난 자식 수 + 1** (2.6) | `[정적]` (값 이름은 `[추측]`) |
| `+0x70` / `+0x78` | IAT 시작 / 크기 | `[정적]` |
| `+0x88` | import 서술자 배열 포인터 | `[정적]` |

**서로 가리킨다 — run19 의 `[추측]` 이 `[정적]` 이 됐다**

- `LdrpAllocateModuleEntry +0x7f` 가 명부 줄 `+0xb0 LoadContext` 칸에 **지시서**를 적는다.
- `LdrpAllocateModuleEntry +0x96` 이 지시서 `+0x38` 칸에 **명부 줄**을 적는다.
- → 둘이 **서로 가리킨다** `[정적]`. run19 해설 2.7 단계 5 에서 "지시서와 명부 줄이 서로 묶여 있을 것" 이라고 `[추측]` 으로 둔 관계가 이것이다.
- 비유: 지시서에 "이 명부 줄 것", 명부 줄에 "지시서는 여기" — **클립으로 서로 묶었다.**

### 2.4 핵심 ① import 마다 배달 기사에게 맡긴다 — 선은 여기서 안 긋는다

#### 단계 1 — 목록을 먼저 세기만 한다 `[정적]`

- `+0x122 ~ +0x14d` 의 짧은 고리. **호출이 없다.**
- 서술자를 이름·FirstThunk 가 0 이 아닌 동안 돌며 두 가지를 센다.
  - 서술자 수
  - 실제로 올릴 수 = IAT 첫 칸이 0 이 아닌 서술자 수
- 올릴 수가 0 이고 핫패치 바이트도 0 이면 → 고리를 통째로 건너뛰고 끝맺음으로.

#### 단계 2 — 칸 배열과 "남은 수" 를 준비한다 `[정적]`

- `+0x17b RtlAllocateHeap(LdrpHeap, .., 서술자 수 × 8)` → 지시서 `+0x58`. 자식 명부 줄을 하나씩 받아 둘 칸이다.
- 지시서 `+0x6c` = **올릴 수 + 1**. 이 숫자는 2.6 에서 다시 나온다.

#### 단계 3 — 한 줄씩 맡긴다 `[정적]`

고리 `+0x1d0 ~ +0x3a3` 한 바퀴:

1. 서술자에서 DLL 이름을 꺼낸다. 0x14 바이트씩 차례로.
   - IAT 첫 칸이 0 인 서술자는 건너뛴다. 그래도 **칸 번호는 올라간다.**
   - 이름이 `0xfffe` 바이트보다 길면 `STATUS_NAME_TOO_LONG`(`0xc0000106`) 으로 고리를 나간다.
2. 이름을 ANSI → UTF-16 으로 바꾼다 (잡일 8곳).
3. **`+0x34e LdrpLoadDependentModuleInternal`** 에 맡긴다.
4. 실패면 칸 = 0, 상태 기록, 고리 탈출.

여기서 중요한 건 "**갈림이 없다**" 는 점이다.

- 이 호출 앞에 "병렬로 할까, 직접 할까" 를 묻는 줄이 없다.
- 이 함수가 읽는 전역은 `LdrpIsHotPatchingEnabled`, `NtdllBaseTag`, `LdrpHeap`, `GlobalRtlNlsState`, `LdrpModuleDatatableLock`, `LdrpImageEntry`, `__security_cookie` 뿐이다 `[정적]`.
- `LdrInitState` 도, 병렬 로딩 결과도 안 읽는다.

#### 단계 4 — "누가 누구에 기대나" 를 적는 줄이 없다 `[정적]`

**새 말 — 간선**: 그래프에서 **점과 점을 잇는 선**. 여기서 점 = DDAG 노드(연결 카드), 선 = "A 는 B 에 기댄다" 한 개. 노드 구조체(`_LDR_DDAG_NODE`, PDB)엔 선을 담는 칸이 둘 있다 `[정적]`:

- `+0x28 Dependencies` — 이름으로는 "내가 기대는 쪽들" `[추측: 이름]`
- `+0x30 IncomingDependencies` — 이름으로는 "나에게 기대는 쪽들" `[추측: 이름]`
- 비유: 연결 카드 사이에 긋는 **줄**. 카드 앞면엔 "내가 기대는 건물", 뒷면엔 "나에게 기대는 건물".

이번에 본 것:

- `LdrpMapAndSnapDependency` 가 노드에 쓰는 줄은 **`State`(`+0x38`) 두 곳뿐**이다: `+0x4ac` → 4, `+0x5ae` → 5.
- `Dependencies`·`IncomingDependencies`·`Modules` 에 쓰는 줄이 **없다.**
- `LdrpAllocateModuleEntry` 도 그 두 칸에 쓰는 줄이 **없다** (2.7).
- → **이 두 함수에서는 줄을 안 긋는다** `[정적]`.

#### 단계 5 — 대신 부모 정보를 안쪽에 넘긴다 `[정적: +0x33e ~ +0x349]`

**새 말 — 부모 / 자식**: 여기서는 **import 하는 쪽 = 부모**, **import 되는 쪽 = 자식**이다 (run 문서 표 이름). test.exe 가 dll1 을 import 하니 exe 가 부모, dll1 이 자식이다 `[소스]`.

- 비유: "기대는 건물 목록" 을 가진 건물이 부모, 목록에 적힌 건물이 자식.

`+0x34e` 에 싣는 인자 6개 (run 문서 표 그대로):

| 인자 | 값 | 역할 |
|---|---|---|
| 1 `RCX` | 지역 문자열 | 자식 이름, UTF-16 |
| 2 `RDX` | 지시서 | **부모의 작업 지시서** |
| 3 `R8` | `R13` = 지시서 `+0x38` | **부모 명부 줄** |
| 4 `R9D` | 0 | 적재 이유 = `LoadReasonStaticDependency`(0). `Internal +0x1179` 가 새 명부 줄 `LoadReason` 에 씀 (줄만) |
| 5 `[RSP+0x20]` | `&지시서.58[i]` | 자식 명부 줄을 돌려받는 칸 |
| 6 `[RSP+0x28]` | `&[RSP+0x48]` | 임시 포인터 (끝에 `+0x451` 에서 놓음) `[추측]` |

- 인자 4 의 0 은 run21 의 주문 사유와 같은 칸이다. kernel32 주문은 4(`DynamicLoad`), 여기 import 는 0(`StaticDependency`).
- 비유: 배달 기사에게 "이 건물(자식) 가져와" 쪽지를 주면서 **부모 지시서와 부모 명부 줄을 같이** 건넨다. "누구 짐인지" 를 기사가 알게 된다.

#### 단계 6 — 지시서 `+0x30` = 부모 명부 줄, 근거 넷 `[정적]`

자식 쪽 지시서의 `+0x30` 칸에 부모 명부 줄이 들어간다. 왜 그렇게 말할 수 있나:

1. `LdrpLoadDependentModuleInternal +0x1100 ~ +0x1108`: 새 자식 지시서 `+0x30` 에 **세 번째 인자(부모 명부 줄)** 를 넣는다.
2. `LdrpAllocateModuleEntry +0x9a ~ +0xbb`: 지시서 `+0x30` 이 있으면 그 명부 줄의 활성화 컨텍스트(`+0x88`)를 **물려받는다.**
3. `LdrpMapDllWithSectionHandle +0x286 ~ +0x28f`: 지시서 `+0x30` 이 있으면 자식 명부 줄 `ParentDllBase`(`+0xb8`, PDB 이름) = 부모 `DllBase`.
4. 0 인 길도 확인됐다:
   - `LdrpLoadDll +0xf6` 은 이 칸으로 갈 값에 0 을 넣는다 → `LdrLoadDll` 길(kernel32 등)은 0.
   - exe 지시서는 `LdrpInitializeProcess` 앞쪽 `memset(.., 0, 0xd0)` (call `+0x105`, ret `+0x10a`) 으로 0 이 되고, 뒤쪽 `+0x133b ~ +0x1349` 는 이름·경로·`0x200`·상태 포인터만 채운다 → exe 도 0.

PDB 칸 이름 `ParentDllBase` 가 3번에서 나온 게 이 해석과 잘 맞는다. 하지만 지시서 `+0x30` 칸 자체엔 여전히 **이름을 붙이지 않는다** (2.3).

#### 그래서 — 단계별로

1. 바깥 고리는 줄마다 **무조건** 배달 기사를 부른다 `[정적]`.
2. 바깥 고리와 명부 줄 만들기엔 **선을 긋는 줄이 없다** `[정적]`.
3. 부모 정보(부모 지시서·부모 명부 줄)는 **배달 기사에게 넘어간다** `[정적]`.
4. → 그러니 선은 **배달 기사(`LdrpLoadDependentModuleInternal`) 안**에서 그을 것이다 `[추측]`.
   - 왜 짐작인가: 그 본문을 안 읽었다. 결과서는 다른 후보도 남겼다 — 안쪽에 없으면 `LdrpCondenseGraph`·`LdrpSnapModule` 쪽일 수도 있다 (결과서 Q12b 질문 1).
   - 확인은 **Q12b** (정적, 다음 항목).

#### 비유로 정리

- 건물 담당이 "기대는 건물 목록" 을 한 줄씩 읽는다.
- 줄마다 배달 기사에게 쪽지를 준다. 쪽지엔 "가져올 건물 이름 + **부모 지시서·부모 명부 줄**".
- 건물 담당 자신은 연결 카드에 **줄을 긋지 않는다.** 진행 도장만 찍는다.
- 줄은 아마 쪽지를 받은 배달 기사가 긋는다 — 다음 편에서 확인.

### 2.5 핵심 ② 같은 함수가 올라온 DLL 마다 다시 불린다

#### 설치 담당이 마지막에 하는 일 `[정적]`

`LdrpMapDllWithSectionHandle` ([run21 해설 2.4](run21-load-dll-internal-해설.md) 의 "설치 담당") 의 `.asm` 을 이번에 직접 봤다. 순서:

1. 갖다 놓고, 명부·색인에 끼운다 (`+0x146 LdrpInsertDataTableEntry`, `+0x151 LdrpInsertModuleToIndexLockHeld`).
2. `+0x24f LdrpCompleteMapModule` → `+0x26c LdrpProcessMappedModule`.
3. 명부 줄 `Flags` 비트 24 `CorILOnly` 가 **꺼져** 있으면 → **`+0x1c8 LdrpMapAndSnapDependency(지시서)`**.

**새 말 — `CorILOnly`**: 명부 줄 `Flags` 의 비트 24, PDB 이름이다 `[정적]`. 이름으로는 ".NET 중간 코드(IL)만 든 모듈" 표시 `[추측: 이름]`. run19 의 "IL-only 아니면" 과 같은 갈래다.

다른 길로 빠지는 경우 `[정적]`:

- `CorILOnly` 가 **켜져** 있으면 → 대신 `+0x2d5 LdrpCorProcessImports`.
- 같은 모듈이 이미 목록에 있으면 → `+0x3c3 LdrpLoadContextReplaceModule`. 위 둘 다 안 탄다.
- 이미지로 안 올린 것은 `State = 9` 만 쓰고 안 부른다 (`+0x387 ~ +0x3ac`).

#### 그래서 줄줄이 내려간다 `[정적: 코드 모양]`

- 보통 DLL 이면 설치 담당이 **매번** `LdrpMapAndSnapDependency` 를 부른다.
- 그 함수는 그 DLL 의 "기대는 건물 목록" 을 돌며 다시 배달 기사를 부른다.
- 배달 기사가 그 자식을 올리면, 또 설치 담당 → 또 `LdrpMapAndSnapDependency`.
- 이렇게 **같은 함수가 자기 안쪽 깊은 곳에서 다시 불리는 모양**을 재귀라고 한다. run21 의 "재귀 모양" 이 이번엔 **바깥 고리 쪽 코드로** 보였다.

#### test.exe → dll1 → dll2 이야기 (run 문서 그림 그대로)

읽는 법:

- 들여쓰기가 깊을수록 **안쪽 호출**이다.
- `?` = **모르는 칸**이다. 안 읽은 함수 안이거나, 실행 때 값이다.
- 화살표 사이에 다른 함수가 더 있을 수 있다 (`LdrpLoadDependentModuleInternal` 본문은 안 읽음).
- 어느 바퀴가 dll1·dll2 인지는 소스의 의존 관계로 짐작한 것이다 `[소스]` + `[추측]`.

```
exe문맥 (+0x30 = 0, +0x38 = exe 항목)
LdrpMapAndSnapDependency(exe문맥)                                  ← LdrpInitializeProcess +0x1aa9
  IAT 쓰기 가능 · import 서술자 읽기
  고리 한 바퀴 (import = dll1.dll [소스])
    +0x34e LdrpLoadDependentModuleInternal("dll1.dll", exe문맥, exe 항목, 0, &exe문맥.58[i])
      ├ 새 dll1문맥 (+0x30 = exe 항목) → +0x115c LdrpAllocateModuleEntry → dll1 항목 + 노드 (State 0 ?)
      ├ ? 간선을 적는 줄 (Q12b)
      └ ? … LdrpMapDllWithSectionHandle   (+0x28f: dll1.ParentDllBase = exe.DllBase)
            └ +0x1c8 LdrpMapAndSnapDependency(dll1문맥)                ← 같은 함수가 dll1 에 대해
                 고리 한 바퀴 (import = dll2.dll [소스])
                   +0x34e LdrpLoadDependentModuleInternal("dll2.dll", dll1문맥, dll1 항목, 0, ..)
                     ├ 새 dll2문맥 (+0x30 = dll1 항목), dll2 항목 + 노드
                     └ ? … LdrpMapDllWithSectionHandle
                           └ +0x1c8 LdrpMapAndSnapDependency(dll2문맥)
                                 (dll2 의 import ?)  남은 수 0 이면 State = 4
                                 +0x30 ≠ 0 → +0x4f7 LdrpQueueWork(dll2문맥)
                 남은 수 = ?  0 이면 State = 4 → +0x30 ≠ 0 → +0x4f7 LdrpQueueWork(dll1문맥)
  남은 수 = ?  0 이면 State = 4 → +0x30 == 0 → +0x4c2 LdrpSnapModule(exe문맥)
```

한 층씩 풀면:

1. **바깥층 (exe)** — `LdrpInitializeProcess +0x1aa9` 가 exe 지시서로 부른다. 부모 칸 `+0x30` 이 0 이다.
2. exe 목록의 한 줄이 dll1 이다 `[소스]`. 배달 기사에게 맡기면서 **부모 = exe** 를 넘긴다.
3. 배달 기사가 dll1 용 **새 지시서**(`+0x30` = exe 명부 줄)를 만들고, `+0x115c` 에서 dll1 명부 줄 + 연결 카드를 만든다 (2.7).
4. 어떤 길로든 설치 담당에 닿으면, 그 `+0x28f` 에서 dll1 명부 줄에 "**부모 번지 = exe 번지**" 를 적는다.
5. **가운데층 (dll1)** — 설치 담당이 `+0x1c8` 에서 **같은 함수**를 dll1 지시서로 부른다.
6. dll1 목록의 한 줄이 dll2 다 `[소스]`. 같은 일을 **부모 = dll1** 로 반복.
7. **안쪽층 (dll2)** — dll2 의 import 는 이번에 안 봤다 (`?`).
8. 각 층의 끝맺음은 2.6 에서.

`?` 로 남긴 것 (run 문서):

- 배달 기사가 **어느 길**(이미 있음 / KnownDll / 새로 올리기)로 설치 담당에 닿나 → Q12b
- 각 시점에 남은 수(`+0x6c`)가 0 인가 → Q12b·Q23
- 일감이 **어느 직원**에서 도나 → Q47
- dll2 의 import 가 무엇인가 (dll2 import 표를 안 봤다)

사이 길은 지도 이름 사슬로만 있다 `[정적: 지도]` (run21 과 같음):

- 파일 길: `LdrpLoadDependentModuleInternal +0x1cae LdrpProcessWork` → `+0x6a LdrpMapDllSearchPath` → `+0x25c LdrpMapDllNtFileName` → `+0x28c LdrpMapDllWithSectionHandle` → `+0x1c8 LdrpMapAndSnapDependency`
- KnownDll 길: `Internal +0x1792 LdrpLoadKnownDll` → `+0xf1 LdrpMapDllWithSectionHandle`

#### run8 관찰과 맞나

- `[관찰]` run8: "`+0x1bb6` 에서 dll1·dll2 가 **이미** 올라와 있었다".
- 이 고리는 `+0x1aa9` **전**에 끝나고, 그 뒤 `+0x1ab1 LdrpDrainWorkQueue(1)` 이 일감을 기다린다 (run19) `[정적]`.
- → **어긋나지 않는다.** 둘 다 `+0x1bb6` 보다 앞이다.

#### 비유로 정리

- 본 건물 담당이 목록을 보고 "dll1 가져와" → 기사가 dll1 을 갖다 놓는다.
- 설치 담당은 갖다 놓을 때마다 **같은 건물 담당 절차**를 다시 시작한다. 이번엔 dll1 의 목록으로.
- dll1 목록의 "dll2 가져와" → 또 갖다 놓고 → 또 같은 절차 (dll2 의 목록).
- 계단을 한 칸씩 내려가는 모양. 맨 아래에서 일이 끝나면 한 칸씩 **올라오며** 끝맺음을 한다.

### 2.6 핵심 ③ 끝맺음 — 부모가 없으면 직접 꽂고, 있으면 게시판에

#### 새 말 — 남은 자식 수 (지시서 `+0x6c`)

- 단계 2 에서 이 칸에 **"올릴 수 + 1"** 을 넣었다 `[정적]`.
- 자식 쪽 코드(`LdrpLoadDependentModuleInternal +0x12df ~ +0x12f7`)가 이 칸을 줄이는 줄이 있다 (줄만 봄).
- 바깥 고리가 끝나면 이 함수는 `+0x461 ~ +0x47d` **잠금 안에서 자기 몫 1 을 줄인다** `[정적]`. 그 잠금 안에서 하는 일은 이것 하나뿐이다.
  - 잠금은 전역 `LdrpModuleDatatableLock` 이다 (`RtlAcquireSRWLockExclusive` / `RtlReleaseSRWLockExclusive`).
- "+1 = 이 함수 자신의 몫" 으로 읽힌다 `[추측: 값 이름]`.
- 결과서엔 핫패치일 때 `+2` 로 시작한다는 줄도 있다 (핫패치 DLL 몫으로 보임 `[추측]`).
- 비유: 건물 담당이 체크판에 "**아직 안 온 짐 수 + 나**" 를 적는다. 짐이 도착할 때마다 하나씩 지우고, 담당도 자기 일을 다 하면 자기 몫을 지운다. **0 이 되면 다 끝난 것.**

왜 "+1" 을 두나 — 내 짐작 `[추측]`:

- 고리를 도는 **도중에** 자식들이 먼저 다 끝나 숫자가 0 이 되면, 아직 맡기지 않은 줄이 남았는데 "끝" 으로 보일 수 있다.
- 담당 자신의 몫 1 을 끝까지 쥐고 있다가 고리 뒤에 놓으면 그런 일이 없다.
- 코드로 확인한 이유는 아니다. 값 이름·뜻은 Q12b 에서 자식 쪽 줄을 읽어야 정해진다.

#### 끝맺음 세 갈래 `[정적]`

| 상황 | 하는 일 | 자리 |
|---|---|---|
| IAT 가 없음 (`지시서+0x70 == 0`) | 노드 `State = 5`(`Snapped`) 만 쓰고 끝. snap 안 부름 | `+0x5ae` |
| 줄인 뒤 남은 수 ≠ 0 | **아무것도 안 하고** 끝 | `+0x49a` |
| 줄인 뒤 남은 수 == 0 | 노드 `State = 4`(`Snapping`) → 아래 갈림 | `+0x4ac` |

- 진행 도장 번호 이름은 PDB `_LDR_DDAG_STATE` 다 `[정적]`. run21 해설 2.6 표(5 ~ 9) 앞쪽이 이번에 나왔다: 0 `PlaceHolder`, 3 `WaitingForDependencies`, 4 `Snapping`, 5 `Snapped`.
- 비유: 연결 카드에 "**꽂는 중**"(4) 도장을 찍고 꽂으러 간다. 꽂을 콘센트가 아예 없는 건물이면 "**꽂기 끝**"(5) 도장만 찍는다.

남은 수가 0 이 아닐 때 그 몫을 **누가 이어받나**는 모른다 `[추측]`.

- 단서 (줄만, run 문서): `LdrpLoadDependentModuleInternal +0x12df ~ +0x12f7` 이 부모 지시서 `+0x6c` 를 줄인다. `+0x1606 ~ +0x1634` 는 부모 노드에 `State = 3`(`WaitingForDependencies`) 을 쓰고 `0x103`(`STATUS_PENDING`) 을 돌려준다.
- "마지막으로 끝나는 자식 쪽 코드가 이어받는다" 로 보이지만 `[추측]` → Q12b.

#### 남은 수 0 일 때의 갈림 — 지시서 `+0x30` 하나 `[정적]`

- `+0x30 == 0` (부모 없음) → **`+0x4c2 LdrpSnapModule(지시서)`** — 지금 직접 꽂는다.
- `+0x30 ≠ 0` (부모 있음) → **`+0x4f7 LdrpQueueWork(지시서)`** — 자기 꽂기를 게시판 쪽지로 넘긴다.

문맥별로 (정적 조건까지):

| 지시서 | `+0x30` | 끝맺음 (남은 수 0 일 때) |
|---|---|---|
| exe (`LdrpInitializeProcess +0x1aa9`) | 0 | `+0x4c2 LdrpSnapModule` 직접 |
| kernel32 (`LdrLoadDll` → `LdrpFindOrPrepareLoadingModule` 지시서) | 0 | `+0x4c2 LdrpSnapModule` 직접 |
| kernelbase 등 kernel32 의 자식 (`Internal` 이 만든 지시서) | kernel32 명부 줄 | `+0x4f7 LdrpQueueWork` |
| dll1 (exe 의 자식) | exe 명부 줄 | `+0x4f7 LdrpQueueWork` |
| dll2 (dll1 의 자식) | dll1 명부 줄 | `+0x4f7 LdrpQueueWork` |

- kernelbase 줄은 "kernelbase 가 kernel32 의 import 이면" 이라는 전제가 붙는다 `[추측: kernel32 import 표를 안 봄]`.
- dll1·dll2 줄은 소스 의존 관계로 맞춘 것이다 `[소스]` + `[추측]`.
- **왜 부모 유무로 가르는지는 코드에 쓰여 있지 않다.** 모른다.

#### 헷갈리기 쉬운 것 둘

**(가) 배달 기사 호출과 게시판 쪽지는 "같은 선택지의 두 갈래" 가 아니다** `[정적]`

- 자식 DLL 을 맡기는 건 **늘** `+0x34e` 직접 호출이다.
- `+0x4f7 LdrpQueueWork` 는 "**이 건물 자신의 전선 꽂기**" 를 넘길까에만 나온다.
- 그러니 "dll1 **올리기 자체**를 일꾼에게 넘기나" 는 이 함수가 아니라 배달 기사 안(`+0x182c LdrpQueueWork`·`+0x1cae LdrpProcessWork`) 몫이다 → Q12b, 동적 Q23·Q47.

**(나) 병렬 로딩이 켜졌는지는 이 함수가 안 본다** `[정적]`

- 2.4 단계 3 의 전역 목록에 병렬 로딩 관련 값이 없다. `LdrpMapDllWithSectionHandle` 쪽도 안 읽는다.
- 그래서 kernel32 준비 때(켜기 `+0x1a42` **앞**)와 exe import 때(켜기 **뒤**)에 **같은 코드**가 돈다.
- 다른 건 두 가지뿐이다: 지시서 `+0x30` 값, 그리고 `LdrpQueueWork` 본문이 일감을 **어디서** 돌리나 (본문 안 읽음).
- 지도에서 본 것 (이름·호출 자리만): `LdrpQueueWork +0x82` → `TpPostWork`, `LdrpProcessWork +0x131` → `LdrpSnapModule`.

여기서 따라 나오는 것 하나:

1. kernelbase 지시서는 부모(kernel32)가 있다 → 끝맺음이 `+0x4f7 LdrpQueueWork` 다 `[정적: 코드 모양]`.
2. 그런데 kernel32 준비는 일꾼 풀을 만들기(`+0x1a42`) **전**이다 (run19).
3. → **풀이 없을 때도 게시판 쪽지를 붙이는 코드 길이 있다.** 그 쪽지를 누가 처리하는지는 모른다 → Q47·Q20.
4. 그래서 run19 의 "kernel32 준비는 일꾼 없이 메인 직원이 한다" 는 여전히 `[추측]` 이다.

#### 비유로 정리

- 짐이 다 왔으면(체크판 0) 도장 "꽂는 중" 을 찍는다.
- **본 건물(exe)이나 관리실이 직접 주문한 건물(kernel32)** 은 부모가 없다 → 담당이 그 자리에서 직접 전선을 꽂는다.
- **누군가의 목록에 딸려 온 건물(kernelbase, dll1, dll2)** 은 부모가 있다 → "이 건물 전선 꽂아 주세요" 쪽지를 게시판에 붙인다.
- 일꾼이 와 있는지는 담당이 확인하지 않는다. 쪽지만 붙인다.

### 2.7 `LdrpAllocateModuleEntry` — 명부 줄과 연결 카드를 한 쌍으로

run19 에서 "exe 명부 줄을 만드는 함수" 로만 봤던 그 함수다 (`LdrpInitializeProcess +0x135e`). 이번에 본문을 봤다.

#### 두 번 빌린다 `[정적]`

| ret | 하는 일 | 크기가 같은 PDB 구조체 |
|---|---|---|
| `+0x2e` | `RtlAllocateHeap(LdrpHeap, .., 0x138)` | `_LDR_DATA_TABLE_ENTRY` (명부 줄) |
| `+0x60` | `RtlAllocateHeap(LdrpHeap, .., 0x50)` | `_LDR_DDAG_NODE` (연결 카드) |

- 카드 할당이 실패하면 `+0x15b` 에서 명부 줄도 놓고 0 을 돌려준다.
- → **명부 줄과 연결 카드는 늘 같이 태어난다** `[정적]`.

#### 처음 적는 값 (run 문서 표에서)

| 대상 | 칸 (PDB) | 값 | 표시 |
|---|---|---|---|
| 명부 줄 | `+0x98 DdagNode` | 새 카드 (지시서가 NULL 이어도) | `[정적]` |
| 카드 | `+0x00 Modules` | 이 명부 줄 하나만 든 원형 목록 | `[정적]` |
| 카드 | `+0x18 LoadCount` | 1 | `[정적]` |
| 명부 줄 | `+0x114 ReferenceCount` | 2 | `[정적]` |
| 카드 | `+0x38 State` | **쓰는 줄 없음** → 0 = `PlaceHolder` 일 것 | `[정적: 쓰는 줄 없음]` / 0 은 `[추측]` |
| 카드 | `+0x28 Dependencies`, `+0x30 IncomingDependencies` | **쓰는 줄 없음** → 비어 있을 것 | `[정적: 쓰는 줄 없음]` / 비어 있음은 `[추측]` |

"0 일 것" 이 왜 `[추측]` 인가 — 단계별로:

1. 두 할당 모두 힙 플래그에 `| 8` 이 붙는다 `[정적]`.
2. 그 8 을 "**0 으로 채워 달라**" 비트로 읽었다. 공개 상수 이름에 기댄 것이라 `[추측]`.
3. 이 함수가 안 쓰는 칸은 그 덕에 0 일 것이다.
4. → `State` 0·선 칸 비어 있음은 **2번 추측에 기댄다.** 실제 값은 동적 Q46·Q23.

- 비유: 새 건물이 오면 명부에 **한 줄**, 연결 카드 **한 장**을 같이 만든다. 카드엔 도장도 줄도 아직 **비어 있다.**

#### 지시서가 있을 때 하는 일 `[정적]`

- **서로 가리키기** (2.3): 명부 줄 `+0xb0 LoadContext` = 지시서, 지시서 `+0x38` = 명부 줄.
- 지시서 `+0x20` 플래그 비트 몇 개를 명부 줄 `Flags` 로 옮긴다 (`Redirected`, `ImageDll`, `ChpeEmulatorImage`).
- `LdrInitState == 1` 일 때만 (`+0xc2`): 지금 직원이 `LdrpDllNotificationLock` 주인 칸(`+0x10`)의 직원과 **다르면** `Flags |= 0x20`(`ProcessStaticImport`, `+0x194`).

#### 활성화 컨텍스트와 적재 이유 — 여기서도 부모 칸 `[정적]`

- **부모 있음** (`+0x30 ≠ 0`) 이고 부모 `+0x88` 이 0 이 아니면 → 부모 것을 물려받는다 (`+0xbb RtlAddRefActivationContext`).
- **부모 없음** (`+0x30 == 0`, exe·`LdrLoadDll` 길) → `+0x16b RtlGetActiveActivationContext` 로 지금 것을 받고, 명부 줄 **`LoadReason`(`+0x10c`) = 4**(`DynamicLoad`).
- `LdrpInitializeProcess` 엔 `+0x10c` 를 쓰는 줄이 없다 (`grep` 0건) → exe 명부 줄의 적재 이유는 **4 로 남는 것으로 읽힌다** `[정적]`. 실제 값은 동적 Q46.
  - 비유: 본 건물도 주문 사유 칸에는 "일반 주문"(4) 이 적힌 채로 남는다.

#### 누가 이 함수를 부르나 — caller 6곳 (ret)

| caller | ret | 무엇의 명부 줄 |
|---|---|---|
| `LdrpInitializeProcess` | `+0x135e` | exe (run19) |
| 〃 | `+0x157c` | AppVerifier 복제 (run19) |
| `LdrpLoadDependentModuleInternal` | `+0x115c` | 의존 DLL (바로 앞 `+0x10db` 에서 힙에 새 지시서를 만든 뒤) |
| `LdrpFindOrPrepareLoadingModule` | `+0x47a` | `LdrLoadDll` 길 DLL (kernel32 등) |
| `LdrpInitializeNtdllDataTableEntry` | `+0x67` | ntdll (인자 줄은 안 봄) |
| `LdrpAllocatePlaceHolder` | `+0xd0` | (인자 줄은 안 봄) |

- 배달 기사 쪽(`+0x115c`)은 새 지시서에 플래그(비트 15 켬)·경로·상태 포인터·**`+0x30` = 부모 명부 줄**·`+0xb8` = -1·이름을 채운 뒤 부른다. 돌려받은 명부 줄은 인자 5 칸(`&부모지시서.58[i]`)에 넣는다 (`+0x1164`) `[정적]`.
- → "**새 DLL 을 만나면 지시서 + 명부 줄 + 연결 카드를 만든다**" 가 코드 모양으로 맞다 `[정적]`.
- 그 갈래가 "**아직 안 올라온 DLL 일 때만**" 인지는 본문 순서를 안 봐서 `[추측]` → Q12b.

### 2.8 곁가지 — 콘센트 덮개, 수리판, 돌림, 변조 검사

test.exe 길에선 대부분 **안 탈 것**으로 보이는 갈래들이다. 짧게.

**IAT 쓰기 가능** `[정적, .c]`

- `LdrpPrepareImportAddressTableForSnap` 이 IAT 디렉터리(12번)를 찾아 지시서 `+0x70/+0x78` 에 적는다.
- 지시서 `+0x20 & 0x800000` 이 꺼져 있으면 `NtProtectVirtualMemory(.., 4, ..)` 로 보호를 바꾼다. 원래 값은 지시서 `+0x90` 에.
- 4 = 읽기·쓰기 페이지 `[추측: 공개 상수]`.

**핫패치 갈래** `[정적]`

- 전역 `LdrpIsHotPatchingEnabled`(`ntdll+0x1d26f0`) 가 0 이면 `+0x10e`·`+0x5d8` 을 안 탄다.
- 켜져 있으면 `+0x10e LdrpQueryCurrentPatch` 결과 바이트가 1 일 때 고리 밖에서 `+0x5d8 LdrpLoadDependentModuleW` 로 "따로 있는 수리판 DLL" 을 올리는 갈래다. 그 뜻은 `[추측]`.

**리다이렉션 갈래** `[정적]`

- 전역 `LdrpRedirectionModule`(`ntdll+0x1d2600`) 이 0 이면 `+0xd2` 가 거짓이다.
- 참이면 지시서 `+0x20 |= 0x2000000`.

**활성화 컨텍스트 찾기** `[정적]`

- `+0x63` 은 지시서 `+0x20 & 0x800000` 이 꺼져 있고 명부 줄 `LoadReason ≠ 9` 일 때 탄다.

**변조 검사** `[정적: 조건 이름은 모름]`

- `LdrpGetImportDescriptorForSnap` 은 exe 명부 줄이고 전역 `DAT_1801ea4f8` 비트 44~45 가 1 일 때만 변조 검사(`LdrpCheckPagesForTampering`)와 깨끗한 보기 다시 맵(`LdrpMapCleanModuleView`)을 한다 (결과서).

test.exe 가 핫패치·리다이렉션 갈래를 안 탈 것은 `[추측]` 이다. 전역 값은 동적 **Q46** (`db ntdll+0x1d26f0 L1`, `dq ntdll+0x1d2600 L1`).

### 2.9 그 밖에 확인된 것

- **버전 대조 일치** `[정적]`: `LdrpLoadDll +0xfb → ret +0x100 LdrpLoadDllInternal`, `LdrpInitializeProcess +0x1bb6 → ret +0x1bbb` 가 run7·run8 콜스택 값과 같다. 지도의 `LdrpInitializeProcess +0x1aa4 → ret +0x1aa9`, `LdrpMapDllWithSectionHandle +0x1c3 → ret +0x1c8` 도 같다.
- **call 자리 / ret 자리 표기**: 결과서는 몇 곳을 **call 자리**로 적었다. run 문서와 flow.md 는 `_calls.tsv` 로 **ret** 을 맞췄다. 이 글도 ret 을 쓴다.

| 결과서 (call) | 이 글 (ret) |
|---|---|
| `LdrpMapDllWithSectionHandle +0x1c3` | `+0x1c8` |
| `LdrpLoadDependentModuleInternal +0x1157` | `+0x115c` |
| `LdrpFindOrPrepareLoadingModule +0x475` | `+0x47a` |
| `LdrpInitializeProcess +0x1359` | `+0x135e` |

- **실패 길** `[정적]`: 자식 맡기기가 음수면 고리를 나가고 상태를 결과 메모지에 적는다. 이때 snap 도 쪽지도 없다.
- **이름 문자열 잡일 8곳**: `strlen`, `memset`, `RtlAnsiStringToUnicodeString`, `RtlUTF8ToUnicodeN`, `NtdllpReallocateStringRoutine`, 그리고 같은 주소에 이름이 여럿인 세 곳(`+0x541` 버퍼 잡기, `+0x567` 메모리 복사, `+0x36e` 버퍼 놓기). 이름 후보는 run 문서.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만.

| 자리 | 전 (run21 뒤) | 후 (run22) |
|---|---|---|
| 기준 | ntdll.pdb (run17 ~ run21), 정적 근거 run9 ~ run21 | + **run22** |
| 두 그림 사이 설명 | run21 문단까지 | + run22 문단: `(1)~(2) 사이` `LdrpMapAndSnapDependency` 아래를 코드 순서로 채움, dll1·dll2 는 같은 함수가 다시 불리는 모양, 자식은 `LdrpQueueWork` 로 끝남 |
| 그림 2 `p6` | "exe 모듈 항목" | "exe 모듈 항목 **+ 그래프 노드**" |
| 그림 2 `j2` | "dll1·dll2 를 올리고 연결 추정" | "[test.exe] 인자 = exe 로드 문맥, 부모 칸 `+0x30` = 0. import 마다 자식을 맡기고, 끝에 한 번 snap. **간선 적는 줄 없음**" |
| 그림 2 새 `(정적)` 상자 | 없음 | exe 층 `ja1`~`ja4` (IAT 준비, 서술자, 배달 기사 [dll1 추정], `LdrpSnapModule`), dll1 층 `jb1`~`jb5` (Allocate, 설치 담당, 같은 함수, 배달 기사 [dll2 추정], `LdrpQueueWork`), dll2 층 `jc0`~`jc3`. 사이 함수를 모르는 화살표는 `(2) ?` |
| 2.4 트리·설명 | `+0x1aa9` 한 줄, `+0x135e` 는 exe 항목만 | `+0x1aa9` → "안쪽 2.6 (run22)", `+0x135e` 가 항목 + 노드 한 쌍·서로 가리킴·`LoadReason` 4 |
| 2.5 | run21 의 "따라 올라올 길이 지도에 있다" | + run22 "고리 기준으로 한 칸 더": kernel32 지시서로 같은 함수 → import 마다 배달 기사 → kernelbase 는 `LdrpQueueWork`. **켜기 앞에도 `LdrpQueueWork` 코드 길** `[정적: 코드 모양]`. 그래프 후보 둘을 읽음 (노드 = Allocate, 간선은 둘 다 없음) |
| 2.6 제목 | `[공백]` import DLL이 메모리에 올라오는 순간 | + "(바깥 고리 코드 `[정적]` run19·run21·run22, 실행은 `[공백]`)" |
| 2.6 본문 | 이름만 본 callee, 지시서 ↔ exe 항목 `[추측]`, "직접인지 일꾼인지" 모름 | + 본문 읽음, **지시서 ↔ 항목 관계 정해짐** `[정적]`, "좁혀졌다" (자식은 늘 직접, 쪽지는 자기 snap 만). 새 소절 넷: `LdrpMapAndSnapDependency`, `LdrpAllocateModuleEntry`, 같은 함수가 다시 불린다, test.exe → dll1 → dll2 이야기 |
| 3장 일꾼 | 첫 일감 후보 = `+0x1aa4` 안 `LdrpQueueWork` | + run22: exe 지시서는 쪽지를 안 붙임, 첫 일감 후보는 dll1·dll2 쪽. kernel32 준비 때도 쪽지 코드 길 있음 → "일꾼 없이" 는 여전히 `[추측]` |
| 4장 공백 표 | 기존 줄 | 3 일꾼·2.5 그래프·2.5 이름 줄·2.6 dll1·dll2 줄에 run22 (일부) 덧붙임. 2.6 새 줄: 간선 자리·갈림·인자 5 (Q12b), 남은 자식 수 (Q12b·Q23), 실행 값 (Q23·Q47), 전역 값·처음 값 (Q46), 이름·뜻 못 정한 것 (run22) |
| 4.1 | `(1)~(2) 사이` 줄 | 그 줄에 run22, 새 줄 셋 (exe 층, 배달 기사 층, 설치 담당 → 같은 함수 층) |
| 5장 | run21 까지 | run22 줄, 요청서 번호 표 run22 줄 |

태그로 보면 핵심 변화는 다섯이다:

- **"`+0x1aa9` 안에서 import 를 어떻게 도나"**: callee 이름만 `[추측: 이름]` → **`[정적]`** (고리 `+0x1d0 ~ +0x3a3`, 줄마다 `+0x34e`, snap 은 밖에서 한 번).
- **"지시서 ↔ exe 명부 줄"**: `[추측]` → **`[정적]`** (서로 가리킴, `+0x7f`·`+0x96`).
- **"간선을 여기서 긋나"**: 모름 → **`[정적]` "이 두 함수엔 없다"**. 어디서 긋는지는 `[추측]` (안쪽, Q12b).
- **"직접 올리나, 일꾼에게 넘기나"**: 모름 → **(일부, `[정적]`) 자식은 늘 직접 맡기고, 쪽지는 부모 있는 건물의 자기 snap 만**. 실제 어느 직원인지는 Q47.
- **"그래프 노드는 어디서"**: 후보 이름 → **`[정적]` `LdrpAllocateModuleEntry` 가 명부 줄과 한 쌍으로**. 카드 `State` 0·선 칸 비어 있음은 `[추측]` (`| 8`).

2.6 제목 머리 `[공백]` 은 **그대로**다. 바깥 고리 **코드**가 `[정적]` 으로 채워졌을 뿐, dll1·dll2 가 실제로 올라오는 순간은 아직 아무도 안 봤다.

그림 2 를 볼 때 주의:

- 새 상자는 **전부 `(정적)`** 이다. 관찰한 상자는 없다.
- `[dll1 추정]`·`[dll2 추정]` 은 "그때 맡은 모듈" 을 소스 의존 관계로 짐작한 것이다. 코드는 같은 함수가 모듈마다 다시 불리는 모양까지만 말한다.
- `(2) ?` 화살표는 사이에 안 읽은 함수가 있다는 뜻이다 (지도 이름 사슬로만 닿음).

### 큰 그림에서 어떻게 이어지나

- run20 = kernel32 주문서, run21 = 배달 창구, **run22 = 갖다 놓은 뒤 "기대는 건물 목록" 을 도는 담당.**
- 이 담당은 kernel32 때도, exe 때도, dll1·dll2 때도 **같은 사람, 같은 절차**다. 다른 건 지시서의 **부모 칸** 하나다.
- 2.6 의 사슬이 **코드로는** 이렇게 이어진다:
  - `LdrpInitializeProcess +0x1aa9` → `LdrpMapAndSnapDependency`(exe) → `+0x34e` 배달 기사(dll1) → … → 설치 담당 → `+0x1c8` `LdrpMapAndSnapDependency`(dll1) → `+0x34e`(dll2) → …
  - 끝맺음: dll2·dll1 은 쪽지 (`+0x4f7`), exe 는 직접 꽂기 (`+0x4c2`).
  - 그다음 `+0x1ab1` 게시판 비우기 → `+0x1af2` 켜기 준비 (켜기는 건너뜀, run21) → `+0x1bbb` 전원 켜기 (run19).
- **다음 구간** (`plan.md` "다음 항목"): **Q12b — 배달 기사 `LdrpLoadDependentModuleInternal`** (정적, 9250바이트·75곳).
  - 넘긴 질문 (결과서): 선 긋는 줄이 있나, `+0x6c` 줄이기와 `State = 3`·`0x103` 의 뜻, 이미 있음 / KnownDll / 새로 올리기 갈림과 인자 5, `+0x182c LdrpQueueWork`·`+0x1cae LdrpProcessWork` 조건, 인자 4·6.
  - 결과를 보고 그래프 정리(`LdrpCondenseGraph` → …)·주소 연결(`LdrpSnapModule`)이 더 필요하면 그 자리에서 Q12c 로 (plan.md).
- **Q14** ("dll1·dll2 를 올리는 쪽") 는 **일부**만 답이 났다. 바깥 고리 순서와 "dll1 에 대해 같은 함수가 다시 불린다" 는 코드 모양까지. 합칠지는 Q12b 뒤에 다시 본다.
- 버스를 탈 때(동적) 볼 것은 새 번호 없이 **Q23** (이 함수가 몇 번, 어떤 지시서로), **Q46** (전역 값·새 명부 줄 처음 값), **Q47** (끝맺음이 어느 갈래, 어느 직원) 에 붙었다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 간선(선)을 **어디서** 긋나. "이미 올라옴 / KnownDll / 새로 올리기" 갈림. 인자 5 칸에 무엇이 들어가나 | 배달 기사 `LdrpLoadDependentModuleInternal` 본문을 안 읽었다 (몇 줄만) | **Q12b** (정적, 다음 항목). 이어 볼 줄: `+0x12df ~ +0x12f7`, `+0x1606 ~ +0x1634`, `+0x182c LdrpQueueWork`·`+0x1cae LdrpProcessWork` 조건, `+0x1179` |
| 남은 자식 수(`+0x6c`)가 **언제** 0 이 되나, 0 이 아니라 그냥 끝났을 때 그 몫을 누가 이어받나 | 자식 쪽 줄을 줄만 봤다. 실행 때 값이기도 하다 | 정적 **Q12b**, 동적 **Q23**: `ntdll+0x21506`(`+0x466`, 잠금 잡은 직후) 에서 `dd @rbx+0x6c L1` |
| dll1·dll2 에 대해 이 함수가 **정말** 다시 불리나, 그때 지시서 `+0x30` 값 | 코드 모양까지만. 실행은 안 봤다 | 동적 **Q23**: 입구 `ntdll+0x210a0` 에서 `du poi(@rcx+0x38)+0x58`, `dq @rcx+0x30 L1`, `k`. 고리 안 `ntdll+0x213e9`(call `+0x349`) 에서 `du poi(@rcx+8)` |
| 끝맺음이 모듈마다 **어느 갈래**인가, 쪽지(일감)를 **어느 직원**이 처리하나 (특히 풀이 없는 kernel32 준비 때) | `LdrpQueueWork` 본문을 안 읽었다. 실행 때 값 | 동적 **Q47** (kernel32 준비 때는 **Q20**): `ntdll+0x2155d`(snap call) / `ntdll+0x21592`(쪽지 call) 에서 `~`, `dd ntdll+0x1d23c8 L1` |
| 핫패치·리다이렉션 전역 값, exe 명부 줄 `LoadReason` 이 정말 4 인가, 새 카드 `State` 0·`LoadCount` 1 인가 | 전역·힙 값은 실행 때 정해진다. "0 으로 채움" 은 `[추측]` | 동적 **Q46**: `db ntdll+0x1d26f0 L1`, `dq ntdll+0x1d2600 L1`. `LdrpAllocateModuleEntry` `RET` `ntdll+0x6fbf8` 에서 `r rax`, `dd @rax+0x10c L1`, `dd poi(@rax+0x98)+0x38 L1`, `dd poi(@rax+0x98)+0x18 L1` |
| kernelbase 가 정말 kernel32 의 import 인가 | kernel32 import 표를 안 봤다 | run21 해설과 같음. **Q20** 콜스택으로 간접 확인 |
| dll2 의 import 가 무엇인가 | dll2 import 표를 안 봤다 | 대기열 줄은 따로 없다 (이야기 그림의 `?`) |
| 왜 부모 유무로 직접 꽂기 / 쪽지를 가르나 | 코드에 이유가 쓰여 있지 않다 | 모른다. Q12b·Q47 결과를 보고 다시 |
| 이름·뜻 못 정한 것: 지시서 칸 이름 전부, `+0x50`(`[추측]` 자식 명부 줄 칸)·`+0xb8`(-1 로 시작)의 뜻, `+0x20` 비트들의 뜻, `+0x6c` 값 이름, `DAT_1801ea4f8` 비트 44~45, `LdrpQueryCurrentPatch` 결과 바이트의 뜻, 힙 플래그 `\| 8` | `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없다. 나머지는 callee 본문이거나 공개 상수 이름에 기댐 | flow.md 4장 "이름·뜻 못 정한 것 (run22)" 줄 (대기열 줄은 따로 없다) |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run22-map-snap-dep.md`](run22-map-snap-dep.md) — 특히 "호출 지점", "로드 문맥 칸", "`LdrpMapAndSnapDependency` 순서", "그래프 간선을 적는 자리", "같이 올리기 vs 일감으로 넘기기", "`LdrpAllocateModuleEntry`", "test.exe → dll1 → dll2 이야기", "kernelbase 가 … 따라 올라오는 길", "호출 관계"
- 정적 요청서: [`_workspace/sast/17_map-snap-dep.md`](../../_workspace/sast/17_map-snap-dep.md)
- 정적 결과서 (질문 1 ~ 6, PDB 칸 이름, 로드 문맥 칸 표, 그림 2 상자 후보, 동적 멈춤점, Q12b 에 넘길 질문, Q14 판단): [`_workspace/sast/17_map-snap-dep_result.md`](../../_workspace/sast/17_map-snap-dep_result.md)
- 앞 바퀴 해설:
  - [`run21-load-dll-internal-해설.md`](run21-load-dll-internal-해설.md) — 2.4 지도 이름 사슬·설치 담당, 2.5 의존 DLL 길과 재귀 모양, 2.6 진행 도장, 2.7 켜기를 미루는 비트, 2.10 적재 이유
  - [`run20-kernel32-load-해설.md`](run20-kernel32-load-해설.md) — 2.4 순서 문제, 2.11 결과 메모지
  - [`run19-proc-init-modules-해설.md`](run19-proc-init-modules-해설.md) — 2.6 작업 지시서·연결 카드, 2.7 `+0x1aa4` 와 snap(전기 연결), 2.9 일꾼·게시판, 2.12 팻말
  - [`run18-proc-init-setup-해설.md`](run18-proc-init-setup-해설.md) — 2.6 건물 명부
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — 부르는 자리와 돌아올 자리, 3.7 오프셋 순서 ≠ 실행 순서
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "두 그림 사이 설명" run22 문단
  - "그림 2" `(1)~(2) 사이` 의 `j2`·`ja1`~`ja4`·`jb1`~`jb5`·`jc0`~`jc3`, `(0)` 의 `p6`
  - "2.4" 트리 `+0x1aa9` 줄, `+0x135e` 설명
  - "2.5" run22 "고리 기준으로 한 칸 더", 그래프 후보 줄
  - "2.6" 새 소절 넷 (`LdrpMapAndSnapDependency`, `LdrpAllocateModuleEntry`, 같은 함수가 다시 불린다, 이야기)
  - "3" 일꾼 줄의 run22 두 줄
  - "4. 공백 목록" 2.6 새 줄들, "4.1" `(1)~(2) 사이` 줄들
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q12a(끝), Q12b(다음), 결정 기록 2026-10-23 (Q12 나눈 이유), Q14·Q23·Q46·Q47 출처

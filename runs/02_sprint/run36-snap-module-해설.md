# run36 snap-module — 해설

> 이 글은 [run36 문서](run36-snap-module.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q50** — 2.5·2.6 주소 연결 `ntdll!LdrpSnapModule` (4052바이트, 36곳): IAT 에 import 함수 주소를 적는 곳, `LdrpLoadDependentModuleInternal` 을 부르는 `+0x5df` 갈래(지연 import·forwarder?), 부모 카운터와의 관계 (`_workspace/plan.md`, 이번에 **끝**)
  - 새 항목 둘: **Q68** (동적, snap 실행 값), **Q69** (작은 정적, `State = -3` 을 처음 쓰는 곳). **Q47** 에는 출처를 더했다.
  - 다음 항목: **Q51** (kernel32 import 서술자 순서, 작은 정적).
- 근거:
  - run 문서 [`run36-snap-module.md`](run36-snap-module.md) (가장 중요)
  - 요청서 [`_workspace/sast/31_snap-module.md`](../../_workspace/sast/31_snap-module.md)
  - 결과서 [`_workspace/sast/31_snap-module_result.md`](../../_workspace/sast/31_snap-module_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q50, Q47, Q60, Q68, Q69
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`. 다시 analyze 하지 않은, flow.md 기준과 같은 분석본이다.
  - 본문을 읽은 건 두 함수다.
    - `LdrpSnapModule` (rva `0x3a8c0`, 4052바이트, `.asm` 908줄, 직접 호출 36줄 + 예외 필터 1) — 이미 뽑혀 있던 파일
    - `LdrpDoPostSnapWork` (rva `0xa4200`, 228바이트, 호출 5줄) — 이번에 새로 뽑음
  - 칸 이름·열거 값(`_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_LDR_DLL_LOAD_REASON`)은 **PDB 타입**으로 읽었다.
  - PE 규격 구조체(import 서술자, export 디렉터리, thunk)는 ntdll PDB 에 없어서 **PE 규격 이름**으로 쓴다.
  - **새 태그 `[정적+샘플]`** 이 이번에 처음 나온다.
    - 뜻: sample 로 빌드한 exe·dll 의 import 표와, MS 바이너리 kernel32 의 export 표를 **스크립트로 직접 세어서 나온 값**이다.
    - `[정적]` 과 다른 점: 코드 줄을 읽은 게 아니라 **파일에 적힌 값을 센 것**이다. 실행 때 정말 그 값대로 갔는지는 모른다 (flow.md 표시 규칙).
    - 비유: 요리책(코드)을 읽은 게 아니라, 장바구니(PE 파일)에 뭐가 몇 개 들었는지 **세어 본** 것.

이 글은 [run22 해설](run22-map-snap-dep-해설.md) ~ [run24 해설](run24-work-queue-해설.md) 에서 **이름만 보고 지나간 상자 하나**를 연 편이다.

- run22 에서 건물 담당(`LdrpMapAndSnapDependency`)이 끝맺음에서 `+0x4c2 LdrpSnapModule` 을 부르는 걸 봤다.
- run24 에서 게시판 쪽지 처리(`LdrpProcessWork`)가 `+0x131 LdrpSnapModule` 을 부르는 걸 봤다.
- 두 번 다 "**본문은 Q50**" 으로 미뤘다. 이번이 그 Q50 이다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출 → [run9 해설](run9-create-user-process-해설.md) 2절
- 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
- 빠른 실패 `INT 0x29` → [run24 해설](run24-work-queue-해설.md) 2.3
- 로드 문맥(**작업 지시서**), DDAG 노드(**연결 카드**) → [run19 해설](run19-proc-init-modules-해설.md) 2.6
- snap, IAT → [run19 해설](run19-proc-init-modules-해설.md) 2.7
- 내보내기 표(export, **호실 이름표**) → [run16 해설](run16-thunk-context-해설.md) 2.9, [run20 해설](run20-kernel32-load-해설.md) 2.5
- 전달 항목 forwarder → [run20 해설](run20-kernel32-load-해설.md) 2.5
- 노드 상태 번호(**진행 도장**) → [run21 해설](run21-load-dll-internal-해설.md) 2.6, [run22 해설](run22-map-snap-dep-해설.md) 2.6
- 적재 이유 `LoadReason`(**주문 사유**) → [run21 해설](run21-load-dll-internal-해설.md) 2.10
- import 서술자, IAT 를 쓰기 가능으로(**덮개 열기**) → [run22 해설](run22-map-snap-dep-해설.md) 2.1·2.8
- 지시서 칸 `+0x70/+0x78` (IAT 시작·크기), `+0x30`(부모), `+0x6c`(**체크판**) → [run22 해설](run22-map-snap-dep-해설.md) 2.3·2.6
- 배달 기사 `LdrpLoadDependentModuleInternal`, 간선(카드 사이 줄), `0x103` 기다림 갈래 → [run23 해설](run23-load-dependent-해설.md) 2.3·2.7
- 끼워 넣기(인라인) → [run23 해설](run23-load-dependent-해설.md) 2.1
- 게시판·일감, `LdrpSignalModuleMapped`(체크판 지우기) → [run24 해설](run24-work-queue-해설.md) 2.3·2.7
- TLS 서류철(`LdrpTlsList`), `TlsIndex = 0xffff`, `LdrpHandleTlsData` → [run28 해설](run28-tls-init-call-해설.md) 2.3
- CFG 와 교환원 → [run28 해설](run28-tls-init-call-해설.md) 2.9

비유도 그대로 이어 쓴다.

- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- DLL = **건물**, 모듈 항목 = **명부 한 줄**, DDAG 노드 = 명부 줄마다 붙은 **연결 카드**, `State` = 카드의 **진행 도장**
- 로드 문맥 = **작업 지시서**, 부모 지시서 `+0x6c` = **체크판**
- `LdrpMapAndSnapDependency` = **건물 담당**, `LdrpLoadDependentModuleInternal` = **배달 기사**, `LdrpQueueWork` = **게시판에 쪽지 붙이기**
- **IAT = 건물마다 가진 "빈 주소록"** (사용자에게 설명한 그림 그대로)
  - 칸마다 "dll1 건물의 `Dll1Hello` 담당" 같은 **이름만** 적혀 있고, 번호(주소) 칸은 비어 있다.
  - snap = 그 **빈 칸을 채우는 일**이다.
  - 예전 해설(run19·run22)은 같은 것을 "벽 콘센트에 전선 꽂기" 라고 불렀다. 가리키는 건 같다.
  - 관리실 **"벽 주소록"**(run16·run20 의 전역 칸, `PsNtdllExports`·`Kernel32ThreadInitThunkFunction` 같은 것)과는 **다른 물건**이다. 이번 것은 건물 하나하나가 자기 안에 가진 주소록이다.
- export 표 = 상대 건물 입구의 **호실 이름표** (run16·run20)
- **forwarder = 호실 이름표에 호실 번호 대신 붙어 있는 "이 담당은 다른 DLL(건물)에 있다는 안내문"** (사용자에게 설명한 그림 그대로. run20 의 "옆 건물 몇 호로 가세요" 와 같은 것)
- run 문서는 이번에 "**단축번호 칸·전화번호부**" 비유를 썼다. 이 글은 위 그림으로 옮긴다 (가리키는 건 같다).
  - 단축번호 칸 = 빈 주소록 칸 (IAT)
  - 전화번호부 = 호실 이름표 (export 표)
  - "옆 가게 OO 에게 물어보세요" = 다른 DLL 에 있다는 안내문 (forwarder)
  - "아마 몇 쪽" = 이름 쪽지에 같이 적힌 hint (2.4)
  - 판을 다시 잠그기 = 주소록 덮개 다시 닫기 (2.9)

한 줄로 먼저 말하면:

- `LdrpSnapModule` 은 건물 하나의 **빈 주소록을 처음부터 끝까지 채우는** 함수다 `[정적]`.
  - 기대는 건물(서술자)마다 → 그 건물 호실 이름표를 펴고 → 이름 칸마다 번호를 찾아 → **주소록 칸에 적는다.**
  - 다 채우면 **덮개를 다시 닫고**(끝맺음), 카드에 **도장 5**(`Snapped`)를 찍는다.

이번 바퀴의 **핵심 다섯**:

1. **주소록 칸에 적는 손은 딱 하나, `+0x3e7`** 이다. 이름으로 찾든, 번호로 찾든, 안내문을 따라갔든, 전부 이 한 줄로 모인다 `[정적]` → 2.3
2. **이름 찾기는 이 함수 안에서 직접** 한다. 쪽지의 hint 자리를 먼저 보고, 틀리면 이름표를 반씩 접어 찾는다 (이분 탐색). 따로 부르는 함수가 없다 `[정적]` → 2.4
3. **`+0x4e5 ~ +0x5da` 갈래는 forwarder(안내문)** 다. 지연 import 가 아니다. 안내문이 "관리실(`NTDLL`)" 을 가리키면 아무도 안 부르고, 다른 건물이면 **그 자리에서 배달 기사를 부른다** `[정적]` → 2.6·2.7
4. **도장 5 는 `+0xb87` 한 곳**, 끝맺음 `LdrpDoPostSnapWork` 가 성공한 뒤다. 실패하면 이 함수는 도장을 안 건드린다 `[정적]` → 2.8
5. 끝맺음 `LdrpDoPostSnapWork` 안에서 **DLL 의 TLS 서류철 장이 만들어진다** (`LdrpHandleTlsData`). run28 에서 "누가 부르나" 가 비어 있던 그 함수다 `[정적]` → 2.9

그리고 test.exe 에 대해서는 파일을 세어 봤다 `[정적+샘플]`: 주소록 칸 수는 **test.exe 91 · dll1 89 · dll2 88**, 안내문 10개씩은 **모두 관리실 행**이다. 덤으로 **dll2 의 import 가 `KERNEL32.dll` 하나**라는 것도 나왔다 → 2.12

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessW … NtCreateUserProcess … NtResumeThread        (run9 ~ run14, run34)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → … ═▶ IRETQ                          (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ LdrpInitializeProcess   (가게 전체 세팅)                                     (run17 ~ run19, run35)
        ├─ +0x18d0  LdrpInitializeTls  (exe 의 TLS 서류철 장)                      (run28)
        ├─ +0x19a2  kernel32 준비 → … → LdrpMapAndSnapDependency(kernel32)          (run20 ~ run22)
        │                                   └─ +0x4c2 LdrpSnapModule              ← 이번 (같은 함수)
        ├─ +0x1aa9  LdrpMapAndSnapDependency(exe)                                  (run22)
        │     ├─ +0x34e  배달 기사 [dll1 추정]                                     (run23)
        │     └─ +0x4c2  LdrpSnapModule(exe 지시서)   (부모 없음·체크판 0 일 때)    ← 이번
        ├─ +0x1ab1  LdrpDrainWorkQueue(1)  (게시판 비우기)                          (run24)
        │     └─ +0x187 LdrpProcessWork
        │           └─ +0x131 LdrpSnapModule(exe·dll1·dll2 지시서)  (도장 ≠ 0)    ← 이번
        │                  └─ +0xb66 LdrpDoPostSnapWork                          ← 이번
        │                         └─ +0x93 LdrpHandleTlsData (dll1·dll2)         ← 이번 (caller 가 나옴)
        ├─ +0x1af2  LdrpPrepareModuleForExecution(exe) → 그래프 정리                (run21·run24)
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)               (run27·run28)
   └─ NtContinue ═▶ RtlUserThreadStart → … → main                                 (run16, run29·run30)
```

- 위치는 **짐 들이기**(건물 가져오기)와 **전원 켜기**(DllMain·TLS 콜백) **사이**다.
  - 앞: 건물을 가져와 설치하고 연결 카드 사이에 줄을 긋는다 (run21 ~ run26).
  - 이번: 설치된 건물의 **빈 주소록을 채운다** (snap).
  - 뒤: 카드를 정리하고 (도장 5 → 6), 전원을 켠다 (run24·run27).
- flow.md 로는 **2.6** 의 새 절 `LdrpSnapModule`·`LdrpDoPostSnapWork` 가 중심이다. 2.5·2.7·4장에도 줄이 붙었다.

### 무엇이 궁금했나

run 문서 "목적" 의 여섯 질문이다.

1. 주소록(IAT) 칸에 함수 주소를 적는 줄은 어디인가. 이름 → 주소 찾기는 무엇으로 하나. 서술자는 어디까지 도나.
2. `+0x4e5 strrchr ~ +0x5da LdrpLoadDependentModuleInternal` 갈래가 forwarder(안내문) 인가.
3. 카드에 도장 5 (`Snapped`) 를 찍는 곳.
4. 끝맺음 `LdrpDoPostSnapWork` 가 하는 일.
5. snap 이 부모 카드·부모 체크판을 건드리나.
6. 작은 갈래와 실패 코드.

### 바퀴 전에 알던 것

- `[정적]` (run22) 건물 담당이 고리 앞 `+0x9e LdrpPrepareImportAddressTableForSnap` 에서 IAT 를 찾아 지시서 `+0x70`(시작)·`+0x78`(크기) 에 적고 **쓰기 가능**으로 바꾼다 (덮개 열기). 원래 보호값은 `+0x90`.
- `[정적]` (run22) 끝맺음에서 체크판이 0 이면 도장 4 (`Snapping`) → 부모 없음이면 `+0x4c2 LdrpSnapModule`, 있으면 `+0x4f7` 게시판. IAT 가 없는 건물은 `+0x5ae` 에서 **도장 5 만** 찍고 snap 을 안 부른다.
- `[정적]` (run24) `LdrpProcessWork` 는 도장이 0 이 아니면 `+0x131 LdrpSnapModule`. 결과가 음수면 `*(ctx+0x28)` 에 옮긴다.
- `[정적]` (run23) 배달 기사는 부모 체크판이 **이미 0** 인데 안 온 자식을 만나면 `0x103` 을 돌려준다. 이 길은 다른 caller 몫으로 **보였다** `[추측]` — 후보가 `LdrpSnapModule +0x5df`.
- `[정적]` (run28) DLL 의 TLS 서류철 장은 `LdrpHandleTlsData` 가 만들고 끝에 `TlsIndex = 0xffff`. 그런데 **누가 그 함수를 부르나**는 없었다.
- `[추측]` (flow.md 2.5) `LdrpSnapModule` = "IAT 에 import 함수 주소를 적는 곳". `+0x5df` 갈래 = "지연 import 또는 forwarder".
- `[관찰]` (run7) test.exe 는 import 가 이어져서 `main` 까지 간다. 그러니 **어딘가에서** 주소록이 채워졌다. 그 줄은 못 봤다.
- 지도에서 본 것 (요청서): `LdrpGetProcedureAddress`·`LdrpNameToOrdinal` 같은 "이름으로 찾기" 함수 호출이 **지도에 없었다.** 이게 질문 1a 의 출발점이다.

---

## 2. 찾은 것

### 2.1 한눈에 — snap 한 번의 순서

run 문서 "하는 일 순서" 를 대표만 남겼다.

```
LdrpSnapModule(ctx)                              ← MapAndSnap +0x4c2 / ProcessWork +0x131
 ├─ +0x86   LdrpHandlePendingModuleReplaced      (입구에서 무조건)
 ├─ 서술자 고리 (ctx+0x80 < ctx+0x68)             기대는 건물 하나씩, 전부
 │   ├─ 자식 항목 = ctx+0x58[번호]
 │   ├─ +0x18e  자식 export 디렉터리 찾기          (호실 이름표 펴기)
 │   └─ thunk 고리 (OFT 칸이 0 일 때까지)          주소록 칸 하나씩
 │        ├─ 번호(서수) 로 찾기 / 이름 → hint 먼저 → 반씩 접기
 │        ├─ 주소 = 자식 DllBase + 함수 RVA      (+0x3ac)
 │        ├─ 그 주소가 이름표 안이면 = 안내문(forwarder)
 │        │     ├─ "NTDLL"  → 관리실 줄 그대로 (+0x649, 아무도 안 부름)
 │        │     └─ 그 밖    → +0x5df 배달 기사 (이유 1)
 │        └─ ★ +0x3e7  주소록 칸 = 주소           (호출 아님, 값 쓰기)
 ├─ ★ +0xb66 LdrpDoPostSnapWork(ctx)             덮개 닫기 → CFG 칸 검사 → TLS → CFG
 ├─ +0xb80  끝 로그
 ├─ ★ +0xb87 카드 도장 = 5 (Snapped)             (호출 아님, 값 쓰기)
 └─ RET (eax = 상태)
```

- `★` 셋이 **대표 지점**이다 (run 문서 "대표 지점" 표).
  - `+0x3e7` = `ntdll+0x3aca7`
  - `+0xb61`(call) / `+0xb66`(ret) = `ntdll+0x3b421` / `ntdll+0x3b426`
  - `+0xb87` = `ntdll+0x3b447`
- `+0x3e7` 과 `+0xb87` 은 **호출이 아니라 값 쓰기**다. 그래서 flow.md 그림에선 화살표가 아니라 **상자 글**로 들어갔다.
- 비유로 한 줄: 관리실 직원이 건물 하나의 **빈 주소록**을 들고 → 기대는 건물마다 **호실 이름표**를 펴서 → 이름 칸마다 호실 번호를 찾아 **적고** → 다 적으면 **덮개를 닫고** → 카드에 "**주소 다 이음**" 도장을 찍는다.

### 2.2 한 줄씩 읽는 법

**줄: `└─ ★ +0x3e7  주소록 칸 = 주소`**

- 누가: `LdrpSnapModule` 이 **호출 없이 직접** 쓴다. 명령은 `MOV qword ptr [RCX],RSI` (`18003aca7`) `[정적]`.
- 어디에: `RCX` 가 가리키는 칸 = 지금 채우는 **주소록 칸**. `RCX` 는 `+0x3df` 에서 스택 칸 `[RSP+0xa0]` 의 "쓰기 커서" 를 꺼낸 값이다.
- 무엇을: `RSI` = 찾아낸 함수 주소. 보통은 `+0x3ac` 에서 계산한 "자식 `DllBase` + 함수 RVA" 다.
- 무엇을 위해: 이 건물이 나중에 `CALL [그 칸]` 으로 상대 함수를 부를 수 있게 하려고. (run30 해설에서 test.exe 가 `LoadLibraryExW` 를 **칸 rva `0x6e250` 을 거쳐서** 부르는 걸 봤다. 그 칸이 채워지는 곳이 바로 여기다.)
- 바로 뒤: `+0x3f6` 커서 +8 (다음 칸으로), `+0x402` 지시서 `+0x84` 를 하나 올림 ("여기까지 했음" 표시, 2.5).

**줄: `│     └─ 그 밖 → +0x5df 배달 기사 (이유 1)`**

- 누가: `LdrpSnapModule`.
- 어디서: 부르는 자리 `+0x5da`, 돌아올 자리 **`+0x5df`**.
- 누구를: `LdrpLoadDependentModuleInternal` — run22·run23 의 그 **배달 기사**다.
- 무엇을 위해: 안내문이 가리킨 **다른 건물을 그 자리에서 가져오라고** 맡긴다. 주문 사유는 **1 = `LoadReasonStaticForwarderDependency`** (PDB) `[정적]`. 자세한 건 2.6·2.7.

**줄: `├─ ★ +0xb66 LdrpDoPostSnapWork(ctx)`**

- 누가: `LdrpSnapModule`.
- 어디서: 부르는 자리 `+0xb61`, 돌아올 자리 **`+0xb66`**.
- 누구를: `LdrpDoPostSnapWork` (rva `0xa4200`, 228바이트). caller 는 이 한 곳뿐이다.
- 무엇을 위해: 서술자를 **다 돈 뒤** 마무리. 덮개 닫기, CFG 칸 검사, TLS 서류철 장, CFG 풀기 (2.9).
- 조건: 그때까지 상태가 음수가 아닐 때만 (`+0xb59`) `[정적]`.

### 2.3 질문 1 — 주소록 칸을 채우는 줄은 딱 하나

**새 말 셋** (이 절을 읽는 데 필요하다)

- **thunk 칸**: import 표에서 함수 **하나** 몫의 8바이트 칸이다. 칸마다 "이름" 또는 "번호" 가 들어 있고, 값이 0 인 칸에서 그 건물 몫이 끝난다 `[정적]` (`+0x2b3`).
  - 비유: 주소록의 **한 줄**.
- **OFT / FirstThunk**: import 서술자(20바이트) 안에 칸 줄 위치가 둘 있다. `+0` 이 OFT, `+0x10` 이 FirstThunk 다 (결과서).
  - 코드는 **OFT 줄에서 이름을 읽고**, **FirstThunk 줄에 주소를 쓴다** (`RCX` 커서가 FirstThunk + importer `DllBase` 에서 시작, `+0x129` 근처) `[정적]`.
  - OFT 가 0 이거나 이미지 밖이면 FirstThunk 줄을 대신 읽는다 (`+0x138`) `[정적]`.
  - OFT 는 PE 규격 이름 `OriginalFirstThunk` 의 줄임으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 주소록이 **두 장 겹쳐** 있다. 위 장(OFT)에는 이름만 적혀 있어서 그걸 **읽고**, 아래 장(FirstThunk)의 같은 줄에 번호를 **적는다.**
- **서수(ordinal)**: 이름 대신 **번호로** 함수를 가리키는 방법. thunk 칸의 **맨 위 비트**가 켜져 있으면 서수다 (`+0x2ca SHR RDI,0x3f`, `+0x2e5`) `[정적]`.
  - 비유: 이름표를 안 보고 "**3번 호실**" 로 바로 찾아가기.

**근거** `[정적]`

1. `.asm` 에서 스택이 아닌 메모리에 쓰는 줄은 **7줄**뿐이다 (+ `INC` 2줄). 결과서가 하나씩 다 봤다.
   - `+0x105`, `+0x3e7`, `+0x462`, `+0xa72`, `+0xb87`, `+0xe03`, `+0xefa` (결과서 "ctx 칸" 표).
2. 그중 주소록(IAT)으로 갈 수 있는 건 **`+0x3e7` 하나**다. 나머지는 지시서 칸·카드 도장·실패 상태 쓰기다.
3. 바로 앞 `+0x3cc JS` — 찾기가 실패했으면 이 줄을 **건너뛰고** 오류 처리로 간다.

**왜 "하나" 로 모일 수 있나 — 쓸 값(`RSI`)만 갈래마다 다르다** `[정적]`

| 갈래 | `RSI` 를 정하는 곳 |
|---|---|
| 보통 (이름·서수) | `+0x3ac` 자식 `DllBase` + 함수 RVA |
| 안내문(forwarder)을 따라간 뒤 | `+0x885` 최종 주소 |
| 리다이렉트 (거의 안 탐, 2.11) | `+0xd62` `LdrpCheckRedirection` 이 돌려준 주소 |

- 비유: 번호를 **어떻게 알아냈든**(이름표에서 / 안내문 따라 다른 건물에서), 주소록에 **적는 펜은 하나**다.
- 그래서 동적으로 볼 때도 `ntdll+0x3aca7` 하나에만 서면 **모든 주소록 쓰기**가 보인다 (단, 아주 많이 선다 → 4절).

**서수로 찾기 — 짧게** `[정적]`

1. 색인 = 서수 − export `Base`.
2. `+0x396` 함수 수와 비교 (넘으면 못 찾음).
3. `+0x39c LFENCE` 뒤 `+0x3a1` 에서 함수 주소 표(`AddressOfFunctions`) 의 그 칸을 읽는다.

### 2.4 이름으로 찾기 — hint 먼저, 그다음 반씩 접기

**질문 1a 의 답: 이 함수 안에서 직접 한다 (예)** `[정적]`

- 지도에 "이름으로 찾기" 함수 호출이 없었던 이유가 이것이다.
- `_calls.tsv`·`.asm` 에 **간접 호출도 하나도 없다.**
- `LdrpGetProcedureAddress`·`LdrpNameToOrdinal` 은 [끼워 넣기(인라인)](run23-load-dependent-해설.md) 로 들어와 있다. 증거는 **로그 문자열**이다.
  - `+0xb1c` 로그 인자에 `"LdrpNameToOrdinal"`
  - `+0x756`·`+0xb42` 로그 인자에 `"LdrpGetProcedureAddress"`
  - 소스 파일 이름 `minkernel\ldr\ldrsnap.c`
- 비유: 다른 직원을 부르는 대신, 그 직원의 **업무 매뉴얼을 통째로 복사해서** 자기 매뉴얼에 붙여 둔 것. 그래서 "부르기" 기록은 없고 그 직원 **이름이 적힌 메모**만 남아 있다.

**새 말 — hint**: import 쪽 이름 쪽지에 **이름과 같이 적힌 2바이트 번호**다. "상대 건물 이름표에서 **아마 몇째 줄**에 있을 것" 이라는 메모다 `[정적]` (결과서: 쪽지 = hint 2바이트 + 이름).

**새 말 — 이분 탐색**: 정렬된 목록에서 **가운데**를 보고, 찾는 게 앞쪽이면 앞 반만, 뒤쪽이면 뒤 반만 남기는 걸 되풀이하는 찾기 방법.

- 비유: 가나다순 이름표를 **반으로 접고, 또 반으로 접어** 가며 찾기.
- 코드는 export 이름 표가 **정렬돼 있다고 믿고** 이렇게 찾는다 (결과서: PE 규격상 export 이름은 정렬).

**한 걸음씩** `[정적]`

1. `+0x316` 쪽지의 hint 를 읽는다.
2. `+0x321` hint 가 이름 수 **이상**이면 (말이 안 되는 hint) → 가운데(`+0x44c`)부터 시작.
3. hint 가 범위 안이면 → **이름표 hint 번째 줄**과 한 바이트씩 비교 (인라인 문자열 비교).
4. 같으면 → 끝. 그 줄의 서수로 함수 주소를 읽는다 (`+0x371`, `+0x3a1`).
5. 다르면 → 비교 결과에 따라 범위를 반으로 줄인다.
   - 찾는 이름이 뒤쪽 → `+0x45c` (low = mid + 1)
   - 찾는 이름이 앞쪽 → `+0x437` (high = mid − 1)
6. `+0x332` low > high 가 되면 → **못 찾음** (2.11).

- 핵심: **hint 자리가 이분 탐색의 첫 한 걸음**이다 `[정적]`.
  - hint 가 맞으면 한 번에 끝.
  - hint 가 틀려도 손해는 "비교 한 번" 뿐이다. 그다음부터는 보통 이분 탐색이 된다.
- 비유: 쪽지에 "아마 120번째 줄" 이라고 적혀 있으면 **120번째 줄부터 펴 본다.** 맞으면 끝, 아니면 거기를 접는 선으로 삼아 반씩 접기 시작한다.
- test.exe 에서는 이 hint 가 **KERNEL32 쪽은 전부 틀린다** `[정적+샘플]`. 그래도 문제없는 이유가 위 5번이다 → 2.12.

### 2.5 서술자는 전부, 책갈피를 남기며

**질문 1b 의 답: 건물의 서술자를 전부 돈다** `[정적]`

- 바깥 고리: 지시서 `ctx+0x80`(지금 서술자 번호) < `ctx+0x68`(서술자 개수) 인 동안.
  - 서술자 하나 = 기대는 건물 하나 ([run22 해설](run22-map-snap-dep-해설.md) 2.1 "기대는 건물 목록의 한 줄").
  - 자식 항목 = `ctx+0x58[번호]`. 0 이면 그 서술자는 건너뛴다 (`+0x149`).
- 안쪽 고리: OFT 줄의 thunk 칸을 하나씩, **0 인 칸**에서 끝 (`+0x2b3`).
- 건물 담당(run22)은 서술자마다 배달 기사를 불렀다. snap 은 그게 **다 끝난 뒤** 한 번 불려서, 그 건물의 서술자를 **처음부터 끝까지 한꺼번에** 채운다.

**책갈피 — `ctx+0x80` · `ctx+0x84`** `[정적]`

- `ctx+0x80` = 지금 몇 번째 서술자인가.
- `ctx+0x84` = 그 서술자 안에서 **이미 끝낸 칸 수**.
  - 한 칸 쓸 때마다 `+0x402` 에서 하나 올린다.
  - 서술자가 끝나면 `+0x462` 에서 0 으로, `ctx+0x80` 을 하나 올린다 (`+0x471`).
  - 다시 시작할 땐 커서를 `ctx+0x84 × 8` 만큼 **앞서** 놓는다 (`+0x291`).
- 이 둘이 **지역 변수가 아니라 작업 지시서에** 적혀 있다는 게 중요하다.
  - 그래서 snap 을 **중간에 멈췄다가, 나중에 같은 자리부터 다시** 할 수 있다.
  - 실제로 멈추는 경우가 2.7 이다.
- 비유: 주소록을 채우다 잠깐 손을 떼야 할 때, **작업 지시서에 책갈피**를 꽂아 둔다. "3번째 건물, 그 안에서 7칸까지 했음". 다음 사람(또는 다음 번의 나)은 거기서부터 잇는다.

**IAT 범위 `ctx+0x70/+0x78` 은 snap 본문이 안 읽는다** `[정적]`

- run22 의 덮개 열기(`LdrpPrepareImportAddressTableForSnap`)가 적은 칸인데, snap 본문 `.asm` 에는 이 칸을 읽는 줄이 없다.
- 주소록 칸 자리는 **서술자의 FirstThunk** 로 정한다.
- `+0x70/+0x78` 은 끝맺음 `LdrpDoPostSnapWork` 가 **덮개를 다시 닫을 때** 쓴다 (2.9).
- 비유: "덮개 위치" 메모는 덮개를 여닫는 사람만 본다. 칸을 채우는 사람은 서술자를 보고 줄을 찾는다.

### 2.6 질문 2 — 다른 DLL 에 있다는 안내문 (forwarder)

**답: 예, forwarder 처리다. 지연 import 가 아니다** `[정적]`

**어떻게 "이건 안내문이다" 를 알아보나** `[정적]`

1. 찾은 "주소" 를 계산한다 (`+0x3ac`).
2. 그 주소가 **자식 건물의 export 디렉터리(이름표) 범위 안**인지 본다 (`+0x3c0`, `+0x487`).
3. 안이면 → 그건 함수 코드가 아니라 **`"다른DLL.함수"` 꼴 글자**다 (PE 규격 forwarder).
   - 이름 import 든 서수 import 든 같다.
- 비유: 호실 이름표에서 번호를 찾았는데, 그 "번호" 가 가리키는 곳이 **이름표 판 자기 자신**이다. 즉 번호 자리에 **안내문 종이가 붙어 있는** 것이다. "이 담당은 OO 건물의 △△ 입니다".

**안내문 읽기** `[정적]`

1. `+0x4e5` (ret `+0x4ea`) `strrchr(문자열, '.')` — **마지막 점**에서 자른다. 앞 = 건물 이름, 뒤 = 함수 이름.
   - 점이 없거나 건물 이름이 너무 길면 `0xc000007b`.
2. 점 바로 뒤가 `'#'` 이면 (`+0x518`) → `DLL.#서수` 꼴. `+0x93d ~ +0x94b RtlCharToInteger` 로 번호를 숫자로 바꾼다 (ret `+0x950`). 이름 포인터는 0 으로 둔다 (`+0x958`).
   - 비유: 안내문에 "OO 건물 **3번 호실**" 이라고 번호로 적힌 경우.

**갈림 — 안내문이 가리키는 건물이 관리실인가** `[정적]`

- **`NTDLL` 이면 → 아무도 안 부른다.**
  - 길이 5 이고 `ntdll` 과 **대소문자 무시**로 같으면 (`+0x533 ~ +0x555`)
  - `+0x649` 자식 = `LdrpNtDllDataTableEntry` (관리실 명부 줄, [run35 해설](run35-ntdll-entry-해설.md) 의 그 전역 칸).
  - 비유: 안내문이 "**관리실** 로 가세요" 면, 관리실은 가게 문 열 때부터 이미 있다. 부를 필요 없이 관리실 이름표를 바로 편다.
- **그 밖이면 → 그 자리에서 가져온다.**
  - `+0x5a5 LdrpAppendAnsiStringToFilenameBuffer` 로 건물 이름을 담고
  - **`+0x5da` (ret `+0x5df`) `LdrpLoadDependentModuleInternal`** — 배달 기사를 부른다.

**배달 기사에게 주는 것** `[정적]` (run23 의 인자 번호)

| 번호 | 값 | 뜻 |
|---|---|---|
| 1 | 대상 건물 이름 | 안내문의 점 앞부분 |
| 2 | `ctx` | 주소록 **주인** 건물(importer)의 작업 지시서 |
| 3 | 안내문을 가진 건물 항목 | 이름표를 내보낸 쪽 (수출하는 모듈) |
| 4 | **1** | 주문 사유 `LoadReasonStaticForwarderDependency` |
| 5 | 결과 칸 | 가져온 건물 명부 줄을 돌려받는 칸 |
| 6 | 간선 칸 | 끝에 `+0x855 RtlFreeHeap` 으로 푼다 |

- 이름 안의 api-set 풀이는 배달 기사 안의 몫이다 (결과서, run23 해설 2.5).

**가져온 뒤 — 대상 건물 이름표에서 다시 찾기** `[정적]`

- `+0x674 ~ +0x7d9` 에서 대상의 export 를 다시 찾는다. 이번엔 **hint 없이** 바로 이분 탐색이다 (안내문엔 hint 가 없으니까).
- 대상 주소가 **또 안내문**이면 → 다시 한다. **32번**이 되면 `0xc000007b` (`+0xc22`, `+0xfd5`).
  - 비유: 안내문을 따라갔더니 또 안내문. 이렇게 32번 돌면 "**뭔가 고리가 꼬였다**" 고 보고 포기한다.

**지연 import 가 아닌 근거** `[정적]`

- 이 함수가 읽는 NT 헤더 칸에 **지연 import 디렉터리(13번)가 없다** (결과서).
- 그러니 flow.md 2.5 의 `[추측]` "지연 import 또는 forwarder" 는 **forwarder** 로 좁혀졌다.

**가져온 건물은 누구의 자식이 되나 — 주소록 주인(importer)** `[정적]`

1. 카드 사이 줄(간선)은 배달 기사 안 `+0xf69/+0xf8b` 가 긋는다 (run23).
2. 그때 부모 카드 = **인자 2 의 건물** = 주소록 주인(importer).
3. 그러니 안내문 대상 건물은 **"안내문을 가진 건물" 의 자식이 아니라, 주소록 주인의 자식**으로 줄이 그어진다. 다른 건 주문 사유가 1 이라는 것뿐이다.

- 비유: test.exe 주소록을 채우다 kernel32 이름표에서 "OO 건물로 가세요" 안내문을 봤다면, OO 건물은 **test.exe 의 연결 카드 밑에** 줄로 매달린다. kernel32 밑이 아니다.

### 2.7 안내문 대상이 아직 안 왔으면 — 잠깐 멈췄다 이어서

이 절은 run23 해설 2.7 의 `[추측]` 하나가 **맞았다**는 이야기다. 천천히 간다.

**출발점 — snap 이 불릴 때 체크판은 이미 0** `[정적]`

- 건물 담당 `+0x4c2` 는 체크판이 0 일 때만 snap 을 부른다 (run22).
- 게시판 길도 `LdrpSignalModuleMapped` 가 체크판을 0 으로 만든 뒤 snap 일감을 넣는다 (run24).
- 그러니 snap 안에서 배달 기사를 부를 땐 **주소록 주인 지시서의 체크판(`ctx+0x6c`) 이 0** 이다.

**그래서 run23 의 "체크판 0" 갈래가 여기서 탄다** `[정적]`

1. 배달 기사는 "체크판이 이미 0 인데 **아직 안 온 자식**(`State < 2`)" 을 만나면:
   - `ctx+0x50` = 그 자식
   - `ctx+0x6c` = 1 (체크판을 다시 1 로)
   - 주소록 주인 카드 `State = 3` (`WaitingForDependencies`, **기다림 도장**)
   - 반환 **`0x103`**
2. snap 은 `0x103` 을 받으면 (`+0x634`, `+0x85f CMP R12D,0x103`):
   - **끝맺음(`LdrpDoPostSnapWork`)도, 도장 5 도 건너뛰고**
   - `+0xeb7 XOR EAX,EAX` — **0 (성공) 으로 일단 나간다.**
3. 이때 책갈피 `ctx+0x80/+0x84` 는 **그대로**다. 멈춘 칸은 `+0x402` 의 "하나 올리기" 에 도달하지 못했다.

**다시 불리는 길** `[정적: 줄 관계]`

1. 그 자식이 도착하면(Mapped) `LdrpSignalModuleMapped` 가 주소록 주인 체크판을 1 → 0 으로 지운다.
2. 0 이 됐으니 주인 카드 `State = 4` + `LdrpQueueWork` — 게시판에 "주소 채워 주세요" 쪽지 (run24).
3. 그 쪽지를 `LdrpProcessWork +0x131` 이 떼어 snap 을 **다시** 부른다.
4. snap 은 책갈피를 보고 **멈췄던 그 칸부터** 잇는다.

- 줄 관계는 정적으로 보였지만, 실제로 이 순서로 도는지는 동적이다.
- 대상이 이미 `State ≥ 2` 면 배달 기사가 0 을 돌려줘서 멈추지 않고 그냥 이어간다.
- 대상을 가져오기가 **실패**하면 `+0x5ea JS` → `+0xe03` 에서 `*(importer.LoadContext+0x28) = 상태` → 오류 처리.

**비유로 한 번에**

- 주소록을 채우다 "OO 건물로 가세요" 안내문을 만났다.
- OO 건물을 주문했는데 **아직 트럭 위**다.
- 그럼 직원은 지시서에 **책갈피**를 꽂고, 카드에 "**기다리는 중**" 도장을 찍고, 체크판에 "1" 을 적고 **일단 손을 뗀다.**
- OO 건물이 도착하면 체크판이 지워지고, 게시판에 "이어서 채워 주세요" 쪽지가 붙는다.
- 누군가 그 쪽지를 떼어 와서 **책갈피 자리부터** 다시 채운다.

**run23 `[추측]` 판정**

- run23 해설 2.7: "`0x103` 길은 **다른 caller** 몫으로 보인다 `[추측]`: `LdrpSnapModule +0x5df`, `LdrpLoadDependentModuleW +0xae`."
- → **snap 쪽은 맞다** `[정적]`. `LdrpLoadDependentModuleW +0xae` 쪽은 그대로 `[추측]` (run 문서 flow.md 판정 표).
- 디버거로 볼 때 주의 (run 문서 "디버거 메모"): 이렇게 미뤄지면 **같은 건물의 snap 입구(`ntdll+0x3a8c0`)가 두 번** 설 수 있다. 두 번째는 `ctx+0x80`/`+0x84` 가 0 이 아니다.

### 2.8 질문 3 — 도장 5 는 한 곳, 끝맺음 뒤

**답: `+0xb87` 한 곳** `[정적]`

- 명령: `+0xb87 MOV dword ptr [RAX+0x38],0x5` (`18003b447`).
- `RAX` = 주소록 주인 항목의 `DdagNode` (연결 카드), `+0x38` = `State` (진행 도장).
- 번호 5 = `Snapped` ([run21 해설](run21-load-dll-internal-해설.md) 2.6 표). run21 이 **이름으로만** "함수 주소 잇기 끝" 이라 읽었던 그 도장이, 정말로 **주소록을 다 채운 뒤에** 찍힌다는 게 코드로 보였다.

**앞뒤 순서** `[정적]`

1. `+0xb59` — 그때까지 상태가 음수면 → 끝맺음·도장 둘 다 건너뜀
2. **`+0xb61` 끝맺음 `LdrpDoPostSnapWork`** (ret `+0xb66`)
3. `+0xb69` — 끝맺음 결과가 음수면 → 도장 건너뜀
4. `+0xb7b` 끝 로그 (ret `+0xb80`)
5. **`+0xb87` 도장 5**
6. `+0xb8e` 꼬리 (`ctx+0xc0` 뷰 닫기 등)

- 비유: 주소록을 다 채우고 → 덮개를 닫고 → 일지에 "끝" 을 적은 **다음에야** 카드에 도장을 찍는다. 덮개 닫기가 실패하면 도장은 안 찍는다.

**실패하면 도장은?** `[정적]`

- 이 함수는 **안 쓴다.** 시작 때의 4 (`Snapping`) 그대로다.
- 실패는 **반환값으로만** 나간다 (`+0xbc1`). caller 가 그걸 `*(ctx+0x28)` 에 옮긴다 (run22·run24).
- 그럼 오류 도장 −3 (`LdrModulesSnapError`) 은 **누가 처음 찍나?** → **못 찾았다** `[공백]`.
  - `.asm` 모음에서 −3 을 쓰는 줄은 `LdrpCondenseGraphRecurse +0x14e` 하나뿐이다. 이건 "자식이 −3 이면 부모에게 옮기는" 전파 줄이다 (run24).
  - → 정적 **Q69**.

**도장 5 를 찍는 곳은 이제 둘** `[정적]`

| 곳 | 언제 |
|---|---|
| `LdrpSnapModule +0xb87` | 주소록이 있는 건물, 다 채운 뒤 (이번) |
| `LdrpMapAndSnapDependency +0x5ae` | 주소록이 **없는** 건물 (snap 을 안 부르고 바로, run22) |

### 2.9 질문 4 — 끝맺음 LdrpDoPostSnapWork

228바이트짜리 작은 함수다. 하는 일은 넷이다.

```
LdrpDoPostSnapWork(ctx)                       E = ctx+0x38 (주소록 주인 항목)
 ├─ (1) +0x43  NtProtectVirtualMemory        덮개 다시 닫기        (ctx+0x70 ≠ 0 일 때)
 ├─     +0x49 ~ +0x66  CFG 칸 검사            다르면 INT 0x29       (ctx+0xa0 ≠ 0 일 때)
 ├─ (2) +0x93  LdrpHandleTlsData(E)           TLS 서류철 장         (E.TlsIndex == 0 일 때)
 ├─ (3) +0x6d  LdrControlFlowGuardEnforcedWithExportSuppression()
 └─ (4) +0xa9  LdrpUnsuppressAddressTakenIat(E.DllBase, 0, 0)       ((3) ≠ 0 일 때)
```

- 오프셋은 ret. call 은 `+0x3e`, `+0x8e`, `+0x68`, `+0xa4` `[정적]`.
- 번호는 **코드가 실제로 지나가는 순서**다. 오프셋으로는 `+0x68` 이 `+0x8e` 보다 앞이지만, 점프(`+0x53`/`+0x5f JZ +0x84`)로 TLS 를 **먼저** 보고, 그 뒤(`+0x89 JNZ`·`+0x97 JNS`)에 `+0x68` 로 간다 `[정적]`. ([오프셋 순서 ≠ 실행 순서](run9-create-user-process-해설.md), run9 해설 3.7)

**(1) 덮개 다시 닫기 — run22 덮개 열기의 짝 (예)** `[정적]`

1. run22 의 `LdrpPrepareImportAddressTableForSnap` 이 주소록을 **`PAGE_READWRITE`(4)** 로 열면서, 원래 보호값을 `ctx+0x90` 에 적었다.
2. 여기서 `NtProtectVirtualMemory(-1, &ctx+0x70, &ctx+0x78, [ctx+0x90], &임시)` — **같은 범위를, 적어 둔 원래 값으로** 되돌린다.
3. 주소록이 없는 건물(`ctx+0x70 == 0`)은 건너뛴다. 실패(음수)면 그대로 끝 (TLS·CFG 안 함).

- 비유: 칸을 채우려고 열었던 **주소록 덮개를 다시 닫고 잠근다.** 열 때 "원래 어떤 자물쇠였나" 를 지시서에 적어 뒀으니, 그대로 다시 채운다.
- 그래서 2.5 의 `ctx+0x70/+0x78` 은 **여닫는 사람만** 쓰는 칸이었다.

**칸 검사 — 덮개가 열린 동안 누가 건드리지 않았나** `[정적]`

- 덮개를 열 때(Prepare) 모듈 LoadConfig 의 `GuardCFCheckFunctionPointer` 칸 주소를 `ctx+0xa0` 에, 그때 값을 `ctx+0x98` 에 적어 둔다.
- 여기서 지금 값과 비교해서 **다르면 `INT 0x29` (ECX = `0x13`, 빠른 실패)**.
- 모듈이 CFG 계측일 때만 채워진다 (결과서). `0x13` 의 SDK 이름은 `[추측]`.
- 비유: 덮개를 연 동안 **교환원 연락처 칸**([CFG 교환원](run28-tls-init-call-해설.md), run28 해설 2.9)가 바뀌었는지 확인한다. 바뀌었으면 누가 몰래 손댄 것이니 **그 자리에서 가게를 멈춘다.**

**(2) TLS 서류철 장 — DLL 쪽은 여기서 만든다** `[정적]`

- 조건은 하나: 항목 **`TlsIndex(+0x6e) == 0`**. 이 함수엔 exe / DLL 구분이 **없다.**
- `TlsIndex` 는 TLS 서류철 장이 만들어지면 `0xffff` 가 된다 (`LdrpHandleTlsData +0x3a8`, `LdrpInitializeTls +0x102`, [run28 해설](run28-tls-init-call-해설.md) 2.3).
- 그러니 들어가는 건 "**아직 서류철 장을 안 만든 건물**" 뿐이다.
- run28 해설 2.3 표의 "DLL 쪽 `LdrpHandleTlsData`" 를 **부르는 곳**이 이번에 나왔다: `LdrpDoPostSnapWork +0x8e` (ret `+0x93`).

exe 와 DLL 이 갈리는 이유를 단계로 보면:

1. exe 의 서류철 장은 `LdrpInitializeProcess +0x18cb LdrpInitializeTls` 가 만든다 (run28) `[정적]`.
2. 그 호출 오프셋이 exe 건물 담당 `+0x1aa4 MapAndSnap` 보다 **앞**이다 `[정적]`.
3. 그래서 exe 가 끝맺음에 올 땐 이미 `TlsIndex ≠ 0` 이라 건너뛸 것이다 `[추측: 오프셋 순서]`.
   - 왜 아직 짐작인가: 오프셋이 앞이라는 것과 **실제로 먼저 실행됐다**는 것은 다르다. 실행 순서는 따라가 보지 않았다.
4. dll1·dll2 는 TLS 디렉터리가 있다 `[소스]`. 끝맺음에 올 때 `TlsIndex == 0` 이라 여기서 `LdrpHandleTlsData` 로 들어간다 `[정적: 조건 줄]`.
5. TLS 디렉터리가 **없는** DLL 은 들어가도 callee 안 `+0x88` 에서 건너뛴다 (run28).

- 비유: 본 건물(exe)은 개업 준비 때 **미리** 서류철 장을 끼워 뒀다. 다른 건물(dll1·dll2)은 **주소록을 다 채우고 덮개를 닫을 때** 서류철 장을 끼운다.

**그래서 시간 순서가 하나 생긴다** (flow.md 2.7) `[정적]`

- DLL 의 TLS 서류철 장은 **그 DLL 의 주소 채우기가 끝날 때, 도장 5 바로 앞에서** 생긴다.
- 전원 켜기(도장 7 → 9, TLS **콜백** 부르기, run27·run28) 보다 **훨씬 전**이다.
- 서류철 **장 만들기**와 TLS **콜백 부르기**는 다른 일이다. 콜백은 전원 켤 때다.

**(3)·(4) CFG 쪽** `[정적]` + `[추측]`

- `LdrControlFlowGuardEnforcedWithExportSuppression()` 이 0 이 아니면 `LdrpUnsuppressAddressTakenIat(DllBase, 0, 0)`. 실패면 로그.
- 뜻("CFG 켜짐 + export 억제" 프로세스에서, 이 DLL 이 import 한 함수의 억제를 풀기)은 **이름과 로그 문구로 읽은 것** `[추측]`. 켜졌는지는 동적.

**이 함수도 카드 도장·체크판은 안 쓴다** `[정적]`.

### 2.10 질문 5 — 부모는 안 건드린다

**답: 아니오 — snap 은 부모 카드·부모 체크판을 안 쓴다** `[정적]`

1. 2.3 의 "스택 밖 쓰기 7줄" 에 부모 쪽(`ctx+0x30`)·`ctx+0x6c` 를 쓰는 줄이 없다.
2. `ctx+0x30` 은 **읽지도 않는다.**
   - `R15+0x30` 을 읽는 줄(`+0x674`·`+0x7ed`)이 있긴 한데, 그때 `R15` 는 지시서가 아니라 **자식 항목**이다. 그래서 그 칸은 자식의 `DllBase` 다.
   - 비유: 같은 "30번 칸" 이라도 **어느 서류의** 30번 칸인지 봐야 한다. 그때 손에 든 건 지시서가 아니라 명부 줄이었다.
3. 도장 쓰기는 **자기 카드** `+0xb87` 하나. 끝맺음도 안 쓴다.
4. 부모에 영향이 가는 유일한 곳은 2.7 의 배달 기사 안(`ctx+0x6c`·`ctx+0x50`·주인 `State = 3`)이다. 그건 snap **중간**의 일이지 **끝**의 일이 아니다.
5. caller 쪽 마무리도 실패 상태 옮기기(`*(ctx+0x28)`)뿐이다.

**그래서 — 부모는 자식이 "주소 다 이음" 이 되기를 기다리지 않는다** `[정적: 줄 관계]`

- 부모 체크판을 지우는 건 snap 이 아니라 **자식이 도착하는(Mapped) 순간**의 `LdrpSignalModuleMapped` 다 (run24).
- 그러니 부모가 주소를 채우기 시작하는 조건은 "자식들이 **도착했다**" 이지, "자식들이 **자기 주소록까지 다 채웠다**" 가 아니다.
- run24 해설 2.10 이야기 그림과도 맞는다: 거기선 게시판에서 **exe 꽂기(2)가 dll1 꽂기(4)보다 먼저** 나왔다.

왜 그래도 되나 — 내 짐작 `[추측]`:

- 2.3 에서 본 것처럼 주소록에 적는 값은 "자식 `DllBase` + 이름표의 함수 RVA" 다.
- 이 계산에 필요한 건 **자식이 땅에 놓였다는 것(DllBase)** 과 **자식의 호실 이름표(export 표)** 뿐이다. 둘 다 도착(Mapped) 때 이미 있다.
- 자식 **자신의 주소록**이 채워졌는지는 이 계산에 안 쓰인다.
- 그래서 기다릴 필요가 없는 것으로 보인다. 다만 이 "이유" 는 코드에 쓰여 있지 않다.
- 비유: 옆 건물의 **호실 번호**를 내 주소록에 적는 데, 옆 건물이 **자기 주소록**까지 다 채웠을 필요는 없다. 건물이 와 있고 이름표만 붙어 있으면 된다.

### 2.11 질문 6 — 작은 갈래와 실패 코드

test.exe 길에선 대부분 **안 탈 것**으로 보이는 갈래들이다. 짧게.

| 갈래 | 조건 | test.exe 에서 |
|---|---|---|
| `+0x81 LdrpHandlePendingModuleReplaced` | 입구에서 무조건. 조건은 callee 안 (안 읽음) | 섬 |
| `+0xfc LdrpFreeReplacedModule` | 가져오는 사이 같은 DLL 이 **다른 항목으로 교체**됐을 때만. `ctx+0x58[번호]` 를 바꿔 치고(`+0xa72`·`+0x105`) 옛 항목 정리 | 안 탈 것 `[추측]` |
| `+0xd5d` / `+0xecd LdrpCheckRedirection` | 지시서 `+0x20` 비트 `0x2000000` (run22 `MapAndSnap +0xd6` 이 `LdrpRedirectionModule ≠ 0` 일 때 켬). 돌려준 값 `0xffffffffffbadd11` = 리다이렉트 없음 | 안 탈 것 `[추측]` |
| `+0xe29` / `+0xe44 CompatCachepLookupCdb` | **못 찾음** 오류 처리 안에서만 (`+0xe0b`). 로그만 다르고 상태는 안 바뀜 | 안 탐 |
| PE32(`0x10b`) 복사본 `+0x8a2 ~ +0xa4b` | 32비트 이미지용으로 같은 일을 한 벌 더 써 둔 곳 | 64비트끼리는 안 탈 것 `[추측]` |

**못 찾았을 때** `[정적]`

- thunk 칸마다 "찾는 중" 값 `0xc000007a` 로 시작한다 (`+0x2bc`). 찾으면 0 이 된다.
- 끝내 못 찾으면 (판정 `+0xe59`):
  - 이름 import → **`0xc0000139`** (`STATUS_ENTRYPOINT_NOT_FOUND`, `+0xe5e`)
  - 서수 import → **`0xc0000138`** (`STATUS_ORDINAL_NOT_FOUND`, `+0xe76`)
  - 둘 다 `+0xe8f LdrpReportError` (ret `+0xe94`) 를 거친다.
- 그 밖: 이름표 없음·안내문 이상·너무 깊음 `0xc000007b`, 헤더 이상·서수 0 `0xc000000d`.
- 안내문 대상 건물을 **못 가져와도**(`0xc0000135` 등) `+0xe0b` 에서 `0xc0000139` 로 바뀌어 나갈 수 있다. 원래 값은 `LoadContext+0x28` 쪽에 남는다 (`+0xe03`).
- 비유: "이 이름의 담당이 이름표에 없어요" 가 `0xc0000139`, "그 번호 호실이 없어요" 가 `0xc0000138`.

**곁가지 둘**

- 예외: 필터 `LdrpSnapModule$filt$0` → `LdrpGenericExceptionFilter`. 착지점 `+0xe9e ~ +0xeb6` (`.asm` 에 없는 틈이라 결과서가 **원본 바이트**로 읽음)은 꼬리 `+0xb93` 으로 간다. 어느 구간이 `__try` 인지는 안 봤다.
- `+0x605` 의 Ghidra 이름 `RtlDeleteBoundaryDescriptor` 는 **믿지 말 것.** 같은 주소(`ntdll+0x3d630`)에 이름이 9개 있고, 실제로는 "프로세스 힙에서 해제하는 7줄짜리 껍데기" 다. 여기선 안내문 건물 이름 버퍼가 힙으로 커졌을 때만 푼다 `[정적]`.

### 2.12 test.exe 에서는 — 파일을 세어 본 값

여기부터는 **`[정적+샘플]`** 이다 (맨 위 설명). 코드가 아니라 **파일 표를 센 값**이다.

**주소록 칸 수 = `+0x3e7` 이 서는 횟수** `[정적+샘플]`

| 건물 | 서술자 (import 수) | 주소록 칸 (`+0x3e7`) | 안내문(forwarder) |
|---|---|---|---|
| `test.exe` | `dll1.dll` (1: `Dll1Hello`, hint 0) · `KERNEL32.dll` (90) | **91** | KERNEL32 쪽 10개 → 모두 `NTDLL.Rtl…` |
| `dll1.dll` | `dll2.dll` (1: `Dll2Hello`, hint 0) · `KERNEL32.dll` (88) | **89** | 10개 → `NTDLL.Rtl…` |
| `dll2.dll` | `KERNEL32.dll` (88) | **88** | 10개 → `NTDLL.Rtl…` |

- test.exe 의 서술자는 `dll1.dll` 과 **`KERNEL32.dll` 둘**이다. 앞 해설 그림들은 dll1 바퀴만 그렸다. KERNEL32 바퀴는 이미 올라온 건물이라 체크판을 바로 하나 줄일 것이라 고리 끝 카운터는 그림과 같다 `[추측]` (flow.md 2.6).

**hint — KERNEL32 쪽은 전부 틀린다** `[정적+샘플]`

- `Dll1Hello`·`Dll2Hello` 는 hint 0 이 이름표 0번과 맞는다 → **첫 비교에서** 찾는다.
- KERNEL32 쪽은 **전부 틀린다.** 이 빌드 kernel32 이름표와 **정확히 5 씩** 어긋난다.
  - 결과서의 설명: 샘플을 만든 PC 의 import 라이브러리 기준 hint 라서.
- 그래서 KERNEL32 import 는 전부 "hint 자리 비교 한 번 + 이분 탐색" (이름 1697개, 대략 11걸음).
- **동작엔 문제가 없다.** 2.4 의 5번 — 틀리면 이분 탐색으로 간다 — 덕분이다.
- 비유: 쪽지에 적힌 "아마 몇째 줄" 메모가 **다른 해 이름표 기준**이라 다 5줄씩 밀려 있다. 그래도 반씩 접어 찾으면 금방 나온다.

**안내문 10개 — 전부 관리실 행** `[정적+샘플]`

- `InitializeSListHead`, `InterlockedPushEntrySList`, `InterlockedFlushSList`, `EnterCriticalSection`, `LeaveCriticalSection`, `DeleteCriticalSection`, `EncodePointer`, `HeapAlloc`, `HeapSize`, `HeapReAlloc` → 모두 `NTDLL.Rtl…`.
- 그러니 세 건물 다 **`+0x649` 관리실 지름길**만 타고, `+0x5df` 배달 기사는 **안 부를 것**이다 `[추측: 이 샘플 import 만 셈]`.
  - `RtlCharToInteger`(`#서수` 꼴), 2.7 의 `0x103` 멈춤도 안 탈 것 `[추측]`.
- kernel32 자신(서술자 104·import 1274)·kernelbase(4·750)의 import 도 api-set 호스트를 KernelBase 로 **가정해** 세 보니 안내문 대상은 NTDLL 뿐이었다 — 근사 `[정적+샘플, 가정 있음]`.
- 비유: test.exe 주소록에서 만나는 안내문 10장이 **전부 "관리실로 가세요"** 다. 관리실은 이미 있으니, 이번 시동에선 안내문 때문에 새 건물을 부를 일이 없어 보인다.

**dll2 의 import 가 채워졌다** `[정적+샘플]`

- **`KERNEL32.dll` 하나뿐**이다 (88개).
- run22 해설 2.5 그림과 run24 해설 2.10 그림의 "**dll2 의 import = ?**" 칸이 이것으로 채워진다. flow.md 이야기 그림은 이걸 "이미 올라옴" 으로 이어 그렸다 (dll2 건물 담당 → 0 → 도장 4). 실제로 그렇게 도는지는 동적 Q23 `[추측]`.

**덤 — Q60 단서** `[정적+샘플, 간접]`

- 위 계산에서 test.exe 의 KERNEL32 import 90개 중 안내문은 위 10개뿐이고, **`LoadLibraryExW` 는 그 목록에 없다.**
- → kernel32 의 `LoadLibraryExW` 는 안내문(forwarder)이 **아닐 것**이다. (결과서가 이 이름을 따로 짚진 않았다.)
- 그렇다면 run7 `[관찰]` 의 `KERNELBASE!LoadLibraryExW` 와 이어 주는 건 kernel32 쪽 **짧은 스텁**일 것이다 `[추측]` → 정적 Q60 에서 확인.

### 2.13 앞 해설에 붙인 안내 한 줄

run 문서 "정정" 절은 "**없음**" 이다. 이전 run 의 `[추측]` 둘은 틀린 게 아니라 **좁혀지거나 맞았다.**

다만 앞 해설 본문에 **이번에 바뀐 내용이 예전 그대로** 남은 곳이 있어서, 그 해설 맨 위에 `> **정정 (run36)**: …` 한 줄씩 넣었다 (본문은 그대로).

| 해설 | 예전 글 | 이번 |
|---|---|---|
| [run19](run19-proc-init-modules-해설.md) 2.7 | 새 말 snap "IAT 칸마다 주소를 채워 넣는 일로 알려져 있다 `[추측]`" | `[정적]` `+0x3e7` (2.3) |
| [run20](run20-kernel32-load-해설.md) 2.5 | 새 말 forwarder "다른 DLL 에 있다고 넘겨 주는 것으로 알려져 있다 `[추측]`" | ntdll 이 그렇게 다루는 코드 `[정적]` (2.6) |
| [run21](run21-load-dll-internal-해설.md) 2.6 | 도장 5 `Snapped` "함수 주소 잇기 끝" `[추측: 이름]` | `[정적]` `+0xb87`, 주소록 다 채우고 끝맺음 뒤 (2.8) |
| [run22](run22-map-snap-dep-해설.md) 2.5 | "dll2 의 import `?`" | `KERNEL32.dll` 하나 `[정적+샘플]` (2.12). 2.8 덮개 열기의 짝 = `LdrpDoPostSnapWork +0x3e` (2.9) |
| [run23](run23-load-dependent-해설.md) 2.7 | "`0x103` 길은 다른 caller 몫 `[추측]`" | snap 쪽은 맞음 `[정적]` (2.7) |
| [run24](run24-work-queue-해설.md) 2.10·4절·끝 그림 | "dll2 의 import = ?", "IAT 쓰기·도장 5 쓰는 곳은 모름" | 채워짐 (2.3·2.8·2.12) |
| [run28](run28-tls-init-call-해설.md) 2.3·2.5 | `LdrpHandleTlsData` 를 부르는 곳 없음, 일꾼이 그 락을 잡을 수 있는지 "정적으로 못 정한다" | caller = `LdrpDoPostSnapWork +0x8e`. 일꾼에서 잡는 **길은 코드상 있다** `[정적: 길]`, 실제는 동적 (2.9, 3절) |

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 표시 규칙 | 없음 | 새 태그 **`[정적+샘플]`** (파일 표를 센 값, 코드 줄 아님) |
| 그림 2 `ja4` (`LdrpSnapModule [test.exe]`) | 조건만 ("고리 밖 한 번 …") | + 본문: 서술자 전부, `+0x3e7` 주소록 쓰기 한 줄, hint → 이분 탐색, forwarder (NTDLL 지름길), `+0xb87 State = 5` |
| 그림 2 `jw5`·`jw9`·`jw11` (일감 길 snap) | "(정적)" 만 | + 칸 수 **91 · 89 · 88**, `+0xb87 State = 5` |
| 그림 2 새 `(정적)` 상자 | 없음 | `LdrpDoPostSnapWork` 넷 (`ps1`~`ps4`, 라벨 `+0xb66`). dll1·dll2 아래 `LdrpHandleTlsData` 둘 (`ht3`·`ht4`, 라벨 `+0x93`). exe 쪽은 TLS 건너뛸 것 `[추측]` 이라 상자 글에만 |
| 그림 2 `jc2` (dll2 건물 담당) | "dll2 의 import 는 안 봄" | "import = KERNEL32.dll 하나 — 샘플 PE 셈" |
| 2.5 `+0x5df` 지도 줄 | (flow.md 엔 지도 줄만. "지연 import 또는 forwarder" `[추측]` 은 plan·요청서에) | **forwarder** `[정적]`, 지연 import 아님, NTDLL 이면 안 올림. kernel32·kernelbase 쪽도 안내문 대상은 NTDLL 뿐 `[정적+샘플]` 근사 |
| 2.6 `0x103` 갈래 | "다른 caller(`LdrpSnapModule +0x5df`, …) 몫" `[추측]` | **snap 쪽은 맞다** `[정적]` (`LdrpLoadDependentModuleW +0xae` 쪽은 그대로) |
| 2.6 새 `####` 절 둘 | 없음 | **`LdrpSnapModule`** (트리, 질문 1~6 요약, test.exe 샘플 값), **`LdrpDoPostSnapWork`** (트리, 넷) |
| 2.6 일감 한 바퀴·이야기 그림 | "`LdrpSnapModule(P문맥)` (본문 Q50)", "dll2 의 import = ?" | `+0x3e7` → `+0xb66` → `+0xb87`, 칸 수, dll1·dll2 `LdrpHandleTlsData`, dll2 import = KERNEL32 |
| 2.6 남은 칸 | "`LdrpSnapModule` 안 (IAT 쓰기, `State = 5`, Q50)" | **(해결됨, 정적)** |
| 2.7 −3 도장 | (전파 줄만) | + "처음 쓰는 곳은 모름 `[공백]`, snap 자신은 실패해도 도장 안 씀" → Q69 |
| 2.7 TLS | "DLL 은 `LdrpHandleTlsData +0x10b`" (부르는 곳 없음) | + caller `LdrpDoPostSnapWork +0x8e`. DLL 의 TLS 장은 **snap 끝, 도장 5 바로 앞**, 켜기 훨씬 전 |
| 2.7 일꾼 | (run24·run28) | + 일감엔 snap 도 있다 → 주소록 쓰기·도장 5·TLS 장 만들기가 **일꾼에서** 일어날 수 있다 `[정적: 길]`. run8 `[관찰]` "일꾼에서 DllMain·TLS 콜백 안 불림" 과는 안 어긋남 |
| 4장 공백 표 | Q50 줄 | **(해결됨)**. 새 줄: 동적 Q68, 정적 Q69. Q60 줄에 단서 |

### 큰 그림에서 어떻게 이어지나

- run22 ~ run24 에서 "건물 가져오기" 쪽은 거의 다 채웠다. 남은 큰 빈칸이 **"가져온 뒤 주소를 잇는 쪽"** 이었다. 이번에 그 칸이 코드로 찼다.
- 이제 한 건물이 겪는 일을 도장 순서로 쭉 말할 수 있다.
  - 0 → (가져오기) → **2 도착(Mapped)** → (자식 기다림, 3) → **4 주소 채우는 중** → `+0x3e7` 칸 채우기 → `LdrpDoPostSnapWork` (덮개 닫기, TLS 장) → **5 주소 다 이음 (`+0xb87`)** → (그래프 정리) 6 → … → 7 → 8 → 9 (전원 켜기, run27)
- 다음 구간과의 연결:
  - **전원 켜기 쪽(run27·run28)**: TLS 콜백은 켜기 때 서류철에서 명단을 꺼내 부른다. 그 서류철 장이 **언제 끼워지나**가 이번에 보였다 (snap 끝).
  - **일꾼 스레드 문제(Q25·Q47)**: snap 이 게시판 일감으로도 돈다. 그러니 주소록 쓰기·TLS 장 만들기(`LdrpTlsLock` 독점, run28) 가 **일꾼에서** 일어날 수 있는 **길**이 코드에 있다 `[정적: 길]`. 실제로 그런지는 같은 동적 실행에서 본다.
  - **`main` 쪽(run30, Q60)**: test.exe 가 `CALL [칸]` 으로 부르는 그 칸들이 여기서 채워진다. `LoadLibraryExW` 칸이 KERNELBASE 로 이어지는 방식은 Q60.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| snap 이 **어느 스레드에서, 어떤 순서로** 도나 (exe·dll1·dll2·kernel32) | 정적으로는 길만 보인다. 누가 일감을 떼는지는 실행 때 | **Q47** (동적): `ntdll+0x3a8c0` (snap 입구) 에서 건물 이름·`@$tid`, `ntdll+0x3b447` (도장 5) 에서 이름·`@$tid`. 기존 `ntdll+0x2155d` / `ntdll+0x21592` 와 같은 실행 |
| 주소록 칸에 실제로 써지는 값 — `Dll1Hello` 칸이 dll1 안 주소인가, 안내문 10개가 `ntdll!Rtl…` 인가 | 주소는 실행 때 값 | **Q68 (a)**: `ntdll+0x3aca7` 에서 `r rcx`, `ln @rsi` |
| 관리실 지름길이 정말 건물마다 10번인가 | 샘플 셈은 파일 값이라 `[정적+샘플]` | **Q68 (b)**: `ntdll+0x3af09` 횟수 |
| 안 타야 할 곳을 정말 안 타나 — 배달 기사 부르기, `0x103` 비교, 못 찾음 보고, 리다이렉트, 교체 정리 | 조건 값(`LdrpRedirectionModule` 등)이 실행 때 값 | **Q68 (c)**: `ntdll+0x3ae9a`, `+0x3b11f`, `+0x3b74f`, `+0x3b61d`, `+0x3a9bc` 에 0번 |
| 끝맺음 실제 값 — 되돌리는 보호값, `LdrpHandleTlsData` 가 **dll1·dll2 에서만** 서나 (exe 는 안 섬 `[추측: 오프셋 순서]`), CFG export 억제가 켜졌나 | 실행 순서·프로세스 설정 | **Q68 (d)**: `ntdll+0xa423e` (인자), **`ntdll+0xa428e`**, `ntdll+0xa426d` 뒤 `eax` |
| 갈래 조건 값 | 전역 값은 실행 때 | **Q68 (e)**: `dq ntdll+0x1d2600 L1`, `dq ntdll+0x1ea508 L1`, `db ntdll+0x1ea4ec L1` |
| 오류 도장 −3 (`LdrModulesSnapError`) 을 **처음** 찍는 곳, snap 실패 반환이 `*(ctx+0x28)` 뒤 어디서 도장으로 바뀌나 | `.asm` 모음엔 전파 줄 하나뿐 `[공백]` | **Q69** (작은 정적) |
| `LdrpSnapModule` 의 `__try` 범위, `LdrpHandlePendingModuleReplaced` 안 조건, 지시서 `ctx+0xc0` 뷰의 정체 | 이번 질문 밖 (`.pdata` 안 읽음, callee 본문 안 읽음, 결과서 "정체 모름") | 앞 둘은 flow.md 4장 Q69 줄 "그 밖 run36 에서 안 본 것", `ctx+0xc0` 은 결과서 "ctx 칸" 표에만 (대기열 번호 없음) |
| CFG 칸 검사의 `0x13` 이 SDK 의 어떤 이름인가, `LdrpUnsuppressAddressTakenIat` 의 정확한 뜻 | 이름·로그 문구로만 읽음 `[추측]` | 번호 없음 |
| kernel32 `LoadLibraryExW` 가 스텁인가 | 이번엔 샘플 셈의 간접 단서뿐 `[정적+샘플, 간접]` | **Q60** (정적, kernel32 PE 표) |
| 일꾼이 snap 일감을 맡아 `LdrpTlsLock` 을 독점으로 잡나 | 길만 코드에 있다 `[정적: 길]` | **Q25·Q47** (동적, 같은 실행) |

- 디버거로 볼 때 주의 (run 문서 "디버거 메모"):
  - `ntdll+0x3aca7` 은 건물마다 수십 번 이상 선다. kernel32·kernelbase snap 까지 합치면 **2천 번이 넘는다** (test.exe 91 · dll1 89 · dll2 88 · kernel32 1274 · kernelbase 750). 건물 이름 조건 + 횟수 제한이 꼭 필요하다 (dast-mentor 몫).
  - 도장 5 자리(`ntdll+0x3b447`)에서 주소록 주인 항목은 **`rdi`** 다. `rax` 는 연결 카드다.
  - 안내문 대상을 기다리느라 미뤄지면 같은 건물 snap 입구(`ntdll+0x3a8c0`)가 **두 번** 설 수 있다. 두 번째는 책갈피(`ctx+0x80`/`+0x84`)가 0 이 아니다 (2.7).

---

## 5. 더 읽기

- run 문서: [`run36-snap-module.md`](run36-snap-module.md) — 호출 지점 표, 큰 구간 표 8개, "하는 일 순서" 트리, 질문 1 ~ 6 절, 샘플 PE 계산, 대표 지점, flow.md 판정 표, 아직 확인 필요(멈춤점), 디버거 메모
- 결과서: [`_workspace/sast/31_snap-module_result.md`](../../_workspace/sast/31_snap-module_result.md) — "ctx 칸" 표 (스택 밖 쓰기 7줄), 질문별 답, `LdrpDoPostSnapWork` 표, test.exe 샘플 계산, 동적 확인거리 1 ~ 7
- 요청서: [`_workspace/sast/31_snap-module.md`](../../_workspace/sast/31_snap-module.md)
- flow.md: [`flow.md`](flow.md)
  - 그림 2 `(1)~(2) 사이` 의 `ja4`·`jw5`·`jw9`·`jw11`, 새 `ps1`~`ps4`·`ht3`·`ht4`, `jc2`
  - 2.5 `+0x5df` 지도 줄 아래 run36 줄
  - 2.6 `#### 주소 잇기 ntdll!LdrpSnapModule`, `#### snap 끝맺음 ntdll!LdrpDoPostSnapWork` 절, 일감 한 바퀴·이야기 그림
  - 2.7 −3 도장 줄, TLS 절의 caller 줄, 일꾼 줄
  - 4장 공백 표 (Q50 해결됨, Q68, Q69, Q60 단서)
- 앞 해설:
  - [`run19-proc-init-modules-해설.md`](run19-proc-init-modules-해설.md) — 2.7 snap 이라는 말이 처음 나온 곳
  - [`run20-kernel32-load-해설.md`](run20-kernel32-load-해설.md) — 2.5 호실 이름표, forwarder 라는 말
  - [`run21-load-dll-internal-해설.md`](run21-load-dll-internal-해설.md) — 2.6 진행 도장 번호표, 2.10 주문 사유
  - [`run22-map-snap-dep-해설.md`](run22-map-snap-dep-해설.md) — 2.1 import 서술자, 2.3 지시서 칸, 2.6 끝맺음, 2.8 덮개 열기
  - [`run23-load-dependent-해설.md`](run23-load-dependent-해설.md) — 2.3 간선, 2.7 체크판과 `0x103`
  - [`run24-work-queue-해설.md`](run24-work-queue-해설.md) — 2.6 쪽지 처리, 2.7 체크판 지우기, 2.10 이야기 그림
  - [`run28-tls-init-call-해설.md`](run28-tls-init-call-해설.md) — 2.3 서류철 장과 `TlsIndex = 0xffff`, 2.5 자물쇠
  - [`run35-ntdll-entry-해설.md`](run35-ntdll-entry-해설.md) — 관리실 명부 줄 `LdrpNtDllDataTableEntry` (이번 관리실 지름길이 쓰는 칸)

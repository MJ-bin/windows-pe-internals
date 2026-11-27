# run113 ntdll-user-structs — 해설

> 이 글은 [run113 문서](run113-ntdll-user-structs.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.
>
> **고쳐 읽기 (run154)**: 2.2 줄 ③ 의 "`DbgkpMarkProcessPeb` … ret `+0x2c8972`" 는 **조각 `FUN_140845053` 시작 기준** 수다. 함수 (`DbgkCopyProcessDebugPort`) 시작 기준으로는 **call `+0x2c8a0c` · ret `+0x2c8a11`** 이다 (같은 줄의 `+0x3c` · `+0x43` 은 함수 기준이라 두 기준이 섞여 있었다) `[정적]`. 또 2.2 줄 ③ · 2.7 의 "부모의 디버그 포트" 는 정확히는 **`NtCreateUserProcess` 를 부른 현재 프로세스** 의 것이고, 디버그 객체 핸들 속성이 **없을 때만** 본다 (cmd → test.exe 에선 같은 프로세스라 값은 같다) `[정적]`. 본문은 그대로 두었다 → [run154 해설 2.3 · 2.4](run154-dbgk-events-해설.md)

- 대기열: **Q78e1** — Q78e ("ntdll 앞쪽, 힙 빼고") 를 2026-11-15 에 그 자리에서 둘로 나눈 것 중 **유저 구조체 몫**. PEB · `PebLdr` · 모듈 항목 · 노드의 모르는 칸 92 (`_workspace/plan.md`, 이번에 **끝**)
  - 나머지 반쪽 **Q78e2** (ntdll 전역 쪽) 는 정적이 이미 끝났고 (sast/111), 문서는 다음 바퀴 **run114** 차례다.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다.
  - 남은 것이 간 곳 (4절):
    - 동적 → 이미 있던 **Q46** 에 덧붙임 (run113 몫, 모두 낮음)
    - 정적 → 새 **Q202** ① ~ ④
    - Q78e2 에 얹을 전역 → 다음 바퀴 (run114)
- 근거:
  - run 문서 [`run113-ntdll-user-structs.md`](run113-ntdll-user-structs.md) (가장 중요)
  - 요청서 [`_workspace/sast/109_ntdll-user-structs.md`](../../_workspace/sast/109_ntdll-user-structs.md) · 결과서 [`109_ntdll-user-structs_result.md`](../../_workspace/sast/109_ntdll-user-structs_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다.** 글은 기준 · 2.1 · 2.4 · 2.5 · 2.6 · 2.7 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): 3.1 · 3.4 · 3.5 · 3.6 에 새 줄, 2장 숫자가 **크게** 바뀜 (3절 표).
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 `[관찰]` 은 모두 **run44 때 찍어 둔 사진** (`ms/logs/04_baggage-read_{P1,P1a,P1b,P2}.txt`) 을 결과서 · flow-writer 가 칸마다 다시 읽은 것이다.
- **결과서와 run 문서의 숫자가 다른 곳이 있다** (2.11). 이 글은 **run 문서 숫자**를 따른다.
  - 결과서 93칸 → run 문서 92칸 (`NtGlobalFlag2` 를 뺌) → "0 을 씀" 16 → **15**.
  - 한 칸을 nt 와 ntdll 이 둘 다 쓰는 곳 둘을 **양쪽에** 셌다.

---

### 이미 푼 말 — 링크만

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- PEB = 가게 문 앞 **안내판** → [run10 해설 "PEB"](../02_sprint/run10-allocate-process-해설.md#peb)
- 모듈 = **건물**, 모듈 항목 (`_LDR_DATA_TABLE_ENTRY`) = **명부 줄**, 노드 (`_LDR_DDAG_NODE`) = 명부 줄마다 붙은 **연결 카드**, 로드 문맥 = **작업 지시서** → [run19 해설 2.6](../02_sprint/run19-proc-init-modules-해설.md) · [run22 해설 2.7](../02_sprint/run22-map-snap-dep-해설.md#27-ldrpallocatemoduleentry--명부-줄과-연결-카드를-한-쌍으로)
- 노드 `State` = 연결 카드의 **진행 도장** (5 ~ 9) → [run21 해설 2.6](../02_sprint/run21-load-dll-internal-해설.md)
- **고리** (서로 기대는 동그라미 묶음) 와 그래프 정리 (`LdrpCondenseGraph`) → [run24 해설 2.9](../02_sprint/run24-work-queue-해설.md#29-핵심--그래프-정리--서로-기대는-고리를-하나로)
- `LdrpInitializeGraphRecurse` = **순서 담당**, `LdrpInitializeNode` = **켜기 담당** → [run27 해설](../02_sprint/run27-init-graph-해설.md)
- 네 가지 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길) → [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법)
- **DWORD 되쓰기** = 표시 묶음을 통째로 다시 쓰면 안 바꾼 비트에도 "0 을 쓴" 것 → [run52 해설 2.7](run52-kthread-create-해설.md#27-핵심-2-dword-하나를-되쓰면-왜-다른-비트에-0-을-쓴-것이-되나)
- 범위 밖 길을 셋 (ntdll · nt · 다른 DLL) 으로 나누는 법 → [run54 해설 2.13](run54-teb-fill-해설.md)
- `KUSER_SHARED_DATA` (KUSD) = 커널이 모든 프로세스에 보여 주는 공용 정보 페이지 → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md)
- IFEO = exe 이름별로 실행 옵션을 적어 두는 레지스트리 자리 `[추측]` → [run10 해설](../02_sprint/run10-allocate-process-해설.md)
- shim (호환성 고치기 장치 `[추측]`) → [run66 해설](run66-apphelp-cache-해설.md)
- 잡 (job, 프로세스 여럿을 한 묶음으로 관리하는 커널 객체 `[추측]`) → [run82 해설 2.12](run82-exception-user-해설.md)
- 비상 연락망 (VEH) → [run82 해설 2.5](run82-exception-user-해설.md)
- 디버그 포트 (유저 디버거가 붙을 때 쓰는 연락 창구 `[추측]`) → [run81 해설 2.9](run81-exception-kernel-해설.md)
- 핫패치 = 가게 문을 안 닫고 선반 하나만 갈아 끼우기 → [run35 해설](../02_sprint/run35-ntdll-entry-해설.md)
- PEB `+0xf8` GDI **진열장** (`GdiProcessCallout` 이 건다) → [run86 해설 2.10](run86-gdi32-init-해설.md)
- 사진 지점 P1 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **Tarjan 번호** → 2.5
- 한 칸에 **이름이 둘** (정수 칸 · 비트 칸, `?[4B]` 겹침 칸) → 2.3
- **양쪽 셈** → 2.11
- 그 밖에 이번에만 나오는 칸 이름 (`PatchLoaderData` · `SwitchBackContext` · `WaitOnAddressHashTable` …) 은 그 자리에서 한 줄씩.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, PEB = 가게 문 앞 **안내판**.
- 모듈 = **건물**, 모듈 항목 = 명부의 **한 줄**, 노드 = 그 줄에 붙은 **연결 카드**.
- **하나만 바꿔 부른다.**
  - run 문서는 이번에 모듈 항목을 "**직원 명부의 한 줄**", 노드를 "**조직도 칸**" 이라고 불렀다.
  - 이 글은 앞 해설들대로 **명부 줄 · 연결 카드** 라고 부른다. 같은 뜻이다.
  - run 문서는 `LdrpInitializeGraphRecurse` 를 "켜기 담당" 이라고 썼는데, 앞 해설 (run27) 에선 이 함수가 **순서 담당** 이고 켜기 담당은 `LdrpInitializeNode` 다. 이 글은 run27 이름을 쓴다.
- run 문서가 이번에 쓴 그림 (그대로 가져온다):
  - 안내판 칸 124개 중 남은 62칸을 다 봤더니 대부분 "**우리 가게엔 해당 없음**" 이었다. 디버거 손님이 있을 때 (`BeingDebugged`), 체인점일 때 (`ProcessInJob`), 그림 그리는 가게일 때 (GDI 칸 셋) 만 적는 칸이다.
  - 새 명부 줄은 **연필 자국 없는 종이** (`HEAP_ZERO_MEMORY`) 에 쓴다. 명부 줄과 연결 카드를 서로 가리키게 묶고 (`NodeModuleLink`), 입사 시각 (`LoadTime`) 을 **공용 시계** (KUSD `SystemTime`) 에서 베낀다.
  - 노드 번호 (`PreorderNumber`) 는 카드 묶음을 처음 걸어 볼 때 붙이는 **방문 순번** 이다. 걷는 사람은 순서 담당이 아니라 **묶기 담당** (`LdrpCondenseGraphRecurse`) 이다.

### 한 줄로 먼저 — 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| 유저 구조체 넷의 모르는 칸 | **92 → 여전히 모름 0** |
| 92칸이 간 곳 | 아는 칸 **42** · 아무도 안 씀 **28** · 범위 밖 길 `[추측]` **22** |
| 구간에서 값이 생긴 7칸 (`P1~P1a` 4 · `P1a~P1b` 3) | 모두 **이미 아는 길 안** 의 ntdll 줄 `[정적]`, 사진과 맞음 `[관찰]` |
| `끝까지 0` 85칸 | 0 을 씀 **15** · 조건부 안 탐 **20** · 아무도 안 씀 **28** · 범위 밖 **22** |
| 모듈 항목 `Flags` 비트 10개 | 켜는 줄이 ntdll 에 없음. 그래도 **DWORD 되쓰기**라 "0 을 씀" 으로 셈 (오케스트레이터 결정) |
| 정정 ① | 명부 줄과 연결 카드는 **따로 두 번** 빌린다 (한 덩어리 아님) `[정적]` |
| 정정 ② | `PreorderNumber` 는 순서 담당이 아니라 **묶기 담당** `LdrpCondenseGraphRecurse +0x1e` 가 쓴다 `[정적]` |
| 정정 ③ | run112 트랩 프레임 다섯 칸의 "P1 에서도 0" = **"0 을 써서 0"** |
| run66 열린 질문 | `SwitchBackContext` 를 쓰는 건 `SbUpdateSwitchContextBasedOnDll +0xd9` → **닫힘** |
| 간접 호출 | 칸을 쓰는 것 **0개** `[정적]` |
| 짐 지도 2장 | 모름 **218 → 126**. 남은 유저 모름은 **KUSD 102 뿐** |

이 글에서 붙잡아 둘 세 줄:

> **(1) 안내판 · 명부 · 카드에 "모름" 이 하나도 안 남았다.**
> 92칸 = 아는 칸 42 + 아무도 안 씀 28 + 범위 밖 22. 남은 유저 쪽 모름은 공용 정보 페이지 (KUSD) 102칸뿐이다.
>
> **(2) 남은 칸 대부분은 "test.exe 같은 가게엔 해당 없음" 이다.**
> 디버거 · 잡 · shim · IFEO · GUI 가 있어야 쓰이는 칸이거나, 아예 쓰는 줄이 없는 빈칸이다.
>
> **(3) 옛 그림 둘을 고쳤다.**
> 명부 줄 + 연결 카드는 "한 덩어리" 가 아니라 따로 두 번 빌린 것이고, 노드 방문 순번은 묶기 담당이 매긴다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 한 구간이 아니라, 안내판 · 명부 · 카드를 **만지는 곳 전부** 를 훑었다. 그래서 위치가 여럿이다.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 ① 커널, 부모 cmd 스레드 안
  NtCreateUserProcess
   ├─ PspAllocateProcess ─ +0x2314 MmCreatePeb       안내판 만들기
   │                          +0x57a LeapSecondFlags = 0                      ← 이번 (2.6)
   └─ PspInsertProcess                               가게 등록
        ├─ +0xd9  PspImplicitAssignProcessToJob ─ … ProcessInJob   (잡 안일 때만)        ← 이번 (2.7)
        └─ +0x143 DbgkCopyProcessDebugPort ─ … BeingDebugged       (디버거 아래일 때만)  ← 이번 (2.7)
 ② test.exe 첫 스레드, ntdll
  LdrInitializeThunk → … → _LdrpInitialize
     +0x107 CrossProcessFlags 비트 1 켬 … +0x20f 끔                                    ← 이번 (2.3)
   └─ +0x16c  LdrpInitializeProcess
        ├─ (exe) +0x135e  LdrpAllocateModuleEntry   명부 줄 + 연결 카드              ← 이번 (2.4)
        │        +0x13be ~ +0x13c4  exe 항목 Flags DWORD 되쓰기                       ← 이번 (2.6)
        ├─ +0x14ee  LdrpProcessMappedModule  +0xa6 LoadTime                           ← 이번 (2.3)
        ├─ +0x1af2  LdrpPrepareModuleForExecution
        │     ├─ +0x12f  LdrpCondenseGraph ─ +0x26 LdrpCondenseGraphRecurse  번호 매기기 ← 이번 (2.5)
        │     └─ +0x7b   LdrpNotifyLoadOfGraph ─ … SbUpdateSwitchContextBasedOnDll       ← 이번 (2.3)
        └─ +0x1b29  LdrpInitializePatchData  PEB PatchLoaderData                       ← 이번 (2.3)
  → NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다 (run 문서 "호출 관계" 그대로). call 자리는 2.2 · 2.3 에서 따로 적는다.
- 명부 줄은 exe 만이 아니라 ntdll · DLL 것도 같은 `LdrpAllocateModuleEntry` 가 만든다 `[정적]` (run 문서 경로 `{ntdll · exe · DLL}`).
- 지도의 큰 길은 **그대로** 다. 새 단계도 새 caller 도 없다. 이미 있던 상자 **안쪽** 의 쓰는 줄만 늘었다.

### 무엇이 궁금했나

짐 지도 7.2 에 유저 구조체 넷의 "모르는 칸" 이 92 남아 있었다.

- `_PEB` 62 · `_PEB_LDR_DATA` 2 · `_LDR_DATA_TABLE_ENTRY` 24 · `_LDR_DDAG_NODE` 4.

질문은 두 덩어리였다 (요청서):

1. **구간이 정해진 7칸** — 사진에서 값이 생긴 순간은 아는데 쓰는 줄을 몰랐다.
   - `P1~P1a` 4칸: `CrossProcessFlags` · `FlagGroup` · `NodeModuleLink` · `LoadTime`
   - `P1a~P1b` 3칸: `PatchLoaderData` · `SwitchBackContext` · `PreorderNumber`
2. **`끝까지 0` 85칸** — 사진 8장 내내 0 이다. 이걸 네 가지 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길) 로 가를 수 있나?

덧붙여: 간접 호출이 이 칸들을 쓰나?

### 바퀴 전에 알던 것

- `[정적]` (run10) `MmCreatePeb` 이 안내판 첫 칸들을 채운다. (run111) 그중 `NtGlobalFlag2` 는 `MmCreatePeb +0x4b2`.
- `[정적]` (run22) `LdrpAllocateModuleEntry` 가 명부 줄 (`0x138`) · 연결 카드 (`0x50`) 를 두 번 빌린다 (ret `+0x2e` · `+0x60`). 힙 플래그 `| 8` 을 "0 으로 채워 달라" 로 읽은 건 `[추측]`.
- `[정적]` (run24) `LdrpCondenseGraphRecurse` 가 카드에 번호를 붙이고 고리를 찾는다 (`PreorderNumber` · `LowestLink`).
- `[정적]` (run35) `LdrpProcessMappedModule` 이 모듈마다 불리는 길.
- `[정적]` (run66) 열린 질문 ③: `SbUpdateSwitchContextBasedOnDll` 이 명부 줄 `SwitchBackContext` 를 쓰나?
- `[정적]` (run82) VEH · VCH 비트는 "초기화 동안 아무도 안 씀".
- `[정적]` (run85 · run86) user32 · GDI 가 쓰는 안내판 칸이 있다. "범위 밖으로 셀지는 결정" 으로 남음. `GdiDCAttributeList` · `GdiHandleBuffer` 는 그 줄기에서 안 나옴.
- `[관찰]` (run44) 이 칸들의 사진 값.

---

## 2. 찾은 것

### 2.1 한눈에 — 92칸은 어디로 갔나

run 문서 "요약 표" 를 구조체로 묶어서 옮겼다. 범위 밖 셋 (ntdll · nt · 다른 DLL) 은 한 칸에 합쳤다.

| | 칸 | 아는 칸 (구간 줄) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 | 범위 밖 | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `_PEB` | 62 | 2 | 3 | 9 | 26 | 22 (ntdll 4 · nt 6 · 다른 12) | 0 |
| `_PEB_LDR_DATA` | 2 | — | — | — | 2 | — | 0 |
| `_LDR_DATA_TABLE_ENTRY` | 24 | 4 | 11 (명시 1 · 되쓰기 10) | 9 | — | — | 0 |
| `_LDR_DDAG_NODE` | 4 | 1 | 1 | 2 | — | — | 0 |
| **합** | **92** | **7** | **15** | **20** | **28** | **22** | **0** |

- 아는 칸 = 7 + 15 + 20 = **42** (모두 정확).
- 아무도 안 씀 28 · 범위 밖 22 는 짐 지도 2장 아래 줄에서 따로 센다.
- 사진과 칸마다 맞춰 봤다 `[관찰]` run44. "정적과 동적이 다른 곳" 은 **없다**.

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `LdrpAllocateModuleEntry  (+0x2e 항목 0x138 · +0x60 노드 0x50, 둘 다 HEAP_ZERO_MEMORY)`

- **누가**: ntdll 로더. test.exe 첫 스레드가 돌린다.
- **어디서**: `+0x29` 와 `+0x5b` 에서 `RtlAllocateHeap` 을 **두 번** 부른다 (ret `+0x2e` · `+0x60`).
- **누구를**: `LdrpHeap` (로더가 쓰는 힙) 에서 빌린다.
- **무엇을 위해**:
  - 첫 번째 `0x138` 바이트 = 명부 줄.
  - 두 번째 `0x50` 바이트 = 연결 카드.
  - 둘 다 플래그 `NtdllBaseTag + 0x40000 | 8` — 이 `8` 이 `HEAP_ZERO_MEMORY` (0 으로 채운 종이) 다.
- 모두 `[정적]`. 바로 아랫줄 `+0x73 DdagNode` · `+0xdb/+0xde NodeModuleLink` · `+0xe9 Lock = 0` 은 그 두 장을 묶고 칸 하나를 0 으로 적는 줄이다 (2.3 · 2.4 · 2.6).

#### 줄 ②: `├─ +0x14ee  LdrpProcessMappedModule  +0xa6 LoadTime ← KUSD SystemTime`

- **누가**: `LdrpInitializeProcess` 가 `+0x14e9` 에서 부른다 (ret `+0x14ee`). exe 의 명부 줄이다.
- **어디서 / 누구를**: `LdrpProcessMappedModule +0xa6` 에서 명부 줄 `+0x100` `LoadTime` 에.
- **무엇을**: KUSD `SystemTime` (`0x7ffe0014`) 의 8바이트 사본. 공용 시계를 그대로 베낀다.
- 다른 모듈은 다른 곳에서 같은 함수에 온다: ntdll 은 `LdrpInitializeNtdllDataTableEntry +0x11f`, DLL 은 `LdrpMapDllWithSectionHandle +0x267`.
- `[정적]`. 사진: exe · ntdll 같은 값 `0x01dd646b35091f27`, k32 쪽은 그 뒤 값 `[관찰]`.

#### 줄 ③: `PspInsertProcess └ +0x143 DbgkCopyProcessDebugPort ─ DbgkpMarkProcessPeb +0x84 BeingDebugged (DebugPort 일 때만)`

- **누가**: 부모 cmd 스레드가 커널 안에서. `PspInsertProcess` 가 `+0x13e` 에서 부른다 (ret `+0x143`).
- **어디서**: `DbgkCopyProcessDebugPort +0x3c` 에서 부모의 디버그 포트 (`[RDX+0x308]`) 를 0 과 비교하고, `+0x43 JNZ` 로 갈린다.
- **누구를**: 0 이 아닐 때만 `DbgkpMarkProcessPeb` 까지 가서 (함수 본문에서 떨어진 조각 안의 call, ret `+0x2c8972`) `+0x84` 에서 안내판 `BeingDebugged` 에 쓴다.
- **무엇을**: `CL` = "새 가게의 디버그 포트가 0 이 아니다" 라는 참/거짓.
- 근거: `DbgkCopyProcessDebugPort` 는 `.asm` 으로 `[정적]`, `DbgkpMarkProcessPeb` 안쪽은 `.asm` 이 없어 결과서 `objdump` 로만 본 `[정적: objdump]`.
- test.exe 는 이 길을 **안 탄다** (2.7).

### 2.3 구간이 정해진 7칸 — 모두 이미 아는 길 안

**답: 7칸 모두 쓰는 줄을 찾았고, 이미 지도에 있는 함수 안이다. 쓴 주인은 모두 ntdll** `[정적]`. 사진 구간과 맞다 `[관찰]` run44.

#### 새 말: 한 칸에 이름이 둘

- PDB 는 **같은 4바이트** 를 "정수 칸 하나" 와 "비트 칸 여럿" 으로 **따로** 적어 두기도 한다.
  - 비유: 안내판의 한 칸에 이름표가 두 장 붙어 있다. "표시 칸 전체" 라는 이름표와 "그 안 1번 칸" 이라는 이름표.
- 짐 지도는 PDB 이름마다 한 줄씩 센다. 그래서 **비트 칸 줄은 알았는데 정수 칸 줄은 "모름" 에 남는** 일이 생긴다.
- 이번 7칸 중 둘이 이런 경우였다.

#### `P1~P1a` 4칸

| 칸 | 쓰는 줄 `[정적]` | 한 줄 풀이 | 사진 `[관찰]` |
|---|---|---|---|
| 안내판 `+0x50` `CrossProcessFlags` (정수 칸) | `_LdrpInitialize +0x107 BTS.LOCK` → `+0x20f BTR.LOCK` | 비트 1 `ProcessInitializing` 을 켰다가 끝에 끈다. 짐 지도 3.1 에 이미 있던 그 줄과 **같은 칸** (이름표가 둘이었을 뿐) | P1 0 → P1a 2 → P1b 2 → P2 0 |
| 명부 줄 `+0x68` `FlagGroup` | `Flags` 와 같은 DWORD. 처음 값 줄은 `LdrpAllocateModuleEntry +0x10d` · `+0x11a` · `+0x129` · `+0x194`, ntdll 은 `LdrpInitializeNtdllDataTableEntry +0x93`, exe 는 `LdrpInitializeProcess +0x13c4` | 이것도 이름표 둘 (`?[4B]` 겹침 칸) — 3.5 `Flags` 줄과 같은 칸 | exe `0x22c4` · ntdll `0x20a2c4` |
| 명부 줄 `+0xa0` `NodeModuleLink` | `LdrpAllocateModuleEntry +0xd4` → `+0xdb` · `+0xde` (카드 주소), 카드 쪽 `+0xe2` · `+0xe5` | 명부 줄 ↔ 연결 카드 `Modules` 가 **서로 가리킨다** (2.4) | `[노드 - 노드]` |
| 명부 줄 `+0x100` `LoadTime` | `LdrpProcessMappedModule +0xa6` | 공용 시계 사본 (2.2 줄 ②) | exe · ntdll `0x01dd646b35091f27` |

#### `P1a~P1b` 3칸

| 칸 | 쓰는 줄 `[정적]` | 한 줄 풀이 | 사진 `[관찰]` |
|---|---|---|---|
| 안내판 `+0x330` `PatchLoaderData` | `LdrpInitializeProcess +0x1b24` → `LdrpInitializePatchData +0x52` (`+0x49 GS:[0x60]` 로 안내판 주소) | `LdrInitState = 2` (`+0x1b1e`) **바로 뒤, 무조건**. `LdrpHeap` 에서 0 으로 채운 `0x18`B 블록을 받아 `[0] = 1` · `[8] = &LdrLoadDll` · `[0x10] = &LdrGetProcedureAddress` 를 넣고, 그 주소를 안내판에 둔다 | P1a null → P1b `0x25996f3bfa0` |
| 명부 줄 `+0xc0` `SwitchBackContext` | `LdrpSendPostSnapNotifications +0xa3` → `SbUpdateSwitchContextBasedOnDll +0xd9` | 카드 알림 때 모듈마다. `SbpDetermineDllContext` 가 돌려준 **호환 문맥 블록** 의 `+4` 주소. 인자 `R8D ≠ 0` 이면 건너뜀 (`+0xac` · `+0xaf`) | P1a null → P1b exe `0x7ffafb7d3014` |
| 카드 `+0x48` `PreorderNumber` | `LdrpCondenseGraphRecurse +0x13 INC [RDX]` → `+0x1e` | 묶기 담당의 방문 순번 — **정정**이라 2.5 에서 따로 | P1b exe 1 · ntdll 0 · k32 1 · kb 2 · dll1 2 · dll2 3 |

- **`PatchLoaderData`** 라는 이름과 블록 내용 (로더 함수 주소 둘) 으로 보아, 패치 쪽이 로더 함수를 부를 수 있게 둔 **연락처 쪽지** 로 보인다 `[추측: 이름과 내용에서 읽음 — 근거 문서는 뜻을 안 적음]`.
- **호환 문맥 블록**은 이름으로 보아 모듈별 호환성 설정 묶음으로 보인다 `[추측: 이름에서 읽음]`. 블록을 정하는 본문 (`SbpDetermineDllContext`) 은 안 봤다 → Q202 ④.

#### `SwitchBackContext` 사진 값 읽기 — 단계별로

결과서가 사진 값으로 계산한 것이다 `[관찰 계산]`.

1. ntdll 기준 주소 = PEB `Ldr` − `PebLdr` rva `0x1d2880` = `0x7ffafb660000`.
2. exe 의 값 `0x7ffafb7d3014` = 기준 + `0x173014`.
3. 호환 문맥 표는 rva `0x173010` 에서 시작하고, 한 칸이 `0x20` 바이트다.
4. 그래서 `0x173014` = 표의 **번호 0 칸** `+4`. k32 쪽 `+0x173094` 는 **번호 4 칸** `+4`.

- 이걸로 run66 의 열린 질문 ③ "`SbUpdateSwitchContextBasedOnDll` 이 이 칸을 쓰나" 가 **닫혔다**.

#### `LdrpHeap` 은 프로세스 힙과 같은가 — 사진에 이미 답이 있었다

- 결과서는 "`PatchLoaderData` 블록이 프로세스 힙 구역 안이라 `LdrpHeap` = 프로세스 힙으로 보인다" 를 `[추측]` 으로 적고 동적 D5 로 넘겼다.
- flow-writer 가 사진에서 바로 찾았다 `[관찰]`:
  - P1a `LdrpHeap` (`ntdll+0x1d2860` = `0x7ffafb832860`) 의 값 = `0x25996f30000`
  - = PEB `ProcessHeap`
- 그래서 D5 는 **새 dast 없이 닫혔다**. 짐 지도 1장 "항목 주소가 프로세스 힙 = `LdrpHeap` 영역 안 (run44)" 과도 같다.

### 2.4 정정 1 — 명부 줄과 연결 카드는 따로 두 번 빌린다

#### 무엇이 있었나

- 짐 지도 3.5 `DdagNode` 줄과 1장 그림에 "**항목 0x138 + 노드 0x50 한 쌍** (같이 만듦)" 이라고 적혀 있었다.
- 이 말은 "명부 줄 바로 뒤에 카드가 붙어 있는 **한 덩어리**" 로 읽힐 수 있다.

#### 코드는 어떤가 — 단계별로 `[정적]`

1. `LdrpAllocateModuleEntry +0x29` 에서 `RtlAllocateHeap` — 크기 `0x138` (명부 줄).
2. `+0x5b` 에서 `RtlAllocateHeap` **한 번 더** — 크기 `0x50` (카드).
3. 둘 다 `LdrpHeap`, 둘 다 `HEAP_ZERO_MEMORY`.
4. `+0x73` 에서 명부 줄 `+0x98 DdagNode` 에 카드 주소를 적는다.
5. `+0xd4 ~ +0xe5` 에서 명부 줄 `NodeModuleLink` 와 카드 `Modules` 를 서로 가리키게 묶는다.

- → 두 장은 **같이 태어나지만 따로 빌린 종이** 다. 붙어 있다는 보장이 없다.
- → 카드는 **늘 `DdagNode` (`+0x98`) 로** 찾는다. "명부 줄 주소 + `0x138`" 로 찾으면 안 된다.

#### 그래서 바뀐 것

- 짐 지도 3.5 `DdagNode` 줄과 1장 그림의 "한 쌍" → "**따로 두 번 할당, 한 덩어리 아님**".
- flow.md 2.6 `LdrpAllocateModuleEntry` 절은 처음부터 두 번 할당 (`+0x2e` · `+0x60`) 으로 적혀 있었다. run113 줄만 보탰다.
- run22 해설 2.7 도 "두 번 빌린다" 로 맞게 적었다. 다만 그 글의 "`| 8` = 0 으로 채움" `[추측]` 이 이제 `[정적]` 이 됐다 — run22 해설 맨 위에 "고쳐 읽기 (run113)" 한 줄을 달았다.
- 비유: 새 건물이 오면 명부에 **한 줄**, 연결 카드 **한 장**을 같이 만든다 — 여기까진 그대로다. 다만 둘은 **다른 서랍에서 꺼낸 종이** 고, 명부 줄에 "내 카드는 저기" 라는 쪽지 (`DdagNode`) 를 적어 둔다.

### 2.5 정정 2 — `PreorderNumber` 는 묶기 담당이 매긴다

#### 새 말: Tarjan 번호

- **Tarjan 번호** = 고리 (서로 기대는 동그라미) 를 찾으려고 그래프를 걸으며 카드마다 매기는 **방문 순서 번호** `[추측]` (일반적으로 알려진 설명).
  - 비유: 길을 걸으며 들른 집 대문에 "1, 2, 3 …" 번호 스티커를 붙인다. 나중에 "내가 처음 들른 집으로 되돌아가는 골목이 있나" 를 번호로 가린다.
  - 결과서가 이 모양을 "Tarjan 번호 매기기" 라고 불렀다. run24 해설 2.9 에서 이미 "**Tarjan 알고리즘**과 같은 모양 (이름은 `[추측]`)" 으로 다뤘다 → [고리 · 그래프 정리](../02_sprint/run24-work-queue-해설.md#29-핵심--그래프-정리--서로-기대는-고리를-하나로).

#### 무엇이 있었나

- 요청서는 `PreorderNumber` 를 **카드 걷기 (순서 담당 `LdrpInitializeGraphRecurse`)** 가 매긴다고 짐작했다.
- flow.md 2.6 의 run24 절은 처음부터 맞게 (묶기 쪽) 적혀 있었다. 짐작만 틀렸던 것이다.

#### 코드는 어떤가 — 단계별로 `[정적]`

```
LdrpPrepareModuleForExecution  +0x12a 호출 → LdrpCondenseGraph  +0x21 호출 → LdrpCondenseGraphRecurse
  +0x13  INC [RDX]                 같이 쓰는 번호를 1 올린다
  +0x1e  MOV [RCX+0x48],EAX        → PreorderNumber
```

1. 묶기 담당이 카드에 들어오면 **같이 쓰는 번호** 를 하나 올린다 (`+0x13`).
2. 그 번호를 카드 `PreorderNumber` (`+0x48`) 에 적는다 (`+0x1e`).
3. **같은 번호를** `LowestLink` (`+0x20`) 에도 적는다 (`+0x24`). → 짐 지도 3.6 `LowestLink` 줄과 같은 함수 · 같은 값.
4. 카드를 쌓아 두는 더미 (스택) 에 올릴 때 `CondenseLink` (`+0x40`) 에 이전 머리를 넣는다 (`+0x2a`).

- 짐 지도 3장에 줄임 하나가 생겼다: **`②C…` = `②P… → LdrpCondenseGraph (호출 +0x12a) → LdrpCondenseGraphRecurse (호출 +0x21)`** (`②P…` 는 `LdrpPrepareModuleForExecution` 까지).

#### 사진 값 `[관찰]`

- P1b: exe 1 · ntdll 0 · k32 1 · kb 2 · dll1 2 · dll2 3.
- ntdll 이 0 인 까닭: ntdll 카드는 이미 도장 `9` 라 묶기 담당이 걷지 않는다 (결과서).
- 번호가 겹치는 까닭 (exe 와 k32 가 둘 다 1, kb 와 dll1 이 둘 다 2) 은 근거 문서가 따로 풀지 않았다.
  - 이 글의 짐작 `[추측]`: kernel32 · kernelbase 를 먼저 준비할 때 (flow.md 2.5) 묶기를 한 번, exe 쪽에서 또 한 번, **따로** 돌린 것으로 보인다. 그러면 k32 1 · kb 2, 그리고 exe 1 · dll1 2 · dll2 3 이 된다.
  - 이유: run24 해설 2.9 에 "도장이 5 보다 큰 자식은 건너뛴다" 가 있다. exe 쪽 묶기 때 k32 · kb 는 이미 지나간 카드라 다시 번호를 안 받았을 것이다. 번호를 처음부터 다시 세는지는 확인 안 됐다.

#### PE_emulator 에게

- `PreorderNumber` 를 "카드 걷기에서 번호" 로 만들었으면 **틀림** — 묶기 함수 몫이다 (`LowestLink` 와 같은 함수 · 같은 값).

### 2.6 `끝까지 0` ① — 0 을 씀 15

사진 8장 내내 0 인 칸 중, **누군가 0 을 일부러 적는 줄** 이 있는 것들이다.

| 칸 | 줄 `[정적]` | 어떤 0 인가 |
|---|---|---|
| 안내판 `LeapSecondFlags` (`+0x7c0` DWORD) · `.SixtySecondEnabled` · `.Reserved` (3) | nt `MmCreatePeb +0x57a AND dword [RBX+0x7c0],0` | **명시 0**. `+0x573` 에서 `LeapSecondData` 를 쓴 바로 뒤, 무조건 |
| 명부 줄 `Lock` (`+0x90`) (1) | `LdrpAllocateModuleEntry +0xe9 MOV qword [RBX+0x90],0` | 명시 0 |
| 명부 줄 `Flags` 비트 10개 (10) | `LdrpInitializeProcess +0x13be ~ +0x13c4` | **DWORD 되쓰기** (아래 셈 결정) |
| 카드 `CondenseLink` (`+0x40`) (1) | `LdrpCondenseGraphRecurse +0x2a` (올릴 때 이전 머리) → `+0x6a` · `+0x9a` (뽑을 때 0) | 뽑을 때 명시 0 |

- `MmCreatePeb` 의 `memset` 은 지역 견본용 (`+0x78`) 하나뿐이다. 안내판의 나머지 0 은 새 0 페이지 (`MiCreatePebOrTeb`) 에서 온다 (짐 지도 3.1). 그래서 `+0x57a` 는 "지우개" 가 아니라 **펜** 으로 센다.
- ntdll 이 `SixtySecondEnabled` 를 **켜는** 줄도 있다: `LdrpInitializeExecutionOptions +0x725 OR [RBX+0x7c0],1`.
  - 조건: `+0x71c TEST byte [RBX+0x7c4],1` — `NtGlobalFlag2` 비트 0 이 켜졌을 때만.
  - test.exe 는 `NtGlobalFlag2` 가 0 이라 안 탄다.
  - 이 줄은 IFEO 건너뛰기 (`+0x604`) **뒤** 라 IFEO 키와 상관없다 (flow-writer `.asm`).
  - 그래서 이 비트는 nt (0) · ntdll (조건부 켬) **양쪽** 에 센다 (2.11).

#### `CondenseLink` 는 왜 0 으로 끝나나 — 단계별로

1. 묶기 담당이 카드를 더미에 올릴 때 `+0x2a` 가 `CondenseLink` 에 **이전 머리 포인터** 를 넣는다. → 잠깐 0 이 아니다.
2. 다 보고 나서 "내 번호 == 가장 낮은 번호" 면 더미에서 뽑는다 (`+0x4d CMP` · `+0x50 JNZ` 안 감).
3. 뽑을 때 `+0x6a` (`RAX = 0`, `+0x63 XOR`) · `+0x9a MOV qword [R15],0` 이 0 을 적는다.
4. test.exe 그래프는 고리가 없다. 그래서 카드마다 바로 뽑혀 0 으로 끝난다.

- 사진 8장이 모두 0 인 것과 맞다.

#### 셈 결정 — `Flags` 비트 10개는 왜 "0 을 씀" 인가

1. `[정적]` 명부 줄 `Flags` 의 비트 10개 (`TelemetryEntryProcessed` · `LoadInProgress` · `EntryProcessed` · `AuxIatCopyPrivate` · `ReservedFlags3` · `DontRelocate` · `ChpeImage` · `ReservedFlags5` · `ReservedFlags6` · `CompatDatabaseProcessed`) 는 **켜는 줄이 ntdll 어디에도 없다** (`or/and/bts/btr/mov` 꼴 전수 + 바이트 꼴 `[+0x69]` · `[+0x6a]`).
2. `[정적]` 그런데 exe 명부 줄을 만들 때 `+0x13be MOV EAX,[RCX+0x68]` → `+0x13c1 OR EAX,4` → `+0x13c4 MOV [RCX+0x68],EAX` 가 그 4바이트를 **통째로** 다시 쓴다 (flow-writer `.asm`).
3. 통째로 다시 쓰면 안 바꾼 비트에도 "그 자리에 0 을 적은" 셈이다 → [run52 해설 2.7](run52-kthread-create-해설.md#27-핵심-2-dword-하나를-되쓰면-왜-다른-비트에-0-을-쓴-것이-되나).
4. 그래서 run52 · run53 · run112 기준 그대로 **"0 을 씀"** 으로 셌다 (오케스트레이터 결정, run112 `CrossThreadFlags` 와 같은 규칙).

- 이 되쓰기는 **exe 명부 줄만** 이다. 다른 명부 줄은 0 으로 채운 종이 그대로다 — 켜는 줄이 없으니 값은 같다.
- 이 규칙을 안 치면: 0 을 씀 **5** · 아무도 안 씀 **38** 이 된다 (결과서 둘째 숫자를 run 문서 셈으로 옮긴 것).

### 2.7 `끝까지 0` ② — 조건부 안 탐 20: test.exe 같은 가게엔 해당 없음

켜는 줄은 길 안에 있는데, test.exe 가 조건을 안 채워서 안 탄 칸들이다. run 문서 표에서 묶어 옮겼다 `[정적]`.

| 칸 | 켜는 곳 | test.exe 가 안 타는 까닭 |
|---|---|---|
| 안내판 `BeingDebugged` | nt `DbgkpMarkProcessPeb +0x84` (2.2 줄 ③) | 부모 cmd 가 디버거 아래가 아니다. run44 `BeingDebugged 0` `[관찰]` |
| 안내판 `CrossProcessFlags.ProcessInJob` (비트 0) | nt `PspApplyWorkingSetLimitsToProcess +0x19b LOCK BTS` ← `PspInsertProcess +0xd4 PspImplicitAssignProcessToJob` | 부모가 잡 안이 아니다. 사진 `ProcessInJob 0y0` |
| 안내판 `AppCompatInfo` (`+0x2e0`) | `LdrpInitializeProcess +0x19e5` (0 을 씀) | shim 길 입구 (`+0x19d7` · `+0x19dc`) 를 통과해야 한다. shim 안 켜짐 (`g_ShimsEnabled` 0, run66). 탔어도 0 |
| 안내판 `TracingFlags` + 비트 4칸 (5) | `LdrpInitializeExecutionOptions +0x2b6` (IFEO 값) | IFEO 부분을 `+0xad JNZ` / `+0xd1 JS` 로 통째 건너뛴다. 범위 밖에서도 씀: nt `EtwpUpdateProcessTracingCallback` (ETW 세션) |
| 안내판 `WaitOnAddressHashTable` (1024B) | `RtlpWakeByAddress` 의 `LOCK CMPXCHG` 슬롯 | 어떤 스레드가 실제로 주소에서 **기다려야** 쓰인다 |
| 명부 줄 `Flags` 비트 7개 | 아래 목록 | 아래 목록 |
| 명부 줄 `SigningLevel` (`+0x11c`) | `LdrpSetModuleSigningLevel` → `NtGetCachedSigningLevel` (커널이 씀) · 실패 갈래 `LdrpValidateIntegrityContinuity` | 입구 `LdrpMapDllNtFileName +0x141` 이 보는 전역 `LdrpAuditIntegrityContinuity` 가 0 (사진) |
| 명부 줄 `ActivePatchImageBase` (`+0x128`) | `LdrpInitializeNtdllDataTableEntry +0x264` · `LdrpApplyPatchImage +0x15d` | 핫패치 ntdll 아님 (`HotPatchState` 0) |
| 카드 `ServiceTagList` (`+0x10`) | `LdrpAddNodeServiceTag +0x67` ← `LdrpPrepareModuleForExecution +0x6a` | TEB `SubProcessTag` 0 (run54) |
| 카드 `LoadWhileUnloadingCount` (`+0x1c`) | `LdrpIncrementModuleLoadCount +0x201 INC` | 내려가는 카드를 다시 늘리는 일 없음 |

- **`WaitOnAddressHashTable`** = 어떤 메모리 값이 바뀌기를 기다리는 스레드들의 **자리표** 로 보인다 `[추측]` (이름과 "주소 대기 슬롯" 이라는 결과서 설명에서). 지도 안 `RtlpWaitOnAddress` 는 경합 길에서만 불린다.

명부 줄 `Flags` 비트 7개:

- `PackagedBinary` (비트 0) — 패키지 앱 아님 (PEB `BitField` 비트 4 `IsPackagedProcess` 0).
- `MarkedForRemoval` (비트 1) — 언로드 때만 (`LdrpUnloadNode +0x211`).
- `ShimDll` (비트 8) — shim 안 켜짐.
- `VerifierProvider` (비트 10) — App Verifier 안 켜짐 (`AVrfpLoadAndInitializeProvider +0x186`).
- `ProcessAttachFailed` (비트 20) — DLL 시작 코드가 실패할 때만 (`LdrpInitializeNode +0x29e`). 다 성공.
- `ChpeEmulatorImage` (비트 26) — 작업 지시서 `+0x20` 비트 27 일 때 (`LdrpAllocateModuleEntry +0x129`) `[추측: 비트 뜻]`.
- `Redirected` (비트 28) — 작업 지시서 `+0x20` 비트 2 일 때 (`LdrpAllocateModuleEntry +0x10d`) `[추측: 비트 뜻]`.

#### `BeingDebugged` 와 우리 디버거 — 단계별로

이 칸은 "디버거가 붙어서 달라지는 점" 과 바로 이어져서 따로 본다.

1. `[정적]` 이 칸은 새 가게의 **디버그 포트** 가 0 이 아닐 때만 1 이 된다.
2. `[정적]` 그 디버그 포트는 부모의 것을 복사해서 생긴다 (`DbgkCopyProcessDebugPort +0x3c`).
3. 결과서: 우리가 쓰는 **커널 디버거 (KDNET)** 는 디버그 포트를 안 쓴다.
4. 그래서 커널 디버거가 붙어 있어도 이 칸은 0 이다 — run44 사진 `BeingDebugged 0` `[관찰]` 과 맞다.

- → **디버거가 붙어서 달라지는 점: 없음.**
- 유저 모드 디버거 아래에서 띄우면 1 이 될 것이다 `[추측]` (run 문서).

### 2.8 `끝까지 0` ③ — 아무도 안 씀 28

ntdll · nt · 본 DLL 들 어디에도 **쓰는 줄이 없는** 칸이다 `[정적]`.

- 안내판 26칸: `Padding0 ~ Padding6` (정렬 빈칸 7) · 예약 6 (`ReservedBits0` · `SystemReserved` · `SparePointers` · `SpareUlongs` · `UnusedNlsField` · `PlaceholderCompatibilityModeReserved`) · WOW64 · ARM64 전용 3 · 옛 칸 2 · `TlsExpansionCounter` · `IFEOKey` · `ReadImageFileExecOptions` · NLS 표 셋 · `AppModelFeatureState` · `pImageHeaderHash` (목록 전체는 run 문서 2.3).
- `_PEB_LDR_DATA` 2칸: `SsHandle` · `EntryInProgress` — 전역 `PebLdr` 의 `+0x8` · `+0x40` 을 가리키는 명령이 **0건**, `[PEB+0x18]` 로 따라가도 없다.

#### 요청서 짐작과 다른 점 둘

- **`IFEOKey` · `ReadImageFileExecOptions`** — 요청서는 "IFEO 키가 없어서 안 탐" 으로 봤다.
  - 실제로는 **쓰는 줄 자체가 없다** `[정적]`.
  - `LdrpInitializeExecutionOptions` 는 키 핸들을 지역 칸 `[RSP+0x68]` (`+0xb8 LEA R9`) 에만 두고 끝에 닫는다.
  - 비유: 이름은 "특별 지시 서류함 열쇠" 칸인데, 24H2 로더는 열쇠를 **자기 주머니** 에만 넣고 안내판엔 안 건다.
  - run46 "호출자에게 돌려줄 뿐" 을 더 강하게 확인한 것이다.
- **`AnsiCodePageData` · `OemCodePageData` · `UnicodeCaseTableData`** — 24H2 커널은 이 셋을 안 채운다.
  - `MmCreatePeb` 은 코드 페이지 **번호** 셋 (`ActiveCodePage` · `OemCodePage` · `UseCaseMapping`) 만 쓴다.
  - 사진 P2 도 null `[관찰]`.

#### 까닭을 모르는 둘 `[공백]`

- `AppModelFeatureState` · `pImageHeaderHash` 는 이름으로 보면 쓰는 쪽이 있을 법한데 못 찾았다.
- 본 범위: ntdll · nt · user32 · gdi32 · kernel32 · KernelBase 훑기. 커널 쪽 훑기에는 한계가 있다 (아래 2.9 끝) → Q202 ①.

### 2.9 `끝까지 0` ④ — 범위 밖 길 22: 다른 사람이 다른 때 쓰는 칸

쓰는 줄은 있는데, 그 함수가 **지도 밖** 에 있는 칸이다. run54 `_TEB` 처럼 셋으로 나눴다.

#### ntdll 4 (지도 밖 함수, 이번에 `.asm` 뽑음) `[정적]`

- `ProcessUsingVEH` · `ProcessUsingVCH` — `RtlpAddVectoredHandler +0x1af` 가 켜고 `RtlpRemoveVectoredHandler +0x1b8` · `RtlpCallVectoredHandlers +0x262` 가 끈다 (run82 그대로).
  - run82 는 "초기화 동안 아무도 안 씀" 이라고 했는데, 이번 가름은 "범위 밖 (ntdll)" 이다. 쓰는 함수가 지도 밖에 있다는 뜻이라 **뜻은 같다**.
- `WerShipAssertPtr` — `SetAssertBufferPtrinPeb +0x52`.
- `PlaceholderCompatibilityMode` — `RtlSetProcessPlaceholderCompatibilityMode +0x1d` (같은 이름 nt 함수 `+0x2c` 도).

#### nt 6

- `ProcessPreviouslyThrottled` · `ProcessCurrentlyThrottled` — `PspTrySetProcessPebThrottlingFlags +0x5f` · `+0x71` `[정적: objdump]`.
- `TelemetryCoverageHeader` — `EtwpCoverageEnsureUserModeView +0x140` ← `EtwSetProcessTelemetryCoverage +0x4f` `[정적: objdump]`.
- **쓰는 줄 못 찾음 셋** `[공백]`:
  - `ProcessImagesHotPatched` (핫패치 길 짐작 `[추측]`)
  - `CloudFileFlags` · `CloudFileDiagFlags` (클라우드 파일 필터 cldflt 짐작 `[추측]`)

#### 다른 DLL · 모듈 12 — "그림 그리는 가게" 칸들

- user32 `_UserClientDllInitialize`: `KernelCallbackTable` · `UserSharedInfoPtr` (`+0x58` 한 칸에 겹침) · `PostProcessInitRoutine` (`+0x409`, 0 을 씀) · `Win32ClientProcInfo`.
- user32 `InitProcessGdiScaling +0xc8`: `AppCompatFlagsUser`.
- win32kbase `GdiProcessCallout` — run86 의 **GDI 진열장** 을 거는 그 함수:
  - `GdiSharedHandleTable` (`+0x22b`, run86)
  - **`GdiDCAttributeList` (`+0xe9`)** — 이번에 처음 `[정적: objdump]`
  - **`GdiHandleBuffer` (`+0xef ~ +0xfe`, 명시 `memset(…, 0, 0xf0)`)** — 이번에 처음 `[정적: objdump]`
  - run86 은 이 둘을 "이번 줄기에 안 나옴" 이라고 했다. 이번에 같은 함수 안에서 찾았다.
- gdi32 `hGetPEBHandle +0x57` · `+0x159` — `GdiHandleBuffer` 를 핸들 캐시로 쓴다.
- 쓰는 DLL 짐작 넷 `[추측]`: `AppCompatFlags` (shim · apphelp — ntdll 은 `LdrpInitializeProcess +0x19fd` 에서 읽기만) · `AtlThunkSListPtr` (atlthunk) · `ProcessUsingFTH` (fth) · `WerRegistrationData` (wer).

- 비유: 이 칸들은 **그림 그리는 가게** 가 GDI 부서에 등록할 때 관리사무소의 그래픽 부서가 적는 칸이다. test.exe 처럼 창을 안 만드는 가게엔 해당 없다.

#### `PostProcessInitRoutine` — 쓰는 쪽과 부르는 쪽

- 쓰는 쪽: user32 `_UserClientDllInitialize +0x409` 가 **0 을 쓴다** (run85).
- 부르는 쪽: ntdll `LdrpInitializeProcess +0x1d16 CALL [RDI+0x230]` (CFG 간접 호출, null 이면 건너뜀).
- 사진 8장 0 `[관찰]` → test.exe 에서는 이 간접 호출을 안 탄다.
- **0 이 아닌 값을 넣는 쪽은 그대로 모른다.**

#### 커널 쪽 "못 찾음" 은 왜 `[공백]` 인가

- 커널 훑기는 `[EPROCESS+0x2e0]` 로 읽은 PEB 주소를 따라가는 **직선 훑기** 였다 (갈림길에서 합치는 처리 없음).
- 첫 판은 버그 하나 (`test r,r` 처리) 때문에 `PspApplyWorkingSetLimitsToProcess` (`ProcessInJob`) 를 놓쳤다가, 고쳐서 다시 돌렸다.
- 그래서 "못 찾음" 은 "없다" 가 아니라 **`[공백]`** 이다. 분기 합침이 있는 추적으로 다시 보는 게 Q202 ①.

### 2.10 간접 호출 — 칸을 쓰는 건 0개

- 줄이 든 함수 20여 개의 `indirect:` 를 다 봤다 `[정적]`:
  - shim 콜아웃 둘 (`LdrpSendPostSnapNotifications +0x122` · `LdrpMapDllSearchPath +0x3b4`) · `LdrpInitShimEngine +0x160`
  - `LdrpInitializeProcess` 넷 (`+0x17fc` WOW64 · `+0x1b59` `Kernel32ThreadInitThunkFunction` · `+0x1cdf` · `+0x1d16` `PostProcessInitRoutine`)
  - `_LdrpInitialize +0x9a` (WOW64)
- 모두 이미 알려졌거나 칸을 안 쓴다.
- 칸을 바꿀 **수 있는** 건 shim 콜아웃 (`g_pfnSE_*`) 뿐이고, `g_ShimsEnabled` 0 이라 안 탄다. → Q35 에 새로 넣을 것 없음 (메모 한 줄).

### 2.11 결과서와 다르게 센 곳 — run 문서 숫자를 따른 까닭

#### 93칸 → 92칸

- 결과서는 93칸을 셌다. 그중 `NtGlobalFlag2` 는 **run111 이 이미 짐 지도 3.1 로 옮긴 아는 칸** 이다 (요청서가 7.2 옛 목록을 옮기며 같이 넣음).
- 결과서는 이 칸을 "0 을 씀 (nt `MmCreatePeb +0x4b2`)" 으로 셌지만, 짐 지도 줄과 같은 근거라 새로 늘 칸이 없다.
- 그래서 run 문서는 이 칸을 뺐다 → **"0 을 씀" 16 → 15**. 다른 숫자는 같다.

#### 새 말: 양쪽 셈

- **양쪽 셈** = 한 칸을 nt 와 ntdll 이 **둘 다** 쓰면, 짐 지도 2장 "nt 가 씀" · "ntdll 이 씀" 열 **양쪽에** 센다 (짐 지도 1장 규칙, run70 `MinimumStackCommit` · run111 `NtGlobalFlag2` 와 같은 꼴).
  - 비유: 안내판 한 칸에 관리사무소도 펜을 대고, 가게 주인도 (조건이 맞으면) 펜을 댄다. 장부엔 두 사람 이름을 다 적는다.
  - 그래서 2장 표에서 열을 더하면 칸 수보다 클 수 있다.
- 이번 둘:
  - `LeapSecondFlags.SixtySecondEnabled` — nt `+0x57a` 0 · ntdll `+0x725` 조건부 켬. 결과서는 "0 쪽으로" 만 셌다.
  - 명부 줄 `SigningLevel` — nt (`NtGetCachedSigningLevel` 출력) · ntdll (`LdrpValidateIntegrityContinuity` 실패 갈래).
- 네 가지 가름의 숫자 (0 을 씀 / 조건부 안 탐) 는 그대로다. 바뀐 건 2장 "누가 씀" 열뿐이다.

#### 그 밖에

- **경로 줄임**: 결과서 `①I…` 는 짐 지도에 이미 같은 뜻 (run50 `①… → PspInsertProcess`) 이 있어 그대로 썼다. `②C…` 는 새로 뒀다 (2.5).
- **`ShutdownInProgress`**: 결과서는 "3.4 에 이미 있음" 이라 했지만 짐 지도 3.4 엔 `LdrShutdownProcess +0xae` 만 있었다. 프로세스 반사 길 (범위 밖) 이라 셈은 그대로, 근거에 이름 `RtlpProcessReflectionStartup +0x280` 만 보탰다.
- **`[정적: objdump]` 표시**: 커널 `DbgkpMarkProcessPeb` · `PspApplyWorkingSetLimitsToProcess` · `PspTrySetProcessPebThrottlingFlags` · `Etwp*` 는 `.asm` 이 없어 결과서 `objdump` 로만 있다. 결과서의 "64곳 `.asm` 대조" 는 ntdll 과 있던 커널 `.asm` 몫이다.

### 2.12 정정 — run112 트랩 프레임, 그리고 요청서 짐작

#### run112: "P1 에서도 0" 은 "0 을 써서 0"

run112 문서의 트랩 프레임 표와 그 아래 문장이 어긋나 보였다. 둘 다 맞았다.

1. 표: `Rax` · `R8 ~ R11` 은 **모든 창구** (그중 `KiBreakpointTrap +0x18 · +0x24 ~ +0x30`) 가 적는 칸, "P1a 부터 유저 값".
2. 문장: "이 24칸은 P1 에서도 0".
3. `[정적]` `KiBreakpointTrap` 은 P1 에서도 이 다섯 칸을 **적는다** (`.asm`). 적은 값이 첫 유저 레지스터라 0 이다.
4. `[관찰]` 같은 입구가 같은 때 적은 이웃 칸 — `Rcx` (`+0x1c`, 맡긴 문맥 주소 `0x9b2fcffa40`) · `Rdx` (`+0x20`, ntdll 기준 주소 `0x7ffafb660000`) · `ExceptionActive` (`+0x14`, 1) — 는 P1 사진에서 0 이 **아니다** (`ms/logs/04_baggage-read_P1.txt:1061 ~ 1069`).
5. → 다섯 칸의 "P1 에서도 0" 은 "안 써서 0" 이 아니라 **"0 을 써서 0"**. 표의 "P1a 부터 유저 값" 은 "0 이 아닌 첫 값" 이라는 뜻이다.

- run112 문서에 "(run113 보탬)" 한 줄이 붙었다. 짐 지도 셈 (덮임 25) 은 그대로다.
- run112 해설 2.12 는 이걸 `[추측: 이 해설의 읽기]` 로 적었었다. 이번에 근거가 생겨서 그 해설 맨 위에 "고쳐 읽기 (run113)" 한 줄을 달았다.
- 사진 읽기 요령 (run 문서 "디버거 메모"): **`int3` 로 멈춘 사진의 트랩 프레임에서 0 은 두 가지다** — 입구가 0 을 적었거나, 아예 안 적었거나. 같은 입구가 적은 **이웃 칸** 을 같이 보면 가린다.
  - 비유: 출발 메모의 빈칸이 "적을 게 없어서 0 이라고 쓴 칸" 인지 "펜이 안 닿은 칸" 인지는, 같은 사람이 같은 때 쓴 옆 칸에 글씨가 있는지로 안다.

#### 요청서에 있던 이름 둘 — 이 빌드엔 없다

- `LdrpCreateNodeForModule` · `LdrpBuildDependencyGraph` 는 `_index.tsv` · `pdb_aliases.py --where` 둘 다 0개 (결과서).
- 카드는 `LdrpAllocateModuleEntry` 가 명부 줄과 같이 만든다 (2.4). 요청서는 중간 산출물이라 안 고친다.

#### 요청서 짐작 둘 — 틀렸다

- `PreorderNumber` 는 카드 걷기가 아니라 묶기 (2.5).
- `IFEOKey` · `ReadImageFileExecOptions` 는 "조건부 안 탐" 이 아니라 쓰는 줄 없음 (2.8).

### 2.13 PE_emulator 에게 (run 문서 "PE_emulator 쪽", 짧게)

`../PE_emulator` 코드는 결과서도 flow-writer 도 안 읽었다. 고칠지는 사용자 결정이다.

- `CrossProcessFlags`: **확인 필요**. 맞추려면 `_LdrpInitialize` 흉내가 비트 1 을 켜고 끝에 끈다 (사진 `2 → 0`).
- `LoadTime`: KUSD `SystemTime` 을 읽어 쓰면 맞다. 상수면 사진과 다르다 (시간이라 어차피 다름).
- `PreorderNumber`: "카드 걷기에서 번호" 로 만들었으면 **틀림** (2.5).
- `SwitchBackContext` · `PatchLoaderData`: **확인 필요**. 0 으로 두면 사진과 다르지만, 읽는 쪽이 있을 때만 영향.
- `_PEB_LDR_DATA` `SsHandle` · `EntryInProgress`: 0 으로 두면 맞음 (확인 필요).
- 명부 줄 · 카드 할당: **`entry + 0x138` 로 카드를 읽는 코드는 만들지 않는다** (늘 `entry + 0x98`).

### 2.14 정리 — 비유로 한 번 더

- 안내판 칸 62개를 다 봤다. 대부분은 "**우리 가게엔 해당 없음**" 이었다.
  - 디버거 손님 (`BeingDebugged`), 체인점 (`ProcessInJob`), 호환성 고치기 (`AppCompatInfo` · `ShimDll`), 특별 지시 (`TracingFlags`), 그림 그리는 가게 (GDI 칸) 일 때만 적는다.
  - 몇 칸은 이름만 남은 **빈칸** 이다 (`Padding*` · 예약 · 옛 칸 · `IFEOKey`).
- 명부 줄과 연결 카드는 **다른 서랍에서 꺼낸 새 종이 두 장** 이다. 둘을 쪽지로 서로 묶고 (`DdagNode` · `NodeModuleLink`), 입사 시각을 공용 시계에서 베낀다 (`LoadTime`).
- 카드 방문 순번은 **묶기 담당** 이 동그라미를 찾으며 붙인다 (`PreorderNumber`). 고리가 없으면 더미에 올렸다 바로 내려서 `CondenseLink` 는 0 으로 끝난다.
- 남은 건 이름 몇 개 (`[공백]` 5칸 · 쓰는 DLL 이름 `[추측]` 넷) 와, 이미 사진이 간접으로 말해 주는 실행 값 넷이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 | — | ntdll pdb 줄에 run113, 근거 로그 줄 (run44 로그를 칸마다 다시 읽은 값) |
| 2.1 `MmCreatePeb` 표 | `+0x7c0` 줄 없음 | `+0x7c0` `LeapSecondFlags` **0 을 쓴다** `[정적]` |
| 2.1 `PspInsertProcess` | 잡 · 디버그 포트 갈래만 | 두 갈래가 안내판 칸 둘 (`ProcessInJob` · `BeingDebugged`) 을 켬, test.exe 는 둘 다 안 탐 `[정적]` |
| 2.4 `_LdrpInitialize` | `ProcessInitializing` 비트 줄 | `CrossProcessFlags` 정수 칸의 처음 값도 이 줄 `[정적]` + 사진 `[관찰]` |
| 2.4 `LdrpInitializeProcess` | 명부 줄 + 카드 "한 쌍" | 따로 두 번 · exe `Flags` DWORD 되쓰기 · `PatchLoaderData` · `AppCompatInfo` · `PostProcessInitRoutine` 쓰는 쪽 (user32) `[정적]` |
| 2.4 `LdrpInitializeNtdllDataTableEntry` | — | `LoadTime` · `ActivePatchImageBase` (핫패치 갈래) `[정적]` |
| 2.5 올린 뒤 알림 절 | `SbUpdateSwitchContextBasedOnDll` 이 무엇을 쓰는지 열림 | `SwitchBackContext` (`+0xd9`) `[정적]` |
| 2.6 `LdrpAllocateModuleEntry` | 힙 플래그 `\| 8` = "0 으로 채움" `[추측]` (run22) | **`HEAP_ZERO_MEMORY` `[정적]`** + `NodeModuleLink` · `Lock` · 비트 26 · 28 |
| 2.7 `LdrpCondenseGraph` 절 | 번호 매기기 (run24) | 칸 셋의 오프셋 (`+0x1e` · `+0x24` · `+0x2a` / `+0x6a` / `+0x9a`) + 사진 |
| 4장 | run66 정적 ③ 열림, 2.6 이름 못 정한 것 줄의 `\| 8` `[추측]` | run66 ③ `(해결됨)`, `\| 8` `(일부, 정적) → run113`, 새 줄 넷 (해결됨 · 동적 · 정적 · PE_emulator) |
| 5장 | — | run113 두 표 |

- 한 줄로: **2.6 의 오래된 `[추측]` 하나 (`| 8`) 가 `[정적]` 이 됐고, 2.1 · 2.4 · 2.5 · 2.7 의 이미 있던 상자 안에 쓰는 줄이 붙었다.**

### 짐 지도 `baggage.md` 2장 — 숫자가 크게 바뀌었다

#### 구조체 21개, 직접 칸 1,456

| 가름 | 바퀴 전 | 바퀴 뒤 | 무엇 때문에 |
|---|---|---|---|
| 아는 칸 | 816 (정확 785 · 짐작 31) | **858** (정확 827 · 짐작 31) | 이번 42 (모두 정확) |
| 아무도 안 씀 | 157 | **185** | 이번 28 |
| 범위 밖 길 `[추측]` | 240 | **262** | 이번 22 |
| 트랩 프레임 덮임 | 25 | 25 | — |
| 모름 | **218** | **126** | 이번 92 |

- 합은 그대로 1,456 이다.
- 남은 모름 126 = 유저 **KUSD 102** + 커널 24 (`_EPROCESS` 1 · `_MMVAD` 9 · `_MMVAD_SHORT` 14).
- 아는 칸 중 끝까지 0 (7.1): 327 → **362** (0 을 씀 15 + 조건부 안 탐 20).
- "구간이 정해진 모르는 칸" (`K1 이전` 이 아닌 곳): 10 → **3** (유저 0 · 커널 0 · 전역 3).

#### 유저 구조체 넷 (2장 표 줄)

| 구조체 | 칸 | nt 가 씀 | ntdll 이 씀 | 모름 |
|---|---|---|---|---|
| `_PEB` | 124 | 42 → **47** | 25 → **35** | 62 → **0** |
| `_PEB_LDR_DATA` | 9 | 0 | 7 | 2 → **0** |
| `_LDR_DATA_TABLE_ENTRY` | 65 | 1 → **2** | 41 → **65** | 24 → **0** |
| `_LDR_DDAG_NODE` | 10 | 0 | 6 → **10** | 4 → **0** |
| **유저 7개 합** | 488 | 118 → **124** | 136 → **174** | 194 → **102** |

- `_PEB` nt +5 = `BeingDebugged` · `ProcessInJob` (조건부) + `LeapSecondFlags` 셋 (0 을 씀).
- `_PEB` ntdll +10 = `CrossProcessFlags` · `PatchLoaderData` (구간 줄) + 조건부 일곱 (`AppCompatInfo` · `TracingFlags` 5 · `WaitOnAddressHashTable`) + `SixtySecondEnabled` (양쪽 셈).
- 명부 줄 nt +1 = `SigningLevel` (양쪽 셈).
- 안내판 3.1 머리 줄: 아는 칸 62 → **76** (정확 50 → 64) · 아무도 안 씀 **26** · 범위 밖 **22**.
- 3장 줄 수: 3.1 42 → **55**, 3.4 12 → **13**, 3.5 52 → **68**, 3.6 18 → **22** (새 줄 34 · 고친 줄 3).
- 1장 그림의 "모듈마다 한 쌍 (같이 만듦)" → "모듈마다 둘 (따로 두 번 할당)" 로 고쳤다.

### 큰 그림에서

- **유저 쪽 구조체 일곱 중 여섯이 "모름 0" 이 됐다.** 남은 건 KUSD 하나 (102칸) 다.
- 에뮬레이터 쪽에서 보면: 안내판 · 명부 줄 · 카드를 만들 때 **어느 칸을 누가 어느 줄에서** 적는지, 그리고 test.exe 에선 **안 적어도 되는 칸** 이 무엇인지 칸 단위로 있다.
- 다음 문서 차례는 **Q78e2** (run114, sast/111 — ntdll 전역 쪽) 다. 이번 결과서가 그쪽에 얹을 전역 넷을 넘겼다 (4절).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 모듈마다 `LdrpSearchPath` 출력 바이트가 0 인가 (`PackagedBinary` 안 켜짐의 직접 근거) | 실행 때 스택 값. 사진 6모듈 비트 0 은 간접 증거 | 동적 **Q46** (낮음): `bp ntdll!LdrpMapDllSearchPath+0x131` → `db @rsp+0x50 L1` |
| shim 길 입구 `[R13+0x210]` 가 정말 0 인가 | 지역 값이라 사진으로 못 읽음 (`g_ShimsEnabled` 0 이 간접 증거) | 동적 **Q46** (낮음): `LdrpInitializeProcess +0x19dc` 에서 `dd @r13+0x210 L1` |
| `RtlpWakeByAddress` 가 `NtContinue` 전에 한 번이라도 불리나 | 호출 여부는 사진에 안 남음 | 동적 **Q46** (낮음): `bp ntdll!RtlpWakeByAddress` |
| `EtwpUpdateProcessTracingCallback` 이 은퇴 전에 서나 | 호출 여부 (사진 `TracingFlags 0` ×8 이 간접 증거) | 동적 **Q46** (낮음): `bp nt!EtwpUpdateProcessTracingCallback` |
| nt 쪽 쓰는 줄 못 짚은 5칸 `[공백]` (`ProcessImagesHotPatched` · `CloudFileFlags` · `CloudFileDiagFlags` · `AppModelFeatureState` · `pImageHeaderHash`) | 커널 훑기가 직선 (분기 합침 없음). cldflt.sys 는 안 봄 | 정적 **Q202 ①** |
| `AppCompatFlags` 를 쓰는 DLL, `AtlThunkSListPtr` · `ProcessUsingFTH` · `WerRegistrationData` 를 쓰는 DLL 이름 (지금 `[추측]`) | 그 DLL 들을 안 봄 | 정적 **Q202 ②** |
| 작업 지시서 `+0x20` 비트 2 · 27 의 뜻 (`Redirected` · `ChpeEmulatorImage` 를 켜는 갈래) | PDB 에 작업 지시서 칸 정보가 없다 (run22) | 정적 **Q202 ③** (낮음) |
| `SbpDetermineDllContext` 와 호환 문맥 표 (rva `0x173010`, `0x20`B 칸) — `SwitchBackContext` 값을 정하는 본문 | 이번엔 쓰는 줄만 봄 | 정적 **Q202 ④** (낮음) |
| Q78e2 에 얹을 전역: `LdrpAuditIntegrityContinuity` · `LdrpEnforceIntegrityContinuity` (읽는 줄 `LdrpMapDllNtFileName +0x141`), `LdrpModuleDatatableLock` (`LdrpFindExistingModule +0x21` · `+0x35` · `+0x6f`), `g_pShimEngineModule` (조건부 안 탐) | 전역은 Q78e2 몫 | 다음 바퀴 **run114** (Q78e2, sast/111) |
| `PostProcessInitRoutine` 에 0 이 아닌 값을 넣는 쪽 | user32 가 0 을 넣는 줄만 앎 | 대기열 번호 없음 (run 문서 "이전 run 에서 닫은 것" 에 "그대로 모름") |
| `PreorderNumber` 사진 값이 겹치는 까닭 (2.5 의 이 글 짐작) | 근거 문서가 안 다룸 | 대기열 번호 없음 — 이 글의 `[추측]` |

- D5 (`LdrpHeap` = 프로세스 힙?) 는 **사진으로 닫혀** 대기열에 안 갔다 (2.3).
- 결과서 판단은 "dast 요청 필요 없음 (전부 낮음)" 이다. 그래서 동적 넷은 새 번호 없이 Q46 에 덧붙었다.

---

## 5. 더 읽기

- run 문서: [`run113-ntdll-user-structs.md`](run113-ntdll-user-structs.md) — 요약 표, 호출 지점 표, 7칸 표 (1절), 85칸 가르기 (2.1 ~ 2.4), 결과서와 다르게 적은 곳, 정정
- 결과서: [`_workspace/sast/109_ntdll-user-structs_result.md`](../../_workspace/sast/109_ntdll-user-structs_result.md) — 훑기 방법과 한계, 칸마다 쓰는 줄 (질문 1 ~ 3), 간접 호출 (4), 동적 D1 ~ D5, 정정 · 새로 알게 된 것
- 요청서: [`_workspace/sast/109_ntdll-user-structs.md`](../../_workspace/sast/109_ntdll-user-structs.md)
- flow.md: [2.1](flow.md) (`MmCreatePeb` 표 · `PspInsertProcess`) · 2.4 (`_LdrpInitialize` · `LdrpInitializeProcess` · `LdrpInitializeNtdllDataTableEntry`) · 2.5 (올린 뒤 알림) · 2.6 (`LdrpAllocateModuleEntry`) · 2.7 (`LdrpCondenseGraph`) · 4장 run113 줄 · 5장 run113
- 짐 지도: [`baggage.md`](baggage.md) 1장 (그림 "한 쌍" 정정) · 2장 (숫자) · 3장 줄임 `②C…` · **3.1 · 3.4 · 3.5 · 3.6** · 7.1 · 7.2 · 7.5
- 앞 해설: [run22](../02_sprint/run22-map-snap-dep-해설.md) (명부 줄 · 카드 만들기) · [run24](../02_sprint/run24-work-queue-해설.md) (고리 · 묶기) · [run27](../02_sprint/run27-init-graph-해설.md) (순서 담당 · 켜기 담당) · [run66](run66-apphelp-cache-해설.md) (shim · 알림) · [run86](run86-gdi32-init-해설.md) (GDI 진열장) · [run112](run112-thread-later-해설.md) (같은 셈 방식, 트랩 프레임)

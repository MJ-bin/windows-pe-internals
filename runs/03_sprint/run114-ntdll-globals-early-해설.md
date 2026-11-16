# run114 ntdll-globals-early — 해설

> 이 글은 [run114 문서](run114-ntdll-globals-early.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q78e2 + Q78f**, 그리고 **Q80** 을 같이 닫았다 (`_workspace/plan.md`, 셋 다 이번에 **끝**)
  - **Q78e2** = Q78e ("ntdll 앞쪽, 힙 빼고") 의 **전역 몫**. 짝인 유저 구조체 몫 Q78e1 은 지난 바퀴 [run113](run113-ntdll-user-structs-해설.md) 에서 끝났다. 이번으로 **Q78e 가 다 끝났다.**
  - **Q78f** = KERNELBASE · KERNEL32 구간 (`P1a~P1b`) 에 남은 ntdll 전역 셋.
  - **Q80** = 같은 `≤P1a` 전역을 "ntdll 파일에 처음부터 적힌 값 vs 사진 P1a 값" 으로 가르기. 결과서가 그 대조를 같이 해 와서 이번에 닫았다.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다. **짐 지도 바퀴**다.
  - 남은 것이 간 곳 (4절):
    - 동적 → 이미 있던 **Q46** 에 덧붙임 (run114 몫, 급하지 않음)
    - 정적 → 새 **Q203** ① ~ ④
    - 셈 결정 거리 (잠금 칸 · `RtlFailedCriticalDebugAllocations`) → 다음 **Q78g2** 문서 바퀴 (**run116**)
    - 가름 열을 새로 둘지 → **사용자 결정 대기**
- 근거:
  - run 문서 [`run114-ntdll-globals-early.md`](run114-ntdll-globals-early.md) (가장 중요)
  - 요청서 [`_workspace/sast/111_ntdll-globals-early.md`](../../_workspace/sast/111_ntdll-globals-early.md) · 결과서 [`111_ntdll-globals-early_result.md`](../../_workspace/sast/111_ntdll-globals-early_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다.** 글은 기준 · 2.4 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): 2장 숫자 · 3.2 · 3.8 · 3.9 · 7.1 · **7.3** · 7.5 · 7.24 · 7.44 · 새 7.45.
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 `[관찰]` 은 모두 **run44 때 찍어 둔 사진** (`ms/logs/04_baggage-read_{P1a,P1b,P2}.txt`) 을 결과서가 칸마다 다시 읽은 것이다.
  - "파일 값" 은 ntdll 파일의 구역 바이트 (재배치 뒤) 다.
- **결과서와 run 문서가 다르게 센 곳이 있다** (2.14). 이 글은 **run 문서**를 따른다.
  - `RtlpHypervisorSharedUserVa` 의 쓴 주인을 **ntdll** 로 (run59 규칙).
  - `LdrpDebugFlags` 는 `ShowSnaps` 와 **같은 주소의 다른 이름** 이라 짐작 → 정확.

---

### 이미 푼 말 — 링크만

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) = 가게 문 앞 **안내판**
- ntdll 전역 = 관리실 **칠판**, `.mrdata` → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md)
- 프로세스 힙 = 가게 **창고**, 임계 구역 · SRW 락 = **열쇠**, SList (잠금 없는 목록) → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.4 · 2.13
- 창고 짓기 (힙 관리자 → 프로세스 힙), NT 힙 / 세그먼트 힙, IFEO, 무작위 몫 → [run46 해설](run46-process-heap-해설.md)
- 매개변수 블록 = 부모가 써 준 **개업 서류**, `LdrSystemDllInitBlock` = **업무 수첩** → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- 사진 지점 K1 ~ P2, 사진 구간 (`≤P1a` · `P1a~P1b` · `끝까지 0` · `K1 이전`) → [run44 해설](run44-baggage-snap-해설.md) 2.8 · 2.12
- 네 가지 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길) → [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법)
- "syscall 결과를 지역 버퍼로 받아 옮기는 칠판 칸은 ntdll 이 쓴 것" (run59 규칙) → [run59 해설](run59-init-leftovers-해설.md) (맨 앞 "세 줄" 의 (3))
- 짐작 크기 안에 **이름 없는 별개 칸** 이 숨어 있는 꼴 (`LdrpThunkSignature` 뒤 `0x1ce268`) → [run60 해설 2.11](run60-func-table-mrdata-해설.md)
- 기능 상태 검사 (첫 검사가 설정을 받아 옴) → [run62 해설 2.9](run62-tp-etw-init-해설.md)
- shim (호환성 고치기 장치 `[추측]`) → [run66 해설](run66-apphelp-cache-해설.md)
- 7.3 목록을 바퀴마다 줄여 온 짝: [run57](run57-nls-apiset-해설.md) · [run59](run59-init-leftovers-해설.md) · [run61](run61-random-ex-init-해설.md) · [run62](run62-tp-etw-init-해설.md) · [run65](run65-ldr-init-bodies-해설.md) (같은 방법 — 링크만)

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **셈 기준 · 가름 열** → 2.2
- **임계 구역 디버그 정보** → 2.5
- **LFH** (Low Fragmentation Heap) → 2.7 (run46 해설에는 이름만 나오고 풀이가 없어서 여기서 한 줄)
- **이름 칸 · 옆 칸 · 코드 접근 폭** → 2.11

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**, 프로세스 힙 = 가게 **창고**.
- ntdll 전역 = 관리실 **칠판**. 칸마다 이름표가 붙어 있다.
  - ntdll 파일에서 올 때 **이미 글씨가 인쇄돼 온 칸** 도 있다 (= 파일 처음 값).
- **하나만 바꿔 부른다.**
  - run 문서는 이번에 ntdll `.data` 를 "가게 뒤 **창고 선반**" 이라고 불렀다.
  - 그런데 앞 해설들에서 **창고 = 프로세스 힙** 이다. 이번 바퀴엔 힙 이야기 (LFH) 도 나와서 둘이 섞이면 헷갈린다.
  - 그래서 이 글은 앞 해설대로 **칠판** 이라고 부른다. 같은 뜻이다.
- run 문서가 이번에 쓴 그림 (말만 칠판으로 바꿔서 가져온다):
  - 남은 칸 72개를 다 열어 봤더니, **절반 (36) 은 인쇄돼 온 그대로** 였고 이 가게 문 여는 길에선 아무도 안 만진다.
  - 나머지 절반 중 18칸은 문 열 때 직원이 직접 채웠고 (그중 2칸은 관리사무소 (nt) 창구에 물어 받은 번호), 2칸은 "0" 을 적었고, 16칸은 "**특별한 가게일 때만**" 채우는 칸이었다 (패키지 앱 · 앱 검사기 · shim · IFEO 가 있을 때 등).
  - 이름표가 칸 크기를 잘못 짐작해 **옆 칸 글씨를 자기 것처럼 보이게** 한 곳이 넷 있었다 (2.11).

### 한 줄로 먼저 — 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| 이번에 가른 ntdll 전역 | **72개** = 7.3 `.data ≤P1a` 57 · `.mrdata` 4 · `.data P1a~P1b` 3 + 그 밖 8 |
| 72개가 간 곳 | 값을 쓰는 줄 **18** (② 16 · syscall 2) · 0 을 씀 **2** · 조건부 안 탐 **16** · 아무도 안 씀 **22** · 범위 밖 길 **14** |
| 짐 지도에서 | 앞 셋 **36** → 3.8 "아는 칸" 으로 옮김 · 뒤 둘 **36** → 7.3 에 두고 표시만 |
| 7.3 ntdll 전역 모름 | **304 → 268** (`.data` 274 → 238) |
| 정정 ① (run45) | `EnvironmentVersion` 나머지 +2 = 해시 표 쪽이 아니라 **`RtlpWow64ThunkEnvironmentTo64` 안 `RtlSetEnvironmentVar` 두 번** `[정적]` |
| 정정 ② (run46) | 프로세스 힙 LFH 는 "나중 · 범위 밖" 이 아니라 **사진 P1a 앞에 이미 켜져 있다** `[관찰]` + 켜는 줄 사슬 `[정적]` |
| Q80 | **닫힘** — `≤P1a` 61 중 46 = P1a 값이 파일 값 그대로, 15 = 쓰는 줄로 닫힘 |
| run46 이 넘긴 조건부 힙 전역 8 | **모두 닫힘** |
| 7.3 라벨 정정 | 넷이 옆 칸 값 때문이었다 (`ShowSnaps` · `RtlpDisableHeapLookaside` · `LdrpInitOnceLoadAsDataCrits` · `RtlpSearchPathMode`) |
| 그림 | **안 바뀜** — 이미 있는 함수 안쪽 줄과 그 아래 한두 걸음만 늘었다 |

이 글에서 붙잡아 둘 세 줄:

> **(1) 칠판 칸 72개를 다 가름했다. 그런데 "모름" 숫자는 36만 줄었다.**
> 나머지 36 (아무도 안 씀 · 범위 밖) 은 가름은 끝났지만, 전역에 그 가름을 셀 열이 아직 없어서 7.3 에 "모름" 으로 남겨 두고 표시만 했다. 열을 새로 둘지는 사용자가 정한다.
>
> **(2) 옛 짐작 둘을 고쳤다.**
> `EnvironmentVersion` 의 나머지 +2 는 WOW64 썽크 함수 안의 환경 변수 쓰기 두 번이고, 프로세스 힙 LFH 는 P1a 앞에 이미 켜진다.
>
> **(3) 칸 크기 짐작이 라벨을 속인 곳이 있다.**
> 칸 크기를 "다음 이름까지 거리" 로 짐작하니 옆 칸 값이 이름 칸에 묻혔다. 코드가 실제로 만지는 폭으로 다시 보니 라벨 넷이 바뀌었다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 한 구간이 아니라 **칠판 칸을 쓰는 줄 전부** 를 훑었다. 대부분 `LdrpInitializeProcess` 몸통 안이다.

```
cmd.exe 의 부탁 → 커널 (① 가게 · 첫 직원 만들기) → 첫 스레드 ntdll 시작
 ② test.exe 첫 스레드, ntdll
  LdrpInitializeInternal  +0x3b 핫패치 깃발 0→1 (이긴 스레드만)
   └─ +0x93  NtCreateEvent ─ LdrpHotPatchInitCompleteEvent         ← 이번 (2.4)
  _LdrpInitialize ─ +0x16c LdrpInitializeProcess
     +0x4db / +0x4fe   무결성 깃발 둘 = 0                            ← 이번 (2.8)
     +0x6b5 ~ +0x7c9   임계 구역 타임아웃 · 디버그 정보 묶음            ← 이번 (2.5)
     ├─ +0x9f2  RtlInitializeHeapManager  … 첫 열쇠의 디버그 정보       ← 이번 (2.5)
     ├─ +0xa50  LdrpInitializeProcessHeap (run46 창고 짓기)
     │          … 첫 힙 할당 어딘가 → LFH 켜기                         ← 이번 (2.7, 정정)
     ├─ +0xab2 ~ +0xb52  KVS 기능 상태 · 엔클레이브 목록 · 0xc5 질의   ← 이번 (2.4 · 2.5)
     ├─ +0xc32  RtlpInitEnvironmentBlock → WOW64 썽크 → 환경 변수 두 번 ← 이번 (2.6, 정정)
     ├─ +0x1aa9 … LdrpInitMuiCrits   (P1a~P1b)                         ← 이번 (Q78f)
     └─ +0x1ab1 / +0x1b91 LdrpDrainWorkQueue ─ LdrpUpdateStatistics   ← 이번 (Q78f)
  → NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다 (run 문서 "호출 관계" 그대로). call 자리는 2.3 · 2.5 의 표에 따로 적는다.
- 지도의 큰 길은 **그대로** 다. 새 단계도 새 caller 도 없다.

### 무엇이 궁금했나

짐 지도 7.3 에는 "쓴 자리를 모르는 ntdll 전역" 이 **304개** 남아 있었다. 이번엔 그중 **앞쪽** 을 맡았다.

- `.data ≤P1a` 57 · `.mrdata ≤P1a` 2 · `.mrdata K1 이전` 2 — Q78e2
- `.data P1a~P1b` 3 (`RtlpSearchPathMode` · `LdrpStatistics` · `LoadAsDataCrits`) — Q78f
- 그 밖 8: run46 이 넘긴 조건부 힙 전역 중 새 4, run113 이 넘긴 전역 4
- 칸 말고도 두 질문:
  - 매개변수 `EnvironmentVersion` 0 → 3 중 **나머지 +2** 는 누가?
  - 묶음 7 힙 **뒤** 부분 (`LdrpInitializeProcess +0xa50 ~ +0xb7d`) 에서 무엇이 칠판에 쓰이나?
- 그리고 Q80: 같은 `≤P1a` 칸의 P1a 값이 **파일에 인쇄돼 온 값** 과 같은가?

### 바퀴 전에 알던 것

- `[정적]` `EnvironmentVersion` +1 은 `RtlpInitEnvironmentBlock +0x86` (run45). 나머지 +2 는 "`+0xa0 RtlpSetupEnvironmentHashTable` 쪽" `[추측]` — **이번에 틀린 것으로 판명** (2.6).
- `[정적]` run46: 조건부로만 쓰는 힙 전역 8개의 쓰는 줄은 결과서에 있었지만 "닫힌 칸" 으로 안 셌다. LFH 두 칸은 "범위 밖" 으로 적었다 — **이번에 틀린 것으로 판명** (2.7).
- `[관찰]` run44 사진: 칸마다 처음 값이 생긴 **구간** 은 이미 안다. 다만 `.data` · `.mrdata` 앞 3페이지는 K1 ~ P1 에 읽기 불가라 `≤P1a` 로만 묶였다.
- run113 이 Q78e2 에 얹을 전역 넷을 넘겼다 (무결성 깃발 둘 · `LdrpModuleDatatableLock` · `g_pShimEngineModule`).

---

## 2. 찾은 것

### 2.1 한눈에 — 72칸은 어디로 갔나

run 문서 요약 표를 줄였다.

| 묶음 | 칸 | 값을 쓰는 줄 (② · syscall) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (잠금 포함) | 범위 밖 | → 3.8 | 7.3 에 남음 |
|---|---|---|---|---|---|---|---|---|
| `.data ≤P1a` | 57 | 14 · 2 | 0 | 11 | 19 (잠금 2) | 11 | 27 | 30 |
| `.mrdata ≤P1a` 2 · `K1 이전` 2 | 4 | 0 | 0 | 0 | 2 | 2 | 0 | 4 |
| `.data P1a~P1b` (Q78f) | 3 | 2 · 0 | 0 | 0 | 0 | 1 | 2 | 1 |
| 그 밖 · 조건부 힙 새 4 | 4 | 0 | 0 | 4 | 0 | 0 | 4 | 0 |
| 그 밖 · run113 넘김 4 | 4 | 0 | 2 | 1 | 1 (잠금) | 0 | 3 | 1 |
| **합** | **72** | **16 · 2** | **2** | **16** | **22** | **14** | **36** | **36** |

- 표 읽는 법: 왼쪽 다섯 가름 칸을 더하면 "칸" 이 된다. 그 다섯을 두 무더기로 나눈 게 오른쪽 두 칸이다.
  - **→ 3.8** = 값을 쓰는 줄 + 0 을 씀 + 조건부 안 탐
  - **7.3 에 남음** = 아무도 안 씀 + 범위 밖
- 왜 이렇게 나누나 → 바로 다음 2.2.

### 2.2 셈 기준 — 왜 "조건부 안 탐" 은 옮기고 "아무도 안 씀" 은 안 옮기나

이번 바퀴의 숫자 (모름 304 → 268) 는 **어떻게 세느냐** 에 달려 있다. 천천히 보자.

#### 새 말: 셈 기준 · 가름 열

- **셈 기준** = 가름이 끝난 칸을 짐 지도의 **어느 칸 (열) 에 넣어 셀지** 정한 약속이다.
  - 비유: 이삿짐 상자를 열어 보고 "누가 쌌나" 를 알아냈어도, 점검표에 그 답을 적을 **칸이 있어야** "확인 끝" 도장을 찍을 수 있다.
- **가름 열** = 짐 지도 2장 표에서 "아무도 안 씀" · "범위 밖" 처럼 가름 결과를 따로 세는 열이다.
  - 구조체 칸에는 이 열이 이미 있다 (run113 해설 3절 표의 "아무도 안 씀 185 · 범위 밖 262").
  - **전역에는 아직 없다.** 전역은 "쓴 자리 아는 것 / 모름" 두 갈래뿐이다.

#### 이번 셈 기준 (오케스트레이터가 정함, run60 `LdrpMrdataHeap*` · run66 `g_pShimmedModuleList` 선례)

1. **값을 쓰는 줄이 있는 전역** (②, syscall) 과 **0 을 씀** → 3.8 "아는 칸".
2. **조건부 안 탐** 도 3.8 로 옮긴다.
   - 까닭 (run 문서): 쓰는 줄은 **지도 길 안에** 있다. test.exe 가 그 조건을 안 채울 뿐이다.
   - 그러니 "누가 · 어디서 쓰나" 는 답이 있다. 비어 있는 이유도 코드로 댈 수 있다.
3. **아무도 안 씀** (결과서 말로 "쓰는 줄 없음") · **범위 밖 길** → 이번엔 **7.3 에 그대로 두고** `(run114 가름: …)` 표시만 한다.
   - 까닭: 전역에 그 가름을 셀 열이 없다. 열을 새로 둘지는 **사용자 결정 대기** 다 (sast/111 · sast/113 결과서의 A/B/C 안).

#### 그래서 숫자가 이렇게 된다

- 3.8 로 간 36 만큼 모름이 준다: **304 → 268**.
- 7.3 의 268 안에는 "**가름은 끝났지만 모름으로 남은**" 36 이 들어 있다 (run 문서 "아직 확인 필요" 정적 ②).
  - 그러니 268 을 "아직 아무것도 모르는 칸 268" 로 읽으면 안 된다.
- 결과서는 합치기 제안을 둘 냈다. 작은 쪽 (값을 쓴 칸만) 과 큰 쪽 (조건부 안 탐 · 0 을 씀도). 이번엔 **큰 쪽** 을 따랐다.

#### 비유로

- 점검표에 "확인 끝" 칸이 두 개 있다고 하자: "**누가 넣었나 적음**" 칸과 "**모름**" 칸.
- 열어 보니 "특별한 날에만 넣는 상자" (조건부 안 탐) 는 누가 넣는지 알아서 첫 칸에 적었다.
- "아무도 안 넣는 상자" · "다른 집 사람이 넣는 상자" 는 적을 칸이 없어 "모름" 칸에 두고 옆에 연필로 메모만 했다.
- "그 둘을 위한 칸을 새로 그릴까?" 가 사용자에게 남은 질문이다.

### 2.3 한 줄씩 읽는 법

run 문서 "정적 — 호출 지점" 표에서 셋을 골라 "누가 / 어디서 / 누구를 / 무엇을 위해" 로 읽는다.

#### 줄 ①: `ntdll!LdrpInitializeInternal | +0x8e | +0x93 | NtCreateEvent`

- **누가**: ntdll `LdrpInitializeInternal` — 첫 스레드가 ntdll 에 들어와 가장 먼저 지나는 함수들 중 하나.
- **어디서**: call `+0x8e`, 돌아올 자리 `+0x93`.
- **누구를**: `NtCreateEvent` (syscall).
- **무엇을 위해**: 이벤트를 하나 만든다. 첫 인자로 칠판 칸 `LdrpHotPatchInitCompleteEvent` 의 **주소** 를 넘긴다 `[정적]`.
  - 그래서 핸들 번호를 그 칸에 **직접 적는 건 nt** 다. 사진 값은 핸들 8 `[관찰]`.
  - 앞 `+0x3b` 의 `CMPXCHG.LOCK [LdrpHotPatchInitialized]` 에서 **이긴 첫 스레드만** 이 줄에 온다 `[정적]`.
  - flow.md: 둘째 스레드가 기다리는 이벤트가 이것이다 `[정적]`.

#### 줄 ②: `ntdll!LdrpInitializeProcess | +0xb4d | +0xb52 | NtQuerySystemInformation`

- **누가**: `LdrpInitializeProcess`, 묶음 7 힙 뒤 부분.
- **어디서**: call `+0xb4d`.
- **누구를**: `NtQuerySystemInformation`, 클래스 `0xc5`.
- **무엇을 위해**: 답을 **지역 버퍼** 로 받는다. 그다음 ntdll 이 `+0xb5e` 에서 칠판 칸 `RtlpHypervisorSharedUserVa` 로 옮겨 적는다 `[정적]`.
  - syscall 이 실패하면 `+0xb54 JS` 로 안 쓴다.
  - 사진 값 `0x7ffe3000` `[관찰]` — run 문서 말로 "KUSD 뒤 3쪽, nt 가 올린 쪽".
- 줄 ① 과 줄 ② 는 **둘 다 syscall 이 값을 정하는데**, 쓴 주인이 다르게 세진다 → 2.4.

#### 줄 ③: `ntdll!RtlpWow64ThunkEnvironmentTo64 | +0x126 | +0x12b | RtlSetEnvironmentVar`

- **누가**: `RtlpWow64ThunkEnvironmentTo64` — `RtlpInitEnvironmentBlock` 이 `+0xa4` 에서 부른다.
- **어디서**: call `+0x126`. 표 한 줄마다 한 번.
- **누구를**: `RtlSetEnvironmentVar` (환경 변수 하나 쓰기).
- **무엇을 위해**: 표 `Wow64EnvironmentThunkTable` 의 줄마다, 앞 `+0xa5 RtlQueryEnvironmentVariable` 로 `…W6432` 이름 변수를 찾고, **찾았을 때만** 원래 이름 변수에 그 값을 쓴다 `[정적]`.
  - 이게 바로 `EnvironmentVersion` 나머지 +2 의 정체다 → 2.6.

### 2.4 syscall 둘 — 왜 하나는 nt, 하나는 ntdll 이 썼다고 세나

값이 syscall 에서 오는 칠판 칸이 둘 있다. 결과서는 둘 다 "쓴 주인 nt" 로 적었다. run 문서는 **하나만 nt** 로 셌다.

단계로 보면:

1. **규칙** (짐 지도, run59): "syscall 결과를 **지역 버퍼** 로 받아 옮기는 캐시 전역은 ntdll 이 쓴 것" 이다.
   - 커널은 ntdll 이 넘긴 버퍼에 답을 적을 뿐이다. 칠판 칸에 펜을 대는 건 ntdll 이다.
2. **`LdrpHotPatchInitCompleteEvent`**: 칸 주소를 syscall 에 **바로** 넘긴다 (`+0x82 LEA [칸]`). 커널이 그 칸에 직접 쓴다 → **nt** `[정적]`.
   - `LdrpInitCompleteEvent` 와 같은 규칙이다.
3. **`RtlpHypervisorSharedUserVa`**: 커널은 지역 버퍼 (`[RSP+0x168]`) 에 쓴다. ntdll 이 `+0xb56 MOV RAX,[RSP+0x168]` → `+0xb5e MOV [칸],RAX` 로 옮긴다 → **ntdll** `[정적]`.
4. 가름 (값을 쓰는 줄 있음) 은 둘 다 같다. 다른 건 **쓴 주인 칸** 하나뿐이다.
   - 그래서 짐 지도 2장 `.data` 의 nt 가 5 → **6** (+1 은 핫패치 이벤트 하나), ntdll 이 162 → **197**.

비유: 관리사무소 창구에 "번호표 이 칸에 찍어 주세요" 하고 칠판을 들이밀면 **창구 직원** 이 쓴 것이다. 창구에서 쪽지로 받아 와서 칠판에 옮겨 적으면 **관리실 직원** 이 쓴 것이다.

### 2.5 값을 쓰는 줄 18 — 대표만

run 문서 1절 표에서 대표만 옮긴다. 모두 `[정적]` (쓰는 줄) + 사진 값 `[관찰]`.

| 이름 | 쓰는 줄 (call · 쓰는 줄 자리) | 사진 값 |
|---|---|---|
| `RtlpTimeout` | `LdrpInitializeProcess +0x6b5` (PEB `+0xc0` 복사) | `0xffffe86d079b8000` = −30일 (값 주인 nt, PEB 와 같음) |
| `RtlpTimeoutDisable` | `+0x76d` (`RtlpTimeout` < −1시간이면 1) | 1 |
| `LdrpThunkSignature` (이름 칸 `0x50`B) | `LdrpCaptureCriticalThunks +0x95` | 스텁 5개 앞 16B — 사진 80B 와 바이트 단위로 같음 |
| `RtlpFlsContext` | `LdrpInitializeProcess +0x82e ~ +0x86e` | 0 · 목록 머리 자기 (`+0x48`) |
| `RtlpEnvironmentHashTable` (`0x20`B) | `RtlpSetupEnvironmentHashTable +0x10 ~ +0x52` · `RtlpScanEnvironment` | `{개수 0x2f, 크기 0x400, 표, 스캔됨 1, 환경}` |
| `LdrpEnclaveListLock` · `LdrpEnclaveList` | `+0xad3` 주소 → `+0xada RtlInitializeCriticalSectionEx(칸, 0, 0)` · `+0xae6` · `+0xaed` | `DebugInfo −1` … · 자기 가리킴 |
| `Feature_KVS_BugFixes_55830752__private_featureState` | `…ReevaluateCachedFeatureEnabledState +0xa0` | `0x40147` |
| `LdrpStatistics` (12B, Q78f) | `LdrpUpdateStatistics +0xe` · `+0x33` · `+0x2b` | `{3, 3, 3}` |
| `LoadAsDataCrits` (Q78f) | `LdrpInitMuiCrits +0x4f RtlInitializeCriticalSectionEx(칸, 0, 0)` | `DebugInfo −1` · `LockCount −1` |

- `RtlpTimeout` 은 ntdll 이 **베끼기만** 한다. 값 주인은 PEB `+0xc0 CriticalSectionTimeout` 을 쓴 nt 다 (짐 지도 3.1, 단계 ①).
- `Feature_KVS…` 칸: 첫 기능 상태 검사 (`+0xaad`, [run62 해설 2.9](run62-tp-etw-init-해설.md)) 가 **필요 기능** 인 KVS 칸도 같이 평가해 쓴다. flow.md 에 따르면 이 기능엔 `IsEnabled…` 함수가 없다.
- `LdrpStatistics {3, 3, 3}` 은 주 스레드 갈래 (`LdrpDrainWorkQueue`) 와 일꾼 갈래 (`LdrpWorkCallback`) 가 **둘 다 탔다** 는 뜻이다 `[관찰 간접]`.

#### 새 말: 임계 구역 디버그 정보 — 열쇠마다 다는 꼬리표

- 임계 구역 = **열쇠** ([run18 해설](../02_sprint/run18-proc-init-setup-해설.md)).
- **임계 구역 디버그 정보** = 열쇠마다 붙여 두는 **꼬리표** 다. 꼬리표는 한 줄 목록 `RtlCriticalSectionList` 에 꿰인다 `[정적]`.
  - 꼬리표에 무엇이 적히는지는 이번 근거 문서가 다루지 않는다.
- 이번에 이 꼬리표 묶음 칸 셋 (`RtlCriticalSectionDebugSList` · `RtlpStaticDebugInfo` · `RtlCriticalSectionList`) 의 쓰는 줄을 찾았다.

단계로 `[정적]`:

1. 힙 (창고) 이 생기기 **전에**, `LdrpInitializeProcess +0x78d` 가 SList 머리를 0 으로 놓는다.
2. `+0x79e ~ +0x7b4` 가 칠판에 미리 마련된 꼬리표 8장 (`RtlpStaticDebugInfo`) 을 서로 잇는다.
3. `+0x7c4` 가 그 8장을 SList (`RtlCriticalSectionDebugSList`) 에 **한꺼번에** 넣는다 (`InterlockedPushListSList` — 같은 주소 이름 2개, 후보 `RtlInterlockedPushListSList`). 깊이 8.
4. 창고 짓기 구간에서 `RtlInitializeHeapManager +0x12a` 가 첫 열쇠 `RtlpProcessHeapsLock` 을 만든다 (깃발 `0x10000000`).
5. 그 안 `RtlpAddDebugInfoToCriticalSection` 이 SList 에서 꼬리표 하나를 **꺼내** (`+0x17`), `RtlCriticalSectionList` 꼬리에 잇는다 (`+0x353` · `+0x356`).
6. 둘째 꼬리표 = 프로세스 힙 락.

사진과 맞춰 보면 `[관찰]`:

- SList 깊이: 8 → P1a **6** → P1b **5**. 꺼낸 만큼 줄었다.
- `RtlpStaticDebugInfo` 항목 0 `+0x8` = `RtlpProcessHeapsLock`, 항목 1 `+0x38` = 프로세스 힙 락. 그래서 목록 첫 둘이 이 두 열쇠라고 **값 증거로** 읽었다.

아직 확인 아님:

- 깃발 `0x10000000` = "디버그 정보 강제" 라는 뜻은 `[추정]` 이다 (결과서 — `RtlInitializeCriticalSectionEx` 본문은 안 읽음).
- 다른 열쇠 (`LdrpEnclaveListLock` · `LoadAsDataCrits`) 는 `Flags 0` 이라 `DebugInfo = −1` 이다. 꼬리표를 안 단 것으로 보인다 `[추측]` (일반적으로 알려진 설명 — −1 은 "디버그 정보 없음" 표시).

비유: [run46 해설](run46-process-heap-해설.md) 의 "창고가 생기기 전에 필요한 것들은 어디 두나" 와 같은 꼴이다. 창고가 없으니 **꼬리표 8장을 칠판에 미리 찍어 바구니에 넣어 두고**, 열쇠가 생길 때마다 한 장씩 꺼내 단다.

### 2.6 정정 1 — `EnvironmentVersion` 나머지 +2 (run45 고침)

#### 무엇이 있었나

- run44 사진: 매개변수 블록 `EnvironmentVersion` 이 0 → **3** `[관찰]`.
- run45: +1 은 `RtlpInitEnvironmentBlock +0x86` `[정적]`. 나머지 +2 는 "`+0xa0 RtlpSetupEnvironmentHashTable` 쪽" `[추측]`.
  - 이 짐작이 **틀렸다.** 해시 표 함수는 버전을 올리지 않는다.

#### 코드는 어떤가 — 단계별로 `[정적]`

같은 `RtlpInitEnvironmentBlock` 호출 (`LdrpInitializeProcess +0xc2d`) 안, 사진 구간 `P1~P1a`.

| 순서 | 줄 | 하는 일 | `EnvironmentVersion` |
|---|---|---|---|
| ① | `RtlpInitEnvironmentBlock +0x86 INC` | 환경을 힙 블록으로 복사한 뒤 | 0 → 1 |
| ② | `+0xa4` → `RtlpWow64ThunkEnvironmentTo64 +0x126 RtlSetEnvironmentVar` | 표 첫 줄: `ProgramFiles` ← `ProgramW6432` 값 | 1 → 2 |
| ③ | 같은 `+0x126` 둘째 바퀴 | `CommonProgramFiles` ← `CommonProgramW6432` | 2 → 3 |
| — | 셋째 바퀴 | `PROCESSOR_ARCHITECTURE` ← `PROCESSOR_ARCHITEW6432` | 그대로 |

- 셋째 바퀴는 왜 안 오르나: `PROCESSOR_ARCHITEW6432` 는 WOW64 프로세스 환경에만 있는 변수라, test.exe 에선 찾기가 실패해 `+0xc4 JS` 로 건너뛴다 `[정적]`.
  - WOW64 = 64비트 Windows 에서 32비트 프로그램을 돌리는 층 `[추측]` (일반적으로 알려진 설명).
- 표 `Wow64EnvironmentThunkTable` (`.rdata` `0x173b30`, 줄 `0x28`B × 3) 내용은 결과서가 파일에서 읽었다.

#### 왜 "딱 두 번" 이라고 말할 수 있나

1. 지도 안에서 이 칸 (`[reg+0x3f8]`) 을 올리는 줄은 `RtlpInitEnvironmentBlock +0x86` 과 `RtlSetEnvironmentVar` 둘뿐이다 (나머지 `RtlSetCurrentEnvironment` · `RtlSetEnvironmentStrings` 는 지도 밖 공개 API).
2. 그러니 1 (INC) + 2 (환경 변수 쓰기 두 번) = 3.
3. 사진 `0 → 3` 과 맞는다 `[정적 + 사진 간접]`.
4. 아직 확인 아님: `RtlSetEnvironmentVar` 안에서 버전을 올리는 줄이 갈래마다 한 번인 건 봤지만, 갈래 전체는 안 따라갔다 `[정적, 갈래 전체는 안 따라감]`. 실제 1 → 2 → 3 을 멈춰서 보는 건 동적 D4 (4절).

#### 해시 표는 그럼 무엇을 했나

- `RtlSetEnvironmentVar` 가 `RtlpClearEnvironmentHashTable` 로 표를 비우고, 다음 `RtlQueryEnvironmentVariable` 이 `RtlpScanEnvironment` 로 다시 채운다 `[정적]`.
- 사진 P1a `{개수 47, 스캔됨 1}` 과 맞는다 (마지막 조회 줄은 안 짚음).
- 곧 해시 표 함수는 **버전을 올리는 쪽이 아니라, 버전이 오를 때 비워졌다 다시 채워지는 쪽** 이었다.

비유: 개업 서류 (매개변수 블록) 의 "부록 (환경)" 에 **수정 횟수 칸** 이 있다. 부록을 창고로 옮길 때 한 번 (+1), 그다음 "64비트용 주소로 고쳐 적기" 표를 보며 두 줄을 고쳐 적을 때 한 번씩 (+2). 셋째 줄은 고칠 원본이 없어서 건너뛴다.

#### PE_emulator 에게

- **틀림 (알고 있는 차이) — 우리 1, 진짜 3.** 우리 환경 표에 `ProgramW6432` · `CommonProgramW6432` 가 없어 `RtlQueryEnvironmentVariable` 이 실패하고 몸통을 건너뛴다.
- 두 변수를 넣으면 `+0x126` 이 두 번 불려 3 이 될 것 `[정적 추정]` (PE_emulator 일 55 "다시 볼 것" 과 같은 얘기).

### 2.7 정정 2 — 프로세스 힙 LFH 는 P1a 앞에 이미 켜진다 (run46 고침)

#### 새 말: LFH

- **LFH (Low Fragmentation Heap)** = 작은 블록을 **크기별 칸에 모아** 나눠 주는 힙 할당기다 `[추측]` (일반적으로 알려진 설명).
- 비유: 창고 안에 **작은 물건 전용 칸칸이 선반** 을 하나 들여놓는 것. 같은 크기 상자끼리 모아 두면 빈 틈이 덜 생긴다.

#### 무엇이 있었나

- run46: `RtlpLowFragHeapRandomData` (LFH 켜질 때) 를 "범위 밖", `RtlpLowFragHeapGlobalFlags` 비트 0 을 "블록 나눠 주기 쪽 — 범위 밖" 으로 적었다.
- 곧 "LFH 는 **나중에** 켜지는 일, 우리 범위 밖" 으로 읽혔다. 이게 **틀렸다.**

#### 근거 → 그래서

1. **사진** `[관찰]` run44: P1a 에 이미 `RtlpLowFragHeapGlobalFlags` = **1**, `RtlpLowFragHeapRandomData` 32칸 = **난수** 가 있다.
   - P1a 는 `LdrpInitializeProcess` 가 끝나기 **전** 이다. 그러니 LFH 는 이미 그 앞에서 켜졌다.
2. **켜는 줄 사슬** `[정적]`:
   - `RtlpAllocateHeap +0xe5` (`[정적: objdump]` — 이 함수는 `.asm` 이 없어 결과서 objdump 풀이로 봄)
   - → `RtlpPerformHeapMaintenance +0x22`
   - → `RtlpActivateLowFragmentationHeap +0xde`
   - → `RtlpCreateLowFragHeap +0x256` (깃발 비트 0) · `+0x25c` `RtlpInitializeLfhRandomDataArray` (`+0x4b` 32칸, xorshift64\*)
3. **조건** `[정적]`:
   - 힙 깃발 비트 29 — 창고를 지을 때 `RtlpCreateHeap +0x1a3 BTS EDX,0x1d` 가 켠다 (생성 깃발 `& 0x80` 일 때).
   - `RtlpDisableHeapLookaside` 비트 0 꺼짐.
   - `RtlpAllocateHeap` 은 힙 `+0x78 & 0x30000000` 일 때 정비 함수를 부른다 (`+0xd9` · `+0xe0`).
4. **그래서**: 프로세스 힙 LFH 는 `LdrpInitializeProcess` 중간 (`P1~P1a`) 의 **어느 프로세스 힙 할당 안에서** 켜진다.
5. **아직 확인 아님**: **어느** 할당인지는 모른다. `RtlAllocateHeap` 은 공용 부품이라 지도에서 그 아래를 따라가지 않는다 → 동적 **D1** (4절).

#### run46 의 "범위 밖" 결정은 어떻게 되나

- run46 때 사용자 결정 "블록 나눠 주기 속 (`RtlAllocateHeap` 본문) 은 기록만" 은 **분석 범위** 에 대한 약속이다.
- 이번 정정은 그 약속을 바꾼 게 아니다. "LFH 를 켜는 일이 P1a 앞에 **일어난다**" 는 사실을 바로잡은 것이다. 칠판 칸 둘은 쓰는 줄이 있으니 3.8 로 갔다.

비유: 창고를 다 짓고 나서 "작은 물건 선반" 은 나중에 들일 줄 알았다. 그런데 사진을 보니 **첫 상자를 꺼내 줄 무렵에 이미 선반이 들어와 있었다.** 누가 몇 번째 상자 때 들였는지는 아직 모른다.

#### PE_emulator 에게

- 우리 시험 기대값 `RtlpLowFragHeapGlobalFlags` 0 은 LFH 켜기 **앞** 단계라 맞다. 사진 P1a 의 1 은 켜기 **뒤** 값이다.
- 힙 할당 길 (LFH 켜기) 을 넣으면 `RtlpLowFragHeapRandomData` 32칸도 나온다. 난수라 사진과 같을 필요는 없다.

### 2.8 0 을 씀 2 — 무결성 깃발 둘

- `LdrpEnforceIntegrityContinuity` (`+0x4db SETZ`) · `LdrpAuditIntegrityContinuity` (`+0x4fe MOV`) `[정적]`.
- 값은 `LdrSystemDllInitBlock +0xa8` (커널이 채운 `MitigationOptionsMap` 둘째 qword) 에서 `>> 4 & 3` 으로 계산한다. **무조건** 실행되는 줄이다.
- test.exe 는 입력 비트가 0 → 둘 다 **0 을 쓴다**. 사진 `끝까지 0` 과 맞는다.
- 읽는 쪽: `LdrpMapDllNtFileName +0x141 · +0x416` · `LdrpValidateIntegrityContinuity +0x90`.
- run59 · run113 이 "Q78e2 몫" 으로 넘긴 것이 여기서 닫혔다.
- 비유: 업무 수첩 한 줄을 보고 칠판 두 칸에 "해당 없음 (0)" 을 **꼭 적고 가는** 것. 안 적은 게 아니라 0 을 적은 것이다 ([run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법) 의 "0 을 씀").

### 2.9 조건부 안 탐 16 — test.exe 같은 가게엔 해당 없음

16칸 모두 **값 = 파일 처음 값 = 사진** 이다. 쓰는 줄은 있는데 test.exe 가 조건을 안 채운다. 조건별로 묶으면:

| 조건 (test.exe 는?) | 칸 | 근거 줄 `[정적]` |
|---|---|---|
| **패키지 앱** + `DllPath` 비어 있지 않음 (test.exe `BitField 0x4`) | `LdrpPolicyBits` (파일 `0x16`) | `LdrpInitializePolicy +0x7e` 등 5줄 (sast/120 (a) 가 다시 세어 같음) |
| **`NtGlobalFlag & 2`** (로더 스냅 보기, test.exe 0) | `ShowSnaps` = `LdrpDebugFlags` | `LdrpInitializeProcess +0x59c` · `+0x754` `OR …,1` |
| **`NtGlobalFlag & 0x800`** | `RtlpGlobalTagHeap` | `RtlCreateTagHeap +0x97` (`+0x45 JZ`) |
| **IFEO 부분 통째 건너뜀** (`+0xa6 TEST [PP+8],0x4000` · `+0xad JNZ`, `Flags 0x4001`) | `g_SxsMaxDeadActivationContexts` · `RtlpUnloadEventTraceExNumber` · `LdrpQueryProcessModuleInformationLoopDetectorCount` · `RtlpDisableHeapLookaside` | `LdrpInitializeExecutionOptions +0x49c` · `+0x1fe` · `+0x5fe` · `+0x127` |
| **스위트 마스크 비트 16** (test.exe `0x110`) | `RtlpLargestLfhBlock` (파일 `0x4000`) | `RtlInitializeHeapManager +0x186` (`+0x17d JNC`) |
| **세그먼트 힙** 을 고를 때 (`RtlpHpOptIntoSegmentHeap` 반환 ≠ 0 — test.exe 는 NT 힙) | `RtlpHpHeapFeatures` · `g_RegInfo` | `RtlInitializeHeapManager +0x31` · `+0x5b` (`+0x2f JZ`) 등 |
| 전역 힙 설정 길의 조건부 갈래 | `RtlpHpAppCompatFlags` | `RtlpHpSetLowFragHeapGlobalFlags +0x85` 등 |
| **힙 오류** 를 보고할 때만 | `RtlpHeapFailureInfo` | `RtlpLogHeapFailure +0x21 ~` 등 |
| **AppVerifier** (`AvrfAppVerifierMode == 0` 이면 건너뜀) | `RtlpDebugPageHeapTable` | `AvrfMiniLoadDll +0xa4f` (`LdrpInitializeProcess +0x1568`) |
| **shim 엔진** (`g_ShimsEnabled` 0) | `g_pShimEngineModule` | `LdrpInitShimEngine +0xf5` 등, `+0x1ca7 CMP · +0x1cae JZ` 로 `+0x1cfe` 도 건너뜀 |
| 멤버 풀 칸이 0 일 때만 (풀이 붙는 멤버라 안 탐) | `TppCleanupGroupMemberpNoPoolList` | `TppCleanupGroupMemberInitialize +0x284` (`+0x17d JZ`) |
| 간접 호출 `TermsrvGetWindowsDirectoryW` 가 길이 0 | `LdrpUserDllDirectories` | `LdrAddDllDirectory +0x1b5` 에 안 닿음 (`+0x4e` → `+0x50` 끝) |

- `LdrpPolicyBits` 는 run65 정적 ① (짐 지도 7.24) "3.8 로 옮길지" 의 답이다 → **옮김** (조건부 안 탐).
- `LdrpQueryProcessModuleInformationLoopDetectorCount` 의 `0x19000` 은 run66 실마리대로 **자기 값 (파일 값)** 이 맞았다.
- `LdrpUserDllDirectories` 의 "안 탐" 은 사진 변화가 없다는 데서 `[관찰 간접]`.
- run46 이 넘긴 "조건부 힙 전역 8" 은 위 표 여섯 + 2.7 의 LFH 둘로 **모두 닫혔다.**
- 비유: [run113 해설 2.7](run113-ntdll-user-structs-해설.md) 과 같다. "패키지 가게 · 특별 지시 (IFEO) · 앱 검사 · 호환성 고치기 · 다른 설계도 창고일 때만" 쓰는 칸이다. test.exe 는 그 어느 것도 아니다.

### 2.10 7.3 에 남는 36 — 가름은 끝, 표시만

2.2 의 셈 기준대로 7.3 에 두고 `(run114 가름: …)` 만 붙였다.

#### 아무도 안 씀 22 (결과서 "쓰는 줄 없음")

이름 칸에 쓰는 명령이 ntdll 에 **아예 없다**. 값은 파일 처음 값.

- `.data ≤P1a` 19: CRT 표 (`_pctype` · `_pwctype` · `__initiallocinfo` · `__lconv_c` …), 스위치 표 항목 둘, `_DEBUG` 이름 칸 셋, `Utf8TableInfo` 등 + **잠금 칸 2** (`LdrpLoaderLock*` · `LdrpDllNotificationLock`).
  - 잠금 칸은 처음 값 = 파일이고, 공용 `RtlEnterCriticalSection` · `RtlLeaveCriticalSection` 이 `+0x8 ~ +0x10` 만 바꾼다.
- `.mrdata` 2: `__guard_check_icall_fptr` · `__security_cookie_complement`.
  - `__security_cookie_complement` 는 파일 값 `~0x2b992ddfa232` 그대로다. `InitSecurityCookie` 는 `__security_cookie` 만 새로 쓰므로 P1a 의 쿠키와 이 칸은 **안 맞는다** — 실패 보고 때만 쓰인다.
- `끝까지 0` 1 (잠금 칸): `LdrpModuleDatatableLock` (SRW 락 빠른 길이 수십 곳 `[추정]`, 사진 3장 풀린 상태 0).

#### 범위 밖 길 14 `[추측]`

쓰는 함수가 **지도 밖** 이다 (공개 API · 오류 · 종료 길).

- `.data ≤P1a` 11: `RtlpSecMemListHead` · `LdrpSnapsLock` · `LdrpDllNotificationList` · `RtlpDiskSpeedPolicy` 등.
- `.mrdata` 2: `NtUserPfn` (user32 `InitializeNtdllUserPfn` 만 — run85 실마리 그대로) · `LdrpVectorHandlerList` (VEH 공개 API — run82).
- `.data P1a~P1b` 1: `RtlpSearchPathMode` (이름 칸은 공개 API `RtlSetSearchPathMode +0xe6` 뿐 — P1b 에 바뀐 건 **옆 칸**, 2.11).

#### `K1 이전` 라벨이 뜻한 것

- `.mrdata` 끝 페이지 둘 (`LdrpVectorHandlerList` · `__security_cookie_complement`) 은 라벨이 `K1 이전` 이었다.
- 이번에 보니 **K1 = 파일 = P1a = P2** 다. "커널이 K1 전에 썼다" 가 아니라 "**파일에 원래 있던 값**" 이라는 뜻이었다.

### 2.11 이름 칸 vs 옆 칸 — 라벨 넷이 속았다

#### 새 말: 이름 칸 · 옆 칸 · 코드 접근 폭

- PDB 에는 전역의 **크기** 가 없다. 그래서 7.3 은 크기를 "**다음 이름까지 거리**" 로 짐작했다 (전부 `[추측]`).
- 그러면 사이에 **이름 없는 칸** 이 있을 때, 그 칸이 앞 이름에 묻힌다. 이걸 이 글에선 **옆 칸** 이라 부른다 (run 문서 말 그대로).
- 이번 결과서는 크기를 **코드 접근 폭** — 코드가 그 이름 주소를 실제로 몇 바이트 만지나 — 으로 다시 봤다. 그렇게 본 칸이 **이름 칸** 이다.
- 이 꼴은 [run60 해설 2.11](run60-func-table-mrdata-해설.md) 의 `LdrpThunkSignature` 뒤 `0x1ce268` 에서 이미 한 번 봤다 (run 문서 말로 "run46 이 먼저 본 꼴").
- 비유: 칠판 이름표가 "여기서 다음 이름표까지 내 칸" 이라고 우겼는데, 실제로 그 이름의 주인은 앞쪽 몇 칸만 쓴다. 뒤쪽 글씨는 **이름표 없는 이웃** 이 쓴 것이다.

#### 무엇이 바뀌었나

| 이름 | 7.3 라벨 | 이름 칸 사진 | 라벨의 원인 (옆 칸) |
|---|---|---|---|
| `ShowSnaps` | `≤P1a` | **끝까지 0** | `+0x10` (`0x1ccf70`) 파일 값 `0xffffffff` (`ShipAssert +0xa6 lock xadd` 카운터) |
| `RtlpDisableHeapLookaside` | `≤P1a` | **끝까지 0** | `+0x8` · `+0x10` (3.8 목록 밖 자리 — run46) |
| `LdrpInitOnceLoadAsDataCrits` | `≤P1a` | P1a 0 → P1b 2 (**`P1a~P1b`**) | `+0x8` (`0x1ce430`, IFEO 바탕 키 핸들 — run46) |
| `RtlpSearchPathMode` | `P1a~P1b` | **끝까지 0** | `+0x8` (`0x1d2358`) P1b `0xb0` — 레지스트리 키 핸들 캐시 |
| `SbDynamicContextDllCount` | `≤P1a` | 이름 칸 (`+0x0`) 파일 값 그대로 | `+0x4` 개수 칸 P1b 3 |

- run 문서 말로 "넷" 은 앞 넷이다. `SbDynamicContextDllCount` 는 같은 표에 같이 실렸다.
- **고친 것**: 3.8 줄 사진 칸 · 구간, 7.3 표시. 옆 칸은 3.8 "목록 밖 자리" 새 줄로 (2.12).
- **안 고친 것**: 7.3 · 2.1 의 **사진 구간 숫자**. 사진 분류는 짐작 크기 기준이라 그대로 둔다 (run66 `g_pShimmedModuleList` 선례).

#### 한 줄 읽기 — `RtlpSearchPathMode`

1. 7.3 은 이 이름을 `P1a~P1b` 로 적었다. P1a 에서 P1b 사이에 값이 생겼다는 뜻이다.
2. 그런데 이름 칸 4B 를 쓰는 줄은 공개 API `RtlSetSearchPathMode +0xe6` 뿐이고, 사진 3장 모두 0 이다.
3. P1b 에 바뀐 건 `+0x8` (`0x1d2358`) 이다. `RtlpLookupCurDirSetting +0x5f NtOpenKey` → `+0x6e CMPXCHG.LOCK` 로 `Session Manager` 키 핸들을 캐시한다 (값은 nt 가 정한 핸들 번호 `0xb0`).
4. 그래서 이름은 7.3 에 남기고 (범위 밖) 옆 칸은 3.8 목록 밖 자리 새 줄로 갔다.

### 2.12 목록 밖 자리 (옆 칸) 새 줄 셋 — 전역 수에 안 셈

이름 없는 칸이라 "전역 438" 숫자엔 안 들어간다. 3.8 표 끝 "목록 밖 자리" 에만 줄이 생겼다.

- `0x1cd804` (`SbDynamicContextDllCount +0x4`, 개수 칸) ← `SbUpdateSwitchContextBasedOnDll +0xfb INC.LOCK` · `+0x2ba XADD.LOCK`. `P1a~P1b` 에 0 → 3.
- `0x1d2358` (`RtlpSearchPathMode +0x8`, 키 핸들 캐시) ← 위 2.11 한 줄 읽기. 경로는 `LdrpDrainWorkQueue` 아래 여러 걸음, `RtlpComputeDllPath` 부터 지도 밖 (결과서).
- `0x1cc578` (`RtlpDebugPageHeapTable +0x8`) ← `LdrpInitializeProcess +0xcca` (앞 `+0xcc0 CMP · +0xcc8 JZ` 로 건너뛸 수 있음 — 조건 뜻은 안 봄 → Q203 ②).

### 2.13 Q80 닫힘 — 파일 값 대조

Q80 은 run44 때 생겼다. "K1 ~ P1 엔 사본 페이지가 없어 못 읽은 `≤P1a` 칸 — 파일 처음 값과 P1a 값을 견줘 가르자" 는 항목이었다.

| 결과 | `.data ≤P1a` 57 | `.mrdata` 4 | `.data P1a~P1b` 3 | 합 64 |
|---|---|---|---|---|
| P1a = P1b = P2 = 파일 값 | 39 | 4 | 1 | 44 |
| P1a = 파일 값, 뒤에 바뀜 | 3 | 0 | 2 | 5 |
| P1a ≠ 파일 값 (코드가 씀) | 15 (② 13 · syscall 2) | 0 | 0 | 15 |

- **Q80 이 보는 `≤P1a` 61 중 46 = P1a 값 = 파일 값** (39 + 3 + 4). 46 모두 코드 근거 (쓰는 줄 없음 · 조건부 · 범위 밖 · 잠금 · 옆 칸) 를 나란히 적었다.
- **15 = 파일 ≠ P1a** → 쓰는 줄로 닫힘 (2.5 표의 칸들).
- Q80 원래 범위의 나머지는 앞 run 들 (run45 ~ run113) 이 이미 닫았다 (`PebLdr` 는 run113). → **오케스트레이터 판단: Q80 닫힘 (run114)**.

#### 46 이 두 번 나온다 — 헷갈리지 말 것

- run 문서 요약 아래: 결과서 "**파일 처음 값 그대로 46**" = 아무도 안 씀 21 + 조건부 안 탐 11 + 범위 밖 14.
  - 이건 **64칸을 쓰는 줄로 가름** 했을 때 "쓰는 줄이 없거나 안 타는" 칸 수다.
- 위 Q80 의 **46** = `≤P1a` 61 중 "P1a 값이 파일 값과 같은" 칸 수다.
  - 이건 **값을 견준** 결과다. 쓰는 줄이 있어도 P1a 까지 파일 값 그대로면 여기 들어간다 (예: `LdrpInitOnceLoadAsDataCrits` 0 → P1b 2).
- 숫자는 같지만 **세는 기준이 다르다.** run 문서도 "`LdrpInitOnceLoadAsDataCrits` 를 `P1a~P1b` 로 고치면 `≤P1a` 기준으론 45" 라고 따로 적었다.

#### 하나 남는 칸

- 짐 지도 7.5 의 CFG 도우미 `LdrpGuardDispatchIcallNoESFptr` (`.mrdata ≤P1a`, 이미 3.9 아는 칸) 는 "Q80 으로 가려진다" 고 적혀 있었는데, sast/111 대조 **밖** 이다.
- run45 해설 4절 표의 "Q80 (정적)" 줄이 가리키던 그 칸이다. → 정적 **Q203 ③** (4절).

### 2.14 결과서와 다르게 센 곳 — run 문서를 따른 까닭

| 무엇 | 결과서 | run 문서 | 까닭 |
|---|---|---|---|
| `.data` 모름 | 275 → 239 | **274 → 238** | 결과서 숫자는 run105 (`AvrfpLoaderEntry`) 전 기준이라 하나씩 다르다 |
| `RtlpHypervisorSharedUserVa` 쓴 주인 | nt | **ntdll** | run59 규칙 (2.4) |
| `LdrpDebugFlags` | `ShowSnaps` 만 셈 | **짐작 → 정확** | 같은 주소 `0x1ccf60` 의 다른 이름. 이미 3.8 짐작 줄이 있어 새 칸 수는 그대로, 정확 +1 · 짐작 −1 |
| `RtlFailedCriticalDebugAllocations` (`0x1d272c`) | 옆 칸 줄 (목록 밖 자리) | **이름 있는 전역** — 7.3 에 두고 7.5 실마리 | `globals.tsv` 에 이름이 있다. 쓰는 줄 `+0x773` 은 0 을 쓴다 `[정적]`. 오케스트레이터 셈 (0 을 씀 2) 에 없어 안 옮김 — **옮기면 0 을 씀 3 · 모름 267** |
| `LdrpInitOnceLoadAsDataCrits` · `LoadAsDataCrits` 경로 | `… LdrAddLoadAsDataTable +0x37 → LdrpInitMuiCrits` | **그 길이 아님** — 후보 셋 + 그럴듯한 최단 길 `[추정]` | sast/113: 그 길로 오면 반드시 써지는 `LoadAsDataTable*` 세 칸이 사진 0 |
| 잠금 칸 셋 | "쓰는 줄 없음 (잠금)" | 셈 기준대로 7.3 표시 | 그런데 run60 · run69 는 잡고 푸는 락을 3.8 로 옮겼다 — **같은 꼴인데 다르게 셈** (4절 → run116) |

#### `LdrpDebugFlags` 짐작 → 정확, 단계로

1. 짐 지도 3.8 에 `LdrpDebugFlags` 줄이 이미 있었다. 다만 **짐작** 줄이었다.
2. `globals.tsv` 에 `LdrpDebugFlags` 와 `ShowSnaps` 가 **둘 다 `0x1ccf60`** 이다. 같은 칸에 PDB 이름이 둘이다. 전역 수 438 에 둘 다 센다.
3. 이번에 `+0x59c` · `+0x754` `OR [LdrpDebugFlags],…` 줄이 `.asm` 에 있음을 확인했다 (조건부).
4. 3장 규칙 "정확 줄이 하나라도 있으면 정확" (run49) → **정확**.
5. 그래서 2장 `.data` 정확이 37 늘고 (새 36 + 이 하나) 짐작이 1 줄었다.

### 2.15 간접 호출 — 셋 다 안 탐

칸을 쓰는 줄이 **간접 호출 대상 안** 에 있는 곳은 셋이고, 모두 test.exe 에서 안 탄다 `[정적]`.

- `LdrpInitializePerUserWindowsDirectory +0x47` → `kernel32!TermsrvGetWindowsDirectoryW`. 길이 0 을 돌려줘 끝난다. **왜 0 인지** 는 kernel32 안 (D3).
- `RtlInitializeHeapManager +0x67` (`g_RegInfo+0x8` 함수) — `+0x2f JZ` 로 건너뜀.
- `Reevaluate +0xe0 · +0x106` — 구독 포인터 둘이 0 `[관찰]`.

### 2.16 PE_emulator 에게 (run 문서 "PE_emulator 쪽", 짧게)

`../PE_emulator` 코드는 flow-writer 가 안 읽었다. 고칠지는 사용자 결정이다.

- `EnvironmentVersion`: **틀림** — 우리 1, 진짜 3 (2.6).
- `RtlpLowFragHeapGlobalFlags`: 우리 0 은 켜기 앞 값 (2.7).
- `LdrpThunkSignature` · `RtlpTimeout` · `RtlpTimeoutDisable` · 무결성 깃발 둘 · `LdrpPolicyBits`: **맞음**.
- 해시 표 · `LdrpEnclave*` · KVS · `RtlpHypervisorSharedUserVa` (우리 `0xc5` 반환에 달림) · 핫패치 이벤트 · `LdrpStatistics` · `LoadAsDataCrits` · `TermsrvGetWindowsDirectoryW` 흉내: **확인 필요**.
- "파일 처음 값 그대로" 46칸: ntdll `.data` 를 구역 원본 `0x2000` 바이트 + 0 채움으로 올리고 재배치만 맞으면 코드 없이 같다 — **확인 필요**.

### 2.17 디버거가 붙어서 달라지는 점

- 없다 `[추측]`. `ShowSnaps` 는 `NtGlobalFlag & 2` 로만 켜지고, KDNET 은 `NtGlobalFlag` 를 안 바꾼다.
- 다만 **사용자 모드 디버거** 로 띄우면 힙 디버그 깃발이 바뀌어 LFH 길이 달라질 수 있다 `[추측]`.

### 2.18 정리 — 비유로 한 번 더

- 관리실 칠판의 남은 칸 72개를 하나씩 들여다봤다.
  - 36칸은 문 여는 날 **누가 적는지 (혹은 왜 비워 두는지)** 를 코드로 댈 수 있어 "확인 끝" 칸에 적었다.
  - 36칸은 "아무도 안 만짐 / 다른 집 사람이 만짐" 이라 적을 칸이 없어 연필 메모만 했다.
- 칠판 칸 둘은 관리사무소 창구가 값을 줬다. 창구가 **직접** 찍은 칸은 nt, **쪽지로 받아 옮긴** 칸은 ntdll.
- 개업 서류 부록의 수정 횟수 3 = 옮기기 1 + 64비트용 주소로 고쳐 적기 2.
- 창고의 작은 물건 선반 (LFH) 은 생각보다 **일찍** 들어와 있었다.
- 이름표가 칸 크기를 잘못 짐작해 옆 칸 글씨를 자기 것처럼 보인 곳 넷을 바로잡았다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 | — | ntkrnlmp · ntdll pdb 줄에 run114, 근거 로그 줄 (run44 덤프 읽기 로그 셋을 파일 값과 칸마다 견줌) |
| 2.4 `LdrpInitializeInternal` | — | 위 `NtCreateEvent` (call `+0x8e`) 의 칸 주소 `+0x82 LEA [LdrpHotPatchInitCompleteEvent]` — **nt 가 그 전역에 핸들을 직접 쓴다** `[정적]` |
| 2.4 묶음 8 줄 | `EnvironmentVersion` 나머지 +2 는 "모름" | `RtlpWow64ThunkEnvironmentTo64` 안 `RtlSetEnvironmentVar` ×2 `[정적]` |
| 2.4 프로세스 힙 절 | — (LFH 두 칸은 4장 run46 "조건부 힙 전역 8" 줄에만) | **LFH 는 P1a 앞에 이미 켜진다** `[관찰]` + 켜는 사슬 `[정적]` · 임계 구역 디버그 정보 첫 둘 `[정적]` (깃발 뜻 `[추정]`) |
| 2.4 기능 상태 절 | — (묶음 7 힙 뒤 부분은 4장에 "Q78" 로 열림) | 모르던 칸 넷: KVS 상태 칸 · `LdrpEnclaveListLock` · `LdrpEnclaveList` · `RtlpHypervisorSharedUserVa` `[정적]` |
| 4장 | run45 `EnvironmentVersion` +2 `[추측]` · run46 조건부 힙 8 · run65 정적 ① · run113 정적 ③ 열림 | 넷 다 `(해결됨 → run114)` + 새 줄 넷 (해결됨 · 동적 D1 ~ D6 · 정적 ① ~ ⑥ · PE_emulator) |
| 5장 | — | run114 두 표 |

- 한 줄로: **2.4 의 오래된 `[추측]` 하나 (해시 표 쪽) 가 틀린 것으로 바뀌어 `[정적]` 이 됐고, run46 의 "범위 밖" 하나가 사진 `[관찰]` 과 쓰는 줄 `[정적]` 으로 뒤집혔다.**

### 짐 지도 `baggage.md` — 숫자

#### ntdll `.data` (2장 표 줄)

| 가름 | 바퀴 전 | 바퀴 뒤 |
|---|---|---|
| 쓴 자리 아는 것 | 164 (정확 160 · 짐작 4) | **200** (정확 197 · 짐작 3) |
| 그중 nt 가 씀 | 5 | **6** |
| 그중 ntdll 이 씀 | 162 | **197** |
| 모름 | 274 | **238** |

#### ntdll 전역 3구역 합 (`.data` · `.mrdata` · `.00cfg`)

- 아는 것 189 → **225** · 모름 **304 → 268**.
- `.mrdata` 는 그대로 (모름 26) — 가름 넷은 표시만.

#### 7.3 사진 구간별 모름

| 구간 | 바퀴 전 | 바퀴 뒤 |
|---|---|---|
| `≤P1a` | 59 | **32** |
| `P1a~P1b` | 3 | **1** |
| `끝까지 0` | 236 (`.data` 214) | **229** (`.data` 207) |
| `K1 이전` | 2 | 2 |
| 못 읽음 | 4 | 4 |

- `끝까지 0` 이 7 준 것은 0 을 씀 2 + 조건부 5 (조건부 힙 새 4 · `g_pShimEngineModule`).
- 그래서 7.1 "아는 전역 중 끝까지 0" 은 60 → **67**.
- "0 그대로 (사진)" 열 (`.data` 265*) 은 **그대로** 다. 사진 분류는 짐작 크기 기준이라 안 고친다 (2.11).

#### 다시 짚기 — 268 을 읽는 법

- 268 안에는 이번에 **가름이 끝난 36** (아무도 안 씀 22 · 범위 밖 14) 이 들어 있다.
- 이 36 을 따로 셀 열을 둘지는 **사용자 결정 대기** 다 (2.2).
- 다음 Q78g2 바퀴 (run116) 셈까지 같이 놓고 보면, 결과서들의 A/B/C 안 중 무엇을 고르느냐에 따라 7.3 숫자가 크게 달라진다.

#### 3장 줄

- 3.2 새 줄 1 + 고친 줄 1 (`EnvironmentVersion`).
- 3.8 머리 run114 줄 · 새 줄 36 (전역 33줄 — 36칸 · 목록 밖 자리 3) + 고친 줄 1 (`LdrpDebugFlags`).
- 3.9 머리 메모 (`.mrdata` 넷 가름).

### 큰 그림에서

- **Q78e (ntdll 앞쪽) 가 다 끝났다.** 유저 구조체 몫 (run113) 과 전역 몫 (이번) 이 둘 다 닫혔다.
- **Q80 도 닫혔다.** run44 때부터 "못 읽은 칸, 파일 값과 견줘 보자" 로 열려 있던 항목이다. 한 칸 (`LdrpGuardDispatchIcallNoESFptr`) 만 Q203 ③ 으로 남았다.
- 에뮬레이터 쪽에서 보면: 칠판 `≤P1a` 칸 대부분은 **ntdll 파일 바이트를 그대로 올리면 맞는** 칸이고, 코드가 바꿔야 하는 15칸은 어느 줄인지 다 있다.
- 다음 문서 차례는 **Q78g1** (sast/110) 그리고 **Q78g2** (sast/113, **run116**) 다 (plan "다음 항목"). 이번에 미룬 셈 거리 (잠금 칸 · `RtlFailedCriticalDebugAllocations`) 는 run116 에서 같이 맞춘다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** LFH 를 켜는 첫 프로세스 힙 할당이 `LdrpInitializeProcess` 의 어디인가 | `RtlAllocateHeap` 은 공용 부품이라 지도에서 끊김. 사진은 "P1a 앞" 까지만 | 동적 **Q46** (run114 몫): `ntdll!RtlpPerformHeapMaintenance` 입구 첫 도착 `k` |
| **D2** `LdrpInitMuiCrits` 첫 부름이 후보 셋 중 어느 것인가 | 정적 최단 길은 sast/113 이 뺐고, 후보 셋이 남음 | 동적 **Q46**: `ntdll!LdrpInitMuiCrits` 입구 첫 도착 `k` (정적으로 좁히는 길은 **Q203 ①**) |
| **D3** `TermsrvGetWindowsDirectoryW` 가 왜 길이 0 인가 | kernel32 안 (터미널 서버 아님 `[추측]`) | 동적 **Q46** |
| **D4** `EnvironmentVersion` 1 → 2 → 3 단계 | 정적으로 줄 수는 맞췄지만 실제 지나가는 건 안 봄 | 동적 **Q46**: `RtlpWow64ThunkEnvironmentTo64+0x12b` 지날 때마다 |
| D5 · D6 (선택) KVS 평가 호출 · `LdrpPolicyBits` `0x16` 읽기 | 급하지 않음 | 결과서 · run 문서에만 (plan Q46 줄엔 D1 ~ D4 만 적힘) |
| `LdrResSearchResource +0x6e3 ~ +0x727` 조건 (파일 경로로 찾는 갈래) | D2 를 못 하면 정적으로 첫 줄을 좁힐 수 있음. 값은 이미 닫힘 | 정적 **Q203 ①** |
| `LdrpInitializeProcess +0xcc0 · +0xcc8` 조건 (`0x1cc578` 에 쓰는 줄을 건너뛰나) | 조건 뜻은 안 봄 | 정적 **Q203 ②** |
| CFG 도우미 `LdrpGuardDispatchIcallNoESFptr` 의 P1a 값이 파일 값인가 | sast/111 대조 밖 — Q80 을 닫으며 남은 마지막 한 칸 | 정적 **Q203 ③** |
| 같은 주소에 PDB 이름 둘인 전역 짝이 더 있나 (`LdrpDebugFlags` · `ShowSnaps` 처럼) | 한쪽만 3장에 있으면 다른 쪽이 7.3 "모름" 으로 남는다 | 정적 **Q203 ④** (도구 메모) |
| `RtlFailedCriticalDebugAllocations` 를 3.8 로 옮길지 (`+0x773` 0 을 씀) | 오케스트레이터 셈 (0 을 씀 2) 에 없었음 | 다음 **Q78g2** 문서 바퀴 (**run116**) 에서 옮김 |
| 잠금 칸 셋 (`LdrpLoaderLock` · `LdrpDllNotificationLock` · `LdrpModuleDatatableLock`) 을 어떻게 셀지 | run60 · run69 와 같은 꼴인데 이번엔 "아무도 안 씀 (잠금)" 으로 셈 | **run116** 에서 run60 · run69 선례대로 3.8 로 맞춤 (Q78g2 의 잠금 칸 7과 같이, 오케스트레이터 기본값) |
| 전역에 "아무도 안 씀 · 범위 밖" 가름 열을 둘지 | 셈 기준 문제 — 지금은 7.3 에 표시만 | **사용자 결정 대기** (sast/111 · sast/113 A/B/C 안) |
| `끝까지 0` 전역 중 이름 칸 쓰는 줄이 지도 안에 있는 것 (`.data` 39 · `.mrdata` 14) | 이번 범위 밖 (Q78g2 몫) | **Q78g2** (정적 끝, run116 문서) |

- 동적은 모두 결과서 판단 "급하지 않음 — Q104 VM 단계에 얹을 것" 이다. 새 번호 없이 Q46 에 덧붙었다.

---

## 5. 더 읽기

- run 문서: [`run114-ntdll-globals-early.md`](run114-ntdll-globals-early.md) — 요약 표, 호출 지점 표, 1절 (18칸) · 2절 (0 을 씀 · 조건부) · 3절 (7.3 에 남는 36) · 4절 (라벨 정정) · 5절 (목록 밖 자리) · 6절 (`EnvironmentVersion`) · 7절 (LFH), Q80 닫힘, 결과서와 다르게 적은 곳, 정정
- 결과서: [`_workspace/sast/111_ntdll-globals-early_result.md`](../../_workspace/sast/111_ntdll-globals-early_result.md) — 가름 표 (1.1 ~ 1.5), 1a ~ 1k, 질문 2 ~ 5, Q80 에 넘길 판단, 동적으로 확인할 것, 다음 요청 후보
- 요청서: [`_workspace/sast/111_ntdll-globals-early.md`](../../_workspace/sast/111_ntdll-globals-early.md)
- flow.md: [2.4](flow.md) (`LdrpInitializeInternal` · 묶음 8 · 프로세스 힙 절 · 기능 상태 절) · 4장 run114 줄 · 5장 run114
- 짐 지도: [`baggage.md`](baggage.md) 2장 (숫자) · 3.2 · **3.8** · 3.9 · 7.1 · **7.3** (머리 · 가름 표시 · 라벨 정정) · 7.5 · 7.24 · **7.45**
- 정정된 앞 해설: [run45](run45-work-list-해설.md) (2.10 (1) `EnvironmentVersion`) · [run46](run46-process-heap-해설.md) (LFH) — 둘 다 맨 위 "고쳐 읽기 (run114)"
- 같은 셈 방식: [run113](run113-ntdll-user-structs-해설.md) (유저 구조체 몫) · [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법) (네 가지 가름)

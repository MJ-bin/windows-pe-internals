# run118 shimdata-read — 해설

> 이 글은 [run118 문서](run118-shimdata-read.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q90** — ntdll 이 PEB `pShimData` · `SystemDefaultActivationContextData` 를 **읽는** 곳 (`_workspace/plan.md`, 이번에 **끝**)
  - 문맥 쪽 (`+0x2f8` · `+0x308`) 은 run88 에서 이미 닫혔다. 이번엔 **`pShimData` (`+0x2d8`) 읽는 쪽만** 남아 있었다.
  - 사용자 질문 2026-10-27 ("부모가 써 준 PEB 칸을 ntdll 이 쓰나") · 결정 2026-11-15 "남은 정적 분석 전부" 의 차례다. **짐 지도 바퀴**다.
  - 남은 것이 간 곳 (4절): 동적 → **Q91** (라이브 확인, 선택) · **Q46** (안 걸려야 / 걸려야 맞는 멈춤점, 선택) · 정적 → 새 **Q208** ① ~ ③
- 근거:
  - run 문서 [`run118-shimdata-read.md`](run118-shimdata-read.md) (가장 중요)
  - 요청서 [`_workspace/sast/116_shimdata-read.md`](../../_workspace/sast/116_shimdata-read.md) · 결과서 [`116_shimdata-read_result.md`](../../_workspace/sast/116_shimdata-read_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀜.** 글만 — 표시 규칙 · 2.2 · 2.4 · 2.5 · 2.6 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): **2장 숫자 그대로.** 새 **7.6.15** (블록 칸 표, 체크리스트 밖) · 3.1 · 3.5 · 3.8 고친 줄 · 7.9 D1 닫힘 · 새 7.49.
- 이번 바퀴는 **정적 + 사진 덤프 오프라인 읽기** 다. VM 은 안 돌렸다.
  - 그래서 이 글엔 태그가 하나 더 나온다 — **`[덤프 오프라인]`**. 2.1 에서 천천히 푼다.
- 결과서와 run 문서가 다른 곳: `LdrpCheckComponentOnDemandEtwEvent` 오프셋 넷. 이 글은 **run 문서를 따른다** (2.15).
- 앞 바퀴 짝:
  - [run87 해설](run87-sxs-create-해설.md) 2.9 — 부모가 자식 PEB `pShimData` 를 **쓰는** 쪽 (`+0x57d0`)
  - [run88 해설](run88-sxs-redirect-해설.md) — 같은 Q90 의 문맥 쪽, 그리고 사진 덤프 오프라인 읽기를 처음 한 곳 (2.7)
  - [run46 해설](run46-process-heap-해설.md) 2.4 — 세그먼트 힙 조건 ④ 를 "못 봄 (D1)" 으로 남긴 곳 (이번에 닫힘)
  - [run113 해설](run113-ntdll-user-structs-해설.md) 2.3 — 명부 줄 `SwitchBackContext` 를 누가 쓰나

---

### 이미 푼 말 — 링크만

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) · ntdll = 가게 안 **관리실** → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- 부모 cmd.exe = 새 가게를 **신청하는 쪽 가게** → [run14 해설](../02_sprint/run14-create-process-internal-해설.md)
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) = 가게 문 앞 **안내판**, `pShimData` = 안내판에 적힌 **"호환 안내문" 위치** → [run14 해설](../02_sprint/run14-create-process-internal-해설.md) · [run87 해설 2.9](run87-sxs-create-해설.md)
- VAD = 땅의 **구획 장부** → [run10 해설](../02_sprint/run10-allocate-process-해설.md)
- 모듈 = **건물**, 모듈 항목 = 명부의 **한 줄**, 노드 = **연결 카드** → [run113 해설](run113-ntdll-user-structs-해설.md) 첫머리
- `SwitchBackContext` (명부 줄 `+0xc0`) · 호환 문맥 표 `0x173010` · "번호 0 칸 `+4`" 계산 → [run113 해설 2.3](run113-ntdll-user-structs-해설.md)
- `SbDynamicContextDllCount` (`+0x4` 개수 칸, P1a 0 → P1b 3) → [run114 해설](run114-ntdll-globals-early-해설.md)
- NT 힙 / 세그먼트 힙 = 창고 **설계도 두 종류**, 고르는 조건 ① ~ ⑨ → [run46 해설 2.4](run46-process-heap-해설.md)
- apphelp = 호환성 도우미, shim 엔진 `g_ShimsEnabled` · `g_pfnSE_*` → [run66 해설](run66-apphelp-cache-해설.md)
- [ETW](../02_sprint/run12-insert-thread-해설.md#212-그-밖에-확인된-것) = 사건 기록 장치 `[추측]`
- 스냅샷 = 가게를 얼린 **보관 상자**, 덤프 (`memory.dmp`) = WinDbg 가 펼쳐 보는 **앨범**, 사진 지점 K3 · P0 · P1 · P1a · P1b · P2 → [run44 해설 2.2 · 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)
- 가상 → 물리 바꾸기 = **페이지 걷기** → [run59 해설 2.13](run59-init-leftovers-해설.md)
- 물리 연속 가정 (run88 의 오프라인 읽기 방식) → [run88 해설 2.7](run88-sxs-redirect-해설.md)

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **`[덤프 오프라인]`** (새 태그) → 2.1
- **shim (앱 호환 보정)** → 2.2 (run66 해설에서 짧게 나온 말을 한 번 더)
- **SwitchBack** · **ctx (SwitchBack 문맥)** → 2.5
- **게이트** → 2.6
- **COD** → 2.10

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**, PEB = **안내판**, 모듈 = **건물**, 모듈 항목 = **명부 줄**.
- 부모 cmd = **신청하는 쪽 가게**, `pShimData` 가 가리키는 블록 = **호환 안내문** (run14 해설 말).
- **하나만 바꿔 부른다.**
  - run 문서는 이번에 부모를 "본점", 블록을 "호환 메모 봉투", DLL 을 "짐" 이라고 불렀다.
  - 앞 해설에선 이미 **신청하는 쪽 가게 · 호환 안내문 · 건물** 이라 이 글은 그 말을 쓴다. 뜻은 같다. ("짐" 은 이 프로젝트에서 짐 지도의 **개업 준비물** 이라 DLL 에 안 쓴다.)
- run 문서 그림을 말만 바꿔서 가져오면:
  - 신청하는 쪽 가게가 새 가게 문을 열기 전에 **호환 안내문** 한 장을 넣어 준다. 칸마다 "이 가게는 어느 시절 규칙으로 돌아가나" 같은 메모가 있다.
  - 관리실 (ntdll) 은 이 안내문을 **여섯 군데서** 들여다본다. 그런데 test.exe 안내문은 거의 비어 있다 (8,192 바이트 중 74 바이트만 글씨).
  - 그래서 "특별 보정 엔진 켜기 (shim)", "기록 남기기 (ETW)", "새 설계도 창고 (세그먼트 힙)" 는 모두 "해당 없음" 으로 지나간다.
  - **딱 하나** — 건물이 들어설 때마다 명부 줄에 "이 건물은 몇 년식 규칙인가" **연식 꼬리표** 를 붙이고, 그 연식 칸 개수를 하나 센다. 안내문이 아예 없으면 이 꼬리표 붙이기를 통째로 건너뛴다.

### 한 줄로 먼저 — 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| ntdll 이 PEB `pShimData` 를 읽는 줄 | **9곳 · 함수 6** (`.text` 의 `[reg+0x2d8]` 28줄 중) `[정적]` |
| 블록 크기 · 내용 | `0x11c0`B (VAD 2쪽). 2쪽 `0x2000`B 중 0 아닌 바이트 **74** `[덤프 오프라인]` |
| 블록을 만드는 쪽 | 부모 kernel32 `BasepGetAppCompatData` (+ `SbpCreateSwitchContext` 가 ctx = 블록 `+0x7e0` 을 채움) `[정적]` |
| test.exe 에서 **타는** 갈래 | **`SbUpdateSwitchContextBasedOnDll` 하나** (모듈 `+0xc0` · `SbDynamicContextDllCount +4`) `[정적]` + `[덤프 오프라인]` |
| 안 타는 갈래 | shim 엔진 (`+0x210` = 0) · ETW (`[ctx+0xc]` = 0) · 세그먼트 힙 (`+0x11b2` = 0) · `SbSelectProcedure` · COD |
| 블록이 바뀌나 | P0 ~ P2 **한 바이트도 안 바뀜** `[덤프 오프라인]` — ntdll 은 초기화 동안 블록에 안 쓴다 |
| run46 D1 | `+0x11aa` · `+0x11b2` **둘 다 0 → NT 힙**. 닫힘 (라이브 확인은 선택) |
| 태그 올림 | `AppCompatInfo` 줄의 `R13` = "시작 때 모은 앱 호환 블록" `[추측]` → `[정적]` (`pShimData` 지역 사본) |
| 그림 · 짐 지도 2장 | **둘 다 그대로.** 블록 칸 표는 새 7.6.15 (체크리스트 밖) |
| PE_emulator | `pShimData` = 0 으로 두면 **`SbUpdateSwitchContextBasedOnDll` 만 다름** — 확인 필요 |

이 글에서 붙잡아 둘 세 줄:

> **(1) 관리실은 호환 안내문을 여섯 군데서 보지만, test.exe 에선 거의 다 "해당 없음" 이다.**
> 안내문에 글씨가 74 바이트뿐이고, 관리실이 보는 칸은 대부분 0 이다.
>
> **(2) 타는 건 하나 — 건물마다 연식 꼬리표 붙이기.**
> 지금 연식이 이미 가장 낮은 칸 (번호 0, 6.0) 이라, 꼬리표만 붙이고 안내문 자체는 안 고친다. 사진 다섯 장에서 안내문 바이트가 똑같은 것과 맞는다.
>
> **(3) 사진을 "확대경으로" 다시 봤다.**
> 디버거로 그 순간 본 게 아니라, 찍어 둔 사진 파일을 서버에서 우리 스크립트로 읽었다. 그래서 태그가 `[관찰]` 이 아니라 `[덤프 오프라인]` 이다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 **두 시간대** 를 이어 봤다. 안내문을 **만들어 넣는** 부모 쪽 (이미 run87 에서 본 곳) 과, 그걸 **읽는** test.exe 의 ntdll 쪽 (이번).

```
cmd.exe 창에서 "test.exe" 입력
 └─ KERNELBASE!CreateProcessInternalW  (신청하는 쪽 가게)
     ├─ +0x30ee  NtCreateUserProcess                (관리사무소가 가게 · 안내판 만들기 — run9 ~)
     ├─ +0x56c2  kernel32!BasepGetAppCompatData     안내문 0x11c0B 만들기      ← 이번 (대조)
     │            └─ … SbpCreateSwitchContext       ctx = 블록 +0x7e0 채우기  ← 이번 (대조)
     └─ +0x5755 / +0x5795 / +0x57d7  자식 땅에 2쪽 받기 · 안내문 복사 · PEB +0x2d8  (run87)
 ─ ─ ─
 test.exe 첫 스레드 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
   ├─ +0x69f   PEB +0x2d8 읽어 지역 칸에 복사                               ← 이번
   ├─ +0x9f2   RtlInitializeHeapManager → RtlpHpOptIntoSegmentHeap +0x184   ← 이번 (세그먼트 힙?)
   ├─ +0x19cd  SbObtainTraceHandle +0x22                                     ← 이번 (ETW?)
   ├─ +0x19dc  블록 +0x210 확인 → LdrpInitShimEngine (안 탐)                 ← 이번 (shim?)
   └─ +0x1af2  LdrpPrepareModuleForExecution → … 모듈마다
                └─ SbUpdateSwitchContextBasedOnDll → SbGetCurrentSwitchContext +0x9   ← 이번 (유일하게 탐)
   (DLL 열기가 접근 거부일 때만: LdrpMapDllNtFileName → LdrpCheckComponentOnDemandEtwEvent +0x6c)  ← 이번
 → NtContinue 은퇴 → (CRT) → main
```

- callee 앞 `+0x..` 는 **ret 자리**, 그냥 `+0x..` 는 읽는 줄 자리다 (run 문서 "호출 관계" 그대로).
- 지도 범위로는 전부 **이미 있는 대표 지점 안쪽** 이다 — `LdrpInitializeProcess` · `RtlInitializeHeapManager` 상자 `p2h` · 2.5 알림 줄. 그래서 그림은 안 바뀌었다.

### 무엇이 궁금했나

- 부모가 써 준 PEB `pShimData` (`+0x2d8`) 를 ntdll 이 **어디서 읽고**, 읽은 블록 안 **어느 칸을 보고 무엇을 하나**.
- test.exe 블록 값으로 **어느 갈래가 타고 어느 갈래가 안 타나**.
- 덤으로: run46 에서 못 본 세그먼트 힙 조건 ④ (D1 — 블록 `+0x11aa` · `+0x11b2`) 값.

### 바퀴 전에 알던 것

- `[정적]` (run87) 부모 kernelbase `CreateProcessInternalW` 가 자식에 2쪽을 받고 (`+0x574e NtAllocateVirtualMemory`) 블록을 복사하고 (`+0x578e`) PEB `+0x2d8` 에 주소를 쓴다 (`+0x57d0`).
- `[관찰]` (run44 사진) PEB `+0x2d8` = `0x25996d10000` (`_P0.txt:1635`), 그 자리 VAD `25996d10 ~ 25996d11` Private READWRITE 2쪽 (`_P0.txt:1381`). K3 엔 아직 null.
- `[정적]` (run88) ntdll 이 `pShimData` 를 읽는 곳은 **후보 이름만** 있었다 `[공백]` — `LdrpInitializeProcess +0x69f`, `LdrpCheckComponentOnDemandEtwEvent +0x6c`, `RtlpHpOptIntoSegmentHeap +0x184`, `SbSelectProcedure` 넷, `SbObtainTraceHandle +0x22`.
- `[정적]` (run46) 세그먼트 힙 조건 ④ 가 `pShimData` 안 두 바이트인 건 알았지만 **값은 못 봤다** → D1 (Q91 동적).
- `[정적]` (run113) 명부 줄 `+0xc0` `SwitchBackContext` 를 `SbUpdateSwitchContextBasedOnDll +0xd9` 가 쓴다. 그리고 `AppCompatInfo` 를 0 으로 쓰는 `+0x19e5` 앞 `R13` 은 "시작 때 모은 앱 호환 블록" 으로 보였다 `[추측]`.

---

## 2. 찾은 것

### 2.1 새 태그 `[덤프 오프라인]` — 사진을 확대경으로 다시 본 것

먼저 이걸 짚고 가야 뒤 표들이 읽힌다. 이번 바퀴의 블록 값은 거의 다 이 태그다.

#### 한 줄 정의

- **`[덤프 오프라인]`** = run44 때 찍어 둔 사진 (`memory.dmp`) 을 **서버에서 우리 스크립트로 읽은 바이트**.
- 비유: 사진을 찍어 둔 뒤, **나중에 확대경으로** 사진 한 구석을 들여다본 것.

#### `[관찰]` 과 무엇이 다른가

| | `[관찰]` | `[덤프 오프라인]` |
|---|---|---|
| 무엇을 보나 | 그 순간의 메모리 값 | **같다** — 그 순간의 메모리 값 |
| 누가 읽나 | **디버거** (WinDbg) 가 읽어 줌 | **우리 스크립트** 가 파일을 읽음 |
| 주소 바꾸기 (가상 → 물리) | 디버거가 함 | **우리 스크립트가 함** |
| 그래서 | 디버거를 믿으면 됨 | 스크립트 번역이 맞는지 **따로 확인** 해야 함 |

- 비유로 다시: `[관찰]` 은 현장에서 직접 눈으로 본 것, `[덤프 오프라인]` 은 그때 찍은 사진을 **우리가 만든 확대경** 으로 본 것. 사진 자체는 같은 순간이지만, 확대경이 엉뚱한 자리를 비추면 틀린다.
- 그래서 flow.md 표시 규칙에 "섞지 않는다" 와 "번역은 같은 사진의 디버거 줄 몇 개와 맞춰 검증한 뒤에만 쓴다" 가 같이 들어갔다.

#### 어떻게 읽었나 — 한 줄씩

1. **파일의 물리 쪽**: `memory.dmp` 는 VMware 사진을 `vmss2core` 로 바꾼 **PAGEDU64 풀 덤프** 다. 머리 `+0x88` 에 물리 구역 2개 (`0 ~ 0xc0000000`, `0x100000000 ~ +0x140000000`) 가 적혀 있고, 자료는 `+0x2000` 부터다.
   - 즉 이 파일은 **메모리 칩 위 자리 (물리 주소)** 순서로 되어 있다.
2. **test.exe 의 페이지 표 시작 주소**: test.exe `DirectoryTableBase` = `0x192c2e000` (`04_baggage-read_P0.txt:333`, 디버거가 이미 적어 둔 줄).
3. **페이지 걷기**: 그 시작 주소에서 4단 페이지 표를 따라 내려가, test.exe 가상 주소 `0x25996d10000` 이 칩 위 어디인지 찾았다 ([run59 해설 2.13](run59-init-leftovers-해설.md) 의 "단지 안내도 → 동 → 층 → 호실").
4. **검증**: 같은 방법으로 번역한 PEB 칸 — `+0x10` (`0x7ff61a370000`) · `+0x2d8` (`0x25996d10000`) · `+0x308` (`0x25996d00000`) · `+0x2e0` · `+0x2f8` (0) — 이 `04_baggage-read_P0.txt` · `_P1a.txt` **디버거 줄과 같았다**. 그래서 번역을 믿는다.
5. **읽은 것**: 블록 2쪽 (`0x2000`B) 을 사진 다섯 장 (P0 · P1 · P1a · P1b · P2) 에서 뽑았다. 두 쪽 다 물리 메모리에 있었다.

- 함정 하나 (run 문서 "디버거 메모"): **P2 덤프는 머리의 기본 `DirectoryTableBase` 가 test.exe 가 아니라 다른 프로세스 것** 이다. 그래서 P2 는 test.exe 값을 직접 줘서 읽었다.
- 도구 (`scratchpad/s116/vread.py`) 와 블록 파일 (`blk_*.bin`) 은 **스크래치에만** 있다 (커밋 안 됨, 지워질 수 있음). 레포에 둘지는 사용자 결정.

#### run88 과 다른 점

- run88 도 사진을 오프라인으로 읽었지만, 그땐 **가상 → 물리 번역 없이** "바로 다음 물리 쪽이 진짜 다음 쪽" 이라고 믿고 읽었다 (물리 연속 가정, [run88 해설 2.7](run88-sxs-redirect-해설.md)).
  - 그래서 "어느 가게 것인지" 를 못 짝지었고, 태그도 `[관찰]` + "test.exe 블롭인지는 `[확인 필요]`" 였다 (오케스트레이터 지시).
- 이번엔 **test.exe 의 페이지 표로 번역** 했다. 그래서 "test.exe 의 `0x25996d10000` 에 있던 바이트" 라고 말할 수 있다.
- run88 쪽 표시는 **그 판 그대로 둔다** (run 문서 "정정"). 이번 판부터 같은 종류는 `[덤프 오프라인]` 이다.

### 2.2 한눈에 — 관리실이 안내문을 들여다보는 여섯 곳

**새 말 — shim (앱 호환 보정)**: 옛 프로그램이 새 Windows 에서도 돌게 **중간에 끼워 넣는 보정 장치** 다 `[추측]` (일반적으로 알려진 설명). 비유: 오래된 가전에 붙이는 **변환 어댑터** ([run66 해설](run66-apphelp-cache-해설.md) 의 비유 그대로). `pShimData` 는 이름 그대로 "shim 쪽 자료" 자리다.

run 문서 "1. 읽는 곳 전수" 표를 줄인 것이다. 사실은 전부 `[정적]`, test.exe 값은 `[덤프 오프라인]`.

| # | 읽는 줄 | 안내문의 어느 칸을 보나 | 무엇을 위해 | test.exe |
|---|---|---|---|---|
| 1 | `LdrpInitializeProcess +0x69f` | (주소만 복사) → `+0x19dc` 에서 `+0x210` | shim 엔진을 켤지 | `+0x210` = 0 → **안 탐** |
| 2 | `SbObtainTraceHandle +0x22` | ctx `+0xc` | ETW 기록을 켤지 | 0 → **안 탐** (0 반환) |
| 3 | `SbGetCurrentSwitchContext +0x9` | ctx `+0x30` | ctx 주소를 돌려줌 (← `SbUpdateSwitchContextBasedOnDll`) | ctx 있음 → **탐** |
| 4 | `SbSelectProcedure +0x5f` · `+0x8f` · `+0x140` · `+0x199` | ctx | OS 판별 함수 고르기 | **안 부름** |
| 5 | `RtlpHpOptIntoSegmentHeap +0x184` | `+0x11b2` 비트 `0x4` · `+0x11aa` 비트 `0x40` | 세그먼트 힙을 켤지 | 0 → **안 탐** (NT 힙) |
| 6 | `LdrpCheckComponentOnDemandEtwEvent +0x6c` | `+0x1178` (씀) | DLL 열기 접근 거부 때 알림 | **안 탐으로 보임** `[추측]` |

- 줄 수: 1 + 1 + 1 + 4 + 1 + 1 = **9줄**, 함수 **6개**.
- run 문서 표는 7행이다. 2행 (`+0x19cd ~ +0x19e3`) 은 PEB 를 다시 읽는 게 아니라 1행이 **복사해 둔 지역 칸을 다시 꺼내 쓰는 줄** 이라 9줄에 안 센다. 이 글 표는 그 둘을 1번 한 줄로 묶었다.

### 2.3 어떻게 "빠짐없이" 셌나 — 28줄에서 9줄로

- 결과서는 ntdll `.text` **전체** 를 `objdump` 로 풀어, `[reg+0x2d8]` 꼴을 다 찾았다 → **28줄** `[정적]`.
- 그다음 줄마다 **앞줄** 을 봤다: 그 레지스터가 PEB 에서 왔나 (`GS:[0x60]` 이나 PEB 를 든 `RDI` 등).
  - PEB 에서 온 것 → **9줄**.
  - 아닌 것 → **19줄**: 스택 `[rsp+0x2d8]` 8줄 (`LdrpInitializeProcess +0x1349 · +0x1a67 · +0x1ade · +0x1ba7 · +0x1bcc` 등) · 다른 구조체 11줄 (활성화 문맥 스택 프레임 · 스택 추적 DB · Xpress 풀기 · 스냅샷 복사).
- 함수 이름은 `_index.tsv` 로 붙였다.
- 비유: 가게 장부 전체에서 "안내판 7번 칸" 이라는 글자를 다 찾은 뒤, 앞뒤를 읽어 **진짜 안내판 얘기인 것** 만 남겼다. 같은 번호라도 "창고 7번 선반" 얘기는 버렸다.
- 범위 밖 (이름만): kernelbase `CreateFileInternal +0x61f` 도 `pShimData` 를 읽는다. 초기화 동안 불리는지는 모른다 (동적, 4절).

### 2.4 한 줄 읽기 — `+0x19dc CMP [R13+0x210],0`

- **누가**: test.exe 첫 스레드의 `ntdll!LdrpInitializeProcess` 가
- **어디서**: `+0x19dc` 에서
- **누구를**: `R13` 이 가리키는 블록 — 바로 앞 `+0x19cd MOV R13,[RSP+0x180]` 으로 꺼낸 값이고, 그 칸은 `+0x69f` 에서 PEB `+0x2d8` 을 복사해 둔 자리다
- **무엇을 위해**: 블록 `+0x210` 이 0 이 아니면 shim 엔진을 올리려고 (`+0x19ef LdrpInitShimEngine`). 0 이면 `+0x19e3 JZ +0x19f4` 로 건너뛴다.
- test.exe: `+0x210` = 0 `[덤프 오프라인]` → 건너뜀.

### 2.5 안내문은 누가 어떻게 만드나 — 부모 쪽 (대조)

**새 말 — SwitchBack**: 함수 이름 머리 `Sb` 의 뜻이다. **DLL 마다 "어느 Windows 판처럼 굴지" 를 고르는 장치** 로 보인다 `[추측: 이름과 코드에서 읽음]` — 근거: 판 목록 `SbSupportedOsList` (6.0 · 6.1 · 6.2 · 6.3 · 10.0), `SbSelectProcedure` 가 "OS 버전별 함수 고르기", `SbpDetermineDllContext` 가 DLL PE 머리의 OS 버전 + 호환 매니페스트를 본다 `[정적]`. 비유: 건물마다 붙이는 **연식 꼬리표** ("이 건물은 몇 년식 규칙대로").

**새 말 — ctx (SwitchBack 문맥)**: 블록 `+0x7e0` 부터의 칸 묶음이다 `[정적]`. run 문서는 줄여서 "ctx" 라고 쓴다. 비유: 호환 안내문 안의 **"연식 규칙" 칸 묶음**.

부모 쪽 사슬 `[정적]` (run 문서 "호출 관계" 위쪽):

```
kernelbase!CreateProcessInternalW
 ├─ +0x56c2  kernel32!BasepGetAppCompatData     +0x31 크기 0x11c0 · +0x8f RtlAllocateHeap
 │            └─ +0x266  SbPrepareSwitchContext
 │                        └─ +0xc5  SbpCreateSwitchContext    ctx = 블록 +0x7e0 채움
 └─ +0x5755 NtAllocateVirtualMemory (자식) → +0x5795 NtWriteVirtualMemory (블록) → +0x57d7 (PEB +0x2d8)   (run87)
```

- `0x11c0` 을 쪽으로 올리면 2쪽 — 사진 VAD 2쪽과 맞는다.
- 비유: 신청하는 쪽 가게가 **자기 창고** (부모 힙) 에서 안내문을 쓰고, 새 가게 땅에 두 칸을 빌려 **베껴 넣고**, 안내판에 "안내문은 여기" 를 적는다.

#### test.exe 안내문에 실제로 적힌 것 `[덤프 오프라인]`

2쪽 `0x2000`B 중 0 아닌 바이트 **74개**. 대표만 (전체 표는 run 문서 "2." · 짐 지도 7.6.15):

| 블록 오프셋 | 뜻 | 값 |
|---|---|---|
| `+0x208` | 블록 크기 `[추측]` | `0x11c0` |
| `+0x20c` | 머리표 | `0xac0dedab` |
| **`+0x210`** | **shim 엔진 켤지** | **`0`** |
| `+0x214` | 이미지 `Machine` `[추측]` | `0x8664` |
| ctx `+0x0` (`+0x7e0`) | 바뀜 번호 | `1` |
| ctx `+0x8` (`+0x7e8`) | 켜짐 | `1` |
| ctx `+0xc` (`+0x7ec`) | **ETW 켜짐** | **`0`** |
| ctx `+0x28` (`+0x808`) | DLL 버전과 견주는 기준 | `0` |
| ctx `+0x30` (`+0x810`) | 처음 정한 호환 문맥 GUID | `{e2011457-1546-43c5-a5fe-008deee3d3f0}` = 표 번호 0 (6.0) |
| ctx `+0x40` (`+0x820`) | 지금 문맥 GUID | 위와 같음 |
| `+0xb38` | 부모 PID | `0x19a8` = test.exe `InheritedFromUniqueProcessId` (`_P0.txt:154` `[관찰]`) |
| `+0xb3c ~` | 부모 이미지 경로 | `C:\WINDOWS\system32\cmd.exe` |
| `+0x1144` | 이미지 파일 크기 `[추측]` | `0x86a00` = 551,424 = `sample/bin/test.exe` 크기 `[소스]` |
| **`+0x11aa`** 비트 `0x40` | **세그먼트 힙 켜라** | **`0`** |
| **`+0x11b2`** 비트 `0x4` | **위 값이 정해졌나** | **`0`** |

- 표 번호 0 (6.0) 은 일반적으로 Windows Vista 무렵의 판 번호로 알려져 있다 `[추측]` (일반적으로 알려진 설명). run 문서는 이 칸을 "매니페스트 없는 exe 의 기본 칸" 으로 읽었다 `[추측: 뜻]`.
- 굵은 줄이 관리실이 **갈림을 정할 때 보는** 칸이다. 거의 다 0 이다.
- 부모 PID · 경로 · 파일 크기는 test.exe 와 맞아서, "이 블록이 정말 test.exe 몫" 이라는 확인도 된다.

### 2.6 갈래 하나 — shim 엔진: `R13` 은 `pShimData` 사본이었다

**새 말 — 게이트**: 그 길로 들어가기 전에 서 있는 **문지기 조건** 이다. "이 칸이 0 이 아니어야 들어간다" 같은 것.

#### 무엇을 찾았나 — 단계별로 `[정적]`

1. `+0x5d9` · `+0x5db` — 지역 칸 `[RSP+0x180]` 을 미리 0 으로.
2. `+0x696 CMP UseWOW64` · `+0x69d JNZ +0x6ae` — WOW64 면 아래를 건너뛴다.
3. `+0x69f` — PEB `+0x2d8` 을 읽어, `+0x6a6` 에서 `[RSP+0x180]` 에 복사.
4. (한참 뒤) `+0x19cd MOV R13,[RSP+0x180]` — 그 사본을 꺼낸다.
5. `+0x19d7 TEST` · `+0x19da JZ` — 사본이 0 이면 건너뜀.
6. **`+0x19dc CMP [R13+0x210],0`** · `+0x19e3 JZ +0x19f4` — **블록 `+0x210` 이 게이트.**
7. 통과하면 `+0x19e5` PEB `AppCompatInfo` (`+0x2e0`) = 0, `+0x19ef LdrpInitShimEngine(블록)`.
- 같은 `R13` 이 뒤 `+0x1cd4 MOV RDX,R13` 으로 `g_pfnSE_InstallAfterInit` 인자가 된다 — `+0x1ca7` `g_ShimsEnabled` 가 켜졌을 때만 (`+0x1cae JZ +0x1d07`).

#### test.exe

- `+0x210` = 0 `[덤프 오프라인]` → `+0x19e3` 에서 `+0x19f4` 로. `LdrpInitShimEngine` 안 부름.
- PEB `AppCompatInfo` 는 0 그대로 (`_P0.txt:1636` `(null)` `[관찰]`). `g_ShimsEnabled` 0 → `SE_InstallAfterInit` 도 안 부름.
- 비유: "변환 어댑터가 필요하면 여기 표시" 칸이 비어 있어서, 관리실은 어댑터 상자를 아예 안 연다.

#### 그래서 태그가 올랐다

- run113 의 짐작 "`R13` = 시작 때 모은 앱 호환 블록 `[추측]`" → **맞았다. `[정적]`** (`+0x69f` · `+0x6a6` · `+0x19cd`).
- 틀린 게 아니라 근거가 생긴 것이다 (run 문서 "정정").
- 그리고 run66 정적 ② ("shim 엔진 올리는 조건") 의 **첫 단추** 가 닫혔다 — 첫 게이트는 `+0x210`. 그 안 두 함수 (`LdrpLoadShimEngine` · `LdrpGetShimEngineInterface`) 의 조건은 남는다.

### 2.7 갈래 둘 — ETW: `SbObtainTraceHandle` 은 0 을 돌려준다

- `LdrpInitializeProcess +0x19c8` 이 늘 부른다 (인자 0) `[정적]`.
- 안에서 `[정적]`:
  - `+0xf` `GS:[0x60]` → `+0x22` PEB `+0x2d8` → `+0x2e ADD RBX,0x7e0` (ctx) → `+0x37` `[ctx+0x30]` 확인.
  - `+0x44` · `+0x47 JNZ +0x5c` — **`[ctx+0xc]` (ETW 켜짐) 가 0 이 아니면** `MS_Windows_AeSwitchBack_Provider` 를 `EtwNotificationRegister` 로 등록하고 핸들을 `[ctx+0x10]` 에 쓴다 (`+0xad LOCK CMPXCHG`).
  - 아니면 `+0x49` 에서 0 반환.
- test.exe: `[ctx+0xc]` = 0 `[덤프 오프라인]` → **0 반환. 쓰기도 시스템 콜도 0.**
- 비유: "연식 규칙 바뀔 때마다 기록부에 적기" 칸이 꺼져 있어서, 기록부를 안 편다.

### 2.8 세그먼트 힙 — run46 D1 이 닫혔다

[run46 해설 2.4](run46-process-heap-해설.md) 에서 "넷째 줄 칸이 흐려서 못 읽었다" 고 했던 그 칸이다.

#### 코드 `[정적]`

- `RtlInitializeHeapManager +0x28` (ret `+0x2d`) 가 `RtlpHpOptIntoSegmentHeap` 을 늘 부른다.
- 안에서:
  - `+0x66` PEB → `+0x184` PEB `+0x2d8` → `+0x18b TEST` · `+0x18e JZ +0x1aa` (블록 없으면 ⑤ 로).
  - `+0x190 TEST [+0x11b2],4` · `+0x197 JZ +0x1aa` — **비트 4 가 없으면** `+0x11aa` 는 보지도 않고 ⑤ (`RtlGetSuiteMask`) 로.
  - 비트 4 가 있으면 `+0x199 TEST [+0x11aa],0x40` · `+0x1a0 JZ +0x281` — `0x40` 없으면 0 반환 (NT 힙), 있으면 `+0x1a6` · `+0x1b5` 1 반환 (세그먼트 힙).

#### 왜 "닫혔다" 고 하나 — 단계별로

1. 근거 (사진): `+0x11b2` = 0 · `+0x11aa` = 0 `[덤프 오프라인]`.
2. 그래서: `+0x197` 에서 ④ 를 그냥 지나 ⑤ 로 간다. ④ 에서 **멈추지 않았다**.
3. ⑤ ~ ⑨ 는 run46 에서 이미 test.exe 값으로 모두 "아님" 이었다 → 끝까지 가서 0 = **NT 힙**.
4. 결과는 run46 사진과 같다: 힙 `+0x10` = `0xffeeffee` `[관찰]` (run44 P1a).
- 그래서 run46 의 남은 물음 "**어느 줄에서** 세그먼트 힙을 안 골랐나" 의 답 = **④ 에서 멈춘 게 아니라 끝까지 내려갔다**.
- 덤: 블록이 아예 0 (null) 이어도 `+0x18e JZ +0x1aa` 로 같은 ⑤ 로 간다.
- 라이브로 한 번 더 볼지는 **선택** 이다 (Q91, `RtlpHpOptIntoSegmentHeap+0x190`).
- 비유로 이어서: 설계도 고르는 체크리스트 넷째 줄이 흐렸는데, 사진을 확대경으로 보니 **빈칸** 이었다. 그래서 넷째 줄에서 멈추지 않고 끝까지 내려가 "옛날식 설계도" 로 정해진 것이다.

### 2.9 `SbSelectProcedure` — 부르는 길 자체를 안 탄다

- 초기화 범위 안에서 부르는 곳은 `RtlInitializeHeapManager +0x56` (ret `+0x5b`) **하나** `[정적]`.
- 그 앞 `+0x2f JZ +0x6c` — 바로 앞 함수 (`RtlpHpOptIntoSegmentHeap`) 가 0 을 돌려주면 건너뛴다.
- test.exe 는 2.8 에서 0 (NT 힙) → **안 부름**.
- 이 함수가 하는 일 `[정적]`: `[ctx+0x30]` 이 없으면 `RtlGetVersion` + `SbSupportedOsList` 로, 있으면 `[ctx+0]` 를 캐시와 견줘 OS 판에 맞는 함수를 고른다.

### 2.10 COD — DLL 열기가 "접근 거부" 일 때만

**새 말 — COD**: 함수 이름 `LdrpCheckComponentOnDemandEtwEvent` 의 `ComponentOnDemand` 를 run 문서가 줄여 부른 말이다. 뜻은 근거 문서가 안 적었다 — 이름만 보면 "필요할 때 구성 요소를 받아 오는" 쪽으로 읽힌다 `[추측: 이름에서]`.

- 부르는 곳은 `LdrpMapDllNtFileName +0x4d7` (ret `+0x4dc`) 하나 `[정적]`.
  - 앞 `+0x4c0 CMP EAX,0xc0000022` (접근 거부) 이고 `R13D` 가 0 일 때만.
- 안에서 `[정적]`: `+0x56 CompatCachepLookupCdb` 가 0 이면 끝 → `+0x6c` PEB `+0x2d8` → `+0x73` · `+0x76` (0 이면 끝) → `+0x7c LdrpIsCODServiceEnabled` → `+0x9d NtCreateEvent` → **`+0xce` 블록 `+0x1178` 에 이벤트 핸들을 씀** → `+0x126 EtwEventWriteNoRegistration` → `+0x141 NtWaitForSingleObject` (10초) → `+0x14f NtClose` → `+0x154` `+0x1178` = 0 으로 되돌림.
- 관리실이 **안내문에 직접 쓰는** 몇 안 되는 줄이다. 그런데 잠깐 쓰고 다시 0 으로 지운다.
- test.exe: **안 탐으로 보임** — DLL 열기가 접근 거부로 끝나지 않았다고 보기 때문 `[추측]`. 사진 `+0x1178` = 0 `[덤프 오프라인]` 과는 맞지만, 쓰고 지우면 사진엔 안 남으니 이것만으론 못 가린다. → 동적 (4절).

### 2.11 유일하게 타는 것 — `SbUpdateSwitchContextBasedOnDll` (건물마다 연식 꼬리표)

#### 어디서 불리나 `[정적]`

```
LdrpInitializeProcess +0x1af2  LdrpPrepareModuleForExecution
 └─ +0x7b  LdrpNotifyLoadOfGraph
     └─ +0x25  LdrpSendPostSnapNotifications   (모듈마다)
         └─ +0xa8  SbUpdateSwitchContextBasedOnDll
              ├─ +0x19  SbGetCurrentSwitchContext   +0x9 PEB +0x2d8 → ctx
              └─ +0xc6  SbpDetermineDllContext      → +0xd9 모듈 +0xc0 · +0xfb 개수 +1
```

#### 안에서 — 단계별로 `[정적]`

1. `+0x14` — `SbGetCurrentSwitchContext` 로 ctx 를 받는다.
   - 그 함수는 `+0x9` PEB `+0x2d8` → `+0x15 ADD RAX,0x7e0` → `+0x1d` `[ctx+0x30]` 이 0 이면 0 (`+0x25`).
2. `+0x32 JZ +0x39` — ctx 가 0 이면 **여기서 끝**. **`pShimData` 가 0 이면 여기서 끝난다.**
3. `+0x47 CMP [ctx+8],0` · `+0x4a JZ +0x2ec` — "켜짐" 이 0 이면 아무것도 안 하고 끝. test.exe `[ctx+8]` = 1 `[덤프 오프라인]` → 계속.
4. `+0xc1 SbpDetermineDllContext(DllBase)` — 이 DLL 의 PE 머리 OS 버전 + `SbpRetrieveCompatibilityManifest` 로 "어느 판용 DLL 인가" 를 표 칸으로 돌려준다.
5. `+0xd5 LEA RDI,[RDX+4]` · **`+0xd9 MOV [R14+0xc0],RDI`** — 명부 줄 `SwitchBackContext` 에 그 표 칸 `+4` 주소를 놓는다.
6. **`+0xfb INC.LOCK SbDynamicContextDllCount+4 + 번호×8`** — 그 판 칸의 개수 +1.

#### 한 줄 읽기 — `+0xd9 MOV [R14+0xc0],RDI`

- **누가**: `SbUpdateSwitchContextBasedOnDll` 이, 모듈 알림 때마다
- **어디서**: `+0xd9` 에서
- **누구를**: `R14` = 지금 알리는 모듈의 명부 줄. 그 `+0xc0` 칸 (`SwitchBackContext`)
- **무엇을 위해**: "이 건물은 표의 몇 번 판 규칙" 이라는 꼬리표 (표 칸 `+4` 주소) 를 붙이려고
- 사진 `[관찰]` (run113 · run44): P1a null → P1b exe `0x7ffafb7d3014` (= 표 번호 0 칸 `+4`).

#### 그런데 왜 안내문은 안 바뀌나 — 단계별로

ctx 를 **바꾸는** 갈래도 이 함수 안에 있다 `[정적]`. 거기선 안내문에 쓴다 (`+0x1f6 INC [ctx]` · `+0x202` 새 GUID → `[ctx+0x40]` · `+0x2a0 INC [ctx]`).

1. DLL 표 칸이 번호 4 (10.0) 면 `+0x10f ~ +0x126` 에서 바로 `+0x12c` (끝).
2. 아니면 `+0x1c6 MOV EAX,[R15]` · `+0x1c9 CMP [RDX],EAX` · `+0x1cb JGE +0x12c` — **DLL 칸 번호가 지금 칸 번호 이상이면 안 바꾼다.**
3. test.exe 의 지금 칸 = **번호 0** (6.0, 가장 낮은 칸) `[덤프 오프라인]`.
4. 그래서: 어떤 DLL 도 0 보다 낮을 수 없다 → 바꾸는 갈래 (`+0x1ea SbpContextLock` 뒤) 는 **안 탄다**.
5. 맞춰 보기: 블록 바이트가 P0 ~ P2 **다섯 장 같다** `[덤프 오프라인]` — 바꾸는 갈래가 안 탄 것과 맞는다.

- 그리고 이 갈래가 **돈 흔적** 은 이미 사진에 있었다 `[관찰]` (run44): 명부 줄 `+0xc0` (run113) · `SbDynamicContextDllCount` 번호 0 칸 개수 P1a 0 → P1b · P2 3 (run114).
- 비유: 가게 기본 연식이 이미 가장 오래된 "6.0년식" 이라, 새 건물이 들어올 때 꼬리표만 붙이고 "가게 연식을 더 옛날로 바꾸기" 는 일어날 수가 없다.

### 2.12 사진 다섯 장이 같다는 것의 뜻 — 그리고 한계

- 정적으로 ntdll 이 블록에 **쓰는 칸** 은 넷뿐이다 `[정적]`:
  - `+0x7f0` (ETW 갈래 — 2.7) · `+0x7e0` · `+0x820` (문맥 바꾸기 — 2.11) · `+0x1178` (COD — 2.10).
- test.exe 에서 넷 다 안 타는 갈래다.
- 사진: 블록 바이트 P0 = P1 = P1a = P1b = P2 (md5 같음) `[덤프 오프라인]`. K3 는 PEB `+0x2d8` 이 아직 null (`_K3.txt:1633`).
- 둘이 맞는다 → "**초기화 동안 ntdll 은 이 블록에 아무것도 안 쓴다**".
- 한계 (run 문서 "4."):
  - 한 번의 실행 (run44) 값이다.
  - 사진 **사이** 에 썼다가 되돌렸다면 사진엔 안 남는다. (COD 가 바로 그런 꼴이라 2.10 은 `[추측]` 으로 남았다.)

### 2.13 쓰는 줄을 못 찾은 4칸 `[공백]`

- 관리실이 읽는 칸 중 **부모 쪽 쓰는 줄을 못 찾은 칸** 이 넷이다: `+0x210` · `+0x11a8` · `+0x11aa` · `+0x11b2` `[공백]`.
  - `BasepGetAppCompatData` 안엔 쓰기가 없었다.
- 짐작: apphelp 의 호환 질의 (SDB) 결과일 것 — shim 이 걸린 exe 에서만 켜진다 `[추측]`.
- test.exe 에선 넷 다 0 `[덤프 오프라인]`. 그래서 지금 결과엔 영향이 없다.
- 누가 켜는지는 Q208 ① (4절).

### 2.14 PE_emulator 에게

- 지금 에뮬레이터: 블록을 안 만들고 `PEB+0x2d8 = 0` (결과서가 본 줄: `Src/ntdll/LdrInit.cpp:5763`). flow-writer 는 에뮬레이터 코드를 안 읽었다.
- 갈래별로 (run 문서 "PE_emulator 쪽"):
  - shim 게이트 · ETW · 세그먼트 힙 · COD — 0 이든 사진 블록이든 **같은 길 → 맞음**.
  - `SbSelectProcedure` — 어느 쪽이든 안 탐.
  - **`SbUpdateSwitchContextBasedOnDll` 만 다름** — 0 이면 `+0x32` 에서 끝나 명부 줄 `+0xc0` 과 `SbDynamicContextDllCount +4` 가 0 으로 남는다. 진짜는 모듈마다 표 칸 주소, 번호 0 칸 개수 0 → 3.
  - 결과서가 에뮬레이터 `Src` 에서 이 함수 줄을 못 찾았다 (grep 0) → **확인 필요**.
- 맞추려면 (결정은 사용자): `PEB+0x2d8` 에 최소 블록 (ctx `+0x7e0 ~ +0x838` 의 사진 값) + `SbUpdateSwitchContextBasedOnDll` 줄. 다른 칸은 0 이어도 읽기 결과가 같다.
- 비유: 에뮬레이터 가게엔 호환 안내문이 아예 없다. 다른 건 다 "해당 없음" 이라 상관없는데, **연식 꼬리표만 안 붙는다**.

### 2.15 결과서와 다르게 적은 곳

- **`LdrpCheckComponentOnDemandEtwEvent` 오프셋 넷** — flow-writer 가 `.asm` · `_calls.tsv` 로 고쳤다. 뜻은 그대로. 이 글도 고친 값을 썼다.

| 무엇 | 결과서 | run 문서 (이 글) |
|---|---|---|
| `CompatCachepLookupCdb` call | `+0x52` | **`+0x56`** (ret `+0x5b`) |
| 블록 `+0x1178` 쓰기 | `+0xca` | **`+0xce`** |
| `EtwEventWriteNoRegistration` call | `+0x122` | **`+0x126`** |
| `NtWaitForSingleObject` call | `+0x13d` | **`+0x141`** (ret `+0x146` 은 같음) |

- `NtCreateEvent` (`+0x9d`) · `NtClose` (`+0x14f`) 는 flow-writer 가 보탰다.
- `+0x1144` = test.exe 파일 크기: 결과서는 값만 맞췄고, flow-writer 가 `sample/bin/test.exe` 크기 551,424 B 와 맞춰 봤다 (`[소스]`, 칸 이름은 그대로 `[추측]`).
- 덤프 블록 다시 셈: flow-writer 가 결과서가 뽑은 블록 파일 다섯 장의 md5 · 0 아닌 바이트 74 · 표 칸 값을 다시 읽어 같음을 봤다 (새 분석 아님).
- 태그: 결과서 `[덤프 오프라인]` 을 **그대로** 썼다 (run88 때 `[관찰]` 로 바꾼 것과 다름 — 2.1).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 곳 | 전 | 후 (run118) |
|---|---|---|
| 기준 · 표시 규칙 | 태그 목록에 없음 | **`[덤프 오프라인]` 새 줄** — "그 순간 값이라는 점은 `[관찰]` 과 같지만 스크립트가 번역 — 섞지 않는다, 디버거 줄로 검증한 뒤에만" |
| 2.2 `CreateProcessInternalW` | 부모가 `+0x57d0` 에 주소를 씀 (run87) `[정적]` | + **블록 꼴**: `BasepGetAppCompatData` `0x11c0`B · ctx `+0x7e0` (`SbpCreateSwitchContext`) `[정적]`, 값 `[덤프 오프라인]` |
| 2.4 `LdrpInitializeProcess` 뒤쪽 | shim 길 입구 `+0x19d7` · `+0x19dc` 만 | + **`R13` = `pShimData` 지역 사본** (`+0x69f` → `+0x19cd`), 게이트 블록 `+0x210` `[정적]` |
| 2.4 힙 절 | "넷째 (`pShimData` 바이트) 만 못 봤다 → 동적 D1" | ~~취소선~~ → **넷째도 "아님"** — `+0x11b2` = 0 · `+0x11aa` = 0 `[덤프 오프라인]` |
| 2.5 알림 절 | `+0xd9` 가 `+0xc0` 을 씀 (run113) | + **블록을 보고 타는 유일한 곳**, 게이트 `+0x32` (ctx 0 이면 끝) · `+0x47` (켜짐), 바꾸는 갈래 안 탐 |
| 2.6 Q90 줄 | "`pShimData` 읽는 쪽은 후보 이름만 `[공백]`" | ~~취소선~~ → **닫힘 — Q90 끝**, 9곳 · 함수 6 |
| 4장 run46 줄 | D1 = 동적 Q91 | ~~D1~~ → 해결됨 (run118, `[덤프 오프라인]`) |
| 4장 run66 ② | shim 엔진 올리는 조건 (사진으로만 앎) | 일부 해결 — 첫 게이트 `+0x210`. 안 두 함수 조건은 남음 |
| 4장 run88 · run113 ⑤ | Q90 남김 · `SbpDetermineDllContext` 본문 | 해결됨 / `.asm` 은 뽑음, 칸 고르기 규칙은 남음 |
| 4장 새 줄 셋 | — | run118 동적 ① ~ ⑤ · 정적 1 ~ 3 · PE_emulator |
| 4.1 `p2h` 줄 | 값은 Q91 (D1 · D4 · …) | D1 이 빠짐 (D4 · D5 · D9 …) |

### 짐 지도 `baggage.md` 에서 바뀐 것

- **2장 숫자 그대로.** 블록 안 칸은 체크리스트 구조체가 아니라서 안 센다.
- 쓰는 말: `[덤프 오프라인]` 한 줄.
- 3.1 `pShimData` 줄 — "읽는 쪽" 근거 (9곳 · 함수 6, 탄 것은 하나). `AppCompatInfo` 줄 — `R13` `[추측]` → `[정적]`.
- 3.5 `SwitchBackContext` · 3.8 `SbDynamicContextDllCount +4` — 게이트 (`pShimData` 가 0 이면 안 씀).
- 새 **7.6.15** — 블록 칸 표 (체크리스트 밖, 줄 수에 안 셈). ntdll 은 초기화 동안 블록에 **안 쓴다**.
- 7.9 D1 → 닫힘. 새 **7.49** — 이 조각의 질문.

### 큰 그림에서

- **사용자 질문 (2026-10-27) "부모가 써 준 PEB 칸을 ntdll 이 쓰나" 의 마지막 조각** 이 닫혔다.
  - 문맥 쪽 (`+0x2f8` · `+0x308`) — run88.
  - `pShimData` 쪽 — 이번. **읽기는 하는데, test.exe 에선 연식 꼬리표 하나만 남고 안내문 자체는 안 고친다.**
- 짐 지도로는: 부모가 놓은 짐 하나 (호환 안내문) 를 **누가 읽고 무엇이 갈리는지** 까지 다 셌다.
- 에뮬레이터로는: "안내문을 0 으로 둬도 되나" 에 답이 생겼다 — **연식 꼬리표 둘만 빼면 된다**. 꼬리표를 채울지는 사용자 결정.
- 방법으로는: 사진을 **test.exe 페이지 표로 번역해 읽는 길** 이 한 번 검증됐다. 앞으로 사진 값을 더 읽을 때 쓸 수 있다 (도구를 레포에 둘지는 사용자 결정).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `+0x11aa` · `+0x11b2` 를 **라이브로** 봐도 0 인가 | 이번 값은 `[덤프 오프라인]` (디버거가 아니라 스크립트). 답은 났고, 확인만 남음 | **Q91** (동적, **선택**) — `RtlpHpOptIntoSegmentHeap+0x190` `db @rax+0x11a8 L0x10` |
| 안 탄다고 한 갈래가 정말 안 타나 — COD (`LdrpMapDllNtFileName+0x4d7`) · shim (`LdrpInitializeProcess+0x19ef`) · ETW (`SbObtainTraceHandle+0x5c`) · 문맥 바꾸기 (`SbUpdateSwitchContextBasedOnDll+0x1ea`) · `RtlInitializeHeapManager+0x56` | 정적 갈래 + 사진이라 "안 걸림" 은 못 봄. 특히 COD 는 쓰고 지우는 꼴이라 사진으로 못 가림 `[추측]` | **Q46** (동적, run118 더함, **선택**) — 멈춤점이 **안 걸리면** 맞음 |
| 탄다고 한 `+0xd9` 가 모듈마다 run113 사진 값과 같나 | 사진은 결과 칸만 봄 | **Q46** (같이) — `SbUpdateSwitchContextBasedOnDll+0xd9` `[R14+0xc0]` |
| kernelbase `CreateFileInternal +0x2f5` (`SbSelectProcedure`) 가 초기화 동안 불리나 | 정적으로는 부르는 길만 있음 | **Q46** (같이) |
| 블록 `+0x210` · `+0x11a8` · `+0x11aa` · `+0x11b2` 를 **누가 켜나** | 부모 `BasepGetAppCompatData` 안엔 쓰기 없음 `[공백]`. 후보 apphelp 등은 이번 범위 밖 (이름만) | **Q208 ①** (정적, 범위 넓음) |
| `SbpDetermineDllContext` 가 DLL 마다 **어느 판 칸을 고르는 규칙** | `.asm` 은 뽑았지만 부르는 둘 (`SbpRetrieveCompatibilityManifest` · `SbpResolveBasedOnName`) 까지만 | **Q208 ②** (정적, 작음) — 에뮬레이터가 꼬리표를 채우려면 필요 |
| **디버거가 붙어서 달라지는 점** — 블록 값을 정하는 부모 쪽 갈래에 디버거 깃발을 보는 줄이 있나 | 이번에 안 봄 `[공백]` | **Q208 ③** (정적) |
| shim 엔진 안 두 함수 (`LdrpLoadShimEngine` · `LdrpGetShimEngineInterface`) 의 조건 | 이번엔 첫 게이트 (`+0x210`) 만 | Q127 (b) — plan 상태 "정적 끝 (sast/120) — 문서 대기" |
| shim 이 걸린 exe 에서만 켜지는 갈래 (`+0x210` ≠ 0 · 세그먼트 힙 비트 · ETW 켜짐) | test.exe 로는 못 봄 | 다른 샘플 필요 — **범위 밖** (번호 없음) |
| PE_emulator 가 명부 줄 `+0xc0` · 개수 칸을 지금 어떻게 두나 · 채울지 | 결과서가 에뮬레이터에서 이 함수 줄을 못 찾음 | **사용자 결정** (확인 필요) |
| 덤프 오프라인 읽기 도구를 레포에 둘지 | 지금은 스크래치에만 (지워질 수 있음) | **사용자 결정** |

---

## 5. 더 읽기

- run 문서: [`run118-shimdata-read.md`](run118-shimdata-read.md) — 요약, 호출 지점 표, 호출 관계, "1. 읽는 곳 전수", "2. 블록 칸 표" (전체 25줄), "3. 갈리는 것", "4. 사진 오프라인 읽기", 결과서와 다르게 적은 곳, PE_emulator 쪽
- 요청서: [`_workspace/sast/116_shimdata-read.md`](../../_workspace/sast/116_shimdata-read.md)
- 결과서: [`_workspace/sast/116_shimdata-read_result.md`](../../_workspace/sast/116_shimdata-read_result.md) — 질문 1 ~ 4, PEB 아닌 `+0x2d8` 19줄 목록, 사진 오프라인 읽기 방법, 다음 요청 후보 (`LdrpCheckComponentOnDemandEtwEvent` 오프셋 넷은 run 문서 값으로 읽기)
- flow.md: 표시 규칙 표 (`[덤프 오프라인]`) · 2.2 `CreateProcessInternalW` 블록 꼴 · 2.4 `LdrpInitializeProcess` 뒤쪽 · 힙 절 · 2.5 알림 절 · 2.6 Q90 줄 · 4장 run118 줄 셋
- 짐 지도: `baggage.md` 7.6.15 (블록 칸 표) · 3.1 · 3.5 · 3.8 · 7.9 · 7.49
- 앞 해설: [run87 해설 2.9](run87-sxs-create-해설.md) · [run88 해설 2.7](run88-sxs-redirect-해설.md) · [run46 해설 2.4](run46-process-heap-해설.md) · [run113 해설 2.3](run113-ntdll-user-structs-해설.md) · [run114 해설](run114-ntdll-globals-early-해설.md)

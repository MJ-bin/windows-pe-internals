# run130 run65-66-leftovers — 해설

> 이 글은 [run130 문서](run130-run65-66-leftovers.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열 (`_workspace/plan.md`, 이번에 **끝**):
  - **Q124** — run65 가 남긴 작은 정적 (a) ~ (e)
  - **Q127** — run66 이 남긴 작은 정적 (a) ~ (c)
  - 덤 **Q228 (나)** — 짐 지도 7.6.12 커널 줄을 run125 결과로 채우기 (사용자 결정 2026-11-22)
- 이번 바퀴의 차례:
  - 정적은 2026-11-16 에 끝났다 (사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례). 그 뒤 "여기까지만" 결정으로 문서만 멈춰 있었다.
  - 2026-11-22 사용자 결정 "run130 = sast/120 문서 먼저" 로 이번에 문서가 됐다.
- 남은 것이 간 곳:
  - 동적 → **Q238** (모두 선택. VM 은 미룸)
  - 정적 → **Q239** (필수 아님. "필수 정적만" 결정으로 미룸)
- 근거:
  - run 문서 [`run130-run65-66-leftovers.md`](run130-run65-66-leftovers.md) (가장 중요)
  - 요청서 [`_workspace/sast/120_run65-66-leftovers.md`](../../_workspace/sast/120_run65-66-leftovers.md) · 결과서 [`120_run65-66-leftovers_result.md`](../../_workspace/sast/120_run65-66-leftovers_result.md) (판정 **분석성공**, 동적 필수 없음)
  - flow.md · 짐 지도 변경: docs 커밋 `cd5785c` (`git show cd5785c -- runs/03_sprint/flow.md`)
  - 덤의 근거: [run125 문서](run125-worker-factory.md) · `_workspace/sast/122_worker-factory_result.md` (새 분석 없음, 옮기기만)

### 이번 바퀴에서 조심할 곳 넷

1. **정적만이다.** VM 실행도, 동적 요청서도 없다.
   - `[관찰]` 로 적힌 값은 run44 사진 로그를 **앞 run 들이 맞춘 줄 그대로 다시 짚은 것** 이다. 새 실행이 아니다.
2. **오프셋 세 가지.** run 문서와 같은 규칙이다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 "`+0x733 LdrpQueryIllegalCWDDevices`" 처럼 함수 이름 앞 숫자 = **call 자리**.
   - 그냥 "`+0xb7 SUB R15,[RDI]`" = 함수 안 **줄**.
   - 콜스택으로 본 적은 없다.
3. **새 단계는 없다.** 이번에 늘어난 건 전부 **이미 있는 상자 안쪽** 의 줄이다. 그래서 그림은 그대로다 (2.11).
4. **질문이 작고 여러 개다.** 한 소제목에 질문 하나씩 푼다. 2.1 표에서 먼저 답을 보고, 궁금한 절로 가면 된다.

### 이미 푼 말 — 링크만

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- IFEO = **가게 이름별 특별 지시** → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 3.4
- KUSD = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5 · 부팅 때 정책을 게시판에 옮기는 줄 → [run115 해설](run115-kusd-boot-해설.md)
- 장치 특성 마스크 `LdrpIllegalCWDDevices` = "이런 땅 위 **현재 위치 핀** 은 DLL 찾을 때 믿지 마" 체크 칸 → [run65 해설 2.6](run65-ldr-init-bodies-해설.md)
- TLS = 직원 **개인 사물함**, TEB `+0x58` = **열쇠꾸러미**, `LdrpTlsBitmap` = 사물함 **번호 배정표** → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) 2.3 · [run65 해설 2.10](run65-ldr-init-bodies-해설.md)
- 스케줄러 공유 데이터 = 관리사무소에 신청해 받는 **직원 카드 묶음 상자**, TEB `+0x1850` = **열쇠 대출 카드** → [run65 해설 2.9](run65-ldr-init-bodies-해설.md) · [run41 해설 2.3](../02_sprint/run41-tls-lock-detail-해설.md)
- 섹션 객체 = 커널이 들고 있는 **조립식 건물 세트**, 매핑 = 땅에 **건물 갖다 놓기** → [run31 해설](../02_sprint/run31-image-section-해설.md) 2절
- 모듈 항목 = **명부 한 줄**, DDAG 노드 = **연결 카드**, `State` = **진행 도장** → [run66 해설](run66-apphelp-cache-해설.md) 머리
- apphelp 묻기 = "이 물건 **리콜 대상** 인가요?" → [run66 해설 2.3](run66-apphelp-cache-해설.md)
- shim = 옛 가전에 붙이는 **변환 어댑터**, `pShimData` 블록 = **호환 안내문**, 게이트 = **문지기** → [run118 해설](run118-shimdata-read-해설.md) 2.2 · 2.6
- SwitchBack = 건물마다 붙이는 **연식 꼬리표** → [run118 해설](run118-shimdata-read-해설.md) 2.4
- 일꾼 공장 = **파견소**, 완료 포트 = **우편함**, 공장 몸 `f` = **장부**, 덩어리 `P` = **메모판**, 풀어주기 패킷 = **빈 쪽지** → [run125 해설 2.2 · 2.3](run125-worker-factory-해설.md)
- 짐 지도 셈 기준 · "조건부 안 탐" 을 3.8 로 옮기는 까닭 → [run114 해설 2.2](run114-ntdll-globals-early-해설.md)
- 사진 P1a · P1b → [run44 해설 2.8](run44-baggage-snap-해설.md)

이번에 처음 나오는 말 — **합류 지점**, **정렬 (줄 맞춤)**, **`SizeOfZeroFill`**, **전수 검사**, **shim 엔진 입구** — 은 본문에서 처음 나올 때 한 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = **신청 창구**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**.
- 짐 지도 = 개업 **점검표**.
- 이번 바퀴는 run 문서 비유 그대로다:
  - 점검표 칸 몇 개가 "아마 이럴 것" 으로 남아 있었다.
  - 이번엔 그 칸마다 **"점검표 몇 번째 줄에 그렇게 쓰여 있다"** 를 찾아 붙였다.
  - 새 칸을 만든 게 아니라, **이미 있는 칸에 근거 쪽지를 붙인** 바퀴다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

`LdrpInitializeProcess` 안 여기저기다. run65 · run66 이 본 자리를 **다시 한 번씩** 들른다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ +0x536   LdrpInitializeExecutionOptions
 │         │            └─ +0x738 LdrpQueryIllegalCWDDevices        ← (b) 닿나?
 │         ├─ +0xfca   LdrpInitializePolicy  (LdrpPolicyBits)         ← (a) 셈
 │         ├─ +0x182c  LdrpAllocateSchedulerSharedData
 │         │            └─ 커널 PspSchedulerSharedDataRegionCreate   ← (d) 크기
 │         ├─ +0x18d0  LdrpInitializeTls
 │         │            └─ +0x162 LdrpAllocateTls                    ← (c) 0x11
 │         ├─ +0x19f4  LdrpInitShimEngine (test.exe 안 탐)            ← Q127 (b) 문지기
 │         └─ (알림)   LdrpSendPostSnapNotifications
 │                      ├─ +0xa8 SbUpdateSwitchContextBasedOnDll     ← Q127 (c)
 │                      └─ +0xce Flags |= 8                          ← Q127 (a)
 │         … 로더 풀 첫 넣기 → 커널 일꾼 공장                         ← 덤 (짐 지도만)
 └─ … → NtContinue → exe 진입점 → main
```

### 무엇이 궁금했나

run65 · run66 이 "다음에 보자" 하고 남긴 질문들이다.

- **Q124 (run65 몫)**
  - (a) `LdrpPolicyBits` 를 짐 지도 7.3 (모름) 에서 3.8 (아는 칸) 로 옮길까?
  - (b) `+0x733 LdrpQueryIllegalCWDDevices` 는 IFEO 키가 **없을 때도** 불리나?
  - (c) exe TLS 블록이 `0x11` 바이트라는 건 `[추측]` 이었다. 그 셈이 쓰인 줄은?
  - (d) 스케줄러 공유 데이터 섹션은 사진에서 2쪽이었다. 그 크기는 누가 정하나?
  - (e) 그림 2 에 새 상자를 넣을까? → 사용자 결정
- **Q127 (run66 몫)**
  - (a) 모듈 항목 `Flags.LoadNotificationsSent` 를 켜는 줄이 `+0xce` 말고 또 있나?
  - (b) shim 엔진을 올리는 길은 어떤 조건에서 타나?
  - (c) `SbUpdateSwitchContextBasedOnDll` 이 모듈 항목 `SwitchBackContext` 를 쓰나?

### 바퀴 전에 알던 것

- `[정적]` (run65) 마스크 `LdrpIllegalCWDDevices` 를 정하는 건 `LdrpQueryIllegalCWDDevices`. 하지만 "IFEO 키가 없으면 IFEO 구간을 건너뛴다" (run46) 는 말이 있어서, 그 함수까지 닿는지는 안 봤다.
- `[관찰]` (run44) KUSD `CurDirDevicesSkippedForDlls = 0y00`, 마스크 `끝까지 0*`.
- `[추측]` (run65 · 짐 지도) exe TLS 블록 `0x11`B — 결과서 값만 있고 셈 줄은 없었다.
- `[관찰]` (run44 P1a) 스케줄러 공유 데이터 매핑 VAD 2쪽.
- `[정적]` + `[관찰]` (run66) `+0xce` 가 비트 3 을 켜고, 사진에서 다섯 모듈만 1 → "syscall 5번" `[추측]` (강한 받침).
- `[정적]` (run118) shim 엔진의 첫 게이트 = `pShimData` 사본, 그다음 블록 `+0x210`. test.exe 는 `+0x210 = 0` `[덤프 오프라인]`.
- `[정적]` (run113 · run118) `SbUpdateSwitchContextBasedOnDll +0xd9` 가 모듈 `+0xc0` 을 쓴다.

---

## 2. 찾은 것

### 2.1 한눈에 — 질문마다 한 줄 답

run 문서 "요약" 표 그대로다.

| 질문 | 답 | 표시 |
|---|---|---|
| Q124 (a) `LdrpPolicyBits` 를 3.8 로? | **이미 run114 가 옮겼다.** 쓰는 줄 5개 다시 셈, 같음. **2장 숫자 그대로** (2.3) | `[정적]` |
| Q124 (b) `+0x733` 이 IFEO 키 없을 때도 닿나? | **닿는다.** `+0x71c` 에서 길이 합쳐진다. `RCX = 0`. 값 0 은 **이 VM 의 KUSD 정책** 때문 (2.4) | `[정적]` |
| Q124 (c) TLS 블록 `0x11` 의 근거 줄? | `LdrpAllocateTls +0xb7` · `+0xd1 ~ +0xd5` · `+0xec` · `+0xf0` → `1 + 0x10 = 0x11`. `SizeOfZeroFill` 은 **안 읽는다** (2.5) | `[정적]` (+ PE 바이트) |
| Q124 (d) 스케줄러 섹션 크기? | **커널 상수 `0x2000`** — `PspSchedulerSharedDataRegionCreate +0x127` (2.6) | `[정적]` |
| Q124 (e) 그림 2 새 상자? | **사용자 결정: 안 넣음** (2.11) | 결정 |
| Q127 (a) 비트 3 을 켜는 다른 줄? | **`+0xce` 하나뿐**, 끄는 줄 0 (2.7) | `[정적]` |
| Q127 (b) shim 엔진 길 조건? | **문지기 둘** (`+0x19d7` · `+0x19dc`), 입구 셋. test.exe 는 셋 다 안 탐 (2.8) | `[정적]` |
| Q127 (c) `SwitchBackContext` 를 쓰나? | **쓴다** — `+0xd9`. 모듈 `+0xc0` 을 쓰는 줄은 이것뿐 (2.9) | `[정적]` |
| Q228 (나) 짐 지도 7.6.12 커널 줄 | run125 칸으로 채움 (2.10) | `[정적]` (run125) |

- 정적과 동적이 어긋난 곳은 **없다.** 사진 값이 모두 정적 길과 맞는다 (run 문서).

### 2.2 한 줄씩 읽는 법

run 문서 "정적 — 호출 지점" 표에서 둘을 골라 "누가 / 어디서 / 누구를 / 무엇을 위해" 로 읽는다.

#### 줄 ①: `ntdll!LdrpAllocateTls | +0xfa | +0xff | ntdll!RtlAllocateHeap (LdrpTlsHeap, 태그만, 크기 0x11)`

- **누가**: `LdrpAllocateTls` — TLS 항목마다, 스레드마다 사물함 상자를 마련하는 함수.
- **어디서**: 함수 안 `+0xfa` 에서 부르고, 돌아오는 자리는 `+0xff`. 멈춤점을 건다면 `ntdll!LdrpAllocateTls+0xfa` (Q238 ②).
- **누구를**: `RtlAllocateHeap` — 창고 (힙) 에서 덩어리 하나 받기.
- **무엇을 위해**: exe 의 TLS 블록 (`0x11` 바이트) 을 받으려고.
  - "태그만" = 할당 플래그에 꼬리표 (`NtdllBaseTag + 0xc0000`) 만 있고, **0 으로 채우라는 비트는 없다** 는 뜻이다 (2.5).

#### 줄 ②: `nt!PspSchedulerSharedDataRegionCreate | +0x158 | +0x15d | nt!MmCreateSectionEx (R8D = 0x2000)`

- **누가**: 커널 `PspSchedulerSharedDataRegionCreate` — 스케줄러 공유 데이터 덩어리를 만드는 함수.
- **어디서**: `+0x158` call, `+0x15d` ret.
- **누구를**: `MmCreateSectionEx` — 섹션 (조립식 건물 세트) 만들기.
- **무엇을 위해**: 크기 `0x2000` (2쪽) 짜리 세트를 만들려고. 그 크기가 셋째 인자 `R8D` 로 들어간다 (2.6).

### 2.3 Q124 (a) — `LdrpPolicyBits` 는 왜 "다시 안 세나"

#### 코드가 말하는 것 `[정적]`

- 이 칸 (`0x1cd7fc`) 을 **쓰는** 줄은 ntdll 전체에 다섯이다:
  - `LdrpInitializePolicy +0x7e` (`= 0x29`) · `+0x26b` (`|= 4`) · `+0x305` (`|= 0x40`) · `+0x31d` (`|= 2`) · `+0x329` (`&= ~1`)
  - 나머지 16줄은 읽기다.
- 다섯 모두 문 두 개 뒤에만 있다:
  - `+0x46 TEST [PEB+3],0x10` — 패키지 앱인가
  - `+0x72 CMP word [DllPath.Length],0` — `DllPath` 가 비지 않았나
- test.exe 는 `BitField 0x4` (`[관찰]` run44) → `+0x51 JZ +0x58` 로 그냥 돌아간다 → 파일 값 `0x16` 그대로.

#### 왜 짐 지도 숫자를 안 바꾸나 — 단계별로

1. 결과서는 "7.3 에서 빼서 3.8 로 옮기자" 고 적었다.
2. 그런데 지금 짐 지도를 보면, **run114 가 이미 옮겼다.**
   - 3.8 에 "조건부 안 탐" 줄이 있고,
   - 7.3 에서는 취소선으로 뺐고,
   - 7.24 ① 에 그 기록이 있다.
3. 즉 결과서의 판단과 **같은 결론이 이미 점검표에 반영돼 있다.**
4. 그래서 → 다시 옮겨 세면 **같은 칸을 두 번 센다.**
5. 이번엔 쓰는 줄 다섯을 **다시 세어 같음** 만 확인했다. **2장 숫자 그대로.**

- 같은 판단을 전에도 한 적이 있다: run123 이 Q113 (c) 무결성 전역 둘을 "run114 가 이미 셌다" 로 안 센 것 ([run123 해설 2.12](run123-run58-60-leftovers-해설.md)).
- 비유 (run123 해설 그대로): 재고표에 이미 "입고" 줄이 적혀 있는데, 옛 장부를 보고 온 사람이 "하나 더 적어야 해요" 한 것이다. 장부 판이 바뀐 걸 확인하고 안 적었다.
- "조건부 안 탐" 을 왜 3.8 에 두는지 자체는 [run114 해설 2.2](run114-ntdll-globals-early-해설.md) 에 있다 — 쓰는 줄이 지도 길 안에 있고, test.exe 가 조건을 안 채울 뿐이라서.

### 2.4 Q124 (b) — `+0x733` 은 IFEO 키가 없어도 닿는다

#### 무엇이 헷갈렸나

- run46 은 "IFEO 키가 없으면 IFEO 를 건너뛴다" 고 했다.
- `LdrpQueryIllegalCWDDevices` 는 같은 함수 (`LdrpInitializeExecutionOptions`) 의 **뒤쪽** `+0x733` 에 있다.
- 그럼 "건너뛴다" 가 이 호출까지 건너뛰는 걸까? run65 는 이걸 안 봤다.

#### 새 말 — 합류 지점

- 갈래로 나뉘었던 길이 **다시 만나는 줄** 이다. 그 줄부터는 어느 갈래로 왔든 같은 코드를 탄다.
- 비유: 두 골목이 만나는 **큰길 입구**. 어느 골목으로 왔든 거기서부터는 같은 길을 걷는다.

#### 길을 따라가 보자 `[정적]`

run 문서 나무를 줄였다 (줄 번호는 그대로).

```
LdrpInitializeExecutionOptions
 │  +0xa6 TEST [ProcessParameters.Flags],0x4000   "IFEO 키 없음" 표시 (test.exe 0x4001 [관찰] run44)
 │  +0xad JNZ → +0x604                            ← test.exe: IFEO 읽기 구간 (+0xb3 ~ +0x603) 통째로 건너뜀
 ├─ +0x609  LdrControlFlowGuardEnforced          (키 있음 · 없음이 처음 만나는 줄)
 ├─ +0x623  RtlSetGlobalHeapFeatures             +0x623 XOR ESI,ESI (상태 0)
 │  +0x6b9 · +0x6be (키 핸들 RDI = 0) · +0x6c4 (BeingDebugged == 0) → 셋 다 +0x71c 로 합침
 │  +0x72c TEST ESI · +0x72e JS → +0x797         (ESI < 0 일 때만 건너뜀 — test.exe 는 0)
 │  +0x730 MOV RCX,RDI                            RCX = 0 (키 없음)
 └─ +0x738  LdrpQueryIllegalCWDDevices
```

단계별로:

1. test.exe 의 매개변수 `Flags` 는 `0x4001` `[관찰]` — `0x4000` 비트 = "IFEO 키 없음" 이 켜져 있다.
2. 그래서 `+0xad JNZ` 를 타고 `+0x604` 로 뛴다. **건너뛰는 건 IFEO 를 "읽는" 구간 (`+0xb3 ~ +0x603`) 까지** 다.
3. `+0x604` 는 키가 있을 때 오는 길과 **처음 만나는 줄** 이다. 여기서 `LdrControlFlowGuardEnforced` · `RtlSetGlobalHeapFeatures` 를 지난다.
4. 뒤에 문이 셋 (`+0x6b9` · `+0x6be` · `+0x6c4`) 있는데, 셋 다 **`+0x71c` 로 모인다.** 여기가 두 번째 합류 지점이다.
5. 남은 문은 하나 — `+0x72e JS`. 상태값 (`ESI`) 이 **음수일 때만** 건너뛴다. test.exe 는 `+0x623` 에서 0 을 넣은 뒤 그대로다.
6. 그래서 → `+0x730` 에서 `RCX` 에 키 핸들 (`RDI`) 을 넣는데, 키가 없으니 **0**. 그리고 `+0x733` 에서 부른다.

- 안 닿는 경우는 상태가 음수일 때뿐이다 (큰 페이지 이미지의 키 열기 실패, 디버그 힙 환경 변수 변환 실패 · 검증기 패키지 실패). test.exe 는 해당 없음 `[정적 + 관찰 값]`.
- 비유: "이 가게 이름으로 된 특별 지시 서류철이 없다" 는 표시가 있으면, 관리실은 **서류철 뒤지는 단계만** 건너뛴다. 그다음 "현재 위치 핀 규칙 정하기" 단계는 어차피 모두가 지나는 큰길 위에 있다.

#### 안에서는 — 키가 0 이면 게시판을 본다 `[정적]`

- `LdrpQueryIllegalCWDDevices` 는 `+0x13 TEST RCX` · `+0x16 JNZ` 로 키 핸들을 본다.
  - 0 이면 → `+0x18` 부터 KUSD `0x7ffe02d5` 의 비트 4~5 (`CurDirDevicesSkippedForDlls`) 를 읽는다.
  - 0 이 아니면 → 먼저 IFEO 값 `CWDIllegalInDLLSearch` (`+0x6e`) 를 읽고, 없거나 −1 ~ 2 밖이면 KUSD 길.
- KUSD 값에 따라 마스크가 정해진다: `0 → 0` · `1 → 0x2000` · `2 → 0x10` · `3 → −1`.
- `+0x40` 이 결과를 `LdrpIllegalCWDDevices` 에 **명시적으로 쓴다.** test.exe 는 0 을 쓴다 (파일 값도 0 이라 결과는 같다).

#### ★ 그래서 run65 의 말 하나가 바뀐다

- run65: "마스크는 어느 쪽이든 0 이다."
- run130: test.exe 에선 0 이 맞다. 하지만 **이유는 이 VM 의 정책 값** (`CurDirDevicesSkippedForDlls = 0`, `[관찰]` run44) 이다. 코드가 0 을 보장하지 않는다.
  - IFEO 에 `CWDIllegalInDLLSearch` 를 넣었거나 정책을 바꾼 컴퓨터에선 0 이 아니다.
  - 그러면 `LdrpInitializeProcess +0x1183 LdrpCheckAppDirType` 이 탄다 (run65).
- 이 정책을 게시판에 옮기는 건 부팅 때 커널 `PspInitPhase2 +0x136 · +0x13f · +0x16c` 다 (sast/110, run115).
- 비유: 공용 게시판에 "핀 규칙: 없음" 이라고 붙어 있어서 0 이 나온 것이다. 게시판 글이 바뀌면 답도 바뀐다.

### 2.5 Q124 (c) — exe TLS 블록 `0x11` 바이트의 셈

#### 먼저 재료 — test.exe 의 TLS 디렉터리 (PE 바이트)

결과서 값이고, flow-writer 가 `sample/bin/test.exe` 를 다시 읽어 같았다.

| 칸 | 값 |
|---|---|
| `StartAddressOfRawData` · `EndAddressOfRawData` | `0x140079f90` · `0x140079f91` → 원본 **1 바이트** |
| `SizeOfZeroFill` | **0** |
| `Characteristics` | `0x00100000` → 정렬 코드 1 (1 바이트 정렬) |

#### 새 말 — `SizeOfZeroFill`

- PE 파일 TLS 디렉터리의 칸 하나다. 이름 그대로 읽으면 "원본 데이터 뒤에 **0 으로 채울 바이트 수**" 다 `[추측]` (일반적으로 알려진 설명).
- 비유: 사물함 상자에 넣을 물건 목록 끝에 "빈 칸 몇 개 더" 라고 적힌 메모.
- 이번에 본 것: 진짜 코드는 이 메모를 **안 읽는다** (아래).

#### 새 말 — 정렬 (줄 맞춤)

- 주소를 16 같은 수의 배수 자리에 맞추는 것이다.
- 비유: 선반 칸이 16칸마다 그어져 있어서, 상자를 꼭 **선 위에서 시작** 하게 놓는 것. 그러려면 여유 공간이 조금 더 필요하다.

#### 셈 줄 — `LdrpAllocateTls` `[정적]`

| 줄 | 하는 일 |
|---|---|
| `+0x79 MOV R12D,0x10` | 정렬 시작값 16 |
| `+0x98 ~ +0x9f` | 항목 `+0x34` (= 디렉터리 `Characteristics`) 에서 정렬 코드 → 1 |
| `+0xb4 MOV R15,[RCX]` · **`+0xb7 SUB R15,[RDI]`** | `R15 = End − Start` = 1. 항목 `+0x30` (`SizeOfZeroFill`) 을 읽는 줄은 **없다** |
| `+0xc5 ~ +0xce` | 정렬 = `1 << (코드 − 1)` → 1 |
| `+0xd1 CMP R8D,0x10` · `+0xd5 CMOVC` · `+0xdc LEA EBX,[R8-1]` | **정렬은 최소 `0x10`** → `EBX = 0xf` |
| **`+0xec LEA R8,[R15+1]` · `+0xf0 ADD R8,RBX`** | 크기 = `(End − Start) + 정렬` = **`0x11`** |
| `+0xe0 ~ +0xe6` → `+0xfa RtlAllocateHeap` | 플래그 = `NtdllBaseTag + 0xc0000` 뿐 — **0 채움 비트 (`8`) 없음** |
| `+0x108 ~ +0x112` | 받은 주소를 16 정렬로 올리고, 바로 앞 8 바이트에 원래 주소를 숨김 |
| `+0x121` · `+0x12b` | 벡터 칸에 걸고, **원본 `End − Start` 바이트만** 복사 |

단계별로 읽으면:

1. 물건 크기 = `End − Start` = 1 바이트 (`+0xb7`).
2. 파일이 원하는 정렬은 1 바이트지만, 코드는 **최소 16** 으로 올린다 (`+0xd1 ~ +0xd5`).
3. 받을 크기 = 물건 1 + 줄 맞춤 여유 16 = **`0x11`** (`+0xec` · `+0xf0`).
4. 그래서 블록 `0x11`B = 앞 `0x10` (정렬 여유 + 숨긴 주소) + 뒤 1B 데이터.

- 비유 (run 문서 그대로): `0x11` 바이트는 "그쯤이겠지" 가 아니라 **"물건 1 바이트 + 줄 맞춤 여유 16 바이트"** 라는 셈이 코드에 그대로 있었다.
- 사물함 비유로: 사물함 상자를 창고에서 받을 때, 선반 선에 맞추려고 16칸 여유를 더 받는다. 상자 앞쪽 빈자리에 "원래 받은 자리" 메모를 끼워 두고, 물건은 1개만 넣는다.

#### `SizeOfZeroFill` 과 "0 채움" — 정정 하나

- `SizeOfZeroFill` 이 0 이 아닌 이미지라도, 진짜 코드는 그만큼 **받지도 0 으로 채우지도 않는다** `[정적]`. test.exe 는 0 이라 상관없다.
- 그래서 짐 지도 7.6.5 의 "0 채움 (0B)" 은 **틀린 말** 이었다 — 0 채움 단계 자체가 없다. 짐 지도만 고쳤다 (run65 문서엔 그 말이 없다).

#### 사진과 맞나 — 어긋나지 않는다 `[추측: 계산]`

- run44 칸 0 = `0x25996f38440` 이 "블록 + `0x10`" 이라면, 블록은 `…38430`.
- 그 자리는 벡터 블록 (`…383d0`, `0x58`B) 바로 다음이다.
- 계산으로 맞춘 것이라 `[추측: 계산]` 이다. 실제 `r8 = 0x11` 은 동적 (Q238 ②).

#### 덤 — `LdrpInitializeTls` 는 바이트를 안 센다 `[정적]`

- 항목 수 · 비트 수만 센다. 바이트 크기 셈은 `LdrpAllocateTls` 몫이다.
- DLL 쪽 `LdrpHandleTlsData +0x17a ~ +0x1df` 도 같은 식이다 (결과서). 다만 test.exe 길에서 쓰이는지는 이번에 안 따졌다.

### 2.6 Q124 (d) — 스케줄러 섹션 2쪽은 커널이 정한다

#### ntdll 의 `8` 은 크기가 아니다 `[정적]`

- `LdrpAllocateSchedulerSharedData` 는 `NtSetInformationProcess(-1, 0x70, &핸들, 8)` 을 부른다 (`+0x19` · `+0x28` · `+0x2c`).
- 여기서 `8` 은 **출력 핸들 칸의 길이** 다. 크기를 넘기는 게 아니다.
- 커널 `NtSetInformationProcess +0xec4 CMP R13D,8` 이 **정확히 8** 을 요구한다.
- 비유: 창구에 "카드 묶음 상자 주세요" 신청서를 낼 때 적는 `8` 은 "**번호표 받을 칸 크기**" 지, 상자 크기가 아니다.

#### 크기는 커널의 상수 `[정적]`

```
nt!NtSetInformationProcess
 └─ +0xf1c  PspSchedulerSharedDataRegionCreate
              +0x127 MOV R15D,0x2000                     크기 = 0x2000 (2쪽)
              ├─ +0x15d  MmCreateSectionEx             섹션 (건물 세트)
              ├─ +0x193  MiMapViewInSystemSpace        커널 쪽에 세움
              └─ +0x251  MmMapViewOfSection            이 가게 땅에도 세움
```

- 셋 모두 같은 `0x2000` 이다 — 섹션, 커널 쪽 보기, 이 프로세스 쪽 보기.
- `+0x1a9 MOV R9D,2` — 쪽마다 한 칸을 채우는 반복 2번.
- **헷갈리지 말 것**: `+0x7d MOV R14D,0xf8` 은 객체 본체 크기 (`ObCreateObjectEx`) 이지 섹션 크기가 아니다 (결과서).
- 사진 `[관찰]` run44 P1a "VAD `25996cb0` 2쪽 매핑" (짐 지도 7.6.5) 과 맞는다.
- 비유: 상자 크기는 가게가 고르는 게 아니라 **관리사무소 규격** 이다. 늘 2쪽짜리.

### 2.7 Q127 (a) — `LoadNotificationsSent` 를 켜는 줄은 `+0xce` 하나

#### 새 말 — 전수 검사

- 어떤 칸을 쓰는 줄을 모듈 전체에서 **하나도 빠짐없이** 찾는 것이다.
- 비유: "이 서랍 여는 사람" 을 찾으려고 출입 기록을 **처음부터 끝까지** 넘겨 보는 것.
- 이번엔 sast/103 방법 (`scan_flags.py`) 을 다시 돌렸다.

#### 결과 `[정적]`

- ntdll 에서 이 비트 (항목 `+0x68` 비트 3) 를 **만드는** 쓰기는 `LdrpSendPostSnapNotifications +0xce OR dword [RBX+0x68],8` **하나**.
- 비트 3 을 **끄는** 줄은 0.
- "읽고-고쳐-되쓰는" 줄 (`LdrpCompleteMapModule` · `LdrpProcessMappedModule` · `LdrpInitializeProcess +0x13c4 ~ +0x13f0` · `AvrfMiniLoadDll`) 은 같은 항목의 **옛 값** 이라 새 비트를 안 만든다.
- KernelBase · kernel32 에는 모듈 항목 `Flags` 를 쓰는 줄이 없다.
- 이 줄 앞엔 비트 3 을 보는 문지기가 없다. 건너뛰는 길은 검증기 (`NtGlobalFlag & 0x100`, test.exe 0) 뿐이다.

#### 그래서 무엇이 닫혔나 — 단계별로

run66 해설 2.12 의 사슬을 다시 놓으면:

1. `[정적]` 비트 3 을 켜는 줄은 `+0xce` 하나 ← **이번에 닫힘** (run66 의 "아직 확인 아님" 자리).
2. `[관찰]` 사진 P1b: test.exe · KERNEL32 · KERNELBASE · dll1 · dll2 = 1, ntdll = 0 (`ms/logs/04_baggage-read_P1b.txt:3392 · 3602 · 3707 · 3812 · 3917` = 1, `:3497` = 0).
3. 그래서 → "알림 = 모듈 다섯" 은 `[정적 + 관찰]` 로 닫혔다.
4. **아직 `[추측]`**: syscall (`NtApphelpCacheControl`) 이 **5번** 이라는 것.
   - 알림 한 번마다 syscall 을 하는지는 `LdrpCheckModule` 안 조건이 따로 정하기 때문이다.
   - 받침은 "모듈이 다섯" 쪽에서 닫혔고, syscall 수 자체는 그대로다.

- 비유: "리콜 질문지를 보낸 물건" 표시를 하는 사람이 관리실에 **딱 한 명** 뿐임을 확인했다. 그러니 표시가 다섯 개면 질문지 단계를 탄 물건이 다섯이다. 다만 질문지가 실제로 상담실 창구까지 갔는지는 또 다른 조건이 정한다.
- 한계 (결과서): ntdll · KernelBase · kernel32 코드만 봤다. apphelp · 검증기 DLL 은 범위 밖. `[reg+idx*N+0x68]` 꼴은 못 본다.

### 2.8 Q127 (b) — shim 엔진 길: 문지기 둘, 입구 셋

#### 문지기 둘 `[정적]`

run 문서 나무를 줄였다.

```
LdrpInitializeProcess
 │  +0x19cd MOV R13,[RSP+0x180]                         PEB +0x2d8 pShimData 의 지역 사본
 │  +0x19d7 TEST R13 · +0x19da JZ → +0x19f4             문지기 1: pShimData ≠ 0
 │  +0x19dc CMP [R13+0x210],0 · +0x19e3 JZ → +0x19f4    문지기 2: 블록 +0x210 ≠ 0  ← test.exe 는 여기서 돌아감
 └─ +0x19f4  LdrpInitShimEngine(pShimData)
              ├─ +0xd2   LdrpLoadDll("apphelp.dll")
              ├─ +0x10e  LdrpGetShimEngineInterface   (내보내기 12개 → g_pfnSE_*)
              ├─ +0x165  간접 = g_pfnSE_InitializeEngine
              └─ +0x173  LdrpLoadShimEngine  …  +0x148 g_ShimsEnabled = 1
```

- 문지기는 **이 둘뿐** 이다.
- 안쪽 셋 (`LdrpInitShimEngine` · `LdrpGetShimEngineInterface` · `LdrpLoadShimEngine`) 엔 새 문지기가 없다. 실패하면 로그를 남기고 **조용히 끝난다**.
  - 예외 하나: `LdrpLoadShimEngine` 안에서 shim DLL 의 `LdrpInitializeNode` 가 실패하면 `NtTerminateProcess` — 프로세스를 죽인다.
- 비유 (run 문서 그대로): 변환 어댑터 엔진은 가게 문 앞 **문지기 둘** (안내문이 있나 · 안내문 `+0x210` 칸이 0 이 아닌가) 만 통과하면 들어온다. test.exe 는 **둘째 문지기에서 돌아간다**.
  - 첫째 문지기는 통과한다 — 안내문 (블록) 자체는 있다 (run118).

#### `g_ShimsEnabled` · `g_ShimsLoading` 쓰는 줄 `[정적]`

- `g_ShimsEnabled = 1` 은 **`LdrpLoadShimEngine +0x148` 한 줄** 뿐이다 (`_refs/g_ShimsEnabled.tsv`).
- 0 을 쓰는 줄은 둘:
  - `LdrpGetShimEngineInterface +0x129` — 인터페이스를 못 얻었을 때. 값이 0 인 근거: `+0x15 XOR EDI,EDI` 뒤 `RDI` 를 바꾸는 줄이 없다 (flow-writer `.asm`).
  - `LdrpInitializeProcess +0x1ceb` — 켜졌을 때 `SE_InstallAfterInit` 가 0 을 돌려주면 (run66).
- `g_ShimsLoading` 값 (flow-writer `.asm`):
  - `+0x7e` = `R14B` = **1** (`+0x5e MOV R14B,1`)
  - `+0x141` = `R13B` = **0** (`+0x4c XOR R13D,R13D`, 그 뒤 `R13` 쓰기 없음)
- 그래서 짐 지도의 `[추측]` 두 값이 **`[정적]`** 이 됐다. 레지스터가 어디서 왔는지까지 따라가서다.

#### 새 말 — shim 엔진 입구

- shim 엔진을 올리는 코드로 **들어가는 문** 이다. 이번에 셋을 찾았다.
- 비유: 어댑터 엔진이 가게에 들어오는 **문 세 개** — 개업 때 정문, 나중에 쪽문, 리콜 상담 창구 옆 문.

| 입구 | 부르는 곳 | `g_ShimsEnabled` 를 켜나 | test.exe |
|---|---|---|---|
| ① 프로세스 시작 | `LdrpInitializeProcess +0x19ef` → `LdrpInitShimEngine` | 켠다 | **안 탐** — 블록 `+0x210 = 0` (`[덤프 오프라인]` run118) → `+0x19e3 JZ` |
| ② 나중에 (내보내기) | `LdrInitShimEngineDynamic` (rva `0x8b1b0`) `+0x67` → `LdrpLoadShimEngine` · `+0xe4` → `LdrpGetShimEngineInterface` | 켠다 | **안 탐** — ntdll · kernel32 · KernelBase 안에 부르는 줄 0. 부르는 쪽은 다른 DLL (apphelp 같은) `[추측]` |
| ③ apphelp 캐시 대답 | `LdrpGetProcApphelpCheckModule +0x285` | 안 켠다 (인터페이스만) | **안 탐** — `g_pShimmedModuleList = 0` (`[관찰]` run44, run66) |

- ② 의 `LdrInitShimEngineDynamic` 은 이번에 **새로 뽑은 함수 하나** 다 (`_calls.tsv` +9줄).
- 남은 `[공백]`: 블록 `+0x210` 을 **누가 쓰나** (sast/116 그대로 — 부모 쪽 kernelbase · apphelp 길 후보). 이게 "shim 이 걸린 exe" 로 바뀌는 날의 문턱이다 → Q239 ①.

### 2.9 Q127 (c) — `SwitchBackContext` 는 `+0xd9` 가 쓴다

- **쓴다** `[정적]`: `SbUpdateSwitchContextBasedOnDll +0xd9 MOV [R14+0xc0],RDI`.
  - `RDI` = `SbpDetermineDllContext` (call `+0xc1`, ret `+0xc6`) 가 준 표 칸 `+4`.
  - run113 · run118 그대로 — 이번엔 근거만 옮겼다.
- 결과서가 덧붙인 전수: ntdll 에서 **모듈 항목 `+0xc0` 을 쓰는 줄은 이것뿐**.
  - `LdrpSnapModule +0xefa` 는 import 문맥이다 (모듈 항목 아님).
  - `LdrpMapCleanModuleView +0xa8` · `AvrfMiniLoadDll +0x8f8` 은 구조체 정체를 안 따라갔다 `[공백: 낮음]` → Q239 ④.
- 비유: 명부 한 줄의 **연식 꼬리표 칸** 에 글을 적는 사람은 관리실에 한 명뿐이다.

### 2.10 덤 — 짐 지도 7.6.12 커널 줄을 run125 로 채움 (Q228 (나))

#### 무엇이 비어 있었나

- 짐 지도 7.6.12 (로더 풀 첫 넣기) 에 "(커널) 로더 풀 일꾼 공장 — 풀어 줄 수" 라는 줄이 하나 있었다.
- 태그는 `[추측: 커널 본문 안 봄]`, 커널 칸은 `[공백]`.
- run125 가 파견소 안을 처음 들여다봤으니, 그 칸으로 채울 수 있었다. **새 분석은 없다** — run125 문서 · sast/122 결과서에 있는 칸만 옮겼다.

#### 한 줄 → 네 줄 (run125 `[정적]`, 칸 이름은 PDB 에 없어 `[추측: 이름]`)

로더 풀 첫 넣기 `TppWorkPost +0x4f3` 의 `NtReleaseWorkerFactoryWorker` 한 번이 커널에서 바꾸는 것:

1. **메모판 `P`** (공장 `+0x10` 이 가리키는 `0x28`B)
   - `P+0x18` 넣은 일감 수 0 → 1 (`+0xfb`)
   - `P+0x20` "패킷이 포트에 있음" 0 → 1 (`+0x10c`)
2. **우편함** (완료 포트) — 네 칸이 0 인 빈 쪽지 하나. 기다리는 일꾼이 없으니 줄에 붙고 `SignalState` 0 → 1.
3. **장부의 일꾼 수 칸** — 그 자리에서 `+0x201 ExpWorkerFactoryCheckCreate`. 새 공장 첫 넣기는 조건 일곱이 모두 참 → `f+0x190` (만드는 중) ++ → `+0x19b ExpWorkerFactoryCreateThread` → `f+0x188` (Ready 전) ++.
4. **일꾼 스레드 하나** — `RtlpCreateUserThreadEx` → `ZwCreateThreadEx` (= `NtCreateThreadEx` 길). `CreateFlags 0x11` → `LoaderWorker`. 멈춘 채 만들고 `+0x153 ZwResumeThread` 로 같은 호출 안에서 푼다.

- 새 일꾼이 `NtWorkerFactoryWorkerReady` 를 부르면 `+0x190` · `+0x188` 이 내려가고 `+0x180` · `+0x184` 가 오른다 — 이건 **일꾼 스레드** 몫 (`② 일꾼`).
- 비유 (run125 해설 그대로): 관리실이 파견소 창구에 "한 명 보내 주세요" 쪽지를 낸다. 파견소는 메모판에 "넣은 쪽지 1" 을 적고, 우편함에 빈 쪽지를 넣고, 그 자리에서 둘러본다. "기다리는 사람 없고, 정원 남았네" → 바로 한 명 뽑아 보낸다.

#### 같이 고친 곳 — 모두 2장 숫자에 안 든다

- 4장 짝 `(run72) 로더 풀 +0x38 WorkerFactory` 줄의 커널 쪽 `[공백]` → `[정적]`
- 7.6.9 커널 줄 "`+0x198` bit 11 뜻 `[공백]`" → `LoaderWorker`
- 7.28 ② · 7.31 ④ `(해결됨)`, 7.28 **5** `(일부 해결)` (PDB 에 타입 없음 → `dq` 로 칸 오프셋, 값은 동적 그대로)
- 7.6 은 **체크리스트 밖**, 4장은 **짝 표** 다. 그래서 2장 숫자는 그대로다.

#### 작은 말 바로잡기

- 사용자가 말한 "3장 run72 커널 칸" 은 짐 지도 **4장 (두 공간 짝)** 의 `(run72)` 줄이다.
- 짐 지도 3장엔 run72 커널 칸이 없다. run125 문서 "결정할 것" 의 "3장" 은 4장을 가리킨 것이다 (run 문서).

#### 사진 맞춤 `[추측]` (run125 그대로)

- run44 `ActiveThreads` P1a 1 → P1b 3 (일꾼 2) 은 "첫 넣기 1명 + 연쇄 1명" 과 어긋나지 않는다.
- 어느 풀 · 어느 계기인지는 동적 (Q226).

### 2.11 사용자 결정 — 그림 2 에 상자를 안 넣는다

사용자 결정 2026-11-22 (plan.md 결정 기록) 을 짧게:

- **안 넣는 것 셋**:
  - Q124 (e) — `LdrpInitializeProcess` 의 새 callee 상자 (정책 · 현재 디렉터리 · CPU 기능 · 스케줄러 둘 · TLS 준비)
  - Q119 (e) — `LdrpEnableUMGLTracingStateSync` 상자
  - Q228 (가) — 커널 일꾼 공장 노드 (Q226 동적 뒤 다시 본다)
- **왜**: 그림 2 의 `LdrpInitializeProcess` 상자들엔 `(n)` 번호가 붙어 있다. 중간에 상자를 끼우면 그 뒤 번호가 **모두 밀린다**.
  - 그런데 이 구간은 칸 · syscall 만 늘고, 흐름 (모듈 · 스레드) 은 안 바뀐다 (run62 · run65 메모).
  - 그래서 글 (2.4 절) 과 짐 지도에만 적는다.
- **채우는 것 하나**: Q228 (나) — 위 2.10.
- 비유: 개업 순서도 (그림) 에는 큰 단계만 번호를 붙여 둔다. 작은 준비물은 점검표 (짐 지도) 에 적는다. 순서도에 끼워 넣으면 번호표를 다 새로 붙여야 해서다.

### 2.12 디버거가 붙으면 달라지는 곳 하나 `[정적]`

- `LdrpInitializeExecutionOptions +0x6c0 CMP [PEB+2]` (`BeingDebugged`) 가 0 이 아니면, `+0x6c6 ~ +0x715` 가 환경 변수를 읽어 `NtGlobalFlag |= 0x70` 하는 길을 탄다 (결과서).
- 커널 디버거만 붙은 run44 는 `BeingDebugged` 0 이라 안 탄다. 유저 디버거를 붙이면 탈 것이다 `[추측: 유저 디버거를 붙이면 탐]`.
- 그 길에서 변환이 실패하면 `ESI < 0` → `+0x72e JS` 로 `+0x733` 을 **건너뛸 수 있다.** 2.4 의 "닿는다" 가 디버거 때문에 바뀔 수 있는 자리다.

### 2.13 PE_emulator 쪽 — 지금 우리 맞나 (결과서)

`../PE_emulator` 코드는 flow-writer 가 안 읽었다. 고칠지는 사용자 결정이다.

| 질문 | 맞나 | 한 줄 |
|---|---|---|
| (a) `LdrpPolicyBits` | 맞음 | 공사 중 빈 함수 + 파일 값 `0x16` |
| (b) `+0x733` | 맞음 | "`RCX = RDI = 0`, KUSD `+0x2d5` = 0 → 0" 과 같음 |
| (c) TLS 블록 | 맞음 | 진짜 `LdrpInitializeTls` → `LdrpAllocateTls` 를 돌려 `0x11` 이 저절로 나옴. VM 파일이 같은지만 확인 필요 |
| (d) 섹션 크기 | 맞음 (알고 줄임) | 섹션 · 매핑 없음, `TEB+0x1850 = 0`. 만들 땐 `0x2000` 한 상수 |
| Q127 (a) 비트 3 | 맞음 | 진짜 `+0xce` 가 켬 |
| Q127 (b) shim 길 | 맞음 (결과 같음, 갈래만 다름) | 우리는 `PEB+0x2d8 = 0` 이라 **첫째** 문지기 (`+0x19da`) 에서, 진짜는 **둘째** (`+0x19e3`) 에서 돌아감. 둘 다 `LdrpInitShimEngine` 안 부름 |
| Q127 (c) `SwitchBackContext` | 확인 필요 | 블록 0 이라 `+0xd9` 안 탐 → 모듈 `+0xc0` 0 (사진은 값 있음) |

- 비유로 Q127 (b): 우리 에뮬레이터는 안내문 자체가 없어서 **첫 문지기** 에서 돌아가고, 진짜 VM 은 안내문은 있는데 `+0x210` 칸이 비어 **둘째 문지기** 에서 돌아간다. 결과 (어댑터 엔진 없음) 는 같다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 곳 (커밋 `cd5785c`)

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | — | pdb 줄 둘 · 근거 로그 줄 run130 (새 사진 읽기 없음, 앞 run 이 맞춘 줄을 다시 짚음) · PE 바이트 줄 (TLS 디렉터리) |
| 그림 메모 (run62 · run65) | "넣을지는 사용자와 정한다" | **사용자 결정 2026-11-22: 안 넣는다** + 새 run130 메모 (그림 자체는 그대로) |
| 2.4 `LdrpInitializePolicy` 줄 | 패키지 갈래에만 쓰는 줄 | + 쓰는 줄 다섯 다시 셈 (run130) |
| 2.4 `+0x733` | "닿는지 안 봤다" | **닿는다** `[정적]`, 0 은 KUSD 정책 때문 |
| 2.4 스케줄러 섹션 | 크기 셈 안 봄 | **커널 상수 `0x2000`** `[정적]` |
| 2.4 TLS 블록 | `0x11`B `[추측]` | 셈 줄 붙음 `[정적]`, `SizeOfZeroFill` 안 읽음 |
| 2.5 `LoadNotificationsSent` | "다른 줄 없는지는 안 봤다" | **`+0xce` 하나, 끄는 줄 0** `[정적]`. syscall 수는 `[추측]` 그대로 |
| 2.5 `+0xc0` | — | 쓰는 줄은 `+0xd9` 하나 |
| 2.5 shim 엔진 | 게이트 일부 (run118) | **문지기 둘 · 입구 셋 · `g_ShimsEnabled` 쓰는 줄 전수** `[정적]` |
| 3장 run125 절 | 노드 넣을지 결정 대기 | 결정 둘 표시 ((가) 안 넣음 · (나) 채움) |
| 4장 | run62 · run65 · run66 · run125 남은 줄 | 해당 줄 `(해결됨)` + 새 run130 줄 둘 (동적 · 정적) |

### 짐 지도 (`baggage.md`) 에서 바뀐 곳

- **2장 · 3장 숫자 그대로. 새 짐 칸 없음.**
  - 3장은 이미 있는 줄 10개의 경로 · 값 태그 · 근거만 고쳤다 (3.5 2 · 3.8 5 · 3.9 3).
  - `g_ShimsLoading` 두 값 · `g_ShimsEnabled +0x129` 값이 `[추측]` → `[정적]` 이 됐지만, 셋 다 이미 "정확 (줄)" 로 세던 줄이라 숫자는 그대로다.
- 3.8 · 3.9 shim 줄의 "지도 경로 모름" → `②… → LdrpInitShimEngine (호출 +0x19ef) → …` (문지기 `+0x19d7` · `+0x19dc`).
- 7.6.5: 스케줄러 섹션 `0x2000`, TLS 블록 셈 줄, **"0 채움" 정정**.
- 7.6.12: 커널 줄 한 줄 → 네 줄 (2.10). 4장 짝 · 7.6.9 · 7.28 · 7.31 표시.
- 7.24 · 7.25 · 7.28 · 7.31 `(해결됨)`, 새 7.54, 8장.

### 다른 run 문서에 붙은 한 줄

- **run65** — 닫힘 + 작은 정정: `+0x733` 은 **닿는다**. "마스크는 어느 쪽이든 0" → 0 은 맞지만 이유는 **KUSD 정책 0**. TLS 블록 `[추측]` → `[정적]`. 스케줄러 섹션 → 커널 상수 `0x2000`.
- **run66** — 닫힘: 켜는 줄 `+0xce` 하나. "모듈 다섯" 은 `[정적]`, syscall 수는 `[추측]` 그대로. shim 길 → 문지기 둘 · 입구 셋.
- **run125** — 닫힘: 짐 지도 7.6.12 · 4장 짝을 run130 에서 채움. 그림 노드는 지금은 안 넣음.

### 큰 그림과 이어지는 곳

- 이번 바퀴로 run65 · run66 이 남긴 정적은 **다 닫혔다.** 남은 건 "실제로 그 값이 나오나" 를 보는 동적 확인 (모두 선택) 뿐이다.
- 짐 지도 목표 쪽에서 보면: 새 짐은 없고, **이미 센 짐의 근거가 단단해졌다.** "아마 이럴 것" 이 "이 줄에 쓰여 있다" 로 바뀐 칸들이다.
- "이 VM 에서만 그런 값" 이 하나 드러났다 (2.4 의 마스크 0). 다른 컴퓨터 정책이면 `LdrpCheckAppDirType` 길이 열린다. 에뮬레이터가 KUSD 값을 바꾸면 이 길을 따라가야 한다.
- shim 엔진의 문지기 둘이 정해졌으니, "shim 이 걸린 exe" 를 돌리는 날 볼 곳은 블록 `+0x210` 을 **누가 쓰나** 하나로 좁혀졌다 (Q239 ①).
- 다음 차례: 사용자 결정 (2026-11-22) 대로 **VM 은 미루고, 정적은 필수만**. 이번 바퀴의 남은 것은 Q238 (동적) · Q239 (정적) 로 줄 섰다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| VM 의 test.exe TLS 디렉터리가 `sample/bin/test.exe` 와 같은가 | PE 바이트는 서버 파일로 읽었다. VM 파일이 같다는 건 가정 (sast/59 와 같음) | 동적 **Q238 ①** (exe 기준 `+0x79500` `dq … L5`) — VM 미룸 |
| 실제로 `RtlAllocateHeap` 에 `0x11` 이 넘어가나 | 정적 셈 + 사진 주소 맞춤 `[추측: 계산]` 까지 | 동적 **Q238 ②** (`LdrpAllocateTls+0xfa` `r r8`) |
| `LdrpQueryIllegalCWDDevices` 입구 `RCX` 가 정말 0, 마스크 0 인가 | 길은 정적으로 닿지만 실행 값은 아직 | 동적 **Q238 ③** |
| 커널이 정말 `0x2000` 으로 섹션을 만드나 | 상수는 코드에 있지만 실행 때 확인은 아직 (사진 2쪽과는 맞음) | 동적 **Q238 ④** (`PspSchedulerSharedDataRegionCreate+0x158` `r r8`, 프로세스 이름 조건) |
| shim 함수 셋이 정말 안 불리나 | 사진 값 (`g_Shims*` 0) 과 정적 길로만 앎 | 동적 **Q238 ⑤** (멈춤점이 **안 서면** 맞음) |
| 비트 3 이 켜지는 순간의 RIP 가 `+0xce` 인가 | 전수 검사는 ntdll · KernelBase · kernel32 코드만. `[reg+idx*N+0x68]` 꼴은 못 봄 | 동적 **Q238 ⑥** (`ba w4`) |
| apphelp syscall 이 정말 5번인가 | 알림 다섯은 닫혔지만, 알림마다 syscall 을 하는지는 `LdrpCheckModule` 안 조건이 따로 정함 → `[추측]` | 동적 — run66 동적 2 (히트 수) 와 같은 줄, **Q104** VM 단계 |
| 블록 `[pShimData+0x210]` 을 누가 쓰나 | 부모 쪽 (kernelbase · apphelp) 길이라 이번 범위 밖 `[공백]` | 정적 **Q239 ①** (미룸) — shim 이 걸린 exe 로 바뀌는 날의 문턱 |
| `LdrInitShimEngineDynamic` 의 진짜 호출자 | ntdll · kernel32 · KernelBase 엔 없다. 다른 DLL 은 안 봄 `[추측]` | 정적 **Q239 ①** 에 같이 |
| 섹션 `0x2000` 안 한 칸 크기 · 스레드마다 나누는 법 | 커널 `PspSchedulerSharedDataRegionSlotAllocate` · `…SlotFree` 본문을 안 봄 | 정적 **Q239 ②** (미룸) — PE_emulator 가 진짜 값을 만들 때 |
| DLL 쪽 TLS 블록 크기 | 식은 이번에 확정. DLL 들의 PE 바이트는 안 셈. test.exe 길에서 `LdrpHandleTlsData` 가 쓰이는지도 안 따짐 | 정적 **Q239 ③** (작음, 미룸) |
| `LdrpMapCleanModuleView +0xa8` · `AvrfMiniLoadDll +0x8f8` 의 `[reg+0xc0]` 가 모듈 항목인가 | 구조체 정체를 안 따라감 `[공백: 낮음]` | 정적 **Q239 ④** (낮음, 미룸) |
| run44 일꾼 2개가 어느 풀 · 어느 계기로 생겼나 | 정적으로는 "첫 넣기 1 + 연쇄 1" 과 어긋나지 않는다까지 `[추측]` | 동적 **Q226** (VM 미룸) |

- 동적은 모두 **선택** 이다 (결과서 "dast 필요 없음"). 커널 멈춤점은 모든 프로세스에 걸리니 test.exe 조건이 꼭 필요하다.

---

## 5. 더 읽기

- run 문서: [`run130-run65-66-leftovers.md`](run130-run65-66-leftovers.md) — 요약 표, 호출 지점 표, Q124 (b) 나무 전체, TLS 셈 표, shim 길 나무, 덤 절, PE_emulator 표, 아직 확인 필요
- 결과서: [`_workspace/sast/120_run65-66-leftovers_result.md`](../../_workspace/sast/120_run65-66-leftovers_result.md) — 질문마다 줄 근거, `LdrpQueryIllegalCWDDevices` 값 갈래, PE 바이트 원본
- 요청서: [`_workspace/sast/120_run65-66-leftovers.md`](../../_workspace/sast/120_run65-66-leftovers.md)
- flow.md: [2.4 `LdrpInitializeProcess` 절](flow.md) (정책 · CWD 마스크 · 스케줄러 · TLS 줄) · 2.5 올린 뒤 알림 절 (비트 3 · shim 게이트 · `+0xc0`) · 3장 run125 절 · 4장 run130 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.5 · 3.8 · 3.9 · 4장 (run72 짝) · 7.6.5 · 7.6.9 · **7.6.12** · 7.54
- 앞선 해설: [run65](run65-ldr-init-bodies-해설.md) (2.5 정책 · 2.6 마스크 · 2.9 스케줄러 · 2.10 TLS) · [run66](run66-apphelp-cache-해설.md) (2.12 "5번" 사슬) · [run118](run118-shimdata-read-해설.md) (2.6 shim 게이트) · [run125](run125-worker-factory-해설.md) (파견소) · [run114](run114-ntdll-globals-early-해설.md) (2.2 셈 기준) · [run123](run123-run58-60-leftovers-해설.md) (2.12 두 번 세지 않기)
- 대기열: `_workspace/plan.md` Q124 · Q127 · Q228 (끝) · **Q238** · **Q239** · Q226 · Q104

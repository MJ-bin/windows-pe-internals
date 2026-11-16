# run115 kusd-boot — 해설

> 이 글은 [run115 문서](run115-kusd-boot.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q78g1** — Q78g ("KUSD 와 남은 전역") 의 **KUSD 몫**: `_KUSER_SHARED_DATA` 모르는 칸 102 (`K1 이전` 61 · `끝까지 0` 41) 의 쓴 자리 (`_workspace/plan.md`, 이번에 **끝**)
  - Q78g 는 2026-11-16 에 그 자리에서 둘로 나뉘었다. 짝인 **Q78g2** (ntdll `.00cfg` · 남은 ntdll 전역) 가 다음 문서 차례다.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다. **짐 지도 바퀴**다.
  - 이번 바퀴로 **유저 구조체 모름이 0** 이 됐다 (3절).
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q79** (덤프 · 부팅 뒤 읽기로 조건 전역 값), **Q94** (부팅 중 멈춤점)
    - 정적 → 새 **Q204 ① ~ ⑤**
    - **Q192 ②** (run105 가 남긴 "`+0x308` 을 쓰는 커널 줄") 는 이번에 닫혔다.
- 근거:
  - run 문서 [`run115-kusd-boot.md`](run115-kusd-boot.md) (가장 중요)
  - 요청서 [`_workspace/sast/110_kusd-boot.md`](../../_workspace/sast/110_kusd-boot.md) · 결과서 [`110_kusd-boot_result.md`](../../_workspace/sast/110_kusd-boot_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 그림 1 은 **`SYSCALL` 화살표 꼬리표 한 마디만**. 글은 기준 · 2.2 스텁 절 · 2.3 KUSD 절 (+ 새 소절) · 3장 bcryptprimitives 절 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): 쓰는 말 (`ⒷR`) · 1장 · **2장 숫자** · 3장 줄임 표 · **3.7** · 4장 · 5장 `Ⓑ` · 7.1 · 7.2 · 7.5 · 7.29 · 7.37 · 새 7.46.
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 `[관찰]` 은 모두 **run44 때 찍어 둔 사진** 8장의 KUSD 구획 (`ms/logs/04_baggage-read_supp_<K1 ~ P2>.txt` `:182 ~ :285`) 을 다시 읽은 것이다.
  - 쓰는 줄은 거의 다 **흐름도 범위 앞** (부팅) 이다. 그래서 그림은 거의 안 바뀌었다.
- **run 문서가 결과서와 다르게 적은 곳이 있다** (2.14). 이 글은 **run 문서** 를 따른다.
  - 단계 줄임 `ⒷL` (결과서 제안) → **`ⒷR`** (이미 있던 `ⒷL…` 과 겹쳐서).
  - 경로 꼴: 결과서의 `함수 +0xN → 다음 함수` → 짐 지도 관례 `→ 다음 함수 (호출 +0xN)`.

---

### 이미 푼 말 — 링크만

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- `KUSER_SHARED_DATA` (KUSD) = 관리사무소가 모든 가게 앞에 붙여 두는 **공용 게시판** → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) · [run70 해설](run70-cpu-features-해설.md) "비유"
- 게시판의 `ProcessorFeatures` = **기본 기능 64칸 체크리스트**, 그걸 쓰는 두 모양을 "CCTV 두 각도" 로 훑은 이야기 → [run70 해설](run70-cpu-features-해설.md) 2.7 ~ 2.9
- **BSP** (부팅 때 가장 먼저 일어나는 프로세서 0) · AP (나머지) → [run70 해설](run70-cpu-features-해설.md) 2.7
- 게시판 `+0x308` `SystemCall` 칸, 스텁 494곳, `SYSCALL` = **보통 문** / `INT 0x2e` = **옛 쪽문** → [run105 해설](run105-ntdll-small-해설.md) 2.3
- 부팅 단계 `Ⓑ` = 관리사무소가 **문 여는 날** → [run48 해설 2.3](run48-system-dll-boot-해설.md)
- **위상** (phase) = 문 여는 날의 **교시** (0 교시, 1 교시 …), `MmInitSystem` 이 교시마다 와서 그 교시 일만 함 → [run64 해설 2.1](run64-apiset-phase-해설.md)
- 네 가지 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길) → [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법)
- 사진 지점 K1 ~ P2, 사진 구간 (`K1 이전` · `끝까지 0` …) → [run44 해설](run44-baggage-snap-해설.md) 2.8 · 2.12
- **IAT** = 벽 **콘센트** (다른 모듈 함수 주소를 꽂아 두는 칸). "IAT 로 부른다" = 그 콘센트에 꽂힌 주소로 건너가 부른다 → [run19 해설 2.7](../02_sprint/run19-proc-init-modules-해설.md) · [run30 해설](../02_sprint/run30-exe-crt-start-해설.md)
- **드라이버** = 관리사무소 건물 안에 입주한 **전문 상담실** (nt 본체 밖, 같은 커널 공간) → [run66 해설](run66-apphelp-cache-해설.md)
- win32k = 창 그리기 담당 부서 → [run85 해설](run85-user32-init-해설.md)
- smss = 부팅 때 가장 먼저 뜨는 유저 프로세스 (관리인) `[추측]` → [run98 해설](run98-rpc-dir-create-해설.md)
- SRW 락 = **열쇠** → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.4 · 2.13
- 시계 인터럽트 = 벽시계가 **째깍** 할 때 → [run76 해설](run76-thread-quantum-해설.md)

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **쓰기 별칭** (writable alias) · **고정 별칭** → 2.2
- **nt 공개 함수** → 2.7
- **`ⒷR`** (부팅 뒤 · 프로세스 생성 앞) → 2.6
- **스핀** (락을 기다릴 때) → 2.13
- 그 밖에 칸 이름 (`TickCountMultiplier` · `ActiveConsoleId` · `UnparkedProcessorCount` …) 은 그 자리에서 한 줄씩.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**.
- KUSD = 관리사무소가 모든 가게 앞에 붙여 두는 **공용 게시판**. 가게는 읽기만 한다.
- 부팅 = 관리사무소가 **문 여는 날**, 위상 = 그날의 **교시**.
- **바꿔 부르는 것 셋.** run 문서는 이번에 KUSD 를 "동네 게시판", nt 를 "관청" 으로 불렀다. 앞 해설들과 섞이지 않게 이렇게 옮긴다:
  - **관청 → 관리사무소** (run11 부터 쓴 말).
  - **다른 관공서 (win32k · cng 같은 드라이버) → 건물 안 전문 상담실** (run66).
  - **관청 창구 (nt 공개 함수) → 게시판 담당 창구.**
- run 문서의 그림 하나는 그대로 가져온다: 관리사무소가 게시판에 쓰는 **펜은 하나** 다 (2.2).

### 한 줄로 먼저 — 무엇을 얻었나

| 물음 | 답 (run 문서 그대로) |
|---|---|
| 게시판 모르는 칸 102 는? | **모름 0.** 아는 칸 80 · 아무도 안 씀 17 · 범위 밖 길 `[추측]` 5 (2.1) |
| 관리사무소는 게시판을 몇 가지 길로 쓰나? | **하나.** 전역 포인터 `MmWriteableSharedUserData` 로만 쓴다. 고정 주소 `0xFFFFF780…` 로 쓰는 줄은 0 `[정적]` (2.2) |
| 언제 쓰나? | 거의 다 **문 여는 날** (부팅 `Ⓑ`), 몇 칸은 **문 연 뒤 · 가게 생기기 전** (`ⒷR`), 시계 칸은 **계속** `[정적]` (2.4) |
| test.exe 가게가 문 여는 동안 새로 쓰나? | 쓰는 줄을 **못 찾았다** (호출 사슬로 안 닿음까지). 사진에서도 바뀌는 건 시계 칸 8개뿐 `[정적]` + `[관찰]` (2.4 · 2.12) |
| `ActiveConsoleId` 는 csrss 가 시스템 콜로 쓰나? | **아니다.** win32k **드라이버** 가 nt 공개 함수를 IAT 로 부른다 — 요청서 짐작 정정 `[정적]` (2.7) |
| `SystemCall` (`+0x308`) 은 누가 쓰나? (run105 숙제) | **`KiInitializeKernel +0x8e`**. 조건 둘이 안 맞아 이 VM 에선 **안 탐** → 0 `[정적]` + `[관찰]` (2.8) |
| run70 이 남긴 `[추측]` 은? | `InitBootProcessor +0x406` 의 `R15B` = **늘 1** 로 확정 `[정적]` (2.11) |
| 짐 지도 2장 | 구조체 모름 **126 → 24**. 유저 구조체 모름 **0**, 남은 24 는 모두 커널 (3절) |

이 글에서 붙잡아 둘 세 줄:

> **(1) 공용 게시판은 문 여는 날에 거의 다 적힌다.**
> test.exe 가 문 여는 동안 (`NtCreateUserProcess` ~ `NtContinue`) 새로 적는 줄은 못 찾았다. 바뀌는 건 시계 칸뿐이다.
>
> **(2) 관리사무소의 게시판 펜은 하나다.**
> `MmWriteableSharedUserData` (쓰기 별칭) 하나로만 쓴다. 그래서 run70 · run105 가 "고정 주소" 각도로 훑었을 때 쓰는 사람이 안 보였던 것이다.
>
> **(3) 짐 지도 유저 쪽이 다 찼다.**
> 유저 구조체 일곱의 모름이 0 이 됐다. 남은 구조체 모름 24 칸은 모두 커널이다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 거의 다 **흐름도 범위 앞** 이다. test.exe 가 있기 훨씬 전, 컴퓨터가 켜질 때다.

```
(문 여는 날 = 부팅 Ⓑ — 흐름도 범위 앞)
 0 교시 (위상 0, BSP)   entry → KiInitializeKernel → InitBootProcessor …            ← 이번 (대부분)
 1 교시 (위상 1)        Phase1Initialization → Phase1InitializationDiscard · IoInitSystem …  ← 이번
(문 연 뒤 · 가게 생기기 전 = ⒷR)
 smss 의 NtInitializeRegistry · 첫 사용자 스레드 · 드라이버 (win32k · cng …)        ← 이번
(계속) 벽시계 째깍 → KeClockInterruptNotify → KiUpdateTime                         ← 이번 (시각 8칸)

cmd.exe 의 부탁 → NtCreateUserProcess → … (사진 K1 ~ P2) … → NtContinue 은퇴 → (CRT) → main
 ├ 이 구간 안에서 게시판을 새로 쓰는 줄은 못 찾음 (시계 칸만 바뀜)
 ├ test.exe 첫 스레드도 PspNotifyThreadCreation +0xb4 를 지나지만 Cookie 는 안 씀    ← 이번 (2.10)
 └ ntdll 스텁 494곳이 게시판 +0x308 을 읽고 SYSCALL 길로 감 (그림 1 꼬리표)        ← 이번 (2.8)
```

- 나무의 오프셋은 call 자리다 (run 문서 맨 위 메모). 부팅 길이라 콜스택으로 본 적이 없어서 결과서 꼴 그대로 둔다.
- 흐름도 그림 1 에서 바뀐 건 **`SYSCALL` 화살표 꼬리표 한 마디** 뿐이다. 새 상자는 없다.

### 무엇이 궁금했나

짐 지도 7.2 에 공용 게시판 (KUSD) 의 "모르는 칸" 이 **102** 남아 있었다. 유저 쪽 구조체 모름은 이것뿐이었다 ([run113 해설](run113-ntdll-user-structs-해설.md) 3절).

- `K1 이전` 61칸 — 사진 첫 장 (K1) 에 이미 값이 있다. **누가 언제 썼나?**
- `끝까지 0` 41칸 — 사진 8장 내내 0 이다. **네 가지 가름 중 어디인가?**

요청서의 질문은 다섯이었다 (run 문서 "목적"):

1. 쓰는 줄 · 값의 출처 · 지도 경로
2. `끝까지 0` 41 을 넷으로 가르기
3. 쓰는 길이 커널 밖에서 시작하면 "놓는 법 · 쓴 주인 · 부르는 쪽"
4. 간접 호출이면 좁히기
5. (1 과 같이) 지도 경로

### 바퀴 전에 알던 것

- `[정적]` (run70) 게시판 103칸 중 **`ProcessorFeatures` 하나** 는 쓰는 쪽을 안다. 부팅 때 커널 함수 넷이 28칸만 쓴다. 훑기는 **두 모양** (포인터 `MmWriteableSharedUserData` + 거리 · 절대 주소 `0xFFFFF780000002xx`) 으로 했다.
- `[정적, 한계 있음]` (run105) 스텁이 읽는 `+0x308 SystemCall` 칸 — 쓰는 커널 줄은 **"이 방법으로는 안 보임"**. 고정 주소 `0xfffff78000000XXX` 를 레지스터에 올리는 1026곳을 훑었는데 0곳이었다.
- `[관찰]` (run44) 사진 8장에서 게시판 값은 **시각 칸 8개만** 바뀐다. "값은 부팅 때 정해진 것" 은 `[추측]` 이었다.
- 실마리 몇 개: (run89) `ImageNumberLow/High` · `Cookie` 를 읽는 .NET 갈래, `ExpReadComPlusPackage` 가 `ComPlusPackage` 를 쓰나 · (run91) `RNGSeedVersion` 을 읽는 bcryptprimitives · (run83) `DbgConsoleBrokerEnabled`. 모두 **쓰는 쪽은 모름** 이었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 102칸은 어디로 갔나

run 문서 "요약 표" 그대로다.

| 구간 | 칸 | nt 가 부팅 때 직접 | nt 가 쓰고 부르는 쪽이 부팅 뒤 다른 모듈·프로세스 | 시각 `*` | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 | 범위 밖 길 `[추측]` | 모름 |
|---|---|---|---|---|---|---|---|---|---|
| `K1 이전` | 61 | 44 | 9 | 8 | — | — | — | — | 0 |
| `끝까지 0` | 41 | — | — | — | 8 | 11 | 17 | 5 | 0 |
| **합** | **102** | **44** | **9** | **8** | **8** | **11** | **17** | **5** | **0** |

- **아는 칸** 은 쓰는 줄이 있는 칸이다: `K1 이전` 61 + 0 을 씀 8 + 조건부 안 탐 11 = **80** (run49 · sast/46 방식 — "0 을 씀" · "조건부 안 탐" 도 쓰는 줄이 있으니 아는 칸).
  - 여기에 run70 의 `ProcessorFeatures` 1 을 더하면 게시판 103칸 중 아는 칸 **81** 이다. 셈 `1 + 61 + 8 + 11 = 81`. 모두 **정확** (짐작 0).
  - 짐 지도 메모의 "아는 칸 80" 은 이번에 새로 생긴 몫, 2장 표의 "81" 은 run70 것을 더한 합이다. 서로 어긋난 숫자가 아니다.
- 나머지는 아무도 안 씀 **17** + 범위 밖 길 **5** = 22.
- "시각 `*`" 8칸 = 계속 바뀌는 **시계 칸** 이다. 사진에서 "소음" 으로 뺐던 칸들이다.
- "nt 가 부팅 때 직접 44" = **부팅 때 첫 쓰는 줄이 nt 안** 이라는 뜻이다. 그중 몇 칸 (`NtBuildNumber` · `TimeZoneBiasStamp` · `LangGenerationCount` · `ComPlusPackage` · `KdDebuggerEnabled` · `SharedDataFlags`) 은 부팅 뒤에도 다시 쓰는 줄이 있다 (run 문서).

### 2.2 핵심 — 게시판에 쓰는 길은 하나 (`MmWriteableSharedUserData`)

이게 이번 바퀴에서 가장 큰 발견이다. 다른 결과가 다 여기 기대고 있어서 먼저 푼다.

#### 새 말 — 쓰기 별칭, 고정 별칭

- **새 말 — 별칭** (alias) = **같은 물리 쪽을 가리키는 또 다른 주소**. 주소는 달라도 가리키는 종이는 한 장이다.
- **새 말 — 쓰기 별칭** (writable alias) = 그런 두 번째 주소 중 **쓰기가 되는 쪽**. 다른 하나는 읽기만 된다.
  - 비유: 게시판은 **유리 진열장** 이다. 앞 유리 (가게 쪽) 로는 보기만 한다. 관리사무소 안쪽에 **뒷문** 이 하나 있고, 그 뒷문으로만 손을 넣어 적을 수 있다. 뒷문이 쓰기 별칭이다.
- **고정 별칭** = 커널이 늘 쓰던 정해진 주소 `0xfffff78000000000`. run105 · run44 가 사진을 읽은 주소가 이것이다.

#### 무엇을 찾았나 `[정적]`

1. 24H2 커널은 게시판을 전역 포인터 **`MmWriteableSharedUserData`** (nt+`0xfc5008`) 로만 쓴다.
   - 이 포인터를 읽는 곳은 전부 **200곳 = 77 함수**. sast/65 (run70) 의 `refs` 200줄과 같은 수라 잘린 게 아니다.
2. 이 포인터의 **PE 처음 값** (파일에 박혀 온 값) 은 고정 별칭 `0xfffff78000000000` 이다.
3. 부팅 0 교시에 **`MiInitNucleus +0x44d → MiProtectSharedUserPage +0x1a4`** 가 **쓰기 별칭을 새로 만들어 포인터를 바꾼다.**
   - 같은 함수가 기능 스위치 `Feature_KernelSharedUserDataAslr` 사용 보고를 부른다. 이름으로 보아 게시판 주소 무작위화 `[추정]`.
   - 고정 별칭은 그 뒤 읽기 전용으로 보인다 `[추정]`. (`MiProtectSharedUserPage` 는 `.asm` 이 없어 결과서 objdump 로만 봤다 — 4절 Q204 ⑤.)
4. 고정 별칭 (`movabs 0xfffff780000000xx`, **1,294줄**) 을 담은 레지스터로 **쓰는 줄은 0**.
   - 걸린 1줄은 오탐이었다: `RtlGetSystemGlobalData` 의 **읽기** 조각 `FUN_1406f0d88 +0x305`.
5. 쓰는 함수 **54개**, 쓰는 줄 **131곳** (`.asm` 과 하나씩 대조 **144/144** 일치 — 한 줄이 겹치는 칸에 여러 번 나와서 144번). `ProcessorFeatures` 28줄은 run70 몫이라 뺐다.
   - 가장 많이 쓰는 함수: `InitBootProcessor` 17칸 · `RtlSetSystemGlobalData` 10칸 · `KiUpdateTime` 8칸 · `Phase1InitializationDiscard` 7칸 · `KiSetupTimeIncrement` 7칸.

#### 그래서 — run70 · run105 가 "안 보임" 이었던 까닭

- run70 은 CCTV 를 **두 각도** 로 돌렸다: ① 포인터 + 거리 ② 절대 주소 `0xFFFFF780000002xx`.
- run105 는 ② 각도 (고정 주소를 레지스터에 올리는 1026곳) 로 `+0x308` 을 찾았다.
- 그런데 이번에 보니 **② 각도는 쓰기에선 아예 빈 각도** 였다 `[정적]`. 고정 주소로는 읽기 · 잠금 주소 넘기기만 한다.
  - 비유: 앞 유리 쪽 CCTV 에는 **적는 사람이 원래 안 찍힌다.** 적는 사람은 모두 뒷문으로 들어오니까.
- 이번 훑기는 ① 쪽을 더 넓혔다: `MmWriteableSharedUserData` 를 읽은 레지스터를 **복사 · `lea` · `add` 까지 따라가** 만든 `[칸]` 이 **명령의 목적지** 인 줄 (`kscan.py`). 포인터를 메모리에 넣은 13곳 · 칸 주소를 인자로 넘긴 `call` 86곳은 손으로 가렸다.
- 한계 (run 문서 "방법"): 레지스터 추적이라 분기 합치기 · 호출 뒤 휘발 레지스터를 안 따라가 **오탐 · 누락 가능**. 컨테이너 쪽 게시판 사본 (`PspSiloInitializeUserSharedData`) 은 안 따라갔다. nt 밖 모듈 안에서 직접 게시판을 쓰는 일은 못 본다.

### 2.3 한 줄씩 읽는 법

짐 지도 3.7 의 줄 셋을 "누가 / 어디서 / 무엇을 / 왜" 로 풀어 읽는다.

#### 한 줄 읽기 ① — `TickCountMultiplier` (`+0x4`)

짐 지도 3.7 줄의 경로 칸:

```
ⒷI… +0x384
```

- **`ⒷI…`** = 짐 지도 3장 줄임 표의 새 줄임 (run115): `ⒷK… → InitBootProcessor (호출 +0x7fe)`. 풀면 `entry → KiInitializeKernel (호출 +0x27e) → InitBootProcessor (호출 +0x7fe)` 다.
- **누가**: `InitBootProcessor` — 0 교시 (위상 0) 본체, BSP 에서.
- **어디서**: 그 함수 안 `+0x384` 줄이 게시판 `+0x4` 에 쓴다.
- **무엇을**: `KeMaximumIncrement` (156250 = 15.625ms) 를 24비트 소수로 바꾼 값 = `0xfa00000` (`+0x306 ~ +0x35a` 고리). 같은 값을 전역 `ExpTickCountMultiplier` 에도 둔다 (`+0x379`).
- **맞춰 보기**: 사진 `:184` `TickCountMultiplier 0xfa00000` `[관찰]` — 계산과 같다.
- 비유: 문 여는 날 0 교시에 관리사무소 직원이 게시판 4번 칸에 "째깍 한 번 = 이만큼" 환산표를 적는다.

#### 한 줄 읽기 ② — `ActiveConsoleId` (`+0x2d8`)

짐 지도 3.7 줄의 경로 칸 앞부분:

```
RtlSetActiveConsoleId +0x18 ← (IAT 호출 자리) win32kbase Win32kBaseDriverEntry +0xcd9 · InitVideo +0x1d5 …
```

- **누가 쓰나**: nt 의 `RtlSetActiveConsoleId` 안 `+0x18` 줄. 그러니 **쓴 주인 = nt** 다.
- **누가 부르나**: nt 안에서는 아무도 안 부른다 (부르는 줄 0). 부르는 쪽은 **win32kbase.sys · win32kfull.sys** 드라이버다. 그 드라이버의 IAT 콘센트에 꽂힌 nt 함수 주소로 건너온다.
- **언제**: `ⒷR` — 부팅 뒤 · 가게 생기기 전 (2.6). 세션을 켤 때 · 원격 접속 때 등.
- **맞춰 보기**: 사진 `:222` `ActiveConsoleId 1` `[관찰]`.
- 비유: 건물 안 창 그리기 상담실 (win32k) 이 게시판 담당 창구에 와서 "활성 콘솔 번호 1 로 적어 주세요" 한다. 펜을 쥔 건 여전히 관리사무소다.

#### 한 줄 읽기 ③ — `UnparkedProcessorCount` (`+0x36a`)

```
ⒷI… +0x4c3 · ⒷP… → KeStartAllProcessors (호출 +0x91f) → KiUpdateProcessorCount (호출 +0x419) +0x9b
```

- **처음**: 0 교시 `InitBootProcessor +0x4c3` 이 **1** 을 쓴다 (BSP 하나).
- **그다음**: 1 교시 `Phase1InitializationDiscard` 가 다른 프로세서 (AP) 를 올릴 때마다 `KiUpdateProcessorCount +0x9b` 가 **+1** 한다.
- 이름으로 보아 "쉬지 않는 (unparked) 프로세서 수" 다 `[추측: 이름]`. 부팅 뒤엔 코어를 쉬게 하면 `KiParkCurrentProcessor +0x1b1` 이 −1, 깨우면 `KiUnparkCurrentProcessor +0x9a` 가 +1 한다 (짐 지도 3.7).
- **맞춰 보기**: 사진 `:264` `UnparkedProcessorCount 2` `[관찰]` = BSP 1 + AP 1.
- 이 칸은 ntdll 이 **읽어서 길을 가른다.** PE_emulator 쪽에서 중요해서 2.13 에서 다시 본다.

### 2.4 언제 쓰나 — 문 여는 날 · 문 연 뒤 · 계속

run 문서 "호출 관계" 나무를 대표만 줄였다 (오프셋은 call 자리).

```
[0 교시 — 위상 0, BSP]
entry (= KiSystemStartup)
 ├─ +0x1b5  KiInitializeBootStructures      ⒷS   NX 정책 (+0x370) · EnclaveFeatureMask (+0x382, SGX 만 — 안 탐)
 ├─ +0x1d1  KdInitSystem(0)                       KdDebuggerEnabled = 1
 ├─ +0x1e8  KiInitializeXSaveConfiguration  ⒷX   XState 840B 통째 복사
 └─ +0x27e  KiInitializeKernel              ⒷK   +0x8e SystemCall (조건 둘 — 안 탐) · TestRetInstruction · VirtualizationFlags
      └─ +0x7fe  InitBootProcessor          ⒷI   17칸 (버전 · 아키텍처 · BootId · 프로세서 수 셋 · TickCountMultiplier …)
           ├─ +0x2b0  KeInitializeClock(0) ─ +0x4e5 KiSetupTimeIncrement   QPC 칸 · Baseline 둘
           ├─ +0x595  MmInitSystem(0) ─ +0xb1 MiInitNucleus           ⒷM   쓰기 별칭 (+0x44d) · NtProductType
           │                           └ +0x144 MiInitSystem(0)             물리 쪽 수 둘 · LargePageMinimum 0
           └─ +0x5f5  EtwInitialize(0) …                                    TelemetryCoverageRound 1

[1 교시 — 위상 1, 시스템 스레드]
Phase1Initialization
 ├─ +0x1e  Phase1InitializationDiscard      ⒷP   HAL 타이머 · 시간대 · SystemTime · 프로세서 수 +1 · NtProductType …
 ├─ +0x36  IoInitSystem ─ … ⇢ (콜백) IopMarkBootPartition +0x1ca IopStoreBootDriveLetter   NtSystemRoot
 └─ +0x61  Phase1InitializationIoReady ─ +0x1d5 PsInitSystem(2) ─ +0x3b PspInitPhase2      MitigationPolicies (SEH · CurDir)

[문 연 뒤 · 가게 생기기 전 — ⒷR]
NtInitializeRegistry (smss [추정]) ─ +0xa7 CmCompleteRegistryInitialization ─ +0x13f PsBootPhaseComplete   SharedDataFlags 비트 1·2·3
PspUserThreadStartup ─ +0x9c PspNotifyThreadCreation +0xb4                                              Cookie (0 일 때만)
win32kbase · win32kfull · cng · condrv · kdnet ┄(IAT)▶ RtlSetActiveConsoleId · RtlSetSystemGlobalData …

[계속 — 벽시계 째깍]
KeClockInterruptNotify +0x2ba KiUpdateTime   시계 칸
```

- 새 줄임 넷 (`ⒷX…` · `ⒷI…` · `ⒷM…` · `ⒷP…`) 은 짐 지도 3장 줄임 표에 들어갔다. 이미 있던 `ⒷS…` · `ⒷK…` 은 run70 것이다.
- `MmInitSystem` 은 교시마다 와서 다른 일을 한다 ([run64 해설 2.1](run64-apiset-phase-해설.md) 의 "같은 담당자, 교시마다 다른 일") `[정적]`:
  - `(−1)` = `KiInitializeBootStructures +0x2ad` — 게시판 칸 안 씀.
  - `(0)` = `InitBootProcessor +0x595` → `+0xb1 MiInitNucleus` (쓰기 별칭 · `SafeBootMode` · `NtProductType`) → `+0x144 MiInitSystem(0)` (물리 쪽 수 · `LargePageMinimum` 0).
  - `(1)` = `Phase1InitializationDiscard +0xc65` → `+0xf0 MiInitSystem(1)` (`LargePageMinimum` `0x200000`).
  - run64 의 갈래 표와 같다. 앞 문서에서 틀리게 적은 곳은 못 찾았다 (run 문서 "결과서와 다르게 적은 곳").
- **HAL 은 nt 안에 있다** `[정적]`. hal.dll 은 코드 없는 껍데기다 (구역 `.rdata` · `.data` · `.edata` · `.rsrc`, `0x6000`B). 그래서 `QpcBias` · `QpcData` · `QpcBypassEnabled` 는 nt 가 자기 공개 함수 `RtlSetSystemGlobalData` 를 부르는 꼴이다.
- 그런데 `HalInitSystem → HalpTimerInitSystem` 은 **표 호출** 이라 `[추정]` 이다. 이 칸들이 어느 교시에 쓰이는지는 Q204 ① 몫 (4절).

#### 시각 `*` 8칸 — 처음 값과 계속 고치는 쪽

- 처음 값: Baseline 둘은 `KiSetupTimeIncrement +0x135` · `+0x143` (0 교시 길에서만 닿음). `SystemTime` 은 `KeSetSystemTime` (`ⒷP +0x743`). 나머지 여섯은 **첫 째깍** (`KeClockInterruptNotify +0x2ba → KiUpdateTime`).
- 계속 고치는 쪽은 이름만: `KiUpdateTimeAssist` · `KiUpdateSystemTime` · 디버거 얼림 · 깨움 (`KeFreezeExecution` / `KeThawExecution`) · 절전 복귀 (`KeRebaselineSystemTime` / `KeRebaselineInterruptTime`).
- 비유: 게시판 한구석에 **벽시계** 가 붙어 있다. 문 여는 날 처음 시각을 맞추고, 그 뒤로는 째깍할 때마다 바늘이 저절로 간다.

#### test.exe 가 문 여는 동안은?

- `[정적]` test.exe 길 (`NtCreateUserProcess` ~ `NtContinue`) 안에서 게시판을 새로 쓰는 줄은 **못 찾았다.**
- 다만 "안 돈다" 를 증명한 건 아니다. "호출 사슬로 안 닿는다" 까지다 (flow.md 2.3 새 소절).
- 사진 쪽과도 맞는다 `[관찰]`: K1 → P2 사이에 바뀌는 칸은 시각 8칸뿐.
- 그래서 짐 지도 3.7 의 "값은 부팅 때 정해진 것 `[추측]`" 이 **`[정적]`** 이 됐다 (부팅 `Ⓑ` 52칸 · 부팅 뒤 `ⒷR` 9칸 — 둘 다 test.exe 프로세스 생성 앞).

### 2.5 `K1 이전` 61칸 — 값은 어디서 왔나

run 문서 2절 표를 대표만 줄였다. 칸마다 쓰는 줄 · 경로는 짐 지도 3.7.

| 값의 출처 | 대표 칸 |
|---|---|
| 상수 (코드에 박힘) | `ImageNumberLow` · `High` (`0x8664`) · `NtMajorVersion` (`0xa`) · `NtBuildNumber` 첫 값 (`0x65f4`) · `TestRetInstruction` (`0xc3`) · `CyclesPerYield` 첫 값 (`0xa`) |
| 로더 블록 (부트 로더가 넘겨준 짐 꾸러미) | `BootId` · `NtSystemRoot` 의 `\WINDOWS` · `NXSupportPolicy` · (조건) `SafeBootMode` · `KiSystemCallSelector` |
| 레지스트리 | `SharedDataFlags` 비트 1·2·3 · `NtProductType` · `SuiteMask` · `ComPlusPackage` · `TimeZoneBias*` · `NtBuildNumber` 둘째 |
| CPU · 하이퍼바이저 | `XState` (CPUID 리프 0xd) · `VirtualizationFlags` · `QpcFrequency` · `QpcBias` · `QpcBypassEnabled` |
| 계산 | `TickCountMultiplier` · `NumberOfPhysicalPages` · `CyclesPerYield` 둘째 · `Cookie` (여러 값을 섞음) |
| 다른 모듈이 값을 정해 nt 공개 함수로 넘김 (`ⒷR`) | `RNGSeedVersion` (cng) · `ActiveConsoleId` · `ConsoleSessionForegroundProcessId` · `LastSystemRITEventTickCount` (win32k) · `KdDebuggerEnabled` 둘째 비트 (kdnet) |

- "로더 블록" 은 run 문서 표의 `LOADER_PARAMETER_BLOCK` 이다. "부트 로더가 넘겨준 짐 꾸러미" 는 이 글의 풀이다 `[추측]` (일반적으로 알려진 설명 — 커널이 켜질 때 받는 부팅 정보 묶음).
- 에뮬레이터에서 보면: 상수 · 계산 줄은 **그대로 다시 셈할 수 있고**, 로더 블록 · 레지스트리 · CPU · 다른 모듈 줄은 **입력** 이다.

### 2.6 새 말 — `ⒷR` (문 연 뒤 · 가게 생기기 전)

- **새 말 — `ⒷR`** = **부팅이 끝나고 돌아가는 동안, test.exe 프로세스가 생기기 전** (짐 지도 "쓰는 말"). R 은 "돌아가는 동안" 이라는 뜻으로 붙였다 (짐 지도 "쓰는 말").
  - smss · csrss · winlogon 이 부른 시스템 콜, 첫 사용자 스레드, 드라이버가 nt 공개 함수를 부르는 때.
  - 비유: 관리사무소가 **문은 열었는데 아직 손님 가게 (test.exe) 는 안 들어온** 때.
- 사슬 줄임이 아니라 **때** 라서 끝에 `…` 를 안 붙인다. 단계 칸에도 쓴다.
- 짐 지도 범위 (① 커널 · ② ntdll) **앞** 이라 ① 에서 보면 입력이다 — `Ⓑ` 와 같은 규칙 (run70).
  - 그래도 KUSD 는 체크리스트 구조체라서 **2장 숫자에 넣는다.**
- 시계 칸의 계속 갱신은 `ⒷR` 이 아니라 `Ⓑ→계속` 으로 적는다.

#### 왜 결과서의 `ⒷL` 을 `ⒷR` 로 바꿨나 (run 문서 "결과서와 다르게 적은 곳")

1. 결과서는 이 때를 `ⒷL` 로 제안했다.
2. 그런데 짐 지도 3장 줄임 표에는 이미 **`ⒷL…`** 이 있다 — run48 의 `Ⓑ… → PsLocateSystemDlls → PspLocateSystemDll` 사슬 (시스템 DLL 찾기).
3. 같은 글자를 두 뜻으로 쓰면 헷갈린다. 그래서 **`ⒷR`** 로 바꿨다.
4. 나머지 넷 (`ⒷI` · `ⒷM` · `ⒷX` · `ⒷP`) 은 겹치는 게 없어 그대로, 짐 지도 관례대로 `ⒷI…` 꼴로 넣었다.

### 2.7 정정 — `ActiveConsoleId` 류는 시스템 콜이 아니다

여기는 요청서의 짐작이 틀린 곳이다. 천천히 간다.

#### 새 말 — nt 공개 함수

- **새 말 — nt 공개 함수** = nt 가 **다른 커널 모듈이 부를 수 있게 내보낸 함수** (export). 드라이버는 이걸 자기 IAT 콘센트에 꽂아 두고 부른다.
  - 비유: 관리사무소 1층의 **게시판 담당 창구**. 건물 안 상담실 (드라이버) 이 와서 "이 칸 이렇게 적어 주세요" 하면 창구 직원이 적는다.
  - 시스템 콜과 다른 점: 시스템 콜은 **가게 (유저 프로세스)** 가 관리사무소 문을 두드리는 것이다. 공개 함수 호출은 **이미 건물 안에 있는 상담실** 이 옆 창구로 걸어가는 것이다.

#### 요청서 짐작과 실제

- 요청서 짐작: `ActiveConsoleId` 는 csrss · winlogon 이 **시스템 콜로** 쓴다.
- 실제 `[정적]`: nt 공개 함수 `RtlSetActiveConsoleId` (`+0x18` 이 쓰는 줄) 를 **win32kbase.sys · win32kfull.sys (커널 드라이버)** 가 IAT 로 부른다. nt 안엔 부르는 줄이 0 이다.
- 같은 꼴 (드라이버가 nt 공개 함수 호출 — syscall 아님) `[정적]`:
  - `ConsoleSessionForegroundProcessId` ← win32kfull
  - `LastSystemRITEventTickCount` ← win32kbase (입력 이벤트마다)
  - `RNGSeedVersion` ← cng.sys (`RtlSetSystemGlobalData +0x14f`, IAT 호출 rva `+0x300b5` · `+0x61085`, PDB 없음)
  - `DbgConsoleBrokerEnabled` ← condrv (`CdCreateBroker +0x15e` → `RtlSetSystemGlobalData(14)`)
  - `KdDebuggerEnabled` 둘째 비트 ← kdnet.dll
- 모두 쓰는 줄이 nt 안이라 **쓴 주인 = nt** 다. 짐 지도 2장 "다른 DLL" 열은 그대로 0 (run51 · run47 규칙 — 부르는 쪽은 괄호에).
- 부르는 쪽 모듈 코드는 **안 읽었다.** PDB 로 "IAT 호출 줄 → 함수 이름" 까지만 붙였다 (`iatsites.py`). PDB 없는 cng · kdnet · kdcom 은 rva 만 (Q204 ③).

#### 진짜 시스템 콜로 쓰는 칸 (다른 프로세스가 부름)

- `LangGenerationCount` (`NtGetMUIRegistryInfo +0x374`)
- `ComPlusPackage` (`NtSetSystemInformation +0x8e2`. 처음 묻는 쪽이 `ExpReadComPlusPackage +0x9e` 로 채움)
- `SharedDataFlags` 비트 1·2·3 (`NtInitializeRegistry` — smss `[추정]`)
- `TimeZoneBias*` (부팅은 직접, 이후 `NtSetSystemTime` 등)

이로써 run89 의 질문 "`ExpReadComPlusPackage` 가 KUSD `ComPlusPackage` 를 쓰나" 는 **쓴다** (`+0x9e`, 처음 물을 때) 로 닫혔다.

### 2.8 `SystemCall` (`+0x308`) — run105 숙제가 닫혔다

run105 해설 2.3 은 "누가 쓰나 — 이 방법으로는 안 보임" 에서 멈췄다. 이번에 찾았다.

#### 쓰는 줄 `[정적]`

- **`KiInitializeKernel +0x8e`** — `MOV [RAX+0x308],ESI`.
  - `RAX` = `MmWriteableSharedUserData` (뒷문 = 쓰기 별칭), `ESI` = 1.
  - 0 교시 (`entry +0x27e`) 다.
- 2.2 에서 본 대로, run105 가 못 본 까닭은 **고정 별칭 각도로만 훑어서** 다. 이 줄은 뒷문으로 쓴다.

#### 조건 둘 — 둘 다 맞아야 1 을 쓴다 `[정적]`

1. `+0x73 CMP [KiSystemCallSelector],ESI` — 전역 `KiSystemCallSelector` 가 **1** 이어야 한다.
   - 이 전역의 PE 처음 값은 0 이다.
   - `KiInitializeBootStructures +0x2d1` 이 **로더 확장 `+0x84` 비트 8** 이 켜졌을 때만 1 로 만든다.
2. `+0x7b TEST [HvlEnlightenments],0x80000` — 전역 `HvlEnlightenments` 의 **비트 19** 가 켜져야 한다.
3. 둘 중 하나라도 안 맞으면 `+0x79` · `+0x85` 가 `+0x9b` 로 **건너뛴다.** `+0x8e` 에 안 닿는다.

flow-writer 가 `.asm` 으로도 `+0x5a ~ +0x8e` 를 봤다 (`+0x73` · `+0x7b` · `+0x87 MOV RAX,[MmWriteableSharedUserData]` · `+0x8e`).

#### 그래서 이 VM 에서는 (단계별로)

1. 사진 8장 모두 `SystemCall : 0` (`_supp_<K1 ~ P2>.txt:246`) `[관찰]`.
2. 1 을 쓰는 줄은 하나뿐이고, 조건이 있다 `[정적]`.
3. 그러니 이 VM 은 조건이 안 맞아 **조건부 안 탐** 이다.
4. 스텁 494곳은 bit 0 이 꺼진 걸 보고 **보통 문 (`SYSCALL`)** 으로 간다 (run105 와 같음).
5. **아직 확인 아님**: 두 조건 전역의 실제 값 (`nt!KiSystemCallSelector` nt+`0x100815c` · `nt!HvlEnlightenments` nt+`0xfc6af8`) 은 사진에 없다 → 동적 (4절, Q79).

- 비유: 게시판 "오늘은 옛 쪽문 쓰세요" 칸은 문 여는 날 0 교시에만 적힐 수 있다. "부트 로더가 쪽문 표시를 줬고, 하이퍼바이저 쪽 스위치도 켜졌을 때" 만이다. 이 VM 은 둘이 다 안 맞아서 빈칸이다.

### 2.9 `끝까지 0` 41칸 — 넷으로 가름

run 문서 3절 표를 대표만 줄였다.

| 가름 | 칸 | 대표 | 뜻 |
|---|---|---|---|
| **0 을 씀** | 8 | `CryptoExponent` · `NtMinorVersion` (`InitBootProcessor +0x397` · `+0xbf3` 명시 0) · `TimeZoneId` (이 VM 0 = UNKNOWN) · `SystemExpirationDate` (정식판 0) · `QpcReserved` | 코드가 그 칸 (· 같은 바이트) 에 실제로 쓴다 |
| **조건부 안 탐** | 11 | `SafeBootMode` · `DbgSecureBootEnabled` (이 VM Secure Boot 꺼짐) · **`SystemCall`** · `EnclaveFeatureMask` (SGX · VBS 안쪽) · `UserModeGlobalLogger` · `ImageFileExecutionOptions` | 쓰는 줄이 부팅 길 안에 있는데 조건이 안 맞음 |
| **아무도 안 씀** | 17 | `TickCountLowDeprecated` · `TimeSlip` · `AlternativeArchitecture` · 패딩 · 예약 칸 · `UserPointerAuthMask` (ARM64 용 `[추측]`) | 어느 모양으로도 쓰는 줄 없음. 로더가 0 으로 채운 쪽이라 0 `[추정]` |
| **범위 밖 길 `[추측]`** | 5 | `AitSamplingValue` · `DismountCount` · `DbgErrorPortPresent` · `DbgConsoleBrokerEnabled` · `InterruptTimeBias` | 부팅 길 안엔 없고 시스템 콜 · 드라이버 · 절전 복귀에서만 |

- `SharedDataFlags` 비트 칸 12개는 **켜진 4 (1·2·3·8) + 끝까지 0 의 8** 로 갈렸다. 비트마다 쓰는 함수가 다르다. 사진 `0x10e` = 비트 1·2·3·8 과 맞다 `[관찰]`.
- 비유 (run113 해설과 같은 결): 게시판 41칸 대부분은 "**이 가게 · 이 VM 엔 해당 없음**" 이다. 안전 모드로 켰을 때, Secure Boot 일 때, SGX 가 있을 때만 적히는 칸이거나, 아예 비워 둔 칸이다.

### 2.10 `Cookie` — test.exe 첫 스레드도 지나가지만 안 쓴다

- `[정적]` 쓰는 줄 = `PspNotifyThreadCreation +0xb4` (`CMPXCHG.LOCK [RCX+0x330]`). **칸이 0 일 때만** 한 번 쓴다.
  - 값은 `KeQueryPerformanceCounter` · `ExGenRandom(1)` · `rdtsc` · PRCB 칸 · `KeQuerySystemTimePrecise` 를 xor 로 섞은 것 (짐 지도 3.7).
  - 이후 스레드는 `+0x3b` 에서 0 이 아니라 건너뛴다.
- 부르는 길: `PspUserThreadStartup +0x9c → PspNotifyThreadCreation`. 사용자 스레드가 처음 출발할 때마다 지난다.
- **test.exe 첫 스레드 (`P0`) 도 이 함수를 지나지만, 이미 값이 있어서 안 쓴다** `[정적]`.
- 처음 쓰는 스레드가 smss 주 스레드인지는 `[추정]` 이다 → Q204 ④ (정적), Q94 (동적).
- 맞춰 보기: 사진 `:254` `Cookie 0xe8e5ccd6`, 8장 같음 `[관찰]`.
- 비유: 게시판 "비밀 도장" 칸은 **처음 출근한 직원** 이 한 번만 찍는다. test.exe 직원이 출근할 땐 이미 찍혀 있어서 그냥 지나간다.

### 2.11 run70 이 남긴 것 둘 닫힘 + 나무 한 줄 정정

#### `InitBootProcessor +0x406` 의 `R15B` = 늘 1 (run70 `[추측]` → `[정적]`)

- 게시판 `ProcessorFeatures[23]` (`FASTFAIL`, `+0x28b`) 을 쓰는 줄 = `+0x406 MOV [RAX+0x28b],R15B`.
- run70 은 "`+0x16b MOV R15D,0x1` 뒤 갈래 둘을 안 따라갔다 — 1 그대로 `[추측]`" 이었다.
- 이번 `[정적]` (단계별로):
  1. `r15d` 는 `+0x6d` 에서 1 이 된다.
  2. 중간 갈래 (`+0x123`) 에서 바뀔 수 있다.
  3. 하지만 `+0x16b MOV R15D,0x1` 에서 **다시 1** 이 된다.
  4. 그래서 `+0x406` 에 닿을 때 `R15B` 는 **늘 1**.
- flow-writer 가 `+0x16b` · `+0x406` 을 `.asm` 으로도 봤다. → 짐 지도 3.7 `[23]` 줄 `[추측]` → `[정적]`, flow.md 4장 run70 정적 ③ 닫힘.

#### `XState` 를 쓰는 줄 (run70 정적 ②)

- `KiInitializeXSaveConfiguration` 이 게시판 `+0x3d8` `XState` 를 쓰는 줄 = `+0x196 ~ +0x202` `[정적]`.
  - 지역 `_XSTATE_CONFIGURATION` **840B 를 통째 복사** 한다. 그 뒤 `+0x21e ~ +0x23c` 는 읽기.
- run70 해설 2.8 의 "그 칸은 `KiInitializeXSaveConfiguration` 이 쓴다고 보이지만 본문은 안 읽었다 `[공백]`" 이 닫혔다. flow.md 2.3 나무의 `[추측] — 본문 안 읽음` → `[정적]`.

#### 정정 — `KiSetFeatureBits` "KUSD 안 씀" 은 너무 넓었다

- flow.md 2.3 나무 (run70 이 그림): `KiSetFeatureBits … (KUSD 안 씀)`.
- 실제 `[정적]`: `ProcessorFeatures` 는 안 쓰지만, **`EnclaveFeatureMask` (`+0x36c`) 를 SGX 일 때 쓴다** (`+0x83a` · `+0x859`). 이 VM 은 안 탄다.
- run70 문서는 `ProcessorFeatures` (`+0x274`) 만 말했으니 **틀리지 않았다.** 그래서 run70 문서 맨 위 정정 줄은 안 넣었다. flow.md 나무 한 줄만 고쳤다.
- run70 해설 맨 위에는 "고쳐 읽기 (run115)" 한 줄을 붙였다 (위 둘 + 이 정정).

### 2.12 사진과 맞춰 보기 — 불일치 0, 그리고 디버거 때문에 생긴 값

- `[관찰]` 쓰는 줄이 있는 칸은 시각 8 빼고 **8장 모두 같은 값**. "K1 ~ P2 사이엔 안 바뀐다" 는 run44 결론이 줄 단위로 닫혔다.
- 계산으로 맞은 값 (run 문서 8절 대표): `TickCountMultiplier` `0xfa00000` · `MitigationPolicies` `0xa` (= NX 2 + SEH 2 × 4) · `SharedDataFlags` `0x10e` · 프로세서 수 `2 · 2 · 1` · `TimeZoneBias` −9시간 (KST) · `SystemCall` 0.
- 줄만 맞음 (값 계산 본문은 안 읽음): `CyclesPerYield` `0x22` · `QpcBypassEnabled` `0x83` · `BootId` `0xf` · `SuiteMask` `0x110` · `Cookie` · `XState` `0xe7`.
- 사진은 부팅이 끝난 뒤 값이라 **부팅 중 순서** (`ⒷS` → `ⒷP`) 는 못 맞춘다. 순서는 정적 코드 순서만이다.

#### 디버거가 붙어서 달라지는 점 (CLAUDE.md "따로 적기")

- **`KdDebuggerEnabled` = `0x3` 은 KDNET 디버거가 붙어서 생긴 값** 이다.
  - nt 쓰는 줄 (`KdInitSystem(0) +0x571`) 은 **1** 만 쓴다 `[정적]`.
  - kdnet.dll 이 `RtlSetSystemGlobalData(10)` 로 비트를 더한다 `[정적]` (비트 뜻은 안 읽음).
  - 디버거 없이 부팅하면 0 일 것 `[추측]` → 동적 (4절).
- 시각 칸은 디버거가 멈출 때 `KeFreezeExecution` / `KeThawExecution` 도 고친다. 이 사진들은 디버거 붙은 VM 이라 그 줄도 돌았을 수 있다 (원래 "소음" 칸).
- 디버거와 무관하게 **VM 환경** 에 달린 값: `QpcBypassEnabled` `0x83` (타이머 종류) · `NumberOfPhysicalPages` · `ActiveProcessorCount` (8GB · 2코어).

### 2.13 PE_emulator 쪽 (run 문서 "PE_emulator 쪽", 짧게)

`../PE_emulator` 코드는 flow-writer 가 안 읽었고 결과서가 읽었다. 고칠지는 사용자 결정이다.

- 지금 에뮬레이터 (`Src/nt/AddressSpace.cpp` `_WriteKusdBootValues`, `:462`) 는 게시판을 **덤프에서 떠 오지 않고**, 아홉 칸 + 시계 셋만 상수 · 계산으로 쓴다. 그 값은 맞다. 나머지 줄 있는 칸은 0 이다.
  - ntdll 이 test.exe 초기화에서 어느 칸을 읽는지는 **확인 필요**.
- **`UnparkedProcessorCount` (`+0x36a`) — 틀림 가능 (확인 필요)**: 에뮬레이터 0, 진짜 2.
  - ntdll `RtlpAcquireSRWLockExclusiveContended +0xed` 가 `CMP word [0x7ffe036a],1` 로 읽고, `+0xf6 JBE` 로 **≤ 1 이면 스핀을 건너뛴다.** 길이 갈린다.
  - **새 말 — 스핀** = 락 (열쇠) 이 잠겨 있을 때 바로 잠들지 않고 **잠깐 제자리에서 돌며 다시 보는 것** `[추측]` (일반적으로 알려진 설명). 프로세서가 하나뿐이면 돌아 봐야 열쇠를 쥔 쪽이 못 움직이니 건너뛰는 것으로 보인다 `[추측]` (이 글의 짐작 — 근거 문서는 "≤ 1 이면 건너뜀" 까지만 말한다).
  - `RtlFreeHeap` · `RtlpLowFragHeapAllocFromContext` 등 5함수가 이 칸을 읽는다 (결과서). 같은 묶음 `ActiveProcessorCount` 는 2 로 맞다.
- **`SystemCall` 은 0 이어야 맞다** — 지금 0 (맞음). 1 로 쓰면 스텁 494곳이 옛 쪽문 (`INT 0x2e`) 으로 간다.
- `끝까지 0` 41칸 0 — 맞음. 커널 밖이 부르는 칸 (`ActiveConsoleId` 등) 은 입력 값으로 두면 된다 (읽는 쪽이 test.exe 길에 있는지는 확인 안 함).
- 에뮬레이터 코드 주석 "진짜 쓴 자리 모름 (… Q78g)" 은 이번 결과로 낡았다.

### 2.14 결과서와 다르게 적은 곳 — run 문서를 따른 까닭

| 무엇 | 결과서 | run 문서 (이 글) | 까닭 |
|---|---|---|---|
| 문 연 뒤 · 가게 생기기 전 단계 | `ⒷL` | **`ⒷR`** | 이미 있는 `ⒷL…` (run48) 과 겹침 (2.6) |
| 경로 꼴 | `함수 +0xN → 다음 함수` (오프셋 = 그 함수 안에서 다음을 부르는 줄) | `→ 다음 함수 (호출 +0xN)` (오프셋은 마지막 함수 뒤만) | 짐 지도 3장 관례. 뜻은 같다 |
| `Ⓑ2 +0xf1` | `PspInitPhase2` 안 줄 | `Phase1InitializationIoReady → PsInitSystem(2) (호출 +0x1d5) → PspInitPhase2 (호출 +0x3b) +0xf1` 로 풀어 씀 | 짐 지도 `Ⓑ2…` 는 한 단계 더 들어간 `PspInitializeSystemDlls` 까지라 |
| 3.7 머리 run 번호 | `(해결됨 → run110, sast/110)` | **run115** | sast 번호를 run 번호로 쓴 것 |
| `R15B` 태그 | `[추정]` → `[정적]` | `[추측]` → `[정적]` | run70 원래 태그가 `[추측]` |

- 셈 (아는 칸 `1 + 61 + 8 + 11 = 81`, 아무도 안 씀 17, 범위 밖 5) 은 결과서와 같다.

### 2.15 정리 — 비유로 한 번 더

- 관리사무소 앞 공용 게시판에 103칸이 있다. 이제 **모든 칸의 사연** 을 안다.
- 거의 다 **문 여는 날** 0 교시 · 1 교시에 관리사무소 직원이 적었다.
- 몇 칸은 문 연 뒤 건물 안 상담실 (win32k · cng · condrv · kdnet) 이 **게시판 담당 창구** 에 와서 적어 달라고 했다. 펜은 늘 관리사무소가 쥔다.
- 관리사무소는 게시판 **뒷문 하나** 로만 적는다. 앞 유리 쪽 CCTV 에 적는 사람이 안 찍혔던 이유다.
- test.exe 가게가 문 여는 동안은 아무도 새로 안 적는다. 벽시계 바늘만 간다.
- "옛 쪽문" 칸 (`SystemCall`) 은 적을 조건이 안 맞아 빈칸이고, 그래서 test.exe 의 전화는 모두 보통 문으로 간다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 꼬리표 한 마디)

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 | — | ntkrnlmp pdb 줄 · 근거 로그 줄에 run115 (run44 KUSD 구획을 다시 읽은 값 12줄) |
| 그림 1 `SYSCALL` 꼬리표 | "… 사진 8장 0, run105" | + "쓰는 줄 `KiInitializeKernel +0x8e` 는 부팅 때 조건부 안 탐, run115" |
| 2.2 스텁 절 | 쓰는 커널 줄 "안 보임" `[정적, 한계 있음]` | (해결됨) `KiInitializeKernel +0x8e`, 조건 둘, 이 VM 조건부 안 탐 `[정적+관찰]` |
| 2.3 KUSD `ProcessorFeatures` 나무 | `KiSetFeatureBits` "KUSD 안 씀" · `KiInitializeXSaveConfiguration` `[추측] — 본문 안 읽음` · `+0x406` 값 미확정 | `EnclaveFeatureMask` SGX 만 (정정) · `+0x196 ~ +0x202` 840B 복사 `[정적]` · `R15B = 1` 확정. "두 모양" 중 둘째는 쓰기에선 빈 모양 |
| 2.3 새 소절 | — | **"KUSD 나머지 102칸을 채우는 곳 — 부팅 때 · 부팅 뒤"** (쓰는 길 하나 · 부팅 나무 · 102칸 가름 · `ActiveConsoleId` 정정 · `SystemCall` · `UnparkedProcessorCount` · 사진) |
| 3장 bcryptprimitives 절 | `RNGSeedVersion` 쓰는 쪽 모름 | `RtlSetSystemGlobalData +0x14f` ← cng.sys IAT `[정적]` |
| 4장 | run105 정적 ② · run70 정적 ② ③ 열림, run89 정적 ① 의 `ExpReadComPlusPackage` 열림 | 모두 (해결됨 → run115). 새 줄 넷 (Q78g1 해결 · 동적 1 ~ 5 · 정적 ① ~ ⑤ · PE_emulator) |
| 5장 | — | run115 두 표 |

- 한 줄로: **흐름도 범위 앞 (부팅) 에 KUSD 소절 하나가 생겼고, 2.2 · 2.3 의 열린 질문 셋이 닫혔다.** test.exe 길 자체는 그대로다.

### 짐 지도 `baggage.md` 2장 — 유저 구조체 모름 0

#### 구조체 21개, 직접 칸 1,456

| 가름 | 바퀴 전 (run115 전) | 바퀴 뒤 | 무엇 때문에 |
|---|---|---|---|
| 아는 칸 | 858 (정확 827 · 짐작 31) | **938** (정확 907 · 짐작 31) | 이번 80 (모두 정확) |
| 아무도 안 씀 | 185 | **202** | 이번 17 |
| 범위 밖 길 `[추측]` | 262 | **267** | 이번 5 |
| 트랩 프레임 덮임 | 25 | 25 | — |
| 모름 | **126** | **24** | 이번 102 |

- 합은 그대로 1,456 이다.
- 아는 칸 중 `끝까지 0` (7.1): 362 → **381** (0 을 씀 8 + 조건부 11).
- 짐 지도 3.7 줄: 5 → **36** (새 31 — 한 줄이 여러 칸을 묶음, 고친 줄 1 `ProcessorFeatures [23]`).

#### 2장 표 줄

| 줄 | 칸 | nt 가 씀 | ntdll 이 씀 | 모름 |
|---|---|---|---|---|
| `_KUSER_SHARED_DATA` | 103 | 1 → **81** | 0 | 102 → **0** |
| **구조체 유저 7개 합** | 488 | 124 → **204** | 174 | 102 → **0** |
| **구조체 커널 14개 합** | 968 | 605 | 0 | **24** (그대로) |
| **ntdll 전역 3구역 합** | 493 | 8 | 219 | **268** (그대로) |

- ntdll 이 쓴 칸은 0 그대로다. ntdll 은 게시판을 **읽기만** 한다.

#### 짐 지도가 어디까지 왔나 — 한눈에

| 덩어리 | 모름 | 남은 것 | 어디서 |
|---|---|---|---|
| 유저 구조체 7개 (PEB · 매개변수 · TEB · 명부 · 명부 줄 · 연결 카드 · KUSD) | **0** | — | 다 찼다 |
| 커널 구조체 14개 | **24** | `_EPROCESS.SharedCommitCharge` 1 + VAD 칸 안 읽음 23 (`_MMVAD` 9 · `_MMVAD_SHORT` 14) | VAD 는 덤프 보충 읽기 **Q79** |
| ntdll 전역 3구역 | **268** | `.data` 238 · `.mrdata` 26 · `.00cfg` 4 | 다음 문서 차례 **Q78g2** |

구조체 모름이 줄어 온 길 (짐 지도 7.2 머리 그대로, 대표만):

| 언제 | 구조체 모름 |
|---|---|
| run45 전 | 1,275 |
| run49 전 | 1,196 |
| run53 전 | 639 |
| run54 전 | 470 |
| run112 전 | 300 |
| run113 전 | 218 |
| run115 전 | 126 |
| **지금** | **24** (모두 커널) |

### 큰 그림에서

- **유저 구조체 일곱이 모두 "모름 0"** 이 됐다. 에뮬레이터 가상 주소 공간에 놓이는 구조체는 칸마다 **누가 · 언제 · 어느 줄에서** 쓰는지, 또는 왜 0 인지가 있다.
- KUSD 는 특히 "**초기화 동안 놓이는 짐이 아니라, 초기화 전에 이미 있는 입력**" 으로 정리됐다. 에뮬레이터는 상수 · 계산 칸은 다시 셈하고, 나머지는 입력 값으로 두면 된다 (2.5 · 2.13).
- 남은 것은 커널 24칸 (VAD 쪽 — 덤프 읽기) 과 ntdll 전역 268칸이다. 다음 문서 차례 Q78g2 가 전역 쪽이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| "조건부 안 탐" 11칸의 **조건 전역 값** (`nt!KiSystemCallSelector` nt+`0x100815c` · `nt!HvlEnlightenments` `0xfc6af8` 비트 19 · `KeDynamicPartitioningSupported` · `CmpLKGEnabled` · `InitSafeBootMode` 등 9개) — 모두 0 이면 사진과 맞음 | 실행 때 값. 사진에 이 전역들이 없다 | 동적 **Q79** (부팅 뒤 `dd` 한 번씩, 읽기만 — 결과서 "급하지 않음") |
| "0 을 씀" 칸의 **입력값** (`CmGlobalValidationRunlevel` · `ExpNtExpirationDate` · `PspSehValidationPolicy` 등 6개) | 실행 때 값 | 동적 **Q79** (읽기만) |
| 부팅 중 **순서 · 몇 번**: `IopStoreBootDriveLetter +0x136` (`NtSystemRoot` 를 두 번 쓸 수 있나) · `PspNotifyThreadCreation +0xb4` (누가 처음 `Cookie`) · `ExpReadComPlusPackage +0x9e` (누가 처음 묻나) · `HalpTimerConfigureQpcBypass +0xb3` (어느 위상) | 사진은 부팅 뒤 값이라 순서를 못 맞춤. 콜백 · 표 호출은 정적으로 횟수를 모름 | 동적 **Q94** (부팅 중 멈춤점 — 선택, 낮음) |
| 디버거 없이 부팅하면 `KdDebuggerEnabled` 가 0 인가 `[추측]` · 레지스트리 `Policies\System` 셋이 `0x10e` 와 맞나 | 이 VM 은 늘 KDNET 이 붙어 있음 · 레지스트리 값은 안 읽음 | 동적 (선택) — 결과서 4 · 5. plan 결정 기록은 "동적 → Q79 · Q94" 로만 적어서 **어느 쪽에 붙는지는 이 글이 정하지 않는다** |
| ① HAL 타이머 간접 사슬 · 위상 (`HalInitSystem ⇢ HalpTimerInitSystem` 등 — `QpcBias` · `QpcData` · `QpcBypassEnabled` 가 어느 교시인가) | 표 호출 · 간접 호출이라 `[추정]` | 정적 **Q204 ①** (작음). plan 에는 `KeSetSystemTime ⇢ KiSetSystemTimeDpc` 도 같이 적혀 있다 |
| ② 값 계산식 본문 (`ExpComputeCyclesPerYield` · `RtlGenerateQpcToIncrementConstants` · `ExGetSuiteMask` · `ExpGetNtProductTypeFromLicenseValue` 등) — 에뮬레이터가 CPU · 환경에서 셈하려면 | 이번엔 쓰는 줄만, 계산 본문은 안 읽음 | 정적 **Q204 ②** (작음) |
| ③ cng.sys · kdnet.dll · kdcom.dll 의 IAT 호출 줄 이름 (`RNGSeedVersion` 올리는 때 · `KdDebuggerEnabled` 둘째 비트 뜻) | PDB 가 없어 rva 만 | 정적 **Q204 ③** (PDB 받기) |
| ④ `Cookie` 를 처음 쓰는 스레드가 정말 smss 인가 | `PspUserThreadStartup` 앞쪽을 안 따라감 `[추정]` | 정적 **Q204 ④** (작음) |
| ⑤ 고정 별칭이 정말 읽기 전용이 되나 · 주소 무작위화인가 | `MiProtectSharedUserPage` 는 `.asm` 이 없어 objdump 로만 봄 `[추정]` | 정적 **Q204 ⑤** |
| ntdll 이 test.exe 초기화에서 게시판 **어느 칸을 읽는지** (에뮬레이터 0 칸이 괜찮은지) · `UnparkedProcessorCount` 를 2 로 써도 되는지 | 이번 범위는 "쓰는 쪽". 읽는 쪽 전수는 안 함 | 대기열 번호 없음 — run 문서 "PE_emulator 쪽" (확인 필요, 사용자 결정) |
| `ActiveConsoleId` 를 읽는 쪽 (`WTSGetActiveConsoleSessionId` 류) 이 test.exe 길에 있나 | 확인 안 함 | 대기열 번호 없음 — run 문서 `[확인 필요]` |

- 결과서 판단은 "동적 1 · 2 는 읽기만, 급하지 않음" 이다.
- 훑기 한계 (레지스터 추적 — 분기 합치기 · 휘발 레지스터 · 컨테이너 사본 · nt 밖 직접 쓰기) 는 2.2 끝에 적었다. 결과서는 "모름 0" 으로 셌지만, 이 한계 안에서의 0 이다.

---

## 5. 더 읽기

- run 문서: [`run115-kusd-boot.md`](run115-kusd-boot.md) — 요약 표, 호출 지점 표, 호출 관계 나무, 1 ~ 8절 (쓰는 길 · 값의 출처 · 넷 가름 · 커널 밖 · `SystemCall` · run70 확정 · 간접 호출 · 사진), 결과서와 다르게 적은 곳, PE_emulator 쪽
- 결과서: [`_workspace/sast/110_kusd-boot_result.md`](../../_workspace/sast/110_kusd-boot_result.md) — 판단 기준, 한눈에, 쓰는 말, 표 A (102칸 전부), 훑기 한계, `FUN_` 조각 주의
- 요청서: [`_workspace/sast/110_kusd-boot.md`](../../_workspace/sast/110_kusd-boot.md)
- flow.md: [그림 1](flow.md) `SYSCALL` 꼬리표 · 2.2 스텁 절 · 2.3 KUSD `ProcessorFeatures` 나무 + 새 소절 "KUSD 나머지 102칸을 채우는 곳" · 3장 bcryptprimitives 절 · 4장 run115 줄 · 5장 run115
- 짐 지도: [`baggage.md`](baggage.md) 쓰는 말 (`ⒷR`) · 2장 (숫자) · 3장 줄임 표 (`ⒷX…` · `ⒷI…` · `ⒷM…` · `ⒷP…` · `ⒷR`) · **3.7** · 4장 KUSD 짝 줄 (고정 별칭 + 쓰기 별칭) · 5장 `Ⓑ` 9번 · 7.2 · 7.46
- 앞 해설:
  - [run70](run70-cpu-features-해설.md) — 같은 방법, 공용 게시판 · 체크리스트 · CCTV 두 각도 (이번에 맨 위 "고쳐 읽기 (run115)" 를 붙임)
  - [run105](run105-ntdll-small-해설.md) — `SystemCall` 칸을 **읽는** 쪽, 보통 문 / 옛 쪽문 (이번에 맨 위 "고쳐 읽기 (run115)" 를 붙임)
  - [run48](run48-system-dll-boot-해설.md) — 부팅 단계 `Ⓑ`, 문 여는 날
  - [run64](run64-apiset-phase-해설.md) — 위상 = 교시, `MmInitSystem` 교시마다
  - [run113](run113-ntdll-user-structs-해설.md) — 같은 셈 방식, 유저 구조체 넷이 먼저 모름 0

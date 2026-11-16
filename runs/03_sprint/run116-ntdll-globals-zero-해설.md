# run116 ntdll-globals-zero — 해설

> 이 글은 [run116 문서](run116-ntdll-globals-zero.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q78g2** — Q78g ("KUSD 와 남은 전역") 의 **나머지**: ntdll 전역 `끝까지 0` (`.data` 215 · `.mrdata` 22) + `.00cfg` `못 읽음` 4 (`_workspace/plan.md`, 이번에 **끝**)
  - 짝인 **Q78g1** (KUSD) 은 바로 앞 바퀴 [run115](run115-kusd-boot-해설.md) 에서 끝났다. 이번으로 **Q78g 가 다 끝났다.**
  - 그리고 이게 **Q78 (짐 지도 C) 의 마지막 정적 조각** 이다. 맨 끝 6절에서 Q78 전체를 돌아본다.
  - run114 가 넘긴 둘도 같이 맞췄다: `RtlFailedCriticalDebugAllocations` 옮기기 · 잠금 칸 셈 ([run114 해설](run114-ntdll-globals-early-해설.md) 4절 표 끝 세 줄).
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다. **짐 지도 바퀴**다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q79** (`.00cfg` 읽기) · **Q46** (`NtOpenKey` 반환 둘 등)
    - 정적 → 새 **Q205 ① ~ ③**
    - 셈 결정 셋 → **사용자** (6절 끝)
- 근거:
  - run 문서 [`run116-ntdll-globals-zero.md`](run116-ntdll-globals-zero.md) (가장 중요)
  - 요청서 [`_workspace/sast/113_ntdll-globals-zero.md`](../../_workspace/sast/113_ntdll-globals-zero.md) · 결과서 [`113_ntdll-globals-zero_result.md`](../../_workspace/sast/113_ntdll-globals-zero_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다.** 글은 기준 · 2.4 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): 2장 숫자 · 2장 끝 "짐 지도 C 끝" 줄 · 3.8 · 3.9 · 3.10 · 7.1 · **7.3** · 7.5 · 7.45 · 새 **7.47**.
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 `[관찰]` 은 모두 **run44 때 찍어 둔 사진** (`ms/logs/04_baggage-read_{P1a,P1b,P2}.txt`) 을 결과서가 다시 읽은 것이다.
  - 결과서는 이름 칸 첫 `0x20` 바이트가 `.data` · `.mrdata` 237칸 모두 P1a · P1b · P2 에서 0 인 걸 다시 봤다. `.00cfg` 는 사진 8장 모두 `????` (못 읽음).
- **결과서와 run 문서가 다르게 센 곳이 있다** (2.12). 이 글은 **run 문서**를 따른다.
  - 조건부 안 탐 `.data` 새 줄은 **76** (결과서 77 에는 이미 3장에 있던 `AvrfpLoaderEntry` 가 들어 있다).
  - 잠금 칸은 결과서 "아무도 안 씀 · 잠금 7" 이 아니라 **3.8 "잡고 풂" 10칸** (run114 의 셋 포함).

---

### 이미 푼 말 — 링크만

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) = 가게 문 앞 **안내판**
- ntdll 전역 = 관리실 **칠판**, `.mrdata` → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md)
- **잠금 칸** (임계 구역 · SRW 락) = **열쇠** → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md) (묶음 6) · [run60 해설](run60-func-table-mrdata-해설.md) (SRW 락) · 임계 구역 디버그 정보 = 열쇠마다 다는 **꼬리표** → [run114 해설 2.5](run114-ntdll-globals-early-해설.md)
- 네 가지 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길) → [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법)
- 셈 기준 · 가름 열 (전역엔 "아무도 안 씀 · 범위 밖" 열이 아직 없다) → [run114 해설 2.2](run114-ntdll-globals-early-해설.md)
- 이름 칸 · 옆 칸 · 코드 접근 폭 → [run114 해설 2.11](run114-ntdll-globals-early-해설.md)
- 사진 지점 K1 ~ P2, 사진 구간 (`≤P1a` · `끝까지 0` · `못 읽음` …) → [run44 해설](run44-baggage-snap-해설.md) 2.8 · 2.12
- App Verifier (앱 검사기) 묶음 = **점검관이 온 날에만** 하는 일 → [run19 해설 2.5 묶음 2](../02_sprint/run19-proc-init-modules-해설.md) · [run105 해설](run105-ntdll-small-해설.md)
- IFEO (exe 별 특별 지시) 를 처음 여는 자리 → [run46 해설 2.6](run46-process-heap-해설.md)
- shim (호환성 고치기 장치 `[추측]`) → [run66 해설](run66-apphelp-cache-해설.md)
- WOW64 (64비트 Windows 에서 32비트 프로그램을 돌리는 층 `[추측]`) → [run114 해설 2.6](run114-ntdll-globals-early-해설.md)
- 기능 상태 검사 (WIL 기능 칸) = 칠판 구석의 **기능 스위치 표** → [run62 해설 2.9](run62-tp-etw-init-해설.md)
- ApiSet = **별명 사전** → [run57 해설](run57-nls-apiset-해설.md)
- 매니페스트 = exe 에 붙인 **설정 쪽지**, 활성화 문맥 (SxS) = 그 쪽지로 만든 **설정 묶음** → [run87 해설](run87-sxs-create-해설.md)
- CFG = 간접 호출 목적지를 확인하는 **교환원** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md) · [run63 해설](run63-cfg-func-table-해설.md)
- 묶음 번호 (묶음 4 = IFEO 읽기, 뒤5 = kernel32 준비, 뒤9 = SMT 지연 잠 · exe TLS) → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md) · [run19 해설](../02_sprint/run19-proc-init-modules-해설.md)

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **죽은 줄** → 2.4 ①
- **잡고 풂** (잠금 칸을 세는 법) → 2.5
- **상수 기능** (WIL) → 2.7
- **표 구동 쓰기** → 2.8
- **PCH (precompiled header) 표시** → 2.10

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**.
- ntdll 전역 = 관리실 **칠판**. 칸마다 이름표가 붙어 있다.
- 잠금 칸 = **열쇠**. 칠판 옆 열쇠 걸이에 걸려 있다.
- **하나만 바꿔 부른다** (run114 해설과 같은 까닭).
  - run 문서는 이번에도 ntdll `.data` 를 "가게 뒤 **창고 선반**" 이라고 불렀다.
  - 앞 해설들에서 **창고 = 프로세스 힙** 이라, 이 글은 그대로 **칠판** 이라고 부른다. 같은 뜻이다.
- run 문서가 이번에 쓴 그림 (말만 칠판으로 바꿔서 가져온다):
  - 이번엔 **끝까지 빈 칸** 241개를 다 열어 봤다. 빈 칸이라도 사정이 다 다르다.
  - 3칸은 직원이 문 열 때 **"0" 이라고 또박또박 적은** 칸이다 (원래 비어 있어서 겉보기는 같다).
  - 99칸은 **"특별한 가게일 때만"** 채우는 칸이다 — 앱 검사기 · shim · 32비트 · 매니페스트 · CFG 강제 같은 조건이 test.exe 엔 없다.
  - 10칸은 **열쇠** 칸이다. 문 여는 길에 잠갔다가 다시 풀어서, 사진엔 처음 모양 그대로 찍힌다.
  - 124칸은 이 가게 문 여는 길에선 **아무도 안 만지거나** (44), **다른 날 (종료 · 오류 · 공개 API) 에만** 쓰는 칸 (80) 이다.

### 한 줄로 먼저 — 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| 이번에 가른 ntdll 전역 | **241칸** (`.data` `끝까지 0` 215 · `.mrdata` `끝까지 0` 22 · `.00cfg` `못 읽음` 4) + run114 가 넘긴 잠금 칸 2 = **243** |
| 3장으로 옮김 (새) | **111** = 0 을 씀 **3** · 잠금 칸 (잡고 풂) **10** · 조건부 안 탐 **98** |
| 7.3 에 남김 (표시만) | **124** = 아무도 안 씀 **44** · 범위 밖 길 **80** `[추측]` |
| 이미 3장에 있던 것 | 8 (sast/111 이 닫은 7 · run105 `AvrfpLoaderEntry` 1) |
| 7.3 ntdll 전역 모름 | **268 → 157** — 그리고 157 은 **모두 가름 끝** (아무도 안 씀 63 · 범위 밖 94) |
| 짐작 → 정확 | `g_LdrpWow64LdrpInitialize` (`[추측]` → `[정적]`, 표 구동) |
| 새로 보인 syscall 길 | 묶음 4 IFEO 건너뛰기 **앞** 에서 `NtOpenKey` · `NtQueryValueKey` · `NtClose` (키가 열리면) |
| 그림 | **안 바뀜** — 이미 있는 함수 안쪽 한두 걸음만 늘었다 |

이 글에서 붙잡아 둘 세 줄:

> **(1) "끝까지 0" 이어도 사정은 넷으로 갈린다.**
> 0 을 또박또박 쓰는 칸 · 조건이 안 맞아 안 쓰는 칸 · 잠갔다 풀어 제자리인 열쇠 칸 · 아무도 안 만지거나 다른 날에만 쓰는 칸. 사진만으로는 이 넷을 못 가른다. 코드로 갈랐다.
>
> **(2) IFEO 를 건너뛰는 test.exe 도 레지스트리를 한 번 더 본다.**
> 교착 완화 함수가 IFEO 건너뛰기 **앞** 에 있어서, `Session Manager` 키를 열어 값 하나를 묻는다. PE_emulator 의 빈 덫엔 이 syscall 셋이 없다.
>
> **(3) 짐 지도 C (Q78) 의 정적 조각이 다 끝났다.**
> 남은 "모름" 은 구조체 24 (모두 커널) · 전역 157 (모두 가름 끝). 전역 쪽은 "모르는" 게 아니라 **셀 열이 없어서** 모름 칸에 남은 것이다 (6절).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 한 구간이 아니라 **칠판 빈 칸을 건드리는 줄 전부** 를 훑었다. 지도 위에서 이번에 짚은 곳만 표시하면:

```
cmd.exe 의 부탁 → 커널 (① 가게 · 첫 직원 만들기) → 첫 스레드 ntdll 시작
 ② test.exe 첫 스레드, ntdll
  _LdrpInitialize  +0xf3 열쇠 LdrpLoaderLock (잡고 풂)                     ← 이번 (2.5)
   └─ +0x16c LdrpInitializeProcess
      +0x693 … +0x773  RtlFailedCriticalDebugAllocations = 0 (무조건)       ← 이번 (2.4 ②)
      ├─ +0x536  LdrpInitializeExecutionOptions (묶음 4)
      │          └─ +0xa2 교착 완화 함수 → 칸 0 · NtOpenKey · NtQueryValueKey · NtClose  ← 이번 (2.4 ③)
      │          +0xa6 / +0xad  IFEO 통째 건너뜀
      ├─ +0x1718 AVrfInitializeVerifier   (앱 검사기만 — 안 탐)              ← 이번 (2.6)
      ├─ +0x17c6 LdrpLoadWow64            (WOW64 만 — 안 탐)                ← 이번 (2.8)
      ├─ +0x1963 LdrpInitializeImportRedirection (일 안 함)                 ← 이번 (2.6)
      ├─ +0x19a2 LdrpInitializeKernel32Functions (뒤5)
      │          └─ … ExecuteHotpatchTestRuntimeFunction → RuntimeGlobal = 0 ← 이번 (2.4 ①)
      ├─ +0x19f4 LdrpInitShimEngine       (pShimData 0 → 안 부름)           ← 이번 (2.6)
      ├─ … 열쇠 몇 개 (모듈 알림 · 모듈 찾기 · DLL 경로) 잡고 풂            ← 이번 (2.5)
      └─ +0x1c2a LdrpInitializeSmtDelayedSleep (뒤9) → NtOpenKey …          ← 이번 (2.6)
  → NtContinue 은퇴 → (CRT) → main
```

- callee 앞 `+0x..` 는 ret 자리다 (run 문서 "호출 관계" 그대로). 칸을 만지는 줄 (`+0xf3` lea · `+0x693 … +0x773`) 앞은 그 줄 자리다. call 자리는 2.3 · 2.4 에 따로 적는다.
- 지도의 큰 길은 **그대로** 다. 새 단계도 새 caller 도 없다. run 문서 말로 "이미 있는 함수의 **안쪽** 한두 걸음만 늘었다".

### 무엇이 궁금했나

짐 지도 7.3 에는 "쓴 자리를 모르는 ntdll 전역" 이 **268개** 남아 있었다. 그중 이번 몫은:

- `.data` `끝까지 0` **215** — 사진 P1a ~ P2 내내 0 이던 칸
- `.mrdata` `끝까지 0` **22**
- `.00cfg` `못 읽음` **4** — 사진이 아예 못 읽은 칸 (`__guard_xfg_check_icall_fptr` 등)

요청서의 질문은 넷이었다 (요청서 "알고 싶은 것"):

1. 전역마다 네 가지로 가르기 (조건부면 조건도, 범위 밖이면 쓰는 함수 이름 하나)
2. `.00cfg` 4칸의 파일 값 · 쓰는 줄 · P2 까지 값
3. 쓰는 줄이 간접 호출 대상 안이면 좁히기
4. 0 을 쓰는 줄 · 조건부 줄의 경로

그리고 run114 가 넘긴 셈 숙제 둘:

- `RtlFailedCriticalDebugAllocations` (0 을 씀) 를 3.8 로 옮길까?
- 잠금 칸 셋 (`LdrpLoaderLock` · `LdrpDllNotificationLock` · `LdrpModuleDatatableLock`) 을 어떻게 셀까?

### 바퀴 전에 알던 것

- `[관찰]` run44 사진: 이 241칸은 이름 칸 기준으로 P2 까지 0 (`.00cfg` 는 못 읽음). 그런데 **왜 0 인지** 는 모른다.
- `[정적]` sast/111 결과서가 "이름 칸 쓰는 줄이 지도 안에 있는 것" 53개 (`.data` 39 · `.mrdata` 14) 를 미리 뽑아 두었다 (요청서 "관련").
- `[정적]` run114: `RtlFailedCriticalDebugAllocations` 는 `LdrpInitializeProcess +0x773` 이 0 을 쓴다. 다만 그때 셈엔 안 넣었다.
- `[추측]` run17: 3.9 `g_LdrpWow64LdrpInitialize` 는 짐작 줄이었다.
- 셈 선례: run60 `LdrpInvertedFunctionTableSRWLock` · run69 `TppPoolpListLock` 은 "잡고 푸는 락" 으로 3.8 에 있다. 그런데 run114 는 같은 꼴 셋을 "아무도 안 씀 (잠금)" 으로 7.3 에 뒀다 — **같은 꼴, 다른 셈** 이었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 243칸은 어디로 갔나

run 문서 요약 표를 그대로 줄였다.

| 구역 | 칸 | 0 을 씀 | 조건부 안 탐 | 잠금 칸 (잡고 풂) | 아무도 안 씀 | 범위 밖 `[추측]` | 이미 3장 | → 3장 (새) | 7.3 에 남음 |
|---|---|---|---|---|---|---|---|---|---|
| `.data` `끝까지 0` | 215 | 3 | 76 | 8 | 43 | 77 | 8 | **87** | **120** |
| `.mrdata` `끝까지 0` | 22 | 0 | 18 | 0 | 1 | 3 | 0 | **18** | **4** |
| `.00cfg` `못 읽음` | 4 | 0 | 4 | 0 | 0 | 0 | 0 | **4** | **0** |
| (run114 넘김) `.data ≤P1a` 잠금 둘 | 2 | — | — | 2 | — | — | — | **2** | 0 |
| **합** | **243** | **3** | **98** | **10** | **44** | **80** | **8** | **111** | **124** |

- 표 읽는 법 (run114 해설 2.1 과 같다):
  - **→ 3장** = 0 을 씀 + 조건부 안 탐 + 잠금 칸
  - **7.3 에 남음** = 아무도 안 씀 + 범위 밖
  - **이미 3장** = 이번 목록에 이름이 있었지만, 앞 바퀴가 벌써 3장에 넣어 둔 칸 (sast/111 이 닫은 7 + run105 의 `AvrfpLoaderEntry`)
- 잠금 칸 `.data` 8 = 결과서 잠금 7 + run114 가 7.3 에 둔 `LdrpModuleDatatableLock`.
- `.data` 조건부 76 은 run 문서 셈이다. 결과서는 77 로 셌다 (2.12).

### 2.2 이번 셈 기준 — run114 와 같고, 둘이 더해졌다

오케스트레이터가 정했다. 바탕은 [run114 해설 2.2](run114-ntdll-globals-early-해설.md) 그대로다.

1. **0 을 씀 · 조건부 안 탐** → 3.8 · 3.9 · 3.10 "아는 칸" (run60 · run66 · run114 선례).
2. **아무도 안 씀 · 범위 밖 길** → 7.3 에 그대로 두고 `(run116 가름: …)` 표시만. 가름 열을 새로 둘지는 **사용자 결정 대기** (7.45 ② · 7.47 ②).
3. **새로 더한 것 ① (run114 넘김)**: `RtlFailedCriticalDebugAllocations` → 3.8. 결과서의 "0 을 씀 3" 안에 이미 들어 있다.
4. **새로 더한 것 ② (run114 넘김)**: 잠금 칸은 run60 · run69 처럼 **3.8 "잡고 풂"** 으로. 결과서 잠금 7 + run114 의 셋 = 10칸. → 2.5 에서 천천히.
5. `__@@_PchSym_…` 7칸은 그대로 세고 7.3 에 메모만 (전역 수에서 뺄지 사용자 결정) → 2.10.

비유로: run114 해설 2.2 의 점검표 그대로다. "누가 넣었나 적음" 칸과 "모름" 칸 둘뿐. 이번엔 **열쇠 상자** 를 어느 칸에 적을지를 정했다 — 문 여는 동안 꺼내 썼다가 제자리에 걸어 둔 열쇠는 "누가 넣었나 적음" 쪽이다.

### 2.3 한 줄씩 읽는 법

run 문서 "정적 — 호출 지점" 표에서 셋을 골라 "누가 / 어디서 / 누구를 / 무엇을 위해" 로 읽는다.

#### 줄 ①: `ntdll!LdrGetProcedureAddressForCaller | +0x53 | +0x58 | ExecuteHotpatchTestRuntimeFunction`

- **누가**: `LdrGetProcedureAddressForCaller` — 함수 주소 찾기. 뒤5 kernel32 준비 (`LdrpInitializeKernel32Functions` call `+0xfa` · `+0x12b`) 가 부른다 `[정적]`.
- **어디서**: call `+0x53`, 돌아올 자리 `+0x58`. 함수 **맨 앞** 이다.
- **누구를**: `ExecuteHotpatchTestRuntimeFunction` (34B) — 이름으로 보면 핫패치 시험용 함수 `[추측: 이름]`.
- **무엇을 위해**: 안 `+0x17` 이 칠판 칸 `RuntimeGlobal` 에 **늘 0** 을 쓴다. 돌아오자마자 `+0x58 GetHotpatchTestRuntimeFunctionState` 가 그 칸을 읽는다 `[정적]`.
  - 곧 "쓰고 바로 읽는" 짝이다. 왜 "늘" 인지는 2.4 ①.

#### 줄 ②: `ntdll!LdrpInitializeExecutionOptions | +0x9d | +0xa2 | LdrpInitializeCriticalSectionExceptionGlobalMitigation`

- **누가**: `LdrpInitializeExecutionOptions` — 묶음 4, IFEO 읽기 함수 (`LdrpInitializeProcess +0x531` 이 부름).
- **어디서**: call `+0x9d`. 이 자리가 중요하다 — **IFEO 건너뛰기 (`+0xa6` · `+0xad`) 보다 앞** 이다 `[정적]`.
- **누구를**: `LdrpInitializeCriticalSectionExceptionGlobalMitigation` (rva `0xfaa6c`, 203B). 이름을 풀면 "임계 구역 예외 · 전역 완화" — 이 글에선 **교착 완화 함수** 라고 줄여 부른다.
  - 교착 (deadlock) = 두 직원이 서로 상대 열쇠를 기다리며 영영 멈추는 것 `[추측]` (일반적으로 알려진 설명).
  - 칸 이름 `RtlpRaiseExceptionOnPossibleDeadlock` 을 보면 "교착일 수 있을 때 예외를 던질지" 를 정하는 스위치로 보인다 `[추측: 이름]`.
- **무엇을 위해**: 스위치 칸에 0 을 쓰고, 레지스트리 `Session Manager` 키의 값 하나를 묻는다 (2.4 ③).
  - test.exe 는 IFEO 를 통째 건너뛰지만 (`Flags 0x4001` `[관찰]`), 이 함수는 그 **앞** 이라 **늘** 탄다.

#### 줄 ③: `ntdll!LdrpLoadWow64 | +0xf1 | +0xf6 | LdrGetProcedureAddress`

- **누가**: `LdrpLoadWow64` — WOW64 (32비트 층) 를 올리는 함수. `LdrpInitializeProcess +0x17c1` 이 부른다.
- **어디서**: call `+0xf1`, 고리 안에서 여러 번. `+0xd7 CMP EDI,6` 이 고리 끝 검사다.
- **누구를**: `LdrGetProcedureAddress` (함수 주소 찾기).
- **무엇을 위해**: `.rdata` `0x173830` 의 표 6항목 `{이름, 칸 주소}` 를 돌며, 찾은 주소를 **표에 적힌 칸** 에 채운다 `[정적]` → 2.8.
  - test.exe 는 `UseWOW64` 0 이라 `+0x1766` · `+0x176d JE → +0x1806` 으로 이 함수를 **안 부른다**.

### 2.4 0 을 씀 3 — 겉은 빈 칸인데 직원이 "0" 을 적고 간다

셋 다 사진은 `끝까지 0`. 하지만 [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법) 의 "0 을 씀" 이다 — 펜이 닿았다.

#### ① `RuntimeGlobal` (`0x1cddb8`, 4B) — 왜 "늘" 0 인가

`ExecuteHotpatchTestRuntimeFunction` 은 34B 짜리라 통째로 읽을 수 있다. 단계로 `[정적]`:

1. `+0x0 LEA RAX,[칸]` — 칸 **주소** 를 `RAX` 에 담는다.
2. `+0x7 CMP RAX,0x20` — 그 주소가 `0x20` 인가?
3. `+0xb JNZ → +0x17` — 아니면 `+0x17` 로 뛴다.
4. `+0x17 MOV [칸],0` — 칸에 0.

- 칸 주소는 ntdll 안 주소라 **절대 `0x20` 이 아니다**. 그래서 3번에서 **늘** 뛴다.
- 그 사이 `+0xd` 에는 `0x14` 를 쓰는 줄이 있다. 늘 건너뛰니 실행될 일이 없다.
  - **새 말 — 죽은 줄**: 코드에 있지만, 길이 늘 그 줄을 건너뛰어서 **실행될 일이 없는 줄**. run 문서 말로 "핫패치 시험이 본문을 바꿀 때만" 산다.
  - 비유: 칠판 앞에 "**이 칠판이 0x20 번지에 있으면** 14 를 적어라, 아니면 0 을 적어라" 고 써 붙인 것. 칠판은 절대 그 번지에 없으니 직원은 늘 0 을 적는다.
- 결과서도 처음엔 이걸 "죽은 저장" 으로 읽었다가 스스로 고쳤다 (run 문서 "정정" — 결과서 안 일이라 정정 대상은 아님).

#### ② `RtlFailedCriticalDebugAllocations` (`0x1d272c`, 4B) — run114 넘김 ①

- 쓰는 줄: `LdrpInitializeProcess +0x773 MOV [칸],R12D`. 그 앞 `+0x693 XOR R12D,R12D` 로 `R12D` = 0 `[정적]`.
- 왜 무조건인가 (단계로):
  1. `+0x76b JGE → +0x773` 이 있다. 그런데 뛰든 안 뛰든 **같은 `+0x773` 에 닿는다**.
  2. `+0x693 ~ +0x773` 사이로 **밖에서 끼어드는 점프가 없다** — 결과서가 점프 목적지를 전부 셌다.
  3. 그러니 `+0x693` 에서 만든 0 이 그대로 `+0x773` 에 쓰인다.
- 이 칸을 **키우는** 줄은 `RtlpAddDebugInfoToCriticalSection +0x2bd LOCK INC` 하나 — 열쇠 꼬리표 (임계 구역 디버그 정보) 할당이 **실패할 때만** 이다.
- 칸 이름 그대로 "꼬리표 달기 실패 횟수" 칸으로 보인다 `[추측: 이름]`. 문 여는 날 0 으로 맞춰 두고, 실패할 때만 올린다.
- 이번에 7.5 실마리 줄에 `(해결됨 → run116)` 이 붙고 3.8 로 옮겼다.

#### ③ `RtlpRaiseExceptionOnPossibleDeadlock` (`0x1d4009`, 1B) — 교착 완화 스위치

2.3 줄 ② 의 함수 안이다. 갈림길이 여럿이라 천천히 `[정적]`:

| 순서 | 줄 | 하는 일 | test.exe 는 |
|---|---|---|---|
| 1 | `+0x1b CMP [LdrpIsSecureProcess],0` · `+0x22 JNZ → +0xac` | 보안 프로세스면 바로 `+0xac` 에서 스위치 **1** | `LdrpIsSecureProcess` (`0x1d28e0`) 0 `[관찰]` P1a · P1b · P2 → 안 뜀 |
| 2 | `+0x44 MOV BYTE [칸],0` | 스위치 **0** | **여기서 0 을 씀** |
| 3 | `+0x4b` `NtOpenKey` (`Session Manager`) | 키 열기. 실패면 `+0x52 JS` 로 끝 | 열리는지는 모름 (동적 2) |
| 4 | `+0x7d` `NtQueryValueKey` (`RaiseExceptionOnPossibleDeadlock`) | 값 하나 묻기 | |
| 5 | `+0x89` `NtClose` | 키 닫기 | |
| 6 | `+0xb5` · `+0xbc` · `+0xc3` · `+0xc8 JBE` → `+0xca → +0xac` | 종류 4 · 길이 4 · 값 > 0 이면 스위치 **1** | 값 없음 → 0 그대로 |

- 그래서 test.exe 에선 2번이 쓴 **0** 이 끝까지 간다. 사진 `끝까지 0` 과 맞는다 `[정적 + 사진]`.
- **덤으로 알게 된 것**: 이 길에 **syscall 셋** 이 있다 (키가 열리면). IFEO 를 건너뛰는 가게도 레지스트리를 한 번 더 본다는 뜻이다.
  - 키가 정말 열리는지는 정적으로 못 정한다 → 동적 2 (4절).
- 비유: 문 열 때 "교착 경보기" 스위치를 일단 **끔** 으로 내려 두고, 관리사무소 서류함 (`Session Manager`) 에 "경보기 켜라는 지시 있나?" 를 묻는다. 지시가 없으면 끈 채로 둔다. 보안 가게라면 묻지도 않고 바로 켠다.

#### sast/111 의 "0 을 씀 후보" 다섯은?

- sast/111 은 "init 줄기 직접 줄이라 0 을 씀 **후보**" 로 다섯을 넘겼다.
- 이번에 보니 0 을 쓰는 건 `RtlFailedCriticalDebugAllocations` **하나** 뿐이다.
- 나머지 넷 (`RtlpForceCSToUseEvents` · `LdrpForkActiveLock` · `LdrpForkConditionVariable` · `LdrpRedirectionModule`) 은 **조건부 안 탐** 이다 (2.6 표). sast/111 결과서 쪽 정정이고, 짐 지도 7.45 ① 에 `(해결됨 → run116)` 으로 적혔다.
- run114 문서는 그때 "후보" 로만 적었으니 run 문서 정정은 없다.

### 2.5 잠금 칸 10 — 꺼내 썼다가 다시 걸어 둔 열쇠

#### 새 말: 잡고 풂

- **잡고 풂** = 잠금 칸 (열쇠) 을 문 여는 길에 **잠갔다가 다시 풀어서**, 끝나면 처음 값으로 돌아오는 것.
  - 짐 지도 3.8 에는 이런 칸을 "잡고 푸는 락 — ② 에서 쓰고 처음 값으로 돌아옴" 줄로 적는다 (run60 · run69 선례).
- 비유: 열쇠 걸이에 걸린 열쇠. 직원이 꺼내 문을 잠그고, 일을 마치면 풀어서 다시 건다. 저녁에 사진을 찍으면 **아침과 똑같이 걸려 있다.** 하지만 "아무도 안 만진 열쇠" 와는 다르다 — 낮에 손을 탔다.

#### 판단 기준 (run 문서 한 줄)

- **잡고 푸는 줄이 지도 안 함수에 있으면 3.8, 없으면 "아무도 안 씀".**
  - 잡고 푸는 줄 = 칸 주소를 `lea` 로 잠금 함수에 넘기는 줄, 또는 SRW 락 빠른 길 (`LOCK CMPXCHG` · `LOCK BTS`).
- 10칸 **모두** 지도 안에 줄이 있었다. 그래서 "아무도 안 씀" 으로 남긴 잠금 칸은 **없다**.

#### 10칸 — 대표 줄과 "탔나"

| 이름 | 잡고 푸는 줄 (대표) | test.exe 가 그 길을 타나 |
|---|---|---|
| `LdrpLoaderLock` (임계 구역) | `_LdrpInitialize +0xf3 LEA` | 탐 — P1b 사진이 **잡힌 상태** (`LockCount −2` · `RecursionCount 1` · `OwningThread 0x26ec`), P2 풀림 `[관찰]` |
| `LdrpDllNotificationLock` (임계 구역) | `LdrpSendPostSnapNotifications +0x30` 등 lea 14곳 | 탐 (모듈 알림 길 — run66) |
| `LdrpModuleDatatableLock` (SRW) | `LdrpFindExistingModule +0x21 LEA` · SRW 빠른 길 수십 곳 `[추정]` | 탐 (모듈 찾기) |
| `RtlCriticalSectionLock` | `RtlpAddDebugInfoToCriticalSection +0xbe LEA` | 탐 — 같은 함수가 꼬리표 목록 `RtlCriticalSectionList` 를 씀 (run114) |
| `RtlpCachedPathLock` | `LdrpReleaseDllPath +0x2f LEA` · `+0x55 LOCK BTS` | 탐 (DLL 경로 풀기) — 정확한 첫 줄은 동적 |
| `LdrpPathLock` | `LdrpComputeLazyDllPath +0x19` · `+0xbf LEA` | 탐 — run114 가 같은 사슬 아래 옆 칸 `0x1d2358` 값을 봄 |
| `LdrpDllDirectoryLock` | `RtlpComputeDllPath +0x52` 등 LEA | 탐 — 같은 사슬 |
| `SbpContextLock` | `SbUpdateSwitchContextBasedOnDll +0x1ea` 등 LEA | 함수는 탐. 이 잠금 줄이 같은 갈래인지는 안 풂 `[추정]` |
| `MuiCacheSWRLock` | `LdrpGetFromMUIMemCache +0xce LOCK CMPXCHG` | 줄은 지도 안. **탔는지 모름** — 조건부일 수 있음 `[추정]` |
| `RtlpSecMemLock` | `RtlpCallSecureMemoryCallbacks +0x2a` 등 lea | 줄은 지도 안. **탔는지 모름** — 조건부일 수 있음 `[추정]` |

- 근거는 모두 `[정적]` (`.asm`). 마지막 둘과 `LdrpModuleDatatableLock` 의 `+0x17a` 는 `.asm` 이 없어 결과서 objdump 풀이로 본 `[정적: objdump]` 이다.
- 처음 값은 10칸 모두 **파일 그대로** 다 (임계 구역 둘은 파일 값, 나머지 여덟은 0). P2 에서 모두 처음 값.
- `LdrpLoaderLock` 의 7.3 라벨은 `≤P1a*` 그대로 둔다 (`*` = 뒤에 바뀜 — P1b 잡힘).

#### 아직 확인 아님 — 마지막 둘

- `MuiCacheSWRLock` · `RtlpSecMemLock` 은 기준 ("줄이 지도 안") 대로 3.8 로 옮겼다.
- 그런데 **탔다는 근거는 없다.** 그래서 짐 지도 근거 칸에 "조건부일 수 있음" 을 달았다.
- 그대로 둘지는 셈 일관성 거리 → 정적 **Q205 ③** (4절) · 6절 결정 ③.

#### 앞으로의 규칙 (run 문서 "도구 메모")

- 잠금 칸은 이번 바퀴로 "잡고 푸는 줄이 길 안이면 3.8" 하나로 맞췄다 (run60 · run69 · run116).
- 다음부터 결과서가 "쓰는 줄 없음 (잠금 칸)" 으로 적어도 같은 규칙으로 센다 — 오케스트레이터 확인 거리.

### 2.6 조건부 안 탐 98 — test.exe 같은 가게엔 해당 없음

98칸 모두 **값 = 파일 처음 값 = 0 = 사진** 이다 (`.00cfg` 는 사진이 없음 — 2.9). 쓰는 줄은 지도 길 안에 있다. test.exe 가 그 조건을 안 채울 뿐이다.

run 문서 3절 표를 **조건별로** 줄였다. 한 줄 = 한 조건 (대표 줄만).

| 묶음 (칸 수) | 그 칸을 쓰는 때 | test.exe 는 왜 안 타나 `[정적]` |
|---|---|---|
| App Verifier (`.data` 11 · `.mrdata` 4) | 점검관이 온 날 | `AvrfAppVerifierMode` 0 → `LdrpInitializeProcess +0x1568 CMP` · `+0x156f JE → +0x1753` (다른 문 셋은 `NtGlobalFlag` 0 · KUSD `+0x3a0` · PEB `+0xbc` 깃발) |
| shim 엔진 콜아웃 (`.mrdata` 9) | 호환성 고치기 엔진을 올릴 때 | `PEB.pShimData` 0 → `+0x19e3 JE → +0x19f4` (sast/14 · 61) |
| WOW64 (`.mrdata` 5) | 32비트 층을 올릴 때 | `UseWOW64` 0 → `+0x176d JE → +0x1806` |
| WIL 기능 칸 (`.data` 13) | ApiSet 항목에 기능 번호가 붙었을 때 · 패키지 앱 · 옛 DOS 장치 이름 갈래 `[추정]` | 2.7 |
| SxS 보존 (3) | 보존 스위치 `g_SxsKeepActivationContextsAlive` 가 켜질 때 | 그 스위치 칸을 **쓰는 코드가 없다** |
| IFEO (3) | 특별 지시를 읽을 때 | `+0xa6 TEST [PP+8],0x4000` · `+0xad JNZ → +0x604` (`Flags 0x4001` `[관찰]`) |
| 언로드 기록 (4) | DLL 을 실제로 내릴 때 | init 길의 부르는 곳은 오류 · 옵션 갈래 |
| import 재지정 (4) | `RedirectionDllName` 이 있을 때 | 비어 있음 (`""` `[관찰]` P2:3252) → `+0x56 JNE` 로 반환 |
| 경로 (4) | 사용자별 Windows 폴더 · `DllPath` · 상대 경로 오류 보고 | 길이 0 (run114) · `DllPath ""` · 오류 갈래 `[추정]` |
| LoadAsData 표 (3) | 파일 경로로 자원을 찾을 때 | 그 길로 오면 반드시 써지는데 사진 0 → 안 탐 `[정적 + 사진]` |
| MUI (7) | `.mui` 대체 모듈 · 언어 갈래 | 사진 0 = 안 탐 |
| NLS (2) | 매니페스트에 `activeCodePage` 가 있을 때 | 없음 (sast/52) |
| 힙 디버그 · 추적 (6) | `NtGlobalFlag` 디버그 비트 · 힙 정보 클래스 4 · 힙 추적이 켜진 힙 | `NtGlobalFlag` 0 · init 은 클래스 1 · 7 만 · 추적 힙 아님 `[추정]` |
| 스레드 풀 (3) | 직렬화 풀 · 풀 없는 멤버 갈래 | 그 갈래 아님 `[추정]` |
| 오류 보고 (3) | DLL 열기 거부 · 하드 에러 · DLL 못 찾음 | 오류 없음 |
| 복제 자식 (2) | `RtlCloneUserProcess` 로 만든 자식 (`InheritedAddressSpace ≠ 0`) | test.exe 0 `[관찰]` |
| TLS (2) | 지연 반환 · 이미 쓰이는 슬롯 갈래 | 그 갈래 아님 `[추정]` |
| 임계 구역 (2) | 오래 기다릴 때 · `PEB.AppCompatFlags` 비트 22 | `[추정]` · 비트 0 `[관찰]` |
| 그 밖 1씩 (4) | 예외 풀기 `[추정]` · TraceLogging 특성 등록 `[추정]` · 핫패치 상태 바이트 · SMT 레지스트리 값 | 각 조건 없음 (핫패치 상태 바이트 0 · SMT 값은 "이 VM 없음") |
| CFG `.00cfg` (4) | CFG **강제** 일 때 | 2.9 |

- 합: `.data` 76 + `.mrdata` 18 + `.00cfg` 4 = **98**.
- `[추정]` 이 붙은 11칸 (결과서 "다음 요청 후보") 은 **쓰는 줄은 `[정적]`, 조건 점프 줄까지는 안 풀었다.** 값 (0) 은 닫혔다 → 정적 **Q205 ①**.
- 비유: [run114 해설 2.9](run114-ntdll-globals-early-해설.md) 그대로다. "점검관이 온 날 · 호환성 고치기 · 32비트 손님 · 설정 쪽지에 코드 페이지가 적힌 날 · 교환원이 엄격 모드일 때" 만 채우는 칸이다. test.exe 는 그 어느 날도 아니다.

#### 한 줄 읽기 — 복제 자식 칸 둘

1. `_LdrpInitialize +0x336 · +0x33d` 가 `LdrpForkActiveLock` · `LdrpForkConditionVariable` 을 쓴다 `[정적]`.
2. 그 앞 `+0x62 CMP [PEB+0],BL` · `+0x64 JNE → +0x284` — PEB 첫 바이트 `InheritedAddressSpace` 가 0 이 아닐 때만 그 갈래로 간다.
3. `InheritedAddressSpace ≠ 0` 은 `RtlCloneUserProcess` 로 만든 **복제 자식** 일 때다 (run 문서).
4. test.exe 는 0 `[관찰]` → 안 탄다 → 두 칸은 처음 값 0.
5. sast/111 은 이 둘을 "0 을 씀 후보" 로 넘겼었다. 이번에 조건부로 바로잡혔다 (2.4 끝).

### 2.7 WIL 기능 칸을 가른 규칙 — 결과서 1.2

[run62 해설 2.9](run62-tp-etw-init-해설.md) 의 **기능 스위치 표** 이야기다. 표의 줄 (기술자) 이 칠판의 어느 칸에 답을 적을지 들고 있다.

- 기술자 (`.rdata` `0x1b7cd0 ~ 0x1b80f0`, 항목 `0x38`B 19개) `[정적]`:
  - `+0x8` 상태 칸 주소 · `+0x10` 보고 칸 주소
  - `+0x25` · `+0x26` · `+0x27` 바이트
  - `+0x28` 필요 기능 목록

#### 새 말: 상수 기능

- **상수 기능** = 답이 **미리 정해져 있어서** 물어볼 필요가 없는 기능. 기술자 `+0x25` 또는 `+0x26` 이 0 이 아니면 평가하지 않고 `+0x27` 만 읽는다 (`wil_details_AreDependenciesEnabled +0x20 ~ +0x2a`) `[정적]`.
- 비유: 스위치 표에 "이 줄은 **늘 켜짐** (인쇄됨)" 이라고 박힌 줄. 직원은 본사에 안 묻고, 칠판 답 칸도 안 채운다.

#### 그래서 칸이 이렇게 갈린다

1. 상수 기능이면 → 그 기능의 **상태 칸은 안 써짐** (아무도 안 씀 쪽 — 2.10 의 "WIL 상수 기능 상태 칸 5").
2. 둘 다 0 이면 → `…Reevaluate +0xa0` 이 상태 칸을 쓴다. 다만 그 평가가 **ApiSet 항목에 기능 번호가 붙었을 때** (`ApiSetpResolveHost +0x204`) · **패키지 앱일 때** 같은 갈래 안이면 조건부 안 탐.
3. **보고 칸** 은 `wil_details_IsEnabledFallback +0x43 TEST ESI,ESI` · `+0x45 JNZ` — 보고 종류가 0 이 아닐 때만 `+0x72 ReportUsageToService` 가 쓴다.
4. 사진 `ImplVal` 0 · `KVS` `0x40147` (run114) 이 이 규칙과 맞는다 `[정적 + 사진]`.

### 2.8 WOW64 표 — 짐작이 정확으로 바뀐 칸

#### 새 말: 표 구동 쓰기

- **표 구동 쓰기** = 칸 주소를 코드 줄에 직접 안 적고, **`{이름, 칸 주소}` 짝을 적은 표** 를 고리로 돌며 칸을 채우는 것.
- 그래서 "이 칸에 쓰는 명령" 을 주소로 훑으면 **안 잡힌다.** 칸 주소가 코드가 아니라 `.rdata` 표에만 있어서다.
- 비유: 직원이 칠판 칸 이름을 외워서 쓰는 게 아니라, **체크리스트 종이** 를 들고 "1번 줄: 이 이름 찾아서 저 칸에" 를 6번 반복하는 것. 칠판 칸만 봐서는 누가 썼는지 모르고, 체크리스트를 봐야 안다.

#### 무엇을 찾았나 `[정적]`

- `LdrpLoadWow64 +0xcd LEA R15` = `.rdata` `0x173830` 표. 항목 `0x10`B 6개 `{이름 포인터, 칸 주소}`.
  - 첫 항목: `Wow64LdrpInitialize` → `0x1e70c8` (`g_LdrpWow64LdrpInitialize`). 그 밖 다섯.
- `+0xd7 CMP EDI,6` 고리가 `+0xf1 LdrGetProcedureAddress(…, R9 = 칸 주소)` 로 채운다.
- 결과서의 훑기 스크립트는 레지스터에 담은 포인터로 쓰는 줄을 못 잡는다 (run 문서 "방법" 한계). 그래서 이 표는 `.asm` 을 직접 읽어 보탰다.

#### 그래서

- 3.9 `g_LdrpWow64LdrpInitialize` 줄: run17 `[추측]` → **`[정적]`** (짐작 → 정확).
- 나머지 다섯 (`Wow64ApcRoutine` 등) 은 새 줄 — 조건부 안 탐 (test.exe 는 WOW64 아님).
- 짐 지도 2장 `.mrdata` 짐작이 1 → **0** 이 된 까닭이 이것이다.

### 2.9 `.00cfg` 4칸 — 사진이 못 읽은 칸 (결과서 질문 2)

`.00cfg` 는 ntdll 안 CFG 쪽 함수 포인터 칸이 모인 작은 구역이다 (rva `0x1e6000`, 크기 `0x28`). 첫 칸 `0x1e6000` (`__guard_dispatch_icall_fptr`) 은 이미 3.10 에 있다. 이번엔 나머지 넷.

| 칸 (rva) | 파일 값 (재배치 뒤) | 쓰는 줄 | P2 까지 값 (짐작) |
|---|---|---|---|
| `__guard_xfg_check_icall_fptr` (`0x1e6008`) | `ntdll+0xfac60` (`_guard_check_icall_nop`, `ret 0`) | `LdrpCfgCheckRoutineCallback +0x25 · +0x49` | 파일 값 그대로 |
| `__guard_xfg_dispatch_icall_fptr` (`0x1e6010`) | `ntdll+0x1664b0` | `LdrpCfgDispatchRoutineCallback +0x25 · +0x49` | 파일 값 그대로 |
| `__guard_xfg_table_dispatch_icall_fptr` (`0x1e6018`) | `ntdll+0x1664b0` | 위와 같음 | 파일 값 그대로 |
| `__castguard_check_failure_os_handled_fptr` (`0x1e6020`) | 0 (`.reloc` 없음) | `LdrpCgRoutineCallback` | 0 그대로 |

단계로 `[정적]`:

1. 쓰는 줄은 모두 **콜백** 안이다. 콜백은 `LdrpCfgProcessLoadConfig +0x70c` 의 **간접 호출** 로만 불린다.
2. 그 간접 호출은 **CFG 강제일 때만** 간다: `+0x3a9 TEST R12D` · `+0x3ac JZ → +0x454`.
3. test.exe 는 `LdrControlFlowGuardEnforced` 0 (`LdrSystemDllInitBlock +0xb8` 0) → `+0x454` 로 반환. 콜백 안 탐.
4. CastGuard 칸은 따로 `+0x17c BT R15D,0x18` · `+0x181 JC` (`GuardFlags` 비트 24 꺼짐) 로도 막힌다.
5. 그러니 네 칸은 **파일에 인쇄돼 온 값 그대로** 일 것이다.

- 아직 확인 아님: 사진이 없어서 "그대로" 는 **짐작** 이다 (결과서 판단). 그래서 짐 지도 2장 `.00cfg` 아는 것 5칸 모두 짐작으로 셌다.
- 이 네 칸을 **읽는** ntdll 코드는 없다 (코드 참조 0 — 로드 구성 필드와 `.reloc` 만).
  - 옆 `0x1e6000` 만 CFG 간접 호출마다 읽힌다. 교환원이 매번 쳐다보는 전화번호 칸이다.
- 왜 사진 8장이 모두 `????` 였는지는 정적으로 못 정한다 `[공백]` → 동적 1 → **Q79** (4절).

### 2.10 7.3 에 남는 124 — 가름은 끝, 표시만

2.2 셈 기준대로 7.3 에 두고 `(안 씀)` · `(범위 밖)` 표시만 붙였다.

#### 아무도 안 씀 44 — 이름 칸에 쓰는 명령이 ntdll 에 없음

- `.data` 43:
  - WIL 보고 칸 12 (직접 묻는 함수가 없음 · 보고 종류 0)
  - WIL 상수 기능 상태 칸 5 (2.7 의 1번)
  - WIL 훅 포인터 3 · SxS 디버그 스위치 3 · 힙 디버그 스위치 5
  - `RtlpHpNullGUID` · `NlsEmptyLeadByteInfoTable` · `RtlpCsVerifyDoNotBreak` · `HotPatchSpareGlobals`
  - 그 밖 4 (`RtlpFreezeTimeBias` · `g_dwLastErrorToBreakOn` · `LdrpFailureData` · `g_LdrBreakOnLdrpInitializeProcessFailure`)
  - **`__@@_PchSym_…` 7** — 아래
- `.mrdata` 1: `LdrpUnalignedUserCallTargetBitMap`.

#### 새 말: PCH (precompiled header) 표시

- **PCH 표시** = 컴파일러가 **미리 만들어 둔 머리 파일** (precompiled header) 의 흔적이 전역 **이름처럼** 남은 것 `[추측]` (일반적으로 알려진 설명).
  - 그래서 **변수가 아닐 수 있다.**
- 이번 근거 `[정적]`: `__@@_PchSym_…` 이름 7개는 **코드 참조가 0** 이다. 읽는 줄도 쓰는 줄도 없다.
- run 문서 판단: "변수가 아니라 컴파일러 PCH 심볼 표시일 가능성 `[추정]`".
- 비유: 칠판에 **이름표만 붙어 있고 칸은 없는** 자리. 칠판을 만든 공장이 조립하다 남긴 검수 스티커 같은 것이다. 이걸 칸으로 셀지 말지는 정해야 한다.
- 지금은 **그대로 센다** (전역 438 안). 빼면 전역 438 → 431, 모름 157 → 150 → **사용자 결정** (6절).

#### 범위 밖 길에서 씀 `[추측]` 80 — 쓰는 함수가 지도 밖

쓰는 함수가 지도 밖이고, 지도 안 호출자도 없다.

- `.data` 77: 힙 누수 검출 13 (`RtlDetectHeapLeaks` 류) · 미예상 코드 길 텔레메트리 8 · 텔레메트리 6 · 프로퍼티 저장소 · 포인터 매핑 5 · `RtlSetIoCompletionCallback` 5 · 잠긴 메모리 4 · ETW 로거 4 · 기능 토글 `__ft_*` 4 · WIL 밖 기능 4 · 힙 디버그 3 · 그 밖 (2개씩 · 1개씩).
- `.mrdata` 3: `RtlpProtectedPolicies*` (`RtlSetProtectedPolicy` 만).
- 비유: "다른 날 — 가게 문 닫는 날 · 사고 난 날 · 손님이 따로 부탁한 날" 에만 쓰는 칸.

#### 7.3 전체로 보면

- run114 가 남긴 33 (아무도 안 씀 19 · 범위 밖 14) 과 합치면 **7.3 모름 157 = 아무도 안 씀 63 · 범위 밖 94**.
- **모두 가름이 끝났다.** "쓴 자리를 정말 모르는" 전역은 **0** 이다 (run 문서).
- run114 해설 2.2 의 점검표 비유로: 이제 "모름" 칸에 남은 상자 157개에 **전부 연필 메모가 달렸다.** 메모 칸을 정식 칸으로 그릴지만 남았다.

### 2.11 간접 호출 — 결과서 질문 3

- 칸을 쓰는 줄이 **간접 호출 대상 안** 인 곳은 `.00cfg` 4칸 **하나** 뿐이다 — `LdrpCfgProcessLoadConfig +0x70c` (콜백 셋), 안 탐 (2.9) `[정적]`.
- `LdrpLoadWow64 +0xf1` 는 **직접 호출** 이다. 표를 돌 뿐이다 (2.8).
- `…Reevaluate +0xe0 · +0x106` 는 구독 포인터 둘인데 0 이다 — 2.10 의 "WIL 훅 포인터" (아무도 안 씀).

### 2.12 결과서와 다르게 적은 곳 — run 문서를 따른 까닭

| 무엇 | 결과서 | run 문서 | 까닭 |
|---|---|---|---|
| 조건부 안 탐 `.data` | 77 (전체 99) | 새 줄 **76** (전체 98) | `AvrfpLoaderEntry` 는 run105 가 이미 3.8 짐작 줄로 둠 |
| 잠금 칸 | "아무도 안 씀 · 잠금 칸 7" | **3.8 "잡고 풂" 10** | 셈 기준 (2.2 의 4번). run114 의 셋도 같이 옮김 |
| 7.3 숫자 | run114 **전** 305 기준 (B 안 203 등) | 지금 **268** 에서 셈 | 결과서가 쓸 때 run114 가 아직 반영 전이었다 |
| 구조체 모름 | "301" | **24** | 결과서의 301 은 옛 숫자 (run 문서) |
| 정확 / 짐작 | — | 새 `.data` 89 · `.mrdata` 18 은 **정확**, `.00cfg` 4 는 **짐작** | 쓰는 줄 · 주소 넘김 줄 · 잡고 푸는 줄이 칸 단위로 `[정적]` (run114 `RtlpDisableHeapLookaside` 선례). `.00cfg` 는 사진 없이 추정 |

#### 정확 / 짐작 — 왜 결정 거리가 남나

1. run114 는 "주소만 넘기는 줄" (IFEO 출력 칸 주소를 넘김) 만 있는 `RtlpDisableHeapLookaside` 를 **정확** 으로 셌다.
2. 이번에도 같은 꼴 (lea 출력 칸 — `AVrfpHandleTraces` · `AVrfpDebug` · `RtlpShutdownProcessFlags` · `LdrpRedirection*CalloutFunc` 둘 · `RtlpHpTagInitVar` 등) 을 **정확** 으로 셌다.
3. 그런데 run105 의 `AvrfpLoaderEntry` 는 같은 꼴인데 **짐작** 이다.
4. 한쪽으로 맞출지가 남았다 → 6절 결정 ③.
   - run 문서는 이걸 "셈 일관성 (오케스트레이터)" 몫으로 적었고, plan 결정 기록은 "사용자 결정 거리" 로 올렸다.

### 2.13 PE_emulator 에게 (run 문서 "PE_emulator 쪽", 짧게)

`../PE_emulator` 코드는 flow-writer 가 안 읽었다. 고칠지는 사용자 결정이다.

- **틀림 — 빈 덫 `LdrpInitializeCriticalSectionExceptionGlobalMitigation`** (`Src/ntdll/LdrInit.cpp:1445`).
  - 진짜는 칸에 0 을 쓰고 (`+0x44`) syscall 셋 (`NtOpenKey` `+0x4b` · `NtQueryValueKey` `+0x7d` · `NtClose` `+0x89`) 을 부른다 (2.4 ③).
  - 칸이 0 으로 시작하면 **값은 같다.** 하지만 `0xcc` 무늬로 채워 시험하면 다르고, syscall 셋이 빠져 있다. 동적 2 로 실제 반환을 본 뒤 고칠 거리.
- **맞음**: `LdrpInitializeProcess` 몸통이 진짜 코드라 `RtlFailedCriticalDebugAllocations` 기대 0 이 같다. 안 타는 조건의 입력 (`NtGlobalFlag` 0 · `InheritedAddressSpace` · `UseWOW64` 0 · `g_ShimsEnabled` 0 · `SmtDelayedConfiguration` 0 · 시스템 DLL 블록 `+0xb8` 0) 도 같다.
- **다름 (값은 같음)**: SMT — PE_emulator 는 `NtOpenKey` 를 "없음" 으로 둬서 일찍 끝난다. 칸은 같지만 syscall 수가 다르다 (진짜는 키가 열리면 `NtQueryValueKey` 5 · `NtClose` 1 `[추정]`).
- **확인 필요**: PEB `AppCompatFlags` · `pShimData` · KUSD `+0x3a0` · `RedirectionDllName` 을 0 · 빈 값으로 두는지, 활성화 문맥 응답, `LdrpCfgProcessLoadConfig` 가 진짜 몸통인지, `.00cfg` 칸에 `.reloc` 가 적용되는지.

### 2.14 디버거가 붙어서 달라지는 점

- 없다 `[추측]`. App Verifier · 힙 디버그 갈래는 `NtGlobalFlag` 로만 켜지고, KDNET 은 그 값을 안 바꾼다.
- 다만 **사용자 모드 디버거** 로 띄우면 힙 디버그 깃발이 켜져 `RtlpHeapInvalidBreakPoint` 등 힙 디버그 칸 길이 열릴 수 있다 `[추측]`.

### 2.15 정리 — 비유로 한 번 더

- 관리실 칠판의 **끝까지 빈 칸** 241개를 하나씩 들여다봤다.
  - 3칸은 직원이 "0" 을 적고 갔다. 그중 하나는 "이 칠판이 0x20 번지면 14" 라는 절대 안 맞는 조건 덕에 **늘** 0 이다.
  - 98칸은 "특별한 가게일 때만" 채운다. test.exe 는 평범한 가게다.
  - 10칸은 열쇠다. 낮에 꺼내 썼다가 저녁엔 제자리에 걸려 있다. 둘은 정말 꺼냈는지 아직 모른다.
  - 124칸은 아무도 안 만지거나 다른 날에만 쓴다. 7칸은 아예 칸이 아니라 **이름표만** 일 수 있다.
- 교착 경보기 스위치는 IFEO 를 건너뛰는 가게도 문 열 때 한 번 내리고, 서류함에 묻는다.
- 32비트 손님용 칸 여섯은 체크리스트 종이로 채운다 — 그래서 주소로 훑으면 안 보였다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 | — | ntkrnlmp · ntdll pdb 줄에 run116, 메모 한 줄 (run44 덤프 읽기 로그 셋을 다시 읽음 — 237칸 0, `LdrpIsSecureProcess` 0, `.00cfg` `????`. 새 동적 실행 없음) |
| 2.4 머리 | run 목록 … run114 | … run114, **run116** |
| 2.4 묶음 4 | — (교착 완화 함수 줄 없음) | **IFEO 를 건너뛰어도 교착 완화 함수 하나는 늘 부른다** (`+0x9d`) — 칸 0 + `NtOpenKey` · `NtQueryValueKey` · `NtClose` `[정적 + 사진]` |
| 2.4 뒤5 · 뒤9 | — | 뒤5 안 `ExecuteHotpatchTestRuntimeFunction` → `RuntimeGlobal` = 0 (늘) · 뒤9 SMT 레지스트리 (`NtOpenKey` · 값 다섯) `[정적]` |
| 4장 run114 줄 | ① Q78g2 · ② 가름 열 (268 중 36) · ③ 잠금 칸 셈 열림 | ① · ③ `(해결됨 → run116)`, ② 에 "run116 뒤 **157 모두 가름 끝**" 보탬 |
| 4장 새 줄 | — | run116 해결됨 · 동적 1 ~ 5 · 정적 ① ~ ⑤ · PE_emulator |
| 5장 | — | run116 두 표 |

- 한 줄로: **test.exe 길 자체는 그대로다.** 다만 묶음 4 에 "건너뛰어도 늘 타는 작은 길 + syscall 셋" 이 하나 생겼다. PE_emulator 의 syscall 시간표에 들어갈 후보다.

### 짐 지도 `baggage.md` — 숫자

#### 2장 표 줄

| 줄 | 쓴 자리 아는 것 | 모름 | ntdll 이 씀 |
|---|---|---|---|
| ntdll `.data` (438) | 200 → **289** (정확 197 → 286 · 짐작 3) | 238 → **149** | 197 → **286** |
| ntdll `.mrdata` (50) | 24 → **42** (정확 23 → 42 · 짐작 1 → **0**) | 26 → **8** | 22 → **40** |
| ntdll `.00cfg` (5) | 1 → **5** (정확 0 · 짐작 1 → 5) | 4 → **0** | 0 → **4** |
| **전역 3구역 합 (493)** | 225 → **336** | **268 → 157** | 219 → **330** |

- nt 가 쓴 칸은 그대로 (`.data` 6).
- `.mrdata` 정확이 19 늘었다 = 새 18 + `g_LdrpWow64LdrpInitialize` 짐작 → 정확 (2.8).

#### 7.3 사진 구간별 모름

| 구간 | 바퀴 전 | 바퀴 뒤 |
|---|---|---|
| `K1 이전` | 2 | 2 |
| `P1a~P1b` | 1 | 1 |
| `≤P1a` | 32 | **30** (잠금 둘) |
| `끝까지 0` | 229 (`.data` 207 · `.mrdata` 22) | **124** (`.data` 120 · `.mrdata` 4) |
| 못 읽음 | 4 | **0** |

- 7.1 "아는 전역 중 끝까지 0": 67 → **172** (= 296 − 124).
- "0 그대로 (사진)" 열 (`.data` 265* · `.mrdata` 31*) 은 **그대로** 다. 사진 분류라 안 고친다 (run66 선례).

#### 3장 줄

- 3.8 머리 run116 줄 · **새 줄 44** (89칸 — 0 을 씀 3 · 잠금 7줄 10칸 · 조건부 34줄 76칸).
- 3.9 머리 · 새 줄 3 (18칸) + 고친 줄 1 (`g_LdrpWow64LdrpInitialize`).
- 3.10 머리 · 새 줄 1 (4칸) + 첫 줄 메모 (`0x1e6000` 도 `LdrpCfgProcessLoadConfig` 가 모으는 다섯 중 하나라는 결과서 메모 — 셈은 그대로).
- 6장 공용 부품 새 줄 1 (잠금 잡고 풀기).

### 큰 그림에서

- **Q78g 가 다 끝났고, 그래서 Q78 (짐 지도 C) 의 정적 조각이 다 끝났다.**
- 전역 쪽은 "모름 157" 이라는 숫자가 남았지만, 그 157 은 **모두 이유가 붙은 칸** 이다. 에뮬레이터 쪽에서 보면:
  - 아무도 안 쓰는 칸 · 다른 날에만 쓰는 칸은 **ntdll 파일 바이트를 그대로 올리면** 처음 값이 맞을 것이다 `[추측]` — 값이 모두 "파일 처음 값" 이라서. run114 해설 2.16 처럼 PE_emulator 쪽은 **확인 필요** 다.
  - 조건부 칸은 **조건의 입력** (`NtGlobalFlag` · `UseWOW64` · `pShimData` …) 이 진짜와 같으면 같은 갈래를 탈 것이다 `[추측]` — 쓰는 줄이 그 조건 뒤에만 있어서. 그 입력 목록이 2.6 표와 2.13 에 있다.
- 남은 일은 **동적 확인 몇 개** 와 **셈 결정 셋** 이다 (4절 · 6절).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **동적 1** `.00cfg` 다섯 칸의 실제 값 (기대 `ntdll+0x166480 · 0xfac60 · 0x1664b0 · 0x1664b0 · 0`), 사진 8장이 왜 `????` 였나 `[공백]` | 사진이 못 읽음. 정적으론 "파일 값 그대로" 짐작까지 | 동적 **Q79** — `.pagein ntdll+0x1e6000` 뒤 `dq ntdll+0x1e6000 L5`, 안 되면 `!pte` |
| **동적 2** `NtOpenKey` 반환 둘 (`LdrpInitializeSmtDelayedSleep+0x20` · `LdrpInitializeCriticalSectionExceptionGlobalMitigation+0x50`) — 0 (열림) 이면 syscall 이 더 난다 | 레지스트리 키가 있는지는 실행 때 값 | 동적 **Q46** (읽기만, `r eax`) — PE_emulator syscall 시간표와 맞추는 용 |
| **동적 3** `LdrpInitMuiCrits` 첫 부름이 후보 셋 중 어느 것인가 | run114 D2 와 같음 | 동적 **Q46** (run114 몫으로 이미 있음) |
| 동적 4 (선택) `ApiSetpIsFeatureEnabled` 가 init 동안 몇 번 · `IsFeatureEnabled` 의 `ecx` — 0 번이면 ApiSet 기능 갈래 "안 탐" 증명 | 정적으론 조건까지만 | 동적 **Q46** (선택) |
| 동적 5 (선택) 핫패치 상태 바이트 (`db poi(ntdll+0x1d2908)+0xc L1`, P1b) = 0 — `LibLoaderTelemetryInitRunOnce` 가 0 인 까닭의 한 고리 | 실행 때 값 | 동적 **Q46** (선택) |
| **정적 ①** `[추정]` 11칸의 조건 점프 줄 (`LdrpGetDllPath` · `RtlpIsDosDeviceName_Ustr` · `LdrpHandleTlsData` · `TppCleanupGroupMemberInitialize` · `EtwpUpdateEnableInfoAndCallback` · `RtlpHpStackTraceConfig` 앞뒤) | 쓰는 줄까지만 보고 점프 조건은 안 풂. 값은 닫혀 급하지 않음 | 정적 **Q205 ①** (작음) |
| **정적 ②** 3.10 `__guard_dispatch_icall_fptr` (`0x1e6000`) 쓴 주인 — 지금 "모름" 인 짐작 줄을 ntdll 로 고칠지 | 결과서가 `LdrpCfgProcessLoadConfig` 다섯 칸 중 하나로 적었지만 (CFG 강제 때 콜백) 셈은 안 고침 | 정적 **Q205 ②** (작음) |
| **정적 ③** "탔는지 모르는" 잠금 둘 (`MuiCacheSWRLock` · `RtlpSecMemLock`) 의 잡고 푸는 줄이 test.exe 길에서 타나 | 줄이 지도 안이라 3.8 로 셌지만, 그 갈래를 탄 근거가 없음 (2.5) | 정적 **Q205 ③** |
| 셈 결정 셋 — 전역 가름 열 · PCH 7칸 · 정확/짐작 일관성 | 분석 문제가 아니라 **세는 약속** 문제 | **사용자 결정** (6절 끝) |

- 동적은 모두 결과서 판단 "급하지 않음 — Q104 VM 단계에 얹을 것" 이다. 멈춤점은 test.exe 조건을 붙인다.

---

## 5. 더 읽기

- run 문서: [`run116-ntdll-globals-zero.md`](run116-ntdll-globals-zero.md) — 요약 표, 호출 지점 표, 호출 관계 나무, 1절 (0 을 씀) · 2절 (잠금 칸) · 3절 (조건부 안 탐 표) · 4절 (7.3 에 남는 124) · 5절 (`.00cfg`) · 6절 (WIL · WOW64) · 7절 (간접 호출), 결과서와 다르게 적은 곳, 정정, "짐 지도 C 정적 조각 끝"
- 결과서: [`_workspace/sast/113_ntdll-globals-zero_result.md`](../../_workspace/sast/113_ntdll-globals-zero_result.md) — 판단 기준, 한눈에, 241칸 표, 1.1 (0 을 씀 풀이) · 1.2 (WIL 규칙) · 1.3, 질문 2 ~ 4, 동적으로 확인할 것, 다음 요청 후보
- 요청서: [`_workspace/sast/113_ntdll-globals-zero.md`](../../_workspace/sast/113_ntdll-globals-zero.md)
- flow.md: [기준 · 2.4](flow.md) (묶음 4 교착 완화 줄 · 뒤5 · 뒤9) · 4장 run114 · run116 줄 · 5장 run116
- 짐 지도: [`baggage.md`](baggage.md) 2장 (숫자 + 끝 "짐 지도 C 끝" 줄) · **3.8** · 3.9 · 3.10 · 6장 · 7.1 · **7.3** (가름 표시 · 취소선) · 7.5 · 7.45 · **7.47**
- 같은 셈 방식 앞 해설: [run114](run114-ntdll-globals-early-해설.md) (바로 앞 전역 조각 — 셈 기준 · 가름 열) · [run115](run115-kusd-boot-해설.md) (KUSD — 유저 구조체 모름 0, "어디까지 왔나" 표) · [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법) (네 가지 가름)
- 잠금 칸 선례: [run60](run60-func-table-mrdata-해설.md) (`LdrpInvertedFunctionTableSRWLock`) · [run69](run69-loader-pool-해설.md) (`TppPoolpListLock`)

---

## 6. 짐 지도 C (Q78) 를 돌아보며

새 사실은 없다. 짐 지도 2장 · 7.2 · 7.3 머리 · 8장과 run45 ~ run116 문서의 숫자를 한자리에 모은 것이다.

### 6.1 짐 지도 A · B · C 가 뭐였나

| 이름 | 대기열 | 무엇 | 비유 (앞 해설) |
|---|---|---|---|
| 짐 지도 A — 체크리스트 | Q76 (run43) | PDB 에서 칸 목록 베끼기 — 구조체 21개 직접 칸 **1,456**, ntdll 전역 **493** | 관리사무소의 **빈 양식 견본집** ([run43 해설](run43-baggage-checklist-해설.md)) |
| 짐 지도 B — 사진 | Q77 (run44) | 한 번의 실행을 8 지점 (K1 ~ P2) 에서 찍어 칸마다 처음 값이 생긴 **구간** 정하기 | 개업 직전 가게 안을 **사진** 으로 찍어 두기 ([run44 해설](run44-baggage-snap-해설.md)) |
| 짐 지도 C — 쓴 자리 · 줄기 | **Q78** (run46 ~ run116) | 칸마다 **누가 · 어느 줄에서** 썼나 (또는 왜 안 썼나) 를 코드로 찾기 | 준비물 하나가 제자리에 놓이기까지의 **배달 경로** ([run43 해설](run43-baggage-checklist-해설.md)) |

- Q78 은 너무 커서 시간 순서 조각으로 나눴다 (사용자 결정 2026-10-28): Q78a (힙) · Q78b (exe 섹션) · Q78c (프로세스 객체) · Q78d (첫 스레드) · Q78e (ntdll 앞쪽) · Q78f (KERNELBASE · KERNEL32 구간) · Q78g (KUSD 와 남은 전역).
- 이번 run116 의 Q78g2 가 그 **마지막 정적 조각** 이다.

### 6.2 처음엔 얼마나 몰랐나

run43 첫판 (짐 지도 8장 run43 줄):

- 구조체 직접 칸 **1,456** 중 이미 문서에 쓴 자리가 있던 것: 정확 138 · 짐작 43. 나머지 **1,275** 가 모름.
- ntdll 전역 **493** 중 아는 것 **54**. 나머지 **439** 가 모름.

### 6.3 줄어 온 길 — 구조체 (7.2 머리 그대로, 대표만)

| 언제 | 구조체 모름 | 그 바퀴 (Q78 조각이면) |
|---|---|---|
| run45 전 | **1,275** | — |
| run47 전 | 1,270 | run45 · run46 (Q78a 힙) 이 몇 칸 |
| run49 전 | 1,196 | run47 (Q78b exe 섹션 묶음) |
| run50 전 | 1,059 | run49 (Q78c1 `_KPROCESS` · 토큰 · 핸들 표) |
| run51 전 | 848 | run50 (Q78c2a `_EPROCESS` 만들 때) |
| run52 전 | 819 | run51 (Q78c2b `_EPROCESS` 뒤 구간) |
| run53 전 | 639 | run52 (Q78d1 `_KTHREAD`) |
| run54 전 | 470 | run53 (Q78d2 `_ETHREAD` · 트랩 프레임) |
| run57 전 | 337 | run54 (Q78d3 `_TEB`) |
| run112 전 | 300 | 그 사이 여러 바퀴가 몇 칸씩 |
| run113 전 | 218 | run112 (Q78d4 스레드 뒤 구간) |
| run115 전 | 126 | run113 (Q78e1 ntdll 유저 구조체) |
| **지금** | **24** | run115 (Q78g1 KUSD) — 유저 구조체 모름 0 |

- 구조체 1,456 의 지금 모습 (2장 아래 줄): 아는 칸 **938** (정확 907 · 짐작 31) + 아무도 안 씀 **202** + 범위 밖 `[추측]` **267** + 트랩 프레임 덮임 **25** + 모름 **24**.
- 구조체에는 "아무도 안 씀 · 범위 밖" **가름 열이 있다.** 그래서 가름이 끝난 칸은 모름에서 빠진다.

### 6.4 줄어 온 길 — 전역 (7.3 머리 그대로, 대표만)

| 언제 | 전역 모름 |
|---|---|
| run45 전 | **439** |
| run46 전 | 434 |
| run57 전 | 417 |
| run59 전 | 413 |
| run60 전 | 388 |
| run62 전 | 377 |
| run63 전 | 349 |
| run66 전 | 334 |
| run69 전 | 311 |
| run70 전 | 308 |
| run114 전 | 304 |
| run116 전 | 268 |
| **지금** | **157** (모두 가름 끝) |

- 전역은 Q78 조각 (run46 Q78a 힙 · run114 Q78e2 · Q78f · 이번 run116 Q78g2) 과, 그 사이 **다른 바퀴들** (run57 ~ run70 대 — NLS · 초기화 남은 일 · 역함수 표 · 기능 상태 · CFG · apphelp · 스레드 풀 · CPU 기능 등) 이 같이 줄였다. 마지막 두 걸음 (304 → 268 → 157) 이 run114 · run116 이다.
- 전역 493 의 지금 모습: 아는 것 **336** (`.data` 289 · `.mrdata` 42 · `.00cfg` 5) + 모름 **157**.
- 전역에는 가름 열이 **없다.** 그래서 가름이 끝났어도 "아무도 안 씀 · 범위 밖" 칸은 모름에 남는다 — 이게 6.5 의 157 이다.

### 6.5 남은 24 · 157 은 무엇인가

#### 구조체 24 — 모두 커널 (run115 그대로)

| 무엇 | 칸 | 왜 남았나 | 어디서 보나 |
|---|---|---|---|
| `_EPROCESS.SharedCommitCharge` | 1 | `K1 이전` 값 — 쓰는 줄 **후보만** 있음 (7.5) | 동적 Q97 (E5 — `MiInsertSharedCommitNode+0x33e` 후보) |
| VAD 칸 (`_MMVAD` 9 · `_MMVAD_SHORT` 14) | 23 | 사진이 `!vad` 목록만 떠서 **칸을 안 읽음** | 덤프 보충 읽기 Q79 (VAD 노드마다 `dt`) |

- 유저 구조체 일곱 (PEB · 매개변수 · TEB · 명부 · 명부 줄 · 연결 카드 · KUSD) 은 모름 **0** 이다 (run115).

#### 전역 157 — 모두 가름 끝

| 가름 | 칸 | 누가 남겼나 | 무엇들 (대표) |
|---|---|---|---|
| 아무도 안 씀 | **63** | run114 19 · run116 44 | CRT 표 · 스위치 표 항목 · `_DEBUG` 이름 칸 (run114) · WIL 보고 · 상수 기능 칸 · 디버그 스위치 · **PCH 표시 7** (run116) |
| 범위 밖 길 `[추측]` | **94** | run114 14 · run116 80 | 공개 API · 오류 · 종료 길 (run114) · 힙 누수 검출 · 텔레메트리 · ETW 로거 · `RtlpProtectedPolicies*` (run116) |

- 구역으로 나누면 `.data` 149 · `.mrdata` 8 · `.00cfg` 0.
- 그러니 **쓴 자리를 정말 모르는 전역은 0** 이다 (짐 지도 2장 끝 run116 줄).
- 비유로: 점검표의 "모름" 칸에 상자 157개가 남았는데, **상자마다 연필 메모가 다 붙어 있다.** "아무도 안 넣음" 이거나 "다른 날 다른 사람이 넣음".

#### `[공백]` 으로 남은 작은 것 (run 문서 "짐 지도 C 정적 조각 끝")

- `.00cfg` 4칸 P2 값 — 사진 읽기 불가 (동적 1 → Q79)
- `[추정]` 11칸의 정확한 조건 점프 — 값은 닫힘 (Q205 ①)
- `LdrpInitMuiCrits` 첫 부름 줄 — 후보 셋 (동적 3 → Q46)

### 6.6 사용자가 정할 것 셋 (이름만)

모두 **분석이 아니라 세는 약속** 이다. plan 결정 기록은 "묶음 끝에 하나씩" 묻기로 적었다.

1. **전역 가름 열** — 전역에도 "아무도 안 씀 · 범위 밖" 열을 둘지. 두면 7.3 모름 **157 → 0** (7.45 ② · 7.47 ②).
2. **PCH 7칸** — `__@@_PchSym_…` 7칸을 전역 수에서 뺄지. 빼면 전역 **438 → 431**, 모름 **157 → 150** (7.47 ③).
3. **정확 / 짐작 셈 일관성** — 주소만 넘기는 줄이 있는 칸을 어느 쪽으로 셀지. run105 `AvrfpLoaderEntry` 는 짐작, run114 · run116 은 정확 (2.12 · 7.47 ④).

- 그다음 큰 결정은 CLAUDE.md 의 약속대로다: "어디까지 구현할지는 목록이 다 된 뒤 사용자와 정한다."

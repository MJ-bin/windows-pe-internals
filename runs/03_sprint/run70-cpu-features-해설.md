# run70 cpu-features — 해설

> 이 글은 [run70 문서](run70-cpu-features.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q133** — PE_emulator 요청 21: "SymCrypt CPU 기능 찾기 · KUSD `ProcessorFeatures` · 스택 건드리기" (`_workspace/plan.md`, 이번에 **끝**)
  - 새로 생긴 할 일: **Q135** (정적, 맨 뒤) — 4절.
  - 동적 몫은 **Q104 VM 단계** 에 얹었다.
  - 다음 항목: **Q104 VM 단계** (사용자가 VM 시간에 한다. "VM 전까지 쭉" 은 여기서 멈춘다).
- 근거:
  - run 문서 [`run70-cpu-features.md`](run70-cpu-features.md) (가장 중요)
  - 요청서 [`_workspace/sast/65_cpu-features.md`](../../_workspace/sast/65_cpu-features.md)
  - 결과서 [`_workspace/sast/65_cpu-features_result.md`](../../_workspace/sast/65_cpu-features_result.md) (판정: **분석성공**)
  - flow.md 변경: 2.1 `MmCreatePeb` 표 (`+0x318` 줄) · 2.3 (새 부팅 메모 "KUSD `ProcessorFeatures` 를 채우는 곳") · 2.4 (새 절 "CPU 기능 감지", `_LdrpInitialize` · `LdrpInitializeProcess` 줄 하나씩) · 4장 · 5장
- 이번 바퀴는 **정적만** 했다.
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그를 flow-writer 가 **다시 읽은 것** 이다. 새 실행이 아니다.

이미 푼 말은 **링크만** 건다.

- `KUSER_SHARED_DATA` (KUSD) = 관리사무소가 붙여 두는 **공용 게시판** → [run18 해설 2.11](../02_sprint/run18-proc-init-setup-해설.md)
- 게시판의 `ProcessorFeatures` = **기본 기능 64칸 체크리스트**, 그 뒤 확장 128칸은 클래스 `0xfa` 로 따로 받음 → [run65 해설 2.8](run65-ldr-init-bodies-해설.md)
- `RtlIsProcessorFeaturePresent` = 게시판 체크리스트를 **읽기만** 하는 함수, `__cpu_features_init` = 장비 점검표 → [run59 해설 2.9](run59-init-leftovers-해설.md)
- 게시판의 XState 칸 `+0x3d8` → [run55 해설](run55-context-length2-해설.md)
- 덫 = PE_emulator 가 진짜 ntdll 함수를 Unicorn 안에서 돌리는 것 → [run58 해설](run58-registry-strings-해설.md) · [run62 해설 1절](run62-tp-etw-init-해설.md)
- 예약 / 확정 (커밋) = 말뚝 박기 / 바닥 깔기 → [run46 해설 2.7](run46-process-heap-해설.md)
- 사진 K1 ~ P2 (K1 = `PspAllocateThread` 입구, P2 = `NtContinue` 입구) → [run44 해설 2.8](run44-baggage-snap-해설.md)

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**, 커널 = **관리사무소**.
- KUSD = 관리사무소가 모든 가게 앞에 붙여 두는 **공용 게시판**. 그 안 `ProcessorFeatures` = **기본 기능 64칸 체크리스트**.
- 이번에 새로 나오는 것 하나: CPU 의 **자기소개서** (CPUID). 아래 2.2 에서 푼다.

### 한 줄로 먼저 — 무엇을 얻었나

| 물음 | 답 |
|---|---|
| SymCrypt 가 CPU 기능을 찾을 때 무엇을 보나? | **CPU 자기소개서 (CPUID) 만** 본다. 4 ~ 6번, 쪽 번호 0·1·7 `[정적]` (2.3) |
| PE_emulator 는 무엇만 맞추면 되나? | Unicorn 이 CPUID **리프 0·1·7 (서브리프 0)** 에 맞게 답하면 된다 `[정적]` (2.5) |
| 공용 게시판 체크리스트 64칸은 누가 채우나? | **부팅 때** 커널 함수 넷이 **28칸** 만 쓴다. 36칸은 쓰는 줄이 안 나왔다 `[정적]` (2.7 · 2.8) |
| 그 규칙이 진짜 VM 과 맞나? | 맞다. 1 인 24칸이 **모두** 28칸 안이고, 36칸은 **모두 0** `[관찰]` (2.10) |
| `LdrpTouchThreadStack` 은 test.exe 에서 타나? | **안 탄다.** PEB `+0x318` 이 0 이라 건너뛴다 `[정적]` + 사진 `[관찰]` (2.11) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 장면이 **둘** 이다. 하나는 흐름도 범위 **앞** (부팅) 이고, 하나는 ntdll 로더 안이다.

```
(부팅 — 흐름도 범위 앞, test.exe 가 있기 훨씬 전)
 커널이 CPU 에 묻고, 공용 게시판 체크리스트를 채운다                 ← 이번 ②

cmd.exe 의 부탁 → 커널 NtCreateUserProcess → … MmCreatePeb (PEB +0x318 씀) → 첫 스레드 출발
 └─ ntdll LdrInitializeThunk → _LdrpInitialize
      ├─ +0x16c LdrpInitializeProcess
      │    ├─ … (사진 P1b)
      │    └─ … +0x1df4 SymCryptDetectCpuFeaturesByCpuid              ← 이번 ①
      ├─ +0x1a1 LdrpTouchThreadStack   (test.exe 는 안 탄다)          ← 이번 ③
      └─ … NtContinue 은퇴 (사진 P2) → main
```

### 무엇이 궁금했나

PE_emulator 가 세 가지를 물었다 (요청 21).

1. PE_emulator 117 은 `SymCryptDetectCpuFeaturesByCpuid` 를 **공사 중 빈 함수** 로 두었다. 진짜로 돌리려면 무엇이 필요한가?
2. 공용 게시판 체크리스트 (`ProcessorFeatures`) 를 Unicorn CPU 에서 **셈해서** 채우고 싶으면, 규칙이 뭔가?
3. PE_emulator 118 은 `LdrpTouchThreadStack` 본문이 필요하다.

### 바퀴 전에 알던 것

- `[정적]` (run59) `LdrpInitializeProcess` 뒤쪽 `+0x1d75 ~ +0x1e27` 과 `SymCryptDetectCpuFeaturesByCpuid` 가 CPU 기능 전역 (`x86_cpu_enable_*`·`g_SymCrypt*`) 을 쓴다. CPUID 를 직접 부른다.
- `[정적]` (run59 · run61) ntdll 은 게시판 체크리스트를 **읽기만** 한다 (`RtlIsProcessorFeaturePresent`, `RtlRandom` 의 28번 칸).
- `[정적]` (run65) `KiSetFeatureBits +0x888` 이 `KeProcessorFeaturesBuffer` (확장 128칸 쪽) 를 0 으로 채운다. 그런데 **기본 64칸을 누가 쓰나** 는 몰랐다 `[공백]`.
- `[관찰]` (run44) 사진에 게시판 64칸 값, SymCrypt 칠판 값, PEB 값이 다 있었다. 다만 PEB `+0x318` 을 **누가** 쓰는지는 몰랐다.

---

## 2. 찾은 것

### 2.1 한눈에 (대표만)

```
(부팅, BSP — 흐름도 범위 앞)
entry (= KiSystemStartup)
 ├─ +0x1ba  KiInitializeBootStructures
 │    ├─ +0x283  KiSetProcessorSignature                       BSP 만
 │    │    ├─ +0x33  RtlDetectProcessorFeatures              CPUID × 표 59줄 → FeatureBits
 │    │    └─ +0x64  KiPublishProcessorFeatures              → 게시판 2칸 (33 · 35)
 │    ├─ +0x375  KiInitializeNxSupportDiscard                → 게시판 1칸 (12)
 │    └─ +0x387  KiSetFeatureBits                            (게시판 안 씀)
 └─ +0x283  KiInitializeKernel
      │    +0x382  KeFeatureBits ← RBX
      │    +0x5a3 ~ +0x798  게시판 24칸
      └─ +0x803  InitBootProcessor                           → +0x406 게시판 1칸 (23)

(test.exe 첫 스레드)
LdrpInitializeProcess
 │  +0x1de6 TEST g_SymCryptFlags, 1 · +0x1ded JNZ          (프로세스당 한 번)
 ├─ +0x1df4  SymCryptDetectCpuFeaturesByCpuid               CPUID 만, 4 ~ 6번
 │  +0x1df4 OR.LOCK g_SymCryptFlags |= 1 · +0x1e27 PresentCheck = ~NotPresent
_LdrpInitialize
 │  +0x190 RCX = PEB+0x318 · +0x19a JZ                      (test.exe 0 → 안 탐)
 └─ +0x1a1  LdrpTouchThreadStack
      └─ +0x53  NtQueryVirtualMemory (클래스 0)
```

- 숫자 `+0x…` 는 이전처럼 **돌아올 자리 (ret)** 다. 전부는 run 문서 "호출 관계".
- 새 말 둘은 바로 아래에서 푼다: **BSP** (2.7), **CPUID** (2.2).

---

### 2.2 새 말 — CPUID 는 CPU 의 자기소개서, 리프는 쪽 번호

- **`CPUID`** = 프로그램이 CPU 에게 "너 뭐 할 줄 알아?" 하고 묻는 **명령 하나** 다 `[추측]` (일반적으로 알려진 설명).
  - 함수도 아니고 syscall 도 아니다. 그냥 CPU 명령이다. 그래서 커널에 안 가고 **바로** 답이 온다.
  - 비유: CPU 가 들고 다니는 **자기소개서**.
- **리프 (leaf)** = 묻기 전에 `EAX` 에 넣는 **번호**. "자기소개서 몇 쪽을 보여 줘" 다 `[추측]` (일반적으로 알려진 설명).
  - 리프 0 쪽 = 제조사 이름과 "몇 쪽까지 있나" (최대 리프).
  - 리프 1 쪽 = 기본 기능 목록. 리프 7 쪽 = 그 뒤에 생긴 기능 목록.
  - **서브리프** = 같은 쪽 안의 **뒷면 번호** (`ECX`). 이번 바퀴에선 늘 0 이다 `[정적]` (매번 `XOR ECX,ECX` 가 앞에 있다).
- 답은 레지스터 넷 (`EAX`·`EBX`·`ECX`·`EDX`) 에 비트로 온다. 비트 하나 = 기능 하나 "있음 / 없음".

---

### 2.3 SymCrypt 가 자기소개서를 읽는 법 — 4 ~ 6번

- **SymCrypt** = 이름에 붙은 암호 라이브러리로 보인다 `[추측]` (이름으로 본 것). 이번에 본 건 그 안의 "CPU 기능 찾기" 함수 하나다.
- `SymCryptDetectCpuFeaturesByCpuid` (rva `0x11f42c`, 295B) 는 **인자 없는 `void` 잎 함수** 다 `[정적]`.
  - 하위 호출은 `__security_check_cookie` (`+0x10d/+0x112`) **하나뿐**.
  - `SYSCALL` · `XGETBV` 명령이 **없다**. 게시판 (KUSD) 도, `RtlProcessorFeaturesBuffer` 도 **안 읽는다**.

#### CPUID 명령은 5자리, 실행은 4 ~ 6번 `[정적]`

| 자리 | 리프 | 몇 번 | 언제 |
|---|---|---|---|
| `+0x3b` | 0 | 1 | 늘 (최대 리프 알기) |
| `+0x5a` | 1, 7 | **2** | 표 루프 안. 리프가 바뀔 때마다 (1 → 7) |
| `+0x9d` | 0 | 0 ~ 1 | AES-NI 가 "있음" 일 때 (제조사 무관) |
| `+0xbe` | 1 | 0 ~ 1 | 위 + 제조사 글자가 `AuthenticAMD` 일 때 |
| `+0xec` | 1 | 1 | 늘 (결과를 칠판에 적기) |

- 자리는 5개인데 왜 "4 ~ 6번" 인가?
  1. `+0x5a` 는 루프 안이라 **두 번** 탄다 (리프 1 한 번, 리프 7 한 번).
  2. 그래서 늘 타는 것만 세면 `+0x3b` + `+0x5a` ×2 + `+0xec` = **4번**.
  3. AES-NI 가 있으면 `+0x9d` 가 더해져 **5번**, 거기에 AMD 면 `+0xbe` 까지 **6번**.
- 결과서 "한눈에" 는 "AMD 일 때만 0·1" 로 적었는데, `.asm` 으로는 위처럼 **리프 0 (`+0x9d`) 은 제조사 무관, 리프 1 (`+0xbe`) 만 AMD** 다. run 문서가 이렇게 맞췄다.

#### 한 줄 읽기 — `+0xec` 줄

- **누가**: `SymCryptDetectCpuFeaturesByCpuid`.
- **어디서**: `+0xec`, 함수 끝 무렵.
- **누구를**: CPU 자기소개서 **리프 1 쪽** (서브리프 0).
- **무엇을 위해**: 받은 네 레지스터를 칠판 `g_SymCryptCpuid1` (`+0x1d33f0`, 16B) 에 **그대로 베껴 둔다** — `+0x0` EAX (`+0xee`) · `+0x4` EBX (`+0xf4`) · `+0x8` ECX (`+0xfa`) · `+0xc` EDX (`+0x100`) `[정적]`.
  - 이 칸을 읽는 곳은 `_refs` 에 없다 `[정적]`.

---

### 2.4 "못 하는 것" 목록을 만든다 — 시작이 `0xffffc020`

여기가 조금 거꾸로라 천천히 간다.

- 이 함수가 만드는 건 "**할 줄 아는 것**" 목록이 아니라 "**못 하는 것**" 목록이다. 이름도 `…NotPresent` (없음).
- 순서 `[정적]`:
  1. 모으는 레지스터 `R8D` 를 **`0xffffc020`** 로 시작한다 (`+0x30`).
     - 이 값에서 이미 켜져 있는 비트 5 · 14 ~ 31 은 **표에 없는 비트** 라 늘 "없음" 이다.
  2. ntdll 안 읽기 전용 표 **`cpuidBitInfo`** (`ntdll+0x186c10`, `.rdata`, 20줄, 160B) 를 한 줄씩 돈다.
  3. 줄마다 "이 CPUID 비트가 **꺼져** 있나?" 를 본다 (`+0x7d BT` · `+0x80 JC`). 꺼져 있으면 그 줄의 마스크를 `OR` 해서 "없음" 비트를 켠다 (`+0x82`).
  4. 다 돌고 **`+0xe3` 에서 딱 한 번** `g_SymCryptCpuFeaturesNotPresent` (`+0x1cd7b0`, 4B) 에 쓴다.
- 비유: 처음엔 "표에 없는 것은 다 못 함" 에 동그라미를 쳐 둔 종이를 들고, 자기소개서를 보며 **빠진 기능에만** 동그라미를 더한다. 그 종이를 마지막에 칠판에 한 번 붙인다.

표 몇 줄만 (전부는 run 문서 "없음" 비트 표):

| "없음" 비트 | 이 CPUID 비트가 꺼지면 켜짐 | 이름 `[추측: 공개 CPUID 문서]` |
|---|---|---|
| 2 (`0x4`) | 리프 1 ECX[25] (+ AMD 가문 합 < `0x15`) | AES-NI |
| 4 (`0x10`) | 리프 1 ECX[28] · 리프 7 EBX[5] | AVX·AVX2 |
| 9 (`0x200`) | 리프 1 ECX[30] | RDRAND |
| 13 (`0x2000`) | 리프 1 ECX[13] | CMPXCHG16B |

- 함수가 돌아오면 부르는 쪽 `LdrpInitializeProcess` 가 두 칸을 더 쓴다 `[정적]`:
  - `+0x1df4 OR.LOCK` 으로 `g_SymCryptFlags` 에 1 을 켠다 ("이미 했음" 깃발).
  - `+0x1e27` 에서 `g_SymCryptCpuFeaturesPresentCheck` = `NotPresent` 의 **NOT** (뒤집은 값) — 즉 "할 줄 아는 것" 목록.

#### 부르는 조건 — 프로세스당 한 번 `[정적]`

- `+0x1de6 TEST [g_SymCryptFlags],DIL` · `+0x1ded JNZ +0x1e2d` — 깃발 비트 0 이 이미 켜져 있으면 건너뛴다.
- 깃발은 `.data` 0 으로 시작하고, 쓰는 줄은 `+0x1df4` 하나, 읽는 줄은 `+0x1de6` 하나다. 그래서 **프로세스당 한 번** 탄다.

---

### 2.5 PE_emulator 한 줄 — 리프 0·1·7 만 맞게 답하면 된다

- `SymCryptDetectCpuFeaturesByCpuid` 를 **진짜로** 돌려도, Unicorn 이 CPUID **리프 0·1·7 (서브리프 0)** 에 맞게 답하기만 하면 된다 `[정적]`.
  - 메모리 입력은 ntdll 안 표 `cpuidBitInfo` 하나 (+ 늘 있는 `__security_cookie`). 이건 ntdll 파일에 이미 들어 있다.
  - 쓰는 곳은 `.data` 20바이트뿐 (`+0x1cd7b0` 4B · `+0x1d33f0` 16B).
- 덫으로 둘 거면 이 두 칠판만 2.4 표대로 채우면 된다.
- 하나 더: 같은 구간에서 `LdrpInitializeProcess` **자신도** CPUID 를 둘 더 부른다 — `+0x1d45` 리프 1, `+0x1dbb` 리프 7 (조건부). 이것도 Unicorn 몫이다 `[정적]`.
  - 그리고 `+0x1d99 RtlIsProcessorFeaturePresent(0x29)` 는 게시판 `+0x29d` (번호 41) 한 바이트를 읽는다. 이건 2.7 부터 볼 **게시판 체크리스트** 쪽이다.

---

### 2.6 정정 하나 — `g_SymCryptFlags` 는 비트 **0**

- 짐 지도 `baggage.md` 3.8 에는 "**비트 1** 켬" 으로 적혀 있었다 (run59 때 sast/54 결과서 표를 옮김).
- 맞는 건 **비트 0 (값 1)** 이다 `[정적]`.
  - 근거: `OR.LOCK [..],EDI` 의 `EDI` 는 `+0x1d3c MOV EDI,1` 이다. `+0x1de6 TEST [..],DIL` 도 같은 1 을 본다.
  - `[관찰]` 사진 P2 값도 1 이다.
- 같은 줄의 `g_SymCryptCpuid1` 도 "EAX · EBX" 둘 → **16B 넷** (ECX `+0x8` · EDX `+0xc` 보탬) 으로 고쳤다.
- run59 문서 자체엔 비트 번호가 없어서 그 문서엔 정정 줄을 안 넣었다. 결과서 sast/54 는 고치지 않는다.

---

### 2.7 공용 게시판 체크리스트는 부팅 때 채워진다

이제 둘째 장면. **test.exe 가 생기기 훨씬 전** 이야기다.

- 새 말 — **BSP** = 부팅 때 가장 먼저 일어나는 **프로세서 번호 0** 이다 (결과서). 다른 프로세서는 **AP** 라고 부른다.
  - 비유: 아침에 관리사무소 문을 여는 **첫 출근자**. 게시판 붙이기는 이 사람이 한다.
- 게시판 `ProcessorFeatures` (`+0x274`, 64B) 에 실제로 쓰는 커널 함수는 **넷** 이다 `[정적]`:

| 함수 | 쓰는 칸 수 | 칸 번호 |
|---|---|---|
| `KiInitializeKernel` (`+0x5a3 ~ +0x798`) | 24 | 2·3·6·8·9·10·14 (조건 없이 7) + FB1 조건 17 |
| `KiPublishProcessorFeatures` | 2 | 33 (`+0x295`) · 35 (`+0x297`) |
| `KiInitializeNxSupportDiscard` | 1 | 12 (`+0x280`) |
| `InitBootProcessor` | 1 | 23 (`+0x28b`) |
| 합 | **28** | |

- 요청서는 `KiSetFeatureBits` 가 쓰는 줄 알았는데 **아니다** `[정적]`. 그 함수는 PRCB `+0x8fa0` 와 `KeProcessorFeaturesBuffer` (확장 쪽, run65) 몫이다.
- 커널은 게시판을 쓸 때 포인터 `MmWriteableSharedUserData` (nt `0xfc5008`) 를 들고 쓴다 `[정적]`.
  - 비유: 가게들 앞에선 게시판이 **유리 너머** (읽기 전용) 지만, 관리사무소는 **뒷문 열쇠** 가 따로 있다.

#### 한 줄 읽기 — `KiInitializeKernel +0x60c` (13번 칸)

- **누가**: `KiInitializeKernel` (부팅, 첫 출근자).
- **어디서**: `+0x60c`.
- **누구를**: 공용 게시판 체크리스트 **13번 칸** (`0x7ffe0274 + 13`).
- **무엇을 위해**: `KeFeatureBits` 의 **비트 19** 가 켜져 있으면 1 을 적는다. 그 비트는 원래 CPUID **리프 1 ECX[0]** 에서 왔다 `[정적]`. 이름은 SSE3 로 보인다 `[추측: winnt.h 기억]`.

---

### 2.8 규칙 — 자기소개서 → 관리사무소 장부 → 게시판 칸 번호

게시판에 바로 옮겨 적는 게 아니라 **한 번 거친다**.

1. **자기소개서 읽기** — `RtlDetectProcessorFeatures` 가 표 `KiCpuFeatureTable` (nt rva `0x8800`, 59줄) 대로 CPUID 를 읽어 PRCB `+0x8fa0` 의 FeatureBits · FeatureBits2 에 비트를 모은다 `[정적]`.
2. **장부에 옮기기** — 첫 출근자는 `KiInitializeKernel +0x382` 에서 그걸 전역 **`KeFeatureBits`** (nt rva `0xfc6b70`) 에 옮긴다. 둘째 묶음은 **`KeFeatureBits2`** (`0xfc6ba0`) `[정적]`.
   - 새 말 — **`KeFeatureBits`** = 관리사무소 안 **장부** 에 적힌 "이 CPU 기능 목록" 비트 묶음. 게시판보다 칸이 많고, 가게에선 안 보인다.
   - run 문서는 줄여서 "FB1" (`KeFeatureBits`) · "FB2" (`KeFeatureBits2`) 라고 쓴다.
3. **게시판에 붙이기** — 장부 비트가 켜졌으면 게시판 칸에 1 (2.7 의 함수 넷).

규칙 표 몇 줄만 (전부는 run 문서 "KUSD 규칙 표"):

| 칸 | 조건 | 장부 비트의 출처 | `PF_*` `[추측]` |
|---|---|---|---|
| 2 · 3 · 6 · 8 · 9 · 10 · 14 | 없음 (늘 1) | — | CMPXCHG8B · MMX · SSE · RDTSC · PAE · SSE2 · CMPXCHG16B |
| 12 | `KiIsNXSupported` 참 | EFER 비트 11 을 켬 | NX_ENABLED |
| 13 | FB1 19 | 리프 1 ECX[0] | SSE3 |
| 28 | FB1 32 | 리프 1 ECX[30] | RDRAND |
| 39 | FB1 50 **그리고** 게시판 `+0x3d8` 비트 2 | 리프 1 ECX[28] | AVX |
| 33 | FB2 1 | 리프 7 ECX[22] | RDPID |

- 대부분은 "CPUID 비트 하나 → 장부 비트 하나 → 칸 하나" 로 곧게 간다.
- 곧지 않은 것들 `[정적]`:
  - **AVX 셋 (39·40·41)** 은 CPUID 만으로 안 된다. 게시판의 XState 칸 `+0x3d8` 비트도 같이 본다.
    - 그 칸은 `entry +0x1e8 KiInitializeXSaveConfiguration` 이 쓴다고 보이지만 본문은 안 읽었다 `[공백]`.
  - 칸 **17 · 20 · 21** 의 장부 비트 (23 · 26 · 27) 는 CPUID 표에 없다. 17 은 `+0x3d8` 가 0 이 아니면 (`+0x2e5`), 20 · 21 은 `KiSetFeatureBits` 의 가상화 검사 (Intel 은 MSR, AMD 는 MSR 또는 리프 `0x8000000a`) 에서 켜진다.
  - 칸 **36 · 60** 의 장부 비트엔 **콜백** (`RtlHandleSSSE3Override` · `RtlHandleBMI2Override`) 이 붙어 있다. 콜백 본문은 안 읽었다.
- 필수 기능 줄 5개 (CMPXCHG16B · SSE4.2 · LAHF · SYSCALL · NX(Intel)) 가 없으면 `KeBugCheckEx(0x5d, 0xffffffff, …)` 를 부른다 `[정적]`.
- AP (다른 프로세서) 도 `KiInitializeKernel` 의 게시판 쓰기 줄을 **다시 지나간다**. 그 앞 `+0x42c ~ +0x54a` 에서 BSP 의 `KeFeatureBits` 와 다르면 빠진다 (bugcheck 쪽 `[추측]`).

---

### 2.9 28칸과 36칸 — "안 나왔다" 의 한계

- 64칸 중 쓰는 줄이 있는 칸 = **28**. 나머지 **36칸** (0 · 1 · 4 · 5 · 11 · 15 · 16 · 18 · 19 · 24 ~ 27 · 29 ~ 31 · 34 · 43 ~ 59 · 62 · 63) 은 쓰는 줄이 **안 나왔다** `[정적]`.
- 그런데 "안 나왔다" 는 **두 모양으로만** 훑은 결과다 `[정적]`:
  1. `MmWriteableSharedUserData` 포인터 + 거리
  2. 절대 주소 `0xFFFFF780000002xx`
- 다른 레지스터에 포인터를 오래 들고 다니며 쓰는 줄은 놓칠 수 있다.
- 비유: 게시판 앞 CCTV 를 **두 각도로만** 돌려 봤다. 그 두 각도에선 36칸에 손대는 사람이 안 보였다. 사각지대는 남는다.
- 다른 모양으로 한 번 더 훑기는 Q135 (g) 로 넘겼다 (4절).

---

### 2.10 사진과 맞춰 보기 `[관찰]` — 어긋나는 칸 0

run44 사진 8장을 flow-writer 가 다시 읽었다 (커널 쪽 주소 `fffff780'00000270 ~ 2b0`, 8장 모두 같은 값).

#### 게시판 64칸

- 1 인 칸 **24**: 2·3·6·8·9·10·12·13·14·17·21·22·23·28·32·33·36·37·38·39·40·41·42·60.
- 단계별로 맞춰 보면:
  1. 24칸이 **모두** 정적 28칸 안에 있다.
  2. 28칸 중 0 인 넷 = 7 (3DNOW) · 20 (SLAT) · 35 (MONITORX) · 61 (MOVDIR64B). 넷 다 **조건부** 칸이라 0 이어도 이상하지 않다.
  3. "쓰는 줄 없음" 36칸은 **모두 0**.
  4. 값이 K1 (`PspAllocateThread` 입구) 보다 앞부터 끝까지 그대로다 → **부팅 때 놓인 짐** 이라는 정적 결론과 맞다.
- 조심할 것: 0 이라는 건 "아무도 안 씀" 의 증거는 **아니다**. 누가 0 을 썼을 수도 있다.

#### XState 칸과 자기소개서 사본

- 게시판 `+0x3d8` = **`0xe7`** (비트 0·1·2·5·6·7) → AVX (39) 의 비트 2, AVX512F (41) 의 비트 5 ~ 7 조건이 맞고, 사진 39·41 = 1 과 맞다. 17 (`+0x3d8 ≠ 0`) = 1 도 맞다.
- SymCrypt 칠판 `g_SymCryptCpuid1` P2 = EAX `0x00a60f12` · EBX `0x02000800` · ECX `0xfef83203` · EDX `0x078bfbff`.
  - 리프 1 ECX 비트 0·9·19·20·28·30 이 켜졌다 → 게시판 13·36·37·38·39·28 = 1 과 맞다 (같은 VM, 같은 CPU).
  - EAX 의 가문 = `0x19` → AMD 계열로 보인다 `[추측]` (Intel 은 가문 6). 제조사 글자는 사진에 없다.
- `g_SymCryptCpuFeaturesNotPresent`: P1a·P1b `0xffffffff` → **P2 `0xffffc020`**.
  - P2 값이 **시작값 그대로** 다 → 표 20줄이 모두 "있음" 이었고, AMD 보정 (가문 `0x19` ≥ `0x15`) 도 안 탔다. 정적 규칙과 맞다.
- `g_SymCryptFlags` P2 = 1, `PresentCheck` P2 = `0x3fdf` = `~0xffffc020`.

비유로 한 번 더: 관리사무소가 아침에 붙인 체크리스트 (게시판) 와, 관리실이 자기소개서를 직접 보고 적은 메모 (칠판) 가 **같은 CPU 를 같은 말로** 설명하고 있다.

---

### 2.11 `LdrpTouchThreadStack` — 스택 쪽마다 한 번씩 미리 깨우기, test.exe 는 안 탄다

셋째 장면.

#### 무엇을 하는 함수인가 `[정적]`

- `LdrpTouchThreadStack` (rva `0x8bdc4`, 180B). 인자 `RCX` = "최소한 이만큼은 확정해 둬라" 바이트 수.
- 순서:
  1. TEB 에서 스택 위·아래 끝 (`StackBase` · `StackLimit`) 을 본다.
  2. `+0x4e/+0x53 NtQueryVirtualMemory(-1, StackLimit, 0, &MBI, 0x30, &길이)` — syscall **1번**. 스택 구역의 시작 (`AllocationBase`) 을 안다.
  3. 아래 한계 = `max(StackBase − 0x1000 − 최소커밋, AllocationBase + 0x3000)`.
  4. 맨 위 한 쪽 아래부터 **4KiB (한 쪽) 마다 4바이트 읽기만** 하며 내려간다 (`+0x7f MOV EAX,[RAX]`). **쓰기는 없다.**
- 왜 읽기만 하나? 읽어서 가드 페이지 (스택 맨 아래 보호 쪽) 를 건드리면 **커널이 확정 범위를 늘려 주는** 방식으로 보인다 `[추측: 커널 쪽은 안 읽음]`.
  - 비유: 말뚝만 박힌 땅 ([예약](run46-process-heap-해설.md)) 을 위에서부터 **한 칸씩 밟아 보며** 내려간다. 밟을 때마다 관리사무소가 와서 바닥을 깔아 준다. 미리 깨워 두는 셈이다.
- 밟다가 예외가 나면 (`+0x7a ~ +0x95` 범위) `LdrpGenericExceptionFilter` 를 거쳐 `+0x95` 에서 `0xC0000017` 을 돌려준다. 그러면 `_LdrpInitialize +0x227 JS` → `LdrpInitializationFailure` → `NtTerminateProcess`. 가게 문을 못 연다 `[정적]`.

#### 왜 test.exe 는 안 타나

- 부르는 곳 `_LdrpInitialize` `[정적]`:
  - `+0x190 MOV RCX,[RSI+0x318]` — PEB `+0x318 MinimumStackCommit` 을 읽는다.
  - `+0x197 TEST` · `+0x19a JZ +0x1e3` — **0 이면 건너뛴다.**
- test.exe 의 PEB `+0x318` = **0**, 사진 8장 모두 `[관찰]`. 마지막 P2 (`NtContinue` 입구) 는 이 읽는 줄 `+0x190` 보다 뒤라, 읽을 때도 0 이었다.

#### 그 칸은 누가 쓰나 `[정적]` — 이번에 처음 알았다

| 쓰는 곳 | 값 | test.exe |
|---|---|---|
| 커널 `MmCreatePeb +0x4cc` (늘) | `MmRegistryState+0x28` (nt rva `0xfc5218`) = 레지스트리 `Session Manager\Memory Management` 의 `MinimumStackCommitInBytes`, 기본값 없음 | 사진 K1 에서 이미 0 → 0 을 썼다 `[관찰 간접]` |
| ntdll `LdrpInitializeExecutionOptions +0x413` (`LdrpInitializeProcess +0x531/+0x536` 안) | IFEO 값이 지금 PEB 값보다 **클 때만** | 매개변수 `Flags 0x4001` (비트 14 = IFEO 키 없음) 이라 이 줄에 안 닿는다 |

- 비유: "스택을 미리 깔아 둘까?" 스위치가 PEB 에 있다. 관리사무소 레지스트리 설정이 비어 있고, test.exe 전용 설정 (IFEO) 도 없어서 스위치가 **꺼진 채** 로 온다.
- exe 헤더의 `SizeOfStackCommit` 이 이 칸을 채우는 줄은 **안 나왔다** `[정적]`.

---

### 2.12 결과서와 다르게 적은 곳 (짧게)

- 결과서 "한눈에" 의 `KiInitializeKernel` 게시판 쓰기 범위 `+0x58a ~ +0x77a` → run 문서는 쓰기 줄 기준 **`+0x5a3 ~ +0x798`** 로 적었다. `+0x58a` 는 `KiEnableXSave` 의 돌아올 자리이고, 마지막 쓰기는 61번 칸 `+0x798` 이다.
- 결과서 "AMD 일 때만 0·1" → 2.3 처럼 나눠 적었다.
- 결과서가 "동적으로 확인할 것" 으로 둔 게시판 64칸 · `+0x3d8` · SymCrypt 칠판 넷은 **run44 사진에 이미 있었다** (2.10). 남은 동적 몫은 장부 (`KeFeatureBits` · `KeFeatureBits2`) 값과 콜백 효과뿐이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 절 | 전 | 후 |
|---|---|---|
| 2.1 `MmCreatePeb` 표 | `+0x318` 줄 없음 | `+0x318` ← `MmRegistryState+0x28` `[정적]`, 사진 8장 0 `[관찰]` |
| 2.3 부팅 메모 | 게시판 64칸을 누가 쓰나 없음 | 새 절 "KUSD `ProcessorFeatures` 를 채우는 곳" — 사슬 그림, 28칸 / 36칸, 규칙 3단 `[정적]` + 사진 `[관찰]` |
| 2.4 `_LdrpInitialize` | `LdrpTouchThreadStack` 갈래 줄 없음 | `+0x190 ~ +0x1a5` 갈래 줄, test.exe 안 탐 `[정적]` + `[관찰]` |
| 2.4 `LdrpInitializeProcess` 뒤10 | SymCrypt 가 칠판을 쓴다는 것만 (run59) | 부르는 조건 (깃발 비트 0), 쓰는 칠판 둘 `[정적]` |
| 2.4 새 절 "CPU 기능 감지" | 없음 | SymCrypt CPUID 4 ~ 6번, `LdrpTouchThreadStack` 본문, PEB `+0x318` 쓰는 곳 둘 |
| 4장 · 5장 | — | 해결된 줄 하나, 새 동적 · 새 정적 · "결과서와 다르게" 줄, run70 줄 |

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- 공용 게시판 (KUSD) 에 **처음으로 "쓴 자리를 아는 칸"** 이 생겼다 — `ProcessorFeatures`. 단계는 새 표시 **`Ⓑ`** (부팅, 프로세스 생성 전).
- PEB `MinimumStackCommit` 이 모르던 칸에서 아는 칸으로 (3.1).
- ntdll 칠판 `g_SymCryptCpuFeaturesNotPresent` 가 아는 칸으로 (3.8). 같은 절의 비트 번호 정정 (2.6).
- 장부 쪽 커널 전역 (`KeFeatureBits` 등) 은 새 절 7.6.10 에 모았다.

### 큰 그림에서

- PE_emulator 는 이제 SymCrypt 함수를 **빈 함수 대신 진짜로** 돌릴 수 있다. Unicorn CPUID 대답만 맞으면 된다.
- 게시판 체크리스트를 CPU 에서 셈하는 길도 열렸다. 다만 두 군데가 아직 반쯤이다:
  - AVX 셋은 XState 칸 `+0x3d8` 도 맞춰야 한다 (쓰는 곳 본문 안 읽음).
  - 36 · 60 칸은 콜백이 장부 비트를 지울 수 있다 (본문 안 읽음).
- `LdrpTouchThreadStack` 은 test.exe 길에선 **안 지나가는 가지** 로 확정. PE_emulator 는 PEB `+0x318` = 0 만 맞추면 `+0x19a JZ` 에서 이 가지를 건너뛴다 `[정적]`.

---

## 4. 아직 모르는 것

### 정적으로 남은 것 → **Q135**

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| (a) 콜백 `RtlHandleSSSE3Override` · `RtlHandleBMI2Override` 가 장부 비트를 지우는 조건 (게시판 36 · 60 을 CPUID 만으로 셈할 수 있나) | 간접 호출이라 본문을 안 열었다 | Q135, 정적 |
| (b) `KiInitializeXSaveConfiguration` 이 게시판 `+0x3d8` 에 쓰는 줄 · 값 (AVX 셋의 둘째 조건) | 이번 범위 밖, 본문 안 읽음 `[공백]` | Q135, 정적 |
| (c) `InitBootProcessor +0x16b` `R15D` = 1 이 `+0x406` 까지 그대로인지 (23번 칸) | 갈래 둘을 안 따라갔다 `[추측]` | Q135, 정적 |
| (d) `KiInitializeKernel +0x42c ~ +0x54a` AP 비교가 빠지는 도착 줄 | 도착 줄을 안 읽었다 | Q135, 정적 |
| (e) `RtlDetectProcessorFeatures +0x541` 간접 호출 (MSR 표 쪽인지) | 간접 호출, 대상은 `[추측]` | Q135, 정적 |
| (f) `KiRestoreFeatureBits` — 절전 복귀 때 게시판을 다시 쓰나 | 이번 범위 밖 `[추측]` | Q135, 정적 |
| (g) 36칸 "쓰는 줄 없음" 을 다른 모양으로 다시 훑기 | 두 모양으로만 훑었다 (2.9) | Q135, 정적 |
| (h) `MmRegistryState` 다른 18칸과 `CmpGetSystemControlValues` 를 부르는 부팅 자리 | 이번엔 `+0x28` 하나만 필요했다 | Q135, 정적 |

### 실행해 봐야 아는 것 → **Q104 VM 단계**

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| 장부 값 `KeFeatureBits` · `KeFeatureBits2`, `MmRegistryState+0x28` (기대 0) | 커널 전역 값은 run44 사진에 없다 | Q104, 동적 (부팅 뒤 한 번 읽기) |
| `LdrpTouchThreadStack` 이 정말 안 불리는지 — `_LdrpInitialize+0x197` 에서 `rcx` = 0, 입구가 안 서는지 | 사진은 칸 값이 0 인 것만 보여 준다. "안 불림" 은 멈춤점으로 본다 | Q104, 동적 |
| (선택) SymCrypt 칠판을 `LdrpInitializeProcess+0x1e2d` 한 자리에서, VM CPU 제조사 (`!cpuinfo`) | 사진 P2 로 이미 맞춤, 확인만 남음 | Q104, 동적 |

---

## 5. 더 읽기

- run 문서: [`run70-cpu-features.md`](run70-cpu-features.md) — "PE_emulator 쪽에서 쓸 것" (CPUID 표, "없음" 비트 표 20줄, 게시판 규칙 표 전부), "사진과 맞춰 본 것", "정정"
- 결과서: [`_workspace/sast/65_cpu-features_result.md`](../../_workspace/sast/65_cpu-features_result.md)
- 요청서: [`_workspace/sast/65_cpu-features.md`](../../_workspace/sast/65_cpu-features.md)
- flow.md: [`flow.md`](flow.md) 2.3 "KUSD `ProcessorFeatures` 를 채우는 곳" · 2.4 "CPU 기능 감지" · 2.1 `MmCreatePeb` 표
- 짐 지도: [`baggage.md`](baggage.md) 3.1 · 3.7 · 3.8 · 7.6.10 · 7.29
- 앞 해설: [run59](run59-init-leftovers-해설.md) 2.9 (CPU 기능 칠판 처음) · [run65](run65-ldr-init-bodies-해설.md) 2.8 (확장 128칸, 클래스 `0xfa`) · [run44](run44-baggage-snap-해설.md) 2.8 (사진 8장)

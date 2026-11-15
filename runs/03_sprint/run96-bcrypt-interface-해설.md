# run96 bcrypt-interface — 해설

> 이 글은 [run96 문서](run96-bcrypt-interface.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q178a** — (Q178 ①) 공급자 DLL 을 올린 뒤: `_LoadInterface` · `ValidateFunctionTable` · `_LoadImage` · `BCryptOpenAlgorithmProvider +0x851/+0x883` 간접 호출. `dwInterface` ↔ `Get…Interface` 이름, 함수 표 검사, run94 최소 조건 표 5·6 행, bcryptprimitives 내보내기 이름 (`_workspace/plan.md`, 이번에 **끝**)
  - **한 바퀴로 끝났다.** 사용자 결정 (2026-11-15) "Q176 ② · Q178 ① 먼저" — Q178 을 Q178a (① 지금) 와 Q178b (나머지) 로 나눴다. 정적만.
  - 나란히 돈 것: Q176a (run95 timer2-set).
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → **Q179 ① ~ ⑤** (run94 가 만든 Q179 에 더함)
    - 정적 → 새 **Q183 ① ~ ③**, 범위가 줄어든 **Q178b**, 그대로 남은 **Q173b**
- 근거:
  - run 문서 [`run96-bcrypt-interface.md`](run96-bcrypt-interface.md) (가장 중요)
  - 요청서 [`_workspace/sast/92_bcrypt-interface.md`](../../_workspace/sast/92_bcrypt-interface.md) · 결과서 [`92_bcrypt-interface_result.md`](../../_workspace/sast/92_bcrypt-interface_result.md)
    - 판정 **분석성공**. 요청서 질문 1 ~ 6 모두 답했다.
    - 못 닫은 것 셋: 진짜 `Get…Interface` 가 `pszProvider` 를 어떻게 쓰나 · 알고리즘 → 번호 전체 표 (커널 몫) · CFG 칸의 실행 때 값 (동적).
    - bcrypt 는 다시 analyze 하지 않았다 (`export` 2번). bcryptprimitives 는 **PE 내보내기 표만** 읽었다 (본문은 안 읽음).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · 흐름도 앞 글 · **3장 bcrypt 절에 새 하위 절** · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): **안 고침** (3절)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행도, 사진 다시 읽기도 없다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

**CFG** (바로 아래) · **함수 표** (2.0) · **`Get…Interface`** (2.0) · **표 머리** (2.5)

- **CFG** (Control Flow Guard) = 간접 호출의 대상이 **허락된 주소인지 확인하는 문지기** 로 알려져 있다 `[추측]` (일반적으로 알려진 설명, [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다) 와 같은 말).
  - 앞 해설들은 이 문지기를 전화 **교환원** 으로 그렸다: 번호가 허락된 곳인지 보고 이어 준다. 이 글도 **교환원** 으로 잇는다.
  - "허락된 곳 목록" 은 교환원의 **출입 명단** (CFG 비트맵) 이다 → [run63 해설](run63-cfg-func-table-해설.md) 2.8.
  - 이번 글에서 중요한 건 2.10 에서 다룬다: bcrypt 의 교환원이 **진짜로 확인을 하나, 그냥 연결만 하나**.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 bcrypt 를 안 올린다** (run92 · run94 와 같다 — run44 사진 모듈 6개 `[관찰]`, sample 에 암호 API 없음 `[소스]`).
   - 그래서 이번 이야기도 **"bcrypt 를 올리고 암호 API 를 부르는 다른 프로세스라면"** 이다. 흐름도 **그림은 그대로** 다.
2. **이름이 두 개 나온다. 섞으면 run94 처럼 헷갈린다** (2.9).
   - **호출자 `pszAlgId`** = 프로그램이 `BCryptOpenAlgorithmProvider` 에 넘긴 이름 (예: `L"AES"`).
   - **KsecDD 답의 `pszFunction`** = 상담실 답장 소개 카드에 적힌 알고리즘 이름 (run94 의 답장 `0x20` → `0x60`).
   - 둘이 가는 곳이 다르다. 이번 바퀴에서 **run94 의 한 줄이 틀렸다** 는 것도 이 차이 때문이다.
3. **오프셋이 세 종류 나온다.**
   - `_LoadInterface +0x130` 처럼 **함수 이름 뒤 `+0x`** = 그 함수 **코드 안 자리**.
   - "표 `+8`" = 공급자 DLL 이 준 **함수 표 안의 칸 자리**.
   - "핸들 `+0x30`" = bcrypt 가 손님에게 돌려줄 **알고리즘 핸들 안의 칸 자리**. 표를 핸들 `+0x30` 에 베끼니까, "핸들 `+0x58`" = "표 `+0x28`" 처럼 **둘이 겹쳐 나오는 곳** 이 있다 (2.6).
4. **표 칸 이름 (`OpenAlgorithmProvider` · `GetProperty` 등) 은 `[추측]` 이다.**
   - PDB 에 함수 표 · 핸들 구조체 정의가 **없다** (run 문서 환경).
   - 칸의 **오프셋 · 크기 · 검사 · 부르는 인자는 `[정적]`** (코드), **이름은 공개 헤더 기억 `[추측]`**.
5. **비유가 둘이다.**
   - flow.md 3장 새 하위 절은 **"담당 부서 · 연락처 명단 · 수첩 · 판 번호"** 비유다.
   - 이 글은 run94 해설을 이어서 **접수처 · 상담실 답장 · 부품 건물 · 호실 이름표 · 손잡이** 로 부른다. 함수 표만은 flow.md 처럼 **연락처 명단** 으로 부른다. 그림만 다르고 사실은 같다.

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 프로세스 = **가게**, 커널 = **관리사무소**, DLL = 가게에 들이는 **건물**, 핸들 = **손잡이** → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 드라이버 = **전문 상담실**, IOCTL = **번호 붙은 주문서** → [run66 해설 2.7](run66-apphelp-cache-해설.md)
- bcrypt = **암호 앞쪽 건물** (손님을 받는 **접수처**), bcryptprimitives = **암호 부품 건물** → [run92 해설](run92-bcrypt-ksecdd-해설.md) · [run91 해설](run91-bcrypt-init-해설.md)
- **공급자** (실제로 암호 일을 하는 DLL), **인터페이스 번호 `dwInterface`** (주문서의 "일 종류" 칸), 상담실 **답장** 과 그 안의 **소개 카드** (공급자 칸) · **건물 안내** (이미지 칸) → [run94 해설](run94-bcrypt-resolve-해설.md) 2.0 · 2.3 · 2.8 · 2.9
- 내보내기 표 = 건물 입구의 **호실 이름표** → [run16 해설 2.9](../02_sprint/run16-thunk-context-해설.md) · [run20 해설 2.5](../02_sprint/run20-kernel32-load-해설.md)
- `GetProcAddress` = 실행 중에 "이 API 있나?" 하고 **이름으로 주소를 찾기** → [run30 해설 2.8](../02_sprint/run30-exe-crt-start-해설.md) "늦게 묶는 API"
- **점프 표** = 번호로 갈 곳을 찾는 표 (안내 데스크의 표) → [run91 해설 2.9](run91-bcrypt-init-해설.md) "새 말 — 점프 표"
- `__fastfail` (프로세스를 그 자리에서 끝냄) → [run18 해설 2.10](../02_sprint/run18-proc-init-setup-해설.md#210-일찍-끝나는-길--19--5)
- 크리티컬 섹션 = 한 번에 한 명만 들어가는 방의 **열쇠** → [run68 해설 2.7](run68-dll-init-rest-해설.md#27-락--새-말-크리티컬-섹션)

### 한 줄로 먼저 — 무엇을 얻었나

**이번 글의 중심은 이 한 줄이다.**

> **번호 ↔ 이름은 bcrypt 안에 박혀 있고 (1 `GetCipherInterface` … 8 `GetKeyEncapsulationInterface`), 진짜 `bcryptprimitives.dll` 은 8개를 다 내보낸다. 그 뒤의 표 · 검사 · 열기는 전부 진짜 DLL 코드가 정한다 — 에뮬레이터가 값을 만들어 줄 칸은 없다. 막힐 곳 후보는 `GetProcAddress` · CFG 간접 호출 · DLL 안 코드다.** `[정적]`

그리고 run94 의 한 줄을 고쳤다 `[정적]`:

> KsecDD 답의 `pszProvider` · `pszFunction` 은 `Get…Interface` 의 **첫째 · 둘째 인자** 로 간다 (로그용 아님). 핸들에 복사되는 이름은 **호출자 `pszAlgId`** 다 (2.9).

| 요청서 질문 | 한 줄 답 | 이 글 |
|---|---|---|
| 1 번호 ↔ 이름 | `1..8` 전부 정해져 있다 + bcryptprimitives 가 8개 다 내보냄 | 2.3 |
| 2 `Get…Interface` 부르는 법 | 인자 4개 (번호 6 은 3개), 음수 그대로 위로, 표는 핸들 `+0x30` 으로 복사 | 2.4 · 2.6 |
| 3 `ValidateFunctionTable` | 머리는 `_LoadInterface` 가, NULL 칸은 이 함수가. 자리는 `BCryptOpenAlgorithmProvider +0x70b` | 2.5 · 2.7 |
| 4 `+0x851` / `+0x883` | 표 `+8` (열기), 재시도 조건 셋, CFG 경유 | 2.8 · 2.10 |
| 5 `_LoadImage` · 이미 올라간 DLL | `LoadLibraryExW(경로, 0, 0)`, 목록에 있으면 안 부름 | 2.11 |
| 6 최소 조건 표 | run94 4-a 5 · 6 행 채움 | 2.13 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**run94 의 바로 다음 걸음** 이다. 여전히 test.exe 에는 없는 **옆길** 이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ (2.6) import DLL 들이기
 │         └─ (2.7) DLL 전원 켜기
 │               └─ (bcrypt 를 올리는 프로세스라면) bcrypt!DllMain        ← run92
 └─ … → NtContinue (은퇴) → exe 진입점 → main
                                        │
                                        └─ (그 프로그램이 암호 API 를 부르면)
                                             BCryptOpenAlgorithmProvider
                                               ├─ BCryptResolveProviders → IOCTL 0x390400   ← run94: "어느 건물이 맡나요?"
                                               ├─ LoadProviderEx → _LoadImage → LoadLibraryExW  ← run94: 그 건물을 들임
                                               │     (bcryptprimitives!DllMain                  ← run91)
                                               └─ _LoadInterface → GetProcAddress(Get…Interface)
                                                  → 표 복사 → ValidateFunctionTable → 표 +8 열기  ← 이번 (run96)
```

- 위치를 한 번 더: 이 일은 **프로그램이 암호 API 를 부를 때** 일어난다. 초기화 (은퇴 전) 에 꼭 일어나는 일은 아니다.
- flow.md 3장 "bcrypt 시작 함수" 절 아래, run94 하위 절 바로 뒤에 새 하위 절로 놓였다.

### 왜 이 질문이 지금 왔나

- run94 로 **`BCryptResolveProviders` 를 통과하는 가장 작은 KsecDD 답장** (`0x98` 바이트) 이 나왔다. 에뮬레이터가 그 답장을 주면 bcrypt 는 공급자 DLL 을 올린다.
- 그다음에 bcrypt 가 무엇을 찾고 무엇을 검사하나 — 그게 **에뮬레이터의 다음 벽 후보** 다 (요청서 배경).
- run94 최소 조건 표의 **5 · 6 행이 비어 있었다** (`GetProcAddress` · `ValidateFunctionTable`). 이번에 채운다.

### 무엇이 궁금했나

요청서 질문 여섯 (run 문서 "원문 질문이 어디서 답해졌나").

1. **번호 ↔ 이름** — `_LoadInterface` 가 번호 `1..8` 마다 어떤 내보내기 이름을 고르나.
2. **`Get…Interface` 를 어떻게 부르나** — 인자 순서, 반환을 어떻게 보나, 받은 표를 어디에 두나.
3. **`ValidateFunctionTable` 이 무엇을 검사하나** — 머리, NULL 이면 안 되는 칸, 실패 status, 그리고 이 함수가 어디서 불리나.
4. **`+0x851` / `+0x883` 간접 호출** — 표의 몇 번째 칸을 어떤 인자로 부르나, CFG 를 지나나.
5. **`_LoadImage` 와 이미 올라간 DLL** — 같은 DLL 을 두 번 올리나.
6. **최소 조건 표** — run94 4-a 의 5 · 6 행.

### 바퀴 전에 알던 것

- `[정적]` (run94) `BCryptOpenAlgorithmProvider +0x518 LoadProviderEx` → `_LoadImage +0x9d LoadLibraryExW(시스템 폴더 + "\" + 이름)`. 그 뒤의 `_LoadInterface` · `+0x70b ValidateFunctionTable` · `+0x851` / `+0x883` 간접 호출은 **이름과 자리만** 알았다.
- `[정적]` (run94) `dwInterface` 는 `1..8` 이어야 한다 (`+0x3b7`). 6 = 난수 · 2 = hash · 3 = 비대칭 암호화는 코드로 봤다. **1 = 암호는 `[추측]`**.
- `[공백]` (run94) 공급자 DLL 이 내보내야 할 `Get…Interface` 이름과 번호의 짝.
- (sast/90 결과서) 답장의 `pszProvider` 는 "로그에만 씀" — **이번에 틀린 걸로 드러났다** (2.9).
- `[정적]` (run91) bcryptprimitives 의 `DllMain` 최소 조건. `Get…Interface` 본문은 아무도 안 읽었다.

---

## 2. 찾은 것

### 2.0 새 말 — 함수 표, `Get…Interface`

이 두 말을 먼저 잡고 가자. 이번 글 전체가 이 위에 있다.

- **함수 표** = 함수 주소를 8 바이트씩 줄줄이 적은 표다. 맨 앞 (`+0`) 에 머리가 있고, `+8` 부터 함수 주소 칸이 이어진다 `[정적]` (run 문서 "표 칸 뜻").
  - bcrypt 는 이 표의 칸을 꺼내서 **간접 호출** 로 부른다. "표 `+8` 을 부른다" = "표의 `+8` 칸에 적힌 주소로 간다" 다.
  - 비유: 부품 건물이 건네주는 **연락처 명단**. 맨 위에 판 번호 (머리), 그 아래 "열기 담당 → 내선 ○○", "속성 조회 담당 → 내선 ○○" … 처럼 줄이 이어진다.
- **`Get…Interface`** = 공급자 DLL 이 내보내는 함수 8개 (`GetCipherInterface` 등) 를 묶어 부르는 run 문서 표기다. 하는 일은 "**이 종류 일의 함수 표를 주세요**" 에 답하는 것 — 받을 칸에 표 주소를 적어 준다 `[정적]` (bcrypt 가 `&표` 를 넘기고 그 뒤 그 칸을 읽는다, 2.4).
  - 비유: 부품 건물의 **"암호 담당" 호실**, **"난수 담당" 호실** … 에 전화를 걸면, 그 호실이 자기 연락처 명단을 건네준다.

### 2.1 한눈에 — 공급자 DLL 을 올린 뒤의 줄기

run 문서 "호출 관계" 를 줄였다. 전부 `[정적]`. ★ = 에뮬레이터가 막힐 수 있는 자리.

```
bcrypt!BCryptOpenAlgorithmProvider (rva 0x9410)  (phAlgorithm, pszAlgId, pszImplementation, dwFlags)
 │  … run94: KsecDD 답장 → +0x3b7 dwInterface ∈ 1..8
 │  +0x498 핸들+0x24 ← 번호 · +0x4bb wcsncpy_s 핸들+0x1bc ← 호출자 pszAlgId
 ├─ (1) +0x518  LoadProviderEx (rva 0x8840)
 │        │  +0x9e ~ +0xd1  목록 g_pLoadedProviderList 에서 이미지 이름 찾기 → 있으면 _LoadImage 건너뜀
 │        ├─ +0x121  _LoadImage → +0x9d LoadLibraryExW(경로, NULL, 0)            없을 때만 (run94)
 │        ├─ +0xee   _LoadInterface (rva 0x10810)
 │        │        │  +0x4e  점프 표 0x110cc — 번호 → 이름
 │        │        ├─ +0x130  GetProcAddress(HMODULE, 이름)           ★ NULL → +0x144 0xc0000139
 │        │        ├─ +0x1dc … +0x3d7  CFG 간접 Get…Interface(pszProvider, pszFunction, &표, 0)   ★
 │        │        │        └─ 번호 6 만 +0x345  (pszProvider, &표, 0)
 │        │        └─ +0x5d9 …  표 첫 2바이트 ≥ 1 ?                    ★ 아니면 0xc00000bb
 │        └─ +0x247  memcpy(핸들+0x30, 표, 번호별 0x30 ~ 0x70)
 ├─ (2) +0x70b  ValidateFunctionTable (rva 0xff60)   번호별 함수 칸 NULL?   ★ → 0xc0000008
 ├─ (3) +0x851  CFG 간접 표+8 (&핸들[+0x18], pszAlgId, dwFlags)         ★ status ≥ 0 이면 성공
 │        └─ +0x883  0xc000a001 · 번호 2 · 플래그 0x8 → 0x8 빼고 다시
 ├─ (4) +0xb19 / +0xbcd  CFG 간접 표+0x10 (번호 2 / 3 만)
 ├─ (5) +0xcc7  CFG 간접 표+0x18 × 속성 수                              반환 안 봄
 └─ +0xce7  *phAlgorithm = 핸들 · +0xd19 0
```

- **`HMODULE`** 는 `LoadLibraryExW` 가 돌려준 "올라온 DLL 의 손잡이" 다 `[정적]` (`_LoadInterface` 의 첫 인자).
- 비유로 한 줄씩:
  - (1) 접수처가 부품 건물을 들이고 (이미 들였으면 건너뛰고), **호실 이름표** 에서 "암호 담당" 호실을 찾아 전화해 **연락처 명단** 을 받는다. 명단 맨 위 판 번호가 0 이면 돌려보낸다. 받은 명단은 손님에게 줄 **손잡이 안쪽 칸** 에 베껴 적는다.
  - (2) 베낀 명단에 **빈 줄** 이 없는지 훑는다.
  - (3) 명단 첫 줄 (열기 담당) 로 전화를 건다.
  - (4) (5) 필요하면 속성을 묻고 · 정하고, 손님에게 손잡이를 건넨다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ +0x130  GetProcAddress(HMODULE, 이름)   NULL → +0x144 0xc0000139` (`_LoadInterface` 아래)

- **누가**: `bcrypt!_LoadInterface`.
- **어디서**: call 자리 `+0x130`, ret 자리 `+0x137` (run 문서 "호출 지점" 표).
- **누구를**: `GetProcAddress`.
- **무엇을 넘기나** `[정적]`:
  - `RCX = R8` (`+0x12d`). 이 `R8` 은 함수 입구 `+0x28 MOV R8,RCX` 로 받아 둔 **첫 인자 = 들어온 `HMODULE`** 이다.
  - `RDX` = 번호별 이름. 예: 번호 1 이면 `+0x54` 줄이 고른 `GetCipherInterface` (bcrypt 안 문자열 rva `0x20010`).
- **무엇을 위해**: 방금 들인 부품 건물의 **호실 이름표** 에서, 번호에 맞는 담당 호실 주소를 찾으려고.
- **결과를 어떻게 보나** `[정적]`: `+0x13c MOV R10,RAX` · `+0x13f TEST` · `+0x142 JNZ`. 찾았으면 그 주소를 `R10` 에 두고 다음으로. **NULL 이면 `+0x144` 에서 `0xc0000139`** 로 끝난다.

#### 줄 2: `├─ (3) +0x851  CFG 간접 표+8 (&핸들[+0x18], pszAlgId, dwFlags)` (`BCryptOpenAlgorithmProvider` 아래)

- **누가**: `bcrypt!BCryptOpenAlgorithmProvider`.
- **어디서**: call 자리 `+0x851`, ret 자리 `+0x856`. `ValidateFunctionTable` 이 통과한 뒤 (`+0x715 JNS +0x83a`) 에만 온다.
- **누구를**: 이름으로 부르지 않는다. 지역 칸 `[RBP-0x28]` 에 꺼내 둔 주소 (`+0x841` · `+0x84a`) 를 **교환원 (CFG)** 을 거쳐 부른다. run 문서는 이 대상을 **함수 표 `+8` (열기)** 로 적었다 `[정적]`. 칸 이름 `OpenAlgorithmProvider` 는 `[추측: 공개 헤더]`.
- **무엇을 넘기나** `[정적]`:
  - `RCX = &핸들[+0x18]` (`+0x84d`) — 공급자가 **자기 쪽 핸들** 을 써 줄 자리.
  - `RDX = R12` = **호출자 `pszAlgId`** (`+0x83e`). `R12` 는 함수 입구 `+0x5f MOV R12,RDX` 로 받아 둔 둘째 인자다.
  - `R8D = [RSP+0x44]` = **호출자 플래그** (`+0x845`).
- **무엇을 위해**: 공급자에게 "이 알고리즘을 열어서, 네 쪽 손잡이를 여기 적어 줘" 하려고.
- **결과를 어떻게 보나** `[정적]`: `+0x856 MOV R15D,EAX` → `+0x88f TEST R15D` · `+0x892 JNS +0xa86`. **0 이상이면 성공 길.**

### 2.3 ★ 번호 ↔ 이름 — 짝은 bcrypt 안에 박혀 있다 (질문 1)

#### 어떻게 고르나 — 단계별로 `[정적]`

1. 번호 (`dwInterface`) 에서 1 을 뺀다 (`+0x38 DEC EAX`).
2. 7 보다 크면 표 밖이다 (`+0x3a CMP EAX,7`).
3. **점프 표** (rva `0x110cc`) 에서 갈 곳을 꺼낸다 (`+0x43 MOV ECX,[R12+RAX*4+0x110cc]`) → `+0x4e JMP RCX`.
4. 도착한 줄이 `LEA RDX,[이름]` 하나로 이름을 고른다 (번호 1 → `+0x54` … 번호 8 → `+0xa8`).
5. 모두 `+0x12d` 로 모여 `+0x130 GetProcAddress`.

- 비유: 안내 데스크 표에 "종류 1 → '암호 담당' 이름표", "종류 6 → '난수 담당' 이름표" 가 **처음부터 인쇄돼 있다**. 접수처는 상담실 답장의 종류 번호만 보고 이름표를 고른다.

#### 짝 표 (run 문서 "번호 ↔ 이름 표" 에서)

| `dwInterface` | 이름 | bcrypt 문자열 rva | bcryptprimitives 가 내보내나 |
|---|---|---|---|
| 1 | `GetCipherInterface` | `0x20010` | **O** |
| 2 | `GetHashInterface` | `0x20028` | **O** |
| 3 | `GetAsymmetricEncryptionInterface` | `0x20040` | **O** |
| 4 | `GetSecretAgreementInterface` | `0x20068` | **O** |
| 5 | `GetSignatureInterface` | `0x20088` | **O** |
| 6 | `GetRngInterface` | `0x200a0` | **O** |
| 7 | `GetKeyDerivationInterface` | `0x200b0` | **O** |
| 8 | `GetKeyEncapsulationInterface` | `0x200d0` | **O** |
| `0x10001` · `0x10002` · `0x10004` | `GetKeyStorageInterface` · `GetSChannelInterface` · `GetKeyProtectionInterface` | — | X — **이 길엔 안 닿는다** (`+0x3b7` 이 `1..8` 만 통과) |

- `[정적: PE 표]` **bcryptprimitives 의 내보내기 표는 11개** 다: 위 8개 + `MSCryptConvertRsaPrivateBlobToFullRsaBlob` · `ProcessPrng` · `ProcessPrngGuid`. 이름이 있고, 주소가 모두 `.text` 안이다 (다른 DLL 로 넘기는 포워더가 아님). flow-writer 가 `objdump -p` 로 다시 셌다.
  - 내보내기 번호 · rva · 크기는 run 문서 표에 있다. (예: `GetCipherInterface` = 2 · `0xad50` · 128, `GetRngInterface` = 6 · `0x48b90` · 13.)
- 이걸로 run94 의 두 빈칸이 닫혔다:
  - "공급자 DLL 이 내보내야 할 이름 ↔ 번호" `[공백]` → **`[정적]`**.
  - "1 = 암호" `[추측]` → 번호 1 ↔ `GetCipherInterface` 로 **이름 짝이 `[정적]`**.

#### 이 표가 덮는 방향은 하나뿐이다

- 이 표는 **"번호 → 이름"** 쪽이다.
- **"알고리즘 이름 → 번호"** (예: `RSA` 는 몇 번?) 는 커널이 KsecDD 답장 `+0x18` 에 써 주는 값이라 이번에도 `[공백]` 이다 (→ Q178b · Q173b ③, 4절).

### 2.4 ★ `Get…Interface` 를 부르는 모양 (질문 2)

#### 인자 — 번호 6 만 다르다 `[정적]`

이름을 찾은 뒤 번호로 한 번 더 갈린다 (`+0x188 MOV EAX,[R15]` … `+0x1ba` 점프 표 rva `0x110ec`). `R15` 는 KsecDD 답장의 **공급자 칸** (run94 의 소개 카드) 이다.

| 번호 | 첫째 | 둘째 | 셋째 | 넷째 |
|---|---|---|---|---|
| 1 ~ 5 · 7 · 8 | `[R15+0x10]` = `pszProvider` (공급자 이름) | `[R15+8]` = `pszFunction` (알고리즘 이름) | `R14` = 표 주소를 받을 칸 | `0` |
| **6 (Rng)** | `[R15+0x10]` = `pszProvider` | `R14` = 표 주소를 받을 칸 | `0` | (없음) |

- 근거 줄: 번호 1 은 `+0x1cb` · `+0x1cf` · `+0x1d2` · `+0x1d6` · `+0x1d9 MOV RAX,R10` · `+0x1dc` 부름. 번호 6 은 `+0x338` · `+0x345` 부름.
- `R10` = 줄 1 (2.2) 에서 `GetProcAddress` 가 찾아 준 주소. 그걸 `RAX` 에 넣고 **교환원 (CFG)** 을 거쳐 부른다.
- `[R15+8]` · `[R15+0x10]` 을 run94 답장 기준으로 다시 읽으면 **답장 `0x20` · `0x28`** 칸이다 (공급자 칸이 답장 `0x18` 에서 시작하니까: `0x18 + 0x08 = 0x20`, `0x18 + 0x10 = 0x28` — run 문서가 같은 대응을 적었다).
- 칸 이름 (`pszProviderName` · `pszAlgorithmName` · `ppFunctionTable` · `dwFlags`) 은 `[추측: 공개 헤더]`.
- 비유: 접수처가 "암호 담당" 호실에 전화해서 **첫 마디로 공급자 이름, 둘째 마디로 알고리즘 이름** 을 말하고, "명단은 이 칸에 적어 주세요" 하고 빈 칸을 내민다. 난수 담당에겐 알고리즘 이름을 안 말한다.

#### 반환을 어떻게 보나 — 단계별로 `[정적]`

1. `+0x1e5 TEST EAX` · `+0x1e7 JNS +0x58b` (번호마다 같은 꼴).
2. **음수면**: WPP 로그 뒤 `+0x573 MOV EAX,EBX` 로 **그 음수를 그대로** 돌려준다.
   - → `LoadProviderEx +0xf7 JZ` 안 탐 → `+0x10f DebugTraceError` → 실패 길 (`+0x514 _FreeImage`, 출력 0).
3. **0 이상이면**: `+0x58b` → `+0x5c0` 점프 표 rva `0x1110c` → **머리 검사** (2.5).
4. 머리 검사를 통과하면 `+0x5a2 XOR EBP,EBP` — **성공 반환은 늘 0** 이다. 공급자가 양수를 돌려줘도 bcrypt 는 0 으로 바꿔 올린다.

#### 표 주소가 NULL 이면 — 크래시 `[정적: 읽기]`

- 머리 검사는 `+0x5d1 MOV RAX,[R14]` 로 받은 표 주소를 꺼내고, 바로 `+0x5d9 CMP CX,[RAX]` 로 **그 주소를 읽는다**.
- 부르기 전에 받을 칸 `[R14]` 를 0 으로 비워 두지도 않는다 (결과서).
- 그래서 공급자가 "성공" 이라 하면서 **표 주소를 안 적어 주면**, bcrypt 는 NULL (또는 쓰레기) 을 읽다가 넘어진다.
- 비유: "명단 드렸어요" 라고 해 놓고 빈 칸을 돌려주면, 접수처는 그 빈 칸을 명단인 줄 알고 읽으려다 넘어진다.

### 2.5 표 머리 검사 — "첫 2바이트가 1 이상" 하나뿐 `[정적]`

**새 말 — 표 머리**: 함수 표 맨 앞 (`+0`) 의 칸. 함수 주소가 아니라 표 자체에 대한 정보 칸이다.

- 번호마다 같은 꼴이다: `MOV RAX,[R14]` · `MOV ECX,1` · **`CMP CX,word ptr [RAX]`** · `JBE` (통과).
  - 곧 **표 첫 2바이트 ≥ 1** 이면 통과. 0 이면 `MOV EBP,0xc00000bb`.
- CMP 줄: 번호 1 `+0x5d9` · 2 `+0x64b` · 3 `+0x671` · 4 `+0x69a` · 5 `+0x6cf` · 6 `+0x704` · 7 `+0x739` · 8 `+0x76e`. 실패 줄 (번호 1) `+0x5de`.
- **두 번째 2바이트나 크기 칸은 안 본다.**
- 첫 2바이트가 공개 헤더의 `Version.MajorVersion` 으로 보인다는 건 `[추측]`.
- 비유: 명단 맨 위 **판 번호** 가 0 만 아니면 된다. 몇 판인지, 몇 줄짜리인지는 안 따진다.
- 주의: 이 검사는 공급자가 준 **원본 표** 에 대해서다. 다음 절에서 bcrypt 는 표를 베끼고, NULL 검사 (2.7) 는 **베낀 표** 에 대해서 한다.

### 2.6 ★ 표를 어디에 두나 — 핸들 `+0x30` 으로 베낀다 (질문 2)

#### 순서 `[정적]`

1. `_LoadInterface` 는 표 주소를 `LoadProviderEx` 의 지역 칸 `[RSP+0x48]` 에만 돌려준다 (`+0xe6 LEA R9,[RSP+0x48]`).
2. `LoadProviderEx` 가 복사 크기를 번호별로 고른다 (`+0x16a ~ +0x230`, 점프 표 rva `0x8d9c`).
3. `+0x237 MOV RDX,[RSP+0x48]` (원본 표) · `+0x23c MOV RCX,[RSP+0xa0]` (다섯째 인자 = **핸들 `+0x30`**) · `+0x244 MOV R8D,EAX` (크기) · **`+0x247 memcpy`**.

#### 번호별 복사 크기

| 번호 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| 크기 | `0x60` | `0x60` | `0x70` | `0x68` | `0x60` | `0x30` | `0x58` | `0x60` |

- **원본 표는 복사 뒤 안 쓴다.** 그러니 공급자 쪽 표는 DLL 안 정적 데이터여도 되고, 복사 크기만큼 읽히기만 하면 된다.
- 이후 bcrypt 의 다른 함수는 **`핸들 + 0x30 + 표 칸`** 을 부른다.
  - 예: `BCryptGenRandom +0x6b MOV RAX,[RAX+0x58]` — 핸들 `+0x58` = 표 `+0x28` (run 문서가 적은 대응: `0x30 + 0x28 = 0x58`).
- 비유: 명단 원본은 돌려주고, 손님에게 건넬 **손잡이 안쪽 칸** 에 베낀 사본만 들고 다닌다. 나중에 손님이 손잡이로 "난수 주세요" 하면 그 사본의 줄을 보고 전화한다.

#### 핸들 안에서 이번에 보인 칸 (run 문서 "핸들 칸" 에서 대표만)

| 핸들 칸 | 뜻 |
|---|---|
| `+0x00` | `0x3c8` (`+0x49f`) |
| `+0x18` | **공급자 쪽 핸들** — 열기 (2.8) 가 써 준다 |
| `+0x24` | `dwInterface` (run94 `+0x498`) |
| `+0x28` | 올린 공급자 노드 (2.11 목록의 한 칸) |
| `+0x30 ~` | **베낀 함수 표** (`0x30 ~ 0x70` 바이트) |
| `+0x1bc` | 알고리즘 이름 = **호출자 `pszAlgId`** (`+0x4bb wcsncpy_s`, 최대 `0x104` 글자) |

### 2.7 ★ `ValidateFunctionTable` — 빈 줄이 없는지만 본다 (질문 3)

#### 어디서 불리나 — `_LoadInterface` 안이 아니다 `[정적]`

요청서는 이 함수가 `_LoadInterface` 안 어딘가에서 불릴 거라 보고 물었다. 실제로는:

1. `+0x518 LoadProviderEx` 가 끝나고 (`+0x539 TEST R15D` · `+0x53c JNS +0x682`),
2. 핸들 `+0x30` 에서 `0x188` 바이트를 지역 `[RBP-0x10]` 으로 한 번 더 복사한 뒤 (`+0x682 ~ +0x6e3`),
3. 그 지역 사본을 넘겨 **`BCryptOpenAlgorithmProvider +0x70b`** 에서 부른다 (ret `+0x710`).

- 그래서 검사가 **두 군데로 나뉜다**:
  - **머리** (첫 2바이트 ≥ 1) — `_LoadInterface` 가, **원본 표** 에 대해 (2.5).
  - **NULL 칸** — `ValidateFunctionTable` 이, **베낀 표** 에 대해 (이 절).

#### 무엇을 보나 `[정적]`

- 번호가 `1..8` 밖이면 `+0xd JA +0x48e` → `0xc0000008`. (이 길에선 run94 의 `+0x3b7` 이 먼저 막는다.)
- 안이면 `+0x1a` 점프 표 rva `0x10444` 로 번호별 검사로 간다.
- **머리 (버전 · 크기) 는 안 본다.** `+8` 부터 8 바이트씩 함수 칸이 **NULL 이 아닌지만** 본다. 하나라도 NULL 이면 `0xc0000008`.

| 번호 | 이름 | 복사 크기 | NULL 이면 안 되는 칸 | 실패 줄 |
|---|---|---|---|---|
| 1 | Cipher | `0x60` | `+8 ~ +0x58` 11칸 전부 | `+0x88` |
| 2 | Hash | `0x60` | `+8 ~ +0x48` 9칸 (`+0x50` · `+0x58` 은 안 봄) | `+0x117` |
| 3 | AsymEnc | `0x70` | `+8 ~ +0x68` 13칸 전부 | `+0x1be` |
| 4 | SecretAgreement | `0x68` | `+8 ~ +0x60` 12칸 전부 | `+0x254` |
| 5 | Signature | `0x60` | `+8 ~ +0x58` 11칸 전부 | `+0x2e4` |
| 6 | Rng | `0x30` | `+8 ~ +0x28` 5칸 전부 | `+0x350` |
| 7 | KeyDerivation | `0x58` | `+8 ~ +0x50` 10칸 전부 | `+0x3da` |
| 8 | KeyEncapsulation | `0x60` | `+8 ~ +0x58` 11칸 전부 | `+0x466` |

- 복사 크기의 마지막 칸까지 본다. 번호 2 만 마지막 둘을 뺀다.
- 덤으로 칸 몇 개를 꺼내 준다: 표 `+8` · `+0x20` · `+0x18` · `+0x10` → 출력 넷. 단 **번호 2 · 3 만** 채우고, 나머지 번호는 초기값 0 그대로 (`BCryptOpenAlgorithmProvider +0x66`).
- 이 함수가 더 부르는 건 `WPP_SF_sDsd` (`+0x4d3`) 하나뿐 — 잎에 가깝다.
- 비유: 베낀 명단에서 **번호 받을 줄에 빈 칸이 있으면** "이 명단은 못 쓰겠어요" (`0xc0000008`). 판 번호는 여기선 다시 안 본다.

### 2.8 열기 — 표 `+8`, 그리고 그 뒤 (질문 4)

#### 열기 `[정적]`

- 2.2 줄 2 그대로: 표 `+8` 을 `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)` 로 부른다. **status ≥ 0 이면 성공.**
- 여기서 넘기는 이름은 **KsecDD 답의 `pszFunction` 이 아니라 호출자 이름** 이다 (2.9).

#### 다시 열기 (`+0x883`) — 조건 셋이 다 맞을 때만 `[정적]`

1. 열기 결과가 `0xc000a001` (`+0x859`).
2. 번호 2 (hash) (`+0x860 CMP [RSP+0x78],2` · `+0x86a JNZ`).
3. `(플래그 & 0x68) == 8` (`+0x86c ~ +0x873`).

- 그러면 `+0x875 AND R8D,0xfffffff7` 로 플래그 `0x8` 을 빼고 다시 부른다. 그 뒤 `+0x888 OR [핸들+8],8` 로 핸들 플래그 칸에 `8` 을 적어 둔다.
- `0x8` = `BCRYPT_ALG_HANDLE_HMAC_FLAG` 는 `[추측: 이름]`.
- 비유: 해시 담당에게 "HMAC 으로 열어 주세요" 했는데 특정 거절 (`0xc000a001`) 이 오면, "그럼 그냥 여세요" 로 한 번 더 부탁하고 손잡이에 "HMAC 은 내가 챙김" 메모를 단다. (왜 그렇게 하는지의 뜻풀이는 비유일 뿐이고, 코드로 본 건 위 세 조건과 두 줄이다.)

#### 그 뒤 `[정적]`

- **번호 2 · 3 만** 표 `+0x10` 을 부른다: 번호 2 `+0xb19` (`L"HashBlockLength"`) · 번호 3 `+0xbcd` (`L"PaddingSchemes"`). 음수면 표 `+0x20` (`+0xc31`) 을 부르고 실패한다 — **다음 공급자로 안 간다**.
- 속성 수만큼 표 `+0x18` (`+0xcc7`). 반환은 안 본다.
- `+0xce7` 에서 `*phAlgorithm = 핸들`, `+0xd19` 에서 0 을 돌려준다.

#### 표 칸 뜻 한 장 (부르는 모양 `[정적]`, 이름 `[추측: 공개 헤더]`)

| 표 칸 | 이름 (추측) | 부르는 자리 |
|---|---|---|
| `+0` | 머리 (Version) | `_LoadInterface +0x5d9` 등 (2.5) |
| `+8` | `OpenAlgorithmProvider` | `+0x851` · `+0x883` |
| `+0x10` | `GetProperty` | `+0xb19` · `+0xbcd` |
| `+0x18` | `SetProperty` | `+0xcc7` |
| `+0x20` | `CloseAlgorithmProvider` | `+0xc31` |
| `+0x28 ~` | 알고리즘별 (번호 6: 난수 만들기) | `BCryptGenRandom +0x75` 등 — 이번 범위 밖 |

### 2.9 ★ run94 정정 — 이름 두 개가 각각 어디로 가나

이번 바퀴에서 가장 조심해서 읽을 곳이다. 천천히 가자.

#### 이름 두 개

- **(가) 호출자 `pszAlgId`** — 프로그램이 `BCryptOpenAlgorithmProvider(&h, L"AES", …)` 처럼 넘긴 이름.
- **(나) KsecDD 답의 `pszFunction`** — 상담실 답장 소개 카드의 알고리즘 이름 칸 (run94 답장 `0x20` 칸 → `0x60` 쪽의 문자열).
- 그리고 하나 더, **(다) KsecDD 답의 `pszProvider`** — 소개 카드의 공급자 이름 칸 (run94 답장 `0x28` 칸, 최소 답에선 `-1` = 없음).

#### run94 가 적은 것

- run94 "가장 작은 출력 한 장" 표의 `0x60` 줄: "`BCryptOpenAlgorithmProvider +0x4bb` 가 **핸들 안에 복사**" — 곧 (나) 가 핸들에 들어간다고 읽혔다.
- sast/90 결과서: (다) `pszProvider` 는 "**로그에만 씀**".

#### 이번에 코드로 본 것 — 단계별로 `[정적]`

1. `+0x4bb wcsncpy_s` 의 원본은 `+0x49c MOV R8,R12` 다.
2. `R12` 는 함수 입구 `+0x5f MOV R12,RDX` — `BCryptOpenAlgorithmProvider` 의 **둘째 인자 = (가) 호출자 `pszAlgId`** 다.
3. → 핸들 `+0x1bc` 에 들어가는 건 **(가)** 다. (나) 가 아니다.
4. 그럼 (나) 는 어디로? `_LoadInterface +0x1cb MOV RDX,[R15+8]` — **`Get…Interface` 의 둘째 인자** 로 간다 (2.4).
5. (다) 는? `_LoadInterface +0x1d2 MOV RCX,[R15+0x10]` (번호 6 은 `+0x338`) — **`Get…Interface` 의 첫째 인자** 로 간다. 로그용이 아니다.
6. 열기 (표 `+8`) 에 가는 이름은 다시 **(가)** 다 (`+0x83e`, 2.2 줄 2).

| 이름 | 어디로 가나 |
|---|---|
| (가) 호출자 `pszAlgId` | 핸들 `+0x1bc` (`+0x4bb`) · 열기 둘째 인자 (`+0x851`) |
| (나) KsecDD `pszFunction` | `Get…Interface` 둘째 인자 (`_LoadInterface +0x1cb`) |
| (다) KsecDD `pszProvider` | `Get…Interface` 첫째 인자 (`_LoadInterface +0x1d2` · 번호 6 `+0x338`) |

- 비유: 손님이 접수처에 "AES" 라고 말했다 (가). 상담실 소개 카드에도 알고리즘 이름 (나) 과 공급자 이름 (다) 이 적혀 있다. 접수처는 **손잡이 이름표** 와 **열기 전화** 엔 손님이 말한 이름 (가) 을 쓰고, **명단 달라는 전화** 엔 소개 카드의 두 이름 (다)(나) 을 첫째 · 둘째 마디로 쓴다.

#### 그래서 무엇이 달라지나

- **run94 의 결론 "그 최소 답이 `BCryptResolveProviders` 를 통과한다" 는 그대로** 다. bcrypt 쪽 검사는 바뀐 게 없다.
- 달라지는 건 **그 다음** 이다: 최소 답의 (다) 가 `-1` 이면 **NULL 이 진짜 `Get…Interface` 첫 인자로 간다**.
  - 진짜 DLL 이 NULL 을 받아 주나? → `[공백]` (DLL 본문을 안 읽음 → Q183 ①).
  - 안전한 쪽은 `L"Microsoft Primitive Provider"` 를 넣는 것 — `[추측: 결과서]`.
- run94 문서 맨 위에 "정정 (run96)" 한 줄이 들어갔고, run94 해설 맨 위에도 "고쳐 읽기 (run96)" 한 줄을 넣었다 (본문은 둘 다 그대로).

### 2.10 CFG — 교환원이 확인을 하나, 연결만 하나

#### 간접 호출이 지나는 길 `[정적: PE 바이트]`

이번 글의 간접 호출은 **전부** 같은 길을 지난다 — `Get…Interface` · 열기 · 표 `+0x10` · `+0x18` · `+0x20` · `BCryptGenRandom +0x75`.

```
CALL 0x18001d010   →  jmp 0x18001c060   →  jmp [0x18001ed98]   (__guard_dispatch_icall_fptr)
```

- `_calls.tsv` 에는 `indirect:cfg:?` 로 찍힌다.
- `0x18001ed98` 칸 = `__guard_dispatch_icall_fptr` — **교환원이 앉는 자리** 다 (로드 구성의 `GuardCFDispatchFunctionPointer`, 결과서).
- bcrypt `DllCharacteristics 0x41e0` 에 `GUARD_CF` 비트가 있다.

#### 파일 속 값은 "확인 없이 연결" `[정적: PE 바이트]`

- 파일 그대로면 `[0x18001ed98] = 0x18001c090` — 거기 명령은 **`jmp rax`** 다. 검사 없이 그냥 대상으로 뛴다.
- 비유: 교환원 자리에 **"묻지 말고 바로 연결"** 이라 적힌 쪽지만 붙어 있다.

#### 실행 때 바뀌나 — 모른다 `[공백]`

- 실행 때 ntdll 이 이 칸을 진짜 검사 함수로 바꿔 쓰는지는 **동적으로만** 안다 (→ Q179 ①).
- 바꿔 쓴다면, 대상 (`bcryptprimitives` 안 함수) 이 **출입 명단** (CFG 비트맵) 에 "유효" 로 올라 있어야 한다 `[추측]`. 아니면 `__fastfail` (`int 0x29`) 로 프로세스가 끝난다 `[추측]`.

#### 그럴 것 같다는 짐작 — 단계별로 (flow-writer 보탬 ⑤)

1. 근거: run63 은 test.exe 에서 `LdrControlFlowGuardEnforced` 가 0 이라고 적었다 (`LdrSystemDllInitBlock +0xb8` CFG 비트맵 주소 = 0, 짐 지도 사진). 그래서 `LdrpCfgProcessLoadConfig` 의 CFG 갈래를 안 탄다 `[정적]` (run63).
2. 그래서: 그 갈래가 DLL 의 `__guard_dispatch_icall_fptr` 를 바꿔 쓰는 곳이라면, 일반 프로세스에선 bcrypt 의 간접 호출도 `jmp rax` 그대로일 것이다.
3. 아직 확인 아님: "그 갈래가 바꿔 쓰는 곳" 이라는 것 자체가 `[추측]` 이다 (쓰는 줄은 안 봤다). 같은 꼴의 파일 값 (`jmp rax`) 은 ntdll 칸 (run28) · test.exe 칸 (run30) 에도 적혀 있다.

- 비유로: 출입 명단이 아예 없는 가게라면 교환원도 "묻지 말고 연결" 쪽지 그대로일 것이다 — 이건 짐작이고, 확인은 Q179 ① 에서 한 줄 (`dq bcrypt!__guard_dispatch_icall_fptr L1`) 로 한다.

#### 콜스택 함정 `[추측]`

- 중개가 `JMP` 뿐이라 **콜스택에 bcrypt 쪽 중개 프레임이 안 남는다** ([run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다) 와 같은 꼴).
- 그래서 `bcryptprimitives!GetCipherInterface` 에서 `k` 를 찍으면 바로 위가 `bcrypt!_LoadInterface+0x1e1` 로 보일 것이다.
- 대상을 확실히 보려면 `_LoadInterface +0x1dc` (call 직전, `rax` = 대상) 에 거는 게 확실하다 (run 문서 디버거 메모).

### 2.11 같은 DLL 은 한 번만 들인다 (질문 5)

#### 두 번 안 올리는 장치는 `LoadProviderEx` 안에 있다 `[정적]`

1. 목록 `g_pLoadedProviderList` (rva `0x25b80`) 를 `+0x9e ~ +0xd1` 에서 돈다. 노드 이름과 이미지 이름을 `_wcsicmp` (대소문자 무시) 로 비교한다. 이 목록은 크리티컬 섹션 `g_csLoaderLock` (rva `0x25ba0`) 로 지킨다 (결과서).
2. **있으면**: `+0xd8 MOV RAX,[RBX+8]` (노드의 `HMODULE`) → `_LoadImage` 를 **건너뛰고** `+0xee _LoadInterface` 로. 노드의 참조 수 `[노드+0x10]` 을 1 올린다.
3. **없으면**: `+0x121 _LoadImage` → 성공이면 `+0x12a JZ +0xe1` → `+0xee _LoadInterface`. 그리고 새 노드를 만들어 목록 머리에 단다 (`+0x366 RtlAllocateHeap` · `+0x403`).
   - 새 노드: `+0` 이름 · `+8 HMODULE` · `+0x10` 참조 수 · `+0x18` 다음.

- 예: AES 한 번 · 난수 한 번 열면 `LoadLibraryExW` 는 **한 번** 이다 (run 문서).
- 단, 이 목록은 **bcrypt 가 올린 것만** 안다. 다른 길로 이미 올라간 `bcryptprimitives.dll` 이면 `LoadLibraryExW` 가 불리고, 같은 `HMODULE` 을 돌려주는 건 ntdll 로더 몫 `[추측]`.
- 비유: 접수처가 **"들인 건물 장부"** 를 따로 둔다. 장부에 있으면 다시 안 들이고 장부에 적힌 손잡이를 쓴다. 장부엔 접수처가 직접 들인 건물만 적힌다.

#### `_LoadImage` 자체 `[정적]`

- `+0x9d LoadLibraryExW(경로, NULL, 0)` — 검색 경로 플래그 없음. 경로 = 시스템 폴더 + `\` + 이름 (run94).
- 이름이 `bcrypt.dll` 이면 (`+0x7b RtlCompareUnicodeString`, 대소문자 무시, 경로가 아닌 이름 비교) `+0x140 g_hInstance` (자기 자신) 를 쓰고 `LoadLibraryExW` 를 안 부른다.
- 실패 때만 `_FreeImage` (`+0x12 FreeLibrary`). `g_hInstance` · NULL 이면 안 한다.

#### Vsm 갈래 — 일반 프로세스는 안 탄다 `[정적]`

- `BCryptOpenAlgorithmProvider +0x1e2 TEST AL,1` · `+0x1e4 JNZ +0x201` 를 **안 탈 때** (`g_TrustedEnvironment` 비트 0 이 0) 만 `+0x1f4 VsmOpenAlgorithmProvider` → `+0x16d VsmLoadInterface` 로 간다.
- 일반 프로세스는 그 값이 `1` 이라 안 탄다 — run92 의 NtQIP `0x56` 조건을 지키면 그대로다. 본문은 안 따라갔다 (이름만).

### 2.12 실패 status 와 "다음 공급자로 가나"

#### 이 사슬의 실패 status (run 문서 표에서 줄임) `[정적]`

| status | 자리 | 조건 |
|---|---|---|
| `0xc0000139` | `_LoadInterface +0x144` | `GetProcAddress` NULL |
| 공급자가 준 음수 | `_LoadInterface +0x1e7` 등 | `Get…Interface` 음수 |
| `0xc00000bb` | `_LoadInterface +0x5de` 등 · `+0xc6` · `+0x464` · `+0x7e0` | 표 첫 2바이트 0 · 번호가 정해진 11개 밖 |
| `0xc0000008` | `ValidateFunctionTable` 실패 줄 여덟 · `+0x48e` | 번호별 NULL 칸 · 번호 `1..8` 밖 |
| `0xc0000135` / `0xc000007b` | `_LoadImage +0x18c` / `+0x176` | `LoadLibraryExW` 실패 (`GetLastError == 0x7e` 면 앞) |
| `0xc00000e5` | `LoadProviderEx +0x37` | `g_fLoaderInitialized == 0` (run94) |

(`0xc000000d` · `0xc0000017` · `0xc000009a` · `0xc0000095` 같은 노드 쪽 실패는 run 문서 "실패 status 표".)

#### 다음 공급자로 넘어가는 건 "시스템 기본" 모드일 때만 `[정적]`

- 조건: `+0x201` 에서 `g_fLoadCNGDone` 이 0 이 아니고, 호출자의 `pszImplementation` 이 NULL 일 때 (`+0x21f TEST RCX` · `+0x222 JNZ` 안 탐) → **`+0x224 MOV byte [RSP+0x40],1`**.
- 그 표시가 1 이면:
  - `LoadProviderEx` 음수 · `ValidateFunctionTable` 음수 → `+0x66e` · `+0x673` 을 지나 `+0x67d JMP +0xa12` → 다음 공급자.
  - 열기 음수 → `+0x9e2` · `+0x9e7` 을 지나 `+0x9f4 UnloadProvider` 뒤 다음 공급자.
- 표시가 1 이 아니면 그 자리에서 실패 (`+0xc36`).
- `GetProperty` 음수는 표시와 상관없이 **다음 공급자 없이** `+0xc31` Close → 실패.
- **run94 최소 답은 공급자가 하나** 라 "다음" 이 없다. 어느 단계든 실패하면 그 status 로 끝난다.

### 2.13 ★ 최소 조건 표 5 · 6 행 — 에뮬레이터가 만들 칸은 없다 (질문 6)

전제: run94 4-a 1 ~ 4 행을 맞췄고 (공급자 한 칸 + 이미지 이름 `bcryptprimitives.dll`), `LoadLibraryExW` 가 진짜 DLL 을 올려 attach 를 통과시켰다 (run91).

| # | 무엇 | 기대 | 안 맞으면 |
|---|---|---|---|
| **5** | `GetProcAddress` (`_LoadInterface +0x130`) — 번호별 이름 (AES 1 → `GetCipherInterface`, hash 2 → `GetHashInterface`, RNG 6 → `GetRngInterface`) | 0 아닌 주소 (진짜 DLL 에 8개 다 있음) | NULL → **`0xc0000139`** → `_FreeImage` → 다음 공급자 / 실패 |
| **5-b** | `Get…Interface` (CFG 간접) | status ≥ 0 **그리고** 표 주소 ≠ NULL, 표 첫 2바이트 ≥ 1 | 음수 → 그대로 위로. 표 주소 NULL → 크래시. 머리 0 → **`0xc00000bb`** |
| **5-c** | KsecDD 답의 `pszProvider` · `pszFunction` | 진짜 DLL 이 받아 줄 값 | **`[공백]`** — DLL 본문 안 읽음 |
| **5-d** | 표 복사 (`LoadProviderEx +0x247`) | 번호별 크기만큼 읽힘 | 읽기 오류 |
| **6** | `ValidateFunctionTable` (`+0x70b`) | 번호별 NULL 금지 칸이 다 채워짐 | **`0xc0000008`** |
| **6-b** | 열기 (표 `+8`, `+0x851`) | status ≥ 0, `핸들[+0x18]` 채움 | 음수 → 다음 공급자 / 실패 (`0xc000a001` 재시도는 2.8) |
| **6-c** | `GetProperty` (번호 2 · 3 만) | status ≥ 0 | 음수 → Close 뒤 실패 |
| 6-d | `SetProperty` | 반환 안 봄 (run94 최소 답은 속성 0개) | — |
| **7** | 성공 | `*phAlgorithm = 핸들`, 반환 0 | |

#### 이 표를 어떻게 읽나 — run94 표와 성격이 다르다

- run94 표 (1 ~ 4 행) 는 **에뮬레이터가 만들어야 할 답** (KsecDD 답장) 이었다.
- 이번 5 · 6 행은 **진짜 DLL 이 정하는 값** 이다 `[정적]` — 표 머리, NULL 칸, 열기 status 모두 `bcryptprimitives` 안 코드가 정한다. **에뮬레이터가 만들 값은 없다.**
- 그러니 에뮬레이터에서 `0xc0000008` · `0xc00000bb` 가 보이면, 원인은 KsecDD 답이 아니라 **"DLL 이 진짜처럼 안 돌았다"** (재배치 · IAT · CFG · 메모리) 쪽일 가능성이 높다 `[추측: 결과서]`.
  - 예: 재배치 · IAT 를 안 하면 표 칸이 0 → `0xc0000008` (run 문서 "지금 우리" 표).
- 비유: 지금부터는 **진짜 부품 건물이 제대로 서 있기만 하면** 통과한다. 흉내 상담실이 쓸 답장은 더 없다. 막힌다면 건물을 세우는 쪽 (로더) 이 덜 세운 것이다.

#### 지금 PE_emulator 쪽 (run 문서 "지금 우리" 표에서 — 엔진 코드는 안 읽었다)

| 갈래 | 지금 우리 |
|---|---|
| 번호 ↔ 이름 | **맞음** — AES 1 · hash 2 · RNG 6 다 `1..8` 안, 진짜 DLL 에 이름 있음 |
| `GetProcAddress` | 에뮬레이터 로더가 진짜처럼 돌면 O `[추측]` |
| `Get…Interface` 인자 | `pszProvider = -1` (NULL) 이면 진짜 DLL 이 받는지 `[공백]` |
| 표 머리 · NULL 칸 | 우리가 만들 칸 없음 |
| CFG | 칸 값이 파일 값 (`jmp rax`) 이면 그냥 점프. 바꿔 썼다면 검사가 대상을 받아 줘야 함 `[공백]` |
| `LoadLibraryExW` 횟수 | 이름당 한 번. 로그에 같은 DLL 이 두 번 올라가면 이상 |

#### 번호 6 (난수) 이 가장 단순하다 `[정적]`

- 표 `0x30`, 검사 5칸, `GetProperty` 없음 (`+0xaa1 CMP EAX,2` · `+0xb57 CMP EAX,3` 둘 다 아님).
- 그다음 `BCryptGenRandom +0x75` 가 **핸들 `+0x58` (= 표 `+0x28`)** 을 `(핸들[+0x18], 버퍼, 크기, 플래그)` 로 부른다 (`+0x6b MOV RAX,[RAX+0x58]`) — **이게 다음 벽 후보** 다 (→ Q183 ②).

### 2.14 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

- ① 결과서의 "`+0x216 ~ +0x229 MOV byte [RSP+0x40],1`" — 그 줄은 **`+0x224`** 하나다 (`+0x216` 은 `MOV RCX,[RSP+0x70]`, `+0x229` 는 `LEA EDI,[RCX+2]`). 결론 같음.
- ② (보탬) `LoadProviderEx` 의 "목록에 없음" 길이 `_LoadInterface` 로 가는 줄 — `+0x126 MOV EDI,EAX` · `+0x128 TEST` · **`+0x12a JZ +0xe1`** → `+0xee`.
- ③ (보탬) 결과서가 `_LoadInterface.asm` 범위를 `+0x0 ~ +0x589` 로 적었는데, 함수는 `+0x8b6` 까지다 (2222 B). 결과서 본문은 그 뒤 줄도 읽었으니 범위 표기만 다르다.
- ④ (보탬) 핸들 `+0x00` · `+0x04` · `+0x10` 을 쓰는 줄 = `+0x49f` · `+0x4ab` · `+0x4b3`.
- ⑤ (보탬, `[추측]`) 일반 프로세스에선 CFG 칸이 파일 값 그대로일 가능성 (2.10).
- ⑥ (보탬) 지도에서 `VsmOpenAlgorithmProvider +0x16d → VsmLoadInterface` 를 확인했다. `_calls.tsv` 의 `VsmLoadInterface +0x16d` (간접) 는 **같은 숫자인 다른 줄** (VsmLoadInterface 안) 이니 헷갈리지 말 것.
- 정정: **run94** 두 칸 (2.9). 이전 run 문서 중 고친 건 run94 하나다.

### 2.15 디버거 · 도구 메모

- **디버거가 붙어서 달라지는 점** `[추측]`: 이번에 읽은 줄 안에 디버거 칸을 읽는 줄은 없다. 단 CFG 칸 값은 프로세스 완화 정책 · 로더에 달렸으니, 동적 Q179 ① 의 값은 "디버거가 붙은 그 프로세스 값" 이라고 적어 둘 것.
- **이름 앞 `_`**: `_LoadInterface` · `_LoadImage` 처럼 `_` 붙은 PDB 이름이 있으니, WinDbg 에선 `x bcrypt!*LoadInterface*` 로 먼저 확인하라고 run 문서가 적었다.
- 함정 후보 (CFG 중개 `JMP` → 콜스택에 안 남음, 2.10) 는 windbg-kd-runbook 에 더할 제안으로 run 문서에 남았다 (하네스라 사용자 결정).

### 2.16 정리 — 비유로 한 번 더

1. 손님 (프로그램) 이 **암호 앞쪽 건물** 접수처에 "AES 해 주세요" 한다 (`BCryptOpenAlgorithmProvider`, 이름 (가)).
2. 접수처는 상담실 답장 (run94) 의 **소개 카드** 를 받았다: 종류 1번, 알고리즘 이름 (나), 공급자 이름 (다), 건물 이름 `bcryptprimitives.dll`.
3. 손잡이를 하나 만들고, 손잡이 이름표에 **손님이 말한 이름 (가)** 을 적는다.
4. **들인 건물 장부** 를 본다. 없으면 부품 건물을 들이고 (`LoadLibraryExW`) 장부에 적는다. 있으면 그 손잡이를 쓴다.
5. 종류 번호로 **안내 데스크 표** 를 봐서 "암호 담당 (`GetCipherInterface`)" 이름표를 고르고, 부품 건물 **호실 이름표** 에서 그 호실을 찾는다 (`GetProcAddress`). 없으면 `0xc0000139`.
6. **교환원** 을 거쳐 그 호실에 전화한다. 첫 마디 (다), 둘째 마디 (나), "명단은 이 칸에" (난수 담당에겐 (나) 를 안 말함).
7. 거절 (음수) 이면 그대로 손님에게. 명단을 받으면 **판 번호가 0 이 아닌지** 만 본다 (0 이면 `0xc00000bb`).
8. 명단을 **손잡이 안쪽 칸** 에 번호별 길이만큼 베낀다. 원본은 다시 안 본다.
9. 베낀 명단에 **빈 줄** 이 없는지 훑는다 (`0xc0000008`).
10. 명단 첫 줄 (열기) 로 전화해 손님 이름 (가) 과 플래그를 말한다. 부품 건물이 자기 쪽 손잡이를 `+0x18` 칸에 적어 준다.
11. 해시 · 비대칭이면 속성을 하나 묻고, 속성을 정한 뒤 손잡이를 손님에게 건넨다.
12. 이 모든 전화는 교환원을 지난다. 교환원이 **"묻지 말고 연결"** 쪽지 그대로인지, 진짜 명단을 들고 확인하는지는 **아직 모른다**.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — bcryptprimitives pdb 줄 | run91 몫만 | **run96 몫**: 프로젝트는 안 열고 PE 내보내기 표만, 11개 (`Get…Interface` 8 + 셋) |
| 기준 — bcrypt pdb 줄 | run92 · run94 몫 | **run96 몫** 붙음 (다시 analyze 안 함, `_LoadInterface` · `ValidateFunctionTable` 등 새로 뽑음, PE 바이트) |
| 기준 — 근거 로그 | — | run96 줄: 정적 바퀴, test.exe 길에 없음, 사진 대조 없음, 내보내기 이름은 PE 파일로 셈 (실행 때 모듈 아님) |
| 흐름도 앞 글 | — | run96 문단 하나: **그림을 안 고쳤다** (run92 · run94 와 같은 까닭) |
| 그림 | — | **그대로** |
| 3장 "bcrypt 시작 함수" 절 제목 | `… → LoadLibraryExW` (run94) 까지 | 뒤에 `LoadProviderEx → _LoadInterface → GetProcAddress(Get…Interface) → 표 복사 → …` 사슬 붙음 |
| 3장 run94 하위 절 끝 "정적 →" 줄 | "`Get…Interface` 이름 ↔ 번호 = 다음 벽 후보" | 거기에 **(해결됨) → run96** |
| 3장 새 하위 절 | — | **"공급자 DLL 을 올린 뒤 — 이름 찾기 · 표 받기 · 표 검사 · 열기 (run96)"** — 비유 (연락처 명단) + 트리 하나 + 줄 열둘 남짓 (번호 ↔ 이름 · 내보내기 · 인자 · 복사 · 검사 · 열기 · 한 번만 · 다음 공급자 · CFG · 진짜 DLL 몫 · run94 정정 · 다르게 적은 곳) + 남은 것 |
| 4장 run94 정적 줄 | ① 다음 벽 후보 · ④ 번호 1 과 이름 짝 `[공백]` | ① **(해결됨 → run96, Q178a)**, ④ 줄 그어 해결 표시 |
| 4장 run94 PE_emulator 줄 | "1 = 암호 `[추측]`" | 뒤에 "(run96) 번호 1 ↔ `GetCipherInterface`" · run94 정정 안내 |
| 4장 새 줄 | — | run96 줄 셋: 동적 · 정적 남은 것 · PE_emulator 쪽 / 다르게 적은 곳 / 보탠 것 / run94 정정 |
| 5장 | — | run 표 · 03_sprint 요청서 번호에 run96 줄 |

태그가 바뀐 것:

- 번호 1 ↔ 이름: run94 "1 = 암호" `[추측]` → **`[정적]`** (`GetCipherInterface`, `_LoadInterface +0x54`).
- 공급자 DLL 이 내보내야 할 이름 ↔ 번호: run94 `[공백]` → **`[정적]` · `[정적: PE 표]`** (8개 다 있음).
- `ValidateFunctionTable` 자리: 요청서 "`_LoadInterface` 안 몇 번째?" → **`BCryptOpenAlgorithmProvider +0x70b`** `[정적]`.
- 답장 `pszProvider`: sast/90 결과서 "로그에만 씀" → **`Get…Interface` 첫 인자** `[정적]` (정정).
- run94 답장 `0x60` 줄 "핸들 안에 복사": → **핸들에 들어가는 건 호출자 `pszAlgId`** `[정적]` (정정).

### 짐 지도 `baggage.md` — 안 고침

- test.exe 는 bcrypt 를 안 올리니 **새 짐 칸도, 고칠 칸도 없다** (run94 와 같은 판단).

### 큰 그림에서 어떻게 이어지나

- 우리 지도 (test.exe) 는 **안 바뀐다**. 이 일은 "**bcrypt 를 올리고 암호 API 를 부르는 다른 프로세스라면**" 의 옆길이다.
- 이 프로세스에서 순서를 이으면:
  1. (run92) 2.7 전원 켜기 — bcrypt `DllMain` 이 KsecDD 손잡이를 건다.
  2. (run94) 암호 API → 공급자 찾기 IOCTL → 답장 풀기 → 공급자 DLL 을 `LoadLibraryExW`.
  3. (run91) bcryptprimitives `DllMain`.
  4. **(run96) `GetProcAddress(Get…Interface)` → 함수 표 받기 · 머리 검사 → 핸들에 베끼기 → NULL 칸 검사 → 표 `+8` 열기 → 손님에게 핸들.**
  5. (아직 안 봄) bcryptprimitives 의 `Get…Interface` · 열기 본문, 그리고 `BCryptGenRandom +0x75` 가 부르는 난수 표 `+0x28` (→ Q183).
- 이번 바퀴로 **"에뮬레이터가 꾸며 줄 답" 의 끝** 이 보였다. run94 까지는 흉내 상담실이 답장을 잘 써 주면 됐다. run96 부터는 **진짜 DLL 이 진짜처럼 돌기만 하면** 된다 — 그래서 다음 문제는 로더 쪽 (재배치 · IAT · CFG) 일 가능성이 높다 `[추측: 결과서]`.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q179 ① ~ ⑤** (동적, run94 동적과 같은 프로세스)

| 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|
| **① (제일 중요) CFG 칸 실행 값** — `dq bcrypt!__guard_dispatch_icall_fptr L1` 이 파일 값 `bcrypt+0x1c090` 인가, ntdll 안 함수인가 (`ln`). 후자면 그 함수가 `bcryptprimitives` 안 대상을 받아 주나 | 파일 값만 읽었다. 실행 때 ntdll 이 바꿔 쓰는지는 동적으로만 |
| ② 진짜 KsecDD 답의 `pszProvider` · `pszFunction` — 특히 `pszProvider` 가 NULL 인가 `Microsoft Primitive Provider` 인가 | 커널 답은 범위 밖. Q179 의 `BCryptResolveProviders+0x5f2` 한 곳에서 같이 나온다 |
| ③ `Get…Interface` 가 실제로 주는 표 — 머리 첫 2바이트 · 칸 값 (NULL 없나) | DLL 본문을 안 읽었다 |
| ④ 열기 함수가 받는 `pszAlgId` · 플래그 · 반환 status, 번호 2 · 3 의 `GetProperty` 반환 | 같음 |
| ⑤ `bcryptprimitives.dll` 이 bcrypt 열기 전에 이미 올라가 있나 (`lm m bcryptprimitives`), `_LoadImage +0xa4` 가 이름당 한 번만 걸리나 | 실행 순서 문제 |

- **test.exe 로는 못 본다** (bcrypt 를 안 올림 `[관찰]` run44, sample 에 암호 API 없음 `[소스]`) → Q104 에 안 붙였다. **Q174 · Q179 와 같은 프로세스에서 묶을 후보**, 샘플 고르기는 사용자 결정.
- 추천 멈춤점 (run 문서, 그 프로세스 조건):

| 멈춤점 | 볼 것 |
|---|---|
| `bcrypt!_LoadInterface+0x130` / `+0x137` | 직전: `lm a @rcx` · `da @rdx` (고른 이름). 직후: `ln @rax` (0 이면 `0xc0000139` 길) |
| `bcrypt!LoadProviderEx+0xf3` | `eax` · 표 머리 · 칸 |
| `bcrypt!BCryptOpenAlgorithmProvider+0x70b` / `+0x710` | `ecx` (번호) · 표 사본 / `eax` |
| `bcrypt!BCryptOpenAlgorithmProvider+0x851` / `+0x856` | `ln @rax` · `du @rdx` · `r r8d` / `eax` |
| `+0xb19` / `+0xbcd` (번호 2 · 3), `+0xce7` | 속성 이름 · 반환 / 핸들 전체 |

- 대상 함수를 보려면 `_LoadInterface +0x1dc` 의 `rax` 로 (2.10 콜스택 함정).

### 정적으로 남은 것 → **Q183 ① ~ ③** (bcryptprimitives · ntdll — 다음 벽)

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| bcryptprimitives `GetCipherInterface` (`0xad50`) · `GetHashInterface` (`0xacb0`) · `GetRngInterface` (`0x48b90`) 본문과 표 `+8` (열기) · `+0x10` — **`pszProvider` NULL 이어도 되나** (run94 최소 답 `-1` 의 근거), `pszAlgId` 이름 검사, 표 머리 값 | 이번엔 bcryptprimitives 의 내보내기 표만 읽었다 | Q183 ① — **이 다음 벽** |
| 난수 표 `+0x28` (`BCryptGenRandom +0x75` 가 부르는 함수) · `Close` 표 `+0x20` — 난수가 나오는 마지막 줄까지 | 범위 밖 | Q183 ② |
| (후순위) DLL 의 `__guard_dispatch_icall_fptr` 를 ntdll 이 바꿔 쓰는 줄 — run63 `LdrpCfgProcessLoadConfig` CFG 갈래인가 | 쓰는 줄을 안 봤다 (2.10 짐작의 고리) | Q183 ③ (Q179 ① 이 더 빠를 수 있음) |

### 범위가 줄어든 것 → **Q178b**

- 이번 표로 **"번호 → 이름" 은 닫혔다.** Q178b 에 남은 건 둘:
  - `BCryptEnumProviders` · `BCryptEnumAlgorithms` 가 `ppBuffer` NULL 길 (`IoCallKernelDriver`) 을 쓰는지.
  - **"알고리즘 이름 → 번호"** (`RSA` 같은 이름이 몇 번인지) — 커널 `ksecdd.sys` 몫이라 Q173b ③ 과 겹친다.

### Q173b 에 그대로 남은 것 (이번에 안 함)

- ① ntdll `LdrAddRefDll` · `RtlPcToFileHeader` — bcrypt `DllMain` 안 `GetModuleHandleExW(5, &DllMain)` 가 성공하는 조건.
- ③ (후순위) `ksecdd.sys` 의 `0x390400` 처리 — 진짜 답장을 정적으로 보고 싶을 때.
- ④ (후순위) 실패 길 `NtClose` × 5 의 나머지 2 · `NtTraceControl` × 4 구성.

### 대기열 밖 — 이름 · 짐작으로 남은 것

- 함수 표 칸 이름 (`OpenAlgorithmProvider` · `GetProperty` · `SetProperty` · `CloseAlgorithmProvider`), 머리 = `Version.MajorVersion`, `Get…Interface` 인자 이름 — PDB 에 구조체 정의가 없어 `[추측: 공개 헤더]`.
- `0x8` = `BCRYPT_ALG_HANDLE_HMAC_FLAG` — `[추측: 이름]`.
- `0xc0000008` · `0xc00000bb` 의 원인이 "DLL 이 진짜처럼 안 돈 쪽" 이라는 것 — `[추측: 결과서]`.

---

## 5. 더 읽기

- run 문서: [`run96-bcrypt-interface.md`](run96-bcrypt-interface.md) — "PE_emulator 쪽에서 쓸 것" (Q178a 결론 · 지금 우리 표 · **최소 조건 표**), "호출 지점", "**번호 ↔ 이름 표**", "`Get…Interface` 부르는 모양", "표 머리 검사", "표를 어디에 두나", "**NULL 이면 안 되는 칸**", "표 칸 뜻", "열기 · 재시도", "CFG", "`_LoadImage` 와 이미 올라간 DLL", "핸들 칸", "실패 status 표", "검사 순서", "호출 관계", "**정정**"
- 결과서: [`_workspace/sast/92_bcrypt-interface_result.md`](../../_workspace/sast/92_bcrypt-interface_result.md) — "한눈에", 질문별 답 1 ~ 6 (3-a 머리 · 3-b NULL 칸 · 3-c 실패 status · 3-d 순서), "정정·메모", "동적분석으로 확인해야 할 것", "다음 요청 후보"
- 요청서: [`_workspace/sast/92_bcrypt-interface.md`](../../_workspace/sast/92_bcrypt-interface.md)
- flow.md: [3장 "bcrypt 시작 함수"](flow.md) 절의 새 하위 절 "공급자 DLL 을 올린 뒤 — 이름 찾기 · 표 받기 · 표 검사 · 열기", 4장 run94 줄 (해결 표시) · run96 줄 셋, 5장 run96 줄
- 대기열: [`_workspace/plan.md`](../../_workspace/plan.md) Q178a (끝) · **Q179** · **Q183** · Q178b · Q173b (그리고 묶기 후보 Q174)
- 앞 해설:
  - [run94](run94-bcrypt-resolve-해설.md) — **바로 앞 걸음** (공급자 찾기 IOCTL, 답장 · 소개 카드 · 쪽 번호, 가장 작은 답장 `0x98`). 맨 위 "고쳐 읽기 (run96)" 줄 참고
  - [run92](run92-bcrypt-ksecdd-해설.md) — bcrypt `DllMain`, KsecDD 손잡이
  - [run91](run91-bcrypt-init-해설.md) — 공급자 DLL bcryptprimitives 의 `DllMain`, 점프 표
  - [run63](run63-cfg-func-table-해설.md) — CFG 출입 명단, `LdrControlFlowGuardEnforced`
  - [run28](../02_sprint/run28-tls-init-call-해설.md) 2.9 — CFG 중개 (교환원은 통화 기록에 이름을 안 남긴다)

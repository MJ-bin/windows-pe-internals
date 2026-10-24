# run40 dll-crt-entry — 해설

> 이 글은 [run40 문서](run40-dll-crt-entry.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q55** — 2.8 sample dll 의 `_DllMainCRTStartup` → `DllMain` 이 `lpReserved`(셋째 인자)를 그대로 넘기나. 덤으로 dll1·dll2 시작 함수 안 `LoadLibraryExW` ×3 (run7 `0x7ffd80e2f5fb`·`0x7ffd80ebf60b`) 도 UCRT `try_get_function_slow` 인가 (`_workspace/plan.md`, 이번에 **끝**)
  - 실행 때 확인할 거리는 새 번호 없이 **Q27** (사슬·`r8`·`k`) 와 **Q31** (`LoadLibraryExW` ×3 이름·순서) 에 출처로 붙었다.
  - 새 정적 공백 셋은 **Q73** (a)·(b)·(c) 로 맨 아래에 붙었다.
  - 같은 바퀴에 **Q54** 를 새 run 없이 **문서로 닫았다** (2026-10-24 사용자 결정). 남은 전수 확인은 **Q72**. → 2.10
  - 다음 항목: **Q56** (plan "다음 항목").
- 근거:
  - run 문서 [`run40-dll-crt-entry.md`](run40-dll-crt-entry.md) (가장 중요)
  - 요청서 [`_workspace/sast/35_dll-crt-entry.md`](../../_workspace/sast/35_dll-crt-entry.md)
  - 결과서 [`_workspace/sast/35_dll-crt-entry_result.md`](../../_workspace/sast/35_dll-crt-entry_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 연 바이너리는 **MS 것이 아니라 우리 dll1.dll, dll2.dll** 이다. test.exe 때(run30)처럼 `sample/bin/` 의 dll·pdb 를 `ms/bin/`·`ms/sym/` 에 복사해서 Ghidra 에 처음 넣었다.
  - 함수가 dll1 **3333개**, dll2 **3332개** 잡혔다. `/MT` 빌드라 UCRT·vcruntime 이 dll 안에 통째로 들어 있어서다 (run30 해설 2.1 의 `/MT` 와 같은 이유).

이 글은 [run30 해설](run30-exe-crt-start-해설.md) 의 **dll 판**이다.

- run30 은 test.exe 의 **현관**(진입점)과 그 안쪽 CRT 를 봤다.
- 이번엔 dll1·dll2 의 현관을 본다. 그리고 [run28 해설](run28-tls-init-call-해설.md) 에서 넘겨받은 질문 하나 — "로더가 건넨 **봉투**를 CRT 가 그대로 `DllMain` 에 넘기나" — 에 답한다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](run9-create-user-process-해설.md) 2절 · rva → [run10 해설](run10-allocate-process-해설.md) 2절
- 꼬리 점프 (콜스택에 안 쌓이는 넘어가기) → [run17 해설 2.4](run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리)
- PE 진입점 `AddressOfEntryPoint` (**설계도 첫 장의 "전원 스위치는 몇 호"**) → [run25 해설 2.3](run25-dll-entry-해설.md#새-말--pe-진입점-addressofentrypoint)
- `lpReserved` (스위치와 함께 건네는 **메모**), `ProcessStaticImport` 비트, 진입점 넷째 인자 = 첫 스레드 CONTEXT 포인터 → [run27 해설 2.8](run27-init-graph-해설.md#28-진입점-넷째-인자--벽에-붙여-둔-context-의-쓰임)
- 한 칸 밀기 (`LdrpCallInitRoutineInternal` 이 넷째 인자를 진입점 셋째 인자로) → [run28 해설](run28-tls-init-call-해설.md) 2.7
- CFG 중개 (**교환원**, 통화 기록에 이름을 안 남김) → [run28 해설 2.9](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)
- 지도 범위 결정 "진입점 = 경계, CRT 한 상자", CRT, `/MT`, 보안 쿠키, 늦게 묶는 API (`try_get_function_slow`), 아래 16비트 맞추기 → [run30 해설](run30-exe-crt-start-해설.md) 2.1 · 2.5 · 2.7 · 2.8
- IAT (**벽 콘센트**) → [run19 해설](run19-proc-init-modules-해설.md) 2.7

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**, DLL = **건물**
- **전원 켜기** = DLL 시작 함수 부르기, 켜기 담당 = `LdrpInitializeNode` (run27)
- 우리 지도 = 배달 기사가 **현관까지** 오는 길, 진입점 = **현관**, CRT = 현관 안에서 **짐 푸는 법** (run30)
- `lpReserved` = 로더가 시작 함수에 건네는 **봉투**. 안에 든 것 = 첫 직원의 **업무 시작 메모**(첫 스레드 CONTEXT) 를 가리키는 쪽지, 또는 **빈 봉투**(0).
  - run27 해설에선 이걸 "스위치와 함께 건네는 메모" 라고 불렀다. 같은 것이다. 이번엔 "뜯나, 안 뜯나" 가 질문이라 **봉투**로 부른다.
- **이번에 더하는 비유** (run 문서 "결과" 의 회사 접수처 그대로) — 건물 현관 안쪽:
  - `_DllMainCRTStartup` = **정문 경비**
  - `dllmain_dispatch` = **접수처**
  - `dllmain_crt_process_attach` = 사무실 **준비 담당** (CRT 준비)
  - 우리 `DllMain` = **손님**

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 다섯**:

1. dll1·dll2 의 현관 번지(진입점) = **`_DllMainCRTStartup`** (dll1 rva `0x1d60`, dll2 `0x1d50`) `[정적]` → 2.3
2. 정문 경비는 도장(쿠키)만 찍고 **`+0x38` 꼬리 `JMP`** 로 접수처(`dllmain_dispatch`)에 넘긴다. 그래서 `DllMain` 의 콜스택에 **경비가 안 보인다** `[정적]` → 2.4
3. **봉투는 안 뜯고 그대로 간다.** `R8` 은 서랍(보존 레지스터) 두 곳을 거쳐 `DllMain` 까지 **같은 값** `[정적]` → 2.5
4. 접수처는 **준비 담당이 일을 다 끝낸 뒤** `+0x91` (ret `+0x96`) 에서 `DllMain` 을 부른다. run7 `[확인 필요]` 는 코드로 맞다 `[정적]` → 2.6
5. run7 의 두 수수께끼 주소: **7번 `…f5fb` = dll2, 9번 `…f60b` = dll1**, 둘 다 `try_get_function_slow +0x73` `[정적]` + `[관찰]`. run7 의 "dll2 범위 안" 은 틀렸다 → 2.7

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 **돌아올 자리(ret)** 다. 숫자는 앞 run 들 그대로.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessW … NtCreateUserProcess … NtResumeThread           (run9 ~ run14, run34)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ LdrpInitializeProcess   (가게 전체 세팅, DLL 올리기)                          (run17 ~ run26, run35 ~ run39)
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)                 (run27)
              └─ LdrpInitializeNode +0x19c
                   └─ LdrpCallInitRoutine +0x93
                        └─ LdrpCallInitRoutineInternal +0x22   (봉투를 넘기는 자리)   (run28)
 ─────────────────── 여기가 dll 의 현관 (dll 진입점) ───────────────────
 dll2 / dll1 (컴파일러가 넣은 CRT)
  _DllMainCRTStartup  ═JMP▶  dllmain_dispatch                                      ← 이번
     … CRT 준비 (LoadLibraryExW ×3 → 다시 ntdll 로더)                                ← 이번
     … DllMain(1)                                                                   ← 이번
 ─────────────────────────────────────────────────────────────────
   └─ NtContinue ═▶ RtlUserThreadStart → … → test.exe 현관 → main                  (run29·run30)
```

- 지금 목표(프로세스 생성 → `main`)에서 **로더가 DLL 전원을 켜는 구간의 맨 끝**이다.
- run30 에서 exe 쪽에 그은 "여기가 현관" 줄을, 이번엔 **dll 쪽에도** 긋는다. 이 줄 아래는 OS 가 아니라 dll 안에 컴파일러가 넣은 코드다.
- flow.md 로는 **2.8** 이 중심이다. 그림 2 의 dll 상자, 2.7 한 줄, 4장·5장에도 줄이 더해졌다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. dll1·dll2 의 PE 진입점 이름이 정말 `_DllMainCRTStartup` 인가?
2. 진입점 → `DllMain` 사이에 무엇이 있나? **봉투(`lpReserved`, 진입 때 `R8`) 를 안 뜯고 그대로 넘기나?** `DllMain` 은 CRT 준비 **뒤에** 불리나?
3. run7 의 `LoadLibraryExW` ×3 돌아올 주소 두 개 (`0x7ffd80e2f5fb`, `0x7ffd80ebf60b`) 는 **어느 dll 의 어느 함수**인가?
4. dll1 과 dll2 의 진입점 쪽 구조가 같은가?

### 바퀴 전에 알던 것

- `[정적]` (run28) `LdrpCallInitRoutineInternal +0x1d` 가 한 칸 밀어 진입점을 **`(RCX = DllBase, EDX = 1, R8 = r9 원본)`** 으로 부른다. `r9 원본` = 정적 import 모듈이면 `LdrpProcessInitContextRecord` 값 (첫 스레드 CONTEXT 포인터), 아니면 0.
  - 즉 봉투는 ntdll 쪽에선 **넷째 인자(`R9`)**, 한 칸 밀린 뒤 진입점에선 **셋째 인자(`R8`)** 다. 같은 봉투다.
- `[관찰]` (run8) `LdrpCallInitRoutine` 의 `rcx` 가 `_DllMainCRTStartup` 과 정확히 일치. 그 직후 같은 스레드에서 `DllMain` (`rdx=1`).
- `[관찰]` (run6) `DllMain` 은 두 dll 모두 rva `0x12e0`.
- `[관찰]` (run7) 시작 함수가 불린 뒤, `DllMain` 보다 먼저 `KERNELBASE!LoadLibraryExW` 가 3번. 돌아올 주소는 `0x7ffd80e2f5fb` (7번, "모듈 미확인"), `0x7ffd80ebf60b` (9번, "dll2 주소 범위 안").
- `[정적]` (run30) test.exe 의 같은 ×3 은 UCRT `try_get_function_slow +0x73` 이었다.
- `[추측]` (flow.md 2.8) "`LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain`. 사이에 다른 함수가 있는지는 모른다."
- `[추측]` (flow.md 2.8) "CRT 가 자기 준비를 끝낸 뒤 `DllMain` 을 불러주는 구조. `LoadLibraryExW` 3번도 CRT 준비 과정." "DLL 도 `/MT` 라 같은 UCRT 함수일 것."
- `[소스]` `sample/dll1.c` 의 `DllMain(HINSTANCE, DWORD, LPVOID reserved)` 은 `reserved` 를 **안 쓴다**.

---

## 2. 찾은 것

### 2.1 한눈에

run 문서 "호출 관계" 를 줄였다. 두 dll 이 같은 모양이라 `dllN` 으로 적는다. 숫자는 ret.

```
ntdll!LdrpCallInitRoutineInternal +0x22   (CFG 중개는 JMP 뿐, 프레임 없음)
 └─ dllN!_DllMainCRTStartup                  ← 정문 경비. 입구에서만 보임
      ├─ +0x21  __security_init_cookie        (이유 1 일 때만)
      └═ +0x38 JMP (꼬리) ═▶ dllN!dllmain_dispatch      ← 여기부터 경비 프레임 없음
           ├─ (1) +0x7b  dllmain_crt_dispatch ═▶ (JMP) dllmain_crt_process_attach
           │              └─ … CRT 준비 … └─ try_get_function_slow +0x73 → LoadLibraryExW ×3 (run7)
           └─ (2) +0x96  dllN!DllMain (1)       ← 손님. 봉투 그대로
```

- 전부 `[정적]` 이다.
- **새 말 — 이유 (`fdwReason`)**: `DllMain` 의 둘째 인자. "왜 불렀나" 를 숫자로 알려 준다. run 문서 기준으로 **1 = 프로세스 시작**, **0 = 프로세스 끝**, **2·3 = 스레드 쪽**. 이번 길은 전부 이유 1 이다.
  - 비유: 접수처가 손님에게 "오늘은 **개업** 때문에 오셨죠?" 하고 용건을 확인하는 번호.
- 그림 2 에는 이 중 **현관 상자 하나 + `LoadLibraryExW` ×3 + `DllMain`** 만 들어갔다 (3절).

두 dll 의 함수 시작 rva (run 문서 표에서 옮김):

| 함수 | dll1 rva | dll2 rva | 크기 |
|---|---|---|---|
| `_DllMainCRTStartup` | `0x1d60` | `0x1d50` | 61 |
| `dllmain_dispatch` | `0x1ba0` | `0x1b90` | 304 |
| `dllmain_crt_dispatch` | `0x1990` | `0x1980` | 86 |
| `dllmain_crt_process_attach` | `0x19f0` | `0x19e0` | 259 |
| `DllMain` | `0x12e0` | `0x12e0` | 52 |
| `try_get_function_slow` | `0x4f598` | `0x4f588` | 433 |

- 함수 **안쪽 오프셋(`+0x..`)은 두 dll 이 명령까지 같다** (결과서가 절대 주소만 지우고 비교). 다른 건 시작 rva 뿐이다: `DllMain` 뒤로 dll2 가 **`0x10` 앞**이다.
- 이 `0x10` 차이가 2.7 에서 결정적인 단서가 된다. 기억해 두자.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 정문 경비가 넘기는 줄

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `dllN!_DllMainCRTStartup` | `+0x38` | — (꼬리 `JMP`) | **`dllN!dllmain_dispatch`** | [정적] |

- 누가: `_DllMainCRTStartup` (61바이트짜리 진입점)
- 어디서: `+0x38`. `call` 이 아니라 **`JMP`** 라서 ret 칸이 비어 있다. 돌아올 자리를 **안 남긴다.**
- 누구를: `dllmain_dispatch` 맨 앞
- 무엇을 위해: CRT 준비와 `DllMain` 부르기를 **통째로 맡기려고.** 받은 `RCX`·`EDX`·`R8` 은 그대로 실어서 넘긴다 (2.5).

#### 한 줄 읽기 ② — 손님을 부르는 줄

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `dllN!dllmain_dispatch` | `+0x91` | **`+0x96`** | **`dllN!DllMain`** | [정적] |

- 누가: 접수처 `dllmain_dispatch`
- 어디서: `+0x91` 에서 부르고, `DllMain` 이 끝나면 `+0x96` 으로 돌아온다. **콜스택에 찍히는 숫자는 `+0x96`** 이다.
- 누구를: 우리 `DllMain`
- 무엇을 위해: 준비가 끝났으니 손님에게 **용건(이유 1)과 봉투(`R8`)** 를 건네며 안내한다. 바로 앞 `+0x89 ~ +0x8e` 에서 `R8`·`EDX`·`RCX` 를 다시 싣는다.

#### 한 줄 읽기 ③ — 관리실에 다시 전화하는 줄

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `dllN!try_get_function_slow` | `+0x6d` | **`+0x73`** | `LoadLibraryExW` (`CALL [IAT]`, `(이름, NULL, 0x800)`) | [정적] |

- 누가: UCRT 안 `try_get_function_slow` — test.exe 에서 본 것과 **같은 함수**다 (run30).
- 어디서: `+0x6d` 에서 부르고 `+0x73` 으로 돌아온다. 이 `+0x73` 이 run7 의 두 돌아올 주소와 맞는 자리다 (2.7).
- 누구를: `LoadLibraryExW` — dll 의 콘센트(IAT) 칸을 거쳐서 (dll1 rva `0x6e1f8`, dll2 `0x6e1e8`).
- 무엇을 위해: "이 API 가 든 DLL 을 올려 주세요" — **ntdll 로더를 다시 부르려고.** 앞에서 `+0x62 EDX = 0`, `+0x67 R8D = 0x800` 을 싣는다. 하는 일은 test.exe 와 같고, 데이터 칸 위치만 다르다 (`module_names` dll1 `0x708b0`, dll2 `0x708a0`).

### 2.3 핵심 ① 현관 번지 = `_DllMainCRTStartup`, 그리고 "이 파일이 그 파일인가"

#### 이름 확인 `[정적]`

- PE 진입점 rva: dll1 `0x1d60`, dll2 `0x1d50`. 둘 다 PDB 이름 **`_DllMainCRTStartup`** (61바이트).
- run8 `[관찰]` "`LdrpCallInitRoutine` 의 `rcx` = `_DllMainCRTStartup`" 과 같다.
- PDB 타입은 `int __cdecl _DllMainCRTStartup(HINSTANCE__ *, ulong, void *)` 모양이다 (결과서). **셋째 `void *` 가 봉투 자리**다.
- `[소스]` 소스엔 `DllMain` 만 있고 `_DllMainCRTStartup` 은 없다. 빌드 때 CRT(`/MT`)가 넣어 준 함수다 (flow.md 2.8).

#### 올린 파일이 VM 에서 돈 그 파일인가 — 단계별로

이번에도 MS 바이너리가 아니라서 이 확인이 따로 필요했다 (run30 해설 2.4 와 같은 순서).

1. `[정적]` **pdb 와 dll 이 짝이다.**
   - **새 말 — CodeView 서명**: dll 파일 안에 적힌 "**내 짝 pdb 는 이 번호**" 표. pdb 쪽에도 같은 번호가 적혀 있다.
   - 비유: 열쇠와 자물쇠에 같은 일련번호가 찍혀 있는 것.
   - dll1 `B1FF0915-FAC1-447A-AEB8-10FF10AD0564` age 1, dll2 `FC67F681-4188-49D8-83B5-609127E1DE39` age 1. 결과서가 pdb 파일 구조(MSF)를 직접 읽어 **글자까지 같음**을 봤다.
2. `[정적]` + `[관찰]` 진입점 rva 의 PDB 이름 = run8 `rcx` 이름.
3. `[정적]` + `[관찰]` `DllMain` rva `0x12e0` = run6 값.
4. `[정적]` + `[관찰]` run7 의 두 돌아올 주소, 그 아래 16비트가 이 파일들에 **ret 자리로 하나씩** 있다 (2.7).
5. → 여러 로그와 다 맞는다. 그래서 이 빌드를 근거로 쓴다.
6. 단 01_sprint 기록만으로 "VM 에서 돈 dll 과 **같은 빌드**" 까지 단정하진 않는다 `[추측]` (run 문서). test.exe 는 run3 ~ run5 와 run6 ~ run8 빌드가 따로였는데, dll 도 그랬는지는 **기록이 없다** (flow.md 5장).

### 2.4 핵심 ② 정문 경비는 꼬리 `JMP` — 콜스택에서 사라진다

#### 경비가 하는 일은 딱 둘 `[정적]`

- 이유가 1 이면 `+0x1c` 에서 `__security_init_cookie` 를 부른다 (ret `+0x21`). 이유가 1 이 아니면 `+0x1a JNZ` 로 건너뛴다.
  - 보안 쿠키 = 택배 상자 **봉인 스티커** 무늬를 정해 두는 일 ([run30 해설 2.5](run30-exe-crt-start-해설.md)).
- 그다음 `+0x38` 에서 **`JMP dllmain_dispatch`**.
- 끝이다. CRT 준비는 경비 **자기 몸**이 하지 않는다. 그건 접수처가 부르는 준비 담당 몫이다 (2.6).

#### 왜 콜스택에서 사라지나 — 스택을 한 칸씩 따라가 보자

콜스택(`k`)은 스택에 쌓인 **돌아올 주소**를 위에서부터 읽은 것이다. 그러니 "누가 돌아올 주소를 남겼나" 만 따라가면 된다.

1. ntdll `LdrpCallInitRoutineInternal +0x1d` 가 **`CALL`** 한다 (CFG 중개는 `JMP` 뿐이라 따로 안 남는다, run28). → 스택 맨 위에 **`Internal+0x22`** 가 쌓인다.
2. 경비 `_DllMainCRTStartup` 이 들어온다. `+0xa` 에서 `RDI` 를 스택에 잠깐 맡긴다 (`PUSH`).
3. `+0x1c` 쿠키 함수를 **`CALL`** 한다. → `+0x21` 이 쌓였다가, 쿠키 함수가 끝나면 **빠진다.**
4. `+0x37 POP RDI` — 맡긴 것을 도로 찾는다. 이제 스택 맨 위는 다시 **`Internal+0x22`** 다. 경비가 쓴 흔적이 하나도 없다.
5. `+0x38 JMP` — 돌아올 주소를 **새로 안 쌓고** 접수처로 넘어간다.
6. → 접수처 `dllmain_dispatch` 는 마치 **ntdll 이 자기를 직접 부른 것처럼** 선다. 일이 끝나면 곧장 ntdll (`Internal+0x22`) 로 돌아간다.
7. 접수처 `+0x91` 이 `DllMain` 을 **`CALL`** 한다. → `+0x96` 이 쌓인다.
8. → `DllMain` 안에서 `k` 를 찍으면 경비 자리는 **처음부터 없다.**

- 비유 (run 문서 그대로): 정문 경비가 출입증에 **도장(쿠키)** 만 찍고, **자기 자리를 비운 채** 접수처로 바로 들여보낸다. 그래서 나중에 "누가 데려왔나" 를 거꾸로 따라가면 경비는 안 나온다.
- run30 의 `mainCRTStartup` (현관 안내원) 과 **똑같은 모양**이다 (flow.md 2.8 도 "exe 의 `mainCRTStartup` 과 같은 모양, 2.10" 이라고 적었다).

#### 같은 일이 한 번 더 — 접수처 → 준비 담당

- 접수처 `+0x76` 이 `dllmain_crt_dispatch` 를 **`CALL`** 한다 (ret `+0x7b`).
- 그런데 `dllmain_crt_dispatch` 도 이유 1 이면 `+0x42` 에서 **꼬리 `JMP`** 로 `dllmain_crt_process_attach` 에 넘긴다.
- 그래서 준비 담당이 일을 끝내고 `+0xec RET` 하면, **곧장 접수처 `+0x7b`** 로 돌아간다 `[정적]` (결과서). `dllmain_crt_dispatch` 도 콜스택에 안 남는다.

#### `DllMain` 에서 `k` 를 찍으면 — 예상 `[정적]`

위가 가장 안쪽:

```
dllN!DllMain
dllN!dllmain_dispatch+0x96
ntdll!LdrpCallInitRoutineInternal+0x22
ntdll!LdrpCallInitRoutine+0x93
ntdll!LdrpInitializeNode+0x19c
```

- **`_DllMainCRTStartup` 은 안 나온다** (꼬리 `JMP`). **`dllmain_crt_dispatch` 도 없다** (`process_attach` 로 `JMP`).
- 이렇게 안 나오면 **꼬리 점프 가정이 틀린 것**이다 → 동적 **Q27** (run 문서).
- 반대로 경비 **입구**에서 `k` 를 찍으면 바로 위가 `LdrpCallInitRoutineInternal+0x22` 다. 아직 `JMP` 전이니까 (run28 예측 그대로).
- run8 `[관찰]` "`rcx` = `_DllMainCRTStartup`, 직후 `DllMain`" 은 **틀린 게 아니다.** 들어가는 문은 경비 자리가 맞다. 다만 **콜스택상 `DllMain` 의 부모는 `dllmain_dispatch`** 로 읽어야 한다 (flow.md 2.8 글을 그렇게 고쳤다).
- run 문서 멈춤점 메모: 경비 입구에서 `k` 한 번, `g` 로 `DllMain` 까지 가서 `k` 한 번. **두 `k` 의 차이**로 꼬리 점프를 바로 볼 수 있다.

### 2.5 핵심 ③ 봉투는 안 뜯고 그대로 — `R8` 이 가는 길

이번 바퀴의 원래 질문이다. 사용자에게 한 설명을 그대로 이어 간다.

- 로더(켜기 담당)가 시작 함수에 **봉투**(`lpReserved`)를 건넨다. 정적 import 로 붙은 dll1·dll2 라면 봉투 안엔 첫 직원의 **업무 시작 메모**(첫 스레드 CONTEXT) 를 가리키는 쪽지가 들었을 것이다 (run27·run28).
- 질문: 현관 안의 CRT 가 **봉투를 뜯어 보거나 바꿔치기하지 않고** 손님(`DllMain`)에게 그대로 넘기나?

#### 새 말 둘

- **보존 레지스터** (`RDI`, `RSI`): 호출 규약상 "**불린 함수가 쓰려면 원래 값을 지켜 놨다가 돌려줘야 하는**" 레지스터 (결과서). 그래서 함수를 하나 부르고 와도 값이 그대로 남는다.
  - 비유: 남이 잠깐 빌려 쓸 수는 있지만, 반드시 **원래 물건을 도로 넣어 두는 규칙이 있는 서랍**. 봉투를 여기 넣어 두면 중간에 누구를 만나고 와도 그대로 있다.
  - 그래서 경비는 `+0xa PUSH RDI` / `+0x37 POP RDI`, 접수처는 `+0x13 PUSH RSI` / `+0x3d POP RSI` 로 **자기도 그 규칙을 지킨다** (결과서).
- **그림자 칸** (`[RSP+0x18]`): x64 호출 규약에서 **부르는 쪽이 인자 몫으로 스택에 미리 비워 두는 자리**. 불린 함수가 레지스터 인자를 여기 받아 적을 수 있다 `[추측: 일반적으로 알려진 설명]`. 근거 문서는 "호출자 그림자 칸에 흘려 적기만 (다시 안 읽음)" 까지 적었다.
  - 비유: 책상 옆 **메모꽂이**. 봉투 사본을 꽂아 두기만 하고 다시 안 본다.

#### 봉투가 지나가는 자리 `[정적]`

run 문서의 줄 그림을 표로 옮겼다. "봉투를 손에 든 곳" 만 따라가면 된다.

| 순서 | 어디 | 하는 일 | 비유 |
|---|---|---|---|
| 1 | ntdll `Internal +0x1d` | `R8` = 봉투로 진입점을 부름 | 봉투를 건넴 |
| 2 | 경비 `+0xf` | `RDI = R8` | 서랍 ①(`RDI`)에 넣음 |
| 3 | 경비 `+0x1c` | 쿠키 함수 호출 (이유 1 일 때만) | 도장 찍으러 잠깐 다녀옴 |
| 4 | 경비 `+0x21` | `R8 = RDI` | 서랍 ①에서 꺼냄 |
| 5 | 경비 `+0x38` | `JMP` 접수처 | 그대로 들고 들여보냄 |
| 6 | 접수처 `+0x5` | `[RSP+0x18] = R8` (다시 안 읽음) | 메모꽂이에 사본만 꽂음 |
| 7 | 접수처 `+0x1b` | `RSI = R8` (`RSI` 에 쓰는 줄은 이것 하나) | 서랍 ②(`RSI`)에 넣음 |
| 8 | 접수처 `+0x6e` → `+0x76` | `R8 = RSI` → 준비 담당 쪽 호출 (ret `+0x7b`) | 준비 담당에게 **보여만** 줌 (아래) |
| 9 | 접수처 `+0x89` | `R8 = RSI` | 서랍 ②에서 꺼냄 |
| 10 | 접수처 `+0x91` | **`CALL DllMain`** (ret `+0x96`) | **손님에게 건넴** |
| 11 | `DllMain +0x0` | `[RSP+0x18] = R8` (흘려 적기만) | 손님도 메모꽂이에 꽂기만. 소스가 `reserved` 를 안 씀 `[소스]` |

- **`R8` 에 값을 쓰는 줄은 모두 "서랍에서 꺼내 되싣는 줄" 뿐이다.** 0 을 넣거나 다른 값을 넣는 줄·갈래가 **없다** `[정적]`.
- 갈래가 갈려도 `DllMain` 을 부르기 직전은 늘 `R8 = RSI` 다. 이유 0·3 도 `+0x45 JA → +0x89` 로 **같은 줄**에 온다 `[정적]` (결과서).
- 8번의 준비 담당 쪽도 봉투를 **옮겨 맡기기만** 한다: `dllmain_crt_dispatch +0x3b RDX = R8` → `process_attach +0xa RSI = RDX`. 동적 TLS 콜백이 있을 때만 그 콜백에 넘기는데, 콜백 칸 시작값이 0 이라 건너뛴다 (2.9). **값으로 무엇을 정하지는 않는다** `[정적]`.
- → **답: 봉투는 안 뜯고, 안 바꾸고, `DllMain` 에 그대로 간다** `[정적]`.

#### CRT 가 봉투를 "만져 보는" 때는 딱 하나 — 프로세스 끝 `[정적]`

- 이유 **0** (프로세스 끝) 일 때만, `dllmain_crt_dispatch +0x47 TEST R8,R8` / `+0x4a SETNZ CL` 로 **"봉투가 0 이 아닌가"** 를 본다.
- 그 결과(0 또는 1)가 `dllmain_crt_process_detach` 를 거쳐 `+0x68 __scrt_uninitialize_crt(ECX = 그 값, EDX = 0)` 로 간다.
  - 이름으로는 "끝나는 중인가(`is_terminating`)" 자리로 읽힌다 `[추측]` (결과서). 안쪽에서 무엇이 달라지는지는 안 따라갔다 → **Q73 (b)**.
- 실패 길 (`DllMain` 이 이유 1 에서 0 을 돌려준 경우) 의 `dllmain_dispatch +0xb2 TEST RSI,RSI` 도 같은 식이다.
- 비유: 문 닫을 때만 봉투를 **손으로 들어서 "뭐가 들었나 / 비었나"** 만 확인한다. 뜯어서 읽지는 않는다 (`TEST` 는 0 인지 아닌지만 본다).
- 이유 1 (지금 우리 길) 에서는 이것도 안 한다.

#### 그래서 남은 질문은 하나 — 봉투 안에 정말 CONTEXT 가 들었나

- 코드로는 "ntdll 이 건넨 그 값이 `DllMain` 까지 간다" 까지 닫혔다.
- 그 값이 **실제로** 첫 스레드 CONTEXT 포인터인지, 0 인지는 실행 때 값이다 → 동적 **Q27**.
  - dll1·dll2 명부 줄에 `ProcessStaticImport` 비트(봉투에 쪽지를 넣으라는 **스티커**) 가 정말 붙었는지도 Q27 이다. 이 스티커를 **붙이는 곳**은 같은 바퀴에 문서로 정리했다 (2.10).

### 2.6 핵심 ④ 손님은 준비가 끝난 뒤 — run7 `[확인 필요]` 판정

#### 순서 `[정적]`

접수처 `dllmain_dispatch` 가 이유 1 일 때:

1. `+0x3f ~ +0x45` — 이유가 0·3 이면 `+0x89` 로 바로 뛴다. 이유 1·2 는 아래로.
2. `+0x47 ~ +0x53` — `_pRawDllMain` 칸을 읽는다. 값이 0 이라 부르지 않고 건너뛴다 (2.9).
3. **`+0x76` (ret `+0x7b`) — 준비 담당 쪽 호출.** `process_attach` 가 CRT 준비를 다 하고 `EAX = 1` 로 돌아온다.
4. `+0x81 ~ +0x83` — 준비가 실패(`EAX == 0`)면 `+0x11e` 로 가서 **`DllMain` 을 안 부르고** 0 을 돌려준다.
5. **`+0x91` (ret `+0x96`) — `DllMain`.**

- → `DllMain` 은 CRT 준비 호출이 **끝나고 돌아온 뒤**다. run7 `[확인 필요]` "DLL 시작 함수가 CRT 준비를 먼저 하고, 그다음 DllMain" 은 **코드로 맞다** `[정적]`.
- 한 가지만 고쳐 읽는다: 준비를 하는 건 경비(`_DllMainCRTStartup`) **자기 몸**이 아니라, 접수처가 부르는 준비 담당 몫이다.
- 비유: 접수처가 먼저 **사무실 준비**를 다 시킨 뒤(3), 준비가 안 됐으면 손님을 돌려보내고(4), 됐으면 손님을 안으로 모신다(5).

#### `LoadLibraryExW` ×3 도 준비 안쪽이다 — 단계별로

1. `[관찰]` (run7) `LoadLibraryExW` 3번은 `DllMain` **앞**이었다.
2. `[정적]` 지도를 `dllmain_crt_process_attach` 뿌리로 돌렸더니, `try_get_function_slow` 로 가는 직접 호출 길이 **모두 이 함수 아래**였다.
   - 지도가 잡은 길 둘: `AreFileApisANSI` 길, `LCMapStringEx` 길. 둘 다 `process_attach +0x74` → `__scrt_dllmain_after_initialize_c +0x20` → `_configure_narrow_argv` 에서 갈라진다 (run 문서).
3. `[정적]` `process_attach` 는 접수처 `+0x76` 에서 들어가니, `+0x91 DllMain` 보다 앞이다.
4. → 세 번이 **CRT 준비 과정**이라는 flow.md 2.8 의 `[추측]` 은 코드로 맞다 `[정적]`.
5. 아직 확인 아님:
   - test.exe 의 셋째 길 `FlsGetValue2` (run30 길 A) 는 **함수 포인터 표**를 거쳐서 지도가 못 따라간다. dll 에서도 같은 길인지는 안 봤다 → **Q73 (a)**. 한 상자 원칙이라 동적 Q31 의 이름 셋으로 갈음할 수도 있다.
   - 세 번이 정말 그 세 길인지, 무엇을 올리는지 → 동적 **Q31**.

#### 덤 — 이유 0·3 은 순서가 반대 `[정적]`

- 이유 0·3 (끝나는 쪽) 은 `DllMain` (`+0x91`) **먼저**, 그 뒤 `+0xe8 dllmain_crt_dispatch`.
- 비유: 개업 땐 **사무실 준비 → 손님**, 폐업 땐 **손님 배웅 → 사무실 정리**. 생각해 보면 자연스러운 순서다.

### 2.7 핵심 ⑤ run7 의 두 주소 — dll2 인가, dll1 인가

run7 에는 수수께끼 두 줄이 있었다 `[관찰]`.

- 7번: `KERNELBASE!LoadLibraryExW` ← `0x7ffd80e2f5fb` — "모듈 미확인"
- 9번: `KERNELBASE!LoadLibraryExW` ← `0x7ffd80ebf60b` — "dll2 주소 범위 안" (run6 때 dll2 범위)

run7 은 dll 주소가 빗나간 run 이다. dll 베이스가 **실행마다 바뀌어서** (run8: 재부팅 없이도 run6 과 달라짐), run6 주소로 건 멈춤점이 엉뚱한 dll 에 걸렸다 (run7 정정 노트).

#### 열쇠 — 아래 16비트는 안 변한다

- 전제 (run 문서): dll 은 **64KB(`0x10000`) 단위**로 올라간다. 그러면 베이스의 마지막 네 자리는 늘 `0000` 이다.
- 그래서 **실제 주소의 마지막 네 자리 = rva 의 마지막 네 자리.** 베이스가 바뀌어도 이건 그대로다.
- 비유 (run30 해설 2.7 그대로): 아파트 **동 번호**(베이스)는 이사할 때마다 바뀌어도 **호수 끝 네 자리**(아래 16비트)는 그대로다.
- run7 의 두 주소 → **`f5fb`**, **`f60b`**.

#### 맞추기 — 단계별로 `[정적]`

1. 각 dll 에서 `LoadLibraryExW` 콘센트 칸 (dll1 rva `0x6e1f8`, dll2 `0x6e1e8`) 을 부르는 `call` 을 **전부** 찾았다. 모듈마다 **9곳**. (+ 꼬리 `JMP` 1곳은 돌아올 자리가 없어서 뺀다.) 결과서는 `.text` 의 `ff 15` 바이트까지 훑어 Ghidra 결과와 맞췄다 (같음).
2. 그 9곳의 ret rva (결과서 표):

| 모듈 | ret rva |
|---|---|
| dll1 | `0x4f4ba` `0x4f50d` **`0x4f60b`** `0x4f65f` `0x4f7a7` `0x4f7f7` `0x4f8b6` `0x4f909` `0x4f98b` |
| dll2 | `0x4f4aa` `0x4f4fd` **`0x4f5fb`** `0x4f64f` `0x4f797` `0x4f7e7` `0x4f8a6` `0x4f8f9` `0x4f97b` |

3. 끝 네 자리 `f5fb` 는 **dll2 에만** (`0x4f5fb`), `f60b` 는 **dll1 에만** (`0x4f60b`) 있다.
4. 둘 다 `try_get_function_slow +0x73` 이다 (dll1 시작 `0x4f598`, dll2 시작 `0x4f588`).
5. → **7번 `…f5fb` = dll2, 9번 `…f60b` = dll1** `[정적]` + `[관찰]`.

- 왜 같은 함수 `+0x73` 인데 끝자리가 다른가: 2.1 에서 본 **`0x10` 차이** 때문이다. dll2 의 함수가 `0x10` 앞에서 시작하니, 같은 `+0x73` 도 `0x10` 앞이 된다 (결과서).
- 내가 덧붙이는 생각 `[추측]`: 만약 두 dll 이 rva 까지 **완전히 같았다면** 끝 네 자리도 같아서, 이 방법으로는 못 갈랐을 것이다. `0x10` 차이가 운 좋게 이름표 노릇을 했다.

#### 함정 — "dll2 범위 안" 이 왜 틀렸나

요청서는 먼저 이걸 물었다: "dll2 가 run6 베이스 그대로라면 9번은 dll2 rva `0x4f60b` 아닌가?"

1. `[정적]` dll2 의 rva `0x4f60b` 는 `try_get_function_slow +0x83` 이다.
2. `[정적]` 그런데 그 자리는 `+0x7f` 의 `CALL [GetLastError]` (6바이트, rva `0x4f607` ~ `0x4f60c`) **한가운데**다.
3. **새 말 — 명령 경계**: 기계어 명령은 길이가 제각각이라, 명령마다 **첫 바이트**가 있다. 돌아올 주소는 늘 "`call` 명령 바로 **다음 명령의 첫 바이트**" 다. 명령 한가운데 바이트는 돌아올 주소가 **될 수 없다.**
   - 비유: 책갈피는 **줄의 시작**에 끼운다. 단어 한가운데 글자에서 "여기서부터 이어 읽어" 할 수는 없다.
4. 반대로 dll1 의 rva `0x4f60b` 는 명령 경계이고, 바로 앞이 `LoadLibraryExW` call 이다 `[정적]`.
5. → 9번은 dll2 가 **아니라 dll1** 이다. run7 이 "dll2 범위" 라고 적은 건 **run6 때** dll2 가 있던 범위였다. run7 에선 그 자리에 **dll1** 이 올라와 있었다.

- 비유: 단지에 **같은 설계도로 지은 건물 두 채**(dll1, dll2)가 있다. run7 날 메모엔 "2동 자리에서 전화 왔음" 이라고 적었는데, 그날은 **1동 건물이 지난번 2동 자리로 이사**해 있었다. 호수 끝 네 자리를 보면 1동 집이다.

#### 맞물림 셋 — 따로 구한 숫자들이 서로 맞는다

1. 9번에서 거꾸로 뺀 dll1 베이스 = `0x7ffd80ebf60b` − `0x4f60b` = **`0x7ffd80e70000`** (run 문서). 이건 run6 에서 dll2 가 있던 자리다. run7 맨 위 정정 노트 "dll1 이 run6 의 dll2 자리에 올라옴" 과 **같다**.
2. 같은 베이스로 run7 10번 `dll2!DllMain` (`ba` 를 건 run6 주소 `0x7ffd80e712e0`) 을 풀면 `0x7ffd80e70000 + 0x12e0` = **dll1 의 `DllMain`** 이다. run7 정정 노트의 "가능성이 크다" 가 한 번 더 맞는다.
3. 7번(dll2) → 9번(dll1) 순서는 run8 `[관찰]` 초기화 순서 **dll2 → dll1 → exe** 와 같다. 7번에서 구한 dll2 베이스 `0x7ffd80de0000` 과 dll1 베이스의 이미지 범위 (`0x7ffd80de0000 ~ 0x7ffd80e6c000`, `0x7ffd80e70000 ~ 0x7ffd80efc000`) 는 **겹치지 않는다**.

#### 그래서 run7 시간표 6 ~ 10번을 다시 읽으면 (flow.md 2.8 "run7 정정")

| run7 번호 | 다시 읽은 것 |
|---|---|
| 6 | dll2 시작 함수 |
| 7 | dll2 CRT 의 `LoadLibraryExW` ×3 (`…f5fb`) |
| 8 | dll1 시작 함수 |
| 9 | dll1 CRT 의 `LoadLibraryExW` ×3 (`…f60b`) |
| 10 | **dll1** `DllMain` (표엔 `dll2!DllMain`) |

- dll2 의 `DllMain` 은 run7 에서 `ba` 가 빗나가 **안 찍혔다.**
- run7 "아직 확인 필요" 의 짐작 "7번이 dll1 이라면 순서는 dll1 → dll2" 도 틀렸다 (7번 = dll2).
- 01_sprint run7 문서는 **고치지 않았다** (오케스트레이터 지시). 정정은 run40 문서와 flow.md 2.8·4장·5장에 있다.

### 2.8 참고 — 준비 담당의 순서 (`dllmain_crt_process_attach`)

여기부터 지도 밖이다. 이름과 순서만 가볍게. 전부 `[정적]`, 숫자는 ret.

| ret | 하는 일 |
|---|---|
| `+0x17` | `__scrt_initialize_crt` — 실패면 `+0x19` 에서 0 반환 |
| `+0x24` | 시작 잠금 |
| `+0x4a` · `+0x53` · `+0x58` · `+0x5d` | C 초기화 전 준비, `_RTC_Initialize`, type_info, stdio 기본값 |
| `+0x70` | `_initterm_e` (`.CRT$XI`) |
| `+0x79` | `__scrt_dllmain_after_initialize_c` — 안에서 `_configure_narrow_argv` 등 (지도가 잡은 `LoadLibraryExW` 길 둘의 뿌리) |
| `+0x90` | `_initterm` (`.CRT$XC`) |
| `+0xa5` | 잠금 풀기 |
| `+0xaf` | `__scrt_get_dyn_tls_init_callback` — 칸 시작값 0 이라 `+0xb6` 에서 건너뜀 |
| — | `+0xd8 INC [__proc_attached]` → `+0xde EAX = 1` → `+0xec RET` |

- run30 의 exe 쪽 `__scrt_common_main_seh` 와 **뼈대가 비슷하다** (초기화 → `_initterm_e` → `_initterm` → 동적 TLS 콜백 검사). 다만 exe 는 그 뒤 `main` 을 직접 부르고, dll 은 **돌아가서 접수처가 `DllMain` 을 부른다.**
- 이미 시작한 적이 있으면 (`+0x2e`·`+0x35 JNZ +0xf9`) `+0xfe __scrt_fastfail` 로 죽는다.
- 기억해 둘 건 하나다: **`LoadLibraryExW` 전화는 이 표 안쪽, 즉 `DllMain` 보다 앞**에서 온다.

### 2.9 곁가지 — 0 인 칸 둘, 안 타는 길, 안 부르는 사본

- `[정적]` **`_pRawDllMain` 칸** (dll1 rva `0x6e3a0`, dll2 `0x6e390`): `.rdata` 읽기 전용, **파일 값 0, 쓰는 곳 0**. 그래서 `dllmain_dispatch +0x51 JNZ` 가 안 뛰고 `+0x53` 으로 건너뛴다. 접수처의 간접 호출 셋 (`+0x5a`·`+0xd1`·`+0x112`) 은 이 칸 쪽이라 우리 길에선 **안 탄다.**
- `[정적]` **동적 TLS 콜백 칸** (`__dyn_tls_init_callback`, 둘 다 rva `0x84078`): 시작값 0. `process_attach +0xb6` 에서 건너뛴다. run30 의 test.exe 와 같다 ([run30 해설 2.10](run30-exe-crt-start-해설.md)).
- `[정적]` + `[소스]` **실패 길**: `DllMain` 이 이유 1 에서 0 을 돌려주면 `+0xad` 에서 이유 0 으로 한 번 더 부르고 `+0xb8 dllmain_crt_process_detach`. 우리 `DllMain` 은 늘 `TRUE` 라 **안 탄다** `[소스]`.
- `[정적]` **`dllmain_raw`** (dll1 `0x1ce0`): 부르는 곳이 없는 사본이다. 본체가 `dllmain_dispatch` 안에 합쳐졌다. run30 의 `invoke_main` 처럼 **이름만 남은 사본**이다.
- `[정적]` **dll1 과 dll2 는 같은 코드다.** 진입점 쪽 함수 아홉 개를 절대 주소만 지우고 비교하면 머리 줄 말고 같다. 데이터 칸만 `0x10` 차이 (`module_names` `0x708b0` ↔ `0x708a0` 등). 단 `__dyn_tls_init_callback` 칸은 둘 다 `0x84078`.

### 2.10 같은 바퀴에 닫은 것 — Q54 (새 run 없이, 문서로)

이번 바퀴와 직접 이어지는 얘기라 짧게 같이 본다.

#### Q54 가 뭐였나

[run27 해설 2.8](run27-init-graph-해설.md#28-진입점-넷째-인자--벽에-붙여-둔-context-의-쓰임) 에서 남긴 질문 둘:

1. 명부 줄 `Flags.ProcessStaticImport` (0x20) — **봉투에 쪽지를 넣을지 정하는 스티커** — 를 **누가 붙이나**?
2. 연결 카드 사이 끈(간선) 칸 `+0x18` 비트 0 은 무슨 뜻인가? (순서 담당 GR 이 이 비트 붙은 끈은 건너뛴다)

#### 왜 새 바퀴 없이 닫았나

- 답이 **이미 앞 바퀴들 결과서에 있었다** (2026-10-24 사용자 결정, Q32 때와 같은 방식). 새로 분석한 게 아니라, 흩어진 근거를 flow.md 한 줄로 모았다.
- 고친 곳: flow.md **4장 2.7 "이름·뜻 못 정한 것 (run27)"** 줄, 그리고 2.8 넷째 인자 줄. run27 문서는 그대로 뒀다 (flow.md 로 충분, plan.md).

#### 정리된 답

1. 스티커 붙이는 곳 = **`LdrpAllocateModuleEntry +0x194`** (sast/17, run22) `[정적]`.
   - 조건: `LdrInitState == 1` 이고, 지금 스레드가 `LdrpDllNotificationLock` 주인이 **아닐** 때 `Flags |= 0x20`.
   - `LdrInitState == 1` 은 [run19 해설](run19-proc-init-modules-해설.md) 의 `+0x1a4c` (1 로) ~ `+0x1b1e` (2 로) 사이, 즉 **exe 의 import 를 올리고 연결하는 동안**이다 `[정적]` (run19).
   - 비유 ([run22 해설 2.7](run22-map-snap-dep-해설.md) 의 "명부 줄 만들기"): 명부에 새 건물 줄을 쓸 때, **개업 날 처음부터 같이 들이는 중**이고 특정 열쇠를 쥔 사람이 지금 직원이 아니면, 그 줄에 "개업 동기" 스티커를 붙인다.
   - dll1·dll2 줄이 **정말 그 조건에서** 만들어졌는지는 실행 값이다 → **Q27**.
2. 끈 칸 `+0x18` 비트 0 = **대비용으로 할당한 끈 칸**에서 켠다 (`LdrpLoadDependentModuleInternal +0x17d9`·`LdrpRecordModuleDependency +0x11a`), 보통 칸은 `+0xede` 에서 지운다 (sast/18·22) `[정적]`.
   - `LdrpRecordModuleDependency` 를 부르는 곳은 지도상 **핫패치 갈래 하나** (`LdrpInitializeNtdllDataTableEntry` call `+0x23e` / ret `+0x243`, 패치 ntdll 이 있을 때만).
   - → test.exe 에선 GR 이 건너뛸 끈이 **없을 것** `[추측]`.

#### 남은 것 → Q72 (정적)

- `0x20` 을 켜는 곳이 정말 **하나뿐인가** (전수), 조건 칸의 뜻.
- 비트 0 을 켜고·읽는 곳 전부, `LdrpRecordModuleDependency` caller 가 정말 하나인가.
- 대표 지점 목표엔 덜 중요해서 맨 뒤로 갔다.

#### 이번 바퀴와 어떻게 이어지나

봉투 이야기를 한 줄로 이으면:

1. 명부 줄에 **스티커**(`ProcessStaticImport`) 를 붙인다 — `LdrpAllocateModuleEntry +0x194` `[정적]` (Q54)
2. 켜기 담당이 스티커를 보고 봉투에 **쪽지**(CONTEXT 포인터) 를 넣거나 빈 봉투(0) 를 만든다 — `LdrpInitializeNode +0x17a` `[정적]` (run27)
3. 한 칸 밀어 시작 함수에 건넨다 — `LdrpCallInitRoutineInternal +0x1d` `[정적]` (run28)
4. CRT 는 봉투를 **안 뜯고** `DllMain` 에 건넨다 — `dllmain_dispatch +0x91` `[정적]` (이번)
5. 손님(`DllMain`)도 안 뜯는다 `[소스]`.

- **노선도로는 봉투의 길이 처음부터 끝까지 이어졌다.** 남은 건 실제로 버스를 타서 봉투 안을 보는 것뿐이다 (Q27).

### 2.11 정리 — 비유로 한 번 더

- 켜기 담당(로더)이 dll 건물 현관에서 **봉투**를 건넨다. 개업 동기 스티커가 붙은 건물이면 봉투 안엔 첫 직원 업무 시작 메모를 가리키는 쪽지가 들었다.
- **정문 경비**(`_DllMainCRTStartup`)는 봉투를 서랍에 잠깐 넣고, 출입증에 **도장**(쿠키)을 찍고, 봉투를 다시 꺼내 쥔 채 **자기 자리를 비우고** 접수처로 들여보낸다. 그래서 기록엔 경비가 안 남는다.
- **접수처**(`dllmain_dispatch`)는 봉투를 자기 서랍에 넣고, **준비 담당**에게 사무실 준비를 시킨다. 준비 담당은 연장이 필요하면 **관리실에 전화**(`LoadLibraryExW` ×3) 한다 — 이 전화만 지도에 그린다.
- 준비가 끝나면 접수처는 서랍에서 봉투를 꺼내 **그대로** 손님(`DllMain`)에게 건넨다. 아무도 봉투를 뜯지 않았다.
- dll1·dll2 두 건물은 **같은 설계도**다. 방 번호만 `0x10` 씩 밀려 있어서, run7 날의 전화가 어느 건물에서 왔는지 호수 끝자리로 가려낼 수 있었다.

### 2.12 곁가지 — 도구 메모

run 문서 "디버거 메모 / 도구 메모" 를 옮긴다. 분석 결과엔 영향이 없다.

- 고칠 버그는 없다.
- `_guard_dispatch_icall_nop.asm` 이 머리 한 줄뿐이다. Ghidra 가 2바이트 `JMP RAX` 를 크기 1 함수로 잡아서다. 읽는 데는 문제없다.
- 지도(`map`)는 여전히 **함수 포인터 표**를 못 따라간다 (run30 과 같음, 하네스 후보 37). 그래서 `FlsGetValue2` 길을 못 봤다 (Q73 a).
- `_calls.tsv` 에서 `DllMain +0x16` 의 callee 가 `alias:ReasonName+1` 로 찍힌다. 같은 주소의 다른 이름이 구역 이름 `.text$mn` 이라서다. `ReasonName` 은 `sample/log.h` 의 함수다 `[소스]`.
- dll 주소는 실행마다 바뀌니, 멈춤점은 고정 주소 `ba` 대신 **`lm` 으로 그때 베이스를 얻어 `dllN+rva`** 로 건다 (run8 방법). run7 처럼 빗나가지 않게.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 어디 | 전 | 후 |
|---|---|---|
| 앞머리 기준 | — | dll1.pdb·dll2.pdb 서명 줄, "정적 근거 run9 ~ **run40** … run40 dll1·dll2 (sample 빌드)", dll 도 `sample/bin/` 파일을 썼다는 줄 (같은 빌드 단정은 `[추측]`) |
| 그림 2 dll 상자 `f1`·`i1` | `dll2!_DllMainCRTStartup` (실선) | **CRT 경계 상자** (점선): "dll2 진입점 = PE AddressOfEntryPoint (rva `0x1d50`, `dll2!_DllMainCRTStartup`) — 여기부터 컴파일러 CRT (OS 아님, 정적 run40) … 꼬리 JMP 라 k 엔 안 남음 … `dllmain_dispatch +0x96` 자리에서 DllMain, lpReserved 그대로" (dll1 은 rva `0x1d60`) |
| 그림 2 KERNELBASE | `m2` "LoadLibraryExW ×3" 하나 (test.exe 상자에서만) | **`m4`** (dll2 CRT, run7 7번 `…f5fb`), **`m5`** (dll1 CRT, run7 9번 `…f60b`), `m2` (test.exe CRT, run7 13번) — 셋으로 나눔 |
| 그림 2 화살표 | `f1 -?-> f2`, `i1 -?-> i2` | `f1 -(1)?-> m4`, `f1 -(2)?-> f2` / `i1 -(1)?-> m5`, `m5 -.-> i1`, `i1 -(2)?-> i2` |
| "두 그림 사이 설명" | — | run40 문단 새로: 경계 상자로 바꾼 이유, 화살표가 `?` 인 이유, 돌아옴 점선이 dll1 쪽에만 있는 이유 |
| 2.7 진입점 인자 줄 | "`_DllMainCRTStartup` → `DllMain` 은 CRT 안이라 아직 안 봄 — 정적 Q55" | + "→ run40 `[정적]`: CRT 가 셋째 인자를 바꾸지 않고 `dllmain_dispatch +0x91` 에서 `DllMain` 에 넘긴다" |
| 2.8 머리 그림 | `… → dllN!_DllMainCRTStartup → dllN!DllMain(1)` | `… → dllN!_DllMainCRTStartup ═JMP▶ [CRT] → dllN!DllMain(1)` |
| 2.8 `[추측]` "사이에 다른 함수가 있는지는 모른다" | `[추측]` | → **사이에 하나 있다** `[정적]`: 꼬리 `JMP` → `dllmain_dispatch` → `+0x91` (ret `+0x96`) `DllMain`. 콜스택 부모 = `dllmain_dispatch`. 실행은 Q27 |
| 2.8 넷째 인자 줄 | 켜는 곳 안 봄, "그대로 넘기는지는 정적 Q55" | 켜는 곳 = `LdrpAllocateModuleEntry +0x194` (Q54 문서로 닫음), "Q55" 부분 **취소선** → **(해결됨, 정적) 그대로 넘긴다** (`RDI`·`RSI` 경로). 실제 값은 Q27 |
| 2.8 `[추측]` "CRT 준비 뒤 `DllMain`, ×3 도 CRT 준비" | `[추측]` | → **코드로 맞다** `[정적]` (`+0x76` 준비 → `+0x91` `DllMain`) |
| 2.8 "어느 DLL 주소였는지 확정 못 함" | 그대로 | **취소선** → 7번 = dll2 (rva `0x4f5fb`), 9번 = dll1 (rva `0x4f60b`) |
| 2.8 "DLL 도 같은 UCRT 함수일 것 `[추측]`" | `[추측]` | **취소선** → **같은 UCRT 함수다** `[정적]` |
| 2.8 새 절 둘 | — | **"run7 정정"** (6 ~ 10번 다시 읽기), **"CRT 안쪽 (참고 — 지도 범위 밖)"** |
| 4장 공백 표 | 2.7 Q54 줄 열림 | 2.7 Q54 줄 **해결됨** (남은 것 Q72). 2.8 새 줄 다섯: 해결됨 둘 (사슬·`lpReserved`, run7 두 주소), Q27 출처, Q31 출처, **Q73** |
| 4.1 표 | `_DllMainCRTStartup` → `DllMain` | 진입점 상자 → (2) `?` `DllMain` (사이에 `dllmain_dispatch`), 진입점 상자 → (1) `?` `LoadLibraryExW ×3` 줄 새로 |
| 5장 | run7 "일부 정정" | run7 줄에 run40 정정, run40 줄, 요청서 표에 sast/35 |

- 태그 변화 한 줄 요약: 2.8 의 `[추측]` **세 줄**이 `[정적]` 판정을 받았다 (사이 함수, 준비 순서, 같은 UCRT 함수). run7 의 "모듈 미확인" 과 "dll2 범위" 는 **갈렸다/정정됐다.**
- 그림의 화살표는 **여전히 `?`** 다. 실행은 안 봤으니까.

### 그림 2 — 진입점 상자가 점선이 되고, `LoadLibraryExW` 노드가 생긴 까닭

사용자가 그림을 볼 때 가장 먼저 눈에 띌 변화라 단계별로 푼다.

#### 왜 점선이 됐나

1. 2026-10-23 결정 **"진입점 = 경계, CRT 한 상자"** 는 원래 test.exe 때 정했다 ([run30 해설 2.1](run30-exe-crt-start-해설.md)). 진입점은 헤더에 적힌 주소일 뿐이고, 그 안은 컴파일러 코드라 OS 지도 밖이라는 결정이다.
2. 그때 그림 2 의 test.exe 상자 `m1` 이 **점선 경계 상자**가 됐다.
3. dll 쪽은 그때 바이너리를 안 열어서, 상자 `f1`·`i1` 이 그냥 `dllN!_DllMainCRTStartup` (실선) 로 남아 있었다.
4. 이번에 열어 보니 **dll 진입점도 똑같이** PE `AddressOfEntryPoint` 가 가리키는 CRT 코드였다 (rva `0x1d50`/`0x1d60` = `_DllMainCRTStartup`) `[정적]`.
5. → 같은 결정을 dll 에도 그대로 써서, 두 상자를 `m1` 과 **같은 모양의 점선 경계 상자**로 바꿨다. 점선 = "**여기부터 OS 가 아니라 컴파일러 CRT**" 라는 표시다.
6. 그래서 `dllmain_dispatch`·`process_attach` 같은 CRT 함수는 **노드로 늘리지 않았다.** 상자 글과 flow.md 2.8 "CRT 안쪽 (참고)" 에만 적었다.

- 비유: 배달 지도에서 각 집의 **현관문**만 점선으로 그려 둔다. 현관 안 가구 배치는 지도에 안 그린다.

#### 왜 `LoadLibraryExW` 노드가 dll 마다 생겼나

1. 같은 결정에 예외가 하나 있다: CRT 가 **ntdll 로더를 다시 부르는 것**(`LoadLibraryExW`)은 OS 쪽 일이라 **화살표로 남긴다** (run30 해설 2.1).
2. test.exe 는 run30 에서 `…67723` 이 test.exe 의 `try_get_function_slow +0x73` 으로 갈려서, `m1 → m2` 화살표가 이미 있었다.
3. dll 쪽은 run7 `[관찰]` 에 ×3 이 **두 번** 찍혀 있었지만, 그 두 주소가 **어느 dll 것인지 몰랐다** ("모듈 미확인", "dll2 범위" 는 틀린 말). 어느 상자에서 화살표를 뽑아야 할지 모르니 그릴 수 없었다.
4. 이번에 **7번 = dll2, 9번 = dll1** 로 갈렸다 (2.7).
5. → 그래서 dll2 상자에서 `m4`, dll1 상자에서 `m5` 로 가는 **`(1) ?`** 화살표를 하나씩 넣었다. `DllMain` 은 그 뒤라서 **`(2) ?`** 로 번호가 붙었다 (2.6 의 순서 그대로).
6. 노드를 `m2` 하나로 합치지 않고 셋(`m4`·`m5`·`m2`)으로 나눈 건, 상자 글에 **run7 몇 번 주소인지**를 따로 적기 위해서다.

#### 화살표가 왜 아직 `?` 인가

- 현관 상자와 `DllMain`·`LoadLibraryExW` 사이에 CRT 함수가 **여럿 끼어 있다** (경비 → 접수처 → 준비 담당 → …).
- 그리고 그 사이를 **실행으로 본 적이 없다.** 콜스택으로 "누가 불렀나" 를 확인한 적이 없으니 `?` 다.

#### 돌아옴 점선이 왜 dll1 쪽에만 있나 (`m5 -.-> i1`)

- run7 에서 9번(dll1 ×3) 뒤에 **dll1 `DllMain`** 이 실제로 찍혔다 (표 10번).
- dll2 쪽은 7번(×3) 뒤 dll2 `DllMain` 이 `ba` 가 빗나가 **안 찍혔다.** 그래서 "돌아와서 그다음으로 갔다" 를 관찰로 말할 근거가 없어 점선을 안 그렸다.

### 큰 그림에서 어떻게 이어지나

- 이번 바퀴로 **로더가 DLL 전원을 켜는 길이 현관까지, 그리고 현관 안 첫 손님(`DllMain`) 까지 노선도로 이어졌다.**
  - run27 (켜기 순서) → run28 (한 칸 밀기, 봉투 건네기) → **run40 (봉투가 `DllMain` 까지)**.
  - 그리고 Q54 정리로 봉투에 쪽지를 넣을지 정하는 **스티커**까지 이어졌다 (2.10).
- exe 쪽(run30)과 dll 쪽(이번)이 **같은 모양**이라는 게 보였다: 둘 다 진입점은 쿠키 + 꼬리 `JMP` 만 하고, 진짜 일은 그다음 CRT 함수가 한다. `LoadLibraryExW` ×3 도 둘 다 `try_get_function_slow +0x73`.
- 남은 건 대부분 **버스 타기**(동적)다. 들고 갈 이번 바퀴 예측:
  - `DllMain` (`dllN+0x12e0`) 에서 `k`: 부모가 `dllN!dllmain_dispatch+0x96`, `_DllMainCRTStartup` 없음 (Q27).
  - 경비 입구 → 접수처 입구 → `DllMain` call → `DllMain` 입구, 네 곳의 `r8` 이 **모두 같고** ntdll 이 넘긴 `r9` 원본과 같은가 (Q27).
  - `KERNELBASE!LoadLibraryExW` 입구에서 돌아올 주소가 `dll2+0x4f5fb` → `dll1+0x4f60b` 순서인가, 이름 셋 (Q31).
- 대기열 다음 항목은 **Q56** (2.7 TLS 락 세부, 작은 정적). 단계 3 "작은 정적부터 몰아서" 순서다 (plan.md).

---

## 4. 아직 모르는 것

dll 베이스는 실행마다 바뀐다. 그래서 `lm` 으로 그때 베이스를 얻어 `dllN+rva` 로 건다. 유저 주소라 **test.exe 조건 필수**다 (run 문서).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 봉투(`r8`) 안에 **실제로** 첫 스레드 CONTEXT 포인터가 들었나 (ntdll 이 넘긴 `r9` 원본 `poi(ntdll!LdrpProcessInitContextRecord)` 와 같은가) | 실행 때 값. 코드로는 "같은 값이 간다" 까지 | 동적 **Q27** — `dll1+0x1d60`/`dll2+0x1d50` (경비 입구), `dll1+0x1ba0`/`dll2+0x1b90` (접수처 입구), `dll1+0x1c31`/`dll2+0x1c21` (`DllMain` call), `dll1+0x12e0`/`dll2+0x12e0` (`DllMain` 입구) 의 `r8` |
| `DllMain` 의 콜스택이 정말 `dllmain_dispatch+0x96` → `Internal+0x22` 이고 경비가 없나 | 실행 때 | 동적 **Q27** — `DllMain` 입구 `k`, 돌아온 직후 `dll1+0x1c36`/`dll2+0x1c26` 의 `eax` = 1 |
| dll1·dll2 명부 줄에 `ProcessStaticImport` 스티커가 정말 붙었나 | 붙이는 조건(`LdrInitState == 1`, 열쇠 주인 아님)은 실행 때 값 | 동적 **Q27** |
| CRT 준비 끝(`dll1+0x1c1b`/`dll2+0x1c0b`) 이 `LoadLibraryExW` ×3 **뒤**인가, 세 번이 무엇을 올리나 (이름), 순서가 dll2 → dll1 인가 | 실행 때. 무엇을 올리는지는 api-set 이 어디로 풀리나에 달림 | 동적 **Q31** — `KERNELBASE!LoadLibraryExW` 입구에서 `poi(@rsp)` 가 `dll2+0x4f5fb` / `dll1+0x4f60b` 인 것만 세고 `du @rcx` |
| `FlsGetValue2` 길이 dll 에서도 `try_get_function_slow` 로 가나 | 함수 포인터 표를 거쳐서 지도가 못 따라감 (한 상자 원칙) | 정적 **Q73 (a)** — 또는 Q31 이름으로 갈음 |
| `__scrt_uninitialize_crt(ECX = (lpReserved ≠ 0), EDX = 0)` 안에서 무엇이 달라지나 (프로세스 끝) | 안쪽을 안 읽음. 진입점 도착 목표엔 덜 중요 | 정적 **Q73 (b)** |
| 이유 2·3 (스레드) 길 `__scrt_dllmain_crt_thread_attach/detach` 안쪽 | 범위 밖 | 정적 **Q73 (c)** |
| `ProcessStaticImport` 를 켜는 곳이 하나뿐인가, 끈 칸 비트 0 의 켜고·읽는 곳 전부 | Q54 를 문서로 닫으며 전수는 안 봄 | 정적 **Q72** |
| 동적 TLS 콜백 칸이 실행 때도 0 인가 | 파일 시작값만 봄 | 동적 **Q27** (선택) — `dq dllN+0x84078 L1` |
| VM 에서 돈 dll 이 이 빌드와 **같은 빌드**인가 | 여러 값이 맞지만 01_sprint 기록만으로 단정 못 함 `[추측]`. dll 도 빌드가 둘이었는지 기록 없음 | 번호 없음 (run 문서) |
| WinDbg 가 CRT 함수를 PDB 이름(`dllmain_dispatch` 등)으로 보여 주나 | 디버거 VM 심볼 경로에 dll pdb 가 있어야 함 `[추측]` | 하네스 후보 (39) (plan.md) — Q27·Q31 전에 |

---

## 5. 더 읽기

- run 문서: [`run40-dll-crt-entry.md`](run40-dll-crt-entry.md) — 맨 위 "지도 범위", 환경(빌드 대조), 호출 지점 표, `R8` 줄 그림, 호출 관계·`k` 예상, run7 두 주소 표, 그림 반영, 2.8 `[추측]` 판정, 정정, 동적 확인거리 표, "다른 항목 정리" (Q54)
- 정적 결과서: [`_workspace/sast/35_dll-crt-entry_result.md`](../../_workspace/sast/35_dll-crt-entry_result.md) — 질문 1 ~ 4 답, 사슬 줄 그림 원본, ret rva 9개 전체 표, 함정(`0x4f60b` 가 명령 한가운데), 지도가 잡은 두 길 전체, 새 정적 공백
- 요청서: [`_workspace/sast/35_dll-crt-entry.md`](../../_workspace/sast/35_dll-crt-entry.md)
- flow.md: [`flow.md`](flow.md)
  - 그림 2 dll 상자 (`f1`·`i1` 경계 상자, `m4`·`m5`), "두 그림 사이 설명" 의 run40 문단
  - **2.8** — 머리 그림, `_DllMainCRTStartup` → `DllMain` 절의 판정들, **"run7 정정"**, **"CRT 안쪽 (참고 — 지도 범위 밖)"**
  - 2.7 진입점 인자 줄, 4장 공백 표 2.7 (Q54) · 2.8 줄들, 4.1 그림 2 표, 5장 run7·run40 줄
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 앞 해설:
  - [run30 해설](run30-exe-crt-start-해설.md) — 2.1 지도 범위 결정, 2.5 exe 쪽 꼬리 `JMP`, 2.7 아래 16비트 맞추기, 2.8 `try_get_function_slow`
  - [run28 해설](run28-tls-init-call-해설.md) — 한 칸 밀기, 2.9 교환원, 4절의 Q55 (이번에 끝)
  - [run27 해설](run27-init-graph-해설.md) — 2.8 진입점 넷째 인자와 `lpReserved` (메모 = 이번의 봉투)
  - [run22 해설](run22-map-snap-dep-해설.md) — 2.7 `LdrpAllocateModuleEntry` (스티커 붙이는 곳, Q54)
  - [run17 해설 2.4](run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리) — 꼬리 점프
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q55 (끝), Q54 (끝, 문서로), Q27·Q31 (출처 더함), Q72·Q73 (새), Q56 (다음), 결정 기록 2026-10-24

# run30 exe-crt-start — 해설

> 이 글은 [run30 문서](run30-exe-crt-start.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q18** — 2.10 `KERNEL32!BaseThreadInitThunk` callee (`_workspace/plan.md`, 이번에 **끝**)
  - 범위가 바퀴 처음에 한 번 바뀌었다. kernel32 쪽은 run25·run29 로 이미 답했으니, 남은 "`BaseThreadInitThunk +0x12` (ret `+0x17`) 가 부르는 대상 → `main`" 을 **test.exe 정적**으로 보기로 했다 (사용자 결정, 2026-10-23).
  - 바퀴 **도중에** 한 번 더 결정이 있었다: **"진입점 = 경계, CRT 한 상자"** (2.1 에서 자세히).
  - 새 항목: **Q59** (정적) — kernel32 `BasepInitializeTermsrvFpns` 안쪽. Q18 범위를 바꾸면서 빠진 것이다.
  - 출처를 더한 항목: **Q55** (dll 쪽 `LoadLibraryExW` ×3), **Q30·Q31** (좁힘).
  - 다음 항목: **Q19** (단계 3). 단, plan.md 는 "단계 2 처음 항목 끝 → 멈춤, 사용자 '다음' 기다림" 이라고 적어 두었다.
- 근거:
  - run 문서 [`run30-exe-crt-start.md`](run30-exe-crt-start.md) (가장 중요)
  - 요청서 [`_workspace/sast/25_exe-crt-start.md`](../../_workspace/sast/25_exe-crt-start.md)
  - 결과서 [`_workspace/sast/25_exe-crt-start_result.md`](../../_workspace/sast/25_exe-crt-start_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - plan.md 결정 기록 2026-10-23 (지도 범위)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 이번에 연 바이너리는 **MS 것이 아니라 우리 test.exe** 다. 사용자가 VM 에서 실제로 돌린 빌드를 `sample/bin/` 에 그대로 올렸다.
  - pdb 도 로컬 `sample/bin/test.pdb` 다. 서명 `C66E7084C5CA4874B1677F6F1C36A0511` (age 1) 이 exe 쪽 서명과 글자까지 같다.
  - 이미지 베이스 `0x140000000`, 진입점 rva `0x1d40`. Ghidra 가 잡은 함수가 **3356개**나 된다. 이유는 2.4 에서.
  - dll1·dll2 는 손대지 않았다 (Q55 몫).

이 글은 [run29 해설](run29-continue-start-해설.md) 의 **뒷이야기**이자, **단계 2 의 마지막 편**이다.

- run29 는 직원이 **본업 자리**(`RtlUserThreadStart`)에 도착해서 **출근 인사 담당**(`BaseThreadInitThunk`)을 부르는 데까지 봤다.
- 이번엔 출근 인사 담당이 부르는 **그다음 자리** — test.exe 의 진입점 — 와 그 안쪽을 본다.
- 그런데 이번 바퀴의 진짜 주인공은 함수 하나가 아니라 **"지도를 어디까지 그릴 것인가"** 라는 결정이다. 그래서 그 얘기부터 한다 (2.1).

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출 → [run9 해설](run9-create-user-process-해설.md) 2절
- rva, PEB (**안내판**) → [run10 해설](run10-allocate-process-해설.md) 2절
- PE 진입점 `AddressOfEntryPoint` (**설계도 첫 장의 "전원 스위치는 몇 호"**) → [run25 해설 2.3](run25-dll-entry-해설.md#새-말--pe-진입점-addressofentrypoint)
- 커널이 진입점을 `Rcx` 에 적어 주는 곳 (**업무 시작 메모**) → [run16 해설 2.4](run16-thunk-context-해설.md#24-장면--처음-메모--pspcreateusercontext-가-채우는-값)
- 꼬리 점프 (콜스택에 안 쌓이는 넘어가기) → [run17 해설 2.4](run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리)
- IAT (**벽 콘센트**) → [run19 해설](run19-proc-init-modules-해설.md) 2.7
- api-set (**별명**) → [run23 해설 2.5](run23-load-dependent-해설.md#25-핵심--apiset--가상-이름을-진짜-이름으로-바꾸는-자리)
- `BaseThreadInitThunk` (**출근 인사 담당**), `+0x17` 자리 → [run25 해설 2.6](run25-dll-entry-해설.md#26-핵심--basethreadinitthunk-는-kernel32-안-진짜-코드다)
- CFG 중개 (**교환원**, 통화 기록에 이름을 안 남김) → [run28 해설 2.9](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)
- TLS 콜백 명단은 0 까지 돈다 → [run28 해설 2.4](run28-tls-init-call-해설.md#24-핵심--명단-한-칸--창구-한-번-0-까지)
- `main` 에서 `k` 예측 (이번에 한 줄 정정됨) → [run29 해설 2.14](run29-continue-start-해설.md#214-main-에서-콜스택을-찍으면--예측과-01_sprint-관찰)

비유도 그대로 이어 쓴다.

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**
- 프로세스 = **가게**, 스레드 = **직원**, PEB = **안내판**
- `RtlUserThreadStart` = **본업 자리**, `BaseThreadInitThunk` = kernel32 의 **출근 인사 담당**, CFG 중개 = **교환원**
- DLL = **건물**, IAT = 벽 **콘센트**, api-set = **별명**
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- **이번에 더하는 비유 하나** — 지도 범위를 말할 때 쓴다:
  - 우리 지도(flow.md) = **배달 기사가 현관까지 오는 길**
  - exe 진입점 = **현관**
  - CRT = **현관 안에서 짐 푸는 법**. 이건 **집마다 다르다.**

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 다섯**:

1. **지도의 경계가 정해졌다.** exe 진입점은 PE 헤더에 적힌 값일 뿐이고, 커널은 그 값을 `Rcx` 로 넘길 뿐이다. 그 안(CRT)은 컴파일러가 넣은 코드라 **지도 밖**이다. 지도엔 CRT 가 ntdll 로더를 **다시 부르는** `LoadLibraryExW` ×3 과 `main` 만 남긴다 (사용자 결정) → 2.1
2. **진입점 rva `0x1d40` = `mainCRTStartup`** (18바이트). 손에 쥐여 준 안내판(PEB)은 **안 본다** `[정적]` → 2.4
3. **`mainCRTStartup` 은 쿠키만 섞고 `+0xd` 에서 `JMP` 로 넘긴다.** 그래서 `main` 의 콜스택에 **안 나온다.** run29 의 예측 한 줄이 정정됐다 `[정적]` → 2.5
4. **`main` 은 `__scrt_common_main_seh +0x10a` (ret `+0x10f`) 에서 불린다** `[정적]` → 2.6 (참고)
5. **run7 의 수수께끼 주소 `…67723` = `test!try_get_function_slow +0x73`** `[정적]` + `[관찰]`. 세 번 불린 이유는 "세 길이 하나씩" 이라는 `[추측]` → 2.7 ~ 2.9 (참고)

그리고 이 바퀴로 **단계 2 (ntdll 지도) 의 처음 항목 Q8 ~ Q18 이 모두 끝났다** → 3절.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   ├─ +0xe   로더 본체 (가게 세팅, DLL 올리기·켜기, TLS)                          (run17 ~ run28)
   └─ +0x18  NtContinue ══▶ 커널 ══▶ IRETQ                                         (run29)
        ══▶ RtlUserThreadStart
              └─ +0x2c  출근 인사 담당 칸 (= BaseThreadInitThunk 추정)            (run29)
                    └─ +0x17  진입점(PEB)                                          (run25)
 ───────────────────────── 여기가 현관 (exe 진입점) ─────────────────────────
 test.exe (컴파일러가 넣은 CRT)
  mainCRTStartup (rva 0x1d40)                                                       ← 이번
   … CRT 준비 (LoadLibraryExW ×3 → 다시 ntdll 로더)                                 ← 이번
   … main                                                                           ← 이번
```

- 지금 목표(프로세스 생성 → `main`)의 **맨 끝 구간**이다.
- 줄 하나가 새로 생겼다. **"여기가 현관"** 줄이다. 이 줄 위는 OS 가 하는 일, 아래는 우리 exe 안에 컴파일러가 넣은 코드다.
- flow.md 로는 **2.10** 이 중심이다. 2.8, 2.9, 그림 2 의 test.exe 상자, 4장 표에도 줄이 더해졌다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. 진입점 `0x1d40` 의 이름은 정말 `mainCRTStartup` 인가? 받은 인자(PEB)를 쓰나?
2. `mainCRTStartup` → `main` 사슬은 어떻게 생겼나? `main` 을 부르는 줄은 어디인가?
3. run7 에서 본 `LoadLibraryExW` ×3 은 test.exe 안 **어느 함수**가 불렀나? 무엇을 올리려고? 왜 세 번?
4. test.exe 안의 CFG, TLS 콜백 배열은 어떤 모양인가?

그리고 **바퀴 도중에 사용자가 물었다** (plan.md 결정 기록 2026-10-23 을 줄임):

> `mainCRTStartup` 은 PE `AddressOfEntryPoint` 가 가리키는 주소일 뿐이고, CRT 는 컴파일러가 넣은 코드라 nt·ntdll 지도엔 필요 없지 않나?

이 질문이 이번 바퀴의 무게중심을 바꿨다. 2.1 에서 차근차근 본다.

### 바퀴 전에 알던 것

- `[정적]` (run16) 첫 스레드의 처음 메모: `Rip` = `RtlUserThreadStart`, **`Rcx` = exe 진입점**, `Rdx` = PEB. 진입점 값은 코드에서 `TransferAddress` 칸이다.
- `[정적]` (run29) `RtlUserThreadStart +0x27` (ret `+0x2c`) 이 칸 `Kernel32ThreadInitThunkFunction` 을 `(0, 진입점, PEB)` 로 부른다. CFG 중개는 `JMP` 뿐이라 프레임이 없다.
- `[정적]` (run25) `BaseThreadInitThunk` 는 첫 인자 0 이면 `+0x12` (ret `+0x17`) 에서 진입점을 `RCX = PEB` 로 간접 호출한다.
- `[관찰]` (run5) 메인 스레드 시작 주소 `0x7ff70ee11d40`, 진입점 `mainCRTStartup` (RVA `0x1D40`).
- `[관찰]` (run7) `ExeTls` 다음, `main` 전에 test.exe 안 `0x7ff70ee67723` 에서 `KERNELBASE!LoadLibraryExW` 가 **3번** 불렸다.
- `[추측]` (flow.md 2.10) "`mainCRTStartup` 은 CRT 준비(`LoadLibraryExW` 3번 포함)를 하고 `main` 을 부른다."
- `[추측]` (flow.md 2.10) "그 코드(`…67723`)가 test.exe 의 어느 함수인지는 이름으로 확인하지 않았다."
- `[추측]` (run29 해설 2.14) `main` 에서 `k` 를 찍으면 `… → BaseThreadInitThunk+0x17 → test!mainCRTStartup+? → … → test!main`.

---

## 2. 찾은 것

### 2.1 먼저 — 지도의 경계가 정해졌다

#### 사용자의 질문, 그리고 결정

- 질문은 위 1절 그대로다. "진입점은 주소 하나일 뿐이고, 그 안은 컴파일러 코드잖아?"
- 결정 (plan.md 2026-10-23, run 문서 맨 위): **"진입점 = 경계, CRT 한 상자"**.
  - 그림 2 엔 **진입점 경계 상자 하나**(점선)만 둔다.
  - 그 상자에서 나가는 화살표는 둘뿐: (1) `LoadLibraryExW` ×3 (→ ntdll 로더), (2) `main`.
  - CRT 함수 이름(`__scrt_common_main_seh`, `try_get_function_slow` …)은 **그림에 안 넣는다.** run30 문서와 flow.md 2.10 "CRT 안쪽 (참고)" 에만 둔다.
  - 동적 항목 Q30·Q31 도 그에 맞춰 좁혔다 (4절).

#### 왜 그 줄이 경계로 자연스러운가 — 단계별로

1. **진입점은 헤더에 적힌 값 하나다.** PE 헤더의 `AddressOfEntryPoint` 칸. 이름이 아니라 주소만 적혀 있다 ([run25 해설 2.3](run25-dll-entry-해설.md#새-말--pe-진입점-addressofentrypoint)).
2. **커널은 그 값을 `Rcx` 에 적어 줄 뿐이다** `[정적]` (run16). 그 주소에 뭐가 있는지는 안 본다.
3. **ntdll·kernel32 도 그 값을 들고 가서 부를 뿐이다** `[정적]` (run29 `RtlUserThreadStart +0x2c`, run25 `BaseThreadInitThunk +0x17`). 간접 호출이다.
4. **그 주소 안은 컴파일러가 넣은 코드다.** test.exe 는 `/MT` 로 빌드했다 `[소스]` (`sample/build.bat`).
   - **새 말 — `/MT`**: CRT 코드를 **exe 안에 같이 넣으라**는 빌드 옵션. 그래서 test.exe 안에 UCRT·vcruntime 이 통째로 들어 있다 (run 문서). Ghidra 가 함수를 3356개나 잡은 이유가 이것이다.
   - **새 말 — CRT**: `main` 앞뒤에서 C 프로그램이 돌 준비(환경, `argv`, 로캘 등)를 하는 코드 묶음. OS 가 아니라 **컴파일러 쪽**이 준다 (run 문서 "컴파일러가 넣은 코드(OS 아님)").
5. → 그러니 "OS 가 프로세스를 만들어 **`main` 앞까지 데려다주는 길**" 이라는 우리 목표에서, **진입점이 OS 쪽 마지막 칸**이다.
6. **단 하나 예외**: CRT 가 짐을 풀다가 **ntdll 로더를 다시 부르는 일**(`LoadLibraryExW` ×3)은 OS 쪽 일이다. 그래서 그 화살표는 지도에 남긴다.

#### 비유로 — 배달 기사와 현관

- 우리 지도는 **배달 기사가 현관까지 오는 길**이다.
  - 관리사무소(커널)가 송장에 **현관 번지**(진입점)를 적어 준다.
  - 관리실(ntdll)과 출근 인사 담당(kernel32)이 그 번지까지 **데려다준다.**
- 현관 안에서 **짐을 어떻게 푸는지**는 **집마다 다르다.** 그게 CRT 다.
  - 이 집(test.exe)은 `/MT` 라서 짐 푸는 도구를 **집 안에 다 갖고 있다.**
  - 다른 컴파일러·다른 옵션이면 안쪽 모양이 다를 수 있다 `[추측: 일반적으로 알려진 설명]`. 레포 안에도 비슷한 예가 하나 있다 — [run25 해설 2.4](run25-dll-entry-해설.md#24-곁가지--두-시작-함수-안은-dll1dll2-와-모양이-다르다) 에서 kernel32·KernelBase 의 시작 함수는 dll1·dll2 의 `_DllMainCRTStartup` → `DllMain` 모양과 **달랐다** `[정적]`.
- 다만 짐을 풀다가 **관리실에 전화해서 "연장통 좀 가져다주세요"** 하는 일(`LoadLibraryExW`)이 있다. 그건 다시 관리실 일이라 **그 전화만** 지도에 그린다.

#### 그래서 이 해설도 그렇게 나눈다

- **2.2 ~ 2.5**: 현관 — 경계 근처. 자세히.
- **2.6 ~ 2.10**: 현관 안 — CRT 안쪽. **참고**로 가볍게. 숫자는 run 문서 그대로 옮기지만, 다 외울 필요 없다.

### 2.2 한눈에

run 문서 "호출 관계" 를 그대로, 지도 안/밖만 표시했다.

```
[KERNEL32]  BaseThreadInitThunk
 └─ +0x17  ? 진입점(PEB) — CFG 중개 JMP, 프레임 없음                 (run25·run29, 대상은 Q30)
──────────── 현관 (지도의 경계) ────────────
[test.exe]  mainCRTStartup  (rva 0x1d40 = 스레드 시작 주소 [관찰 run5]. 받은 PEB 안 씀)
 ├─ +0x9   __security_init_cookie
 ══▶ +0xd JMP (꼬리, 프레임 걷음)
[test.exe]  __scrt_common_main_seh                                   ← 여기부터 "참고"
 ├─ (1) +0x19   __scrt_initialize_crt   … 길 A·B → try_get_function_slow
 ├─ (2) +0x61   _initterm_e             … 길 C   → try_get_function_slow
 │                                                  └─ +0x73  KERNELBASE!LoadLibraryExW   (×3 [관찰 run7])  ← 지도에 남김
 ├─ (3) +0x10f  main                                                                                         ← 지도에 남김
 └─ (4) +0x176  exit(main 반환값)
```

- 전부 `[정적]` 이다 (`[관찰]` 표시가 붙은 것만 빼고).
- 그림 2 에는 이 중 **현관 상자 하나 + `LoadLibraryExW` ×3 + `main`** 만 들어갔다 (3절).

### 2.3 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 현관 안내원이 넘기는 줄

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `test!mainCRTStartup` | `+0xd` `JMP` | (꼬리) | `test!__scrt_common_main_seh` (`+0x0`) | [정적] |

- 누가: `mainCRTStartup` (진입점, 18바이트짜리)
- 어디서: `+0xd`. 그런데 `call` 이 아니라 **`JMP`** 다. 그래서 "ret" 칸이 비어 있다. 돌아올 자리를 **안 남긴다.**
- 누구를: `__scrt_common_main_seh` 의 맨 앞(`+0x0`)
- 무엇을 위해: 진짜 CRT 준비와 `main` 호출을 **통째로 맡기려고.** 바로 앞 `+0x9 ADD RSP,0x28` 로 자기 자리를 먼저 치운다.

#### 한 줄 읽기 ② — `main` 을 부르는 줄

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `test!__scrt_common_main_seh` | `+0x10a` | `+0x10f` | `test!main` | [정적] |

- 누가: `__scrt_common_main_seh`
- 어디서: `+0x10a` 에서 부르고, `main` 이 끝나면 `+0x10f` 로 돌아온다. **콜스택에 찍히는 숫자는 `+0x10f`** 다.
- 누구를: 우리 `main`
- 무엇을 위해: 짐 풀기(CRT 준비)를 다 끝낸 뒤 **본론** 시작. 끝나면 반환값을 들고 `exit` 로 간다.

#### 한 줄 읽기 ③ — 관리실에 다시 전화하는 줄

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `test!try_get_function_slow` | `+0x6d` | **`+0x73`** | `LoadLibraryExW(이름, NULL, 0x800)` (IAT `0x6e250`) | [정적] + run7 `…67723` [관찰] 과 같은 자리 |

- 누가: `try_get_function_slow` (UCRT 안 함수)
- 어디서: `+0x6d` 에서 부르고 `+0x73` 으로 돌아온다. 이 `+0x73` 이 run7 에서 본 **돌아올 주소 `…67723`** 과 같은 자리다 (2.7).
- 누구를: `LoadLibraryExW` — test.exe 의 콘센트(IAT) 칸 `0x6e250` 을 거쳐서.
- 무엇을 위해: "이 API 가 들어 있는 DLL 을 올려 주세요" — 즉 **ntdll 로더를 다시 부르려고.** `0x800` 은 "시스템 폴더에서만 찾아라" 는 뜻이다 (run 문서, `LOAD_LIBRARY_SEARCH_SYSTEM32`).

### 2.4 핵심 ① 현관 번지 = `mainCRTStartup`, 그리고 안내판은 안 펴 본다

#### 이름 확인 `[정적]`

- rva `0x1d40` 의 PDB 이름 = **`mainCRTStartup`** 이다.
- run5 `[관찰]` "진입점 `mainCRTStartup` (RVA `0x1D40`)" 과 같다.
- 지금까지 이 이름은 디버거가 보여 준 것뿐이었다. 이번에 **출석부(PDB)로도** 확인됐다.

#### 18바이트, 네 줄 `[정적]`

- `+0x0` 자리 마련 → `+0x4` `__security_init_cookie` 호출 (ret `+0x9`) → `+0x9` 자리 치우기 → `+0xd` `JMP`.
- **`rcx`·`rdx`·`r8` 을 한 번도 안 읽는다.** 넘겨받은 다음 함수 `__scrt_common_main_seh` 도 첫 줄들에서 `rcx` 를 안 읽는다 (결과서).
- PDB 타입은 `ulong __cdecl mainCRTStartup(void *)` 로 인자 하나를 받는 모양이긴 하다 (결과서). 받는 모양만 있고 **쓰지는 않는다.**

#### 안내판이 손에서 손으로 — 그리고 끝에서 버려진다

여러 바퀴에 흩어진 걸 이어 보면:

1. `[정적]` (run16) 관리사무소가 업무 시작 메모에 적는다: `Rcx` = 진입점, `Rdx` = PEB.
2. `[정적]` (run29) 본업 자리 `RtlUserThreadStart` 가 받아서, 출근 인사 담당 칸을 `(0, 진입점, PEB)` 로 부른다.
3. `[정적]` (run25) 출근 인사 담당 `BaseThreadInitThunk` 는 진입점을 `RCX = PEB` 로 부른다.
4. `[정적]` (이번) 진입점 `mainCRTStartup` 은 그 `RCX` 를 **안 본다.**

- 비유: 관리사무소에서 출발할 때부터 **안내판(PEB)** 을 손에 꼭 쥐여 보냈다. 세 사람을 거쳐 현관까지 잘 왔는데, 현관 안 사람은 **펴 보지도 않고** 내려놓는다.
- 주의: 3번에서 칸 값이 정말 `BaseThreadInitThunk` 인지(Q48), 4번 주소로 정말 들어가는지(Q30)는 아직 버스를 안 타 봤다. 노선도로는 이렇게 이어진다는 것까지다.

#### 올린 파일이 VM 에서 돈 그 파일인가 — 단계별로

이번엔 MS 바이너리가 아니라서 이 확인이 따로 필요했다.

1. `[정적]` pdb 서명 = exe 서명 (글자까지 같음). 즉 **이 pdb 는 이 exe 짝**이다.
2. `[정적]` + `[관찰]` 진입점 rva `0x1d40` = run5 에서 본 값.
3. `[정적]` + `[관찰]` run7 의 `…67723` 과 맞는 자리가 이 exe 안에 **딱 하나** 있다 (2.7).
4. → 두 로그와 다 맞는다. 그래서 이 빌드를 근거로 쓴다.
5. 단 01_sprint 기록상 **run3 ~ run5 와 run6 ~ run8 은 따로 빌드한** test.exe 다 (flow.md 5장). 올라온 파일은 두 기록 모두와 맞아서, **어느 쪽 빌드인지까지는 모른다** (run 문서).

### 2.5 핵심 ② 현관 안내원은 꼬리 `JMP` — 콜스택에서 사라진다

#### 새 말 — 보안 쿠키 (`__security_init_cookie`)

- 함수가 돌아가기 전에 "스택이 덮어써지지 않았나" 를 맞춰 보려고 쓰는 **비밀 값**을, 프로그램 시작 때 한 번 섞어 두는 함수 `[추측: 일반적으로 알려진 설명]`. 근거 문서는 "쿠키 값 섞기, 인자 없음" 까지만 적었다.
- 비유: 택배 상자의 **봉인 스티커** 무늬를 오늘 것으로 정해 두는 일.
- [run25 해설 2.4](run25-dll-entry-해설.md#24-곁가지--두-시작-함수-안은-dll1dll2-와-모양이-다르다) 에서 kernel32·KernelBase 의 작은 CRT 조각도 이것만 준비했었다.

#### 왜 사라지나 — 단계별로 `[정적]`

1. `+0x4` 에서 쿠키 함수를 **`call`** 한다. 이건 돌아올 자리(`+0x9`)를 남긴다. 쿠키 함수가 끝나면 지워진다.
2. `+0x9 ADD RSP,0x28` — 자기 자리를 **치운다.**
3. `+0xd JMP __scrt_common_main_seh` — **`call` 이 아니라 `JMP`** 다. 돌아올 자리를 새로 안 쌓는다 ([꼬리 점프](run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리)).
4. → `__scrt_common_main_seh` 는 마치 **`BaseThreadInitThunk` 가 자기를 직접 부른 것처럼** 스택 위에 선다.
5. → 그 아래에서 부르는 `main` 의 콜스택에도 `mainCRTStartup` 은 **안 보인다.**

- 비유 (flow.md 2.10 그대로): 현관 안내원이 출입증에 **도장(쿠키)** 만 찍고, 자기 자리를 비운 채 담당자 방으로 **바로 들여보낸다.** 그래서 나중에 "누가 데려왔나" 를 거꾸로 따라가면 안내원은 안 나온다.

#### run29 예측의 정정 `[정적]`

[run29 해설 2.14](run29-continue-start-해설.md#214-main-에서-콜스택을-찍으면--예측과-01_sprint-관찰) 에서 `main` 콜스택을 이렇게 예측했다 (아래부터):

1. `ntdll!RtlUserThreadStart+0x2c`
2. `KERNEL32!BaseThreadInitThunk+0x17`
3. ~~`test!mainCRTStartup+?`~~ ← **안 보일 것이다**
4. … (CRT 함수들)
5. `test!main`

이번 예측 (위가 가장 안쪽):

```
test!main
test!__scrt_common_main_seh+0x10f
KERNEL32!BaseThreadInitThunk+0x17
ntdll!RtlUserThreadStart+0x2c
```

- 3번 자리를 `__scrt_common_main_seh+0x10f` 가 차지한다. 그 사이에 다른 CRT 함수도 없다.
- 이렇게 안 나오면 **꼬리 점프 가정이 틀린 것**이다 (run 문서). → 동적 Q30.
- run29 문서 맨 위에도 정정 한 줄이 들어갔다.
- 반대로 run29 의 다른 예측, "`mainCRTStartup` **입구**에서 `k` 하면 바로 위가 `BaseThreadInitThunk+0x17`" 은 **그대로 맞다.** 입구에선 아직 `JMP` 전이니까.
- `mainCRTStartup+0x9` 가 보이는 순간은 딱 하나다: 쿠키 함수 **안에서** `k` 를 찍을 때.

#### 덤 — 아무도 안 부르는 사본 둘 `[정적]`

- `__scrt_common_main` (rva `0x1ae0`) 은 `mainCRTStartup` 과 **똑같은 18바이트**인데 부르는 곳이 없다.
- `invoke_main` (rva `0x1cb0`) 은 `+0x31` 에서 `JMP main` 을 하는데, **이것도 부르는 곳이 없다** (`.pdata` 참조 하나뿐).
- `main` 을 가리키는 곳은 셋인데 (`_refs/main.tsv`), 진짜 길은 `__scrt_common_main_seh +0x10a` 하나다.
- 왜 남았나는 `[추측]`: `invoke_main` 이 `__scrt_common_main_seh` 안으로 합쳐지고, 따로 떨어진 사본만 남았다.
- 요청서는 `mainCRTStartup` → `__scrt_common_main` → `__scrt_common_main_seh` → … → `invoke_main` → `main` 모양을 `[추측]` 으로 예상했었다. 실제로는 **가운데 둘이 빠진** 짧은 길이었다.

### 2.6 참고 — 현관 안: `__scrt_common_main_seh` 의 짐 푸는 순서

여기부터 지도 밖이다. 이름과 순서만 가볍게. 전부 `[정적]`, 숫자는 ret.

| 순서 | ret | 하는 일 |
|---|---|---|
| 1 | `+0x19` | `__scrt_initialize_crt(1)` — UCRT 초기화 표를 돈다 (길 A·B 의 뿌리, 2.9) |
| 2 | `+0x2e` | 시작 잠금 |
| 3 | `+0x61` | `_initterm_e` — `.CRT$XI` 표를 돈다 (길 C 의 뿌리, 2.9) |
| 4 | `+0x82` | `_initterm` — `.CRT$XC` 표를 돈다 |
| 5 | `+0x9e` | 잠금 풀기 |
| 6 | `+0xa3` | `__scrt_get_dyn_tls_init_callback` — 값이 0 이면 `+0xaa` 에서 다음 호출을 건너뛴다. test.exe 는 시작값 0 이라 안 탈 것 |
| 7 | `+0xf2`·`+0xfa`·`+0x102` | 환경·`argv`·`argc` 준비 (`main` 인자) |
| 8 | **`+0x10f`** | **`main`** |
| 9 | `+0x116` | `__scrt_is_managed_app` |
| 10 | `+0x176` | `exit(main 반환값)` — 관리 코드 앱이 아니면 이 길 |

- `main` 의 반환값은 `+0x10f MOV EBX,EAX` 로 담았다가 `+0x16f MOV ECX,EBX` 로 `exit` 에 넘긴다 `[정적]` (flow.md 2.10). `test.c` 의 `main` 은 `return 0` 이니 `exit(0)` 일 것이다 `[소스]`. `main` 이후는 이번 범위 밖.
- 비유: 현관 안에서 **박스 뜯고(1·3·4) → 문 잠그고 정리 → 손에 쥘 것 챙기고(7) → 본론(8) → 뒷정리(10).**
- 기억해 둘 건 딱 하나다. **`LoadLibraryExW` 전화는 1·3 안쪽, 즉 `main` 보다 앞**에서 온다.

### 2.7 핵심 ③ run7 의 `…67723` 은 누구인가 — 아래 16비트 맞추기

run7 이 남긴 수수께끼: `KERNELBASE!LoadLibraryExW` 가 test.exe 안 `0x7ff70ee67723` 에서 세 번 불렸다 `[관찰]`. 그 자리가 test.exe 의 **어느 함수**인지 몰랐다.

#### 먼저 — 부르는 자리를 어떻게 찾았나 `[정적]`

- test.exe 는 `LoadLibraryExW` 를 **콘센트(IAT) 칸 rva `0x6e250`** 을 통해 부른다. `CALL [그 칸]` 모양이다.
- 그래서 그 칸 주소를 가리키는 곳을 찾았다 (`refs test.exe 0x6e250`). 이름 `LoadLibraryExW` 로 찾으면 **0줄**이 나온다 — 도구 메모 (2.11).
- 결과: **9곳.** 다섯 함수에 흩어져 있다 (`try_get_first_available_module`, `try_get_function_slow`, `try_get_module`, `try_get_proc_address_from_first_available_module`, `try_load_library_from_system_directory`).

#### 맞추기 — 단계별로

1. `[관찰]` (run7) 돌아올 주소 = `0x7ff70ee67723`. 아래 16비트(마지막 네 자리) = **`7723`**.
2. 전제 (run 문서·결과서): 이미지 베이스는 **64KB(`0x10000`) 단위**로 놓인다. 그러면 베이스의 마지막 네 자리는 늘 `0000` 이다.
3. 그래서 → **실제 주소의 마지막 네 자리 = rva 의 마지막 네 자리.** 베이스가 실행마다 달라도 이건 안 변한다.
4. `[정적]` 9곳의 ret rva 를 다 적어 보면, 마지막 네 자리가 `7723` 인 건 **딱 하나**: `try_get_function_slow +0x73` (rva **`0x57723`**).
5. 거꾸로 빼 보면 베이스 = `0x7ff70ee67723` − `0x57723` = **`0x7ff70ee10000`**.
6. 맞춰 보기: run5 `[관찰]` 시작 주소 `0x7ff70ee11d40` − `0x1d40` 도 **같은 `0x7ff70ee10000`** 이다 (두 로그 값으로 계산, run 문서).
7. → **`…67723` = `test!try_get_function_slow +0x73`** `[정적]` + `[관찰]`.

- 비유: 아파트 동 번호(베이스)는 이사할 때마다 바뀌어도, **호수 끝 네 자리**(아래 16비트)는 그대로다. 송장에 적힌 호수 끝이 `7723` 인 집이 단지 안에 하나뿐이니 그 집이다.
- 그러니 run7 의 `LoadLibraryExW` 는 `mainCRTStartup` **안**이 아니라, 한참 아래 **CRT 준비 함수들 밑**에서 걸려 온 전화였다.

#### 작은 주의 — 이름표가 둘이다

- test.exe 콘센트 칸 `0x6e250` 은 **`KERNEL32.dll` 몫**으로 적혀 있다 (결과서: "`KERNEL32.dll` import, `api-ms-…` 아님"). run 문서 표도 callee 를 `KERNEL32!LoadLibraryExW` 로 적었다.
- run7 이 실제로 멈춘 곳, 그리고 그림의 상자는 **`KERNELBASE!LoadLibraryExW`** 다 `[관찰]`.
- 그 사이(kernel32 의 그 이름이 KernelBase 로 어떻게 이어지나)는 **이번 근거에 없다.** 정적으로 확인된 건 "test.exe 는 KERNEL32 이름표 칸을 부른다" 까지다.

### 2.8 참고 — `try_get_function_slow`: 연장통을 앞에서부터 열어 본다

여기도 지도 밖이다. "왜 `LoadLibraryExW` 를 부르나" 만 납득하면 된다.

#### 새 말 — 늦게 묶는 API

- 빌드할 때 주소를 박아 두지 않고, **실행 중에 "이 API 있나?" 하고 찾아서** 쓰는 방식. 이 함수가 바로 그 일을 한다: DLL 을 올리고(`LoadLibraryExW`), 이름으로 주소를 찾는다(`GetProcAddress`) `[정적]`.
- 비유 (run 문서): 가게(UCRT)가 "이 연장(API) 있나요?" 할 때 **연장통(DLL) 후보 목록**을 앞에서부터 열어 본다. 한 번 연 연장통은 **선반(캐시)** 에 올려 두고 다시 안 연다.

#### 하는 일 `[정적]`

1. 후보 모듈 id 목록을 앞에서부터 돈다.
2. 선반(모듈 캐시, rva `0x83ef0`)이 비었으면 → `+0x6d` `LoadLibraryExW(이름, NULL, 0x800)`.
3. 열리면 선반에 올리고 → `+0x19e` (ret `+0x1a4`) `GetProcAddress`. **그 연장통에 API 가 없어도 다음 후보로 안 간다.**
4. 안 열리면 → 선반에 `-1` 표시, 다음 후보로. (에러 `0x57` 이고 이름이 `api-ms-`·`ext-ms-` 가 아닐 때만 `+0xc1` (ret `+0xc7`) 에서 플래그 0 으로 한 번 더.)
5. 찾은 주소를 섹션 **`.fptable`** (rva `0x8a000`) 의 함수 포인터 표에 적는다. 앞뒤로 `VirtualProtect`. 이름에 "f" 가 들어가지만 **CFG 와 상관없다.**

#### 어떤 연장을 찾나 — `main` 전에 이 함수로 오는 API 셋 `[정적]`

| 길 | API | 후보 모듈 (순서대로) |
|---|---|---|
| A | `FlsGetValue2` | `api-ms-win-core-fibers-l1-1-2` → `kernelbase` |
| B | `LCMapStringEx` (없으면 `LocaleNameToLCID`, 후보 같음) | `api-ms-win-core-localization-l1-2-1` → `kernel32` |
| C | `AreFileApisANSI` | `kernel32` 하나뿐 |

- 이름 문자열은 결과서가 원본 바이트(UTF-16LE)로 읽었다.
- `api-ms-…` 는 [별명](run23-load-dependent-해설.md#25-핵심--apiset--가상-이름을-진짜-이름으로-바꾸는-자리)이다. 24H2 에서 **어느 진짜 DLL 로 풀리는지, 첫 시도에 열리는지는 정적으로 모른다** (Q31).

### 2.9 참고 — 세 번 = 세 길? (짐작을 단계별로)

#### 세 길은 어디서 오나 `[정적]`

자세한 나무는 [run 문서 "`main` 전에 `try_get_function_slow` 로 가는 세 길"](run30-exe-crt-start.md) 에 있다. 뿌리와 끝만:

- **길 A**: `__scrt_common_main_seh +0x19` → … UCRT 초기화 표 8번째 → `__acrt_tls2_supported +0x37` → `try_get_function_slow`
- **길 B**: `__scrt_common_main_seh +0x19` → … 같은 표 11번째 `__acrt_initialize_multibyte` → … (로캘 함수 여러 칸) → `__acrt_LCMapStringEx +0x4f` → `try_get_function_slow`
- **길 C**: `__scrt_common_main_seh +0x61` → `_initterm_e` → `.CRT$XI` 첫 항목 → … `__acrt_AreFileApisANSI +0x32` → `try_get_function_slow`
- 표 항목을 부르는 간접 호출의 중개(`_guard_dispatch_icall`)는 `jmp rax` 뿐이다. 그리고 `__acrt_initialize` 는 꼬리 `JMP` 다. 둘 다 콜스택에 안 남는다 `[정적]`.
- 다른 표 항목들에서는 `try_get_function_slow` 로 가는 정상 길이 없었다. 단 **표 안 더 깊은 간접 호출은 못 봤다** (지도의 한계).

#### 짐작 — 근거 → 그래서 → 아직 확인 아님

1. 근거 `[관찰]` (run7): `LoadLibraryExW` 가 `main` 전에 **세 번**, 모두 같은 자리 `…67723` 에서.
2. 근거 `[정적]`: 선반(캐시) 때문에 **같은 연장통은 한 번만** 연다.
3. 근거 `[정적]`: `main` 전 길이 **세 개**, 각자 **첫 후보가 서로 다르다** (fibers 별명 / localization 별명 / `kernel32`).
4. 그래서 → "후보 목록을 돌며 하나씩 세 번" 이 아니라, **"서로 다른 API 셋이 각자 첫 후보를 한 번씩"** 이라고 읽는 게 가장 자연스럽다 `[추측]`.
5. 아직 확인 아님:
   - 세 이름이 **모두 첫 시도에 열려야** 맞는 짐작이다. 하나라도 실패하면 다음 후보(`kernelbase`/`kernel32`)로 더 불러서 횟수가 달라진다.
   - run7 표는 세 번을 한 줄로 적었다. 세 번 다 `…67723` 이었다면 `+0xc1` (다시 시도, ret `0x57777`) 길은 안 탔다 `[추측: run7 원본 로그가 레포에 없어 요약 표로만 판단]`.
   - 실행 순서는 표를 앞에서부터 도니 A → B → C 일 것 `[추측]`.
   - 길 B 의 `__acrt_initialize_multibyte` 는 깃발(rva `0x83864`)로 **처음 한 번만** 일한다 `[정적]`. 그래서 길 C 쪽에서 같은 함수를 또 불러도(`_configure_narrow_argv +0x43`) 일을 안 할 것 `[추측]`.
- → 동적 **Q31**. 다만 2.1 결정으로 Q31 은 "**세 번이 ntdll 로더에서 무엇을 올리나**(이름·`LdrpLoadDll` 결과)" 로 좁혔다. 길 A·B·C 구분은 참고로만 본다.

### 2.10 곁가지 — test.exe 안 CFG, TLS 배열

#### CFG — 표는 없고, 교환원은 그냥 연결만 한다 `[정적]`

- test.exe 엔 **CFG 함수 표가 없다.** `DllCharacteristics = 0x8160` (`GUARD_CF` 비트 없음), `GuardCFFunctionTable = 0`, `GuardCFFunctionCount = 0`, `GuardFlags = 0x100`. `[소스]` `build.bat` 에 `/guard:cf` 가 없다.
- 그래도 중개 칸 `__guard_dispatch_icall_fptr` (rva `0x6e2f8`) 는 있다. 값은 `_guard_dispatch_icall_nop` — 내용이 `jmp rax` 하나다.
- 비유: test.exe 안의 [교환원](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)은 **명단 확인 없이** 그냥 연결만 해 준다. 그리고 역시 통화 기록(콜스택)에 안 남는다.
- 남은 궁금증: **kernel32 쪽** 교환원(`BaseThreadInitThunk +0x12`)은 검사를 하는데, 명단(표)이 없는 test.exe 현관을 어떻게 통과시키나? "표가 없는 이미지는 전부 유효로 처리" 라고 알려져 있지만 이 빌드에선 확인 안 했다 `[추측]` → Q30.

#### TLS 콜백 배열 — `ExeTls` 하나 `[정적]`

- `_tls_used` (rva `0x79500`) 의 `AddressOfCallBacks` = rva `0x6e380`. 배열 = **`[ExeTls (0x1400012a0), 0]`**.
- CRT 가 TLS 배열에 따로 넣은 건 없다. CRT 의 `__dyn_tls_init_callback` 은 시작값 0 이고 (`thread_local` 안 씀), 배열이 아니라 `__scrt_common_main_seh` 안(2.6 의 6번)에서 따로 검사한다.
- 그래서 [run28](run28-tls-init-call-해설.md#24-핵심--명단-한-칸--창구-한-번-0-까지) 의 `LdrpCallTlsInitializers` (명단을 0 까지 돎) 는 test.exe 에서 **`ExeTls` 한 번 부르고 멈출 것**이다 `[정적]`.
- 이건 현관 **앞**(로더) 이야기라 지도 안이다. flow.md 2.9 에 들어갔다.

### 2.11 정리 — 비유로 한 번 더

- 관리사무소(커널)가 송장에 **현관 번지**(`0x1d40`)를 적어 주고, 손에 **안내판**(PEB)을 쥐여 보낸다.
- 오리엔테이션(로더)이 끝나고, 본업 자리 → 출근 인사 담당 → **현관**까지 배달된다. 여기까지가 **우리 지도**다.
- 현관 안내원(`mainCRTStartup`)은 안내판을 **펴 보지도 않는다.** 봉인 스티커 무늬(쿠키)만 정하고, 자기 자리를 비운 채 짐 담당(`__scrt_common_main_seh`)에게 **바로 들여보낸다.** 그래서 기록엔 안내원이 안 남는다.
- 짐 담당은 박스를 뜯다가(초기화 표), 연장이 필요하면 연장통 담당(`try_get_function_slow`)에게 맡긴다. 연장통 담당은 **관리실에 전화**(`LoadLibraryExW`)해서 연장통을 받아 선반에 올린다. 이 전화가 세 번 왔다 — 이 전화만 지도에 그린다.
- 짐을 다 풀면 짐 담당이 **본론**(`main`)을 부른다 (`+0x10f`). 끝나면 `exit`.
- 현관 안의 이 모든 순서는 **이 집(`/MT` 로 지은 test.exe) 방식**이다. 다른 집은 다를 수 있다.

### 2.12 곁가지 — 도구 메모

run 문서 "디버거 메모 / 도구 메모" 를 옮긴다. 분석 결과엔 영향이 없다.

- MS 바이너리가 아닌 sample 빌드는 `ms/bin/test.exe`, `ms/sym/test.exe.pdb` 로 **복사**해서 그대로 돌렸다. 다시 빌드해 올리면 복사도 다시 해야 한다.
- import 함수를 부르는 곳은 이름이 아니라 **콘센트 칸 rva** 로 찾는다 (`refs test.exe 0x6e250`). `_index.tsv` 의 `LoadLibraryExW` 는 `JMP [IAT]` 조각일 뿐이다.
- 지도(`map`)는 **함수 포인터 표**를 못 따라간다. 그래서 CRT 초기화 표 안 함수는 뿌리를 따로 줘서 이었다.
- `fetch_pdb.py` 는 로컬 PDB 를 모른다. 이번엔 `--info` 로만 썼다.
- 위 셋은 하네스 후보 (36) ~ (38) 로 적혀 있다.
- **WinDbg 에서 이름이 다르게 보일 수 있다** `[추측]`: 디버거 VM 심볼 경로에 `test.pdb` 가 없으면 `test+0x1b00` 처럼 rva 로만 보인다. 이번 예측 콜스택의 이름은 PDB 기준이다. (하네스 후보 (39), plan.md)

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 어디 | 전 | 후 |
|---|---|---|
| 그림 2 test.exe 상자 `m1` | `test!mainCRTStartup` "(추측: 스레드 시작 주소)" | **진입점 경계 상자**(점선): "exe 진입점 = PE `AddressOfEntryPoint` (rva `0x1d40`, `test!mainCRTStartup`) — 여기부터 컴파일러 CRT (OS 아님, 정적 run30). 커널은 헤더 값을 Rcx 로 넘길 뿐. 받은 PEB 는 안 씀. …" |
| 그림 2 화살표 | `m1` → `?` → `LoadLibraryExW` ×3, → `main` | 같은 모양 그대로 `(1) ?`, `(2) ?`. 사이에 CRT 함수가 끼고 caller 를 콜스택으로 본 적이 없어서 `?` 다. 부른 자리 `test+0x57723` 이 CRT 안이라는 건 상자 글에 적었다 |
| "두 그림 사이 설명" | — | run30 문단 새로: 경계 상자 하나로 정리한 이유, 남긴 화살표 둘 |
| 2.8 (dll2 → dll1) | dll 쪽 `LoadLibraryExW` ×3 = CRT 준비 `[추측]` | 줄 추가: exe 쪽 같은 ×3 은 `try_get_function_slow +0x73` 이었다 `[정적]`. DLL 도 `/MT` 라 같은 함수일 것 `[추측]` (Q55) |
| 2.9 (TLS 콜백) | 배열 모양 모름 | **`[ExeTls, 0]` 하나뿐** `[정적]` → `LdrpCallTlsInitializers` 는 한 번 부르고 멈출 것 |
| 2.10 트리 | `+0x17 ? 진입점(PEB) = test!mainCRTStartup 추정` 에서 끝 | `+0x17` 줄은 "진입점 값은 run30, 실제 대상은 Q30". 그 아래 **`[test.exe]` 경계 줄** + "여기부터 컴파일러 CRT (지도 밖)" + `(1) ? LoadLibraryExW ×3 → ntdll 로더`, `(2) ? main` |
| 2.10 `[추측]` "`mainCRTStartup` 이 CRT 준비 + `main`" | `[추측]` | **판정: 방향은 맞지만 함수가 다르다.** 그 일은 `__scrt_common_main_seh` 가 한다 `[정적]`. ×3 은 길이 셋까지 `[정적]`, 셋 = 세 번은 `[추측]` |
| 2.10 `[추측]` "`…67723` 이 어느 함수인지 모름" | `[추측]` | **취소선** → `test!try_get_function_slow +0x73` (rva `0x57723`) `[정적]` |
| 2.10 새 절 | — | **"CRT 안쪽 (참고 — 지도 범위 밖)"**: 경계 설명, `mainCRTStartup`, `__scrt_common_main_seh`, `try_get_function_slow`, 세 길, `main` 에서 `k` 예측, CFG |
| 4장 공백 표 | `BaseThreadInitThunk` 대상, `…67723` 미확인, Q31 넓은 문구 | 대상 = 진입점 값은 `[정적]`(실행은 Q30), `…67723` 줄 **해결됨**, `LoadLibraryExW` 인자 `(이름, NULL, 0x800)` `[정적]`, Q30·Q31 **좁힌 문구** |
| 4.1 표 | `mainCRTStartup` → `LoadLibraryExW` ×3, → `main` | 진입점 경계 상자 → (1) `?` ×3, (2) `?` `main` 으로 고침, 근거 칸에 run30 |
| 앞머리·5장 | — | test.pdb 서명, "run30 test.exe", run30 줄, 요청서 번호 표에 sast/25 |
| run29 문서 | `main` 에서 `k` 예측에 `mainCRTStartup+?` | 맨 위 **정정 한 줄** |

- 태그 변화 한 줄 요약: 2.10 의 `[추측]` 두 줄이 **판정**을 받았다 (하나는 "함수가 다름", 하나는 이름 확정). 그림의 화살표는 **여전히 `?`** 다 — 실행은 안 봤으니까.
- plan.md 결정 기록엔 "CLAUDE.md '지금 목표' 문구 수정은 따로 보여 주고 OK 받기 (아직 안 고침)" 도 남아 있다.

### 단계 2 (ntdll 지도) 처음 항목이 다 끝났다 — 한눈에

plan.md 대기열의 "끝 (runN)" 을 그대로 옮겼다. 새 사실은 없다.

| 대기열 | run | 다룬 것 (plan.md 항목을 줄임) |
|---|---|---|
| Q8 | [run17](run17-ldr-init-entry-해설.md) | `LdrInitializeThunk` → `LdrpInitializeInternal` → `LdrpInitialize` (로더 입구) |
| Q9a | [run18](run18-proc-init-setup-해설.md) | `LdrpInitializeProcess` 앞쪽 — 프로세스 전체 준비 |
| Q9b | [run19](run19-proc-init-modules-해설.md) | `LdrpInitializeProcess` 뒤쪽 — 모듈 목록·kernel32·import·그래프 |
| Q10 | [run20](run20-kernel32-load-해설.md) | `LdrpInitializeKernel32Functions`, `LdrLoadDll`, `LdrpLoadDll` |
| Q11 | [run21](run21-load-dll-internal-해설.md) | `LdrpLoadDllInternal`, `LdrpPrepareModuleForExecution` |
| Q12a | [run22](run22-map-snap-dep-해설.md) | import 를 하나씩 도는 바깥 고리 `LdrpMapAndSnapDependency` |
| Q12b | [run23](run23-load-dependent-해설.md) | 의존 DLL 하나 처리 `LdrpLoadDependentModuleInternal` |
| Q12c | [run24](run24-work-queue-해설.md) | 일감 흐름 `LdrpQueueWork`·`LdrpProcessWork`·`LdrpDrainWorkQueue` |
| Q13 | [run25](run25-dll-entry-해설.md) | KERNELBASE·KERNEL32 시작 함수 이름 |
| Q14 | [run26](run26-map-dll-file-해설.md) | dll1·dll2 를 파일로 올리는 길 |
| Q15 | [run27](run27-init-graph-해설.md) | `LdrpInitializeGraphRecurse`, `LdrpInitializeNode` (켜기 순서) |
| Q16 | [run28](run28-tls-init-call-해설.md) | `LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `ImageTlsCallbackCaller` |
| Q17 | [run29](run29-continue-start-해설.md) | 로더 끝 `NtContinue`, `RtlUserThreadStart` |
| Q18 | run30 (이 글) | `BaseThreadInitThunk` 다음 → test.exe 진입점 (→ 경계 결정) |

- 이렇게 보면 그림 2 (test.exe 메인 스레드) 의 길이 **노선도로는 현관까지 끊김 없이** 이어졌다.
  - 단계 1 (run9 ~ run16): 관리사무소 쪽 — 가게 차리기, 첫 직원, 출근 첫 발, 업무 시작 메모 바꿔 끼우기.
  - 단계 2 (run17 ~ run30): 관리실 쪽 — 오리엔테이션(로더) 전체, 다시 관리사무소 들러 본업 자리로, 출근 인사 담당, **현관**.
- 그런데 단계 1 때와 마찬가지로 **전부 `[정적]` 이다.** 노선도는 다 읽었지만, 이번 스프린트에선 버스를 한 번도 안 탔다.
- 단계 2 쪽에 **나중에 붙은** 정적 항목들은 대기열에 남아 있다 (예: Q55, Q59 — 4절).

### 큰 그림에서 어떻게 이어지나

- 이번 결정으로 **지도의 끝이 또렷해졌다.** "`main` 까지" 라는 목표에서, OS 가 하는 일의 마지막 칸은 **현관(진입점)** 이다.
  - 현관 뒤에서 OS 가 다시 나오는 건 `LoadLibraryExW` ×3 → ntdll 로더 하나뿐이다. 이건 이미 그린 로더 길(`LdrLoadDll` → `LdrpLoadDll`, run20·run21)을 **한 번 더 타는** 것이다.
- 그다음은 **단계 3 — 빈 곳 채우기**다. 대기열 다음 항목은 **Q19** (동적, 첫 스레드가 실제로 풀리는 시점).
- plan.md: 단계 3 을 시작하기 전에 사용자에게 **진행 방식**을 묻기로 했다 — "작은 정적 먼저 몰아서" 할지, "줄 선 순서대로" 할지.
- 버스를 탈 때 들고 갈 이번 바퀴 예측:
  - `test+0x1d40` (현관) 에서 `k`: 바로 위가 `BaseThreadInitThunk+0x17` (Q30).
  - `test+0x1320` (`main`) 에서 `k`: 2.5 의 네 줄 (Q30, 참고).
  - `test+0x5771d` (`LoadLibraryExW` 부르는 줄) 에서 이름 셋 (Q31).

---

## 4. 아직 모르는 것

오프셋은 `test + rva` 다. 베이스는 실행마다 다르지만 아래 16비트는 같다 (2.7). test.exe 안이라 다른 프로세스엔 안 걸리지만, 커널 디버거에선 test.exe 가 올라온 **뒤에** 걸어야 한다 (run 문서).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `BaseThreadInitThunk +0x17` 이 부르는 대상이 정말 `AddressOfEntryPoint` (`test+0x1d40`) 인가 — 현관 입구 `k` 바로 위가 `BaseThreadInitThunk+0x17` 인가 | 간접 호출이라 대상은 실행 때 값. 정적으론 "넘기는 진입점 값 = `mainCRTStartup`" 까지 | 동적 **Q30** (2026-10-23 좁힘: **진입점 도착까지 + CFG 통과**) |
| kernel32 CFG 검사가 CFG 표 없는 test.exe 현관을 어떻게 통과시키나 | "표 없으면 전부 유효" 는 `[추측]`, 이 빌드에서 확인 안 함. ntdll 쪽 CFG 비트맵 처리도 이번 범위 밖 | 동적 **Q30** |
| `main` 입구 `k` 에 정말 `mainCRTStartup` 이 없고 `__scrt_common_main_seh+0x10f` 가 오나 (`test+0x1b00` 입구에서도) | 실행 때 | 동적 **Q30** — 단 CRT 안 프레임은 **참고로만** |
| `LoadLibraryExW` ×3 이 ntdll 로더에서 **무엇을 올리나** — 이름과 `LdrpLoadDll` 결과 | `api-ms-…` 별명이 24H2 에서 어디로 풀리는지, 첫 시도에 열리는지 정적으로 모름 | 동적 **Q31** (좁힘: 이름·`LdrpLoadDll` 결과만) — `test+0x5771d` 에서 `du @rcx`, `test+0x57723` 에서 `rax` |
| 세 번 = 세 길(A·B·C)이 각자 첫 후보 한 번씩인가 | `[추측]`. 셋 다 첫 시도에 열려야 맞음 | 동적 **Q31** (참고) — 세 번이 넘거나 `test+0x57777` 에 서면 짐작이 틀린 것 |
| `LoadLibraryExW` 가 `LdrLoadDll` 에 싣는 인자 | KERNELBASE 코드다 (test.exe 쪽 인자 `(이름, NULL, 0x800)` 만 이번에 `[정적]`) | 동적 **Q31** 또는 kernelbase 정적 (flow.md 4장) |
| dll1·dll2 시작 함수 안 `LoadLibraryExW` ×3 (run7 `0x7ffd80e2f5fb`·`0x7ffd80ebf60b`) 도 같은 UCRT 함수인가 | dll 바이너리는 이번에 안 열었다. `/MT` 라 같을 것 `[추측]` | 정적 **Q55** (한 줄 확인. 원래 Q55 질문 — `DllMain` 에 `lpReserved` 를 그대로 넘기나 — 과 같은 바이너리) |
| kernel32 `BasepInitializeTermsrvFpns` 안쪽 (`BaseThreadInitThunk` 첫 인자 ≠ 0 갈래, 가게 세팅 때 `(1,0,0)` 호출이 타는 쪽 `[추측]`) | Q18 범위를 test.exe 로 바꾸면서 안 봤다 | 정적 **Q59** (새 항목) |
| 칸 `Kernel32ThreadInitThunkFunction` 값이 정말 `BaseThreadInitThunk` 인가 | 칸 값은 실행 때 | 동적 **Q48** (run29 에서 이어짐) |
| 올라온 test.exe 가 run3 ~ run5 빌드인가, run6 ~ run8 빌드인가 | 두 기록 모두와 맞아서 못 가린다 | 번호 없음 (run 문서) |
| test.exe 의 `KERNEL32` 이름표 칸이 어떻게 `KERNELBASE!LoadLibraryExW` 로 이어지나 (2.7 끝) | 이번 근거에 없다 | 번호 없음 (보고에 적음) |
| CRT 표 항목 안 더 깊은 간접 호출에서 `try_get_function_slow` 로 가는 길이 또 있나 | 지도가 함수 포인터 표 안 간접 호출을 못 따라감 | 번호 없음. 동적 Q31 의 횟수로 간접 확인 |
| WinDbg 가 CRT 함수 이름을 PDB 이름대로 보여 주나 | 디버거 VM 심볼 경로에 `sample/bin` 이 있어야 함 `[추측]` | 하네스 후보 (39) — Q30·Q31·Q55 전에 |

---

## 5. 더 읽기

- run 문서: [`run30-exe-crt-start.md`](run30-exe-crt-start.md) — 맨 위 "지도 범위" 결정, 환경(빌드 대조), 호출 지점 표, `main` 을 부르는 곳, `LoadLibraryExW` 9곳 표, `try_get_function_slow` 하는 일, 세 길 나무 전체, 예상 콜스택, CFG, TLS 배열, `[추측]` 두 줄 판정, 정정, 동적 확인거리
- 정적 결과서: [`_workspace/sast/25_exe-crt-start_result.md`](../../_workspace/sast/25_exe-crt-start_result.md) — 질문 1 ~ 6 답, `LoadLibraryExW` 후보 모듈 문자열(원본 바이트), 길 B·C 의 `k` 예상 전체, 스크립트 문제
- 요청서: [`_workspace/sast/25_exe-crt-start.md`](../../_workspace/sast/25_exe-crt-start.md)
- flow.md: [`flow.md`](flow.md)
  - 그림 2 test.exe 상자 (`m1` 경계 상자)
  - "두 그림 사이 설명" 의 run30 문단
  - **2.10** — 트리의 경계 줄, 판정, **"CRT 안쪽 (참고 — 지도 범위 밖)"** 절 전체
  - 2.8 의 run30 줄, 2.9 TLS 배열
  - 4장 공백 표 2.10 줄들, 4.1 그림 2 표, 5장 run30 줄
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 앞 해설:
  - [run29 해설](run29-continue-start-해설.md) — 2.13 본업 자리가 출근 인사 담당을 `(0, 진입점, PEB)` 로 부름, 2.14 `main` 콜스택 예측 (3번 줄이 이번에 정정됨)
  - [run25 해설](run25-dll-entry-해설.md) — 2.3 PE 진입점, 2.4 시스템 DLL 의 다른 CRT 모양, 2.6 `BaseThreadInitThunk` 두 갈래와 `+0x17`
  - [run16 해설](run16-thunk-context-해설.md) — 2.4 업무 시작 메모의 `Rcx` = 진입점, "단계 1 이 끝났다" 표
  - [run28 해설](run28-tls-init-call-해설.md) — 2.4 TLS 명단은 0 까지, 2.9 교환원
  - [run17 해설](run17-ldr-init-entry-해설.md) — 2.4 꼬리 점프
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q18 (끝), Q19 (다음), Q30·Q31 (좁힘), Q55, Q59 (새), 결정 기록 2026-10-23 "지도 범위 — 진입점 = 경계, CRT 한 상자", "Q18 끝"

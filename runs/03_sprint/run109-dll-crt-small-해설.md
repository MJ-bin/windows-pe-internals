# run109 dll-crt-small — 해설

> 이 글은 [run109 문서](run109-dll-crt-small.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q73** — 2.8 sample dll CRT 에 남은 작은 정적 셋 (a) · (b) · (c) (`_workspace/plan.md`, 이번에 **끝**, 한 바퀴로)
  - Q73 은 [run40](../02_sprint/run40-dll-crt-entry-해설.md) (sast/35) 이 남긴 "새 정적 공백" 셋이다.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다.
  - 이번 바퀴는 **짐 지도 바퀴** 로 셌다 (짐 지도에 체크리스트 밖 칸 묶음이 하나 생겼다 — 3절).
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q31** (run109 몫이 덧붙음), **Q79** (이미 있는 P2 덤프를 오프라인으로 읽기)
    - 정적 → 새 **Q197 ① ~ ④**
- 근거:
  - run 문서 [`run109-dll-crt-small.md`](run109-dll-crt-small.md) (가장 중요)
  - 요청서 [`_workspace/sast/105_dll-crt-small.md`](../../_workspace/sast/105_dll-crt-small.md) · 결과서 [`105_dll-crt-small_result.md`](../../_workspace/sast/105_dll-crt-small_result.md) (판정 **분석성공**)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다** (CRT 한 상자 원칙). 글은 기준 · 2.8 · 2.10 · 3장 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): 고친 줄 1 (3.3 `FlsData`), 새 7.6.13 · 7.40, 2장 숫자 그대로.
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 연 것은 MS 바이너리가 아니라 **우리 dll1.dll · dll2.dll** 이다 (run40 때 이미 Ghidra 에 넣어 둔 것, 다시 analyze 안 함).
  - 이 글의 `[관찰]` 은 하나뿐이다 — run44 사진 로그의 TEB `FlsData` 두 줄을 flow-writer 가 다시 읽은 것 (2.9).

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **FLS** (fiber local storage) → 1절 "무엇이 궁금했나"
- **쪽 보호 값** (읽기 전용 2 · 읽기+쓰기 4) → 2.7
- **FLS 파괴자 콜백** → 2.12
- 이미 푼 말은 링크만 건다:
  - **CRT** = 현관 안에서 **짐 푸는 법**, `/MT` = 짐 푸는 도구를 **집 안에 다 갖고 있는** 빌드 → [run30 해설 2.1](../02_sprint/run30-exe-crt-start-해설.md#21-먼저--지도의-경계가-정해졌다)
  - **늦게 묶는 API**, `try_get_function_slow` (연장통을 앞에서부터 열어 보기), 길 A · B · C → [run30 해설 2.8](../02_sprint/run30-exe-crt-start-해설.md#28-참고--try_get_function_slow-연장통을-앞에서부터-열어-본다) · [2.9](../02_sprint/run30-exe-crt-start-해설.md#29-참고--세-번--세-길-짐작을-단계별로)
  - **ApiSet** (api-set = **별명**, `apisetschema.dll` = **별명 사전**) → [run101 해설 2.9](run101-k32-leftovers-해설.md#29-forwarder-가-아니라-스텁이다) · [run23 해설 2.5](../02_sprint/run23-load-dependent-해설.md#25-핵심--apiset--가상-이름을-진짜-이름으로-바꾸는-자리)
  - **스텁** (넘기기만 하는 짧은 조각 = 옆 건물로 이어진 **복도**) → [run101 해설 2.9](run101-k32-leftovers-해설.md#29-forwarder-가-아니라-스텁이다)
  - **forwarder** (**"옆 건물 몇 호로 가세요" 안내문**) → [run36 해설 2.6](../02_sprint/run36-snap-module-해설.md#26-질문-2--다른-dll-에-있다는-안내문-forwarder)
  - `lpReserved` = 로더가 시작 함수에 건네는 **봉투** → [run40 해설 2.5](../02_sprint/run40-dll-crt-entry-해설.md#25-핵심--봉투는-안-뜯고-그대로--r8-이-가는-길)
  - **TLS** (스레드마다 따로 갖는 변수 칸) → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) "새 말 — TLS"
  - **lowio** (CRT 안 fd 번호와 OS 손잡이를 잇는 아래층) → [run84 해설 2.3](run84-console-crt-해설.md) · [2.13](run84-console-crt-해설.md)
  - CFG 간접 호출 = **교환원** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)

### 이번 바퀴에서 조심할 곳 넷

1. **세 질문은 서로 다른 시간에 있다.**
   - (a) 는 **은퇴 (`NtContinue`) 전** — 로더가 dll2 · dll1 의 전원을 켤 때 (사진 P1b ~ P2 사이). 짐 지도 시간 안이다.
   - (b) 는 **프로세스 끝** (이유 0). 지도 밖.
   - (c) 는 **새 스레드** (이유 2 · 3). 지도 밖.
   - 그래서 2절도 (a) 2.3 ~ 2.9, (b) 2.10, (c) 2.11 ~ 2.13 으로 나눈다.
2. **"적어도 3번" 과 "7번쯤" 은 다르다.**
   - `NtProtectVirtualMemory` 가 dll 마다 **적어도 3번** 은 `[정적]` 이다 (길 A 하나만 셈).
   - 세 길을 다 타면 **7번쯤** 은 `[정적 + 추측]` 이다. 세 길이 dll 에서 정말 세 번 타는지는 동적 Q31.
3. **아직 안 센 syscall 이 있다.** `LoadLibraryExW` · `GetProcAddress` 가 ntdll 안에서 내는 syscall 수는 **안 읽었다** (2.8, Q197 ①).
4. **오프셋은 call 자리다.** ret 자리는 run 문서 "정적 — 호출 지점" 표에 있다.
   - 다만 1절 큰 그림과 2.1 트리는 run 문서 "호출 관계" 처럼 **ret 자리**로 적었다. 숫자가 5 ~ 7 쯤 다르면 call / ret 차이다 (예: `try_get_function_slow` `+0x6d` call ↔ `+0x73` ret).
   - 함수 안 오프셋은 dll1 · dll2 가 같다. 함수 시작 rva 만 dll2 가 `0x10` 앞이다 (2.14).

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**, DLL = **건물**
- kernel32 = **기본 설비 건물**, KERNELBASE = 그 옆 **본 설비 건물** → [run25 해설](../02_sprint/run25-dll-entry-해설.md)
- 우리 지도 = 배달 기사가 **현관까지** 오는 길, 진입점 = **현관**, CRT = 현관 안 **짐 푸는 법** (run30)
- dll 현관 안 (run40):
  - `_DllMainCRTStartup` = **정문 경비**, `dllmain_dispatch` = **접수처**, `dllmain_crt_process_attach` = 사무실 **준비 담당**, 우리 `DllMain` = **손님**
  - `lpReserved` = **봉투**
- `try_get_function_slow` 쪽 (run30 · 이번 run 문서 "결과"):
  - DLL = **연장통**, 모듈 핸들 캐시 (`module_handles`) = 한 번 연 연장통을 올려 두는 **선반**
  - 함수 포인터 표 (`.fptable`) = **공구 목록** (찾은 연장의 주소를 적어 두는 종이)
  - 이번에 더하는 것 (run 문서 그대로): 공구 목록엔 평소 **유리 덮개** (읽기 전용) 가 씌워져 있다. 한 줄 적을 때마다 덮개를 열고 닫는다.
- TEB = 직원 **이름표** → [run83 해설 2.6](run83-console-kernelbase-해설.md)
- 프로세스 힙 = 가게 **창고** → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md)
- 유저 쪽 잠금 (임계 구역 · SRW 락) = **열쇠** → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md)
- 짐 = **개업 준비물**, 짐 지도 = 준비물 **점검표**, 상태 사진 = 준비 도중 찍은 **사진** (P1b · P2 …) → [run43 해설](run43-baggage-checklist-해설.md) · [run44 해설](run44-baggage-snap-해설.md)
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 | 표시 |
|---|---|---|
| (a) dll 도 `FlsGetValue2` 를 `try_get_function_slow` 로 찾나? | **찾는다.** test.exe 길 A 와 id · 이름 · 순서가 같다 | `[정적]` |
| (a) 첫 연장통 이름 · 플래그는? | `api-ms-win-core-fibers-l1-1-2`, 플래그 `0x800`. 요청서가 짐작한 `kernel32` · `fibers-l2` 는 **아니다** | `[정적]` |
| (a) 그 이름은 어디로 풀리나? | `kernelbase.dll` → `KERNELBASE!FlsGetValue2` 는 forwarder `NTDLL.RtlFlsGetValue2` → 공구 목록 `.fptable +0x18` 에 **`ntdll!RtlFlsGetValue2`** | `[정적: PE 표]` |
| (a) 은퇴 전 syscall 을 내나? | dll 코드엔 `syscall` 0. 부르는 쪽에서 **`NtProtectVirtualMemory` dll 마다 적어도 3번** (`VirtualProtect`) | `[정적]` |
| (b) 이유 0 에서 무엇이 갈리나? | 봉투가 0 인가 아닌가 (`CL`) 하나. 프로세스 끝 (`CL = 1`) 이면 **`_flushall` 만**, 정리 표 · `FreeLibrary` · `FlsFree` 는 안 한다 | `[정적]` |
| (c) 이유 2 · 3 은 무엇을 하나? | 새 스레드의 **FLS 칸 + 힙 덩이** 만들기 · 비우기. TLS 가 아니다. dll 코드에 syscall 없음 | `[정적]` |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 **ret 자리**. 숫자는 앞 run 들 · 이번 run 문서 그대로.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessW … NtCreateUserProcess … NtResumeThread           (run9 ~ run14, run34)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ LdrpInitializeProcess
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)        사진 P1b ~ P2 사이   (run27)
              └─ … LdrpCallInitRoutineInternal +0x22                                         (run28)
 ─────────────────── 여기가 dll 의 현관 (dll 진입점) ───────────────────
 dll2 / dll1 (컴파일러가 넣은 /MT CRT)
  _DllMainCRTStartup ═JMP▶ dllmain_dispatch                                                 (run40)
     … CRT 준비: 표 2번 · 7번 → VirtualProtect · LoadLibraryExW · GetProcAddress            ← 이번 (a)
     … DllMain(1)
 ─────────────────────────────────────────────────────────────────
   └─ NtContinue (은퇴) ═▶ … → test.exe 현관 → main                                         (run29 · run30)
        ├─ CreateThread → 새 스레드: dll 마다 이유 2 (DllMain 앞) · 이유 3 (DllMain 뒤)      ← 이번 (c), 지도 밖
        └─ main 이 끝난 뒤 … LdrShutdownProcess → dll 마다 이유 0                           ← 이번 (b), 지도 밖
```

- (a) 는 run40 이 본 **"준비 담당" 안쪽** 이다. 로더가 dll 전원을 켜는 구간의 맨 끝, 은퇴 바로 앞.
- (b) · (c) 는 지도 범위 (프로세스 생성 → `main`) **밖** 이다. run40 이 "이름만 봤다" 며 남겨 둔 갈래들을 이번에 한 칸씩 열어 봤다.
- flow.md 로는 **2.8** 이 중심이다. 2.10 · 3장 · 4장에도 줄이 붙었다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면 셋이다.

- **(a)** CRT 초기화 표 안에서 `FlsGetValue2` 를 찾는 길이 dll 에서도 `try_get_function_slow` (`LoadLibraryExW` · `GetProcAddress`) 로 가나? 이름 · 모듈 이름 · 플래그는? **은퇴 전에 syscall 을 내나?**
- **(b)** 이유 0 (DLL 을 뗄 때) 에 `__scrt_uninitialize_crt(ECX = (lpReserved ≠ 0), EDX = 0)` 안에서 무엇이 달라지나?
- **(c)** 이유 2 · 3 (새 스레드가 붙고 · 떨어질 때) 에 `__scrt_dllmain_crt_thread_attach/detach` 가 무엇을 부르나? **TLS 정리인가, FLS 정리인가?** syscall 이 있나?

**새 말 — FLS** (fiber local storage):

- 스레드마다 따로 쓰는 **작은 칸 묶음** 이다. TLS 와 비슷하지만 원래는 **파이버** (한 스레드 안에서 번갈아 도는 가벼운 실행 단위) 단위로 따로 갖게 만든 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이름에 `Fls` 가 붙은 API (`FlsAlloc` · `FlsGetValue` · `FlsSetValue` · `FlsFree`, 그리고 `FlsGetValue2`) 가 이 칸을 다룬다.
- 비유: 직원 이름표 (TEB) 뒤에 달린 **작은 사물함 열쇠꾸러미**. 칸 번호 (FLS 색인) 는 가게가 정하고, 칸 안에 넣는 물건은 직원마다 다르다.
- 이번 글에서 TEB 쪽 자리는 `+0x17c8` `FlsData` 다 (run 문서 · 결과서 — PDB `_TEB`).

### 바퀴 전에 알던 것

- `[정적]` (run30) test.exe 의 길 A: UCRT 초기화 표 8번째 → `__acrt_tls2_supported +0x37` → `try_get_function_slow`. 첫 후보 `api-ms-win-core-fibers-l1-1-2`, 둘째 `kernelbase`.
  - 그런데 그 별명이 24H2 에서 **어느 진짜 DLL 로 풀리는지는 정적으로 모른다** 고 남겼다 (flow.md 2.10).
- `[관찰]` (run7) dll 시작 함수가 불린 뒤 `DllMain` 보다 먼저 `KERNELBASE!LoadLibraryExW` 가 3번.
- `[정적]` (run40) 그 ×3 의 돌아올 자리는 dll2 · dll1 의 `try_get_function_slow +0x73`, `dllmain_dispatch +0x76` 아래 (`DllMain` 앞).
  - 지도 도구가 따라간 길은 둘 (`AreFileApisANSI` · `LCMapStringEx`). **길 A 는 함수 포인터 표를 거쳐서 지도가 못 따라갔다** → Q73 (a).
- `[정적]` (run40) 이유 0 에서 CRT 는 봉투를 **딱 한 번 `TEST` 만** 한다 (`dllmain_crt_dispatch +0x47 TEST R8,R8` → `CL`). 그 `CL` 이 안에서 무엇을 가르는지는 안 봤다 → Q73 (b).
- `[정적]` (run101) `KERNEL32!LoadLibraryExW` 는 7바이트 `JMP` 스텁 → `KERNELBASE!LoadLibraryExW`.
- `[추측]` (짐 지도 3.3) TEB `FlsData` 를 처음 채우는 `RtlFlsSetValue` 를 부르는 쪽은 "사용자 CRT 의 `FlsAlloc`/`FlsSetValue` → KERNELBASE" 일 것.
- `[추측]` (run84 해설 2.13) sample `/MT` CRT 의 lowio 초기화가 dll 준비 (은퇴 전) 에서 불릴 수 있다.

---

## 2. 찾은 것

### 2.1 한눈에 — 트리 하나

run 문서 "호출 관계" 를 줄였다. 숫자는 **ret 자리**, dll1 기준 (dll2 도 같은 오프셋). ① ② ③ 은 `NtProtectVirtualMemory`.

```
(이유 1 — dll2 먼저, dll1 나중. 은퇴 전 P1b~P2)
dllmain_dispatch
 └─ +0x7b  dllmain_crt_dispatch ══JMP══▶ dllmain_crt_process_attach
            └─ +0x17  __scrt_initialize_crt(0)
                       └─ +0x22  __acrt_initialize ══JMP══▶ __acrt_execute_initializers
                                  └─ +0x2a  표 항목 (CFG 간접)
                                       ├─ [2] __acrt_initialize_winapi_thunks
                                       │        └─ +0x26  VirtualProtect(.fptable, 2)                 ①
                                       └─ [7] __acrt_initialize_ptd
                                                ├─ +0x9   __acrt_tls2_supported
                                                │          └─ +0x37  try_get_function_slow(3, "FlsGetValue2")
                                                │                     ├─ +0x73   LoadLibraryExW("api-ms-win-core-fibers-l1-1-2", 0x800)
                                                │                     ├─ +0x123  VirtualProtect(4)              ②
                                                │                     │  +0x13c  XCHG .fptable+0x18 ← ntdll!RtlFlsGetValue2
                                                │                     ├─ +0x154  VirtualProtect(2)              ③
                                                │                     └─ +0x1a4  GetProcAddress("FlsGetValue2")
                                                ├─ +0x1b  __acrt_FlsAlloc(destroy_fls)
                                                └─ +0x2f  __acrt_getptd_noexit → 첫 스레드 acrt 칸 만들기
 └─ +0x96  DllMain(1)

(이유 0 — 프로세스 끝, 지도 밖)   … dllmain_crt_process_detach (CL = 1) → __scrt_uninitialize_crt(1, 0)
                                    ├─ __acrt_uninitialize(1) → _flushall 만
                                    └─ __vcrt_uninitialize(1) → 아무것도 안 함

(이유 2 · 3 — 새 스레드, 지도 밖) dllmain_crt_dispatch → __scrt_dllmain_crt_thread_attach  (DllMain 앞) : FLS 칸 둘 + 힙 덩이
                                  dllmain_crt_dispatch → __scrt_dllmain_crt_thread_detach  (DllMain 뒤) : 칸 비우고 덩이 돌려주기
```

- 트리에서 그림처럼 보면: **표 2번이 먼저 공구 목록에 덮개를 씌우고 (①), 표 7번이 연장 하나를 찾아 덮개를 열고 적고 닫는다 (② ③).**
- 지도에 실제로 그려진 건 이 중 `LoadLibraryExW` ×3 노드뿐이다 (그림 2, run40). 나머지는 2.8 "CRT 안쪽 더" 글에만 있다 (3절).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — `try_get_function_slow +0x6d` (call, ret `+0x73`)

`LoadLibraryExW(L"api-ms-win-core-fibers-l1-1-2", NULL, 0x800)` `[정적]`

- **누가**: dll1 (또는 dll2) 의 UCRT 함수 `try_get_function_slow`.
- **어디서**: 함수 안 `+0x6d`. 바로 앞 `+0x62 XOR EDX,EDX` (둘째 인자 NULL), `+0x67 MOV R8D,0x800` (셋째 인자 플래그).
- **누구를**: `LoadLibraryExW` — KERNEL32 스텁을 지나 `KERNELBASE!LoadLibraryExW` 로 닿는다 (run101).
- **무엇을 위해**: `FlsGetValue2` 라는 연장이 들어 있을 첫 연장통을 연다. 선반 (`module_handles[1]`, rva `0x834a0 + 8`) 이 비었을 때만 이 줄을 탄다.
- 이 줄이 run7 이 본 `KERNELBASE!LoadLibraryExW` ×3 의 **돌아올 자리 (`+0x73`)** 다 (run40). 이번엔 그 세 번 가운데 **한 번이 어느 이름을 들고 가는지** 를 코드로 정했다.

#### 한 줄 읽기 ② — `try_get_function_slow +0x13c` (호출 아님 — 값이 바뀌는 순간)

`XCHG [.fptable + id*8], 주소 (없으면 -1)` `[정적]`

- **누가**: 같은 `try_get_function_slow`.
- **어디서**: `+0x13c`. 앞 `+0x11d` 에서 덮개를 열고 (4), 뒤 `+0x14e` 에서 닫는다 (2).
- **누구를**: 함수를 부르지 않는다. 공구 목록 칸 하나에 값을 **바꿔 넣는다**.
- **무엇을 위해**: id 3 (`FlsGetValue2`) 이면 칸은 `.fptable +0x18`. 넣는 값은 `GetProcAddress` 가 준 주소 = `ntdll!RtlFlsGetValue2` (2.5). 못 찾았으면 `-1`.
- 짐 지도 눈으로 보면 이 줄이 **dll 이미지 안 칸 하나를 놓는 순간** 이다 (baggage 7.6.13 첫 줄).

#### 한 줄 읽기 ③ — `dllmain_crt_process_detach +0x68` (call, ret `+0x6d`)

`__scrt_uninitialize_crt(ECX = CL, EDX = 0)` `[정적]`

- **누가**: dll 의 정리 담당 `dllmain_crt_process_detach` (이유 0).
- **어디서**: `+0x68`. 바로 앞 `+0x62 XOR EDX,EDX` (둘째 인자 0), `+0x64 MOVZX ECX,DIL` (첫째 인자 = 아까 봉투를 `TEST` 해서 만든 `CL`).
- **누구를**: `__scrt_uninitialize_crt`. 이 함수를 부르는 곳은 이것 하나다 (`refs` 1줄).
- **무엇을 위해**: CRT 를 정리한다. 무엇을 얼마나 정리할지는 `CL` 이 가른다 (2.10).

---

## (a) `FlsGetValue2` 길 — 은퇴 전, 지도 시간 안

### 2.3 출발점 — CRT 초기화 표의 7번 칸

#### 표가 뭐였나 `[정적]`

- `__acrt_execute_initializers` 는 `__acrt_initializers` 표 (dll1 rva `0x6fd50`) 를 앞에서부터 돈다.
- 칸은 **16개**. 칸마다 {시작 함수, 끝 함수} 한 쌍. 시작 함수를 `+0x25` (ret `+0x2a`) 에서 CFG 간접 호출 (교환원) 로 부른다.
- 실패하면 `+0x56` (ret `+0x5b`) 에서 거꾸로 끝 함수를 부른다.

#### 이번에 필요한 칸은 둘 `[정적]`

| 칸 (0부터) | 시작 함수 | 이번에 하는 일 |
|---|---|---|
| **2** | `__acrt_initialize_winapi_thunks` | 공구 목록에 덮개 씌우기 — `VirtualProtect` 한 번 (2.7) |
| **7** | `__acrt_initialize_ptd` | `FlsGetValue2` 찾기 → FLS 칸 만들기 (2.4 ~ 2.9) |

- "7번" 과 "8번째" 는 같은 칸이다. 0 부터 세면 7, 1 부터 세면 8번째다. run30 해설의 test.exe "8번째 쌍" 과 **같은 칸** 이다 (결과서).
- 2번이 7번보다 앞이라 **덮개가 먼저 씌워진 뒤에** 연장을 찾는다. 그래서 7번에서 적을 땐 덮개를 열어야 한다.
- 왜 지도 도구가 못 따라갔나: 표 안 함수는 "주소를 표에서 꺼내 부르는" 간접 호출이라 호출 목록에 이어지지 않는다 (run30 해설 "도구 메모"). 이번엔 표 바이트를 직접 읽어서 이었다.

#### 7번 칸 안의 차례 `[정적]`

1. `+0x4` (ret `+0x9`) `__acrt_tls2_supported` — "`FlsGetValue2` 를 쓸 수 있나?" 를 묻는다.
2. `+0x10` 그 답 (참/거짓) 을 칸 `__acrt_use_tls2_apis` (rva `0x831c0`) 에 적는다.
3. `+0x16` (ret `+0x1b`) `__acrt_FlsAlloc(destroy_fls)` — FLS 칸 번호 하나를 받아 `__acrt_flsindex` (rva `0x82160`) 에 적는다. 콜백 주소는 `+0x9 LEA RCX,[rva 0x49320]`.
4. `+0x2a` (ret `+0x2f`) `__acrt_getptd_noexit` — 첫 스레드의 acrt 칸을 만든다 (2.9).

### 2.4 어떤 연장을, 어느 연장통에서 — 이름표 셋

#### `__acrt_tls2_supported` 가 넘기는 것 `[정적]`

- 공구 목록 칸 `.fptable+0x18` 을 먼저 본다 (`+0x4` 읽기).
  - 0 이면 아직 안 찾아본 것 → 찾으러 간다.
  - `-1` 이면 전에 못 찾은 것 → 실패 처리.
  - 그 밖이면 이미 찾은 주소.
- 0 이면 `+0x32` (ret `+0x37`) `try_get_function_slow(ECX = 3, RDX = "FlsGetValue2", R8/R9 = 후보 id [1, 19])`.

#### 이름표 셋 — dll1 `.rdata` 원본 바이트 `[정적]`

| 항목 | dll1 rva (dll2) | 값 |
|---|---|---|
| 찾는 연장 이름 | `0x70f60` (`0x70f50`) | `FlsGetValue2` |
| 연장통 후보 id | `0x70f58` (`0x70f48`) | `[1, 19]` |
| `module_names[1]` | `0x709b0` | **`api-ms-win-core-fibers-l1-1-2`** |
| `module_names[19]` | `0x70e60` | **`kernelbase`** |

- 즉 "첫 연장통은 별명 `api-ms-win-core-fibers-l1-1-2`, 안 되면 `kernelbase`" 다.
- run30 의 test.exe 표와 **id · 이름이 같다.** 같은 UCRT 사본이 test.exe · dll1 · dll2 에 각자 들어 있으니 자연스럽다 (`/MT`).
- 요청서가 짐작한 `kernel32` · `fibers-l2` 는 **아니었다.** (문서에 적힌 적은 없는 짐작이라 정정할 곳은 없다 — run 문서 "정정".)

### 2.5 그 별명은 어디로 풀리나 — 별명 사전 → 안내문 → ntdll

여기가 이번 바퀴에서 **처음 생긴 답** 이다. 단계별로 `[정적: PE 표]`.

1. 별명 사전 (24H2 `apisetschema.dll` 의 `.apiset` 표) 에서 `api-ms-win-core-fibers-l1-1-2` 를 찾으면 → 기본 호스트 **`kernelbase.dll`**.
2. `kernelbase.dll` 의 export 표에서 `FlsGetValue2` 를 보면 → 코드가 아니라 **forwarder 문자열 `NTDLL.RtlFlsGetValue2`**.
   - 비유: 본 설비 건물 (KERNELBASE) 에 `FlsGetValue2` 호실 이름표는 있는데, 문에 **"관리실 (ntdll) `RtlFlsGetValue2` 호로 가세요" 안내문** 이 붙어 있다.
3. 그래서 → `GetProcAddress` 가 돌려주는 값 = **`ntdll!RtlFlsGetValue2`** (rva `0xd6930`).
4. 그 값이 2.2 ② 의 `XCHG` 로 공구 목록 `.fptable +0x18` 에 들어간다.

#### `ntdll!RtlFlsGetValue2` 는 무엇을 하나 `[정적: objdump]`

- 68바이트짜리 작은 함수다. Ghidra `.asm` 이 없어서 ntdll.dll 바이트를 `objdump` 로 봤다.
- `gs:[0x17c8]` (= TEB `FlsData`) 를 읽고 칸을 **찾기만** 한다. 다른 함수 호출도, syscall 도 없다.
- 비유: 이름표 뒤 사물함 열쇠꾸러미에서 칸 하나를 **들여다보기만** 하는 일.

#### test.exe 에도 그대로 — 왜 그렇게 말할 수 있나

- 근거: test.exe 의 길 A 도 같은 id · 같은 이름 (`fibers-l1-1-2` → `kernelbase`) 이다 (run30).
- 그래서 → test.exe 쪽 별명도 `kernelbase` 로 풀린다 (flow.md 2.10 이 이렇게 고쳐 적었다).
- 덤: `kernelbase` 는 이미 올라온 건물이라 길 A 는 **첫 시도에 열릴 것** 이다. 다만 `LdrLoadDll` 이 실제로 무엇을 돌려주는지는 동적 (Q31).
- 길 B 의 첫 별명 (`api-ms-win-core-localization-l1-2-1`) 이 어디로 풀리는지는 **이번에도 안 봤다.**

### 2.6 첫 연장통에서 끝난다 — 둘째 후보 · 재시도 갈래를 안 탄다

`try_get_function_slow` 안의 갈림을 길 A 에 맞춰 따라가면 `[정적]`:

1. 첫 후보가 `LoadLibraryExW` 로 열리면 → 곧바로 `+0x19e` (ret `+0x1a4`) `GetProcAddress(모듈, "FlsGetValue2")` → `+0x1a7 JMP +0xf2` 로 끝으로.
   - 그래서 **둘째 후보 `kernelbase` 로는 안 간다.**
2. 플래그 `0x800` 은 `LOAD_LIBRARY_SEARCH_SYSTEM32` 다. run40 과 같다.
3. "실패하면 플래그 0 으로 한 번 더" 갈래 (`+0xc1`, ret `+0xc7`) 는 조건이 셋이다: 실패 · 에러 `0x57` · 이름이 `api-ms-` (`+0x9a`) 도 `ext-ms-` (`+0xb0`) 도 아님.
   - 첫 후보는 `api-ms-` 로 시작한다. 그래서 → 실패하더라도 **이 재시도는 안 탄다.**
   - 둘째 후보 `kernelbase` 는 `api-ms-` 가 아니라서 재시도 갈래가 열려 있다 (안 가니까 이번 길엔 상관없음).
- 찾든 못 찾든, 칸에 적는 단계 (`+0xf0` 뒤) 는 탄다. 그 단계가 다음 절의 덮개 여닫기다.

### 2.7 유리 덮개 여닫기 — `NtProtectVirtualMemory` 적어도 3번

#### 새 말 — 쪽 보호 값

- 메모리 쪽 (페이지) 마다 "읽기만 되나 / 쓰기도 되나" 가 정해져 있다. `VirtualProtect` 가 그 값을 바꾼다.
- 이번에 나오는 값 둘: **2 = 읽기 전용**, **4 = 읽기+쓰기** (run 문서 · 결과서).
- 비유: 공구 목록 위 **유리 덮개**. 2 = 덮개 닫힘, 4 = 덮개 열림.
- 이 값은 커널이 쥐고 있어서, 바꾸려면 관리사무소에 부탁해야 한다 → syscall `NtProtectVirtualMemory`.

#### 덮개를 만지는 줄 셋 `[정적]`

| 번호 | 어디 (call) | 하는 일 | 조건 |
|---|---|---|---|
| ① | `__acrt_initialize_winapi_thunks +0x20` | `VirtualProtect(.fptable, 0x100, 2)` — 덮개 닫기 | **조건 없음** (`+0x0 ~ +0x20` 갈래 없음) |
| ② | `try_get_function_slow +0x11d` | `VirtualProtect(.fptable, 0x100, 4)` — 덮개 열기 | 연장 하나 풀 때마다 |
| ③ | `try_get_function_slow +0x14e` | `VirtualProtect(…, 2)` — 다시 닫기 | 연장 하나 풀 때마다 |

- 차례는 **2 → 4 → 2**. 끝 값은 읽기 전용이다.
- 주소는 `.fptable` = dll 베이스 + `0x8a000`, 크기 `0x100` 바이트.
  - 파일에 적힌 구역 속성은 읽기+쓰기다 (결과서 표). 그걸 ① 이 읽기 전용으로 바꿔 두는 것이다.

#### `VirtualProtect` 한 번 = syscall 한 번인가 — 단계별로

1. `[정적]` dll 이 부르는 `KERNEL32!VirtualProtect` 는 7바이트 스텁 `JMP [IAT 0x8b690]` 이다 (복도).
2. `[정적]` 그 IAT 칸의 별명 `api-ms-win-core-memory-l1-1-0` → 기본 호스트 `kernelbase.dll`.
3. `[정적]` `KERNELBASE!VirtualProtect +0x34` (ret `+0x3b`) 가 `NtProtectVirtualMemory(-1, …)` 를 부른다.
4. `[정적]` `KERNELBASE!VirtualProtect` 안에 `NtProtectVirtualMemory` 호출 줄이 **하나 더** (`+0x96`, ret `+0x9d`) 있다 (flow-writer 가 보탬).
   - 그런데 이 둘째 줄은 첫 결과가 `0xc0000045` 이고 `+0x6d RtlFlushSecureMemoryCache` 가 참일 때만 탄다 (`+0x5c CMP` · `+0x61 JNZ` · `+0x7b JZ`).
5. 그래서 → **보통 길에선 `VirtualProtect` 한 번 = `NtProtectVirtualMemory` 한 번.**

#### 몇 번인가 — 셈

- 길 A 하나만 세면: ① 1번 + ② ③ 2번 = **dll 마다 적어도 3번** `[정적]`.
- `try_get_function_slow` 는 연장 하나를 풀 때마다 (찾든 못 찾든) ② ③ 을 탄다.
- 그래서 dll 하나가 `DllMain` 앞에서 세 길 (A `FlsGetValue2` · B `LCMapStringEx` · C `AreFileApisANSI`) 을 다 타면 1 + 2×3 = **7번쯤** `[정적 + 추측: 세 길이 dll 에서 정말 세 번 타는지는 동적 Q31]`.
- 이 syscall 들은 **은퇴 전** 에 난다. 짐 지도 시간 안이다. 다만 놓이는 것 (dll 이미지 쪽 보호) 은 체크리스트 밖이라 7.6.13 에 따로 적었다 (3절).
- 덧붙여 test.exe 쪽도 같은 `VirtualProtect` 길이니 연장 하나마다 두 번일 것이다 `[정적 + 추측: test.exe 의 kernel32 스텁 길은 dll 것과 같다고 봄]` (flow.md 2.10). test.exe 쪽은 **은퇴 뒤** 라 짐 지도 범위 밖이다.

### 2.8 아직 셈에 없는 것 — 연장통 열기 · 연장 꺼내기 안쪽

- `LoadLibraryExW` → `KERNELBASE!LoadLibraryExW +0x130` (ret `+0x137`) `LdrGetDllPath`, `+0xf8` (ret `+0xff`) `LdrLoadDll` `[정적]`.
- `GetProcAddress` → `KERNELBASE!GetProcAddress +0x7b` (ret `+0x82`) `LdrGetProcedureAddressForCaller` `[정적]`.
- 이 두 ntdll 함수 **안에서** syscall 이 몇 번 나는지는 **안 읽었다** → 확인 필요.
  - 이미 올라온 `kernelbase` 를 돌려주는 길이라 0 이어도 이상하지 않다 `[추측]`.
  - 이게 dll 진입점 "호출 약속" (나와도 되는 syscall 목록) 의 **마지막 빈칸** 이다 → 정적 Q197 ①.
- `__acrt_lock(0xe)` (`+0xf9`) / `__acrt_unlock` (`+0x15a`) 는 `EnterCriticalSection` / `Leave…` 다 `[정적]`. 열쇠라서 혼자 돌 때 (안 막힐 때) 는 커널에 안 간다 `[추측]`.

### 2.9 찾은 주소는 누가 쓰나 — 그리고 첫 스레드의 FLS 칸

#### 쓰는 자리 `[정적]`

1. `__acrt_initialize_ptd +0x10` 이 "쓸 수 있다" 를 `__acrt_use_tls2_apis` 에 적는다 (2.3).
2. 나중에 `__acrt_getptd_noexit +0x16` 이 그 칸을 본다. 1 이면 (`+0x1d JZ +0x49` 안 탐) `+0x2e` (ret `+0x33`) 에서 `__acrt_FlsGetValue2` 를 부른다.
3. `__acrt_FlsGetValue2` 는 `MOV RAX,[.fptable+0x18]` · `JMP RAX` 두 줄이다. 즉 공구 목록에서 주소를 꺼내 **`ntdll!RtlFlsGetValue2` 로 바로 간다.**
4. 읽은 값이 0 이면 (이 스레드 칸이 아직 없음) `+0x3f` (ret `+0x44`) 에서 **이 스레드의 acrt 칸을 처음 만드는 함수** 를 부른다.
   - 이 함수는 같은 주소에 이름이 둘이다: `internal_get_ptd_head_slow` 와 C++ 꾸민 이름 하나. 같은 함수의 두 표기라 run 문서는 앞 이름을 쓴다.
- 0 이면 (`FlsGetValue2` 를 못 쓰면) `+0x49` 아래 옛 길로: `__acrt_FlsGetValue` (`+0x61`) → `+0x72` 같은 함수.

#### 처음 만드는 함수 안 `[정적]`

| call | 하는 일 |
|---|---|
| `internal_get_ptd_head_slow +0x21` | `__acrt_FlsSetValue(idx, -1)` — "만드는 중" 표시 |
| `+0x42` | `_calloc_base(1, 0x3c8)` — 창고에서 `0x3c8` 바이트 한 덩이 |
| `+0x68` | `__acrt_FlsSetValue(idx, 덩어리)` — 사물함 칸에 넣기 |
| `+0x8b` | `construct_ptd_array` |

- 비유: 직원 사물함 칸에 먼저 "공사 중" 쪽지를 붙이고, 창고에서 상자 하나를 받아와 칸에 넣고, 상자 안을 정리한다.

#### vcrt 쪽도 칸 하나 `[정적]` (flow-writer 가 보탬)

- `__vcrt_initialize_ptd +0xb` `FlsAlloc(__vcrt_freefls)` → 칸 번호를 `__vcrt_flsindex` (rva `0x82140`) 에.
- `+0x25` `FlsSetValue(idx, &__vcrt_startup_thread_ptd)` (rva `0x82d80`) — 첫 스레드는 창고 대신 **미리 만들어 둔 정적 칸** 을 쓴다.
- 이 함수를 누가 부르나: `__scrt_initialize_crt +0x14 __vcrt_initialize` 아래일 것 `[추측: __vcrt_initialize 본문을 안 뽑음]` → Q197 ④.

#### 사진과 맞대 보기 — TEB `FlsData` `[관찰]` + `[정적 길]`

- `[관찰]` (run44 사진 로그, flow-writer 가 다시 읽음) TEB `FlsData` (`+0x17c8`):
  - P1b: `(null)` (`ms/logs/04_baggage-read_P1b.txt:2769`)
  - P2: `0x00000259'96f3c160` (`_P2.txt:2781`)
- 즉 이 칸은 **P1b 와 P2 사이** (dll 전원 켜기 구간) 에 처음 채워졌다.
- `[정적]` 그 구간 안에서 처음 `RtlFlsSetValue` 를 부를 수 있는 코드로 이번에 **dll CRT 의 `FlsSetValue` 줄** 이 나왔다:
  - `__vcrt_initialize_ptd +0x25`, `internal_get_ptd_head_slow +0x21` 등 → KERNEL32 스텁 → `KERNELBASE!FlsSetValue +0x4` → `ntdll!RtlFlsSetValue`.
- `[추측]` 그중 어느 줄이 **첫 번** 인지 (dll2 의 vcrt 쪽일 것) 는 아직 모른다 (짐 지도 7.17 D3 그대로).
- 그래서 짐 지도 3.3 `FlsData` 줄은 "부르는 쪽 길" 만 `[추측]` → `[정적]` 으로 올라갔고, 첫 호출자는 `[추측]` 그대로다.

#### 곁가지 — 부르는 곳 없는 사본 둘 `[정적]`

- `try_get_FlsGetValue2` (`0x4eedc`) · `get_FlsGetValue2` 는 같은 일을 하는 사본인데 **부르는 곳이 없다** (`refs` 0줄).
- 진짜 길은 `__acrt_tls2_supported` 쪽이다. run30 · run40 의 "이름만 남은 사본" 과 같은 꼴이다.

---

## (b) 이유 0 — 프로세스 끝, 지도 밖

### 2.10 갈림은 봉투 하나 (`CL`)

#### 봉투에 무엇이 오나 — 부르는 쪽 둘 `[정적]` (ntdll 에 있던 `.asm`)

| 언제 | 부르는 줄 | 봉투 (`lpReserved`) | `CL` |
|---|---|---|---|
| **프로세스 끝** | `LdrShutdownProcess +0x17a` → `LdrpCallInitRoutine(…, R9 = R13 = 1)` (`+0x57 LEA R13D,[R12+1]` · `+0x16d MOV R9,R13`) | 1 | **1** |
| **`FreeLibrary` 로 내릴 때** | `LdrpProcessDetachNode +0x143` → `LdrpCallInitRoutine(…, R9 = 0)` (`+0x136 XOR R9D,R9D`) | 0 | **0** |

- dll 쪽에선 `dllmain_crt_dispatch +0x47 TEST R8,R8` · `+0x4a SETNZ CL` 로 봉투를 "비었나 / 뭐가 들었나" 만 확인한다 (run40 해설 2.5 의 그 줄).
- sample 은 dll 을 **정적으로 import** 하니 `main` 이 끝날 때 **`CL = 1` 쪽만** 탄다 `[정적 + 소스]`.

#### `__scrt_uninitialize_crt` 안의 다른 가림 — dll 길에선 안 갈린다 `[정적]`

- 이 함수 (rva `0x23c0`, 43바이트) 에는 둘째 인자 `DL` 로 가르는 줄이 있다 (`+0x6 ~ +0x14`).
- 안쪽을 통째로 건너뛰는 조건: `is_initialized_as_dll ≠ 0` **이고** `DL ≠ 0`.
- 단계별로:
  1. `is_initialized_as_dll` (rva `0x82d20`) 은 `__scrt_initialize_crt +0x8` 이 `ECX = 0` 일 때 켠다. dll 길은 `ECX = 0` 으로 불렀다 (2.1 트리 `__scrt_initialize_crt(0)`) → 1.
  2. 그런데 dll 길은 `DL = 0` 이다 (2.2 ③ `+0x62 XOR EDX,EDX`).
  3. 그래서 → 건너뛰기는 **안 탄다.** 늘 `+0x16 __acrt_uninitialize(CL)` · `+0x1e __vcrt_uninitialize(CL)` 로 간다.
- `DL ≠ 0` 길은 이 dll 의 `exit` / `_cexit` 쪽일 것 `[추측]` (요청 밖).

#### `CL` 이 가르는 것 `[정적]`

| `CL` | `__acrt_uninitialize` (rva `0x21b14`) | `__vcrt_uninitialize` (rva `0x32b0`) |
|---|---|---|
| **1** (프로세스 끝) | `__piob` (rva `0x839c0`) ≠ 0 이면 **`_flushall` 만** (`+0x12`). 정리 표는 안 돈다 | 아무것도 안 함 (`+0x6 JNZ +0x12`) |
| **0** (`FreeLibrary`) | `+0x30` 꼬리 `JMP __acrt_execute_uninitializers` — 표 끝에서 거꾸로 끝 함수들 | `__vcrt_uninitialize_ptd` (`FlsFree`) · `__vcrt_uninitialize_locks` |

- `CL = 0` 쪽 표 거꾸로 돌기에서 눈에 띄는 것:
  - 7번 `__acrt_uninitialize_ptd` → `FlsFree(__acrt_flsindex)`
  - 2번 `__acrt_uninitialize_winapi_thunks` → `module_handles` 칸마다 `FreeLibrary` (아까 연 연장통을 닫음)
- 비유: 가게 문을 **아예 닫을 때 (`CL = 1`)** 는 쌓인 우편만 내보내고 (`_flushall`) 나머지 정리는 안 한다. 어차피 건물째 사라지니까. **한 건물만 빼낼 때 (`CL = 0`)** 는 사물함 반납, 연장통 반납, 열쇠 정리까지 다 한다.
  - "어차피 사라지니까" 는 비유일 뿐이다. 코드가 그 이유를 적어 둔 건 아니다.

#### sample 에서 `_flushall` 이 정말 불리나 — 단계별로 `[정적]`

1. `CL = 1` 길에서 `_flushall` 은 `__piob ≠ 0` 일 때만 부른다.
2. `__piob` 를 쓰는 곳은 `__acrt_initialize_stdio` (rva `0x52060`) 하나다 (`_refs/0x839c0.tsv`).
3. 그 함수는 `.CRT$XI` 표 (`.CRT$XIA` rva `0x6e320`, 둘째 항목 `0x6e330`) 에 있다.
4. 그 표는 `dllmain_crt_process_attach +0x6b` (ret `+0x70`) `_initterm_e` 가 돈다 (run40 해설 2.8 표의 `+0x70`).
5. 그래서 → 이유 1 때 `__piob` 가 채워지고, 프로세스 끝에 **`_flushall` 이 불린다.**
- `_flushall` 은 CRT 스트림 버퍼에 남은 것을 내보내는 함수로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 그 안에서 `WriteFile` (→ `NtWriteFile`) 까지 가는지는 **안쪽을 안 봐서 모른다.** 우리 `Log` 는 CRT 스트림을 안 쓴다 (`sample/log.h`) `[소스]` → 정적 Q197 ③.

---

## (c) 이유 2 · 3 — 새 스레드, 지도 밖

### 2.11 이유마다 가는 곳 — 그리고 `DllMain` 앞 · 뒤

#### `dllmain_crt_dispatch` 의 가름 (rva `0x1990`, 86바이트) `[정적]`

| 이유 | 가는 곳 |
|---|---|
| 0 | `+0x51` 꼬리 `JMP` `dllmain_crt_process_detach` (2.10) |
| 1 | `+0x42` 꼬리 `JMP` `dllmain_crt_process_attach` (run40) |
| **2** | `+0x2e` (ret `+0x33`) **`__scrt_dllmain_crt_thread_attach`** |
| **3** | `+0x21` (ret `+0x26`) **`__scrt_dllmain_crt_thread_detach`** |
| 그 밖 | 그냥 1 |

- 순서 (run40 해설 2.6 "이유 0 · 3 은 순서가 반대" 와 같은 모양):
  - 이유 2 는 `DllMain` **앞** (`dllmain_dispatch +0x76`). 0 이 오면 `+0x83 JZ +0x11e` 로 `DllMain` 을 건너뛴다.
  - 이유 3 은 `DllMain` (`+0x91`) **뒤** (`+0xe8`).
  - 비유: 새 직원이 오면 **사물함부터 만들어 주고** 손님 (`DllMain`) 을 부른다. 직원이 떠날 땐 **손님 배웅 뒤에** 사물함을 비운다.
- 정말 오나: sample `DllMain` 은 `DisableThreadLibraryCalls` 를 안 부른다 (`DllMain.asm`). 그래서 `CreateThread` 하나면 이유 2 · 3 이 온다 `[정적 + 소스]`.
- `_CRT_INIT +0x21` · `+0x2e` 도 이 두 함수를 부르지만, `_CRT_INIT` (rva `0x1d00`) 은 부르는 곳이 없는 **안 쓰는 사본** 이다 (`refs` 0줄).

#### 이유 2 — 사물함 칸 둘 만들기 (`__scrt_dllmain_crt_thread_attach`, rva `0x2110`, 41바이트) `[정적]`

1. `+0x4` `__vcrt_thread_attach` → `__vcrt_getptd_noexit`:
   - vcrt 칸이 비었으면 `FlsSetValue(idx, -1)` → **`calloc(1, 0x80)`** → `FlsSetValue(idx, 덩어리)` → 덩어리 `+0x78` 에 `0xfffffffe`.
   - 실패면 `AL = 0`.
2. `+0xd` `__acrt_thread_attach` → `__acrt_getptd_noexit`:
   - `__acrt_use_tls2_apis` 가 1 이면 `RtlFlsGetValue2` 로 먼저 읽는다 (2.9 와 같은 함수).
   - 비었으면 `internal_get_ptd_head_slow` → **`_calloc_base(1, 0x3c8)`** + `construct_ptd_array`.
3. acrt 가 실패하면 `+0x16 __vcrt_thread_detach` 로 되돌린다.
- 새 스레드라 TEB `FlsData` 가 0 으로 시작하니 `RtlFlsGetValue2` 는 0 을 줄 것이다 (flow.md 3장 — "새 TEB `FlsData` 0 → 0").
- 주 스레드는 이미 이유 1 때 칸이 있으니 (2.9) 읽기만 한다.

#### 이유 3 — 칸 비우고 덩이 돌려주기 (`__scrt_dllmain_crt_thread_detach`, rva `0x2140`, 21바이트) `[정적]`

1. `+0x4` `__acrt_thread_detach` → `__acrt_freeptd`:
   - `FlsGetValue` → 값이 있으면 **`FlsSetValue(idx, 0)` 먼저** (칸부터 비움) → `destroy_ptd_array` → `_free_base` (창고에 반납).
2. `+0x9` `__vcrt_thread_detach` → `__vcrt_freeptd_for_this_thread`:
   - `FlsSetValue(idx, 0)` → 값이 시작 스레드용 정적 칸 `__vcrt_startup_thread_ptd` 가 아니면 `free`.
3. `+0xe MOV AL,1` — **늘 1** 을 돌려준다.

### 2.12 TLS 가 아니라 FLS — 그리고 파괴자 콜백

#### TLS 인가 FLS 인가 `[정적]`

- 둘 다 **FLS** 다. `TlsAlloc` 쓰는 줄이 없다 (dll 이 `TlsAlloc` 을 import 하지도 않는다 — 결과서).
- `.tls` 구역 (로더가 채우는 암시적 TLS, run28) 도 이 두 함수는 **안 건드린다.**

#### 새 말 — FLS 파괴자 콜백

- `FlsAlloc` 으로 칸 번호를 받을 때 함께 넘기는 함수 주소다. 칸 주인이 사라질 때 그 칸을 치우라고 불러 달라는 함수로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유: 사물함 칸에 붙여 두는 **"내가 떠나면 이 번호로 연락해 치워 주세요" 쪽지**.
- 이번에 나온 둘 `[정적]`:
  - acrt `destroy_fls` (rva `0x49320`) — `__acrt_initialize_ptd +0x9 LEA RCX` → `+0x16 FlsAlloc`
  - vcrt `__vcrt_freefls` (rva `0x19920`) — `__vcrt_initialize_ptd +0x4 LEA RCX` → `+0xb FlsAlloc`

#### 그럼 스레드가 끝날 때 콜백이 일을 하나 — 짐작

1. 근거 `[정적]`: 이유 3 은 칸을 **먼저** `FlsSetValue(idx, 0)` 으로 비운다.
2. 그래서 → 그 뒤에 ntdll 이 FLS 콜백을 불러도 이 칸은 **비어 있을 것** `[추측]`.
3. 아직 확인 아님: ntdll 이 콜백을 **언제 · 어디서** 부르나는 이번 범위 밖이다 → 정적 Q197 ② · 동적 Q31.

### 2.13 이유 2 · 3 의 syscall — dll 코드엔 없다

한 칸씩 안쪽으로 따라가면 `[정적]`:

1. dll 코드: `syscall` 명령 0개 (dll1 `.asm` grep 0).
   - 부르는 건 KERNEL32 가져오기 (`GetLastError` · `SetLastError` · `FlsGetValue` · `FlsSetValue` · `HeapAlloc` · `HeapFree`) 와 `FlsGetValue2` 포인터뿐.
2. 한 칸 안쪽 (KERNELBASE): `FlsAlloc` / `FlsSetValue` / `FlsGetValue` / `FlsFree` 는 **얇은 겉포장** 이다.
   - 각각 `+0xe` / `+0x4` / `+0x1f` / `+0x4` 에서 `ntdll!RtlFlsAlloc` / `RtlFlsSetValue` / `RtlFlsGetValue` / `RtlFlsFree` 를 부르고, 실패면 `BaseSetLastNTError`.
3. 두 칸 안쪽 (ntdll): 이미 있던 `RtlFlsSetValue.asm` 에 `SYSCALL` 0개.
   - 안에서 부르는 것: `RtlpFlsHeapAlloc` (`+0x94` · `+0xc0`), `RtlAcquireSRWLockExclusive` (`+0x10b`), `RtlReleaseSRWLockExclusive` (`+0x142`), `memset` (`+0x176`).
4. `HeapAlloc` · `HeapFree` 는 힙 함수 (`RtlAllocateHeap` / `RtlFreeHeap`) 다.
5. 그래서 → 직접 syscall 은 없고, **창고 (힙) 가 모자라거나 비울 때만** 커널로 간다 (`NtAllocateVirtualMemory` / `NtFreeVirtualMemory`).
- 실제로 나는지는 정적으로 못 정한다 → 동적 (Q31 에 붙은 run109 몫, "범위 밖" 줄).

---

### 2.14 dll1 과 dll2 는 같다 — 주소만 다르다

- `[정적]` 이번에 읽은 함수 **31개** 가 주소만 빼면 명령 · 안쪽 오프셋까지 같다.
- `.data` 칸은 두 dll 이 **같은 rva**:
  - `__vcrt_flsindex` `0x82140` · `__acrt_flsindex` `0x82160` · `is_initialized_as_dll` `0x82d20` · `__acrt_use_tls2_apis` `0x831c0` · `module_handles` `0x834a0` · `.fptable` `0x8a000`
- `.rdata` 칸 (이름 · id 배열 · `module_names`) 과 함수 시작 rva 만 dll2 가 `0x10` 앞이다.
- 비유 (run40 해설 2.9 그대로): 단지에 **같은 설계도로 지은 건물 두 채**. 방 배치는 같고, 몇몇 표지판 자리만 한 칸 당겨져 있다.

### 2.15 덤 — run84 의 lowio `[추측]` 이 반쯤 풀렸다

- run84 해설 2.13 은 "sample `/MT` CRT 의 lowio 초기화가 dll 준비 (은퇴 전) 에서 불릴 수 있다" 를 `[추측]` 으로 남겼다.
- 이번에 2.3 의 표를 읽어 보니 `__acrt_initializers` **8번이 `__acrt_initialize_lowio`** 다. 그래서 dll2 · dll1 CRT 준비 (은퇴 전) 에서 **불린다** `[정적: 표 순서]`.
- 그 안이 run84 의 `initialize_stdio_handles_nolock` (`GetStdHandle` · `GetFileType`) 을 부르는지는 **안쪽을 안 읽어서** 여전히 `[추측]` 이다 → 정적 Q197 ③.
- 그래서 "반쯤" 이다. "불리나" 는 풀렸고, "불려서 무엇을 하나" 는 남았다.

### 2.16 PE_emulator 쪽 — 짧게 (결과서 요약, flow-writer 는 코드를 안 읽음)

- (a) **확인 필요.** 보호 syscall 은 엔진이 직접 대답하는 종류라 3번은 괜찮을 것 `[추측]`.
  - dll 진입점 "호출 약속" 에 `NtProtectVirtualMemory` ×3 이상과 `LoadLibraryExW` · `GetProcAddress` 의 ntdll 안 syscall 이 들어 있는지는 못 봤다.
  - `PE_emulator/Src` 에 `FlsGetValue2` · `RtlFls*` · `fibers-l1` 을 직접 다루는 줄은 grep 0. 진짜 kernelbase · ntdll 코드가 돌아서 `.fptable+0x18` 에 `ntdll!RtlFlsGetValue2` 를 써야 `__acrt_use_tls2_apis` = 1 이 된다.
- (b) **확인 필요.** 프로세스 끝 (`CL = 1`) 엔 `FreeLibrary` · `FlsFree` 정리가 없다는 게 정적 답이다. `_flushall` 이 `NtWriteFile` 을 내는지는 모른다.
- (c) **아직 못 탐.** 새 스레드 길은 PE_emulator 의 `NtCreateThreadEx` 가 아직 기본 대답이라 (Worker 안 돎) 안 나온다.

### 2.17 정리 — 비유로 한 번 더

- dll 의 준비 담당은 개업 날, 공구 목록에 먼저 **유리 덮개** 를 씌운다 (①).
- 그다음 "`FlsGetValue2` 연장 있나요?" 하고 첫 연장통 이름표 (`api-ms-win-core-fibers-l1-1-2`) 를 연다. 별명 사전을 보면 그 통은 이미 들어와 있는 **본 설비 건물 (kernelbase)** 이다.
- 그 건물의 `FlsGetValue2` 호실엔 **"관리실 `RtlFlsGetValue2` 호로 가세요" 안내문** 이 붙어 있다. 그래서 공구 목록엔 관리실 연장 주소가 적힌다. 적을 땐 덮개를 열고 (②) 적고 (`XCHG`) 닫는다 (③). 덮개를 여닫을 때마다 관리사무소에 한 번씩 부탁한다 (`NtProtectVirtualMemory`).
- 그 연장으로 첫 직원 **이름표 뒤 사물함** (FLS) 을 들여다보고, 비었으면 창고에서 상자를 받아 넣는다.
- 새 직원이 오면 손님보다 먼저 사물함을 만들고 (이유 2), 떠나면 손님 배웅 뒤 사물함을 비운다 (이유 3). 관리사무소는 창고가 모자랄 때만 찾는다.
- 가게 문을 아예 닫을 땐 (이유 0, `CL = 1`) 우편만 내보내고 (`_flushall`) 끝. 한 건물만 뺄 때 (`CL = 0`) 에만 사물함 · 연장통을 다 반납한다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

그림은 **안 바뀌었다.** CRT 한 상자 원칙 (2026-10-23 결정 "진입점 = 경계, CRT 한 상자") 그대로다. 진입점 상자 → `?` `KERNELBASE!LoadLibraryExW ×3` 노드도 그대로다. 은퇴 전 `NtProtectVirtualMemory` ×3 이상은 **글로만** 적었다.

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 | — | kernelbase · kernel32 · ntdll · dll1/dll2 pdb 줄에 run109 몫, 근거 로그에 "정적 바퀴, TEB `FlsData` 두 줄만 다시 읽음" |
| 2.8 지도 줄 | "test.exe 길 A (`FlsGetValue2`) 는 함수 포인터 표를 거쳐 지도가 못 따라가서 dll 쪽은 안 봤다" | (해결됨, run109) **dll 에도 길 A 가 있다** `[정적]`. 세 번이 실제로 A · B · C 인지는 그대로 동적 Q31 |
| 2.8 `Log` 줄 | lowio 초기화가 불리는지 `[추측]` | 표 8번이라 **불린다** `[정적: 표 순서]`, 안쪽은 `[추측]` (2.15) |
| 2.8 새 절 | — | "CRT 안쪽 더 — 길 A · 은퇴 전 syscall · 이유 0 · 2 · 3 (참고 — 지도 범위 밖)" |
| 2.10 | "`api-ms-…` 가 24H2 에서 어디로 풀리는지는 정적으로 모름" | 길 A 첫 후보 `fibers-l1-1-2` → **`kernelbase.dll`**, `FlsGetValue2` = forwarder `NTDLL.RtlFlsGetValue2` `[정적: PE 표]`. 길 B 는 그대로 모름 |
| 2.10 | test.exe 쪽 "앞뒤로 `VirtualProtect`" (run30 `[정적]`) | `KERNELBASE!VirtualProtect +0x34` 로 가니 API 하나마다 `NtProtectVirtualMemory` 두 번일 것 `[정적 + 추측]` (은퇴 뒤라 짐 지도 밖) |
| 3장 `test!Worker` | — | dll 이유 2 (`DllMain` 앞, FLS 칸 둘) · 이유 3 (`DllMain` 뒤, 칸 비우기) 한 줄씩 `[정적]`. ntdll FLS 콜백 자리 `[공백]` |
| 4장 | 2.8 Q73 줄 열림 | Q73 줄 **해결됨**. 2.8 · 2.10 Q31 줄에 run109 몫 덧붙임. run109 새 줄 셋 (동적 · 정적 · PE_emulator) |
| 그림 메모 (그림 2 (2)) | 세 길 후보가 무엇인지 `?` | dll 에도 길 A 가 있어 후보는 test.exe 와 같은 A · B · C `[정적]`. 그림엔 `NtProtectVirtualMemory` 안 넣음 |
| 5장 | — | run109 · sast/105 줄 |

### 짐 지도 `baggage.md` 에서 바뀐 것

- **2장 숫자 그대로.** 이번에 나온 칸은 다른 모듈 (sample dll 이미지 · 힙) 이라 체크리스트 밖이다.
- 3.3 TEB `FlsData` 줄: 부르는 쪽 길이 `[추측]` → **`[정적]`** (dll CRT `FlsSetValue` → `KERNELBASE!FlsSetValue +0x4` → `RtlFlsSetValue`). 첫 호출자는 `[추측]` 그대로 (D3).
- 새 **7.6.13**: dll CRT 가 은퇴 전에 놓는 칸 (`.fptable+0x18` · `module_handles[1]` · `__acrt_use_tls2_apis` · FLS 색인 둘 · `__vcrt_startup_thread_ptd` · `is_initialized_as_dll` · `__piob` · 첫 스레드 acrt 덩이) 과 커널 쪽 `.fptable` 쪽 보호 (끝 값 읽기 전용).
  - 비유: 점검표 본문 (2장) 이 아니라 **부록** 에 "이 건물 안에서 생기는 준비물" 을 따로 적은 것.
- 새 **7.40**: Q73 이 넘긴 질문 모음.

### 큰 그림에서 어떻게 이어지나

- 은퇴 전 syscall 목록에 **`NtProtectVirtualMemory` 가 dll 마다 적어도 3번** 이 코드로 붙었다. PE_emulator 의 dll 진입점 "호출 약속" 에 바로 쓰이는 숫자다.
  - 그 약속의 **마지막 빈칸** 은 `LdrLoadDll` · `LdrGetProcedureAddressForCaller` 가 ntdll 안에서 내는 syscall 수다 → Q197 ① (중요).
- TEB `FlsData` 가 P1b → P2 사이에 생기는 이유가 "dll CRT 가 FLS 칸을 만들어서" 라는 **길** 로 이어졌다. 첫 호출자만 남았다.
- (b) · (c) 는 지도 밖이지만, 다음 범위 (새 스레드 · 종료) 로 갈 때 쓸 바닥을 미리 깔았다.
- 다음 바퀴들은 이미 정적이 끝난 다른 결과서들 (sast/106 ~ 120) 의 문서 차례다 (run 문서 "환경" — 이번 문서엔 안 넣음).

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 (동적)

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| dll 쪽 `LoadLibraryExW` 첫 번이 정말 `api-ms-win-core-fibers-l1-1-2` · `r8d = 0x800` 인가, 돌려준 `rax` = kernelbase 베이스인가, `k` 에 길 A (`__acrt_tls2_supported+0x37` → `__acrt_initialize_ptd+0x9` → `__acrt_execute_initializers+0x2a`) 가 보이나 | 실행 때 값. 정적은 코드 모양까지 | **Q31** (동적) |
| 세 번이 정말 A · B · C 인가 (그래서 `NtProtectVirtualMemory` 가 7번쯤인가) | 표 안 더 깊은 간접 호출까지는 못 봄, 횟수는 실행 때 | **Q31** |
| `NtProtectVirtualMemory` 가 `VirtualProtect` ret 자리 (dll1 `+0x509fe` · `+0x4f6bb` · `+0x4f6ec`; dll2 −`0x10`) 마다 한 번씩, 보호 `2 → 4 → 2` 인가 | 실행 때 | **Q31** |
| TEB `FlsData` 를 처음 채우는 `RtlFlsSetValue` 의 호출자 (`dll2!__vcrt_initialize_ptd+0x2b` 인가) | 후보 줄은 여럿, 첫 번은 실행 순서 | **Q31** (짐 지도 7.17 D3 과 같은 질문) |
| 은퇴 때 (P2) dll 칸 값 — `.fptable+0x18` = ntdll + `0xd6930`, `__acrt_use_tls2_apis` = 1, `.fptable` 쪽 = `PAGE_READONLY`, `module_handles[1]` = kernelbase, FLS 색인 둘, `is_initialized_as_dll` = 1, `__piob` ≠ 0 | run44 사진은 dll 칸을 안 읽었다 | **Q79** (이미 있는 P2 덤프를 오프라인으로 — 새 실행 없이 닫힐 수 있음) |
| (범위 밖) 새 스레드 이유 2 · 3 사이 syscall 수 (예상 0), ntdll 이 FLS 콜백을 부르나 | 지도 밖, 실행 때 | **Q31** 의 run109 몫 "범위 밖" 줄 |
| (범위 밖) 프로세스 끝 `cl` = 1 · `_flushall` 이 서나 · 그 사이 `NtWriteFile` | 지도 밖, 실행 때 | **Q31** 의 run109 몫 "범위 밖" 줄 |

### 정적으로 남은 것 → Q197

| 번호 | 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|---|
| **①** (ntdll, 중요) | `LdrLoadDll` 이 별명 이름 (`0x800`) 을 이미 올라온 kernelbase 로 풀어 돌려줄 때 syscall 수, `LdrGetProcedureAddressForCaller` 도 | 이번 요청은 sample dll 이 대상이라 ntdll 프로젝트를 안 건드림 |
| **②** (ntdll) | `RtlFlsAlloc` 이 TEB `FlsData` 를 건드리나, `RtlpFlsHeapAlloc` 첫 칸, `LdrShutdownThread` · `LdrpInitializeThread` 의 FLS 콜백 자리, 7.6.13 ntdll 몫 FLS 전역을 3.8 줄과 맞추기 | 같은 이유 (있던 `RtlFlsSetValue.asm` 만 읽음) |
| **③** (dll1, 낮음) | `__acrt_initialize_stdio` · `_flushall` · `__acrt_initialize_lowio` 안쪽 — 프로세스 끝 `NtWriteFile` 조건, run84 `initialize_stdio_handles_nolock` `[추측]` | 범위 밖이라 이름만 |
| **④** (dll1, 작음) | `__vcrt_initialize` 본문 — `__vcrt_initialize_ptd` 를 부르는 자리 | 본문을 안 뽑음 (2.9 `[추측: 사이]`) |

### 이번 바퀴가 일부러 안 본 것

- 길 B (`api-ms-win-core-localization-l1-2-1`) 가 24H2 에서 어디로 풀리는지. 이번 질문은 길 A 였다.
- `DL ≠ 0` 길 (`exit` / `_cexit` 쪽일 것 `[추측]`). 요청 밖.
- `../PE_emulator` 코드 자체 (flow-writer 는 안 읽음, 결과서 요약만 옮김). 고칠지는 사용자 결정.

## 5. 더 읽기

- run 문서: [`run109-dll-crt-small.md`](run109-dll-crt-small.md) — "정적 — 호출 지점" 표 (call · ret 전부), "호출 관계" 트리 전체
- 요청서: [`_workspace/sast/105_dll-crt-small.md`](../../_workspace/sast/105_dll-crt-small.md)
- 결과서: [`_workspace/sast/105_dll-crt-small_result.md`](../../_workspace/sast/105_dll-crt-small_result.md) — 초기화 표 16칸 전부, 정리 표 거꾸로 차례, 근거 줄 모음
- flow.md: [`flow.md`](flow.md) 2.8 "CRT 안쪽 더 — 길 A · 은퇴 전 syscall · 이유 0 · 2 · 3", 2.10, 3장 `test!Worker`, 4장 run109 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.3 `FlsData`, 7.6.13, 7.40
- 앞 바퀴 해설:
  - [run30 해설](../02_sprint/run30-exe-crt-start-해설.md) — test.exe 길 A · B · C, 연장통 · 선반 · 공구 목록
  - [run40 해설](../02_sprint/run40-dll-crt-entry-해설.md) — dll 현관 안 (정문 경비 · 접수처 · 준비 담당), 봉투, `LoadLibraryExW` ×3 의 주인
  - [run84 해설](run84-console-crt-해설.md) — `/MT` CRT 의 lowio, 2.13
  - [run101 해설](run101-k32-leftovers-해설.md) — kernel32 스텁 → ApiSet → kernelbase

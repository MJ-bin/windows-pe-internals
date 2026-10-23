# run28 tls-init-call — 해설

> 이 글은 [run28 문서](run28-tls-init-call.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **정정 (run36)**: 2.3 표 "DLL 쪽 `LdrpHandleTlsData`" 를 부르는 곳이 나왔다 `[정적]` — `LdrpDoPostSnapWork +0x8e` (ret `+0x93`), 항목 `TlsIndex == 0` 일 때만 (snap 끝, 도장 5 바로 앞). 2.5 의 "일꾼이 같은 자물쇠를 `LdrpHandleTlsData` 에서 독점으로 잡을 수 있는지는 정적으로 못 정한다" 는 "snap 일감을 일꾼이 맡으면 **길은 코드상 있다** `[정적: 길]`" 로 좁혀졌다. 실제로 그런지는 그대로 동적 Q25·Q47. 본문은 그대로 두었다 → [run36 해설 2.9](run36-snap-module-해설.md#29-질문-4--끝맺음-ldrpdopostsnapwork)

- 대기열: **Q16** — 2.7 `LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `ImageTlsCallbackCaller` 본문 (`_workspace/plan.md`, 이번에 **끝**)
  - 새 항목 둘 (둘 다 작은 정적):
    - **Q55** — sample dll 의 `_DllMainCRTStartup` → `DllMain` 이 `lpReserved`(셋째 인자)를 그대로 넘기나.
    - **Q56** — TLS 락 세부 (TEB `+0x1850` 표, 추적 칸 비트, `RtlAcquireSRWLockExclusive` 갈래, `[RDI+0x58]` 조건).
  - 동적으로 볼 것(결과서 D1 ~ D8)은 새 번호 없이 **Q25·Q26·Q27·Q28** 에 붙였다 (오케스트레이터 결정).
  - 다음 항목: **Q17** — 2.10 로더 끝 → 진입점 (`NtContinue`, `RtlUserThreadStart`).
- 근거:
  - run 문서 [`run28-tls-init-call.md`](run28-tls-init-call.md) (가장 중요)
  - 요청서 [`_workspace/sast/23_tls-init-call.md`](../../_workspace/sast/23_tls-init-call.md)
  - 결과서 [`_workspace/sast/23_tls-init-call_result.md`](../../_workspace/sast/23_tls-init-call_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (run17 ~ run27 과 같은 파일).
  - 본문을 읽은 건 **네 함수**다:

| 함수 | rva | 크기 | 직접 호출 |
|---|---|---|---|
| `ntdll!LdrpCallTlsInitializers` | `0x10e50` | 963바이트 | 6곳 + 예외 필터 1곳 |
| `ntdll!LdrpCallInitRoutine` | `0x10ba0` | 667바이트 | 8곳 + `__finally` 조각 4곳 |
| `ntdll!LdrpCallInitRoutineInternal` | `0x11eab8` | 47바이트 | 1곳 (CFG 간접) |
| `ntdll!ImageTlsCallbackCaller` | `0xe83e0` | 55바이트 | 2곳 (CFG 간접 1, 직접 1) |

  - 셋째 `LdrpCallInitRoutineInternal` 은 요청 때 지도에서 찾아 범위에 더했다. 넷째 `ImageTlsCallbackCaller` 는 호출 지도에 없어서 이름으로 뽑았다 (sast/05: 호출 참조가 없고 주소만 꺼내 쓰는 함수).
  - 곁에서 몇 함수는 **호출 자리·칸 오프셋만** 봤다 (TLS 항목을 누가 만드나, 누가 이 함수를 부르나, CFG 모양).

이 글은 [run27 해설](run27-init-graph-해설.md) 의 **다음 편**이다.

- run27 은 관리실의 **켜기 담당**(`LdrpInitializeNode`)이 건물마다 **직원 첫 안내(TLS) → 전원 스위치(진입점)** 를 누른다는 데까지 봤다.
- 그때 두 버튼 **안쪽**은 안 열었다. "버튼을 누르면 정확히 무슨 일이 일어나나" 가 이번 바퀴다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](run9-create-user-process-해설.md) 2절
  - 직접 호출과 간접 호출 (냉장고 메모) → [run9 해설](run9-create-user-process-해설.md) 2절 "직접 호출과 간접 호출"
- PEB, rva, 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
- [예외 보호 구역](run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일) (try/except, 사고가 나면 **정해진 착지점**으로)
- 예외 착지점(**착지 매트**), unwind 범위 표(**비상 대피도**) → [run27 해설](run27-init-graph-해설.md) 2.9
- 독점 잠금 / 공유 잠금 (명부실 열쇠) → [run23 해설 2.3](run23-load-dependent-해설.md#23-핵심--줄간선은-여기서-긋는다)
- ETW (관리실 **작업 일지**) → [run20 해설](run20-kernel32-load-해설.md) 2.11
- thunk (**교환원**), CFG → [run20 해설 2.9](run20-kernel32-load-해설.md#29-910-마지막-인사--kernelbasepostinit)
- TLS 콜백, 활성화 컨텍스트, `DLL_PROCESS_ATTACH` (= 이유 1) → [run19 해설 2.11](run19-proc-init-modules-해설.md#211-exe-tls-콜백--전원-다-켠-뒤에)
- 켜기 담당, 진입점 넷째 인자, `lpReserved` (스위치와 함께 건네는 **메모**) → [run27 해설](run27-init-graph-해설.md) 2.7 · 2.8
- 시작 함수 = PE 진입점 → [run25 해설](run25-dll-entry-해설.md) 2.3

비유도 그대로 이어 쓴다.

- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- DLL = **건물**, PEB.Ldr = **건물 명부**, 모듈 항목(`_LDR_DATA_TABLE_ENTRY`) = **명부 한 줄**
- DDAG 노드 = **연결 카드**, `State` = 카드의 **진행 도장**
- `LdrpInitializeGraphRecurse` = **순서 담당**, `LdrpInitializeNode` = **켜기 담당**
- TLS 콜백 부르기 = 건물 **직원 첫 안내**, 진입점(시작 함수) 부르기 = **전원 스위치**
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번에 새로 나오는 사람들 (flow.md 2.7 의 비유와 같다. 비유 이름일 뿐이다):
  - `LdrpCallInitRoutine` = **공용 창구** — 관리실이 유저 코드(건물 사람)를 부를 때 늘 거치는 창구
  - `LdrpCallInitRoutineInternal` = **창구 직원** — 서류 칸을 한 칸씩 옮겨 적고 첫 칸의 사람에게 넘긴다
  - `ImageTlsCallbackCaller` = **TLS 전용 안내원** — 진짜 TLS 콜백을 불러 주고, 창구엔 늘 "잘 됐음" 이라고 답한다
  - flow.md 는 이걸 "잘 됐음 **도장**" 이라고 적었다. 이 글에서는 카드의 **진행 도장**(State)과 헷갈리지 않게 **"잘 됐음" 대답**이라고 쓴다.

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 다섯**:

1. **콜백 명단은 명부 줄이 아니라 관리실 TLS 서류철(`LdrpTlsList`)에서 꺼낸다.** 자물쇠는 찾을 때만 잠근다 `[정적]` → 2.3 · 2.5
2. **명단 한 칸 = 공용 창구 한 번**, 0 이 나올 때까지. 안 부르는 조건이 셋이고, 그중 하나가 run8 의 "kernel32 길에서 콜백이 없던" 수수께끼와 맞는다 `[정적]` → 2.4
3. **"한 칸 밀기" 는 `LdrpCallInitRoutineInternal` 에서 일어난다.** run27 의 `[추측]` 이 맞았다. 자리만 한 층 안쪽이다 `[정적]` → 2.7
4. **`ImageTlsCallbackCaller` 는 어댑터다.** 콜백을 `(DllBase, 이유, 0)` 으로 부르고 늘 TRUE 를 돌려준다 `[정적]` → 2.8
5. **정정: TLS 콜백 예외는 켜기 담당까지 안 온다.** 안쪽 `LdrpCallTlsInitializers` 가 먼저 잡는다. run27 해설 2.9 의 한 줄이 틀렸다 `[정적]` → 2.10

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
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)              (run17 ~ run19)
        ├─ +0x19a2  kernel32 준비 → … → Node(KERNELBASE) / Node(KERNEL32)            (run20·run21·run25·run27)
        │                    └─ +0x16b TLS (콜백 없음),  +0x19c 시작 함수            ← 이번 (같은 코드)
        ├─ +0x1aa9 ~ +0x1ab1  짐 들이기 (dll1·dll2 올리기)                           (run22 ~ run24·run26)
        ├─ +0x1bbb  순서 담당 GR (exe 카드) → Node [dll2] [dll1]                     (run27)
        │     └─ Node +0x16b  LdrpCallTlsInitializers   (직원 첫 안내)              ← 이번
        │     └─ Node +0x19c  LdrpCallInitRoutine       (전원 스위치)               ← 이번 (창구 안쪽)
        └─ +0x1c99  LdrpCallTlsInitializers (exe TLS)                               ← 이번 (같은 함수)
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16·run25, 뒤는 다음 Q17)
```

- 전원 켜기 구간(`+0x1bbb`)의 **가장 안쪽**이다. 이 아래는 이제 유저 코드(dll 의 TLS 콜백, `_DllMainCRTStartup`)다.
- 같은 함수들이 **kernel32 준비 때**, **exe TLS 때**, **새 스레드 때**에도 쓰인다. 그래서 이번 결과는 그쪽 관찰들도 같이 설명한다.
- flow.md 로는 **2.7** 이 중심이다. **2.5**, **2.8**, **2.9**, 2.4, 3장(새 스레드)에도 줄이 더해졌다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. `LdrpCallTlsInitializers` 는 TLS 콜백 목록을 **어디서** 꺼내나? 콜백마다 도나? 무엇을 넘기나? **언제 하나도 안 부르나**? 락·예외는?
2. `LdrpCallInitRoutine` 은 인자를 **그대로** 넘기나? 무엇을 돌려주나?
3. run27 이 짐작한 "**한 칸 밀기**"(`DllMain` 이 `(DllBase, 1, r9)` 를 받음)는 **어디서** 일어나나?
4. `ImageTlsCallbackCaller` 는 진짜 콜백을 **어떻게** 부르고, **왜** 끼어 있나?
5. 그림 2 의 `LdrpCallInitRoutine` → `?` → `ImageTlsCallbackCaller` 칸을 함수 사슬로 채울 수 있나?

### 바퀴 전에 알던 것

- `[관찰]` (run7·run8) dll 마다 `LdrpInitializeNode` 안에서 **TLS(`+0x16b`) → 시작 함수(`+0x19c`)**.
- `[관찰]` (run8) `LdrpCallTlsInitializers +0x19b` 에서 `rcx = ntdll!ImageTlsCallbackCaller`.
- `[관찰]` (run8) `LdrpCallInitRoutine` 의 `r8 = 1` 이 바로 뒤 `DllMain` 의 `rdx = 1` 과 같았다.
- `[관찰]` (run8) kernel32 길에서 `LdrpCallTlsInitializers` 는 **한 번** 불렸는데 **콜백 호출이 없었다**.
- `[정적]` (run27) 켜기 담당이 싣는 값:
  - TLS: `LdrpCallTlsInitializers(ECX = 1, RDX = 명부 줄)`, 조건 `TlsIndex ≠ 0` 뿐, 반환값은 버림.
  - 진입점: `LdrpCallInitRoutine(EntryPoint, DllBase, 1, R9)`. `R9` = 정적 import 면 `LdrpProcessInitContextRecord` 값, 아니면 0.
- `[추측]` (run27) `LdrpCallInitRoutine` 이 인자를 **한 칸씩 밀어** `DllMain(DllBase, 1, r9)` 로 넘긴다.
- `[추측]` (flow.md 2.7) `ImageTlsCallbackCaller` = 진짜 TLS 콜백을 대신 불러 주는 중간 함수. `LdrpCallInitRoutine` = 유저 코드를 부르는 공용 창구.
- `[소스]` test.exe·dll1·dll2 는 **모듈마다 TLS 콜백이 하나**씩 있다 (`ExeTls`, `Dll1Tls`, `Dll2Tls`).

---

## 2. 찾은 것

### 2.1 한눈에 — TLS 콜백 하나가 불리기까지

run 문서 "호출 관계" 를 줄였다. 숫자는 run 문서 그대로. 모두 `[정적]` 이다. 잡일(로그·ETW)과 락 경합 갈래는 뺐다.

```
LdrpInitializeNode  +0x16b (call +0x166)          [exe 는 LdrpInitializeProcess +0x1c99]
└─ LdrpCallTlsInitializers(이유, 명부 줄)
    ├─ 자물쇠(공유) → LdrpTlsList 에서 이 명부 줄의 장 찾기 → 자물쇠 풂
    ├─ 명단 = [그 장 +0x28]  (0 이면 끝)
    └─ 한 바퀴 +0x132 ~ +0x19b   ← 전체가 __try +0x12b ~ +0x1a2
        └─ +0x19b  LdrpCallInitRoutine(ImageTlsCallbackCaller, DllBase, 이유, 콜백)    공용 창구
             └─ +0x93  LdrpCallInitRoutineInternal                                     창구 직원: 한 칸 밀기
                  └─ +0x22  ImageTlsCallbackCaller(DllBase, 이유, 콜백)                 TLS 전용 안내원
                       └─ +0x1b  콜백(DllBase, 이유, 0)   → 늘 TRUE 로 바꿔 돌려줌

진입점 길 (같은 창구, 안내원만 없음)
LdrpInitializeNode  +0x19c (call +0x197)
└─ LdrpCallInitRoutine(EntryPoint, DllBase, 1, r9)
    └─ +0x93  LdrpCallInitRoutineInternal
         └─ +0x22  _DllMainCRTStartup(DllBase, 1, r9)
```

- `+0x22`·`+0x1b` 사이에는 **CFG 중개**(2.9)가 있지만, 콜스택에 안 남아서 그림에서 뺐다.
- **TLS 길과 진입점 길은 같은 창구·같은 창구 직원을 지난다.** 다른 건 TLS 쪽에만 안내원이 한 명 더 낀다는 것뿐이다.

### 2.2 한 줄씩 읽는 법

run 문서 "호출 지점" 표에서 둘을 골랐다. "누가 / 어디서 / 누구를 / 무엇을 위해" 로 읽는다.

#### 한 줄 읽기 ① — 명단 한 칸마다 창구를 부르는 줄

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpCallTlsInitializers` | `+0x196` | `+0x19b` | `LdrpCallInitRoutine` | **콜백마다** | [정적] + [관찰] run8 (`rcx`) |

- **누가**: `LdrpCallTlsInitializers`. 한 건물의 TLS 콜백 명단을 한 칸씩 보는 중이다.
- **어디서**: `+0x196` 에서 부르고 `+0x19b` 로 돌아온다. run8 콜스택에 찍힌 `LdrpCallTlsInitializers+0x19b` 가 이 줄이다.
- **누구를**: 공용 창구 `LdrpCallInitRoutine`. 실어 보내는 네 칸 (이름으로):
  - `rcx` = `ImageTlsCallbackCaller` 주소 (`+0x18f`) — run8 이 본 그 값
  - `rdx` = **DllBase** (`+0x18b`, 명부 줄 `+0x30`)
  - `r8` = **이유** (`+0x188`, 입구 `ECX` 에서)
  - `r9` = **진짜 콜백 주소** (`+0x185`, 명단 칸에서 꺼낸 값)
- **무엇을 위해**: 명단 한 칸의 콜백을 **창구를 통해** 부르려고. 돌아오면 `+0x19b JMP +0x132` 로 한 바퀴 머리로 간다. 창구가 돌려준 `AL` 은 **안 본다**.
- 눈여겨볼 것: 진짜 콜백은 **첫 칸이 아니라 넷째 칸(`r9`)** 에 실린다. 첫 칸은 안내원이다. 왜 이렇게 싣는지가 2.7 · 2.8 의 이야기다.

#### 한 줄 읽기 ② — 안내원이 진짜 콜백을 부르는 줄

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!ImageTlsCallbackCaller` | `+0x16` | `+0x1b` | **CFG 간접 호출** (대상 = 받은 `r8` = 진짜 콜백) | 콜백 ≠ `RtlAcquireSRWLockExclusive` | [정적] |

- **누가**: TLS 전용 안내원 `ImageTlsCallbackCaller`.
- **어디서**: `+0x16` 에서 부르고 `+0x1b` 로 돌아온다. 콜백 안에서 `k` 를 찍으면 **바로 위 프레임**이 `ImageTlsCallbackCaller+0x1b` 일 것이다 (예측, 2.9).
- **누구를**: 이름이 명령에 박혀 있지 않다 ([간접 호출](run9-create-user-process-해설.md)). 받은 셋째 칸(`r8`)에 들어 있던 **진짜 콜백**이다. test.exe 라면 `Dll2Tls`·`Dll1Tls`·`ExeTls` 일 것 `[소스]`.
- **무엇을 위해**: 진짜 콜백을 `(DllBase, 이유, 0)` 으로 부르려고. 부르기 직전 `+0x13` 에서 셋째 칸을 0 으로 바꾼다.
- **조건**: 콜백 주소가 `RtlAcquireSRWLockExclusive` 와 같으면 이 줄 대신 `+0x29` 직접 호출로 간다 (2.8 끝). sample 콜백은 그 주소가 아니라 이 줄을 탄다 `[소스]` + `[정적]`.

### 2.3 핵심 ① 콜백 명단은 관리실 서류철에서 꺼낸다

#### 새 말 — TLS, TLS 디렉터리, `AddressOfCallBacks`

- **TLS** (Thread Local Storage) = 스레드마다 **따로** 갖는 변수 칸으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 직원마다 하나씩 주는 **개인 사물함**.
  - TLS 콜백은 [run19 해설 2.11](run19-proc-init-modules-해설.md#211-exe-tls-콜백--전원-다-켠-뒤에) 에서 본 대로 "붙을 때 먼저 불러 달라" 고 적어 둔 함수다.
- **TLS 디렉터리** = PE 파일 안에 "이 모듈은 TLS 를 쓴다" 를 적어 둔 작은 표다. PE 규격의 `IMAGE_TLS_DIRECTORY64` 이고, 그 안의 **`AddressOfCallBacks`** 칸이 **콜백 주소 배열**을 가리킨다 (PE 규격 기준 `[정적]`).
  - 배열은 콜백 주소를 차례로 적고 **0 으로 끝난다** (이번 코드가 0 에서 멈추는 걸로 확인, 2.4).
  - 비유: 건물이 관리실에 낸 **TLS 신청서**. 그 신청서 한 칸에 "**켤 때 부를 사람 명단**은 몇 쪽에 있음" 이 적혀 있다.

#### 답 — 명부 줄이 아니라 전역 목록 `[정적]`

run27 까지는 "켜기 담당이 **명부 줄**을 넘기니까, 명부 줄 어딘가에서 콜백을 꺼내겠지" 정도로 짐작할 수 있었다. 코드는 달랐다.

1. 입구에서 명부 줄(`RDX`)을 `RDI` 에, 이유(`ECX`)를 `R12D` 에 챙긴다 (`+0x1a`, `+0x1d`).
2. 전역 **`LdrpTlsList`** (PDB 이름, rva `0x1cc938`) 를 돈다 (`+0x81`).
3. 목록 항목마다 `+0x98 CMP [항목+0x38], RDI` — **이 명부 줄을 가리키는 항목**을 찾는다.
4. 못 찾으면 0 (`+0xa7`). 찾으면 그 항목의 **`+0x28`** 을 읽는다 (`+0x11d`) = 콜백 배열 주소.

- → **콜백 목록은 명부 줄(`_LDR_DATA_TABLE_ENTRY`)의 칸이 아니라 `LdrpTlsList` 항목에서 꺼낸다.**
- 비유: 관리실에는 건물 명부와 별도로 **TLS 서류철**이 있다. 건물마다 한 장씩 꽂혀 있고, 그 장 뒤에 "어느 명부 줄 건물의 장인지" 가 적혀 있다. 담당은 서류철을 넘기며 **이 건물의 장**을 찾는다.

#### 서류철 한 장 (TLS 목록 항목, 0x48바이트) — PDB 타입 없음

PDB 에 이 구조체 이름이 없다. 그래서 **오프셋 + 역할**로만 적는다 (run 문서 "먼저 알아 둘 칸").

| 칸 | 역할 | 비유 |
|---|---|---|
| `+0x00` / `+0x08` | 다음 / 앞 (서로 이어진 원형 목록) | 서류철 고리 |
| `+0x10 ~ +0x37` | **TLS 디렉터리 사본** 0x28바이트 | 신청서 사본 |
| └ `+0x28` | **`AddressOfCallBacks`** (사본 시작 `+0x10` + 규격 `0x18`) | "부를 사람 명단" 위치 |
| `+0x38` | 명부 줄 포인터 | "○○ 건물의 장" 표시 |
| `+0x40` | TLS 번호 | 사물함 번호 |

- `+0x28` = `AddressOfCallBacks` 는 **PE 규격 배치**로 읽은 것이다 `[정적]` (PDB 가 아니라 PE 규격 기준).

#### 서류철 한 장은 누가 끼우나 — 그리고 `TlsIndex = 0xffff` `[정적]`

곁에서 호출 자리·칸만 봤다.

| 무엇 | 누가 |
|---|---|
| 장 하나 만들기 (0x48, 신청서 사본 0x28 복사, TLS 번호, 서류철 꼬리에 끼움) | `LdrpAllocateTlsEntry` (rva `0xa6764`) |
| DLL 쪽 | `LdrpHandleTlsData` — TLS 디렉터리가 있으면 `+0x10b` (ret `+0x110`) 에서 위 함수, **`+0x3a8` 명부 줄 `TlsIndex` = `0xffff`** |
| exe 쪽 | `LdrpInitializeTls` (`LdrpInitializeProcess +0x18cb`, ret `+0x18d0`) — `+0xf5` (ret `+0xfa`) 에서 위 함수, **`+0x102` exe 명부 줄 `TlsIndex` \|= `0xffff`** |
| 스레드마다 사물함 묶음 (TLS 벡터) | `LdrpAllocateTls` |

- 그래서 켜기 담당·세팅 담당이 보던 조건 **"`TlsIndex ≠ 0`" = "TLS 디렉터리가 있어서 서류철에 장이 끼워졌다"** 는 뜻이다 `[정적]`.
- 오래된 `[추측]` 두 개가 풀렸다:
  - "`TlsIndex` 를 쓰는 곳 = `LdrpInitializeTls` 쪽" ([run19 해설 2.11](run19-proc-init-modules-해설.md#211-exe-tls-콜백--전원-다-켠-뒤에) 끝) → **exe 쪽은 맞다.** DLL 쪽은 `LdrpHandleTlsData` 다.
  - "TLS 데이터 마련도 이 함수가 하나?" → **아니다.** `LdrpCallTlsInitializers` 는 할당·복사 호출이 없다. **콜백 부르기만** 한다 `[정적]`.
- 비유: 신청서를 받아 서류철에 끼우고 명부 줄에 "TLS 있음(`0xffff`)" 표시를 하는 사람은 따로 있다. 이번 함수는 **끼워진 서류철을 보고 사람을 부르기만** 한다.

### 2.4 핵심 ② 명단 한 칸 = 창구 한 번, 0 까지

#### 한 바퀴 — 단계별로 `[정적]`

범위 `+0x132 ~ +0x19b`.

1. `+0x132` 명단 칸을 읽는다 (`RSI = [RBX]`).
2. `+0x138` 그 값이 **0 이면 끝** (`+0x19d` → `+0xe7`).
3. `+0x142` **부르기 전에** 다음 칸으로 넘겨 둔다 (`RBX += 8`).
4. `+0x180` 로그 `LdrpLogInternal` (ret `+0x185`) — `"Calling TLS callback %p for DLL \"%wZ\" at %p\n"`. 실제로 찍히는지는 그 안쪽 몫이다.
5. 네 칸을 싣고 `+0x196` 공용 창구 (ret `+0x19b`) — 2.2 ① 그대로.
6. `+0x19b JMP +0x132` — 1번으로.

- **콜백 하나 = 창구 한 번.** 명단에 셋이면 세 번이다.
- **이유 값은 거르지 않는다.** 이 함수 안에 이유를 비교하는 줄이 없다. 1 이든 2 든 그대로 콜백에 간다.
- sample 은 모듈마다 콜백이 하나다 `[소스]`. 그래서 한 바퀴 머리(`+0x132`)에 **두 번** 서고, 둘째에 0 을 만나 끝날 것이다 (결과서 D3 예측, 동적 Q25).
- 비유: 담당이 명단을 위에서부터 한 줄씩 읽는다. 이름이 있으면 창구로 부르고, **빈 줄이 나오면 끝.**

#### 하나도 안 부르는 조건 셋 `[정적]`

| | 조건 | 자리 | 비유 |
|---|---|---|---|
| (a) | `LdrpTlsList` 에 이 명부 줄의 장이 없다 | `+0xe2` | 서류철에 이 건물 장이 없음 |
| (b) | 장은 있는데 `AddressOfCallBacks` 가 0 | `+0x129` | 신청서에 "명단 위치" 칸이 비었음 |
| (c) | 명단 첫 칸이 0 | `+0x138` | 명단은 있는데 첫 줄부터 빈 줄 |

#### 그래서 kernel32 길 수수께끼 — 단계별로

run8 `[관찰]`: kernel32 준비 때 `LdrpCallTlsInitializers` 는 **한 번** 불렸는데 **콜백 호출이 없었다**. run27 은 "한 번만 불린 건 조건(`TlsIndex ≠ 0`)으로 설명되지만, 왜 콜백이 없었는지는 Q16" 이라고 남겼다.

1. 결과서가 PE 파일을 직접 읽었다 `[정적: PE 파일]` (`pefile`):

| 모듈 | TLS 디렉터리 | 콜백 배열 |
|---|---|---|
| `kernel32.dll` | 없음 | — |
| `KernelBase.dll` | **있음** | **첫 칸이 0** (파일 값, RVA `0x2ab070`) |
| `ntdll.dll` | 없음 | — |

2. kernel32 는 TLS 디렉터리가 없다 → 서류철에 장이 안 생긴다 → `TlsIndex` 가 0 그대로 → 켜기 담당이 `LdrpCallTlsInitializers` 를 **아예 안 부른다** (2.3).
3. KERNELBASE 는 디렉터리가 있다 → `TlsIndex ≠ 0` → 불린다. 그런데 명단 첫 칸이 0 → 조건 **(c)** 로 콜백 없이 끝.
4. → 관찰된 "한 번 불렸고 콜백 없음" 과 **딱 맞는 후보**가 KERNELBASE 다.
5. 하지만 그 한 번이 정말 KERNELBASE 였는지는 실행 때 값이다 → `[추측]`, 동적 **Q25** (입구 `ntdll+0x10e50` 에서 `BaseDllName`).

- flow.md 그림 2 에 원래 있던 `[KERNELBASE 추정]` 과 같은 쪽이다. run27 해설 2.4 에서 "재귀 안 = KERNELBASE" 짐작을 했는데, 그 짐작과도 또 맞는다.
- 비유: KERNELBASE 는 TLS 신청서를 **내긴 냈는데 명단이 비어 있는** 건물이다. 담당은 서류철에서 장을 찾아 명단을 펴 보고, 첫 줄이 비었으니 그냥 닫는다.

### 2.5 자물쇠 — 서류철을 찾는 동안만 `[정적]`

#### 공유로 잠그고, 찾으면 바로 푼다

- 서류철 자물쇠 = 전역 **`LdrpTlsLock`** (PDB 이름, rva `0x1d2460`, SRW 락).
- [공유 잠금](run23-load-dependent-해설.md#23-핵심--줄간선은-여기서-긋는다) 으로 잡는다. 빠른 길은 함수 안에 바로 쓰여 있다 (`+0x6d CMPXCHG`). 막히면 `+0x113 RtlpAcquireSRWLockSharedContended` (ret `+0x118`).
- 푸는 것도 함수 안 (`+0xae`). 막히면 같은 함수 안 느린 길 `+0x1f5 ~ +0x3d1`.
- **자물쇠를 쥐는 건 서류철 찾기(`+0x66 ~ +0xb7`)뿐이다.** 명단을 돌며 콜백을 부르는 동안엔 **안 쥔다.**
- 비유: 서류철 보관함을 열어 이 건물 장을 찾고, 명단 위치만 적어 들고 나와서 **보관함을 잠근 뒤에** 사람들을 부른다.
- 왜 그렇게 하나는 코드에 쓰여 있지 않다. 콜백(유저 코드)이 오래 걸리거나 다시 로더를 부를 수 있으니 그동안 자물쇠를 쥐지 않는 게 안전해 보인다 — 이건 내 짐작이다 `[추측]` (일반적으로 알려진 설명은 아님, 코드 모양에서 짐작).

#### 경합 갈래 셋은 콜백과 상관없다

- `+0x1e8 NtSetInformationThread`, `+0x1fe RtlRaiseStatus(0xc0000264)`, `+0x386 NtAlertThreadByThreadIdEx` — 셋 다 **자물쇠 풀기·기록 정리 쪽**이다.
- 다른 스레드와 자물쇠가 겹치지 않으면 셋 다 안 탄다.
- 시동 때 로더 일꾼이 같은 자물쇠를 `LdrpHandleTlsData` 에서 **독점**으로 잡을 수 있는지는 정적으로 못 정한다 → 동적 **Q25** (세 자리에 안 서야 한다).
- 자물쇠 앞뒤로 TEB `+0x1850` 이 가리키는 **8칸 표**에 자물쇠 주소를 적었다 지운다. run23 의 "SRW 락 소유 추적" `[추측]` 과 같은 모양이다. PDB 이름(`SchedulerSharedDataSlot`)은 쓰임새와 안 맞아서 이름은 `[추측]` 으로 둔다 → 정적 **Q56**.

### 2.6 공용 창구 `LdrpCallInitRoutine` — 그대로 넘기고, 그대로 돌려준다 `[정적]`

#### 받은 넷을 손대지 않는다

- 시작에서 네 칸을 챙긴다: `+0x26 RSI=R9`, `+0x29 R14D=R8D`, `+0x2c R15=RDX`, `+0x2f RDI=RCX`.
- 부르기 직전 **같은 순서로** 다시 싣는다: `+0x82 R9=RSI`, `+0x85 R8D=R14D`, `+0x88 RDX=R15`, `+0x8b RCX=RDI`.
- `+0x8e` 창구 직원 `LdrpCallInitRoutineInternal` (ret `+0x93`).
- 돌려주는 값 = 창구 직원의 `AL` **그대로** (`+0x93` 저장 → `+0xec MOVZX EAX,BL`). 가공 없음.
- 그러니 **한 칸 밀기는 이 함수가 아니다.** run27 은 "이 함수가 민다" 고 짐작했는데, 미는 건 한 층 안쪽이다 (2.7).

#### 창구 앞뒤로 하는 일 — 기록·사고 대비

| 일 | 언제 |
|---|---|
| [ETW](run20-kernel32-load-해설.md) 이벤트 (부르기 전 `+0x1b5 ~ +0x213` 이벤트 `0x14a3`, 부른 뒤 `+0x27c` 이벤트 `0x1496`) | 세 조건이 **모두**일 때만: 공용 데이터 로거 바이트(`0x7ffe0384`) ≠ 0, `PEB.TracingFlags`(`+0x378`) bit 2 `LibLoaderTracingEnabled`, 로거 높은 바이트 `& 0x20` |
| 오류 로그 `+0x12f LdrpLogError(0xc0000142, 0x1496, 이유, 0)` | **결과 `AL == 0` 이고 이유 `== 1`** 일 때만 |
| `__finally` (`__try` `+0x82 ~ +0x97`, 정리 `$fin$0`) | **예외로 빠져나가도** 끝 이벤트·오류 로그를 남긴다 |

- **새 말 — `__finally`**: [예외 보호 구역](run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일)의 한 종류로, 정상으로 끝나든 사고로 빠져나가든 **꼭 하는 마무리**를 적어 두는 자리로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 사고를 **잡지는 않는다.** 마무리만 하고 사고는 위로 그대로 보낸다 `[정적]` (이 함수의 경우).
  - 비유: 창구가 문 닫을 때 **일지에 "끝" 한 줄 쓰기.** 손님이 소란을 피우고 나가도 일지는 쓴다. 소란은 위층에서 처리한다.
- 이유가 2·3 이면 FALSE 여도 오류 로그는 없다. 상태값 `0xc0000142` 를 실제로 정하는 건 부르는 쪽이다 (run27 `LdrpInitializeNode +0x298`).
- ETW 가 test.exe 에선 꺼져 있을 것이다 `[추측]` → 동적 **Q27** (세 ETW 자리에 안 서야 한다).
- 스택을 바꾸거나 다른 스레드로 넘기는 일은 **없다** (`RSP` 는 프롤로그·에필로그에서만 바뀜).

#### 정말 "공용" 창구인가 — 부르는 곳 8곳 `[정적]`

`LdrpInitializeNode`, `LdrpCallTlsInitializers`, `LdrpInitializeThread`, `LdrpProcessDetachNode`, `LdrShutdownProcess`, `LdrShutdownThread`, AppVerifier 둘 (`AVrfpLoadAndInitializeProvider`, `AvrfMiniLoadDll`).

- 켜기·TLS·새 스레드·떼어 내기·프로세스 끝·스레드 끝 — **관리실이 유저 코드를 부르는 일은 거의 다 이 창구를 지난다.** flow.md 의 `[추측]` "공용 창구" 가 **맞다** `[정적]`.

### 2.7 핵심 ③ 한 칸 밀기 — 창구 직원 `LdrpCallInitRoutineInternal`

사용자가 이 말을 처음 본다는 눈높이로 천천히 간다.

#### 새 말 — 인자 칸

- x64 Windows 에서 함수를 부를 때 **앞 네 인자**는 레지스터 `rcx`, `rdx`, `r8`, `r9` 에 **이 순서대로** 싣는 걸로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 첫째 인자 = `rcx`, 둘째 = `rdx`, 셋째 = `r8`, 넷째 = `r9`.
- 비유: 서류에 **네 칸**이 있다. 1칸·2칸·3칸·4칸. 받는 사람은 "1칸에 적힌 게 내 첫째 인자" 라고 읽는다.

#### 새 말 — 한 칸 밀기

- **한 칸 밀기** = run 문서·flow.md 가 쓰는 말이다. 받은 서류의 **1칸을 "부를 사람" 으로 떼어 내고**, 2·3·4칸을 **1·2·3칸으로 한 칸씩 당겨 적어** 그 사람에게 넘기는 것.
- 비유: 창구에 낸 서류가 `[받는 사람 | A | B | C]` 이면, 창구 직원은 받는 사람을 보고 그 사람에게 `[A | B | C]` 를 건넨다.
- 왜 이런 모양이 필요하냐면 — 창구는 **누구를 부를지(1칸)와 그 사람에게 줄 것(2·3·4칸)을 한 번에** 받아야 하기 때문이다. 받는 사람(`DllMain`, TLS 콜백)은 인자가 셋뿐이다.

#### 코드에서 — 줄마다 `[정적]`

47바이트짜리 함수다. 하는 일은 사실상 이게 전부다.

| 줄 | 받은 것 | 넘길 자리 |
|---|---|---|
| `+0xf MOV R11,RCX` → `+0x17 MOV RAX,R11` | 받은 `rcx` (1칸) | `rax` = **부를 함수** |
| `+0xc MOV R10,RDX` → `+0x1a MOV RCX,R10` | 받은 `rdx` (2칸) | 새 `rcx` (1칸) |
| `+0x4 MOV EAX,R8D` → `+0x12 MOV EDX,EAX` | 받은 `r8d` (3칸) | 새 `rdx` (2칸) |
| `+0x14 MOV R8,R9` | 받은 `r9` (4칸) | 새 `r8` (3칸) |
| `+0x1d CALL` (CFG 중개) → ret `+0x22` | — | `rax` 를 부름 |
| `+0x22 ~ +0x2e` | 결과 `AL` | 그대로 돌려줌 |

- 대상 함수는 **`(받은 rdx, 받은 r8d, 받은 r9)`** 를 받는다. 자기 `__try` 는 없다.

#### 진입점 길에 대 보면 — run27 짐작의 판정

| 단계 | `rcx` | `rdx` | `r8` | `r9` / `rax` |
|---|---|---|---|---|
| `LdrpInitializeNode +0x197` (run27) | EntryPoint | DllBase | 1 | `r9` = CONTEXT 포인터 또는 0 |
| `LdrpCallInitRoutineInternal +0x1d` | DllBase | 1 | `r9` 값 | `rax` = EntryPoint |
| `_DllMainCRTStartup` 받는 것 | `hinstDLL` | `fdwReason = 1` | `lpReserved` | |

1. run8 `[관찰]`: `LdrpCallInitRoutine` 의 `r8 = 1` → `DllMain` 의 `rdx = 1`.
2. 코드: `+0x4`·`+0x12` 가 받은 `r8d` 를 새 `rdx` 로 옮긴다 `[정적]`. 관찰과 **같은 모양**이다.
3. → run27 의 "한 칸 밀어 `DllMain(DllBase, 1, r9)`" 은 **맞다** `[정적]`. 다만 미는 곳은 `LdrpCallInitRoutine` 이 아니라 **`LdrpCallInitRoutineInternal`** 이다.
4. 정적 import DLL 의 셋째 인자 = `LdrpProcessInitContextRecord` 값 — 여기까지가 `[정적]` (run19 + run27 + 이번).
5. 아직 아닌 것 둘:
   - `_DllMainCRTStartup` 이 그걸 `DllMain` 의 `lpReserved` 로 **그대로** 넘기나 — CRT 안이라 안 봤다 → 정적 **Q55** (sample dll).
   - 그 값이 실제로 CONTEXT 포인터인가 — 실행 때 값 → 동적 **Q27**.

- 비유로: run27 해설에서 "스위치를 누를 때 **첫 직원 업무 메모**를 같이 건넨다" 고 했다. 이번에 **누가 그 메모를 어느 칸에 옮겨 적어 건네는지**가 보였다 — 창구 직원이 4칸의 메모를 3칸으로 당겨 적는다.

#### 왜 이 47바이트를 그림에 따로 그렸나

- TLS 길과 진입점 길이 **둘 다** 지난다.
- 한 칸 밀기가 일어나는 **유일한 곳**이다. 이게 없으면 "`r8 = 1` 이 `DllMain` 의 `rdx = 1` 이 된다" 가 설명이 안 된다.
- 콜스택에도 `+0x22` 로 보일 자리다 (2.9).
- 그래서 flow.md 그림 2 에서 `LdrpCallInitRoutine` 상자 **7개** 뒤에 모두 `(정적)` 상자로 넣었다 (TLS 셋, 진입점 넷).

### 2.8 핵심 ④ TLS 전용 안내원 `ImageTlsCallbackCaller` — 어댑터

#### 새 말 — 어댑터 (중간 함수)

- **어댑터** = 모양이 서로 안 맞는 두 쪽 사이에 끼워서 맞춰 주는 짧은 중간 함수다. run 문서·결과서가 `ImageTlsCallbackCaller` 를 이렇게 불렀다.
- 비유: **돼지코(전원 변환 플러그).** 콘센트(공용 창구)와 플러그(TLS 콜백)의 모양이 달라서, 사이에 끼워 꽂히게 해 준다.
- 무엇이 안 맞나 — 둘이다 (아래 "왜 끼우나").

#### 하는 일 — 줄마다 `[정적]`

- 받는 것 `(rcx, rdx, r8) = (DllBase, 이유, 진짜 콜백)` — 창구 직원이 한 칸 밀어 준 모양이다.
- `+0x4 RAX = R8` — 진짜 콜백 주소를 "부를 함수" 자리로.
- `+0x13 XOR R8D,R8D` — **셋째 칸을 0 으로.**
- `rcx`·`rdx` 는 손 안 댄다.
- `+0x16` CFG 중개로 `rax` 를 부름 (ret `+0x1b`).
- `+0x1b MOV EAX,1` — **콜백이 뭘 돌려주든 1(TRUE)** 로 덮어 돌려준다.

- → **콜백은 `(DllHandle = DllBase, Reason = 이유, Reserved = 0)` 을 받는다.**

레지스터가 옮겨 가는 모양 (TLS 길 전체, run 문서 그대로):

| 단계 | `rcx` | `rdx` | `r8` | `r9` / `rax` |
|---|---|---|---|---|
| `LdrpCallTlsInitializers +0x196` | `ImageTlsCallbackCaller` | DllBase | 이유 | `r9` = 콜백 |
| `LdrpCallInitRoutine +0x8e` | (그대로) | (그대로) | (그대로) | (그대로) |
| `LdrpCallInitRoutineInternal +0x1d` | DllBase | 이유 | 콜백 | `rax` = `ImageTlsCallbackCaller` |
| `ImageTlsCallbackCaller +0x16` | DllBase | 이유 | **0** | `rax` = 콜백 |
| 콜백 받는 것 | `DllHandle` | `Reason` | `Reserved = 0` | |

- 읽는 법: 진짜 콜백 주소가 **4칸 → 3칸 → `rax`** 로 옮겨 가며 결국 "부를 함수" 가 된다. 그 자리(3칸)는 0 으로 비워서 넘긴다.

#### 왜 끼우나 — 코드에 보이는 것만 `[정적]`

1. **돌려주는 값이 안 맞는다.**
   - TLS 콜백은 반환값이 없는 함수다.
   - 그런데 공용 창구는 `AL` 이 FALSE 면 (이유 1 일 때) 오류 로그를 남긴다 (2.6).
   - 안내원이 늘 1 을 돌려주니 **TLS 콜백 때문에 오류 로그가 켜지는 일이 없다.**
2. **넷째 칸의 뜻이 안 맞는다.**
   - `DllMain` 이면 넷째 칸(밀린 뒤 셋째) = `lpReserved` (메모).
   - TLS 는 거기에 **콜백 주소**를 싣는다. 안내원이 그걸 꺼내 쓰고, 콜백에게는 `Reserved = 0` 으로 바꿔 준다.
3. 그래서 "TLS 콜백도 `DllMain` 과 **같은 창구의 기록·사고 대비 틀**(ETW·`__finally`·오류 로그)을 쓰게 하려는 것" 으로 보인다. 이게 이유의 **전부**인지는 `[추측]`.

- 비유: 공용 창구는 원래 "**스위치(진입점)**" 손님용으로 만들어졌다. "**첫 안내(TLS)**" 손님은 서류 모양도 다르고 "잘 됐나요?" 질문에 대답도 안 한다. 그래서 TLS 전용 안내원이 사이에 서서 서류를 맞춰 주고, 창구엔 대신 "잘 됐음" 이라고 답한다.
- flow.md 의 `[추측]` "실제 TLS 콜백을 대신 불러 주는 중간 함수" 는 **맞다** `[정적]`. 덧붙은 것: `Reserved` 를 0 으로, 늘 TRUE.

#### 갈림 하나 — `RtlAcquireSRWLockExclusive`

- `+0x7 ~ +0x11`: 콜백 주소가 `RtlAcquireSRWLockExclusive` 와 같으면 `+0x29` 에서 **직접** 부른다 (ret `+0x2e`, 결과는 역시 1).
- 컴파일러가 "자주 쓰는 대상" 을 직접 부르게 만든 갈래로 보인다 `[추측]`. 이유 정하기는 정적 **Q56**.
- sample 콜백은 그 주소가 아니니 `+0x16` 쪽을 탄다 `[소스]` + `[정적]`. 확인은 동적 **Q26·Q28** (`ntdll+0xe8406` 에 안 서야 한다).

#### 이름 이야기 — 호출 지도에 없던 까닭

- `ImageTlsCallbackCaller` 는 PDB 에 공개 심볼이 아니라 **지역 함수 기호**로 들어 있다 (결과서가 `llvm-pdbutil` 로 확인). 이름은 run8 WinDbg 표시와 같다.
- 그리고 아무도 이 함수를 **직접 call 하지 않는다.** `LdrpCallTlsInitializers +0x18f` 가 주소만 꺼내 `rcx` 에 싣는다. 그래서 호출 지도(sast/05)에 안 잡혔다.
- 대신 PE 의 CFG 함수 표에 이 주소가 등록돼 있다 = "간접 호출로 와도 되는 곳" 으로 허락받았다 `[정적]`.

### 2.9 CFG 중개 — 교환원은 통화 기록에 이름을 안 남긴다

#### 새 말 — CFG (다시, 짧게)

- **CFG** = 간접 호출의 목적지가 **허락된 주소인지** 확인하는 보호 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). [run9 해설](run9-create-user-process-해설.md) 2절 "직접 호출과 간접 호출", [run20 해설 2.9](run20-kernel32-load-해설.md#29-910-마지막-인사--kernelbasepostinit) 에서 한 줄씩 봤다.
- 비유 (run20 그대로): 전화를 걸면 **교환원**이 번호가 허락된 곳인지 보고 이어 준다.
- 이번에 새로 중요한 건 **교환원이 통화 기록(콜스택)에 이름을 남기느냐** 다.

#### 코드에서 — 전부 `JMP` `[정적]`

`LdrpCallInitRoutineInternal +0x1d` 와 `ImageTlsCallbackCaller +0x16` 은 **같은 곳**을 call 한다.

```
CALL 0x180172020   (5바이트 중개, _guard_dispatch_icall$thunk$…)
  → JMP _guard_dispatch_icall                  (rva 0x166360)
      → JMP [__guard_dispatch_icall_fptr]       (rva 0x1e6000 칸)
```

- call 은 맨 처음 한 번뿐이고, 그 뒤는 **전부 `JMP`** 다.
- `JMP` 는 돌아올 자리를 스택에 안 쌓는다 → **콜스택에 프레임이 안 생긴다.**
- 그래서 불린 함수의 돌아올 자리는 바로 `Internal +0x22` / `ImageTlsCallbackCaller +0x1b` 다.
- 비유: 교환원이 이어 주기만 하고 빠진다. 통화 기록엔 **건 사람(창구 직원·안내원)** 과 **받은 사람(콜백)** 만 남는다.

#### 그래서 콜백 안에서 `k` 를 찍으면 — 예측 `[정적]`

위에서 아래로 = 안쪽부터:

1. 콜백 (`Dll2Tls` 등)
2. `ImageTlsCallbackCaller+0x1b`
3. `LdrpCallInitRoutineInternal+0x22`
4. `LdrpCallInitRoutine+0x93`
5. `LdrpCallTlsInitializers+0x19b`
6. `LdrpInitializeNode+0x16b` (exe 는 `LdrpInitializeProcess+0x1c99`)

- 진입점(`_DllMainCRTStartup`, KERNELBASE·KERNEL32 시작 함수) 안에서는 바로 위가 `LdrpCallInitRoutineInternal+0x22` 일 것이다.
- 이건 버스를 타 보기 전 **노선도로 그린 예측**이다. 확인은 동적 **Q25·Q26·Q27·Q28** (그리고 kernel32 쪽은 Q21).

#### 칸 값은 아직 모른다

- `__guard_dispatch_icall_fptr` 칸의 **파일 값**은 `_guard_dispatch_icall_nop` (rva `0x166480`, `JMP RAX` — 검사 없이 바로 이어 줌).
- CFG 가 켜진 프로세스에서 이 칸이 `LdrpDispatchUserCallTarget` (rva `0x124580`, 비트맵 검사) 로 바뀌는지는 정적으로 못 정한다 (ntdll 안에 이 칸에 쓰는 코드가 `refs` 에 없다) → 동적 **Q27**.
- 어느 쪽이든 끝이 `JMP RAX` 라 **프레임을 안 남긴다** `[정적]`. 그러니 위 콜스택 예측은 칸 값과 상관없이 같다.
- 비유: 교환원이 "그냥 이어 주는 사람" 인지 "허락 목록을 확인하는 사람" 인지는 그날 근무표에 달렸다. 하지만 어느 쪽이든 **통화 기록엔 안 남는다.**

### 2.10 핵심 ⑤ 정정 — TLS 콜백 예외는 켜기 담당까지 안 온다

**run27 해설에서 틀린 곳부터 분명히 짚는다.**

| 자리 | run27 해설에 적은 말 | 이번에 알게 된 것 |
|---|---|---|
| [run27 해설](run27-init-graph-해설.md) 2.9 "켜기 담당의 대피도" 끝 | "**TLS 콜백이든** DllMain 이든 예외를 던지면 여기서 잡힌다." | TLS 콜백 예외는 **안쪽에서 먼저** 잡힌다. 켜기 담당의 대피도는 **DllMain(진입점) 예외만** 받는다 |
| 같은 절 "결과 비교" 표 | "**TLS**·DllMain 예외 → `0xc0000142`" 줄 | TLS 콜백 예외로는 `0xc0000142` 가 **안 된다**. 이 줄은 DllMain 예외에만 맞다 |
| 같은 절 표 아래 | "TLS 쪽 실패는 **예외로만** 알려진다" | TLS 쪽 실패는 **아예 위로 안 알려진다** (예외도 안에서 삼킨다) |

run 문서 "정정" 절, flow.md 2.7·2.8 (취소선 + 정정 줄), run27 문서 맨 위 한 줄도 같은 내용이다. run27 해설 파일 자체는 이번에 고치지 않았다.

#### 왜 그렇게 되나 — 단계별로 `[정적]`

1. `LdrpCallTlsInitializers` 에도 **자기 대피도**가 있다 (unwind 범위 표, `pefile` 로 읽음).
   - `__try` = **`+0x12b ~ +0x1a2`** (끝 미포함) = **명단 도는 한 바퀴 전체**.
   - 필터 `$filt$0`: `RtlReportException` 으로 보고만 하고 (`+0x2b`, ret `+0x30`), 예외 종류를 안 보고 **늘 1** (잡아서 착지점으로).
   - 착지점 `+0x1a2` = `JMP +0xe7` (함수 끝 정리) 5바이트. 호출 없음 (원본 바이트로 확인).
2. 콜백 하나가 예외를 던진다 → 창구의 `__finally` 가 일지만 쓰고 위로 보낸다 (2.6) → **`LdrpCallTlsInitializers` 의 `__try` 가 먼저 받는다.**
3. 필터가 늘 1 → 착지점 → 함수 끝 → **그냥 돌아간다.**
4. `LdrpCallTlsInitializers` 는 값을 안 돌려준다 (`void`). 오류를 위로 안 올린다.
5. 켜기 담당은 "TLS 다 됐네" 하고 **진입점 호출로 그대로 간다** (run27: TLS 반환값을 버림).

- **새 말 — 필터**: 대피도에서 "이 사고를 여기서 잡을까?" 를 정하는 짧은 함수로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 1 이면 잡아서 착지점으로, 아니면 위층으로 넘긴다. 이번 필터는 **무조건 잡는다** `[정적]`.

#### 예외는 **가장 가까운** 대피도가 먼저 받는다

- 예외는 콜스택을 **안쪽부터** 거슬러 올라가며 대피도를 찾는 걸로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- TLS 콜백 → 안내원(대피도 없음) → 창구 직원(없음) → 창구(`__finally` 만, 안 잡음) → **`LdrpCallTlsInitializers`(잡음)** → (여기서 끝) → 켜기 담당.
- 진입점(DllMain) → 창구 직원(없음) → 창구(`__finally` 만) → **켜기 담당(잡음)**. 사이에 `LdrpCallTlsInitializers` 가 없으니 run27 말대로 켜기 담당이 잡는다.
- 비유: 건물 3층에서 불이 나면 **3층 소화기**가 먼저 쓰인다. 3층에서 꺼지면 1층 관리실 비상벨은 안 울린다. run27 때는 1층 비상벨 배선(Node 의 범위 `[+0x10e, +0x1b0)` 가 TLS 호출 자리를 감쌈)만 봤고, 3층에 소화기가 있다는 걸 몰랐다.
  - run27 의 범위 읽기 자체는 **틀리지 않았다.** 안쪽에서 먼저 잡힌다는 걸 그때는 몰랐을 뿐이다 (run 문서 "정정").

#### 그래서 무엇이 달라지나

| 일어난 일 | 결과 |
|---|---|
| TLS 콜백이 예외 | 보고만 하고 **그 모듈의 남은 콜백은 건너뜀**. DLL 초기화는 **실패가 아님**. 진입점은 그대로 불림 |
| TLS 콜백이 아무 값이나 돌려줌 | 안내원이 늘 TRUE 로 바꿈 → 영향 없음 |
| DllMain 이 FALSE | `0xc0000142`, `ProcessAttachCalled` 켬 (run27 그대로) |
| DllMain 이 예외 | 창구 `__finally` → 켜기 담당 `__except` → `0xc0000142`, `ProcessAttachCalled` 안 켬 (run27 그대로) |

- 한 줄로: **TLS 콜백은 무슨 일이 있어도 DLL 초기화를 실패시키지 못한다** `[정적]`.
- 주의: "남은 콜백을 건너뛴다" 는 **같은 모듈 안**의 이야기다. 한 바퀴 전체가 한 `__try` 라서 다음 칸으로 안 간다. sample 은 모듈마다 콜백이 하나라 건너뛸 게 없다 `[소스]`.

### 2.11 같은 함수를 부르는 다른 자리 — 이유 값 넷

`LdrpCallTlsInitializers` 는 켜기 담당만 부르는 게 아니다 `[정적]`. `RDX` 는 늘 **명부 줄**, `ECX` 는 **이유**다.

| 이유 (윈도 상수) | 부르는 자리 (ret) |
|---|---|
| 1 프로세스에 처음 붙음 | `LdrpInitializeNode +0x16b` (dll), `LdrpInitializeProcess +0x1c99` (exe) |
| 2 새 스레드 | `LdrpInitializeThread +0x291` (모듈마다) / `+0x3c0` (exe) |
| 0 프로세스 끝 | `LdrpProcessDetachNode +0x135`, `LdrShutdownProcess +0x16d` / `+0x2d1` |
| 3 스레드 끝 | `LdrShutdownThread +0x151` / `+0x1e5` |

- 이 함수가 이유를 거르지 않으니 (2.4), **같은 명단이 이유만 바꿔 네 번의 때에 쓰인다.**
- 새 스레드 쪽은 01_sprint `[관찰]` "Worker 스레드: 모듈마다 TLS → DllMain(이유 2), 마지막 `ExeTls`(이유 2)" 와 같은 모양이다 `[정적]`.
  - `LdrpInitializeThread` 는 모듈마다 `+0x28c` (ret `+0x291`) TLS → 바로 뒤 `+0x2a0` (ret `+0x2a5`) 진입점 `(진입점, DllBase, 2, 0)`.
  - exe 쪽 `+0x3bb` (ret `+0x3c0`) 은 `PebLdr.ShutdownInProgress` 가 0 일 때만.
  - 이건 "새 스레드" 단계 이야기라 지금 목표(`main` 까지) 밖이다. flow.md 3장에 줄만 더했다.
- 비유: 직원 첫 안내(이유 1)뿐 아니라, **새 직원 출근(2)**, **퇴근(3)**, **가게 문 닫기(0)** 때도 같은 명단을 펴서 같은 사람들을 부른다.

### 2.12 정리 — 비유로 한 번 더

- 켜기 담당이 "○○ 건물 **직원 첫 안내**" 버튼을 누르면 (`+0x16b`):
- 담당은 관리실 **TLS 서류철**을 자물쇠(공유)로 열고, ○○ 건물의 장을 찾아 "부를 사람 명단" 위치만 적고, **자물쇠를 잠근다.**
- 명단을 위에서부터 읽는다. 이름이 있으면 **공용 창구**에 서류 `[TLS 전용 안내원 | DllBase | 이유 | 그 사람]` 을 낸다. 빈 줄이면 끝.
- 공용 창구는 일지를 쓰고 (켜져 있으면), 서류를 **그대로** 창구 직원에게 넘긴다.
- 창구 직원은 첫 칸(안내원)을 보고, 나머지를 **한 칸씩 당겨** `[DllBase | 이유 | 그 사람]` 을 안내원에게 건넨다. 교환원을 거치지만 기록엔 안 남는다.
- 안내원은 셋째 칸의 **그 사람**을 꺼내 `[DllBase | 이유 | 0]` 으로 부르고, 그 사람이 뭐라 하든 창구엔 **"잘 됐음"** 이라고 답한다.
- 그 사람이 소란(예외)을 피우면, 창구는 일지만 쓰고 넘기고, **담당이 3층 소화기로 끈다.** 명단의 남은 사람은 안 부르고 그냥 돌아간다. 켜기 담당은 아무 일 없던 것처럼 **전원 스위치**로 간다.
- 전원 스위치(`+0x19c`)도 **같은 창구·같은 창구 직원**을 지난다. 다만 안내원 없이 창구 직원이 곧장 `_DllMainCRTStartup` 에게 `[DllBase | 1 | 메모]` 를 건넨다.

### 2.13 곁가지 — 도구 메모

run 문서 "도구 메모" 를 옮긴다. 분석 결과엔 영향이 없다.

- `ImageTlsCallbackCaller` 크기: `_index.tsv` 는 55바이트인데 `.asm` 마지막 `RET` 가 `+0x37` 이라 56바이트로 셈이 된다. 표에는 `_index.tsv` 값을 적었다.
- `_calls.tsv` 에 `ImageTlsCallbackCaller +0x16` 줄이 **두 번** 들어 있다 (같은 줄 중복). 호출 수는 2곳으로 셌다.
- `pdb_aliases.py --where` 로 `ImageTlsCallbackCaller` 를 찾으면 "공개 심볼에 없음" 이라고 나온다. 지역 함수 기호를 안 읽어서다 (결과서 "스크립트 문제"). plan.md 하네스 후보 (29) 로 적어 두었다.
- 결과서와 `.asm` 의 점프 목적지·call·ret 대조: **어긋난 곳 없음.**
- 버전 대조: `LdrpInitializeNode +0x166/+0x16b`, `+0x197/+0x19c`, run8 `LdrpCallTlsInitializers +0x19b` 가 코드와 같다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 그림 2 `LdrpCallInitRoutine` 아래 (TLS 셋 `n2b`·`n1b`·`x2`) | `LdrpCallInitRoutine` → `?` → `ImageTlsCallbackCaller` | → `+0x93` **`LdrpCallInitRoutineInternal` (정적)** → `+0x22` `ImageTlsCallbackCaller` **(정적)**. 사이의 `?` 화살표를 지움 |
| 그림 2 `LdrpCallInitRoutine` 아래 (진입점 넷 `k10`·`k12`·`n2d`·`n1d`) | `LdrpCallInitRoutine` → `?` → 시작 함수 | → `+0x93` **`Internal` (정적)** → `?` → 시작 함수. `?` 는 이제 `Internal` → 시작 함수 칸 (대상이 실행 때 값이라서) |
| 그림 2 `ImageTlsCallbackCaller` 상자 (`n2c`·`n1c`·`x3`) | 이름만 | `(정적)` 점선. "콜백을 DllBase, 1, 0 으로 부르고 늘 TRUE. 이름은 run8 rcx 로 봄, 들어가는 건 미관찰" |
| 그림 2 `LdrpCallTlsInitializers` 상자 (`n2a`, `k9`, `x1`) | 조건만 | `LdrpTlsList` 에서 꺼냄·0 까지·예외면 남은 콜백 건너뜀 / `k9` 에 "KERNELBASE 는 콜백 배열 첫 칸 0 — PE 파일" / `x1` 에 "`TlsIndex` 는 `LdrpInitializeTls` 가 0xffff 로" |
| 그림 2 `n2` (Node [dll2]) | "FALSE·예외면 `0xc0000142`" | "**진입점** FALSE·예외면 … TLS 콜백 예외는 여기까지 안 옴, run28" |
| 2.7 Node 절 "예외" 줄 | "TLS 콜백이든 DllMain 이든 예외 → `__except`" | **~~TLS 콜백이든~~** + 정정 줄 (TLS 콜백 예외는 안쪽에서 잡힘) |
| 2.7 Node 절 "반환값 버림" 줄 | "TLS 쪽 실패는 예외로만 알려진다" | **취소선** + "TLS 콜백이 실패해도 DLL 초기화는 실패가 안 된다" |
| 2.7 새 소절 | 없음 | **"TLS 콜백 길 한눈에"** (트리 + 콜스택 예측) |
| 2.7 `LdrpCallTlsInitializers` 절 | `[관찰]`·조건만, `[추측]` "→ `ImageTlsCallbackCaller` 로 들어간다" | `[정적]` 명단 출처·한 바퀴·안 부르는 조건 셋·락·예외·TLS 데이터 안 만듦·부르는 자리 표. `[추측]` 줄은 취소선 + "사이에 `Internal`" |
| 2.7 `LdrpCallInitRoutine` 절 | `[추측]` "공용 창구", `[추측]` "한 칸 밀기" | 둘 다 **맞다** `[정적]`. 단 미는 곳은 안쪽. 그대로 넘김·`AL` 그대로·ETW·오류 로그·`__finally` |
| 2.7 새 절 둘 | 없음 | **`LdrpCallInitRoutineInternal`** (한 칸 밀기, CFG 중개 `JMP`), **`ImageTlsCallbackCaller`** (`[추측]` 이었던 "대신 불러 주는 중간 함수" → `[정적]`) |
| 2.5 kernel32 쪽 | "TLS 콜백 호출 없음" (이유 모름) | `[정적: PE 파일]` KERNELBASE 콜백 배열 첫 칸 0, kernel32 디렉터리 없음 → 관찰된 한 번은 KERNELBASE `[추측]` |
| 2.8 dll2 → dll1 | `ImageTlsCallbackCaller → Dll2Tls?` | `… → Internal → ImageTlsCallbackCaller → Dll2Tls?`, 콜백 예외 규칙 정정, `lpReserved` "일부 해결" |
| 2.9 exe TLS | `[공백]` `ImageTlsCallbackCaller` → `ExeTls` 사이 | **취소선** → `[정적]` CFG 중개(`JMP` 뿐), 사이에 프레임 남기는 함수 없음. `TlsIndex` 를 누가 쓰나 → `LdrpInitializeTls +0x102` **맞다** |
| 2.4 | — | `LdrpInitializeTls` 안 (`+0xfa` → `+0x102` → `+0x162`) 한 줄 |
| 3장 새 스레드 | `[관찰]` 만 | 같은 함수를 이유 2·3 으로 부르는 자리 `[정적]` |
| 4장 공백 표 | 2.7 "`ImageTlsCallbackCaller` 안쪽", 2.9 "`TlsIndex` 누가" | 둘 다 **(해결됨, 정적)**. 실행 값 줄 셋 (Q25 / Q26·Q28 / Q27) 과 "이름·뜻 못 정한 것" 줄 추가 |
| 4.1 그림 2 표 | `LdrpCallInitRoutine` → `ImageTlsCallbackCaller` 한 줄 | **(해결됨)** 표시 + 새 줄 둘 (`→ Internal`, `Internal → ImageTlsCallbackCaller`). 콜스택 예측 프레임을 각 줄에 |

### 큰 그림에서 어떻게 이어지나

- 이번으로 **전원 켜기 구간(`+0x1bbb`)의 정적 지도가 바닥까지 찼다.**
  - run27: 순서 담당 → 켜기 담당 (누구부터 켜나).
  - run28: 켜기 담당 → 창구 → 창구 직원 → (안내원) → 유저 코드 (버튼 안쪽).
  - 그 아래는 이제 **유저 코드**다 (dll 의 TLS 콜백, `_DllMainCRTStartup`).
- 그림 2 에 남은 `?` 는 **"어느 함수인지 실행 때 정해지는"** 칸뿐이다 — 콜백 배열에 든 주소, 진입점 칸의 주소. 노선도로는 더 못 간다. **버스를 타야** 한다:
  - **Q25** — TLS 길 실제 인자·명단·KERNELBASE 여부·자물쇠 경합
  - **Q26·Q28** — 콜백이 실제로 `Dll2Tls` → `Dll1Tls` → `ExeTls` 순서로 `(DllBase, 1, 0)` 을 받나
  - **Q27** — 진입점 `lpReserved` 값, CFG 칸 값, ETW 꺼짐
  - 이 넷은 2.9 의 **콜스택 예측**을 들고 가면 한 번에 맞춰 볼 수 있다.
- 정적 대기열은 **Q17** 로 넘어간다 — 로더가 다 끝난 뒤 `NtContinue` 로 `RtlUserThreadStart` 에 가서 `main` 까지 가는 길. 지금 목표(프로세스 생성 → `main`)의 **마지막 구간**이다.
- 작은 정적 둘(Q55, Q56)은 대기열 뒤에 붙었다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 한 칸 밀기가 **실제로** 그렇게 일어나나 (TLS 길) | 정적은 "코드에 이렇게 쓰여 있다" 까지. 레지스터 값은 실행 때 | 동적 **Q25** (D1) — `ntdll+0x10fe6` (`LdrpCallTlsInitializers +0x196`) 에서 `rcx` = `ntdll+0xe83e0`, `rdx`, `r8`, `ln @r9`. `ntdll+0x11ead5` (`Internal +0x1d`) 에서 한 칸 밀린 모양 |
| 명단에 콜백이 몇 개인가, 0 에서 멈추나 | 명단(콜백 배열)은 실행 때 메모리 | 동적 **Q25** (D3) — `ntdll+0x10f76` (`+0x126`) 에서 `dqs @rbx L3`, 한 바퀴 머리 `ntdll+0x10f82` 횟수 (모듈마다 1개 `[소스]` → 2번 서고 둘째에 0) |
| kernel32 길의 "불렸지만 콜백 없음" 이 정말 **KERNELBASE** 였나 | PE 파일로 후보만 찾았다. 그 호출이 어느 명부 줄이었는지는 실행 때 | 동적 **Q25** (D4) — 입구 `ntdll+0x10e50` 에서 `du poi(@rdx+0x60)` 와 `ecx`, `+0x126` 에서 `dq @rbx L1` (= 0 예측) |
| 자물쇠 경합 갈래를 안 타나 | 다른 스레드(로더 일꾼)와 겹치는지는 실행 때 | 동적 **Q25** (D6) — `ntdll+0x10f63`, `ntdll+0x11038`, `ntdll+0x111d6` 에 안 설 것 |
| 콜백이 실제로 `Dll2Tls` → `Dll1Tls` → `ExeTls` 순서로 `(DllBase, 1, 0)` 을 받나, `RtlAcquireSRWLockExclusive` 갈래를 안 타나 | 콜백 주소는 실행 때 값. 들어가는 순간은 아직 못 봤다 | 동적 **Q26** (dll)·**Q28** (exe) (D2) — `ntdll+0xe83f6` (`ImageTlsCallbackCaller +0x16`) 에서 `ln @rax`, `rcx`·`rdx`·`r8`. `ntdll+0xe8406` 에 안 설 것. 콜백 입구 `k` 로 2.9 예측 프레임 |
| 진입점 `lpReserved` 값이 정말 `LdrpProcessInitContextRecord` 값(CONTEXT 포인터)인가, 반환 `al` | 실행 때 값 | 동적 **Q27** (D7) — `ntdll+0x11ead5` 에서 `rax` 가 진입점일 때 `r8`, `ntdll+0x10c33` (`LdrpCallInitRoutine +0x93`) 의 `al` (기존 `ntdll+0xe767`/`ntdll+0xe76c` 와 같이) |
| `_DllMainCRTStartup` 이 셋째 인자를 `DllMain` 의 `lpReserved` 로 그대로 넘기나 | CRT 코드(sample dll 안)라 이번 범위(ntdll) 밖 | 정적 **Q55** (sample dll, 작음) |
| CFG 칸 값이 `_guard_dispatch_icall_nop` 인가 `LdrpDispatchUserCallTarget` 인가 | ntdll 안에 이 칸에 쓰는 코드가 안 보였다. 실행 때 값 | 동적 **Q27** (D5) — `dq ntdll+0x1e6000 L1`, `ln poi(ntdll+0x1e6000)` (멈춤 없이 아무 때나) |
| ETW 가 꺼져 있나 | 레지스트리·ETW 세션에 달린 실행 때 값 | 동적 **Q27** (D8) — `ntdll+0x10d55`, `ntdll+0x10db3`, `ntdll+0x10e1c` 에 안 설 것. `db 7ffe0384 L2`, `dd @$peb+0x378 L1` |
| TLS 목록 항목(0x48바이트) 칸의 진짜 이름 | PDB 에 타입이 없다. `+0x28` = `AddressOfCallBacks` 는 PE 규격 배치로 읽음 | flow.md 4장 "이름·뜻 못 정한 것 (run28)" 줄 (번호 없음) |
| TEB `+0x1850` 8칸 표의 진짜 이름, 추적 칸 맨 위 비트를 켜는 곳 (`RtlpAcquireSRWLockSharedContended` 안일 것 `[추측]`) | PDB 이름이 쓰임새와 안 맞음. 켜는 쪽은 callee 본문 | 정적 **Q56** |
| `ImageTlsCallbackCaller` 의 `RtlAcquireSRWLockExclusive` 직접 호출 갈래가 왜 있나 (컴파일러 갈래 `[추측]`) | 코드엔 갈림만 있고 이유는 안 쓰여 있다 | 정적 **Q56** |
| `LdrShutdownProcess +0x168` 앞 `[RDI+0x58] ≠ 0` 조건의 뜻 | 요청 밖이라 줄만 봤다 | 정적 **Q56** |
| 끼우는 까닭이 "같은 창구 틀을 쓰려고" 가 전부인가 | 코드엔 하는 일(넷째 칸 → 부를 함수, `Reserved = 0`, 늘 TRUE)만 보인다 | `[추측]` 그대로 |

---

## 5. 더 읽기

- run 문서: [`run28-tls-init-call.md`](run28-tls-init-call.md) — 호출 지점 표 전체, `LdrpCallTlsInitializers` 순서 8단계, 예외·락 경합 갈래, 레지스터 옮겨 가는 표 둘, `[추측]` 세 줄 판정, 정정, 동적 확인 목록
- 정적 결과서: [`_workspace/sast/23_tls-init-call_result.md`](../../_workspace/sast/23_tls-init-call_result.md) — 질문 1 ~ 6 답 (TLS 항목 칸 근거, 필터·착지점 바이트, CFG 중개 사슬), 그림 2 상자 후보, D1 ~ D8 표, 스크립트 문제
- 요청서: [`_workspace/sast/23_tls-init-call.md`](../../_workspace/sast/23_tls-init-call.md)
- flow.md: [`flow.md`](flow.md)
  - 그림 2 (새 `LdrpCallInitRoutineInternal` 상자 7개, `(정적)` `ImageTlsCallbackCaller` 상자 3개)
  - 2.7 "TLS 콜백 길 한눈에", `LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `LdrpCallInitRoutineInternal`, `ImageTlsCallbackCaller` 절, `LdrpInitializeNode` 절의 정정 줄
  - 2.5 (KERNELBASE 콜백 배열), 2.8, 2.9, 3장 새 스레드
  - 4장 공백 표 run28 줄들, 4.1 그림 2 표
- 앞 해설:
  - [run27 해설](run27-init-graph-해설.md) 2.7 (켜기 담당 순서), 2.8 (진입점 넷째 인자, `lpReserved`), 2.9 (대피도 — **TLS 콜백 예외 부분은 이번 2.10 으로 정정**)
  - [run19 해설](run19-proc-init-modules-해설.md) 2.11 (exe TLS 콜백)
  - [run20 해설](run20-kernel32-load-해설.md) 2.9 (교환원·CFG)
  - [run23 해설](run23-load-dependent-해설.md) 2.3 (공유·독점 잠금)

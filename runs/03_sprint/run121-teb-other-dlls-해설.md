# run121 teb-other-dlls — 해설

> 이 글은 [run121 문서](run121-teb-other-dlls.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q102** — `_TEB` "범위 밖 길 — 다른 DLL" 39칸 확인 (작은 정적). run54 가 칸 이름만 보고 "쓰는 DLL" 을 짐작해 둔 칸들이다 (`_workspace/plan.md`, 이번에 **끝**)
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다. **짐 지도 바퀴**다.
  - 남은 것이 간 곳 (4절):
    - 동적 → 새 **Q211** (D1 · D2)
    - 정적 → 새 **Q212 ① ~ ④**
    - "못 찾은 16칸을 '아무도 안 씀' 으로 옮길까" → **사용자 결정 대기**
- 근거:
  - run 문서 [`run121-teb-other-dlls.md`](run121-teb-other-dlls.md) (가장 중요)
  - 요청서 [`_workspace/sast/117_teb-other-dlls.md`](../../_workspace/sast/117_teb-other-dlls.md) · 결과서 [`117_teb-other-dlls_result.md`](../../_workspace/sast/117_teb-other-dlls_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 2.5 · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): 3.3 · 새 **3.3.1** · 새 **7.52**. 2장 숫자는 그대로
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 "사진" 은 전부 **run44 때 찍어 둔 사진** 이다 (`ms/logs/04_baggage-read_P2.txt`).
  - Ghidra analyze 도 새로 안 했다. System32 파일을 `objdump` 로 풀어서 찾았다 (2.2).
- **run 문서가 결과서와 다르게 적은 곳이 하나 있다.** 이 글은 **run 문서**를 따른다.
  - 결과서 3장의 사진 줄 번호는 칸 이름과 짝이 어긋나 있다 (예: 결과서 "`NlsCache` null (2697)" — 로그 2697 줄은 `WOW32Reserved`).
  - flow-writer 가 로그를 열어 바른 짝으로 적었다: `WOW32Reserved` 2697 · `StaticUnicodeBuffer` 2738 · `GuaranteedStackBytes` 2761 · `SavedPriorityState` 2767 · `IsImpersonating` 2774 · `NlsCache` 2775. 값 (0 / null / `""`) 은 같고 결론도 안 바뀐다.
- 이번에 [run54 해설](run54-teb-fill-해설.md) · [run86 해설](run86-gdi32-init-해설.md) 맨 위에 "고쳐 읽기 (run121)" 한 줄씩을 붙였다 (2.12).

---

### 이미 푼 말 (링크만)

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실** → [run65 해설 "비유"](run65-ldr-init-bodies-해설.md)
- DLL = 가게에 들인 **건물**, 시작 함수 (`DllMain`) = 건물 **전원 켜기**, csrss = 이웃 **인사팀 사무실**, user32 = **창 담당 건물**, win32k = 관리사무소의 **그래픽 부서**, gdi32 = 그림 도구 건물 **앞 동**, gdi32full = 그 건물 **본관** → [run86 해설 "비유"](run86-gdi32-init-해설.md)
- TEB = 직원 **개인 안내판** → [run11 해설 2.2](../02_sprint/run11-allocate-thread-해설.md#22-한-줄씩-읽는-법) · [run54 해설 "비유"](run54-teb-fill-해설.md)
- TLS = 직원 **개인 사물함** → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) · [run41 해설](../02_sprint/run41-tls-lock-detail-해설.md)
- 끝까지 0 의 네 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길) → [run52 해설 2.6 · 2.9](run52-kthread-create-해설.md) · "범위 밖 길에서만 씀 `[추측]`", 오프셋 훑기의 **오탐** → [run50 해설 2.4 · 2.5](run50-eprocess-create-해설.md)
- `_TEB` 의 "다른 DLL" 39칸, **칸 이름표** 비유, "KERNELBASE 4 · 이름 짐작 35" → [run54 해설 2.13](run54-teb-fill-해설.md)
- `_TEB32` = 32비트 안내판 (옆 건물 칸을 잘못 짚기), WOW64 → [run54 해설 "한 줄로 먼저" (3)](run54-teb-fill-해설.md) · [run10 해설 3.10](../02_sprint/run10-allocate-process-해설.md#310-ntdll-쪽-조금-더)
- 사진 지점 P1a · P1b · P2 → [run44 해설 2.8](run44-baggage-snap-해설.md)
- 이유 1 (`DLL_PROCESS_ATTACH`, "프로세스에 처음 붙음") → [run25 해설](../02_sprint/run25-dll-entry-해설.md) · [run19 해설 2.11](../02_sprint/run19-proc-init-modules-해설.md#211-exe-tls-콜백--전원-다-켠-뒤에)
- NLS = 관리실 책상의 **글자 사전** → [run57 해설](run57-nls-apiset-해설.md) · KERNELBASE 전원 켜기 안 `+0x4e1/+0x4e6 BaseNlsDllInitialize` → [run68 해설 2.6](run68-dll-init-rest-해설.md)
- 가장 (impersonation) = 다른 사람의 신분증을 **잠깐 빌려** 일하는 것 `[추측]` (일반적으로 알려진 설명) → [run24 해설](../02_sprint/run24-work-queue-해설.md) · [run100 해설](run100-rpc-sd-assign-해설.md)

### 이번 글의 새 말 (본문에서 처음 나올 때 한 줄씩 푼다)

- **`objdump` 훑기**: 2.2

### 비유 (그대로 이어 쓴다)

- TEB 는 이 시리즈에서 쭉 직원 **개인 안내판** 이다.
  - run 문서는 이번에 TEB 를 "**사물함**" 이라고 불렀다. 그런데 이 시리즈에서 사물함은 이미 TLS (run28 · run41) 다.
  - 그래서 이 글은 **개인 안내판** 으로 쓴다. 가리키는 건 run 문서의 "사물함" 과 같다.
- run54 해설 2.13 의 그림을 그대로 잇는다.
  - 안내판 칸 몇 개에 "그림판 부서용", "3D 부서용", "소켓 부서용" 같은 **칸 이름표** 가 붙어 있다.
  - run54 는 부서 사무실에 가 보지 않고 **이름표만 보고** "이 칸은 그 부서가 쓰겠지" 라고 적었다.
- 이번에 한 일 (run 문서의 그림):
  - **회사 전체 (System32) 의 일지를 다 뒤져**, 그 칸에 실제로 물건을 넣는 손을 찾았다.
  - 23칸은 넣는 손을 찾았다. 16칸은 아무도 넣는 걸 못 봤다.
- 그리고 test.exe 가게에는 건물이 **6채** 뿐이다 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2). 그림판 · 3D · 소켓 부서 건물은 아예 안 들였다.
  - 그러니 그 부서들의 칸은 처음 받은 빈 상태 (0) 그대로다.

### 한 줄로 먼저: 무엇을 얻었나

| 요청서 질문 | 한 줄 답 (run 문서 그대로) | 이 글 |
|---|---|---|
| 1 칸마다 짐작한 DLL 에 쓰는 줄이 있나, 틀렸으면 어디인가 | **쓰는 줄 있음 23 · 못 찾음 16.** 23 = 짐작 맞음 19 · 짐작 DLL 도 쓰지만 주인은 다른 DLL 2 (`GdiTebBatch` · `GdiBatchCount` → gdi32full) · **짐작 틀림 2** (`WinSockData` → **mswsock.dll**, `WOW32Reserved` → **KERNELBASE**) `[정적]` | 2.1 ~ 2.8 |
| 2 그 함수가 test.exe 초기화에서 불리나 | **타는 칸 0.** 다른 DLL 은 안 올라온다 (사진 P2 모듈 6개 `[관찰]`). KERNELBASE 5칸 중 넷은 길이 없고, `NlsCache` 는 길은 닿지만 쓰는 줄이 **`IsImpersonating ≠ 0` 갈래** 안이다 `[정적 + 관찰]` | 2.9 ~ 2.11 |
| 3 짐 지도 3.3 을 줄 단위로 | 새 **3.3.1** (칸마다 39줄, 줄 수에는 안 셈). 3.3 고친 줄 셋. **2장 숫자 그대로** | 3절 |

- 이전 run 이 틀린 곳: **run54** (다른 DLL 짐작 셋) · **run86** (`Gdi*` 6 짐작 "그대로") (2.12).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 바퀴는 길 위의 한 점이 아니다. **안내판 39칸** 을 쓰는 손이 지도 **밖** 여기저기에 흩어져 있다. 지도 **안** 에 걸리는 건 한 갈래뿐이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │
 ├─ 단계 ① 커널 ── MmCreateTeb      개인 안내판 새로 (빈칸 + 이름표 몇 장, run54)
 ├─ 단계 ② ntdll
 │  ●P1 LdrInitializeThunk
 │    … KERNELBASE 전원 켜기 (_KernelBaseBaseDllInitialize, P1a ~ P1b)
 │        +0x4e1 BaseNlsDllInitialize → … → GetCPHashNode
 │              → GetCurrentNlsCache · CheckSpecialLocales   ← 지도 안에서 닿는 유일한 곳 (2.11)
 │                                                               쓰는 줄은 갈래 밖
 │  ●P2 NtContinue                     ← 사진 P2: 건물 6채, 39칸 모두 0
 └─ (CRT) → main

(지도 밖 — test.exe 가 안 들인 건물)  gdi32full · user32 · opengl32 · mswsock · combase …   ← 39칸 대부분의 주인
(지도 밖 — 다른 가게 · 관리사무소)     csrss 안 csrsrv · basesrv, 커널 win32k
```

- 새 상자는 없다. 지도 안에 걸리는 건 **이미 그림에 있는 상자** (`KERNELBASE!_KernelBaseBaseDllInitialize` → `BaseNlsDllInitialize`) 안쪽 한 갈래뿐이다.
- 그래서 대표 지점 규칙대로 **그림은 안 바꿨다** (run 문서 "지도 범위").

### 무엇이 궁금했나

run54 가 넘긴 질문이다 (요청서).

1. 39칸마다, 짐작한 DLL 에 **쓰는 줄이 정말 있나?** 틀렸으면 진짜로 쓰는 DLL 은 어디인가?
2. 그 함수가 **test.exe 초기화 동안 불리나?** (test.exe 가 올리는 DLL 은 KERNEL32 · KERNELBASE 뿐)
3. 짐 지도 3.3 의 "다른 DLL 39" 한 줄을 **칸 · 쓰는 DLL · 함수 · test.exe 에서 타나** 로 펴 놓을 수 있나?

### 바퀴 전에 알던 것

- `[추측]` run54 (sast/48): "다른 DLL" 39칸 중 **35칸** 은 칸 이름만 보고 주인을 짐작했다. 나머지 **KERNELBASE 4칸** 만 쓰는 줄을 봤다 ([run54 해설 2.13](run54-teb-fill-해설.md)).
  - 그때 결과서에 이미 "`WOW32Reserved` 는 KERNELBASE 에서도 쓰는 줄이 하나 보였다" 는 덧붙임이 있었다. 주인으로 적은 건 wow64cpu (짐작) 였다.
- `[관찰]` run44: 이 39칸은 **초기화 동안 (사진 K2 ~ P2 일곱 장) 0** 이다. 이건 이번에도 그대로다.
- `[소스]` test.exe 는 user32 · gdi32 를 안 올린다 (`sample/log.h:3`, 요청서).
- `[정적]` run85: user32 `ClientThreadSetup` 이 `Win32ClientInfo` 칸을 쓴다. user32 는 `Win32ThreadInfo` 를 안 쓴다.
- `[정적]` run86: gdi32 · gdi32full 의 `DllMain` 줄기에는 TEB `Gdi*` 쓰기가 **없다**. 그래서 "gdi32 6 (`Gdi*`)" 짐작은 **그대로 `[추측]`** 로 남았다.
- `[정적]` run68 · run71: KERNELBASE 전원 켜기의 NLS 사슬 (`+0x4e1/+0x4e6 BaseNlsDllInitialize` …).

---

## 2. 찾은 것

### 2.1 한눈에: 39칸을 어떻게 셌나

```
_TEB "범위 밖 길 — 다른 DLL" 39칸
├─ 쓰는 줄 있음 23                                                   [정적]
│   ├─ 짐작 DLL 이 맞음 19
│   ├─ 짐작 DLL 도 쓰지만 주인은 다른 DLL 2
│   │     GdiTebBatch · GdiBatchCount  → 주로 gdi32full                (2.6)
│   └─ 짐작이 틀림 2
│         WinSockData    ws2_32   → mswsock.dll                        (2.4)
│         WOW32Reserved  wow64cpu → KERNELBASE                         (2.5)
└─ 쓰는 줄 못 찾음 16   (그중 13칸은 읽는 줄도 없음)                  (2.8)

test.exe 초기화에서 타는 칸 = 0                                       [정적 + 관찰]
├─ 23 중 18칸: 안 들인 건물 몫 16 · 다른 가게 (csrss) · 관리사무소 몫 2  → 처음부터 안 탐   (2.9)
└─ 23 중 5칸: KERNELBASE
     ├─ 넷: 초기화 길에서 직접 호출로 안 닿음                                         (2.10)
     └─ NlsCache: 닿지만 쓰는 줄이 IsImpersonating ≠ 0 갈래 안                       (2.11)
```

- 칸 수 대조: 39줄 = 23 + 16 (결과서).
- ntdll · kernel32 는 39칸 중 **쓰는 칸이 하나도 없다** `[정적]`. 그러니 "다른 DLL" 이라는 이름이 맞았다.
- 칸마다 표 (39줄, 함수 rva · 모든 `+0x`) 는 run 문서 결과 1장과 짐 지도 3.3.1 에 있다. 이 글은 대표만 든다.

### 2.2 새 말: `objdump` 훑기 — 어떻게 찾았나

**새 말 — `objdump` 훑기**

- 프로그램 바이트를 **기계어 줄로 풀어 놓고**, 원하는 꼴을 **글자 검색으로** 찾는 것이다.
- 빠르다. 대신 **간접으로 쓰는 꼴** 은 놓칠 수 있다.
- 비유: 회사 전체 일지를 복사해 와서, "안내판 몇 번 칸에 넣음" 이라는 **글자** 가 나오는 줄을 찾아 형광펜을 긋는 것이다. 일지에 "옆 사람한테 안내판을 건네줌 → 그 사람이 넣음" 처럼 **돌려서** 적힌 건 글자 검색에 안 걸린다.

이번 훑기의 걸음 (run 문서 "방법") `[정적]`:

1. System32 의 `*.dll *.exe *.ocx *.cpl *.ax *.drv` **3,916개** 를 풀었다. 그중 x64 **3,881개** 를 봤고 .NET **35개** 는 건너뛰었다.
2. `MOV REG,GS:[0x30]` — 지금 스레드의 TEB 주소를 꺼내는 줄 — 에서 시작한다.
3. 그 레지스터를 `MOV` · `LEA` · `ADD` 로 따라간다. 호출 뒤엔 휘발 레지스터를 버리고, 앞쪽 분기는 상태를 합친다. `GS:[칸]` 으로 바로 닿는 줄도 같이 본다.
4. 39칸 오프셋 범위 (`pdb_struct.py ntdll.dll _TEB`) 에 드는 접근만 **쓰기 / 읽기 / 주소 꺼내기 (`LEA`)** 로 나눈다.
5. 함수 이름은 `.pdata` 함수 시작 → `_index.tsv` 이름, 없으면 **PDB 공개 이름**. 이름이 꼭 필요한 **12개 DLL 만** PDB 를 새로 받았다 (advapi32 · combase · mswsock · opengl32 · imm32 · ws2_32 · sechost · wow64cpu · WUDFHost.exe · regsvc · CoreMessaging · ttdrecordcpu).
6. 커널 쪽은 win32k 셋 (`win32kbase.sys` · `win32kfull.sys` · `win32k.sys`) 만, `GS:[0x188]` (지금 스레드) → `+0xf0` (TEB) 로 따라갔다.

왜 믿을 만한가 (결과서 "버전 대조") `[정적]`:

- 훑기가 낸 "함수 시작 + 오프셋" 이 `_index.tsv` 함수 시작과 모두 맞는다.
- `.asm` 이 이미 있는 함수 (`GetCurrentNlsCache` · `CheckSpecialLocales` · `_KernelBaseBaseDllInitialize` 등) 의 줄과도 맞는다. flow-writer 도 그중 여러 줄을 `.asm` 으로 다시 봤다.
- sast/48 의 KERNELBASE 줄도 같은 오프셋으로 다시 나왔다.

왜 "못 찾음" 은 "없음" 이 아닌가 (결과서 "검색 한계"):

- TEB 포인터를 **함수 인자 · 스택 · 구조체에 담아 넘긴 뒤** 쓰는 줄은 놓칠 수 있다. 실제로 `UnmarkUmdfDriverInitiatedRequest` 는 TEB 를 인자로 받는다.
- SysWOW64 · .NET 35개 · win32k 셋 밖의 드라이버는 안 봤다.
- 그래서 "못 찾음 16" 은 **하한** 이다 (flow.md 기준 메모).
- 덤 (도구 메모): 처음판은 `SetThreadPriority` 의 `SavedPriorityState` 쓰기를 **놓쳤다**. "앞쪽 분기 상태 합치기" 와 "연결된 `.pdata` 조각은 초기화하지 않기" 를 넣고서야 잡혔다. 글자 검색이 생각보다 쉽게 놓친다는 산 예다.

옆 건물 칸을 잘못 짚지 않게 뺀 것 (run54 해설 "(3)" 과 같은 함정):

- wow64.dll 의 `[+0xc0]` · `[+0xc4]` 쓰기는 64비트 칸 `User32Reserved` (`+0x80`) 범위에 걸려 보인다. 그런데 `WowTebOffset` 을 더한 **32비트 안내판 (`_TEB32`) 의 칸** 이라 뺐다 `[정적]`.
- ntdll 의 `RtlpCheckRelativeDrive +0x248 · +0x257` 등 5줄 (`[+0xf28]`) 도 같은 꼴로 보여 뺐다 `[추측]`.

### 2.3 한 줄씩 읽는 법

run 문서 결과 1장 표에서 한 줄을 골라 풀어 읽는다.

#### 줄: `26 | +0x1738 WinSockData | ws2_32 | O mswsock.dll | SockWaitForSingleObject +0x72 · +0x18e · +0x290 · +0x2bf (0x93c0) · AllocateWinsockThreadData +0x52 (0xcd20) | 안 올림`

- **어느 칸**: TEB `+0x1738`, 이름 `WinSockData`.
- **짐작 (sast/48)**: 이름에 "WinSock" 이 들어 있어서 ws2_32 로 적었다 `[추측]`.
- **누가 (실제)**: **mswsock.dll** 의 두 함수 `[정적]`.
  - `SockWaitForSingleObject` (함수 시작 rva `0x93c0`) — `+0x72` · `+0x18e` · `+0x290` · `+0x2bf` 네 자리에서 이 칸에 쓴다.
  - `AllocateWinsockThreadData` (rva `0xcd20`) — `+0x52` 에서 `AND [..],0`, 곧 **0 으로 비운다** (결과서).
- **무엇을 위해**: run 문서 · 결과서엔 칸의 뜻이 적혀 있지 않다. 이 글도 짐작하지 않는다.
- **test.exe 에서**: mswsock 은 사진 P2 의 건물 6채에 없다 `[관찰]` → **안 올림**, 처음부터 안 탄다.

#### 줄: `33 | +0x17a0 NlsCache | KERNELBASE | O KERNELBASE (10 함수, 쓰기 15줄) | GetCurrentNlsCache +0x1021 · +0x1096 (0x52350) … | 조건부, 초기화 땐 안 탐`

- **어느 칸**: TEB `+0x17a0`, `NlsCache`.
- **누가**: KERNELBASE 의 NLS 함수 **10개**, 쓰는 줄은 모두 **15줄** `[정적]`. 대표가 `GetCurrentNlsCache` (rva `0x52350`) 의 `+0x1021` · `+0x1096`.
- **무엇을**: 열 함수 모두 같은 꼴이다 — `RtlAllocateHeap(0x698)` 으로 새 블록을 받고 → 채우고 → TEB 이 칸에 놓는다 (결과서).
- **test.exe 에서**: 이 줄만 "안 올림" 이 아니라 "**조건부**" 다. KERNELBASE 는 들인 건물이고, 길도 닿기 때문이다. 왜 결국 안 타는지가 2.11 이다.

### 2.4 짐작 틀림 ①: `WinSockData` 의 주인은 ws2_32 가 아니라 mswsock

- `[정적]` ws2_32 는 이 칸을 **읽지도 쓰지도 않는다**. 쓰는 곳은 mswsock.dll 의 `SockWaitForSingleObject` · `AllocateWinsockThreadData` 둘이다 (2.3).
- 그러니까 이름표 "소켓 부서용" 자체는 맞았다. 다만 그 부서 안에서 **실제로 물건을 넣는 사무실** 이 이름표에서 떠올린 곳 (ws2_32) 이 아니라 옆 사무실 (mswsock) 이었다.
  - 일반적으로 ws2_32 는 소켓 기능의 **바깥 창구**, mswsock 은 그 뒤에서 실제로 일하는 쪽으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이번 결과와 어긋나지 않는 그림이지만, run 문서가 확인한 것은 "누가 쓰나" 까지다.

### 2.5 짐작 틀림 ②: `WOW32Reserved` 의 주인은 wow64cpu 가 아니라 KERNELBASE

- `[정적]` 64비트 TEB `+0x100` `WOW32Reserved` 에 쓰는 곳은 **딱 하나**: KERNELBASE `Wow64SetThreadDefaultGuestMachine +0x33` (rva `0xc50f0`, `MOV [RCX+0x100],RBX`).
  - 같은 함수 `+0x29` 와 `Wow64DetectMachineTypeInternal +0x2b` (rva `0xc5150`) 는 읽는다.
- `[정적]` wow64cpu 는 64비트 칸을 **안 만진다**.
- 왜 헷갈렸나: 32비트 안내판 (`_TEB32`) 에도 같은 이름 `WOW32Reserved` (`+0xc0`) 가 있다. wow64.dll 이 쓰는 건 **그쪽** 이다 (2.2 의 "뺀 것").
  - 이름은 같은데 **다른 판의 다른 칸** 이다. run54 해설 "(3) 오프셋 숫자만 보면 옆 건물 칸을 잘못 짚는다" 와 같은 함정을, 이번엔 **이름** 으로 밟은 셈이다.
- 64비트 칸이 무슨 뜻으로 쓰이나: "스레드 기본 게스트 머신" 값 자리처럼 보인다 `[추측]` (결과서).
- test.exe: 초기화 길에서 `Wow64SetThreadDefaultGuestMachine` 로 가는 직접 호출 사슬이 없다 `[정적]`. 사진 P2 도 null (`_P2.txt:2697`) `[관찰]`.

### 2.6 주인이 다름: `Gdi*` 6칸 중 쓰는 줄이 있는 건 2칸, 주인은 gdi32full

| 칸 | 결과 | 쓰는 자리 (대표) |
|---|---|---|
| `+0x2f0` `GdiTebBatch` | **O** — gdi32full 주 · gdi32 일부 · 커널 win32kfull | gdi32full 8 함수 (`PatBlt +0x28a` · `ExtTextOutWImpl +0x382` · `SelectObjectImpl +0x43d` …), gdi32 `InternalDeleteObject +0x268` · `DeleteRegion +0xaf`, win32kfull `GdiThreadCalloutFlushUserBatch +0x4c` |
| `+0x1740` `GdiBatchCount` | **O** — 같은 셋 | 같은 함수들 (`PatBlt +0x2c5` …), gdi32 `InternalDeleteObject +0x24f` · `DeleteRegion +0x96`, win32kfull `GdiThreadCalloutFlushUserBatch +0x3a` (= 0) |
| `+0x7e8` `GdiCachedProcessHandle` · `+0x7f0` `GdiClientPID` · `+0x7f4` `GdiClientTID` · `+0x7f8` `GdiThreadLocalInfo` | **X** 넷 | — |

- 모두 `[정적]` (결과서 1장).
- run86 해설의 그림으로 읽으면:
  - 그림 도구 건물에서 이 두 칸에 손을 대는 건 주로 **본관 (gdi32full)** 이다. **앞 동 (gdi32)** 은 지우는 함수 둘 (`InternalDeleteObject` · `DeleteRegion`) 만 쓴다.
  - 그리고 관리사무소 그래픽 부서 (win32kfull) 의 `GdiThreadCalloutFlushUserBatch` 가 비운다.
- 쓰는 함수 이름이 `PatBlt` · `ExtTextOutWImpl` · `SelectObjectImpl` 같은 **그리기 함수** 들이다. run86 이 본 "`DllMain` (전원 켜기) 줄기엔 `Gdi*` 쓰기가 없다" 는 **그대로 맞다** — 전원 켤 때가 아니라 그릴 때 쓰는 칸이다.
  - 일반적으로 GDI 는 그리기 요청을 TEB 에 모았다가 한꺼번에 커널로 보낸다고 알려져 있다 `[추측]` (일반적으로 알려진 설명). 칸 이름 (`Batch`) 과 커널 쪽 함수 이름 (`FlushUserBatch`) 이 그 설명과 어울린다. 하지만 run 문서가 확인한 건 "누가 어디서 쓰나" 까지다.

### 2.7 나머지 칸 — 부서별로 한눈에

결과서 2장 "묶음별 한 줄" 을 줄였다. 모두 `[정적]`, "안 올림" 은 `[관찰]` (P2 건물 6채에 없음).

| 부서 (칸 수) | 쓰는 줄 있음 (O) | 못 찾음 (X) | test.exe |
|---|---|---|---|
| 창 · 그래픽 (user32 계열 3 + `Win32ThreadInfo` 1) | `Win32ClientInfo` — user32 주 (`ClientThreadSetup +0xd3` · `PeekMessageW +0x96` …) · gdi32full · imm32 · CoreMessaging · (csrsrv). `Win32ThreadInfo` — **커널만** (아래) | `User32Reserved` · `UserReserved` | 안 올림 / 안 탐 |
| 3D (`gl*` 8) | 6칸 — opengl32 (`GLInitializeThread` · `MakeAnyCurrent` · `glclt*` …) | `glSection` · `glTable` | 안 올림 |
| RPC (2) | `ReservedForNtRpc` — rpcrt4 (`ThreadSelf +0x2dc` · `_InitializeDLL +0x30`) | `ActiveRpcHandle` | 안 올림 |
| OLE (4) | `ReservedForOle` — **combase** 10 함수 (ole32 · coml2 는 읽기만) | `TxnScopeEnterCallback` · `TxnScopeExitCallback` · `TxnScopeContext` | 안 올림 |
| 성능 · 추적 (2) | `PerflibData` — **advapi32** (+ regsvc, sechost 는 X) | `EtwTraceData` | 안 올림 |
| CSR (1) | `CsrClientThread` — csrsrv · basesrv (**csrss.exe 안**) | — | 안 탐 (csrss 자기 스레드의 안내판) |
| 이름만 (6) | `ReservedForDebuggerInstrumentation` — ttdrecordcpu (TTD 기록기) · `ReservedForWdf` — WUDFHost.exe (UMDF 호스트) · `ReservedForCrt` — ucrtbase | `ReservedForPerf` · `ReservedForCodeCoverage` · `pShimData` | 안 올림 |
| 소켓 · WOW64 · GDI | 2.4 · 2.5 · 2.6 | 2.6 (`Gdi*` 넷) | |
| KERNELBASE (4, + `WOW32Reserved`) | `StaticUnicodeBuffer` · `GuaranteedStackBytes` · `SavedPriorityState` · `NlsCache` 모두 O | — | 안 탐 (2.10 · 2.11) |

조금 더 볼 칸 셋:

- **`Win32ThreadInfo` (`+0x78`)** — 유저 DLL 은 **읽기만** 한다 (user32 53 · imm32 13 · gdi32 2 — 결과서 숫자) `[정적]`. 쓰는 줄은 커널 win32kbase 의 `xxxDestroyThreadInfo +0x104` — **정리하는 쪽** 하나다.
  - **놓는 줄은 못 찾았다** `[공백]`. 후보 이름은 `xxxCreateThreadInfo` (rva `0x194d84`) 인데 안 풀었다. TEB 를 인자로 넘기는 꼴이라 2.2 의 훑기에 안 걸린다.
  - run85 의 "user32 는 안 쓴다" 와 맞는다.
- **`CsrClientThread` (`+0x70`)** — 쓰는 코드가 **이웃 인사팀 사무실 (csrss) 안** 이다. 그러니 쓰는 건 그 사무실 직원 **자기** 안내판이지, test.exe 직원의 안내판이 아니다. ntdll 에는 쓰는 줄이 없다.
- **`ReservedForCrt` (`+0x1820`)** — ucrtbase 가 OS 호출 앞뒤로 1 / 0 을 쓴다 (`enter_os_call +0x22` · `leave_os_call +0x22`). test.exe 는 CRT 를 **정적으로** 넣어 (`/MT`) `[소스]` ucrtbase 를 안 올린다.

### 2.8 못 찾은 16칸 — "아무도 안 씀" 과 닮았지만, 증명은 아니다

16칸 (run 문서 요약):

- `ActiveRpcHandle` · `User32Reserved` · `UserReserved`
- `GdiCachedProcessHandle` · `GdiClientPID` · `GdiClientTID` · `GdiThreadLocalInfo`
- `glSection` · `glTable`
- `EtwTraceData`
- `TxnScopeEnterCallback` · `TxnScopeExitCallback` · `TxnScopeContext`
- `ReservedForPerf` · `ReservedForCodeCoverage` · `pShimData`

무엇이 확실하고 무엇이 아닌가:

- 3,881개 x64 파일에서 쓰는 줄을 **못 찾았다** `[공백]`.
- 그중 **13칸은 읽는 줄도 없다**. 읽기만 있는 셋은 `TxnScope*` 세 칸이다 — ntdll `TppWorkerThread +0x8bd · +0x9cf` · `TppCallbackCheckThreadAfterCallback +0x1c7` · verifier.dll.
- 성격은 짐 지도의 "아무도 안 씀" 과 같은 종류로 보인다 — 옛 칸 · 계측 칸 · 이름만 있는 칸 (결과서 4.2).
- 하지만 2.2 의 검색 한계 때문에 "없음 증명" 은 **아니다**.

비유로 다시 (태그 없음):

- 이름표는 붙어 있는데, 회사 일지 전체에서 그 칸에 물건을 넣었다는 줄이 **한 번도 안 나온다**.
- "아무도 안 쓰는 칸" 일 가능성이 크다. 다만 "옆 사람에게 안내판을 건네줘서 넣게 한" 일지는 글자 검색에 안 걸리니, **딱 잘라 말하진 못한다**.

#### 이 16칸을 어디에 셀까 — 사용자 결정 대기

- 지금은 짐 지도 3.3 "범위 밖 길에서만 씀 — **다른 DLL 39**" 안에 "쓰는 줄 못 찾음 16" 으로 두었다. 그래서 **2장 숫자는 그대로** 다.
- 옮기면 이렇게 바뀐다 (run 문서 · 짐 지도 7.52):
  - 3.3 "아무도 안 씀" **28 → 44** · "범위 밖" **59 → 43** (다른 DLL **39 → 23**)
  - 2장 위 "아무도 안 씀" 유저 합 · "범위 밖" 유저 합도 같이 바뀐다.
  - "0 그대로 (사진)" 112 와 2장 표 칸은 그대로다.
- 옮길지는 **사용자 결정 대기** 다. 이 글은 정하지 않는다.

### 2.9 ★ test.exe 초기화에서 타는 칸이 0인 까닭 (1): 그 건물을 안 들였다

1. 근거 `[관찰]`: 사진 P2 (`NtContinue` 입구) 의 모듈은 **6개뿐** — test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2 (`ms/logs/04_baggage-read_P2.txt:3397 ~ 3923`).
2. 근거 (결과서): test.exe · dll1 · dll2 는 가져오는 게 KERNEL32 (와 서로) 뿐이고, 정적 CRT 안에도 39칸 쓰는 줄이 0 이다.
3. 그래서 쓰는 줄이 있는 23칸 중 **18칸** 은 처음부터 안 탄다:
   - 안 들인 건물 몫 **16** (gdi32full · user32 · opengl32 · mswsock · combase …)
   - 다른 가게 (csrss) · 관리사무소 (커널 win32k) 몫 **2** (`CsrClientThread` · `Win32ThreadInfo`)
4. 남는 건 들인 건물 안 **KERNELBASE 5칸** 뿐이다 → 2.10 · 2.11.

비유: 그림판 부서 건물이 가게에 없으니, 그림판 부서용 칸은 아무도 건드릴 수 없다.

### 2.10 ★ (2) KERNELBASE 5칸 중 넷 — 길이 없다

KERNELBASE 는 들인 건물이다. 그러니 "쓰는 함수가 초기화 동안 **불리나**" 를 따로 봐야 한다.

어떻게 봤나 (run 문서 "방법") `[정적]`:

- KERNELBASE · kernel32 의 **직접 호출 그래프** 를 만들고,
- 출발점에서 쓰는 함수까지 **닿나** 를 봤다. 출발점은 DllMain 입구 · `_KernelBaseBaseDllInitialize` 의 **PROCESS_ATTACH (이유 1) 갈래만** (`+0x195` 부터) · kernel32 입구 · `BaseThreadInitThunk`.

| 쓰는 함수 (칸) | 직접 호출로 닿나 `[정적]` | 사진 P2 `[관찰]` |
|---|---|---|
| `BaseCopyStream` · `$fin$0` (`StaticUnicodeBuffer`) | 안 닿음 | `""` (2738) |
| `SetThreadStackGuarantee` · `SwitchToFiberContext` (`GuaranteedStackBytes`) | 안 닿음 | 0 (2761) |
| `SetThreadPriority` (`SavedPriorityState`) | 안 닿음 (`CtrlRoutine` 로 가는 주소를 꺼내는 줄만 있음) | null (2767) |
| `Wow64SetThreadDefaultGuestMachine` (`WOW32Reserved`) | 안 닿음 | null (2697) |

- 넷 다 "길 없음 + 사진 0" 이 겹친다. 그래서 **안 탐**.
- 사진 P2 는 모든 `DllMain` 이 끝난 뒤다. KERNEL32 · KERNELBASE 전원 켜기는 `P1a ~ P1b` 사이다.

헷갈리기 쉬운 KERNELBASE 전원 켜기 안 두 줄 (run 문서 호출 지점 표 · 호출 관계) `[정적]`:

- `_KernelBaseBaseDllInitialize +0xdf SetThreadLocale` — **THREAD_ATTACH 갈래** (`+0x4e CMP EDX,2`) 의 콘솔 앱 전용 줄이다. 첫 스레드는 PROCESS_ATTACH 라 안 탄다.
- `_KernelBaseBaseDllInitialize +0x80 · +0x9e` — `SavedPriorityState` 를 **읽는** 줄인데, THREAD_DETACH 갈래 (`RtlCleanUpTEBLangLists` 뒤) 다.
- 곧, 같은 함수 안에 칸 이름이 보여도 **"이유" 갈래** 가 다르면 첫 스레드 초기화와는 상관없다.

#### `NlsCache` 의 다른 쓰는 함수들

- `NlsCache` 를 쓰는 열 함수 중 여섯 (`InternalLcidToName` · `CompareStringW` · `CompareStringA` · `GetSortNode` · `GetStringTypeExW` · `GetLocaleInfoHelper`) 은 **안 닿는다** `[정적]`.
- `NlsValidateLocale` 은 **닿는다** — 단 `TipInitialize` (DllMain 입구가 부름) 아래 JSON 해석 → `WideCharToMultiByte` → `NlsValidateLocale` 사슬이다 `[정적]`.
  - TIP 시험용 갈래이고, 켜지는 조건은 `[공백]` 이다.
  - 사진이 0 이라 안 탔다고 본다 `[관찰 간접]`.
- 남은 둘 (`GetCurrentNlsCache` · `CheckSpecialLocales`) 이 진짜 이야깃거리다 → 2.11.

### 2.11 ★ (3) `NlsCache` — 길은 닿는데, 쓰는 줄이 갈래 밖

이번 바퀴에서 지도 **안** 에 걸리는 유일한 곳이다. 단계별로 간다.

```
[② test.exe 첫 스레드 — KERNEL32 준비 안, P1a ~ P1b]
KERNELBASE!_KernelBaseBaseDllInitialize     (+0x4e CMP EDX,2 — PROCESS_ATTACH 갈래만)
 └─ +0x4e1  BaseNlsDllInitialize
             └─ NlsProcessInitialize → SetupAnsiOemCodeHashNodes → GetCPHashNode
                  ├─ +0x13e  CheckSpecialLocales      쓰기 +0x1283 · +0x12f8 [TEB+0x17a0]   ← 갈래 밖
                  └─ +0x4c6  GetCurrentNlsCache
                               ├─ +0x3d   ECX ← [TEB+0x179c] IsImpersonating
                               ├─ +0x45   JNZ +0x64e        ← 0 이 아닐 때만
                               │            +0xa60 [TEB+0x17a0] 읽기 → 0 이면 +0xfcc 할당
                               │            +0x1021 · +0x1096  [TEB+0x17a0] ← 새 블록
                               └─ (0 이면) 다음 줄로 — 이 갈래엔 NlsCache 쓰기 없음
```

(run 문서 "호출 관계" 를 줄였다. 모두 `[정적]`.)

1. 근거 `[정적]`: 길이 닿는다. KERNELBASE 전원 켜기 `+0x4e1` (ret `+0x4e6`) `BaseNlsDllInitialize` → `NlsProcessInitialize` → `SetupAnsiOemCodeHashNodes` → `GetCPHashNode` → `+0x4c6` (ret `+0x4cb`) `GetCurrentNlsCache` / `+0x13e` (ret `+0x143`) `CheckSpecialLocales`.
   - 이건 run68 해설 2.6 ("NLS — 관리실이 펼친 글자 사전을, KERNELBASE 도 자기 책상에 한 판 더") 의 줄기와 같은 줄기다 (flow.md 2.5 의 `+0x1c NlsProcessInitialize` · run117 의 `GetCPHashNode +0x2d3`).
   - 다만 `BaseNlsDllInitialize` → … → `GetCPHashNode` 사이 call 자리는 결과서에 **이름 사슬로만** 있다.
2. 근거 `[정적]`: `GetCurrentNlsCache +0x3d` 가 TEB `+0x179c` **`IsImpersonating`** 을 읽고, `+0x45 JNZ` 로 0 이 **아닐 때만** `+0x64e` 쪽으로 간다.
   - `IsImpersonating` 은 이름 그대로 읽으면 "이 직원이 지금 남의 신분증을 빌려 일하는 중인가" 표시다 (가장, 이미 푼 말) `[추측: 이름으로 읽음]`. 다만 이번에 KERNELBASE NLS 함수들이 여기에 2 를 쓰는 것도 보였다 (2.12).
3. 근거 `[정적]`: `NlsCache` 를 **만드는** 줄 — `+0xa60` 에서 읽어 0 이면 `+0xfcc` 에서 할당하고 `+0x1021` · `+0x1096` 에서 놓는다 — 은 모두 그 갈래 안이다.
4. 근거 (결과서): 0 일 때 가는 쪽엔 TEB `NlsCache` 쓰기가 **없다**.
5. 근거 `[관찰]`: 사진 P2 에서 `IsImpersonating` = 0 (`_P2.txt:2774`), `NlsCache` = null (`:2775`).
6. **그래서**: 첫 스레드 초기화 땐 `IsImpersonating` 이 0 이라 그 갈래를 안 탄다 → `NlsCache` 는 안 쓰인다 `[정적 + 관찰]`.
7. **아직 확인 아님**:
   - `GetCPHashNode +0x4c6` 자체가 초기화에서 **실제로 불리는** 갈래인지 (`IsUnspecifiedLcid` 쪽) 는 아직 안 봤다 → Q212 ③.
   - "실제로 안 탔다" 는 사진으로 **간접** 확인한 것이다. **썼다가 되돌린** 경우는 사진에 안 남는다 → Q211 D2.

비유 (태그 없음):

- 직원마다 글자 사전의 **개인 사본** 을 둘 자리 (`NlsCache`) 가 안내판에 있다.
- 사본을 만드는 함수 (`GetCurrentNlsCache`) 까지는 첫 직원도 찾아간다.
- 그런데 그 함수는 먼저 안내판의 "**가장 중**" 칸을 본다. 거기가 0 이면 사본 만드는 쪽 문을 열지 않고 지나간다.
- 첫 직원의 "가장 중" 칸은 0 이다. 그래서 사본 자리는 빈 채로 남는다.

헷갈릴 수 있는 곳 하나 — "읽는 줄" 은 어디에 있나:

- 위 트리가 맞다 (run 문서 3절, flow-writer 가 고침).
  - **함수 단위**: `GetCurrentNlsCache` 는 `GetCPHashNode +0x4c6` 아래, 초기화 사슬에 있다 `[정적]`.
  - **줄 단위**: `NlsCache` 를 읽는 `+0xa60` 도 쓰는 줄과 **같은** `IsImpersonating ≠ 0` 갈래 (`+0x45 JNZ +0x64e` 뒤) 안이다 `[정적]`.
  - 그 갈래 **앞** 에서 TEB 를 읽는 줄은 `+0x3d` (`IsImpersonating`) 하나다.
- 남은 공백 하나 `[공백]`: 0 갈래에서도 `+0x64e` 뒤쪽 주소로 뛰어드는 줄이 있다 (`+0x58 JZ → +0x9f0` 등 12줄, plan Q212 ③). 그 길로 `+0xa60` 에 닿을 수 있는지는 줄 단위로 안 따라갔다 → **Q212 ③** 에 붙었다.
- 결론은 그대로다: 초기화 동안 `NlsCache` 는 안 쓰인다. 사진 P2 도 null 이다 `[관찰]`.

### 2.12 곁에서 고친 것, 그리고 이전 run 고쳐 읽기

#### 곁에서 고친 것 둘 (짐 지도 3.3 줄) `[정적]`

1. **`IsImpersonating` (`+0x179c`) 을 쓰는 곳에 KERNELBASE NLS 10 함수가 빠져 있었다.**
   - `InternalLcidToName +0x985` · `CheckSpecialLocales +0xdf6` · `CompareStringW +0xa2b` · `GetSortNode +0x1367` · `CompareStringA +0xd14` · `GetUserOverrideString +0x749` · `GetStringTypeExW +0xad4` · `GetCurrentNlsCache +0xbee` · `NlsValidateLocale +0x940` · `GetLocaleInfoHelper +0xc00`.
   - 값 2 (`MOV R12D,2` → `[TEB+0x179c]`) 를 쓰거나 되돌린다. 뜻은 "스레드별 NLS 캐시를 쓰는 중" 표시로 보인다 `[추측]`.
   - sast/48 은 nt · ntdll 의 쓰기만 적었었다. `GetCurrentNlsCache` · `CheckSpecialLocales` 는 초기화 사슬에 있지만 그 갈래를 안 탄다 (사진 0).
2. **`FatalAppExitA +0x49` 은 `StaticUnicodeBuffer` 가 아니라 `StaticUnicodeString` (`+0x1258`) 줄이다.**
   - 그 줄은 `LEA RCX,[RBX+0x1258]` — 칸 **주소** 를 넘긴다.
   - `StaticUnicodeString` 은 길이와 주소를 담은 **머리** 이고, 그 주소가 TEB + `0x1268` = `StaticUnicodeBuffer` 다 (짐 지도 3.3).
   - 비유: 봉투 **겉면** (길이 · 어디 있나) 과 봉투 **안 종이** 는 다른 칸이다. `FatalAppExitA` 는 겉면을 건넨 거였다.
   - `StaticUnicodeBuffer` 자체에 쓰는 곳은 `BaseCopyStream` 쪽이다 (복사 중 임시 칸으로 쓰고 되돌림 `[추측]`).

#### 이전 run 고쳐 읽기

- **run54** (다른 DLL 짐작을 만든 바퀴):
  - `ws2_32 1` (`WinSockData`) → **mswsock.dll**
  - `WOW64 1` (`WOW32Reserved`) → **KERNELBASE** (`Wow64SetThreadDefaultGuestMachine +0x33`)
  - `GDI 6` → `GdiTebBatch` · `GdiBatchCount` 둘만 쓰는 줄 있음 (gdi32full 주), 넷은 못 찾음
  - 16칸은 짐작 DLL 에도, System32 어디에도 쓰는 줄이 없다.
  - run54 해설 쪽으로는: 2.13 "다른 DLL 39 의 속" 표의 **KERNELBASE 4** 는 이제 `WOW32Reserved` 까지 **5칸** 이 KERNELBASE 몫이다. 4절의 "지금 대기열 항목으로 적혀 있지 않다" 는 Q102 로 들어가 이번에 끝났다.
- **run86** (gdi32 바퀴): ⑧ "짐 지도 3.3 의 `gdi32 6 (Gdi*)` 짐작은 그대로 `[추측]`" → **둘만 맞고 (주인은 gdi32full), 넷은 쓰는 줄 없음**. "`DllMain` 줄기엔 `Gdi*` 쓰기가 없다" 는 그대로 맞다.
- 두 run 문서 맨 위에 flow-writer 가 정정 한 줄씩을 넣었다. 이번에 **두 해설 맨 위** 에도 "고쳐 읽기 (run121)" 한 줄씩을 붙였다. 본문은 그대로 두었다.

### 2.13 PE_emulator 쪽 (결과서 "지금 우리 맞나")

- `../PE_emulator` 코드는 flow-writer 가 안 읽었다. 결과서가 본 줄을 옮긴다. 고칠지는 사용자 결정이다.
- **맞음**: 에뮬레이터는 `Src/nt/Process.cpp` 의 `_FillTeb` (`MmCreateTeb` 13줄) · `_WriteSameTebFlags` 만 TEB 를 채운다. 이 39칸은 안 건드려 0 — 사진 P2 와 같다.
- **확인 필요**: 에뮬레이터가 KERNELBASE 를 **진짜 코드로** 돌리나. 돌린다면 `GetCurrentNlsCache` 가 `IsImpersonating == 0` 갈래로 가는지만 보면 된다 (D1 과 같은 질문).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 (사진 메모) | — | (run121) 정적 바퀴. P2 모듈 6개 · KERNELBASE 5칸 + `IsImpersonating` 의 사진 줄 번호 (flow-writer 가 로그로 다시 맞춤) |
| 기준 (방법 메모) | — | (run121) System32 x64 3,881개 `objdump` 훑기, PDB 12개만 새로, "못 찾음" 은 하한 |
| 2.5 제목 | … run113, run118 | … run113, run118, **run121** |
| 2.5 KERNELBASE NLS 사슬 | `NlsCache` 이야기 없음 | `[정적]` 사슬이 TEB `NlsCache` 를 쓰는 함수까지 **닿지만 쓰지는 않는다** (`IsImpersonating` 갈래), 사진 0 `[관찰]` |
| 4장 2.3 (짐 지도) | 작은 정적: "다른 DLL" 35칸 (이름 짐작) 은 DLL 을 뽑아 확인 | 그 부분 **해결됨** → run121 (쓰는 줄 23 · 못 찾음 16, 짐작 틀림 2, 타는 칸 0) |
| 4장 새 줄 셋 | — | 2.5 동적 D1 · D2 / 2.5 · 짐 지도 3.3 정적 ① ~ ④ · 셈 결정 / 2.5 PE_emulator 쪽 |
| 5장 | — | run121 줄 둘 (근거 run · 03_sprint 요청서 번호) |

### 짐 지도에서 바뀐 것

- 3.3 "범위 밖 길 — 다른 DLL 39": 쓰는 DLL **짐작 `[추측]`** → **쓰는 줄 있음 23 `[정적]` · 못 찾음 16 `[공백]`**.
- 새 **3.3.1**: 칸마다 39줄 (3장 줄 수 · 칸 수에는 안 셈 — 위 한 줄이 이미 39칸을 셈).
- 3.3 고친 줄 둘: `IsImpersonating` (KERNELBASE NLS 10 함수 보탬) · `StaticUnicodeString` (`FatalAppExitA +0x49` 정정).
- 새 **7.52**: 넘긴 질문 · 셈 결정.
- **2장 숫자 그대로.** 초기화 동안 쓰는 칸 0 — 사진 "끝까지 0" 그대로다.

### 큰 그림에서

- TEB 의 "다른 DLL" 줄이 **이름표 짐작** 에서 **일지 확인** 으로 바뀌었다. 하지만 초기화 동안 값 (0) 은 원래부터 사진으로 확실했고, 이번에도 안 흔들렸다.
  - run54 해설 2.13 의 말 그대로다: 불확실했던 건 "나중에 누가 쓰나" 쪽이었다. 이번에 그쪽이 23칸은 닫히고 16칸은 "못 찾음" 으로 좁혀졌다.
- 지도 안에서 새로 보인 건 KERNELBASE NLS 사슬 안의 **갈림길 하나** (`IsImpersonating`) 다. 첫 스레드는 0 이라 안 가는 쪽이다.
- 에뮬레이터 쪽으로는: 이 39칸은 **0 으로 두면 사진과 같다** (결과서 "맞음"). KERNELBASE 를 진짜 코드로 돌릴 때 그 갈림길만 확인하면 된다.
- 다음 차례: plan 의 문서 대기 줄에서 Q102 (sast/117) 다음은 **Q105 (sast/118)** 다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** 초기화 중 `KERNELBASE!GetCurrentNlsCache` 가 정말 불리나, 어느 갈래로 도나 | 사진은 "값" 만 보여 준다. 호출됐는지는 못 본다 | **Q211** (동적, 낮음): 입구에 `bp` (test.exe 조건) → `dd @$teb+0x179c L1` · `dq @$teb+0x17a0 L1` · `k` (`GetCPHashNode+0x4cb` 에서 왔나) |
| **D2** TEB `+0x179c` · `+0x17a0` 에 초기화 중 쓰기가 한 번도 없나 (썼다 되돌림 포함) | 썼다 되돌린 건 사진에 안 남는다 | **Q211** (동적, 낮음): `ba w4 @$teb+0x179c` · `ba w8 @$teb+0x17a0` 를 `LdrpInitializeProcess` 입구부터 `NtContinue` 까지, hit 수 |
| ① `Win32ThreadInfo` 를 유저 TEB 에 **놓는** 줄 (후보 win32kbase `xxxCreateThreadInfo` `0x194d84`) | TEB 를 인자로 넘기는 꼴이라 훑기에 안 걸림. 그 함수를 안 풂 `[공백]` | **Q212 ①** (정적, 낮음 — test.exe 는 GUI 를 안 올림). 필요하면 win32kbase Ghidra analyze 한 번 |
| ② 못 찾은 16칸이 정말 아무도 안 쓰나 | 훑기 한계 (인자 · 스택 · 구조체로 넘긴 뒤 쓰는 꼴), SysWOW64 · .NET 35개 · win32k 밖 드라이버 안 봄 | **Q212 ②** (정적, 낮음) |
| ③ `GetCPHashNode +0x4c6` 가 초기화에서 실제로 불리는 갈래인가 (`IsUnspecifiedLcid` 쪽) | 이번엔 "닿나" 만 봤다 | **Q212 ③** (정적, 낮음 — 새 칸은 없을 것, 짐 지도 7.6 NLS 칸에 이미 줄) |
| ④ `TipInitialize` → `NlsValidateLocale` 갈래가 켜지는 조건 | TIP 시험용 갈래, 조건 `[공백]` | **Q212 ④** (정적, 낮음) |
| 못 찾은 16칸을 짐 지도 "아무도 안 씀" 으로 옮길까 | 세는 기준을 정하는 일 | **사용자 결정 대기** (2.8) |
| 에뮬레이터가 KERNELBASE 를 진짜 코드로 돌리나 | `../PE_emulator` 코드는 안 읽음 | PE_emulator 쪽 확인 — 실행 쪽 질문은 D1 (Q211) 과 같다. 고칠지는 사용자 결정 |

- 동적 둘은 결과서가 "dast 요청 필요 없음" 이라고 했다. 사진 P2 가 이미 0 으로 답했기 때문이다. 그래서 둘 다 **낮음** 이다.
- 임시 도구 (결과서 도구 폴더) 를 커밋할지도 **사용자 결정 대기** 다. 결과서는 이 도구를 `scripts/field_writers.py` 로 정식 도구화하자고 제안했다 (하네스 일 — 오케스트레이터 · 사용자).

---

## 5. 더 읽기

- run 문서: [`run121-teb-other-dlls.md`](run121-teb-other-dlls.md) — 요약, 방법, 결과 1장 (39칸 표), 2장 (KERNELBASE 5칸), 호출 지점 표, 호출 관계, 3장 (곁에서 알게 된 것), 정정, 아직 확인 필요, 도구 메모
- 결과서: [`_workspace/sast/117_teb-other-dlls_result.md`](../../_workspace/sast/117_teb-other-dlls_result.md) — 1장 39칸 표 (방법 약어 OD · PDB · Gh · asm), 2장 묶음별 한 줄, 3장 KERNELBASE 5칸 (사진 줄 번호 짝은 run 문서를 볼 것), 4장 정정 · 새로 알게 된 것, 5장 동적, 6장 다음 요청 후보, 7장 검색 한계
- 요청서: [`_workspace/sast/117_teb-other-dlls.md`](../../_workspace/sast/117_teb-other-dlls.md)
- 짐 지도: [`baggage.md`](baggage.md) — **3.3 `_TEB`** · **3.3.1** (칸마다 39줄) · **7.52**
- 흐름도: [`flow.md`](flow.md) — 기준 (run121 사진 · 방법 메모) · 2.5 KERNELBASE NLS 사슬 글머리 · 4장 run121 줄 · 5장 run121 줄
- 진행표: `_workspace/plan.md` Q102 (끝) · Q211 · Q212, 결정 기록 2026-11-16
- 앞 해설:
  - [run54](run54-teb-fill-해설.md) 2.13 — "다른 DLL" 39 의 속, 칸 이름표 비유 (이번에 "고쳐 읽기 (run121)" 를 붙임)
  - [run86](run86-gdi32-init-해설.md) 2.14 ⑧ · 3절 짐 지도 — `Gdi*` 짐작 그대로 (이번에 "고쳐 읽기 (run121)" 를 붙임)
  - [run85](run85-user32-init-해설.md) — user32 `ClientThreadSetup`, `Win32ClientInfo`
  - [run68](run68-dll-init-rest-해설.md) 2.6 — KERNELBASE 전원 켜기의 NLS
  - [run50](run50-eprocess-create-해설.md) 2.4 · 2.5 — 범위 밖 길 `[추측]`, 오프셋 훑기의 오탐
  - [run44](run44-baggage-snap-해설.md) 2.8 — 사진 지점

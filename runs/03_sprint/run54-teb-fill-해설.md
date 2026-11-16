# run54 teb-fill — 해설

> 이 글은 [run54 문서](run54-teb-fill.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

> **고쳐 읽기 (run121)**: 2.13 "다른 DLL 39 의 속" 표의 짐작 중 `ws2_32 1` (`WinSockData`) 은 **mswsock.dll**, `WOW64 1` (`WOW32Reserved`) 은 **KERNELBASE** (`Wow64SetThreadDefaultGuestMachine +0x33`) 이고 (그래서 KERNELBASE 몫은 4 → 5칸), `GDI 6` 중 쓰는 줄이 있는 건 `GdiTebBatch` · `GdiBatchCount` 둘 (주로 gdi32full) 뿐이다. 39칸 = 쓰는 줄 있음 23 `[정적]` · System32 어디에서도 쓰는 줄을 못 찾음 16 `[공백]` ("없음 증명" 은 아님). 초기화 동안 0 · 2장 숫자는 그대로. 4절의 "지금 대기열 항목으로 적혀 있지 않다" 는 Q102 로 들어가 run121 에서 끝났다. 본문은 그대로 두었다 → [run121 해설](run121-teb-other-dlls-해설.md)

- 대기열: **Q78d3**: Q78d (첫 스레드) 넷 중 **셋째 조각**. `_TEB` 모르는 칸 133 (`_workspace/plan.md`, 이번에 **끝**)
  - 다음 항목: **Q78d4** (스레드 그 뒤 구간, 커널) → Q78e → Q78f → Q78g → Q79 → Q80 → Q82.
  - 남은 질문이 간 곳: D1 (덤프로 답할 수 있음) → **Q79** 출처 · D2·D3 (콜스택이 필요) → 새 **Q101** (맨 뒤) · D4 ~ D6 (낮음) → **Q100**.
- 근거:
  - run 문서 [`run54-teb-fill.md`](run54-teb-fill.md) (가장 중요)
  - 요청서 [`_workspace/sast/48_teb-fill.md`](../../_workspace/sast/48_teb-fill.md)
  - 결과서 [`_workspace/sast/48_teb-fill_result.md`](../../_workspace/sast/48_teb-fill_result.md) (판정: **분석성공**, PDB 일치 ntoskrnl·ntdll 둘 다)
  - 짐 지도 [`baggage.md`](baggage.md) 3.3 (`_TEB` 다시 씀) · 7.17 (새 절)
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (기준 · 2.1 `PspAllocateThread`·`MmCreateTeb` 절 · 2.3 `PspUserThreadStartup` 절 · 2.4 `LdrpInitializeProcess` 트리 · 3장 일꾼 · 4장 · 5장. 그림은 안 바뀜)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 값 대조는 run44 때 찍어 둔 사진 (K2 ~ P2 일곱 장) 으로만 했다. 이 글의 "사진" 은 전부 run44 사진이다.
  - K1 사진엔 TEB 가 아직 없다 (전부 `????`) `[관찰]`. 그래서 대조는 K2 부터다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 네 가지 가름 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀 / 범위 밖 길), DWORD 되쓰기, 겹치는 칸, 훑기 한계 → [run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법) · [2.7](run52-kthread-create-해설.md#27-핵심-2-dword-하나를-되쓰면-왜-다른-비트에-0-을-쓴-것이-되나) · [2.9](run52-kthread-create-해설.md#29-핵심-3-아무도-안-씀-21--범위-밖-길-70--그리고-이-둘의-한계)
- 지우개 (`memset`) 는 안 세고 펜만 센다 → [run53 해설 2.4](run53-ethread-create-해설.md#24-핵심-1-memset-이-칸을-0-으로-밀었는데-왜-0-을-씀-이-아닌가)
- "범위 밖 길에서만 씀 `[추측]`", 오프셋 훑기의 오탐 → [run50 해설 2.4](run50-eprocess-create-해설.md#24-새-셈-범위-밖-길에서만-씀-추측) · [2.5](run50-eprocess-create-해설.md#25-새-말-오프셋-훑기의-오탐)
- TEB = 직원 **개인 안내판**, 작업대 (유저 스택) → [run11 해설 2.2](../02_sprint/run11-allocate-thread-해설.md#22-한-줄씩-읽는-법) · [2.4](../02_sprint/run11-allocate-thread-해설.md#24-갈림길-작업대개인-안내판은-언제-만드나)
- PEB = 가게 **안내판** → [run10 해설](../02_sprint/run10-allocate-process-해설.md#peb)
- 남의 땅에 잠깐 들어가기 (`KiStackAttachProcess`) → [run10 해설](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)
- WOW64 (64비트 Windows 에서 32비트 프로그램을 돌리는 장치) → [run10 해설 3.10](../02_sprint/run10-allocate-process-해설.md#310-ntdll-쪽-조금-더)
- `PspUserThreadStartup` 출근 도장 다섯 칸 (`+0x78` `CurrentLocale`, `+0x99` …) → [run15 해설 2.8](../02_sprint/run15-user-thread-startup-해설.md#28-pspuserthreadstartup-안--출근-도장-다섯-칸)
- TLS = 직원 **개인 사물함**, TEB `+0x58` = 사물함 **열쇠꾸러미** → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) · [run41 해설](../02_sprint/run41-tls-lock-detail-해설.md)
- 활성화 컨텍스트 = 건물 **전용 작업 지침서** → [run27 해설](../02_sprint/run27-init-graph-해설.md) · [run19 해설 2.11](../02_sprint/run19-proc-init-modules-해설.md#211-exe-tls-콜백--전원-다-켠-뒤에)
- `LoadOwner` = **짐 들이기 주인 표시** → [run21 해설 2.11](../02_sprint/run21-load-dll-internal-해설.md#211-잡일-셋--게시판-메인-직원-표시-우회-검사) · [run24 해설](../02_sprint/run24-work-queue-해설.md)
- `UpdateTebApc` = 둘째 **쪽지** → [run53 해설 2.6](run53-ethread-create-해설.md#26-핵심-2-updatetebapc-88바이트-안에-다른-칸이-겹쳐-있다)
- 사진 지점 K1 · K2 · P0 · P1 · P1a · P1b · P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, `_ETHREAD` = 직원 **인사 카드** (run11), `_KTHREAD` = 그 첫 장 **업무 카드** (run52).
- **TEB = 직원 개인 안내판** (run11 해설부터 쭉 이 이름). 이번 주인공이다.
  - run 문서는 TEB 를 "개인 **서랍**", 커널을 "**본사**" 라고 불렀다. run11 해설에서 이미 "flow.md 에선 개인 서랍이라고도 불렀다" 고 적어 뒀다.
  - 이 글은 앞 해설들과 맞추려고 **개인 안내판 / 관리사무소** 로 부르겠다. 뜻은 같다.
- run 문서의 그림은 그대로 가져온다. "관리사무소가 안내판을 **새로** 들여놓는다. 새것이라 빈칸뿐이다. 이름표 몇 장을 붙인다. 그 뒤 직원 본인 (ntdll) 이 칸막이를 넣는다. 나머지 칸은 나중에 **다른 부서**가 쓴다."
- 그 밖에 이어 쓰는 것: `memset` = **지우개** (run52·run53), TLS 벡터 = 사물함 **열쇠꾸러미** (run41), 난수 상태 = **제비뽑기 통** (run53).

### 한 줄로 먼저: 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| `_TEB` 모르는 칸 | **133 → 여전히 모름 0** |
| `K1~K2` 8칸 | 모두 **커널**. `MmCreateTeb` 64비트 갈래 13줄 + `PspAllocateThread +0x793` `[정적]`. 사진 K2 값과 칸마다 맞음 |
| `P0~P1` 5칸 | 커널, 첫 스레드 시작 때 `KiUpdateTebSchedulingPropertiesCurrentThread +0x136 · +0x141` `[정적]` |
| `P1~P2` 8칸 | **ntdll** (TLS 벡터·난수 씨앗·활성화 문맥 스택·`HeapData`·`LoadOwner`·`LastStatusValue`·`FlsData`) `[정적]` |
| 끝까지 0 112칸 | 0 을 씀 **12** · 조건부 안 탐 **13** · 아무도 안 씀 **28** · 범위 밖 길 **59** `[추측]` (ntdll 19 · nt 1 · 다른 DLL 39) |
| TEB 의 0 은 어디서 | `memset` 이 **없다** `[정적]`. 새로 커밋된 페이지라 처음부터 0 `[추측]` |
| `MmCreateTeb +0x175 · +0x17e` | 64비트 TEB 칸이 아니라 **`_TEB32.RealClientId`** (WOW64 갈래, 안 탐) `[정적]` |
| 첫 스레드 `SameTebFlags` | **늘 `0x400` 뿐** — 코드로 닫힘 `[정적]` |
| 간접 호출 | TEB 칸을 쓰는 것 **0개** (`.asm` 36개) `[정적]` |
| 정정 | 짐 지도 3.3 `NtTib` 에 **커널 쓰는 줄**이 빠져 있었다 → 보탬 |

이 글에서 붙잡아 둘 다섯 줄:

> **(1) TEB 는 두 손을 거친다.**
> 관리사무소 (커널) 가 틀을 만들고 이름표를 붙인다. 직원 본인 (ntdll) 이 칸막이를 넣는다. 같은 판을 시간 차로 나눠 쓴다.
>
> **(2) 이번엔 지우개가 없다. 판이 새것이라서다.**
> 그래서 run53 같은 "지우개도 세면?" 대안 숫자도 없다.
>
> **(3) 오프셋 숫자만 보면 옆 건물 칸을 잘못 짚는다.**
> `+0x175 · +0x17e` 는 32비트 안내판 (`_TEB32`) 의 칸이었다.
>
> **(4) "다른 DLL" 39칸 중 35칸은 칸 이름만 보고 짐작한 주인이다.**
> 초기화 동안 0 이라는 것은 사진으로 확실하다. 불확실한 건 "나중에 누가 쓰나" 쪽이다.
>
> **(5) 쓰는 줄을 알아도 "누가 불렀나" 는 실행해 봐야 안다.**
> `LastStatusValue`·`FlsData` 가 그렇다 → Q101.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

TEB 는 **세 시간대에 걸쳐** 채워진다. 그래서 이번 "현재 위치" 는 한 군데가 아니라 셋이다.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 ① 커널, 부모 cmd 스레드 안
  NtCreateUserProcess
   ├─ +0xc36  PspAllocateProcess              가게 장부 (run49·run50)
   ├─ +0xe5c  PspAllocateThread               첫 직원 뽑기 (run52·run53)
   │    ├─ +0x5ef  PspSetupUserStack           작업대 + 스택 경계 메모 (INITIAL_TEB)   ← 이번
   │    ├─ +0x631  MmCreateTeb                 개인 안내판 만들기 (13줄)              ← 이번
   │    ├─ +0x792  KiStackAttachProcess
   │    │  +0x793  SameTebFlags (줄)            "첫 직원" 표시                        ← 이번
   │    └─ …
   ├─ +0xfc2  PspInsertProcess                상가 명부 (run51)
   └─ +0x101a PspInsertThread                 (Q78d4 쪽)
 ② 커널, test.exe 첫 스레드
  KiStartUserThread → PspUserThreadStartup
   └─ +0x99  KeUpdateTebSchedulingPropertiesCurrentThread
              └─ +0x1a KiUpdateTebSchedulingPropertiesCurrentThread   선호 자리       ← 이번
 ③ ntdll, test.exe 첫 스레드
  LdrInitializeThunk → … → LdrpInitializeProcess                      칸막이 넣기     ← 이번
   … P1b ~ P2 (DLL 시작 코드 쪽)                     LastStatusValue · FlsData        ← 이번 (부른 쪽 모름)
  NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다. "(줄)" 이라고 쓴 `+0x793` 은 호출이 아니라 그 함수 안 **쓰는 줄**이다.
- 사진 지점으로 보면: ① 은 `K1~K2`, ② 는 `P0~P1`, ③ 은 `P1~P1a` · `P1a~P1b` · `P1b~P2` 구간이다.

### 무엇이 궁금했나

짐 지도에서 `_TEB` 는 137칸 중 아는 칸이 **4** 뿐이었다. 나머지 133칸을 닫는다 (요청서).

1. `K1~K2` 8칸: `MmCreateTeb` 대입 줄 (run11 "약 47줄") 에서 칸마다 오프셋과 **값의 출처**. 그리고 `MmCreateTeb +0x175 · +0x17e` 가 어느 칸인가.
2. `P0~P2` 13칸: 칸마다 쓰는 함수 (nt / ntdll / 다른 DLL).
3. 끝까지 0 112칸: 네 가지로 가르기. 범위 밖 길은 이번에 처음 **ntdll / nt / 다른 DLL** 셋으로 나눈다.
4. 간접 호출.
5. 칸마다 짐 지도 3장 양식.

- 범위 밖: 일꾼 스레드 2개의 TEB (C10, Q79), `TlsSlots`·`TlsExpansionSlots` 가 가리키는 안쪽, `GdiTebBatch` 같은 큰 배열 안쪽, `_TEB32` (WOW64).

### 바퀴 전에 알던 것

- `[정적]` 아는 칸 4: `NtTib` (ntdll `LdrpMinimalMapModule +0xe2 ~ +0xea` 의 `ArbitraryUserPointer`), `CurrentLocale` (`PspUserThreadStartup +0x78`), `RanProcessInit` (`_LdrpInitialize +0xea`), `SchedulerSharedDataSlot` (`LdrpAcquireSchedulerSharedDataSlot +0x92`).
- `[정적]` (run11) `MmCreateTeb` 안에 TEB 대입이 "약 47줄" 있다. 칸별로는 안 갈랐다 `[공백]`.
- `[정적]` (run52 메모) `PspAllocateThread +0x793` 가 `SameTebFlags` 를 쓴다, 비트 계산 `+0x54a ~ +0x5a5`.
- `[정적]` (run53 메모) `MmCreateTeb +0x175 · +0x17e` 는 `_ETHREAD` 칸이 아니다 (훑기가 `EtwSupport` 로 오인한 줄).
- `[추측]` (flow.md 2.3) `IdealProcessor`·`PrimaryGroupAffinity` 같은 TEB 칸은 `KeUpdateTebSchedulingPropertiesCurrentThread` 쪽일 것.
- `[관찰]` (run44) K1 엔 TEB 가 없고 (`????`), K2 ~ P2 에서 칸마다 값.

---

## 2. 찾은 것

### 2.1 한눈에: 세 시간대, 두 손

run 문서 "호출 관계" 를 줄였다. 모두 `[정적]`.

```
(부모 cmd 스레드, 커널)
NtCreateUserProcess
 └─ +0xe5c  PspAllocateThread
         │  +0x54a ~ +0x5a5  만들기 플래그 → SameTebFlags 값 (첫 스레드는 늘 0x400)
         ├─ +0x5ef  PspSetupUserStack → RtlCreateUserStack   INITIAL_TEB (스택 경계)
         ├─ +0x631  MmCreateTeb
         │       ├─ +0x7f   KiStackAttachProcess
         │       ├─ +0xac   MiAllocateFromSubAllocatedRegion   새 커밋 페이지 (memset 없음)
         │       │  +0xc1 ~ +0x13a  64비트 갈래 13줄
         │       └─ +0x29e  KiUnstackDetachProcess
         ├─ +0x792  KiStackAttachProcess
         │  +0x793  SameTebFlags (16비트 통째)
(test.exe 첫 스레드, 커널)
KiStartUserThread → PspUserThreadStartup
         │  +0x78  CurrentLocale (run15)
         └─ +0x99  KeUpdateTebSchedulingPropertiesCurrentThread
                 └─ +0x1a  KiUpdateTebSchedulingPropertiesCurrentThread
                          +0x136 이상적 프로세서 4B · +0x141 PrimaryGroupAffinity 16B
(test.exe 첫 스레드, ntdll)
LdrpInitializeProcess
 ├─ +0x158  LdrInitializePerThreadRng ⇒ RtlInitializeTRng   RngState 앞 16B
 ├─ +0xb12  RtlpInitializeThreadActivationContextStack     _ActivationStack · ActivationContextStackPointer
 ├─ +0x18d0 LdrpInitializeTls → +0x162 LdrpAllocateTls      ThreadLocalStoragePointer
 ├─ +0x1b91 LdrpDrainWorkQueue           LoadOwner 켬 (마지막 켬)
 └─ +0x1bec LdrpDropLastInProgressCount  LoadOwner 끔
```

- `⇒` 는 꼬리 점프다 (부르고 돌아오는 게 아니라 그대로 넘어감). `LdrInitializePerThreadRng +0x54` 의 `JMP`.
- 트리에 없는 것 (지도 밖): `RtlNtStatusToDosError` (`LastStatusValue`), `RtlFlsSetValue` (`FlsData`), 힙 할당기 (`HeapData`·`RngState` 갱신). 2.10 · 2.11 에서 본다.

비유로 한 번:

- ① 관리사무소가 새 안내판을 들여와 **이름표**를 붙인다: 사번 (`ClientId`), 가게 안내판 위치 (`ProcessEnvironmentBlock`), 작업대 높이 (스택 경계), "첫 직원" 표시 (`InitialThread`).
- ② 첫 출근 날, 관리사무소가 **"선호 자리"** 를 한 번 더 적는다.
- ③ 직원 본인 (ntdll) 이 **칸막이**를 넣는다: 사물함 열쇠꾸러미, 제비뽑기 통, 지침서 더미.
- 나머지 칸 대부분은 그림판 (GDI)·3D (OpenGL)·소켓처럼 **나중에 다른 부서가 쓸 칸**이라 초기화 동안 비어 있다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 가게 안내판 위치를 직원 안내판에 적는 줄

`MmCreateTeb +0x11d` `[정적]`

- **누가**: `MmCreateTeb`. 부모 cmd 스레드가 도는 중인데, `+0x7a KiStackAttachProcess` 로 **새 가게 (test.exe) 땅에 잠깐 들어가 있는** 상태다.
- **어디서**: `+0x11d`. 바로 앞 `+0x116` 이 `EPROCESS.Peb` (+0x2e0) 를 읽는다.
- **누구를 / 무엇을**: TEB `+0x60` `ProcessEnvironmentBlock` 에 PEB 주소를 쓴다.
- **무엇을 위해**: 직원이 자기 안내판만 보고도 **가게 안내판 (PEB)** 을 찾아가게 하려는 것이다. ntdll 은 TEB 에서 PEB 를 꺼내 쓴다 (run18 해설 `RDI = [TEB+0x60]`).
- 사진 K2: `0x9b2fa66000`.

#### 한 줄 읽기 ②: "첫 직원" 표시를 16비트 통째로 쓰는 줄

`PspAllocateThread +0x793` `MOV word [RBX+0x17ee],R12W` `[정적]` (오케스트레이터 `.asm` 확인)

- **누가**: `PspAllocateThread` (접수 창구). `RBX` = TEB, `R12W` = 앞에서 계산해 둔 16비트 값.
- **어디서**: `+0x793`. 바로 앞 `+0x78d` 에서 `KiStackAttachProcess` 로 새 가게 땅에 **다시** 들어간다 (ret `+0x792`). `MmCreateTeb` 는 이미 나왔기 때문이다.
- **무엇을**: TEB `+0x17ee` `SameTebFlags` 16비트를 **통째로** 쓴다. 값이 0 이 아닐 때만 (`+0x66f TEST R12W`).
- **왜 눈여겨보나**: 16비트를 한 번에 쓰니, 켜진 비트 (`InitialThread`) 뿐 아니라 **꺼진 비트들도 같이 쓴 셈**이 된다 → 2.12 "0 을 씀".

#### 한 줄 읽기 ③: 첫 출근 날 "선호 자리" 를 적는 줄

`KiUpdateTebSchedulingPropertiesCurrentThread +0x136` `MOV [RCX+0x1744],EAX` `[정적]` (오케스트레이터 `.asm` 확인)

- **누가**: 이번엔 부모가 아니라 **test.exe 첫 스레드 자신**이 커널에서 돈다. `PspUserThreadStartup +0x94` → `KeUpdateTebSchedulingPropertiesCurrentThread +0x15` → 이 함수.
- **어디서**: `+0x136`. 같은 함수 `+0x141` 이 한 번 더 쓴다 (`PrimaryGroupAffinity`).
- **무엇을**: TEB `+0x1744` 에 4바이트 하나. 이 4바이트에 칸 이름이 다섯 개 겹쳐 있다 → 2.9.
- 사진 P1: `0x01010000`.

### 2.3 133칸은 어디로 갔나

#### 표 (run 문서·결과서 맨 앞 표 그대로)

| 사진 구간 | 칸 | 아는 칸 nt | 아는 칸 ntdll | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 | 범위 밖 ntdll | 범위 밖 nt | 범위 밖 다른 DLL | 모름 |
|---|---|---|---|---|---|---|---|---|---|---|
| `K1~K2` | 8 | **8** | — | — | — | — | — | — | — | 0 |
| `P0~P1` | 5 | **5** | — | — | — | — | — | — | — | 0 |
| `P1~P1a` | 5 | — | **5** | — | — | — | — | — | — | 0 |
| `P1a~P1b` | 1 | — | **1** | — | — | — | — | — | — | 0 |
| `P1b~P2` | 2 | — | **2** | — | — | — | — | — | — | 0 |
| `끝까지 0` | 112 | — | — | **12** (nt 11 · ntdll 1) | **13** (nt 8 · ntdll 5) | **28** | **19** | **1** | **39** | 0 |
| **합** | **133** | **13** | **8** | **12** | **13** | **28** | **19** | **1** | **39** | **0** |

- "아는 칸 다른 DLL" 열은 run 문서 표에 있지만 모두 0 이라 여기선 뺐다.
- `P1b~P2` 두 칸 (`LastStatusValue`·`FlsData`) 은 **쓰는 줄이 ntdll** 이다. 부른 쪽 (KERNELBASE·CRT) 은 `[공백]` 이라 쓴 주인을 ntdll 로 셌다 → 2.11.

#### 한 줄 읽기: 끝까지 0 줄

- 112칸 모두 사진 K2 ~ P2 일곱 장 내내 0 이었다 (예외 0) `[관찰]`.
- 12칸은 0 을 적는 줄이 있고, 13칸은 켜는 줄이 있는데 test.exe 가 안 탄다.
- 28칸은 쓰는 줄이 없다. 59칸은 초기화 길 **밖**에서 쓰는 것 같다 `[추측]`.

#### 짐 지도로 옮길 때 (run52·run53 과 같은 규칙)

- "아는 칸" 21 + "0 을 씀" 12 + "조건부 안 탐" 13 = **46** → 쓴 주인 열 ("nt 가 씀" / "ntdll 이 씀").
- "아무도 안 씀" **28** → 2장 표 아래 줄. **유저 구조체에선 처음**이다.
- "범위 밖 길에서만 씀 `[추측]`" **59** → 모름에서 빼고 따로 한 줄. 이것도 유저 구조체에선 처음.
- 그리고 이미 아는 칸 `NtTib` 에 커널 쓰는 줄을 보탠다 (2.15 정정).

| | nt 가 씀 | ntdll 이 씀 | 다른 DLL | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|---|---|
| `_TEB` (137칸) | 1 → **34** | 3 → **17** | 0 → **0** | 0 → **28** | 0 → **59** | 133 → **0** | 4 + 0 → **50 + 0** |

- nt 34 와 ntdll 17 을 더하면 51 인데 아는 칸은 50 이다. **`NtTib` 한 칸을 양쪽에 셌기 때문**이다 (34 + 17 − 1 = 50).
- `SameTebFlags*` 16비트 통째 칸은 nt 로만 셌다. ntdll 이 뒤에 켜고 끄는 비트는 비트 칸 (`RanProcessInit`·`LoadOwner`) 으로 센다.
- 새 아는 칸 46 은 모두 정확. 짐작 0.
- 짐 지도 전체 (구조체 21개, 직접 칸 1,456): 아는 칸 656 → **702** (정확 601 → **647** · 짐작 55) + 아무도 안 씀 132 → **160** + 범위 밖 길에서만 씀 198 → **257** + 모름 470 → **337**.

### 2.4 핵심 (1): 같은 안내판을 커널이 만들고 ntdll 이 채운다

이번 바퀴에서 가장 먼저 잡아야 할 그림이다. 앞 조각들 (`_KTHREAD`·`_ETHREAD`·트랩 프레임) 은 **커널 혼자** 썼다. TEB 는 다르다.

#### 왜 두 손이 닿나

- TEB 는 **유저 쪽** (test.exe 주소 공간) 에 사는 구조체다 (짐 지도 3.3 제목 "유저, `0x1878`, ntdll PDB").
- 그래서 커널도 쓸 수 있고 (커널은 어디든 쓴다), ntdll 도 쓸 수 있다 (자기 가게 땅이니까).
- 다만 ① 시간대엔 **부모 cmd 스레드**가 돌고 있다. 지금 밟고 있는 땅이 cmd 가게다.
  - 그래서 쓰기 전에 `KiStackAttachProcess` 로 **새 가게 땅에 잠깐 들어간다** ([run10 해설](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)) `[정적]`.
  - 이번에 그 들어가기가 **두 번** 보인다: `MmCreateTeb +0x7a` 와 `PspAllocateThread +0x78d`.

#### 누가 언제 무엇을 (순서대로)

| 차례 | 시간대 (사진 구간) | 누가 | 무엇을 | 근거 |
|---|---|---|---|---|
| 1 | ① 부모, 커널 (`K1~K2`) | `PspSetupUserStack → RtlCreateUserStack` | 작업대 경계를 **메모 (`INITIAL_TEB`)** 에 적는다. TEB 에는 아직 안 쓴다 | `[정적]` |
| 2 | 〃 | `MmCreateTeb` | 새 페이지를 받아 TEB 자리로 쓰고, 13줄로 이름표를 붙인다 | `[정적]` |
| 3 | 〃 | `PspAllocateThread +0x793` | `SameTebFlags` 16비트 (`0x400`) | `[정적]` |
| 4 | ② 첫 스레드, 커널 (`P0~P1`) | `PspUserThreadStartup` | `+0x78` `CurrentLocale` (run15), `+0x94 →` 선호 자리 5칸 | `[정적]` |
| 5 | ③ 첫 스레드, ntdll (`P1~P1a`) | `_LdrpInitialize` · `LdrpInitializeProcess` 와 그 callee, 힙 할당기 | 난수 씨앗 · 지침서 더미 · 열쇠꾸러미 · (이미 아는) `RanProcessInit`·`SchedulerSharedDataSlot` · 힙이 `HeapData` | `[정적]` |
| 6 | 〃 (`P1a~P1b`) | `LdrpDrainWorkQueue` / `LdrpDropLastInProgressCount` | `LoadOwner` 켬 / 끔 | `[정적]` |
| 7 | 〃 (`P1b~P2`) | ntdll `RtlNtStatusToDosError` · `RtlFlsSetValue` | `LastStatusValue` · `FlsData`. **누가 불렀는지는 모름** | `[정적]` 줄 · `[공백]` 호출자 |

- 새 말 — **`INITIAL_TEB`**: 유저 스택의 경계 (`StackBase` `+0x10` · `StackLimit` `+0x18` · `AllocatedStackBase` `+0x20`) 를 담아 `MmCreateTeb` 에 건네는 **작은 메모** `[정적]` (`MmCreateTeb` 두 번째 인자). `RtlCreateUserStack` 이 채운다.
  - 비유: 작업대를 짜 준 목수가 "높이 얼마, 바닥 어디" 를 쪽지에 적어 안내판 담당에게 넘기는 것.
- 비유로: 관리사무소는 **안내판을 들여와 이름표까지만** 붙인다. 칸막이·사물함 열쇠는 직원이 출근한 뒤 **본인이** 넣는다.

#### 같은 칸을 양쪽이 쓰기도 한다: `NtTib`

- `NtTib` 은 TEB 맨 앞 묶음이다. 그 안쪽을 **양쪽이 나눠 쓴다** `[정적]`.
  - 커널 `MmCreateTeb`: `StackBase` (+0x8) · `StackLimit` (+0x10) · `Version` (+0x20 = `0x1e00`) · `Self` (+0x30 = TEB 주소). 줄 `+0x12a · +0x132 · +0xc1 · +0xc5`.
  - ntdll `LdrpMinimalMapModule`: `ArbitraryUserPointer` (+0x28). 원래 알던 줄.
- 그래서 `NtTib` 은 "nt 가 씀" 과 "ntdll 이 씀" 양쪽에 센다 (2.3 의 −1).

#### 같은 비트 묶음도 나눠 쓴다: `SameTebFlags`

- 커널이 `+0x793` 에서 16비트를 **처음으로 통째** 쓴다 (`0x400`).
- 그 뒤 ntdll 이 비트 하나씩 켜고 끈다: `RanProcessInit` (`_LdrpInitialize +0xea`), `LoadOwner` (`LdrpDrainWorkQueue +0xd3` / `LdrpDropLastInProgressCount +0x12`).
- 이 뒤 바뀜이 있어서 칸 이름에 `*` 가 붙는다. 이번엔 **처음 값 줄만** 닫았다.

### 2.5 핵심 (2): 왜 이번엔 몸통 `memset` 이 없어도 0 인가

천천히 가 보자. 앞 바퀴와 **반대**라서 헷갈리기 쉽다.

#### 앞 바퀴 (run52·run53) 는 어땠나

- `_KTHREAD`·`_ETHREAD` 는 `PspAllocateThread +0x307 memset` 이 카드를 **지우개로 민다** `[정적]`.
- 트랩 프레임도 `KiInitializeContextThread +0x187 memset` 으로 민다 `[정적]`.
- 지우개가 필요했던 까닭이 사진에 보였다: K1 사진의 같은 주소엔 **옛 값**이 있었다 (`ExceptionActive` `0xb3` …) ([run52 해설 2.10](run52-kthread-create-해설.md#210-함정-k1-사진의-옛-객체), [run53 해설 2.4](run53-ethread-create-해설.md#24-핵심-1-memset-이-칸을-0-으로-밀었는데-왜-0-을-씀-이-아닌가)).

#### 이번 TEB 는

1. **`memset` 호출이 없다** `[정적]`.
   - `MmCreateTeb` 가 부르는 것: 앞쪽 `KiStackAttachProcess` · `MiAllocateFromSubAllocatedRegion` · `KiUnstackDetachProcess` (`_calls.tsv`).
   - `MiAllocateFromSubAllocatedRegion` 이 부르는 것: `RtlFindClearBitsAndSetEx` · `MiCommitExistingVad` 등. 여기에도 `memset` 이 없다.
2. 그런데 사진 K2 에서 쓰는 줄이 없는 칸은 **모두 0** 이다 `[관찰]`.
3. 그럼 0 은 어디서 왔나 → TEB 페이지는 서브할당 구역을 **새로 커밋한 요구 시 0 페이지**라 처음부터 0 이다 `[추측]` (run 문서·결과서).
4. 이 짐작과 맞는 사진이 하나 더 있다: **K1 엔 TEB 자리가 아예 `????`** 다 `[관찰]`. 지우개가 필요했던 카드들과 달리, 그 자리에 **읽을 페이지 자체가 없었다**.

새 말 셋 (모두 이번 결과를 읽는 데만 필요한 만큼):

- **커밋** = 미리 잡아 둔 주소 자리에 **실제로 쓸 메모리를 붙이는 것** `[추측]` (일반적으로 알려진 설명).
  - 비유: 땅 (주소) 을 사 둔 것과, 그 땅에 **판을 실제로 세우는 것**은 다른 일이다. 커밋은 세우는 쪽.
- **요구 시 0 페이지** = 처음 건드릴 때 운영체제가 **0 으로 채운 새 페이지**를 대 주는 방식 `[추측]` (일반적으로 알려진 설명).
  - 비유: 공장에서 막 나온 **새 안내판**. 누가 쓴 적이 없으니 지울 것도 없다.
- **서브할당 구역** — `MiAllocateFromSubAllocatedRegion` 의 이름에서 온 말이다. 큰 자리를 잡아 두고 그 안을 잘라 나눠 주는 곳으로 보인다 `[추측: 이름]`.
  - TEB 크기 `0x1878` 을 페이지 단위로 올려 `0x2000` 만큼 받는다 (`MmCreateTeb +0x85 ~ +0xa2`) `[정적]`.

#### 태그를 정확히 나누면

| 말 | 태그 |
|---|---|
| `memset` 을 부르지 않는다 | `[정적]` (`_calls.tsv`) |
| 초기화 동안 쓰는 줄 없는 칸이 0 이다 | `[관찰]` (사진 K2 ~ P2) |
| 0 인 **까닭**이 "새로 커밋된 요구 시 0 페이지" 라서다 | `[추측]` (커밋 방식) |

- run 문서 표현대로: "`memset` 부재는 `[정적]`, 커밋 방식은 `[추측]`".

#### 그래서 셈에 무엇이 달라지나

- run53 에선 "지우개도 0 을 쓴 것으로 세면?" 이라는 **대안 숫자** (0 을 씀 139) 가 있었다.
- 이번엔 지우개가 없으니 대안 숫자도 **없다** (run 문서). 요청서 문구와 run50·run52 셈의 차이가 숫자에 영향을 안 준다 (결과서 판정).
- "아무도 안 씀" 28 의 0 은 **새 페이지에서** 온다. run53 처럼 "`memset` 이 0 을 보장하는 유일한 줄" 이 아니다.

#### 내 짐작 하나 `[추측]`

- 에뮬레이터가 TEB 를 흉내 낼 때, `MmCreateTeb` 의 13줄만 옮기고 나머지는 **처음부터 0 인 새 자리**에 두어야 사진과 같은 값이 될 것 같다.
- 이유: 진짜 코드엔 지우는 줄이 없으니, 0 은 "자리 자체가 새것" 이라는 데 기대고 있어서다. 판단은 사용자 몫이다.

### 2.6 `K1~K2` 8칸: 관리사무소가 붙이는 이름표

#### 먼저 갈림길: 13줄만 탄다

- `MmCreateTeb` 의 메모리 쓰기 줄은 **모두 50줄**이다 `[정적]`. run11 의 "약 47줄" 과 같은 묶음이다.

| 갈래 | 줄 | test.exe |
|---|---|---|
| 64비트 (EDI = 4) | **13줄** `+0xc1 ~ +0x13a` | **탄다** |
| WOW64 x86 (EDI = 5, TEB 뒤 `+0x2000` 에 `_TEB32`) | 19줄 `+0x14a ~ +0x1db` | 안 탄다 |
| EDI = 6 (둘째 TEB 쪽) | 17줄 `+0x1e9 ~ +0x27f` | 안 탄다 |
| 나머지 | 1줄 | — |

- 가르는 곳: `EPROCESS.WoW64Process` (+0x310) 가 0 → `R13 = 0` → EDI = 4 (`+0x58 ~ +0x6a`) `[정적]`. test.exe 는 x64 라 0 이다.
- EDI 값 4·5·6 이 각각 무엇을 뜻하는지 이름은 근거에 없다. 갈래를 고르는 값으로만 본다.

#### 13줄이 쓰는 칸 (run 문서 표에서)

| 줄 | 칸 | 값 | 사진 K2 |
|---|---|---|---|
| `+0xc1` · `+0xc5` | `NtTib.Version` · `NtTib.Self` | `0x1e00` · TEB 주소 | (이미 아는 칸 — 정정) |
| `+0xcd` · `+0xd4` | `ClientId` | `_ETHREAD.Cid` 사본 | `0x1dd0` · `0x26ec` |
| `+0xde` | `TxFsContext` | 상수 `0xfffe` | |
| `+0xe9` · `+0xf3` | `RealClientId` | `ClientId` 와 같은 값 | |
| `+0x100` · `+0x10f` | `StaticUnicodeString` 머리 | `MaximumLength` `0x20a` · `Buffer` = TEB + `0x1268` | `Buffer` `0x9b2fa68268` |
| `+0x11d` | `ProcessEnvironmentBlock` | `EPROCESS.Peb` | `0x9b2fa66000` |
| `+0x12a` · `+0x132` | `NtTib.StackBase` · `StackLimit` | `INITIAL_TEB +0x10` · `+0x18` | `StackBase` `0x9b2fd00000` |
| `+0x13a` | `DeallocationStack` | `INITIAL_TEB +0x20` (`AllocatedStackBase`) | `0x9b2fc00000` |

- 모두 `[정적]` 이고 사진 K2 와 칸마다 맞는다.
- 쓰는 줄은 13 인데 표는 8줄이다. 쓰는 줄 둘을 표 한 줄에 묶은 곳이 다섯 군데라서다 (16바이트 칸을 8바이트씩 두 번 쓰는 `ClientId`·`RealClientId` 등).

세 가지만 짚자.

- **`ClientId` 사진 값은 run44 에서 본 스레드 이름표와 같다.** run44 해설 2.8 의 첫 스레드가 `1dd0.26ec` 였다. 프로세스 ID · 스레드 ID 한 쌍이 안내판에 그대로 붙은 것이다.
  - `RealClientId` 는 같은 값의 사본이다. 결과서는 "가장하기에서 원래 ID 를 되찾으려는 사본" 이라고 읽었다.
- **`DeallocationStack` 과 `StackBase` 차이** = `0x9b2fd00000 − 0x9b2fc00000` = 1MB. 결과서가 "예약 1MB" 라고 적은 그 크기다.
  - 비유: 작업대 **바닥** (예약 구역 맨 아래) 과 **지금 쓰는 위쪽 끝** 을 둘 다 이름표로 붙여 둔 것.
- **안 쓰는 것도 있다** `[정적]`: `StaticUnicodeString.Length` (0 그대로), `NtTib.ExceptionList` (+0x0, WOW64 갈래만 `+0x1d8`).
  - 그리고 이 13줄은 끝까지 0 칸에 일부러 0 을 쓰는 줄이 **하나도 없다**. 모두 값이 있는 줄이다.

#### 남은 둘: `SameTebFlags*` · `SameTebFlags.InitialThread`

- `MmCreateTeb` 가 아니라 `PspAllocateThread +0x793` 한 줄이다 (2.2 한 줄 읽기 ②). 자세한 건 2.8.
- 왜 이 칸만 따로 쓰는지 (설계 이유) 는 근거에 없다. **모른다.**

### 2.7 핵심 (3): `MmCreateTeb +0x175 · +0x17e` 는 32비트 안내판 칸이다 — 훑기가 착각한 예

#### 무엇이 수수께끼였나

- run53 (sast/47) 의 훑기는 커널 전체에서 `_ETHREAD` 칸 오프셋과 **숫자가 같은 쓰기 줄**을 모았다.
- 그때 `MmCreateTeb +0x175 · +0x17e` 가 걸렸다. 숫자만 보면 `_ETHREAD` 의 `EtwSupport` 칸 같았다.
- sast/47 은 "이건 `_ETHREAD` 칸이 아니다, TEB 쪽 기준이다" 까지만 말하고 Q78d3 로 넘겼다 ([run53 해설 2.10](run53-ethread-create-해설.md#210-아무도-안-씀-34--범위-밖-길-75-의-한계)).

#### 이번에 따라가 본 순서

1. 두 줄이 쓰는 곳은 `[RCX+0x6b8]` · `[RCX+0x6b4]` 다 (오케스트레이터 `.asm` 확인) `[정적]`.
2. 그런데 **`RCX` 가 무엇이냐**가 핵심이다. 이 줄에서 `RCX` = TEB + `0x2000` 이다 `[정적]`.
3. TEB + `0x2000` 은 WOW64 갈래가 **32비트 TEB** 를 두는 자리다 (2.6 갈래 표).
4. 이 두 줄은 EDI = 5 (WOW64 x86) 갈래 `+0x14a ~ +0x1db` **안**에 있다 `[정적]`.
5. 32비트 TEB 양식 (`pdb_struct.py ntdll.dll _TEB32`) 에서 `+0x6b4` 는 **`RealClientId`** 다 `[정적] PDB`.
   - `+0x6b8` ← `UniqueThread` (`[R14+8]`), `+0x6b4` ← `UniqueProcess` (`[R14]`). 64비트 갈래가 `RealClientId` 에 쓰는 값과 같은 출처다.
6. 그래서 → **64비트 TEB 칸이 아니고, test.exe 는 이 줄을 안 탄다.**

- 새 말 — **`_TEB32`**: 32비트 프로그램 (WOW64) 용 TEB 양식. 64비트 TEB 바로 뒤 (`+0x2000`) 에 따로 놓인다 `[정적]` (결과서). 크기 `0x1038`.
  - 비유: 64비트 안내판 바로 옆에 붙여 세우는 **32비트용 안내판**. 32비트 손님이 올 때만 세운다.

#### 왜 착각이 생겼나

- 오프셋 훑기는 "`+0x6b8` 에 쓴다" 는 **숫자**만 본다. 그 숫자가 **어느 판 기준**인지 (`RCX` 가 무엇인지) 는 따로 따라가야 안다.
- 같은 숫자가 `_ETHREAD` 에도, `_TEB32` 에도 있을 수 있다.
- 비유: "5층 3호" 라는 호수만 보고 찾아가면, **옆 건물에도 5층 3호가 있을 때** 틀린 집 문을 두드린다. 어느 건물 (`RCX`) 의 호수인지가 먼저다.
- 이 사례는 [run50 해설 2.5](run50-eprocess-create-해설.md#25-새-말-오프셋-훑기의-오탐) 의 "오탐" 이 실제로 어떤 꼴인지 보여 준다. 이번 결과서의 훑기 한계 (2.13) 와도 이어진다.

### 2.8 `SameTebFlags`: 첫 스레드는 늘 `0x400` 뿐

#### 값을 만드는 곳 `[정적]` (`PspAllocateThread +0x54a ~ +0x5a5`)

| 만들기 플래그 | → `SameTebFlags` 비트 | 칸 이름 |
|---|---|---|
| `0x40` | `0x400` (비트 10) | `InitialThread` |
| `0x2` | `0x8` (비트 3) | `SkipThreadAttach` |
| `0x80` | `0x2000` (비트 13) | `LoaderWorker` |
| `0x100` | `0x4000` (비트 14) | `SkipLoaderInit` |

- 비유: 직원 뽑을 때 받은 **입사 서류의 체크박스**를 안내판의 표시 칸으로 옮겨 적는 것.

#### 왜 "늘" 인가 — 단계별로

1. 첫 스레드는 `NtCreateUserProcess` 가 만든다. 이 함수가 `ThreadFlags` 를 받아 만들기 플래그를 꾸린다.
2. **`NtCreateUserProcess +0x1bf`** `TEST [RBP+0x7d8],0xfffffffe` → `+0x1ce JZ`: `ThreadFlags` 에 **비트 0 말고 다른 비트가 있으면 거절**한다 (`0xc000000d`) `[정적]`.
   - 그래서 `0x2`·`0x80`·`0x100` 같은 비트를 들고 올 길이 막혀 있다.
   - 들어오는 쪽도 보면, KERNELBASE `CreateProcessInternalW` 가 넘기는 값은 `+0x6cb` 의 상수 1 이다 `[정적]` (결과서).
3. **`+0xdfd`** `OR [RBP+0x10],0x60`: `0x40` 을 **늘** 더한다 `[정적]`.
4. 그래서 → 계산 결과는 늘 `0x400` 이다. 사진 K2 도 `0x400` `[관찰]`.

- 결론: `SkipThreadAttach`·`LoaderWorker`·`SkipLoaderInit` 을 켜는 줄 (`+0x56a · +0x587 · +0x599`) 은 있지만, 이 길에선 **코드로 닫혀 있다**.
  - 이 셋은 로더 일꾼 (`NtCreateThreadEx` 길, Q79) 에서만 켜진다.
  - run53 해설 2.7 의 "체크표를 미리 지운다" 와 비슷한 모양이다. 실행 값을 안 봐도 코드만으로 닫힌다.

#### 16비트를 다 세어 보면

`SameTebFlags` 는 16비트 한 칸 + 비트 16개다 (결과서). 이번에 16비트가 이렇게 갈렸다.

| 비트 | 개수 | 가름 | 2.x |
|---|---|---|---|
| `InitialThread` | 1 | 아는 칸 (`+0x793` 이 켬) | 이 절 |
| `RanProcessInit` | 1 | 원래 아는 칸 (ntdll) | — |
| `LoadOwner` | 1 | 아는 칸 (ntdll 켬/끔) | 2.10 |
| `SkipThreadAttach` · `LoaderWorker` · `SkipLoaderInit` | 3 | 조건부 안 탐 (코드로 닫힘) | 2.12 |
| `ClonedThread` | 1 | 조건부 안 탐 (복제 갈래) | 2.12 |
| `SafeThunkCall` · `InDebugPrint` · `HasFiberData` · `WerInShipAssertCode` · `SuppressDebugMsg` · `DisableUserStackWalk` · `RtlExceptionAttached` · `SessionAware` · `SkipFileAPIBrokering` | 9 | **0 을 씀** (`+0x793` 통째 쓰기의 0 비트) | 2.12 |

- 1 + 1 + 1 + 3 + 1 + 9 = 16.

### 2.9 `P0~P1` 5칸: 첫 출근 날 적는 "선호 자리"

#### 새 말 둘

- **이상적 프로세서** = 이 스레드가 **되도록이면** 돌고 싶은 CPU 로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 직원의 **선호 자리**. 꼭 거기 앉아야 하는 건 아니다.
- **친화도 (affinity)** = 이 스레드가 **돌아도 되는** CPU 목록으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: **앉아도 되는 자리 목록**. 선호 자리는 이 목록 안에서 고른다.

#### 쓰는 줄 `[정적]` (오케스트레이터 `.asm` 확인)

| 줄 | 칸 | 값의 출처 | 사진 P1 |
|---|---|---|---|
| `+0x136` `MOV [RCX+0x1744],EAX` (4B) | `CurrentIdealProcessor` = `IdealProcessorValue`, 그 안 `ReservedPad0·1`·`ReservedPad2`·`IdealProcessor` | `_KTHREAD.UserIdealProcessor` (+0xc4) → 전역 `KiGlobalState` 표 → 그룹 = 값 >> 6, 번호 = 값 & 0x3f | `0x01010000` |
| `+0x141` `MOVDQU [RCX+0x1860],XMM0` (16B) | `PrimaryGroupAffinity` | `Mask` = `UserAffinity` 의 주 그룹 8B, `Group` = `UserAffinityPrimaryGroup` | `0x3` |

#### 칸 다섯 개가 4바이트 하나인 까닭

[run52 해설](run52-kthread-create-해설.md) 의 겹치는 칸과 같은 꼴이다. 같은 4바이트에 **이름이 여러 겹** 붙어 있다 (결과서 "겹치는 칸").

| 바이트 (+0x1744 부터) | 뜻 | 칸 이름 |
|---|---|---|
| 앞 2바이트 | 그룹 | `ReservedPad0` · `ReservedPad1` |
| 셋째 바이트 | 번호 | `ReservedPad2` |
| 넷째 바이트 | 번호 사본 | `IdealProcessor` |
| 4바이트 통째 | 위 묶음 | `CurrentIdealProcessor` = `IdealProcessorValue` |

- 사진 값 `0x01010000` 을 이 표로 읽으면 "그룹 0, 번호 1" 이다 (결과서).
  - x64 는 낮은 바이트가 앞 주소에 온다 `[추측]` (일반적으로 알려진 설명). 그래서 앞 2바이트 = `00 00` (그룹 0), 셋째·넷째 = `01`·`01` (번호 1 과 그 사본).
- 칸 수: `P0~P1` 5칸 = `CurrentIdealProcessor`·`IdealProcessorValue`·`ReservedPad2`·`IdealProcessor` + `PrimaryGroupAffinity`.
  - `ReservedPad0·1` 은 값이 0 이라 "0 을 씀" 쪽 (2.12) 에 들어갔다.
- `PrimaryGroupAffinity` 의 `Mask` `0x3` = 프로세서 0·1 이 앉아도 되는 자리다.
  - 뒤 `Reserved` 6바이트는 이 함수가 안 쓴 **스택 자리를 그대로 옮긴다** `[정적]`. 사진은 0 이고, 에뮬레이터는 0 으로 두면 된다고 결과서가 적었다.

#### flow.md 의 짐작이 맞았다

- flow.md 2.3 은 "`IdealProcessor`·`PrimaryGroupAffinity` 는 `KeUpdateTebSchedulingPropertiesCurrentThread` 쪽일 것 `[추측]`" 이라고 적어 뒀다.
- 이번에 → **`[정적]` 맞다**. 정확히는 그 함수가 부르는 `KiUpdateTebSchedulingPropertiesCurrentThread` 가 쓴다.

#### 같은 함수가 나중에 또 불릴 수 있다 (Q78d4)

- run53 에서 본 **둘째 쪽지** `UpdateTebApc` 의 `NormalRoutine` = `KiUpdateTebApc` 였다 ([run53 해설 2.6](run53-ethread-create-해설.md#26-핵심-2-updatetebapc-88바이트-안에-다른-칸이-겹쳐-있다)).
- 그 `KiUpdateTebApc` 도 **같은 함수** (`KiUpdateTebSchedulingPropertiesCurrentThread`) 를 부른다 `[정적]`.
- 즉 친화도가 바뀌면 쪽지를 펼쳐 **선호 자리를 고쳐 적는 길**이 있다. 쪽지를 큐에 넣는 쪽은 `KiQueueTebUpdateApc` ← `KiSetIdealProcessorThread +0x10e` · `KiSetAffinityThread +0xf1` 이다.
- 이 초기화 길에선 그 쪽지가 아직 안 걸린다. 언제·어떤 조건에서 걸리는지는 **Q78d4** 에서 본다.
- run53 해설이 "`UpdateTebApc` 쪽지가 언제 펼쳐지나 — 근거 없음" 이라고 남겼던 질문에, 적어도 **펼치면 무엇을 쓰는지**는 이번에 보였다.

### 2.10 `P1~P2` 8칸: 직원 본인 (ntdll) 이 넣는 칸막이

모두 `[정적]`, 사진과 맞는다. 칸마다 줄·경로는 짐 지도 3.3.

| 칸 | 쓰는 곳 | 무엇 | 비유 |
|---|---|---|---|
| `ThreadLocalStoragePointer` (+0x58) | `LdrpAllocateTls +0x1e2` (← `LdrpInitializeTls` 호출 `+0x15d`) | 정적 TLS 벡터 포인터. `LdrpTlsBitmap ≠ 0` 일 때만 (`+0x43`). test.exe 는 exe TLS 가 있다 | 사물함 **열쇠꾸러미** |
| `RngState` 앞 16B (+0x260) | `LdrInitializePerThreadRng ⇒ RtlInitializeTRng` | 씨앗 (`LdrSystemDllInitBlock`·TEB 주소·KUSD 틱·스택 주소를 섞음) → splitmix64 로 8B 둘. 뒤 16B 는 아무도 안 씀 | 직원 안내판 위 **제비뽑기 통** |
| `_ActivationStack` (+0x290, 40B) · `ActivationContextStackPointer` (+0x2c8) | `RtlpInitializeThreadActivationContextStack` `+0x15 ~ +0x47` · `+0x52` | TEB **안에** 스택 하나를 만들고, 그 주소 (TEB + `0x290`) 를 포인터 칸에 | **지침서 더미**와 그 위치 메모 |
| `HeapData` (+0x17b0) | 힙 LFH·VS 함수들 (줄 후보 여섯) | DWORD 하나가 아니라 **바이트 넷**을 따로 쓴다 | — |
| `SameTebFlags.LoadOwner` | 켬 `LdrpDrainWorkQueue +0xd3` / 끔 `LdrpDropLastInProgressCount +0x12` | 짐 들이기 주인 표시 | **짐 들이기 주인** 표시 |
| `LastStatusValue` (+0x1250) | `RtlNtStatusToDosError +0x1d` | 마지막 상태 값. 입구에서 **무조건** 저장 | → 2.11 |
| `FlsData` (+0x17c8) | `RtlFlsSetValue +0x104` | 스레드 FLS 블록 포인터. 처음 불릴 때 0x58B 블록을 만들어 놓음 | → 2.11 |

몇 가지만 짚자.

- **활성화 문맥 스택**:
  - 활성화 컨텍스트 = 건물 **전용 작업 지침서** (run27). 지침서를 폈다 덮는 일이 겹칠 수 있으니 **쌓아 두는 더미**가 필요하다 `[추측]` (스택이라는 이름에서).
  - 이번 줄은 그 더미 하나를 **안내판 안 자리** (+0x290) 에 만들고, 포인터 칸 (+0x2c8) 이 자기 안을 가리키게 한다. 사진 `0x9b2fa67290` = TEB + `0x290`.
  - 이미 포인터가 있으면 건너뛴다 (`+0x5`) `[정적]`. test.exe 첫 스레드는 없어서 만든다.
- **`RngState` 는 이름만 같은 다른 통이다.**
  - run53 의 `_ETHREAD.RngState` 는 커널 인사 카드 쪽 제비뽑기 통이었다 (`PsInitializeThreadRngState`).
  - 이번 TEB `RngState` 는 **ntdll 이 씨를 뿌린** 안내판 쪽 통이다. 채우는 함수 이름은 둘 다 `RtlInitializeTRng` 지만 (커널 쪽 · ntdll 쪽 각각), 칸은 다르다 `[정적]` (run 문서 "이름만 같은 다른 칸").
  - 그 뒤 힙 할당기가 통을 계속 흔든다 (`RtlAllocateHeap +0x229 · +0x234` 등, 이름만). 그래서 `*` 칸이다.
- **`HeapData` 는 바이트 넷**:
  - `[+0x17b0]` · `[+0x17b1]` · `[+0x17b2]` · `[+0x17b3]` 를 서로 다른 LFH·VS 함수가 쓴다 (결과서 표).
  - 줄 후보는 여섯 곳이다. 그중 **어느 줄이 먼저 실행되나**는 `[공백]` (D5 → Q100).
- **`LoadOwner` 짝**:
  - 부르는 자리가 여럿이다: 켬 `+0x1302 · +0x19c1 · +0x1aac · +0x1b8c`, 끔 `+0x1753 · +0x1b40 · +0x1be7` (`LdrpInitializeProcess`).
  - 사진 P1a 0 · P1b 1 · P2 0 이니 짝은 **`+0x1b8c` 켬 → `+0x1be7` 끔**으로 보인다 `[추측]` (D6 → Q100).
  - 왜 짐작인가: 사진 세 장은 "그 구간 어딘가에서 켜졌다/꺼졌다" 만 보여 준다. 어느 자리에서 마지막으로 켜고 껐는지는 사진만으로 못 가른다. 그래서 `ba w2` 로 쓰는 순간을 잡아 `k` 를 보자는 것이 D6 이다.

### 2.11 핵심 (4): 쓰는 줄은 알아도 "누가 불렀나" 는 실행해 봐야 안다 (Q101)

`P1b~P2` 의 두 칸 이야기다. 칸은 닫혔는데 질문이 남는다. 그 차이를 천천히 보자.

#### `LastStatusValue` — 무엇을 알았나

1. `RtlNtStatusToDosError` 는 **입구에서 무조건** 받은 상태 값을 TEB 에 저장한다 `[정적]`.
   - `+0xf` `MOV RAX,GS:[0x30]` (TEB 를 꺼냄) → `+0x1d` `MOV [RAX+0x1250],ECX` (저장).
2. 그래서 이 칸을 쓰는 **한 줄**은 `+0x1d` 로 정해졌다.
3. 사진 P2 의 값은 `0xc0000034` (STATUS_OBJECT_NAME_NOT_FOUND) 다 `[관찰]`.
   - 새 말 — **NTSTATUS / 상태 값**: 커널·ntdll 함수가 "잘 됐다 / 이래서 못 했다" 를 돌려주는 32비트 숫자 `[추측]` (일반적으로 알려진 설명). `0xc0000034` 는 이름으로는 "그런 이름의 것을 못 찾았다" 다.
   - 비유: 직원 안내판 구석의 **"마지막 실패 메모"** 칸.

#### 그런데 왜 "누가 불렀나" 는 모르나

- 이 함수는 **여러 길에서 불린다**. 상태 값을 오류 번호로 바꿀 일이 있는 곳이면 어디서든 부른다.
- 그리고 입구에서 무조건 쓰니까, **불릴 때마다 칸을 덮는다** (상태가 0 이어도) (결과서 정정 7).
- 그래서 P2 에 남은 `0xc0000034` 는 **P1b ~ P2 사이 마지막으로 부른 누군가**의 값이다.
- 그 "누군가" 는:
  - 정적으로는 후보가 너무 많다. 결과서·run 문서가 적은 후보는 둘이다: `LdrpInitializeGraphRecurse` 아래 **DLL 시작 코드**인가, KERNELBASE `BaseSetLastNTError` (→ `RtlSetLastWin32ErrorAndNtStatusFromNtStatus +0x4`) 길인가. 둘 다 후보일 뿐이다.
  - 덤프로도 안 된다. 덤프는 **그 순간의 상태** 만 담고, 이미 지나간 **콜스택**은 없다.
  - → 그 함수가 불리는 순간 멈춰서 `k` (콜스택) 를 찍어야 한다.

비유 (태그 없음):

- 우체통 (TEB 칸) 과 투입구 (`+0x1d`) 는 찾았다. 우체통 안 편지 (`0xc0000034`) 도 읽었다.
- 그런데 **누가 넣었는지**는 편지에 안 적혀 있다. 넣는 순간 옆에서 지켜봐야 안다.

#### `FlsData` 도 같은 모양이다

1. 처음 `RtlFlsSetValue` 가 불리면 (`TEB+0x17c8 == 0`) 0x58B 블록을 만들어 0 으로 채우고 `+0x104` 에서 TEB 에 놓는다 `[정적]`.
   - 새 말 — **FLS**: 파이버별 저장 칸 (Fiber Local Storage). TLS 와 비슷한 개인 저장 칸이다 `[추측]` (일반적으로 알려진 설명).
   - 비유: 사물함 (TLS) 옆에 하나 더 있는 **작은 보관함**. `FlsData` 는 그 보관함 묶음 위치.
2. 사진 P2: `0x25996f3c160` (프로세스 힙 안) `[관찰]`.
3. 부르는 쪽은 사용자 CRT → KERNELBASE `FlsSetValue` → ntdll 로 보인다 `[추측]`. 정확한 호출자는 `[공백]`.
4. 이 함수는 **지도 밖**이다 (사용자 코드 쪽에서 내려오는 길).

#### 그래서 무엇이 닫히고 무엇이 남나

| | 상태 |
|---|---|
| 칸을 **쓰는 줄** | 닫힘 `[정적]` (`+0x1d`, `+0x104`) |
| 칸의 **값** | 사진으로 앎 `[관찰]` |
| 칸의 쓴 주인 (짐 지도 열) | ntdll (쓰는 줄이 ntdll 이라서) |
| **누가 그 줄까지 데려왔나** (콜스택) | `[공백]` → **Q101** (D2·D3) |

- 짐 지도 3.3 칸 표는 이것 없이도 **완성**이다 (모름 0). 남는 건 "누가 불렀나" 뿐이다 (짐 지도 7.17).
- 내 짐작 `[추측]`: 에뮬레이터 쪽에서 보면 `RtlNtStatusToDosError` 를 진짜처럼 지으면 이 칸은 저절로 따라 맞을 것 같다 (결과서 정정 7 도 같은 말). 다만 사진과 **같은 값**이 되려면 같은 호출자가 같은 순서로 불러야 하니, 그걸 알려면 Q101 이 필요하다.

### 2.12 끝까지 0 112칸 (1): 0 을 씀 12 · 조건부 안 탐 13 · 아무도 안 씀 28

#### 0 을 씀 12 `[정적]`

| 칸 | 줄 | 어떤 0 |
|---|---|---|
| `SameTebFlags` 의 0 비트 9개 (2.8 표) | `PspAllocateThread +0x793` | 16비트 통째 쓰기의 0 비트 = **입력값 쓰기** (run52 의 DWORD 되쓰기 기준과 같음) |
| `ReservedPad0` · `ReservedPad1` | `KiUpdateTebSchedulingPropertiesCurrentThread +0x136` | 4B 의 그룹 2바이트 = 입력값 (그룹 번호) 0 |
| `HeapWalkContext` | `RtlAllocateHeap +0x6a` → `+0x3d2` | **쓰고 되돌림**: 힙 주소를 넣었다가 끝에서 0 으로 되돌린다. 사진은 할당 도중이 아니라 늘 0 |

- 9비트 중 셋은 범위 밖에서도 쓴다 `[추측]`: `InDebugPrint` ← `vDbgPrintExWithPrefixInternalHelper`, `WerInShipAssertCode` ← `ShipAssert`, `HasFiberData` ← KERNELBASE `ConvertThreadToFiber`. 나머지 여섯은 쓰는 이름을 못 찾았다.
- `HeapWalkContext` 는 비유로: 일하는 동안만 **"지금 정리 중인 선반"** 팻말을 걸었다가, 끝나면 떼는 칸.

#### 왜 2.8 의 `SkipThreadAttach` 같은 비트는 "0 을 씀" 이 아니라 "조건부" 인가

- 그 비트들도 `+0x793` 통째 쓰기에선 0 으로 같이 써진다.
- 다만 결과서 표를 보면, **그 비트를 켜는 줄이 길 안에 따로 있는** 비트 (`SkipThreadAttach`·`LoaderWorker`·`SkipLoaderInit`·`ClonedThread`) 는 조건부 표에, **켜는 줄이 없는** 9비트는 0 을 씀 표에 있다 (결과서 3.1·3.2).
- 이 나눔을 규칙 문장으로 적은 곳은 근거에 없다. 위는 표 두 개를 보고 한 내 읽기다 `[추측]`.

#### 조건부 안 탐 13 — test.exe 가 왜 안 타나 (run 문서 표에서)

| 칸 | 왜 안 탐 |
|---|---|
| `SkipThreadAttach` · `LoaderWorker` · `SkipLoaderInit` (3) | **코드로 닫힘** (2.8). 로더 일꾼 길 (Q79) 에서만 |
| `ClonedThread` · `IsImpersonating` · `MuiImpersonation` (3) | **복제 갈래** — 사용자 스택 인자 ≠ 0 → 일반 길 |
| `WowTebOffset` (1) | `WoW64Process = 0` (x64 exe) |
| `ExtendedFeatureDisableMask` (1) | `KeFeatureBits` 비트 `0x37` 등 — **탔는지 / 탔는데 값 0 인지 `[공백]`** (D1) |
| `LastErrorValue` (1) | 힙 실패 없음 · 값이 다를 때만 씀 |
| `HardErrorMode` (1) | 드라이브 상대 경로일 때만 — 탔어도 되돌려 0 (D4) |
| `HeapFlsData` (1) | 사진 P2 까지 0 → 안 탐 `[관찰 간접]` |
| `TlsSlots` · `TlsExpansionSlots` (2) | 초기화 길에서 `RtlTlsAlloc` 이 불리나 `[공백]` (D4) |

- 칸 수: 3 + 3 + 1 + 1 + 1 + 1 + 1 + 2 = 13.
- 비유: "해당자만 기입" 칸 ([run52 해설 2.6](run52-kthread-create-해설.md#26-핵심-1-끝까지-0-을-네-가지로-가르는-법)). 이 직원은 일꾼도, 복제 직원도, 32비트 손님도 아니다.
- `ExtendedFeatureDisableMask` 는 run52 의 `_KTHREAD` 같은 이름 칸과 **같은 읽기**로 닫힌다 ([run52 해설 2.8](run52-kthread-create-해설.md#28-조건부-안-탐-12-testexe-첫-스레드가-탄-길)). 덤프에서 전역 하나만 읽으면 두 칸이 한꺼번에 풀린다 → D1, Q79.
- `HardErrorMode`·`TlsSlots`·`TlsExpansionSlots` 는 "탔어도 0 으로 끝나는 칸" 이라 정적으로 못 가른다 (결과서). 칸 값 (0) 은 어느 쪽이든 같다.

#### 아무도 안 씀 28

- 쓰는 줄을 커널 길·ntdll 지도 함수·KERNELBASE·KERNEL32 어디서도 못 찾은 칸이다. 값 0 은 새 페이지에서 온다 (2.5).
- 대표:
  - `Padding0 ~ Padding8` (9): 정렬용 빈 칸.
  - `Instrumentation*` 5칸: OS 는 읽기만 하고 **사용자가 설치한 계측 콜백**이 쓰는 칸으로 보인다 `[추측]`.
  - `Vdm`·`FpSoftwareStatusRegister` 등은 옛 칸, `ChpeV2CpuAreaInfo` 는 ARM64 전용 `[추측]`.
  - `TlsLinks`·`CountOfOwnedCriticalSections`·`Instrumentation`·`EffectiveContainerId` 는 쓸 코드가 있을 법한데 못 찾았다 `[공백]` (훑기 한계 안).
- 전체 28칸 목록은 run 문서.

### 2.13 핵심 (5): 범위 밖 길 59 — 그리고 "다른 DLL" 35칸은 이름만 보고 짐작했다

#### 이번에 처음 셋으로 나눴다

- 커널 구조체 (run50 ~ run53) 에선 "범위 밖 길" 이 거의 다 커널 함수였다.
- TEB 는 **유저 구조체**라 쓰는 쪽이 커널만이 아니다 (run 문서). 그래서 셋으로 나눴다.

| 쓰는 쪽 | 칸 | 대표 |
|---|---|---|
| **ntdll** (지도 밖 ntdll 함수) | 19 | `ThreadPoolData` ← `TppAllocThreadData`, `ActivityId` ← `EtwEventActivityIdControl`, `PreferredLanguages` ← `RtlSetThreadPreferredUILanguages`, `LockCount` ← `RtlLockCurrentThread` |
| **nt** | 1 | `ExceptionCode` ← `KeRaiseUserException +0x63` (잘못된 핸들 예외, `.asm` 있음) |
| **다른 DLL** | 39 | 아래 |

- 한 칸을 둘 이상이 쓰면 하나에만 셌다: 길 안 분류가 있으면 그것으로, 없으면 ntdll > nt > 다른 DLL 순 (run 문서).

#### "다른 DLL" 39 의 속 — 근거의 세기가 셋이다

| 묶음 | 칸 | 근거 |
|---|---|---|
| KERNELBASE (쓰는 줄 확인) | **4** — `StaticUnicodeBuffer`·`NlsCache`·`GuaranteedStackBytes`·`SavedPriorityState` | KERNELBASE 를 `objdump` 로 훑어 **쓰는 줄을 봤다** |
| 칸 이름으로 짐작한 DLL | 29 — GDI 6 (`GdiTebBatch` 등) · user32 3 · win32kbase 1 · opengl32 8 · ws2_32 1 · rpcrt4 2 · ole32/combase 4 · advapi32/sechost 2 · CSR 1 · WOW64 1 | 그 DLL 을 **열어 보지 않았다**. 칸 이름 (`Gdi…`, `gl…`, `WinSockData` …) 으로 주인을 짐작 `[추측]` |
| 이름만으로 짐작 | 6 — `ReservedForPerf`·`ReservedForCodeCoverage`·`ReservedForWdf`·`ReservedForCrt`·`ReservedForDebuggerInstrumentation`·`pShimData` | 〃 `[추측]` |

- 29 + 6 = **35** 가 run 문서가 말한 "칸 이름으로 짐작한 DLL" 이다. 4 + 35 = 39.
- 작은 덧붙임 (결과서): `WOW32Reserved` 는 KERNELBASE 에서도 쓰는 줄이 하나 보였지만, 주인으로 적은 `wow64cpu` 는 짐작이다. CSR 칸은 "ntdll 에 `CsrNewThread` 가 없다" 는 근거가 붙어 있다.

#### 왜 35칸을 짐작으로 둘 수밖에 없었나

- 이번 훑기 대상은 **ntdll · 커널 · KERNELBASE · KERNEL32** 넷이다 (결과서). gdi32·opengl32·ws2_32 같은 DLL 은 대상이 아니었다.
- 요청서도 처음부터 "GDI·OpenGL·Winsock·OLE·RPC·CSR·WOW64 처럼 다른 DLL 이 나중에 쓰는 칸은 이름만 — 줄마다 찾지 않아도 된다" 고 정했다.
- 그러니 이 35칸의 "누가 쓰나" 는 **DLL 을 뽑아 봐야** 확인된다 (run 문서 "아직 확인 필요" 의 작은 정적).

비유 (태그 없음):

- 안내판에 "그림판 부서용", "3D 부서용", "소켓 부서용" 이라고 **칸 이름표**가 붙어 있다.
- 우리는 그 부서 사무실에 가 보지 않고, **이름표만 보고** "이 칸은 그림판 부서가 쓰겠지" 라고 적었다.
- KERNELBASE 4칸만 그 부서에 직접 가서 **쓰는 손을 봤다**.

#### 그래도 확실한 것 하나

- 59칸 모두 **초기화 동안 (K2 ~ P2) 0** 이다 `[관찰]` (사진 일곱 장, 예외 0).
- 그리고 초기화 길 (커널 길·ntdll 지도) 안에서 이 칸을 쓰는 줄은 **못 찾았다** `[정적]` (훑기 한계 안에서).
- 그러니 불확실한 건 **"나중에 누가 쓰나"** 쪽이다. **"초기화 동안 무슨 값인가"** 는 흔들리지 않는다.
- 이 구분이 run 문서가 59칸을 "모름" 이 아니라 따로 한 줄 ("범위 밖 길에서만 씀 `[추측]`") 로 둔 까닭이다 ([run50 해설 2.4](run50-eprocess-create-해설.md#24-새-셈-범위-밖-길에서만-씀-추측)).

#### 훑기 한계 — 이번에 달라진 것만

방법·결론은 [run52 해설 2.9](run52-kthread-create-해설.md#29-핵심-3-아무도-안-씀-21--범위-밖-길-70--그리고-이-둘의-한계) · [run53 해설 2.10](run53-ethread-create-해설.md#210-아무도-안-씀-34--범위-밖-길-75-의-한계) 그대로다. 이번 TEB 에서 새로 생긴 어려움 (run 문서):

- **TEB 를 얻는 길이 여럿**이다. ntdll 은 `gs:[0x30]` 로 TEB 를 꺼낸다 (run18 해설). 그 레지스터를 분기·레이블을 넘어 따라가며 모았다.
  - 첫판 훑기는 선형으로만 읽어 `pop r14; ret` 뒤 레이블에서 TEB 를 잃었고, `_LdrpInitialize +0xea` 같은 줄을 놓쳤다 (도구 메모).
- **TEB 를 인자로 받는 함수** (`RtlpInitializeThreadActivationContextStack` 은 `RCX` 로, `RtlpFlsSetValue` 는 슬롯 포인터로) 는 숫자 일치 후보를 **눈으로** 가렸다 → 오탐·놓침 가능.
- **인덱스 쓰기** (`TlsSlots[i]`) 도 눈으로 가렸다.
- 커널 쪽은 `[x+0xf0]` (`_KTHREAD.Teb`) 에서 읽은 레지스터만 따라갔다.
- 오탐으로 버린 실제 예: `RtlpAllocateActivationContextStackFrame +0x1fd [RCX+0x2c8]` — 숫자는 `ActivationContextStackPointer` 와 같지만 TEB 가 아니라 프레임 캐시 칸 (결과서). 2.7 의 "옆 건물 같은 호수" 와 같은 꼴이다.

### 2.14 갈림길 다섯 · 간접 호출 없음

#### test.exe 가 탄 길 (run 문서 표)

| 갈림길 | 가르는 곳 | test.exe |
|---|---|---|
| WOW64 | `EPROCESS.WoW64Process` (`MmCreateTeb +0x58 ~ +0x6a`, `PspAllocateThread +0x79b`) | 0 → 64비트 갈래 13줄 |
| 첫 스레드 플래그 | `NtCreateUserProcess +0x1bf` · `+0xdfd` | `SameTebFlags = 0x400` |
| 복제 | 사용자 스택 인자 = 0 (`PspAllocateThread +0x5ab ~ +0x5b3`) | 인자 ≠ 0 → 일반 길 |
| TLS | `LdrpTlsBitmap ≠ 0` (`LdrpAllocateTls +0x43`) | exe TLS 있음 → 벡터 만듦 |
| 활성화 문맥 스택 | TEB 포인터가 이미 있나 (`+0x5`) | 없음 → 만듦 |

- 갈림길 자체는 `[정적]`. test.exe 가 어느 쪽인지는 사진 값으로 맞춰 본 것 `[관찰 간접]`.
- 복제 갈래 (`+0x2cab6d ~ +0x2cabbd`, 먼 조각) 도 `ClientId`·`RealClientId` 를 쓰지만, **만들어 둔 TEB 를 다시 쓰는 길**이라 test.exe 는 안 탄다 `[정적]`. 먼 조각은 [run53 해설 2.10](run53-ethread-create-해설.md#210-아무도-안-씀-34--범위-밖-길-75-의-한계).

#### 간접 호출

- 이 길에서 TEB 칸을 쓰는 간접 호출은 **0개** `[정적]`. `.asm` 36개 (커널 14 · ntdll 22) 를 봤다.
- 간접 호출 자체는 ntdll 에 하나 있다: `_LdrpInitialize +0x9a CALL RAX` (`g_LdrpWow64LdrpInitialize`). `UseWOW64 ≠ 0` 갈래라 x64 test.exe 는 안 탄다 (run17·run18 해설에서 본 그 줄). → Q35 에 넣을 것 없음.

### 2.15 정정 (run 문서 "정정")

| 무엇 | 전 | 후 |
|---|---|---|
| 짐 지도 3.3 `NtTib` 줄 | ntdll `ArbitraryUserPointer` (+0x28) 만 | + **커널 `MmCreateTeb`** 가 `StackBase`·`StackLimit`·`Version`·`Self` 를 씀 (`+0x12a · +0x132 · +0xc1 · +0xc5`). `NtTib` 은 nt·ntdll 양쪽 |
| 3.3 머리 메모 (run52·run53) | "Q78d3 에서 볼 것" | `.asm` 으로 **맞다**. 표로 옮기고 `(해결됨 → run54)`. 보탬: 첫 스레드는 늘 `0x400`, `+0x175 · +0x17e` 는 `_TEB32` |
| flow.md 2.1 `MmCreateTeb` "약 47줄" | 칸별로 안 갈랐다 `[공백]` | 정확한 수 **50줄** (갈래 셋) 을 보탬. 같은 묶음이다 |

- `NtTib` 정정 덕에 사진 하나가 설명된다: 사진 K2 에서 `NtTib` 이 **이미 0 이 아닌** 까닭이 이 커널 줄이다.
  - 전엔 ntdll 줄만 있어서, K2 (ntdll 이 돌기 전) 에 값이 있는 게 어색했다.
- run11·run43 문장은 "약" 이라 틀리지 않아서 안 고쳤다. 이전 run 문서 맨 위 정정 안내: **없음** (run 문서).
- 칸 수·아는 칸 수는 그대로다 (`NtTib` 은 이미 아는 칸이었다). "nt 가 씀" 만 +1.

### 2.16 정리: 비유로 한 번 더

1. 목수 (`RtlCreateUserStack`) 가 작업대를 짜고, 높이·바닥을 **쪽지 (`INITIAL_TEB`)** 에 적는다.
2. 관리사무소 (`MmCreateTeb`) 가 새 가게 땅에 들어가 **새 안내판**을 세운다. 공장에서 막 나온 것이라 빈칸뿐이다 (지우개 없음).
3. 64비트 직원이니 **64비트용 이름표 13장**만 붙인다: 사번 둘, 가게 안내판 위치, 작업대 높이·바닥, 기본 문자열 칸 머리, 몇 가지 상수. 32비트용 안내판 (`_TEB32`) 은 안 세운다.
4. 땅에서 나왔다가, 접수 창구 (`PspAllocateThread`) 가 **다시 들어가** "첫 직원" 표시 (`0x400`) 를 16칸 띠째로 붙인다. 첫 직원은 이 띠가 늘 같다.
5. 직원이 첫 출근해 커널을 지날 때, 관리사무소가 **선호 자리**와 **앉아도 되는 자리 목록**을 적는다.
6. ntdll 로 넘어오면 직원 본인이 **제비뽑기 통**에 씨를 뿌리고, **지침서 더미**를 안내판 안에 만들고, **사물함 열쇠꾸러미**를 건다.
7. 짐 들이는 동안 "**짐 들이기 주인**" 표시를 켰다 끈다. 힙이 `HeapData` 칸 네 바이트를 만진다.
8. DLL 시작 코드 쪽 어딘가에서 **마지막 실패 메모**와 **작은 보관함 위치**가 적힌다. 누가 적게 했는지는 아직 모른다.
9. 나머지 칸 대부분은 그림판·3D·소켓 부서가 **나중에** 쓸 자리라 비어 있다. 그 부서 이름은 대부분 칸 이름표를 보고 짐작한 것이다.

### 2.17 곁가지: 도구 메모

run 문서 "도구 메모" 에서 — 하네스 제안이라 오케스트레이터가 사용자에게 묻는다.

- **제안 1 `field_writers.py --struct _TEB`** (sast/43·44·46·47 이어서): 분기·레이블 합치기가 있는 레지스터 추적, 인자로 받은 구조체 포인터 따라가기, `.asm` 오프셋으로 바꿔 쓰기. 2.13 의 한계를 줄이려는 것이다.
- 제안 2: `pdb_struct.py` 의 겹치는 칸을 "같은 바이트 묶음" 으로 보여 주는 옵션. 2.9 의 다섯 이름 표를 이번에도 손으로 맞췄다.
- 제안 3 (사소): `ExportFunctions` 가 이미 `.asm` 이 있는 함수는 건너뛴다고 알려 주기.
- 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb·ntdll.pdb 쓴 run 목록에 run54 없음 | 둘 다 run54 추가. "(run54) 정적 바퀴, K2 ~ P2 일곱 장 다시 읽음. K1 엔 TEB 가 아직 없다" 한 줄 |
| 2.1 `PspAllocateThread` 절 | run52 메모: TEB `SameTebFlags` (`+0x793`) 는 Q78d3 로 넘김 | `(TEB 쪽 해결됨 → run54)` + `[정적]` `+0x793` 16비트 통째 쓰기, **첫 스레드는 늘 `0x400`** (`[관찰]` K2 와 맞음), 복제 갈래 TEB 쓰기는 안 탐 |
| 2.1 `MmCreateTeb` 절 | "약 47줄, 칸별로는 아직 안 갈랐다 `[공백]`" | `(해결됨 → run54)` + `[정적]` **50줄 (갈래 셋)**, 64비트 13줄이 쓰는 칸, `+0x175 · +0x17e` = `_TEB32.RealClientId`, **`memset` 없음** (`[추측: 커밋 방식]`) |
| 2.3 트리 (`PspUserThreadStartup` 아래) | `+0x99 KeUpdateTebSchedulingPropertiesCurrentThread` 까지 | `·` 줄 `+0x1a KiUpdateTebSchedulingPropertiesCurrentThread` (`+0x136`·`+0x141`), "그림엔 없음" |
| 2.3 `PspUserThreadStartup` 절 | `IdealProcessor`·`PrimaryGroupAffinity` 는 3번 쪽일 것 `[추측]` | → **맞다** `[정적]`. 두 줄과 사진 값 (`[관찰]` P1 `0x01010000`·`0x3`), `KiUpdateTebApc` 도 같은 함수를 부름 (Q78d4) |
| 2.4 `LdrpInitializeProcess` 트리 | `+0x158 LdrInitializePerThreadRng` 이름만, `LdrpAllocateTls` 줄 | `+0x158` 에 "⇒ `RtlInitializeTRng` → TEB `RngState` 앞 16B", 새 `·` 줄 `+0xb12 RtlpInitializeThreadActivationContextStack`, TLS 줄 끝에 `+0x1e2 TEB ThreadLocalStoragePointer` |
| 3장 일꾼 2개 | `SameTebFlags` 비트로 로더 일을 건너뛰는지 `[추측]` | + `[정적]` 비트를 켜는 줄 `+0x56a · +0x587 · +0x599`, 첫 스레드 길에선 코드로 닫힘. 일꾼 값은 그대로 동적 (Q79) |
| 4장 | run52·run53 "다음 조각에 넘김" 의 Q78d3 | `(해결됨 → run54)` + 해결 줄, (run54 새로) TEB 칸 실행 확인 (D1 → Q79, D2·D3 → 새 항목, D4 ~ D6 → Q100), (run54 새로) Q78d4 넘김 |
| 5장 | — | run54 줄 둘 |

- 그림 (mermaid) 은 안 바뀌었다. 새 단계·새 caller 가 없다 (run 문서 "그림"). `KeUpdateTebSchedulingPropertiesCurrentThread` 는 이미 그림 2 노드라, 그 안 callee 는 글 트리에만 넣었다.

태그 변화 대표:

- `MmCreateTeb` TEB 대입: "약 47줄" + 칸별 `[공백]` → **`[정적]` 50줄, 칸마다 줄**.
- `P0~P1` 다섯 칸의 쓰는 곳: `[추측]` (flow.md 2.3) → **`[정적]` `+0x136 · +0x141`**.
- `MmCreateTeb +0x175 · +0x17e`: "`_ETHREAD` 칸 아님" 까지만 → **`[정적]` `_TEB32.RealClientId`**.
- 첫 스레드 `SameTebFlags`: 값만 `[관찰]` `0x400` → **`[정적]` 늘 `0x400` (코드로 닫힘)**.
- TEB 의 0: 말 없음 → **`memset` 없음 `[정적]` + 새 페이지 `[추측]`**.

### 짐 지도 `baggage.md` 에서 바뀐 것 (열둘째판)

- **3.3 `_TEB` 를 다시 썼다**: 줄 4 → **38** (새 줄 34, 그중 `NtTib` 커널 줄 1 은 이미 아는 칸에 보탬). 줄 끝 "**Q78d3 로 닫힘**".
  - 머리에 "만드는 곳" (코드 순서), "0 은 어디서", "겹치는 칸", "복제 갈래" 메모.
- 새 줄임 둘: **`①TB…`** = `①A… → MmCreateTeb (호출 +0x62c)`, **`①TS…`** = `①T… → KeUpdateTebSchedulingPropertiesCurrentThread (호출 +0x94) → KiUpdateTebSchedulingPropertiesCurrentThread (호출 +0x15)`.
  - 주의 (run 문서): `①TB…` 는 글자가 T 로 시작하지만 **`①T…` 아래가 아니라 `①A…` 아래**다. `①TS…` 는 `①T…` 아래가 맞다.
- 새 **7.17** = 이번 동적 질문 (D1 ~ D6).
- 2장 숫자는 2.3 표 그대로. "아무도 안 씀"·"범위 밖 길" 줄이 **유저 구조체에 처음** 생겼다.

### 큰 그림에서 어떻게 이어지나

- **첫 스레드의 "짐" 이 커널 → 유저로 처음 건너갔다.** `_KTHREAD` (run52) · `_ETHREAD`·트랩 프레임 (run53) 은 모두 관리사무소 안 서류였다. TEB 는 **가게 땅 위**에 서는 첫 스레드 짐이다.
  - 그래서 처음으로 한 구조체에 **nt·ntdll 두 쓴 주인**이 같이 나온다 (2.4).
- **값의 출처가 다시 한 번 "자기 자신" 쪽이다.** 사번 (`_ETHREAD.Cid` 사본), 가게 안내판 주소 (`EPROCESS.Peb`), 작업대 경계 (`INITIAL_TEB`), 스레드 객체의 선호 자리. 부모 cmd 에서 베껴 오는 칸은 이번에 안 보였다.
- **유저 공간 짐인데 쓰는 손은 둘이다.** TEB 는 유저 쪽 (에뮬레이터 가상 주소 공간 쪽) 에 놓이는 짐이다 (짐 지도 3.3 제목 "유저"). 그런데 처음 값을 쓰는 손은 커널 줄 (이번에 본 것: `MmCreateTeb` 13 · `PspAllocateThread` 1 · `KiUpdateTeb…` 2, 결과서 줄 수) 과 ntdll 줄로 나뉜다.
  - 내 짐작 `[추측]`: 2.5 대로 TEB 자리는 처음부터 0 이어야 하고, 2.11 의 두 칸은 호출자를 알기 전까진 "사진 값과 같게" 맞추기 어려울 것 같다. 이유는 각 절에. 어디까지 구현할지는 사용자와 정할 일이다.
- 다음 조각 **Q78d4** 는 같은 직원의 **그 뒤 구간** (커널) 이다. `PspInsertThread` 의 `CrossThreadFlags` 되쓰기, `KeStartThread` 쪽, 그리고 이번에 넘긴 **`UpdateTebApc` 쪽지가 TEB `P0~P1` 다섯 칸을 다시 쓰는 길**을 본다.

---

## 4. 아직 모르는 것

번호 D1 ~ D6 은 결과서 그대로다. run52·run53 의 D 번호와 **다른 질문**이다. **안 닫아도 짐 지도 3.3 칸 표는 완성**이다 (모름 0). 남는 건 "누가 불렀나" · "어느 줄이 먼저인가" 다 (짐 지도 7.17).

멈춤점 조건 (지금 프로세스 test.exe, P1b 뒤 등) 은 run 문서 "아직 확인 필요" 에 적혀 있다. 실제로 걸 때의 판단은 dast-mentor 몫이다.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** `ExtendedFeatureDisableMask` (TEB `+0x1848`) 갈래를 탔나 / 탔는데 0 인가 | 전역 `KeFeatureBits` 비트 `0x37` · KUSD `+0x3d8` 값은 실행 때 값 | 덤프 **Q79** (run52 D1 과 같은 읽기 — `_KTHREAD`·`_TEB` 두 칸이 한 번에 닫힘) |
| **D2** `LastStatusValue` = `0xc0000034` 를 쓴 `RtlNtStatusToDosError` 의 **호출자** | 부르는 길이 많고, 덤프엔 지나간 콜스택이 없음 (2.11) | 멈춤 **Q101**: `bp ntdll!RtlNtStatusToDosError` + 값 `0xc0000034` 조건 → `k` |
| **D3** `FlsData` 를 처음 놓는 `RtlFlsSetValue` 의 **호출자** (CRT → KERNELBASE → ntdll 인가) | 지도 밖 길, 덤프엔 콜스택 없음 | 멈춤 **Q101**: `bp ntdll!RtlFlsSetValue` + `FlsData == 0` 조건 → `k` |
| **D4** 초기화 길에서 `RtlTlsAlloc`·`RtlpHpEnvTlsAlloc` 이 불리나 (`TlsSlots`·`TlsExpansionSlots`·`HeapFlsData`·`HardErrorMode` 가 "안 탐" 인지 "탔는데 0" 인지) | 탔어도 0 으로 끝나는 칸이라 사진으로 못 가름 | 멈춤 **Q100** (낮음): hit 수 |
| **D5** `HeapData` 바이트를 처음 쓰는 줄 (후보 여섯 중) | 정적으론 실행 순서를 못 정함 | 멈춤 **Q100** (낮음): `ba w1` |
| **D6** `LoadOwner` 켬/끔 짝이 `+0x1b8c` ↔ `+0x1be7` 인가 | 사진 세 장은 구간만 보여 줌 (2.10) | 멈춤 **Q100** (낮음): `ba w2` + `k` |
| `UpdateTebApc` 쪽지가 TEB `P0~P1` 다섯 칸을 언제 다시 쓰나 · `KeStartThread` 쪽 `UserAffinity`·`UserIdealProcessor` 최종 값 | 이번 범위 밖 (큐에 넣는 쪽은 이름만 봄) | 정적 **Q78d4** |
| "다른 DLL" 35칸의 진짜 주인 | 그 DLL 을 안 뽑음. 칸 이름으로 짐작 `[추측]` (2.13) | 작은 정적 — DLL 을 뽑아 봐야 함. **지금 대기열 항목으로 적혀 있지 않다** (run 문서·flow.md 4장에 "작은 정적" 메모만) |
| "범위 밖 길" 59 · "아무도 안 씀" 28 의 쓰는 함수가 맞나 | `objdump` 훑기 (TEB 인자 전달·인덱스 쓰기 놓침, 숫자 일치 오탐) | 작은 정적 — `.asm` 로 한 줄씩. Q100 의 "정적 대안" 과 같은 종류 |
| `SameTebFlags` 를 왜 `MmCreateTeb` 가 아니라 `PspAllocateThread` 가 따로 쓰나 | 코드는 "어디서 쓴다" 만 보여 줌 | 근거 없음. 대기열에 없는 질문이다 |
| 일꾼 스레드 2개의 TEB | 이번 범위 밖 | 덤프 **Q79** (C10) |

- 대기열 순서 (plan): **Q78d4** → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q100·Q101 은 맨 뒤.

---

## 5. 더 읽기

- run 문서: [`run54-teb-fill.md`](run54-teb-fill.md) — 요약 표와 셈 방식, 호출 지점 표, 호출 관계 트리, TEB 의 0 은 어디서, `K1~K2` 8칸 (13줄 표), `P0~P2` 13칸, 끝까지 0 112칸 (조건부 표·범위 밖 길 표), 갈림길 다섯, 간접 호출, 정정, 아직 확인 필요
- 결과서: [`_workspace/sast/48_teb-fill_result.md`](../../_workspace/sast/48_teb-fill_result.md) — 판단 기준과 셈, 1·2번 칸마다 표, 3.1 ~ 3.4 끝까지 0 가르기, 4 간접 호출, 5 줄기와 경로, 동적 질문 D1 ~ D6, Q78d4 넘길 것, 정정 1 ~ 9
- 요청서: [`_workspace/sast/48_teb-fill.md`](../../_workspace/sast/48_teb-fill.md)
- 짐 지도: [`baggage.md`](baggage.md) — 2장 · **3.3 `_TEB`** · 3장 줄임 `①TB…`·`①TS…` · 7.2 · 7.5 · **7.17**
- 흐름도: [`flow.md`](flow.md) — 기준, 2.1 `nt!PspAllocateThread` 절·`MmCreateTeb` 절, 2.3 `PspUserThreadStartup` 트리와 절, 2.4 `LdrpInitializeProcess` 트리, 3장 일꾼, 4장 run54 줄, 5장
- 진행표: `_workspace/plan.md` Q78d3 (끝) · Q78d4 · Q79 · Q100 · Q101, 결정 기록 2026-10-30
- 앞 해설:
  - [run53](run53-ethread-create-해설.md) 2.4 (지우개는 안 센다) · 2.6 (`UpdateTebApc` 쪽지) · 2.10 (훑기 한계, `+0x175 · +0x17e` 오인)
  - [run52](run52-kthread-create-해설.md) 2.6 ~ 2.10 — 네 가름, DWORD 되쓰기, 조건부, 훑기 한계, 옛 객체
  - [run50](run50-eprocess-create-해설.md) 2.4 · 2.5 — 범위 밖 길, 오탐
  - [run44](run44-baggage-snap-해설.md) 2.8 — 사진 지점
  - [run41](../02_sprint/run41-tls-lock-detail-해설.md) — TEB `+0x58` 열쇠꾸러미, `SchedulerSharedDataSlot`
  - [run15](../02_sprint/run15-user-thread-startup-해설.md) 2.8 — `PspUserThreadStartup` 출근 도장
  - [run11](../02_sprint/run11-allocate-thread-해설.md) 2.2 · 2.4 — TEB = 개인 안내판, 작업대·안내판은 언제 만드나

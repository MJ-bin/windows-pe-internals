# run139 wow64-kernel — 해설

> 이 글은 [run139 문서](run139-wow64-kernel.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q253a** — PE_emulator-AR 요청 `AR01_internals-wow64.md` 의 **C 절** "커널 WoW64 갈래 몸" (`_workspace/plan.md`, 이번에 **끝 (run139)**)
  - 원 요청: `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` C 1 ~ 10.
  - 요청 차례 **C → A → B** 의 첫째다. 디컴파일은 먼저 끝났고, 문서는 run138 (A 절, Q253b) 뒤에 썼다.
  - 결과서 판정 **분석성공** (C 1 ~ 10 전부. 못 닫은 몇 줄은 `[추측]` · `[공백]`).
  - 겸사로 원문 **B 4** (32비트 ntdll 이름 8칸) 도 닫혔다 → Q253c 에서 빠짐.
  - 남은 것이 간 곳 (4절): 동적 → **Q262**, 정적 → **Q263**, 짐 지도 보탬 → **Q264** (사용자 결정).
- 근거:
  - run 문서 [`run139-wow64-kernel.md`](run139-wow64-kernel.md) (가장 중요)
  - 요청서 [`_workspace/sast/135_wow64-kernel.md`](../../_workspace/sast/135_wow64-kernel.md) · 결과서 [`135_wow64-kernel_result.md`](../../_workspace/sast/135_wow64-kernel_result.md)
    - ntoskrnl 은 **다시 analyze 안 함**. `^이름$` export depth 0 — 요청서 8개 + 길 따라 22개.
  - flow.md 변경: `git show 335f2c1 -- runs/03_sprint/flow.md`. **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다 (넣을 자리 판단만 → Q264).
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다.** run138 과 같다.
   - x64 이미지면 `PspSelectMachineForProcess` 출력 `+2` 가 0 이라, `PspAllocateProcess +0xfad TEST BL,0x40` 에서 `'WowP'` 를 건너뛴다 `[정적]`.
   - EPROCESS `WoW64Process : (null)` 도 봤다 `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`, run106 이 맞춘 줄).
   - 그래서 이번에 나오는 짐은 **test.exe 사진에 하나도 없다.** "x86 exe 였다면 커널이 이렇게 짓는다" 를 코드로 읽은 것이다.
2. **WoW64 큰 그림은 다시 길게 안 푼다.** [run138 해설 2.1](run138-wow64-dlls-해설.md#21-큰-그림-먼저--층이-넷-길이-둘) 의 "옛날식 사무실 · 통역사 · 문" 그림을 그대로 쓴다.
3. **오프셋이 세 가지다.** 앞 해설들과 같은 규칙이다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문의 "`+0xfc4 ExAllocatePool2`" 처럼 함수 이름 **앞** 숫자 = **call 자리**, 대상 없는 숫자 = 그 함수 안 **줄**.
   - `TEB64 +0x1488` · `PEB32 +0x10` 처럼 **덩어리 이름과 붙은** 숫자 = 그 덩어리 맨 앞에서 센 **칸 자리**.
   - 콜스택으로 본 적은 없다 (정적만).
4. **`SwapContext +0x5xx` 는 조각에서 잰 거리다.** Ghidra 가 이 구간을 다른 함수 조각 (`FUN_1406b686f`) 으로 잡아서, `.asm` 의 `(= SwapContext +…)` 거리 (함수 시작에서 잰 거리) 로 적었다. WinDbg 가 같은 이름 · 거리로 보일지는 `[추측]` — 멈춤점 걸기 전에 `u nt!SwapContext+0x538 L1` 로 확인할 것 (run 문서 "디버거 메모").
5. **run 문서 비유의 "건물주" 는 이 글에서 "관리사무소" 다.** 앞 해설들에서 커널 = 관리사무소였기 때문이다. 가리키는 건 같다.

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 프로세스 = **가게**, 주소 공간 = 가게의 **빈 땅**, 스레드 = **직원**, ntdll = 가게 안 **관리실** (run10 · run11)
- `_EPROCESS` = 관리사무소의 **가게 장부** → [run50 해설](run50-eprocess-create-해설.md)
- PEB = 가게 **안내판** → [run10 해설](../02_sprint/run10-allocate-process-해설.md#peb), TEB = 직원 **개인 안내판** → [run11 해설 2.2](../02_sprint/run11-allocate-thread-해설.md#22-한-줄씩-읽는-법), `_TEB32` = 64비트 안내판 바로 옆 (`+0x2000`) 에 세우는 **32비트용 안내판** → [run54 해설 2.7](run54-teb-fill-해설.md)
- VAD = 땅의 **구획 장부** → [run10 해설](../02_sprint/run10-allocate-process-해설.md#주소-공간), 새 가게 땅에 잠깐 들어가 쓰고 나오기 (`KiStackAttachProcess`) → [run10 해설](../02_sprint/run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)
- 예약 = 빈 땅에 **말뚝**, 커밋 = **바닥 깔기**, 가드 = "여기 밟으면 알려 줘" **줄**, 유저 스택 = 직원 **작업대**, 그 크기를 정하는 목수 = `RtlCreateUserStack` → [run74 해설 2.5](run74-thread-create-해설.md)
- CONTEXT = **업무 시작 메모** → [run11 해설 2.6](../02_sprint/run11-allocate-thread-해설.md#26-유저-쪽-시작-주소는-context-에서-옮겨-적는다), `IRETQ` = 출발 메모 다섯 칸으로 **순간 이동** → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- 프로세스 매개변수 블록 = 개업 **서류** → [run32 해설 2.3](../02_sprint/run32-params-initblock-해설.md#23-새-말--프로세스-매개변수-블록), `LdrSystemDllInitBlock` = 관리실 책상 위 **업무 수첩** → [run32 해설 2.8](../02_sprint/run32-params-initblock-해설.md#28-새-말--시작-정보-블록-그리고-어디에-쓰나)
- `PsNtdllExports` = 관리사무소 **벽 주소록** → [run16 해설 2.9](../02_sprint/run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다), `PspSystemDlls` = **세트 대장** → [run48 해설 2.5](run48-system-dll-boot-해설.md#25-새-말-시스템-dll-표-pspsystemdlls)
- CPU = **계산대**, PRCB = 계산대 **장부**, `SwapContext` = 계산대 **교대** → [run75 해설 2.8](run75-thread-switch-해설.md#28-교대--kiswapcontext--swapcontext)
- 셀렉터 = **출입증** (`0x33` 본관 = 64비트, `0x23` 옛날식 사무실 = 32비트), CPU 영역 = 문 옆 **업무 메모판** → [run138 해설 2.10 · 2.11](run138-wow64-dlls-해설.md#210-문을-지나는-법--cs-0x23--0x33-a2)
- 짐 = 개업 날 아침까지 놓여 있어야 하는 **개업 준비물** → [run43 해설](run43-baggage-checklist-해설.md)
- MSR = CPU 안의 **설정 칸** → [run42 해설](../02_sprint/run42-continue-detail-해설.md), LAA = exe 헤더의 "큰 주소도 다룰 줄 안다" 표시 → [run38 해설](../02_sprint/run38-map-module-해설.md)

**이번에 더하는 비유 — "64비트 준비물 옆에 놓는 32비트 준비물"**

- run138 에서 x86 exe 가게는 **64비트 건물 안에 옛날식 사무실 (32비트) 을 하나 꾸민 가게** 였다.
- 그 사무실을 꾸민 건 통역사 (wow64.dll) 지만, **사무실 짐 대부분은 관리사무소 (커널) 가 미리 갖다 놓는다.** 이번 바퀴가 그 이야기다.
- 갖다 놓는 짐은 거의 다 **64비트 짐의 쌍둥이** 다.
  - 가게 안내판 (PEB) 옆에 **옛날식 안내판** (PEB32).
  - 개업 서류 옆에 **옛날식 서류 사본** (32비트 매개변수 블록).
  - 직원 개인 안내판 (TEB) 옆에 **옛날식 개인 안내판** (TEB32).
  - 작업대 (64비트 스택) 옆에 **옛날식 작업대** (32비트 스택).
  - 업무 시작 메모 (64비트 CONTEXT) 옆에 **옛날식 업무 시작 메모** (x86 CONTEXT, CPU 영역 안).
- 그리고 옛날식 짐은 **낮은 층 (4GB 아래)** 에만 놓인다. 옛날식 직원은 높은 층 번지를 못 읽기 때문이다 `[추측]` (일반적으로 알려진 설명 — 32비트 포인터는 4GB 까지만 가리킨다). 그 "낮은 층만" 규칙의 뿌리가 2.5 다.

### 한 줄로 먼저 — 무엇을 얻었나

- x86 exe 를 WoW64 로 정하는 곳 = **`PspSelectMachineForProcess`** → `PsWow64IsMachineSupported(0x14c)` `[정적]`. run89 의 `[추측]` 이 (절반) 닫혔다.
- 커널은 x86 프로세스를 위해 **`'WowP'` 0x10 B (`{Peb, NtdllType = 1}`) · PEB32 · 32비트 매개변수 · TEB32 (= TEB64 + `0x2000`) · 32비트 스택 · CPU 영역 · 첫 x86 문맥** 을 더 짓는다 `[정적]`.
- **4GB 상한의 뿌리 = `EPROCESS.HighestUserAddress (+0x368)`** — `MiComputeProcessUserVa +0xa8` 가 `0xffff0000` (LAA) / `0x7fff0000` 을 쓴다 `[정적]`.
- **GDT 칸 10 베이스는 `SwapContext` 가 스레드마다 TEB32 로 고친다** `[정적]`. 칸 4 바이트는 ntoskrnl 에 없다 (로더 몫 `[공백]`).
- run138 의 "CPU 영역은 커널이 짓는다 `[추측]`" → **`[정적]`** 으로 닫힘: 자리 `PspWow64SetupCpuArea`, 가리키는 칸 `PspWow64InitThread +0xae` (TEB64 `+0x1488`).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

WoW64 지도는 세 조각이다 (run138 해설 3절 그림). 이번엔 **맨 위 조각** 이다.

```
 커널이 32비트 짐을 짓는다          (Q253a, sast/135)                 ← 이번 (run139)
   'WowP' · PEB32 · 32비트 서류 · TEB32 · 32비트 스택 · CPU 영역 · GDT 칸 10 …
        ▼
 64비트 쪽 DLL 이 문까지 데려간다    (Q253b, run138)
   LdrpLoadWow64 → Wow64LdrpInitialize → BTCpuSimulate ═▶ 32비트
        ▼
 32비트 ntdll 이 받는다            (Q253c, sast/137 — 도는 중)
```

test.exe 지도의 시간 순서로 놓으면, 이번 줄들은 **이미 아는 커널 함수 안의 "x86 일 때만" 갈래** 다.

```
cmd.exe → NtCreateUserProcess
  PspAllocateProcess                                         (run10 · run50)
    +0xa8c  PspSelectMachineForProcess        ← 이번: 기계 고르기
    +0xfc9  'WowP' · +0xffb 칸 번호            ← 이번 (test.exe 는 +0xfad 에서 건너뜀)
    +0x2095 MmInitializeProcessAddressSpace … MiComputeProcessUserVa   ← 이번: 4GB 상한
    +0x2314 MmCreatePeb … MiInitializeWowPeb  ← 이번: PEB32
    +0x239c PspSetupUserProcessAddressSpace   ← 이번: 32비트 서류 · 32비트 수첩
  PspAllocateThread                                          (run11 · run54 · run74)
    64비트 스택 + CPU 영역 → 32비트 스택 → MmCreateTeb (TEB32) → PspWow64InitThread   ← 이번
  … NtResumeThread → 첫 교대
SwapContext (스레드 바꿀 때마다)                              (run75)
    GDT 칸 10 베이스 ← TEB32                   ← 이번
KiUserExceptionDispatcher 로 예외 배달                        (run81 · run82)
    32비트 코드 예외 손질                       ← 이번 (급하지 않음)
```

### 무엇이 궁금했나

에뮬레이터 쪽 계획 (run138 과 같은 갈래 AR): 커널 · 64비트 ntdll 초기화는 흉내 내고, `LdrpInitializeProcess +0x17fe` 부터는 **진짜 wow64 코드** 를 돌린다. 그러면 그 진짜 코드가 **읽는 상태를 커널이 진짜 모양대로** 지어 줘야 한다. 그래서 원문 C 절 열 질문:

1. `PspSelectMachineForProcess` — x86 을 WoW64 로 정하는 조건 · 출력.
2. `'WowP'` 0x10 B 의 칸.
3. x86 이면 칸 번호 1 인가.
4. PEB32 의 자리 · 칸.
5. 32비트 매개변수 블록, 그리고 32비트 시작 블록과의 관계.
6. TEB32 19줄 칸별 · `PspAllocateThread +0x79b ~ +0x7e3`.
7. 32비트 스택 · CPU 영역 · 첫 x86 문맥.
8. 4GB 상한.
9. GDT 칸 4 · 10.
10. 32비트 코드 예외 배달.

### 바퀴 전에 알던 것

- `[정적]` (run10) `MmCreatePeb +0x680 MiInitializeWowPeb` — 이름만.
- `[정적]` (run11 · run32 · run50 · run89) `PspAllocateProcess +0xa87 · +0xfc4 · +0xffb`, `PspSetupUserProcessAddressSpace +0x1f2 · +0x214`, `PspAllocateThread +0x9eb · +0xb7d`, `Flags.Wow64VaSpace4Gb` — 자리 · 이름만.
- `[정적]` (run48) `PspSystemDlls[1]` = `PspWowX86SystemDllData`, `PsWowX86SharedInformation` 을 "복사하는 것으로 읽힘" `[추측]`.
- `[정적]` (run54) `MmCreateTeb +0x175 · +0x17e` = `_TEB32.RealClientId`. x86 갈래 19줄은 칸별로 안 풀었다.
- `[정적]` (run16 · run29) 64비트 스레드 CS `0x33`, 32비트 `0x23`, SS `0x2b`.
- `[정적]` (run75) `SwapContext +0x545 ~ +0x55d` FS `0x53` · `UserFsBase`.
- `[정적]` (run81) `KiDispatchException +0x5d1 ~ +0x62a` — "WoW64 · 최소 프로세스" 이름만.
- `[정적]` (run89) `PspSelectMachineForProcess` 가 하는 일 `[추측]`.
- `[정적]` (run138) CPU 영역 꼴 `{+0x0 플래그, +0x2 Machine, +0x4 x86 CONTEXT}`, TEB64 `+0x1488` 이 가리킴. 그런데 wow64 두 DLL 에 이 칸을 **쓰는 줄이 없다** → "커널이 짓는다" `[추측]`.
- `[관찰]` (run106) test.exe EPROCESS `WoW64Process : (null)`.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계 (flow.md 새 절 나무를 줄인 것, ret 자리)

```
nt!PspAllocateProcess   (x86 이미지만 — test.exe 는 +0xfad 에서 건너뜀)                       [정적]
 ├─ (1) +0xa8c  PspSelectMachineForProcess     +0xf8 PsWow64IsMachineSupported(0x14c) → 출력 {0x14c, 1, 0}
 ├─ (2) +0xfc9  ExAllocatePool2('WowP', 0x10)  → +0xfcc EPROCESS.WoW64Process · +0x100b Peb = 1
 ├─ (3) +0xffb  PspWow64PickBestNtdll          → NtdllType = 1
 ├─ (4) +0x2095 MmInitializeProcessAddressSpace … MiComputeProcessUserVa  +0xa8 HighestUserAddress
 ├─ (5) +0x2314 MmCreatePeb
 │              └─ +0x680  MiInitializeWowPeb   +0x62 MiCreatePebOrTeb(1, 0x4b8) → PEB32 · 27칸
 └─ (6) +0x239c PspSetupUserProcessAddressSpace
                ├─ +0x1f7  PspWow64SetupUserProcessAddressSpace
                │          └─ +0x47  PspWow64ThunkProcessParameters   → PEB32 +0x10
                └─ +0x219  PspPrepareSystemDllInitBlock(1)   32비트 수첩
nt!PspAllocateThread    (+0x5de JNZ +0x976)
 ├─ (1) +0x9ae  PspSetupUserStack(commit 0x8000 · reserve 0x40000)  +0x158 PspWow64SetupCpuArea
 ├─ (2) +0x9eb  PspWow64SetupUserStack   +0x61 RtlpWow64CreateUserStack
 ├─ (3) +0x631  MmCreateTeb               종류 5 — TEB32 = TEB64 + 0x2000, 19줄
 └─ (4) +0xb7d  PspWow64InitThread        +0xae [TEB64+0x1488] = CPU 영역
                └─ +0x108  PspWow64InitThreadGuestx86   x86 첫 문맥
nt!SwapContext   (스레드마다)   +0x538 ~ +0x542 GDT 칸 10 베이스 ← TEB32 · +0x54a FS = 0x53
```

읽을 때 주의:

- `PspAllocateThread` 아래 줄은 **실제 도는 순서** 로 적었다 (스택 둘 → TEB → 초기화). 오프셋 순서 (`+0x631` 이 `+0x9ae` 보다 작다) 와 다르다 (run 문서 7절, 결과서 "`PspAllocateThread` 안 순서").
- `SwapContext` 의 숫자는 ret 가 아니라 **줄** 자리다.
- 대표만 남겼다. 전체는 run 문서 "정적 — 호출 지점" 표.

**같은 내용을 "쌍둥이 짐" 표로** — 64비트 짐은 test.exe 에도 있고, 32비트 짝은 x86 일 때만 놓인다.

| 64비트 짐 (test.exe 에도 있음) | 32비트 짝 (x86 일 때만) | 놓는 곳 | 자리 |
|---|---|---|---|
| PEB (`MmCreatePeb +0x146`) | **PEB32** (27칸) | `MmCreatePeb +0x67b MiInitializeWowPeb` | PEB64 와 **같은 서브 영역** |
| 64비트 매개변수 블록 (run32) | **32비트 매개변수 블록** | `PspWow64ThunkProcessParameters +0x85` | 상한 `HighestUserAddress − 1` 아래 |
| 64비트 `LdrSystemDllInitBlock` (종류 0) | **32비트 `LdrSystemDllInitBlock`** (종류 1) | `PspSetupUserProcessAddressSpace +0x214` | 32비트 ntdll 이미지 안 |
| TEB64 | **TEB32** (= TEB64 + `0x2000`, 19줄) | `MmCreateTeb` 종류 5 | 같은 서브 영역 |
| 64비트 스택 (WoW64 면 **고정 크기**) | **32비트 스택** (가드 2 쪽) | `RtlpWow64CreateUserStack` | MM 이 정함 (상한은 `[공백]`) |
| 64비트 업무 시작 메모 (CONTEXT) | **x86 CONTEXT** (CPU 영역 안) | `PspWow64InitThread` · `…Guestx86` | 64비트 스택 맨 위 |
| `PsNtdllExports` (벽 주소록) | **`PsWowX86SharedInformation`** (32비트 주소록) | 부팅 (run48) | 커널 |

커널 쪽에만 있는 것: `_EWOW64PROCESS` (0x10 B), `EPROCESS` 다섯 칸 (`Machine` · `WoW64Process` · `Flags.OverrideAddressSpace` · `Flags.Wow64VaSpace4Gb` · `HighestUserAddress`), GDT 칸 10 베이스, `FS_BASE` MSR (결과서 "칸 표 요약").

### 2.2 한 줄씩 읽는 법

**줄 1: `├─ (2) +0xfc9  ExAllocatePool2('WowP', 0x10)  → +0xfcc EPROCESS.WoW64Process`**

- 누가: `PspAllocateProcess` — 새 가게 장부 (`EPROCESS`) 를 거의 다 채우는 함수 (run10 · run50).
- 어디서: `+0xfad TEST BL,0x40` 으로 먼저 "WoW 칸 필요" 표시를 본다. 켜져 있으면 `+0xfb6 EDX = 0x10` · `+0xfbb ECX = 0x40` · `+0xfbe R8D = 'WowP'` 를 싣고 **`+0xfc4` 에서 부른다**. 콜스택엔 `+0xfc9` 로 찍힌다 `[정적]`.
- 누구를: `ExAllocatePool2` — 관리사무소 안 (커널) 메모리를 받는 공용 부품.
- 무엇을 위해: 0x10 바이트짜리 **"이 가게는 옛날식 사무실이 있음" 쪽지** 를 만들어, `+0xfcc` 에서 장부 `WoW64Process (+0x310)` 칸에 끼운다.
- 덧붙임: test.exe 는 `+0xfad` 에서 이 줄을 건너뛴다. 그래서 장부 칸이 `(null)` 이다 `[관찰]` (run106).

**줄 2: `└─ (4) +0xb7d  PspWow64InitThread   +0xae [TEB64+0x1488] = CPU 영역`**

- 누가: `PspAllocateThread` — 첫 직원 뽑기 (run11). 그 안의 WoW64 갈래.
- 어디서: `+0x661 CMP [R15+0x310],0` · `+0x669 JNZ` — 가게 장부의 `WoW64Process` 칸이 비어 있지 않을 때만. call `+0xb78`, ret `+0xb7d` `[정적]`.
- 누구를: `PspWow64InitThread(Process, InitialTeb, 문맥64, TEB64)`.
- 무엇을 위해: 그 안 `+0xa3 KiStackAttachProcess` 로 새 가게 땅에 들어가서, **`+0xae` 에서 TEB64 `+0x1488` (`TlsSlots[1]`) 에 CPU 영역 주소를 적는다** `[정적]`. run138 의 wow64 가 맨 처음 읽던 그 칸이다.
- 비유: 직원 개인 안내판의 한 칸에 "**네 옛날식 업무 메모판은 여기 걸려 있다**" 를 적어 두는 줄.

**줄 3: `nt!SwapContext ... +0x538 ~ +0x542 GDT 칸 10 베이스 ← TEB32`**

- 누가: `SwapContext` — 계산대 교대 (run75). 스레드를 바꿀 때마다 돈다.
- 어디서: `+0x538 [RCX+0x52] = AX` · `+0x53f [RCX+0x54] = AL` · `+0x542 [RCX+0x57] = AH` — 세 줄이 칸 10 베이스의 하위 32비트를 나눠 쓴다 `[정적]`.
- 누구를: 부르는 게 아니라 **직접 쓴다**. `RCX` = `KPCR.GdtBase` (`+0x531 RCX = [RBX−0x180]`).
- 무엇을 위해: 새로 계산대에 서는 직원이 WoW64 면, 32비트 코드가 `FS:[0]` 으로 **자기 TEB32** 를 보게 하려고 (2.12).

### 2.3 첫 갈림 — 기계 고르기 (C1)

**새 말 — 기계 (Machine) 값**: exe 헤더에 적힌 "이 코드는 어떤 CPU 용인가" 번호. 이번에 나오는 값은 `0x14c` = x86 (32비트), `0x8664` = x64 (run48 해설 세트 대장 표의 "머신" 칸). 비유: 가게 설계도 표지의 **"몇 비트 건물"** 표시.

**보통 x86 exe 길** — 부모 x64, 특별한 속성 없음, `.NET` 아님 `[정적]`

1. `+0x15` 이미지 기계를 읽는다 (`[CreateInfo+0x68]` = `SECTION_IMAGE_INFORMATION.Machine`).
2. `+0x54 CMP R15W,R9W` (`R9D` = `0x14c`) · `+0x58 JZ +0x11c` — x86 이면 옆으로 간다.
3. `ImageFlags & 3 == 3` (`.NET`) 이 아니면 → 요청 기계 `[+0xa8]` 가 0 → `+0x76 PsGetProcessMachine(부모)` = `0x8664`.
4. 고른 기계 = **이미지 기계 `0x14c`**. `+0xc0` 에서 부모 (`0x8664`) 와 다르니까 →
5. `+0xf3 PsWow64IsMachineSupported(0x14c)` — 참이면 `+0x103 DIL = 1`.
6. 출력 4 바이트: **`+0xd1 [out] = 0x14c` · `+0xd4 [out+2] = 1` · `+0xd8 [out+3] = 0`**.
7. 거짓이면 `+0xfc` 에서 `0xc000007b`.

**`PsWow64IsMachineSupported` 가 "참" 이려면** `[정적]`

- 시스템 DLL 종류 0 ~ 5 중에 첫 dword 가 플래그 `& 8` 이고 상위 워드 = 기계 인 칸이 있어야 한다.
- 이 빌드에선 그게 `PspWowX86SystemDllData` (첫 dword `0x014c0008`) 다.
- 그래서 → **부팅 때 세트 대장의 1번 줄 (SysWOW64 ntdll) 이 채워져 있어야 x86 이 허용된다** (run48 `PsLocateSystemDlls` 칸).
- 비유: 관리사무소 세트 대장에 "옛날식 관리실 세트" 줄이 없으면, 옛날식 사무실이 필요한 가게는 **개업 신청 자체가 반려된다** (`0xc000007b`).

**출력 세 바이트가 가는 곳** (`PspAllocateProcess` 로 돌아와서) `[정적]`

| 출력 | 가는 곳 | 줄 |
|---|---|---|
| `+0` 기계 | 장부 `Machine (+0x6ec)` | `+0xaa2` → `+0x1043` |
| `+2` "WoW 칸 필요" | 지역 플래그 `0x40` → 2.2 줄 1 의 `'WowP'` | `+0xaa9` / `+0xab3` |
| `+3` `OverrideAddressSpace` | 장부 `Flags` 비트 17 | `+0xac5` / `+0xac7` → `+0x13ef ~ +0x13f6` |

**다른 두 길**

- **x64 exe (test.exe)**: `+0x54` 에서 안 갈라지고 기계 `0x8664`, `+2` = 0, `+3` = 0 `[정적]` (sast/50 과 같음).
- **`.NET` AnyCPU (x86 이미지 + `ImageFlags & 3 == 3`)** `[정적]`:
  - `Prefer32bit` 이거나 요청 · 부모 기계가 `0x14c`/`0x1c4` 면 32비트 (WoW).
  - 아니면 `PspQueryComPlusRunUnderWoW` 가 "WoW 로" 를 말할 때만 WoW.
  - 그 밖엔 **기계 `0x8664` + `+3` = 1** — x86 표지인데 64비트로 돌린다.
  - 이것이 run89 의 `[추측]` 을 절반 닫는다 (3절).

### 2.4 `'WowP'` — 0x10 바이트 쪽지와 칸 번호 1 (C2 · C3)

**새 말 — `_EWOW64PROCESS`**: `'WowP'` 로 받은 0x10 B 의 PDB 양식 이름. 칸은 딱 둘이다 `[정적]`.

| 칸 | 값 | 쓰는 줄 |
|---|---|---|
| `+0x0 Peb` | 처음 **1** (임시) | `PspAllocateProcess +0x100b` |
| `+0x0 Peb` | 나중에 **PEB32 주소** | `MiInitializeWowPeb +0x6c ~ +0x78` |
| `+0x8 NtdllType` | `PspWow64PickBestNtdll` 결과 — x86 이면 **1** | `+0xffb` |

- `Machine` 은 이 쪽지 안에 **없다**. 장부 `+0x6ec` 에 따로 있다 `[정적]`.
- `Peb = 1` 의 `1` 은 `R13` 에서 온다 (`+0x6b5` 에서 1, 이 길에서 안 바뀜). 결과서가 눈으로 따라간 것이라 이 추적은 약간 `[추측]` 이다.
- 풀은 비페이지 (`0x40`) 다. 0 으로 채워지는 건 `ExAllocatePool2` 공개 뜻으로 읽은 것 `[추측]`.
- 비유: 관리사무소가 장부에 끼워 두는 **옛날식 사무실 쪽지**. 처음엔 "안내판 자리: (아직, 1)" 이라 적어 두고, 옛날식 안내판을 세운 뒤 진짜 주소로 고쳐 쓴다.

**칸 번호 = 1** `[정적]`

- `PspSystemDlls` (PE 바이트) = `{Native, WowX86, 0, 0, …}`. 1번 줄의 첫 dword = `0x014c0008` (플래그 `0x8` · 기계 `0x14c`).
- `PspWow64PickBestNtdll` 의 보통 반복이 **`i = 1` 에서 맞는다**. `PsWow64GetProcessNtdllType` 은 `[WoW64Process+8]` 을 읽기만 한다.
- 그래서 → SysWOW64 ntdll 이 **둘째로** 가게에 붙는다 (flow.md 2.1).
- 덤: `[CreateInfo+0x11]` 의 최상위 비트가 켜지면 플래그 `0xc` 칸만 보는 다른 반복이라 x86 은 0 이 된다. 그 비트 뜻은 `[공백]`.

### 2.5 4GB 상한의 뿌리 — `HighestUserAddress` (C8)

시간 순서로는 PEB32 보다 **먼저** 정해진다 (`PspAllocateProcess` 의 `(4)` 가 `(5) MmCreatePeb` 앞). 그리고 뒤의 짐들이 모두 이 값을 본다. 그래서 여기서 먼저 본다.

**새 말 — `HighestUserAddress`**: 가게 장부 `+0x368` 칸. "이 가게 땅은 **몇 번지까지** 쓸 수 있나" 의 끝 번지 `[정적]`. 비유: 빈 땅의 **울타리**. 울타리 너머엔 아무것도 못 놓는다.

**누가 쓰나** — `MiComputeProcessUserVa` (← `MmInitializeProcessAddressSpace +0x136/+0x13b`) `[정적]`

1. 이미지 섹션이고 `OverrideAddressSpace` 가 꺼져 있을 때만 (`+0x6c BT R8D,0x11`) 울타리를 줄인다.
2. `+0x8b` exe 헤더 LAA 비트 · `+0x91` WoW64 · 기계 `0x14c`/`0x1c4` 를 본다.
3. **LAA 면** → `+0xde ~ +0xf0` 울타리 **`0xffff0000`** + `Wow64VaSpace4Gb` 비트 켬.
4. **LAA 아니면** → `+0x137 TEST EDX,R8D` 로 그 비트가 이미 켜져 있나 본다.
   - 켜져 있으면 (`PspAllocateProcess +0x1015` 가 켰으면) 위 3 과 같은 갈래.
   - 아니면 `+0x140 ~ +0x152` 울타리 **`0x7fff0000`** (비트 안 켬).
5. `+0xa8 [EPROCESS+0x368] = RDI` — 여기서 칸에 쓴다.

- 그러니 `Wow64VaSpace4Gb` 비트를 켜는 곳은 둘 (`PspAllocateProcess +0x1015` — `Flags` 인자 비트 23 일 때, 그리고 위 `+0xf0`), **읽는 줄은 `+0x137` 하나** 다 `[정적]`. 비트 23 의 이름은 `[공백]`.
- 비교로: test.exe 장부의 이 칸은 **`0x7fffffff0000`** 이었다 `[관찰]` (run50, 사진 K1). x86 가게 울타리는 그보다 훨씬 안쪽이다.

**누가 읽나** (결과서 표 — `.asm` 줄 + objdump 훑기) `[정적]`

- `MiGetUserReservationHighestAddress +0x12` → PEB/TEB **서브 영역** 상한 (2.6)
- `MiAllocateVirtualMemoryPrepare +0x3ac · +0x517` → `NtAllocateVirtualMemory` 상한
- `MiSelectUserAddress +0x9d` (≤ 4GB 면 작은 공간 갈래) · `MiFindEmptyAddressRange +0x2c1` · `MiInsertVad +0x5c`
- `MmMapViewOfSection +0xdf · +0x198` · `NtMapViewOfSection +0x1d1 · +0x555` → 뷰 매핑
- `PspWow64ThunkProcessParameters +0x5f` → 32비트 서류 (2.7)
- 위를 넘을 때 돌려주는 상태 코드는 안 봤다 `[공백]`.
- 비유: 땅에 무엇을 놓든 (안내판 · 서류 · 건물 · 빈 땅 대여) **울타리 안쪽인지 먼저 보는 담당이 여럿** 있다. 그래서 따로 "4GB 아래로" 를 외치지 않아도 32비트 짐이 다 낮은 층에 놓인다.

**이미지 (exe · DLL 건물) 는 한 단계 더** — `MiSelectImageBase` `[정적]`

- WoW 이미지면 `+0x6c MiGetImageWowSubsystemIndex` (표 `MiWowSubsystems6432` = `{0x014c}`) → x86 이면 `ESI = 3`.
- `+0x151 CMP ESI,3` · `+0x154 JZ +0x252` → WoW 겹침표 · **천장 `0x78000000`** (`+0x26a`).
- 곧 x86 이미지가 **선호 주소에 못 앉을 때** 1.875GB 아래에서 자리를 고른다. ([run73 해설](run73-image-base-select-해설.md) 의 기준 주소 고르기와 같은 함수.)
- 겹침표의 PDB 이름 (`_MI_SECTION_WOW_STATE` 꼴 `[추측]`) 은 Q263.

### 2.6 PEB32 — 옛날식 안내판 (C4)

**언제** `[정적]`

- `MmCreatePeb` 가 **PEB64 를 다 쓴 뒤**, `+0x598 CMP [R13+0x310],0` · `+0x5a0 JNZ` (WoW64 만) → **`+0x67b MiInitializeWowPeb`** (ret `+0x680`).

**어디에** — 새 말 하나가 필요하다

**새 말 — 서브 할당 영역 (`'VadS'`)**: PEB · TEB 같은 작은 짐을 놓으려고 **2MB 짜리 구획 하나를 통째로 잡아 두고**, 그 안을 한 쪽 (페이지) 씩 잘라 주는 자리 `[정적]`.

- 구획 장부 (VAD) 에 `'VadS'` 한 줄. 기본 `0x200` 쪽 (2MB), 실패하면 반씩 줄여 `0x10` 쪽까지.
- 어느 쪽이 비었는지는 **비트표** 로 센다 (`RtlFindClearBitsAndSetEx`). 첫 자리 힌트는 랜덤 (`ExGenRandom`).
- 상한은 `MiGetUserReservationHighestAddress` = `min(0x7ffffffeffff, HighestUserAddress − 1)`.
- 비유: 빈 땅 한쪽에 **작은 짐 전용 칸막이 창고** 를 하나 세워 두고, 안내판이 필요할 때마다 빈 칸 하나씩 내주는 것.

그래서 PEB32 자리는 `[정적]`:

1. `MiInitializeWowPeb +0x5d MiCreatePebOrTeb(종류 1)` — 종류 1 크기 `0x4b8` (`MiCreatePebOrTeb +0x4d`, 쪽 올림 `0x1000`).
2. → `MiAllocateFromSubAllocatedRegion` 이 칸막이 창고의 빈 칸을 찾는다. 없으면 `MiAllocateNewSubAllocatedRegion` 으로 창고를 하나 더.
3. PEB64 · PEB32 · TEB 가 **같은 창고에서 차례로** 잘린다.
4. 창고 자체가 울타리 (`HighestUserAddress`) 안이라 → **4GB 아래**.

- PEB32 가 PEB64 **바로 다음 쪽** 인지는 "next-fit 이라 보통 그렇다" `[추측: 거리]` (Q262 4).
- 덤: run43 의 "`MmCreateTeb +0xa7` `MiAllocateFromSubAllocatedRegion` = TEB 자리 `[추측: 이름]`" 이 이번에 **`[정적]`** 으로 닫혔다. 이름 그대로 "서브 할당 영역에서 쪽 받기" 다.

**무엇을 적나 — 27칸** (`MiInitializeWowPeb +0xb8 ~ +0x1ae`, 대표만) `[정적]`

- 대부분 **`MmCreatePeb` 가 PEB64 에 쓴 값과 같은 출처** 다. 64비트 안내판을 보고 옛날식 안내판에 옮겨 적는 셈이다.

| PEB32 칸 | 값 / 출처 |
|---|---|
| `+0x8 ImageBaseAddress` | `EPROCESS.SectionBaseAddress` 하위 32비트 |
| `+0x64 NumberOfProcessors` · `+0x68 NtGlobalFlag` | 전역 `KeNumberProcessors` · `NtGlobalFlag` |
| `+0x78` · `+0x7c HeapSegment*` | 손질한 값 (`> 0x40000000` → `0x100000`, commit > reserve → `0x2000`) |
| `+0xa4 ~ +0xb0 OS*Version` | 기본 10 · 0 · 26100 · CSD · 2 — x86 갈래는 **PE32 load config 를 다시 읽어** 다시 계산 (`+0x3a9`) |
| `+0x1d4 SessionId` | `PsGetProcessSessionId` |
| `+0x228 ActiveCodePage` 등 | 서버 사일로 NLS |

- 전체 27칸은 run 문서 4 ~ 5절 표.
- **이 함수가 안 쓰는 칸**:
  - `ProcessParameters +0x10` → 2.7 에서 다른 함수가.
  - `ApiSetMap +0x38` · `SharedData +0x50` · `ActiveProcessAffinityMask +0xc0` · `ExtendedFeatureDisableMask +0x480` → run57 · run59 의 함수들이 WoW64 면 PEB32 에도 쓴다 (결과서 "다른 곳").
  - `Ldr` · `ProcessHeap` · `BeingDebugged` 등 → 못 찾음 `[공백]`.
- 끝으로 `+0x78` 에서 쪽지 (`_EWOW64PROCESS.Peb`) 의 `1` 을 **PEB32 주소로 덮는다** (2.4).

**run138 의 `Wow64Info` 와 맞춰 보면**

- run138: wow64 의 `Wow64Info` = PEB32 `+0x490` (≥ `0x24` B).
- 이번: PEB32 할당 크기 `0x4b8` 안에 든다 (`0x490 + 0x28 = 0x4b8`) — run 문서가 크기만 맞춰 본 것 `[추측: 크기만 맞춰 봄]`.
- 커널이 그 칸에 쓰는 줄은 `MiInitializeWowPeb` 에 없다 (마지막 쓰기 `+0x478`).
- 비유: 옛날식 안내판 아래 **통역사 메모 칸** 자리까지 관리사무소가 넉넉하게 땅을 잡아 둔다. 칸에 글씨를 쓰는 건 통역사다.

### 2.7 32비트 서류 사본 — 매개변수 블록과 32비트 수첩 (C5)

**순서** — `PspSetupUserProcessAddressSpace` 안 `[정적]`

```
+0x1e1  PspPrepareSystemDllInitBlock(0)        64비트 수첩 (test.exe 도 여기)
+0x1f2  PspWow64SetupUserProcessAddressSpace   ← 32비트 서류 (x86 만)
          └─ +0x42 PspWow64ThunkProcessParameters
+0x20a  PsWow64GetProcessNtdllType             = 1
+0x214  PspPrepareSystemDllInitBlock(1)        32비트 수첩
```

**32비트 서류 만들기** (`PspWow64ThunkProcessParameters`) `[정적]`

1. 크기 = `MaximumLength + EnvironmentSize − 0x188` (`+0x1f ~ +0x44`).
2. `+0x5f` 상한 `R8 = HighestUserAddress − 1` (2.5 의 울타리) 을 `ZeroBits` 자리에 넣고 → **`+0x85 ZwAllocateVirtualMemory(…, 0x3000, PAGE_READWRITE)`**. 새 가게 땅에 붙은 채로 빈 방을 잡는다.
3. 칸마다 32비트 모양으로 옮긴다:
   - 정수 칸 → 하위 32비트.
   - **문자열 열 개** → 글자를 새 블록 `+0x2c8` 뒤로 복사하고, 칸엔 32비트 포인터.
   - `RuntimeData` (CRT 핸들 상속표) → 항목 9 B 를 5 B 로 줄여서.
   - `Environment` → 맨 뒤 (`+0x6fb` · `+0x740`).
4. **`+0x761` PEB32 `+0x10 ProcessParameters` = 블록**, `+0x76c [ctx+0xe8]` = 블록.

- 입력은 `[ctx+0xd8]` = **커널이 먼저 잡아 둔 64비트 사본** 이다 (유저 사본 `[ctx+0xe0]` 아님).
- 32비트 칸 이름은 공개 32비트 모양에서 가져왔다 `[추측: 이름]` (PDB 에는 64비트만).
- 비유: run32 의 개업 서류가 지나는 길 끝에서, 관리사무소가 **옛날 양식으로 한 부 더 베껴** 옛날식 안내판 `+0x10` 칸에 꽂아 둔다. 원본은 관리사무소가 가진 사본에서 베낀다.

**32비트 수첩** (`PspPrepareSystemDllInitBlock(1)`) `[정적]`

- 32비트 ntdll 의 `LdrSystemDllInitBlock` 이다. 자리 = `PsWowX86SharedInformation +0x38` 칸이 가리키는 곳, `Size == 0x128` 검사.
- **두 블록 (서류 · 수첩) 은 서로의 주소를 안 담는다.**
- `[ctx+0xe8]` · `[ctx+0xec]` (PEB32 하위 32비트, `+0x35`) 를 누가 읽는지는 안 따라갔다 `[공백]` (Q263).

### 2.8 TEB32 — 옛날식 개인 안내판 19줄 (C6)

**갈래와 자리** `[정적]`

1. `MmCreateTeb +0x58 ~ +0x6a` — 쪽지 `Peb` 칸이 0 이 아니면 WoW.
2. `+0x2e9 CMP EDI,5` — 기계 `0x14c`/`0x1c4` 면 **종류 5** (아니면 6, `+0x2df`).
3. 종류 5 크기 `0x3038` → **4 쪽** (`0x4000`). `+0xa7 MiAllocateFromSubAllocatedRegion` — PEB 와 **같은 함수 · 같은 칸막이 창고**.
4. **`+0x14a RCX = TEB64 + 0x2000`** — 여기서부터가 TEB32.

- run79 의 `MmDeleteTeb +0xa7 0x4000` (TEB 치울 때 크기) 이 이 종류 5 크기와 맞는다.
- 비유: 직원 개인 안내판을 걸 때, x86 가게면 **두 장짜리 큰 판** 을 받아 앞장 (64비트) 과 뒷장 (32비트, `+0x2000`) 을 같이 쓴다.

**19줄 — 대표만** (`+0x156 ~ +0x1d8`, PDB `_TEB32` 칸 이름) `[정적]`

| 줄 | 칸 | 값 |
|---|---|---|
| `+0x198` | TEB32 `+0x0 NtTib.ExceptionList` | `0xffffffff` |
| `+0x1b4` · `+0x1ba` · `+0x1c0` | TEB32 `+0x4 StackBase` · `+0x8 StackLimit` · `+0xe0c DeallocationStack` | **32비트 스택** 세 칸 (2.9) |
| `+0x15a` | TEB32 `+0x18 NtTib.Self` | TEB32 주소 |
| `+0x19b` | TEB32 `+0x30 ProcessEnvironmentBlock` | **PEB32** |
| `+0x175` · `+0x17e` | TEB32 `+0x6b8` · `+0x6b4 RealClientId` | ClientId (run54 가 이미 본 두 줄) |
| `+0x19f` | TEB32 `+0xf70` (PDB 이름 `GdiBatchCount`) | **TEB64 주소** 하위 32비트 |
| `+0x1ca` | TEB64 `+0x180c WowTebOffset` | `+0x2000` |
| `+0x1d2` | TEB32 `+0xfdc WowTebOffset` | `−0x2000` |
| `+0x1d8` | TEB64 `+0x0 NtTib.ExceptionList` | **TEB32 주소** |

- 전체 19줄은 run 문서 6절 표.
- **두 장이 서로를 가리킨다** `[정적]`:
  - TEB64 `+0x0` → TEB32, TEB32 `+0xf70` → TEB64. (`+0xf70` 은 "쌍둥이 찾기 칸으로 보임" `[추측]`.)
  - `WowTebOffset` 은 앞장에 `+0x2000`, 뒷장에 `−0x2000`.
- 비유: 앞장 맨 위에 "뒷장은 여기", 뒷장에 "앞장은 여기" 를 적어 두는 것. 어느 쪽에서 봐도 짝을 찾는다.

**왜 `WowTebOffset` 이 중요한가 — 64비트 ntdll 갈림의 뿌리**

1. 근거 `[정적]` (run18): 64비트 ntdll 의 `UseWOW64 = TEB.WowTebOffset > 0` (`LdrpInitializeProcess +0x5f2`).
2. 근거 `[정적]` (이번): 그 칸에 `+0x2000` 을 쓰는 줄 = `MmCreateTeb +0x1ca`, x86 갈래 안.
3. 그래서 → run138 해설의 `+0x1766 UseWOW64 ?` 갈림에서 "≠ 0" 쪽으로 가는 **스위치를 켜는 손이 커널의 이 줄** 이다 (flow.md 새 절 "TEB32 19줄" 줄).
- test.exe 는 이 갈래를 안 타서 `UseWOW64` 0 `[관찰]` (run44 사진).

**TEB32 에 쓰는 다른 커널 줄 하나** — `SameTebFlags` `[정적]`

- `PspAllocateThread +0x66f TEST R12W,R12W` · `+0x673 JNZ +0x780` — 플래그 워드가 0 이 아닐 때만 들어간다.
- `+0x7a4 R12D = 0x8664` · `+0x7aa RAX = TEB64 + 0x2000` · `+0x7b7 CMP [R15+0x6ec],R12W`:
  - 기계 `0x8664` (ARM64 호스트의 x64 WoW) 면 `+0x7c9 [RAX+0x17ee]`.
  - **x86 이면 `+0x7db [RAX+0xfca]` = TEB32 `SameTebFlags`**.
- `+0x7a4` 줄은 flow-writer 가 `.asm` 으로 찾아 보탠 줄이다 (결과서는 `CMP …,0x8664` 로 줄여 적음 — run 문서 "flow-writer 대조").
- 그 밖 TEB32 칸 (`WOW32Reserved +0xc0` 등) 은 커널이 안 쓰고 0 으로 시작한다. `+0xc0` 은 wow64.dll 이 쓴다 (run121 · run138).

### 2.9 작업대 둘과 CPU 영역 (C7 앞쪽)

`PspAllocateThread` 의 WoW64 갈래 순서 (`+0x5d6 CMP [R15+0x310],0` · `+0x5de JNZ +0x976`) `[정적]`:

```
(1) +0x9ae  PspSetupUserStack(commit 0x8000 · reserve 0x40000)      64비트 작업대 — 크기 고정
             ├─ +0x8e   RtlCreateUserStack
             └─ +0x158  PspWow64SetupCpuArea   맨 위를 잘라 CPU 영역
(2) +0x9eb  PspWow64SetupUserStack                                   32비트 작업대 — 호출자 크기는 여기로
             └─ +0x61   RtlpWow64CreateUserStack
(3) +0x631  MmCreateTeb                                              2.8
(4) +0xb7d  PspWow64InitThread                                       2.10
```

**① 64비트 작업대는 크기가 고정이다** `[정적]`

- `+0x97e ~ +0x98a` 지역 변수에 **commit `0x8000` · reserve `0x40000`** 을 넣고 `+0x9a9` 에서 `PspSetupUserStack` 을 부른다. 호출자가 준 크기는 무시한다.
- 보통 (test.exe) 길은 `+0x5ea` 에서 호출자 크기로 부른다 (run74).
- 비유: x86 가게 직원의 64비트 작업대는 통역사 · 문이 쓰는 **뒷방 작업대** 라 늘 같은 크기로 짠다. 신청서에 적힌 크기는 옛날식 작업대 몫이다.

**② CPU 영역은 64비트 작업대 맨 위를 잘라 만든다** `[정적]`

- `PspSetupUserStack +0x153 PspWow64SetupCpuArea(&InitialTeb.StackBase, Process)` (WoW64 만).
- `PspWow64SetupCpuArea +0x54 ~ +0x83`:
  1. 기계 → 문맥 플래그 (x86 `0x10000`, 0 이면 `0xc000000d`).
  2. 길이 `L` = 레거시 + 확장.
  3. `StackBase` 를 그 길이만큼 **내린다** (정렬, `−4`, `& −0x10`).
- 그다음 `PspSetupUserStack +0xa7 ~ +0xdc` 가 랜덤 `(ExGenRandom & 0x7f) << 4` 와 `0x28` 을 빼서 **문맥64 `Rsp`** 를 만든다.
- 그래서 → 64비트 `Rsp` 는 **CPU 영역 아래** 에서 시작한다 `[정적: 흐름]`.
- 비유: 작업대 맨 위 한 칸을 칸막이로 막아 **업무 메모판 자리** 로 쓰고, 그 아래부터 작업대로 쓴다. 메모판은 작업대 위쪽 끝에 붙어 있으니, 작업대에 짐이 아무리 쌓여도 (스택은 아래로 자란다) 메모판을 덮지 않는다 `[추측]` (스택이 아래로 자란다는 건 일반적으로 알려진 설명).

**③ 32비트 작업대** (`RtlpWow64CreateUserStack`) `[정적]`

크기 정하기 — 64비트 목수 (`RtlCreateUserStack`, run74) 와 거의 같은 꼴:

1. 입력 commit · reserve 가 0 이면 exe 의 **PE32 헤더** `SizeOfStackCommit (+0x64)` · `SizeOfStackReserve (+0x60)` (`+0x8b` · `+0x8e`). PE32 가 아니면 `+0xc1 0xc000007b`.
2. commit 도 0 이면 `0x4000` (`+0x11a`).
3. commit ≥ reserve 면 reserve 를 1MB 올림 (`+0x12b`).
4. commit 쪽 올림, reserve 64KB 올림 (`+0x138` · `+0x149`).
5. PEB64 `+0x318 MinimumStackCommit` 이 더 크면 그 값 (`+0x157`).

놓기 — 말뚝, 바닥, 줄:

- **말뚝**: `+0x1aa ZwSetInformationProcess(-1, 0x29 = ProcessThreadStackAllocation, …)` — 64비트 목수와 같은 꼴. 자리는 MM 이 정한다. MM 이 울타리 (`HighestUserAddress`) 로 자르는지는 안 따라갔다 `[공백]` (4GB 아래일 것 `[추측]`, Q263).
- **쪽지 칸**: `+0x1bf InitialTeb +0x38 = base` · `+0x1c7 +0x28 = base + reserve` (**StackBase32**).
- **바닥**: `+0x1fe` 맨 위 commit 커밋 · `+0x213 +0x30 = 커밋 시작` (**StackLimit32**).
- **줄**: reserve − commit ≥ `0x2000` 이면 그 아래 `0x2000` 을 `0x104` 로 — **가드 2 쪽** (64비트는 3 쪽, run74).
- 실패면 `+0x2aa ZwFreeVirtualMemory`. 성공하면 `PspWow64SetupUserStack +0x75 ~ +0x7b` 가 입력 구조 첫 바이트 비트 2 를 켠다.
- 이 쪽지 세 칸 (`InitialTeb +0x28 · +0x30 · +0x38`) 이 2.8 표의 TEB32 `StackBase` · `StackLimit` · `DeallocationStack` 으로 들어간다.

### 2.10 옛날식 업무 시작 메모 — 첫 x86 문맥 (C7 뒤쪽)

**`PspWow64InitThread` 가 메모판을 건다** `[정적]`

1. `+0x6a/+0x71` `RSI = 문맥64.Rip` 를 `[PsNtdllExports+0x10]` (64비트 `RtlUserThreadStart`) 와 **비교** 한다.
   - 같으면 (보통) → `+0x14d ~ +0x161` `RSI = [PspWow64GetSharedInformation(종류) + 0x20]` = **32비트 `RtlUserThreadStart`**.
   - 다르면 그 `Rip` 그대로.
2. `+0x7e/+0x85` `R12 = 문맥64.Rcx` · `R13 = 문맥64.Rdx`. `+0x95` `R15 = InitialTeb +0x10` (CPU 영역 시작).
3. `+0xa3 KiStackAttachProcess` → 새 가게 땅에 들어가서:
   - **`+0xae [TEB64+0x1488] = R15`** — `_TEB.TlsSlots[1]` 이 CPU 영역을 가리킨다.
   - **`+0xb5 ~ +0xbf [R15] = 기계 << 16`** — 영역 `+0x0` 플래그 워드 **0**, `+0x2` 기계 **`0x14c`**.
   - `+0xc7` XState 기능 (KUSD `+0x3d8`) 이 남으면 플래그 `|= 0x10040`.
   - `+0xdc RtlInitializeExtendedContext2(영역+4, …)` — `+0x4` 부터 x86 CONTEXT 틀.
4. `+0xe1 CMP EBX,0x14c` — x86 이 아니면 `0xc000000d` (ARMNT 거절).
5. `+0x103 PspWow64InitThreadGuestx86` — 메모에 값 쓰기 (아래 표).
6. `+0x122 KiUnstackDetachProcess` — 나온다.

- run16 해설이 남긴 "`PspWow64InitThread +0x71` 도 `PsNtdllExports+0x10` 을 읽는다" 는 **비교** 줄이었다 (1번).

**메모에 적는 값** (`PspWow64InitThreadGuestx86`, 칸 이름은 공개 `CONTEXT32` 모양 `[추측: 이름]`) `[정적]`

| 칸 | 값 | 줄 |
|---|---|---|
| `+0xb8 Eip` | **32비트 `RtlUserThreadStart`** (또는 문맥64 `Rip`) | `+0x6c` |
| `+0xb0 Eax` | 문맥64 `Rcx` (exe 진입점) | `+0x3e ~ +0x49` |
| `+0xa4 Ebx` | 문맥64 `Rdx` (PEB — WoW64 면 32비트 값, run16) | `+0x4f ~ +0x59` |
| `+0xc4 Esp` | **`StackBase32 − (ExGenRandom & 0x1ff) × 4 − 0x10`** (스택 랜덤화 꺼짐 `MitigationFlags & 0x40` 이면 랜덤 0) | `+0x29` · `+0x42` · `+0x56` · `+0x64` |
| `+0xbc SegCs` | **`0x23`** | `+0x98` |
| `+0xc8 SegSs` · `+0x98 SegDs` · `+0x94 SegEs` · `+0x8c SegGs` | **`0x2b`** | `+0x88` · `+0x82` · `+0x78` · `+0x72` |
| `+0x90 SegFs` | **`0x53`** | `+0x8e` |
| `+0xc0 EFlags` | `0x202` | `+0x108` |
| `+0x0 ContextFlags` | `0x1003f` 를 OR | `+0xe4` |
| FPU · `ExtendedRegisters` | 0, `ControlWord 0x27f` · `TagWord 0xffff` · MXCSR `0x1f80` | (run 문서 표) |
| `Edi · Esi · Ecx · Edx · Ebp` | 안 씀 (새 스택 쪽이라 0 `[추측]`) | — |

- run138 해설 2.11 표의 칸 자리 (`Eip +0xbc` · `SegCs +0xc0` · `Esp +0xc8`) 와 숫자가 4 씩 다른 건 기준이 달라서다. run138 표는 **CPU 영역 맨 앞** 에서, 이 표는 **x86 CONTEXT 맨 앞** (영역 `+0x4`) 에서 센 것으로 읽힌다 `[추측: 두 표를 맞춰 본 것]`.
- 64비트 메모와 나란히:

| | 64비트 메모 (test.exe, run16) | 옛날식 메모 (이번) |
|---|---|---|
| 첫 자리 | 64비트 `RtlUserThreadStart` → 커널이 `LdrInitializeThunk` 로 바꿔 끼움 | 32비트 `RtlUserThreadStart` → wow64 가 바꿔 끼움 (아래) |
| 진입점 | `Rcx` | `Eax` |
| PEB | `Rdx` | `Ebx` |
| 출입증 | CS `0x33` | CS `0x23` · FS `0x53` |
| 어디 있나 | 커널 출발 메모 → `IRETQ` | CPU 영역 (메모판) — 문 (wow64cpu) 의 `IRETQ` 가 읽음 |

**중요: 커널은 64비트 메모를 건드리지 않는다** `[정적]`

- 64비트 문맥은 **읽기만** 한다 (`[RBX+0x80/0x88/0xf8]`).
- 그래서 스레드는 **64비트 모드 (CS `0x33`) 로 출발** 한다 (`KxContextToKframes +0x9a ~ +0xa7`, run29).
- 흐름: 64비트 ntdll `LdrInitializeThunk` → `UseWOW64` → `wow64!Wow64LdrpInitialize` 가 이 메모판을 읽어 32비트로 넘어간다 (run138).
- 비유: 관리사무소는 옛날식 메모를 **메모판에 걸어만 두고**, 직원은 평소처럼 본관 (64비트) 으로 출근한다. 본관 관리실이 "옛날식 손님이네" 를 알아채고 통역사를 부르면, 통역사가 그 메모판을 들고 옛날식 사무실 문을 연다.

**`Eip` 는 한 번 더 바뀐다 — 두 결과서 이어 읽기**

1. 커널이 놓은 `Eip` = 32비트 `RtlUserThreadStart` `[정적]` (이번).
2. run138: `Wow64LdrpInitialize` 가 `Eip` 를 64비트 수첩 `+0x18` 값 (wow64 이름 `Ntdll32LoaderInitRoutine`) 으로 바꾼다 `[정적]`.
3. 이번 표 B-0 + run32 복사 줄: 수첩 `+0x18` = `PsWowX86SharedInformation +0x00` 의 사본 = **32비트 `LdrInitializeThunk`** `[정적: 두 결과서 이어 읽기]`.
4. 그래서 → "커널은 `RtlUserThreadStart` 를 놓고, 다음 손이 로더 입구로 바꿔 끼운다" 는 꼴이 64비트 쪽 (run16: 커널 `RtlUserThreadStart` → `PspInitializeThunkContext` 가 `LdrInitializeThunk`) 과 같다 `[추측: 비교]`.

### 2.11 run138 의 `[추측]` 이 닫힌 곳 — CPU 영역은 커널이 짓는다

run138 해설 2.11 "누가 이 메모판을 거나" 의 4번이 `[추측]` 이었다. 이번에 어떻게 닫혔나, 단계별로.

1. run138 근거 `[정적]`: wow64 · wow64cpu 에 TEB64 `+0x1488` 과 영역 `+0x2` Machine 칸에 **쓰는 줄이 없다**. 그런데 wow64 는 맨 처음부터 이 칸을 **읽는다**. → "누군가 먼저 걸어 뒀다" → 커널 `[추측]`.
2. 이번 근거 `[정적]` — 자리: `PspSetupUserStack +0x153 PspWow64SetupCpuArea` 가 64비트 스택 맨 위를 잘라 영역을 만든다 (2.9 ②).
3. 이번 근거 `[정적]` — 가리키는 칸: `PspWow64InitThread +0xae` 가 TEB64 `+0x1488` 에 영역 주소를 쓴다.
4. 이번 근거 `[정적]` — 머리: `+0xbf` 가 영역 `+0x0` 에 `0x014c0000` (플래그 0, Machine `0x14c`).
5. 이번 근거 `[정적]` — 몸: `+0xdc` 가 `+0x4` 에 x86 CONTEXT 틀, `Guestx86` 가 값.
6. run138 꼴 `{+0x0 플래그 WORD, +0x2 Machine 0x14c} + +0x4 x86 CONTEXT` 와 **같다**.
7. 그래서 → **CPU 영역은 커널이 짓는다** `[정적]`. 플래그 처음 값도 run138 의 "처음 값 = 커널 `[추측]`" → **0** `[정적]`.

- 덤으로 run138 의 첫 진입 `IRETQ {Eip, 0x23, EFlags, Esp, 0x2b}` · FS `0x53` 이 이번 x86 메모의 `SegCs 0x23` · `SegSs 0x2b` · `SegFs 0x53` 과 맞는다.
- run138 문서 맨 위에 "고쳐 읽기 (run139)" 한 줄이 들어갔다. 틀린 줄을 고친 게 아니라 `[추측]` 이 닫힌 것이다.

### 2.12 GDT 칸 10 — 직원이 바뀔 때마다 고쳐 쓰는 출입증 칸 (C9)

**새 말 — GDT**: CPU 가 읽는 **출입증 설명표**. 셀렉터 (출입증 번호) 가 이 표의 몇째 칸을 가리키고, 칸에 적힌 내용 (베이스 · 한계 · 형식) 대로 CPU 가 그 출입증을 해석한다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).

- 셀렉터 값의 아래 세 비트를 떼면 칸 자리가 된다고 알려져 있다 `[추측]` (일반적으로 알려진 설명). 코드도 그렇게 본다: `KiDispatchException` 이 CS 를 `& 0xfff8 == 0x20` 으로 검사한다 `[정적]`.
- 이번에 나오는 칸 (run 문서 · 요청서):
  - **칸 4 (`0x20`)** ↔ 셀렉터 `0x23` — 32비트 코드.
  - 칸 5 ↔ `0x2b` · 칸 6 ↔ `0x33` — 데이터 · 64비트 코드.
  - **칸 10 (`0x50`)** ↔ 셀렉터 `0x53` — FS.
- **베이스** = 그 칸이 가리키는 시작 번지. 32비트 코드가 `FS:[0]` 이라고 쓰면 "칸 10 베이스 + 0" 을 읽는다는 뜻이다. 그래서 칸 10 베이스 = **32비트 코드가 보는 자기 TEB 자리** 다 (run 문서: "칸 10 베이스 = TEB32 (32비트 코드의 `FS:[0]`)").
- 비유: 출입증 `0x53` 을 들고 "내 안내판 (FS) 0번 칸" 을 찾으면, 설명표 10번 칸에 적힌 주소로 간다. 그 주소를 **지금 계산대에 선 직원 것** 으로 고쳐 둬야 남의 안내판을 안 읽는다.

**찾은 것 — 커널이 만지는 건 셋뿐** `[정적]`

(가) 부팅 — `entry` (= PDB `KiSystemStartup`)

- `+0x98 SGDT` · `+0x9f R8 = GDT 베이스` · `+0xda CMP [Prcb+0x24],0` (부팅 CPU 만) → `+0xe1 AX = 0x3c00` · **`+0xe5 [GDT+0x50] = 0x3c00`** (칸 10 한계 하위).
- 이어 `+0xea DS/ES = 0x2b` · `+0xf4 ~ +0xf8 FS = 0x53`.
- 다른 CPU 의 GDT 는 안 따라갔다 `[공백]` (Q263).

(나) **교대할 때마다 — `SwapContext`** (새 직원 `RSI`, PRCB `RBX`)

1. `+0x504 BT [RSI+0x74],0xa` · `+0x509 JC` — 시스템 스레드면 통째로 건너뛴다.
2. `+0x51e` `EAX = [RSI+0x678]` (**`ETHREAD.UserFsBase`** 하위 32비트). 그 앞 `+0x50f` 프로세스 포인터, `+0x516` WoW64 검사.
3. WoW64 면 `+0x526 ~ +0x52c EAX = KTHREAD.Teb (+0xf0) + 0x2000` = **TEB32**.
4. `+0x531 RCX = [RBX−0x180]` (`KPCR.GdtBase` — 계산대 장부에 적힌 "이 계산대 GDT 위치").
5. **`+0x538 [RCX+0x52] = AX` · `+0x53f [RCX+0x54] = AL` · `+0x542 [RCX+0x57] = AH`** — 칸 10 베이스 하위 32비트.
6. `+0x545 ~ +0x54a FS = 0x53` → `+0x54c ~ +0x55d` **`IA32_FS_BASE (0xc0000100) ← UserFsBase`**.
   - FS 를 다시 실으면 베이스가 서술자 값으로 덮이므로 되돌리는 것 (run 문서 풀이).
7. 이어 DS/ES/GS `0x2b`, `KERNEL_GS_BASE ← KTHREAD.Teb` (TEB64) — run75 해설 표의 "개인 안내판을 계산대 게시판에 꽂기" 줄.

- 나가는 쪽도 있다: `+0x138 CMP [이전 프로세스+0x310],0` → WoW64 면 `+0x142 ~ +0x14f RDMSR 0xc0000100 → [RDI+0x678/0x67c]` — 나가는 직원의 FS 베이스를 `UserFsBase` 에 저장.
- 정리 `[정적]`: **WoW64 스레드는 칸 10 베이스 = TEB32, `FS_BASE` MSR = `UserFsBase`** (64비트 모드에서 쓰는 값).
- 64비트 스레드도 칸 10 베이스에 `UserFsBase` 하위 32비트가 들어가지만, 셀렉터 `0x53` 을 안 쓰니 무의미 `[추측]`.
- run75 가 본 `+0x545 ~ +0x55d` 의 **바로 앞 줄** 이 이번에 보태진 것이다.
- 비유: 계산대 교대 때마다 관리사무소가 **설명표 10번 칸을 새 직원의 옛날식 안내판 주소로 고쳐 쓴다.** 계산대 하나에 GDT 가 하나라서, 직원이 바뀌면 그 칸도 바뀌어야 한다.

(다) 부팅 — 출입증 순서를 박는 한 줄 (`KiInitializeBootStructures`)

- `+0x42e EDX = 0x230010` · `+0x438 WRMSR 0xc0000081` → **`STAR = 0x0023_0010_0000_0000`**.
- `SYSRET` 이 64비트로 돌아갈 때 CS `0x23 + 0x10 = 0x33` (칸 6) · SS `0x23 + 8 = 0x2b` (칸 5), 32비트로 갈 때 CS `0x23` (칸 4).
- 이어 `CSTAR = KiSystemCall32` (KVA 섀도면 `…Shadow`), `LSTAR = KiSystemCall64`, `FMASK = 0x4700`.
- 그래서 → **칸 4 · 5 · 6 순서가 이 한 줄에 박혀 있다** `[정적]`.

**못 찾은 것 — 칸 4 의 바이트** `[정적: 훑기]` / `[공백]`

- 칸 4 바이트 · 칸 10 의 접근 바이트 · 플래그 · 한계 상위를 쓰는 줄은 **ntoskrnl 에 없다**.
- 근거: `sgdt/lgdt` 가 든 함수 7곳과 `[GDT+0x52/+0x54/+0x57]` 쓰기 (`SwapContext` 조각 하나) 를 다 훑었다 (결과서).
- 그래서 → **로더 (`winload`) 가 준 GDT 그대로** 로 읽힌다. 값은 `[공백]` (Q262 8 `dq @gdtr`, 또는 Q263 winload).
- "없음을 본 것" 은 `[정적: 훑기]`, 실제 바이트 값은 `[공백]` — 둘을 섞지 말 것.

### 2.13 32비트 코드에서 난 예외 (C10, 짧게)

- **배달 대상은 늘 64비트 `KiUserExceptionDispatcher`** `[정적]`: `+0x58c RAX = [PsNtdllExports+0x20]`, `+0x580/+0x583 SegCs = 0x33`, `+0x571/+0x578 Rsp = RDI − 0x4d0`.
  - 32비트 처리기 (`PsWowX86SharedInformation +0x8`) 는 이 함수에서 안 읽는다. 32비트로 넘기는 건 wow64 의 `Wow64PrepareForException` 이다 (run82 · run138).
  - 비유: 옛날식 사무실에서 사고가 나도, 관리사무소는 **본관 사고 창구** 로만 알린다. 옛날식 사무실로 전하는 건 통역사 몫.
- WoW64 손질 둘 `[정적]`:
  - **정렬 예외 + `EFLAGS.AC`** (`+0x74f CMP [R14],0x80000002` · `+0x75c` AC `0x40000`) → `+0x76e CLI` · `+0x76f BTR …,0x12` 로 AC 를 끄고 **예외를 삼키고 계속**.
  - **CS 가 32비트 (`SegCs & 0xfff8 == 0x20`)** 면 `+0x605` `0x80000003 → 0x4000001f`, `+0x613` `0x80000004 → 0x4000001e`, 그 뒤 `R15 &= 0xfffffff0`.
- 남은 것: `+0x35b AND RDI,-0x60001` 뜻 `[공백]`, 예외 틀이 어느 스택에 놓이나 `[공백]` (32비트 스택 쪽일 수 `[추측]`).

### 2.14 덤 — 32비트 벽 주소록 8칸 이름 (표 B-0, 원문 B 4)

**새 말 — `PsWowX86SharedInformation`**: 부팅 때 커널이 32비트 ntdll 에서 이름으로 찾아 적어 두는 **32비트용 벽 주소록** (run48). 64비트 `PsNtdllExports` 의 쌍둥이다.

| 칸 | 32비트 ntdll 이름 | 커널 안에서 읽는 곳 |
|---|---|---|
| `+0x00` | `LdrInitializeThunk` | (64비트 수첩 `+0x18` 로 복사 — 2.10 끝) |
| `+0x08` | `KiUserExceptionDispatcher` | 안 읽음 (2.13) |
| `+0x20` | **`RtlUserThreadStart`** | `PspWow64InitThread +0x15d` → x86 `Eip` |
| `+0x30` | (이름 없음 — WOW ntdll 기본 주소, run48) | |
| `+0x38` | **`LdrSystemDllInitBlock`** | `PspPrepareSystemDllInitBlock +0x2e2` (32비트 수첩) |

- 나머지: `+0x10 KiUserApcDispatcher` · `+0x18 KiUserCallbackDispatcher` · `+0x28 RtlpQueryProcessDebugInformationRemote` · `+0x40 RtlpFreezeTimeBias`. 16칸 중 `+0x48 ~ +0x78` 은 0 `[정적: PE]`.
- 결과서가 PE 바이트 (`NtdllWowX86Exports`, 8개) 를 직접 읽었다. run48 의 "복사하는 것으로 읽힘 `[추측]`" 이 닫혔다.
- **test.exe 와도 닿는 점** `[정적]`: `PspPrepareSystemDllInitBlock +0xde ~ +0xf7` 이 이 16칸을 수첩 `+0x18` 부터 통째 복사하는데, **종류 0 (64비트 수첩) 에서도** 한다 (`+0xb7 ~ +0xc3`, run32 줄 그대로). 그래서 x64 프로세스의 64비트 ntdll 수첩에도 이 표가 들어 있다.
  - 비유: 관리실 업무 수첩엔, 옛날식 사무실이 없는 가게라도 **옛날식 관리실 주소록 사본** 이 같이 꽂혀 온다.

### 2.15 비유로 처음부터 끝까지 한 번 더

1. x86 exe 가 개업 신청을 낸다. 관리사무소는 설계도 표지의 "32비트 건물" 표시를 보고, 세트 대장에 **옛날식 관리실 세트** 줄이 있는지 확인한다 (기계 고르기). 없으면 반려.
2. 가게 장부에 **"옛날식 사무실 있음" 쪽지** (`'WowP'`) 를 끼운다. 안내판 자리는 아직 "1", 관리실 세트 번호는 1.
3. 가게 땅의 **울타리** 를 낮게 친다 (`0xffff0000` 또는 `0x7fff0000`). 이 뒤로 땅에 놓는 건 전부 울타리 안이다.
4. 가게 안내판 (PEB) 을 세운 뒤, 같은 **칸막이 창고** 에서 한 칸 더 받아 **옛날식 안내판** (PEB32) 을 세우고 27칸을 옮겨 적는다. 쪽지의 "1" 을 진짜 주소로 고친다.
5. 개업 서류를 **옛날 양식으로 한 부 더** 베껴 옛날식 안내판 `+0x10` 에 꽂는다. 옛날식 관리실 업무 수첩 (32비트 `LdrSystemDllInitBlock`) 도 채운다.
6. 첫 직원을 뽑는다. 본관용 작업대 (64비트 스택) 는 **고정 크기** 로 짜고, 그 맨 위를 막아 **업무 메모판** (CPU 영역) 자리로 쓴다. 옛날식 작업대 (32비트 스택) 는 신청서 · 설계도 크기대로 따로 짠다.
7. 개인 안내판은 **두 장짜리** 로 받아 앞장 (TEB64) · 뒷장 (TEB32, `+0x2000`) 을 쓴다. 서로 "짝은 여기" 를 적고, 앞장에 `WowTebOffset = +0x2000` — 이게 나중에 관리실의 "32비트 손님이네" 스위치다.
8. 개인 안내판 `TlsSlots[1]` 에 "메모판은 여기" 를 적고, 메모판에 **옛날식 업무 시작 메모** 를 쓴다: 출발 자리 = 옛날식 `RtlUserThreadStart`, 출입증 `0x23` · `0x2b` · `0x53`.
9. 직원은 평소처럼 **본관 출입증 (`0x33`)** 으로 출근한다. 64비트 메모는 손대지 않았으니까.
10. 계산대 교대 때마다 관리사무소는 **설명표 10번 칸** 을 새 직원의 옛날식 안내판 주소로 고쳐 쓴다.
11. 그 뒤는 run138: 관리실이 통역사를 부르고, 통역사가 메모판을 들고 문을 연다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

그림은 안 바뀌었다 (그림 1 · 2 에 WoW64 노드가 없다). 바뀐 건 글이다 (run 문서: 기준 · 2.1 WoW 줄 아홉 자리 · 2.3 부팅 절 한 줄 · 3장 · 4장 · 4.1 · 5장).

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (PDB 줄 · 근거 줄) | — | run139 추가. 새 사진 읽기 없음 — test.exe 가 이 갈래를 안 탄다는 값 (run106 `[관찰]`) 과 `+0xfad` `[정적]` 을 다시 짚음 |
| 2.1 `PspAllocateProcess` | WoW64 줄은 자리 · 이름만 | **WoW64 갈래 몸** `[정적]` — 기계 고르기 · `'WowP'` · `Wow64VaSpace4Gb` · `Machine` |
| 2.1 칸 번호 | `PspWow64PickBestNtdll` 이름만 | x86 → **1** `[정적]` (`PspWowX86SystemDllData` `0x014c0008`) |
| 2.1 `MmCreatePeb` | `+0x680 MiInitializeWowPeb` 이름만 | 조건 · 같은 서브 영역 · 27칸 `[정적]` |
| 2.1 `PspSetupUserProcessAddressSpace` · `PspPrepareSystemDllInitBlock` | 이름만 | 32비트 서류 · PEB32 `+0x10` `[정적]`, 16칸 = WOW 표 `[정적: PE]` |
| 2.1 `PspAllocateThread` · `PspSetupUserStack` | `+0x9eb` · `+0xb7d` 이름만 | WoW64 순서 · 64비트 고정 크기 · `PspWow64SetupCpuArea` `[정적]` |
| 2.1 `MmCreateTeb` | `+0xa7` TEB 자리 `[추측: 이름]` (run43), x86 19줄 안 풂 | `[정적]` 서브 영역, **19줄 칸별** `[정적]` |
| 2.3 부팅 절 | `PsWowX86SharedInformation` 값 | **8칸 이름** `[정적: PE]` |
| 3장 run75 절 (`SwapContext`) | `+0x545 ~ +0x55d` FS | 바로 앞 **`+0x538 ~ +0x542` GDT 칸 10 베이스** `[정적]` |
| 3장 run81 절 (`KiDispatchException`) | WoW64 갈래 이름만 | AC 삼킴 · `0x4000001f/1e` · 늘 64비트 처리기 `[정적]` |
| 3장 run138 절 | CPU 영역 "커널이 지음" `[추측]` (Q253a) | **`[정적]`** (`PspSetupUserStack +0x153` · `PspWow64InitThread +0xae`). `Eip` 의 수첩 `+0x18` = 32비트 `LdrInitializeThunk` `[정적: 표 B-0 이어 읽기]` |
| 3장 끝 | 없음 | **새 절 "WoW64 (x86) 초기화 — 커널 쪽"** (나무 + `[정적]` 줄 + 짐 지도 판단) |
| 4장 | Q253a 열림, run89 Q166 ① 커널 기계 고르기 열림 | Q253a 닫힘, Q166 ① 대부분 닫힘, 새 줄 셋 (동적 Q262 · 정적 Q263 · PE_emulator-AR) |
| 4.1 (추측·정적으로 둔 호출 관계) | "64비트 test.exe 면 `+0xffb` 는 안 타야 함 `[추측]`" | 안 탄다 — `+0xfad` `[정적]` + `WoW64Process` null `[관찰]` |
| 5장 | — | run139 줄 둘 |

**다른 run 문서 맨 위 "고쳐 읽기 (run139)" 한 줄씩** — 틀린 줄은 없고, `[추측]` 이 닫힌 것:

- **run43**: `MiAllocateFromSubAllocatedRegion` "TEB 자리 `[추측: 이름]`" → `[정적]` (2.6).
- **run48**: `PsWowX86SharedInformation` "복사하는 것으로 읽힘 `[추측]`" → `[정적]` (2.14).
- **run89**: `PspSelectMachineForProcess` "하는 일 `[추측]`" → `[정적]` (2.3). 다만 **절반만** — 커널이 x86 이미지를 64비트로 돌리는 건 `ImageFlags & 3 == 3` 일 때뿐 `[정적]` 인데, CLR 헤더 없는 PE32 에서 그 두 비트가 꺼져 있는지 (`ImageFlags` 를 채우는 MM 줄) 는 안 봤다 `[공백]`.
- **run138**: CPU 영역 "커널이 지음 `[추측]`" → `[정적]` (2.11).

**짐 지도 (`baggage.md`) 는 안 고쳤다 — 판단만** (run 문서 마지막 절, → Q264)

- 이번 짐은 test.exe 에 **안 놓인다** `[관찰]`. 짐 지도 2장 · 3장 숫자는 run44 사진으로 **test.exe 에 실제로 놓인 칸** 을 센 것이라, 3장에 넣으면 셈이 섞인다.
- 그래도 CLAUDE.md "구현할지로 거르지 않는다" 와, PE_emulator-AR 이 이 목록을 쓴다는 점에서 **목록에는 있어야 한다**.
- 그래서 flow-writer 판단: **7.6 에 새 절 "WoW64 x86 프로세스일 때만 놓이는 짐 — 조건부 갈래"**. 3.11 `Wow64VaSpace4Gb` 줄 조건 글 ("32비트 주소 공간일 때만") 은 **"LAA (4GB) 이거나 `Flags` 인자 비트 23 일 때"** 로 고칠 감 (2.5 — LAA 아닌 x86 은 2GB 이고 비트를 안 켠다).
- 고칠지 · 언제 (짐 지도 바퀴로) 는 사용자 결정 → **Q264**.

### 큰 그림에서 어떻게 이어지나

**WoW64 지도의 세 조각 중 위 두 조각이 채워졌다.**

```
 커널이 32비트 짐을 짓는다          (Q253a, 이번 run139)              ← 여기 (채움)
   'WowP' · PEB32 · 32비트 서류/수첩 · TEB32 · 스택 둘 · CPU 영역 · GDT 칸 10 · 울타리
        ▼   (wow64 가 읽는 칸 = 커널이 쓰는 칸 — 이번에 짝이 맞음)
 64비트 쪽 DLL 이 문까지 데려간다    (Q253b, run138)                    (채움)
        ▼
 32비트 ntdll 이 받는다            (Q253c, sast/137 — 도는 중)
```

- **run138 과 맞물리는 곳** (run 문서 "앞 run 과 이어 읽기", 모두 맞음):
  - CPU 영역 꼴 · 처음 플래그 0 · 출입증 값 (2.11).
  - wow64 의 `Eip` 바꿔 끼우기 대상 = 32비트 `LdrInitializeThunk` (2.10 끝). run138 해설 2.8 이 `[추측]` 으로 남겼던 "`+0x18` 값 = 32비트 `LdrInitializeThunk`" 가 이번에 **`[정적: 두 결과서 이어 읽기]`** 가 되었다. 32비트 ntdll 본문 쪽 확인은 여전히 Q253c.
  - `Wow64Info` 자리가 PEB32 할당 안에 든다 (2.6, `[추측: 크기만]`).
- **test.exe 지도와 이어지는 곳**: 이번 줄들은 모두 test.exe 가 **이미 지나간 함수 안의 옆 갈래** 다. 그래서 test.exe 쪽 줄 (run10 · run11 · run16 · run29 · run32 · run48 · run54 · run75 · run79 · run81) 과 자리가 다 맞고, 고친 줄이 없다.
  - test.exe 에 직접 닿는 것은 하나 — 64비트 수첩 `+0x18` 에도 WOW 16칸이 복사된다 (2.14).

**에뮬레이터 (PE_emulator-AR) 에게 뜻하는 것** (run 문서 표 — `../PE_emulator-AR` 코드는 안 읽음, 고칠지는 사용자 결정)

- **AR02**: `_CheckX64Only` 풀고 2.3 판정 흉내 → `Machine` · `'WowP'` 여부 · `OverrideAddressSpace`. 장부에 `WoW64Process` 0x10 B **새로**. `PspSystemDlls[1]` 은 진짜와 **같다 (맞음)**. `PsWowX86SharedInformation` 8칸을 표 B-0 이름으로 — **x64 길도** 수첩 `+0x18` 에 복사되니 필요.
- **AR03**: `MiInitializeWowPeb` **새로** (27칸 · 쪽지 갱신 · PE32 load config 다시 읽기 · 힙 값 손질). 서브 영역 흉내 (2MB `'VadS'` · next-fit · 첫 힌트 랜덤 · 상한 `HighestUserAddress`) 를 PEB64 · PEB32 · TEB 가 함께 쓰면 진짜 꼴. 32비트 서류 · 32비트 수첩 **새로**.
- **AR04**: `MmCreateTeb` WoW 갈래 (4 쪽 · `+0x2000` · 19줄) + `SameTebFlags +0xfca`, 스택 둘 (64비트 고정 크기) · CPU 영역 자르기 · `TlsSlots[1]` · x86 CONTEXT 초기값 **새로**.
- **AR05**: GDT **칸 4 새로** (진짜 바이트는 `[공백]`, VM 전까지 표준 32비트 DPL3 코드 `0x00cffb000000ffff` `[추측]`), 칸 10 한계 `0x3c00`, **베이스는 스레드 바꿀 때마다 TEB32** (스케줄러에 한 줄) + `FS 0x53` + `FS_BASE ← UserFsBase` + 나가는 스레드 저장. STAR 순서는 지금 `Syscall.cpp` 와 맞음.
- **AR07 이후**: `KiDispatchException` WoW64 갈래, 울타리 상한, `MiSelectImageBase` 천장 `0x78000000`.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 울타리 값이 정말 `0xffff0000` / `0x7fff0000` 인가, `Wow64VaSpace4Gb` 비트 | 실행 때 값 (LAA 켠 · 끈 x86 시험 exe 필요) | **Q262 1** (동적): `nt!MiComputeProcessUserVa+0xaf` |
| 기계 고르기 출력 · `'WowP'` 실제 값 · `NtdllType` 1 | 실행 때 | Q262 2 · 3 |
| **PEB32 가 PEB64 바로 옆인가** `[추측: 거리]`, `'VadS'` 2MB | next-fit 이라 사이 할당에 따라 다름 | Q262 4 (`!vad`, `dt nt!_PEB32`) |
| 32비트 서류 블록 크기 · PEB32 `+0x10` 실제 값 | 실행 때 | Q262 5 |
| TEB32 19칸 실제 값, 첫 스레드 · 둘째 스레드 비교 | 실행 때 | Q262 6 |
| 32비트 스택 자리 · x86 CONTEXT 전체 · 64비트 `Rsp` 가 CPU 영역 아래인지 | 실행 때 | Q262 7 |
| **GDT 칸 4 · 칸 10 나머지 바이트** `[공백]` | ntoskrnl 에 쓰는 줄이 없음 (로더 몫) | **Q262 8** (`dq @gdtr+0x20` · `+0x50`) 또는 **Q263** (winload 정적) |
| `SwapContext` 에서 WoW64 스레드 칸 10 = TEB32 인가, WinDbg 가 `SwapContext+0x538` 로 보이나 `[추측]` | 조각 거리라 표시 이름 불확실 | Q262 8 (먼저 `u` 로 명령 확인) |
| 32비트 코드 예외 실제 흐름, 예외 틀 스택 `[공백]`, `+0x35b` `0x60001` 뜻 `[공백]` | 실행 때 / 안 따라감 | Q262 9 · Q263 |
| `PsWowX86SharedInformation` · `PspSystemDlls` 실제 값 | 이름만 PE 로 봄 | Q262 10 (Q42 와 겹침) |
| `LdrpInitializeProcess +0x17fe` 가 돌아오나 | run138 과 같은 줄 | Q262 11 (= Q260 2) |
| PEB32 를 쓰는 나머지 곳 (`PspGetStandardHandleList +0x12` 등), PEB32 `+0x490` `Wow64Info` 쓰는 커널 줄 | 이번엔 `MiInitializeWowPeb` 만 | **Q263** (정적) |
| 32비트 스택 자리를 MM 이 어떻게 고르나 (`ProcessThreadStackAllocation` MM 쪽) `[공백]` | MM 본문 안 따라감 | Q263 |
| `MiSelectImageBase` WoW 겹침표 PDB 이름 · `MiObtainRelocationBits` | 이번엔 갈래 자리만 | Q263 |
| 다른 CPU 의 GDT (`KeStartAllProcessors` · `KiInitializeProcessor`) `[공백]` | 부팅 CPU 만 봄 | Q263 |
| `[ctx+0xe8]` · `[ctx+0xec]` 를 읽는 곳 `[공백]`, `Flags` 비트 23 이름 `[공백]`, `PS_ATTRIBUTE` 번호 | 안 따라감 | Q263 |
| CLR 헤더 없는 PE32 의 `ImageFlags & 3` (run89 절반) `[공백]` | MM 쪽 줄 안 봄 | run 문서 "정정" — 4장 Q166 |
| 그 밖 `[공백]`: `[CreateInfo+0x11]` 비트 7 뜻, `CriticalSectionTimeout` 전역 (rva `0xe2db90`) 이름, 울타리 위를 막는 상태 코드, wow64cpu 가 4GB 아래 오는 까닭 (이번은 x86 이미지 갈래만) | 범위 밖 | run 문서 "아직 확인 필요" (wow64cpu 는 Q261 ④ 그대로) |
| **짐 지도에 넣을지 · 언제** | 사용자 결정 | **Q264** |

- 동적 11개 (Q262) 는 **x86 시험 exe (LAA 켠 것 · 끈 것)** 가 있어야 한다. 사용자 결정이고, Q253d · Q260 VM 과 합쳤다. 커널 멈춤점은 그 프로세스 조건을 붙인다.

---

## 5. 더 읽기

- run 문서: [`run139-wow64-kernel.md`](run139-wow64-kernel.md) — 호출 지점 표 전체, PEB32 27칸 표, TEB32 19줄 표, x86 CONTEXT 표, GDT (가)(나)(다), 표 B-0, 앞 run 과 이어 읽기, flow-writer 대조, 짐 지도 판단, PE_emulator-AR 표
- 결과서: [`_workspace/sast/135_wow64-kernel_result.md`](../../_workspace/sast/135_wow64-kernel_result.md) — "먼저 읽는 한 줄 답", "칸 표 요약 (baggage 꼴)", 동적 11개 멈춤점, 다음 요청 후보
- 요청서: [`_workspace/sast/135_wow64-kernel.md`](../../_workspace/sast/135_wow64-kernel.md)
- flow.md: [3장 끝 "WoW64 (x86) 초기화 — 커널 쪽"](flow.md), 2.1 각 절의 run139 줄, 3장 run75 · run81 · run138 절의 run139 줄, 4장 run139 줄
- 앞 해설: [run138 해설](run138-wow64-dlls-해설.md) (WoW64 큰 그림 · CPU 영역 · 출입증), [run10 해설](../02_sprint/run10-allocate-process-해설.md) (PEB · 땅 · 남의 땅), [run54 해설](run54-teb-fill-해설.md) (TEB · `_TEB32`), [run74 해설](run74-thread-create-해설.md) (작업대 · 말뚝 · 바닥 · 줄), [run75 해설](run75-thread-switch-해설.md) (`SwapContext` 교대), [run32 해설](../02_sprint/run32-params-initblock-해설.md) (서류 · 수첩), [run48 해설](run48-system-dll-boot-해설.md) (세트 대장 · 벽 주소록), [run16 해설](../02_sprint/run16-thunk-context-해설.md) (출발 메모 바꿔 끼우기), [run43 해설](run43-baggage-checklist-해설.md) (짐 지도 · `MmCreateTeb`), [run89 해설](run89-dotnet-start-해설.md) (`.NET` 기계 고르기)
- 원 요청: `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` C 절

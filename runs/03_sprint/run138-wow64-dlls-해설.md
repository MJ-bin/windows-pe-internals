# run138 wow64-dlls — 해설

> 이 글은 [run138 문서](run138-wow64-dlls.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q253b** — PE_emulator-AR 요청 `AR01_internals-wow64.md` 의 **A 절** "wow64 DLL 넷" (`_workspace/plan.md`, 이번에 **끝 (run138)**)
  - 원 요청: `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` (갈래 AR — "x86 exe 를 진짜 WoW64 로").
  - 요청 차례는 **C → A → B** 다. 이번이 둘째 A.
    - C = **Q253a** (커널 쪽, sast/135) — 디컴파일은 끝났고 문서가 다음 차례.
    - B = **Q253c** (32비트 ntdll 쪽, sast/137) — 아직 도는 중.
  - 남은 것이 간 곳 (4절): 동적 → **Q260**, 정적 → **Q261**.
  - 결과서 판정 **분석성공** (A 1 ~ 5 전부. 못 닫은 몇 줄은 `[추측]` · `[공백]`).
- 근거:
  - run 문서 [`run138-wow64-dlls.md`](run138-wow64-dlls.md) (가장 중요)
  - 요청서 [`_workspace/sast/136_wow64-dlls.md`](../../_workspace/sast/136_wow64-dlls.md) · 결과서 [`136_wow64-dlls_result.md`](../../_workspace/sast/136_wow64-dlls_result.md)
    - **처음 analyze 둘**: wow64cpu.dll (51,128 B, 함수 29) · wow64.dll (356,936 B, 함수 1142).
    - wow64win · wow64base · wow64con 은 PDB 만 받았다 (import 표 · 이름표로 닫힘).
    - Ghidra 가 함수로 못 읽은 wow64cpu 구간은 objdump · ndisasm 파일로 읽었다.
  - flow.md 변경: `git diff --cached -- runs/03_sprint/flow.md` (이번 바퀴, 아직 커밋 전). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 x64 다. 64비트 ntdll 의 갈림 값 `UseWOW64` 가 0 이었다 `[관찰]` (run44 사진, flow.md 2.4 "갈림 값").
   - EPROCESS `WoW64Process : (null)` 도 봤다 `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`, run106 이 맞춘 줄).
   - 그래서 이번 바퀴는 test.exe 초기화 **밖** 이야기다. 에뮬레이터 쪽 (PE_emulator-AR) 이 "x86 exe 도 진짜 코드로 돌리고 싶다" 고 해서 읽었다.
2. **WoW64 를 본격적으로 들여다보는 건 이번이 처음이다.**
   - 지금까지는 "64비트 Windows 에서 32비트 프로그램을 돌리는 장치" 라는 **한 줄 이름**으로만 지나갔다 ([run10 해설 3.10](../02_sprint/run10-allocate-process-해설.md#310-ntdll-쪽-조금-더), [run17 해설 2.9](../02_sprint/run17-ldr-init-entry-해설.md#29-나중-스레드-길--짧게)).
   - 그래서 2.1 에서 **큰 그림부터** 천천히 간다.
3. **오프셋이 세 가지다.** 앞 해설들과 같은 규칙이다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문의 "`+0xa8 LdrpLoadDll`" 처럼 부르는 함수 이름 **앞** 숫자 = **call 자리**, 대상 없는 숫자 = 그 함수 안 **줄**.
   - `TEB64 +0x1488` · `CPU 영역 +0x2` 처럼 **덩어리 이름과 붙은** 숫자 = 그 덩어리 맨 앞에서 센 **칸 자리**.
   - 콜스택으로 본 적은 없다 (정적만).
4. **"RVA `0x1a38`" 처럼 함수 이름 없이 나오는 wow64cpu 주소가 있다.**
   - Ghidra 가 함수로 못 잡은 구간이라 함수 이름 + 오프셋 대신 **모듈 맨 앞에서 센 자리** 로만 적었다 (결과서 "알아 둘 것").
5. **run 문서의 비유에는 `[추측: 비유]` 가 붙어 있다.** 이 글에서는 비유에 태그를 안 붙인다 (설명일 뿐이라). 사실 줄의 태그만 run 문서 그대로 옮긴다.

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실** (run10 · run19)
- PEB = 가게 **안내판**, TEB = 직원 **개인 안내판** (run10 · run11). `_TEB32` = **32비트 안내판** (64비트 TEB 뒤 `+0x2000`) → [run54 해설 2.7](run54-teb-fill-해설.md)
- CONTEXT = **업무 시작 메모**, 유저 스택에 맡긴 원래 메모 = **사물함** (run11 · run16). `LdrpInitializeProcess` 가 `[RSP+0xe0]` 에 쥔 것 = 그 사물함 메모 → [run19 해설 2.2](../02_sprint/run19-proc-init-modules-해설.md)
- `LdrpProcessInitialized` = 문 앞 **칠판** (0 → 1 → 2), `LdrpInitCompleteEvent` = **종**. `LdrpInitializationComplete` = "칠판에 2 쓰고 종 치기" → [run17 해설 2.6 · 2.8](../02_sprint/run17-ldr-init-entry-해설.md#26-lock-cmpxchg--첫-스레드-를-가리는-한-줄)
- `LdrpInitializeProcess` 뒤쪽 = **이삿짐 들이고 전원 켜기**. WOW64 갈림 = "32비트 손님이면 **다른 통로**로 안내" → [run19 해설 2.5 · 2.10](../02_sprint/run19-proc-init-modules-해설.md#210-간접-호출-4곳--칸에서-주소를-꺼내-부르기)
- 시스템 콜 = **신청 창구**, 시스템 콜 번호 = **번호표**, 서비스 표 = 창구 벽의 **번호표 안내판** → [run29 해설 2.4](../02_sprint/run29-continue-start-해설.md#24-창구까지--ntdll-스텁과-번호-0x43)
- `IRETQ` = 출발 메모의 다섯 칸 (`RIP, CS, RFLAGS, RSP, SS`) 을 한꺼번에 꺼내 **순간 이동** → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- `LdrSystemDllInitBlock` = 관리실 책상 위 **업무 수첩** (관리사무소가 들어와 채워 두고 감) → [run32 해설 2.8](../02_sprint/run32-params-initblock-해설.md#28-새-말--시작-정보-블록-그리고-어디에-쓰나)
- import 목록 = 건물의 **"기대는 건물 목록"** → [run22 해설](../02_sprint/run22-map-snap-dep-해설.md)
- 썽크 = 받은 부탁을 그대로 넘기는 **얇은 다리** → [run136 해설 2.3](run136-winsta-name-해설.md)
- `KUSER_SHARED_DATA` (KUSD) = 커널이 모든 프로세스에 같은 주소로 보여 주는 **공용 페이지**, `0x7ffe0308` (`+0x308`) = `SYSCALL` / `INT 0x2e` 를 고르는 칸 → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md)
- `LdrpLoadWow64 +0x98` 의 `0x800` = "갖다 놓기 창구를 `Ex` 쪽으로 돌리는" **지시서 표시** → [run106 해설 2.6](run106-snap-map-small-해설.md)

**이번에 더하는 비유** (run 문서 "결과" 비유를 가게 그림으로 옮긴 것 — 가리키는 건 같다):

- x86 exe 를 열면, **64비트로 지은 가게 건물 안에 옛날식 사무실 (32비트) 을 하나 꾸민다.**
  - 옛날식 사무실 직원은 **옛날 말 (32비트)** 만 한다.
  - 그런데 관리실 (64비트 ntdll) 과 관리사무소 (커널) 의 신청 창구는 **새 말 (64비트)** 만 받는다.
- 그래서 사이에 둘을 둔다.
  - **wow64.dll = 통역사.** 옛날 말 신청서를 새 말 신청서로 바꿔 창구에 낸다.
  - **wow64cpu.dll = 사무실 문.** 옛날식 사무실과 본관 사이를 오가는 유일한 문이다.
  - 통역사는 **조수 셋** (wow64base · wow64win · wow64con) 을 데리고 온다.
- 옛날식 사무실 안에는 **자기 관리실** (32비트 ntdll, `SysWOW64\ntdll.dll`) 이 따로 있다.

### 한 줄로 먼저 — 무엇을 얻었나

- 64비트 ntdll 은 **통역사 한 명만** 부른다: `LdrpLoadWow64` 가 `…\System32\wow64.dll` 하나를 올리고, 이름 **6개** 를 찾아 ntdll 전역 6칸에 적는다 `[정적]`. 조수 셋은 import 로 따라온다.
- 이 6칸 중 첫 칸 `g_LdrpWow64LdrpInitialize` 의 주인 = **`wow64!Wow64LdrpInitialize`** `[정적]`. run17 · run19 가 남긴 `[추측: wow64 쪽]` 이 닫혔다.
- **`Wow64LdrpInitialize` 는 돌아오지 않는다** `[정적]`. 사무실을 꾸민 뒤 직원을 문 안으로 들여보내고 (`BTCpuSimulate`), 그 문 앞의 **끝없는 고리**에 머문다. 예외는 딱 하나 (2.9).
- 문 (wow64cpu) 은 **`IRETQ` · `JMPF` (CS `0x23`)** 로 32비트에 들어가고, **`JMPF` (CS `0x33`)** 로 나온다 `[정적]`. `RETF` 는 없다.
- 직원이 신청할 일이 생기면 문으로 나와 **통역사 창구** `Wow64SystemServiceEx` 에 내고, 번호를 보고 **안내판 4장** 중 하나에서 통역 담당 (`whNt*`) 을 찾아 64비트 `Nt*` 로 바꿔 낸다 `[정적]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

test.exe 지도 위에서 이번 구간은 **갈라지는 옆길**이다.

```
cmd.exe → CreateProcessInternalW → NtCreateUserProcess … NtResumeThread      (run9 ~ run14)

 새 프로세스 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → … ═▶ IRETQ                      (run15 · run16)
   (x86 exe 면 커널이 32비트 짐도 더 짓는다 — PEB32 · TEB32 · CPU 영역 …)   (Q253a, sast/135)

 유저, 64비트 ntdll
  LdrInitializeThunk → … → _LdrpInitialize +0x16c LdrpInitializeProcess      (run17 ~ run19)
     +0x1766 UseWOW64 ?
       ├─ 0 (test.exe)  → +0x1806 네이티브 길 → … → NtContinue → main         (run19 ~ run29)
       └─ ≠ 0 (x86 exe) → +0x17c6 LdrpLoadWow64                               ← 이번 (run138)
                          +0x17fe ⇢ wow64!Wow64LdrpInitialize                  ← 이번
                                     ⇢ wow64cpu ═▶ 32비트 코드                 ← 이번 (문까지)

 유저, 32비트 ntdll (SysWOW64\ntdll.dll)
  LoaderInitRoutine → …  → (32비트 쪽 NtContinue 은퇴?)                       (Q253c, sast/137)
```

- run19 해설에서 `+0x1766` 갈림은 "32비트 손님이면 **다른 통로**로 안내" 로 한 줄만 보고 지나갔다.
- 이번엔 그 **다른 통로 안으로 들어가** 본다. 단, 32비트 코드가 시작되는 **문 앞까지만**. 문 안 (32비트 ntdll) 은 Q253c 몫이다.

### 무엇이 궁금했나

에뮬레이터 쪽 계획은 이렇다 (요청서): 64비트 ntdll 초기화는 흉내 내다가, **`LdrpInitializeProcess +0x17fe` 부터는 진짜 코드를 돌린다.** 그러려면 그 진짜 코드가 무엇을 읽고, 어디로 가고, 돌아오는지 알아야 한다. 원문 A 절 다섯 질문:

1. `LdrpLoadWow64` — 무슨 DLL 을 어떻게 올리고, 이름 몇 개를 찾아 어디에 쓰나. 실패하면?
2. wow64cpu — 64 ↔ 32 전환은 무슨 명령 · 무슨 셀렉터로? 32비트 문맥 (CPU 영역) 은 어디에? `Wow64Transition` 은 누가 쓰나?
3. wow64 — `g_LdrpWow64LdrpInitialize` 의 주인과 차례. CPU dll 고르기. 32비트 `LdrInitializeThunk` 로 어떻게 가나. **돌아오나?**
4. syscall 다리 — 32비트 `Nt*` 부탁이 들어오는 입구 · 표 모양 · 실패 상태.
5. wow64base · wow64con — 올라오나, 누가 올리나.

### 바퀴 전에 알던 것

- `[정적]` (run18) `UseWOW64 = TEB.WowTebOffset > 0` (`LdrpInitializeProcess +0x5f2`).
- `[정적]` (run19) `+0x17c1 / +0x17c6 LdrpLoadWow64` → `LdrInitState = 3` → `+0x17fc / +0x17fe CALL g_LdrpWow64LdrpInitialize(CONTEXT)` → `+0x1801 JMP +0x1d1b`. 대상 이름은 `[추측: wow64 쪽 시작 함수]`.
- `[정적]` (run17) 나중 스레드도 `_LdrpInitialize +0x9a` 에서 같은 칸을 부른다. 그 칸 주소를 넘기는 곳은 `LdrpLoadWow64 +0xe8`.
- `[정적]` (run106) `LdrpLoadWow64 +0x98` = 지시서 표시 `0x800` 의 뿌리.
- `[정적]` (run82) `KiUserExceptionDispatcher +0x1` 이 `Wow64PrepareForException` (rva `0x1e70e0`) 을 본다. 64비트 프로세스에선 0. 그 칸을 가리키는 표 칸 `0x173848`.
- `[정적]` (run121) wow64.dll 의 `[+0xc0]` · `[+0xc4]` 쓰기는 **32비트 안내판 (TEB32) 칸**이다.
- `[정적]` (run48) 부팅 때 32비트 ntdll 기준 주소가 `PsWowX86SharedInformation +0x30` 에 놓인다.
- `[공백]` wow64 DLL 넷은 analyze 한 적이 없었다 (`ms/sym` 에 `wow64cpu.dll.pdb` 하나만).

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 층이 넷, 길이 둘

**새 말 — WoW64**

- 64비트 Windows 에서 32비트 (x86) 프로그램을 돌리는 **통역 층**. 이름은 "Windows 32-bit on Windows 64-bit" 의 줄임으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이번 바퀴로 그 층의 **몸** 이 네 DLL 이라는 게 코드로 보였다: wow64.dll · wow64cpu.dll · (조수) wow64base · wow64win · wow64con `[정적]`.

**층 그림** — 위가 32비트, 아래가 커널

```
 ┌ 옛날식 사무실 (32비트) ─────────────────────────────┐
 │  x86 exe · 32비트 DLL · 32비트 ntdll (SysWOW64)       │  Q253c
 │     Nt* 스텁 → (Wow64Transition 이 가리키는 곳?)       │  어느 쪽을 부르나는 Q253c
 └───────────────▲──────────────────┬───────────────────┘
       IRETQ / JMPF (CS 0x23)       │ JMPF (CS 0x33)
 ┌ 문 (wow64cpu.dll) ──────────────┴───────────────────┐
 │  RunSimulatedCode  ◀──고리──  CpupReturnFromSimulatedCode │  [정적]
 │                                 ├─ 터보 썽크 → SYSCALL 바로 │
 │                                 └─ ServiceNoTurbo           │
 └─────────────────────────────────┬───────────────────┘
 ┌ 통역사 (wow64.dll) ─────────────▼───────────────────┐
 │  Wow64SystemServiceEx → 안내판 4장 → whNt*            │  [정적]
 └─────────────────────────────────┬───────────────────┘
 ┌ 관리실 (64비트 ntdll) ──────────▼───────────────────┐
 │  Nt* → SYSCALL                                        │
 └─────────────────────────────────┬───────────────────┘
                                   ▼  관리사무소 (커널)
```

**길 하나 — 처음 들어가는 길 (위에서 아래가 아니라, 아래에서 위로)**

1. 64비트 관리실 (`LdrpInitializeProcess`) 이 통역사를 데려온다 (`LdrpLoadWow64`) → 2.4
2. 통역사 (`Wow64LdrpInitialize`) 가 사무실을 꾸민다: 문 (wow64cpu) 을 고르고 달고, 32비트 직원의 첫 업무 메모를 쓴다 → 2.6 · 2.7 · 2.8
3. 문 (`BTCpuSimulate` → `RunSimulatedCode`) 이 **`IRETQ`** 로 직원을 32비트 사무실 안으로 들여보낸다 → 2.10
4. 그 뒤로 통역사는 **관리실로 돌아가지 않는다** → 2.9

**길 둘 — 32비트 직원이 신청할 때 (위에서 아래로, 그리고 다시 위로)**

1. 32비트 코드가 문으로 간다: `JMPF` (CS `0x33`) → `CpupReturnFromSimulatedCode` (RVA `0x1a38`) `[정적]`
2. 문이 번호표의 위쪽 비트를 보고, 보통은 통역사 창구로: `ServiceNoTurbo` → `Wow64SystemServiceEx` `[정적]`
3. 통역사가 번호를 보고 안내판에서 `whNt*` 를 찾아 부르고, `whNt*` 는 64비트 ntdll `Nt*` → `SYSCALL` `[정적]`
4. 답이 오면 문이 다시 32비트로: `JMP 0x194f` (= `RunSimulatedCode +0x2f`) `[정적]`

- 이 4번이 **돌아오지 않는 이유와 같은 고리**다. 문은 나온 사람을 늘 다시 들여보낸다 (2.9).

### 2.2 한눈에 — 이번에 읽은 길 (run 문서 "호출 관계" 를 줄인 것, ret 자리)

```
ntdll!LdrpInitializeProcess   (UseWOW64 ≠ 0 일 때만 — test.exe 는 +0x176d JZ 로 건너뜀)        [정적]
 ├─ +0x17c6  LdrpLoadWow64(…\System32\)
 │            ├─ +0xad  LdrpLoadDll(…\System32\wow64.dll, 0x800)   ── import 로 조수 셋 같이
 │            ├─ +0xf6  LdrGetProcedureAddress ×6  → ntdll .mrdata 6칸
 │            └─ +0x150 LdrpDereferenceModule
 └─ +0x17fe  ⇢ wow64!Wow64LdrpInitialize(CONTEXT)     보통 돌아오지 않음
              ├─ +0x142 ProcessInit  (처음만)
              │          ├─ +0x1b0 CpuLoadBinaryTranslator → NtOpenKey(…\Wow64\x86) (없으면 wow64cpu.dll) · LdrLoadDll
              │          └─ +0x1c9 ⇢ wow64cpu!BTCpuProcessInit
              ├─ +0x14b ntdll!LdrProcessInitializationComplete   (처음만 — 칠판 2 · 종)
              ├─ +0x25d CpuNotifyMapViewOfSection(32비트 ntdll) → … +0xb2 [Wow64Transition] ← 문 주소
              ├─ +0xcb  Run64IfContextIs64 ── 참 → +0x297 RET (돌아오는 유일한 길)
              ├─ +0xf9  ThreadInit → TEB32 +0xc0 ← 문 주소
              ├─ +0x116 Wow64SetupInitialCall   Eip ← LdrSystemDllInitBlock +0x18 값
              └─ +0x122 RunCpuSimulation ⇢ wow64cpu!BTCpuSimulate → RunSimulatedCode  (고리)
                          ├─ IRETQ (첫 진입) / JMPF [R14]  CS 0x23  ──▶ 32비트 ntdll (Q253c)
                          └─ ◀── JMPF CS 0x33 → CpupReturnFromSimulatedCode
                                   └─ ServiceNoTurbo 0x1a79 ⇢ wow64!Wow64SystemServiceEx
                                             └─ +0x19b ⇢ whNt* → ntdll!Nt* ⇒ syscall
```

읽을 때 주의:

- `⇢` 는 **간접 호출 · import 호출** (칸에서 주소를 꺼내 부르기), `⇒` 는 syscall → 커널 (run 문서 범례).
- `Wow64LdrpInitialize` 아래 줄 순서가 오프셋 순서와 다르다 (`+0x25d` 다음에 `+0xcb`). **실제 도는 순서대로** 적어서다. 처음 스레드는 `+0x266 JMP +0xc3` 로 앞쪽에 다시 붙는다 (2.6).
- 대표만 남겼다. 전체는 run 문서 "정적 — 호출 지점" 표.

### 2.3 한 줄씩 읽는 법

**줄 1: `└─ +0x17fe  ⇢ wow64!Wow64LdrpInitialize(CONTEXT)`**

- 누가: 64비트 ntdll `LdrpInitializeProcess` — 첫 스레드의 "가게 전체 세팅" 담당.
- 어디서: `+0x17ed` 에서 전역 칸 `g_LdrpWow64LdrpInitialize` 를 `RAX` 로 꺼내고, `+0x17f4` 에서 `RCX = [RSP+0xe0]` (사물함 메모), **`+0x17fc CALL RAX`**. 돌아올 자리 `+0x17fe` `[정적]`.
- 누구를: 그 칸에 적힌 주소 = **`wow64!Wow64LdrpInitialize`** (rva `0x11ed0`) `[정적]`.
- 무엇을 위해: 32비트 사무실을 꾸미고 32비트 직원을 들여보내려고.
- 덧붙임: 돌아올 자리 `+0x17fe` 가 적혀 있지만, **보통은 여기로 안 돌아온다** `[정적]`. 그래서 이 줄은 "책갈피는 꽂았는데 아무도 뽑지 않는" 줄이다 (2.9).

**줄 2: `│            ├─ +0xad  LdrpLoadDll(…\System32\wow64.dll, 0x800)`**

- 누가: `LdrpLoadWow64` (ntdll rva `0x89194`).
- 어디서: call `+0xa8`, ret `+0xad`. 셋째 인자 `0x800` 은 바로 앞 `+0x98 MOV R8D,0x800` 이 싣는다 `[정적]`.
- 누구를: 64비트 로더의 DLL 들이기 함수 `LdrpLoadDll`.
- 무엇을 위해: 통역사 (wow64.dll) 건물을 가게 땅에 들인다. 이 한 번에 import 로 조수 셋도 같이 들어온다 `[정적: PE import]`.

**줄 3: `└─ ServiceNoTurbo 0x1a79 ⇢ wow64!Wow64SystemServiceEx`**

- 누가: wow64cpu 안, Ghidra 가 함수로 못 잡은 구간. PDB 공개 이름은 둘 (`ServiceNoTurbo` · `TurboDispatchJumpAddressEnd`), 자리는 RVA `0x1a6e` `[정적]`.
- 어디서: call RVA `0x1a73`, ret RVA `0x1a79`. 함수 이름 대신 **모듈 맨 앞에서 센 자리**로 적었다.
- 누구를: `[0x4350]` 칸에서 꺼낸 주소 = wow64.dll 의 `Wow64SystemServiceEx` `[정적]`.
- 무엇을 위해: 32비트 직원이 낸 신청서 (`ECX` = 번호, `RDX` = 32비트 인자 위치) 를 통역사 창구로 넘기려고.

### 2.4 첫 걸음 — `LdrpLoadWow64` 는 통역사 한 명만 부른다 (A1)

**무엇을 올리나** `[정적]`

1. 글 만들기: `+0x69` 에서 받은 시스템 폴더 글 (`…\System32\`) 을 붙이고, `+0x7a` 에서 `wow64.dll` (`.rdata` `0x17bf78`) 을 붙인다 → `…\System32\wow64.dll`.
2. `+0x8e LdrpInitializeDllPath(글, 0x4001, &경로)` → **`+0xa8 LdrpLoadDll(&글, &경로, 0x800, &항목)`** → `+0xb4 LdrpReleaseDllPath`.

- 받은 시스템 폴더 글은 `LdrpInitializeProcess +0xd36 ~ +0xd4a` 가 만든 것이다. 그 앞부분이 무엇인지는 안 따라갔다 `[공백]`.

**이름 6개 → ntdll 전역 6칸** `[정적]`

**새 말 — `.mrdata`**: ntdll 안에서 로더가 **평소엔 쓰기를 잠가 두는** 전역 구역. `LdrProtectMrdata(0)` 로 열고 `(1)` 로 다시 잠근다 (결과서: "`.mrdata` 쓰기 허용"). 비유: 관리실의 **잠긴 연락처 칸**. 열쇠로 열고 적은 뒤 다시 잠근다.

표 `0x173830` (16 B × **6칸**, 칸 = `{이름, 쓸 ntdll 전역 주소}`) 을 돌며 `+0xf1 LdrGetProcedureAddress` 로 찾아 **바로 그 칸에** 적는다 (`+0xc3 LdrProtectMrdata(0)` ~ `+0x143 LdrProtectMrdata(1)` 사이).

| 칸 | 이름 | 적는 ntdll 전역 | 그 전역을 읽는 곳 |
|---|---|---|---|
| 0 | `Wow64LdrpInitialize` | `0x1e70c8` `g_LdrpWow64LdrpInitialize` | `_LdrpInitialize +0x90` · `LdrpInitializeProcess +0x17ed` |
| 1 | `Wow64PrepareForException` | `0x1e70e0` | `KiUserExceptionDispatcher +0x1` (run82) |
| 2 | `Wow64ApcRoutine` | `0x1e70d0` | `KiUserApcDispatcher +0x6e` |
| 3 | `Wow64PrepareForDebuggerAttach` | `0x1e70f8` | `DbgUiRemoteBreakin +0x38` |
| 4 | `Wow64SuspendLocalThread` | `0x1e7118` | `RtlpWow64SuspendThreadWorker +0x0` |
| 5 | `Wow64SuspendLocalProcess` | `0x1e7110` | `RtlpWow64SuspendLocalProcess +0x0` |

- 비유: 관리실 연락처 칸 6개에 **통역사의 내선 번호 6개**를 적어 둔다. "첫 세팅은 이 번호", "사고 (예외) 나면 이 번호", "쪽지 (APC) 오면 이 번호" …
- 이 6칸은 파일 처음 값이 **0** 이고, 직접 쓰는 코드 줄은 없다. `LdrGetProcedureAddress` 가 출력 칸으로 받아 쓸 뿐이다 `[정적]`. 그래서 run82 의 "64비트에선 `Wow64PrepareForException` 이 0" 이 맞다 — test.exe 는 `LdrpLoadWow64` 를 안 타니까.
- 끝에 `+0x14b LdrpDereferenceModule(항목)` 로 **참조를 한 번 내려놓는다** (성공 때도) `[정적]`. 그 뒤 wow64.dll 이 몇 개 참조로 남는지는 `[공백]`.

**실패하면 — 가게 문을 닫는다** `[정적]`

- `LdrpLoadDll` 실패 또는 이름 하나라도 못 찾음 → 그 상태를 돌려준다 (이미 쓴 앞 칸은 안 되돌림).
- → `LdrpInitializeProcess +0x17cc JS` → 함수 끝 → `_LdrpInitialize +0x2f9 LdrpInitializationFailure` → **`+0x304 NtTerminateProcess(-1, 상태)`**.

**조수 셋은 어떻게 오나 (A5)** `[정적: PE import]`

- wow64.dll 의 import 표 (flow-writer `pefile`): `ntdll.dll` 598 · **`wow64base.dll` 1 (`sdwhbase`)** · **`wow64win.dll` 1 (`sdwhwin32`)** · **`wow64con.dll` 1 (`sdwhcon`)**.
- 셋 다 **변수 하나씩**만 가져온다. 그 변수가 2.13 의 **안내판** 이다.
- 그래서 `+0xa8 LdrpLoadDll(wow64.dll)` 이 import 를 훑는 한 번에 셋이 **같이** 올라온다. 고르는 레지스트리도, `LoadLibrary` 글도 없다.
- 비유: 통역사의 "기대는 건물 목록" 에 조수 셋이 적혀 있어서, 통역사를 들이면 관리실이 알아서 조수도 들인다.
- **문 (wow64cpu) 만 다르다.** 통역사가 나중에 따로 `LdrLoadDll` 로 들인다 (2.7).

### 2.5 `g_LdrpWow64LdrpInitialize` 의 주인 = `wow64!Wow64LdrpInitialize`

run17 · run19 가 남긴 `[추측]` 이 어떻게 닫혔는지 단계별로.

1. 근거 `[정적]`: 2.4 표 0번 칸 — `LdrpLoadWow64` 가 **이름 글 `Wow64LdrpInitialize`** 로 찾은 주소를 바로 `0x1e70c8` (`g_LdrpWow64LdrpInitialize`) 에 쓴다.
2. 근거 `[정적]`: 그 이름은 wow64.dll 내보내기 표에 있고, 그 주소의 PDB 이름도 `Wow64LdrpInitialize` (rva `0x11ed0`) 다.
3. 근거 `[정적]`: 이 칸에 쓰는 다른 코드는 없다 (파일 처음 값 0).
4. 그래서: `LdrpInitializeProcess +0x17fc` 와 `_LdrpInitialize +0x9a` (나중 스레드) 의 `CALL RAX` 대상 = **`wow64!Wow64LdrpInitialize`** `[정적]`.

- 이번에 run17 · run19 문서 맨 위에 "고쳐 읽기 (run138)" 한 줄씩 들어갔다. 틀린 걸 단정한 줄은 없었고, `[추측]` 이 닫힌 것뿐이다.

### 2.6 `Wow64LdrpInitialize` 차례 — 다섯 묶음 (A3)

run 문서 표는 16단계다. 다섯 묶음으로 나눠 본다. 입구 `RCX` = 커널이 맡긴 64비트 CONTEXT (사물함 메모) 다 `[정적]`.

**묶음 1 — 자리 잡기 (1 ~ 2단계)** `[정적]`

- TEB32 = TEB64 + `WowTebOffset` (`+0x55`), PEB32 = `[TEB32+0x30]` (`+0x69`).
- **새 말 — `Wow64Info`**: 통역사가 쓰는 작은 정보 칸. 자리는 **PEB32 + `0x490`** 이다. `sizeof(_PEB32)` 가 정확히 `0x490` 이라 **PEB32 바로 뒤** 다 (결과서 `pdb_struct.py`). 비유: 32비트 안내판 **바로 아래 붙은 통역사 메모 칸**.
- 그 주소를 TEB64 `+0x14d0` (`TlsSlots[10]`) 에 적는다 (`+0x8f`). 이 칸은 wow64 가 **자기가 쓴다**.
- `RtlWow64GetCpuAreaInfo(TEB64 +0x1488, 0, &정보)` — **CPU 영역** 을 풀어 본다 (2.11). 음수면 `+0x2a1 INT 0x29` (코드 `0x4b`).

**묶음 2 — 프로세스 처음만 (3 ~ 10단계)** `[정적]`

- `+0xba` 바이트 `0x4c736` 이 0 이면 (= 아직 처음) 이 묶음을 한다. 아니면 묶음 3 으로 건너뛴다. **나중 스레드는 이 묶음을 안 한다.**
- `+0x13d` **`ProcessInit()`** — 사무실 꾸미기 본체 (아래 표). 실패하면 `+0x15d NtTerminateProcess`.
- `+0x144` **`LdrProcessInitializationComplete()`** — ntdll 25 B 썽크로 `LdrpInitializationComplete(&LdrpProcessInitialized, &LdrpInitCompleteEvent, 0x1484)` 로 넘어간다 (결과서).
  - 이건 run17 의 **"칠판에 2 쓰고 종 치기"** 다.
  - 네이티브 길에선 관리실 (`_LdrpInitialize +0x252`) 이 종을 쳤다. **WoW64 길에선 통역사가 대신 친다.**
  - 순서 주의: `ProcessInit` 의 성공 여부를 **보기 전에** 부른다 (flow-writer 대조).
- `+0x179` 바이트 `0x4c736` ← 1 ("처음은 끝났다").
- `+0x1c8` **exe 알림**, `+0x258` **32비트 ntdll 알림** — 둘 다 `CpuNotifyMapViewOfSection`. 32비트 ntdll 알림 안에서 `Wow64Transition` 이 써진다 (2.12).
  - 32비트 ntdll 경로는 `Wow64DetectMachineTypeInternal` (→ `0x14c`) → `Wow64SelectSystem32PathInternal` (→ `\SysWOW64`) → `<시스템루트>\SysWOW64\ntdll.dll` 로 만든다.
- `+0x266 JMP +0xc3` → 묶음 3 으로.

`ProcessInit` 안 (wow64 rva `0x12e3c`) 대표 줄 `[정적]`:

| 줄 | 하는 일 | 비유 |
|---|---|---|
| `+0x54` | `Wow64Info +0x0 ← 0x1000` | 통역사 메모 첫 칸 |
| `+0x5f` | `Wow64InitializeEmulatedSystemInformation` → `NtQuerySystemInformation(0x3e)` → `Wow64pMaxAddress` (실패해도 계속) | 사무실 크기 한도 알아 두기 |
| `+0x6c ~ +0x9a` | 64비트 ntdll **`LdrSystemDllInitBlock`** 에서 32비트 ntdll 정보 복사: `+0x48` → `NtDll32Base`, **`+0x18` → `Ntdll32LoaderInitRoutine`**, `+0x20 · +0x28 · +0x30` → 32비트 `KiUser*Dispatcher` 셋 | 관리실 **업무 수첩**에서 "옛날식 관리실 주소 · 첫 출근 자리" 를 베껴 적기 |
| `+0xa8` | `Wow64pInitializeFilePathRedirection` — `NtOpenFile` · `NtQueryObject` (윈도 폴더), `NtOpenDirectoryObject` (`\KnownDlls32`) | 앞 둘이 실패하면 **프로세스 죽음**, `\KnownDlls32` 실패는 통과 |
| `+0xb5 ~ +0x14d` | 서비스 표 `ServiceTables` (`0x52030`) ← 안내판 4장 (2.13) | 창구 벽에 안내판 붙이기 |
| `+0x154 ~ +0x1a2` | PEB32 에 64비트 PEB 칸 셋 복사 (`+0x1f8` · `+0x200` · `+0x1e8`) | 32비트 안내판에 옮겨 적기 |
| **`+0x1ab`** | **`CpuLoadBinaryTranslator`** — 문 고르고 달기 (2.7) | 문 달기 |
| `+0x1c7` | `BtFuncs[0]` = `wow64cpu!BTCpuProcessInit` | 문 점검 |
| `+0x1e6` | `Wow64pInitializeCrossProcessMechanism` (`NtCreateSection` · `NtMapViewOfSection`) | — |

**묶음 3 — 갈림 (11단계)** `[정적]`

- `+0xc6` **`Run64IfContextIs64(문맥)`** — 참이면 `+0xcd JNZ +0x26b` → **`+0x297 RET`**. 2.9 에서 자세히.

**묶음 4 — 스레드 준비 (12 ~ 14단계)** `[정적]`

- `+0xd7` CPU 영역 `+0x0` 에 비트 1 켜기.
- `+0xda ~ +0xf1` CPU 전용 칸 (`CpuThreadSize` — wow64cpu 는 0).
- `+0xf4` **`ThreadInit`** → `CpuThreadInit` 이 `BTCpuGetBopCode` 값 (= 문 주소) 을 **TEB32 `+0xc0`** 에 쓴다 (`+0x32`). run121 이 본 "wow64 가 쓰는 TEB32 칸" 이 바로 이것이다.

**묶음 5 — 32비트로 넘기기 (15 ~ 16단계)** `[정적]`

- `+0x108 ThunkStartupContext64TO32` → `+0x111 Wow64SetupInitialCall` — 32비트 첫 업무 메모 쓰기 (2.8).
- `+0x11a` CPU 영역 `+0x0` 에 **비트 0** 켜기 → `+0x11d` **`RunCpuSimulation()`** → `BtFuncs+0x20` = **`wow64cpu!BTCpuSimulate`**.
- 그 뒤 `+0x122` 은 **`INT3`** 다. 돌아올 자리에 "여기 오면 안 됨" 표시가 놓여 있다 (2.9).

### 2.7 문 고르기 — 레지스트리, 없으면 `wow64cpu.dll`

**새 말 — CPU dll (바이너리 트랜슬레이터)**: 통역사가 쓰는 "문" 부품. 32비트 코드를 실제로 돌리고, 64 ↔ 32 를 오가게 한다. wow64.dll 이 이름표 34칸으로 함수 주소를 받아 쓴다 `[정적]`. 비유: 사무실에 **어떤 문을 달지** 고르는 것. 보통은 기본 문 (wow64cpu.dll) 이다.

**고르는 순서** (`CpuGetBinaryTranslatorPath`) `[정적]`

1. `+0xad NtOpenKey(\Registry\Machine\Software\Microsoft\Wow64\x86)`
2. 키가 있으면: `+0x122 NtQueryValueKey(값 이름 = exe 파일 이름)` → 없으면 `+0x18d` 기본값 (`""`) 으로 다시.
3. **키가 없으면** (`0xc0000034`, `+0xbe JZ +0x253`) → `+0x27b` 글 `wow64cpu.dll` → `…\system32\wow64cpu.dll`.

- 비유: 관리사무소 서류함에 "이 가게는 이 문을 달 것" 지시가 있으면 따르고, 서류함 자체가 없으면 **기본 문**.
- 에뮬레이터 입장에서 좋은 소식: 레지스트리에 키가 없어도 길이 열려 있다 `[정적]` (결과서).

**다는 법** (`CpuLoadBinaryTranslator`) `[정적]`

- **`+0x90 LdrLoadDll(0, 0, &경로, &핸들)`** — 조수 셋과 달리 **문만은 따로** 들인다.
- 표 `0x3a220` (**34칸**) 으로 `LdrGetProcedureAddress` → `BtFuncs` (`0x520f0`, 34 × 8 B).
- **필수 3**: `BTCpuProcessInit` (`+0x0`) · `BTCpuSimulate` (`+0x20`) · `BTCpuGetBopCode` (`+0x78`). 나머지 31 은 없으면 건너뛴다.
- wow64cpu 가 실제로 내보내는 건 `BTCpu*` 8개 (+ `TurboDispatchJumpAddressStart` · `…End`) 다.

### 2.8 32비트 ntdll 은 "부르지" 않고 "메모를 넘긴다"

질문 3 의 "32비트 `LdrInitializeThunk` 를 부르는 자리" 는 **없다** `[정적]`. 대신 업무 메모를 써서 문에 맡긴다.

**`Wow64SetupInitialCall` 이 하는 일** `[정적]`

1. `ThunkStartupContext64TO32` 가 64비트 사물함 메모를 **32비트 메모 (x86 CONTEXT)** 로 옮겨 적는다.
2. 32비트 스택 (`Esp`) 아래에 **그 메모의 사본** 자리를 만든다.
3. 그 아래에 호출 틀을 만든다: **`[Esp+4]` = 사본 주소**, **`[Esp+8]` = 32비트 ntdll 기준 주소** (`NtDll32Base`).
4. **`Eip ← Ntdll32LoaderInitRoutine`** (= 64비트 ntdll `LdrSystemDllInitBlock +0x18` 값). `Wow64SetupInitialCall +0x76` 이 `CONTEXT32.Eip` 칸에 쓴다.
   - (`Wow64Info +0x8` 이 0 이 아니면 `Eip` 는 그 값, `Ecx ← Ntdll32LoaderInitRoutine`. 누가 `+0x8` 을 쓰는지는 `[공백]`.)

**64비트 때와 나란히 놓으면**

| | 64비트 (test.exe, run16 · run17) | 32비트 (이번) |
|---|---|---|
| 첫 자리 | `LdrInitializeThunk` | `LdrSystemDllInitBlock +0x18` 값 |
| 받는 메모 | `Rcx` = 사물함 메모 주소 | `[Esp+4]` = 32비트 메모 사본 주소 |
| 받는 ntdll 주소 | `Rdx` = ntdll 칸 | `[Esp+8]` = 32비트 ntdll 기준 |
| 누가 놓나 | 커널 (출발 메모 → `IRETQ`) | 통역사 (CPU 영역 메모 → 문의 `IRETQ`) |

- 모양이 거의 같다. 결과서도 "32비트 `LdrInitializeThunk` 는 `(PCONTEXT, NtDllBase)` 로 들어옴" 을 Q253c 에서 **확인할 것** 으로 넘겼다.
- 그래서 "`+0x18` 값 = 32비트 `LdrInitializeThunk`" 는 아직 `[추측]` 이다.
  - 근거: wow64 가 붙인 이름 `Ntdll32LoaderInitRoutine`, 그리고 위 표처럼 64비트와 같은 꼴의 인자.
  - 확인 아님: 32비트 ntdll 본문을 아직 안 봤다 → **Q253c**.
- 비유: 관리사무소가 64비트 직원 책상에 출발 메모를 붙여 두었듯, **통역사가 32비트 직원 책상에 출발 메모를 붙인다.** "옛날식 관리실 오리엔테이션실로 가서, 이 메모 사본을 들고 시작해."

### 2.9 핵심 — `Wow64LdrpInitialize` 는 돌아오지 않는다

이번 바퀴에서 에뮬레이터 쪽에 가장 중요한 답이다. 천천히 가 보자.

**먼저, "돌아온다" 는 게 뭔가** ([콜스택](../02_sprint/run9-create-user-process-해설.md#콜스택))

- `LdrpInitializeProcess +0x17fc CALL RAX` 는 책갈피 `+0x17fe` 를 꽂고 간다.
- 부른 함수가 언젠가 `RET` 을 하면 그 책갈피를 뽑아 `+0x17fe` 로 돌아온다. 그다음 줄이 `+0x1801 JMP +0x1d1b` (끝 정리) 다.
- 그러니 질문은: **`Wow64LdrpInitialize` 안 어딘가에 그 책갈피를 뽑는 `RET` 까지 가는 길이 있나?**

**근거 셋 — 보통 길엔 없다** `[정적]`

① 문 (`BTCpuSimulate`, rva `0x1780`) 이 **끝없는 고리** 다.

```
+0x4  CALL RunSimulatedCode
+0x9  JMP +0xb
+0xb  JMP +0x4
```

- `RunSimulatedCode` 가 혹시 돌아와도, 다시 `+0x4` 로 가서 또 부른다. 빠져나가는 줄이 없다.

② 그런데 `RunSimulatedCode` 는 **돌아오지도 않는다.**

- 머리부터 `CpupSyscallStub` 앞까지 (RVA `0x1920 ~ 0x1f6f`) **`ret` 이 0개** 다 (flow-writer 가 objdump 로 다시 셈).
- 32비트에서 나오는 길은 모두 `CpupReturnFromSimulatedCode` (`0x1a38`) → (통역) → **`0x1a7d JMP 0x194f`** (= `RunSimulatedCode +0x2f`) 로 다시 32비트로 들어간다.
- 즉 문은 "나온 사람을 다시 들여보내는" 일만 한다.

③ 돌아올 자리에 **"여기 오면 안 됨" 표시** 가 놓여 있다.

- `Wow64LdrpInitialize +0x122` = `INT3`.
- `RunCpuSimulation +0x12 ~ +0x17` = `INT 0x29` (코드 7).
- 둘 다 "도달하면 멈추라" 는 명령이다. 돌아올 걸 기대하는 코드라면 여기에 정리 코드가 있었을 것이다.

**그래서**: 처음 스레드는 `+0x11d RunCpuSimulation` 에서 32비트 세계로 들어가고 **다시는 `LdrpInitializeProcess +0x17fe` 로 안 온다** `[정적]`.

- 이 프로세스의 남은 삶 (32비트 ntdll 초기화 → x86 exe `main` → …) 은 모두 이 고리 **위에서** 돈다.
- 비유: 통역사는 사무실을 꾸미고 직원을 들여보낸 뒤 **문 앞을 떠나지 않는다.** 관리실로 돌아가 "끝났어요" 하지 않는다. 종 (칠판 2) 은 이미 2.6 에서 자기가 쳤다.

**예외 하나 — `Run64IfContextIs64` 가 참일 때** `[정적]`

- 11단계 `+0xc6 Run64IfContextIs64(문맥)` 이 참이면 `+0xcd JNZ +0x26b` → (쿠키 검사 `+0x275`) → **`+0x297 RET`**. 이 함수 전체에서 **돌아오는 유일한 길** 이다.
- 참이 되는 조건 (결과서):
  - 문맥의 시작 주소 (`R9` 칸이 표시 `0xF0E0D0C0A0908070` 이면 `Rip`, 아니면 `Rcx`) 를 본다.
  - PEB64 `Ldr` 목록에서 그 주소가 든 모듈이 **64비트 PE · CLR 헤더 없음** 이면 참.
- 쉽게: "이 스레드는 **64비트 코드에서 시작하는** 스레드" 면 32비트로 넘기지 않고 64비트 로더로 돌려보낸다.
- x86 exe 첫 스레드에서 이것이 **거짓** 인지는 실행 때 값이라 `[추측]` (Q260 2).
  - 짐작의 이유: 조건이 "64비트 PE 모듈 안" 이다. 첫 스레드 시작 주소가 x86 exe (64비트 PE 아님) 안이라면 거짓이 된다.
  - 확인 아님: 시작 주소가 실제로 어느 모듈 안인지는 아직 못 봤다.

**run19 이 고쳐지는 곳**

- run19 (그리고 run19 해설 2.5 묶음 3) 는 "WOW64 면 … `+0x1801 JMP` 로 끝 정리 `+0x1d1b` 까지 건너뛴다" 로 읽었다.
- 이번 결과: **`+0x1801` 은 위 예외 갈래에서만** 탄다 `[정적]`. 보통 길은 거기까지 오지도 않는다.
- 나중 스레드 `_LdrpInitialize +0x9a` 도 같다. 돌아오면 `+0xa2 LdrpInitializeThread` 인데, 그것도 예외 갈래에서만.
- 덤 (flow-writer 대조): 예외 갈래로 돌아와도 종이 두 번 울리지 않는다. `_LdrpInitialize +0x31b` 가 칠판이 **아직 1 일 때만** `LdrpInitializationComplete` 를 부르는데, 통역사가 이미 2 로 올려 두었기 때문이다 `[정적]`.

### 2.10 문을 지나는 법 — CS `0x23` / `0x33` (A2)

**새 말 — 셀렉터 (CS)**

- `CS` 는 CPU 가 "지금 어떤 코드 규칙으로 돌고 있나" 를 고르는 레지스터다. 거기 들어가는 번호를 **셀렉터** 라 하고, 그 번호가 가리키는 설명표 (GDT) 칸에 따라 **64비트 모드 / 32비트 (호환) 모드** 가 갈린다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이 빌드에서 보인 값:
  - **`0x33`** — 보통 64비트 유저 스레드의 CS. test.exe 스레드도 이 값으로 시작한다 `[정적]` (run15: `KiInitializeContextThread +0x163`).
  - **`0x23`** — wow64cpu 가 32비트 코드로 들어갈 때 넣는 값 `[정적]`. 결과서 AR05 는 이것을 "compat 코드" 칸으로 적었다.
  - `0x2b` (SS · DS · ES · GS 데이터), `0x53` (FS) 도 같이 쓴다 `[정적]`.
- 비유: **출입증.** `0x33` = 본관 (64비트) 출입증, `0x23` = 옛날식 사무실 출입증. 출입증을 바꿔 들어야 그 방 규칙으로 일할 수 있다.

**새 말 — 먼 점프 (`JMPF`)**

- 보통 `JMP` 는 주소만 바꾼다. **`JMPF` 는 주소와 CS 셀렉터를 한꺼번에** 바꾸는 점프다 `[추측]` (일반적으로 알려진 설명).
- 이번 코드의 꼴 `[정적]`: `JMPF m16:32 [R14]` — 메모리 `[R14]` 에 `{Eip, 0x0023}` 을 놓고 뛴다.
- 비유: 문턱을 넘는 **한 걸음에 출입증까지 바꿔 다는** 것.

**64 → 32 (들어가기) — 두 가지 길** `[정적]`

| 길 | 자리 | 명령 | 꼴 |
|---|---|---|---|
| 빠른 길 | `RunSimulatedCode +0x6b` (RVA `0x198b`) | **`JMPF m16:32 [R14]`** | `{Eip, 0x23}`, 앞에 `MOV SS,0x2b` · `MOV ESP,[R13+0x48]` |
| 다시 싣는 길 | `RunSimulatedCode +0x116` (RVA `0x1a36`) | **`IRETQ`** | 틀 `{Eip, 0x23, EFlags, Esp, 0x2b}`. 앞에 `FXRSTOR` · 세그먼트 · FS (`0x53` 또는 `WRFSBASE`) |

- **어느 길로 가나**: `+0x2f BTR [R13-0x80],0` — CPU 영역 `+0x0` 의 **비트 0** 을 보고 끈다. 켜져 있었으면 `IRETQ` 길.
- **첫 진입은 `IRETQ`** 다. 2.6 묶음 5 의 `Wow64LdrpInitialize +0x11a` 가 바로 그 비트 0 을 켜 두었기 때문이다.
- `IRETQ` 는 run15 에서 본 그 명령이다 (커널 → 유저). 이번엔 **유저 안에서** 64 → 32 로 쓰인다. 다섯 칸을 한꺼번에 바꾸니, 32비트 직원의 모든 레지스터 상태를 **통째로 다시 실을** 때 쓴다.
- 비유: 처음 들어갈 땐 **정식 입장** (`IRETQ` — 출발 메모 다섯 칸을 다 읽고 순간 이동), 그 뒤 잠깐 나왔다 들어갈 땐 **옆문** (`JMPF` — 주소 · 출입증만).

**32 → 64 (나오기) — 둘 다 CS `0x33`** `[정적]`

| 이름 | 자리 | 꼴 |
|---|---|---|
| `KiFastSystemCall` (빠른) | wow64cpu 구역 `W64SVC` RVA **`0x7000`** | `JMPF ptr16:32` (`EA`) → 64비트 `JMP [R15+0xf8]` |
| `KiFastSystemCall2` (느린) | wow64cpu 구역 `WOW64SVC` RVA **`0x3010`** | `push 0x33` … `jmp far [esp]` (= `JMPF m16:32`) |

- 둘 다 끝은 `CpupReturnFromSimulatedCode` (RVA `0x1a38`) 다. 거기서 32비트 `Eip` · `Esp` · `EFlags` · 레지스터를 CPU 영역에 저장하고 (`XCHG R14,RSP` 로 64비트 스택을 되찾고), 번호표를 보고 다음으로 보낸다.
- **`RETF` 는 없다** (`retf` · `lret` 0) `[정적]`. 요청서가 물었던 세 후보 중 `JMPF` 와 `IRETQ` 만 쓴다.
- 어느 쪽 문 주소를 쓰나는 `BTCpuGetBopCode` 가 고른다: `CpupSystemCallFast` 가 1 이면 `0x7000`, 아니면 `0x3010`. 1 이 되는 곳은 `BTCpuProcessInit +0xd8` 하나 (조건: KUSD `+0x308` ≠ 1 · "동적 코드 금지" 꺼짐 · `W64SVC` 실행 권한 바꾸기 성공).

**32비트 구간 동안 지켜야 할 것** `[정적]`

- `R12` (= TEB64) · `R13` (= CPU 영역 + `0x80`) · `R14` (64비트 스택) · `R15` (터보 표 `0x41b0`) 는 32비트 코드가 도는 동안 **64비트 레지스터에 그대로** 있어야 한다. 돌아올 때 이 값들로 길을 찾기 때문이다.
- 비유: 직원이 사무실 안에 있는 동안, 문 옆 **보관함 넷**에 통역사의 물건이 그대로 있어야 한다.

**wow64cpu 는 4GB 아래에 올라와야 맞는다** `[정적]`

- `CpuThreadInit +0x32` 가 문 주소의 **하위 32비트만** TEB32 `+0xc0` 에 쓴다.
- 32비트 코드가 `W64SVC` · `WOW64SVC` 를 **직접 실행** 한다.
- `0x7000` 의 `EA` 명령 바이트 (`ea 09 70 00 80 34 00`) 에 **재배치** 가 하나 걸려 있다 (`0x7001`, 타입 10). 이미지가 4GB 아래면 재배치로 고칠 때 셀렉터 자리가 `0x34 − 1 = 0x33` 이 된다 (결과서 풀이).
  - 재배치 = 건물이 예정 주소가 아닌 곳에 놓였을 때 안의 번지를 고쳐 쓰는 일 ([run26 해설](../02_sprint/run26-map-dll-file-해설.md)).
- **누가 낮게 올리나** 는 안 봤다 `[공백]` (Q261 4). 선호 기준 주소는 `0x180000000` (PE) 이다.

### 2.11 CPU 영역 — 문 옆의 32비트 업무 메모판

**새 말 — CPU 영역**: 32비트 직원이 문을 나올 때 **하던 일 상태 (32비트 CONTEXT) 를 적어 두는 칸**. 다시 들어갈 때 여기서 읽어 되살린다. TEB64 `+0x1488` (`TlsSlots[1]`) 이 가리킨다 `[정적]`. 비유: 문 옆에 걸린 **업무 메모판**. run11 의 업무 시작 메모를 32비트용으로 늘 걸어 두는 것.

칸 (KUSD `XState` 가 꺼진 경우, 64비트 ntdll `RtlWow64GetCpuAreaInfo` 가 계산) `[정적]`:

| 오프셋 | 칸 | 누가 |
|---|---|---|
| `+0x0` | 플래그 WORD (비트 0 = 다시 싣기, 비트 1 = ?) | 켬: wow64 `+0xd7` (비트 1) · `+0x11a` (비트 0). 끔: `RunSimulatedCode +0x2f` |
| `+0x2` | **Machine** `0x14c` | 읽기만 |
| `+0x4` | **x86 `CONTEXT`** (`0x2cc` B) — `Eip` (`+0xbc`), `SegCs` (`+0xc0`), `Esp` (`+0xc8`) … | 나올 때 저장, 들어갈 때 읽기 |
| `+0xd0` | `ExtendedRegisters` (512 B) | `FXRSTOR` |
| `+0x2f8` | CPU 전용 포인터 칸 | wow64 `+0xf1` (wow64cpu 는 0) |
| `+0x300` | 끝 | |

**누가 이 메모판을 거나** — 단계별로

1. 근거 `[정적]`: wow64.dll · wow64cpu.dll 을 다 훑어도 TEB64 `+0x1488` 에 **쓰는 줄이 없다**. 읽는 줄만 있다 (`Wow64LdrpInitialize +0x9f`, `RunSimulatedCode +0x20` 등).
2. 근거 `[정적]`: `+0x2` Machine 칸에 쓰는 줄도 둘 다 없다.
3. 그런데 2.6 묶음 1 에서 이미 이 칸을 **읽는다** — 통역사가 오기 전에 누군가 걸어 두었다는 뜻이다.
4. 그래서: **커널** 이 만든다 `[추측]`. 커널 쪽 이름 후보는 `PspWow64SetupCpuArea` 다 (run 문서 — 이름 · 줄은 sast/135 결과에서 맞춘다, Q253a).

- 에뮬레이터 쪽 뜻: 진짜 wow64 를 돌리려면 **이 메모판은 에뮬레이터 커널이 미리 걸어 줘야** 한다 (run 문서 PE_emulator-AR AR04 ②).

### 2.12 `Wow64Transition` — 옛날식 관리실 벽에 적는 "문 위치"

**새 말 — `Wow64Transition`**: 32비트 ntdll 이 **내보내는 4바이트 변수** `[정적]`. 비유: 옛날식 관리실 벽에 붙은 **"문은 여기" 쪽지**.

**누가 쓰나** `[정적]` — 통역사 (wow64.dll) 다

```
Wow64LdrpInitialize +0x258  CpuNotifyMapViewOfSection(32비트 ntdll)
  +0xb5  CpupResolveReverseImports
          +0x2a  BTCpuGetBopCode()                         → 문 주소
          +0x5b  RtlFindExportedRoutineByName(이미지, "Wow64Transition")
          +0x9a  NtProtectVirtualMemory(4 B, 쓰기 가능)
          +0xb2  MOV [RSI],EAX                              ← 여기서 씀 (RVA 0xbe5a)
          +0xc9  원래 보호로
```

- 값 = `KiFastSystemCall` (wow64cpu + `0x7000`) 또는 `KiFastSystemCall2` (+ `0x3010`) — 2.10 의 **나오는 문** 주소다.
- 같은 값이 스레드마다 **TEB32 `+0xc0` (`WOW32Reserved`)** 에도 들어간다 (`CpuThreadInit +0x32`).
- 32비트 `Nt*` 스텁이 이 둘 중 **어느 쪽을 보고** 문으로 가는지는 32비트 ntdll 본문이라 아직 모른다 → **Q253c**.
- 덤: 이 쪽지 쓰기는 이미지의 Subsystem 버전이 KUSD 의 OS 버전과 같을 때만 한다 (`+0x93 ~ +0xb0`, 결과서).

### 2.13 신청서 다리 — `Wow64SystemServiceEx` 와 안내판 4장 (A4)

**입구** `[정적]`

- `Wow64SystemServiceEx(ECX = 번호, RDX = 32비트 스택 인자 위치)` (wow64 rva `0x8c30`, 내보내기).
- 부르는 곳은 wow64cpu `ServiceNoTurbo` (RVA `0x1a73`) — 2.3 줄 3.

**번호 → 안내판 · 칸** `[정적]`

번호 (`EAX` 에 실려 온 32비트 쪽 번호표) 를 비트로 나눈다.

```
 비트 16 이상 : 터보 번호 (wow64cpu 가 씀, 0 이면 통역사 창구로)
 비트 12 ~ 13 : 안내판 번호  0 nt · 1 win32k · 2 console · 3 base
 비트  0 ~ 11 : 그 안내판의 칸 번호
```

**안내판 4장** — `ServiceTables` (`0x52030`, 4 × `0x30` B) `[정적]`

| 번호 | 안내판 | 누구 것 | 칸 수 | 담당 이름 꼴 |
|---|---|---|---|---|
| 0 | `sdwhnt32` | wow64.dll 자기 | **509** | `whNt*` (`whNtAccessCheck` 0 … `whNtWow64GetCurrentProcessorNumber` 508) |
| 1 | `sdwhwin32` | 조수 wow64win | **1497** | `whNt*` |
| 2 | `sdwhcon` | 조수 wow64con | **4** | `wh*` (`whUserConnectToServer` 등) |
| 3 | `sdwhbase` | 조수 wow64base | **16** | `whNt*` |

- 이 4장이 2.4 에서 조수 셋이 import 로 내준 **변수** 다. `ProcessInit +0xb5 ~ +0x14d` 가 창구 벽에 붙인다.
- 비유: run29 의 **번호표 안내판** 이 관리사무소 창구 벽에 하나 있었다. 통역사 창구엔 **4장** 이 붙어 있고, 번호표의 가운데 두 비트가 "몇 번째 안내판" 이다.

**담당 (`whNt*`) 이 하는 일** `[정적]`

- `+0x199 CALL RAX` 로 `whNt*(RCX = 인자 위치)` 를 부른다 (셋은 주소 비교로 바로 — `+0x169` · `+0x174` · `+0x18b`).
- `whNt*` 는 `RCX[0], [4], …` 로 **32비트 인자를 하나씩 읽고**, **64비트 ntdll `Nt*` 를 import 로 불러** 커널로 보낸다 (예 `whNtReadVirtualMemory +0x4f`).
- 비유: 통역 담당이 옛날 말 신청서를 한 칸씩 읽어 **새 말 신청서** 로 다시 써서, 관리실 창구 (`Nt*` → `SYSCALL`) 에 낸다.

**실패 상태 — 범위 검사는 사실상 없다** `[정적]`

- `+0xa0 CMP EDI,[표+0x10]` → 넘으면 `0xc000001c`.
- 그런데 한계가 4장 모두 `0x1000` 이고 칸 번호는 12비트 (`≤ 0xfff`) 라 **늘 통과** 한다. console 은 4칸뿐인데도.
- 변환 중 예외는 `Wow64HandleSystemServiceError` 가 표 `+0x20` 오류 종류로 정한다. nt 표는 0 이라 상태 그대로.

**지름길 — 터보 썽크** `[정적]`

- **새 말 — 터보 썽크**: 자주 쓰는 몇몇 부탁을 **통역사를 안 거치고 문 (wow64cpu) 안에서 바로** 커널로 보내는 짧은 코드. 번호의 비트 16 이상이 0 이 아니면 이쪽이다.
- 예: `WaitForMultipleObjects` 썽크 — `CpupSyscallStub` (`+0x0 MOV RAX,0x1a`) → `+0x11 SYSCALL` (KUSD `+0x308` 비트 0 이면 `INT 0x2e`).
- 비유: 간단한 부탁은 **문지기가 통역사 없이 바로 창구로** 들고 간다.
- 에뮬레이터 쪽 뜻: `SYSCALL` 명령이 **64비트 ntdll 밖 (wow64cpu 안)** 에서도 나온다.

### 2.14 비유로 처음부터 끝까지 한 번 더

1. x86 exe 가게가 열린다. 관리사무소 (커널) 가 32비트용 안내판 (PEB32 · TEB32) 과 **문 옆 메모판** (CPU 영역) 까지 걸어 둔다 (Q253a, `[추측]` 부분 있음).
2. 관리실 (64비트 ntdll) 은 세팅하다가 `+0x1766` 에서 "32비트 손님이네" 를 알아챈다.
3. **통역사 한 명** (wow64.dll) 을 들인다. 기대는 건물 목록에 있던 **조수 셋** 이 같이 온다. 연락처 칸 6개에 통역사 내선 번호를 적는다.
4. 관리실은 첫 번호 (`g_LdrpWow64LdrpInitialize`) 로 통역사를 부르며 사물함 메모를 건넨다.
5. 통역사는 사무실을 꾸민다: 통역사 메모 칸 (`Wow64Info`), 수첩에서 옛날식 관리실 주소 베끼기, **창구 안내판 4장**, **문 고르고 달기** (wow64cpu).
6. 칠판에 2 를 쓰고 **종을 친다** (원래 관리실이 하던 일).
7. 옛날식 관리실 벽에 **"문은 여기" 쪽지** (`Wow64Transition`) 를 붙인다.
8. 32비트 직원 책상에 **출발 메모** 를 붙인다: "옛날식 관리실 입구로 가서, 메모 사본 들고 시작해."
9. 문에 맡기면, 문은 **정식 입장** (`IRETQ`, 출입증 `0x23`) 으로 직원을 들여보낸다.
10. 그 뒤로는 고리다: 직원이 부탁할 일이 생기면 출입증 `0x33` 으로 문을 나와 → 통역사 창구 → 안내판 → 통역 담당 → 관리실 창구 → 관리사무소 → 답 → 다시 들여보내기.
11. 통역사는 **관리실로 돌아가 "끝났어요" 하지 않는다.** 관리실의 `+0x17fe` 책갈피는 그대로 남는다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

그림은 안 바뀌었다 (그림 2 에 `UseWOW64` 갈래 노드가 없다). 바뀐 건 글이다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (PDB 줄) | wow64 쪽 줄 없음 | 새 줄 둘: wow64cpu · wow64 PDB (처음 analyze), wow64win · wow64base · wow64con PDB (받기만) |
| 2.4 `_LdrpInitialize +0x9a` | 대상 `[추측: wow64 쪽]` (run17) | **`wow64!Wow64LdrpInitialize`** `[정적]`. 보통 안 돌아와 `+0xa2` 는 예외 갈래만. `+0x31b` 은 칠판이 아직 1 일 때만 종 |
| 2.4 `LdrpInitializeProcess` 나무 · `+0x17fe` 줄 | 대상 `[추측: wow64 쪽 시작 함수]`, 뒤에 `+0x1801 JMP +0x1d1b` (run19) | 대상 `[정적]`, **보통 돌아오지 않음 → `+0x1d1b` 는 예외 갈래만** |
| 3장 끝 | 없음 | **새 절 "WoW64 (x86) 초기화 — 64비트 쪽 DLL"** (나무 + `[정적]` 줄들: 6칸 · 차례 · 돌아오지 않음 · 메모 넘기기 · CPU dll · 전환 꼴 · CPU 영역 · `Wow64Transition` · 다리 · PE_emulator-AR) |
| 4장 2.4 이름 줄 | `g_LdrpWow64LdrpInitialize` 가 가리키는 함수 — 못 정함 | **(해결됨, 정적 → run138)** |
| 4장 새 줄 | — | Q253b 닫힘 줄, run138 남은 것 셋 (동적 Q260 · 정적 Q261 · PE_emulator-AR) |
| 기준 근거 줄 | — | run138: 새 사진 읽기 없음, test.exe 가 이 갈래를 안 탄다는 앞 run 의 줄만 다시 짚음 |
| 5장 근거 run · 요청서 번호 | — | run138 줄 둘 |

- 짐 지도 (`baggage.md`) 는 안 건드렸다. test.exe 초기화 밖이라 짐 목록에 들어갈 것이 없다.

### 큰 그림에서 어떻게 이어지나

**WoW64 지도의 세 조각 중 가운데가 채워졌다.**

```
 커널이 32비트 짐을 짓는다      (Q253a, sast/135 — 문서 다음 차례)
   PEB32 · TEB32 · CPU 영역 · LdrSystemDllInitBlock +0x18 ~ +0x48 · GDT 칸 …
        ▼
 64비트 쪽 DLL 이 문까지 데려간다  (Q253b, 이번 run138)          ← 여기
   LdrpLoadWow64 → Wow64LdrpInitialize → BTCpuSimulate ═▶ 32비트
        ▼
 32비트 ntdll 이 받는다          (Q253c, sast/137 — 도는 중)
   LoaderInitRoutine(메모 사본, 32비트 ntdll 기준) → … → 은퇴 자리?
```

- 위 조각과 이어지는 곳: 이번에 "**wow64 는 안 쓰고 읽기만 하는 칸**" 이 드러났다 — CPU 영역 (TEB64 `+0x1488`) 과 그 Machine 칸, `LdrSystemDllInitBlock +0x18 ~ +0x48`. 이것들은 커널 쪽 (Q253a) 이 지어야 한다.
- 아래 조각과 이어지는 곳: 32비트 첫 자리 `Eip` 와 인자 꼴 (`[Esp+4]` · `[Esp+8]`), 문 주소 쪽지 (`Wow64Transition` · TEB32 `+0xc0`). Q253c 가 이것을 받아 확인한다.

**에뮬레이터 (PE_emulator-AR) 에게 뜻하는 것** (run 문서 표 — 고칠지는 사용자 결정)

- **AR06**: `+0x17c6` 은 **진짜 `LdrpLoadWow64` 를 돌려도 된다.** 그리고 **`+0x17fe` 는 돌아오지 않는다** — 엔진은 `+0x1801` 을 "돌아옴" 으로 두지 말고 진짜 코드에 넘기고 끝. 종도 따로 또 치지 않기.
- **AR04**: PEB32 바로 뒤 `Wow64Info` 자리, **CPU 영역은 커널 쪽이 지어야**, 32비트 스택 여유, 64비트 수첩 `+0x18 ~ +0x48`.
- **AR05**: `JMPF m16:32` · `IRETQ` · `EA` · `FXRSTOR` · 세그먼트 `MOV` · 조건부 `WRFSBASE` · wow64cpu 안 `SYSCALL`, GDT `0x23` · `0x2b` · `0x33` · `0x53`, wow64cpu 4GB 아래, KUSD `+0x308` 값.
- **AR06 지어 둘 것**: `\??\<시스템루트>` 열기 + `NtQueryObject(1)` 은 **없으면 프로세스 죽음**. `…\Wow64\x86` · `\KnownDlls32` · `NtQuerySystemInformation(0x3e)` 는 없어도 통과.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **x86 exe 에서 정말 안 돌아오나** (`Run64IfContextIs64` 가 거짓인가) | 시작 주소는 실행 때 값 `[추측]` | **Q260 2** (동적): `wow64!Wow64LdrpInitialize+0xcb` `al`, `+0x122` · `ntdll!LdrpInitializeProcess+0x17fe` 가 안 걸리는지 |
| 6칸이 실제로 채워지나, 차례 각 단계 결과 | 정적만 | Q260 1 · 3 |
| 실제로 고른 CPU dll 경로, **wow64cpu 가 4GB 아래에 올라오나** | 레지스트리 · 올린 주소는 실행 때 | Q260 4 (`lm m wow64cpu`) |
| 빠른 문 (`0x7000`) 을 쓰나 느린 문 (`0x3010`) 을 쓰나 | `CpupSystemCallFast` · KUSD `+0x308` 은 실행 때 | Q260 6 |
| 첫 `IRETQ` 틀 · CPU 영역 처음 값 · 세그먼트 전/후 | 실행 때 값 | Q260 7 · 11 (`GS:[0]` 이 TEB32 인지 `[추측]`) |
| 32 → 64 입구 · 안내판 · 터보 썽크가 실제로 쓰이나 | 실행 때 | Q260 8 · 9 · 13 |
| `Wow64Info` · `TlsSlots[10]` · `\KnownDlls32` 실제 값 | 실행 때 | Q260 10 · 12 |
| **CPU 영역을 누가 거나** (커널 `PspWow64SetupCpuArea` 쪽 `[추측]`) | wow64 두 DLL 에 쓰는 줄이 없음, 커널은 이번 범위 밖 | **Q253a** (sast/135 결과 대조) |
| `LdrSystemDllInitBlock +0x18` 값이 정말 32비트 `LdrInitializeThunk` 인가 `[추측]` | 32비트 ntdll 본문 범위 밖 | **Q253c** (sast/137) |
| 32비트 `Nt*` 스텁이 `Wow64Transition` 과 `fs:[0xc0]` 중 무엇을 보나, 32비트 쪽 `NtContinue` 은퇴 길 | 같음 | **Q253c** |
| `whNt*` 본문 (자주 쓰는 것), `Wow64pCpuInitializeStartupContext` · `ThunkStartupContext64TO32` 분기, `Wow64KiUserCallbackDispatcher` · `Wow64ApcRoutine` | 이번엔 차례 · 다리 꼴까지만 | **Q261 2** (정적) |
| wow64cpu 터보 썽크 본문 | Ghidra 가 함수로 안 잡은 구간 | **Q261 3** (정적) |
| wow64cpu 를 누가 4GB 아래로 올리나 `[공백]` | 로더 · 커널 `MiSelectImageBase` 쪽 | **Q261 4** (정적, Q253a C 8 과 같이) |
| KUSD `XState` 가 켜졌을 때 CPU 영역 크기 · `CONTEXT_EX` `0xcc` 뜻 `[공백]` | 꺼진 경우만 계산함 | **Q261 5** (정적) |
| 그 밖 `[공백]`: `LdrpInitializeProcess +0xd36 ~ +0xd4a` 글 앞부분, `LdrpLoadWow64` 뒤 wow64.dll 참조 수, CPU 영역 `+0x0` 비트 1 뜻 · 처음 값, `Wow64Info +0x4` (CpuFlags 로 보임 `[추측]`) · `+0x8` 쓰는 곳, `LdrSystemDllInitBlock +0xc8` · `+0xd0` 을 wow64 가 읽는 두 곳 (CFG 쪽 `[추측]`) | 이번 범위 밖 | run 문서 "아직 확인 필요" |

- 동적 13개 (Q260) 는 **x86 시험 exe** 가 있어야 한다. 사용자 결정이고, Q253d (AR01 D 절 VM 한 번) 와 합쳤다.

---

## 5. 더 읽기

- run 문서: [`run138-wow64-dlls.md`](run138-wow64-dlls.md) — 호출 지점 표 전체, 이름 6칸 표, `Wow64LdrpInitialize` 16단계 표, `ProcessInit` 표, 전환 꼴 표, CPU 영역 칸 표, 다리 표, import 표, PE_emulator-AR 표, flow-writer 대조
- 결과서: [`_workspace/sast/136_wow64-dlls_result.md`](../../_workspace/sast/136_wow64-dlls_result.md) — "읽는 / 쓰는 칸 모음", "부르는 syscall", 동적 13개 멈춤점, 다음 요청 후보
- 요청서: [`_workspace/sast/136_wow64-dlls.md`](../../_workspace/sast/136_wow64-dlls.md)
- flow.md: [3장 "WoW64 (x86) 초기화 — 64비트 쪽 DLL"](flow.md), 2.4 `_LdrpInitialize` · `LdrpInitializeProcess` 의 run138 줄, 4장 run138 줄
- 앞 해설: [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) (칠판 · 종, 나중 스레드 `+0x9a`), [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) (WOW64 갈림, `+0x17fc` 간접 호출), [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq) (`IRETQ`), [run32 해설](../02_sprint/run32-params-initblock-해설.md) (`LdrSystemDllInitBlock`), [run54 해설](run54-teb-fill-해설.md) (`_TEB32`), [run82 해설](run82-exception-user-해설.md) (`Wow64PrepareForException`), [run121 해설](run121-teb-other-dlls-해설.md) (TEB32 `+0xc0`)
- 원 요청: `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` A 절

# run140 wow64-ntdll32 — 해설

> 이 글은 [run140 문서](run140-wow64-ntdll32.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q253c** — PE_emulator-AR 요청 `AR01_internals-wow64.md` 의 **B 절** "32비트 ntdll" (`_workspace/plan.md`, 이번에 **끝 (run140)**)
  - 원 요청: `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` B 1 ~ 4.
  - 요청 차례 **C → A → B** 의 **셋째이자 마지막**이다. C = run139 (커널), A = run138 (wow64 DLL).
  - 원문 B 4 (이름 8개) 는 run139 표 B-0 이 이미 닫았다. 이번엔 그 8개가 32비트 ntdll 내보내기 표에 있는지만 맞춰 봤다 (2.14).
  - 결과서 판정 **분석성공** (B 1 ~ 5 전부. 못 닫은 몇 줄은 `[추측]` · `[공백]`).
  - 남은 것이 간 곳 (4절): 동적 → **Q265**, 정적 → **Q266**.
- 근거:
  - run 문서 [`run140-wow64-ntdll32.md`](run140-wow64-ntdll32.md) (가장 중요)
  - 요청서 [`_workspace/sast/137_wow64-ntdll32.md`](../../_workspace/sast/137_wow64-ntdll32.md) · 결과서 [`137_wow64-ntdll32_result.md`](../../_workspace/sast/137_wow64-ntdll32_result.md)
    - **처음 analyze 하나**: `SysWOW64\ntdll.dll` (x86, 1,822,400 B, 함수 4687, PDB `wntdll.pdb`).
  - flow.md 변경: `git show 845ed70 -- runs/03_sprint/flow.md`. **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다.** run138 · run139 와 같다.
   - 64비트 ntdll 갈림 값 `UseWOW64` 0 `[관찰]` (run44 사진, flow.md 2.4 "갈림 값"), EPROCESS `WoW64Process : (null)` `[관찰]` (run106 이 맞춘 줄).
   - 그래서 32비트 ntdll 이 도는 순간이 test.exe 에는 없다. "x86 exe 였다면 32비트 ntdll 이 이렇게 받는다" 를 코드로 읽은 것이다.
2. **ntdll 이 둘이다.** 이 글에서 이렇게 부른다.
   - **32비트 ntdll** (= run 문서의 `ntdll32`) — `SysWOW64\ntdll.dll`. `ntdll32` 는 우리 Ghidra 모듈 이름일 뿐이다. WinDbg 가 이걸 어떤 이름으로 보일지는 아직 모른다 `[공백]` (Q265).
   - **64비트 ntdll** — `System32\ntdll.dll`. test.exe 가 쓰는 그 ntdll.
3. **같은 오프셋이 두 모듈에 있다.** 특히 `LdrpInitializeProcess +0x17fe` 는 64비트에도 32비트에도 있는데 **전혀 다른 줄**이다 (2.6). 그래서 이 글은 오프셋 앞에 늘 어느 ntdll 인지 붙인다.
4. **오프셋 읽는 규칙** 은 앞 해설들과 같다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문의 "`+0x24 → +0x29`" = call 자리 → ret 자리. 대상 없는 숫자 = 그 함수 안 **줄**.
   - `TEB32 +0xfdc` 처럼 덩어리 이름과 붙은 숫자 = 그 덩어리 맨 앞에서 센 **칸 자리**.
   - `0x4b2b4230` 같은 주소는 파일에 적힌 기준 주소 (`0x4b280000`) 로 센 것이다. 실제로 올라오는 주소는 Q265.
   - 콜스택으로 본 적은 없다 (정적만).
5. **함수 이름에 `@8` 같은 꼬리가 붙어 있다.** 32비트 PDB 의 이름 꼴이다 (2.4 에서 푼다). 이 글은 64비트와 같은 이름 (꼬리 뗀 것) 을 쓴다.
6. **run 문서 비유의 "문고리" 는 이 글에서 run138 의 "문은 여기" 쪽지다.** 가리키는 건 같다 (`Wow64Transition`).

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- WoW64 큰 그림 = 64비트 가게 건물 안의 **옛날식 사무실** (32비트), wow64.dll = **통역사**, wow64cpu.dll = 사무실 **문**, 조수 셋 → [run138 해설 2.1](run138-wow64-dlls-해설.md#21-큰-그림-먼저--층이-넷-길이-둘)
- 셀렉터 = **출입증** (`0x33` 본관, `0x23` 옛날식 사무실), CPU 영역 = 문 옆 **업무 메모판** → [run138 해설 2.10 · 2.11](run138-wow64-dlls-해설.md#210-문을-지나는-법--cs-0x23--0x33-a2)
- `Wow64Transition` = 옛날식 관리실 벽의 **"문은 여기" 쪽지**, 통역사 창구의 **안내판 4장**, **터보 썽크** → [run138 해설 2.12 · 2.13](run138-wow64-dlls-해설.md)
- `Wow64Info` = 32비트 안내판 바로 아래 붙은 **통역사 메모 칸**, `.mrdata` = 관리실의 **잠긴 연락처 칸** → [run138 해설 2.4 · 2.6](run138-wow64-dlls-해설.md)
- PEB32 = **옛날식 안내판**, TEB32 = **옛날식 개인 안내판** (TEB64 뒷장, `+0x2000`), 옛날식 업무 시작 메모 (첫 x86 문맥) → [run139 해설 2.6 · 2.8 · 2.10](run139-wow64-kernel-해설.md)
- 32비트용 **벽 주소록** (`PsWowX86SharedInformation`, 표 B-0) → [run139 해설 2.14](run139-wow64-kernel-해설.md)
- 커널 = **관리사무소**, 스레드 = **직원**, ntdll = 가게 안 **관리실**, CONTEXT = **업무 시작 메모**, 맡겨 둔 원래 메모 = **사물함 메모** → [run16 해설 2.3](../02_sprint/run16-thunk-context-해설.md#23-비유-하나로-처음부터-끝까지)
- `LdrInitializeThunk` = **오리엔테이션실 입구**, 끝나면 `NtContinue` 로 "이 메모대로 보내 주세요" → [run16 해설 2.7](../02_sprint/run16-thunk-context-해설.md), [run29 해설 2.3](../02_sprint/run29-continue-start-해설.md) ("제 출발 메모를 이걸로 바꿔 주세요")
- `LdrpInitialize` = **안내 데스크**, `LdrpInitializeInternal` = **중간 방**, `_LdrpInitialize` = **갈림길 방**, 꼬리 점프 → [run17 해설 2.3 · 2.4](../02_sprint/run17-ldr-init-entry-해설.md#23-이름이-비슷한-세-함수--누가-누구인가)
- `LdrpProcessInitialized` = 문 앞 **칠판**, `LdrpInitializationComplete` = "칠판에 2 쓰고 **종** 치기" → [run17 해설 2.6](../02_sprint/run17-ldr-init-entry-해설.md#26-lock-cmpxchg--첫-스레드-를-가리는-한-줄)
- `LdrpInitializeProcess` 뒤쪽 = **이삿짐 들이고 전원 켜기**, 간접 호출 4곳 (`+0x17fc` WOW64 통로의 문 · `+0x1b59` kernel32 "출근 인사") , `LdrInitState` = 가게 문 **팻말** → [run19 해설 2.10 · 2.12](../02_sprint/run19-proc-init-modules-해설.md#210-간접-호출-4곳--칸에서-주소를-꺼내-부르기)
- 시스템 콜 번호 = **번호표**, 서비스 표 = **번호표 안내판**, `NtContinue` 번호 `0x43` → [run29 해설 2.4](../02_sprint/run29-continue-start-해설.md#24-창구까지--ntdll-스텁과-번호-0x43)
- `LdrSystemDllInitBlock` = 관리실 책상 위 **업무 수첩** → [run32 해설 2.8](../02_sprint/run32-params-initblock-해설.md#28-새-말--시작-정보-블록-그리고-어디에-쓰나)
- `\KnownDlls` = 동네 **공용 창고** → [run65 해설 2.3](run65-ldr-init-bodies-해설.md)
- 재배치 = 건물이 예정 주소가 아닌 곳에 놓였을 때 안의 번지를 고쳐 쓰는 일 → [run26 해설](../02_sprint/run26-map-dll-file-해설.md)

**이번에 더하는 비유** (run 문서 "결과" 비유를 가게 그림으로 옮긴 것):

- 이번 주인공은 **옛날식 사무실 안의 관리실** (32비트 ntdll) 이다. 그 안에서 처음 일하는 사람이다.
- 그 관리실도 **오리엔테이션실 입구** (32비트 `LdrInitializeThunk`) 가 있다. 직원은 문으로 들어오면서 **쪽지 두 장** 을 들고 그 입구에 선다.
  - 한 장: 업무 시작 메모의 **사본** (`[Esp+4]`).
  - 한 장: 옛날식 관리실 **주소** (`[Esp+8]`).
- 그 관리실은 64비트 관리실과 **같은 순서로** 가게를 세팅한다. 다만 64비트 쪽에 있던 "통역사에게 넘기기" 단계가 **없다**. 그래서 **끝까지 세팅하고 돌아온다.**

### 한 줄로 먼저 — 무엇을 얻었나

- 32비트 `LdrInitializeThunk` 는 스택에서 **`[Esp+4]` = 메모 사본 · `[Esp+8]` = 32비트 ntdll 기준 주소** 를 받는다 `[정적]`. run138 의 통역사가 놓고 간 두 쪽지와 **딱 맞물린다** (2.3).
- 사슬 이름은 64비트와 같다: `LdrInitializeThunk` → `LdrpInitialize` ⇒ `LdrpInitializeInternal` → `_LdrpInitialize` → `LdrpInitializeProcess` `[정적]`. 오프셋만 다르다 (2.4).
- **64비트 `LdrpInitializeProcess` 는 WoW 갈래에서 안 돌아오지만, 32비트 판은 끝까지 돌고 돌아온다** `[정적]`. x86 exe 의 kernel32 올리기 · import 연결 · TLS 콜백은 **이 32비트 코드가 한다** (2.5).
- 32비트 로더는 `TEB32 +0xfdc` (음수) → TEB64 → `TlsSlots[10]` → **`Wow64Info[0]`** 를 읽는다. 그 값이 **0 이면 끝없는 고리** 에 빠진다 `[정적 읽기]` (2.7 · 2.8).
- `Nt*` 스텁 509개는 모두 `mov eax,N · mov edx,&_Wow64SystemServiceCall · call edx · ret n` 꼴이고, `_Wow64SystemServiceCall` = **`JMP [Wow64Transition]`** 이다. 그 칸은 **파일 속에선 0** 이고, 통역사가 채운다 `[정적]` (2.10 · 2.11).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

WoW64 지도는 세 조각이다 (run138 · run139 해설 3절 그림). 이번엔 **맨 아래 조각** 이다. 이걸로 세 조각이 다 찼다.

```
 커널이 32비트 짐을 짓는다          (Q253a, run139)                        (채움)
   'WowP' · PEB32 · 32비트 서류/수첩 · TEB32 · 스택 둘 · CPU 영역 · GDT 칸 10
        ▼
 64비트 쪽 DLL 이 문까지 데려간다    (Q253b, run138)                        (채움)
   LdrpLoadWow64 → Wow64LdrpInitialize → BTCpuSimulate ═▶ 32비트
        ▼
 32비트 ntdll 이 받는다            (Q253c, 이번 run140)                    ← 여기
   LdrInitializeThunk([Esp+4], [Esp+8]) → … → LdrpInitializeProcess (끝까지) → NtContinue
```

run138 해설 1절 그림으로 보면, 그 그림 맨 아래에 물음표로 남겨 둔 줄이다.

```
 유저, 32비트 ntdll (SysWOW64\ntdll.dll)
  LoaderInitRoutine → …  → (32비트 쪽 NtContinue 은퇴?)                       (Q253c, sast/137)
                                                                              ← 이번에 채움
```

### 무엇이 궁금했나

에뮬레이터 쪽 계획 (갈래 AR): 64비트 ntdll 초기화는 흉내 내다가 `LdrpInitializeProcess +0x17fe` 부터 진짜 wow64 코드에 넘긴다 (run138). 그러면 **그 뒤 처음 도는 32비트 코드가 32비트 ntdll** 이다. 그 코드가 무엇을 받고, 무엇을 읽고, 바깥에 어떻게 부탁하는지 알아야 한다. 요청서 다섯 질문:

1. 32비트 `LdrInitializeThunk` 입구가 받는 것, 첫 함수 사슬. 수첩 `+0x18` 값이 정말 이 함수인가.
2. 32비트 `LdrpInitializeProcess` 가 처음 읽는 칸 (64비트판과 다른 점만).
3. `Nt*` 스텁 꼴 — `EAX` 에 싣는 값, 부르는 곳 (`Wow64Transition` 대 `fs:[0xc0]`), 인자 자리, `ret n`, `Wow64Transition` 파일 값.
4. 디스패처 셋의 입구, 32비트 `NtContinue` 가 가는 길.
5. 이름 8개가 32비트 ntdll 내보내기 표에 있나. `[4]` 가 `RtlUserThreadStart` 인가.

### 바퀴 전에 알던 것

- `[정적]` (run138) 통역사 `Wow64SetupInitialCall` 이 32비트 스택에 **`[Esp+4]` = 메모 사본 · `[Esp+8]` = 32비트 ntdll 기준** 을 놓고 `Eip` ← 수첩 `+0x18` 값.
  - "`+0x18` 값 = 32비트 `LdrInitializeThunk`" 는 run138 에서 `[추측]`, run139 에서 `[정적: 두 결과서 이어 읽기]` 까지 왔다. **받는 쪽 코드** 는 아직 아무도 안 봤다.
- `[정적]` (run138) `Wow64Transition` 은 `CpupResolveReverseImports +0xb2` 가 쓰고, 같은 값이 TEB32 `+0xc0` 에도 (`CpuThreadInit +0x32`). 32비트 스텁이 **어느 쪽을 보나** 는 Q253c 로 넘겼다.
- `[정적]` (run138) 번호표 비트 (0 ~ 11 칸 · 12 ~ 13 안내판 · 16 이상 터보), nt 안내판 `sdwhnt32` 509칸.
- `[정적]` (run138) `Wow64Info` = PEB32 + `0x490`, `Wow64Info[0]` ← `0x1000` (`ProcessInit +0x54`), 그 주소를 TEB64 `+0x14d0` 에 (`Wow64LdrpInitialize +0x8f`).
- `[정적]` (run139) TEB32 19줄 — `+0xfdc = −0x2000`, `+0x18` Self, `+0x30` PEB32, `+0x0` `0xffffffff`. 첫 x86 메모 `Eip` = 32비트 `RtlUserThreadStart`, `Eax` = 진입점, `Ebx` = PEB.
- `[정적]` (run15 ~ run19) 64비트 사슬과 오프셋 (`+0xe` · `+0x3c JMP` · `+0x5a` · `+0x16c`, `NtContinue` `+0x18`).
- `[공백]` 32비트 ntdll 은 analyze 한 적이 없었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계 (flow.md 새 절 나무를 줄인 것, ret 자리)

```
(wow64cpu RunSimulatedCode — IRETQ, CS 0x23 · Eip = 수첩 +0x18 값, run138)
32비트 LdrInitializeThunk   [Esp+4] 메모 사본 · [Esp+8] 32비트 ntdll 기준               [정적]
 ├─ (1) +0x11  LdrpInitialize                     (_LdrpInitialize@8)
 │              └─ +0x30 JMP  LdrpInitializeInternal
 │                             └─ +0xaf  _LdrpInitialize   (__LdrpInitialize@8, UseWOW64 비교 없음)
 │                                        ├─ +0x8b  LdrInitializeMrdata
 │                                        └─ +0xd4  LdrpInitializeProcess   끝까지 돌고 +0x7c9 RET
 │                                                    ├─ +0x40f ~ +0x430  TEB32+0xfdc → TEB64+0x14d0 → Wow64Info[0]
 │                                                    ├─ +0xa27  NtOpenDirectoryObject(\KnownDlls32)
 │                                                    ├─ +0x14db LdrpInitializeKernel32Functions
 │                                                    ├─ +0x16a4 LdrpMapAndSnapDependency
 │                                                    ├─ +0x17fe Kernel32ThreadInitThunkFunction  (64비트 +0x17fe 와 다른 줄)
 │                                                    ├─ +0x185c LdrpInitializeGraphRecurse
 │                                                    └─ +0x1918 LdrpCallTlsInitializers
 └─ (2) +0x29  NtContinue(메모 사본, 1) ⇢ wow64!whNtContinue → Wow64NtContinue
                                          ├─ +0x44  CpuSetContext ⇢ wow64cpu!BTCpuSetContext
                                          ├─ +0x1ed NtContinueEx   (TEB64 +0x14b8 목록에 틀이 있을 때만)
                                          └─ +0x211 NtTestAlert    (목록이 비고 TestAlert)
32비트 Nt* (509개)   mov eax,N · mov edx,&_Wow64SystemServiceCall · call edx · ret n
 └─ +0xc  _Wow64SystemServiceCall   JMP [Wow64Transition]  ⇢ wow64cpu (run138)
          (NtQueryInformationProcess 만: 기준 윗바이트 0x4b 면 call fs:[0xc0])
```

읽을 때 주의:

- `⇢` = 간접 · syscall 다리 (칸에서 주소를 꺼내 가거나, 문을 지나 통역사로 가는 길). run 문서 범례 그대로.
- 대표만 남겼다. 전체는 run 문서 "정적 — 호출 지점" 표.

### 2.2 한 줄씩 읽는 법

**줄 1: `└─ (2) +0x29  NtContinue(메모 사본, 1)`**

- 누가: 32비트 `LdrInitializeThunk` (rva `0x34230`, 72 B).
- 어디서: `+0x1f PUSH 1` · `+0x21 PUSH [EBP+8]` 로 인자 둘을 쌓고 `+0x24` 에서 부른다. 돌아올 자리 `+0x29` `[정적]`.
- 누구를: 32비트 `NtContinue` 스텁. 같은 주소에 이름이 둘 (`_NtContinue@8` · `_ZwContinue@8`) 붙어 있다.
- 무엇을 위해: 세팅이 다 끝났으니 "**처음 받은 메모 사본대로 이어 가 주세요**" 하려고. 64비트 `LdrInitializeThunk +0x13 → +0x18 NtContinue(사물함 메모, 1)` 과 같은 꼴이다.
- 덧붙임: `+0x11` 에서 `_LdrDelegatedLdrInitializeThunk` 칸이 0 일 때만 이 줄로 온다. 0 이 아니면 다른 범위 (`+0x85a48 ~`) 로 가는데, 이번 범위 밖이다 `[정적]`.

**줄 2: `└─ +0xd4  LdrpInitializeProcess   끝까지 돌고 +0x7c9 RET`**

- 누가: 32비트 `_LdrpInitialize` (PDB `__LdrpInitialize@8`, rva `0x342a0`, 667 B) — 갈림길 방.
- 어디서: call `+0xcf`, ret `+0xd4`. 그 앞 `+0x2f CMPXCHG [_LdrpProcessInitialized]` 에서 칠판을 0 → 1 로 바꾸는 데 이긴 스레드 (= 첫 스레드) 만 온다 `[정적]`.
- 누구를: 32비트 `LdrpInitializeProcess` (rva `0xc9344`, 6597 B) — 가게 전체 세팅 담당.
- 무엇을 위해: 옛날식 사무실 쪽 세팅 전부 (2.5).
- 덧붙임: 64비트 쪽은 같은 자리가 `+0x16c` 다 (run17). 그리고 **"끝까지 돌고 `+0x7c9 RET`"** 이 이번 바퀴의 핵심 대비다 (2.5).

**줄 3: `└─ +0xc  _Wow64SystemServiceCall   JMP [Wow64Transition]`**

- 누가: 32비트 `Nt*` 스텁 아무거나 (예: `NtContinue`, rva `0x79fd0`).
- 어디서: 스텁 `+0xa` 의 `call edx`, 돌아올 자리 `+0xc` `[정적]`.
- 누구를: `_Wow64SystemServiceCall` (rva `0xb5b70`, 6 B) — 하는 일은 `JMP [Wow64Transition]` 한 줄뿐.
- 무엇을 위해: 벽의 "문은 여기" 쪽지를 읽어 문 (wow64cpu) 으로 가려고 (2.10 · 2.11).

### 2.3 입구 — 통역사가 놓은 두 쪽지와 맞물린다 (B1)

이번 바퀴에서 제일 먼저 확인하고 싶었던 곳이다. 천천히 가 보자.

**새 말 — stdcall · fastcall (x86 호출 약속)**

- x86 (32비트) 에서 함수에 인자를 건네는 **약속** 이름들이다 `[추측]` (일반적으로 알려진 설명).
  - **stdcall** = 인자를 **스택에** 쌓아 건네고, 불린 함수가 끝에 `ret n` 으로 그 인자들을 **스스로 치운다**.
  - **fastcall** = 앞 인자 둘을 **`ECX` · `EDX` 레지스터** 로 건넨다.
- 64비트 쪽은 앞 인자들을 레지스터로 건넸다 (앞 해설들의 `Rcx` = 사물함 메모 · `Rdx` = ntdll 칸 처럼). x86 의 stdcall 은 그 자리에 **스택** 을 쓴다고 보면 된다.
- 비유: stdcall = 쪽지를 **책상 위에 쌓아** 건네기, fastcall = **양손에 쥐여** 건네기.
- run 문서는 32비트 `LdrInitializeThunk` 를 "stdcall 두 인자", `LdrpInitialize` 를 "fastcall 꼴" 로 읽었다.

**받는 쪽 코드** `[정적]` (`LdrInitializeThunk` `+0x0 ~ +0xc`)

| 자리 | 하는 일 (run 문서 표기) | 뜻 |
|---|---|---|
| `+0x0 ~` | 머리 (`PUSH EBP` 뒤 `EBP` 를 스택 기준으로 고정) | 이제 `EBP` 로 스택을 센다 |
| `+0x5` | `EDX = [EBP+0xc]` | = 들어올 때 **`[Esp+8]`** → **32비트 ntdll 기준 주소** |
| `+0x8` | `ECX = [EBP+8]` | = 들어올 때 **`[Esp+4]`** → **메모 사본 (CONTEXT)** |
| `+0xc` → `+0x11` | `LdrpInitialize` 부르기 | 두 손에 쥐여 (`ECX` · `EDX`) 다음 방으로 |

- 왜 `[EBP+8]` = 들어올 때 `[Esp+4]` 인가:
  1. 들어올 때 `[Esp]` · `[Esp+4]` · `[Esp+8]` 이 쪽지 자리다.
  2. `PUSH EBP` 가 4바이트를 하나 더 얹으니, 원래 칸들이 모두 4씩 뒤로 밀린다.
  3. `MOV EBP,ESP` 로 그 자리를 `EBP` 에 고정한다.
  4. 그래서 `[EBP+4]` = 원래 `[Esp]`, `[EBP+8]` = 원래 `[Esp+4]`, `[EBP+0xc]` = 원래 `[Esp+8]`.
- **`[Esp]` 는 아무도 안 읽는다** `[정적]`. 보통 이 칸은 "다 끝나면 돌아갈 곳" 이다.
  - 이 함수는 누가 `CALL` 해서 온 게 아니라 **메모를 보고 "그냥 거기서 시작"** 한 것이다 `[정적]` (run138 해설 2.8 "부르지 않고 메모를 넘긴다", run16 해설 2.3 의 5번과 같은 모양).
  - 그래서 돌아갈 곳이 필요 없는 것으로 보인다 `[추측]`. 실제로 끝은 `RET` 이 아니라 `NtContinue` 다 `[정적]` (2.12).

**통역사가 놓은 쪽 (run138) 과 나란히**

| | 통역사가 놓은 것 (run138, `Wow64SetupInitialCall`) | 32비트 관리실이 읽는 것 (이번) |
|---|---|---|
| 시작 자리 | `Eip` ← 수첩 `+0x18` 값 (`Ntdll32LoaderInitRoutine`) | 32비트 `LdrInitializeThunk` (rva `0x34230`) |
| `[Esp+4]` | 32비트 메모 **사본** 주소 | `+0x8 ECX = [EBP+8]` → 문맥 |
| `[Esp+8]` | 32비트 ntdll 기준 (`NtDll32Base`) | `+0x5 EDX = [EBP+0xc]` → 기준 |
| `[Esp]` | (놓은 값 이야기 없음) | 안 읽음 |

- 놓는 쪽과 받는 쪽이 **칸 하나하나 맞물린다** `[정적]`. run138 맨 위에 "고쳐 읽기 (run140)" 한 줄로 이 사실이 들어갔다.
- 64비트 때와도 같은 모양이다: 64비트 `LdrInitializeThunk` 는 `Rcx` = 사물함 메모, `Rdx` = ntdll 칸 (run138 해설 2.8 표). 레지스터 둘이 **스택 칸 둘** 로 바뀌었을 뿐이다.
- 비유: 통역사가 32비트 직원 책상에 "옛날식 관리실 오리엔테이션실로 가서, 이 메모 사본을 들고 시작해" 라고 붙여 둔 메모 (run138 해설 2.8). 이번에 **오리엔테이션실 입구에서 실제로 그 쪽지 두 장을 받아 드는 손** 을 봤다.

**수첩 `+0x18` 값이 정말 이 함수인가 → 예** — 근거 셋이 이어진다

1. 근거 `[정적: PE]` (run139 표 B-0): 커널 이름 표 0번 글 = `"LdrInitializeThunk"` → 32비트 벽 주소록 `PsWowX86SharedInformation +0x00`. 결과서가 `pefile` 로 다시 읽어 같다.
2. 근거 `[정적]` (run32 · run139): `PspPrepareSystemDllInitBlock +0xde ~ +0xf7` 이 그 주소록을 수첩 `+0x18` 부터 통째로 복사한다.
3. 근거 `[정적: PE]` (이번): 32비트 ntdll 내보내기 표에 `LdrInitializeThunk` 가 rva `0x34230` (번호 151) 에 있다. flow-writer 도 `pefile` 로 같은 값을 얻었다.
4. 그래서 → wow64 가 그 값을 `Ntdll32LoaderInitRoutine` 로 읽어 `Eip` 로 쓰면 (run138), 32비트 직원이 처음 서는 곳 = **32비트 `LdrInitializeThunk`** `[정적]`.

- run138 해설 2.8 이 "확인 아님: 32비트 ntdll 본문을 아직 안 봤다 → Q253c" 로 남겼던 줄이 이걸로 닫혔다.

### 2.4 사슬 — 64비트와 같은 이름, 다른 숫자

**새 말 — x86 PDB 이름의 꼬리 (`@N` 장식)**

- 32비트 PDB 에는 함수 이름에 **앞 밑줄 하나 + `@` + 숫자** 가 붙어 있다. 숫자는 인자 바이트 수로 알려져 있다 `[추측]` (일반적으로 알려진 설명 — stdcall · fastcall 이름 꾸밈).
- 그래서 함정이 하나 있다 `[정적]`:
  - 32비트 `_LdrpInitialize@8` = 64비트 **`LdrpInitialize`** (안내 데스크)
  - 32비트 `__LdrpInitialize@8` = 64비트 **`_LdrpInitialize`** (갈림길 방)
- 즉 **밑줄 하나는 꾸밈** 이고, 그 안에 원래 이름이 있다. run17 해설 2.3 의 "밑줄은 진짜 이름의 일부" 가 여기서도 그대로다 — 갈림길 방은 원래 이름부터 `_` 가 붙어 있어서, 꾸밈까지 붙으면 `__` 가 된다.
- `LdrInitializeThunk` · `KiUser*` · `Nt*` · `RtlUserThreadStart` 는 꼬리가 없다 `[정적]`.
- 비유: 출석부 (PDB) 의 옛날식 반은 이름 앞에 반 표시 `_` 와 뒤에 짐 개수 `@8` 을 적는다. 사람은 같다.

**사슬 표** `[정적]` — 64비트 해설 (run17) 의 방 이름 그대로

| 방 | 32비트 (rva · 크기) | 부르는 줄 (call → ret) | 64비트 대응 |
|---|---|---|---|
| 오리엔테이션실 입구 | `LdrInitializeThunk` (`0x34230`, 72 B) | `+0xc → +0x11` 안내 데스크, 돌아오면 `+0x24 → +0x29 NtContinue(사본, 1)` | `+0x9 → +0xe`, `+0x13 → +0x18` |
| 안내 데스크 | `LdrpInitialize` (`0x34265`, 53 B) | `+0x30 JMP` 중간 방 (꼬리 점프) | `+0x3c JMP` |
| 중간 방 | `LdrpInitializeInternal` (`0xb0eac`, 192 B) | `+0xaa → +0xaf` 갈림길 방 | `+0x5a` |
| 갈림길 방 | `_LdrpInitialize` (`0x342a0`, 667 B) | 첫 스레드: `+0x8b LdrInitializeMrdata` → `+0xcf → +0xd4` 세팅 담당 | `+0x16c` |
| 세팅 담당 | `LdrpInitializeProcess` (`0xc9344`, 6597 B) | 2.5 | — |

- **꼬리 점프** (run17 해설 2.4) 도 그대로다. 안내 데스크가 `+0x30 JMP` 로 비켜서니, 콜스택엔 안내 데스크가 안 보일 것이다 `[정적]` (결과서: "콜스택엔 이 프레임이 안 보임").
- **칠판** 도 그대로다. 갈림길 방 `+0x2f CMPXCHG [_LdrpProcessInitialized]` 가 0 → 1 에 이긴 스레드만 세팅 담당을 부른다 `[정적]`.
- 중간 방 `+0x1f` 는 TEB32 `SameTebFlags` 의 `0x4000` (SkipLoaderInit) 을 본다. 켜져 있으면 `+0x26 JNZ` 로 갈림길 방을 **아예 안 부른다** `[정적]`. 첫 스레드는 이 칸이 0 이 정상이라 일반 길로 간다 (2.9).
- 안내 데스크의 잡일 하나: 쿠키가 처음일 때만 `+0x14 ~ +0x23` 에서 PEB32 `+0x10` (서류) → `+0x8 Flags` 부호 비트를 보고 `_LdrpIsSecureProcess` 를 켠다 `[정적]`.
- 차이는 하나, 아주 크다: **갈림길 방에 `UseWOW64` 비교가 없다** `[정적]`. 64비트 갈림길 방에는 있다 (`+0x22d`, 결과서). 32비트 `.asm` 전체에서 `UseWOW64` · `g_LdrpWow64*` 글자가 0개다 (결과서 `grep`, flow-writer 도 0). 다음 절이 이 차이의 결과다.

### 2.5 핵심 대비 — 64비트 판은 안 돌아오고, 32비트 판은 끝까지 돌고 돌아온다

이번 바퀴에서 에뮬레이터 쪽에 가장 중요한 대비다. 두 세팅 담당을 나란히 놓아 보자.

**64비트 `LdrpInitializeProcess` (x86 exe 일 때, run19 · run138)** `[정적]`

```
 … 묶음 뒤1 · 뒤2
 뒤3  +0x1766 UseWOW64 ?  ── ≠ 0 ──▶ +0x17c6 LdrpLoadWow64  (통역사 들이기)
                                    LdrInitState = 3
                                    +0x17fc CALL g_LdrpWow64LdrpInitialize  (책갈피 +0x17fe)
                                       ⇢ wow64!Wow64LdrpInitialize → 문 앞 고리 …   ✗ 안 돌아옴
 (뒤4 ~ 뒤10: kernel32 · import · 출근 인사 · 그래프 · TLS … — 이 길에선 안 탐)
```

**32비트 `LdrpInitializeProcess` (이번)** `[정적]`

```
 … 앞쪽 세팅 (2.7 ~ 2.9 의 읽기들)
 +0xe39            ntdll 항목 (명부 첫 줄)
 +0xfe9 · +0x10cb  exe 항목 (명부 둘째 줄)
 +0x14db           LdrpInitializeKernel32Functions   (kernel32 준비)
 +0x16a4           LdrpMapAndSnapDependency          (exe 의 import 올리고 연결)
 +0x1712           LdrInitState = 2                  (팻말 "짐 다 들임")
 +0x17fe           Kernel32ThreadInitThunkFunction   (kernel32 출근 인사, CALL ESI)
 +0x185c           LdrpInitializeGraphRecurse        (전원 켜기)
 +0x1918           LdrpCallTlsInitializers           (exe TLS 콜백)
 +0x1995           PEB32.PostProcessInitRoutine      (값이 있을 때만)
 +0x19c7 JMP +0x4e6 (EAX = 0) → +0x7c9 RET           ✓ 돌아옴
```

- 위 줄들의 이름은 run 문서 그대로다. 순서는 run19 해설 2.5 의 **네이티브 길** (뒤4 ~ 뒤10) 과 같은 꼴이다.
- 그러니 흐름은 이렇다:
  1. 64비트 세팅 담당은 `+0x1766` 에서 "32비트 손님이네" 를 알아채고, 통역사에게 넘긴 뒤 **다시는 돌아오지 않는다** (run138 해설 2.9).
  2. 그 대신 통역사가 옛날식 사무실을 꾸미고 직원을 들여보낸다.
  3. 들어온 직원이 만난 **32비트 세팅 담당은 "통역사에게 넘기기" 단계 자체가 없다.** 그래서 네이티브 길처럼 **끝까지** 간다.
  4. 끝나면 `RET` 으로 갈림길 방 → 중간 방 → 오리엔테이션실 입구로 돌아오고, 입구가 `+0x24 NtContinue` 를 부른다.
- **x86 exe 의 kernel32 올리기 · import 연결 · TLS 콜백은 이 32비트 코드가 한다** `[정적]`. 64비트 쪽에서 "`+0x17fe` 이후 그림" 이라 불렀던 일들이 사실 **이 안에서** 일어난다 (결과서).
- 비유:
  - 64비트 관리실 세팅 담당 = 손님이 옛날식이면 **통역사를 불러 통째로 맡기고 자리를 뜨는** 사람. 다시 안 온다.
  - 32비트 관리실 세팅 담당 = 옛날식 사무실 **안의** 세팅 담당. 맡길 데가 없으니 이삿짐 들이고 전원 켜고 첫 안내까지 **끝까지 다 하고** "끝났어요" 하고 돌아온다.
- run138 해설 2.9 에서 "통역사는 관리실로 돌아가 '끝났어요' 하지 않는다" 를 봤다. 그 "끝났어요" 를 하는 사람이 **다른 층 (32비트) 에 따로 있었던** 셈이다.

**에뮬레이터에게 뜻하는 것** (run 문서 AR06 — 고칠지는 사용자 결정)

- 엔진 쪽 "초기화 끝" 표시를 64비트 `LdrpInitializeProcess +0x1801` 에 걸면 안 된다 (run138). 그리고 32비트 쪽 끝 (`LdrInitializeThunk +0x24 NtContinue`) 도 **에뮬레이터가 보는 64비트 `NtContinue` 가 아니다** (2.12). 그래서 "32비트 초기화 끝" 은 다른 신호로 잡아야 한다 `[정적 추론]`.

### 2.6 같은 `+0x17fe`, 다른 줄

헷갈리기 딱 좋은 곳이라 따로 뗐다.

| | 64비트 `LdrpInitializeProcess` | 32비트 `LdrpInitializeProcess` |
|---|---|---|
| `+0x17fc → +0x17fe` | **`CALL g_LdrpWow64LdrpInitialize`** (통역사 부르기, run19 · run138) | **`CALL ESI` = `Kernel32ThreadInitThunkFunction`** (`+0x17e2` 에서 칸을 읽음) |
| 같은 일을 하는 줄 | `Kernel32ThreadInitThunkFunction` = `+0x1b59 → +0x1b5e` | (위) |
| 그래프 초기화 | `+0x1bb6 → +0x1bbb` | `+0x1857 → +0x185c` |

- 둘 다 `[정적]` (run 문서 "앞 run 과 이어 읽기", 64비트 줄은 64비트 `.asm` 으로 flow-writer 가 다시 봄).
- **오프셋 숫자만 우연히 같고, 하는 일은 전혀 다르다.** 32비트 `+0x17fe` 는 오히려 64비트 `+0x1b5e` 와 같은 일 (kernel32 "출근 인사" 를 미리 한 번, run19 해설 2.10) 이다.
- 왜 신경 쓰나: 에뮬레이터 요청의 기준점이 "64비트 `LdrpInitializeProcess +0x17fe`" 다. 동적 때 멈춤점을 걸면서 모듈을 헷갈리면 **엉뚱한 줄** 에 걸린다. run 문서 "디버거 메모" 에도 이 경고가 들어갔다.
- 비유: 두 건물의 **같은 호수 (1702호)** 에 사는 다른 사람. 건물 이름 (모듈) 을 꼭 같이 적어야 한다.

### 2.7 TEB 쌍둥이 칸과 `Wow64Info` — 세 바퀴가 칸 하나씩 맡는다

32비트 세팅 담당이 앞쪽에서 읽는 칸 중 **WoW 에만 있는 읽기** 가 하나 있다 (`+0x409 ~ +0x430`). 이 짧은 구간에 세 바퀴 (커널 · 통역사 · 32비트 관리실) 가 다 얽혀 있어서 천천히 본다.

**순서대로** `[정적]` (32비트 `LdrpInitializeProcess`)

1. `+0x409` — `FS:[0x18]` (Self) 를 읽는다 = **TEB32 주소**.
2. `+0x40f` — TEB32 `+0xfdc` **`WowTebOffset`** 을 읽는다.
3. `+0x415 ~ +0x417` — **음수냐?**
   - 음수면 `+0x419` 에서 더한다 → TEB32 + (음수) = **TEB64** (앞장).
   - 0 이나 양수면 TEB32 그대로.
4. `+0x41b` — 지금 가리키는 판의 `+0x18` 이 **자기 자신** 인가?
   - TEB64 의 `+0x18` 은 자기가 아니다 → `+0x41e` 에서 갈라져 **`+0x428` TEB64 `+0x14d0` (`TlsSlots[10]`)** 을 읽는다.
   - TEB32 라면 자기다 → `+0x420` TEB32 `+0xe38` (`TlsSlots[10]`) 을 읽는다.
5. `+0x42e` — 거기 적힌 주소 (= **`Wow64Info`**) 의 첫 칸 `Wow64Info[0]` 을 읽는다.
6. `+0x430` 부터 — 그 값을 `_NativePageSize` 에 두고, 비트를 세어 `_NativePageShift` 에 둔다 (2.8).

- `TlsSlots` 는 TEB 안 칸 이름 (PDB) 이다. run138 · run139 에서 `TlsSlots[1]` (CPU 영역) 을 봤는데, 여기는 `[10]` 번 칸이다.
- 비유: 옛날식 개인 안내판 (TEB32) 에 "**앞장은 여기서 −0x2000**" 이 적혀 있다. 32비트 관리실은 그걸 보고 앞장 (TEB64) 으로 넘어가 10번 칸에서 "**통역사 메모 칸은 여기**" 를 찾고, 그 메모 칸 첫 줄 "**쪽 크기 = 0x1000**" 을 읽는다.

**누가 어느 칸을 채웠나 — 세 바퀴가 하나씩**

| 칸 | 누가 쓰나 | 어느 바퀴 |
|---|---|---|
| TEB32 `+0xfdc` = `−0x2000` | 커널 `MmCreateTeb +0x1d2` | run139 (표 18) |
| TEB32 `+0x18` = 자기 주소 | 커널 `MmCreateTeb +0x15a` | run139 |
| TEB64 `+0x14d0` = `Wow64Info` 주소 (PEB32 + `0x490`) | 통역사 `Wow64LdrpInitialize +0x8f` | run138 |
| `Wow64Info[0]` = `0x1000` | 통역사 `ProcessInit +0x54` | run138 |
| (읽기) | 32비트 `LdrpInitializeProcess +0x409 ~ +0x430` | **run140 (이번)** |

- 세 바퀴 문서가 각자 따로 읽은 줄인데, **쓰는 칸과 읽는 칸이 정확히 맞물린다** `[정적]`.

**TEB 쌍둥이 칸 — 같은 쌍이 양쪽 갈림을 다 정한다** `[정적: 이어 읽기]`

- 64비트 관리실: TEB64 `+0x180c` (`+0x2000`) 가 **양수** 면 `UseWOW64` (run18 `+0x5f2`) → 통역사 길.
- 32비트 관리실: TEB32 `+0xfdc` (`−0x2000`) 가 **음수** 면 TEB64 로 간다.
- 둘 다 커널 `MmCreateTeb +0x1ca` · `+0x1d2` (run139) 가 쓴 **한 쌍** 이다.
- 비유: 두 장짜리 개인 안내판의 앞장엔 "+0x2000 뒤에 뒷장", 뒷장엔 "−0x2000 앞에 앞장". run139 해설 2.8 에서 "어느 쪽에서 봐도 짝을 찾는다" 했던 그 쪽지가, **64비트 관리실과 32비트 관리실 각각의 길잡이** 로 쓰인 셈이다.

**음수가 아니면?** `[정적]` (run 문서 AR03)

- 0 이나 양수면 4번에서 TEB32 쪽 `+0xe38` 을 읽는다. 그런데 그 칸은 **아무도 안 쓴다** `[공백]` (wow64 는 TEB64 쪽에만 씀). 그러면 0 → 5번 `+0x42e` 에서 **널 참조** 로 죽는다.
- 그래서 에뮬레이터가 TEB32 를 지을 때 `+0xfdc` 는 **반드시 음수 `−0x2000`** 이어야 한다.
- 덤 (run 문서 AR04): 32비트 코드가 TEB64 (= TEB32 − `0x2000`) 를 **직접** 읽으므로 TEB64 도 4GB 아래에 있어야 한다. run139 의 "같은 칸막이 창고 (서브 영역)" 설명과 맞는다.

### 2.8 `Wow64Info[0]` 가 0 이면 — 끝없는 고리

2.7 의 6번을 자세히 본다. 요청에서 강조한 줄이다.

**하는 일 — 쪽 크기에서 "몇 비트 자리인가" 세기** `[정적]`

1. `+0x430` — 읽은 값 (보통 `0x1000`) 을 전역 `_NativePageSize` 에 둔다.
2. `+0x435` — 전역 `_NativePageShift` 를 0 으로 시작한다 (이 자리 `EBX` 가 0).
3. `+0x43b ~ +0x43d` — 맨 끝 비트가 이미 1 이면 셀 필요 없이 건너뛴다.
4. `+0x43f` — 셈 칸 `ECX` 를 0 으로.
5. `+0x441 ~ +0x446` — 이 세 줄이 고리다:

```
+0x441  SHR EAX,1                 값을 오른쪽으로 한 칸 민다
+0x443  INC ECX                   민 횟수 +1
+0x444  TEST DL,AL / +0x446 JZ +0x441   맨 끝 비트가 아직 0 이면 다시 민다
```

- `DL` 은 상수 1 로 읽힌다 (결과서). 그래서 `TEST DL,AL` = "맨 끝 비트가 1 인가?".
- `+0x435` 의 `EBX` · `+0x43b ~ +0x43d` 건너뛰기 줄은 flow-writer 가 `.asm` 으로 보탠 것이다 (결과서는 "= 0" 으로 줄여 적음, run 문서 "flow-writer 대조").
- 보통 값 `0x1000` 처럼 **1 이 딱 한 자리에만 있는 값** (2 의 거듭제곱) 이면, 그 1 이 맨 끝으로 올 때까지 밀고 멈춘다. 민 횟수 = "몇 비트 자리인가" 다.

**0 이면 왜 안 끝나나** `[정적 읽기]`

1. `EAX` = 0 이면 `SHR EAX,1` 을 해도 **계속 0** 이다.
2. 그러면 `TEST DL,AL` 도 **계속 0** 이고, `+0x446 JZ` 는 **늘 `+0x441` 로 되돌아간다.**
3. 빠져나가는 줄이 없다 → **멈추지 않고 헛돈다.**

- 그러니 `Wow64Info[0]` 은 **2 의 거듭제곱 (`0x1000`)** 이어야 한다 (run 문서). 진짜 wow64 가 돌면 `ProcessInit +0x54` 가 넣어 주니 자동으로 맞는다. 엔진이 wow64 를 **흉내 낸다면** TEB64 `+0x14d0` → 그 주소 → `0x1000` 을 직접 지어야 한다 (run 문서 AR03).
- **run138 의 고리와 다르다.** run138 해설 2.9 의 문 앞 고리 (`BTCpuSimulate`) 는 **일부러 만든** 고리다 — 나온 사람을 늘 다시 들여보내는 일. 이번 고리는 **값이 잘못됐을 때만** 생기는 헛돌기다. 이름만 같은 "끝없는 고리" 다.
- 비유: 자로 "이 길이는 눈금 몇 칸?" 을 셀 때, 반씩 접어 가며 셈하는 사람. 길이가 0 이면 아무리 접어도 0 이라 영원히 "아직 아니네" 하며 접고 있다.
- 에뮬레이터 입장에서 까다로운 점 `[추측]`: 위 세 줄에는 메모리를 읽는 줄이 없다. 그러니 2.7 의 "널 참조" 처럼 예외로 죽지 않고 **헛돌기만** 할 것으로 보인다. 빠뜨리면 원인을 찾기 어려운 종류다 (이 짐작은 고리 세 줄 모양에서 나온 것이고, 실행으로 본 건 아니다).

### 2.9 그 밖에 읽는 칸 — 대표만 (B2)

전체 표는 run 문서 2절 · 결과서 2절. 여기선 "64비트와 다른 점" 과 "0 이면 죽는 칸" 만.

**0 이면 바로 죽는 칸** `[정적]`

- **PEB32 `+0x10 ProcessParameters`** — 0 이면 바로 널 참조 (`LdrpInitialize +0x1a` 에서 읽고 `+0x1d` 에서 그 안을 봄, run 문서 AR03). 채우는 곳은 커널 `PspWow64ThunkProcessParameters +0x761` (run139 의 **옛날식 서류 사본**).
- **TEB32 `+0x30` PEB** — 0 이면 널 참조 (`+0x128` 에서 PEB32 `+0xc` 에 쓰려다). 커널 `MmCreateTeb +0x19b` (run139).
- **TEB64 `+0x14d0`** · **`Wow64Info[0]`** — 2.7 · 2.8.

**64비트판 (run18 · run32 · run114) 과 다른 점** `[정적]` — run 문서 목록 여섯

1. WoW 전용 읽기 `+0x409 ~ +0x448` (2.7 · 2.8).
2. `UseWOW64` · `LdrpLoadWow64` · `g_LdrpWow64LdrpInitialize` 줄이 **없다** → 끝까지 돈다 (2.5).
3. **`\KnownDlls32`** — `+0xa22 NtOpenDirectoryObject` 의 이름 글이 `\KnownDlls32` (파일 상수 `0x4b287a00`). 64비트는 `\KnownDlls`. 비유: 옛날식 손님 전용 **공용 창고** 가 따로 있다.
4. **환경 변수 WoW 변환** — `+0x8d2 → +0x8d7 _RtlpInitEnvironmentBlock` 안에서 `_RtlpWow64ThunkEnvironmentTo32` 가 3줄 표 (`ProgramFiles` · `CommonProgramFiles` · `PROCESSOR_ARCHITECTURE`) 로 값을 바꾸고, `NtWow64GetNativeSystemInformation(1, 버퍼, 0xc, 0)` (스텁 `EAX = 0x1f5`) 로 진짜 기계 정보를 묻는다.
   - 결과서 표: `ProgramFiles` ← `ProgramFiles(x86)` · `ProgramW6432`, `PROCESSOR_ARCHITECTURE` ← `"x86"` · `PROCESSOR_ARCHITEW6432` 꼴.
   - 비유: 옛날식 직원에게는 주소록의 "창고 위치" 를 **옛날식 창고 주소로 바꿔 적어** 준다.
5. **자기 수첩** — 32비트 관리실이 읽는 `LdrSystemDllInitBlock` 은 **32비트 ntdll 이미지 안의 별개 변수** (rva `0x13d2a0`, `.mrdata`) 다. 64비트 수첩과 이름만 같다.
   - 파일 속에 이미 `+0x0 Size = 0x128` 하나가 들어 있다 (flow-writer `pefile` 로 `28 01 00 00` 확인). run139 의 커널 `Size == 0x128` 검사와 맞는다.
   - 읽는 칸: `+0x8` (ntdll 항목 `OriginalBase` 에서 뺌), `+0x98` (난수), `+0xa0 ~ +0xb7` (완화 옵션, `+0x34c` 에서 통째 복사) 등.
   - **안 읽는 칸: `+0x18 ~ +0x97` (WOW 16칸)**. 그 칸은 64비트 수첩 쪽 사본을 통역사가 읽을 뿐이다 (run138).
   - 비유: 옛날식 관리실 책상에도 **자기 업무 수첩** 이 있다. 그런데 거기 꽂힌 "옛날식 관리실 주소록 사본" 페이지는 자기는 안 펴 본다 — 그 페이지를 쓰는 사람은 본관의 통역사다.
6. **첫 syscall 이 별난 스텁** — `+0x5e NtQueryInformationProcess(-1, 0x24 쿠키, …)` (쿠키 전역이 0 일 때). 2.11 의 그 한 스텁이다.

**첫 스레드라 "0 이 정상" 인 칸** `[정적]`

- TEB32 `+0xfca SameTebFlags` — 중간 방 `+0x1f` (`0x4000`), 갈림길 방 `+0x38` (`0x2000`) · `+0x238` (`0x40`). 모두 0 이면 일반 길. 커널 `PspAllocateThread +0x7db` 가 쓴다 (run139).
- PEB32 `+0x0` · `+0x2 BeingDebugged` · `+0x3 BitField` · `+0x1d8` · `+0x14c` 등 — 0 이면 "일반 길" (run 문서 표).
- PEB32 `+0xc Ldr` · `+0x18` · `+0x1c` · `+0xa0` · `+0x28` 은 **32비트 관리실이 직접 쓰는** 칸이다 — 비워 두면 된다.

### 2.10 `Nt*` 스텁 꼴 — 바이트 한 줄씩 (B3)

**대표 스텁 `NtContinue`** (rva `0x79fd0`) `[정적]`

```
b8 43 00 00 00   ba 70 5b 33 4b   ff d2      c2 08 00
mov eax,0x43     mov edx,0x4b335b70  call edx  ret 8
```

| 자리 | 뜻 |
|---|---|
| `+0x0` | `EAX` 에 **번호표** `0x43` |
| `+0x5` | `EDX` 에 `_Wow64SystemServiceCall` 의 주소 (rva `0xb5b70`) |
| `+0xa` | 그리로 간다 (책갈피 `+0xc`) |
| `+0xc` | 돌아오면 인자 8바이트를 치우고 부른 쪽으로 (`ret 8`) |

- 64비트 스텁 (run29 해설 2.4: `MOV R10,RCX` · `MOV EAX,0x43` · `SYSCALL` · `RET`) 과 비교하면, **`SYSCALL` 자리에 `call edx` 가 있다.** 32비트 관리실은 관리사무소 창구로 직접 못 가고, **문 (wow64cpu) 으로** 간다.
- 번호표가 64비트 스텁과 같은 `0x43` 이다. 그런데 이 번호는 **통역사 안내판 `sdwhnt32` 의 칸 번호** 다 — 칸 `0x43` = `whNtContinue`, 인자 8 ✓ (결과서 대조). 관리사무소 창구로 바로 가는 번호가 아니다 (2.12).

**번호표 `EAX` 쪼개기** `[정적]` — run138 해설 2.13 그림 그대로

```
 비트 16 이상 : 터보 번호 (wow64cpu, 0 이면 통역사 창구로)
 비트 12 ~ 13 : 안내판 번호 (32비트 ntdll 스텁은 모두 0 = nt)
 비트  0 ~ 11 : 그 안내판의 칸 번호
```

- 509개 스텁 = 32비트 ntdll 이 내보내는 `Nt*`/`Zw*` 주소 515개 중 syscall 스텁인 것. `NtCurrentTeb` · `NtGetTickCount` · `NtdllDefWindowProc_A/W` · `NtdllDialogWndProc_A/W` 6개는 스텁이 아니다 (flow-writer 가 다시 셈).
- 509 = 508 (같은 꼴) + `NtQueryInformationProcess` (다른 꼴). 칸 번호 0 ~ 508 이 겹침 없이 하나씩 → **통역사 nt 안내판 509칸과 딱 맞다** (run138).
- 예: `NtTerminateProcess` = `mov eax,0x7002c` → 칸 `0x2c`, **터보 7**. `NtTestAlert` = `0x201d3` → 터보 2.
- 터보 번호 셈 (flow-writer 다시 셈): **0 이 409개**, 나머지 99개가 1 ~ 30. 결과서 목록엔 1 · 2 가 빠져 있었는데 flow-writer 가 스텁 바이트로 다시 세어 보탰다 (1 × 4, 2 × 1). "32칸 터보 표 안" 이라는 결론은 그대로다.
- 비유: 번호표 하나에 세 정보가 들어 있다 — "어느 안내판", "몇 번 칸", 그리고 "문지기가 지름길로 들고 갈 수 있나 (터보)".

**`EDX` 는 인자 위치가 아니다 — 스택 그림** `[정적]`

진짜 32비트 Windows 의 `KiFastSystemCall` 은 `mov edx,esp` 로 **인자 위치** 를 `EDX` 에 담는다. 이 스텁의 `EDX` 는 **갈 곳 주소** 일 뿐이다. 인자는 스택에 그대로 있다.

```
부른 쪽이 NtXxx(인자1, 인자2) 를 부른 직후       스텁의 call edx 직후
  [Esp]    부른 쪽으로 돌아갈 곳                    [Esp]    스텁 +0xc (돌아올 곳)
  [Esp+4]  인자1                                    [Esp+4]  부른 쪽으로 돌아갈 곳
  [Esp+8]  인자2                                    [Esp+8]  인자1   ← 여기
                                                    [Esp+0xc] 인자2
```

- 문 (wow64cpu `CpupReturnFromSimulatedCode`, run138 RVA `0x1a38 ~ 0x1a4a`) 이 오른쪽 그림을 받아:
  1. `r8d = [esp]` — 스텁 `+0xc` 를 "돌아올 `Eip`" 로 챙긴다.
  2. `esp += 4`.
  3. **`r11 = esp + 4`** = 원래 `Esp + 8` = **인자1 자리**.
- 그 `R11` 이 통역사 창구 `Wow64SystemServiceEx` 의 `RDX` (= "32비트 스택 인자 위치", run138 해설 2.3 줄 3) 가 된다.
- 답이 오면 문은 결과를 메모판 `[r13+0x34]` (`Eax`) 에 넣고 32비트로 돌아간다. 그러면 `Eip` = 스텁 `+0xc` 의 **`ret n`** 이 실행된다. `n` 은 통역사 안내판의 인자 바이트 수와 같다 (결과서 7개 대조 ✓).
- 비유: 직원은 신청서 (인자) 를 **책상 위에 쌓아 둔 채** 문으로 간다. 문지기가 "책상 위 몇 번째 장부터가 신청서" 를 짚어 통역사에게 넘긴다.

**32비트 ntdll 안에 "진짜 32비트 창구" 스텁도 있긴 하다** `[정적]`

- `KiFastSystemCall` (`0x7c010`: `mov edx,esp; sysenter`), `KiIntSystemCall` (`0x7c030`: `lea edx,[esp+8]; int 0x2e`).
- 그런데 **`Nt*` 스텁 509개는 이걸 안 부른다** (509개 바이트로 확인). 다른 코드가 부르는지는 안 봤다 `[공백]`.

### 2.11 `Wow64Transition` — 파일 속 0, 통역사가 채운다

**모든 스텁이 한 칸을 본다** `[정적]`

```
_Wow64SystemServiceCall  (rva 0xb5b70, 6 B)
  ff 25 14 b0 3b 4b    JMP [0x4b3bb014]     = 기준 + 0x13b014 = Wow64Transition
```

- 508개 스텁 → `call edx` → 이 6바이트 → **`Wow64Transition` 칸에 적힌 주소로 점프**.
- 이 칸을 코드에서 읽는 줄은 **이 한 줄뿐** 이다 (`_refs/Wow64Transition.tsv` — 나머지 둘은 내보내기 표 · 외부 참조).
- 비유: 옛날식 관리실의 모든 직원은 바깥 일이 생기면 **같은 안내 데스크 한 곳** (`_Wow64SystemServiceCall`) 으로 가고, 그 데스크는 벽의 **"문은 여기" 쪽지** 를 보고 문으로 보낸다.

**파일 속 값은 0** `[정적: PE]`

- `Wow64Transition` (내보낸 변수, rva `0x13b014`, `.mrdata`) 의 파일 속 값 = `00 00 00 00` (flow-writer `pefile` 로 확인).
- 즉 디스크의 32비트 ntdll 에는 **쪽지가 비어 있다.** 누가 채우나:
  - 통역사 `Wow64LdrpInitialize +0x258 CpuNotifyMapViewOfSection(32비트 ntdll)` → `CpupResolveReverseImports +0xb2` 가 `BTCpuGetBopCode()` 값 (= 문 주소) 을 쓴다. `.mrdata` 라 `NtProtectVirtualMemory` 로 잠깐 열고 쓴다 (run138 해설 2.12).
- run138 의 차례로는 통역사가 **쪽지를 먼저 채우고** (`+0x258` 쪽 알림 안에서) 나중에 문을 연다 (`+0x11d`). 그러니 32비트 직원이 들어올 땐 쪽지가 이미 적혀 있다.
- 비유: 벽에 붙은 "문은 여기" 쪽지는 **인쇄될 때 빈칸** 이다. 통역사가 사무실을 꾸밀 때 연락처 칸 자물쇠를 열고 그 빈칸에 문 위치를 적어 넣는다.
- 에뮬레이터 입장 `[추측]`: 진짜 wow64 를 돌리면 저절로 채워진다. 반대로 wow64 를 흉내 내면서 이 칸을 안 채우면, 칸 값 0 으로 점프하게 될 것이다 (파일 값 0 + `JMP [칸]` 한 줄에서 나온 짐작).

**예외 하나 — `NtQueryInformationProcess` 만 `fs:[0xc0]` 를 쓸 수 있다** `[정적]`

- 32비트 ntdll `.text` 전체에서 `fs:0xc0` 를 쓰는 명령은 `NtQueryInformationProcess +0x11` (rva `0x79d21`) **단 하나** 다 (결과서 objdump 훑기).
- 그 스텁 (37 B) 은 이렇게 갈린다:
  1. `mov eax,0x19` → `call $+5` · `pop edx` 로 **자기 코드 주소** 를 얻는다.
  2. `cmp byte [edx+0x14],0x4b` — 같은 함수 안에 박혀 있는 dword `0x4b280000` (파일 기준 주소, 재배치 대상) 의 **윗바이트** 를 본다.
  3. 32비트 ntdll 이 윗바이트 `0x4b` 인 주소에 앉았으면 → **`call fs:[0xc0]`** (TEB32 `+0xc0`).
  4. 아니면 → 다른 스텁처럼 `mov edx,&_Wow64SystemServiceCall · call edx`.
- 두 길 모두 결국 **같은 문 주소** 다: `Wow64Transition` 은 `CpupResolveReverseImports +0xb2`, TEB32 `+0xc0` 은 `CpuThreadInit +0x32` 가 같은 `BTCpuGetBopCode()` 값을 쓴다 (run138).
- 왜 이 한 스텁만 이렇게 갈리는지는 **모른다** `[공백]`.
- 2.9 의 6번처럼 이 스텁은 32비트 세팅 담당의 **첫 syscall** (쿠키 조회, `+0x5e`) 이다. 그래서 에뮬레이터가 32비트 ntdll 을 **선호 기준 주소 (`0x4b280000`)** 에 올리면, **TEB32 `+0xc0` 도 채워져 있어야** 한다 (진짜 wow64 `ThreadInit` 이 채움, run 문서 AR07).
- 실제로 어느 주소에 올라오는지는 Q265 9번.
- 비유: 거의 모든 직원은 벽 쪽지를 보는데, **한 사람만** "사무실이 원래 자리에 있으면 내 개인 안내판 메모 (TEB32 `+0xc0`) 를 본다" 는 버릇이 있다. 둘 다 같은 문을 가리키니 결과는 같다. 그 버릇의 이유는 모른다.

### 2.12 32비트 `NtContinue` 는 어디로 가나 — 64비트 `NtContinue` 가 아니다 (B4)

64비트 쪽부터 떠올려 보자 (run29 해설 2.3): 64비트 `NtContinue` 는 관리사무소 창구에 "**제 출발 메모를 이걸로 바꿔 주세요**" 하고 내면, 관리사무소가 출발 메모를 덮어쓰고 `IRETQ` 로 보내 버린다. **돌아오지 않는다.**

32비트 `NtContinue` 는 관리사무소에 가지 않는다. 길을 따라가 보자 `[정적]` (wow64 · wow64cpu `.asm`).

1. **스텁 (칸 `0x43`) → 문 → 통역사 창구 → `whNtContinue`** (wow64 rva `0x1d410`)
   - `+0x4 DL = [RCX+4]` (TestAlert), `+0x7 ECX = [RCX]` (32비트 메모) → `+0x9 → +0xe Wow64NtContinue`.
2. **`Wow64NtContinue +0x3f → +0x44 CpuSetContext`** → `+0x2d BtFuncs+0x88` = wow64cpu **`BTCpuSetContext`** (rva `0x1010`) — **메모판 (CPU 영역) 에 새 메모 적기**.
   - 지금 스레드 (`+0x49 CMP RBX,-2`) 이고 메모판 비트 0 이 켜져 있으면 (`+0x54`) → `RtlCopyContext` 로 **바로 덮어쓴다** (`+0xa0`).
   - 꺼져 있으면 → 64비트 **`NtSetInformationThread(−2, 0x1d, 문맥, 0x2cc)`** (`+0x59 ~ +0x6a`, ret `+0x71`). `0x2cc` 는 run138 해설 2.11 의 x86 CONTEXT 크기와 같은 숫자다.
   - 32비트 코드가 syscall 중일 땐 비트 0 이 꺼져 있다 (`RunSimulatedCode +0x2f BTR` 가 32비트로 들어갈 때 끔, run138) → **보통 `NtSetInformationThread(0x1d)` 쪽** `[정적 추론]`.
3. **TEB64 `+0x14b8` (`TlsSlots[7]`) 목록 보기** (`+0x97`)
   - 이 32비트 메모를 가진 **틀** 이 목록에 있으면 (`+0xa7 CMP [RBX+0x10],R13`) → 그 틀의 64비트 메모로 **64비트 `NtContinueEx`** (`+0x1e6 → +0x1ed`).
   - 목록이 비었으면 (`+0xa1 JZ → +0x205`) → TestAlert 일 때 **64비트 `NtTestAlert`** (`+0x20a → +0x211`), 그리고 `+0x216` 에서 메모의 `Eax` 를 돌려준다 → 32비트로 복귀.
4. 덤: 32비트 **`NtContinueEx`** 스텁 (칸 `0xa5`) 은 다르다. `whNtContinueEx +0x59 → +0x60` 로 **늘** 64비트 `NtContinueEx` 를 부른다.

**정리** — run 문서 그대로

- 32비트 `LdrInitializeThunk +0x24` 의 `NtContinue` = "**메모판에 새 메모 적기 (+ 보통 `NtSetInformationThread(0x1d)`) + `NtTestAlert`**" 다 `[정적]`.
- 64비트 `NtContinueEx` 는 `+0x14b8` 목록에 틀이 있을 때뿐이다 `[정적]`.
- 첫 스레드 때 그 목록이 비어 있나 → `[공백]`. 결과서가 wow64 `.text` 에서 `+0x14b8` 에 **넣는** 줄을 못 찾았다 (쓰는 줄은 `Wow64NtContinue +0xca` · rva `0x20723` 둘). 그래서 처음엔 0 일 것 `[추측]` (Q265 · Q266).
- 그러면 그 뒤 32비트로 다시 들어갈 때 메모 = **사본의 처음 값** (`Eip` = 32비트 `RtlUserThreadStart`, `Eax` = 진입점, `Ebx` = PEB32 — run139 첫 x86 메모) 으로 이어진다 `[추측: run138 · run139 이어 읽기]` (Q265).

**비유로**

- 64비트 직원: 관리사무소 창구에 "출발 메모를 이걸로 바꿔 주세요" → 관리사무소가 바꿔 주고 **순간 이동** 시킨다.
- 32비트 직원: 문 밖 통역사에게 "**메모판의 메모를 이걸로 바꿔 주세요**" → 통역사가 메모판을 바꿔 끼우고 (필요하면 관리사무소에 "이 직원 32비트 메모 바꿨어요" 신고 = `NtSetInformationThread(0x1d)`), 쪽지 배달 확인 (`NtTestAlert`) 을 한 뒤, 문이 **새 메모대로** 직원을 다시 들여보낸다.
- 그러니 에뮬레이터 커널이 보는 건 64비트 `NtContinue` 가 아니라 **64비트 `NtSetInformationThread(0x1d)` · `NtTestAlert`** 다 (run 문서 AR06). "32비트 초기화 끝" 은 그 둘과 문 재진입으로 잡아야 한다 `[정적 추론]`.

### 2.13 디스패처 셋 · `RtlUserThreadStart` 입구 — 짧게 (B3 · B4)

**새 말 — 디스패처 (`KiUser*Dispatcher`)**: 예외 · APC (나중에 처리할 쪽지) · 콜백이 생겼을 때 직원을 **보내는 유저 쪽 접수처** 로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 64비트판은 run81 · run82 에서 봤다. 비유: 사고 · 쪽지 · 부탁 전화가 오면 직원이 들르는 **안내 창구 셋**.

| 함수 (rva) | 스택에서 받는 것 | 하는 일 → 끝 |
|---|---|---|
| `KiUserExceptionDispatcher` (`0x7bf70`) | `[Esp]` 예외 기록, `[Esp+4]` 메모 | `RtlDispatchException` → 처리됨 `NtContinue(메모, 0)` / 아니면 `NtRaiseException` |
| `KiUserApcDispatcher` (`0x7be40`) | `[Esp]` APC 루틴, `[Esp+4 ~ +0xc]` 인자 셋, `[Esp+0x10]` 비트 0, **메모는 `Esp+0x14`** | 루틴 부르기 (`+0x34`) → `NtContinue(메모, 플래그)` |
| `KiUserCallbackDispatcher` (`0x7bf20`) | `[Esp]` 건너뜀, **`[Esp+4]` 번호** | PEB32 `+0x2c KernelCallbackTable` 의 그 번호 칸을 부름 → `NtCallbackReturn(0, 0, EAX)` |
| `RtlUserThreadStart` (`0x7c000`, 13 B) | **`EAX` = 시작 주소, `EBX` = 인자** | `[Esp+4] ← EAX` · `[Esp+8] ← EBX` → `JMP __RtlUserThreadStart@8` |

- 모두 `[정적]`.
- 맨 아래 줄이 run139 와 맞물린다: 커널이 첫 x86 메모에 **`Eax` = 진입점 · `Ebx` = PEB** 를 놓았다 (run139 해설 2.10). 32비트 `RtlUserThreadStart` 는 그 두 레지스터를 **스택 칸 둘로 옮겨** 다음 함수에 넘긴다. 레지스터로 받은 걸 stdcall 꼴로 바꿔 넘기는 셈이다.
- 디스패처 셋의 스택 틀을 **누가 쌓나** 는 통역사 쪽 (`Wow64PrepareForException` · `Wow64ApcRoutine` · `Wow64KiUserCallbackDispatcher`) 으로 보이지만 `[추측]` — 이번엔 안 따라갔다 (Q266).

### 2.14 이름 8개 — 내보내기 표에 다 있다, 칸 번호 하나 바로잡기 (B5)

run139 표 B-0 의 8개 이름이 32비트 ntdll 내보내기 표에 다 있는지 맞춰 봤다 `[정적: PE]`.

| 벽 주소록 `+` | 수첩 `+` | 이름 | 32비트 ntdll rva |
|---|---|---|---|
| `+0x00` | `+0x18` | `LdrInitializeThunk` | `0x34230` |
| `+0x08` · `+0x10` · `+0x18` | `+0x20` · `+0x28` · `+0x30` | `KiUserExceptionDispatcher` · `KiUserApcDispatcher` · `KiUserCallbackDispatcher` | `0x7bf70` · `0x7be40` · `0x7bf20` |
| `+0x20` | `+0x38` | **`RtlUserThreadStart`** | `0x7c000` |
| `+0x28` | `+0x40` | `RtlpQueryProcessDebugInformationRemote` | `0xd1470` |
| `+0x30` | `+0x48` | (이름 없음 — 32비트 ntdll 기준 주소) | — |
| `+0x38` | `+0x50` | **`LdrSystemDllInitBlock`** | `0x13d2a0` |
| `+0x40` | `+0x58` | `RtlpFreezeTimeBias` | `0x139798` |

- 8개 모두 있다 (전달 내보내기 아님). run139 표 B-0 과 칸 · 이름 · 순서가 **모두 같다**.
- **`[4]` = 32비트 `RtlUserThreadStart`** 가 확정됐다 `[정적: PE]`.
- **바로잡기** (결과서 파일은 안 고침, run138 문서 맨 위 "고쳐 읽기 (run140)" 에 들어감):
  - sast/136 결과서 "다음 요청 후보 1 ④" 는 수첩 `+0x48` 을 `[6]`, `+0x50` 을 "`[7]` 미사용" 으로 적었다.
  - 바로: `+0x48` = **이름 없는 기준 주소** (이름 표 줄이 아님), **`+0x50` = `LdrSystemDllInitBlock`**, `+0x58` = `RtlpFreezeTimeBias`.
  - 통역사가 읽는 `+0x18 ~ +0x38` · `+0x48` 은 그대로 맞다. 틀린 걸 단정한 run 은 없다.
- 비유: 벽 주소록에 한 칸 (`+0x30`) 만 이름 대신 **사무실 주소** 가 적혀 있어서, 그 뒤 칸 번호를 하나씩 밀려 셌던 것이다.

### 2.15 비유로 처음부터 끝까지 한 번 더 (이번 바퀴만)

1. 문 (wow64cpu) 이 **정식 입장** (`IRETQ`, 출입증 `0x23`) 으로 직원을 옛날식 사무실에 들여보낸다. 도착한 곳은 옛날식 관리실 **오리엔테이션실 입구** (32비트 `LdrInitializeThunk`).
2. 직원은 책상 위 쪽지 두 장을 집는다: **메모 사본** (`[Esp+4]`) 과 **옛날식 관리실 주소** (`[Esp+8]`). 맨 위 칸 (`[Esp]`, 돌아갈 곳) 은 안 본다 — 불려 온 게 아니니까.
3. 안내 데스크 → 중간 방 → 갈림길 방. 칠판을 0 → 1 로 바꾼 첫 직원이 **세팅 담당** 을 부른다.
4. 세팅 담당은 개인 안내판 뒷장의 "−0x2000" 을 보고 앞장으로 넘어가, 10번 칸에서 **통역사 메모 칸** 을 찾아 "쪽 크기 0x1000" 을 읽는다. (0 이 적혀 있으면 여기서 영원히 접고 있다.)
5. 옛날식 **공용 창고** (`\KnownDlls32`) 를 열고, 옛날식 직원용으로 **창고 주소를 바꿔 적은** 환경 변수를 만든다.
6. 명부에 관리실 · 가게를 적고, kernel32 를 들이고, import 를 올려 잇고, **출근 인사를 미리 한 번** 하고, 전원을 켜고, **TLS 첫 안내** 까지 한다. 64비트 쪽엔 없던 "통역사에게 넘기기" 단계가 없으니 **끝까지** 간다.
7. "끝났어요" 하고 돌아와, 오리엔테이션실 입구가 `NtContinue(메모 사본, 1)` 을 낸다.
8. 그 부탁은 관리사무소가 아니라 **문 밖 통역사** 에게 간다 — 벽의 "문은 여기" 쪽지 (`Wow64Transition`, 통역사가 미리 채워 둔 것) 를 보고 문을 지나서.
9. 통역사는 **메모판의 메모를 사본으로 바꿔 끼우고**, 쪽지 배달 확인을 한 뒤 직원을 다시 들여보낸다. 직원은 메모대로 **본업 자리** (32비트 `RtlUserThreadStart`) 로 간다 `[추측]` (2.12).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

그림은 안 바뀌었다 (그림 1 · 2 에 WoW64 노드가 없다). 바뀐 건 글이다 (`git show 845ed70 -- runs/03_sprint/flow.md`).

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (PDB 줄) | 32비트 ntdll 줄 없음 | **`wntdll.pdb`** 새 줄 (처음 analyze, 하드링크 `ms/bin/ntdll32.dll` 로 모듈 이름 `ntdll32`) |
| 기준 근거 줄 | — | run140: 새 사진 읽기 없음, test.exe 가 이 갈래를 안 탄다는 앞 run 의 줄만 다시 짚음 |
| 기준 정적 근거 메모 | — | x86 PDB 이름 꼬리 (`_LdrpInitialize@8` = 64비트 `LdrpInitialize`), 주소 `0x4b2b…` = 파일 기준 |
| 2.1 `PspPrepareSystemDllInitBlock` 표 B-0 줄 아래 | 이름 8개 (run139) | 8개 모두 32비트 ntdll 내보내기에 있음 `[정적: PE]`, 수첩 `+0x50` = `LdrSystemDllInitBlock`, 32비트 ntdll 은 자기 수첩 `+0x18 ~ +0x97` 을 안 읽음 |
| 3장 run138 절 머리 | 32비트 쪽은 "다른 바퀴" | "아래 셋째 절 (run140)" |
| 3장 run138 절 나무 | `──▶ 32비트 ntdll (Q253c)` | `──▶ 32비트 ntdll (run140 절)` |
| 3장 run138 절 "문맥을 넘긴다" 줄 | `[Esp+4]` · `[Esp+8]` 은 놓는 쪽만 | + **받는 쪽도 맞다** `[정적]` (`+0x8 ECX = [EBP+8]` · `+0x5 EDX = [EBP+0xc]`) |
| 3장 run138 절 `Wow64Transition` 줄 | 쓰는 쪽만 | + **읽는 쪽** `[정적]`: 스텁 508개 → `JMP [Wow64Transition]` (파일 값 0), `fs:[0xc0]` 는 `NtQueryInformationProcess` 하나 |
| 3장 끝 (run139 절 뒤) | 없음 | **새 절 "WoW64 (x86) 초기화 — 32비트 ntdll"** (나무 + `[정적]` 줄: 입구 · `UseWOW64` 없음 · 읽는 칸 · TEB 쌍둥이 칸 · 스텁 · 디스패처 · `NtContinue` 길 · 이름 8개 · PE_emulator-AR) |
| 4장 run138 "정적으로 남은 것" 줄 | 후보 1 (32비트 ntdll) 은 Q253c 감 | **(해결됨) run140**, `whNtContinue` → `Wow64NtContinue` → `CpuSetContext` 길도 run140 이 읽음 |
| 4장 새 줄 | — | Q253c 닫힘 줄, run140 남은 것 셋 (동적 Q265 · 정적 Q266 · PE_emulator-AR) |
| 5장 | — | run140 줄 둘 (근거 run · 요청서 번호) |

- 짐 지도 (`baggage.md`) 는 안 건드렸다. test.exe 초기화 밖이다. (WoW64 짐을 짐 지도에 넣을지는 run139 때부터 사용자 결정 Q264 로 대기 중.)
- **다른 run 문서**: run138 맨 위에 "고쳐 읽기 (run140)" 한 줄 (2.3 · 2.11 · 2.14). run139 는 고칠 줄 없음.

### 큰 그림에서 어떻게 이어지나 — AR01 세 바퀴를 한 장으로

**세 조각이 다 찼다.** 바퀴 순서는 C (run139) → A (run138) → B (run140) 였지만, **시간 순서** 로 다시 놓으면 이렇다.

```
① 커널이 짓는다 (run139)
   'WowP' · PEB32 (+0x490 뒤 = Wow64Info 자리) · 32비트 서류 · 32비트 수첩
   TEB32 = TEB64 + 0x2000  (앞장 +0x180c = +0x2000 · 뒷장 +0xfdc = −0x2000)
   CPU 영역 (메모판) 에 첫 x86 메모: Eip = 32비트 RtlUserThreadStart · Eax 진입점 · Ebx PEB
   → 직원은 평소처럼 64비트 (CS 0x33) 로 출발
        ▼
② 64비트 ntdll (run17 ~ run19 · run138)
   LdrInitializeThunk → … → LdrpInitializeProcess
   +0x1766 UseWOW64 (앞장 +0x180c > 0) → +0x17c6 LdrpLoadWow64 → +0x17fc 통역사 호출   ✗ 안 돌아옴
        ▼
③ wow64 = 통역사 (run138)
   Wow64Info[0] = 0x1000 · TEB64 +0x14d0 · 안내판 4장 · 문 달기
   Wow64Transition 칸 채우기 (파일 속 0 → 문 주소) · TEB32 +0xc0
   Eip ← 수첩 +0x18 · [Esp+4] 메모 사본 · [Esp+8] 32비트 ntdll 기준
        ▼
④ wow64cpu = 문 (run138)
   BTCpuSimulate → RunSimulatedCode  IRETQ (CS 0x23) ═▶ 32비트   (◀═ JMPF CS 0x33 고리)
        ▼
⑤ 32비트 ntdll (이번 run140)
   LdrInitializeThunk([Esp+4], [Esp+8]) → … → LdrpInitializeProcess
     뒷장 +0xfdc < 0 → 앞장 +0x14d0 → Wow64Info[0]   (0 이면 헛도는 고리)
     \KnownDlls32 · kernel32 · import · 출근 인사 · 전원 · TLS                         ✓ 돌아옴
   +0x24 NtContinue(사본, 1) ⇢ 통역사 CpuSetContext (메모판 바꾸기) · NtTestAlert
   (바깥 부탁은 모두 Nt* 스텁 → JMP [Wow64Transition] → ④ 문 → ③ 통역사 → ② 의 Nt* → 커널)
        ▼  (문이 메모판의 사본대로 다시 들여보냄 [추측])
⑥ x86 exe
   32비트 RtlUserThreadStart (EAX 진입점 · EBX PEB) → __RtlUserThreadStart → … → 진입점
   (__RtlUserThreadStart 뒤는 아직 — Q266 ③)
```

- 각 바퀴가 **쓴 칸** 과 다음 바퀴가 **읽는 칸** 이 줄마다 맞물린다 (run 문서 "앞 run 과 이어 읽기" — 모두 맞음). 이번에 맞물린 곳만 다시 모으면:
  - ③ 의 `[Esp+4]` · `[Esp+8]` · `Eip` ↔ ⑤ 의 입구 (2.3).
  - ① 의 뒷장 `−0x2000` · ③ 의 `+0x14d0` · `0x1000` ↔ ⑤ 의 `+0x409 ~ +0x430` (2.7).
  - ③ 의 `Wow64Transition` · TEB32 `+0xc0` ↔ ⑤ 의 스텁 (2.10 · 2.11).
  - ① 의 첫 x86 메모 `Eax` · `Ebx` ↔ ⑥ 의 `RtlUserThreadStart` (2.13).
  - ① 의 이름 8개 ↔ ⑤ 의 내보내기 표 (2.14).
- **test.exe 지도와는 닿지 않는다.** 모두 test.exe 가 안 타는 옆 갈래다. 그래서 test.exe 쪽 줄 (run15 ~ run19 · run29) 은 고친 게 없다. 비교 대상으로만 썼다.

**에뮬레이터 (PE_emulator-AR) 에게 뜻하는 것** (run 문서 표 — `../PE_emulator-AR` 코드는 안 읽음, 고칠지는 사용자 결정)

- **AR03**: PEB32 `+0x10` 필수 (0 이면 바로 죽음). PEB32 바로 뒤 `Wow64Info[0] = 0x1000` (wow64 를 흉내 내면 직접). TEB32 `+0xfdc` 는 음수 `−0x2000`. 32비트 수첩은 32비트 ntdll 이미지 안 (`Size 0x128` 은 이미 있음), 새로 쓸 칸 목록은 run 문서 표.
- **AR04**: TEB32 `+0x0` · `+0x18` · `+0x30` · `+0xfca` (0) · `+0xfdc`, **TEB64 도 4GB 아래**.
- **AR06**: 32비트 `LdrpInitializeProcess` 는 **돌아온다**. 그 끝의 `NtContinue` 는 에뮬레이터가 보는 64비트 `NtContinue` 가 아니라 `NtSetInformationThread(0x1d)` · `NtTestAlert` 로 보인다.
- **AR07 이후**: 초기화 중 syscall 목록 (`NtQueryInformationProcess(0x24)` · `NtQuerySystemInformation` · `NtCreateEvent` · `NtWow64GetNativeSystemInformation` · `NtOpenDirectoryObject(\KnownDlls32)` · `NtOpenSymbolicLinkObject` / `NtQuerySymbolicLinkObject` · `NtClose`) — 모두 `whNt*` 를 거친다. 그중 `KnownDllPath` 링크 열기 · 읽기가 실패하면 초기화가 죽는다 (`+0xa3e JGE` 아니면 `+0xa5d` → `+0x4a0`). 32비트 ntdll 을 선호 기준에 올리면 **TEB32 `+0xc0` 도 필요**.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 32비트 `LdrInitializeThunk` 첫 명령 때 `esp` · `[esp+4]` · `[esp+8]` 실제 값, 세그먼트, `fs:[0xfdc]` 가 `0xffffe000` 인가 | 실행 때 값 | **Q265 1** (동적): `ntdll32+0x34230` |
| 사슬이 정말 그렇게 가나, **32비트 `LdrpInitializeProcess` 가 정말 돌아오나** | 실행 때 | Q265 2 (`+0x34241` 에 오고, `+0x34259` 는 안 걸려야 함) |
| `Wow64Info[0]` 실제 값 (`0x1000`?), TEB64 `+0x14d0` | 실행 때 | Q265 3 (`ntdll32+0xc9772`) |
| 32비트 수첩 실제 값 | 실행 때 | Q265 4 |
| `Wow64Transition` · TEB32 `+0xc0` 실제 값, **`call fs:[0xc0]` 가 걸리나** | 실행 때 | Q265 5 |
| 환경 변수가 실제로 바뀌나 | 실행 때 | Q265 6 |
| **첫 `NtContinue` 때 TEB64 `+0x14b8` 목록이 비었나**, `NtContinueEx` / `NtSetInformationThread(0x1d)` 중 무엇을 타나, 메모판 비트 0 | 넣는 줄을 정적으로 못 찾음 `[공백]` / 실행 때 | Q265 7 · **Q266 ①** |
| 디스패처 셋 입구의 실제 스택 | 예외 · APC · 콜백이 날 때만 | Q265 8 |
| **32비트 ntdll 이 `0x4b280000` 에 앉나 `0x7…` 인가** | 실행 때 | Q265 9 (`lm`) |
| WinDbg 가 32비트 ntdll 을 어떤 모듈 이름으로 보이나 | 정적만 | Q265 (먼저 `lm` 으로 확인) |
| `NtSetInformationThread(0x1d)` 의 **커널 쪽** (메모판 비트 0 을 켜나), TEB64 `+0x14b8` 목록에 **틀을 넣는 곳**, 디스패처 스택을 누가 쌓나 `[추측: wow64 쪽]` | 이번 범위 밖 (커널 · wow64 본문) | **Q266 ①** (정적) |
| 32비트 초기화가 부르는 `whNt*` 본문 (`whNtQuerySystemInformation` · `whNtWow64GetNativeSystemInformation` · `whNtQueryInformationProcess` · `whNtOpenDirectoryObject` 등) | 이번엔 스텁 꼴까지만 | **Q266 ②** |
| 32비트 `__RtlUserThreadStart` → `BaseThreadInitThunk` → x86 exe 진입점 (⑥ 의 뒤) | 이번엔 `RtlUserThreadStart` 입구까지만 | **Q266 ③** |
| 32비트 `LdrpInitializeProcess` 뒤쪽 본문 (kernel32 · import 가 `SysWOW64` · `\KnownDlls32` 와 얽힌 곳), `NtQueryInformationProcess` 스텁이 왜 갈리나 `[공백]` | 이번엔 줄기 · 읽는 칸까지만 | Q266 안에서 낮은 순위 |
| 그 밖 `[공백]`: `\KnownDlls32` 열기 실패 때 상태 반환 규칙, `+0x14bc ~ +0x14cb` 조건 칸 `[ESI+0x5c]` 뜻, `InitialThread` 비트를 커널이 켜나, `KiFastSystemCall` 을 부르는 다른 코드 | 범위 밖 | run 문서 "아직 확인 필요" |

- 동적 9개 (Q265) 는 **x86 시험 exe** 가 있어야 한다. 사용자 결정이고, Q253d · Q260 · Q262 의 VM 과 합쳤다.
- 32비트 모듈 멈춤점은 `bp <32비트 ntdll 기준>+rva` 꼴이고, 32비트 레지스터는 `.effmach x86` 로 본다 (run 문서).

---

## 5. 더 읽기

- run 문서: [`run140-wow64-ntdll32.md`](run140-wow64-ntdll32.md) — 호출 지점 표 전체, 입구 사슬 표, 읽는 칸 표 (TEB32 · TEB64 · PEB32 · 매개변수 · 자기 수첩 · KUSD), 스텁 바이트 표, 디스패처 표, 이름 8개 rva 표, 앞 run 과 이어 읽기, flow-writer 대조, 정정, PE_emulator-AR 표
- 결과서: [`_workspace/sast/137_wow64-ntdll32_result.md`](../../_workspace/sast/137_wow64-ntdll32_result.md) — "먼저 읽는 한 줄 답", 2절 전체 칸 표, 3절 `NtQueryInformationProcess` 바이트, 동적 9개 멈춤점, 다음 요청 후보, 스크립트 문제 (모듈 이름 겹침 · 하드링크 우회)
- 요청서: [`_workspace/sast/137_wow64-ntdll32.md`](../../_workspace/sast/137_wow64-ntdll32.md)
- flow.md: [3장 "WoW64 (x86) 초기화 — 32비트 ntdll"](flow.md) (run139 절 뒤), 3장 run138 절의 run140 줄 둘, 2.1 `PspPrepareSystemDllInitBlock` 표 B-0 아래 run140 줄, 4장 run140 줄
- 앞 해설:
  - [run138 해설](run138-wow64-dlls-해설.md) (통역사 · 문 · 쪽지 · 안내판 · 메모판 · 돌아오지 않는 통역사)
  - [run139 해설](run139-wow64-kernel-해설.md) (옛날식 안내판 · 두 장짜리 개인 안내판 · 첫 x86 메모 · 표 B-0)
  - [run16 해설](../02_sprint/run16-thunk-context-해설.md) (오리엔테이션실 · 사물함 메모), [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) (방 넷 · 꼬리 점프 · 칠판), [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) (`UseWOW64` 갈림 값), [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) (뒤쪽 10묶음 · 간접 호출 · 팻말), [run29 해설](../02_sprint/run29-continue-start-해설.md) (64비트 `NtContinue` · 스텁 `0x43`), [run32 해설](../02_sprint/run32-params-initblock-해설.md) (업무 수첩)
- 원 요청: `../PE_emulator-AR/_workspace/00_planner/AR01_internals-wow64.md` B 절

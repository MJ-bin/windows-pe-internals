# run59 init-leftovers — 해설

> 이 글은 [run59 문서](run59-init-leftovers.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

> **고쳐 읽기 (run123)**: 2.12 ⓗ 의 "**12~13·24~25 의 2 는 입력 표가 갖고 온 값**" (출처 `[공백]`) 은 틀림 — 호출한 쪽 `PspAllocateProcess` 가 그 자리에서 쓴다 (비트 12~13 = `+0x1e03 ~ +0x1e15` 무조건, 비트 24~25 = `+0x1e1d ~ +0x1e30` `KeIsCoreIsolationMitigationPolicyEnforceable` 거짓일 때). 4절의 "PDB 에 옵션 열거형이 없음" 도 틀림 — PDB `PS_MITIGATION_OPTION` 이 있다 (32 ~ 39 이름은 run123). 4절 "만들기 정보 `+0x1d8` 을 **읽는** 함수" 는 닫힘 — `PspCaptureProcessParameters +0x26` · `PspDeleteCreateProcessContext +0x11a`. 본문은 그대로 두었다 → [run123 해설](run123-run58-60-leftovers-해설.md) 2.6 · 2.7 · 2.11

- 대기열: **Q109 나머지 (B·C·D (1)~(3)·E)** — PE_emulator 요청 4-B~E: "공사 중 빈 함수" 본문 · 커널 비트 뜻 · 기록 정정 (`_workspace/plan.md`, 이번에 **Q109 전체가 끝**)
  - Q109 의 첫 일 ⓠ (레지스트리 글자) 는 run58 에서 끝났다. 이번이 나머지 전부다.
  - 다음 항목: **Q111** (PE_emulator 요청 5 — 함수표·`.mrdata` 보호) → Q104 VM 단계 → Q78d4 → …
  - 남은 질문이 간 곳: 정적으로 남은 것 → 새 **Q113**, 동적 1~10 → 새 **Q114** (그중 3 은 **Q79** 에도), 깃발 둘의 실행 값 → 원래 있던 **Q43** (4절).
- 근거:
  - run 문서 [`run59-init-leftovers.md`](run59-init-leftovers.md) (가장 중요)
  - 요청서 [`_workspace/sast/54_init-leftovers.md`](../../_workspace/sast/54_init-leftovers.md) (17문항, 순서 D → B → C → E)
  - 결과서 [`_workspace/sast/54_init-leftovers_result.md`](../../_workspace/sast/54_init-leftovers_result.md) (판정: **분석성공**, PDB 일치, 17문항 모두 답 — 부분 답 둘)
  - flow.md 변경: 기준 · 2.1 (`PspAllocateProcess`·`MmCreatePeb`·`PspSetupUserProcessAddressSpace`·`PspPrepareSystemDllInitBlock`) · 2.4 (`LdrpInitialize`·`_LdrpInitialize`·`LdrpInitializeProcess` 묶음 2·run51 쿠키 절) · 4장 · 5장. **그림은 안 바뀜**
  - baggage.md 변경: 2장 숫자 · 3장 새 줄 27 (3.1 · 3.2 · 3.8 · 3.9) · 3.11 `Cookie` 줄 정정 · 새 7.19 (열다섯째판)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - `[관찰]` 로 적힌 값은 run44 사진 (K1) 을 **다시 읽은 것** 이다. 새 실행이 아니다.
  - 질문 17 (환경 변수 이름) 만 run44 의 K1 **덤프 원본**을 서버에서 읽기 전용으로 열어 읽었다.
- 결과서와 run 문서가 다른 곳은 **2.14 한 절에 모았다**. 이 글은 **run 문서 숫자**를 따른다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 오프셋 순서 ≠ 실행 순서 → [run9 해설 3.7](../02_sprint/run9-create-user-process-해설.md#37-오프셋이-큰데-먼저-실행되는-줄--오프셋-순서--실행-순서)
- 한 주소에 이름 여럿 (ICF 별칭) → [run9 해설](../02_sprint/run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭)
- `LdrpInitialize` 의 잡일 셋, 두 깃발, `LOCK CMPXCHG` 로 "첫 스레드" 가리기, 30ms 씩 쉬며 기다리기 → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.5 · 2.6
- 꼬리 점프 → [run17 해설 2.4](../02_sprint/run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리)
- 보안 쿠키 (`__security_init_cookie`) = 택배 상자 **봉인 스티커** 무늬 → [run30 해설](../02_sprint/run30-exe-crt-start-해설.md) 2.5 "새 말 — 보안 쿠키"
- 프로세스 쿠키 `EPROCESS.Cookie`, "게으른 만들기" (처음 달라고 할 때 깎아 주는 **열쇠**) → [run51 해설](run51-eprocess-later-해설.md) 2.8 (단, 그 절의 "첫 호출" 은 이번에 정정 — 2.14)
- 정보 클래스 (창구 서류 번호) → [run51 해설 2.2](run51-eprocess-later-해설.md#22-새-말-정보-클래스)
- `.mrdata` = **유리 덮개를 씌운 칠판**, ntdll 전역 = 관리실 **칠판** → [run43 해설 2.9](run43-baggage-checklist-해설.md) "구역 새 말 둘"
- `LdrSystemDllInitBlock` = 관리실 **업무 수첩**, 완화 옵션 = 가게 **안전 규칙** → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- PEB 견본·`BitField`, leap second (윤초), silo (컨테이너 같은 격리 칸) → [run10 해설](../02_sprint/run10-allocate-process-해설.md)
- KUSD = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- `ExtendedFeatureDisableMask` 를 "해당자만 기입" 칸이라 못 가른 이야기 → [run52 해설](run52-kthread-create-해설.md) 2.8
- ETW (커널의 사건 기록 장치 `[추측]`) → [run12 해설](../02_sprint/run12-insert-thread-해설.md)
- 사진 K1 · P1 · P1a … → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

이번에 처음 나오는 말 (`XADD`, 메모리 장벽, `RDRAND`, 성능 카운터, 보호 바꾸기, 포인터 섞기, 캐시, 풀림 정보, 비휘발 플러시 …) 은 **본문에서 처음 나오는 자리**에서 한 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**, 프로세스 = **가게**, PEB = 가게 **안내판**.
- ntdll 전역 = 관리실 **칠판**. `.mrdata` 칠판엔 **유리 덮개**가 씌워져 있다.
- 시스템 콜 = 관리사무소 **신청 창구**.
- run 문서가 이번에 쓴 그림도 같이 쓴다 (관리실이 문을 열 때 하는 잡일 셋):
  - **보안 쿠키 정하기** — run 문서는 "금고 비밀번호", run30 해설은 "봉인 스티커 무늬" 라고 불렀다. 같은 것이다. 이 글은 **봉인 스티커** 로 쓴다.
    - run 문서의 "잠긴 서랍" = 이 글의 **유리 덮개 칠판** (`.mrdata`).
  - **비상구 지도 표시** (특수 기계 프레임)
  - **장비 점검표** (CPU 기능 바이트)
- 그리고 쿠키가 **둘** 나온다. 헷갈리기 쉬워서 처음부터 갈라 둔다 (2.3 에서 자세히):
  - **봉인 스티커** = ntdll 이 스스로 정하는 `__security_cookie`
  - **열쇠** = 관리사무소가 가게마다 깎아 주는 `EPROCESS.Cookie` (run51 해설의 열쇠 그대로)

### 한 줄로 먼저 — 무엇을 얻었나

| 묶음 | 질문 | 답 (run 문서 그대로) |
|---|---|---|
| **B** | `InitSecurityCookie` 는 syscall 을 부르나 | 본문의 syscall 은 기다리는 스레드의 `NtDelayExecution` 하나뿐. **쿠키를 만드는 syscall 셋은 callee 안**이다: `LdrpGenSecurityCookie` 안 `NtQueryPerformanceCounter`, `LdrInitSecurityCookie` 안 `NtProtectVirtualMemory` ×2 `[정적]` |
| B | `RtlEncodePointer` 는 쿠키를 어디서 얻나 | 캐시 칸 `CookieValue` (`0x1d2408`). **0 일 때만** `NtQueryInformationProcess(0x24)` `[정적]` |
| B | 이 프로세스의 첫 `NtQueryInformationProcess(0x24)` 는 | **`LdrpInitializeProcess +0x12f` `RtlEncodePointer(0)`** — 힙 관리자 쪽이 아니다 (run51 정정) `[정적]` |
| B | `RtlpInitializeNonVolatileFlush` 는 | 시스템 정보 `0xc0` 한 번 → 성공하면 전역 넷 `[정적]` |
| B | 특수 프레임 · CPU 기능 | 표 2칸 고쳐 쓰기 · `__isa_info` 1바이트. **둘 다 syscall 없음** `[정적]` |
| D | PE_emulator 의 asm 읽기 셋 | **셋 다 맞다** (`Initialized` 쓰는 줄은 `+0x166`) `[정적]` |
| C | 커널 비트 뜻 | PEB `BitField` 비트마다 출처, `F` 비트 8 = "PEB 를 가지는 프로세스" (이름은 `[추측]`) 등 — 2.12 표 |
| E | K1 환경 변수 이름 | **47개** `[관찰]` (덤프) |

이 글에서 붙잡아 둘 세 줄:

> **(1) 보안 쿠키의 syscall 은 `InitSecurityCookie` 본문에 없다. 한 층 아래 (`LdrInitSecurityCookie`) 와 두 층 아래 (`LdrpGenSecurityCookie`) 에 있다.** PE_emulator 가 "공사 중" 함수를 채울 때 어느 함수 안에 무엇을 넣을지가 여기서 갈린다.
>
> **(2) 쿠키는 둘이다.** 봉인 스티커 (`__security_cookie`, ntdll 이 시계·난수로 스스로 만든다) 와 열쇠 (`EPROCESS.Cookie`, 커널에서 받아 `CookieValue` 칸에 복사해 둔다). 만드는 길도, 놓이는 칠판도 다르다.
>
> **(3) syscall 결과로 채우는 전역 (쿠키 캐시, 플러시 넷) 도 칸에 쓰는 줄은 ntdll 이다.** 커널은 ntdll 이 넘긴 지역 버퍼에 답을 적을 뿐이다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 한 구간이 아니라 **두 곳에 흩어진 남은 일**을 모았다. 라벨은 ret 자리 (콜스택 숫자) 다.

```
cmd.exe 의 부탁 → 커널 (부모 cmd 스레드, flow.md 2.1)
 NtCreateUserProcess
  ├─ +0xab8 PspGetProcessParameterOverrides          ← C ⓖ
  └─ +0xc36 PspAllocateProcess
       ├─ +0x2277 ~ +0x22e1  PEB 견본 BitField 만들기  ← C ⓐ (+ ⓓ 비트 8)
       ├─ +0x2314 MmCreatePeb  (+0x2b8 SecureState 검사) ← C ⓒ · ⓑ leap second
       └─ +0x239c PspSetupUserProcessAddressSpace
            ├─ +0x10b PspLocateInPEManifest           ← C ⓔ (조건부)
            ├─ +0x227 KeCopyXfdMaskToPeb              ← C ⓙ
            ├─ +0x234 PspUpdatePebForAffinityChange   ← C ⓙ
            └─ +0x263 PspMapSiloSharedDataView        ← C ⓛ
   (K1 사진 순간 = PspAllocateThread 입구 — 환경 블록 이름 47개 ← E)

test.exe 첫 스레드 (flow.md 2.4)
 LdrInitializeThunk +0xe → LdrpInitialize
   ├─ +0x46 InitSecurityCookie          ← B (이 글에서 가장 천천히)
   ├─ +0x27 InitSpecialMachineFrames    ← B
   ├─ +0x2c __cpu_features_init         ← B
   └─ ⇒ LdrpInitializeInternal → +0x5a _LdrpInitialize   (+0x10d ~ +0x13e ← D 1)
                                   └─ +0x16c LdrpInitializeProcess
                                        ├─ +0x134 RtlEncodePointer          ← B (천천히)
                                        ├─ +0x166 PebLdr.Initialized · 스핀 셋 · RtlpFcProcessManager ← D 3
                                        └─ +0x292 RtlpInitializeNonVolatileFlush  ← B (천천히)
                    … 로더 끝: LdrpInitializationComplete (ETW 갈래)  ← B 덤
 → (CRT) → main
```

- `LdrpInitialize` 의 세 줄은 오프셋이 `+0x46` · `+0x27` · `+0x2c` 라 숫자 순서가 뒤죽박죽이다.
  - 실제로 도는 순서는 **쿠키 → 특수 프레임 → CPU 기능** 이다 `[정적]` (run 문서 3절: `+0x17 JZ +0x41` 로 먼저 쿠키, 끝나면 `+0x58 JGE +0x19` 로 돌아와 특수 프레임).
  - [오프셋 순서 ≠ 실행 순서](../02_sprint/run9-create-user-process-해설.md#37-오프셋이-큰데-먼저-실행되는-줄--오프셋-순서--실행-순서) 의 또 한 예다.

### 왜 이 질문이 지금 왔나

- `../PE_emulator` 는 이 함수들을 **"공사 중 빈 함수"** 로 두고 있다 (요청서 "왜").
  - 이름과 부르는 자리는 안다. 그런데 **안에서 무엇을 쓰고, 어떤 syscall 을 부르는지** 를 모르면 채울 수가 없다.
  - PE_emulator 는 진짜 nt·ntdll 과 **같은 이름·호출관계** 로 짓는다 (CLAUDE.md). 그래서 "syscall 이 어느 함수 안에 있나" 까지 맞아야 한다.
- D 묶음은 PE_emulator 가 자기 쪽에서 asm 을 읽어 **이미 닫은 것**이다. 우리 `.asm` 으로 맞는지만 확인한다.
- 묶음이 커서 요청서는 순서를 **D → B → C → E** 로 정했다. 다 못 하면 "끝낸 것 / 못 한 것" 을 나누라고 했다. 결과는 17문항 모두 답, 부분 답 둘.

### 무엇이 궁금했나

요청서 17문항을 네 묶음으로:

1. **D 기록 정정 확인** (1~3): 동적 함수표 전역을 쓰는 곳, 시작 정보 블록 `+0x98` 을 읽는 곳, `PebLdr.Initialized`·`RtlpFcProcessManager`·스핀 횟수 쓰는 줄.
2. **B ntdll 본문** (4~7): `InitSecurityCookie` · `InitSpecialMachineFrames` · `__cpu_features_init` · `RtlEncodePointer` · `RtlpInitializeNonVolatileFlush` · `RtlGetCurrentServiceSessionId` (ETW 갈래) — 쓰는 전역, syscall, 갈래 조건.
3. **C ntoskrnl 비트 뜻** (8~16): PEB `BitField` 비트마다 출처, `+0x170` 비트 0, `F` 비트 8, 만들기 정보 `+0x10` 비트 4, `PspGetProcessParameterOverrides`, 옵션 셋째 칸, PEB `+0x138`·`+0x7c8`, silo 뷰, leap second.
4. **E** (17): K1 환경 블록 변수 **이름** 목록.

이 글은 PE_emulator 가 함수를 채우는 데 바로 쓰는 **B 를 앞에, 천천히** 다룬다. C 는 표로 짧게, D·E 는 짧게.

### 바퀴 전에 알던 것

- `[정적]` (run17, sast/12) `LdrpInitialize` 가 세 함수를 부르는 자리와 조건. 두 깃발 (`SecurityCookieInitialized`·`SpecialMachineFramesInitialized`) 을 1 로 쓰는 줄은 `InitSecurityCookie +0x42`·`InitSpecialMachineFrames +0x57` 하나씩.
  - 세 함수 **안은 안 읽었다.** 이름으로 "보안 쿠키, 특수 프레임, CPU 기능" 이라고 짐작만 했다 `[추측]` (run17 해설 2.5).
- `[정적]` (run18, sast/13) `LdrpInitializeProcess +0x12f RtlEncodePointer(0)` → `RtlpUnhandledExceptionFilter`. 묶음 2 에 "CPU 2개 이상이면 스핀 3곳".
- `[정적]` (sast/45, run51) 커널 쪽 `NtQueryInformationProcess` `0x24` 는 `_EPROCESS.Cookie` (`+0x2b8`) 를 처음 0 이면 만들어 `lock cmpxchg` 로 넣는다.
  - 그때 ntdll 쪽 첫 호출을 **힙 관리자 쪽** (`RtlInitializeHeapManager → RtlpGetModifiedProcessCookie`) 으로 적었다. → 이번에 정정.
- `[정적]` (sast/50, run50) `PspAllocateProcess +0x2248 BT EBX,8` — 꺼지면 `MmCreatePeb` 등을 건너뜀. 켜는 줄과 조건은 알았고 **뜻**은 몰랐다.
- `[정적]` (sast/27, run32) `PspSetupUserProcessAddressSpace` 가 `PspLocateInPEManifest`·`KeCopyXfdMaskToPeb`·`PspUpdatePebForAffinityChange`·`PspMapSiloSharedDataView` 를 부른다 — **본문은 안 봄**.
- `[관찰]` (run44 K1 사진) PEB `+0x138` = `3`, 시작 정보 블록 `+0xb0` = `0x02002002`, 짐 지도 3.9 `__security_cookie` 는 `[추측]` 짐작 줄 (사진 `K1 이전` → `P1~P1a` 에 바뀜).

---

## 2. 찾은 것

### 2.1 한눈에 — ntdll 쪽 사슬 (대표만)

run 문서 "호출 관계" 의 ntdll 쪽을 그대로 옮겼다. 라벨은 ret 자리, `⇒` 는 꼬리 점프.

```
LdrInitializeThunk
 └─ +0xe  LdrpInitialize
           ├─ (1) +0x46  InitSecurityCookie
           │              ├─ (1) +0x1d  LdrpGenRandom
           │              └─ (2) +0x3d  LdrInitSecurityCookie
           │                             ├─ (1) +0x7a   LdrpGenSecurityCookie → +0x92 NtQueryPerformanceCounter
           │                             ├─ (2) +0x1e5  NtProtectVirtualMemory   (풀기)
           │                             └─ (3) +0x217  NtProtectVirtualMemory   (되돌림)
           ├─ (2) +0x27  InitSpecialMachineFrames → +0x3e RtlLookupFunctionEntry (두 번)
           ├─ (3) +0x2c  __cpu_features_init → RtlIsProcessorFeaturePresent (네 번)
           └─ ⇒ LdrpInitializeInternal → +0x5a _LdrpInitialize   (+0x10d ~ +0x13e 동적 함수표 전역)
                                                └─ +0x16c LdrpInitializeProcess
                                                           ├─ (1) +0x134  RtlEncodePointer → +0x48 NtQueryInformationProcess(0x24)
                                                           ├─ (2) +0x158  LdrInitializePerThreadRng
                                                           │      +0x166 PebLdr.Initialized · +0x1cd ~ +0x203 스핀 · +0x21c ~ +0x286 RtlpFcProcessManager
                                                           └─ (3) +0x292  RtlpInitializeNonVolatileFlush → +0x36 시스템 정보 0xc0
```

- 눈여겨볼 것: syscall (`Nt…`) 이 **어느 깊이**에 달려 있나.
  - 쿠키 쪽은 셋 다 **`InitSecurityCookie` 아래 한두 층** 에 있다.
  - `RtlEncodePointer`·`RtlpInitializeNonVolatileFlush` 는 **자기 본문** 에 바로 있다.
  - 특수 프레임·CPU 기능엔 syscall 이 **없다** (기다리는 스레드의 `NtDelayExecution` 만 빼고).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — `LdrInitSecurityCookie` call `+0x1e0` / ret `+0x1e5` → `NtProtectVirtualMemory`

- **누가**: `LdrInitSecurityCookie` (`InitSecurityCookie` 가 call `+0x38` 로 부른 함수).
- **어디서**: CALL 명령이 `+0x1e0` 에 있다. 멈춰서 콜스택을 찍으면 `LdrInitSecurityCookie+0x1e5` 로 보인다.
- **누구를**: `NtProtectVirtualMemory` — 메모리 몇 바이트의 **보호 (읽기만 / 읽고 쓰기 …) 를 바꿔 달라** 는 창구. 8바이트를 `PAGE_READWRITE` 로 `[정적]`.
  - 비유: 유리 덮개 칠판의 **덮개를 잠깐 여는** 신청.
- **무엇을 위해**: 바로 다음 `+0x1e9` 에서 `__security_cookie` 칸에 새 값을 쓰려고. 그 칸이 `.mrdata` 안이라 덮개를 안 열면 못 쓴다 `[정적]`.
- 쓰고 나면 call `+0x212` (ret `+0x217`) 에서 한 번 더 `NtProtectVirtualMemory` — **옛 보호로 되돌린다** `[정적]`.

#### 한 줄 읽기 ② — `RtlEncodePointer` call `+0x43` / ret `+0x48` → `NtQueryInformationProcess(−1, 0x24, &버퍼, 4, NULL)`

- **누가**: `RtlEncodePointer` (이번 프로세스에선 `LdrpInitializeProcess` 가 call `+0x12f` 로 처음 부름).
- **어디서**: CALL 은 `+0x43`, 콜스택엔 `+0x48`.
- **누구를**: `NtQueryInformationProcess`. 인자를 하나씩 읽으면 `[정적]`:
  - `−1` = **지금 이 프로세스** (`+0x3b OR RCX,-1`).
  - `0x24` = 정보 클래스 (서류 번호). 이름 `ProcessCookie` 는 공개 헤더 이름이라 `[추측]` (PDB 열거형에 없음 — run51 해설 2.2).
  - `&버퍼`, `4` = 4바이트 받을 자리.
- **무엇을 위해**: 관리사무소에 "**우리 가게 열쇠 번호 알려 주세요**" 하고 묻는다. 받은 값을 `+0x50` 에서 캐시 칸에 적어 둔다 (2.7).

### 2.3 B ① 보안 쿠키 — 먼저, 쿠키가 둘이다

이번 바퀴에 "쿠키" 라는 말이 두 가지 다른 것에 쓰인다. 섞이면 뒤가 다 꼬여서 먼저 가른다.

| | 봉인 스티커 | 열쇠 |
|---|---|---|
| 이름 | `__security_cookie` | 커널 `_EPROCESS.Cookie` (`+0x2b8`) / ntdll 사본 `?CookieValue@?1??RtlpGetCookieValue@@9@9` |
| 놓인 곳 | ntdll `.mrdata` rva `0x1ea5a8` (유리 덮개 칠판) | 커널 칸 + ntdll `.data` rva `0x1d2408` (보통 칠판) |
| 크기 | 48비트 값 (덮개는 8바이트만 연다) | 4바이트 |
| 누가 만드나 | **ntdll 스스로** — 시계·틱·스레드 번호·주소·성능 카운터·난수 섞기 | **커널** — 처음 0 이면 만듦 (sast/45). ntdll 은 받아서 복사만 |
| 부르는 함수 | `InitSecurityCookie` → `LdrInitSecurityCookie` | `RtlEncodePointer` (·`RtlDecodePointer`) |
| syscall | `NtQueryPerformanceCounter` 1 · `NtProtectVirtualMemory` 2 | `NtQueryInformationProcess(0x24)` — 캐시 0 일 때만 |

- 위 표는 모두 `[정적]` (run 문서 "확인된 것 B", 결과서 4a·5).
- 이번에 본 차례 안에서는 두 줄기가 서로를 안 부른다. 봉인 스티커 사슬의 syscall 목록에 `0x24` 조회가 없고, 열쇠 쪽 `RtlEncodePointer` 차례에 `__security_cookie` 가 안 나온다 (run 문서 3절) `[정적]`.
- 쓰임:
  - 봉인 스티커 = 함수가 돌아가기 전에 "스택이 덮어써지지 않았나" 를 맞춰 보는 비밀 값 `[추측: 일반적으로 알려진 설명]` (run30 해설).
  - 열쇠 = 포인터를 섞어 적는 데 쓴다 (2.7 의 식은 `[정적]`, 왜 섞는지는 `[추측]`).

이제 봉인 스티커 쪽을 네 걸음으로 본다 (2.4 ~ 2.6).

### 2.4 B ② 보안 쿠키 — 번호표를 뽑고, 1번만 일한다

`InitSecurityCookie` (rva `0xe1548`, 112B) 의 첫 줄은 이렇다 `[정적]`:

- `+0x9  XADD.LOCK [SecurityCookieInitCount],1`
- `+0x13 CMP 순번,1` → `+0x16 JNZ +0x4f` (1 이 아니면 기다리는 길)

#### 새 말 — `XADD` (더하고 옛 값 받기)

- CPU 명령 하나로 **칸 값에 더하고, 더하기 전 값을 돌려받는** 명령 `[추측: 일반적으로 알려진 설명]`. 앞에 `LOCK` 이 붙어 [run17 해설 2.6](../02_sprint/run17-ldr-init-entry-해설.md) 의 `LOCK CMPXCHG` 처럼 다른 CPU 가 끼어들지 못한다.
- 결과서는 이렇게 읽었다: "돌려받은 값 + 1 = 이 스레드 **순번**" `[정적]`.
- 비유: 은행 **번호표 기계**. 누르면 번호가 하나씩 올라가고, 내 손엔 내 번호가 남는다. 두 사람이 동시에 눌러도 같은 번호는 안 나온다.

#### 그래서 갈래는 둘 `[정적]`

| 순번 | 하는 일 |
|---|---|
| **1** (첫 번째로 온 스레드) | 쿠키를 만든다 (2.5) → 덮개 열고 쓰고 닫는다 (2.6) → `+0x42` 깃발 `SecurityCookieInitialized` = 1 |
| 2 이상 | 쿠키에 손 안 댐. `NtDelayExecution(−0x493e0 = 30ms)` (call `+0x61`, ret `+0x66`) 로 쉬며 깃발이 켜질 때까지 다시 본다 (`+0x6d JZ +0x5a` 다시 / `+0x6f JMP +0x49` 끝) |

- 비유: 번호표 1번만 창구에서 일을 보고, 나머지는 의자에 앉아 **"완료" 불이 켜질 때까지** 30ms 마다 고개를 든다.
- 실제로는 거의 한 번만 탄다. 부르는 쪽 `LdrpInitialize +0xa` 가 **깃발을 먼저** 보고, 이미 1 이면 `InitSecurityCookie` 를 아예 안 부르기 때문이다 (결과서 4a) `[정적]`.
- `SecurityCookieInitCount` (`0x1d23d4`) 는 PDB 이름이다. 요청서가 "힌트 이름" 으로 들고 온 것이 맞았다 (`pdb_aliases.py --where`) `[정적]`.

#### 같은 꼴이 한 번 더

- `InitSpecialMachineFrames` 도 똑같다: `+0xf XADD.LOCK [SpecialMachineFramesInitCount],1` → 순번 1 만 일 → `+0x57` 깃발 = 1, 나머지는 30ms 대기 (call `+0x7c`, ret `+0x81`) `[정적]`.
- 그래서 짐 지도 6장 공용 부품에 **"첫 스레드만 하는 일 꼴"** 한 줄이 새로 생겼다 (baggage 6장).

### 2.5 B ③ 보안 쿠키 — 값은 어디서 오나 (씨앗 + 섞기)

순번 1 스레드가 하는 일을 순서대로 `[정적]`:

1. **난수 하나** — call `+0x18` (ret `+0x1d`) `LdrpGenRandom`.
   - KUSD `ProcessorFeatures[28]` 이 0 이 아니면 `RDRAND` 를 최대 10번, 아니면 0.
   - **새 말 — `RDRAND`** = CPU 가 직접 난수를 뽑아 주는 명령 `[추측: 일반적으로 알려진 설명]`. 쓰기 전에 공용 게시판 (KUSD) 칸 `ProcessorFeatures[28]` 을 먼저 본다 `[정적]` (그 칸이 RDRAND 기능 표시라는 뜻풀이는 `[추측]`).
2. **씨앗 만들기** — `+0x1d` 에서 업무 수첩 `LdrSystemDllInitBlock +0x98` 의 4바이트를 읽고, `+0x31` 에서 1번 난수와 `XOR`.
   - 그 수첩 칸은 **커널이 미리 적어 둔 난수** 다 (`PspPrepareSystemDllInitBlock +0x100`, `ExGenRandom`) — run32 그대로.
   - 비유: 관리사무소가 수첩에 적어 준 숫자 + 관리실이 직접 굴린 주사위 = 씨앗.
3. **쿠키 함수 부르기** — call `+0x38` (ret `+0x3d`) `LdrInitSecurityCookie(0, 0, &__security_cookie, 씨앗)`.
4. 그 안에서 먼저 **지금 값 확인** — `+0x5f` 에서 파일 기본값 `0x2b992ddfa232` 와 비교, **다르면** `+0x6c JNZ +0x1a0` 으로 빠져 **아무것도 안 쓴다**.
   - 비유: 스티커가 아직 **공장 기본 무늬**일 때만 새 무늬로 바꾼다. 누가 이미 바꿔 놨으면 손대지 않는다.
5. **재료 섞기** — call `+0x75` (ret `+0x7a`) `LdrpGenSecurityCookie` (rva `0xe1838`):
   - 공용 게시판의 시각·틱 칸 (`+0x14`·`+0x18` `InterruptTime`, `+0x320` `TickCountQuad`, `+0x4` `TickCountMultiplier`) + TEB `+0x40`·`+0x48` (`ClientId`) + 주소 인자를 섞고,
   - **`NtQueryPerformanceCounter`** 한 번 (call `+0x8d`, ret `+0x92`). 전역은 안 쓴다.
   - **새 말 — 성능 카운터** = 아주 잘게 도는 시계의 지금 눈금. 실행할 때마다 거의 같은 값이 안 나와서 섞는 재료로 쓰인다 `[추측: 일반적으로 알려진 설명]`.
6. **마지막 식** (결과서 4a, run 문서 B 줄):
   - 새 값 = `(LdrpGenSecurityCookie(&쿠키) ^ (씨앗 하위32비트 << 16) ^ (씨앗 >> 32)) & 0xFFFFFFFFFFFF` → **48비트**.
   - 새 값이 0 이거나 기본값과 같으면 `0x2b992ddfa233` 으로 바꾼다 (`+0x221`).

- 정리: 쿠키 한 장에 **재료가 다섯 군데**서 온다 — 커널이 적어 둔 수첩 숫자, CPU 난수, 공용 게시판 시계, 내 스레드 번호·주소, 성능 카운터.
- 그중 **창구 (syscall) 를 거치는 건 성능 카운터 하나** 뿐이다 `[정적]`.

### 2.6 B ④ 보안 쿠키 — 덮개를 열고, 쓰고, 닫고, 불을 켠다

`LdrInitSecurityCookie` 안 `[정적]`:

```
call +0x1e0 / ret +0x1e5  NtProtectVirtualMemory (8B, PAGE_READWRITE)   ← 덮개 열기
     +0x1e9  MOV [RDI],RBX   __security_cookie = 새 값                   ← 쓰기
call +0x212 / ret +0x217  NtProtectVirtualMemory (옛 보호로)              ← 덮개 닫기
```

그리고 `InitSecurityCookie` 로 돌아와서 `[정적]`:

- `+0x3d OR.LOCK [RSP],0` → `+0x42 SecurityCookieInitialized = 1`
- **새 말 — 메모리 장벽** = "앞에서 쓴 것이 다른 CPU 에도 다 보이게 된 **다음에** 뒷줄을 하라" 는 신호. 결과서가 `OR.LOCK [RSP],0` 을 이 뜻으로 적었다 `[정적]` (명령이 왜 그런 효과를 내는지는 `[추측: 일반적으로 알려진 설명]`).
  - 비유: 스티커를 다 붙이고 풀이 마른 걸 확인한 **다음에** "완료" 불을 켠다. 불이 먼저 켜지면, 기다리던 직원이 덜 마른 스티커를 볼 수 있다.

#### 왜 이게 PE_emulator 에 중요한가 — 단계별로

1. 근거 `[정적]`: `InitSecurityCookie` 본문에 있는 syscall 은 기다리는 길의 `NtDelayExecution` **하나뿐** 이다 (run 문서 1절 표 밑 사슬).
2. 근거 `[정적]`: `NtQueryPerformanceCounter` 는 두 층 아래 (`LdrpGenSecurityCookie`), `NtProtectVirtualMemory` 둘은 한 층 아래 (`LdrInitSecurityCookie`) 에 있다.
3. 근거 `[정적]`: `LdrInitSecurityCookie` 는 `InitSecurityCookie` 만 부르는 게 아니다. `LdrpProcessMappedModule` 도 call `+0x107` (ret `+0x10c`) 로 부른다 — 씨앗도 똑같이 수첩 `+0x98` ^ `LdrpGenRandom` 으로 만든다 (결과서 질문 2 표). 무엇의 쿠키를 위해서인지는 이번 근거에 없다.
4. 그래서: 결과서 한 줄 표의 `InitSecurityCookie` 줄 "syscall = `NtQueryPerformanceCounter` 1, `NtProtectVirtualMemory` 2" 는 **"이 함수를 부르면 그 아래까지 합쳐 일어나는 syscall"** 로 읽어야 한다. run 문서는 표 글자는 그대로 두고 밑에 사슬을 따로 적었다.
5. 짐작 `[추측]`: PE_emulator 가 "같은 이름·호출관계" 를 지키려면, syscall 을 `InitSecurityCookie` 몸통에 직접 넣기보다 `LdrInitSecurityCookie`·`LdrpGenSecurityCookie` 를 따로 두고 그 안에 넣는 쪽이 진짜와 맞는다. 3번처럼 다른 곳에서도 쓰이는 부품이라서다. 어떻게 지을지는 PE_emulator 쪽 결정이다.

#### 사진과 맞나

- `[관찰]` run44: `__security_cookie` 는 `K1 이전` 값이 있다가 **`P1~P1a` 에서 바뀐다**.
- `[정적]` 쓰는 줄 `LdrInitSecurityCookie +0x1e9` 는 `LdrpInitialize` 안 — `P1~P1a` 구간 (ntdll 혼자 하는 앞부분) 과 맞다.
- 그래서 짐 지도 3.9 `__security_cookie` 줄이 **짐작 `[추측]` → 정확 `[정적]`** 이 됐다.
- 디버거와는 무관하다. 순번 1 스레드가 한 번만 바꾼다 (baggage 7.19).

#### 덤 — 쿠키 직후 한 줄

- `LdrpInitialize +0x46 ~ +0x58`: PEB → `ProcessParameters` → `Flags` (`+0x8`) 맨 윗 비트가 켜졌으면 `+0x5a LdrpIsSecureProcess = 1` `[정적]`. run17 이 이미 본 줄이다. test.exe 에서 그 비트가 켜졌는지는 이번 근거에 없다.

### 2.7 B ⑤ `RtlEncodePointer` — 열쇠 번호는 한 번만 묻고 적어 둔다

`RtlEncodePointer` (rva `0xe9610`, 95B) 차례 `[정적]`:

```
+0xe  MOV EAX,[CookieValue 캐시 0x1d2408] → +0x16 JZ +0x2a
 ├─ (캐시 ≠ 0)  +0x18 ~ +0x20  (포인터 XOR 쿠키) ROR (쿠키 & 0x3f)  → 끝
 └─ (캐시 = 0)  call +0x43 / ret +0x48  NtQueryInformationProcess(−1, 0x24, &버퍼, 4, NULL)
                +0x4a JS +0x58 → call +0x5a RtlRaiseStatus (실패)
                성공 → +0x50 캐시 = 값 → +0x56 JMP +0x18 (위 식으로)
```

#### 새 말 둘

- **포인터 섞기 (인코딩)** = 함수 주소를 칠판에 **그대로 적지 않고**, 열쇠 번호와 섞어서 적어 두는 것. 식은 위 `[정적]`. 섞는 까닭은 "누가 칠판 주소를 몰래 바꿔 써도 열쇠 없이는 쓸모 있는 주소가 안 나오게" 라고 일반적으로 알려져 있다 `[추측]`.
  - 비유: 칠판에 "비상 연락처" 를 적을 때 **암호로** 적는다. 풀 때 (`RtlDecodePointer`) 는 반대로 한다.
- **캐시** = 한 번 물어본 답을 가까운 곳에 **적어 두고** 다음부턴 다시 안 묻는 것.
  - 이번 캐시 칸 이름은 `?CookieValue@?1??RtlpGetCookieValue@@9@9` — 함수 안 `static` 변수에 컴파일러가 붙이는 꼴의 PDB 이름이다 `[정적]` (결과서 5). 이 글에선 줄여서 **`CookieValue`** 라고 쓴다.
  - 비유: 관리사무소에서 받은 열쇠 번호를 관리실 **수첩 귀퉁이에 적어 둔다.** 다음부턴 수첩만 본다.

#### 그래서 syscall 은 처음 한 번 `[정적]`

- 캐시가 0 인 동안만 창구에 간다. 성공하면 그 뒤 모든 `RtlEncodePointer`·`RtlDecodePointer` 는 **syscall 없이** 캐시로 끝난다.
  - `RtlDecodePointer` 도 같은 칸을 읽고 (`+0x6`) 쓴다 (`+0x5c`) (`_refs/0x1d2408.tsv`).
- 실패하면 `RtlRaiseStatus` 를 부른다 `[정적]` (그 자리에서 예외를 일으키는 함수로 알려져 있다 `[추측: 일반적으로 알려진 설명]`).
- 짐 지도 6장에 `RtlEncodePointer`/`RtlDecodePointer` 가 **공용 부품** 한 줄로 들어갔다.

#### 이 프로세스의 첫 `0x24` 조회는 어디인가 — 단계별로

1. 근거 `[정적]`: `LdrpInitializeProcess` 는 call `+0x12f` (ret `+0x134`) 에서 `RtlEncodePointer(0)` 을 부른다 (run18 부터 알던 줄).
2. 근거 `[정적]`: 그때 캐시 `CookieValue` 는 아직 0 이라 `+0x43` 에서 syscall 을 탄다 (run 문서 정정 절 "캐시가 0 이라 syscall 을 탐"). 이 칸을 쓰는 줄로 `_refs` 에 잡힌 것은 `RtlEncodePointer +0x50`·`RtlDecodePointer +0x5c` 다.
3. 근거 `[정적]`: 힙 관리자 쪽 `RtlInitializeHeapManager` (call `+0x9ed`) → `RtlpGetModifiedProcessCookie +0x22` 도 같은 클래스 `0x24` 를 부르지만, `+0x9ed` 는 `+0x12f` 보다 **뒤** 다.
   - 이쪽은 캐시를 **안 쓰고 매번** 묻는다. 받은 값에 `*0x7fffffed + 0x7fffffc3 mod 0x7fffffff` 를 해서 바꿔 쓴다.
4. 그래서: 이 프로세스에서 **처음 열쇠를 달라고 하는 곳은 `+0x12f` 쪽** 이다. 커널이 `EPROCESS.Cookie` 를 처음 깎는 것도 이때다 (커널 쪽 "0 이면 한 번 만듦" 은 sast/45 그대로).
5. 아직 확인 아님: 이건 run 문서가 적은 대로 **"코드 순서상"** 이다. 두 호출 순서를 실행 중 콜스택으로 본 것은 아니다.

- 비유: 열쇠를 처음 받으러 간 사람은 힙 창고 담당이 아니라, 그보다 먼저 출근한 **비상 연락처 담당** 이었다.
- 덤: `+0x134` 에서 `RtlEncodePointer(0)` 이 돌려준 값을 `RtlpUnhandledExceptionFilter` 칸에 쓴다 — flow-writer 가 `.asm` 에서 본 것이고 결과서 표 밖이라 짐 지도에선 **아직 안 셌다** (7.5 실마리 → Q113 (c)).

### 2.8 B ⑥ `RtlpInitializeNonVolatileFlush` — 창구에 한 번 묻고, 칠판 넷에 적는다

`LdrpInitializeProcess` call `+0x28d` (ret `+0x292`) → `RtlpInitializeNonVolatileFlush` (rva `0xfb648`, 144B) `[정적]`.

#### 새 말 — 비휘발 플러시

- **비휘발 메모리** = 전원이 꺼져도 내용이 남는 메모리. **플러시** = CPU 가 잠깐 들고 있던 쓰기를 메모리까지 확실히 **내려 보내는** 일 `[추측: 일반적으로 알려진 설명]`.
- 이 함수는 "이 컴퓨터에서 그걸 하려면 어떤 방법을 써야 하나" 를 미리 알아 두는 준비로 보인다 `[추측]` (함수·전역 이름과 커널이 채우는 칸 이름에서).
  - 비유: 금고에 서류를 넣을 때 "그냥 넣으면 되나, 한 번 더 눌러 닫아야 하나, 몇 장씩 넣나" 를 관리사무소에 물어 **칠판에 적어 두는** 일.
- 각 비트가 **어느 CPU 명령** 에 해당하는지는 `[공백]` 이다 (결과서가 안 봄).

#### 무엇을 묻나 `[정적]`

- `+0x1e MOV ECX,0xc0` → call `+0x31` (ret `+0x36`) 시스템 정보 조회, 버퍼 `0x20` 바이트.
  - 클래스 `0xc0` = nt PDB 열거형 `SystemFlushInformation` (192).
  - 이 CALL 대상은 `.asm` 에서 **같은 주소에 이름 2개** 로 보인다 — 후보 `NtQuerySystemInformation`·`RtlGetNativeSystemInformation` (run45 `LdrInitializeMrdata` 때와 같은 것). [한 주소에 이름 여럿](../02_sprint/run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) 이야기다.
- 커널 `ExpGetSystemFlushInformation` 이 버퍼를 채운다:
  - `+0x0` 플래그: 비트 0 은 늘 1, 비트 1·2 = `KeFeatureBits` 비트 35·36
  - `+0x4` = `KPRCB+0xe8` 캐시 줄 크기
  - `+0x8` = `ExpPlatformFlushCapabilities` (없으면 ACPI `NFIT` 표를 읽음)

#### 무엇을 쓰나 `[정적]` (결과서 질문 6 표 그대로, 괄호는 파일 처음 값)

| 줄 | 전역 | 값 | 조건 |
|---|---|---|---|
| `+0x41` | `RtlpIsFlushRequired` (`0x1cd0c0`, 1B, 처음 1) | 0 | `[버퍼+8]` 비트 0 |
| `+0x85` | `RtlpOptimalFlushMethod` (`0x1cd0b0`, 4B, 처음 1) | 2 | `[버퍼+0]` 비트 2 |
| `+0x56` | `RtlpOptimalFlushMethod` | 3 | 비트 2 꺼지고 비트 1 켜짐 |
| `+0x60` | `RtlpIsDrainRequired` (`0x1ce749`, 1B, 처음 0) | 1 | 위 둘 중 하나일 때 |
| `+0x6b` | `RtlpClFlushSize` (`0x1cd0b8`, 8B, 처음 64) | `[버퍼+4]` | 성공하면 늘 |

- **실패하면 아무것도 안 쓴다** (`+0x38 JS +0x72`). 그러면 네 칸은 파일 처음 값 그대로다.
- 이 VM 에서 어느 줄을 탔는지는 정적으로 못 안다 → Q114 1.

#### 이 칸들은 "커널이 썼다" 로 세나, "ntdll 이 썼다" 로 세나

- 짐 지도 2장의 규칙 `[정적]`: 쿠키 캐시·플러시 넷처럼 **값이 syscall 결과** 여도, 칸에 쓰는 줄은 ntdll 이라 **ntdll 로만** 센다.
- 까닭: 커널은 ntdll 이 넘긴 **지역 버퍼** 에 답을 적는다. 칠판 칸에 직접 쓰는 건 그 뒤 ntdll 이다.
  - run57 의 `GlobalRtlNlsState` 는 반대로 syscall 이 칸에 **직접** 써서 nt 열에도 셌다. 그것과 다르다.
- 비유: 관리사무소는 **답장 종이** 에만 적어 준다. 칠판에 옮겨 적는 건 관리실 직원이다.
- 결과서는 flush syscall 한 줄을 "커널이 칸을 채움" 쪽 (2장 nt 몫) 에도 적어 두자고 제안했다 (짐 지도용 표 "들어갈 곳").

### 2.9 B ⑦ 특수 기계 프레임 · CPU 기능 — 둘 다 syscall 없음

#### `InitSpecialMachineFrames` (rva `0xe14b4`, 139B) `[정적]`

- 번호표 꼴은 2.4 와 같다.
- 순번 1 이면 표 `RtlpContinuationContextMachineFrameEntries` (`.data` `0x1cc208`, 8바이트 × 2칸) 를 **두 바퀴** 돈다:
  - 칸 안 값 = 함수 주소 (파일 값: `[0]` = ntdll+`0x164e10` `KiUserExceptionDispatcher` 쪽, `[1]` = ntdll+`0x164ca0` `KiUserApcDispatcher` 쪽).
  - call `+0x39` (ret `+0x3e`) `RtlLookupFunctionEntry` 로 그 함수의 `RUNTIME_FUNCTION` 을 찾고,
  - `+0x46` 에서 칸을 `ImageBase + UnwindData` 로 **바꿔 쓴다**.
- **새 말 — 풀림 정보 (`UnwindData`)** = 함수마다 붙어 있는 "예외가 나면 이 함수의 스택을 어떻게 거꾸로 되짚나" 안내 쪽지 `[추측: 일반적으로 알려진 설명]`. `RUNTIME_FUNCTION` 은 그 쪽지를 찾아 주는 목차 줄이다.
  - 비유 (run 문서): **비상구 지도.** 표에 "이 두 방" 의 이름만 적혀 있던 것을, 각 방의 **비상구 지도 위치** 로 바꿔 적어 둔다.
- 읽는 쪽 `RtlpIsContinuationContextMachineFrameEntry +0xb · +0x12` 가 이 표와 비교한다 `[정적]`. 그래서 "이 풀림 정보가 커널 → 유저 진입 함수의 것인가" 를 가르는 표로 보인다 `[추측]`.
- `RtlLookupFunctionEntry` 는 ntdll 안 함수다 → **syscall 없음**. `+0x57` 깃발 = 1.

#### `__cpu_features_init` (rva `0x12e47c`, 130B) `[정적]`

- **깃발 검사 없이 매번** 불린다 (나중 스레드도). 그래도 매번 같은 길이다 — 갈래 없음.
- `RtlIsProcessorFeaturePresent` 네 번 (call `+0xb` / `+0x17` / `+0x45` / `+0x5f`, 번호 `0x26` / `0x2a` / `0x27` / `0x28`) → `+0x74` 에서 `__isa_info` (`0x1ccf00`) 한 바이트를 쓴다.
  - 값 = 늘 `1` | `0x2a` 있으면 `2` | `0x27` → `4` | `0x26` → `8` | `0x28` → `0x10`. 파일 처음 값은 `2`.
  - 번호 이름 (SSE4.2·AVX·AVX2·ERMS) 은 공개 헤더 짐작 `[추측]` — 코드엔 숫자만.
  - `RtlIsProcessorFeaturePresent` 는 공용 게시판 `ProcessorFeatures` 칸 (또는 `RtlProcessorFeaturesBitMap`) 을 **읽기만** 한다. → syscall 없음.
- 비유 (run 문서): **장비 점검표** 한 장. 게시판에 붙은 "이 건물 장비 목록" 을 보고 체크 표시만 한다.
- 이 바이트를 읽는 쪽은 `memset`·`wcslen`·`strlen` 류다 (`_refs/__isa_info.tsv`).

#### 요청서 질문 하나 — 사진 `P1b~P2` 의 CPU 기능 전역 6개는 이 함수가 쓰나 → **아니오** `[정적]`

- `x86_cpu_enable_*` 셋 · `g_SymCrypt*` 셋은 `LdrpInitializeProcess` **뒤쪽** `+0x1d75 ~ +0x1e27` 과 `SymCryptDetectCpuFeaturesByCpuid +0xee`·`+0xf4` 가 쓴다 (CPUID 직접).
- 그 자리는 `P1b` (`+0x1bb6` call) 보다 뒤 코드라, 사진 구간 `P1b~P2` 와 맞는다.
- 그래서 짐 지도 7.3 의 `.data P1b~P2 (6)` 모름이 **0** 이 됐다.

### 2.10 B ⑧ 덤 — 로더가 끝날 때 ETW 갈래

`LdrpInitializationComplete` 의 갈래만 짧게 `[정적]` (자세한 줄은 run 문서 3절):

1. `RtlGetCurrentServiceSessionId` (call `+0x19`) — `PEB.SharedData` (`+0x90`) 가 NULL 이면 **0**.
   - test.exe 는 K1 사진에서 `PEB+0x90 = (null)` `[관찰]` → 0. (그 칸을 채우는 쪽은 2.12 ⓛ — 보통 프로세스는 안 채움.)
2. 세션 번호 0 → 검사 칸 = 공용 게시판 **`0x7ffe0384`** (PDB `UserModeGlobalLogger` `unsigned short[16]` 의 `[2]` 원소 아래 바이트).
3. 그 칸이 0 이 아니고 + `PEB.TracingFlags` 비트 2 (`LibLoaderTracingEnabled`) + 윗 바이트 비트 5 일 때만 `LdrpLogEtwEvent` (call `+0xa5`).
4. 어느 쪽이든 `+0x44` 에서 `LdrpProcessInitialized` 1 → 2, 이벤트가 있으면 `NtSetEvent` (call `+0x51`).

- 4번은 [run17 해설 2.6](../02_sprint/run17-ldr-init-entry-해설.md) 의 칠판 "옛 값 2 = 끝났다" 가 켜지는 줄이다.
- 이 VM 에서 ETW 이벤트를 남겼는지는 실행 값이라 모른다 → Q114 9.

### 2.11 D — PE_emulator 의 asm 읽기 셋 확인 (셋 다 맞다)

| 질문 | 답 `[정적]` |
|---|---|
| 1. 동적 함수표 전역을 쓰는 곳이 `_LdrpInitialize +0x10d ~ +0x13e` 인가 | **예.** 일곱 줄: `+0x114`·`+0x11b` 목록 머리 = 자기 (빈 원형 목록), `+0x122` 락 0, `+0x129 ~ +0x13e` Tree 넷 0. 첫 스레드 길만 (`+0x3a CMPXCHG` 0 → `+0x5a JZ +0xcf`) |
| 2. 시작 정보 블록 `+0x98` 을 읽는 곳이 `LdrInitializePerThreadRng +0x4`·`LdrpProcessMappedModule +0xe7` 인가 | **예** (둘 다 4바이트). 전체는 **다섯 곳** — 그 둘 + `InitSecurityCookie +0x1d` (쿠키 씨앗, 2.5) · `RtlpInitRandomExVector +0x3d` · `AvrfMiniLoadDll +0x83c`. 쓰는 쪽은 커널 `PspPrepareSystemDllInitBlock +0x100` 뿐 |
| 3. `PebLdr.Initialized` = 1, `RtlpFcProcessManager`, 스핀 횟수 | `Initialized` 는 **`+0x166`** (아래 한 줄 읽기). `RtlpFcProcessManager` (`0x118`B) 는 `memset` 넷 + 8바이트 0 넷 → 전부 0, `+0x27f`·`+0x286` 이 `+0x108` 빈 목록 머리. 스핀은 **전역 셋, 쓰기 여섯 줄** |

- **새 말 — 빈 원형 목록** = 목록 머리의 앞·뒤 칸이 **자기 자신** 을 가리키는 꼴. "아직 아무도 줄 서지 않았다" 는 뜻 `[추측: 일반적으로 알려진 설명]`.
  - 비유: 줄서기 안내 줄의 앞끝과 뒤끝을 **같은 기둥** 에 묶어 둔 것.
- 1번의 락·Tree 넷은 파일 처음 값도 0 이라 **값은 안 바뀐다**. 목록 머리만 0 → 자기 주소로 바뀐다.
- 3번 스핀: 먼저 셋 다 0, `PEB.NumberOfProcessors` > 1 이면 `0x2800` 으로 덮는다 (`SRWLockSpinCycleCount` `+0x1cd`/`+0x1dd` · `ConditionVariableSpinCycleCount` `+0x1e7`/`+0x1f0` · `RtlpWaitOnAddressSpinCycleCount` `+0x1fa`/`+0x203`).
  - run18 의 "스핀 3곳" 은 **전역 수** 로 읽으면 맞다. 그래서 run18 은 안 고쳤다.
- `Fc` 가 무슨 약자인지는 코드에 없다 `[공백]`.

#### 한 줄 읽기 ③ — `+0x162` 는 쓰는 줄이 아니다

- `+0x162 LEA R15D,[R14+1]` — `R14` 가 0 이라 **값 1 을 레지스터에 만드는** 줄이다.
- `+0x166 MOV byte [PebLdr+0x4],R15B` — 그 1 을 **칸에 쓰는** 줄이다.
- 비유: 분필을 집는 손 (`+0x162`) 과 칠판에 쓰는 손 (`+0x166`). 요청서는 분필 집는 순간을 "쓴 자리" 로 짐작했다.
- 짐 지도 3.4 는 처음부터 `+0x166` 이라 안 고쳤다 (값 칸에 `+0x162` 출처만 보탬).

### 2.12 C — 커널 비트 뜻 (표로 짧게)

#### ⓐ PEB `BitField` (`+0x3`) — 비트마다 어디서 오나 `[정적]`

`PspAllocateProcess` 가 PEB 를 직접 쓰는 게 아니라 **견본** (지역 `[RSP+0x1a8]`, `+0x1ab` = `BitField`) 을 만들어 `MmCreatePeb` 에 넘기고, `MmCreatePeb` 가 복사한다 (`+0x22e1 MOV [RSP+0x1ab],R12B` → call `+0x230f`).

| 비트 | 이름 (PDB) | 출처 |
|---|---|---|
| 0 | `ImageUsesLargePages` | 지역 플래그 `F` 비트 12 = 큰 페이지 요청 (9번째 인자 `0x10` 또는 IFEO `UseLargePages`) |
| 1 | `IsProtectedProcess` | `EPROCESS.Protection` 의 `Type` ≠ 0 |
| 2 | `IsImageDynamicallyRelocated` | `F` 비트 13 = 섹션 `ImageFlags` 비트 2 |
| 3 | `SkipPatchingUser32Forwarders` | 안 켬 |
| 4 | `IsPackagedProcess` | `PsQueryProcessAttributes` — 토큰에 `WIN://PKG` 속성이 있나 |
| 5 | `IsAppContainer` | `SeQueryInformationToken(0x1d)` 결과 (번호 이름은 `[추측]`) |
| 6 | `IsProtectedProcessLight` | `Protection.Type == 1` |
| 7 | `IsLongPathAwareProcess` | 안 켬 (커널은 안 씀) |

- `[관찰]` run44 K1: `BitField = 0x4` (비트 2 만), `ImageFlags = 0x4`, `Mutant = -1` → 위 식과 **맞다**.
- 지역 플래그 `F` (`EBX`) = 함수 앞부분에서 조건마다 켜 두는 **메모 비트 모음**. 이름 `LARGE_PAGES` 는 phnt 쪽 `[추측]`.

#### 나머지 C 묶음 `[정적]` (요점만)

| 질문 | 답 | test.exe |
|---|---|---|
| ⓒ 프로세스 `+0x170` 비트 0 | `_KPROCESS.SecureState.Flags.SecureProcess` ("보안 프로세스", VSL). 켜는 곳은 `KeInitializeProcess +0x473` 하나. `MmCreatePeb +0x2b8` 은 켜져 있으면 이미지 헤더 대신 고정값을 쓴다 | 꺼짐 `[추측]` (값은 동적, Q114 5) |
| ⓓ `F` 비트 8 | 효과 = **"PEB 를 가지는 프로세스"** — 최소 프로세스 아님 (`F` 비트 1 꺼짐) + 부모·섹션 없는 시스템류 아님 (`F` 비트 0 꺼짐). 이름은 코드에 없다 `[추측: 이름]` | `F` = 0 → 켜짐 |
| ⓔ 만들기 정보 `+0x10` 비트 4, `PspLocateInPEManifest` | 비트 4 = 사용자 `PS_CREATE_INFO.InitFlags` 비트 1 을 옮긴 것 (이름 `DetectManifest` 는 `[추측]`). 함수는 exe 의 `RT_MANIFEST` ID 1 자원을 찾아 `+0x10` 비트 5 · `+0x130` · `+0x138` · 매개변수 `Flags` 비트 13 을 켬. 못 찾음은 성공 처리 | **자원 디렉터리가 없어** 불려도 아무것도 안 켜짐 `[정적]` (PE 헤더). K1 `Flags = 0x4001` `[관찰]` 과 맞음. 불렸는지는 Q114 6 |
| ⓖ `PspGetProcessParameterOverrides` | 쓰는 곳은 만들기 정보 `+0x1d8` 하나. 간접 import 슬롯의 **PE 파일 값이 `ret 0` 스텁** (ntoskrnl rva `0x4a10c0`) 이라 파일 값 그대로면 0. `PsDefaultLoaderThreads` 는 **안 읽는다** (읽는 곳은 `PspSetupUserProcessAddressSpace +0xc1` 하나) | `+0x1d8` 읽는 쪽 `[공백]` (Q113) · 슬롯이 실행 때도 스텁인가 (Q114 4) |
| ⓗ 옵션 셋째 칸 `+0xb0` (`0x02002002`) | 옵션 40개 × 4비트, 셋째 8바이트 = 옵션 32~39. 사진 값 = 비트 0~1·12~13·24~25 가 각 **2** → `== 1` 효과 없음. 0~1 의 2 는 `PspApplyMitigationOptions +0x510 ~ +0x518` 이 쓴 것. **12~13·24~25 의 2 는 입력 표가 갖고 온 값** | 출처 `[공백]` (Q113) · ntdll `+0x4b3`·`+0xa09` 는 비트를 안 읽음 |
| ⓙ PEB `+0x138` · `+0x7c8` | `+0x138` `ActiveProcessAffinityMask` = `PspWritePebAffinityInfoAttached +0x6f`. `+0x7c8` `ExtendedFeatureDisableMask` = `KeCopyXfdMaskToPeb` 조각 `+0x7`, `KeFeatureBits` 비트 `0x37` 일 때만 | `+0x138` = `3` `[관찰]` 과 맞음. `+0x7c8` = 0 은 안 탔는지 0 을 썼는지 못 가림 (Q114 3 · Q79) |
| ⓛ `PspMapSiloSharedDataView` | 서버 silo 가 없으면 **아무것도 안 쓰고 0** (`+0x25`). 있으면 PEB `+0x90` `SharedData` = 뷰 주소 | `PEB+0x90` NULL `[관찰]` |
| ⓑ PEB `+0x7b8` `LeapSecondData` | 읽는 ntdll 함수 `RtlpTimeToTimeFields +0x1e`·`RtlpTimeFieldsToTime +0x1d`. 섹션은 **부팅 때 한 번** `ExInitializeLeapSecondData` 가 만든다 (`0x1000` 익명, 내용 `_LEAP_SECOND_DATA`, 값은 레지스트리). 프로세스마다 `MmCreatePeb +0x126` 이 읽기용 뷰를 붙임 | 이 VM 레지스트리 값은 동적 (Q114 8) |

- ⓙ 의 `+0x7c8` 은 [run52 해설](run52-kthread-create-해설.md) 2.8 의 "해당자만 기입" 칸과 **같은 이야기** 다. 전역 `KeFeatureBits` 한 번 읽기로 PEB·KPROCESS·KTHREAD·TEB 네 칸이 함께 닫힌다 (baggage 7.19).
- ⓖ 의 스텁 rva `0x4a10c0` 은 [run53 해설](run53-ethread-create-해설.md) 2.6 에서 `KernelRoutine` 으로 본 "하는 일 없는 빈 함수" 와 **같은 rva** 다 (두 문서에 적힌 숫자가 같다).
  - 비유: 바깥 업체 전화번호 칸에 **"아무 말 없이 끊는 번호"** 가 기본으로 적혀 있다. 부팅 때 진짜 업체 번호로 바뀌는지는 아직 모른다.

### 2.13 E — K1 환경 블록 변수 이름 47개 `[관찰]`

- 덤프 원본에서 test.exe 의 환경 블록 (`0x25996cb0734`, `0xd14` 바이트) 을 통째로 읽었다. run44 로그엔 앞 `0x200` 바이트만 있었다.
  - 읽은 방법 한 줄 (run 문서): 덤프 머리의 물리 메모리 구간 표 → test.exe `DirectoryTableBase` `0x192c2e000` 로 4단계 페이지 걷기 → 그 주소. 앞 `0x200` 바이트가 로그와 같은지 확인했다.
  - **새 말 — 페이지 걷기** = 가상 주소를 물리 주소로 바꿀 때 페이지 표를 네 번 따라 내려가는 일 `[추측: 일반적으로 알려진 설명]`. 비유: 주소 "몇 동 몇 층 몇 호" 를 단지 안내도 → 동 안내도 → 층 안내도 → 호실 순서로 찾아가기.
- 이름만 적었다 (값은 안 적음). 정렬 순서 그대로, 끝은 빈 항목 둘 (크기 `0xd14` 와 맞음).
- 눈에 띄는 것:
  - `=::` · `=C:` · `=ExitCode` — 이름 앞 `=` 가 **이름의 일부** 다 (드라이브별 현재 폴더·종료 코드 관례).
  - `EFC_5052_*` 6개 — 이 VM 부모 셸 쪽이 넣은 것 `[추측]`.
  - 나머지는 `Path` · `TEMP` · `SystemRoot` · `windir` 같은 흔한 이름들. 전체 목록은 run 문서 "확인된 것 E".
- 이건 **이 VM 의 부모 셸이 물려준 환경** 이다. 에뮬레이터가 그대로 베낄 값이 아니다 (결과서).

### 2.14 정정 — 한 절에 모아서

#### (가) 이전 기록이 틀렸던 곳 (run 문서 "정정")

| 무엇 | 전 | 후 | 고친 곳 |
|---|---|---|---|
| 첫 `NtQueryInformationProcess(0x24)` | 힙 관리자 `RtlInitializeHeapManager → RtlpGetModifiedProcessCookie` 가 커널 `Cookie` 를 처음 만든다 (run51) | **`LdrpInitializeProcess +0x12f` (ret `+0x134`) `RtlEncodePointer(0)` → `+0x43` (ret `+0x48`)** 가 첫 호출. 힙 관리자 쪽 (`+0x9ed`) 은 이미 만든 값을 읽음 | run51 문서 맨 위 한 줄, baggage 3.11 `Cookie` 줄·5장 ②, flow.md 2.4 run51 쿠키 절 |
| `PspSetupUserProcessAddressSpace +0x227 · +0x234 · +0x263` | call 자리처럼 적힘 (요청서·baggage 7.5·5장 ①·flow.md 2.1 대표 줄) | **ret 자리**. call 은 `+0x222 · +0x22f · +0x25e` | baggage·flow.md 에 "(ret)" 붙임 |
| `PspAllocateProcess` 의 직접 호출 복사본 | run32 순서 표에 없음 | `+0x2574` 갈래에 `KeCopyXfdMaskToPeb`·`PspUpdatePebForAffinityChange`·`MmMapApiSetView`·`PspMapSiloSharedDataView` 를 **직접 부르는 복사본** (call `+0x259e`·`+0x25ae`·`+0x25ba`·`+0x25c8`). `PspSetupUserProcessAddressSpace` 를 건너뛸 때만. test.exe 는 `PspSetupUserProcessAddressSpace` 쪽 `[추측]` (동적 Q35) | flow.md 2.1 (run32 문서는 안 고침) |
| `Initialized` = 1 쓰는 줄 | 요청서 짐작 `+0x162` | `+0x166` (2.11) | baggage 3.4 는 처음부터 맞았음 |
| baggage 7.1 "전역 281" | 281 | run45·run46 몫 7개를 안 뺀 옛 숫자 → 274 → 이번에 **271** | baggage 7.1 |

- 비유 (첫 줄): 열쇠를 처음 받으러 간 사람을 **잘못 적었다.** 창구 (커널) 가 "0 이면 한 번 깎아 준다" 는 규칙은 그대로다. 바뀐 건 "누가 먼저 왔나" 뿐이다.

#### (나) 결과서와 run 문서가 다른 곳 (뜻은 같고 줄만 — 결과서는 안 고침, run 문서·flow.md·baggage 는 `.asm` 값)

| 결과서 | run 문서 (`.asm`) |
|---|---|
| `PspPrepareSystemDllInitBlock +0x126 MOVUPS [RBX+0xa0]` | `+0x126` 은 스택 **읽기**, 블록에 **쓰기는 `+0x12b`**. `+0xb0` 도 `+0x132` 읽기 → `+0x138` 쓰기. run32 가 맞다 |
| `ExInitializeLeapSecondData` call `+0x3a` · `+0xb1` · `+0x1b1` | call **`+0x3e` · `+0xb5` · `+0x1b5`** (ret `+0x43` · `+0xba` · `+0x1ba`). 셋 다 4바이트씩 앞 줄 |
| `MmCreatePeb … +0x132 MmMapViewOfSection` | `+0x132` 는 **ret**, call 은 `+0x12d` |
| KUSD `+0x384` = `UserModeGlobalLogger` **둘째 원소** 아래 바이트 | PDB 타입이 `unsigned short[16]` 이라 **`[2]` (셋째 원소)** 아래 바이트 |
| 한 줄 표 `InitSecurityCookie` 줄의 syscall 셋 | 표 글자는 그대로. syscall 은 **callee 안** 이라고 표 밑에 사슬을 따로 적음 (2.6) |
| 질문 6 의 호출 대상 `NtQuerySystemInformation` | `.asm` 은 **같은 주소 이름 2개** — 후보 `NtQuerySystemInformation`·`RtlGetNativeSystemInformation` (2.8) |

- 결과서 숫자 넷이 "**명령 하나 앞 줄**" 을 적었다 (run 문서 도구 메모). run56~58 의 call/ret 섞임에 이어 네 번째다.
  - 그래서 이 글의 숫자는 모두 run 문서 쪽이다.

#### (다) 해설끼리 — 옛 해설은 안 고쳤다

- [run51 해설](run51-eprocess-later-해설.md) 2.8 의 그림과 "쿠키 시스템 콜이 바로 이 줄을 탄다" 는 **힙 관리자 쪽을 첫 호출로 본 옛 기록** 기준이다.
  - 그 길도 커널 `Cookie` 줄에 닿긴 한다. 다만 **처음 깎는 쪽은 아니다** — 위 (가) 첫 줄.
  - 이 글은 그 해설을 고치지 않았다.

### 2.15 정리 — 비유로 한 번 더

1. 관리실 직원들이 출근하면, **번호표 1번** 만 잡일을 한다. 나머지는 "완료" 불이 켜질 때까지 30ms 마다 고개를 든다 (2.4).
2. 1번은 먼저 **봉인 스티커** 무늬를 정한다. 재료는 관리사무소가 수첩에 적어 둔 숫자, CPU 주사위, 게시판 시계, 자기 번호, 정밀 시계 (2.5).
   - 스티커 칠판에는 유리 덮개가 있어서 **창구에 덮개를 열어 달라고 하고**, 쓰고, **다시 닫아 달라고** 한다 (2.6).
   - 창구에 가는 일은 1번 직원 본인이 아니라 그가 부른 **조수 둘** (`LdrInitSecurityCookie`·`LdrpGenSecurityCookie`) 이 한다.
3. 이어서 **비상구 지도** 두 칸, **장비 점검표** 한 칸을 혼자 적는다 — 창구 갈 일 없음 (2.9).
4. 가게 전체 세팅 (`LdrpInitializeProcess`) 에 들어가면, 맨 먼저 비상 연락처 담당이 관리사무소에 **열쇠 번호** 를 묻고 수첩 귀퉁이에 적어 둔다. 이 가게의 첫 열쇠 요청이다 (2.7).
5. 조금 뒤 **금고 서류 넣는 법** 을 창구에 한 번 묻고, 답장을 보고 칠판 넷에 옮겨 적는다 (2.8).
6. 그 전에 관리사무소는 안내판 몇 칸을 미리 채워 뒀다 — "주소가 옮겨졌음" 비트, CPU 친화도 `3`. 보호 건물·큰 방·silo 같은 칸은 test.exe 에 해당이 없다 (2.12).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 절 | 전 | 후 |
|---|---|---|
| 기준 | PDB 줄에 run59 없음 | ntkrnlmp·ntdll PDB 줄에 run59. 사진 다시 읽기 메모에 "K1 덤프 원본에서 환경 블록 이름 47개" |
| 2.1 `NtCreateUserProcess` | `+0xab3` 줄 없음 | `[정적]` `+0xab3` (ret `+0xab8`) `PspGetProcessParameterOverrides` — `+0x1d8` 하나, 파일 값 스텁 |
| 2.1 `PspAllocateProcess` | 견본 복사만 | `[정적]` PEB 견본과 지역 플래그 `F` 대표 줄 + **`+0x2574` 복사본 갈래** (run32 표에 없던 것) |
| 2.1 `PspSetupUserProcessAddressSpace` | callee 넷 이름만, `+0x255`·`+0x263` | `[정적]` 넷의 본문 요점. `+0x255`·`+0x263` 은 ret (call `+0x250`·`+0x25e`) |
| 2.1 `PspPrepareSystemDllInitBlock` | `+0x98` ← `ExGenRandom` (RngData `[추측]`) | + ntdll 이 읽는 곳 다섯, 옵션 24B 의 읽기·쓰기 줄과 셋째 칸 비트 뜻 |
| 2.4 `LdrpInitialize` | 세 함수 이름만 (안은 `[추측]`) | `[정적]` **세 함수 본문** (call / ret) |
| 2.4 `_LdrpInitialize` | `+0x10d ~ +0x13e` 몰랐음 | `[정적]` 동적 함수표 전역 일곱 줄 |
| 2.4 `LdrpInitializeProcess` 트리 | `+0x134` · 스핀 · flush 줄 없음 | `+0x134 RtlEncodePointer(0)` ① **이 프로세스 첫 `0x24`**, `+0x1cd ~ +0x286` 스핀·Fc ②, `+0x292` flush ② |
| 2.4 run51 쿠키 절 | 첫 호출 = `+0x9f2 RtlInitializeHeapManager → … +0x27` | 첫 호출 = `+0x134 RtlEncodePointer → +0x48` (정정 문단) |
| 2.4 ETW 갈래 | 없음 | `[정적]` `LdrpInitializationComplete` 갈래 줄 |
| 4장 | run57 "정적으로 남은 것" 에 비트 8·4·1 뜻 | 비트 8·1 **일부 해결** (비트 4 그대로). 새 줄: 해결됨 · Q113 · Q114 · 정정 |
| 5장 | — | run59 근거 줄 둘 |
| 그림 | — | **안 바뀜** |

- run17 해설에서 이름으로만 짐작한 "세 함수 안" `[추측]` 이, 이번에 flow.md 2.4 에 `[정적]` 근거 (run59) 로 들어왔다.

### 짐 지도 `baggage.md` 에서 바뀐 것 (열다섯째판)

- **3장 새 줄 27** (3.1 PEB 4 · 3.2 매개변수 1 · 3.8 `.data` 20 · 3.9 `.mrdata` 2). 3장 줄 620 → **647**. 줄 끝마다 **"Q109 로 닫힘"**.
- **2장 숫자**:
  - 구조체 아는 칸 704 → **715** (정확 649 → 660) · 모름 335 → **324** (`_PEB` 86 → 75).
  - 전역 쓴 자리 아는 것 80 → **105** (`.data` 69 → 89 · `.mrdata` 10 → 15) · 모름 413 → **388**.
- 태그·정확도가 바뀐 줄:
  - 3.9 `__security_cookie`: `[추측]` 짐작 → **`[정적]` 정확** (`LdrInitSecurityCookie +0x1e9`).
  - 3.11 `Cookie`: ntdll 쪽 경로를 `RtlEncodePointer (호출 +0x12f)` 로 정정.
  - 3.12 `SecureState`: 비트 0 뜻 보탬.
- 새 **7.19** = 이번 바퀴가 넘긴 질문 (Q114 1~10 + Q113).
- 6장 공용 부품 3줄: 첫 스레드만 하는 일 꼴 · `RtlEncodePointer`/`RtlDecodePointer` · `RtlIsProcessorFeaturePresent`.

### 큰 그림에서 어떻게 이어지나

- PE_emulator 쪽: run 문서 1절 "한 줄 표" 와 2절 "짐 지도용 표" 가 그대로 **공사 중 함수 채우기의 근거** (`[정적] run59`) 가 된다.
  - 특히 쿠키 사슬은 **어느 함수 안에 어느 syscall** 을 둘지 (2.6), 쿠키 캐시는 **처음 한 번만** 창구에 가게 (2.7), flush 는 **실패하면 아무것도 안 쓰게** (2.8) 지을 근거다.
- 다음 차례 **Q111** (PE_emulator 요청 5) 과 이어지는 점:
  - 이번에 `.mrdata` 칸 (`__security_cookie`) 을 쓰려면 **`NtProtectVirtualMemory` 로 덮개를 연다** 는 것을 한 곳에서 봤다 `[정적]`.
  - Q111 은 `.mrdata` 를 열고 닫는 다른 함수 `LdrProtectMrdata` 가 같은 syscall 을 부르는지를 묻는다 (plan Q111 (b)). 같은 꼴일지는 아직 **모른다** — Q111 에서 본다.
- 짐 지도 쪽: Q109 로 PEB 11칸 · 전역 25 가 닫혔다. 남은 모름 (구조체 324 · 전역 388) 은 Q78d4 ~ Q78g · Q79 · Q80 차례다.

---

## 4. 아직 모르는 것

### 정적으로 남은 것 → **Q113**

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| 옵션 셋째 칸 비트 12~13·24~25 의 "2" 가 **처음 어디서** 오나, 옵션 번호 32~39 의 이름 | `PspApplyMitigationOptions` 는 그 비트를 안 쓴다 → 입력 쪽 (`PspBuildCreateProcessContext`·`PspInheritMitigationOptions`) 을 안 봄. PDB 에 옵션 열거형이 없음 | Q113 (a) |
| 만들기 정보 `+0x1d8` 을 **읽는** 함수 | 쓰는 쪽만 봄 | Q113 (b) |
| 결과서 표 밖 전역 셋 (`RtlpUnhandledExceptionFilter` `+0x134`, `LdrpEnforceIntegrityContinuity`·`LdrpAuditIntegrityContinuity` `+0x4c4 ~ +0x4fe`) 을 2장에 셀지 | `.asm` 에 쓰는 줄은 보이지만 결과서 숫자가 아니라 짐 지도 7.5 실마리로만 둠 | Q113 (c) |
| (대기열 번호 없음, 기록만) `F` 비트 18~26 · 비트 4 뜻, `RtlpFcProcessManager` 의 `Fc`, flush 비트가 어느 CPU 명령인지 | 질문 밖 | flow.md 4장 Q113 줄 |

### 실행해 봐야 아는 것 → **Q114** (결과서 번호 그대로)

| 번 | 무엇 | 왜 정적으로 안 되나 | 어디서 멈추나·읽나 |
|---|---|---|---|
| 1 | flush 전역 넷이 어떤 값이 됐나 | 커널 답 (이 컴퓨터 CPU·플랫폼) 에 달림 | `ntdll!RtlpInitializeNonVolatileFlush+0x36` (ret) 에서 `dq @rsp+0x20 L4` |
| 2 | `__isa_info` 값 | 이 CPU 의 기능에 달림 | `ntdll!__cpu_features_init+0x7a` |
| 3 | PEB `+0x7c8` 갈래를 탔나 | 전역 `KeFeatureBits` 비트 `0x37` 값 | `dq nt!KeFeatureBits` (덤프로도 — **Q79** 와 같은 읽기) |
| 4 | `__imp_PsGetProcessParameterOverrides` 가 실행 때도 스텁인가 | 부팅 때 확장 모듈이 바꿀 수 있음 | `dq nt!_imp_PsGetProcessParameterOverrides L2` |
| 5 | `EPROCESS+0x170` = 0 인가 | K1 로그에 안쪽이 안 찍힘 | `nt!MmCreatePeb+0x2b8` 에서 `db @r13+0x170 L1` |
| 6 | `PspLocateInPEManifest` 가 불리나 (낮음 — 결과는 같음) | 사용자 쪽 플래그 값 | `PspSetupUserProcessAddressSpace +0xfa` 에서 `db @rsi+0x10 L1` |
| 7 | 옵션 셋째 칸 12~13·24~25 의 2 출처 | 입력 표 값 (Q113 (a) 와 이음) | `PspApplyMitigationOptions` 입구 `dq @r8 L3` vs 출구 |
| 8 | leap second `Enabled`·`Count` | 이 VM 레지스트리 값 | `dq nt!ExLeapSecondData L1` |
| 9 | ETW 갈래 | KUSD·`TracingFlags` 실행 값 | `ntdll!LdrpInitializationComplete+0x2c` 에서 `db 7ffe0384 L2` |
| 10 | 스핀 셋이 0 인가 `0x2800` 인가 | CPU 수 | `ntdll!LdrpInitializeProcess+0x20d` 에서 `dd @$peb+b8 L1` |

- 읽기 명령은 run 문서 "아직 확인 필요" 와 baggage 7.19 에서 옮겼다. 짐 지도 7.19 는 1·2·5·6·7·9·10 을 "멈춤점이 필요", 3·4·8 을 "전역 읽기 (덤프로도 될 것 `[추측]`)" 로 갈랐다.

### 원래 있던 항목에서 보는 것

- 두 깃발 (`SecurityCookieInitialized`·`SpecialMachineFramesInitialized`) 의 **실행 값** → **Q43** (동적). 이번엔 1 로 쓰는 줄만 다시 확인했다.
- `PsDefaultLoaderThreads` 값 → **Q79** (덤프). `PspGetProcessParameterOverrides` 는 안 읽는다는 것만 이번에 닫혔다.
- `+0x2574` 복사본 갈래를 test.exe 가 안 탄다는 것 → **Q35** (동적, run 문서 정정 절).
- 첫 `0x24` 조회가 **실행에서도** `+0x12f` 쪽인지 콜스택으로 본 적은 없다. run 문서는 "코드 순서상" 으로 적었고, 이것만 따로 묻는 대기열 항목은 없다.

---

## 5. 더 읽기

- run 문서: [`run59-init-leftovers.md`](run59-init-leftovers.md)
  - 1절 PE_emulator 용 한 줄 표 + 표 밑 syscall 사슬 · 2절 짐 지도용 표 · 3절 함수별 차례 (`.asm` 줄)
  - "확인된 것" D · B · C · E, "정정", "짐 지도 반영", "아직 확인 필요"
- 요청서: [`_workspace/sast/54_init-leftovers.md`](../../_workspace/sast/54_init-leftovers.md)
- 결과서: [`_workspace/sast/54_init-leftovers_result.md`](../../_workspace/sast/54_init-leftovers_result.md) — 질문 4a (쿠키 식) · 5 (`RtlEncodePointer`) · 6 (flush 표) · 8 (`BitField` 표) · 13 (옵션 표)
- flow.md: [2.1 커널 nt](flow.md) (`PspAllocateProcess`·`PspSetupUserProcessAddressSpace`·`PspPrepareSystemDllInitBlock`) · 2.4 ntdll 로더 시작 (`LdrpInitialize` 세 함수 본문·run51 쿠키 절) · 4장 run59 줄 넷
- 짐 지도: [`baggage.md`](baggage.md) 2장 · 3.8 · 3.9 · 3.11 · 6장 · 7.19
- 이전 해설: [run17](../02_sprint/run17-ldr-init-entry-해설.md) (입구 잡일·깃발) · [run30](../02_sprint/run30-exe-crt-start-해설.md) (보안 쿠키) · [run51](run51-eprocess-later-해설.md) (열쇠·게으른 만들기) · [run58](run58-registry-strings-해설.md) (Q109 첫 일)

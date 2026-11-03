# run61 random-ex-init — 해설

> 이 글은 [run61 문서](run61-random-ex-init.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q116** — PE_emulator 요청 9: "`RtlpRandomExInit` 을 힙 전에 누가 켜나" (`_workspace/plan.md`, 이번에 **Q116 끝**)
  - 사용자 결정 (2026-11-03): 맨 앞, 작고 급함. PE_emulator 52 바퀴가 이 답을 기다리며 멈춰 있었다.
  - 다음 항목: **Q104 VM 단계** → Q78d4 → Q78e → … (plan "다음 항목")
  - 남은 질문이 간 곳: 동적 확인 1 · 2 · 4 → **Q104 VM 단계**, "P1a ~ P1b 사이에 `RtlRandom` 을 또 부른 주인" → 새 **Q117** (맨 뒤).
- 근거:
  - run 문서 [`run61-random-ex-init.md`](run61-random-ex-init.md) (가장 중요)
  - 요청서 [`_workspace/sast/56_random-ex-init.md`](../../_workspace/sast/56_random-ex-init.md)
  - 결과서 [`_workspace/sast/56_random-ex-init_result.md`](../../_workspace/sast/56_random-ex-init_result.md) (판정: **분석성공**, PDB 일치)
  - flow.md 변경: 기준 · 그림 2 `p2h` 상자 글 · 2.4 힙 절 · 2.4 run51 쿠키 절 · 4장 · 4.1 · 5장
  - baggage.md 변경: 2장 숫자 · 3.8 새 줄 7 · 3.11 · 4장 · 5장 · 6장 · 7.3 · 새 **7.21**
  - 고쳐진 옛 문서: [`run46-process-heap.md`](run46-process-heap.md) (정정), [run46 해설](run46-process-heap-해설.md) (정정)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P0.txt` ~ `_P2.txt`) 를 **다시 읽은 것** 이다. 새 실행이 아니다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 한 주소에 이름 여럿 (`RtlRandom` = `RtlRandomEx`) → [run9 해설](../02_sprint/run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭)
- `LOCK CMPXCHG` (발판에 먼저 오른 한 명만 0 → 1 로 고친다) → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.6
- ntdll 전역 = 관리실 **칠판** → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md)
- `LdrSystemDllInitBlock` = 관리실 **업무 수첩** (`+0x98` = 커널이 미리 적어 둔 난수) → [run32 해설](../02_sprint/run32-params-initblock-해설.md), [run59 해설](run59-init-leftovers-해설.md) 2.5
- `KUSER_SHARED_DATA` (KUSD) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 정보 클래스 `0x24` · 프로세스 쿠키 = 관리사무소가 깎아 주는 가게 **열쇠** → [run51 해설](run51-eprocess-later-해설.md) 2.2 · 2.8, [run59 해설](run59-init-leftovers-해설.md) 2.3
- `RDRAND` (CPU 가 직접 난수를 뽑아 주는 명령, "CPU 주사위") → [run59 해설](run59-init-leftovers-해설.md) 2.5
- 프로세스 힙 = **창고**, 힙 관리자 = **창고 관리실**, 힙 구간 시스템 콜 표, 꼬리 JMP → [run46 해설](run46-process-heap-해설.md)
- 사진 P0 · P1 · P1a · P1b · P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

이번에 처음 나오는 말 (**한 번만 하기 (run-once)**, **난수 벡터**) 은 본문에서 처음 나오는 자리에서 한 줄로 푼다. `RDRAND` 는 run59 해설에서 이미 풀었지만, 이번 글에서 나오는 자리에 한 줄로 다시 붙인다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**.
- 시스템 콜 = 관리사무소 **신청 창구**. 프로세스 쿠키 = 관리사무소가 깎아 주는 가게 **열쇠** (그 번호 4바이트).
- 프로세스 힙 = 가게 **창고**, 힙 관리자 = **창고 관리실** (run46 해설).
- run 문서가 이번에 쓴 비유는 **주사위 상자** 다. 이 글도 그대로 쓴다:
  - `RtlRandom` = 관리실의 **주사위 상자**. 누가 "난수 하나" 하면 이 상자에서 꺼내 준다.
  - 상자 안에는 주사위 **128개** 가 들어 있어야 한다 (난수 벡터, 2.4).
  - 상자 뚜껑에 **팻말** 이 붙어 있다: "아직 안 넣음 / 넣는 중 / 다 넣음" (한 번만 하기, 2.2).
  - run 문서는 "본사에 우리 가게 번호를 묻는다" 고 썼다. 이 글은 앞 해설과 맞춰 "**관리사무소 창구에 열쇠 번호를 묻는다**" 로 쓴다. 같은 뜻이다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| 진짜 Windows 에선 힙 전에 누가 `RtlpRandomExInit` 을 켜 두나? | **아무도 안 켠다.** 파일 값 0 그대로 힙 구간에 들어온다 `[정적]` |
| 그럼 처음 켜는 곳은? | 힙 구간 **맨 앞**: `RtlInitializeHeapManager +0xd → RtlHpGlobalsInitialize +0x4c → RtlRandom` 첫 호출 `[정적]` |
| 켤 때 시스템 콜은? | `RtlpInitRandomExVector` 안 `NtQueryInformationProcess(-1, 0x24, …, 4)` **하나** (call `+0x24` / ret `+0x29`) `[정적]` |
| run46 은? | **이 하나를 빠뜨렸다.** 힙 구간 시스템 콜 12 → **13** (무작위 몫 n = 0 이면 12), `NtQueryInformationProcess` 1 → **2** `[정적]` |
| PE_emulator 에 줄 답 | **힙 안에서 켜는 게 맞다.** 파일 값 0 그대로 두고, 힙 구간 `NtQueryInformationProcess` 약속을 **2번** 으로 |
| 사진 | P1a · P1b · P2 에서 `RtlpRandomExInit` = **2** (run-once 끝 값과 같음) `[관찰]` run44 + `[정적]` |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run46 이 본 **창고 관리실 차리기** (묶음 7) 의 맨 첫 줄이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk                      (사진 P1)
 │    └─ LdrpInitializeProcess
 │         ├─ +0x134  RtlEncodePointer        열쇠 번호 첫 질문 (run59)
 │         ├─ +0x536  LdrpInitializeExecutionOptions   (IFEO — test.exe 는 건너뜀)
 │         ├─ +0x9f2  RtlInitializeHeapManager         창고 관리실 차리기 (run46)
 │         │     └─ +0x12  RtlHpGlobalsInitialize
 │         │           └─ +0x51  RtlRandom 첫 호출     ← 이번 바퀴 (주사위 상자 처음 열기)
 │         │     …
 │         ├─ +0xa50  LdrpInitializeProcessHeap        창고 짓기 (run46)
 │         ├─ …       kernel32 준비                     (사진 P1a)
 │         └─ … DLL 올리기 · 켜기 → NtContinue 은퇴      (사진 P2)
 └─ RtlUserThreadStart → (CRT) → main    (범위 밖)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준).
- 이번 자리는 사진 **P1 과 P1a 사이** 다. 그래서 사진만으로는 "언제 켜졌나" 를 못 가르고, 정적으로 가른다 (2.7).

### 무엇이 궁금했나

PE_emulator 가 진짜 ntdll 의 `RtlInitializeHeapManager` 를 처음 돌려 봤다. 그랬더니 run46 표와 다른 게 나왔다.

- run46 표: 힙 구간에서 `NtQueryInformationProcess` 는 **1번** (`RtlpGetModifiedProcessCookie +0x27`).
- PE_emulator: **2번**. 더 나온 첫째는 돌아올 자리가 ntdll `+0x95f45` = `RtlpInitRandomExVector +0x29` 였다.

설명은 둘 중 하나다.

- (가) 진짜 Windows 에선 힙 **전에** 누가 `RtlpRandomExInit` 을 켜 둔다. 그래서 진짜에선 이 호출이 안 나온다. PE_emulator 는 그 "앞에서 켜기" 를 빠뜨렸다.
- (나) 진짜 Windows 에서도 힙 구간에서 처음 켠다. **run46 이 이 호출을 빠뜨렸다.**

이번 바퀴는 이 둘을 가르는 것이다 (요청서 1 ~ 5).

### 바퀴 전에 알던 것

- `[정적]` (run46) 힙 구간 시스템 콜 12개, `NtQueryInformationProcess` 1번 = #3 `RtlpGetModifiedProcessCookie +0x27`. `RtlHpGlobalsInitialize` 의 `RtlRandom` 두 번은 "시스템 콜 없이".
- `[관찰]` (PE_emulator) 첫 `NtQueryInformationProcess` 인자 `(-1, 0x24, 스택 주소, 4, 0)`, 돌아올 자리 `0x180095f45`.
  - 진짜 ntdll 코드를 Unicorn 에서 돌린 것이라 **26100.9457 VM 에서 본 것은 아니다** (요청서).
- `[정적]` (run59) 이 프로세스의 첫 `NtQueryInformationProcess(0x24)` 는 `LdrpInitializeProcess +0x12f` 의 `RtlEncodePointer` 다.
- `[관찰]` (run44 → baggage 7.3) `RtlpRandomExInit` 은 "쓴 자리 모르는 전역" 의 `≤P1a` 목록 (88개) 안에 있었다. 즉 P1a 사진엔 이미 값이 있었는데, 누가 썼는지는 몰랐다.

---

## 2. 찾은 것

### 2.1 한눈에 — 상자를 처음 여는 사슬

run 문서 "호출 관계" 를 대표만 줄였다. `+0x..` 는 ret 자리, 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄이다.

```
LdrpInitializeProcess
 ├─ +0x134  RtlEncodePointer(0)                 NtQueryInformationProcess 0x24 — 프로세스 첫 0x24 (run59)
 │  …
 └─ +0x9f2  RtlInitializeHeapManager                                              [정적]
          ├─ +0x12  RtlHpGlobalsInitialize
          │        ├─ +0x51  RtlRandom (첫 호출, 팻말 0)
          │        │        │  +0x34  CMPXCHG  팻말 0 → 1
          │        │        ├─ +0x53  RtlpInitRandomExVector
          │        │        │        ├─ +0x29  NtQueryInformationProcess 0x24   ← 힙 구간 시스템 콜 #1 (run61 새로)
          │        │        │        │  +0x87  난수 벡터 128칸 채우기
          │        │        │        │  +0xc5  RtlpRandomExAuxVarY
          │        │        ├─ +0x6c  RtlRunOnceComplete      팻말 1 → 2
          │        └─ +0x61  RtlRandom (둘째, 팻말 2 → 곧장 난수, 시스템 콜 없음)
          ├─ +0xa2  RtlpGetModifiedProcessCookie
          │        └─ +0x27  NtQueryInformationProcess 0x24   ← #4 (run46 옛 #3)
          └─ …
```

- 같은 `NtQueryInformationProcess(0x24)` 가 프로세스 전체로 **3번** 나온다: `+0x134` 쪽 (run59) → 위 `+0x29` → 위 `+0x27` `[정적]`.
- 둘째 `RtlRandom` (`+0x61`) 은 팻말이 이미 2 라서 시스템 콜이 없다 `[정적]`.
- 콜스택 기대 (첫 시스템 콜에서 `k`) `[정적]`: `RtlpInitRandomExVector+0x29` ← `RtlRandom+0x53` ← `RtlHpGlobalsInitialize+0x51` ← `RtlInitializeHeapManager+0x12` ← `LdrpInitializeProcess+0x9f2` ← `_LdrpInitialize+0x16c`.
  - run46 해설 2.5 에서 "큰 예약 때 콜스택엔 `RtlInitializeHeapManager` 가 **없다** (꼬리 JMP)" 고 했다. 여기선 **보인다.** 꼬리 JMP (`+0x1a0`) 보다 한참 앞이라서다.
  - 디버거가 `RtlRandom` 을 `RtlRandom` 으로 보일지 `RtlRandomEx` 로 보일지는 `[추측]` (같은 주소 이름 둘).

### 2.2 새 말 — 한 번만 하기 (run-once)

- **한 번만 하기 (run-once)**: 여러 스레드가 동시에 와도 "준비" 를 **딱 한 번만** 하게 해 주는 칸. 칸 값의 **아래 두 비트** 가 상태다 (결과서 1번, `RTL_RUN_ONCE` 꼴).
  - 비유: 주사위 상자 뚜껑의 **팻말**. "아직 안 넣음" 이면 처음 온 사람이 "넣는 중" 으로 뒤집고 주사위를 채운다. 다 채우면 "다 넣음" 으로 뒤집는다. 다른 사람은 팻말만 보고 기다리거나 바로 쓴다.
- 이번 칸: `RtlpRandomExInit` (ntdll `.data`, rva `0x1ce878`, 8바이트). **파일 값 0** — `.data` 디스크 크기 `0x2000` 너머라 0 으로 채워지는 쪽이다 `[정적]`.

팻말 넷 (`RtlRandom +0xa` 가 읽어서 가른다) `[정적]`:

| 팻말 (`& 3`) | 뜻 | `RtlRandom` 이 하는 일 |
|---|---|---|
| **0** | 아직 안 넣음 (파일 값) | `+0x34 CMPXCHG.LOCK` 으로 0 → 1 잡기. 이긴 쪽만 `+0x4e` 채우기 → `+0x67` 끝내기 (→ 2). 진 쪽은 `+0x40 JNZ → +0x29` 로 다시 읽음 |
| **1** | 넣는 중 (다른 스레드) | `+0x2f JNZ → +0x77`, `+0x7a JZ → +0x152` → `RtlpRunOnceWaitForInit` (call `+0x15c` / ret `+0x161`) 으로 기다렸다가 `+0x161 JMP → +0x29` 다시 읽음 |
| **2** | 다 넣음 | `+0x27 JZ → +0x9a` 곧장 난수. 둘째 호출부터 전부 이 길 |
| **3** | 비동기 (여기선 잘못) | `+0x86 MOV EAX,0xc00000f0` → `RtlReportCriticalFailure` (call `+0x95` / ret `+0x9a`). 정상에선 안 옴 |

- test.exe 첫 스레드가 혼자 도는 동안엔 팻말 1 을 볼 일이 없다 (기다리는 길 안 탐) `[정적]`.
- run59 해설 2.4 의 **번호표 기계** (`XADD`, "1번만 일한다") 와 하는 일은 비슷하다. 도구가 다르다: 거기는 번호표, 여기는 `CMPXCHG` 로 팻말을 뒤집는다.

### 2.3 한 줄 읽기 — `RtlRandom +0x34` (팻말을 0 → 1 로 뒤집는 줄)

- **누가**: ntdll `RtlRandom` (같은 주소 PDB 이름 `RtlRandomEx`, rva `0x95db0`).
- **어디서**: `+0x34`. 명령은 `CMPXCHG.LOCK [RtlpRandomExInit],RDI` (RDI = 1) `[정적]`.
- **누구를**: 칠판 칸 `RtlpRandomExInit`.
- **무엇을 위해**: "팻말이 아직 0 이면 1 로 바꿔라, 그 사이 누가 바꿨으면 바꾸지 마라" 를 한 번에 한다. 성공한 스레드 **하나만** 아래 `+0x4e` 로 내려가 주사위를 채운다.
  - `LOCK CMPXCHG` 는 [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.6 의 "발판에 먼저 오른 한 명" 과 같은 꼴이다.
- **조건**: 그 앞 `+0x24 CMP DL,2` 가 2 가 아니고, `+0x2f JNZ` 를 안 탔을 때 (= 팻말 0).
- **결과**: 팻말 1. 사진에는 안 남는다 (곧 2 로 바뀌니까).

이 칸을 건드리는 줄은 ntdll 전체에서 **`RtlRandom` 안 5곳뿐** 이다 `[정적]` (`_refs/RtlpRandomExInit.tsv`):

- `+0xa` 읽기 · `+0x34` 읽고 쓰기 · `+0x45` · `+0x56` · `+0x152` 주소 넘기기.
- 포인터 표로 이 칸을 가리키는 곳은 0곳.

### 2.4 새 말 — 난수 벡터, 그리고 `RtlpInitRandomExVector` 가 하는 일

#### 새 말

- **난수 벡터**: `RtlRandom` 이 난수를 꺼낼 때 섞어 쓰는 **미리 채워 둔 난수 128칸짜리 표**. 이름은 `RtlpRandomExConstantVector` (ntdll `.data`, rva `0x1d39c0 ~ 0x1d3bc0`, 128 dword = `0x200` 바이트) `[정적]`.
  - 비유: 상자 안의 **주사위 128개**. 상자를 처음 열 때 한 번 채우고, 그 뒤엔 꺼낼 때마다 한 개씩 새것과 바꿔 넣는다.
- 옆에 붙은 칸 하나: `RtlpRandomExAuxVarY` (rva `0x1d3bc0`, dword) `[정적]`. 벡터를 채운 뒤 한 번 더 돌린 값이 들어가고, 그 뒤엔 `RtlRandom` 이 부를 때마다 결과를 더한다.

#### `RtlpInitRandomExVector` (rva `0x95f1c`, 218바이트) — 네 걸음 `[정적]`

1. **열쇠 번호 묻기** — `NtQueryInformationProcess(-1, 0x24, &지역 4바이트, 4, NULL)`, call `+0x24` / ret `+0x29`.
   - 이 함수의 시스템 콜은 **이것 하나뿐** 이다.
   - 실패하면 `+0x2b JS → +0xd3` 에서 `RtlRaiseStatus` (call `+0xd5` / ret `+0xda`).
2. **씨앗 만들기** — 받은 4바이트 XOR 업무 수첩 `LdrSystemDllInitBlock +0x98` (`+0x3d`).
   - 수첩 `+0x98` 은 커널이 미리 적어 둔 난수다. run59 가 "읽는 곳 다섯" 으로 센 것 중 하나다 (결과서 3번).
3. **주사위 128개 채우기** — 루프 `+0x5a ~ +0x92` (카운트 `0x80`, `+0x4e`). 한 바퀴마다 `x = (x × 0x7fffffed + 0x7fffffc3) mod 0x7fffffff` 를 돌리고, 그 값을 벡터 한 칸에 쓴다 (`+0x87`).
4. **한 번 더** — 한 번 더 돌린 값을 `RtlpRandomExAuxVarY` 에 쓴다 (`+0xc5`). 그리고 1 (성공) 을 돌려준다 (`+0xaa`).

- 인자 셋 (RunOnce, Parameter, Context) 은 **안 쓴다** (`+0x1c` 에서 RCX 를 덮음) `[정적]`.
- 팻말 `RtlpRandomExInit` 은 **안 건드린다.** 팻말을 2 로 뒤집는 건 부른 쪽 `RtlRandom` 이다 (2.5).
- 비유: 상자를 처음 연 사람이 관리사무소 창구에 "**우리 가게 열쇠 번호** 뭐예요?" 하고 한 번 묻는다. 그 번호에 업무 수첩 숫자를 섞어 씨앗을 만들고, 그 씨앗으로 주사위 128개를 차례로 깎아 상자에 넣는다.

#### 한 줄 읽기 — `RtlpInitRandomExVector +0x24` (call) / `+0x29` (ret)

- **누가**: `RtlpInitRandomExVector`.
- **어디서**: call `+0x24`. 콜스택에 보일 자리는 ret `+0x29` (= PE_emulator 가 본 `0x180095f45`, 결과서가 정적과 같다고 확인) `[정적]`.
- **누구를**: `NtQueryInformationProcess` — 관리사무소 창구.
- **무엇을**: 인자 다섯이 각각 이 줄들에서 놓인다 `[정적]`:
  - 핸들 `-1` (= 나 자신) ← `+0x1c OR RCX,-1`
  - 클래스 `0x24` ← `+0x20 LEA EDX,[R9+0x20]` (R9 = 4 라서 4 + `0x20` = `0x24`)
  - 받을 자리 = 지역 4바이트 `[RSP+0x58]` ← `+0xb`
  - 길이 4 ← `+0x16`
  - 반환 길이 `NULL` ← `+0x10` (`[RSP+0x20]` 에 0)
- **무엇을 위해**: 씨앗 재료 (열쇠 번호). 클래스 `0x24` 이름 `ProcessCookie` 는 공개 헤더 이름이라 `[추측]` (run51 해설 2.2 그대로).

#### 상자를 열 때마다 하는 일 (참고만)

난수 계산 자체는 요청서 범위 밖이라 쓰는 칸만 적혔다 `[정적]`:

- 벡터 한 칸 (`AuxVarY & 0x7f`) 을 새 값과 맞바꿔 꺼낸다 (`+0xe6`).
- KUSD `+0x290` 이 0 이 아니면 `RDRAND` 를 최대 10번 돌려 섞는다 (`+0x103`).
  - `RDRAND` = CPU 가 직접 난수를 뽑아 주는 명령 `[추측: 일반적으로 알려진 설명]`. 비유로는 **CPU 주사위** (run59 해설 2.5).
  - KUSD `+0x290` 은 `ProcessorFeatures` (`+0x274`) 의 28번째 칸이다. 그 칸 이름 `PF_RDRAND_INSTRUCTION_AVAILABLE` 은 `[추측]` (결과서 4번).
- 결과를 `AuxVarY` 에 더한다 (`+0x11f ADD.LOCK`).

### 2.5 팻말을 2 로 뒤집는 줄 — `RtlRunOnceComplete +0x67`

- `RtlRandom` 이 채우기를 마치면 `RtlRunOnceComplete(&RtlpRandomExInit, 0, 0)` 을 부른다 (call `+0x67` / ret `+0x6c`) `[정적]`.
- 그 안 `+0x67 XCHG [R11],R8` 이 팻말 칸에 **2** 를 쓴다. R11 = 넘겨받은 `&RtlpRandomExInit`, flags 0 이라 값 = `2 | Context 0` = 2 `[정적]`.
  - 헷갈리기 쉬운 점: `RtlRandom` 의 call 자리도 `+0x67`, `RtlRunOnceComplete` 안 `XCHG` 자리도 `+0x67` 이다. **다른 함수의 같은 숫자** 다.
- 되돌리기 갈래도 있다: `RtlpInitRandomExVector` 가 0 을 돌려주면 `+0x5f JZ → +0x136` 에서 flags 4 로 불러 (call `+0x13b` / ret `+0x140`) 팻말을 0 으로 되돌린다. 하지만 이 함수는 실패 때 `RtlRaiseStatus` 로 가지 0 을 돌려주지 않아서 **안 탄다** `[정적]`.
- 비유: 주사위를 다 넣은 사람이 직접 팻말을 뒤집는 게 아니라, **팻말 담당** (`RtlRunOnceComplete`) 을 불러 "다 넣음" 으로 바꿔 달라고 한다.

### 2.6 힙 전에 켜는 길은 없다 — 단계별로

이게 이번 바퀴의 갈림길 ((가) 냐 (나) 냐) 이다. 천천히 본다.

1. **근거 ① — 팻말을 바꾸는 줄은 `RtlRandom` 안뿐이다** `[정적]`.
   - 2.3 의 5곳 + 그 안에서 주소를 넘겨받는 `RtlRunOnceComplete +0x67` · `RtlpRunOnceWaitForInit +0x4a`. 다른 함수는 이 칸에 손을 못 댄다.
   - 그러니 "힙 전에 켜졌나" = "힙 전에 `RtlRandom` 이 불렸나" 와 같은 질문이 된다.
2. **근거 ② — `RtlRandom` 을 부르는 곳은 함수 셋, 호출 넷** `[정적]` (`_refs/RtlRandom.tsv`).

   | 호출 자리 (call / ret) | 힙 전에 도나 |
   |---|---|
   | `RtlHpGlobalsInitialize +0x4c / +0x51` · `+0x5c / +0x61` | 아니오 — 힙 구간 안 (`+0x9ed` 이후). **이게 첫 호출** |
   | `RtlpCreateIFEOKeyFilterKey +0x7e / +0x83` | 길은 있지만 **안 열린다** (아래 3) |
   | `RtlHeapTrkInitialize +0x1a0 / +0x1a5` | 아니오 — 내보내기만 있고 ntdll 안에서 부르는 곳 0 |

   - 간접 · 꼬리 호출도 없다. 나머지 참조는 내보내기 표 슬롯 둘 (서수 1421 `RtlRandom` · 1422 `RtlRandomEx`, 둘 다 `0x95db0`) · `.pdata` · CFG 표뿐이다.
3. **근거 ③ — 앞쪽에서 닿는 유일한 길 (IFEO 필터 키) 은 닫혀 있다** `[정적]`.
   - 길: `LdrpInitializeProcess +0x531 → LdrpInitializeExecutionOptions +0xc5 → RtlpOpenImageFileOptionsKeyEx +0x184 → RtlpCreateIFEOKeyFilterKey +0x7e → RtlRandom`.
   - 문이 두 겹으로 닫혀 있다:
     - 바깥 문: test.exe 는 `LdrpInitializeExecutionOptions +0xa6` · `+0xad JNZ` (매개변수 `Flags & 0x4000`) 로 이 call 자체를 건너뛴다 (run46, run46 해설 2.6 의 "서랍 확인 건너뜀").
     - 안쪽 문: 설령 들어가도 `+0xbd XOR R8D,R8D` 로 "만들기 표시" 가 0 이라, `RtlpOpenImageFileOptionsKeyEx +0x2a2 JZ` 에서 `+0x184` 를 건너뛴다.
   - 지도 안에서 `RtlpOpenImageFileOptionsKeyEx` 를 부르는 다른 곳도 모두 바로 앞에 `XOR R8D,R8D` 다.
4. **근거 ④ — run59 쿠키 사슬도 안 닿는다** `[정적]`: `InitSecurityCookie` · `LdrInitSecurityCookie` · `LdrpGenRandom` · `LdrInitializePerThreadRng` · `RtlEncodePointer` · `LdrInitializeMrdata` 는 `RtlRandom` · `RtlpInitRandomExVector` · `RtlpRandomExInit` 에 닿지 않는다.
   - 이름이 비슷해서 헷갈릴 수 있는데, run59 의 "CPU 주사위" (`LdrpGenRandom`) 와 이번 "주사위 상자" (`RtlRandom`) 는 **다른 도구** 다.
5. **근거 ⑤ — 힙 구간 길은 늘 탄다** `[정적]` (결과서 5(a)):
   - `LdrpInitializeProcess +0x9ed` 를 건너뛰는 점프는 실패 출구 (`+0xa7b`, `+0x1e3b`) 뿐이다.
   - `RtlInitializeHeapManager +0xd` 는 함수 맨 처음 call, 조건 없음.
   - `RtlHpGlobalsInitialize +0x0 ~ +0x5c` 사이 분기 0.
6. **그래서**: 이 프로세스에서 `RtlRandom` 이 처음 불리는 곳 = `RtlHpGlobalsInitialize +0x4c`. 그때 팻말은 파일 값 0 → 상자를 채우며 `NtQueryInformationProcess(0x24)` 를 한 번 부른다. 답은 **(나) run46 이 빠뜨렸다** `[정적]`.
7. **아직 확인 아님**: 실제로 멈춰서 "그 순간 팻말 0 → 1 → 2" 를 본 건 아니다 → Q104 VM 단계 (4절).

- 비유: "상자를 누가 먼저 채워 뒀나?" 를 찾으려고 상자 뚜껑에 손댈 수 있는 사람 명단을 뽑았다. 명단에 셋뿐이다. 하나는 창고 관리실 직원 (힙 구간), 하나는 서랍 담당 (문이 두 겹으로 잠겨 못 옴), 하나는 이 가게엔 출근도 안 하는 사람. 그러니 처음 여는 사람은 창고 관리실 직원이다.

### 2.7 사진과 맞춰 보기 — 사진은 "끝 값" 만 보여 준다

`RtlpRandomExInit` 의 실제 주소 = ntdll 베이스 `0x7ffafb660000` + `0x1ce878` = `7ffa'fb82e878` (run44 부팅 기준).

| 사진 | 지점 | 값 | 로그 줄 |
|---|---|---|---|
| P0 | `PspUserThreadStartup` 입구 | 읽기 불가 `????` | `04_baggage-read_P0.txt:3706` |
| P1 | `LdrInitializeThunk` 입구 | 읽기 불가 `????` | `04_baggage-read_P1.txt:3705` |
| P1a | `LdrpInitializeProcess +0x199d` | **`0x2`** | `04_baggage-read_P1a.txt:4097` |
| P1b | `+0x1bb6` | `0x2` | `04_baggage-read_P1b.txt:5215` |
| P2 | `NtContinue` 입구 | `0x2` | `04_baggage-read_P2.txt:5227` |

#### 왜 사진만으로는 못 가르나 — 단계별로

1. 요청서가 세운 기준: "P1 에 0 이고 P1a 에 0 아니면 → 힙 구간에서 켬".
2. 그런데 P0 · P1 사진은 ntdll `.data` **9쪽 전부** (`0x1cc000 ~ 0x1d4000`) 가 읽기 불가였다 `[관찰]`. 기준의 앞 반 ("P1 에 0") 을 못 채운다.
3. 그래서 가르는 건 2.6 의 정적 근거다.
4. 사진이 주는 건 맞장구다: P1a 값 `2` 가 run-once **끝 값** (`2 | Context 0`) 과 딱 맞는다 `[관찰]` + `[정적]`. 그리고 힙 구간 (`+0x9ed`) 은 P1 ~ P1a 사이라 구간 안이다.
5. 같이 맞는 것 `[관찰]`: 난수 벡터는 P1a 에 이미 채워져 있다 (`619336c3`, `7e5153be` …, `_P1a.txt:5398`).

- 비유: 가게 문 닫기 전 사진 (P1a) 엔 팻말이 "다 넣음" 이다. 아침 사진 (P1) 은 그 칸이 흐려서 안 보인다. 그래서 "아침엔 '안 넣음' 이었나" 는 사진으론 모르고, **뚜껑에 손댈 수 있는 사람 명단** (정적) 으로 정한다.

#### 덤 — P1a 와 P1b 사이에도 누가 주사위를 꺼냈다

- `RtlpRandomExAuxVarY` (`7ffa'fb833bc0`): P1a `0xc899afee` (`_P1a.txt:5430`) → P1b · P2 `0xb6cd45cc` (`_P1b.txt:6548`, `_P2.txt:6560`) `[관찰]`.
- 이 칸을 쓰는 줄은 `RtlpInitRandomExVector +0xc5` 와 `RtlRandom +0x11f` 뿐이다 `[정적]`. 그러니 **P1a ~ P1b 사이에 `RtlRandom` 이 또 불렸다** (결과서).
- run 문서 (flow-writer) 는 로그 줄을 더 비교해서, 같은 구간에 벡터가 **두 칸** 바뀐 것도 봤다 `[관찰]`:
  - `7ffa'fb833b20` (벡터 `+0x160`) `6c75474a` → `0025bf3c`
  - `7ffa'fb833b78` (벡터 `+0x1b8`) `3aecf307` → `7ffde723`
- 한 번 부를 때 한 칸만 바꾸니 (`+0xe6`) **최소 두 번** 불린 것으로 보인다 `[추측]` (같은 칸을 두 번 골랐으면 더 많을 수도). P1b ~ P2 사이엔 두 칸 다 그대로.
- **누가** 불렀는지는 `[공백]`. ntdll 안 caller 는 2.6 표의 셋뿐이라, 다른 모듈이 내보내기 `RtlRandomEx` / `RtlRandom` 을 부른 것 `[추측]` (결과서). → 새 **Q117**.

### 2.8 run46 이 빠뜨린 것 — 바뀐 숫자

#### 무엇이 틀렸나

- run46 은 `RtlHpGlobalsInitialize` 의 `RtlRandom` 두 번을 "**시스템 콜 없이**" 로 적었다.
- 실제로는 **첫 호출만** 팻말이 0 이라 `RtlpInitRandomExVector` 안 `NtQueryInformationProcess(0x24)` 를 한 번 부른다 `[정적]`.
- `RtlHpGlobalsInitialize` 가 `RtlInitializeHeapManager` 의 첫 call 이라 이 호출은 구간 **맨 앞** 이다. 그래서 새 **#1** 이 된다.
- 왜 놓쳤을까: 이번 바퀴 전엔 `RtlpInitRandomExVector` 의 `.asm` 이 없었다 (요청서 "`.asm` 없음"). 그래서 run46 때 그 안을 못 들여다봤을 것으로 보인다 `[추측]` — 결과서도 "첫 호출에서만 도는 … syscall 을 못 봤다" 까지만 적었다.

#### 숫자 전 → 후 (run 문서 "정정", run46 문서 맨 앞 표)

| 무엇 | 전 (run46) | 후 (run61) |
|---|---|---|
| 힙 구간 시스템 콜 | 12개 (n = 0 이면 11개) | **13개** (n = 0 이면 12개) |
| 그중 `NtQueryInformationProcess` | 1 (#3) | **2** (#1 · #4) |
| 번호 | #1 ~ #12 | 맨 앞에 새 #1 `RtlpInitRandomExVector +0x29`, 옛 #n → **#n+1** (옛 #3 `RtlpGetModifiedProcessCookie` = 새 #4, 옛 #11 `NtFreeVirtualMemory` = 새 #12) |
| "힙 주소 무작위" 줄 | 시스템 콜 없이 만든다 | 첫 `RtlRandom` 만 `NtQueryInformationProcess(0x24)` 1번, 그 뒤 난수는 시스템 콜 없이 |
| 프로세스 전체 `0x24` | (run59) `+0x134` 첫째, 힙 관리자 쪽 둘째 | `+0x134` → `RtlpInitRandomExVector` → `RtlpGetModifiedProcessCookie` 순서 **3번** |

- 합 13 = `NtOpenKey` 2 · `NtQueryInformationProcess` 2 · `NtQuerySystemInformation` 3 · `NtAllocateVirtualMemoryEx` 3 · `NtAllocateVirtualMemory` 2 · `NtFreeVirtualMemory` 1 (run46 문서 고친 요약 줄).
- 셋 다 같은 커널 열쇠 번호를 받는다고 보지만, 값이 정말 같은지는 동적 `[추측]`.
- [run46 해설](run46-process-heap-해설.md) 본문도 이 숫자로 고쳤다 (옛 값은 취소선).

### 2.9 PE_emulator 에 줄 답

**힙 안에서 켜는 게 맞다** (결과서 5(d), run 문서 맨 앞).

- `RtlpRandomExInit` 은 파일 값 0 그대로 둔다. 진짜 `RtlRandom` 이 `RtlHpGlobalsInitialize +0x4c` 에서 처음 켜게 둔다.
- 힙 구간 `NtQueryInformationProcess` 약속은 **2번**: 첫째 `RtlpInitRandomExVector +0x29` (클래스 `0x24`), 둘째 `RtlpGetModifiedProcessCookie +0x27`.
- 거꾸로 미리 2 로 켜 두면? 시스템 콜이 한 번 빠지고, 난수 벡터 (`0x200` 바이트) · `RtlpRandomExAuxVarY` 가 진짜와 달리 **0 으로 남는다.** 주사위 없는 빈 상자에 "다 넣음" 팻말만 붙인 꼴이다.

에뮬레이터가 맞춰 줄 입력 셋:

1. 업무 수첩 `LdrSystemDllInitBlock +0x98` (커널이 쓰는 난수 4바이트) 이 이미 채워져 있어야 한다 — `RtlpInitRandomExVector +0x3d` 가 읽는다 `[정적]`.
2. 이 시스템 콜이 돌려줄 4바이트는 앞서 `RtlEncodePointer +0x48` 이 받은 것과 **같은 열쇠 번호** 가 자연스럽다 `[추측]` (같은 값인지는 동적).
3. KUSD `+0x290` 이 0 이 아니면 `RtlRandom +0x103` 에서 `RDRAND` 가 돈다 (최대 10번) `[정적]`. Unicorn 이 `RDRAND` 를 어떻게 다루는지에 따라 갈린다.

맞춰 볼 상태 (힙 구간 뒤 = run44 P1a 사진) — 값 자체는 씨앗 (열쇠 번호 · 커널 난수 · 틱) 에 달려 실행마다 다르니, "**0 이 아님 / 2**" 만 맞추면 된다:

| 칸 | 힙 전 | 힙 구간 뒤 |
|---|---|---|
| `RtlpRandomExInit` (`0x1ce878`, 8B) | 0 (파일 값) | **2** (`[관찰]` P1a) |
| `RtlpRandomExConstantVector` (`0x1d39c0`, `0x200`B) | 0 | 128칸 모두 채워짐 (`[관찰]` P1a) |
| `RtlpRandomExAuxVarY` (`0x1d3bc0`, 4B) | 0 | 0 아님 (`[관찰]` P1a `0xc899afee`) |

### 2.10 결과서와 run 문서가 다르게 적은 곳 (뜻은 같음)

- **run46 그림에 넣은 줄**: 결과서는 call 자리로 `+0x4c RtlRandom → +0x4e RtlpInitRandomExVector (#1)` 를 넣으라고 했다. run46 그림은 ret 자리 기준이라 `+0x51` → `+0x53` → `+0x29` 로 넣었다 (call 자리는 같은 줄의 `+0x4c` · `+0x4e` · `+0x24`).
- **P1a ~ P1b 의 `RtlRandom` 횟수**: 결과서는 "또 불렸다" 까지, run 문서는 벡터 두 칸 변화를 더 읽어 "최소 두 번" `[추측]` 까지 적었다 (2.7 덤).
- 이 글의 숫자는 모두 run 문서 쪽이다.

### 2.11 정리 — 비유로 한 번 더

1. 창고 관리실이 문을 열자마자 (힙 구간 맨 앞) 주사위가 필요해서 **주사위 상자** (`RtlRandom`) 를 연다.
2. 뚜껑 팻말이 "아직 안 넣음" (0, 파일 값) 이다. 그 사람이 팻말을 "넣는 중" (1) 으로 뒤집는다 (`+0x34`).
3. 관리사무소 창구에 **열쇠 번호** 를 한 번 묻고 (`NtQueryInformationProcess 0x24`), 업무 수첩 숫자와 섞어 씨앗을 만든다.
4. 씨앗으로 주사위 **128개** 를 깎아 상자를 채우고, 덤으로 한 개 더 굴려 옆 칸 (`AuxVarY`) 에 적는다.
5. 팻말 담당을 불러 "다 넣음" (2) 으로 바꾼다 (`RtlRunOnceComplete +0x67`).
6. 두 번째부터는 팻말만 보고 바로 꺼낸다. 창구에 다시 안 묻는다.
7. run46 은 "이 상자는 창구에 안 묻는다" 고 적었다. **처음 열 때만** 묻는다. 그 한 번이 힙 구간의 새 #1 이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 그림 2 `p2h` 상자 글 | "힙 관리자 — 난수·키, IFEO 첫 열기, PEB 힙 목록 3칸 준비 … 큰 예약 둘" | 끝에 "run61: 맨 앞 첫 RtlRandom 이 난수 벡터를 켬 — NtQueryInformationProcess 0x24 한 번" 을 붙임. 상자·화살표 수는 그대로 |
| 2.4 힙 절 트리 | `+0x12 RtlHpGlobalsInitialize … — 시스템 콜 없음` | 그 아래 두 줄: `+0x51 RtlRandom (첫) → +0x53 RtlpInitRandomExVector … NtQueryInformationProcess 0x24 (+0x29) — 구간 첫 syscall` · `+0x61 RtlRandom (둘째, 상태 2 — syscall 없음)` |
| 2.4 힙 절 머리 | 시스템 콜 12개 | ~~12개~~ **13개** (run61 정정) |
| 2.4 힙 절 새 단락 | — | `[정적]` (run61) "RandomEx 난수 벡터를 처음 켜는 곳 = 이 구간 맨 앞" |
| 2.4 run51 쿠키 절 | 힙 관리자 쪽 `0x24` = "둘째" | 그 앞에 `RtlpInitRandomExVector +0x29` 줄 (둘째), 힙 관리자 쪽은 **셋째**. 프로세스 전체 `0x24` 3번 |
| 4장 공백 표 | (Q116 줄 없음) | Q116 **(해결됨)** + 새 줄 넷: 동적 확인 (→ Q104 VM 단계), P1a ~ P1b `RtlRandom` 주인 `[공백]`, 정정, 결과서와 다르게 적은 곳 |
| 4.1 `(정적)` 상자 표 | `p2h` 줄 | 상자 글을 붙였다는 메모 + "꼬리 JMP 전이라 콜스택에 `RtlInitializeHeapManager` 가 보여야 함" |
| 5장 | run46 줄 "시스템 콜 12개 · `NtQueryInformationProcess` 1" | ~~12개~~ 13개 · ~~1~~ 2 (run61 정정) + run61 줄 둘 |

태그 변화 대표:

- `RtlHpGlobalsInitialize` 의 `RtlRandom`: "시스템 콜 없음" (`[정적]` run46) → **첫 호출만 `NtQueryInformationProcess(0x24)` 1번** (`[정적]` run61).
- `RtlpRandomExInit` 을 켜는 곳: 모름 (요청서 `[추측]` "힙 전에 누가 켰을 것") → **`[정적]` 힙 구간 맨 앞, 힙 전엔 길 없음**.

### 짐 지도 `baggage.md` 에서 바뀐 것 (열일곱째판)

- 3.8 `.data` 에 **7줄** (줄 끝 "Q116 으로 닫힘"):
  - `RtlpRandomExInit` 3줄: `RtlRandom +0x34` (0 → 1) · `RtlRunOnceComplete +0x67` (1 → 2) · `RtlpRunOnceWaitForInit +0x4a` (조건부, 안 탐)
  - `RtlpRandomExConstantVector` 2줄: `RtlpInitRandomExVector +0x87` (채우기) · `RtlRandom +0xe6` (맞바꾸기)
  - `RtlpRandomExAuxVarY` 2줄: `RtlpInitRandomExVector +0xc5` · `RtlRandom +0x11f`
- 숫자: `.data` 아는 것 91 → **94**, 모름 347 → **344**, ntdll 이 씀 90 → **93**. ntdll 전역 3구역 합 아는 것 113 → **116**, 모름 380 → **377**. 7.3 `.data` `≤P1a` 88 → **85**.
- 6장 공용 부품: `RtlRandom` 줄 "시스템 콜 없음" → "첫 호출만 1번" + 새 부품 줄 둘 (RandomEx 벡터, 한 번만 하기).
- 3.11 `Cookie` · 4장 쿠키 짝 · 5장 ② 에 `RtlpInitRandomExVector` 를 이음. 3.8 `0x1ce430` 줄의 "구간 시스템 콜 #1" → #2.
- 새 **7.21**: 이 바퀴의 동적 질문 1 ~ 4.

### 큰 그림에서 어떻게 이어지나

- **PE_emulator 52 바퀴가 다시 갈 수 있다.** "앞에서 미리 켜 줘야 하나?" 가 아니라 "진짜 코드가 하게 두고, 약속 횟수만 2로" 가 답이다.
- 힙 구간의 시스템 콜 표 (run46) 가 다시 맞춰졌다. 에뮬레이터가 시스템 콜 순서를 약속으로 쓰는 만큼, 한 줄 빠진 게 바로 멈춤으로 이어졌다. 이번 정정으로 그 표는 13줄이 됐다.
- 짐 지도 쪽: "P1a 엔 값이 있는데 누가 썼는지 모름" 이던 전역 셋이 주인과 쓴 줄을 갖게 됐다. 쓰는 줄이 모두 힙 구간 맨 앞이다.
- 새로 생긴 실마리 하나: 초기화 뒤쪽 (P1a ~ P1b) 에 ntdll 밖의 누군가가 `RtlRandom` 을 부른다. 지도의 다른 모듈 쪽을 볼 때 이어질 거리다 (Q117).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **1** `RtlRandom` 첫 히트의 돌아올 자리가 정말 `ntdll+0x95641` (`RtlHpGlobalsInitialize +0x51`) 인가, 팻말이 `RtlInitializeHeapManager` 입구 (`ntdll+0x9c334`) 에서 0 → `RtlRandom +0x53` (`ntdll+0x95e03`) 에서 1 → `+0x6c` (`ntdll+0x95e1c`) 에서 2 인가 | 정적 바퀴. P0 · P1 사진은 `.data` 가 읽기 불가라 "힙 전 0" 을 사진으로 못 봄 | **Q104 VM 단계** (동적, 급하지 않음 — 정적 근거로 닫힘) |
| **2** `RtlpInitRandomExVector +0x29` (`ntdll+0x95f45`) 에서 받은 열쇠 번호가 `RtlEncodePointer +0x48` · `RtlpGetModifiedProcessCookie +0x27` 값과 같은가 | 실행 때 값. 받은 4바이트는 지역 칸이라 사진에 안 남음 | **Q104 VM 단계** (run46 D5 와 같이) |
| **3** P1a ~ P1b 사이 `RtlRandom` 을 또 부른 **주인**, 몇 번 불렸나 (최소 두 번 `[추측]`) | ntdll 안 caller 는 셋뿐이라 다른 모듈로 보이지만 `[추측]`. 누가인지는 콜스택이 필요 | 새 **Q117** (동적: `bp ntdll!RtlRandom` 셋째 이후 히트 `k`, 또는 `ba w4 ntdll+0x1d3bc0`. 정적으로 먼저 거르려면 kernelbase · kernel32 import 에서 `RtlRandom*` 찾기) |
| **4** 멈춤점 정리 (`RtlInitializeHeapManager` 입구 · `RtlRandom +0x53` / `+0x6c` · `RtlpInitRandomExVector +0x29`) | — (1 ~ 3 을 한 번에 보기 위한 묶음) | **Q104 VM 단계** (test.exe 조건은 기존 약속 그대로) |
| 디버거가 `RtlRandom` 을 어느 이름 (`RtlRandom` / `RtlRandomEx`) 으로 보이나 | 같은 주소 이름 둘. 표시 규칙은 실행해 봐야 앎 `[추측]` | Q104 VM 단계에서 `k` 볼 때 같이 |
| KUSD `+0x290` 칸의 이름 (`PF_RDRAND_INSTRUCTION_AVAILABLE`), test.exe 때 그 값 | 이름은 공개 이름 짐작 `[추측]`, 값은 이번에 안 봄 | 근거 문서에 대기열 번호 없음 |

- 정적으로 남은 것: **없음** (run 문서 "아직 확인 필요").
- 대기열 순서 (plan): **Q104 VM 단계** → Q78d4 → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q117 은 맨 뒤.

---

## 5. 더 읽기

- run 문서: [`run61-random-ex-init.md`](run61-random-ex-init.md) — "PE_emulator 쪽에서 쓸 것" (1 시스템 콜 약속, 2 팻말 표, 3 `RtlpInitRandomExVector`, 4 기대 값), 호출 지점 표, 질문별 답, 사진 대조, 호출 관계, 정정, 짐 지도 반영, 아직 확인 필요
- 결과서: [`_workspace/sast/56_random-ex-init_result.md`](../../_workspace/sast/56_random-ex-init_result.md) — 질문 1 (쓰는 곳 5곳), 2 (앞쪽 길 없음, IFEO 길), 3 (본문), 4 (갈래), 5(a)~(d) (run46 다시 보기, 사진, 정정할 줄, PE_emulator 답)
- 요청서: [`_workspace/sast/56_random-ex-init.md`](../../_workspace/sast/56_random-ex-init.md) — "무슨 일이 있었나" (PE_emulator 멈춤 보고)
- 고친 옛 문서: [`run46-process-heap.md`](run46-process-heap.md) 맨 앞 표 (새 #1), [run46 해설](run46-process-heap-해설.md) 2.10 · 2.12
- 흐름도: [`flow.md`](flow.md) — 그림 2 `p2h`, 2.4 `#### 프로세스 힙 만들기 — 묶음 7` (run61 단락), 2.4 run51 쿠키 절, 4장 run61 줄들, 5장 run61 줄
- 짐 지도: [`baggage.md`](baggage.md) — 3.8 `RtlpRandomEx*` 7줄, 6장 `RtlRandom` · 한 번만 하기 부품, 7.21
- 진행표: `_workspace/plan.md` Q116 (끝) · Q104 · Q117, 결정 기록 2026-11-03
- 앞 해설: [run46](run46-process-heap-해설.md) (창고 관리실 · 시스템 콜 표 · 꼬리 JMP) · [run59](run59-init-leftovers-해설.md) 2.3 ~ 2.5 (열쇠와 봉인 스티커 · 번호표 · `RDRAND`) · [run51](run51-eprocess-later-해설.md) 2.8 (열쇠 깎기) · [run17](../02_sprint/run17-ldr-init-entry-해설.md) 2.6 (`LOCK CMPXCHG`) · [run44](run44-baggage-snap-해설.md) (사진 지점)

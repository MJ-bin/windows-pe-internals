# run25 dll-entry — 해설

> 이 글은 [run25 문서](run25-dll-entry.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **정정 (run37)**: 2.5 의 "102개 별명 중에도 KERNELBASE 로 풀리는 게 있을 수 있다 `[추측]`" 은 맞았다 — 같은 OS 의 별명 사전 파일(`apisetschema.dll`)로 풀면 99개가 kernelbase, 3개가 ntdll `[정적: PE 표]`. 2.5 "어느 길로 **처음** 들어오나 `[공백]`" 과 4절 같은 줄은 닫혔다 — **길 A**: 목록 #3 이 본명 `KERNELBASE.dll` 이고, 앞의 #0·#1(별명)·#2(`ntdll.dll`)는 모두 ntdll, 별명으로 kernelbase 를 가리키는 첫 줄은 #4 다 `[정적: PE 표]`. 실행 때도 그런지와 실행 때 사전 값은 그대로 동적 Q20. 본문은 그대로 두었다 → [run37 해설 2.3](run37-k32-import-order-해설.md#23-질문-답--처음-들이는-줄은-3번-본명-그대로) · [2.5](run37-k32-import-order-해설.md#25-새로-오는-건물은-kernelbase-하나)

- 대기열: **Q13** — 2.5 KERNELBASE·KERNEL32 시작 함수 이름 (PE 진입점 → PDB 이름) (`_workspace/plan.md`, 이번에 **끝**)
  - 새 항목: **Q51** (정적, 작음) — kernel32 import 서술자 **순서**. 4절에서 다시 나온다.
  - 출처를 더한 항목: Q18 · Q21 · Q22 · Q48 · Q49 (모두 이번 숫자로 "무엇을 보면 되나" 가 또렷해졌다).
  - 다음 항목: Q14 (plan "다음 항목").
- 근거:
  - run 문서 [`run25-dll-entry.md`](run25-dll-entry.md) (가장 중요)
  - 요청서 [`_workspace/sast/20_dll-entry.md`](../../_workspace/sast/20_dll-entry.md)
  - 결과서 [`_workspace/sast/20_dll-entry_result.md`](../../_workspace/sast/20_dll-entry_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - kernel32 (`kernel32.dll`, 26100.9457) 는 **이번에 처음** 열었다. pdb GUID+Age `AE9E9D8F7AC1467A645320FDC23235D01`, 함수 2633개.
  - KernelBase 는 run14 때 만든 분석을 다시 썼다 (pdb `72CCE506944DE0267C172AD64287F9BD1`).
  - 이번엔 함수 몸통 말고 **PE 표**(진입점·import·export)도 `pefile` 로 직접 읽었다.

이 글은 [run24 해설](run24-work-queue-해설.md) 의 **다음 편**이다. 그런데 자리는 조금 옮긴다.

- run22 ~ run24 는 건물을 **들여오는**(짐 들이기) 이야기였다.
- 이번엔 들여온 건물의 **전원을 켤 때 누르는 스위치**, 그리고 main 으로 가는 길의 **출근 인사 담당**을 본다.
- 이번 run 은 **작다.** 핵심은 셋뿐이다. 짧게 가자.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출, ICF 별칭 → [run9 해설](run9-create-user-process-해설.md) 2절
- rva, PEB → [run10 해설](run10-allocate-process-해설.md) 2절
- PDB 공개 이름 (**출석부**) → [run17 해설 2.3](run17-ldr-init-entry-해설.md#23-이름이-비슷한-세-함수--누가-누구인가)
- 꼬리 점프 (콜스택에 안 쌓이는 넘어가기) → [run17 해설 2.4](run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리)
- 내보내기 표 export (**호실 이름표**) → [run16 해설 2.9](run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다)
- 전달 항목 forwarder (**"옆 건물 몇 호로 가세요"**), `Kernel32ThreadInitThunkFunction` 칸 (**벽 주소록 "출근 인사 담당" 칸**) → [run20 해설](run20-kernel32-load-해설.md) 2.5
- CFG 와 thunk (**교환원**) → [run20 해설 2.9](run20-kernel32-load-해설.md#29-910-마지막-인사--kernelbasepostinit)
- import 표 (**기대는 건물 목록**), import 서술자 (그 목록의 **한 줄**) → [run22 해설 2.1](run22-map-snap-dep-해설.md#21-한눈에--ldrpmapandsnapdependency-트리)
- api-set (**별명**), 별명 → 진짜 이름 풀이 → [run23 해설](run23-load-dependent-해설.md) 2.5
- `DLL_PROCESS_ATTACH` (= 이유 1, "프로세스에 처음 붙음") → [run19 해설 2.11](run19-proc-init-modules-해설.md#211-exe-tls-콜백--전원-다-켠-뒤에)
- csrss (**인사팀 사무실**) → [run14 해설 2.6](run14-create-process-internal-해설.md#26-3-그-사이-부모가-하는-일--검사--csrss-알림--후처리)

비유도 그대로 이어 쓴다.

- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- DLL = **건물**. kernel32 = 꼭 필요한 **기본 설비 건물**, KERNELBASE = 그 옆 **본 설비 건물**
- 짐 들이기 = DLL 올리기, **전원 켜기 = DLL 시작 함수 부르기** (run19)
- PDB = **출석부**, export 표 = 건물 입구 **호실 이름표**, import 표 = **기대는 건물 목록**, api-set = **별명**
- `BaseThreadInitThunk` = kernel32 의 **출근 인사 담당** (run19·run20)
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 셋**:

1. **시작 함수 이름** — 두 DLL 의 PE 진입점 rva 와 PDB 이름이 **정확히 같다** `[정적]`. run8 은 "**근처**" 로 봤다 `[관찰]`. 왜 둘이 다른지는 `[추측]` 만 있고, run8 원본 로그가 없어서 지금은 못 따진다 → Q21 에서 다시 본다 → 2.3
2. **kernel32 는 `KERNELBASE.dll` 을 이름 그대로 import 한다** (별명 102개와 따로) `[정적]`. run20 부터의 "kernelbase 가 따라 들어온다" 에 **별명 풀이 없이도 되는 길**이 하나 더 생겼다. 어느 쪽이 먼저인지는 Q51 → 2.5
3. **`BaseThreadInitThunk` 는 전달 항목(forwarder)이 아니라 kernel32 안 진짜 코드**다 (rva `0x2cd70`) `[정적]`. main 으로 가는 길(flow.md 2.10)의 한 칸이 단단해졌다 → 2.6

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다. 이번 자리는 **두 군데**다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)              (run17 ~ run19)
        ├─ +0x19a2  LdrpInitializeKernel32Functions                                 (run20)
        │     └─ +0xc0 LdrLoadDll("KERNEL32.DLL") → … (짐 들이기, 전원 켜기)         (run20 ~ run24, run7 관찰)
        │           └─ LdrpInitializeNode +0x19c  LdrpCallInitRoutine               (run8 관찰)
        │                 ├─ ? → KERNELBASE 시작 함수                               ← 이번 ①
        │                 └─ ? → KERNEL32 시작 함수                                 ← 이번 ①
        ├─ +0x1aa9 … dll1·dll2 짐 들이기                                             (run22 ~ run24)
        ├─ +0x1b5e  Kernel32ThreadInitThunkFunction 칸의 함수 (1,0,0)               (run19·run20) ← 이번 ③ (칸 값 후보)
        └─ +0x1bbb … dll2 → dll1 전원 켜기                                          (run19)
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → BaseThreadInitThunk → … → main    ← 이번 ③ (뒤쪽은 추측)
```

- ① 은 flow.md **2.5** (kernel32 준비), ③ 은 **2.10** (진입점에서 main 까지) 이다.
- ② (kernel32 import 표) 는 ① 의 "짐 들이기" 안쪽 이야기다. kernel32 를 들일 때 **kernelbase 가 왜 같이 오나**.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. `LdrpCallInitRoutine` 이 부르는 KERNELBASE·KERNEL32 시작 함수의 **진짜 이름**은? (run8 은 "근처" 였다)
2. 두 시작 함수가 이유 1(프로세스 시작)일 때 **무엇을 하나** (대표만). KERNEL32 시작 함수가 KERNELBASE 를 부르나?
3. kernel32 의 import 이름은 **별명(api-set)** 인가, **`KERNELBASE.dll` 본명**인가?
4. ntdll 이 이름으로 찾는 세 함수(run20)는 export 에 있나, **전달 항목**인가?

### 바퀴 전에 알던 것

- `[관찰]` (run8) `LdrpCallInitRoutine` (`LdrpInitializeNode+0x19c`) 의 `rcx` 가 `KERNELBASE!KernelBaseDllInitialize` **근처**, `KERNEL32!BaseDllInitialize` **근처** (정확히 일치 아님). run8 표에 `[확인 필요]` 로 남았다.
- `[관찰]` (run8) dll1·dll2 쪽은 `_DllMainCRTStartup` → `DllMain` 으로 **정확히 일치**했다.
- `[정적]` (run20) ntdll 이 이름으로 찾는 함수 셋: kernel32 `BaseThreadInitThunk`·`TermsrvGetWindowsDirectoryW`, kernelbase `KernelbasePostInit`. `Kernel32ThreadInitThunkFunction` 칸 값 = kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소.
- `[추측]` (run20) 그 주소가 정말 `BaseThreadInitThunk` **본체**인가 — 전달 항목이면 다른 곳일 수 있다.
- `[추측]` (flow.md 2.5) "kernel32 를 들이면 kernelbase 가 따라 들어온다". kernel32 import 표를 **안 봐서** 별명인지 본명인지 몰랐다 (run21 ~ run24 해설에서 계속 남긴 말).
- `[관찰]` (01_sprint) Worker 스레드 종료 콜스택에 `RtlUserThreadStart+0x2c` → `BaseThreadInitThunk+0x20` 이 보였다. 메인 스레드에선 직접 안 봤다.

---

## 2. 찾은 것

### 2.1 한눈에

run 문서 "호출 관계" 를 줄였다. 숫자는 run 문서 그대로. 모두 `[정적]`.

```
LdrpCallInitRoutine (ntdll, run8 관찰 자리. 들어가는 건 아직 ?)
 ├─ ? → KERNELBASE!KernelBaseDllInitialize   (rva 0x5060 = PE 진입점)
 │       ├─ +0xd   KernelBaseBaseDllInitialize                    모든 이유
 │       │      └─ +0x44 꼬리 JMP → _KernelBaseBaseDllInitialize
 │       │                   이유 1: … +0x2d9 CsrClientConnectToServer …
 │       └─ 이유 1: +0xbf WilInitialize → … → +0xea AppContainerRegisterInit
 └─ ? → KERNEL32!BaseDllInitialize            (rva 0x2c5d0 = PE 진입점)
         └─ +0x22  _BaseDllInitialize                              모든 이유
                 이유 1: … +0x224 _Kernel32LegacyLib_BaseDllInitialize
                                   └─ +0x20 KERNELBASE!KernelBaseGetGlobalData   (import)

KERNEL32!BaseThreadInitThunk (rva 0x2cd70, export, forwarder 아님)
 ├─ 첫 인자 0:  +0x17 간접 호출 RDX(R8) (CFG 스텁) → +0x20 RtlExitUserThread
 └─ 첫 인자 ≠0: +0x2d RtlGetSuiteMask → (AL & 0x10) +0x3b BasepInitializeTermsrvFpns
```

- `?` 화살표 둘은 그대로 `?` 다. **실제로 들어가는 걸 본 사람은 아직 없다** (동적 Q21).
- 시작 함수 안쪽은 대표만 골랐다. 전부는 run 문서 "호출 지점" 표에 있다.

### 2.2 한 줄씩 읽는 법

run 문서 "호출 지점" 표에서 둘을 골랐다.

#### 한 줄 읽기 ① — KERNEL32 가 KERNELBASE 를 부르는 줄

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `KERNEL32!_Kernel32LegacyLib_BaseDllInitialize` | `+0x19` | `+0x20` | **`KERNELBASE!KernelBaseGetGlobalData`** (import, 이유 1 갈래) | [정적] |

- **누가**: kernel32 시작 함수의 **두 칸 아래** 함수 `_Kernel32LegacyLib_BaseDllInitialize`. (진입점 `BaseDllInitialize` → `_BaseDllInitialize` `+0x224` → 이 함수.)
- **어디서**: 자기 시작에서 `+0x19` 에서 부르고, 돌아오면 `+0x20`. 콜스택엔 `_Kernel32LegacyLib_BaseDllInitialize+0x20` 으로 찍힐 자리다.
- **누구를**: KERNELBASE 의 `KernelBaseGetGlobalData`. kernel32 import 표의 **`KERNELBASE.dll` 항목**을 거쳐 부른다.
- **무엇을 위해**: 이름으로 보면 KERNELBASE 쪽 공용 데이터를 받아 오는 것 같다 `[추측: 이름]`.
- 비유: kernel32 건물이 전원을 켜는 도중에 **옆 KERNELBASE 건물에 전화**를 건다. 그러니 그 전화를 받으려면 KERNELBASE 가 **먼저 켜져 있어야** 한다 `[추측]` (flow.md 2.5 의 "KERNELBASE 를 먼저 켠다" `[추측]` 과 맞는다).

#### 한 줄 읽기 ② — 출근 인사 담당이 퇴근시키는 줄

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `KERNEL32!BaseThreadInitThunk` | `+0x19` | `+0x20` | ntdll `RtlExitUserThread` (첫 인자 0 갈래) | [정적] |

- **누가**: kernel32 의 `BaseThreadInitThunk` (출근 인사 담당).
- **어디서**: `+0x19` 에서 부르고, 돌아올 자리는 `+0x20`.
- **누구를**: ntdll 의 `RtlExitUserThread`. 이름 그대로 스레드를 끝내는 함수다.
- **무엇을 위해**: 바로 앞 `+0x12` 에서 직원의 **본업**(시작 함수)을 부르고, 본업이 돌아오면 여기서 직원을 **퇴근**시킨다.
- 이 `+0x20` 이 01_sprint 콜스택의 `BaseThreadInitThunk+0x20` 과 **같은 숫자**다. 2.6 에서 다시 본다.

### 2.3 핵심 ① 시작 함수 이름 — 정적은 "정확히 같다", 관찰은 "근처"

#### 새 말 — PE 진입점 (`AddressOfEntryPoint`)

- **PE 진입점** = exe·DLL 파일 머리(PE 헤더)에 적힌 "**이 파일을 시작할 때 여기부터 실행하라**" 는 주소 한 칸 (rva). DLL 이면 로더가 전원을 켤 때 부르는 **시작 함수**가 이 주소다 `[추측]` (일반적으로 알려진 설명).
- 비유: 건물 설계도 첫 장에 적힌 "**전원 스위치는 몇 호에 있음**".
- 이름이 아니라 **주소만** 적혀 있다는 게 중요하다. 그 주소에 **누가 사는지**는 출석부(PDB)를 봐야 안다.

#### 정적으로 본 것 `[정적]`

| 모듈 | PE `AddressOfEntryPoint` | 그 주소의 PDB 이름 | 같은 주소 다른 이름 | run8 이 적은 이름의 rva | 차이 |
|---|---|---|---|---|---|
| kernel32 | `0x2c5d0` | `BaseDllInitialize` | 없음 | `BaseDllInitialize` = `0x2c5d0` | **0** |
| KernelBase | `0x5060` | `KernelBaseDllInitialize` | 없음 | `KernelBaseDllInitialize` = `0x5060` | **0** |

한 단계씩:

1. 설계도 첫 장(PE 헤더)에서 스위치 호실 번호를 읽었다: kernel32 `0x2c5d0`, KernelBase `0x5060`.
2. 출석부(PDB)에서 그 호실에 누가 사나 찾았다: `BaseDllInitialize`, `KernelBaseDllInitialize`.
3. 같은 호실에 다른 이름은 **없다** (별칭 아님).
4. → run8 이 `ln` 으로 본 **바로 그 두 이름이 스위치 자리의 주인**이다.

- 도구 쪽 주의 하나: Ghidra 는 이 두 함수를 PDB 이름 대신 **`entry`** 라고 부른다 (스위치 자리에 자기 스티커를 먼저 붙인다). 그래서 PDB 이름으로 찾으면 0개가 나온다. 글에선 PDB 이름을 쓴다.

#### 이름이 거의 같은 이웃 — 헷갈리지 말 것 `[정적]`

- kernel32: 진입점 `BaseDllInitialize`(`0x2c5d0`) 바로 뒤 `+0x40` 에 **밑줄 붙은** `_BaseDllInitialize`(`0x2c610`) 가 있다. 실제 일은 이쪽이 한다.
- KernelBase: 진입점 `KernelBaseDllInitialize`(`0x5060`) 와 이름이 거의 같은 `KernelBaseBaseDllInitialize`(`0x51d0`, `+0x170`), `_KernelBaseBaseDllInitialize`(`0x4e6c0`) 는 진입점이 부르는 **아래 함수**다.
- 비유: 스위치 방 바로 옆방에 이름이 한 글자 다른 사람이 산다.

#### 그런데 run8 은 "근처" 라고 했다 — 둘이 다른 이유는 모른다

나란히 놓으면:

- run8 `[관찰]`: `ln @rcx` 가 두 이름 "**근처** (정확히 일치 아님)".
- run25 `[정적]`: 진입점 rva 가 두 이름과 **정확히 같다**.

먼저 **안 맞는 설명**부터 지운다 `[정적]`:

- "진입점이 다른 이름의 작은 함수다" → 아니다. 그 호실 주인은 딱 그 이름 하나다.
- "진입점이 이름 없는 다리 함수(thunk)다" → 아니다. 같은 이유.

남는 짐작은 둘이다. **둘 다 `[추측]`** 이다.

- **새 말 — `ln`**: 주소 하나를 주면 그 **앞뒤의 가장 가까운 이름**을 보여 주는 WinDbg 명령 `[추측]` (일반적으로 알려진 설명, run 문서·결과서도 이렇게 적었다). 비유: "이 주소는 ○○호 **근처**예요" 하고 앞뒤 집 문패를 같이 읽어 주는 안내원.

1. `[추측]` `ln` 출력 모양을 "근처" 로 적었을 수 있다.
   - 근거: kernel32 는 `BaseDllInitialize` 바로 뒤 `+0x40` 에 `_BaseDllInitialize` 가 있다.
   - 그래서: `ln` 이 두 이름을 같이 띄웠을 것이고, 그걸 "근처" 로 받아 적었을 수 있다.
   - 아직 확인 아님: 출력 원문이 없다.
2. `[추측]` 또는 그때 `rcx` 가 **진입점이 아닌 다른 값**이었을 수 있다.

어느 쪽인지 **지금은 못 따진다.** 이유는 하나다.

- run8 원본 로그(`run8-dllcheck.log`)가 **레포에 없다.** `ln` 이 실제로 뭐라고 찍었는지 다시 볼 수가 없다.

그래서 이렇게 정리한다.

- run8 이 **틀렸다는 뜻이 아니다.** run8 은 관찰 기록이라 그대로 둔다.
- 노선도(정적)는 "스위치 = 이 이름" 이라고 말한다. 버스(동적)에서 본 건 "근처" 였다. **같은 걸 본 건지조차 아직 모른다.**
- 다시 버스를 탈 때(**Q21**) 출력을 **그대로** 남기고, 주소에서 모듈 시작을 빼 본다. `ntdll!LdrpCallInitRoutine` 입구 (조건 `r8 == 1`, test.exe) 에서:

```
ln @rcx
? @rcx - kernelbase
? @rcx - kernel32
```

- 정적 예측: KERNELBASE 차례면 `0x5060`, KERNEL32 차례면 `0x2c5d0`. 숫자가 이렇게 나오면 짐작 1 쪽, 다르면 짐작 2 쪽이다.

### 2.4 곁가지 — 두 시작 함수 안은 dll1·dll2 와 모양이 다르다

짧게만 본다. 전부 `[정적]` 이고, 순서·조건은 **점프를 읽은 것**이다. 실행 때 그대로 타는지는 동적 (Q21).

- **새 말 — 이유(reason)**: DLL 시작 함수의 둘째 인자. "왜 부르나" 를 숫자로 준다. 1 = 프로세스에 처음 붙음 ([run19 해설 2.11](run19-proc-init-modules-해설.md#211-exe-tls-콜백--전원-다-켠-뒤에) 의 `DLL_PROCESS_ATTACH`). 두 진입점 모두 `EDX` 에서 읽는다 (kernel32 `entry +0xa`, KernelBase `entry +0x6`).

**모양**:

- dll1·dll2 는 `_DllMainCRTStartup` → `DllMain` 이었다 (run8 `[관찰]`).
- 두 시스템 DLL 은 **그 모양이 아니다.** 아주 작은 CRT 조각 `_DllMainCRTStartupForGS2` (24바이트, 이유 1 일 때 보안 쿠키 `__security_init_cookie` 만 준비) 를 거쳐 **바로 전용 초기화 함수**로 간다.
  - kernel32: `BaseDllInitialize` → (이유 1 일 때만) `_DllMainCRTStartupForGS2` → `+0x22` `_BaseDllInitialize`
  - KernelBase: `KernelBaseDllInitialize` → `+0xd` `KernelBaseBaseDllInitialize` → `_DllMainCRTStartupForGS2` → `+0x44` **꼬리 `JMP`** → `_KernelBaseBaseDllInitialize`
- 비유: 일반 건물(dll1·dll2)은 시공사 표준 배전반을 거쳐 스위치가 켜진다. 설비 건물 둘은 표준 배전반에서 **안전장치 하나만** 떼어 쓰고, 나머지는 **직접 만든 배선**으로 켠다.

**KERNELBASE 가 켤 때 하는 일 (대표)**:

- `_KernelBaseBaseDllInitialize` 이유 1 갈래: `+0x1b2` 메모리 관리 준비 → `+0x1d6` `RtlCreateTagHeap` → **`+0x2d9` `CsrClientConnectToServer`** → … `+0x4e6` `BaseNlsDllInitialize` → `+0x526` `ConsoleInitialize`.
- 눈여겨볼 건 **`CsrClientConnectToServer`** 다. 이름 그대로 csrss(인사팀 사무실)에 **연결**하는 함수다. run14 에서 **부모** cmd.exe 가 인사팀에 "새 가게 생겼어요" 알렸다면, 이번엔 **새 가게 자신**이 인사팀에 줄을 대는 자리가 kernelbase 시작 함수 안에 있다는 것.
- 그 뒤 진입점 자신이 이유 1 일 때: `+0xbf` `WilInitialize` → `+0xc9` `TipInitialize` → `+0xd3` `FeatureTuningInitialize` → `+0xd8` (rva `0x5ff0`, 음수면 실패로 끝냄) → `+0xea` `AppContainerRegisterInit`.

**KERNEL32 가 켤 때 하는 일 (대표)**:

- `_BaseDllInitialize` 이유 1 갈래: `+0x73` `RtlImageNtHeader` → `+0x99` `RtlSetThreadPoolStartFunc` → `+0xba` `LdrSetDllManifestProber` → … → `+0x217` `LdrDisableThreadCalloutsForDll` → `+0x224` `_Kernel32LegacyLib_BaseDllInitialize`.
- 여기서 **KERNELBASE 함수를 부르는 줄이 둘** 있다. 둘 다 kernel32 import 표의 `KERNELBASE.dll` 항목이다:
  - `_Kernel32LegacyLib_BaseDllInitialize +0x19` (ret `+0x20`) `KernelBaseGetGlobalData` — 이유 1 갈래 (2.2 한 줄 읽기 ①).
  - `_BaseDllInitialize +0x380` (ret `+0x387`) `GetRegistryExtensionFlags` — `RtlGetSuiteMask & 0x10` 이고 `+0x364` 가 통과할 때만. 터미널 서비스 갈래라 test.exe 에선 안 탈 수 있다 `[추측]`.
- 진입점 `BaseDllInitialize` **자신은** KERNELBASE 를 직접 부르지 않는다.

### 2.5 핵심 ② kernel32 는 `KERNELBASE.dll` 을 본명으로 적어 둔다

#### run20 ~ run24 동안 남아 있던 말

- "kernel32 를 들이면 kernelbase 가 따라 들어온다" `[추측]`.
- run23 에서 그 길을 코드로 하나 그렸다: kernel32 의 기대는 건물 목록에 **별명**(api-set)이 있으면, 배달 기사 안 `+0x2ff` 등에서 `KERNELBASE.dll` 로 **풀려서** 같이 올라온다 `[정적: 길]`.
- 하지만 늘 이 단서가 붙었다: "**kernel32 import 표를 안 봤다**".

#### 이번에 본 것 — kernel32 의 기대는 건물 목록 `[정적]`

| 묶음 | 개수 | 예 |
|---|---|---|
| `api-ms-win-…` 별명 | 102개 (함수 815개) | `api-ms-win-core-processthreads-l1-1-0.dll`(37), `api-ms-win-core-file-l1-1-0.dll`(73) |
| 본명 | 2개 | **`ntdll.dll`** (366개), **`KERNELBASE.dll`** (93개) |
| delay import | 3개 | `ext-ms-win-oobe-query-l1-1-0.dll`, `ext-ms-win-packagevirtualizationcontext-l1-1-0.dll`, `RPCRT4.dll` |

- 일반 import 는 모두 104줄 (= 별명 102 + 본명 2). 바운드 import 없음.
- **새 말 — delay import**: 시작 때 바로 들이지 않고 **처음 쓸 때** 들이는 목록 (run 문서 "처음 쓸 때 올림"). 시작 때는 안 올라온다는 건 일반적인 뜻에 기댄 것이다 `[추측: delay import 의 일반적 뜻]`. 비유: "필요하면 그때 부를게요" 메모. 이번 이야기에는 안 끼므로 넘어간다.

#### 그래서 무엇이 바뀌나 — 한 단계씩

1. kernel32 목록에 **`KERNELBASE.dll` 이 본명으로 한 줄** 있다 `[정적]`.
2. 건물 담당은 목록을 **한 줄씩** 배달 기사에게 맡긴다 (run22 `[정적]`).
3. 그러니 그 한 줄만으로도 **kernelbase 바퀴가 코드상 생긴다.** 별명 풀이를 거칠 필요가 없다.
4. → "kernel32 를 들이면 kernelbase 가 따라 들어온다" 는 **import 표만으로 성립**한다 `[정적]`.

- 비유: kernel32 가 들고 온 목록에 "KERNELBASE 건물" 이 **본명으로 적혀** 있었다. 별명장(ApiSet 맵)을 펼쳐 보지 않아도 KERNELBASE 를 부르게 돼 있다.
- 그리고 run23 의 별명 길은 **없어지지 않았다.** 102개 별명 중에도 KERNELBASE 로 풀리는 게 있을 수 있다 `[추측]`. 그렇다면 그 바퀴는 "이미 있나" 에서 찾힐 것이다 `[추측]`.
- 본명 줄은 별명이 아니라서 `+0x2ff` 풀이 쪽은 안 탈 것이다 `[추측: run23 조건 "이름이 api-set 이면"]`.

#### 그럼 어느 길로 **처음** 들어오나 — 아직 모른다 `[공백]`

길이 둘이 됐다.

- 길 A: 본명 `KERNELBASE.dll` 줄
- 길 B: 그보다 **앞에 있는** 별명 줄이 KERNELBASE 로 풀림

- 어느 쪽이 먼저인지는 **목록의 줄 순서**에 달렸다. 이번 결과서엔 개수만 있고 **순서가 없다** → 정적 **Q51** (kernel32 import 서술자 순서, `pefile`, 작음).
- 별명이 실제로 무엇으로 풀리나는 실행 때 별명장 값이라 동적 **Q20**.

### 2.6 핵심 ③ `BaseThreadInitThunk` 는 kernel32 안 진짜 코드다

#### run20 에서 남은 것

- 벽 주소록 "출근 인사 담당" 칸(`Kernel32ThreadInitThunkFunction`) 값 = kernel32 호실 이름표에서 **이름 `BaseThreadInitThunk` 로 찾은 주소** `[정적]` (run20).
- 남은 `[추측]`: 그 이름표 줄이 **"옆 건물 몇 호로 가세요"**(전달 항목)라면, 칸에 적힌 번호는 kernel32 가 아니라 다른 건물일 수 있다.

#### 이번에 본 것 — ntdll 이 찾는 세 이름 `[정적]`

| 이름 | 모듈 | ordinal | rva | forwarder? |
|---|---|---|---|---|
| `BaseThreadInitThunk` | kernel32 | 75 | `0x2cd70` | **아니오** (76바이트) |
| `TermsrvGetWindowsDirectoryW` | kernel32 | 1511 | `0x35f10` | **아니오** (18바이트) |
| `KernelbasePostInit` | KernelBase | 1062 | `0xf4780` | **아니오** (93바이트) |

- 어떻게 "아니오" 라고 아나: 셋 다 export 표가 가리키는 rva 가 **export 디렉터리 범위 밖의 코드 주소**다 (kernel32 export dir `0xa65f0` 크기 `0xed6c`, KernelBase `0x391ab0` 크기 `0x1006c`) `[정적]`. 같은 주소에 다른 PDB 이름도 없다.
- 일부러 찾아본 이유: 전달 항목이 드물지 않다. kernel32 export 1697개 중 211개, KernelBase 2037개 중 104개가 전달 항목이다 `[정적]`. **이 셋은 아니다.**
- 비유: 이름표의 "출근 인사 담당" 줄은 "옆 건물로" 가 아니라 **"이 건물 0x2cd70 호"** 를 바로 가리킨다. 그 방엔 실제로 76바이트짜리 사람이 앉아 있다.

#### 출근 인사 담당은 무엇을 하나 `[정적]`

인자: `ECX` 첫 인자, `RDX` 시작 주소, `R8` 파라미터 (결과서).

- **첫 인자 0** 이면:
  1. `+0x12` (ret `+0x17`) — 교환원(CFG 스텁 `0x180087010`)을 거쳐 `RDX`(시작 주소)를 `RCX = R8` 로 **간접 호출**. 직원의 본업을 시작한다.
  2. `+0x19` (ret `+0x20`) — 본업이 돌아오면 ntdll `RtlExitUserThread`. 퇴근.
- **첫 인자 0 아님** (`+0xa JNZ`) 이면:
  - `+0x26` (ret `+0x2d`) ntdll `RtlGetSuiteMask` → `AL & 0x10` 이면 `+0x36` (ret `+0x3b`) `BasepInitializeTermsrvFpns`. 본업은 **안 부르고** 터미널 서비스 쪽 준비만 한다.

#### 관찰과 숫자가 맞는다

- 01_sprint 의 Worker 스레드 종료 콜스택: `BaseThreadInitThunk+0x20` `[관찰]`.
- 이번: `+0x20` 은 `+0x19 RtlExitUserThread` 호출의 돌아올 자리 `[정적]`.
- → 그 Worker 는 "첫 인자 0" 갈래로 와서, **본업을 마치고 `RtlExitUserThread` 안에 있던 순간**이었다고 읽힌다 (flow.md 2.10).

#### 가게 세팅 때 한 번 부르는 `(1,0,0)` 은?

- run19 에서 본 `LdrpInitializeProcess` 의 `Kernel32ThreadInitThunkFunction(1,0,0)` 호출(ret `+0x1b5e`)은 첫 인자가 **1** 이다.
- 칸 값이 정말 `BaseThreadInitThunk` 라면, 이 호출은 "**첫 인자 0 아님**" 갈래 — 본업 없이 터미널 서비스 준비만 하는 쪽 — 일 것이다 `[추측: 칸 값이 BaseThreadInitThunk 라면. 값은 Q48]`.

#### main 으로 가는 길 (flow.md 2.10) 에서

```
LdrInitializeThunk +0x18  NtContinue                          [정적] run16
 ═▶ RtlUserThreadStart                                        도착은 [추측] (Q17·Q29)
     └─ +0x7  Kernel32ThreadInitThunkFunction 칸 읽기         [정적] run19
              칸 값 = kernel32 에서 이름 BaseThreadInitThunk 로 찾은 주소   [정적] run20
              그 이름표 줄 = kernel32 rva 0x2cd70, forwarder 아님           [정적] run25 ← 이번
     └─ BaseThreadInitThunk (첫 인자 0) +0x12 간접 호출 RDX(R8)            [정적] run25 ← 이번
           대상이 mainCRTStartup → main 인가                                [추측] (Q18 정적, Q30 동적)
```

- run20 까지는 "칸 = 이름으로 찾은 주소" 였다.
- 이번에 "그 이름이 **kernel32 안 진짜 코드**를 가리킨다" 와 "그 코드가 **시작 주소를 간접 호출**한다" 가 `[정적]` 으로 붙었다.
- 아직 `[추측]` 인 고리: `RtlUserThreadStart` 가 정말 이 칸을 **부르나**, 간접 호출 대상이 **`test!mainCRTStartup`** 인가.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 맨 위 기준 | kernelbase pdb 만 | + **kernel32.pdb** `AE9E9D8F7AC1467A645320FDC23235D01` (run25). 정적 근거에 "PE 표는 `pefile`", "Ghidra 는 진입점을 `entry` 로 잡음" 메모 |
| 그림 2 `d1`·`d2` 상자 | "KERNELBASE 시작 함수 / KernelBaseDllInitialize **근처**", KERNEL32 도 같음 | **PDB 이름** `KERNELBASE!KernelBaseDllInitialize` / `KERNEL32!BaseDllInitialize` + "정적: PE 진입점 rva `0x5060`/`0x2c5d0` 과 정확히 같음. run8 ln 에선 근처로 보임, 들어가는 건 미관찰". 상자는 **점선 그대로**, 화살표도 `?` 그대로 |
| 그림 2 `s1` | `KERNEL32!BaseThreadInitThunk (추측)` | "(추측 **연결**. 정적: export rva `0x2cd70`, forwarder 아님)" — 연결은 여전히 추측, 함수 자체는 정적 |
| 2.5 시작 함수 줄 | `? KERNELBASE 시작 함수` | `? KERNELBASE 시작 함수 KernelBaseDllInitialize (PE 진입점 rva 0x5060, [정적] run25)`, KERNEL32 도 같은 식 |
| 2.5 kernelbase 가 따라옴 | `[추측]` "kernel32 의 import 가 api-set 이름인가 (import 표 안 봄)" | **(해결됨)** `[정적]`: 별명 102개 + 본명 `KERNELBASE.dll`. 따라옴은 import 표만으로 성립. 남은 건 실제로 그 길로 올라오나 (Q20), 처음 바퀴가 어느 줄인가 `[공백]` (Q51) |
| 2.5 새 절 "시작 함수" | 없음 | 진입점 = PDB 이름 표, **정적과 관찰이 다르다** + `[추측]` 둘, 두 시작 함수의 모양·대표 호출, KERNEL32 → KERNELBASE 호출 둘 |
| 2.5·2.10 세 export | "forwarder 면 다른 곳" `[추측]` | **forwarder 아님** `[정적]`: `0x2cd70`·`0x35f10`·`0xf4780` |
| 2.10 | `BaseThreadInitThunk` 안은 모름 | 안쪽 두 갈래 `[정적]`, ret `+0x20` = 01_sprint 관찰 자리 |
| 4장 | 2.5 "시작 함수 정확한 이름 (Q13)" | (해결됨, 정적). 새 줄: "정적과 관찰이 다름" (Q21), "kernelbase 를 처음 들이는 바퀴" (Q51), "시작 함수 안 실행 갈래" (Q21), 2.10 `BaseThreadInitThunk` 안쪽 (Q18·Q30). Q48·Q49 멈춤점에 **예측 값** 추가 |

태그로 보면:

- **시작 함수 이름**: `[관찰]` "근처" → `[관찰]` "근처" **그대로** + `[정적]` "정확히 같음" **나란히**. 하나가 다른 하나를 덮지 않았다.
- **kernel32 → kernelbase import**: `[추측]` → `[정적]` (import 표).
- **`BaseThreadInitThunk` 본체**: `[추측]` → `[정적]` (forwarder 아님). 다만 `RtlUserThreadStart` → `BaseThreadInitThunk` **연결**은 여전히 `[추측]`.

### 큰 그림에서 어떻게 이어지나

- 2.5 (kernel32 준비) 는 이제 **노선도로 볼 수 있는 건 거의 다 봤다.** 남은 큰 질문은 "실제로 그 주소로 들어가나, 어느 바퀴가 먼저인가" 같은 **버스 타 보기**(Q20·Q21·Q22·Q49) 쪽이다. 정적으로는 Q51 (순서 하나) 정도만 남았다.
- 2.10 (main 으로 가는 길) 은 `BaseThreadInitThunk` 한 칸이 단단해져서, 남은 건 **앞 칸** `RtlUserThreadStart` (Q17) 와 **뒤 칸** 간접 호출 대상 (Q18·Q30) 이다.
- 다음 정적 항목은 **Q14** (plan "다음 항목").

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| run8 은 왜 "근처" 였나 (출력 모양 / 다른 `rcx` 값) | run8 원본 로그가 레포에 없다. 실행 때 값이라 정적으로 못 정한다 | 동적 **Q21**: `LdrpCallInitRoutine` 입구 (`r8 == 1`, test.exe) 에서 `ln @rcx` 출력 그대로, `? @rcx - kernelbase` = `0x5060`, `? @rcx - kernel32` = `0x2c5d0` 인가 |
| 실제로 진입점으로 들어가나, 콜스택 모양 | 들어가는 걸 본 적이 없다 | 동적 **Q21**: `kernelbase+0x5060`·`kernel32+0x2c5d0` 에서 `k`. 예상 `[추측]`: `KERNEL32!BaseDllInitialize+0x22` → `_BaseDllInitialize`, KernelBase 는 꼬리 `JMP` 라 `_KernelBaseBaseDllInitialize` 가 `KernelBaseDllInitialize+0xd` 바로 아래 |
| `LdrpInitializeNode` 의 [KERNELBASE]/[KERNEL32] 대응 | 실행 때 재귀 깊이 | 동적 **Q22** (같은 `ln` 으로) |
| 시작 함수 안 갈래를 test.exe 가 실제로 타나 (`_BaseDllInitialize +0x364 ~ +0x380`, `RtlGetSuiteMask` 값) | 점프만 읽었다. 값은 실행 때 | 동적 **Q21** |
| kernelbase 를 **처음** 들이는 바퀴가 본명 줄인가, 앞쪽 별명 줄인가 | 결과서에 import 줄 **순서**가 없다 `[공백]` | 정적 **Q51** (kernel32 import 서술자 순서, `pefile`) |
| 별명 102개가 실제로 어느 DLL 이 되나 | 별명장(ApiSet 맵)은 실행 때 값 | 동적 **Q20** |
| `Kernel32ThreadInitThunkFunction` 칸의 실제 값 | 실행 때 값 | 동적 **Q48**: `ln @rax` 예측 `KERNEL32!BaseThreadInitThunk` (`kernel32+0x2cd70`) |
| `TermsrvGetWindowsDirectoryW`·`KernelbasePostInit` 을 찾은 실제 값 | 실행 때 값 | 동적 **Q49**: 예측 `kernel32+0x35f10`, `kernelbase+0xf4780` |
| `BaseThreadInitThunk +0x12` 간접 호출 대상이 `test!mainCRTStartup` 인가, `BasepInitializeTermsrvFpns` 안쪽 | 간접 호출이라 대상이 실행 때 정해진다. 안쪽은 이름만 봤다 | 정적 **Q18**, 동적 **Q30** |
| `KernelbasePostInit` 이 하는 일 | 부르는 두 함수 이름만 봤다 (`BasepGetLongPathAwareRegistryState`, `BasepIsProcessLongPathAwareByManifest`). 긴 경로 설정일 것 `[추측: 이름]` | flow.md 4장 "이름·뜻 못 정한 것 (run20)" 줄 |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run25-dll-entry.md`](run25-dll-entry.md) — 특히 "진입점 rva 와 PDB 이름", "kernel32 import 표", "ntdll 이 이름으로 찾는 세 함수", "정적과 동적이 다른 곳", "아직 확인 필요"
- 결과서: [`_workspace/sast/20_dll-entry_result.md`](../../_workspace/sast/20_dll-entry_result.md) — 질문별 답 1 ~ 5
- 요청서: [`_workspace/sast/20_dll-entry.md`](../../_workspace/sast/20_dll-entry.md)
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - 그림 2 의 `d1`·`d2`·`s1`·`z1` 상자
  - "2.5" 의 run25 줄들, 새 "시작 함수" 절
  - "2.10" 의 `BaseThreadInitThunk` 줄
  - 4장 2.5·2.10 줄, 멈춤점 표 (Q21·Q22·Q48·Q49)
- 이전 해설: [run20 해설](run20-kernel32-load-해설.md) (벽 주소록 칸, forwarder), [run23 해설](run23-load-dependent-해설.md) 2.5 (별명 풀이 길), [run24 해설](run24-work-queue-해설.md) (바로 앞 편)
- 01_sprint: [`runs/01_sprint/run8-dllcheck.md`](../01_sprint/run8-dllcheck.md) 표 4·5번 줄 ("근처")

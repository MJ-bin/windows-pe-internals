# run20 kernel32-load — 해설

> 이 글은 [run20 문서](run20-kernel32-load.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q10** — 2.5 `LdrpInitializeKernel32Functions`, `LdrLoadDll`, `LdrpLoadDll` callee (`_workspace/plan.md`, 이번에 **끝**)
- 근거:
  - run 문서 [`run20-kernel32-load.md`](run20-kernel32-load.md) (가장 중요)
  - 요청서 [`_workspace/sast/15_kernel32-load.md`](../../_workspace/sast/15_kernel32-load.md)
  - 결과서 [`_workspace/sast/15_kernel32-load_result.md`](../../_workspace/sast/15_kernel32-load_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q10(끝), 새 동적 **Q49**, 출처를 더한 Q11·Q13·Q20·Q48
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll (`ntdll.dll`, 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`). run17 ~ run19 와 같은 파일이다.
  - 본문을 읽은 건 세 함수다: `LdrpInitializeKernel32Functions` (748바이트), `LdrLoadDll` (729바이트), `LdrpLoadDll` (611바이트).
  - `LdrpFindLoadedDllInternal`·`LdrpCodeAuthzInitialize` 는 갈림만 보려고 **앞쪽만** 훑었다.
  - DLL 이름·함수 이름 문자열은 **원본 바이트**로 읽었다 (2.14 에 이유).

이 글은 [run19 해설](run19-proc-init-modules-해설.md) 의 **이어지는 편**이다. run19 에서 `LdrpInitializeProcess` 뒤쪽 10묶음을 걸어 봤고, 그중 **묶음 5 "kernel32 들이기"** (`+0x19a2`) 의 안을 이번에 열었다.

- 기초 말은 다시 안 푼다. **링크만** 건다.
  - 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출, 비트와 플래그, ICF 별칭 → [run9 해설](run9-create-user-process-해설.md) 2절
  - PEB, 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
  - 전역 변수 → [run16 해설 2.4](run16-thunk-context-해설.md), **내보내기 표(export table)** → [run16 해설 2.9](run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다)
  - 모듈 목록(PEB.Ldr)·모듈 항목 → [run18 해설 2.6](run18-proc-init-setup-해설.md#26-pebldr--건물-명부가-생기는-순간-0x1c1)
  - `UNICODE_STRING`(쪽지), DDAG 노드, CFG, snap → [run19 해설](run19-proc-init-modules-해설.md) 2.1·2.6·2.5 묶음 4·2.7
  - 음수 = 실패 (상태 값) → [run12 해설 2.8](run12-insert-thread-해설.md#28-알림-받은-쪽이-안-돼-하면--psterminateprocess-로-가는-길)
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**
  - ntdll = 가게 안 **관리실**. 관리사무소는 test.exe 건물과 관리실까지만 놓아 주고, **나머지 건물은 관리실 직원이 들여온다** (run10)
  - PEB.Ldr = 가게 **건물 명부**, 모듈 항목 = **명부 한 줄** (run18)
  - 내보내기 표 = 건물 입구의 **호실 이름표**, 전역 칸에 주소를 적어 두기 = **벽 주소록**에 적기 (run16)
  - `RtlUserThreadStart` = 직원의 **본업 자리** (run16)
  - kernel32 = 가게라면 꼭 필요한 **기본 설비 건물**, `Kernel32ThreadInitThunkFunction` 칸의 함수 = kernel32 의 "**출근 인사**" (run19)
  - 짐 들이기 = DLL 올리기, 전원 켜기 = DLL 시작 함수 부르기 (run19)
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번 run 문서엔 "새 집에 전화기를 들이고 전화번호부에서 번호를 적어 두기" 비유가 붙었다. 이 글은 같은 이야기를 **가게 그림**으로 옮겨 쓴다 (가리키는 건 같다).
  - 전화번호부에서 번호 찾기 = kernel32 건물의 **호실 이름표**에서 이름으로 호실 찾기
  - 번호를 적어 두기 = 관리실 **벽 주소록 칸**에 적기

한 줄로 먼저 말하면:

- kernel32 준비는 이런 순서다 `[정적]`:
  - 명부에 KERNEL32 가 있나 본다 → 없으면 **KERNEL32.DLL 을 들인다** → 그 호실 이름표에서 **함수 두 개**를 찾아 적어 둔다 → **KERNELBASE.dll 을 들인다** (이름으로) → 보안 설정 확인 → KERNELBASE 의 **`KernelbasePostInit`** 을 찾아 부른다.
- 이번 바퀴의 **핵심 셋**:
  1. 두 `LdrLoadDll` 의 이름은 첫째 **`KERNEL32.DLL`**, 둘째 **`KERNELBASE.dll`** 이다 `[정적]`. 그런데 run7 에선 KERNELBASE·KERNEL32 **시작 함수가 둘 다 첫째 안에서** 불렸다 `[관찰]`. 이 둘이 왜 서로 부딪치지 않는지 → 2.4
  2. `BaseThreadInitThunk` 주소를 **이름으로 찾아 벽 주소록 칸에 적는 곳이 ret `+0xff`** 다 `[정적]`. 그 칸을 나중에 `RtlUserThreadStart` 가 읽는다 → **main 으로 가는 길**과 이어진다 → 2.5
  3. 이 함수엔 **exe 의 import 를 읽는 코드가 없다** `[정적]`. "kernel32 는 exe 와 상관없이 ntdll 이 스스로 불러온다" 는 `[추측]` 에 코드 근거가 하나 더 붙었다 → 2.6

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ +0xe  로더 입구 네 함수 → 첫 스레드 판정                                   (run17)
             └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)
                  ├─ 앞쪽 +0x0 ~ +0x12dd    살림 차리기                         (run18)
                  └─ 뒤쪽 +0x12de ~ 끝      짐 들이고 전원 켜기                 (run19)
                       ├─ +0x19a2  LdrpInitializeKernel32Functions              ← 이번 (run20)
                       ├─ +0x1aa9  LdrpMapAndSnapDependency   (dll1·dll2 후보)
                       ├─ +0x1b5e  (간접) Kernel32ThreadInitThunkFunction(1,0,0)
                       └─ +0x1bbb  LdrpInitializeGraphRecurse …
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16, 뒤는 추측)
```

- run19 는 이 방을 "**문 앞에서**" 봤다: 언제 부르나(`Subsystem` 2/3), 어떤 칸을 채우나(`Kernel32ThreadInitThunkFunction`, 출력 칸 `[RSP+0x188]`).
- 이번엔 **문 안**이다. flow.md 로는 2.5 `ntdll → KERNELBASE / KERNEL32 준비` 절이다.

### 무엇이 궁금했나

run 문서 "목적" 그대로.

1. `LdrpInitializeKernel32Functions` 가 **무엇을 어떤 순서로** 하나.
2. run7 에서 본 두 `LdrLoadDll` 이 **각각 어느 DLL** 을 올리나.
3. `Kernel32ThreadInitThunkFunction` 칸에 **무엇을** 넣나.
4. 덤으로:
   - `LdrLoadDll`·`LdrpLoadDll` 이 인자를 어떻게 바꿔 `LdrpLoadDllInternal` 로 넘기나
   - run18 이 준비한 전역 8개를 누가 읽나
   - "시작 때 kernel32 는 없다" `[추측]` 을 코드로 말할 수 있나

### 바퀴 전에 알던 것

- `[관찰]` (run7) `+0x19a2` 안에서 `LdrLoadDll` 이 **두 번** 불렸다. 돌아올 자리는 `+0xc0`, `+0x156`.
- `[관찰]` (run7) 첫째 `LdrLoadDll` 안 사슬: `LdrLoadDll +0x170` → `LdrpLoadDll +0x100` → `LdrpLoadDllInternal +0x284` → `LdrpPrepareModuleForExecution +0xef` → `LdrpInitializeGraphRecurse`.
- `[관찰]` (run7) 그 안에서 `LdrpCallInitRoutine` 이 두 번 불렸다. `rcx` 는 각각 `KERNELBASE!KernelBaseDllInitialize` **근처**, `KERNEL32!BaseDllInitialize` **근처**였다. 두 번 모두 첫째(`+0xc0`)와 둘째(`+0x156`) `LdrLoadDll` **사이**였다.
- `[관찰]` (run7) 둘째 `LdrLoadDll` 에선 `LdrpLoadDll` 까지만 봤다 (요청서).
- `[정적]` (run19) 이 함수는 exe 헤더 `Subsystem` 이 2·3 일 때만 불린다. 병렬 로딩 켜기 **앞**(`LdrInitState` = 0)이다.
- `[정적]` (run19, `refs`) 이 함수의 `+0xe4` 가 전역 `Kernel32ThreadInitThunkFunction` 칸의 주소를 넘긴다. 그 칸을 읽는 곳은 `LdrpInitializeProcess` 와 `RtlUserThreadStart +0x7`.
- `[추측]` (flow.md 2.5) exe 가 무엇을 import 하든 ntdll 이 kernel32 를 **스스로** 불러온다.
- `[추측]` (flow.md 2.5) `LdrpInitializeProcess` 가 시작될 땐 kernel32·kernelbase 는 **아직 없다**.
- `[추측]` (flow.md 2.5) kernel32 를 올릴 때 kernel32 가 import 하는 kernelbase 도 **따라** 올린다.
- `[추측]` (run19) `Kernel32ThreadInitThunkFunction` 칸의 값은 `KERNEL32!BaseThreadInitThunk`.

---

## 2. 찾은 것

### 2.1 한눈에 — 트리 (1) ~ (10)

run 문서 "호출 관계" 그대로다. 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)).

```
LdrpInitializeProcess +0x19a2   (Subsystem 2/3 일 때, run19)
 └─ LdrpInitializeKernel32Functions (0xb8024)       ← RCX = 출력 칸 (입구에서 0)
      ├─ (1) +0x86   LdrpAppendUnicodeStringToFilenameBuffer   "KERNEL32.DLL"
      ├─ (2) +0xa4   LdrpFindLoadedDllInternal (플래그 0x20)   이름만으로 이미 있나
      │      찾음 → +0x1ff (베이스 = 항목+0x30, Flags 0x20000000 켜졌으면 (7) 로)
      ├─ (3) +0xc0   LdrLoadDll(0x4001, 0, "KERNEL32.DLL")      못 찾았을 때만   [관찰] run7
      │     └─ +0x170  LdrpLoadDll
      │          ├─ +0xc2   LdrpPreprocessDllName
      │          └─ +0x100  LdrpLoadDllInternal(이름, 경로 구조체, 플래그, 4, …, 항목 칸, 상태 칸)   [관찰] run7
      │      (0xc0000135 이면 (4)~(6) 건너뛰고 (7) 로)
      ├─ (4) +0xff   LdrGetProcedureAddressForCaller  "BaseThreadInitThunk"  → 전역 Kernel32ThreadInitThunkFunction
      ├─ (5) +0x130  LdrGetProcedureAddressForCaller  "TermsrvGetWindowsDirectoryW" → 출력 칸 *RCX (결과 안 봄)
      ├─ (6) +0x135  LdrpSnapKernelBaseExtensions
      ├─ (7) +0x156  LdrLoadDll(0x4001, 0, "KERNELBASE.dll")                     [관찰] run7
      │     └─ +0x170  LdrpLoadDll
      ├─ (8) +0x165  LdrpCodeAuthzInitialize          레지스트리 값에 따라 안에서 LdrLoadDll("ADVAPI32.DLL")
      ├─ (9) +0x19b  LdrGetProcedureAddressForCaller  "KernelbasePostInit" → [RSP+0x58]
      └─ (10) +0x1af (간접, CFG) [RSP+0x58] = KernelbasePostInit
```

읽을 때 주의:

- `[관찰]` 이 붙은 줄은 01_sprint run7 에서 디버거로 본 자리다. 나머지 줄은 **전부 `[정적]`** 이다.
  - `[관찰]` 줄도 이번에 `_calls.tsv` 숫자와 **똑같았다** (`+0xc0`, `+0x156`, `+0x170`, `+0x100`). 이 빌드의 코드가 run7 때와 같다는 버전 대조다.
- 호출 15곳 중 **대표만** 남겼다. 빠진 4곳은 잡일이다: `memset`(`+0x54`, 0x110 바이트 지역 버퍼 비우기), 쿠키 검사 1, 실패 로그 `LdrpLogInternal` 3.
- **번호 체계가 셋이다.** 헷갈리기 쉬우니 먼저 짚는다.
  - 이 트리의 **(1) ~ (10)** = 이 함수 안 호출 순서. run 문서·flow.md 2.5 트리가 이 번호를 쓴다.
  - flow.md **그림 2 화살표 (1) ~ (7)** = 그림에 상자로 올린 것만 센 번호다. `(1) +0xa4`, `(2) +0xc0`, `(3) +0xff`, `(4) +0x156`, `(5) +0x165`, `(6) +0x19b`, `(7) +0x1af`. 그래서 트리 (4) `+0xff` 가 그림에선 (3) 이다.
  - flow.md 2.5 트리 **(3) 아래 들여쓴 (1)(2)** = run7 콜스택의 재귀 단계 번호다 (`LdrpInitializeGraphRecurse` 안). 이 함수의 (1)(2) 와는 다른 것이다.
  - 가장 확실한 건 **오프셋**이다. 번호가 헷갈리면 ret 숫자로 맞춰 보면 된다.

### 2.2 한 줄씩 읽는 법 — (1) 부터 (10) 까지

한 줄씩, "누가 / 어디서(+0x) / 누구를 / 무엇을 위해" 로 읽는다. **누가**는 열 줄 모두 같다: `LdrpInitializeKernel32Functions` (메인 스레드 = 가게 세팅 담당). 그래서 아래엔 나머지 셋만 적는다.

**(0) 입구 — 들어오자마자 `+0x6c`** `[정적]`

- 호출자 `LdrpInitializeProcess` 가 넘긴 `RCX` 는 **출력 칸** 하나의 주소다 (호출자 쪽 `[RSP+0x188]`, run19).
- 입구 `+0x6c` 에서 그 칸을 먼저 **0** 으로 만든다. 나중에 (5) 가 채운다.

**(1) `+0x86 LdrpAppendUnicodeStringToFilenameBuffer`** `[정적]`

- 어디서: 부르는 자리 `+0x81`, 돌아올 자리 `+0x86`.
- 누구를: `LdrpAppendUnicodeStringToFilenameBuffer`.
- 무엇을 위해: 지역 버퍼에 전역 `LdrpKernel32DllName` 을 담는다 (`+0x5d`). 그 전역의 문자열은 **`KERNEL32.DLL`** (대문자, 유니코드 12글자) 이다.
- 비유: "기본 설비 건물" 주문서에 **건물 이름**부터 적는다.

**(2) `+0xa4 LdrpFindLoadedDllInternal`** `[정적]`

- 어디서: 부르는 자리 `+0x9f`, 돌아올 자리 `+0xa4`.
- 누구를: `LdrpFindLoadedDllInternal(버퍼, 0, &항목, &x, 0x20)`. 다섯째 인자 `0x20` 은 `+0x8b` 에서 싣는다.
- 무엇을 위해: **"KERNEL32.DLL 이 이미 명부에 있나"** 를 이름만으로 확인한다. 자세히는 2.3.
- 갈림: `+0xa6 JNS +0x1ff` — 찾았으면(0 이상) (3) 을 건너뛴다.

**(3) `+0xc0 LdrLoadDll(0x4001, 0, "KERNEL32.DLL", &베이스)`** `[정적]` + `[관찰]` run7 (자리)

- 어디서: 부르는 자리 `+0xbb`, 돌아올 자리 **`+0xc0`** (run7 콜스택 숫자와 같다).
- 누구를: `LdrLoadDll`. `RCX = 0x4001` (`+0xb6`), `RDX = 0`, `R8 = 이름`, `R9 = 베이스 칸` (`[RSP+0x40]`).
- 무엇을 위해: **KERNEL32.DLL 을 들인다.** (2) 가 못 찾았을 때만.
- 안쪽: `+0x170 LdrpLoadDll` → `+0xc2 LdrpPreprocessDllName` → `+0x100 LdrpLoadDllInternal`. 2.10·2.11.
- 갈림 (`+0xc2`): 결과가 정확히 `0xc0000135` 면 (4)~(6) 건너뛰고 (7) 로. 그 밖의 음수면 로그 후 끝. 0 이상이면 (4) 로. 2.12.

**(4) `+0xff LdrGetProcedureAddressForCaller` — `BaseThreadInitThunk`** `[정적]`

- 어디서: 부르는 자리 `+0xfa`, 돌아올 자리 **`+0xff`**.
- 누구를: `LdrGetProcedureAddressForCaller(kernel32, "BaseThreadInitThunk", 0, &Kernel32ThreadInitThunkFunction, 0, 호출자 주소)`.
- 무엇을 위해: kernel32 에서 **이름으로** `BaseThreadInitThunk` 함수 주소를 찾아, 전역 **`Kernel32ThreadInitThunkFunction`** 칸에 적는다. 핵심 ② (2.5).
- 갈림: 음수면 로그 후 끝 (`+0x103`).

**새 말 — `LdrGetProcedureAddressForCaller`**: 이름 그대로 "DLL 안에서 프로시저(함수) 주소를 얻는다" 는 함수다 `[추측: 이름]`. 이번에 본 인자는 이렇다 `[정적]`:

- `RCX` = DLL 베이스 (어느 건물에서 찾나)
- `RDX` = 함수 이름 **ANSI 문자열** (무슨 이름으로 찾나)
- `R8` = 0 (번호로 찾지 않는다)
- `R9` = **결과 칸** (찾은 주소를 어디에 적나)
- 다섯째 = 0, 여섯째 = **이 함수의 돌아갈 주소** (`LdrpInitializeProcess +0x19a2`). 이름의 "ForCaller"(부른 곳) 대로다.
- ANSI 문자열 = 글자 하나를 1바이트로 적는 문자열 쪽지로 알려져 있다 `[추측]` (일반적으로 알려진 설명). DLL 이름은 유니코드 쪽지, 함수 이름은 ANSI 쪽지로 다르게 쓴다.
- 비유: [run16 해설 2.9](run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다) 에서 관리사무소가 ntdll 건물 **호실 이름표**를 보고 호실을 찾아 벽 주소록에 적었다. 이번엔 **관리실**이 kernel32 건물 호실 이름표를 보고 같은 일을 한다.

**(5) `+0x130 LdrGetProcedureAddressForCaller` — `TermsrvGetWindowsDirectoryW`** `[정적]`

- 어디서: 부르는 자리 `+0x12b`, 돌아올 자리 `+0x130`.
- 누구를: 같은 함수. 이름은 `TermsrvGetWindowsDirectoryW`, 결과 칸은 **(0) 의 출력 칸** (`+0x11c MOV R9,RDI`).
- 무엇을 위해: kernel32 의 `TermsrvGetWindowsDirectoryW` 주소를 호출자에게 돌려준다. 2.7.
- 갈림: **결과를 안 본다.** 실패하면 출력 칸은 (0) 의 0 그대로.

**(6) `+0x135 LdrpSnapKernelBaseExtensions`** `[정적]`

- 어디서: 부르는 자리 `+0x130`, 돌아올 자리 `+0x135`. (5) 의 돌아올 자리가 곧 (6) 의 부르는 자리다. 두 call 이 딱 붙어 있다.
- 무엇을 위해: 이름으로 보면 "kernelbase 확장을 연결(snap)" 이다. 하는 일은 안 봤다 `[추측: 이름]`.
- 갈림: 음수면 **로그 없이** 끝 (`+0x139`).

**(7) `+0x156 LdrLoadDll(0x4001, 0, "KERNELBASE.dll", &베이스)`** `[정적]` + `[관찰]` run7 (자리)

- 어디서: 부르는 자리 `+0x151`, 돌아올 자리 **`+0x156`** (run7 콜스택 숫자와 같다).
- 누구를: `LdrLoadDll`. 인자는 (3) 과 같고 이름만 전역 `LdrpKernelbaseDllName` = **`KERNELBASE.dll`** (대소문자 섞인 14글자). 베이스 칸 `[RSP+0x40]` 을 **다시 채운다**.
- 무엇을 위해: **KERNELBASE 를 이름으로 들인다.** 그런데 정말 "새로 들이는" 건지는 핵심 ① (2.4).
- 갈림: 음수면 로그 후 끝 (`+0x15a`).

**(8) `+0x165 LdrpCodeAuthzInitialize`** `[정적]`

- 어디서: 부르는 자리 `+0x160`, 돌아올 자리 `+0x165`.
- 무엇을 위해: (7) 이 성공하면 **항상** 부른다. 안에서 레지스트리 값에 따라 **ADVAPI32.DLL 을 들일 수도** 있다. 2.8.
- 갈림: 0 이상이거나 정확히 `0xc0000135` 면 계속 (`+0x1e5`). 그 밖의 음수면 로그 후 끝.

**(9) `+0x19b LdrGetProcedureAddressForCaller` — `KernelbasePostInit`** `[정적]`

- 어디서: 부르는 자리 `+0x196`, 돌아올 자리 `+0x19b`.
- 누구를: 같은 함수. 찾는 곳은 **kernelbase** (베이스 칸이 (7) 에서 다시 채워졌으니), 이름은 `KernelbasePostInit`, 결과 칸은 **지역** `[RSP+0x58]` (`+0x179`).
- 무엇을 위해: 바로 다음 (10) 에서 부를 주소를 얻는다.
- 갈림: 음수면 로그 후 끝 (`+0x19f`).

**(10) `+0x1af` 간접 호출 — `[RSP+0x58]`** `[정적]`

- 어디서: 부르는 자리 `+0x1aa`, 돌아올 자리 `+0x1af`.
- 누구를: 코드에 박힌 이름이 없다. [간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출)이다. 바로 앞 `+0x1a5 MOV RAX,[RSP+0x58]` 로 (9) 가 채운 칸을 꺼내 부른다 → **`KernelbasePostInit`**.
- 무엇을 위해: kernel32 준비의 **마지막 일**. 2.9.
- 갈림: 음수면 로그 후 끝.

**끝 — `+0x1c7`** `[정적]`

- 마지막 상태(`EBX`) 를 돌려준다. 호출자 `LdrpInitializeProcess +0x19a2` 는 음수면 로그 후 에러 끝으로 간다.

#### 비유로 다시 한 번 — (1) ~ (10)

1. 주문서에 "KERNEL32" 라고 적는다
2. 명부에 이미 있나 본다
3. 없으면 **기본 설비 건물(kernel32)을 들인다**
4. 그 건물 호실 이름표에서 "**출근 인사 담당**" 호실을 찾아 **벽 주소록**에 적는다
5. "Windows 폴더 안내" 호실도 찾아 세팅 담당 손에 쥐여 준다
6. kernelbase 확장 연결 (하는 일은 아직 모름)
7. "KERNELBASE" 라고 적은 주문서도 낸다
8. 보안 담당(ADVAPI32)이 필요한지 설정을 확인한다
9. KERNELBASE 건물 이름표에서 "**개업 뒤 인사**"(`KernelbasePostInit`) 호실을 찾는다
10. 그 호실에 **바로 전화를 건다**

### 2.3 "이미 있나" 확인 — 그리고 "시작 때 kernel32 는 없다"

**(2) 가 하는 일** `[정적]`

- `LdrpFindLoadedDllInternal` 에 플래그 `0x20` 을 주면, 안에서 `+0x76 LdrpFindLoadedDllByName` **하나만** 부르고 그 결과를 돌려준다.
- 경로를 풀어 보거나 검색 경로를 뒤지는 갈래는 `0x20` 이 **꺼졌을 때**의 쪽이다 (결과서, 앞 0x100 바이트만 훑음).
- 그래서 이 호출은 **이름만으로 모듈 목록(해시 표 `LdrpHashTable`)을 뒤지는 확인**이다.

**새 말 — 해시 표**: 이름에서 칸 번호를 바로 계산해 그 칸만 보면 되게 만든 표로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

- 비유: 건물 명부를 첫 줄부터 다 읽지 않고, **이름 색인 카드함**에서 "K" 칸만 열어 보는 것.
- `LdrpHashTable` 은 run18 이 앞쪽 `+0x3c9` 에서 **비워 둔** 바로 그 전역이다.

**갈림 셋** `[정적]`

| 경우 | 가는 곳 | 그 뒤 |
|---|---|---|
| 찾음 (0 이상) | `+0x1ff` | 베이스 = 항목 `+0x30` (`DllBase`). `PEB.ProcessParameters.Flags` 의 `0x20000000` 비트를 본다 (`+0x21a`). 꺼짐 → (4) 로 / 켜짐 → (7) 로 (kernel32 쪽 (4)~(6) 건너뜀) |
| 못 찾음 (음수) | (3) | `LdrLoadDll("KERNEL32.DLL")` |
| (3) 이 `0xc0000135` | `+0x13b` | (4)~(6) 건너뛰고 (7) 로 |

- `0x20000000` 비트의 이름은 PDB 로 못 찾았다. 뜻은 `[추측: 특수 모드의 예외 길]`.

**test.exe 는 어느 쪽인가 — 단계별로** (run 문서 "시작 때 kernel32 는 아직 없다")

1. (2) 는 이름만으로 `LdrpHashTable` 을 뒤진다 `[정적]`.
2. 그 표에 항목을 끼우는 `LdrpInsertDataTableEntry` 를 부르는 곳은 **4곳**이다 (지도) `[정적]`:
   - ntdll 자신 (`LdrpInitializeNtdllDataTableEntry +0xf2`) — run18 의 명부 첫 줄
   - exe 항목 (`LdrpInitializeProcess +0x1450`) — run19 의 명부 둘째 줄
   - AppVerifier 묶음 안 (`+0x1625`) — 검증기가 꺼지면 통째로 안 탄다
   - DLL 을 매핑한 뒤 (`LdrpMapDllWithSectionHandle +0x141`)
3. run18·run19 로 보면 `+0x199d` 앞엔 DLL 을 매핑하는 호출이 없다 (WOW64·.NET 길 빼고) `[정적]`.
4. 그래서 `+0x199d` 에 닿을 때 표에 있는 건 **ntdll·exe (와 검증기가 켜졌으면 그쪽) 뿐**이다 `[정적]`. 실제 값은 동적.
5. 그러니 test.exe 에서 (2) 는 "**못 찾음**" 이고 (3) 을 탄다 `[추측: 정적 결론, 반환값은 동적 Q49]`.
6. run7 이 `+0xc0` 을 실제로 봤다 `[관찰]`. **관찰과도 맞다.**

- "이미 있음" 갈래(`+0x1ff`)는 kernel32 가 먼저 올라온 프로세스를 위한 길로 보인다 `[추측]`.
- 비유: 색인 카드함엔 아직 **관리실(ntdll)과 본 건물(exe) 카드 두 장뿐**이다. 그러니 "KERNEL32" 칸을 열어 봐도 비어 있고, 관리실은 주문을 낸다.
- 주의: 이건 **유저 쪽** 코드로 본 이야기다. 커널이 프로세스를 만들 때 "exe·ntdll **뿐**" 인지는 여전히 미확인이다 ([run10 해설 3.11](run10-allocate-process-해설.md#311-exentdll-을-올린다-와-그-둘뿐-은-다른-말이다), flow.md 4장).

### 2.4 핵심 ① 두 `LdrLoadDll` 의 이름 — run7 순서와 왜 안 부딪치나

#### 먼저 사실 둘을 나란히

**코드가 말하는 것** `[정적]` (이름은 원본 바이트)

| ret | 이름 (전역) | 인자 |
|---|---|---|
| 첫째 `+0xc0` | **`KERNEL32.DLL`** (`LdrpKernel32DllName`) | `RCX=0x4001`, `RDX=0` |
| 둘째 `+0x156` | **`KERNELBASE.dll`** (`LdrpKernelbaseDllName`) | 같음 |

- 그리고 둘째(KERNELBASE)는 kernel32 쪽 함수 찾기 (4)·(5) **뒤**에 있다.

**디버거가 본 것** `[관찰]` (run7, flow.md 2.5)

- 시작 함수를 부르는 `LdrpCallInitRoutine` 이 **두 번** 불렸다.
  - 첫째: `rcx` 가 `KERNELBASE!KernelBaseDllInitialize` **근처**
  - 둘째: `rcx` 가 `KERNEL32!BaseDllInitialize` **근처**
- 두 번 다 **첫째 `LdrLoadDll`(`+0xc0`) 안**이었다. 콜스택이 `+0xc0 → +0x170 → +0x100 → +0x284 → +0xef LdrpInitializeGraphRecurse` 로 이어졌다.
- 둘째 `LdrLoadDll`(`+0x156`) 에선 `LdrpLoadDll` 까지만 봤다.

#### 어디가 어긋나 보이나

- 코드만 보면 "KERNELBASE 는 **둘째** 호출이 들인다" 처럼 읽힌다.
- 그런데 KERNELBASE 의 시작 함수는 **첫째** 호출 안에서, 그것도 KERNEL32 보다 **먼저** 불렸다.
- 아직 들이지도 않은 건물의 전원을 먼저 켤 수는 없다. 그러니 뭔가 하나는 다르게 읽어야 한다.

#### 왜 부딪치지 않나 — 단계별로

1. **"이름으로 주문한 순서" 와 "실제로 들어온 순서" 는 다른 말이다.**
   - 코드가 보여 주는 건 **주문서를 낸 순서**다 `[정적]`: KERNEL32 주문 → (함수 찾기) → KERNELBASE 주문.
   - 디버거가 보여 준 건 **전원이 켜진 순서**다 `[관찰]`: KERNELBASE → KERNEL32, 둘 다 첫째 주문 안.
2. 이 둘을 같이 만족하는 그림이 하나 있다 `[추측]`:
   - 첫째 주문 `LdrLoadDll("KERNEL32.DLL")` 안에서, kernel32 가 **기대는(import 하는)** kernelbase 도 **따라** 들어온다.
   - 그래서 전원도 기대는 쪽(kernelbase)부터 켠다. flow.md 2.5 의 `[추측]` "KERNEL32 가 KERNELBASE 에 기대니까 KERNELBASE 를 먼저 켠다" 와 같은 원리다.
   - 그러면 둘째 주문 `LdrLoadDll("KERNELBASE.dll")` 이 갔을 땐 kernelbase 가 **이미 있다**. 둘째는 "새로 들이기" 가 아니라 **이미 있는 걸 다시 찾아 베이스를 받는 호출**일 것이다.
3. 이 그림을 받쳐 주는 것:
   - run7 이 둘째 호출에서 `LdrpLoadDll` **까지만** 봤다 `[관찰]`. 첫째처럼 시작 함수까지 내려간 흔적이 없다.
   - (7) 바로 뒤 (9) 가 **kernelbase 베이스**로 `KernelbasePostInit` 을 찾는다 `[정적]`. 둘째 호출의 목적이 "kernelbase 베이스 얻기" 라고 보면 자연스럽다 `[추측]`.

#### 아직 확인 아님 — 왜 `[추측]` 인가

- "kernel32 가 kernelbase 를 import 한다" 자체가 flow.md 에서 `[추측]` 이다. kernel32 의 import 표는 이번에 안 봤다.
- 첫째 호출 **안에서 kernelbase 가 메모리에 올라오는 순간**을 아직 못 봤다. `LdrpLoadDllInternal` 의 올리기 쪽(`+0x284` 앞)은 본문을 안 읽었다 (flow.md 2.5 `[공백]`).
- run7 의 두 시작 함수 이름도 "기호 **근처**" 였지 정확히 일치가 아니었다 `[관찰]`. 어느 함수인지는 Q13·Q21·Q22.
- 확인할 곳:
  - 정적 **Q11**: `LdrpLoadDllInternal` 안
  - 동적 **Q20**: 첫째 `LdrLoadDll(KERNEL32.DLL)` 안에서 kernelbase 가 올라오는 순간
  - 동적 **Q49**: `LdrLoadDll` 입구(`ntdll+0x3cbf0`)에서 `dS @r8` 로 몇 번·어떤 이름이 오나, ret `+0x156` 의 `eax`

#### 비유로 정리

- 관리실이 "기본 설비 건물(KERNEL32) 주세요" 주문서를 냈다.
- 배달 기사가 그 건물을 가져오면서, 그 건물이 **기대는 옆 건물(KERNELBASE)도 같이** 가져와 세우고, 옆 건물부터 전원을 켰다 — 디버거가 본 것.
- 그 뒤 관리실이 "KERNELBASE 주세요" 주문서를 또 냈다. 기사는 "이미 있네요" 하고 **번지만** 알려 준다 — 코드 순서와 맞추면 이렇게 읽힌다는 짐작.
- 그러니 두 사실은 부딪치지 않는다. **주문서의 순서**와 **건물이 들어온 순서**가 다를 뿐이다.

### 2.5 핵심 ② `+0xff` — 벽 주소록 칸 하나가 main 으로 가는 길에 놓인다

#### run19 에서 남겨 둔 것

- run19 는 `refs` 로 "`+0xe4` 가 `Kernel32ThreadInitThunkFunction` 칸의 **주소를 넘긴다**" 까지만 봤다 `[정적]`.
- 칸에 **무엇이** 들어가는지는 `[추측]` (`BaseThreadInitThunk`) 이었다.

#### 이번에 본 것 — 한 줄씩 `[정적]`

1. `+0xe4 LEA R9,[...]` 는 칸에 값을 쓰는 줄이 **아니다**. 칸의 **주소**를 넷째 인자 `R9`(결과 칸)에 싣는 줄이다.
2. 바로 그 뒤 `+0xfa` 호출 (ret **`+0xff`**) `LdrGetProcedureAddressForCaller` 가 그 칸에 값을 **써 준다**.
3. 그때 찾는 곳은 `RCX` = (3) 이 돌려준 **kernel32 베이스**, 찾는 이름은 `RDX` = **`BaseThreadInitThunk`** (원본 바이트, 19글자).
4. 그러니 **칸 값 = kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소**다.
5. 칸의 자리는 rva `0x1d2760` 이다 (결과서).

- 비유: 관리실 직원이 kernel32 건물 입구의 **호실 이름표**에서 "BaseThreadInitThunk" 를 찾아, 관리실 **벽 주소록**의 "출근 인사 담당" 칸에 그 호실 번호를 적는다.
- run16 2.9 의 관리사무소 이야기와 **모양이 똑같다.** 그땐 관리사무소가 부팅 때 ntdll 이름표를 보고 `RtlUserThreadStart`(본업 자리)를 적었다. 이번엔 관리실이 가게 세팅 때 kernel32 이름표를 보고 출근 인사 담당을 적는다.

#### 그 칸을 누가 읽나 `[정적]` (`refs`)

| 읽는 곳 | 언제 | 무엇을 |
|---|---|---|
| `LdrpInitializeProcess +0x1b45` (읽기) → `+0x1b59` (부르기, ret `+0x1b5e`) | 가게 세팅 중, 한 번 | 칸의 함수를 `(1,0,0)` 으로 부름 ([run19 해설 2.10](run19-proc-init-modules-해설.md#210-간접-호출-4곳--칸에서-주소를-꺼내-부르기)) |
| **`RtlUserThreadStart +0x7`** | 직원이 본업 자리에 올 때마다 | 칸을 읽음 |

- 순서도 맞다: 칸을 채우는 `+0x199d`(이 함수를 부르는 자리)가 칸을 부르는 `+0x1b59` 보다 **앞**이다 `[정적]` (run19 10묶음: 묶음 5 → 묶음 7).

#### main 으로 가는 길에서 어디인가

flow.md 2.10 의 길에 이번 칸을 끼워 보면:

```
LdrInitializeThunk +0x18  NtContinue(맡긴 문맥)             [정적] run16
 ═▶ RtlUserThreadStart                                      도착은 [추측] (Q17·Q29)
     └─ +0x7  Kernel32ThreadInitThunkFunction 칸 읽기       [정적] refs (run19)
              ↑ 그 칸 값 = kernel32 에서 이름 BaseThreadInitThunk 로 찾은 주소   [정적] run20 (+0xff)
     └─ 그 칸의 함수를 부름 → … → mainCRTStartup → main     [추측] (Q17 정적, Q30·Q48 동적)
```

- 이번 바퀴로 **"칸 값이 어디서 왔나"** 가 코드로 닫혔다.
- 그래서 main 으로 가는 사슬에서 `[추측]` 이 줄었다: "칸 = `BaseThreadInitThunk`" 는 이제 "**이름이 `BaseThreadInitThunk` 인 내보내기로 찾은 주소**" 까지 `[정적]` 이다.
- 비유: 직원이 본업 자리에 오면 벽 주소록 "출근 인사 담당" 칸부터 본다. 그 칸은 **개업 준비 때 관리실이 kernel32 이름표를 보고 적어 둔 번호**다.

#### 남은 `[추측]` 둘

1. 그 주소가 정말 **`KERNEL32!BaseThreadInitThunk` 본체**인가.
   - **새 말 — 전달 항목(forwarder)**: 내보내기 표의 한 줄이 "이 이름의 함수는 **다른 DLL** 에 있다" 고 넘겨 주는 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
   - 비유: 호실 이름표에 "이 담당은 **옆 건물 몇 호**로 가세요" 라고 적힌 줄.
   - `BaseThreadInitThunk` 가 그런 줄이면 칸 값은 **다른 곳**일 수 있다 `[추측]`.
   - 확인: **Q48** — ret `+0xff` (`ntdll+0xb8123`) 뒤 `dq ntdll+0x1d2760 L1` → `ln`, 그리고 `LdrpInitializeProcess +0x1b59` 에서 `ln @rax`.
2. `RtlUserThreadStart` 가 그 값을 **부르나**. `RtlUserThreadStart` 본문은 안 봤다 → 정적 Q17, 동적 Q30.

### 2.6 핵심 ③ exe 의 import 를 안 본다 — "스스로 불러온다" 의 코드 근거

flow.md 2.5 의 `[추측]` 은 "exe 가 무엇을 import 하든 상관없이, ntdll 이 kernel32 를 **스스로** 불러온다" 다. 이번 코드로 무엇을 말할 수 있는지 단계별로 보자.

**근거 1 — 부르는 조건** `[정적]` (run19)

- `LdrpInitializeProcess +0x1980 ~ +0x1993`: exe 헤더 `Subsystem` 이 2(GUI)·3(콘솔)일 때**만** 이 함수를 부른다.
- 조건에 import 목록이 없다.

**근거 2 — 이 함수의 호출 15곳** `[정적]` (이번)

- 15곳을 다 늘어놓으면: `memset`, 이름 버퍼 담기, 이미 있나 확인, `LdrLoadDll` ×2, 함수 찾기 ×3, `LdrpSnapKernelBaseExtensions`, `LdrpCodeAuthzInitialize`, 간접 호출 1, 쿠키 검사, 로그 ×3.
- 이 중에 **exe 의 import 표를 읽는 호출이 없다.**

**근거 3 — 이름이 어디서 오나** `[정적]` (이번)

- 들이는 DLL 이름은 **고정 전역 두 개**에서 온다: `LdrpKernel32DllName`, `LdrpKernelbaseDllName`.
- exe 에서 읽어 온 이름이 아니다.

**그래서**

- 세 근거를 합치면 "exe 의 import 와 상관없이 이름을 정해 두고 들인다" 는 **코드로 맞다** `[정적]` (run 문서).
- 그래도 flow.md 에서 이 줄의 머리 태그는 **`[추측]` 그대로**다. 밑에 `[정적]` 근거가 두 줄(run19, run20) 붙은 모양이다. 실행으로 맞춰 보는 건 **Q49**.
- 범위 주의: 근거 2·3 은 **이 함수 본문** 이야기다. 안쪽 callee(`LdrpLoadDllInternal` 등) 본문은 이번에 안 읽었다.

**run19 와 나란히 보면**

- [run19 해설 2.7](run19-proc-init-modules-해설.md) 에서 dll1·dll2 쪽 후보 `+0x1aa4 LdrpMapAndSnapDependency` 는 callee 이름이 하나같이 **import 쪽**이었다 (`GetImportDescriptor…` 등, `[추측: 이름]`).
- 이번 kernel32 쪽은 **반대로 import 를 안 본다** `[정적]`.
- 비유:
  - dll1·dll2 = 본 건물이 낸 **주문 목록(import)** 을 보고 들이는 짐.
  - kernel32 = 본 건물이 뭘 주문했든 상관없이, 가게라면 **관리실이 알아서** 들이는 기본 설비. 주문서엔 건물 이름이 **미리 인쇄**돼 있다 (고정 전역).

### 2.7 (5) 출력 칸과 (6) — 짧게

**(5) `TermsrvGetWindowsDirectoryW` → 호출자 출력 칸** `[정적]`

- 입구 `+0x6c` 에서 0 으로 만든 출력 칸(호출자 `[RSP+0x188]`)에, (5) 가 kernel32 의 **`TermsrvGetWindowsDirectoryW` 주소**를 적는다. 실패하면 0 그대로.
- 이 칸은 run19 에서 본 대로 `LdrpInitializeProcess +0x1b72 LdrpInitializePerUserWindowsDirectory` 로 간다.
- 그 함수가 이 주소로 무엇을 하는지는 범위 밖이다.
- 비유: "Windows 폴더 안내" 호실 번호를 찾아, 세팅 담당이 내민 **빈 메모지**에 적어 돌려준다. 묶음 7 의 "사용자 Windows 폴더" 일에서 그 메모지를 쓴다.

**(6) `LdrpSnapKernelBaseExtensions`** `[정적]`

- kernel32 쪽 길(`+0xd1`)로 온 경우에만 탄다. 건너뛰는 경우는 둘: (3) 이 `0xc0000135`, 또는 "이미 있음" + `Flags & 0x20000000`.
- 하는 일은 안 봤다 `[추측: 이름]`.
- 실패 길 중 **혼자만 로그 없이** 끝난다 (2.12).

### 2.8 (8) `LdrpCodeAuthzInitialize` — ADVAPI32 가 끼어들 수 있는 길

앞쪽만 훑었다 (결과서 "요청서 범위 밖"). 본 만큼만 `[정적]`:

1. 전역 `LdrpIsSecureProcess` 가 0 이 아니면 → 바로 0 반환 (`+0x20 ~ +0x37`).
2. 이미지 `Subsystem` 이 2·3 이 아니면 → 바로 0 반환 (`+0x5f ~ +0x6b`).
3. 그 밖엔 **레지스트리 키 세 개**를 `NtOpenKey`·`NtQueryValueKey` 로 연다.
4. **값에 따라** `+0x141 LdrLoadDll(0, 0, "ADVAPI32.DLL", …)` 을 부르고, `SaferiIsDllAllowed` 를 찾아 전역 `LdrpAdvapi32DllHandle`·`LdrpSaferIsDllAllowedRoutine` 에 넣는다 (`+0x19e`, `+0x1a8`).
5. 레지스트리 값이 없으면(`ESI==0`) 4번 길은 안 탄다.

- **새 말 — 레지스트리**: Windows 가 설정 값을 모아 두는 데이터베이스로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 관리실 **설정 장부**.
- 그러니 **kernel32 준비 중에 ADVAPI32 가 올라올 수도 있는 코드가 있다** `[정적]`. 실제로 올라오는지는 이 VM 의 레지스트리 값에 달렸다 → **Q49** (`LdrLoadDll` 입구에서 이름 세기).
- 여기서 `LdrLoadDll` 의 첫 인자는 `0` 이다. (3)·(7) 의 `0x4001` 과 다르다. `RCX==0` 이면 `LdrLoadDll` 은 경로 칸도 플래그 칸도 안 채운다 (2.10).
- 비유: 보안 설정 장부에 "검사관 필요" 라고 적혀 있을 때만 **보안 담당 건물(ADVAPI32)** 을 하나 더 들인다.

### 2.9 (9)·(10) 마지막 인사 — `KernelbasePostInit`

- (9) 가 kernelbase 호실 이름표에서 `KernelbasePostInit` 을 찾아 **지역 칸** `[RSP+0x58]` 에 적는다 `[정적]`. 그 칸은 입구 `+0x38` 에서 0 으로 시작한다 (결과서).
- (10) 은 `+0x1a5` 에서 그 칸을 `RAX` 로 꺼내 `+0x1aa` 에서 부른다 `[정적]`.
  - 이 call 은 **CFG 검사 thunk** 를 거친다. [CFG](run19-proc-init-modules-해설.md) 는 run19 해설 묶음 4 에서 한 줄로 봤다 (간접 호출 대상이 허락된 주소인지 확인하는 보호 장치 `[추측]`).
  - **새 말 — thunk**: 여기선 "진짜 대상으로 가기 전에 한 번 거치는 짧은 다리 함수" 라는 뜻이다 `[추측]` (일반적으로 알려진 설명). 비유: 전화를 걸면 **교환원**이 번호가 허락된 곳인지 보고 이어 준다.
  - 그래서 call 명령은 교환원(thunk) 쪽으로 가는 **5바이트**짜리이고, ret 은 `+0x1af` 다. 진짜 대상은 호출 직전 `rax` 다 (run 문서 도구 메모).
- 부르기 직전 `RCX`·`RDX`·`R8` 을 **따로 싣지 않는다** `[정적]`. 인자 없는 함수로 보인다 `[추측: 안을 안 봄]`.
- 대상 = KERNELBASE 의 `KernelbasePostInit` 로 읽는 게 맞다 (이름 문자열 확인). 실제 주소는 동적 **Q49** (`ntdll+0xb81ce` 에서 `ln @rax`).
- `KernelbasePostInit` 이 하는 일은 모른다 `[추측]`.
- 비유: 개업 준비 마지막에 KERNELBASE 건물 "**개업 뒤 인사**" 호실 번호를 찾아 **바로 전화를 건다.** 전화는 교환원을 거친다.
- flow.md 그림 2 에선 이게 새 **회색 상자 `z3`** 이 됐다 ("어느 함수인지 실행 때 정해지는 간접 호출" 칸, run19 의 `z1`·`z2` 옆).

### 2.10 `LdrLoadDll` 안 — `0x4001` 은 "경로가 아니라 플래그"

(3)·(7) 이 부르는 `LdrLoadDll` (rva `0x3cbf0`) 이 `LdrpLoadDll` 앞에서 무엇을 하는지다. 모두 `[정적]`.

**첫 인자 `RCX` 는 두 얼굴이다**

- 낮은 비트가 **1** 이면 → "경로가 아니라 **플래그**" 로 읽는다. [비트와 플래그](run9-create-user-process-해설.md#비트와-플래그) 의 그 플래그다.
- kernel32 준비 때 `RCX = 0x4001`: 낮은 비트 1 → 플래그 방식. 비트를 지운 **`0x4000`** 을 지역 플래그 칸(`+0x58`)에 두고, 경로 칸(`+0x40`)은 **0 그대로** 넘긴다 (`+0x14f`, `+0x157`).
  - `0x4000` 은 공개 문서의 `LOAD_LIBRARY_SEARCH_SYSTEM32_NO_FORWARDER` 값과 같다 `[추측: 이름은 공개 문서 기억]`.
- 낮은 비트가 0 이고 0 이 아니면 → 검색 경로 포인터로 본다. `RCX==0` 이면 (2.8 의 ADVAPI32 호출) 아무것도 안 채운다.
- 비유: 주문서 첫 칸에 "**어디서 찾을지 주소**" 를 적을 수도, "**찾는 방식 도장**" 을 찍을 수도 있다. 맨 끝 작은 표시(낮은 비트)로 어느 쪽인지 구분한다. kernel32 주문서엔 도장이 찍혀 있다.

**단계 표** (run 문서 `LdrLoadDll` 표를 줄임)

| 단계 | 자리 | kernel32 준비 때 |
|---|---|---|
| 지역 구조체 비우기 | `+0x24 ~ +0x5a` (`[RSP+0x40 ~ +0xbf]`) | 그대로 |
| `RDX`(`DllCharacteristics`) 비트 옮기기 → `EBX` | `+0x64 ~ +0xa5` | `RDX=0` → `EBX=0` |
| 앞에서 돌려보내기 3갈래 | `+0x106 ~ +0x244` 등 | 셋 다 안 탐 (`0x4001 & 0x401 = 0x1`) |
| 경로 칸·플래그 칸 | `+0x14f`, `+0x157` | 경로 0, 플래그 `0x4000` |
| **`LdrpLoadDll`** | `+0x16b` / **`+0x170`** | 탄다 `[관찰]` run7 |
| 경로 해제 | `+0x189` / `+0x18e` | `[RSP+0xbc]` 가 켜졌을 때만. 켜는 곳은 더 안쪽 `[추측]` (Q11) |
| 결과 | `+0x18e ~ +0x19e` / `+0x1a3` | 성공일 때만 `*R9 = 항목+0x30`(`DllBase`), `LdrpDereferenceModule(항목)` |

- 돌려보내기 셋 중 ③ TEB `SameTebFlags` 비트 `0x2000` (PDB `LoaderWorker`) → `0xc000071c` 는 **로더 일꾼 스레드에서 못 부르게 막는 것**으로 보인다 `[추측]`. run19 의 일꾼 이야기와 이어지는 자리다.
- `LdrpDereferenceModule` 은 안쪽이 올려 둔 **임시 참조를 내려놓는** 것으로 보인다 `[추측: 이름]`. 비유: 빌려 간 열쇠 꼬리표를 하나 돌려놓기.
- **`LoadLibraryExW` 도 같은 본문이다** `[정적]`. 갈림은 인자(`RCX`·`RDX`)로만 정해진다. `LoadLibraryExW` 가 무엇을 싣는지는 KERNELBASE 코드라 모른다 → Q31 또는 kernelbase 정적 `[확인 필요]`.
- ntdll 안에서 `LdrLoadDll` 을 부르는 곳은 4곳이다 (지도): `LdrpInitializeKernel32Functions`, `LdrpCodeAuthzInitialize`, `RtlLogMessageInEventLogger`, `SbpResolveBasedOnName` `[정적]`.

### 2.11 `LdrpLoadDll` → `LdrpLoadDllInternal` — 넘기는 9개와 "상태 칸"

`LdrpLoadDll` (rva `0x1f9c0`) 의 순서 `[정적]`:

1. `+0x50 memset` → 앞 ETW 검사 → 이름 버퍼 준비
2. **`+0xc2 LdrpPreprocessDllName(이름, &버퍼, 0, &플래그)`** — 결과를 상태 칸 `[RSP+0x50]` 에. 음수면 다음 호출 건너뜀
3. **`+0x100 LdrpLoadDllInternal`** `[관찰]` run7 (자리)
4. (이름이 힙으로 옮겨졌으면) `+0x114` 해제
5. 뒤 ETW 검사 → `[RSP+0x50]` 반환

**넘기는 9개** `[정적]` (`+0xca ~ +0xfb`)

| 칸 | 값 |
|---|---|
| `RCX` | 전처리된 이름 버퍼 |
| `RDX` | `LdrLoadDll` 의 경로 구조체 (그대로) |
| `R8D` | 플래그 (전처리가 바꿨을 수 있음) |
| `R9D` | `4` |
| `[RSP+0x20]`, `[RSP+0x28]`, `[RSP+0x40]` | 0 |
| `[RSP+0x30]` | 항목 출력 칸 |
| `[RSP+0x38]` | **상태 출력 칸** `&[RSP+0x50]` |

- `R9D = 4` 는 PDB 열거 `_LDR_DLL_LOAD_REASON` 의 `LoadReasonDynamicLoad` 값과 **같다**. 이 자리가 "적재 이유" 라는 건 `[추측: 값이 같다는 것만]`.

**눈여겨볼 것 — 반환값이 "상태 칸"** `[정적]`

- `+0x100` 뒤에 `EAX` 를 **읽지 않는다**.
- 그래서 `LdrpLoadDll` 이 돌려주는 건 `LdrpLoadDllInternal` 의 반환값이 아니라, **그 안이 상태 칸에 적어 둔 값**이다.
- 칸에 실제로 쓰는지는 Q11.
- 비유: 배달 기사가 말로 대답하는 걸 듣는 게 아니라, 건네준 **결과 메모지**에 적힌 걸 읽는다.

**ETW 두 번은 추적이 켜졌을 때만** `[정적]`

- **새 말 — ETW**: Windows 의 이벤트 추적 기록 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 관리실 **작업 일지**.
- `PEB.TracingFlags`(+0x378) 비트 `0x4` (PDB `LibLoaderTracingEnabled`) 등이 켜졌을 때만 `LdrpLogEtwEvent(0x14a8 앞 / 0x14a9 뒤)`. test.exe 는 꺼져 있으면 그냥 지나갈 것 `[추측: 값은 동적]`.

**이름 주의** — `+0x10f` (ret `+0x114`) 해제 호출은 한 주소(rva `0x3d630`)에 이름이 9개다. [ICF 별칭](run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) 이야기다. 그래서 이름 대신 "메모리 해제 쪽 함수" 로 적었다 `[추측: 이름들로 짐작]`.

### 2.12 실패로 끝나는 길 7곳, 그리고 일부러 봐주는 `0xc0000135`

**7곳** `[정적]` (로그 파일 `minkernel\ldr\ldrinit.c`)

| # | 무엇이 실패하면 | 로그 줄 |
|---|---|---|
| 1 | (3) KERNEL32 적재 (`0xc0000135` 빼고) | `0x877` |
| 2 | (4) `BaseThreadInitThunk` 찾기 | `0x88e` |
| 3 | (7) KERNELBASE 적재 | `0x8cf` |
| 4 | (8) CodeAuthz (`0xc0000135` 빼고) | `0x8df` |
| 5 | (9) `KernelbasePostInit` 찾기 | `0x8f1` |
| 6 | (10) `KernelbasePostInit` 호출 | `0x8fa` |
| 7 | (6) `LdrpSnapKernelBaseExtensions` | **로그 없음** |

- 로그 6가지는 로그 함수 호출 자리 3곳(`+0x25c`·`+0x28c`·`+0x2d6`)을 나눠 쓴다.
- 어느 쪽이든 상태를 돌려주고, 호출자 `+0x19a2` 가 음수면 에러 끝으로 간다. [run17 해설 2.8](run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일) 의 "세팅이 실패하면 가게 문을 닫는다" 로 이어진다.
- 눈여겨볼 것: (5) `TermsrvGetWindowsDirectoryW` 찾기는 **실패해도 안 끝난다** (결과를 안 봄).

**`0xc0000135` 는 실패로 안 친다** `[정적]`

- `0xc0000135` = `STATUS_DLL_NOT_FOUND` 값이다 (run 문서). 이름 그대로 "DLL 을 못 찾음".
- 두 곳에서 **일부러** 봐준다: (3) kernel32 적재, (8) CodeAuthz.
  - (3) 이 이 값이면 (4)~(6) 을 건너뛰고 (7) 로 간다.
- 왜? kernel32 가 없는 프로세스 조건을 위한 길로 보인다 `[추측]`.
- 비유: "기본 설비 건물이 **아예 없는 동네**" 라면 그 건물 일은 넘어가고 KERNELBASE 쪽만 한다.

### 2.13 run18 이 남긴 전역 8개 — 이번 세 함수는 직접 안 읽는다

run18 앞쪽이 준비해 둔 전역 8개를 **누가 읽나** `refs` 로 봤다 `[정적]`.

- **이번 세 함수가 직접 읽는 건 하나도 없다.** 모두 한두 단계 아래에서 읽는다.
- 이번 길에 바로 닿는 건 셋이다:
  - `LdrpHashTable` — (2) 아래 `LdrpFindLoadedDllInternal +0x76` → `LdrpFindLoadedDllByName` 이 뒤진다 (2.3 의 색인 카드함)
  - `LdrpSystemDllBase` — 함수 찾기 세 곳이 거치는 `LdrGetProcedureAddressForCaller +0x1ca` 가 읽는다
  - `LdrpHeap` — 54곳이 읽는다. 이번 길에선 `LdrGetProcedureAddressForCaller`, `LdrpDereferenceModule +0xbb` 등
- 나머지 다섯 (`LdrpKnownDllDirectoryHandle`, `LdrpKnownDllPath`, `LdrpSystem32`, `RtlpSystem32Dirs`, `LdrpAppHeaders`) 은 KnownDlls·CFG·검색 경로·매핑 쪽 함수가 읽는다. 그 함수들은 `LdrpLoadDllInternal` 아래일 것 `[추측]` → **Q11**.
- 표 전체는 run 문서 "run18 이 남긴 전역 8개를 읽는 곳".
- 비유: run18 에서 관리실이 서랍에 준비물 8개를 넣어 뒀다. 이번 세 사람(주문 담당)은 서랍을 직접 열지 않는다. **배달 기사**(더 안쪽 함수)가 연다.

### 2.14 그 밖에 확인된 것

- **버전 대조 일치** `[정적]` — `_calls.tsv` 의 `+0xbb → ret +0xc0`, `+0x151 → ret +0x156`, `LdrLoadDll +0x16b → ret +0x170`, `LdrpLoadDll +0xfb → ret +0x100` 이 run7 콜스택 숫자와 모두 같다.
- **이름 확인** — `pdb_aliases.py --where ntdll.dll LdrLoadDll` → `LdrLoadDll rva=0x3cbf0` 그대로 (별칭 없음).
- **문자열을 원본 바이트로 읽은 이유** — 함수 이름 문자열 셋(`BaseThreadInitThunk` 등)은 PDB 이름이 없는 데이터라, `.asm` 주석엔 **가장 가까운 다른 PDB 이름 + 오프셋**이 붙어 있었다. 그 주석 이름은 엉뚱해서 문서에 안 썼다. 대신 원본 바이트로 글자를 직접 읽었다 (run 문서 도구 메모, plan 하네스 후보 (20)).
  - 비유: 우편함 이름표가 **옆집 이름**으로 붙어 있어서, 봉투를 직접 뜯어 읽은 셈이다.
- **간접 호출 ret 계산** — `+0x1aa` 는 5바이트 call 이라 ret `+0x1af` (2.9).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만.

| 자리 | 전 (run19 뒤) | 후 (run20) |
|---|---|---|
| 기준 | ntdll.pdb (run17 ~ run19), 정적 근거 run9 ~ run19 | ntdll.pdb (+ **run20**), 정적 근거 **run9 ~ run20** |
| 두 그림 사이 설명 | — | run20 문단: 그림 2 `(1) kernel32 준비` 상자에 **코드 순서 상자**를 넣었다 |
| 그림 2 `(1)` 상자 | `k1` → `k2 LdrLoadDll` … `k13 LdrLoadDll` (이름 없음) | `k2` "이름 KERNEL32.DLL, 못 찾았을 때만", `k13` "이름 KERNELBASE.dll. 이미 올라온 것 다시 찾기 추정". 새 `(정적)` 상자 넷: `ka` 이미 있나, `kb` `BaseThreadInitThunk` 찾기, `kc` `LdrpCodeAuthzInitialize`, `kd` `KernelbasePostInit` 찾기 |
| 그림 2 회색 상자 | `z1` "… BaseThreadInitThunk 추정" | `z1` "칸 = kernel32 에서 이름 BaseThreadInitThunk 로 찾은 주소. 실제 값은 실행 때". 새 **`z3`** "`RSP+0x58` 칸의 함수" (`KernelbasePostInit`) |
| 그림 2 화살표 (k1 에서) | `(1) +0xc0`, `(2) +0x156` | `(1) +0xa4`, `(2) +0xc0`, `(3) +0xff`, `(4) +0x156`, `(5) +0x165`, `(6) +0x19b`, `(7) +0x1af` |
| 2.4 `+0x1b5e` 줄 | 칸을 채우는 곳 `+0xe4` | `+0xe4`(칸 주소를 실음) → ret `+0xff` 호출, 읽기 `+0x1b45`. run20: 칸 값은 이름 `BaseThreadInitThunk` 로 찾은 주소 |
| 2.5 "올리기" 칸 | `[공백]` 아직 못 봄 | + "올리라고 부르는 호출과 DLL 이름만 `[정적]` (run20)" |
| 2.5 트리 | `(1) +0xc0`, `(2) +0x156` 둘 | 코드 순서 **(1) ~ (10)** 을 run7 콜스택 사이에 끼움 |
| 2.5 `LdrpInitializeKernel32Functions` 절 | `[관찰]` 두 번 부름, `[추측]` 스스로, run19 `[정적]` 줄 | + run20 `[정적]`: 두 이름, 순서, 이미 있나, `0x20000000`, `0xc0000135`, 실패 7곳, import 안 봄, `+0xe4` 뜻, `TermsrvGetWindowsDirectoryW`, 함수 이름 셋, `KernelbasePostInit`, CodeAuthz·ADVAPI32, 전역 8개 |
| 2.5 `LdrLoadDll` 안쪽 | `[관찰]` 사슬 | + 자리 `_calls.tsv` 일치, `LdrLoadDll` 이 앞에서 하는 일, `LdrpLoadDll` 의 9개·상태 칸 |
| 2.5 `[공백]` 올리는 순간 | `[추측]` 앞부분에서 kernel32·kernelbase | + run20: 둘째 이름 호출이 뒤 → 둘째는 다시 찾기 `[추측]`. "이미 있나" 코드 + 표에 끼우는 4곳 → 시작 때 없다 **유저 쪽 코드로 지지** |
| 2.5 시작 함수 | `[관찰]` 근처 기호 둘 | + 이름으로 찾아 쓰는 함수 셋 (Q13 에서 같이) |
| 2.10 | `RtlUserThreadStart +0x7` 이 칸을 읽음 (`refs`), 채우는 곳 `+0xe4` | + **채우는 곳 = ret `+0xff` `GetProc(kernel32, "BaseThreadInitThunk")`**. 남은 `[추측]` 둘 (본체인지, 부르는지) |
| 4장 공백 표 | 2.5 줄 셋 | 기존 줄에 run20 (일부 해결) 덧붙임 + 새 줄: (해결됨) 이 함수 순서, 실행 값 (Q49), 이름·뜻 못 정한 것 (run20), `LoadLibraryExW` 인자 |
| 4.1 | — | 회색 상자 (11) 줄에 run20, 새 줄 셋 (kernel32 준비 정적 상자, 두 `LdrLoadDll` 이름, `z3`) |
| 5장 | run19 까지 | run20 줄, 요청서 번호 표 run20 줄 |

태그로 보면 핵심 변화는 다섯이다:

- **두 `LdrLoadDll` 의 DLL 이름**: 모름 → **`[정적]`** (`KERNEL32.DLL` / `KERNELBASE.dll`, 원본 바이트).
- **`Kernel32ThreadInitThunkFunction` 칸 값**: `[추측]` `BaseThreadInitThunk` → **`[정적]` "kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소"**. 본체인지(forwarder)·`RtlUserThreadStart` 가 부르는지는 여전히 `[추측]`.
- **"exe import 와 상관없이 스스로"**: 머리 태그 `[추측]` 그대로, `[정적]` 근거가 run19 하나 → **run19·run20 둘**.
- **"시작 때 kernel32·kernelbase 는 없다"**: `[추측]` → **유저 쪽 코드로 지지** `[정적]`. 커널 쪽 "그 둘뿐" 은 그대로 미확인.
- **새 `[추측]`**: 둘째 `LdrLoadDll("KERNELBASE.dll")` 은 이미 올라온 것을 다시 찾는 호출. **새 `[정적]`**: kernel32 준비 중 ADVAPI32 가 올라올 수 있는 길이 코드에 있다.

그림 2 를 볼 때 주의 둘:

- **화살표 번호는 트리 번호와 다르다** (2.1). `k1` 에서 나가는 화살표 (1) ~ (7) 은 그림에 올린 것만 센 번호다.
- **`z3` 는 `k1`(`LdrpInitializeKernel32Functions`) 에서 나간다.** `z1`(출근 인사)은 `c4`(`LdrpInitializeProcess`) 에서 `(11) +0x1b5e` 로 나가는 것과 다르다. 둘 다 회색 점선 = "대상이 실행 때 정해지는 간접 호출" 이다.

### 큰 그림에서 어떻게 이어지나

- run19 에서 걸어 본 세팅 방 뒤쪽 10묶음 중, **묶음 5 (kernel32 들이기)** 의 문 안을 이번에 봤다.
  - 문 밖에서 본 것 (run19): 언제 부르나, 어느 칸을 채우나.
  - 문 안에서 본 것 (run20): 주문서 두 장의 이름, 그 사이에 호실 번호 세 개 찾기, 마지막 전화 한 통.
- **main 쪽으로**: 이번 `+0xff` 가 적은 칸을 묶음 7 의 `+0x1b59` 가 한 번 부르고, 나중에 본업 자리 `RtlUserThreadStart +0x7` 이 읽는다. 그 뒤 `BaseThreadInitThunk` → `mainCRTStartup` → `main` 은 아직 `[추측]` (Q17·Q18·Q30).
- **안쪽으로**: 두 주문서가 모두 `LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal` 로 내려간다. 진짜 "짐 들이기"(올리기)는 그 안 `+0x284` 앞쪽일 것이다 `[추측]`.
- **다음 구간** (`plan.md` "다음 항목"): **Q11 — `LdrpLoadDllInternal` (`+0x284` 앞 = 올리기 쪽), `LdrpPrepareModuleForExecution` callee** (정적).
  - 이번에 넘긴 연결점: `LdrpLoadDllInternal` 의 인자 9개(2.11), `[RSP+0xbc]` 경로 해제 바이트를 켜는 곳, 상태 칸에 쓰는지, 전역을 읽는 함수들(KnownDlls·`LdrpHashTable`·`LdrpAppHeaders`·CFG 쪽).
  - 핵심 ① 의 "둘째는 다시 찾기" `[추측]` 도 Q11 에서 코드로 볼 수 있다. 실마리: `LdrpLoadDllInternal +0x1ed` 가 부르는 `LdrpFindOrPrepareLoadingModule` 이 `LdrpHashTable`(2.3 의 색인 카드함)을 읽는다는 게 이번 `refs`·지도로 나왔다 (결과서 4번 표).
- 버스를 탈 때(동적) 볼 것은 **Q49** (이 함수 실행 값, ADVAPI32 포함), **Q48** (칸 값), **Q20** (kernelbase 가 올라오는 순간) 에 정리됐다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 때 실제 값: (2) 가 "못 찾음" 인가, 두 `LdrLoadDll` 결과·베이스, (5) 출력 칸이 `TermsrvGetWindowsDirectoryW` 인가, (6)·(7)·(8) 결과, (10) 직전 `rax` 가 `KERNELBASE!KernelbasePostInit` 인가 | 갈림과 칸은 `[정적]`, 값은 실행 때 정해진다 | **Q49** (동적). 멈춤점은 run 문서 "아직 확인 필요" (`ntdll+0xb80c8` 등) |
| `LdrLoadDll` 이 **몇 번, 어떤 이름**으로 불리나 — **ADVAPI32 가 끼나** | (8) 안 ADVAPI32 길은 레지스트리 값에 달렸다 | **Q49** (`ntdll+0x3cbf0` 에서 `dS @r8`) |
| `Kernel32ThreadInitThunkFunction` 의 실제 주소가 `KERNEL32!BaseThreadInitThunk` 본체인가 (forwarder 면 다른 곳) | 이름으로 찾는다는 것까지만 코드로 보인다. 주소는 실행 때 | **Q48** (동적, `dq ntdll+0x1d2760 L1` → `ln`, `+0x1b59` 에서 `ln @rax`) |
| `RtlUserThreadStart` 가 그 칸의 함수를 부르나, 그 뒤 `main` 까지 | `RtlUserThreadStart` 본문은 안 봤다 | **Q17** (정적), **Q30** (동적) |
| kernelbase 가 **첫째 호출 안에서** 올라오나, 둘째는 정말 "다시 찾기" 인가 | `LdrpLoadDllInternal` 안쪽(올리기 쪽)을 안 읽었다 | **Q11** (정적), **Q20** (동적) |
| `LdrpLoadDllInternal` 이 9개 인자를 어떻게 쓰나, `[RSP+0xbc]` 를 켜는 곳, 상태 칸에 쓰나, 전역을 읽는 함수들 | 범위 밖 (한 단계 아래) | **Q11** (정적) |
| KERNELBASE·KERNEL32 **시작 함수 이름** (run7 "기호 근처"), 이번에 나온 이름 셋(`BaseThreadInitThunk`·`TermsrvGetWindowsDirectoryW`·`KernelbasePostInit`)의 정체 | kernel32·kernelbase 바이너리는 이번에 안 봤다 | **Q13** (정적), Q21·Q22 (동적) |
| `LoadLibraryExW` 가 `LdrLoadDll` 에 싣는 인자 | KERNELBASE 코드다 | **Q31** (동적) 또는 kernelbase 정적 |
| 이름·뜻 못 정한 것: `PEB.ProcessParameters.Flags` `0x20000000`, `0x4000` 플래그 이름, `R9D=4` 가 적재 이유인지, `LdrpSnapKernelBaseExtensions`·`KernelbasePostInit` 이 하는 일 | PDB 에 이름이 없거나, 공개 문서 기준이거나, 본문을 안 봤다 | flow.md 4장 공백 줄 "이름·뜻 못 정한 것 (run20)" (대기열 줄은 따로 없다) |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run20-kernel32-load.md`](run20-kernel32-load.md) — 특히 "`LdrpInitializeKernel32Functions` 순서와 갈림", "`LdrLoadDll`", "`LdrpLoadDll`", "run18 이 남긴 전역 8개", "시작 때 kernel32 는 아직 없다", "호출 관계"
- 정적 요청서: [`_workspace/sast/15_kernel32-load.md`](../../_workspace/sast/15_kernel32-load.md)
- 정적 결과서 (질문별 답 1-a ~ 1-h, `LdrLoadDll`·`LdrpLoadDll` 단계, `refs` 8개 표, 그림 2 상자 후보, 동적 멈춤점 표, Q11 ~ Q13 연결점): [`_workspace/sast/15_kernel32-load_result.md`](../../_workspace/sast/15_kernel32-load_result.md)
- 앞 바퀴 해설:
  - [`run19-proc-init-modules-해설.md`](run19-proc-init-modules-해설.md) — 2.5 묶음 5 (kernel32 들이기), 2.7 dll1·dll2 쪽 후보 (import 를 보는 쪽), 2.10 간접 호출 4곳 (`+0x1b59` 출근 인사)
  - [`run18-proc-init-setup-해설.md`](run18-proc-init-setup-해설.md) — 2.6 명부, 2.7 명부 첫 줄 (ntdll 항목), 2.8 앞쪽은 아무것도 안 올린다
  - [`run16-thunk-context-해설.md`](run16-thunk-context-해설.md) — 2.3 본업 자리 이야기, 2.9 내보내기 표·이름으로 찾아 벽 주소록에 적기
  - [`run10-allocate-process-해설.md`](run10-allocate-process-해설.md) — 3.11 "exe·ntdll 을 올린다" 와 "그 둘뿐" 은 다른 말
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — 부르는 자리와 돌아올 자리, 직접 호출과 간접 호출, 비트와 플래그, ICF 별칭
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "두 그림 사이 설명" run20 문단
  - "그림 2" `(1) kernel32 준비` 상자 (`ka`·`kb`·`kc`·`kd`), 회색 상자 (`z1`·`z3`)
  - "2.4" `+0x1b5e` 줄, "2.5" 트리와 `LdrpInitializeKernel32Functions`·`LdrLoadDll` 안쪽·`[공백]` 올리는 순간·시작 함수 절, "2.10" run20 줄
  - "4. 공백 목록" 2.5 줄들, "4.1"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q10(끝), Q11(다음), Q13·Q20·Q48 출처, Q49(새), 하네스 후보 (20)

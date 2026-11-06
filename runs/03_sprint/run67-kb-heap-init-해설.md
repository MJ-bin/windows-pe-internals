# run67 kb-heap-init — 해설

> 이 글은 [run67 문서](run67-kb-heap-init.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q128** — PE_emulator 요청 16 의 급한 셋 + 4번 (`_workspace/plan.md`, 이번에 **끝**)
  - 사용자 결정 (2026-11-06): Q128 → Q129 → Q104 VM 단계.
  - 다음 항목: **Q129** (요청 16 의 나머지 본문).
  - 남은 질문이 간 곳: 동적 4건 → **Q104 VM 단계**, 작은 정적 셋 → 새 **Q130** (맨 뒤).
- 근거:
  - run 문서 [`run67-kb-heap-init.md`](run67-kb-heap-init.md) (가장 중요)
  - 요청서 [`_workspace/sast/62_kb-heap-init.md`](../../_workspace/sast/62_kb-heap-init.md)
  - 결과서 [`_workspace/sast/62_kb-heap-init_result.md`](../../_workspace/sast/62_kb-heap-init_result.md) (판정: **분석성공**)
  - flow.md 변경: 기준 · 그림 2 (KERNELBASE·KERNEL32 시작 함수 아래 새 상자, exe 쪽 `LdrpFindDllActivationContext` 상자) · 2.5 새 절 · 2.6 새 절 · 4장 · 4.1
  - 짐 지도 변경: 3.8 · 새 7.6.7 · 새 7.26 · 7.3 숫자 (3절에서 짧게)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다.
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 를 **다시 읽은 것** 이다. 새 실행이 아니다.
  - 그 사진엔 **kernelbase·kernel32 의 `.data` 가 없다** (ntdll `.data` 만 읽었다). 그래서 이번 칸들 자체의 값은 못 봤고, 값의 **출처** 칸만 봤다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 전원 켜기 = DLL 시작 함수 (DllMain) 부르기, 이유 1 = 프로세스에 처음 붙음 → [run25 해설](../02_sprint/run25-dll-entry-해설.md)
- 프로세스 힙 = 가게 **창고**, `PEB.ProcessHeap` = 창고 주소를 적은 **가게 장부** 칸 → [run46 해설](run46-process-heap-해설.md)
- ANSI 코드 페이지 (가게가 쓰는 **사전 번호**), 글자 서랍 `GlobalRtlNlsState` → [run57 해설](run57-nls-apiset-해설.md) 2.8 · 2.9
- CFG = 간접 호출 목적지를 확인하는 **교환원** → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) 2.9
- 사진 P1a·P1b·P2 → [run44 해설 2.8](run44-baggage-snap-해설.md)

이번에 처음 (또는 거의 처음) 나오는 말 — **매니페스트·활성화 컨텍스트**, **탐침 (= 콜백)** — 은 2.5 에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**.
- KERNELBASE·KERNEL32 = 가게에 들인 **건물** 둘. 시작 함수 = 건물 **전원 켜기**.
- 프로세스 힙 = **창고**, `PEB.ProcessHeap` = 장부에 적힌 **창고 주소**.
- 이번에 새로 붙는 그림:
  - 건물마다 자기 **수첩** (그 DLL 의 전역) 이 있다. 전원을 켜면서 수첩에 "우리 창고는 어디" 같은 걸 받아 적는다.

### 한 줄로 먼저 — 무엇을 얻었나

| PE_emulator 가 막힌 곳 | 답 (run 문서 그대로) |
|---|---|
| ① `GlobalAlloc → RtlAllocateHeap(BaseHeap = 0)` 에서 멈춤 | **`BaseHeap` = `PEB.ProcessHeap` (PEB `+0x30`) 그대로.** 새 힙을 안 만든다 `[정적]` (2.3) |
| ② `GetCommandLineA` 가 빈 값을 줄 수 있음 `[추측]` | 성공이면 프로세스 힙에 새 **9바이트 `"test.exe\0"`**. 실패면 빈 글이 아니라 **NULL** `[정적]` (2.4) |
| ③ 매니페스트 탐침 칸이 0 → exe import 길이 갈릴 수 있음 `[추측]` | kernel32 시작 함수가 exe import 올리기보다 **앞**이다. 그래서 진짜는 exe·dll1·dll2 때 칸 = **`kernel32+0x23f90`** `[정적]` (2.5) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

kernel32 를 들이고 **전원을 켜는 안쪽**, 그리고 그 뒤 **exe 의 import (dll1·dll2) 를 들이는 자리** 다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ … 창고 짓기 (run46), 명부에 ntdll·exe
 │         ├─ +0x19a2  kernel32 들이기 + 전원 켜기
 │         │     ├─ KERNELBASE 시작 함수    ← 이번 ① ② (힙 칸, 명령줄 사본)
 │         │     └─ KERNEL32 시작 함수      ← 이번 ③ (탐침 칸 채우기)
 │         ├─ +0x1aa9  exe 의 import (dll1·dll2) 들이기
 │         │     └─ 모듈마다 탐침 부르기     ← 이번 ③ (칸을 읽는 쪽)
 │         └─ … dll 전원 켜기 → TLS 콜백
 └─ … → NtContinue → exe 진입점 → main
                 └─ OutputDebugStringA → … → GlobalAlloc   ← ① 이 멈춘 곳 (은퇴 뒤)
```

### 왜 이 질문이 지금 왔나

- PE_emulator 는 kernelbase·kernel32 시작 함수를 **진짜로 돌리지 않고 흉내 낸다** (칸에 직접 쓰기 + 진짜 ntdll 함수 덫).
- 흉내 낼 근거 (본문 asm) 가 없던 자리는 "공사 중" 으로 비워 뒀다. 그 빈 자리 셋에서 막혔다 (요청서):
  - (a) **가장 급함**: 은퇴 뒤 test.exe 가 `OutputDebugStringA → CreateDBWinMutex → GlobalAlloc → RtlAllocateHeap(BaseHeap = 0)` 에서 멈춤.
  - (b) `BaseAnsiCommandLine` 이 비어 `GetCommandLineA` 가 빈 값을 줄 수 있음 `[추측]`.
  - (c) `LdrSetDllManifestProber` 가 공사 중이라 탐침 칸이 0 → exe import 길이 진짜와 갈릴 수 있음 `[추측]`.

### 무엇이 궁금했나

1. `KernelBaseDllInitializeMemoryManager` 가 쓰는 칸과 **값의 출처**. 새 힙을 만드나, PEB 칸을 베끼나?
2. `KernelBaseGlobalData +0x40 ~ +0x57` 을 누가 쓰나.
3. 명령줄 ANSI 사본을 만드는 `RtlUnicodeStringToAnsiString` 은 어느 갈래로 가나, 실패하면?
4. `LdrSetDllManifestProber` 가 쓰는 칸. 그리고 **kernel32 시작 함수와 exe import 중 누가 먼저인가.**

### 바퀴 전에 알던 것

- `[정적]` (run25) 두 시작 함수의 이름과 대표 호출만. 예: `_KernelBaseBaseDllInitialize +0x1b2` 에 `KernelBaseDllInitializeMemoryManager`, kernel32 `_BaseDllInitialize +0xba` 에 `LdrSetDllManifestProber` — 이름만 있고 안쪽은 안 봤다.
- `[정적]` (run46) 프로세스 힙은 `LdrpInitializeProcess +0xa4b LdrpInitializeProcessHeap` 가 만든다.
- `[관찰]` (run44 사진) ntdll 탐침 칸 셋이 P1a 에선 0, P1b 부터 값이 있다. 누가 언제 썼는지는 사진만으로 몰랐다.

---

## 2. 찾은 것

### 2.1 한눈에 — 핵심 줄기

run 문서 트리에서 대표만 뽑았다. 라벨은 ret 자리 (콜스택에 보이는 오프셋).

```
LdrpInitializeProcess
 ├─ +0x19a2  LdrpInitializeKernel32Functions → … (kernel32 들이기, 전원 켜기)
 │     ├─ KERNELBASE 시작 함수 → _KernelBaseBaseDllInitialize
 │     │     ├─ +0x1b2  KernelBaseDllInitializeMemoryManager
 │     │     │            └─ +0x1b  BaseHeap ← [PEB+0x30]                       ① 
 │     │     └─ +0x497  RtlUnicodeStringToAnsiString (명령줄 ANSI 사본)           ②
 │     │                  └─ +0x2c7  프로세스 힙에서 9B
 │     └─ KERNEL32 시작 함수 → _BaseDllInitialize
 │           └─ +0xba   LdrSetDllManifestProber(kernel32+0x23f90, …)            ③ 칸 채우기
 └─ +0x1aa9  LdrpMapAndSnapDependency (exe 문맥)          ← 여기부터 탐침 칸 ≠ 0
       └─ +0x63  LdrpFindDllActivationContext
                  └─ +0x83  (CFG 간접) KERNEL32!BasepProbeForDllManifest     ③ 칸 읽어 부르기
```

- 전부 `[정적]`. 탐침 칸의 실제 값만 `[관찰]` (run44 사진).
- 사이에 안 그린 호출이 많다 (`CsrClientConnectToServer` 등은 Q129).

### 2.2 한 줄씩 읽는 법

run 문서 "호출 지점" 표에서 한 줄을 골라 끝까지 읽어 본다.

**`KERNELBASE!_KernelBaseBaseDllInitialize | +0x1ad | +0x1b2 | KERNELBASE!KernelBaseDllInitializeMemoryManager | 이유 1 갈래`**

- **누가**: KERNELBASE 시작 함수의 몸통 `_KernelBaseBaseDllInitialize`.
  - 진입점 `KernelBaseDllInitialize` → `+0xd` `KernelBaseBaseDllInitialize` → `+0x44` 꼬리 점프로 여기 온다 `[정적]`.
- **어디서**: CALL 명령이 `+0x1ad`, 다음 줄이 `+0x1b2`. 콜스택엔 `_KernelBaseBaseDllInitialize+0x1b2` 로 보일 자리다.
- **누구를**: `KernelBaseDllInitializeMemoryManager` (rva `0xf8344`, 155B). 인자도 반환값도 없다.
- **조건**: 이유 1 (프로세스에 처음 붙음) 갈래 안.
- **무엇을 위해**: KERNELBASE 수첩에 "우리 창고 = 어디" 를 적으려고 (2.3).

---

### 2.3 ① `BaseHeap` — 새 창고가 아니라, 같은 창고에 이름표 하나 더

#### 막힌 곳

- `GlobalAlloc +0x7a` (ret `+0x81`) 가 `RtlAllocateHeap([BaseHeap], 플래그, 크기)` 를 부른다 `[정적]`.
- PE_emulator 는 `KernelBaseDllInitializeMemoryManager` 를 비워 뒀다 → `BaseHeap` 칸이 0 → 첫 인자 0 → 멈춤.
- **`BaseHeap`** = KERNELBASE 전역 칸 (rva `0x3aefa0`, 8B). `GlobalAlloc` 같은 KERNELBASE 함수가 "어느 창고에서 꺼낼까" 를 여기서 읽는다.

#### 답 — 세 줄이면 끝난다

`KernelBaseDllInitializeMemoryManager` 의 앞쪽 (run 문서 근거 줄 그대로):

```
+0x6   MOV RAX,GS:[0x60]        ; PEB
+0x17  MOV RCX,[RAX+0x30]       ; PEB.ProcessHeap
+0x1b  MOV [BaseHeap],RCX
```

- 단계별로:
  1. `GS:[0x60]` 에서 PEB 주소를 받는다.
  2. PEB `+0x30` = `ProcessHeap` (장부의 창고 주소) 를 읽는다.
  3. 그 값을 그대로 `BaseHeap` 에 쓴다. **조건 없음** `[정적]`.
- 그래서 → **`BaseHeap` = `PEB.ProcessHeap`**. 새 힙을 만드는 호출이 없다 `[정적]`.
- `[관찰]` run44: `PEB.ProcessHeap` = `0x25996f30000` (P1a·P1b·P2 같음). 그러니 `BaseHeap` 도 이 값이어야 한다 — 단 kernelbase 칸 자체는 사진에 없다 (`[정적 + 관찰 값]`).

비유: 창고는 하나다. 관리실 장부엔 "창고" 라고 적혀 있고, KERNELBASE 수첩엔 "BaseHeap" 이라고 적혀 있다. **같은 창고에 이름표만 하나 더 붙인 것.** run46 의 `LdrpHeap` 도 같은 꼴이었다 (관리실도 같은 창고를 씀).

#### 같은 함수가 같이 채우는 것 (짧게)

- `SystemRangeStart` (rva `0x3b0350`) = `0xFFFF800000000000` `[정적]`.
  - 칸이 0 일 때만 (처음은 0) `NtQuerySystemInformation(0x32)` syscall 을 탄다. 이 함수의 **유일한 syscall**.
  - 커널은 변수를 안 읽고 **상수**를 준다 (`ExpQuerySystemInformation +0x1068`) `[정적]`.
  - 이름으로 보아 유저 공간과 커널 공간의 경계 주소다 `[추측]` (일반적으로 알려진 설명).
- 핸들 표 `BaseHeapHandleTable` (`0x30`B) = `{0xffff, 0x10, 나머지 0}` — `RtlInitializeHandleTable` 이 채움 `[정적]`. 이동식 블록 (`GMEM_MOVEABLE`) 쪽에서 쓴다.
- 끝으로 셋의 사본을 `KernelBaseGlobalData +0x40` (BaseHeap) · `+0x48` (핸들 표 주소) · `+0x50` (SystemRangeStart) 에 둔다 `[정적]`.
  - 이 구간을 **쓰는 곳은 이 함수의 세 줄뿐** (`_refs` 313줄 중 WRITE 셋) `[정적]`. 질문 2 의 답.

#### 플래그도 정해진다

- `GlobalAlloc` 플래그 = `옵션 | ([KernelBaseGlobalData+0] + 0x100000)` `[정적]`.
- `KernelBaseGlobalData+0` 은 `RtlCreateTagHeap` 반환 (`_KernelBaseBaseDllInitialize +0x1d6` → `+0x1db`). `NtGlobalFlag & 0x800` 이 없으면 0 `[정적]`. `[관찰]` run44 `NtGlobalFlag` = 0.
- 그래서 → 플래그 = **`0x100000 | 옵션`**.

#### 두 번 쓰이는 건 정상

- KERNELBASE 진입점이 몸통에서 돌아온 뒤 (`+0xd`), **이유와 상관없이** `KernelBaseGlobalData` 에서 다시 읽어 `SystemRangeStart` (`+0x20`) · `BaseHeap` (`+0x2e`) · `BaseHeapHandleTablePtr` (`+0x3f`) 에 쓴다 `[정적]`. 같은 값이다.
- KERNEL32 도 **자기 수첩**에 `BaseHeap` (rva `0xc2240`) · `SystemRangeStart` (rva `0xc2238`) 를 갖는다. `_BaseDllInitialize +0xc7` 의 메모리 관리 사본 함수가 `KernelBaseGetGlobalData` 로 받은 `+0x40` · `+0x50` 을 옮긴다 `[정적]`.
  - 이 함수는 PDB 에 같은 주소 이름이 2개라, 이름 대신 하는 일로 적었다 (후보 `BaseDllInitializeMemoryManager2`, `BaseDllInitializeMemoryManager`).

PE_emulator 에 넘길 한 줄 (짐 지도 7.6.7): **`BaseHeap` = `PEB.ProcessHeap`**, `SystemRangeStart` = `0xFFFF800000000000`, `KernelBaseGlobalData+0` = 0, `BaseHeapHandleTable` = `{0xffff, 0x10, 0 …}`.

---

### 2.4 ② 명령줄 ANSI 사본 — 9바이트 `"test.exe\0"`, 실패면 NULL

#### 막힌 곳

- `GetCommandLineA` 는 `BaseAnsiCommandLine` (rva `0x3af050`) 의 `Buffer` 를 그대로 돌려준다 (`+0x0 MOV RAX,[BaseAnsiCommandLine+0x8]` → `+0x7 RET`) `[정적]`.
- PE_emulator 엔 이 칸을 채우는 `RtlUnicodeStringToAnsiString` 본문이 없었다 → "빈 값을 줄 수 있음" `[추측]`.

#### 답 — 두 사본은 모양이 다르다

KERNELBASE 시작 함수 몸통에서 차례로 (`[정적]`):

1. `+0x485`·`+0x489`: `ProcessParameters.CommandLine` 의 **머리 16B 만** `BaseUnicodeCommandLine` 에 복사.
   - 머리 = 길이·최대 길이·글자 버퍼 주소. 글자는 매개변수 블록 안에 그대로 두고 **같이 쓴다**. 새 버퍼 없음.
2. `+0x490` (ret `+0x497`): `RtlUnicodeStringToAnsiString(&BaseAnsiCommandLine, &BaseUnicodeCommandLine, 1)`.
   - 셋째 인자 1 = "출력 버퍼를 새로 받아라". 그래서 `+0x2c7` 에서 문자열 버퍼 할당 함수 → `RtlAllocateHeap(PEB.ProcessHeap, 0, 크기+1)`.
   - **ANSI 사본만 창고에서 새 상자 하나**를 받는다.

비유: 유니코드 사본은 **원본 서류철 위치만 적은 메모**. ANSI 사본은 원본을 **다른 글씨체로 새로 베껴 쓴 종이 한 장** (창고에서 받은 새 종이).

#### test.exe 에선 어떤 값인가

- 입력 `[관찰]` run44: `CommandLine = {Length 0x10, MaximumLength 0x12, Buffer 0x25996f3813e}`, 글 `"test.exe"`.
- 갈래: 이 VM 은 ACP 949 · `DBCSCodePage 1` 이라 **DBCS 갈래** (`+0x76 JNZ` → `+0x28d`) `[정적]` · 표 값은 `[관찰]` 사진 NLS 표. UTF-8 갈래 (`0xfde9`) 는 안 탄다.
  - 길이 셈과 변환을 **함수 안에서 직접** 한다 (다른 변환 함수 안 부름). syscall 없음 `[정적]`.
- 출력 `[정적]`: `BaseAnsiCommandLine = {Length 8, MaximumLength 9, Buffer → 힙 새 9바이트 "test.exe\0"}`, 반환 0.
  - 9 = 글자 8 + 끝 표시 NUL 1.
  - "ANSI 길이 = 유니코드 길이 / 2" 는 **ASCII 만일 때만** 맞다. 한글처럼 949 에서 2바이트인 글자가 있으면 더 길어진다.

#### 실패하면 — 빈 글 `""` 이 아니라 NULL

단계별로 (`[정적]`):

1. 변환이 음수 상태를 주면 `+0x49e JS` → `+0x6b0`.
2. `+0x6b0` 에서 `Buffer` 를 0, `+0x6b7` 에서 길이 둘을 0 으로.
3. `+0x6bd JMP +0x4a4` — **DllMain 은 실패하지 않고 계속** 간다.
4. 그 뒤 `GetCommandLineA` 는 `Buffer` 를 그대로 주니 → **NULL**.

- 실패할 수 있는 상태값: 크기가 `0xffff` 를 넘으면 `0xC00000F0`, 힙 할당이 NULL 이면 `0xC0000017` 등 `[정적]`.
- 정정 한 줄: PE_emulator 원문의 "빈 값" 은 정확히는 **NULL** 이다 (run 문서 "정정").

---

### 2.5 ③ 매니페스트 탐침 칸 — kernel32 가 먼저 출근한다

#### 새 말 둘

- **매니페스트 · 활성화 컨텍스트**: 매니페스트 = DLL·exe 에 붙여 넣는 **설정 쪽지** (XML), 활성화 컨텍스트 = 그 쪽지를 읽어 만든 **설정 묶음** `[추측]` (일반적으로 알려진 설명). run57 해설 2.8 에서 "활성화 문맥" 으로 한 번 풀었다.
  - PE 파일 안에선 자원 (`RT_MANIFEST`, `0x18`) 으로 들어 있다. 탐침이 이 자원을 찾는다 `[정적]`.
- **탐침 (= 콜백)**: kernel32 가 ntdll 칠판에 **"DLL 들일 때마다 이 번호로 전화해"** 하고 적어 둔 함수 주소. ntdll 은 직접 쪽지를 안 찾고, 그 번호로 kernel32 에게 물어본다.
  - 칠판 칸 이름이 `LdrpManifestProberRoutine` (ntdll rva `0x1d23c0`). 비어 있으면 (0) 전화하지 않고 넘어간다.
  - 콜백 일반은 [run12 해설](../02_sprint/run12-insert-thread-해설.md) 의 "알림 함수(콜백)" 와 같은 꼴이다.

비유: 짐 상자 (DLL) 를 들일 때마다 관리실이 kernel32 직원에게 **"이 상자에 설명서 들어 있어요?"** 하고 전화한다. 직원이 아직 출근 전이면 (칸 0) 묻지 않고 넘어간다.

#### 칸을 채우는 쪽 — 22바이트짜리 함수

- kernel32 `_BaseDllInitialize +0xb3` (ret `+0xba`) 가 `LdrSetDllManifestProber(kernel32+0x23f90, kernel32+0x61520, [__imp_ReleaseActCtx])` 를 부른다 `[정적]`.
- 이 함수 (rva `0x10c7e0`, 22B) 는 인자 셋을 칠판 칸 셋에 그대로 적고 끝 `[정적]`:
  - `LdrpManifestProberRoutine` ← `kernel32+0x23f90` (`BasepProbeForDllManifest`)
  - `LdrpCreateActCtxLanguageW` ← `kernel32+0x61520`
  - `LdrpReleaseActCtxW` ← kernelbase `ReleaseActCtx`
  - 호출·검사·syscall 없음. 셋 다 쓰는 곳은 이 함수 하나.
- `[관찰]` run44 사진: P1a 셋 다 0 → P1b 부터 `kernel32+0x23f90` 등. 정적 값과 같다.
- 둘째 호출 `+0x3af/+0x3b6` 은 셋 다 0 으로 되돌리는 **attach 실패의 정리 길**. 정상 실행은 안 탄다 `[정적]`.

#### 칸을 읽는 쪽

- `LdrpMapAndSnapDependency +0x5e` (ret `+0x63`) → `LdrpFindDllActivationContext(모듈 항목)` `[정적]`.
- 그 안에서 (`[정적]`):
  - `+0x6` · `+0x1a JZ` → `+0x114`: 칸이 0 이면 **바로 0 을 돌려주고 아무 칸도 안 쓴다**.
  - 칸 ≠ 0 이면 `+0x7e` (ret `+0x83`) 에서 그 함수를 **CFG 간접 호출** (교환원 거침).
  - 탐침이 핸들을 주면 그때만 모듈 항목 `+0x88` (`EntryPointActivationContext`) 에 쓴다. 쓰는 칸은 이것 하나, PEB 는 안 쓴다.

#### ★ 누가 먼저인가 — 짐작이 틀렸다

요청서 4(b) 에 오케스트레이터가 덧붙인 짐작은 이랬다 (plan.md 2026-11-06):

> kernel32 시작 함수가 dll1·dll2 의 `LdrpFindDllActivationContext` 보다 **뒤라면**, 진짜도 0 이라 PE_emulator 의 0 이 맞다.

**틀렸다. 앞이다.** 단계별로 (`[정적]` 코드 길):

1. kernel32 시작 함수는 `LdrpInitializeProcess +0x199d` (ret `+0x19a2`) `LdrpInitializeKernel32Functions` → `LdrLoadDll("KERNEL32.DLL")` → … 안에서 돈다.
2. exe import 올리기는 `+0x1aa4` (ret `+0x1aa9`) `LdrpMapAndSnapDependency` — `+0x19a2` **뒤** 다.
3. 그래서 → exe → dll1 → dll2 를 올릴 때 탐침 칸은 이미 **`kernel32+0x23f90`** 이고, `+0x83` 에서 탐침을 부른다.
4. 거꾸로 kernel32·kernelbase **자신**을 올릴 때는 시작 함수 전이라 칸 0 → `+0x1a` 에서 0. **PE_emulator 의 0 은 이때만 맞다.**

- `[관찰]` run44 사진도 같은 쪽 (P1a 0 → P1b 값) 이다. 단 P1a ~ P1b 사이에 kernel32 올리기와 exe import 올리기가 **둘 다** 들어 있어서, 사진만으로는 순서를 못 가른다. 실행 순서 확인은 동적 (4절).
- 요청서는 고치지 않았다 (run 문서 "정정").

#### 그런데 결과 칸은 같다 — 빠지는 건 "부르는 함수 하나"

- `[정적+샘플]` sample 셋 (test.exe·dll1·dll2) 은 PE 자원 디렉터리가 RVA 0 / 크기 0 → **매니페스트가 없다**.
- 탐침 `BasepProbeForDllManifest` 는 `+0x86/+0x8d LdrResFindResourceDirectory` (`RT_MANIFEST` `0x18`, ID 2) 로 찾고, 찾았을 때만 `+0xea/+0xf1 CreateActCtxW` 를 탄다 `[정적]`. sample 셋은 안 탄다.
- 그래서 탐침은 0 을 돌려주고 `+0x88` 을 안 쓸 것이다 `[추측: 실행이 성공한다는 사실에서]`.
  - 왜 `[추측]` 인가: 탐침이 "자원 없음" 일 때 주는 **정확한 상태값**은 `LdrpResGetResourceDirectory` 안쪽이 정하는데, 거기까지 안 읽었다.
  - 그 값이 "없음 취급" 집합 (`0xC000000F` 등) 이거나 0 이면 로드는 계속된다 `[정적]`. 그 밖의 음수면 로드 실패다.
- 정리: PE_emulator 가 칸을 0 으로 둬도 **남는 칸 값은 같다**. 다만 진짜에 있는 **"탐침 부르기" 한 번이 빠진다** (exe·dll1·dll2 마다).

비유: 진짜 가게에선 kernel32 직원이 일찍 출근해서, 상자마다 "설명서 있어요?" 전화를 받고 "없어요" 라고 답한다. PE_emulator 가게에선 직원이 없어서 전화를 안 한다. 상자 정리 결과는 같지만, **통화 기록 한 줄이 없다.**

---

### 2.6 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

| 무엇 | 사진 값 | 이번 정적 결과와 |
|---|---|---|
| 탐침 칸 셋 (`ntdll+0x1d23a0` ~ `+0x1d23c0`) | P1a 0 → P1b·P2 `kernel32+0x61520` · `kernelbase+0xefcd0` · `kernel32+0x23f90` | 같다 |
| `PEB.ProcessHeap` | `0x25996f30000` (P1a·P1b·P2) | `BaseHeap` 의 출처 |
| `CommandLine` | `{0x10, 0x12, 0x25996f3813e}` `"test.exe"` | 출력 `{8, 9, …}` 의 입력 |
| NLS 표 | 949 · 최대 2바이트 · `DBCSCodePage 1` | DBCS 갈래 |
| `PEB.ActivationContextData` | `(null)` (사진 8장) | exe 항목도 탐침을 부른다 (`+0x16c` "0 → 계속") |
| `NtGlobalFlag` | 0 | `KernelBaseGlobalData+0` = 0 |

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 곳

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | pdb 목록 run66 까지 | 넷 다 **run67** 추가 · 정적 바퀴 한 줄 (run44 로그 다시 읽음) |
| 그림 2 KERNELBASE | 시작 함수 상자 하나 | + `KernelBaseBaseDllInitialize` ⇒ `_KernelBaseBaseDllInitialize` → `(1) +0x1b2` `KernelBaseDllInitializeMemoryManager` / `(2) +0x497` `RtlUnicodeStringToAnsiString`. 모두 `(정적)` 상자 |
| 그림 2 KERNEL32 | 시작 함수 상자 하나 | + `_BaseDllInitialize` → `+0xba` `LdrSetDllManifestProber` `(정적)` |
| 그림 2 exe 쪽 | `LdrpMapAndSnapDependency` 아래 (1) ~ (4) | 맨 앞에 **`(1) +0x63 LdrpFindDllActivationContext`** → `?` `BasepProbeForDllManifest`. 뒤 번호가 한 칸씩 밀림 ((2) ~ (5)) |
| 2.5 새 절 | — | "시작 함수 안에서 놓는 칸" — 트리 + `[정적]` 줄 (2.3 · 2.4 · 2.5 의 내용) |
| 2.6 새 절 | `+0x63` 줄에 이름만 | `LdrpFindDllActivationContext` 절 — 갈래 · "없음 취급" 집합 · **시간 순서 (kernel32 가 앞)** · 탐침 본문 한 줄 |
| 4장 · 4.1 | Q128 대기 | 해결됨 줄 + 새 줄 (동적 → Q104, 정적 → Q130, 짐작 틀림·이름 적는 법) |

- 태그 변화: 탐침 칸의 **값** 은 원래 `[관찰]` (사진) 이었다. 이번에 **누가 채우는지·언제 읽는지** 가 `[정적]` 으로 붙었다. 실행 순서 자체는 아직 동적 확인 전.

### 짐 지도 (`baggage.md`) 에서 바뀐 곳

- 새 **7.6.7**: KERNELBASE `.data` 칸 10 + KERNEL32 `.data` 칸 2 + 힙 블록 1 (명령줄 ANSI 9B). 체크리스트 밖 (다른 DLL 전역) 이라 2장 칸 수엔 안 센다.
- 3.8 `.data` 에 4줄: 모르던 ntdll 전역 3 (탐침 칸 셋 `LdrpManifestProberRoutine`·`LdrpCreateActCtxLanguageW`·`LdrpReleaseActCtxW`) 이 아는 칸이 됐다. 넷째 줄은 attach 실패 때 셋을 0 으로 되돌리는 조건부 줄 (test.exe 안 탐).
- 그래서 ntdll 전역 "쓴 자리 모르는 것" 328 → **325** (7.3 · 2장 숫자).
- 새 **7.26**: 이번에 넘긴 질문.

### 큰 그림과 이어지는 곳

- **PE_emulator 가 은퇴 뒤 멈춘 벽 (①) 이 풀렸다.** `BaseHeap` 에 `PEB.ProcessHeap` 을 넣으면 `GlobalAlloc` 이 창고에서 블록을 받는다.
- ② 덕분에 `GetCommandLineA` 를 흉내 낼 때 "성공이면 9B, 실패면 NULL" 두 꼴만 맞추면 된다.
- ③ 은 "PE_emulator 의 0 이 맞다" 가 아니라 **"결과는 같지만 한 단계가 빠진다"** 로 정리됐다. 매니페스트가 있는 DLL 을 올리게 되면 그때는 길이 갈린다.
- 다음 바퀴는 **Q129** — 같은 KERNELBASE 시작 함수의 나머지 (`CsrClientConnectToServer`·`ConsoleInitialize`·`BaseNlsDllInitialize` 등).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 실행 순서: kernel32·kernelbase (칸 0) → `LdrSetDllManifestProber` → exe·dll1·dll2 (칸 값) 가 진짜 이 순서인가 | 코드 길만 봤다. 사진은 둘 다 같은 구간에 있어 못 가른다 | 동적 1 → **Q104 VM 단계** (`bp ntdll!LdrSetDllManifestProber`, `bp ntdll!LdrpFindDllActivationContext`) |
| 탐침이 exe·dll1·dll2 에 주는 **상태값** | `LdrpResGetResourceDirectory` 안쪽을 안 읽었다 | 동적 2 → **Q104** (`+0x83` 에서 `r eax`), 또는 정적 → **Q130 (a)** (닫으면 동적 2 는 필요 없음) |
| kernelbase·kernel32 `.data` 칸의 실제 값 (`BaseHeap` 등) | run44 사진이 ntdll `.data` 만 읽었다 | 동적 3 → **Q104** |
| 명령줄 ANSI 사본의 실제 반환·힙 블록 주소·내용 | 실행 때 값. `RtlAllocateHeap` 안 syscall 도 모름 | 동적 4 → **Q104** |
| `KernelBaseGlobalData` 이웃 칸의 값 출처 | 이번 질문 밖이라 쓰는 자리만 표로 넘겼다 (짐 지도 7.6.7) | 정적 → **Q130 (b)** (NLS 쪽 `+0x58 ~` 는 Q129 와 겹침) |
| CRT 의 `LoadLibraryExW ×3` 이 새 DLL 을 올릴 때도 탐침을 부르나 | 같은 `LdrpMapAndSnapDependency` 길이라 부를 것 `[추측]` | 정적 → **Q130 (c)** (Q31 과 이어짐), 동적 1 멈춤점이 같이 답함 |

- ntdll·kernelbase 멈춤점은 **test.exe 일 때만** 걸리게 조건을 붙인다 (run 문서).

---

## 5. 더 읽기

- run 문서: [`run67-kb-heap-init.md`](run67-kb-heap-init.md) — "PE_emulator 쪽에서 쓸 것" (한 줄 결론 셋 · 작은 표), 호출 지점 표, 호출 관계 트리, 사진과 맞춰 본 것, 정정
- 결과서: [`_workspace/sast/62_kb-heap-init_result.md`](../../_workspace/sast/62_kb-heap-init_result.md) — `KernelBaseGlobalData` 이웃 칸 표, `RtlUnicodeStringToAnsiString` 갈래 전부, "없음 취급" 집합
- 요청서: [`_workspace/sast/62_kb-heap-init.md`](../../_workspace/sast/62_kb-heap-init.md) — 막힌 셋 원문, 4(b) 짐작
- flow.md: [2.5 "시작 함수 안에서 놓는 칸"](flow.md) · [2.6 `LdrpFindDllActivationContext` 절](flow.md) · 그림 2 · 4장 run67 줄 · 4.1
- 짐 지도: [`baggage.md`](baggage.md) 3.8 · **7.6.7** · **7.26** · 7.3
- 앞선 해설: [run25](../02_sprint/run25-dll-entry-해설.md) (시작 함수 이름·대표 호출) · [run46](run46-process-heap-해설.md) (창고 짓기, `LdrpHeap` = 같은 창고) · [run57](run57-nls-apiset-해설.md) (코드 페이지·매니페스트 없는 exe) · [run22](../02_sprint/run22-map-snap-dep-해설.md) (`LdrpMapAndSnapDependency`) · [run44](run44-baggage-snap-해설.md) (사진)

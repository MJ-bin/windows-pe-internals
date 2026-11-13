# run68 dll-init-rest — 해설

> 이 글은 [run68 문서](run68-dll-init-rest.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.
>
> **고쳐 읽기 (run83)**: "한 줄로 먼저" 표 "그 밖" 줄의 "(syscall 3)", 2.1 표 순서 12 의 syscall "3", 그 아래 합의 "콘솔 3" · "15 ~ 16번", 2.8 의 "syscall **3번**" 은 run83 에서 정정됐다 — 부모 콘솔에 붙기 갈래의 syscall 은 **4번** (넷째 = 콘솔 언어 ID 를 묻는 `NtDeviceIoControlFile`), kernelbase 시작 함수 syscall 합은 **16 ~ 17번** (+ NLS 모름) 이다. → [run68 문서](run68-dll-init-rest.md) 맨 위 "정정 (run83, sast/80)" · [run83 문서](run83-console-kernelbase.md) "정정" 절 · [run83 해설 2.6](run83-console-kernelbase-해설.md#26--run68-정정--넷째-syscall-어느-나라-말-써요).
>
> 같은 run83 에서 하나 더: 2.8 의 "새 콘솔 만들기 (`ConsoleAllocate`, conhost 띄우기)" 는 — kernelbase 는 conhost 를 **직접 띄우지 않고**, 콘솔 상담실 (condrv) 에 IOCTL `0x500037` 주문서 (입력 = conhost 용 개업 서류) 를 넣어 **맡긴다**. `ConsoleAllocate` 아래엔 프로세스를 만드는 syscall 이 없다. → [run83 해설 2.7](run83-console-kernelbase-해설.md) "여기서 꼭 짚을 것".

- 대기열: **Q129** — PE_emulator 요청 16 의 나머지 본문 (`_workspace/plan.md`, 이번에 **끝**)
  - 이것으로 **PE_emulator 요청 16 이 전부 끝났다** (급한 셋은 run67 = Q128).
  - 다음 항목: **Q104 VM 단계** (사용자 결정 2026-11-06: Q128 → Q129 → Q104 VM 단계).
  - 남은 질문이 간 곳: csrss 연결 결과 → **Q84** (동적), 시작 함수 칸 값 → **Q104 VM 단계** (동적), 작은 정적 일곱 → 새 **Q131** (맨 뒤).
- 근거:
  - run 문서 [`run68-dll-init-rest.md`](run68-dll-init-rest.md) (가장 중요)
  - 요청서 [`_workspace/sast/63_dll-init-rest.md`](../../_workspace/sast/63_dll-init-rest.md)
  - 결과서 [`_workspace/sast/63_dll-init-rest_result.md`](../../_workspace/sast/63_dll-init-rest_result.md) (판정: **분석성공**)
  - flow.md 변경: 기준 · 그림 2 (KERNELBASE 아래 상자 넷, KERNEL32 아래 상자 둘) · 2.5 (여러 줄 + 새 절 "시작 함수 안 나머지") · 4장 · 4.1 · 5장
  - 짐 지도 변경: 3.1 · 3.2 · 3.5 · 3.8 · 새 7.6.8 · 새 7.27 · 7.2 / 7.3 숫자 (3절에서 짧게)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다.
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 를 **다시 읽은 것** 이다. 새 실행이 아니다.
  - 그 사진엔 여전히 **kernelbase·kernel32 의 `.data` 가 없다**. 그래서 두 DLL 수첩 칸의 실제 값은 못 봤다. 본 건 "누가, 무엇으로, 어디에 쓰나" 까지다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 전원 켜기 = DLL 시작 함수 (DllMain) 부르기, 이유 1 = 프로세스에 처음 붙음 → [run25 해설](../02_sprint/run25-dll-entry-해설.md)
- csrss = 관리사무소와 따로 있는 **인사팀 사무실** → [run14 해설 2.6](../02_sprint/run14-create-process-internal-해설.md) · [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) (서버 DLL = 사무실 안 **창구들**, 1번 창구 = basesrv)
- 섹션 = 관리사무소 창고의 **원본 물건**, 뷰 = 그걸 가게 땅에 **펼쳐 놓은 한 판** → [run38 해설](../02_sprint/run38-map-module-해설.md) · [run57 해설](run57-nls-apiset-해설.md)
- NLS 표 = **글자 사전**, 코드 페이지 949 = 몇 번 사전 → [run57 해설](run57-nls-apiset-해설.md)
- ETW = **방송국**, 공급자 = **채널**, RegHandle = 채널 **번호표**, "Once" = 첫 등록 때 한 번만 하는 준비 → [run62 해설 2.6 · 2.7](run62-tp-etw-init-해설.md)
- 보안 쿠키 = 택배 상자 **봉인 스티커** 무늬, 쿠키가 둘 (모듈 것 / 프로세스 것) → [run59 해설 2.3](run59-init-leftovers-해설.md)
- 콘솔 호스트 (conhost) = 가게 대신 **쇼윈도와 출입문을 관리해 주는 옆집** → [run51 해설 2.9](run51-eprocess-later-해설.md)
- 사진 P1a·P1b·P2 → [run44 해설 2.8](run44-baggage-snap-해설.md)

이번에 처음 나오는 말 — **크리티컬 섹션**, **콘솔 연결** (그리고 짧게 **ALPC 포트**, **멀티세션 SKU**, **예외 필터**) — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, PEB = 가게 **안내판**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**.
- ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**.
- KERNELBASE·KERNEL32 = 가게에 들인 **건물** 둘. 시작 함수 = 건물 **전원 켜기**. 그 DLL 의 전역 = 건물마다 가진 **수첩** (run67).
- 프로세스 힙 = **창고** (run46).
- 이번에 새로 붙는 그림:
  - csrss 연결 = 가게가 **이웃 인사팀 사무실에 처음 전화를 거는 것**. 전화를 걸면서 "같이 쓸 메모장" 을 건네고, 사무실이 동네에 붙여 둔 **게시판** 을 우리 땅에 한 판 펼친다 (2.4).

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| ① PE_emulator 의 `_SYSTEM_BASIC_INFORMATION` 대답이 모자라나 | **모자란다.** 지금 `+0x18`·`+0x28`·`+0x38` 만 채운다. kernelbase 는 `+0x8` 과 `+0x20` 도 읽는다 → **`+0x8 = 0x1000`, `+0x20 = 0x10000` 을 더 채워야** 한다. 둘 다 커널이 상수로 쓰는 칸이다 `[정적]` (2.3) |
| ② csrss 연결은 무엇을 하나 | `CsrClientConnectToServer → CsrpConnectToServer` 가 **syscall 6번** 으로 ntdll `Csr*` 전역 **9개** 와 PEB `+0x88`·`+0x98`·`+0x380` 을 채운다. **값은 csrss 가 준다** → 동적 (Q84) `[정적]` (2.4) |
| ③ test.exe 에서 안 타는 것은 | 셋: `DisableRegCachingForConstrainedImpersonation` 본문 · `RtlSetProtectedPolicy` · `GetRegistryExtensionFlags`. 셋 다 까닭이 코드 줄과 사진 값으로 나온다 `[정적]` (2.11) |
| 그 밖 | 콘솔은 "**부모 콘솔에 붙기**" 갈래 (syscall 3), `+0x508` 락은 디버그 정보를 **안 만든다**, NLS 는 run57 과 같은 사전을 **자기 뷰로 또** 연다, kernel32 는 자기 모듈 항목에 `DontCallForThreads` 비트를 켠다 `[정적]` |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

kernel32 를 들이고 **전원을 켜는 안쪽** 이다. run67 과 같은 자리고, 이번엔 거기서 **안 읽은 나머지 전부** 다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ … 창고 짓기 (run46), 명부에 ntdll·exe
 │         ├─ +0x19a2  kernel32 들이기 + 전원 켜기
 │         │     ├─ KERNELBASE 시작 함수
 │         │     │     ├─ 힙 칸·명령줄 사본 (run67)
 │         │     │     └─ SysInfo · csrss 연결 · 예외 필터 · NLS · 락 · 콘솔 · ETW …   ← 이번
 │         │     └─ KERNEL32 시작 함수
 │         │           ├─ 탐침 칸 (run67)
 │         │           └─ 정책 · CSDVersion · 스레드 알림 끄기 · SysInfo …              ← 이번
 │         ├─ +0x1aa9  exe 의 import (dll1·dll2) 들이기
 │         └─ … dll 전원 켜기 → TLS 콜백
 └─ … → NtContinue → exe 진입점 → main
```

### 왜 이 질문이 지금 왔나

- PE_emulator 는 kernelbase·kernel32 시작 함수를 **진짜로 돌리지 않고 흉내 낸다** (칸에 직접 쓰기 + 진짜 ntdll 함수 덫).
- run67 이 가장 급한 셋 (힙 칸, 명령줄, 탐침 칸) 을 닫았다.
- 이번은 나머지 약 18 함수다. 함수마다 **인자 · 쓰는 칸 · 값 출처 · 하위 호출 · syscall · 반환** 이 필요했다 (요청서).

### 무엇이 궁금했나

요청서의 12문항을 묶으면 셋이다.

1. **KERNELBASE 시작 함수 안을 시간 순서로**: ETW 등록, `DisableRegCaching…`, `SysInfo`, csrss 연결, 예외 필터, NLS, 락, 콘솔 — 각각 무엇을 놓나.
2. **`_SYSTEM_BASIC_INFORMATION` 칸 모양**: 커널이 어느 칸에 무엇을 쓰고, 두 DLL 은 어느 칸을 실제로 읽나.
3. **KERNEL32 시작 함수 안**: `RtlSetProtectedPolicy`·`LdrDisableThreadCalloutsForDll`·`GetRegistryExtensionFlags` 등을 test.exe 가 타나.

### 바퀴 전에 알던 것

- `[정적]` (run25) 두 시작 함수의 **대표 호출 이름만**. 예: `+0x2d9 CsrClientConnectToServer`, `+0x4e6 BaseNlsDllInitialize`, `+0x526 ConsoleInitialize`.
- `[정적]` (run51) `ConsoleInitialize` 아래 `ConsoleCommitState` 가 `ConsoleHandle` 을 덮고 `OwnerProcessId` 를 바꾼다.
- `[정적]` (run67) 메모리 관리 · `RtlCreateTagHeap` → 0 · 명령줄 ANSI 사본 · 탐침 칸.
- `[관찰]` (run44 사진) P1a ~ P1b 사이에 `Csr*` 8개 · PEB CSR 칸 4개 · `ConsoleHandle` `4 → 0x60` 이 바뀐다. 누가 언제 썼는지는 사진만으로는 몰랐다 (그 구간엔 dll1·dll2 올리기도 섞여 있다).
- PE_emulator nt 대답 `FillSystemBasicInformation` 은 `+0x18`·`+0x28`·`+0x38` 세 칸만 채운다 (요청서).

---

## 2. 찾은 것

### 2.1 한눈에 — KERNELBASE 전원 켜기, 시간 순서로 한 줄씩

`_KernelBaseBaseDllInitialize` 의 이유 1 갈래를 **실행 순서** 로 늘어놓았다. 라벨은 ret 자리.
"무엇을 놓나" 칸이 이 절의 핵심이다. 모두 `[정적]` 이다.

먼저 바깥 껍데기 (래퍼 `KernelBaseBaseDllInitialize`) 가 두 가지를 한다:

- `+0x5c` `RtlInitializeCriticalSectionAndSpinCount(&_CrtLock_Exit, 0xfa0)` — attach 일 때만. 락 하나 (2.7).
- `+0x2d` `_DllMainCRTStartupForGS2` — 모듈 자기 봉인 스티커 (2.10).
- 그다음 `+0x44` 꼬리 점프로 몸통 `_KernelBaseBaseDllInitialize` 에 들어간다.

몸통 안:

| 순서 | 자리 (ret) | 하는 일 | 무엇을 놓나 | syscall |
|---|---|---|---|---|
| 1 | `+0x1b2` | 메모리 관리 (run67) | `BaseHeap` 등 | (run67) |
| 2 | `+0x1d6` | `RtlCreateTagHeap` → 0 (run67) | `KernelBaseGlobalData+0` = 0 | — |
| 3 | `+0x1ee` · `+0x22a` | 객체 디렉터리 이름 만들기 | 지역 버퍼에 `\Sessions\1\Windows` | — |
| 4 | `+0x241` | `NtQuerySystemInformation(0)` | kernelbase **`SysInfo`** `0x40`B (2.3) | 1 |
| 5 | `+0x25a` · `+0x278` | `RtlRandomEx` ×2 (본문 run61) | `BasepFiberCookie` | — |
| 6 | `+0x2d9` | **csrss 연결** | ntdll `Csr*` 9개 · PEB 세 칸 (2.4) | 6 |
| 7 | `+0x2fb ~ +0x469` | 연결 결과 옮겨 적기 | `BaseStaticServerData` · `KernelBaseGlobalData+0x4 ~ +0x27` · `BaseRCNumber` | — |
| 8 | `+0x497` | 명령줄 ANSI 사본 (run67) | `BaseAnsiCommandLine` | — |
| 9 | `+0x4b2` · `+0x4c0` | 예외 필터 (2.5) | ntdll `RtlpUnhandledExceptionFilter` · kernelbase `BasepCurrentTopLevelFilter` | — |
| 10 | `+0x4e6` | `BaseNlsDllInitialize` (2.6) | `gpServerNlsUserInfo` · NLS 전역 · ntdll `gBaseAddress`·`gSystemLocale` | 3~4 (+ 모름) |
| 11 | `+0x508` | 락 하나 (2.7) | `KernelBaseGlobalData+0xb0` | — |
| 12 | `+0x526` | `ConsoleInitialize` (2.8) | 콘솔 전역 · 매개변수 `ConsoleHandle` | 3 |
| 13 | `+0x165` | ETW 공급자 등록 (2.9) | 공급자 칸 `0x3a9020` | 2 |
| 14 | `+0x189` | `g_Win64Registry ← 1` → `DisableRegCaching…` (본문 안 탐) → `g_Initialized` +1 | `g_Win64Registry`·`g_Initialized` | — |
| 반환 | | TRUE | | |

- **순서 13 은 코드 자리가 앞인데 실행은 뒤다.** ETW 등록 (`+0x160/+0x165`) 은 몸통 앞쪽에 쓰여 있지만, 콘솔이 성공하면 `+0x52e JMP` 로 그리로 **되돌아 뛰어** 간다. 그래서 콘솔 **뒤** 에 돈다.
- **진행 표시판** `BasepInitializationStage` (kernelbase `0x3aeea0`, word): 단계가 끝날 때마다 값을 올린다.
  - `0x64` (`+0x28b`) → `0xc8` (`+0x2eb`) → `0x190` (`+0x4da`) → `0x1f4` (`+0x4fa`) → `0x258` (`+0x51a`) → `0x2bc` (`+0x16a`).
  - 비유: 전원 켜기 체크리스트에 **몇 번 칸까지 끝냈나** 를 적는 표시판.
- **실패 길**: 4·6·10·11 이 음수/0 이면 `+0x5ab` 쪽에서 AL = 0, 12 가 0 이면 `+0x528 JZ` — 모두 DllMain FALSE.

syscall 을 덩어리별로 더하면 (run 문서·결과서 표):

- `SysInfo` 1 + csrss 6 + NLS 3~4 + 콘솔 3 + ETW 2. 여기에 NLS 의 `MakeLocHashNode(0x7f)` 아래가 **모름** 으로 더 붙는다.
- kernel32 시작 함수는 1번 (자기 `SysInfo`). run67 의 `NtQuerySystemInformation(0x32)` 1번은 따로다.
- 주의: 결과서는 kernelbase 합을 "16 ~ 18번" 이라고 적었다. 위 덩어리 (1 + 6 + 3~4 + 3 + 2) 를 더하면 **15 ~ 16번** 이 맞다 — run 문서는 15 ~ 16 으로 고쳤다 (오케스트레이터 확인, 결과서는 그대로).

### 2.2 한 줄씩 읽는 법

run 문서 "호출 지점" 표에서 한 줄을 골라 끝까지 읽어 본다.

**`KERNELBASE!_KernelBaseBaseDllInitialize | +0x2d2 | +0x2d9 | ntdll!CsrClientConnectToServer | [PEB+0x20].Flags 양수 (secure process 아님 — +0x2a7)`**

- **누가**: KERNELBASE 시작 함수의 몸통 `_KernelBaseBaseDllInitialize`.
- **어디서**: CALL 명령이 `+0x2d2`, 다음 줄이 `+0x2d9`. 콜스택엔 `_KernelBaseBaseDllInitialize+0x2d9` 로 보일 자리다.
  - 요청서는 "`+0x2d4/+0x2d9` 쯤" 이라고 짐작했는데, call 은 **`+0x2d2`** 다 (run 문서 "정정").
- **누구를**: ntdll 의 `CsrClientConnectToServer` (rva `0xdb7a0`, 489B).
- **조건**: 바로 앞 `+0x2a7` 이 `[PEB+0x20].Flags` 의 부호를 본다. 음수 (= secure process) 면 다른 함수로 간다. test.exe 는 양수라 이 줄을 탄다 `[정적]`.
- **인자**: `\Sessions\1\Windows` · 서버 번호 1 (BASESRV) · 연결 정보 8B (= `kernelbase!CtrlRoutine` 주소) · 크기 8 · `CalledFromServer` 출력칸 `[정적]`.
  - `1` 은 세션 번호다. `[관찰]` run44 `PEB.SessionId = 1`.
- **무엇을 위해**: 이웃 인사팀 사무실 (csrss) 의 **1번 창구 (basesrv)** 와 첫 연결을 맺으려고 (2.4).

---

### 2.3 ★ `_SYSTEM_BASIC_INFORMATION` — PE_emulator 대답에 두 칸이 더 필요하다

#### 무엇인가

- KERNELBASE 가 `+0x241` 에서 커널에게 "**이 컴퓨터 기본 정보 주세요**" (클래스 0) 하고 묻는다.
  - 인자: `NtQuerySystemInformation(0, &SysInfo, 0x40, NULL)` (`+0x22d LEA RDX` · `+0x234 XOR ECX,ECX` · `+0x236 LEA R8D,[R9+0x40]`) `[정적]`.
  - 답은 kernelbase 수첩의 **`SysInfo`** (rva `0x3aee60`, `0x40`B) 에 그대로 담긴다. 이 칸을 채우는 줄은 이것 **하나뿐** 이다 `[정적]`.
- KERNEL32 도 자기 수첩에 **한 벌 더** 받는다: `_Kernel32LegacyLib_BaseDllInitialize +0x9c/+0xa3` → kernel32 `SysInfo` (rva `0xc2260`, `0x40`B) `[정적]`.
- 비유: 두 건물이 각자 수첩에 관리사무소에서 받은 **"이 동네 기본 규격표"** 를 한 장씩 베껴 둔다. 나중에 일할 때마다 그 표를 펴 본다.

#### 커널 쪽 — 길이가 꼭 `0x40` 이어야 한다

- 커널 `nt!ExpGetSystemBasicInformation` (rva `0x457484`, 264B) 이 사용자 버퍼에 바로 쓴다 `[정적]`.
- 길이가 **정확히 `0x40`** 이 아니면 `STATUS_INFO_LENGTH_MISMATCH` (`0xC0000004`) — `ExpQuerySystemInformation +0x72b CMP` · `+0x72e JNZ` `[정적]`.
- 칸 이름은 PDB 에 이 타입이 없어서 공개 문서 이름이다 `[추측-이름]`. 오프셋·쓰는 줄·값은 `[정적]`.

#### 칸 표 (두 DLL 이 읽는 칸만 굵게)

| 오프셋 | 뜻 `[추측-이름]` | 값 | 두 DLL 이 읽나 | PE_emulator 지금 |
|---|---|---|---|---|
| `+0x00` | Reserved | 상수 0 | 아니오 | 0 (맞음) |
| `+0x04` | TimerResolution | 커널 전역 `KeMaximumIncrement` — 부팅 때 (동적) | 아니오 | 0 |
| **`+0x08`** | **PageSize** | **상수 `0x1000`** | **예** (kernelbase 20여 곳 · kernel32) | **0 — 고쳐야 함** |
| `+0x0c` ~ `+0x14` | 물리 페이지 수·가장 낮은/높은 번호 | 파티션 값 — 동적 | 아니오 | 0 |
| **`+0x18`** | AllocationGranularity | 상수 `0x10000` | 예 | 채움 |
| **`+0x20`** | **MinimumUserModeAddress** | **상수 `0x10000`** | **예** (`GlobalMemoryStatusEx +0x155`) | **0 — 고쳐야 함** |
| **`+0x28`** | MaximumUserModeAddress | 상수 `0x7ffffffeffff` | 예 (`GlobalMemoryStatusEx +0x149`) | 채움 |
| `+0x30` | ActiveProcessorsAffinityMask | `KeActiveProcessors` — 동적. CPU 2개라 `3` 일 것 `[추측]` | 아니오 | 0 |
| `+0x38` | NumberOfProcessors (1B) | 위 마스크의 `POPCNT` — 동적. 2 일 것 `[추측]` | 아니오 | 채움 |

- 정리: 칸 11개 = **상수 5** (`+0x0`·`+0x8`·`+0x18`·`+0x20`·`+0x28`) + **부팅 때 정해지는 6** `[정적]`.

#### 왜 `+0x8` 이 급한가 — 단계별로

1. PE_emulator 는 `+0x8` 을 0 으로 둔다.
2. 그런데 kernelbase 에서 `SysInfo+0x8` 을 읽는 함수가 20여 개다. 예: **`HeapCreate +0x6`**, `CreateFiberEx`, `GlobalMemoryStatusEx`, `WriteProcessMemory`, `SetThreadStackGuarantee` … (`_refs` 29줄) `[정적]`. kernel32 도 `IsBadReadPtr`·`IsBadWritePtr`·`GlobalMemoryStatus` 에서 `+0x8` 을 읽는다.
3. 이름으로 보면 이 칸은 "페이지 크기" 다 `[추측-이름]`. 크기 계산에 쓰는 값이 0 이면 그 함수들이 진짜와 다른 결과를 낼 것이다 `[추측]`.
4. 그래서 → 최소 **`+0x8 = 0x1000`**, 그리고 `GlobalMemoryStatusEx` 를 위해 **`+0x20 = 0x10000`** 을 더 채우자는 게 run 문서의 결론이다.
- 둘 다 커널이 **상수** 로 쓰는 칸이라 쉽다 (`+0x3f`·`+0x50` 줄) `[정적]`.
- 결과서 의견: `+0xc`·`+0x10`·`+0x14`·`+0x30` 은 두 DLL 이 안 읽지만, 진짜와 같게 채워 두는 게 정직하다. `+0x38` 도 두 DLL 은 안 읽는다.

비유: 동네 규격표에 "**벽돌 한 장 크기**" 칸이 비어 있으면, 그 표를 보고 벽을 쌓는 일꾼 20명이 다 엉뚱하게 계산한다. 숫자 하나만 적어 주면 된다.

---

### 2.4 ★ csrss 연결 — 이웃 인사팀 사무실에 처음 거는 전화

#### 새 말 하나 — ALPC 포트

- **ALPC 포트** = 프로세스끼리 메시지를 주고받는 **전화선 끝** `[추측]` (일반적으로 알려진 설명).
  - csrss 는 `\Sessions\1\Windows\ApiPort` 라는 이름의 포트를 열어 두고 기다린다. 가게는 그 번호로 전화를 건다.
- run 문서: 연결에 쓰는 syscall 은 **`NtConnectPort`** 다. ALPC 포트로 가는 **옛 이름 API** 이고, `NtSecureConnectPort`·`NtAlpcConnectPort` 가 아니다 `[정적]`. (요청서 짐작이 틀린 곳 — 2.14)

#### 왜 csrss 에 전화를 거나

- 앞 해설에서 csrss 는 관리사무소와 따로 있는 **인사팀 사무실** 이었다 (run14). 부모 쪽에선 "새 가게 생겼어요" 를 그 사무실에 알렸다 (run34).
- 이번엔 **새 가게 자신** 이 처음으로 그 사무실에 전화를 건다. 상대는 1번 창구 (BASESRV) 다.
- 전화를 걸 때 "**제 비상 연락처는 이거예요**" 라는 쪽지 (8바이트 = `kernelbase!CtrlRoutine` 주소) 를 같이 건넨다 `[정적]`.

#### 길 고르기 — test.exe 는 "손님 (클라이언트)" 길

`CsrClientConnectToServer` 안에서 (`[정적]`):

1. `+0xe7` `CsrInitOnceDone ← 1`, `+0xf2` `CsrHeap ← PEB.ProcessHeap` (창고 주소 베끼기).
2. `+0x7c` `RtlImageNtHeaderEx` 로 exe 헤더를 본다. `Subsystem` 이 1 (NATIVE) 이 아니면 `+0x8f` `CsrClientProcess ← 1`.
   - test.exe 는 `Subsystem = 3` → **손님 길**.
   - 반대쪽 "서버 길" (`+0x137` 이하) 은 csrss **자신** 만 탄다.
3. `+0xa4` `CsrPortHandle` 이 0 (아직 전화 안 걸었음) 이면 `+0x118` **`CsrpConnectToServer`** 로.

#### 전화 거는 순서 — syscall 6번

`CsrpConnectToServer` (ntdll rva `0xdbfe4`) 안, 처음 연결 (`[정적]`, 라벨 ret):

| 자리 | 하는 일 | 비유 |
|---|---|---|
| `+0x11e` | `RtlAllocateHeap(CsrHeap, …)` → 이름 버퍼 `0x42`B (`CsrPortName.Buffer`) | 전화번호 적을 메모지 한 장 |
| `+0x1bb` | **`NtOpenSection`** `\Sessions\1\Windows\SharedSection` (읽기 권한 4) | 사무실이 동네에 붙여 둔 **공용 게시판** 찾기 |
| `+0x242` | **`NtCreateSection`** 이름 없는 `0x10000`B (`SEC_COMMIT`, 읽기·쓰기) | 사무실과 **같이 쓸 메모장** 하나 만들기 |
| `+0x2e4` | **`NtConnectPort`** (`&CsrPortHandle`, `\Sessions\1\Windows\ApiPort`, 연결 블록 `0x30`B) | **전화 걸기** — 메모장도 같이 넘김. 사무실이 답을 블록에 적어 줌 |
| `+0x2f0` | **`NtClose`** (만든 섹션) | 메모장 손잡이 하나 내려놓기 |
| `+0x33d` | **`NtMapViewOfSection`** (SharedSection, **읽기 전용**) | 공용 게시판을 우리 땅에 한 판 펼치기 |
| `+0x349` | **`NtClose`** (SharedSection) | 게시판 손잡이 내려놓기 |
| `+0x3f8` | `RtlpCreateHeap(0x8000, 포트 뷰, 0x10000, …)` | 같이 쓰는 메모장 **안에** 작은 개인 창고 하나 |

- 이 6번이 끝나면 쓰는 칸 (`[정적]`):
  - PEB `+0x380` `CsrServerReadOnlySharedMemoryBase` (`+0x36d`) · `+0x88` `ReadOnlySharedMemoryBase` (`+0x379`) · `+0x98` `ReadOnlyStaticServerData` (`+0x38d`).
  - ntdll 칠판: `CsrProcessId` (`+0x39c`) · `CsrPortMemoryRemoteDelta` (`+0x3ab`) · `CsrPortHeap` (`+0x3f8`) · `CsrPortBaseTag ← 0` (`+0x404`).
  - `CsrPortHandle` 은 **커널이** 쓴다 (`NtConnectPort` 의 출력 칸).
- 합쳐서 ntdll `Csr*` 전역 **9개**: `CsrInitOnceDone` · `CsrHeap` · `CsrClientProcess` · `CsrPortName` · `CsrPortHandle` · `CsrProcessId` · `CsrPortMemoryRemoteDelta` · `CsrPortHeap` · `CsrPortBaseTag`.
  - run44 사진의 "`Csr*` 8개" 와 이어진다. 짐 지도는 9개 중 `CsrPortBaseTag` (0 을 씀) 를 `CsrServerApiRoutine` 과 함께 "**끝까지 0**" 으로 세고, 나머지 8개를 `P1a~P1b` 로 센다.
  - `CsrServerApiRoutine` 은 이 길에서 **안 쓴다** (서버 길에서만) `[정적]`.

#### 주소 옮겨 적기 — "사무실 지도 번지" 를 "우리 지도 번지" 로

PEB `+0x98` 과 kernelbase `BaseStaticServerData` 는 같은 꼴의 계산이다 (`[정적]`):

- PEB `+0x98` = 사무실이 준 포인터 − 사무실 쪽 게시판 주소 + 우리 쪽 게시판 주소.
- kernelbase `BaseStaticServerData` (`0x3aedb8`) = `[PEB+0x98]+8` 의 값 − `PEB+0x380` + `PEB+0x88` (`+0x2fb ~ +0x32b`).
- 왜 이렇게 하나 — 단계별로:
  1. 공용 게시판은 **같은 물건** 을 사무실도 펼치고 우리도 펼친 것이다. 펼친 자리 (주소) 만 다르다.
  2. 사무실이 적어 준 위치는 **사무실 땅 기준 번지** 다 (`PEB+0x380` = 사무실 쪽 게시판 시작).
  3. 그래서 "게시판 시작에서 얼마나 떨어져 있나" 만 남기고 (빼기), 우리 게시판 시작 (`PEB+0x88`) 에 붙인다 (더하기).
- `[관찰]` run44 P1b 값이 이 식과 맞는다: `ReadOnlySharedMemoryBase = 0x7ff4ecf70000`, `ReadOnlyStaticServerData = 0x7ff4ecf70750`, `CsrServerReadOnlySharedMemoryBase = 0x7ff5575f0000`. `0x750` 이 게시판 안 위치다.
- 이어서 kernelbase 는 `BaseRCNumber` (`+0x342`) · `KernelBaseGlobalData+0x4` (`CalledFromServer ≠ 0` = 0, `+0x337`) · `+0x8`·`+0x18` (16B씩, 문자열 머리를 옮기고 `Buffer` 를 우리 뷰로 고침) 도 쓴다 `[정적]`.
- KERNEL32 도 같은 식으로 자기 수첩에 한 번 더 (`_BaseDllInitialize +0x189`, `_Kernel32LegacyLib_BaseDllInitialize`) `[정적]`.

#### 왜 값은 동적인가

- 핸들 숫자, 게시판 주소, csrss PID 는 **전화를 받은 사무실이 알려 주는 값** 이다. 코드에는 "어디에 적나" 만 있고 "무엇을 적나" 는 없다.
- 그래서 꼴은 `[정적]` 으로 닫혔고, 값은 **동적 (Q84)** 이다.
- 비유: 전화를 걸면 받아 적을 칸은 정해져 있다 (이름·내선 번호·게시판 위치). 그런데 상대가 **무슨 번호를 불러 줄지** 는 실제로 걸어 봐야 안다.

#### 덤 — 둘째 전화 (`CsrpClientConnectToServer`) 는 정상이면 안 건다

- `+0xb5/+0xba` `CsrpClientConnectToServer` 는 사무실 응답 상태가 **음수일 때만** 탄다 `[정적]`.
- 사무실 쪽 1번 창구 `basesrv!BaseClientConnectRoutine` 은 쪽지 크기가 8 이면 0 을 돌려준다 (`+0x13 XOR EAX,EAX`) `[정적]`. kernelbase 는 8 을 넘긴다.
- 그래서 → 안 탄다. 다만 이 결론은 **csrss 쪽 상수에 기댄다**. 그리고 사무실 안에서 그 루틴을 부르는 `CsrpHandleInlineUserConnect` 쪽은 `.c` 기준이라 `.asm` 대조를 안 했다 → 동적 확인 Q84, `.asm` 대조 Q131 (d).

---

### 2.5 예외 필터 — 마지막 담당자 이름을 암호로 적어 둔다

- **예외 필터** = 프로그램에서 아무도 처리 못 한 예외가 나면 **마지막에 불리는 함수** `[추측]` (일반적으로 알려진 설명).
- `+0x4ab/+0x4b2` `RtlSetUnhandledExceptionFilter(kernelbase!UnhandledExceptionFilter)` `[정적]`.
  - 안에서 `+0x9` `RtlEncodePointer` 로 암호화한 값을 ntdll 칠판 **`RtlpUnhandledExceptionFilter`** 에 적는다. 21B 짜리 함수.
- `+0x4b9/+0x4c0` `RtlEncodePointer(0)` → `+0x4ce` kernelbase **`BasepCurrentTopLevelFilter`** ← 암호화한 NULL `[정적]`.
- syscall 없음. 암호 열쇠 (`CookieValue`) 는 run59 에서 `LdrpInitializeProcess +0x12f` 가 **이미** 받아 뒀기 때문이다 `[정적]`.
- 값 자체는 쿠키와 kernelbase 베이스 주소에 달려 **동적** 이다 (식만 정적).

비유: 관리실 칠판에 "사고 나면 부를 마지막 담당 = ○○" 을 적는데, 그대로 적지 않고 **가게 비밀번호로 섞어서** 적는다.

---

### 2.6 NLS — 관리실이 펼친 글자 사전을, KERNELBASE 도 자기 책상에 한 판 더

- `+0x4e1/+0x4e6` `BaseNlsDllInitialize(…, 1, BaseStaticServerData)` `[정적]`.
  - `+0x10` `gpServerNlsUserInfo ← BaseStaticServerData + 0x178` — csrss 가 준 "NLS 사용자 정보" 칸의 **우리 쪽 주소**.
  - `+0x1c` `NlsProcessInitialize` 로 간다.
- `NlsProcessInitialize` 안 (`[정적]`):
  1. `AllocTables` — 힙 블록 2개.
  2. `SetupMainNlsFiles` — `NtGetNlsSectionPtr` 종류 `0xe` (대문자 표) 1번. 조건 `PEB.UseCaseMapping ≠ 0`, `[관찰]` `0x6a5`.
     - 그 안에서 ntdll `RtlGetLocaleFileMappingAddress` → `NtInitializeNlsFiles` 1번 → ntdll `gSystemLocale` (`+0x34`) · `gBaseAddress` (`+0x3f`).
     - ntdll 초기화 줄기에는 이 함수 호출이 없어서 **여기가 처음** 이다 `[정적, 지도 기준]`.
  3. `SetupAnsiOemCodeHashNodes` — 949 번 사전 (`NtGetNlsSectionPtr` 종류 `0xb`) 1~2번.
     - ANSI 와 OEM 이 둘 다 949 라서, 둘째는 첫째가 넣은 걸 찾을 것이다 `[추측]`.
  4. `MakeLocHashNode(0x7f)` — **이 아래는 못 닫았다**. 레지스트리·csrss 부르기 후보가 있다 → Q131 (a), 동적 Q104 4.
- run57 과의 관계: **같은 섹션 (같은 사전 원본) 을 kernelbase 가 자기 뷰로 또** 연다 `[정적]`.

비유: 관리실 책상엔 이미 949 번 사전이 한 판 펼쳐져 있다 (run57). KERNELBASE 건물도 자기 책상에 **같은 사전을 한 판 더** 펼친다. 원본은 하나, 펼친 판은 둘.

---

### 2.7 락 — 새 말: 크리티컬 섹션

#### 새 말 — 크리티컬 섹션

- **크리티컬 섹션** (`_RTL_CRITICAL_SECTION`, `0x28`B) = 유저 쪽 락. 여러 스레드 중 **한 번에 하나만** 어떤 일을 하게 막는 장치 `[추측]` (일반적으로 알려진 설명).
- 비유: **한 번에 한 명만 들어가는 방의 열쇠.** 열쇠를 가진 사람만 방에 들어가고, 나머지는 문 앞에서 기다린다.
- 칸 몇 개 (run 문서 · 짐 지도 이름):
  - `DebugInfo` = 열쇠에 다는 **출입 기록부** 를 가리키는 칸.
  - `SpinCount` = 잠들기 전에 문 앞에서 **잠깐 서성이며 기다리는 횟수** `[추측]` (일반적으로 알려진 설명).

#### `+0x508` 이 만드는 열쇠

- `+0x501/+0x508` `RtlInitializeCriticalSection(&KernelBaseGlobalData+0xb0)` `[정적]` (오케스트레이터 확인).
  - `KernelBaseGlobalData+0xb0` (rva `0x3aef70`, `0x28`B) = **컴퓨터 이름 캐시 락**. 잡는 곳 `GetComputerNameExW`, 지우는 곳 `BasepUnInitComputerNameCache`.
- 채우는 값 (`RtlInitializeCriticalSection`, rva `0xd65c0`) `[정적]`:
  - `LockCount` −1 · `RecursionCount`·`OwningThread`·`LockSemaphore` 0.
  - `SpinCount` = `0x20007d0`. CPU 가 1개 이하면 0. `[관찰]` run44 `NumberOfProcessors = 2` → `0x20007d0`.
  - **`DebugInfo` = −1** (`+0x51`) — 출입 기록부를 **안 단다** 는 표시.
- syscall 없음. 전역 목록 `RtlCriticalSectionList` 에도 안 끼운다.

#### 왜 출입 기록부를 안 다나 — 단계별로

1. 기록부를 만드는 건 ntdll 칠판 바이트 `RtlpForceCSDebugInfoCreation` 이 0 이 아닐 때뿐이다 (`+0x58 CMP` · `+0x5f JZ`) `[정적]`.
2. 그 바이트를 1 로 쓰는 곳은 셋뿐: `LdrpInitializeProcess +0x999` (`NtGlobalFlag & 0x1000` 또는 `LdrpShouldCreateStackTraceDb`) · `+0x9b5` · `+0x1a10` (`PEB.AppCompatFlags` 비트 22) `[정적]` (`_refs`).
3. `[관찰]` run44: `NtGlobalFlag = 0`, `AppCompatFlags = 0`.
4. 그래서 → 바이트는 0 그대로 → 기록부 없음 → 목록에 안 끼움.

- 한 가지 걸리는 점: run46 에서 본 `RtlInitializeCriticalSectionEx` 는 기록부를 ntdll 정적 배열 `RtlpStaticDebugInfo` 에서 받아 **달았다** (짐 지도 `RtlpProcessHeapsLock` 줄). 같은 "열쇠 만들기" 인데 기본값이 다르다. 까닭은 아직 모른다 → **Q131 (e)**.
- 디버거 영향: `NtGlobalFlag` 값은 커널 디버거가 붙은 실행 (run44) 에서 본 것이다. 다른 실행 방식에서 바뀌는지는 모른다 (run 문서 도구 메모).

#### 헷갈리기 쉬운 것 — 열쇠가 셋이다

| 어디서 | 함수 | 어떤 열쇠 |
|---|---|---|
| 래퍼 `KernelBaseBaseDllInitialize +0x55/+0x5c` (attach 만) | `RtlInitializeCriticalSectionAndSpinCount(…, 0xfa0)` | `_CrtLock_Exit` |
| 몸통 `+0x501/+0x508` | `RtlInitializeCriticalSection` | `KernelBaseGlobalData+0xb0` (컴퓨터 이름 캐시) |
| `ConsoleInitialize +0x42/+0x49` | `RtlInitializeCriticalSection` | `ConsoleStateLock` (kernelbase `0x3af160`) |

- 요청서는 `+0x508` 이 `…AndSpinCount` 쪽일 수 있다고 짐작했는데, `+0x508` 은 그냥 `RtlInitializeCriticalSection` 이고 `…AndSpinCount` 는 래퍼가 부른다 (run 문서 "정정").

---

### 2.8 콘솔 — 새 말: 콘솔 연결

#### 새 말 — 콘솔 연결

- test.exe 는 까만 창 (콘솔) 프로그램이다. 창을 실제로 그리는 건 옆집 **콘솔 호스트** 다 (run51 해설).
- **콘솔 연결** = 가게가 콘솔 드라이버 (`ConDrv`) 를 통해 "**이 창과 이어 주세요**" 하고 연결 손잡이를 하나 받는 일 `[정적]` (`NtCreateFile` … `\Connect`).
- 비유: 부모 (cmd) 가 쓰던 **쇼윈도를 같이 쓰겠다** 고, 부모가 넘겨 준 **소개장** (핸들 4) 을 내밀고 옆집에 "연결 신청서" 를 넣는다. 그러면 **내 전용 출입증** (새 핸들) 이 나온다.
  - `[관찰]` (run44 해설) 핸들 4 는 `\Reference {ConDrv}`, 새 핸들 `0x60` 은 `\Connect {ConDrv}` 로 보였다.

#### test.exe 가 타는 갈래 — "부모 콘솔에 붙기"

입력 값 `[관찰]` run44: `Subsystem = 3` (콘솔) · `ConsoleHandle = 4` · `ConsoleFlags = 0`.

`ConsoleInitialize` (kernelbase rva `0xbeb60`) 안, 순서대로 (`[정적]`):

1. `+0x49` `ConsoleStateLock` 열쇠 만들기 (2.7 표).
2. `+0x6a` `RtlImageNtHeader` → `Subsystem == 3` → `ConsoleIsConsoleApplication ← 1`.
3. `+0x94`·`+0x99`·`+0x9e` 준비 셋 — 할당 옵션 정리 (test.exe 는 아무것도 안 씀) · Ctrl 처리기 준비 · exe 이름 복사.
4. `+0xbd` `ConsoleShouldAllocateConsole` → TRUE. 매니페스트가 없어서 설정 조회가 실패하기 때문이다 (`[정적 + 관찰]`).
5. `ConsoleHandle` 값을 본다 (`+0x170 ~ +0x185`):
   - 0·−2·−3 이면 **새 콘솔 만들기** (`ConsoleAllocate`, conhost 띄우기). −1 이면 바로 마무리.
   - 4 는 그 어느 것도 아니다 → **실제 핸들** → "부모 콘솔에 붙기".
6. `+0x19f` `ConsoleCreateConnectionObject` → `+0x1c8` **`NtCreateFile(<핸들 4> 기준 "\Connect")`** → 새 핸들.
7. `+0x1cf` `ConsoleCommitState` (run51) → 매개변수 `ConsoleHandle` 을 `4 → 새 핸들` 로 덮기 · `NtDeviceIoControlFile(0x500023)` · `NtSetInformationProcess(0x31)`.

- syscall **3번**: `NtCreateFile` 1 (이번에 처음 닿음) + run51 의 둘 `[정적]`.
- 새 핸들 숫자 (사진 `0x60`), conhost PID 등은 콘솔 드라이버·부모 쪽이 주는 값이라 **동적** (Q104 5).
- 표준 핸들 셋은 안 바뀐다 (`ConsoleFlags & 2` 가 0) `[정적]`.

#### 짧게 — `ProcessGroupId` 는 안 쓴다 (결과서와 다르게 적은 곳)

- 결과서는 "매개변수 `+0x408` `ProcessGroupId` (0 → PID)" 를 짐 지도 반영거리로 적었다.
- 그런데 `ConsoleInitialize +0x9e CMP [params+0x408],EDI` 는 그 칸이 **0 일 때만** `+0xb2` 에서 PID 를 쓴다 `[정적]`.
- `[관찰]` run44: K1 부터 P2 까지 늘 `0x2f0`. 그래서 test.exe 에선 **조건부 안 탐** (짐 지도 3.2 에 그렇게 한 줄).
- `0x2f0` 은 test.exe PID (`0x1dd0`) 가 아니다. 부모 쪽에서 온 값일 것이다 `[추측]`.

---

### 2.9 ETW 등록 — 다섯째 채널, 준비는 이미 끝나 있다

- 공급자 이름 **`Microsoft.Windows.Kernel.Base`**, GUID `{3c74afb9-8d82-44e3-b52c-365dbf48382a}`, 칸 kernelbase `0x3a9020` (`0x40`B, PDB 이름 없음) `[정적]`.
- 모양은 run62 의 ntdll 쪽 등록과 같다: `EtwEventRegister` → `NtTraceControl(0xf)`, `EtwEventSetInformation` → `NtTraceControl(0x1e)`. **syscall 2번** `[정적]`.
- 왜 "Once" 4번이 없나:
  1. "Once" 는 이 프로세스의 **첫** 등록 때만 붙는다 (run62).
  2. 첫 등록은 `LdrpInitializeProcess +0xbee/+0xbf3` 에서 이미 했다.
  3. 이건 **다섯째** 등록이다 → Once 없음.
- 번호표 (RegHandle) 계산값 `0x5_00000041` — 앞 넷이 그대로 있다고 보고 계산한 것 `[정적 계산]`. 사진 확인은 Q104 6.

비유: 방송국 개국 준비는 관리실이 이미 끝냈다. KERNELBASE 는 **다섯째 채널** 하나를 올리고 번호표만 받는다.

---

### 2.10 봉인 스티커 — 모듈 자기 것, 프로세스 것과 다른 사슬

- `_DllMainCRTStartupForGS2` (24B) 는 이유 1 일 때 `+0x9/+0xe` `__security_init_cookie` 만 부른다 `[정적]`.
  - `__security_cookie` 가 0 이거나 기본값 `0x2b992ddfa232` 이면 `RDTSC` 로 새로 만들고, 옆 칸 `__security_cookie_complement` ← `~cookie`.
- 이건 그 모듈 **자기** `/GS` 봉인 스티커다. run59 의 프로세스 쿠키 (ntdll `CookieValue`, `RtlEncodePointer` 용) 와는 **별개 사슬** 이다 `[정적]`.
  - run59 해설 2.3 "쿠키가 둘" 의 첫째 (모듈 것) 쪽이다.
- 누가 부르나: kernelbase 는 래퍼 `+0x28/+0x2d` (모든 이유, 안에서 1 만), kernel32 는 진입점 `+0x31/+0x36` (이유 1).

---

### 2.11 ★ test.exe 에서 안 타는 셋 — 까닭은 모두 "갈림길의 값"

셋 다 함수 **자리** 는 있다. 하지만 test.exe 에서 갈림길 값이 "건너뛰기" 쪽이다.

#### (가) `DisableRegCachingForConstrainedImpersonation` — 함수는 불리지만 본문은 안 탄다

- **새 말 — 멀티세션 SKU**: 여러 사용자가 한 Windows 에 동시에 접속해 쓰는 판 `[추측]` (일반적으로 알려진 설명). 이 함수는 그런 판이 **아닐 때만** 본문을 탄다.
- 단계별로 (`[정적]`):
  1. `_KernelBaseBaseDllInitialize +0x184/+0x189` 에서 함수 자체는 불린다 (`g_Initialized == 0`, 첫 attach).
  2. 함수 머리 `+0x32/+0x39` 가 `RtlIsMultiSessionSku()` 를 부른다. 그 안은 이 세 줄이다:
     ```
     +0x26 MOV EAX,[0x7ffe02f0]
     +0x2d SHR EAX,8
     +0x30 AND AL,1
     ```
     KUSD `SharedDataFlags` 의 비트 8 을 꺼내는 것이다.
  3. `[관찰]` run44: `SharedDataFlags = 0x10e` → 비트 8 켜짐 → AL = 1.
  4. `+0x3e TEST AL,AL` · `+0x40 JZ` (본문은 0 일 때만) → 떨어져서 `+0x46` 반환.
- 탔다면: 토큰을 본다 (`NtQueryInformationToken(-4, 0x1d)` + `NtAccessCheck`). 결과 1바이트를 `g_DisableCachingWhileImpersonated` 에.
- 재미있는 점: **이름과 달리 레지스트리는 안 읽는다** `[정적]`.

#### (나) `RtlSetProtectedPolicy` — exe 가 "새 버전용" 이라서

- 단계별로 (kernel32 `_BaseDllInitialize`, `[정적]`):
  1. `+0xc7 ~ +0xd6` exe 헤더의 서브시스템 값 검사를 지나 `+0x28d` 로.
  2. `+0x28d CMP word [NT+0x48],6` — exe 헤더 `MajorSubsystemVersion` 이 6 이상인가.
  3. test.exe = **6** (결과서 `objdump -p`) → `+0x292 JNC` → `+0xfb` 로 **건너뜀**.
- 탔다면: ntdll 정책 표 `RtlpProtectedPolicies` 에 GUID `BasepAllowResourceConversion` = 1 을 넣는다.
- 비유: "옛날 규격 가게만 받는 특별 허가증" 이라, 새 규격 (6) 가게는 안 받는다.

#### (다) `GetRegistryExtensionFlags` — exe 가 "터미널 서버 인식" 표시를 달고 있어서

- 단계별로 (kernel32 `_BaseDllInitialize`, `[정적]`):
  1. `+0x1f9/+0x200` `RtlGetSuiteMask` → `+0x205 TEST AL,0x10`. `[관찰]` `SuiteMask = 0x110` → 켜짐 → `+0x207 JNZ` 로 `+0x350` 쪽.
  2. `+0x364/+0x369` `InitializeRegTermsrvFpns` 를 부른다. 이게 **곧바로 0** 을 돌려준다.
     - 까닭: 안의 `IsTerminalServerCompatible` 이 exe `DllCharacteristics` 비트 15 (`TERMINAL_SERVER_AWARE`) 를 본다. test.exe `0x8160` 은 켜져 있다 (flow.md 2.5).
  3. `+0x369 TEST AL,AL` · `+0x36b JZ` → `GetRegistryExtensionFlags` (`+0x380/+0x387`) 를 **건너뜀**.
- 탔다면: 레지스트리 3번 (`NtOpenKey`·`NtQueryValueKey("RegistryExtensionFlags")`·`NtClose`) → kernelbase `g_RegKrnGlobalState`.
- 이건 run25 에서 "안 탈 수 있다 `[추측]`" 이라고 적었던 자리다. **맞았다** → `[정적]` 으로 올림 (3절).

#### 덤 — 정상일 때 안 타는 것 셋 더

- `CsrpClientConnectToServer` — csrss 응답이 음수일 때만 (2.4 덤).
- `ConsoleAllocate` (새 콘솔) — `ConsoleHandle = 4` 가 0·−1·−2·−3 이 아니라서 (2.8).
- `CsrClientConnectToServer` 의 서버 길 — csrss 자신만 (2.4).

정리 표:

| 함수 | 갈림길 값 (`[관찰]`) | 그래서 |
|---|---|---|
| `DisableRegCaching…` 본문 | KUSD `SharedDataFlags = 0x10e` (비트 8) | 멀티세션 SKU → 바로 반환 |
| `RtlSetProtectedPolicy` | exe `MajorSubsystemVersion = 6` (PE 헤더) | 6 이상 → 건너뜀 |
| `GetRegistryExtensionFlags` | `SuiteMask = 0x110`, exe `DllCharacteristics = 0x8160` (PE 헤더) | 앞 함수가 0 → 건너뜀 |

---

### 2.12 KERNEL32 쪽 — 표로 짧게

`_BaseDllInitialize` (rva `0x2c610`) attach 갈래, 실행 순서 (`[정적]`, 라벨 ret):

| 자리 | 하는 일 | 무엇을 놓나 / 비고 |
|---|---|---|
| 진입점 `+0x36` | `_DllMainCRTStartupForGS2` (2.10) | kernel32 자기 봉인 스티커 |
| `+0x73` | `RtlImageNtHeader(exe)` | NULL 이면 DllMain FALSE |
| `+0x99` | **0 을 돌려주는 빈 함수** (ntdll `0x10bc80`, `XOR EAX,EAX; RET`) | kernel32 import 이름 `RtlSetThreadPoolStartFunc`. 같은 주소 이름 22개라 하는 일로 적음. 넘긴 두 함수는 어디에도 저장 안 됨 |
| `+0xba` · `+0xc7` | 탐침 칸 · 메모리 관리 사본 (run67) | |
| `+0xf0` | `RtlSetProtectedPolicy` | **안 탐** (2.11 나) |
| `+0x189` · `+0x194` | 연결 결과 옮겨 적기 | kernel32 `BaseStaticServerData` · `BaseCSDVersion` (BSSD + `0x3a`) |
| `+0x27c` | `RtlInitUnicodeString(&PEB.CSDVersion, BaseCSDVersion)` | `CSDVersion.Buffer == 0` 일 때. 코드 자리는 뒤쪽이지만 `+0x19e JNZ` 로 들어갔다 `+0x288 JMP` 로 돌아오는 **떨어진 조각** |
| `+0x200` · `+0x369` | `RtlGetSuiteMask` · `InitializeRegTermsrvFpns` → 0 | `GetRegistryExtensionFlags` **안 탐** (2.11 다) |
| `+0x217` | `LdrDisableThreadCalloutsForDll(kernel32)` | kernel32 모듈 항목 `Flags` 비트 18 **`DontCallForThreads`** (아래) |
| `+0x224` | `_Kernel32LegacyLib_BaseDllInitialize` | `KernelBaseGlobalData+0x28 ← 0` · kernel32 `BaseStaticServerData`·`BaseDllIniFileMapping` · **kernel32 `SysInfo`** (`+0xa3`, syscall 1) |

`LdrDisableThreadCalloutsForDll` (81B) 을 단계별로 (`[정적]`):

1. `PebLdr+0x48` (`ShutdownInProgress`) 가 0 이면 `+0x2a` `LdrpFindLoadedDllByHandle` 로 kernel32 명부 줄을 찾는다.
2. 그 줄의 `+0x6e` (`TlsIndex`) 가 0 일 때만,
3. `+0x3b OR byte [항목+0x6a],4` — `Flags` (`+0x68`) 의 **비트 18 `DontCallForThreads`** 를 켠다.
4. kernel32 는 TLS 디렉터리가 없어 `TlsIndex = 0` (run28 PE 파일, run44 사진) → **켜진다**.

- 이름으로 보아 "스레드가 생기고 끝날 때마다 이 DLL 의 전원 스위치를 다시 누르지 마라" 는 표시다 `[추측]` (일반적으로 알려진 설명).
- 그 밖: `KernelBaseGetGlobalData` (8B) = `&KernelBaseGlobalData` 를 돌려주는 `LEA` + `RET`. kernel32 가 `+0x28` 에 쓰는 0 은 detach 에서 `BaseDllReadWriteIniFile` 을 부를지 가를 때만 읽힌다 `[정적]`.

---

### 2.13 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

| 무엇 | 사진 값 | 이번 정적 결과와 |
|---|---|---|
| PEB CSR 칸 셋 | P1a 0 → P1b `0x7ff4ecf70000` · `0x7ff4ecf70750` · `0x7ff5575f0000` (P2 같음) | 옮기기 식과 맞음 (2.4) |
| PEB `CSDVersion` | P1a·P1b·P2 모두 `""` 로 찍힘 | 짐 지도는 `P1a~P1b` 로 셌다. `Buffer` 가 NULL → 빈 글 주소로 바뀌어도 이렇게 찍힐 것 `[추측]` (`dt` 출력에 `Buffer` 값이 없음) |
| `PEB.SessionId` | 1 | `\Sessions\1\Windows` |
| KUSD `SharedDataFlags` | `0x10e` | (가) 본문 안 탐 |
| `SuiteMask` | `0x110` | (다) `+0x350` 쪽 |
| `PEB.UseCaseMapping` | `0x6a5` | NLS 대문자 표 탄다 |
| `NumberOfProcessors` | 2 | 스핀 `0x20007d0` |
| 매개변수 `ConsoleHandle`·`ConsoleFlags` | 4 · 0 | 부모 콘솔에 붙기 |

- ntdll `Csr*` 8개·`gBaseAddress`·`gSystemLocale` 의 사진 구간 (`P1a~P1b`) 은 정적 결론 "KERNELBASE 시작 함수가 처음 쓴다" 와 **맞다**. 단 그 구간엔 dll1·dll2 올리기도 섞여 있어서, 사진만으로는 못 가른다 (Q84).

### 2.14 정정과 "다르게 적은 곳" (짧게)

- **이전 run 의 정정은 없다.** 오히려 두 짐작이 맞았다:
  - flow.md 2.5 (run25) "`GetRegistryExtensionFlags` 는 안 탈 수 있다 `[추측]`" → 안 탄다 `[정적]`.
  - 짐 지도 7.8 "`Csr*` 8개·PEB CSR 칸 4개 ↔ `CsrClientConnectToServer` `[추측: 이름]`" → 쓰는 줄이 나와 3장으로 옮김.
- **요청서 짐작이 틀린 곳** (요청서는 안 고침):
  - "`+0x2d4/+0x2d9` 쯤" → call `+0x2d2` / ret `+0x2d9`.
  - "`NtSecureConnectPort`/`NtAlpcConnectPort` 등" → `NtConnectPort`.
  - "`+0x508` … `…AndSpinCount`" → `+0x508` 은 `RtlInitializeCriticalSection`.
- **결과서와 다르게 적은 곳**: kernel32 `CSDVersion` 쓰기를 call `+0x275` / ret `+0x27c` 로 (결과서는 `+0x26e / +0x275`, `+0x26e` 은 `LEA`). 빈 함수 이름 수 22개 (요청서 21). `ProcessGroupId` 는 안 쓴다 (2.8). `WindowFlags` 비트 지우기는 정확한 줄이 없어 짐 지도에 안 옮김.
- **`.c` 기준인 곳** (`.asm` 대조 안 함): csrss 쪽 `CsrpHandleInlineUserConnect` · `RtlpCreateHeap` 의 "CommitRoutine 있음" 가지에 syscall 없음 · `RtlCheckTokenMembershipEx` 안 `NtAccessCheck` 횟수 · `RtlpCreateHeap` 의 클래스 0 캐시.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 곳

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | pdb 목록 run67 까지 | 넷 다 **run68** 추가 · 정적 바퀴 한 줄 (run44 로그 다시 읽음, kernelbase·kernel32 `.data` 는 여전히 사진에 없음) |
| 그림 2 KERNELBASE | 몸통 아래 `(1) +0x1b2` · `(2) +0x497` 둘 | `(1) +0x1b2` · **`(2) +0x2d9` `CsrClientConnectToServer` → `+0x118` `CsrpConnectToServer`** · `(3) +0x497` · **`(4) +0x4e6` `BaseNlsDllInitialize`** · **`(5) +0x526` `ConsoleInitialize`**. 몸통 상자 메모에 안 그린 것 (`SysInfo`·예외 필터·락·ETW·`DisableRegCaching…`) |
| 그림 2 KERNEL32 | `+0xba` 하나 | `(1) +0xba` · **`(2) +0x217` `LdrDisableThreadCalloutsForDll`** · **`(3) +0x224` `_Kernel32LegacyLib_BaseDllInitialize`**. `_BaseDllInitialize` 메모에 빈 함수 · `CSDVersion` · 안 타는 둘 |
| 2.5 사진 줄 | "`Csr*`·PEB CSR 칸·`ConsoleHandle` 은 시작 함수 안의 일로 보인다 `[추측]`" | 그 아래 **`[정적]` 줄**: 코드로는 그렇다 — 쓰는 줄이 모두 시작 함수 아래. 실행 때 그 순간은 동적 |
| 2.5 `GetRegistryExtensionFlags` | "안 탈 수 있다 `[추측]`" | **(해결됨, 정적) 안 탄다** — `InitializeRegTermsrvFpns` 가 0 |
| 2.5 시작 함수 줄들 | 이유 1 갈래 대표 이름만 | 쿠키 줄 (모듈 자기 것) · KERNELBASE 나머지 순서 줄 · run51 "`.asm` 없음" → 이제 있음 · KERNEL32 run68 줄 |
| 2.5 새 절 | — | **"시작 함수 안 나머지 — csrss 연결 · `SysInfo` · 예외 필터 · NLS · 락 · 콘솔 · ETW · kernel32 정책·스레드 알림 끄기"** — 트리 + `[정적]` 줄 |
| 4장 | Q129 대기 | 해결됨 줄 + 새 줄 넷 (동적 → Q84, 동적 → Q104, 정적 남은 것, 짐작 틀림·다르게 적은 곳) |
| 4.1 · 5장 | run67 까지 | 그림 2 두 줄을 run68 상자로 고침, run68 줄 |

- 태그 변화 한 줄로: P1a ~ P1b 에 바뀌는 `Csr*`·PEB CSR 칸·`ConsoleHandle` 의 **쓰는 쪽** 이 `[추측]` 에서 **`[정적]` 근거가 붙은 것** 으로 바뀌었다. **값** 은 여전히 `[관찰]` (사진) 이고, 정확한 순간은 동적 확인 전이다.

### 짐 지도 (`baggage.md`) 에서 바뀐 곳

- 3.1 PEB 줄 4 (CSR 칸 셋 · `CSDVersion`), 3.2 매개변수 줄 1 (`ProcessGroupId` 조건부 안 탐), 3.5 모듈 항목 줄 1 (`DontCallForThreads`), 3.8 `.data` 줄 17.
- 그래서 숫자:
  - 구조체 모르는 칸 316 → **311** (7.2, 5칸 닫힘).
  - ntdll 전역 쓴 자리 모르는 것 325 → **311** (7.3, 14개 닫힘).
- 새 **7.6.8**: KERNELBASE·KERNEL32 수첩 칸 (`SysInfo` 두 벌 · 연결 결과 · 예외 필터 · NLS · 락 · 콘솔 · ETW · 쿠키) + 핸들·뷰·섹션. 체크리스트 밖이라 2장 칸 수엔 안 센다.
- 새 **7.27**: 이번에 넘긴 질문.

### 큰 그림과 이어지는 곳

- **PE_emulator 요청 16 이 다 끝났다.** kernelbase·kernel32 시작 함수를 흉내 낼 근거가 이제 거의 다 있다.
  - 바로 고칠 것 하나: nt 대답의 `SysInfo` 에 **`+0x8 = 0x1000`, `+0x20 = 0x10000`** (2.3).
  - 안 타는 셋은 흉내 낼 필요가 없다는 근거가 생겼다 (2.11).
- csrss 연결은 **꼴은 닫히고 값은 열린** 상태다. PE_emulator 가 이 칸들을 채우려면 csrss 가 실제로 주는 값을 한 번 봐야 한다 (Q84).
- 다음 바퀴는 **Q104 VM 단계** — 이번에 쌓인 멈춤점 (`SysInfo` 두 벌, 진행 표시판, NLS syscall 수, 콘솔 새 핸들 …) 을 한 번 실행으로 모아서 본다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| csrss 연결 결과 값 (`CsrPortHandle` 숫자, 응답 블록, PEB 세 칸, `BaseStaticServerData`) | 사무실 (csrss) 이 주는 값이다. 코드엔 "어디에 적나" 만 있음 | **Q84** 동적 — `ntdll!CsrpConnectToServer+0x2e4` (`NtConnectPort` ret), `+0x38d` 직후, `_KernelBaseBaseDllInitialize+0x32b` 직후 |
| 둘째 전화 (`CsrpClientConnectToServer`) 를 정말 안 거나 | csrss 쪽 상수에 기댄 결론. 그 반쪽 (`CsrpHandleInlineUserConnect`) 은 `.c` 기준 | **Q84** (응답 상태 `[RBP+0x1c]` = 0 기대) · 정적 **Q131 (d)** `.asm` 대조 |
| `SysInfo` 동적 6칸 (`+0x4`·`+0xc`·`+0x10`·`+0x14`·`+0x30`·`+0x38`) 값 | 부팅 때 정해지는 커널 값. 사진에 kernelbase·kernel32 `.data` 가 없음 | **Q104** 1 — `+0x241` 에서 `dq kernelbase+0x3aee60 L8`, kernel32 `+0xa3` 에서 `dq kernel32+0xc2260 L8` |
| 진행 표시판이 정말 이 순서로 오르나 | 순서는 점프를 읽은 것 | **Q104** 2 — `dw kernelbase+0x3aeea0 L1` 을 단계마다 |
| `DisableRegCaching…` 가 정말 아무것도 안 쓰나 | 사진 값 + 코드로 계산한 결론 | **Q104** 3 |
| NLS syscall 실제 수, `MakeLocHashNode(0x7f)` 아래 | 그 아래 갈래를 안 따라갔다 | **Q104** 4 (동적) · **Q131 (a)** (정적) |
| `ConsoleShouldAllocateConsole` 안 `RtlQueryActivationContextApplicationSettings` 본문 | 매니페스트 없는 exe 에서 실패한다는 결론의 본문을 안 읽음 (syscall 없을 것 `[추측]`) | **Q131 (b)** |
| 콘솔 새 핸들 숫자 · `ConsoleShouldAllocateConsole` 이 정말 TRUE 인가 | 콘솔 드라이버·부모가 주는 값 | **Q104** 5 |
| ETW 번호표 · 락 칸 · kernel32 `Flags` 비트 18 실제 값 | 계산값·코드 결론 | **Q104** 6 |
| NLS 힙 블록 크기·개수 | 이번 범위 밖 (짐 지도 7.6.8 "미확인") | **Q131 (c)** |
| 열쇠 만들기 두 함수의 `DebugInfo` 기본값이 다른 까닭 (−1 vs 정적 배열) | 이번엔 `RtlInitializeCriticalSection` 쪽만 봄 | **Q131 (e)** |
| `KernelBaseGlobalData +0x60 ~ +0x90` 칸별 뜻 | NLS 파일 정보 칸이라는 것까지만 | **Q131 (f)** |
| `ConsoleCommitState` 의 `WindowFlags` 비트 지우기 줄 | 정확한 줄이 없음. test.exe `WindowFlags = 0` 이라 결과는 같음 | **Q131 (g)** |
| 디버거가 없을 때도 `NtGlobalFlag` 가 0 인가 | run44 는 커널 디버거가 붙은 실행. 바뀌면 열쇠마다 출입 기록부가 생김 `[정적]` | 모름 (run 문서 도구 메모) |

- ntdll·kernelbase 멈춤점은 **test.exe 일 때만** 걸리게 조건을 붙인다 (run 문서).

---

## 5. 더 읽기

- run 문서: [`run68-dll-init-rest.md`](run68-dll-init-rest.md) — "PE_emulator 쪽에서 쓸 것" (한 줄 결론 여덟 · `SysInfo` 칸 표 · 작은 표 · 안 타는 것 표), attach 순서 표, 호출 관계 트리, syscall 표, 사진과 맞춰 본 것, 정정
- 결과서: [`_workspace/sast/63_dll-init-rest_result.md`](../../_workspace/sast/63_dll-init-rest_result.md) — 질문별 답 1 ~ 12 (특히 4번 `CsrpConnectToServer` 표, 8번 `ConsoleInitialize` 표, 9번 칸 표)
- 요청서: [`_workspace/sast/63_dll-init-rest.md`](../../_workspace/sast/63_dll-init-rest.md) — 12문항 원문
- flow.md: [2.5 "시작 함수 안 나머지" 절](flow.md) · 2.5 시작 함수 줄들 · 그림 2 · 4장 run68 줄 · 4.1 · 5장
- 짐 지도: [`baggage.md`](baggage.md) 3.1 · 3.2 · 3.5 · 3.8 · **7.6.8** · **7.27** · 7.2 · 7.3
- 앞선 해설: [run67](run67-kb-heap-init-해설.md) (같은 시작 함수의 급한 셋) · [run25](../02_sprint/run25-dll-entry-해설.md) (시작 함수 이름) · [run51](run51-eprocess-later-해설.md) (`ConsoleCommitState`, 콘솔 호스트) · [run57](run57-nls-apiset-해설.md) (글자 사전) · [run59](run59-init-leftovers-해설.md) (쿠키 둘, `RtlEncodePointer`) · [run62](run62-tp-etw-init-해설.md) (ETW 등록·Once) · [run14](../02_sprint/run14-create-process-internal-해설.md) · [run34](../02_sprint/run34-parent-leftovers-해설.md) (csrss) · [run44](run44-baggage-snap-해설.md) (사진)

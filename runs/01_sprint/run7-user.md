# 2026-10-17 run7 — 유저(ntdll) 쪽: LdrInitializeThunk → main

> **정정 (run8 이후)**: 이 run에서 `dll2!DllMain`으로 찍힌 것은 실제로는 **dll1의 DllMain**이었을 가능성이 크다.
> dll1·dll2의 DllMain 오프셋이 같고(`+0x12e0`), 이번엔 dll1이 run6의 dll2 자리에 올라온 것으로 보인다.
> 실제 초기화 순서는 **dll2 → dll1 → exe** (run8에서 이름으로 확인). 아래 "dll1 먼저" 추정은 틀렸다.

## 환경

- run6과 같은 부팅 (VM 일시정지 후 재개, 재부팅 없음)
- test.exe: Cid `0af8`, EPROCESS `ffff908f8ea54080`
- 메인 스레드: `ffff908f8abaa080`

## 방법

- cmd의 `PspCallProcessNotifyRoutines`에서 멈춤 → `.reload /user`
- ntdll 함수 5개: `bp /p <test> ntdll!X ".echo; ? @$thread; k 5; g"`
- sample 함수 4개: `ba e1 <run6 주소> ".if (@$proc == <test>) {...; r rdx; g} .else {gc}"`
  - `rdx` = DllMain/TLS 콜백이 불린 이유 (1 프로세스 시작, 2 스레드 시작, 3 스레드 끝, 0 프로세스 끝)
- 로그: `run7-user.log`

## 메인 스레드 시간표 (`ffff908f8abaa080`) — main까지

| 순번 | 함수 | 부른 곳 (콜스택 요약) | 표시 |
|---|---|---|---|
| 1 | `ntdll!LdrInitializeThunk` | (커널에서 바로 진입) | [26100.9457 확인] |
| 2 | `ntdll!LdrpInitializeProcess` | `LdrInitializeThunk+0xe` → `LdrpInitializeInternal+0x5a` → `LdrpInitialize+0x16c` | [26100.9457 확인] |
| 3 | `ntdll!LdrpLoadDll` | `LdrpInitializeProcess+0x19a2` → `LdrpInitializeKernel32Functions+0xc0` → `LdrLoadDll` | [26100.9457 확인] |
| 4 | `LdrpCallTlsInitializers` → `LdrpCallInitRoutine` ×2 | `LdrpLoadDllInternal` → `LdrpPrepareModuleForExecution` → `LdrpInitializeGraphRecurse` → `LdrpInitializeNode` | [26100.9457 확인] |
| 5 | `ntdll!LdrpLoadDll` | `LdrpInitializeKernel32Functions+0x156` | [26100.9457 확인] |
| 6 | `LdrpCallTlsInitializers` → `LdrpCallInitRoutine`(TLS) → `LdrpCallInitRoutine`(시작 함수) | `LdrpInitializeProcess+0x1bbb` → `GraphRecurse` ×3 → `LdrpInitializeNode` | [26100.9457 확인] |
| 7 | `LdrpLoadDll` ×3 | `KERNELBASE!LoadLibraryExW` ← `0x7ffd80e2f5fb` (모듈 미확인) | [26100.9457 확인] |
| 8 | `LdrpCallTlsInitializers` → `LdrpCallInitRoutine`(TLS) → `LdrpCallInitRoutine`(시작 함수) | `LdrpInitializeProcess+0x1bbb` → `GraphRecurse` ×2 → `LdrpInitializeNode` | [26100.9457 확인] |
| 9 | `LdrpLoadDll` ×3 | `KERNELBASE!LoadLibraryExW` ← `0x7ffd80ebf60b` (dll2 주소 범위 안) | [26100.9457 확인] |
| 10 | **`dll2!DllMain`** (rdx=1) | | [26100.9457 확인] |
| 11 | `LdrpCallTlsInitializers` → `LdrpCallInitRoutine` | `LdrpInitializeProcess+0x1c99` | [26100.9457 확인] |
| 12 | **`test!ExeTls`** (rdx=1) | | [26100.9457 확인] |
| 13 | `LdrpLoadDll` ×3 | `KERNELBASE!LoadLibraryExW` ← `0x7ff70ee67723` (test.exe 안) | [26100.9457 확인] |
| 14 | **`test!main`** | | [26100.9457 확인] |

(6번과 8번 사이에 ntdll 일꾼 스레드 `ffff908f8a980080`의 `LdrInitializeThunk`가 한 번 찍힘)

### 호출 관계 (메인 스레드)

```
LdrInitializeThunk
 └─ LdrpInitializeInternal
     └─ LdrpInitialize
         └─ LdrpInitializeProcess
             ├─ +0x19a2  LdrpInitializeKernel32Functions
             │            └─ LdrLoadDll → LdrpLoadDll → LdrpLoadDllInternal
             │                 └─ LdrpPrepareModuleForExecution → GraphRecurse → LdrpInitializeNode
             ├─ +0x1bbb  LdrpInitializeGraphRecurse (import로 붙은 DLL들 초기화)
             │            └─ LdrpInitializeNode
             │                 ├─ +0x16b  LdrpCallTlsInitializers → LdrpCallInitRoutine (TLS 콜백)
             │                 └─ +0x19c  LdrpCallInitRoutine (DLL 시작 함수 → DllMain)
             └─ +0x1c99  LdrpCallTlsInitializers → LdrpCallInitRoutine → test!ExeTls
```

## 확인된 것

- 유저 쪽 첫 흐름: `LdrInitializeThunk` → `LdrpInitializeInternal` → `LdrpInitialize` → `LdrpInitializeProcess`
- `LdrpInitializeProcess`가 **kernel32를 직접 `LdrLoadDll`로 불러온다** (`LdrpInitializeKernel32Functions`).
- **import로 붙은 DLL은 `LdrpLoadDll`을 타지 않는다.** `LdrpLoadDll`은 kernel32 로드와 `LoadLibraryExW` 호출 때만 찍혔다. (함정 카메라 결과)
- import DLL 초기화는 `LdrpInitializeGraphRecurse` → `LdrpInitializeNode`가 모듈마다 한다.
  - 모듈마다 **TLS 콜백 먼저**(`+0x16b`), 그다음 **시작 함수**(`+0x19c`)
- `LdrpCallInitRoutine`은 DllMain 전용이 아니라 **TLS 콜백을 부를 때도** 쓰인다 (`LdrpCallTlsInitializers+0x19b`에서 부름).
- exe의 TLS 콜백(`test!ExeTls`, rdx=1)은 **모든 DLL 초기화가 끝난 뒤, main 전에** 불린다.
- `ExeTls`와 `main` 사이에 test.exe 안 코드(CRT 시작 코드로 추정)가 `LoadLibraryExW`를 3번 부른다.
- DLL마다 시작 함수 안에서 `LoadLibraryExW` 3번 → dll2의 경우 그 뒤에 `dll2!DllMain`.
  → DLL 시작 함수(`_DllMainCRTStartup` 추정)가 CRT 준비를 먼저 하고, 그다음 DllMain을 부르는 것으로 보임 [확인 필요]

## 아직 확인 필요

- **`dll1!DllMain`이 한 번도 안 찍혔다.**
  - 원인 추정: dll1이 run6과 **다른 주소**에 올라와서 `ba`가 빗나감.
  - 근거: 7번의 `0x7ffd80e2f5fb`는 run6의 dll1·dll2 범위 어디에도 없고, dll2 쪽(`0x7ffd80ebf60b`)과 끝자리 모양이 비슷하다.
  - 이게 dll1이라면 초기화 순서는 **dll1 → dll2 → exe**. 그래프 깊이도 dll1 쪽(GraphRecurse ×3)이 dll2(×2)보다 깊다.
  - → sample 코드에서 누가 누구를 import하는지와 함께 확인 필요
- ntdll 일꾼 스레드(`8a980080`, `8bc58080`)는 `LdrInitializeThunk`만 찍히고 DllMain·TLS는 안 불렸다.
  → 스레드 시작 알림을 건너뛰도록 만들어진 것으로 보임 [확인 필요]
- kernel32 쪽 4번 단계에서 초기화된 모듈 이름 (kernelbase/kernel32 추정) [확인 필요]

## main 이후 (참고, 이번 범위 밖)

- Worker 스레드(`ffff908f901cc080`): `LdrInitializeThunk` → `LdrpInitializeThread`
  - 시작: 모듈별 TLS → DllMain(rdx=2) 순서, 마지막에 `test!ExeTls`(rdx=2)
  - 끝: `RtlExitUserThread` → `LdrShutdownThread` → `dll2!DllMain`(rdx=3) … → `test!ExeTls`(rdx=3)
- 메인 스레드 종료: `KERNEL32!ExitProcessImplementation` → `RtlExitUserProcess` → `LdrShutdownProcess` → `LdrpCallInitRoutine` …
  - 종료 직전 `LoadLibraryExW`로 DLL이 하나 더 올라와 초기화됨 (모듈 미확인)

## 디버거 메모

- 마지막에 `Single step exception - code 80000004`로 **멈춤** (`dll2!DllMain` 주소, 프로세스 종료 중).
  - `ba`(하드웨어) 멈춤점이 걸렸는데 디버거가 조건 명령을 실행하지 않고 "한 걸음 실행" 예외로 보고함. [원인 확인 필요]
  - 이후 `bc *`, `.logclose`. → 타깃이 멈춘 채면 `g` 필요.
- DLL 주소는 재부팅 없이도 바뀔 수 있다 (dll1 추정). `ba`에 고정 주소를 쓰면 빗나갈 수 있다.
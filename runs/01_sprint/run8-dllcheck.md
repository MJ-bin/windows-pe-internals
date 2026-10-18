# 2026-10-17 run8 — DLL 주소 확정 + 초기화 대상 이름으로 보기

## 환경

- run7과 같은 부팅
- test.exe: Cid `0390`, EPROCESS `ffff908f92ce2080`
- 메인 스레드: `ffff908f96eea080`

## 방법

- cmd의 `PspCallProcessNotifyRoutines`에서 멈춤 → `.reload /user`
- `ub ntdll!LdrpInitializeProcess+0x1bbb L1` →
  `00007ffd962edfba  call ntdll!LdrpInitializeGraphRecurse` (= `LdrpInitializeProcess+0x1bb6`) [26100.9457 확인]
- 이 call 직전에 멈춤(BEFORE-GRAPH) → 이때 dll1·dll2 주소를 보고 **이름으로** `ba`를 건다
- `LdrpCallInitRoutine`에서 `ln @rcx`(부를 함수 이름), `r r8`(이유) 기록
- `test!main`에서 멈추고 끝
- 로그: `run8-dllcheck.log`

## 모듈 주소 (BEFORE-GRAPH 시점)

| 모듈 | run6 | run8 |
|---|---|---|
| dll1 | `00007ffd 814e0000` | `00007ffd 4f190000` |
| dll2 | `00007ffd 80e70000` | `00007ffd 4ea20000` |
| test | `00007ff7 0ee10000` | `00007ff7 0ee10000` (같음) |

→ **재부팅 없이도 DLL 주소는 실행마다 바뀔 수 있다.** test.exe는 그대로였다.
→ dll1·dll2 모두 `DllMain`이 같은 오프셋(`+0x12e0`)에 있다.

## 메인 스레드 시간표 (`ffff908f96eea080`) — main까지

| 순번 | 지점 | `LdrpCallInitRoutine`이 부른 것 (`ln @rcx`) | r8 | 표시 |
|---|---|---|---|---|
| 1 | `ntdll!LdrInitializeThunk` | | | [26100.9457 확인] |
| 2 | `ntdll!LdrpInitializeProcess` | | | [26100.9457 확인] |
| 3 | `LdrpCallTlsInitializers` (`LdrpInitializeNode+0x16b`) | (TLS 콜백 호출 없음) | | [26100.9457 확인] |
| 4 | `LdrpCallInitRoutine` (`LdrpInitializeNode+0x19c`) | `KERNELBASE!KernelBaseDllInitialize` 근처 (정확히 일치 아님) | 1 | [확인 필요] |
| 5 | `LdrpCallInitRoutine` (`LdrpInitializeNode+0x19c`) | `KERNEL32!BaseDllInitialize` 근처 (정확히 일치 아님) | 1 | [확인 필요] |
| 6 | **BEFORE-GRAPH** (`LdrpInitializeProcess+0x1bb6`) | | | [26100.9457 확인] |
| 7 | `LdrpCallTlsInitializers` → `LdrpCallInitRoutine` | `ntdll!ImageTlsCallbackCaller` | 1 | [26100.9457 확인] |
| - | (ntdll 일꾼 스레드 `97b10080`, `96dc9080`의 `LdrInitializeThunk`) | | | |
| 8 | `LdrpCallInitRoutine` (`LdrpInitializeNode+0x19c`) | `dll2!_DllMainCRTStartup` | 1 | [26100.9457 확인] |
| 9 | **`dll2!DllMain`** (rdx=1) | | | [26100.9457 확인] |
| 10 | `LdrpCallTlsInitializers` → `LdrpCallInitRoutine` | `ntdll!ImageTlsCallbackCaller` | 1 | [26100.9457 확인] |
| 11 | `LdrpCallInitRoutine` (`LdrpInitializeNode+0x19c`) | `dll1!_DllMainCRTStartup` | 1 | [26100.9457 확인] |
| 12 | **`dll1!DllMain`** (rdx=1) | | | [26100.9457 확인] |
| 13 | `LdrpCallTlsInitializers` (`LdrpInitializeProcess+0x1c99`) → `LdrpCallInitRoutine` | `ntdll!ImageTlsCallbackCaller` | 1 | [26100.9457 확인] |
| 14 | **`test!ExeTls`** (rdx=1) | | | [26100.9457 확인] |
| 15 | **`test!main`** | | | [26100.9457 확인] |

## 확인된 것

- import DLL 초기화 순서: **dll2 → dll1 → exe**
  - dll1이 dll2를 import하는 구조라면 맞는 순서 (기대는 쪽이 나중) — sample 코드와 대조 필요
- `LdrpCallInitRoutine`의 `rcx` = 부를 함수, `r8` = 이유(1=프로세스 시작)
  - 근거: `rcx`가 `_DllMainCRTStartup`과 정확히 일치, `r8=1`이 바로 뒤 `DllMain`의 `rdx=1`과 일치
- DLL의 시작 함수는 `DllMain`이 아니라 **CRT의 `_DllMainCRTStartup`**. 이게 `DllMain`을 부른다.
- TLS 콜백은 직접 불리지 않고 **`ntdll!ImageTlsCallbackCaller`를 거쳐** 불린다.
- 모듈 하나마다: `LdrpInitializeNode` → TLS(`+0x16b`) → 시작 함수(`+0x19c`)
- ntdll 일꾼 스레드 2개는 import DLL 초기화가 시작될 무렵 출발한다.

## run7 정정

- run7에서 `dll2!DllMain`으로 기록한 것은 **실제로는 dll1의 DllMain이었을 가능성이 크다.**
  - run7에서 `ba`를 run6의 dll2 주소(`80e712e0`)에 걸었는데, dll1·dll2의 DllMain 오프셋이 같다(`+0x12e0`).
  - run7에서 dll1이 run6 dll2 자리(`80e70000`)에 올라왔다면 정확히 그 주소가 된다.
  - 그러면 run7의 순서도 "알 수 없는 모듈(=dll2) → dll2라고 적은 것(=dll1)"이 되어 run8과 맞는다.
- run7에서 "dll1이 먼저"라고 추정한 것은 틀렸다.

## 아직 확인 필요

- 4·5번 `ln` 결과가 "정확히 일치"가 아니다. kernelbase·kernel32 시작 함수 주소가 기호 사이에 있다.
- `ImageTlsCallbackCaller` 안에서 실제 TLS 콜백(dll CRT의 것, `test!ExeTls`)을 부르는 과정
- import DLL이 **메모리에 올라오는(매핑) 순간**은 아직 못 봤다 (BEFORE-GRAPH 전에 이미 올라와 있었음)

## 디버거 메모

- DLL에 `ba`를 고정 주소로 걸지 않는다. BEFORE-GRAPH에서 멈춘 뒤 **이름으로** 건다.
- `test!main`에서 멈추고 끝내니 종료 중 `Single step exception` 문제가 안 생겼다.
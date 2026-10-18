# 2026-10-17 run5 — test.exe 스레드 4개의 정체

## 환경

- run3·run4와 동일 (26100.9457)
- cmd.exe: EPROCESS `ffff908f8acd4080`
- test.exe: Cid `156c`, EPROCESS `ffff908f92cd60c0`, 부모 `1d94`

## 목적

run4에서 예상(2개)보다 많은 4개의 스레드가 나왔다. 각 스레드가 어디서 시작하는지 보고 누가 누군지 구분한다.

## 방법

1. cmd의 `nt!PspCallProcessNotifyRoutines`에서 멈춤 → `bc 0` (다시 안 걸리게)
2. `bp /p <test> nt!PspUserThreadStartup ".echo ...; !thread @$thread; g"`
3. 로그: `run5-thread-who.log`

## 결과

| 순번 | 스레드 (Cid) | ETHREAD | Win32 Start Address | 정체 | 표시 |
|---|---|---|---|---|---|
| 1 | `156c.2740` | `ffff908f91ad60c0` | `0x00007ff70ee11d40` | test.exe 진입점 `mainCRTStartup` (RVA `0x1D40`) → 메인 스레드 | [26100.9457 확인] |
| 2 | `156c.1ef8` | `ffff908f938ec080` | `ntdll!TppWorkerThread` | ntdll 스레드풀 일꾼 | [26100.9457 확인] |
| 3 | `156c.2814` | `ffff908f946d7080` | `ntdll!TppWorkerThread` | ntdll 스레드풀 일꾼 | [26100.9457 확인] |
| 4 | `156c.0ffc` | `ffff908f8d87d080` | `0x00007ff70ee112e0` | test.exe 안 → `CreateThread`로 만든 스레드 | 주소: [26100.9457 확인] / 함수 이름: [확인 필요] |

- 네 스레드 모두 커널 콜스택은 같다:
  `nt!PspUserThreadStartup` ← `nt!KiStartUserThread+0x28` ← `nt!KiStartUserThreadReturn` → (유저) `ntdll!RtlUserThreadStart`
- `ntdll!TppWorkerThread` = `0x00007ffd9627a2e0` (이번 부팅 기준)

## 확인된 것

- test.exe가 실행되면 스레드 4개가 이 순서로 출발한다: **메인 → ntdll 일꾼 2개 → 우리 `CreateThread` 스레드**
- 일꾼 2개는 test.exe 코드가 아니라 **ntdll이 스스로 만든** 스레드다 (시작 주소가 `ntdll!TppWorkerThread`).
- 일꾼 2개는 메인 스레드 바로 뒤, 우리 `CreateThread`보다 먼저 출발한다.

## 아직 확인 필요

- 일꾼 2개를 만드는 이유: DLL을 여러 개 동시에 불러오기 위한 ntdll 도우미(병렬 로더)로 알려져 있음 [확인 필요]
  - 다음: 누가 이 스레드를 만드는지(만드는 쪽 콜스택) 확인
- `0x7ff70ee112e0`이 sample의 스레드 함수인지 (RVA `0x12E0`)
  - 확인 방법: 타깃에서 빌드한 test.pdb를 디버거 VM에 두고 이름으로 보기
- (확인 완료) `0x7ff70ee11d40` = test.exe 진입점: 타깃에서 `dumpbin /headers` → `1D40 entry point (0000000140001D40) mainCRTStartup`
- 커널이 유저 첫 위치를 `LdrInitializeThunk`로 보내는 것과, 콜스택에 보이는 `RtlUserThreadStart`의 관계

## 디버거 메모

- Break 시점엔 test.exe가 이미 끝나 있어서 `lm m test`가 빈칸 → test.exe 주소 범위는 못 봤다.
  다음엔 test.exe가 살아 있을 때(멈춤점 안에서) 보거나, test.pdb로 이름을 바로 띄운다.
- run4 끝의 "cmd가 `NtCreateUserProcess`를 한 번 더 부른 것"은 아직 원인 모름.
  손으로 누른 Break라면 `Break instruction exception` 문구가 찍히는데, 그때는 `==== NtCreateUserProcess`(cmd 필터 멈춤점)가 찍혔다. [확인 필요]
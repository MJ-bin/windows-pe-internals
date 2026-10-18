# 2026-10-17 run6 — 심볼 짝 확인 + 유저 쪽 이름 확인

## 환경

- 26100.9457, 재부팅 없이 run5에서 이어짐
- test.exe: **디버거 VM에서 빌드한 것**을 타깃에 복사해서 실행 (방법 B)
  - 타깃 경로: `C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin\test.exe`
  - 디버거 pdb 경로: `C:\windows-pe-internals\sample\bin` (`.sympath+`)
  - test.exe Timestamp: `Fri Oct 16 22:49:38 2026 (6AD22B72)`
- test.exe: Cid `2bf4`, EPROCESS `ffff908f913c9080`

## 방법

1. cmd의 `PspCallProcessNotifyRoutines`에서 멈춤 → test.exe 주소 얻음
2. `bp /p <test> nt!PspUserThreadStartup` (멈춤) → 4번째 스레드에서 확인 작업
   - 이 시점엔 test.exe, dll1, dll2가 모두 메모리에 있다
3. `.reload /user` → `lm`, `x`

## 결과: 심볼 짝

| 모듈 | 주소 범위 | 심볼 | 표시 |
|---|---|---|---|
| test | `00007ff7 0ee10000` ~ `0ee9c000` | private pdb symbols | 짝 맞음 |
| dll1 | `00007ffd 814e0000` ~ `8156c000` | (x로 타입까지 나옴) | 짝 맞음 |
| dll2 | `00007ffd 80e70000` ~ `80efc000` | (x로 타입까지 나옴) | 짝 맞음 |

- 4번째 스레드 시작 주소 = **`test!Worker`** (`0x00007ff70ee112e0`) → run5의 "CreateThread 스레드" 추정 확정 [26100.9457 확인]

## 결과: 이름 확인 (주소는 이번 부팅 기준)

### sample 코드

| 이름 | 주소 | 비고 |
|---|---|---|
| `test!main` | `00007ff7 0ee11320` | |
| `test!ExeTls` | `00007ff7 0ee112a0` | exe TLS 콜백 (`test!exe_tls_cb`가 가리킴) |
| `test!Worker` | `00007ff7 0ee112e0` | CreateThread 스레드 함수 |
| `dll1!DllMain` | `00007ffd 814e12e0` | |
| `dll2!DllMain` | `00007ffd 80e712e0` | |

### ntdll (모두 [26100.9457 확인] — 이름 존재만)

| 이름 | 주소 |
|---|---|
| `ntdll!LdrInitializeThunk` | `00007ffd 96341420` (run4) |
| `ntdll!LdrpInitialize` | `00007ffd 96341448` |
| `ntdll!_LdrpInitialize` | `00007ffd 962ebf8c` (같은 이름으로 검색됨, 별개 함수) |
| `ntdll!LdrpInitializeProcess` | `00007ffd 962ec404` |
| `ntdll!LdrpLoadDll` | `00007ffd 9627f9c0` |
| `ntdll!LdrpLoadDllInternal` | `00007ffd 9627fc30` |
| `ntdll!LdrpCallInitRoutine` | `00007ffd 96270ba0` |
| `ntdll!LdrpCallTlsInitializers` | `00007ffd 96270e50` |

## 눈에 띈 것

- `LdrInitializeThunk`(`...41420`) 바로 뒤에 `LdrpInitialize`(`...41448`)가 붙어 있다.
  → `LdrInitializeThunk` → `LdrpInitialize` → `_LdrpInitialize` 로 이어질 가능성 [확인 필요]
- 실제로 **불리는지, 순서는 어떤지**는 아직 모름 → run7에서 확인

## 디버거 메모

- `!thread`의 시작 주소가 이름으로 나오려면 `.reload /user`가 먼저 필요했다.
- 주소들은 **재부팅 전까지만** 유효하다. 재부팅하면 run6을 다시 해서 주소를 새로 얻는다.
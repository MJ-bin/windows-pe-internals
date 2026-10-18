# 2026-10-17 run4 — test.exe 스레드 출발 (커널 → 유저)

## 환경

- run3과 동일 (26100.9457, ntkrnlmp.pdb `C29EBFB06B78B3C020DCA66D99713F9E1`)
- cmd.exe: Cid `1d94`, EPROCESS `ffff908f8acd4080`
- test.exe: Cid `102c`, EPROCESS `ffff908f943f5080`, 부모 `1d94`

## 목적

test.exe의 스레드가 처음 움직일 때, 커널 출발점과 유저(ntdll) 출발점이 어떻게 이어지는지 확인.

## 방법

1. cmd의 `nt!PspCallProcessNotifyRoutines`에서 **진짜로 멈춤** (이 시점엔 test.exe가 이미 목록에 있음)
2. `.reload /user` → `x ntdll!LdrInitializeThunk` → `!process 0 0 test.exe`
3. 멈춤점
   - 커널: `bp /p <test> nt!PspUserThreadStartup ".echo ...; ? @$thread; k 6; g"`
   - 유저: `ba e1 ntdll!LdrInitializeThunk ".if (@$proc == <test>) {.echo ...; ? @$thread; k 6; g} .else {gc}"`
4. 로그: `run4-thread-start.log`

## 결과

| 순번 | 커널/유저 | 스레드 | 함수 | 부른 곳 | 표시 |
|---|---|---|---|---|---|
| 1 | 커널 | `ffff908f943f4080` | `nt!PspUserThreadStartup` | `nt!KiStartUserThread+0x28` | [26100.9457 확인] |
| 2 | 유저 | `ffff908f943f4080` | `ntdll!LdrInitializeThunk` | (부른 곳 없음) | [26100.9457 확인] |
| 3 | 커널 | `ffff908f946db080` | `nt!PspUserThreadStartup` | `nt!KiStartUserThread+0x28` | [26100.9457 확인] |
| 4 | 유저 | `ffff908f946db080` | `ntdll!LdrInitializeThunk` | (부른 곳 없음) | [26100.9457 확인] |
| 5 | 커널 | `ffff908f89b75080` | `nt!PspUserThreadStartup` | `nt!KiStartUserThread+0x28` | [26100.9457 확인] |
| 6 | 유저 | `ffff908f89b75080` | `ntdll!LdrInitializeThunk` | (부른 곳 없음) | [26100.9457 확인] |
| 7 | 커널 | `ffff908f89b74080` | `nt!PspUserThreadStartup` | `nt!KiStartUserThread+0x28` | [26100.9457 확인] |
| 8 | 유저 | `ffff908f89b74080` | `ntdll!LdrInitializeThunk` | (부른 곳 없음) | [26100.9457 확인] |

커널 쪽 콜스택 (4번 모두 같은 모양):

```
00  nt!PspUserThreadStartup
01  nt!KiStartUserThread+0x28
02  nt!KiStartUserThreadReturn
03  ntdll!RtlUserThreadStart        ← 유저 스택 쪽
```

## 확인된 것

- `ntdll!LdrInitializeThunk` = `00007ffd96341420` (이번 부팅 기준 주소)
- test.exe 스레드는 **4개**가 출발했다.
- 스레드마다 **커널 `PspUserThreadStartup` → 유저 `LdrInitializeThunk`** 순서. 같은 스레드가 그대로 넘어간다.
- `LdrInitializeThunk`는 첫 스레드만이 아니라 **모든 새 스레드**에서 불린다.
- `LdrInitializeThunk`의 콜스택엔 부른 함수가 없다 → 누가 `call`로 부른 게 아니라, 커널에서 넘어오면서 바로 여기서 시작한다.
- 첫 스레드(`943f4080`)는 EPROCESS(`943f5080`) 바로 옆 주소다.

## 아직 확인 필요

- **스레드가 왜 4개인가.** sample 코드로는 메인 + `CreateThread` 1개 = 2개를 예상했다.
  - 나머지 2개는 ntdll이 DLL을 나눠서 불러오려고 만드는 도우미 스레드일 가능성 [확인 필요]
  - 다음: 각 스레드의 시작 주소(Win32 Start Address)로 누가 누군지 구분
- 커널이 유저 출발점을 `LdrInitializeThunk`로 어떻게 정하는지, 콜스택의 `RtlUserThreadStart`와는 어떤 관계인지 [확인 필요]

## 디버거 메모

- 이 WinDbg에선 **`ba`에 `/p`를 못 붙인다** (Syntax error).
  - 실패할 때 주소 `0`, 꺼진 상태(`d`)의 찌꺼기 멈춤점이 남는다 → `bl`로 보고 `bc`로 지울 것.
  - 대안: `".if (@$proc == <EPROCESS>) {...; g} .else {gc}"` — 정상 동작 확인.
- `ntdll`은 cmd 쪽에서 `.reload /user` 해도 test.exe 쪽 주소와 같아서 그대로 쓸 수 있었다.
- test.exe 끝난 뒤 cmd가 `NtCreateUserProcess`를 한 번 더 불러 STOP 멈춤점에 다시 걸렸다. [원인 확인 필요]
  - 다음부터는 test.exe 주소를 얻은 뒤 STOP 멈춤점을 지운다.
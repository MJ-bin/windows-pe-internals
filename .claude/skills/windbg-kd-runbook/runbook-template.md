# runbook 양식 — `_workspace/dast/NN_키워드_runbook.md`

- 사람이 VM 앞에서 **위에서부터 한 줄씩** 따라 하는 문서다. 설명은 짧게, 명령은 정확하게.
- 명령 블록 안에는 **영어만** (P2). 바꿔 넣을 값은 `<CMD>`, `<TEST>` 처럼 꺾쇠로.
- 단계마다 네 칸: **할 일 / 명령 / 정상이면 / 이상하면**.
- 값을 넣어야 하는 단계는 "체크포인트"로 표시한다. 사용자가 그 출력을 붙여 주면 값을 채운 명령을 다시 준다.

````markdown
# NN 키워드 — 실행 안내

> 로그 붙여넣을 곳: 서버 `ms/logs/NN_키워드.txt` (지금 빈 파일)
> 디버거 VM 로그 경로: `C:\logs\NN_키워드.txt` (처음이면 디버거 VM cmd에서 `mkdir C:\logs`)
> 재부팅: 필요 없음 · 예상 시간: 약 N분
> 이번에 보는 것: (요청서 "확인할 것" 한 줄 요약)

## 0. 준비
- [ ] 타깃 VM: cmd 창을 열고 `cd C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin`. **test.exe는 아직 실행하지 않는다.**
- [ ] 디버거 VM: WinDbg 커널 디버깅이 붙어 있다 (지난 run처럼 연결 메시지가 보이고, Break 하면 `kd>` 프롬프트가 나온다).

## 1. 디버거 VM — 멈추고 로그 시작
- 할 일: WinDbg에서 **Break** (Debug → Break, 또는 Ctrl+Break). 그다음 아래 명령.
```
.logopen C:\logs\NN_키워드.txt
.reload
bc *
bl
!process 0 0 cmd.exe
```
- 정상이면: `bl` 출력이 비어 있다. cmd.exe 목록이 나온다.
- 체크포인트: cmd.exe가 둘 이상이면 출력을 붙여 주세요. 사용자가 연 cmd를 같이 고른다 (P5).

## 2. cmd에 멈춤점 → test.exe 실행
```
bp /p <CMD> nt!PspCallProcessNotifyRoutines
g
```
- 할 일: **타깃 VM** cmd에서 `test.exe` 실행.
- 정상이면: 디버거가 `Breakpoint 0 hit` 으로 멈춘다.
- 이상하면: 안 멈추고 test.exe가 끝까지 돌았다 → `<CMD>` 주소가 틀렸다. Break → `bl` 붙여 주세요.

## 3. test.exe 주소 얻기
```
!process 0 0 test.exe
bc 0
.reload /user
```
- 체크포인트: `PROCESS ffff....` 줄을 붙여 주세요. 이 값이 `<TEST>`.
- `bc 0`: cmd 쪽 멈춤점은 여기서 지운다 (P13).

## 4. 이번 run의 멈춤점  ← 여기를 요청서에 맞게 채운다
(지나가는 곳 = 기록만, 멈추는 곳 = 멈춤)
```
bp /p <TEST> ntdll!LdrpInitializeProcess+0x1bb6
bp /p <TEST> <이번 멈춤점> ".echo ==== <NAME>; ? @$thread; k 10; g"
g
```
- 정상이면: `LdrpInitializeProcess+0x1bb6`(BEFORE-GRAPH)에서 멈춘다. 그 전 기록이 `==== ...`로 찍혀 있다.

## 5. BEFORE-GRAPH에서 DLL 멈춤점 (필요할 때만)
```
.reload /user
lm m dll*
ba e1 dll2!DllMain ".if (@$proc == <TEST>) {.echo ==== dll2!DllMain; r rdx; k 6; g} .else {gc}"
bp /p <TEST> test!main
g
```
- DLL은 이름으로 건다. 고정 주소 금지 (P9).

## 6. 끝 (`test!main`에서 멈춘 뒤)
```
bc *
.logclose
g
```
- 할 일: 디버거 VM의 `C:\logs\NN_키워드.txt` 내용을 **전부** 서버 `ms/logs/NN_키워드.txt`에 붙여넣기 → "로그 붙였어"라고 말하기.

## 자주 하는 실수 (이번 run 관련)
- P1 Break 먼저, 그다음 `.logopen`
- P7 `ba`에는 `/p`를 못 붙인다 → `.if (@$proc == <TEST>)`
- (이번 계획에 해당하는 것만)
````

## 기록 명령 고르기

| 보고 싶은 것 | 명령 (조건 명령 안에) |
|---|---|
| 누가 불렀나 | `k 6` (깊이 필요하면 `k 10`, 재귀면 `k 20`) |
| 어느 스레드 | `? @$thread` |
| 부를 함수 / 이유 | `ln @rcx; r r8` (`LdrpCallInitRoutine`) |
| DllMain·TLS 이유 | `r rdx` |
| 어떤 모듈이 올라왔나 | `lm m <이름>*` (멈춘 상태에서) |
| 그 자리 call 한 줄 | `ub <caller+off> L1` |

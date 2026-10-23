# 01_sprint에서 겪은 함정 (run1 ~ run8)

runbook을 만들 때 이번 계획에 해당하는 것만 골라 넣는다. 번호는 바꾸지 않는다 (판정 문단에서 "P7" 처럼 부른다).

| # | 함정 | 증상 | 이렇게 한다 | 처음 겪은 곳 |
|---|---|---|---|---|
| P1 | `.logopen`을 Break 전에 입력 | 명령이 안 먹거나 로그 앞부분이 빔 | **Break 먼저**, 그다음 `.logopen` | 셋업 |
| P2 | 명령에 한글 | 글자 깨짐 | 명령·`.echo` 문구 모두 영어 | run1~3 |
| P3 | 끝 표시를 공용 출구(`@$ra` = `nt!KiSystemServiceCopyEnd+0x28`)에 | 다른 스레드가 먼저 밟아 기록이 일찍 끝남 (run1 빈 로그) | 끝은 `test!main` 멈춤점 또는 손 Break | run1 |
| P4 | 필터 없는 커널 멈춤점 | svchost·EdgeUpdate 등으로 계속 멈춤, test.exe 실행할 틈이 없음 | `bp /p <EPROCESS>`. 지나가는 곳은 `.echo ...; g`. **특히 조심할 함수 `[정적]`, 관찰 전**: `nt!PspMapSystemDll` (caller 4곳 — 부팅 때 `PspInitPhase3`·`PsLocateSystemDlls` 쪽에서도 불림. 새 프로세스만 보려면 `nt!PsMapSystemDlls` 입구나 `nt!MiMapProcessExecutable+0xdc`, sast/04), `nt!KiSchedulerApc`·`nt!KeResumeThread`·`nt!KeReadyThread` (스레드마다 아주 자주 불림 — 조건 필수, sast/08), `nt!PspInitializeThunkContext` (모든 프로세스의 모든 유저 스레드가 출발할 때 거침, sast/11) | run2 (예: sast/04·08) |
| P5 | 프로세스를 이름으로 고름 | cmd.exe가 둘 (하나는 부모 svchost) | **EPROCESS 주소**로 건다. `!process 0 0 cmd.exe`에서 사용자가 연 cmd를 고른다 | run2 |
| P6 | `/t` 필터 | 걸렀는지 확인 못 함 | `/t`는 안 쓴다. `/p`만 | run1 |
| P7 | `ba`에 `/p` | `Syntax error` + 주소 0, 꺼진(`d`) 찌꺼기 멈춤점 | `ba e1 <addr> ".if (@$proc == <EPROC>) {...; g} .else {gc}"`. 실패했으면 `bl` 보고 그 번호 `bc` | run4 |
| P8 | `bc`만 입력 | 아무것도 안 지워짐 | `bc *` 또는 `bc <번호>` | run2 |
| P9 | DLL에 고정 주소로 `ba` | 다른 모듈에 걸림 (run7: dll1을 dll2로 잘못 기록) | 재부팅 없이도 DLL 주소는 바뀐다. **BEFORE-GRAPH에서 멈춘 뒤 이름으로** 건다 | run7 |
| P10 | 주소를 다음 부팅에 재사용 | 빗나감 | 주소는 재부팅 전까지만. test.exe·ntdll은 같은 부팅 안에선 그대로였다 | run6 |
| P11 | 아직 메모리에 안 올라온 페이지에 `bp` | 멈춤점 설정 실패 | 하드웨어 `ba e1` 사용 (CPU에 4개뿐) | run7 |
| P12 | 프로세스 종료 중 하드웨어 멈춤점 | `Single step exception - code 80000004`로 멈춤 | `test!main`에서 끝낸다. 멈췄으면 `bc *` → `g` | run7 |
| P13 | test.exe를 얻은 뒤에도 cmd 쪽 멈춤점이 남음 | test.exe가 끝난 뒤 cmd가 `NtCreateUserProcess`를 또 불러 다시 멈춤 (원인 모름) | test.exe EPROCESS를 얻으면 cmd 쪽 멈춤점을 바로 `bc <번호>` | run4 |
| P14 | `.reload /user` 안 함 | `!thread` 시작 주소, `ln`이 이름으로 안 나옴 | 멈춘 뒤 `.reload /user` 먼저 | run6 |
| P15 | Break 중 타깃 VM 화면 클릭 | 호스트까지 멈춘 것처럼 보임 | 타깃 화면은 건드리지 않는다. `Ctrl+Alt`로 입력 풀기 | 셋업 |
| P16 | VM 일시정지 순서 | 연결 끊김 | 멈출 땐 타깃 먼저, 켤 땐 디버거 먼저 | 셋업 |
| P17 | ntdll 소프트웨어 멈춤점을 남겨 둠 | 다른 프로세스에서도 ntdll 코드가 같은 페이지라 계속 트랩 → 느려짐 [추측] | 끝나면 `bc *` | run7 |
| P18 | test.exe 끝난 뒤 Break | `lm m test`가 빈칸 | 모듈 정보는 test.exe가 살아 있을 때(멈춤점 안에서) 본다 | run5 |
| P19 | **별칭 무리** 함수에 이름으로 `bp` (한 주소에 함수 이름이 여러 개 — 링커가 같은 코드를 합친 ICF) | 그 주소를 쓰는 **다른 함수들의 호출에도** 멈춤. 콜스택·`ln` 에 이름이 하나만(엉뚱한 것) 보일 수 있음 `[추측]` | runbook 만들 때 `pdb_aliases.py --lookup <모듈> <이름>` 으로 확인. 무리면 그 함수 입구 대신 **caller 의 call 자리**(`caller+call_off`)에 걸거나, 입구에 `.if (poi(@rsp) == <caller+ret_off 주소>) {...} .else {gc}` 조건. 로그의 이름이 무리면 판정 문단에 "별칭 무리"라고 적는다 | sast/03 (정적, 관찰 전) |
| P20 | 커널 → 유저로 넘어가는 `IRETQ` 줄 자체에 멈춤점 (`nt!KxStartUserThread+0x3e1`, `nt!KiKernelExit+0xa5`) | 바로 앞에서 `SWAPGS`(GS 가 유저 값)·CR3 전환을 이미 해서 디버거가 이상하게 굴 수 있음 `[추측]`, 관찰 전 | 유저로 들어가기 직전 값은 `nt!KiStartUserThread+0x28` 에서 `dq @rbp+0xe8 L5` (Rip·CS·EFlags·Rsp·SS) 로 읽는다. 유저 쪽 첫 자리는 `ntdll!LdrInitializeThunk` 입구에서 | sast/10·run15 (정적, 관찰 전) |
| P21 | 이름이 두 주소에 걸친 함수에 **이름으로** `bp` (`ntdll!LdrpInitialize` = rva `0xe1448` 과 `0x8bf8c`(`_LdrpInitialize`, 콜스택 표시 이름)) | 엉뚱한 쪽에 서거나 콜스택 이름과 멈춘 곳이 안 맞음 `[추측]`, 관찰 전 | `ntdll+<rva>` 로 건다. 먼저 `x ntdll!*LdrpInitialize`·`ln ntdll+0x8c0f8` 로 WinDbg 가 무슨 이름으로 보이는지 본다 | sast/12·run17 (정적, 관찰 전) |
| P22 | **call 자리와 ret 자리를 섞음** | 멈춤점이 한 명령 어긋나거나, 콜스택 `caller+0x..` 와 문서 라벨이 안 맞음 (run18: `LdrpInitializeProcess +0x1bb6` call / `+0x1bbb` ret) | 라벨·콜스택 비교는 `_calls.tsv` 의 ret_off, "호출 직전 값"을 볼 땐 call_off 에 건다. 명령 길이가 다르다: `CALL RAX` 2바이트(`LdrpInitializeProcess +0x17fc` → ret `+0x17fe`), `CALL rel32` 5바이트, `CALL [rip+X]` 6바이트 — 손으로 더하지 말고 `_calls.tsv` 를 본다. 확인은 `ub <caller+ret> L1` | run18·run19·run20 (정적) |
| P23 | CFG 검사 thunk 를 거치는 간접 호출의 대상을 call 줄에서 찾음 | `ln` 에 검사 함수(`__guard_dispatch_icall_fptr` 등)만 보임 | call 자리에 멈춰 `ln @rax` — 진짜 대상은 호출 직전 `rax` (예: `LdrpInitializeKernel32Functions +0x1aa` → `KERNELBASE!KernelbasePostInit` 예상) | sast/15·run20 (정적, 관찰 전) |
| P24 | **주소로만 넘겨지는 콜백** 입구에만 멈춤점 (`ntdll!LdrpWorkCallback`) | 일이 돌았는데 안 섬 — 스레드 풀이 같은 반복문 복사본을 직접 돌 수 있음 (`TppWorkpExecuteCallback +0x1b4` 에서 콜백 주소 비교) `[정적]`, 관찰 전 | 복사본 쪽 ret `ntdll+0x19050`(`TppWorkpExecuteCallback +0x2e0`)이나 그 안에서 부르는 `LdrpProcessWork` 입구(`ntdll+0x17140`)에도 같이 건다 | sast/19·run24 (정적, 관찰 전) |

## 값 읽는 법 (자주 씀)

- `LdrpCallInitRoutine`: `ln @rcx` = 부를 함수, `r r8` = 이유
- `DllMain`·TLS 콜백: `r rdx` = 이유 (1 프로세스 시작, 2 스레드 시작, 3 스레드 끝, 0 프로세스 끝)
- 콜스택 오프셋 `caller+0x..` = call 다음 명령 위치 = 정적 결과 `_calls.tsv`의 `ret_off`
- `ub <caller+off> L1` = 그 자리의 call 명령 한 줄 (BEFORE-GRAPH 오프셋 `+0x1bb6`을 이렇게 얻었다)
- 이름표만 함수 이름인 곳: `nt!KiStartUserThread` 는 Ghidra 함수 `KxStartUserThread +0x60` 의 이름표다. 그래서 `KxStartUserThread +x` 주소는 WinDbg 에 `KiStartUserThread+(x−0x60)` 로 보일 것이다 `[추측]` (예: Kx `+0x88` = `KiStartUserThread+0x28`, run4 관찰과 같은 주소, sast/10). 정적 결과의 Kx 오프셋을 멈춤점에 쓸 땐 이렇게 바꾼다.

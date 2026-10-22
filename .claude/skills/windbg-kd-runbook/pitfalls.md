# 01_sprint에서 겪은 함정 (run1 ~ run8)

runbook을 만들 때 이번 계획에 해당하는 것만 골라 넣는다. 번호는 바꾸지 않는다 (판정 문단에서 "P7" 처럼 부른다).

| # | 함정 | 증상 | 이렇게 한다 | 처음 겪은 곳 |
|---|---|---|---|---|
| P1 | `.logopen`을 Break 전에 입력 | 명령이 안 먹거나 로그 앞부분이 빔 | **Break 먼저**, 그다음 `.logopen` | 셋업 |
| P2 | 명령에 한글 | 글자 깨짐 | 명령·`.echo` 문구 모두 영어 | run1~3 |
| P3 | 끝 표시를 공용 출구(`@$ra` = `nt!KiSystemServiceCopyEnd+0x28`)에 | 다른 스레드가 먼저 밟아 기록이 일찍 끝남 (run1 빈 로그) | 끝은 `test!main` 멈춤점 또는 손 Break | run1 |
| P4 | 필터 없는 커널 멈춤점 | svchost·EdgeUpdate 등으로 계속 멈춤, test.exe 실행할 틈이 없음 | `bp /p <EPROCESS>`. 지나가는 곳은 `.echo ...; g`. **특히 조심할 함수 `[정적]`, 관찰 전**: `nt!PspMapSystemDll` (caller 4곳 — 부팅 때 `PspInitPhase3`·`PsLocateSystemDlls` 쪽에서도 불림. 새 프로세스만 보려면 `nt!PsMapSystemDlls` 입구나 `nt!MiMapProcessExecutable+0xdc`, sast/04), `nt!KiSchedulerApc`·`nt!KeResumeThread`·`nt!KeReadyThread` (스레드마다 아주 자주 불림 — 조건 필수, sast/08) | run2 (예: sast/04·08) |
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

## 값 읽는 법 (자주 씀)

- `LdrpCallInitRoutine`: `ln @rcx` = 부를 함수, `r r8` = 이유
- `DllMain`·TLS 콜백: `r rdx` = 이유 (1 프로세스 시작, 2 스레드 시작, 3 스레드 끝, 0 프로세스 끝)
- 콜스택 오프셋 `caller+0x..` = call 다음 명령 위치 = 정적 결과 `_calls.tsv`의 `ret_off`
- `ub <caller+off> L1` = 그 자리의 call 명령 한 줄 (BEFORE-GRAPH 오프셋 `+0x1bb6`을 이렇게 얻었다)

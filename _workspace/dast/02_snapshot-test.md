# 02 snapshot-test — 동적 분석 요청

- 날짜: 2026-10-25
- 스프린트: 03_sprint / 예정 run: 따로 안 닫음 (Q77a 시험의 둘째 판. 결과는 Q77 본 바퀴 run 문서에 합침)
- 대기열 항목: Q77 (짐 지도 B — 사진) 의 앞부분 **Q77a 시험 덤프**
- 관련: `_workspace/dast/01_snapshot-test.md` (요청서), `_workspace/dast/01_snapshot-test_runbook.md`, `ms/logs/01_snapshot-test_result.md` (분석성공)
- 재시도: **dast/01 은 분석성공이었지만 스냅샷 파일을 쓸 수 없었다** — 타깃 VM 이 (가상 TPM 때문에) 부분 암호화돼 있어 `vmss2core -W8` 가 `Snapshot4.vmsn` 을 "Unrecognized .vmss file (magic 8943dd9e)" 로 거부 `[관찰]`. 사용자 실수 아님.

## 그 사이 바뀐 것 (2026-10-25, 사용자 결정·실행, plan.md 결정 기록)

- 덤프 읽기 방식 = **C**: VM 에선 스냅샷만 → 서버 `vmss2core` 로 `.dmp` → 디버거 VM WinDbg 로 열어 읽음.
- 타깃 `bob_baseline_vm`: 가상 TPM 제거 → 스냅샷 4개 삭제 → 암호화 해제 (사용자). 전에 폴더 통째 백업 `/home/mjbin/vmware/bob_baseline_vm.bak-20261025/`.
  - 오케스트레이터 확인 `[관찰]`: `.vmx` 에 encrypt/keySafe/vtpm 줄 없음, `.vmsd` 스냅샷 0개, `.nvram` 머리 `MRVN`.
  - 그래서 이번 스냅샷이 **새 `Snapshot1`** 로 생길 수 있다 (번호가 다시 1부터일 수 있음 `[추측]`). 시각으로 가린다.
- 세팅 (S0~S7) 은 dast/01 에서 끝났다. KDNET: 타깃 `192.168.100.133` → 디버거 `192.168.100.130`, port 50000, key 는 dast/01 때 새로 만든 것 (타깃 디스크 BCD 에 있음, 스냅샷 삭제·암호화 해제로 안 바뀜). **이번 runbook 은 세팅 단계 없이**, "타깃 켜기 → WinDbg 연결 확인" 한 단계만 앞에 둔다.
  - TPM 이 빠져서 Windows 로그인이 PIN 대신 비밀번호를 물을 수 있다 `[추측]`.
  - 디버거 IP 가 DHCP 로 바뀌었으면 연결이 안 된다 → 그때만 디버거 `ipconfig` 와 타깃 `bcdedit /dbgsettings` 를 보게 한다.
- dast/01 판정이 지적한 runbook 잘못 두 가지를 고친다:
  - `!vtop` 은 `@$peb`·심볼 이름을 못 받는다 → **숫자 VA** 로 친다. 숫자는 바로 앞 `? @$peb` 출력에서 사용자가 옮기거나, 오케스트레이터가 채워 준다.
  - 멈춘 동안 WinDbg `.time` 은 안 바뀐다 → 스냅샷 시간은 **서버에서 `date +%T.%N`** 으로 잰다 (VMware 메뉴 누르기 직전·진행 표시 끝난 직후). 사용자가 서버 터미널에서 치거나, 오케스트레이터가 대신 친다.
- 사용자 요청으로 실행 중 안내는 **오케스트레이터가 runbook 대로 직접** 한다. 서버 쪽 읽기·복사 명령은 오케스트레이터가 서버에서 실행해도 된다 (runbook 에 "서버 쪽 — 오케스트레이터가 실행 가능" 이라고 표시).

## 확인할 것 (정적으로 못 본 것)

1. 암호화를 푼 타깃에서 ① `ntdll!LdrInitializeThunk` 입구 (test.exe 첫 스레드) 에 섰을 때 찍은 스냅샷을 서버 `vmss2core` 가 WinDbg `.dmp` 로 바꿀 수 있나.
2. 바꾼 `.dmp` 를 디버거 VM WinDbg 로 열었을 때, **커널과 test.exe 유저 메모리 둘 다** 읽히나 — 이번 실행에서 라이브로 찍은 대조값과 같은 값이 나오나.
3. 스냅샷 파일 크기·찍는 시간, 변환 시간·`.dmp` 크기.

## 멈출 곳 후보

| 지점 | 멈출지 / 기록만 | 찍을 것 | 근거 |
|---|---|---|---|
| (기준점) cmd 의 `nt!PspCallProcessNotifyRoutines` (`bp /p <CMD>`) | 멈춤 | test.exe EPROCESS | dast/01 1~4단계 그대로 |
| ① `ntdll!LdrInitializeThunk` 입구, `bp /p <TEST>` | 멈춤 | 대조값 (아래) → 스냅샷 | dast/01 5~7단계 |

### 대조값 (라이브, 스냅샷 전) — dast/01 6단계와 같되 `!vtop` 만 숫자 VA

- `!process -1 0`, `!thread -1 0`, `? @$proc`/`@$thread`/`@$teb`/`@$peb`, `k 5`, `r`, `r cr3`
- `lm m ntdll`, `db ntdll!LdrInitializeThunk L10`
- `dt nt!_EPROCESS @$proc UniqueProcessId Peb ImageFileName SectionBaseAddress`, `dt nt!_PEB @$peb ImageBaseAddress Ldr ProcessHeap ProcessParameters`, `dt nt!_CONTEXT @rcx ContextFlags Rip Rsp Rcx`, `dq @$peb L4`
- `!vtop 0 <PEB 숫자>`, `!vtop 0 <LdrInitializeThunk 숫자>`

### 스냅샷 → 변환 → 오프라인 열기

1. 스냅샷 (호스트 VMware, `bob_baseline_vm` 만, 이름 `02_snapshot-test`). 앞뒤 서버 `date +%T.%N`.
2. 끝: `bc *` → `.logclose` → `g` (test.exe 끝까지). 라이브 로그는 `C:\logs\02_snapshot-test.txt` → 서버 `ms/logs/02_snapshot-test.txt`.
3. 서버 (오케스트레이터 실행 가능): 새 `-SnapshotN.vmsn`/`.vmem` 을 `ms/dump/02_snapshot-test/` 로 `cp`, 그 폴더에서 `vmss2core -W8 <vmsn> <vmem>` → `memory.dmp`. 시간·크기 기록. `-W8` 이 안 되면 `-W` / `-WSCAN` 등 다른 옵션을 시도할지 dast-mentor 가 runbook 에 순서로 적는다.
4. `.dmp` 를 디버거 VM 으로 옮기는 길: 디버거 VM `Windows 11 x64` 에는 공유 폴더가 없다 → VMware 설정에서 공유 폴더 하나를 켜는 법을 안내한다 (호스트 경로 예: `/home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump`, 읽기 전용이면 충분). 이건 디버거 VM 설정 변경이라 사용자가 한다. 다른 VM (`WIN11-26H1-HDAUDIO`) 은 건드리지 않는다.
5. 디버거 VM 에서 **새 WinDbg 창** (커널 세션과 별개) → Open dump file → `memory.dmp`. 그다음 오프라인 대조:
   - `!process 0 0 test.exe` → EPROCESS 가 라이브 값과 같나
   - `.process /p /r <EPROCESS>` 로 test.exe 문맥 → `dt nt!_PEB <PEB> ImageBaseAddress Ldr ProcessHeap ProcessParameters`, `dq <PEB> L4`, `db <LdrInitializeThunk 주소> L10` → 라이브 값과 같나
   - `!thread <ETHREAD> 0`, `r` (덤프의 CPU 문맥이 ① 그대로인지 — 어느 CPU 가 test.exe 를 돌았는지는 라이브 `!thread` 의 processor 번호, `~<n>s` 로 바꿈)
   - 오프라인 로그 `C:\logs\02_snapshot-test_offline.txt` → 서버 `ms/logs/02_snapshot-test_offline.txt` (dast-mentor 가 빈 파일 같이 만든다)

## 성공 기준

- 라이브 로그에 ① 에서 test.exe 로 섰고 대조값 (EPROCESS·ETHREAD·PEB·TEB·cr3·ntdll 베이스·PEB 칸·`dq @$peb`·입구 바이트·`!vtop` 물리 주소) 이 있다.
- 서버에서 `vmss2core` 가 `memory.dmp` 를 만들었다 (명령·출력·크기·시간 기록).
- 오프라인 로그에서 `.dmp` 의 test.exe EPROCESS, PEB 칸 값, `dq <PEB> L4`, 입구 바이트가 라이브 대조값과 같다 (= 커널·유저 둘 다 읽힘).
- 변환이 안 되거나 오프라인 값이 다르면 → 예기치못한상황발생으로 판정하고 무엇이 달랐는지 적는다.

## 끝 지점

- 라이브: ① 스냅샷 뒤 `bc *` → `.logclose` → `g`. 다른 지점에서는 멈추지 않는다.
- 오프라인: 대조 명령까지 치고 `.logclose`.

## 범위 밖

- ② `NtContinue` 와 ⓪·①-a·①-b — 이번 결과로 C 방식이 되는지 본 뒤 Q77b 요청서에서.
- 구조체 전체 덤프 명령 묶음 (Q77b 에서).
- 하네스 (스킬 pitfalls·환경 표) 고치기 — 오케스트레이터가 사용자 검토 뒤 따로.

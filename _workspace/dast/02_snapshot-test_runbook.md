# 02 snapshot-test — 실행 안내

> 로그 붙여넣을 곳 (둘 다 지금 빈 파일):
> - 라이브 (커널 디버깅): 서버 `ms/logs/02_snapshot-test.txt` ← 디버거 VM `C:\logs\02_snapshot-test.txt`
> - 오프라인 (덤프 열기): 서버 `ms/logs/02_snapshot-test_offline.txt` ← 디버거 VM `C:\logs\02_snapshot-test_offline.txt`
> 재부팅: 필요 없음 · 예상 시간: 연결 확인 5분 + 라이브 15분 + 스냅샷·변환 몇 분~십여 분 + 오프라인 15분
> 이번에 보는 것: 암호화를 푼 타깃에서 ① 스냅샷 → 서버 `vmss2core` 로 `memory.dmp` → 디버거 VM WinDbg 로 열었을 때 **커널과 test.exe 유저 메모리가 둘 다 읽히고 라이브 값과 같은지**
> dast/01 에서 고친 것: `!vtop` 은 **숫자 VA** 로 친다 (6단계) · 스냅샷 시간은 **서버 `date +%T.%N`** 으로 잰다 (7단계)

**VM 세 대** (호스트 = 이 분석 서버, Ubuntu 22.04, VMware Workstation 17.6.3):

| 역할 | VMware 이름 | IP | 메모 |
|---|---|---|---|
| **타깃** | `bob_baseline_vm` ("BOB Windows 11 24H2") | `192.168.100.133` | 가상 TPM 뺌·암호화 해제됨. 스냅샷은 **여기만** |
| **디버거** | `Windows 11 x64` | `192.168.100.130` | WinDbg 깔려 있음 (dast/01). 이번에 공유 폴더 하나를 켠다 (9단계) |
| **건드리지 않음** | `WIN11-26H1-HDAUDIO` | — | 다른 작업. 스냅샷·`vmrun`·설정 변경 금지 |

**누가 치나**: `[사용자]` = VM 안 / VMware 메뉴. `[서버 — 오케스트레이터가 실행 가능]` = 서버 터미널 명령. 오케스트레이터가 대신 쳐도 되고, 사용자가 쳐도 된다.

멈추는 곳: (기준점) cmd 의 `nt!PspCallProcessNotifyRoutines` → ① `ntdll!LdrInitializeThunk` 입구 (test.exe 일 때만). ① 에서 대조값 → 스냅샷.

값 이름 (라이브에서 얻어 뒤에서 씀 — 오케스트레이터가 채워 줄 수 있다):

| 이름 | 어디서 | dast/01 때 값 (이번엔 다름, 모양 참고용) |
|---|---|---|
| `<CMD>` | 1단계 cmd.exe EPROCESS | `ffffa985cfe0a080` |
| `<TEST>` | 4단계 test.exe EPROCESS | `ffffa985d4d840c0` |
| `<THUNKVA>` | 3단계 `x ntdll!LdrInitializeThunk` 주소 | `7ff88b2e1420` |
| `<ETHREAD>` · `<CPU>` | 6단계 `!thread -1 0` 의 `THREAD` 값 · `RUNNING on processor N` 의 N | `ffffa985d5cd80c0` · `1` |
| `<PEBVA>` | 6단계 `? @$peb` 출력 | `53fd452000` |

- 숫자를 옮길 때 가운데 **역따옴표(`` ` ``)는 빼고** 친다. 예: ``00000053`fd452000`` → `53fd452000`.

---

## 0. 타깃 켜기 → 연결 확인

켜는 순서: **디버거 먼저, 타깃 나중** (P16).

1. `[사용자]` 디버거 VM `Windows 11 x64` 가 켜져 있고 WinDbg 가 커널 연결 대기 중인지 본다. 아니면 WinDbg → File → Attach to kernel → **Net** → Port `50000`, Key = dast/01 때 만든 key → OK.
   - key 를 모르면: 타깃이 켜진 뒤 타깃 **관리자 cmd** 에서 `bcdedit /dbgsettings` 로 본다.
2. `[사용자]` VMware 에서 **`bob_baseline_vm`** 을 켠다 (Power on). 로그인.
   - TPM 을 뺐기 때문에 PIN 대신 **비밀번호**를 물을 수 있다 `[추측]`.
3. 디버거 VM WinDbg 창을 본다.
- 정상이면: `Connected to target 192.168.100.133 on port 50000 on local IP 192.168.100.130` 과 `Kernel Debugger connection established.` 가 뜬다.
- 바탕화면까지 올라오면 `[사용자]` WinDbg 에서 **Break** → `kd>` 에서:
```
vertarget
g
```
- 정상이면: `vertarget` 에 `26100` 이 보이고, `g` 뒤 타깃이 다시 움직인다. Break 동안 타깃 화면은 클릭하지 않는다 (P15).
- 이상하면 (체크포인트): 몇 분 지나도 `Waiting to reconnect...` 만 → 디버거 VM 에서 `ipconfig | findstr IPv4` (IP 가 `.130` 인지), 타깃 관리자 cmd 에서 `bcdedit /dbgsettings` (hostip 가 `192.168.100.130`, port `50000` 인지) 를 붙여 주세요.

## 0-C. 타깃 — cmd 준비 (test.exe 는 아직 실행하지 않는다)

`[사용자]` 타깃에서 **보통 cmd** 를 새로 열고:
```
ver
ipconfig | findstr IPv4
cd C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin
dir test.exe
title pesnap
tasklist /v /fi "imagename eq cmd.exe"
```
- 정상이면: `ver` = `10.0.26100.9457`, IP = `192.168.100.133`, `test.exe` 가 있다. 창 제목이 `pesnap` 인 줄의 **PID** (10진수) 를 적는다 = `<CMDPID>`.

## 0-D. 서버 — 스냅샷 전 모습 남기기 `[서버 — 오케스트레이터가 실행 가능]`
```
vmrun -T ws list
ls -l --time-style=full-iso /home/mjbin/vmware/bob_baseline_vm/
command -v vmss2core
df -h /home/mjbin
```
- 정상이면: `vmrun` 목록에 `/home/mjbin/vmware/bob_baseline_vm/BOB Windows 11 24H2.vmx` 가 있다. `-Snapshot*` 파일은 없을 것이다 (스냅샷 다 지움). `vmss2core` 경로가 나온다.

---

## 1. 디버거 VM — 멈추고 로그 시작

`[사용자]` WinDbg 에서 **Break**. `kd>` 에서:
```
.logopen C:\logs\02_snapshot-test.txt
.echo ==== STEP1 SETUP
.sympath
.reload
!lmi nt
bc *
bl
!process 0 0 cmd.exe
? 0n<CMDPID>
```
- 정상이면: `!lmi nt` GUID `C29EBFB0-6B78-B3C0-20DC-A66D99713F9E` Age `1`. `bl` 비어 있음. `? 0n<CMDPID>` 의 16진수와 **`Cid` 가 같은 줄**의 `PROCESS ffff....` = `<CMD>`.
- 체크포인트: 헷갈리면 출력 붙여 주세요 (P5).

## 2. cmd 에 멈춤점 → test.exe 실행
```
bp /p <CMD> nt!PspCallProcessNotifyRoutines
bl
g
```
- `[사용자]` 타깃 `pesnap` cmd 에서 `test.exe` 실행.
- 정상이면: `Breakpoint 0 hit` / `nt!PspCallProcessNotifyRoutines`.
- 이상하면: 안 멈추고 끝까지 돌면 `<CMD>` 가 틀림 → Break → `bl` 붙여 주세요.
- (dast/01 때처럼 중간에 `Break instruction exception` 이 한 번 끼면 그냥 `g`.)

## 3. 이름 확인
```
.echo ==== STEP3 NAMES
.reload /user
x nt!PspCallProcessNotifyRoutines
x ntdll!LdrInitializeThunk
lm m ntdll
? ntdll!LdrInitializeThunk - ntdll
```
- 정상이면: 마지막 줄 `00000000'000e1420`. `x ntdll!LdrInitializeThunk` 의 주소 = `<THUNKVA>` (역따옴표 빼고 적어 둔다).

## 4. test.exe 주소 → cmd 쪽 멈춤점 지우기
```
.echo ==== STEP4 TEST
!process 0 0 test.exe
bc 0
bl
```
- 정상이면: `PROCESS ffff.... Image: test.exe` 한 줄 = `<TEST>`. `bl` 비어 있음 (P13).

## 5. ① 멈춤점
```
bp /p <TEST> ntdll!LdrInitializeThunk
bl
g
```
- 정상이면: `Breakpoint 0 hit` / `ntdll!LdrInitializeThunk:`.

## 6. ① 에서 — 대조값 (스냅샷 **전에**)

한 블록씩 친다.
```
.echo ==== STOP1 LdrInitializeThunk
!process -1 0
!thread -1 0
? @$proc
? @$thread
? @$teb
? @$peb
k 5
r
r cr3
```
- 여기서 `<ETHREAD>`·`<CPU>` (`!thread -1 0` 줄), `<PEBVA>` (`? @$peb`) 를 적는다.
```
.echo ==== STOP1 MODULES
lm m ntdll
lm m test
? ntdll!LdrInitializeThunk - ntdll
db ntdll!LdrInitializeThunk L10
```
```
.echo ==== STOP1 FIELDS
dt nt!_EPROCESS @$proc UniqueProcessId Peb ImageFileName SectionBaseAddress
dt nt!_PEB @$peb ImageBaseAddress Ldr ProcessHeap ProcessParameters
dt nt!_CONTEXT @rcx ContextFlags Rip Rsp Rcx
dq @$peb L4
```
**`!vtop` 은 숫자로** (dast/01 에서 `@$peb`·심볼 이름은 `usage:` 만 나옴). `<PEBVA>`·`<THUNKVA>` 를 숫자로 바꿔서:
```
.echo ==== STOP1 VTOP
!vtop 0 <PEBVA>
!vtop 0 <THUNKVA>
.echo ==== STOP1 DONE
```
- 정상이면: `!process -1 0` = `test.exe`, `rip` = `<THUNKVA>`, `!vtop` 두 줄 다 `translates to physical address ...`. `lm m test` 빈칸·`Ldr`·`ProcessHeap` `(null)` 은 정상 (dast/01 과 같음).
- 이상하면: `Image:` 가 test.exe 가 아니면 멈추고 붙여 주세요. **스냅샷 찍지 않는다.** `!vtop` 이 또 `usage:` 면 숫자에 역따옴표가 남았는지 본다.

## 7. ① 에서 — 스냅샷 (디버거는 멈춘 그대로)

**타깃 화면 안은 클릭하지 않는다** (P15). VMware 메뉴만. 입력이 잡혀 있으면 `Ctrl+Alt`.

0. `[사용자]` **직전 확인** — 하나라도 아니면 찍지 않는다.
   - [ ] 고른 탭이 **`bob_baseline_vm`** ("BOB Windows 11 24H2").
   - [ ] `WIN11-26H1-HDAUDIO` 도 `Windows 11 x64` (디버거) 도 아니다.
   - [ ] 그 화면에 `pesnap` cmd 창이 보인다 (굳어 있음, 보기만).
1. **시작 시각** `[서버 — 오케스트레이터가 실행 가능]` — 사용자가 "지금 누른다" 고 말하기 직전에:
```
date +%T.%N
```
   (WinDbg `.time` 은 멈춘 동안 안 바뀐다 — dast/01. 이번엔 안 쓴다.)
2. `[사용자]` VMware → `bob_baseline_vm` 탭 → VM → Snapshot → **Take Snapshot...** → 이름 `02_snapshot-test` → 진행 표시가 끝날 때까지 기다린다. 끝나면 "끝났다" 고 말한다. `vmrun snapshot` 은 쓰지 않는다.
3. **끝 시각** `[서버 — 오케스트레이터가 실행 가능]` — "끝났다" 직후:
```
date +%T.%N
```
4. 새 파일 `[서버 — 오케스트레이터가 실행 가능]`:
```
ls -l --time-style=full-iso /home/mjbin/vmware/bob_baseline_vm/
du -h /home/mjbin/vmware/bob_baseline_vm/*-Snapshot*
grep -i snapshot /home/mjbin/vmware/bob_baseline_vm/*.vmsd
```
- 정상이면: 방금 시각의 `BOB Windows 11 24H2-Snapshot<N>.vmsn` 과 `.vmem` 이 생겼다. 번호 `<N>` 은 1 부터 다시일 수 있다 `[추측]` — **시각**과 `.vmsd` 의 `02_snapshot-test` 항목으로 가린다.
- 이상하면: 연결 끊김·재연결 메시지가 WinDbg 에 나오면 그대로 두고 `? @$proc` 한 번.

## 8. 라이브 끝 — 멈춤점 지우고 test.exe 보내기

`[사용자]` WinDbg 에서:
```
.echo ==== END
bc *
bl
.logclose
g
```
- 정상이면: 타깃 cmd 에서 test.exe 가 끝나고 프롬프트가 돌아온다.
- 이상하면: `Single step exception` 으로 또 멈추면 `bc *` → `g` (P12).
- `[사용자]` `C:\logs\02_snapshot-test.txt` 를 **전부** 서버 `ms/logs/02_snapshot-test.txt` 에 붙인다 (오프라인 단계 전에 해도 되고 뒤에 해도 됨).
- 스냅샷은 되돌리지(Revert)도 지우지도 않는다.

## 9. 서버 — 복사 → `vmss2core` 변환 `[서버 — 오케스트레이터가 실행 가능]`

`<N>` 은 7단계 값.
```
mkdir -p /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/02_snapshot-test
cd /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/02_snapshot-test
time cp --sparse=always "/home/mjbin/vmware/bob_baseline_vm/BOB Windows 11 24H2-Snapshot<N>.vmsn" "/home/mjbin/vmware/bob_baseline_vm/BOB Windows 11 24H2-Snapshot<N>.vmem" .
ls -l
time vmss2core -W8 "BOB Windows 11 24H2-Snapshot<N>.vmsn" "BOB Windows 11 24H2-Snapshot<N>.vmem"
ls -l
```
- 정상이면: `vmss2core` 가 끝에 성공 줄을 내고 폴더에 `memory.dmp` 가 생긴다. `time` 두 번(복사·변환)과 `ls -l` (`.dmp` 크기)를 기록한다.
- 이상하면 — 아래 순서로 **하나씩** 해 보고, 각 출력을 기록한다:
  1. `-W8` 가 실패 → 같은 폴더에서 `time vmss2core -W "...Snapshot<N>.vmsn" "...Snapshot<N>.vmem"` (옛 Windows 형식).
  2. 그래도 실패 → `vmss2core` (옵션 없이) 로 사용법 목록을 찍어 기록. 다른 옵션은 거기서 고른다 — **여기서 멈추고** dast-mentor 에게 출력 전달.
  3. 다시 `Unrecognized .vmss file (magic ...)` 가 나오면 → 암호화가 아직 남아 있는 것일 수 있다 `[추측]`. 멈추고 `.vmx` 의 `encrypt`·`vtpm` 줄을 다시 확인.
- 이 폴더는 git 에 안 올라간다 (`ms/dump/` 은 `.gitignore`). `git check-ignore -v memory.dmp` 로 한 번 본다.

## 10. 디버거 VM 에 공유 폴더 켜기 `[사용자]` (VM 설정 변경)

**`Windows 11 x64` 에만** 한다. 다른 VM 은 건드리지 않는다.

1. VMware → **`Windows 11 x64`** 탭 → VM → Settings → **Options** → **Shared Folders**.
2. **Always enabled** 고르기 → **Add...** →
   - Host path: `/home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump`
   - Name: `dump`
   - **Read-only** 켜기 → Finish → OK.
   - 켜진 채로 바꿀 수 있다 `[추측: Workstation 기본]`. 안 되면 출력/화면 붙여 주세요.
3. 디버거 VM 의 cmd 에서 복사:
```
mkdir C:\dump
dir "\\vmware-host\Shared Folders\dump\02_snapshot-test"
copy "\\vmware-host\Shared Folders\dump\02_snapshot-test\memory.dmp" C:\dump\02_memory.dmp
dir C:\dump
```
- 정상이면: `memory.dmp` 가 보이고 복사 뒤 크기가 9단계 `ls -l` 과 같다.
- 이상하면: `\\vmware-host` 가 안 열리면 VMware Tools 가 없거나 꺼져 있을 수 있다 `[추측]`. 탐색기 주소창에 `\\vmware-host\Shared Folders` 를 쳐 보고 화면을 알려 주세요.

## 11. 오프라인 — 덤프 열기 (커널 세션과 **별개** WinDbg 창)

커널 세션 WinDbg 는 그대로 두고 (닫아도 되지만 굳이), **WinDbg 를 하나 더** 켠다.

1. `[사용자]` 새 WinDbg → File → **Open dump file** → `C:\dump\02_memory.dmp`.
2. 로딩이 끝나면 (`kd>` 또는 `0: kd>` 프롬프트):
```
.logopen C:\logs\02_snapshot-test_offline.txt
.echo ==== OFF1 OPEN
.sympath
.reload
!lmi nt
vertarget
!process 0 0 test.exe
```
- 정상이면: `!lmi nt` GUID 가 라이브와 같다. `!process 0 0 test.exe` 의 `PROCESS` 값이 라이브 `<TEST>` 와 같다.
- 이상하면: 파일을 못 연다·`!process` 가 빈칸 → 멈추고 붙여 주세요.

3. test.exe 문맥으로 바꿔 유저 메모리 읽기. `<TEST>`·`<PEBVA>`·`<THUNKVA>` 는 **라이브 로그 값**:
```
.echo ==== OFF2 USER
.process /p /r <TEST>
!process -1 0
dt nt!_PEB <PEBVA> ImageBaseAddress Ldr ProcessHeap ProcessParameters
dq <PEBVA> L4
db <THUNKVA> L10
!vtop 0 <PEBVA>
```
- 정상이면: 네 값 모두 라이브 6단계와 같다 — `dt nt!_PEB` 네 칸, `dq` 두 줄, `db` 16바이트, `!vtop` 물리 주소.
- `db` 가 `40 53 48 83 ...` 이면 int3 가 아닌 원래 바이트 (라이브와 같음).
- 이상하면: `????????` (읽기 실패) 가 나오면 → 그 줄 그대로 두고 다음으로 (그 자체가 결과).

4. ① 그 순간의 스레드·CPU 문맥. `<ETHREAD>`·`<CPU>` 는 라이브 6단계 값:
```
.echo ==== OFF3 THREAD
!thread <ETHREAD> 0
~<CPU>s
r
k 5
.echo ==== OFF DONE
.logclose
```
- 정상이면: `!thread` 가 같은 Cid·Teb. `~<CPU>s` 뒤 `r` 의 `rip` = `<THUNKVA>`, `rcx`·`rsp` 도 라이브 `r` 과 같다 `[추측: vmss2core 가 CPU 문맥을 넣었다면]`.
- `[사용자]` `C:\logs\02_snapshot-test_offline.txt` 를 **전부** 서버 `ms/logs/02_snapshot-test_offline.txt` 에 붙인다.

## 12. 기록 (HOST NOTES)

`[서버 — 오케스트레이터가 실행 가능]` 라이브 로그 `ms/logs/02_snapshot-test.txt` **맨 아래**에 붙인다 (모르는 칸 `?`):
```
==== HOST NOTES
target_vm: bob_baseline_vm (vTPM removed, decrypted)  memory <MB>
ips: target <ip>  debugger <ip>
snapshot_name: 02_snapshot-test (vmsd entry <...>)
snap_time: start <date> end <date>
snap_vmsn: <file name>  <bytes>
snap_vmem: <file name>  <bytes>  <du -h>
copy_time: <real>
vmss2core: <command>  <exit / last lines>  time <real>
dmp: memory.dmp  <bytes>
dmp_to_debugger: shared folder dump (read-only), copy to C:\dump\02_memory.dmp  <time if known>
debugger_issues: <none / ...>
```
그다음 "로그 붙였어".

---

## 자주 하는 실수 (이번 run 관련)

- **P1** Break 먼저, 그다음 `.logopen` (라이브). 오프라인은 덤프를 연 뒤 `.logopen`.
- **P2** 명령·`.echo` 영어만.
- **P4** 커널 멈춤점은 둘 다 `bp /p`.
- **P5** cmd 는 EPROCESS 주소로 (`<CMDPID>` → `Cid`).
- **P8** `bc 0` / `bc *` (그냥 `bc` 아님).
- **P13** test.exe 를 얻으면 바로 `bc 0`.
- **P15** 멈춘 동안 타깃 화면 클릭 금지. 스냅샷은 VMware 메뉴로.
- **P16** 켤 땐 디버거 먼저. 실행 중엔 Suspend 말고 **스냅샷만**.
- **P17** 끝나면 `bc *`.
- **P18** 라이브 값(`lm`·`dt`·`!vtop`)은 ① 에서 멈춘 동안. `g` 뒤엔 못 본다 — 오프라인에서 보는 건 덤프 값이다.
- **dast/01 고침 1**: `!vtop` 은 숫자 VA 만 (역따옴표 빼고). `@$peb`·`ntdll!...` 넣으면 `usage:` 만 나온다.
- **dast/01 고침 2**: 멈춘 동안 `.time` 은 그대로다. 스냅샷 시간은 서버 `date +%T.%N` 앞뒤로.
- **엉뚱한 VM**: 스냅샷은 `bob_baseline_vm` 만. 공유 폴더는 `Windows 11 x64` 만. `WIN11-26H1-HDAUDIO` 는 어떤 것도 하지 않는다. `vmrun` 은 `list` 만.
- **두 WinDbg 창 헷갈림**: 커널 세션 창(라이브)과 덤프 창(오프라인)을 섞지 않는다. 오프라인 명령은 덤프 창에서만. 라이브 로그는 8단계에서 이미 닫혔다.
- `LdrInitializeThunk` 는 test.exe 의 모든 새 스레드가 지난다 (run4). 첫 번째로 서는 것이 첫 스레드.

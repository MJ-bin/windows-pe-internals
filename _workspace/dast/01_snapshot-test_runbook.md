# 01 snapshot-test — 실행 안내

> 로그 붙여넣을 곳: 서버 `ms/logs/01_snapshot-test.txt` (지금 빈 파일)
> 디버거 VM 로그 경로: `C:\logs\01_snapshot-test.txt` (S1 에서 폴더를 만든다)
> 재부팅: **타깃 1번** (S5, 디버그 설정을 새로 켰을 때만) · 예상 시간: 세팅 약 30~60분 (WinDbg 내려받기·심볼 내려받기 포함) + 실행 약 15분 + 스냅샷 몇 분 + 복사 몇 분
> 이번에 보는 것: test.exe 첫 스레드가 `ntdll!LdrInitializeThunk` 입구에 선 순간, 호스트 VMware 스냅샷으로 **메모리 통째 파일**을 얻을 수 있나 + 크기·시간 + 나중에 맞춰 볼 대조값 몇 개

**VM 세 대 정리** (호스트 = 이 분석 서버, Ubuntu 22.04, VMware Workstation 17.6.3. 호스트 쪽 명령은 전부 서버 터미널에서 친다):

| 역할 | VMware 이름 | 폴더 | 메모 |
|---|---|---|---|
| **타깃** | `bob_baseline_vm` (표시 이름 "BOB Windows 11 24H2") | `/home/mjbin/vmware/bob_baseline_vm/` | 8 GB, 지금 일시정지 상태. test.exe 를 여기서 돌리고, **여기만** 스냅샷한다 |
| **디버거** | `Windows 11 x64` | (이번엔 폴더 안 씀) | 16 GB. WinDbg 를 여기에 새로 깐다 |
| **건드리지 않음** | `WIN11-26H1-HDAUDIO` (recovery) | — | 다른 작업 중. 이번 분석과 무관. 스냅샷·`vmrun`·설정 변경 대상으로 쓰지 않는다 |

흐름:

- **A. 세팅 (S0 ~ S7)** — 이번에 처음. 디버거 VM 에 WinDbg 를 깔고, 타깃과 KDNET 으로 잇는다.
- **B. 실행 (0-C ~ 11)** — 멈추는 곳은 딱 하나다.
  - ① `ntdll!LdrInitializeThunk` 입구 (test.exe 일 때만)
  - 거기까지 가려고 cmd 쪽에서 한 번 잠깐 멈춘다 (test.exe 주소 얻기용). 이건 기준점이고 스냅샷은 안 찍는다.

체크포인트 (출력 붙여 주면 값 채운 명령을 돌려줌):

- 세팅 중: **S2** (Secure Boot·BitLocker·`ver` 가 예상과 다를 때), **S5** (연결이 안 될 때). 그 밖에도 막히면 그 단계 출력을 붙여 주세요.
- 실행 중 (헷갈릴 때만): **1단계** (cmd 고르기), **4단계** (`<TEST>` 주소).

---

# A. 세팅 (이번에 처음)

## S0. 호스트(서버) — 켜진 VM 보기, (선택) 디버거 VM 메모리 줄이기

1. 지금 켜진 VM 목록 (읽기만 하는 명령):
```
vmrun -T ws list
free -g
```
- 정상이면: `WIN11-26H1-HDAUDIO` 쪽 vmx 가 보일 수 있다 — **그대로 둔다.**
2. **(선택) 디버거 VM 메모리 줄이기** — 정하는 건 사용자.
   - 까닭: 호스트 메모리 46 GB 중 지금 쓸 수 있는 게 약 35 GB. 다른 작업 VM 16 GB 가 켜진 채로 타깃 8 GB + 디버거 16 GB 를 더 켜면 빠듯할 수 있다 `[추측]`. WinDbg 만 돌리면 8 GB 로 충분할 것 `[추측]`.
   - 하는 법: VMware 에서 **`Windows 11 x64`** 탭 → VM → Settings → Hardware → Memory → `8192 MB` → OK.
   - **디버거 VM 이 완전히 꺼져(Powered off) 있을 때만** 바꿀 수 있다. 일시정지(Suspended) 상태면 바꾸지 말고 그냥 둔다.
   - 줄이지 않아도 된다. 줄였으면 HOST NOTES 에 적는다.

## S1. 디버거 VM (`Windows 11 x64`) — 켜고 WinDbg 준비

켜는 순서는 **디버거 먼저, 타깃 나중** (P16).

1. VMware 에서 `Windows 11 x64` 를 켠다 (Power on / Resume). 로그인.
2. **관리자 PowerShell** 을 연다 (시작 버튼 오른쪽 클릭 → Terminal (Admin) 또는 Windows PowerShell (Admin)). 아래를 한 블록씩.

자기 IP 보기:
```
ipconfig | findstr IPv4
```
- `192.168.100.x` 꼴 주소가 나온다. 이게 `<DBGIP>` (예전 기록은 `.132` 였지만 DHCP 라 다를 수 있다). **적어 둔다** — S3 에서 쓴다.

폴더·심볼 경로·방화벽:
```
New-Item -ItemType Directory -Force C:\logs, C:\symbols
setx _NT_SYMBOL_PATH "srv*C:\symbols*https://msdl.microsoft.com/download/symbols" /M
New-NetFirewallRule -DisplayName "KDNET-50000" -Direction Inbound -Protocol UDP -LocalPort 50000 -Action Allow -Profile Any
```
- 정상이면: 폴더 두 개가 만들어지고, `setx` 는 `SUCCESS`, 방화벽 규칙은 `Enabled : True` 를 보여 준다.

WinDbg 설치:
```
winget install --id Microsoft.WinDbg -e --accept-source-agreements --accept-package-agreements
```
- 정상이면: `Successfully installed`. 시작 메뉴에 **WinDbg** 가 생긴다.
- 이상하면:
  - `winget` 이 없다는 말이 나오면 → Microsoft Store 앱에서 "WinDbg" 검색해 설치, 또는 브라우저로 `https://aka.ms/windbg/download`.
  - 그래도 안 되면 출력 붙여 주세요.
3. 아직 WinDbg 는 켜지 않아도 된다 (S4 에서 켠다). WinDbg 를 처음 켤 때 Windows 방화벽 창이 뜨면 **허용**.

## S2. 타깃 VM (`bob_baseline_vm`) — 켜고 상태 확인 (아직 아무것도 바꾸지 않는다)

1. VMware 에서 **`bob_baseline_vm`** 탭을 고르고 켠다 (일시정지 상태라 Resume — 10-17 상태에서 이어진다). 로그인.
2. **관리자 cmd** 를 연다. 아래 명령.
```
ver
ipconfig | findstr IPv4
manage-bde -status C:
bcdedit /enum {current}
bcdedit /dbgsettings
```
3. Secure Boot 는 **관리자 PowerShell** 에서 (cmd 에선 안 됨):
```
Confirm-SecureBootUEFI
```
- 정상이면:
  - `ver` 가 `10.0.26100.9457`.
  - `ipconfig` 의 `192.168.100.x` 가 `<TGTIP>` (예전 기록은 `.133`). 적어 둔다.
  - `manage-bde` 에 `Protection Off` (또는 `Fully Decrypted`). (BitLocker 가 켜져 있으면 부팅 설정을 바꿀 때 복구 키를 물을 수 있다)
  - `Confirm-SecureBootUEFI` 가 `False`. (켜져 있으면 커널 디버깅을 못 켠다)
- `bcdedit` 결과로 다음 단계를 고른다:
  - **(가)** `/enum {current}` 에 `debug  Yes` **이고** `/dbgsettings` 가 `debugtype Net`, `hostip <DBGIP>` (S1 값과 같음), `port 50000` → 설정이 이미 맞다. `key` 값을 적어 두고 **S3 건너뛰고 S4 로**. 이 경우 재부팅도 필요 없다 (S5 에서 WinDbg 가 알아서 붙는지 본다).
  - **(나)** 그 밖 (`debug No`, `debug` 줄이 없음, hostip 가 다름, debugtype 이 Net 이 아님) → **S3 으로**.
- 이상하면 (체크포인트): `ver` 가 26100.9457 이 아니거나, Secure Boot 가 `True` 거나, BitLocker 가 켜져 있으면 → **여기서 멈추고** 출력 붙여 주세요. (Secure Boot 를 끄려면 VM 을 끄고 VMware 설정을 바꿔야 해서, 같이 정한다)

## S3. 타깃 — 디버그 설정 켜기 (S2 가 (나) 일 때만)

관리자 cmd 에서. `<DBGIP>` 는 S1 값.
```
bcdedit /debug on
bcdedit /dbgsettings net hostip:<DBGIP> port:50000
bcdedit /dbgsettings
```
- 정상이면: 둘째 줄이 `Key=xxxx.xxxx.xxxx.xxxx` 를 보여 준다. 이게 `<KEY>`. **그대로 적어 둔다** (점까지 정확히). 셋째 줄에서 hostip·port 가 맞는지 한 번 더 본다.
- **아직 재부팅하지 않는다.** 디버거가 먼저 기다리고 있어야 한다 (S4 → S5).
- 이상하면: `The value is protected by Secure Boot policy` 같은 말이 나오면 → Secure Boot 가 켜져 있는 것. 멈추고 출력 붙여 주세요.

## S4. 디버거 VM — WinDbg 를 커널 연결 대기로

1. 디버거 VM 에서 WinDbg 를 켠다 (S1 의 `setx` 뒤에 처음 켜는 것이어야 심볼 경로가 먹는다. 이미 켜 뒀으면 껐다 켠다).
2. File → **Attach to kernel** → **Net** 탭.
   - Port number: `50000`
   - Key: `<KEY>` (S2 (가) 또는 S3 에서 적은 값)
   - Target 칸이 있으면 비워 둔다. "Break on connection" 은 끈 채로.
   - OK.
- 정상이면: `Waiting to reconnect...` 같은 줄이 뜨고 기다린다. (S2 (가) 였으면 곧바로 S5 의 연결 메시지가 뜰 수도 있다)
- 방화벽 허용 창이 뜨면 **허용**.

## S5. 타깃 재부팅 → 연결 확인

1. **S3 을 했으면** 타깃 관리자 cmd 에서:
```
shutdown /r /t 0
```
   S2 (가) 였으면 재부팅하지 않고 2번으로.
2. 디버거 VM WinDbg 창을 본다.
- 정상이면: 아래 비슷한 줄이 뜬다.
  - `Connected to target <TGTIP> on port 50000 on local IP <DBGIP>`
  - `Kernel Debugger connection established.`
  - 그 뒤 심볼 내려받기 때문에 한동안 느릴 수 있다 (처음 한 번).
3. 타깃이 로그인 화면/바탕화면까지 다시 올라오면, 디버거 VM WinDbg 에서 **Break** (Debug → Break, 또는 Ctrl+Break). `kd>` 프롬프트가 나오면:
```
vertarget
g
```
- 정상이면: `vertarget` 에 `26100` 이 보이고, `g` 뒤 타깃 VM 이 다시 움직인다 (마우스·창이 반응).
- Break 동안 **타깃 화면은 클릭하지 않는다** (P15). 굳은 것 같으면 디버거에서 `g`.
- 이상하면 (체크포인트): 몇 분 지나도 `Waiting to reconnect...` 만 나온다 → 아래를 붙여 주세요.
  - 타깃: `bcdedit /dbgsettings` 출력, `ipconfig | findstr IPv4`
  - 디버거: `ipconfig | findstr IPv4`, WinDbg 창 내용
  - (흔한 원인 `[추측]`: 디버거 IP 가 S3 의 hostip 와 다름 / key 오타 / 방화벽)

## S6. 타깃 — test.exe 넣기·확인

타깃 cmd (관리자 아니어도 됨) 에서:
```
dir C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin
```
- 정상이면: `test.exe`, `dll1.dll`, `dll2.dll` 이 보인다 → S7 로.
- 없으면 공유 폴더로 넣는다:
  1. 서버 터미널에서 (repo 의 빌드된 파일을 타깃 공유 폴더로 복사):
```
mkdir -p /home/mjbin/Downloads/pe-sample
cp /home/mjbin/prj/bob/teamprj/windows-pe-internals/sample/bin/test.exe /home/mjbin/prj/bob/teamprj/windows-pe-internals/sample/bin/dll1.dll /home/mjbin/prj/bob/teamprj/windows-pe-internals/sample/bin/dll2.dll /home/mjbin/prj/bob/teamprj/windows-pe-internals/sample/bin/test.pdb /home/mjbin/prj/bob/teamprj/windows-pe-internals/sample/bin/dll1.pdb /home/mjbin/prj/bob/teamprj/windows-pe-internals/sample/bin/dll2.pdb /home/mjbin/Downloads/pe-sample/
```
  2. 타깃 cmd 에서:
```
mkdir C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin
copy "\\vmware-host\Shared Folders\Downloads\pe-sample\*" C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin\
dir C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin
```
  - `\\vmware-host\Shared Folders\Downloads` 경로는 `[추측]`. 안 열리면 탐색기 주소창에 `\\vmware-host\Shared Folders` 를 쳐서 보이는 이름으로 바꾼다. 그래도 안 보이면 VMware 에서 `bob_baseline_vm` → VM → Settings → Options → Shared Folders 가 켜져 있는지 본다 (타깃 VM 설정만. 다른 VM 은 건드리지 않음).
  - test.exe 는 `/MT` 빌드라 따로 깔 런타임은 없다 `[소스: sample/build.bat]`.

## S7. (선택) `kd-ready` 스냅샷 — 되돌리기용

세팅이 끝난 상태를 남겨 두면 다음 바퀴에 S1~S6 을 다시 안 해도 된다. 정하는 건 사용자. **메모리 덤프용이 아니다.**

- 디버거가 Break 상태가 **아닐 때** (타깃이 돌고 있을 때) 찍는다.
- VMware 에서 `bob_baseline_vm` → VM → Snapshot → Take Snapshot → 이름 `kd-ready`. 디버거 `Windows 11 x64` 도 같은 이름으로 (선택).
- `WIN11-26H1-HDAUDIO` 는 찍지 않는다.
- 디스크를 VM 메모리만큼 더 쓴다 (서버 여유 약 575 GB `[관찰]`).
- 나중에 이 스냅샷으로 되돌리면 KDNET 이 다시 붙는 데 시간이 걸리거나 S4 를 다시 해야 할 수 있다 `[추측]`.

---

# B. 실행

## 0-C. 타깃 VM — cmd 준비 (test.exe 는 아직 실행하지 않는다)

- 할 일: 타깃 VM 에서 **보통 cmd** (관리자 아니어도 됨) 를 새로 열고 아래 여섯 줄.
```
ver
ipconfig | findstr IPv4
cd C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin
dir test.exe
title pesnap
tasklist /v /fi "imagename eq cmd.exe"
```
- 정상이면:
  - `ver` 가 `10.0.26100.9457`, `ipconfig` 가 S2 의 `<TGTIP>` 와 같다.
  - `dir` 에 `test.exe` 가 보인다.
  - `tasklist` 목록에서 창 제목(Window Title) 칸에 `pesnap` 이 들어간 줄의 **PID** (10진수) 를 적어 둔다. 이게 1단계에서 쓸 `<CMDPID>`.
- 이상하면: `test.exe` 가 없으면 S6 으로 돌아간다.

## 0-D. 호스트(서버) — 타깃 VM 폴더 확인

타깃 폴더는 `/home/mjbin/vmware/bob_baseline_vm/` 이다. 맞는지 보고, 스냅샷 전 모습을 남긴다.
```
vmrun -T ws list
ls -l --time-style=full-iso /home/mjbin/vmware/bob_baseline_vm/
df -h /home/mjbin
```
- 확인 (셋 다 맞아야 한다):
  - [ ] `vmrun -T ws list` 에 `/home/mjbin/vmware/bob_baseline_vm/....vmx` 가 있다 (타깃이 켜져 있음).
  - [ ] VMware 의 `bob_baseline_vm` 탭 화면에 0-C 의 `pesnap` cmd 가 보인다 (= 그 탭이 타깃).
  - [ ] 그 탭이 `WIN11-26H1-HDAUDIO` 도, `Windows 11 x64` (디버거) 도 아니다.
- 타깃 VM 의 **메모리 크기**: `bob_baseline_vm` → VM → Settings → Hardware → Memory (예: `8192 MB`). 적어 둔다.
- `ls` 출력에서 `.vmx` 파일 이름(확장자 뺀 것)을 `<VM>` 으로 부른다 (스냅샷 파일 이름 앞부분이 이것).
- 이상하면: 폴더가 없거나 위 확인이 하나라도 안 맞으면 → 멈추고 `vmrun -T ws list` 와 `ls /home/mjbin/vmware/` 출력을 붙여 주세요.

## 1. 디버거 VM — 멈추고 로그 시작

- 할 일: WinDbg 에서 **Break** (Debug → Break, 또는 Ctrl+Break). `kd>` 프롬프트가 나오면 아래 명령.
```
.logopen C:\logs\01_snapshot-test.txt
.echo ==== STEP1 SETUP
.time
.sympath
.reload
!lmi nt
bc *
bl
!process 0 0 cmd.exe
? 0n<CMDPID>
```
- 정상이면:
  - `.sympath` 에 `srv*C:\symbols*https://msdl.microsoft.com/download/symbols` 가 들어 있다.
  - `!lmi nt` 의 pdb 이름이 `ntkrnlmp.pdb`, GUID 가 `C29EBFB0-6B78-B3C0-20DC-A66D99713F9E` (Age `1`) 다. (빌드 26100.9457 이라는 뜻)
  - `bl` 출력이 비어 있다.
  - `? 0n<CMDPID>` 가 16진수를 보여 준다 (예: `Evaluate expression: 7572 = 00000000'00001d94`). cmd.exe 목록에서 **`Cid` 가 이 16진수와 같은 줄**의 `PROCESS ffff....` 값이 `<CMD>`.
- 이상하면:
  - `.sympath` 가 비었으면: `.sympath srv*C:\symbols*https://msdl.microsoft.com/download/symbols` 치고 `.reload` 다시.
  - GUID 가 다르면: 빌드가 다르다. 멈추고 출력 붙여 주세요.
- 체크포인트: 어느 cmd 인지 헷갈리면 `!process` 출력과 `<CMDPID>` 를 붙여 주세요 (P5).

## 2. cmd 에 멈춤점 → test.exe 실행

`<CMD>` 를 1단계 값으로 바꿔서 친다.
```
bp /p <CMD> nt!PspCallProcessNotifyRoutines
bl
g
```
- 할 일: **타깃 VM** 의 `pesnap` cmd 에서 `test.exe` 실행.
- 정상이면: 디버거가 `Breakpoint 0 hit` 으로 멈춘다 (`nt!PspCallProcessNotifyRoutines`).
- 이상하면:
  - 안 멈추고 test.exe 가 끝까지 돌았다 → `<CMD>` 가 틀렸다. Break → `bl` 출력 붙여 주세요.
  - test.exe 를 치기도 전에 멈췄다 → `/p` 가 빠졌거나 `<CMD>` 가 다른 프로세스. `bl` 출력 붙여 주세요.

## 3. 이름 확인 (이 빌드에 있는지)

01_sprint 에서 같은 빌드로 확인한 이름이지만, 이 repo 첫 실행이라 한 번 본다.
```
.echo ==== STEP3 NAMES
.reload /user
x nt!PspCallProcessNotifyRoutines
x ntdll!LdrInitializeThunk
lm m ntdll
? ntdll!LdrInitializeThunk - ntdll
```
- 정상이면:
  - `x` 두 줄 모두 주소 하나씩 나온다.
  - 마지막 줄이 `00000000'000e1420` 이다 (ntdll 안 위치 `0xe1420`, 정적 분석 값과 같음).
- 이상하면: `x ntdll!LdrInitializeThunk` 가 빈칸이거나 마지막 값이 `e1420` 이 아니면 → 여기서 멈추고 출력 붙여 주세요.

## 4. test.exe 주소 얻기 → cmd 쪽 멈춤점 지우기
```
.echo ==== STEP4 TEST
!process 0 0 test.exe
bc 0
bl
```
- 정상이면: `PROCESS ffff....  ... Image: test.exe` 줄이 하나 나온다. 그 `ffff....` 값이 `<TEST>`. `bl` 은 비어 있다.
- `bc 0`: cmd 쪽 멈춤점은 여기서 바로 지운다 (P13).
- 체크포인트: test.exe 가 안 나오거나 둘 이상이면 출력을 붙여 주세요.

## 5. ① 멈춤점 걸고 보내기

`<TEST>` 를 4단계 값으로 바꿔서 친다. **기록 명령을 안 붙인다** (= 진짜 멈춤).
```
bp /p <TEST> ntdll!LdrInitializeThunk
bl
g
```
- 정상이면: 곧 `Breakpoint 0 hit` (번호는 0 이 아닐 수도 있음) 으로 멈추고, 프롬프트 바로 위에 `ntdll!LdrInitializeThunk:` 가 보인다.
- 이상하면:
  - 안 멈추고 test.exe 가 끝까지 돌았다 → Break → `bl` 붙여 주세요.
  - `Unable to insert breakpoint` 같은 말이 나오면 → 출력 붙여 주세요 (하드웨어 `ba e1` 로 바꾼다, P11).

## 6. ① 에서 — 대조값 찍기 (스냅샷 **전에**)

멈춘 그대로 아래를 **한 블록씩** 친다. 전부 지금 프로세스(`@$proc`) 기준이라 바꿔 넣을 값이 없다.

```
.echo ==== STOP1 LdrInitializeThunk
.time
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
```
.echo ==== STOP1 VTOP
!vtop 0 @$peb
!vtop 0 ntdll!LdrInitializeThunk
.echo ==== STOP1 DONE
```

- 정상이면:
  - `!process -1 0` 의 `Image:` 가 `test.exe`. 그 `PROCESS` 값이 `<TEST>` 와 같다. `DirBase` 가 찍혀 있다.
  - `!thread -1 0` 에 `THREAD ffff....` 와 `Teb:` 값이 있다.
  - `r` 의 `rip` 가 `ntdll!LdrInitializeThunk` 주소 (= 3단계 `lm` 의 ntdll 시작 + `0xe1420`).
  - `k 5` 는 `ntdll!LdrInitializeThunk` 한 줄뿐이거나 아주 짧다 (부른 함수가 없음, run4).
  - `dt nt!_PEB` 에서 `ImageBaseAddress`·`ProcessParameters` 는 값이 있고, **`Ldr`·`ProcessHeap` 은 0 이어도 정상**이다 (둘 다 이 뒤에 ntdll 이 채움 `[정적]`).
  - `lm m test` 는 **빈칸이어도 괜찮다** (아직 로더 목록이 안 만들어진 때라 그럴 수 있음 `[추측]`). test 시작 주소는 `ImageBaseAddress`·`SectionBaseAddress` 로 대신 본다.
  - `!vtop` 두 줄 모두 `... translates to physical address ...` 처럼 물리 주소가 나온다. (서버 도구가 메모리 파일에서 같은 자리를 찾는 데 쓴다)
- 이상하면:
  - `Image:` 가 test.exe 가 아니면 → 여기서 멈추고 출력 붙여 주세요. **스냅샷 찍지 않는다.**
  - `dt nt!_PEB` 나 `dt nt!_CONTEXT` 가 `Symbol nt!_PEB not found` 면 → `nt!` 를 `ntdll!` 로 바꿔 한 번 더 친다. 그래도 안 되면 그냥 넘어간다 (로그에 남은 걸로 충분).
  - `? @$peb` / `? @$teb` 가 0 이나 에러면 → 그냥 넘어간다. `!process`·`!thread` 출력에 같은 값이 있다.

## 7. ① 에서 — 호스트 VMware 스냅샷 (디버거는 멈춘 그대로)

**타깃 VM 화면 안은 클릭하지 않는다** (P15). VMware 창의 메뉴·툴바만 쓴다. 입력이 VM 에 잡혀 있으면 먼저 `Ctrl+Alt`.

0. **스냅샷 직전 확인** — 하나라도 아니면 찍지 말고 멈추고 물어봐 주세요.
   - [ ] VMware 에서 지금 고른 탭이 **`bob_baseline_vm`** ("BOB Windows 11 24H2") 이다.
   - [ ] `WIN11-26H1-HDAUDIO` 가 **아니다** (다른 작업 VM — 절대 스냅샷하지 않는다). `Windows 11 x64` (디버거) 도 아니다.
   - [ ] 그 탭 화면에 0-C 의 `pesnap` cmd 창이 보인다 (디버거가 멈춘 상태라 화면은 굳어 있다. 보기만 한다).
1. 디버거 VM WinDbg 에서 시작 시각을 남긴다.
```
.echo ==== SNAP START
.time
```
2. VMware Workstation 에서 **`bob_baseline_vm` 탭** 이 골라진 상태로 → VM → Snapshot → **Take Snapshot...**
   - 이름: `01_snapshot-test`
   - 켜져 있는 VM 이라 메모리도 같이 저장된다 (이 폴더들에 이미 있는 `-SnapshotN.vmem` 이 그 결과 `[관찰]`).
   - 창 아래쪽 진행 표시("Saving state" 등)가 끝날 때까지 기다린다.
   - `vmrun snapshot` 은 쓰지 않는다 (엉뚱한 vmx 를 칠 위험. 이번엔 메뉴로만).
3. 끝나면 디버거 VM WinDbg 에서:
```
.echo ==== SNAP END
.time
```
4. 서버 터미널에서 새로 생긴 파일을 본다 (0-D 의 목록과 비교).
```
ls -l --time-style=full-iso /home/mjbin/vmware/bob_baseline_vm/
du -h /home/mjbin/vmware/bob_baseline_vm/*-Snapshot*.vmem /home/mjbin/vmware/bob_baseline_vm/*-Snapshot*.vmsn
```
   - 찾을 것: 방금 시각의 `<VM>-Snapshot<N>.vmem` (메모리) 와 `<VM>-Snapshot<N>.vmsn` (상태). 이 번호를 `<N>` 으로 부른다.
   - 번호가 없는 `<VM>.vmem` 은 **지금 돌고 있는 VM 의 것**이라 고르지 않는다. S7 의 `kd-ready` 를 찍었다면 그것도 `-Snapshot` 파일이 있으니, **시각**으로 가린다.
   - 크기가 VM 메모리보다 작을 수 있다 (이미 있는 8 GB VM 스냅샷은 약 5 GB `[관찰]`). `ls -l` 의 바이트와 `du -h` 를 둘 다 적는다.
- 정상이면: 방금 시각의 `-Snapshot<N>.vmem` 하나와 `-Snapshot<N>.vmsn` 하나가 생겼다. 이름·크기를 적어 둔다.
- 이상하면:
  - `.vmsn` 만 있고 `.vmem` 이 없다 → VMware 가 메모리를 `.vmsn` 안에 같이 넣었을 수 있다 `[추측]`. `.vmsn` 크기를 적고 그대로 진행 (몇 GB 면 메모리가 들어 있는 것).
  - 둘 다 없거나 아주 작다 → 디버거는 멈춘 그대로 두고, 8단계(예비)로 간다.
  - 스냅샷 중·뒤에 WinDbg 에 연결 끊김 / 재연결 메시지가 나오면 → 그대로 두고 로그에 남게 둔다. 다음에 `kd>` 가 다시 받는지 `? @$proc` 한 번 쳐 본다.

## 8. (예비, 7단계에서 메모리 파일을 못 얻었을 때만) WinDbg 전체 덤프

7단계가 잘 됐으면 **건너뛴다.**

- 할 일: 디버거 VM 에서 `mkdir C:\dump` (처음이면). 그다음 WinDbg 에서:
```
.echo ==== DUMP START
.time
.dump /f C:\dump\01_snapshot-test.dmp
.echo ==== DUMP END
.time
```
- 정상이면: 진행 표시가 나오다가 `Dump successfully written` 같은 말로 끝난다. 네트워크로 메모리를 다 끌어오는 거라 **아주 오래 걸릴 수 있다** `[추측]`.
- 끝나면 파일 크기 확인 (디버거 VM cmd): `dir C:\dump`

## 9. 끝 — 멈춤점 지우고 test.exe 보내기

스냅샷 파일 이름·크기를 적었으면, 디버거 VM WinDbg 에서:
```
.echo ==== END
bc *
bl
.logclose
g
```
- 정상이면: 타깃 VM 의 cmd 에서 test.exe 가 끝까지 돌고 프롬프트가 돌아온다. `bl` 은 비어 있다.
- 이상하면: `Single step exception` 등으로 또 멈추면 `bc *` → `g` (P12).
- **스냅샷을 되돌리지(Revert) 않는다.** 지우지도 않는다 (서버에 복사한 걸 확인할 때까지).

## 10. 서버 폴더로 복사 (같은 디스크 안)

호스트가 곧 서버라 네트워크 전송이 없다. **옮기기(`mv`) 말고 복사(`cp`)** — VMware 쪽 스냅샷 파일은 그대로 둔다.
`<VM>`·`<N>` 은 7단계에서 찾은 값으로 바꾼다. 서버 터미널에서:
```
mkdir -p /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/01_snapshot-test
time cp --sparse=always "/home/mjbin/vmware/bob_baseline_vm/<VM>-Snapshot<N>.vmem" "/home/mjbin/vmware/bob_baseline_vm/<VM>-Snapshot<N>.vmsn" /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/01_snapshot-test/
ls -l /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/01_snapshot-test/
du -h /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/01_snapshot-test/*
```
- 정상이면: `time` 이 `real ...` 로 걸린 시간을 보여 준다. `ls -l` 바이트 수가 7단계 4번의 바이트 수와 같다.
- 압축은 하지 않는다 (같은 디스크라 필요 없음).
- 8단계(예비)로 갔다면: 디버거 VM 의 `C:\dump\01_snapshot-test.dmp` 는 guest 안에 있다 (디버거 VM 엔 공유 폴더가 없다). 꺼내는 길은 그때 같이 정한다 — 출력 붙여 주세요.
- 이상하면: 크기가 다르거나 `cp` 에러가 나면 → 에러를 그대로 붙여 주세요.

## 11. 로그 붙이기

1. 디버거 VM 의 `C:\logs\01_snapshot-test.txt` 내용을 **전부** 서버 `ms/logs/01_snapshot-test.txt` 에 붙여넣는다.
2. 그 **맨 아래**에 아래 블록을 붙이고 값을 채운다 (모르는 칸은 `?`).
```
==== HOST NOTES
host_os: Ubuntu 22.04 (same machine as analysis server)
vmware: Workstation 17.6.3
target_vm: bob_baseline_vm  dir /home/mjbin/vmware/bob_baseline_vm/  memory <e.g. 8192 MB>
debugger_vm: Windows 11 x64  memory <16384 MB or reduced to ...>
not_touched: WIN11-26H1-HDAUDIO (other work)
ips: target <TGTIP>  debugger <DBGIP>
setup: <S2 (ga) already set / S3 bcdedit set + reboot>  windbg install <winget / store / other>
kd_ready_snapshot: <not taken / taken on target / taken on both>
vmrun_list: <paste vmrun -T ws list lines from 0-D>
snapshot_name: 01_snapshot-test
snap_vmem: <file name>  <bytes from ls -l>  <du -h>
snap_vmsn: <file name>  <bytes from ls -l>  <du -h>
snapshot_time: <seconds, or see SNAP START/END in log>
fallback_dump: <not used / C:\dump\01_snapshot-test.dmp  <bytes>  <time>>
copy_time: <real from time cp>
server_path: /home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/01_snapshot-test/
server_ls: <paste ls -l lines>
debugger_issues: <none / what happened during or after snapshot>
```
3. "로그 붙였어" 라고 말하기.

---

## 자주 하는 실수 (이번 run 관련)

- **P1** Break 먼저, 그다음 `.logopen`.
- **P2** 명령·`.echo` 문구는 영어만. (HOST NOTES 의 칸 이름도 영어. 값 설명은 한글이어도 됨 — 서버 파일이라 괜찮다)
- **P4** 조건 없는 커널 멈춤점 금지. 2단계·5단계 모두 `bp /p`. `/p` 를 빼먹으면 다른 프로세스에서 계속 멈춘다.
- **P5** cmd 는 이름 말고 EPROCESS 주소로 고른다 (0-C 의 PID → 1단계 `? 0n<CMDPID>` → `Cid`).
- **P6** `/t` 는 안 쓴다.
- **P8** `bc` 만 치면 안 지워진다. `bc 0` / `bc *`.
- **P13** test.exe 주소를 얻으면 cmd 쪽 멈춤점을 바로 `bc 0` (4단계).
- **P15** 디버거가 멈춘 동안 타깃 VM 화면 안을 클릭하지 않는다. 스냅샷은 VMware 메뉴로. 호스트가 굳은 것 같으면 `Ctrl+Alt`.
- **P16** 켤 땐 **디버거 먼저, 타깃 나중** (S1 → S2). 이번 실행 중에는 VM 을 일시정지(Suspend)하지 않는다 — **스냅샷만** (Suspend 와 다름). 혹시 멈춰야 하면 타깃 먼저.
- **엉뚱한 VM 스냅샷** (이번에 새로 넣은 주의): 스냅샷은 `bob_baseline_vm` 만. `WIN11-26H1-HDAUDIO` 는 다른 작업 중 — 스냅샷·`vmrun`·설정 변경 대상으로 쓰지 않는다. 7단계 0번 확인을 건너뛰지 않는다. `vmrun` 은 `list` (읽기만) 만 쓴다.
- **세팅 순서** (이번에 새로 넣은 주의): 타깃 재부팅(S5)은 디버거 WinDbg 가 기다리는 상태(S4)가 된 **뒤에**. 디버거 VM IP 가 DHCP 로 바뀌면 타깃 `hostip` 와 안 맞아 연결이 안 된다 — 다음 세션에 안 붙으면 S1 의 `ipconfig` 부터 다시 본다.
- **P17** ntdll 멈춤점을 남겨 두지 않는다. 끝나면 `bc *` (9단계).
- **P18** `lm`·`dt`·`!vtop` 은 test.exe 가 살아 있을 때 (6단계, ① 에서 멈춘 동안) 친다. 9단계 `g` 뒤엔 test.exe 가 끝나서 못 본다.
- 끝 표시를 공용 출구에 걸지 않는다 (P3). 이번 끝은 ① 의 스냅샷 + 손으로 친 `bc *` → `g` 다. `test!main` 은 이번엔 필요 없다.
- `LdrInitializeThunk` 는 test.exe 의 **모든 새 스레드**가 지난다 (run4). 첫 번째로 서는 것이 첫 스레드다. 스냅샷은 그 첫 번째에서만 찍고, 9단계에서 `bc *` 하므로 뒤 스레드에서는 안 선다.

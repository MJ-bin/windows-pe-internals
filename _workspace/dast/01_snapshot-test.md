# 01 snapshot-test — 동적 분석 요청

- 날짜: 2026-10-25
- 스프린트: 03_sprint / 예정 run: 바퀴를 따로 닫지 않음 (Q77a 시험. 결과는 Q77 본 바퀴 run 문서에 합침)
- 대기열 항목: Q77 (짐 지도 B — 사진) 의 앞부분 **Q77a 시험 덤프**
- 관련 정적 결과: `_workspace/sast/38_baggage-checklist_result.md` — "Q77(사진)에서 떠야 할 것 — 추천" 절 (156~197줄)
- 재시도: 없음
- 이 repo 의 **첫 동적 요청서**다 (dast 번호 01). KDNET 연결·멈춤점 거는 법은 `windbg-kd-runbook` 스킬 (01_sprint 함정 목록) 을 따른다.

## 배경 (대화에서 정한 것)

- 사용자 결정 (2026-10-25, plan.md 결정 기록): Q77 은 **통째 덤프 방식**.
  - 사용자가 멈춘 순간에 메모리를 **통째로** 뜨고, 파일을 서버로 옮긴다.
  - 칸 읽기·비교는 서버 스크립트로 한다 (도구는 아직 없음 — 하네스 변경으로 따로 만든다. 이번 요청 범위 밖).
  - 그래서 이번 runbook 에는 `dt` 로 구조체를 다 찍는 단계가 **없다**. 서버 도구가 맞게 읽는지 맞춰 볼 **대조값 몇 개**만 찍는다.
- 이번은 **시험 1장**이다. 목적은 세 가지:
  1. 멈춤점 ① 에 test.exe 문맥으로 정확히 서는지
  2. 덤프 파일 **크기**와 뜨는·옮기는 **시간**
  3. 어떤 방법이 쓸 만한지 → 결과로 본 덤프 장수 (②, 그리고 사용자가 원한 추가 지점 ⓪·①-a·①-b) 를 정한다
- 사용자는 지금 **VMware 호스트** 앞에 있다 (디버거 VM·타깃 VM 둘 다 이 호스트에서 돈다).
  - 그래서 기본 방법 = **멈춘 상태에서 호스트의 VMware 로 타깃 VM 스냅샷** (메모리 포함). 메모리 파일이 호스트 디스크의 VM 폴더에 생긴다. 네트워크로 넘길 필요가 없다.
  - 호스트 OS (Windows / Linux) 는 아직 모른다. runbook 첫 단계에서 묻거나 둘 다 적어 준다.
  - **(2026-10-25 덧붙임, 사용자 답 + 오케스트레이터 확인)** 호스트 = **이 분석 서버와 같은 기계** (Ubuntu 22.04, hostname `mjbin`). 디버거·타깃 guest 는 둘 다 Windows 11.
    - VMware Workstation 17.6.3 (`/usr/bin/vmware`, `/usr/bin/vmrun`) `[관찰]`.
    - VM 폴더: `/home/mjbin/vmware/<VM 이름>/`. 스냅샷 메모리는 그 폴더에 `<VM>-SnapshotN.vmem` + `<VM>-SnapshotN.vmsn` 로 생긴다 (이미 있는 스냅샷 파일로 확인 `[관찰]`). 이미 있는 `.vmem` 크기는 3.9 ~ 10 GB (8 GB VM 은 약 5 GB).
    - 그래서 **서버로 보내기 = 같은 디스크 안 복사** (`cp`). 네트워크 전송 없음. 디스크 여유 575 GB `[관찰]`.
    - 어느 VM 이 타깃·디버거인지는 아직 사용자 확인 전. 후보: `bob_baseline_vm` (displayName "BOB Windows 11 24H2", 8 GB) / `WIN11-26H1-HDAUDIO` (16 GB, 지금 recovery vmx 가 켜져 있음) / `Windows 11 x64` (16 GB). runbook 은 VM 이름을 `<타깃 VM 폴더>` 처럼 자리로 두고, 사용자가 채우게 한다.
    - 사용자 답 (2026-10-25): 지금 켜져 있는 VM (`WIN11-26H1-HDAUDIO` recovery) 은 **다른 작업 중 — 이번 분석과 무관**. runbook 은 그 VM 을 건드리지 않게 한다 (스냅샷·`vmrun` 대상으로 쓰지 말 것).
    - **사용자 답 (2026-10-25): 타깃 VM = `bob_baseline_vm`** (displayName "BOB Windows 11 24H2", 8 GB, 4 vCPU), **디버거 VM = `Windows 11 x64`** (16 GB, 16 vCPU). runbook 에 이 이름을 박아 넣는다 (`<TVM>` = `bob_baseline_vm`). 0-D 의 "고르기" 는 "확인" 으로 바꿔도 된다.
    - **디버거 VM 에 WinDbg·커널 디버깅 준비가 아직 없다 (사용자 답).** → runbook 을 **세팅부터** 시작한다 (아래 "세팅 (이번에 처음)" 절).
    - 스냅샷을 VMware 에서 지우면 그 `.vmem` 도 사라질 수 있으니 `ms/dump/01_snapshot-test/` 로 **복사** 해 둔다 (옮기기 아님).
  - 예비 방법 = 디버거 VM 의 WinDbg `.dump /f` (전체 메모리 덤프). 스냅샷에서 메모리 파일을 못 찾을 때만.

## 세팅 (이번에 처음) — runbook 앞에 넣을 것

오케스트레이터가 서버에서 읽기만 해서 본 것 `[관찰]` (vmx 파일, `/etc/vmware`):

- 두 VM 다 `firmware = efi`, NIC `e1000e`, `ethernet0.connectionType = nat` (vmnet8). vmnet8 = `192.168.100.0/24`, 호스트 쪽 `192.168.100.1`, DHCP 범위 `.128 ~ .254`. 스킬 표의 IP (타깃 `.133`, 디버거 `.132`) 와 같은 대역이지만 **DHCP 라 실제 IP 는 VM 안에서 `ipconfig` 로 확인** 하게 한다.
- `vmx` 에 `uefi.secureBoot.enabled` 줄은 없다 (Secure Boot 상태는 타깃 VM 안에서 확인하게: `msinfo32` 또는 `Confirm-SecureBootUEFI`). CLAUDE.md 상 타깃은 Secure Boot 꺼짐이 전제.
- 타깃 `bob_baseline_vm` 은 지금 **일시정지(suspend) 상태** (`checkpoint.vmState = ...-02ca0be3.vmss`, 10-17). 켜면 그 상태에서 이어진다. 타깃에 KDNET 이 이미 설정돼 있는지는 모른다 → `bcdedit` 로 먼저 본다.
- 타깃에는 공유 폴더가 있다: 호스트 `/home/mjbin/Downloads` ↔ guest `Downloads` (VMware Tools 공유 폴더, `\\vmware-host\Shared Folders\Downloads` 로 보일 것 `[추측]`). test.exe 가 타깃에 없으면 이 길로 넣을 수 있다 (sample 빌드 위치는 repo `sample/`). 디버거 VM 은 공유 폴더 설정 없음.
- 호스트 메모리 46 GB, 지금 쓸 수 있는 것 약 35 GB `[관찰: free -g]`. 이미 다른 작업 VM 16 GB 가 켜져 있는데, 타깃 8 GB + 디버거 16 GB 를 더 켜면 합 40 GB 라 빠듯할 수 있다 `[추측]`. 디버거 VM 메모리를 켜기 전에 줄이는 것 (예: 8 GB) 을 **선택지로** 안내해 달라 (WinDbg 만 돌리면 충분할 것 `[추측]`). 정하는 건 사용자.

runbook 세팅 단계에 넣을 것 (순서·명령은 dast-mentor 가 스킬대로 정한다):

1. 디버거 VM (`Windows 11 x64`): WinDbg 설치 (방법은 dast-mentor 가 고름 — Store / `winget` 등), 심볼 경로 `srv*C:\symbols*https://msdl.microsoft.com/download/symbols`, `C:\logs`·`C:\symbols` 폴더, 자기 IP 확인, 방화벽에서 KDNET 포트 (50000/UDP) 받기.
2. 타깃 VM (`bob_baseline_vm`): 켜기 (suspend 에서 이어짐), `ver` 로 26100.9457 확인, `bcdedit` 로 지금 디버그 설정 확인 → 없으면 `bcdedit /debug on` + `/dbgsettings net hostip:<디버거 IP> port:50000` (나온 key 를 적어 둠), 재부팅이 필요하다는 것과 **켜는 순서** (디버거 WinDbg 를 먼저 기다리게 → 타깃 재부팅).
3. 연결 확인: WinDbg 에 연결 메시지, Break → `g` 로 타깃이 다시 도는지.
4. test.exe 와 dll 이 타깃 0-C 경로에 있는지. 없으면 넣는 법 (위 공유 폴더).
- 세팅이 끝난 상태를 다시 쓰기 좋게, 세팅 직후 타깃·디버거 스냅샷을 찍을지 **선택지로** 안내 (이름 예: `kd-ready`). 이건 메모리 덤프용이 아니라 되돌리기용.
- 세팅 중 막히면 그 단계 출력을 붙여 달라고 체크포인트를 둔다.

## 확인할 것 (정적으로 못 본 것)

1. test.exe 첫 스레드가 `ntdll!LdrInitializeThunk` 입구 (rva `0xe1420`, sast/38) 에 섰을 때, 그 순간의 메모리 통째 파일을 얻을 수 있나.
2. 그 파일의 이름·크기, 뜨는 데 걸린 시간, 서버로 옮기는 데 걸린 시간 (대략이면 됨).
3. 서버 도구가 나중에 맞춰 볼 대조값 (아래 "찍을 것").

## 멈출 곳 후보

| 지점 | 멈출지 / 기록만 | 찍을 것 | 근거 |
|---|---|---|---|
| ① `ntdll!LdrInitializeThunk` 입구, **test.exe 일 때만** | 멈춤 | 아래 "대조값" 전부 → 그다음 스냅샷 | plan Q77, sast/38 추천 멈춤점; 유저 첫 위치가 여기인 까닭은 run16 `[정적]` |

- test.exe 프로세스가 아직 없을 때 어떻게 걸지 (프로세스 생성 시점에서 먼저 멈추고 `/p` 필터로 거는 등) 는 dast-mentor 가 스킬대로 정한다. 모든 프로세스에 걸리는 조건 없는 커널 멈춤점은 쓰지 않는다.

### 대조값 (멈춘 상태에서, 스냅샷 **전에** 찍는다)

- 지금 프로세스가 test.exe 인지: `!process -1 0` (EPROCESS 주소, `DirBase`, 이름)
- 지금 스레드: `!thread -1 0` 또는 같은 뜻의 명령 (ETHREAD 주소, TEB 주소)
- `k` (콜스택 몇 줄) 와 `r` (레지스터, 특히 `rip`, `rcx` — `rcx` 가 맡겨 둔 문맥 주소, run16)
- `lm m ntdll` · `lm m test` (모듈 베이스)
- 몇 칸 값: `_EPROCESS` 의 `UniqueProcessId`·`Peb`·`ImageFileName`, `_PEB` 의 `ImageBaseAddress`·`Ldr`·`ProcessHeap`·`ProcessParameters` (명령 형태는 dast-mentor 가 정함)
- 타깃 VM 메모리 크기 (VMware 설정에서 보이는 값) 를 로그 끝에 한 줄로 적게 한다.

### 스냅샷 (대조값 다 찍은 뒤, **디버거에서 멈춘 그대로**)

- 호스트 VMware 에서 타깃 VM 스냅샷 (메모리 포함). 이름에 `01_snapshot-test` 를 넣는다.
- 스냅샷이 만든 파일 중 메모리 파일 (`.vmem`) 과 상태 파일 (`.vmsn`) 을 찾아 이름·크기를 로그에 적는다. 위치는 호스트 OS 와 VMware 설정에 따라 다를 수 있다 `[추측]` — runbook 에 찾는 법을 적는다.
- 서버로 옮긴다: 대상 `/home/mjbin/prj/bob/teamprj/windows-pe-internals/ms/dump/01_snapshot-test/` (폴더 없으면 만든다). 압축은 해도 되고 안 해도 된다 — 했으면 방법을 적는다. 걸린 시간을 적는다.
- 스냅샷 찍는 동안·뒤에 디버거 연결이 끊기거나 이상하면 그대로 로그에 적는다 (디버거·VMware 때문에 생긴 일로 따로 기록).

## 성공 기준

- 로그에 다음이 다 있으면 "분석성공":
  - ① 에서 섰고, 그때 `!process -1 0` 이 test.exe, `rip` 가 `ntdll!LdrInitializeThunk` (ntdll 베이스 + `0xe1420`) 다.
  - 대조값 (EPROCESS·DirBase·ETHREAD·TEB·PEB 주소, ntdll·test 베이스, 위 칸 값들) 이 찍혀 있다.
  - 스냅샷 메모리 파일 이름·크기, 서버에 옮긴 경로가 적혀 있다.
- 스냅샷에서 메모리 파일을 못 얻어 `.dump /f` 로 바꿨으면 그것도 성공으로 본다 (파일 이름·크기·시간 기록).

## 끝 지점

- 스냅샷을 찍고 파일 위치를 확인했으면 멈춤점을 지우고 `g` 로 test.exe 를 끝까지 보낸다. 다른 지점에서는 멈추지 않는다.
- 스냅샷을 되돌리기(restore) 하지 않는다 (이번엔 파일만 필요).

## 범위 밖

- ② `NtContinue` 와 추가 지점 ⓪·①-a·①-b — 시험 결과 (크기·시간) 를 보고 본 덤프 요청서에서 정한다.
- 구조체 전체 `dt`, `!vad`, `!handle`, ntdll `.data` 덤프 — 서버 도구가 덤프에서 읽는다.
- 서버에서 덤프를 읽는 도구 만들기·설치 (하네스 변경, 사용자 검토 뒤 따로).

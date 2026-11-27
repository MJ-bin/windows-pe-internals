# VM 한 번에 넓게 뜨기 (PE_emulator 380 · D182 · D184 ~ D190)

PE_emulator 가 쓸 "진짜 Windows 사진" 을 한 번에 넓게 떠 오는 도구.
무엇을 왜 뜨는지는 `../PE_emulator/_workspace/05_tuning_planner/380_vm-collect-wishlist.md` (A · C 번호) 와
`../PE_emulator/_workspace/00_planner/380_ms-layout-needs.md` (나무 꼴 · 폴더별 뜨기 방식) 를 따른다.

## 들어 있는 것

| 파일 | 하는 일 |
|---|---|
| `collect_vm.ps1` | VM 에서 돌리는 본체. 다섯 모드: `Check` → `User` → `Net` ×2 → `DryRun` → `Full` |
| `VmCommon.cs` `VmReg.cs` `VmNt.cs` `VmUi.cs` `VmDisk.cs` | `collect_vm.ps1` 이 처음에 컴파일하는 C# (Windows PowerShell 5.1 `Add-Type`) |
| `verify_bundle.py` | 호스트 (서버) 에서 sha256 · manifest · content 를 확인 |
| `../vm_probe.c` · `../build_vm.bat` | 메시지 상자 · 대화상자 · 디버그 이벤트 시료 (`vm_probe.exe`) 와 그 빌드 (+ `winhttp_once.exe`) |

## 뜨는 것 · 안 뜨는 것

- **옛 꼴 그대로** (PE_emulator `ref_reg_users.py` 가 엄격): `registry/` 옛 여섯 jsonl · `disk/tree.csv` 6 칸 · `identity.csv` 앞 3 칸 · `manifest.csv`.
  - 바뀐 것은 하나: `tree.csv` 에 `%windir%` 의 **파일 줄도** 들어감 (A9). 그리고 사용자 `Desktop` · `Documents` · `Downloads` 는 `skip` 줄 (C10, 규칙 D2).
- **옆 파일 (새)**: `registry/user_default.jsonl` · `user_localservice.jsonl` · `user_networkservice.jsonl` (A15) · `registry/key_meta.jsonl` · `registry/roots.jsonl` (A16) · `sd/sd_table.jsonl` (A5) · `disk/tree_meta.csv` (A9 · A5) · `disk/content.csv` · `sysinfo/` · `objects/` · `probe/`.
- **파일 내용 (C/)**: `Windows` 는 기본 내용 (기록 · 상태 · 캐시 폴더는 이름만), `WinSxS` 는 Manifests + Win32 SxS 만, MS 작은 폴더 · `WindowsApps` 앱 프레임워크 · 한국어 언어 팩 · Defender Platform · 셸 Caches · `desktop.ini` 는 내용, 나머지는 이름 · 크기만. 규칙 전체는 `collect_vm.ps1` 의 `$ContentRules` (첫 줄이 이김) 와 `info.txt` `content_rule` 줄.
- **계속 안 뜸**: `SAM` · `SECURITY` · `BCD00000000` 내용 (열 때 status + 뿌리 SD 만) · 다른 사용자 하이브 · 하이브 파일 · `DigitalProductId` · password · productkey 값 (V1) · 사용 기록 키 (K1 · K2) · 브라우저 캐시 (D1) · 열쇠 · 자격 폴더 내용 (`Crypto` · `Protect` · `Credentials` · `Vault`, 이름만) · VMware 이름이 든 폴더 · 파일 내용 (이름만).
- **VM 고유 값**: 바꾸지 않고 날 값 그대로 뜬다. 무엇이 VM 고유 값인지는 `identity.csv` (`Kind,Value,Source,Where`) 와 `info.txt` 의 `vm_unique_kinds` · `vm_unique_also`.
  - Kind: `user_name` · `user_domain` · `user_sid` · `user_profile` · `computer_name` · `dns_host_name` · `machine_guid` · `interactive_user` (옛 여덟) + `machine_sid` · `logon_sid` · `volume_serial` · `volume_guid` · `smbios_uuid` · `bios_serial` · `baseboard_serial` · `hw_profile_guid` · `product_id` · `sqm_machine_id` · `sus_client_id` · `install_date` · `last_boot` · `mac` · `adapter_guid` · `ip_address` · `disk_serial` · `disk_model` · `gpu_name`.
  - 그 밖에 값 안에 섞여 있는 곳: SMBIOS 통째 (`sysinfo/firmware.jsonl`) · 토큰 · SD (사용자 · 기계 · 로그온 SID) · 객체 이름 (pid 든 이름 · 볼륨 GUID · VMware 장치) · 프로세스 · 서비스 · 드라이버 · 창 목록 (VMware 도구) · WMI.

## 정해 둔 것 (사용자 "추천대로", 2026-11-27)

| 물음 | 고른 것 |
|---|---|
| wishlist Q1 옛 묶음까지 통째로 다시? | **통째** — 한 번 돌리기 = 한 시각의 사진. 옛 파일 꼴은 그대로 |
| wishlist Q4 `DigitalProductId` | **안 뜸** (V1 그대로) |
| wishlist Q5 A17 .NET 네이티브 이미지 (1 ~ 2 GB) | **뜸** (`Windows\assembly\NativeImages_*` 내용) |
| wishlist Q6 A7 목록 보이기 | **다 뜸**, 보이기는 PE_emulator 가 정함 |
| (Q2 · Q3 는 D189 · D190 로 이미 정해짐) | identity 넓힘 · `Windows` 내용 넓이 |
| I1 `C/` 철자 | 디스크 이름 그대로 (`Windows` — 뿌리 목록에서 읽은 철자) |
| I2 나르기 | 부분마다 tar.gz (`registry` · `meta` · `content`) + `.sha256`. 풀고 맞으면 tar 는 internals 한 곳에만 |
| I3 dry run | `DryRun` 모드가 규칙별 · 폴더별 파일 수 · 바이트를 찍고 멈춤 → 분석 쪽이 보고 OK 한 뒤 `Full` |
| I4 출력 자리 | **C: 밖** (둘째 가상 디스크나 공유 폴더). C: 에 두려면 `-AllowSystemDrive` — 그 폴더는 `skip` 줄이 되고 `info.txt` 에 이름이 남음 |
| I5 시각 칸 | FILETIME 16 진 16 자리 |

## 시간 · 디스크 어림 `[추측]`

| 모드 | 시간 | 메모 |
|---|---|---|
| Check | 1 분 안 | 빌드 확인 · C# 컴파일 · 빈 자리 · 도구 |
| User | 1 ~ 3 분 | 글꼴 목록이 가장 김 |
| Net ×2 | 각 1 분 안 | 랜선 뽑고 한 번, 꽂고 한 번 |
| DryRun | 3 ~ 10 분 | 디스크 이름만 훑음 (옛 75 초 + `%windir%` 파일 줄) |
| Full | **1 ~ 2 시간** | 레지스트리 5 분 · 트리 5 분 · 메타 10 ~ 20 분 · 내용 해시 10 ~ 20 분 · 그 밖 5 ~ 10 분 · **content.tar.gz 압축 30 ~ 60 분** |

- 내용 (풀린 것) 은 dry run 이 정확히 알려 줌 — 요구서 어림은 약 9 ~ 13 GB (A17 1 ~ 2 GB · A8 179 MB 포함). tar.gz 는 그 40 % 쯤.
- 출력 자리 빈 공간: **20 GB 이상** 권함 (`Full` 이 확인함, `-Force` 로 넘길 수 있음). 내용은 C: 에서 바로 tar 로 읽어서 따로 복사본을 만들지 않는다.

## 실행 안내

### 0. 옮기기 (호스트 → VM)

- 이 레포의 `sample\` 폴더 통째를 VM 의 **C: 밖** (예: `E:\pe\sample\`) 에 둔다. C: 에 두면 새 흔적이 남는다 (D107 leftover).
- 출력 자리도 C: 밖: 둘째 가상 디스크 (예: `E:\vmcollect`, 40 GB 이상) 또는 VMware 공유 폴더.
- VM 빌드가 `26100.9457` 이어야 한다 — `collect_vm.ps1` 이 첫 줄에서 보고, 다르면 아무것도 안 쓰고 멈춘다.

### 1. 빌드 (VM, x64 Native Tools Command Prompt — 관리자 아님)

```
E:\pe\sample> build_vm.bat
```
→ `bin\vm_probe.exe` · `bin\winhttp_once.exe` (winhttp 는 다음 RPC 동적 묶음용).

### 2. Check · User · Net (VM, **보통** PowerShell — "관리자 권한으로 실행" 아님, 64 비트)

```
Set-ExecutionPolicy -Scope Process Bypass
cd E:\pe\sample\vm-collect
.\collect_vm.ps1 -Mode Check  -Dest E:\vmcollect
.\collect_vm.ps1 -Mode User   -Dest E:\vmcollect
# VMware 에서 네트워크 어댑터 "Connected" 를 끈 뒤
.\collect_vm.ps1 -Mode Net    -Dest E:\vmcollect -NetTag unplugged
# 다시 켜고 주소를 받은 뒤 (30 초쯤)
.\collect_vm.ps1 -Mode Net    -Dest E:\vmcollect -NetTag plugged
```

### 3. DryRun (VM, **관리자** PowerShell, 64 비트)

```
Set-ExecutionPolicy -Scope Process Bypass
cd E:\pe\sample\vm-collect
.\collect_vm.ps1 -Mode DryRun -Dest E:\vmcollect
```
- 화면의 두 표 (규칙별 · 큰 폴더 25) 와 마지막 줄 `content total` 을 **분석 쪽 (Claude) 에 붙여 넣고 OK 를 받는다.** 파일은 `E:\vmcollect\win11-24H2-26100.9457\dryrun.csv` · `dryrun_top.csv`.
- 줄이거나 넓힐 폴더가 있으면 `$ContentRules` 를 고친 판으로 다시 DryRun.

### 4. Full (VM, 관리자 PowerShell)

```
.\collect_vm.ps1 -Mode Full -Dest E:\vmcollect
```
- 끝 가까이 메시지 상자 · 대화상자 창이 잠깐씩 떴다 닫힌다 (`vm_probe.exe` 가 스스로 Enter 를 넣음). 건드리지 않는다.
- 끝나면 `=== summary ===` 와 `archive : ... exit 0` 세 줄을 확인. `exit` 이 0 이 아니면 `tar_*.log` · `errors.log` 를 같이 가져온다.

### 5. 가져오기 (VM → 서버)

- `E:\vmcollect\win11-24H2-26100.9457\` 폴더 **통째** 를 서버의 `~/prj/bob/teamprj/windows-pe-internals/ms/bin/_incoming/win11-24H2-26100.9457/` 로 복사 (WinSCP · scp 등). VM 에서 풀지 않는다.
- 그 폴더 안: `info.txt` `manifest.csv` `errors.log` `identity.csv` `dryrun.csv` `dryrun_top.csv` `tar_*.log` + `registry.tar.gz` `meta.tar.gz` `content.tar.gz` (+ `content_nonansi.tar.gz`) 와 `.sha256` 들.

### 6. 확인 · 풀기 (서버)

```
cd ~/prj/bob/teamprj/windows-pe-internals/ms/bin/_incoming/win11-24H2-26100.9457
sha256sum -c *.sha256                     # 모두 OK 여야 함
B=../../win11-24H2-26100.9457             # = ms/bin/win11-24H2-26100.9457 (PE_emulator 가 cp -al 로 가져갈 원본)
mkdir -p $B/C
tar xzf registry.tar.gz -C $B
tar xzf meta.tar.gz     -C $B
tar xzf content.tar.gz  -C $B/C
[ -f content_nonansi.tar.gz ] && tar xzf content_nonansi.tar.gz -C $B/C
cp info.txt manifest.csv errors.log identity.csv dryrun.csv dryrun_top.csv $B/
python3 ~/prj/bob/teamprj/windows-pe-internals/sample/vm-collect/verify_bundle.py . $B   # 끝에 ALL OK
```
- `verify_bundle.py` 가 보는 것: 빌드 줄 · tar 의 sha256 · tar exit 0 · `manifest.csv` 모든 줄 (크기 · sha256) · `tree.csv` 머리 6 칸 · `content.csv` 모든 줄 (`C/` 파일 크기 · sha256). 빨리 보려면 `--content 2000`.
- tar 는 `_incoming/` 에 그대로 둔다 (I2 — internals 한 곳에만). `ms/bin/` 은 커밋되지 않는다.
- PE_emulator 가 새 묶음을 읽어 넣는 일은 PE_emulator 바퀴 몫 (여기서는 안 함).

## 알아 둘 것

- 이 스크립트는 서버에서 Windows 없이 썼다 — **실행해 본 적이 없다.** 그래서 `Check` 가 C# 컴파일부터 한다. 컴파일 오류 · 모드마다의 경고 (`errors.log` 의 `STEP` 줄) 는 분석 쪽에 붙여 넣으면 고친 판을 준다.
- 한 단계가 실패해도 다음 단계로 간다 (실패는 `errors.log`). 레지스트리 · 디스크 · 묶기는 멈추지 않고 끝까지 간다.
- 디버거가 있어야 하는 것 (wishlist B) 은 여기 없다 — 대기열의 동적 항목 (RPC 묶음 Q280 · Q283 등, GUI 묶음 Q273 · Q277, WoW64 묶음, Q297) 으로 따로 간다.

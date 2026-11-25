# 140 mountdev-name — 정적 분석 요청

- 날짜: 2026-11-25
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q269** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/335_internals-mountdev-name.md` — **원문을 먼저 읽을 것**. 에뮬레이터 로그의 syscall 꼴 (접근·공유·옵션 값) 과 예외 자리가 거기 있다). 사용자 결정 2026-11-25 (PE_emulator D154) 의 셋째. **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64.
  - kernelbase: **analyze 됨** — `^이름$` export 만. `_index.tsv` 후보: `GetVolumeNameForVolumeMountPointW` (0x268f0, 533 B) · `GetVolumePathNameInternalW` (0x26b10, 1985 B) · `BasepGetVolumeGUIDFromNTName` (0x2a2a8, 972 B) · `BasepGetVolumeDosLetterNameFromNTName` (0x27480) · `BasepGetVolumeNameFromReparsePoint` (0x25950) · `GetFinalPathNameByHandleW` (0x28ac0) · `GetVolumePathNamesForVolumeNameW` (0xe4a10). **어느 것이 `0x4d0008` 을 부르는지는 코드로 찾는다** (이름 짐작 금지 — IOCTL 상수 `0x4d0008` 을 즉시값으로 싣는 줄을 찾기).
  - mountmgr.sys (`System32/drivers/mountmgr.sys`): **처음 analyze** (PDB 받기 포함). 질문 2 에서 IOCTL 처리 갈래 (디스패치 → 그 코드 갈래) 만 export.
  - (되면, 질문 3) shell32.dll / windows.storage.dll — **처음 analyze 가 무거우면 하지 말고** "다음 요청 후보" 로 남김.
- 주의: 같은 시간에 sast/138 (rpcrt4·ntdll·ntoskrnl) · sast/139 (user32·win32kfull) decompiler 가 돈다. 메모리를 보고 mountmgr analyze 를 건다 (작은 드라이버).
- 표시: IOCTL 이름 (`IOCTL_MOUNTDEV_QUERY_DEVICE_NAME` 등) · `MOUNTDEV_NAME` 꼴은 공개 헤더 지식 → `[추측: 공개 헤더]` 로 적고, **바이트 근거는 코드 줄**로.

## 알고 싶은 것

1. **`0x4d0008` 을 부르는 함수** — kernelbase 에서 `NtDeviceIoControlFile(…, 0x4d0008, 입력 0, 출력, 0x208)` 을 부르는 함수 이름과 호출 자리 (+오프셋), 그 앞 `NtQueryInformationProcess(0x17)` 둘 · `NtOpenFile(\??\C:, 0x100080, 공유 3, 옵션 0x10)` 이 같은 함수에서 나오나.
   - 출력 버퍼 (0x208 B) 에서 읽는 칸: 길이 칸 폭 (2 B?) · 이름 시작 오프셋 · 길이 단위 (바이트/글자) — 줄 근거.
   - status 별 가지: 성공 · `0x80000005` (버퍼 넘침 — 더 큰 버퍼로 다시 부르나) · 그 밖 실패 때 호출자가 받는 값 (FALSE + `SetLastError` 값).
   - 에뮬레이터는 "성공인데 출력 안 씀" 을 줬고 그 뒤 `RtlCopyMemory` 가 스택 끝을 넘어 읽었다 → **길이 칸을 검사 없이 복사 길이로 쓰는 줄**이 있나 (몇 바이트까지 믿나).
2. **바로 뒤 `\??\MountPointManager`** — `NtCreateFile(\??\MountPointManager, 0x100080, 속성 0x80, 공유 3, 열기 1, 옵션 0x20060)` 뒤 보내는 IOCTL:
   - 코드 · 입력 구조 칸 (장치 이름을 어떻게 싸나 · 길이 칸) · 출력에서 읽는 칸 (`\??\Volume{GUID}\` 꼴 이름을 어디서 꺼내나) · 실패 때 가지 — kernelbase 쪽 줄로.
   - mountmgr.sys 쪽: 그 코드 갈래가 입력을 검사하는 차례 · 실패 status (길이 짧음 · 이름 없음) — 이름 표(장부) 본문은 이름만.
   - `NtCreateFile` 이 실패 (에뮬레이터 지금 `path_prefix_unsupported`) 하면 kernelbase 는 무엇을 돌려주나.
3. (되면) **shell 쪽** — `ShellExecuteW` 안에서 이 kernelbase 함수의 실패 (FALSE) 를 받으면 그냥 지나가나 멈추나 ("정직한 실패" 로도 길이 열리나). 부르는 자리를 찾기 어렵거나 analyze 가 무거우면 "다음 요청 후보".

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| kernelbase | 위 후보 중 `0x4d0008` 을 싣는 함수 (+ 그 caller 한 단계 — export API 까지) | 1 | 질문 1 |
| kernelbase | 같은 함수 또는 이웃의 `MountPointManager` IOCTL 자리 | 1 | 질문 2 |
| mountmgr.sys | 디스패치 (`IRP_MJ_DEVICE_CONTROL`) → 질문 2 코드 갈래 | 0~1 | 질문 2 |
| (되면) shell32 / windows.storage | 그 kernelbase API 를 부르는 자리 | 0 | 질문 3 |

- 범위: 볼륨 이름 묻기 두 IOCTL 의 입력·출력·status. volmgr·파티션 드라이버 본문, mountmgr 장부 갱신은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/140_mountdev-name_result.md`.
  1. asm 을 남겨 줌. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1) syscall 차례 표 (`차례 · syscall · 인자 · 결과 갈래 · 줄`) — 에뮬레이터 로그 꼴과 나란히 · (2) 출력 버퍼 칸 표 (`IOCTL · 오프셋 · 크기 · 뜻 · 읽는 줄`) · (3) 입력 구조 칸 표 (MountPointManager IOCTL) · (4) status → 반환값 표.
  4. **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: 팀원 `ext/syscall/syscall_layer_cpp/src/file/nt_device_io_control_file.cpp:617 ~ :654` (볼륨 핸들 IOCTL) · `src/file/nt_create_file.cpp` (`\??\MountPointManager` 장치). **"엔진이 돌려줄 최소 바이트"** (C: = `\Device\HarddiskVolume3` 일 때 `0x4d0008` 출력 바이트 꼴, MountPointManager 출력 꼴) 와 **"정직한 실패" 로 줄 status** 둘 다.
  5. PDB GUID 확인 줄 (mountmgr 새로 받은 것 포함).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

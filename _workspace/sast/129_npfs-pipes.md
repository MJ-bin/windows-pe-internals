# 129 npfs-pipes — 정적 분석 요청

- 날짜: 2026-11-22
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **새 Q241** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/298_internals-npfs-pipes.md` — "파이프 (이름 있는·없는) — kernelbase 가 커널에 무엇을 넘기고, npfs 가 만들기·열기·읽기·쓰기에 무엇을 돌려주나", 튜닝 `298_tune10.md` T7′, 병목 25 · 파이프 만들기 샘플 77). 사용자 결정 2026-11-22 "이 세 요청 먼저 가장 우선". **정적만** (VM 은 미룸 — 결과서 "동적으로 확인할 것" 에 멈춤점만).
- **요청 원문을 먼저 읽는다**: 위 파일 (38 줄). 에뮬레이터 로그의 syscall 인자 값 (access·share·disposition·options·종류·쿼터) 이 다 거기 있다. 이 요청서는 그 질문을 옮기고 범위를 정한다.
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `KernelBase.dll` — 이미 analyze 됨 (`CreateFileW`·`CreateFileInternal`·`ReadFile`·`WriteFile`·`GetFileType` `.asm` 있음). 다시 analyze 하지 말고 `^이름$` export 만.
  - `ntoskrnl.exe` — 이미 analyze 됨 (`IopCreateFile`·`IopParseDevice`·`NtFsControlFile`·`IopXxxControlFile`·`IopSynchronousServiceTail`·`IopCompleteRequest` `.asm` 있음). `^NtCreateNamedPipeFile$` 만 export.
  - `drivers/npfs.sys` (120,264 B) — **아직 analyze 안 됨, PDB 도 없음** → `fetch_pdb.py` 로 먼저 받고 GUID 확인 (afd·condrv 때처럼). 작음.
- 이어 쓰는 것: sast/124 (IRP 완료 — IOSB·이벤트·포트), sast/125 (파일 완료 포트), sast/121·123 (afd 드라이버 들머리 읽은 꼴), run98 (smss 가 `\Device\NamedPipe\` 보안 다는 줄).
- 주의: 같은 시간에 다른 decompiler 가 **user32·combase** (sast/128) 를 돈다 — 그쪽은 건드리지 않는다. analyze 는 서버 메모리를 보고 한 번에 하나.

## 왜 (요청한 쪽 말 요약)

- 이름 있는: `CreateNamedPipeA("\\.\pipe\MSSE-<수>-server", …)` 가 0 → 다른 스레드의 `CreateFileA(같은 이름)` 가 −1 → `Sleep` 뒤 되풀이. 20 / 20 같은 꼴 (진짜라면 이 길로 넘긴 코드가 실행됨).
- 이름 없는: `CreatePipe` 가 참인데 두 핸들 0 (세 syscall 다 기본 대답). 37 중 26 이 자식 프로세스를 만듦 = 자식 표준 출력 길.
- 에뮬레이터 로그의 syscall 인자는 Unicorn 안에서 돈 **진짜 kernelbase 코드** 가 낸 값 (대답 뒤 길은 진짜 아님).

## 알고 싶은 것 (★ = 이번에 꼭, 나머지는 시간 되면)

1. **kernelbase 가 넘기는 것**
   - (a) ★ `CreateNamedPipeW`: Win32 인자 (`dwOpenMode`·`dwPipeMode`·`nMaxInstances`·버퍼 크기 둘·`nDefaultTimeOut`) → `NtCreateNamedPipeFile` 14 인자 옮김 표. 요청 원문의 `[추측]` 셋 (disposition `0x2` ↔ `FILE_FLAG_FIRST_PIPE_INSTANCE` · options `0x0` ↔ `FILE_FLAG_OVERLAPPED` · 종류 `0x2` 비트 ↔ `PIPE_REJECT_REMOTE_CLIENTS`) 이 맞나. 시간 0 일 때 넣는 값. 실패 때 돌려주는 값 (`INVALID_HANDLE_VALUE`) 과 오류값을 정하는 길. (A 판은 이름만 — W 로 가는지 한 줄)
   - (b) ★ `CreatePipe`: 세 syscall (① `NtOpenFile("\Device\NamedPipe\")` ② `NtCreateNamedPipeFile(이름 "")` ③ `NtOpenFile(이름 "")`) 의 인자가 로그와 같은가 · ② ③ 의 root 가 무엇인가 (① 의 핸들 / ② 의 핸들) · `nSize` 0 일 때 쿼터 `0x1000` 근거 · `lpPipeAttributes` 상속 표시가 어느 syscall 의 `OBJ_INHERIT` 로 가나 · 중간 실패 때 무엇을 닫고 무엇을 돌려주나 · ① 핸들을 끝에 닫나 (전역에 두고 다시 쓰나).
   - (c) ★ `ConnectNamedPipe`: `0x110008` 을 내는 꼴 (동기 / `OVERLAPPED` — `NtFsControlFile` 인자) · status 별 돌려주는 값·오류값 (`STATUS_PIPE_CONNECTED` → 참 + `ERROR_PIPE_CONNECTED` 류인지, `STATUS_PENDING` 일 때 동기면 기다리나).
   - (d) `WaitNamedPipeW`·`PeekNamedPipe`·`SetNamedPipeHandleState`·`DisconnectNamedPipe`: 내는 제어 코드 · 입출력 버퍼 크기 · 어느 핸들에 내나만 (`WaitNamedPipeW` 는 `\Device\NamedPipe\` 루트를 여는지도).
2. **npfs.sys 만들기·열기·닫기** — 드라이버 시작 함수 (`DriverEntry` / `NpFsdInitialize` 류) 가 채우는 `MajorFunction` 표에서 들머리를 잡는다 (CREATE · CREATE_NAMED_PIPE · READ · WRITE · FILE_SYSTEM_CONTROL · QUERY/SET_INFORMATION · QUERY_VOLUME_INFORMATION · CLEANUP · CLOSE). 안쪽 구조체 칸 오프셋은 **필요 없음** (요청한 쪽이 npfs 를 올리지 않고 겉 동작만 냄) — 검사 차례와 status · `IoStatusBlock.Information` 만.
   - (a) ★ 서버 쪽 만들기 (ntoskrnl `NtCreateNamedPipeFile` → `IoCreateFile` 의 넘기는 꼴 한 줄 → npfs 파이프 만들기): 새 이름 / 이미 있는 이름 (disposition `0x2`·`0x3` 별) / 인스턴스 수 넘음 / 종류·읽기·공유 값이 첫 인스턴스와 다름 / 빈 이름 + root (이름 없는 파이프). access·share 에서 방향 (들어옴·나감·양쪽) 을 정하는 규칙.
   - (b) ★ 클라이언트 쪽 열기 (npfs CREATE): 없는 이름 / 서버가 아직 듣기 (`0x110008`) 를 안 부른 인스턴스 / 인스턴스가 모두 쓰이는 중 / 방향과 안 맞는 access / 끝 `\` 로 루트 열기 / 서버 핸들을 root 로 한 빈 이름 열기.
   - (c) ★ 닫기 (CLEANUP): 한쪽 끝을 닫으면 다른 끝의 상태가 무엇이 되나 (3 의 "상대가 닫힘" status 와 같은 값인지), 기다리던 읽기·듣기 IRP 를 무슨 status 로 끝내나.
3. **데이터 길 — 읽기·쓰기·듣기의 완료 꼴** (같은 프로세스 안 한 끝에 쓰고 다른 끝에서 읽는 것이 첫 걸음)
   - (a) ★ READ: 읽을 것이 없을 때 (PENDING 으로 큐에 거나 — 동기 핸들이면 I/O 관리자가 기다림, sast/124 줄 가리키기) · 쓰는 끝이 닫힌 뒤 (남은 자료 있을 때 / 없을 때 status — `STATUS_PIPE_BROKEN`·`STATUS_END_OF_FILE` 어느 쪽인가) · 버퍼보다 큰 자료 (바이트 꼴. 메시지 꼴 `STATUS_BUFFER_OVERFLOW` 는 있으면).
   - (b) ★ WRITE: 상대가 아직 안 붙음 / 닫힘 의 status · 쿼터를 넘는 쓰기가 기다리나 · `Information` 값.
   - (c) ★ 듣기 `0x110008`: 이미 붙어 있음 / 아직 없음 (PENDING) / 붙었다 닫음 (`STATUS_PIPE_CLOSING` 류) 의 status 와 완료 꼴.
   - (d) `GetFileType` (kernelbase `GetFileType.asm` 있음) 이 묻는 장치 종류 (`NtQueryVolumeInformationFile` `FileFsDeviceInformation`) 에 npfs 가 주는 `DeviceType` (`FILE_DEVICE_NAMED_PIPE` `0x11` 인지) — C 런타임이 표준 핸들마다 부름. `NtQueryInformationFile`·`NtSetInformationFile` 의 파이프 종류 (`FilePipeInformation` 등) 칸 모양은 PDB 에 타입이 있으면 (없으면 이름만).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| kernelbase | `CreateNamedPipeW` · `CreatePipe` · `ConnectNamedPipe` (+ 시간 되면 `WaitNamedPipeW`·`PeekNamedPipe`·`SetNamedPipeHandleState`·`DisconnectNamedPipe`) | 1 | 질문 1 |
| ntoskrnl | `^NtCreateNamedPipeFile$` | 0 | 질문 2(a) 넘기는 꼴 |
| npfs | 시작 함수 (`MajorFunction` 표) · 만들기·파이프 만들기·CREATE · READ · WRITE · FSCTL 듣기 · CLEANUP · 볼륨 정보 | 1 | 질문 2·3 |

- 범위: 위까지. 안 묻는 것 (요청 원문 그대로): 원격 파이프 (`\\server\pipe\…`) · 클라이언트 가장 · 보안 서술자 · `TransactNamedPipe` · 메일슬롯 (msfs) · 시스템 서비스 파이프. npfs 안쪽 자료 구조 (CCB·FCB 칸) 는 이름만.
- 분량이 크면: ★ 중 **1(a)(b)(c) → 2(a)(b) → 3(a)(c) → 2(c) → 3(b)** 차례로 결과서에 먼저 쓰고, 나머지는 "다음 요청으로" 라고 적어도 된다.

## 받고 싶은 것

- 결과서 `_workspace/sast/129_npfs-pipes_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1a) `Win32 인자 → NtCreateNamedPipeFile 인자 · 바꾸는 규칙 · 줄` · (1b) `syscall · 인자 · root · 줄` · (2·3) `요청 종류 · 조건 · status · Information · 기다림 여부 · 줄`.
  4. PDB GUID 확인 줄 (npfs).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall/syscall_layer_cpp/src/file/nt_legacy_file_frontends.cpp:48` (`nt_create_named_pipe_file` — 지금 `named_pipe_filesystem_backend_unsupported`) · `src/file/nt_create_file.cpp:110`·`:189`·`nt_open_file.cpp` (`\??\pipe\…` `path_prefix_unsupported` · 빈 이름 `object_name_empty_unverified`) · `src/namespace.cpp:93` (`\??\PIPE` 링크) · `src/file/nt_file_misc.cpp:104` → `nt_device_io_control_file.cpp` (`0x110008`) · `nt_read_file.cpp`·`nt_write_file.cpp` · `include/syscall_layer/files.hpp`. 원칙: 호스트에 진짜 파이프를 만들지 않음.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (요청 원문: 같은 프로세스에서 `CreatePipe` → `WriteFile` → `ReadFile` → 쓰는 끝 `CloseHandle` → `ReadFile` 의 status 다섯 — 멈춤점 `caller+0x…`) 과 **"다음 요청 후보"**.

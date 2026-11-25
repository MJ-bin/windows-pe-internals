# PE_emulator 에 넘길 요약 — D154 요청 넷의 답

- 쓴 날: 2026-11-25 (windows-pe-internals 오케스트레이터)
- 이 파일은 **길잡이**다. 근거·오프셋·표는 run 문서의 "PE_emulator 쪽" 절에 있다 (아래 경로). 이 파일과 run 문서가 다르면 run 문서가 맞다.
- 표시: `[정적]` 디컴파일 코드 · `[관찰]` 디버거/사진 · `[추측]` · `[공백]`. 이 파일의 "지금 우리" = PE_emulator.
- 번호: 원 결정 (D154) 의 "Q260 ~ Q263" 은 우리 대기열에서 이미 다른 항목이 쓰고 있어 **Q267 ~ Q270** 으로 붙였다 (Q260 = Q267 · Q261 = Q268 · Q262 = Q269 · Q263 = Q270).
- 넷 다 **정적만**. VM 확인거리는 각 run 문서 "아직 확인 필요" 와 우리 대기열 (아래 "남은 것") 에 있다.

| 요청 (원문) | 우리 Q | 답한 run (최종 근거) | 정적 결과서 | 해설 |
|---|---|---|---|---|
| `00_planner/AS02_internals-async-bind.md` | Q267 | `runs/03_sprint/run141-async-bind.md` | `_workspace/sast/138_async-bind_result.md` | `run141-async-bind-해설.md` |
| `05_tuning_planner/335_internals-user32-connect.md` | Q268 | `runs/03_sprint/run142-user32-connect.md` | `_workspace/sast/139_user32-connect_result.md` | `run142-user32-connect-해설.md` |
| `05_tuning_planner/335_internals-mountdev-name.md` | Q269 | `runs/03_sprint/run143-mountdev-name.md` | `_workspace/sast/140_mountdev-name_result.md` | `run143-mountdev-name-해설.md` |
| `05_tuning_planner/343_internals-gdi-startup.md` | Q270 | `runs/03_sprint/run144-gdi-startup.md` | `_workspace/sast/141_gdi-startup_result.md` | `run144-gdi-startup-해설.md` |

---

## Q267 — 비동기 epmapper 연결 뒤 bind (AS02)

### 답 (한 줄씩) — run141

- **bind 는 부른 스레드가 아니라 풀 일꾼이 보낸다** `[정적]`. 비동기 첫 연결이면 `DriveStateForward` 는 `+0x515 Connect` 의 보류 `0xc0021018` 을 받고 거기서 끝나고, `Bind__74b50` 이 그것을 0 으로 바꿔 돌려준다 → 부른 쪽 (winnsi) 은 이벤트에서 잔다.
- **요청서 짐작 정정**: `DriveStateForward +0x5d4` 는 Tp 로 맡기는 자리가 아니라 같은 함수 `+0x43` 으로 도는 고리다. 맡김은 `SignalAsync +0x2f` → `TrySubmitWork` → `TrySubmitThreadpoolCallback` → `ntdll!TpSimpleTryPost` → `TppWorkPost`.
- **일꾼을 깨우는 것 = 서버의 연결 응답 메시지 (ALPC 종류 `0xb`) + 그 완료 패킷** `[정적: 받는 쪽 rpcrt4]` · `[추측: 커널이 그 길로 넣는지]`. 일꾼은 `IoCompleted` 에서 종류 `0xb` 를 처리 → 맡김 → 둘째 `DriveStateForward` (보안 문맥 4줄 — 동기와 같은 코드) → `LRPC_CASSOCIATION::Bind +0x626 NtAlpcSendWaitReceivePort` (bind).

### 지금 우리 맞나 · 틀리나

- 맞음: seq 2185 · 2191 (`NtAlpcConnectPort` Flags 0, SID 있음) · 2192 (`NtAlpcQueryInformation(0)` — 길 안 바뀜) · 2193 (`NtAlpcSetInformation(2)`) · 부른 쪽 `NtWaitForSingleObject` 무한 대기.
- **틀림 (멈춤의 직접 원인)**: 연결 응답 메시지와 완료 패킷이 없다. → `epm.cpp accepts_async_connect` 에서 클라이언트 포트 수신 큐에 **헤더 `0x28` B, `[+0x04] & 0xff == 0xb`, 나머지 0** 인 메시지 하나 + 풀 완료 포트에 `{키, 0, 0, 0}` (붙이기 전에 큐에 넣어 두면 `SetInformation(2)` 때 개수만큼 패킷이 나간다 — run137).
- **틀림**: 일꾼 `NtWaitForWorkViaWorkerFactory` 가 빈 포트에서 바로 `0x102` → `NtTerminateThread`. 진짜는 패킷이 올 때까지 잔다 (run125). 이것이 일꾼이 다 죽는 직접 원인.
- 연결 응답 뒤 일꾼 쪽 syscall 차례: run141 "PE_emulator 쪽" 의 "할 것" 셋째 글머리.

### 남은 것

- 진짜 커널이 넣는 연결 응답 헤더 칸 값 `[공백]` → 우리 **Q272 ①** (정적, 가장 급함). 동적 멈춤점 → Q271.

---

## Q268 — user32 창 API 넷의 가드 (335 user32-connect)

### 답 (한 줄씩) — run142

- `MessageBox*` · `DialogBoxParam*` · `LoadIcon*` · `GetSystemMetrics` 는 같은 꼴 가드를 **각자** 품는다: TEB `+0x78 == 0` 이면 `NtUserGetThreadState(0xe)` (win32u `0x1000`) → **반환 0 이면 실패 (0 / −1)**, 0 아니면 계속 `[정적]`.
- **win32kfull 종류 `0xe` 는 아무 칸도 안 읽고 상수 1** `[정적]`. 목적은 반환값이 아니라 win32k syscall 이라 커널이 스레드를 GUI 로 바꾸는 것 (`xxxCreateThreadInfo +0x756` 이 TEB `+0x78` 을 씀, `xxxClientThreadSetup` → `KeUserModeCallback(0x54)` → user32 `__ClientThreadSetup`).

### 지금 우리 맞나 · 틀리나

- **틀림 (넷 실패의 직접 원인)**: `NtUserGetThreadState(0xe)` 기본 대답 0 → **`ECX == 0xe` 면 `RAX = 1`**. 이것 하나로 넷 모두 가드를 지난다.
- (같이 하면 좋음) TEB `+0x78` 을 0 아닌 값 — 안 그러면 user32 85곳에서 이 syscall 이 계속 나감.
- 가드 뒤 첫 벽 (메시지 상자가 뜨려면): TEB `+0x820` ≠ 0 (0 채운 `0x48` B) · `psi+0x1b56` / `+0x1b66` ≠ 0 · `gSharedInfo+0x18` ≠ 0 (주 모니터 물체) · GDI 답 (Q270 과 맞출 것) · `NtUserFindExistingCursorIcon` · `NtUserCreateWindowEx` hwnd. 목록 8개: run142 "PE_emulator 쪽".
- **정할 것 (사용자 · PE_emulator)**: 모달 고리를 끝낼 입구는 `NtUserPeekMessage` 뿐 — `WM_COMMAND(IDOK)` 를 줄지 (→ 1) `WM_QUIT` 을 줄지 (→ 0) (`335_tune15.md` Q5).

### 남은 것

- 동적 Q273 (Q162 와 같은 세션) · 정적 Q274 (`InternalCreateDialog` 창 만들기 · `tagWND` 칸 · TEB `+0x820/+0x828` 채우기 · `GetSystemMetrics` 지표별 칸 · `LoadIcoCur` 자원 길).

---

## Q269 — `\??\C:` 볼륨 이름 묻기 (335 mountdev-name)

### 답 (한 줄씩) — run143

- `0x4d0008` 을 보내는 곳은 kernelbase **`GetVolumeNameForRoot`** (rva `0x24c30`, export 아님) `[정적]`. 출력 **앞 2 B = 이름 길이 (바이트), `+2` 부터 이름** — 그 길이를 **상한 검사 없이** 할당·`memcpy` 길이로 쓴다 → "성공인데 출력 안 씀" 이 스택 넘어 읽기로 이어진 자리. 음수 status 면 재시도 없이 FALSE.
- 다음 `\\.\MountPointManager` 에 **`0x6d0008`**: 첫 호출 출력 0x20 B → 진짜는 `0x80000005` + 필요 Size, kernelbase 는 그 크기로 **횟수 상한 없이** 다시 부른다. 항목에서 길이 정확히 `0x60` 인 `\??\Volume{GUID}` 를 골라 `\\?\Volume{GUID}\` 를 돌려준다.
- shell 쪽 (`windows.storage` `_FixupWorkingDirectory`) 은 FALSE 를 받아도 지나간다 `[정적: objdump · PDB 공개 이름]`. `ShellExecuteW` 가 그 사슬을 타는지는 `[공백]`.

### 지금 우리 맞나 · 틀리나

- **틀림 (빠짐)**: `0x4d0008` (볼륨 갈래는 지금 `nt_device_io_control_file.cpp:675 ~ :742` — 요청서의 `:617 ~ :654` 는 줄이 밀린 것). 성공을 주려면 **길이 칸 + 이름을 반드시** 쓴다.
- **틀림 (빠짐)**: `NtCreateFile(\??\MountPointManager)` → `stop("path_prefix_unsupported")` (`nt_create_file.cpp:209`). 멈춤이라 kernelbase 로 값이 안 감.
- 맞음: `NtQueryInformationProcess(0x17)` (`GetDriveTypeW` 안) · `NtOpenFile(\??\C:, 0x100080, 3, 0x10)`.
- 최소 답 바이트 (`0x4d0008` 0x30 B · `0x6d0008` 첫 호출 `0x80000005`+Size, 둘째 호출 0x80 B) 와 **정직한 실패 status** (`0x4d0008` → `0xC0000010`, MPM 열기 → `0xC0000034`, `0xC0000022` 는 피할 것): run143 "PE_emulator 쪽".
- **금지 꼴**: `0x80000005` 를 주면서 Size 를 안 쓰거나 계속 `0x80000005` → 끝없는 고리. **status 0 인데 출력을 안 쓰는 대답이 최악.**

### 남은 것

- 동적 Q275 · 정적 Q276 (windows.storage/shell32 지도 · `BasepGetVolumeNameFromReparsePoint` · partmgr/volmgr 등).

---

## Q270 — `GdiplusStartup` 과 GDI 공유 핸들 표 (343 gdi-startup)

### 답 (한 줄씩) — run144

- `GdiplusStartup` 이 1 로 나가는 첫 가지 = **`InternalGdiplusStartup +0x1c3 CreateRectRgn(0,0,1,1)` 이 NULL → `+0x1d9 JZ`** `[정적]`. NULL 은 gdi32 `CreateRectRgn` 이 만든다: 커널이 준 핸들을 **PEB `+0xf8` 공유 핸들 표** 에서 다시 읽어 칸 종류 (`+0xe` == 4) · 윗 16비트 (`+0xc`) · 주인 PID (`+0x8`) · 사용자 포인터 (`+0x10`) 를 보고, 칸이 0 이면 `DeleteRegion` (= 로그의 `NtGdiDeleteObjectApp`) 뒤 NULL.
- 표 = PEB `+0xf8` 그대로 시작, 칸 0x18 B, 칸 번호 = 핸들 `& 0xffff`. `GdiValidateHandle` 은 칸 `+0xc` == 핸들 윗 16비트 · 칸 `+0x8` (& ~1) == PID **또는 0** 만 본다.
- **AT03 원인 확인**: 스톡 배열만 채우고 표 칸은 0 → `GdiValidateHandle` 윗 16비트 비교에서 FALSE → `RegisterClassExWOWW` `0x57` + 0.

### 지금 우리 맞나 · 틀리나

- **모자람**: `nt_gdi_init2.cpp` 공유 블록 (표 0). `NtGdiInit2` 반환 (= `gCookie`) 은 0 아님 + 하위 6비트 0 권장 (지금 1).
- **틀림 (둘)**: `nt_gdi_create_rect_rgn.cpp` 핸들 `0x20800` — ① 표 칸을 안 채움 ② **종류 비트가 2 (영역은 4)** → 꼴 `번호 | 0x040000 | (고유 << 24)`.
- `NtGdiDeleteObjectApp` stub 은 유효한 객체에 1.
- 보류 패치 `AT03_stock-objects_held.patch`: 표 칸을 같이 채우면 통과. 스톡 핸들은 `0x900000 | 번호 | 고유 << 24` 꼴이 안전.
- **최소 표 칸 목록 (1 ~ 7)**: run144 "PE_emulator 쪽". 칸 `+0x10` 이 가리키는 블록은 gdi32 가 **쓴다** — 쓰기 가능한 다른 메모리여야 함.

### 남은 것

- **다음 벽 = `CreateICA("DISPLAY")`** (`InternalGdiplusStartup +0x296` → `NtGdiOpenDCW` HDC · `DC_ATTR` · `GetDeviceCaps`) → 우리 **Q278 N1** (정적, 가장 급함). 동적 Q277.

---

## 우리 쪽에서 물을 것 (사용자 결정 대기, 결정 기록 2026-11-25)

- Q264 짐 지도 보탬 (WoW64 절 · TEB `+0x78` · PEB `+0x140`).
- 하네스 제안 넷 (분석 스크립트 — 모듈 이름 · `rva:` 선택자 · 긴 파일 이름 · `pdb_aliases.py` 대소문자).
- VM 묶음 (Q271 · Q273 · Q275 · Q277 — GUI · RPC 시험 프로그램 필요).

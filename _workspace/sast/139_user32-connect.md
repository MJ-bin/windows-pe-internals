# 139 user32-connect — 정적 분석 요청

- 날짜: 2026-11-25
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 는 user32 를 안 올림)
- 대기열: **Q268** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/335_internals-user32-connect.md` — **원문을 먼저 읽을 것**. 막힌 자리 seq 와 받은 샘플 수가 거기 있다). 사용자 결정 2026-11-25 (PE_emulator D154) 의 둘째. 우리 대기열 Q161 ③ · Q163 ④ 를 당긴 것. **정적만.** 값 (`psi` 칸 내용·핸들 값) 은 GUI 샘플 동적 Q162 몫 — 이번에 안 함.
- **먼저 읽는 것** (이미 답한 칸은 가리키기만):
  - `runs/03_sprint/run85-user32-init.md` (`gSharedInfo` user32 rva `0xda350` 은 csrss USERSRV 연결 답 0x248 B 의 `+8` 부터, 첫 8 B = `gpsi`; `DllMain` 이 읽는 `psi` 칸 `+0x0`·`+0x8bc`·`+0x1b66`)
  - `runs/03_sprint/run86-gdi32-init.md` A12·A13 (`GetAppCompatFlags2`: TEB `+0x78` 이 0 이면 `NtUserGetThreadState(0xe)`; 커널이 GUI 스레드로 바꾸며 `__ClientThreadSetup` 되부름)
  - `runs/03_sprint/run131-user32-regclass.md` (`RegisterClassExWOWW` 는 `gpsi` 안 읽음)
  - `runs/03_sprint/run136-winsta-name.md` (win32kfull·win32kbase 는 analyze 됨, `NtUserGetObjectInformation` 꼴)
- 대상 빌드: Windows 11 24H2 26100.9457 x64. **user32 · win32kfull · win32kbase 는 이미 analyze 됨** — `^이름$` export 만.
- 주의: 같은 시간에 sast/138 (rpcrt4·ntdll·ntoskrnl) · sast/140 (kernelbase·mountmgr) decompiler 가 돈다. **sast/141 (gdi-startup) 은 user32 가 겹쳐 이 요청이 끝난 뒤** 시작한다 — user32 프로젝트를 오래 잡고 있지 않게.

## 알고 싶은 것

1. **가드** — 아래 넷이 실패 (0 / −1) 로 나가는 **첫 가지**: 무엇을 읽나 (TEB `+0x78` `Win32ThreadInfo` · TEB `+0x800` 언저리 칸 · `gpsi` 의 어느 칸 · `NtUserGetThreadState(0xe)` 반환값 · `gSharedInfo` 의 어느 칸).
   - `MessageBoxW`/`MessageBoxA` → `MessageBoxTimeoutW` → `MessageBoxWorker` (→ `SoftModalMessageBox`)
   - `DialogBoxParamW` → `DialogBoxIndirectParamAorW` → `InternalDialogBox` (→ `InternalCreateDialog`)
   - `LoadIconW` → `LoadIcoCur` (에뮬레이터: syscall 0 개로 0)
   - `GetSystemMetrics` → `RealGetSystemMetrics` (에뮬레이터: `GetSystemMetrics(0x1000)` 0)
   - 넷이 같은 도우미 (연결 확인 · `ClientThreadSetup` 류) 를 쓰면 그 함수 하나로 답. **도우미 이름 + 실패로 가는 조건 줄.**
2. **가장 짧은 성공 길** — 가드를 지난 `MessageBoxW(NULL, 글, 제목, MB_OK)` 한 번이 돌아올 때까지:
   - 부르는 win32k syscall 차례 (이름은 `ms/disassemble/win32u/_syscalls.tsv` 번호로 맞춤)
   - user32 가 syscall 없이 읽는 칸 (`psi` · 공유 핸들 표 · 창 객체 사본 — 칸 오프셋)
   - 모달 고리가 끝나는 조건 (어떤 메시지 · 누가 `EndDialog` 까지 가게 하나). run85 꼴 "최소 길 지도" 수준. 깊으면 틀만 잡고 "다음 요청으로".
3. **win32kfull `NtUserGetThreadState` 종류 `0xe`** — 뜻 (어느 칸을 돌려주나), 정상 반환이 0 인지, 이 부름의 부수 효과 (GUI 스레드 변환 · `__ClientThreadSetup` 되부름) 가 목적인지. 종류 `0xe` 갈래만.

## 대상

| 모듈 | 함수 (rva, 크기) | depth | 이유 |
|---|---|---|---|
| user32 | `MessageBoxW` (0x5ed90) · `MessageBoxA` (0x96e40) · `MessageBoxTimeoutW` (0x5edf0) · `MessageBoxWorker` (0x5f1d4, 949 B) · `SoftModalMessageBox` (0x21f20, 2174 B — 가드·syscall 차례만) | 1 | 질문 1·2 |
| user32 | `DialogBoxParamW` (0x66260) · `DialogBoxIndirectParamAorW` (0x621b0) · `InternalDialogBox` (0x20ae8, 376 B) · `InternalCreateDialog` (0x1eb90, 4144 B — 가드 자리만) | 1 | 질문 1 |
| user32 | `LoadIconW` (0x559e0) · `LoadIcoCur` (0x618c, 772 B) | 1 | 질문 1 |
| user32 | `GetSystemMetrics` (0x19080) · `RealGetSystemMetrics` (0x8700, 1279 B) | 1 | 질문 1 |
| user32 | `GetAppCompatFlags2` (0x6100 — run86 에 있으면 읽기만) · `ClientThreadSetup` (0x4f810) · `__ClientThreadSetup` (0x4f7d0) · 1 에서 나온 공통 도우미 | 1 | 질문 1·3 |
| win32kfull | `NtUserGetThreadState` (0x1ec5b0, 705 B — 종류 `0xe` 갈래만) | 1 | 질문 3 |

- 범위: 가드 · 최소 길 틀 · 종류 `0xe`. 창 그리기 · 메시지 큐 커널 본문 · 대화상자 템플릿 파싱 본문은 이름만.
- 크면 **1 → 3 → 2** 차례로 쓰고 나머지는 "다음 요청으로" (원문에서 1 이 병목).

## 받고 싶은 것

- 결과서 `_workspace/sast/139_user32-connect_result.md`.
  1. asm 을 남겨 줌. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1) 가드 표 (`API · 가드 함수 · 읽는 칸 · 실패 조건 · 반환 · 줄`) · (2) 최소 길 표 (`차례 · syscall (번호) · 인자 요점 · 읽는 칸 · 줄`) · (3) 종류 `0xe` 한 줄 + 부수 효과.
  4. **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 에뮬레이터는 `NtUserGetThreadState(0xe)` 기본 대답 0, TEB `+0x78` 0, 되부름 안 함. 쓰는 곳: 팀원 `ext/syscall/syscall_layer_cpp/src/alpc/csr_user_connect_stub.cpp` (`psi` 블록) · `src/gui/` 새 stub · 되부름이 꼭 필요하면 `KeUserModeCallback` (지금 없음). **"가드를 통과시키는 최소 조건"** 을 한 목록으로.
  5. PDB GUID 확인 줄.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (Q162 세션과 합치게) 과 **"다음 요청 후보"**.

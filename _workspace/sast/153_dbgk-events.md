# 153 dbgk-events — 정적 분석 요청

- 날짜: 2026-11-27
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청. 디버그 속성으로 지은 자식 = test.exe 초기화 범위 밖)
- 대기열: **Q291** (새, 380 ⑥). PE_emulator 요청 원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 의 **"⑥ `Dbgk*` — 디버그 속성으로 지은 자식의 첫 이벤트 · 기다림 · 이어가기" 절 전부** (읽기만 — 막힌 자리 · `DbgkCreateThread.asm` 줄 · 팀원 주석 "확인 거리" 가 있음, **근거 아님**). 사용자 결정 2026-11-27 (PE_emulator D179): 차례 ⑥. **정적만.**
- 먼저 읽는 것: `runs/03_sprint/run50-eprocess-create.md` (`PspInsertProcess +0x13e DbgkCopyProcessDebugPort` · `+0x2f3 ObReferenceObjectByHandle` · 사진에선 `DebugPort` 0) · 있는 asm `ms/disassemble/ntoskrnl/Dbgk*.asm` 여섯 · `PspNotifyThreadCreation.asm`. 구조체 오프셋은 `pdb_struct.py` 로 (EPROCESS `DebugPort` · `_DEBUG_OBJECT` · `_DEBUGGER_ENTRY`?/`_DBGKM_*` · `_DBGUI_WAIT_STATE_CHANGE` 크기 — 원문 `0xB8` 를 PDB 로 확인).
- 대상 빌드: 26100.9457 x64:
  - `ntoskrnl.exe` — 프로젝트 있음 (analyze 됨). `DbgkCreateThread` (조각 `+0x2079f4 ~ +0x207afb` 포함) · `DbgkpSendApiMessage` (`0xa7d268`) · `DbgkpQueueMessage` · `DbgkpPostModuleMessages` (`0xa62fc4`) · `DbgkpPostFakeProcessCreateMessages` 류 · `DbgkSendSystemDllMessages` · `NtCreateDebugObject` · `NtWaitForDebugEvent` · `DbgkpConvertKernelToUserStateChange` · `DbgkpOpenHandles` · `NtDebugContinue` · `DbgkpWakeTarget` · (`NtCreateUserProcess` 속성 `0x60001` 을 받는 줄 — `PspBuildCreateProcessContext` 류) — 이름은 `_index.tsv` · `pdb_aliases.py --where ntoskrnl.exe` 로 먼저 확인.
  - `ntdll.dll` — (되면) `DbgUiConvertStateChangeStructure` · `DbgUiWaitStateChange` 만. **⚠ ntdll 은 sast/148 이 쓰는 중일 수 있음: `_workspace/sast/148_rpcss-connect-sd_result.md` 가 없으면 열지 말고 "다른 프로젝트 몫" 에 남김.**
- 같은 시간에 도는 decompiler: sast/147 (ntdll32 · kernelbase32 · wow64 · csrsrv) · sast/148 (rpcss · combase · ntdll · sechost · advapi32) · sast/150 → 151 (gdi · user32 · win32k) · sast/152 (windows.storage · shell32 · appinfo · kernelbase) · sast/154 (rpcrt4 · winnsi). **이 요청은 ntoskrnl (+ ⚠ ntdll) 만.** ntoskrnl 프로젝트는 크다 — 새 export 전에 `free -g` (available 12G 아래면 기다림). scratchpad 임시 파일은 `s153/` 하위.

## 알고 싶은 것 (원문 1 ~ 3)

1. **자식에 디버그 객체가 붙는 길과 첫 이벤트**: `NtCreateUserProcess` 속성 `0x60001` → (`PspInsertProcess +0x2f3` 뒤) 무엇을 쓰나 — EPROCESS `DebugPort` · PEB `BeingDebugged` 를 누가 언제. 첫 스레드에서 `DbgkCreateThread` 가 보내는 이벤트 **종류 · 차례** (프로세스 만듦 · 스레드 만듦 · DLL 올림 (exe · ntdll) …) — `+0x2c4 TEST AL,0x4` (`[R14+0x5a0]`) · `+0x327 TEST AL,0x10` (`[R14+0x5a4]`) 깃발 뜻 (PDB 칸 이름), 조각 `+0x2079f4 ~ +0x207afb` · 메시지 칸 (`[RSP+0x90]` 부터 0x40 B — `0x400018` · `8` · `1` 의 뜻).
2. **`DbgkpSendApiMessage` · `DbgkpPostModuleMessages`**: 이벤트를 디버그 객체 큐에 넣는 줄 · 큐 항목 칸 · **보낸 스레드가 어디서 무엇을 기다리며 멈추나** (같은 프로세스의 다른 스레드는 — 얼리나) · 깨어나는 줄.
3. **`NtWaitForDebugEvent` · `NtDebugContinue`**: 큐에서 고르는 규칙 (이미 건넨 것 표시) · 빈 큐일 때 기다림과 status (시간 끝 · alert · APC) · **출력 구조 칸 표** (`NewState` 값마다 채우는 칸 — 프로세스 만듦일 때 디버거 핸들 표에 여는 핸들과 접근 권한) · `NtDebugContinue` 가 ClientId 로 찾아 깨우는 줄 · 받는 ContinueStatus 값 · 실패 status.

- 분량이 크면 **1 → 3 → 2** 차례로 쓰고 나머지는 "다음 요청으로" (1 · 3 이 stub 의 꼴을 정함).

## 받고 싶은 것

- 결과서 `_workspace/sast/153_dbgk-events_result.md`. 근거 `[정적] ms/disassemble/ntoskrnl/<함수>.asm +오프셋` · 구조체는 `[정적] PDB <구조체>+0x..`.
  1. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 3).
  2. 표 (원문 "받을 꼴"): **이벤트 차례 표** (차례 · 종류 · 보내는 함수 · 줄 · 메시지 칸) · **출력 구조 칸 표** (`NewState` 마다) · 큐 항목 칸 표 · 기다림 / 깨움 표 · status 표.
  3. PDB GUID 확인 줄.
- **"PE_emulator 쪽 — stub 이 해야 할 최소"**: 원문 "쓰는 곳" (레 `Src/nt/` 자식 짓기 · 첫 스레드 알림 자리 + 서 `nt_debug_more.cpp:94 · :119` · `src/process/nt_create_user_process.cpp:169`) 에 쓸 꼴 — 엔진 몫 (자식 첫 스레드) · 팀원 몫 (큐 · 기다림) 을 나눠서, "기다림 가짜 성공" (D155) 이 진짜와 어떻게 다른가 한 줄. 팀원 주석 (`0xB8` B · ContinueStatus 여섯 값) 이 맞나.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에:
  - **"VM 으로 확인할 것 — 디버거 필요"** (원문: 플래그 1 로 자식 하나 띄우는 시험 exe — 새 동적 후보. 멈춤점 `nt!함수+0x…` · 찍을 값)
  - **"VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것"** (없으면 "없음")
  - **"다른 프로젝트 몫"** · **"다음 요청 후보"**

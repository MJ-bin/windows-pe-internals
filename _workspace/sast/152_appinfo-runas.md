# 152 appinfo-runas — 정적 분석 요청

- 날짜: 2026-11-27
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청)
- 대기열: **Q290** (새, 380 차례 ⑤) + **Q276a** (Q276 을 나눈 앞 몫 — windows.storage · shell32 analyze · 지도, 뿌리 `ShellExecuteExW`). PE_emulator 요청 원문 `../PE_emulator/_workspace/05_tuning_planner/374_internals-appinfo-runas.md` (읽기만 — **"막힌 자리" · "이미 아는 것" · "알고 싶은 것 (셋)" 을 먼저**). 튜닝 근거 `374_tune19.md` UAC1. 사용자 결정 2026-11-27 (PE_emulator D179): 차례 ⑤. 원문 "Q276 ① 과 같이 묶을 수 있음" → 묶음 (사용자 "결정은 추천대로"). **정적만.**
- **먼저 돌리는 까닭**: shell32 (8 MB) · windows.storage (9 MB) · appinfo 는 **처음 analyze** 라 오래 걸림 → 차례 ⑤ 지만 analyze 를 먼저 걸어 둠. 결과서도 다 되는 대로 쓰면 됨 (문서 차례는 오케스트레이터가 맞춤).
- 먼저 읽는 것: `runs/03_sprint/run143-mountdev-name.md` · `_workspace/sast/140_mountdev-name_result.md` "다음 요청 후보" 1 (`ShellExecuteExW` 뿌리 · `CInvokeCreateProcessVerb::Execute` windows.storage rva `0xe1de0`) · `runs/03_sprint/run145-epm-endpoint.md` (`ept_map` 고르기 · SID 로 SD 짓기 5 절) · `runs/03_sprint/run149-com-rpcss.md` (COM → RPCSS bind 넷 — 원문 "RPCSS 연결 셋" 과 견줄 것).
- 대상 빌드: 26100.9457 x64 `System32/`:
  - `windows.storage.dll` · `shell32.dll` — **처음 analyze + `run_ghidra.sh map` (뿌리 `ShellExecuteExW`)**. 둘 다 크다: **하나씩** 돌리고 `MAXMEM=12G` 를 써도 됨 (`free -g` available 이 14G 아래면 8G 로). 지도는 Q276a 몫 — 사슬에 닿는 깊이까지만.
  - `appinfo.dll` — **처음 analyze**. 원문 3 은 "자식 만드는 호출 한 곳" 까지만 (함수가 크면).
  - `KernelBase.dll` (64 비트, 프로젝트 `kernelbase` 있음) — 사슬이 지나가면 그 함수만 (`CreateProcessAsUserW` 류 등).
- 같은 시간에 도는 decompiler: sast/147 (ntdll32 · kernelbase32 · wow64 · csrsrv) · sast/148 (rpcss · combase · ntdll · sechost · advapi32) · sast/149 → sast/154 (rpcrt4 · winnsi) · sast/150 → 151 (gdi · user32 · win32k) · sast/153 (ntoskrnl · ntdll). **이 요청은 windows.storage · shell32 · appinfo · kernelbase 만 연다.** rpcrt4 · combase 가 필요하면 열지 말고 결과서 "다른 프로젝트 몫" 절에 이름 · 물음을 남긴다 (EPM 쪽은 run145 로 이미 닫힘). scratchpad 임시 파일은 `s152/` 하위.

## 알고 싶은 것 (원문 1 ~ 3)

1. **부탁하는 쪽**: `ShellExecuteExW` (verb `runas`) → 승격을 부탁하는 함수 사슬 (모듈 · 함수 · 줄). RPC 를 거는 함수 · 인터페이스 UUID · 버전 · 프로시저 번호 · protseq · 끝점 (동적 끝점이라 `ept_map` 을 묻나 — 요청에 SID 를 넣는 까닭), 요청 인자 꼴 (실행 파일 · 명령줄 · 플래그 · 시작 정보 · 창 · 시간 제한) · 응답 꼴 (프로세스 · 스레드 핸들 · PID · 오류). 원문 "막힌 자리" 의 iface `0xe1af8308`… 가 이것인가.
2. **응답 뒤**: 클라이언트 검사 · 일 (핸들 검사 · 스레드 깨우기 · `hProcess` 를 돌려주는 조건), 오류 값마다 `ShellExecuteEx*` 반환과 `GetLastError` (`0x4c7` 취소 · `0x6f7` · `0x5`). 앞의 RPCSS 연결 셋 (`0xe60c73e6` bind 거절 `0x6b5` ×3) 이 승격 길에 꼭 필요한가 (거절돼도 이어 가나 — 그 줄).
3. **들어주는 쪽** (appinfo — consent 창은 범위 밖): "승인" 뒤 자식을 만들 때 쓰는 토큰 (연결된 승격 토큰 · 승격 종류 · 무결성 · 그룹 · 특권 중 정적으로 보이는 것) · 부모 지정 방식 (속성 목록 등) · 만드는 플래그 (멈춘 채 만들고 클라이언트가 깨우나).

## 범위 · 차례

- 분량이 크면 **1 → 2 → 3** 차례로 결과서에 쓰고 나머지는 "다음 요청으로". consent.exe 본문 · UI 는 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/152_appinfo-runas_result.md`. 근거 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋`, 이름은 PDB 에 있으면 `[정적] PDB`, 없으면 `[추측: 이름]`.
  1. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 3).
  2. 표: (1) 부탁 사슬 표 (`ShellExecuteExW` → … → RPC 부르는 줄) (2) RPC 표 (UUID · 버전 · 프로시저 · protseq · 끝점 · 요청 / 응답 칸) (3) 오류 값 → 반환 · `GetLastError` 표 (4) appinfo 자식 만들기 표 (토큰 · 부모 · 플래그).
  3. Q276a: windows.storage · shell32 지도 상태 (`_map/` 경로 · 뿌리 깊이 · 노드 수) 한 줄.
  4. PDB GUID 확인 줄 (셋 다).
- **"PE_emulator 쪽 — 지금 맞나·틀리나"**: 원문 "답이 오면 쓰는 곳" (`src/rpc/epm.cpp` 장부에 그 인터페이스 항목 + SID 든 요청 받기 · 새 창구 · 엔진 입구로 승격 자식 짓기 · 팀원 토큰 승격 꼴 · cmstp COM 승격이 같은 길인가) 에 쓸 꼴 한 줄씩 + "stub 이 채울 최소 목록".
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에:
  - **"VM 으로 확인할 것 — 디버거 필요"** (요청 · 응답 스텁 바이트 — 대기열 Q280 · Q283 RPC 묶음에 얹을 꼴, 멈춤점 `모듈!함수+0x…`)
  - **"VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것"** (원문: 승격된 창에서 `whoami /all` 한 번 — 그 밖에 UAC 레지스트리 (`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` 등) · appinfo 서비스 설정 등 무엇이 필요한지)
  - **"다른 프로젝트 몫"** (rpcrt4 · combase 등 이번에 못 연 것)
  - **"다음 요청 후보"**

# 154 rpc-extras — 정적 분석 요청

- 날짜: 2026-11-27
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청)
- 대기열: **Q292** (새, 380 덤 1 ~ 3). PE_emulator 요청 원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 의 **"덤" 표 세 줄 전부** (읽기만 — 막힌 자리 seq · 이미 아는 것이 표 안에 있음). 사용자 결정 2026-11-27 (PE_emulator D179): 380 묶음 맨 끝. 원문 "지금 막는 곳 없음 · 작음 · 한 줄씩이면 됨". **정적만.**
- 먼저 읽는 것: **`_workspace/sast/149_epm-ndr-procs_result.md`** (같은 손 — pefile 로 NDR 2.0 표 읽는 꼴) · `runs/03_sprint/run137-alpc-completion.md` 3-0 · 3-2 (winnsi NDR 2.0 시작 `0x70c2` + `[0, 66, 132, 204, 270, 336, …]` · 5 (`0x7212`) 표 · NDR64 6 `0x6590` · 7 `0x6360` 머리) · `run129-winnsi-rpc.md` 3절 (6 = 24 B 구조체 + 문맥) · `run133-lrpc-wire.md` 2-3 (LRPC 종류 0 / 1 / 2 / 3 / 6 / 10 / 11) · `run141-async-bind.md`.
- 대상 빌드: 26100.9457 x64 `System32/`: `winnsi.dll` (프로젝트 있음 · `.rdata` 는 pefile) · `rpcrt4.dll` (프로젝트 있음 — sast/149 가 끝난 뒤라 열어도 됨).
- 같은 시간에 도는 decompiler: sast/151 (user32 · win32k) · sast/152 (windows.storage · shell32 · appinfo · kernelbase) · sast/153 (ntoskrnl · ntdll) 일 수 있음. **이 요청은 winnsi · rpcrt4 만.** 새 export 전에 `free -g`. scratchpad 임시 파일은 `s154/` 하위.

## 알고 싶은 것 (덤 1 ~ 3)

1. **winnsi 프로시저 6 · 7 NDR 2.0 파라미터 표** (run137 3-2 꼴) + 요청 · 응답 스텁 길이 — 6 요청 52 B · 응답 24 B 가 맞나, 7 완료 응답 칸.
2. **`RpcAsyncCancelCall` 의 보내기만 `0x10000`**: winnsi `NsiRpcDeregisterChangeNotificationEx +0x302 RpcAsyncCancelCall(7, 1)` 뒤 rpcrt4 가 보내는 메시지 — `NtAlpcSendWaitReceivePort` Flags `0x10000` 을 만드는 줄 · LRPC 종류 (`+0x28`) · 칸 · 서버가 답하나 · 클라이언트가 2 ms 씩 자며 (`NtDelayExecution`) 기다리는 끝 조건 (원문 막힌 자리 `rBG02_rv1 8724f2b346f9 seq 2136 ~ 2143`).
3. **nsi 포트의 LRPC 종류 0 · 1 밖 메시지**: 새 스레드가 이미 묶인 nsi 포트로 첫 메시지를 종류 ≠ 0 · 1 로 비동기 보내는 길 — 누가 보내고 종류 값 · 칸은 무엇이며 답을 기다리나. 못 가르면 그렇다고 (원문: "우리가 로그에 종류 값을 먼저 남김").

## 받고 싶은 것

- 결과서 `_workspace/sast/154_rpc-extras_result.md`. 근거 `[정적] <모듈> .rdata rva 0x…` (pefile) 또는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋`.
  1. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 3).
  2. 표: winnsi 6 · 7 파라미터 표 · 스텁 길이 표 · 취소 메시지 칸 표 · 종류 값 표.
  3. PDB GUID 확인 줄.
- **"PE_emulator 쪽 — 지금 맞나·틀리나"**: 덤마다 한 줄 (`src/rpc/nsi.cpp` `TODO(VM)` · `src/rpc/lrpc.cpp` 보내기만 길 · `lrpc.cpp:280 · :328` 종류).
- 끝에: **"VM 으로 확인할 것 — 디버거 필요"** (Q280 · Q271 RPC 묶음에 얹을 꼴) · **"VM 으로 확인할 것 — 디버거 없이"** (없으면 "없음") · **"다음 요청 후보"**.

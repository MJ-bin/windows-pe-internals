# 130 lrpc-wire — 정적 분석 요청

- 날짜: 2026-11-22
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q230** (Q223 의 질문 3 남은 몫 — sast/127 "다음 요청 후보 1·2·3"). PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/303_internals-rpc-nsi-notify.md` 의 3 번 (T2 = nsisvc 알림 등록 **성공** 흉내). 사용자 결정 2026-11-22 "Q230 도 지금 같이" (그 전 결정 "등록 성공이 필요한 샘플이 보일 때만" 을 바꿈). **정적만.** 진짜 메시지 바이트는 동적 (Q231 ⑤) — 이번엔 안 함.
- **먼저 읽는 것**: `_workspace/sast/127_winnsi-rpc_result.md` 전부 — 특히 "3. 서버가 돌려줘야 하는 최소" 3-1 표 (① ~ ⑨ 차례, ③·⑥ bind 와 ④ `ept_map` 응답 칸이 `[공백]`), 3-2 (프로시저 5 입출력), "2. rpcrt4 LRPC 클라이언트의 연결 길", "다음 요청 후보" 1·2·3.
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `rpcrt4.dll` — 이미 analyze 됨. 다시 analyze 하지 말고 `^이름$` export 만 (이름은 `_index`·`pdb_aliases.py --where rpcrt4.dll` 로 확인 — C++ 메서드라 Ghidra 이름이 `Bind`·`HandleReply` 처럼 짧을 수 있음, sast/127 처럼 같은 이름 여럿이면 rva 로 가림).
  - `nsisvc.dll` (61,440 B) — **아직 analyze 안 됨**, PDB 없음 → `fetch_pdb.py` 로 받고 GUID 확인. 작음. 서버 쪽 프로시저 5·6·7 구현이 이 파일에 없으면 (서비스 껍데기뿐이면) 어디 있는지 (import·문자열) 한 줄만 적고 거기서 멈춘다 — 다른 DLL 을 새로 analyze 하지 말 것.
- 주의: 같은 시간에 다른 decompiler 둘이 **user32·combase** (sast/128) 와 **npfs·kernelbase·ntoskrnl** (sast/129) 를 돈다 — 그쪽은 건드리지 않는다. nsisvc analyze 전에 서버 메모리 (`free -g`) 를 본다.

## 왜 (요청한 쪽 말 요약)

- 샘플 212 (악성 203) 가 winnsi 알림 등록에서 멈춘다. sast/127 의 싼 길 (epmapper 연결 `0xc0000034` → 1722 → `WinHttpSendRequest` 0) 은 "깨끗하게 실패" 다. **등록을 한 번 성공시켜 그 뒤 길 (진짜 HTTP 요청) 을 보려면** 서버 흉내 (sast/127 3-1 ① ~ ⑧) 가 필요하고, 그중 LRPC 선 위 형식이 비어 있다.
- 같은 틀이 LSA (`GetUserName*` 0 반환 189 샘플) · RPCSS (`CoInitializeSecurity` — `NtAlpcSendWaitReceivePort(핸들 0)` `0xc0000008` 뒤 combase `+0x55b5a` 가 주소 `0x4` 읽기, 22 샘플) 에도 쓰인다 → **공통 몫 (rpcrt4 LRPC 머리·bind) 과 winnsi/epmapper 몫을 나눠** 적는다.

## 알고 싶은 것

1. **LRPC bind (3-1 ③·⑥)** — 클라이언트가 보내는 bind 메시지와 받아들이는 답:
   - 보내는 쪽: `LRPC_CASSOCIATION::Bind` (또는 PDB 이름) 가 만드는 메시지 — `PORT_MESSAGE` 뒤 `_LRPC_BIND_MESSAGE` 칸 (오프셋·크기·값: 메시지 종류 값, 인터페이스 UUID·버전, 전송 문법 개수와 목록, 길이). `NtAlpcSendWaitReceivePort` 의 Flags·메시지 속성 인자.
   - 받는 쪽: `CompleteBind` / `LRPC_BIND_CCALL::HandleReply` 가 **검사하는 칸** (메시지 종류 · 상태 · 고른 전송 문법 번호 · 길이) 과 각 검사가 틀리면 돌려주는 RPC 오류.
   - → 표: `칸 · 오프셋 · 크기 · 클라이언트가 넣는 값 / 기대하는 값 · 틀리면 · 줄`.
2. **LRPC 요청·응답 머리 (3-1 ④·⑦)** — `LRPC_CCALL::SendReceive` (동기) / 비동기 보내기 (`AlpcSend`·`AlpcSendWaitReceivePort`·`PreinitRequestAlpcHeaders`·`RpcpAlpcpGetHeaderSize`):
   - 요청 머리 칸 (메시지 종류 · 프로시저 번호 · 호출 ID / 시퀀스 · 컨텍스트 · 길이) — 오프셋·값.
   - 응답 머리에서 클라이언트가 검사하는 칸 (`LRPC_CCALL::HandleReply` 류) — 종류 값 (응답 / 오류 / fault) · 길이 · 호출 ID 맞추기 · 큰 응답 (view / 데이터 속성) 갈래. 오류·fault 응답이면 어떤 RPC 오류가 되나.
   - `NtAlpcSendWaitReceivePort` 가 오류 (`0xc0000008` 핸들 무효 · `0xc0000034` 류) 를 돌려줄 때 rpcrt4 가 바꾸는 RPC 오류 (`RpcpStrictAlpcToRpcStatus` rva 0x99d70) — 표 하나. (RPCSS 22 샘플의 "핸들 0 보내기 → `0xc0000008`" 뒤 rpcrt4 가 돌려주는 값, 그게 combase `+0x55b5a` 의 주소 `0x4` 읽기로 이어지는지는 combase 를 안 열고 rpcrt4 쪽 반환값까지만.)
3. **`ept_map` 응답 검사 (3-1 ④)** — `ProcessNextChunk`·`ReturnLookupValues`·`TowerConstruct`·`OsfTowerConstruct` (sast/127 에서 이미 뽑은 것 읽기):
   - 묻는 탑 (tower) 의 층 (floor) 차례·프로토콜 ID 값 (인터페이스 층 · 전송 문법 층 · ncalrpc 층 — 값 그대로).
   - 응답에서 클라이언트가 검사하는 것: 탑 개수 · 상태 · 끝점 층에서 끝점 이름을 꺼내는 줄 (문자열 꼴 — ASCII 인가, 길이 칸) · 빈 응답 → `0x6d9`.
   - → 서버 흉내가 돌려줄 **최소 탑 하나** 의 꼴 (층별 바이트 꼴 — 값은 코드에서 정해지는 만큼만, 모르면 `[공백]`).
4. **nsisvc 서버 쪽 (3-1 ⑦, sast/127 다음 요청 후보 2)** — 있으면:
   - 끝점 이름 (`RpcServerUseProtseqEp*` 의 문자열 그대로 · 또는 동적 끝점 + `RpcEpRegister`).
   - 프로시저 5 구현이 입력 (모듈 ID · 객체 번호 · compartment) 을 어떻게 검사하는지와 컨텍스트 핸들을 만드는 줄 — 흉내가 같은 대답을 내려면 무엇을 봐야 하나.
   - 서버 함수가 nsisvc 에 없으면: 어디 있는지 한 줄 (import·문자열 근거) + "다음 요청으로".

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| rpcrt4 | `LRPC_CASSOCIATION::Bind` · `CompleteBind` · `LRPC_BIND_CCALL::HandleReply` (PDB 이름 확인) | 1 | 질문 1 |
| rpcrt4 | `LRPC_CCALL::SendReceive` · `LRPC_CCALL::HandleReply` · `AlpcSendWaitReceivePort` (0x3fa50·0x6def8) · `AlpcSend` (0x42a18·0x56d44) · `PreinitRequestAlpcHeaders` (0xbe540·0xbe560) · `RpcpAlpcpGetHeaderSize` (0x57650) · `RpcpStrictAlpcToRpcStatus` (0x99d70) | 0~1 | 질문 2 |
| rpcrt4 | `ProcessNextChunk` · `ReturnLookupValues` · `TowerConstruct` · `OsfTowerConstruct` (있는 `.asm` 읽기 + 없으면 export) | 0~1 | 질문 3 |
| nsisvc | 서비스 시작 (`ServiceMain` 류) · 끝점 등록 줄 · 프로시저 5 구현 | 0~1 | 질문 4 |

- 범위: 머리 칸·검사 줄까지. NDR 해석기 본문 (`Ndr64AsyncClientCall` 안쪽 마샬링) · 보안 협상 본문 · ALPC 커널 쪽 본문은 이름만. 프로시저 5 의 NDR 본문 바이트는 형식 문자열 (rva 0x66e0·0x6590·0x6360 — sast/127) 로 짐작되는 만큼만 `[추측]` 으로.
- 분량이 크면 **1 → 2 → 3 → 4** 차례로 결과서에 먼저 쓰고, 나머지는 "다음 요청으로" 라고 적어도 된다.

## 받고 싶은 것

- 결과서 `_workspace/sast/130_lrpc-wire_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1) bind 메시지 · bind 답 칸 표 · (2) 요청 · 응답 머리 칸 표 + `ALPC status → RPC 오류` 표 · (3) 탑 층 표 · (4) 끝점 이름 · 프로시저 5 검사 표.
  4. **sast/127 3-1 표를 고쳐 다시 적은 것** — ③·④·⑥ 의 `[공백]` 을 채운 판 (서버 흉내가 차례대로 돌려줄 것).
  5. PDB GUID 확인 줄 (nsisvc).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall` 갈래 rpc, `src/alpc/nt_alpc_create_port.cpp:192`·`:305`·`:346`·`:623` (T2 — `:623` `NtAlpcSendWaitReceivePort` `alpc_service_protocol_unverified` 에 서버 흉내를 넣을 때 필요한 것). 공통 몫 (LSA·RPCSS 도 쓰는 것) 과 winnsi/epmapper 몫을 나눠서.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (Q231 ⑤ 와 합칠 수 있게 — `NtAlpcSendWaitReceivePort` 를 부르는 rpcrt4 자리 `caller+0x…`) 과 **"다음 요청 후보"**.

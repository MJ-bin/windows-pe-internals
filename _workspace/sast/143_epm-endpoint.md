# 143 epm-endpoint — 정적 분석 요청

- 날짜: 2026-11-26
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q279** (새 항목). PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-epm-endpoint.md` — **원문을 먼저 끝까지 읽을 것**. 사용자 결정 2026-11-26 "**물음 1 만**, 다른 것보다 가장 먼저". 원문 물음 2 · 3 (VM) 은 이번 범위 아님 (대기열 Q280 — VM 묶음). 물음 3 의 "정적으로 될 몫" (nsisvc 프로시저 6 · webio `Startup` 다음) 도 이번엔 안 함.
- **먼저 읽는 것**: `_workspace/sast/130_lrpc-wire_result.md` (run133 — 질문 탑 70 B · `LookupNextChunk +0x11d` · `ProcessNextChunk` 응답 검사 · nsisvc `NsiStartRpcServer` → `RpcEpRegisterW` · `ServerSetupAddress` 의 `LRPC-` 이름), `_workspace/sast/132_alpc-completion_result.md` (run137 — EPM 프로시저 8 NDR 2.0 스텁 칸 · 요청 120 B · 응답 40 B), `_workspace/sast/127_winnsi-rpc_result.md` (run129 — nsi 인터페이스 `7ea70bcf-…` v1.0).
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `RpcEpMap.dll` — **아직 analyze 안 됨**. PDB `fetch_pdb.py` (GUID 확인). 이번 요청의 몸.
  - `rpcrt4.dll` — **이미 analyze 됨**. 다시 analyze 하지 말고 `^이름$` export 만 (`RpcEpRegisterW` 와 그 아래 — 탑 짓기 · epmapper 에 보내는 것 · `RpcServerInqBindings`. 있는 asm: `ServerSetupAddress`).
- 주의: 같은 시간에 다른 decompiler 가 **user32 · win32kfull · win32kbase** (sast/142) 를 돈다 — 그쪽은 건드리지 않는다. 새 analyze 전에 `free -g`. scratchpad 임시 파일은 `s143/` 하위에.

## 왜 (요청한 쪽 말 요약)

- 악성 396 샘플이 비동기 epmapper 연결을 받고, 376 이 winnsi 꼴 (nsi 인터페이스를 물음). 에뮬레이터 epmapper 는 **늘 탑 0 개 + 상태 `0x16c9a0d6`** 으로 답함 → rpcrt4 `0x6d9` → `WinHttpSendRequest` FALSE 가 끝없이 되풀이.
- 진짜 epmapper 가 답을 어떻게 고르고 채우는지 근거가 없어 못 채움 (규칙: 이름 · 값을 지어내지 않음). **이것만 와도 서 바퀴 (`epm.cpp` 등록 장부 + 탑 답) 를 시작할 수 있음.**

## 알고 싶은 것 (원문 물음 1)

1. **장부에서 맞는 항목 고르기** — `RpcEpMap.dll` 의 프로시저 8 (`ept_map` — 3 · 7 과 몸이 같으면 그것) 이 견주는 것: 인터페이스 UUID · 주 / 부 버전 (부 버전 크기 비교 규칙) · 전송 문법 · 3층 프로토콜 (`0x0c` ncalrpc). 어느 함수 · 어느 줄인가.
2. **돌려주는 탑** — 등록 때 받은 탑을 그대로 복사하나 (아니면 다시 짓나 — 1 · 2층 바이트를 질문 탑에서 가져오나). 맞는 항목이 여럿이면 차례와 수 (`max_towers` 까지 채우나). **끝점 핸들을 0 아닌 값으로 주는 때** (다음 묶음이 있을 때만인가) · 배열 머리 (`max_count` · `offset` · `actual_count`) 값.
3. **맞는 항목이 없을 때 출력** — 상태 값 (`0x16c9a0d6` = `EPT_S_NOT_REGISTERED` 인가) · 탑 수 · 핸들 · 배열 머리. 에뮬레이터 지금 답 (끝점 핸들 0 · 탑 수 0 · 배열 `max_towers · 0 · 0` · 상태 `0x16c9a0d6`, 40 B) 과 같은가.
4. **부른 쪽 검사** — 프로시저 8 에 SID 인자 · 보안 콜백 · 호출자 프로세스 확인이 있나 (있으면 어느 줄 · 실패 때 값).
5. **등록 쪽** — rpcrt4 `RpcEpRegisterW` 가 epmapper 에 넣는 탑: 4층 이름이 `ServerSetupAddress` 의 `LRPC-…` 그 이름인가, 그 프로세스의 다른 끝점 (다른 protseq · 다른 바인딩) 도 같이 들어가나, 주석 글 ("NSI server endpoint") 자리. epmapper 쪽 등록 프로시저 (`ept_insert` 류) 가 장부에 넣는 꼴 (덮어쓰기 · 중복) 은 이름 · 한 줄만.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| RpcEpMap | 서버 인터페이스 표 (프로시저 번호 → 함수) · `ept_map` 몸 · 장부 찾기 · 탑 견주기 · 응답 채우기 | 1 | 1 ~ 4 |
| RpcEpMap | 등록 (`ept_insert` 류) · 장부 항목 꼴 | 0 | 5 |
| rpcrt4 | `RpcEpRegisterW` → (탑 짓기 · epmapper 부르기) · `RpcServerInqBindings` | 1 | 5 |

- 이름은 PDB 로 (`pdb_aliases.py --where RpcEpMap.dll <이름>`) 확인 — 위 이름들은 `[추측]`.
- 범위: 고르기 · 채우기 · 검사 · 등록 꼴까지. epmapper 서비스 시작 · 장부 정리 · 다른 프로시저 (2 · 4 · 5 · 6 등) 는 이름만. 클라이언트 쪽 (`LookupNextChunk` · `ProcessNextChunk`) 은 run133 에 있음 — 다시 안 함.
- 분량이 크면 **3 → 1 → 2 → 5 → 4** 로 결과서에 먼저 쓰고 나머지는 "다음 요청으로" 라고 적어도 된다 (3 · 1 · 2 가 서 바퀴를 연다).

## 받고 싶은 것

- 결과서 `_workspace/sast/143_epm-endpoint_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm` (RpcEpMap 은 모듈 이름 그대로 — 폴더 이름을 결과서에 적을 것). 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 5 마다 한 줄).
  3. 표: (1) **"맞는 항목 있음 / 없음" 두 경우의 응답 칸 표** — run137 의 응답 칸 차례 (핸들 20 · 탑 수 · 배열 머리 12 · 포인터 ID · 탑 · 패딩 · 상태) 에 맞춰 `칸 · 있음 값 · 없음 값 · 쓰는 줄` · (2) 견주기 표 — `견주는 것 · 규칙 (같아야 / 크거나 같아야 / 안 봄) · 줄` · (3) 등록 탑 표 — `층 · 바이트 · 출처 · 줄`.
  4. PDB GUID 확인 줄 (RpcEpMap 새로 · rpcrt4 기존 프로젝트와 같은지).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** — `ext/syscall/syscall_layer_cpp/src/rpc/epm.cpp` 의 지금 답 (위 3) 과 "등록 장부 + 탑 답" 을 지을 때 필요한 것 한 줄씩: 장부에 넣을 것 (누가 언제 등록하나 — nsisvc), 고르기 규칙, 응답 바이트 꼴.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (원문 물음 2 · 3 의 멈춤점과 합치게 — 대기열 Q280 에 얹음; RpcEpMap 쪽 멈춤점이 생기면 `RpcEpMap!함수+0x…` 와 그것이 svchost (RPCSS) 안이라는 점) 과 **"다음 요청 후보"**.

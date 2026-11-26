# 2026-11-26 (작성일, 정적 분석 2026-11-26) run145 — epmapper 가 `ept_map` 에 답을 고르고 채우는 법 (Q279, 정적만): 프로시저 **3 · 7 · 8 은 같은 몸** (`ept_map_auth_async` / `ept_map` → `ept_map_auth` → `ept_map_auth_sd` → `GetEntries` → `GetEntriesNoLock`). 장부 항목은 `WildCardMatch` 가 고른다 — **인터페이스 UUID 같음 · 주 버전 같음 · 장부 부 버전 ≥ 질문 부 버전 · (장부 객체 UUID 가 nil 이면 무엇이든)**, 그리고 그 항목의 탑 가운데 **protseq 문자열이 질문 3층 (`0x0c` → "ncalrpc") 과 대소문자 무시로 같은 것**. **전송 문법 (2층) 과 4층 끝점은 안 본다.** 돌려주는 탑은 **등록 때 받은 바이트를 그대로 복사한 것** (`ept_insert_ex +0x274 memcpy` → `GetEntriesNoLock +0x435 memcpy`), `max_towers` 개까지, 넘치면 이어받기 블록. 맞는 것이 없으면 **상태 `0x16c9a0d6` · 탑 0 · 핸들 0 (`max_towers ≥ 2`) → 40 B — PE_emulator `epm.cpp` 의 지금 답과 바이트가 같다.** 등록은 nsisvc `NsiStartRpcServer +0x337 RpcEpRegisterW` → rpcrt4 `RegisterEntries` 가 **바인딩마다 탑 하나** (`BindingAndIfToTower`, 4층 = 동적 끝점 `LRPC-`+18자) 를 만들어 epmapper 의 **다른 인터페이스 `0b0a6584…` v1.1 프로시저 2 (`ept_insert_ex`)** 로 보낸다

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("epmapper `ept_map` (프로시저 8) 이 nsi 인터페이스에 돌려주는 진짜 답", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-epm-endpoint.md` 의 **물음 1 만**, 튜닝 `357_tune17.md` V3b — 악성 396 이 비동기 epmapper 연결을 받고 그 가운데 winnsi 꼴 376) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 rpcrt4 · winnsi 를 안 올리고 (초기화 끝 P2 모듈 6개 `[관찰]` `ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 · run137 · run141 과 같은 줄), `sample/*.c` 에 RPC API 가 없다 `[소스]`. epmapper 자체는 **다른 프로세스** (서비스 `RpcEptMapper` = `svchost.exe -k RPCSS -p` 의 `RpcEpMap.dll` — 원 요청의 `[수집] reg-users`) 다. 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (rpcrt4 · nsisvc 줄 · 새 RpcEpMap 줄 · 근거 줄) · 3장 run133 절 · run137 절 한 줄씩 · run141 절 뒤 새 절 · 4장 (run137 PE_emulator 줄 표시 · Q279 닫힘 줄 · run145 줄 셋) · 5장 (run145 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q279** (PE_emulator 요청 365 epm-endpoint 물음 1, 사용자 결정 2026-11-26 "물음 1 만, 다른 것보다 가장 먼저", 정적만). 결과서 판정 **분석성공** — 다섯 줄 (고르기 · 돌려주는 탑 · 없을 때 · 부른 쪽 검사 · 등록 쪽) 모두 `[정적]` 근거로 답함.
> - 이어 쓰는 앞 run: **run129** (nsi 인터페이스 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0, `FatalLookupErrorEncountered` 가 `0x16c9a0d6` → `0x6d9`), **run133** (질문 탑 70 B · `LookupNextChunk +0x11d` 프로시저 8 · `max_towers` 4 · `ProcessNextChunk` 응답 검사 · nsisvc `NsiStartRpcServer` → `RpcEpRegisterW` · `ServerSetupAddress` 의 `LRPC-` 이름 · 3-3 "등록 때 받은 탑을 그대로 돌려준다" `[추측]`), **run137** (EPM 프로시저 8 NDR 2.0 응답 칸 차례 · 탑 0개 40 B `[규칙]`), **run141** (비동기 epmapper 연결 → bind).
> - 앞 run 고쳐 읽기 둘: run133 · run137 맨 위 한 줄 (아래 "앞 run 고쳐 읽기").
> - 남은 동적 → **Q280** (원문 물음 2 · 3 의 VM 묶음 — 결과서 D1 ~ D7 을 얹음), 남은 정적 → 결과서 "다음 요청 후보" 1 ~ 6 (대기열 번호는 오케스트레이터가 붙임).

## 요약

- **프로시저 → 함수** `[정적: pefile 서버 구조체]`: 공개 EPM `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0 — 3 = `ept_map` (`0x3d00`), 7 = `ept_map_auth` (`0x3d60`), **8 = `ept_map_auth_async` (`0x3ca0`)**, 셋 다 비동기 서버 썽크 `NdrAsyncServerCall`. 0 · 1 · 5 · 6 은 **한 몸 (`0x83c0`, 예외 `0x6d8` 만 던짐)** — 공개 인터페이스로는 등록 · 삭제가 안 된다. 등록은 로컬 전용 `0b0a6584-9e0f-11cf-a3cf-00805f68cb1b` v1.1 의 **2 = `ept_insert_ex`**.
- **같은 몸** `[정적]`: `ept_map_auth_async +0x4e` · `ept_map +0x4c` (SID 칸에 NULL, `+0x43`) → `ept_map_auth +0x1ee` → `ept_map_auth_sd` → `+0x2c2 GetEntries` (공유 잠금) → `GetEntriesNoLock`. 응답은 같은 호출 안 `ept_map_auth_sd +0x1b8 RpcAsyncCompleteCall` 로 간다 (보류하는 한 길만 빼고).
- **고르기** `[정적]`: 질문 탑은 `ept_map_auth_sd +0x102 TowerExplode(탑, &인터페이스, &전송 문법, &protseq, NULL, NULL)` 로 푼다. `WildCardMatch` (`0x1240`): UUID 16 B 같음 (`+0x4 ~ +0x15`) · 주 버전 같음 (`+0x2f TEST EAX,0xffff0000`) · **장부 ≥ 질문** (`+0x36 CMP` · `+0x39 JC`) · 객체 UUID (`+0x3b ~ +0x59`, 장부가 nil 이면 맞음 `+0x6b UuidIsNil` · `+0x79 SETZ`). protseq 는 `GetEntriesNoLock +0x2dd _stricmp`. **전송 문법은 풀어만 두고 안 읽고** (`ept_map_auth_sd [RBP-0x10]` 은 `+0x99` · `+0xf7` 두 줄뿐), **4층은 끝점 인자가 NULL 이라 `TowerExplode +0x1bc JZ` 로 안 읽는다.**
- **돌려주는 탑** `[정적]`: 장부에 넣을 때 `ept_insert_ex +0x274 memcpy` (`tower_length + 4` B), 꺼낼 때 `GetEntriesNoLock +0x3e2 → +0x40f HeapAlloc → +0x435 memcpy`. 층 바이트를 고쳐 쓰는 줄은 TCP · HTTP 탑의 `+0x4b` 4 B 하나뿐 (`+0xb83`, ncalrpc 아님). 차례 = 전역 목록 `IFObjList` 차례 → 항목 안은 **등록 차례의 거꾸로** (`ept_insert_ex +0x279` · `+0x27c`).
- **끝점 핸들** `[정적]`: `max_towers ≥ 2` 이고 맞는 탑이 `max_towers` 개 이하면 **0** (`GetEntriesNoLock +0x4c3`). 넘치면 이어받기 블록 포인터 (`+0x875 GetNewContext`), **`max_towers ≤ 1` 이면 늘 −1** (`+0x4b6` · `+0xa36`), 들어온 핸들이 −1 이면 탑 0 · `0x16c9a0d6` (`+0x635` → `+0xa29 ~ +0xa31`).
- **없을 때** `[정적]`: `GetEntriesNoLock +0x867 MOV EDX,0x16c9a0d6` → `ept_map_auth_sd +0x2c7` · `+0x38d` 가 상태 칸에 씀, 탑 수 0 (`+0x8e`), 핸들 0. 단 하나 다른 길: **"서비스 트리거" 레지스트리 키가 있으면 보류** (`+0x346 RtdsTriggerExists` → `+0x502 NtrbEnqueueRequest`, `RpcAsyncCompleteCall` 안 부름).
- **부른 쪽 검사** `[정적]`: 프로시저 안에 호출자 PID · 이미지 · 토큰 비교는 **없다**. SID (5번째) 가 NULL 이면 검사 전부 건너뜀 (`ept_map_auth +0x2f / +0x32`) — 우리 클라이언트는 NULL (run133 3-1). 인터페이스 콜백은 조건부 (`StartEndpointMapper +0x151 ~ +0x169`), 실패 값 5.
- **등록 쪽** `[정적]`: nsisvc `+0x2de RpcServerInqBindings` → `+0x337 RpcEpRegisterW(IfSpec, 벡터, NULL, L"NSI server endpoint")` → `RegisterRpcEndpointsW +0x67` → `RegisterRpcEndpointsA +0xe0` → `RegisterEntries` — 항목 `0x58` B × 바인딩 수 (`+0xeb`), 바인딩마다 `+0x332 BindingAndIfToTower`, `+0x296 NdrClientCall3(프록시 0xe1500, 프로시저 2)`. 벡터는 **그 프로세스의 모든 서버 주소** 라 다른 protseq · 다른 끝점도 같이 들어간다. 4층 이름 = `I_RpcBindingInqDynamicEndpointW` 가 주는 동적 끝점 (`ServerSetupAddress` 의 그 이름이라는 마지막 연결은 `[추측]`).

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백셋째 바퀴. plan **Q279**. **짐 지도 바퀴 아님.**
- 정적: **RpcEpMap 처음 analyze** (`System32\RpcEpMap.dll` 147,456 B, 9초, 함수 440 — `ms/decomp/rpcepmap/_index.tsv` 머리 + 440줄, `ms/ghidra/rpcepmap.analyze.log` 209줄 `SetPdb: RpcEpMap.dll <- ms/sym/RpcEpMap.dll.pdb`). **폴더 이름은 소문자** (`ms/decomp/rpcepmap/` · `ms/disassemble/rpcepmap/`). rpcrt4 는 다시 analyze 안 함 (`^이름$` export 만). nsisvc 는 sast/130 때 `.asm` 읽기만.

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| RpcEpMap (`RpcEpMap.pdb`) | `928AE9EA93768986C32C5500323376111` | **새 줄** (처음 analyze). PDB 안 Age 3 · 심볼 서버 키는 바이너리 Age 1 (결과서 — ws2_32 · nsisvc 와 같은 모양) |
| rpcrt4 | `0204A8A137C42B7DF1F0811A3A8277B21` | 같음 (run99 · run133 · run137 · run141) |
| nsisvc | `CE94A7C9096AB626B28E58C48D6605591` | 같음 (run133) — 이번엔 있던 `.asm` 만 읽음 |

- 버전 대조 (결과서): 이 문서가 인용하는 `+0x` 를 원본 바이트 (`pefile`) 와 맞춤 — 일치 (`ept_map_auth_sd +0x102` · `+0x2c9` · `+0x38d`, `GetEntriesNoLock +0xa36` · `+0x867`, `WildCardMatch +0x2f` · `+0x36`, `ept_insert_ex +0x21b` · `+0x3e3`, rpcrt4 `RegisterEntries +0xeb` · `+0x296` · `+0x332` 등). "알려진 사실" 표엔 이 두 모듈 줄이 없다.
- 공개 PDB 라 구조체 타입 이름이 없다. `IFOBJ` · `PSEP` 노드 이름은 매직 값 (`0x49464f42` · `0x50534550`) 을 글자로 읽어 붙인 **우리 이름** `[이름 추측]`.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): 비동기 epmapper 연결 → bind → **프로시저 8 답 = 탑 0개 + `0x16c9a0d6`** (`epm.cpp:13 ~ :28` 이 늘 40 B) → rpcrt4 `0x6d9` → `WinHttpSendRequest` FALSE 되풀이 (60초에 579번). 진짜 epmapper 가 답을 어떻게 고르고 채우는지 근거가 없어 장부를 못 지음. 요청서 `_workspace/sast/143_epm-endpoint.md`:
  1. 장부에서 맞는 항목 고르기 — UUID · 주 / 부 버전 (부 버전 크기 규칙) · 전송 문법 · 3층 `0x0c`.
  2. 돌려주는 탑 — 등록 탑 그대로인가, 여럿이면 차례 · 수, 끝점 핸들이 0 아닌 때, 배열 머리.
  3. 맞는 항목이 없을 때 출력 — 에뮬레이터 지금 답 (40 B) 과 같은가.
  4. 부른 쪽 검사 — SID · 보안 콜백 · 호출자 확인.
  5. 등록 쪽 — `RpcEpRegisterW` 가 넣는 탑 (4층 이름 · 다른 끝점 · 주석), `ept_insert_ex` 가 장부에 넣는 꼴.
- 쓰는 곳 (원문): `ext/syscall/syscall_layer_cpp/src/rpc/epm.cpp` — 서 바퀴 (등록 장부 + 탑 답).

## 방법

- 정적 sast/143 (결과서 `_workspace/sast/143_epm-endpoint_result.md`, **분석성공**, 스크립트 문제 없음):
  - RpcEpMap (새 analyze → export): `^(ept_map_auth_async|ept_map|ept_map_auth|ept_map_auth_sd)$` depth 1 (17개) + 등록 · 고르기 · 보안 · 트리거 쪽 depth 0 (34개) → `.asm` 51 · `.c` 51 (+ `ept_mgmt_delete` 16 B 몸), `_calls.tsv` 297줄, 지도 `_map/ept_map_auth_async_*` (뿌리에서 함수 43), `_refs` 둘 (`LocalEpmpSecurityCallback` · `RPC_INTERFACE_EVENT_GUID`). 서버 · 클라이언트 인터페이스 구조체 · 디스패치 표는 `pefile` 로 직접.
  - rpcrt4 (export 만): `RpcEpRegisterW` · `RpcEpRegisterNoReplaceW` · `RpcServerInqBindings(Ex)` · `TowerExplode` · `OsfTowerExplode` · `PruneForEpRegistration` · `RegisterRpcEndpoints*` depth 1, `BindingAndIfToTower` · `BindToEpMapper` · `OpenEndpointMapper` · `InquireBindings` · `FindInterface` · `InterfaceExported` · `I_RpcBindingInqDynamicEndpointW` · `InquireDynamicEndpoint` depth 0, `I_RpcBindingIsClientLocal` · `I_RpcServerCheckClientRestriction` depth 1. 있던 `ServerSetupAddress` · `TowerConstruct` · `FatalLookupErrorEncountered` 읽기.
  - nsisvc: 있던 `NsiStartRpcServer.asm` 읽기, 주석 글 `NSI server endpoint` 는 `strings`.
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - rpcepmap `WildCardMatch +0x0 ~ +0x87` (전부); `ept_map_auth_sd +0x10 · +0x99 · +0xa2 · +0xb4 · +0xcb · +0xd2 · +0xd6 · +0xdb · +0xe9 · +0xf2 · +0xf7 · +0x102 · +0x109 · +0x110 · +0x116 · +0x11c · +0x129 · +0x1ae · +0x1b8 · +0x249 · +0x262 ~ +0x2ce · +0x2da · +0x2e3 · +0x2ec · +0x346 · +0x376 · +0x38d · +0x39d · +0x40c · +0x45c · +0x4e5 · +0x502 · +0x516`; `GetEntriesNoLock +0x8e · +0x121 · +0x192 · +0x197 · +0x1f2 · +0x21f · +0x223 · +0x2b0 · +0x2dd · +0x2fc · +0x363 · +0x39b · +0x39d · +0x3c7 · +0x3e2 · +0x40f · +0x435 · +0x449 · +0x44c · +0x461 · +0x46d · +0x478 · +0x47a · +0x49f ~ +0x4ea · +0x4ec ~ +0x51f · +0x54e · +0x635 · +0x63f · +0x657 · +0x822 ~ +0x8ae · +0x9c6 · +0xa01 ~ +0xa3d · +0xb7f · +0xb83 · +0xbc2 ~ +0xbdf`; `ept_map_auth +0x14 ~ +0xfd` · `+0x128 · +0x149 · +0x166 · +0x183 · +0x1ee · +0x1f3 ~ +0x2ba`; `ept_map +0x43 · +0x4c`; `ept_map_auth_async +0x4e`; `GetEntries +0x2a · +0xc7 · +0xd5`; `ept_mgmt_delete +0x4 · +0x9`; `ept_insert_ex +0x84 · +0x9d · +0xc7 · +0xe6 · +0x165 · +0x21b · +0x26d ~ +0x27c · +0x2ee · +0x308 · +0x3ad · +0x3e3 · +0x418 · +0x45d · +0x462 · +0x50c · +0x667 · +0x6c5`; `StartEndpointMapper +0x151 · +0x157 · +0x163 · +0x169 · +0x182 · +0x19b · +0x1c6`; `LocalEpmpSecurityCallback +0x1f · +0x25 · +0x2c · +0x35`; `RemoteEpmpServerSecurityCallback +0x4 · +0xd · +0x31 · +0x3e`.
  - rpcrt4 `TowerExplode +0x4b · +0x160 ~ +0x1cf · +0x265`; `FatalLookupErrorEncountered +0x26 · +0x2d · +0x32 · +0x87`; `RegisterEntries +0x3e · +0x41 · +0x7e · +0xeb · +0x1a5 · +0x22c · +0x296 · +0x2d5 · +0x332 · +0x36a · +0x36f · +0x39f · +0x3ad · +0x405 · +0x48a`; `RpcEpRegisterW +0xf`; `RegisterRpcEndpointsW +0x67`; `RegisterRpcEndpointsA +0x54 · +0x5a · +0x82 · +0x94 · +0xd8 · +0xe0`; `BindingAndIfToTower +0x88 · +0x98 · +0xb3 · +0xb8 · +0xf5 · +0x120 · +0x1e6 · +0x20e`; `RpcServerInqBindings +0x26`; `InquireDynamicEndpoint +0x6 · +0x1f · +0x2b · +0x2f`; `ServerSetupAddress +0xe8 · +0x179`.
  - nsisvc `NsiStartRpcServer +0x2de · +0x325 · +0x334 · +0x337`.
  - `pdb_aliases.py --lookup RpcEpMap.dll 0x83c0 · 0x5a7c · 0x1240 · 0x13010`, `--lookup rpcrt4.dll 0x62e58 · 0x62060 · 0x654c4 · 0x9d5a0`, `--where RpcEpMap.dll RemoteEpmpServerSecurityCallback` (아래 "flow-writer 대조").
  - `../PE_emulator` 의 `epm.cpp` (47줄) 를 **줄 번호만 훑음** (읽기만, 고치지 않음).

## 결과

비유:

- **epmapper 는 "건물 안내 데스크"다.** 서비스가 문을 열 때 데스크에 "우리 사무실 = 몇 호" 카드 (탑) 를 맡기고, 손님이 "nsi 사무실 어디예요?" 하고 물으면 데스크는 **맡겨 둔 카드를 복사해서** 건넨다. 카드를 새로 쓰지 않는다.
- 데스크가 보는 건 **회사 이름 (UUID) · 큰 판 번호 (주 버전) · 작은 판 번호가 손님 것보다 크거나 같은지 · 전화 종류 (ncalrpc)** 뿐이다. 손님이 쓴 "말투 (전송 문법)" 와 "아는 호수 (4층)" 는 안 본다.
- 카드가 없으면 "그런 사무실 없음 (`0x16c9a0d6`)" 이라고만 답한다. 우리 에뮬레이터는 지금 **늘 이 답** 을 하고 있다 — 데스크에 카드를 맡긴 서비스가 없어서다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{rpcepmap,rpcrt4,nsisvc}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `rpcepmap!ept_map_auth_async` (프로시저 8, `0x3ca0`) | `+0x4e` | `+0x53` | `ept_map_auth` (`0x3d60`) — 인자 10개 그대로 | [정적] |
| `rpcepmap!ept_map` (프로시저 3, `0x3d00`) | `+0x4c` | `+0x51` | `ept_map_auth` — `+0x43 MOV [RSP+0x20],0` (SID 칸 NULL) | [정적] |
| `rpcepmap!ept_map_auth` (프로시저 7) | `+0x37` · `+0x7c` | `+0x3e` · `+0x83` | `IsValidSid` — SID ≠ NULL 일 때만 (`+0x2f TEST` · `+0x32 JZ → +0x9e`) | [정적] |
| 〃 | `+0x128` | `+0x12f` | `AddAccessAllowedAce(ACL, 2, 0x20000, SID)` — SID 갈래 | [정적] |
| 〃 | **`+0x1ee`** | **`+0x1f3`** | **`ept_map_auth_sd`** (`0x4024`) | [정적] |
| 〃 | `+0x91` | `+0x98` | `RpcRaiseException(0x6d8)` — SID · SD 만들기 실패 (`+0x8c`) | [정적] |
| `rpcepmap!ept_map_auth_sd` | `+0xa2` | `+0xa9` | `IsValidSecurityDescriptor` — 나쁘면 예외 `0x57` (`+0xb4`) | [정적] |
| 〃 | **`+0x102`** | **`+0x109`** | **rpcrt4 `TowerExplode`** (가져오기 `[0x180015e30]`) — 끝점 · 네트워크 주소 인자 NULL (`+0xe9` · `+0xf2`) | [정적] |
| 〃 | `+0x249` | `+0x24e` | `GetEntries` — 관리 인터페이스 (`MgmtIf`) 갈래 (이번 물음 밖) | [정적] |
| 〃 | **`+0x2c2`** | **`+0x2c7`** | **`GetEntries`** (`0x13f0`) — 비교 함수 `WildCardMatch` (`+0x268` · `+0x275`), `max_towers×8` (`+0x2a3`), 모드 2 (`+0x29b`) | [정적] |
| 〃 | `+0x346` | `+0x34b` | `RtdsTriggerExists` — 트리거 키 (없음 · 핸들 0 · 거부 표시 0 · `g_TriggersDisabled == 0` 일 때만, `+0x2c9 ~ +0x2ec`) | [정적] |
| 〃 | `+0x39d` · `+0x40c` | `+0x3a2` · `+0x411` | 잠금 잡기 (`RPC_SRWLOCK::Request`) · `GetEntriesNoLock` 한 번 더 — 키 있음 갈래 | [정적] |
| 〃 | `+0x4e5` · `+0x502` | `+0x4ea` · `+0x507` | `InitializeBufferedRequest` · `NtrbEnqueueRequest` — **보류** (`+0x516` → `RpcAsyncCompleteCall` 안 부름) | [정적] |
| 〃 | **`+0x1b8`** | **`+0x1bf`** | **`RpcAsyncCompleteCall`** — `+0x1ae TEST R12D,R12D` 참 (보류 안 한 모든 길) | [정적] |
| `rpcepmap!GetEntries` | `+0x2a` · `+0xc7` · `+0xd5` | `+0x31` · `+0xcc` · `+0xdc` | `RtlAcquireSRWLockShared` · **`GetEntriesNoLock`** (`0x1500`) · `RtlReleaseSRWLockShared` | [정적] |
| `rpcepmap!GetEntriesNoLock` | **`+0x192`** | **`+0x197`** | ⇢ 비교 함수 (CFG 간접 `CALL 0x180013010`) = `WildCardMatch` (`0x1240`) — 반환 0 = 맞음 | [정적] |
| 〃 | `+0x1f2` | `+0x1f9` | `AccessCheck(SD, 등록자 토큰 [노드+0x18], 0x20000, …)` — SD 있을 때만 | [정적] |
| 〃 | **`+0x2dd`** | **`+0x2e4`** | **`_stricmp`** (장부 탑 protseq ↔ 질문 protseq) | [정적] |
| 〃 | **`+0x40f`** · **`+0x435`** | `+0x416` · `+0x43a` | **`HeapAlloc` · `memcpy`** — 장부 탑 (`+0x3e2 [RDI+0x28]`) 을 그대로 복사 | [정적] |
| 〃 | `+0x461` | `+0x468` | `GetTickCount64` → `+0x46d [노드+0x98]` (마지막 사용 시각, 응답 바이트와 무관) | [정적] |
| 〃 | `+0x4fb` | `+0x502` | `I_RpcBindingIsClientLocal` — 방화벽 확장 켜짐 (`+0x363 CMP [gFwManagerExtensionState],1`) 일 때만 | [정적] |
| 〃 | `+0x54e` | `+0x555` | `RpcServerInqCallAttributesW` — 같은 방화벽 갈래 | [정적] |
| 〃 | `+0x875` | `+0x87a` | `GetNewContext` (이어받기 블록 0x20 B, 매직 `0xfafafafa`) — 탑이 `max_towers` 를 넘칠 때 | [정적] |
| `rpcepmap!ept_insert_ex` (로컬 인터페이스 프로시저 2, `0x22a0`) | `+0x9d` | `+0xa4` | rpcrt4 `TowerExplode` — 등록 탑에서 protseq · 끝점 | [정적] |
| 〃 | `+0x165` | `+0x16c` | `HeapAlloc` — PSEP 노드 (`tower_length + 0x40 + 끝점 + protseq`), 매직 `+0x21b` | [정적] |
| 〃 | **`+0x274`** | **`+0x279`** | **`memcpy`** — 등록 탑 `tower_length + 4` B 를 노드에 (`+0x26d` · `+0x270`) | [정적] |
| 〃 | `+0x2ee` · `+0x308` | `+0x2f5` · `+0x30d` | `RpcImpersonateClient` · `GetCurrentUserToken` → `+0x418 [노드+0x18]` | [정적] |
| 〃 | `+0x3ad` | `+0x3b4` | `HeapAlloc` — IFOBJ 노드 (`0xa0` + 주석), 매직 `+0x3e3` | [정적] |
| 〃 | `+0x45d` | `+0x462` | `EnLinkOnIFOBJList` | [정적] |
| 〃 | `+0x6c5` | `+0x6ca` | `FindIFOBJNode` — 덮어쓰기 대상 찾기 | [정적] |
| 〃 | `+0x50c` | `+0x511` | `CompletePendingEprRequests` — 보류된 질문 깨우기 | [정적] |
| `rpcepmap!StartEndpointMapper` | `+0x182` | `+0x189` | `RpcServerRegisterIf3` — 공개 `e1af8308` (콜백 조건부 `+0x151 ~ +0x169`) | [정적] |
| 〃 | `+0x1c6` | `+0x1cd` | `RpcServerRegisterIf3` — 로컬 `0b0a6584` (콜백 `LocalEpmpSecurityCallback`, `+0x19b`) | [정적] |
| `nsisvc!NsiStartRpcServer` | `+0x2de` | `+0x2e5` | `RpcServerInqBindings` | [정적] |
| 〃 | **`+0x337`** | **`+0x33e`** | **`RpcEpRegisterW`** (`+0x325 RDX` 벡터 · `R8` 0 · `R9` `L"NSI server endpoint"`) | [정적] |
| `rpcrt4!RpcEpRegisterW` | `+0xf` | `+0x14` | `RegisterRpcEndpointsW` (`0x62754`) | [정적] |
| `rpcrt4!RegisterRpcEndpointsW` | `+0x67` | `+0x6c` | `RegisterRpcEndpointsA` (`0x62828`) | [정적] |
| `rpcrt4!RegisterRpcEndpointsA` | `+0xe0` | `+0xe5` | `RegisterEntries` (`0x62934`) — 다섯째 인자 1 (replace, `+0xd8`) | [정적] |
| `rpcrt4!RegisterEntries` | `+0x7e` · `+0x405` | `+0x83` · `+0x40a` | `BindToEpMapper` (둘째는 `0x6ba` · `0x6bf` 뒤 다시) | [정적] |
| 〃 | **`+0x332`** | **`+0x337`** | **`BindingAndIfToTower`** (`0x62060`) — 바인딩마다 | [정적] |
| 〃 | `+0x36a` | `+0x36f` | `RtlStringCbCopyA` — 주석 0x40 B 칸 | [정적] |
| 〃 | **`+0x296`** · `+0x48a` | **`+0x29b`** · `+0x48f` | **`NdrClientCall3(프록시 0xe1500, 프로시저 2, …)`** = 서버 `ept_insert_ex` (앞뒤 `+0x22c` · `+0x2d5` `EpContextMutex`) | [정적] |
| 〃 | `+0x1a5` · `+0x3ad` | `+0x1aa` · `+0x3b2` | `RPC_SERVER::InterfaceExported` (둘째는 벡터 0개 — epmapper 에 안 보냄, `+0x41 JZ → +0x39f`) | [정적] |
| `rpcrt4!BindingAndIfToTower` | `+0x88` | `+0x8d` | `RpcIfInqId` — 1층 = 서버 인터페이스 ID | [정적] |
| 〃 | **`+0xb8`** | **`+0xbd`** | **`I_RpcBindingInqDynamicEndpointW`** — 4층 끝점 | [정적] |
| 〃 | `+0xf5` · `+0x120` | `+0xfa` · `+0x125` | `UnicodeToAnsiString` · `RpcStringBindingParseA` (정적 끝점일 때) | [정적] |
| 〃 | `+0x20e` | `+0x213` | `TowerConstruct` (`0x5a350`, sast/130 3-1 과 같은 틀) | [정적] |
| `rpcrt4!RpcServerInqBindings` | `+0x26` | `+0x2b` | `RPC_SERVER::InquireBindings(GlobalRpcServer, …, NULL, NULL)` | [정적] |
| `rpcrt4!SVR_BINDING_HANDLE::InquireDynamicEndpoint` (`0x9d5a0`) | `+0x2f` | `+0x34` | `DuplicateString([[this+0xe8]+0x10])` — `+0x6 CMP [this+0x100],1` (동적일 때) | [정적] |

### 1. 프로시저 → 함수, 그리고 같은 몸 (질문 1) [정적]

| 인터페이스 (RpcEpMap rva) | 프로시저 → 함수 |
|---|---|
| **`e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0** (`0x148e0`, 공개 — 질문이 오는 곳) | 0 · 1 · 5 · 6 = 예외 `0x6d8` 만 던지는 16 B 몸 (`0x83c0`, `ept_mgmt_delete.asm +0x4` · `+0x9`; 같은 주소 이름 4개, 후보: `ept_insert` · `ept_delete` · `ept_inq_object` · `ept_mgmt_delete`) · 2 = `ept_lookup` (`0x12d0`) · **3 = `ept_map`** · 4 = `ept_lookup_handle_free` (`0x83e0`) · **7 = `ept_map_auth`** · **8 = `ept_map_auth_async`**. 썽크: 3 · 7 · 8 = `NdrAsyncServerCall` (`0x72c0`), 그 밖 `NdrServerCall2` (`0x72f0`) |
| **`0b0a6584-9e0f-11cf-a3cf-00805f68cb1b` v1.1** (`0x14cc0`, 로컬 — 등록이 오는 곳) | 0 `OpenEndpointMapper` · 1 `AllocateReservedIPPort` · **2 `ept_insert_ex`** · **3 `ept_delete_ex`** · 4 `SetRestrictRemoteClients` · 5 `ResetWithNoAuthException` · 6 `ReRaiseInterfaceTriggers` |
| `1d55b526-c137-46c5-ab79-638f2a68e869` v1.0 (`0x15830`, 13개) | 디버그 인터페이스 — 이름만 |

- rpcrt4 쪽 클라이언트 구조체 (`pefile`): `e1af8308` v3.0 = `0xe1220` (프록시 `0xe11e8` — 질문, run133 3-1), **`0b0a6584` v1.1 = `0xe14a0` (프록시 `0xe1500` — 등록)**.
- 로컬 인터페이스는 **ncalrpc 로만** 받는다: `LocalEpmpSecurityCallback` 이 전송 종류 ≠ 4 이면 5 (`+0x25 SUB EAX,4` … `+0x2c AND EAX,5`, 조회 실패도 `+0x35 MOV EAX,5`).

### 2. 고르기 — 견주기 표 (질문 1) [정적]

| 견주는 것 | 규칙 | 줄 |
|---|---|---|
| 인터페이스 UUID (질문 1층) | **같아야** (16 B) | `WildCardMatch +0x4 ~ +0x15` (노드 `+0x48` · `+0x50`), 아니면 `+0x1a` 1 |
| 주 버전 | **같아야** — 버전을 `주 << 16 \| 부` 로 합쳐 (`ept_map_auth_sd +0x11c ~ +0x129`) 상위 16비트 XOR | `WildCardMatch +0x25 ~ +0x34` |
| **부 버전** | **장부 ≥ 질문** (합친 값 부호 없는 비교, 장부가 작으면 안 맞음) | `WildCardMatch +0x36 CMP R8D,R9D` · `+0x39 JC` |
| 객체 UUID | 장부가 **nil 이면 무엇이든 맞음**, 아니면 요청과 같아야. 요청 포인터 NULL 이면 nil 로 (`LocalNullUuid`) | `WildCardMatch +0x3b ~ +0x59` · `+0x6b UuidIsNil` · `+0x79 SETZ`; `ept_map_auth_sd +0xcb ~ +0xd2` |
| **전송 문법 (질문 2층)** | **안 봄** — 풀어서 `[RBP-0x10]` 에 두지만 다시 안 읽음, `GetEntries` 인자에도 없음 | `ept_map_auth_sd +0x99` (초기화) · `+0xf7` (넘김) 뿐 |
| **3층 프로토콜** | 질문 3층 ID `0x0c` → protseq "ncalrpc" (층 수 **정확히 4**, 아니면 `0x16c9a0cd`), `0x0a` / `0x0b` → TCP 갈래, 그 밖 `0x16c9a0cd`. 장부 탑 protseq 와 **`_stricmp` 로 같아야** | `TowerExplode +0x4b` (층 3 ~ 6) · `+0x16b ~ +0x180` · `+0x1a6 CMP R9D,4`; `GetEntriesNoLock +0x2b0 ~ +0x2fc` (`+0x2dd`) |
| **4층 (끝점)** | **안 봄** — 끝점 인자 NULL 이라 4층을 안 읽고 0 반환 | `TowerExplode +0x1b9 TEST RSI,RSI` · `+0x1bc JZ → +0x265` |
| SID (5번째 인자) | NULL 이면 안 봄 (4절) | `ept_map_auth +0x2f ~ +0x32` |
| 장부 탑 자체 | 검사 없이 복사 | `GetEntriesNoLock +0x3e2 ~ +0x435` |
| (뜻 모를 검사) | 객체 UUID 포인터 == 탑 포인터면 `0x6d8` `[추측: 정상 호출엔 안 걸림]` | `ept_map_auth_sd +0xd6` · `+0xdb` |

- 훑는 차례: `IFObjList` 를 앞에서부터 (`GetEntriesNoLock +0x121`), 맞는 항목마다 그 항목의 탑 목록 전체 (`+0x2b0 [RBX+0x20]` → `+0x47a` 다음 탑) 에서 protseq 같은 것을 하나씩. 탑 수가 `max_towers` 에 닿으면 (`+0x39b` · `+0x39d JNC → +0x657`) 나머지는 이어받기 블록.
- 질문 탑 풀기에 실패하면 상태 = 그 코드 (`0x16c9a0cd` 등) · 탑 수 0 · 핸들 안 건드림, 호출은 정상 끝 (`ept_map_auth_sd +0x110` · `+0x116`).

### 3. 돌려주는 탑 · 응답 칸 (질문 2 · 3) [정적]

- **그대로 복사** — 등록 때 `ept_insert_ex +0x274 memcpy` (`+0x26d MOV R8D,[RDX]` · `+0x270 ADD R8,4` = `tower_length + 4` B), 질문 때 `GetEntriesNoLock +0x3e2 → +0x40f → +0x435`. 1 · 2층도 **등록한 쪽 인터페이스** 에서 온 바이트다 (질문 2층이 NDR 2.0 이든 NDR64 든 같은 탑, 1층 버전도 등록 때 버전).
- 예외 하나: protseq 가 `ncacn_ip_tcp` · `ncacn_http` 인 탑은 `+0x4b` 의 4 B 를 덮어쓸 수 있다 (`+0x44c JNZ → +0xb7f`, `+0xb83 MOV [RAX+0x4b],ECX`) — ncalrpc 아님. 그 4 B 의 뜻 `[추측: TCP 탑 주소 칸]`.

응답 칸 표 (프로시저 8, NDR 2.0, `max_towers = 4`, 맞는 탑 1개 = nsi 하나 · 끝점 23자). 칸 차례는 run137 3-1 그대로. "쓰는 줄" = 서버 코드가 값을 정하는 줄 (NDR 틀이 정하는 칸은 `[규칙]`):

| 칸 (스텁 오프셋) | 있음 | 없음 | 쓰는 줄 |
|---|---|---|---|
| 끝점 핸들 20 B (`+0x00`) | **0** | **0** | `GetEntriesNoLock +0x4b6 CMP EAX,1` · `+0x4bd JBE` · **`+0x4c3 MOV [RAX],RDI`**. `max_towers ≤ 1` 이면 `+0xa36 MOV qword [RAX],-1`, 이어받기 블록이 있으면 `+0x885` 에서 그 포인터 |
| `num_towers` (`+0x14`) | n | 0 | `+0x8e MOV [R9],EDI` (처음 0) · `+0x478 INC` (탑마다) |
| 배열 `max_count · offset · actual_count` (`+0x18`) | `4 · 0 · n` | `4 · 0 · 0` | `[규칙]` run137 3-1 (서버 코드엔 쓰는 줄 없음 — `actual_count` = 위 탑 수) |
| 원소 포인터 ID (`+0x24`, 4 B × n) | 0 아닌 값 | 없음 | `[규칙]` (탑 포인터를 놓는 줄 `+0x449 MOV [RDX],RAX`) |
| 탑 (`+0x28 ~`) | conformance `0x58` · `tower_length` `0x58` · 88 B (아래 탑 표) = **96 B** | 없음 | `+0x3e2 → +0x435` (등록 탑 사본) |
| 패딩 | 0 B (88 은 4 의 배수) | 없음 | `[규칙]` |
| 상태 (`+0x88` / 없음 `+0x24`) | **0** | **`0x16c9a0d6`** | 있음: `+0x4c6 MOV EAX,EDX` → `ept_map_auth_sd +0x2c7 MOV [RBX],EAX`. 없음: `+0x867 MOV EDX,0x16c9a0d6` · `ept_map_auth_sd +0x38d` |
| **합** | **140 B (`0x8c`)** — 응답 머리 `TotalLength = 0x40 + 0x8c = 0xcc` (run133 2-3) | **40 B** | |

- **핸들이 0 이 아닌 때** (정리): ① 맞는 탑이 `max_towers` 를 넘음 → 이어받기 블록 포인터 (`+0x39d JNC → +0x657`, `+0x875`), ② 이어받는 호출에서 다 비우면 블록을 풀고 `(max_towers > 1) ? 0 : −1` (`+0x837 CMP R15D,1` · `+0x840 SETA` · `+0x843 DEC` · `+0x846`), ③ **`max_towers ≤ 1` 이면 블록이 없어도 −1**, ④ 들어온 핸들이 −1 이면 0 으로 바꾸고 탑 0 · `0x16c9a0d6` (`+0x635 CMP RCX,-1` → `+0xa29 ~ +0xa31`). `max_towers = 0` 이면 `+0x852 JZ → +0x8a8` 로 가서 상태 0 · 핸들 −1. −1 이 선 위에서 어떤 20 B 가 되는지는 rpcrt4 서버 쪽 컨텍스트 핸들 마샬링을 안 읽어 `[공백]` — 우리 클라이언트는 `max_towers = 4` 라 이 길을 안 탄다.
- 클라이언트 (run133 3-2): 핸들 0 이면 "더 없음", 0 아니면 다음 조각을 또 묻는다. → 탑을 4개 이하로 주면 한 번에 끝난다.

등록 탑 표 (nsisvc → epmapper, 끝점 `LRPC-` + 18자 = 23자). 탑 = `u32 tower_length` + 탑 바이트, 총 **92 B** (4 + 88). 만드는 곳 `BindingAndIfToTower` → `TowerConstruct` (run133 3-1 의 같은 틀, 이번엔 4층이 채워진 꼴):

| 층 | 바이트 | 출처 | 줄 |
|---|---|---|---|
| 머리 | `58 00 00 00` (`tower_length` = `0x40` + 끝점 24 B, run133 3-3 식과 같음) · `04 00` | 끝점 길이 | `TowerConstruct` (sast/130 3-1) |
| 1층 | `13 00` `0d` + nsi UUID 16 B + 주 `01 00` · `02 00` + 부 `00 00` | **서버 인터페이스 구조체의 `InterfaceId`** (nsisvc rva `0x6420`, v1.0) — 질문 탑 아님 | `BindingAndIfToTower +0x88 RpcIfInqId` |
| 2층 | `13 00` `0d` + NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860` + `02 00` · `02 00` + `00 00` | **서버 인터페이스 `+0x18` 의 `TransferSyntax`** (nsi = NDR 2.0 v2.0, `pefile`) — 질문 2층 아님 | `BindingAndIfToTower +0x98 ~ +0xb3` |
| 3층 | `01 00` `0c` `02 00` `00 00` | protseq `ncalrpc` → `0x0c` | `+0x1e6 RtlMultiByteToUnicodeN` → `+0x20e TowerConstruct` |
| 4층 | `01 00` `10` `18 00` + `LRPC-` + 18자 + `00` | **바인딩의 동적 끝점** → ANSI. 정적 끝점이면 문자열 바인딩을 파싱 | `+0xb8 I_RpcBindingInqDynamicEndpointW` · `+0xf5 UnicodeToAnsiString` · `+0x120 RpcStringBindingParseA` |

- 네트워크 주소 칸은 없다 (ncalrpc 는 5층 없음 — sast/130 3-1 표와 같음).
- 이름 부분 18자는 `ServerSetupAddress +0xe8 ProcessPrng` 난수라 **부팅마다 다르다** — 꼴 (길이 23 · 접두사 `LRPC-`) 만 맞추면 된다.

### 4. 맞는 항목이 없을 때 — 두 길 (질문 3) [정적]

- **평범한 길**: `GetEntriesNoLock` 이 탑을 하나도 못 넣으면 (`+0x49f JZ → +0x84b`) — `max_towers ≠ 0` (`+0x852`) · 그때까지 오류 없음 (`+0x854 TEST EDX` · `+0x856 JNZ`) · 거부 표시 아님 (`+0x85c CMP [RSP+0x48],5`) → **`+0x867 MOV EDX,0x16c9a0d6`** → `+0x4a5` 로 돌아가 핸들을 정하고 `+0x4c6` 반환. 거부가 한 번이라도 있었으면 상태는 같고 표시 칸만 1 (`+0xbd8`, SID 를 준 호출만).
- `ept_map_auth_sd` 는 `+0x2c7` 에서 한 번, 트리거 키가 없으면 `+0x38d` 에서 또 한 번 `0x16c9a0d6` 을 쓴다.
- **결과 = 위 응답 칸 표 "없음" 열, 40 B** — `epm.cpp` 의 `reply(0x28)` · `+0x18 = max_towers` · `+0x24 = 0x16c9a0d6` (나머지 0) 과 바이트까지 같다.
- 상태 이름: rpcrt4 가 `0x16c9a0cd ~ +9` 범위를 `0x6d9` 로 (`FatalLookupErrorEncountered +0x26 SUB EAX,0x16c9a0cd` · `+0x2d CMP EAX,9` · `+0x32 MOV EBX,0x6d9`), `0x16c9a0cd` 자체는 `+0x87 MOV EBX,0x6d8` (run129 4절과 같은 함수). DCE 이름 (`ept_s_not_registered`) 은 `[이름 추측]`.
- **다른 길 — 서비스 트리거 키가 있으면 보류**: 조건 (상태 `0x16c9a0d6` · 핸들 0 `+0x2da` · 거부 표시 0 `+0x2e3` · `g_TriggersDisabled == 0` `+0x2ec`) 이 맞으면 `CreateIfIdString` 으로 `"<인터페이스 UUID>[:<객체 UUID>]"` 를 만들어 `+0x346 RtdsTriggerExists` — 키 경로 `HKLM\Software\Microsoft\Windows\CurrentVersion\NetworkServiceTriggers\Triggers\bc90d167-9470-4139-a9ba-be0bbbf5b74d\<…>` (`bc90d167…` = `RPC_INTERFACE_EVENT_GUID`, `pefile`; 객체 쪽을 잘라 한 번 더 `+0x376`). 키가 있으면 잠금 (`+0x39d`) → 다시 찾기 (`+0x40c`) → 그래도 없으면 버퍼 요청을 대기열에 넣고 (`+0x4e5` · `+0x502`) **호출을 끝내지 않는다** (`+0x516`). 인터페이스별 한도를 넘으면 `0x6d8` (`+0x45c`). 보류는 그 인터페이스가 등록될 때 `ept_insert_ex +0x50c CompletePendingEprRequests` 로 풀린다.
  - 이 VM 레지스트리에 nsi UUID 키가 있는지는 안 봤다 `[공백]` — 없으면 평범한 길이 전부.

### 5. 부른 쪽 검사 (질문 4) [정적]

- **호출자 PID · 이미지 · 토큰 비교 없음.** `RpcServerInqCallAttributesW` 는 `GetEntriesNoLock +0x54e` 한 곳 (방화벽 확장 갈래), `RpcImpersonateClient` 는 `ept_insert_ex` 에만.
- **SID (5번째)**: `ept_map_auth +0x27 MOV RDI,[RSP+0x90]` · `+0x2f TEST` · `+0x32 JZ → +0x9e` — **NULL 이면 SD 도 NULL, 검사 없음.** 있으면 `IsValidSid` (`+0x37` · `+0x7c`) → SD 0x28 B `HeapAlloc` (`+0x59`, 실패 `+0x73 JZ → +0x276` 예외 `0xe`) → `InitializeSecurityDescriptor` (`+0xb7`) → ACL (`+0xdf I_RpcAllocate`) → `AddAccessAllowedAce(…, 2, 0x20000, SID)` (`+0x128`) · DACL · 그룹 · 소유자 (`+0x149 · +0x166 · +0x183`). 실패하면 `EDI = 0x16c9a0cd` (`+0x246` 등) → `+0x199` · `+0x2a6` · `+0x24b` → `+0x253 JMP +0x8c` → **예외 `0x6d8`** (`+0x91`). 그 뒤 `ept_map_auth_sd +0xa2 IsValidSecurityDescriptor` (실패 `0x57`).
- SD 가 있으면 항목을 고른 뒤 **등록자 토큰** (`[노드+0x18]`, `ept_insert_ex +0x308 GetCurrentUserToken` → `+0x418`) 으로 `AccessCheck(…, 0x20000, …)` (`GetEntriesNoLock +0x1f2`) — 거부면 건너뛰고 표시 (`+0x21f` · `+0x223`).
- **인터페이스 콜백**: 공개 인터페이스의 `RemoteEpmpServerSecurityCallback` (`0x5a30`) 은 `I_RpcSystemFunction001(4, …)` 이 0 이 아닐 때만 붙는다 (`StartEndpointMapper +0x151 CMP` · `+0x157 LEA` · `+0x163` 깃발 `0x10`; 0 이면 `+0x169` 콜백 NULL). 몸: `g_fOpenEpmpInterface ≠ 0` 이면 허용 (`+0x4` · `+0xd`), 아니면 `I_RpcServerCheckClientRestriction` → 0 아니면 `EPMAllowAnonymous == 3` 일 때만 허용 (`+0x31 ~ +0x3e`), 아니면 **5**. 이 제한이 로컬 ncalrpc 호출자에게도 걸리는지는 `I_RpcServerCheckClientRestriction` 갈래를 못 닫아 `[공백]`.
- 방화벽 확장 (`gFwManagerExtensionState == 1`, `GetEntriesNoLock +0x363`): 호출자가 원격일 때만 확장에 물어 탑을 거른다 (`+0x4fb I_RpcBindingIsClientLocal` · `+0x517 TEST EAX,EAX`) `[추측: ncalrpc 는 로컬로 판정 — 그 함수 안은 안 읽음]`.

### 6. 등록 쪽 (질문 5) [정적]

| 단계 | 한 일 | 줄 |
|---|---|---|
| 주석 길이 | 64 B 이상이면 `0x6d7` (`"NSI server endpoint"` 19자 통과) | `RegisterRpcEndpointsA +0x54 CMP RAX,0x40` · `+0x5a` |
| UUID 벡터 NULL | nil UUID 하나짜리 벡터로 (`Count = 1`) → 객체 UUID nil | `RegisterRpcEndpointsA +0x82 ~ +0x94` |
| replace | 다섯째 인자 1 | `RegisterRpcEndpointsA +0xd8` |
| 바인딩 0개 | epmapper 에 안 보내고 `InterfaceExported` 만 | `RegisterEntries +0x3e` · `+0x41 JZ → +0x39f ~ +0x3ad` |
| epmapper 연결 | `BindToEpMapper` (ncalrpc → `epmapper`) | `RegisterEntries +0x7e` |
| 항목 배열 | `벡터 개수 × 0x58` B (객체 UUID 16 · 탑 포인터 8 · 주석 64), 바인딩마다 탑 (실패한 바인딩은 건너뜀) · 주석 복사 | `+0xeb IMUL RCX,R15,0x58` · `+0x332` · `+0x36a` · `+0x36f ADD R13,0x58` |
| 보내기 | 객체 UUID 하나마다 한 번 `NdrClientCall3(0xe1500, 2, …)` — `0x6ba` · `0x6bf` 면 다시 바인딩해 한 번 더 | `+0x296` · `+0x405` · `+0x48a` |
| 성공 뒤 | `InterfaceExported` | `+0x1a5` |

- **4층 이름**: `I_RpcBindingInqDynamicEndpointW` → `SVR_BINDING_HANDLE::InquireDynamicEndpoint` 가 `[this+0x100] == 1` (동적) 이면 **서버 주소 객체 (`[this+0xe8]`) `+0x10` 의 끝점 문자열을 복사** (`+0x1f` · `+0x2b` · `+0x2f DuplicateString`). 그 문자열을 채우는 쪽이 `LRPC_ADDRESS::ServerSetupAddress` (`+0xe8 ProcessPrng` · `+0x179 DuplicateString`, run133 4-1) 라는 **마지막 연결 (출력 칸 = 주소 객체 `+0x10`) 은 안 읽었다** `[추측]`.
- **다른 끝점도 들어가나 — 예, 전부**: 벡터는 `RPC_SERVER::InquireBindings` 가 그 프로세스의 **모든 서버 주소** (인터페이스 · protseq 로 안 거름) 에서 만든다. `ept_map` 응답엔 질문 protseq (`ncalrpc`) 와 같은 것만 나오고, ncalrpc 주소가 둘 이상이면 탑이 여럿.
- **차례**: 장부 탑 목록은 등록 벡터의 **거꾸로** (`ept_insert_ex +0x279 MOV [RBX],RDI` 새 탑 다음 = 옛 머리 · `+0x27c` 머리 = 새 탑) → 응답 맨 앞 = 벡터의 마지막 ncalrpc 탑. 클라이언트는 원하는 수 (이름 하나) 가 차면 멈추니 (run133 3-2) **첫 탑의 이름을 쓴다.**
- **`ept_insert_ex` 가 장부에 넣는 꼴** (한 줄씩): PSEP 노드 (`+0x165` · 매직 `+0x21b`, `+0x18` protseq · `+0x20` 끝점 · `+0x28` 탑 사본) / IFOBJ 노드 (`+0x3ad` · 매직 `+0x3e3`, `+0x38` 객체 UUID · `+0x48` 인터페이스 UUID · `+0x58` 버전 · `+0x60` 주석 · `+0x18` 등록자 토큰 · `+0x20` PSEP 목록 · `+0x98` 마지막 사용 시각) / **덮어쓰기** — replace 1 이면 같은 프로세스 · 같은 객체 · 인터페이스 · 주 버전 · 장부 부 ≤ 새 부 항목을 찾아 (`+0x6c5 FindIFOBJNode`) protseq 같은 옛 탑을 지우고 새 목록을 앞에 붙임, 버전이 정확히 같으면 그 항목에 합치고 아니면 새 항목 (`+0x45d EnLinkOnIFOBJList`). replace ≠ 1 은 protseq · 끝점이 다 같은 것만 지움. 등록자 프로세스가 끝나면 컨텍스트 핸들 rundown (`HPROCESS_rundown`) 이 지운다 `[이름 추측 · 안 읽음]`.

### 호출 관계

```
nsisvc!NsiStartRpcServer                                                                   [정적]
 ├─ (1) +0x2e5  RpcServerInqBindings ⇒ RPC_SERVER::InquireBindings     그 프로세스 서버 주소 전부
 └─ (2) +0x33e  rpcrt4!RpcEpRegisterW(nsi, 벡터, NULL, "NSI server endpoint")
                └─ +0x14  RegisterRpcEndpointsW +0x6c RegisterRpcEndpointsA (+0xd8 replace 1)
                          └─ +0xe5  RegisterEntries                  항목 0x58 B × 바인딩 수
                                    ├─ +0x83   BindToEpMapper
                                    ├─ +0x337  BindingAndIfToTower   (바인딩마다)
                                    │          ├─ +0x8d   RpcIfInqId                 1층 · 2층 = 서버 인터페이스
                                    │          ├─ +0xbd   I_RpcBindingInqDynamicEndpointW   4층 LRPC-…
                                    │          └─ +0x213  TowerConstruct             92 B
                                    └─ +0x29b  NdrClientCall3(0xe1500, 2)  ── ALPC ──▶ RpcEpMap!ept_insert_ex
                                                                                       ├─ +0xa4  TowerExplode
                                                                                       ├─ +0x279 memcpy  탑 사본 (목록 앞에)
                                                                                       ├─ +0x462 EnLinkOnIFOBJList
                                                                                       └─ +0x511 CompletePendingEprRequests

(질문) rpcrt4!LookupNextChunk +0x11d Ndr64AsyncClientCall(프로시저 8)  ── ALPC ──▶ (svchost RPCSS)   [정적]
RpcEpMap!ept_map_auth_async (8) / ept_map (3, SID NULL)
 └─ +0x53 / +0x51  ept_map_auth (7)              SID NULL → 검사 없음
                   └─ +0x1f3  ept_map_auth_sd
                              ├─ +0x109  TowerExplode(질문, &If, &문법, &protseq, NULL, NULL)   2층 · 4층 안 봄
                              ├─ +0x2c7  GetEntries → GetEntriesNoLock (모드 2)
                              │          ├─ +0x197  ⇢ WildCardMatch     UUID · 주 같음 · 부 장부 ≥ 질문 · 객체 nil
                              │          ├─ +0x2e4  _stricmp            protseq "ncalrpc"
                              │          ├─ +0x416 · +0x43a  HeapAlloc · memcpy   등록 탑 그대로
                              │          └─ +0x4c3 핸들 0 · +0x867 없음 0x16c9a0d6
                              ├─ +0x34b  RtdsTriggerExists      (없음일 때만) 키 있으면 +0x507 NtrbEnqueueRequest 보류
                              └─ +0x1bf  RpcAsyncCompleteCall   응답 (있음 140 B / 없음 40 B)
```

- (나무 오프셋은 ret 자리. `⇒` 는 내보내기 껍데기 안을 건너뜀, `⇢` 는 CFG 간접 호출, `── ALPC ──▶` 는 다른 프로세스로 가는 RPC. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 "지금 우리 맞나 · 틀리나")

flow-writer 는 `../PE_emulator/ext/syscall/syscall_layer_cpp/src/rpc/epm.cpp` (47줄) 를 줄 번호만 훑었다 (읽기만). 고칠지는 사용자 · PE_emulator 결정.

| 우리 지금 (`epm.cpp`) | 진짜 (정적) | 판정 |
|---|---|---|
| `:24 ~ :27` 늘 40 B — 핸들 0 · 탑 수 0 · 배열 `max_towers · 0 · 0` · 상태 `0x16c9a0d6` | "맞는 항목 없음" 답 (`max_towers ≥ 2`) 과 **바이트까지 같다** (3절 표 "없음") | **"없음" 일 때만 맞음**. `:23` `TODO(VM)` ("등록 없음" 응답 스텁 바이트) 은 **정적으로 닫힘** — 서버가 값을 정하는 줄은 `GetEntriesNoLock +0x4c3` (핸들) · `+0x8e` (탑 수) · `+0x867` (상태), 배열 머리는 `[규칙]` |
| `:21` 장부 없음 → 늘 "없음" | nsi 는 **nsisvc 가 서비스 시작 때 등록** (`NsiStartRpcServer +0x337`) — 질문보다 먼저 장부에 있다 `[추측: 서비스 자동 시작 — 서비스 설정은 안 읽음]` | **틀림 — 이게 벽** (`0x6d9` → `WinHttpSendRequest` FALSE 되풀이) |
| `:41` 프로시저 8 만, `:40` "3 · 7 은 공사 중" | 3 · 7 · 8 은 **같은 몸** (비동기 썽크 셋 다). 3 · 7 은 요청 NDR 꼴만 다름 (`FC_FP` 포인터 ID, run137 3-1) | 맞음 — 3 · 7 을 열 땐 같은 장부 함수에 붙이면 됨 |
| `:20` `max_towers > 500` → `nullopt` | 서버 NDR 범위 `0 ~ 500` (run137 `FC_RANGE`) | 맞음 |
| `max_towers = 0` · `1` 일 때도 같은 40 B | 진짜는 `0` → 상태 0 · 핸들 −1, `1` → 핸들 −1 (3절 정리 ③ · `+0x852`) | 우리 클라이언트는 4 라 영향 없음. 다른 샘플이 0 / 1 로 부르면 달라짐 — 선 위 20 B 는 `[공백]` |
| `:18` SID 포인터 ID ≠ 0 → `nullopt` | 진짜는 SID 로 SD 를 지어 등록자 토큰과 `AccessCheck` (5절), 거부면 그 항목만 건너뜀 | 우리 클라이언트는 NULL 이라 지금은 영향 없음 (flow-writer 보탬 — 결과서 표엔 없는 줄) |

**서 바퀴 (등록 장부 + 탑 답) 에 필요한 것** — 정적으로 닫힌 만큼:

1. **장부 항목**: `{ 객체 UUID (nil) · 인터페이스 UUID · 주 · 부 · 주석 (≤ 63자) · 탑 목록 [{ protseq, 끝점, 탑 바이트 }] }`.
2. **누가 · 언제**: nsi (`7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0, 주석 `"NSI server endpoint"`, `ncalrpc`, 끝점 = 우리 nsi 창구 포트 이름 `LRPC-` + 18자) 는 **nsisvc 서비스 시작 때** — 에뮬레이터는 **epmapper 창구가 뜰 때 미리 한 항목을 채우면** 된다. 이름은 nsi 창구 포트 (`\RPC Control\<이름>`) 와 같아야 한다 (클라이언트가 그 이름으로 둘째 연결, run133 3-3).
3. **고르기**: 질문 1층 UUID 같음 + 주 같음 + 장부 부 ≥ 질문 부 + (장부 객체 nil 이거나 같음) + 질문 3층 `0x0c` 이면 protseq "ncalrpc" 인 탑만. 전송 문법 · 4층은 안 씀. 질문 층 수가 4 가 아니면 (`0x0c` 일 때) 상태 `0x16c9a0cd` · 탑 0.
4. **응답 바이트** (n ≤ 4): 핸들 20 B 0 · `n` · `4, 0, n` · 포인터 ID n개 (0 아님) · 탑마다 `[len][len][바이트]` (4 정렬) · 상태 0. nsi 하나면 **140 B**: `+0x14` 1 · `+0x18` 4 · `+0x1c` 0 · `+0x20` 1 · `+0x24` 포인터 ID · `+0x28` `0x58` · `+0x2c` `0x58` · `+0x30 ~ +0x87` 탑 88 B (3절 탑 표) · `+0x88` 0. 응답 머리 `TotalLength 0xcc`.
5. **탑 바이트**: 3절 등록 탑 표 그대로 — 1층 nsi v1.0 · 2층 NDR 2.0 v2.0 (질문이 NDR64 를 물어도 이것) · 3층 `0x0c` · 4층 `0x10` + `18 00` + 이름 23자 + NUL.
6. 샘플이 `RpcEpRegister*` 를 직접 부르면 로컬 인터페이스 `0b0a6584` v1.1 프로시저 2 (`ept_insert_ex`) 를 받아야 하는데, 그 요청 NDR 바이트는 안 풀었다 (다음 요청 후보 1).

**정적으로 못 닫은 것** (결과서 그대로):

- `[공백]` nsisvc 가 든 svchost 의 **ncalrpc 주소 개수** — 탑 수와 첫 탑 이름이 달라짐 (동적 D4).
- `[공백]` 트리거 키가 이 VM 에 있는지, `g_TriggersDisabled` · `gFwManagerExtensionState` · `EPMAllowAnonymous` 값 (D6 · D7).
- `[공백]` `max_towers ≤ 1` 의 −1 핸들이 선 위에서 되는 20 B.
- `[추측]` 4층 이름 = 주소 객체 `+0x10` = `ServerSetupAddress` 의 이름 (마지막 연결).
- `[공백]` `I_RpcServerCheckClientRestriction` 의 ncalrpc 갈래.
- `[공백]` `ept_insert_ex` 요청 NDR 바이트.

## 확인된 것

- `[정적]` 공개 EPM 프로시저 3 · 7 · 8 은 `ept_map_auth` → `ept_map_auth_sd` → `GetEntries` → `GetEntriesNoLock` 한 몸. 0 · 1 · 5 · 6 은 예외 `0x6d8` 만. 등록은 로컬 인터페이스 `0b0a6584` v1.1 프로시저 2 (`ept_insert_ex`), ncalrpc 만 받음.
- `[정적]` 고르기 = UUID 같음 · 주 같음 · **장부 부 ≥ 질문 부** · 장부 객체 nil 이면 무엇이든 · protseq 대소문자 무시 같음. **전송 문법 · 4층 안 봄.**
- `[정적]` 돌려주는 탑 = 등록 탑 바이트 사본 (두 번 `memcpy`, 층을 고쳐 쓰지 않음 — TCP · HTTP 탑 `+0x4b` 만 예외). `max_towers` 개까지, 항목 안 차례는 등록의 거꾸로.
- `[정적]` 끝점 핸들 = `max_towers ≥ 2` 이고 넘치지 않으면 0, 넘치면 이어받기 블록, `max_towers ≤ 1` 이면 −1.
- `[정적]` 없음 = 상태 `0x16c9a0d6` · 탑 0 · 핸들 0 → 40 B (= 에뮬레이터 지금 답). 트리거 키가 있으면 보류.
- `[정적]` 프로시저 안에 호출자 신원 검사 없음 — SID (선택) 와 조건부 인터페이스 콜백뿐.
- `[정적]` `RpcEpRegisterW` 는 그 프로세스의 **모든 바인딩** 마다 탑 하나를 만들어 한 번에 보낸다. 1 · 2층 = 서버 인터페이스, 4층 = 동적 끝점. 주석은 항목 칸에만 (`ept_map` 응답엔 안 실림).
- `[관찰]` test.exe 초기화 끝 모듈 6개에 rpcrt4 가 없다 (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 과 같은 줄) — 이번 길은 test.exe 에선 안 생긴다.

## flow-writer 대조

결과서 오프셋을 `.asm` 과 맞춰 봤다. **어긋난 곳 둘** (고친 값을 이 문서에 씀) + 표기 차이:

1. **`GetEntriesNoLock +0x4f9 CALL I_RpcBindingIsClientLocal`** (결과서 4절) → `+0x4f9` 는 `XOR ECX,ECX` (첫 인자 0). **call 은 `+0x4fb` (ret `+0x502`)**. 뒤로 `+0x507 CMP EAX,0x6e4` · `+0x50c JNZ` · `+0x512 MOV EAX,1` · `+0x517 TEST EAX,EAX` 가 붙는다 — 이 문서는 `+0x4fb` 로 적음.
2. **"`+0xbcf` 에서 `[접근거부표시] = 1`"** (결과서 3-1) → `+0xbcf` 는 `TEST R8,R8` (표시 칸 포인터가 NULL 인지), `+0xbd2 JZ → +0x867` (NULL 이면 상태만), **쓰는 줄은 `+0xbd8 MOV dword [R8],1`** — 이 문서는 `+0xbd8` 로 적음.
3. **`GetEntriesNoLock +0x192` 는 직접 call 이 아니다**: `CALL 0x180013010` (CFG 간접 디스패치, `_calls.tsv` 표시 `indirect:cfg:?`). 대상은 `GetEntries` 로 받은 비교 함수 = `ept_map_auth_sd +0x268 LEA RDX,[0x180001240]` → `+0x275 MOV [RSP+0x68],RDX` (`WildCardMatch`). 결과서의 "`+0x192` 가 `WildCardMatch` 를 부름" 과 뜻은 같고, 표 · 나무에 `⇢` 로 적었다.
4. **`ept_map_auth` SID 갈래 실패 값** — 결과서 "어느 하나라도 실패하면 예외 `0x6d8`" 에 하나 보탬: SD 0x28 B `HeapAlloc` 실패는 `+0x73 JZ → +0x276 MOV ECX,0xe` → `+0x27b RpcRaiseException` = **예외 `0xe`**. SD 채우기 실패는 `EDI = 0x16c9a0cd` (`+0x246` · `+0x258` · `+0x262` · `+0x26c` · `+0x292` · `+0x29c`, ACL `I_RpcAllocate` 실패는 `+0x288 EDI = 0xe`) → `+0x199 TEST EDI` · `+0x19b JNZ → +0x2a6` (ACL 풀기) → `+0x24b` → `+0x253 JMP +0x8c` → 예외 `0x6d8` (`+0x246` 만 `+0x24b` 로 바로). 그리고 `IsValidSid` 는 `+0x37` · `+0x7c` 두 번.
5. **끝점 핸들 갈래 앞 두 줄** (결과서 표 1 은 `+0x4a5 ~ +0x4c3` 을 한 묶음으로): `+0x4ab CMP ECX,EAX` · `+0x4ad JA → +0x4c6` (핸들 안 건드림 — `ECX` 뜻은 안 따라감) · `+0x4af CMP [RBP-0x78],0` · `+0x4b4 JNZ → +0x4c6` (이어받기 블록이 있으면 `+0x885` 에서 이미 씀). 그다음 `+0x4b6 CMP EAX,1` · `+0x4bd JBE → +0xa36` (−1) / `+0x4c3` (0).
6. **alias**: `_calls.tsv` 의 `alias:X+1` (`ept_map_auth_sd +0x39d` `Request` · rpcrt4 `InterfaceExported` · `BindingAndIfToTower` · `RtlStringCbCopyA` · `InquireBindings` · `UnicodeToAnsiString` · `DuplicateString`) 은 같은 함수의 C++ 장식 이름 하나 (`.asm` `ALIASES(1)`) — `pdb_aliases.py --lookup` 로 `RpcEpMap.dll 0x5a7c · 0x1240 · 0x13010`, `rpcrt4.dll 0x62e58 · 0x62060 · 0x654c4 · 0x9d5a0` 모두 "별칭 무리 아님". PDB 이름 그대로 썼다 (run133 과 같은 모양). **별칭 무리는 `0x83c0` 하나** — 같은 주소 이름 4개 (`ept_inq_object` · `ept_insert` · `ept_delete` · `ept_mgmt_delete`), 하는 일 ("예외 `0x6d8` 만 던지는 16 B 몸") 로 적음.
7. **이름**: 요청서의 `RemoteEpmpSecurityCallback` 은 PDB 이름 `RemoteEpmpServerSecurityCallback` (rva `0x5a30`, `pdb_aliases.py --where` 로 다시 확인) — 결과서 정정과 같음.
8. **tower_length 식**: 결과서 표 3 "끝점 RHS 24 + 5 + 0x3b = 0x58" 은 run133 3-3 의 "`0x40` + 끝점 길이 (NUL 포함)" 과 같은 값 (`0x40 + 24 = 0x58`). 이 문서는 run133 식으로 적음.

## 앞 run 고쳐 읽기

틀린 것은 없고 `[추측]` · `[규칙]` 이 정적으로 닫힌 것이라 "정정" 이 아니라 "고쳐 읽기" 로 각 문서 맨 위에 한 줄씩 넣었다.

- **run133**: 3-3 "1 · 2층 값 · 길이 칸은 `TowerConstruct` 가 쓰는 꼴 그대로 두면 안전 — 진짜 epmapper 가 등록 때 받은 탑을 그대로 돌려준다고 짐작 `[추측]`" → **`[정적]`** (`ept_insert_ex +0x274` · `GetEntriesNoLock +0x435` 두 번 `memcpy`, 층을 고쳐 쓰지 않음). 보탤 점: 1 · 2층은 **서버 인터페이스** 값 (질문 2층과 무관), 탑 수는 그 프로세스의 ncalrpc 주소 수만큼, 차례는 등록의 거꾸로. `LRPC-` 이름이면 `tower_length` `0x58` (같은 식).
- **run137**: 3-1 "탑 0개: 끝점 핸들 0 · `num_towers` 0 · 배열 `4 · 0 · 0` · 상태 → 40 B `[규칙]`" → 서버가 값을 정하는 줄이 **`[정적]`** 으로 닫힘 (`GetEntriesNoLock +0x4c3` 핸들 0 · `+0x8e` 탑 수 0 · `+0x867` `0x16c9a0d6`). 단 `max_towers ≤ 1` 이면 핸들 −1. 배열 머리는 그대로 `[규칙]`.

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (rpcrt4 안 올림) 이고, epmapper 는 다른 프로세스 (svchost RPCSS) 다.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" D1 ~ D7 — 대기열 **Q280** (원문 물음 2 · 3 의 VM 묶음, Q231 ④⑤⑥ · Q258 ①⑧ · Q271 과 같은 세션) 에 얹음. **RpcEpMap 멈춤점은 전부 svchost (RPCSS, 서비스 `RpcEptMapper`) 안** 의 유저 멈춤점 — 커널 디버거에서 그 프로세스 조건을 붙인다. `rbp` = `ept_map_auth_sd +0x10 LEA RBP,[RSP-0x10]` 의 값):

1. **D1** `RpcEpMap!ept_map_auth_sd+0x109` (`TowerExplode` ret) — `eax` 0, `[rbp-0x28]` (UUID 16 B + 주 · 부), `[rbp-0x60]` "ncalrpc".
2. **D2 (필수)** `RpcEpMap!ept_map_auth_sd+0x2c7` (첫 `GetEntries` ret) — `eax` 상태 · `[r13]` 탑 수 · `[rbp-0x48]` 가 가리키는 핸들 칸 · `[rbp-0x40]` 배열의 포인터 n개 → 각 탑 바이트가 3절 탑 표와 같은지. 원문 물음 2 의 `rpcrt4!IoCompleted+0xcf` 와 한 실행에 걸면 서버가 만든 탑과 클라이언트가 받은 버퍼를 맞대 볼 수 있다.
3. **D3** `RpcEpMap!GetEntriesNoLock+0x4c6` (마지막 상태) · `+0x867` (없음) — `edx`.
4. **D4 (필수, 덤프만)** `dq RpcEpMap!IFObjList L1` → 노드 `+0x48` 이 nsi (`7ea70bcf…`) 인 것의 `+0x38` · `+0x58` · `+0x60` · `+0x20` → PSEP 마다 `+0x18` · `+0x20` · `+0x28` — 등록 탑 그대로 · 개수 · 차례 (ncalrpc 주소 개수 `[공백]` 을 닫음). `ept_insert_ex` 는 부팅 때 지나가 잡기 어렵다.
5. **D5 (선택)** `rpcrt4!RegisterEntries+0x29b` (`NdrClientCall3` ret) — **nsisvc 가 든 svchost (LocalService)** 에서 서비스 시작 때, 항목 배열 `0x58 × n`.
6. **D6** 레지스트리 `HKLM\Software\Microsoft\Windows\CurrentVersion\NetworkServiceTriggers\Triggers\bc90d167-9470-4139-a9ba-be0bbbf5b74d\` 아래 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` 키 있음 / 없음.
7. **D7** `RpcEpMap!gFwManagerExtensionState` (rva `0x1cf60`) · `g_TriggersDisabled` (`0x1ccc8`) · `EPMAllowAnonymous` (`0x1c5e8`) 값.

정적 (결과서 "다음 요청 후보" — 대기열 번호는 오케스트레이터가 붙임):

1. `ept_insert_ex` (로컬 `0b0a6584` 프로시저 2) NDR 요청 · 응답 바이트 — 샘플이 `RpcEpRegister*` 를 직접 부르거나 에뮬레이터가 nsisvc 등록을 흉내 낼 때. 같은 인터페이스 0 (`OpenEndpointMapper`) · 3 (`ept_delete_ex`) 은 이름만.
2. `I_RpcServerCheckClientRestriction` · `IsSecureClient` 의 ncalrpc 갈래 (5절 `[공백]`).
3. `ServerSetupAddress` ↔ 서버 주소 객체 `+0x10` (`CreateOrUpdateAddresses`) — 6절 `[추측]`.
4. 서버 컨텍스트 핸들 −1 마샬링 (`NdrServerContextNewMarshall` 류) — `max_towers ≤ 1` 호출이 오면.
5. 원문 물음 3 의 정적 몫 (nsisvc 프로시저 6 · webio `Startup` 다음) — Q280 쪽.
6. 셸 꼴 16 · exe 직접 4 가 묻는 인터페이스 — 정적보다 동적 D2 의 `[rbp-0x28]` 한 번이 빠름.

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 (epmapper 고르기 · 채우기 · 등록) 에서 디버거 여부를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`.
- **스크립트 문제 없음** (결과서). 읽을 때 조심할 것 (결과서 메모): Ghidra `.c` 에서 `ept_map_auth_sd` · `ept_insert_ex` 의 `TowerExplode` 인자가 4 · 3개로 줄어 보이지만 실제 6개 (`.asm` 레지스터로 셈), `GetEntriesNoLock.c` 는 반환 `void` 로 나오지만 `.asm` 은 `EAX` 로 상태를 돌려준다 (`+0x4c6`). `refs` 는 PDB 이름만 받는다.
- **폴더 이름**: 스크립트가 모듈 이름을 소문자로 만들어 `ms/{decomp,disassemble}/rpcepmap/` (바이너리 이름 `RpcEpMap.dll`). `pdb_aliases.py` 는 `RpcEpMap.dll` (대소문자 그대로) 로 불러야 표를 찾는다.
- 같은 때 다른 decompiler 셋 (sast/142 user32 · win32k, sast/144 com-rpcss, sast/145 gdi-display-dc) 이 `ms/` · `_workspace/sast/` 에서 일했다 — 이 문서는 그 파일을 안 건드렸다.

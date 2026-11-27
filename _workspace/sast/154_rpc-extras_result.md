# 154 rpc-extras — 정적 분석 결과

- 요청서: `_workspace/sast/154_rpc-extras.md` (Q292, 덤 1 ~ 3) · 원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` "덤" 표 (읽기만) · PE_emulator 쪽 읽은 것 (읽기만): `ext/syscall/syscall_layer_cpp/src/rpc/nsi.cpp` (120줄), `lrpc.cpp` (382줄), `alpc/nt_alpc_cleanup.cpp` 앞 40줄.
- 날짜: 2026-11-27. 빌드: Windows 11 24H2 26100.9457 x64.
- 모듈: winnsi (`System32/winnsi.dll`) · rpcrt4 (`System32/rpcrt4.dll`).
- **Ghidra 쓴 곳**: rpcrt4 프로젝트만 열었다 (analyze 안 함). `export` 3번 (rva 지정, depth 0, 함수 27개) + `refs` 15번 (`_refs/`). **winnsi 프로젝트는 안 열었다** — `pefile` 로 `.rdata` 를 읽고 이미 있는 `ms/disassemble/winnsi/*.asm` 만 봤다. 새 export 앞마다 `free -g` 확인 (free 5 → 1 → 2 GB, available 27 ~ 28 GB). 다른 모듈 프로젝트는 안 열었다. 스크래치 `scratchpad/s154/`.
- 이 문서에 디컴파일 코드를 붙이지 않았다. 바이트 몇 개 (PE 데이터) 와 우리 말 요약뿐.

## 스크립트 문제

없음. (`export` · `refs` 모두 정상 종료. `refs` 는 함수 시작 rva 를 줘도 호출한 곳을 다 찾아 준다 — 이번에 요긴했다.)

## PDB GUID 확인

| 모듈 | 바이너리 디버그 디렉터리 (CodeView `RSDS`, `pefile`) | `ms/sym/<이름>.pdb.info.txt` | 결과 |
|---|---|---|---|
| winnsi.dll | `907CE42FFA71F5085D1468AD2D40D62C1` | `907CE42FFA71F5085D1468AD2D40D62C1` | **같다** |
| rpcrt4.dll | `0204A8A137C42B7DF1F0811A3A8277B21` | `0204A8A137C42B7DF1F0811A3A8277B21` | **같다** (sast/149 와 같은 값) |

- 커널 기준 GUID (`C29EBFB0…`) 는 ntoskrnl 용 — 이번에 ntoskrnl 을 안 열었다.

## 버전 대조 ("알려진 사실" 표에 winnsi · rpcrt4 줄 없음 → 원본 바이트로 대조) — 일치

| 자리 | 확인 | 결과 |
|---|---|---|
| winnsi `NsiRpcDeregisterChangeNotificationEx +0x123 / +0x302` | asm: `LEA EDX,[R8+6]` 뒤 `+0x12e Ndr64AsyncClientCall` (프로시저 6), `+0x302` 이 `RpcAsyncCancelCall` (`_calls.tsv` ret `+0x309`) | run129 · 요청서와 같다 |
| winnsi `NsiRpcRegisterChangeNotificationEx +0x543 / +0x54e` | `LEA EDX,[R8+7]` 뒤 `+0x54e` = 프로시저 7 | 같다 |
| rpcrt4 `AlpcSendCancelMessage +0x9a` | 바이트 `48 ff 15 8f 6d 08 00` (7 B) → ret `+0xa1` (= asm). 가져오기 칸 rva = `0x6272a + 7 + 0x86d8f` = `0xe94c0` (`NtAlpcSendWaitReceivePort`, `refs` 가 찾은 15곳이 쓰는 칸과 같다) | 맞음 |
| 〃 `+0x88` · `+0x57` · `+0x4f` | `ba 00 00 01 00` (`MOV EDX,0x10000`) · `48 c7 44 24 68 04 00 00 00` (`[RSP+0x68]` = 4 → 메시지 `+0x28`) · `c7 44 24 40 18 00 40 00` (DataLength `0x18` · TotalLength `0x40`) | 맞음 |
| rpcrt4 `CancelAsyncCall +0x73 / +0x78 / +0x8a` | `83 f8 09` · `83 f8 0c` · `e8 d1 06 00 00` → 대상 `0x61f30 + 0x8f + 0x6d1 = 0x62690` (`AlpcSendCancelMessage`) | 맞음 |
| rpcrt4 `AlpcCancelMessage +0xe9 / +0x27 / +0xdf` | `b9 02 00 00 00` (`Sleep(2)`) · `8b 53 14` (`[속성+0x14]`) · `83 7b 18 00` (`[속성+0x18]`) | 맞음 |
| rpcrt4 `SendReservedMessage +0x2d1` | `49 c7 46 28 05 00 00 00` (`[버퍼+0x28]` = 5) | 맞음 |
| winnsi `.rdata` 오프셋 표 `0x7310` | `[0, 66, 132, 204, 270, 336, 390, 438, 498]` → 프로시저 5 = `0x7212` (run137 3-0 과 같다), 6 = `0x7248`, 7 = `0x7278`, 8 = `0x72b4` | 맞음 |

---

## 먼저 읽는 한 줄 답

**덤 1 (winnsi 6 · 7 형식 글)**: 6 · 7 의 NDR 2.0 파라미터 표를 읽었다 (1절). **6 응답 24 B 는 맞다** (컨텍스트 핸들 20 + 반환 4). **6 요청 52 B 는 틀렸다 — 56 B** (모듈 ID 가 GUID 일 때; LUID 면 48 B). 까닭: 모듈 ID 는 비캡슐 공용체라 **가름값 (enum16, 2 B) 이 선 위에 한 번 더 나가고** 공용체 몸은 4 정렬이다 (`NdrNonEncapsulatedUnionMarshall` 이 가름값을 버퍼에 쓰는 줄 `[정적]`; 이 함수가 불리는 사슬은 `[추측]` → 진짜 길이는 VM 으로, 아래 1번) → 모듈 ID 선 위 길이 = 24 B (GUID) / 16 B (LUID), run137 3-2 의 "20 B" 가 아니다. 같은 이유로 **프로시저 5 요청도 40 B 가 아니라 44 B** (GUID). **7 요청 = 컨텍스트 핸들 20 B 하나.** **7 완료 응답 = 32 B** (출력 셋 + 반환; 포인터 둘이 NULL 이고 반환 `0x4c7` 일 때) — 그리고 진짜 서버는 걸린 7 을 **fault 가 아니라 일반 응답 (반환값 0x4c7)** 으로 끝낸다.

**덤 2 (`RpcAsyncCancelCall` 보내기만 `0x10000`)**: `Flags 0x10000` 은 `LRPC_CASSOCIATION::AlpcSendCancelMessage +0x88 MOV EDX,0x10000`. 보내는 것 = **LRPC 종류 4 (취소)**, 0x40 B (`DataLength 0x18` · `TotalLength 0x40` · `Type 0x4000` · `+0x28` = 4 · `+0x34` = 호출 ID), 속성 없음, 받기 칸 없음. 서버는 **답하지 않는다** (서버 `ProcessIO +0xa0e` 가 `HandleCancelMessage` 만 부르고, 답 보내기는 메시지 `Type` 의 `0x2000` 비트가 켜졌을 때뿐 — 보내기만 한 메시지는 안 켜짐 `[추측: 비트 뜻]`). 클라이언트가 **2 ms 씩 자는 곳은 `AlpcCancelMessage +0xee Sleep(2)`**, 끝 조건은 단 하나 — **그 부름이 요청 때 커널에 넘긴 속성 칸 (`호출+0x128`) 의 Context 속성 `MessageId` (`+0x14`) 또는 `CallbackId` (`+0x18`) 가 0 이 아니게 되는 것**. 시간 제한 · 다른 탈출 없음. 진짜 커널은 요청을 보낼 때 이 칸을 채워 주니 (`[추측]` — 커널 쪽은 안 열었다) 진짜 윈도에선 한 번도 안 잔다; 에뮬레이터가 비동기 요청 때 이 칸을 안 채우면 **영원히** 잔다 (BG02 의 `NtDelayExecution(2 ms)` 끝없음과 맞는다, 2절).

**덤 3 (종류 0 · 1 밖 메시지)**: 클라이언트가 nsi 포트로 보낼 수 있는 종류 0 · 1 밖은 **4 (취소)** 와 **5 (예약 메시지 묶음)** 둘뿐이다 (rpcrt4 의 `NtAlpcSendWaitReceivePort` 부르는 곳 15곳을 다 봄, 3절). **"새 스레드의 첫 호출 앞" 에 붙어 나가는 것은 5**: 부름마다 거치는 `NegotiateTransferSyntax` 가 (조건이 맞으면, `+0x49c`) **`FindOrCreateCausalFlow`** 를 부르고, 이 스레드가 그 바인딩에서 처음이면 (새 스레드) 새 인과 흐름 객체를 만들면서 **`CleanupOrphanedCasaulFlowEntries +0xec`** 로 **죽은 스레드들이 남긴 흐름 항목**을 모아 `SendCausalFlowListForCleanup` 에 넘기고, 연결에 쌓인 항목이 **12개 이상**이면 (`+0x56 CMP [연결+0x128],0xc`) **`SendReservedMessage` (종류 5, Flags 0, 보내기만)** 가 나간다. 그 뒤 바로 이어지는 것이 그 스레드의 요청 (종류 0, 프로시저 5) — 원문 그림과 같다. **PE_emulator 코드와 대조하면 4 는 아니다**: 종류 4 는 Flags `0x10000` 이라 `lrpc_shape_error` (`lrpc.cpp:139`) 에서 `svc_async_unsupported` 로 걸러지고 (BG02 에서 본 그 이유와 같다), 원문의 `svc_lrpc_kind_unsupported` (`:280 · :328`) 까지 오려면 Flags 0 · 보내기만 (`:328`) 이어야 한다 = 종류 5. 칸: `+0x00` DataLength = `0xd8 − 남은 칸` · `+0x02` TotalLength = `0x100 − 남은 칸` · `+0x04` Type `0x4000` · `+0x18` MessageId = `[연결+0x148]` · `+0x28` = 5 · 본문 `+0x30 ~` 항목들 (3-1). 서버는 **답한다** (`HandleReservedMessageRequest`), 클라이언트는 **기다리지 않는다** (비동기; 답은 나중에 `IoCompleted +0x394 → HandleReservedMessageReply` 가 받고, 안 와도 막히지 않는다 `[추측]`). 한계: 그 로그의 표본에서 12개가 실제로 쌓였는지는 로그가 없어 못 가른다 — Flags 와 `+0x28` 을 남기면 끝난다.

---

## 1. winnsi 프로시저 6 · 7 (덤 1) [정적]

### 1-1. 어디서 읽었나 (`pefile`, 전부 `.rdata` rva)

| 항목 | rva | 읽은 것 |
|---|---|---|
| 프록시 정보 | `0x6000` | `[0]` pStubDesc `0x6040` · `[1]` 프로시저 문자열 `0x70c2` · `[2]` 오프셋 표 `0x7310` · `[3]` NDR 2.0 문법 식별자 `0x6e88` · `[4]` 개수 2 · `[5]` 문법 표 `0x6150` |
| 문법 표 (항목 둘, 80 B 씩) | `0x6150` | `[0]` NDR 2.0 (`8a885d04-1ceb-11c9-9fe8-08002b104860` v2.0) → 프로시저 `0x70c2` · 오프셋 `0x7310` · **타입 문자열 `0x6eb2`**. `[1]` NDR64 (`71710533-…` v1.0) → 포인터 표 `0x6940` |
| 프로시저 시작 rva | `0x70c2 +` 표 | 5 = `0x7212` (54 B) · **6 = `0x7248` (48 B)** · **7 = `0x7278` (60 B)** · 8 = `0x72b4` (표가 9칸이라 있다. 이 문서 범위 밖 — 동기 `Oi2 0x46` 에 파라미터 6) |
| 이 문서의 "타입 +0x.." | 타입 문자열 `0x6eb2` | rva = `0x6eb2` + 거리 |

### 1-2. 머리 (NDR 2.0 Oif) — 5 · 6 · 7 비교

| 칸 | 5 (등록, 비교용) | **6 (해제)** | **7 (알림 기다림)** |
|---|---|---|---|
| 앞 두 바이트 (핸들 종류 · Oi 깃발) | `00 48` | `00 48` | `00 48` |
| 프로시저 번호 | 5 | 6 | 7 |
| 스택 크기 | `0x30` | `0x28` | `0x38` |
| 핸들 | 명시적 `FC_BIND_PRIMITIVE`, 스택 자리 8 (자리 0 = 비동기 핸들) | 같음 | 같음 |
| 클라이언트 · 서버 상수 | `0x18` · `0x40` | `0x38` · `0x40` | `0x24` · `0x22` |
| Oi2 깃발 | `0xc6` | `0xc6` | `0xc5` |
| 파라미터 수 | 4 | 3 | 5 |
| 확장 머리 10 B | `0a 85 00 00 01 …` | 같음 | `0a 03 01 00 00 …` |

- Oi2 비트 `[추측: 이름]`: 비트 0 서버 크기 · 1 클라이언트 크기 · 2 반환 있음 · 6 확장 있음 · 7 비동기 핸들. `0xc6` = 1 · 2 · 6 · 7, `0xc5` = 0 · 2 · 6 · 7 → 6 · 7 둘 다 **비동기** (winnsi 가 `Ndr64AsyncClientCall` 로 부르는 것과 맞다, run137 4절).
- 상수 `0x38` (6 요청) 은 아래 56 B 와 우연히 같다 `[추측]`. 5 의 `0x18` 은 44 B 와 안 맞아서 이 상수들로 선 위 길이를 정하지는 않았다.

### 1-3. 파라미터 표 — 프로시저 6 (`ndr_deregister` `[추측: 이름 대응]`) — 파라미터 3 (`0x7260 ~ 0x7277`)

attr 비트 `[추측: 이름]`: `0x0002` 해제 · `0x0008` 입력 · `0x0010` 출력 · `0x0020` 반환 · `0x0040` 기본형 · `0x0080` 값 전달 · `0x0100` 단순 참조 · `0x2000 ×n` 서버 할당 크기 (n × 8 B).

| # | attr | 스택 | 타입 오프셋 → 타입 바이트 | 포인터 종류 | 뜻 |
|---|---|---|---|---|---|
| 0 | `0x010b` (크기 · 해제 · 입력 · 단순 참조) | `0x10` | `+0x4c` (`0x6efe`) `1a 03 18 00 00 00 08 00 08 40 36 08 40 5b` = `FC_BOGUS_STRUCT` 메모리 24 B: LONG · 패딩 · **POINTER** (→ `+0x3c` 모듈 ID, `FC_UP`) · LONG · 패딩 | 참조 (ID 없음). 구조체 안 포인터는 `FC_UP` (ID 4 B) | 24 B 구조체 {예약 u32, **모듈 ID 포인터**, 객체 번호 u32} — **5 의 첫 파라미터와 같은 타입** |
| 1 | `0x0118` (입력 · 출력 · 단순 참조) | `0x18` | `+0x1bc` (`0x706e`) `30 e0 00 00` = `FC_BIND_CONTEXT` 깃발 `0xe0` (포인터 경유 `0x80` · 입력 `0x40` · 출력 `0x20`) | 참조 | **컨텍스트 핸들 (입력 · 출력)** 20 B |
| 2 | `0x0070` (출력 · 반환 · 기본형) | `0x20` | 기본형 `08` (`FC_LONG`) | — | **반환 DWORD** |

- 5 의 파라미터 둘째 (8 B 구조체 compartment) 가 6 에는 없다. 5 의 컨텍스트 핸들은 깃발 `0xa0` (출력만), 6 은 `0xe0` (입력 + 출력).

### 1-4. 파라미터 표 — 프로시저 7 (`ndr_request_change` `[추측: 이름 대응]`) — 파라미터 5 (`0x7292 ~ 0x72b3`)

| # | attr | 스택 | 타입 오프셋 → 타입 바이트 | 포인터 종류 | 뜻 |
|---|---|---|---|---|---|
| 0 | `0x0008` (입력) | `0x10` | `+0x1c0` (`0x7072`) `30 41 00 00` = `FC_BIND_CONTEXT` 깃발 `0x41` (입력 `0x40` · null 불가 `0x01`) — **포인터 경유 비트 없음 = 값** | 값 | **컨텍스트 핸들 (입력)** 20 B. 요청의 전부 |
| 1 | `0x4113` (크기 · 해제 · 출력 · 단순 참조 · 서버 할당 16 B) | `0x18` | `+0x82` (`0x6f34`) `1a 03 10 00 00 00 06 00 36 08 40 5b` = `FC_BOGUS_STRUCT` 16 B: **POINTER** · LONG · 패딩. 포인터 `12 00 e6 ff` (`FC_UP`) → `+0x76` (`0x6f28`) = **`1b 00 01 00 19 00 08 00 00 00 02 5b` 바이트 배열**, 개수 = 구조체 `+8` 의 LONG (`19 00 08 00`) | 참조. 구조체 안 포인터 `FC_UP` | 출력 구조체 A (16 B) = {**바이트 배열 포인터**, 그 길이 u32} |
| 2 | `0x6113` (… 서버 할당 24 B) | `0x20` | `+0xa2` (`0x6f54`) `1a 03 18 00 00 00 08 00 0d 40 36 08 08 5b` = `FC_BOGUS_STRUCT` 24 B: **ENUM16** · 패딩 · **POINTER** · LONG · LONG. 포인터 `12 00 e4 ff` → `+0x96` (`0x6f48`) = 바이트 배열 (`1b 00 01 00 19 00 1c 00 00 00 02 5b`) | 참조. 구조체 안 포인터 `FC_UP` | 출력 구조체 B (24 B) = {enum16, **바이트 배열 포인터**, LONG, LONG} |
| 3 | `0x2010` (출력 · 서버 할당 8 B) | `0x28` | `+0x1cc` (`0x707e`) `11 0c 0d 5c` = `FC_RP` (단순 포인터) → `FC_ENUM16` | 참조 | 출력 **enum16** (선 위 2 B) |
| 4 | `0x0070` | `0x30` | 기본형 `08` | — | **반환 DWORD** |

- 배열 개수 칸 뜻: A 는 구조체 `+8` (LONG) 로 분명하다. **B 는 `19 00 1c 00` = 오프셋 `0x1c` 인데 구조체가 24 B (`0x18`) 라 구조체 안에 안 들어온다** — 기준이 다르다고 보이지만 풀지 않았다 `[공백]`. 포인터가 NULL 이면 상관없다 (아래 길이 표).
- 이 표의 모양은 run129 3절 ("7 = 컨텍스트 핸들 (in) → 16 B 구조체 · 24 B 구조체 · DWORD · 반환 DWORD") 과 맞고, 그 "DWORD" 는 **enum16 (선 위 2 B)** 으로 고쳐 읽는다. 구조체 A · B 가 모듈 ID 를 담는다는 짐작도 틀렸다 — 담는 것은 바이트 배열 포인터다 (모듈 ID 구조체를 가리키는 건 5 · 6 의 첫 파라미터뿐).

### 1-5. 모듈 ID 의 선 위 꼴 — 길이를 가른 증거 [정적]

5 · 6 의 24 B 구조체 안 포인터가 가리키는 `+0x3c` (`0x6eee`) 의 타입:

| 자리 | 바이트 / 값 | 뜻 |
|---|---|---|
| `0x6eee` | `1a 03 18 00 00 00 00 00 06 3e 0d 4c 00 bd ff 5b` | `FC_BOGUS_STRUCT` 메모리 24 B: SHORT (`Length`) · 패딩 2 · **ENUM16 (`Type`)** · **`FC_EMBEDDED_COMPLEX` → `-0x43` = `0x6eb8`** |
| `0x6eb8` | `2b 0d 06 00 fc ff 01 00 02 00` + 항목 설명 | **`FC_NON_ENCAPSULATED_UNION`**, 가름 종류 ENUM16, 가름값은 **구조체 안 4 B 앞 (`fc ff` = −4) 의 `Type`** |
| `0x6ec2` | `10 00 02 00 01 00 00 00 10 00 02 00 00 00 16 00 00 00` | 공용체 메모리 16 B, 항목 2개: **값 1 → `0x6eda` GUID 구조체 16 B**, **값 2 → `0x6ee6` 8 B 구조체 (LUID)**, 기본 항목 `00 00` = 비어 있음 |

- 구조체 안의 `Type` 과 공용체의 가름값은 같은 값이지만 **공용체가 자기 가름값을 한 번 더 쓴다**: `rpcrt4!NdrNonEncapsulatedUnionMarshall` (`ms/disassemble/rpcrt4/NdrNonEncapsulatedUnionMarshall.asm`) `+0x20` 에서 가름값을 구하고 (`NdrpComputeConformance`) `+0x5d ~ +0x65` 에서 **가름 종류별 쓰기 표 `[0xe33c0 + 종류×8]` 를 부른다**. ENUM16 칸 (`0x0d`) = `0x900d0` `NdrpSimpleTypeEnum16Marshall` — 2 정렬로 **2 B 를 버퍼에 쓰고 버퍼 포인터를 2 올린다** (바이트 `48 83 ec 28 f7 02 00 80 ff ff … 0f b7 02 66 89 01 49 83 40 08 02`, `pefile`). 그 뒤 `+0x6a ~ +0xa3` 이 몸 (항목) 쪽 정렬을 하고 `+0xb7 ~ +0xca` 가 가름값과 맞는 항목을 찾는다.
- 그래서 선 위 모듈 ID: `Length` u16 (+0) · `Type` u16 (+2) · **가름값 u16 (+4)** · 패딩 2 (+6) · **GUID 16 B (+8)** → **24 B**. LUID 면 `+8` 부터 8 B → **16 B**.
- 이 연결 (복잡 구조체가 안의 공용체를 이 함수로 부른다 · NDR 2.0 문법은 DCE 엔진) 은 `[추측]` — run137 4절 (문법 슬롯이 NDR 2.0 이면 DCE 엔진) 과 함수 이름으로 이어 봤을 뿐 호출 줄을 따라가지는 않았다. **진짜 요청 길이는 VM 으로 확인한다** (아래).

### 1-6. 스텁 길이 표 (NDR 2.0, 4 정렬 `[규칙]`) — 칸 · 크기는 1-3 ~ 1-5 에서 나옴 `[정적]`

| 프로시저 · 방향 | 길이 | 칸 차례 (오프셋은 스텁 처음 기준) |
|---|---|---|
| **5 요청** (비교용) | **44 B** (모듈 ID GUID) / 36 B (LUID). run137 3-2 의 40 B 를 고친다 | `+0x00` 예약 u32 · `+0x04` 포인터 ID u32 (0 아님) · `+0x08` 객체 번호 u32 · **`+0x0c` 모듈 ID 24 B** (Length 2 · Type 2 · 가름값 2 · 패딩 2 · GUID 16) · `+0x24` compartment 8 B |
| **6 요청** | **56 B** (GUID) / 48 B (LUID). 52 B 가 아니다 | `+0x00 ~ +0x0b` 5 와 같은 12 B · **`+0x0c` 모듈 ID 24 B** · `+0x24` 컨텍스트 핸들 20 B (속성 u32 + GUID 16) |
| 6 요청, 모듈 ID 포인터 NULL (ID 0) | **32 B** | 12 B (포인터 ID 0) + 컨텍스트 핸들 20 B (`+0x0c` 부터) |
| **6 응답** | **24 B — 맞다** | `+0x00` 핸들 20 B (서버가 지우면 속성 0 + GUID 0) · `+0x14` 반환 u32 (0 = 성공) |
| **7 요청** | **20 B** | `+0x00` 컨텍스트 핸들 20 B 하나 |
| **7 응답, 변화 없이 끝냄 (반환 `0x4c7`, 출력 셋 0, 포인터 NULL)** | **32 B** | `+0x00` A 의 배열 포인터 ID 4 (=0) · `+0x04` A 의 길이 u32 (0) · `+0x08` B 의 enum16 2 (0) · `+0x0a` 패딩 2 · `+0x0c` B 의 배열 포인터 ID 4 (=0) · `+0x10` B LONG (0) · `+0x14` B LONG (0) · `+0x18` 출력 enum16 2 (0) · `+0x1a` 패딩 2 · `+0x1c` **반환 u32 = `0x000004c7`** |
| 7 응답, 배열 포인터가 NULL 이 아닐 때 | 32 B + 구조체 뒤마다 (conf u32 + 바이트, 4 정렬) | 포인터 ID ≠ 0 인 구조체 바로 뒤 (A 는 8 B 구조체 뒤, B 는 16 B 구조체 뒤) 에 배열 본문이 따라온다 — 개수는 A 는 `+0x04` 의 길이, B 는 `[공백]` |

- 정리: 6 요청 = 모듈 ID 포인터 ID ≠ 0 이면 56 (GUID) / 48 (LUID), 0 이면 32. 우리 서버 흉내는 끝 20 B 만 문맥으로 쓰므로 어느 쪽이든 읽힌다.
- "반환 0x4c7 · 출력 NULL" 은 서버 스텁이 `[out]` 참조 구조체를 0 으로 채워 돌려준다고 본 것 `[추측: 스텁이 0 으로 초기화]`. 걸린 7 을 끝내는 서버 줄: `ms/disassemble/nsisvc/RpcNsiDeregisterChangeNotification.asm +0x1fb MOV EDX,0x4c7` → `+0x207` `NsipUnsubscribeAndCompleteAsyncCall` → `+0x6c RpcAsyncCompleteCall(비동기 상태, &0x4c7)` — 둘째 인자 `&[RSP+0x38]` 가 EDX 를 저장한 홈 칸이라 **반환값 자리 = 0x4c7** (오류로 끝내는 fault 가 아니다).

---

## 2. `RpcAsyncCancelCall` 의 보내기만 `0x10000` (덤 2) [정적]

### 2-1. 함수 사슬 (rpcrt4, `ms/disassemble/rpcrt4/`)

| 단계 | 함수 (rva) | 줄 · 하는 일 |
|---|---|---|
| 0 | winnsi `NsiRpcDeregisterChangeNotificationEx` (`+0x302`) | 6 이 오류로 돌아오고 (`+0x1b8 JNZ → +0x2ef` EAX = 1) 걸린 7 이 있으면 (`+0x1a9 CMP [블록+0x80],1`) `+0x2f9 MOV EDX,1` · `+0x2fe MOV RCX,[블록+0x10]` · **`+0x302 RpcAsyncCancelCall(7 의 비동기 상태, 1)`** → 곧 `+0x1db` 가 그 7 의 이벤트를 `WaitForSingleObject(INFINITE)` |
| 1 | `RpcAsyncCancelCall` (`0x61e40`) | `+0x14` 비동기 상태 `[+8]` 검사 · `+0x3e RSI=[상태+0x20]` (부름 객체) · `+0x4f InvalidHandle` 검사 (틀리면 반환 `0x6a6`) · `+0x7f` 가상 `[vtbl+0xa8](부름, EDX=취소를 끝까지 할지)` · `+0x90` 가상 `[vtbl+0x20]` (참조 놓기) |
| 2 | `LRPC_BASE_CCALL::CancelAsyncCall` (`0x61f30`) | `+0x38` 부름 락 `[+0x1d0]` (`RtlEnterCriticalSection`) · `+0x44` 이미 취소 중이면 (`[+0x120]&2` 이고 `[[+0x18]+8] ≥ 3` 또는 `[+0x10c] ≥ 4`) 바로 반환 · **`+0x65` `[+0x120] \|= 4`** · **`+0x6d ~ +0x8a`: 상태 `[+0x108]` 가 9 보다 크고 `0xc` 가 아니면 `AlpcSendCancelMessage([+0x118] 연결, [+0x124] 호출 ID)`** · `+0x9e` 취소를 끝까지 (둘째 인자 ≠ 0) 면 `+0xab CancelCall(부름)` |
| 3 | `LRPC_CASSOCIATION::AlpcSendCancelMessage` (`0x62690`) | 아래 2-2 — **`NtAlpcSendWaitReceivePort` `+0x9a`, Flags `0x10000`** |
| 4 | `LRPC_BASE_CCALL::CancelCall` (`0x62554`) | 부름 락 안에서 `[+0x120] \|= 1` (한 번만, 이미 켜졌으면 건너뜀) · 연결 · 바인딩 참조 · 가상 `[vtbl+0x150]` = `ActualCancelCall(상태, 연결, 바인딩, 흐름)` |
| 5 | `ActualCancelCall` (LRPC_CCALL `0x9b800` → 꼬리 점프 `0x945e8` LRPC_BIND_CCALL) | 상태에 따라 (2-4 표). 상태 9 · 10 이면 **`AlpcCancelMessage` (`0x94724`)** |
| 6 | `LRPC_CASSOCIATION::AlpcCancelMessage` (`0x94724`) | **`NtAlpcCancelMessage(포트, 0, &컨텍스트 사본)`** `+0x7e` — 그 앞에 **2 ms 잠 (2-3)** |

### 2-2. 취소 메시지 칸 표 (`AlpcSendCancelMessage`, 0x40 B)

| 칸 | 값 | 줄 |
|---|---|---|
| 전체 | 0x40 B 를 0 으로 지움 (`memset`) | `+0x1c ~ +0x26` |
| `+0x00` DataLength u16 | **`0x18`** | `+0x4f MOV [RSP+0x40],0x400018` |
| `+0x02` TotalLength u16 | **`0x40`** | 같은 줄 |
| `+0x04` Type u16 | **`0x4000`** (bind 와 같다, 요청은 0) | `+0x3c MOV EAX,0x4000` · `+0x4a` |
| `+0x28` **LRPC 종류** u64 | **4** | `+0x57 MOV [RSP+0x68],4` |
| `+0x30` u32 | 0 (요청의 깃발 · fault 의 오류 칸 자리) | `+0x37` |
| **`+0x34` 호출 ID** u32 | **`[부름+0x124]`** (요청 때 `+0x34` 에 쓴 값과 같은 칸, run133 2-1) | `+0x60 MOV [RSP+0x74],EBX` ← `CancelAsyncCall +0x7d MOV EDX,[RDI+0x124]` |
| `+0x38` 슬롯 u32 | 0 | `+0x41` |
| 나머지 | 0 | — |
| **보내기** | `NtAlpcSendWaitReceivePort(포트 = [연결+0x88], Flags = 0x10000, 메시지 = &위, 보내기 속성 = NULL, 받기 메시지 · 길이 · 받기 속성 · 시간 = 전부 NULL)` | `+0x93` · `+0x88` · `+0x74` · `+0x7f` · `+0x6e ~ +0x8d` · **`+0x9a` (ret `+0xa1`)** |
| 반환값 | **안 본다** (호출 뒤 바로 함수 끝) | `+0xa1 ~ +0xba` |

- `0x10000` 의 공개 이름은 `ALPC_MSGFLG_RELEASE_MESSAGE` 로 기억하지만 `[추측: 이름]` — 코드엔 상수만 있다. 바이트로 확인한 것: `ba 00 00 01 00`.
- 다른 `0x10000`: `DropMsgAttributes +0x98` 의 `0x10000` 은 **Flags 가 아니라 DataView 속성 안 깃발** (보내기 Flags 는 `+0xf8 XOR EDX,EDX` = 0). 이 덤과 상관없다.

### 2-3. 서버가 답하나 · 클라이언트가 2 ms 씩 자며 기다리는 끝 조건

**서버** (`rpcrt4!LRPC_ADDRESS::ProcessIO`, `ProcessIO.asm`):
- ALPC 메시지 종류가 **3 (`LPC_DATAGRAM` `[추측: 이름]`)** 이면 `+0x858 ~ +0x866` 분기로 `+0x9d9` 에 가서 `+0xa0e HandleCancelMessage(연결, [메시지+0x34] 호출 ID)` 만 부른다 (`TotalLength ≥ 0x40` 일 때, `+0x9fa`). **LRPC 종류 `+0x28` 은 이 길에서 안 읽는다.**
- 그 뒤 `+0xa13 ~ +0xa1f`: 메시지 `Type` 의 `0x2000` 비트가 켜졌을 때만 `+0xa44 NtAlpcSendWaitReceivePort` 로 답. 보내기만 한 취소는 안 켜질 것 `[추측]` → **서버는 답하지 않는다.**
- `HandleCancelMessage` (`0x33620`): 호출 ID 가 맞는 부름 (`[부름+0x214]`) 을 찾아 취소 깃발을 켜고 `IssueCallStatusChangeNotification` — **취소 알림을 구독한 부름에만 뭔가 일어난다.** nsisvc 는 구독이 **클라이언트 끊김 (`RpcServerSubscribeForNotification(…, 1, 5, …)`)** 뿐이라 (`ms/disassemble/nsisvc/RpcNsiRequestChangeNotification.asm +0xd6 MOV EDX,1` · `+0xdb LEA R8D,[RDX+4]`) **취소 메시지로 걸린 7 이 끝나지 않는다.** 끝내는 쪽은 클라이언트 (아래 2-4).

**클라이언트 `AlpcCancelMessage` (`0x94724`) 의 2 ms 잠**:
1. `+0x0f ~ +0x24`: 인자 속성 칸 (`호출+0x128`) 첫 dword (할당된 속성) 에서 `& 0xc0000000` (Security · DataView) → `RpcpAlpcpGetHeaderSize` → **Context 속성 위치 = 칸 + 8 + (Security 면 0x18) + (DataView 면 0x20)** (`RpcpAlpcpGetHeaderSize.asm`, 속성 차례 Security → DataView → Context).
2. `+0x27 MOV EDX,[Context+0x14]` (**`MessageId`**) ≠ 0 이면 바로 `+0x32` 로 진행.
3. 0 이면 `+0xdf CMP [Context+0x18],0` (**`CallbackId`**) ≠ 0 이어도 진행.
4. 둘 다 0 이면 **`+0xe9 MOV ECX,2` · `+0xee Sleep(2)`** (= `NtDelayExecution` 2 ms) 하고 `+0xfa JMP +0x27` 로 **다시 읽는다**. **시간 제한 · 횟수 제한 · 다른 탈출구 없음.**
5. 진행하면 컨텍스트 사본 `{PortContext [+0], MessageContext [+8], Sequence [+0x10], MessageId (EDX), CallbackId [+0x18]}` 를 `RSP+0x40 ~` 에 만들어 `NtAlpcCancelMessage(포트, 0, 사본)` (`+0x7e`).
- **이 칸은 어디서 채워지나**: `호출+0x128` 은 비동기 요청을 보낼 때 그대로 `SendMessageAttributes` 로 넘어간 칸이다 — `InitializeRequestAndAttributes +0x1b LEA RSI,[RCX+0x128]` 가 채우고, `AlpcNonPipeSend +0x16 LEA R8,[RCX+0x128]` → `AlpcSend +0x10c MOV R9,R14` → **`AlpcSend +0x128 NtAlpcSendWaitReceivePort`**. 커널이 보내는 syscall 안에서 이 Context 속성의 `MessageId` 등을 쓴다고 보는 것이 이 루프와 맞는다 `[추측]` (커널 쪽 `AlpcpSendMessage` 는 이번에 안 열었다 — 다음 요청 후보).
- PE_emulator 쪽 (`lrpc.cpp` `service_send_async` · `service_read_message_context`) 은 이 칸에서 `MessageContext` (`+8`) 를 **읽기만** 하고 `MessageId` · `Sequence` 는 안 쓴다 (읽기만 한 코드 확인). → 요청 보내기 뒤 취소가 오면 이 루프가 영원히 돈다 = **BG02 의 `NtDelayExecution(2 ms)` 끝없음과 맞는 설명** `[추측: 로그 원본이 없어 줄 단위 대조는 못 함]`.
- 2 ms 잠이 있는 곳 둘 (헷갈리지 않게): **`AlpcCancelMessage +0xee`** (상태 9 · 10 쪽) · `CancelAndWaitToDisappear +0x35` (상태 1 · 3 · 5 쪽, 클라이언트 IO 가 목록에서 빠질 때까지). 이 덤의 길 (요청을 이미 보낸 부름) 은 앞의 것. `ActualCancelCall +0xee` 의 `Sleep(0x14)` = 20 ms 는 `[부름+0x12c]` (속성 Valid 칸) 가 0 일 때 (요청이 아직 안 나감).

### 2-4. 상태 `[부름+0x108]` 에 따른 길 `[정적]` (상태 이름은 안 풀었다 `[공백]`)

| 상태 | 취소 메시지 (종류 4) 보냄? | `ActualCancelCall` 이 하는 일 |
|---|---|---|
| ≤ 8 | 안 보냄 (`CancelAsyncCall +0x73 CMP 9; JLE`) | 1 · 3 → 바인딩의 IO 취소 (`+0x124`), 5 → `Cancel` (CLIENT_IO_PROVIDER), 7 → `CancelPrepareIteration` |
| 9 | 안 보냄 | `+0x4d` 갈래: 속성 Valid ≠ 0 이면 `AlpcCancelMessage` · 0 이면 `Sleep(20)` 돌며 기다림 |
| **10 (0xa)** | **보냄** | 깃발 `[+0x120]&0x80` 이 꺼져 있으면 `+0x4d` 갈래 = **`AlpcCancelMessage` (2 ms 잠 포함)** |
| 11 (0xb) | **보냄** | 아무것도 안 함 (`+0x3f CMP ECX,1; JNZ`) |
| 12 (0xc) | 안 보냄 (`CMP 0xc; JZ`) | 아무것도 안 함 |

- BG02 로그 (취소 메시지가 나갔고 2 ms 잠이 이어졌다) 가 맞으려면 그때 걸린 7 의 상태는 **10** 이어야 한다 (9 는 메시지를 안 보내고, 11 은 안 잔다) `[추측: 로그 대조]`.
- `AlpcCancelMessage` 가 `NtAlpcCancelMessage` 반환을 보는 법 (`+0xa6 ~ +0x11e`): **`0` → 1 (취소됨)** · `0xc0000702` · `0xc0000703` → 3 · `0x4000002e` → 2 · `0x103` → 0 · **그 밖은 `RpcpReportFatalError(2, 상태)` = 치명 (INT3)**. 반환 1 이면 `ActualCancelCall +0x73 ~ +0x99` (연결 `[+0x230]` 가 있고 상태 10 이면 `SignalBrokenFlow` = 종류 5 메시지, 3절) 를 거쳐 `+0xa0` 가상 `[vtbl+0x128](부름, 0x71a)` = **부름을 `0x71a` (`RPC_S_CALL_CANCELLED`) 로 그 자리에서 끝냄** `[추측: 가상 함수 이름]` → winnsi 의 `WaitForSingleObject` 가 풀리고 `RpcAsyncCompleteCall` 이 `0x71a` 를 돌려준다. 반환 2 도 같은 `+0xa0`, 0 · 3 은 끝내지 않는다 (서버가 끝내 주기를 기다림 — nsisvc 는 안 끝낸다).

---

## 3. nsi 포트의 종류 0 · 1 밖 메시지 (덤 3) [정적]

### 3-1. 종류 값 표 — 클라이언트가 포트로 보내는 `NtAlpcSendWaitReceivePort` 15곳 (`_refs/0xe94c0.tsv`)

15곳 중 서버 쪽 (`ProcessIO` 둘 · `QueueOrDispatchCall` · `AlpcSend__56d44` · `AlpcSendReply` · `AlpcSendWaitReceivePort__6def8` · `ValidateCallbackReply`) 을 빼면 클라이언트가 보내는 곳은: `Bind` · `AlpcSend` (비동기 요청) · `DoSendReceive` (동기 요청) · `AlpcSendWaitReceivePort` (`0x3fa50`, `SendReceive` · `IoCompleted` 가 부르는 래퍼) · `IoCompleted` (받기만) · **`AlpcSendCancelMessage`** · **`SendReservedMessage`** · `DropMsgAttributes`.

| 종류 `+0x28` | 보내는 쪽 | 만드는 줄 | Flags | 메시지 크기 · 칸 | 서버 | 클라이언트가 답을 기다리나 |
|---|---|---|---|---|---|---|
| **0 요청** | 클라이언트 (동기 · 비동기) | `InitializeRequestAndAttributes +0x1ab` (run133) | 0 / `0xc00000` | 스텁 + 0x68 | 답함 (종류 3) | 동기는 기다림, 비동기는 완료 통지로 받음 |
| **1 bind** | 클라이언트 | `Bind__4e330 +0x50f` (run133) | 0 | 0x70 | 답함 | 비동기 (완료 통지) |
| **4 취소** | 클라이언트 | **`AlpcSendCancelMessage +0x57`** | **`0x10000`** | **0x40**: `+0x34` 호출 ID, 나머지 0 | **답 안 함** (2-3) | **안 기다림** (반환값도 안 봄). 다만 부른 쪽 (`NsiCompletePendingRpcCall`) 은 그 뒤 걸린 7 의 이벤트를 `INFINITE` 로 기다린다 — 끝내 주는 쪽은 클라이언트 자신 (2-4) |
| **5 예약 메시지 묶음** | 클라이언트 | **`SendReservedMessage +0x2d1`** (`[R14+0x28]` = 5) | **0** (`+0x31c XOR EDX,EDX`), 보내기 속성 = `[연결+0x150] + 0x100` (예약 칸) | 최대 0x100 B: `+0x18` MessageId = `[연결+0x148]`, `Type 0x4000`, `+0x28` = 5, 본문 `+0x30 ~` 에 항목 (항목 하나 0x10 B) 목록 | **답함** (`ProcessIO +0xa88 → HandleReservedMessageRequest`). 클라이언트는 `IoCompleted +0x394 → HandleReservedMessageReply` 로 받는다 | 안 기다림 (비동기). 답이 안 와도 막히지 않는다 `[추측]`: 보낸 뒤 `[연결+0xd0]` 의 `0x40` 비트를 끄고 (`+0x360`) 그 비트가 꺼져 있으면 다음 5 를 안 만든다 (`+0x2b TEST [RCX+0xd0],0x40; JZ`) — 비트를 다시 켜는 곳은 `SetReservedMessageAvailable` (`HandleReservedMessageReply +0x46`) |
| 2 fault · 3 응답 | **서버 → 클라이언트** | run133 2-3 | — | — | — | — |
| 6 · 9 · 10 · 11 | 서버 쪽에서 만든다 (`SendReceive__6d810 +0x17d` = 9, `+0x309` = 6 · `ValidateCallbackReply +0xa3 / +0xb5` = 10 / 11) | 콜백 (서버 → 클라이언트) 관련 | — | — | — | 클라이언트가 먼저 보내는 종류가 아니다 |
| `DropMsgAttributes` | 클라이언트 | `+0xf8 XOR EDX,EDX` (Flags 0), 받았던 메시지 머리를 되돌려 보내고 DataView 속성에 `0x10000` 깃발 | 0 | 받았던 메시지 | — | 안 기다림 |

- **5 가 나가는 조건** (`SendReservedMessage` 가 불리는 곳 4곳, `_refs/0x94d20.tsv`): ① **`SendCausalFlowListForCleanup +0x62` — 쌓인 항목이 12개 이상일 때** (`+0x56 CMP [연결+0x128],0xc; JC` 로 12 미만이면 건너뜀). 이 함수를 부르는 곳 6곳 (`_refs/0x42e14.tsv`): **`CleanupOrphanedCasaulFlowEntries +0xa9`** · `Unbind +0xe5` · `LRPC_BINDING_HANDLE` 쪽 `+0x1d1` · `SetAuthInformation +0x1bf` · `LrpcCreateBindingHandle +0x1f7` · `ResetBindState +0xae`. ② `HandleReservedMessageReply +0x57` (5 에 대한 답이 와서 아직 쌓인 게 있을 때). ③ **`SignalBrokenFlow +0x8f` — 취소가 성공한 뒤** (`ActualCancelCall +0x94`, 2-4). ④ `SignalBrokenPipe +0x5d` (파이프 쪽, 이 길 아님).

### 3-2. "새 스레드가 이미 묶인 포트로 첫 메시지" — 정적 사슬 둘, PE_emulator 코드로 가름

**사슬 A — 종류 5 (새 스레드의 첫 호출 앞, Flags 0)** — 원문 `svc_lrpc_kind_unsupported` (`:280 · :328`) 와 맞는 쪽:

1. winnsi 가 어느 스레드에서든 부름을 낸다 (예: `NsiRpcRegisterChangeNotificationEx +0x1e8` 의 프로시저 5) → `Ndr64AsyncClientCall` → `Ndr64pClientSetupTransferSyntax +0x3e0` → `I_RpcNegotiateTransferSyntax` (`_calls.tsv` 줄, run137 4절) → 바인딩의 가상 `NegotiateTransferSyntax` (`0x43a20`). 마지막 가상 호출이 `I_RpcNegotiateTransferSyntax` 안 어느 칸 (`+0x96` · `+0x150` …) 인지는 안 따라갔다 `[추측]`.
2. `NegotiateTransferSyntax +0x47c ~ +0x49c`: 부름 깃발 `[+0x48] & 0x2000` 이 꺼져 있고 (`+0x47c`) 바인딩의 `[+0x40]` 이 없거나 그 `[+0x48]` 이 0 이면 (`+0x485 ~ +0x493`) **`+0x49c FindOrCreateCausalFlow(바인딩, &흐름)`** (`0x4322c`).
3. `FindOrCreateCausalFlow`: `+0x1f RpcpGetThreadPointer` → 이 스레드의 흐름 목록 `[THREAD+0xa0]` 에서 `[흐름+0x88] == 이 바인딩` 인 것을 찾는다. **있으면 그대로 반환** (`+0x69`). **없으면 (= 이 스레드가 이 바인딩에서 처음)** `+0x8a ~ +0xa3` 새 `LRPC_CAUSAL_FLOW` (0x90 B) 를 만들고 목록에 넣고 → 바인딩 락 `[+0xb8]` 안에서 **`+0xec CleanupOrphanedCasaulFlowEntries(바인딩)`**.
4. `CleanupOrphanedCasaulFlowEntries` (`0x96210`): 바인딩의 흐름 목록 `[+0x1e8]` 을 돌며 항목 상태 `[항목−8] == 1` (스레드가 끝나서 버려진 것 `[추측: 뜻]`) 인 것을 떼어 내고, 연결 협상 수준 `[+0x114] ≥ 5` 이면 `+0xa9 SendCausalFlowListForCleanup([+0x170] 연결, 떼어 낸 목록)` (5 미만이면 그냥 놓는다 `+0xcf`).
5. `SendCausalFlowListForCleanup` (`0x42e14`): 항목마다 `[항목+0x38] & 2` 이면 연결 `[+0x128]` 을 하나 올리고 `[+0x130]` 목록에 붙인다 · **`[+0x128] ≥ 12` 면 `+0x62 SendReservedMessage`**.
6. 그 뒤 같은 스레드의 요청 (종류 0) 이 나간다.

→ 새 스레드의 첫 호출 앞에 **종류 5 가 한 번 끼는 것은 "이 바인딩에서 죽은 스레드의 흐름이 12개 이상 쌓인 뒤 처음 온 새 스레드" 일 때** 이다. 표본이 짧은 수명의 스레드를 여럿 만드는 꼴이면 (예: 이 레포 `sample/` 의 `CreateThread` · 일꾼 풀) 맞을 수 있다 — 표본을 안 봐서 여기까지.

**사슬 B — 종류 4 (취소, Flags `0x10000`)** — winnsi 의 WNF 재등록 길 (원문 "곧 프로시저 5 로 이어 감" 과 코드 순서는 같다):

1. `RegisterNsiSvcRestartNotification +0xd2 RtlSubscribeWnfStateChangeNotification` → `NsiServiceStatusChangeCallback +0x12 SetEvent` (`NsiRecoveryObj`) → 그 이벤트를 `+0x5e RegisterWaitForSingleObjectEx(…, 콜백 = +0x54 LEA RDX,[0x180003610] = NsiReregisterNotificationWorker, 시간 −1, 깃발 0x10)` 로 기다리던 풀 일꾼이 **`NsiReregisterNotificationWorker +0x30`** 을 부른다.
2. `NsiReregisterChangeNotification +0x4d NsiStopWorkerThread` → **`+0x58 NsiCompletePendingRpcCall(블록, 1)`** → `+0x13` 걸린 7 이 있으면 (`[블록+0x80] == 1`) `+0xbc RpcAsyncCancelCall([블록+0x10], 1)` → 2절 사슬 (종류 4 + `NtAlpcCancelMessage`) → `+0x42 WaitForSingleObject(INFINITE)` → `+0xe8` 프로시저 5.
3. 같은 취소가 `NsiRpcRegisterChangeNotificationEx +0x8ac` · `NsiRpcDeregisterChangeNotificationEx +0x302` 의 오류 길에서도 나간다.

**PE_emulator 코드와 가르기** (`lrpc.cpp` 읽기만): 종류 4 의 Flags `0x10000` 은 `lrpc_shape_error` (`:137 ~ :139`) 에서 `a[1] ≠ 0` 이고 `0x20000` (`synchronous`, `:7`) 비트가 없어 **`svc_async_unsupported`** 가 된다 — BG02 에서 본 이유와 같다. 원문 BG03 표본이 `svc_lrpc_kind_unsupported` 라면 형태 검사를 통과한 메시지다: **Flags 0 · 보내기만 → `:271 ~ :273` → `service_send_async` → `:328`** 이면 사슬 A 의 종류 5, (Flags 에 `0x20000` 이 있고 받기 칸이 있는 동기 꼴이면 `:280`). 그래서 **원문 두 표본은 종류 5 (사슬 A) 쪽이 맞다**, 사슬 B (종류 4) 는 BG02 때의 것.

- 같은 풀 일꾼 길이어도 **`NsiWorkerThread` (알림이 와서 7 이 끝났을 때 도는 일꾼)** 는 `+0xfe` 에서 **프로시저 7 요청 (종류 0)** 을 첫 메시지로 낸다 — 종류 ≠ 0 이 아니다.
- **한계**: 원문 로그 (`038cc0421ef5 seq 90468` · `a5f097f9701d seq 253599`) 가 지워졌다. 위 가르기는 코드 + PE_emulator 의 이유 문자열로 한 것이다. 확정하려면 그 메시지의 **Flags · `+0x28` · `+0x18` MessageId** (5 는 `[연결+0x148]` 의 예약 메시지 ID) 를 본다.

---

## PE_emulator 쪽 — 지금 맞나 · 틀리나 (`../PE_emulator` 읽기만)

| 자리 | 지금 | 판정 |
|---|---|---|
| **덤 1** `nsi.cpp:98` `TODO(VM)` (6 · 7 형식) | 6 응답 = 끝 20 B 문맥 + 24 B 0 (`register_reply_size`), 7 완료 = fault | **6 응답 24 B 는 맞음.** `deregister_min_size = 12 + 20 = 32` (`nsi.cpp:14`, 검사 `:77`) 는 모듈 ID 본문을 안 센 값 — 진짜 6 요청은 **56 B (GUID)** / 48 B (LUID) 이고 (포인터 NULL 이면 32), 문맥은 끝 20 B 라 `nsi_context_key(stub, size − 20)` (`:79`) 은 그대로 맞다. 5 의 `size() < 40` (`:36`) 도 진짜 44 B 로 통과하니 영향 없음 (하한으로는 36 이 맞다). **7 완료는 fault 가 아니라 일반 응답 (종류 3), 스텁 32 B, 끝 4 B = `0x000004c7`** (1-6) 으로 바꾸는 게 진짜와 같다 — 지금은 클라이언트가 `RpcAsyncCompleteCall` 에서 RPC 오류 `0x4c7` 을 받고, 진짜는 오류 0 + 반환값 `0x4c7`. winnsi `NsiRpcDeregisterChangeNotificationEx +0x1f3` 는 결과를 안 봐서 6 길에선 결과가 같다. `TODO(VM)` 에 쓸 글: "`[정적]` sast/154: 6 요청 56 B (모듈 ID 24 B = Length · Type · 가름값 · 패딩 · GUID — 공용체 가름값이 한 번 더 나감), 응답 24 B; 7 요청 20 B, 완료 응답 32 B (반환 0x4c7). 진짜 길이는 VM." |
| **덤 2** `lrpc.cpp` 보내기만 길 | `service_send_wait_receive` 는 먼저 `lrpc_shape_error` (`:137 ~ :139`) 를 부른다. 종류 4 의 Flags `0x10000` 은 `0x20000` (`synchronous`) 비트가 없어 **`:139` `svc_async_unsupported`** — BG02 때 본 이유가 바로 이것. (그래서 `:271` 의 `a[1]==0` 갈래에도, `:280` 에도 못 온다) | **틀림 (빠짐).** 맞는 동작: ① Flags `0x10000` · 종류 4 · 속성 NULL · 받기 NULL 이면 **상태 0, 답 없음, 큐에 아무것도 안 넣음** (진짜 서버도 답 안 함). 호출 ID `+0x34` 로 걸린 호출 (`held_calls`) 에 취소를 알려 줄지는 우리 고름 — nsisvc 는 안 끝낸다 (2-3). ② **요청을 비동기로 받을 때 보낸 쪽의 Context 속성 `MessageId` (`+0x14`) · `Sequence` (`+0x10`) 를 0 아닌 값으로 써 줘야** `AlpcCancelMessage` 의 2 ms 잠이 안 돈다 (현재 `service_read_message_context` 는 읽기만). ③ 그 뒤 `NtAlpcCancelMessage(포트, 0, {…, MessageId, CallbackId})` 가 오는데 rpcrt4 가 받아들이는 반환은 `0` · `0xc0000702` · `0xc0000703` · `0x4000002e` · `0x103` 뿐 — **`nt_alpc_cancel_message` 가 message_id 0 에 돌려주는 `0xc0000109` 는 치명 (INT3)** (2-4). ④ 반환 `0` 이면 rpcrt4 가 부름을 `0x71a` 로 직접 끝내므로 서버 쪽에서 할 일 없음. ⑤ 보너스: Flags ≠ 0 인 비동기 요청 (`AlpcSend +0x92 ~ +0xa8` 이 `[부름+0x244] & 0x800000` 이면 `0xc00000`) 도 `a[1]==0` 검사 때문에 동기 갈래로 가는지 점검 거리 |
| **덤 3** `lrpc.cpp:280 · :328` (종류 ≠ 0 · 1) | `:279 ~ :280` = Flags ≠ 0 이고 형태 검사를 통과한 (동기 꼴) 갈래, `:327 ~ :328` = Flags 0 보내기만 갈래 (`service_read_async_message`) 의 "모르는 종류" | **맞는 자리, 해석을 고친다** — 원문의 `:280 · :328` 에 걸린 "종류 ≠ 0 · 1" 은 **종류 5 (Flags 0, 보내기만) → `:328`** 이다. 종류 4 는 여기까지 못 오고 `:139` 에서 걸린다 (덤 2). **5 는 지금처럼 거절해도 클라이언트가 막히지 않는다** `[추측: 3-1 의 `0x40` 비트 논리]` — 급하지 않다. 5 를 받아 줄 거면 답 (ALPC 종류 2, 본문 `[공백]`) 을 큐에 넣고 완료 패킷 하나. 우리가 로그에 먼저 남길 것: **Flags (`a[1]`) · `+0x28` 값 · `+0x18` MessageId** — 이 셋이면 3-2 의 가르기가 끝난다 (5 는 호출 ID `+0x34` 가 없다). 5 를 받아 주고 싶으면 답 (ALPC 종류 2) 을 큐에 넣고 완료 패킷 하나 — 본문 칸은 `[공백]` |

---

## 동적분석으로 확인해야 할 것

### VM 으로 확인할 것 — 디버거 필요 (Q280 · Q271 RPC 묶음에 얹을 꼴)

1. **선 위 요청 길이 (덤 1)**: winnsi 프로시저 5 · 6 의 스텁이 44 / 56 B 인가 (run137 3-2 의 40, 요청서의 52 와 갈림). 멈춤점: `rpcrt4!LRPC_BASE_CCALL::AlpcSend +0x12f` 의 call 직전 (`+0x128`, `NtAlpcSendWaitReceivePort` 입구), `db @r8 L0x80` — `+0x00` DataLength = 스텁 + 0x40 (5: `0x6c` 이면 44 B, `0x68` 이면 40 B), `+0x28` 종류, `+0x3c` 프로시저 번호 5 / 6, 스텁 = `+0x68` 부터 (모듈 ID `Type` · 가름값 · 패딩 위치도 같이 본다). 조건: `poi(@r8+0x3c) == 6`.
2. **7 완료 응답 (덤 1)**: 서버가 걸린 7 을 끝낼 때 진짜 스텁 32 B · 끝 4 B `c7 04 00 00` 인가. 멈춤점: `rpcrt4!IoCompleted +0xcf` (run133 동적 5 와 같은 자리) 에서 `+0x40` 부터 본다. nsisvc 가 도는 VM + 6 을 부르는 프로그램 필요.
3. **취소 (덤 2)**: ① `rpcrt4!LRPC_CASSOCIATION::AlpcSendCancelMessage +0x9a` 입구에서 `db @r8 L0x40` (`+0x28` = 4, `+0x34` = 호출 ID), `edx` = `0x10000`. ② `rpcrt4!LRPC_BASE_CCALL::CancelAsyncCall +0x6d` 에서 `dd @rdi+0x108 L1` = **상태 값** (10 인가). ③ `AlpcCancelMessage +0x27` 에서 `dd @rbx+0x14 L2` = 진짜 `MessageId` · `CallbackId` (0 아님이어야 안 잔다) — 요청 보낸 뒤 `AlpcSend +0x12f` 바로 뒤에 `호출+0x128` 속성 칸 (`dq @rdi+0x128` 쪽) 을 한 번 찍어 커널이 쓰는 값 (`MessageId` · `Sequence`) 도 본다. ④ `NtAlpcCancelMessage` 반환 (`AlpcCancelMessage +0x87` 뒤 `eax`).
4. **새 스레드 첫 메시지 (덤 3)**: `NtAlpcSendWaitReceivePort` 입구에서 Flags (`edx`) · `poi(@r8+0x28)` · 스레드 ID 를 남긴다 — Flags 0 에 `+0x28` = 5 이면 사슬 A. 더 좁게: `rpcrt4!LRPC_CASSOCIATION::SendReservedMessage` 입구 (`0x94d20`) 에서 `dd @rcx+0x128 L1` (쌓인 항목 수, 12 이상이어야 함) 과 반환 주소 (`CleanupOrphanedCasaulFlowEntries +0xae` 이면 새 스레드 길). 사슬 B 는 nsisvc 를 재시작해 WNF 상태를 바꿔야 타서 선택.

### VM 으로 확인할 것 — 디버거 없이

없음 (VM 쪽). PE_emulator 쪽 로그에 Flags · 종류 · 호출 ID 를 남기는 한 줄만 (위 표 덤 3).

## 다음 요청 후보

1. (정적, ntoskrnl — 다른 decompiler 가 도는 프로젝트라 이번엔 안 열었다) **`NtAlpcSendWaitReceivePort` 가 보내는 쪽 Context 속성에 `MessageId` · `Sequence` 를 쓰는 줄** (`AlpcpSendMessage` · 속성 되돌려 쓰기) 와 `NtAlpcCancelMessage` 의 반환 표 — 2-3 의 `[추측]` 을 닫는다. (Q280 의 커널 쪽 몫.)
2. (정적, rpcrt4) **호출 상태 값 이름 · 전이** (9 · 10 · 11 · 12) — 비동기 호출이 요청을 보낸 뒤 몇 번 상태인지 (`DriveStateForward` · 상태 바꾸는 도우미) 를 줄로 닫으면 2-4 의 "10 이어야 한다" 가 `[정적]` 이 된다.
3. (정적, rpcrt4) **NDR 경로 확인**: 복잡 구조체 (`NdrComplexStructMarshall` / `NdrpComplexStructMarshall`) 가 안의 비캡슐 공용체를 `NdrNonEncapsulatedUnionMarshall` 로 넘기는 호출 줄 — 1-5 의 `[추측]` 을 닫아 모듈 ID 24 B 를 `[정적]` 으로 올린다. 동적 1번이 더 빠르면 그쪽으로.
4. (정적, rpcrt4, 급하지 않음) **종류 5 의 본문 항목 칸과 서버 답 (`HandleReservedMessageRequest` `0x95590` · `ParseReservedMessage`)** — 5 를 받아 줄 때만.
5. (정적, winnsi) 7 출력 구조체 B 의 배열 개수 칸 (`19 00 1c 00`, `[공백]`) · 5 의 NDR64 판 — 선 위 NDR 2.0 만 쓰는 동안은 미룬다.

## 판정

**분석성공 (덤 1 · 2) / 부분성공 (덤 3).** 덤 1 은 6 · 7 표와 길이를 냈고 run137 3-2 의 모듈 ID 길이 (40 B → 44 B, 52 B → 56 B) 를 고쳤다 — 단 "공용체 가름값이 선 위에 한 번 더 나간다" 는 함수 한 곳 (`NdrNonEncapsulatedUnionMarshall` + `NdrpSimpleTypeEnum16Marshall`) 의 코드 읽기이고 호출 줄 사슬은 `[추측]` 이라 진짜 길이는 VM 몫. 덤 2 는 메시지 칸 · Flags `0x10000` 줄 · 서버가 답 안 함 · 2 ms 잠의 끝 조건까지 줄로 닫았다 (커널이 `MessageId` 를 쓰는 줄만 `[추측]`). 덤 3 은 "클라이언트가 보낼 수 있는 종류 ≠ 0 · 1 = 4 · 5" 를 닫았고, PE_emulator 의 이유 문자열 (`svc_async_unsupported` ↔ `svc_lrpc_kind_unsupported`) 과 맞춰 **원문 표본은 종류 5 (새 스레드의 첫 호출 앞 인과 흐름 정리)** 로 좁혔다. "새 스레드" 가 어느 스레드인지 · 12개가 실제로 쌓였는지는 로그가 없어 못 가렸다. 이름 못 찾은 함수 없음. 오래 도는 작업 없음.

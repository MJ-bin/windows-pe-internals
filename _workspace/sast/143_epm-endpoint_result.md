# 143 epm-endpoint — 정적 분석 결과

- 요청서: `_workspace/sast/143_epm-endpoint.md` (Q279). 원문 `../PE_emulator/_workspace/05_tuning_planner/365_internals-epm-endpoint.md` 의 **물음 1 만** (사용자 결정 2026-11-26). 물음 2 · 3 (VM) 과 nsisvc 프로시저 6 · webio `Startup` 은 이번 범위 아님 (Q280).
- 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`.
- 날짜: 2026-11-26. 새로 analyze 한 것은 **RpcEpMap.dll 하나** (9초, 함수 440개; 직전 `free -g` = 전체 46 / available 27, 그때 도는 Ghidra java 없음). rpcrt4 는 다시 analyze 하지 않고 `^이름$` export 만 했다 (depth 0 · 1). sast/142 의 user32 · win32kfull · win32kbase 프로젝트는 건드리지 않았다.
- **폴더 이름 (결과서에 적으라는 것)**: 스크립트가 모듈 이름을 소문자로 만들어서 **`ms/decomp/rpcepmap/` · `ms/disassemble/rpcepmap/`** (바이너리 파일 이름은 `RpcEpMap.dll`). rpcrt4 는 `ms/decomp/rpcrt4/` · `ms/disassemble/rpcrt4/`.
- 근거 표기: `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` (부르기는 call 자리, 괄호가 돌아올 자리 `ret`). 코드를 길게 붙이지 않고 우리 말로 요약했다.

## 스크립트 문제

없음. 참고 (고칠 것은 아님, 읽을 때 조심할 것):
- **Ghidra 의 `.c` 가 틀어진 곳**: `ept_map_auth_sd.c` · `ept_insert_ex.c` 의 `TowerExplode` 는 인자가 4 · 3개로 줄어 보이지만 실제 6개 (`TowerExplode(탑, &인터페이스, &전송문법, &protseq, &끝점, &네트워크주소)`). 인자는 `.asm` 의 레지스터로 다시 셌다 (`ept_map_auth_sd.asm +0xe9 ~ +0x102`, `ept_insert_ex.asm +0x84 ~ +0x9d`). `GetEntriesNoLock.c` 는 반환값이 `void` 로 나오지만 `.asm` 은 `EAX` 로 상태를 돌려준다 (`+0x4c6 MOV EAX,EDX`).
- **이름**: 요청서의 `RemoteEpmpSecurityCallback` 은 PDB 이름이 **`RemoteEpmpServerSecurityCallback`** (rva 0x5a30). 나머지 이름 (`ept_map` · `ept_map_auth_async` · `ept_insert_ex` · `WildCardMatch` …) 은 PDB 이름 그대로 `_index.tsv` 에 있었다.
- `refs` 는 PDB 이름만 받는다 (`RemoteEpmpSecurityCallback` 은 "Ghidra 심볼이 아님" 으로 실패, 이름을 고쳐 `export` 로 뽑음).

## PDB GUID 확인

| 모듈 | 바이너리 | PDB guid+age | 로그 |
|---|---|---|---|
| **RpcEpMap** (새로) | `RpcEpMap.dll` 147,456 B | `928AE9EA93768986C32C5500323376111` — `llvm-pdbutil dump --summary` 의 GUID `{928AE9EA-9376-8986-C32C-550032337611}` 및 바이너리 디버그 디렉터리 (`0x928ae9ea`, Age 1) 와 같음. PDB 안 Age 3, 심볼 서버 키는 바이너리의 Age 1 (sast/126 ws2_32 · sast/130 nsisvc 와 같은 모양) | `ms/ghidra/rpcepmap.analyze.log` 208 ~ 209줄 `SetPdb:` 있음, 함수 이름이 PDB 이름으로 잡힘 (`ept_map_auth_sd`, `GetEntriesNoLock` …), `_index.tsv` 440줄 |
| rpcrt4 (기존 프로젝트) | `rpcrt4.dll` | `0204A8A137C42B7DF1F0811A3A8277B21` — `ms/sym/rpcrt4.dll.pdb.info.txt` 가 sast/130 때와 같고, `fetch_pdb.py` 가 "이미 있음" 으로 답함 (다시 안 받음). 함수 이름 PDB 이름 그대로 (`RegisterEntries`, `BindingAndIfToTower`) | `ms/ghidra/rpcrt4.analyze.log` (그대로) |

- 커널 GUID (`C29EBFB0…`) 는 ntoskrnl 만 해당이라 이번엔 해당 없음.
- 공개 PDB 라 구조체 타입 이름이 없다. 아래 칸 이름 (`IFOBJ` 노드 · `PSEP` 노드 등) 은 **줄에서 읽은 우리 이름** (`+0x08` 의 매직 값이 글자로 `BOFI` · `PESP` 로 읽혀서 붙였다 — 코드에 이름이 있는 것은 아님).

## 버전·스크립트 대조

"알려진 사실" 표에 rpcrt4 · RpcEpMap 줄이 없다. 대신 이 문서가 인용하는 `.asm` 의 `+0x` 를 **원본 바이트** (`pefile`) 와 맞췄다 — 전부 일치:
- `ept_map_auth_sd +0x102` = `48 ff 15 …` (`CALL [TowerExplode]`, ret `+0x109`), `+0x2c9` = `3d d6 a0 c9 16` (`CMP EAX,0x16c9a0d6`), `+0x38d` = `c7 03 d6 a0 c9 16`, `+0x45c` = `c7 03 d8 06 00 00`, `+0x268` = `48 8d 15 …` (`LEA RDX,WildCardMatch`), `+0x2c2` = `e8 …` (`CALL GetEntries`).
- `ept_map_auth +0x1ee` · `ept_map +0x4c` · `ept_map_auth_async +0x4e` = `e8 …` (`CALL`).
- `GetEntriesNoLock +0xa36` = `48 c7 00 ff ff ff ff` (`MOV [RAX],-1`), `+0x867` = `ba d6 a0 c9 16`, `+0xa2c` = `b8 d6 a0 c9 16`, `+0x635` = `48 83 f9 ff` (`CMP RCX,-1`), `+0x63f` = `41 81 7b 08 fa fa fa fa`, `+0x46d` = `48 89 83 98 00 00 00`, `+0x2dd` = `48 ff 15 …` (`_stricmp`).
- `WildCardMatch +0x2f` = `a9 00 00 ff ff` (`TEST EAX,0xffff0000`), `+0x36` = `45 3b c1` (`CMP R8D,R9D`).
- `ept_insert_ex +0x21b` = `c7 40 08 50 45 53 50`, `+0x3e3` = `c7 43 08 42 4f 46 49`, `+0x9d` = `48 ff 15 …`, `+0x279` = `48 89 3b` (`MOV [RBX],RDI`).
- `StartEndpointMapper +0x182` · `+0x1c6` = `48 ff 15 …` (`RpcServerRegisterIf3` 둘), `LocalEpmpSecurityCallback +0x25` = `83 e8 04`.
- rpcrt4: `RegisterEntries +0xeb` = `49 6b cf 58` (`IMUL RCX,R15,0x58`), `+0x296` = `e8 …` (`NdrClientCall3`), `+0x332` = `e8 …` (`BindingAndIfToTower`), `+0x7e` = `e8 …` (`BindToEpMapper`); `RpcEpRegisterW +0xf` = `e8 …`; `BindingAndIfToTower +0xb8` · `+0x20e` = `e8 …`; `RegisterRpcEndpointsA +0x54` = `48 83 f8 40`, `+0xe0` = `e8 …`; `RpcServerInqBindings +0x26` = `e8 …`.
- 서버 · 클라이언트 인터페이스 표는 `pefile` 로 직접 읽었다 (아래 1-1). **판정: 일치.** 스크립트 문제 없음.

---

## 먼저 읽는 한 줄 답

**큰 그림**: 진짜 epmapper 는 **등록 때 받은 탑 바이트를 그대로 힙에 복사해 두었다가, 질문이 오면 그대로 꺼내 준다.** 질문 탑에서 쓰는 것은 **1층 (인터페이스 UUID · 버전) 과 3층 (`0x0c` → "ncalrpc") 뿐**이고 2층 (전송 문법) · 4층 (끝점) 은 안 본다. nsi 는 nsisvc 가 **서비스 시작 때** (`NsiStartRpcServer +0x337`) 한 번 등록하고, 이 서비스는 부팅 때 자동으로 도니 test.exe 보다 먼저 장부에 들어가 있다 `[추측: 자동 시작 — 서비스 설정은 이번에 안 읽음]`.

1. **고르기**: 프로시저 **3 · 7 · 8 은 몸이 같다** — 셋 다 `ept_map_auth` → `ept_map_auth_sd` → `GetEntries` → `GetEntriesNoLock` (3 은 SID 칸을 NULL 로 넣어 부르고, 8 은 얇은 감싸개). 장부 항목은 `WildCardMatch` 가 고른다: **인터페이스 UUID 같음 + 주 버전 같음 + 장부 부 버전 ≥ 질문 부 버전 + (장부 객체 UUID 가 nil 이면 무엇이든 맞음 / 아니면 요청 객체 UUID 와 같음)**, 그리고 그 항목의 탑 목록 가운데 **protseq 문자열이 질문 3층에서 나온 "ncalrpc" 와 (대소문자 무시) 같은 것**. **전송 문법과 4층 끝점은 견주지 않는다.**
2. **돌려주는 탑**: 등록 때 탑의 **복사본 그대로** (1 · 2층 바이트도 등록한 쪽 인터페이스에서 온 것 — 질문에서 안 가져옴). 맞는 탑을 전부 **`max_towers` 개까지** 채운다. 차례 = 장부 목록 차례 → 항목 안은 **등록한 차례의 거꾸로**. **끝점 핸들이 0 아닌 때**: (a) 맞는 탑이 `max_towers` 보다 많아 이어받기 블록을 만들 때, (b) **`max_towers ≤ 1` 이면 늘 `-1`** (맞는 게 있든 없든). `max_towers = 4` 이고 맞는 탑이 4개 이하면 **0 (NULL)**. 배열 머리 = `max_count = max_towers` · `offset = 0` · `actual_count = 맞은 수` (NDR 규칙 `[규칙]` — 서버 코드엔 쓰는 줄 없음).
3. **맞는 항목 없음**: 상태 **`0x16c9a0d6`** (rpcrt4 가 `0x6d9` = `EPT_S_NOT_REGISTERED` 로 바꾸는 코드 — `FatalLookupErrorEncountered.asm +0x32`), 탑 수 0, 핸들 0 (`max_towers ≥ 2`) / `-1` (`≤ 1`), 배열 `max_towers · 0 · 0` → **40 B. 우리 지금 답과 같다 (`max_towers = 4`).** 단 하나 다른 길: 그 인터페이스에 **"서비스 트리거" 레지스트리 키가 있으면** 바로 답하지 않고 요청을 **보류**한다 (3-2).
4. **부른 쪽 검사**: 프로시저 안에는 **호출자 프로세스 · PID · 이미지 확인이 없다.** SID 인자 (5번째) 는 **NULL 이면 검사 전부 건너뜀** (클라이언트는 NULL 을 줌, run133 3-1). SID 가 있을 때만 SD 를 만들어 장부 쪽 등록자 토큰으로 `AccessCheck`. 인터페이스 보안 콜백은 공개판은 **조건부로만** 붙고 (원격 제한이 켜졌을 때), 그 콜백이 ncalrpc 로컬 호출자를 막는지는 `I_RpcServerCheckClientRestriction` 의 갈래를 못 닫았다 `[공백]` (진짜 Windows 에서 로컬 질문이 통과한다는 사실은 있음).
5. **등록 쪽**: `RpcEpRegisterW` → `RegisterRpcEndpointsW/A` → `RegisterEntries` 가 **바인딩 벡터의 바인딩마다 탑 하나** (`BindingAndIfToTower`) 를 만들어 epmapper 의 **다른 인터페이스 (`0b0a6584-9e0f-11cf-a3cf-00805f68cb1b` v1.1) 의 프로시저 2 = `ept_insert_ex`** 로 한 번에 보낸다 (객체 UUID 하나당 한 번; NULL 이면 nil 하나). **4층 이름 = 바인딩의 동적 끝점 = `ServerSetupAddress` 가 만든 `LRPC-`+18자** `[정적 + 마지막 연결 추정, 아래 5-2]`. 벡터는 `RpcServerInqBindings` 가 주는 **그 프로세스의 모든 주소** (다른 protseq · 다른 끝점 포함) 이고 전부 장부에 들어간다. 주석 글 (`"NSI server endpoint"`) 은 항목마다 64 B 칸에 복사되고 **`ept_map` 응답에는 안 실린다** (`ept_lookup` 용).

---

## 표 1. "맞는 항목 있음 / 없음" 응답 칸 표 (프로시저 8, NDR 2.0, `max_towers = 4`, 탑 수 1 = nsi 하나)

run137 3-1 의 응답 칸 차례 그대로. "쓰는 줄" 은 **서버 코드가 그 값을 정하는 줄** (NDR 틀이 정하는 칸은 `[규칙]`).

| 칸 (스텁 오프셋) | 있음 값 | 없음 값 | 쓰는 줄 |
|---|---|---|---|
| 끝점 핸들 20 B (`+0x00`) | **20 B 0** (`max_towers ≥ 2` 이고 맞는 탑이 `max_towers` 개 이하) | **20 B 0** (`max_towers ≥ 2`) | `GetEntriesNoLock.asm +0x4a5 ~ +0x4c3` (`CMP EAX,1` · `MOV [RAX],RDI(0)`). `max_towers ≤ 1` 이면 `+0xa36 MOV qword [RAX],-1`; 이어받기 블록이 살아 있으면 그 포인터 (`+0x875 GetNewContext`) |
| 탑 수 `num_towers` (`+0x14`) | **n** (맞는 탑 수, ≤ `max_towers`) | **0** | `GetEntriesNoLock +0x8e MOV [R9],EDI` (시작 때 0), `+0x478 INC [R9]` (탑 하나 넣을 때마다) |
| 배열 머리 12 B (`+0x18`) | `max_count = max_towers (4)` · `offset = 0` · `actual_count = n` | `4 · 0 · 0` | `[규칙]` run137 3-1 (conformance = 스택 인자 `max_towers`, variance = `*num_towers`). 서버 코드는 이 값을 따로 안 씀 — 위 탑 수가 곧 `actual_count` |
| 원소 포인터 ID (`+0x24`, 탑마다 4 B) | 0 아닌 값 (n개) | 없음 (0 개) | `[규칙]` NDR 이 붙임 (원소가 `FC_UP`, 탑 포인터가 NULL 이 아님 — `GetEntriesNoLock +0x449 MOV [RDX],RAX`) |
| 탑 본문 (`+0x28 ~`) | 탑마다 `conformance u32 = tower_length` · `tower_length u32` · 탑 바이트 (**등록 탑 그대로**, 표 3). nsi 하나 (끝점 `LRPC-`+18자) = 4 + 4 + 88 = **96 B** | 없음 | 복사본을 만드는 줄: `GetEntriesNoLock +0x3e2 MOV RAX,[RDI+0x28]` (장부의 탑) → `+0x40f HeapAlloc` → `+0x435 memcpy`. 꼴은 `NewTower` (`NewTower.asm`) 와 같은 `tower_length + 4` 바이트 복사 |
| 패딩 | 탑이 4 의 배수가 아니면 0 으로 (88 은 배수라 **0 B**) | 없음 | `[규칙]` |
| 상태 (`+0x88`) | **0** | **`0x16c9a0d6`** | 있음: `GetEntriesNoLock +0x4c6 MOV EAX,EDX` (EDX = 0) → `ept_map_auth_sd +0x2c7 MOV [RBX],EAX`. 없음: `GetEntriesNoLock +0x867 MOV EDX,0x16c9a0d6` (돌려줌) · `ept_map_auth_sd +0x38d MOV [RBX],0x16c9a0d6` (트리거 없을 때 다시 씀) |
| **스텁 전체 길이** | 20 + 4 + 12 + 4 + 96 + 0 + 4 = **140 B (0x8c)** (응답 머리 `TotalLength = 0x40 + 140 = 0xcc`, sast/130 2-3) | 20 + 4 + 12 + 4 = **40 B** | 위 칸들의 합 |

- **끝점 핸들 값이 0 이 아닌 때 (정리)**: ① 맞는 탑이 `max_towers` 개를 넘으면 넘친 것을 이어받기 블록 (`GetNewContext`, 0x20 B, 매직 `0xfafafafa`) 에 쌓고 그 포인터를 돌려줌 (`GetEntriesNoLock +0x39d JNC +0x657`, `+0x875`), ② 이어받는 호출에서 다 비우면 블록을 풀고 핸들 = `(max_towers > 1) ? 0 : -1` (`+0x833 ~ +0x846`), ③ **`max_towers ≤ 1` 이면 이어받기 블록이 없어도 핸들 = `-1`** (`+0x4b6 CMP EAX,1` · `+0xa36`), ④ 들어온 핸들이 `-1` 이면 핸들을 0 으로 바꾸고 **상태 `0x16c9a0d6` · 탑 0개** 로 끝낸다 (`+0x635 CMP RCX,-1` → `+0xa29 ~ +0xa31`). `-1` 이 선 위에서 어떤 20 바이트가 되는지는 rpcrt4 서버 쪽 컨텍스트 핸들 마샬링을 안 읽어서 `[공백]` — 우리 클라이언트는 `max_towers = 4` 라 이 길을 안 탄다.
- 클라이언트 쪽 (run133 3-2): 핸들이 0 이면 "더 없음", 0 아니면 다음 조각을 또 묻는다. 우리 서 바퀴는 `max_towers = 4` 에서 탑을 **4개 이하**로 주면 핸들 0 으로 끝난다.

## 표 2. 견주기 표 (`WildCardMatch` · `GetEntriesNoLock` · rpcrt4 `TowerExplode`)

질문에서 값을 꺼내는 곳: `ept_map_auth_sd +0x102` (`TowerExplode(질문 탑, &인터페이스 20 B, &전송 문법 20 B, &protseq, NULL, NULL)`; 끝점 · 네트워크 주소 인자 둘은 `+0xe9 MOV [RSP+0x28],RCX` · `+0xf2 MOV [RSP+0x20],RCX` 로 **NULL**). 견주는 곳: `GetEntriesNoLock +0x192` 가 `WildCardMatch(장부 노드, 객체 UUID, 인터페이스, 버전)` 를 부름 (반환 **0 = 맞음**).

| 견주는 것 | 규칙 | 줄 |
|---|---|---|
| 인터페이스 UUID (질문 1층) | **같아야** (16 B 전부) | `WildCardMatch.asm +0x4 ~ +0x15` (노드 `+0x48 · +0x50` vs 질문), 틀리면 `+0x1a MOV EAX,1` (안 맞음) |
| 주 버전 | **같아야** (버전을 `주 << 16 \| 부` 한 칸으로 합쳐 상위 16비트 비교) | `WildCardMatch +0x25 ~ +0x34` (`TEST EAX,0xffff0000`) · 합치는 곳 `ept_map_auth_sd +0x11c ~ +0x129` |
| **부 버전** | **장부 부 ≥ 질문 부** (같은 주 버전일 때 합친 값의 크기 비교 — 장부 값이 더 작으면 안 맞음) | `WildCardMatch +0x36 CMP R8D,R9D` · `+0x39 JC` |
| 객체 UUID | 장부 노드가 **nil 이면 요청이 무엇이든 맞음**. 노드가 nil 이 아니면 요청 객체 UUID 와 **같아야** (요청이 nil 이면 nil 노드만 맞음). 요청 객체가 NULL 포인터면 nil 로 대체 | `WildCardMatch +0x3b ~ +0x59` (같으면 맞음), `+0x6b CALL UuidIsNil` · `+0x79 SETZ BL` (노드가 nil 이면 0 = 맞음); 대체: `ept_map_auth_sd +0xcb ~ +0xd2` (`LocalNullUuid`) |
| **전송 문법 (질문 2층)** | **안 봄** — `TowerExplode` 가 `[RBP-0x10]` 에 풀어 주지만 그 칸을 다시 읽는 줄이 없고 (`ept_map_auth_sd.asm` 에서 `[RBP-0x10]` 은 `+0x99` 초기화 · `+0xf7` 넘김 둘뿐), `GetEntries` 호출 인자에도 안 들어감 | `ept_map_auth_sd +0xf7`, `+0x2c2` |
| **3층 프로토콜** (`0x0c`) | 질문 탑의 3층 프로토콜 ID 가 **`0x0c` 이면 protseq = "ncalrpc"** (층 수가 **정확히 4** 여야, 아니면 `0x16c9a0cd`); `0x0a`/`0x0b` 는 TCP 갈래 (`OsfTowerExplode`); 그 밖은 `0x16c9a0cd`. 그리고 장부 항목의 protseq 문자열이 이것과 `_stricmp` 로 **같아야** (대소문자 무시) — 같은 것만 응답에 들어감 | `TowerExplode.asm +0x4b` (층 수 3 ~ 6), `+0x16b ~ +0x180` (프로토콜 ID 갈래), `+0x1a6 CMP R9D,4`; `GetEntriesNoLock +0x2b0 ~ +0x2fc` (`+0x2dd _stricmp`) |
| **4층 (끝점)** | **안 봄** — 끝점 인자가 NULL 이라 `TowerExplode` 가 4층을 읽지 않고 돌아옴 (층 수 4 만 확인) | `TowerExplode +0x1b9 TEST RSI,RSI` · `+0x1bc JZ +0x265` (`XOR EBX,EBX` … 0 반환) |
| SID (5번째 인자) | NULL 이면 안 봄. 있으면 4절 | `ept_map_auth +0x2f ~ +0x32` |
| 장부 항목의 탑 자체 (`+0x28`) | 정리 · 검사 없음 — 있는 대로 복사 | `GetEntriesNoLock +0x3e2 ~ +0x435` |
| 의미 모를 검사 | 객체 UUID **포인터 == 탑 포인터** 면 예외 `0x6d8` (정상 호출에선 안 걸림) `[추측]` | `ept_map_auth_sd +0xd6 CMP R14,R10` · `+0xdb MOV R15D,0x6d8` |

- 맞는 항목은 `IFObjList` (전역 목록) 를 **앞에서부터** 훑는다 (`GetEntriesNoLock +0x121`). 맞는 항목마다 **그 항목의 탑 목록 전체** (`+0x2b0 MOV RDI,[RBX+0x20]`) 를 훑어 protseq 가 같은 것을 하나씩 응답 배열에 넣는다 (`+0x47a MOV RDI,[RDI]` 다음 탑). 탑 수가 `max_towers` 에 닿으면 (`+0x39b CMP [R9],ECX` · `+0x39d JNC`) 나머지는 이어받기 블록으로 간다.
- 항목 안 탑 차례 = **등록 때 탑 차례의 거꾸로** (5-3). 항목 사이 차례 = 목록 차례 (같은 프로세스 항목들은 붙어 있음, `EnLinkOnIFOBJList`: 새 프로세스의 첫 항목은 목록 맨 끝, 같은 프로세스의 새 항목은 그 프로세스 첫 항목 앞).

## 표 3. 등록 탑 표 (nsisvc → epmapper, 끝점 `LRPC-` + 18자 = 23자)

탑 하나 = `u32 tower_length` + 탑 바이트. 만드는 곳은 `BindingAndIfToTower` → `TowerConstruct` (sast/130 3-1 의 같은 틀; 이번엔 **4층 끝점이 채워진 꼴**). 총 **92 B** (4 + 88).

| 층 | 바이트 | 출처 | 줄 |
|---|---|---|---|
| 머리 | `58 00 00 00` (`tower_length` = 끝점 RHS 24 + 5 + 0x3b = 0x58) · `04 00` (층 수 4) | 끝점 길이에서 계산 | `TowerConstruct.asm` (sast/130 3-1) |
| 1층 | `13 00` `0d` + **nsi 인터페이스 UUID 16 B** `7ea70bcf-48af-4f6a-8968-6a440754d5fa` + 주 `01 00` + `02 00` + 부 `00 00` | **서버 인터페이스 구조체의 `InterfaceId`** (nsisvc rva 0x6420: v1.0). 질문 탑이 아님 | `BindingAndIfToTower.asm +0x88 CALL RpcIfInqId` (ret `+0x8d`) |
| 2층 | `13 00` `0d` + **NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860`** + 주 `02 00` + `02 00` + 부 `00 00` | **서버 인터페이스 구조체 `+0x18` 부터 20 B (`TransferSyntax`)** — nsi 는 NDR 2.0 v2.0 (`pefile`). 질문의 2층이 아님 (NDR64 를 묻든 NDR 2.0 을 묻든 같은 탑) | `BindingAndIfToTower +0x98 ~ +0xb3` (`[RDI+0x18]` 16 B · `[RDI+0x28]` · `[RDI+0x2a]` 복사) |
| 3층 | `01 00` `0c` `02 00` `00 00` (프로토콜 ID `0x0c`) | protseq 문자열이 `ncalrpc` 이면 `TowerConstruct` 가 `_wcsicmp` 로 갈라 `0x0c` 를 씀 | `BindingAndIfToTower +0x1e6 RtlMultiByteToUnicodeN` → `+0x20e CALL TowerConstruct` (ret `+0x213`) |
| 4층 | `01 00` `10` `18 00` (RHS 길이 24) + `LRPC-` + 18자 + `00` | **바인딩의 동적 끝점 문자열** → ANSI. 끝점이 정적이면 문자열 바인딩에서 파싱 | `BindingAndIfToTower +0xb8 CALL I_RpcBindingInqDynamicEndpointW` (ret `+0xbd`), `+0xf5 UnicodeToAnsiString` (ret `+0xfa`), `+0x120 RpcStringBindingParseA` |

- **네트워크 주소 칸은 없다**: `ncalrpc` 는 5층이 없고, `TowerConstruct` 가 `ncalrpc` 일 때 네트워크 주소 인자를 안 쓴다 (`TowerConstruct.asm`, sast/130 3-1 표와 같음).
- **등록 요청 (`ept_insert_ex` 프로시저 2) 의 항목 꼴**: `ept_entry_t` 0x58 B = **객체 UUID 16 B · 탑 포인터 8 B · 주석 64 B** (`RegisterEntries +0xeb IMUL RCX,R15,0x58`, 주석은 `+0x36a RtlStringCbCopyA` 로 0x40 B 칸에 복사). 객체 UUID 가 NULL 로 오면 nil. 이 요청의 NDR 스텁 바이트는 이번에 안 풀었다 `[공백]` (다음 요청 후보).
- 이름 부분은 `ServerSetupAddress` 가 서버 주소를 만들 때 정한 난수 (`LRPC-` + 난수 9 B 의 소문자 16진 18자, sast/130 4-1) 라 **부팅마다 다르다**. 꼴 (길이 23 · 접두사) 만 쓰면 된다.

---

## 1. 장부에서 맞는 항목 고르기

### 1-1. 서버 인터페이스 두 개와 프로시저 → 함수 표 `[정적]` (`pefile` 로 RpcEpMap.dll 의 `RPC_SERVER_INTERFACE` 를 읽음)

| 인터페이스 (rva) | 프로시저 → 함수 (PDB 이름, rva) |
|---|---|
| **`e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0** (공개 EPM, rva 0x148e0) — 질문 탑이 오는 곳 | 0 · 1 · 5 · 6 = `0x83c0` (PDB 이름 넷 `ept_insert` · `ept_delete` · `ept_inq_object` · `ept_mgmt_delete` — **같은 16 B 몸을 같이 씀: 예외 `0x6d8` 만 던짐**, `ept_mgmt_delete.asm +0x4 MOV ECX,0x6d8` · `+0x9 CALL RpcRaiseException`. 공개 인터페이스로는 등록 · 삭제가 안 된다), 2 = `ept_lookup` (0x12d0), **3 = `ept_map` (0x3d00)**, 4 = `ept_lookup_handle_free` (0x83e0), **7 = `ept_map_auth` (0x3d60)**, **8 = `ept_map_auth_async` (0x3ca0)**. 디스패치 썽크: **3 · 7 · 8 = `NdrAsyncServerCall` (0x72c0, 비동기 서버)**, 그 밖 = `NdrServerCall2` (0x72f0) |
| **`0b0a6584-9e0f-11cf-a3cf-00805f68cb1b` v1.1** (epmapper 전용 로컬, rva 0x14cc0) — **등록이 오는 곳** | 0 = `OpenEndpointMapper` (0x8280), 1 = `AllocateReservedIPPort` (0x7bf0), **2 = `ept_insert_ex` (0x22a0)**, **3 = `ept_delete_ex` (0x30a0)**, 4 = `SetRestrictRemoteClients` (0x82e0), 5 = `ResetWithNoAuthException` (0x82d0), 6 = `ReRaiseInterfaceTriggers` (0x4d30) |
| 세 번째 `1d55b526-c137-46c5-ab79-638f2a68e869` v1.0 (rva 0x15830, 프로시저 13개) | 디버그 인터페이스 (`DebugServerSecurityCallback`) — 이번 물음 밖, 이름만 |

- rpcrt4 쪽 클라이언트 구조체도 `pefile` 로 확인: `e1af8308` v3.0 = rva 0xe1220 (프록시 `0xe11e8`, 질문 · `ept_map`), **`0b0a6584` v1.1 = rva 0xe14a0 (프록시 `0xe1500`, 등록)**. `RegisterEntries` 가 `NdrClientCall3(프록시 0xe1500, 프로시저 2, …)` 를 부르므로 (`+0x296`, ret `+0x29b`) **등록 = 서버의 `ept_insert_ex`**.
- 등록 인터페이스는 `ncalrpc` 에서만 받는다: `StartEndpointMapper +0x1c6 CALL RpcServerRegisterIf3` (ret `+0x1cd`) 의 콜백이 `LocalEpmpSecurityCallback` (`+0x19b LEA RAX,[0x180006250]`) 이고, 이것은 **전송 종류가 4 (ncalrpc) 가 아니면 `5` (접근 거부)** (`LocalEpmpSecurityCallback.asm +0x25 SUB EAX,4` … `+0x2c AND EAX,5`; 종류 조회 실패도 5 — `+0x1f JNZ +0x35 MOV EAX,5`).

### 1-2. 프로시저 3 · 7 · 8 은 같은 몸 `[정적]`

- `ept_map_auth_async.asm +0x4e CALL ept_map_auth` (ret `+0x53`) — 인자 10개를 그대로 넘김. `ept_map.asm +0x4c CALL ept_map_auth` (ret `+0x51`) — **SID 자리에 `+0x43 MOV [RSP+0x20],0` (NULL)** 을 넣고 나머지 9개를 넘김. 프로시저 7 은 `ept_map_auth` 그 자체.
- `ept_map_auth` (0x3d60) 은 SID 를 SD 로 바꾸고 (4절) `+0x1ee CALL ept_map_auth_sd` (ret `+0x1f3`) 를 부른다. 부른 뒤 SD 를 해제.
- **응답은 같은 호출 안에서 바로 간다**: `ept_map_auth_sd +0x1b8 CALL RpcAsyncCompleteCall` (ret `+0x1bf`, `+0x1ae TEST R12D,R12D` 가 참일 때 — 보류하지 않은 모든 길). 보류하는 길은 3-2.

### 1-3. 고르는 규칙의 줄 (`ept_map_auth_sd`)

| 단계 | 한 일 | 줄 |
|---|---|---|
| 1 | SD 가 있으면 `IsValidSecurityDescriptor`, 나쁘면 예외 `0x57` | `+0xa2` (ret `+0xa9`), `+0xb4 LEA R15D,[RDI+0x57]` |
| 2 | 객체 UUID 포인터가 NULL 이면 nil UUID 로 대체 | `+0xcb ~ +0xd2` |
| 3 | `TowerExplode` 로 질문 탑 풀기. 실패하면 **상태 = 그 코드 (`0x16c9a0cd` 등), `num_towers = 0`, 핸들 안 건드림**, 그래도 호출은 정상으로 끝냄 | `+0x102` (ret `+0x109`), `+0x110 MOV [RBX],EAX`, `+0x116 MOV [R13],ECX` |
| 4 | 버전 합치기 `주 << 16 \| 부` | `+0x11c ~ +0x129` |
| 5 | 인터페이스가 관리 인터페이스 `afa8bd80-7d8a-11c9-bef4-08002b102989` (`MgmtIf`, `pefile`) 면 다른 비교 (`SearchIFObjNode`: 객체 UUID 만 비교하는 갈래 등) — 이번 물음 밖 | `+0x130 ~ +0x148`, `+0x249 CALL GetEntries` (ret `+0x24e`) |
| 6 | 일반: `GetEntries(객체, 인터페이스, 버전, protseq, SD, 핸들, 탑 배열, max_towers×8, 모드 2, max_towers, &탑 수, 0, 0, WildCardMatch, &접근거부표시)` | `+0x255 ~ +0x2c2` (`+0x268 LEA RDX,[0x180001240]` = `WildCardMatch`, `+0x2a3 MOV [RSP+0x38],EAX` = `max_towers×8`, `+0x29b MOV [RSP+0x40],2` = 모드 2), ret `+0x2c7` |
| 7 | 결과가 `0x16c9a0d6` 이고 **핸들이 0** 이고 **접근거부 표시가 0** 이고 `g_TriggersDisabled == 0` 일 때만 3-2 (트리거) 로 | `+0x2c9 CMP EAX,0x16c9a0d6`, `+0x2da CMP [RAX],RCX`, `+0x2e3 CMP [RBP-0x68],ECX`, `+0x2ec CMP [0x18001ccc8],ECX` |

- `GetEntries` (0x13f0) 는 `EpMapRWLock` 공유 잠금 (`+0x2a`) 을 잡고 `GetEntriesNoLock` (`+0xc7`) 을 부르고 풀 뿐 (`+0xd5`).
- 이름 대응 `[이름 추측]`: `GetEntriesNoLock` 의 인자 `모드` 1 = `ept_lookup` (항목 0x58 B 를 채움), 2 = `ept_map` (탑 포인터만) — `GetEntriesNoLock +0x3c7 SUB ECX,1`, `+0x9c6 CMP EAX,0x58`.

## 2. 돌려주는 탑

- **그대로 복사** `[정적]`: 장부에 넣을 때 `ept_insert_ex +0x274 CALL memcpy` (ret `+0x279`) 가 **등록 요청의 탑 바이트 `tower_length + 4` 개를 PSEP 노드 안에 복사**하고 (`+0x26d MOV R8D,[RDX]` · `+0x270 ADD R8,4`), 꺼낼 때 `GetEntriesNoLock +0x3e2 MOV RAX,[RDI+0x28]` → `+0x40f HeapAlloc` → `+0x435 memcpy` 로 **다시 한 번 그대로 복사**해 응답 배열에 놓는다. 어디에서도 층 바이트를 고쳐 쓰지 않는다. **예외 하나**: protseq 가 `ncacn_ip_tcp` · `ncacn_http` 인 탑은 탑 `+0x4b` 의 4 B (TCP 탑의 주소 칸으로 보임 `[추측]`) 를 로컬 주소로 덮어쓸 수 있다 (`GetEntriesNoLock +0x44c JNZ +0xb7f`, `+0xb83 MOV [RAX+0x4b],ECX`) — ncalrpc 는 해당 없음.
- **1 · 2층은 등록한 쪽 인터페이스에서** (표 3) 와서, 질문 탑의 2층 (NDR 2.0 이든 NDR64 든) 과 무관하다. 1층 버전도 **등록 때 버전** (질문 부 버전보다 크거나 같을 수 있음).
- **여러 개일 때**: 맞는 탑을 전부 `max_towers` 개까지 (`+0x39b CMP [R9],ECX` · `+0x39d JNC +0x657` — 이미 `max_towers` 개면 이어받기 블록으로). 한 번 부름에 같은 항목의 같은 protseq 탑이 여럿이면 (같은 프로세스가 ncalrpc 주소를 여럿 열었을 때) **전부** 들어간다. 차례 = 항목 사이는 `IFObjList` 차례, 항목 안은 **등록 순서의 거꾸로** (5-3).
- 탑을 꺼낼 때마다 그 항목의 "마지막 사용 시각" 을 `GetTickCount64` 로 갱신 (`+0x461` → `+0x46d MOV [RBX+0x98],RAX`) — 응답 바이트에는 영향 없음.
- 핸들 값이 0 아닌 때 · 배열 머리: 표 1 아래 정리.

## 3. 맞는 항목이 없을 때

### 3-1. 평범한 길 (트리거 키 없음) `[정적]`

- `GetEntriesNoLock` 이 끝까지 맞는 탑을 못 넣었을 때 (`+0x49f JZ +0x84b`, 탑 수 0): `max_towers ≠ 0`, 그때까지 오류 (`0xe` · `0x57`) 가 없으면 → **`EDX = 0x16c9a0d6`** (`+0x867`) 로 돌려준다. (`max_towers = 0` 이면 `+0x852 JZ +0x8a8` 로 가서 상태 0 · 핸들 -1.)
- 접근 거부가 한 번이라도 있었으면 (`+0x85c CMP [RSP+0x48],5` → `+0xbcf` 에서 `[접근거부표시] = 1`) 상태는 같고 **표시만 1** 로 해서 트리거 길을 막는다 (SID 를 준 호출만 해당).
- `ept_map_auth_sd` 가 다시 한 번 `0x16c9a0d6` 를 `[RBX]` 에 쓴다 (`+0x38d`). `*num_towers = 0` (`GetEntriesNoLock +0x8e` 에서 시작 때 0), 핸들 = `max_towers ≥ 2` 면 0 (`+0x4c3`) / `≤ 1` 이면 `-1` (`+0xa36`).
- **결과 = 표 1 의 "없음" 열** (`max_towers = 4`: 핸들 20 B 0 · `0` · `4, 0, 0` · 상태 `0x16c9a0d6`, 40 B). **우리 `epm.cpp` 의 지금 답 (`reply(0x28)` · `+0x18 = max_towers` · `+0x24 = 0x16c9a0d6`) 과 바이트가 같다.**
- 상태 이름: `0x16c9a0d6` → rpcrt4 가 `0x6d9` (`EPT_S_NOT_REGISTERED`) 로 바꾼다 (`FatalLookupErrorEncountered.asm +0x26 SUB EAX,0x16c9a0cd` · `+0x2d CMP EAX,9` · `+0x32 MOV EBX,0x6d9`; `0x16c9a0cd` 자체는 `+0x87 MOV EBX,0x6d8`). 이름 대응 (DCE `ept_s_not_registered`) 은 `[이름 추측]` — 코드는 값만 쓴다.

### 3-2. 다른 길: 서비스 트리거가 걸려 있을 때 — 요청을 **보류**한다 `[정적]`

위 7단계 조건 (상태 `0x16c9a0d6` · 핸들 0 · 접근거부 표시 0 · `g_TriggersDisabled == 0`) 이면:

1. `ept_map_auth_sd +0x2f8 ~ +0x335`: 0x94 B 버퍼를 잡고 `CreateIfIdString` 으로 `"<인터페이스 UUID>"` (객체 UUID 가 nil 이 아니면 `":<객체 UUID>"` 를 덧붙임) 문자열을 만든다.
2. `+0x346 RtdsTriggerExists` (ret `+0x34b`) — **레지스터리 키가 있는지** 본다. 키 경로 (`RtdspBuildKeyString` · `RtdspTriggerRegKeyExists +0x32 RegOpenKeyExW(HKLM, …)`): `HKLM\Software\Microsoft\Windows\CurrentVersion\NetworkServiceTriggers\Triggers\bc90d167-9470-4139-a9ba-be0bbbf5b74d\<인터페이스 UUID>[:<객체 UUID>]` (`bc90d167-…` = `RPC_INTERFACE_EVENT_GUID`, rva 0x16690, `pefile`). 객체 UUID 가 있고 못 찾으면 **객체 쪽을 잘라 한 번 더** (`+0x376 MOV [RSI+0x48],CX` · `+0x37d`).
3. **키가 없으면** 즉시 `*status = 0x16c9a0d6` (`+0x38d`) — 3-1 과 같은 답.
4. **키가 있으면** 잠금 (`+0x39d CALL Request`, ret `+0x3a2`) 을 잡고 `GetEntriesNoLock` 을 한 번 더 (`+0x40c`, ret `+0x411`). 그래도 없으면 → (기능 스위치가 꺼져 있거나 인터페이스별 보류 개수가 한도 `g_EpmPerIfCap` 이하면) **버퍼 요청 (`InitializeBufferedRequest` `+0x4e5` → `NtrbEnqueueRequest` `+0x502`) 을 만들어 대기열에 넣고, 호출을 끝내지 않는다** (`+0x516 MOV R12D,R14D` → `RpcAsyncCompleteCall` 안 부름, ETW "ServiceTriggerPerfServiceTriggered" 기록 `+0x539 · +0x558`). 한도를 넘으면 상태 **`0x6d8`** 로 즉시 끝 (`+0x45c MOV [RBX],0x6d8`). 이 보류 요청은 나중에 그 인터페이스가 등록될 때 (`ept_insert_ex +0x50c CALL CompletePendingEprRequests`, ret `+0x511`) 풀려 답을 받는다.
- **우리 쪽에 걸리는 점**: 이 길은 **Windows 레지스트리에 위 키가 있을 때만** 탄다. 우리 VM 레지스트리 수집 (`reg-users`) 에서 `NetworkServiceTriggers\Triggers\bc90d167-…` 아래에 nsi 인터페이스 UUID 키가 있는지만 보면 된다 `[공백: 이번엔 VM 레지스트리를 안 봄]`. 없으면 3-1 이 전부.
- `g_TriggersDisabled` (rva `0x1ccc8`) 가 무엇이 세우는지는 안 읽음 `[공백]` (동적: 값 한 번 읽기).

## 4. 부른 쪽 검사

- **프로시저 8 (`ept_map_auth_async` → `ept_map_auth` → `ept_map_auth_sd` → `GetEntriesNoLock`) 안에 호출자 프로세스 ID · 이미지 · 토큰 비교는 없다** (`RpcServerInqCallAttributesW` 는 `GetEntriesNoLock +0x54e` 한 곳, 방화벽 확장이 켜졌을 때의 아래 길뿐; `RpcImpersonateClient` 는 `ept_insert_ex` 에만).
- **SID 인자** (프로시저 7 · 8 의 5번째, unique 포인터): `ept_map_auth +0x27 MOV RDI,[RSP+0x90]` · `+0x2f TEST RDI,RDI` · `+0x32 JZ +0x9e` — **NULL 이면 SD 도 NULL 로 하고 아무 검사도 안 한다** (우리 클라이언트는 NULL, sast/130 3-1). 있으면: `IsValidSid` (`+0x37`, 실패하면 `+0x91 RpcRaiseException(0x6d8)`), 0x28 B SD 를 `HeapAlloc` (`+0x59`), `InitializeSecurityDescriptor` (`+0xb7`), ACL 을 `I_RpcAllocate(SID 길이 + 0x58)` (`+0xdf`) 로 만들어 **`AddAccessAllowedAce(ACL, 2, 0x20000, SID)`** (`+0x128`) · DACL · 그룹 · 소유자 (`+0x149 · +0x166 · +0x183`) 를 채움. 어느 하나라도 실패하면 **예외 `0x6d8`** (`+0x246 ~ +0x2a6`, `+0x253 JMP +0x8c`). 그다음 `ept_map_auth_sd` 가 `IsValidSecurityDescriptor` (`+0xa2`, 실패 `0x57`).
- SD 가 있을 때 **장부에서 항목을 고른 뒤** 그 항목을 등록한 사용자의 토큰 (`[노드+0x18]`) 으로 `AccessCheck(SD, 토큰, 0x20000, …)` (`GetEntriesNoLock +0x1f2`): 허용이면 맞음, 거부면 그 항목을 건너뛰고 표시 (`+0x21f ~ +0x223`). (등록 쪽은 `ept_insert_ex +0x2ee RpcImpersonateClient` → `+0x308 GetCurrentUserToken` 으로 등록자 토큰을 노드 `+0x18` 에 보관: `+0x418 MOV [RBX+0x18],RSI`.)
- **인터페이스 보안 콜백**:
  - 공개 인터페이스 `e1af8308`: `StartEndpointMapper +0x182 RpcServerRegisterIf3` (ret `+0x189`). 콜백 `RemoteEpmpServerSecurityCallback` (0x5a30) 은 **`I_RpcSystemFunction001(4, …)` 가 0 이 아닐 때만 붙고 깃발 `0x10` 이 같이 켜진다** (`+0x151 CMP [RSP+0x40],EDX` · `+0x157 LEA RAX,[0x180005a30]` · `+0x163 LEA R9D,[R8+0x10]`; 0 이면 콜백 NULL · 깃발 0, `+0x169`). 콜백 몸: `g_fOpenEpmpInterface != 0` 이면 허용 (`+0x4 CMP`, `+0xd XOR EAX,EAX`), 아니면 `I_RpcServerCheckClientRestriction` (원격 클라이언트 제한 · `g_fRestrictRemoteClients`), 그것이 0 이 아니면 `EPMAllowAnonymous == 3` 일 때만 허용 (`+0x31 ~ +0x3e`), 아니면 **5**. 실패 때 값 = **5 (접근 거부)**.
  - 이 제한이 **로컬 ncalrpc 호출자에게도 걸리는지**는 `I_RpcServerCheckClientRestriction` (rpcrt4 0xba8a0) 의 객체 종류 검사 (`InvalidHandle(…, 0x2000)` · `(…, 0x40)`) 를 풀어야 닫힌다 `[공백]`. 실제 Windows 에서 로컬 ncalrpc 질문이 통과한다는 점 (run141 의 연결이 서버까지 닿는 기록) 과, 우리 목적 (서버가 답하는 쪽) 에선 영향 없음.
  - 방화벽 확장 (`gFwManagerExtensionState == 1` 일 때만, `GetEntriesNoLock +0x363 CMP [0x18001cf60],1`): 호출자가 **원격**일 때만 확장에 물어 탑을 걸러낸다 (`+0x4f9 CALL I_RpcBindingIsClientLocal` · `+0x517 TEST EAX,EAX` 가 로컬이면 건너뜀) `[추측: ncalrpc 호출자는 로컬로 판정됨 — 그 가상 함수는 안 읽음]`. 켜졌는지는 동적 (`gFwManagerExtensionState`).
- 닫는 말: 프로시저 8 에서 **호출자를 가리는 줄은 SID (옵션) 뿐**, 그 밖엔 인터페이스 콜백 (조건부) 이다. 실패 값: SID 오류 → 예외 `0x6d8` / `0x57`; 콜백 → `5`.

## 5. 등록 쪽

### 5-1. rpcrt4 `RpcEpRegisterW` 부터 epmapper 까지 (한 줄 사슬)

`nsisvc NsiStartRpcServer +0x2de RpcServerInqBindings` (ret `+0x2e5`) → `+0x337 RpcEpRegisterW(IfSpec, 벡터, NULL, L"NSI server endpoint")` (인자: `NsiStartRpcServer.asm +0x325 ~ +0x334` — `RDX = [RSP+0x40]` 벡터, `R9 = 0x180007810` = 와이드 주석, `R8 = 0` = UUID 벡터 없음; 문자열은 `strings` 로 `NSI server endpoint` 확인) →
`RpcEpRegisterW +0xf` (ret `+0x14`) `RegisterRpcEndpointsW` (0x62754) → `+0x67` `RegisterRpcEndpointsA` (0x62828) → `+0xe0` (ret `+0xe5`) `RegisterEntries` (0x62934).

| 단계 | 한 일 | 줄 |
|---|---|---|
| 주석 길이 | 주석이 **64 B 이상이면 `0x6d7`** 로 끝 (`"NSI server endpoint"` 는 19 자라 통과) | `RegisterRpcEndpointsA +0x54 CMP RAX,0x40` · `+0x5a MOV EAX,0x6d7` |
| UUID 벡터 NULL | **nil UUID 하나짜리 벡터**로 대체 (`Count = 1`) → 객체 UUID = nil | `RegisterRpcEndpointsA +0x82 ~ +0x94` |
| 바꾸기 깃발 | `RegisterEntries` 의 다섯째 인자 = **1 (replace)** | `RegisterRpcEndpointsA +0xd8 MOV [RSP+0x20],1` |
| 바인딩 0 개 | 벡터 `Count == 0` 이면 epmapper 에 안 보내고 로컬 기록만 | `RegisterEntries +0x3e CMP [RDX],R13D` · `+0x41 JZ +0x39f` (벡터 `Count == 0` → `+0x39f ~ +0x3ad CALL InterfaceExported` 만, epmapper 에 안 보냄) |
| epmapper 에 연결 | `BindToEpMapper(…, ncalrpc ← epmapper)`, 실패하면 오류 | `RegisterEntries +0x7e` (ret `+0x83`) |
| 항목 배열 | `벡터 개수 × 0x58` B 를 잡고, 바인딩마다 `BindingAndIfToTower` 로 **탑을 만들어 항목에 넣음** (실패한 바인딩은 건너뜀), 주석을 항목마다 복사 | `+0xeb IMUL RCX,R15,0x58`, `+0x332 CALL BindingAndIfToTower` (ret `+0x337`), `+0x36a RtlStringCbCopyA` (ret `+0x36f`), `+0x36f ADD R13,0x58` |
| 보내기 | **객체 UUID 하나마다 한 번** 모든 항목의 객체 UUID 칸을 그 UUID 로 채우고 `NdrClientCall3(프록시 0xe1500, 프로시저 2, …)` (인자: 전역 컨텍스트 핸들 `hEpContext` · 개수 · 항목들 · replace · …) | `+0x296 CALL NdrClientCall3` (ret `+0x29b`); 앞뒤로 `EpContextMutex` 잠금 (`+0x22c` · `+0x2d5`). 응답이 `0x6ba` · `0x6bf` 면 다시 `BindToEpMapper` 로 바인딩해 한 번 더 (`+0x405`, `+0x48a`) |
| 성공 뒤 | 로컬 서버 객체에 "내보냄" 기록 (`InterfaceExported`) | `+0x1a5` (ret `+0x1aa`) |

### 5-2. 4층 이름은 `ServerSetupAddress` 의 그 이름인가

- **같은 이름이라고 읽힌다** `[정적, 마지막 연결 추정]`. 탑 4층은 `BindingAndIfToTower +0xb8 I_RpcBindingInqDynamicEndpointW` 가 돌려주는 문자열이고, 그것은 `SVR_BINDING_HANDLE::InquireDynamicEndpoint` (`InquireDynamicEndpoint.asm +0x6` `CMP [RCX+0x100],1` · `+0x1f MOV RCX,[RCX+0xe8]` · `+0x2b MOV RCX,[RCX+0x10]` · `+0x2f DuplicateString`) 가 **서버 주소 객체 `+0x10` 의 끝점 문자열을 복사**한 것이다 (동적 끝점일 때; 아니면 NULL → 문자열 바인딩에서 파싱, `BindingAndIfToTower +0x120`).
- 그 주소 객체의 끝점 문자열을 채우는 쪽은 `LRPC_ADDRESS::ServerSetupAddress` (`+0xe8 ProcessPrng` → `LRPC-`+18자, `+0x179 DuplicateString` → `[RSI]` 출력 칸에 저장, sast/130 4-1). **`ServerSetupAddress` 의 출력 칸이 주소 객체 `+0x10` 인지** (`CreateOrUpdateAddresses` 쪽) 는 이번에 안 읽었다 `[추측]`. 동적으로 닫기: epmapper 응답 4층 이름과 `\RPC Control\<이름>` 포트 이름이 같은지 (Q280 의 둘째 연결 멈춤점).
- 정적 끝점이면 (`RpcServerUseProtseqEp`) 탑 4층 = 그 문자열. nsi 는 끝점 인자가 없는 동적 끝점 (sast/130 4-1 `NsiStartRpcServer +0x123`).

### 5-3. 그 프로세스의 다른 끝점 · 다른 바인딩도 같이 들어가나 — **예, 전부**

- 벡터는 `RpcServerInqBindings` → `RPC_SERVER::InquireBindings(GlobalRpcServer, …, NULL, NULL)` (`RpcServerInqBindings.asm +0x26` ret `+0x2b`) 가 만든다 — **그 프로세스의 모든 `RPC_ADDRESS` 를 돌며 (`InquireBindings` 의 `[this+0x48]` 배열 · `[this+0x50]` 개수) 주소마다 네트워크 주소 하나당 바인딩 하나** (`InquireBindings.c`: 두 번 훑어 개수를 세고 채움). 인터페이스 · protseq 로 거르지 않는다 (인터페이스 그룹 · SD 인자가 NULL). 바인딩이 하나도 없으면 `0x6b6`.
- 그러니 nsisvc 가 든 프로세스에 **다른 서비스 · 다른 protseq 의 서버 주소가 있으면 그것도 같이 nsi 인터페이스의 탑으로 등록된다.** `ept_map` 응답에는 질문의 protseq (`ncalrpc`) 와 같은 것만 나온다. **ncalrpc 주소가 둘 이상이면 탑이 여럿**이 된다.
- **차례**: 등록 요청의 항목은 벡터 차례, 장부의 탑 목록은 **거꾸로** — `ept_insert_ex +0x279 MOV [RBX],RDI` (새 탑의 다음 = 지금까지의 머리) · `+0x27c MOV RDI,RBX` (머리 = 새 탑). 응답은 그 목록 차례이므로 **벡터의 마지막 ncalrpc 바인딩 탑이 응답 맨 앞**. 클라이언트 `ProcessNextChunk` 는 원하는 개수 (이름 한 개) 가 차면 멈춘다 (sast/130 3-2) → **첫 탑의 이름이 쓰인다.**
- nsisvc 프로세스에 ncalrpc 주소가 몇 개인지는 정적으로 못 닫는다 `[공백]` (다른 서비스가 같은 `svchost` 에 있는지에 달림). 우리 에뮬레이터는 nsi 창구 주소 하나뿐이면 탑 하나.

### 5-4. epmapper 의 등록 프로시저 `ept_insert_ex` 가 장부에 넣는 꼴 (이름 · 한 줄씩)

- 프로시저: 로컬 인터페이스 `0b0a6584` v1.1 의 **2 번** (`ept_insert_ex`, rva 0x22a0). 인자 꼴 (줄에서 읽은 것): 바인딩 · **프로세스 컨텍스트 핸들** (`HPROCESS`, 처음엔 NULL — 만들 때 `+0x667 CMP [RAX],0xdecafbad` 매직로 검사) · 항목 수 · 항목 배열 · replace · 깃발 · … · 상태 (`[이름 추측]`).
- **탑마다 PSEP 노드**: `TowerExplode` (`+0x9d`) 로 항목 탑을 풀어 protseq · 끝점 문자열을 얻고 (실패하면 상태 = 그 코드; 메모리 `0xe`), **protseq · 끝점 · 탑 사본을 한 덩어리에** 놓는다 (`+0x165 HeapAlloc` 크기 = `tower_length + 0x40 + 끝점 길이 + protseq 길이`; 매직 `0x50534550` `+0x21b`; `+0x18 protseq 포인터` · `+0x20 끝점 포인터` · `+0x28 탑 포인터 (4 정렬)`). 탑의 protseq 가 `ncacn_ip_tcp` · `ncacn_http` 면 포트 관련 검사가 따로 있다 (`+0xc7 · +0xe6 _stricmp`) — ncalrpc 아님.
- **인터페이스 항목 (IFOBJ)**: `+0x3ad HeapAlloc (0xa0 + 주석 길이)`, 매직 `0x49464f42` (`+0x3e3`), **`+0x38` 객체 UUID (첫 항목의 것)** · **`+0x48` 인터페이스 UUID** · **`+0x58` 버전 (주 << 16 \| 부)** · `+0x60` 주석 포인터 (본문은 `+0xa0`) · `+0x18` 등록자 사용자 토큰 · `+0x20` PSEP 목록 머리 · `+0x94` 마지막 인자 × 1000 (`+0x462 IMUL ECX,[RBP+0x7f],0x3e8`) · `+0x98` 마지막 사용 시각 (0).
- **덮어쓰기 · 중복** (한 줄): **replace = 1 (`RpcEpRegister`)** 이면 같은 프로세스의 같은 (객체 UUID, 인터페이스 UUID, 같은 주 버전 · 장부 부 ≤ 새 부) 항목을 찾아 (`FindIFOBJNode`, `SearchIFObjNode` 모드 3 · 6, `ept_insert_ex +0x6c5`) **옛 PSEP 중 새 탑과 protseq 가 같은 것을 지우고** (`PurgeOldEntries(…, 0)`: protseq 만 견줌) 새 탑 목록을 **옛 목록 앞에 붙인다**; 버전이 정확히 같으면 그 항목에 합치고 아니면 새 항목. **replace ≠ 1 (`RpcEpRegisterNoReplace`)** 이면 정확히 같은 버전 항목에만 합치되 protseq **와 끝점**이 같은 것만 지움 (`ExactMatch`, `PurgeOldEntries(…, 1)`). 새 항목은 `EnLinkOnIFOBJList` 로 전역 목록에 걸고 (`+0x45d`), 끝나면 보류 요청을 깨운다 (`+0x50c CompletePendingEprRequests`).
- **등록자 프로세스가 끝나면** 컨텍스트 핸들 rundown (`HPROCESS_rundown`, rva 0x7fe0) 이 그 프로세스 항목을 지운다 `[이름 추측 · 이번엔 안 읽음]`.

---

## 지금 우리 (PE_emulator) 맞나 · 틀리나

대상: `ext/syscall/syscall_layer_cpp/src/rpc/epm.cpp` (읽은 시점의 파일).

| 우리 지금 | 진짜 (정적) | 판정 |
|---|---|---|
| 프로시저 8 만 등록, 3 · 7 은 공사 중 | 3 · 7 · 8 은 **같은 몸** (비동기 서버 썽크 셋 다). 3 · 7 은 NDR 요청 꼴이 달라 (`FC_FP` 포인터 ID 가 붙음, run137 3-1) 스텁 읽는 법만 다름 | 맞음 (서 바퀴에서 3 · 7 도 같은 장부 함수에 연결하면 됨) |
| 늘 40 B: 핸들 0 · 탑 수 0 · 배열 `max_towers · 0 · 0` · 상태 `0x16c9a0d6` (`TODO(VM)` `epm.cpp:23`) | **"맞는 항목 없음" 답 (`max_towers ≥ 2`) 과 바이트까지 같다** (표 1 "없음"). `TODO(VM)` 의 "등록 없음 응답 스텁 바이트" 는 정적으로 닫혔다 | **"없음" 일 때만 맞음** |
| **장부가 없다 → 늘 "없음"** | 진짜 Windows 에서 nsi 는 **부팅 때부터 장부에 있다** (nsisvc 시작 때 등록). 우리가 늘 "없음" 이라 `WinHttpSendRequest` 가 `0x6d9` 로 끝없이 FALSE | **틀림 — 이게 벽** |
| `max_towers > 500` 이면 `nullopt` | 서버 NDR 이 범위 `0 ~ 500` 을 검사 (run137) — 일치 | 맞음 |
| `max_towers ≤ 1` 일 때 | 진짜는 핸들 칸이 `-1` (비 NULL) | 우리 클라이언트는 4 라 영향 없음. 다른 샘플이 1 로 부르면 달라질 수 있음 `[공백: 선 위 바이트]` |

**"등록 장부 + 탑 답" 을 지을 때 필요한 것 한 줄씩**:
- **장부에 넣을 것 / 누가 언제**: 항목 = { 객체 UUID nil · 인터페이스 UUID · 주 · 부 · 주석 (≤ 63 자) · 탑 목록 [{ protseq, 끝점 문자열, 탑 바이트 }] }. **nsi (`7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0, `"NSI server endpoint"`, `ncalrpc`, 끝점 = 우리 nsi 창구 포트 이름 `LRPC-`+18자) 는 nsisvc 가 서비스 시작 때 등록**한다 (`NsiStartRpcServer +0x337`, 서비스는 test.exe 보다 먼저 돎) → 에뮬레이터는 **epmapper 창구가 뜰 때 미리 한 항목을 채우면** 된다 (그 이름의 포트가 nsi 창구와 같아야 함, 5-2). 샘플이 직접 `RpcEpRegister*` 를 부르면 `ept_insert_ex` (로컬 인터페이스 `0b0a6584`, 프로시저 2) 를 받아야 하는데 그건 이번엔 안 풀었다 (다음 요청 후보).
- **고르기 규칙**: 표 2 — 질문 1층 UUID + 주 버전 같음 + 장부 부 ≥ 질문 부 + (장부 객체 nil 이거나 같음) + 3층 `0x0c` 이면 protseq "ncalrpc" 인 탑만. 전송 문법 · 4층 · 질문 객체 UUID 의 nil 여부는 고르기에 안 씀 (nil 장부 항목은 어떤 객체 UUID 질문에도 맞음).
- **응답 바이트 꼴**: 표 1. 맞는 탑 n개 (≤ `max_towers`, 우리는 4) → 핸들 20 B 0 · `n` · `max_towers, 0, n` · 포인터 ID n개 · 탑마다 `[len][len][바이트]` (4 정렬) · 상태 0. 없음 → 40 B (지금 답). 탑 바이트 = 표 3 (nsi 하나, 끝점 23자 → 92 B).

## 정적으로 못 닫은 것

- `[공백]` nsisvc 프로세스의 **ncalrpc 주소 개수** — 탑 수와 첫 탑이 달라짐 (5-3). 동적.
- `[공백]` 트리거 키 (3-2) 가 이 VM 에 **있는지**, `g_TriggersDisabled` · `gFwManagerExtensionState` · `EPMAllowAnonymous` 값.
- `[공백]` `max_towers ≤ 1` 의 `-1` 핸들이 선 위에서 되는 20 B.
- `[추측]` 4층 이름 = 서버 주소 객체 `+0x10` 문자열 = `ServerSetupAddress` 의 이름 (5-2 마지막 연결).
- `[공백]` `I_RpcServerCheckClientRestriction` 의 ncalrpc 갈래 (4절).
- `[공백]` `ept_insert_ex` 요청의 NDR 스텁 바이트 (등록을 에뮬레이터가 받아야 할 때만 필요).
- `[이름 추측]` DCE 상태 이름 (`0x16c9a0cd`, `0x16c9a0d6`), `HPROCESS` · `IFOBJ` · `PSEP` 노드 이름, `ept_insert_ex` 인자 이름.

## 동적으로 확인할 것 (원문 물음 2 · 3 의 멈춤점과 합치기 — Q280 에 얹음)

**epmapper 는 서비스 `RpcEptMapper` = `svchost.exe -k RPCSS -p` 안에서 `RpcEpMap.dll` 로 돈다** (`[수집] reg-users`). 아래 `RpcEpMap!…+0x…` 는 전부 **그 svchost (RPCSS) 프로세스 안**의 유저 멈춤점이다. 커널 디버거에서 그 프로세스 조건을 붙여야 한다. `rbp` 는 함수 머리에서 `LEA RBP,[RSP-0x10]` 로 잡은 값 (`ept_map_auth_sd +0x10`).

| # | 멈춤점 | 볼 것 | 닫는 질문 |
|---|---|---|---|
| D1 | `RpcEpMap!ept_map_auth_sd+0x109` (`TowerExplode` ret) | `eax` = 0, `[rbp-0x28]` (인터페이스 UUID 16 B + 주 u16 + 부 u16), `[rbp-0x60]` = "ncalrpc" 문자열 포인터 | 질문이 우리가 읽은 대로 풀리나 |
| D2 | **`RpcEpMap!ept_map_auth_sd+0x2c7`** (`GetEntries` ret, 첫 번째) | `eax` = 상태, `[r13]` (u32) = 탑 수, `[rbp-0x48]` 가 가리키는 8 B = 핸들 칸, `[rbp-0x40]` 가 가리키는 배열의 포인터 n개 → 각각 `db <ptr> L(poi 하위 16비트 + 4)` = **응답 탑 바이트 (표 3 과 같은지)** | 맞는 항목 · 탑 바이트 · 핸들 값 |
| D3 | `RpcEpMap!GetEntriesNoLock+0x4c6` (`MOV EAX,EDX`, 마지막 상태) · 또는 `+0x867` (없음) | `edx` | 상태 값 |
| D4 | **상태 사진**: `dq RpcEpMap!IFObjList L1` → 노드 → `+0x48` UUID 가 nsi (`7ea70bcf…`) 인 것의 `+0x38` · `+0x58` · `+0x60` (주석) · `+0x20` (PSEP 목록) → 각 PSEP 의 `+0x18` protseq · `+0x20` 끝점 · `+0x28` 탑 | **등록된 탑 그대로** (5-3 의 차례 · 개수 포함). 멈춤점 없이 덤프만으로 닫힘 — `ept_insert_ex` 는 부팅 때 지나가 버려 잡기 어렵다 |
| D5 | (선택) `rpcrt4!RegisterEntries+0x29b` (`NdrClientCall3` ret) 를 **nsisvc 가 든 svchost 에서**, 서비스 시작 때 | 항목 배열 (`0x58 × n`) | 벡터 개수 · 주석 |
| D6 | 레지스트리: `HKLM\Software\Microsoft\Windows\CurrentVersion\NetworkServiceTriggers\Triggers\bc90d167-9470-4139-a9ba-be0bbbf5b74d\` 아래에 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` 키가 있는지 | 키 있음/없음 | 3-2 |
| D7 | `RpcEpMap!gFwManagerExtensionState` (rva 0x1cf60) · `g_TriggersDisabled` (rva 0x1ccc8) · `EPMAllowAnonymous` (rva 0x1c5e8) 읽기 | 값 | 4 · 3-2 의 갈림 |

- 원문 물음 2 의 "요청 · 응답" 멈춤점 (`rpcrt4!LookupNextChunk+0x11d` · `IoCompleted+0xcf`) 과 같은 실행에서 D2 (`ept_map_auth_sd+0x2c7`) 를 같이 걸면, **서버가 만든 탑 바이트 (D2) 와 클라이언트가 받은 버퍼 (`IoCompleted+0xcf`) 를 한 번에 맞대 볼 수 있다.**
- **dast 요청 필요**: 위 D2 · D4 (필수), D6 · D7 (값 읽기), D1 · D3 · D5 (선택). 이름 확인 필요한 것은 없음 (요청서의 함수 이름은 모두 PDB 로 찾았고 `RemoteEpmpSecurityCallback` → `RemoteEpmpServerSecurityCallback` 만 정정).

## 다음 요청 후보

1. **`ept_insert_ex` (로컬 인터페이스 `0b0a6584` 프로시저 2) NDR 스텁 읽기** — 샘플이 `RpcEpRegister*` 를 직접 부르거나 에뮬레이터가 nsisvc 등록을 흉내 낼 때 필요. 요청 (항목 0x58 B 배열 · 컨텍스트 핸들) 과 응답 (상태) 의 바이트 꼴. 같은 인터페이스의 `OpenEndpointMapper` (0) · `ept_delete_ex` (3) 도 이름만.
2. **`I_RpcServerCheckClientRestriction` · `IsSecureClient` 의 ncalrpc 갈래** — 4절 `[공백]` (보안 콜백이 로컬 호출자를 막는지).
3. **`ServerSetupAddress` ↔ 서버 주소 객체 `+0x10`** 연결 (`CreateOrUpdateAddresses`) — 5-2 `[추측]`.
4. **서버 컨텍스트 핸들 `-1` 마샬링** (rpcrt4 `NdrServerContextNewMarshall` 류) — `max_towers ≤ 1` 호출이 오면.
5. 원문 물음 3 의 정적 몫 (nsisvc 프로시저 6 · webio `Startup` 다음) — Q280 쪽.
6. 셸 꼴 16 · exe 직접 4 (묻는 인터페이스를 모름): 정적으로는 질문 탑을 만드는 곳 (`ResolveEndpoint`) 이 이미 닫혀 있어, **동적으로 UUID 한 번 보는 것**이 빠르다 (D2 의 `[rbp-0x28]` 가 곧 그 UUID).

## 뽑은 것

- **RpcEpMap** (새로 analyze → 이번 export): 정규식 `^(ept_map_auth_async|ept_map|ept_map_auth|ept_map_auth_sd)$` depth 1 (17개) + `^(WildCardMatch|ExactMatch|SearchIFObjNode|NewTower|GetNewContext|ept_insert_ex|ept_lookup|ept_delete_ex|FindIFOBJNode|EnLinkOnIFOBJList|LocalEpmpSecurityCallback|ept_lookup_handle_t_rundown|ept_lookup_handle_free|ept_cleanup_handle_t_rundown|CompletePendingEprRequests|StartEndpointMapper|FindPSEP|MatchPSAndEP|GetCurrentUserToken|HPROCESS_rundown)$` depth 0 (20개) + `^(RtdspBuildKeyString|RtdspTriggerRegKeyExists|ConvertToString|ULongToHexString|IsEpmapEventEnabled|LogInterfaceRegister|PurgeOldEntries|DeletePSEPNodeList|UnLinkFromIFOBJList|MIDL_user_allocate|InitializeBufferedRequest)$` · `^(RemoteEpmpServerSecurityCallback|InitializeService|GetForwardEp)$` depth 0 → 합쳐 `.asm` 51 · `.c` 51 (+ `ept_mgmt_delete` 16 B 몸 하나) (`ms/disassemble/rpcepmap/` · `ms/decomp/rpcepmap/`), 호출표 `ms/disassemble/rpcepmap/_calls.tsv` (297줄). 지도: `ms/disassemble/rpcepmap/_map/ept_map_auth_async_{edges,nodes}.tsv`, `_summary.txt` (뿌리 `ept_map_auth_async`, 함수 43). `_refs/`: `LocalEpmpSecurityCallback.tsv` · `RPC_INTERFACE_EVENT_GUID.tsv`.
- **rpcrt4** (다시 analyze 안 함): `^(RpcEpRegisterW|RpcEpRegisterNoReplaceW|RpcServerInqBindings|RpcServerInqBindingsEx|TowerExplode|OsfTowerExplode|PruneForEpRegistration)$` depth 1 · `^RegisterRpcEndpoints.*$` depth 1 · `^(BindingAndIfToTower|BindToEpMapper|OpenEndpointMapper|InquireBindings|FindInterface|InterfaceExported)$` depth 0 · `^(I_RpcBindingIsClientLocal|I_RpcServerCheckClientRestriction)$` depth 1 · `^(I_RpcBindingInqDynamicEndpointW|InquireDynamicEndpoint)$` depth 0. 이 문서가 쓰는 파일: `RpcEpRegisterW.asm` · `RegisterRpcEndpointsW.asm` · `RegisterRpcEndpointsA.asm` · `RegisterEntries.asm` · `BindingAndIfToTower.asm` · `TowerExplode.asm` · `InquireBindings.asm` · `RpcServerInqBindings.asm` · `InquireDynamicEndpoint.asm` (SVR_BINDING_HANDLE, rva 0x9d5a0; `InquireDynamicEndpoint__a7780.asm` 은 기본 클래스) · `I_RpcBindingInqDynamicEndpointW.asm` · `I_RpcBindingIsClientLocal.asm` · `I_RpcServerCheckClientRestriction.asm` · `FatalLookupErrorEncountered.asm` · `ServerSetupAddress.asm` · `TowerConstruct.asm` (sast/130 때). nsisvc `NsiStartRpcServer.asm` (sast/130 때).
- 로그: `ms/ghidra/rpcepmap.analyze.log` · `rpcepmap.export.log` · `rpcrt4.export.log` · `rpcepmap.export.sast143{a,b,c,d}.stdout` · `rpcrt4.export.sast143{a..e}.stdout` · `rpcepmap.refs.sast143{a,b,c}.stdout` · `rpcepmap.map.sast143a.stdout`. 도는 작업 없음.

## 판정

**분석성공.** 요청 물음 1 의 다섯 줄 모두 `[정적]` 근거로 답했다 (3 → 1 → 2 → 5 → 4 차례로 먼저 썼고 전부 끝남). 서 바퀴 (`epm.cpp` 등록 장부 + 탑 답) 를 시작하는 데 필요한 것 — 고르기 규칙 (표 2) · 응답 바이트 꼴 (표 1) · 등록 탑 바이트 (표 3) · 장부에 넣을 것과 때 (nsisvc 서비스 시작) — 은 닫혔다. 닫지 못한 것은 "정적으로 못 닫은 것" 에 모았고 (nsisvc 프로세스의 ncalrpc 주소 개수 · 트리거 키 · `-1` 핸들 바이트가 주), 전부 동적 (D2 · D4 · D6 · D7) 으로 닫힌다. **버전 대조: 일치** (원본 바이트로 확인, "알려진 사실" 표에 이 모듈 줄이 없음). 스크립트 문제 없음.

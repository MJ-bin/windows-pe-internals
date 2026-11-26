# run145 epm-endpoint — 해설

> 이 글은 [run145 문서](run145-epm-endpoint.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q279** — PE_emulator 요청 "epmapper `ept_map` (프로시저 8) 이 nsi 인터페이스에 돌려주는 진짜 답" 의 **물음 1** (`_workspace/plan.md`, 이번에 **끝 (run145)**)
  - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/365_internals-epm-endpoint.md`, 튜닝 `357_tune17.md` V3b — 악성 396 샘플이 비동기 epmapper 연결을 받고, 그 가운데 376 이 winnsi 꼴 (요청 쪽 에뮬레이터 숫자).
  - 사용자 결정 2026-11-26 "**물음 1 만**, 다른 것보다 가장 먼저".
  - 결과서 판정 **분석성공** — 다섯 물음 (고르기 · 돌려주는 탑 · 없을 때 · 부른 쪽 검사 · 등록 쪽) 모두 `[정적]` 근거로 답했다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q280** (원문 물음 2 · 3 의 VM 묶음에 결과서 D1 ~ D7 을 얹음)
    - 정적 → **Q281** (결과서 "다음 요청 후보")
- 근거:
  - run 문서 [`run145-epm-endpoint.md`](run145-epm-endpoint.md) (가장 중요)
  - 요청서 [`_workspace/sast/143_epm-endpoint.md`](../../_workspace/sast/143_epm-endpoint.md) · 결과서 [`143_epm-endpoint_result.md`](../../_workspace/sast/143_epm-endpoint_result.md)
    - **처음 analyze 하나** — `RpcEpMap.dll` (9초, 함수 440). rpcrt4 는 다시 analyze 안 하고 `^이름$` export 만, nsisvc 는 run133 때 만든 `.asm` 읽기만.
    - 스크립트 문제 없음. `.asm` 오프셋을 원본 바이트 (`pefile`) 와 맞춰 봄 — **일치**.
  - flow.md 변경: docs 커밋 `6a99cb3` (`git show 6a99cb3 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다. 그리고 이번엔 다른 가게 안을 들여다봤다.**
   - 초기화 끝 (P2) 모듈 6개에 rpcrt4 · winnsi 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run99 · run129 · run133 · run137 · run141 과 같은 줄).
   - `sample/*.c` 에 RPC API 가 없다 `[소스]`.
   - epmapper 는 **다른 프로세스** 다. 서비스 `RpcEptMapper` = `svchost.exe -k RPCSS -p` 안의 `RpcEpMap.dll` (원 요청의 `[수집] reg-users`).
   - run129 ~ run141 은 **묻는 쪽** (rpcrt4 클라이언트) 이었다. 이번에 처음으로 **답하는 쪽** (epmapper 서버) 과, 거기에 **번호를 올리는 쪽** (nsisvc → rpcrt4 등록) 을 열었다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - 악성 396 · winnsi 꼴 376 · "60초에 579번", 그리고 `epm.cpp` 의 줄 번호 (`:18` ~ `:41`) 는 모두 PE_emulator 쪽 이야기다. `[관찰]` 이 아니다.
3. **오프셋은 앞 해설과 같은 규칙이다.**
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - "`+0x274 memcpy`" 처럼 부르는 함수 이름 **앞** 숫자 = **call 자리**.
   - "`WildCardMatch +0x36`" 처럼 함수 이름 **뒤** 숫자 = 그 함수 안 줄.
   - 칸 이름과 붙은 숫자는 그 덩어리 안 자리다. 이번엔 두 가지가 나온다.
     - "노드 `+0x48`" = **장부 줄 안** 칸 (2.11).
     - "스텁 `+0x14`" = **응답 내용물 안** 칸 (2.6, run137 해설 2.12 와 같은 셈).
   - 콜스택으로 본 적은 없다.
4. **이름 몇 개는 우리가 붙인 것이다.**
   - `IFOBJ` · `PSEP` 노드 이름 = 매직 값 (`0x49464f42` · `0x50534550`) 을 글자로 읽어 붙인 **우리 이름** `[이름 추측]`. 공개 PDB 라 구조체 타입 이름이 없다.
   - 상태 `0x16c9a0d6` 의 DCE 이름 (`ept_s_not_registered`) 도 `[이름 추측]`.
   - 요청서의 `RemoteEpmpSecurityCallback` 은 PDB 이름이 **`RemoteEpmpServerSecurityCallback`** 이다 (결과서 정정 · flow-writer 대조 7).
5. **"카드" 와 "안내 데스크" 가 헷갈릴 수 있다.**
   - run 문서 · flow.md 의 비유는 epmapper = "**건물 안내 데스크**", 탑 = "**카드**" 다.
   - 그런데 앞 해설에서는:
     - epmapper = **114 안내** (run129 해설 2.7 부터).
     - "안내 데스크" = 커널 `\\.\Nsi` (run128 해설).
     - "연락 카드" = **바인딩** (run129 해설 2.4, 손님이 손에 든 것).
   - 그래서 이 글은 앞 해설 말을 그대로 쓴다. epmapper 는 **114**, 탑은 그냥 **탑** 이다. 뜻은 run 문서 비유와 같다.
6. **"있음" 답의 숫자 (140 B) 는 예다.**
   - `max_towers = 4` 이고 맞는 탑이 **1개** (nsi 하나, 끝점 23자) 일 때 계산한 값이다.
   - 진짜로 탑이 몇 개 나오는지는 nsisvc 가 든 svchost 의 ncalrpc 주소 수에 달렸다. 그건 `[공백]` 이다 (동적 D4).

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- ALPC 포트 = 프로세스끼리 거는 **전화선 끝** → [run68 해설 2.4](run68-dll-init-rest-해설.md)
- RPC 런타임 (rpcrt4) = 가게 안 **전화 부서** → [run99 해설](run99-rpc-caller-해설.md)
- 간접 호출 = 번호를 보고 이어 주는 **교환원** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md)
- 썽크 = 받은 부탁을 그대로 넘기는 **얇은 다리** → [run136 해설 2.3](run136-winsta-name-해설.md)
- 토큰 = **신분증**, SID = 신분증에 적힌 **신분 번호** → [run141 해설 2.4](run141-async-bind-해설.md) "새 말 — 요구 서버 SID"
- **run129 에서 푼 것** → [run129 해설](run129-winnsi-rpc-해설.md)
  - 바인딩 = **연락 카드**, 끝점 = 상대 사무실 **직통 번호**, `ncalrpc` = "같은 건물 내선" → 2.4
  - 인터페이스 = 사무실 **이름표** (UUID + 판 번호), 프로시저 번호 = **메뉴 번호**, 컨텍스트 핸들 = **접수 번호표** → 2.5
  - epmapper = **114 안내**, `ept_map` = 114 에 거는 "번호 알려 주세요" 질문 → 2.7
- **run133 에서 푼 것** → [run133 해설](run133-lrpc-wire-해설.md)
  - 전송 문법 (NDR 2.0 · NDR64) = **말투** → 2.5
  - 탑의 **층 · LHS / RHS · 프로토콜 ID** (`0x0d` UUID 층 · `0x0c` ncalrpc 층 · `0x10` 끝점 층) → 2.11
  - **동적 끝점** (`LRPC-` + 무작위 18자), SD = **출입 명부**, **인터페이스 콜백** = 문지기 → 2.13
- **run137 에서 푼 것** → [run137 해설](run137-alpc-completion-해설.md)
  - EPM 프로시저 8 의 파라미터 여덟 · 참조 / 고유 포인터 · 포인터 ID → 2.11
  - conformance · 패딩, 응답 칸 차례 (핸들 20 · 탑 수 · 배열 머리 · 포인터 ID · 탑 · 패딩 · 상태) → 2.12

이번에 처음 나오는 말 — **장부 (등록 목록)**, **protseq**, **객체 UUID · nil**, **`TowerExplode`**, **주 · 부 버전 합친 칸**, **CFG 간접 호출 (`⇢`)**, **공개 창구 · 직원 전용 창구 (로컬 인터페이스)**, **`max_towers` · 이어받기 블록**, **서비스 트리거 · 보류**, **바인딩 벡터**, **덮어쓰기 (replace)** — 은 본문에서 처음 나올 때 한두 줄로 푼다. **탑** 은 run129 · run133 에서 풀었지만, 이번 글의 주인공이라 아래에서 처음 읽는 사람 기준으로 다시 짚는다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**, 토큰 = **신분증**.
- run129 ~ run141 의 그림 그대로:
  - winnsi = 전화 신청을 맡기는 **신청 담당**. 번호 칸이 빈 **연락 카드** 를 들고 있다.
  - rpcrt4 = 가게 안 **전화 부서**, ALPC 포트 = **전화선 끝**.
  - epmapper = **114 안내**, nsisvc = **nsi 사무실**.
  - nsi 사무실 번호는 전화국 (rpcrt4) 이 지어 준 **무작위 번호** (`LRPC-` + 18자) 라서, 손님은 114 에 물을 수밖에 없다 (run133 해설 2.13).
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 앞 해설 말로 바꾼 것 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - 사무실들은 문을 열 때 114 에 **자기 탑을 맡긴다**. 114 는 그걸 **장부** 에 꽂아 둔다.
  - 손님이 "nsi 사무실 번호요" 하고 **맨 위 칸이 빈 탑** 을 내밀면, 114 는 장부를 뒤져 맞는 탑을 찾아 **복사해서** 건넨다. 탑을 새로 쓰지 않는다.
  - 114 가 보는 건 **회사 이름 (UUID) · 큰 판 번호 (주 버전) · 작은 판 번호가 손님 것 이상인지 · 전화 방식 (ncalrpc)** 뿐이다. 손님 탑의 **말투 칸 (2층)** 과 **번호 칸 (4층)** 은 안 읽는다.
  - 장부에 없으면 "그런 사무실 등록 안 됐어요" (`0x16c9a0d6`) 만 말한다.
  - 우리 에뮬레이터 114 는 지금 **늘 이 답** 을 한다. 말투는 맞는데, **장부가 비어 있어서** 다.

#### 새 말 — 탑 (tower), 처음 읽는 사람을 위해 다시

- **탑** = 연락처 정보를 **층층이 쌓은 바이트 묶음** `[정적]`. 맨 앞 4 B 가 전체 길이 (`tower_length`), 그다음 2 B 가 층 수, 그 뒤에 층들이 온다.
- 한 층 = `[LHS 길이][LHS][RHS 길이][RHS]`. LHS 첫 바이트가 그 층이 무슨 층인지 알리는 **프로토콜 ID** 다 (run133 해설 2.11).
- nsi 에서 쓰는 탑은 4층이다:

| 층 | 무엇 | 프로토콜 ID |
|---|---|---|
| 1층 | **회사 이름표** — 인터페이스 UUID + 주 · 부 버전 | `0x0d` |
| 2층 | **말투** — 전송 문법 UUID + 버전 | `0x0d` |
| 3층 | **전화 방식** — ncalrpc | `0x0c` |
| 4층 | **번호** — 끝점 이름 | `0x10` |

- 손님이 내는 **질문 탑** 은 4층 번호가 **비어 있다** (70 B, run133 해설 2.11).
- 사무실이 114 에 맡기는 **등록 탑** 은 4층에 번호가 **채워져 있다** (이번에 92 B, 2.10).
- 비유: **4칸짜리 블록 탑**. 손님은 맨 위 칸을 비운 탑을 내밀고, 114 는 맨 위 칸까지 채워진 탑을 돌려준다.

#### 새 말 — 장부 (epmapper 등록 목록)

- **장부** = epmapper 가 들고 있는 **등록 목록** — RpcEpMap 의 전역 목록 `IFObjList` `[정적]`.
- 장부는 두 단으로 되어 있다 (칸 자리는 2.11):
  - **줄 하나 (IFOBJ 노드)** = "사무실 하나" — 객체 UUID · 인터페이스 UUID · 버전 · 주석 · 맡긴 사람 신분증 (등록자 토큰) · 탑 목록 머리 · 마지막 사용 시각.
  - **그 줄에 매달린 칸 (PSEP 노드)** = "탑 하나" — protseq (전화 방식 글자) · 끝점 · **탑 사본**.
- 비유: 114 의 **전화번호부**. 한 줄에 회사 하나, 그 밑에 그 회사가 맡긴 **탑 복사본** 들이 클립으로 꽂혀 있다.

### 한 줄로 먼저 — 무엇을 얻었나

> **114 (epmapper) 의 `ept_map` 은 메뉴 3 · 7 · 8 이 모두 같은 직원이다. 장부 줄은 "UUID 같음 · 주 버전 같음 · 장부 부 버전 ≥ 손님 부 버전 · (장부 객체 UUID 가 nil 이면 무엇이든)" 으로 고르고, 그 줄에 꽂힌 탑 가운데 전화 방식 글자가 "ncalrpc" 와 (대소문자 무시) 같은 것만 낸다. 손님 탑의 2층 (말투) 과 4층 (번호) 은 안 읽는다.**
> **돌려주는 탑은 사무실이 등록할 때 맡긴 바이트를 그대로 복사한 것이다 (`ept_insert_ex +0x274` · `GetEntriesNoLock +0x435` 두 번 `memcpy`). `max_towers` 개까지 채우고, 다 차면 "나머지는 다음에" 블록을 만든다.**
> **맞는 게 없으면 상태 `0x16c9a0d6` · 탑 0 · 핸들 0 → 40 B. 우리 에뮬레이터의 지금 답과 바이트까지 같다. 그러니 벽은 말투가 아니라 빈 장부다.**
> **nsi 의 탑은 nsisvc 가 서비스 시작 때 `RpcEpRegisterW` 로 올린다. rpcrt4 `RegisterEntries` 가 그 프로세스의 바인딩마다 탑 하나 (4층 = 동적 끝점 `LRPC-` + 18자) 를 지어, 114 의 공개 창구가 아니라 **직원 전용 창구** (`0b0a6584…` v1.1 메뉴 2 = `ept_insert_ex`) 로 보낸다.**

- 모두 `[정적]`.
- 단 셋은 `[추측]` 이다: "nsisvc 는 서비스 자동 시작이라 질문보다 먼저 장부에 있다" (서비스 설정은 안 읽음), "4층 이름 = `ServerSetupAddress` 가 지은 그 이름" (마지막 연결 안 읽음), "ncalrpc 호출자는 방화벽 확장에서 로컬로 판정된다" (그 함수 안 안 읽음).

요청서 질문에 대한 답 (run 문서 "요약" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1 고르기 | `WildCardMatch`: UUID 같음 · 주 같음 · **장부 부 ≥ 질문 부** · 장부 객체 nil 이면 무엇이든. protseq `_stricmp`. **전송 문법 · 4층 안 봄** `[정적]` | 2.4 |
| 2 돌려주는 탑 | **등록 탑 사본** (층을 고쳐 쓰지 않음), `max_towers` 까지, 항목 안 차례 = 등록의 거꾸로. 핸들 = 넘치면 이어받기 블록 · `max_towers ≤ 1` 이면 −1 · 그 밖 0 `[정적]` | 2.5 · 2.6 · 2.7 |
| 3 없을 때 | `0x16c9a0d6` · 탑 0 · 핸들 0 → **40 B = 에뮬레이터 지금 답**. 트리거 키가 있으면 보류 `[정적]` | 2.8 |
| 4 부른 쪽 검사 | 호출자 PID · 이미지 · 토큰 비교 **없음**. SID (선택) · 조건부 인터페이스 콜백뿐 `[정적]` | 2.9 |
| 5 등록 쪽 | 바인딩마다 탑 하나 (1 · 2층 = 서버 인터페이스, 4층 = 동적 끝점), 로컬 `0b0a6584` v1.1 프로시저 2 `ept_insert_ex` 로 `[정적]` | 2.10 · 2.11 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run128 ~ run141 과 같은 자리, **초기화 밖** 이다. `main` 뒤 프로그램이 웹을 쓸 때의 옆길이다. 이번엔 그 옆길이 **다른 가게 둘** 로 뻗는다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 RPC 를 안 쓴다 [소스]          │ 웹을 쓰는 프로그램이라면 (요청 쪽 에뮬레이터 로그):
                                                        └─ WinHttpSendRequest                                       (run128)
                                                             └─ 신청 담당 winnsi → 전화 부서 rpcrt4 → 114 에 전화       (run129 · run141)
                                                                  ├─ 첫인사 · 질문 탑 · 답 검사 (묻는 쪽)               (run133 · run137)
                                                                  │
      ── 다른 가게: svchost (RPCSS) 안 114 ──────────────────────┼──────────────────────────────
                                                                  ├─ 114 가 장부에서 골라 탑을 채우는 법                ← 이번 (답하는 쪽)
      ── 다른 가게: nsisvc 가 든 svchost ────────────────────────┼──────────────────────────────
                                                                  └─ nsi 사무실이 문 열 때 114 장부에 탑을 올리는 법    ← 이번 (등록 쪽)
```

- 등록은 **부팅 뒤 서비스가 시작될 때**, 질문은 **손님이 웹을 쓸 때** 다. 시간으로는 등록이 먼저다 (2.10, 서비스 자동 시작은 `[추측]`).

### 왜 이 질문이 지금 왔나

PE_emulator 쪽에서 막혔다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님).

- run141 까지로 비동기 114 연결 → bind 까지는 길이 열렸다.
- 그다음 **`ept_map` (프로시저 8) 답** 에서 막혔다.
  - 에뮬레이터 114 는 늘 "탑 0개 + `0x16c9a0d6`" (`epm.cpp:13 ~ :28`, 늘 40 B).
  - → rpcrt4 가 `0x6d9` 로 바꿈 → `WinHttpSendRequest` FALSE → 되풀이 (60초에 579번).
- 진짜 114 가 답을 **어떻게 고르고 채우는지** 근거가 없어서, 장부를 지을 수 없었다 (규칙: 이름 · 값을 지어내지 않음).
- 원문 물음 셋 가운데 **물음 1 (정적) 만** 먼저 했다. 물음 2 · 3 (VM) 은 Q280.

### 무엇이 궁금했나

요청서의 다섯 가지:

1. **고르기** — 장부에서 맞는 줄을 무엇으로 고르나. UUID · 주 / 부 버전 (부 버전 크기 규칙) · 전송 문법 · 3층 `0x0c`.
2. **돌려주는 탑** — 등록 탑 그대로인가, 다시 짓나. 여럿이면 차례 · 수. 끝점 핸들이 0 이 아닌 때. 배열 머리.
3. **없을 때** — 출력이 에뮬레이터 지금 답 (40 B) 과 같은가.
4. **부른 쪽 검사** — SID · 보안 콜백 · 호출자 확인이 있나.
5. **등록 쪽** — `RpcEpRegisterW` 가 넣는 탑 (4층 이름 · 다른 끝점 · 주석), 114 쪽 등록 함수가 장부에 넣는 꼴.

### 바퀴 전에 알던 것

- `[정적]` (run129) nsi 인터페이스 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` v1.0. `FatalLookupErrorEncountered` 가 `0x16c9a0d6` → `0x6d9` 로 바꾼다.
- `[정적]` (run133) 손님 쪽:
  - 질문 탑 **4층 70 B** (4층 비움), `LookupNextChunk +0x11d` 가 프로시저 8 을 `max_towers` **4** 로 부른다.
  - 답에서 보는 건 층 수 · 3층 ID · 4층 이름뿐 (`ProcessNextChunk`).
  - nsisvc 는 `NsiStartRpcServer` → `RpcEpRegisterW` 로 번호를 올리고, 번호는 `ServerSetupAddress` 가 지은 `LRPC-` + 18자.
  - 그리고 3-3 의 짐작: "진짜 epmapper 가 **등록 때 받은 탑을 그대로 돌려준다**" — `[추측]`.
- `[정적]` + `[규칙]` (run137) 프로시저 8 응답 칸 차례. 탑 0개면 40 B — 다만 **서버가 그 값을 정한다는 근거는 없어** `[규칙]` 이었다.
- `[정적]` (run141) 비동기 114 연결 → bind 까지.
- 몰랐던 것: **114 안쪽 전부** — 장부 생김새, 고르는 규칙, 탑을 짓나 복사하나, 핸들 값, 등록 길.

---

## 2. 찾은 것

### 2.1 한눈에 — 질문 받는 길 · 등록 받는 길 `[정적]`

run 문서 "호출 관계" 를 줄였다. 위가 **질문** (svchost RPCSS 안), 아래가 **등록** (nsisvc 가 든 svchost → RPCSS).

```
(질문) rpcrt4!LookupNextChunk +0x11d (프로시저 8)  ── ALPC ──▶  (svchost RPCSS)
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

nsisvc!NsiStartRpcServer
 ├─ +0x2e5  RpcServerInqBindings                    그 프로세스 서버 주소 전부
 └─ +0x33e  rpcrt4!RpcEpRegisterW(nsi, 벡터, NULL, "NSI server endpoint")
            └─ … RegisterEntries                    항목 0x58 B × 바인딩 수
                     ├─ +0x337  BindingAndIfToTower   (바인딩마다) 1 · 2층 = 서버 인터페이스, 4층 LRPC-…  92 B
                     └─ +0x29b  NdrClientCall3(0xe1500, 2)  ── ALPC ──▶ RpcEpMap!ept_insert_ex
                                                                         └─ +0x279 memcpy  탑 사본 (목록 앞에)
```

- `⇢` = CFG 간접 호출 (2.2 줄 ①), `── ALPC ──▶` = 다른 프로세스로 가는 RPC. 콜스택으로 본 적 없음.
- 두 `memcpy` 를 눈여겨보자. 아래 (등록) 에서 **장부에 복사해 넣고**, 위 (질문) 에서 **장부에서 복사해 낸다.** 이번 바퀴의 핵심이 이 둘이다 (2.5).

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `│ ├─ +0x197  ⇢ WildCardMatch   UUID · 주 같음 · 부 장부 ≥ 질문 · 객체 nil`

- **누가**: `RpcEpMap!GetEntriesNoLock` (rva `0x1500`) — 장부를 한 줄씩 훑는 함수.
- **어디서**: call `+0x192`, ret `+0x197`.
  - 그런데 `+0x192` 는 `WildCardMatch` 를 **직접** 부르지 않는다. `CALL 0x180013010` 이다 (`_calls.tsv` 표시 `indirect:cfg:?`).
- **새 말 — CFG 간접 호출 (`⇢`)**: 함수 주소를 **값으로 받아** 부를 때, 곧장 뛰지 않고 "이 주소가 진짜 함수 입구가 맞나" 를 확인하는 **관문 함수를 거쳐** 부르는 꼴 — 일반적으로 알려진 설명 `[추측]`. 코드에서 보이는 건 `0x180013010` 을 부른다는 것까지다 `[정적]`.
  - 비유: 교환원 (간접 호출) 앞에 **경비** 가 하나 더 서 있다. 경비가 "그 번호 진짜 있는 번호네" 하고 통과시킨다.
- **누구를**: 비교 함수 = `GetEntries` 에 넘겨 받은 함수 포인터. 넘긴 곳은 `ept_map_auth_sd +0x268 LEA RDX,[0x180001240]` → `+0x275 MOV [RSP+0x68],RDX` — 즉 **`WildCardMatch` (rva `0x1240`)** 다 `[정적]` (flow-writer 대조 3).
- **무엇을 위해**: 장부 줄 하나가 손님 질문과 **맞는지** 묻는다. 반환 **0 = 맞음**.
- 비유: 114 직원이 전화번호부를 한 줄씩 넘기면서, 줄마다 "이 회사 맞나?" 판정표 (2.4) 를 대 본다.

#### 줄 ②: `└─ +0x29b  NdrClientCall3(0xe1500, 2)  ── ALPC ──▶ RpcEpMap!ept_insert_ex`

- **누가**: rpcrt4 `RegisterEntries` (rva `0x62934`) — **nsisvc 가 든 svchost 안** 에서 돈다.
- **어디서**: call `+0x296`, ret `+0x29b`. 앞뒤로 `+0x22c` · `+0x2d5` 에서 `EpContextMutex` 를 잡고 푼다.
- **누구를**: `NdrClientCall3` 에 프록시 `0xe1500` · 프로시저 **2** 를 준다.
  - 프록시 `0xe1500` 는 rpcrt4 안 **`0b0a6584-9e0f-11cf-a3cf-00805f68cb1b` v1.1** 클라이언트 구조체 (`0xe14a0`) 의 것이다 (`pefile`).
  - 그 인터페이스의 프로시저 2 = 114 쪽 **`ept_insert_ex`** (rva `0x22a0`) `[정적]`.
- **무엇을 위해**: "이 항목들 (객체 UUID · 탑 포인터 · 주석, 0x58 B × 바인딩 수) 을 장부에 넣어 주세요".
- 손님 질문 (공개 `e1af8308`) 과 **다른 인터페이스** 로 간다는 게 포인트다 (2.3).

### 2.3 114 엔 창구가 둘 — 그리고 메뉴 3 · 7 · 8 은 같은 직원 `[정적]`

run 문서 1절. 서버 인터페이스 표 · 디스패치 표는 `pefile` 로 직접 읽었다.

#### 새 말 — 공개 창구 · 직원 전용 창구

| 창구 (인터페이스) | 메뉴 → 함수 |
|---|---|
| **공개** `e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0 (질문이 오는 곳) | **3 = `ept_map`** · **7 = `ept_map_auth`** · **8 = `ept_map_auth_async`** · 2 = `ept_lookup` · 4 = `ept_lookup_handle_free` · **0 · 1 · 5 · 6 = 예외 `0x6d8` 만 던지는 16 B 몸** (`0x83c0`) |
| **직원 전용 (로컬)** `0b0a6584-9e0f-11cf-a3cf-00805f68cb1b` v1.1 (등록이 오는 곳) | 0 `OpenEndpointMapper` · 1 `AllocateReservedIPPort` · **2 `ept_insert_ex`** · **3 `ept_delete_ex`** · 4 `SetRestrictRemoteClients` · 5 `ResetWithNoAuthException` · 6 `ReRaiseInterfaceTriggers` |
| 디버그 `1d55b526-c137-46c5-ab79-638f2a68e869` v1.0 (13개) | 이름만 |

- `0x83c0` 은 같은 주소에 이름이 넷 붙어 있다 (`ept_insert` · `ept_delete` · `ept_inq_object` · `ept_mgmt_delete`, 별칭 무리). 어느 이름이든 하는 일은 **"예외 `0x6d8` 던지기"** 하나다 → **공개 창구로는 등록 · 삭제가 안 된다.**
- 직원 전용 창구는 **ncalrpc 로만** 받는다. 문지기 `LocalEpmpSecurityCallback` 이 전송 종류 ≠ 4 이면 5 를 돌려준다 (`+0x25 SUB EAX,4` … `+0x2c AND EAX,5`, 조회 실패도 `+0x35 MOV EAX,5`).
- 비유: 114 엔 **손님 창구** 와 **건물 안 직원 전용 창구** 가 따로 있다. 손님 창구에서 "번호 올려 주세요" 하면 "여긴 안 돼요" 만 듣는다. 번호 올리기는 같은 건물 내선 (ncalrpc) 으로만 받는 직원 창구 2번이다.

#### 메뉴 3 · 7 · 8 은 한 몸

- 셋 다 서버 썽크가 **비동기 `NdrAsyncServerCall`** (`0x72c0`) 이다. 나머지는 `NdrServerCall2` (`0x72f0`).
- 사슬:
  - 8: `ept_map_auth_async +0x4e` → `ept_map_auth` — 인자 10개 그대로.
  - 3: `ept_map +0x43 MOV [RSP+0x20],0` (SID 칸 NULL) → `+0x4c` → `ept_map_auth`.
  - 7 = `ept_map_auth` 자신.
  - 그다음 공통: `ept_map_auth +0x1ee` → `ept_map_auth_sd` → `+0x2c2 GetEntries` (공유 잠금 `RtlAcquireSRWLockShared`) → `GetEntriesNoLock`.
- 응답도 같은 호출 안에서 나간다: `ept_map_auth_sd +0x1b8 RpcAsyncCompleteCall` (`+0x1ae TEST R12D,R12D` 가 참 = 보류 안 한 모든 길). 보류하는 한 길만 예외다 (2.8).
- 비유: 메뉴 번호는 셋이지만 **받는 직원은 한 사람**. 3번 손님은 "신분 번호 칸 (SID)" 을 비워서 7번 직원에게 넘기고, 8번은 받은 걸 그대로 7번에게 넘긴다.
- 에뮬레이터 쪽 뜻: `epm.cpp :40 ~ :41` 이 "3 · 7 은 공사 중" 인데, 열 때 **같은 장부 함수에 붙이면 된다** (요청 NDR 꼴만 다름 — 3 · 7 은 `FC_FP` 포인터 ID, run137 해설 2.11).

### 2.4 ★ 고르기 — 무엇을 견주고 무엇을 안 보나 `[정적]`

run 문서 2절. 요청서 물음 1 의 답이다.

#### 먼저 — 질문 탑을 푸는 곳

- **새 말 — `TowerExplode`**: rpcrt4 의 **탑을 층별로 풀어 꺼내는** 함수. 인자는 6개 — `(탑, &인터페이스, &전송 문법, &protseq, &끝점, &네트워크 주소)` `[정적]`.
  - 뒤의 두 칸에 NULL 을 주면 그 둘은 **안 꺼낸다**.
  - (Ghidra `.c` 에선 인자가 4 · 3개로 줄어 보이지만 실제는 6개 — 결과서 "스크립트 문제" 메모.)
- 114 는 `ept_map_auth_sd +0x102` 에서 `TowerExplode(질문 탑, &인터페이스, &전송 문법, &protseq, NULL, NULL)` 로 부른다. 끝점 · 네트워크 주소 칸은 `+0xe9` · `+0xf2` 에서 NULL 로 채운다.
- **새 말 — protseq** (protocol sequence) = 전화 방식을 **글자로** 쓴 것 (`"ncalrpc"` 등) `[정적]`.
  - 탑 3층은 이걸 **숫자** (`0x0c`) 로 적고, 장부의 탑 칸 (PSEP) 은 **글자** 로 들고 있다.
  - `TowerExplode` 가 3층 `0x0c` 를 `"ncalrpc"` 로 바꿔 준다 (`TowerExplode +0x16b ~ +0x180`).
  - 비유: 탑 3층에 적힌 코드 "0x0c" 를 "내선" 이라는 **글자** 로 바꿔서, 전화번호부에 적힌 글자와 대 본다.

#### 새 말 — 주 · 부 버전을 한 칸으로

- 인터페이스 판 번호는 **주.부** 꼴이다 (nsi = v1.0).
- 114 는 이 둘을 `주 << 16 | 부` 한 칸으로 합친다 (`ept_map_auth_sd +0x11c ~ +0x129`) `[정적]`. 위 16비트 = 주, 아래 16비트 = 부.
- 그래서 "주 같음" 은 위 16비트만 견주고, "부 ≥" 는 합친 값 크기를 견주면 된다.

#### 견주기 표 (run 문서 2절 줄임)

| 견주는 것 | 규칙 | 줄 |
|---|---|---|
| 인터페이스 UUID (질문 1층) | **같아야** (16 B) | `WildCardMatch +0x4 ~ +0x15` (노드 `+0x48` · `+0x50`), 아니면 `+0x1a` 1 |
| 주 버전 | **같아야** — 합친 칸의 위 16비트 | `WildCardMatch +0x25 ~ +0x34` (`+0x2f TEST EAX,0xffff0000`) |
| **부 버전** | **장부 ≥ 질문** (합친 값 부호 없는 비교, 장부가 작으면 안 맞음) | `WildCardMatch +0x36 CMP R8D,R9D` · `+0x39 JC` |
| 객체 UUID | 장부가 **nil 이면 무엇이든 맞음**, 아니면 요청과 같아야. 요청 포인터가 NULL 이면 nil 로 (`LocalNullUuid`) | `WildCardMatch +0x3b ~ +0x59` · `+0x6b UuidIsNil` · `+0x79 SETZ`; `ept_map_auth_sd +0xcb ~ +0xd2` |
| **3층 → protseq** | 질문 3층 `0x0c` → `"ncalrpc"` (층 수 **정확히 4**, 아니면 `0x16c9a0cd`). 장부 탑 protseq 와 **`_stricmp` (대소문자 무시) 로 같아야** | `TowerExplode +0x1a6 CMP R9D,4`; `GetEntriesNoLock +0x2dd` |
| **전송 문법 (질문 2층)** | **안 봄** | `ept_map_auth_sd +0x99` (초기화) · `+0xf7` (넘김) 뿐 |
| **4층 (끝점)** | **안 봄** | `TowerExplode +0x1b9 TEST RSI,RSI` · `+0x1bc JZ → +0x265` |
| SID (5번째 인자) | NULL 이면 안 봄 (2.9) | `ept_map_auth +0x2f ~ +0x32` |
| 장부 탑 자체 | 검사 없이 복사 (2.5) | `GetEntriesNoLock +0x3e2 ~ +0x435` |

- 3층 `0x0a` / `0x0b` 는 TCP 갈래 (`OsfTowerExplode`), 그 밖은 `0x16c9a0cd` 다.

#### 새 말 — 객체 UUID · nil

- **객체 UUID** = 인터페이스 이름표와 따로, "같은 사무실 안 어느 **물건**" 을 가리키는 두 번째 이름표 — 일반적으로 알려진 설명 `[추측]`. 코드에선 장부 줄 `+0x38` 칸이고, 요청 객체 UUID 와 견준다 `[정적]`.
- **nil** = 16 B 가 전부 0 인 UUID.
- nsi 는 등록할 때 UUID 벡터를 NULL 로 줘서 **nil 하나** 로 들어간다 (2.10). → 장부가 nil 이니 손님이 어떤 객체 UUID 를 줘도 맞는다.

#### 왜 "장부 부 ≥ 질문 부" 인가

- 코드가 하는 일은 위 표 그대로다 `[정적]`.
- 왜 그런 규칙인지는 코드에 없다. 일반적으로 알려진 설명 `[추측]`: 부 버전은 **옛 손님을 깨지 않게 늘린 판** 이라, 큰 부 버전 서버는 작은 부 버전 손님도 받을 수 있다는 약속이다.
- 예 (위 규칙을 그대로 적용한 것): 장부 nsi v1.0, 손님 v1.0 → 같으니 맞음. 손님이 v1.1 을 물으면 장부 1.0 < 1.1 이라 안 맞는다.

#### 2층 · 4층을 안 본다는 건 — 단계로

1. 근거: `TowerExplode` 의 끝점 칸이 NULL → `+0x1b9 TEST RSI,RSI` · `+0x1bc JZ → +0x265` 로 4층을 읽지 않고 0 을 돌려준다 (층 수 4 만 확인) `[정적]`.
2. 근거: 2층은 `[RBP-0x10]` 에 풀어 두지만, `ept_map_auth_sd.asm` 에서 그 칸은 `+0x99` (초기화) · `+0xf7` (넘김) 두 줄뿐이고 `GetEntries` 인자에도 없다 `[정적]`.
3. 그래서: 손님 탑 2층에 NDR 2.0 을 쓰든 NDR64 를 쓰든, 4층에 뭘 적든 **답이 같다**.
4. 손님 쪽 (run133) 도 답 탑의 1 · 2층 값은 안 읽었다. 그러니 2층은 **양쪽 다 안 보는 칸** 이다.

#### 훑는 차례

- 장부 `IFObjList` 를 **앞에서부터** (`GetEntriesNoLock +0x121`).
- 맞는 줄마다 그 줄의 탑 목록 전체 (`+0x2b0 [RBX+0x20]` → `+0x47a` 다음 탑) 에서 protseq 같은 것을 하나씩.
- 탑 수가 `max_towers` 에 닿으면 (`+0x39b` · `+0x39d JNC → +0x657`) 나머지는 이어받기 블록으로 (2.7).
- 질문 탑 풀기가 실패하면: 상태 = 그 코드 (`0x16c9a0cd` 등) · 탑 수 0 · 핸들 안 건드림, 호출은 정상 끝 (`ept_map_auth_sd +0x110` · `+0x116`).
- (덤) 뜻 모를 검사 하나: 객체 UUID 포인터 == 탑 포인터면 `0x6d8` (`ept_map_auth_sd +0xd6` · `+0xdb`) — `[추측: 정상 호출엔 안 걸림]`.

비유: 114 직원의 판정표는 네 줄 — "회사 이름 같나 · 큰 판 번호 같나 · 우리 판이 손님 판 이상인가 · (회사가 물건 구분을 안 하면 통과)" — 그리고 "전화 방식 글자 같나". 손님 탑의 2층 (말투) 과 4층 (번호 칸) 은 쳐다보지도 않는다.

### 2.5 ★ 돌려주는 탑 = 등록 때 맡긴 바이트 그대로 `[정적]`

run 문서 3절 앞. 요청서 물음 2 의 앞 절반, 그리고 run133 3-3 의 `[추측]` 이 닫힌 곳이다.

#### 두 번의 `memcpy` — 한 걸음씩

1. **등록 때** (`ept_insert_ex`, 2.11):
   - `+0x26d MOV R8D,[RDX]` (등록 탑의 `tower_length`) · `+0x270 ADD R8,4` → 길이 = `tower_length + 4` B.
   - **`+0x274 memcpy`** — 그만큼을 장부 탑 칸 (PSEP 노드 `+0x28`) 에 복사한다.
2. **질문 때** (`GetEntriesNoLock`):
   - `+0x3e2 MOV RAX,[RDI+0x28]` — 장부 탑 칸을 꺼낸다.
   - `+0x40f HeapAlloc` — 새 자리를 잡는다.
   - **`+0x435 memcpy`** — 장부 탑을 그대로 복사한다.
   - `+0x449 MOV [RDX],RAX` — 그 사본 포인터를 응답 배열에 놓는다.
   - `+0x478 INC` — 탑 수 +1.
3. 그 사이에 층 바이트를 **고쳐 쓰는 줄이 없다**.
   - 예외 하나: protseq 가 `ncacn_ip_tcp` · `ncacn_http` 인 탑은 `+0x4b` 의 4 B 를 덮어쓸 수 있다 (`+0x44c JNZ → +0xb7f`, `+0xb83 MOV [RAX+0x4b],ECX`). ncalrpc 가 아니라 이번 길과 무관하다. 그 4 B 의 뜻은 `[추측: TCP 탑 주소 칸]`.

#### 그래서 무엇이 정해지나

- 답 탑의 **1 · 2층도 등록한 쪽 인터페이스** 에서 온 바이트다 (2.10). 손님 탑에서 가져오지 않는다.
  - 손님 2층이 NDR 2.0 이든 NDR64 든 **같은 탑**.
  - 1층 버전도 **등록 때 버전**.
- 덤: `+0x461 GetTickCount64` → `+0x46d [노드+0x98]` 에 "마지막 사용 시각" 을 적는다. 응답 바이트와는 무관하다.

#### 차례 — 맨 앞은 "마지막에 꽂은 것"

- 장부 줄 차례 = `IFObjList` 차례.
- 한 줄 안의 탑 차례 = **등록 벡터의 거꾸로** — `ept_insert_ex +0x279 MOV [RBX],RDI` (새 탑 다음 = 옛 머리) · `+0x27c` (머리 = 새 탑). 새 탑을 늘 **목록 맨 앞** 에 끼운다.
- → 응답 맨 앞 = 벡터의 **마지막** ncalrpc 탑.
- 손님 (run133 3-2) 은 원하는 수 (이름 하나) 가 차면 멈춘다. 그러니 손님은 **첫 탑의 이름을 쓴다**.

비유: 114 는 맡겨 둔 탑을 **복사기로 복사해서** 건넨다. 손님이 내민 탑에선 아무것도 베끼지 않는다. 같은 회사 탑이 여러 장이면, 맨 나중에 꽂은 게 맨 앞에 있다.

### 2.6 응답 칸 — 있음 140 B · 없음 40 B `[정적]` + `[규칙]`

run 문서 3절 표. 칸 차례와 뜻은 run137 해설 2.12 그대로다. 이번엔 **"누가 그 값을 쓰나"** 가 붙었다.

조건: 프로시저 8, NDR 2.0, `max_towers = 4`, 맞는 탑 1개 (nsi 하나, 끝점 23자).

| 칸 (스텁 오프셋) | 있음 | 없음 | 쓰는 줄 |
|---|---|---|---|
| 끝점 핸들 20 B (`+0x00`) | **0** | **0** | `GetEntriesNoLock +0x4c3 MOV [RAX],RDI` (`max_towers ≥ 2` 일 때, 2.7) |
| `num_towers` (`+0x14`) | 1 | 0 | `+0x8e MOV [R9],EDI` (처음 0) · `+0x478 INC` (탑마다) |
| 배열 `max_count · offset · actual_count` (`+0x18`) | `4 · 0 · 1` | `4 · 0 · 0` | `[규칙]` — 서버 코드엔 쓰는 줄 없음, `actual_count` = 위 탑 수 |
| 원소 포인터 ID (`+0x24`) | 0 아닌 값 | 없음 | `[규칙]` (탑 포인터를 놓는 줄 `+0x449`) |
| 탑 (`+0x28 ~`) | conformance `0x58` · `tower_length` `0x58` · 88 B = **96 B** | 없음 | `+0x3e2 → +0x435` (등록 탑 사본) |
| 패딩 | 0 B (88 은 4 의 배수) | 없음 | `[규칙]` |
| 상태 (있음 `+0x88` / 없음 `+0x24`) | **0** | **`0x16c9a0d6`** | 있음 `+0x4c6` → `ept_map_auth_sd +0x2c7`. 없음 `+0x867` · `ept_map_auth_sd +0x38d` |
| **합** | **140 B (`0x8c`)** → 응답 머리 `TotalLength = 0x40 + 0x8c = 0xcc` | **40 B** | |

- 140 = 20 + 4 + 12 + 4 + 96 + 0 + 4, 40 = 20 + 4 + 12 + 4 (결과서 표 1).
- **한 줄 읽기 — 탑 칸 96 B**: 탑 본체 88 B (`tower_length` `0x58`) 앞에 conformance (`0x58`) 와 `tower_length` (`0x58`) 4 B 씩이 붙는다. 88 은 4 의 배수라 뒤 패딩이 없다 `[규칙]`. 그래서 상태가 `+0x28 + 96 = +0x88` 에 온다.
- run137 해설 2.12 의 128 B 표는 `LRPC-emu` (8자, 탑 octets 73, 패딩 3) 로 든 **예** 였다. 진짜 nsi 이름 (23자) 이면 140 B 다.

### 2.7 끝점 핸들이 0 이 아닌 때 `[정적]`

run 문서 3절 "핸들이 0 이 아닌 때".

#### 새 말 — `max_towers` · 이어받기 블록

- **`max_towers`** = 손님이 "탑을 **최대 몇 장까지** 받을게요" 라고 적어 내는 수. 우리 손님 (rpcrt4) 은 **4** (run133).
- **이어받기 블록** = 맞는 탑이 `max_towers` 보다 많을 때, **못 준 나머지를 기억해 두는 0x20 B 메모** (`GetNewContext`, 매직 `0xfafafafa`) `[정적]`. 그 포인터가 끝점 핸들 칸으로 나간다.
- 비유: "나머지는 다음에 오시면 드릴게요" **번호표**. 손님이 이 번호표 (핸들 ≠ 0) 를 받으면 같은 질문을 또 한다 (run133 3-2). 핸들 0 이면 "더 없음" 으로 끝난다.

#### 경우 넷 (+ 하나)

1. 맞는 탑이 `max_towers` 를 **넘음** → 이어받기 블록 포인터 (`+0x39d JNC → +0x657`, `+0x875 GetNewContext`).
2. 이어받는 호출에서 다 비우면 → 블록을 풀고 `(max_towers > 1) ? 0 : −1` (`+0x837 CMP R15D,1` · `+0x840 SETA` · `+0x843 DEC` · `+0x846`).
3. **`max_towers ≤ 1` 이면 블록이 없어도 −1** (`+0x4b6 CMP EAX,1` · `+0x4bd JBE → +0xa36 MOV qword [RAX],-1`).
4. 들어온 핸들이 −1 이면 → 0 으로 바꾸고 탑 0 · `0x16c9a0d6` (`+0x635 CMP RCX,-1` → `+0xa29 ~ +0xa31`).
5. (덤) `max_towers = 0` 이면 `+0x852 JZ → +0x8a8` 로 가서 상태 0 · 핸들 −1.

- 그 밖 (`max_towers ≥ 2` 이고 넘치지 않음) → **0** (`+0x4c3`).
- 이 갈래 앞에 두 줄이 더 있다 (flow-writer 대조 5): `+0x4ab CMP ECX,EAX` · `+0x4ad JA → +0x4c6` (핸들 안 건드림, `ECX` 뜻은 안 따라감) · `+0x4af CMP [RBP-0x78],0` · `+0x4b4 JNZ → +0x4c6` (블록이 있으면 `+0x885` 에서 이미 씀).
- −1 이 **선 위에서 어떤 20 B 가 되는지** 는 rpcrt4 서버 쪽 컨텍스트 핸들 마샬링을 안 읽어 `[공백]`.
- 우리 손님은 `max_towers = 4` 라 3 · 4 · 5 길을 안 탄다. 탑을 **4개 이하** 로 주면 핸들 0 으로 한 번에 끝난다.

### 2.8 ★ 맞는 게 없을 때 — 두 길 `[정적]`

run 문서 4절. 요청서 물음 3 의 답이다.

#### 평범한 길 — 한 걸음씩

1. `GetEntriesNoLock` 이 탑을 하나도 못 넣으면 `+0x49f JZ → +0x84b`.
2. `max_towers ≠ 0` (`+0x852`), 그때까지 오류 없음 (`+0x854 TEST EDX` · `+0x856 JNZ`), 거부 표시 아님 (`+0x85c CMP [RSP+0x48],5`).
3. **`+0x867 MOV EDX,0x16c9a0d6`**.
4. `+0x4a5` 로 돌아가 핸들을 정하고 (2.7) `+0x4c6` 에서 반환.
5. `ept_map_auth_sd` 가 `+0x2c7` 에서 한 번, 트리거 키가 없으면 `+0x38d` 에서 또 한 번 `0x16c9a0d6` 을 상태 칸에 쓴다.

- 거부 (2.9 `AccessCheck`) 가 한 번이라도 있었으면 상태는 같고 **표시 칸만 1** (`+0xbd8`, SID 를 준 호출만).
- 결과 = 2.6 표 "없음" 열, **40 B**.
  - `epm.cpp` 의 `reply(0x28)` · `+0x18 = max_towers` · `+0x24 = 0x16c9a0d6` (나머지 0) 과 **바이트까지 같다**.
- 상태 이름: rpcrt4 가 `0x16c9a0cd ~ +9` 범위를 `0x6d9` 로 바꾼다 (`FatalLookupErrorEncountered +0x26 SUB EAX,0x16c9a0cd` · `+0x2d CMP EAX,9` · `+0x32 MOV EBX,0x6d9`). `0x16c9a0cd` 자체는 `+0x87` 에서 `0x6d8` (run129 4절과 같은 함수).

#### 다른 길 — 서비스 트리거 키가 있으면 "보류"

- **새 말 — 서비스 트리거 · 보류**:
  - 서비스 트리거 = "누가 이 인터페이스를 찾으면 그때 서비스를 깨워 주세요" 라는 레지스트리 등록 — 일반적으로 알려진 설명 `[추측]`.
  - 코드에서 보이는 것 `[정적]`: 키가 있으면 답을 바로 안 보내고 요청을 **대기열에 넣어 둔다** (보류). 그 인터페이스가 등록될 때 풀린다.
- 한 걸음씩:
  1. 조건: 상태 `0x16c9a0d6` · 핸들 0 (`+0x2da`) · 거부 표시 0 (`+0x2e3`) · `g_TriggersDisabled == 0` (`+0x2ec`).
  2. `CreateIfIdString` 으로 `"<인터페이스 UUID>[:<객체 UUID>]"` 글자를 만든다.
  3. `+0x346 RtdsTriggerExists` — 키 경로 `HKLM\Software\Microsoft\Windows\CurrentVersion\NetworkServiceTriggers\Triggers\bc90d167-9470-4139-a9ba-be0bbbf5b74d\<…>` (`bc90d167…` = `RPC_INTERFACE_EVENT_GUID`). 객체 쪽을 잘라 한 번 더 (`+0x376`).
  4. 키가 있으면 잠금 (`+0x39d`) → 다시 찾기 (`+0x40c`) → 그래도 없으면 `+0x4e5 InitializeBufferedRequest` · `+0x502 NtrbEnqueueRequest`.
  5. **호출을 끝내지 않는다** (`+0x516` → `RpcAsyncCompleteCall` 안 부름). 인터페이스별 한도를 넘으면 `0x6d8` (`+0x45c`).
  6. 풀림: 그 인터페이스가 등록될 때 `ept_insert_ex +0x50c CompletePendingEprRequests`.
- 이 VM 레지스트리에 nsi UUID 키가 있는지는 안 봤다 `[공백]` (동적 D6). 없으면 평범한 길이 전부다.
- 비유: "그 사무실 아직 문 안 열었는데, 열기로 **예약** 돼 있네요. 끊지 말고 기다리세요 — 열리면 바로 알려 드릴게요."

### 2.9 부른 쪽 검사 — 손님 얼굴은 안 본다 `[정적]`

run 문서 5절. 요청서 물음 4 의 답이다.

- **호출자 PID · 이미지 · 토큰 비교 없음.**
  - `RpcServerInqCallAttributesW` 는 `GetEntriesNoLock +0x54e` 한 곳 (방화벽 확장 갈래) 뿐.
  - `RpcImpersonateClient` 는 `ept_insert_ex` (등록) 에만 있다.

#### SID (5번째 인자) — NULL 이면 검사 전부 건너뜀

- `ept_map_auth +0x27 MOV RDI,[RSP+0x90]` · `+0x2f TEST` · `+0x32 JZ → +0x9e` — SID 가 NULL 이면 SD 도 NULL, 검사 없음.
- 우리 손님은 **NULL** 을 준다 (run133 3-1).
- SID 가 있을 때 (줄임):
  1. `IsValidSid` (`+0x37` · `+0x7c` 두 번).
  2. 그 SID 로 **출입 명부 (SD)** 를 짓는다 — SD 0x28 B `HeapAlloc` (`+0x59`) · `InitializeSecurityDescriptor` (`+0xb7`) · ACL (`+0xdf I_RpcAllocate`) · `AddAccessAllowedAce(…, 2, 0x20000, SID)` (`+0x128`) · DACL · 그룹 · 소유자 (`+0x149 · +0x166 · +0x183`).
  3. 실패 값: SD `HeapAlloc` 실패 → 예외 **`0xe`** (`+0x73 JZ → +0x276`). SD 채우기 실패 → `EDI = 0x16c9a0cd` → … → 예외 **`0x6d8`** (`+0x91`). 자세한 갈래는 run 문서 flow-writer 대조 4.
  4. `ept_map_auth_sd +0xa2 IsValidSecurityDescriptor` (실패 `0x57`).
  5. 장부 줄을 고른 뒤 **맡긴 사람 신분증** (등록자 토큰 `[노드+0x18]`, 등록 때 `ept_insert_ex +0x308 GetCurrentUserToken` → `+0x418`) 을 그 명부에 대 본다 — `AccessCheck(…, 0x20000, …)` (`GetEntriesNoLock +0x1f2`). 거부면 그 줄만 건너뛰고 표시 (`+0x21f` · `+0x223`).
- 비유: 손님이 신분 번호 (SID) 를 적어 내면, 114 는 그 번호로 출입 명부를 만들고, 장부 줄마다 **맡긴 사람의 신분증** 을 명부에 대 본다. 우리 손님은 그 칸을 비우니 아무것도 안 대 본다.

#### 인터페이스 콜백 (문지기) — 조건부로만 선다

- 공개 창구의 문지기 `RemoteEpmpServerSecurityCallback` (`0x5a30`) 은 `I_RpcSystemFunction001(4, …)` 이 0 이 아닐 때만 붙는다 (`StartEndpointMapper +0x151 CMP` · `+0x157 LEA` · `+0x163` 깃발 `0x10`; 0 이면 `+0x169` 콜백 NULL).
- 붙었을 때 하는 일:
  - `g_fOpenEpmpInterface ≠ 0` 이면 허용 (`+0x4` · `+0xd`).
  - 아니면 `I_RpcServerCheckClientRestriction` → 0 이 아니면 `EPMAllowAnonymous == 3` 일 때만 허용 (`+0x31 ~ +0x3e`), 아니면 **5**.
- 이 제한이 같은 건물 (ncalrpc) 손님에게도 걸리는지는 `I_RpcServerCheckClientRestriction` 갈래를 못 닫아 `[공백]`.

#### 방화벽 확장 — 원격 손님만

- `gFwManagerExtensionState == 1` 일 때 (`GetEntriesNoLock +0x363`), 손님이 **원격일 때만** 확장에 물어 탑을 거른다 (`+0x4fb I_RpcBindingIsClientLocal` · `+0x517 TEST EAX,EAX`).
- ncalrpc 는 로컬로 판정될 것 `[추측: 그 함수 안은 안 읽음]`.
- 이 call 자리는 결과서가 `+0x4f9` 로 적었는데, `.asm` 에선 `+0x4f9` 가 `XOR ECX,ECX` 이고 call 은 **`+0x4fb`** 다 (flow-writer 대조 1).

### 2.10 ★ 등록 쪽 — nsi 사무실이 114 에 탑을 올리는 길 `[정적]`

run 문서 6절. 요청서 물음 5 의 답이다. 이 길은 **nsisvc 가 든 svchost 안** 에서 돈다.

#### 사슬 — 한 걸음씩

1. `nsisvc!NsiStartRpcServer +0x2de RpcServerInqBindings` → `RPC_SERVER::InquireBindings(GlobalRpcServer, …, NULL, NULL)` — **바인딩 벡터** 를 받는다.
2. `+0x337 RpcEpRegisterW(IfSpec, 벡터, NULL, L"NSI server endpoint")` (`+0x325 RDX` 벡터 · `R8` 0 · `R9` 주석).
3. `RpcEpRegisterW +0xf` → `RegisterRpcEndpointsW +0x67` → `RegisterRpcEndpointsA +0xe0` → `RegisterEntries` (다섯째 인자 1 = replace, `+0xd8`).

#### 새 말 — 바인딩 벡터

- **바인딩 벡터** = `RpcServerInqBindings` 가 주는 **그 프로세스의 서버 주소 전부** 목록 `[정적]`. 인터페이스나 protseq 로 **거르지 않는다**.
- 그래서 nsi 를 올릴 때, 그 프로세스가 가진 **다른 protseq · 다른 끝점** 도 전부 같이 장부에 들어간다.
- `ept_map` 응답엔 손님 protseq (`ncalrpc`) 와 같은 것만 나간다. ncalrpc 주소가 둘 이상이면 **탑도 여럿** 이다.
- 비유: nsi 사무실이 114 에 번호를 올릴 때, 자기 방 번호만이 아니라 **자기 가게 (프로세스) 의 모든 번호** 를 한 묶음으로 올린다. 114 는 손님이 "내선이요" 하면 그 가운데 내선 번호만 골라 준다.

#### `RegisterEntries` 가 하는 일 (run 문서 6절 표 줄임)

| 단계 | 한 일 | 줄 |
|---|---|---|
| 주석 길이 | 64 B 이상이면 `0x6d7` (`"NSI server endpoint"` 19자 통과) | `RegisterRpcEndpointsA +0x54` · `+0x5a` |
| UUID 벡터 NULL | nil UUID 하나짜리로 (`Count = 1`) → 객체 UUID nil | `RegisterRpcEndpointsA +0x82 ~ +0x94` |
| 바인딩 0개 | 114 에 안 보내고 `InterfaceExported` 만 | `RegisterEntries +0x41 JZ → +0x39f ~ +0x3ad` |
| 114 연결 | `BindToEpMapper` (ncalrpc → `epmapper`) | `+0x7e` |
| 항목 배열 | `벡터 개수 × 0x58` B (객체 UUID 16 · 탑 포인터 8 · 주석 64). 바인딩마다 탑 · 주석 복사, 실패한 바인딩은 건너뜀 | `+0xeb IMUL RCX,R15,0x58` · `+0x332` · `+0x36a` · `+0x36f` |
| 보내기 | 객체 UUID 하나마다 한 번 `NdrClientCall3(0xe1500, 2, …)`. `0x6ba` · `0x6bf` 면 다시 바인딩해 한 번 더 | `+0x296` · `+0x405` · `+0x48a` |
| 성공 뒤 | `InterfaceExported` | `+0x1a5` |

- 주석 `"NSI server endpoint"` 은 **장부 줄의 64 B 칸에만** 들어간다. `ept_map` 응답엔 안 실린다 (결과서: `ept_lookup` 용).

#### 등록 탑 92 B — 층마다 어디서 오나

`BindingAndIfToTower` → `TowerConstruct` (run133 3-1 과 같은 틀, 이번엔 4층이 채워진 꼴). 탑 = `u32 tower_length` + 탑 바이트, 총 **92 B** (4 + 88).

| 층 | 바이트 | 출처 | 줄 |
|---|---|---|---|
| 머리 | `58 00 00 00` (`tower_length` = `0x40` + 끝점 24 B) · `04 00` | 끝점 길이 | `TowerConstruct` |
| 1층 | `13 00` `0d` + nsi UUID 16 B + 주 `01 00` · `02 00` + 부 `00 00` | **서버 인터페이스 구조체의 `InterfaceId`** (nsisvc rva `0x6420`, v1.0) — 질문 탑 아님 | `BindingAndIfToTower +0x88 RpcIfInqId` |
| 2층 | `13 00` `0d` + NDR 2.0 `8a885d04-1ceb-11c9-9fe8-08002b104860` + `02 00` · `02 00` + `00 00` | **서버 인터페이스 `+0x18` 의 `TransferSyntax`** — 질문 2층 아님 | `+0x98 ~ +0xb3` |
| 3층 | `01 00` `0c` `02 00` `00 00` | protseq `ncalrpc` → `0x0c` | `+0x1e6` → `+0x20e TowerConstruct` |
| 4층 | `01 00` `10` `18 00` + `LRPC-` + 18자 + `00` | **바인딩의 동적 끝점** → ANSI | `+0xb8 I_RpcBindingInqDynamicEndpointW` · `+0xf5 UnicodeToAnsiString` |

- **한 줄 읽기 — 4층**: `01 00` = LHS 길이 1, `10` = 프로토콜 ID (끝점 층), `18 00` = RHS 길이 `0x18` (24 = 23자 + NUL), 그 뒤 `LRPC-` + 18자 + `00`.
- 정적 끝점이면 4층은 문자열 바인딩을 파싱해 얻는다 (`+0x120 RpcStringBindingParseA`). nsi 는 동적이라 이 갈래가 아니다.
- 네트워크 주소 층은 없다 (ncalrpc 는 5층 없음 — run133 3-1 표와 같음).
- 18자는 `ServerSetupAddress +0xe8 ProcessPrng` 난수라 **부팅마다 다르다**. 꼴 (길이 23 · 접두사 `LRPC-`) 만 맞추면 된다.

#### 4층 이름이 정말 `ServerSetupAddress` 의 그 이름인가 — 짐작의 단계

1. 근거: `I_RpcBindingInqDynamicEndpointW` → `SVR_BINDING_HANDLE::InquireDynamicEndpoint` (`0x9d5a0`) 가 `[this+0x100] == 1` (동적) 이면 **서버 주소 객체 (`[this+0xe8]`) `+0x10` 의 끝점 문자열** 을 복사한다 (`+0x6` · `+0x1f` · `+0x2b` · `+0x2f DuplicateString`) `[정적]`.
2. 근거: `LRPC_ADDRESS::ServerSetupAddress` 가 `LRPC-` + 18자를 짓고 `+0x179 DuplicateString` 한다 (run133 4-1) `[정적]`.
3. 그래서: 둘이 같은 이름일 것 — 주소 객체 `+0x10` 이 `ServerSetupAddress` 의 출력 칸이라면.
4. 아직 아님: 그 **출력 칸 = 주소 객체 `+0x10`** 이라는 마지막 연결은 안 읽었다 `[추측]` (Q281).

비유: nsi 사무실은 개업할 때 전화국이 준 무작위 번호를 받아, **가게의 모든 번호** 와 함께 탑으로 지어 114 의 **직원 전용 창구** 에 맡긴다. 탑 1 · 2층엔 사무실 **자기** 이름표와 말투를 적는다.

### 2.11 장부에 넣는 꼴 — `ept_insert_ex` `[정적]`

run 문서 6절 끝 "`ept_insert_ex` 가 장부에 넣는 꼴". 요청서가 "이름 · 한 줄만" 이라 한 범위다.

| 한 일 | 줄 |
|---|---|
| 등록 탑에서 protseq · 끝점 풀기 | `+0x9d TowerExplode` |
| PSEP 노드 잡기 (`tower_length + 0x40 + 끝점 + protseq`), 매직 | `+0x165 HeapAlloc` · `+0x21b` |
| **탑 사본** (`tower_length + 4` B) | `+0x274 memcpy` |
| 목록 **맨 앞** 에 끼우기 | `+0x279` · `+0x27c` |
| 등록자 신분증 (토큰) | `+0x2ee RpcImpersonateClient` · `+0x308 GetCurrentUserToken` → `+0x418 [노드+0x18]` |
| IFOBJ 노드 잡기 (`0xa0` + 주석), 매직 | `+0x3ad HeapAlloc` · `+0x3e3` |
| 장부에 줄 넣기 | `+0x45d EnLinkOnIFOBJList` |
| 덮어쓸 대상 찾기 | `+0x6c5 FindIFOBJNode` |
| 보류된 질문 깨우기 (2.8) | `+0x50c CompletePendingEprRequests` |

- 장부 칸 자리:
  - PSEP: `+0x18` protseq · `+0x20` 끝점 · `+0x28` 탑 사본.
  - IFOBJ: `+0x38` 객체 UUID · `+0x48` 인터페이스 UUID · `+0x58` 버전 · `+0x60` 주석 · `+0x18` 등록자 토큰 · `+0x20` PSEP 목록 · `+0x98` 마지막 사용 시각.

#### 새 말 — 덮어쓰기 (replace)

- `RegisterRpcEndpointsA +0xd8` 가 다섯째 인자 1 (replace) 로 부른다 → 같은 회사가 다시 올리면 **옛 것을 바꿔 끼운다** `[정적]`.
- replace 1 일 때:
  - 같은 프로세스 · 같은 객체 · 인터페이스 · 주 버전 · **장부 부 ≤ 새 부** 인 줄을 찾아 (`+0x6c5`) protseq 같은 옛 탑을 지우고 새 목록을 앞에 붙인다.
  - 버전이 정확히 같으면 그 줄에 합치고, 아니면 새 줄 (`+0x45d`).
- replace ≠ 1 은 protseq · 끝점이 다 같은 것만 지운다.
- 등록한 프로세스가 끝나면 컨텍스트 핸들 rundown (`HPROCESS_rundown`) 이 지운다 `[이름 추측 · 안 읽음]`.
- 비유: 같은 회사가 번호를 다시 올리면, 114 는 같은 전화 방식의 **옛 카드를 빼고** 새 묶음을 맨 앞에 끼운다.

### 2.12 ★ PE_emulator 판정 — 말투는 맞고, 벽은 빈 장부 (run 문서 "PE_emulator 쪽")

flow-writer 는 `../PE_emulator` 의 `epm.cpp` (47줄) 를 **줄 번호만 훑었다** (읽기만). 고칠지는 사용자 · PE_emulator 결정이다.

| 우리 지금 (`epm.cpp`) | 진짜 (정적) | 판정 |
|---|---|---|
| `:24 ~ :27` 늘 40 B — 핸들 0 · 탑 수 0 · 배열 `max_towers · 0 · 0` · 상태 `0x16c9a0d6` | "없음" 답 (`max_towers ≥ 2`) 과 **바이트까지 같다** | **"없음" 일 때만 맞음.** `:23` `TODO(VM)` 은 **정적으로 닫힘** |
| `:21` 장부 없음 → 늘 "없음" | nsi 는 nsisvc 가 서비스 시작 때 등록 (`NsiStartRpcServer +0x337`) — 질문보다 먼저 장부에 있다 `[추측: 서비스 자동 시작 — 서비스 설정은 안 읽음]` | **틀림 — 이게 벽** |
| `:41` 프로시저 8 만, `:40` "3 · 7 은 공사 중" | 3 · 7 · 8 은 **같은 몸** | 맞음 — 열 땐 같은 장부 함수에 |
| `:20` `max_towers > 500` → `nullopt` | 서버 NDR 범위 `0 ~ 500` (run137 `FC_RANGE`) | 맞음 |
| `max_towers = 0` · `1` 에도 같은 40 B | 진짜는 핸들 −1 (2.7) | 우리 손님은 4 라 영향 없음. 선 위 20 B 는 `[공백]` |
| `:18` SID 포인터 ID ≠ 0 → `nullopt` | 진짜는 `AccessCheck` 후 그 줄만 건너뜀 (2.9) | 우리 손님은 NULL 이라 지금 영향 없음 (flow-writer 보탬) |

#### 그래서 — 왜 "벽은 빈 장부" 인가, 단계로

1. 근거: 에뮬레이터 40 B 와 진짜 "없음" 40 B 가 바이트까지 같다 `[정적]`.
2. 그래서: 손님 (rpcrt4) 이 `0x6d9` 를 받는 건 **답 꼴이 틀려서가 아니라**, 114 가 정직하게 "없음" 이라고 했기 때문이다.
3. 진짜 Windows 에선 nsi 줄이 이미 장부에 있어 "있음" (140 B) 이 나간다 — 장부에 있다는 건 서비스 자동 시작 `[추측]` 에 기댄다.
4. → 고칠 곳은 응답 꼴이 아니라 **장부를 채우는 것**.

#### 서 바퀴에 필요한 것 (run 문서 6줄 줄임)

1. **장부 줄**: `{ 객체 UUID (nil) · 인터페이스 UUID · 주 · 부 · 주석 (≤ 63자) · 탑 목록 [{ protseq, 끝점, 탑 바이트 }] }`.
2. **누가 · 언제**: nsi (`7ea70bcf…` v1.0, 주석 `"NSI server endpoint"`, `ncalrpc`, 끝점 = 우리 nsi 창구 포트 이름 `LRPC-` + 18자) 를 **epmapper 창구가 뜰 때 미리 한 줄** 채운다. 이름은 nsi 창구 포트 (`\RPC Control\<이름>`) 와 같아야 한다 — 손님이 그 이름으로 둘째 연결을 한다 (run133 3-3).
3. **고르기**: 2.4 표 그대로. 질문 3층이 `0x0c` 인데 층 수가 4 가 아니면 `0x16c9a0cd` · 탑 0.
4. **응답 바이트** (n ≤ 4): 2.6 표. nsi 하나면 **140 B**, 머리 `TotalLength 0xcc`.
5. **탑 바이트**: 2.10 등록 탑 표 그대로. 2층은 질문이 NDR64 를 물어도 **NDR 2.0 v2.0**.
6. 샘플이 `RpcEpRegister*` 를 직접 부르면 직원 창구 (`0b0a6584` v1.1 메뉴 2) 를 받아야 한다. 그 요청 NDR 바이트는 안 풀었다 (Q281).

비유: 우리 114 는 말투도 응답 양식도 진짜와 똑같다. 다만 **전화번호부가 백지** 다. 개업 날 아침에 nsi 사무실 한 줄을 미리 적어 두면 된다.

### 2.13 앞 run 고쳐 읽기 — 닫힘 둘

run 문서 "앞 run 고쳐 읽기". **틀린 것은 없고** `[추측]` · `[규칙]` 이 `[정적]` 으로 닫힌 것이라 "정정" 이 아니라 "고쳐 읽기" 다. 두 run 문서 맨 위에 한 줄씩 붙었고, 이 해설과 함께 두 해설서 맨 위에도 한 줄씩 붙였다.

- **run133** 3-3 (해설 2.12 끝): "1 · 2층 값 · 길이 칸은 `TowerConstruct` 꼴 그대로 두면 안전 — 진짜 epmapper 가 등록 때 받은 탑을 그대로 돌려준다고 짐작 `[추측]`" → **`[정적]`** (2.5 의 두 `memcpy`).
  - 보탤 점: 1 · 2층은 **서버 인터페이스** 값 (질문 2층과 무관), 탑 수는 그 프로세스 ncalrpc 주소 수, 차례는 등록의 거꾸로. `LRPC-` 이름이면 `tower_length` `0x58` (같은 식 `0x40` + 끝점 길이).
- **run137** 3-1 (해설 2.12): "탑 0개: 끝점 핸들 0 · `num_towers` 0 · 배열 `4 · 0 · 0` · 상태 → 40 B `[규칙]`" → 서버가 값을 정하는 칸이 **`[정적]`** (`+0x4c3` · `+0x8e` · `+0x867`).
  - 단 `max_towers ≤ 1` 이면 핸들 −1. 배열 머리는 그대로 `[규칙]`.

### 2.14 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음)

run 문서 "flow-writer 대조". 결과서 오프셋을 `.asm` 과 맞춰 본 것이다. 이 글은 **run 문서 쪽** 을 따랐다.

1. `I_RpcBindingIsClientLocal` call: 결과서 `+0x4f9` → **`+0x4fb`** (`+0x4f9` 는 `XOR ECX,ECX`).
2. 거부 표시 쓰는 줄: 결과서 `+0xbcf` → **`+0xbd8 MOV dword [R8],1`** (`+0xbcf` 는 `TEST R8,R8`, `+0xbd2 JZ → +0x867`).
3. `GetEntriesNoLock +0x192` 는 **직접 call 이 아니다** — CFG 간접 (2.2 줄 ①). 뜻은 같고 `⇢` 로 적음.
4. SID 갈래 실패 값에 **예외 `0xe`** (SD `HeapAlloc` 실패) 보탬, `IsValidSid` 는 두 번.
5. 핸들 갈래 앞 두 줄 (`+0x4ab` · `+0x4af`) 보탬 (2.7).
6. alias: `_calls.tsv` 의 `alias:X+1` 은 같은 함수의 C++ 장식 이름 하나 — `pdb_aliases.py --lookup` 로 "별칭 무리 아님". **별칭 무리는 `0x83c0` 하나** (2.3).
7. 이름: `RemoteEpmpSecurityCallback` → PDB `RemoteEpmpServerSecurityCallback`.
8. `tower_length` 식: 결과서 "끝점 RHS 24 + 5 + 0x3b = 0x58" 은 run133 식 "`0x40` + 끝점 길이 (NUL 포함)" 과 같은 값 — run133 식으로 적음.

### 2.15 정리 — 비유로 한 번 더

- 114 (epmapper) 는 다른 가게 (svchost RPCSS) 에 있다. 창구가 둘이다.
  - **손님 창구** (`e1af8308` v3.0): 메뉴 3 · 7 · 8 은 **같은 직원**. 등록 · 삭제 메뉴는 "안 돼요" 만.
  - **직원 전용 창구** (`0b0a6584` v1.1, 내선만): 메뉴 2 가 **등록** (`ept_insert_ex`).
- 개업 때 nsi 사무실은 **가게의 모든 번호** 를 탑으로 지어 직원 창구에 맡긴다. 탑 1 · 2층은 사무실 자기 이름표 · 말투, 4층은 전화국이 지어 준 `LRPC-` + 18자.
- 114 는 맡은 탑을 **복사해서** 전화번호부에 꽂는다 (맨 앞에).
- 손님이 맨 위 칸 빈 탑을 내밀면, 114 는 **회사 이름 · 큰 판 · 작은 판 이상 · 내선** 만 보고 맞는 탑을 **또 복사해서** 4장까지 건넨다. 손님 탑의 말투 칸 · 번호 칸은 안 본다.
- 없으면 "등록 안 됐어요" (`0x16c9a0d6`) 40 B. 예약 (트리거) 이 있으면 끊지 말고 기다리게 한다.
- 손님 얼굴은 안 본다. 손님이 신분 번호를 적어 낼 때만 맡긴 사람 신분증과 맞대 본다.
- 우리 114 는 말투 · 양식은 맞다. **전화번호부가 백지** 일 뿐이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 6a99cb3 -- runs/03_sprint/flow.md`)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — rpcrt4 PDB 줄 | 앞 바퀴 (run99 · run133 · run135 등) 기록까지 | + **run145 — 다시 analyze 안 함, `^이름$` export** (`RpcEpRegisterW` · `TowerExplode` · `RegisterRpcEndpoints*` · `BindingAndIfToTower` 등) |
| 기준 — nsisvc PDB 줄 | run133 기록 | + **run145 — 있던 `NsiStartRpcServer.asm` `+0x2de` · `+0x325 ~ +0x337` 읽기**, 주석 글은 `strings` |
| 기준 — PDB 목록 | RpcEpMap 없음 | **RpcEpMap.pdb 새 줄** (`928AE9EA93768986C32C5500323376111`, 처음 analyze 9초 · 함수 440, 폴더 `rpcepmap` 소문자) |
| 기준 — 근거 줄 | — | **(run145) 정적 바퀴, 사진 값 대조 없음** (test.exe 가 안 올림 `[관찰]` · API 없음 `[소스]`, epmapper 는 다른 프로세스, 요청 쪽 숫자는 `[관찰]` 아님) |
| 1장 그림 | — | **안 바뀜** (범위 밖) |
| 3장 run133 절 | 3-3 "등록 탑 그대로" `[추측]` | 한 줄 보탬: **`[추측]` → `[정적]`** (두 `memcpy`), 1 · 2층 = 서버 인터페이스, 2층 · 4층 안 봄 |
| 3장 run137 절 | 탑 0개 40 B `[규칙]` | 한 줄 보탬: **서버 값 `[정적]`** (`+0x4c3` · `+0x8e` · `+0x867`), `max_towers ≤ 1` 이면 −1, nsi 하나면 140 B |
| 3장 run141 절 뒤 | — | **새 절 "epmapper 가 `ept_map` 에 답을 고르고 채우는 법"** `[정적]` — 비유 한 줄, 나무 하나, 일곱 줄 요약 (한 몸 · 고르기 · 탑 사본 · 없음 · 부른 쪽 검사 · 등록 · PE_emulator) |
| 4장 run137 PE_emulator 줄 | "새 창구: `ept_map` 응답 128 B (탑 0개 40 B …)" | 끝에 "(run145) 이 탑 0개 40 B 는 진짜 없음 답과 **바이트가 같다** — 막힘은 장부" 표시 |
| 4장 새 줄 넷 | — | **Q279 해결됨 줄** + **run145 동적 (→ Q280) · 정적 · PE_emulator** |
| 5장 근거 run | — | **run145 줄** 둘 |

- 태그 변화:
  - run133 3-3 "등록 탑을 그대로 돌려준다" — `[추측]` → **`[정적]`**.
  - run137 3-1 "탑 0개 40 B" 의 서버 값 — `[규칙]` → **`[정적]`** (배열 머리만 `[규칙]` 그대로).
  - 새 `[추측]` 셋: 서비스 자동 시작 · 4층 이름 마지막 연결 · ncalrpc 로컬 판정.
  - 새 `[공백]` 다섯 (run 문서 "정적으로 못 닫은 것"): ncalrpc 주소 개수 · 트리거 키와 전역 셋 값 · −1 핸들 선 위 20 B · `I_RpcServerCheckClientRestriction` ncalrpc 갈래 · `ept_insert_ex` 요청 NDR 바이트.
- run133 · run137 문서 맨 위에 "고쳐 읽기 (run145)" 한 줄씩이 붙었다.
- 짐 지도 (`baggage.md`) 는 안 바뀌었다 — 초기화 동안의 짐이 아니고, epmapper 는 다른 프로세스다.

### 큰 그림에서 어떻게 이어지나

- 흐름도 본선 (프로세스 생성 → `NtContinue` 은퇴 → `main`) 엔 **변화가 없다**. 이번도 `main` 뒤 옆길이다.
- run128 → run129 → run133 → run137 → run141 → **run145** 로, "웹 요청 하나가 망 안내 쪽에 무엇을 묻나" 의 전화 이야기가 **양쪽 끝** 까지 왔다.
  - run129: 114 에 묻는 사슬 (묻는 쪽).
  - run133: 첫인사 · 본론 · 질문 탑의 꼴 (선 위).
  - run137: 답 배달 · 응답 칸 차례 (받는 쪽).
  - run141: 비동기 연결 → 첫인사 사이.
  - **run145: 114 가 답을 고르고 채우는 법 (답하는 쪽) + 그 장부에 번호가 들어가는 법 (등록 쪽).**
- 이로써 에뮬레이터 114 의 `ept_map` 은 **정적으로는 다 지을 수 있게** 됐다 — 장부 줄 하나 · 고르기 · 140 B 응답.
- 그다음 관문은 손님이 받은 이름으로 거는 **둘째 연결** (nsi 사무실 창구) 이고, 그 진짜 모습은 Q280 (VM) 에서 본다.
- 앞 바퀴들과 이어지는 곳:
  - run133 2.13 — 동적 끝점 `LRPC-` 이름을 짓는 곳 (`ServerSetupAddress`). 이번 4층 이름의 출처.
  - run129 4절 — `FatalLookupErrorEncountered` (`0x16c9a0d6` → `0x6d9`).
  - run137 2.11 · 2.12 — 프로시저 8 칸 차례. 이번에 "누가 그 값을 쓰나" 가 붙었다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q280** (동적, VM 묶음 — 미룸)

- **왜 이번에 못 알았나**: 정적만 했다. 장부에 실제로 몇 줄 · 몇 탑이 있는지, 레지스트리 · 전역 값은 실행 때 값이다.
- **무엇이 필요한가**: 원문 물음 2 · 3 과 같은 세션 (Q231 ④⑤⑥ · Q258 ①⑧ · Q271 과 함께, winhttp 한 번 시험 프로그램 — 사용자 결정). **RpcEpMap 멈춤점은 전부 svchost (RPCSS, 서비스 `RpcEptMapper`) 안** 의 유저 멈춤점이라 그 프로세스 조건을 붙인다. `rbp` = `ept_map_auth_sd +0x10 LEA RBP,[RSP-0x10]` 의 값.

| 번호 | 자리 | 무엇을 보나 | 닫는 것 |
|---|---|---|---|
| D1 | `RpcEpMap!ept_map_auth_sd+0x109` (`TowerExplode` ret) | `eax` 0, `[rbp-0x28]` (UUID + 주 · 부), `[rbp-0x60]` "ncalrpc" | 손님이 실제로 묻는 것 (2.4) |
| **D2 (필수)** | `ept_map_auth_sd+0x2c7` (첫 `GetEntries` ret) | `eax` 상태 · `[r13]` 탑 수 · `[rbp-0x48]` 핸들 칸 · `[rbp-0x40]` 배열 → 탑 바이트 | 2.6 · 2.10 표와 같은지. `rpcrt4!IoCompleted+0xcf` 와 한 실행에 걸면 서버가 만든 탑과 손님이 받은 버퍼를 맞대 볼 수 있다 |
| D3 | `GetEntriesNoLock+0x4c6` · `+0x867` | `edx` | 마지막 상태 · 없음 길 (2.8) |
| **D4 (필수, 덤프만)** | `dq RpcEpMap!IFObjList L1` → nsi 줄 (`+0x48` 이 `7ea70bcf…`) 의 `+0x38` · `+0x58` · `+0x60` · `+0x20` → PSEP 마다 `+0x18` · `+0x20` · `+0x28` | 장부 그대로 | **ncalrpc 주소 개수 `[공백]`** · 차례 (2.5 · 2.10). `ept_insert_ex` 는 부팅 때 지나가 잡기 어렵다 |
| D5 (선택) | nsisvc 가 든 svchost (LocalService) 의 `rpcrt4!RegisterEntries+0x29b` | 항목 배열 `0x58 × n` | 등록 쪽 실물 (2.10) |
| D6 | 레지스트리 `…\NetworkServiceTriggers\Triggers\bc90d167-9470-4139-a9ba-be0bbbf5b74d\` 아래 `7ea70bcf-48af-4f6a-8968-6a440754d5fa` | 있음 / 없음 | 보류 길을 타나 (2.8) |
| D7 | `RpcEpMap!gFwManagerExtensionState` (rva `0x1cf60`) · `g_TriggersDisabled` (`0x1ccc8`) · `EPMAllowAnonymous` (`0x1c5e8`) | 값 | 방화벽 · 트리거 · 문지기 갈래 (2.8 · 2.9) |

### 정적으로 남은 것 → **Q281**

| 번호 | 무엇 | 왜 이번에 못 했나 |
|---|---|---|
| 1 | `ept_insert_ex` (직원 창구 메뉴 2) 의 요청 · 응답 NDR 바이트. 같은 창구 0 (`OpenEndpointMapper`) · 3 (`ept_delete_ex`) 은 이름만 | 요청서가 등록 쪽은 "이름 · 한 줄만" 이라 했다. 샘플이 `RpcEpRegister*` 를 직접 부르거나, 에뮬레이터가 nsisvc 등록을 흉내 낼 때 필요 |
| 2 | `I_RpcServerCheckClientRestriction` · `IsSecureClient` 의 ncalrpc 갈래 | 문지기 (2.9) 가 로컬 손님을 막는지 — 갈래를 못 닫음 |
| 3 | `ServerSetupAddress` ↔ 서버 주소 객체 `+0x10` (`CreateOrUpdateAddresses`) | 4층 이름의 마지막 연결 (2.10 `[추측]`) |
| 4 | 서버 컨텍스트 핸들 −1 마샬링 (`NdrServerContextNewMarshall` 류) | `max_towers ≤ 1` 호출이 올 때만 필요 (2.7) |
| 5 | 원문 물음 3 의 정적 몫 (nsisvc 프로시저 6 · webio `Startup` 다음) | 이번 범위 아님 — Q280 쪽 |
| 6 | 셸 꼴 16 · exe 직접 4 가 묻는 인터페이스 | 정적보다 동적 D2 의 `[rbp-0x28]` 한 번이 빠름 |

- plan 의 Q281 줄에는 "nsisvc 서비스 시작 유형" 도 들어 있다 — 2.12 의 `[추측: 서비스 자동 시작]` 을 닫을 몫이다.

### 대기열 밖 — `[공백]` · `[추측]` 으로 남은 것

- `[추측]` nsisvc 가 자동 시작이라 질문보다 먼저 장부에 있다 — 서비스 설정은 안 읽음 (Q281 의 서비스 시작 유형).
- `[추측]` ncalrpc 손님은 방화벽 확장에서 로컬로 판정 — `I_RpcBindingIsClientLocal` 안은 안 읽음.
- `[추측]` 객체 UUID 포인터 == 탑 포인터 검사 (`0x6d8`) 는 정상 호출엔 안 걸림.
- `[추측: TCP 탑 주소 칸]` TCP · HTTP 탑 `+0x4b` 4 B 의 뜻 (ncalrpc 와 무관).
- 이름 — `IFOBJ` · `PSEP` · `ept_s_not_registered` 는 `[이름 추측]`, `HPROCESS_rundown` 은 `[이름 추측 · 안 읽음]`.

---

## 5. 더 읽기

- run 문서: [`run145-epm-endpoint.md`](run145-epm-endpoint.md)
  - 호출 지점 표 전체 · 1절 (프로시저 표) · 2절 (견주기 표) · 3절 (응답 칸 표 · 등록 탑 표) · 4절 (없을 때 · 트리거) · 5절 (부른 쪽 검사) · 6절 (등록 쪽 · 장부 꼴) · "PE_emulator 쪽" · "flow-writer 대조" · "앞 run 고쳐 읽기"
- 결과서: [`_workspace/sast/143_epm-endpoint_result.md`](../../_workspace/sast/143_epm-endpoint_result.md) — "먼저 읽는 한 줄 답", 표 1 (응답 칸) · 표 2 (견주기) · 표 3 (등록 탑), "지금 우리 맞나 · 틀리나", "동적으로 확인할 것", "다음 요청 후보", 판정
- 요청서: [`_workspace/sast/143_epm-endpoint.md`](../../_workspace/sast/143_epm-endpoint.md) — "왜" (악성 396 · winnsi 꼴 376)
- flow.md: [`flow.md`](flow.md) — 3장 새 절 "epmapper 가 `ept_map` 에 답을 고르고 채우는 법" · 3장 "LRPC 선 위 형식" 절 (run133) · "비동기 LRPC 답 배달" 절 (run137) 의 run145 줄 · [4장 공백 목록](flow.md#4-공백-목록-다음에-채울-곳) "3 (범위 밖 — NSI)" Q279 · run145 줄 · [5장 근거 run](flow.md#5-근거-run) run145 줄
- 바로 앞 바퀴들 (같은 전화 이야기):
  - [run129 해설](run129-winnsi-rpc-해설.md) — 2.4 연락 카드 · 2.5 메뉴 번호 · 2.7 114 · 탑 · `ept_map`
  - [run133 해설](run133-lrpc-wire-해설.md) — 2.11 질문 탑 4층 70 B · 2.12 답 검사 · 최소 탑 · 2.13 동적 끝점 · nsi 사무실 개업 차례
  - [run137 해설](run137-alpc-completion-해설.md) — 2.11 프로시저 8 파라미터 · 2.12 응답 칸 차례 (128 B 예 · 40 B)
  - [run141 해설](run141-async-bind-해설.md) — 비동기 114 연결 → bind
- 대기열: `_workspace/plan.md` Q279 (끝) · Q280 (동적, VM 묶음 — 미룸) · Q281 (정적)

# run99 rpc-caller — 해설

> 이 글은 [run99 문서](run99-rpc-caller.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q180c** — (Q180 ③, 낮음) rpcrt4 — `UuidToStringA` 에서 닿는 초기화 안 `\RPC Control` 여는 함수가 SD 에서 무엇을 보나, 어떤 결과면 `0xe` 로 가나, 통과하면 다음 syscall (`_workspace/plan.md`, 이번에 **끝**)
  - **PE_emulator 요청 34 의 셋째 조각**이다.
    - 첫 조각 run97 (Q180a) — 관리사무소 (커널) 가 "명부 사본 주세요" 에 **답하는** 규칙.
    - 둘째 조각 run98 (Q180b) — 관리인 (smss) 이 `\RPC Control` 방과 **원본 명부** 를 만드는 길.
    - 셋째 조각 run99 (이번) — 그 사본을 **받는 쪽** (rpcrt4) 이 무엇을 보고 언제 포기하나.
    - 넷째 조각 Q180d (run100 예정) — 넘긴 명부가 방에 **붙을 때** 의 계산. 이 글에선 **이름만** 나온다.
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → **Q181** (run99 몫 ① ~ ⑩)
    - 정적 → 새 **Q186 ① ~ ④**, 그리고 Q180d (이름만)
- 근거:
  - run 문서 [`run99-rpc-caller.md`](run99-rpc-caller.md) (가장 중요)
  - 요청서 [`_workspace/sast/95_rpc-caller.md`](../../_workspace/sast/95_rpc-caller.md) · 결과서 [`95_rpc-caller_result.md`](../../_workspace/sast/95_rpc-caller_result.md)
    - 판정 **분석성공**. 요청서 질문 1 ~ 5 모두 `.asm` 줄 근거로 답했다.
    - rpcrt4 는 이번에 **처음** PDB 를 받고 처음 analyze 했다 (함수 3289).
    - ntdll 다섯 · kernelbase 둘은 다시 analyze 하지 않고 읽기 전용 export 만 했다.
    - 못 닫은 것: 진짜 `\RPC Control` SD 바이트, 토큰 질문들의 진짜 대답, ntdll `RtlpNewSecurityObject` 의 조건부 갈래, kernelbase 레지스트리 syscall 이름.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · 흐름도 앞 글 · 2.4 run65 (run98) 줄 근거 · 3장 run97 · run98 절 한 줄씩 · **3장 새 절** · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): **안 고침** (3절)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행도, 사진 다시 읽기도 없다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **RPC 런타임** — "한 줄로 먼저" 바로 아래
- **같지 않으면 뛰기** (`CMP` 다음 `JNZ` 읽는 법) — 2.2
- **포기 칸** (실패 길이 모이는 한 자리, `+0x2ed`) — 2.2
- **가져오기 칸** (import 칸) — 2.5 (run86 해설에서 쓴 말, 짧게만)
- **AppContainer** (샌드박스 가게) — 2.6
- **부모 SD · 만든이 SD** (견본 명부 · 초안 명부) — 2.7
- **열림 깃발** (`RpcHasBeenInitialized`) — 2.9
- **자기상대형 SD** 는 run97 해설 2.5 의 "self-relative (한 덩어리 꼴)" 그대로 (링크만)

### 이번 바퀴에서 조심할 곳 넷

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 rpcrt4 를 안 올린다. 초기화 끝 (P2) 모듈은 6개뿐이다 `[관찰]` (run44).
   - `rpcrt4.dll` 은 `\KnownDlls` 섹션 목록에만 있다 (`ms/logs/04_baggage-read_P2.txt:1392`) `[관찰]`. 공용 창고에 놓여 있지만 **안 꺼내 온** 것이다.
   - 그래서 이번 이야기는 **"rpcrt4 를 쓰는 다른 프로그램이라면"** 이다. 흐름도 **그림은 그대로** 다.
2. **에뮬레이터 쪽 숫자는 우리 `[관찰]` 이 아니다.**
   - `r04 2ed5ce549014 seq 289 → 312`, 383 샘플, 되풀이 3137 번 — 모두 요청서 배경, 즉 **PE_emulator 쪽** 에서 왔다.
   - 이 글에서도 태그 없이 "에뮬레이터 쪽 관찰" 로만 부른다.
3. **오프셋은 call 자리다.** ret 자리 (부른 함수가 돌아오는 곳) 는 run 문서 "정적 — 호출 지점" 표에 있다.
   - 이번 바퀴엔 결과서가 멈춤점에 ret 자리를 섞어 쓴 곳이 있어서, run 문서가 바로잡았다 (2.13 · 2.14).
4. **"rpcrt4 는 SD 안을 안 읽는다" 는 rpcrt4 이야기다.** 그 SD 를 받아 쓰는 ntdll 함수 (`RtlpNewSecurityObject`) 안쪽은 **앞쪽과 syscall 자리만** 봤다. ntdll 이 견본 명부를 어떻게 합치는지는 아직 `[공백]` 이다 (2.7).

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 프로세스 = **가게**, 핸들 = **손잡이** → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- ntdll = 가게 안 **관리실** → [run98 해설](run98-rpc-dir-create-해설.md) "한 줄로 먼저"
- 토큰 = 가게의 **신분증** → [run49 해설](run49-kprocess-token-htable-해설.md) "비유"
- 디렉터리 = 사물함이 모인 **방** → [run71 해설 2.2](run71-named-obj-dir-해설.md)
- SD = 방마다 붙은 **출입 명부**. 칸은 주인 (소유자) · 소속 (그룹) · **허락 쪽** (DACL) · **특수 쪽** (SACL) → [run97 해설 2.0](run97-rpc-sd-query-해설.md)
- 출력 버퍼 = **봉투**, `0xC0000023` = "**봉투가 작아요**" → [run97 해설 2.4](run97-rpc-sd-query-해설.md)
- smss = 부팅 때 방을 꾸미는 **관리인**, `\RPC Control` 의 명부 = **느슨한 명부** (Liberal SD) → [run98 해설](run98-rpc-dir-create-해설.md) "한 줄로 먼저" · 2.4
- `\KnownDlls` = 동네 **공용 창고** → [run65 해설 2.3](run65-ldr-init-bodies-해설.md)
- ALPC 포트 = 프로세스끼리 거는 **전화선 끝** → [run68 해설 2.4](run68-dll-init-rest-해설.md)
- CFG 간접 호출 = 번호를 보고 이어 주는 **교환원** → [run96 해설](run96-bcrypt-interface-해설.md) "이번 글의 새 말"
- 레지스트리 = 관리실 **설정 장부** → [run58 해설](run58-registry-strings-해설.md)

flow.md 3장 새 절은 **"새 사무실 · 건물 공용 게시판 · 규칙표 · 견본"** 비유를 썼다. 그림만 다르고 가리키는 건 같다.

| flow.md 3장 새 절 | 이 글 | 실제 |
|---|---|---|
| 새 사무실 열기 | 가게 안에 **전화 부서** 차리기 | RPC 런타임 초기화 (`PerformRpcInitialization`) |
| 건물 공용 게시판 | 관리인이 만든 **방** | `\RPC Control` 디렉터리 객체 |
| 게시판의 규칙표 | 그 방의 **느슨한 명부** | `\RPC Control` 의 SD |
| "견본으로 복사해 와서" | **견본 명부** (부모 SD) | `RtlNewSecurityObjectEx` 첫 인자 |
| 내 규칙표 | 전화 부서의 **기본 명부** | `gDefaultSecurityDescriptor` |
| "종이가 모자라요 + 몇 장" | "**봉투가 작아요** + 크기" | `0xC0000023` + `*len` |
| "괜찮아요 (0)" | "다 됐어요" | status 0 |
| "메모리 부족" 이라고만 적기 | **포기 칸** 에서 `0xe` 적기 | `+0x2ed MOV EBX,0xe` |

### 한 줄로 먼저 — 무엇을 얻었나

먼저 새 말 하나.

- **새 말 — RPC 런타임** = **다른 프로세스의 함수를 부르게 해 주는 통신 도구 모음** 이다. 처음 쓸 때 한 번 준비한다. 이번에 본 rpcrt4 의 `PerformRpcInitialization` 이 그 "한 번 준비" 다 `[정적: 함수 이름 · 깃발]`. "다른 프로세스의 함수를 부르게" 라는 뜻풀이는 일반적으로 알려진 설명이다 `[추측]`.
  - 비유: 가게 안에 다른 가게로 일을 부탁하는 **전화 부서**. 처음 전화를 쓰려는 순간 부서를 한 번 차린다.
  - 부서를 차릴 때 할 일 중 하나가 **부서의 기본 출입 명부** 를 만드는 것이다. 이번 이야기는 전부 그 명부 만들기 안에서 일어난다.

**이번 글의 중심은 이 세 줄이다.** `[정적]`

> **전화 부서의 기본 명부를 만들려고, rpcrt4 `RpcpNormalizeSecurityDescriptor` 가 `\RPC Control` 방을 열고 명부 사본을 두 번 묻는다** (`+0x158` 크기만, `+0x19c` 봉투 주고). 받은 사본은 **안 읽고** 견본으로 ntdll `RtlNewSecurityObjectEx` 에 통째로 넘긴다.

> **첫 대답이 정확히 `0xC0000023` ("봉투가 작아요") 일 때만 다음으로 간다** (`+0x164 CMP EAX,0xc0000023` · `+0x169 JNZ`). 0 ("다 됐어요") 이든 다른 실패든 **포기 칸 `+0x2ed`** 에서 `0xe` 를 적는다. 에뮬레이터는 0 으로 답해서 **길이를 보기도 전에** 여기서 떨어졌다.

> **`0xe` 는 이 사슬의 상수다.** 바꿔 적는 변환은 없고, `UuidToStringA` 는 받은 값을 그대로 돌려줄 뿐이다. 그리고 실패하면 **열림 깃발이 안 켜져서** 다음 부름 때 처음부터 다시 한다.

| 요청서 질문 | 한 줄 답 | 이 글 |
|---|---|---|
| 1 누가 여나 | `RpcpNormalizeSecurityDescriptor +0x12a` 하나. 사슬 = `UuidToStringA` → `PerformRpcInitialization` → `InitializeDefaultSecurityDescriptor` → 이것 | 2.1 · 2.5 · 2.6 |
| 2 SD 에서 무엇을 보나 | 두 번 묻기 맞음. 첫 대답은 `0xC0000023` 만 통과. SD 안은 안 읽음 | **2.2** · 2.3 · 2.4 · 2.7 |
| 3 언제 `0xe` | `+0x2ed` · `+0x2a7` · `PerformRpcInitialization +0x425` 의 상수, `UuidToStringA` 는 옮기기만 | 2.8 · 2.9 |
| 4 통과하면 다음 | 토큰 열기 · 토큰 묻기 → 정책 레지스트리. ALPC · `\RPC Control\DSEC` 아님 | 2.10 |
| 5 최소 조건 | 첫 대답 `0xC0000023` + 진짜 길이, 둘째 대답이 명부 채워 성공, 그다음 토큰 질문 다 성공 | 2.11 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 길은 test.exe 의 길이 **아니다.** test.exe 흐름 옆에 "rpcrt4 를 쓰는 다른 프로그램" 을 나란히 놓으면 이렇다.

```
(부팅)
 관리인 smss — \RPC Control 방 + 느슨한 명부                       (run98)
 │
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll!LdrInitializeThunk → LdrpInitializeProcess … → NtContinue (은퇴)
 │  → 진입점 → main
 │  (test.exe 는 여기서 끝. rpcrt4 를 안 올린다 — run44 [관찰])
 │
(rpcrt4 를 쓰는 다른 프로그램이라면 — PE_emulator 가 만난 상황)
 어떤 코드가 rpcrt4!UuidToStringA 를 부름
 └─ 전화 부서 (RPC 런타임) 가 아직 안 열렸으면 → PerformRpcInitialization
      └─ 기본 명부 만들기 → RpcpNormalizeSecurityDescriptor
           ├─ \RPC Control 방 열기 → 명부 사본 크기 묻기               ← 이번 (run99)
           │     관리사무소가 어떻게 답하나                             (run97)
           └─ 사본을 견본으로 새 명부 만들기 (ntdll)                    ← 이번 (앞쪽만)
                 견본이 붙을 때의 계산                                  (Q180d, run100 예정)
```

- 맨 아래 두 줄 (run97 · Q180d) 은 이번 길에서 **관리사무소 쪽** 에 맡긴 부분이다. 이번엔 rpcrt4 쪽만 봤다.

### 왜 이 질문이 지금 왔나

- run97 에서 "진짜 커널은 크기만 물으면 `0xC0000023` + 필요 길이로 답한다. 에뮬레이터는 0 으로 답했다" 까지 봤다.
- 그리고 그게 `UuidToStringA → 0xe` 의 **가장 가까운 원인 후보** 라고 `[추측]` 으로 남겼다 ([run97 해설 2.4](run97-rpc-sd-query-해설.md) "그래서 에뮬레이터는 왜 막혔나").
- "후보" 를 "확인" 으로 바꾸려면 **받는 쪽 코드** 를 봐야 한다. 0 을 받은 rpcrt4 가 정말 그 자리에서 포기하나? 그게 이번 질문이다.

### 무엇이 궁금했나

"`UuidToStringA` 안에서 **누가** `\RPC Control` 을 열고, 받은 명부에서 **무엇을** 보고, **어떤 대답이면** `0xe` 로 가나? 통과하면 그다음은 무엇인가? 에뮬레이터는 **최소한 무엇을** 줘야 하나?"

- 요청서는 이걸 다섯으로 나눴다 (위 "한 줄로 먼저" 표).
- 사용자가 가장 궁금해한 건 **PE_emulator 쪽 원인** — "status 0 이 왜 바로 `0xe` 로 가나" 다. 그래서 2.2 를 가장 천천히 쓴다.

### 바퀴 전에 알던 것

- `[정적]` (run97) 크기만 물으면 (`NULL` · 0) 진짜 커널은 **`0xC0000023`** 을 돌려주면서 **필요 길이를 `*LengthNeeded` 에 꼭 적는다**. 필요 길이는 `0x14` 에서 출발하므로 **길이 0 으로 "성공" 하는 길은 없다** ([run97 해설 2.4](run97-rpc-sd-query-해설.md)).
- `[정적]` (run97) `0x20001` 손잡이로 `0x17` 을 물어도 통과한다 (LABEL 은 `READ_CONTROL` 이면 됨).
- `[추측]` (run97) "에뮬레이터의 0 대답이 `UuidToStringA → 0xe` 의 가장 가까운 원인" — **이번에 `[정적]` 이 된다** (2.2).
- `[정적]` (run98) `\RPC Control` 방은 부팅 때 smss 가 **느슨한 명부** 를 붙여 만든다.
- `[관찰]` (run44) test.exe 가 올리는 모듈은 6개 — rpcrt4 는 없다.
- 에뮬레이터 쪽 관찰 (요청서 배경, 우리 `[관찰]` 아님): `UuidToStringA` 들어감 → … → `NtQueryInformationToken(-6, 0x1d, 4)` → `NtOpenDirectoryObject("\RPC Control", 0x20001)` → `NtQuerySecurityObject(h, 0x17, NULL, 0, &len)` → `NtClose(h)` → `UuidToStringA` 가 `0xe`. `NtClose` 와 반환 사이 syscall 0.

---

## 2. 찾은 것

### 2.1 한눈에 — 전화 부서를 차리다 기본 명부에서 막히는 길

run 문서 "호출 관계" 를 대표 줄만 남겼다. 숫자는 run 문서 그대로다. 모두 `[정적]`.

```
rpcrt4!UuidToStringA
 │  +0x26  열림 깃발 (RpcHasBeenInitialized) == 0 ?   이미 열렸으면 +0x2f JNZ 로 건너뜀
 └─ +0x31  PerformRpcInitialization                  ← export 123개가 입구에서 부름
      │  +0x21 락 · +0x59 LoadLibraryExW("rpcrt4.dll") · … (앞 단계)
      ├─ +0x2b3  InitializeDefaultSecurityDescriptor      0 아니면 +0x2ba → +0x425 (0xe)
      │     │  초안 명부: S-1-5-7 에 GENERIC_READ 한 줄
      │     └─ +0x13c  RpcpNormalizeSecurityDescriptor (초안, 1, &gDefaultSecurityDescriptor)
      │           ├─ +0xa8   ntdll!RtlGetAppContainerNamedObjectPath
      │           │            └─ +0xb6  NtQueryInformationToken(-6, 0x1d, 4)   → 이름 앞만 가름
      │           ├─ +0x12a  NtOpenDirectoryObject(&h, 0x20001, "\RPC Control") 음수 → +0x2ed
      │           ├─ +0x158  NtQuerySecurityObject(h, 0x17, NULL, 0, &len)
      │           │            +0x164 == 0xC0000023 ? 아니면 → +0x2ed (0xe)     ← 에뮬레이터가 멈춘 곳
      │           ├─ +0x172  AllocWrapper(len)                                    (봉투 마련)
      │           ├─ +0x19c  NtQuerySecurityObject(h, 0x17, buf, len, &len)       (봉투 주고 다시)
      │           ├─ +0x1cc  OpenThreadToken  (없음 0x3f0 이면 +0x2d9 OpenProcessToken)
      │           ├─ +0x209  ntdll!RtlNewSecurityObjectEx(견본 = buf, 초안, …, 신분증)
      │           │            └─ RtlpNewSecurityObject → 신분증 질문 여러 개
      │           ├─ +0x224  RpcpCopySecurityDescriptor → gDefaultSecurityDescriptor
      │           └─ +0x22b  정리 (이름 해제 · 손잡이 놓기 · …)
      ├─ +0x2cf  ReadPolicySettings → 정책 레지스트리 열기
      └─ +0x412  열림 깃발 = 1                         (실패 길은 +0x425 MOV EBX,0xe)
```

- **읽는 순서**: 위에서 아래로 시간 순서다. 들여쓰기 한 칸 = "그 함수 안에서 부른 것".
- `+0x13c` 아래가 이번 이야기의 거의 전부다. `+0x158` 한 줄에서 에뮬레이터 길이 끊겼다.

### 2.2 ★ 한 줄씩 읽는 법 — `+0x164 CMP EAX,0xc0000023` / `+0x169 JNZ`

사용자가 가장 궁금해한 자리다. 아주 천천히 간다.

#### 먼저 그 자리의 세 줄

`ms/disassemble/rpcrt4/RpcpNormalizeSecurityDescriptor.asm` 에서 그대로 옮긴 세 줄 (주소 칸은 뺐다):

```
+0x158  CALL qword ptr [0x1800e96e0]    ; CALL ret=+0x15f -> NtQuerySecurityObject
+0x164  CMP EAX,0xc0000023
+0x169  JNZ 0x180038735
```

#### 새 말 — 같지 않으면 뛰기 (`CMP` 다음 `JNZ`)

x86 명령의 뜻이다 (일반 지식, 이 바이너리에 대한 사실이 아니라 읽는 법).

- `CMP A,B` = A 와 B 를 **견주어 보기만** 한다. 값은 안 바뀐다. "같다 / 다르다" 표시 (깃발) 만 세운다.
- `JNZ X` = 바로 앞 비교가 **같지 않으면** X 로 뛴다. 같으면 안 뛰고 **다음 줄로 그냥 내려간다.**
- 비유: 체크리스트 칸 하나. "대답이 '봉투가 작아요' 인가?" — **예** 면 다음 칸으로, **아니오** 면 맨 뒤 "포기" 칸으로 넘긴다.
- `JNZ` 뒤의 `0x180038735` 는 절대 주소다. 함수 기준으로는 **`+0x2ed`** 다 (결과서: "`+0x2ed` (= `0x180038735`)").

#### 줄 `+0x164` · `+0x169` 를 "누가 / 어디서 / 누구를 / 무엇을 위해" 로

- **누가**: `rpcrt4!RpcpNormalizeSecurityDescriptor` (rva `0x38448`) — 전화 부서의 기본 명부를 만드는 함수.
- **어디서**: `+0x158` 에서 `NtQuerySecurityObject` 를 부르고, 돌아온 바로 뒤 (`+0x15f`) 의 `+0x164` · `+0x169`.
- **누구를 (무엇을 보나)**: `EAX` — 방금 부른 `NtQuerySecurityObject` 의 **돌려준 값 (status)**. 견주는 상대는 **`0xc0000023`** 하나.
  - 길이 칸 `len` 은 **안 본다.** `len` 은 이 두 줄을 지난 다음 `+0x16f MOV ECX,[RBP-0x59]` 에서야 읽는다 (2.3).
- **무엇을 위해**: 첫 부름은 봉투 없이 (`NULL`, 0) **크기만** 물은 것이다. 그 질문에 맞는 대답은 "봉투가 작아요" 뿐이다. 그 대답이 왔는지만 보고, 아니면 바로 포기 칸으로 간다.

#### 새 말 — 포기 칸 (`+0x2ed`)

- `+0x2ed MOV EBX,0xe` → `+0x2f2 JMP` → `+0x22b` (정리) `[정적]`.
- 이 함수에서 실패하는 길은 **거의 다 이 한 자리로 모인다** — 손잡이 열기 실패 (`+0x138`), 첫 대답 (`+0x169`), 봉투 마련 실패 (`+0x17d`), 둘째 대답 (`+0x1aa`), 신분증 실패 (`+0x2bf` · `+0x2e7`), 새 명부 만들기 실패 (`+0x217`) (run 문서 "rpcrt4 쪽 검사 전부" 표).
- 그래서 어디서 실패했든 **같은 `0xe`** 가 나온다. 실패한 이유는 값에 안 남는다.
- 비유: 체크리스트 어느 칸에서 "아니오" 가 나와도 맨 뒤 **"포기 — 메모리 부족이라고 적기"** 칸 하나로 간다.

#### 에뮬레이터 대답이 `0xe` 가 되기까지 — 한 줄씩 `[정적]`

에뮬레이터 대답은 "status 0 + `len` 안 씀" 이었다 (에뮬레이터 쪽 관찰). 이 대답을 코드에 넣어 한 줄씩 따라간다.

1. **`+0x146`** — 부르기 전에 `len` 칸 (`[RBP-0x59]`) 을 **0 으로 지운다**.
   - 그래서 에뮬레이터가 `len` 을 안 써도 `len` 은 쓰레기가 아니라 0 이다.
2. **`+0x158`** — `NtQuerySecurityObject(h, 0x17, NULL, 0, &len)` 를 부른다.
   - 비트 `0x17` (`+0x154`), `NULL` (`+0x14c`), 0 (`+0x149`), `&len` (`+0x14f`) 모두 코드 상수다.
   - 에뮬레이터가 **`EAX = 0`** 으로 돌려준다.
3. **`+0x164 CMP EAX,0xc0000023`** — 0 과 `0xC0000023` 을 견준다 → **다르다.**
4. **`+0x169 JNZ 0x180038735`** — 다르니까 **뛴다** → `+0x2ed`.
   - `+0x16f` 이후 (길이 읽기 · 봉투 마련 · 둘째 부름 · 신분증 · 새 명부 만들기) 는 **한 줄도 안 탄다.**
5. **`+0x2ed MOV EBX,0xe`** — 돌려줄 값을 `0xe` 로 정한다 → `+0x2f2 JMP` → `+0x22b` 정리로.
6. **정리 길** (결과서 질문 2 "지금 우리" 줄) — 챙긴 것만 놓는다:
   - 이름 해제 (`+0x22e FreeWrapper`) — 힙 일이라 syscall 없음.
   - `+0x233` 에서 손잡이 칸을 읽고 → **`+0x23c CloseHandle(h)`** = `NtClose` **하나**.
   - 봉투는 NULL 이라 해제 건너뜀 (`+0x248 TEST RDI` · `+0x24b JZ`).
   - 신분증 손잡이는 0 이라 건너뜀 (`+0x259` · `+0x25c`).
   - 새 명부는 0 이라 `RtlDeleteSecurityObject` 건너뜀 (`+0x26a CMP [RBP-0x41],0` · `+0x26f JZ`).
   - `+0x2a5` 에서 돌아간다. 돌려주는 값 = `0xe`.
7. **`InitializeDefaultSecurityDescriptor`** — 받은 값을 그대로 옮긴다 (`+0x141 JMP +0x9a` → `MOV EBX,EAX` → `+0xb1 MOV EAX,EBX`).
8. **`PerformRpcInitialization +0x2b8 TEST EAX,EAX` · `+0x2ba JNZ`** — 0 이 아니니 **`+0x425 MOV EBX,0xe`** 로. (어떤 값이 와도 여기선 `0xe` 로 뭉친다.)
9. **`UuidToStringA +0x36 TEST EAX,EAX` · `+0x38 JZ +0x5d`** — 0 이 아니니 안 뛰고 `+0x3a ~ +0x5b` (쿠키 검사 · 마무리) 로 떨어진다. **EAX 를 안 건드린다** → 부른 쪽이 `0xe` 를 받는다.
10. **열림 깃발은 그대로 0** — `+0x412` 까지 못 갔으니까 (2.9).

#### 로그와 코드가 줄마다 같다

| 에뮬레이터 쪽 관찰 | 코드 `[정적]` |
|---|---|
| `NtQuerySecurityObject(h, 0x17, NULL, 0, &len)` | 위 2 (`+0x158`) |
| 그다음 `NtClose(h)` | 위 6 (`+0x23c CloseHandle(h)` 하나) |
| `NtClose` 와 반환 사이 syscall 0 | 위 6 · 7 · 8 · 9 — 정리 나머지는 다 건너뛰고, 위 단계들은 값만 옮김 |
| `UuidToStringA` = `0xe` | 위 5 · 8 |

- 그래서 run97 의 `[추측]` "0 대답이 `UuidToStringA → 0xe` 의 가장 가까운 원인" 은 **`[정적]`** 이 됐다 (run 문서 "확인된 것" 끝, flow.md 4장 run97 PE_emulator 줄의 run99 보탬).

#### 비유로 한 번 더

- 전화 부서 직원이 관리사무소 창구에 봉투 없이 묻는다. "`\RPC Control` 방 명부 사본, 크기가 얼마예요?"
- 진짜 창구라면: "봉투가 작아요. 이만큼 필요해요." → 직원은 그 크기 봉투를 마련해 다시 묻는다.
- 에뮬레이터 창구는: "네, 다 됐어요." (봉투도 없는데)
- 직원의 체크리스트 첫 칸: "'봉투가 작아요' 라고 했나?" → **아니오** → 포기 칸. "메모리 부족" 이라고 적고, 방 손잡이만 놓고 돌아간다.
- 직원은 "다 됐어요" 가 무슨 뜻인지 **따져 보지도 않는다.** 정해진 대답이 아니면 그냥 포기다.

### 2.3 "`len` 이 0 이라서" 가 아니다 — 읽는 차례

요청서는 "`len` 이 0 이라 할당 0 → 실패 같은 이유인가?" 하고 물었다. 답은 **아니다.**

#### 차례 `[정적]`

1. `+0x164` · `+0x169` — **status 만** 본다.
2. `+0x16f MOV ECX,[RBP-0x59]` — 여기서 **처음으로 `len` 을 읽는다.**
3. `+0x172 AllocWrapper(len)` — 그 크기로 봉투를 마련한다 (`HeapAlloc(hRpcHeap, 0, len)`, 결과서).

- 1 에서 이미 떨어졌으니 2 는 오지도 않는다. 그래서 지금 실패의 원인은 **`len` 이 아니라 status** 다.

#### status 만 고치면? — 다음 문제

status 만 `0xC0000023` 으로 바꾸고 `len` 은 그대로 0 이라면 (결과서 질문 2):

1. `+0x169` 통과.
2. `AllocWrapper(0)` — 진짜 힙은 0 바이트도 포인터를 돌려주므로 `+0x17d` 를 통과할 것이다 `[추측: 결과서]`.
3. 둘째 부름이 길이 0 으로 나간다 → 진짜 커널이라면 다시 `0xC0000023` (run97: 길이 0 으로 성공하는 길은 없음).
4. `+0x1aa JS` (음수면 뜀) → `+0x2ed` → 또 `0xe`.

- 그래서 **`len` 도 진짜 필요 길이** 여야 한다. status 와 길이는 **한 쌍** 으로 고쳐야 한다 (2.11 의 조건 3).
- 둘째 부름에서 `+0x1aa` 는 "음수인가" 만 본다. 첫째처럼 특정 값을 요구하지 않는다. 0 (성공) 이면 통과다 `[정적]`.

### 2.4 왜 하필 "정확히 `0xC0000023`" 인가 — 단계별로

#### 근거

- `[정적]` (run97) 진짜 커널은 크기만 물으면 **언제나** `0xC0000023` 이다. 필요 길이가 `0x14` 에서 출발하고 비교가 "크면" (`JA`) 이라, 받은 길이 0 이면 반드시 걸린다. 명부가 아예 없어도 필요 길이 `0x14` 를 쓴다.
- `[정적]` (run99) rpcrt4 는 `0xC0000023` 하나만 "예" 로 친다 (`+0x164` · `+0x169`).

#### 그래서

- 진짜 Windows 에선 첫 부름의 정상 대답이 **딱 하나** 다. rpcrt4 코드도 **그 하나만** 통과시킨다. 둘이 짝이 맞는다.
- "0 (성공) 이 오면 어떻게 하지?" 를 처리하는 줄은 없다. 진짜 커널에선 안 나오는 대답이기 때문이라고 짐작한다 `[추측]` (만든 사람의 뜻은 코드에 안 쓰여 있다).
- 에뮬레이터의 "0" 은 진짜에서 나올 수 없는 대답이라 (run97), rpcrt4 가 그걸 "이상한 대답 = 실패" 로 다룬 것이다.

#### 아직 확인 아님

- 진짜 실행에서 `+0x15f` 의 `eax` 가 `c0000023` 이고 `dwo(@rbp-0x59)` 에 필요 길이가 적히는지는 **동적** 으로 본다 (Q181, 4절 ②).

### 2.5 누가 여나 — 여는 함수는 하나

#### 새 말 — 가져오기 칸 (import 칸)

- 다른 DLL 함수의 주소를 담아 두는 칸이다. rpcrt4 는 `NtOpenDirectoryObject` · `NtQuerySecurityObject` 를 ntdll 에서 **직접 가져온다** (`__imp_` 칸 `0xe96e8` · `0xe96e0`) `[정적]`.
- 비유: 전화 부서 책상 위 **단축 번호 칸**. 이 번호를 누르는 자리를 다 찾으면, 그 창구에 가는 자리를 다 찾은 것이다.

#### 찾은 법과 결과 `[정적]`

- `NtOpenDirectoryObject` 칸을 읽는 줄 = **`RpcpNormalizeSecurityDescriptor +0x12a` 1곳** (`_refs/0xe96e8.tsv`).
- `NtQuerySecurityObject` 칸을 읽는 줄 = 같은 함수 `+0x158` · `+0x19c` 2곳뿐 (`_refs/0xe96e0.tsv`).
- 그래서 rpcrt4 안에서 **`\RPC Control` 을 여는 함수는 이것 하나** 다.
- `\RPC Control\` 글자 자체는 11곳에서 쓰지만, 나머지 (`TransformLrpcEndpointToAlpcPortName` · `AlpcConnect` · `InquireBinding`) 는 **이름 만들기만** 한다 (결과서 질문 1 표).

#### 이 함수를 부르는 곳 넷 `[정적]`

- `InitializeDefaultSecurityDescriptor +0x13c` — **초기화** (이번 길).
- `SetAuthInformation +0x4f6` · `UpdateRpcInterfaceInformation +0x59` · `LRPC_FAST_BINDING_HANDLE +0x38a` — **초기화 밖**. 바인딩에 인증을 붙일 때 · 서버 인터페이스를 등록할 때 같은 길로 보인다 `[추측: 이름]`.
- 에뮬레이터 로그에서 `\RPC Control` 열기가 초기화 말고 다른 때에도 보이면 이 셋 쪽이다. 어느 export 에서 닿는지는 Q186 ③.

#### 줄 `+0x12a` 한 줄 읽기

- **누가**: `RpcpNormalizeSecurityDescriptor`.
- **어디서**: `+0x12a` (ret `+0x131`).
- **누구를**: `NtOpenDirectoryObject(&h, 0x20001, &oa)` — 관리사무소에 "이 방 손잡이 주세요".
  - 손잡이 허락 `0x20001` (`+0x11c MOV EDX,0x20001`).
  - 이름표 묶음 (`OBJECT_ATTRIBUTES`): Length `0x30` (`+0x10a`), Root 0 (`+0xf6`), Attributes 0 (`+0xfe`), SD · QoS 0 (`+0x125`), 이름 `\RPC Control` (Length `0x18`).
- **무엇을 위해**: 방 명부 사본을 물을 **손잡이** 를 얻으려고. 음수면 `+0x136 TEST` · `+0x138 JS` → 포기 칸.
- 에뮬레이터 로그의 `NtOpenDirectoryObject("\RPC Control", 0x20001)` 와 값이 같다 → **이 줄까지는 맞다** (2.11).

### 2.6 이름 정하기 — 신분증 질문은 이름 앞만 가른다

에뮬레이터 로그에서 `NtOpenDirectoryObject` 바로 앞에 있던 `NtQueryInformationToken(-6, 0x1d, 4)` 의 정체다.

#### 새 말 — AppContainer

- 클래스 `0x1d` 의 이름은 `TokenIsAppContainer` 로 알려져 있다 `[추측: 공개 문서]` (숫자는 코드).
- AppContainer = 따로 울타리를 친 **샌드박스 가게** 정도로 이해하면 된다 `[추측]` (일반적으로 알려진 설명).
- `-6` 은 "현재 스레드의 유효 신분증" 을 가리키는 가짜 손잡이로 알려져 있다 `[추측: 공개 문서]`.

#### 누가 묻나 `[정적]`

- rpcrt4 가 직접 묻지 않는다. `RpcpNormalizeSecurityDescriptor +0xa8` 이 ntdll **`RtlGetAppContainerNamedObjectPath(0, 0, 0, &us)`** 를 부르고, 그 안 `+0xb6` 이 묻는다.
  - `+0x8c MOV RAX,-6` · `+0xa2 CMOVZ` (신분증 인자가 0 이면 `-6`), `+0x9f` 클래스 `0x1d`, `+0x93` 길이 4.
  - 값 칸 (`[RSP+0x30]`) 은 `+0x1e` 에서 **미리 0 으로 지운다**.

#### 세 갈래 `[정적]` (run 문서 표)

| 대답 | 그다음 | 방 이름 |
|---|---|---|
| status < 0 | `+0xbd JS` → 그 status 반환 | `\RPC Control\` |
| status ≥ 0, 값 = 0 | 빈 문자열, status 0 | `\RPC Control\` |
| status ≥ 0, 값 ≠ 0 (샌드박스 가게) | `+0x116 RtlpGetTokenNamedObjectPath` | `<샌드박스 경로>\RPC Control\` |

- 그래서 일반 가게에선 **어느 대답이든 이름은 같다.** 이 질문은 통과 · 실패를 **안 가른다.** 값 칸을 안 써도 미리 0 이라 괜찮다.
- **피해야 할 것은 하나** — 값 ≠ 0. 그러면 다른 이름의 방을 열러 간다.

#### 끝 `\` 떼기 `[정적]`

- 이름은 `DuplicateString3(앞, L"\RPC Control\", L"")` (`+0xd2`) 로 만든다. 끝에 `\` 가 붙어 있다.
- `+0x105 MOV EAX,0xfffe` · `+0x111 ADD word [RBP-0x39],AX` — 길이 칸에 `0xfffe` 를 더한다 = **2 바이트 (글자 하나) 깎기**. 그래서 실제로 여는 이름은 `\RPC Control` (Length `0x18`).
- 앞의 `+0x88 wcsstr(L"", L"\RPC Control\")` 는 빈 글에서 찾으니 **늘 NULL** 이다 (`+0x2b` 빈 문자열 `0x1800eb970`).

### 2.7 받은 명부는 안 읽는다 — 견본으로만 넘긴다

#### 새 말 — 부모 SD · 만든이 SD (견본 명부 · 초안 명부)

- ntdll `RtlNewSecurityObjectEx` 는 명부 두 개를 받아 **새 명부 하나** 를 만든다 `[정적: 인자 자리]`.
  - **부모 SD** = 위에서 물려받을 **견본 명부**. 이번엔 `\RPC Control` 방 명부 사본 (`buf`).
  - **만든이 SD** = 직접 쓴 **초안 명부**. 이번엔 rpcrt4 가 만든 것.
- 부모 쪽의 물려주기 줄이 새 명부에 들어간다는 건 일반적으로 알려진 설명이다 `[추측]`. 이번 근거에서 그 합치기 (`RtlpInheritAcl2`) 는 안 따라갔다 `[공백]`.
- 비유: 전화 부서 직원은 명부를 혼자 다 쓰지 않는다. 방 명부를 **견본** 으로, 자기 **초안** 과 가게 **신분증** 을 같이 관리실 (ntdll) 의 **명부 만들기 도우미** 에 넘긴다.

#### rpcrt4 는 견본 안을 안 본다 `[정적]`

- `RpcpNormalizeSecurityDescriptor` 의 호출 목록에 명부를 들여다보는 함수가 **하나도 없다**: `RtlGetOwnerSecurityDescriptor` · `RtlGetDaclSecurityDescriptor` · `RtlGetSaclSecurityDescriptor` · `RtlGetAce` · `RtlEqualSid` · `RtlValidSid` 0 (결과서 callee 목록, flow-writer 가 `.asm` 전부 읽어 같음).
- 받은 봉투는 `+0x1fc MOV RCX,RDI` 로 **그대로** `+0x209 RtlNewSecurityObjectEx` 의 첫 인자 (부모) 가 된다.
- 그래서 요청서가 물은 "주인 SID 비교 / 허락 쪽 줄 검사 / 신뢰 등급 표 비교" 는 **이 길에 없다.**
- 에뮬레이터에 주는 뜻: rpcrt4 쪽에선 명부 **값** 을 맞출 필요가 없다. **꼴** ([자기상대형](run97-rpc-sd-query-해설.md), 2.5) 만 맞으면 된다.

#### ntdll 은 견본에서 무엇을 보나 — 본 만큼만 `[정적]`

- `RtlpNewSecurityObject` (`RtlNewSecurityObjectEx +0x59` 에서 부름) 는 검증 함수를 안 부르고 견본의 `Control` (`+2`) 을 직접 본다.
  - `+0x35e MOVZX ECX,[R11+2]` · `+0x363 TEST CL,0x10` (`SE_SACL_PRESENT`) · `+0x366 JNZ`.
- 그 뒤 허락 쪽 · 특수 쪽 합치기 (`RtlpInheritAcl2`, 부르는 자리 `+0x45c` · `+0x61d` · `+0x956` · `+0xb4b`) 는 안 따라감 `[공백]`.
- run98 의 느슨한 명부에 있던 **상속 전용 줄 넷 (`0x0b`)** · 신뢰 등급 표가 전화 부서 기본 명부에 들어갈 수 있다 `[추측: 부모 SD 로 넘기므로]` (flow.md 3장 run98 절 run99 줄). 확인은 Q186 ①.

#### rpcrt4 가 직접 쓰는 초안 `[정적]`

- `InitializeDefaultSecurityDescriptor` 가 스택에서 만든다:
  - `+0x23` SID 바이트 = `S-1-5-7` (이름 "익명 로그온" 은 `[추측: 지식]`).
  - `+0xe4 AddAccessAllowedAce(acl, 2, 0x80000000, sid)` — `GENERIC_READ` 허락 한 줄.
  - `+0xfc` 빈 명부 → `+0x119 SetSecurityDescriptorDacl(sd, TRUE, acl, FALSE)`.
- 주인 · 소속 · 특수 쪽은 **없다.** 새 명부의 주인 · 소속은 신분증에서 온다 (2.10 의 클래스 4 · 5).
- 그리고 `+0x134 MOV EDX,1` 로 `AutoInheritFlags = 1` 을 넣어 `+0x13c` 에서 넘긴다.

### 2.8 `0xe` 는 어디서 정하나 — 상수, 바꿔 적기 없음

#### 정하는 자리 셋 `[정적]`

| 자리 | 언제 |
|---|---|
| `RpcpNormalizeSecurityDescriptor +0x2ed` | 창구 (NT 함수) · 신분증 · 봉투 마련 · 새 명부 실패 (2.2 포기 칸) |
| `RpcpNormalizeSecurityDescriptor +0x2a7` | 이름 만들기 (`DuplicateString3`) 실패 (`+0xda` · `+0xdd JZ`) |
| `PerformRpcInitialization +0x425` | `InitializeDefaultSecurityDescriptor` 가 0 이 아닌 **어떤 값이든** (`+0x2ba JNZ`). 그리고 앞 단계 실패 (`LoadLibraryExW` `+0x68` · `GetBasicSystemInfo` `+0x7a` · 할당 · 컴퓨터 이름 등, 결과서 표) |

- `UuidToStringA` 는 **안 만든다.** 옮기기만 한다 (2.2 의 9).

#### 바꿔 적기 (NTSTATUS → Win32) 가 없다 `[정적]`

- 이 사슬에 `RtlNtStatusToDosError` 호출이 없다.
- NTSTATUS 를 받는 자리 (`+0x136` · `+0x164` · `+0x1a8` · `+0x215`) 는 **부호나 값을 견주기만** 하고, 값은 버린 채 `0xe`.
- 예외 하나: kernelbase `OpenThreadToken` 은 실패하면 `+0x16 BaseSetLastNTError` 로 오류 코드를 바꿔 둔다. rpcrt4 는 그걸 `+0x2ae GetLastError` → `+0x2ba CMP EAX,0x3f0` 으로 본다 (2.10).

#### `0xe` 의 이름

- `0xe` = 14 = `ERROR_OUTOFMEMORY` (= `RPC_S_OUT_OF_MEMORY`) 로 알려져 있다 `[추측: 지식]`. 코드엔 이름이 없다.
- 이 사슬에선 **진짜 메모리 부족이라는 뜻이 아니다.** "이유와 상관없이 초기화 실패" 의 한 값이다 `[정적: 자리 셋 모두 같은 상수]`.
- 그래서 에뮬레이터에서 `0xe` 를 보면 "메모리" 를 찾지 말고 **초기화 안의 어느 칸이 "아니오" 였나** 를 찾아야 한다.

### 2.9 한 번 실패하면 매번 처음부터 — 열림 깃발

#### 새 말 — 열림 깃발 (`RpcHasBeenInitialized`)

- rpcrt4 전역 칸 하나 (`0x180104104`). "전화 부서가 열렸나" 를 적어 두는 깃발이다 `[정적: 칸 · 쓰는 줄]`.
- 비유: 부서 문에 거는 **"영업 중" 팻말**.

#### 켜지는 곳은 딱 한 곳 `[정적]`

- `PerformRpcInitialization +0x412 MOV dword [0x180104104],1` — **맨 끝, 성공했을 때만**.
- 입구에서 두 번 확인한다 (`+0xc` · `+0x2d`, 락 앞뒤). `UuidToStringA` 도 `+0x26` 에서 먼저 본다.
- 실패하면 팻말이 안 걸리니, **다음 export 가 또 `+0x39` 부터 같은 길** 을 탄다.

#### 그래서

- 에뮬레이터 로그의 "한 샘플에서 100번 넘게 되풀이 (가장 많이 3137)" (에뮬레이터 쪽 관찰) 와 맞는 코드다.
- 들어오는 문도 많다. `PerformRpcInitialization` 을 입구에서 부르는 **export 가 123개** 다 (caller 140곳 중 export 이름과 맞는 것) `[정적]`.
  - 예: `UuidToStringA` · `UuidFromStringA` (`+0xa9`) · `UuidCreateSequential` (`+0xd6`) · `RpcStringBindingComposeW` (`+0x115`) · `RpcBindingFromStringBindingW` (`+0x196`) · `RpcServerRegisterIf*` · `I_RpcGetDefaultSD` 등.
- 에뮬레이터 쪽에서 `WinHttpOpen` · `LookupPrivilegeValueW/A` · `gethostbyname` · `SHGetKnownFolderPath` · `CoInitializeSecurity` 안에서도 같은 꼴이 났다 (에뮬레이터 쪽 관찰). 그 모듈들이 이 123개 중 **무엇을** 부르는지는 안 봤다 `[공백]` → Q186 ④.

### 2.10 통과하면 다음 벽 — 신분증 질문, 그다음 설정 장부

첫 대답을 고치면 **처음 보는 syscall** 들이 나온다. ALPC 도 `\RPC Control\DSEC` 도 아니다. `[정적]`

#### 차례 (run 문서 "통과하면 다음" 표)

| 순서 | 어디 | syscall |
|---|---|---|
| ① | `RpcpNormalize +0x1cc` → kernelbase `OpenThreadToken +0x4` | `NtOpenThreadToken(현재 스레드, 8, TRUE)` |
| ② | (없음 `0x3f0` 일 때) `+0x2d9` → `OpenProcessToken +0x4` | `NtOpenProcessToken(현재 프로세스, 8)` |
| ③ | `+0x209 RtlNewSecurityObjectEx +0x59` → `RtlpNewSecurityObject +0x189` | `NtQueryInformationToken(tok, 10, 0x38)` |
| ④ | `RtlpNewSecurityObject +0x234` → `RtlpGetDefaultsSubjectContext` | 클래스 4 (`0x4c`, 한 번에) → `0x19` (`0x54`) → 5 (크기 → 다시) → 6 (크기 → 다시) |
| ⑤ | `RtlpGetDefaultsSubjectContext +0x203` → `RtlpGetDefaultTrustSubjectContext` | `(-4, 0x29, 0x4c)` → `(tok, 0x29, …)` |
| ⑥ | `RpcpNormalize +0x23c` · `+0x25e` | `NtClose(h)` · `NtClose(tok)` |
| ⑦ | `PerformRpcInitialization +0x2cf ReadPolicySettings` | `RegOpenKeyExW(HKLM, "Software\Policies\Microsoft\Windows NT\Rpc", 0, 0x20019)`. 어느 `Nt*Key*` 인지는 `[공백]` |

- 클래스 번호의 이름 (`10` = `TokenStatistics`, `4` = `TokenOwner`, `0x19` = `TokenIntegrityLevel`, `5` = `TokenPrimaryGroup`, `6` = `TokenDefaultDacl`, `0x29` = `TokenProcessTrustLevel`) 과 `-4` = "현재 프로세스 신분증" 은 `[추측: 공개 문서]`. 숫자는 코드.

#### 읽을 곳 셋

- **신분증 없음이 정상** — ① 에서 스레드 신분증이 없으면 kernelbase 가 오류 코드를 `0x3f0` 으로 바꿔 둔다 (`ERROR_NO_TOKEN`, 이름은 결과서). rpcrt4 는 **`0x3f0` 일 때만** ② 로 넘어간다 (`+0x2ba CMP EAX,0x3f0` · `+0x2bf JNZ`). 다른 실패면 포기 칸.
- **크기를 안 묻는 질문도 있다** — ④ 의 클래스 4 · `0x19` 는 정해진 크기 (`0x4c` · `0x54`) 로 **한 번에** 묻는다. 대답이 그 안에 다 들어가야 한다.
- **크기를 묻는 질문은 여기서도 "정확히 `0xC0000023`"** — 클래스 5 · 6 은 `+0x13c CMP EAX,0xc0000023` · `+0x141 JNZ`, `+0x1af CMP` · `+0x1b4 JNZ`. 2.2 와 **같은 꼴** 이다. 에뮬레이터가 여기서도 0 으로 답하면 같은 식으로 떨어진다.

#### 다음 벽이 아닌 것 둘 `[정적]`

- **ALPC (전화선 끝)** — 이 초기화 지도 71 함수에 `NtAlpc*` · `NtCreateSection` 이 0 이다. ALPC 는 `AlpcConnect` (`NtAlpcConnectPortEx` · `NtAlpcConnectPort`) 와 서버 쪽에만 있다 (이름만).
- **`\RPC Control\DSEC`** — `GenerateSectionName +0x25` 하나가 쓰고, `AllocateCellSection +0x13f NtCreateSection` 으로 간다. 이 길은 디버그 정보 정책이 켜졌을 때만 탄다 (`+0x32e` · `+0x33a` → `+0x344 InitializeCellHeap`). 보통은 꺼져 있을 것이다 `[추측: 결과서, 전역 처음 값 0]`.

#### 그 뒤

- `+0x2fb SbSelectProcedure` → 돌려준 주소가 0 이 아니면 `+0x313` 에서 **교환원 (CFG 간접 호출)** 으로 부른다. 대상은 실행 때 정해진다 (run 문서 보탬 ④).
- 이어서 `WinSqmIsOptedIn` · ETW 등록 · `GetRpcIdleTimerWindow` → `+0x412` 팻말 걸기. 이 뒤 syscall 이름은 안 따라갔다 → Q186 ②.

### 2.11 ★ PE_emulator 쪽 — 맞나, 틀리나, 무엇을 줘야 하나

#### 맞나 · 틀리나 (run 문서 표)

| 질문 | 진짜 (26100.9457) `[정적]` | 지금 우리 | 판정 |
|---|---|---|---|
| 1 누가 여나 | `RpcpNormalizeSecurityDescriptor +0x12a` 하나 | 로그 순서가 코드와 같음 | **맞음** |
| 2 명부에서 무엇을 보나 | 두 번 묻기. 첫 대답은 `0xC0000023` 만 통과. 안은 안 읽음 | status 0 + `len` 안 씀 | **틀림** |
| 3 언제 `0xe` | 상수 셋, `UuidToStringA` 는 옮기기만 | `0xe` 반환 | **맞음** (코드대로) |
| 4 통과 뒤 다음 | 신분증 열기 · 묻기 → 정책 장부 | 아직 안 나옴 | **다음 벽** — 구현 상태는 PE_emulator 쪽 확인 `[공백]` |

- 즉 **틀린 곳은 한 군데** — 첫 `NtQuerySecurityObject` 대답이다. `0xe` 자체는 코드가 시킨 대로 나온 것이다.

#### 최소 조건 — 대표만 (14줄 전부는 run 문서 "최소 조건 표")

| # | syscall | 꼭 줘야 하는 것 | 틀리면 |
|---|---|---|---|
| 1 | `NtQueryInformationToken(-6, 0x1d, 4)` | 아무 status. **값 ≠ 0 만 피하기** | 다른 이름의 방을 열러 감 |
| 2 | `NtOpenDirectoryObject(0x20001, "\RPC Control")` | status ≥ 0, 쓸 수 있는 손잡이 | `0xe` (`+0x138`) |
| **3** | **`NtQuerySecurityObject(h, 0x17, NULL, 0, &len)`** | **status = `0xC0000023` 정확히 + `*len` = 진짜 필요 길이** (run97: `0x14` 이상) | **`0xe` (`+0x169`) — 지금 여기** |
| 4 | (봉투 마련, syscall 아님) | NULL 아닌 포인터 | `0xe` (`+0x17d`) |
| **5** | **`NtQuerySecurityObject(h, 0x17, buf, *len, &len)`** | **status ≥ 0, `buf` = 자기상대형 명부** (`Revision 1` · `Control` 에 `0x8000`, run97 "에뮬레이터용 규칙 한 장") | `0xe` (`+0x1aa`) |
| 6 · 7 | `NtOpenThreadToken` / `NtOpenProcessToken` | 스레드는 "없음" (→ `0x3f0`) 또는 성공, 프로세스는 성공 + 손잡이 | `0xe` |
| 8 ~ 13 | `NtQueryInformationToken` 클래스 10 · 4 · `0x19` · 5 · 6 · `0x29` | 다 성공. 10 의 `TokenType` (칸 `+0x18`) = 1. 5 · 6 은 크기만 물을 때 `0xC0000023` + 길이 | 실패 |
| 14 | `NtClose` ×2 | 아무 | — |

- **한 줄 요약** (결과서): 3 만 정확히 `0xC0000023` + 길이, 5 가 명부를 채워 성공, 그리고 6 ~ 13 의 신분증 질문이 다 성공.
- **3 을 고치면 4 ~ 14 가 처음으로 나온다.** 그 구현이 있는지가 다음 벽이다.
- 명부 안에서 rpcrt4 가 꼭 맞춰야 하는 값은 **없다** (2.7). ntdll 쪽은 꼴이 옳아야 하고, 특수 쪽이 없으면 `Control` 의 `0x10` 을 꺼 두면 `+0x363` 줄을 안 탄다 (결과서).

#### 따로 안 따라간 조건부 syscall

- `RtlpNewSecurityObject` 의 callee 에는 `NtDuplicateToken` · `NtAccessCheck` · `NtPrivilegeCheck` (두 곳) · `RtlAddMandatoryAce` 가 있다.
- rpcrt4 의 초안 (허락 한 줄, 주인 · 소속 · 특수 쪽 없음) 으로는 안 탈 것으로 보인다 `[추측: 결과서]`. 조건을 끝까지 안 봤다 → Q186 ① (정적) · Q181 ⑥ (동적).

### 2.12 test.exe 길에 있나 — 없다. 그래서 그림은 그대로

- `[관찰]` (run44) 초기화 끝 (P2) 모듈 6개 `test.exe` · `ntdll.dll` · `KERNEL32.DLL` · `KERNELBASE.dll` · `dll1.dll` · `dll2.dll` (`ms/logs/04_baggage-read_P2.txt:3398 · 3503 · 3608 · 3713 · 3818 · 3923`). rpcrt4 없음.
- `[관찰]` (run44) `rpcrt4.dll` 은 `\KnownDlls` 섹션 목록 (`_P2.txt:1392`) 에만 있다 — 공용 창고 선반에 놓여 있을 뿐.
- `[정적]` 이번에 읽은 ntdll 함수 다섯도 test.exe 초기화 지도 (`_map/LdrInitializeThunk_*`) 에 0 건 (flow-writer 검색).
- 그래서 flow.md 그림엔 안 넣었다. run91 · run92 (bcrypt) 와 같은 까닭이다 (flow.md 흐름도 앞 글 run99 문단).

### 2.13 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 "결과서와 다르게 적은 곳 · 보탠 것" ① ~ ⑦. 이 글은 run 문서 쪽을 따랐다.

- ① (다르게) 결과서 질문 4 ⑤ 의 `+0x203` → 부르는 쪽은 **`RtlpGetDefaultsSubjectContext +0x203`** 이다 (함수 이름이 빠져 있었다). 2.10 표도 이걸 따랐다.
- ② (다르게) 결과서 동적 6 의 `RtlpNewSecurityObject+0x461` · `+0x1121` · `+0x144a` 는 **ret 자리**. "부르기 직전" 이면 call 자리 `+0x45c` · `+0x111c` · `+0x1445` · `+0x1498`. 같은 callee 를 부르는 자리가 더 있다 (`+0x61d` · `+0x956` · `+0xb4b` · `+0x116b` · `+0x1b48`) (2.14).
- ③ (작게) `OpenThreadToken` 이 `BaseSetLastNTError` 를 부르는 call 은 `+0x16` (`+0x14` 는 `MOV ECX,EAX`).
- ④ (보탬) `PerformRpcInitialization +0x313` 은 CFG 간접 호출 (`SbSelectProcedure` 가 돌려준 주소).
- ⑤ (보탬) `RpcpNormalizeSecurityDescriptor` refs 에 caller 4곳 말고 DATA 1줄 (`0x106e48`) — 예외 · 함수 범위 표 칸일 것 `[추측]`. 결론엔 영향 없음.
- ⑥ (보탬) `rpcrt4.dll` 은 run44 `\KnownDlls` 섹션 목록에 있다 `[관찰]` (2.12).
- ⑦ (도구) `_calls.tsv` 의 `alias:PerformRpcInitialization+1 ; ALIASES(1): ?…@@YAJXZ` 는 C++ 꾸밈 이름 하나일 뿐, 별칭 무리가 아니다 (`pdb_aliases.py --lookup` 확인).

#### run98 에 보탠 것 둘 (run98 해설이 짚은 것)

- run98 의 "`KnownDllPath` 링크는 디렉터리를 새로 만들었을 때만" 은 `.asm` 으로도 맞다 — `smss!SmpOpenKnownDllsHandles +0x19d CMP EAX,0x40000000` · `+0x1a2 JZ → +0x219`. flow.md 2.4 run65 (run98) 줄 근거가 `.c` → `.asm` 으로 바뀌었다.
- flow.md 3장 run98 절 비유 "주인 칸은 … 시청 (커널) 이 채운다" 옆에 `[추측]` 이 붙었다. Q180d (run100) 에서 정적으로 닫힐 예정.

### 2.14 디버거 메모 — 함정 둘

- **"부르기 직전" 은 call 자리로, 같은 callee 는 모든 자리에** — 결과서 멈춤점 목록에 ret 자리 (`+0x461` 등) 가 섞여 있었다. `.asm` 의 `CALL … ret=` 줄에서 **call 자리** 를 골라야 한다. 그리고 같은 함수를 부르는 자리가 여럿이면 **다 걸어야** "안 걸림 = 안 탐" 이라고 말할 수 있다 (run 문서 "함정 후보").
- **디버거가 붙으면 갈래가 바뀔 수 있다** `[추측]` — `PerformRpcInitialization` 은 PEB `+0xbc` 의 검증기 · 페이지힙 비트가 켜졌을 때만 `InitializeRpcVerifier` 를 탄다. 디버거 아래에서 이 비트 (`NtGlobalFlag`) 가 달라지면 길이 달라질 수 있으니, 동적 때 `dt nt!_PEB @$peb NtGlobalFlag` 를 같이 찍자는 메모다.

### 2.15 정리 — 비유로 한 번 더

- 어떤 가게가 처음으로 다른 가게에 전화를 걸려고 한다 (`UuidToStringA` 등 123개 문 중 하나).
- 가게 안 **전화 부서** 가 아직 안 열렸다 (팻말 없음). 그래서 부서를 차린다 (`PerformRpcInitialization`).
- 차리는 일 중 하나가 **부서 기본 명부** 만들기다.
  - 직원은 초안을 쓴다: "익명 손님 — 읽기만".
  - 견본으로 쓰려고, 관리인이 부팅 때 만든 **`\RPC Control` 방** 의 손잡이를 받는다.
  - 관리사무소 창구에 봉투 없이 "명부 사본 크기가 얼마예요?" 하고 묻는다.
- **진짜 창구** 는 "봉투가 작아요, 이만큼요" → 직원은 봉투를 마련해 다시 묻고, 받은 사본은 **읽지도 않고** 초안 · 신분증과 함께 관리실 (ntdll) 의 도우미에게 넘긴다. 도우미는 신분증을 이것저것 물어보고 (클래스 10 · 4 · `0x19` · 5 · 6 · `0x29`) 새 명부를 만든다.
- **에뮬레이터 창구** 는 "다 됐어요" → 직원의 체크리스트 첫 칸에서 "아니오" → **포기 칸**. "메모리 부족" 이라고만 적고, 방 손잡이만 놓고 돌아간다.
- 부서장 (`PerformRpcInitialization`) 도, 가게 (`UuidToStringA`) 도 그 쪽지를 그대로 넘긴다. 팻말은 안 걸린다.
- 다음에 또 전화를 쓰려 하면 **처음부터 다시**. 그래서 로그에 같은 꼴이 수천 번 나왔다.
- 고칠 곳은 창구의 **첫 대답 한 마디** ("봉투가 작아요" + 크기) 다. 그걸 고치면 다음은 **신분증 질문** 들이 기다린다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

**그림 (1장) 은 안 바뀌었다.** test.exe 는 rpcrt4 를 안 올려서 이 사슬이 test.exe 초기화 지도에 없다 (2.12). 태그가 올라간 곳은 **하나** — run97 의 `[추측]` 이 `[정적]` 이 됐다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | rpcrt4.pdb 줄 없음 | **rpcrt4.pdb 줄 새로** (`0204A8A137C42B7DF1F0811A3A8277B21`, 처음 analyze, 함수 3289, 지도 둘, export depth 1 셋 · depth 0 9, `refs` 7). ntdll · kernelbase 줄에 run99 몫 (읽기 전용) |
| 근거 로그 | run98 까지 | **run99 줄** — 정적 바퀴, 값 대조 없음, P2 모듈 6개 · `\KnownDlls` 섹션 목록만 다시 읽음, 에뮬레이터 숫자는 우리 `[관찰]` 아님 |
| 흐름도 앞 글 | run98 문단까지 | **run99 문단** — "그림을 안 고쳤다" 와 그 이유 |
| 2.4 run65 `\KnownDlls` 절 (run98) 줄 | 링크 건너뛰기 근거 `.c` | 근거 **`.asm`** (`SmpOpenKnownDllsHandles +0x19d` · `+0x1a2`) |
| 3장 run97 "보안 정보 묻기" 절 | 크기만 묻기 규칙까지 | **(run99) `[정적]` 한 줄** — 이 묻기를 부르는 쪽은 `RpcpNormalizeSecurityDescriptor` 하나, 첫 대답은 정확히 `0xC0000023` 만 통과 |
| 3장 run98 smss 절 | 비유 "시청이 채운다" (태그 없음) | 비유 옆 **`[추측]`**, **(run99) `[정적]` 한 줄** — rpcrt4 는 이 명부를 견본 (부모) 으로만 쓴다 |
| 3장 | (없음) | **새 절 "RPC 런타임 초기화가 `\RPC Control` SD 를 빌려 기본 SD 를 만든다"** — 사무실 · 게시판 비유, 호출 트리, `[정적]` 일곱 줄 |
| 4장 run97 PE_emulator 줄 | "`UuidToStringA → 0xe` 의 가장 가까운 원인 후보 **`[추측]`**" | **(run99 보탬) `[정적]` 으로 섰다** — `+0x164` · `+0x169` → `+0x2ed`, `+0x425` · `UuidToStringA` 가 옮김 |
| 4장 run97 동적 줄 | 커널 쪽 멈춤점으로 SD 바이트 | **(run99 보탬)** 유저 쪽 `rpcrt4!…+0x15f` · `+0x1a3` 로 더 쉽게 |
| 4장 run98 정적 줄 ⑤ | Q180c (rpcrt4 가 무엇을 보나) | **해결됨 → run99** — 안 본다, 견본으로 통째로 넘김 |
| 4장 | — | **run99 새 줄 셋** — 동적 (→ Q181) · 정적 (→ Q186) · PE_emulator 와 다르게 적은 곳 |
| 5장 | run98 까지 | run 표 · 요청서 번호 **run99 줄** |

### 짐 지도 `baggage.md` — 안 고침

- 이번 사슬이 놓는 것 — rpcrt4 전역 `gDefaultSecurityDescriptor` · 열림 깃발 `RpcHasBeenInitialized`, 힙의 명부 사본 — 은 **test.exe 초기화 짐이 아니다.** test.exe 는 rpcrt4 를 안 올리기 때문이다 (run 문서 "flow.md · 짐 지도에 반영한 것").

### 큰 그림에서 어떻게 이어지나

- **PE_emulator 쪽** — 요청 34 의 세 조각이 한 줄로 이어졌다.
  - run98: 관리인이 **원본 명부** 를 붙인다.
  - run97: 관리사무소가 사본 요청에 **이렇게 답해야** 한다 (`0xC0000023` + 길이, 그다음 한 덩어리 꼴).
  - run99: 받는 쪽은 **첫 대답 한 마디** 만 보고 갈린다. 지금 에뮬레이터의 `0xe` 는 거기서 나온다.
  - 남은 한 칸: 넘긴 명부가 방에 **붙을 때** 커널이 무엇을 바꾸나 → Q180d (run100 예정). 그게 정해지면 조건 5 의 "명부 바이트" 기대값이 정해진다.
- **고친 다음의 벽** — 신분증 질문 (클래스 10 · 4 · `0x19` · 5 · 6 · `0x29`) 과 정책 장부 열기. 에뮬레이터가 이걸 이미 하는지는 PE_emulator 쪽에서 확인할 일이다 `[공백]`.
- **우리 지도 쪽** — test.exe 줄기에 더할 건 없다. 대신 "test.exe 가 rpcrt4 를 쓴다면" 의 초기화 입구가 처음으로 정적으로 그려졌다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q181** (동적, run99 몫 ① ~ ⑩)

이번 바퀴엔 VM 을 안 썼다. Q181 은 Q104 VM 단계에 묶을 후보다. **test.exe 로는 못 본다** (rpcrt4 를 안 올림 `[관찰]` run44) — `UuidToStringA` 나 `RpcStringBindingComposeW` 를 부르는 **샘플이 따로** 필요하다. 커널 쪽 멈춤점은 프로세스 조건 필수.

| 번호 | 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|---|
| ① | `+0x12a` 직전 이름이 정말 `\RPC Control` (Length `0x18`) · `rdx` = `0x20001` 인가 | 정적은 코드 상수까지. 실제 값은 실행 때 |
| ② | **`+0x15f`** 의 `eax == c0000023` · `dwo(@rbp-0x59)` 필요 길이, **`+0x1a3`** 의 `!sd @rdi` — **진짜 `\RPC Control` 명부 바이트** (유저 쪽에서 뜸, run98 느슨한 명부 · run97 바이트 규칙과 대조) | 객체에 실제로 붙은 명부는 실행 때 값 (커널 저장 계산은 Q180d) |
| ③ | `+0x1d3` (`OpenThreadToken` 0 ?) · `+0x2b5` (`0x3f0` ?) — 스레드 신분증 없음 가정 | 실행 때 신분증 상태 |
| ④ | `ntdll!RtlGetAppContainerNamedObjectPath+0xbb` 의 `eax` · `dwo(@rsp+0x30)` 이 0 인가 | 〃 |
| ⑤ | 신분증 질문들의 진짜 대답 — `RtlpNewSecurityObject+0x18e` (`TokenType`), `RtlpGetDefaultsSubjectContext+0xc0` · `+0x115` · `+0x13a` · `+0x188` · `+0x1ad` · `+0x1f3`, `RtlpGetDefaultTrustSubjectContext+0x74` · `+0xa7` | 〃 |
| ⑥ | 조건부 syscall 이 이 입력에서 타나 — **call 자리 전부** `RtlpNewSecurityObject+0x45c` · `+0x61d` · `+0x956` · `+0xb4b` · `+0x111c` · `+0x116b` · `+0x1445` · `+0x1498` · `+0x1b48` | 조건을 정적으로 끝까지 안 봤다 |
| ⑦ | `RpcpNormalize+0x210` (`eax`, 새 명부 `poi(@rbp-0x41)`) · `+0x229` (`eax == 0`) | 실행 때 |
| ⑧ | `PerformRpcInitialization+0x2b8` · `+0x2d4` · `+0x412` 에서 팻말이 1 로 (`ba w4 rpcrt4+0x104104`) | 〃 |
| ⑨ | `+0x344 InitializeCellHeap` 가 **안 걸리는지** — 걸리면 `\RPC Control\DSEC` 가 정상 길에 있다는 뜻 | 정책 값은 실행 환경 몫 |
| ⑩ | (보탬) `+0x313` 교환원 호출의 대상 — `ln poi(rpcrt4+0x103e08)` | CFG 간접 대상은 실행 때 |

### 정적으로 남은 것 → **Q186 ① ~ ④** (그리고 Q180d)

run 문서 "아직 확인 필요 — 정적" ① ~ ⑤ 가 대기열에서 이렇게 갔다.

| run 문서 | 대기열 | 무엇 | 왜 이번에 못 봤나 |
|---|---|---|---|
| ① | **Q186 ①** | ntdll `RtlpNewSecurityObject` 조건부 갈래 (`NtPrivilegeCheck` · `NtDuplicateToken` · `NtAccessCheck` · `RtlAddMandatoryAce`) 와 `RtlpInheritAcl2` — 느슨한 명부의 `0x0b` 줄 넷 · 신뢰 등급 표가 전화 부서 기본 명부에 들어가나 | `RtlpNewSecurityObject` (7338 B) 는 앞쪽 + syscall 자리만 봤다 |
| ② | **Q186 ②** | 초기화 뒷부분 syscall — kernelbase `RegOpenKeyExW` · `RegGetValueW` 의 `Nt*Key*`, `WinSqmIsOptedIn`, `EtwEventRegister` (`PerformRpcInitialization +0x2cf ~ +0x412`) | kernelbase 안이라 안 따라감 |
| ③ | **Q186 ③** | `RpcpNormalizeSecurityDescriptor` 다른 caller 셋이 어느 export 에서 닿나 | 이번 범위는 초기화 길만 |
| ④ | **Q186 ④** (낮음, 새 analyze) | advapi32 · winhttp · ws2_32 · shell32 · combase 가 rpcrt4 의 어느 export 를 부르나 | 요청서가 범위 밖으로 둠 |
| ⑤ | **Q180d** (sast/96 → run100 예정 — 이름만) | 커널이 느슨한 명부를 객체에 붙이는 꼴 — 위 동적 ② 의 기대값 | 같은 요청의 다른 조각 |

### 같은 요청의 다음 조각 — 다음 바퀴에서 이어짐

- **Q180d** (run100) — 넘긴 명부가 **방에 붙을 때** 의 계산. 이번 이름만.

---

## 5. 더 읽기

- run 문서: [`run99-rpc-caller.md`](run99-rpc-caller.md)
  - "PE_emulator 쪽에서 쓸 것" — 결론 · **최소 조건 표 (14줄)** · 맞나 틀리나 표 · 원문 질문이 어디서 답해졌나
  - "정적 — 호출 지점" (call · ret 자리 전부) · "`NtQueryInformationToken(-6, 0x1d)` 갈림" · "rpcrt4 쪽 검사 전부" · "`0xe` 는 어디서 정하나" · "통과하면 다음" · "호출 관계"
  - "결과서와 다르게 적은 곳 · 보탠 것" · "run98 에 보탠 것" · "아직 확인 필요" (동적 ① ~ ⑩, 정적 ① ~ ⑤)
- 결과서: [`_workspace/sast/95_rpc-caller_result.md`](../../_workspace/sast/95_rpc-caller_result.md) — "한눈에", 질문 1 ~ 5, "동적으로 확인할 것", "다음 요청 후보"
- 요청서: [`_workspace/sast/95_rpc-caller.md`](../../_workspace/sast/95_rpc-caller.md) — 배경 (에뮬레이터 쪽 관찰)
- flow.md: [`flow.md`](flow.md)
  - 3장 "RPC 런타임 초기화가 `\RPC Control` SD 를 빌려 기본 SD 를 만든다 — … (run99)"
  - 3장 "보안 정보 묻기" 절 · "부팅 때 smss 가 만드는 객체 디렉터리" 절의 (run99) 한 줄씩
  - 4장 run97 줄의 (run99) 보탬, run98 정적 줄 ⑤, run99 새 줄 셋, 5장 run99 줄
- 앞 해설:
  - [run97 해설](run97-rpc-sd-query-해설.md) — 출입 명부 · "봉투가 작아요" (2.4) · 한 덩어리 꼴 (2.5)
  - [run98 해설](run98-rpc-dir-create-해설.md) — 관리인 smss · 느슨한 명부 · 상속 전용 줄
  - [run68 해설 2.4](run68-dll-init-rest-해설.md) — ALPC 포트 (전화선 끝)
  - [run96 해설](run96-bcrypt-interface-해설.md) — CFG 간접 호출 (교환원)
  - [run49 해설](run49-kprocess-token-htable-해설.md) — 신분증 (토큰)

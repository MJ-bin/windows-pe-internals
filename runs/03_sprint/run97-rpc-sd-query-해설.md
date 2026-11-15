# run97 rpc-sd-query — 해설

> 이 글은 [run97 문서](run97-rpc-sd-query.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q180a** — (Q180 ①) ntoskrnl "보안 정보 묻기" 길: 비트 → 권한, LABEL 만 물을 때 고르는 ACE · SACL 머리, 바이트 꼴, 크기만 묻기 status · `*LengthNeeded`, Directory · Token 형의 `SecurityProcedure`, 토큰 객체 SD 붙이는 자리 (이름만) (`_workspace/plan.md`, 이번에 **끝**)
  - **PE_emulator 요청 34 의 첫 조각**이다. 사용자 결정 (2026-11-15) 으로 큰 요청 Q180 을 Q180a · Q180b · Q180c · Q180d 로 나눴다.
  - 같은 요청의 다음 조각은 **다음 바퀴에서 이어진다**: Q180b (smss, run98) · Q180c (rpcrt4, run99) · Q180d (새 객체 SD, run100).
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → **Q181** (run97 몫 ① ~ ⑧)
    - 정적 → 새 **Q184 ① ~ ④**, 그리고 Q180d (새 객체 SD 의 라벨)
- 근거:
  - run 문서 [`run97-rpc-sd-query.md`](run97-rpc-sd-query.md) (가장 중요)
  - 요청서 [`_workspace/sast/93_rpc-sd-query.md`](../../_workspace/sast/93_rpc-sd-query.md) · 결과서 [`93_rpc-sd-query_result.md`](../../_workspace/sast/93_rpc-sd-query_result.md)
    - 판정 **분석성공**. 요청서 질문 1 ~ 6 모두 `.asm` 줄 근거로 답했다.
    - 못 닫은 것: 진짜 객체 SD 값, 형 객체 `+0x98` 의 실행 때 값, 입구 예외 때 돌려주는 값, 간접 호출 대상.
    - ntoskrnl 은 다시 analyze 하지 않았다 (읽기 전용 export 와 map 만).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · 흐름도 앞 글 · 2.1 한 줄 · **3장 새 절** · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): **안 고침** (3절)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행도, 사진 다시 읽기도 없다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **SD (보안 설명자)** · **무결성 라벨** — 2.0 (사용자가 꼭 짚어 달라고 한 둘)
- SD 안의 칸 이름 (**소유자 · 그룹 · DACL · SACL · ACE · SID**) — 2.0
- **보안 질문 체크박스** (`SecurityInformation` 비트) · **손잡이 허락 목록** (핸들 권한) — 2.0
- **self-relative** (한 덩어리 꼴) — 2.5
- **형마다 보안 담당** (`SecurityProcedure`) — 2.7

### 이번 바퀴에서 조심할 곳 넷

1. **test.exe 는 이 길을 안 탄다** (2.9).
   - test.exe 초기화 지도에는 `NtQuerySecurityObject` 를 부르는 줄이 없다 `[정적: 지도]`.
   - test.exe 는 rpcrt4 를 안 올린다 (run44 모듈 6개 `[관찰]`).
   - 그래서 이번 이야기는 **"rpcrt4 를 쓰는 다른 프로그램이 커널에 묻는다면"** 이다. 흐름도 **그림은 그대로** 다.
2. **에뮬레이터 쪽 숫자는 우리 `[관찰]` 이 아니다.**
   - `0x17` · `0x20001` · `0xa0008` · 383 샘플 같은 값은 요청서 배경, 즉 **PE_emulator 로그**에서 왔다.
   - 진짜 Windows 를 디버거로 본 게 아니다. 이 글에서도 태그 없이 "에뮬레이터 쪽 관찰" 로만 부른다.
3. **`+0x` 가 세 종류 나온다.**
   - `NtQuerySecurityObject +0x145` 처럼 **함수 이름 뒤** = 그 함수 **코드 안 자리**.
   - "형 객체 `+0x98`" · "초기화 구조체 `+0x58`" = **구조체 안 칸 자리**.
   - "SD 머리 `+4` · `+8` · `+0xc` · `+0x10`" = 커널이 만들어 주는 **SD 바이트 안 칸 자리** (2.5).
4. **비유가 둘이다.**
   - flow.md 3장 새 절은 **"금고 · 출입 규칙표 · 출입증 · 도장 · 봉투"** 비유다.
   - 이 글은 앞 해설들을 이어서 부른다. 핸들은 run49 해설 결정대로 **손잡이** (출입증 아님), 토큰은 **신분증** 이다. 그림만 다르고 사실은 같다 (아래 "비유" 표).

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 프로세스 = **가게**, 핸들 = **손잡이** → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 토큰 = 가게의 **신분증** → [run31 해설](../02_sprint/run31-image-section-해설.md) · [run49 해설](run49-kprocess-token-htable-해설.md) "비유"
- 이름 있는 객체 = 관리사무소 안 **이름표 붙은 공용 사물함**, 디렉터리 = 사물함이 모인 **방** → [run71 해설 2.2](run71-named-obj-dir-해설.md)
- 객체 형식 (섹션 · 파일 · 디렉터리 · 토큰 같은 "종류") 과 `ObCreateObjectEx` (**공용 서류철 발급 창구**), 형식마다 등록해 둔 담당 함수 (parse 프로시저) → [run47 해설](run47-section-chain-해설.md) 2.5 (`ObCreateObjectEx`) · 2.10 (parse 프로시저)
- 이전 모드 (`PreviousMode`, 이 시스템 콜을 유저가 냈나 커널이 냈나) → [run29 해설 2.6](../02_sprint/run29-continue-start-해설.md)
- 유저가 준 칸을 "한 번 읽고 되써 보기" (써도 되는 칸인지 미리 확인) → [run13 해설 2.7](../02_sprint/run13-resume-thread-해설.md) "`NtResumeThread` 앞뒤 자잘한 일"
- 예외 보호 구역 (`__try`) = 사고 나면 정해진 **착지점** 으로 → [run17 해설 2.8](../02_sprint/run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일) · [run81 해설](run81-exception-kernel-해설.md)
- CFG 간접 호출 = 번호를 보고 이어 주는 **교환원** → [run96 해설](run96-bcrypt-interface-해설.md) "이번 글의 새 말"

flow.md 비유와 이 글 비유를 맞춰 보면:

| flow.md 3장 새 절 | 이 글 | 실제 |
|---|---|---|
| 금고 | 공용 사물함 (방 = 디렉터리) · 신분증 (토큰) | 객체 |
| 출입 규칙표 | **출입 명부** (2.0) | SD |
| 출입증 | **손잡이** | 핸들 |
| 도장 ("규칙표 열람") | 손잡이에 적힌 **허락 목록** 의 한 줄 | `READ_CONTROL` 등 핸들 권한 |
| 봉인된 감사 장부 | 출입 명부의 **특수 쪽** | SACL |
| 봉투 | 봉투 | 출력 버퍼 |

### 한 줄로 먼저 — 무엇을 얻었나

**이번 글의 중심은 이 두 줄이다.** `[정적]`

> **"크기만 알려 줘" 로 물으면, 진짜 커널은 "봉투가 작아요" (`0xC0000023`) 라고 실패를 돌려주면서도 필요한 길이를 `*LengthNeeded` 에 꼭 적어 준다.** 에뮬레이터는 "성공 0 + 길이 안 씀" 으로 답해서 **틀렸다.**

> **LABEL (`0x10`) 은 `READ_CONTROL` 하나로 물을 수 있다.** 그래서 `0x20001` 손잡이로 `0x17` 을 물어도 통과한다. 따로 센 권한이 필요한 건 SACL (`0x8`) 뿐이다.

| 요청서 질문 | 한 줄 답 | 이 글 |
|---|---|---|
| 1 비트 → 권한 | O · G · D · LABEL · `0x20` ~ `0x100` = `READ_CONTROL`, SACL = `ACCESS_SYSTEM_SECURITY` 만, BACKUP = 둘 다. 모자라면 `0xC0000022` | 2.3 |
| 2 LABEL 만 물을 때 | 객체 SACL 에서 형 `0x11` ACE 만 골라 새 SACL 머리를 만든다 | 2.6 |
| 3 바이트 꼴 | 머리 20B → Owner → Group → DACL → SACL, 4바이트 맞춤 | 2.5 |
| 4 크기만 묻기 | `0xC0000023` + `*LengthNeeded` = 필요 길이 (늘 `0x14` 이상) | 2.4 |
| 5 형마다 담당 | Directory · Token 둘 다 `SeDefaultObjectMethod` — 같은 길 | 2.7 |
| 6 토큰 객체 SD | `NtDuplicateToken` 이 붙이는 건 형 `0x14`. `0x11` 을 붙이나는 `[공백]` | 2.8 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 길은 test.exe 의 줄기 **밖** 이다. 그래도 지도와 닿는 곳이 둘 있다 (★).

```
(부팅 때, test.exe 보다 훨씬 앞)
 ObInitSystem · SepTokenInitialization
   └─ 디렉터리 형 · 토큰 형 만들기 → "보안 담당 = 기본 함수" 로 정해짐   ★ 2.7

cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │    └─ PspInitializeProcessSecurity → SeSubProcessToken
 │         └─ 신분증 (토큰) 복제 (run49) → 그 신분증의 출입 명부 붙이기   ★ 2.8 (줄 이름만)
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → … → NtContinue (은퇴) → 진입점 → main
 │
 │    (test.exe 는 여기서 끝. rpcrt4 를 안 올리고, NtQuerySecurityObject 도 안 부른다)
 │
(rpcrt4 를 쓰는 다른 프로그램이라면 — PE_emulator 가 만난 상황)
 rpcrt4 가 RPC 런타임을 켤 때
   ├─ NtOpenDirectoryObject("\RPC Control", 0x20001)
   ├─ NtQuerySecurityObject(h, 0x17, NULL, 0, &len)          ← 이번 (run97): 커널이 어떻게 답하나
   └─ NtClose(h)
```

### 왜 이 질문이 지금 왔나

- PE_emulator 쪽에서 rpcrt4 가 RPC 런타임을 켤 때 (`UuidToStringA` · `WinHttpOpen` · `LookupPrivilegeValueW` 등 안) 위 셋을 부르고 실패했다 (에뮬레이터 쪽 관찰, 383 샘플).
  - 크기만 묻기 (`0x17`, 버퍼 NULL · 길이 0) 에 에뮬레이터가 **"성공 0 + 길이 칸 안 씀"** 으로 답했다.
  - 그러자 버퍼를 준 둘째 부름이 **한 번도 안 나왔고**, `UuidToStringA` 가 `0xe` 로 실패했다.
  - 토큰에도 같은 질문을 한다: `NtOpenProcessToken(자기, 0xa0008)` → `NtQuerySecurityObject(h, 0x10, NULL, 0, &len)`.
- 에뮬레이터는 **LABEL 비트를 커널이 어떻게 답하나** 를 몰랐다. 그래서 요청 34 가 왔다.

### 무엇이 궁금했나

"`NtQuerySecurityObject` 에 `0x17` · `0x10` 을 크기만 물으면, 진짜 커널은
**어떤 권한을 요구하고, 어떤 status · 길이 · 바이트로 답하나?**" — 특히 LABEL (`0x10`) 비트.

이걸 요청서가 여섯으로 나눴다 (위 "한 줄로 먼저" 표).

### 바퀴 전에 알던 것

- `[정적]` (run49) test.exe 의 신분증은 부모 것을 **복제**한다 — `PspInitializeProcessSecurity +0xf7 SeSubProcessToken → +0x43c SepDuplicateToken`. 그 신분증 객체에 **출입 명부를 무엇으로 붙이나** 는 안 봤다.
- `[정적]` (run71) kernelbase 가 `DBWinMutex` 를 만들 때 출입 명부를 직접 만들어 넘긴다 (run71 해설 2.2 "SID 4 · DACL 3 · SACL (낮은 무결성 쓰기 금지)"). 출입 명부를 **만드는** 쪽 이야기였다. 이번엔 **물어보는** 쪽이다.
- `[관찰]` (run44) test.exe 가 올리는 모듈은 6개 — rpcrt4 는 없다.
- 에뮬레이터 쪽 관찰 (요청서 배경, 우리 태그 아님): 위 "왜 이 질문이 지금 왔나".

---

## 2. 찾은 것

### 2.0 새 말 — SD, 무결성 라벨, 그리고 질문 체크박스

이번 결과를 읽으려면 말 몇 개가 먼저 필요하다. 하나씩 간다.

#### 새 말 — SD (보안 설명자, Security Descriptor)

- **SD** = 객체를 **누가 갖고, 누가 무엇을 해도 되는지** 적은 **출입 명부**.
- 사물함 (객체) 마다 한 장씩 붙어 있다. 관리사무소는 누가 손잡이를 달라고 하면 이 명부를 보고 정한다 `[추측]` (일반적으로 알려진 설명).
- 객체의 명부가 어디 붙어 있나: **객체 헤더** 의 SD 칸이다. 결과서는 이 칸을 "객체 주소 `-8` 의 값에서 아래 4비트를 뗀 포인터" 로 적었다 (동적 ① 의 `!sd` 명령). `SeDefaultObjectMethod +0x7d9` 는 객체 주소에서 `0x30` 을 빼서 헤더로 간다 `[정적]`.

명부 안에는 **칸이 넷** 있다. 이번 결과는 이 넷을 어떤 차례로 베끼나가 핵심이다.

| 칸 | 한 줄 뜻 (`[추측]` 일반적으로 알려진 설명) | 비유 |
|---|---|---|
| **소유자 (Owner)** | 이 객체의 주인 | 명부 맨 위 "주인: ○○" |
| **그룹 (Group)** | 주인이 속한 기본 그룹 | "소속: ○○팀" |
| **DACL** | 누구에게 무엇을 **허락 · 거절** 하나 적은 목록 | 명부의 **출입 허락 쪽** |
| **SACL** | 감사 기록 · 라벨 같은 **특수 줄** 목록 | 명부의 **특수 쪽** (flow.md 의 "봉인된 감사 장부") |

- 소유자 · 그룹은 **SID** (사람 · 그룹을 가리키는 번호 이름표) 하나씩이다 `[추측]` (일반적으로 알려진 설명). 크기는 `8 + 4 × SubAuthorityCount` 바이트 `[정적]` (`SeQuerySecurityDescriptorInfo` 크기 계산).
- DACL · SACL 은 **ACE** (목록의 한 줄) 들을 모은 것이다. ACE 마다 **형 번호** 가 있다 — 이번에 중요한 형은 `0x11` 과 `0x14` (2.6) `[정적]` (`RtlpFilterSacl` 이 형 번호로 고른다).

#### 새 말 — 무결성 라벨

- **무결성 라벨** = 객체 · 프로세스의 **신뢰 등급 표**. 낮은 등급은 높은 등급에 **못 쓴다** `[추측]` (일반적으로 알려진 설명).
- 비유: 사물함에 붙은 **"이 등급 이상만 넣기 가능" 스티커.** 가게 신분증에도 등급이 적혀 있다.
- 출입 명부의 어디에 있나: **SACL (특수 쪽) 안의 ACE 하나**, 형 번호 **`0x11` (SYSTEM_MANDATORY_LABEL)** `[정적]` (run 문서 "SACL 고르기" 표).
  - run71 해설 2.2 의 "SACL (낮은 무결성 쓰기 금지)" 이 이런 라벨 줄이었을 것이다 `[추측]` (run71 은 이름이 아니라 뜻으로 적었다).
- 비슷하게 생긴 **형 `0x14` (PROCESS_TRUST_LABEL)** 도 SACL 에 들어간다. 이름에 "LABEL" 이 있지만 **다른 형** 이다 (2.8 에서 중요).

#### 새 말 — 보안 질문 체크박스 (`SecurityInformation` 비트)

- `NtQuerySecurityObject` 에 "명부의 **어느 칸** 을 보여 줘" 를 비트로 체크해서 넘긴다. 이게 `SecurityInformation` 이다.
- 이번에 나오는 체크:

| 비트 | 이름 | 뜻 |
|---|---|---|
| `0x1` | OWNER | 소유자 칸 |
| `0x2` | GROUP | 그룹 칸 |
| `0x4` | DACL | 허락 쪽 |
| `0x8` | SACL | 특수 쪽 (라벨 빼고 나머지 — 2.6) |
| `0x10` | **LABEL** | 특수 쪽 중 **무결성 라벨 줄만** |
| `0x10000` | BACKUP | "다 보여 줘" 에 가까움 (2.5) |

- 에뮬레이터가 물은 `0x17` = `0x1 | 0x2 | 0x4 | 0x10` = **소유자 · 그룹 · DACL · 라벨**. 토큰엔 `0x10` = **라벨만**.

#### 새 말 — 손잡이 허락 목록 (핸들 권한)

- 손잡이 (핸들) 를 받을 때 **"이 손잡이로 할 수 있는 일"** 이 비트로 정해진다. 이번 글에선 **손잡이 허락 목록** 이라 부른다.
- flow.md 3장 새 절의 **"도장"** 이 이 목록의 한 줄이다.
- 이번에 나오는 줄:
  - **`READ_CONTROL` (`0x20000`)** = "출입 명부를 **읽어도** 됨"
  - **`ACCESS_SYSTEM_SECURITY` (`0x1000000`)** = "명부의 **특수 쪽 (SACL)** 까지 읽어도 됨"
- 에뮬레이터 쪽 손잡이:
  - 디렉터리 `0x20001` = `READ_CONTROL | DIRECTORY_QUERY` (요청서 배경)
  - 토큰 `0xa0008` = `READ_CONTROL | WRITE_OWNER | TOKEN_QUERY` (결과서)

---

### 2.1 한눈에 — 커널이 "명부 사본 주세요" 에 답하는 길

run 문서 "호출 관계" 에서 대표만 남겼다. 모두 `[정적]`.

```
nt!NtQuerySecurityObject (손잡이, 체크박스, 봉투, 봉투 길이, *LengthNeeded)
 │  (유저가 낸 부름이면) +0x3e ~ +0xa4  봉투 · 길이 칸 미리 만져 보기          ← 2.4
 │  +0xc7 ~ +0x124  체크박스 → 요구할 손잡이 허락 계산                         ← 2.3
 ├─ +0x145  ObReferenceObjectByHandle(손잡이, 요구 허락, …)   모자라면 0xC0000022
 │  +0x181  형 객체 +0x98 (보안 담당) 읽기 → 주소 비교                         ← 2.7
 ├─ +0x1d4  SeDefaultObjectMethod (EDX = 1 "질문")           디렉터리 · 토큰
 │     ├─ +0x7dd  ObpReferenceSecurityDescriptor   (헤더의 명부를 빌림)
 │     ├─ +0x7fd  SeQuerySecurityDescriptorInfo                                ← 2.5
 │     │     │  크기 모으기 → +0x1a9 *길이 = 필요 → +0x1af 작으면 0xC0000023  ← 2.4
 │     │     │  +0x1b5 머리 → Owner +0x1fa → Group +0x24a → DACL +0x2b9
 │     │     └─ +0x320  RtlpFilterSacl (특수 쪽에서 고르기)                    ← 2.6
 │     └─ +0x809 ~ +0x831 / +0x9f6  빌린 명부 돌려놓기
 ├─ +0x238 IopGetSetSecurityObject · +0x258 CmpSecurityMethod · +0x265 CFG 간접   (다른 형)
 │  +0x1e2  *LengthNeeded = 필요 길이   (성공이든 0xC0000023 이든)             ← 2.4
 └─ +0x1f9  ObfDereferenceObject
```

비유로 한 번:

1. 손님이 손잡이를 내밀며 "이 사물함 명부 사본 주세요. 소유자 · 그룹 · 허락 쪽 · 라벨만요." 한다.
2. 창구는 먼저 **손잡이 허락 목록에 "명부 읽기" 가 있나** 본다.
3. 사물함 **종류** 를 보고, 그 종류의 **보안 담당** 에게 넘긴다. 디렉터리 · 신분증은 **기본 담당** 이다.
4. 기본 담당이 명부를 빌려 와서, **필요한 봉투 크기** 부터 잰다.
5. 봉투가 작으면 "작아요" + 크기를 적어 주고 끝. 크면 사본을 만들어 봉투에 넣는다.

---

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `├─ +0x145  ObReferenceObjectByHandle(손잡이, 요구 허락, …)   모자라면 0xC0000022`

- **누가**: `nt!NtQuerySecurityObject` — 유저가 낸 "보안 정보 묻기" 시스템 콜의 커널 쪽 본체.
- **어디서**: 자기 코드 `+0x145` (ret `+0x14a`).
- **누구를**: `ObReferenceObjectByHandle` — 손잡이를 진짜 객체로 바꿔 주는 함수. 넘기는 것 = (손잡이, **요구 허락**, 형 = NULL, 이전 모드, …).
- **무엇을 위해**: 손잡이가 진짜인지, **허락 목록에 요구한 줄이 다 있는지** 보려고.
  - 비교는 안쪽 `ObpReferenceObjectByHandleWithTag +0x514 TEST EBP,EAX` (요구 & ~가진 허락) · `+0x516 JNZ → +0x693` 에서 한다 `[정적]`.
  - 모자라면 `0xC0000022` (`STATUS_ACCESS_DENIED`, `+0x693`). 잘못된 손잡이면 `0xC0000008` (결과서 `+0x575`) `[정적]`.
  - 실패면 `+0x14c JS → +0x200` 으로 바로 끝 — **`*LengthNeeded` 를 안 쓴다** `[정적]`. (2.4 에서 다시 나온다.)
  - 형 = NULL 이라 "이 손잡이가 디렉터리인가" 같은 **종류 검사는 없다** `[정적]`.

#### 줄 ②: `│  +0x1e2  *LengthNeeded = 필요 길이   (성공이든 0xC0000023 이든)`

- **누가**: 같은 `NtQuerySecurityObject`.
- **어디서**: `+0x1db MOV EAX,[RSP+0x98]` 로 지역 길이 칸을 읽고, **`+0x1e2 MOV [RDI],EAX`** 로 쓴다. `RDI` 는 `+0x4b` 에서 잡아 둔 **유저의 `LengthNeeded` 포인터** `[정적]`.
- **누구를**: 부르는 함수는 없다. 값 옮기기 한 줄.
- **무엇을 위해**: 안쪽 담당이 재 둔 "필요한 봉투 크기" 를 **손님 쪽 칸** 으로 옮겨 주려고.
  - 이 줄은 보안 담당 (`SeDefaultObjectMethod`) 이 **돌아온 뒤**, status 를 보지 않고 탄다 `[정적]`.
  - 그래서 "봉투가 작아요" (`0xC0000023`) 로 실패해도 길이는 적힌다. **이게 이번 바퀴의 핵심 줄이다.**

#### 줄 ③: `│     └─ +0x320  RtlpFilterSacl (특수 쪽에서 고르기)`

- **누가**: `SeQuerySecurityDescriptorInfo` — 명부 사본을 실제로 만드는 함수.
- **어디서**: `+0x320` (ret `+0x325`). 인자는 `+0x30b ~ +0x31d` 에서 만든다 `[정적]`.
- **누구를**: `RtlpFilterSacl(객체 SACL, 쓸 자리, &크기, 마스크 = 체크박스 & 0x1f8)`.
- **무엇을 위해**: 특수 쪽에서 **물은 종류의 줄만** 골라 사본에 붙이려고. `0x17` 이면 마스크가 `0x10` 이라 **라벨 줄만** 간다 (2.6).
- 같은 함수를 **크기만** 재려고 `+0x383` 에서도 한 번 부른다 (`RDX = 0`, `+0x37e`) `[정적]`.

---

### 2.3 ★ 체크박스 → 요구할 손잡이 허락 (질문 1)

#### 표 `[정적]` (`NtQuerySecurityObject +0xc7 ~ +0x124`)

체크된 비트마다 요구 허락을 정하고, 다 **OR** 해서 줄 ① 에 넘긴다.

| 체크 | 이름 | 필요한 손잡이 허락 | 줄 |
|---|---|---|---|
| `0x1` · `0x2` · `0x4` · **`0x10`** | OWNER · GROUP · DACL · **LABEL** | `READ_CONTROL` (`0x20000`) | `+0xd4 BTS EDX,0x11` · `+0xd8 TEST R8B,0x17` · `+0xdc CMOVZ` |
| `0x8` | SACL | `ACCESS_SYSTEM_SECURITY` (`0x1000000`) **만** | `+0x118` · `+0x11c` · `+0x124` |
| `0x20` · `0x40` · `0x80` · `0x100` | ATTRIBUTE · SCOPE · PROCESS_TRUST_LABEL · ACCESS_FILTER | `READ_CONTROL` | `+0xe1 ~ +0x103` · `+0x106 → +0x279` |
| `0x10000` | BACKUP | 둘 다 (`0x1020000`) | `+0xc7` → `+0x26f MOV ECX,0x1020000` |
| 그 밖 | | 요구 없음 | |

#### 읽는 법 — `+0xd8 TEST R8B,0x17` 한 줄

- `0x17` 은 `0x1 | 0x2 | 0x4 | 0x10` 이다.
- 이 한 줄이 "체크박스에 이 넷 중 **하나라도** 있나?" 를 본다.
- 있으면 `READ_CONTROL` (`0x20000`) 이 요구 목록에 들어간다 (`+0xd4 BTS EDX,0x11` · `+0xdc CMOVZ` 와 한 묶음).
- 그러니까 **LABEL 은 소유자 · 그룹 · DACL 과 같은 줄에 묶여 있다.** 따로 센 허락이 필요 없다.

#### 왜 `0x20001` 손잡이로 `0x17` 이 통과하나 — 단계별로 `[정적]`

1. `0x17` → 요구 허락 = `0x20000` (`READ_CONTROL` 하나).
2. 손잡이 허락 목록 = `0x20001` → 그 안에 `0x20000` 이 **있다.**
3. 비교 `+0x514` (요구 & ~가진 허락) = 0 → `+0x516 JNZ` 안 탐 → 통과.
4. 토큰 손잡이 `0xa0008` + `0x10` 도 같은 까닭으로 통과 (`0xa0008` 안에 `0x20000`).

- 단, 이 비교는 **이전 모드가 유저일 때만** 한다 (결과서 `+0x392` · `+0x3a1`). 커널이 스스로 부르면 건너뛴다.

#### "같은 규칙이 다른 함수에도 있다" — 그런데 이 길엔 없다

- `SeQuerySecurityAccessMask` (`+0x0 ~ +0x5a`) 에 **줄마다 같은 규칙** 이 있다 `[정적]`.
- 그런데 그 함수를 부르는 건 `IoCheckFunctionAccess` 뿐이다. `NtQuerySecurityObject` 는 부르지 않고 **같은 규칙의 복사본을 본문에 인라인** 으로 갖고 있다 `[정적]`.
- 비유: 창구 직원이 규정집 (`SeQuerySecurityAccessMask`) 을 찾아보는 게 아니라, **같은 내용을 외워서** 바로 판단한다.

---

### 2.4 ★ 크기만 묻기 — "봉투가 작아요" + 크기 적어 주기 (질문 4)

#### 새 말 대신 — "크기만 묻기" 가 뭔가

- 버퍼 NULL · 길이 0 으로 한 번 물어서 **필요한 길이만** 받고, 그 길이로 버퍼를 마련해 **다시 묻는** 방식이다 `[추측]` (일반적으로 알려진 쓰는 법).
- 에뮬레이터 쪽 관찰에서도 rpcrt4 가 이렇게 물었다. 다만 첫 대답이 틀려서 둘째 부름까지 못 갔다.

#### 네 경우 표 `[정적]`

| 경우 | status | `*LengthNeeded` | 봉투 |
|---|---|---|---|
| 크기만 (NULL · 0) | **`0xC0000023`** (`STATUS_BUFFER_TOO_SMALL`, `SeQuerySecurityDescriptorInfo +0x3a2`) | **씀** = 필요 길이 | 안 씀. 입구 봉투 검사도 건너뜀 |
| 작음 (0 < 길이 < 필요) | `0xC0000023` | 씀 | 안 씀 |
| 충분 | `0` (`+0x46e`) | 씀 = 만든 바이트 수 | 채움 |
| 손잡이 실패 | `0xC0000008` / `0xC0000022` | **안 씀** (`+0x14c JS → +0x200`) | 안 씀 |

#### 길이를 쓰는 순서 — 안에서 한 번, 밖에서 한 번 `[정적]`

1. 안쪽 `SeQuerySecurityDescriptorInfo` 가 필요 길이를 다 모은다 (2.5 의 3번).
2. **`+0x1a9 MOV [RAX],EDI`** — 지역 길이 칸에 **먼저 쓴다.** (`+0x1a1` 은 그 칸 주소를 읽는 줄이다 — run 문서 "다르게 적은 곳" ①.)
3. `+0x1ab CMP` · `+0x1af JA → +0x3a2` — 받은 길이보다 크면 `0xC0000023` 으로 나간다. 봉투는 **이 뒤부터만** 건드리니까 안 건드린 채로.
4. 바깥 `NtQuerySecurityObject +0x1e2` 가 그 값을 **유저 칸** 으로 옮긴다 (2.2 줄 ②).

→ 2번이 3번보다 **앞** 이라서, 실패해도 길이가 적힌다.

#### 길이 0 으로 "성공" 하는 길은 없다 `[정적]`

- 필요 길이는 **`0x14` 에서 출발** 한다 (`+0xee MOV EDI,0x14`) — 머리 20바이트.
- 비교가 `JA` (크면) 라서, 받은 길이 0 < `0x14` → 반드시 `0xC0000023`.
- 객체에 명부가 아예 없어도 (NULL) 필요 길이 `0x14` 를 쓴다 (`+0x435`).
- 그러니 **에뮬레이터의 "성공 0" 은 진짜 길에서 나올 수 없는 답** 이다 (기본 담당을 쓰는 형 기준).

#### 그래서 에뮬레이터는 왜 막혔나 — 단계별로

1. rpcrt4 가 크기만 물었다 (에뮬레이터 쪽 관찰).
2. 에뮬레이터가 "성공 0 + 길이 칸 안 씀" 으로 답했다 → **틀림** `[정적]` (위 표와 다름).
3. 부른 쪽은 버퍼 크기를 못 얻었다 → 둘째 부름이 없었다 (에뮬레이터 쪽 관찰).
4. 이게 `UuidToStringA → 0xe` 의 **가장 가까운 원인 후보** 다 `[추측: 결과서]`.
   - rpcrt4 안에서 무엇을 보고 `0xe` 로 가는지는 Q180c (rpcrt4, run99) 몫이다.

#### 입구에서 먼저 하는 일 — 손님 칸 미리 만져 보기 `[정적]`

이전 모드가 유저일 때만 한다 (`+0x24` `GS:[0x188]` · `+0x2d` `[+0x232]` → `+0x35` · `+0x38 JZ +0x212` 커널이면 건너뜀).

1. `+0x3e ~ +0x5c` — **`LengthNeeded` 포인터** 를 `min(포인터, 0x7fffffff0000)` 로 눌러 4바이트 **읽고-다시 쓰기**. 정렬 검사는 없다. NULL 이면 주소 0 을 건드려 접근 위반.
2. `+0x5e ~ +0x65` — 길이가 0 이면 `JZ → +0xb4`, **봉투 검사를 통째로 건너뜀.** 그래서 (NULL, 0) 이 통과한다.
3. 길이가 0 이 아니면 — `+0x6a TEST BL,3` · `+0x6d JNZ` 4바이트 맞춤 아니면 `ExRaiseDatatypeMisalignment`, `+0x73 ~ +0x7e` 범위 넘으면 `ExRaiseAccessViolation`, 아니면 페이지마다 읽고-다시 쓰기 (결과서 `+0x90 ~ +0xa4`).

- 이 셋이 첫째 `__try` 안이다 (착지점 `+0xb7` → `+0x200`). 착지했을 때 돌려주는 값이 **예외 코드** 인지는 `[추측]` (동적 ⑥).
- `+0x1e2` 쓰기도 둘째 `__try` 안이다 (착지점 `+0x1e6`, 결과서).

비유: 창구가 사본을 만들기 전에, 손님이 내민 **"크기 적을 쪽지"** 를 한 번 집어 봤다 내려놓는다. 봉투를 안 줬으면 (길이 0) 봉투는 안 본다.

---

### 2.5 사본 만드는 법 — 머리 20바이트, 그다음 넷 (질문 3)

모두 `SeQuerySecurityDescriptorInfo` 안 `[정적]`.

#### 새 말 — self-relative (한 덩어리 꼴)

- 출입 명부를 저장하는 꼴이 둘이다 (run 문서 "구성요소 찾기"):
  - **absolute** — 머리 칸에 각 구성요소의 **포인터** 를 둔다. 조각이 여기저기 흩어져도 된다.
  - **self-relative** — 머리 칸에 **"머리에서 몇 바이트 뒤"** 를 적고, 조각을 **한 덩어리로 이어 붙인다.**
- 비유: absolute = "소유자는 3층 서랍에" 같은 **위치 메모**. self-relative = 한 장짜리 **복사본** 에 "소유자는 이 종이 20바이트째부터".
- **출력은 늘 self-relative** 다. 객체 쪽이 absolute 여도 바뀐다 `[정적]`. 봉투에 담아 넘기려면 한 덩어리여야 하니까.

#### 0. 객체에 명부가 없으면 (NULL) `[정적]`

- `+0x3c MOV RAX,[R9]` · `+0x3f TEST` · `+0x42 JZ → +0x430`.
- 필요 길이 `0x14` (`+0x435`), 작으면 `0xC0000023` (`+0x43c`), 아니면 20바이트 `{01 00 00 80, 0 × 16}` 을 쓰고 성공.

#### 1 ~ 4. 사본을 만들기 전 `[정적]`

1. **BACKUP 넓히기** — `+0x10b BT R9D,0x10` · `+0x110 JC → +0x3ef OR R9D,0x1ff`. 원래 `0x80` · `0x100` 이 없었으면 다시 끈다 (`+0x460` · `+0x405`). 넓힌 값을 부른 쪽 체크박스 칸 (`[R8]`) 에 되쓴다 (`+0x3f6` · `+0x40a` · `+0x465`, run 문서 보탬 ⑤).
2. **구성요소 찾기** — DACL 은 객체 `Control` 에 `0x4` 가, SACL 은 `0x10` 이 있을 때만.
3. **크기 모으기** — `0x14` 에서 출발해 물은 칸만 더한다:
   - Owner · Group: `8 + 4n`
   - DACL: `(AclSize + 3) & ~3`
   - SACL: `RtlpFilterSacl` 크기만 (`+0x383`) — 담을 줄이 0 이어도 8
4. **길이 쓰고 비교** — 2.4 의 2 · 3번.

#### 5 ~ 7. 사본 만들기 `[정적]`

5. **머리** (`+0x1b5 ~ +0x1cc`): 20바이트를 0 으로 지우고 → `Revision = 1` (`+0x1c0`) → `Control = 0x8000` (`+0x1c3` · `+0x1c8` · `+0x1cc`).
   - `Revision` 은 객체 것을 안 옮기고 **상수 1** 이다.
6. **본문 시작** = `(봉투 + 0x17) & ~3` (`+0x1d0` · `+0x1d4`). 머리 바로 뒤를 4바이트에 맞춘 자리.
7. **차례대로 붙이기**:

| 차례 | 붙이는 줄 | 머리의 오프셋 칸 | `Control` 에 옮기는 비트 |
|---|---|---|---|
| ① Owner | `+0x1fa` 복사 | `+4` (`+0x203`) | `& 0x1` |
| ② Group | `+0x24a` 복사 | `+8` (`+0x253`) | `& 0x2` |
| ③ DACL | `+0x2b9` 복사 | `+0x10` (`+0x2c2`) | `& 0x140c` |
| ④ SACL | `+0x320 RtlpFilterSacl` | `+0xc` (`+0x329`) | `& 0x2830` |

#### 이 표 읽는 법 — 조심할 곳 셋

- **붙이는 차례와 머리 칸 차례가 다르다.**
  - 붙이는 건 Owner → Group → **DACL → SACL**.
  - 머리 칸은 `+4` Owner · `+8` Group · **`+0xc` SACL · `+0x10` DACL**.
  - 그래서 "SACL 칸이 DACL 칸보다 앞인데, 바이트는 DACL 이 먼저" 다.
- **오프셋 칸 값** = 그 조각이 시작하는 자리 − 봉투 시작. 안 물은 칸은 머리를 지운 그대로 **0**.
- **`Control` 비트는 물은 칸만 옮긴다.**
  - `0x140c` = DACL 쪽 네 비트, `0x2830` = SACL 쪽 네 비트 (비트 이름은 `[추측: 공개 헤더]`, 값은 코드).
  - DACL · SACL 은 **체크박스만 보고 비트를 먼저 옮긴다** (`+0x290 ~ +0x29f`, `+0x2e5 ~ +0x2ee` 가 복사 조건 검사 `+0x2a3` · `+0x2f3` 보다 앞, run 문서 보탬 ④). 그래서 객체에 "DACL 있음" 비트만 있고 조각이 없으면 → 오프셋 0 + 비트 켜짐.
- 조각 사이 올림으로 생긴 빈틈은 **안 지운다** (`memset` 없음). Owner · Group 은 원래 4의 배수라 틈이 안 생긴다 (결과서).

비유: 사본 용지 맨 위 20칸짜리 **목차** 를 비워 두고, 그 아래에 **주인 → 소속 → 허락 쪽 → 특수 쪽** 차례로 붙인다. 붙일 때마다 목차에 "몇째 줄부터" 를 적는다. 목차 순서는 주인 · 소속 · **특수 · 허락** 이다.

---

### 2.6 ★ LABEL 만 물을 때 — 특수 쪽에서 라벨 줄만 고르기 (질문 2)

#### 어느 줄을 고르나 `[정적]` (`RtlpFilterSacl`)

마스크 = 체크박스 `& 0x1f8`. ACE 형마다 마스크에 켜져 있어야 하는 비트:

| ACE 형 | 담으려면 마스크에 |
|---|---|
| **`0x11` SYSTEM_MANDATORY_LABEL** (무결성 라벨) | **`0x10`** (LABEL) |
| `0x12` RESOURCE_ATTRIBUTE | `0x20` |
| `0x13` SCOPED_POLICY_ID | `0x40` |
| `0x14` PROCESS_TRUST_LABEL | `0x80` |
| `0x15` ACCESS_FILTER | `0x100` |
| 그 밖 모두 (감사 · 알람 · 허용 · 거부 · 15 넘는 번호) | `0x8` (SACL) |

- **`0x17` → 마스크 `0x10` → 형 `0x11` 만 담긴다** (`+0x114 TEST BPL,0x10`).
- 반대로 `0x8` 만 체크하면 라벨은 **안 간다.** "SACL 다 줘" 가 라벨을 포함하지 않는다.
- 고른 줄은 **객체 SACL 안 차례 그대로, 통째로** (`AceSize` 바이트) 복사된다.

#### 특수 쪽의 머리를 새로 만든다 `[정적]`

골라 담으면 줄 수 · 크기가 바뀌니까 머리를 다시 쓴다.

1. `+0x97` · `+0x9a` — 객체 SACL 의 **앞 8바이트를 통째로 복사** (`AclRevision` · `Sbz1` · `Sbz2` 는 객체 것).
2. **`+0x9d MOV [R14+2],R9D` (`R9D = 8`)** — 그 위를 `AclSize = 8` · `AceCount = 0` 으로 덮는다.
3. 줄 하나 담을 때마다 `+0xd7 INC [R14+4]` (줄 수 +1) · `+0xe0 ADD [R14+2],AX` (크기 += 그 줄 크기).
4. 결과: `AclSize = 8 + 담은 줄 크기 합`, `AceCount = 담은 개수`.

비유: 특수 쪽 **표지** 를 원본에서 복사해 오되, "몇 줄 · 몇 바이트" 칸만 지우고 **0 줄 · 8바이트** 에서 다시 센다.

#### 라벨 줄이 없으면 — 세 경우 `[정적]`

| 경우 | 사본의 SACL 칸 | `SE_SACL_PRESENT` |
|---|---|---|
| (a) 객체에 SACL 이 **아예 없음** | 오프셋 **0** (복사 건너뜀, `+0x305 JZ` · 크기 쪽 `+0x373 JZ`) | 객체 것 그대로 (없으면 꺼짐) |
| (b) SACL 은 있는데 `0x11` 없음 | **8바이트 빈 SACL** 이 붙고 오프셋 ≠ 0 | 켜짐 |
| (c) "SACL 있음" 비트는 켜졌는데 오프셋 0 (NULL SACL) | 오프셋 0 | 켜짐 |

- 지금 에뮬레이터는 객체 명부에 **SACL 칸 자체가 없다** (결과서 "지금 우리"). 그러면 (a) 와 같은 답이 나온다.
- **진짜 `\RPC Control` · 토큰 객체에 SACL 이 없다면, 지금 구조로도 바이트가 맞는다.**
- 있나 없나는 진짜 명부를 떠 봐야 안다 → 동적 ① (4절).

---

### 2.7 형마다 보안 담당 — 디렉터리 · 토큰은 같은 길 (질문 5)

#### 새 말 — 형마다 보안 담당 (`SecurityProcedure`)

- 객체 **형식** (run47 해설) 마다 "보안 일은 이 함수가 맡는다" 를 등록해 둔다. 칸 이름이 `SecurityProcedure`, **형 객체 `+0x98`** 이다 `[정적]`.
- run47 해설의 **parse 프로시저** 와 같은 모양이다: 종류마다 담당 함수가 하나씩.
- 비유: 사물함 **종류별 보안 담당 직원.** 대부분은 **기본 담당** 이 하고, 파일 · 레지스트리 같은 종류는 **전담 직원** 이 따로 있다.

#### 창구에서 갈라지는 법 `[정적]`

1. 손잡이 → 객체 → 헤더 쿠키로 형 객체 찾기 (결과서 `+0x152 ~ +0x17d`, `ObHeaderCookie` · `ObTypeIndexTable`).
2. `+0x181 MOV R10,[RAX+0x98]` — 형 객체의 보안 담당 주소를 읽는다.
3. 주소 비교 셋:

| 보안 담당 | 부르는 자리 | 종류 |
|---|---|---|
| **`SeDefaultObjectMethod`** (기본) | **`+0x1d4`** (비교 `+0x194` · `+0x1c2` · `+0x1c5`) | 디렉터리 · 토큰 등 |
| `IopGetSetSecurityObject` | `+0x238` | 파일 · 장치 |
| `CmpSecurityMethod` | `+0x258` | 레지스트리 키 |
| 그 밖 | `+0x265` CFG 간접 (교환원) | 대상은 실행 때 (예 `WmipSecurityMethod`) |

- 넷 다 같은 인자 모양이고, `EDX = 1` (`+0x1bd`) = **"질문"** 이다.
- `SeDefaultObjectMethod` 안에서 `EDX` 로 일을 가른다: `2` 삭제 · `0` 쓰기 · **`1` 질문 → `+0x7d9`** · `3` 새로 달기 · 그 밖은 `KeBugCheckEx` (`+0xa00 ~`).

#### 디렉터리 · 토큰이 기본 담당인 이유 — 부팅 때 "0" 으로 넘겨서 `[정적]`

1. 형을 만들 때 넘기는 **초기화 구조체** 에 `+0x58` (보안 담당) 칸이 있다.
2. `ObCreateObjectTypeEx +0x4af CMP [RDI+0x58],R14` — 이 칸이 **0** 이면
3. `+0x4b5 LEA RAX,[SeDefaultObjectMethod]` → **`+0x4bc MOV [RBX+0x98],RAX`** — 형 객체 `+0x98` 에 기본 담당을 넣는다.
4. 두 형 모두 그 칸을 0 으로 둔다:

| 형 | 만드는 자리 | 초기화 구조체 `+0x58` |
|---|---|---|
| Directory (`ObpDirectoryObjectType`) | `ObInitSystem +0x2e4` | 0 — `+0x223 memset` 뒤 다른 칸만 씀 |
| Token (`SeTokenObjectType`) | `SepTokenInitialization +0x8d` (`SepInitializationPhase0 +0x18`) | 0 — `+0x37 memset` 뒤 다른 칸만 씀 |

- (`ObCreateObjectType` 는 `+0xc` 에서 `ObCreateObjectTypeEx` 를 부르는 22바이트 껍데기다.)
- → **에뮬레이터는 형마다 따로 만들 필요가 없다.** 한 구현이면 된다.
- 다만 이건 **"형을 만들 때 값"** 까지다. 만든 뒤 누가 `+0x98` 을 덮는지는 안 봤다 → 동적 ② · 정적 Q184 ③.

#### 이름이 비슷하지만 이 길 밖인 넷 `[정적]`

| 함수 | 왜 길 밖인가 |
|---|---|
| `ObQuerySecurityDescriptorInfo` | 부르는 쪽이 `WmipSecurityMethod` · `FUN_140b2a1cc`. **전담 직원이 쓰는 도우미.** 기본 담당은 `SeQuerySecurityDescriptorInfo` 를 직접 부른다 (`+0x7fd`) |
| `ObpGetObjectSecurity` | **접근 검사 쪽** (`ObpCheckTraverseAccess` · `ObpCheckObjectReference` · `ObCheckCreateObjectAccess` 등). 기본 담당이면 헤더 명부를 직접 읽는다 (`+0x59`), 아니면 체크박스 `0x1bf` (`+0xdc`) 로 형 담당을 부른다 |
| `SeQuerySecurityAccessMask` | 부르는 쪽 `IoCheckFunctionAccess` 뿐 (2.3) |
| `SeQueryMandatoryLabel` | 이번 지도에 없음 |

- 요청서는 이 넷도 "길에 있나부터" 보라고 했다. 답은 **"`NtQuerySecurityObject` 지도 (깊이 4, 273개) 에 없다"** 다.

---

### 2.8 신분증 (토큰) 객체의 출입 명부 — 이름 · 줄만 (질문 6)

#### 누가 명부를 정하나 `[정적]`

- 형의 **기본 명부가 아니라, 만드는 쪽이 넘긴 것 + 계산** 이다.
  - 만드는 쪽이 `ObjectAttributes` 의 SD 를 먼저 본다 (`NtDuplicateToken +0xb2`, `SepCreateTokenEx +0x118`).
  - 넣을 때 `ObInsertObjectEx +0x325 SeAssignSecurityEx2` 가 계산하고, `+0x38c SeDefaultObjectMethod` (**"새로 달기"**) 로 건넨다.
- `ObpCreateDefaultObjectTypeSD` 는 **형 객체 자신의** 명부용이다. 토큰 객체와 상관없다.

#### `NtDuplicateToken` 이 붙이는 건 `0x14` 다 — `0x11` 이 아니다 `[정적]`

명부를 안 받았을 때 (SD 없이 불렸을 때) 넣은 뒤:

1. `+0x21e SepAppendAceToTokenObjectAcl(토큰, 8, SeAliasAdminsSid)` — 줄 하나 덧붙임.
2. `+0x338 SepSetProcessTrustLabelAceForToken`
   - → `+0x140 RtlAddProcessTrustLabelAce` — **형 `0x14` 만** 받는다 (`+0x3c CMP [RSP+0x60],0x14`)
   - → `+0x196 ObSetSecurityObjectByPointer` 로 건다.

그래서 왜 `0x17` 질문엔 안 나오나 — 단계별로:

1. 붙는 줄은 형 `0x14` (PROCESS_TRUST_LABEL).
2. `0x17` 의 마스크는 `0x10`. `0x10` 이 고르는 건 형 `0x11` 뿐 (2.6 표).
3. `0x14` 를 고르려면 `0x80` 이 필요하다. → **`0x17` 대답엔 안 들어간다.**

- 이름에 둘 다 "LABEL" 이 들어가서 헷갈리기 쉽다. **무결성 라벨 = `0x11`**, 이번에 붙는 건 **`0x14`** 다.

#### test.exe 신분증은 다른 길로 만든다

- test.exe 신분증은 `NtDuplicateToken` 이 아니라 run49 의 `PspInitializeProcessSecurity +0xf7 SeSubProcessToken → +0x43c SepDuplicateToken` 이다 `[정적]`.
- `SeSubProcessToken` 안에도 비슷한 줄이 있다 `[정적: _calls.tsv]` (본문 안 읽음, run 문서 보탬 ⑦):
  - `+0x80e ObInsertObjectEx` → `+0x847 SepAppendAceToTokenObjectAcl` → `+0x85b SepFinalizeTokenAcls`
- 이 길이 명부에 **무엇을 붙이나** — 특히 `0x11` 라벨이 있나 — 는 `[공백]` 이다.
- 새 객체 명부에 `0x11` 을 기본으로 붙이나 (`SeAssignSecurityEx2` / `RtlpNewSecurityObject` 안) 도 `[공백]` → **Q180d** (다음 바퀴 run100 예정).

---

### 2.9 test.exe 길에 있나 — 없다. 그래서 그림은 그대로

#### 찾아본 곳 `[정적: 지도]` (run 문서 보탬 ⑧)

- ntdll `_map/LdrInitializeThunk_*` — **0 건**. 비슷한 이름 `NtQuerySecurityAttributesToken` 만 있다 (다른 syscall).
- ntdll · kernelbase · kernel32 `.asm` 전체에서 `QuerySecurityObject` — **0 건**.
- kernelbase 는 **부모 쪽** `CreateProcessInternalW` 지도의 AppContainer · LowBox 갈래에만 있다:
  - `+0x24e9 BasepCreateAppContainer` · `+0x266d BasepCreateLowBox` → `BasepCreateAppContainerObjectDirectories +0x3dd` · `+0x44a` (import).
  - test.exe 는 일반 프로세스라 이 갈래를 안 탈 것이다 `[추측]` (cmd 가 AppContainer 속성을 안 넘김 — 확인한 줄 없음).
- (`UnhandledExceptionFilter` 지도에도 깊이 5 에 보이지만, 처리 안 된 예외 길이라 초기화 밖.)

#### 커널 안쪽은?

- `SeQuerySecurityDescriptorInfo` 는 `NtCreateUserProcess` 지도 깊이 6 ~ 7 에 **보이긴 한다** (`_map/NtCreateUserProcess_edges.tsv`). 실제로 타는지는 모른다 `[공백]`.
- 이름 있는 객체를 열 때의 접근 검사 (`ObpCheckTraverseAccess` → `ObpGetObjectSecurity`) 는 test.exe 초기화에도 있을 법하다 `[추측]`. 하지만 기본 담당 형이면 헤더 명부를 **직접** 읽어서 이번 사슬 (`SeQuerySecurityDescriptorInfo`) 을 안 탄다 `[정적]` (`ObpGetObjectSecurity +0x59`).

→ 그래서 flow.md **그림은 안 고쳤다.** 줄은 3장 새 절과 2.1 한 줄에만.

---

### 2.10 ★ PE_emulator 쪽 — 맞나, 틀리나

run 문서 "지금 PE_emulator 맞나 · 틀리나" 표에서. "지금 우리" 줄은 결과서가 요청서 배경으로 적은 것이다 (`../PE_emulator` 코드는 안 읽었다).

| 갈래 | 진짜 `[정적]` | 지금 에뮬레이터 | 판정 |
|---|---|---|---|
| 체크박스 → 허락 | 2.3 표, 모자라면 `0xC0000022` | LABEL 비트를 모름 | 모자람 — 표로 채우면 맞음 |
| 크기만 · 작음 | `0xC0000023` + `*LengthNeeded` = 필요 길이 (≥ `0x14`) | 성공 0 + 길이 안 씀 | **틀림** |
| 충분 | 0 + 길이 + 봉투 채움 | (둘째 부름이 없어 못 감) | 규칙대로 만들면 맞음 |
| 바이트 꼴 | 2.5 | 차례 모름 | 확인할 것: Owner · Group · DACL 차례인가 |
| 라벨 고르기 | 2.6, 형 `0x11` 만 | SACL 칸 없음 | 진짜에 SACL 이 없으면 결과 같음 — VM 확인 |
| 형별 담당 | 디렉터리 · 토큰 = 기본 담당 | 한 구현 | 맞음 |

- 고칠 것이 가장 분명한 줄은 **"크기만 · 작음"** 이다.
- run 문서에 **"에뮬레이터용 규칙 한 장"** (7단계) 이 있다. 에뮬레이터가 그대로 옮길 수 있게 2.3 ~ 2.6 을 한 장으로 줄인 것이다 → [run 문서](run97-rpc-sd-query.md) "에뮬레이터용 규칙 한 장".

---

### 2.11 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 "결과서와 다르게 적은 곳 · 보탠 것" ① ~ ⑧. 결론을 바꾼 것은 없다.

- ① 길이 쓰는 줄은 `SeQuerySecurityDescriptorInfo` **`+0x1a9`** (결과서 질문 4 의 `+0x1a1` 은 주소 읽는 줄). → 이 글 2.4.
- ② `+0x1e2` 는 **`NtQuerySecurityObject`** 쪽이다 (`SeQuerySecurityDescriptorInfo +0x1e2` 는 다른 줄). → 이 글은 함수 이름을 늘 붙였다.
- ③ Owner · Group · DACL 복사 함수는 같은 주소 (rva `0x6c3640`) 의 별명 여럿 (`RtlCopyMemory` · `memcpy` 등). 하는 일은 복사.
- ④ DACL · SACL `Control` 비트는 체크박스만 보고 먼저 켠다. → 2.5, 2.6 (c).
- ⑤ BACKUP 넓힌 값을 부른 쪽 체크박스 칸에 되쓴다 (유저 메모리는 아님 `[추측: 칸 출처는 안 따라감]`). → 2.5.
- ⑥ Directory 초기화 구조체는 바로 앞 `ObpTypeObjectType` 때 쓴 것을 다시 쓴다. 두 번 다 `+0x58` 엔 안 쓴다.
- ⑦ test.exe 신분증 길 `SeSubProcessToken` 줄. → 2.8.
- ⑧ test.exe 길에 있나 (지도 검색). → 2.9.

---

### 2.12 디버거 메모 — 함정 둘

- **객체 헤더의 명부 칸은 그대로 쓰면 안 된다.** 객체 `-8` 의 값은 아래 4비트가 참조 수 꼴이라, `& ~0xf` 로 떼고 `!sd` 에 넣어야 한다 (결과서 동적 1).
  ```
  !sd (poi(<객체>-8) & 0xfffffffffffffff0)
  ```
- **이 함수들에 조건 없이 멈추면 VM 이 사실상 멈춘다.** `ObpReferenceObjectByHandleWithTag` · `SeQuerySecurityDescriptorInfo` 는 모든 프로세스 · 모든 손잡이에 걸린다.
- 디버거가 붙어서 달라지는 점 `[추측]`: 이번에 읽은 줄 안에 디버거 칸을 읽는 줄은 없다. 단, 디버거에서 `!sd` 로 읽는 건 **유저 길 (권한 비교 · 입구 검사) 을 안 탄다** — 값만 같다.
- 두 함정은 windbg-kd-runbook pitfalls 에 더하자는 **제안** 으로 올라갔다 (plan.md 결정 기록 — 하네스라 오케스트레이터가 사용자에게 따로 묻는다).

---

### 2.13 정리 — 비유로 한 번 더

- 손님 (rpcrt4) 이 관리사무소 창구에 **사물함 방 손잡이** (`0x20001`) 를 내밀고 말한다: "이 방 **출입 명부** 사본이요. 주인 · 소속 · 허락 쪽 · 라벨만요. 봉투는 없어요, **크기만** 알려 주세요."
- 창구 (`NtQuerySecurityObject`) 는 먼저 손님의 쪽지를 집어 봤다 내려놓고 (입구 검사), **손잡이 허락 목록에 "명부 읽기" 가 있나** 본다. 라벨은 "명부 읽기" 하나로 된다. 있으니 통과.
- 방의 **종류** 를 보니 디렉터리 — **기본 보안 담당** (`SeDefaultObjectMethod`) 몫이다. 부팅 때 디렉터리 종류를 만들면서 "담당 칸 비움 = 기본 담당" 으로 정해 뒀다.
- 기본 담당은 명부를 빌려 와서 (`SeQuerySecurityDescriptorInfo`) 필요한 봉투 크기부터 잰다. 목차 20칸 + 주인 + 소속 + 허락 쪽 + (있으면) 라벨 줄.
- **크기를 먼저 쪽지에 적고**, 봉투가 없으니 "**봉투가 작아요**" (`0xC0000023`) 라고 돌려준다. 창구는 그 크기를 손님 쪽지에 옮겨 적는다 (`+0x1e2`).
- 손님은 그 크기로 봉투를 마련해 **다시 온다.** 그러면 담당은 목차 → 주인 → 소속 → 허락 쪽 → 특수 쪽 (라벨 줄만 골라서) 차례로 사본을 만든다.
- 에뮬레이터는 둘째 줄에서 "**성공, 근데 크기는 안 적음**" 이라고 답했다. 손님은 봉투를 못 만들어서 돌아갔다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

**그림 (1장) 은 안 바뀌었다.** test.exe 초기화 지도에 이 syscall 이 없어서다 (2.9). 태그가 올라간 줄도 없다. 새 줄만 생겼다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (ntkrnlmp pdb 줄) · 근거 로그 | run96 까지 | **run97 몫** 더함. 근거 로그 run97 줄: 정적 바퀴, 사진 대조 없음, 에뮬레이터 숫자는 우리 `[관찰]` 아님 |
| 흐름도 앞 글 | run96 문단까지 | **run97 문단** — "그림을 안 고쳤다" 와 그 이유 |
| 2.1 제목 | … run90 | … run90, **run97** |
| 2.1 run49 토큰 줄 아래 | 신분증 복제까지 (`SeSubProcessToken → SepDuplicateToken`) | **(run97 `[정적: _calls.tsv]`) 한 줄** — 신분증 객체 명부를 붙이는 줄 셋 (`+0x80e` · `+0x847` · `+0x85b`), 토큰 형 담당 = 기본 담당, 무엇이 붙나는 `[공백]` |
| 3장 | (없음) | **새 절 "보안 정보 묻기"** — 금고 · 출입 규칙표 비유, 호출 트리, `[정적]` 여섯 줄, `[추측]` 한 줄 (접근 검사 쪽) |
| 4장 | — | run97 새 줄 셋: 동적 (→ Q181) · 정적 (→ Q180d · Q184) · PE_emulator 와 다르게 적은 곳 |
| 5장 | run96 까지 | run 표 · 요청서 번호 **run97 줄** |

### 짐 지도 `baggage.md` — 안 고침

- 객체 헤더의 명부 (SD) 는 **지금 짐 칸이 아니다.**
- 이번 결과서는 **test.exe 객체의 명부 값** 을 말하지 않는다 (규칙만 말한다).
- 그래서 후보로만 남겼다: "토큰 · 프로세스 객체 헤더 SD" → Q184 ② (넣을지는 사용자 결정).

### 큰 그림에서 어떻게 이어지나

- **PE_emulator 쪽** — 이번 결과로 에뮬레이터는 `NtQuerySecurityObject` 의 대답을 **규칙대로** 만들 수 있다.
  - 단, 바이트를 만들려면 **진짜 객체의 명부** 가 있어야 한다. 그건 smss 가 `\RPC Control` 을 만들 때 넘기는 명부 (Q180b, run98) 와, 그 명부가 객체에 붙을 때의 계산 (Q180d, run100) 에서 온다. **다음 바퀴에서 이어진다.**
  - 그 대답을 받은 rpcrt4 가 무엇을 보고 `0xe` 로 가나는 Q180c (run99) 에서 이어진다.
- **우리 지도 쪽** — test.exe 줄기엔 직접 더할 게 없다. 닿는 곳은 둘:
  - 부팅 때 형 만들기 (디렉터리 · 토큰 형의 기본 담당).
  - 2.1 의 신분증 복제 뒤 명부 붙이기 (`SeSubProcessToken`). 여기는 짐 지도 후보가 될 수 있다 (Q184 ②).

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q181** (동적, run97 몫 ① ~ ⑧)

이번 바퀴엔 VM 을 안 썼다. Q181 은 Q104 VM 단계에 묶을 후보다.

**(가) test.exe 실행으로도 볼 수 있을 것** `[추측]` (run 문서: `\RPC Control` 은 전역 객체, 신분증은 `EPROCESS.Token`)

| 번호 | 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|---|
| ① | 진짜 `\RPC Control` · test.exe 신분증 객체의 **명부 바이트** — SACL 이 있나, `0x11` · `0x14` 줄이 있나, 소유자 · 그룹 · DACL 크기. 2.6 의 (a) 인가 (b) 인가 | 정적은 규칙만 본다. 값은 실행 때 |
| ② | 형 객체 `+0x98` 의 **실행 때 값** 이 정말 `SeDefaultObjectMethod` 인가 | 정적은 "형 만들 때" 까지만 봤다 (2.7) |
| ⑧ | test.exe 신분증 명부를 `SeSubProcessToken +0x813` · `+0x84c` · `+0x860` 뒤마다 떠서, 어느 단계가 무엇을 더하나 | 본문을 안 읽었다 (2.8). 부모 cmd 스레드에서 도니까 run44 방식 조건 |

**(나) rpcrt4 를 쓰는 다른 프로세스가 필요한 것**

| 번호 | 무엇을 모르나 |
|---|---|
| ③ | 진짜 대답 대조 — `NtQuerySecurityObject+0x1d9` (status) · `+0x1e2` (필요 길이) 가 2.4 · 2.5 공식과 같나 |
| ④ | 충분한 봉투일 때 실제 바이트 — 차례 · `Control` 이 2.5 표와 맞나 |
| ⑤ | 라벨 고르기 — `RtlpFilterSacl` 입구 마스크가 `0x10` 인가, 고른 크기 |
| ⑦ | 권한 비교 — `0x20001` 에 `0x17` 이 실제로 통과하나 (`ObpReferenceObjectByHandleWithTag+0x516`) |

- **⑥ 입구 예외 때 status** — 틀린 `LengthNeeded` 포인터로 부르면 `0xC0000005` 인가 (2.4 의 `[추측]`). 일부러 그렇게 부르는 프로그램이 필요하다. run 문서는 ⑥ 을 (가) · (나) 어느 쪽에 넣을지 따로 적지 않았고, plan.md Q181 은 ③ ~ ⑦ 묶음에 넣었다.
- ③ ~ ⑦ 은 모든 프로세스에 걸리는 함수라 **조건 필수** (2.12).

### 정적으로 남은 것

run 문서 "아직 확인 필요 — 정적" ① ~ ⑤ 가 대기열에서 이렇게 갔다.

| run 문서 | 대기열 | 무엇 | 왜 이번에 못 봤나 |
|---|---|---|---|
| ① | **Q180d** (다음 바퀴 run100 예정 — 아직 run 문서 없음) | `SeAssignSecurityEx2` / `RtlpNewSecurityObject` — 새 객체 명부에 `0x11` 라벨 줄을 기본으로 붙이나 | 이번 범위 밖 (2.8 `[공백]`) |
| ② | **Q184 ①** | `NtOpenDirectoryObject("\RPC Control", 0x20001)` 의 접근 검사 — 에뮬레이터가 손잡이에 `0x20001` 을 줄 조건 | 묻기 길이 아니라 **열기** 길 |
| ③ | **Q184 ②** | `SeSubProcessToken +0x80e ~ +0x85b` 본문 — test.exe 신분증 명부에 무엇을 붙이나 (짐 지도 후보) | `_calls.tsv` 줄만 봤다 |
| ④ | **Q184 ③** (작음) | 형을 만든 뒤 `+0x98` 을 덮는 줄이 있나 | 안 훑었다. 동적 ② 가 더 빠르다 |
| ⑤ | **Q184 ④** (낮음) | `NtSetSecurityObject` 쓰기 길 | 이번 범위 밖 |

### 같은 요청의 다음 조각 — 다음 바퀴에서 이어짐

- **Q180b** (smss, run98) — `\RPC Control` 을 만드는 쪽.
- **Q180c** (rpcrt4, run99) — 대답을 받는 쪽.
- **Q180d** (run100) — 넘긴 명부가 새 객체에 붙을 때.

---

## 5. 더 읽기

- run 문서: [`run97-rpc-sd-query.md`](run97-rpc-sd-query.md)
  - "PE_emulator 쪽에서 쓸 것" — 결론 · 맞나 틀리나 표 · **에뮬레이터용 규칙 한 장**
  - "비트 → 권한" · "크기만 묻기" · "바이트 만들기" · "SACL 고르기" · "형 → `SecurityProcedure`" · "토큰 객체 SD" · "test.exe 길에 있나"
  - "아직 확인 필요" — 동적 ① ~ ⑧, 정적 ① ~ ⑤
- 결과서: [`_workspace/sast/93_rpc-sd-query_result.md`](../../_workspace/sast/93_rpc-sd-query_result.md) — 질문 1 ~ 6, "에뮬레이터용 규칙 요약", "동적으로 확인할 것" 1 ~ 7
- 요청서: [`_workspace/sast/93_rpc-sd-query.md`](../../_workspace/sast/93_rpc-sd-query.md) — 배경 (에뮬레이터 쪽 관찰)
- flow.md: [`flow.md`](flow.md)
  - 3장 "보안 정보 묻기 — `nt!NtQuerySecurityObject` → … (run97)"
  - 2.1 run49 토큰 줄 아래 (run97) 한 줄
  - 4장 run97 줄 셋, 5장 run97 줄
- 앞 해설:
  - [run49 해설](run49-kprocess-token-htable-해설.md) — 신분증 복제 (`SeSubProcessToken → SepDuplicateToken`)
  - [run71 해설 2.2](run71-named-obj-dir-해설.md) — 출입 명부를 **만드는** 쪽 (`DBWinMutex`)
  - [run47 해설](run47-section-chain-해설.md) 2.5 · 2.10 — 객체 형식 · 형마다 담당 함수 (parse 프로시저)
  - [run29 해설 2.6](../02_sprint/run29-continue-start-해설.md) — 이전 모드

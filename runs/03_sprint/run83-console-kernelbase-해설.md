# run83 console-kernelbase — 해설

> 이 글은 [run83 문서](run83-console-kernelbase.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.
>
> **고쳐 읽기 (run84)**: "한 줄로 먼저" 의 "거기서 하나라도 실패하면 kernelbase 시작 함수가 **FALSE**" 와 2.7 끝 "하나라도 실패를 돌려주면 kernelbase 시작 함수가 FALSE 다" 는 너무 넓다 — 새 콘솔 길에선 **`ConsoleAllocate` 가 실패를 돌려줄 때만** FALSE 다 (`ConsoleInitialize +0x14d JNS` 안 탐 → AL 0 → `_KernelBaseBaseDllInitialize +0x528 JZ`). 마무리 (commit) 쪽 a2 · a3 · a4 (새 콘솔 길에선 b9 ~ b11) 실패로는 시작이 안 깨진다 — a4 (`GetConsoleLangId`) 실패는 오류 코드만, a2 · a3 실패는 FALSE 근거 없음. 붙기 길에선 a1 실패가 FALSE 다. → [run84 문서](run84-console-crt.md) "정정" 절 · [run84 해설](run84-console-crt-해설.md) 2.12 "시작이 깨지는 곳은 어디인가" · 2.14.

- 대기열: **Q149a** — PE_emulator 요청 27 (콘솔 입출력) 의 **kernelbase 쪽** (`_workspace/plan.md`, 이번에 **끝**)
  - Q149 (요청 27 전체) 는 둘로 나눴다.
    - kernelbase 쪽 = **Q149a** (이번).
    - CRT (ucrtbase) · condrv.sys 쪽과 "(가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표" = **Q149b** (다음, run84).
  - 다음 항목: **Q149b** → Q150a → Q150b → Q151a → Q151b → Q152 → [멈춤] (Q104 VM 단계 앞).
  - 이번에 남은 것이 간 곳 (4절):
    - 실행해 봐야 아는 것 → **Q104** (VM 단계)
    - 작은 정적 → 새 **Q158**
    - CRT · condrv → **Q149b**
- 근거:
  - run 문서 [`run83-console-kernelbase.md`](run83-console-kernelbase.md) (가장 중요)
  - 요청서 [`_workspace/sast/80_console-kernelbase.md`](../../_workspace/sast/80_console-kernelbase.md) · 결과서 [`80_console-kernelbase_result.md`](../../_workspace/sast/80_console-kernelbase_result.md)
    - 판정 **분석성공**. 질문 1 ~ 5 모두 `.asm` 줄로 답했고, run68 정정 제안이 하나 있었다. 스크립트 문제 없음.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 그림 2 `bc1` 라벨 숫자 하나 · 2.5 · 2.8 · **3장 새 절 "콘솔 입출력"** · 4장 · 5장 (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음, 서른아홉째판)
  - 이번 바퀴에 같이 한 일 둘:
    - **run68 정정** — 부모 콘솔에 붙는 길의 syscall 이 3번이 아니라 **4번** (2.6).
    - **run82 뒷정리** — 사실이 틀린 건 아니고 표기 셋 (3절).
- 이번 바퀴는 **정적만** 이다.
  - 새 VM 실행은 없다.
  - `[관찰]` 로 적힌 값은 flow-writer 가 run44 덤프 읽기 로그에서 콘솔 갈래를 가르는 칸만 **다시 읽은 것** 이다: 매개변수 블록의 `ConsoleHandle` · `ConsoleFlags` · 표준 핸들 셋 · `WindowFlags`, TEB `CurrentLocale`, KUSD `SharedDataFlags`.

### 이번 바퀴에서 조심할 곳 다섯

1. **"핸들 0" 이 두 가지다.**
   - **`ConsoleHandle`** (매개변수 블록 `+0x10`) = 콘솔 쪽과 이어지는 **연결 손잡이**.
   - **표준 핸들 셋** (`StandardInput` `+0x20` · `StandardOutput` `+0x28` · `StandardError` `+0x30`) = 앱이 읽고 쓸 때 쓰는 손잡이.
   - PE_emulator 는 지금 **둘 다 0** 이다. 그런데 0 일 때 일어나는 일은 둘이 전혀 다르다 (2.4 · 2.10).
2. **`ConsoleHandle` 의 −1 · −2 · −3 은 손잡이가 아니라 "신호" 다.**
   - 부모가 "이렇게 해 줘" 하고 적어 두는 약속 값이다.
   - 그 셋이 각각 `DETACHED_PROCESS` · `CREATE_NEW_CONSOLE` · `CREATE_NO_WINDOW` 인지는 `[추측]` 이다. 값을 **쓰는** 부모 쪽 (`CreateProcessInternalW`) 을 안 읽었다 (→ Q158 ①).
3. **점프가 도착하는 줄과 call 줄이 다르다.**
   - `ConsoleInitialize +0x177 JZ` 가 뛰어가는 곳은 `+0x142` (`LEA RCX,[RBP-0x40]`, 인자 준비) 다.
   - 실제로 `ConsoleAllocate` 를 부르는 줄은 그 바로 뒤 **call `+0x146` / ret `+0x14b`** 다 (flow-writer 보탬 ①).
   - 트리의 `+0x14b` 는 ret 자리 (콜스택에 보일 자리) 다.
4. **이름은 거의 다 `[추측: 이름]` 이다.**
   - PDB 에 콘솔 구조체 · IOCTL · API 번호 열거형이 하나도 없다 (`pdb_struct.py --list` 0개).
   - 그래서 "설명서 (`CD_USER_DEFINED_IO`)", "GetLangId" 같은 이름은 짐작이다. 대신 **칸 위치 · 크기 · 번호 값은 `[정적]`** 이다.
5. **flow.md 3장 새 절의 비유는 이 해설 시리즈와 그림이 조금 다르다.**
   - flow.md: 콘솔 창 = "건물 1층 **안내 데스크**" (`ConDrv` + conhost).
   - 이 해설: conhost = **옆집** (run51), `ConDrv` = 관리사무소 안 **콘솔 상담실** (아래).
   - 그림만 다르고 사실은 같다. 이 글은 해설 쪽 그림을 쓴다.

이미 푼 말은 **링크만** 건다.

- 가게 (프로세스) · 관리사무소 (커널) · 신청 창구 (syscall) · 관리실 (ntdll) · 건물 (KERNELBASE) · 전원 켜기 (시작 함수) · 수첩 (DLL 전역) → [run68 해설 "비유"](run68-dll-init-rest-해설.md#비유--그대로-이어-쓴다)
- 매개변수 블록 = 부모가 써 준 **개업 서류** → [run32 해설 2.3](../02_sprint/run32-params-initblock-해설.md#23-새-말--프로세스-매개변수-블록)
- 핸들 = **손잡이** → [run14 해설](../02_sprint/run14-create-process-internal-해설.md)
- TEB = 직원 **개인 안내판 (이름표)** → [run11 해설 2.2](../02_sprint/run11-allocate-thread-해설.md)
- 콘솔 호스트 (conhost) = 쇼윈도와 출입문을 맡아 주는 **옆집**, `OwnerProcessId` 에 콘솔 담당 번호 `|1` → [run51 해설 2.9](run51-eprocess-later-해설.md)
- **콘솔 연결**, 핸들 4 = 부모가 넘겨 준 **소개장**, 새 핸들 `0x60` = **내 전용 출입증** → [run68 해설 2.8](run68-dll-init-rest-해설.md)
- 크리티컬 섹션 = **열쇠** (`ConsoleStateLock` 도 그중 하나) → [run68 해설 2.7](run68-dll-init-rest-해설.md)
- NLS = **글자 사전**, `MakeLocHashNode` 아래가 아직 모름 → [run68 해설 2.6](run68-dll-init-rest-해설.md)
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = **번호 붙은 주문서**, IRP = **배달 봉투** → [run66 해설 2.7](run66-apphelp-cache-해설.md)
- KUSD = **공용 게시판** → [run70 해설 "비유"](run70-cpu-features-해설.md#비유--그대로-이어-쓴다)
- `OutputDebugStringA` · test.exe 의 `Log` → [run71 해설 2.1](run71-named-obj-dir-해설.md)
- 사진 P1 · P1a · P1b · P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, ntdll = 가게 안 **관리실**.
- KERNELBASE = 가게에 들인 **건물**, 그 시작 함수 = 건물 **전원 켜기**.
- 매개변수 블록 = **개업 서류**. 그 안 `ConsoleHandle` 칸 = 콘솔 쪽 **연결 손잡이** 칸, `Standard*` 칸 셋 = **입 · 귀 · 비상 확성기** 라고 부르자 (출력 · 입력 · 에러). 이 이름은 이 글에서만 쓰는 그림이다.
- conhost = 쇼윈도를 맡아 주는 **옆집**.
- 이번에 하나 더 붙는다:
  - **`ConDrv` = 관리사무소 건물 안 "콘솔 상담실"** (run66 해설의 "전문 상담실" 과 같은 꼴). 가게와 옆집 사이 말은 모두 이 상담실을 거친다.
  - 신청 창구 `NtCreateFile` 로 상담실에 "**이 이름의 연결 줄 하나 놔 주세요**" 하고, 창구 `NtDeviceIoControlFile` 로 **번호 붙은 주문서** (IOCTL) 를 넣는다.

### 한 줄로 먼저

- `ConsoleInitialize` 는 **`ConsoleHandle` 칸 값 하나로** 길을 가른다 `[정적]`.
  - 실제 손잡이 → 부모 콘솔에 **붙기**. syscall **4** (run68 은 3 이라고 했다 — 정정).
  - 콘솔 앱인데 0 · −2 · −3 → **새 콘솔 만들기** (`ConsoleAllocate`). syscall **11**. conhost 는 kernelbase 가 직접 안 띄우고 상담실에 IOCTL `0x500037` 로 **맡긴다**.
  - −1 → 연결 없이 마무리만 (commit). syscall **1**.
  - 콘솔 앱이 아니면 (또는 매니페스트가 "detached") → 아무것도 안 만든다.
- 그래서 PE_emulator 의 짐작 **"`ConsoleHandle = 0` 이면 콘솔 없음 → 조용히 성공" 은 틀렸다** `[정적]`. 콘솔 앱이면 0 은 **새 콘솔 길** 이고, 거기서 하나라도 실패하면 kernelbase 시작 함수가 **FALSE** 다.
- 콘솔 API 는 **길이 셋** 이다 `[정적]`.
  - `WriteFile` · `ReadFile` → 콘솔인지 안 묻고 `NtWriteFile` · `NtReadFile` 바로.
  - `GetConsoleMode` 류 → `NtDeviceIoControlFile(개업 서류의 ConsoleHandle, 0x500016)`.
  - `WriteConsoleW` · `ReadConsoleW` → `NtDeviceIoControlFile(앱이 가진 핸들, 0x500016)`.
  - 주문서 `0x500016` 안엔 내용이 아니라 **"내용은 여기 있어요" 포인터** 가 들어 있다.
- test.exe 의 `Log` 는 CRT 없이 **`WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)`** 을 직접 부른다 `[소스]` (`sample/log.h:38`). 그러니 test.exe 의 출력 길에는 **콘솔 IOCTL 이 없고 `NtWriteFile` 하나** 다 `[정적 + 소스]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → … → ntdll 로더
 │
 ├─ kernel32 들이기 → KERNELBASE 전원 켜기 (flow.md 2.5, run68)
 │     _KernelBaseBaseDllInitialize +0x526 ConsoleInitialize        ← 이번 ①: 갈래 전부 (2.3 ~ 2.8)
 │       └─ 붙기 길 ConsoleCommitState → … GetConsoleLangId           ← 이번 ②: run68 이 빠뜨린 넷째 syscall (2.6)
 │
 ├─ dll2 TLS 콜백 · DllMain · … 의 Log (flow.md 2.8 · 2.10)
 │     WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)                 ← 이번 ③: kernelbase 안에서 무엇이 되나 (2.10)
 │
 → NtContinue 은퇴 → 진입점 → (CRT) → main
       콘솔 API (WriteConsoleW · GetConsoleMode · GetFileType …)   ← 이번 ④: 흐름도 범위 밖, flow.md 3장 새 절 (2.9 · 2.11)
```

- ① ② 는 **은퇴 앞** 이다. 지도에 이미 있던 자리 (run68) 인데, run68 은 test.exe 가 타는 "붙기" 갈래만 풀었다. 이번엔 **모든 갈래** 를 풀었다.
- ③ 은 은퇴 앞에도 있다. test.exe 의 `Log` 는 dll2 TLS 콜백부터 불린다.
- ④ 는 진입점 뒤 이야기라 그림엔 안 넣고 flow.md **3장 새 절** 에 적었다.

### 왜 이 질문이 지금 왔나

- PE_emulator 는 은퇴 뒤 진짜 kernelbase · CRT 를 Unicorn 안에서 돌린다. syscall 만 nt 엔진이 대답한다 (요청서 "배경").
- 그런데 PE_emulator 엔 **부모가 없다**. 그래서 `ConsoleHandle` 도 표준 핸들도 모두 0 이다.
  - PE_emulator 쪽 실행 기록 (요청서): test.exe 가 은퇴 뒤 `NtWriteFile(핸들 0)` 을 **26 번** 부르고 모두 `0xc0000008` 을 받았다.
- "가짜 콘솔" 을 세우려면 알아야 한다:
  - kernelbase 가 콘솔 상담실에 **무엇을 보내고, 어떤 답을 기대하나**.
  - 개업 서류 칸에 **무엇이 있어야** 하나.
- 사용자 결정 (2026-11-12): 요청 26 ~ 30 을 맨 앞으로, 27 은 2순위.

### 무엇이 궁금했나

한 문장으로: **콘솔을 얻는 길은 몇 갈래고 갈래마다 무슨 syscall 을 부르나. 콘솔에 읽고 쓰는 API 는 상담실에 무엇을 보내나. 손잡이가 0 이거나 파일이면 어떻게 되나.**

이번엔 원문 요청 27 번호와 sast/80 번호가 **같다** (run 문서 표의 "1 · 1" 꼴).

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 1 | 콘솔을 얻는 길 셋 (물려받음 · 새 콘솔 · 없음) — syscall · IOCTL · 바뀌는 칸 | 2.1 ~ 2.8 |
| 2 | 자주 쓰는 콘솔 API — syscall · IOCTL · 입력 · 기대 답 | 2.9 |
| 3 | `GetFileType` | 2.11 |
| 4 | 표준 핸들이 0 일 때 | 2.10 |
| 5 | 표준 출력이 파일 · 파이프일 때 | 2.10 |

### 바퀴 전에 알던 것

- `[정적]` run68 (sast/63): test.exe 는 `ConsoleHandle = 4` · `ConsoleFlags = 0` → "부모 콘솔에 붙기". `NtCreateFile(<핸들 4> 기준 "\Connect")` → `ConsoleCommitState` 가 `ConsoleHandle` 을 4 → `0x60` 으로. syscall **3번** 이라고 적었다.
- `[정적]` run51: `ConsoleCommitState` 안 `NtDeviceIoControlFile(0x500023)` → `NtSetInformationProcess(-1, 0x31)` → `EPROCESS.OwnerProcessId` 둘째 쓰기.
- `[관찰]` run44 사진: `ConsoleHandle` 4 → `0x60` (P1a → P1b).
- `[소스]` `sample/log.h`: `Log` 는 `OutputDebugStringA` 다음에 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), …)`.
- PE_emulator 쪽 기록: `ConsoleHandle` · 표준 핸들 0, `NtWriteFile(0)` 26 번 `0xc0000008`.
- 몰랐던 것: 0 · −1 · −2 · −3 일 때 정확히 무엇을 하나, 콘솔 API 가 무엇을 보내나.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 문 하나, 갈림길 둘

`ConsoleInitialize` (kernelbase rva `0xbeb60`) 를 처음부터 끝까지 읽었다. 갈림길은 **둘** 이다 `[정적]`.

```
KERNELBASE!_KernelBaseBaseDllInitialize
 └─ +0x526  ConsoleInitialize                      (+0x526 TEST AL · +0x528 JZ → +0xf6: AL 0 이면 시작 함수도 FALSE)
      ├─ +0x6a   RtlImageNtHeader                   +0x7b Subsystem == 3 ? (콘솔 앱인가)
      ├─ +0xbd   ConsoleShouldAllocateConsole        ── 갈림길 ① (+0xbf JNZ)
      │     FALSE → +0xc5  (c) 아무것도 안 만듦
      │     TRUE  → +0x170 ConsoleHandle 값으로 ── 갈림길 ②
      ├─ +0x14b  ConsoleAllocate                    0 · −2 · −3   → (b) 새 콘솔
      ├─ +0x19f  ConsoleCreateConnectionObject      실제 핸들      → (a) 붙기
      └─ +0x1cf  ConsoleCommitState                 (a) · (b) 성공 · −1 이 모두 여기서 마무리
```

- 트리의 `+0x..` 는 ret 자리다 (run 문서 "호출 관계" 와 같은 규칙).
- 갈림길 ① = "**콘솔이 필요한 가게인가**" (2.3).
- 갈림길 ② = "**부모가 손잡이 칸에 뭘 적어 줬나**" (2.4).

갈래를 한 표로 (run 문서 "질문 1" 표에서 대표만):

| 조건 | 갈래 | syscall | 반환 |
|---|---|---|---|
| exe `Subsystem ≠ 3` (GUI 앱) | (c) 없음 | 표준 칸이 0 이면 없음. 0 이 아니면 칸마다 `NtQueryVolumeInformationFile` (+ 콘솔 장치면 `NtClose`) | AL 1 |
| 콘솔 앱 + 매니페스트 정책 `d`/`D` + `ConsoleHandle` 0 (또는 −2) | (c) 없음 | 위와 같음 | AL 1 |
| 콘솔 앱 + `ConsoleHandle = −1` | 마무리만 | **1** (`NtSetInformationProcess`) | AL 1 |
| 콘솔 앱 + `ConsoleHandle` 0 · −2 · −3 (위 정책이 아닐 때) | (b) 새 콘솔 | **11** (실패 없을 때) | 성공 AL 1 / 실패 **AL 0** |
| 콘솔 앱 + `ConsoleHandle` = 실제 핸들 | (a) 붙기 | **4** | AL 1 |

- test.exe 는 마지막 줄이다. `ConsoleHandle = 4` `[관찰]` (run44).
- PE_emulator 는 넷째 줄이다 (콘솔 앱 · 매니페스트 없음 · 0). 2.4 에서 단계별로.

비유로:

- 가게 전원을 켤 때 건물 (KERNELBASE) 이 먼저 묻는다: "우리 가게, 쇼윈도 (콘솔) 가 필요한 가게야?" — 아니면 끝.
- 필요하면 개업 서류의 손잡이 칸을 본다:
  - 부모가 **소개장** (진짜 손잡이) 을 넣어 줬으면 → 부모 옆집에 붙는다.
  - "새로 하나 얻어" (0 · −2 · −3) 라고 적혀 있으면 → 상담실에 새 옆집을 부탁한다.
  - "필요 없어" (−1) 면 → 장부 정리만 하고 끝.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ +0x14b  ConsoleAllocate    0 · −2 · −3 → (b) 새 콘솔`

- **누가**: `ConsoleInitialize`.
- **어디서**: 갈림 줄 셋이 먼저 있다 `[정적]`.
  - `+0x170 MOV RDX,[params+0x10]` — 개업 서류의 `ConsoleHandle` 을 꺼낸다 ("params" = `PEB→ProcessParameters`, `GS:[0x60]+0x20`).
  - `+0x174 TEST RDX,RDX` · **`+0x177 JZ`** → `+0x142` — 0 이면 뛴다. −2 는 `+0x183 JZ`, −3 은 `+0x189 JZ` 가 같은 곳으로 뛴다.
  - 도착점 `+0x142` 에서 인자를 준비하고, **call `+0x146`**, 돌아오는 자리 **`+0x14b`** (트리 숫자).
- **누구를**: `ConsoleAllocate` (rva `0xbf41c`, 974B).
- **무엇을 위해**: 부모가 콘솔을 안 물려줬으니 **새 콘솔을 만들려고**.
- 돌아온 뒤: 성공이면 `+0x14d JNS` → `+0x1c6` → `ConsoleCommitState` (마무리). 실패면 `+0x156 RtlDeleteCriticalSection` (열쇠 `ConsoleStateLock` 버리기) → AL 0.

#### 줄 2: `└─ +0xe8  NtDeviceIoControlFile(ConsoleHandle, 0x500016, API 0x01000008)   ── SYSCALL a4`

- **누가**: `GetConsoleLangId` (rva `0x4edf4`). 부른 사슬은 `ConsoleCommitState +0xfa` → `SetTEBLangID +0x12` → 여기.
- **어디서**: call `+0xe1` / ret `+0xe8`. 그 앞에 `+0x4c` 가 개업 서류 `ConsoleHandle` 을 읽고, `+0x62 JLE` 가 "0 이하면 막는다".
- **누구를**: 신청 창구 `NtDeviceIoControlFile` — 상담실에 주문서를 넣는 창구.
- **무엇을 위해**: 주문서 번호 `0x500016`, 그 안의 API 번호 `0x01000008` (`+0xc4`) — **"이 콘솔은 어느 나라 말을 쓰나요"** (콘솔 언어 ID, 2B `WORD`) 를 물으려고.
- 이 줄이 run68 이 빠뜨린 **넷째 syscall** 이다 (2.6).

### 2.3 갈림길 ① — "콘솔이 필요한 가게인가" (`ConsoleShouldAllocateConsole`)

**새 말 — `Subsystem`**: exe 머리 (PE 헤더) 에 적힌 "이 프로그램은 어떤 종류" 칸이다. 3 = 콘솔 프로그램 (까만 창). `ConsoleInitialize +0x7b CMP word [NT+0x5c],3` 이 이걸 본다 `[정적]`. 비유: 가게 간판에 적힌 업종.

**새 말 — 매니페스트의 `consoleAllocationPolicy`**: exe 에 붙일 수 있는 설정 파일 (매니페스트) 안의 "콘솔을 어떻게 받을지" 정책 칸이다. 이 코드가 보는 건 **첫 글자가 `d`/`D` (detached) 인가** 하나뿐이다 `[정적]` (flow-writer 보탬 ⑦). 비유: 개업 서류에 끼워 둔 "쇼윈도 필요 없음" 메모.

걸음 `[정적]`:

1. `+0x7b` 업종이 콘솔 (3) 이 아니면 → `+0x83 JNZ` → `+0x13a` → 수첩 `ConsoleIsConsoleApplication` (`kernelbase+0x3af188`) ← 0 → 갈림길 ① 은 FALSE.
2. 콘솔 앱이면 수첩 ← 1 (`+0x89`). 그리고 매니페스트를 본다:
   - 매니페스트가 없으면 → `+0x54 JS` → `+0x92` **TRUE** (run68 "매니페스트 없음 → TRUE" 와 같다).
   - 첫 글자가 `d`/`D` 가 아니면 → `+0x67 JNZ` → TRUE.
   - `d`/`D` 인데 `ConsoleHandle` 이 0 · −2 가 **아니면** → TRUE (`+0x96 ~ +0x9b`).
   - `d`/`D` 이고 `ConsoleHandle` 이 0 (또는 −2) 일 때만 → FALSE.
3. FALSE 면 `+0xbf JNZ` 가 안 뛰고 `+0xc5` 로 → (c) "아무것도 안 만듦" (2.8).

정리하면 FALSE 는 둘뿐이다: **콘솔 앱이 아님**, 또는 **"쇼윈도 필요 없음" 메모 + 손잡이 칸 0 (또는 −2)**.

test.exe 와 PE_emulator 는 둘 다 콘솔 앱 · 매니페스트 없음 → **TRUE**.

### 2.4 ★ 갈림길 ② — 손잡이 칸 값 넷, 그리고 "0 = 조용히 성공" 이 왜 틀렸나

갈림길 ① 이 TRUE 면 `+0x166` (`ConsoleFlags & 4` 확인) → `+0x170` 에서 `ConsoleHandle` 값을 본다 `[정적]`.

| 값 | 줄 | 어디로 |
|---|---|---|
| 0 | `+0x177 JZ` | `+0x142` → `+0x146 ConsoleAllocate` (새 콘솔) |
| −1 | `+0x179 CMP RDX,-1` · `+0x17d JZ` | `+0x1c6` → 빈 상태로 `ConsoleCommitState` |
| −2 | `+0x183 JZ` | 0 과 같은 곳 |
| −3 | `+0x189 JZ` | 0 과 같은 곳 |
| 그 밖 (실제 핸들) | (떨어짐) | `+0x19a ConsoleCreateConnectionObject` → `+0x1ca ConsoleCommitState` (붙기) |

#### 왜 "0 = 조용히 성공" 이 틀렸나 — 단계별로

1. **짐작**: 부모가 없으니 손잡이 칸이 0 → "콘솔 없음" → 아무것도 안 하고 성공 (AL 1). 원문 요청 27 도 0 을 "콘솔 없음 (`DETACHED_PROCESS`)" 으로 적었다.
2. **근거 ①**: PE_emulator 의 test.exe 는 콘솔 앱이고 매니페스트가 없다 → 갈림길 ① 은 **TRUE** (2.3).
3. **근거 ②**: TRUE 다음 `+0x177 JZ` 가 0 을 **새 콘솔 길** 로 보낸다. "콘솔 없음" 으로 보내는 줄은 없다.
4. **그래서**: 0 은 "콘솔 없음" 이 아니라 "**콘솔을 새로 얻어라**" 다 `[정적]`.
5. **더 나쁜 점**: `ConsoleAllocate` 가 실패하면 `+0x14f ~ +0x162` 가 열쇠 (`ConsoleStateLock`) 를 버리고 **AL 0** 을 돌려준다. 그러면 `_KernelBaseBaseDllInitialize +0x526 TEST AL,AL` · `+0x528 JZ` → `+0xf6` 이 그 AL 0 을 그대로 돌려준다 → **KERNELBASE 전원 켜기 실패 (FALSE)** `[정적]` (flow-writer 보탬 ②).
6. **아직 확인 아님**: 그 뒤 로더가 무엇을 하는지는 이번에 안 봤다 `[추측: 프로세스 초기화 실패]`.

"조용히 AL 1" 로 끝나는 길은 **셋뿐** 이다 `[정적]`:

- (i) exe `Subsystem ≠ 3`
- (ii) `ConsoleHandle = −1`
- (iii) 매니페스트 정책 `d`/`D` + `ConsoleHandle` 0 (또는 −2)

#### 덤 — −1 은 손잡이 칸을 0 으로 바꾼다 (flow-writer 보탬 ③)

- −1 길은 **빈 상태** 로 `ConsoleCommitState` 에 들어간다. 상태 덩어리 `0x30`B 는 `ConsoleInitialize +0x32 ~ +0x3e` 가 0 으로 채워 둔 것이다 `[정적]`.
- `ConsoleCommitState +0x3c` · `+0x40` 은 **늘** 상태 `+8` 값을 손잡이 칸에 쓴다 → −1 이 **0** 이 된다 `[정적]`.
- 그래서 그 뒤 `GetConsoleLangId` 는 손잡이 0 을 보고 `+0x62 JLE` 로 막힌다. syscall 이 `NtSetInformationProcess` **1번** 만 남는 이유다.
- 표준 핸들 칸은 안 바뀐다 (상태 비트가 0 이라서) — **개업 서류 값 그대로**.

#### 비유로

- 부모가 소개장 칸을 **비워 두면** (0), 건물은 "아, 쇼윈도가 없구나, 그냥 넘어가자" 가 아니라 "**쇼윈도가 필요한 가게인데 소개장이 없네? 새로 하나 맞춰야지**" 하고 상담실로 간다.
- 상담실이 대답을 못 하면 건물 전원 켜기 자체가 실패한다.
- 조용히 넘어가려면 처음부터 업종이 콘솔이 아니거나, 부모가 "필요 없음" (−1) 이라고 적어 줬거나, 개업 서류에 "쇼윈도 필요 없음" 메모가 있어야 한다.

### 2.5 (a) 부모 콘솔에 붙기 — test.exe 가 타는 길

run68 해설 2.8 에서 본 길이다. 이번엔 syscall 하나하나의 **값** 이 나왔다.

**새 말 — EA (확장 속성)**: 파일을 열 때 같이 실어 보낼 수 있는 "**이름 = 값**" 꼬리표다 `[추측]` (일반적으로 알려진 설명). 이번엔 `NtCreateFile` 의 `EaBuffer` 에 `FILE_FULL_EA_INFORMATION` 한 칸이 실린다 `[정적]`. 비유: 연결 신청서에 **이름표를 붙여 동봉한 서류 봉투**.

syscall 차례 `[정적]` (run 문서 "(a)" 표에서 대표 값만):

| # | 부르는 자리 (call/ret) | syscall | 핵심 값 | 비유 |
|---|---|---|---|---|
| a1 | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` | `NtCreateFile` | 이름 `\Connect`, **`RootDirectory` = 물려받은 `ConsoleHandle`** (test.exe 4), 접근 `0x12019f`, EA `0x54b`B | 소개장을 내밀고 "연결 줄 놔 주세요" + 서류 봉투 |
| a2 | `ConsoleCommitState +0x8a/+0x91` | `NtDeviceIoControlFile` | 핸들 = a1 새 핸들, IOCTL **`0x500023`**, 출력 8B (= conhost PID) `[추측: 서버 PID 얻기]` | "옆집 번호가 뭐예요?" |
| a3 | `ConsoleCommitState +0xe9/+0xf0` | `NtSetInformationProcess` | `-1`, 클래스 `0x31`, 값 `PID \| 1`, 8B | 관리사무소 장부에 "콘솔 담당은 이 집" (run51) |
| a4 | `GetConsoleLangId +0xe1/+0xe8` | `NtDeviceIoControlFile` | 핸들 = 개업 서류 `ConsoleHandle` (이제 a1 새 핸들), IOCTL **`0x500016`**, API **`0x01000008`**, body 2B | "어느 나라 말 써요?" — **run68 에 없던 것** (2.6) |

- a1 의 결과가 내 **전용 출입증** (사진 `0x60`) 이다. 상태 `+8` 에 들어간다 (`ConsoleInitialize +0x1b5`).
- a2 는 상태 `+8` 이 0 이면 건너뛴다 (`+0x4e/+0x51 JZ`). a3 은 늘 한다 (a2 를 건너뛰면 값 1 로).

#### 서류 봉투 안 — 연결 데이터 `0x53c`B

- EA 한 칸 = `+0x5 EaNameLength` 6 · `+0x6 EaValueLength 0x53c` · `+0x8` 이름 **`"server"`** · `+0xf` 값 = **연결 데이터 `0x53c`B** `[정적]`.
- 연결 데이터는 `ConsoleInitializeServerData` (rva `0xbfbb4`) 가 채운다. 대표 칸만 (칸 위치 `[정적]`, 이름 `[추측]`):
  - `+0x08` 개업 서류 `WindowFlags`
  - `+0x1c` `ProcessGroupId` (0 이면 내 PID)
  - `+0x20` 수첩 `ConsoleIsConsoleApplication`
  - `+0x24` 창 제목 (비면 `"Command Prompt"`)
  - `+0x230` exe 이름 (`PEB→Ldr` 첫 모듈 `BaseDllName`)
  - `+0x332` 현재 디렉터리
- 즉 "나는 이런 가게고, 창 제목은 이거고, 지금 여기 있어요" 라는 **자기소개서** 다. 전체 칸 표는 run 문서.
- 이 줄기는 PEB 에서 **읽기만** 한다 (`+0x10` · `+0x18` · `+0x20` · `+0x30`). PEB 자신에 쓰는 칸은 없다 `[정적]`.

#### 마무리 담당 `ConsoleCommitState` (rva `0xbedb0`) 가 쓰는 칸 `[정적]`

- **`params.ConsoleHandle` ← 상태 `+8`** (`+0x40`) — 늘. test.exe 는 4 → `0x60`.
- 수첩 `ConsoleConnectionState` (`0x3af120`, `0x38`B) ← 상태 통째.
- `params.WindowFlags` 비트 9 · 10 지우기 — 켜져 있을 때만 (`+0x14a` · `+0x159`).
- **`params.Standard*` ← 상태 `+0x18` · `+0x20` · `+0x28`** — 상태 비트 칸 (`+0`) 이 켜졌을 때만 (`+0x11c` · `+0x12e` · `+0x140`).
- `ConsoleFlags` 는 **읽기만** 한다.

#### 붙기 길은 표준 핸들을 안 건드린다 — 단계별로 (flow-writer 보탬 ④)

1. 상태 비트 칸 (`+0`) 은 `ConsoleInitialize +0x32 MOVUPS [RBP-0x40]` 로 0 이 된다 `[정적]`.
2. 붙기 길이 상태에 쓰는 건 `+0x1b5` (`+8` = 새 핸들) · `+0x1bd` (`+0x30`) · `+0x1c0` (`+0x10` = 옛 `ConsoleHandle`) 뿐이다 `[정적]`. 비트 칸은 안 켠다.
3. 그래서 `ConsoleCommitState +0xc3 TEST byte [RBX],7` 이 0 → 표준 칸 쓰기 (`+0x10e ~ +0x144`) 를 안 탄다 `[정적]`.
4. 사진도 그렇다: 표준 핸들 `0x68` · `0x6c` · `0x70` 이 P1a → P1b 그대로 `[관찰]`.

비유: 옆집에 붙을 때는 **출입증만** 새로 받는다. 입 · 귀 · 비상 확성기 (표준 핸들) 는 부모가 개업 서류에 적어 준 것을 **그대로** 쓴다.

### 2.6 ★ run68 정정 — 넷째 syscall, "어느 나라 말 써요?"

**새 말 — 로케일 (LCID)**: "이 직원은 어느 나라 말 · 날짜 꼴로 일하나" 를 정하는 번호다 `[추측]` (일반적으로 알려진 설명). 스레드마다 TEB `+0x108` `CurrentLocale` 칸에 있다 (`_TEB` PDB). 비유: 직원 **이름표에 적힌 사용 언어**.

#### run68 은 왜 셋이라고 했나

- run68 은 `ConsoleCommitState` 에서 **`SetTEBLangID` 한 단계 아래를 안 열었다** (run 문서 "정정").
- 지도 (`ConsoleInitialize_nodes.tsv`) 에는 `GetConsoleLangId` 가 깊이 3 으로 이미 있었다.
- 이번에 지도에서 `ConsoleCommitState → SetTEBLangID → GetConsoleLangId` 가 syscall 로 닿는 걸 보고 넓혀 읽었다.

#### 사슬 `[정적]`

```
ConsoleCommitState +0xff  SetTEBLangID (0xbefa0)
   ├─ +0x17  GetConsoleLangId (0x4edf4)       +0x4c ConsoleHandle · +0x62 JLE (≤ 0 → 막음)
   │     └─ +0xe8  NtDeviceIoControlFile(ConsoleHandle, 0x500016, API 0x01000008)   ── a4
   └─ +0x25  SetThreadLocale(LangId) (0x4ef40)     성공일 때만 (+0x19 JZ 안 탐)
               +0x5a  TEB+0x108 (CurrentLocale) ← LCID
```

- 받은 언어 ID 가 0 이 아니면 `SetThreadLocale` 이 **직원 이름표의 언어 칸** (TEB `CurrentLocale`) 을 쓴다.
- 실패면 `+0xf3 RtlSetLastWin32ErrorAndNtStatusFromNtStatus` 하고 넘어간다. 전원 켜기를 멈추지 않는다.

#### 여기서 또 하나 — 사전에 그 언어가 없으면 (flow-writer 보탬 ⑤)

- 결과서는 "`SetThreadLocale` — syscall 없음" 이라고 했다. 이건 **캐시에 그 언어 노드가 있을 때만** 맞다 `[정적]`.
  - 있으면: `+0x1a ~ +0x4f` 해시 표 찾기 → `+0x5a MOV [TEB+0x108],EBX`. syscall 없음.
  - 없으면: `+0x78` 락 → `+0x86 FindLocaleHashNode` → 그래도 없으면 **`+0x93 MakeLocHashNode`**.
- `MakeLocHashNode` 는 run68 이 `[공백]` 으로 둔 바로 그 함수다 (run68 해설 2.6, 레지스트리 · csrss 후보).
- 그래서 넷째 syscall 뒤에 **NLS 쪽 syscall 이 더 있을 수 있다** `[공백]`.
- 사진의 TEB `CurrentLocale` 은 P1 ~ P2 내내 `0x412` `[관찰]`. 같은 값으로 **다시 썼는지 안 썼는지는 사진으로 못 가른다** `[추측]`.

#### 숫자가 어떻게 바뀌었나

| | run68 | run83 정정 뒤 |
|---|---|---|
| 붙기 길 syscall | 3 | **4** |
| kernelbase 시작 함수 syscall 합 | 15 ~ 16 | **16 ~ 17** (+ NLS 모름) |

비유: run68 은 옆집에 붙으면서 "출입증 받기 · 옆집 번호 묻기 · 장부 등록" 셋만 봤다. 그런데 마무리 담당이 끝에 하나 더 — "**옆집은 어느 나라 말 써요?**" 하고 물어서, 그 답을 직원 이름표에 적는다. 그 사전 (NLS) 에 그 말이 없으면 사전을 새로 펴러 갈 수도 있는데, 그 안은 아직 모른다.

### 2.7 (b) 새 콘솔 — kernelbase 는 옆집을 직접 안 짓는다

test.exe 는 안 타는 길이다. 그런데 **PE_emulator 의 지금 상태 (콘솔 앱 · `ConsoleHandle` 0) 가 타는 길** 이다.

#### 먼저 게시판을 본다 — 옛 길 / 중개 길

- `ConsoleAllocate +0x52 TEST byte [0x7ffe02f0],0x40` — 공용 게시판 (KUSD) `SharedDataFlags` **비트 6 `DbgConsoleBrokerEnabled`** (ntoskrnl PDB 이름) `[정적]`.
  - 켜져 있으면 `+0x5a JNZ` → 중개 길 (`ConsoleAttachOrAllocate(NULL)`).
  - 꺼져 있으면 → **옛 길**.
- 이 VM 은 `SharedDataFlags = 0x10e` `[관찰]` (run44, `04_baggage-read_P1a.txt:2938`) → 비트 6 = 0 → **옛 길만** 탄다.
- 에뮬레이터의 `0x7ffe02f0` 값도 비트 6 이 꺼져 있어야 같은 길이다.
- 이름에 `Dbg` 가 붙었지만 디버거 여부가 아니라 KUSD 기능 비트로 읽힌다 `[추측: 이름]` (run 문서 "디버거 메모").

#### 옛 길 — syscall 표 `[정적]` (대표만)

| # | 부르는 자리 | syscall | 무엇 | 비유 |
|---|---|---|---|---|
| b1 | `ConsoleAllocate +0x149/+0x150` | `NtCreateFile` | `\Device\ConDrv\Server` | 상담실 "서버 창구" 줄 하나 |
| b2 | `+0x2b4/+0x2bb` | `NtCreateFile` | `\Reference`, 기준 = b1 | 그 창구의 **참조 번호표** |
| b3 | `ConsoleLaunchServerProcess +0xc6/+0xcd` | `NtDeviceIoControlFile` | 핸들 b1, IOCTL **`0x500037`**, 입력 = **`RTL_USER_PROCESS_PARAMETERS`** | "이 개업 서류대로 옆집 하나 세워 주세요" |
| b4 | `ConsoleAllocate +0x332/+0x339` | `NtClose` | b1 | 서버 창구 줄 내려놓기 |
| b5 | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` | `NtCreateFile` | `\Connect`, **기준 = b2 참조 핸들**, EA `"server"` | (a1 과 같은 꼴) 연결 신청 |
| b6 | `ConsoleCreateStandardIoObjects +0xb1/+0xb8` | `NtCreateFile` | `\Input`, 기준 = 연결 핸들 | 귀 |
| b7 | `+0x151/+0x158` | `NtCreateFile` | `\Output` | 입 |
| b8 | `+0x1a8/+0x1af` | `NtDuplicateObject` | b7 복사 | 비상 확성기 = **입의 복사본** |
| b9 ~ b11 | `ConsoleCommitState` | a2 · a3 · a4 와 같음 | | 마무리 |

- 합: 실패 없는 옛 길 = **syscall 11** (b1 ~ b8 의 8 + 마무리 3) `[정적]`.
- b1 이 실패하면 `NtSetSystemInformation(0x84, {DWORD 1}, 4)` 하고 **b1 을 한 번 더** 한다 (b1′). 클래스 `0x84` 의 커널 쪽 뜻은 `[공백]` (→ Q158 ④).
- b3 의 개업 서류는 `RtlCreateProcessParametersEx` 로 만든다. 이미지 `\??\<NtSystemRoot>\system32\conhost.exe`, 명령줄 `…conhost.exe 0xffffffff -ForceV1` `[정적]`.

#### 여기서 꼭 짚을 것 — 단계별로

1. run68 문서 · 해설엔 `ConsoleAllocate` 를 "conhost 띄우기" 라고 적었다.
2. 그런데 `ConsoleAllocate` 아래엔 **프로세스를 만드는 syscall 이 없다**. 있는 건 상담실 창구에 넣는 주문서 `0x500037` 하나다 `[정적]`.
3. 그 주문서의 입력이 **conhost 용 개업 서류** (`RTL_USER_PROCESS_PARAMETERS`) 다.
4. 그래서 "kernelbase 는 conhost 를 **직접 만들지 않고**, 상담실 (`ConDrv`) 에 서류를 건네 **맡긴다**" 로 읽는다 `[정적]`. 주문서 이름 "서버 띄우기" 는 `[추측]`.
5. 상담실 안에서 실제로 무슨 일이 나는지 (누가 conhost 를 만드나) 는 condrv.sys 쪽 → **Q149b**.

비유: 쇼윈도 관리할 옆집이 없으면, 가게가 직접 옆집을 짓지 않는다. 관리사무소 콘솔 상담실에 **"이 개업 서류로 옆집 하나 세워 주세요"** 주문서를 넣고, 그 번호표 (참조) 로 연결 신청을 한 다음, 입 · 귀 줄을 하나씩 받는다. 비상 확성기는 입 줄을 복사해서 쓴다.

#### PE_emulator 가 이 길에서 대답해야 할 것 (run 문서 "(b)" 끝 줄)

- **핸들을 돌려주는 `NtCreateFile` 다섯** (b1 · b2 · b5 · b6 · b7)
- **`NtDuplicateObject` 하나**
- **IOCTL 세 종류** (`0x500037` · `0x500023` · `0x500016`/`0x01000008`)
- **`NtSetInformationProcess(0x31)`**
- 하나라도 실패를 돌려주면 kernelbase 시작 함수가 FALSE 다 (2.4).

`params.WindowFlags & 0x100` 이면 b6 ~ b8 대신 `ConsoleSanitizeStandardIoObjects` 를 부른다 (안은 안 읽음 → Q158 ②).

### 2.8 (c) 콘솔 없음 — 손잡이를 어떻게 치우나

갈림길 ① 이 FALSE (콘솔 앱 아님 · "쇼윈도 필요 없음" 메모 + 0) 면 `+0xc5` 로 온다 `[정적]`.

- **안 하는 것**: `NtCreateFile` (연결 · 서버 · 참조 · 입력 · 출력) 전부, IOCTL `0x500023` · `0x500037` · `0x500016`. 마무리 담당 (`ConsoleCommitState`) 도 안 불러서 `NtSetInformationProcess` 도 없다.
- **하는 것**:
  - 콘솔 앱이 아니면 `+0xce ConsoleHandle ← 0`.
  - 표준 칸 셋을 `ConsoleCloseIfConsoleHandle` (rva `0xbf19c`) 로 하나씩 본다 (`+0xe2` · `+0xf7` · `+0x100`).
  - 수첩 `ConsoleConnectionState` 0 (`+0x10a ~ +0x11f`).

#### `ConsoleCloseIfConsoleHandle` — 콘솔 줄만 치운다 `[정적]`

1. 칸이 0 → 그대로.
2. `(값 & 0x10000003) == 3` → syscall 없이 칸만 0.
3. 그 밖 → `+0x4c/+0x53 NtQueryVolumeInformationFile` 로 "이 손잡이 뒤가 무슨 장치?" 를 묻는다.
   - `DeviceType == 0x50` (콘솔 장치) 이면 `+0x66/+0x6d NtClose` + 칸 0.
   - **파일 · 파이프 손잡이는 그대로 남는다.**
- 입력 칸은 `WindowFlags & 0x200`, 출력 칸은 `& 0x400` 이면 안 건드린다. 에러 칸은 늘 검사한다.

비유: 쇼윈도가 필요 없는 가게로 정해지면, 입 · 귀 · 확성기 중 **콘솔 상담실로 이어진 줄만** 뽑는다. 창고 (파일) 나 관 (파이프) 으로 이어진 줄은 그대로 둔다.

### 2.9 콘솔 API — 길이 셋, 주문서는 하나

여기부터는 진입점 뒤 (CRT · `main`) 이야기다. 흐름도 범위 밖이라 flow.md **3장 새 절** 에 적었다.

#### 주문서는 `0x500016` 하나 `[정적]`

- 콘솔 전용 API 는 모두 IOCTL **`0x500016`** 하나로 간다.
- 번호 풀이 (`CTL_CODE` 계산, 이름 `[추측]`): 장치 `0x50` (`FILE_DEVICE_CONSOLE`) · 함수 5 · `METHOD_OUT_DIRECT`.
  - 같은 계산으로 `0x500023` = 함수 8, `0x500037` = 함수 13 (둘 다 연결 때만 씀).
- 일의 종류는 주문서 번호가 아니라 **안쪽 API 번호** 로 갈린다.

#### 새 말 — "설명서" (run 문서의 말, `[추측: CD_USER_DEFINED_IO]`)

`NtDeviceIoControlFile` 의 입력 버퍼에 들어가는 것이다. 내용이 아니라 **"내용은 어디 있다" 목록** 이다 `[정적]`.

```
설명서
 +0x00  8B  Client   (앱 콘솔 핸들, 없으면 0)
 +0x08  4B  입력 버퍼 개수
 +0x0c  4B  출력 버퍼 개수
 +0x10  …   버퍼 칸 16B 씩 { 4B 크기, 4B 빈칸, 8B 포인터 }  (입력 먼저, 출력 나중)
```

- 크기 = `0x10 + 0x10 × (입력 + 출력)` — 1·1 이면 `0x30`, 2·1 이면 `0x40`, 3·2 이면 `0x60`.
- **첫 입력 버퍼 = 메시지**: `+0` API 번호 4B · `+4` body 크기 4B · `+8` body.
- **첫 출력 버퍼 = 같은 메시지의 body 자리** (`메시지 + 8`). 서버가 답을 **같은 자리에 덮어 쓴다**.
- 돌려주는 값 = NTSTATUS 그대로. 메시지 안에 상태 칸은 없다.

비유: 주문서 한 장에 "**내 책상 1번 서랍에 신청 내용, 답은 같은 서랍에 넣어 주세요**" 라고만 적는다. 상담실은 그 서랍 (호출한 가게의 메모리) 을 열어 읽고, 답을 같은 서랍에 넣는다.

→ 그래서 PE_emulator 의 `NtDeviceIoControlFile` 몸은 **설명서 안 포인터를 따라가** 메시지를 읽고, 답을 같은 자리에 써야 한다 (run 문서 질문 2 줄).

#### API 번호 여덟 `[정적]` 값, 이름 `[추측]`

| API 번호 | 쓰는 함수 | body |
|---|---|---|
| `0x01000000` GetCP | `GetConsoleOutputCP` | 8 |
| `0x01000001` GetMode | `GetConsoleMode` | 4 |
| `0x01000002` SetMode | `SetConsoleMode` | 4 |
| `0x01000005` ReadConsole | `ReadConsoleInternal` | `0x14` |
| `0x01000006` WriteConsole | `WriteConsoleInternal` | 8 |
| `0x01000008` GetLangId | `GetConsoleLangId` | 2 |
| `0x02000004` SetCP | `SetConsoleOutputCP` | 8 |
| `0x02000007` GetScreenBufferInfo | `GetConsoleScreenBufferInfoEx` | `0x5c` |

#### 길이 셋 — "어느 손잡이로 보내나" 가 다르다 `[정적]`

| 길 | 함수 | 주문서를 보내는 손잡이 (`FileHandle`) | 앱 핸들은 | 막는 조건 (syscall 없이 `0xc0000008`) |
|---|---|---|---|---|
| (가) 우체통 | `WriteFile` · `ReadFile` | 앱이 준 손잡이 | — | 없음 (2.10) |
| (나) 공용 연결선 | `GetConsoleMode` · `SetConsoleMode` · `GetConsoleScreenBufferInfo(Ex)` · `Get/SetConsoleOutputCP` (`ConsoleCallServer` 계통 + 인라인) | **개업 서류 `ConsoleHandle`** (= 출입증 `0x60`) | 설명서 Client 칸에만 | `ConsoleHandle ≤ 0` |
| (다) 앱 손잡이 직접 | `WriteConsoleW` → `WriteConsoleInternal`, `ReadConsoleW` → `ReadConsoleInternal` → `ConsoleCallServerGeneric` | **앱이 준 손잡이 그 자체** | — | `WriteConsoleW`: `ConsoleHandle == 0` 또는 앱 핸들 ≤ 0. `ReadConsoleW`: 앱 핸들 ≤ 0 (`ConsoleHandle` 안 봄) |

비유:

- (가) 편지를 그냥 **우체통** 에 넣는다. 받는 곳이 상담실이든 창고든 건물은 신경 안 쓴다.
- (나) "모드 · 코드 페이지 · 화면 크기" 같은 상담은 가게의 **출입증 줄** 로 상담실에 묻는다. "어느 입 · 귀 얘기인지" 는 주문서 Client 칸에 적는다.
- (다) 글 쓰기 · 읽기는 **그 입 · 귀 줄로 직접** 주문서를 보낸다.

#### 실패하면 — 대표만 (run 문서 표 B)

- `GetConsoleMode` 는 실패면 FALSE + 오류 코드. `ConsoleHandle ≤ 0` 이면 `ERROR_INVALID_HANDLE` (6) FALSE.
- `GetConsoleOutputCP` 는 실패면 **0 을 돌려준다** (FALSE 가 아니라 CP 값 자리에 0).
- `ReadConsoleW` 는 `0xc0000120` (CANCELLED) 면 오류 코드만 세우고 **TRUE**.
- `GetConsoleScreenBufferInfoEx` 는 `cbSize ≠ 0x60` 이면 **syscall 없이** `0xc000000d` → FALSE.
- 화면 정보 body `0x5c`B 의 칸 표 (표 C) 와 나머지는 run 문서.

### 2.10 `WriteFile` 엔 콘솔 갈림이 없다 — test.exe 의 `Log` 와 손잡이 0 (질문 4 · 5)

#### `WriteFile` 이 보는 것은 둘뿐 `[정적]`

- `WriteFile` (rva `0xb2070`) 의 비교는:
  - `+0x39 CMP ECX,-0xb` · `+0x42 CMP ECX,-0xc` · `+0x4b CMP ECX,-0xa` — 표준 상수면 개업 서류의 `Standard*` 칸 값으로 바꾸기.
  - `+0x63` — `lpOverlapped` 가 있나.
- **핸들 하위 비트 · `ConsoleHandle` 비교는 없다.** 콘솔이든 파일이든 파이프든 `+0x86/+0x8d NtWriteFile` 이다 (겹침이면 `+0x113`, `0x103` 이면 `+0x1d1 NtWaitForSingleObject`).
- `ReadFile` 도 같다 (`+0x3d` · `+0x46` · `+0x4f`).
- 콘솔이면 그 `NtWriteFile` 을 **받는 쪽이 `ConDrv`** 일 뿐이다. kernelbase 가 고른 게 아니다 (질문 5 답).

#### test.exe 의 `Log` 는 이 길 하나 `[정적 + 소스]`

```
Log  (sample/log.h:38)  [소스]
 └─ WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), buf, i, &written, NULL)
      GetStdHandle  — syscall 없음, 개업 서류 +0x28 (StandardOutput) 을 그대로   [정적]
      WriteFile +0x86 → NtWriteFile(그 핸들, …)                                  [정적]
```

- `Log` 는 CRT `printf` 를 일부러 안 쓴다 (`log.h:2`) `[소스]`.
- 그래서 test.exe 의 출력 길엔 **콘솔 IOCTL 이 없다**. 필요한 건 개업 서류 `StandardOutput` 에 **쓸 수 있는 손잡이 하나** 와 **`NtWriteFile` 몸** 뿐이다 `[정적 + 소스]`.
- dll2 TLS 콜백부터 `Log` 마다 `NtWriteFile` 이 한 번씩 — **은퇴 앞 (2.8) 에도** 있다.
- 사진의 `StandardOutput` 은 `0x6c` `[관찰]`. 실제 `NtWriteFile` 이 `0x6c` 로 서는지는 → Q104 동적 4.

#### 손잡이가 0 이면 — 누가 막나 (질문 4) `[정적]`

| 함수 | 0 일 때 | syscall |
|---|---|---|
| `GetStdHandle` | 칸의 0 을 **그대로** 준다. 오류 안 세움 (−1 만 오류, `+0x66 CMP RBX,-1` · `+0x6a JNZ`) | 없음 |
| `WriteFile(0, …)` · `ReadFile(0, …)` | **kernelbase 가 안 막는다** → `NtWriteFile(0)` / `NtReadFile(0)` → 커널이 `0xc0000008` 이면 `+0x143 BaseSetLastNTError` + FALSE | **간다** |
| `GetFileType(0)` · `WriteConsoleW(0)` · `ReadConsoleW(0)` | 막는다 | 없음 |
| `GetConsoleMode` 등 (나) 길 | 앱 핸들이 아니라 **`ConsoleHandle ≤ 0`** 이면 막는다 | `ConsoleHandle` 에 달림 |

#### PE_emulator 의 `NtWriteFile(0)` 26번 — 단계별로

1. PE_emulator 의 개업 서류 `+0x28` = 0.
2. `Log` → `GetStdHandle` 이 0 을 그대로 준다 (`+0x36` 이 `params+0x28` 을 읽음).
3. `WriteFile` 은 0 을 안 거른다 → `+0x86 NtWriteFile(0)`.
4. nt 엔진 (팀원 몸) 이 `0xc0000008` → FALSE.
5. 그래서 "`NtWriteFile(0)` 26번 `0xc0000008`" 은 **이 코드와 맞는다** `[정적]`.
6. 26 이 `Log` 횟수와 같은지는 **안 셌다** `[추측]`.

비유: 입 칸이 비어 있어도 건물은 편지를 막지 않고 **우체통에 그냥 넣는다**. 관리사무소가 "받는 곳 없음" 으로 돌려보낼 뿐이다.

### 2.11 `GetFileType` — "이 손잡이 뒤가 무슨 장치?" (질문 3)

**새 말 — `FileFsDeviceInformation` 의 `DeviceType`**: 손잡이 뒤에 있는 장치의 **종류 번호** 를 묻는 질의다 (클래스 4). `0x50` = 콘솔 장치 (`FILE_DEVICE_CONSOLE` `[추측: 이름]`). 비유: 줄 끝에 달린 게 상담실인지 창고인지 관인지 묻기.

`GetFileType` (rva `0xc6290`, 259B) `[정적]`:

- syscall 은 하나: `+0x72/+0x79 NtQueryVolumeInformationFile(h, &IoStatus, &out, 8, 4)`.
- 받은 `DeviceType` 으로:
  - `0x50` → **2** (`FILE_TYPE_CHAR`, "문자 장치") (`+0x87` · `+0x8c`)
  - `0x11` → **3** (파이프)
  - `2 ~ 8` · `0x24` → **1** (디스크)
  - `0xb` · `0xf` · `0x15` · `0x16` · `0x18` · `0x1b` · `0x1c` · `0x1d` · `0x2b` → 2
  - 그 밖 → `RtlSetLastWin32Error(0)` + **0**
  - 점프표 (rva `0xc63b0` · 바이트 색인 `0xc63c4`) 는 결과서가 바이너리에서 직접 읽었다.
- **핸들 0 → syscall 없이** `BaseSetLastNTError(0xc0000008)` + 0 (`+0x15/+0x18 JZ`). 표준 상수인데 칸이 0 이어도 같다 (`+0x55/+0x58`).
- 0 이 아닌 잘못된 핸들 (−1 등) 은 kernelbase 가 안 거르고 커널 상태를 그대로 오류로 세운다.

PE_emulator 에게:

- 표준 핸들이 0 이면 `GetFileType` 은 **엔진까지 안 온다**.
- 가짜 콘솔 손잡이를 만들면, 이 질의에 **`DeviceType = 0x50`** 을 줘야 "콘솔" 로 보인다.
- CRT 가 `GetFileType` 을 언제 부르나 (콘솔이면 `WriteConsoleW` 를 고르나 등) 는 `[추측]` → Q149b.

### 2.12 PE_emulator 에게 — 맨 먼저 볼 `[공백]`

run 문서 "PE_emulator 쪽에서 쓸 것" 첫 줄의 `[공백]` 이다. 이 바퀴의 결론과 PE_emulator 의 실제 기록이 **안 맞는 곳** 이다.

1. 코드대로면: PE_emulator 의 test.exe (콘솔 앱 · `ConsoleHandle` 0) 는 `ConsoleAllocate` 길 → 성공하면 표준 핸들이 생기고, 실패하면 kernelbase 시작 FALSE (2.4 · 2.7).
2. 그런데 PE_emulator 는 `ConsoleHandle` · 표준 핸들 0 인 채로 **은퇴 뒤 `NtWriteFile(0)` 까지 왔다**.
3. 그러면 둘 중 하나다:
   - (가) 진짜 `ConsoleInitialize` 를 **안 지났다** (흉내 몸 `Src/ntdll/BaseDllInit.cpp` 쪽).
   - (나) `ConsoleAllocate` 의 `NtCreateFile` 들과 IOCTL 이 **가짜 성공** 을 돌려줬다.
4. 결과서는 "(나) 면 표준 핸들이 0 이 아니어야" 라고 했다. 그런데 `ConsoleCommitState` 는 상태 칸 값이 0 이면 안 쓴다 (`+0x112` · `+0x124` · `+0x136 TEST RAX,RAX`) `[정적]`.
   - 가짜 `NtCreateFile` 이 핸들 칸을 안 채우고 성공만 주면 0 그대로일 수 있다 `[추측]`.
   - 그러니 "표준 핸들 0" 만으로는 (가) 와 (나) 를 **못 가른다**.
5. 볼 것 (PE_emulator 쪽에서): 수첩 `ConsoleIsConsoleApplication` (`kernelbase+0x3af188`) 값, `ConsoleInitialize` 반환 `AL`, `ConsoleAllocate +0x150` 의 `NtCreateFile` 상태와 출력 핸들.

요청 27 의 kernelbase 몫을 한 줄로 (run 문서): 콘솔을 흉내 낼 때 중심 syscall 은 **셋** — `NtCreateFile` (`\Device\ConDrv\…` 이름 · `RootDirectory` 기준 이름) · `NtDeviceIoControlFile` (`0x500016` 하나에 API 번호로 갈림, `0x500023` · `0x500037` 은 연결 때만) · `NtWriteFile` / `NtReadFile`. 곁가지는 `NtSetInformationProcess(0x31)` · `NtDuplicateObject` · `NtClose` · `NtQueryVolumeInformationFile`.

### 2.13 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

run 문서 "결과서와 다르게 적은 곳 · 보탠 것" ① ~ ⑧. 본문에서 다룬 자리만 가리킨다.

| # | 무엇 | 이 글 |
|---|---|---|
| ① | `ConsoleAllocate` 의 call 은 `+0x146` (결과서 `+0x142` 는 점프 도착점). 같은 꼴로 `ConsoleAttachOrAllocate` call `+0x303` (결과서 `+0x301`), `WriteFile` 의 `BaseSetLastNTError` call `+0x143` (결과서 `+0x141`) | 조심할 곳 3 · 2.2 |
| ② | `ConsoleInitialize` AL 0 → kernelbase 시작 함수 FALSE | 2.4 |
| ③ | −1 은 `ConsoleHandle` 을 0 으로 | 2.4 덤 |
| ④ | 붙기 길은 표준 핸들을 안 씀 — 근거 줄 + 사진 | 2.5 |
| ⑤ | `SetThreadLocale` "syscall 없음" 은 캐시에 있을 때만 | 2.6 |
| ⑥ | `ConsoleCommitState` 쓰는 줄 정확히 (`+0x14a` · `+0x159` · `+0x11c` · `+0x12e` · `+0x140`) | 2.5 |
| ⑦ | 매니페스트 정책 비교 줄 | 2.3 |
| ⑧ | "0 = (b)" 와 원문 "0 = 콘솔 없음 (`DETACHED_PROCESS`)" 의 짝은 `[추측]` | 조심할 곳 2 |

디버거 메모 `[정적]`: 이번 길을 가르는 칸 (`ConsoleHandle` · `ConsoleFlags` · `WindowFlags` · `Subsystem` · KUSD 비트 6) 에 **디버거를 보는 줄은 없다**. 그러니 디버거가 붙어도 콘솔 갈래는 안 바뀐다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — kernelbase PDB run 목록 | … run82 까지 | + run83 (콘솔 `ConsoleInitialize` · `ConsoleCommitState` · `ConsoleAllocate` · `ConsoleCreateConnectionObject` · `ConsoleCallServer*` 셋 · 콘솔 API 13개 새로, 넓힌 것 `ReadConsoleInternal` · `GetConsoleLangId` · `SetThreadLocale`) |
| 기준 — 사진 메모 | run82 줄 로그 이름이 `_P2.txt:4053` | run82 줄 → **`04_baggage-read_P2.txt:4053`** (이름 끝까지) + **run83 줄 새로** (콘솔 칸 · TEB `CurrentLocale` · KUSD `SharedDataFlags` 다시 읽음, 새 실행 없음) |
| 그림 2 `bc1` 라벨 | "… syscall 3" | "… **syscall 4 — run83 정정**" (그림은 안 늘림, 숫자 하나만) |
| 2.5 제목 run 목록 | … run82 | + run83 |
| 2.5 run68 절 트리 | `ConsoleCommitState` 까지 | + `+0xff SetTEBLangID → +0x17 GetConsoleLangId → +0xe8 NtDeviceIoControlFile(0x60, 0x500016, API 0x01000008)` (넷째 syscall) + `+0x526 TEST AL · +0x528 JZ → +0xf6` 줄 |
| 2.5 "콘솔" 글머리 | "syscall 3번" | ~~3번~~ → **4번** + `SetThreadLocale` 이 TEB `CurrentLocale` 을 씀 + **갈래 전부 요약** (run83) + "**PE_emulator 에 중요**" 줄 (0 은 새 콘솔 길) |
| 2.8 `Log` 절 | — | `WriteFile` → `NtWriteFile` 한 줄 (dll2 TLS 콜백부터, 은퇴 앞에도) `[정적 + 소스]` |
| **3장 새 절 "콘솔 입출력 — kernelbase 콘솔 API"** | — | 비유 + 트리 + `[정적]` 넷 (`WriteFile` 갈림 없음 · `0x500016` 하나 · 손잡이 셋으로 갈림 · `GetFileType`) + 동적 남은 것 |
| 4장 run82 정적 줄 | "대기열 번호 없음" | **Q157** |
| 4장 run83 새 줄 셋 | — | 실행 확인 (Q104, 1 ~ 7) · 정적 남은 것 ① ~ ⑤ + Q149b 로 넘길 것 · PE_emulator 공백 · 정정 · 보탬 |
| 5장 | run82 까지 | + run83 (표 둘) |

태그 · 숫자가 바뀐 곳:

- flow.md 2.5 콘솔 붙기 syscall: `[정적]` **3 → 4** (태그는 그대로, 숫자가 바뀜 — 정정). kernelbase 시작 함수 syscall 합 15 ~ 16 → **16 ~ 17**.
- PE_emulator 짐작 "`ConsoleHandle = 0` → 콘솔 없음 → 조용히 성공" → **틀림** `[정적]` (2.4).
- test.exe `Log` 의 kernelbase 안 길 (`GetStdHandle` → `WriteFile` → `NtWriteFile`): 새로 `[정적 + 소스]`.
- TEB `CurrentLocale`: 쓰는 쪽 후보가 하나 더 (② KERNELBASE `SetThreadLocale +0x5a`). 실제로 다시 썼는지는 `[추측]` (2.6).

### run68 · run82 문서와 짐 지도에서 바뀐 것

- **run68 문서**: 맨 위에 정정 한 줄 (syscall 3 → 4, 합 15 ~ 16 → 16 ~ 17), syscall 표에 **`14′`** 줄 하나.
- **run82 문서** — 뒷정리 셋 (사실이 틀린 건 아님):
  - "정적 — 다음 바퀴 후보" 에 대기열 **Q157** 번호.
  - "호출 관계" 의 `RtlUserThreadStart │ +0x2c (CFG 간접) BaseThreadInitThunk` → 가지 밖 줄 규칙대로 그 줄 자리 **`+0x27 · +0x3d`** (ret `+0x2c` · `+0x42`).
  - 로그 표기 `_P2.txt:4053` → **`04_baggage-read_P2.txt:4053`** (보충 로그가 아니라는 걸 이름으로 분명히).
- **짐 지도** (`baggage.md`): **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** (서른아홉째판). 짐 지도 바퀴가 아니다 — 주제가 대부분 은퇴 뒤 콘솔 API 고, 은퇴 앞 `ConsoleInitialize` 는 run68 이 이미 짐으로 셌다. 이미 있는 줄에 근거만:
  - 3.2 머리 메모 — 콘솔 코드가 개업 서류에서 읽고 쓰는 칸 (`ConsoleFlags` 읽기만, `WindowFlags` · `Standard*` 조건부 쓰기 줄).
  - 3.2 `ConsoleHandle` ② 줄 (넷째 syscall 이 읽음, −1 → 0) · `Standard*` ① 줄 (② 조건부 쓰기 둘, test.exe 안 탐).
  - 3.3 `CurrentLocale` 줄 — ② KERNELBASE `SetThreadLocale +0x5a` 쓰기 후보.
  - 7.2 KUSD `DbgConsoleBrokerEnabled` — 읽는 곳 `ConsoleAllocate +0x52` (쓰는 쪽은 그대로 모름).
  - 7.6.8 `ConsoleIsConsoleApplication` 줄 · "콘솔 연결 핸들" 줄 (a4 가 씀) · `WindowFlags` 메모 (run68 이 "정확한 줄이 없어" 못 옮긴 줄이 `+0x14a` · `+0x159` 로 확정).
  - 새 콘솔 길이 놓을 것 (서버 · 참조 · 연결 · 입력 · 출력 핸들, conhost 프로세스) 은 test.exe 가 안 타서 짐으로 안 셌다.

### 큰 그림에서

- **지도 쪽**: 진입점까지의 정상 길은 안 바뀌었다. 바뀐 건 둘이다.
  - 2.5 콘솔 붙기가 syscall **하나 더** (넷째, 콘솔 언어 ID) — 그리고 그 뒤 NLS 쪽이 더 있을 수 있다는 `[공백]`.
  - 2.8 `Log` 마다 `NtWriteFile` 이 한 번씩 있다는 것 — 은퇴 앞에도 콘솔 출력 syscall 이 있다.
- **은퇴 앞과 뒤가 이어졌다**: 은퇴 앞 `ConsoleInitialize` 가 개업 서류의 `ConsoleHandle` (출입증) 과 표준 핸들 칸을 정하고, 은퇴 뒤 콘솔 API 는 바로 그 칸들로 주문서를 보낸다 — (나) 길은 `ConsoleHandle`, (가) · (다) 길은 표준 핸들.
- **PE_emulator 쪽**: "0 이면 조용히 성공" 이 무너졌다. 콘솔 앱을 돌리려면 (a) 표준 출력을 파일 같은 진짜 손잡이로 주거나, (b) 새 콘솔 길의 syscall 열한 개에 그럴듯하게 대답해야 한다. 어느 쪽이 쉬운지 정하는 "최소 조건 표" 가 다음 바퀴 몫이다.
- **다음**: **Q149b** (run84) — CRT 가 어느 API 를 언제 부르나, condrv.sys 가 받는 쪽, (가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표.

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 남은 것은 네 무리다.

- **실행해 봐야 아는 값** → **Q104** VM 단계 (결과서 "동적분석으로 확인해야 할 것" 번호 그대로, 멈춤점은 test.exe 조건).
- **작은 정적** → 새 **Q158** (① ~ ⑤).
- **CRT · condrv** → **Q149b** (run84).
- **PE_emulator 쪽에서 볼 것** → 대기열 줄 없음 (PE_emulator 몫, 2.12).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| KUSD `0x7ffe02f0` 비트 6 이 실행 때도 꺼져 있는지, 에뮬레이터 값도 같은지 | run44 `0x10e` 는 은퇴까지 사진 값. 에뮬레이터 값은 그쪽 | **Q104** 동적 1 (`dd 7ffe02f0 L1`) |
| 넷째 syscall 이 정말 `0x60` 으로 가나, 상태값, 받은 LangId. `SetThreadLocale+0x93` (`MakeLocHashNode`) 이 서는지, `+0x5a` 뒤 TEB `+0x108` | 실행 때 값. 캐시에 그 언어가 있는지는 실행해 봐야 | **Q104** 동적 2 (`GetConsoleLangId+0xe1` `r rcx`, `+0xe8` `r eax` · `dw @rsp+0x58 L1`) |
| 서버가 주는 값 — 모드 · 코드 페이지 · 화면 정보 body `0x5c` · LangId | 상담실 (condrv · conhost) 의 대답이라 정적으로 못 정함 | **Q104** 동적 3 (`ConsoleCallServer+0xde` · `GetConsoleMode+0xe2` · `GetConsoleOutputCP+0xe5` · `WriteConsoleInternal+0x114` · `ConsoleCallServerGeneric+0x110`) |
| test.exe `Log` 의 `NtWriteFile` 핸들이 정말 `0x6c` 인지, 첫 번이 dll2 TLS 콜백인지, `Log` 에서 `0x500016` 이 **안 서는지** | 소스 + 정적까지. 실제 차례는 실행 | **Q104** 동적 4 |
| −1 · −2 · −3 이 각각 어떤 생성 플래그인지 | 값을 쓰는 부모 쪽을 안 읽음 | **Q104** 동적 5, 또는 정적 **Q158 ①** (`CreateProcessInternalW`) |
| `GetFileType` 이 표준 핸들에 주는 값 (`DeviceType 0x50`?) | 실행 때 값 | **Q104** 동적 6 (`GetFileType+0x79` `eax` · `dd @rsp+0x50 L1`) |
| `ConsoleAllocate+0x150` 이 이 VM 에서 **안 서는지** | 〃 | **Q104** 동적 7 (추천 멈춤점 `GetConsoleLangId+0xe8` · `ConsoleInitialize+0x19f` · `ConsoleCommitState+0x91` · `ConsoleAllocate+0x150`) |
| `ConsoleSanitizeStandardIoObjects` 안 — `ConsoleFlags & 2` · `WindowFlags & 0x100` 갈래, 상태 비트 칸을 켜는 곳 | 줄기 밖이라 이름만 | 정적 **Q158 ②** |
| `ConsoleAttachOrAllocate` 의 `"attach"` 길을 부르는 곳 (지도 caller 2) | 이 VM 은 중개 길을 안 타서 더 안 읽음 | 정적 **Q158 ③** |
| ntoskrnl `NtSetSystemInformation` 클래스 `0x84` 가 무엇을 켜나 (b1 실패 뒤) | 커널 쪽 안 봄 | 정적 **Q158 ④** |
| `SetThreadLocale +0x93 MakeLocHashNode` 아래 — NLS 쪽 syscall 이 더 있나 | run68 때부터 `[공백]` 인 함수 | 정적 **Q158 ⑤** (plan: Q131 (a) 와 같이 닫힘) |
| `ConsoleAllocate` 가 실패해 kernelbase 시작이 FALSE 면 로더가 무엇을 하나 | 이번 범위 밖 `[추측: 프로세스 초기화 실패]` | 대기열 줄 없음 |
| condrv.sys 가 받는 쪽 — `IRP_MJ_CREATE` 이름 여섯 · EA `"server"`/`"attach"`/`"broker"` · IOCTL `0x500016` (API 8개) · `0x500023` · `0x500037` · 읽기/쓰기 · `DeviceType 0x50` · `NtSetSystemInformation(0x84)` · `NtSetInformationProcess(0x31)` 커널 몫 | kernelbase 만 봤다 | **Q149b** (1) |
| CRT (ucrtbase) 가 어느 API 를 언제 부르나 — `isatty` 판단이 `GetFileType` 만인지 `GetConsoleMode` 까지인지, `WriteFile` / `WriteConsoleW` 고르기, 표준 핸들 0 일 때, `/MT` CRT 초기화가 표준 핸들을 묻는지 | CRT 를 안 읽음 (`GetConsoleCP` 도 안 읽음) | **Q149b** (2) |
| (가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표 | 이번 표 + 위 둘을 합쳐야 | **Q149b** (3) |
| PE_emulator 가 진짜 `ConsoleInitialize` 를 지났나, `ConsoleAllocate` 에 가짜 성공을 줬나 | PE_emulator 쪽 실행 기록 | PE_emulator 쪽 (2.12 의 "볼 것") |
| 짐 지도 3.2 · 3.3 에 조건부 줄 (`WindowFlags` ② · `Standard*` ② · `CurrentLocale` ② KERNELBASE) 을 옮길지 | 짐 지도 바퀴 몫 | 짐 지도 바퀴 후보 (run 문서) |

---

## 5. 더 읽기

- run 문서: [`run83-console-kernelbase.md`](run83-console-kernelbase.md) — "PE_emulator 쪽에서 쓸 것" (맨 먼저 · 한 줄 · 질문별 표), **질문 1 갈래별 syscall 표 (a) · (b) · (c)**, 연결 데이터 `0x53c`B 칸 표 전부, **질문 2 API 표 A · B · C**, 질문 3 · 4 · 5, 호출 지점 표, 호출 관계 그림, 결과서와 다르게 적은 곳 ① ~ ⑧, 정정 (run68 · run82 뒷정리), 아직 확인 필요 (동적 1 ~ 7 · 정적 ① ~ ⑤ · Q149b 로 넘길 것)
- 결과서: [`_workspace/sast/80_console-kernelbase_result.md`](../../_workspace/sast/80_console-kernelbase_result.md) — 한 줄 결론, IOCTL `0x500016` 풀이, 공통 경로 세 함수, 질문별 답, Q149b 로 넘길 것, run68 정정 제안
- 요청서: [`_workspace/sast/80_console-kernelbase.md`](../../_workspace/sast/80_console-kernelbase.md)
- flow.md: [3장 새 절 "콘솔 입출력 — kernelbase 콘솔 API"](flow.md) · 2.5 run68 절 (트리 (6) 줄 · "콘솔" 글머리 · run83 갈래 요약) · 그림 2 `bc1` · 2.8 `Log` 줄 · 4장 run83 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.2 머리 메모 · `ConsoleHandle` · `Standard*` 줄, 3.3 `CurrentLocale`, 7.2 KUSD, 7.6.8 콘솔 줄
- 앞 이야기:
  - [run68 해설](run68-dll-init-rest-해설.md) 2.8 — 콘솔 연결 · 소개장 · 출입증 (이번 2.5 의 앞. 거기 적힌 "syscall 3" 은 이번에 4 로 정정), 2.6 — NLS `MakeLocHashNode` 공백 (이번 2.6)
  - [run51 해설](run51-eprocess-later-해설.md) 2.9 — 옆집 (conhost), `0x500023` · `0x31` 로 콘솔 담당 등록 (이번 a2 · a3)
  - [run66 해설](run66-apphelp-cache-해설.md) 2.7 — 드라이버 · IOCTL · IRP (상담실 · 주문서 · 배달 봉투)
  - [run71 해설](run71-named-obj-dir-해설.md) 2.1 — `Log` 와 `OutputDebugStringA` (이번 2.10 의 `WriteFile` 은 그 바로 뒤 줄)
  - [run32 해설](../02_sprint/run32-params-initblock-해설.md) 2.3 — 개업 서류 (매개변수 블록)
  - [run44 해설](run44-baggage-snap-해설.md) 2.8 — 사진 P1a · P1b (`ConsoleHandle` 4 → `0x60`)
  - [run82 해설](run82-exception-user-해설.md) — 바로 앞 바퀴 (요청 26 끝)

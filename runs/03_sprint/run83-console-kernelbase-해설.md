# run83 console-kernelbase — 해설

> 이 글은 [run83 문서](run83-console-kernelbase.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q149a** — PE_emulator 요청 27 의 kernelbase 쪽: 콘솔을 얻는 길 셋, 자주 쓰는 콘솔 API 표, 표준 핸들이 0 이거나 파일·파이프일 때 (`_workspace/plan.md`, 이번에 **끝**)
  - 요청 27 (콘솔 입출력) 은 둘로 나눠서 한다.
    - kernelbase 쪽 = **Q149a** (이번, run83).
    - CRT (ucrtbase) · condrv.sys · "(가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표" = **Q149b** (다음, run84).
  - 이번에 남은 것이 간 곳 (4절):
    - 실행해야 보이는 것 → **Q104** (VM 단계)
    - 작은 정적 다섯 → 새 **Q158**
    - CRT · condrv 쪽 → **Q149b**
- 근거:
  - run 문서 [`run83-console-kernelbase.md`](run83-console-kernelbase.md) (가장 중요)
  - 요청서 [`_workspace/sast/80_console-kernelbase.md`](../../_workspace/sast/80_console-kernelbase.md) · 결과서 [`80_console-kernelbase_result.md`](../../_workspace/sast/80_console-kernelbase_result.md)
    - 판정 **분석성공**. 질문 1 ~ 5 모두 `.asm` 줄로 답했다. run68 정정 제안 하나. 스크립트 문제 없음.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 그림 2 `bc1` 라벨 숫자 하나 · 2.5 · 2.8 `Log` 절 · **3장 새 절 "콘솔 입출력"** · 4장 · 5장 (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음, 서른아홉째판)
  - [run68 문서](run68-dll-init-rest.md) 맨 위 **정정 한 줄** + syscall 표 `14′` 줄
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없다.
  - 글에 나오는 `[관찰]` 값은 flow-writer 가 run44 덤프 읽기 로그를 **다시 읽은 것** 이다: 매개변수 `ConsoleHandle` · `ConsoleFlags` · 표준 핸들 셋 · `WindowFlags`, TEB `CurrentLocale`, KUSD `SharedDataFlags`.

### 이번 바퀴에서 조심할 곳 다섯

1. **"`ConsoleHandle = 0`" 을 두 쪽이 다르게 읽었다.**
   - PE_emulator 는 "0 = 콘솔 없음 → 조용히 지나감" 으로 봤다 (요청서).
   - 코드는 exe 가 콘솔 앱이면 "0 = **새 콘솔을 만들어 줘**" 로 읽는다 `[정적]`.
   - 이게 이번 바퀴의 가장 큰 소식이다 (2.8 · 2.10).
2. **run68 의 "syscall 3번" 은 이제 "4번" 이다** (정정).
   - test.exe 가 실제로 탄 "부모 콘솔에 붙기" 길 이야기다.
   - 갈래도, 사진 값도 그대로다. **빠뜨린 syscall 하나를 더한 것** 뿐이다 (2.7).
   - [run68 해설](run68-dll-init-rest-해설.md) 의 "한 줄로 먼저" 표 · 2.1 표 순서 12 · 2.8 의 "syscall 3", 2.1 끝의 "15 ~ 16번" 은 그 해설을 쓸 때 기준이다. 이 글 2.7 로 바꿔 읽으면 된다.
3. **"핸들" 이 넷 나온다.** 헷갈리기 쉬우니 이 글에서 부를 이름을 먼저 정해 둔다.

   | 이 글에서 부르는 말 | 무엇 | test.exe 값 `[관찰]` run44 |
   |---|---|---|
   | **소개장** | 부모가 넘겨 준 `ConsoleHandle` (처음 값) | 4 |
   | **내 출입증** (연결 핸들) | `\Connect` 를 열어 받은 새 핸들. `ConsoleCommitState` 가 개업 서류의 `ConsoleHandle` 칸에 **덮어 쓴다** | `0x60` |
   | **창구 열쇠 셋** (표준 핸들) | 개업 서류의 `StandardInput` · `StandardOutput` · `StandardError` | `0x68` · `0x6c` · `0x70` |
   | **앱이 넘기는 핸들** | `WriteConsoleW(h, …)` 처럼 API 첫 인자로 주는 핸들 | — |

4. **call 자리와 점프 목적지를 구분한다.**
   - `ConsoleAllocate` 를 부르는 줄은 `+0x177 JZ` 가 뛰어가는 `+0x142` 가 아니라 **call `+0x146` / ret `+0x14b`** 다 (run 문서 "보탠 것" ①).
   - 결과서는 `+0x142` 로 적었다. 같은 곳을 가리키지만 "부르는 줄" 은 `+0x146` 이다.
5. **flow.md 3장 새 절의 비유는 이 글과 다르다.**
   - flow.md 는 콘솔을 "건물 1층 안내 데스크", `WriteFile` 을 "우체통" 으로 그렸다.
   - 이 글은 앞 해설들 그림 (드라이버 = 관리사무소 건물 안 **상담실**, conhost = **옆집**) 을 잇는다.
   - 비유만 다르고 사실은 같다.

### 이미 푼 말 — 링크만

- 오프셋, call/ret, 콜스택, 태그, syscall → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 가게 (프로세스) · 관리사무소 (커널) · 신청 창구 (syscall) · 관리실 (ntdll) → [run68 해설 "비유"](run68-dll-init-rest-해설.md)
- KERNELBASE = 가게에 들인 **건물**, 시작 함수 = 건물 **전원 켜기**, DLL 전역 = 건물 **수첩** → [run67 해설](run67-kb-heap-init-해설.md)
- 직원 (스레드), TEB = 직원 **개인 안내판** → [run54 해설](run54-teb-fill-해설.md)
- 핸들 = **손잡이** (출입증) → [run12 해설](../02_sprint/run12-insert-thread-해설.md)
- 프로세스 매개변수 블록 (`params`) = 부모가 써 준 **개업 서류**, 표준 핸들 = 서류에 적힌 **창구 열쇠** → [run32 해설](../02_sprint/run32-params-initblock-해설.md) 2.3 · 2.7
- 콘솔 호스트 (conhost) = 쇼윈도와 출입문을 관리해 주는 **옆집**, `NtSetInformationProcess` 클래스 `0x31` = "콘솔 담당자 등록" 신청 → [run51 해설](run51-eprocess-later-해설.md) 2.2 · 2.9
- 콘솔 연결 = 소개장 (핸들 4) 을 내밀고 옆집에 연결 신청 → 내 출입증 (새 핸들) → [run68 해설](run68-dll-init-rest-해설.md) 2.8
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = 상담실에 넣는 **번호 붙은 주문서** → [run66 해설](run66-apphelp-cache-해설.md) 2.7
- KUSD = 관리사무소가 붙여 두는 **공용 게시판** → [run70 해설 "비유"](run70-cpu-features-해설.md)
- 매니페스트 = exe 에 붙이는 **설정 쪽지**, 코드 페이지 = **몇 번 글자 사전** → [run57 해설](run57-nls-apiset-해설.md) 2.8
- 사진 P1 · P1a · P1b · P2 → [run44 해설](run44-baggage-snap-해설.md) 2.8
- `Log` → `OutputDebugStringA` 가 하는 일 → [run71 해설](run71-named-obj-dir-해설.md) 2.1

이번에 본문에서 처음 풀 말: **condrv**, **Subsystem 3**, **`RootDirectory`**, **`EaBuffer`**, **IOCTL 번호 풀이**, **API 번호**, **설명서 (메시지 버퍼)**. IOCTL · 표준 핸들 · Subsystem 은 앞 해설에 이름이 나온 적이 있어서 한 줄만 다시 짚는다.

### 비유 — 그대로 이어 쓴다, 새로 셋

- test.exe = **가게**. 부모 cmd.exe 가 써 준 **개업 서류** (`params`) 를 들고 문을 연다.
- KERNELBASE = 가게에 들인 **건물**. 그 건물 전원 켜기 (`_KernelBaseBaseDllInitialize`) 안에 **콘솔 준비** (`ConsoleInitialize`) 칸이 있다.
- 콘솔 창 = 가게의 **쇼윈도**. 쇼윈도를 실제로 그리고 손님 키 입력을 받는 건 **옆집** (conhost) 이다.
- 새 그림 셋:
  - **콘솔 상담실** = `ConDrv` (condrv.sys). 관리사무소 건물 안에 입주한 상담실. 이번에 본 kernelbase 코드는 콘솔 일을 모두 이 상담실의 문 (`\Device\ConDrv\…` 또는 거기서 받은 손잡이) 으로 보낸다 (2.3).
  - **이름표 붙은 봉투** = `EaBuffer`. 문을 열어 달라는 신청서 (`NtCreateFile`) 에 클립으로 붙이는 봉투. 이번 봉투 이름은 `"server"`, 안에는 **가게 소개서** (2.6).
  - **짐 목록표** = 설명서. 상담실 주문서 (`0x500016`) 에 물건을 넣지 않고 "1번 짐은 우리 가게 몇 번 서랍에, 답은 몇 번 서랍에" 라고만 적은 표 (2.13).

### 한 줄로 먼저

- 콘솔 준비 (`ConsoleInitialize`) 는 **두 번 갈린다** `[정적]`.
  1. "콘솔이 필요한 가게인가?" — exe 가 콘솔 앱 (`Subsystem 3`) 이 아니거나, 매니페스트가 "떨어져 있기 (detached)" 를 원하고 소개장이 0 (또는 −2) 이면 → **아무것도 안 만든다**.
  2. 필요하면 소개장 값으로:
     - 실제 핸들 → **부모 콘솔에 붙기** (syscall 4)
     - 0 · −2 · −3 → **새 콘솔** (syscall 11)
     - −1 → 연결 없이 마무리만 (syscall 1)
- test.exe 는 소개장 4 → "붙기" 길 `[정적 + 관찰]`.
  - syscall 은 run68 이 센 3 이 아니라 **4** — 콘솔 언어 ID 를 묻는 IOCTL 이 하나 더 있다 `[정적]` (정정).
- **PE_emulator 의 "0 이면 조용히 성공" 은 틀렸다** `[정적]`.
  - 콘솔 앱이면 0 은 새 콘솔 길이다. 그게 실패하면 kernelbase 시작 함수가 FALSE 다.
- 새 콘솔 길에서도 kernelbase 는 conhost 를 **직접 띄우지 않는다** `[정적]`.
  - 상담실 (`\Device\ConDrv\Server`) 에 IOCTL `0x500037` 로 "이 서류로 띄워 줘" 하고 맡긴다.
- 콘솔 API 는 길이 셋 `[정적]`:
  - `WriteFile` · `ReadFile` → 콘솔인지 **안 묻고** `NtWriteFile` · `NtReadFile`.
  - `GetConsoleMode` 류 → `NtDeviceIoControlFile(내 출입증 = params.ConsoleHandle, 0x500016)`.
  - `WriteConsoleW` · `ReadConsoleW` → `NtDeviceIoControlFile(앱이 준 핸들, 0x500016)`.
  - 콘솔 전용 일은 **IOCTL `0x500016` 하나** 에 **API 번호** 만 바꿔 싣는다. 데이터는 주문서 안이 아니라 설명서 속 포인터 너머에 있다.
- test.exe 가 콘솔에 쓰는 길은 `Log` → `WriteFile` 하나다 `[소스]`.
  - kernelbase 안에선 `NtWriteFile` 한 번 `[정적]`. 콘솔 IOCTL 은 없다.
- `GetFileType` = `NtQueryVolumeInformationFile` 하나. 장치 종류가 `0x50` 이면 "문자 장치" (2) `[정적]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · PEB · 첫 스레드, 개업 서류 옮기기 (소개장 · 창구 열쇠도 새 가게용으로 — run32)
 │  첫 스레드 → ntdll LdrInitializeThunk → LdrpInitializeProcess
 │    ├─ kernel32 들이기 + 전원 켜기 (flow.md 2.5)
 │    │     └─ KERNELBASE 시작 함수
 │    │           ├─ … csrss 연결 · NLS · 락 (run68)
 │    │           └─ +0x526 ConsoleInitialize                         ← 이번 ① 콘솔 얻기 (갈래 전부)
 │    ├─ exe import (dll1 · dll2) 들이기
 │    └─ dll2 · dll1 전원 켜기 — TLS 콜백 · DllMain (flow.md 2.8)
 │          └─ Log → OutputDebugStringA → WriteFile(GetStdHandle(…))     ← 이번 ③ 은퇴 앞에도 NtWriteFile
 └─ NtContinue 은퇴 → exe 진입점 → (CRT) → main
       └─ 콘솔에 쓰고 읽기: WriteFile · WriteConsoleW · GetConsoleMode …  ← 이번 ② 콘솔 API (flow.md 3장 새 절)
```

- ① 과 ③ 은 은퇴 **앞** 이다 (짐 지도 범위 안).
- ② 는 진입점 **뒤** 다. 흐름도 범위 밖이라 flow.md 그림엔 안 넣고 3장에 절 하나로 적었다.

### 왜 이 질문이 지금 왔나

- PE_emulator 는 은퇴 뒤 kernelbase · CRT 를 Unicorn 안에서 **진짜로** 돌린다. syscall 만 nt 엔진 (과 팀원 몸) 이 대답한다 (요청서 배경).
- 그러니 "가짜 콘솔" 을 세우려면, kernelbase 가 condrv 에 **무엇을 보내고 어떤 답을 기대하는지** 알아야 한다.
- 요청 27 은 사용자 결정 (2026-11-12) 으로 요청 26 ~ 30 중 2순위다. PE_emulator 첫 실험 회차 정상 8,056 개 중 약 7 천 개가 콘솔 프로그램이다 (요청서).
- 지금 PE_emulator 상태 (요청서):
  - 부모가 없어서 소개장 (`ConsoleHandle`) · 창구 열쇠 셋이 **모두 0**.
  - test.exe 는 은퇴 뒤 `NtWriteFile(핸들 0)` 이 **26번** `0xc0000008` 을 받았다 (오류 코드로 바꾸면 `ERROR_INVALID_HANDLE` — 결과서).

### 무엇이 궁금했나

번호는 sast/80 번호다. 이번엔 원문 요청 27 번호와 같다.

1. **콘솔을 얻는 길 셋** — 물려받음 · 새 콘솔 · 없음. 길마다 syscall · IOCTL · 바뀌는 칸. 그리고 "모두 0" 이 `ConsoleInitialize` 를 성공 (AL 1) 으로 끝내나.
2. **자주 쓰는 콘솔 API** 가 condrv 에 보내는 것 (API · syscall · IOCTL · 입력 · 기대 답 · 실패면). `WriteFile` 이 "콘솔이면 condrv, 아니면 `NtWriteFile`" 로 가르는 줄이 있나.
3. **`GetFileType`** 이 묻는 법.
4. 표준 핸들이 **0** 이면 `WriteFile(0, …)` 은 syscall 까지 가나.
5. 표준 출력이 **파일 · 파이프** 면 condrv 가 끼나.

### 바퀴 전에 알던 것

- `[정적]` (run51) `ConsoleCommitState` 가 `ConsoleHandle` 을 덮고 `NtDeviceIoControlFile(0x500023)` · `NtSetInformationProcess(0x31)` 을 부른다.
- `[정적]` (run68) test.exe 는 콘솔 앱 · 매니페스트 없음 · 소개장 4 → "부모 콘솔에 붙기". `NtCreateFile(<핸들 4> 기준 "\Connect")`. syscall 을 **3번** 으로 셌다.
  - 0 · −2 · −3 이면 새 콘솔 (`ConsoleAllocate`) 이라는 갈림만 적었고, 안은 안 읽었다.
- `[관찰]` ([run44 문서](run44-baggage-snap.md)) 소개장 4 (`\Reference {ConDrv}`) → `0x60` (`\Connect {ConDrv}`), P1a ~ P1b 사이.
- `[관찰 — PE_emulator 흉내]` 모두 0 → `NtWriteFile(0)` 26번 `0xc0000008` (요청서).

---

## 2. 찾은 것

### 2.1 한눈에 — 그림 둘

#### 그림 ① 콘솔 얻기 — 두 번 갈린다 (`ConsoleInitialize`, rva `0xbeb60`) `[정적]`

```
ConsoleInitialize
 │ +0x7b  exe Subsystem == 3 ?
 │ +0xb8  ConsoleShouldAllocateConsole   ── 첫 갈림: 콘솔이 필요한가   (이 그림의 +0x 는 그 줄 자리 = call)
 │    FALSE → +0xc5  (c) 아무것도 안 만듦                              AL 1
 │    TRUE  → +0x170 소개장 (ConsoleHandle) 값으로   ── 둘째 갈림
 │              ├─ 0 · −2 · −3 → +0x146 ConsoleAllocate     (b) 새 콘솔    syscall 11   성공 AL 1 / 실패 AL 0
 │              ├─ −1          → +0x1ca ConsoleCommitState  빈 상태        syscall 1    AL 1
 │              └─ 실제 핸들    → +0x19a ConsoleCreateConnectionObject
 │                               → +0x1ca ConsoleCommitState (a) 붙기       syscall 4    AL 1   ← test.exe (4)
 └ AL 0 이면 → _KernelBaseBaseDllInitialize +0x528 JZ → +0xf6 : 시작 함수 FALSE
```

- (c) 의 syscall: 창구 열쇠 칸이 0 이면 없음. 0 이 아니면 칸마다 장치 종류를 묻는다 (2.9).

#### 그림 ② 콘솔 API — 길이 셋 `[정적]`

```
(가) WriteFile · ReadFile             → NtWriteFile · NtReadFile (h)                       콘솔인지 안 물음
(나) GetConsoleMode · SetConsoleMode ·
     GetConsoleScreenBufferInfo(Ex) ·
     Get/SetConsoleOutputCP           → NtDeviceIoControlFile(params.ConsoleHandle, 0x500016, API 번호)
(다) WriteConsoleW · ReadConsoleW     → NtDeviceIoControlFile(앱이 준 h, 0x500016, API 번호)
     GetFileType                      → NtQueryVolumeInformationFile(h, 4, 8B)              장치 종류 0x50 → 2
     GetStdHandle                     → syscall 없음 (개업 서류 칸 값 그대로)
```

### 2.2 한 줄씩 읽는 법

#### 줄 1: 이번 바퀴의 핵심 세 줄 — `ConsoleInitialize +0x170 ~ +0x177`

```
+0x170 MOV  RDX,[params+0x10]
+0x174 TEST RDX,RDX
+0x177 JZ   → +0x142
```

- **누가**: kernelbase `ConsoleInitialize`. 첫 갈림이 TRUE ("콘솔이 필요하다") 로 나온 뒤다.
- **어디서**:
  - `+0x170` 에서 개업 서류 `+0x10` 칸 (소개장, `ConsoleHandle`) 을 꺼낸다.
  - `+0x174` 에서 0 인지 본다.
  - `+0x177` 에서 0 이면 `+0x142` 로 뛴다.
- **누구를**: `+0x142` 는 인자 준비 한 줄 (`LEA`) 이고, 바로 뒤 **call `+0x146` / ret `+0x14b`** 가 `ConsoleAllocate` (새 콘솔 만들기) 다.
- **무엇을 위해**: 소개장이 0 = "부모가 넘겨 준 콘솔이 없다". 그러니 **새로 만든다**.
- 같은 꼴로:
  - `+0x183 JZ` (−2), `+0x189 JZ` (−3) 도 `+0x142` 로 간다.
  - `+0x179 CMP RDX,-1` · `+0x17d JZ` (−1) 은 `+0x1c6` (빈 상태로 마무리) 로 간다.

#### 줄 2: 넷째 syscall — 호출 관계 그림의 두 줄

```
     ├─ (1) +0x17  GetConsoleLangId (0x4edf4)      +0x4c ConsoleHandle · +0x62 JLE (≤ 0 → 막음)
     │       └─ +0xe8  NtDeviceIoControlFile(ConsoleHandle, 0x500016, API 0x01000008)   ── SYSCALL a4
```

- **누가**: `GetConsoleLangId` (rva `0x4edf4`).
  - 이 함수는 `ConsoleCommitState +0xfa` → `SetTEBLangID +0x12` 에서 불린다. 가지의 `+0x17` 은 `SetTEBLangID` 안 ret 자리다.
- **어디서**: call `+0xe1` / ret `+0xe8`. 가지의 `+0xe8` 은 ret 자리다.
- **누구를**: `NtDeviceIoControlFile`.
  - 손잡이 = `+0x4c` 에서 꺼낸 `params.ConsoleHandle`. 이 순간엔 방금 받은 **내 출입증** (test.exe `0x60`) 이다.
  - 주문서 번호 `0x500016`, 용건 번호 (API) `0x01000008`.
- **무엇을 위해**: 콘솔의 **언어 ID** (2바이트) 를 받아 온다. 성공하면 `SetThreadLocale` 이 그걸로 TEB `CurrentLocale` (`+0x108`) 을 쓴다.
- **막는 줄**: `+0x62 JLE` — 출입증이 0 이하면 syscall 없이 끝난다.

#### 줄 3: "없다" 를 읽는 법 — `WriteFile` 의 비교 줄

```
+0x39 CMP ECX,-0xb
+0x42 CMP ECX,-0xc
+0x4b CMP ECX,-0xa
```

- **누가**: kernelbase `WriteFile` (rva `0xb2070`).
- **무엇을**: 받은 핸들이 표준 핸들 **상수** (−11 출력 · −12 오류 · −10 입력) 인지 본다. 맞으면 개업 서류의 창구 열쇠 칸 값으로 바꾼다.
- **그다음**: `+0x63` 은 `lpOverlapped` 가 있는지만 본다. 그리고 `+0x86` call → `NtWriteFile`.
- 여기서 읽을 것은 **없는 줄** 이다.
  - 핸들 아래 비트를 보는 줄도 없다.
  - 출입증 (`ConsoleHandle`) 과 비교하는 줄도 없다.
  - 그래서 "콘솔이면 condrv 로, 아니면 `NtWriteFile` 로" 가르는 곳이 kernelbase 엔 없다 (2.15).

### 2.3 새 말 — condrv, 그리고 상담실의 문 이름들

- **condrv** (`ConDrv`, 드라이버 파일 condrv.sys): 콘솔 드라이버.
  - 이번에 kernelbase 가 콘솔 일로 여는 이름은 모두 `\Device\ConDrv\…` 이거나, 거기서 받은 손잡이 기준이다 `[정적]` (이번 syscall 표).
  - 비유: 관리사무소 건물 안 **콘솔 상담실** (드라이버 = 상담실, run66 해설).
- 이번에 나온 문 이름 `[정적]` (`.rdata` 문자열은 결과서가 직접 읽음):

| 문 이름 | 무엇을 기준으로 여나 | 쓰는 길 |
|---|---|---|
| `\Device\ConDrv\Server` | 이름 그대로 | (b) 새 콘솔 b1 |
| `\Reference` | 서버 손잡이 기준 | (b) b2 |
| `\Connect` | **소개장 (부모 핸들)** 기준, 또는 참조 손잡이 기준 | (a) a1 · (b) b5 |
| `\Input` · `\Output` | 연결 손잡이 (내 출입증) 기준 | (b) b6 · b7 |
| `\Device\ConDrv\Connect` | 이름 그대로 (기준 손잡이가 NULL 일 때) | 중개 길 (이 VM 은 안 탐) |

- **새 말 — `RootDirectory`**: `NtCreateFile` 에 주는 `OBJECT_ATTRIBUTES` 안의 칸. "이 이름을 **이 손잡이 기준으로** 찾아 주세요" 라는 뜻이다.
  - a1 은 이름이 `\Connect` 하나뿐이고, `RootDirectory` = 소개장 4 다. NULL 이면 이름이 `\Device\ConDrv\Connect` 가 된다 `[정적]`.
  - 비유: 주소를 "OO동 3번지 상담실 `\Connect` 문" 처럼 통째로 쓰지 않고, "**4번 손잡이 방 안의** `\Connect` 문" 처럼 쓰는 것.
- 하나 짚어 볼 것 — 내 짐작:
  - `[관찰]` (run44 문서) 소개장 4 는 사진에서 `\Reference {ConDrv}` 였다.
  - `[정적]` 새 콘솔 길 b5 는 "참조 손잡이 기준 `\Connect`" 다.
  - `[추측]` 그러면 붙기 길 a1 과 새 콘솔 길 b5 는 **같은 꼴** 이다. 다른 점은 참조 손잡이를 부모가 줬나 (a), 내가 b1 · b2 로 만들었나 (b) 뿐으로 보인다. 부모 쪽 코드는 안 읽어서 확인은 아니다.

### 2.4 첫 갈림 — 콘솔이 필요한 가게인가 (`ConsoleShouldAllocateConsole`)

- **새 말 — Subsystem 3**: exe 헤더 (NT 헤더 `+0x5c`) 의 "어떤 종류 프로그램인가" 칸. 3 = 콘솔 (까만 창) 프로그램, 2 = 창 (GUI) 프로그램이다.
  - run19 · run20 해설에서 "kernel32 를 들일지" 정할 때 본 바로 그 칸이다.
  - 비유: 개업 신고서의 **업종** 칸. "쇼윈도가 필요한 가게" 인지 적혀 있다.
- 걸음 `[정적]`:
  1. `ConsoleInitialize +0x7b CMP word [NT+0x5c],3` · `+0x83 JNZ`.
     - 3 이면 `+0x89` 에서 `ConsoleIsConsoleApplication ← 1`.
     - 아니면 `+0x13a` 로 가서 0.
  2. `+0xb8` call `ConsoleShouldAllocateConsole`:
     - 콘솔 앱이 아니면 (`+0x16` · `+0x1d JZ`) → FALSE.
     - 콘솔 앱이면 매니페스트의 `consoleAllocationPolicy` 를 본다.
     - 매니페스트가 없으면 `+0x54 JS` → TRUE.
     - 정책 첫 글자가 `'D'` 또는 `'d'` (detached = 떨어져 있기) 일 때만 detached 로 본다 (`+0x56 ~ +0x67`). 다른 글자면 `+0x67 JNZ` → TRUE.
     - detached 이고 소개장이 0 이면 `+0x76 CMP` · `+0x7b JNZ` 가 안 뛰어 `+0x7d` FALSE. −2 도 FALSE 쪽이다. 그 밖의 소개장 값이면 detached 여도 TRUE (`+0x96 ~ +0x9b`).
  3. `+0xbf JNZ` — TRUE 면 `+0x166` (둘째 갈림), FALSE 면 `+0xc5` (콘솔 없음).
- test.exe: Subsystem 3 · 매니페스트 없음 → **TRUE** `[정적 + 관찰]` (run68).

### 2.5 둘째 갈림 — 소개장 값 다섯 가지

| 소개장 (`ConsoleHandle`) | 줄 | 가는 곳 |
|---|---|---|
| 실제 핸들 (0 · −1 · −2 · −3 이 아닌 값) | `+0x19a` | (a) 부모 콘솔에 붙기 |
| 0 | `+0x177 JZ` | (b) 새 콘솔 |
| −2 | `+0x183 JZ` | (b) 새 콘솔 |
| −3 | `+0x189 JZ` | (b) 새 콘솔 |
| −1 | `+0x17d JZ` → `+0x1c6` | (c) 연결 없이 마무리만 |

- 특수 값들의 이름은 `[추측]` 이다.
  - −1 · −2 · −3 이 각각 `DETACHED_PROCESS` · `CREATE_NEW_CONSOLE` · `CREATE_NO_WINDOW` (부모가 프로세스를 만들 때 주는 플래그) 라는 짐작.
  - 값을 **쓰는** 부모 쪽 (`CreateProcessInternalW`) 을 안 읽어서 확인 못 했다 → Q158 ①.
  - 그래서 원문 요청이 "콘솔 없음 (`DETACHED_PROCESS`)" 을 0 과 짝지은 것도 `[추측]` 이다 (run 문서 보탬 ⑧). **읽는 쪽 코드에서는 0 이 새 콘솔이다** `[정적]`.
- 곁가지 `[정적]`:
  - `ConsoleRationalizeAllocationOptionsWithPeb` (`+0x8f`) 가 소개장 0 · −2 → 전역 `0x3aa9f8 ← 1`, −3 → 2, −1 → `|= -1` 로 적어 둔다. 이 값이 연결 데이터 `+0x21` 바이트로 들어간다. test.exe (4) 는 아무것도 안 쓴다.
  - `ConsoleFlags & 4` (표준 칸 먼저 닫기) · `& 2` (`ConsoleSanitizeStandardIoObjects`) 곁가지도 있다. test.exe 는 `ConsoleFlags = 0` `[관찰]` 이라 안 탄다.

### 2.6 길 (a) 부모 콘솔에 붙기 — test.exe 의 길, syscall 넷

| # | 어디서 (call/ret) | syscall | 무엇 |
|---|---|---|---|
| a1 | `ConsoleCreateConnectionObject +0x1c1/+0x1c8` ← `ConsoleInitialize +0x19a/+0x19f` | `NtCreateFile` | `\Connect`, 기준 = 소개장 4, 접근 `0x12019f`, **`EaBuffer` `0x54b`B** → 새 핸들 (내 출입증) |
| a2 | `ConsoleCommitState +0x8a/+0x91` | `NtDeviceIoControlFile` | 내 출입증, IOCTL `0x500023`, 입력 없음, 출력 8B — conhost PID 로 보인다 `[추측: 서버 PID 얻기]` |
| a3 | `ConsoleCommitState +0xe9/+0xf0` | `NtSetInformationProcess` | `-1`, 클래스 `0x31`, 값 `PID \| 1`, 8B — "콘솔 담당자 등록" (run51) |
| a4 | `ConsoleCommitState +0xfa` → `SetTEBLangID +0x12` → `GetConsoleLangId +0xe1/+0xe8` | `NtDeviceIoControlFile` | `params.ConsoleHandle` (이제 내 출입증), IOCTL `0x500016`, API `0x01000008` → 언어 ID 2B — **run68 에 없던 넷째** |

- 차례대로, 비유로 `[정적]`:
  1. 소개장 4 를 기준으로 "`\Connect` 문을 열어 주세요" 신청 (a1). 신청서에 봉투를 붙인다 (아래).
  2. 받은 출입증으로 상담실에 "옆집 번호 (PID) 알려 주세요" (a2).
  3. 관리사무소에 "우리 콘솔 담당은 이 집" 등록 (a3).
  4. 출입증으로 "쇼윈도 언어는?" (a4). 답이 오면 직원 개인 안내판 (TEB) 의 언어 칸을 맞춘다.
- `[관찰]` (run44) 소개장 4 → `0x60`, P1a → P1b. 이 갈래와 맞다.
- 실패 갈래 `[정적]`:
  - a1 이 `0xc0000022` 를 받고 `ConsoleIsCallerInLowbox` 가 "로우박스" 라고 하면 → 소개장을 닫고 (`+0x1fe NtClose`) **(b) 새 콘솔로** 간다.
  - 로우박스는 격리된 앱 상자 (AppContainer) 로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 그 밖의 실패는 AL 0.

#### 새 말 — `EaBuffer` (이름표 붙은 봉투)

- `NtCreateFile` 에 덧붙여 보낼 수 있는 꾸러미 칸이다. 모양은 `FILE_FULL_EA_INFORMATION` — "이름 + 값" 칸이 이어진 것 `[정적]` (칸 모양).
  - EA 는 흔히 "확장 속성 (Extended Attributes)" 의 줄임으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 여기서는 파일 속성이 아니라 **상담실에 건네는 편지** 로 쓰인다 `[추측]`.
  - 이유: 값이 가게 소개서 (아래 연결 데이터) 이고, 이름이 `"server"` · `"attach"` · `"broker"` 같은 용건 이름이다 `[정적]`.
- a1 봉투 (`0x54b`B, `ConsoleCreateConnectionObject +0xb2` · `+0xc3 ~ +0xe3` · `+0xe6` 복사) `[정적]`:

| 칸 | 값 |
|---|---|
| `+0x0` `NextEntryOffset` | 0 (봉투 하나뿐) |
| `+0x4` `Flags` · `+0x5` 이름 길이 | 0 · 6 |
| `+0x6` 값 길이 | `0x53c` |
| `+0x8` 이름 | `"server"` (ANSI, NUL 포함 7B) |
| `+0xf` 값 | **연결 데이터 `0x53c`B** |

- 연결 데이터 (`ConsoleInitializeServerData`, rva `0xbfbb4`) — 대표만. 칸 위치 `[정적]`, 이름 `[추측]`:
  - 창 모양 칸들 — 개업 서류의 `WindowFlags` · `FillAttribute` · `CountChars` · `Count` · `Starting` X/Y (해당 비트가 켜진 것만), `ShellInfo` 의 `"dde."` · `"hotkey."` 뒤 숫자.
  - `+0x1c` 프로세스 그룹 ID (0 이면 내 PID).
  - `+0x20` 콘솔 앱인가 (`ConsoleIsConsoleApplication`) · `+0x21` 할당 옵션 (2.5 곁가지).
  - `+0x24` 창 제목 (비면 `"Command Prompt"`) · `+0x230` 내 exe 이름 · `+0x332` 현재 폴더.
  - 비유: 신청서에 붙인 **가게 소개서** — "가게 이름은 test.exe, 쇼윈도 제목은 이것, 우리 현재 폴더는 여기".
- 이 줄기가 PEB 에서 **읽기만** 하는 칸: `+0x10` · `+0x18` · `+0x20` · `+0x30`. PEB 자신에 쓰는 칸은 없다 `[정적]`.
- 이름 인자가 있으면 (`"attach"` · `"broker"` — `ConsoleAttachOrAllocate` 길) 둘째 봉투가 `+0x54c` 에 붙는다 `[정적]`. test.exe 는 안 탄다.

### 2.7 ★ run68 정정 — 붙기 길 syscall 은 3 이 아니라 4

헷갈리지 않게 한 표로:

| | run68 (전) | run83 (후) |
|---|---|---|
| 갈래 | 부모 콘솔에 붙기 | **그대로** |
| syscall | `NtCreateFile` · `NtDeviceIoControlFile(0x500023)` · `NtSetInformationProcess(0x31)` = **3** | 위 셋 + **`NtDeviceIoControlFile(0x500016, API 0x01000008)`** = **4** |
| kernelbase 시작 함수 syscall 합 | 15 ~ 16 (+ NLS 모름) | **16 ~ 17** (+ NLS 모름) |
| 사진 값 (`ConsoleHandle 4 → 0x60` 등) | `[관찰]` | **그대로** |

- 왜 빠졌나 — 단계별로 `[정적]`:
  1. run68 은 `ConsoleCommitState +0xfa/+0xff SetTEBLangID` 까지만 봤다. 이름만 보면 "TEB 에 언어 ID 적기" 라서 syscall 이 없을 것 같다.
  2. 그런데 `SetTEBLangID` 는 먼저 `+0x12/+0x17 GetConsoleLangId` 를 부른다.
  3. 그 안 `+0xe1/+0xe8` 이 `NtDeviceIoControlFile` 이다.
  4. 지도 (`ConsoleInitialize_nodes.tsv`) 에는 `GetConsoleLangId` 가 깊이 3 으로 있었다. 한 단계 아래를 안 열었을 뿐이다.
- 넷째 syscall 뒤에 이어지는 일 `[정적]`:
  - 성공이면 `SetTEBLangID +0x20/+0x25 SetThreadLocale(LangId)` → `+0x5a` 에서 TEB `+0x108` (`CurrentLocale`) ← LCID.
  - 결과서는 "`SetThreadLocale` 은 syscall 없음" 이라고 했다. 그런데 이건 **언어 노드가 캐시에 있을 때** 만이다 (run 문서 보탬 ⑤).
    - 없으면 `+0x93 MakeLocHashNode` 로 간다. run68 이 `[공백]` 으로 둔 바로 그 함수다.
    - 그래서 넷째 뒤에 NLS syscall 이 더 있을 수도 있다 `[공백]` → Q158 ⑤ (Q131 (a) 와 같이 닫힘).
  - 실패면 `+0xf3 RtlSetLastWin32ErrorAndNtStatusFromNtStatus` 하고 **넘어간다**. 콘솔 준비가 멈추지 않는다.
- `[관찰]` TEB `CurrentLocale` 은 사진 P1 ~ P2 내내 `0x412` 다.
  - 넷째 syscall 뒤에 같은 값으로 다시 썼는지, 안 썼는지는 사진으로 못 가른다 `[추측]`.
- 고친 곳:
  - run68 문서 맨 위 정정 한 줄 + syscall 표 `14′` 줄.
  - flow.md 2.5 콘솔 글머리 · 그림 2 `bc1` 라벨 ("syscall 3" → "syscall 4").
  - 짐 지도 7.6.8 "콘솔 연결 핸들" 줄 · 3.3 `CurrentLocale` 줄 (근거만).
  - **run68 해설은 안 고쳤다.** 거기 "3" · "15 ~ 16" 은 이 절로 바꿔 읽는다.

### 2.8 길 (b) 새 콘솔 — syscall 11, conhost 는 상담실이 띄운다

#### 먼저 게시판 한 칸

- `ConsoleAllocate +0x52 TEST byte [0x7ffe02f0],0x40` `[정적]`.
  - 공용 게시판 (KUSD) `SharedDataFlags` 의 비트 6. PDB 이름은 `DbgConsoleBrokerEnabled`.
  - 켜져 있으면 `+0x5a JNZ` → `ConsoleAttachOrAllocate` (중개 길) 부터. 꺼져 있으면 옛 길.
- `[관찰]` 이 VM 은 `0x10e` (run68 이 읽은 줄) → 비트 6 = 0 → **옛 길만** 탄다.
  - 에뮬레이터의 `0x7ffe02f0` 값도 비트 6 이 꺼져 있어야 같은 길이다.
  - 이름에 `Dbg` 가 붙었지만 "디버거가 붙었나" 를 보는 칸이 아니라 KUSD 기능 비트로 읽힌다 `[추측: 이름]` (run 문서 디버거 메모).

#### 옛 길, 차례대로 `[정적]` (`ConsoleAllocate`, rva `0xbf41c`, 974B)

| # | syscall | 무엇 | 비유 |
|---|---|---|---|
| b1 | `NtCreateFile` | `\Device\ConDrv\Server` → 서버 손잡이 | 상담실에 "새 쇼윈도 서버 자리 하나" 신청 |
| b2 | `NtCreateFile` | 서버 기준 `\Reference` → 참조 손잡이 | 그 자리의 **소개장** 을 내가 직접 뽑음 |
| b3 | `NtDeviceIoControlFile` | 서버 손잡이, IOCTL `0x500037` `[추측: 서버 띄우기]`, 입력 = **매개변수 블록** (`conhost.exe 0xffffffff -ForceV1`) | "이 개업 서류로 옆집 하나 열어 주세요" |
| b4 | `NtClose` | 서버 손잡이 닫기 | |
| b5 | `NtCreateFile` | 참조 기준 `\Connect` + 봉투 `"server"` (a1 과 같은 꼴) → 연결 손잡이 | 내 출입증 받기 |
| b6 · b7 | `NtCreateFile` ×2 | 연결 기준 `\Input` · `\Output` | 창구 열쇠 둘 깎기 |
| b8 | `NtDuplicateObject` | 출력 손잡이 복사 → 표준 오류 | 오류 창구 열쇠 = 출력 열쇠의 **복사본** |
| b9 ~ b11 | (commit) | a2 · a3 · a4 와 같음 (연결 손잡이로) | |

- 합: 실패 없는 옛 길 = **syscall 11** (b1 ~ b8 의 8 + commit 3).
- 눈여겨볼 점 `[정적]`: **kernelbase 는 conhost 를 직접 띄우지 않는다.**
  - 위 표에 프로세스를 만드는 syscall 이 없다.
  - `ConsoleLaunchServerProcess` 는 `RtlCreateProcessParametersEx` 로 **개업 서류 한 장** 을 만들어 (`+0x67/+0x6e`) 상담실에 IOCTL 로 건넬 뿐이다 (`+0xc6/+0xcd`).
  - 서류 안: 이미지 `\??\<NtSystemRoot>\system32\conhost.exe`, 명령줄 `…conhost.exe 0xffffffff -ForceV1`, 데스크톱 = 내 `params.DesktopInfo`. 끝에 `RtlDestroyProcessParameters`.
  - 상담실이 그 서류로 옆집을 어떻게 여는지는 condrv 쪽 일이다 (Q149b).
- 실패하면 `[정적]`:
  - b1 이 실패하면 `NtSetSystemInformation(0x84, {1}, 4)` 하고 b1 을 한 번 더 한다 (b1′). 클래스 `0x84` 의 뜻은 `[공백]` (Q158 ④).
  - 그래도 실패하거나 다른 데서 실패하면 → `ConsoleCleanupConnectionState` (`NtClose` 5개까지) → `ConsoleAllocate` 가 실패를 돌려줌 → `ConsoleInitialize` 가 `ConsoleStateLock` 을 지우고 (`+0x156 RtlDeleteCriticalSection`) **AL 0**.
- `params.WindowFlags & 0x100` 이면 b6 ~ b8 대신 `ConsoleSanitizeStandardIoObjects` (이미 가진 표준 핸들 검사 · 대체 — 안은 안 읽음, Q158 ②).
- 중개 길 (비트 6 켜짐) 은 `\Device\ConDrv\Connect` 를 바로 열고 conhost 를 띄우는 IOCTL 이 없다. 이 VM 은 안 타서 더 안 읽었다.
- 비유로 한 번 더:
  - 소개장 없이 문을 연 새 가게는, 상담실에 "쇼윈도 서버 자리" 를 하나 신청하고 (b1), 그 자리의 소개장을 직접 뽑는다 (b2).
  - "이 서류로 옆집 하나 열어 줘" 하고 상담실에 맡긴다 (b3). 가게가 직접 옆집을 짓지 않는다.
  - 뽑은 소개장으로 연결해 출입증을 받고 (b5), 창구 열쇠 셋을 깎는다 (b6 ~ b8).
  - 그 뒤 마무리 (b9 ~ b11) 는 붙기 길과 같다.

### 2.9 길 (c) 콘솔 없음 — 조용히 끝나는 세 가지

| | 어디서 갈리나 | 하는 일 | syscall | 창구 열쇠 셋 |
|---|---|---|---|---|
| ① 콘솔 앱 아님 (`Subsystem ≠ 3`) | 첫 갈림 FALSE → `+0xc5` | `+0xce` 소개장 ← 0. 창구 열쇠 셋을 `ConsoleCloseIfConsoleHandle` 로 검사 (`+0xe2` · `+0xf7` · `+0x100`) | 칸이 0 이면 0. 아니면 칸마다 장치 묻기 (+ 콘솔이면 닫기) | **콘솔 장치를 가리킬 때만** 닫고 0. 파일 · 파이프는 그대로 |
| ② 콘솔 앱 + 매니페스트 detached + 소개장 0 (또는 −2) | 첫 갈림 FALSE → `+0xc5` | ① 과 같음. 단 `+0xcc JNZ` 로 `+0xce` 를 건너뜀 → 소개장 그대로 | 같음 | 같음 |
| ③ 콘솔 앱 + 소개장 −1 | 둘째 갈림 `+0x17d JZ` → `+0x1c6` | 빈 상태로 `ConsoleCommitState` → `+0x40` 이 소개장 −1 → **0** | `NtSetInformationProcess(-1, 0x31, {1}, 8)` **1번** | 안 바뀜 — 개업 서류 값 그대로 |

- ③ 에서 syscall 이 하나뿐인 까닭 `[정적]`:
  - a2 는 상태 `+8` (출입증) 이 0 이라 건너뛴다 (`ConsoleCommitState +0x4e/+0x51 JZ`).
  - a4 는 그 전에 `+0x40` 이 소개장 칸을 0 으로 바꿔 놓아서 `GetConsoleLangId +0x62 JLE` 로 막힌다.
  - a3 만 늘 부른다 (값 1 로).
- ① ② 는 `ConsoleCommitState` 를 아예 안 불러서 `NtSetInformationProcess` 도 없다.
- 셋 모두 연결 · 서버 · 참조 · 입력 · 출력 `NtCreateFile` 과 IOCTL 셋 (`0x500023` · `0x500037` · `0x500016`) 이 **없다**.
- 콘솔 장치인지 가리는 법 (`ConsoleCloseIfConsoleHandle`, rva `0xbf19c`) `[정적]`:
  - 칸 0 → 그대로.
  - `(값 & 0x10000003) == 3` → syscall 없이 칸만 0.
  - 그 밖 → `+0x4c/+0x53 NtQueryVolumeInformationFile` 로 장치 종류를 묻고, `0x50` 이면 `+0x66/+0x6d NtClose` + 칸 0.
  - 입력 칸은 `WindowFlags & 0x200`, 출력 칸은 `& 0x400` 이면 안 건드린다. 오류 칸은 늘 검사한다.
- 비유: 쇼윈도가 필요 없는 가게 (①) 이거나 "떨어져 있겠다" 고 쪽지에 쓴 가게 (②) 는 상담실에 아무 신청도 안 한다. 서류에 콘솔 쪽 열쇠가 끼어 있으면 그것만 정리한다. 창고 (파일) 열쇠는 그대로 둔다.

### 2.10 ★ "소개장 0 이면 조용히 성공" 이 왜 틀렸나 — 단계별로

PE_emulator 의 생각 (요청서 질문 1 끝줄): 부모가 없으니 모두 0 → "콘솔 없음" 갈래 → `ConsoleInitialize` 가 아무것도 안 하고 AL 1.

코드를 따라가면 `[정적]`:

1. exe 가 콘솔 앱이다 (`Subsystem 3`) → `+0x89 ConsoleIsConsoleApplication ← 1`.
2. 매니페스트가 없다 → `ConsoleShouldAllocateConsole +0x54 JS` → **TRUE** ("콘솔이 필요하다").
3. `+0xbf JNZ` → `+0x166` → `+0x170` 에서 소개장을 읽는다 → **0**.
4. `+0x177 JZ` → `+0x142` → call `+0x146` **`ConsoleAllocate`** — 새 콘솔 만들기 (2.8 의 syscall 11).
5. `ConsoleAllocate` 가 실패를 돌려주면 → `ConsoleInitialize` AL 0 → `_KernelBaseBaseDllInitialize +0x526 TEST AL,AL` · `+0x528 JZ` → `+0xf6` → **kernelbase 시작 함수 FALSE**.
6. 그 뒤 로더가 무엇을 하는지는 이번에 안 봤다 `[추측: 프로세스 초기화 실패]`.

그래서:

- 콘솔 앱에게 소개장 0 은 "콘솔 없음" 이 아니라 **"새로 만들어 줘"** 다.
- 조용히 AL 1 로 끝나는 길은 셋뿐이다 (2.9): ① 콘솔 앱 아님 ② 소개장 −1 ③ detached 매니페스트 + 소개장 0 (또는 −2).
- 비유: 개업 서류의 "쇼윈도 소개장" 칸이 비었다 = "부모 쇼윈도를 같이 쓸 게 없다" 는 뜻이다. 업종이 "쇼윈도 가게" 면, 가게는 포기하는 게 아니라 **새 쇼윈도를 신청** 한다. 신청이 안 되면 건물 전원 켜기 자체가 실패한다.

#### 그런데 PE_emulator 는 왜 `NtWriteFile(0)` 까지 왔나 — `[공백]`

- 코드대로면 둘 중 하나다 (run 문서):
  - (가) 진짜 `ConsoleInitialize` 를 안 지났다 — 흉내 몸 (`Src/ntdll/BaseDllInit.cpp` 쪽) 이 대신 했다.
  - (나) 지났는데 `ConsoleAllocate` 의 `NtCreateFile` 들과 IOCTL 이 **가짜 성공** 을 돌려줬다.
- 결과서는 "(나) 라면 표준 핸들이 0 이 아니어야 한다" 고 했다 (b6 ~ b8 이 창구 열쇠 칸을 채우니까).
- run 문서는 여기에 단서를 붙였다 — 단계별로:
  1. `ConsoleCommitState` 는 상태 칸 값이 **0 이면 개업 서류에 안 쓴다** (`+0x112` · `+0x124` · `+0x136 TEST RAX,RAX`) `[정적]`.
  2. 가짜 `NtCreateFile` 이 성공만 주고 핸들 칸을 안 채우면, 상태 칸이 0 → 창구 열쇠 칸도 0 그대로일 수 있다 `[추측]`.
  3. 그래서 → "표준 핸들 0" 하나로는 (가) 와 (나) 를 못 가른다.
- PE_emulator 쪽에서 볼 것 (run 문서):
  - kernelbase `ConsoleIsConsoleApplication` (`kernelbase+0x3af188`) 값
  - `ConsoleInitialize` 가 돌려준 `AL`
  - `ConsoleAllocate +0x150` 의 `NtCreateFile` 상태와 출력 핸들

### 2.11 `ConsoleCommitState` 가 개업 서류에 쓰는 칸 — test.exe 는 창구 열쇠를 안 건드린다

`ConsoleCommitState` (rva `0xbedb0`) 가 마무리로 쓰는 것 `[정적]` (flow-writer 가 `.asm` 으로 줄을 맞춤):

| 무엇 | 줄 | 언제 |
|---|---|---|
| `params.WindowFlags` 비트 9 (`0x200`) · 10 (`0x400`) 지우기 | `+0x14a` · `+0x159` | 비트가 켜져 있으면 |
| `params.ConsoleHandle` ← 상태 `+8` (출입증) | `+0x40` | **늘** (0 이어도) |
| 전역 `ConsoleConnectionState` (`0x38`B) ← 상태 통째 | `+0x96 ~ +0xbb` | 늘 |
| `params.StandardInput` · `Output` · `Error` ← 상태 `+0x18` · `+0x20` · `+0x28` | `+0x11c` · `+0x12e` · `+0x140` | 상태 비트 칸 (`1` · `2` · `4`) 이 켜지고 값이 0 이 아닐 때만 |
| TEB `+0x108` (`CurrentLocale`) | `SetThreadLocale +0x5a` | a4 성공일 때 |
| `params.ConsoleFlags` | — | **읽기만** |

- 왜 test.exe 는 창구 열쇠를 안 건드리나 — 단계별로 (run 문서 보탬 ④):
  1. 상태 비트 칸 (`+0`) 은 `ConsoleInitialize +0x32` 에서 0 으로 시작한다.
  2. 붙기 길은 상태 `+8` (출입증) · `+0x30` · `+0x10` (옛 소개장) 만 쓴다. 비트 칸은 안 켠다.
  3. 그래서 `ConsoleCommitState +0xc3 TEST byte [상태],7` 이 0 → 창구 열쇠를 쓰는 줄을 건너뛴다 `[정적]`.
  4. `[관찰]` run44 사진: `0x68` · `0x6c` · `0x70` 이 P1a → P1b 그대로. 맞는다.
- 반대로 새 콘솔 길은 b8 끝에 상태 `+0` = 7 (세 비트 다) 이라 창구 열쇠 셋을 **새로 쓴다** `[정적]`.
- 비유: 붙기 길은 부모가 깎아 준 창구 열쇠를 그대로 쓰고, **출입증만** 새로 받는다. 새 콘솔 길은 열쇠까지 새로 깎는다.
- run68 이 "정확한 줄이 없어" 짐 지도에 못 옮긴 `WindowFlags` 줄이 `+0x14a` · `+0x159` 다 (짐 지도 7.6.8 메모 고침).

### 2.12 콘솔 API 는 길이 셋 — 새 말: IOCTL 번호 풀이 · API 번호

- **IOCTL** (다시 한 줄): 드라이버 (상담실) 에 넣는 번호 붙은 주문서 (run66 해설 2.7).
- **새 말 — IOCTL 번호 풀이**: 번호는 규칙 (`CTL_CODE`) 대로 칸이 나뉜다. 칸 값은 계산 `[정적]`, 이름은 `[추측]`.

| 번호 | 장치 종류 | 함수 | 방식 | 쓰는 곳 |
|---|---|---|---|---|
| `0x500016` | `0x50` (`FILE_DEVICE_CONSOLE`) | 5 | `METHOD_OUT_DIRECT` | 콘솔 API 전부 + a4 |
| `0x500023` | `0x50` | 8 | NEITHER | a2 |
| `0x500037` | `0x50` | 13 | NEITHER | b3 |

  - 장치 종류 `0x50` 은 `GetFileType` 이 "콘솔 장치" 로 보는 그 `0x50` 과 같은 번호다 (2.16).
- **새 말 — API 번호**: `0x500016` 주문서의 메시지 맨 앞 4바이트. "무슨 용건인지" 를 적는 칸이다 `[정적: 값]` `[추측: 이름]`.
  - 여덟 가지 일이 모두 같은 `0x500016` 을 쓰고 이 칸만 다르다. 그래서 상담실은 이 칸으로 일을 가를 것이다 `[추측]` (condrv 안은 Q149b).
  - 비유: 주문서 **양식은 하나** (`0x500016`). 맨 위 "**용건 번호**" 칸만 바꿔 쓴다.
  - 이번에 본 여덟 (run 문서 표 그대로):

| API 번호 | 쓰는 함수 | body 크기 |
|---|---|---|
| `0x01000000` | `GetConsoleOutputCP` | 8 |
| `0x01000001` | `GetConsoleMode` | 4 |
| `0x01000002` | `SetConsoleMode` | 4 |
| `0x01000005` | `ReadConsoleInternal` (← `ReadConsoleW`) | `0x14` |
| `0x01000006` | `WriteConsoleInternal` (← `WriteConsoleW`) | 8 |
| `0x01000008` | `GetConsoleLangId` (a4) | 2 |
| `0x02000004` | `SetConsoleOutputCP` | 8 |
| `0x02000007` | `GetConsoleScreenBufferInfoEx` | `0x5c` |

  - 앞자리가 `0x01` 인 무리와 `0x02` 인 무리 둘이다.
  - PDB 에 콘솔 구조체 · 열거형 이름이 하나도 없다 (`pdb_struct.py --list` 0개). 그래서 이름은 모두 `[추측: 이름]`, 칸 위치 · 크기 · 값은 `[정적]`.

### 2.13 새 말 — 설명서 (메시지 버퍼): 데이터는 포인터 너머에

- **설명서** `[추측: CD_USER_DEFINED_IO]` = `NtDeviceIoControlFile` 의 입력 버퍼로 보내는 작은 표. 데이터를 담지 않고 **데이터가 어디 있는지** 만 적는다.
- 모양 `[정적]`:

```
설명서
 +0x00  Client           8B   앱 핸들 (없으면 0)
 +0x08  입력 버퍼 개수    4B
 +0x0c  출력 버퍼 개수    4B
 +0x10  버퍼 칸 16B 씩    { 크기 4B, 빈칸 4B, 포인터 8B }   ── 입력 칸들 먼저, 출력 칸들 나중
크기 = 0x10 + 0x10 × (입력 + 출력)       예: 1·1 → 0x30, 2·1 → 0x40, 3·2 → 0x60
```

- **첫 입력 버퍼 = 메시지**: `+0` API 번호 4B · `+4` body 크기 4B · `+8` body.
- **첫 출력 버퍼 = 같은 메시지의 body 자리** (`메시지 + 8`). 상담실이 답을 **같은 자리에 덮어 쓴다**.
- 돌려주는 값 = `NtDeviceIoControlFile` 의 NTSTATUS 그대로. 메시지 안에 따로 상태 칸은 없다.

#### 예 ① — `GetConsoleMode(h, &mode)` `[정적]`

1. 메시지 = `{ API 0x01000001, body 크기 4, [모드 4B] }`.
2. 설명서 `0x30`B: Client = `h`, 입력 1 (메시지), 출력 1 (메시지 + 8, 4B).
3. `NtDeviceIoControlFile(params.ConsoleHandle, …, 0x500016, &설명서, 0x30, NULL, 0)` (`GetConsoleMode +0xdb/+0xe2`).
4. 성공이면 body `+0` → `*lpMode` (`+0x137 ~ +0x13b`).

#### 예 ② — `WriteConsoleW(h, 글자, n, …)` `[정적]` — 글자가 메시지 안에 없다

1. 메시지 body 8B: `+0` 쓴 바이트 수 (답 자리) · `+4` 유니코드 플래그 1B (`W` 는 1).
2. 입력 버퍼 **둘**: 메시지 (`0x10`B) 와 **글자 버퍼 그 자체** (`lpBuffer`, `n × 2` 바이트). 출력 버퍼 하나 (body). 설명서 `0x40`B.
3. 보내는 손잡이 = 앱이 준 `h` 그대로, Client = 0 (`WriteConsoleInternal +0xd9` · `+0xa2`).
4. 성공이면 body `+0` 바이트 수 → `*lpNumberOfCharsWritten = 바이트 / 2`.

- 비유: 상담실 주문서에 물건을 넣지 않고 **짐 목록표** 만 붙인다.
  - "1번 짐: 우리 가게 몇 번 서랍, 16바이트 (용건 카드). 2번 짐: 몇 번 서랍, 글자 묶음. 답: 1번 짐 용건 카드의 빈칸에 적어 주세요."
  - 상담실은 목록표를 보고 **가게 서랍까지 와서** 읽고, 답도 그 자리에 쓴다.
- PE_emulator 에게 `[정적]` (run 문서 질문 2 줄): nt 엔진의 `NtDeviceIoControlFile` 몸은 입력 버퍼만 보면 안 된다. **설명서 → 포인터 → 메시지** 를 한 번 더 따라가 읽고, 답을 같은 자리에 써야 한다.

### 2.14 어느 손잡이로 보내나 · 언제 syscall 없이 막나

| 길 | 함수 | 보내는 손잡이 (`FileHandle`) | 설명서 Client | syscall 없이 막는 조건 → `0xc0000008` |
|---|---|---|---|---|
| (나) | `ConsoleCallServer` (부르는 곳 30곳) · `ConsoleCallServerWithBuffers` · 인라인 `GetConsoleMode` · `GetConsoleOutputCP` · `GetConsoleLangId` | **`params.ConsoleHandle`** (내 출입증) | 앱 핸들 (`Get/SetConsoleOutputCP` · `GetConsoleLangId` 는 0) | 출입증 ≤ 0 |
| (다) | `WriteConsoleInternal` (← `WriteConsoleW`) | **앱이 준 출력 핸들** | 0 | 출입증 == 0 **또는** 앱 핸들 ≤ 0 |
| (다) | `ConsoleCallServerGeneric` (← `ReadConsoleInternal` ← `ReadConsoleW`) | **앱이 준 입력 핸들** | 0 | 앱 핸들 ≤ 0 (출입증은 안 봄) |

- 읽는 법:
  - (나) 는 "**내 출입증** 을 들고 상담실에 가서, 어느 창구 이야기인지는 Client 칸에 적는다".
  - (다) 는 "**창구 열쇠 자체** 를 들고 간다".
- 그래서 막는 조건도 다르다.
  - (나) 는 출입증이 없으면 막힌다.
  - `ReadConsoleW` 는 열쇠가 없으면 막힌다.
  - `WriteConsoleW` 는 둘 중 하나라도 없으면 막힌다.
- 비교는 **부호 있는** 비교다 (`ConsoleCallServer +0x61 JLE`). −1 같은 음수도 "없음" 으로 막힌다 `[정적]`.
- 막혔을 때 보이는 것 (표 B 대표) `[정적]`:
  - `GetConsoleMode` 류 → FALSE + `ERROR_INVALID_HANDLE` (6).
  - `GetConsoleOutputCP` → **0** 을 돌려준다 (코드 페이지 값 자리에).
  - `WriteConsoleW` → FALSE.
- 곁가지 몇 줄 `[정적]`:
  - `GetConsoleScreenBufferInfo` 는 `Ex` 를 부르고 (`cbSize 0x60` 짜리 임시), 결과 중 22B 만 옮긴다. `Ex` 에 `cbSize ≠ 0x60` 을 주면 syscall 없이 `0xc000000d`.
  - 그 답 body (`0x5c`B) 는 창 **너비 · 높이** 로 온다. kernelbase 가 `Right = Left + 너비 − 1`, `Bottom = Top + 높이 − 1` 로 바꾼다 (`+0x7f ~ +0x9f`).
  - `SetConsoleOutputCP` 가 성공하면 `SetTEBLangID` 를 다시 불러서 a4 와 같은 IOCTL 이 한 번 더 나간다.
  - `ReadConsoleW` 는 `0xc0000120` (취소) 를 받으면 오류 코드만 세우고 **TRUE** 를 돌려준다.

### 2.15 ★ `WriteFile` 에는 콘솔 갈림이 없다 — test.exe 의 `Log` 가 이 길

- 질문 2 끝 ("콘솔 핸들이면 condrv, 아니면 `NtWriteFile` 로 가르는 줄") 의 답: **그런 줄이 없다** `[정적]` (2.2 줄 3).
  - `WriteFile` 의 비교는 표준 상수 셋 (`+0x39` · `+0x42` · `+0x4b`) 과 `lpOverlapped` (`+0x63`) 뿐이다.
  - 그다음 `+0x86/+0x8d NtWriteFile` (겹침이면 `+0x113/+0x11a`). 상태가 `0x103` 이면 `NtWaitForSingleObject` (`+0x1d1/+0x1d8`).
  - `ReadFile` 도 같다 (`+0x3d` · `+0x46` · `+0x4f`, `NtReadFile`).
- 그럼 콘솔에 쓸 때 condrv 는 어떻게 끼나: `NtWriteFile` 을 받는 쪽이 `ConDrv` 장치라서 낀다. kernelbase 가 고른 게 아니다 `[정적]`. condrv 쪽 처리는 Q149b.
- 비유: 편지에 손잡이 번호만 적어 **관리사무소 우편 창구** (`NtWriteFile`) 에 넣는다.
  - 그 손잡이가 콘솔 상담실 문이면 상담실로, 창고 (파일) 문이면 창고로 간다.
  - 건물 (kernelbase) 은 봉투에 "콘솔용" 표시를 따로 안 한다.

#### test.exe 는 정확히 이 길을 탄다

- `[소스]` test.exe · dll1 · dll2 의 `Log` 는 `OutputDebugStringA(buf)` 바로 뒤에 `WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), buf, i, &written, NULL)` 을 부른다 (`sample/log.h:37 ~ 38`). CRT `printf` 는 일부러 안 쓴다 (`log.h:2`).
- 그래서 kernelbase 안에서 `[정적 + 소스]`:
  1. `GetStdHandle(-11)` — syscall 없음. 개업 서류 `+0x28` (출력 창구 열쇠) 값을 그대로 준다. test.exe 는 사진 `0x6c` `[관찰]`.
  2. `WriteFile(0x6c, …)` → `+0x86` `NtWriteFile` **한 번**.
  3. 콘솔 IOCTL (`0x500016`) 은 **없다**.
- 언제부터: dll2 TLS 콜백부터 `Log` 마다. 곧 **은퇴 앞 (flow.md 2.8) 에도** `NtWriteFile` 이 있다 (flow.md 2.8 `Log` 절 새 줄).
- PE_emulator 의 `NtWriteFile(0)` 26번 `0xc0000008` 은 이 줄이다 — 출력 창구 열쇠 칸이 0 이라서 `[정적]`.
  - 26 이 `Log` 횟수와 같은지는 안 셌다 `[추측]`.
- 그러니 test.exe 의 `Log` 길만 보면, 에뮬레이터에 필요한 건 **`params.StandardOutput` 에 쓸 수 있는 핸들 하나 + `NtWriteFile` 몸** 뿐이다 `[정적 + 소스]`.
  - `/MT` CRT 초기화가 표준 핸들에 무엇을 묻는지 (`GetFileType` 등) 는 run84.

### 2.16 `GetFileType` — 장치 종류 `0x50` 이면 "문자 장치"

- `GetFileType` (rva `0xc6290`, 259B) 은 syscall 하나 `[정적]`: `+0x72/+0x79 NtQueryVolumeInformationFile(h, &IoStatus, &out, 8, 4)`.
  - 클래스 4 = `FileFsDeviceInformation`. 답 8B 중 앞 4B = 장치 종류 (`DeviceType`).
- 답 가르기 `[정적]` (점프표 `0xc63b0` · `0xc63c4` 는 결과서가 바이너리에서 직접 읽음):

| 장치 종류 | 돌려주는 값 |
|---|---|
| `0x50` (콘솔) | **2** (`FILE_TYPE_CHAR`) — `+0x87 CMP EAX,0x50` · `+0x8c MOV EAX,2` |
| `0x11` | 3 (`FILE_TYPE_PIPE`) |
| `2 ~ 8` · `0x24` | 1 (`FILE_TYPE_DISK`) |
| `0xb` · `0xf` · `0x15` · `0x16` · `0x18` · `0x1b` · `0x1c` · `0x1d` · `0x2b` | 2 |
| 그 밖 | 0 (오류 코드 0) |
| syscall 실패 | 0 + 오류 코드 |

- **핸들 0 이면 syscall 없이** 0 + `ERROR_INVALID_HANDLE` (`+0x15/+0x18 JZ`). 표준 상수를 줬는데 그 칸이 0 이어도 같다 (`+0x55/+0x58`).
- 0 이 아닌 이상한 핸들 (−1 등) 은 kernelbase 가 안 거른다. 커널 답을 그대로 오류로 세운다.
- 비유: "이 손잡이 문 너머가 무슨 방이에요?" 하고 관리사무소에 묻는다. "콘솔 상담실 (`0x50`)" 이면 "문자 장치".
- PE_emulator 에게: 가짜 콘솔 핸들을 만들면 이 질문에 `DeviceType = 0x50` 을 줘야 "콘솔" 로 보인다.

### 2.17 표준 핸들 0 일 때 · 파일 · 파이프일 때 (질문 4 · 5)

| 함수 | 창구 열쇠 칸이 0 이면 | syscall |
|---|---|---|
| `GetStdHandle` | 0 을 그대로. 오류도 안 세운다 (−1 만 오류, `+0x66` · `+0x6a`) | 없음 |
| `WriteFile(0, …)` · `ReadFile(0, …)` | **kernelbase 가 안 막는다** → `NtWriteFile(0)` · `NtReadFile(0)` → 커널이 `0xc0000008` 이면 `+0x143/+0x148 BaseSetLastNTError` + FALSE | **간다** |
| `GetFileType(0)` · `WriteConsoleW(0)` · `ReadConsoleW(0)` | 막는다 | 없음 |
| `GetConsoleMode` 류 | 열쇠가 아니라 **출입증** (`ConsoleHandle ≤ 0`) 을 보고 막는다 | 출입증에 달림 |

- 질문 4 → PE_emulator 의 `NtWriteFile(0)` 은 **코드와 맞다** `[정적]`.
- 질문 5 → 표준 출력이 파일 · 파이프여도 **condrv 는 안 낀다**. kernelbase 쪽은 똑같이 `NtWriteFile` 하나다 `[정적]`.
  - 그때 `GetFileType` 은 1 · 3 을 주고, CRT 는 이걸 보고 콘솔 전용 API 를 안 쓸 것이다 `[추측]` (run84).

### 2.18 PE_emulator 에게 — 한 줄 정리 (run 문서 "한 줄" 을 풀어서)

- 콘솔을 흉내 낼 때 중심 syscall 은 셋 `[정적]`:
  1. `NtCreateFile` — `\Device\ConDrv\…` 이름, 또는 `RootDirectory` 기준 이름 (`\Connect` 등).
  2. `NtDeviceIoControlFile` — IOCTL `0x500016` 하나에 API 번호로 갈림. `0x500023` · `0x500037` 은 연결 때만.
  3. `NtWriteFile` · `NtReadFile`.
  - 곁가지: 연결 때 `NtSetInformationProcess(0x31)` · `NtDuplicateObject` · `NtClose`, 핸들 종류 묻기 `NtQueryVolumeInformationFile`.
- 지금 상태 (콘솔 앱 · 소개장 0) 가 타는 새 콘솔 길에서 대답해야 하는 것 (run 문서 (b) "지금 우리"):
  - 핸들을 돌려주는 `NtCreateFile` **다섯** (b1 · b2 · b5 · b6 · b7)
  - `NtDuplicateObject` **하나**
  - IOCTL **세 종류** (`0x500037` · `0x500023` · `0x500016`/`0x01000008`)
  - `NtSetInformationProcess(0x31)`
  - 조심할 것: run 문서는 이 목록에 "하나라도 실패를 돌려주면 시작 함수 FALSE" 라고 붙였다. 그런데 같은 문서 a4 줄은 "실패면 오류 코드만 세우고 넘어감" 이다. 그러니 적어도 a4 는 실패해도 멈추지 않는다. `ConsoleAllocate` 가 실패하면 FALSE 인 건 맞다 (2.10). commit 의 a2 · a3 가 실패하면 어떻게 되는지는 근거에 없다.
- test.exe 의 `Log` 만이라면 → 출력 핸들 하나 + `NtWriteFile` 몸 (2.15).
- (가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표는 run84 (Q149b).

### 2.19 짧게 — 결과서와 다르게 적은 곳 · 디버거 영향

- run 문서 "보탠 것" ① ~ ⑧ 이 이 글 어디에 있나:
  - ① call 자리 `+0x146` (2.2, 조심할 곳 4) · ② AL 0 → 시작 함수 FALSE (2.10) · ③ −1 → 소개장 0 (2.9) · ④ 붙기 길은 창구 열쇠를 안 씀 (2.11)
  - ⑤ `SetThreadLocale` 은 캐시에 있을 때만 syscall 없음 (2.7) · ⑥ `ConsoleCommitState` 쓰는 줄 (2.11) · ⑦ 매니페스트 정책 비교 (2.4) · ⑧ 0 ↔ `DETACHED_PROCESS` 짝은 `[추측]` (2.5)
- 결과서와 run 문서가 결론을 달리 적은 곳: PE_emulator 공백 (가) · (나) 를 "표준 핸들 0" 으로 가를 수 있나 (2.10).
- 디버거 영향 `[정적]`: 이번 길을 가르는 칸 (`ConsoleHandle` · `ConsoleFlags` · `WindowFlags` · `Subsystem` · KUSD 비트 6) 에 디버거를 보는 줄은 없다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — kernelbase PDB run 목록 | … run82 | + run83 (콘솔 함수 넷 depth 1 · `ConsoleCallServer*` 셋 depth 1 · API 13개 depth 0, 넓힌 것 `ReadConsoleInternal` · `GetConsoleLangId` · `SetThreadLocale`) |
| 기준 — 사진 메모 | run82 줄까지 | + run83 줄 (새 실행 없음, run44 로그에서 콘솔 칸 · TEB `CurrentLocale` · KUSD 다시 읽음). run82 줄 로그 이름을 `04_baggage-read_P2.txt:4053` 로 끝까지 적음 |
| 그림 2 `bc1` 라벨 | "syscall 3" | "syscall 4 — run83 정정" (그림은 안 늘림) |
| 2.5 제목 run 목록 | … run82 | + run83 |
| 2.5 KERNELBASE 트리 | `+0x526 ConsoleInitialize` 아래 commit 까지 | + 두 줄: `+0xff SetTEBLangID → +0x17 GetConsoleLangId → +0xe8 NtDeviceIoControlFile(0x60, 0x500016, API 0x01000008)` · `+0x526 TEST AL · +0x528 JZ → +0xf6` |
| 2.5 "콘솔" 글머리 | syscall 3번 | ~~3번~~ → **4번** + `SetThreadLocale +0x5a` 가 TEB `CurrentLocale` 을 씀 + 사진 `0x412` 메모 |
| 2.5 새 글머리 둘 | — | 갈래 전부 `[정적]` · **"PE_emulator 에 중요"**: 소개장 0 인 콘솔 앱 = 새 콘솔 길 |
| 2.8 `Log` 절 | `Log` 가 `OutputDebugStringA` 와 `WriteFile` 을 부른다 `[소스]` — kernelbase 안 `WriteFile` 길은 없음 | + `[정적]` 그 `WriteFile` 은 `GetStdHandle` (syscall 없음) → `NtWriteFile` — `Log` 마다 한 번, 은퇴 앞에도 |
| 3장 새 절 "콘솔 입출력 — kernelbase 콘솔 API" | — | 비유 + 트리 + `[정적]` 넷 (갈림 없음 · `0x500016` 하나 · 손잡이 셋 · `GetFileType`) + 동적 남은 것 |
| 4장 | run82 정적 줄에 번호 없음 | run82 줄 → **Q157**. run83 새 줄 셋 (동적 Q104 1 ~ 7 · 정적 남은 것 ① ~ ⑤ + Q149b 로 넘길 것 · PE_emulator 공백 · 정정 · 보탬) |
| 5장 | run82 까지 | + run83 줄 (표 둘) |

사실 · 태그가 바뀐 곳:

- flow.md 2.5 붙기 길 syscall 수: 3 → **4** (정정. 태그는 그대로 `[정적]`, 숫자가 바뀜).
- 콘솔 갈래 (새 콘솔 · −1 · 콘솔 없음): run68 은 "0 · −2 · −3 이면 새 콘솔" 이라는 갈림만 → 이번에 **안쪽 syscall 표까지** `[정적]`.
- PE_emulator 의 가정 "소개장 0 = 조용히 성공" (요청서) → **틀림** `[정적]`.
- PE_emulator 의 `NtWriteFile(0)` 26번 → 코드와 맞음 `[정적]`. 그 길은 `Log` → `WriteFile` `[소스]`. 26 = `Log` 횟수는 `[추측]`.
- TEB `CurrentLocale` 을 쓰는 곳 후보에 KERNELBASE `SetThreadLocale +0x5a` 가 붙음 `[정적]` (짐 지도 3.3). 사진 값은 그대로 `[관찰]`.

### 짐 지도 · run68 · run82 문서에서 바뀐 것

- 짐 지도 (`baggage.md`): **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** (짐 지도 바퀴 아님, 서른아홉째판). 이미 있는 줄에 근거만:
  - 3.2 머리 메모 (콘솔 코드가 개업 서류에서 읽고 쓰는 칸) · `ConsoleHandle` ② 줄 (넷째 syscall 이 읽음, −1 → 0) · `Standard*` ① 줄 (조건부 쓰기 두 곳, test.exe 안 탐).
  - 3.3 `CurrentLocale` 줄 — KERNELBASE `SetThreadLocale +0x5a` 쓰기 후보 (a4 성공일 때).
  - 7.2 KUSD `SharedDataFlags.DbgConsoleBrokerEnabled` — 읽는 곳 `ConsoleAllocate +0x52` (쓰는 쪽은 그대로 모름).
  - 7.6.8 `ConsoleIsConsoleApplication` 읽는 곳 · "콘솔 연결 핸들" 줄 (a4 가 씀) · `WindowFlags` 메모 (줄 확정).
- run68 문서: 맨 위 **정정 한 줄** + syscall 표 `14′` 줄 (2.7).
- run82 문서 뒷정리 (사실이 틀린 건 아님): 정적 후보 → **Q157**, 호출 관계의 `RtlUserThreadStart` 가지 밖 줄 `+0x2c` → `+0x27 · +0x3d`, 로그 이름을 끝까지.

### 큰 그림에서

- **지도 쪽**:
  - 2.5 kernelbase 전원 켜기 안 **콘솔 상자의 갈래가 다 채워졌다**. test.exe 는 그중 붙기 길 하나 (syscall 4) 를 탄다.
  - 2.8 dll 전원 켜기 구간에 **`NtWriteFile` 이 있다** 는 게 지도에 올라갔다 (`Log` 마다).
  - 진입점 뒤 콘솔 API 는 흐름도 범위 밖이라 3장 절로만 적었다.
- **PE_emulator 쪽**:
  - 지금 "소개장 0" 상태가 실은 **새 콘솔 길** 이라는 게 드러났다. 갈 수 있는 길이 분명해졌다 — 새 콘솔 길의 syscall 에 대답하기 (2.18), 또는 조용히 끝나는 세 길 (2.9). 무엇이 최소인지는 run84 표 몫이다.
  - `WriteFile` 에는 콘솔 코드가 없다. 그래서 리다이렉트든 콘솔이든 `NtWriteFile` 몸 하나로 된다. 콘솔 전용 API 는 `NtDeviceIoControlFile` 몸이 설명서를 풀어야 한다.
- **다음**: **Q149b** (run84) — CRT (ucrtbase) 가 이 API 들을 언제 부르는지, condrv.sys 가 받는 것, (가) · (나) 최소 조건 표.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 에뮬레이터의 KUSD `0x7ffe02f0` 비트 6 이 꺼져 있나 (켜지면 중개 길) | 이 VM 값 (`0x10e`) 만 봤다 | **Q104** 1 |
| 넷째 syscall 의 실제 — 손잡이가 `0x60` 인지, NTSTATUS, LangId 값, `SetThreadLocale +0x93` 이 서는지, TEB `+0x108` | 실행 때 값 | **Q104** 2 |
| 상담실이 돌려주는 값 — 모드 · 코드 페이지 · 화면 정보 body `0x5c` · LangId | 서버가 정한다. 정적으로 못 정함 | **Q104** 3 |
| test.exe `Log` 의 `NtWriteFile` 손잡이가 `0x6c` 인지, 첫 번이 dll2 TLS 콜백인지, `Log` 에선 `0x500016` 이 안 서는지 | 실행 차례 | **Q104** 4 |
| −1 · −2 · −3 의 이름 (`DETACHED_PROCESS` …), 0 ↔ `DETACHED_PROCESS` 짝 | 부모 `CreateProcessInternalW` 안 읽음 | 정적 **Q158 ①** · 동적 Q104 5 |
| `GetFileType` 이 test.exe 표준 핸들에 주는 값 (`DeviceType 0x50`?) | 실행 때 값 | **Q104** 6 |
| `ConsoleSanitizeStandardIoObjects` 안 (`ConsoleFlags & 2` · `WindowFlags & 0x100`, 상태 비트를 켜는 곳) | 이름만 봤다 | 정적 **Q158 ②** |
| `ConsoleAttachOrAllocate` 의 `"attach"` 길을 부르는 곳 | 지도 caller 2 중 하나만 봤다 | 정적 **Q158 ③** |
| `NtSetSystemInformation` 클래스 `0x84` (b1 실패 뒤) 가 무엇을 켜나 | 커널 쪽 안 봄 | 정적 **Q158 ④** |
| `SetThreadLocale +0x93 MakeLocHashNode` 아래 — 넷째 뒤 NLS syscall 이 더 있나 | run68 부터 `[공백]` | 정적 **Q158 ⑤** (Q131 (a) 와 같이 닫힘) |
| PE_emulator 가 진짜 `ConsoleInitialize` 를 지났나 · 가짜 성공을 줬나 | PE_emulator 안의 일 | PE_emulator 쪽 (`kernelbase+0x3af188` · `AL` · `ConsoleAllocate+0x150`) |
| 26 이 `Log` 횟수와 같은가 | 안 셌다 | 대기열 줄 없음 |
| condrv.sys 가 받는 것 — `IRP_MJ_CREATE` 이름 여섯 · EA 이름 셋 · IOCTL 셋 · `IRP_MJ_WRITE` / `READ` · `DeviceType 0x50` · 커널 몫 `0x84` · `0x31` | 이번은 kernelbase 만 | **Q149b** (run84) |
| CRT 가 언제 어느 API 를 부르나 — `isatty` 가 `GetFileType` 만인지, `WriteFile` 과 `WriteConsoleW` 를 언제 고르나, 표준 핸들 0 일 때, `/MT` CRT 초기화가 표준 핸들을 묻나 (`GetConsoleCP` 는 이번에 안 읽음) | ucrtbase 안 읽음 | **Q149b** |
| (가) 파일 리다이렉트 / (나) 가짜 condrv 최소 조건 표 | 위 둘을 합쳐야 나온다 | **Q149b** |

- 추천 멈춤점 (run 문서 동적 7): `kernelbase!GetConsoleLangId+0xe8` · `kernelbase!ConsoleInitialize+0x19f` (run68 Q104 5 와 같은 자리) · `kernelbase!ConsoleCommitState+0x91` · `kernelbase!ConsoleAllocate+0x150` (이 VM 에선 **안 서야** 한다). 모두 test.exe 조건.

---

## 5. 더 읽기

- run 문서: [`run83-console-kernelbase.md`](run83-console-kernelbase.md) — "PE_emulator 쪽에서 쓸 것" 질문별 표, **질문 1 갈래별 syscall 표 (a) · (b) · (c)** 와 연결 데이터 칸 표 전부, **질문 2 표 A · B · C** (API 마다 입력 · 기대 답 · 실패), 질문 3 · 4 · 5, 호출 지점 표, 호출 관계 그림, 보탠 것 ① ~ ⑧, 정정, 동적 Q104 목록
- 결과서: [`_workspace/sast/80_console-kernelbase_result.md`](../../_workspace/sast/80_console-kernelbase_result.md) — "한 줄 결론", 설명서 표 · API 번호 표, 질문별 답, "Q149b 로 넘길 것"
- 요청서: [`_workspace/sast/80_console-kernelbase.md`](../../_workspace/sast/80_console-kernelbase.md)
- flow.md: [2.5 콘솔 글머리 · KERNELBASE 트리 · 그림 2 `bc1`](flow.md) · 2.8 `Log` 절 run83 줄 · **3장 "콘솔 입출력 — kernelbase 콘솔 API"** · 4장 run83 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.2 · 3.3 `CurrentLocale` · 7.2 KUSD · 7.6.8
- run68 문서 맨 위 정정: [`run68-dll-init-rest.md`](run68-dll-init-rest.md)
- 앞 이야기:
  - [run68 해설](run68-dll-init-rest-해설.md) 2.8 — 콘솔 연결, 소개장과 출입증 (이번 2.6 · 2.7 의 출발점)
  - [run51 해설](run51-eprocess-later-해설.md) 2.9 — 옆집 (conhost), 콘솔 담당자 등록 (a2 · a3)
  - [run32 해설](../02_sprint/run32-params-initblock-해설.md) 2.3 · 2.7 — 개업 서류, 창구 열쇠 (표준 핸들)
  - [run66 해설](run66-apphelp-cache-해설.md) 2.7 — 드라이버 = 상담실, IOCTL = 주문서
  - [run71 해설](run71-named-obj-dir-해설.md) 2.1 — `Log` → `OutputDebugStringA` (이번 2.15 는 그 다음 줄 `WriteFile`)
  - [run44 문서](run44-baggage-snap.md) — 소개장 4 (`\Reference {ConDrv}`) → `0x60` (`\Connect {ConDrv}`) 사진
  - [run70 해설](run70-cpu-features-해설.md) — 공용 게시판 (KUSD)

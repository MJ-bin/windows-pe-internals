# run131 user32-regclass — 해설

> **고쳐 읽기 (run146)**: 2.10 · 4절 동적 3 의 `NtUserGetAtomName` "반환 = 글자 수, 끝 NUL 은 `[추측]`" — **정수 원자 (`0xc000` 아래) 는 `[정적]`** 으로 닫혔다. ntoskrnl `RtlQueryAtomInAtomTable` 이 원자 표를 안 찾고 `L"#%u"` 로 지어 (`+0x75` · `+0x298`) 끝 NUL 을 쓰되 세지 않고 (`+0x29f` · `+0x2c5`), win32kbase `UserGetAtomName +0x60` 이 바이트 ÷ 2 를 돌려준다. 커널은 `Length` 를 안 쓴다 (user32 `+0x232 / +0x235` 가 씀). `0x8002` → 6 · `#32770`. 표 원자 (`OleMainThreadWndClass` 같은 `0xc000` 이상) 의 진짜 글은 그대로 동적 3 (Q242). 본문은 그대로 두었다 → [run146 해설](run146-dialog-window-해설.md) 2.3

> 이 글은 [run131 문서](run131-user32-regclass.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q240** — PE_emulator 요청 "COM 초기화 (STA) 가 창 클래스를 등록할 때 user32 · combase 가 커널 답에서 무엇을 읽나" (`298_internals-user32-regclass.md`, 튜닝 `298_tune10.md` U4a, 샘플 135) (`_workspace/plan.md`, 이번에 **끝 (run131)**)
  - 사용자 결정 2026-11-22 "이 세 요청 먼저 가장 우선".
  - 결과서 판정 **분석성공**. 요청서 물음 1 · 2 · 3 모두 줄 근거로 답했다.
  - 덤: 원 요청의 물음 4 (`NtUserGetAtomName`) 는 닫았고, 물음 5 (`NtUserCreateWindowEx`) 는 사슬 · 진입 줄만 봤다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q242** (STA COM 을 여는 새 프로그램이 필요 — 고르기는 사용자 결정)
    - 정적 → **Q243** (`NtUserCreateWindowEx` 본격 등)
  - 같이 일부 닫힌 것: run86 이 남긴 "창 만들기" (4장 run86 정적 ④, Q163 ④) 의 **클래스 등록 몫만**.
- 근거:
  - run 문서 [`run131-user32-regclass.md`](run131-user32-regclass.md) (가장 중요)
  - 요청서 [`_workspace/sast/128_user32-regclass.md`](../../_workspace/sast/128_user32-regclass.md) · 결과서 [`128_user32-regclass_result.md`](../../_workspace/sast/128_user32-regclass_result.md)
    - 처음 analyze 는 **combase 하나** (함수 16,607, `FUN_` 632, MAXMEM 10G · 약 2분 반). user32 · win32u 는 이미 있던 것에서 `^이름$` export 만.
    - 스크립트 문제 없음. Ghidra `.c` 가 틀어 보인 곳은 `.asm` 으로 읽었다.
  - flow.md 변경: docs 커밋 `0a29d76` (`git show 0a29d76 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 넷

1. **test.exe 는 user32 · combase 를 안 올린다.**
   - 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) 에 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run129 와 같은 줄).
   - `sample/*.c` 에 COM · 창 API 가 없다 `[소스]` (`sample/log.h:3` "user32(wsprintf)도 안 쓴다").
   - 그래서 이번 이야기는 "**COM 을 STA 로 여는 다른 프로그램이라면**" 이다. 그림 · 짐 지도는 그대로다.
2. **"요청 쪽 로그" 는 진짜 Windows 로그가 아니다.**
   - seq 9941 ~ 10123, 스택 주소 `0x7fbff…`, `0x7ffffcf20000` 은 모두 **PE_emulator 가 흉내 낸 실행** 의 기록이다. `[관찰]` 이 아니다.
   - 다만 그 로그의 **스택 주소 여섯 개** 가 `.asm` 의 스택 칸 셈과 전부 맞았다 (run 문서 "환경"). 에뮬레이터가 진짜 user32 코드를 돌린 것이니, "오프셋 셈 · 바이너리를 믿어도 된다" 는 근거다. 진짜 Windows `[관찰]` 은 여전히 아니다.
3. **오프셋 세 가지.** run 문서와 같은 규칙이다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 "`+0x61 RegisterClassW`" 처럼 함수 이름 앞 숫자 = **call 자리**.
   - 그냥 "`+0x242 MOVZX EDI,AX`" = 함수 안 **줄**.
   - 콜스택으로 본 적은 없다.
4. **`RegisterOleWndClass` 의 오프셋이 이상하게 크다** (`+0x1397be` 같은 것).
   - 이 함수 몸이 **두 덩어리로 나뉘어** 있어서다 — `+0x0 ~ +0x92` 와 `+0x1397b4 ~ +0x139865` (결과서 "읽을 때 주의").
   - 실패 쪽 처리 (②③) 가 멀리 떨어진 둘째 덩어리에 있다. 오프셋이 크다고 다른 함수가 아니다.
   - WinDbg 에서도 `RegisterOleWndClass+0x1397..` 로 보일지는 `[추측]` (확인 전).

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- user32 = **창 담당 건물**, win32u = **옆 건물 단축번호 버튼판**, `SHAREDINFO` (`gSharedInfo`) = **공용 안내 묶음**, `psi` (`gpsi`) = 묶음 첫 줄 **화살표** → [run85 해설](run85-user32-init-해설.md) 2.1 · 2.5
- win32k = 관리사무소의 **그래픽 부서**, 번호 `0x1000` 이상 = **옆 건물 번호부** → [run51 해설](run51-eprocess-later-해설.md) 2.9 · [run56 해설](run56-syscall-args-해설.md) 2.9
- `NtGdiInit2` 임시 stub 을 "호출 쪽이 읽는 자리" 에 맞춘 선례 → [run86 해설](run86-gdi32-init-해설.md) 2.9
- 지연 가져오기 (delay import) = "**필요하면 그때 부를게요**" 메모 → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 2.5
- api-set = **별명**, PEB `ApiSetMap` = **별명 사전** → [run37 해설](../02_sprint/run37-k32-import-order-해설.md) · [run57 해설](run57-nls-apiset-해설.md)
- 활성화 컨텍스트 = 매니페스트 설정 묶음 → [run22 해설](../02_sprint/run22-map-snap-dep-해설.md)
- HRESULT = COM 쪽 32비트 결과 코드, "음수면 실패" → [run89 해설](run89-dotnet-start-해설.md) ("새 말 — HRESULT")
- TEB `LastErrorValue` (마지막 오류 칸) → [run54 해설](run54-teb-fill-해설.md)
- 프로세스 힙 = 가게 **창고** → [run46 해설](run46-process-heap-해설.md)

이번에 처음 나오는 말 — **COM · combase**, **창 클래스**, **ATOM (아톰)**, **STA · 클래식 STA**, **기본 클래스**, **`CLSMENUNAME`**, **메시지 전용 창 (`HWND_MESSAGE`)**, **`E_OUTOFMEMORY` (`0x8007000e`)** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 프로세스 힙 = **창고**.
- user32 = **창 담당 건물**, win32u = **옆 건물 단축번호 버튼판**, win32k = 관리사무소의 **그래픽 부서**.
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 따름 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - combase = 가게에 들인 **COM 담당 건물**.
  - 창 클래스 등록 = 그래픽 부서에 "**우리 가게 창 설계도 이름**" 을 올리는 것. 그래픽 부서는 **번호표 (ATOM) 한 장** 만 돌려준다.
  - user32 는 번호표에 숫자가 있나만 본다. 다른 서류는 안 받는다.
  - combase 는 번호표를 못 받으면 **지우고 한 번 더** 신청한다. 또 못 받으면 "왜 안 됐어요?" (`GetLastError`) 를 묻는다. 대답이 없으면 (0) "**메모리가 모자랐나 보다**" 로 적는다.
  - 지울 때 그래픽 부서가 "**돌려줄 서류 봉투**" (24 B) 에 뭘 넣어 주면 user32 는 그걸 버리러 간다. **빈 봉투** 면 그냥 지나간다.
- run 문서 · flow.md 는 커널을 "**동사무소**" 라고 불렀다. 이 글은 시리즈대로 **관리사무소 (그래픽 부서)** 로 쓴다. 가리키는 건 같다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

test.exe 흐름 밖, **옆길** 이다. "user32 · combase 를 올리고 COM 을 STA 로 여는 프로그램이라면" 의 이야기다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         └─ (2.7) dll 전원 켜기 — LdrpCallInitRoutine
 │               ├─ (test.exe) dll2 → dll1
 │               └─ (user32 를 올리는 프로세스라면) user32 DllMain   ← run85 · run86
 │
 │  … 그 DllMain 뒤 어느 때, 프로그램이 CoInitializeEx(STA) 를 부르면
 │        └─ combase → user32 → 창 클래스 등록 → 숨은 창 만들기    ← 이번 (run131)
 │
 └─ … → NtContinue (은퇴) → exe 진입점 → main
```

- flow.md 3장 새 절의 말: "위 'user32 시작 함수' 절 (run85) 의 `DllMain` 뒤, COM 을 STA 로 여는 프로세스가 처음 하는 창 일이다."
- `CoInitializeEx` 를 **누가 · 은퇴 앞인지 뒤인지** 는 이번 근거에 없다. 그래서 그림에 "어느 때" 로만 둔다.

### 왜 이 질문이 지금 왔나

- PE_emulator 에서 샘플 **135 개** 가 똑같은 꼴로 죽는다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님):
  1. `NtUserRegisterWindowMessage` (기본 대답, seq 9941)
  2. `NtUserRegisterClassExWOW` → 0
  3. `NtUserUnregisterClass(…, 0x7ffffcf20000, …)` → 0
  4. `NtUserRegisterClassExWOW` 다시 → 0 (2 ~ 4 는 seq 10019 ~ 10021, 셋 다 기본 대답 0)
  5. .NET `OutOfMemoryException` (10063) → 하드 에러 (10123) → 종료 `0xe0434352`
- `0x7ffffcf20000` 은 combase 가 올라간 주소다. 그래서 요청 쪽은 "부르는 쪽은 combase `[추측]`" 이라고 봤다.
- 요청 쪽이 알고 싶은 건 하나로 모인다:
  - 에뮬레이터의 임시 stub (진짜 커널 대신 에뮬레이터가 내놓는 가짜 대답) 이 **값 하나만 돌려줘도 되나**,
  - 아니면 **출력 인자 · 공유 메모리까지 써 줘야 하나**.
  - 진짜 클래스 번호 값은 필요 없다.

### 무엇이 궁금했나 — 그리고 새 말 넷

질문을 읽으려면 말 넷이 먼저 필요하다.

#### 새 말 — COM · combase

- COM 은 Windows 의 "부품 객체" 규칙이고, 그 런타임 본체가 `combase.dll` 이다 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거에서 확인된 것: combase 의 `CoInitializeEx` 가 이번 길의 입구다 `[정적]`.
- 비유: 가게에 들인 **COM 담당 건물**.

#### 새 말 — 창 클래스

- 창을 만들기 전에 "이런 창이 있다" 고 먼저 올려 두는 **창 설계도 이름** 이다. 설계도엔 창 함수 (메시지가 오면 부를 함수) · 배경 붓 · 메뉴 이름 등이 적힌다 `[추측: 일반적으로 알려진 설명]`.
- 이번에 등록하는 설계도 이름은 `L"OleMainThreadWndClass"` (`.rdata` `0x1802cd830`) `[정적]`.
- 비유: 그래픽 부서에 올리는 "**우리 가게 창 설계도 이름**".

#### 새 말 — ATOM (아톰)

- 문자열 이름 대신 쓰는 **16비트 번호** 다. 창 클래스를 등록하면 커널이 이 번호를 돌려준다 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거에서 확인된 것: user32 는 돌아온 값을 **16비트 (AX)** 로만 잘라 본다 (`RegisterClassExWOWW +0x242 MOVZX EDI,AX`) `[정적]`.
- "정수 아톰 이름" = 클래스 이름 자리에 문자열 대신 이 번호를 넣은 것. user32 는 포인터의 **위쪽 48비트가 0** 이면 아톰으로 본다 (결과서) `[정적]`.
- 비유: **번호표**.

#### 새 말 — STA · 클래식 STA

- STA (단일 스레드 아파트) = COM 객체가 **한 스레드에서만** 부름을 받는 방식. 다른 스레드에서 온 부름은 창 메시지로 줄 세워 그 스레드에 넘긴다 — 그래서 STA 에는 **숨은 창** 이 필요하다 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거에서 확인된 것:
  - `CoInitializeEx +0x33 TEST DIL,0xa` 로 STA 인지 가른다 `[정적]`.
  - `ThreadFirstInitialize +0x278 TEST R14D,0x60000000` 이 0 이면 코드가 "**클래식 STA**" 로 친다 `[정적]`. 그 비트 둘의 이름은 근거에 없어서 이 글도 쓰지 않는다.
- 비유: 한 직원 전용 **상담실**. 다른 직원이 상담을 원하면 쪽지를 상담실 우편함에 넣고 기다린다.

#### 그래서 — 요청서 질문 셋

1. **user32 등록 길** — `RegisterClassW` / `RegisterClassExW` → … → `NtUserRegisterClassExWOW`.
   - 인자 개수 · 뜻, 돌아온 ATOM 으로 하는 일, **성공 뒤 더 읽는 칸** (출력 인자 · `gSharedInfo` · `gpsi`) 이 있나.
   - → 한 줄 답: "stub 은 0 아닌 ATOM 하나만 돌려주면 되나".
2. **user32 지움 길** — `UnregisterClassW` → `NtUserUnregisterClass`. 셋째 인자 (출력) 모양, 참 / 거짓일 때 하는 일.
3. **combase** — "등록 → 지움 → 등록" 조건, 둘째도 실패하면 돌려주는 HRESULT, 성공 뒤 부르는 `CreateWindowExW` 인자와 치명 여부, `RegisterWindowMessageW` 가 0 이면 치명인가, 클래스 이름.

### 바퀴 전에 알던 것

- `[정적]` (run85) user32 `DllMain` 줄기와 `gSharedInfo` · `gpsi` 가 어디서 오나. 창 만들기 쪽은 안 봤다.
- `[정적]` (run86) user32 · gdi32 초기화 최소 길. "다음 요청 후보 4" 로 **창 만들기** (`RegisterClassExW` · `CreateWindowExW` · 메시지 루프가 읽는 `gSharedInfo` 칸) 를 남겼다 (Q163 ④).
- `[정적]` (run85) win32u 번호표 `ms/disassemble/win32u/_syscalls.tsv`. 이번 길 번호는 거기 이미 있었다: `NtUserRegisterClassExWOW` `0x10a2` · `NtUserUnregisterClass` `0x10ad` · `NtUserRegisterWindowMessage` `0x1032` · `NtUserCreateWindowEx` `0x106f`.
- combase 는 **한 번도 analyze 한 적 없다.** 그래서 "부르는 쪽이 combase" 는 요청 쪽의 `[추측]` 뿐이었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 입구에서 숨은 창까지 `[정적]`

run 문서 "호출 관계" 를 줄였다 (오프셋은 ret 자리).

```
combase!CoInitializeEx (0x309e0)
 ├─ +0xae   API 세트 있나 (0x179074)        없으면 +0xb2 0x80004001
 └─ +0x49   _CoInitializeEx
            └─ +0x354  ThreadFirstInitialize (0x31518)
                        ├─ +0x309  RegisterOleWndClass (0x12e52c)   ← 프로세스 첫 클래식 STA 일 때만
                        │           ├─ ① +0x68      RegisterClassW ⇢ user32!RegisterClassW
                        │           │                 └─ +0x6f RegisterClassExWOWW
                        │           │                      └─ +0x23d NtUserRegisterClassExWOW  ── SYSCALL [0x10a2] 인자 7, AX 만 봄
                        │           ├─ ② +0x1397c5  UnregisterClassW   (① 이 0 일 때, 결과 안 봄)
                        │           │                 └─ +0xd7 NtUserUnregisterClass        ── SYSCALL [0x10ad] 셋째 = 24 B 출력
                        │           ├─ ③ +0x1397d6  RegisterClassW     (같은 길)
                        │           └─    +0x1397f5  GetLastError → 0 이면 0x8007000e
                        ├─ +0x2c3  InitMainThreadWnd (0x12f250)     ← 등록 성공 · gdwMainThreadId == 0
                        │           └─ ④ +0x57  CreateWindowExW(…, HWND_MESSAGE, …)
                        │                        └─ … VerNtUserCreateWindowEx +0x236 NtUserCreateWindowEx  [0x106f] 인자 17
                        └─ +0x2d7  UnRegisterOleWndClass             ← ④ 실패 뒤 g_cClassicSTAInits 가 0 이면
```

- `⇢` = 지연 가져오기 발판 (`DelayLoad_RegisterClassW`, rva `0x1792a9`) → 공용 꼬리 (rva `0x1790c2`). 그 안의 로딩 코드는 안 읽었다 `[공백]`.
- 커널 쪽 (win32kfull) 본문은 이번 범위가 아니다. 이름만.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `└─ +0x23d NtUserRegisterClassExWOW  ── SYSCALL [0x10a2] 인자 7, AX 만 봄`

- **누가**: user32 의 도우미 `RegisterClassExWOWW` (rva `0x1c99c`) 가
- **어디서**: 함수 안 `+0x236` 에서 부르고, 돌아오는 자리는 `+0x23d` (나무 숫자)
- **누구를**: win32u 스텁 `NtUserRegisterClassExWOW` — 번호 `0x10a2` 로 커널 그래픽 부서에 들어간다
- **무엇을 위해**: 창 클래스를 커널에 올리고 번호표 (ATOM) 를 받으려고
- "AX 만 봄" = 돌아온 64비트 값 중 **아래 16비트** 만 쓴다는 뜻 (2.4).

#### 줄 2: `├─ ② +0x1397c5 UnregisterClassW (① 이 0 일 때, 결과 안 봄)`

- **누가**: combase `RegisterOleWndClass` 가
- **어디서**: 둘째 덩어리 `+0x1397be` 에서 부르고, `+0x1397c5` 로 돌아온다
- **누구를**: user32 `UnregisterClassW(L"OleMainThreadWndClass", g_hinst)`
- **무엇을 위해**: 첫 등록 ① 이 0 이었을 때, 같은 이름을 지우고 다시 등록 (③) 하려고
- "결과 안 봄" = 돌아온 값을 검사하는 줄이 **아예 없다** (2.5).
- 여기서 `g_hinst` 가 요청 쪽 로그의 둘째 인자 `0x7ffffcf20000` 이다 (2.11).

### 2.3 combase 는 언제 등록하나 — "프로세스 첫 클래식 STA" 한 번 `[정적]`

차례대로 (run 문서 3절).

1. **STA 인지, 창 함수가 있는지 먼저 본다.**
   - `CoInitializeEx +0x33 TEST DIL,0xa` → STA 면 `+0xa9` 에서 "API 세트가 있나" 묻는 함수 (rva `0x179074`) 를 부른다.
   - 이 함수는 `ApiSetQueryApiSetPresence(L"ext-ms-win-rtcore-ntuser-window-ext-l1-1-0")` 를 한 번 부르고 (`+0x25`), 결과를 캐시 칸에 적는다 (1 = 있음, 2 = 없음).
   - 없으면 `+0xb2 MOV EBX,0x80004001` (E_NOTIMPL) 로 실패.
   - 비유: "이 동네에 **창 담당 건물** 이 있나요?" 를 별명 사전에 먼저 물어본다.
   - 참고: 이 주소 (`0x179074`) 엔 PDB 이름이 **셋** 붙어 있다 (`IsCreateWindowExWPresent` · `IsIsGUIThreadPresent` · `IsInSendMessageExPresent`, 별칭 무리). 그래서 run 문서는 이름 하나를 고르지 않고 "API 세트가 있나 묻는 함수" 라고 쓴다. 이 글도 그대로 쓴다.
2. **`ThreadFirstInitialize` 의 STA 갈래.**
   - `+0x22b HeapAlloc(g_hHeap, 0, 0x1b0)` (실패 → `0x8007000e`) → `CComApartment` · `FinalConstruct` · `CreateAndFreezeDefaultObjectContext`.
3. **클래식 STA 면 횟수를 센다.**
   - `+0x281` 에서 `g_cClassicSTAInits` (`0x180338008`) 를 1 올린다.
   - **올린 값이 1 이면** (`+0x28f CMP EAX,EDI` · `+0x291 JZ`) — 곧 이 프로세스에서 **첫** 클래식 STA 면 — `+0x304 RegisterOleWndClass`.
4. **등록이 성공하면** (`+0x30d JNS`) `+0x293` 에서 `gdwMainThreadId` (`0x180338864`) 를 본다. 0 이면 `+0x2be InitMainThreadWnd` (2.8).

그래서:

- 창 클래스 등록은 **프로세스에 한 번** 이다. 두 번째 STA 스레드부터는 횟수만 올라가고 등록은 안 한다.
- combase 에서 `RegisterClassW` 가져오기 칸 (`0x3635c8`) 을 읽는 곳은 `RegisterOleWndClass` **하나뿐** 이다 (`refs`). 이름이 비슷한 `NdrpRegisterClass` 류는 창 클래스와 무관하다.

비유: COM 담당 건물이 **첫 상담실을 열 때만** 그래픽 부서에 설계도 이름을 올린다. 상담실이 더 생기면 "상담실 몇 개" 숫자만 고친다.

### 2.4 ★ user32 등록 길 — 인자 7개 주고, 번호표만 받는다 `[정적]`

#### 들어가는 길

- combase 는 `RegisterClassExW` 가 아니라 **`RegisterClassW`** 를 부른다 (요청 쪽 짐작과 다름).
- 하지만 둘 다 같은 도우미 `RegisterClassExWOWW` 로 가고, 같은 syscall 로 끝난다:
  - `RegisterClassW +0x6a` → `RegisterClassExWOWW`
  - `RegisterClassExW +0x1a` → `RegisterClassExWOWW` (`cbSize ≠ 0x50` 이면 `+0x2a SetLastError(0x57)`)
- combase 가 넘기는 설계도 (`WNDCLASSW`): style 0 · 창 함수 `OleMainThreadWndProc` (rva `0x11f200`) · `cbWndExtra 0x14` · `hInstance = g_hinst` · `hbrBackground = 2` · 메뉴 0 · 이름 `L"OleMainThreadWndClass"`.

#### 새 말 — 기본 클래스

- user32 가 **스스로** 등록해 두는 클래스들이다. 도우미는 부르기 전에 "혹시 기본 클래스 이름인가?" 를 먼저 본다 (`+0xb8 RegisterDefaultClass`).
- 이 함수는 잠금 (`gcsClassRegister`) 안에서 **기본 클래스 17개 이름** 과 견줄 뿐이다. `OleMainThreadWndClass` 는 그 이름이 아니라 커널을 안 부르고 1 을 돌려준다 `[정적]`.

#### 새 말 — `CLSMENUNAME`

- 메뉴 이름을 담는 **24 B 묶음**: {ANSI 메뉴 이름, 유니코드 메뉴 이름, `UNICODE_STRING*`} `[정적]`.
- 등록 때는 user32 의 `InitClsMenuNameW` (`+0x143`) 가 채워서 **넘기는 쪽 (입력)** 이다. 커널이 여기 쓴다는 줄은 없다.
- 비유: 설계도에 붙이는 "**메뉴 이름 쪽지**".

#### 인자 7개 (run 문서 표 1 줄임)

| # | 뜻 | 입력/출력 |
|---|---|---|
| 1 | `WNDCLASSEXW` 복사본 0x50 B (user32 스택 `RBP-0x40`) | 입력 |
| 2 | 준 클래스 이름 (`UNICODE_STRING`, 아톰이면 `Length 0 · Buffer = 아톰`) | 입력 |
| 3 | 버전 이름 (`ClassNameToVersion` 결과 — 매니페스트 바꿔치기가 없으면 2 와 같은 글자) | 입력 |
| 4 | `CLSMENUNAME` 24 B (`RBP-0x80`) | **입력** |
| 5 | WORD (0) | 입력 |
| 6 | 플래그 `0x80`, 모듈 `ExpWinVer > 0x30a` 면 `\| 0x40` → 보통 `0xC0`. 이름 `[공백]` | 입력 |
| 7 | 0 (WOW 칸 `[추측]`) | 입력 |

- **출력 인자는 하나도 없다.**

#### 돌아온 뒤 — 딱 한 가지

한 걸음씩 (run 문서 1절).

1. `+0x242 MOVZX EDI,AX` — 돌아온 값에서 **16비트만** 꺼낸다.
2. `+0x255 RtlGetExpWinVer` — 모듈 머리의 Subsystem 버전을 본다. 아주 옛 모듈 (`< 0x30a`) 이면 값을 0 / 1 로 줄인다. combase 는 해당 없음.
3. `+0x265 TEST RDI,RDI`
   - 0 아니면 → 그 ATOM 을 돌려준다. 끝.
   - 0 이면 → `+0x268 JZ` → 메뉴 이름 쪽지 (힙에 있으면) 해제 → 0 을 돌려준다.

그리고 **안 하는 것**:

- 성공 뒤 `gSharedInfo` · `gpsi` · 클래스 캐시를 **안 읽는다.** `RegisterClassExWOWW` 가 읽는 전역은 `__security_cookie` · `gdwRegisteredClasses(Mask)` · `hmodUser` · `pUserHeap` 뿐 (`.asm` 전역 주석).
- 0 을 받아도 **`SetLastError` 를 안 쓴다.** 오류 칸은 커널이 쓴 TEB 값 그대로다.
  - user32 가 직접 쓰는 오류는 `0x57` 하나 (`+0x32b`) — 커널 부르기 **전** 에 거르는 다섯 경우 (기본 클래스 등록 실패 · extra 음수 · user32 자신의 클래스 · 안 쓰는 스타일 비트 · 잘못된 브러시) 뿐이다.

#### `gpsi` 를 읽는 줄은 어디? — flow-writer 대조

- 결과서는 "`+0x21e MOV RAX,[gpsi]`" 를 `RegisterClassExWOWW` 쪽으로 적었다.
- flow-writer 가 `.asm` 을 다시 보니 그 줄은 **`RegisterDefaultClass +0x21e`** 다. `RegisterClassExWOWW.asm` 엔 `gpsi` 줄이 없다.
- 이 줄은 이름이 **정수 아톰일 때만** 탄다 (`gpsi + 0x364` 아톰 표와 견줌). combase 는 문자열 이름이라 안 탄다.
- 뜻은 같다: **combase 길에서 `gpsi` 는 안 읽힌다.**

비유: 창 담당 건물은 그래픽 부서 창구에 설계도와 쪽지를 내고, 받은 **번호표에 숫자가 있나만** 본다. 번호표가 비어 있어도 "왜요?" 를 따로 적어 두지 않는다 — 그래픽 부서가 적어 둔 게 있으면 그게 남을 뿐이다.

### 2.5 등록 → 지움 → 등록 — 조건은 "첫 등록이 0" 하나 `[정적]`

`RegisterOleWndClass` 의 차례 (run 문서 차례 표).

| 차례 | 부르는 것 | 실패 때 |
|---|---|---|
| ① | `RegisterClassW(&설계도)` (`+0x61`) | AX = 0 → ② 로 (이유 안 봄) |
| ② | `UnregisterClassW(L"OleMainThreadWndClass", g_hinst)` (`+0x1397be`) | **결과 검사 없음** |
| ③ | `RegisterClassW(&같은 설계도)` (`+0x1397cf`) | AX = 0 → `GetLastError` (2.7) |

- ① 이 0 아니면 `+0x6d MOVZX EAX,AX` 로 넓혀 **`gOleWindowClass`** (`0x180338870`) 에 담는다. `+0x77 TEST RAX,RAX` · `+0x7a JZ +0x1397b4` 가 "0 이면 둘째 덩어리로" 갈림길이다.
- ③ 이 0 아니면 `+0x1397e8 JNZ` → **S_OK**.
- ② 는 돌아온 값을 **아예 안 본다.** 지움이 성공했든 실패했든 ③ 으로 간다.

#### 왜 지우고 다시 하나 — 코드는 이유를 안 말한다

- 코드에 있는 건 "① 이 0 이면 지우고 다시" 라는 **순서** 뿐이다 `[정적]`.
- "같은 이름이 이미 남아 있어서 실패했을 수 있으니 치우고 다시" 라는 **까닭** 은 코드 줄이 아니다 `[추측]` — run 문서 비유 ("혹시 예전 이름이 남았나") 도 `[추측: 비유]` 로 둔다.
- ① 이 왜 실패했는지 (`GetLastError`) 는 이 자리에서 묻지도 않는다 `[정적]`.

### 2.6 ★ 지움 길 — 빈 봉투를 미리 깔아 두고, "정확히 1" 일 때만 치운다 `[정적]`

#### 인자 3개

| # | 뜻 | 입력/출력 |
|---|---|---|
| 1 | 지울 클래스 이름 (`ClassNameToVersion` 결과, `UNICODE_STRING`) | 입력 |
| 2 | `hInstance` (combase 는 `g_hinst`) | 입력 |
| 3 | **24 B** (`RSP+0x58 ~ +0x6f`) — 부르기 **전** 에 0 으로 채움 (`+0x4e` · `+0x56` · `+0x7d`) | **출력** |

- 셋째는 등록 때의 `CLSMENUNAME` 과 **같은 모양** (24 B) 이다. 다만 이번엔 커널이 **채워 주는 쪽 (출력)** 이다.
- 비유: 창 담당 건물이 그래픽 부서에 "**돌려줄 서류 봉투**" 를 빈 채로 내민다.

#### 돌아온 뒤 — 한 걸음씩

1. `+0xdc MOV EBX,EAX` — 돌아온 값을 잡아 둔다.
2. `+0xde MOV EAX,1` · `+0xe3 CMP RBX,RAX` — **정확히 1** 인가? (결과서는 이걸 "`CMP RBX,1`" 한 줄로 적었다. `.asm` 으론 두 줄 — 뜻은 같다.)
3. 1 이면:
   - `[RSP+0x58]` 이 힙 포인터 (`& 0xffffffffffff0000 ≠ 0`) 면 `RtlFreeHeap(pUserHeap, …)` (`+0xfb`).
   - `[RSP+0x60]` 도 같은 검사 → `RtlFreeHeap` (`+0x11a`).
   - `[RSP+0x68]` 은 안 읽는다.
4. 1 이 아니면 (`+0xe6 JNZ`) **아무것도 안 한다.** `SetLastError` 도 없다.
5. 반환은 `+0x13c MOV EAX,EBX` — **커널 값 그대로**. 0 / 1 로 고치지 않는다.

그래서:

- 봉투가 **0 그대로** 면 3 의 해제 둘 다 건너뛴다. 아무 일도 안 일어난다.
- 봉투에 **포인터를 넣으면** user32 가 그걸 자기 힙에서 해제하려 든다. → stub 이 봉투를 채우는 쪽이 오히려 위험하다.
- 그 밖 (결과서): 이름 풀기가 실패하면 커널을 안 부르고 0 (`+0x8f`). `g_systemCallFilterId == 5` 인 프로세스는 커널을 안 부르고 1 (`+0x28` · `+0x32`, 그 뜻은 `[공백]`).

비유: 그래픽 부서가 "다 지웠어요 (1)" 하고 봉투에 서류를 넣어 주면, 창 담당 건물은 그 서류를 창고에 **버리러 간다.** 빈 봉투면 그냥 지나간다. 그래서 "1 + 빈 봉투" 나 "0" 이나 둘 다 안전하다.

### 2.7 ★ 둘째 등록도 0 이면 — "메모리 부족" 이 만들어지는 길 `[정적]`

#### 새 말 — `E_OUTOFMEMORY` (`0x8007000e`)

- HRESULT 중 하나로, "메모리가 모자람" 을 뜻한다. Win32 오류 14 를 HRESULT 꼴로 바꾼 값 (`HRESULT_FROM_WIN32(14)`) 이다 `[추측: 지식]` (run 문서 "확인된 것").
- 비유: 서류에 찍히는 "**메모리 부족**" 도장.

#### 계산 — `RegisterOleWndClass +0x1397ee ~ +0x139810`

③ 이 0 이면 `GetLastError()` 를 물어보고 (`+0x1397ee`), 그 값 `e` 로 HRESULT 를 만든다.

| `GetLastError()` 값 | 만드는 HRESULT |
|---|---|
| 0 | **`0x8007000e`** (`+0x13980b MOV EAX,0x8007000e`) |
| 양수 | `0x80070000 \| (e & 0xffff)` (`+0x139803 OR`) |
| 음수 | 그대로 (`+0x139810 CMOVNS`) |

- 이 값은 `RegisterOleWndClass` → `ThreadFirstInitialize` → `_CoInitializeEx` → **`CoInitializeEx` 반환** 까지 그대로 간다.
- combase 안에서 프로세스를 죽이진 않는다. 실패 HRESULT 를 부른 쪽에 돌려줄 뿐이다.

#### 요청 쪽 장면과 이어 보기 — 한 걸음씩

1. 에뮬레이터 stub 은 `NtUserRegisterClassExWOW` 에 0 을 돌려주고 **오류 칸 (TEB `LastErrorValue`) 은 안 쓴다** (요청 쪽 로그 — 우리 `[관찰]` 아님).
2. user32 도 0 을 받아도 오류 칸을 안 건드린다 (2.4) `[정적]`.
3. 그래서 combase 가 `GetLastError()` 를 물으면 — 그 칸이 0 이었다면 — **0** 을 받는다.
4. 0 → `0x8007000e` `[정적]`.
5. 요청 쪽 로그는 그 뒤에 .NET `OutOfMemoryException` 이 뜬다. "메모리 부족" HRESULT → "메모리 부족 예외" 로 **잘 맞는다.**
   - 다만 CLR 이 이 값을 그 예외로 옮기는 줄은 이 레포에 clr 이 없어서 `[공백]` 이다.

그래서 메모리가 정말 모자랐던 게 아니다. **"왜 안 됐어요?" 에 대답 (오류값) 이 없어서** combase 가 기본으로 "메모리 부족" 도장을 찍은 것이다.

- 실패를 일부러 흉내 낼 때 주의 (run 문서 PE_emulator 표): 등록을 0 으로 돌리려면 TEB `LastErrorValue` (`+0x68`) 를 써 줘야 한다. 안 쓰면 늘 `0x8007000e` 가 된다.

### 2.8 성공하면 — 숨은 창 하나 (메시지 전용 창) `[정적]`

등록이 성공하고 `gdwMainThreadId == 0` 이면 `InitMainThreadWnd` (rva `0x12f250`) 가 `+0x50` 에서 `CreateWindowExW` 를 부른다.

```
CreateWindowExW(0, gOleWindowClass, L"OleMainThreadWndName", 0x88000000,
                0x80000000 ×4, (HWND)-3, 0, g_hinst, 0)
```

값 풀이 (run 문서, 공개 상수):

- 클래스 = **방금 받은 ATOM** (`+0x21 RDX=[gOleWindowClass]`). 문자열 이름이 아니다.
- `0x88000000` = `WS_POPUP | WS_DISABLED` (`+0x1b`). `WS_VISIBLE` 은 없다.
- `0x80000000` ×4 = `CW_USEDEFAULT` (위치 · 크기는 기본값).
- `(HWND)-3` = **`HWND_MESSAGE`** (`+0x38`).

#### 새 말 — 메시지 전용 창 (`HWND_MESSAGE`)

- 부모 자리에 `HWND_MESSAGE` 를 주면 **화면에 안 나오고 메시지만 받는 창** 이 된다 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거에서 확인된 것: 부모 = `-3`, 스타일에 `WS_VISIBLE` 없음 `[정적]`.
- 비유: 상담실 문 옆의 **간판 없는 우편함**. 손님 눈엔 안 보이고, 쪽지만 받는다.

#### 그 뒤

- 성공: 창 핸들을 `ghwndOleMainThread` (`0x180338878`) 에 담고, `gdwMainThreadId = GetCurrentThreadId()` (`+0x68`) → S_OK.
- 실패 (HWND 0, `+0x66 JZ`): `GetLastError` (`+0x85`) 로 **2.7 과 같은 규칙** 의 HRESULT.
  - 그러면 `ThreadFirstInitialize +0x2c9` 가 `g_cClassicSTAInits` 를 내리고, 0 이 되면 `+0x2d2 UnRegisterOleWndClass` (안에서 `+0x12 UnregisterClassW`, 결과는 안 봄, `+0x22` 에서 `gOleWindowClass` 를 0 으로).
  - 그 HRESULT 로 `CoInitializeEx` 가 실패한다.
- 창 함수 `OleMainThreadWndProc`: `WM_CLOSE` · `WM_DESTROY` 는 부수는 중이 아니면 막고, `0x405` 는 따로, **나머지는 전부 `DefWindowProcW`**. 만드는 동안 오는 메시지에 combase 가 할 일은 없다 (결과서).

#### 비슷한 이름 둘 — 이 길이 아니다

- `GetOrCreateSTAWindow` (rva `0x1229b4`) — 같은 클래스, 창 이름 `OLEChannelWnd`. 부르는 곳은 `StartServer` 다. `CoInitializeEx` 길이 아니다 (결과서).
- `OleInitialize` — combase `_index.tsv` 에 없다 (ole32 쪽, 이 요청 밖).

#### 이게 다음 stub 에 무엇을 뜻하나

- 등록 stub 이 돌려준 ATOM 은 combase 가 `gOleWindowClass` 에 담아 **그대로** `CreateWindowExW` 클래스로 넘긴다.
- 그래서 그다음 `NtUserCreateWindowEx` stub 이 **같은 값** 을 받는다 (run 문서 PE_emulator "한 줄로").

### 2.9 `RegisterWindowMessageW` 는 이 길에 없다 `[정적]`

요청 쪽은 seq 9941 의 `NtUserRegisterWindowMessage` 를 같은 길의 앞 단계로 봤다. 아니었다.

1. user32 에서 `RegisterWindowMessageW` 와 `RegisterClipboardFormatW` 는 **같은 함수** (rva `0x4ce60`) 다.
   - PDB 별칭이 아니라 **내보내기 이름이 둘** 붙은 것이다 (서수 2245 · 2270, 결과서).
2. 이 함수는 `RtlInitUnicodeString` (`+0x24`) → `NtUserRegisterWindowMessage` (`+0x35`) → 받은 UINT 그대로 돌려준다 (오류값 안 씀).
3. combase 에서 이걸 부르는 곳은 `CLIPFORMAT_UserUnmarshal +0xad` · `CLIPFORMAT_UserUnmarshal64 +0x1f7` **둘뿐** 이다 — STA 초기화 · 등록 길과 무관하다.
4. 그래서 seq 9941 의 0 은 이번 막힘의 원인이 아니다. 누가 불렀는지는 `[공백]` (동적 6 — Q242).

비유: 번호표 신청 바로 앞에 찍힌 다른 서류 (클립보드 / 메시지 이름 등록) 는 **같은 줄에 선 다른 손님** 이었다.

### 2.10 덤 — `NtUserGetAtomName` 과 `NtUserCreateWindowEx` 사슬 `[정적]`

#### `NtUserGetAtomName` (`0x109c`) — 원 요청 물음 4, 닫힘

- 부르는 곳: `user32!ClassNameToVersion +0x226`. 클래스 이름이 **아톰일 때만** (`+0x98 TEST RBX,-0x10000` · `+0x9f JZ`).
  - 2.8 의 `CreateWindowExW` 는 클래스로 ATOM 을 주니, 창 만들기 길 (`VerNtUserCreateWindowEx +0x177 ClassNameToVersion`) 에서 이 줄을 탈 것이다 `[추측: 두 [정적] 줄을 이은 것, 실행으로 본 적 없음]`.
- 인자 2개: 아톰, 그리고 `UNICODE_STRING` (`MaximumLength 0x200`, 버퍼는 `pUserHeap` 에서 0x200 B — 0 으로 안 채움).
- 반환 = **글자 수**. user32 가 그 값으로 `Length` 를 직접 쓴다 (`+0x232 ADD AX,AX` · `+0x235`).
- 0 이면 `ClassNameToVersion` 이 NULL → `VerNtUserCreateWindowEx +0x182 JZ` → 커널 `NtUserCreateWindowEx` 를 **안 부르고** NULL.
- 받은 이름은 활성화 컨텍스트에서 "창 클래스 바꿔치기" 가 있나 찾는 **열쇠** 다 (`RtlFindActivationContextSectionString(…, 3, …)`). 못 찾으면 (`0xC0150001` · `0xC0150008`) 원래 아톰 그대로 간다.

#### `NtUserCreateWindowEx` (`0x106f`) — 원 요청 물음 5, 사슬만

- `CreateWindowExW +0x7d` → `CreateWindowInternal +0x1de` → `VerNtUserCreateWindowEx +0x22f` (인자 **17개**).
- 둘째 = 준 클래스, 셋째 = `ClassNameToVersion` 결과 → 바꿔치기가 없으면 **같은 아톰 둘** 이 간다.
- 0 을 받으면 `GetLastError` 가 `0x57f` 일 때만 기본 클래스 / 버전 클래스로 다시 시도. 그 밖 (오류 0 포함) 은 NULL → combase ④ 실패 (2.8).
- 받은 핸들 쪽 갈래 (`CreateWindowInternal +0x1e3 ~ +0x234`, `ValidateHwnd`) 는 `[정적, 대충 읽음]` — 본격은 다음 요청 (Q243 ①).

### 2.11 요청 쪽 로그와 코드 맞춰 보기 · PE_emulator 쪽 한 줄 답

#### 로그와 코드 (요청 쪽 로그 — 우리 `[관찰]` 아님)

- "`NtUserUnregisterClass` 를 부르는 쪽은 combase `[추측]`" → **맞다**.
  - 둘째 인자 `0x7ffffcf20000` = combase 의 `g_hinst` (`0x180337fb0` 칸).
  - 부르는 줄은 `RegisterOleWndClass +0x1397be` (첫 등록 0 뒤) 또는 `UnRegisterOleWndClass +0x12` (창 만들기 실패 뒤) 둘 중 하나 `[정적]`.
- "앞의 `NtUserRegisterWindowMessage` 도 같은 길" → **아니다** (2.9).

#### PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 지금 PE_emulator | 판정 |
|---|---|
| `NtUserRegisterClassExWOW` 늘 `0xC000`, 인자 안 읽고 메모리 안 씀 | **맞다** (AX 가 0 이냐만 봄, 출력 인자 없음) |
| 헤더 주석 "부르는 쪽 `RegisterClassExW`" | **일부 틀림** (combase 는 `RegisterClassW`) |
| `layer.cpp:743` 인자 수 0 | **표기만 다름** (진짜 7) |
| `NtUserUnregisterClass` 기본 대답 0 | **안전** (combase 가 결과를 안 봄. 1 을 줘도 24 B 는 안 써야 안전) |
| `NtUserRegisterWindowMessage` 0 | **막힘과 무관** |
| `NtUserGetAtomName` (`0xc000` 이면 1) | **동작은 하나, 뜻이 다름** (반환 = 글자 수) |

> run 문서 한 줄: **`NtUserRegisterClassExWOW` stub 은 0 아닌 ATOM 하나, `NtUserUnregisterClass` 는 1 (또는 지금처럼 0) — 출력 칸 · 공유 메모리는 안 써 줘도 된다.**

- 요청서의 첫 로그 (seq 10019 ~ 10021) 는 셋 다 **기본 대답 0** 이었다. 결과서는 에뮬레이터의 **새 로그** (AE01 뒤, `NtUserCreateWindowEx = 0` 다음에 `NtUserUnregisterClass`) 도 줄과 대조했다 — 2.8 의 실패 갈래 (`InitMainThreadWnd` 실패 → `UnRegisterOleWndClass`) 와 맞고, 이때도 `GetLastError() == 0` 이면 `0x8007000e` 다. 로그 쪽 사실은 요청 쪽이 준 것이고, 우리는 줄만 맞춰 봤다.
  - 곧 등록이 성공해도, 그다음 창 만들기가 0 이면 **같은 "메모리 부족" 으로 끝난다.** 그래서 다음 고리가 `NtUserCreateWindowEx` 다 (3절).

### 2.12 정리 — 비유로 한 번 더

- COM 담당 건물이 **첫 상담실** 을 열 때, 창 담당 건물을 통해 그래픽 부서에 **창 설계도 이름** 을 올린다.
- 그래픽 부서는 **번호표 한 장** 만 준다. 창 담당 건물은 숫자가 있나만 본다. 받은 번호표는 그대로 **간판 없는 우편함** (숨은 창) 을 만드는 데 쓴다.
- 번호표가 비어 오면 COM 담당 건물은 한 번 **지우고 다시** 신청한다. 지울 때 내민 **빈 봉투** 는, 그래픽 부서가 "1" 과 함께 서류를 넣어 줄 때만 치운다.
- 두 번째도 비어 오면 "왜요?" 를 묻는다. 대답이 없으면 **"메모리 부족"** 도장을 찍어 돌려준다. 에뮬레이터의 .NET 쪽은 그 도장을 보고 "메모리 부족 예외" 를 낸 것으로 읽힌다 (그 변환 줄은 `[공백]`).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 0a29d76 -- runs/03_sprint/flow.md`)

그림 · 2장 단계는 안 바뀌었다. 바뀐 곳:

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 줄 | user32 · win32u 줄은 run85 · run86 몫만. combase 줄 없음 | combase.pdb 새 줄 (`DA1DC0C4F35CEE58F672CBB20460D11D1`, 처음 analyze). user32 줄에 run131 export, win32u 줄에 스텁 넷 (`0x10a2` · `0x10ad` · `0x1032` · `0x106f`) |
| 기준 — 근거 줄 | — | "(run131) 정적 바퀴 … 사진 값 대조 없음, 요청 쪽 값은 `[관찰]` 로 안 씀" |
| 3장 run85 "user32 시작 함수" 절 | 창 클래스 이야기 없음 | 한 줄: `DllMain` 뒤 첫 창 클래스 등록 · 지움은 3장 끝 새 절. 그 길은 `gSharedInfo` · `gpsi` 를 안 읽는다 |
| 3장 끝 | — | **새 절 "창 클래스 등록 (COM STA)"** (`[정적]`) — 나무 하나 + 사실 여섯 줄 |
| 4장 run86 정적 ④ | "창 만들기 (`RegisterClassExW` · `CreateWindowExW` · `gSharedInfo` 칸)" 남음 | **일부 해결 → run131**: 클래스 등록 · 지움 몫만 `[정적]`. `CreateWindowExW` · 메시지 루프 몫은 그대로 |
| 4장 | — | Q240 해결됨 줄, run131 새 줄 셋 (동적 → **Q242**, 정적 → **Q243**, PE_emulator 쪽) |
| 5장 | — | run131 줄 둘 |

- 태그 변화: 창 클래스 등록 쪽은 "아무것도 모름 (run86 이 남긴 몫)" → **`[정적]`**. `[관찰]` 로 올라간 건 없다 (동적 안 함).
- 짐 지도 (`baggage.md`) 는 안 바뀌었다 — test.exe 초기화 밖이라서.

### 큰 그림에서 어떻게 이어지나

- run85 (user32 `DllMain`) → run86 (gdi32 · `NtGdiInit2`) 다음, **COM 을 STA 로 여는 프로그램** 이 처음 하는 창 일이 이번에 채워졌다.
- 이번 길의 끝은 `NtUserCreateWindowEx` (`0x106f`) 다. 등록 stub 이 준 ATOM 이 거기로 그대로 간다 (2.8).
  - 그래서 다음 고리는 "**창 만들기 syscall 은 무엇을 돌려줘야 하나, 돌아온 핸들로 user32 가 무엇을 읽나**" — Q243 ① (run86 ④ 의 남은 몫과 겹침).
- PE_emulator 쪽에선 "등록 · 지움 stub 은 값 하나면 된다" 가 정해졌다. 등록이 성공하면 그다음 부르는 게 `CreateWindowExW` 이고 (2.8), 결과서가 대조한 새 로그도 거기 (`NtUserCreateWindowEx = 0`) 서 같은 HRESULT 로 끝난다 (2.11, 요청 쪽 로그). 그래서 막힘은 이제 창 만들기 쪽이다 `[추측: 요청 쪽 로그 + [정적] 줄]`.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q242** (동적)

- 전부 **test.exe 로는 못 본다.** STA COM 을 여는 프로그램 (`CoInitializeEx(…, COINIT_APARTMENTTHREADED)` 를 부르는 작은 exe 등) 이 필요하다. 고르기는 사용자 결정.
- 유저 멈춤점, 프로세스 조건, ret 자리.

| # | 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|---|
| 1 | 진짜 ATOM 값, `CLSMENUNAME` 이 정말 입력인지 (커널이 안 쓰나) — `user32!RegisterClassExWOWW+0x23d` `ax`, 부르기 전 · 후 `dq @rbp-0x80 L3` | 커널이 그 칸을 쓰는지는 커널 본문 (범위 밖) 이나 실행 때 값으로만 |
| 2 | `NtUserUnregisterClass` 출력 24 B 가 진짜로 0 으로 남는지 — `user32!UnregisterClassW+0xd7` | 커널이 채우는 값 |
| 3 | `NtUserGetAtomName` 의 진짜 반환 · 버퍼 (21글자인지, 끝 NUL 제외인지) — `user32!ClassNameToVersion+0x22d` | 커널이 채우는 값. 끝 NUL 은 지금 `[추측]` |
| 4 | STA 길의 진짜 값 — `combase!ThreadFirstInitialize+0x309` `eax`, `combase!InitMainThreadWnd+0x57` `rax`, (실패 때) `RegisterOleWndClass+0x1397f5` `eax`, 전역 `combase+0x338870` · `+0x338864` | 실행 때 값. 둘째 덩어리 오프셋이 WinDbg 에서 같은 이름으로 보일지도 `[추측]` |
| 5 | "API 세트 있나" (`0x179074`) 가 진짜 Windows 에서 참인지 — `+0x2a` 뒤 `eax` | 별명 사전은 실행 때 PEB 에 있다 |
| 6 | 요청 쪽 seq 9941 `NtUserRegisterWindowMessage` 의 호출 스택 | 정적으로는 combase 길이 아니라는 것까지만 (2.9) |

### 정적으로 남은 것 → **Q243**

1. **`NtUserCreateWindowEx` 본격** (원 요청 물음 5) — 인자 17개 뜻 · 출력, 돌아온 핸들로 user32 가 syscall 없이 읽는 칸 (`gSharedInfo` · `ValidateHwnd` · `MDICompleteChildCreation` 갈래), `InitMainThreadWnd` 가 돌아갈 때까지 더 부르는 user32 함수.
   - 이번엔 사슬 · 진입 줄만 봤다 (`[정적, 대충 읽음]`). run86 정적 ④ · Q163 ④ 의 남은 몫과 겹친다.
2. `UninitMainThreadWnd` 의 `IsWindow` (`+0x25`) · `DestroyWindow` (`+0x46`) 길 — `CoUninitialize` 때 `NtUserDestroyWindow` (`0x1095`) stub 답.
3. `NtUserRegisterClassExWOW` 인자 6 플래그 `0x80` · `0x40` 의 이름 — 지금 `[공백]`.
4. (낮음) combase 지연 가져오기 공용 꼬리 (`__tailMerge_…window_ext_l1_1_0_dll`, rva `0x1790c2`) 안과, 요청 이름 `l1-1-0` ↔ 스키마 `l1-1-1` 의 버전 허용 — 지금 `[추측]`.

### 대기열 밖 — 근거가 없어 남은 것

- CLR 이 `0x8007000e` 를 `OutOfMemoryException` 으로 옮기는 줄 — 이 레포에 clr 이 없다 `[공백]`.
- `g_systemCallFilterId == 5` 의 뜻 (지움 길에서 커널을 안 부르고 1) — `[공백]`.
- 인자 7 이 WOW 칸인지 — `[추측]`.

---

## 5. 더 읽기

- run 문서: [`run131-user32-regclass.md`](run131-user32-regclass.md) — "정적 — 호출 지점" 표 (call · ret 전부), 인자 표 둘, 차례 표, PE_emulator 표, flow-writer 대조
- 요청서: [`_workspace/sast/128_user32-regclass.md`](../../_workspace/sast/128_user32-regclass.md)
- 결과서: [`_workspace/sast/128_user32-regclass_result.md`](../../_workspace/sast/128_user32-regclass_result.md) — "먼저 읽는 한 줄 답", `NtUserGetAtomName` (가) ~ (라), "읽을 때 주의", "동적으로 확인할 것"
- flow.md: [`flow.md`](flow.md) 3장 끝 "창 클래스 등록 (COM STA)" 절 · 3장 "user32 시작 함수" 절 run131 줄 · 4장 run86 줄 ④ · run131 줄 셋
- 앞 바퀴: [run85 해설](run85-user32-init-해설.md) (user32 `DllMain`, `gSharedInfo` · `gpsi`) · [run86 해설](run86-gdi32-init-해설.md) (gdi32, `NtGdiInit2` stub 선례)
- 대기열: `_workspace/plan.md` Q240 · Q242 · Q243

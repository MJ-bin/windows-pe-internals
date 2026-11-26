# run142 user32-connect — 해설

> **고쳐 읽기 (run144)**: 2.8 의 "GDI 답 — DC · 글꼴 · 글자 폭이 0 이면 조용히 0 (Q270)" — `GdiGetCharDimensions` · `GetTextExtentPointW` · `GetTextMetricsW` · `SelectObject` · `GetDeviceCaps` 는 syscall 앞에서 공유 핸들 표의 **DC 칸** (`+0xe == 1` · `+0xc == H>>16` · `+0x8 & ~1 ==` 이 프로세스 PID 정확히 · `+0x10` 풀면 0 아닌 `DC_ATTR` 포인터) 을 본다 `[정적]`. 칸이 없으면 `GdiGetCharDimensions` 가 아무것도 안 쓰고 돌아가 `SoftModalMessageBox +0x153` 이 폭 0 으로 0. HFONT 는 종류 `0xa0000` + 글꼴 칸이 있어야 한다. DC 칸 만들기 · `DC_ATTR` 전체 · 글꼴 사본은 Q278. 본문은 그대로 두었다 → [run144 해설](run144-gdi-startup-해설.md) 2.13

> **고쳐 읽기 (run146)**: 4절 "정적으로 남은 것" 의 N1 · N2 와 최소 길 표 19 ~ 27 의 `[공백]` (어느 호출이 syscall 없이 풀리나 · 표시 조건) 이 `[정적]` 으로 닫혔다 — `WM_SETFONT` · `WM_INITDIALOG` 는 핸들 항목 `+0x8` == TEB `+0x78` · 사본 `+0x12` 비트 2 = 0 · `[[TEB+0x820]+0x10] | [TEB+0x838]` 의 `0x2020` = 0 · 사본 `+0x80` ≠ 0 이면 `SendMessageWorker` 가 syscall 없이 창 프로시저를 부르고, 표시 · 초점 · 앞으로 내기 반환은 안 본다. 4절 "대화상자 자료가 어느 힙 `[추측]`" → **`pUserHeap`** (`pdlg` 0x58 B 는 `AllocLookasideEntry`, 그걸 담는 사본 `+0x128` 여분 블록은 커널 되부름 `0x7b`). 2.11 목록 7 "`ValidateHwnd` 를 통과하는 hwnd" 는 run146 "stub 이 채워야 하는 최소 목록" 으로 채움. 메시지 상자 템플릿의 컨트롤 수는 그대로 `[공백]`. 본문은 그대로 두었다 → [run146 해설](run146-dialog-window-해설.md) 2.5 · 2.6 · 2.8 · 2.9 · 2.12

> 이 글은 [run142 문서](run142-user32-connect.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q268** — PE_emulator 요청 "user32 창 API 넷 (`MessageBox*` · `DialogBoxParam*` · `LoadIcon*` · `GetSystemMetrics`) 이 `NtUserGetThreadState(0xe)` 뒤 무엇을 보고 0 / −1 로 나가나 · 메시지 상자 한 장이 뜨기까지의 가장 짧은 길" (`_workspace/plan.md`, 이번에 **끝 (run142)**)
  - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/335_internals-user32-connect.md`, 튜닝 `335_tune15.md` GC, 병목 샘플 72 (요청 쪽 에뮬레이터 숫자).
  - 사용자 결정 2026-11-25 (PE_emulator D154) 요청 넷의 **둘째**. 정적만.
  - 우리 대기열 **Q161 ③ · Q163 ④ 를 당긴 것**이다 (run85 · run86 이 남겨 둔 "창 API 가 읽는 칸" · "창 만들기 · 메시지 고리").
  - 결과서 판정 **분석성공** — 질문 1 (가드) · 3 (종류 `0xe`) 은 닫음, 질문 2 (최소 길) 는 **틀 + 확인된 벽까지**.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q273** (Q162 세션과 합침)
    - 정적 → **Q274** (결과서 다음 요청 후보 N1 · N2 · N3 · N5)
    - GDI 쪽 (DC · 글꼴 · 글자 폭, 결과서 N4) → **Q270** (sast/141 `gdi-startup`)
- 근거:
  - run 문서 [`run142-user32-connect.md`](run142-user32-connect.md) (가장 중요)
  - 요청서 [`_workspace/sast/139_user32-connect.md`](../../_workspace/sast/139_user32-connect.md) · 결과서 [`139_user32-connect_result.md`](../../_workspace/sast/139_user32-connect_result.md)
    - **처음 analyze 없음.** user32 · win32kfull · win32kbase 는 이미 analyze 돼 있어 `^이름$` export · refs 만 했다. win32u 는 `_syscalls.tsv` 읽기만, ntoskrnl 은 있던 `.asm` 읽기만.
  - flow.md 변경: docs 커밋 `1363f6a` (`git show 1363f6a -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 안 탄다.**
   - test.exe 는 user32 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run131 · run136 과 같은 줄).
   - 그래서 이번 이야기는 **"user32 를 쓰는 프로그램이 처음 창 API 를 부를 때"** 의 일이다. 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - "107 / 107", "seq 1360", "syscall 하나뿐" 같은 것은 PE_emulator 로그다. 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다. 이 글에서는 "에뮬레이터 로그" 라고만 부른다.
3. **오프셋 읽는 법** (앞 해설들과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문 "`+0x62 CALL`" 처럼 명령 이름과 같이 적힌 숫자 = **함수 안 그 줄** (call 자리 포함).
   - "`+0x14 JZ → +0x16c`" 는 그 줄과 점프가 가는 곳.
   - 콜스택으로 본 적은 없다 (정적만).
4. **값은 하나도 안 봤다.**
   - `psi` 칸에 진짜 Windows 가 무슨 숫자를 넣는지, TEB `+0x78` 에 진짜로 뭐가 들어 있는지는 이번 범위 밖이다 (요청대로 — 값은 동적 Q273 몫).
   - 이번에 본 건 "**코드가 어느 칸을 읽고, 0 이면 어디로 가나**" 뿐이다.
5. **비유 이름이 run 문서와 다르다.**
   - run 문서 "결과" 비유: **은행 창구 넷 · 본점 · 번호표 · 책상 · 기준 글자 크기 · 모니터 안내판 · 펜과 종이** (run 문서는 이 비유를 `[추측: 비유]` 로 둔다).
   - 이 글은 앞 해설들 (run85 · run86 · run131 · run136) 과 맞춰 **창 담당 건물 · 그래픽 부서 · 단축번호 · 내선 번호표 · 서버 정보판 · 그림 도구 진열장** 으로 부르고, 이번에 **문지기** 를 새로 붙인다. 그림만 다르고 사실은 같다 (아래 "비유").

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- TEB = 직원 **개인 안내판** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2
- IAT (가져오기 칸) = 건물마다 가진 **빈 주소록** → [run36 해설](../02_sprint/run36-snap-module-해설.md)
- win32k syscall (번호 `0x1000` 이상) · `KiSystemServiceStart` 가 번호부를 고르는 자리 → [run56 해설](run56-syscall-args-해설.md) 2.9
- **run85 에서 푼 것** → [run85 해설](run85-user32-init-해설.md)
  - user32 = **창 담당 건물**, win32u = **단축번호 버튼판** → 2.1
  - `SHAREDINFO` (`gSharedInfo`) = **공용 안내 묶음**, `psi` (`gpsi`) = 그 첫 줄의 **화살표** → 가리키는 곳 = **서버 정보판** → 2.5
  - **GUI 스레드** · **커널 콜백** · `KernelCallbackTable` (**내선 번호표**) · `__ClientThreadSetup` = 84번째 칸 → 2.12
  - **DPI** = 화면 확대 비율 → 2.5
- **run86 에서 푼 것** → [run86 해설](run86-gdi32-init-해설.md)
  - gdi32 · gdi32full = **그림 도구 건물**, GDI 공유 메모리 = **그림 도구 진열장** → 맨 위 "비유"
  - `GetAppCompatFlags2` 안의 `NtUserGetThreadState(0xe)` (표 A13) → 2.11
- **run131 에서 푼 것** → [run131 해설](run131-user32-regclass-해설.md)
  - 창 클래스 = **창 설계도 이름**, 창 함수 = 메시지가 오면 부를 함수 → 1절 "새 말 — 창 클래스"
- **run136 에서 푼 것** → [run136 해설](run136-winsta-name-해설.md)
  - 창 스테이션 = **상가 건물**, 데스크톱 = 그 안의 **방**

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, TEB = 직원 **개인 안내판**, 커널 = **관리사무소**, syscall = **신청 창구**.
- user32 = **창 담당 건물**, win32u = **단축번호 버튼판**, win32k = 관리사무소 **그래픽 부서**.
- `gSharedInfo` = **공용 안내 묶음**, `psi` = 묶음 첫 줄 화살표가 가리키는 **서버 정보판**.
- 커널 콜백 = 관리사무소가 일하다가 가게 사람을 **거꾸로 불러내는** 것. 그때 보는 게 **내선 번호표** (84번 = `__ClientThreadSetup`).
- 이번에 새로 붙는 그림:
  - 창 API 넷 = 창 담당 건물의 **문 넷**. 문마다 **문지기** (가드) 가 한 명씩 서 있다.
  - 문지기는 들어오는 직원의 개인 안내판에서 "**그래픽 손님 등록**" 칸 (TEB `+0x78`) 을 본다.
  - 칸이 비었으면, 문지기는 그래픽 부서 단축번호 하나 (`NtUserGetThreadState(0xe)`) 를 누르고 대답을 기다린다. 대답이 0 이면 문을 닫는다.
  - 그 번호는 사실 **"늘 1 이라고만 대답하는 번호"** 다. 진짜 일은 대답이 아니라, **그 번호를 누르는 순간** 그래픽 부서가 이 직원을 그래픽 손님으로 등록해 주는 것이다 (run85 2.12 의 그림 그대로).
  - 데스크톱 정보 (TEB `+0x820`) = 직원이 일하는 **방의 정보 카드**. (run 문서는 "책상" 이라 불렀다. run136 에서 데스크톱을 "방" 으로 불렀으니 이 글은 **방 정보 카드** 로 쓴다.)
  - 메시지 상자 = 창 담당 건물이 직원 앞에 세우는 **작은 알림판**. 누가 단추를 눌러야 치워진다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| ① 넷이 실패 (0 / −1) 로 나가는 첫 가지는 | **같은 꼴의 문지기를 각자 품고 있다.** 공통 도우미는 없다. "TEB `+0x78` 이 0 이면 `NtUserGetThreadState(0xe)` 를 부르고, **반환이 0 이면 실패**". syscall 뒤에 TEB `+0x78` 을 다시 안 읽는다 `[정적]` (2.3) |
| ② 종류 `0xe` 는 무엇을 돌려주나 | **아무 칸도 안 읽고 상수 1.** 락도 잡기 전에 끝난다 `[정적]` (2.5) |
| ③ 그럼 왜 부르나 | **부수 효과가 목적.** win32k syscall 이라서 커널이 먼저 그 스레드를 GUI 스레드로 바꾸고, 그 안에서 TEB `+0x78` 을 쓰고 user32 `__ClientThreadSetup` 을 되부른다 `[정적, 커널 가운데는 이름만]` (2.6) |
| ④ 에뮬레이터는 왜 막혔나 | 기본 대답 **0** 이 넷을 **정확히** 실패 가지로 보낸다. 고칠 자리는 **syscall 대답 하나** (2.7) |
| ⑤ 1 로 고치면 상자가 뜨나 | **아직 안 뜬다.** 바로 뒤에 벽 셋 (TEB `+0x820` · `psi+0x1b56` · `gSharedInfo+0x18`) 과 GDI 답이 있다 `[정적]` (2.8) |
| ⑥ 상자는 언제 끝나나 | `MB_DlgProc` 가 **`WM_COMMAND` (단추)** 를 받아 `EndDialog` 를 불러야 끝난다. 에뮬레이터에선 그 메시지를 넣을 입구가 `NtUserPeekMessage` 대답 하나 — **무엇을 넣을지는 정책 (사용자 몫)** (2.9) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**초기화가 끝난 뒤, 프로그램이 창 API 를 처음 부르는 순간** 이다. test.exe 에는 없는 옆길이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
 │    └─ (2.7) dll 전원 켜기
 │          └─ (user32 를 올리는 프로세스라면) user32 DllMain → GdiDllInitialize   ← run85 · run86
 └─ … → NtContinue (은퇴) → exe 진입점 → (CRT) → main
                                                   └─ (user32 를 쓰는 프로그램이라면)
                                                       MessageBoxW / DialogBoxParamW /
                                                       LoadIconW / GetSystemMetrics      ← 이번 (run142)
```

- flow.md 에서도 **3장 user32 시작 함수 절 · gdi32 하위 절 바로 다음** 에 새 하위 절로 들어갔다 ("위 user32 시작 함수 절 (run85) · gdi32 하위 절 (run86) 다음, 프로그램이 처음 창 API 를 부를 때의 일이다").
- 프로그램 쪽에서 부르는 일이라 CLAUDE.md 범위로 치면 "`main` 안에서 OS 를 다시 부르는 화살표" 쪽이다. 그래서 그림엔 안 넣었다.

### 왜 이 질문이 지금 왔나

- PE_emulator 는 진짜 nt · ntdll 이름으로 짓는 에뮬레이터다. win32k (그래픽 부서) 는 없고, `NtUser*` syscall 은 기본 대답을 준다.
- 악성 GUI 샘플이 들어오면서 막힘이 생겼다 (에뮬레이터 로그, 원 요청):
  - `MessageBox*` 0 — 107 / 107
  - `DialogBox*` −1 — 50 / 50
  - `LoadIcon*` 0 — 78 / 78
  - `GetSystemMetrics` 0 — 484 / 484
  - 자동 압축 풀기 · 설치기 무리가 **첫 대화상자에서 끝난다**.
- 로그의 공통점: `MessageBox*` 안 syscall 이 **`NtUserGetThreadState(0xe)` 하나뿐** 이고 대답이 0, 그리고 바로 실패.
  - 예: `seq 1359 (MessageBoxA) → 1360 (NtUserGetThreadState(0xe) 기본 대답 0) → 1361 (0)`.

### 무엇이 궁금했나

1. **문지기**: 넷이 실패로 나가는 첫 가지는 무엇을 보나? 넷이 같은 도우미 함수를 쓰나?
2. **최소 길**: 문지기를 지난 `MessageBoxW(NULL, 글, 제목, MB_OK)` 한 번이 돌아올 때까지 무엇을 부르고 무엇을 읽나? 상자는 언제 끝나나?
3. **종류 `0xe`**: 이 syscall 의 진짜 뜻은? 정상 대답은 0 인가? 부르는 목적이 대답이 아니라 부수 효과인가?

### 바퀴 전에 알던 것

- `[정적]` (run85) GUI 스레드로 바뀌는 때 = 그 스레드의 **첫 win32k syscall**. 바뀐 뒤 커널이 내선 번호표 84번 `__ClientThreadSetup` 을 되부른다.
  - 다만 **TEB `+0x78` 을 쓰는 쪽은 win32k 일 것** 이라는 건 `[추측]` 이었다 (user32 코드엔 쓰는 줄이 없었다).
- `[정적]` (run86 A13) `GetAppCompatFlags2` 는 TEB `+0x78` 이 0 이면 `NtUserGetThreadState(0xe)` 를 부른다. 그때 대답은 "**아무 값**" 이라고 적었다 (0 이면 `GetAppCompatFlags2` 가 0 으로 넘어갈 뿐이라서).
- `[정적]` (run85) `psi` 는 **읽을 수 있는 `0x1b68` B 이상** 이면 0 으로 채워도 `DllMain` 은 끝까지 간다.
- `[공백]` (run121 ①) `Win32ThreadInfo` (TEB `+0x78`) 를 유저 TEB 에 놓는 줄 — win32kbase `xxxCreateThreadInfo` 가 후보.

---

## 2. 찾은 것

### 2.1 한눈에 — 문지기 · 진짜 일 · 벽 · 끝 `[정적]`

run 문서 트리를 네 덩어리로 줄인 것이다 (오프셋은 ret 자리).

```
① 문지기 (넷 다 같은 꼴, 각자 안에)
   MessageBoxWorker  +0x69   NtUserGetThreadState(0xe)   0 → 0 반환          ── SYSCALL 0x1000
   InternalCreateDialog +0xd40  〃                          0 → NULL → −1
   LoadIcoCur        +0x245  〃                          0 → 0
   RealGetSystemMetrics +0x123  〃                          0 → 0

② 그 syscall 이 커널에서 일으키는 진짜 일 (스레드의 첫 win32k syscall 일 때)
   nt!KiSystemServiceStart +0x924 KiConvertToGuiThread → … 
     win32kbase!xxxCreateThreadInfo  +0x756 [TEB+0x78] ← PsGetThreadId 반환
                                     +0x1380 ⇢ win32kfull!xxxClientThreadSetup +0x53 KeUserModeCallback(0x54)
                                                ⇢ user32!__ClientThreadSetup → ClientThreadSetup → [TEB+0x820] ← 0x48 B
   win32kfull!NtUserGetThreadState  +0x14 JZ → +0x16c  EAX = 1   (대답은 그냥 1)

③ 문지기 뒤 벽 (메시지 상자 길)
   SoftModalMessageBox  GDI (DC · 글자 폭) 0 → 0 · psi+0x1b56 0 나눔 ·
                        gSharedInfo+0x18 접근 위반 · [TEB+0x820]+0x30 접근 위반

④ 끝 — 모달 고리
   DialogBox2  NtUserPeekMessage / NtUserWaitMessage 고리
     ⇢ MB_DlgProc  WM_COMMAND (단추) → EndDialog → 고리 끝
     WM_QUIT → EndDialog 없이 끝 → MessageBoxW 0
```

**새 말 — 가드 (문지기)**: 함수 앞머리에서 "이 조건이 아니면 더 안 간다" 하고 막는 몇 줄. 비유: 건물 문 앞의 **문지기**.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `MessageBoxWorker +0x69 NtUserGetThreadState(0xe)   0 → 0 반환   ── SYSCALL 0x1000`

- **누가**: user32 `MessageBoxWorker` (rva `0x5f1d4`) — `MessageBoxW` → `MessageBoxTimeoutW` 가 부르는 일꾼 함수다.
- **어디서**: call 자리 `+0x62`, 돌아오는 자리 (ret) `+0x69`.
- **누구를**: user32 IAT 칸 `0xb65f8` 을 거쳐 win32u `NtUserGetThreadState` (번호 `0x1000`) 를. 인자 `ECX = 0xe`.
- **무엇을 위해**: 문지기다. 이 줄 바로 앞 `+0x58` 에서 TEB `+0x78` 이 0 인지 보고, 0 일 때만 여기 온다. 대답이 0 이면 `+0x73` 에서 0 을 들고 `+0x75` 에서 끝으로 뛴다.
- 태그 `[정적]`.

#### 줄 2: `xxxCreateThreadInfo +0x756 [TEB+0x78] ← PsGetThreadId 반환`

- **누가**: 커널 쪽 win32kbase `xxxCreateThreadInfo` (rva `0x194d84`).
- **어디서**: `+0x745` 에서 `PsGetThreadId` 를 부르고 (ret `+0x74c`), `+0x756` 에서 그 반환을 쓴다. 이 줄은 함수 안 줄이다 (call 아님).
- **누구를**: 부르는 건 `PsGetThreadId` 하나. 쓰는 곳은 **유저 TEB 의 `+0x78`**.
- **무엇을 위해**: 이 직원의 개인 안내판에 "그래픽 손님 등록" 표시를 남기려고. 이게 바로 문지기들이 보는 칸이다.
- 태그 `[정적]`. 다만 "값 = 스레드 ID" 는 `[추측: 가져오기 이름에서]` (2.6).

### 2.3 ★ 문지기 넷 — 같은 꼴을 각자 품고 있다 `[정적]`

요청서는 "넷이 같은 도우미 (연결 확인 함수) 를 쓰면 그 함수 하나로 답" 해 달라고 했다. 답은 **"같은 도우미는 없다"** 다.

#### 문지기의 다섯 걸음 (넷 다 똑같다)

1. 지금 스레드의 TEB 주소를 꺼낸다 (`GS:[0x30]`).
2. TEB `+0x78` 이 0 인지 본다.
3. 0 이 **아니면** 그냥 통과.
4. 0 이면 `ECX = 0xe` 로 `NtUserGetThreadState` (IAT `0xb65f8` → win32u `0x1000`) 를 부른다.
5. 대답 (`RAX`) 이 0 이 아니면 통과, **0 이면 실패로 나간다.**

#### 넷의 자리

| API | 문지기가 든 함수 | 문지기 줄 | 실패하면 |
|---|---|---|---|
| `MessageBoxW` · `MessageBoxA` | `MessageBoxWorker` | `+0x58 ~ +0x75` (call `+0x62`) | **0** |
| `DialogBoxParamW` | `InternalCreateDialog` | `+0x13c ~ +0xd50` (call `+0xd39`) | NULL → `InternalDialogBox` 가 **−1** |
| `LoadIconW` | `LoadIcoCur` | `+0x52 ~ +0x258` (call `+0x23e`) | **0** |
| `GetSystemMetrics` | `RealGetSystemMetrics` | `+0x19 ~ +0x131` (call `+0x11c`) | **0** |

- `MessageBoxA` 는 `MessageBoxTimeoutA` 에서 글자를 W 로 바꾼 뒤 같은 길로 간다.
- `InternalCreateDialog +0x13c ~ +0xd50` 처럼 범위가 넓은 건, 걸음 2 다음 `JZ` 가 함수 **뒤쪽** (`+0xd37`) 으로 뛰어가 거기서 syscall 을 부르기 때문이다. 줄 수가 많은 게 아니다.

#### 왜 "도우미 없음" 이 중요한가 — 단계별로

1. 근거: 넷이 각자 안에 같은 다섯 걸음을 품고 있다 `[정적]`. 컴파일러가 각 함수 안에 풀어 넣은 꼴이다.
2. 같은 IAT 칸 `0xb65f8` 을 부르는 곳이 user32 전체에 **85곳** 이다 (직접 `CALL` 79 + `JMP` 껍데기 6, `ms/disassemble/user32/_refs/0xb65f8.tsv`) `[정적]`.
3. 그래서: 도우미 함수 하나를 고쳐서 넷을 살리는 길은 없다. 대신 **85곳 모두가 같은 syscall 대답 하나를 본다.**
4. 결론 (run 문서): **고칠 자리는 syscall 대답 하나** — `0x1000` 의 인자 `0xe` 에 1.

#### 하나 더 — syscall 뒤엔 TEB `+0x78` 을 다시 안 본다

- 넷의 문지기는 대답이 0 이 아니면 **바로 통과** 한다. syscall 뒤에 TEB `+0x78` 을 다시 읽지 않는다 `[정적]`.
- 예외: 함수로 남은 같은 꼴 `PtiCurrent` (`0x19d10`) 는 대답이 0 이 아니면 `+0x3b JNZ → +0x14` 로 돌아가 TEB `+0x78` 을 **다시** 읽는다 `[정적]`. (또 하나 `IsGUIThread` (`0x52210`) 는 인자가 참일 때만 syscall 을 부른다.)
- 비유: 문지기는 그래픽 부서 대답만 듣고 문을 연다. 직원 안내판을 다시 들여다보지 않는다. 그러니 에뮬레이터 입장에선 **대답만 1 이면 문은 열린다.**

### 2.4 덤 — 로그와 맞춰 본 세 가지

#### (가) `MessageBox*` 안 syscall 이 하나뿐인 이유

- `MessageBoxTimeoutW` 안에도 같은 꼴이 두 군데 있다 (`+0xd2` · `+0x12c`) `[정적]`.
- 그런데 그 둘은 TEB `+0x78` 이 아니라 **"TEB 주소 + `0x800` 이 0 인가"** 를 본다 (`+0xcb ADD RAX,0x800` 뒤 `+0xce JNZ`). TEB 주소에 `0x800` 을 더한 값은 0 이 될 수 없다.
- 그래서 그 둘은 절대 안 타는 **죽은 가지** 다 `[정적]`.
- 결과: 에뮬레이터 로그의 "`MessageBox*` 안 syscall 하나" 는 `MessageBoxWorker +0x62` 하나와 맞는다.

**새 말 — 죽은 가지**: 코드엔 있지만 조건이 늘 거짓이라 실제로는 한 번도 안 가는 갈래. 비유: 늘 잠겨 있는 옆문.

#### (나) `DialogBoxParamW` 가 syscall 을 두 번 부른 이유 (짐작)

- 로그: `3642 (DialogBoxParamW) → 3643 · 3644 (NtUserGetThreadState(0xe) 둘) → 3645 (−1)`.
- 부모 창 핸들이 0 이 아니면 `InternalDialogBox +0xaf` 가 `GetDesktopWindow` 를 부르고, 거기에도 같은 문지기가 있다 (`+0x15 ~ +0x99`) `[정적]`.
- 그 뒤 `InternalCreateDialog` 의 문지기가 또 부른다.
- 그래서 둘 = 이 두 문지기로 보인다 `[추측: 부모 인자 확인 필요]` → Q273.

#### (다) `LoadIconW` 의 "syscall 0 개로 0" 은 문지기가 아니다 (정정)

- 원 요청은 이것도 문지기 탓으로 봤다. 그런데:
  1. 문지기가 실패시켰다면 syscall 이 **하나는** 나와야 한다 (걸음 4).
  2. 로그는 syscall 이 0 개다.
  3. 그래서 문지기 실패가 아니다.
- syscall 없이 0 을 돌려주는 다른 길이 있다 `[정적]`: `LoadIcoCur +0x290 ~ +0x2a9` — 자원 번호가 user32 안 표에 있고, 그 칸 비트 0 이 켜졌고, TEB `+0x810` 이 `0x400` 이상이면 0.
- 그 길이었거나, 그때 TEB `+0x78` 이 이미 0 이 아니었을 것이다 `[추측]` → Q273.

### 2.5 ★ 종류 `0xe` 의 진짜 대답 — 아무것도 안 읽는 상수 1 `[정적]`

win32kfull `NtUserGetThreadState` (rva `0x1ec5b0`) 의 앞머리:

- `+0xf MOV EBX,ECX` — 종류 번호를 받아 둔다.
- `+0x11 CMP ECX,0xe` — 종류가 `0xe` 인가?
- `+0x14 JZ → +0x16c` — 맞으면 뒤로 뛴다.
- `+0x16c MOV EAX,1` — **대답 = 1**.
- `+0x171 JMP → +0x97` — 끝.

여기서 읽어 낼 것:

1. **어떤 칸도 안 읽는다.** 스레드 정보도, 세션 정보도 안 본다. 그냥 1.
2. **락도 안 잡는다.** 이 갈래는 `+0x21 EnterSharedCrit` 보다 **앞** 에서 끝난다.
3. 그러니 **정상 대답은 0 이 아니라 1** 이다. 0 은 user32 가 "실패" 로 읽는다 (넷의 문지기 · `PtiCurrent` · `IsGUIThread`).

- 다른 종류 (0 ~ `0xd`) 는 이번 범위 밖이다.

비유: 그래픽 부서의 이 단축번호는 **자동응답** 이다. 누가 걸든 "네 (1)" 하고 끊는다. 내용 있는 대답은 하나도 없다.

그럼 이상하다. 대답이 늘 1 이면, 왜 굳이 이 번호를 누를까? → 2.6

### 2.6 ★ 진짜 목적 — "이 스레드를 GUI 스레드로 바꾸기" `[정적, 커널 가운데는 이름만]`

**새 말 — 부수 효과**: 함수가 돌려주는 값 말고, 부르는 과정에서 **덤으로 바뀌는 것**. 비유: 자동응답 번호를 누르면 대답은 "네" 뿐이지만, **누른 기록** 이 남아서 등록이 된다.

#### 단계별로 따라가기

1. **번호가 win32k 번호다.** `NtUserGetThreadState` 는 `0x1000` — win32k 번호부 쪽이다 (run85 2.12, run56 2.9).
2. **커널은 대답하기 전에 먼저 스레드를 본다.** `nt!KiSystemServiceStart +0x907 CMP EDI,0x20` 이 "win32k 표인가" 를 보고, `+0x91f` 에서 `KiConvertToGuiThread` 를 부른다 (ret `+0x924`) `[정적]`.
3. **GUI 스레드로 바꾼다.** `KiConvertToGuiThread +0x4` → `PsConvertToGuiThread` (이름만). 이 안에서 win32k 쪽 스레드 호출로 가는 줄은 이번에 안 읽었다 `[추측]`.
4. **win32k 가 스레드 정보를 만든다.** win32kfull `W32pThreadCallout +0x61` → win32kbase `UserThreadCallout +0x36c` → `xxxCreateThreadInfo` `[정적]`.
   - `xxxCreateThreadInfo` — 이름대로 읽으면 "GUI 스레드 정보 만들기" `[추측: 이름에서]`.
5. **TEB 칸을 채운다** (그 안) `[정적]`:
   - `+0x745 PsGetThreadId` → **`+0x756 MOV [TEB+0x78], 그 반환`** — 문지기가 보는 칸. TEB 가 있을 때만 쓴다.
   - `+0x772` `pti+0x208` ← TEB `+0x800` 주소 · `+0x787 / +0x794` TEB `+0x8e8` · `+0x8ec` 도 채운다.
6. **user32 를 되부른다** `[정적]`:
   - `xxxCreateThreadInfo +0x137b` 가 ApiSet 표 `[+0x4e8]` 을 거쳐 간접 호출 = win32kfull **`xxxClientThreadSetup`** (rva `0x1c4f60`). 반환이 음수면 변환 실패 (`+0x1389 JS`).
   - `xxxClientThreadSetup +0x4c` → **`KeUserModeCallback(0x54, …)`** — `0x54` = 84. 내선 번호표 **84번** = user32 `__ClientThreadSetup` (run85) → `ClientThreadSetup`.
7. **그다음에야** `NtUserGetThreadState` 본체가 돌아 `+0x16c` 에서 **1** 을 돌려준다.

**새 말 — ApiSet 표 간접 호출 (커널 쪽)**: win32kbase 가 win32kfull 의 함수를 직접 이름으로 부르지 않고, 함수 주소가 적힌 **표의 칸** (`+0x4e8`) 을 거쳐 부르는 것. 그 칸이 `xxxClientThreadSetup` 이라는 건 win32kfull refs 로 확인했다 (결과서). 비유: 옆 부서 내선 목록표를 보고 거는 전화.

#### 되부름 안에서 user32 가 하는 일 (이번에 다시 본 것) `[정적]`

`ClientThreadSetup` (rva `0x4f810`) 은 run85 에서 이미 봤다. 이번에 중요해진 줄만 (run 문서 나무에서 옮김 — 함수를 부르는 줄의 숫자는 ret 자리, `+0xca` · `+0xd3` · `+0x2d5 ~ +0x324` 는 함수 안 줄):

- `+0x9a` `GdiProcessSetup` (첫 GUI 스레드일 때, run86 A12).
- `+0xca` TEB `+0x800` 비트 3 으로 "이미 함" 표시 확인 · `+0xd3` TEB `+0x910` ← 2.
- `+0x2a3` `NtUserGetThreadDesktop` → `+0x2b0 gfSystemInitialized`.
- ★ `+0x2d5 ~ +0x324` — **TEB `+0x820` 이 0 이면** `pUserHeap` 에서 0 으로 채운 `0x48` B 를 빌려 (`+0x2f8` `RtlAllocateHeap(pUserHeap, 8, 0x48)`) 전역 `pdiLocal` 에 두고 TEB `+0x820` 에 넣는다.

**새 말 — `tagDESKTOPINFO` (TEB `+0x820`)**: user32 전역 `pdiLocal` 의 PDB 꾸민 이름이 `?pdiLocal@@3PEAUtagDESKTOPINFO@@EA` 라서, 이 칸은 **데스크톱 정보 구조체를 가리키는 포인터** 다 `[정적: PDB 공개 이름]` (결과서는 이름을 `[추측]` 으로 뒀고 run 문서가 받쳤다). 비유: 직원이 일하는 **방 (데스크톱) 의 정보 카드**. 이 카드가 2.8 의 벽 하나가 된다.

#### TEB `+0x78` 에 들어가는 값 — 포인터가 아니다?

- 쓰는 줄 `+0x756` 의 값은 `PsGetThreadId` 의 반환이다 `[정적: 가져오기 칸 rva 0x2d0268 의 이름]`.
- 그래서 **스레드 ID 로 보인다** `[추측 — 포인터 (pti) 가 아님]`.
- 요청서는 이 칸을 `Win32ThreadInfo` (포인터) 로 불렀다. run 문서 정정 3 이 이걸 바로잡았다.
- 어쨌든 user32 는 이 칸이 **0 인지만** 본다 `[정적]`. 값의 정체는 Q273 (동적) 에서 진짜 GUI 스레드 TEB 를 보고 닫는다.

#### 정리 — 대답이 아니라 "누른 것" 이 목적

| | 무엇 | 태그 |
|---|---|---|
| 대답 | 늘 1 (칸 안 읽음) | `[정적]` |
| 진짜 일 | 커널이 GUI 스레드로 바꿈 → TEB `+0x78` 쓰기 → 84번 되부름 → user32 `ClientThreadSetup` (TEB `+0x820` 등) | `[정적]` (커널 `PsConvertToGuiThread` 안은 이름만) |
| 그래서 문지기의 뜻 | "이 직원, 아직 그래픽 손님 등록 안 됐네? → 등록 번호 한 번 눌러서 등록시키고 와" | 비유 |

비유 (run85 2.12 를 이어서): 문지기는 안내판 칸이 비어 있으면 그래픽 부서 자동응답 번호를 누른다. 대답은 "네" 뿐이다. 하지만 관리사무소는 **그 번호가 눌린 순간** 이 직원을 그래픽 손님으로 등록하고, 안내판에 등록 표시를 적어 주고, 내선 84번으로 창 담당 건물 사람을 불러 "이 직원 방 정보 카드 좀 만들어 주세요" 한다. 문지기는 "네" 만 듣고 문을 연다.

### 2.7 ★ 에뮬레이터의 0 대답 = 막힘의 직접 원인 — 단계별로

1. 근거: 에뮬레이터는 `NtUserGetThreadState(0xe)` 에 기본 대답 **0** 을 준다 (원 요청).
2. 근거: 에뮬레이터 TEB `+0x78` 은 0 이다 (되부름 안 함 — 원 요청). → 문지기 넷 모두 걸음 4 로 간다.
3. 근거: 문지기는 대답이 0 이면 **실패로 나간다** `[정적]` (2.3).
4. 그래서: `MessageBoxWorker +0x73` → 0, `InternalCreateDialog +0xd4e` → NULL → −1, `LoadIcoCur +0x256` → 0, `RealGetSystemMetrics +0x131` → 0.
5. 이게 로그의 "`NtUserGetThreadState(0xe)` 하나뿐 · 반환 0 → 바로 실패" 와 **정확히** 맞는다 (결과서 "로그와 코드가 일치").
6. 그러니 고칠 자리는 **하나**: win32u `NtUserGetThreadState` (`0x1000`) 인자 `ECX == 0xe` 일 때 `RAX = 1`. 이것 하나로 넷 모두 문지기를 지난다.

같이 하면 좋은 것 (run 문서 "PE_emulator 쪽"):

- **TEB `+0x78` 을 0 아닌 값으로** (스레드 ID 꼴 `[추측]`) — 안 해도 길은 안 막힌다 (대답 1 이면 다시 안 읽으니까). 하지만 0 으로 두면 85곳에서 syscall 이 계속 나간다. 예: `GetDpiForSystem` 아래 `IsThreadDesktopComposited` 처럼 메시지 상자 한 번에도 여러 번.
- **되부름 (`__ClientThreadSetup`)** — 문지기 통과엔 **필요 없다**. 다만 되부름이 해 주던 일 하나 (TEB `+0x820` ≠ 0) 가 바로 다음 벽이 된다 → 2.8.

비유: 에뮬레이터의 그래픽 부서 자동응답은 "아니요 (0)" 라고 녹음돼 있었다. 문지기 넷은 그 말만 듣고 문을 닫았다.

### 2.8 ★ 1 로만 고치면 — 바로 뒤 벽 셋 (그리고 GDI) `[정적]`

문지기를 지나도 **상자는 아직 안 뜬다.** `MessageBoxW` 는 `MessageBoxWorker +0x349` 에서 `SoftModalMessageBox` (rva `0x21f20`) 로 들어가 상자를 그릴 준비를 하는데, 거기서 0 이면 안 되는 칸들을 만난다.

**새 말 — 접근 위반 · 0 나눔**: 둘 다 "조용히 실패" 가 아니라 **그 자리에서 넘어지는** 것. 접근 위반 = 0 같은 엉뚱한 주소를 읽으려다 넘어짐, 0 나눔 = 0 으로 나누려다 넘어짐. 비유: 빈 칸을 보고 "실패" 라고 적는 게 아니라, 빈 칸을 밟다 발이 빠지는 것.

코드 자리 순서로 (run 문서 3절 표 차례):

| 차례 | 자리 | 무엇을 읽나 | 0 이면 | 종류 |
|---|---|---|---|---|
| 5 | `SoftModalMessageBox +0xf5` · `+0x63f` `NtUserGetDCEx(0, 0, 3)` (`0x108a`) | 화면 그리기 판 (DC) | 한 번 더 → 또 0 이면 **`MessageBoxW` 0** | GDI 답 (조용한 0) |
| 7 | `+0x114 GetMessageBoxFontForDpi` → `+0xa6 IDIV R8D` | **`psi+0x1b56`** (word) | **0 나눔** | 벽 ① |
| 8 | `+0x149 UserGetCharDimensionsEx` → `+0x153 JZ` | 글자 폭 (gdi32) | **`MessageBoxW` 0** | GDI 답 (조용한 0) |
| 10 | `+0x1fa GetDialogMonitor(NULL, 1)` → `GetPrimaryMonitor +0x7 MOV RCX,[RAX+8]` | **`gSharedInfo+0x18`** (`pDispInfo`, user32 rva `0xda368`) | **접근 위반** | 벽 ② |
| 13 | `+0x372 / +0x385 MOV EBX,[RCX+0x30]` | **TEB `+0x820`** 가 가리키는 곳의 `+0x30` (널 검사 없음) | **접근 위반** | 벽 ③ |

**새 말 — DC (디바이스 컨텍스트)**: 그림을 그릴 때 쓰는 **그리기 판** 손잡이 `[추측: 일반적으로 알려진 설명]`. 비유: 그림 도구 진열장 (run86) 에서 빌려 오는 **도화지 + 붓 세트**.

벽마다 한 줄씩:

- **벽 ① `psi+0x1b56`** — 서버 정보판의 이 칸은 글꼴 크기 계산에서 **나누는 수** 로 쓰인다. run85 의 "0 으로 채운 `psi`" 면 여기가 0 이라 넘어진다. 자연값은 96 (`0x60`) 일 것 `[추측]`, `+0x1b66` 도 같은 값.
- **벽 ② `gSharedInfo+0x18`** — 공용 안내 묶음의 "모니터 안내" 칸. 0 이면 `GetPrimaryMonitor +0x7` 에서 넘어진다. (그 칸은 있는데 가리키는 곳의 모니터 포인터가 0 이면 `GetPrimaryMonitor` 는 0 을 돌려준다 — 그때 넘어지는 건 그다음 `GetMonitorWorkRect` 쪽 `[추측: 그 함수의 널 검사 안 봄]`.)
- **벽 ③ TEB `+0x820`** — 방 정보 카드. **원래는 되부름 (`ClientThreadSetup +0x2d5 ~ +0x324`) 이 채우는 칸** 이다 (2.6). 에뮬레이터는 되부름을 안 하니 0 그대로다.
  - 고치는 법 (run 문서): `KeUserModeCallback` 을 안 만들 거면 user32 와 같은 **0 채운 `0x48` B** 를 직접 TEB `+0x820` 에 넣는다. user32 가 만든 것도 0 채움이라 똑같다.
  - 참고: `IsThreadDesktopComposited` 는 이 칸이 0 이어도 견딘다 (널 검사 있음). 견디지 못하는 건 `SoftModalMessageBox` 와 `GetDesktopWindow` 다.
- **GDI 답** — DC · 글꼴 · 글자 폭이 0 이면 상자는 넘어지지 않고 **조용히 0** 을 돌려준다 (`+0x107` · `+0x153`). 이 답들은 **Q270 (sast/141)** 과 맞춰야 한다.

#### 왜 "1 로만 고치면 바로 벽" 인가 — 단계별로

1. 문지기는 대답만 본다 → 1 이면 통과 (2.3).
2. 그런데 진짜 Windows 에서 그 syscall 은 **부수 효과로 TEB `+0x820` 까지 채워 놓는다** (되부름, 2.6).
3. 에뮬레이터는 대답만 1 로 흉내 내고 부수 효과는 안 한다 → TEB `+0x820` 은 0.
4. 그래서 문은 열리지만, 들어가서 방 정보 카드를 찾는 순간 (`SoftModalMessageBox +0x385`) 넘어진다.
5. 나머지 두 벽 (`psi+0x1b56` · `gSharedInfo+0x18`) 은 되부름과 무관하게, run85 에서 "0 채움이면 된다" 고 한 공용 안내 묶음 · 서버 정보판이 **창 API 길에선 모자라서** 생긴다 → 2.10.

비유: 자동응답을 "네" 로 바꾸면 문지기는 문을 연다. 그런데 진짜 관리사무소라면 그 전화 한 통에 방 정보 카드까지 만들어 줬을 것이다. 우리 흉내는 "네" 만 하니까, 직원은 들어가서 카드를 찾다가 넘어진다. 거기에 서버 정보판의 "기준 글자 크기" 칸과 안내 묶음의 "모니터 안내" 칸도 비어 있다.

#### 다른 셋은?

- `LoadIconW` 만이면: 1 + `NtUserFindExistingCursorIcon` (`0x1039`) 이 0 아닌 핸들 (자원 읽기 건너뛰기).
- `GetSystemMetrics` 만이면: 1 + `psi` 비트 `0x10` 꺼짐 + 지표 표 (`psi+0x768`).
  - 에뮬레이터가 본 `GetSystemMetrics(0x1000)` 의 0 은 문지기를 지나도 **맞는 값일 가능성이 높다** `[추측]` — 지표 `0x1000` 은 서비스 세션과 지금 세션이 같으면 0 (`RealGetSystemMetrics +0x233 ~ +0x271`).
- `DialogBoxParamW` 는 1 + 메시지 상자와 같은 벽 (TEB `+0x820` · `gSharedInfo+0x18` · GDI · 창 만들기). 같은 `InternalCreateDialog` 를 쓰기 때문이다.

### 2.9 ★ 메시지 상자가 끝나려면 — 누군가 단추 메시지를 넣어야 한다 `[정적]`

벽을 다 넘어 상자가 떴다고 하자. 그럼 `MessageBoxW` 는 언제 돌아올까?

**새 말 — 모달 고리**: 상자가 떠 있는 동안 그 스레드가 **다른 일 없이 계속 도는 "메시지 받기" 고리**. 상자가 닫혀야 고리가 끝나고, 그제야 `MessageBoxW` 가 돌아온다. 비유: 알림판 앞에 서서 **누가 단추를 누를 때까지** 계속 기다리는 직원.

**새 말 — 창 메시지 (`WM_*`)**: 창 앞으로 오는 알림 번호. 클릭 · 키 · 타이머 같은 일이 번호로 바뀌어 창에 온다 `[추측: 일반적으로 알려진 설명]`. 이번에 나오는 번호: `WM_INITDIALOG` (`0x110`), `WM_COMMAND` (`0x111`), `WM_TIMER` (`0x113`), `WM_QUIT` (`0x12`). 비유: 창 앞으로 오는 **알림 카드**.

#### 고리 한 바퀴

`InternalDialogBox +0x8a` → `DialogBox2` (rva `0x11154`):

1. `+0x26b _PeekMessage` → `NtUserPeekMessage` (`0x1001`) — 온 메시지가 있나?
2. 없으면 `+0x23f NtUserWaitMessage` (`0x100a`) 에서 **잠든다**.
3. 있으면 `+0x29b IsDialogMessageW` → `+0x2a9 TranslateMessage` → `+0x2b5 DispatchMessageWorker` — 창 함수에게 넘긴다.
4. 매 바퀴 "끝 표시" 를 본다 (`+0x12a ~ +0x142`). 켜져 있으면 빠져나가 `NtUserDestroyWindow` (`+0x3c4`) 뒤 결과를 돌려준다.

#### 정상 끝 = `EndDialog` 하나뿐

- `EndDialog` (rva `0x219e0`) 가 하는 일:
  - `+0x70` 대화상자 자료 `+0x18` 에 **끝 표시** (`|= 1`)
  - `+0x7f` 자료 `+0x20` 에 **결과** (누른 단추 번호)
  - `+0xa9` `NtUserSetWindowPos` (숨기기) → `+0xd9` `NtUserPostMessage` (잠든 고리 깨우기)
- **`EndDialog` 를 부르는 건 `MB_DlgProc`** (rva `0x50ab0`, 메시지 상자의 창 함수) 뿐이다. 두 갈래:
  - **`WM_COMMAND` (`0x111`)** — 단추 번호 3 ~ 7 은 곧장 `EndDialog(hwnd, 번호)`. 1 · 2 는 그 번호 단추가 있을 때만. `MB_OK` 상자의 단추는 `IDOK` = 1 하나 (결과서).
  - **`WM_TIMER` (`0x113`)** — 타임아웃 (`0x7d00`). 그런데 **`MessageBoxW` 는 타이머를 안 건다** (타임아웃 −1 — `MB_DlgProc +0x1d4 NtUserSetTimer` 는 타임아웃이 있을 때만).

#### 단계별로 — "누군가 넣어야 한다"

1. 근거: 고리가 정상으로 끝나는 길은 `EndDialog` 의 끝 표시뿐이다 `[정적]`.
2. 근거: `EndDialog` 를 부르는 건 `MB_DlgProc` 의 `WM_COMMAND` · `WM_TIMER` 갈래뿐이다 `[정적]`.
3. 근거: `MessageBoxW` 는 타이머를 안 건다 → `WM_TIMER` 는 안 온다 `[정적]`.
4. 그래서 남는 건 **`WM_COMMAND` (단추)** 하나다.
5. 진짜 Windows 에선 그 메시지를 사람이 만든다 — 단추 클릭 · Enter · Esc 가 커널에서 메시지가 되어 `NtUserPeekMessage` 로 들어온다 (run 문서).
6. 에뮬레이터엔 사람이 없다. 그러니 **`NtUserPeekMessage` 의 대답이 유일한 입구** 다.

#### 다른 끝 — `WM_QUIT`

- 고리가 `WM_QUIT` (`0x12`) 를 받으면 (`DialogBox2 +0x280 CMP EAX,0x12` · `+0x283 JZ → +0x379`) `NtUserPostQuitMessage` 로 다시 걸어 두고 **`EndDialog` 없이** 빠져나온다.
- 결과 칸 (`+0x20`) 이 처음 값 0 이라 **`MessageBoxW` 는 0** 을 돌려준다 (성공 `IDOK` 가 아님).

#### 그래서 남은 결정 — 정책

| `NtUserPeekMessage` 가 줄 것 | `MessageBoxW` 반환 | 뜻 |
|---|---|---|
| `WM_COMMAND(IDOK)` | **1** | "사용자가 확인을 눌렀다" |
| `WM_QUIT` | **0** | "상자가 정상으로 안 닫혔다" |

- 둘 중 무엇을 줄지는 **사용자 정책** 이다 (`335_tune15.md` Q5). 코드가 정하는 게 아니다.

비유: 알림판은 **누군가 단추를 눌러야** 치워진다. 진짜 가게에선 손님이 누른다. 우리 에뮬레이터 가게엔 손님이 없으니, 관리사무소가 "누가 확인 눌렀어요" 카드를 넣어 줄지, "가게 문 닫아요" 카드를 넣어 줄지 정해야 한다. 아무것도 안 넣으면 직원은 알림판 앞에서 계속 기다린다.

### 2.10 서버 정보판 (`psi`) — 크기는 맞고, 값이 문제 `[정적]`

run85 는 "`psi` 는 0 채운 `0x1b68` B 면 `DllMain` 끝까지 간다" 고 했다. 이번에 보탠 것:

- **크기는 맞다.** 지금까지 본 칸 중 가장 뒤가 `+0x1b66` (word) 라서, `0x1b68` B 면 다 들어간다.
- **하지만 창 API 길엔 0 이면 안 되는 칸이 생긴다.** 대표만:

| 칸 | 비어 있으면 |
|---|---|
| `psi+0x1b56` (word) | **0 나눔** (벽 ①) |
| `psi+0x3a4 + 40*k` (단추 글) | 빈 글 단추 (상자는 뜸) |
| `psi+0x768 + 4*지표` (`GetSystemMetrics` 값 표) | 지표 전부 0 |
| `psi+0x8ec + …` (DPI 의존 지표) | 크기 0 |
| `gSharedInfo+0x18` (모니터 안내) | **접근 위반** (벽 ②) |

- 칸 **뜻** 은 `[추측]`, 오프셋과 읽는 줄은 `[정적]` 이다. 전체 표는 run 문서 5절.
- 진짜 값은 동적 Q273 (`psi` 앞 `0x1c00` B 덤프) 에서 본다.

비유: run85 에서 "서버 정보판이 **백지** 여도 개점 준비는 된다" 고 했다. 그건 맞다. 그런데 손님 앞에 알림판을 세우려면 백지로는 안 되는 줄이 몇 개 있다 — 기준 글자 크기, 모니터 안내, 단추 글.

### 2.11 PE_emulator 판정 — 맞음 · 틀림 (run 문서 "PE_emulator 쪽")

`../PE_emulator` 코드는 이번에 안 읽었다. 고칠지는 사용자 결정이다.

| 지금 에뮬레이터 | 판정 |
|---|---|
| `NtUserGetThreadState(0xe)` 기본 대답 0 | **틀림** — 넷이 실패하는 직접 원인. 정상은 1 |
| TEB `+0x78` 0 그대로 | **문지기엔 괜찮음** (대답 1 이면 다시 안 읽음). 다만 syscall 이 계속 나감 |
| 되부름 (`__ClientThreadSetup`) 안 함 | **문지기 통과엔 필요 없음.** 하지만 TEB `+0x820` 은 직접 채워야 함 |
| `psi` 0 채운 `0x1b68` B | **`DllMain` 까지 맞음, 창 API 길엔 모자람** |
| `GetSystemMetrics(0x1000)` 0 | **문지기 뒤에도 맞을 가능성 높음** `[추측]` |

메시지 상자 한 장이 뜨고 닫히기까지 필요한 것 (run 문서 목록 1 ~ 8 을 묶음):

1. `0x1000` 인자 `0xe` → **1** (이것만으로 문지기 넷 통과)
2. (좋음) TEB `+0x78` 0 아닌 값
3. TEB `+0x820` ≠ 0 — 0 채운 `0x48` B 이상
4. `psi+0x1b56` ≠ 0 (`+0x1b66` 같은 값)
5. `gSharedInfo+0x18` ≠ 0, 그 `+0x8` 이 주 모니터 물체
6. GDI 답 (DC · 글꼴 · 글자 폭) — Q270 과 맞춤
7. `NtUserFindExistingCursorIcon` 0 아님 · `NtUserCreateWindowEx` 가 `ValidateHwnd` 를 통과하는 hwnd
8. 고리 끝 입구 — `NtUserPeekMessage` 가 무엇을 줄지 (**정책**)

### 2.12 앞 run 고쳐 읽기 — 닫힘과 바뀐 곳

run 문서가 각 문서 맨 위에 "고쳐 읽기 (run142)" 한 줄을 넣었다.

| 앞 run | 전에 | 이제 |
|---|---|---|
| run85 | TEB `+0x78` 쓰는 쪽은 win32k `[추측]` | **`[정적]`** win32kbase `xxxCreateThreadInfo +0x756` (값은 스레드 ID 로 보임 `[추측]`). 되부름 자리도 `[정적]`: `+0x137b` ⇢ `xxxClientThreadSetup +0x4c KeUserModeCallback(0x54)` |
| run85 | `psi` 0 채운 `0x1b68` B 면 길 끝까지 | `DllMain` 까지는 그대로 맞음. 창 API 길엔 0 이면 안 되는 칸이 생김 (2.10) |
| run86 | 표 A13 의 답 "아무 값" | 정상 답은 **상수 1**. `GetAppCompatFlags2` 는 0 이어도 넘어가지만, 창 API 넷은 실패한다 |
| run86 | TEB `+0x78` "포인터 — `NtGdiInit2` 가 GUI 로 바꾼 뒤 0 아님 `[추측]`" | 쓰는 곳 `[정적]` (위와 같음), 값은 포인터가 아닌 스레드 ID 로 보임 `[추측]` |
| run121 | ① `Win32ThreadInfo` 를 유저 TEB 에 놓는 줄 `[공백]` | **닫힘** `[정적]` `+0x745 / +0x756`. 짐 지도 3.3 `+0x78` 줄은 짐 지도 바퀴에서 고칠 것 (Q264 에 보탬) |

- 요청서 · 원문의 짐작 정정 넷 (run 문서 "정정"): ① 같은 도우미 없음 (2.3) ② 기본 대답 0 이 아니라 정상은 1 (2.5) ③ TEB `+0x78` 은 포인터가 아님 (2.6) ④ `LoadIconW` 의 syscall 0 개 0 은 문지기가 아님 (2.4 다).

### 2.13 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음, 짧게)

run 문서 "flow-writer 대조" 에서, 이 글을 읽을 때 걸릴 만한 것만:

- 결과서는 `GetDpiDependentMetric` 자리를 `+0x1b6 · +0x1d7 · …` 로 적었는데, 그 자리는 **`GetDpiForSystem` call** 이다. `GetDpiDependentMetric` call 은 바로 뒤 `+0x1c2 · +0x1e3 · +0x261 · +0x3ba · +0x4e6`. 짝마다 `GetDpiForSystem` 이 먼저 불려 그 안 `IsThreadDesktopComposited` 문지기도 그만큼 탄다.
- `MB_DlgProc` 메시지 `0x136` 은 `WM_CTLCOLORDLG` 다 `[추측: 일반 지식 — 코드엔 숫자만]` (결과서는 `WM_CTLCOLORMSGBOX`). 그리고 `WM_COMMAND` 의 7 (`IDNO`) 도 3 ~ 6 과 같이 곧장 `EndDialog` 로 간다 (결과서는 "갈래 따로"). 이 글은 run 문서를 따랐다.
- `xxxCreateThreadInfo +0x756` 쓰기는 지역 TEB 값이 0 이 아닐 때만 (`+0x738 ~ +0x740`).
- `GetDesktopWindow` (rva `0xf890`) 의 PDB 공개 이름은 `GetCurrentThreadDesktopHwnd` 다 (같은 주소 이름 2개). 문서는 내보내기 이름을 쓴다.
- `NtUserEnsureDpiServerInfoForDpi` 는 IAT 직접이 아니라 `CallWin32uFunction<…>` (rva `0x7a830`) 껍데기를 거친다 — 그 함수가 없으면 syscall 이 안 나갈 수 있다 `[추측: 안 읽음]`.

### 2.14 정리 — 비유로 한 번 더

1. 창 담당 건물엔 문이 넷 (`MessageBox` · `DialogBox` · `LoadIcon` · `GetSystemMetrics`) 있고, **문마다 똑같이 생긴 문지기** 가 따로 서 있다. 공통 경비실은 없다.
2. 문지기는 직원 안내판의 "그래픽 손님 등록" 칸을 보고, 비었으면 그래픽 부서 자동응답 번호를 누른다.
3. 자동응답은 늘 **"네 (1)"** 다. 진짜 일은 번호가 눌린 순간 관리사무소가 하는 **등록** — 안내판에 등록 표시, 내선 84번으로 창 담당 건물 사람을 불러 **방 정보 카드** 만들기.
4. 에뮬레이터 자동응답은 **"아니요 (0)"** 로 녹음돼 있어서, 문지기 넷이 모두 문을 닫았다.
5. "네" 로 바꾸면 문은 열린다. 하지만 등록 (부수 효과) 은 흉내 내지 않았으니 **방 정보 카드** 가 없고, 서버 정보판의 **기준 글자 크기**, 안내 묶음의 **모니터 안내** 도 비어 있어 들어가자마자 넘어진다. 도화지 · 붓 (GDI) 도 받아야 한다.
6. 다 갖춰 알림판을 세우면, 이번엔 **누가 단추를 눌러야** 치워진다. 손님이 없는 우리 가게에선 "확인 눌렀어요" 카드를 넣을지 "문 닫아요" 카드를 넣을지 **사용자가 정한다.**

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 1363f6a -- runs/03_sprint/flow.md`)

| 자리 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp · user32 · win32u · win32kbase · win32kfull 줄에 run142 없음 | 각 줄에 run142 보탬 + 근거 줄 (run142) 한 줄 |
| 3장 user32 시작 함수 절 — GUI 스레드 줄 | "TEB `+0x78` 쓰는 쪽은 win32k `[추측]`" | 그대로 두고 괄호로 "(run142: 쓰는 쪽 `[정적]` = win32kbase `xxxCreateThreadInfo +0x756` …, 되부름 자리 `+0x137b` ⇢ `xxxClientThreadSetup +0x4c KeUserModeCallback(0x54)`)" |
| 3장 gdi32 하위 절 | `GetAppCompatFlags2` 의 `NtUserGetThreadState(0xe)` 답 = 아무 값 | 한 줄 보탬: 정상 답은 **상수 1** `[정적]`, 0 이면 창 API 넷이 실패 |
| **3장 새 하위 절** | — | "**창 API 넷의 연결 가드 · `NtUserGetThreadState(0xe)` · 메시지 상자 한 장의 최소 길**" — 비유 · 트리 · `[정적]` 다섯 (문지기 같은 꼴 · 종류 `0xe` = 1 · TEB `+0x820` = `tagDESKTOPINFO *` · `psi` 값 칸 · 모달 고리 끝) |
| 4장 | run85 · run86 · run121 · run131 줄 | 각 줄에 run142 표시 (일부 닫힘), **Q268 해결됨** 줄 하나, **run142 새로** 줄 셋 (동적 Q273 · 정적 Q274 · PE_emulator 쪽) |
| 5장 | — | run142 두 줄 |

- **그림 (1장) 은 안 바뀌었다.** test.exe 초기화 밖 이야기라서다.
- 짐 지도 (`baggage.md`) 도 안 바뀌었다. run121 이 남긴 짐 지도 3.3 `+0x78` 줄 ("놓는 줄 못 찾음") 은 **짐 지도 바퀴에서 고칠 것** 으로 Q264 에 보탰다.

### 큰 그림에서 어떻게 이어지나

- **run85 → run86 → run142 가 한 줄로 이어졌다.**
  - run85: user32 전원 켜기 (`DllMain`) — 공용 안내 묶음을 받아 온다. "GUI 스레드는 첫 win32k syscall 때 바뀐다" 까지.
  - run86: 그림 도구 건물 전원 켜기 — 첫 win32k syscall 은 `NtGdiInit2`. 그 끝의 `GetAppCompatFlags2` 가 문지기 꼴의 첫 사례였다.
  - run142: 프로그램이 처음 창 API 를 부를 때 — 그 문지기가 **user32 전체 85곳** 에 있고, 대답은 1, 진짜 목적은 GUI 스레드 변환 + 되부름 (TEB `+0x78` · TEB `+0x820`) 이라는 것까지.
- **PE_emulator 쪽으로는**: 고칠 첫 자리 (`0x1000` / `0xe` → 1) 가 하나로 좁혀졌고, 그 뒤 벽 셋 + GDI + 정책 하나가 목록으로 나왔다. GDI 쪽은 바로 다음 바퀴 **Q270 (sast/141)** 이 받는다.
- **아직 그림이 다 그려진 건 아니다.** 창 만들기 안쪽 (`InternalCreateDialog` 본문 · 창 물체 칸) 은 틀만 있다 → Q274.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q273** (동적, Q162 세션과 합침 — 미룸)

test.exe 는 user32 를 안 올린다. user32 를 올리는 프로세스 (notepad 류 · 새 GUI 샘플 — 고르기는 사용자 결정) 가 있어야 본다.

| 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|
| 진짜 GUI 스레드의 TEB `+0x78` 이 **스레드 ID 와 같은가** · TEB `+0x800 ~ +0x8f8` 값 (`+0x810` · `+0x820` · `+0x828` · `+0x840` · `+0x8e8` 등) | 정적은 "무엇을 쓰나" 까지. 값은 실행 때 |
| `gSharedInfo` (`0x240` B) · `psi` 앞 `0x1c00` B 의 진짜 값 (`+0x1b56` 이 96 인가 등) | 같음 — 값은 실행 때 |
| 첫 `NtUserGetThreadState(0xe)` 앞뒤로 TEB `+0x78` 이 0 → ≠ 0 으로 바뀌나, 반환이 1 인가, 그 사이 `__ClientThreadSetup` 이 서나 (콜스택에 `xxxCreateThreadInfo+0x1380` 이 보이나) | 커널 가운데 (`PsConvertToGuiThread` → `W32pThreadCallout`) 를 안 읽었고, 콜스택으로 본 적 없음 |
| 메시지 상자 한 번의 syscall 수 (`NtUserCreateWindowEx` 몇 번, `NtUserMessageCall` 이 나오나) | 같은 스레드 창이면 user32 안에서 풀리는 호출이 있어 정적으론 셀 수 없음 |
| `LoadIconW` 의 syscall 0 개 0 이 `LoadIcoCur +0x290` 길인가 (그때 TEB `+0x810`) | 에뮬레이터 로그 seq 2766 ~ 2768 과 값 대조가 필요 |
| 부모 있는 `DialogBoxParamW` 에서 `NtUserGetThreadState(0xe)` 가 둘인가 | 부모 인자 값을 못 봄 |

### 정적으로 남은 것 → **Q274**

- **N1** `InternalCreateDialog` 메시지 상자 갈래 전체 (4144 B, 이번엔 틀만) — 최소 길 표 19 ~ 27 의 `[공백]` (컨트롤 수 · 보이기 조건).
- **N2** 창 물체 사본 (`tagWND`) 칸 표 — 에뮬레이터가 `ValidateHwnd` 를 통과하는 hwnd 를 만들려면 필요.
- **N3** win32kbase `xxxCreateThreadInfo` 본문 (5365 B) — TEB `+0x820` · `+0x828` 을 **언제 누가** 채우나, 데스크톱 없는 프로세스에서의 모양.
- **N5** `GetSystemMetrics` 지표 번호별 읽는 칸 · `LoadIcoCur` 자원 길.
- `CallWin32uFunction<…>` (rva `0x7a830`) 껍데기 안.

### GDI 쪽 → **Q270** (sast/141 `gdi-startup`, 다음 바퀴)

- 결과서 N4: `NtUserGetDCEx` 가 돌려준 DC 를 gdi32 가 어떻게 읽나 (PEB `+0xf8`), 글꼴 · 글자 폭 답. 벽 표의 "GDI 답" 두 줄이 여기서 닫힌다.

### 짐 지도 → **Q264** (사용자 결정 필요)

- 짐 지도 3.3 TEB `+0x78` 줄 "놓는 줄 못 찾음" 을 run142 근거로 고치는 것. 짐 지도 바퀴에서.

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- `PsConvertToGuiThread` → `W32pThreadCallout` 사이 줄 (커널 안 — 안 읽음) `[추측]`.
- 대화상자 자료가 어느 힙에 있나 (`EndDialog` 가 쓰므로 쓰기 가능해야 함) `[추측]`.
- `psi+0x1b56` 의 자연값 96 `[추측]`.
- 메시지 상자에 "누가 답하나" — **정책 (사용자 몫, `335_tune15.md` Q5)**.

---

## 5. 더 읽기

- run 문서: [`run142-user32-connect.md`](run142-user32-connect.md)
  - "결과" 1절 (가드 표) · 2절 (종류 `0xe` 와 GUI 변환 나무) · 3절 (최소 길 표 0 ~ 30) · 4절 (모달 고리 메시지 표) · 5절 (공유 칸 표) · "PE_emulator 쪽" · "정정" · "고쳐 읽기" · "flow-writer 대조"
- 요청서: [`_workspace/sast/139_user32-connect.md`](../../_workspace/sast/139_user32-connect.md)
- 결과서: [`_workspace/sast/139_user32-connect_result.md`](../../_workspace/sast/139_user32-connect_result.md) — 표 (1) 가드 · (2) 최소 길 · (3) 종류 `0xe` · (4) 모달 고리 · (5) 공유 칸
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/335_internals-user32-connect.md`
- flow.md: [3장 — user32 시작 함수 절 · gdi32 하위 절 · "창 API 넷의 연결 가드 …" 하위 절](flow.md) · 4장 run142 줄 · 5장 run142
- 앞 해설:
  - [run85 해설](run85-user32-init-해설.md) 2.5 (공용 안내 묶음 · 서버 정보판) · 2.12 (GUI 스레드 · 내선 번호표 84번)
  - [run86 해설](run86-gdi32-init-해설.md) 2.11 (`GetAppCompatFlags2` · 표 A13)
  - [run131 해설](run131-user32-regclass-해설.md) (창 클래스 · 창 함수)
  - [run136 해설](run136-winsta-name-해설.md) (상가 건물 · 방)
  - [run56 해설](run56-syscall-args-해설.md) 2.9 (`KiSystemServiceStart` 의 win32k 갈래)

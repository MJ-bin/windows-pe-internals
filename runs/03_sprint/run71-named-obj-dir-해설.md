# run71 named-obj-dir — 해설

> 이 글은 [run71 문서](run71-named-obj-dir.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q136**: PE_emulator 요청 22, "kernelbase `BaseStaticServerData` 와 이름 있는 객체 폴더 길" (`_workspace/plan.md`, 이번에 **끝**)
  - 새로 생긴 할 일: **Q138** (정적, 맨 뒤). 4절에서 다룬다.
  - 동적 몫은 **Q104 VM 단계** 에 얹었다.
  - 다음 항목: **Q137** (run72, 정적은 이미 끝났다).
- 근거:
  - run 문서 [`run71-named-obj-dir.md`](run71-named-obj-dir.md) (가장 중요)
  - 요청서 [`_workspace/sast/66_named-obj-dir.md`](../../_workspace/sast/66_named-obj-dir.md)
  - 결과서 [`_workspace/sast/66_named-obj-dir_result.md`](../../_workspace/sast/66_named-obj-dir_result.md) (판정: **분석성공**)
  - flow.md 변경 (3절에서 짧게 다룸): 그림 2 새 상자들 · 2.5 새 절 · 2.8 새 절 · 2.10 새 절 · `bn1` 정정 · 4장 · 5장
- 이번 바퀴는 **정적만** 했다.
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 를 flow-writer 가 **다시 읽은 것** 이다. 새로 실행한 게 아니다.

이미 푼 말은 **링크만** 건다.

- csrss = 관리사무소와 따로 있는 **인사팀 사무실**. 서버 DLL = 사무실 안 **창구들**, 1번 창구 = basesrv → [run14 해설 2.6](../02_sprint/run14-create-process-internal-해설.md) · [run34 해설](../02_sprint/run34-parent-leftovers-해설.md)
- csrss 연결 = 가게가 인사팀 사무실에 처음 거는 **전화**. 사무실 게시판을 우리 땅에 한 판 펼치기, "사무실 번지 → 우리 번지" 옮겨 적기 → [run68 해설 2.4](run68-dll-init-rest-해설.md)
- 크리티컬 섹션 = 한 번에 한 명만 들어가는 **방의 열쇠** → [run68 해설 2.7](run68-dll-init-rest-해설.md)
- 섹션 = 창고의 **원본 물건**, 뷰 = 그걸 땅에 **펼쳐 놓은 한 판** → [run38 해설](../02_sprint/run38-map-module-해설.md)
- 핸들 = **손잡이** → [run26 해설](../02_sprint/run26-map-dll-file-해설.md)
- `UNICODE_STRING` = "몇 글자, 어디부터" 를 적은 **쪽지** → [run19 해설 2.1](../02_sprint/run19-proc-init-modules-해설.md)
- 사진 P1a · P1b · P2 → [run44 해설 2.8](run44-baggage-snap-해설.md)
- 앞선 멈춤 (`OutputDebugStringA → … → GlobalAlloc`, `BaseHeap = 0`) → [run67 해설 1절](run67-kb-heap-init-해설.md)

이번에 처음 나오는 말 — **뮤텍스**, **이름 있는 객체 (와 그 폴더)**, **공유 섹션** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, PEB = 가게 **안내판**, 커널 = **관리사무소**.
- KERNELBASE = 가게에 들인 **건물**, 그 전역 = 건물 **수첩**, 시작 함수 = 건물 **전원 켜기**.
- csrss = **인사팀 사무실**, basesrv = 그 사무실 **1번 창구**.
- run68 에서 본 사무실 **게시판** (`SharedSection`) 은 그대로 쓴다.
  - 관리사무소의 공용 게시판 (KUSD) 과는 **다른 판**이다. 이건 인사팀 사무실 게시판이다.
- 이번에 새로 붙는 그림 하나:
  - **블록 `BaseStaticServerData` (`0xa00`B)** = 그 게시판에 1번 창구가 붙여 둔 **안내문 한 장**.
  - 이번 질문은 거의 다 "그 안내문의 어느 줄을 누가 언제 읽나" 다.

### 한 줄로 먼저 — 핵심 줄기

1. 세션이 시작될 때 csrss 가 게시판 (공유 섹션) 을 만들고, 1번 창구가 그 위에 안내문 (블록 `0xa00`B) 을 붙인다 `[정적]`.
2. test.exe 가 csrss 에 전화를 걸면 (run68), 그 게시판 위치를 받아 자기 땅에 한 판 펼친다 `[정적]`.
3. kernelbase 가 안내문 위치를 "우리 번지" 로 옮겨서 수첩 `BaseStaticServerData` 에 적는다 `[정적]` (run68).
4. 나중에 이름 있는 객체 폴더가 처음 필요해지면, 폴더 이름을 그 안내문 **`+0x20` 줄** 에서 읽는다 `[정적]`.
   - PE_emulator 는 4번에서 멈췄다. 1 ~ 3 이 없어서 수첩 칸이 0 이었다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 한 자리가 아니다. **세 군데** 에 걸친다.

```
(그 전, 다른 프로세스) csrss 세션 시작 — 게시판·안내문 만들기          ← 이번 (2.4)

cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드
 │  첫 스레드 → ntdll!LdrInitializeThunk → LdrpInitializeProcess
 │    ├─ ●P1a  +0x199d  kernel32 들이기 + 전원 켜기
 │    │     ├─ KERNELBASE 시작 함수: csrss 전화 (run68), 수첩에 BaseStaticServerData
 │    │     └─ (어딘가에서) 이름 있는 객체 폴더를 처음 엶                   ← 이번 새로 드러남 (2.7)
 │    ├─ ●P1b  +0x1bb6  DLL 전원 켜기 — dll2 TLS 콜백 · DllMain 의 Log
 │    │     └─ OutputDebugStringA → DBWinMutex                              ← 이번 새로 드러남 (2.7)
 │    └─ ●P2   NtContinue 은퇴
 └─ … → exe 진입점 → main
                └─ Log → OutputDebugStringA → … → 블록 +0x20   ← PE_emulator 가 멈춘 곳 (2.3)
```

### 왜 이 질문이 지금 왔나

- run67 에서 PE_emulator 는 `main` 의 첫 `Log` → `OutputDebugStringA` 안에서 `BaseHeap = 0` 으로 멈췄다. 그건 고쳤다.
- 그랬더니 **같은 길에서 조금 더 가서** 또 멈췄다 (요청서):
  - `main +0x12` → `Log +0x1e0` → `OutputDebugStringA` (ret `+0xc2`) → `CreateDBWinMutex` (ret `+0x371`) → `BaseGetNamedObjectDirectory` (ret `+0xb0`) → `BasepGetNamedObjectDirectoryForToken +0x2b0` → `+0x2b7` — 여기서 `RAX = 0`.
- PE_emulator 엔 csrss 가 없다. 그러니 그 안내문을 **직접 지어 넣어야** 한다. 그러려면 모양과 "꼭 채울 줄" 을 알아야 한다.

### 무엇이 궁금했나

1. kernelbase 세 함수 (`CreateDBWinMutex` · `BaseGetNamedObjectDirectory` · `BasepGetNamedObjectDirectoryForToken`) 가 무엇을 읽고, 어떤 syscall 을 부르나.
2. 안내문 (블록) 은 어떻게 생겼고, 누가 언제 채우나. PE_emulator 가 지을 **최소 칸** 은 무엇인가.

### 바퀴 전에 알던 것

- `[정적]` (run68) `BaseStaticServerData = [PEB+0x98]+8 의 값 − PEB+0x380 + PEB+0x88`. `_KernelBaseBaseDllInitialize +0x32b` 에서 쓴다.
- `[관찰]` (run44 사진, run68 이 옮김) PEB 세 칸 값: `+0x88` = `0x7ff4ecf70000`, `+0x98` = `0x7ff4ecf70750`, `+0x380` = `0x7ff5575f0000`.
- `[정적]` (run68) "`BaseNlsDllInitialize` 가 `+0x178` 을 **읽는다**" 고 적어 뒀다 → 이번에 틀린 걸 알았다 (2.8).

---

## 2. 찾은 것

### 2.1 `OutputDebugStringA` 가 하는 일 — 그리고 새 말 "뮤텍스"

`OutputDebugStringA` 는 "디버거한테 이 글 좀 보여 줘" 하는 함수다. 순서는 이렇다 (`[정적]`, 착지 몸통은 objdump):

1. `+0x77 / +0x7c` **`RaiseException(0x40010006)`** — 먼저 "디버거 있어요?" 하고 손을 든다.
   - 디버거가 이 손을 잡아 주면 (예외를 받으면) 아래는 안 탄다.
   - 아무도 안 잡으면 `__except` 착지 몸통 (`+0x81 ~`) 으로 간다.
2. `+0xbd / +0xc2` **`CreateDBWinMutex`** — 뮤텍스를 만든다. **처음 한 번만** (전역 `[0x1803ad968]` 뮤텍스 핸들 · `[0x1803ad960]` 실패 깃발 둘 다 0 일 때).
3. `+0xfd / +0x102` `WaitForSingleObjectEx(뮤텍스, 10000, 0)` — 그 뮤텍스를 잡을 때까지 기다린다.
4. `+0x119 / +0x11e` `OpenFileMappingW(2, 0, L"DBWIN_BUFFER")` — 글을 받아 줄 프로그램이 있는지 본다.
5. 없으면 `+0x274 / +0x279` `DbgPrint("%s", …)` 로 끝낸다.

#### 새 말 — 뮤텍스

- **뮤텍스** = 크리티컬 섹션처럼 "한 번에 하나만" 을 지키는 열쇠인데, **관리사무소 (커널) 가 맡아 두는 열쇠** 다. 그래서 **이름을 붙여** 다른 프로세스와 같이 쓸 수 있다 `[추측]` (일반적으로 알려진 설명).
- 비유를 이어 쓰면:
  - 크리티컬 섹션 (run68) = **우리 가게 안** 방 열쇠. 우리 가게 사람끼리만 쓴다.
  - 뮤텍스 = 관리사무소에 맡긴 **동네 공용 방 열쇠**. 이름표 (`DBWinMutex`) 가 달려 있어서, 다른 가게도 같은 이름으로 같은 열쇠를 찾는다.
- 왜 `OutputDebugStringA` 에 이게 필요하나 — 여러 프로그램이 동시에 디버그 글을 써도 **한 줄씩 차례로** 들어가게 하려고 그럴 것이다 `[추측]` (일반적으로 알려진 설명).

### 2.2 이름표 붙은 열쇠는 어디 걸려 있나 — 새 말 "이름 있는 객체"와 그 폴더

#### 새 말 — 이름 있는 객체

- **이름 있는 객체** = 관리사무소가 맡아 둔 물건 (뮤텍스·이벤트·세마포어 등) 중 **이름이 붙은 것**. 이름으로 찾아서 여러 프로세스가 같이 쓴다 `[추측]` (일반적으로 알려진 설명).
- 비유: **이름표 붙은 공용 사물함.** 관리사무소 안에 있고, 이름만 알면 어느 가게든 열 수 있다.
- 이 사물함들은 **폴더** 에 모여 있다. test.exe 쪽 폴더 이름은 `\Sessions\1\BaseNamedObjects` 다 (2.3, 2.4).
  - 비유: 사물함이 모인 **방**. "1번 세션 사물함 방".

#### 열쇠를 만들려면 먼저 방 손잡이가 있어야 한다

`CreateDBWinMutex` 는 `DBWinMutex` 를 "그 폴더 **안에**" 만든다. 그래서 먼저 폴더 손잡이 (핸들) 를 구한다 (`[정적]`):

```
CreateDBWinMutex
 ├─ SID 4 · DACL 3 · SACL (낮은 무결성 쓰기 금지)   누가 열 수 있나 (자물쇠 규칙)
 ├─ +0x371  BaseGetNamedObjectDirectory   ← 방 손잡이 구하기
 └─ +0x3cf  NtCreateMutant(0x2120001, Root = 폴더, "DBWinMutex")
```

`BaseGetNamedObjectDirectory` 는 **수첩에 적어 둔 손잡이** 부터 본다 (`[정적]`):

- 수첩 칸 = 전역 **`BaseNamedObjectDirectory`** (kernelbase rva `0x3af0e0`).
- 0 이 아니면 `+0x16` 에서 읽고 `+0x22` 에서 돌려주고 끝. **syscall 도, 안내문 읽기도 없다.**
- 0 이면 (처음이면):
  1. `+0x80 / +0x87` `NtOpenProcessToken` — 우리 가게 **신분증** 을 꺼낸다.
  2. `+0xab / +0xb0` `BasepGetNamedObjectDirectoryForToken(h, 0, 1, 0xf, &dir)` — 신분증을 보고 맞는 방을 연다.
  3. `+0xbd` 수첩에 손잡이를 적는다 (`CMPXCHG` — 먼저 적은 쪽이 이긴다).
  4. `+0xf4 / +0xfb` `NtClose` — 신분증을 다시 넣는다.

비유: "사물함 방 열쇠고리, 수첩에 걸어 둔 거 있나? 없으면 신분증 보여 주고 방 하나 열어 와서 걸어 두자."

### 2.3 ★ 멈춘 줄 — 방 이름을 안내문 `+0x20` 에서 읽는다

`BasepGetNamedObjectDirectoryForToken` 안에서 (`[정적]`):

1. 토큰 질문 셋 — `NtQueryInformationToken` 클래스 `0xc` · `0x1d` · `0x2a` (`+0xf6` · `+0x124` · `+0x153`).
   - 클래스 이름은 PDB 에 없어 공개 번호표를 빌렸다 `[추측]`: 세션 번호 · AppContainer 인가 · 개인 이름공간인가.
2. 보통 길이면 `+0x180` `RtlGetCurrentServiceSessionId` 와 세션 번호를 비교해 두 갈래로 나뉜다.
   - 세션 0 갈래: 지역 버퍼에 `\BaseNamedObjects` 를 만든다.
   - 세션 N 갈래 (`+0x192 JNZ`): `+0x40f / +0x414` `RtlStringCchPrintfW` 로 `\Sessions\N\BaseNamedObjects` 를 만든다.
3. 둘 다 `+0x29d` 에서 다시 만난다. 그리고 `+0x2a8 TEST EDI,EDI` · `+0x2aa JNZ`.
   - **EDX (둘째 인자) 가 0 이 아니면** 지역 버퍼 이름을 쓴다.
   - **0 이면** 안내문을 읽으러 `+0x2b0` 으로 간다.
4. `+0x2b0 MOV RAX,[BaseStaticServerData]` → `+0x2b7 MOVUPS XMM0,[RAX+0x20]` — **여기서 PE_emulator 가 멈췄다.**

#### 한 줄씩 읽는 법 — `+0x2b7`

- **누가**: kernelbase `BasepGetNamedObjectDirectoryForToken`
- **어디서**: 함수 시작에서 `+0x2b7` 떨어진 명령
- **무엇을**: `RAX` (= 수첩 칸 `BaseStaticServerData` 에 적힌 안내문 주소) 에서 `+0x20` 떨어진 곳의 **16바이트** (`UNICODE_STRING` 쪽지 하나) 를 읽는다.
- **무엇을 위해**: 그 쪽지에 적힌 이름으로 폴더를 열려고 (`+0x37f / +0x386` `NtOpenDirectoryObject(&dir, 0xf, …)`).
- PE_emulator 에선 `RAX = 0` 이라, 주소 `0x20` 을 읽으려다 멈췄다.

#### 왜 "세션 0 이든 N 이든 같은 줄" 인가 — 단계별로

1. 근거: 부르는 쪽 `BaseGetNamedObjectDirectory +0x9c XOR EDX,EDX` — EDX 를 0 으로 넘긴다 `[정적]` (오케스트레이터가 `.asm` 으로 한 번 더 봄).
2. 그래서 `+0x2aa JNZ` 는 **안 탄다** → 늘 `+0x2b0` 으로 간다.
3. 그러니 두 갈래 차이는 "지역 버퍼에 무슨 이름을 만들었나" 뿐이고, 그 이름은 **쓰이지 않는다**.
4. 결론: PE_emulator 가 토큰 대답을 세션 1 로 고쳐도 **같은 자리에서 멈춘다.** 안내문을 지어야 한다 `[정적]`.

비유: 방 이름을 "내 머리로 지어 볼까, 게시판 안내문을 볼까" 고르는 갈림길이 있는데, 부르는 쪽이 늘 "안내문 봐" 라고 한다. 안내문이 없으면 그 자리에서 막힌다.

### 2.4 안내문은 누가 언제 붙이나 — 새 말 "공유 섹션"

#### 새 말 — 공유 섹션

- **공유 섹션** = 한 원본 물건 (섹션) 을 **여러 프로세스가 각자 자기 땅에 펼쳐 보는 것**. 같은 내용을 보지만 펼친 자리 (주소) 는 프로세스마다 다르다 `[추측]` (일반적으로 알려진 설명).
- 비유: run68 의 **사무실 게시판** 이 바로 이거다. 사무실도 자기 벽에 한 판, 우리 가게도 우리 벽에 한 판 붙인다. 같은 게시판인데 붙인 벽이 다르다.
- 이 게시판은 test.exe 에겐 **읽기 전용** 으로 펼쳐진다 (run68 `NtMapViewOfSection` 읽기 전용).

#### 붙이는 순서 (csrss 쪽, 세션 시작 때 — 흐름도 범위보다 앞)

```
csrsrv!CsrSrvCreateSharedSection
 ├─ +0x1bf  NtCreateSection (\Sessions\N\Windows\SharedSection)   게시판 원본
 ├─ +0x233  NtMapViewOfSection                                     사무실 벽에 붙임 (C)
 ├─ +0x26e  RtlCreateHeap(0x7008)                                  게시판 안 작은 창고
 └─ +0x2bb  RtlAllocateHeap(0x30)                                  6칸 표 (창구별 안내문 위치)
csrsrv!CsrLoadServerDll  (basesrv = 서버 번호 1)
 └─ +0x380  basesrv!ServerDllInitialization
             +0x379  RtlAllocateHeap(…, 0xa00) → 안내문 S
             +0x39c  [S+0x9e8] ← S          자기 주소
             +0x3a3  [S+0x9c8] ← 0xffffffff
             +0x4a5 · +0x4fb  [S+0x20] ← \Sessions\N\BaseNamedObjects
   +0x3da ~ +0x3ee  표[1] ← S
```

- 모두 `[정적]`.
- 그다음 test.exe 가 전화 (`NtConnectPort`) 를 걸면, `CsrApiHandleConnectionRequest +0x9c ~ +0xd0` 이 답장에 **게시판 시작 (C) · 표 주소 · csrss PID** 를 적어 준다 `[정적]`.
- ntdll `CsrpConnectToServer` 가 PEB 세 칸에 적고 (run68), kernelbase 가 `+0x32b` 에서 수첩 `BaseStaticServerData` 에 **우리 번지** 로 적는다 `[정적]`.
- 그러니 test.exe 가 오기 전에 **안내문 내용은 이미 다 있다** `[정적]`. test.exe 쪽에서 안내문에 쓰는 줄은 이번 범위에 없다 (읽기 전용 뷰라서 `[추측]`).

#### 안내문의 줄 몇 개 (대표만 — 칸 23개 전체는 run 문서 "확인된 것")

| 오프셋 | 무엇 (라벨은 `[추측]`) | test.exe 쪽이 읽는 때 |
|---|---|---|
| `+0x000` · `+0x010` | Windows · System 디렉터리 쪽지 | ② 초기화 (kernelbase 시작 함수) |
| **`+0x020`** | **이름 있는 객체 폴더 이름 쪽지** | 폴더를 **처음 열 때** (`+0x2b7`) |
| `+0x178` | NLS 사용자 정보 (`0x67c`B) | 초기화에선 **주소만** (2.8) |
| **`+0x9c8`** | 시간대 리다이렉트 번호 | 나중 (`IsTimeZoneRedirectionEnabled` 외 15곳) |
| **`+0x9e8`** | **자기 주소 (csrss 쪽)** | 옮김 식 전부 (2.5) |

- 자리·크기·쓰는 줄은 `[정적]`. PDB 에 이 타입이 없어서 (kernelbase·kernel32·basesrv·csrsrv 공개 PDB 구조체 0개) 라벨은 코드 쓰임과 옛 공개 이름을 빌렸다 `[추측]`.
- 이 프로세스 ② 초기화가 읽는 칸은 `+0x0` · `+0x10` · `+0x38` · `+0x3a` · `+0x170` · `+0x7f8` · `+0x9e8` 뿐이다. `+0x20` 은 그보다 **늦게**, 폴더가 처음 필요할 때 읽는다 `[정적]`.

### 2.5 쪽지 안 주소도 "사무실 번지" 다 — `+0x9e8` 자기 주소로 옮기기

- `+0x20` 쪽지에는 "글이 어디 있나" (`Buffer`, `+0x28`) 가 들어 있다. 그런데 이 주소는 **사무실 벽 기준 번지** 다 `[정적]`.
  - 1번 창구가 자기 땅에서 글을 써 넣었으니까.
- 그래서 kernelbase 는 읽으면서 옮긴다 (`+0x2d3 ~ +0x343`, `[정적]`):
  - `Buffer += 안내문 우리 주소 − [안내문 +0x9e8]`
  - `[+0x9e8]` = 1번 창구가 써 둔 **안내문 자기 주소 (사무실 번지)** (`+0x39c`).
- 왜 이렇게 하나 — 단계별로:
  1. "글이 안내문 시작에서 얼마나 떨어졌나" = `Buffer − [+0x9e8]` (사무실 번지끼리 빼기).
  2. 그 거리를 **우리 벽의 안내문 시작** 에 붙이면 우리 번지가 된다.
  3. run68 의 PEB `+0x98` 옮겨 적기와 같은 생각이다. 이번엔 기준을 안내문 안 `+0x9e8` 에서 가져올 뿐이다.
- `[관찰]` run44 에선 R (`0x7ff4ecf70000`) ≠ C (`0x7ff5575f0000`) 이다. 진짜 프로세스는 이 옮김이 **0 이 아니다.**
- (참고) `Buffer` 가 0 이면 `+0x2ca TEST` · `+0x2cd JZ` 로 옮김을 건너뛰고 빈 이름으로 syscall 에 간다. 그래서 **0 만 든 안내문** 도 이 길에선 멈추지 않는다 `[정적]`. 다만 진짜와 같게 하려면 `+0x20` · `+0x9e8` 을 채운다.

### 2.6 ★ PE_emulator 가 지을 최소 칸

run 문서 맨 앞 "최소 칸 목록" 을 그대로 줄였다 (`[정적]`).

**1단계 — 필수** (`OutputDebugStringA` 를 멈추지 않고 지나기):

| 무엇 | 값 | 한마디 |
|---|---|---|
| PEB `+0x88` (R) · `+0x380` (C) | **R == C 로 두기** (권장) | 우리 벽 = 사무실 벽으로 치면 옮김이 0 |
| PEB `+0x98` (T) | 6칸 표 (`0x30`B) 의 주소 | 창구별 안내문 위치 목록 |
| 표 `T[1]` (= `[T+8]`) | S = 안내문 주소 | basesrv 는 서버 번호 1 |
| 안내문 `0xa00`B | 읽을 수 있는 메모리, 안 쓴 칸 0 | 크기 정해짐 |
| 안내문 `+0x9e8` (8B) | **S** (자기 주소) | 정해짐 |
| 안내문 `+0x20` (16B) | `{Length 0x38, MaximumLength 0x3a, Buffer → L"\Sessions\1\BaseNamedObjects"}` | 끝 NUL 포함 `0x3a`B. 세션 번호는 에뮬레이터가 정함 |

- `BaseStaticServerData` 는 kernelbase 시작 함수가 셈한다. PE_emulator 가 시작 함수를 흉내 낸다면, 이 수첩 칸에 안내문 주소를 **바로** 넣어도 같다.

**꼭 기억할 한 칸 — `+0x9c8 = 0xffffffff`**

- 0 으로 두면 `IsTimeZoneRedirectionEnabled` (`[+0x9c8] != -1`) 가 **"시간대 리다이렉트 켜짐"** 으로 읽는다 `[정적]`.
- 1번 창구도 처음 값을 `0xffffffff` 로 쓴다 (`ServerDllInitialization +0x3a3`) `[정적]`.
- 비유: 안내문 빈칸은 대부분 0 이면 "없음" 인데, 이 칸만 **"−1 = 없음"** 이라는 규칙이다. 0 으로 두면 "있음" 으로 읽힌다.
- 나머지 2단계 권장 칸 (`+0x9d0` · `+0x9cc` · `+0x00` · `+0x10` …) 은 run 문서 "권장 칸 — 2단계" 표.

**안내문만 지으면 syscall 이 실패해도 끝까지 간다** `[정적]`:

- `NtOpenDirectoryObject` 실패 → `BaseGetNamedObjectDirectory` 가 그 상태를 올림 → `CreateDBWinMutex` 가 `BaseSetLastNTError` 하고 0 → `OutputDebugStringA` 가 실패 깃발 (`[0x1803ad960] = 1`) 을 켜고 `+0xe5` 에서 `DbgPrint` 길로.
- 성공시키고 싶으면 폴더 핸들 하나 + `NtCreateMutant` 성공이면 된다.

**다른 길 하나 — 수첩에 손잡이를 미리 걸어 두기** `[정적]`:

- `BaseNamedObjectDirectory` (rva `0x3af0e0`) 에 가짜 폴더 핸들을 넣어 두면, `BaseGetNamedObjectDirectory` 는 `+0x22` 에서 바로 끝난다. 안내문을 안 본다.
- 단, 안내문을 읽는 **다른** 함수 (시간대 · DOS 장치 · `CreateProcessInternalW` 등) 는 그대로 남는다. kernelbase 에서 `BaseStaticServerData` 를 읽는 줄은 40개다 (`_refs/0x3aedb8.tsv`).

### 2.7 ★ 새로 드러난 것 — 진짜 가게는 `main` 보다 훨씬 앞에서 이 길을 이미 탔다

이번 바퀴에서 가장 눈에 띄는 대목이다. run44 사진의 **핸들 목록** 을 다시 읽어서 나왔다 (새 실행 없음).

#### 사진에 보인 것 `[관찰]`

| 사진 구간 | 새로 생긴 핸들 | 뜻 |
|---|---|---|
| P1a 목록 (`0x4 ~ 0x70`) | 폴더 **없음** | 아직 사물함 방을 안 열었다 |
| **P1a ~ P1b** (kernel32 준비 구간) | `0x80` **Directory `BaseNamedObjects`** (`GrantedAccess 0xf`) | 사물함 방 손잡이. `0xf` 는 `NtOpenDirectoryObject` 접근 `0xf` 와 같다 |
| 같은 구간 | `0x7c` Mutant `SM0:7632:304:WilStaging_02` · `0x84` · `0x88` Semaphore (`…_p0` · `…_p0h`) | 이름의 `7632` = test.exe PID `0x1dd0`. 그 방 안에 있다 |
| **P1b ~ P2** (DLL 전원 켜기 구간) | `0xbc` **Mutant `DBWinMutex`** (`0x120001`, `HandleCount 7`) | 2.1 의 동네 공용 방 열쇠 |

- `DBWinMutex` 객체 (`ffff8285969fdda0`) 는 **P1b 폴더 목록에 이미 있다** `[관찰]`.
  - → test.exe 가 만든 게 아니라 **다른 가게가 먼저 만든 열쇠를 같이 잡은 것** 이다.
  - 그래서 `NtCreateMutant` 가 "이미 있음" (`0x40000000`) 으로 답하는 갈래 → `RtlSetLastWin32Error(0xb7)` 쪽일 것이다 `[추측]`.
  - `0x120001` 은 청한 `0x2120001` 에서 `MAXIMUM_ALLOWED` 가 빠진 값이고, 자물쇠 규칙의 "모두 `0x120001`" 과 같다 `[정적 + 관찰]`.
- 토큰 `TS Session ID: 0x1` `[관찰]` — 2.3 의 세션 N 갈래 조건과 맞다.

#### 그래서 무엇이 먼저였나 — 단계별로

1. **P1a ~ P1b** 에 폴더 손잡이 `0x80` 이 생겼다 `[관찰]`.
   - 누가 열었는지는 `[공백]` 이다.
   - 같은 때 생긴 이름이 `Wil…` 이라, kernelbase 안 WIL 기능 단계 코드가 이름 있는 뮤텍스를 만들면서 폴더를 연 것으로 보인다 `[추측]`.
   - 수첩 칸 `BaseNamedObjectDirectory` 를 쓰는 줄은 `BaseGetNamedObjectDirectory +0xbd` 말고도 셋 더 있다 (`BaseFormatObjectAttributes +0x13d` · `OpenMutexW +0x17c` · `OpenEventW +0x17c`) `[정적]`. 그중 누가 처음인지는 모른다.
2. **P1b ~ P2** 에 `DBWinMutex` 손잡이 `0xbc` 가 생겼다 `[관찰]`.
   - 이 구간에서 `OutputDebugStringA` 를 처음 부르는 건 sample 코드다: dll2 TLS 콜백 `Dll2Tls` → `Log` (`[소스]` `dll2.c:7`, `log.h:37`), 그다음 `dll2!DllMain +0x25 / +0x2a → Log` (`[정적]`, DllMain 도달은 run6·run7 `[관찰]`).
3. **진입점 뒤 `main` 첫 `Log`** 에선 새로 생기는 짐이 없다 `[정적 추론]`.
   - 뮤텍스 전역 `[0x1803ad968]` 이 이미 차 있어서 `CreateDBWinMutex` 를 다시 안 부른다 (`+0xac ~ +0xb5`). 바로 기다림 → `OpenFileMappingW("DBWIN_BUFFER")` 실패 → `DbgPrint`.
4. 결론: **PE_emulator 가 은퇴 뒤 `main` 에서 처음 닿은 건 진짜와 순서가 다르다.**
   - 진짜는 (1) kernel32 준비 구간의 WIL 이름 객체 길, (2) dll2 TLS · DllMain 의 `Log` 가 먼저 간다.
   - PE_emulator 에서 이 둘을 왜 안 탔는지는 **PE_emulator 쪽 확인 거리** 다. 우리 쪽은 모른다.

비유: 진짜 가게는 개업 준비 때 이미 사물함 방 열쇠고리를 수첩에 걸어 두고 (P1a ~ P1b), DLL 전원을 켤 때 동네 공용 방 열쇠도 잡아 둔다 (P1b ~ P2). `main` 은 그냥 그 열쇠를 쓰기만 한다. PE_emulator 가게는 이 두 번을 건너뛰고 `main` 에서야 처음 방을 찾다가 막혔다.

#### 조심할 점 둘

- 사진엔 kernelbase `.data` 가 없다 (run67 · run68). 그래서 `0x80` 이 수첩 칸 `BaseNamedObjectDirectory` 의 값이라는 건 `[추측]` 이다.
- 핸들 번호가 `0x7c` (뮤텍스) < `0x80` (폴더) 인 것은 "토큰 손잡이가 `0x7c` 를 먼저 받았다가 닫히고, 그 자리를 뮤텍스가 다시 받았다" 로 읽으면 2.2 의 순서 (신분증 꺼내기 → 방 열기 → 신분증 넣기 → 열쇠 만들기) 와 맞는다 `[추측]`.

#### 디버거가 붙으면 달라질 수 있는 곳

- run44 는 **커널 디버거** 가 붙은 실행인데도 `DBWinMutex` 손잡이가 생겼다. 즉 커널 디버거는 2.1 의 "디버거 있어요?" 손을 안 잡았다 `[관찰 간접]`.
- **유저 디버거 (cdb)** 가 붙으면 그 손을 잡아서 착지 몸통을 안 탈 수 있다. 그때는 `DBWinMutex` 같은 짐이 안 생긴다 `[추측]` (결과서 1-D).
- 그래서 이름 있는 객체 짐을 볼 때는 **커널 디버거만** 붙인 실행으로 봐야 한다 (run 문서 도구 메모의 runbook 제안).

### 2.8 run68 정정 — `+0x178` 은 "주소만" 만든다

- run68 에선 "`gpServerNlsUserInfo ← BaseStaticServerData + 0x178`" 을 그 칸을 **읽은 값** 처럼 적었다.
- 실제 코드는 `BaseNlsDllInitialize +0x9 LEA RAX,[R8+0x178]` → `+0x10 MOV [gpServerNlsUserInfo],RAX` 다 `[정적]`.
  - `LEA` = 칸 안을 읽지 않고 **칸의 주소** 만 셈하는 명령.
- 그리고 (`[정적]`):
  - `BaseNlsDllInitialize` 와 `NlsProcessInitialize` 는 그 영역 안을 읽지 않는다.
  - 1번 창구도 초기화 때 그 영역을 안 채운다 (0). 사용자 로그온·NLS 갱신 때 `BaseSrvNlsLogon` · `NlsUpdateCacheInfo` 가 나중에 쓴다.
- 비유: "안내문 `+0x178` 줄을 읽어 왔다" 가 아니라 "안내문 `+0x178` 줄 **위치에 책갈피** 를 꽂아 뒀다" 였다. 책갈피 자리는 지금 비어 있다.
- 단서 하나: run68 의 `[공백]` `MakeLocHashNode(0x7f)` 아래가 `GetCurrentNlsCache` 까지 가면, 그 함수는 이 책갈피로 읽는다. 그 길을 타는지는 run68 공백 그대로다 (Q138 (f)).
- run68 문서 맨 위에 정정 한 줄을 넣었다. flow.md 2.5 트리 · 그림 2 `bn1` · 짐 지도 7.6.8 NLS 줄도 고쳤다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 절 | 전 | 후 |
|---|---|---|
| 그림 2 | `test!main` 은 이름만, `dll2!Dll2Tls` 는 "소스상 TLS 콜백, 실행 미관찰" 만. 진입점 뒤에서 kernelbase 로 가는 화살표 없음 | `KBOD` (kernelbase 넷: `OutputDebugStringA` → `CreateDBWinMutex` → `BaseGetNamedObjectDirectory` → `BasepGetNamedObjectDirectoryForToken`) · `PH5` (ntdll 스텁 셋: `NtOpenDirectoryObject` · `NtCreateMutant` · `DbgPrint`) 새로. `dll2!Dll2Tls` · `test!main` 상자에 "이 프로세스 첫 호출은 TLS" / "PE_emulator 가 멈춘 자리" 메모 `(정적 run71)` |
| 그림 2 `bn1` | `gpServerNlsUserInfo ← BaseStaticServerData+0x178` | `← 주소 BaseStaticServerData+0x178 (LEA, 안 읽음 — run71 정정)` |
| 2.5 | csrss 연결 값 (run68) 까지 | 새 절 "csrss 공유 섹션 블록 `BaseStaticServerData` 와 이름 있는 객체 폴더" — csrss 쪽 사슬 `[정적]` + 이 프로세스 폴더 핸들 `[관찰]` |
| 2.8 | dll2 → dll1 TLS·DllMain | 새 절 "TLS 콜백·`DllMain` 이 kernelbase 를 다시 부르는 것 — `Log` → `OutputDebugStringA`" |
| 2.10 | 진입점 → `main` | 새 절 "진입점 뒤 kernelbase 를 다시 부르는 길" (`main` 첫 `Log` → … → `+0x2b7`) `[정적]` |
| 4장 | — | 해결됨 줄 (Q136) + 새 줄 넷: 사진으로 본 순서 · 동적 확인 1 ~ 5 · 정적으로 남은 것 ① ~ ⑥ · 정정 |

- 짐 지도 (`baggage.md`) 엔 새 표 **7.6.11** 이 생겼다 (체크리스트 밖이라 칸 수엔 안 셈). TEB `LastErrorValue` 줄이 하나 늘었다 (38 → 39) — 첫 `OutputDebugStringA` 가 값을 저장했다가 되돌리는 줄이다.

### 큰 그림에서

- CLAUDE.md 는 "진입점 뒤에서 ntdll 을 다시 부르는 것만 화살표로 남긴다" 고 했다. 이번에 그 화살표가 하나 생겼다. 그런데 사진을 보니 **같은 길을 진입점 앞 (dll2 TLS) 에서 먼저** 탄다. 그래서 그림 2 의 dll2 TLS 상자에도 같은 화살표가 붙었다.
- PE_emulator 쪽으로는 두 가지가 이어진다:
  1. 안내문 하나 (최소 칸 + `+0x9c8 = 0xffffffff`) 를 지으면 지금 멈춤은 풀린다.
  2. 하지만 진짜와 같은 **순서** 를 맞추려면, P1a ~ P1b 에 폴더를 연 길 (Q138 (a)) 과 dll2 TLS 의 `Log` 가 왜 PE_emulator 에서 안 탔는지 (PE_emulator 쪽) 를 봐야 한다.

---

## 4. 아직 모르는 것

### 정적으로 볼 것 — Q138 (맨 뒤)

| 무엇 | 왜 이번에 못 했나 |
|---|---|
| (a) P1a ~ P1b 에 폴더 `0x80` 을 연 길 — WIL 이름 객체 (`SM0:<PID>:304:WilStaging_02` 뮤텍스 · 세마포어 둘) 를 만드는 kernelbase 함수 (`BaseFormatObjectAttributes +0x13d` 후보) | 이번 요청 범위는 `main` 길 세 함수였다. 사진을 다시 읽다가 새로 드러났다 |
| (b) `BaseFormatObjectAttributes` · `OpenMutexW` · `OpenEventW` 가 수첩 칸이 비었을 때 안내문 `+0x20` 을 읽나 | 수첩 칸을 쓰는 줄 넷 중 셋은 결과서 범위 밖 |
| (c) csrss `ServerDllInitialization` 의 `NtCreateDirectoryObject` 4곳이 각각 어느 폴더인지 | 이번엔 "4곳 중 하나" 까지만 봤다 |
| (d) `BaseUsePrivateNamespaces` (rva `0x3a9f30`) 를 쓰는 줄 | 결과서에 안 나왔다. 그래서 "0 이라 아무 일도 안 함" 은 `[추측]` |
| (e) kernel32 `OutputDebugStringA` export 가 forwarder 인지 (Q60 `LoadLibraryExW` 와 같은 꼴) | `Log` 의 IAT 가 kernel32 를 거치는지 안 봤다 |
| (f) `MakeLocHashNode(0x7f)` 아래가 `GetCurrentNlsCache` 로 `gpServerNlsUserInfo` 를 읽나 (Q131 (a) 와 같음) | run68 공백 그대로 |

### 실행 때 값 — Q104 VM 단계 (동적)

kernelbase 멈춤점은 test.exe 일 때만 서게 조건을 붙인다.

1. **안내문 실제 값** — `_KernelBaseBaseDllInitialize+0x332` 에서 안내문 전체와 PEB 세 칸·표. 정적으론 "어디에 무엇이 적히나" 꼴만 알고, csrss 가 실제로 쓴 값은 모른다.
2. **폴더를 처음 연 곳** — P1a ~ P1b 의 첫 `ntdll!NtOpenDirectoryObject` 에서 콜스택. 그리고 수첩 칸 `kernelbase+0x3af0e0` 이 정말 `0x80` 인지.
3. **탄 갈래** — `BasepGetNamedObjectDirectoryForToken+0x2b0` 에서 세션 · 개인 이름공간 · AppContainer 칸. (2 가 이 함수를 안 거치면 여기는 안 선다.)
4. **첫 `OutputDebugStringA`** — `+0xc2` 에서 콜스택 (dll2 TLS 인가 DllMain 인가), `CreateDBWinMutex+0x3cf` 상태 (`0x40000000` 기대), `OpenFileMappingW` 결과 (0 기대).
5. `TEB+0x179c IsImpersonating` = 0 인지 (`BaseGetNamedObjectDirectory+0x63` 갈래).

### 우리 쪽에선 못 보는 것

- PE_emulator 에서 kernel32 준비 구간의 WIL 길과 dll2 TLS · DllMain 의 `Log` 가 **왜 안 탔는지**. PE_emulator 쪽 확인 거리다 (run 문서).

---

## 5. 더 읽기

- run 문서: [`run71-named-obj-dir.md`](run71-named-obj-dir.md) — 특히 맨 앞 "PE_emulator 쪽에서 쓸 것", "확인된 것" 칸 23개 표, "사진과 맞춰 본 것", "정정", "도구 메모"
- 결과서: [`_workspace/sast/66_named-obj-dir_result.md`](../../_workspace/sast/66_named-obj-dir_result.md) — 1-A ~ 1-D (세 함수 · `OutputDebugStringA`), 2-B (칸 표), 2-D (누가 언제)
- 요청서: [`_workspace/sast/66_named-obj-dir.md`](../../_workspace/sast/66_named-obj-dir.md)
- flow.md: [그림 2](flow.md) (`KBOD` · `PH5` · `bn1`), 2.5 "csrss 공유 섹션 블록 …", 2.8 "TLS 콜백·`DllMain` 이 kernelbase 를 다시 부르는 것", 2.10 "진입점 뒤 kernelbase 를 다시 부르는 길", 4장
- 짐 지도: [`baggage.md`](baggage.md) 7.6.11
- 앞선 해설: [run68](run68-dll-init-rest-해설.md) (csrss 전화 · 크리티컬 섹션 · NLS) · [run67](run67-kb-heap-init-해설.md) (같은 길의 앞선 멈춤) · [run44](run44-baggage-snap-해설.md) (사진 P1a · P1b · P2)

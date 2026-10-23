# run34 parent-leftovers — 해설

> 이 글은 [run34 문서](run34-parent-leftovers.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q41** — 2.2 부모 유저 쪽 남은 정적 확인 (작음) 넷: `kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW` 연결, ApiNumber `0x1001d` 정체, ntdll `NtCreateUserProcess`·`NtResumeThread` 스텁, KERNELBASE 가 켜는 ProcessFlags bit 9 를 커널이 어떻게 쓰나 (`_workspace/plan.md`, 이번에 **끝**)
  - 함께: **Q65** 의 "`PspAllocateProcess` 인자 `[RSP+0x90]` bit 9" 부분 (run33 이 남긴 것). 이번에 **풀렸다.** Q65 에는 둘이 남는다.
  - **단계 3 "작은 정적부터 몰아서"** 의 바퀴다 (사용자 결정, 2026-10-23). run33(Q40) 다음이다.
  - 새 항목: **Q66** (정적, 작은 둘). 출처를 더한 항목: **Q19** (동적).
  - 다음 항목: **Q45** (plan.md).
- 근거:
  - run 문서 [`run34-parent-leftovers.md`](run34-parent-leftovers.md) (가장 중요)
  - 요청서 [`_workspace/sast/29_parent-leftovers.md`](../../_workspace/sast/29_parent-leftovers.md)
  - 결과서 [`_workspace/sast/29_parent-leftovers_result.md`](../../_workspace/sast/29_parent-leftovers_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 이미 분석돼 있던 kernel32·KernelBase·ntdll·ntoskrnl 은 다시 안 돌렸다 (flow.md 기준과 같은 분석본).
  - **처음 연 모듈 둘**: `basesrv.dll` (함수 156개), `csrsrv.dll` (함수 129개). 둘 다 csrss 프로세스 안에서 도는 DLL 이다.
  - 덤: `cmd.exe` 는 Ghidra 로 안 열었다. PDB 이름만 받아 `objdump` 줄에 붙여 **참고로만** 읽었다.
  - PE 파일 안의 표(IAT, API-set 표, `KiServiceTable`, `BaseServerApiDispatchTable`)는 임시 스크립트로 읽었다 (하네스 밖, 커밋 안 함).

이 글은 [run14 해설](run14-create-process-internal-해설.md)의 **뒷정리 편**이고, [run33 해설](run33-first-resume-해설.md)의 **짐작 하나를 고치는 편**이다.

- run14 해설 4절에 "Q41 (정적)" 으로 남은 줄이 넷 있었다. 이번에 넷 다 코드로 답이 나왔다.
- run33 해설 2.6 에 "일반 생성에선 이 비교가 맞기 어려워 보인다" 는 짐작이 있었다. **이번 결과로 그 짐작은 반대였다** (2.7).

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 비트·플래그 → [run9 해설](run9-create-user-process-해설.md) 2절
- 시스템 콜(**신청 창구**) → [run9 해설 "시스템 콜"](run9-create-user-process-해설.md#시스템-콜)
- 시스템 콜 번호(**번호표**), 서비스 표 `KiServiceTable`(**번호표 안내판**), ntdll 스텁 → [run29 해설 2.4](run29-continue-start-해설.md#24-창구까지--ntdll-스텁과-번호-0x43)
- 핸들(**손잡이**) → [run9 해설 "핸들"](run9-create-user-process-해설.md#핸들)
- 직접 호출·간접 호출 → [run9 해설](run9-create-user-process-해설.md#직접-호출과-간접-호출)
- CFG(**교환원**) → [run28 해설 2.9](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)
- import(가져온 함수 칸) → [run14 해설 2.3](run14-create-process-internal-해설.md) "새 말: import"
- api-set(**별명**), 별명 → 진짜 이름 풀이 → [run23 해설](run23-load-dependent-해설.md) 2.5, kernel32 의 import 에 별명이 102개 → [run25 해설](run25-dll-entry-해설.md) 2.5
- `dwCreationFlags`(손님 **주문서**), `CREATE_SUSPENDED`(주문서의 4 칸) → [run14 해설](run14-create-process-internal-해설.md) 2.4
- csrss(**인사팀 사무실**), `CsrClientCallServer` → [run14 해설 2.6](run14-create-process-internal-해설.md#26-3-그-사이-부모가-하는-일--검사--csrss-알림--후처리)
- `OwnerProcessId`(장부 "**주인**" 란), `PspProcessClose`(손잡이 **반납 창구**), `ProcessFirstResume`(첫 출근 허가 **도장**) → [run33 해설](run33-first-resume-해설.md) 2.5 · 2.6
- SxS 리다이렉트 → [run23 해설](run23-load-dependent-해설.md) 2.5 "곁가지"

비유도 그대로 이어 쓴다.

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소의 **신청 창구**, ntdll = 가게 안 **관리실**
- 프로세스 = **가게**, 스레드 = **직원**, `_EPROCESS` = 관리사무소 쪽 가게 **장부**. 부모 cmd.exe = 새 가게를 **신청하는 쪽 가게**
- `KERNELBASE!CreateProcessW` = **대행 책상의 접수원**, `CreateProcessInternalW` = 그 뒤의 **대행 책상** (run14)
- 시스템 콜 번호 = **번호표**, `KiServiceTable` = 창구 벽의 **번호표 안내판** (run29)
  - flow.md 는 같은 그림을 "관리실의 **전화기**, 단축 번호 `0xd1`" 로 적었다. **같은 것**이다.
- csrss = 관리사무소와 따로 있는 **인사팀 사무실**, 거기 올라가는 목록 = 인사팀 **명부** (run14)
  - run 문서·flow.md 는 같은 것을 "**동네 주민센터**에 전입 신고" 로 적었다. **같은 것**이다.
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- **이번에 더하는 비유**:
  - `kernel32!CreateProcessW` = 대행 책상 앞의 **앞 접수대** — 서류를 펴 보지도 않고 옆 접수원에게 넘긴다 (2.3)
  - csrss 서버 DLL = 인사팀 사무실 안의 **창구들**, ApiNumber = 신청서 맨 위 "**창구 번호 - 업무 번호**" (2.4)
  - `INT 0x2e` = 관리사무소의 **옛 쪽문**, 공유 사용자 데이터 = 건물 로비의 **공용 게시판** (2.5)

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 넷** (전부 `[정적]`, 실행으로 본 건 아직 없다):

1. **`kernel32!CreateProcessW` 는 껍데기다.** 인자 열 개를 그대로 들고 `KERNELBASE!CreateProcessW` 를 한 번 부른다 (ret **`+0x54`**). 그래서 그림 1 맨 앞에 `KERNEL32` 상자가 새로 생겼다 → 2.3
2. **`0x1001d` = 1번 서버(basesrv)의 `0x1d` 번 업무 = `basesrv!BaseSrvCreateProcess2`.** csrss 가 새 프로세스·첫 스레드를 **자기 명부에 등록**한다. 단 "1번 = basesrv" 는 **간접 증거**다 → 2.4
3. **ntdll 스텁의 번호표 `0xd1`·`0x52` 는 커널 안내판의 그 칸이 맞다** (run29 와 같은 방법). 다만 `SYSCALL` 하나가 아니라 **`INT 0x2e` 로 가는 옆 갈래**가 붙어 있다 → 2.5
4. **ProcessFlags bit 9 = run33 이 본 `[RSP+0x90]` bit 9, 같은 비트다.** `CREATE_SUSPENDED` 가 **없으면** `OwnerProcessId` = **부모 PID 그대로**. → run33 의 "보통 생성에선 비교가 맞기 어렵다" 짐작은 **반대였다** → 2.6 · 2.7

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

시간 순서로 놓았다. 오프셋은 **돌아올 자리(ret)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd!ExecPgm  +0x59c                              (참고: objdump, 그림엔 안 넣음)
     └─ kernel32!CreateProcessW  +0x54                ← 이번 ① 새 상자
          └─ KERNELBASE!CreateProcessW  +0x66          (run14)
               └─ CreateProcessInternalW                (run14)
                    │  +0x800  CREATE_SUSPENDED 없으면 bit 9 켜기   (run14) → 이번 ④ 이 비트의 끝
                    ├─ (1) +0x30ee  ntdll 스텁 NtCreateUserProcess  번호표 0xd1   ← 이번 ③
                    │        ══▶ 커널 nt!NtCreateUserProcess                (run9 ~ run13, run31, run32)
                    │              └─ +0xc36 PspAllocateProcess  +0x707 bit 9 → 주인 란   ← 이번 ④
                    ├─ (2) +0x4672  ntdll!CsrClientCallServer  0x1001d     ← 이번 ②
                    │        ┄┄▶ csrss 의 다른 스레드: basesrv!BaseSrvCreateProcess2
                    └─ (3) +0x5bd3  ntdll 스텁 NtResumeThread  번호표 0x52   ← 이번 ③
                             ══▶ 커널 nt!NtResumeThread                      (run13)
 ─ ─ ─
 test.exe 첫 스레드 → ntdll 로더 → … → main                                 (run15 ~ run30)
```

- 전부 **flow.md 2.2 "부모 쪽 마무리 ~ 첫 스레드 출발"** 과 **그림 1 (부모 cmd.exe 스레드)** 이야기다.
- ④ 만 커널 안(2.1 `PspAllocateProcess`)까지 내려간다. 그리고 그 결과가 run33 의 `PspProcessClose` (2.2) 로 다시 이어진다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면 (모두 run14 가 남긴 작은 것):

1. `kernel32!CreateProcessW` 가 `KERNELBASE!CreateProcessW` 를 부르나? → 그림 1 `kb1` 앞에 상자를 둘 수 있나.
2. csrss 알림의 ApiNumber `0x1001d` 는 **csrss 쪽에서 무슨 일**인가.
3. ntdll `NtCreateUserProcess`·`NtResumeThread` 스텁이 **바로 시스템 콜**을 하나. 그 번호가 커널 안내판의 그 칸인가.
4. KERNELBASE 가 켜는 ProcessFlags **bit 9 (`0x200`)** 를 커널이 어떻게 쓰나.

- 함께: 4번의 bit 9 가 run33 이 본 `PspAllocateProcess` 인자 `[RSP+0x90]` 의 bit 9 와 **같은 비트인가** (Q65 일부).

### 바퀴 전에 알던 것

- `[정적]` (run14) `KERNELBASE!CreateProcessW` (107B) 는 `CreateProcessInternalW` 를 `+0x61` (ret `+0x66`) 에서 부르는 얇은 껍데기.
- `[추측]` (run14) cmd.exe 가 이 입구로 오는지, 사이에 `kernel32!CreateProcessW` 가 끼는지는 안 봤다 `[공백]`.
- `[정적]` (run14) `CreateProcessInternalW +0x7f0 ~ +0x800`: 사본에 4 가 없으면 ProcessFlags `|= 0x200`. `+0x30e7` 에서 `NtCreateUserProcess` (ProcessFlags = `[RSP+0x30]` ← `[RSP+0x110]`).
- `[정적]` (run14) `+0x4672` `CsrClientCallServer` — ApiNumber `0x1001d` (`+0x4655`), 길이 `0x218` (`+0x464f`). `CsrClientCallServer` 는 ntdll 것 (import 표).
- `[정적]` (run29) ntdll `NtContinue` 스텁 `MOV EAX,0x43` → `KiServiceTable[0x43]` = `nt!NtContinue`. 489칸 대조 방법.
- `[정적]` (run33) `PspAllocateProcess +0x710` 이 `OwnerProcessId` (`+0x2d8`) 를 쓴다. 부모 PID `|2`, 인자 `[RSP+0x90]` bit 9 면 안 붙임 (`+0x703 ~ +0x70c`).
- `[정적 읽기 + 추측]` (run33) "그래서 일반 생성에선 `PspProcessClose` 의 주인 PID 비교가 맞기 어려워 보인다." ← **이번에 고쳐진 짐작**
- `[소스]` sample 은 `CreateProcess` 를 안 쓴다 — 부모는 cmd.exe.

---

## 2. 찾은 것

### 2.1 한눈에 — 부모 스레드 길과 csrss 쪽 길

run 문서 "호출 관계" 를 줄였다. 오프셋은 ret. `══▶` 는 시스템 콜(유저 → 커널), `┄┄` 는 **다른 프로세스**로 넘어가는 메시지다.

```
(부모 cmd.exe 스레드, 유저)                                        (run14 + run34)
cmd!ExecPgm  (참고, objdump)                       [정적, 실제 길은 Q19]
 └─ +0x59c  kernel32!CreateProcessW               껍데기, 인자 그대로
      └─ +0x54  KERNELBASE!CreateProcessW         껍데기
           └─ +0x66  KERNELBASE!CreateProcessInternalW
                ├─ (1) +0x30ee  ntdll!NtCreateUserProcess   번호표 0xd1
                │        ══▶ +0x12 SYSCALL  (bit 0 이면 +0x15 INT 0x2e)
                │             → 안내판[0xd1] = nt!NtCreateUserProcess
                │                  └─ +0xc36 PspAllocateProcess  +0x707 bit 9 → +0x710 주인 란
                ├─ (2) +0x4672  ntdll!CsrClientCallServer   ApiNumber 0x1001d
                │        ┄┄ csrss 요청 스레드 (다른 프로세스) ┄┄
                │        csrsrv!CsrApiRequestThread   +0x47f 서버 1 / 아래 0x1d
                │         └─ +0x4e6 (CFG)  basesrv!BaseSrvCreateProcess2
                │              └─ +0x21b  InternalBaseSrvCreateProcess
                │                   └─ +0x17c  csrsrv!CsrCreateProcess
                │                        └─ +0x1d  CsrpCreateProcess → +0x50c CsrInsertProcess
                └─ (3) +0x5bd3  ntdll!NtResumeThread        번호표 0x52
                         ══▶ +0x12 SYSCALL → 안내판[0x52] = nt!NtResumeThread
```

- 이름과 오프셋은 모두 `[정적]` 이다.
- 다만 정도가 다르다:
  - cmd.exe 줄은 Ghidra 가 아니라 `objdump` + PDB 로 읽은 **참고** 다. test.exe 때 정말 이 길인지는 동적 Q19.
  - csrss 쪽 `+0x4e6` 은 **간접 호출**이라, 대상(`BaseSrvCreateProcess2`)은 "표의 값" 으로 정했다. 1번 서버 = basesrv 는 **간접 증거** (2.4).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 앞 접수대가 옆 접수원을 부르는 줄

트리의 `+0x54  KERNELBASE!CreateProcessW` 줄.

- **누가**: `kernel32!CreateProcessW` (rva `0x3bdd0`, 94바이트, 명령 18개)
- **어디서**: 부르는 자리 `+0x4d`, 돌아올 자리 **`+0x54`**. 콜스택에는 `kernel32!CreateProcessW+0x54` 로 보일 자리다.
- **누구를**: IAT 칸(rva `0x8baa8`)에 적힌 함수. 칸 이름은 `api-ms-win-core-processthreads-l1-1-0.dll` 의 `CreateProcessW` 이고, 이게 `KERNELBASE!CreateProcessW` 로 풀린다 (2.3).
- **무엇을 위해**: 받은 인자 열 개를 **바꾸지 않고** 넘기려고. 돌아와서도 결과를 그대로 돌려줄 뿐, 다른 일이 없다. `[정적]`

#### 한 줄 읽기 ② — 주인 란에 무엇을 적을지 정하는 줄

트리의 `PspAllocateProcess  +0x707 bit 9 → +0x710 주인 란` 줄.

- **누가**: `nt!PspAllocateProcess` (run10 의 "가게 짓기" 함수)
- **어디서**: `+0x707`. 이건 호출이 아니라 **비트를 보는** 줄이다.
- **무엇을**: 9번째 인자(ProcessFlags 사본)의 bit 9 가 켜졌나 본다.
  - 바로 앞(`+0x703`)에 "부모 PID `|2`" 를 만들어 둔다.
  - `+0x70c` 에서 bit 9 가 켜졌으면 그걸 버리고 "부모 PID 그대로" 로 바꿔 끼운다.
  - `+0x710` 에서 그 값을 새 장부 `+0x2d8` (`OwnerProcessId`, 주인 란) 에 적는다.
- **무엇을 위해**: 주인 란에 부모 번호를 **그대로** 적을지, **살짝 바꿔(`|2`)** 적을지 고르려고. `[정적]`

#### 한 줄 읽기 ③ — 인사팀이 업무 목록표를 보고 담당을 부르는 줄

트리의 `+0x4e6 (CFG)  basesrv!BaseSrvCreateProcess2` 줄.

- **누가**: `csrsrv!CsrApiRequestThread` (rva `0x9340`) — csrss 안에서 **요청을 받아 나눠 주는 스레드**의 함수
- **어디서**: 부르는 자리 `+0x4e1`, 돌아올 자리 `+0x4e6`. (메시지에 캡처 버퍼가 붙은 갈래면 `+0x651` / `+0x656`.)
- **누구를**: 직접 이름을 부르지 않는다. 서버 구조체 `[+0x28]` 의 표에서 칸 하나를 꺼내 [CFG](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다) 를 거쳐 부른다. 번호가 `0x1001d` 면 그 칸은 `basesrv!BaseSrvCreateProcess2` 다.
- **무엇을 위해**: 부모가 보낸 "새 프로세스 만들었어요" 신청서를 **맞는 담당에게** 넘기려고. `[정적]` (대상은 표의 값)

---

### 2.3 핵심 ① `kernel32!CreateProcessW` 는 껍데기다

#### run14 에서 남아 있던 말

- run14 는 `KERNELBASE!CreateProcessW` 부터 봤다.
- "cmd.exe 가 **이 입구로** 오나? 사이에 `kernel32!CreateProcessW` 가 끼나?" 는 `[추측]`·`[공백]` 이었다.
- 그래서 그림 1 `kb1` 상자 글이 "(정적: 부모가 이 입구로 오는지는 추측)" 이었다.

#### 이번에 본 것 — 인자 열 개를 그대로 `[정적]`

- 함수는 94바이트, 명령 18개. **호출은 딱 하나** (`+0x4d`, ret `+0x54`).
- 인자 열 개:
  - 앞 넷(레지스터)은 아예 안 건드린다.
  - 5 ~ 10번째(스택)는 `+0x7 ~ +0x49` 에서 새 자리로 **한 칸씩 옮기기만** 한다. 값을 바꾸는 줄이 없다.
- 돌아와서는 스택 정리하고 `RET`. 결과(`EAX`)를 그대로 돌려준다.
- 검사·ETW 같은 다른 일도 **없다.**
- export 도 forwarder(다른 DLL 로 가라는 표시)가 아니라 진짜 함수다.
- 비유: 대행 책상(KERNELBASE) 앞에 **앞 접수대**(kernel32)가 하나 더 붙어 있다. 손님 서류를 펴 보지도 않고 옆 접수원에게 그대로 밀어 준다.

#### 어떻게 KERNELBASE 로 가나 — 별명 대조표가 "누가 묻나" 를 본다

- 부르는 IAT 칸의 이름은 `KERNELBASE.dll` 이 아니라 **api-set 이름**(별명)이다: `api-ms-win-core-processthreads-l1-1-0.dll` 의 `CreateProcessW` `[정적]`.
  - api-set 은 [run23 해설](run23-load-dependent-해설.md) 2.5 에서 본 **별명**이다. 실행 때 진짜 DLL 이름으로 바뀐다.
- 결과서는 같은 OS 의 별명 대조표 파일 `apisetschema.dll` 을 직접 읽었다 `[정적: PE 표]`. 이 별명 줄에는 값이 **둘** 있다:
  - 기본 = `kernel32.dll`
  - **묻는 쪽이 `kernel32.dll` 이면** = `kernelbase.dll`
- 새로 짚을 점은 이것 하나다: **같은 별명이라도 "누가 묻느냐" 에 따라 다른 건물로 풀린다.**
  - kernel32 가 물으면 → KERNELBASE. 그래서 이 칸은 `KERNELBASE!CreateProcessW` (export rva `0x7d5f0`, 107B) 로 간다.
  - 다른 가게가 물으면 → kernel32.
  - 비유: 별명 대조표에 "**OO 접수처** → 보통은 kernel32 건물. 단, kernel32 건물이 물어보면 KERNELBASE 건물" 이라고 적혀 있다. 자기 자신을 가리키면 빙빙 돌기만 하니까 그런 칸이 있는 것으로 보인다 `[추측]`.
  - [run23 해설](run23-load-dependent-해설.md) 2.5 에서 ntdll 이 별명을 풀 때 **부모 `BaseDllName` 도 넘긴다** 는 걸 봤다. "누가 묻나" 를 보려면 그게 필요하다는 것과 맞는다 `[추측]`.
- 실행 때 실제로 풀어 주는 건 ntdll 이 PEB 의 ApiSet 표로 한다. 이번에 읽은 건 **같은 OS 의 파일 속 표**다 (run 문서).

#### 그래서 부모 유저 쪽 사슬 (코드 기준) `[정적]`

```
kernel32!CreateProcessW +0x54 → KERNELBASE!CreateProcessW +0x66
  → KERNELBASE!CreateProcessInternalW +0x30ee → ntdll!NtCreateUserProcess
```

#### cmd.exe 쪽 — 참고로만

cmd.exe 는 분석 범위 밖이다. 결과서가 `objdump` + `cmd.exe.pdb` 로 가볍게 본 것만 옮긴다.

- `[정적]` cmd.exe import 표에도 `CreateProcessW`·`CreateProcessAsUserW`·`ResumeThread` 가 **같은 별명**으로 있다.
  - cmd.exe 가 물으면 기본값 `kernel32.dll` 로 풀린다. → **cmd.exe 는 `kernel32!CreateProcessW` 에 닿는다.**
  - 그래서 위 사슬 맨 앞에 cmd.exe 가 붙는다.
- `[정적]` 그 칸을 부르는 대표 자리: **`cmd!ExecPgm`** `CreateProcessW` ret **`+0x59c`** (AsUser 쪽 ret `+0x51b`).
  - `dwCreationFlags` 자리에 상수 **`0x80000`** (`EXTENDED_STARTUPINFO_PRESENT`) 만 싣는다 (`+0x57d`).
  - **`CREATE_SUSPENDED`(4) 는 없다.** ← 2.6 에서 이게 중요해진다.
- `[정적]` `cmd!Start` 는 다르다 (`start` 명령으로 보임 `[추측: 이름]`). 조건에 따라 4 를 켜고, 나중에 `ResumeThread` 를 직접 부른다 (ret `+0xe52`, `+0xfa2`).
- `[추측]` 프롬프트에 test.exe 를 그냥 입력하면 `ExecPgm` 길이다. 실제로 어느 길인지는 동적 Q19.

#### 왜 하는 일도 없는 상자를 그림에 넣었나

run 문서 "그림 반영" 의 이유:

- cmd.exe 가 import 표로 **실제로 닿는 첫 OS 함수**가 이것이다.
- 동적 Q19 에서 `k` 를 찍으면 `kernel32!CreateProcessW+0x54` 로 **보일 자리**다. 그림에 없으면 콜스택과 그림을 맞출 때 한 줄이 빈다.
- 바로 뒤의 `KERNELBASE!CreateProcessW` 도 똑같은 껍데기인데 이미 그림에 있다. 그리고 이 상자는 **모듈 경계(kernel32 → KERNELBASE)** 를 보여 준다.
- 반대로 cmd.exe 상자(`ExecPgm`)는 **안 넣었다.** 분석 범위 밖이고, Ghidra 가 아니라 `objdump` 로 읽은 참고라서다. 글(flow.md 2.2)에만 둔다.

---

### 2.4 핵심 ② `0x1001d` = 1번 창구의 `0x1d` 번 업무 = `BaseSrvCreateProcess2`

#### 새 말 — csrss 서버 DLL

- csrss 는 run14 에서 본 **인사팀 사무실**이다. 관리사무소(커널)와 따로 있는 유저 쪽 프로세스다.
- **서버 DLL** = csrss 프로세스 안에 올라와서 **실제 업무를 맡는 DLL** 들. 이번에 연 둘:
  - `csrsrv.dll` — 요청을 받아 나눠 주는 쪽 (`CsrApiRequestThread`), 그리고 csrss 의 프로세스 명부를 관리하는 쪽 (`CsrCreateProcess`, `CsrInsertProcess`)
  - `basesrv.dll` — 프로세스 만들기 같은 "기본(base) 업무" 쪽 (이름과 표 칸 이름들로 보아)
- 서버 DLL 마다 **번호**가 있고, 각자 **업무 목록표**를 csrss 에 건다 (아래).
- 비유: 인사팀 사무실 안에 **번호 붙은 창구**가 여럿 있다. 0번 창구는 사무실 자체(csrsrv), 1번 창구는 기본 업무 창구(basesrv). 창구마다 벽에 **업무 목록표**가 붙어 있다.

#### 새 말 — ApiNumber

- **ApiNumber** = 부모가 `CsrClientCallServer` 로 보내는 메시지에 적는 **"무슨 업무인지" 번호**. 메시지 `+0x30` 칸에 들어간다 `[정적]` (ntdll `CsrClientCallServer +0x51`).
- 이번에 짜임이 코드로 보였다 `[정적]`:
  - **위 16비트 = 서버 DLL 번호**, **아래 16비트 = 그 서버 안의 업무 번호**.
  - `0x1001d` → 위 16비트 `1` = **1번 서버**, 아래 16비트 `0x1d` = **`0x1d` 번 업무**.
- 비유: 신청서 맨 위에 "**1번 창구 - `0x1d` 번 업무**" 라고 적어 낸다.

#### csrss 쪽에서 번호를 나누는 줄 `[정적]` (`csrsrv!CsrApiRequestThread`)

1. `+0x47f` 번호를 오른쪽으로 16칸 밀어 **위 16비트**만 남긴다 → 서버 번호.
2. `+0x483` / `+0x487` 서버 번호가 **6 미만**인지 본다.
3. `+0x493` 전역 `CsrLoadedServerDll[서버 번호]` 에서 그 서버의 구조체를 꺼낸다. 비었으면 `+0x49a` 오류 길.
4. `+0x49c` **아래 16비트**를 꺼내, 서버 구조체 `[+0x20]`(시작 번호)를 뺀 값이 `[+0x24] − [+0x20]` 보다 작은지 본다 (`+0x4ac`·`+0x4ae`). 즉 "목록표 안의 번호인가".
5. `+0x4c4 ~ +0x4c8` 구조체 `[+0x28]`(업무 목록표)의 그 칸을 꺼내 → `+0x4e1` 에서 부른다 (ret `+0x4e6`).

- 비유: 접수 직원이 신청서 번호를 반으로 쪼갠다. 앞 반으로 **창구**를 찾고, 뒤 반으로 그 창구 벽의 **업무 목록표 몇째 줄**인지 찾아 담당을 부른다.
- ntdll 쪽 (`CsrClientCallServer`) 은 보내기 전에 `+0x27` 에서 **bit 28 을 지운다**. `0x1001d` 에는 bit 28 이 원래 없다 `[정적]`.
- 참고: 결과서는 "`+0x483` 에서 오른쪽으로 밀기" 라고 적었는데, run 문서가 `.asm` 과 맞춰 보니 **한 줄 어긋나 있었다**. 밀기는 `+0x47f`, 6 비교가 `+0x483` 이다. 뜻은 같다. 이 글은 run 문서(`.asm`)를 따랐다.

#### basesrv 의 업무 목록표 `[정적]`

- basesrv 는 시작할 때(`ServerDllInitialization`, rva `0x2f50`) 자기 서버 구조체를 채운다:
  - `+0x153 ~ +0x15a` `[+0x28]` ← **`BaseServerApiDispatchTable`** (rva `0xf550`) — 업무 목록표
  - `+0x149` `[+0x20]` ← 0 — 시작 번호
  - `+0x17f` `[+0x24]` ← **`0x1f`** — 끝 번호
  - → 칸은 `0 ~ 0x1e`, **31개**. `0x1d` 는 그 안이다.
  - 칸 이름(`[+0x20]` = 시작 번호 등)은 공개 문서의 `CSR_SERVER_DLL` 모양에서 빌린 것이다 `[추측]`. 오프셋과 값은 `[정적]`.
- 결과서가 PE 파일에서 그 표를 직접 읽었다. **`0x1d` 칸 (rva `0xf638`) = `BaseSrvCreateProcess2`** (rva `0x6ee0`, 815B) `[정적]`.
  - 앞뒤 칸: `[0x1c]` `BaseSrvNlsUpdateCacheCount`, `[0x1e]` `BaseSrvCreateActivationContext2`.
  - `[0]` 은 `BaseSrvCreateProcess` — 옛 V1 메시지를 V2 로 바꿔서 **같은 `BaseSrvCreateProcess2`** 를 부른다.

#### `BaseSrvCreateProcess2` 가 하는 일 — 대표만 `[정적]`

1. 메시지 안의 CLIENT_ID 로 새 프로세스·스레드 **손잡이를 csrss 가 직접 연다.**
   - CLIENT_ID = 프로세스 번호·스레드 번호 짝으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
   - `NtOpenProcess` (ret `+0xb3`), `NtOpenThread` (ret `+0x1ba`).
   - 그 사이에 `NtQueryInformationProcess` 셋으로 **부모·자식 관계를 검사**한다. 실패면 `0xc0000022`.
2. `InternalBaseSrvCreateProcess` (ret `+0x21b`) 안에서 차례로:
   - SxS 쪽 간접 호출 (ret `+0x100`, **조건부**). 표는 `BaseSrvRegisterSxS` 가 채우고, sxssrv.dll 이 그 함수를 import 한다.
   - **`csrsrv!CsrCreateProcess`** (ret `+0x17c`) → `CsrpCreateProcess` (ret `+0x1d`):
     - 프로세스 구조 만들기 (`CsrAllocateProcess` call `+0x9e`)
     - 예외 포트 걸기
     - **`CsrInsertProcess`** (ret `+0x50c`) — csrss 의 프로세스 목록에 넣기
     - 첫 스레드도 등록 (`CsrCreateThread` call `+0x556`)
   - 성공했으면 `UserNotifyProcessCreate` 간접 호출 (ret `+0x1bc`, 값이 있을 때). 표는 `BaseSetProcessCreateNotify` 가 채우고, winsrvext.dll 이 import 한다.
3. VDM 인자가 있을 때만 `BaseSrvUpdateVDMSequenceNumber` (call `+0x26b`).

- 한 줄로: **"csrss 에 새 프로세스와 첫 스레드를 등록"** 이 코드로 맞다.
- 비유: 인사팀 1번 창구 직원이 새 가게 **열쇠(손잡이)를 직접 받아 들고**, "정말 이 신청 가게의 자식 가게 맞나" 확인한 뒤, 인사팀 **명부**에 가게와 첫 직원을 올린다. 잘 끝나면 (알릴 곳이 적혀 있을 때) 다른 쪽에도 소식을 넘긴다.
- run14 해설에서 "인사팀 명부에 올라가야 일이 매끄럽게 돈다" 고 짐작했던 그 **명부에 올리는 줄**이 `CsrInsertProcess +0x50c` 다.

#### "1번 서버 = basesrv" 는 왜 간접 증거인가 — 단계별로

**근거** (`[정적]`)

1. 코드 어디에도 "basesrv = 1" 이 **박혀 있지 않다.**
2. 번호는 csrss 가 받은 **명령줄**의 `ServerDLL=이름[:함수],번호` 에서 온다.
   - `CsrParseServerCommandLine` 이 그걸 읽어 `CsrLoadServerDll(이름, 함수, 번호)` 를 부른다 (ret `+0x58d`).
   - 번호 0 은 csrsrv 자신이다 (`+0x4f7`).
3. 그 명령줄 문자열은 smss 가 레지스트리에서 만든다고 알려져 있다 `[추측: 일반 지식]`. 정적으로는 못 본다.

**그래서 대신 이렇게 맞춰 봤다** (`[정적]`)

- kernel32·KernelBase 가 `CsrClientCallServer` 에 싣는 **다른 번호 10개**를 모았다: `0x10014`, `0x1000c`, `0x1000d`, `0x1001c`, `0x10016`, `0x10012`, `0x10015`, 그리고 bit 28 을 지우면 `0x1001e`·`0x10007`·`0x10011`.
- 이 번호들의 **아래 16비트가 basesrv 표의 칸 이름과 전부 맞는다.** 예: `0x1001c` 를 싣는 곳은 `CsrBasepNlsUpdateCacheCount`, basesrv `[0x1c]` 칸은 `BaseSrvNlsUpdateCacheCount`.
- 그리고 **위 16비트는 모두 1** 이다.

**그러니** — `0x1001d` 의 1 도 basesrv 로 본다. 다만 이건 "다 맞아떨어진다" 까지다.

**아직 확인 아님** — 확정은 동적: csrss 안에서 `CsrLoadedServerDll` 의 `[1]` 칸 이름이 basesrv 인지 본다 (Q19, 낮음).

- 비유: 창구 번호판이 안 보인다. 그런데 "1번 창구" 앞으로 낸 다른 신청서 10장이 **전부** 기본 업무 창구의 목록표와 한 줄씩 들어맞는다. 그러면 1번 창구는 기본 업무 창구라고 봐도 되겠다. 번호판을 직접 보는 건 나중 일이다.

#### 이 일은 부모 스레드가 아니다 — 그래서 그림엔 노란 메모

- `BaseSrvCreateProcess2` 는 **csrss 프로세스의 요청 스레드**에서 돈다. 부모 cmd.exe 스레드가 아니다.
- 부모는 `CsrClientCallServer` 로 보내고 **답을 기다린다**고 알려져 있다 `[추측: 일반 지식, 기다리는 줄은 안 봄]` → Q66.
- 그림 1 은 "한 그림 = 한 스레드" (부모 cmd.exe 스레드) 규칙이라, 함수 상자로 못 넣는다. **노란 메모 하나**(`nd2 -.- wc`)로 붙였다.

---

### 2.5 핵심 ③ ntdll 스텁 둘 — 번호표 `0xd1`·`0x52`

#### 번호표 대조 — run29 와 같은 방법 `[정적]`

| ntdll 스텁 | rva / 크기 | 싣는 번호표 | 안내판 `KiServiceTable[번호]` | 앞뒤 칸 |
|---|---|---|---|---|
| `NtCreateUserProcess` | `0x1627c0` / 24B | **`0xd1`** | `nt!NtCreateUserProcess` (rva `0xa01340`) | `[0xd0]` NtCreateTransactionManager, `[0xd2]` NtCreateWaitCompletionPacket |
| `NtResumeThread` | `0x1617f0` / 24B | **`0x52`** | `nt!NtResumeThread` (rva `0xa2d600`) | `[0x51]` NtQuerySection, `[0x53]` NtTerminateThread |

- 읽는 법은 [run29 해설 2.4](run29-continue-start-해설.md#24-창구까지--ntdll-스텁과-번호-0x43) 의 `NtContinue` (`0x43`) 와 같다.
  - 안내판 `KiServiceTable` (rva `0xdadc0`, 4바이트 칸), 칸 수 `KiServiceLimit` = **489**.
  - 489칸 **전부**가 `_index.tsv` 의 함수 시작과 맞았다. 읽는 방법이 틀리지 않았다는 확인이다.
  - 앞뒤 칸 이름도 그럴듯하다 (위 표).
- 그래서 "ntdll `NtCreateUserProcess` 가 커널 `NtCreateUserProcess` 로 간다" 는 **이름이 같아서 짐작한 게 아니라 표로 확인한 것**이다.
- 그림 1 의 굵은 화살표 라벨에서 "(추측)" 이 빠진 이유다.

#### 그런데 `SYSCALL` 하나가 아니다 — 옆 갈래 `INT 0x2e`

두 스텁의 모양은 똑같다 `[정적]`. 순서대로:

1. `+0x0` 첫 인자를 `R10` 으로 옮긴다.
2. `+0x3` 번호표를 `EAX` 에 싣는다.
3. `+0x8` 메모리 주소 **`0x7ffe0308`** 의 한 바이트를 보고, `+0x10` 에서 bit 0 이 켜져 있으면 `+0x15` 로 뛴다.
4. 안 켜져 있으면 **`+0x12 SYSCALL`** → `+0x14 RET`.
5. 켜져 있으면 **`+0x15 INT 0x2e`** → `+0x17 RET`.

새 말 둘:

- **공유 사용자 데이터** = 커널이 모든 프로세스에 **똑같은 주소로 보여 주는 공용 페이지**로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 주소 `0x7ffe0308` 은 그 데이터의 `+0x308` 칸이다 (run 문서). 칸 이름은 `SystemCall` 로 보인다 `[추측]` → Q66.
  - 비유: 건물 로비의 **공용 게시판**. 모든 가게 관리실이 같은 자리에서 읽는다. 게시판 한 칸에 "오늘은 옛 쪽문 쓰세요" 표시가 있나 본다.
- **`INT 0x2e`** = 소프트웨어 인터럽트로 커널에 들어가는 **옛 방식의 문**으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 번호표(`EAX`)는 똑같이 들고 간다.
  - 비유: 관리사무소의 **옛 쪽문**. 들고 가는 번호표는 같고, 들어가는 문만 다르다.
  - 그 bit 0 이 어떤 설정에서 켜지는지는 근거 문서에 없다. **모른다.**

정리:

- 즉 "바로 `SYSCALL` 하나" 가 아니라 **갈래가 하나 붙어 있다** `[정적]`.
- 보통은 `SYSCALL` 쪽일 것이다 `[추측]`. 어느 쪽인지는 실행 때 `0x7ffe0308` 값에 달려서 동적 (Q19).
- 같은 모양이 run29 의 `NtContinue` 스텁에도 있다. run29 해설 2.4 표에 `+0x8` 줄로 이미 적혀 있었다. 그때는 `SYSCALL` 만 따라갔다.
- 남는 것: `INT 0x2e` 쪽으로 가면 커널 입구가 `KiSystemServiceCopyEnd` 사슬과 같은가? `SYSCALL` 쪽이면 필요 없는 질문이라 동적 Q19 에 같이 붙였다.

---

### 2.6 핵심 ④ ProcessFlags bit 9 의 길 — 같은 비트였다

**답부터** `[정적]`: KERNELBASE 가 켜는 bit 9 = run33 의 `[RSP+0x90]` bit 9. **같은 비트**다. → Q65 의 bit 9 부분이 풀렸다.

#### 비트 하나가 어디를 지나 어디서 읽히나

run 문서의 줄을 우리 말로 옮겼다. 오프셋은 각 함수 안.

```
KERNELBASE!CreateProcessInternalW                                  (run14 + run34)
 │  +0x7f6  주문서 사본에 4(CREATE_SUSPENDED) 있나?
 │  +0x800  없으면 ProcessFlags 칸 [RSP+0x110] 의 bit 9 켜기
 │  +0x306b → +0x30b3  그 칸을 7번째 인자 자리로
 └─ +0x30ee  ntdll 스텁 ══▶ SYSCALL 0xd1
      nt!NtCreateUserProcess                  7번째 인자 = [RBP+0x7d0]
       │  +0x1b0 / +0x1b6  "켜면 안 되는 비트" 검사 (마스크 0xff317838)
       │                   → bit 9 는 마스크에 없음 = 켜도 되는 비트
       │  +0x2cd (bit 6 지움), +0xdea (bit 10 켬) — bit 9 는 안 건드림
       │  +0xbf6 → +0xbfc  그대로 9번째 인자 자리로
       └─ +0xc36  PspAllocateProcess          9번째 인자 = [RSP+0x8b0]
             │  +0x2ba / +0x2c1  [RSP+0x90] 에 사본  (run33 이 본 [RSP+0x90] 이 여기서 생김)
             │  +0x625  R8D ← [RSP+0x90]
             │  +0x6f1 / +0x6f9  부모 장부 +0x1d0 (부모 PID)
             │  +0x703  "부모 PID | 2" 만들어 둠
             │  +0x707  R8D 의 bit 9 켜졌나?  → +0x70c 켜졌으면 "|2" 버리고 부모 PID 그대로
             └─ +0x710  새 장부 +0x2d8 (OwnerProcessId) 에 적기
```

한 단계씩:

1. **KERNELBASE** (run14 + 다시 확인): 주문서에 `CREATE_SUSPENDED`(4) 가 **없으면** bit 9 를 켠다. 그 칸이 커널로 가는 **7번째 인자**가 된다.
2. **커널 `NtCreateUserProcess`**: 7번째 인자를 받아 **검사만 하고 그대로 넘긴다.**
   - 새 말 — **검사 마스크** = "이 중 하나라도 켜져 있으면 거절" 하는 비트 목록. 여기선 `0xff317838` (비트 3·4·5·11·12·13·14·16·20·21·24~31). 걸리면 `+0x1ce` 에서 `0xc000000d` (잘못된 인자) 로 돌려보낸다 `[정적]`.
     - 비유: 신청서 체크칸 중 "**손님은 체크하면 안 되는 칸**" 목록. bit 9 는 그 목록에 없다 → 손님(호출자)이 체크해도 되는 칸이다.
   - 이 함수 안에서 그 칸을 다시 쓰는 줄은 둘뿐인데 (`+0x2cd` bit 6 지움, `+0xdea` bit 10 켬), **bit 9 는 안 건드린다.** `+0xdea` 는 `PspAllocateProcess` 에 넘긴 **뒤**다.
   - 그리고 `+0xbf6`/`+0xbfc` 에서 같은 값을 `PspAllocateProcess` 의 **9번째 인자**로 넘긴다.
3. **`PspAllocateProcess`**: 9번째 인자를 `+0x2c1` 에서 `[RSP+0x90]` 에 복사해 둔다. **run33 이 본 그 칸이 여기서 생긴다.**
   - `+0x625` 에서 다시 꺼낼 때까지, 이 칸이나 원래 인자를 **바꾸는 줄이 없다** `[정적]`. (`+0x668 ~ +0x670` 에서 원래 인자로 다시 읽어 되쓰는 갈래도 같은 값이다.)
4. **`+0x707`**: 그 bit 9 로 주인 란 값을 고른다 (2.2 한 줄 읽기 ②).

#### 표로 정리 `[정적]`

| 부모가 준 `dwCreationFlags` | KERNELBASE bit 9 | 새 장부 `OwnerProcessId` (`+0x2d8`) |
|---|---|---|
| `CREATE_SUSPENDED`(4) **없음** | 켬 (`+0x800`) | **부모 PID 그대로** |
| `CREATE_SUSPENDED`(4) 있음 | 안 켬 | 부모 PID `\|2` |

- cmd.exe `ExecPgm` 은 `0x80000` 이라 4 가 없다 (2.3). → 그 길로 만든 test.exe 는 **`|2` 없는 쪽**이다 `[정적: 두 코드의 상수, 실제 길은 동적 Q19]`.
- 비유 (run33 이어서): 손님이 "첫 직원 대기시켜 둬" 를 **주문 안 했으면**, 장부 "주인" 란에 부모 가게 번호를 **그대로** 적는다. 주문**했으면** 번호를 **살짝 바꿔(`|2`)** 적는다.

#### 이 비트를 읽는 곳은 하나로 보인다 `[정적]`

- 커널에서 bit 9 를 읽는 곳은 **`PspAllocateProcess +0x707` 하나**로 보인다.
- ProcessFlags 를 받는 다른 함수 셋은 **다른 비트만** 본다:
  - `PspValidateCreateProcessProtection` (`NtCreateUserProcess +0xa4f`·`+0xb95` 에서 부름): bit 2 만.
  - `PspInsertProcess` (`NtCreateUserProcess +0xfaf` 에서 부름): bit 1 만 (`+0x318`). 사본을 `PspImplicitAssignProcessToJob` (`+0xce`) 로 넘기고, 거기선 bit 10·bit 0 만.
- `PspAllocateProcess` 안의 다른 "bit 9 보기" 줄 (`+0x765`, `+0x145d`) 은 **다른 변수**의 bit 9 다. 헷갈리지 말 것.

---

### 2.7 run33 짐작 정정 — "맞기 어렵다" 가 아니라 "보통은 맞을 수 있다"

이번 바퀴에서 제일 조심해서 읽을 곳이다. [run33 해설 2.6](run33-first-resume-해설.md) 의 마지막 부분이 **틀렸다.**

#### run33 은 무엇을 짐작했나

run33 해설 2.6 "다만 일반 생성에선 이 비교가 맞기 어려워 보인다" `[정적 읽기 + 추측]`:

1. `PspProcessClose` 는 도장(bit 15)이 **없을 때**, "닫히는 프로세스의 `OwnerProcessId` == 닫는 프로세스 PID" 인지 본다 (`+0x22 ~ +0x29`). 맞고, 닫는 쪽이 지워지는 중이면 `PsTerminateProcess(0xc000010a)` (ret `+0x4c`).
2. `OwnerProcessId` 는 "부모 PID `|2`" 로 적힌다. bit 9 일 때만 `|2` 를 안 붙인다.
3. **bit 9 가 무슨 뜻인지 몰랐다.** 그래서 `|2` 가 붙는 쪽을 **보통**으로 짐작했다.
4. PID 는 4 의 배수라 `|2` 가 붙으면 아무 PID 와도 안 맞는다 `[추측: 일반 지식]`.
5. → "보통 생성(test.exe)에선 이 비교가 안 맞아, 도장이 없어도 그냥 return 할 것" 이라고 썼다.

#### 무엇이 틀렸나 — 3번

- 이번에 bit 9 의 정체가 나왔다: KERNELBASE 가 **`CREATE_SUSPENDED` 가 없을 때** 켜는 비트 (2.6).
- 보통 생성(cmd.exe `0x80000`)엔 4 가 없다 → bit 9 **켜짐** → **`|2` 안 붙음** → `OwnerProcessId` = **부모 PID 그대로**.
- 그러니 3번이 거꾸로였다. **보통 생성이 `|2` 없는 쪽**이고, `|2` 가 붙는 건 `CREATE_SUSPENDED` 로 만든 **특별한 경우**다.

#### 고친 결과 — 단계별로

1. 보통 생성: 주인 란 = 부모 PID 그대로 → 부모가 손잡이를 닫을 때 "같은 PID" 비교가 **맞을 수 있다** `[정적]`.
2. `CREATE_SUSPENDED` 생성: 주인 란 = 부모 PID `|2` → 어느 PID 와도 안 맞음 → 이 종료 길이 **막힌다** (PID 는 4 의 배수 `[추측: 일반 지식]`).
3. 그러니 이 장치의 뜻도 다시 읽힌다 `[추측]` (run 문서):
   - "**보통 생성**이면, 첫 스레드가 한 번 풀리기 전에 부모가 죽으며 손잡이를 닫을 때 자식도 같이 끝낸다."
   - "**일부러 멈춰 만든**(`CREATE_SUSPENDED`) 자식은 그 길에서 뺀다."
   - 결과서는 예로 "csrss 등록이 실패해 부모가 정리하는 경우" 를 들었다 `[추측]`.

- 여기서 "멈춤" 이 두 가지라는 걸 구분하면 덜 헷갈린다:
  - (가) KERNELBASE 가 **늘** 거는 멈춤 — `ThreadFlags` = 1 ([run14 해설](run14-create-process-internal-해설.md) 2.4). 보통 생성에서도 부모가 csrss 알림 등을 끝내고 `+0x5bd3` 에서 풀어 줄 때까지 멈춰 있다.
  - (나) 호출자가 **주문한** 멈춤 — `CREATE_SUSPENDED`. 이땐 KERNELBASE 가 풀어 주지 않는다 ([run14 해설](run14-create-process-internal-해설.md) 2.8).
  - 이번 결과로 보면 `PspProcessClose` 의 지름길은 **(가) 만 있는 보통 생성**을 대상으로 하고, **(나)** 는 `|2` 로 빼 두는 모양이다 `[추측]`.
- 비유 (run33 이어서): 손님이 대기 주문을 **안 한** 가게는 장부 "주인" 란에 부모 번호가 **그대로** 있다. 그래서 반납 창구의 "주인이세요?" 질문에 진짜 부모가 "예" 라고 답할 수 있다. 대기 주문을 **한** 가게만 번호가 살짝 바뀌어 있어서, 아무도 "예" 를 못 한다.

#### 무엇이 그대로인가

- run33 의 나머지는 바뀌지 않았다:
  - 도장(bit 15)을 읽는 곳은 둘 (`PsMultiResumeThread +0x45`, `PspProcessClose +0xf`), 켜졌으면 `+0x1b` 로 바로 return.
  - Q64 의 짐작 "test.exe 땐 도장이 먼저 찍혀 바로 return 할 것" `[추측]`.
- 다만 **그 짐작의 이유가 바뀌었다.**
  - 전: "PID 비교가 안 맞아서 return" 쪽으로 기울었다.
  - 후: PID 비교는 맞을 수 있다. 그래도 순서상 부모가 `NtResumeThread` (KERNELBASE `+0x5bd3`) 로 **도장을 먼저 찍고**, 그 뒤에 손잡이를 닫으면 `+0x1b` 로 바로 return 한다 `[추측: 순서]`.
  - 즉 test.exe 시동 길에서 "아무 일도 안 일어날 것" 이라는 결론은 그대로지만, **기대는 곳이 도장 하나뿐**이 됐다. 실제로 서는지는 그대로 Q64.
- 고친 곳: run33 문서 맨 위에 정정 한 줄, flow.md 2.2 `PspProcessClose` 절 (옛 줄은 취소선, 아래 정정). run33 **해설** 맨 위에 정정 표시를 넣었다 (2.6 그 문장 옆과 4절 Q65 줄에도 "→ run34 정정"). 자세한 정정은 이 절이다.

---

### 2.8 덤 — 도구 이야기 두 줄

run 문서 "도구 메모" 에서 하네스 제안으로 올라온 것:

- `run_ghidra.sh refs` 에 **import 이름**을 주면 외부 주소로 풀려 엉뚱한 줄이 나온다. 이번엔 `pefile` + 바이트 스캔으로 대신했다.
- **함수 포인터 표 안을 읽는 명령이 없다.** `KiServiceTable` (run29) 과 `BaseServerApiDispatchTable` (이번) 둘 다 임시 스크립트로 읽었다. `table` 같은 명령이 있으면 한 줄로 끝난다는 제안이다.
- 둘 다 제안만이고, 하네스는 안 고쳤다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 그림 1 맨 앞 | `KERNELBASE` 상자부터 | **`KERNEL32` 상자 새로** — `KERNEL32!CreateProcessW` `(정적)` 점선, 간선 `+0x54` → `kb1` |
| 그림 1 `kb1` 글 | "(정적: 부모가 이 입구로 오는지는 추측)" | "(정적: 107B 껍데기. API-set 으로 kernel32 에서 옴)" |
| 그림 1 `kb2` 글 | "ThreadFlags 늘 1" | + "CREATE_SUSPENDED 없으면 ProcessFlags bit 9 켬" |
| 그림 1 `nd1`·`nd3` 글 | "시스템 콜 스텁 추측" | `[정적]` "EAX 0xd1 / 0x52 = KiServiceTable 의 그 칸" |
| 그림 1 굵은 화살표 둘 | "시스템 콜 (추측)" | "`+0x12 SYSCALL` (정적. `INT 0x2e` 갈래도)" |
| 그림 1 `nd2` + 노란 메모 | "csrss 알림" | "ApiNumber 0x1001d" + 노란 메모 `wc`: csrss 다른 스레드의 `basesrv!BaseSrvCreateProcess2` (한 그림 = 한 스레드라 상자 대신 메모) |
| 2.1 `NtCreateUserProcess` | ProcessFlags 이야기 없음 | `[정적]` 검사 마스크 `0xff317838` 에 bit 9 없음, 그대로 `PspAllocateProcess` 9번째 인자로. 유저 → 커널 문 `SYSCALL 0xd1` |
| 2.1 `PspAllocateProcess` | — | `[정적]` `OwnerProcessId` 쓰기 `+0x6f1 ~ +0x710`, bit 9 = ProcessFlags, 읽는 곳은 이 한 줄로 보임 |
| 2.2 `KERNELBASE!CreateProcessW` | `[추측]` "cmd.exe 는 이 입구(또는 kernel32 거쳐)" `[공백]` | 취소선 → `[정적]` **`kernel32!CreateProcessW +0x54` 를 거쳐 옴.** 실행 확인은 Q19 |
| 2.2 새 절 셋 | 없음 | `kernel32!CreateProcessW` (+ cmd.exe 쪽 참고), ntdll 스텁 둘, csrss 쪽 `BaseSrvCreateProcess2` — 모두 `[정적]` |
| 2.2 `CreateProcessInternalW` bit 9 줄 | "커널에서 이 비트의 뜻은 안 봤다" | 취소선 → `[정적]` `+0x707` → `OwnerProcessId` `\|2` 여부 |
| 2.2 `PspProcessClose` 절 | `[정적 읽기 + 추측]` "일반 생성에선 이 비교가 맞기 어려워 보인다" | 취소선 → **정정** `[정적]`: 보통 생성은 부모 PID 그대로라 맞을 수 있다. `\|2` 는 `CREATE_SUSPENDED` 때만 |
| 4장 공백 표 2.2 | Q41 줄 넷 + Q65 줄 | 넷 다 **(해결됨, 정적)**. Q65 는 bit 9 부분 해결, 남은 둘. 새 줄 **Q66** |
| 4.1 표 | — | `KERNEL32 → KERNELBASE` 줄, csrss 노란 메모 줄 추가. `kb1`·`ntdll ==> nt` 줄 갱신 |
| 기준 | 정적 근거 run9 ~ run33 | run9 ~ run34. basesrv·csrsrv·cmd PDB 추가. API-set 표·`BaseServerApiDispatchTable` 도 PE 파일에서 직접 읽었다는 줄 |
| run33 문서 | — | 맨 위에 **정정 한 줄** |

### 큰 그림에서 어떻게 이어지나

- **그림 1 (부모 cmd.exe 스레드) 의 유저 쪽이 코드로 처음부터 끝까지 이어졌다.**
  - cmd.exe → `kernel32` → `KERNELBASE` → ntdll 스텁 → (번호표) → 커널. 모든 칸에 이름과 오프셋이 있다.
  - 남은 건 "**정말 그 길로 갔나**" 하나다 — 동적 Q19 에서 `nt!NtCreateUserProcess` 입구 `k` 한 번으로 볼 수 있다. 맞으면 `KERNEL32` 상자와 `kb1` 이 `[관찰]` 이 된다.
- **csrss 알림이 무엇을 하는지 알게 됐다.** run14 의 "인사팀에 등록" 이 짐작이 아니라 `CsrInsertProcess` 줄이 됐다.
  - 다만 다른 프로세스의 일이라 그림 1 의 대표 지점으로는 메모 한 줄이다.
- **run33 의 `PspProcessClose` 이야기가 한 칸 정리됐다.** 시동 길에서 "아무 일도 안 일어날 것" 은 그대로지만, 그 근거가 **도장 하나**로 좁혀졌다. 그래서 Q64 (동적) 에서 볼 것이 "그때 도장이 켜져 있나" 로 더 분명해졌다.
- 다음 바퀴는 **Q45** (plan.md). 정적 묶음이 끝나면 동적은 **Q19** 부터다. 이번 바퀴가 Q19 에 붙인 (a) ~ (e) 를 그때 같이 본다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 때 부모가 **정말** `cmd!ExecPgm +0x59c` → `kernel32!CreateProcessW +0x54` → `KERNELBASE!CreateProcessW +0x66` → `CreateProcessInternalW +0x30ee` 길로 오나 | 코드에 길이 있는 것까지만 정적이다. cmd.exe 는 범위 밖이라 `objdump` 참고뿐 | **Q19** (a) 동적: `nt!NtCreateUserProcess` 입구 (cmd.exe 문맥) 에서 긴 `k` |
| 그때 7번째 인자(ProcessFlags) 의 bit 9 가 정말 켜져 있나. cmd.exe 가 넘긴 `dwCreationFlags` 가 정말 `0x80000` 인가 | 실행 때 값이다 | **Q19** (b) 동적: 같은 자리에서 `dwo(@rsp+0x38)` (입구 기준 오프셋은 `[추측: 오프셋 계산]`) |
| 새 장부 `OwnerProcessId` 가 정말 부모 PID 그대로 (`\|2` 없음) 인가 | 실행 때 값이다 | **Q19** (c) 동적: `nt!PspAllocateProcess+0x710` 직전 `rax`, 지난 뒤 `dq <새 EPROCESS>+0x2d8 L1` |
| ntdll 스텁이 `SYSCALL` 쪽인가 `INT 0x2e` 쪽인가. `INT 0x2e` 면 커널 입구가 `KiSystemServiceCopyEnd` 사슬과 같은가 | `0x7ffe0308` bit 0 은 실행 때 값이다 | **Q19** (d) 동적: `db 0x7ffe0308 L1` |
| csrss 쪽이 정말 `CsrApiRequestThread+0x4e6` (또는 `+0x656`) → `BaseSrvCreateProcess2` 로 서나, 메시지 번호가 `0x1001d` 인가, 부모 `+0x4672` 와 순서. **1번 서버 = basesrv** 확정 | 다른 프로세스(csrss)이고, 서버 번호는 명령줄 값이라 정적으론 간접 증거까지 | **Q19** (e, 낮음) 동적: csrss 문맥에서 `basesrv!BaseSrvCreateProcess2` 입구 `k`, `poi(@rcx+0x30)`, `dq csrsrv!CsrLoadedServerDll L6` 의 `[1]` 이름. csrss 는 보호 프로세스라 그 프로세스 문맥이 필요하다 |
| `OwnerProcessId` 를 쓰는 **다른** 곳 `NtSetInformationProcess +0x1ecd` 는 어느 정보 종류인가. `ObCloseHandleTableEntry +0x7a9` 간접 호출 앞에서 `PspProcessClose` 로 넘기는 인자 (인자 이름 확인) | 이번 질문(bit 9)의 바깥이라 안 봤다 | **Q65** (정적, 좁혀짐 — 남은 둘) |
| 스텁이 보는 `0x7ffe0308` 칸의 진짜 이름 (`_KUSER_SHARED_DATA.SystemCall` 로 보임 `[추측]`) | 공유 사용자 데이터의 PDB 타입을 안 봤다 | **Q66** (a) (정적, 작음) |
| 부모가 `CsrClientCallServer` 안에서 **csrss 답을 기다리나**. 그래서 csrss 등록이 부모의 `NtResumeThread` 보다 먼저 끝나나 | 지금은 `[추측: 일반 지식]`. ntdll 안쪽 보내고 받는 줄은 안 봤다 | **Q66** (b) (정적, 작음) |
| `PspProcessClose` 가 test.exe 시동 중 `main` 전에 서나. 서면 도장(bit 15) 이 켜져 바로 return 하나 | 누가 언제 손잡이를 닫는지는 코드로 못 정한다. 이번 정정으로 **기대는 곳이 도장 하나**가 됐다 (2.7) | **Q64** (동적, Q19 와 같은 실행) |

---

## 5. 더 읽기

- run 문서: [`run34-parent-leftovers.md`](run34-parent-leftovers.md) — 특히 "정적 — 질문 1 ~ 4", "run33 `PspProcessClose` 와 이어 보기", "그림 반영", "정정"
- 결과서: [`_workspace/sast/29_parent-leftovers_result.md`](../../_workspace/sast/29_parent-leftovers_result.md) — 질문별 답 1 ~ 4 (간접 증거 번호 10개 전체 목록은 질문 2), "동적분석으로 확인해야 할 것", "스크립트 문제"
- 요청서: [`_workspace/sast/29_parent-leftovers.md`](../../_workspace/sast/29_parent-leftovers.md)
- flow.md:
  - [그림 1 — 부모 cmd.exe 스레드](flow.md) (`KERNEL32` 상자, `kb1`·`kb2`·`nd1`·`nd2`·`nd3`, 노란 메모 `wc`)
  - 2.1 `NtCreateUserProcess` 의 ProcessFlags 줄, `PspAllocateProcess` 의 `OwnerProcessId` 쓰기 줄
  - 2.2 부모 쪽 전체 순서 트리, 새 절 `kernel32!CreateProcessW`·ntdll 스텁·csrss 쪽, `PspProcessClose` 절의 정정
  - 4장 공백 표 2.2 줄, 4.1 표 `KERNEL32 → KERNELBASE`·csrss 노란 메모 줄
- 앞 해설:
  - [run9 해설](run9-create-user-process-해설.md) (`NtCreateUserProcess`, 시스템 콜)
  - [run14 해설](run14-create-process-internal-해설.md) 2.3 (`KERNELBASE!CreateProcessW` 껍데기), 2.4 (bit 9 를 켜는 `+0x800`), [2.6](run14-create-process-internal-해설.md#26-3-그-사이-부모가-하는-일--검사--csrss-알림--후처리) (csrss 알림 ②), 2.8 (`CREATE_SUSPENDED` 면 건너뜀)
  - [run23 해설](run23-load-dependent-해설.md) 2.5 (api-set 별명 풀이), [run25 해설](run25-dll-entry-해설.md) 2.5 (kernel32 import 의 별명 102개)
  - [run29 해설 2.4](run29-continue-start-해설.md#24-창구까지--ntdll-스텁과-번호-0x43) (번호표 `0x43` 대조, 같은 방법)
  - [run33 해설](run33-first-resume-해설.md) 2.5 · 2.6 (`PspProcessClose`, `OwnerProcessId` — 이번에 2.6 끝부분이 정정됨)

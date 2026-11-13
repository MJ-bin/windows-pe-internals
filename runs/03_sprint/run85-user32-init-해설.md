# run85 user32-init — 해설

> 이 글은 [run85 문서](run85-user32-init.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.
>
> **고쳐 읽기 (run86)**: "run86 에서 다룸" 으로 남긴 자리 ("조심할 곳" 5, 2.12 끝, 2.13 끝, 2.15 정정) 의 답은 [run86 해설 2.13](run86-gdi32-init-해설.md) 에 있다 — 결과서 sast/82 의 짐작 둘은 틀렸다 (`GdiDllInitialize` 를 부르는 곳은 user32 `+0x996` 하나, 두 번 불리는 건 `GdiProcessSetup` / 첫 win32k syscall 은 `NtGdiInit` 이 아니라 gdi32full `DllMain +0x2e` **`NtGdiInit2` (`0x12ee`)**). 그리고 1장 그림의 "gdi32 먼저 `[추측]`" 과 2.14 의 "user32 `DllMain` 전에 이미 올라와 있을 것 `[추측: 의존하는 쪽이 먼저]`" 은 — **올라오는 것 (매핑) 은 맞고** (gdi32full 도 gdi32 가 정적으로 가져와 같이 올라온다 `[정적: PE 표]`), **`DllMain` 차례는 user32 → gdi32 → gdi32full → user32 순환이라 로더가 실행 때 정한다** (동적 Q162 1). → [run86 해설 2.5](run86-gdi32-init-해설.md) · [run86 문서](run86-gdi32-init.md) "결과서와 다르게 적은 곳 · 보탠 것" ⑨ · "정정". 하나 더: 3절 · 4절의 "PEB 의 user32 칸 셋" 은 **넷** 이다 (`AppCompatFlagsUser +0x2d0` 포함 — 짐 지도 7.2).

- 대기열: **Q150a** — PE_emulator 요청 28 (user32 · gdi32 초기화 최소 길, "GUI 벽") 의 **첫 조각, user32 · win32u 쪽** (`_workspace/plan.md`)
  - Q150 (요청 28 전체) 은 둘로 나눴다.
    - user32 · win32u 쪽 = **Q150a** (이번, run85).
    - gdi32 · gdi32full 쪽과 "창 안 만드는 프로세스가 진입점까지 가는 **최소 조건 표 둘**" = **Q150b** (다음, run86 — sast/83).
  - 다음 항목: **Q150b** → Q151a → Q151b → Q152 → Q142 → [멈춤] (Q104 VM 단계 앞).
  - 이번에 남은 것이 간 곳 (4절):
    - 실행해 봐야 아는 것 → **Q162** (VM — user32 를 올리는 프로세스가 있어야 본다. **test.exe 로는 못 봐서** Q104 와 따로 둔 새 행, notepad 류 / 새 GUI 샘플은 사용자 결정).
    - 작은 정적 ① ~ ⑦ → **Q161**.
    - gdi32 쪽 → **run86** (Q150b).
- 근거:
  - run 문서 [`run85-user32-init.md`](run85-user32-init.md) (가장 중요)
  - 요청서 [`_workspace/sast/82_user32-init.md`](../../_workspace/sast/82_user32-init.md) · 결과서 [`82_user32-init_result.md`](../../_workspace/sast/82_user32-init_result.md)
    - 판정 **분석성공 (최소 길 지도 수준)**. 질문 1 ~ 4 모두 `.asm` 줄로 답했다. user32 · win32u 를 **처음 analyze** 했다. 스크립트 문제 없음.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 2.5 · **3장 새 절 "user32 시작 함수"** · 3장 콘솔 절 한 줄 · 4장 · 5장 (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음, 마흔한째판)
  - 같이 한 일: **run84 뒷정리** — run84 의 "정적으로 남은 것" 에 **Q159**, "새 샘플 필요" 에 **Q160** 번호를 붙였다.
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없다.
  - `[관찰]` 로 적힌 값은 flow-writer 가 run44 덤프 읽기 로그에서 **다시 읽은 것** 이다 (2.10 · 2.14).

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 user32 를 안 올린다.**
   - `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`.
   - import 표: test.exe → `dll1` · `KERNEL32`, dll1 → `dll2` · `KERNEL32`, dll2 → `KERNEL32` `[정적+샘플]`.
   - run44 P2 사진의 모듈은 6개 (`test.exe` · `ntdll` · `KERNEL32` · `KERNELBASE` · `dll1` · `dll2`) `[관찰]`.
   - 그래서 이번 길은 **흐름도 그림 밖** 이다. "user32 를 import 하는 **다른** 프로세스라면" 의 이야기다.
   - 누구 이야기냐면 — PE_emulator 의 실험 대상이다. GUI DLL 을 import 하는 샘플 4,655 개를 첫 회차에서 뺐고, 콘솔 프로그램도 user32 를 import 하는 게 많다 (요청서 "배경").
2. **결론은 거의 다 "보통 프로세스" 기준이다.**
   - 보통 프로세스 = csrss 같은 **서버 프로세스가 아니고** (`gfServerProcess == 0`), **보호 프로세스도 아닌** (PEB `+3` 보호 비트 0) 프로세스.
   - 서버 갈래 · 보호 프로세스 갈래는 이름과 조건만 적는다.
3. **"user32 가 `NtOpenSection(SharedSection)` 을 부른다" 는 틀렸다.**
   - PE_emulator 가 본 장면이다. 정상 길에선 이 syscall 이 user32 때 **안 나온다** (2.6).
4. **칸 이름은 많이 `[추측]` 이다.**
   - user32 · win32u PDB 는 공개 심볼만이라 **타입이 0개** 다 (`llvm-pdbutil` 0 records).
   - 그래서 `SHAREDINFO` · `SERVERINFO` · `psi` 같은 이름은 `[이름 추측]` 이다.
   - 대신 **칸 위치 · 크기 · 읽는 줄은 `.asm` 의 `[정적]`** 이다.
5. **gdi32 쪽은 이번에 안 봤다.**
   - user32 시작 함수는 `+0x996` 에서 `GdiDllInitialize` 를 **무조건** 부른다. 그 안은 짝 바퀴 **run86** (Q150b) 이다.
   - 결과서 sast/82 끝 "Q150b 로 넘길 것" 에 gdi32 쪽 짐작이 둘 있다. run85 문서는 이 둘을 옮기지 않았다. 이 글도 사실로 쓰지 않는다 → **run86 에서 다룸**.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 전원 켜기 = DLL 시작 함수 (DllMain) 부르기, 이유 1 = 프로세스에 처음 붙음, PE 진입점 (`AddressOfEntryPoint`) → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 2.3
- 꼬리 점프, 시작 함수 앞의 봉인 스티커 (`_DllMainCRTStartupForGS2`) → [run68 해설](run68-dll-init-rest-해설.md) 2.10 · [run59 해설](run59-init-leftovers-해설.md) 2.3
- csrss = 이웃 **인사팀 사무실**, 서버 DLL = 사무실 안 **창구**, 1번 창구 = basesrv → [run14 해설](../02_sprint/run14-create-process-internal-해설.md) 2.6 · [run34 해설](../02_sprint/run34-parent-leftovers-해설.md)
- csrss 연결 = 사무실에 거는 **전화**, ALPC 포트 = **전화선 끝**, `SharedSection` = 사무실 **게시판**, 포트 섹션 = 사무실과 **같이 쓰는 메모장**, `CsrPortHeap` = 메모장 안 **작은 개인 창고**, `CsrPortMemoryRemoteDelta` = "사무실 번지 ↔ 우리 번지" **옮겨 적기**, `CsrPortHandle` = 커널이 써 주는 **전화선 손잡이** → [run68 해설](run68-dll-init-rest-해설.md) 2.4
- 게시판 원본은 csrss 가 세션 시작 때 만든다 → [run71 해설](run71-named-obj-dir-해설.md)
- `_SYSTEM_BASIC_INFORMATION` (클래스 0, `0x40`B, **동네 기본 규격표**) → [run68 해설](run68-dll-init-rest-해설.md) 2.3
- 크리티컬 섹션 (열쇠) → [run68 해설](run68-dll-init-rest-해설.md) 2.7
- `.mrdata` = **유리 덮개 칠판**, 덮개 열고 닫기 (`LdrProtectMrdata` → `NtProtectVirtualMemory 0x50`) → [run43 해설](run43-baggage-checklist-해설.md) 2.9 · [run60 해설](run60-func-table-mrdata-해설.md)
- ETW = **방송국**, TraceLogging 등록 = **채널 올리기** → [run62 해설](run62-tp-etw-init-해설.md)
- syscall 스텁, 번호부, 번호 `0x1000` 이상 = **옆 건물 (그래픽 쪽) 번호부**, `KiConvertToGuiThread` → [run56 해설](run56-syscall-args-해설.md) 2.4 "덤" · 2.9
- win32k = 커널 도서관의 **다른 동**, **창 그리기 담당 부서** → [run33 해설](../02_sprint/run33-first-resume-해설.md) · [run51 해설](run51-eprocess-later-해설.md) 2.9
- KnownDll 길 = **공용 창고** 에서 꺼내 오기 → [run21 해설](../02_sprint/run21-load-dll-internal-해설.md) 2.4
- CFG 간접 호출 = **교환원** 을 거쳐 부르기 → [run25 해설](../02_sprint/run25-dll-entry-해설.md)
- IFEO = exe 이름별 실행 옵션 레지스트리 자리 → [run10 해설](../02_sprint/run10-allocate-process-해설.md), 활성화 컨텍스트 = 매니페스트 설정 묶음 → [run22 해설](../02_sprint/run22-map-snap-dep-해설.md)
- 사진 P1a · P2 → [run44 해설](run44-baggage-snap-해설.md) 2.8

이번에 처음 나오는 말 — **user32 · win32u**, **USERSRV (3번 창구)**, **캡처 버퍼**, **`SHAREDINFO` · `psi`**, **`gfServerProcess`**, **커널 콜백 (`KernelCallbackTable`)**, **GUI 스레드** (그리고 짧게 **IME**, **DPI**) — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, PEB = 가게 **안내판**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**.
- ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**, `.mrdata` = **유리 덮개 칠판**.
- KERNELBASE · KERNEL32 = 가게에 들인 **건물**. 시작 함수 = 건물 **전원 켜기**. DLL 전역 = 건물마다 가진 **수첩**.
- csrss = 이웃 **인사팀 사무실**. kernelbase 가 전원을 켜면서 그 사무실 **1번 창구** 에 처음 전화를 걸었다 (run68). 그때 전화선이 깔리고, 사무실 게시판을 우리 땅에 한 판 펼쳤다.
- 이번에 새로 붙는 그림:
  - **user32 = 가게에 새로 들이는 "창 담당 건물"**. 전원을 켜면서 **같은 전화선** 으로 인사팀 사무실 **3번 창구** 에 한 통화를 건다.
  - 그 통화의 답장이 **공용 안내 묶음** (`SHAREDINFO`) 이다. 묶음 첫 줄은 "서버 정보판은 저기 있어요" 하는 **화살표** (`psi`) 다.
  - 커널 콜백 = 관리사무소가 일하다가 가게 쪽 사람을 **거꾸로 불러내는** 것. 그때 보는 게 안내판에 붙은 **내선 번호표** (`KernelCallbackTable`) 다.
- flow.md 3장 새 절의 비유는 그림이 조금 다르다 ("새로 문을 연 지점 · 본사 · 통화선"). 그림만 다르고 사실은 같다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| ① user32 `DllMain` 이 꼭 받아야 하는 바깥 답은 | **둘뿐**: `NtQuerySystemInformation(0)` 성공, 그리고 csrss **3번 창구 (USERSRV)** 연결 답. 나머지는 실패해도 FALSE 가 아니다 `[정적]` (2.3 · 2.4 · 2.8) |
| ② csrss 연결은 어떻게 하나 | ntdll `CsrClientConnectToServer(…, 3, 0x248B 버퍼, …)`. kernelbase 가 이미 연결했으면 (`CsrPortHandle ≠ 0`) **`NtAlpcSendWaitReceivePort` 한 번** 으로 끝 `[정적]` (2.4) |
| ③ `gSharedInfo` · `gpsi` 는 어디서 오나 | 그 답 0x248B 의 `+8` 부터 0x240B 가 `gSharedInfo`, 첫 8B (`psi`) 가 `gpsi`. `psi` 는 **읽을 수 있는 0x1b68B 이상** 이면 0 으로 채워도 길 끝까지 간다. 널이면 접근 위반 `[정적]` (2.5) |
| ④ PE_emulator 가 본 `NtOpenSection(SharedSection)` 은 | **정상 길이 아니다.** user32 엔 없고 ntdll 첫 연결 길 (`CsrpConnectToServer +0x1b6`) 에만 있다 → 그 순간 `CsrPortHandle` 이 0 이었다. 왜 0 인지는 `[공백]` (2.6) |
| ⑤ `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 | `DllMain` 에 **없다**. 서버 프로세스 갈래 (`ClientThreadSetup`) 뿐 `[정적]` (2.11) |
| ⑥ GUI 스레드로 바뀌는 때 | 그 스레드의 **첫 win32k syscall** 때 커널이 바꾼다. `DllMain` 본체의 win32k syscall 다섯은 **모두 조건부**. 무조건 부르는 첫 후보는 `+0x996 GdiDllInitialize` 쪽 → run86 `[정적, 커널은 이름만]` (2.12) |
| ⑦ win32u 이름표 | 1499개, `0x1000 ~ 0x15da`, `ms/disassemble/win32u/_syscalls.tsv` `[정적]` (2.13) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**DLL 전원 켜기** 구간이다 (flow.md 2.7). 다만 test.exe 에는 없는 건물 (user32) 이야기라 **옆길** 이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ kernel32 들이기 + 전원 켜기
 │         │     └─ KERNELBASE 전원 켜기 → csrss 1번 창구에 첫 전화 (run68)   ← 전화선 손잡이 CsrPortHandle 이 여기서 생김
 │         ├─ exe 의 import 들이기 (test.exe 는 dll1 · dll2)
 │         │     (user32 를 import 하는 exe 라면 user32 · gdi32 … 도 여기서 — KnownDll 길 [추측: 실행 안 봄])
 │         └─ (2.7) dll 전원 켜기 — LdrpCallInitRoutine
 │               ├─ (test.exe) dll2 → dll1
 │               └─ (user32 를 올리는 프로세스라면) gdi32 먼저 [추측] → user32 전원 켜기   ← 이번 (run85)
 │                                                                  └─ GdiDllInitialize 안   ← run86
 └─ … → NtContinue (은퇴) → exe 진입점 → main
```

- user32 전원 켜기는 **은퇴 앞** 이다 (flow.md 3장 새 절: "2.7 의 `LdrpCallInitRoutine` 이 부르는 user32 진입점 (이유 1, 은퇴 앞)").
- "gdi32 먼저" 는 `[추측: 2.7 초기화 순서 — 의존하는 쪽이 먼저]` 다 (run 문서 "보탠 것" ⑥, 2.14).

### 왜 이 질문이 지금 왔나

- PE_emulator 는 은퇴 전 DLL 의 `DllMain` 을 **진짜로 부르고**, syscall 은 nt 엔진 · 팀원 몸이 대답한다. csrss 도 win32k 도 **없다** (요청서 "배경").
- 그런데 user32 를 import 만 해도 초기화에서 막혔다. 그래서 첫 실험 회차에서 GUI DLL 을 직접 import 하는 샘플 4,655 개를 뺐다.
- 지금 막히는 장면 `[관찰 — PE_emulator 흉내]`:
  - user32 `DllMain` → `NtOpenSection("\Sessions\1\Windows\SharedSection")` 이 `0xc000003a` (팀원 몸) → `0xc0000142` (DLL 초기화 실패).
- 질문은 하나로 모인다: **user32 `DllMain` 이 TRUE 를 돌려주려면, 어떤 syscall 에 어떤 답, 어떤 공유 데이터가 있어야 하나.**
- 사용자 결정 (2026-11-12): 첫 답은 **"최소 길 지도" 수준**. 더 깊은 것은 "다음 요청 후보" 로.

### 무엇이 궁금했나

요청서 질문 넷.

1. **user32 `DllMain` 의 줄기** — 부르는 함수 · syscall 을 차례대로.
   - (a) ntdll `CsrClientConnectToServer` 를 부르나, 인자는, run68 의 첫 연결과 무엇이 다른가.
   - (b) `NtOpenSection(SharedSection)` 은 user32 안인가, ntdll 안인가.
   - (c) win32u 쪽 syscall (`NtUserProcessConnect` 등) 과 필요한 답.
   - (d) `DllMain` 이 FALSE 를 돌려주는 자리.
2. **공유 데이터 최소 칸** — `gSharedInfo` · `gpsi` 를 어디서 받나, `DllMain` 이 **읽는 칸만**.
3. **GUI 스레드로 바뀌는 때** — `DllMain` 안인가, 첫 win32k syscall 때인가.
4. **win32u syscall 번호표** — 이번 길 것 (싸면 전체).

### 바퀴 전에 알던 것

- `[정적]` (run68) ntdll `CsrClientConnectToServer` 의 **첫 연결** 길. kernelbase 가 `\Sessions\1\Windows` · 서버 번호 1 (BASESRV) · 연결 정보 8B 로 부르고, 안에서 `CsrpConnectToServer` 가 syscall 6번으로 `CsrPortHandle` · PEB `+0x380` · `+0x88` · `+0x98` 을 채운다.
- `[정적]` (run68) 같은 함수의 `+0xb5/+0xba CsrpClientConnectToServer` 는 "정상이면 안 탄다 — csrss 응답 상태가 음수일 때만" (run68 해설 2.4 "덤").
- `[정적]` (run71) 게시판 원본은 csrsrv `CsrSrvCreateSharedSection +0x1bf NtCreateSection` 이 만든다.
- `[정적]` (run56) syscall 번호의 비트 12 (`0x1000`) 가 서 있으면 win32k 번호부. win32k 번호면 `KiConvertToGuiThread` 를 거친다.
- `[관찰 — PE_emulator 흉내]` 위의 `NtOpenSection` `0xc000003a` → `0xc0000142`.
- `[추측]` (PE_emulator) win32u syscall 은 `0x1000` 대 번호인데 이름표가 없었다.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 문 하나, 몸통 하나, 꼭 맞아야 할 답 둘

**새 말 — user32 · win32u**

- **user32** = 창 · 메시지 · 입력 같은 **화면 쪽 일** 을 맡는 유저 DLL 이다 `[추측]` (일반적으로 알려진 설명). 비유: 가게에 들이는 **창 담당 건물**.
- **win32u** = 커널의 그래픽 부서 (win32k) 로 거는 **syscall 스텁만** 모아 둔 DLL 이다. 스텁마다 번호 하나 넣고 `SYSCALL` 하는 24B 짜리다 `[정적]` (2.13). 비유: **옆 건물 단축번호 버튼판**.

run 문서 "호출 관계" 그림을 줄여 그렸다. 라벨은 ret 자리. 모두 `[정적]`.

```
user32!UserClientDllInitialize (0x51120, PE 진입점)               이유 1
 ├─ +0x1d  _DllMainCRTStartupForGS2                     봉인 스티커, 1 반환
 └─ +0x2c  JMP ▶ _UserClientDllInitialize (0x6ba84)       EAX = DllMain 반환
      ├─ 준비: 임계구역 ×10 · InitializeUMHandleList ×2                     실패 → FALSE
      ├─ +0x1d8  NtQuerySystemInformation(0, 0x40)        ── SYSCALL 0x36       ★ 답 ①
      ├─ +0x1f8  InitializeNtdllUserPfn                    .mrdata 표 셋 (프로세스당 한 번)
      ├─ +0x24f · +0x287  세션 번호 → "\Sessions\1\Windows"
      ├─ +0x2b1  ntdll!CsrClientConnectToServer(…, 3, 0x248B, …)                ★ 답 ②
      │            이미 연결이면 → … → NtAlpcSendWaitReceivePort  ── SYSCALL 0x8e (한 번)
      │   +0x30b ~ +0x387  답 → gSharedInfo · gpsi   ·   +0x38e psi 첫 바이트 읽기
      │   +0x3fc  PEB+0x58 ← apfnDispatch
      ├─ InitInstrument · TraceLogging ×3 · GDI 스케일 · DPI 설정      실패해도 계속
      ├─ +0x99d  GdiDllInitialize(hmodUser32, 1)                          → run86
      ├─ +0x9bd  InitProcessDpiAwareness                               조건부 win32k
      └─ +0xa8c  반환 = GdiDllInitialize 결과가 음수 아니면 1
```

- **문은 하나, 몸통도 하나다.**
  - PE 진입점 `UserClientDllInitialize` (rva `0x51120`, 49B) 는 껍데기다. 이유 1 이면 봉인 스티커를 붙이고 (`+0x18/+0x1d`), `+0x2c` 에서 **꼬리 점프** 로 몸통 `_UserClientDllInitialize` (rva `0x6ba84`, 2745B) 에 넘긴다 `[정적]`.
  - 몸통의 `EAX` 가 그대로 `DllMain` 반환값이다.
  - `+0xb5/+0xbc DisableThreadLibraryCalls` 를 불러서, 스레드가 생기고 없어질 때 (이유 2 · 3) 는 안 불린다 `[정적]`.
- **꼭 맞아야 하는 바깥 답은 둘** 이다 `[정적]`.
  - ① `NtQuerySystemInformation(0)` 성공 (2.3).
  - ② csrss 3번 창구 연결 답 (2.4 · 2.5).
- 나머지 syscall (레지스트리 읽기 · `.mrdata` 열고 닫기 · ETW 등록 · 조건부 win32k) 은 **실패해도 FALSE 가 아니다** `[정적]` (2.8).
  - 단 하나 조심할 것: ntdll 코드 `RtlInitializeNtUserPfn` 은 **프로세스당 한 번만** 성공한다 (2.9).

비유: 창 담당 건물의 전원을 켤 때, 바깥에서 꼭 받아 와야 하는 서류는 두 장이다 — 관리사무소의 **동네 기본 규격표**, 그리고 인사팀 3번 창구의 **공용 안내 묶음**. 나머지 서류는 없어도 전원은 켜진다.

### 2.2 한 줄씩 읽는 법

run 문서 "호출 관계" 그림에서 두 줄을 골라 끝까지 읽어 본다.

#### 줄 1: `├─ (6) +0x2b1  ntdll!CsrClientConnectToServer(디렉터리, 3, 버퍼 0x248B, 0x248, &gfServerProcess)    +0x2b8 JS → FALSE`

- **누가**: user32 몸통 `_UserClientDllInitialize`.
- **어디서**: CALL 이 `+0x2aa`, 다음 줄이 `+0x2b1`. 콜스택엔 `_UserClientDllInitialize+0x2b1` 로 보일 자리다.
- **누구를**: ntdll `CsrClientConnectToServer` (rva `0xdb7a0`). **run68 에서 kernelbase 가 부른 바로 그 함수** 다.
- **인자** (`.asm` 으로 읽음 — Ghidra 는 이 import 서명을 인자 4개로 잘못 붙여서 다섯째가 디컴파일에 안 보인다) `[정적]`:
  - `RCX` = 객체 디렉터리 이름 (`\Sessions\1\Windows` 또는 `\Windows`, 2.8)
  - `EDX` = **`3`** — 서버 번호 (USERSRV)
  - `R8` = 0 으로 채운 **0x248B** 버퍼 주소 (호출자 스택 `RSP+0x60`)
  - `R9D` = `0x248` — 길이 (**값** 이다, 포인터 아님)
  - `[RSP+0x20]` = `&gfServerProcess` (user32 rva `0xda9e8`)
- **무엇을 위해**: 인사팀 사무실 **3번 창구** 에 인사하고 **공용 안내 묶음** 을 받아 오려고 (2.4 · 2.5).
- **실패면**: 반환이 음수면 `+0x2b8 JS` → `DllMain` FALSE (FALSE 자리 6번, 2.7).

**새 말 — USERSRV (3번 창구)**: csrss 안 서버 DLL 마다 번호가 있다. run68 의 kernelbase 는 **1번 (BASESRV)** 에 걸었고, user32 는 **3번 (USERSRV)** 에 건다 `[정적: 인자 EDX = 3]`. 비유: 같은 인사팀 사무실의 **다른 창구**.

#### 줄 2: `└─ +0xf2  NtAlpcSendWaitReceivePort(CsrPortHandle, 0x20000, 메시지, 0, 메시지, &0x3b8, 0, 0)   ── SYSCALL 0x8e`

- **누가**: ntdll `CsrClientCallServer` (rva `0xdbdd0`). 줄 1 의 함수가 → `CsrpClientConnectToServer` → 여기로 내려온다.
- **어디서**: CALL `+0xed`, 돌아올 자리 `+0xf2`.
- **누구를**: syscall `0x8e` `NtAlpcSendWaitReceivePort` (ntdll rva `0x161f60`).
- **인자** `[정적]`: 포트 = `CsrPortHandle` (run68 첫 전화 때 받은 손잡이), 플래그 `0x20000`, **보내는 메시지 = 받는 메시지 = 같은 버퍼**, 받는 길이 `0x3b8`.
- **무엇을 위해**: 이미 깔린 전화선으로 "보내고 → 답 기다리고 → 받기" 를 **한 syscall** 로 한다.
  - 이름 풀이 (Send · Wait · Receive) 는 `[추측]` (일반적으로 알려진 설명). "같은 버퍼로 보내고 같은 버퍼로 받는다" 는 `[정적]`.
- **답이 맞으려면** (2.4 끝): ① syscall 성공 ② 메시지 `+0x34` (상태) = 0 ③ 캡처 버퍼 `+8` 부터 `SHAREDINFO` 모양.

---

### 2.3 ★ 답 ① — 동네 기본 규격표, user32 도 한 장

- `_UserClientDllInitialize +0x1d1/+0x1d8` → `NtQuerySystemInformation(0, 버퍼, 0x40, NULL)` — syscall `0x36` `[정적]`.
  - 클래스 0 = `SystemBasicInformation`. run68 에서 kernelbase · kernel32 가 각자 받던 그 **동네 기본 규격표** 다 (run68 해설 2.3).
- 실패 (음수) 면 `+0x1df JS` → **FALSE** (FALSE 자리 4번) `[정적]`.
- 성공하면 버퍼 `+0x28` (`MaximumUserModeAddress`) 을 user32 수첩 `gHighestUserAddress` (rva `0xda9f8`) 에 옮겨 적는다 (`+0x1e5 ~ +0x1ec`) `[정적]`.
- PE_emulator 쪽 — run68 에서 kernelbase 를 위해 정리한 그 대답 (길이 `0x40`, 채울 칸) 이 여기서도 그대로 통할 것이다 `[추측]` (같은 클래스 · 같은 길이라서). run 문서 데이터 칸 표에 오른 이 답의 칸은 `+0x28` 하나다.

비유: 창 담당 건물도 전원을 켜면서 관리사무소에 **동네 규격표** 를 한 장 받아, 그중 "우리 땅 끝 번지" 한 줄을 자기 수첩에 베낀다.

---

### 2.4 ★ 답 ② — 3번 창구에 거는 "둘째 전화"

#### 갈림길은 하나 — 전화선 손잡이가 있나

ntdll `CsrClientConnectToServer` 안에서 (`[정적]`):

1. `+0xa4 CMP CsrPortHandle, 0` — 전화선 손잡이가 이미 있나 본다.
2. **0 이 아니면** (보통 길): `+0xab JZ` 를 안 타고 → **`+0xb5/+0xba CsrpClientConnectToServer`** (rva `0xdb990`).
3. **0 이면** (첫 연결): `+0xab JZ → +0xfe` → **`+0x113/+0x118 CsrpConnectToServer`** (rva `0xdbfe4`) — run68 의 syscall 6번 길.

| | 첫 전화 (run68) | 둘째 전화 (이번) |
|---|---|---|
| 누가 | kernelbase `_KernelBaseBaseDllInitialize +0x2d2/+0x2d9` | user32 `_UserClientDllInitialize +0x2aa/+0x2b1` |
| 창구 (서버 번호) | 1 (BASESRV) | **3 (USERSRV)** |
| 연결 정보 | 8B | **0x248B** (전부 0) |
| 그때 `CsrPortHandle` | 0 | **0 아님** (첫 전화가 남김) |
| 타는 길 | `CsrpConnectToServer` — 게시판 열기 · 메모장 만들기 · 전화 걸기 · 게시판 펼치기 … | **`CsrpClientConnectToServer`** — 같은 선으로 한 통화 |
| syscall | 6 | **`NtAlpcSendWaitReceivePort` 1** |

- 둘째 전화는 게시판 (공유 섹션) · 포트 · 뷰를 **다시 안 만든다** `[정적]`.
- `CsrPortHandle` 은 첫 전화 때 **커널이 써 주는** 칸이다. `NtConnectPort` 의 출력 칸이기 때문이다 (`CsrpConnectToServer +0x2d3 LEA RCX,[CsrPortHandle]` → `+0x2df/+0x2e4`, run68) `[정적]`. 이게 2.6 의 열쇠다.

#### run68 해설 2.4 "덤" 과 어긋나나? — 아니다

- run68 해설 2.4 끝 "덤" 은 "둘째 전화 (`CsrpClientConnectToServer`) 는 정상이면 안 건다" 였다.
- 그건 **kernelbase 첫 전화 안** 에서의 이야기다. 첫 연결 뒤 응답 상태가 음수일 때만 (`+0x11c` · `+0x121 JL → +0xad`) 그 줄로 간다 — 여전히 맞다.
- 이번에 더한 것: **같은 줄이 user32 의 둘째 연결에선 보통 길** 이다 (run 문서 "정정": "이전 run 이 틀린 것은 없다", flow.md 2.5 run68 글머리에 한 줄).

#### 둘째 전화 안 — 다섯 걸음 `[정적]`

`CsrpClientConnectToServer` (rva `0xdb990`), 라벨은 ret:

| 걸음 | 자리 | 하는 일 |
|---|---|---|
| 1 | `+0x45` | `CsrAllocateCaptureBuffer(1, 0x248)` — 캡처 버퍼 (아래 새 말) 를 `CsrPortHeap` 에서 빌림. 할당 실패면 `0xc0000017` |
| 2 | `+0x5c` · `+0x6e` | `CsrAllocateMessagePointer` → 메시지 `+0x48` 에 포인터. 메모리 복사 (호출자 버퍼 0x248B → 캡처 버퍼). 메시지 `+0x40` ← 서버 번호 3, `+0x50` ← 길이 |
| 3 | `+0x84` | **`CsrClientCallServer(메시지, 캡처, API 0, 0x18)`** → 안에서 `+0xf2 NtAlpcSendWaitReceivePort` (줄 2) |
| 4 | `+0x9a` | 음수가 아니면 메모리 복사 (캡처 버퍼 → 호출자 버퍼) |
| 5 | `+0xa2` | `CsrFreeCaptureBuffer` |

**새 말 — 캡처 버퍼**: 메시지 본문에 다 안 들어가는 큰 짐을 같이 보내려고, csrss 와 **같이 쓰는 메모장 (포트 뷰) 안** 에서 잠깐 빌리는 칸이다 `[정적: 힙 = CsrPortHeap, 포트 뷰 안 — 결과서]`. 비유: 같이 쓰는 메모장 한 귀퉁이를 잘라 만든 **답장 봉투**. user32 는 빈 용지 (0x248B, 전부 0) 를 봉투에 넣어 보내고, 3번 창구가 채워서 돌려준다.

#### 주소 옮겨 적기 — 이번엔 "우리 번지 → 사무실 번지" 로 보냈다가 되돌린다

run68 에선 사무실이 준 번지를 **우리 번지로** 고쳐 적었다. 이번엔 반대 방향도 있다 (flow-writer `.asm`, 결과서와 다르게 적은 곳 ②) `[정적]`:

- 보내기 전: 메시지 `+0x28` ← 캡처 버퍼 주소 + `CsrPortMemoryRemoteDelta` (`CsrClientCallServer +0x81 ~ +0x90`). 캡처 버퍼 안 포인터 목록마다 가리키는 칸에 delta 를 더한다 (`+0x9f ~ +0xb9`).
- 돌아오면 거꾸로 되돌린다 (`+0xfa ~ +0x12a`).
- 그래서 csrss 는 **자기 쪽 주소** 를 받는다.
- `CsrAllocateMessagePointer` 는 캡처 버퍼 안 빈 자리를 4B 맞춤으로 잘라 주고, 그 포인터 칸 위치를 목록에 적는다 (`+0x4 ~ +0x29`).
- PE_emulator 쪽 — 엔진이 캡처 버퍼에 답을 쓸 땐 이 delta 를 빼서 **이 프로세스 주소로** 써야 할 것이다 `[추측: 엔진 쪽 — delta 가 0 이면 그대로]`.

비유: 같은 메모장이라도 사무실 벽과 우리 벽에 붙은 자리가 다르다. 보낼 땐 "우리 벽 몇 번 칸" 을 "사무실 벽 몇 번 칸" 으로 고쳐 쓰고, 답이 오면 다시 우리 벽 번지로 고친다.

#### 엔진이 줄 답 — 셋 `[정적]`

`CsrClientCallServer` 의 반환 규칙 (결과서): syscall 이 음수면 `+0x131` 에서 그 값을 메시지 `+0x34` 에 적고, 아니면 메시지 `+0x34` 를 그대로 돌려준다. 그래서:

1. `NtAlpcSendWaitReceivePort` **성공**
2. 메시지 `+0x34` (상태) = **0**
3. 캡처 버퍼 0x248B 의 **`+8` 부터 `SHAREDINFO` 모양** `[이름 추측]` — 첫 8B 가 `psi` (2.5)

곁가지 (결과서) — 이 길에서 갈라지는 셋: 보안 프로세스 (`+0x1e JNZ`), 보호 프로세스 (`+0x69 JNZ` — 서버 번호 2 일 때만 `0xc0000022` 로 거절, 이번 연결은 서버 번호 0 의 API 라 통과), 서버 자신 (`+0x76 JZ` → `+0x182` 간접 `CsrServerApiRoutine`). 보통 프로세스는 셋 다 안 탄다.

---

### 2.5 ★ 답장 봉투 안 — `gSharedInfo` · `gpsi` · `psi` 세 칸

#### 봉투 0x248B 의 모양 `[정적]` (이름은 `[추측]`)

| 봉투 안 자리 | 크기 | 어디로 | 뜻 |
|---|---|---|---|
| `+0x0 ~ +0x7` | 8 | `DllMain` 은 안 읽음 | `[추측]` 개수 · 버전 |
| `+0x8 ~ +0x247` | `0x240` | **`gSharedInfo`** (user32 rva `0xda350`) 로 통째 복사 (`+0x30b ~ +0x37c`) | `SHAREDINFO` 모양 `[이름 추측]` |

- `gSharedInfo` 는 정확히 0x240B 다. rva `0xda350 ~ 0xda590` 이고, 바로 뒤 `0xda590` 이 `gpsi` 다 (결과서) `[정적]`.
- 그다음 `gSharedInfo` 의 **첫 8B** 를 `gpsi` (rva `0xda590`) 에 한 번 더 적는다 (`+0x380 ~ +0x387`) `[정적]`.

**새 말 — `SHAREDINFO` · `psi`**

- **`SHAREDINFO`** (`[이름 추측]`) = 3번 창구가 답장에 적어 주는 **공용 안내 묶음**. 핸들 표 · 메시지 표 류도 들어 있을 것이다 `[이름 추측]`. `DllMain` 은 첫 칸만 쓰고 나머지는 베끼기만 한다.
- **`psi`** (`SERVERINFO*` `[이름 추측]`) = 묶음 **첫 줄**. "서버 정보판은 저기 있어요" 하는 **화살표** (주소) 다. `gpsi` 는 그 화살표의 사본이다.

#### `DllMain` 이 화살표 끝에서 읽는 칸 — 셋뿐 `[정적]`

| `psi` 아래 | 크기 | 읽는 줄 | 켜지면 | 0 이면 |
|---|---|---|---|---|
| `+0x0` 비트 `0x4` | 1 | `+0x38e TEST byte ptr [RAX],4` | IME 표 로드 `[뜻 추측]` | IME 건너뜀 |
| `+0x8bc` 비트 `0x20` | 1 | `InitProcessDpiAwareness +0xa8` | DPI 판정 → win32k syscall 가능 | DPI 쪽 win32k syscall 안 감 |
| `+0x1b66` | 2 | `GetDpiForCurrentProcess +0x13` (위 비트가 켜졌을 때만) | 시스템 DPI 캐시 `[이름 추측]` | — |

- **IME** = 한글 · 한자처럼 글자를 조합해 넣는 **입력기** `[추측]` (일반적으로 알려진 설명).
- **DPI** = 화면 확대 비율 (글자 · 그림을 몇 배로 그리나) `[추측]` (일반적으로 알려진 설명).
- 그래서 **최소** 는: `psi` 가 가리키는 곳이 **0 으로 채운 읽기 가능 `0x1b68`B** 면 길 끝까지 간다 `[정적]` (run 문서 결론).

#### 널이면 — FALSE 가 아니라 넘어진다 (단계별로)

1. `+0x38e` 는 `psi` 가 가리키는 첫 바이트를 **바로 읽는다**. 그 앞에 "널인가" 검사가 없다 `[정적]`.
2. 그래서 `psi` 가 0 이면 FALSE 를 돌려주는 대신 그 자리에서 **접근 위반** (없는 땅을 읽으려다 넘어짐) 이 난다 `[정적]`.
3. 곧 "`CsrClientConnectToServer` 성공 + 널 `psi`" 는 `DllMain` 이 FALSE 도 못 돌려주고 **터지는** 조합이다 (결과서).

비유: 3번 창구가 안내 묶음을 줬는데 첫 줄 화살표가 **아무 데도 안 가리키면**, user32 는 서버 정보판을 보러 가다 그대로 넘어진다. 판이 **백지** 인 건 괜찮다 — 백지면 "IME 없음 · DPI 없음" 으로 읽고 지나간다.

#### 공유 섹션 뷰에서 받는 게 아니다

- `gSharedInfo` · `gpsi` 는 **csrss 답에서만** 온다. 공유 섹션 (`SharedSection`) 뷰의 **오프셋으로 받는 게 아니다** `[정적]`.
- `psi` 가 가리키는 곳이 그 공유 섹션 뷰인지는 정적으로 못 정한다 `[추측]` (결과서는 "그 데스크톱 힙 뷰일 가능성이 높지만" 이라고 적었다).
- PE_emulator 쪽 — `SharedSection` 만 채우는 쪽이면 **모자라 보인다**. 필요한 건 "`NtAlpcSendWaitReceivePort` 답 안의 `psi` 포인터 + 그 아래 읽기 가능 `0x1b68`B" 다 (결과서 질문 2 "지금 우리").
- `gSharedInfo` 의 나머지 (`+0x8 ~ +0x23f`, `0x238`B) 는 `DllMain` 이 **쓰기만** 한다. 창 API 를 쓸 때 필요할 것 → 정적 ③ (4절).

---

### 2.6 ★ PE_emulator 가 본 `NtOpenSection` `0xc000003a` — 단계별로

`0xc0000142` = DLL 초기화 실패 (`STATUS_DLL_INIT_FAILED`). `DllMain` 이 FALSE 를 돌려줄 때 로더가 쓰는 값이다 (결과서). 그러니 아래 FALSE 11 자리 (2.7) 중 하나와 이어진다.

#### 근거 → 그래서

1. **근거** `[정적]`: `NtOpenSection(\Sessions\N\Windows\SharedSection)` 은 user32 `.asm` **어디에도 없다** (`_UserClientDllInitialize` 바깥 호출 목록에도, 지도 `_map` 에도 없음).
2. **근거** `[정적]`: 있는 곳은 ntdll `CsrpConnectToServer +0x1b6/+0x1bb` (접근 4) 하나다. 이 함수는 `CsrPortHandle == 0` 일 때만 탄다 (`CsrClientConnectToServer +0xa4 CMP` · `+0xab JZ → +0xfe` · call `+0x113`).
3. **근거** `[정적]`: `CsrPortHandle` 은 첫 전화의 `NtConnectPort` 출력 칸이다 — **커널 (= 엔진) 이 써 준다** (run68).
4. **그래서**: user32 `DllMain` 에서 `NtOpenSection(SharedSection)` 이 보였다는 건 **그 순간 ntdll `CsrPortHandle` (rva `0x1d4e20`) 이 0** 이었다는 뜻이다 (run 문서).
5. 그 뒤 사슬 `[정적]`: `0xc000003a` → `CsrpConnectToServer` 실패 → `CsrClientConnectToServer` 음수 → `_UserClientDllInitialize +0x2b8 JS` → FALSE (자리 6번) → 로더가 `0xc0000142`.

#### 아직 확인 아님 — 왜 0 이었나 `[공백]`

PE_emulator 쪽 사정이라 모른다. 후보 둘 `[추측]`:

- ① kernelbase 의 첫 연결 (run68 순서 6) 을 **안 탔다** — 흉내가 그 DLL 시작을 건너뛰었거나, 덤프에서 이어 붙인 상태에 `CsrPortHandle` 이 비었거나.
- ② 탔지만 `NtConnectPort` 몸이 성공을 돌려주면서 **출력 칸에 핸들을 안 썼다**.

①쪽에 가깝다고 보는 이유 `[추측]` (run 문서):

- kernelbase 가 **같은 몸** (`NtOpenSection` → `0xc000003a`) 으로 첫 연결 길을 탔다면, 거기서 먼저 실패해 **kernelbase 시작 함수가 FALSE** 였을 것이다 (run68 "실패 길: 6 이 음수면 DllMain FALSE").
- 그런데 실제로 처음 막힌 곳은 user32 다. 그러니 kernelbase 는 그 길을 안 탔을 가능성이 크다.

#### 바로잡는 쪽 — 둘 중 하나 (run 문서)

- **(가)** kernelbase 의 첫 연결이 성공해 `CsrPortHandle` 이 0 이 아니게 두고, user32 의 둘째 연결엔 `NtAlpcSendWaitReceivePort` 답 (2.4 끝 셋) 을 준다 — **정상 Windows 와 같은 길**.
- **(나)** 첫 연결 길 6 syscall 을 user32 때 대답한다 — 다만 이 길에서 서버 3 의 연결 정보 0x248B 가 어떻게 가고 답이 어디로 오는지는 **이번에 안 봤다** `[공백]` (정적 ⑥).

비유: user32 는 "전화선은 이미 깔렸겠지" 하고 수화기를 들었는데 선이 없었다. 그래서 그 자리에서 선을 새로 깔려고 게시판부터 찾으러 갔고 (`NtOpenSection`), 게시판이 없어서 막혔다. 진짜 문제는 게시판이 아니라 **kernelbase 가 깔았어야 할 선이 없었던 것** 이다.

---

### 2.7 FALSE 자리 11곳 — 모두 한 곳으로 모인다

`_UserClientDllInitialize` 의 FALSE 길은 모두 `+0x9b1 XOR EAX,EAX` (= `0x18006c435`) 로 모인다 `[정적]`. 무리별로 묶으면:

| 무리 | # | 점프 | 조건 |
|---|---|---|---|
| **꼭 받는 답 둘** | 4 | `+0x1df JS` | `NtQuerySystemInformation(0)` 음수 |
| | 6 | `+0x2b8 JS` | `CsrClientConnectToServer` 음수 (PE_emulator 의 장면) |
| 안쪽 준비 (ntdll · user32 코드) | 1 | `+0x193 JL` | 임계구역 10개 상태 OR 가 음수 |
| | 2 · 3 | `+0x1a7 JS` · `+0x1bb JS` | `InitializeUMHandleList` #1 · #2 음수 |
| | 5 | `+0x1fa JZ` | `InitializeNtdllUserPfn` 0 — **이미 한 번 불렸어도** (2.9) |
| 특별 갈래에서만 | 7 | `+0x2fe JNZ` | 보호 프로세스인데 PEB `+0x58` 이 이미 0 아님 |
| | 8 | `+0x3df JZ` | `InitializeCsrUserPfn` 0 (서버 프로세스만) |
| | 9 | `+0x4b5 JZ` | `g_systemCallFilterId == 5` 갈래 `RtlAllocateHeap(0x498)` NULL |
| | 10 | `+0x74a JZ` | UserModeMenus 갈래 `RtlAllocateHeap(8)` NULL |
| gdi32 | 11 | `+0x9af JNS` 안 탐 | `GdiDllInitialize` 음수 → run86 |

- 반환 규칙 `[정적]`: `+0xa8c` 에서 `EAX = (~GdiDllInitialize 결과) >> 31`. 곧 **gdi32 쪽 결과가 음수인지만** 본다.
  - 그래서 gdi32 쪽이 0 (FALSE) 을 줘도 user32 `DllMain` 은 **TRUE** 다.
- `psi` 널은 이 표에 없다 — FALSE 가 아니라 `+0x38e` 접근 위반 (2.5).

---

### 2.8 나머지 차례 — 실패해도 되는 것들 (짧게)

run 문서 "차례 표" 0 ~ 21 중 2.3 · 2.4 · 2.5 · 2.7 에서 안 다룬 것만. 모두 `[정적]` (태그가 다른 건 따로 적음).

| 차례 | 자리 | 하는 일 | syscall | 실패면 |
|---|---|---|---|---|
| 0 | `+0x9f` · `+0xae` | PEB `+0x2c0` (`SessionId`) 읽기, PEB `+0x7d0` (`Win32ClientProcInfo`) ← 0 | — | |
| 1 · 2 | `+0xc8 ~ +0x185` · `+0x1a5` · `+0x1b9` | 임계구역 ×10 (`gcsClipboard` 등), `InitializeUMHandleList` ×2 | 없음 | FALSE (2.7) |
| 5 · 6 | `+0x24f` · `+0x287` | 서비스 세션 번호 (`WTSGetServiceSessionId` → ntdll `RtlGetCurrentServiceSessionId`, PEB `+0x90` 첫 dword, 널이면 0) 와 내 `SessionId` 가 **다를 때만** `"\Sessions\%ld\Windows"` 를 만든다 (`+0x25b CMP` · `+0x25d JZ`). 같으면 `+0x254` 의 `\Windows` | 없음 | |
| 9 | `+0x3d1/+0x3d6` | (CFG 간접) `gImmApiEntries+0xa0` (rva `0xd9630`) — 늘 부름. 파일 안 값 `0x5a1a0` = 0 돌려주는 3B 함수 | 없음 | |
| 11 | `+0x3fc` · `+0x409` · `+0x410` | PEB `+0x58` ← `apfnDispatch`, PEB `+0x230` ← 0, `RegisterWaitForInputIdle` (2.10) | 없음 | |
| 12 | `+0x463/+0x46a` | `g_systemCallFilterId == 7` (파일 처음 값) 이면 `GetProcessMitigationPolicy(자기, 0xb, &v, 4)` | kernelbase 안이라 안 풂 `[추측: NtQueryInformationProcess]` | 계속 |
| 14 | `+0x576/+0x57b` | `InitInstrument` — `…\Control\Error Message Instrument\` 키 읽기 | `NtOpenKey 0x12` · `NtQueryValueKey 0x17` · `NtClose 0xf` | 키 없어도 됨 |
| 16 | `+0x5e5` · `+0x5f1` · `+0x5fd` 등 | TraceLogging 등록 ×3 (방송국 채널 셋), `WppInitUm`, `WilInitialize_Result(0)` | user32 직접은 없음. 등록마다 `NtTraceControl` (`0xf` · 트레잇 `0x1e`) 이 나갈 것 `[추측: run62 · run68 꼴, user32 사슬은 안 봄]` | 계속 |
| 17 | `+0x767` · `+0x76c` | `InitProcessGdiScaling` · `InitProcessSystemDpiBehavior` — IFEO · 정책 키 `…\Policies\Microsoft\Windows\Display` · 매니페스트 설정 | `NtOpenKey` · `NtQueryValueKey` ×2 · `NtClose` 류 | 계속 |
| 19 · 20 | `+0x9bd` · `+0xa1e` · `+0xa87` | DPI 인식 설정, 매니페스트 `disableWindowFiltering` · `forceFocusBasedMouseWheel` | 조건부 win32k (2.11) | 계속 |

- 차례 16 의 `NtTraceControl` 은 run 문서 "보탠 것" ③ 이다. 결과서 표의 "syscall 없음" 은 **user32 가 직접 부르는 것만** 센 말이다. run86 최소 조건 표에서 "엔진이 대답할 syscall" 목록에 넣을 후보다.
- 차례 5 · 6 의 갈래는 test.exe 사진 값으로 미리 정해 볼 수 있다 (2.10).

---

### 2.9 ★ `.mrdata` 의 `NtUserPfn` — 프로세스당 딱 한 번

차례 4 다. user32 가 ntdll 관리실의 **유리 덮개 칠판** 에 자기 함수 표를 적는다.

```
├─ (4)  +0x1f8  InitializeNtdllUserPfn (0x6b924)                           +0x1fa JZ → FALSE
│        ├─ (1) +0x46  ntdll!RtlInitializeNtUserPfn (0x11f7c0)   +0x85 LdrProtectMrdata(0) · +0x97 · +0xa9 · +0xbf 표 셋 → NtUserPfn · +0xc9 플래그 ← 1 · +0xd0 LdrProtectMrdata(1)
│        └─ (2) +0x69  ntdll!RtlRetrieveNtUserPfn (0x11fc30)
```

- 하는 일 `[정적]`:
  1. `+0x85` 덮개 열기 (`LdrProtectMrdata(0)`).
  2. 크기 `0xc0` · `0xc0` · `0x90` 표 셋을 ntdll `.mrdata` `NtUserPfn` (rva `0x1e7130 ~ 0x1e733f`) 에 복사.
  3. `+0xc9` 플래그 (`0x1e7340`) ← 1.
  4. `+0xd0` 덮개 닫기 (`LdrProtectMrdata(1)`).
  - 덮개 열고 닫기에서 syscall 이 나가는 건 중첩 숫자판이 0 ↔ 1 일 때 `NtProtectVirtualMemory 0x50` (run60).
- 표의 뜻은 "user32 함수 표 사본" `[뜻 추측]`.
- **프로세스당 한 번** `[정적]`: 플래그가 이미 1 이면 `0xc000000d` → `InitializeNtdllUserPfn` 0 → FALSE (자리 5번).
  - 그러니 같은 프로세스에서 user32 `DllMain` 을 이유 1 로 **두 번** 부르면, 두 번째는 여기서 FALSE 다 (표 4번 · FALSE 5번에서 바로 나오는 말).

#### test.exe 사진 — 아무도 안 썼다 `[관찰]`

- `NtUserPfn` 66칸이 P1a · P2 모두 `0x7ffafb797040` = ntdll 시작 + **`0x137040` = `UninitUser32Proc`**, 플래그 `0x1e7340` 은 0 (`_P1a.txt:5766 ~ 5799`, `_P2.txt:6896 ~ 6929`).
- ntdll **파일** 의 같은 자리도 66칸 모두 `0x180137040`, 플래그 0 이다 (flow-writer `objdump -s`).
- 곧 test.exe 에선 **파일 처음 값 (재배치만) 그대로** 다. `RtlInitializeNtUserPfn` (user32 가 부를 때만) 도, `RtlResetNtUserPfn` (플래그 ≠ 0 일 때만 `UninitUser32Proc` 로 되채움, `+0x8 ~ +0x5f`) 도 안 탔다.
- 짐 지도 7.3 의 `NtUserPfn` `≤P1a` 가 이것이다 → 7.5 에 실마리로 적었다.

비유: 관리실 유리 덮개 칠판에 "**창 담당 건물 연락처**" 칸이 처음부터 있다. 처음엔 모두 "창 담당 없음" (`UninitUser32Proc`) 으로 적혀 있다. 창 담당 건물이 전원을 켤 때 덮개를 한 번 열고 자기 연락처로 고쳐 적은 뒤 "적었음" 도장을 찍는다. 도장이 이미 있으면 두 번째는 **거절** 이다. test.exe 가게엔 창 담당 건물이 없으니 칠판은 처음 그대로다.

---

### 2.10 user32 가 PEB 에 놓는 칸 셋 — 그리고 test.exe 사진

user32 를 올리는 프로세스에서만 `[정적]`:

| PEB 칸 | 쓰는 줄 | 값 |
|---|---|---|
| `+0x7d0` `Win32ClientProcInfo` | `+0xae` | 0 (UserModeMenus 기능이 켜지면 `+0x760` 에서 `gpClientProcInfo`) |
| `+0x58` `KernelCallbackTable` | `+0x3fc` | **`apfnDispatch`** (user32 rva `0xb4720`, 0x498B 표 — 2.12) |
| `+0x230` `PostProcessInitRoutine` | `+0x409` | 0 |

- `g_systemCallFilterId == 5` 갈래면 `apfnDispatch` 0x498B 를 힙에 베껴 PEB `+0x58` 을 그 사본으로 바꾼다 (`+0x48e ~ +0x568`) `[정적]`.

test.exe 사진과 맞춰 본 것 (flow-writer, run44 덤프 읽기 로그 — 새 실행 아님):

- 위 PEB 셋, 그리고 TEB `Win32ThreadInfo` · `Win32ClientInfo` 는 P1a ~ P2 **모두 비었다** `[관찰]` (`_P2.txt:2464 · 2505 · 2554 · 2694 · 2726`, `_P1a.txt:1754 · 1795 · 1844 · 1984 · 2016`). user32 를 안 올린 것과 맞다.
- PEB `SessionId` = 1, `SharedData` = 널 `[관찰]` (`_P1a.txt:1798 · 1764`, `_P2.txt:2508 · 2474`).
  - 그래서 user32 를 같은 프로세스에 올렸다면, 서비스 세션 번호는 0 (널이면 0), 내 세션은 1 → **다르니까** `\Sessions\1\Windows` 갈래다 `[정적 갈래 + 관찰 값]`. run68 kernelbase 연결과 같은 이름이다.
- 짐 지도 7.5 `PostProcessInitRoutine`: 넣는 쪽이 "모름" 이었는데, 하나 찾았다 — user32 `+0x409` 가 **0 을 넣는다** `[정적]` (user32 를 올리는 프로세스만). test.exe 사진의 0 은 "아무도 안 넣음" 쪽이다 `[추측]`.

---

### 2.11 ★ win32k syscall — `DllMain` 엔 "무조건" 이 하나도 없다

#### 다섯 개, 모두 조건부 `[정적]`

| win32u 이름 | 번호 | 부르는 자리 | 조건 |
|---|---|---|---|
| `NtUserSetProcessUIAccessZorder` | `0x157b` | `_UserClientDllInitialize +0x5d2/+0x5d9` | 승격 아님 + 매니페스트 `uiAccess` 켜짐 |
| `NtUserSetProcessDpiAwarenessContext` | `0x1576` | `SetProcessDpiAwarenessContext +0x3a/+0x41` (← `InitProcessDpiAwareness`) | IFEO `DpiAwareness` 있음, 매니페스트 dpi 요소 있음, `psi+0x8bc` 비트 `0x20` 켜짐 + 시스템 DPI < 121, 또는 TEB `+0x8e0` 비트 `0x80` |
| `NtUserGetDpiForCurrentProcess` | `0x1440` | `GetDpiForCurrentProcess +0x28/+0x2f` | `psi+0x8bc` 비트 `0x20` 켜짐 (캐시가 비었을 때) |
| `NtUserDisableProcessWindowFiltering` | `0x13ec` | `+0xa17/+0xa1e` | 매니페스트 `disableWindowFiltering` = true |
| `NtUserSetProcessMousewheelRoutingMode` | `0x1579` | `+0xa80/+0xa87` | 매니페스트 `forceFocusBasedMouseWheel` = true |

- 그래서 **`psi` 0 채움 · 매니페스트 없음 · IFEO 없음 · TEB `Win32ClientInfo` 0** 이면 다섯 다 **안 부른다** `[정적]`.
  - 이건 디컴파일 읽기다. 실제로 어느 갈래로 가는지는 동적 (4절 동적 4).
  - TEB `+0x800` 비트 29 가 켜져 있으면 `-3` 으로 `SetProcessDpiAwarenessContext` 를 부르는 쪽으로 떨어진다 (결과서).
- 다섯 다 FALSE 자리와 이어지지 않는다 (run 문서 차례 표 "FALSE 아님"). `0x157b` · `0x1576` 은 결과서가 "반환 안 봄" 이라고 적었다.

#### `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` — 서버 프로세스만

- 요청서는 user32 `DllMain` 이 이 둘을 부를 거라고 짐작했다. **아니다** `[정적]`.
- 두 syscall 의 IAT 칸 (`0xb63f8` · `0xb63f0`) 을 읽는 코드는 user32 전체에서 **`ClientThreadSetup` 한 곳** 뿐이다 (`_refs/0xb63f8.tsv` · `0xb63f0.tsv`).
  - `+0x120/+0x127 NtUserProcessConnect(-1, 0x240, buf)` (`0x10e2`), `+0x228/+0x22f NtUserInitializeClientPfnArrays` (`0x14aa`).
  - 그 블록은 **`gfServerProcess ≠ 0`** (`+0xdd` · `+0x1b9`) + `gfFirstThread ≠ 0` 일 때만 탄다.
- **새 말 — `gfServerProcess`**: user32 수첩의 "**나는 서버 (csrss 쪽) 인가**" 표시. ntdll `CsrClientConnectToServer` 가 줄 1 의 다섯째 인자로 받아서 쓴다 (`+0xc7` · `+0x1cb`) `[정적]`.
  - PE 헤더 서브시스템이 1 (NATIVE) 이면 1, 아니면 0. csrss 같은 native 이미지만 1 이고, test.exe 같은 콘솔 · GUI 이미지는 0 이다 (결과서).
- 그래서 → **보통 프로세스의 `gSharedInfo` 는 csrss 답 (2.4) 에서만 온다.**
  - 재밌는 점: `NtUserProcessConnect(-1, 0x240, buf)` 의 크기 `0x240` 이 `gSharedInfo` 크기와 같다 (결과서). 서버는 그래픽 부서에서 직접, 보통 가게는 인사팀 창구를 거쳐 **같은 모양의 묶음** 을 받는 셈이다.

비유: "창 담당 건물은 전원을 켜면서 그래픽 부서에 직접 등록한다" 는 짐작이었다. 실제로 그건 **인사팀 사무실 자신** 같은 특별한 프로세스만 하는 일이다. 보통 가게는 인사팀 3번 창구에서 안내 묶음을 받아 온다.

---

### 2.12 ★ GUI 스레드로 바뀌는 때 — 첫 "옆 건물 번호" 를 누르는 순간

**새 말 — GUI 스레드**: win32k 번호부 (그래픽 쪽) 를 쓸 수 있게 커널이 **바꿔 준** 스레드다. run56 해설 2.9 에서 본 것처럼, 커널은 GUI 스레드면 다른 번호부 (`…Shadow`) 를 편다 `[정적, run56]`.

#### 언제 바뀌나 `[정적, 커널은 이름만]`

1. 스레드가 **처음으로 win32k syscall** (번호 `0x1000` 이상) 을 부른다.
2. 커널이 그 자리에서 `KiConvertToGuiThread` (`0x6affb0`) → `+0x4/+0x9 PsConvertToGuiThread` 로 바꾼다 (이름만 — sast/45 가 `+0x1e0 · +0x238` 에서 win32k 콜아웃을 부른다고 읽음).
   - run56 해설 2.9 의 "win32k 번호면 `+0x91f KiConvertToGuiThread` 뒤 `+0x940` 에서 다시" 가 이 입구다.
3. 그러니 user32 `DllMain` 안에서 win32k syscall 을 부르면, **그때** 바뀐다 (그 스레드의 첫 win32k syscall 이므로 — 결과서).

- TEB `+0x78` (`Win32ThreadInfo`) 는 **user32 코드가 안 쓴다** `[정적]` (`_UserClientDllInitialize` · `ClientThreadSetup` 에 `+0x78` 쓰기 줄 없음).
  - 쓰는 쪽은 win32k 일 것이다 `[추측: win32kbase.sys 의 스레드 콜아웃; sast/48 도 같은 쪽으로 짐작]`.

#### 바뀐 뒤 — 커널이 user32 를 거꾸로 부른다

**새 말 — 커널 콜백 · `KernelCallbackTable`**

- **커널 콜백** = 커널이 일하는 도중에 **유저 쪽 함수를 잠깐 불러 쓰고**, 그 함수가 끝나면 `NtCallbackReturn` 으로 커널에 돌아가는 것 `[추측]` (일반적으로 알려진 설명).
- 이번에 본 증거 `[정적]`: `__ClientThreadSetup` (rva `0x4f7d0`) 의 끝은 `NtCallbackReturn` (ntdll syscall `0x5`) 으로 가는 **꼬리 점프** 다 (`+0x20`, flow-writer). 결과서는 이걸 보고 "커널이 `KeUserModeCallback` 으로 부른 콜백이다" 라고 읽었다.
- **`KernelCallbackTable`** (PEB `+0x58`) = 커널이 "몇 번 함수" 로 부를 때 보는 **내선 번호표** 다. user32 가 2.10 에서 `apfnDispatch` (0x498B) 를 여기 꽂는다.
  - `__ClientThreadSetup` 은 그 표의 **84번째 칸** (`+0x2a0`, rva `0xb49c0`) 이다 `[정적: refs]`.

```
user32!__ClientThreadSetup (0x4f7d0)                   ← apfnDispatch +0x2a0 (84번째 칸)
 ├─ (1) +0x9   ClientThreadSetup (0x4f810)
 └─ (2) +0x20  JMP ▶ NtCallbackReturn(0, 0, TRUE → 0 / FALSE → 0xc0000001)
```

- 돌려주는 상태 `[정적]` (flow-writer): `ClientThreadSetup` 이 TRUE 면 0, FALSE 면 `0xc0000001` (`+0x9 NEG` · `+0xb SBB` · `+0x10 NOT` · `+0x15 AND`). 첫 두 인자는 0.

`ClientThreadSetup` 이 보통 프로세스 · 첫 GUI 스레드에서 하는 일 (결과서, 대표만) `[정적]`:

- `+0x82` `gfFirstThread ≠ 0` 이면 `+0x8c` 에서 0 으로 바꾸고 `+0x93/+0x9a` **`GdiProcessSetup`** (gdi32 → run86).
- `+0xca` TEB `+0x800` 비트 8 이 이미 켜져 있으면 FALSE (= 같은 스레드에서 두 번째 콜백).
- 보통 갈래 (`+0xe5 JZ → +0x258`): `NtUserRemoteConnectState` (`0x151f`) · `InitClientDrawing` · `NtUserGetThreadDesktop` (`0x107b`) · TEB `+0x820` ← `pdiLocal` · `NtUserGetProcessUIContextInformation` (`0x1475`) · `LoadAppInitDlls`.

#### 그럼 user32 `DllMain` 안의 첫 win32k syscall 은?

- `DllMain` 본체의 다섯은 모두 조건부다 (2.11). 최소 조건이면 하나도 안 나간다.
- 그런데 `+0x996 GdiDllInitialize` 는 **무조건** 부른다 `[정적]`. 그 안에서 win32k syscall 이 나가는지는 **이번에 안 봤다** → **run86** (Q150b).
- 그래서 "user32 `DllMain` 안에서 무조건 부르는 첫 후보는 `GdiDllInitialize` 쪽" 까지가 이번 답이다.

#### PE_emulator 에게

- win32k syscall 이 하나라도 나가면, 그 스레드에서 `__ClientThreadSetup` 콜백이 user → kernel → user 로 돌아와야 한다.
- 엔진이 "첫 win32k syscall → `KernelCallbackTable[84]` 콜백 부르기" 를 못 하면 `ClientThreadSetup` 이 안 돌아 `pdiLocal` · TEB `+0x820` 등이 안 채워진다 `[추측: 엔진 쪽]`.
- PE_emulator 에 GUI 스레드 흉내가 있는지는 모른다 (결과서 질문 3 "지금 우리": 정보 없음).

비유: 직원이 처음으로 **옆 건물 (그래픽 부서) 단축번호** 를 누른다. 관리사무소는 그 자리에서 이 직원을 "그래픽 손님" 으로 등록하고, 가게 안내판에 붙은 **내선 번호표 84번** 으로 창 담당 건물 사람을 불러 "이 직원 준비시켜 주세요" 한다. 그 사람은 준비를 마치고 "다 했어요" (`NtCallbackReturn`) 하고 끊는다. 번호표가 안 붙어 있거나, 관리사무소가 거꾸로 부르는 법을 모르면 준비가 빠진 채로 간다.

---

### 2.13 win32u 이름표 — 1499개, 번호는 스텁에 그대로

- win32u 스텁은 모두 같은 24B 꼴이다 `[정적]`: `MOV R10,RCX; MOV EAX,<번호>; TEST byte ptr [0x7ffe0308],1; JNZ → INT 0x2e; SYSCALL; RET`.
  - 바이트로는 `4C 8B D1 B8 <imm32>` 로 시작한다. ntdll `Nt*` 이름표를 만들 때와 **같은 방법** 으로 읽으면 된다 (run56).
- 전체 표: `ms/disassemble/win32u/_syscalls.tsv` — **1499개**, 번호 `0x1000 ~ 0x15da`, 번호 중복 없음, Ghidra `.asm` 과 12/12 같음 `[정적]`.
  - 열: 번호 16진 · 10진 · 이름 · rva · 크기 · 같은 주소 export 별칭.
- PE_emulator 의 짐작 "win32u 번호는 `0x1000` 대" 는 **맞다**. 이제 이름을 붙일 수 있다.

이번 길에 나온 것 (대표, 전체는 run 문서 번호표):

| 번호 | 이름 | 이번 길에서 |
|---|---|---|
| `0x1000` | `NtUserGetThreadState` | (참고) 가장 낮은 번호 |
| `0x107b` | `NtUserGetThreadDesktop` | `ClientThreadSetup +0x29c` |
| `0x10e2` | `NtUserProcessConnect` | `ClientThreadSetup +0x120`, **서버 갈래만** |
| `0x13ec` | `NtUserDisableProcessWindowFiltering` | `DllMain +0xa17` (조건부) |
| `0x1440` | `NtUserGetDpiForCurrentProcess` | `GetDpiForCurrentProcess +0x28` (조건부) |
| `0x14aa` | `NtUserInitializeClientPfnArrays` | `ClientThreadSetup +0x228`, 서버 갈래만 |
| `0x151f` | `NtUserRemoteConnectState` | `ClientThreadSetup +0x272` |
| `0x1576` | `NtUserSetProcessDpiAwarenessContext` | `SetProcessDpiAwarenessContext +0x3a` (조건부) |
| `0x1579` | `NtUserSetProcessMousewheelRoutingMode` | `DllMain +0xa80` (조건부) |
| `0x157b` | `NtUserSetProcessUIAccessZorder` | `DllMain +0x5d2` (조건부) |

- 같은 길의 ntdll 쪽 번호: `NtQuerySystemInformation 0x36` · `NtOpenKey 0x12` · `NtQueryValueKey 0x17` · `NtClose 0xf` · `NtProtectVirtualMemory 0x50` · `NtCallbackReturn 0x5` · **`NtAlpcSendWaitReceivePort 0x8e`**. 첫 연결 길: `NtOpenSection 0x37` · `NtCreateSection 0x4a` · `NtMapViewOfSection 0x28` · `NtConnectPort 0xa4`.
- gdi32 쪽 번호 (`NtGdi*`) 는 → **run86 에서 다룸**.

---

### 2.14 덤 — 공용 창고에 넷, 그리고 gdi32 가 먼저

- **공용 창고 (`\KnownDlls`)** 에 `user32.dll` · `win32u.dll` · `gdi32.dll` · `gdi32full.dll` 섹션이 있다 `[관찰]` (run44, `_P2.txt:1387 · 1396 · 1408 · 1399`).
  - 그러니 user32 를 import 하는 프로세스는 flow.md 2.6 의 **KnownDll 길** (공용 창고에서 꺼내 오기) 로 올릴 것이다 `[추측: 실행은 안 봄]`.
  - PE_emulator 가 KnownDll 섹션을 흉내 낸다면 이 넷이 필요하다 (run 문서).
- **user32 는 GDI32 를 정적으로 가져온다** `[정적: PE 표]` (flow-writer `pefile`): IAT `0xb4fe0 GdiDllInitialize` · `0xb4fd8 GdiProcessSetup`, 둘 다 `GDI32.dll`.
  - 그러니 gdi32 는 user32 `DllMain` 전에 이미 올라와 있을 것이다 `[추측: 2.7 초기화 순서 — 의존하는 쪽이 먼저]`.
  - gdi32 의 몸과 gdi32full 쪽은 → **run86**.

### 2.15 결과서와 다르게 적은 곳 · 보탠 것 · 정정 (짧게)

- 보탠 것 ① ~ ⑥ (flow-writer `.asm` · `pefile` · 사진) — 본문에 다 녹였다:
  - ① `NtCallbackReturn` 은 call 이 아니라 **꼬리 `JMP`** (`+0x20`), 상태 0 / `0xc0000001` → 2.12
  - ② `CsrClientCallServer` 의 포인터 옮기기 (delta) → 2.4
  - ③ 차례 16 "syscall 없음" 은 user32 직접만 — TraceLogging 이 `NtTraceControl` 을 낼 것 `[추측]` → 2.8
  - ④ test.exe 사진 대조 → 2.9 · 2.10
  - ⑤ `\KnownDlls` 넷 · ⑥ GDI32 정적 가져오기 → 2.14
- 이름 확인: `_calls.tsv` 의 `alias:` 로 나온 user32 함수들 (`InitializeUMHandleList` 등) 은 `pdb_aliases.py --lookup` 으로 **별칭 무리가 아님** 을 확인하고 이름을 그대로 썼다. ntdll `alias:RtlCopyMemory+3` (같은 주소 이름 3개) 은 "메모리 복사" 로 적었다.
- 정정:
  - **이전 run 이 틀린 것은 없다.** run68 의 "정상이면 `CsrpClientConnectToServer` 는 안 탄다" 는 첫 연결 때 이야기로 맞고, 이번에 "둘째 연결의 보통 길이기도 하다" 를 더했다 (2.4).
  - 결과서 sast/82 의 gdi32 쪽 짐작 둘 (`GdiDllInitialize` 를 부르는 횟수 · `NtGdiInit` 이 첫 win32k syscall 후보인가) 은 → **run86 에서 다룸**. run85 문서에 옮기지 않았다.
- 디버거가 붙어서 달라지는 점: 이번에 읽은 user32 줄기에선 디버거 여부를 보는 줄이 없었다 `[정적: 읽은 줄 안에서]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 — 새 PDB 두 줄 | — | **user32.pdb** `CDB947D1B96B87977915DE04CF54279C1` · **win32u.pdb** `6295787D0B7E537E98F77F31F4426D1C1` (둘 다 처음 analyze, 타입 없음. win32u 줄에 번호표 `_syscalls.tsv`) |
| 기준 — ntdll · kernelbase · ntkrnlmp PDB 줄 | … run84 까지 | + run85 (ntdll `RtlInitializeNtUserPfn` 등 6개 depth 0 · `Csr*` 넷 `.asm` 다시 읽음 / kernelbase `.dll` `objdump` 8줄 / 있던 `KiConvertToGuiThread` `.asm` 읽기만) |
| 기준 — 근거 메모 | — | run85 줄 (정적 바퀴, test.exe 는 user32 안 올림, 다시 읽은 사진 칸들) |
| 2.5 머리 run 목록 | … run83 | + run85 |
| 2.5 run68 csrss 연결 글머리 | "정상이면 `CsrpClientConnectToServer` 는 안 탄다" | + 한 줄: **같은 줄이 둘째 연결 (user32, 서버 3) 의 보통 길** `[정적]`, `NtOpenSection` 이 user32 때 보이면 `CsrPortHandle` 0 |
| 3장 콘솔 절 "동적으로 남은 것" | "새 샘플 필요" | + "(→ 대기열 **Q160**, run85 때 붙임)" |
| **3장 새 절** | — | "**user32 시작 함수 (`DllMain`)**" — 비유 · 트리 · `[정적]` 여섯 · `[관찰]` 하나 · 동적 남은 것 |
| 4장 run84 줄 | 정적 "대기열 번호 없음", 새 샘플 번호 없음 | **Q159** · **Q160** |
| 4장 run85 새 줄 | — | 실행 확인 (**Q162** — test.exe 로 못 봐서 Q104 와 따로) · 정적 남은 것 ① ~ ⑦ (**Q161**) · PE_emulator 쪽 · 보탠 것 · 짐 지도 결정 · run84 뒷정리 |
| 5장 | … run84 | + run85 (표 둘), run84 줄에 Q159 · Q160 |

- **흐름도 그림은 안 바뀌었다.** test.exe 가 user32 를 안 올리기 때문이다.

태그 · 판단이 바뀐 곳:

- "user32 `DllMain` → `NtOpenSection(SharedSection)`" `[관찰 — PE_emulator 흉내]` → **정상 길이 아니다** `[정적]`. 그 장면은 "`CsrPortHandle` 이 0 이었다" 는 뜻.
- run68 "`CsrpClientConnectToServer` 는 정상이면 안 탄다" → 첫 연결 때만 그렇고, **둘째 연결에선 보통 길** `[정적]`.
- `NtUserProcessConnect` 가 user32 초기화에서 불린다 (요청서 짐작) → **서버 프로세스 갈래뿐** `[정적]`.
- win32u 번호 `0x1000` 대 `[추측]` (PE_emulator) → **맞음, 이름표 1499개** `[정적]`.
- 짐 지도 7.5 `PostProcessInitRoutine` "넣는 쪽 모름" → 넣는 쪽 하나 (user32 `+0x409`, 0) `[정적]`, test.exe 의 0 은 "아무도 안 넣음" 쪽 `[추측]`.
- 짐 지도 7.3 `NtUserPfn` `≤P1a` → **파일 처음 값 그대로, test.exe 에선 아무도 안 씀** `[관찰]` + `objdump`.

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** (마흔한째판). test.exe 는 user32 를 안 올려서, user32 칸은 모두 "user32 를 올릴 때만" 이다.
- 근거만 붙인 곳: 3.3 TEB "범위 밖 길 — 다른 DLL 39" 줄 (user32 가 `Win32ClientInfo` 를 쓰는 줄 · `Win32ThreadInfo` 는 user32 가 아님) · 7.2 `_PEB` `끝까지 0` 에 user32 가 쓰는 PEB 칸 메모 · 7.3 `NtUserPfn` 가리킴 · 7.5 `PostProcessInitRoutine` 덧붙임 + `NtUserPfn` 실마리 · 8장 두 표.
- **결정이 남았다** (오케스트레이터 · 사용자): 7.5 의 `NtUserPfn` 을 3.9 로 옮겨 2장 `.mrdata` 모름을 26 → 25 로 셀지, PEB 의 user32 칸 셋을 TEB 처럼 "범위 밖 길 — 다른 DLL" 로 셀지.

### 큰 그림에서

- **지도 쪽**: 진입점까지의 test.exe 길은 그대로다. 대신 2.7 "dll 전원 켜기" 에 user32 가 끼면 무엇이 더해지는지 옆길 지도가 생겼다.
- **은퇴 앞의 두 전원 켜기가 이어졌다**:
  - kernelbase 전원 켜기의 **첫 전화** (run68) 가 전화선 손잡이 `CsrPortHandle` 을 남긴다.
  - user32 전원 켜기의 **둘째 전화** (이번) 는 그 손잡이가 있어야 한 통화로 끝난다.
  - 그러니 PE_emulator 의 user32 문제는 사실 **kernelbase 첫 전화** 쪽부터 봐야 한다 (2.6).
- **PE_emulator 쪽**: user32 `DllMain` 의 최소 답이 정해졌다 — `NtQuerySystemInformation(0)` 성공 + ALPC 한 번의 답 셋 + 읽기 가능한 `psi` 0x1b68B. 남은 건 gdi32 쪽이다.
- **다음**: **Q150b** (run86) — gdi32 · gdi32full `DllMain` 과 "창 안 만드는 프로세스" 최소 조건 표 둘 (syscall 답 / 데이터 칸). 재료는 이번 차례 표 · 데이터 칸 표 · FALSE 11 자리다.

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 남은 것은 다섯 무리다.

- **실행해 봐야 아는 값** → **Q162** (결과서 "동적분석으로 확인해야 할 것" 번호 그대로). **test.exe 로는 못 본다** (user32 안 올림 `[소스]`) — 그래서 Q104 와 따로 둔 새 행이다.
  - 볼 프로세스는 새 샘플 (user32 를 import 만 하는 콘솔 exe) 또는 이미 있는 GUI 프로그램 (예: notepad) 둘 다 된다 `[추측]`. **고르는 건 사용자 결정** (Q160 의 `/MD` 샘플과 묶을 수도).
- **엔진 쪽** (PE_emulator 실행 기록) — 동적 1 의 앞 절반.
- **작은 정적** ① ~ ⑦ → **Q161**.
- **gdi32 쪽** → **run86** (Q150b).
- **짐 지도 결정** → 오케스트레이터 · 사용자.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| PE_emulator 에서 user32 `DllMain` 때 ntdll `CsrPortHandle` (rva `0x1d4e20`) 이 정말 0 이었나, kernelbase 첫 연결 (`_KernelBaseBaseDllInitialize +0x2d2`) 을 탔나, `NtConnectPort` 가 출력 칸을 채웠나 | PE_emulator 쪽 사정 `[공백]` (2.6) | **동적 1 [엔진 쪽]** |
| 진짜 Windows 에서 같은 칸이 0 이 아닌가 | 실행 때 값 | **Q162** 동적 1 [VM] — `user32!_UserClientDllInitialize+0x2aa` 에서 `dq ntdll+0x1d4e20 L1` |
| `NtAlpcSendWaitReceivePort` 가 받은 실제 답 — 메시지 `+0x34`, 캡처 버퍼 안 `psi` 값과 그 아래 `+0x0` · `+0x8bc` · `+0x1b66` | csrss 가 정하는 값 | **Q162** 동적 2 — `ntdll!CsrpClientConnectToServer+0x9a` (복사 직후) |
| `user32!gfServerProcess` 가 0 인지, `gSharedInfo` 0x240B 실제 내용 | 실행 때 값 | **Q162** 동적 3 |
| `psi+0x8bc` 비트 `0x20` 이 켜져 있나 → DPI 쪽 win32k syscall (`0x1576` · `0x1440`) 이 실제로 나가나 | 정적은 디컴파일 읽기까지 | **Q162** 동적 4 |
| 그 프로세스의 첫 win32k syscall 이 `DllMain` 안인가 (gdi32 쪽 또는 DPI 쪽), `__ClientThreadSetup` 이 84번 칸으로 불리나, TEB `+0x78` 이 언제 채워지나 | 커널 쪽은 이름만, gdi32 안은 안 봄 | **Q162** 동적 5 — `user32!ClientThreadSetup` 입구 · `nt!PsConvertToGuiThread` (그 프로세스 조건) |
| gdi32full 이 user32 `DllMain` 때 이미 올라와 있나 | gdi32 쪽 안 봄 | **Q162** 동적 6 + **run86** |
| `g_systemCallFilterId` 가 5 로 바뀌나 (`+0x46a` 직후 `v & 0xf`) | 실행 때 값 | **Q162** 동적 7 |
| UserModeMenus 기능이 켜졌나 (켜지면 PEB `+0x7d0` · `NtUserInitializeUserModeMenus`) | 실행 때 값 | **Q162** 동적 8 |
| user32 TraceLogging 등록 셋이 `NtTraceControl` 을 몇 번 부르나 | user32 사슬 안 봄 `[추측]` (2.8) | **Q162** (flow-writer 더함) + **Q161** ⑦ |
| ① `SHAREDINFO` · `SERVERINFO` 칸 이름, `psi +0x0` · `+0x8bc` · `+0x1b66` 의 뜻 | PDB 에 타입 없음. win32kbase · win32kfull 안쪽을 봐야 함 ("win32k 안쪽 깊이" 라 **사용자 결정**) | **Q161** ① |
| ② csrss 가 USERSRV 연결 답을 만드는 쪽 (`CsrSrvClientConnect` · winsrv) | 이번 범위 밖 (csrsrv 는 run71 에서 봄) | **Q161** ② |
| ③ 창 안 만드는 프로세스가 흔히 부르는 user32 API (`GetSystemMetrics` · `MessageBox` 등) 가 읽는 `psi` 칸 | `DllMain` 만 봄 (`_refs/0xda590.tsv` 368줄) | **Q161** ③ |
| ④ `ClientThreadSetup` 의 `InitClientDrawing` · `LoadAppInitDlls` · `NtUserRegisterLPK` 깊이 | 이름만 | **Q161** ④ |
| ⑤ `InitializeLpkHooks` (rva `0x501e0`) 가 LPK 표시를 언제 정하나 (`NtUserRegisterLPK` 조건) | 안 읽음 | **Q161** ⑤ |
| ⑥ 첫 연결 길에서 서버 3 의 0x248B 가 어떻게 가나 (연결 블록에 실리나, `+0x11c` 음수로 `+0xad` → `CsrpClientConnectToServer` 를 또 타나) | 이번엔 둘째 연결만 봄. PE_emulator 가 (나) 를 고를 때만 의미 (2.6) | **Q161** ⑥ |
| ⑦ user32 TraceLogging 사슬 · `GetProcessMitigationPolicy(0xb)` 의 syscall (`[추측: NtQueryInformationProcess]`) | kernelbase 프로젝트를 안 엶 | **Q161** ⑦ — run86 최소 조건 표를 닫으려면 |
| gdi32 · gdi32full `DllMain`, `GdiDllInitialize` · `GdiProcessSetup` 안, 최소 조건 표 둘 | 이번 범위 밖 (요청 28 의 4 · 5) | **run86** (Q150b, sast/83) |
| 짐 지도: `NtUserPfn` 을 3.9 로 옮길지 (`.mrdata` 모름 26 → 25), PEB user32 칸 셋을 "범위 밖 길 — 다른 DLL" 로 셀지 | 세는 기준을 정하는 일 | 오케스트레이터 · 사용자 결정 |

덤 — 이번에 안 풀린 옛 질문 하나: [run51 해설](run51-eprocess-later-해설.md) 2.9 의 `Win32Process` "누가 첫 win32k 호출을 일으키나" (F9, **Q98**) 는 이번에도 그대로다. 이번 결과 (GUI 변환 = 그 스레드의 첫 win32k syscall, test.exe 엔 user32 가 없음) 는 그 질문의 배경을 조금 더 알려 줄 뿐이고, test.exe 에서 누가 그 호출을 했는지는 답하지 않는다 `[추측: 두 문서를 이어 읽은 것]`.

---

## 5. 더 읽기

- run 문서: [`run85-user32-init.md`](run85-user32-init.md) — 맨 위 한 줄 결론, "PE_emulator 쪽에서 쓸 것" (요청 28 user32 쪽 결론 · 원문 질문이 어디서 답해졌나 · 질문마다 표), **차례 표 (0 ~ 21)**, **FALSE 11 자리**, **데이터 칸 표**, **win32u 번호표**, "PE_emulator 가 본 `0xc000003a`" 절, 호출 지점 표, 호출 관계 그림, 결과서와 다르게 적은 곳 · 보탠 것 ① ~ ⑥, 정정, 아직 확인 필요 (동적 1 ~ 8 · 정적 ① ~ ⑦ · 짐 지도 결정)
- 결과서: [`_workspace/sast/82_user32-init_result.md`](../../_workspace/sast/82_user32-init_result.md) — 먼저 읽을 다섯 줄, 질문 1 (차례 표 · (a) ~ (d)), 질문 2 (데이터 칸), 질문 3 (GUI 스레드), 질문 4 (번호표), 동적으로 확인할 것, 다음 요청 후보 (gdi32 쪽 짐작은 run86 에서 다룸)
- 요청서: [`_workspace/sast/82_user32-init.md`](../../_workspace/sast/82_user32-init.md)
- flow.md: [3장 새 절 "user32 시작 함수 (`DllMain`)"](flow.md) · 2.5 run68 csrss 연결 글머리 · 기준 (user32 · win32u PDB) · 4장 run85 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.3 TEB "범위 밖 길 — 다른 DLL 39" · 7.2 `_PEB` · 7.3 · 7.5
- win32u 전체 번호표: `ms/disassemble/win32u/_syscalls.tsv` (커밋 안 함)
- 앞 이야기:
  - [run68 해설](run68-dll-init-rest-해설.md) 2.4 — **첫 전화** (kernelbase · 1번 창구 · syscall 6번 · 옮겨 적기). 이번 2.4 · 2.6 의 앞. 2.4 끝 "덤" 은 첫 연결 때 이야기로 읽기.
  - [run68 해설](run68-dll-init-rest-해설.md) 2.3 — 동네 기본 규격표 (`NtQuerySystemInformation(0)`)
  - [run71 해설](run71-named-obj-dir-해설.md) — 사무실 게시판 원본 (`SharedSection`)
  - [run56 해설](run56-syscall-args-해설.md) 2.4 "덤" · 2.9 — win32k 번호부 · `KiConvertToGuiThread`
  - [run51 해설](run51-eprocess-later-해설.md) 2.9 — win32k (창 그리기 담당 부서) · `Win32Process` (Q98)
  - [run60 해설](run60-func-table-mrdata-해설.md) — `.mrdata` 덮개 열고 닫기
  - [run21 해설](../02_sprint/run21-load-dll-internal-해설.md) 2.4 — KnownDll 길 (공용 창고)
  - [run44 해설](run44-baggage-snap-해설.md) 2.8 — 사진 P1a · P2

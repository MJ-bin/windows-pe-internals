# run132 npfs-pipes — 해설

> 이 글은 [run132 문서](run132-npfs-pipes.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q241** — PE_emulator 요청 "파이프 (이름 있는·없는) — kernelbase 가 커널에 무엇을 넘기고, npfs 가 만들기·열기·읽기·쓰기에 무엇을 돌려주나" (`298_internals-npfs-pipes.md`, 튜닝 `298_tune10.md` T7′, 병목 25 · 파이프 만들기 샘플 77) (`_workspace/plan.md`, 이번에 **끝 (run132)**)
  - 사용자 결정 2026-11-22 "이 세 요청 먼저 가장 우선".
  - 결과서 판정 **분석성공**. 요청서 ★ 전부 (1(a)(b)(c) · 2(a)(b)(c) · 3(a)(b)(c)) 와 1(d) · 3(d) 를 `.asm` 줄로 답했다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q244** (전부 선택. 파이프를 쓰는 작은 exe 가 필요 — 고르기는 사용자 결정)
    - 정적 → **Q245**
- 근거:
  - run 문서 [`run132-npfs-pipes.md`](run132-npfs-pipes.md) (가장 중요)
  - 요청서 [`_workspace/sast/129_npfs-pipes.md`](../../_workspace/sast/129_npfs-pipes.md) · 결과서 [`129_npfs-pipes_result.md`](../../_workspace/sast/129_npfs-pipes_result.md)
    - 처음 analyze 는 **npfs 하나** (10초, 함수 130). kernelbase · ntoskrnl · kernel32 · ntdll 은 이미 있던 것에서 `^이름$` export 만.
    - 스크립트 문제 없음. npfs `.c` 의 음수 십진 상수 · 깨진 반환값은 `.asm` 으로 다시 셌다.
  - flow.md 변경: docs 커밋 `1b5a8ac` (`git show 1b5a8ac -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 파이프를 안 쓴다.**
   - `sample/*.c` · `log.h` 에 파이프 API 가 없다 `[소스]` (`grep -i pipe` 0줄).
   - test.exe 의 표준 핸들은 콘솔 (`DeviceType 0x50`) 이다 (run83 · run84).
   - 그래서 이번 이야기는 "**`main` 뒤에 파이프를 쓰는 다른 프로그램이라면**" 이다. 그림 · 짐 지도는 그대로다.
2. **"요청 쪽 로그" 는 진짜 Windows 로그가 아니다.**
   - "`CreateNamedPipeA` 가 0", "② ③ root 0", "40 샘플의 access · share · disposition 분포" 는 모두 PE_emulator 가 흉내 낸 실행의 기록이다. `[관찰]` 이 아니다.
   - 다만 그 40 샘플의 분포가 1(a) 규칙 (2.4) 으로 **하나도 안 틀리고** 설명된다. 에뮬레이터가 진짜 kernelbase 코드를 돌린 것이니 "오프셋 셈 · 바이너리를 믿어도 된다" 는 근거다. 진짜 Windows `[관찰]` 은 여전히 아니다.
3. **오프셋 세 가지.** run 문서와 같은 규칙이다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 표 · 본문의 "`+0x22e NtCreateNamedPipeFile`" 처럼 함수 이름 앞 숫자 = **call 자리**.
   - 그냥 "`+0x1d4`" = 함수 안 **줄**.
   - 콜스택으로 본 적은 없다.
4. **npfs PDB 엔 구조체 타입이 0개다.**
   - 함수 이름은 다 있다 (공개 PDB). 그런데 칸 모양은 없어서, CCB · FCB 칸은 "npfs 코드가 읽고 쓰는 칸" 으로 적었다.
   - 상태 번호 1 ~ 4 의 **값과 쓰는 줄** 은 `[정적]`, **이름** (DISCONNECTED · LISTENING …) 은 공개 헤더 값과 같다고 본 `[추측: 이름]` 이다.
5. **status 가 아주 많이 나온다.** 이 글에 나오는 것만 먼저 모아 둔다 (이름 · DOS 오류 번호는 결과서 · run 문서 그대로).

   | status | 이름 (결과서) | `GetLastError` | 한 줄 뜻 | 이 글 |
   |---|---|---|---|---|
   | `0xC000014B` | `PIPE_BROKEN` | 109 | 상대가 닫았고 남은 자료도 없음 | 2.11 · 2.13 |
   | `0xC00000B2` | `PIPE_CONNECTED` | 535 | 이미 붙어 있음 (듣기에서) | 2.10 |
   | `0xC00000B1` | `PIPE_CLOSING` | 232 | 상대가 닫는 중 (쓰기 · 듣기에서) | 2.10 · 2.12 |
   | `0xC00000B0` | `PIPE_DISCONNECTED` | 233 | 서버가 끊음 | 2.13 |
   | `0xC00000B3` | `PIPE_LISTENING` | 536 | 아직 아무도 안 붙음 | 2.10 · 2.11 · 2.12 |
   | `0xC00000D9` | `PIPE_EMPTY` | 232 | 비었는데 "안 기다림" 모드 | 2.11 |
   | `0xC00000AC` | `PIPE_NOT_AVAILABLE` | 231 | 듣는 인스턴스가 없음 (열기에서) | 2.8 |
   | `0xC00000AB` | `INSTANCE_NOT_AVAILABLE` | 231 | 인스턴스 수가 꽉 참 (만들기에서) | 2.7 |
   | `0xC0000034` | `OBJECT_NAME_NOT_FOUND` | 2 | 그런 이름 없음 | 2.8 |
   | `0xC0000022` | `ACCESS_DENIED` | 5 | 거절 (권한 · 방향 · 첫 인스턴스 요구) | 2.7 · 2.8 |

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = **번호 붙은 주문서**, IRP = 주문서를 담는 **배달 봉투**, 드라이버 객체의 `MajorFunction` 칸 → [run66 해설 2.7](run66-apphelp-cache-해설.md)
- `STATUS_PENDING` (`0x103`) = "아직 처리 중 (접수했어요)" → [run92 해설 2.7](run92-bcrypt-ksecdd-해설.md)
- IOSB · `Information` = **전표** (위 칸 도장 `Status`, 아래 칸 "몇 바이트") → [run94 해설 2.6](run94-bcrypt-resolve-해설.md)
- 이벤트 = **신호등** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)
- **run126 에서 푼 것 (이번에 제일 많이 기댄다)** → [run126 해설 2.16](run126-afd-addrlist-해설.md)
  - status 도장 색 — 초록 (성공 `0x0…`) · 노랑 (경고 `0x8…`) · 빨강 (오류 `0xC…`)
  - IRP 길 완료 규칙 — **빨강 도장이 PENDING 없이 오면 전표도 신호등도 안 건드림**
  - **동기 손잡이 + PENDING 이면 I/O 관리자 (접수 사무실) 가 안에서 기다림** (`IopSynchronousServiceTail` call `+0x3e1` · ret `+0x3e6` `IopWaitForSynchronousIo` — run126 문서), 손잡이 신호등 (파일 객체 이벤트)
- **run127 에서 푼 것** — 완료 포트 = **우편함**, 파일 객체 = 접수 사무실의 **카드**, 묶인 파일의 "끝났어요" 쪽지 조건 → [run127 해설](run127-file-completion-해설.md)
- 동기 / 비동기 손잡이 (CreateOptions `0x20` 이면 동기, 0 이면 비동기) → [run124 해설 2.3](run124-afd-socket-해설.md), 블로킹 / 비블로킹 → [run124 해설](run124-afd-socket-해설.md) ("새 말 — 블로킹 / 비블로킹")
- 파이프 = **관**, 표준 핸들 칸 셋 (입 · 귀 · 비상 확성기), `GetFileType` 이 `0x11` 을 보면 3 (파이프) → [run84 해설](run84-console-crt-해설.md) · [run83 해설](run83-console-kernelbase-해설.md)
- SD = **출입 명부** (DACL = 허락 쪽) → [run97 해설 2.0](run97-rpc-sd-query-해설.md), smss 가 부팅 때 `\Device\NamedPipe\` 에 명부를 다는 줄 → [run98 해설](run98-rpc-dir-create-해설.md)
- `GetLastError` 가 읽는 TEB `LastErrorValue` (마지막 오류 칸) → [run54 해설](run54-teb-fill-해설.md)
- 손잡이를 놓으면 `IopCloseFile` 이 `IRP_MJ_CLEANUP` 을 보낸다 → [run47 해설](run47-section-chain-해설.md)

이번에 처음 나오는 말 — **이름 있는 · 없는 파이프**, **서버 끝 · 클라이언트 끝**, **npfs**, **인스턴스**, **share (`ShareAccess`)**, **disposition**, **파이프 상태 넷 · 듣기**, **FSCTL**, **root + 빈 이름**, **이름표**, **FCB · CCB**, **쿼터**, **바이트 꼴 · 메시지 꼴**, **NOWAIT (완료 모드)**, **DOS 오류 번호**, **겹침 (`OVERLAPPED`)**, **상속 표시**, **CLEANUP · CLOSE** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**.
- run126 · run127 의 그림 그대로:
  - 드라이버 = 관리사무소 안 **전문 상담실**, I/O 관리자 = **접수 사무실** (주문서를 상담실에 건네고, 끝나면 전표를 손님 칸에 넣고 신호등을 켜는 쪽).
  - IRP = **배달 봉투**, IOSB = **전표**, 이벤트 = **신호등**, 완료 포트 = **우편함**.
- run84 의 그림 그대로: 손잡이 뒤가 상담실이면 콘솔, **창고** 면 파일, **관** 이면 파이프.
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 따름 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - 파이프 = **두 방 사이에 놓은 우체통 관**.
  - npfs = 관리사무소 안 **관 상담실**. 관을 놓아 주고, 관 안의 편지를 옮겨 준다.
  - 관을 놓는 사람 (서버) 이 "이쪽에서만 넣을 수 있어요" 같은 **방향** 을 정하는데, 관 상담실은 그 방향을 **"누구랑 나눠 쓰나 (share)" 칸 하나** 로만 읽는다.
  - 관을 놓자마자 **문이 열린 채로 기다리는 중** 이라, 맞은편 사람이 오면 바로 붙는다.
  - 한쪽이 관을 떼면, 남은 편지는 꺼낼 수 있고, 다 꺼낸 뒤엔 **"관이 깨졌어요" (`BROKEN`)** 다 — "끝까지 다 읽었어요" (`END_OF_FILE`) 가 아니다.

### 한 줄로 먼저 — 무엇을 얻었나

> **방향은 share 하나로 정한다 (2 = 들어옴, 1 = 나감, 3 = 양쪽). `DesiredAccess` 는 방향이 아니라 "처음 상태" 를 정한다.**
> **새 관 (인스턴스) 은 처음부터 "듣는 중 (LISTENING)" 이다. 그래서 서버가 `ConnectNamedPipe` 를 안 불러도 클라이언트가 열면 바로 붙는다.**
> **이미 붙은 관에 또 "기다릴게요" (`ConnectNamedPipe`) 를 하면 `0xC00000B2` → kernelbase 는 FALSE + 535. 참이 아니다.**
> **쓰는 끝이 닫힌 뒤, 남은 자료를 다 읽고 나서의 빈 읽기는 `0xC000014B` (109). `END_OF_FILE` 이 아니다.**
> **`CreatePipe` 는 syscall 셋 — 루트 열기 ① → ① 을 root 로 빈 이름 만들기 ② (읽기 핸들) → ② 를 root 로 빈 이름 열기 ③ (쓰기 핸들).**

모두 `[정적]`.

요청서 질문에 대한 답 (run 문서 "요약" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1(a) `CreateNamedPipeW` 14 인자 | access 방향별 · share 2/1/3 · disposition 2/3 · options 0/`0x20` · 시간 늘 포인터. 요청 원문 `[추측]` 셋 **다 맞음** `[정적]` | 2.4 |
| 1(b) `CreatePipe` 세 syscall | ② root = ①, ③ root = ②, ① 은 전역에 두고 안 닫음, `nSize` 0 → `0x1000` `[정적]` | 2.9 |
| 1(c) `ConnectNamedPipe` | `0x110008`, 음수는 전부 FALSE — `0xC00000B2` 도 FALSE + 535 `[정적]` | 2.10 |
| 1(d) 나머지 넷 | `WaitNamedPipeW` 는 `\DosDevices\pipe\` 를 연다 등 `[정적]` | 2.15 |
| 2(a) 만들기 | 새 이름 `0`/2, 있는 이름 `0`/1 · `0xC00000AB` · `0xC0000022`, 방향은 share 만 `[정적]` | 2.5 ~ 2.7 |
| 2(b) 열기 | 없는 이름 `0xC0000034`, 듣는 인스턴스 없음 `0xC00000AC`, 붙음 `0`/1 `[정적]` | 2.8 |
| 2(c) 닫기 | CONNECTED → CLOSING, 대기 IRP `0xC000014B`, 언제 CLEANUP 이 오나는 `[공백]` | 2.13 |
| 3(a) 읽기 | 빈 블로킹 `0x103`, NOWAIT `0xC00000D9`, 상대 닫힘 + 빈 `0xC000014B` `[정적]` | 2.11 |
| 3(b) 쓰기 | 안 붙음 `0xC00000B3`, 닫힘 `0xC00000B1`, 쿼터 안 `0` + `Length` `[정적]` | 2.12 |
| 3(c) 듣기 | CONNECTED `0xC00000B2`, LISTENING + 블로킹 `0x103`, CLOSING `0xC00000B1` `[정적]` | 2.10 |
| 3(d) `GetFileType` | ntoskrnl 이 장치 객체로 직접 `0x11` → 3, npfs 안 감 `[정적]` | 2.14 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

test.exe 흐름 **밖**, `main` 뒤의 **옆길** 이다. "파이프를 쓰는 프로그램이라면" 의 이야기다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk → … → NtContinue (은퇴)
 │
 └─ exe 진입점 → (CRT) → main
      │  CRT 시작: 표준 핸들마다 GetFileType      ← run84 (test.exe 는 콘솔 0x50 → 2)
      │
      └─ (파이프를 쓰는 프로그램이라면)
           ├─ CreatePipe / CreateNamedPipe        ← 이번 (run132)
           │    → kernelbase → syscall → nt (I/O 관리자) → npfs
           ├─ ConnectNamedPipe · ReadFile · WriteFile · CloseHandle
           └─ 자식 프로세스의 표준 출력을 파이프로 받기
```

- flow.md 3장 끝 새 절의 말: "`main` 뒤 프로그램이 파이프를 쓸 때 (자식 표준 출력 · 스레드끼리 이름 있는 파이프) 의 길이다."
- run84 의 `GetFileType` 이 표준 핸들 뒤를 묻는 바로 그 질문이, 손잡이 뒤가 파이프면 **3** 으로 답해지는 이유가 이번에 같이 닫혔다 (2.14).

### 왜 이 질문이 지금 왔나

PE_emulator 에서 파이프가 두 꼴로 막힌다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님):

- **이름 있는 파이프**:
  1. `CreateNamedPipeA("\\.\pipe\MSSE-<수>-server", …)` 가 **0**
  2. 다른 스레드의 `CreateFileA(같은 이름)` 가 **−1**
  3. `Sleep` 뒤 되풀이 — 20 / 20 같은 꼴
- **이름 없는 파이프**: `CreatePipe` 가 참인데 **두 핸들이 0** (세 syscall 다 기본 대답). 37 중 26 이 자식 프로세스를 만든다 = **자식 표준 출력 길**.

에뮬레이터가 쓸 곳: `nt_create_named_pipe_file` (지금 `named_pipe_filesystem_backend_unsupported`) · 빈 이름 + root 열기 · `\??\pipe\…` 이름 · `0x110008` · 파이프 읽기 / 쓰기. 원칙은 "**호스트에 진짜 파이프를 만들지 않는다**".

### 새 말 — 파이프 · 이름 있는 · 없는

- 파이프 = 한쪽이 쓴 바이트를 다른 쪽이 읽는 **커널 안의 통로** 다 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거에서 확인된 것 `[정적]`:
  - **이름 있는 파이프** = `\\.\pipe\X` 같은 이름으로 만든다 (`CreateNamedPipeW`). 다른 쪽은 그 이름으로 연다 (`CreateFile`).
  - **이름 없는 파이프** = `CreatePipe` 로 만든다. 이름을 **이름표에 안 올린다** (2.9). 그래서 핸들을 손에 쥔 쪽 (물려받은 자식 포함) 만 쓴다.
- 비유: 이름 있는 관 = 문패를 단 관. 이름 없는 관 = 문패 없이, 열쇠 (손잡이) 를 받은 사람만 쓰는 관.

### 새 말 — 서버 끝 · 클라이언트 끝

- 관 하나엔 **입구가 둘** 이다.
  - **서버 끝** = 관을 만든 쪽 (`NtCreateNamedPipeFile` 로 얻은 핸들).
  - **클라이언트 끝** = 맞은편에서 연 쪽 (`NtCreateFile` / `NtOpenFile` 로 얻은 핸들).
- npfs 는 파일 객체에 표식을 남겨 둘을 가른다: `FsContext2 = CCB | 3` (서버) · `CCB | 1` (클라이언트). 아랫 비트 0 = 쓸 수 있음, 비트 1 = 서버 (`NpListen +0x39 ~ +0x4f`) `[정적]`.
- `CreatePipe` 에선 **읽기 핸들 = 서버 끝, 쓰기 핸들 = 클라이언트 끝** 이다 `[정적]`.

### 새 말 — npfs

- `System32\drivers\npfs.sys` (120,264 B). **이름 있는 파이프 파일 시스템** 이라는 커널 드라이버이고, 장치 이름은 `\Device\NamedPipe` 다 (`.rdata` rva `0x42b8`) `[정적]`.
- 부팅 때 `DriverEntry +0x149 NpCreateDevice` → `+0x60 IoCreateDevice` 로 장치를 만든다. 장치 종류 **`0x11`**, 특성 `0x20000` `[정적]`.
- 비유: 관리사무소 안 **관 상담실**.

### 무엇이 궁금했나 — 요청서 질문 셋

1. **kernelbase 가 넘기는 것** — `CreateNamedPipeW` 14 인자 옮김표 · `CreatePipe` 세 syscall 의 인자와 root · `ConnectNamedPipe` 의 status → 반환값 (+ `WaitNamedPipeW` 등 넷).
2. **npfs 만들기 · 열기 · 닫기** — 조건별 status · `Information`.
3. **데이터 길** — 읽기 · 쓰기 · 듣기의 완료 꼴, `GetFileType` 의 장치 종류.

요청 원문엔 `[추측]` 이 셋 있었다 — disposition 2 ↔ `FILE_FLAG_FIRST_PIPE_INSTANCE`, options 0 ↔ `OVERLAPPED`, 종류 비트 1 ↔ `PIPE_REJECT_REMOTE_CLIENTS`. 이번에 **셋 다 맞다** 고 나왔다 (2.4).

### 바퀴 전에 알던 것

- `[정적]` (run84) `GetFileType` 이 `DeviceType 0x11` 을 보면 → 파이프. 그런데 **누가** `0x11` 을 답하나는 안 봤다.
- `[정적]` (run98) smss 가 부팅 때 `\Device\NamedPipe\` 에 보안 (명부) 을 단다 (`SmpCreateProtectedPrefixes` 의 `NtSetSecurityObject`).
- `[정적]` (run126, sast/124) IRP 완료 규칙 — 오류 + PENDING 없음이면 IOSB 안 씀, 동기 핸들의 PENDING 은 I/O 관리자가 기다림.
- `[정적]` (run127, sast/125) 파일을 완료 포트에 묶기, 묶인 파일의 "끝났어요" 쪽지 조건.
- npfs 는 **한 번도 analyze 한 적이 없었다.** PDB 도 이번에 처음 받았다.

---

## 2. 찾은 것

### 2.1 한눈에 — 다섯 줄기

run 문서 "호출 관계" 를 줄인 것이다 (오프셋은 ret 자리, `⇒` 는 syscall · I/O 관리자를 건너뜀). 모두 `[정적]`.

```
(1) 이름 있는 파이프 만들기
kernelbase!CreateNamedPipeW
 ├─ +0xd1   RtlDefaultNpAcl → NtQueryInformationToken ×3     SD 없을 때 기본 명부
 └─ +0x235  NtCreateNamedPipeFile   ── SYSCALL  14 인자
              ⇒ nt!NtCreateNamedPipeFile +0x151 IopCreateFile (CreateFileType 1)
                 ⇒ IopParseDevice +0xfac MajorFunction = 1
                    ⇒ npfs!NpFsdCreateNamedPipe
                         ├─ +0x310  NpCreateNewNamedPipe        새 이름 → 0 · Information 2 · LISTENING
                         └─ +0x3d2  NpCreateExistingNamedPipe   있는 이름 → 0 · 1 / 0xC00000AB / 0xC0000022

(2) 이름 없는 파이프
kernelbase!CreatePipe
 ├─ ① +0x210  NtOpenFile(\Device\NamedPipe\)          처음 한 번만 · 전역 0x3ad980 · 안 닫음
 ├─ ② +0x106  NtCreateNamedPipeFile(root ①, 빈 이름)  = 읽기 핸들 (서버 끝)
 └─ ③ +0x15d  NtOpenFile(root ②, 빈 이름)             = 쓰기 핸들 (클라이언트 끝)
                ⇒ npfs!NpFsdCreate +0x2f7 NpCreateClientEnd → +0x2e5 NpSetConnectedPipeState

(3) 듣기
kernelbase!ConnectNamedPipe +0x6a NtFsControlFile(0x110008)
 ⇒ npfs!NpFsdFileSystemControl +0x27 NpCommonFileSystemControl +0x1c6 NpListen

(4) 읽기 · 쓰기 · 닫기
NtReadFile  ⇒ npfs!NpFsdRead
NtWriteFile ⇒ npfs!NpFsdWrite +0x6d NpCommonWrite
CloseHandle … nt!IopCloseFile +0x10a CLEANUP ⇒ npfs!NpFsdCleanup +0x27 NpCommonCleanup +0x198 NpSetClosingPipeState

(5) 종류 묻기
kernelbase!GetFileType +0x79 NtQueryVolumeInformationFile(4)
 ⇒ nt 가 직접: 장치 객체 DeviceType 0x11 → FILE_TYPE_PIPE 3        (npfs 안 감)
```

- 비유로: 가게 직원 (kernelbase) 이 신청 창구 (syscall) 에 "관 놓아 주세요" 를 내면, 접수 사무실 (I/O 관리자) 이 봉투에 "**관 놓기 (1번 칸)**" 표시를 해서 관 상담실 (npfs) 로 보낸다. 관 상담실은 관을 놓고 문을 열어 둔다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ ② +0x106  NtCreateNamedPipeFile(root ①, 빈 이름)  = 읽기 핸들 (서버 끝)`

- **누가**: `kernelbase!CreatePipe`.
- **어디서**: call 은 `+0xff`, 돌아올 자리 (ret) 는 `+0x106` (run 문서 호출 지점 표). 나무에는 ret 인 `+0x106` 이 적힌다.
- **누구를**: `NtCreateNamedPipeFile` — 파이프 **서버 끝을 만드는** syscall.
- **무엇을 위해**: 이름 없는 파이프의 **읽기 핸들** 을 얻으려고. 이름 칸은 **비워 두고** (Length 0, Buffer NULL), 대신 "기준 손잡이 (root)" 칸에 ① 에서 연 `\Device\NamedPipe\` 손잡이를 넣는다 `[정적]` (`+0x6e` · `+0x72` · `+0x88`).
- 이 줄이 바로 에뮬레이터가 막힌 자리다. 에뮬레이터는 ① 에 손잡이를 안 줘서, 로그엔 "root 0" 으로 찍혔다 (2.16).

#### 줄 2: `⇒ nt!NtCreateNamedPipeFile +0x151 IopCreateFile (CreateFileType 1)`

- **누가**: 커널의 `nt!NtCreateNamedPipeFile`.
- **어디서**: call `+0x14c`, ret `+0x151`.
- **누구를**: `IopCreateFile` — 파일 열기 · 만들기의 공용 길.
- **무엇을 위해**: "이건 **파이프 만들기** 다" (`CreateFileType = 1`, `+0x10e`) 와 파이프 설정 묶음 (`NAMED_PIPE_CREATE_PARAMETERS`) 포인터 (`+0x109`) 를 실어 보내려고 `[정적]`.
  - 그 두 값은 `OPEN_PACKET +0x78` · `+0x80` 에 들어가고 (`IopCreateFile +0x2c6` · `+0x2d1`),
  - `IopParseDevice +0xfa3` 에서 1 이면 `+0xfac` 에서 봉투의 `MajorFunction` 을 **1** 로 적는다.
- **ntoskrnl 은 14 인자의 뜻을 안 본다** — 값 검사는 전부 npfs 몫이다. 커널이 하는 건 "유저 모드일 때 시간 포인터의 정렬 · 읽기 검사" 뿐 (`+0x5c` · `+0x62`) `[정적]`.

#### 줄 3: `⇒ npfs!NpFsdCreate +0x2f7 NpCreateClientEnd → +0x2e5 NpSetConnectedPipeState`

- **누가**: npfs 의 클라이언트 열기 입구 `NpFsdCreate`.
- **어디서**: call `+0x2f2`, ret `+0x2f7`. 그 안에서 `NpCreateClientEnd` 가 call `+0x2e0`, ret `+0x2e5` 로 `NpSetConnectedPipeState` 를 부른다.
- **무엇을 위해**: 듣던 인스턴스를 **붙은 상태 (CONNECTED)** 로 바꾸고, 기다리던 듣기 봉투 (`0x110008`) 가 있으면 **status 0 으로 끝내려고** `[정적]` (`NpFsdCreate +0x353 IofCompleteRequest` 완료 고리).

### 2.3 관 상담실의 입구 — `MajorFunction` 칸 `[정적]`

`DriverEntry +0x8b ~ +0x142` 가 드라이버 객체의 `MajorFunction` 칸 (`_DRIVER_OBJECT +0x70`) 을 채운다. 봉투 종류 번호 (IRP_MJ) 마다 받는 함수가 다르다.

#### 새 말 — `IRP_MJ_CREATE` 와 `IRP_MJ_CREATE_NAMED_PIPE`

- 봉투 겉면의 **종류 번호** 다. 0 = 보통 열기 (CREATE), **1 = 파이프 만들기 (CREATE_NAMED_PIPE)** `[정적]` (run 문서 들머리 표).
- npfs 는 둘을 **다른 함수** 로 받는다:

| IRP_MJ | npfs 함수 (rva) | 뜻 |
|---|---|---|
| CREATE (0) | `NpFsdCreate` (`0xe3c0`) | **클라이언트 열기** |
| CREATE_NAMED_PIPE (1) | `NpFsdCreateNamedPipe` (`0xeba0`) | **서버 만들기** |
| CLOSE (2) · CLEANUP (0x12) | `NpFsdClose` · `NpFsdCleanup` | 닫기 (2.13) |
| READ (3) · WRITE (4) | `NpFsdRead` · `NpFsdWrite` | 읽기 · 쓰기 |
| FSCTL (0xd) | `NpFsdFileSystemControl` | 듣기 · 끊기 · 기다리기 · 엿보기 |

(전체 표는 run 문서 2절. 안 채운 `DEVICE_CONTROL` (0xe) 등은 I/O 관리자 기본 `[추측]`.)

- 비유: 관 상담실 문이 여러 개다. "관 놓아 주세요" 는 1번 문, "저 관에 연결해 주세요" 는 0번 문.
- 그래서 같은 `\Device\NamedPipe` 를 향해도 **서버는 `NtCreateNamedPipeFile`, 클라이언트는 `NtCreateFile` / `NtOpenFile`** 로 들어온다.

### 2.4 ★ `CreateNamedPipeW` 가 넘기는 14 인자 `[정적]`

`kernelbase!CreateNamedPipeW +0x22e NtCreateNamedPipeFile` (ret `+0x235`). A 판 `kernel32!CreateNamedPipeA` 는 이름만 유니코드로 바꿔 W 를 부른다 (`+0x6a`).

#### 새 말 — share (`ShareAccess`)

- 파일을 열 때 "**나 말고 누가 같이 읽어도 / 써도 되나**" 를 적는 칸이다. 1 = 읽기 같이 허락, 2 = 쓰기 같이 허락, 3 = 둘 다 `[추측: 일반적으로 알려진 설명]`.
- **파이프에선 이 칸이 방향이 된다** — 2.5 에서 푼다.

#### 새 말 — disposition

- "이 이름이 **이미 있으면 / 없으면** 어떻게 할까" 를 적는 칸이다 (`CreateDisposition`).
- 이번에 나오는 값 (결과서 이름):
  - 1 = `FILE_OPEN` (있을 때만 연다)
  - 2 = `FILE_CREATE` (없을 때만 만든다)
  - 3 = `FILE_OPEN_IF` (있으면 열고 없으면 만든다)
- 비유: 관 놓기 신청서의 "**이미 같은 문패가 있으면?**" 칸.

#### 옮김표 (대표만 — 전체는 run 문서 1(a))

| # | 인자 | kernelbase 가 넣는 값 | 줄 |
|---|---|---|---|
| 2 | `DesiredAccess` | 들어옴 `0x80100000` · 나감 `0x40100000` · 양쪽 `0xc0100000` (+ `dwOpenMode & 0x010c0000`) | `+0x1a7` · `+0x2e1` · `+0x339` · `+0x1ef` |
| 5 | `ShareAccess` | 들어옴 **2** · 나감 **1** · 양쪽 **3** | `+0x183` · `+0x2e6` · `+0x33e` |
| 6 | `CreateDisposition` | `FILE_FLAG_FIRST_PIPE_INSTANCE` (`0x80000`) 면 **2**, 아니면 **3** | `+0x1d4 ~ +0x1e8` |
| 7 | `CreateOptions` | `OVERLAPPED` 없으면 **`0x20`** (동기 손잡이), 있으면 0 | `+0x137 ~ +0x14d` |
| 8 | `NamedPipeType` | 비트 0 = `PIPE_TYPE_MESSAGE`, 비트 1 = `PIPE_REJECT_REMOTE_CLIENTS` | `+0x1b1 ~ +0x1d1` |
| 11 | `MaximumInstances` | 255 → `0xffffffff`, 아니면 그 수 | `+0x66 ~ +0x79` |
| 12 · 13 | 쿼터 둘 | `nInBufferSize` · `nOutBufferSize` **그대로** (0 이어도) | `+0x1f5` · `+0x1e5` |
| 14 | `DefaultTimeout` | **늘 포인터**. 0 → `-500000` (상대 50 ms) | `+0x150 ~ +0x168` · `+0x2f1` |

- 요청 원문의 `[추측]` 셋 — disposition 2 ↔ `FIRST_PIPE_INSTANCE` (`+0x1d4`), options 0 ↔ `OVERLAPPED` (`+0x137 ~ +0x14d`), 종류 비트 1 ↔ `REJECT_REMOTE_CLIENTS` (`+0x1c3 ~ +0x1d1`) — 이 **다 맞다** `[정적]`.

#### 새 말 — 겹침 (`OVERLAPPED`)

- `FILE_FLAG_OVERLAPPED` 로 만들면 I/O 를 낸 뒤 **먼저 돌아오고 나중에 결과를 받는** 손잡이가 된다 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거에서 확인된 것: 이 플래그가 있으면 CreateOptions 0 (비동기 손잡이), 없으면 `0x20` (동기 손잡이) `[정적]`. 동기 / 비동기 손잡이는 [run124 해설 2.3](run124-afd-socket-해설.md) 그대로.

#### 부르기 전에 하는 일 — 명부를 늘 단다

- SD (출입 명부) 를 안 주면 `+0xca ntdll!RtlDefaultNpAcl` 로 **기본 명부** 를 만들어 단다. 그래서 OA (이름 · 명부 묶음) 의 명부 칸은 **거의 늘 NULL 이 아니다** `[정적]` (`+0xc5 ~ +0x117`).
- `RtlDefaultNpAcl` 은 토큰을 세 번 묻는다 (`NtQueryInformationToken` 클래스 4 · `0x1d` · `0x1f`) — 에뮬레이터 로그에서 이 호출이 `NtCreateNamedPipeFile` **앞에** 나와야 한다 (run 문서).
- 이름은 `+0x80 RtlDosPathNameToNtPathName_U` 로 `\\.\pipe\X` → `\??\pipe\X` 로 바뀐다 `[정적]`.

#### 돌아온 뒤 — 핸들 칸을 그대로 돌려준다

- status ≥ 0 이고 `Information == 1` (이미 있던 이름) → 핸들 + `0xb7` (`ERROR_ALREADY_EXISTS`).
- 그 밖 성공 → 핸들 + 0. 음수 → `-1` + DOS 오류 `[정적]`.
- 핸들은 `+0x2b1 MOV RAX,[RSP+0x78]` — **커널이 칸을 안 쓰면 그 자리 값 그대로** 돌려준다. 요청 쪽 로그의 "status 0 + 0 반환" 이 바로 이 줄이다 (2.16).

#### 새 말 — DOS 오류 번호

- 커널 status (`0xC…`) 를 Win32 쪽 작은 번호로 바꾼 것. `GetLastError` 가 돌려주는 값이다 ([run54 해설](run54-teb-fill-해설.md) 의 `LastErrorValue` 칸).
- 바꾸는 표는 ntdll 에 있다 (`RtlpRunTable` rva `0x195b50` · `RtlpStatusTable` rva `0x193a10` — 결과서가 일회용으로 읽음) `[정적]`. 이 글의 "→ 535" 같은 숫자가 이 번호다.

### 2.5 ★ 방향은 share 하나로 정한다 `[정적]`

이번 바퀴에서 제일 의외인 사실이다. 단계별로.

1. **근거**: `NpCreateNewNamedPipe +0x375 ~ +0x3b4` 가 share 를 본다. 2 → 들어옴 (서버가 읽음), 1 → 나감, 3 → 양쪽. 그 밖 (0, 4 이상) 은 `0xC000000D` `[정적]`.
2. **근거**: 같은 함수에서 `DesiredAccess` 는 방향 셈에 안 들어간다. 대신 **처음 상태** 를 정한다 (`+0x1c5 ~ +0x20a`, 2.6) `[정적]`.
3. **그래서**: kernelbase 가 access 와 share 를 **둘 다** 방향에 맞춰 넣어 주지만 (2.4 표), npfs 가 방향으로 읽는 건 share 쪽이다.
4. **에뮬레이터에게**: 방향을 access 비트로 셈하면 안 되고 share 로 셈해야 한다.

비유: 관 놓기 신청서엔 "어느 쪽에서 넣나" 와 "누구랑 나눠 쓰나" 가 둘 다 적혀 오는데, 관 상담실은 **"누구랑 나눠 쓰나" 칸만 보고 관 방향을 정한다.**

### 2.6 ★ 새 관은 처음부터 "듣는 중" `[정적]`

#### 새 말 — 파이프 상태 넷 · 듣기

- npfs 는 인스턴스마다 상태 한 바이트를 둔다 (CCB `+0x8`). 값과 이름:

| 값 | 이름 `[추측: 이름]` | 뜻 (이번 근거) |
|---|---|---|
| 1 | DISCONNECTED | 끊김 (서버가 `DisconnectNamedPipe` 한 뒤 등) |
| 2 | **LISTENING** | **듣는 중** — 클라이언트가 오면 바로 붙을 수 있음 |
| 3 | CONNECTED | 붙음 |
| 4 | CLOSING | 한쪽이 닫음 |

- **듣기** = 서버 끝이 "클라이언트가 붙을 때까지 기다릴게요" 하는 것. Win32 로는 `ConnectNamedPipe`, 커널로는 FSCTL `0x110008` 이다 (2.10).
- 비유: 관 문이 열린 채 기다리는 상태가 LISTENING.

#### 처음 상태를 정하는 줄

- `DesiredAccess & 0xf2000003 ≠ 0` 이면 **LISTENING (2)**, 0 이면 DISCONNECTED (1) (`NpCreateNewNamedPipe +0x1c5 ~ +0x20a`, 같은 식 `NpCreateExistingNamedPipe +0x18b ~ +0x1ab`) `[정적]`.
- 그 비트는 데이터 권한 비트다 (결과서: `FILE_READ_DATA` · `FILE_WRITE_DATA` · `GENERIC_*` · `MAXIMUM_ALLOWED` `[추측: 이름]`).
- kernelbase 가 넣는 access (`0x80100000` 등) 엔 늘 `GENERIC_*` 비트가 있다. 요청 쪽 로그의 서버 access 도 전부 그렇다. → **처음부터 LISTENING**.

#### 그래서

1. 새 인스턴스는 만들어지자마자 FCB 의 **듣는 인스턴스 목록** (`+0x138`) 에 있다.
2. 클라이언트 열기 (`NpCreateClientEnd`) 가 그 목록에서 바로 찾는다 (2.8).
3. → **서버가 `ConnectNamedPipe` 를 안 불러도 클라이언트가 붙는다.**
4. 서버가 나중에 `ConnectNamedPipe` 를 부르면, 이미 붙어 있으니 `0xC00000B2` 가 온다 (2.10).

비유: 관을 놓자마자 문이 열려 있다. "기다릴게요" 는 안 불러도 된다.

### 2.7 같은 이름에 관을 하나 더 — 검사 차례 `[정적]`

#### 새 말 — 인스턴스

- 같은 이름 (문패) 아래 놓인 **관 한 가닥** 이다. 서버가 같은 이름으로 `CreateNamedPipe` 를 또 부르면 인스턴스가 하나 늘고, 클라이언트 하나가 인스턴스 하나에 붙는다 `[정적]` (2(a) · 2(b) 표).
- 최대 수는 **첫 인스턴스가 정한 값** 이다 (`FCB +0xfc`) `[정적]`.
- 비유: 같은 문패 밑에 관이 여러 가닥. 손님 한 명이 관 한 가닥을 쓴다.

#### 새 말 — 이름표 · FCB · CCB

- **이름표** = npfs 가 파이프 이름을 올려 두는 표 (prefix 표). `NpFindPrefix` 로 찾는다 `[정적]`. 비유: 관 상담실 벽의 **문패 판**.
- **FCB** = 이름 하나 (문패 하나) 의 기록 — 최대 수 · 방향 · 종류 · 듣는 인스턴스 목록 등.
- **CCB** = 인스턴스 하나 (관 한 가닥) 의 기록 — 상태 · 두 방향 대기열 · 양쪽 파일 객체 등.
- 칸 위치는 결과서가 코드에서 읽은 것만 적었다 (PDB 타입 없음, 조심할 곳 4).

#### 이미 있는 이름에서 — 이 차례로 거른다

| 차례 | 검사 | 걸리면 | 줄 (`NpCreateExistingNamedPipe`) |
|---|---|---|---|
| 1 | 파이프 명부가 "인스턴스 만들기" 권한을 주나 | 보통 `0xC0000022` | `+0xa0` · `+0x12d` |
| 2 | **인스턴스 수가 꽉 찼나** | **`0xC00000AB`** (→ 231) | `+0x136 ~ +0x13f` → `+0x267` |
| 3 | **disposition 2 인가** (`FIRST_PIPE_INSTANCE`) | **`0xC0000022`** (→ 5) | `+0x145` · `+0x14a` → `+0x293` |
| 4 | **방향 (share) 이 첫 인스턴스와 같나** | **`0xC0000022`** | `+0x153 ~ +0x17a` → `+0x293` |
| 통과 | | **`0`** · `Information` **`1`** | `+0x244` · `+0x247` |

- 순서가 중요하다: 꽉 찬 이름에 `FIRST_PIPE_INSTANCE` 로 오면 3 이 아니라 **2 에서** `0xC00000AB` 로 걸린다.
- **안 견주는 것**: 종류 · 읽기 모드 · 쿼터 · 최대 · 시간. 둘째 인스턴스의 읽기 · 완료 모드 · 쿼터는 **자기 CCB 에만** 들어간다 (`+0x180 ~ +0x1c6`) `[정적]`.

#### 새 이름이면

- 이름표에 없고, 첫 글자 `\`, 길이 > 2 B → **`0`** · `Information` **`2`** (`NpCreateNewNamedPipe +0x2f8` · `+0x2fb`) `[정적]`.
- 새 이름인데 disposition 1 (`FILE_OPEN`) → `0xC0000034`. `TimeoutSpecified 0` · 최대 0 · 시간 ≥ 0 · share 가 1 · 2 · 3 밖 · 바이트 꼴 + 메시지 읽기 · 이름이 `\` 뿐 → `0xC000000D` `[정적]`.
- `Information` 1 / 2 가 kernelbase 쪽에서 "`ERROR_ALREADY_EXISTS` 냐 0 이냐" 를 가른다 (2.4).

### 2.8 클라이언트 열기 — 언제 붙고 언제 거절되나 `[정적]`

`NpFsdCreate` → `NpCreateClientEnd`. 대표만 (전체는 run 문서 2(b)).

| 경우 | status | `Information` | 줄 |
|---|---|---|---|
| **없는 이름** | **`0xC0000034`** (→ 2) | 0 | `NpFsdCreate +0x26b` · `+0x26f` → `+0x379 ~ +0x385` |
| 이름은 있는데 인스턴스 0 개 (서버 끝 다 닫음) | `0xC0000034` | 0 | `+0x290` · `+0x298` → `+0x5d7` |
| **듣는 인스턴스 없음** (모두 붙었거나 닫는 중 · 끊김) | **`0xC00000AC`** (→ 231 `ERROR_PIPE_BUSY`) | 0 | `NpCreateClientEnd +0x162 ~ +0x170` → `+0x360` |
| **방향과 안 맞는 access** (읽기인데 들어옴 파이프 등) | `0xC0000022` | 0 | `+0x140 ~ +0x14d` · `+0x31f ~ +0x339` |
| **붙음** | **`0`** | **`1`** | `NpFsdCreate +0x2f2` |

- **붙으면** (2.2 줄 3): 인스턴스 LISTENING → CONNECTED, 클라이언트 파일 객체 `FsContext2 = CCB | 1`, **기다리던 듣기 봉투를 status 0 으로 끝냄** `[정적]`.
- 클라이언트 열기는 **share · disposition 을 안 쓴다** (`FILE_DIRECTORY_FILE` 갈래에만). 그래서 `CreateFile(OPEN_ALWAYS)` 로도 없는 파이프는 안 생기고 `0xC0000034` 다 `[정적]`.
- 여기서 "방향" 은 access 로 본다. 2.5 의 "방향은 share" 는 **만들 때** 이야기이고, 열 때는 "이 관 방향에 맞는 권한을 달라고 했나" 를 access 로 거른다.
- 비유: 문패가 없으면 "그런 관 없어요". 문패는 있는데 열린 관이 없으면 "지금 다 바빠요 (BUSY)".

### 2.9 ★ `CreatePipe` — 세 syscall 과 "root + 빈 이름" `[정적]`

#### 새 말 — root + 빈 이름

- 파일을 열 때 OA 엔 이름 칸 말고 **기준 손잡이 (RootDirectory)** 칸이 있다. 이름을 "그 손잡이 기준으로" 읽는다 `[추측: 일반적으로 알려진 설명]`.
- npfs 는 이 칸을 이렇게 쓴다 `[정적]`:
  - root = **루트 (`\Device\NamedPipe\`) 손잡이** + 빈 이름 + 만들기 → **이름 없는 파이프** 를 만든다 (이름표에 안 올림) (`NpFsdCreateNamedPipe +0xf4 ~ +0x10b` · `+0x318 ~ +0x329`).
  - root = **서버 끝 손잡이** + 빈 이름 + 열기 → **그 관의 클라이언트 끝** 을 연다 (`NpFsdCreate +0x4b4 ~ +0x4d6` → `+0x27a`).
- 비유: "이 열쇠가 여는 관 상담실에 문패 없는 관 하나 놓아 주세요", "이 관의 맞은편 입구를 주세요".

#### 세 syscall

| syscall | access | 이름 · root | share · options · 그 밖 |
|---|---|---|---|
| ① `NtOpenFile` (call `+0x209`) | `0x80100000` | `\Device\NamedPipe\` (Length `0x24`, **MaximumLength 0**), Root 0, **Attributes 0** | share 3 · options `0x20` |
| ② `NtCreateNamedPipeFile` (call `+0xff`) | `0x80100100` | **빈 이름**, **Root = ①**, Attributes `0x40` / `0x42` | share 3 · disposition 2 · options `0x20` · 최대 1 · 쿼터 둘 다 `nSize` 또는 `0x1000` · 시간 = 상대 120초 |
| ③ `NtOpenFile` (call `+0x156`) | `0x40100080` | **빈 이름**, **Root = ②**, Attributes `0x40` / `0x42` | share 3 · options `0x60` |

- ① 의 Attributes 0 = **대소문자 구분** · 상속 없음이다 `[정적]` (run 문서). 에뮬레이터가 이름을 대소문자 무시로만 찾으면 안 맞을 수 있는 자리다 `[추측]`.

#### ① 은 프로세스에 하나, 안 닫는다

- 처음 한 번만 열고 kernelbase 전역 (rva `0x3ad980`) 에 `LOCK CMPXCHG` 로 넣는다 (`+0x46` · `+0x21f`) `[정적]`.
- 다른 스레드가 먼저 넣었으면 **내 것** 을 `NtClose` 한다 (`+0x232`).
- **끝에 안 닫는다.** refs 로 이 칸을 만지는 곳은 `CreatePipe` 의 세 줄뿐이다 (`ms/disassemble/kernelbase/_refs/0x3ad980.tsv`).
- `.asm` 의 `InteractiveUserNameSpaceSeparation+0x8` 주석은 "가장 가까운 앞 PDB 이름" 일 뿐, 그 변수가 아니다.
- 비유: 관 상담실 열쇠 (①) 는 가게에 하나 걸어 두고 계속 쓴다.

#### 새 말 — 쿼터

- 관 한 방향에 **쌓아 둘 수 있는 바이트 양** 이다. 쓰기는 `Quota − QuotaUsed` 안이면 바로 끝나고, 넘으면 기다린다 (2.12) `[정적]`.
- `CreatePipe` 는 `nSize` 0 이면 `0x1000` (두 방향 다). `CreateNamedPipe(…, 0, 0, …)` 는 **0 그대로** 간다 — 올려 주는 줄이 없다 `[정적 읽기, 동작은 동적 권함]`.
- 비유: 관 안에 쌓아 둘 수 있는 편지 양.

#### 새 말 — 상속 표시

- 손잡이에 "**자식 프로세스에게 물려줘도 된다**" 고 붙이는 표시. OA Attributes 의 `0x2` (`OBJ_INHERIT`) 다 (결과서 이름).
- `bInheritHandle ≠ 0` 이면 **② ③ 둘 다** `0x42` (`+0x19e ~ +0x1af`), ① 은 없음 `[정적]`. SD 도 ② ③ 에 간다.
- 자식 표준 출력 길 (37 중 26) 이 이 표시로 자식에게 쓰기 끝을 넘긴다 `[추측: 일반적으로 알려진 쓰임]`.

#### 실패 · 성공

- ① · ② 실패 → `FALSE` (출력 안 건드림). ③ 실패 → **② 핸들만 닫고** (`+0x251`) `FALSE`.
- 성공 → `*hReadPipe = ②`, `*hWritePipe = ③`, 반환 1 (`+0x170 ~ +0x179`) `[정적]`.
- npfs 쪽에서 본 모양: share 3 → 양쪽, 바이트 꼴, 최대 1, 이름표에 안 올라감. ③ 이 열리는 순간 2.8 의 "붙음" 길을 타서 CONNECTED 가 된다.

### 2.10 ★ `ConnectNamedPipe` — 이미 붙었으면 FALSE + 535 `[정적]`

#### 새 말 — FSCTL

- `NtFsControlFile` 로 내는 **파일 시스템용 주문서** 다. IOCTL (번호 붙은 주문서) 과 닮은 꼴이다 `[추측: 일반적으로 알려진 설명]`.
- 봉투 종류가 FSCTL (0xd) 이라 npfs 의 `NpFsdFileSystemControl` 로 간다 `[정적]` (2.3 표).
- 이번에 나오는 번호: `0x110008` 듣기 · `0x110004` 끊기 · `0x110018` 기다리기 · `0x11400c` 엿보기 (`NpCommonFileSystemControl` 의 네 call) `[정적]`.

#### kernelbase 가 내는 꼴

`NtFsControlFile(h, ev, 0, ctx, iosb, 0x110008, 0, 0, 0, 0)` — 입출력 버퍼는 없다.

- 동기 호출 (`lpOverlapped == NULL`): 이벤트 · ApcContext NULL, IOSB 는 지역 `[RSP+0x50]` `[정적]`.
- 겹침 호출: `Event = hEvent`, `ApcContext = lpOverlapped` (`hEvent` 아랫 비트 1 이면 NULL), IOSB = `lpOverlapped` 자신, 부르기 전에 `Internal = 0x103` `[정적]` (`+0x21 ~ +0x39`).

#### npfs 가 상태별로 돌려주는 것 (`NpListen`)

| 상태 | status | 완료 꼴 |
|---|---|---|
| 서버 끝 아님 | `0xC00000AF` (→ 1) | 바로 |
| **CONNECTED (이미 붙음)** | **`0xC00000B2`** (→ 535) | 바로 (빨강 도장이라 전표 안 씀 — run126) |
| **LISTENING + 블로킹** | **`0x103`** | 듣기 목록에 걸고 취소 루틴 달기. 붙으면 **0** 으로 끝남 |
| LISTENING + NOWAIT | `0xC00000B3` (→ 536) | 바로 |
| DISCONNECTED (끊은 뒤 다시 듣기) | 기다리던 `WaitNamedPipe` 들을 깨우고 LISTENING 으로 바꾼 뒤 블로킹 `0x103` / NOWAIT `0xC00000B3` | 위와 같음 |
| **CLOSING (상대가 닫음)** | **`0xC00000B1`** (→ 232) | 바로 |
| 이미 취소된 봉투 | `0xC0000120` (→ 995) | 바로 |

(줄은 run 문서 3(c) 표. 예: CONNECTED 는 `NpListen +0x90 ~ +0x99`.)

- PENDING 으로 걸린 듣기 봉투가 끝나는 넷: 클라이언트 붙음 → 0, 서버 끝 닫힘 → `0xC000014B`, `DisconnectNamedPipe` → `0xC00000B0`, 취소 → `0xC0000120` `[정적]`.

#### 새 말 — NOWAIT (완료 모드)

- 파이프 끝마다 "**기다림 없이 바로 답해 달라**" 는 모드가 있다 (`CompletionMode`, `dwPipeMode & 1` = `PIPE_NOWAIT`) `[정적]`.
- 이 모드면 기다려야 할 자리에서 `0x103` 대신 오류 (`0xC00000B3` · `0xC00000D9`) 로 바로 끝난다.

#### kernelbase 가 status 를 반환값으로 바꾸는 법

| `NtFsControlFile` 반환 | `ConnectNamedPipe` | `GetLastError` | 줄 |
|---|---|---|---|
| `0` | `TRUE` | 안 건드림 | `+0x76` → `+0x8d` · `+0x94` |
| 음수 (**`0xC00000B2` 포함**) | **`FALSE`** | DOS 오류 (`0xC00000B2` → **535**) | `+0x76` → `+0x78` · `+0x7a` |
| `0x103` + 겹침 | `FALSE` | 997 | `+0x8d` · `+0x92` → `+0x78` |
| `0x103` + `lpOverlapped == NULL` | `+0xaa NtWaitForSingleObject(h)` 뒤 IOSB status 로 다시 판정 | — | `+0x9b` · `+0xa0` · `+0xaa` · `+0xba` |

- **kernelbase 안에 `0xC00000B2` 를 따로 다루는 줄은 없다** — `+0x76` 이 음수를 다 실패 길로 보낸다 `[정적]`.
  - 그래서 요청 쪽 질문 "`STATUS_PIPE_CONNECTED` → 참 + `ERROR_PIPE_CONNECTED`?" 의 답은 **참이 아니라 FALSE + 535** 다.
  - "FALSE 인데 535 면 사실은 붙은 것" 으로 읽는 건 **부르는 프로그램 몫** 이다 `[추측: 일반적으로 알려진 쓰임]`.
- `+0xaa` 기다림은 **비동기 손잡이를 `lpOverlapped` 없이 부를 때만** 탄다. 동기 손잡이 (`0x20`) 면 접수 사무실이 `NtFsControlFile` 안에서 이미 기다리기 때문이다 (run126) `[정적]`.
- 겹침 + `0xC00000B2` 이면 `Internal` 이 `0x103` 으로 남을 수 있다 — 빨강 도장은 전표를 안 쓰니까 (run126) `[추측: 이어 붙인 결론]`.

비유: 이미 손님이 붙어 있는데 "기다릴게요" 하면 관 상담실이 "**벌써 붙었어요**" 를 **빨강 도장** 으로 찍어 준다. 직원 (kernelbase) 은 빨강이면 무조건 "실패" 칸에 적는다.

### 2.11 ★ 읽기 — 상대가 닫으면 `BROKEN`, `END_OF_FILE` 아님 `[정적]`

`NpFsdRead` (rva `0x11d70`). 대표만 (전체는 run 문서 3(a)).

| 상황 | status | `Information` | 기다림 | 줄 |
|---|---|---|---|---|
| LISTENING (아직 안 붙음) | `0xC00000B3` (→ 536) | 0 | 아니오 | `+0xc5` → `+0x34b` |
| **자료 있음, 바이트 꼴** | **`0`** — 모자라도 **있는 만큼만** | `min(Length, 있는 바이트)` | 아니오 | `+0x10d` → `+0x27e NpReadDataQueue` |
| 자료 있음, 메시지 꼴 + 버퍼 작음 | `0x80000005` (→ 234), 나머지는 큐에 | 복사한 바이트 | 아니오 | `NpReadDataQueue +0x36e` · `+0x37c` |
| **빈 + 상대 닫힘 (CLOSING)** | **`0xC000014B`** (→ 109) | 0 | 아니오 | `+0x118` · `+0x11b` → `+0x23a` |
| 빈 + NOWAIT | `0xC00000D9` (→ 232) | 0 | 아니오 | `+0x121` · `+0x124` → `+0x298` |
| **빈 + 블로킹** | **`0x103`** (`+0x154 NpAddDataQueueEntry` 가 큐에 걸고 취소 루틴 달기) | — | **예** | `+0x154` |

#### 새 말 — 바이트 꼴 · 메시지 꼴

- **바이트 꼴** = 쓴 바이트가 한 줄로 이어진다. 읽으면 있는 만큼 가져간다.
- **메시지 꼴** = 쓴 덩어리 하나가 메시지 하나다. 버퍼가 작으면 `0x80000005` 로 "더 있어요" 를 알린다.
- `NamedPipeType` 비트 0 (2.4) 과 읽기 모드가 정한다 `[정적]`. `CreatePipe` 파이프는 바이트 꼴이다.

#### 왜 "닫힌 뒤에도 남은 자료가 먼저 읽히나" — 줄 차례로

1. **근거**: "자료 있음" 검사 (`+0x10d`) 가 "CLOSING" 검사 (`+0x118`) 보다 **앞** 에 있다 `[정적]`.
2. **근거**: 닫을 때 `NpSetClosingPipeState` 는 **상대가 이미 써 둔 자료를 남긴다** (`+0x4b ~ +0x14e`, 2.13) `[정적]`.
3. **그래서**: 쓰는 끝이 닫혀도, 읽는 쪽은 남은 자료를 다 읽는다. 다 비운 뒤의 읽기에서야 `0xC000014B` 가 온다.
4. 에뮬레이터 지금 ("끝이면 `STATUS_END_OF_FILE`") 은 **파이프엔 틀린다** (run 문서 PE_emulator 표). 파이프의 "끝" 은 `BROKEN` (109) 이다.

#### 빈 읽기의 두 갈래

- **동기 손잡이**: 접수 사무실이 파일 객체 신호등을 기다린다 (run126).
- **비동기 손잡이**: PENDING 으로 돌아오고, 상대가 쓸 때 **쓰는 스레드 안에서** 채워 끝난다 `[정적]`.
- 같은 스레드에서 `WriteFile` 뒤 `ReadFile` 은 바로 끝난다. **자료 없이 블로킹 `ReadFile` 을 부르면 그 스레드는 영원히 기다린다** (`CreatePipe` 파이프는 완료 모드 0) `[정적]`.

#### 오류로 바로 끝나면 전표를 안 쓴다

- `0xC000014B` · `0xC00000D9` 처럼 빨강 도장으로 바로 끝난 `NtReadFile` 은 호출자 IOSB 를 **안 건드린다** `[정적]` — run126 의 "오류 + PENDING 없음" 갈래와 이번 status 를 이어 붙인 결론이다 (결과서 표기). 동적 1 (Q244) 에서 확인한다.
- flow.md 의 run126 절에도 이 한 줄이 붙었다 (3절).

비유: 한쪽이 관을 떼도 관 속 편지는 남는다. 다 꺼낸 다음에 또 손을 넣으면 "**관이 깨졌어요**" 다. "편지 끝" 이라는 말은 이 관 상담실엔 없다.

### 2.12 쓰기 — 쿼터 안이면 바로, 넘으면 기다림 `[정적]`

`NpFsdWrite` (rva `0x12540`) `+0x68` → `NpCommonWrite`.

| 상황 | status | `Information` | 기다림 | 줄 (`NpCommonWrite`) |
|---|---|---|---|---|
| **LISTENING (상대 안 붙음)** | **`0xC00000B3`** (→ 536) | 0 | 아니오 | `+0x2e5` → `+0x34d` |
| **CLOSING (상대 닫힘)** | **`0xC00000B1`** (→ 232) | 0 | 아니오 | `+0x2e9` |
| **CONNECTED + 쿼터 안** | **`0`** | **`Length` 전부** | 아니오 | `+0xd4` · `+0xda` · `+0xe8 ~ +0x103` · `+0x138` |
| CONNECTED + **쿼터 넘음 + 블로킹** | **`0x103`** — 읽는 쪽이 비우면 `{0, Length}` 로 끝남 | 끝날 때 `Length` | **예** | `+0x141` → `+0x239` |

- 쿼터 0 인 이름 있는 파이프 (`CreateNamedPipe(…, 0, 0, …)`) 는 읽기 대기가 있을 때만 바로 끝나고 아니면 기다린다 `[정적 읽기, 동작은 동적 권함]`.
- 길이 0 쓰기는 `[추측: 자세히는 안 읽음]`.
- 비유: 관 안 편지 자리가 남아 있으면 바로 "넣었어요". 꽉 차면 맞은편이 꺼낼 때까지 손에 들고 기다린다.

### 2.13 닫기 — CLEANUP 이 오면 CLOSING `[정적 + 공백]`

#### 새 말 — CLEANUP · CLOSE

- npfs 는 닫기 봉투를 두 종류로 받는다 (2.3 표):
  - **CLEANUP (0x12)** — 결과서 말로 "한 파일 객체의 마지막 핸들이 닫힐 때" 온다. 그 "마지막" 의 뜻은 아래 `[공백]`. npfs 는 여기서 **상태를 바꾸고 대기 봉투를 끝낸다** `[정적]`.
  - **CLOSE (2)** — 노드를 푼다 `[정적]`. 파일 객체가 정말 사라질 때 온다 `[추측: 일반적으로 알려진 설명]`.
- 둘 다 늘 status 0 으로 끝난다 (`NpFsdCleanup` · `NpFsdClose`) `[정적]`.
- 비유: CLEANUP = "이 관 입구 닫습니다" 신고. CLOSE = 관 입구 카드를 버림.

#### 닫힐 때 일어나는 일

| 닫히는 때 | 상태 | 기다리던 봉투 |
|---|---|---|
| CONNECTED 에서 한쪽 끝 | → **CLOSING (4)** (`NpSetClosingPipeState +0x6f`) | 닫는 쪽 대기열 전부 · 상대의 대기 읽기 → `0xC000014B`. **상대가 이미 써 둔 자료는 남긴다** |
| LISTENING 에서 서버 끝 | 그대로 | 듣기 봉투 전부 `0xC000014B` |
| CLOSING 에서 나머지 끝 | 대기열 비움 · `NpClearSecurity` | 남은 것 `0xC000014B` |
| 서버 끝 (FCB 쪽) | `NpUnlinkCcb`, 인스턴스 0 이면 `NpUnlinkFcb` — **이름이 이름표에서 사라짐** → 뒤 열기 `0xC0000034` | |

- 한쪽이 닫힌 뒤 상대: 읽기 + 자료 있음 → 읽힘, 읽기 + 빈 → `0xC000014B` (109), 쓰기 → `0xC00000B1` (232), 듣기 → `0xC00000B1` `[정적]`.
- `DisconnectNamedPipe` (`0x110004`, 서버 끝) 는 CONNECTED · CLOSING → DISCONNECTED, 양쪽 대기 봉투 `0xC00000B0` (233), 클라이언트 손잡이는 그 뒤 읽기 · 쓰기가 `0xC00000B0` `[정적]`.

#### ★ 언제 CLEANUP 이 오나 — 못 정했다 `[공백]`

단계별로:

1. **근거**: `nt!IopCloseFile +0x10a` 가 CLEANUP 봉투를 짓는 줄이고, `+0x67 CMP R9,1` 일 때 거기로 들어간다 `[정적]`.
2. **모르는 것**: `R9` 가 "**이 프로세스의** 핸들 수" 인지 "**시스템 전체의** 핸들 수" 인지 정적으로 못 정했다 `[공백]`.
3. **왜 중요하나**: 자식이 쓰는 끝을 **물려받은** 상태에서 부모가 자기 쓰는 끝을 닫으면 —
   - "프로세스 몫" 이면 부모가 닫는 순간 CLEANUP → CLOSING → 부모의 읽기는 남은 자료 뒤 `BROKEN`.
   - "시스템 전체" 면 자식이 아직 쥐고 있으니 CLEANUP 이 안 옴 → 부모의 읽기는 PENDING (기다림).
   - 이 두 갈래는 run 문서의 "자식이 살아 있는 동안 부모의 읽기가 BROKEN 인지 PENDING 인지" 를 내가 2.11 · 2.13 표에 이어 풀어 쓴 것이다 `[추측]`.
4. **아직 확인 아님** → 동적 Q244 3 · 정적 Q245 2.

### 2.14 `GetFileType` — npfs 까지 안 간다 `[정적]`

run84 에서 "`0x11` 이면 파이프" 까지는 알았다. 이번엔 **누가 `0x11` 을 답하나** 다.

1. `kernelbase!GetFileType +0x72` 가 `NtQueryVolumeInformationFile(클래스 4 = FileFsDeviceInformation, 8 B)` 를 부른다.
2. ntoskrnl 이 **직접** 답한다: `NtQueryVolumeInformationFile +0x1fe` (클래스 4) → `+0x3d6 ~ +0x3e2` (장치 종류 `0x14` 네트워크 파일 시스템이 아니면) → `+0x41f` 장치 객체 `+0x48` (`DeviceType`) · `+0x426` `+0x34` (`Characteristics`) 를 출력에 → `+0x442` · `+0x445` 전표 `{0, 8}`. 길이 8 미만이면 `0xC0000004` (`+0x50b`).
3. **봉투를 npfs 로 안 보낸다.**
4. 그 장치 객체 = `NpCreateDevice +0x60 IoCreateDevice(종류 0x11, 특성 0x20000)` (1절 "새 말 — npfs").
5. `DeviceType 0x11` → `GetFileType` 은 **3 (`FILE_TYPE_PIPE`)**. 콘솔 `0x50` → 2 는 run83 · run84 와 같다.

- `GetFileType` 은 특성을 안 읽는다. 그래서 에뮬레이터는 **장치 종류 `0x11` 만** 맞으면 된다.
- 특성의 최종값은 `0x20000` + (I/O 관리자가 더 얹는 비트 `[공백]`) → 동적 Q244 2.
- 비유: "이 손잡이 뒤가 뭐예요?" 는 관 상담실까지 안 묻고, 접수 사무실이 **상담실 문패 (장치 객체)** 를 보고 바로 답한다.

### 2.15 나머지 넷 — 짧게 `[정적]`

| 함수 | 내는 것 | 눈여겨볼 점 |
|---|---|---|
| `WaitNamedPipeW` | `NtFsControlFile(0x110018)` | **새로 연 `\DosDevices\pipe\`** 에 낸다 (`\Device\NamedPipe\` 아님). 낸 뒤 닫음 |
| `PeekNamedPipe` | `NtFsControlFile(0x11400c)`, 임시 이벤트 | `0x80000005` 도 성공으로 침 (`+0x139`) |
| `SetNamedPipeHandleState` | `NtSetInformationFile` 클래스 `0x17` | `maxcnt` · `timeout` 을 주면 클래스 `0x19` 도 — **npfs 가 안 받음** |
| `DisconnectNamedPipe` | `NtFsControlFile(0x110004)`, 임시 이벤트 | 클라이언트 손잡이면 `0xC00000AF` → 1 |

- `WaitNamedPipeW` 는 듣는 인스턴스가 있으면 0 으로 바로, 없으면 `NpAddWaiter` 로 기다림까지만 읽었다. **타임아웃 status 는 `[공백]`** (4절).
- npfs 가 받는 정보 클래스: 조회 `0x17` (8 B) · `0x18` (0x28 B) 등, 설정은 `0x17` 만 일함 (run 문서 3(d)).

### 2.16 요청 쪽 로그와 코드가 다른 곳 — 그리고 에뮬레이터에게

요청 쪽 로그 (우리 `[관찰]` 아님) 를 코드 `[정적]` 로 풀면:

| 로그에서 본 것 | 코드로 풀면 |
|---|---|
| "② ③ root 0" | 진짜는 ① · ② 의 손잡이가 간다. 에뮬레이터가 ① 에 손잡이를 안 줘서 생긴 모습 |
| "`CreateNamedPipeA` 가 0" | 에뮬레이터가 status 0 인데 핸들 칸을 안 써서, `CreateNamedPipeW +0x2b1` 이 그 칸 값 (0) 을 그대로 돌려준 것. **실패 꼴이 아니다** (실패면 `-1`) |
| "`CreateFileA` 가 −1 → `Sleep` 되풀이" | 서버 쪽이 진짜로 만들어졌다면 첫 열기가 바로 붙는다 (처음부터 LISTENING, 2.6). 지금은 서버 끝이 없어서 진짜라면 `0xC0000034` (→ 2) 일 자리 |
| "`STATUS_PIPE_CONNECTED` → 참?" | **참이 아니라 FALSE** + 535 (2.10) |

run 문서 PE_emulator 표 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, **고칠지는 사용자 결정**) 의 요점:

- **맞다**: `nt_create_named_pipe_file` 의 인자 읽기 (시간 포인터 검사는 유저 모드일 때만 — 2.2 줄 2).
- **막힘을 푸는 첫 셋**:
  1. `NtCreateNamedPipeFile` 성공 대답 — status 0 · `Information` **2** (새 · 이름 없음) / **1** (있는 이름) · 핸들 = 새 서버 끝.
  2. **빈 이름 + root 열기** — root ≠ 0 + 빈 이름 (Length 0, Buffer NULL) 이 `CreatePipe` ② ③ 의 정상. ① 의 `MaximumLength` 는 0 이라 "`MaximumLength ≥ Length`" 검사를 넣으면 틀린다.
  3. **`\??\pipe\` 이름** — 받아야 할 이름은 셋: `\??\pipe\<이름>` · `\Device\NamedPipe\` (대소문자 구분) · `\DosDevices\pipe\` (`WaitNamedPipeW`). SD 가 있으면 `stop` 하는 줄은 걸림돌이다 — `CreateNamedPipeW` 가 **늘** 기본 SD 를 달아 보내니까 (2.4).
- **파이프엔 틀림**: 읽기의 "끝이면 `END_OF_FILE`" (2.11). 쓰기는 파이프 갈래가 필요 (2.12).
- 한 줄로: **npfs 겉 동작은 프로세스 안 상태기계 (1 ~ 4) + 두 방향 바이트 대기열 + 쿼터 하나로 낼 수 있다** — 호스트 파이프 없이.
- 덧: 비동기 파이프 손잡이를 우편함에 묶을 때의 쪽지 조건은 run127 표 — I/O 관리자 규칙이라 드라이버와 무관하다 `[추측: 결과서는 파이프 + 포트 조합을 안 읽음]`.

### 2.17 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음)

- 결과서 3(d) 의 "`NtQueryVolumeInformationFile +0x1fe CMP R15D,4; JZ +0x3b6`" 에서 점프 목적지는 **`+0x3d6`** 이다. `0x3b6` 은 절대 주소 (`0x1408c93b6`) 의 끝자리, 함수 시작 `0x1408c8fe0` 과의 차이가 `0x3d6` (run 문서). 2.14 는 `+0x3d6` 으로 적었다.
- 결과서 1(c) 의 "`+0x9b/+0xa0 → +0xa2 ~ +0xbe`" 는 겹침 없음 갈래 (`+0x6f TEST RBX` · `+0x72 JZ +0x9b`) 이고, `NtWaitForSingleObject` call 은 `+0xaa` (ret `+0xb1`) 다. 기다림 뒤 음수면 `+0xb8 JS +0x78`, 아니면 `+0xba` 에서 IOSB status 를 들고 판정으로 간다. 2.10 표는 이 줄로 적었다.

### 2.18 정리 — 비유로 한 번 더

- 가게 직원 (kernelbase) 이 "**관 놓아 주세요**" 를 낸다. 신청서엔 방향이 두 번 (access · share) 적혀 있고, 명부 (SD) 도 늘 붙어 있다.
- 접수 사무실 (I/O 관리자) 은 신청서 내용은 안 보고, 봉투에 "**1번 문 (관 놓기)**" 표시만 해서 관 상담실 (npfs) 로 보낸다.
- 관 상담실은:
  - **"누구랑 나눠 쓰나" 칸** 으로 관 방향을 정하고,
  - 관을 놓자마자 **문을 열어 둔다** (LISTENING).
  - 같은 문패로 또 오면 **권한 → 가닥 수 → "첫 관만요" → 방향** 차례로만 거른다.
- 맞은편 사람이 0번 문으로 와서 "**저 관에 붙여 주세요**" 하면 바로 붙는다. 서버가 "기다릴게요" 를 안 했어도.
- 이미 붙은 뒤 "기다릴게요" 하면 **빨강 도장** "벌써 붙었어요" (`0xC00000B2`). 직원은 빨강이면 FALSE 에 적는다.
- 한쪽이 관을 떼면, 남은 편지는 꺼낼 수 있고, 다 꺼낸 뒤엔 "**관이 깨졌어요**" (`0xC000014B`).
- 문패 없는 관 (`CreatePipe`) 은 상담실 열쇠 ① 로 "문패 없이 하나" (②) 를 놓고, ② 를 내밀어 "이 관 맞은편 입구" (③) 를 받는다. 열쇠 ① 은 가게에 걸어 두고 계속 쓴다.
- "손잡이 뒤가 뭐예요?" (`GetFileType`) 는 접수 사무실이 상담실 문패 (`0x11`) 를 보고 바로 답한다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (docs 커밋 `1b5a8ac`)

**그림은 그대로다** (test.exe 는 파이프를 안 쓴다). 바뀐 곳:

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 줄 | npfs 줄 없음 | **npfs.pdb `C433ACB0BA96C41BACA83308CFF250A61` 새 줄** (run132 처음 analyze). ntkrnlmp · kernelbase · kernel32 · ntdll 줄에 run132 추가 |
| 기준 — 사진 대조 | — | "(run132) 정적 바퀴, 사진 값 대조 없음. 요청 쪽 값은 에뮬레이터 로그라 `[관찰]` 로 안 씀" 한 줄 |
| 3장 콘솔 절 (`GetFileType`) | `0x11` → 파이프까지만 | `0x11` 은 npfs 장치의 종류이고, 이 질문은 **ntoskrnl 이 장치 객체 칸으로 직접** 답한다 `[정적]` 한 줄 |
| 3장 run126 절 ("AFD 주소 목록 묻기 · IOCTL 완료") | afd · 콘솔 IOCTL 이야기 | npfs 파이프 오류 status 도 "오류 — 아무것도 안 건드림" 갈래, 빈 블로킹 읽기 · 듣기 · 쿼터 넘은 쓰기의 `0x103` 은 동기 손잡이면 `+0x3e6 IopWaitForSynchronousIo` 갈래 `[정적]` 한 줄 |
| 3장 끝 | — | **새 절 "파이프 (이름 있는 · 없는)"** (run132 `[정적]`) — 나무 + 사실 일곱 줄 (kernelbase 값 · `CreatePipe` · npfs 만들기 · 열기 · 닫기와 데이터 · CLEANUP `[정적 + 공백]` · `GetFileType`) |
| 4장 | Q241 없음 | "3 (범위 밖 — 파이프)" 줄 넷: Q241 해결됨 · 동적 Q244 · 정적 Q245 · PE_emulator 쪽 |
| 5장 | — | run132 줄 추가 (run 표 · sast 표). run131 sast 줄의 대기열 번호를 **Q242 · Q243** 으로 채움 |

- 이전 run 정정은 없다. 태그가 올라간 곳도 없다 — 이번 사실은 모두 처음 적히는 `[정적]` 이다.

### 큰 그림에서 어떻게 이어지나

- **run84 와 이어진다.** CRT 가 시작 때 표준 핸들마다 `GetFileType` 을 묻는다. 표준 출력이 파이프 (자식 프로세스로 실행된 경우 등) 면 그 답은 **3** 이고, 그 3 은 ntoskrnl 이 npfs 장치 객체 (`0x11`) 를 보고 낸다. "손잡이 뒤가 관이면 파이프" 의 **관 쪽 끝** 이 이번에 채워졌다.
- **run126 · run127 과 이어진다.** 파이프 오류 status 는 모두 run126 의 "빨강 도장은 전표 안 씀" 갈래를 탄다. 그래서 에뮬레이터는 파이프 오류 때 IOSB 를 쓰지 말아야 한다. 겹침 파이프를 우편함에 묶으면 run127 의 쪽지 조건이 그대로 적용될 것이다 `[추측]`.
- **에뮬레이터 막힘의 뿌리가 셋으로 좁혀졌다** (2.16): `NtCreateNamedPipeFile` 성공 대답 · 빈 이름 + root 열기 · `\??\pipe\` 이름. 이 셋이 풀리면 "이름 있는 파이프 20 / 20" 과 "자식 표준 출력 26 / 37" 이 다음 막힘 (읽기 · 쓰기 · 닫기 status) 으로 넘어갈 것이다 `[추측]` — 그 status 는 2.11 ~ 2.13 표에 다 있다.
- 남은 큰 갈림 하나는 **"부모가 쓰는 끝을 닫으면 CLEANUP 이 오나"** (2.13) 다. 자식 표준 출력 길에서 부모의 마지막 `ReadFile` 이 `BROKEN` 으로 끝나느냐 기다리느냐가 여기에 걸린다.

---

## 4. 아직 모르는 것

### 동적 — Q244 (전부 선택, VM 묶음 — Q222 NSI VM 세션과 같이)

- 파이프를 쓰는 작은 exe 가 필요하다 (test.exe 는 안 씀). **고르기는 사용자 결정.**
- 커널 멈춤점 (`npfs!…` · `nt!IopCloseFile`) 은 시스템 전체 파이프가 지나가니 **그 프로세스일 때만** 멈추게 조건을 붙인다.

| # | 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|---|
| 1 | 요청 원문의 다섯 status — `CreatePipe` → `WriteFile(n)` → `ReadFile` → 쓰는 끝 `CloseHandle` → `ReadFile`. 예측: TRUE · `0`/n · `0`/n · (CLEANUP 0) · **`0xC000014B` 이고 IOSB 안 써짐** | 정적 예측 + run126 규칙을 이어 붙인 결론이라 실행으로 맞춰 봐야 함 | `KERNELBASE!CreatePipe+0x210` · `+0x106` · `+0x15d`, `WriteFile+0x8d` / `+0x11a`, `ReadFile+0x8d` / `+0x12a`, `npfs!NpFsdWrite` · `NpFsdRead` · `NpFsdCleanup` 입구 |
| 2 | 장치 `Characteristics` 최종값 (`0x20000` + ?) | I/O 관리자가 더 얹는 비트는 실행 때 값 | `nt!NtQueryVolumeInformationFile+0x426`, `!devobj \Device\NamedPipe` |
| 3 | **CLEANUP 이 오는 때** (2.13) | `IopCloseFile +0x67` 의 `R9` 뜻을 정적으로 못 정함 | `nt!IopCloseFile` 입구 `r8` · `r9`, `+0x10a` 에 서나, `npfs!NpFsdCleanup` 이 오나 — 자식이 쓰는 끝을 물려받은 상태에서 부모가 닫을 때 |
| 4 | `WaitNamedPipeW` 의 기다림 · 타임아웃 status | 기다림 함수 (`NpAddWaiter` 뒤) 를 이번에 안 봄 | Q245 1 과 같이 |
| 5 | `\DosDevices\pipe\` 가 진짜 풀리나 | 이름 풀기는 실행 때 객체 관리자 몫 | `KERNELBASE!WaitNamedPipeW+0x182` 의 `r8` OA → `!obja` |
| 6 | `FO_NAMED_PIPE` (0x80) 이 `NtReadFile` / `NtWriteFile` 에서 하는 일 | ntoskrnl 쪽 갈래를 안 읽음 | 비동기 손잡이로 `ByteOffset` 없이 읽고 쓸 때 |
| 7 | 한 동기 손잡이에서 블로킹 읽기 중 다른 스레드의 쓰기 | 파일 객체 락 직렬화 쪽 줄을 안 봄 `[공백]` | (이름 있는 파이프 샘플은 파일 객체가 달라 영향 없음) |

### 정적 — Q245

1. `NpAddWaiter` · `NpCancelWaiter` · `NpTimerDispatch` · `NpCancelWaitQueueIrp` — `WaitNamedPipeW` 의 기다림 · 깨움 · 타임아웃 status (동적 4).
2. ntoskrnl `NtReadFile` / `NtWriteFile` 의 `FO_NAMED_PIPE` · `FO_SYNCHRONOUS_IO` 갈래와 `IopCloseFile` 의 `R8` / `R9` 뜻 (동적 3 · 6 · 7) — "자식이 핸들을 물려받은 파이프의 CLEANUP 시점".
3. `NpPeek` (`0x11400c`) 출력 칸 · 상태별 status, `NpTransceive` (`TransactNamedPipe` — 이번엔 범위 밖), `NpFsdFlushBuffers`.
4. `NpFindPrefix` · `NpTranslateAlias` (`LOCAL\` 별칭 — AppContainer · 사일로에서 이름이 바뀜), `NpReadParameters` · `NpReadAlias`.
5. 메시지 꼴 `NpReadDataQueue` · `NpWriteDataQueue` 정밀 읽기 (이번엔 바이트 꼴 중심) · NOWAIT 쓰기 조합 · 길이 0 쓰기.
6. kernelbase `GetNamedPipeInfo` · `GetNamedPipeHandleStateW` 의 사용자 이름 얻기 간접 호출 — 범위 밖, 이름만.

### 그 밖 `[공백]`

- npfs 루트 명부 (SD) 가 smss 가 부팅 때 `\Device\NamedPipe\` 에 다는 것인지 (run98 과 이은 `[추측]`).
- `NpTranslateAlias` 가 `LOCAL\` 밖 이름을 그대로 넘기는지 (`.c` 첫머리만 봄).
- 클라이언트 쪽 읽기에서 오프셋 필수 검사가 면제되는지 (run 문서 PE_emulator 표의 `[공백]`).

---

## 5. 더 읽기

- run 문서: [`run132-npfs-pipes.md`](run132-npfs-pipes.md) — 호출 지점 표 · 14 인자 표 · 조건별 status 표 · PE_emulator 표 전부
- 요청서: [`_workspace/sast/129_npfs-pipes.md`](../../_workspace/sast/129_npfs-pipes.md)
- 결과서: [`_workspace/sast/129_npfs-pipes_result.md`](../../_workspace/sast/129_npfs-pipes_result.md) — 질문별 답 · CCB/FCB 칸 · status 이름
- flow.md: [`flow.md`](flow.md) 3장 끝 "파이프 (이름 있는 · 없는)" 절, 3장 "콘솔 입출력" 절의 `GetFileType` 줄, "AFD 주소 목록 묻기 · IOCTL 완료" 절 (run126), 4장 "3 (범위 밖 — 파이프)" 줄, 5장 run132 줄
- 앞 해설: [run126 해설](run126-afd-addrlist-해설.md) 2.16 (IRP 완료 규칙) · [run127 해설](run127-file-completion-해설.md) (완료 포트에 묶기) · [run84 해설](run84-console-crt-해설.md) (`GetFileType` · 표준 핸들)
- 대기열: `_workspace/plan.md` Q241 (끝) · Q244 (동적) · Q245 (정적)

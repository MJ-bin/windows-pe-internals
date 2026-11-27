# run156 appinfo-runas — 해설

> 이 글은 [run156 문서](run156-appinfo-runas.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q290** + **Q276a** (`_workspace/plan.md`, 둘 다 이번에 **끝 (run156)**)
  - **Q290** — PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/374_internals-appinfo-runas.md` (튜닝 `374_tune19.md` UAC1, 탓 35 `runas_fail`). "`ShellExecuteExW` (verb `runas`) 의 승격 부탁 — 누가 어떤 RPC 로 부탁하고 무엇을 돌려받나".
  - **Q276a** — run143 이 남긴 Q276 의 ① 만 앞으로 당긴 것. windows.storage · shell32 를 처음 analyze 하고 지도를 그린다 (뿌리 `ShellExecuteExW`).
  - 사용자 결정 2026-11-27 (PE_emulator D179) — 380 묶음 차례 **⑤**. 원문이 "Q276 ① 과 같이 묶을 수 있음" 이라 해서 묶었다.
  - 결과서 판정 **분석성공** — 원문 물음 셋 (부탁하는 쪽 · 응답 뒤 · 들어주는 쪽) 모두 줄 근거로 답했다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q283** (RPC VM 묶음, run 문서 D1 ~ D8)
    - 정적 → **Q300** (결과서 "다음 요청 후보" 1 ~ 6)
- 근거:
  - run 문서 [`run156-appinfo-runas.md`](run156-appinfo-runas.md) (가장 중요)
  - 요청서 [`_workspace/sast/152_appinfo-runas.md`](../../_workspace/sast/152_appinfo-runas.md) · 결과서 [`152_appinfo-runas_result.md`](../../_workspace/sast/152_appinfo-runas_result.md)
    - **처음 analyze 셋** — shell32 (함수 34,079) · windows.storage (43,911) · appinfo (1,387). 하나씩 돌렸다.
    - kernelbase 는 있던 프로젝트에서 `SHCoCreateInstance` 하나만 뽑았다. rpcrt4 · combase · rpcss · kernel32 · ole32 는 **Ghidra 로 안 열었다** (있던 `.asm` 읽기 · `pefile` · `objdump` 만).
    - 스크립트 문제 없음.
  - flow.md 변경: docs 커밋 `1f3a320` (`git show 1f3a320 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다. 그리고 부탁을 들어주는 쪽은 다른 가게다.**
   - test.exe 는 `ShellExecute*` 를 안 부른다 — `sample/` 에 `ShellExecute` · `runas` · `shell32` 글자가 없다 `[소스]`.
   - 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) 에 shell32 · windows.storage 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run143 · run155 와 같은 줄).
   - appinfo 는 **다른 프로세스** 다 (svchost 안 서비스).
   - 그래서 이번 이야기는 "**`main` 뒤에 관리자 실행을 부탁하는 프로그램이라면**" 이다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - `r19 00858b3f74b5 seq 1561484` · "RPCSS bind 거절 `0x6b5` ×3" · "`ept_map` rpc_status `0x6f7`" · `rax 0` 같은 것은 PE_emulator 로그다. 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다.
   - `epm.cpp:231` · `tokens.hpp:21` 같은 줄 번호도 PE_emulator 코드 이야기다.
3. **오프셋 규칙은 앞 해설들과 같다.**
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 · 표에서 "`+0x4a SHTestTokenMembership`" 처럼 함수 이름 **앞** 숫자 = **call 자리**. "`+0x3a CMP …`" 처럼 명령과 같이 적힌 숫자 = 그 함수 안 줄.
   - 그래서 같은 호출이 나무에선 `+0x4f`, 본문에선 `+0x4a` 로 나온다. 둘 다 맞다.
   - `.rdata` 구조체 자리 (`0x702b90` 등) 는 **모듈 rva** 다.
   - 콜스택으로 본 적은 없다 (정적만).
4. **비유 이름이 run 문서와 다르다.**
   - run 문서 "결과" 비유: appinfo = "**관리실**", EPM = "**안내 데스크**" (run 문서는 이 비유를 `[추측: 비유]` 로 둔다).
   - 그런데 앞 해설에서 이미 **RPCSS = COM 관리실** (run136 · run149), **epmapper = 114 안내** (run129 · run145) 다.
   - 그래서 이 글은 appinfo = **승격 사무실**, EPM = **114** 로 부른다. 그림만 다르고 사실은 같다.
5. **숫자 `0x60001` 이 run154 와 겹친다.**
   - run154 에서 `0x60001` 은 kernelbase 가 커널에 넘기는 **속성 칸 번호** 이고, 값은 디버그 객체 핸들이었다 `[정적]` (run154).
   - 이번 `0x60001` 은 appinfo 가 `UpdateProcThreadAttribute` 에 넣는 번호이고, run 문서는 `ProcThreadAttributeExtendedFlags` 로 읽었다 (값 4 B, 비트 뜻 `[추측]`).
   - 숫자만 같고 **넣는 목록이 다르다**. 그래서 다른 뜻일 수 있다 `[추측]`. run 문서는 이 둘을 잇지 않았다.
6. **"원문이 인용한 PE_emulator 파일" 과 "지금 PE_emulator 파일" 이 다르다.**
   - 결과서는 원문 인용만 보고 "PE_emulator 쪽" 을 썼다.
   - flow-writer 가 지금 `epm.cpp` · `tokens.hpp` 를 직접 읽어 보니 **두 군데가 달랐다.** 2.17 에서 천천히 짚는다. 이번 글에서 가장 중요한 곳 중 하나다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- 멈춘 채 만들기 · `CREATE_SUSPENDED` = 첫 직원 목에 건 **"대기" 팻말** → [run14 해설](../02_sprint/run14-create-process-internal-해설.md) 2.4 · 2.8 "새 말: `dwCreationFlags` 와 `CREATE_SUSPENDED`"
- IAT = 벽 **콘센트** → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.7
- 썽크 = 받은 부탁을 그대로 넘기는 **얇은 다리** → [run136 해설](run136-winsta-name-해설.md) "새 말 — 썽크"
- 간접 호출 = 번호를 보고 이어 주는 **교환원**, 가상표 = 교환원의 **내선표** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md) · [run141 해설](run141-async-bind-해설.md) "새 말 — 가상표"
- 가상 함수 = "담당자에게 전화" 라고만 적힌 메모 → [run143 해설](run143-mountdev-name-해설.md) "새 말 — 가상 함수 (간접 호출)"
- HRESULT · 바꾸기 규칙 (양수 `e` → `0x80070000 | (e & 0xffff)`) → [run89 해설](run89-dotnet-start-해설.md) "새 말 — HRESULT" · [run136 해설](run136-winsta-name-해설.md) 2.6
- 토큰 = **신분증**, SID = 신분증에 적힌 **신분 번호**, 요구 서버 SID = 연락 카드의 "**이 사람과만 통화**" 칸, 가장 = 남의 신분증을 **빌려 들기** → [run141 해설](run141-async-bind-해설.md) "새 말 — 요구 서버 SID" · "새 말 — 보안 문맥 · 가장"
- COM · combase = 가게에 들인 **COM 담당 건물**, RPCSS = 동네 **COM 관리실** → [run131 해설](run131-user32-regclass-해설.md) · [run136 해설](run136-winsta-name-해설.md) · [run149 해설](run149-com-rpcss-해설.md) "새 말 — COM · RPCSS, 처음 읽는 사람을 위해 다시"
- RPC 런타임 (rpcrt4) = 가게 안 **전화 부서** → [run99 해설](run99-rpc-caller-해설.md)
- **run129 에서 푼 것** → [run129 해설](run129-winnsi-rpc-해설.md)
  - 바인딩 = **연락 카드**, 끝점 = 상대 사무실 **직통 번호**, `ncalrpc` = "같은 건물 **내선**" → 2.4
  - 인터페이스 = 사무실 **이름표** (UUID + 판 번호), 프로시저 번호 = **메뉴 번호**, 비동기 RPC ("끝나면 신호등 켜 주세요") → 2.5
  - epmapper = **114 안내**, `ept_map` = 114 에 거는 "번호 알려 주세요" 질문 → 2.7
- **run133 에서 푼 것** → [run133 해설](run133-lrpc-wire-해설.md)
  - bind = **첫인사**, 전송 문법 (NDR 2.0 · NDR64) = **말투** → 2.5
  - 동적 끝점 (`LRPC-` + 무작위 18자) → 2.13
- **run145 에서 푼 것** → [run145 해설](run145-epm-endpoint-해설.md)
  - **탑** (4층 블록 탑), 114 의 **장부** (전화번호부), **객체 UUID · nil**, 고르기 규칙, 없을 때 답 `0x16c9a0d6` → 앞머리 "새 말 — 탑" · "새 말 — 장부" · 2.4 · 2.8

이번에 처음 나오는 말 — **verb (실행 방법 이름)**, **지연 로드**, **UAC 승격**, **연결된 토큰 (Limited · Full · Linked)**, **켜진 그룹 · deny-only (거부 전용) 그룹**, **consent (확인 창)**, **부모 지정 속성**, **핸들 복제**, **`hInstApp`**, **SCM** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**, 토큰 = **신분증**.
- 바인딩 = **연락 카드**, 끝점 = **직통 번호**, `ncalrpc` = **내선**, 인터페이스 = 사무실 **이름표**, 프로시저 번호 = **메뉴 번호**, bind = **첫인사**.
- epmapper = **114 안내**. 114 는 장부 (**전화번호부**) 를 들고 "그 사무실은 몇 번이에요" 를 알려 준다 (run145).
- 멈춘 채 만들기 = 새 가게의 첫 직원 목에 **"대기" 팻말** 을 걸어 두는 것 (run14).
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 앞 해설 말로 바꾼 것 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - 관리자 힘이 필요한 일은 가게 주인이 **직접 못 한다.** 동네 **승격 사무실** (appinfo, 다른 가게 안) 에 **신청서** 를 낸다.
  - 신청서에는 "무엇을 (실행 파일 · 명령줄) · 어느 폴더에서 · 어느 창에서" 를 적는다.
  - 승격 사무실 직통 번호는 연락 카드에 **안 적혀 있다.** 그래서 먼저 **114 에 묻는다.** 원문이 본 `0xe1af8308` 은 이 **114 의 번호** 다.
  - 114 에 물을 때 카드에 "**이 사람과만 통화: SYSTEM**" 칸 (`S-1-5-18`) 을 같이 적어 낸다.
  - 승격 사무실은 "정말 하실 거예요?" 창 (consent) 을 띄운다. 승인이 나면 **관리자 신분증** 으로 새 가게를 짓되, 출생 신고서의 **부모 칸에 신청자 이름** 을 적는다.
  - 새 가게의 첫 직원에겐 **대기 팻말** 을 걸어 둔 채, 새 가게 **손잡이 두 개를 신청자 주머니에 복사해 넣어** 준다.
  - 팻말을 떼는 (깨우는) 건 **신청자 몫** 이다.
  - 이미 관리자 신분증을 **꺼내 든** 사람 (켜진 Administrators 그룹) 이면 신청서 없이 그냥 문을 연다 (`CreateProcessW`).

### 한 줄로 먼저 — 무엇을 얻었나

> **shell32 의 `ShellExecuteExW` 는 얇은 다리 한 줄이다. 승격 부탁은 windows.storage 안에서 14 걸음을 지나 `AicLaunchAdminProcess +0x636 Ndr64AsyncClientCall` 에서 나간다.**
> **전화 상대는 인터페이스 `201ef99a-7fa0-444c-9399-19ba84f12a1a` v1.0, 메뉴 0 = appinfo `RAiLaunchAdminProcess`. 비동기 · `ncalrpc` · 직통 번호 없음 → rpcrt4 가 114 에 묻는다. 원문의 `0xe1af8308` 은 114 자신이고, 그 질문 속 SID 는 클라이언트가 "서버는 SYSTEM 이어야 한다" 로 넣은 `S-1-5-18` 이다.**
> **승격 쪽으로 가려면 문이 둘 — verb 가 "runas" 이고, 내 신분증에 Administrators 가 켜져 있지 않아야 한다 (`SHTestTokenMembership(NULL, 0x220)` 거짓). 켜져 있으면 RPC 없이 그냥 `CreateProcessW`.**
> **승격 사무실은 승인 뒤 받은 토큰으로, 부모 = 신청자 (`0x20000`), `CREATE_SUSPENDED` 가 든 플래그 `0x04080414 | 0x80004` 로 `CreateProcessAsUserW` 한다. 손잡이 둘은 신청자 핸들 표로 복제해 돌려준다. 깨우는 건 클라이언트 (`CallCreateProcess +0xdf7 ResumeThread`).**
> **실패하면 `ShellExecuteExW` 는 FALSE. 그런데 `SEE_MASK_FLAG_NO_UI` 가 꺼져 있으면 `GetLastError` 는 원래 오류가 아니라 `0x4c7` 이 된다. `hInstApp` 은 `0x4c7` · `0x6f7` · `0x5` 모두 5.**
> **앞의 RPCSS 연결 셋은 이 길에 꼭 필요하지 않다.**

- 모두 `[정적]` (UUID · 가상표 · 글자 값은 `[정적: PE]`).
- 단 몇 가지는 `[추측]` · `[공백]` 이다: RPCSS bind 셋이 `CoCreateInstance(ImmersiveShell)` 에서 났다는 **짝**, 승격 토큰의 **원천** (`LUAGetElevatedToken` 의 `TokenLinkedToken` 일 것), `hProcess` 칸을 채우는 곳, `this+0x1f8` 비트의 출처.

원문 물음에 대한 답 (run 문서 "요약" 기준):

| 원문 물음 | 답 | 이 글 |
|---|---|---|
| 1 부탁하는 쪽 — 사슬 · UUID · 프로시저 · protseq · 끝점 · 왜 SID · 요청 / 응답 꼴 | windows.storage 14 걸음 → `AicLaunchAdminProcess +0x636`. `201ef99a` v1.0 메뉴 0, 비동기, `ncalrpc`, 끝점 없음 → `ept_map`. SID = QOS V3 `Sid` = `S-1-5-18`. `0xe1af8308` = EPM `[정적]` | 2.3 ~ 2.9 |
| 2 응답 뒤 — 검사 · 깨우기 · `hProcess` · 오류마다 반환 · RPCSS 셋 | 핸들 검사 없음, 클라이언트가 `ResumeThread`. 오류 ≠ 0 이면 FALSE, 상자 조건이면 `0x4c7`, `hInstApp` 5. RPCSS 셋은 **꼭 필요 없음** `[정적]` (짝 `[추측]`) | 2.12 ~ 2.14 |
| 3 들어주는 쪽 — 토큰 · 부모 · 플래그 · 누가 깨우나 | 승인 뒤 토큰 (칸을 짓는 줄은 appinfo 에 없음), 부모 = 호출자 `0x20000`, `CreateProcessAsUserW(…, FALSE, 플래그 \| 0x80004, …)`, 서버는 안 깨움, 핸들 복제 `[정적]` | 2.10 · 2.11 |
| Q276a — 지도 | windows.storage 함수 424 (깊이 10) · shell32 노드 1. 가상 호출에서 끊겨 사슬은 `_refs/` 로 거꾸로 이음 `[정적]` | 2.16 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

test.exe 흐름 밖, **옆길** 이다. run143 (볼륨 이름 묻기) 의 windows.storage 사슬이 이 길 **가운데** 를 지나고, 끝은 run145 (114 가 답을 고르는 법) 로 이어진다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 ShellExecute 를 안 쓴다 [소스]  │ 관리자 실행을 부탁하는 프로그램이라면:
                                                        └─ shell32!ShellExecuteExW("runas")  (얇은 다리)
                                                             └─ windows.storage 14 걸음              ← 이번
                                                                  ├─ 작업 폴더 고치기 (run143 볼륨 길)
                                                                  ├─ 두 문: verb · 내 신분증          ← 이번
                                                                  └─ AicLaunchAdminProcess (RPC)      ← 이번
      ── rpcrt4 → 114 (RpcEpMap, run145) ─────────────────────┼──────────────────────
      ── 다른 가게: svchost (AppInfo 서비스) ──────────────────┼──────────────────────
                                                                  └─ appinfo!RAiLaunchAdminProcess    ← 이번
                                                                       ├─ consent.exe (확인 창 — 범위 밖, 이름만)
                                                                       └─ AiLaunchProcess → CreateProcessAsUserW
```

- flow.md 에서는 **3장 끝 ("nsi 걸린 호출 취소 · 예약 메시지" 절, run155 뒤)** 에 새 절 "**`runas` 승격 부탁**" 으로 들어갔다. 새 절 첫 말이 run143 절 (가운데) 과 run145 절 (끝) 을 가리킨다.
- 프로그램이 `main` 뒤에 부르는 일이라 그림엔 안 넣었다.

### 왜 이 질문이 지금 왔나

PE_emulator 쪽에서 막혔다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님).

- r19 의 한 샘플이 `ShellExecuteExW(lpVerb "runas")` 를 불렀다 (`seq 1561484`).
- 그 뒤 로그 차례:
  1. RPCSS `0xe60c73e6` bind 거절 `0x6b5` ×3
  2. epmapper bind
  3. `ept_map` — iface `0xe1af8308` proc 8 · reply 2 · rpc_status `0x6f7`
  4. `rax 0` (실패)
- 에뮬레이터 쪽은 "**이 `0xe1af8308` 이 승격 인터페이스인가? 왜 SID 를 넣나? 승인 뒤 자식은 어떻게 짓나?**" 를 몰라서 다음 걸음을 못 정했다.

### 무엇이 궁금했나

원문 물음 셋 (요청서 "알고 싶은 것"):

1. **부탁하는 쪽**: `ShellExecuteExW` (`runas`) → RPC 를 거는 함수 사슬. UUID · 버전 · 프로시저 · protseq · 끝점 (왜 SID 를 넣나), 요청 · 응답 꼴. 원문의 `0xe1af8308` 이 이것인가.
2. **응답 뒤**: 클라이언트 검사 · 깨우기 · `hProcess` 조건, 오류 값마다 반환과 `GetLastError` (`0x4c7` · `0x6f7` · `0x5`). RPCSS 연결 셋이 꼭 필요한가.
3. **들어주는 쪽 (appinfo)**: 승인 뒤 자식을 만들 때의 토큰 · 부모 지정 방식 · 플래그 (멈춘 채 만들고 누가 깨우나).

그리고 Q276a — windows.storage · shell32 지도.

### 바퀴 전에 알던 것

- `[정적]` run143: windows.storage `CInvokeCreateProcessVerb::Execute` (rva `0xe1de0`) → `Launch` → `_TryCreateProcess` → `_PrepareAndCallCreateProcess +0xdf _FixupWorkingDirectory` 사슬.
- `[공백]` run143: 그런데 **`ShellExecute*` → `Execute` 가 이어지지 않았다.** `Execute` 가 가상 함수라서 직접 호출로 못 따라갔다 ([run143 해설](run143-mountdev-name-해설.md) "새 말 — 가상 함수").
- `[정적]` run145: 114 의 `ept_map_auth_async` = EPM 프로시저 8, 고르기 규칙, 장부가 비면 `0x16c9a0d6`, SID 로 SD 짓기 (5절).
- `[정적]` run149 · run151: COM 의 첫 사용이 RPCSS 에 bind 넷 + `Connect` 를 건다.
- `[정적]` run34: kernelbase `CreateProcessInternalW` 의 생김새 (이번엔 `+0x47ba BaseCheckElevation` 한 줄만 다시 봄).
- shell32 · windows.storage · appinfo 는 **한 번도 Ghidra 로 연 적이 없었다** (run143 땐 PDB 만 받았다).

---

## 2. 찾은 것

### 2.1 한눈에 — 부탁 사슬 나무 `[정적]`

run 문서 "호출 관계" 를 줄인 것이다. 오프셋은 **ret 자리**.

```
shell32!ShellExecuteExW  JMP [IAT] ⇒ (지연 로드) windows.storage
windows.storage!ShellExecuteExW
 └─ +0xc1 ShellExecuteNormal
     ├─ +0x5a ExecuteNormal +0x26a _DoExecute +0xcf _ExecuteAssoc
     │   └─ +0x17a ⇢ [+0x48] CBindAndInvokeStaticVerb::Execute
     │       └─ +0x194 TryCreateProcessDdeHandler +0xc5 InitAndCallExecute
     │           └─ +0x219 ⇢ [+0x48] CInvokeCreateProcessVerb::Execute
     │               ├─ +0x4f  SetElevationExecMethodIfNeeded   (verb "runas" → [this+0x1a8] = 2)
     │               └─ +0x5d  Launch +0x112 _TryCreateProcess +0x41 _PrepareAndCallCreateProcess
     │                   └─ +0x152 CallCreateProcess
     │                       ├─ +0x4f   SHTestTokenMembership(NULL, 0x220)   참 → CreateProcessW (RPC 없음)
     │                       ├─ +0x937  CoCreateInstance(ImmersiveShell)   실패 무시
     │                       ├─ +0xc09  AicLaunchAdminProcess
     │                       │           ├─ +0x1c7  AicpCreateBindingHandleEx   ncalrpc · 끝점 없음 · Sid S-1-5-18
     │                       │           └─ +0x63d  Ndr64AsyncClientCall   201ef99a v1.0 메뉴 0
     │                       │                ⇒ (rpcrt4) ept_map_auth_async   [run145]
     │                       │                ⇒ (svchost) appinfo!RAiLaunchAdminProcess
     │                       │                     ├─ +0xe38  AiCheckLUA → consent.exe   0x4c7 · 0x2e4
     │                       │                     └─ +0x109b AiLaunchProcess
     │                       │                          ├─ +0x10a9  CreateProcessAsUserW(승격 토큰, 0x04080414 | 0x80004)
     │                       │                          └─ +0x14a0 · +0x14f6  NtDuplicateObject → 호출자 핸들 표
     │                       ├─ +0xc9a  PcaMonitorProcess2   반환 안 씀
     │                       └─ +0xdfe  ResumeThread(hThread)
     └─ +0x65 Finalize   (상자 띄웠으면 오류 → 0x4c7) → ShellExecuteExW +0x43 SetLastError
```

- `⇢` = 가상 호출 (칸 `[+0x48]` 의 대상은 PE 가상표로 확인), `⇒` = 다른 모듈 (지연 로드) 이나 다른 프로세스.
- 콜스택으로 본 적은 없다.

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `├─ +0x4f   SHTestTokenMembership(NULL, 0x220)   참 → CreateProcessW (RPC 없음)`

- **누가**: windows.storage `CallCreateProcess` (`0x17eb78`).
- **어디서**: call 자리 `+0x4a`, 돌아올 자리 `+0x4f`. 바로 앞 `+0x3a CMP [RCX+0x1a8],2` 로 "방법이 2 (승격) 인가" 를 먼저 본다.
- **누구를**: 같은 모듈 `SHTestTokenMembership`. 인자 `NULL` (= 지금 스레드의 신분증) 과 `0x220`.
- **무엇을 위해**: "내 신분증에 **Administrators (`S-1-5-32-544`) 가 켜져 있나**" 를 묻는다. 참이면 `+0x53 XOR R14D,R14D` 로 방법을 0 으로 낮추고 **RPC 없이** `CreateProcessW` 로 간다.
- 왜 이게 중요한지는 2.5 에서 천천히.

#### 줄 ②: `├─ +0x63d  Ndr64AsyncClientCall   201ef99a v1.0 메뉴 0`

- **누가**: windows.storage `AicLaunchAdminProcess` (`0x331204`).
- **어디서**: call 자리 `+0x636`, 돌아올 자리 `+0x63d`. 바로 앞 `+0x62f LEA RCX,[0x1806c4920]` (프록시 정보) · `+0x62d XOR EDX,EDX` (메뉴 번호 0) · `+0x5bb OR [RSP+0x68],-1` (시간 제한 `0xffffffff`).
- **누구를**: rpcrt4 `Ndr64AsyncClientCall` — NDR64 말투로 **비동기** RPC 를 보내는 함수.
- **무엇을 위해**: 승격 사무실 (appinfo) 의 메뉴 0 `RAiLaunchAdminProcess` 에 **신청서** 를 낸다.
- 이 줄이 "승격 부탁이 가게 밖으로 나가는 자리" 다. 원문 물음 1 의 답이 이 한 줄에 모인다.

### 2.3 shell32 는 얇은 다리 한 줄 — 지연 로드 `[정적]`

#### 새 말 — 지연 로드

- **지연 로드 (delay load)** = 처음 부를 때가 되어서야 그 DLL 을 올리고 주소를 채우는 import 방식 `[추측: 일반적으로 알려진 설명]`.
- 비유: 벽 콘센트 (IAT) 에 처음엔 "**처음 쓰면 그때 전선 꽂아 주는 임시 플러그**" 가 꽂혀 있다. 한 번 쓰면 진짜 전선으로 바뀐다.

#### 무엇을 봤나

- shell32 `ShellExecuteExW` (`0x2013f0`) 는 **6바이트, `JMP [IAT]` 한 줄** 이다 (`JMP [0x18077a3e0]`).
- 그 슬롯은 처음엔 `DelayLoad_ShellExecuteExW` 를 가리킨다. 그게 `+0x7` 에서 `__tailMerge_windows_storage_dll` 로 꼬리 점프해 **`Windows.Storage.dll` 의 `ShellExecuteExW`** 를 채운다.
- PE 지연 가져오기 표에도 `ShellExecuteExW` · `ShellExecuteW` · `ShellExecuteExA` … 가 `Windows.Storage.dll` 아래에 있다 (결과서 `pefile`).
- 그래서 **shell32 쪽 일은 여기서 끝.** shell32 지도도 노드 1 · 간선 0 이다.
- 비유: shell32 창구에 부탁을 내면, 직원은 "그건 옆 건물 (windows.storage) 일이에요" 하고 쪽지를 그대로 넘긴다.

### 2.4 windows.storage 안 14 걸음 — 교환원 두 번 `[정적]`

run 문서 1-2 표를 줄인 것이다. 넘어가는 줄은 call 자리.

| # | 함수 (rva) | 넘어가는 줄 | 한 일 (대표만) |
|---|---|---|---|
| 1 | `ShellExecuteExW` (`0x38680`) | `+0xbc` | `cbSize ≠ 0x70` 이면 `hInstApp=5` · `SetLastError(5)` · FALSE |
| 2 | `ShellExecuteNormal` (`0x353d08`) | `+0x55` | `new CShellExecute (0x250)`, 끝 `Finalize` 반환 = 오류 값 |
| 3 | `CShellExecute::ExecuteNormal` (`0x17b518`) | `+0x265` | `+0x140 _Init` (verb → `this+0x60`), `+0x252 SHCoInitialize` |
| 4 | `_DoExecute` (`0x5bf5ac`) | `+0xca` | 연관 배열 만들기, 끝에 오류 보고 · 상자 |
| 5 | `_ExecuteAssoc` (`0x241298`) | **`+0x175` ⇢ `[+0x48]`** | `+0xa7` ⇢ `QueryObject(0x2200010, verb, IID_IExecuteCommand)` |
| 6 | `_GetExecute` → kernelbase `SHCoCreateInstance` | `+0x4a` · `+0x3f` | `CLSID_ExecuteAssociation` 을 자기 `DllGetClassObject` 로 만듦 |
| 7 | `CBindAndInvokeStaticVerb::Execute` (`0x3212d0`) | `+0x18f` | 앞의 처리기들 (DelegateExecute · DropTarget) 이 "내 일 아님" 이어야 함 `[공백: VM 레지스트리]` |
| 8 | `TryCreateProcessDdeHandler` → `InitAndCallExecute` | `+0x3b` 객체 · `+0xc0` → **`+0x214` ⇢ `[+0x48]`** | `new CInvokeCreateProcessVerb (0x348 B)` |
| 9 | **`CInvokeCreateProcessVerb::Execute`** (`0xe1de0`) | `+0x4a` · `+0x58` | verb 가 "runas" 면 `this+0x1a8 = 2` (2.5) |
| 10 | `Launch` (`0xe102c`) | `+0x10d` | 명령 템플릿 |
| 11 | `_TryCreateProcess` (`0x17d118`) | `+0x3c` | 정책 · UNC 검사 |
| 12 | `_PrepareAndCallCreateProcess` (`0x17d38c`) | `+0x14d` | 플래그 기본값 `0x4000410`, `+0xdf _FixupWorkingDirectory` (run143 볼륨 길) |
| 13 | `CallCreateProcess` (`0x17eb78`) | `+0xc04` | 갈림길 (2.5 · 2.6) |
| 14 | **`AicLaunchAdminProcess`** (`0x331204`) | **`+0x636`** | RPC (2.7) |

#### 새 말 — verb (실행 방법 이름)

- **verb** = 탐색기 오른쪽 클릭 메뉴에 있는 "열기" · "관리자 권한으로 실행" 같은 **실행 방법의 이름** `[추측: 일반적으로 알려진 설명]`. `runas` 는 "관리자로 실행" 쪽 이름이다.
- 비유: 같은 물건 (파일) 을 두고 "그냥 열어 주세요" 와 "관리자가 열어 주세요" 는 **주문서 종류** 가 다르다.

#### 교환원 두 번 — run143 의 `[공백]` 이 닫힌 곳

- 5 → 7, 8 → 9 는 **가상 호출** 이다. 코드 줄에는 "내선표의 `+0x48` 번째 칸" 만 있다.
- 그래서 run143 은 "`ShellExecute*` → `Execute` 가 안 이어진다 `[공백]`" 으로 뒀다.
- 이번엔 **내선표를 직접 읽었다** (flow-writer, `pefile` + PDB 공개 이름):
  - `CBindAndInvokeStaticVerb` 의 `IExecuteCommand` 가상표 (rva `0x6a1fd0`) `+0x48` = `0x3212d0` = 7 번 함수.
  - `CInvokeCreateProcessVerb` 의 같은 가상표 (rva `0x6a2068`) `+0x48` = `0xe1de0` = 9 번 함수.
- 그런데 내선표만으로는 부족하다. **그 자리에 어느 객체가 와 있나** 도 알아야 한다.
  - 8 → 9 는 같은 함수 `+0x3b` 에서 `CInvokeCreateProcessVerb` 를 **바로 만든다.** 그래서 깔끔하다.
  - 5 → 7 은 `CExecuteAssociation_CreateInstance +0x66` 이 `CBindAndInvokeStaticVerb` 생성자를 부르는 줄로 이었다. 다만 `IAssociationArray::QueryObject` 가 그 객체를 **돌려주는 줄 자체** 는 `[공백]` 이다 (결과서).
- 비유: 교환원 메모에 "담당자 내선 0x48 번" 이라고만 있었는데, 이번엔 **그날 명단 (가상표)** 과 **그 자리에 앉힌 사람 (만드는 줄)** 을 둘 다 찾아 맞춰 봤다.
- 그래서 run143 문서 맨 위에 "정적으로 닫힘" 고쳐 읽기 한 줄이 붙었다. "실제로 불리나" (Q275 5) 는 그대로 동적이다.

#### 지도로는 왜 못 찾았나

- 지도 (`_map/`) 는 **직접 `call` 만** 따라간다. 그래서 `_ExecuteAssoc` (깊이 4) 까지만 닿고 가상 호출에서 끊긴다.
- `AicLaunchAdminProcess` 는 지도에 **없다.** 사슬은 `_refs/` 로 **거꾸로** 이었다 (`AicLaunchAdminProcess` ← `CallCreateProcess` ← … ← `Launch` ← `Execute`) — 2.16.

### 2.5 ★ 승격으로 갈리는 두 문 `[정적]`

여기가 이번 글에서 가장 헷갈리기 쉬운 곳이다. 천천히 간다.

#### 새 말 — UAC 승격

- **UAC 승격** = 관리자 계정도 평소엔 **힘을 뺀 신분증** 으로 일하다가, 필요할 때만 확인 창을 거쳐 **힘 있는 신분증** 으로 프로그램을 띄우는 Windows 장치 `[추측: 일반적으로 알려진 설명]`.
- 비유: 가게 주인 지갑에 신분증이 **두 장** 있다. 평소엔 "일반 직원" 카드를 쓰고, "관리자" 카드는 서랍에 넣어 둔다. 서랍을 열려면 "정말요?" 확인을 거쳐야 한다.

#### 새 말 — 연결된 토큰 (Limited · Full · Linked)

- 위 "두 장" 이 토큰으로는 **짝지은 두 토큰** 이다 `[추측: 일반적으로 알려진 설명]`.
  - **Limited** (힘 뺀 쪽) — 평소 쓰는 신분증.
  - **Full** (힘 있는 쪽) — 승격된 신분증.
  - **Linked** = 한쪽이 "내 짝은 저 토큰" 하고 가리키는 칸 (`TokenLinkedToken`).
  - 짝이 없는 보통 토큰은 **Default** 다.
- 이번 근거에서 이 말이 나오는 곳 `[정적]`: appinfo `LUAGetElevatedToken` (`0x4d9b4`) 이 `TokenElevationType` 을 묻고
  - **Limited (3)** 면 `TokenLinkedToken` 을 돌려준다.
  - **Full (2)** 이거나, Default (1) 인데 `TokenElevation ≠ 0` 이면 자기 복사를 돌려준다.
  - Default 인데 승격 아니면 **토큰 없음**.
- 비유: 일반 직원 카드 뒷면에 "**관리자 카드는 서랍 3번 칸**" 이라고 적혀 있는 것. 그게 Linked 다.

#### 새 말 — 켜진 그룹 · deny-only (거부 전용) 그룹

- 신분증에는 "나는 이런 모임 (그룹) 소속" 줄이 여러 개 있다. 줄마다 **켜짐 / 꺼짐 / 거부 전용** 같은 표시가 붙을 수 있다 `[추측: 일반적으로 알려진 설명]`.
- **deny-only (거부 전용)** = 그 그룹 줄이 적혀는 있지만, **문을 여는 데는 못 쓰고** "그 그룹은 들어오지 마세요" 라고 적힌 문에서만 걸리는 줄 `[추측: 일반적으로 알려진 설명]`.
- 비유: 일반 직원 카드에도 "관리자" 줄이 인쇄돼 있다. 그런데 그 위에 **"확인용 — 출입엔 못 씀" 도장** 이 찍혀 있다.

#### 문 1 — verb 가 "runas" 인가 (9 번 걸음)

1. `CInvokeCreateProcessVerb::Execute +0x4a` 가 `SetElevationExecMethodIfNeeded` (`0xe2128`) 를 부른다.
2. 그 안 `+0x19 MOV EDX,0x2020011` → `+0x2a` ⇢ `QueryExists` (`CAssocShellVerbElement` 가상표 `+0x30` = `0x210150`, flow-writer 확인).
3. `QueryExists` 는 먼저 레지스트리 값 **`Elevate`** 를 묻는다 (`0x2020011` → `"Elevate"`).
4. 없으면 `+0x42 LEA RDX,L"runas"` → `+0x49 StrCmpICW(verb, L"runas")` — **글자 비교** (대소문자 무시).
5. 참이면 `SetElevationExecMethodIfNeeded +0x7e` 가 `[this+0x1a8] = 2` — **방법 2 = 승격**.

- 방법 칸 `this+0x1a8` 의 값: **0 `CreateProcessW` · 1 `CreateProcessAsUserW` · 2 승격**.
- 비유: 주문서 종류 칸에 "관리자로" 라고 적혀 있으면 **"승격 신청" 상자** 에 넣는다.

#### 문 2 — 내 신분증에 Administrators 가 켜져 있지 않은가 (13 번 걸음)

1. `CallCreateProcess +0x3a CMP [RCX+0x1a8],2` — 방법이 2 인가.
2. 2 면 `+0x4a SHTestTokenMembership(NULL, 0x220)`.
   - 그 안: `+0x6a AllocateAndInitializeSid` 로 `S-1-5-32-544` (BUILTIN\Administrators) 를 짓고, `+0xbc CheckTokenMembership(NULL, …)` 로 묻는다.
3. **참이면** `+0x53 XOR R14D,R14D` — **방법을 0 으로 낮춘다.** → `+0x1f0 CreateProcessW`. RPC 없음.
4. **거짓이면** 방법 2 그대로 → `+0x136 TEST R14D,R14D` (0 아님) → `+0x13b CMP R14D,1` (1 아님) → **`+0x13f JNZ +0x210`** — 두 `CreateProcess*` 를 다 건너뛰고 승격 쪽으로 간다.

#### 왜 "거짓이어야" 승격 쪽인가 — 단계로

- 근거: `SHTestTokenMembership` 이 참이면 코드는 방법을 0 으로 낮춘다 `[정적]`.
- 그래서: "이미 관리자 힘이 켜진 신분증이면, 굳이 승격 사무실에 신청할 필요 없이 **그냥 직접 연다**" 로 읽힌다. 이미 승격된 창에서 `runas` 를 해도 확인 창이 안 뜨는 것과 같은 모양이다 `[추측]`.
- 그럼 "평소 UAC 관리자" 는 왜 거짓인가?
  - run 문서: "그룹이 **꺼져 있거나 거부 전용 (deny-only)** 이라 거짓이어야 승격 쪽으로 간다".
  - `CheckTokenMembership` 이 **켜진 그룹만 센다** 는 것은 `[추측: SDK 설명, 안 읽음]`.
  - 즉 힘 뺀 신분증 (Limited) 의 Administrators 줄이 "확인용 — 출입엔 못 씀" 도장 상태라면 거짓이 나와 RPC 길로 간다 `[추측: VM whoami 로 확인할 것]`.
- 아직 확인 아님: 진짜 VM 의 승격 전 신분증에서 Administrators 가 정말 "Group used for deny only" 인지는 `whoami /all` 로 떠 와야 한다 (4절, D4 와 같은 뜻).

### 2.6 다른 갈래 둘 — 암묵 승격 · 관리자 앱 확인 `[정적]`

#### kernelbase 가 끼는 곳은 runas 가 아니다

- 방법 0 (`CreateProcessW`) 이 **`0x800702e4`** (`ERROR_ELEVATION_REQUIRED`) 로 실패해도 같은 승격 쪽으로 온다.
- 그 `0x2e4` 는 kernelbase `CreateProcessInternalW +0x47ba BaseCheckElevation` 이 `0xc000042c` 를 낼 때 생긴다.
- 즉 kernelbase 가 이 사슬에 끼는 곳은 **"그냥 열었더니 관리자가 필요하대요" 갈래 (암묵 승격)** 이다. runas 는 처음부터 문 1 로 승격 상자에 들어가서 이 갈래를 안 거친다.

#### 승격 쪽 첫 문 — `this+0x1f8` 비트 `0x2`

- `+0x835` · `+0x844 TEST` 로 `this+0x1f8` 의 `0x2` 가 켜져 있으면, `+0x86c AicCheckAdminApp` · `LUAShouldElevate` 로 "정말 관리자 앱인가" 를 먼저 본다.
- 아니면 `0x800702e4` 로 RPC 없이 끝.
- **이 비트를 누가 세우나는 `[공백]`** 이다.

#### 그다음 → RPC 앞 COM 한 번

- `+0x8f6 CRPCTimeout(5000)` → `+0x930 CoCreateInstance(ImmersiveShell)` (조건 `(this+0x1f8 & 0x10) == 0`) → `+0x9af Disarm` → **`+0xc04 AicLaunchAdminProcess`**.
- 이 COM 한 번이 원문 "RPCSS 연결 셋" 과 얽힌다 → 2.14.

### 2.7 ★ 승격 사무실에 거는 전화 — RPC 칸 `[정적]` + `[정적: PE]`

run 문서 2절 표를 줄인 것이다.

| 칸 | 값 | 비유 |
|---|---|---|
| 인터페이스 | **`201ef99a-7fa0-444c-9399-19ba84f12a1a` v1.0** (클라이언트 rva `0x702b90` · 서버 appinfo rva `0x50190`, 같은 UUID · 버전) | 승격 사무실 **이름표** |
| 프로시저 | **0 = `RAiLaunchAdminProcess`** (`+0x62d XOR EDX,EDX`). 서버 표 8칸 (0 ~ 7) | 메뉴 0 번 "관리자로 실행해 주세요" |
| 호출 꼴 | **비동기** (`Ndr64AsyncClientCall`, 이벤트 알림), 명시 바인딩, 기다림 = 메시지 펌프 뒤 `RpcAsyncCompleteCall` | 신청서 내고 신호등 보며 기다림. 기다리는 동안에도 창 메시지는 계속 처리 (메시지 펌프) |
| protseq · 끝점 | `ncalrpc` · **끝점 없음** (`AicpCreateBindingHandleEx +0x5c RpcStringBindingComposeW(…, NULL, NULL, NULL)`, `+0x4e XOR R9D,R9D`) | 내선인데 **직통 번호 칸이 비었다** |
| 객체 UUID | 바인딩 글의 객체 UUID = **`201ef99a-…` (인터페이스와 같은 글)** | |
| 인증 | 수준 6 (패킷 프라이버시) · 서비스 10 (WINNT) · QOS V3 `Sid` = **`CreateWellKnownSid(22)` = `S-1-5-18`** (`+0xc7 LEA ECX,[RDX+0x16]` · `+0xca`) | "**이 사람과만 통화: SYSTEM**" |
| 바인딩 캐시 | 전역 `[0x180831100]` 에 한 번 만든 핸들을 `CMPXCHG` 로 두고 다시 씀 | 연락 카드 한 장 만들어 지갑에 넣어 둠 |

#### 한 줄 읽기 — "인증" 칸

- **누가**: `AicpCreateBindingHandleEx` (`0x628484`).
- **어디서**: `+0xca` 에서 `CreateWellKnownSid` 를 부른다. 그 앞 `+0xc7 LEA ECX,[RDX+0x16]` 이 첫 인자에 **0x16 = 22** 를 놓는다.
- **무엇을**: 22 번 잘 알려진 SID = LocalSystem `S-1-5-18` 을 짓는다. 이걸 `RPC_SECURITY_QOS_V3` 의 `Sid` 칸에 넣어 `+0x126 RpcBindingSetAuthInfoExW` 로 연락 카드에 붙인다.
- **무엇을 위해**: "받는 쪽은 **SYSTEM 신분** 이어야 한다" 는 요구다. 아무 가게나 "제가 승격 사무실인데요" 하고 받으면 곤란하니까 `[추측: 까닭]`.
- 비유: run141 의 "**이 사람과만 통화**" 칸에 **SYSTEM** 이라고 적었다.

### 2.8 ★ 원문의 `0xe1af8308` 은 114 자신이다 — 단계로

원문 질문 "막힌 자리의 iface `0xe1af8308` 이 이것인가?" 의 답은 **아니오** 다.

1. 2.7 에서 봤듯 연락 카드에 **직통 번호가 없다** `[정적]`.
2. 서버 쪽도 `RpcServerUseProtseqW("ncalrpc", 10, NULL)` 로 번호를 안 정한다 → 동적 끝점 `[정적]` (2.9).
3. 그러니 클라이언트 rpcrt4 는 **114 에 물을 수밖에 없다.** `\RPC Control\epmapper` 에 **`ept_map_auth_async`** 를 보낸다.
4. 그 114 의 이름표가 **`e1af8308-5d1f-11c9-91a4-08002b14a0fa` v3.0**, 메뉴 **8** = `ept_map_auth_async` 다 (run145).
5. 그래서 원문 로그의 "iface `0xe1af8308` proc 8" 은 **114 에 거는 질문** 이고, 승격 사무실 이름표 (`201ef99a`) 는 그 질문 **탑 1층** 안에 들어 있다.

질문 안에 들어가는 것 (run 문서 2절 "`ept_map` 을 묻는 까닭" 줄):

- 탑 1층 = `201ef99a` v1.0.
- **객체 UUID = `201ef99a-…`** (nil 아님).
- **SID = `S-1-5-18`** — QOS V3 `Sid` 가 rpcrt4 `ResolveEndpoint +0x20c NormalizeServerSid` 를 거쳐 간다 `[정적: 줄 둘, 이음은 rpcrt4 몫]`.
- `0x6d9` 로 거절되면 `+0x246 GetNextNormalizedSid` 로 **다음 SID 로 한 번 더** 묻는다 (있던 `.asm`, 결과서).

그래서 원문 "왜 SID 를 넣나" 의 답:

- 클라이언트 (windows.storage) 가 연락 카드에 "서버는 SYSTEM 이어야 한다" 를 적었고, rpcrt4 가 114 에 물을 때 그 칸을 **같이 들고 간다.** 114 는 그 SID 로 "그 이름으로 등록한 사람이 정말 그 신분인가" 를 볼 수 있다 (run145 5절).
- 비유: 114 에 "승격 사무실 번호요. **단, SYSTEM 이 받는 번호로요**" 라고 묻는 것.

### 2.9 승격 사무실은 시작할 때 114 장부에 이름을 올린다 `[정적]`

114 가 답하려면 장부에 줄이 있어야 한다 (run145). appinfo 는 이렇게 올린다:

1. `ServiceMain +0x9e4 RpcServerUseProtseqW("ncalrpc", 10, NULL)` — **동적 끝점** (번호는 전화국이 지어 줌).
2. `+0xab4 AiExtEnableDesktopRpcInterface` — 지연 로드 → `appinfoext.dll` 내보내기 `0x1f50` → 그 몸이 `JMP [IAT]` 한 줄 → **다시 `appinfo!AiEnableDesktopRpcInterface`** (`objdump + pefile`).
   - 비유: 옆 건물 (appinfoext) 창구로 갔더니 "그건 원래 건물 일이에요" 하고 **되돌려 보낸다.** 얇은 다리가 한 바퀴 돌아 제자리로 온다.
3. `AiEnableDesktopRpcInterface +0x56 RpcServerRegisterIfEx(&0x50190, NULL, NULL, 0x29, 0x4d2, NULL)`.
   - `0x29` = AUTOLISTEN · ALLOW_SECURE_ONLY · ALLOW_LOCAL_ONLY `[추측: SDK 값 이름]`, 최대 호출 1234 (`0x4d2`), **보안 콜백 없음**.
4. `+0x139 RpcEpRegisterW(&0x50190, 모든 바인딩, NULL, L"AppInfo")` — UUID 벡터 NULL → **장부 객체 nil**, 주석 `AppInfo`.

- 장부 객체가 **nil** 이라서, 질문의 객체 UUID (`201ef99a-…`) 가 달라도 run145 고르기 규칙 3 ("장부 객체가 nil 이면 무엇이든") 으로 통과한다.
- `ServiceMain` 자신이 직접 등록하는 건 **다른** 인터페이스 둘 (`0497b57d-…` · `0f738e20-…`) 이다. 헷갈리지 않게.

### 2.10 신청서 칸 · 답장 칸 `[정적]`

run 문서 2절 아래 표를 줄인 것이다 (NDR64 프로시저 형식 rva `0x703700`).

| 칸 | 클라이언트가 넣는 값 (대표) |
|---|---|
| 바인딩 | 새로 / 전역 캐시 |
| p0 실행 파일 | `lpApplicationName` (`.cmd`/`.bat` 면 NULL) |
| p1 명령줄 | `this+0x200` |
| p2 `StartFlags` | 2.6 끝 값 + `& ~0x400`, 조건부 `\|8` · `\|0x10` |
| p3 `CreationFlags` | **`0x04080414`** (아래) |
| p4 현재 폴더 | `this+0x50`, 비면 `GetCurrentDirectoryW` |
| p5 창 스테이션\바탕화면 | 보통 `WinSta0\Default` |
| p6 시작 정보 | 56 B (`STARTUPINFOW` 일부 + 모니터 정보) |
| p7 창 | `HWND` (`this+0xc0`) |
| p8 시간 제한 | **`0xffffffff`** — 서버가 consent 기다림에 씀 |
| p9 `PROCESS_INFORMATION` | `[out]` 24 B — `hProcess` 8 · `hThread` 8 · PID 4 · TID 4 |
| p10 `ELEVATION_REASON` | `[out]` int32 — 클라이언트는 안 씀, 서버가 runas 때 **6** `[정적: 값만, 이름 추측]` |
| 반환 | Win32 오류 (RPC 오류가 우선) |

- 세는 법 한 마디: run 문서는 "요청 11칸 (+ 바인딩)", 결과서는 "요청 12칸" 이라 적었다. 바인딩을 넣어 세느냐의 차이이고, 칸 자체 (p0 ~ p10 · 반환) 는 같다.

#### 한 줄 읽기 — p3 `0x04080414`

- 이 숫자는 다섯 깃발을 합친 것이다:
  - `CREATE_SUSPENDED 0x4` — **대기 팻말** (run14)
  - `CREATE_NEW_CONSOLE 0x10`
  - `CREATE_UNICODE_ENVIRONMENT 0x400`
  - `EXTENDED_STARTUPINFO_PRESENT 0x80000`
  - `CREATE_DEFAULT_ERROR_MODE 0x04000000`
- 어디서 왔나: `_PrepareAndCallCreateProcess +0x2b` 가 기본값 `0x4000410` 을 `[this+0xe4]` 에 넣고, `CallCreateProcess +0xcb` 가 보낼 때 `| 0x80004` 를 붙인다.
- 눈여겨볼 점: **대기 팻말 `0x4` 는 보낼 때 붙인 쪽 (`0x80004`) 에서 온다.** 기본값 `0x4000410` 에는 없다. 이게 2.12 "누가 깨우나" 의 열쇠다.

### 2.11 ★ 승격 사무실 안 — 신청을 받아 자식을 짓기까지 `[정적]`

#### 차례 (`RAiLaunchAdminProcess`, `0x43070`)

1. **UAC 켜짐 확인** — `+0x407 CheckElevationEnabled` (kernel32, `RtlQueryElevationFlags & 1`, `objdump`). 0 이면 승인 없이 **호출자 토큰 사본** 으로 곧장 `AiLaunchProcess` (승격 없음).
2. **호출자 알아보기** — `+0x437 AiGetClientInformation`:
   - `I_RpcBindingInqLocalClientPID` → `NtOpenProcess(0x1004c0)` — 신청자 가게의 **손잡이**.
   - `RpcImpersonateClient` → `NtOpenThreadToken` → `NtDuplicateToken` → `RpcRevertToSelf` — 신청자 **신분증 사본** (잠깐 빌려 들고 복사).
   - `NtQueryInformationToken` — 세션 id.
3. **정책 · 실행 파일** — `LUAGetUACPromptPolicy` 등, 그리고 `+0x580 RpcImpersonateClient` → `+0x5c4 CreateFileW` — **신청자 신분으로** 실행 파일이 열려야 한다 (안 되면 `0x5` 등).
4. **runas 표시** — `StartFlags & 1` 이면 요구 수준 2 · `ELEVATION_REASON = 6`.
5. **승인** — `+0xe33 AiCheckLUA` (바로 아래 "승인 뒤 검사").
6. **만들기** — `+0x1096 AiLaunchProcess` (아래).
7. **끝** — `+0x131f RpcAsyncCompleteCall`.

#### 새 말 — consent (확인 창)

- **consent.exe** = "이 앱이 변경하도록 허용하시겠어요?" 확인 창을 띄우는 프로그램 `[추측: 일반적으로 알려진 설명]`. 이번엔 **이름만** 다룬다 (범위 밖, 본문 안 읽음).
- 근거로 본 것 `[정적]`: `AiLaunchConsentUI` 가 `consent.exe %u %u %p` 를 `AiLaunchProcess` 로 띄우고 (`+0x296`), `AipVerifyConsent` (`+0x2aa`) 뒤 `WaitForSingleObject(시간 제한)` (`+0x2d4`). 종료 코드 0 이면 consent 가 넘긴 토큰을 돌려준다.
- 시간 제한이 p8 `0xffffffff` 라서 **끝없이 기다린다.**

#### 승인 뒤 검사 — `AiCheckLUA` (`0x16214`)

- `AipRequireElevationPrompt` 가 "확인 필요 없음" 이면 0 · 토큰 없음.
- 확인 창을 띄웠는데 **토큰이 안 왔고** `0x10` 이 꺼져 있으면 → **`0x4c7`** (`+0x118 TEST byte [RDI],0x10` · `+0x121 MOV EBX,0x4c7`). 사용자가 "아니요" 를 누른 갈래로 읽힌다 `[추측]`.
- 받은 토큰이 **승격 아님** (`+0x16e AiCheckForElevatedUser`) 이거나 **관리자 아님** (`+0x1af AiCheckForAdminUser`) 이면 → **`0x2e4`** (`+0x18f`).
- 통과하면 `+0x1cf NtSetInformationToken(토큰, 0xc, 호출자 세션)` — 세션 id 를 **신청자 것으로** 고친다.

#### 토큰 — appinfo 는 **검사만** 한다

- appinfo 가 승격 토큰에 대해 하는 일은 "**승격 (`0x14`) 인가 · 관리자인가 · 세션**" 셋뿐이다 `[정적]`.
- 토큰 칸 (승격 종류 · 무결성 · 그룹 · 특권) 을 **짓는 줄은 appinfo 안에 없다.** 토큰은 consent 쪽이 만들어 돌려준다.
- 원천은 2.5 의 `LUAGetElevatedToken` 이 Limited → **Linked** 를 읽는 그 길일 것 `[추측]`. consent.exe 가 프로시저 7 `RAiGetElevatedToken` 으로 받는지는 consent.exe 를 안 읽어서 **모른다.**
- 비유: 승격 사무실 직원은 서랍에서 꺼내 온 관리자 카드를 **들여다보기만** 한다 — "관리자 맞네, 이 손님 것 맞네". 카드를 새로 인쇄하지는 않는다.

#### 새 말 — 부모 지정 속성

- **부모 지정 속성** = 새 프로세스를 만들 때 "이 아이의 **부모는 (만드는 나 말고) 저 프로세스** 로 해 주세요" 라고 속성 목록에 적는 칸 (`PROC_THREAD_ATTRIBUTE_PARENT_PROCESS`, `0x20000`) `[정적: 값]` · 이름 뜻은 `[추측: 일반적으로 알려진 설명]`.
- 비유: 승격 사무실 직원이 대신 출생 신고를 하면서, 부모 칸엔 **신청자 가게 이름** 을 적는다.

#### 자식 짓기 — `AiLaunchProcess` (`0x30370`)

| 항목 | 값 | 줄 |
|---|---|---|
| 토큰 | 승인 뒤 받은 승격 토큰 (승인이 필요 없었으면 호출자 토큰 사본). 플래그 `0x80` 이 꺼져 있으면 `NtDuplicateToken` 으로 한 번 더 복사 | `+0x25a` |
| 부모 | **호출자 프로세스** — `0x20000` 에 2 번 걸음의 손잡이 (`0x1004c0` = SYNCHRONIZE · QUERY_INFORMATION · CREATE_PROCESS · DUP_HANDLE) | `+0x9ba` · `+0x9c7` |
| 속성 목록 (늘) | `0x20000` + **`0x60001`** (4 B, 승격 때 `2` 가 켜질 수 있음 `[추측: 비트 뜻]`) — "조심할 곳" 5 참고 | `+0x9fb` |
| 만드는 호출 | `CreateProcessAsUserW(토큰, exe, 명령줄, NULL, NULL, FALSE, 호출자 플래그 \| 0x80004, 환경, 폴더, STARTUPINFOEXW, &pi)` — 세 갈래가 같은 인자 | `+0x10a2` · `+0x12a7` · `+0x138e` |
| 만든 뒤 검사 | `AipValidateLaunchedProcess` — 연 파일과 맞나, IFEO `Debugger` 등, 실패 `0x5` | `+0x1334` · `+0x141a` |
| 핸들 돌려주기 | `NtDuplicateObject(-1, h, 호출자, &out, …, 옵션 2)` ×2, 서버 원본은 `CloseHandle` | `+0x1499` · `+0x14ef` |

- `bInheritHandles = FALSE` — 승격 사무실의 손잡이들을 자식에게 물려주지 않는다.
- 서버가 `| 0x80004` 를 한 번 더 해도 `0x04080414` 는 그대로다 (이미 들어 있음).

#### 새 말 — 핸들 복제 (다른 가게 표로)

- 손잡이 (핸들) 번호는 **그 가게의 핸들 표 안에서만** 뜻이 있다. 승격 사무실이 쥔 손잡이 번호를 그대로 신청자에게 주면 신청자 가게에선 엉뚱한 것을 가리킨다 `[추측: 일반적으로 알려진 설명]`.
- 그래서 `NtDuplicateObject(자기, 손잡이, 신청자 가게, …)` 로 **신청자 핸들 표에 새 손잡이를 하나 만들어** 그 번호를 돌려준다 `[정적]`. 옵션 2 = `DUPLICATE_SAME_ACCESS` (같은 권한으로).
- 비유: 새 가게 열쇠를 **복사해서 신청자 열쇠고리에 걸어 준다.** 원래 열쇠는 사무실이 버린다 (`CloseHandle`).
- flow-writer 보탬: 플래그 `& 0x401` 이 켜진 갈래에선 옵션 0 · 접근 `0x20000100` 이다. ShellExecute 길에서 이 비트가 켜지는지는 안 봤다 (비트 뜻 `[추측]`).

### 2.12 ★ 누가 대기 팻말을 떼나 — 클라이언트다 `[정적]`

여기도 헷갈리기 쉽다. 두 쪽 모두 `ResumeThread` 줄을 갖고 있기 때문이다.

1. **서버 쪽 줄**: `AiLaunchProcess +0x1567 ResumeThread` 는 `CreationFlags & 4` 가 **0 일 때만** 부른다.
2. ShellExecute 는 `0x04080414` 를 보낸다 — `0x4` 가 **켜져 있다** (2.10).
3. 그래서 서버는 **안 깨운다.** 새 가게 첫 직원은 대기 팻말을 단 채 남는다.
4. **클라이언트 쪽 줄**: `CallCreateProcess +0xde6 TEST byte [R15+0xe4],4` → 0 이면 `+0xdf7 ResumeThread([R15+0x188])` (= `hThread`).
5. `[this+0xe4]` 에 들어 있는 건 기본값 `0x4000410` 이다 (2.10). 대기 팻말 `0x4` 는 **보낼 때 OR 로 붙인 사본** 에만 있고, 저장해 둔 칸에는 없다.
6. 그래서 클라이언트 쪽 검사는 "**호출자가 원래 대기 팻말을 달라고 했나**" 를 보는 셈이고, 안 했으면 **클라이언트가 직접 뗀다.**

- 실패 (-1) 하면 패키지 앱일 때만 `TerminateProcess`.
- 비유: 신청자는 "팻말은 **제가** 뗄게요" 라고 신청서에 적어 낸다. 사무실은 팻말을 단 채로 열쇠만 넘기고, 신청자가 열쇠를 받은 뒤 직접 뗀다.
- 왜 이렇게 하는지는 근거에 없다. 신청자가 깨우기 전에 무언가 (예: `PcaMonitorProcess2` · 포그라운드 정책) 를 먼저 걸어 두려는 차례로 보인다 `[추측]` — 2.13 의 차례가 그렇다.

### 2.13 응답 뒤 — 클라이언트가 하는 일 · `hProcess` `[정적]`

- **핸들 검사는 없다.** 오류 0 일 때만 차례로:
  1. `+0xc93 PcaMonitorProcess2(hProcess, 1, 실행 파일, 명령줄, 폴더, 2)` — 반환을 안 쓴다.
  2. (패키지 앱이면 `RunWithApplicationInstanceManager`)
  3. `+0xdda SetProcessLaunchForegroundPolicy(pid, 4)` — 함수가 있고 `this+0x1f8 & 0x20` 일 때.
  4. **`+0xdf7 ResumeThread`** (2.12)
  5. `+0xe40 SetLastError`
- 그 뒤 `_TryCreateProcess +0x79 NotifyCreatedProcess` · `+0x85 _FixActivationStealingApps`.

#### `hProcess` 를 돌려주는 조건

- `Finalize +0x30 CMP [RBX+0xa0],0` (오류 0) 이고 `+0x39 TEST byte [RDI+4],0x40` (`SEE_MASK_NOCLOSEPROCESS`) 이면 → `pExecInfo->hProcess (+0x68) = [this+0xa8]`.
- **`this+0xa8` 을 누가 채우나는 `[공백]`** 이다. `OnCreated` 까지만 이었다.

### 2.14 ★ 오류 → 반환 · `GetLastError` — `0x4c7` 로 덮어쓰기 `[정적]`

#### 새 말 — `hInstApp`

- **`hInstApp`** = `SHELLEXECUTEINFO` 안의 옛날식 결과 칸. 성공이면 32 보다 큰 값, 실패면 작은 오류 번호 (`SE_ERR_*`) 를 넣는다 `[추측: 일반적으로 알려진 설명]`. 이번 근거에선 "표에 없는 값이면 5 (`SE_ERR_ACCESSDENIED`)" 를 봤다 `[정적]`.

#### 오류가 위로 올라가는 길

1. `AicLaunchAdminProcess +0x74d` 가 Win32 오류 E 를 돌려준다.
2. `CallCreateProcess +0xc0c` — 양수면 `+0xc15 | 0x80070000` (HRESULT 로).
3. … → `_DoExecute +0x134 _ReportHresult` — Win32 시설이면 아래 16비트를 `this+0xa0` 에.
4. `+0x152 _FinalMapError` → `+0x15c _TryErrorMsgBox`.
5. `Finalize +0x59 _FinalMapError(…, &hInstApp)` · **`+0x5e CMP [RBX+0xcd],0` · `+0x65 MOV ECX,0x4c7` · `+0x6f CMOVNZ EAX,ECX`**.
6. `ShellExecuteExW +0x3c SetLastError` · `+0x48` / `+0xee CMP EDI,0x8000000a`.

#### 세 가지 규칙

- **BOOL**: 0 또는 `0x8000000a` 면 TRUE, 그 밖 FALSE.
- **`0x4c7` 덮어쓰기**: `_TryErrorMsgBox` 가 "오류 ∉ {0, `0x4c7`, `0x8000000a`} · `fMask & 0x400` (NO_UI) 꺼짐 · 창이 NULL 이거나 살아 있음" 이면 (상자 함수가 있으면 상자를 띄우고) `this[0xcd] = 1`. 그러면 `Finalize` 가 반환 오류를 **`0x4c7` 로 바꾼다.**
  - 비유: 직원이 손님에게 "**오류 났어요**" 상자를 이미 보여 줬으면, 장부엔 원래 까닭 대신 "**취소됨**" 이라고 적는다.
- **`hInstApp`**: 오류 0 → `this+0x98` (비면 42). 2 · 3 · 5 · 8 그대로. 표 (rva `0x768560`) 에 있으면 매핑, **그 밖 5**.

#### 표 (run 문서 3-2 줄임)

| E (서버 / RPC) | 어디서 | NO_UI 켬 → `GetLastError` | NO_UI 끔 → `GetLastError` | `hInstApp` |
|---|---|---|---|---|
| 0 | 성공 | 0 (TRUE) | 0 (TRUE) | 42 또는 `this+0x98` |
| **`0x4c7`** | consent 가 토큰을 안 줌 (`AiCheckLUA +0x121`) | `0x4c7` | `0x4c7` (상자 없음) | 5 |
| `0x2e4` | 받은 토큰이 승격 · 관리자 아님 (`+0x18f`) | `0x2e4` | **`0x4c7`** | 5 |
| **`0x5`** | 실행 파일 열기 실패 (`+0x5c4 CreateFileW`) 등 | 5 | **`0x4c7`** | 5 |
| **`0x6f7`** (원문 막힌 자리) | RPC 런타임 오류, 재시도 집합 밖 | `0x6f7` | **`0x4c7`** | 5 |
| `0x6ac` · `0x6b5` · `0x6ba` · `0x6d9` | 재시도 뒤 (아래) | 그 값 | `0x4c7` | 5 |

- 모두 FALSE (0 줄만 TRUE).
- 원문 로그의 `rax 0` (FALSE) 는 `0x6f7` 줄과 맞는다. `GetLastError` 가 `0x6f7` 인지 `0x4c7` 인지는 그 샘플의 `fMask & 0x400` 에 달렸다 `[공백: 샘플 소스 안 봄]`.

#### 다시 거는 오류 넷 — `0x6f7` 은 안 든다

- `AicLaunchAdminProcess +0x528 ~ +0x56d`: 오류가 `{0x6ac, 0x6b5, 0x6ba, 0x6d9}` 에 들면 (비트표 `0x200000004201`) **`AicpStartAIS`** 를 부르고 **두 번까지** 다시 건다. 셋째 실패는 `0x425` (`+0x6be`).

#### 새 말 — SCM

- **SCM (서비스 제어 관리자)** = 서비스를 켜고 끄는 Windows 의 관리 창구 `[추측: 일반적으로 알려진 설명]`. 비유: 동네 사무실들의 **출근 관리 데스크**.
- `AicpStartAIS` = `OpenSCManagerW` → `OpenServiceW(L"AppInfo", 0x14)` → `StartServiceW` (`0x420` = 이미 실행 중 무시) → `QueryServiceStatus` 를 500 ms 씩 최대 11번 `[정적]`.
- 즉 "승격 사무실이 아직 출근 안 했나 보다, 깨워 놓고 다시 걸자" 다.
- `0x6f7` (서식이 틀렸어요) 은 출근 문제가 아니라서 **다시 안 건다** → 그대로 실패.

### 2.15 RPCSS 연결 셋 — 꼭 필요한가: **아니오** `[정적]` (짝은 `[추측]`)

원문 로그엔 `ept_map` 앞에 RPCSS bind 거절이 세 번 있었다. "이게 막혀서 승격이 안 된 건가?" 가 질문이었다.

1. 이 길의 COM 후보는 셋이다:
   - ① `ExecuteNormal +0x252 SHCoInitialize` = `CoInitializeEx` — run149 대로 RPCSS 에 안 간다.
   - ② `_GetExecute +0x4a SHCoCreateInstance(CLSID_ExecuteAssociation)` — 자기 `DllGetClassObject` 로 먼저 만든다 (`_SHCoCreateInstance +0xae`). 성공하면 COM 을 안 탈 것 `[추측: 클래스 표 안 읽음]`.
   - ③ **`CallCreateProcess +0x930 CoCreateInstance(CLSID_ImmersiveShell, CLSCTX_LOCAL_SERVER)`** — 진짜로 COM 관리실에 가는 건 이것.
2. ③ 이 실패해도 `+0x93c TEST EAX,EAX` · **`+0x93e JS +0x997`** → 결과를 안 쓰고 `+0x9af Disarm` → `+0xc04` 로 간다.
3. 성공했을 때 달라지는 건 `StartFlags |= 0x40` 하나다.
4. 그래서 **RPCSS 가 거절해도 승격 RPC 는 나간다** `[정적]`.

- 원문 로그 차례 (RPCSS 거절 ×3 → epmapper bind → `ept_map`) 는 ③ → `+0xc04` 차례와 맞는다. 하지만 그 셋이 ③ 에서 났다는 **짝** 은 로그에 호출자가 없어서 `[추측]` 이다 (run149 "첫 COM 사용 → RPCSS bind 넷" 과 같은 손).
- 비유: 승격 신청 전에 COM 관리실에 "화면 모드가 뭐예요?" 를 한 번 물어본다. 관리실이 안 받아도 "그럼 모르는 채로" 신청서를 낸다.

### 2.16 덤 — cmstp COM 승격 · Q276a 지도 `[정적]`

#### cmstp COM 승격은 같은 길인가

- **같은 사무실 (`201ef99a`) · 같은 승인 엔진 (`AiCheckLUA` → consent), 다른 메뉴** 다.
  - 메뉴 6 `RAiForceElevationPromptForCOM` (`AiBuildCOMParams` → `AipGetTokenForService` → `AiCheckLUA`) · 메뉴 7 `RAiGetElevatedToken`.
  - 이 둘은 **프로세스가 아니라 토큰을 돌려준다.**
- 클라이언트는 windows.storage `AicForceElevationPromptForCOM` (`0x628130`, 파일 작업 쪽) 말고, **rpcss 가 `ole32!CoAicGetTokenForCOM` (지연 로드) 을 부르는 길** 이 따로 있다. 그쪽은 `[공백]`.
- 비유: 같은 승격 사무실에 "**새 가게 지어 주세요**" (메뉴 0) 와 "**관리자 카드만 빌려 주세요**" (메뉴 6 · 7) 라는 다른 신청서가 있다.

#### Q276a — 지도 상태

- **windows.storage**: `_map/ShellExecuteExW_*`. 뿌리 `0x38680`, 깊이 10, 공용 도구 기준 20 → **함수 424** (펼침 308 · 공용 104 · 깊이 끝 12), 끊긴 끝 192 (외부 168 · 간접 24). `nodes.tsv` 행 616, 간선 1,439.
- **shell32**: 노드 1 · 간선 0 (썽크).
- 한계는 2.4 끝 — 직접 호출만이라 `_ExecuteAssoc` (깊이 4) 에서 끊긴다.
- run143 "다음 요청 후보 1" (= Q276 ①) 은 이걸로 닫혔다.

### 2.17 ★ PE_emulator 쪽 — 원문 인용이 **지금 파일과 안 맞는** 두 곳

run 문서 "PE_emulator 쪽" 의 가장 중요한 대목이다. 고칠지는 사용자 · PE_emulator 결정이다.

- 결과서는 **원문 인용만** 보고 PE_emulator 쪽을 썼다.
- flow-writer 가 `../PE_emulator` 의 `ext/syscall/syscall_layer_cpp/src/rpc/epm.cpp` (286줄) · `include/syscall_layer/tokens.hpp` (50줄) 를 **직접 읽어 보니** (읽기만) 두 군데가 달랐다.

#### 다른 곳 ① — SID 든 요청은 **이미 받는다** → 진짜 벽은 **장부**

- 원문: "SID 가 들어 있으면 답을 안 만들어 fault `0x6f7` (`epm.cpp:18`)".
- 지금 파일:
  - `:231 ~ :240` `ept_map_auth` 가 (BG04 표시) SID 를 `is_valid_sid` 로 **검사만** 하고, SD 없이 장부 검색으로 넘긴다.
  - `0x6f7` 은 **SID 가 틀릴 때만** 이다 (`:236`).
  - `:18` 은 지금 `EpmEntry` 칸 줄이다.
- 그럼 지금 판에서 어디서 막히나 — 단계로 `[추측: 코드 읽기, 로그 없음]`:
  1. 장부 `ifobj_list()` (`:42 ~ :47`) 에 **nsi 한 항목뿐** 이다.
  2. 그래서 `201ef99a` 는 `:116` `ept_s_not_registered` (`0x16c9a0d6`) · 탑 0 으로 간다.
  3. 그러면 rpcrt4 는 **다음 정규화 SID 로 한 번 더** 묻는다 (`ResolveEndpoint +0x246`, 결과서).
  4. 끝내 `0x6d9` 면 `AicLaunchAdminProcess` 가 **`AicpStartAIS` (SCM `OpenServiceW("AppInfo")`) 를 두 번까지** 부른다 (`+0x528 ~ +0x56d`, 2.14).
  5. 즉 r19 의 `0x6f7` 꼴과는 **다른 막힘** 이 나올 것이다 `[추측]`.
- 비유: 원문은 "114 가 '이 사람과만 통화' 칸을 보면 전화를 끊는다" 고 적었다. 그런데 지금 114 는 그 칸을 **읽고 넘어간다.** 진짜 문제는 114 **전화번호부에 승격 사무실이 아예 없다** 는 것이다 (run145 의 "벽은 빈 장부" 와 같은 모양).

#### 다른 곳 ② — 에뮬레이터 사용자 신분증에 **관리자 그룹이 없다**

- 원문: "에뮬레이터 사용자: **관리자 그룹** · 승격 안 됨 · Medium (`tokens.hpp:35 ~ :41`)".
- 지금 파일:
  - `:21 ~ :27` 그룹 목록 = Everyone · Users (`S-1-5-32-545`) · INTERACTIVE · Authenticated Users · 로그온 SID **다섯뿐**. **Administrators (`S-1-5-32-544`) 가 없다.**
  - `:35 ~ :41` 은 무결성 Medium · 종류 · 세션 1 · `elevation_type = 1` (Default) · `elevated = false`.
- 그래서 무엇이 되나:
  - `SHTestTokenMembership(NULL, 0x220)` 가 **거짓** (그룹이 아예 없음) → 문 2 를 통과해 **RPC 길로 간다.**
  - r19 로그가 `ept_map` 까지 간 것과 맞는다 `[추측: 우리 CheckTokenMembership 길은 안 봄]`.
- 결과서와 무엇이 다른가:
  - 결과서는 원문 인용대로 "관리자 그룹이 있다" 를 전제로, "그 그룹이 켜져 있으면 RPC 없이 `CreateProcessW` 로 가 버린다" 를 경고했다.
  - 지금 파일엔 그룹이 아예 없으니 그 걱정은 **지금은 해당 없다.** 다만 반대로, 나중에 그룹을 **켜서** 넣으면 정말로 RPC 없이 `CreateProcessW` 로 간다 (`+0x4a` → `+0x53`).
- 진짜 Windows 모양에 맞추려면 (run 문서 "팀원 토큰에 승격 꼴"):
  - (a) 호출자: Administrators 를 **거부 전용** 으로 넣고 `elevation_type = 3` (Limited) + `TokenLinkedToken` = 승격 토큰 짝 `[정적: appinfo 줄]` · `[추측: deny-only 속성 값 · 진짜 그룹 꼴 — VM whoami]`.
  - (b) 승격 토큰: `TokenElevation 1` · `elevation_type 2` (Full) · Administrators 켜짐 · 무결성 High · 세션 = 호출자 세션. appinfo 가 보는 건 승격 · 관리자 · 세션 셋뿐이고, 나머지 칸 값은 VM `whoami /all`.
- 비유: 원문은 "에뮬레이터 손님 지갑에 관리자 카드가 (도장 없이) 들어 있다" 고 적었다. 그런데 지금 지갑을 열어 보니 **관리자 카드가 아예 없다.** 그래서 지금은 오히려 신청서 길로 잘 간다.

#### stub 이 채울 최소 목록 (run 문서 순서 그대로)

1. 114 장부에 `201ef99a` v1.0 항목 (객체 nil · `AppInfo`). SID `S-1-5-18` · 객체 `201ef99a-…` 인 `ept_map_auth_async` 에 그 탑을 돌려줌. 없으면 `0x16c9a0d6` · 탑 0 → SID 재시도 → `AicpStartAIS`.
2. 승격 사무실 창구: bind + 메뉴 0 (비동기) 신청서 풀기.
3. 답장: 반환 0 · `PROCESS_INFORMATION` (호출자 표 핸들 둘 · PID · TID) · `ELEVATION_REASON` 6.
4. 엔진: 승격 토큰으로 호출자의 자식을 **멈춘 채** 만들기 (깨우기는 클라이언트).
5. 클라이언트 부수 호출이 안 막히게: `PcaMonitorProcess2` (지연 로드 실패가 예외가 되는지는 `[공백]`), `CoCreateInstance(ImmersiveShell)` 실패 허용 (지금처럼), `SetProcessLaunchForegroundPolicy` (있을 때만).
6. 앞 조건: 호출자 토큰 그룹 상태 (Administrators 가 켜져 있지 않을 것), verb `runas` 를 풀 레지스트리 — r19 가 `ept_map` 까지 갔으니 지금도 통과하는 것으로 보임 `[추측]`.

### 2.18 flow-writer 대조 — 결과서와 다르게 적은 곳 (뜻은 같음)

결과서 오프셋 약 160줄을 `.asm` · `pefile` 로 맞춰 봤다. **결론을 바꾸는 어긋남은 없다.** 이 글은 run 문서 값을 따른다.

1. 요청 인자 놓는 범위 — 결과서 `+0x5a5 ~ +0x62f` 의 `+0x5a5` 는 명령 경계가 아니라서 **`+0x58a ~ +0x62f`** 로 적었다.
2. `Finalize` 의 `0x4c7` — 결과서 "`+0x6f CMOVNZ EAX,0x4c7`" → `.asm` 은 **`+0x65 MOV ECX,0x4c7` · `+0x6f CMOVNZ EAX,ECX`**.
3. 지도 깊이별 숫자 — 결과서 `[1, 8, 25, …, 18]` 은 `nodes.tsv` **행 수** (함수 + 끊긴 끝, 616) 를 센 값. 함수만 (424) 은 `[1, 7, 20, 34, 69, 54, 80, 66, 56, 25, 12]`. 간선은 1,440줄 − 머리 1 = 1,439.

보탬 넷: 가상표 셋 확인 (2.4) · 서버 등록 줄 `+0x56` · `+0x139` (2.9) · `NtDuplicateObject` 접근 `0x20000100` 갈래 (2.11) · `ServiceMain +0xaab` 별칭 무리 (같은 주소 이름 2개 — `IsAiExtEnableDesktopRpcInterfacePresent` · `IsAiExtDisableDesktopRpcInterfacePresent`, 뒤 줄 때문에 "Enable 쪽이 있나" 로 읽힘 `[추측]`).

### 2.19 정리 — 비유로 한 번 더

- 가게 주인이 "**관리자로 열어 주세요**" 주문서 (`runas`) 를 shell32 창구에 낸다. shell32 는 얇은 다리라서 바로 옆 건물 windows.storage 로 넘긴다.
- windows.storage 는 14 걸음을 지나며 두 문을 본다:
  - 주문서 종류가 "관리자로" 인가 → 승격 상자에 넣는다 (방법 2).
  - 주인 지갑에 관리자 카드가 **이미 켜져** 있나 → 켜져 있으면 그냥 직접 연다. 아니면 신청서를 쓴다.
- 신청 전에 COM 관리실에 화면 모드를 한 번 묻는다. 안 받아도 상관없다.
- 승격 사무실 직통 번호를 몰라서 **114 에 묻는다.** "SYSTEM 이 받는 번호로요" 라는 칸을 같이 들고. 원문이 본 `0xe1af8308` 은 그 **114** 다.
- 승격 사무실은 신청자 신분을 확인하고, "정말요?" 창 (consent) 을 띄우고, 돌아온 관리자 카드가 진짜인지 **들여다보기만** 한다.
- 그리고 관리자 카드로 새 가게를 짓되 **부모 칸엔 신청자** 를 적고, 첫 직원엔 **대기 팻말** 을 단 채, **열쇠 두 개를 복사해 신청자 열쇠고리에** 걸어 준다.
- 신청자는 열쇠를 받고, 몇 가지를 걸어 둔 뒤 **직접 팻말을 뗀다.**
- 실패하면 신청자는 "실패" 를 돌려준다. 이미 오류 상자를 보여 줬으면 장부엔 "**취소됨 (`0x4c7`)**" 으로 적는다.
- 에뮬레이터 쪽에선 지금 **114 전화번호부에 승격 사무실이 없다** 는 게 다음 벽일 것이다. 손님 지갑엔 관리자 카드가 아예 없어서, 신청서 길로는 잘 간다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show 1f3a320 -- runs/03_sprint/flow.md`)

**그림 (1 · 2장) 은 안 바뀌었다.** test.exe 초기화 밖이라서다. 바뀐 곳은 글뿐이다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (shell32 · windows.storage 줄) | run143 — PDB **받기만**, analyze 안 함 | + **run156 — 처음 analyze** (함수 수 · `FUN_` 수 · 지도) |
| 기준 (새 줄) | 없음 | **appinfo.pdb** `9A9DD55CF57F3F72CCF874887B92209C1` 줄 (처음 analyze, 다른 프로세스) |
| 기준 (kernelbase 줄 · 근거 줄) | — | 한 마디씩 + **run156 근거 줄** (정적 바퀴, 사진 값 대조 없음, test.exe 안 올림 `[관찰]`) |
| 3장 "볼륨 이름 묻기" 절 (run143) | 나무 `CInvokeCreateProcessVerb::Execute ⇢? ShellExecute* 에서 [공백]` | 그 줄 옆 "(run156: ⇢? 이어짐)" + **"(해결됨, 정적 → run156)"** 줄 — `[공백]` → `[정적]` (가상 칸 대상은 PE 가상표 값) |
| 3장 새 절 | 없음 | **"`runas` 승격 부탁"** — 나무 하나 + `[정적]` 줄들 (두 문 · RPC · 응답 뒤 · RPCSS 셋 `[추측: 짝]` · appinfo · cmstp · Q276a 지도) + PE_emulator 줄 (`[추측]` + `[정적]`) + 남은 것 줄 |
| 4장 "3 (범위 밖 — 볼륨)" 줄 둘 (run143) | Q275 5 · Q276 1 대기 | 표시 붙음 — Q276 ① 은 해결됨 (run156: Q276a) |
| 4장 "3 (범위 밖 — 승격)" | 없음 | run156 줄 셋 (해결됨 · 남은 것 · PE_emulator 쪽) |
| 5장 근거 | — | run156 두 줄 |

- 앞 run 고쳐 읽기 하나: **run143 문서 맨 위 한 줄** ("`ShellExecute*` → `Execute` 이음 `[공백]` 이 정적으로 닫힘"). 원문은 그대로 두었다. 틀린 줄은 없고, run143 이 `[공백]` · `[추측]` 으로 둔 줄이 채워졌을 뿐이다.
- 짐 지도 반영: **새 짐 칸 없음** (판단만, `baggage.md` 안 고침).

### 큰 그림에서 어떻게 이어지나

- **run143 ↔ 이번**: run143 의 windows.storage 사슬 (`Execute` → … → `_FixupWorkingDirectory`) 은 위가 끊겨 있었다. 이번에 `ShellExecuteExW` 부터 이어졌다. `_FixupWorkingDirectory` (`+0xdf`) 는 `CallCreateProcess` (`+0x14d`) 보다 앞이라 **runas 길도 볼륨 길을 지난다** (사이 갈래 `+0x81` 은 안 따짐).
- **run145 ↔ 이번**: 114 가 답을 고르는 법은 이미 알았다. 이번에 "**누가 · 무슨 이름표로 · 어떤 SID 를 들고**" 114 에 묻는지가 하나 더 채워졌다. 그리고 승격 사무실이 **시작할 때 장부에 어떻게 올리는지** (2.9) 도 알게 됐다 — 장부에 넣을 항목 꼴이 바로 나온다.
- **run149 · run151 ↔ 이번**: COM 관리실에 거는 첫 전화가 이 길에도 한 번 낀다. 다만 **실패해도 지나간다** — 그래서 run149 의 "지금 거절은 안전한 정직한 실패" 가 이 길에서도 맞다.
- **run14 · run34 ↔ 이번**: 대기 팻말 (`CREATE_SUSPENDED`) 을 "누가 떼나" 가 또 나왔다. run14 에선 kernelbase 가 호출자 대신 뗐다. 이번엔 **두 프로세스** 가 끼고, 떼는 건 신청자 (windows.storage) 다. 승격 자식도 결국 `CreateProcessAsUserW` 로 지어지니, 그 안쪽은 우리 지도 1장의 프로세스 생성 길 (kernelbase `CreateProcessInternalW` → 커널) 을 탈 것이다 `[추측]` — run 문서는 이 이음을 따라가지 않았다.
- **PE_emulator**: 다음 걸음 순서가 정해졌다 — 장부 항목 → 승격 사무실 창구 (메뉴 0 비동기) → 엔진 자식 (멈춘 채 · 부모 속성 · 핸들 복제) → 분리 토큰 꼴 (거부 전용 + Linked 짝).

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q283** (동적, RPC VM 묶음 — Q280 과 같은 세션, 미룸)

run 문서는 "Q280 · Q283 RPC 묶음에 얹을 꼴" 이라 했고, plan 에는 **Q283** 의 근거 칸에 run156 D1 ~ D8 이 붙었다. 멈춤점은 test.exe 가 아니라 **`ShellExecuteExW` 를 부르는 프로세스 / svchost (appinfo)** 조건이다.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| D1 신청서 바이트가 2.10 표와 맞나 | 정적은 칸 놓는 줄만 봄 | `windows_storage!AicLaunchAdminProcess+0x636` (call 직전) `[rsp+0x20 ~ +0x78]` 12칸 · `rcx` `0x1806c4920` · `edx` 0 |
| D2 114 질문 안 SID 가 정말 `S-1-5-18` 인가 · 객체 UUID · 탑 | QOS `Sid` → 질문 칸 이음은 rpcrt4 몫 | `rpcepmap!ept_map_auth` 입구 (run145) 또는 rpcrt4 `LookupNextChunk+0x11d` 직전. SID `01 01 00 00 00 00 00 05 12 00 00 00` 인가 |
| D3 답장 바이트 | 실행 때 값 | `appinfo!RAiLaunchAdminProcess+0x131f` (call 직전) · `windows_storage!AicLaunchAdminProcess+0x4b4` `eax` |
| D4 방법 값 · 문 2 결과 | 실행 때 값 (특히 deny-only 인가) | `CInvokeCreateProcessVerb::Execute+0x4f` 뒤 `dd @rbx+0x1a8 L1` (2 인가), `CallCreateProcess+0x4f` `eax` (0 이어야 RPC 길) |
| D5 COM 한 번이 원문 RPCSS 셋과 같은 호출인가 | 2.15 의 `[추측]` 짝 | `CallCreateProcess+0x937` `eax` |
| D6 진짜 승격 토큰 칸 | 토큰을 짓는 줄이 appinfo 에 없음 | `appinfo!AiLaunchProcess+0x10a2` `rcx` → `!token`, 대조로 `AiGetClientInformation+0x148` 뒤 호출자 토큰 사본 |
| D7 돌려받은 핸들이 호출자 표 안 값인가 | 실행 때 값 | `CallCreateProcess+0xdf7` `rcx` (hThread) |
| D8 창 갈래 | hwnd 가 있을 때만 | `AicLaunchAdminProcess+0x390` · `+0x3c1` |

디버거 메모 (run 문서):

- appinfo `AipValidateLaunchedProcess` 가 IFEO `Debugger` 값으로 갈래를 가른다 `[정적]` — VM 에서 IFEO `Debugger` 로 붙이는 방법을 쓰면 검사 길이 달라진다. 커널 디버거 (KDNET) 는 상관없다 `[추측]`.
- consent 기다림 시간 제한이 `0xffffffff` 라, 확인 창 앞에서 멈춤점으로 오래 붙잡아도 시간 초과는 없다 `[정적: 값]`.

### 디버거 없이 VM 에서 떠 올 것 → 380 묶음 **2 단계 VM 수집 스크립트** 재료

- 승격된 창 · 승격 전 창 `whoami /all` 한 번씩 — Administrators 가 "Group used for deny only" 인가 (2.5 · D4 와 같은 뜻), 승격 쪽 High · 특권 목록.
- UAC 레지스트리 `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System` 의 `EnableLUA` · `ConsentPromptBehaviorAdmin` · `ConsentPromptBehaviorUser` · `ConsentPromptBehaviorEnhancedAdmin` · `EnableAutomaticAdmin` (+ `PromptOnSecureDesktop` `[추측]`).
- verb 키 `reg query HKCR\exefile\shell\runas /s` · `HKCR\exefile\shell\open /s` (`DelegateExecute` · `DropTarget` 이 없는지 — 2.4 의 7 번 걸음 `[공백]`).
- 서비스 `sc qc appinfo` · `sc query appinfo` · `HKLM\SYSTEM\CurrentControlSet\Services\Appinfo` (`ObjectName` · `ServiceDll`).
- 원문 샘플이 `fMask` 에 `SEE_MASK_FLAG_NO_UI (0x400)` 를 쓰는지 (2.14 의 `GetLastError` 갈림) — PE_emulator 쪽 표본에서.

### 정적으로 남은 것 → **Q300** (`_workspace/plan.md`, 대기)

| 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|
| `CShellExecute+0xa8` (`hProcess`) · `+0x98` 를 쓰는 곳 (2.13) | `OnCreated` 의 가상 호출까지만 이었다 |
| `this+0x1f8` 비트 (`0x1` · `0x2` · `0x10` · `0x20`) 의 출처 (2.6) | 세우는 줄을 못 찾았다 |
| `CLSID_ExecuteAssociation` 이 windows.storage 클래스 표에 있나 + `_CreateAssocFromItem` 이 `HKCR\exefile\shell\runas` 를 읽는 줄 (2.15 ②) | 클래스 표를 안 읽었다 |
| kernel32 `BaseCheckElevation` · `CheckElevation` · `CheckElevationEnabled` (암묵 승격 갈래, 2.6) | kernel32 는 이번 범위 밖 (`objdump` 한 줄만) |
| consent.exe 가 승격 토큰을 어디서 받나 (2.11 `[추측]` 원천) | consent.exe 를 안 열었다 |
| rpcrt4 `ResolveEndpoint` `0x6d9` 재시도 횟수 · 끝 오류, QOS `Sid` → 질문 SID 이음 (2.8) | 다른 바퀴가 rpcrt4 를 쓰는 중이라 안 열었다 |
| appinfo `AiCheckForAdminUser` · `AipRequireElevationPrompt` 깃발 표 | 이번 질문 범위 밖 |
| `PcaMonitorProcess2` 안 (또 RPC 를 거나), ole32 `CoAicGetTokenForCOM` (cmstp, 2.16) | 다른 모듈 몫 |

### 대기열 밖 — 이름 · 짐작으로 남은 것

- `IAssociationArray::QueryObject` 가 `CBindAndInvokeStaticVerb` 를 돌려주는 줄 `[공백]` (2.4).
- 플래그 · 속성 비트 이름 (`0x29`, `0x60001` 값, `0x20011`) `[추측]`.
- NDR 문법 두 개 중 무엇으로 협상되나 `[공백]` (rpcrt4 몫).
- api 집합 `ext-ms-win-shell32-shellcom-l1-1-0` · `ext-ms-win-appcompat-pcacli-l1-1-0` 의 실제 DLL `[공백]`.
- "실제로 `ShellExecute*` 가 이 사슬을 타나" (Q275 5) — 동적, 그대로.

---

## 5. 더 읽기

- run 문서: [`run156-appinfo-runas.md`](run156-appinfo-runas.md) — "정적 — 호출 지점" 표 · 1-2 사슬 표 · 1-3 갈림길 · 2절 RPC 표 · 요청 / 응답 칸 표 · 3-2 오류 표 · 3-3 RPCSS 셋 · 4-1 · 4-2 appinfo 표 · "호출 관계" · "PE_emulator 쪽" · "flow-writer 대조" · "아직 확인 필요"
- 결과서: [`_workspace/sast/152_appinfo-runas_result.md`](../../_workspace/sast/152_appinfo-runas_result.md) — "먼저 읽는 한 줄 답" · (1) ~ (4) 표 · "VM 으로 확인할 것" · "다른 프로젝트 몫" · "다음 요청 후보"
- 요청서: [`_workspace/sast/152_appinfo-runas.md`](../../_workspace/sast/152_appinfo-runas.md)
- flow.md: [`flow.md`](flow.md) 3장 "`runas` 승격 부탁" 절 · "볼륨 이름 묻기" 절 (run143) · "epmapper 가 `ept_map` 에 답을 고르고 채우는 법" 절 (run145) · "COM 이 RPCSS 에 거는 첫 전화" 절 (run149) · 4장 "3 (범위 밖 — 승격)" 줄
- 앞 해설: [run143](run143-mountdev-name-해설.md) (가상 함수, windows.storage 볼륨 길 — 이번에 위가 이어짐) · [run145](run145-epm-endpoint-해설.md) (탑 · 장부 · 고르기 규칙) · [run149](run149-com-rpcss-해설.md) · [run151](run151-rpcss-connect-sd-해설.md) (COM 관리실 첫 전화) · [run141](run141-async-bind-해설.md) (요구 서버 SID · 가장 · 가상표) · [run14](../02_sprint/run14-create-process-internal-해설.md) (대기 팻말) · [run154](run154-dbgk-events-해설.md) (다른 뜻의 `0x60001`)

# run149 com-rpcss — 해설

> 이 글은 [run149 문서](run149-com-rpcss.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **정정 · 고쳐 읽기 (run151) (a) SD 크기 0**: 2.8 표 칸 28 ~ 30 줄 (원래 `:436`, 지금 `:440`) "0 ×5 · 0 · NULL" 과 "PE_emulator 쪽" 최소 칸 3 (원래 `:638`, 지금 `:642`) 의 "SD 0" 은 **틀렸다** `[정적]` — 그렇게 답하면 combase 가 0 B 덩어리를 SD 로 믿고 `CoInitializeSecurity +0x385` 에서 길이를 재다 덩어리 밖을 읽어 넘어진다 (`ntdll!RtlLengthSecurityDescriptor +0x2d`). 고쳐 읽기: "칸 28 = `{124, 124, 232, 228, 0}` · 29 = 708 · 30 = rpcss 가 코드 상수로 짓는 SD 넷 이어 붙임 — run151 문서".

> **정정 · 고쳐 읽기 (run151) (b) 메뉴 차례**: "메뉴 7 → 6" (원래 `:114` · `:208` 나무 · `:590` · `:619` 비유 · `:654` · `:730` · `:735`, 지금 4 씩 밀림) 은 **차례가 반대** `[정적]` — 처음엔 아파트 번호가 아직 안 등록돼 있어 **6 (등록) 이 먼저**, 7 (OID 더 받기) 은 등록 뒤 (`CallTheResolver +0x9d JZ`, 첫 때 0 인 것은 `[추측]`). 그래서 "그다음 벽" 은 메뉴 6 — run151 문서.

- 대기열: **Q257a** — run136 이 남긴 정적 **Q257 의 ①** "combase `Connect` 안" 만 앞으로 당긴 것 (`_workspace/plan.md`, 이번에 **끝 (run149)**)
  - Q257 을 그 자리에서 둘로 나눴다. ① → **Q257a** (이번), ② 창 스테이션 · 데스크톱 커널 본문 · ③ `GetConnection +0xc5` `E_ACCESSDENIED` 갈래 → **Q257b** (그대로 대기).
  - 원 요청 `../PE_emulator/_workspace/05_tuning_planner/367_internals-com-rpcss.md`, 튜닝 `366_tune18.md` OR1 · r18 — 샘플 566 이 이 거절을 받았다 (요청 쪽 에뮬레이터 숫자).
  - 사용자 결정 2026-11-26 "이것도 부탁해" (365 둘 다음).
  - 결과서 판정 **분석성공** — 요청 1 · 2 · 3 의 작은 물음마다 줄 근거로 답했다. 3 의 (b) `CoGetObject` 와 "클래스 없음" HRESULT 길만 `[공백]`.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q283** (D1 ~ D11, 그 가운데 D2 · D3 · D5 · D7 필수)
    - 정적 → **Q284** (결과서 "다음 요청 후보" 1 ~ 6, Q257b 에 보태지 않음)
- 근거:
  - run 문서 [`run149-com-rpcss.md`](run149-com-rpcss.md) (가장 중요)
  - 요청서 [`_workspace/sast/144_com-rpcss.md`](../../_workspace/sast/144_com-rpcss.md) · 결과서 [`144_com-rpcss_result.md`](../../_workspace/sast/144_com-rpcss_result.md)
    - **처음 analyze 하나** — `rpcss.dll` (약 1분, `MAXMEM=8G`). combase · kernelbase 는 다시 analyze 안 하고 export · refs · 지도만. rpcrt4 · RpcEpMap 은 **있던 `.asm` 읽기만** (그때 sast/143 이 rpcrt4 프로젝트를 쓰고 있었다).
    - 람다 `operator()` 는 combase 안에 같은 이름이 70개라, 결과서가 **rva 로 고르는 일회용 복사본** 으로 뽑았다 (`operator____18b9c0.asm`). 스크립트 문제 둘 (아래 "조심할 곳" 4).
  - flow.md 변경: docs 커밋 `d4f1ca6` (`git show d4f1ca6 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다. 그리고 전화를 받는 쪽은 다른 가게다.**
   - 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) 에 combase · rpcrt4 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run131 · run136 · run145 와 같은 줄).
   - `sample/*.c` 에 COM API 가 없다 `[소스]` (`sample/log.h:3`).
   - RPCSS 는 **다른 프로세스** 다 — `svchost.exe -k rpcss -p` 안의 `rpcss.dll` (원 요청의 `[수집] reg-users`).
   - 그래서 이번 이야기는 "**COM 을 쓰는 다른 프로그램이라면**" 이다. 그림 · 짐 지도는 그대로다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - "샘플 566", "`CoInitializeSecurity` = `0x800706b5`", "여섯 샘플이 6,837 ~ 6,995 번씩", "`Flags 0x20000`" 같은 것은 PE_emulator 로그다. 진짜 Windows 를 본 게 아니라서 `[관찰]` 이 아니다.
   - `lrpc.cpp:77` 같은 줄 번호도 PE_emulator 코드 이야기다 (flow-writer 가 줄 번호만 훑었다).
3. **오프셋 읽는 법** (앞 해설들과 같은 규칙):
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 · 표에서 명령과 같이 적힌 숫자 (`+0x33c CreateFromBindingString`, `+0x8c6 CMP …`) = **함수 안 그 줄** (call 자리 포함). run 문서 맨 위에도 그렇게 적혀 있다.
   - `RetryRPC +0x21fa8e` · `CreateFromBindingHandle +0x14b731` 처럼 **엄청 먼 오프셋** 은 "떨어진 범위" — 함수 몸 일부가 멀리 떨어진 코드 덩어리에 있어서 함수 시작에서 잰 거리가 커진 것이다 ([run148 해설](run148-dialog-end-해설.md) "조심할 곳" 3 의 덩어리와 같은 꼴).
   - `combase+0x18c1ee` (동적 D4) 는 **모듈 시작 기준** 이다. 람다 시작 `0x18b9c0` + `0x82e` 이라 같은 자리다.
   - 콜스택으로 본 적은 없다 (정적만).
4. **이름이 겹친다.**
   - combase 안에 `operator()` 가 **70개**, `Connect` 가 **5개** 다.
   - 우리 RPC 스텁은 **`Connect__12f318.asm`** (rva `0x12f318`) 이다. `Connect.asm` 은 rva `0xaef40` 의 **다른 함수**.
   - `InitRemoting.asm` 이 우리 것 (rva `0x36ad4`), `InitRemoting__11e0fc.asm` 은 다른 것.
   - 그래서 결과서가 "스크립트 문제 둘" 을 적었다 — `ExportFunctions` 가 이름으로만 고름 · `CallGraphMap` 지도가 이름 기준 (예: 지도의 `ICoCreateInstanceExWorker +0x12f1 → operator() → Connect` 는 **가짜**). 하네스 제안 거리다.
5. **비유 이름이 run 문서와 다르다.**
   - run 문서 "결과" 비유: RPCSS = "**건물 관리 사무소**", `ept_map` = "**안내 데스크**", 컨텍스트 핸들 = "**출입증**" (run 문서는 이 비유를 `[추측: 비유]` 로 둔다).
   - 그런데 앞 해설들에선 이미 **관리사무소 = 커널**, **epmapper = 114 안내**, **컨텍스트 핸들 = 접수 번호표** 다.
   - 그래서 이 글은 앞 해설 말로 부른다: RPCSS = **COM 관리실** ([run136 해설](run136-winsta-name-해설.md)), `ept_map` = **114 에 번호 묻기**, 컨텍스트 핸들 = **접수 번호표**. 그림만 다르고 사실은 같다.
6. **값은 하나도 안 봤다.**
   - 진짜 rpcss 가 돌려주는 핸들 · PID · 서명 같은 값은 이번 범위 밖이다 (동적 Q283 몫).
   - 응답 칸의 **차례** 는 `[규칙]` 이다 — 형식 문자열 바이트는 `[정적]` 이지만, 선 위 바이트 자리로 푼 것은 공개 NDR 규칙에 기댔다는 표시 ([run137 해설](run137-alpc-completion-해설.md) "조심할 곳" 5).

이미 푼 말은 **링크만** 건다. 다만 이번 글의 뼈대가 되는 **COM · RPCSS · 인터페이스 · bind · 프로시저 번호** 는 처음 읽는 사람도 따라오도록 1절 · 2절에서 한 줄씩 다시 짚는다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- COM · combase = 가게에 들인 **COM 담당 건물** → [run131 해설](run131-user32-regclass-해설.md) "새 말 — COM · combase"
- HRESULT = COM 쪽 32비트 결과 코드, "음수면 실패" → [run89 해설](run89-dotnet-start-해설.md) "새 말 — HRESULT"
- HRESULT 바꾸기 규칙 (양수 `e` → `0x80070000 | (e & 0xffff)`) → [run136 해설](run136-winsta-name-해설.md) 2.6
- `CoInitializeSecurity` · RPCSS = **COM 관리실**, 람다 `operator()` → [run136 해설](run136-winsta-name-해설.md) 1절 "새 말" · 2.9
- ALPC 포트 = 프로세스끼리 거는 **전화선 끝** → [run68 해설 2.4](run68-dll-init-rest-해설.md)
- RPC 런타임 (rpcrt4) = 가게 안 **전화 부서** → [run99 해설](run99-rpc-caller-해설.md)
- **run129 에서 푼 것** → [run129 해설](run129-winnsi-rpc-해설.md)
  - 바인딩 = **연락 카드**, 끝점 = 상대 사무실 **직통 번호**, `ncalrpc` = "같은 건물 **내선**" → 2.4
  - 인터페이스 = 사무실 **이름표**, 프로시저 번호 = **메뉴 번호**, 컨텍스트 핸들 = **접수 번호표**, 비동기 RPC → 2.5
  - epmapper = **114 안내**, `ept_map` = 114 에 거는 "번호 알려 주세요" → 2.7
- **run133 에서 푼 것** → [run133 해설](run133-lrpc-wire-해설.md)
  - bind = **첫인사**, 전송 문법 (NDR 2.0 · NDR64) = **말투**, 제안 마스크 · 슬롯 → 2.5
  - 수락 마스크 · "비트 정확히 하나" 검사 → 2.6
  - fault = "못 했어요, 까닭은 이 번호" 답장 → 2.4
- **run137 에서 푼 것** → [run137 해설](run137-alpc-completion-해설.md)
  - 형식 문자열 = 프로시저의 **서식 설명서** → 2.10
- **run141 에서 푼 것** → [run141 해설](run141-async-bind-해설.md)
  - 요구 서버 SID = 연락 카드의 "**이 사람과만 통화**" 칸 → 2.4, 가장 = 남의 **신분증을 빌려 들기** → 2.10
- **run145 에서 푼 것** → [run145 해설](run145-epm-endpoint-해설.md)
  - 114 의 **장부** (= 전화번호부), 114 의 **손님 창구 · 직원 전용 창구** → 앞머리 "새 말 — 장부" · 2.3
- **run148 에서 푼 것** → [run148 해설](run148-dialog-end-해설.md)
  - **금지 꼴** (stub 이 이렇게 대답하면 못 빠져나오거나 터지는 꼴) → 2.9

이번에 처음 나오는 말 — **교환대 (RPCSS 가 하는 일)**, **바인딩 글 (문자열 바인딩)**, **정적 끝점**, **`CFastBH`**, **동기 bind**, **창구 넷 (`ILocalObjectExporter` · `IMachineActivatorControl` · `IROT` · `ISCM`)**, **스텁 (`NdrClientCall2`)**, **DUALSTRINGARRAY (로컬 해결사 연락처)**, **예약 OID**, **프로세스 서명**, **`gGotSecurityData` (성공 도장)**, **텔레메트리 호출**, **"줄 `0x3..`" 번호**, **`RetryRPC` · `CheckStatus`**, **정직한 실패 · 끝없이 다시 거는 금지 꼴**, **암묵 호출** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, 핸들 = **손잡이**, 토큰 = **신분증**.
- combase = 가게에 들인 **COM 담당 건물**, rpcrt4 = 가게 안 **전화 부서**, ALPC 포트 = **전화선 끝**.
- 바인딩 = **연락 카드**, 끝점 = **직통 번호**, `ncalrpc` = **내선**, 인터페이스 = 사무실 **이름표**, 프로시저 번호 = **메뉴 번호**, 컨텍스트 핸들 = **접수 번호표**, bind = **첫인사**, 전송 문법 = **말투**.
- epmapper = **114 안내**. 114 는 장부 (**전화번호부**) 를 들고 "그 사무실은 몇 번이에요" 를 알려 준다 (run145).
- RPCSS = 동네 **COM 관리실** (run136). combase 는 거기에 전화를 걸 때 "저는 `WinSta0` 건물 `Default` 방에서 걸어요" 라는 **주소** 를 붙인다 (run136).
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 앞 해설 말로 바꾼 것 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - **COM 관리실은 114 와 같은 사무실에, 같은 대표 번호로 앉아 있다.** 대표 번호 (`epmapper`) 는 COM 담당 건물 수첩에 **이미 적혀 있어서**, 114 에 "COM 관리실 번호요" 하고 물을 일이 없다.
  - 전화가 이어지면 COM 관리실의 **창구 넷** (해결사 · 활성화 제어 · 실행 중 목록 · 클래스 관리) 에 차례로 **첫인사** 를 한다. 한 창구라도 "그런 창구 없어요" (`0x6b5`) 하면 그 자리에서 끊는다.
  - 넷 다 받아 주면 첫 창구 메뉴 0번 **"저 왔어요" (`Connect`)** 를 보낸다. 내 주소 · 내 이름 (exe 경로) 을 주고, **접수 번호표 · 관리실 직원 번호 (rpcss PID) · 내 서명 · 관리실 연락처** 를 받는다. 넷 가운데 하나라도 빈칸이면 "이상한데?" 하고 포기한다.
  - "바빠요" (`0x6bb`) 라는 대답만은 **0.1초 쉬고 끝없이 다시 건다.** 그 밖의 "못 해요" 는 한 번에 받아들이고 끝낸다.

### 한 줄로 먼저 — 무엇을 얻었나

> **combase 는 바인딩 글 `ncalrpc:[epmapper,Security=Impersonation Dynamic False]` 로 끝점을 박아 넣어서, 114 에 번호를 묻지 않고 (`ept_map` 없이) `\RPC Control\epmapper` 로 곧장 간다. bind 는 동기다.**
> **한 번의 `GetConnection` 안에서 bind 가 네 번 — `ILocalObjectExporter` (`e60c73e6-88f9-11cf-9af1-0020af6e72f4` v2.0, PDB 이름으로 확정) → `IMachineActivatorControl` → `IROT` → `ISCM`. 말투는 넷 다 NDR 2.0 하나. 하나라도 fault 면 거기서 `0x8007NNNN` 으로 끝난다.**
> **첫 프로시저는 `ILocalObjectExporter` 0번 = `Connect` (입력 6 · 출력 25 · 상태). 상태가 0 이어도 접수 번호표 · 해결사 연락처 포인터 · rpcss PID · 프로세스 서명 가운데 하나라도 0 / NULL 이면 `0x8000ffff`.**
> **실패 쪽: `RetryRPC` 는 상태 `0x6bb` 일 때만 100 ms 쉬고 끝없이 다시 건다. 그 밖 양수 N 은 `CheckStatus` 가 `0x80070000 | N` 으로 바꿔 깨끗이 끝낸다. 그래서 에뮬레이터의 지금 대답 (bind 에 `0x6b5`) 은 안전한 정직한 실패다.**
> **진짜에서 이 창구들은 `rpcss.dll` 이 받고, `epmapper` 번호는 `rpcss.dll` 과 `RpcEpMap.dll` 이 같은 표 칸 (16) 으로 함께 연다.**

- 모두 `[정적]`.
- 단 둘은 `[추측]` 이다: "bind 문법 마스크 = 1" (마스크 만드는 줄까지 안 따라감), "bind 2 ~ 4 번은 새 연결 없이 같은 연결을 다시 쓴다" (`PickAssociation` 비교 줄 일부만 봄).

요청서 질문에 대한 답 (run 문서 "요약" · 결과서 "먼저 읽는 한 줄 답" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1 바인딩 글 · `ept_map` 을 묻나 | `ncalrpc:[epmapper,…]`, **안 묻는다** (끝점이 글에 박힘, 끝점 풀이 함수를 안 가져옴) `[정적]` | 2.3 |
| 1 받는 모듈 | `rpcss.dll`. `epmapper` 칸 16 을 RpcEpMap 과 나눠 씀 `[정적]` | 2.4 |
| 1 인터페이스 · 문법 | bind 넷, NDR 2.0 하나 (마스크 1 `[추측]`) | 2.5 · 2.6 |
| 1 보안 | 요구 서버 SID NetworkService · 가장 3 · 인증 6 · 서비스 10 — **연결 쪽에만**, bind 메시지엔 칸 없음 `[정적]` | 2.6 |
| 2 첫 프로시저 · 스텁 꼴 | 0번 `Connect`, `NdrClientCall2`, 입력 6 · 출력 25 `[정적]` | 2.7 · 2.8 |
| 2 응답 검사 | 0 이면 안 되는 칸 넷 → `0x8000ffff`. OID 개수 · SD 크기 합은 **안 막음** `[정적]` | 2.9 |
| 2 실패 쪽 | `0x6bb` 만 끝없이 다시, 그 밖 양수는 `0x8007NNNN` 으로 끝 `[정적]` | 2.11 |
| 3 그다음 | `CoInitializeSecurity` 는 `SetAppID` (1) 만 더, `CoInitializeEx` 는 이 번호로 안 감, 첫 `CoCreateInstance` 는 프로시저 7 → 6. `GetConnection` 부르는 곳 46 `[정적]` | 2.12 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

test.exe 흐름 밖, **옆길** 이다. run131 (창 클래스 등록) · run136 (창 스테이션 이름 묻기) 과 같은 combase 의 **COM 보안 쪽** 이고, 이번엔 그 끝에서 **다른 가게 (svchost) 에 거는 첫 전화** 를 연다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  ntdll: LdrInitializeThunk → LdrpInitializeProcess → DLL 들이기 · 전원 켜기
 └─ NtContinue (은퇴) → exe 진입점 → (CRT 점선 상자) → main
                                                        │
      test.exe 는 여기서 COM 을 안 쓴다 [소스]          │ COM 을 쓰는 프로그램이라면:
                                                        └─ CoInitializeSecurity  (직접, 또는 첫 CoCreateInstance 가 대신)
                                                             └─ GetConnection → 람다 operator()
                                                                  ├─ 주소 글 만들기 (창 스테이션 · 데스크톱)        (run136)
                                                                  ├─ 대표 번호로 곧장 전화 · 창구 넷에 첫인사     ← 이번
                                                                  └─ Connect (메뉴 0) · 답 검사 · 실패 처리        ← 이번
      ── 다른 가게: svchost (-k rpcss) ───────────────────────────┼─────────────────────────────
                                                                  ├─ RpcEpMap.dll = 114 (ept_map 답)              (run145)
                                                                  └─ rpcss.dll = COM 관리실 (_Connect)            ← 이번 (받는 쪽, 필요한 만큼)
```

- flow.md 에서는 **3장 "창 스테이션 · 데스크톱 이름 묻기" 절 (run136) 바로 뒤** 에 새 절 "**COM 이 RPCSS 에 거는 첫 전화**" 로 들어갔다.
- 프로그램이 `main` 뒤에 부르는 일이라 그림엔 안 넣었다.

#### 새 말 — COM · RPCSS, 처음 읽는 사람을 위해 다시

- **COM** = Windows 의 "부품 객체" 규칙이고, 그 런타임 본체가 `combase.dll` 이다 `[추측: 일반적으로 알려진 설명]` (run131 해설). 비유: 가게에 들인 **COM 담당 건물**.
- **RPCSS** = COM 을 관리하는 시스템 서비스 `[추측: 일반적으로 알려진 설명]` (run136 해설). 비유: 동네 **COM 관리실**.
  - 하는 일로 치면 COM 의 **교환대** 다. 다른 가게에 있는 COM 물건이 어디 있는지 이어 주고, 물건을 새로 띄워 주고, 지금 떠 있는 물건 목록을 든다 `[추측: 일반적으로 알려진 설명]`.
  - 이번 근거가 보여 주는 건 거기까지 가는 **창구 이름 · 메뉴 이름** 뿐이다 — 해결사 (`ILocalObjectExporter`, combase 쪽 클래스 이름이 `CRpcResolver`) · 활성화 제어 (`IMachineActivatorControl`) · 실행 중 목록 (`IROT`) · 클래스 관리 (`ISCM`), 메뉴 `ClientResolveOXID` · `ServerRegisterOXID` … `[정적: 이름]`. 그 몸은 이번에 안 읽었다.
  - 진짜로 이 창구들을 받는 모듈 = **`rpcss.dll`** `[정적]` (2.4). 그 모듈이 `svchost.exe -k rpcss -p` 안에 있다는 건 원 요청의 서비스 설정 (`[수집] reg-users`) 이 근거다.
- 비유: 교환대 직원에게 일을 부탁하려면, 먼저 **교환대에 전화를 걸어 "저 왔어요" 하고 접수 번호표를 받아야** 한다. 이번 바퀴가 바로 그 첫 전화다.

### 왜 이 질문이 지금 왔나

PE_emulator 쪽에서 막혔다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님).

- r18 의 샘플 566 이 `\RPC Control\epmapper` 에 **동기** 로 연결한 뒤, 곧바로 인터페이스 `e60c73e6…` 로 **bind** 한다.
- 에뮬레이터 창구는 그 인터페이스를 몰라서 fault `0x6b5` 를 준다 → `CoInitializeSecurity` = `0x800706b5`.
- 예약 작업 (`schtasks`) · 프로세스 죽이기 (`taskkill`) · 그림자 복사본 지우기 (`vssadmin`) · WMI 묻기 (`CoGetObject`) 가 모두 그 바로 앞에서 끝난다.
- 에뮬레이터 정책은 "**꼴만 맞춘 최소 대답 · 값 지어내지 않음 · 모르는 부탁은 정직한 실패**" 다.
- bind 를 받아 주면 첫 프로시저가 올 텐데, 그 **응답 꼴** 과 **combase 가 응답에서 무엇을 검사하는지** 를 몰라서 못 채웠다. 틀린 성공 응답은 지금보다 나쁠 수 있으니, 먼저 코드로 알아야 했다.

### 무엇이 궁금했나

요청서의 세 덩어리:

1. **bind — 무엇을 내미나**: 바인딩 글 (`ept_map` 을 안 묻는 게 코드로 맞나) · 인터페이스 UUID · 버전 · 말투 · 연결 / bind 꼴을 바꾸는 보안 · 진짜에서 받는 모듈.
2. **첫 프로시저**: 번호 · 요청 · 응답 칸 차례 · 타입 · **응답에서 combase 가 검사하는 것** · out 값 가름 (가) ~ (라) · 실패 쪽 (깨끗이 끝나는 상태 · 끝없이 다시 거는 상태).
3. **그다음**: `CoInitializeSecurity` 나머지 · `CoInitializeEx` · 첫 COM 사용이 이 번호로 부르는 것 · `GetConnection` 을 부르는 곳.

### 바퀴 전에 알던 것

- `[정적]` (run136) `CoInitializeSecurity +0x47d GetConnection` → `GetConnection +0xbe` 람다 `operator()` → `+0x82e Connect` (rva `0x12f318`). `Connect` 둘째 인자 = 주소 글 `_pwszWinstaDesktop`, 셋째 = exe 경로.
  - **`Connect` 안은 안 읽었다.** "`Connect` → epmapper 로 이어진다" 는 `[추측]` 이었다 (run136 해설 2.8).
  - 핸들 단계 0 이면 "0 + 글 NULL" 로 **`Connect(NULL)`** 에 간다 — 그 끝은 몰랐다.
- `[정적]` (run133) LRPC 연결 · bind `0x70` B 꼴 · 문법 마스크 `+0x48` · `CompleteBind` 의 "비트 하나" 검사 · ALPC → RPC 상태 표.
- `[정적]` (run137) 형식 문자열 · 프로시저 표 읽는 법. 그리고 "같은 비동기 틀을 RPCSS 도 쓴다 (원 요청 말)" — `[추측]`.
- `[정적]` (run145) 같은 `epmapper` 번호의 **114 쪽** (RpcEpMap) — 장부 · 고르기 · 등록.
- `[추측 — 공개 이름]` (원 요청) "`e60c73e6…` = `ILocalObjectExporter`".
- 몰랐던 것: **`Connect` 안 전부** — 어디로 어떻게 전화하나, 무엇을 보내고 무엇을 받나, 무엇을 검사하나, 실패하면 다시 거나.

---

## 2. 찾은 것

### 2.1 한눈에 — 대표 번호 · 첫인사 넷 · `Connect` · 답 검사 `[정적]`

flow.md 새 절의 나무를 그대로 옮긴 것이다 (오프셋은 ret 자리, `(+0x8c6 …)` 은 그 함수 안 줄).

```
combase!CoInitializeSecurity (0x7e880) +0x482 CRpcResolver::GetConnection (0xa14d0) +0xc3 operator() (0x18b9c0)   [정적]
 ├─ +0x2b7  CreateWellKnownSid(24)                  요구 서버 SID = NetworkService (연결 쪽에만)
 ├─ +0x341  CFastBH::CreateFromBindingString        ILocalObjectExporter e60c73e6… v2.0
 │          ├─ +0x80  RpcStringBindingParseW("ncalrpc:[epmapper,Security=Impersonation Dynamic False]")
 │          ├─ +0xcf  RpcBindingCreateW               정적 끝점 · LRPC 3 — ept_map 안 물음
 │          └─ +0xea  BindWithTimeout +0x13 RpcBindingBind(NULL, …)   동기 bind ── ALPC ──▶ (svchost rpcss)
 ├─ +0x3cf · +0x3ee · +0x46d  CFastBH::CreateFromBindingHandle   IMachineActivatorControl 1.0 · IROT 0.2 · ISCM 0.2
 │          └─ +0x2a RpcBindingCopy · +0x41 RpcBindingUnbind · +0x59 BindWithTimeout
 ├─ (bind 하나라도 fault N) → 0x8007NNNN 로 끝 (재시도 없음)
 ├─ +0x833  Connect (0x12f318) +0x182 NdrClientCall2 (프로시저 0)  ── ALPC ──▶ rpcss!_Connect (0x76d80)
 ├─ +0x889  RetryRPC    상태 0x6bb → Sleep(100) → +0x6b4 (다시, 한도 없음)
 ├─ +0x8af  CheckStatus 양수 N → 0x8007NNNN
 ├─ (+0x8c6 · +0x8de · +0x8e8 · +0x900)  _ph · DSA · gdwScmProcessID · _ProcessSignature 중 0 → 0x8000ffff
 ├─ +0x9b8  InitializeSystemSecurityDescriptors
 └─ +0xa83  gGotSecurityData = 1 · +0xae4 _dwFlags |= 1
(첫 COM 사용) … CoGetDefaultContext +0x44 InitChannelIfNecessary +0x11e CComApartment::InitRemoting
 ├─ +0xbb   InitializeSecurity +0x81 CoInitializeSecurity   (암묵 — 위 길)
 └─ +0x2a2  CallTheResolver +0x10d NdrClientCall2 (프로시저 7) · +0x1bc RegisterOXIDAndOIDs → 프로시저 6
```

- `── ALPC ──▶` 는 **다른 프로세스로 가는 RPC** 다. 연결 · 메시지가 실제로 오가는 길은 rpcrt4 안이라 이번에 안 읽었다 (그 길은 run133 · run141 에서 봤다).
- 한 줄 요약: **카드 만들기 → 첫인사 넷 → "저 왔어요" → (바빠요면 다시) → 결과를 HRESULT 로 → 빈칸 검사 → 성공 도장.**

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `├─ +0x341  CFastBH::CreateFromBindingString   ILocalObjectExporter e60c73e6… v2.0`

- **누가**: combase 람다 `operator()` (rva `0x18b9c0`). 이 람다를 부르는 곳은 `GetConnection +0xbe` **하나뿐** 이다 (`_refs/0x18b9c0.tsv`).
- **어디서**: 람다 `+0x33c` 에서 부르고, 돌아오는 자리가 `+0x341`.
- **누구를**: `CFastBH::CreateFromBindingString` (rva `0x11ab98`).
- **무엇을 위해**: 바인딩 글로 **연락 카드** 를 만들고, 첫 창구 (`ILocalObjectExporter`) 에 **첫인사 (bind)** 를 한다. 인터페이스 구조체는 그 전에 `+0x15d LEA RCX,[0x1802fcd00]` 로 잡아 둔다.
- 실패하면 `+0x34a JMP +0xcb8` 로 가서 람다가 끝난다 (run 문서가 "줄 `0x300`" 이라 부르는 갈래).

#### 새 말 — "줄 `0x3..`" 번호

- run 문서가 실패 갈래마다 "줄 `0x300`", "줄 `0x38a`" 처럼 붙이는 번호다. 코드에서는 실패 보고 함수에 넘기는 **상수** 로 보인다 — 예: `+0x90e MOV EDX,0x38a` `[정적]`.
- 결과서는 `0x300` 갈래에 "Failed to create binding handle to rpcss (ILocalObjectExporter)" 라는 글을 같이 적었다. 그래서 **소스 파일의 줄 번호** 로 보인다 `[추측: 상수 모양과 붙은 글로 짐작]`.
- 이 글에서는 그냥 "**어느 실패 갈래인지 가리키는 번호표**" 로 읽으면 된다.

#### 줄 ②: `├─ +0x833  Connect (0x12f318) +0x182 NdrClientCall2 (프로시저 0)  ── ALPC ──▶ rpcss!_Connect (0x76d80)`

- 오프셋이 둘이다.
  - `+0x833` = **람다 안** ret 자리 (call 은 `+0x82e`).
  - `+0x182` = **`Connect` 안** ret 자리 (call 은 `+0x17b`). 곧 "람다가 `Connect` 를 부르고, `Connect` 가 다시 `NdrClientCall2` 를 부른다".
- **누구를**: `NdrClientCall2(ILocalObjectExporter_StubDesc 0x274270, lclor ProcFormatString+0x2)` — 첫 창구 메뉴 **0번**.
- **무엇을 위해**: COM 관리실에 "**저 왔어요**" 를 보내고, 접수 번호표 · 관리실 PID · 서명 · 연락처 등 25칸을 받아 온다 (2.7 · 2.8).
- 실어 보내는 것 (람다가 레지스터 · 스택에 채움): RCX `[R14]` (`+0x78c`) · RDX `_pwszWinstaDesktop` (`+0x742`) · R8 exe 경로 (`+0x751`) · R9 IPID 표 (`+0x77e`) · `[RSP+0x20]` 깃발 (`+0x82a`) · `[RSP+0x28]` `0x8664` (`+0x823`) `[정적]`.
- `── ALPC ──▶` 끝의 `rpcss!_Connect` (rva `0x76d80`) 가 받는 쪽이다 (2.10).

### 2.3 ★ 114 에 번호를 안 묻는다 — 번호가 카드에 박혀 있다 `[정적]`

run 문서 1-1. 요청서 물음 1 "**`ept_map` 을 안 묻고 곧장 가는 게 코드로 맞나**" 의 답 = **예**.

#### 새 말 — 바인딩 글 (문자열 바인딩) · 정적 끝점

- **바인딩 글** = 연락 카드 내용을 **한 줄 글자** 로 적은 것. "전화 방식 : [끝점, 옵션]" 꼴이다.
  - combase 의 것: **`ncalrpc:[epmapper,Security=Impersonation Dynamic False]`** — combase `.rdata` rva `0x2cc510` 의 UTF-16 글 `[정적]` (결과서 `pefile`).
  - 읽으면: 전화 방식 = **내선** (`ncalrpc`), 번호 = **`epmapper`**, 옵션 = 보안 설정.
- **정적 끝점** = 카드에 **번호가 처음부터 적혀 있는** 것. 반대는 run129 의 winnsi — 번호 칸을 비워 두고 첫 전화 때 114 에 물어 채웠다 (run129 해설 2.4 · 2.7).
- 비유: winnsi 는 "nsi 사무실, 내선, 번호는 모름" 카드를 들고 114 에 먼저 물었다. COM 담당 건물은 수첩에 **"대표 번호 `epmapper`"** 가 이미 적혀 있다.

#### 한 걸음씩 — 카드 만들기 (`CreateFromBindingString`)

1. `+0x2f LEA R12,[0x1802cc510]` — 바인딩 글을 잡는다.
2. `+0x63 MOV RCX,R12` → `+0x79 RpcStringBindingParseW` — 글을 쪼개 **끝점 `epmapper`** 를 뽑는다.
3. `+0xb2 MOV [RBP-0x19],3` — 프로토콜 번호 **3 (LRPC)**. `+0xbd MOV [RBP-0x9],RAX` — 뽑은 끝점을 틀에 넣는다. 네트워크 주소는 NULL.
4. 옵션: Version 1 · Flags 1 · ComTimeout 5 (`+0xc1`) · CallTimeout `0xffffffff` (`+0x8f`) — 구조체 이름은 `[이름 추측]`.
5. `+0xc8 RpcBindingCreateW(틀, 보안, 옵션, &핸들)` — **카드 완성**.
6. `+0xe5 BindWithTimeout` — **첫인사** (아래).

#### 왜 "안 묻는다" 고 말할 수 있나 — 단계로

1. 끝점이 **글에 박혀 있다** (위 1 ~ 3).
2. combase 가 가져오는 RPC 함수 목록에 **끝점을 푸는 함수가 없다** — `RpcEpResolveBinding` · `RpcMgmtEpEltInq*` 가 없고, `RpcStringBindingParseW` · `RpcBindingCreateW` · `RpcBindingCopy` · `RpcBindingUnbind` · `RpcBindingBind` · `NdrClientCall2` · `NdrClientCall3` 뿐이다 (결과서 `llvm-readobj --coff-imports`).
3. 그래서 combase 가 직접 114 에 묻는 길은 코드에 없다 → `ept_map` 없이 `\RPC Control\epmapper` 로 곧장 간다 `[정적]`.

#### 새 말 — 동기 bind

- **동기** = 전화를 걸고 **답이 올 때까지 수화기를 들고 서 있는** 방식. 반대가 run129 해설 2.5 의 비동기 RPC (신호등 맡기고 딴 일).
- `BindWithTimeout +0xc RpcBindingBind(NULL, 핸들, 인터페이스)` — 첫 인자 (비동기 상태) 가 **NULL** (`+0xa XOR ECX,ECX`) 이라 동기다 `[정적]`.
- 이름에 "Timeout" 이 있지만, 셋째 인자 (시간제한) 는 **안 쓰인다** (`+0x4 MOV R8,RDX` · `+0x7 MOV RDX,RCX` 로 자리만 옮김) `[정적]`.
- 요청 쪽 로그의 `Flags 0x20000` (동기) 과 맞는 모양이다 (에뮬레이터 로그라 `[관찰]` 아님).

### 2.4 ★ 한 번호, 두 주인 — `rpcss.dll` 과 `RpcEpMap.dll` `[정적]`

run 문서 1-4. 그럼 대표 번호 `epmapper` 로 걸면 **누가 받나**?

#### 받는 쪽 — `rpcss.dll`

- `rpcss!StartObjectExporter +0xb43 LEA RCX,[0x180129500]` → `+0xb65 RpcServerRegisterIf3(…)` — `ILocalObjectExporter` **서버 구조체** 를 등록한다 `[정적]`.
  - `Flags 0x20` = 로컬 전용 `[이름 추측]`, 콜백 `LocalInterfaceOnlySecCallback` (rva `0x1fbf0`).
  - 메뉴 12개 모두 `NdrServerCall2` (동기) — 결과서 `pefile` 디스패치 표.
- 나머지 창구 셋도 rpcss 안이다 — `IMachineActivatorControl` (`0x129060`, 메뉴 7개) · `IROT` (`0x129130`, 11개) · `ISCM` (`0x128f00`, 27개) — 서버 구조체 rva (결과서).
- 공개 DCOM `IObjectExporter` (`99fcfec4…`, `0x1292d0`) 도 같은 프로세스에 있지만 이번 길엔 안 나온다.

#### 같은 번호를 둘이 연다

- 번호를 여는 줄: `rpcss!UseProtseqIfNecessary +0x23e RpcServerUseProtseqEpExW`. 전역 표 `gaProtseqInfo` (rva `0x164030`, 칸 24 B) 의 **칸 16 = {1, `ncalrpc`, `epmapper`}** (결과서 `pefile`).
- 114 쪽: `RpcEpMap!InitializeService +0x23 UseProtseqIfNecessary(16)` — **칸 번호가 같다**.
- → 같은 (`ncalrpc`, `epmapper`) 를 **두 모듈이 연다** = 전화선 하나를 서비스 둘이 나눠 쓴다 `[정적]`.
- 둘이 같은 `svchost -k rpcss` 안이라는 건 원 요청의 서비스 설정이 근거다 (코드만으로는 못 정함).
- run145 의 EPM (`e1af8308…`) · RpcEpMap 직원 전용 창구 (`0b0a6584…`) 와는 **한 전화선의 다른 창구 (인터페이스)** 다.

비유: 114 사무실 대표 번호로 전화하면 **같은 사무실 안에 COM 관리실 창구들도 같이 앉아 있다.** 전화를 건 사람이 첫인사에서 "**114 창구요**" 하면 114 가, "**해결사 창구요**" 하면 COM 관리실이 받는다. 그래서 COM 담당 건물은 114 에 번호를 물을 필요가 없다 — 이미 그 번호다.

### 2.5 ★ 창구 넷에 첫인사 — bind 넷 `[정적]`

run 문서 1-2.

#### 새 말 — 인터페이스 · bind, 처음 읽는 사람을 위해 다시

- **인터페이스** = 상대 사무실 창구의 **이름표** — UUID (긴 고유 번호) + 판 번호 (주 · 부 버전) (run129 해설 2.5).
- **bind** = 본론 전에 "**저는 이 창구 (인터페이스) 를 찾고, 이 말투를 할 줄 알아요**" 를 맞추는 **첫인사** (run133 해설 2.5). 창구가 모르는 이름표면 fault 로 "그런 창구 없어요" 라고 답한다.
- 한 전화선 위에서 창구마다 따로 첫인사를 한다. 그러면 창구마다 **슬롯 번호** 를 받고, 본론 메시지 머리에 그 번호를 붙여 "어느 창구 말인지" 를 알린다 (run133 해설 2.5 · 2.7).

#### 새 말 — `CFastBH`

- combase 가 **연락 카드 손잡이 하나를 담아 두는 작은 상자** (`HeapAlloc(0x18)`). 핸들은 상자 `+0x10` 칸에 들어간다 (`CreateFromBindingHandle +0x6a CMPXCHG [CFastBH+0x10]`) `[정적]`.
- 람다가 상자 넷을 만들고 (`+0x14c` · `+0x18f` · `+0x1d2` · `+0x211`), 각 상자에 인터페이스 구조체를 하나씩 단다 (`+0x15d` · `+0x1a0` · `+0x1e3` · `+0x222`).

#### 넷의 이름표 (run 문서 1-2 표)

| 차례 | 창구 (PDB 이름) | UUID | 버전 | bind 하는 줄 |
|---|---|---|---|---|
| 1 | `ILocalObjectExporter` — 해결사 | `e60c73e6-88f9-11cf-9af1-0020af6e72f4` | **2.0** | `+0x33c CreateFromBindingString` |
| 2 | `IMachineActivatorControl` — 활성화 제어 | `c6f3ee72-ce7e-11d1-b71e-00c04fc3111a` | 1.0 | `+0x3ca CreateFromBindingHandle` |
| 3 | `IROT` — 실행 중 목록 | `b9e79e60-3d52-11ce-aaa1-00006901293f` | 0.2 | `+0x3e9` |
| 4 | `ISCM` — 클래스 관리 | `412f241e-c12a-11ce-abff-0020af6e7a17` | 0.2 | `+0x468` |

- (한국어 창구 이름은 run 문서 비유를 따른 별명이다.)
- 원 요청의 `[추측 — 공개 이름]` "`e60c73e6…` = `ILocalObjectExporter`" 는 **PDB 이름으로 닫혔다** `[정적]` — `.asm` `+0x15d` 주석이 `ILocalObjectExporter___RpcClientInterface` 다.
- 실패 갈래는 차례대로 줄 `0x300` · `0x306` · `0x30a` · `0x30e`.

#### 2 ~ 4 번은 어떻게 — 한 걸음씩

1. 1번 카드를 **복사** — `RpcBindingCopy` (`+0x23`).
2. 복사본의 첫인사를 **지움** — `RpcBindingUnbind` (`+0x3a`).
3. 다른 이름표로 **다시 첫인사** — `BindWithTimeout` (`+0x54`).
4. 핸들을 상자에 넣음 — `+0x6a CMPXCHG [CFastBH+0x10]`.

- 끝점 이름이 같아서, rpcrt4 `PickAssociation` 이 **이미 있는 연결을 다시 쓸 것** 으로 보인다. 봤던 줄은 `+0xa1 _wcsicmp` (끝점) · `+0xb4 / +0xb9` 상태 ≠ 2 · ≠ 5 · `+0xbe CMP [RDI+0x48],R13D` 뿐이다.
- → "**새 `NtAlpcConnectPort` 없이 같은 전화선에 첫인사 (종류 1) 가 세 번 더**" 는 `[추측]` 이다 (보안 칸 비교 줄을 이 함수에서 못 봄 → 동적 Q283 D2).

#### 하나라도 거절하면

- 넷 중 어느 첫인사든 fault 면 **그 자리에서** `0x8007NNNN` 으로 람다가 끝난다 (2.11 의 마지막 표 줄).
- 그래서 **넷을 한꺼번에 받아 줘야** `Connect` 까지 간다.

#### 실패를 기억하지 않는다

- 상자 넷은 람다를 부를 때마다 **새로 만든다** — 실패를 적어 두는 칸이 없다 `[정적]`.
- 람다를 건너뛰는 건 `GetConnection +0x34 / +0x56 TEST [_dwFlags],1` 이 켜져 있을 때뿐이다. 그 비트는 **성공 때만** 켠다 (람다 `+0xae4 OR [_dwFlags],R15D`).
- → **COM 을 부를 때마다 처음부터 다시 연결 · 첫인사** 를 한다 `[정적]`.
- 요청 쪽 로그의 "여섯 샘플이 6,837 ~ 6,995 번씩" 과 같은 모양이다 (에뮬레이터 로그라 `[관찰]` 아님).

비유: COM 담당 건물은 관리실에 전화가 안 되면, 수첩에 "안 됨" 이라고 적지 않는다. 다음에 COM 일이 생기면 **또 처음부터 전화를 건다.**

### 2.6 말투와 보안 — 보안은 연결 쪽에만 실린다 `[정적]`

run 문서 1-1 아래쪽 · 1-3.

#### 말투 — NDR 2.0 하나 (마스크 1 은 `[추측]`)

- 넷 다 인터페이스 구조체의 `TransferSyntax` = **NDR 2.0** (`8a885d04-1ceb-11c9-9fe8-08002b104860` v2.0), `Flags` 비트 25 (`0x2000000`) **꺼짐** · `InterpreterInfo` 0 `[정적]`.
- rpcrt4 `BaseBind +0x91 MOV EAX,[R8+0x58]` · `+0x95 BT EAX,0x19` · `+0x99 JNC → +0x3a4 MOV RDX,[R8+0x18]` — 이 비트가 꺼지면 문법 표 대신 **구조체 자신의 말투 하나** 를 쓴다 `[정적]`.
- 그래서 첫인사의 제안 마스크는 **`1` (NDR 2.0 만)** 일 것이다 `[추측: 짐작 — 마스크를 만드는 `Bind__4e330 +0x4e0` 까지 이 갈래로 이어 보지는 않음]`.
- 스텁도 `NdrClientCall2` 라 어차피 **NDR 2.0 전용** 이다.
- → **서버 흉내는 `+0x48 = 1` 로 답해야 한다.** NDR64 (`2`) 로 답하면 이 클라이언트는 못 푼다.
- run133 의 winnsi · EPM 은 마스크 **3** (두 말투) 이었다 (run133 해설 2.5). 이번 손님은 **말투가 하나뿐** 이다.

#### 보안 — 무엇이 어디로 가나

| 칸 | 값 | 줄 |
|---|---|---|
| 요구 서버 SID | **NetworkService `S-1-5-20`** | 람다 `+0x2ad LEA ECX,[RDI-0x2c]` (`0x44 − 0x2c = 0x18` = 24) · `+0x2b0 CreateWellKnownSid` |
| 보안 품질 (QOS) | Version 4 · Capabilities 1 · IdentityTracking 1 (동적) · ImpersonationType 3 | `+0x2c3` · `+0x2e0` · `+0x2e3` · `+0x2e6` |
| 인증 | AuthnLevel 6 · AuthnSvc 10 | `+0x2fe` · `+0x2f7` |

- 요구 서버 SID = 연락 카드의 "**이 사람과만 통화**" 칸 (run141 해설 2.4). 여기선 "NetworkService 신분인 상대여야 한다" 는 뜻이다.
- 이 값들은 **연결** (`NtAlpcConnectPort` 의 `RequiredServerSid` · `SecurityQos`) 쪽으로만 간다. **첫인사 메시지 `0x70` B 에는 보안 칸이 없다** (run133 1-2) `[정적]`.
- QOS 가 연결 객체로 옮겨 가는 줄은 rpcrt4 `RpcBindingCreateW` 안이라 안 읽었다 `[추측]`.
- 에뮬레이터 쪽 의미: 동기 연결에 요구 서버 SID 가 와도 받아 주면 된다 — 지금 이미 그렇다 (2.13).

### 2.7 ★ 첫 프로시저 = 0번 `Connect` — 무엇을 보내나 `[정적]`

run 문서 2-0 · 2-1.

#### 새 말 — 프로시저 번호, 처음 읽는 사람을 위해 다시

- **프로시저 번호** = 그 창구의 **메뉴 번호** (run129 해설 2.5). 첫인사가 끝나면 "창구 1, 메뉴 0번 해 주세요" 처럼 번호로 부탁한다.
- 본론 메시지 머리에서 서버는 `+0x3c` (메뉴 번호) · `+0x38` (슬롯 = 어느 창구) 만 보면 어느 부탁인지 안다 (run133 2-1).

#### 새 말 — 스텁 (`NdrClientCall2`)

- **스텁** = 함수 인자를 형식 문자열 (서식 설명서) 대로 **바이트로 줄 세워 보내고, 답을 다시 풀어 칸에 넣는** 대리 함수. combase `Connect` 는 몸이 거의 **`NdrClientCall2` 한 번** 이다 (`+0x17b`).
- `NdrClientCall2` = NDR 2.0 용 엔진 `[정적: 이름]`. 서버 쪽 짝은 `NdrServerCall2`.
- 비유: 부탁서 서식에 맞춰 칸을 채워 보내고, 돌아온 답장을 서식대로 오려 붙이는 **서류 담당**.

#### 자리

- combase `Connect` rva `0x12f318` (399 B) — `+0x16a LEA RDX,[lclor__MIDL_ProcFormatString+0x2]` (rva `0x2c26e2`) · `+0x174 LEA RCX,[ILocalObjectExporter_StubDesc]` (rva `0x274270`) · `+0x17b NdrClientCall2`.
- **프로시저 0**, 스택 `0x108` (핸들 + 32 칸), 파라미터 32 (반환 포함).
- 서버 `rpcss!_Connect` rva `0x76d80` (1887 B) — 서버 쪽 프로시저 문자열 0번이 클라이언트 것과 **바이트까지 같다** (결과서 `pefile`). 반환은 `error_status_t` (0 = 성공).

#### 보내는 칸 — 입력 6 (run 문서 2-1)

| 차례 | 칸 | 이 호출의 값 | 줄 |
|---|---|---|---|
| — (RCX) | 바인딩 핸들 (선 위에 안 실림) | 1번 첫인사 핸들 | 람다 `+0x78c` |
| 0 | 창 스테이션 · 데스크톱 글 | `_pwszWinstaDesktop` (예 `WinSta0\Default`), 핸들 단계 0 이면 **NULL** (run136) | `+0x742` |
| 1 | exe 경로 | `GetModuleFileNameW(NULL, buf, 0x207)` 결과 | `+0x751` |
| 2 | IPID 표 정보 (96 B 구조체) | **보통 NULL** — 개발자 라이선스가 있을 때만 채움 | `+0x77e` · 채움 `+0x499 ~ +0x53e` |
| 3 | 깃발 | 0 ~ 7, 보통 0 | `+0x82a` |
| 4 | 머신 | **`0x8664`** 고정 | `+0x823` |
| 9 | 예약 OID 요청 수 | **10** 고정 | `Connect +0x11b` |

- **새 말 — 예약 OID**: COM 관리실에서 **물건 번호 (OID) 를 미리 받아 두는** 칸으로 보인다 — 이름 `_OidsReserved` · 서버 `AllocateReservedIdsInternal(10, …)` `[정적: 이름]`, "물건 번호" 라는 뜻은 `[추측: 일반적으로 알려진 설명]`. 10개를 달라고 부탁한다.
- 서버 흉내는 **요청 본문을 안 읽어도 된다** — 머리의 메뉴 번호 · 슬롯만 보면 된다 (run 문서 2-1).

비유: "저 왔어요" 부탁서에 적는 것 — **내 주소** (건물\방), **내 이름** (exe 경로), 기계 종류 (`0x8664`), "물건 번호 10개 미리 주세요".

### 2.8 ★ 받는 칸 25 — 무엇이 돌아오나 `[정적]` (차례는 `[규칙]`)

run 문서 2-2 를 대표만 줄인 것. 칸 번호는 파라미터 번호다.

먼저 가름 (요청서가 정한 말):

- **(가)** 상수 · 규칙 — 어느 컴퓨터나 같은 값
- **(나)** 프로세스 · 연결마다 다른 값
- **(다)** 난수 · 식별자
- **(라)** 0 / NULL / 빈 것이어도 지나감

| 칸 | 무엇 → combase 가 넣는 곳 | 가름 | 0 · NULL 이면 | 최소 대답 (꼴만) |
|---|---|---|---|---|
| **5** | **접수 번호표** `_ph` (컨텍스트 핸들 20 B) | (다)+(나) | **불가** → `0x8000ffff` | 속성 0 + 0 아닌 GUID |
| 6 | 핑 주기 `giPingPeriod` | (가) rpcss 초기값 `0x168` = 360 | 검사 없음 | 360 |
| **7** | **로컬 해결사 연락처** → `_pdsaLocalResolver` (DUALSTRINGARRAY) | (나)/(다) | **포인터 NULL 불가** → `0x8000ffff` | 비-NULL, 내용 `TODO(VM)` |
| 8 | 로컬 MID `gLocalMid` | (다) | 검사 없음 | 0 아닌 상수 |
| 10 | 예약 OID 배열 → `_OidsReserved` (0x50 B) | (다) | 값 0 가능 | 열 칸 0 |
| **11** | 예약 OID 개수 | (다)/(나) | 0 가능 (라). **10 초과 금지** | **0** |
| 15 | 레거시 보안 글 | (가) 기본 NULL | **NULL 가능 (라)**. 있으면 2 글자 이상 | NULL |
| 16 / 17 | 인증 수준 / 가장 수준 | (가) 기본 2 · 2 | 가능 | 2 · 2 |
| 24 | 스레드 번호 | (나) `gNextThreadID++` (초기 1) | 가능 | 1, 2, 3 … |
| **25** | **rpcss PID** `gdwScmProcessID` | (나) | **불가** → `0x8000ffff` | 0 아닌 값 (bind 답 `+0x08` 과 같게 `[추측]`) |
| **26** | **프로세스 서명** `_ProcessSignature` | (다)+(나) | **불가** → `0x8000ffff` | 0 아닌 값 |
| 28 ~ 30 | SD 다섯 조각 크기 · 총 크기 · 덩어리 | (가) | 크기 합 0 이면 덩어리 NULL 가능 | 0 ×5 · 0 · NULL |
| 31 | 반환 상태 | 서버가 정함 | 0 = 성공 | **0** |

- 나머지 칸 (12 설정 비트 · 13 / 14 컨테이너 인증 · 18 ~ 23 서비스 목록 · 채널 훅 · 27 프로세스 GUID) 은 run 문서 2-2 표에 있다. 대부분 (가) 이고 비어도 지나간다.
- **새 말 — DUALSTRINGARRAY (로컬 해결사 연락처)**: 칸 7 의 타입 이름 (`[out] DUALSTRINGARRAY**`) `[정적]`. 이름으로 보면 "글자 묶음 두 벌" 꼴이다 `[추측: 이름에서]`. rpcss 가 자기 **듣는 주소** 를 적어 준다 (`_Connect +0x3bd CopyMyOrBindings`) `[정적]`. 뒤에서 `MakeSCMProxy` 가 이 글로 활성화 연결을 만드는 것으로 보인다 `[추측]`. 비유: 관리실의 **연락처 메모**.
- **새 말 — 프로세스 서명**: rpcss 가 이 손님 프로세스에 붙인 **64비트 값** (`CProcess+0x248`) `[정적]`. 뒤에서 `DelegateActivationToSCM +0x1ac` 가 읽는다. 비유: 관리실이 내 이름 옆에 찍어 준 **확인 도장 번호**.
- "**지어내는**" 칸은 서버 **자기 정체** (번호표 · PID · 서명 · GUID · MID · 스레드 번호) 뿐이다. 시스템 설정을 지어내는 칸은 없다 — 2 / 2 · 360 · 0 은 rpcss 코드의 기본값이다 `[정적]`.
- 응답 선 위 **진짜 바이트** (조건 길이 · 정렬 · 포인터 id) 는 안 풀었다 → 동적 Q283 D7.

### 2.9 ★ combase 의 답 검사 — 0 이면 안 되는 넷, 안 막는 둘 `[정적]`

run 문서 2-2 아래 · 3절 표 아래 두 줄.

#### 0 이면 안 되는 넷 — 한 걸음씩

`Connect` 가 상태 0 으로 돌아오고 `CheckStatus` 도 지나면, 람다가 차례로 본다:

1. `+0x8c6 CMP [_ph],R13` · `+0x8cd JNZ` — **접수 번호표가 NULL 이면** → `+0x8d4` 줄 `0x387`.
2. `+0x8de CMP [RBP-0x48],R13` · `+0x8e2 JZ +0xb37` — **해결사 연락처 포인터가 NULL 이면** → `+0xb3c` 줄 `0x388`.
3. `+0x8e8 CMP` · `+0x8ef JNZ` — **rpcss PID 가 0 이면** → `+0x8f6` 줄 `0x389`.
4. `+0x900 CMP` · `+0x907 JNZ` — **프로세스 서명이 0 이면** → `+0x90e` 줄 `0x38a`.

- 넷 다 결과는 **`0x8000ffff`** 이고, 그 전에 **텔레메트리 호출** 이 하나 붙는다 (`+0x8cf` · `+0x8f1` · `+0x909` · `+0xb37`, 레거시 글 갈래는 `+0x989`).
- **새 말 — 텔레메트리 호출**: `MicrosoftTelemetryAssertTriggeredNoArgs` — 이름으로 보면 "**있으면 안 되는 일이 일어났다**" 는 **기록을 남기는** 호출이다 `[추측: 이름에서]`. 반환을 바꾸지는 않는다.
- 비유: 관리실이 "네, 접수됐어요" 해 놓고 **번호표 · 직원 번호 · 도장 · 연락처 가운데 하나라도 빈칸** 으로 주면, COM 담당 건물은 "이상한데?" 하고 사고 일지에 한 줄 적은 뒤 포기한다.

#### 그 밖의 검사

- 연락처 포인터가 있으면 `+0x925 HeapAlloc(0x10)` · `+0x954 Attach` 로 담는다. 여기서 실패하면 `+0x962` 줄 `0x38e` → `0x8007000e`.
- 레거시 보안 글: `+0x970 TEST RDX,RDX` · `+0x973 JZ` — **NULL 이면 건너뜀**. 있으면 `+0x975 ~ +0x983` 글자 수 **2 미만 → 줄 `0x398` → `0x8000ffff`**.
- `+0x9b3 InitializeSystemSecurityDescriptors` — SD 다섯 조각을 크기마다 `LocalAlloc` + `memcpy`. 검사는 `LocalAlloc` NULL 뿐 (실패면 줄 `0x39c`).

#### 안 막는 둘 — 그래서 에뮬레이터가 지켜야 하는 것

| 무엇 | combase 가 하는 일 | 어기면 |
|---|---|---|
| **예약 OID 개수** (칸 11) | `+0xa5b memset(_OidsReserved, 0, 0x50)` 뒤 `+0xa7e memcpy(…, 개수×8)` — **상한 검사 없음** | 10 을 넘으면 `_OidsReserved` (0x50 B = 8 B × 10) 를 **넘쳐 쓴다** |
| **SD 크기 합** (칸 28 ~ 30) | 조각 크기마다 복사 — 합 ≤ 총 크기 검사 **없음** | 합이 크면 덩어리를 **넘쳐 읽는다** |

- 곧 combase 는 "**서버가 착하다**" 고 믿는 칸이 있다. 진짜 rpcss 는 10 을 넘기지 않으니 (입력이 10) 문제없지만, 흉내 서버는 **스스로 지켜야** 한다.

#### 성공 도장 — `gGotSecurityData`

- 위 검사를 **다 지나고**, SD 만들기도 성공한 **다음에야**:
  1. `+0xa83 MOV [gGotSecurityData],R15D` (= 1, `+0x768 MOV R15D,1`)
  2. `+0xae4 OR [_dwFlags],R15D` (2.5 의 "람다 건너뛰기" 비트)
  3. `+0xb30 XOR EAX,EAX` — 0 반환.
- `gGotSecurityData` 를 뒤에서 읽는 쪽 (이름만): `CoInitializeSecurity +0xa6 / +0x48e` · `SetExplicitWinstaDesktopString +0xa` (켜져 있으면 "너무 늦음") · `CoAllowUnmarshalerCLSID` · `IsMethodInvocationAllowed` · `IsObjectCreationAllowed` 등. 지우는 곳은 `CRpcResolver::Cleanup +0x2c`.
- 비유: 모든 서류가 맞아야 수첩에 "**관리실 접수 완료**" 도장을 찍는다. 이 도장이 있으면 다음부터는 전화를 다시 안 건다.

### 2.10 진짜 rpcss 쪽 — `0x57` 과 `5`, 그리고 run136 `Connect(NULL)` 의 끝 `[정적]`

run 문서 2-3. 받는 쪽 `rpcss!_Connect` 는 **요청 · 응답 꼴을 정하는 줄만** 봤다.

- **손님 확인**: `+0x161 CheckLocalCall` (ncalrpc 아니면) · `+0x177 RetrievePIDForLocalClient` 실패 → **상태 5** (`+0x168` / `+0x180 JZ → +0x73a MOV EAX,5`).
- **입력 확인**: **winsta 글 포인터가 NULL** (`+0x186 CMP [RBP-0x38],RDX` · `+0x18a JZ → +0x733`) · 출력 포인터 NULL (`+0x190 ~ +0x1a8`) · 깃발 `& 0xfffffff8` · 깃발 4 인데 `+0x27d IsGetDesktopAppXComRootHandlePresent` 거짓 → **상태 `0x57`** (`+0x733 MOV EAX,0x57`).
- exe 경로는 NULL 검사가 없다 (결과서).

#### 그래서 — run136 의 남은 끝이 닫혔다

1. run136: 창 스테이션 **핸들 단계** 에서 0 이 나오면 combase 는 "0 + 글 NULL" 로 **`Connect(NULL)`** 에 간다 `[정적]` (run136 해설 2.8).
2. 이번: 진짜 rpcss 는 **winsta 글이 NULL 이면 상태 `0x57`** 로 거절한다 (`_Connect +0x186` → `+0x733`).
3. combase `CheckStatus(0x57)` = `0x80070057` → `CoInitializeSecurity` = **`0x80070057`**.
4. → run136 의 "핸들 0 길" 은 진짜 Windows 에선 **`0x80070057` 로 끝난다** `[정적]`.

비유: 주소를 안 적은 "저 왔어요" 부탁서는 관리실이 "**서류가 잘못됐어요**" (`0x57`) 로 돌려보낸다. run136 때는 "주소 없이 그냥 전화한다" 까지만 알았는데, 그 전화의 결말이 이것이다.

### 2.11 ★ 실패 쪽 — 정직한 실패 vs 끝없이 다시 거는 금지 꼴 `[정적]`

run 문서 3절. 요청서가 가장 알고 싶어 한 곳이다.

#### 새 말 — `RetryRPC` · `CheckStatus`

- 둘 다 combase 의 프로시저 스텁들이 **같이 쓰는 도우미** 다 (`CallTheResolver +0x117 / +0x122` 도 같은 둘) `[정적]`.
- **`RetryRPC`** (rva `0x3790c`) = "**다시 걸어 볼까?**" 를 정하는 함수. 1 이면 다시, 0 이면 그만.
- **`CheckStatus`** (rva `0x37958`) = RPC 상태를 **HRESULT 로 바꾸는** 함수. 규칙은 run136 해설 2.6 의 것과 같다: 0 → 0, 양수 N → `0x80070000 | (N & 0xffff)`, 음수 → 그대로.

#### `RetryRPC` 안 — 한 걸음씩

1. `+0x4 MOV EDX,0x6bb` · `+0x9 CMP` — 상태가 **`0x6bb`** 인가? (이름은 `[이름 추측: SERVER_TOO_BUSY]`)
2. 아니면 → **0** (그만).
3. 맞으면 `+0xb JZ +0x21` → 떨어진 범위로 가서 `+0x21fa8e MOV ECX,0x64` · `+0x21fa93 Sleep(100)` 뒤 **1** (`+0x21fa8e ~ +0x21fa9f`).
4. 람다로 돌아와 `+0x897 TEST` · **`+0x899 JNZ +0x6b4`** — 1 이면 `+0x6b4` 로 되돌아가 목록 셋 · 레거시 글을 비우고 **`Connect` 를 다시** 보낸다.
5. **횟수를 세는 칸이 없다.** 그래서 서버가 계속 `0x6bb` 를 주면 **100 ms 마다 영원히** 다시 건다.

#### 받은 것마다 (run 문서 3절 표 줄임)

| 받은 것 | 다시 거나 | 결과 HRESULT |
|---|---|---|
| 상태 0 | 아니오 | 2.9 검사를 지나면 0 |
| **상태 `0x6bb`** | **예 — 한도 없음** (`Sleep(100)` 마다) | 없음 (끝없이) |
| 그 밖 양수 N (`0x6b5` · `0x6ba` · `0x6be` · `0x6bf` · `0x57` · `0xe` …) | 아니오 | `0x8007NNNN` → 람다 → `GetConnection` → `CoInitializeSecurity +0x486 JS +0x89c` → 같은 값 |
| 상태 5 | 아니오 | `0x80070005`, 단 `GetConnection +0xc5 CMP EAX,0x80070005` · `+0xca JZ` 의 **따로 갈래** (`+0x1bc84c`, 안 읽음 — **Q257b**) |
| 음수 | 아니오 | 그대로 |
| **bind 단계 fault** (넷 중 어느 것이든) | **아니오 — `RetryRPC` · `CheckStatus` 에 안 닿음** | `CreateFromBindingString +0x16b MOVZX EBX,DI` · `+0x16e OR EBX,0x80070000` 이 같은 규칙. **N = `0x6b5` 면 `0x800706b5`** |
| 상태 0 인데 넷 중 0 | 아니오 | `0x8000ffff` + 텔레메트리 (2.9) |
| 암묵 길에서 `0x80070057` | 아니오 | `InitializeSecurity +0x90` 이 **`0x80040153`** (`[이름 추측]`) 로 바꿈 (2.12) |

#### 새 말 — 정직한 실패 · 끝없이 다시 거는 금지 꼴

- **정직한 실패** = 흉내 서버가 모르는 부탁에 "**못 해요**" 를 분명히 답해서, 부른 쪽이 그걸 실패 HRESULT 로 받아 **한 번에 끝내고 다음으로 넘어가게** 하는 대답. 에뮬레이터 정책의 말이다 (run 문서 "목적").
- **금지 꼴** = 흉내 서버가 이렇게 대답하면 부른 쪽이 **영영 못 빠져나오거나 메모리를 망가뜨리는** 꼴 (run148 해설 2.9 와 같은 뜻). 이번 것 가운데 가장 무서운 것이 **`0x6bb` — 끝없이 다시 걸기** 다.
- 둘의 차이는 **숫자 하나** 다. 같은 "못 해요" 라도:
  - `0x6ba` · `0x6b5` 처럼 말하면 → `CheckStatus` 가 HRESULT 로 바꿔 **한 번에 끝**.
  - `0x6bb` 로 말하면 → `RetryRPC` 가 "아, 바쁘구나" 하고 **0.1초 뒤 또** — 영원히.

#### 왜 지금 에뮬레이터 대답이 안전한가 — 단계로

1. 지금 에뮬레이터는 모르는 인터페이스의 **첫인사** 에 fault `0x6b5` 를 준다 (`lrpc.cpp:74 ~ :77`, `:21`).
2. 첫인사 단계 fault 는 **`RetryRPC` 를 거치지 않는다** — `CreateFromBindingString +0x16b ~ +0x16e` 이 바로 `0x800706b5` 로 바꾸고, 람다가 줄 `0x300` 으로 끝난다.
3. → 다시 걸지 않고 `CoInitializeSecurity` = `0x800706b5` **한 번** 으로 끝난다 = **정직한 실패** `[정적]`.
4. 요청 쪽 로그 "한 API 안 거절 1" 과 같은 모양이다 (에뮬레이터 로그).

#### 왜 `0x6bb` 는 꼭 피해야 하나 — 단계로

1. fault 응답으로 상태를 주면, 클라이언트 `SanitizeErrorCodeFromServer` 는 **0 · 치명 14종만** `0x6c0` 으로 바꾸고 그 밖은 그대로 넘긴다 (run133 1-3).
2. → **`0x6bb` 도 그대로 `RetryRPC` 에 닿는다.** 금지 꼴이 실제로 걸린다.
3. 거꾸로, 전송 쪽 오류 (ALPC → RPC 표, run133 2-5) 에는 `0x6bb` 로 가는 줄이 **없다**. 곧 `0x6bb` 는 **서버가 직접 줄 때뿐** 이다.
4. → 흉내 서버만 조심하면 된다.

#### 그럼 무엇을 쓰나

- **정직한 실패로 쓸 수 있는 값**: `0x6bb` 를 뺀 아무 양수.
- 가장 자연스러운 것은 **`0x6ba`** — rpcss 가 안 떠 있을 때와 같은 HRESULT `0x800706ba` 가 된다 (결과서 권고).
- **피할 것**: `0x6bb` (끝없음) · `5` (따로 갈래, Q257b) · "상태 0 인데 빈 응답" (`0x8000ffff` 로 끝나긴 하지만 텔레메트리 호출).

비유: 관리실 흉내를 내는 대리 직원이 "**그 일은 안 해요**" 라고 하면 손님은 바로 수화기를 내려놓는다. 그런데 "**지금 바빠요**" 라고 하면, 손님 전화기는 **자동 재다이얼** 이 켜져서 0.1초마다 다시 건다 — 그리고 대리 직원은 늘 같은 말을 하니 영원히 끝나지 않는다.

### 2.12 그다음 — 같은 번호로 무엇이 또 가나 `[정적]`

run 문서 4절. 요청서 물음 3 — 지도만 (이름 · 번호 · 줄).

#### `CoInitializeSecurity` — `SetAppID` 하나만 더

- `+0xa6 / +0xac` (`gGotSecurityData == 0`) → `+0x47d GetConnection` → `+0x486 JS +0x89c` (실패면 `+0x8a9` 같은 값) / 성공이면 `+0x48c ~ +0x494 JNZ +0xb2` 로 본 줄기에 합류.
- 이 번호로 더 가는 것은 **`+0x3f6 ~ +0x43c` 한 곳**: `g_AppId` ≠ `GUID_NULL` 이면 `+0x420 I_RpcSetDCOMAppId` (rpcrt4, 프로세스 안) → **`+0x43c SetAppID`** = 메뉴 **1**.
- 보통 exe 는 AppID 가 없어 `GUID_NULL` 일 것이다 `[추측]`.

#### `CoInitializeEx` — 이 번호로 안 간다

- 지도 `_map/CoInitializeEx_*` (깊이 10, 함수 514) 에 `GetConnection` · `Connect` · `SetAppID` 가 **없다**.
- 이 번호로 닿는 것은 되돌리기 길의 `Disconnect` (메뉴 10) · `ServerFreeOXIDAndOIDs` (8) 뿐이다 (중간 고리는 이름 `Cleanup` 이 겹쳐 `[추측]`).
- → **`CoInitializeEx` 만으로는 `Connect` 가 안 나간다.** 다만 간접 호출 33곳은 못 봤다 `[공백]`.

#### 새 말 — 암묵 호출

- 프로그램이 `CoInitializeSecurity` 를 **직접 안 불러도**, combase 가 필요할 때 **대신 불러 주는** 것. run 문서의 말이다.
- 비유: 손님이 관리실 접수를 깜빡하고 물건부터 주문하면, COM 담당 건물이 **알아서 먼저 접수 전화를 넣어 준다.**

#### 첫 COM 사용 — in-proc `CoCreateInstance` 이어도 전화가 간다

1. `CoCreateInstance +0xd1 → CoCreateInstanceExCopy +0x162 → DoCreateInstance +0x1d7 → ICoCreateInstanceExWorker +0xb52 → GetActivationPropertiesIn +0x1fa` (앞 다섯 고리는 `_map/CoCreateInstance_edges.tsv` 줄).
2. → `CoGetDefaultContext +0x3f → InitChannelIfNecessary +0x119 → CComApartment::InitRemoting` (`.asm`).
3. `InitRemoting +0xb6 InitializeSecurity` → `+0x7c CoInitializeSecurity` **암묵 호출** (`gpsaSecurity` 가 NULL 일 때만, `+0x21`) → **첫인사 넷 + `Connect`** (위 길 전부).
4. 이어 `InitRemoting +0x29d CallTheResolver` → `+0x106 NdrClientCall2` = **메뉴 7** (서버 `_ServerAllocateOIDs`) → `+0x1b7 RegisterOXIDAndOIDs` → **메뉴 6** (`ServerRegisterOXID`).

- 그래서 `CoInitializeSecurity` 를 직접 안 부른 프로세스도 **`CoCreateInstance` 한 번에 같은 `0x800706b5` 를 만난다** `[정적]`.
- 암묵 길에선 `0x80070057` 이 `InitializeSecurity +0x90` 에서 **`0x80040153`** (`[이름 추측]`) 로 바뀐다. 그 뒤에도 `+0x95 UninitializeSecurity` 를 부른다 (다른 음수와 같은 줄로 감).

#### `CoGetObject` · out-of-proc

- **`CoGetObject`** — ole32 쪽이라 안 열었다 `[공백]`. 실행 중 목록 쪽 후보 첫 부름은 `IrotGetObject` (IROT 3) · `IMgotGetObject` (IROT 9).
- **out-of-proc · 서비스** — `DelegateActivationToSCM +0x156 BindToSCMProxy` → `+0x17 GetConnection` · `+0x49 MakeSCMProxy` · `+0x81 MakeCloakedSCMProxy`. 이건 `ILocalObjectExporter` 가 아니라 **ORPC 프록시** 로 rpcss 활성화 쪽에 간다. "클래스 · 서비스 없음" HRESULT 길은 안 읽었다 `[공백]`.

#### `GetConnection` 을 부르는 곳 — 46

- `_refs/0xa14d0.tsv` 48줄 = **call 46** + 데이터 2.
- `CoInitializeSecurity +0x47d` · 해결사 쪽 8 · `BindToSCMProxy +0x17` · IROT 11 (`Irot*` · `IMgot*`) · ISCM · IMachineActivatorControl 25 (`NotifyStarted` · `GetThreadID` · `IsObjectCreationAllowed` · `RetireServer` …). 목록 전부는 결과서 4-d.
- 곧 combase 가 관리실에 하는 **거의 모든 부탁** 이 먼저 `GetConnection` 을 지난다 → 성공 도장이 없으면 **그때마다 처음부터** (2.5).

#### 해결사 창구의 메뉴판 — 12개 (run 문서 4-e 줄임)

| 메뉴 | combase 스텁 | rpcss 서버 |
|---|---|---|
| **0** | **`Connect`** | `_Connect` |
| 1 | `SetAppID` | `_SetAppID` |
| 6 | `ServerRegisterOXID` | `_ServerAllocateOXIDAndOIDs` |
| 7 | `CComApartment::CallTheResolver` | `_ServerAllocateOIDs` |
| 8 | `ServerFreeOXIDAndOIDs` | `_ServerFreeOXIDAndOIDs` |
| 10 | `Disconnect` | `_Disconnect` |

- 나머지 (2 · 3 · 4 · 5 · 9 · 11) 는 run 문서 4-e. 클라이언트 · 서버 프로시저 문자열을 차례로 풀어 매겼고, 두 쪽 파라미터 수 · 속성 · 스택 칸 열두 개가 **모두 같다** (결과서).

비유: "저 왔어요" 가 끝나면, 첫 COM 물건을 만들 때 바로 **"물건 번호 주세요" (메뉴 7)** → **"우리 가게 등록해 주세요" (메뉴 6)** 가 같은 전화로 이어진다.

### 2.13 ★ PE_emulator 판정 — 맞음 · 틀림 · 최소 대답 (run 문서 "PE_emulator 쪽")

flow-writer 는 `../PE_emulator` `lrpc.cpp` (359줄) · `epm.cpp` (47줄) 를 **줄 번호만** 훑었다 (읽기만). 고칠지는 사용자 · PE_emulator 결정이다.

| 우리 지금 | 진짜 (정적) | 판정 |
|---|---|---|
| `lrpc.cpp:74 ~ :77` 모르는 인터페이스 첫인사 → fault `0x6b5` (`:21`). 창구 정의 `epm.cpp:33 ~ :44` 는 EPM 하나 | 진짜 combase 는 같은 번호에 **EPM 이 아닌 넷** 을 첫인사 | **맞음 — 안전한 정직한 실패** (재시도 없음, 2.11) |
| `lrpc.cpp:79 ~ :80` 말투 고르기 + 창구 마스크 1 (`epm.cpp:42`) | 이 손님은 NDR 2.0 전용 (2.6) | **맞음.** 새 창구도 마스크 `1` |
| `lrpc.cpp:102 ~ :103` 메뉴를 **번호 (`opnum`) 만으로** 찾음 | 같은 번호에 창구 다섯 (EPM + 넷) 이면 번호가 **겹친다**: EPM 8 = `ept_map_auth_async` vs 해결사 8 = `ServerFreeOXIDAndOIDs` | **틀려질 예정.** (인터페이스 UUID, 번호) 로 찾아야 함 (`:100` 에서 이미 `uuid` 를 꺼내 둠 — 비교에 안 씀) |
| `lrpc.cpp:83 ~ :84` 슬롯별 바인딩 | 한 연결에 첫인사 넷, 슬롯 서로 다름 `[추측: 같은 연결 재사용 — Q283 D2]` | 맞음 |
| `lrpc.cpp:186` 동기 연결을 받음 | 이 길은 동기 첫인사 · 동기 스텁. 요구 서버 SID 가 와도 받음 | 맞음 |
| `epm.cpp:41` 메뉴 8 하나 | 이 번호의 다른 창구 몸은 아직 없음 | 짓는 중 |

#### 성공 쪽을 만들 때 — 최소 대답 (run 문서 그대로, 짧게)

1. **연결**: 동기 연결을 받음 (이미 됨).
2. **첫인사 넷**: 각각 종류 1 · `+0x48 = 1` · `+0x08` = 서버 PID (0 아닌 값) · 슬롯 = 제안 그대로. **넷 중 하나라도 거절하면 `0x800706b5` 로 끝** — 넷을 한꺼번에 올려야 한다.
3. **`Connect` 응답 최소 칸** (2.8 표): 번호표 (0 아닌 GUID) · 핑 360 · 연락처 비-NULL · MID ≠ 0 · OID 개수 **0** · 레거시 NULL · 인증 / 가장 2 / 2 · 목록 셋 0 / NULL · 스레드 번호 · **PID ≠ 0** · **서명 ≠ 0** · GUID · SD 0 · **상태 0**. `TODO(VM)` 로 둘 것: 연락처 내용 · 설정 비트 · 서비스 목록 · SD 조각 (진짜 값은 Q283 D5).
4. 그 뒤 메뉴 (7 · 6 · 10 · 1 …) 는 몸을 모르면 fault — 상태는 **`0x6ba`** 를 권한다.

#### 금지 꼴 여덟 (run 문서 그대로)

1. **fault 상태 `0x6bb`** — 100 ms 마다 끝없이 다시 건다.
2. **응답 없음** — 동기 호출이 영영 안 돌아온다 (호출 시간제한 `0xffffffff` 를 무한으로 짐작 `[추측]`).
3. **상태 0 인데 번호표 · 연락처 · PID · 서명 가운데 하나가 0 / NULL** — `0x8000ffff` + 텔레메트리. 번호표 20 B 를 다 0 으로 주면 NULL 핸들.
4. **예약 OID 개수 > 10** — 넘쳐 쓰기.
5. **SD 크기 합 > 총 크기, 또는 크기가 있는데 덩어리 NULL** — 넘쳐 읽기.
6. **첫인사 답 말투 `2` (NDR64)** — 이 손님은 NDR 2.0 전용.
7. **상태 `5`** — 따로 갈래 (Q257b, 안 읽음).
8. **첫인사 답 마스크 `3`** — "비트 하나" 검사에 걸려 `0x6c0` (run133 1-3).

#### "그다음 벽" 한 줄

- `Connect` 가 성공하면, **첫 COM 사용 (`CoCreateInstance`, in-proc 이어도)** 에서 `InitRemoting → CallTheResolver` 가 같은 연결로 **메뉴 7 → 6** 을 바로 보낸다 (2.12).
- 그 몸을 짓기 전에는 **7번에서 `0x6ba` 류로 끝내는 것** 이 안전하다. 그러면 `CoInitializeSecurity` 는 성공하고, 첫 COM 사용이 `0x800706ba` 류로 실패한다.
- 조심: 지금의 `CoInitializeSecurity = 0x800706b5` 와 **증상이 달라져** 로그 비교가 달라 보일 수 있다.
- 그다음은 활성화 (`DelegateActivationToSCM` → ORPC) · 실행 중 목록 (IROT 3 · 9).

비유: 지금 대리 직원은 관리실 창구 넷에 대해 "**그런 창구 없어요**" 라고 정직하게 답하고 있다 — 손님은 한 번에 포기하니 안전하다. 진짜처럼 받아 주려면 **창구 넷을 한꺼번에 열고**, "저 왔어요" 답장의 **빈칸 넷을 꼭 채우고**, **물건 번호 10개를 넘겨 주지 말고**, 모르는 메뉴엔 "바빠요" 대신 "**관리실 쉬는 날이에요**" (`0x6ba`) 라고 해야 한다. 그리고 메뉴판을 **창구 이름 + 메뉴 번호** 로 찾아야 한다 — 114 의 8번과 해결사의 8번은 다른 메뉴다.

### 2.14 앞 run 고쳐 읽기 — 닫힘 둘

틀린 것은 없고 `[추측]` 이 정적으로 닫히거나 끝이 이어진 것이라, run 문서는 "정정" 이 아니라 "**고쳐 읽기**" 로 각 문서 맨 위에 한 줄씩 넣었다. 해설서에도 맨 위에 한 줄씩 넣었다.

| 앞 run | 그때 | 이번에 |
|---|---|---|
| **run136** (a) | 2-1 끝 "`Connect` 안 (RPCSS 쪽 RPC · epmapper 길 — run129 · run133 의 rpcrt4 몫과 이어질 것 `[추측]`)" | **`[정적]` 으로 닫힘** — 바인딩 글 `ncalrpc:[epmapper,…]` 로 `ept_map` 없이 곧장, 첫인사 넷 뒤 메뉴 0 (2.3 · 2.5 · 2.7) |
| **run136** (b) | "핸들 단계 0 → 0 + 글 NULL → `Connect(NULL)`" — 끝은 모름 | **진짜 끝**: rpcss `_Connect +0x186` 이 winsta 글 NULL 이면 상태 `0x57` → `CoInitializeSecurity` = `0x80070057` (2.10). combase 쪽 줄은 그대로 맞다 |
| **run137** | "같은 비동기 틀을 RPCSS (`CoInitializeSecurity`) 도 쓴다 (원 요청 말) `[추측: RPCSS 쪽 연결 객체가 같은 썽크를 쓰는지는 안 읽음]`" | combase 의 RPCSS 길 (첫인사 넷 + `Connect`) 은 **동기** `[정적]` — `BindWithTimeout +0xc RpcBindingBind(NULL, …)`, 스텁 `NdrClientCall2`. 비동기 완료 포트 틀은 이 길에서 안 쓴다 (2.3). 연결 깃발 자체는 rpcrt4 안이라 안 읽음 |

### 2.15 flow-writer 대조 — 결과서와 다르게 적은 곳 (짧게)

run 문서는 결과서 오프셋을 `.asm` 과 하나하나 맞췄다. **어긋난 곳 넷** 은 run 문서 값을 쓴다 (이 글도 그 값을 썼다). 뜻은 모두 같고, 대부분 "**call 줄 / 인자 줄 / 점프 줄을 이웃 줄로 적은 것**" 이다.

1. 인터페이스 구조체를 다는 줄은 `+0x14c` (그건 `HeapAlloc` call) 가 아니라 **`+0x15d LEA RCX,[0x1802fcd00]`**.
2. `RpcBindingUnbind` call 은 `+0x35` (인자 줄) 가 아니라 **`+0x3a`** (ret `+0x41`).
3. 서명 == 0 갈래 번호를 넣는 줄은 `+0x913` (그건 `JMP`) 이 아니라 **`+0x90e MOV EDX,0x38a`**.
4. 레거시 글을 넣는 줄 — 전역 주소는 **`+0x998 LEA RCX,[gLegacySecurity]`**, `+0x9a3` 은 unique_ptr 바꾸기 공용 도우미 call (rva `0x5e9f0`, 별칭 무리 — `pdb_aliases.py` 11개 · Ghidra 주석 14개, 수가 다름).

보탬 다섯: (5) 첫 첫인사 call `CreateFromBindingString +0xe5` (ret `+0xea`), (6) `rpcss!_Connect` SD 조각 범위는 `+0x3dd ~ +0x4aa` 로 읽음, (7) `CoCreateInstance` 앞 다섯 고리는 지도 줄로만 맞춤, (8) `InitializeSecurity` 는 `0x80040153` 로 바꾼 뒤에도 `UninitializeSecurity` 를 부름, (9) `GetConnection` 부르는 곳 46 = 48줄 − 데이터 2.

- PE_emulator 줄 번호: 결과서의 "`lrpc.cpp:99 ~ :103`" 는 `:99` 가 슬롯 못 찾음 fault 이고, **번호 비교는 `:102 ~ :103`** 이다.

### 2.16 정리 — 비유로 한 번 더

- COM 담당 건물 (combase) 은 COM 일을 하려면 먼저 동네 **COM 관리실 (RPCSS)** — COM 의 **교환대** — 에 접수 전화를 넣어야 한다.
- 관리실은 **114 와 같은 사무실, 같은 대표 번호 (`epmapper`)** 에 앉아 있다. 수첩에 그 번호가 적혀 있으니, 114 에 "관리실 번호요" 하고 묻지 않는다 (`ept_map` 없음).
- 전화가 이어지면 관리실 **창구 넷** (해결사 · 활성화 제어 · 실행 중 목록 · 클래스 관리) 에 차례로 첫인사를 한다. 말투는 **NDR 2.0 하나**. 한 창구라도 "없어요" 하면 바로 끊는다 — 그리고 수첩엔 아무것도 안 적으니, **다음 COM 일 때 또 처음부터** 건다.
- 넷 다 받아 주면 해결사 창구 메뉴 0번 **"저 왔어요" (`Connect`)** — 내 주소 · 내 이름을 주고, 25칸 답장을 받는다.
- 답장에서 **번호표 · 연락처 · 직원 번호 · 도장 번호** 가 하나라도 비면 "이상한데?" (`0x8000ffff`). 물건 번호 개수와 SD 크기는 **믿고** 그대로 베낀다 — 흉내 서버가 지켜야 한다.
- 다 맞으면 수첩에 **"접수 완료" 도장** (`gGotSecurityData = 1`). 그다음 첫 물건을 만들 때 **"물건 번호 주세요" (7) → "등록해 주세요" (6)** 가 이어진다.
- 실패 쪽: "**바빠요**" (`0x6bb`) 만 **자동 재다이얼** (0.1초마다, 끝없이). 그 밖의 "못 해요" 는 한 번에 받아들인다. 그래서 지금 에뮬레이터의 "그런 창구 없어요" (`0x6b5`) 는 **정직한 실패** 라 안전하고, 모르는 메뉴엔 "관리실 쉬는 날" (`0x6ba`) 이 권하는 답이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show d4f1ca6 -- runs/03_sprint/flow.md`)

**그림은 그대로다.** 이번 길은 test.exe 초기화 밖이고, 받는 쪽은 다른 프로세스 (svchost) 다. 바뀐 곳은 글뿐이다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (combase · kernelbase · rpcrt4 · RpcEpMap 줄) | 앞 run 까지 | run149 가 읽은 것 한 마디씩 (RpcEpMap: "`InitializeService +0x23 UseProtseqIfNecessary(16)` — rpcss 와 같은 `epmapper` 표 칸") |
| 기준 — **새 rpcss 줄** | 없음 | `rpcss.pdb` `316298244AA2A8A6A520F13E7355EC211`, 처음 analyze (약 1분), depth 0 export 18개 · refs 11 |
| 근거 줄 | — | "(run149) 정적 바퀴 … 사진 값 대조 없음 — test.exe 는 combase · rpcrt4 를 안 올린다 · 요청 쪽 값은 에뮬레이터 로그라 `[관찰]` 로 안 씀" |
| 3장 "창 스테이션 · 데스크톱 이름 묻기" 절 (run136) | 끝 `+0x82e Connect` — 안은 안 읽음, 남은 것 Q257 | 한 줄 보탬 "(run149) **`Connect` 안이 열렸다** `[정적]` … `Connect(NULL)` 길은 **`0x57` → `0x80070057`**", 남은 것 줄에 "① 은 Q257a → run149, ② · ③ 은 Q257b" |
| 3장 새 절 (run136 절 바로 뒤) | 없음 | **"COM 이 RPCSS 에 거는 첫 전화"** — 비유 · 나무 한 장 (2.1) + `[정적]` 넷 (첫인사 넷 · 한 번호 / `Connect` = 메뉴 0 · 0 이면 안 되는 칸 넷 / 실패 쪽 / 그다음) |
| 3장 "epmapper 가 `ept_map` 에 답을 고르고 채우는 법" 절 (run145) | — | 한 줄 보탬 "(run149) **같은 `epmapper` 번호를 rpcss 도 연다** `[정적]` … (UUID, 번호) 로 찾아야 한다" |
| 4장 run136 정적 줄 (Q257) | 1 · 2 · 3 모두 열림, 1 의 이음은 `[추측]` | "Q257a / Q257b 로 나눔: **1 → Q257a (해결됨, 정적 → run149)**", 1 의 `[추측]` → "run149 `[정적]`" |
| 4장 새 줄 | — | **Q257a 닫힘 줄** + run149 줄 셋 (동적 **Q283** · 정적 **Q284** · PE_emulator) |
| 5장 | — | run149 두 줄 |

- 짐 지도 (`baggage.md`) 는 안 바뀐다 — **새 짐 칸 없음** (이번 길은 초기화 밖).

태그 변화만 뽑으면:

- run136 "`Connect` → epmapper 이음" — `[추측]` → **`[정적]`** (바인딩 글 · import 목록)
- 원 요청 "`e60c73e6…` = `ILocalObjectExporter`" — `[추측 — 공개 이름]` → **`[정적]`** (PDB 이름)
- run136 "`Connect(NULL)` 의 끝" — 모름 → **`[정적]`** `0x57` → `0x80070057`
- run137 "RPCSS 도 같은 비동기 틀" — `[추측]` → **동기 `[정적]`**
- 새로 남은 `[추측]`: bind 마스크 1 · 첫인사 2 ~ 4 가 같은 연결 재사용 · 응답 없음 = 영원히

### 큰 그림에서 어떻게 이어지나

- COM 보안 쪽 옆길을 시간 순서로 늘어놓으면:
  - run131 — COM 을 STA 로 열 때 창 클래스 등록
  - run136 — `CoInitializeSecurity` 가 주소 글 (창 스테이션 · 데스크톱) 을 만듦
  - **run149 — 그 주소를 들고 COM 관리실에 거는 첫 전화: 번호 · 첫인사 넷 · "저 왔어요" · 답 검사 · 실패 처리** ← 이번
  - 다음 — 첫 COM 사용의 메뉴 7 → 6 (Q284 1)
- 그리고 RPC 쪽 옆길 (run129 · run133 · run137 · run141 · run145) 과 **한 전화선** 에서 만난다. run145 는 그 전화선의 114 창구, 이번은 같은 전화선의 COM 관리실 창구다.
- 에뮬레이터 쪽에서 보면 길이 셋으로 정리된다:
  - **지금**: 첫인사에서 "없어요" — 안전한 정직한 실패. 그대로 둬도 된다.
  - **성공시키려면**: 창구 넷 + `Connect` 몸을 **한꺼번에**, 그리고 메뉴판을 (UUID, 번호) 로.
  - **그다음 벽**: 메뉴 7 → 6. 그 전까지는 7 에서 `0x6ba`.
- run136 의 "핸들 0 길" 이 진짜에선 `0x80070057` 로 끝난다는 게 나와서, 그 길로 돌아가는 건 진짜 Windows 를 흉내 내는 게 아니라는 것도 더 분명해졌다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q283** (동적, VM — 미룸)

결과서 "동적으로 확인할 것" D1 ~ D11. **D2 · D3 · D5 · D7 필수**, D4 · D6 값 읽기, **D8 ~ D11 선택**. 멈춤점은 test.exe 가 아니라 **COM 을 쓰는 프로세스** (`schtasks.exe` · `taskkill.exe` 같은 자식이나 COM 보안을 여는 GUI 프로세스) 조건, 서버 쪽 (D8 · D9) 은 svchost (rpcss) 조건이다. 고르기는 사용자 결정 — Q256 · Q271 · Q280 의 RPC 묶음과 같은 세션에 얹을 꼴.

| 무엇을 | 왜 이번에 못 알았나 |
|---|---|
| **D2** 한 `GetConnection` 안에서 연결 몇 번 · 첫인사 몇 번 (`rpcrt4!LRPC_CASSOCIATION::Bind+0x626` / `+0x62d` · `NtAlpcConnectPort` 입구) | `PickAssociation` 비교 줄을 일부만 봤다 — "같은 연결, 첫인사 ×4" 는 `[추측]` |
| **D3** 진짜 rpcss 가 고른 `+0x48` (1 인가) · `+0x08` PID (`rpcrt4!CompleteBind` 입구 `db @rdx L0x70`) | 마스크를 만드는 줄까지 안 따라갔고, 서버가 고르는 값은 실행해야 보인다 |
| **D5** `Connect` 뒤 상태와 번호표 · PID · 서명 · 핑 · MID · 인증 / 가장 · OID · 연락처의 진짜 값 (`combase+0x18c1f3`) | 2.8 표의 값은 코드의 기본값 · 꼴뿐이다 |
| **D7** 응답 스텁의 진짜 바이트 (`rpcrt4!DoSendReceive+0x154` 뒤 `UnpackResponse` 입구) | 칸 차례는 `[규칙]` 로 풀었다 |
| D1 · D4 · D6 — 처음 연결 길인지 · 요청 인자 · `CheckStatus` 반환 | 값 읽기 |
| D8 · D9 (서버) — `rpcss!_Connect+0x73f` 반환 · `LocalInterfaceOnlySecCallback` 반환 (Q257b 와 묶음) | 서버 쪽은 필요한 줄만 읽었다 |
| D10 · D11 — 개발자 모드 검사가 읽는 키 · 첫 COM 사용의 둘째 · 셋째 부름 상태 | 선택 |

### 정적으로 남은 것 → **Q284**

결과서 "다음 요청 후보" 1 ~ 6. Q257b (창 스테이션 커널 본문 · `E_ACCESSDENIED`) 에는 보태지 않았다.

1. **(그다음 벽)** rpcss 메뉴 7 · 6 · 10 · 1 몸 (`_ServerAllocateOIDs` · `_ServerAllocateOXIDAndOIDs` · `_Disconnect` · `_SetAppID`) 의 입력 · 출력 꼴과 combase 쪽 검사 (`CallTheResolver +0x117 ~ +0x1b7` · `RegisterOXIDAndOIDs`).
2. **`LocalInterfaceOnlySecCallback`** (rva `0x1fbf0`, 2721 B) — 로컬 손님을 거절하는 조건. 거절하면 첫인사가 5 로 끝나 Q257b 갈래다.
3. **`MakeSCMProxy` · `DelegateActivationToSCM`** — 해결사 연락처가 어떤 글을 기대하나, 활성화 첫 ORPC 메뉴 · "클래스 / 서비스 없음" HRESULT 길.
4. **ole32 `CoGetObject`** 의 첫 RPC · `IrotGetObject` (IROT 3) 꼴.
5. **rpcrt4 `PickAssociation`** 비교 줄 전부 · `RpcBindingCopy` / `RpcBindingUnbind` — 첫인사 넷이 연결을 나눠 쓰나 (D2 와 겹침).
6. `KIsUnlockSettingEnabled` — `Connect` 앞 개발자 모드 검사가 읽는 레지스트리.

### 대기열 밖 — `[공백]` / `[추측]` 으로 남은 것

- `[공백]` 해결사 연락처 (DUALSTRINGARRAY) 의 진짜 내용과 뒤 쓰임. `wNumEntries 0` 이 뒤에서 괜찮은지.
- `[공백]` 설정 비트 · 서비스 목록 · 채널 훅 · SD 조각의 VM 진짜 값.
- `[공백]` 응답 선 위 진짜 바이트.
- `[공백]` `CoInitializeEx` 의 간접 호출 33곳.
- `[추측]` 응답이 아예 안 오면 영원히 기다린다 — 호출 시간제한 `0xffffffff` 를 무한으로 읽은 짐작.
- `[추측]` 보통 exe 는 AppID 가 없어 `SetAppID` 가 안 나간다.
- 하네스 제안 거리 (결과서 스크립트 문제 둘): `ExportFunctions` 에 `rva:` 고르기 · `CallGraphMap` `_edges.tsv` 에 rva 열. **사용자 검토 전** 이다.

### 짐 지도

- **새 짐 칸 없음.** 이번 길은 test.exe 초기화 밖 (combase · rpcrt4 안 올림) 이고, RPCSS 는 다른 프로세스다. 짐 지도 2장 숫자는 그대로다.

---

## 5. 더 읽기

- run 문서: [`run149-com-rpcss.md`](run149-com-rpcss.md)
  - "요약" · "정적 — 호출 지점" 표 · 1절 (bind — 1-1 카드 · 1-2 넷 · 1-3 말투 · 1-4 받는 모듈) · 2절 (`Connect` — 2-1 입력 · 2-2 응답 25칸 · 2-3 서버 검사) · 3절 (실패 처리 표) · 4절 (그다음 — 4-e 메뉴판) · "호출 관계" · "PE_emulator 쪽" · "flow-writer 대조" · "앞 run 고쳐 읽기" · "아직 확인 필요"
- 요청서: [`_workspace/sast/144_com-rpcss.md`](../../_workspace/sast/144_com-rpcss.md)
- 결과서: [`_workspace/sast/144_com-rpcss_result.md`](../../_workspace/sast/144_com-rpcss_result.md) — "스크립트 문제" · "먼저 읽는 한 줄 답" · 표 (1) ~ (4) · 4-d (`GetConnection` 46곳 전부) · "지금 우리 맞나 · 틀리나" · "동적으로 확인할 것" · "다음 요청 후보"
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/367_internals-com-rpcss.md`
- flow.md: [3장 — "COM 이 RPCSS 에 거는 첫 전화" 절 · "창 스테이션 · 데스크톱 이름 묻기" 절 · "epmapper 가 `ept_map` 에 답을 고르고 채우는 법" 절](flow.md) · 4장 run136 · Q257a · run149 줄 · 5장 run149
- 앞 해설:
  - [run136 해설](run136-winsta-name-해설.md) 2.8 (`Connect(NULL)` 길) · 2.9 (주소 글이 `Connect` 둘째 인자로) — 맨 위 고쳐 읽기 한 줄 참고
  - [run137 해설](run137-alpc-completion-해설.md) 2.10 (형식 문자열) — 맨 위 고쳐 읽기 한 줄 참고
  - [run129 해설](run129-winnsi-rpc-해설.md) 2.4 (연락 카드 · 직통 번호 · 내선) · 2.5 (이름표 · 메뉴 번호 · 접수 번호표) · 2.7 (114)
  - [run133 해설](run133-lrpc-wire-해설.md) 2.5 (첫인사 · 말투 · 마스크) · 2.6 (첫인사 답 검사) · 2.10 (ALPC 오류 → RPC 오류)
  - [run145 해설](run145-epm-endpoint-해설.md) (같은 전화선의 114 창구)
  - [run141 해설](run141-async-bind-해설.md) 2.4 (요구 서버 SID)
  - [run148 해설](run148-dialog-end-해설.md) 2.9 (금지 꼴)

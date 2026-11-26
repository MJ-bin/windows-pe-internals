# run136 winsta-name — 해설

> **고쳐 읽기 (run149)**: 2.8 "아직 확인 아님 — `Connect` → epmapper 로 이어진다는 것은 `[추측]`" 과 "한 줄로 먼저" · 1절 그림의 "다음 벽 `Connect` (RPCSS)" 는 **`[정적]` 으로 닫혔다** — 같은 람다의 `+0x33c CreateFromBindingString` 이 바인딩 글 `ncalrpc:[epmapper,Security=Impersonation Dynamic False]` (combase rva `0x2cc510`) 로 끝점을 박아 **`ept_map` 없이** `\RPC Control\epmapper` 로 곧장 가고, bind 넷 (`ILocalObjectExporter` `e60c73e6-88f9-11cf-9af1-0020af6e72f4` v2.0 — PDB 이름 · `IMachineActivatorControl` · `IROT` · `ISCM`) 뒤 `+0x82e Connect` = ILocalObjectExporter **프로시저 0** (`NdrClientCall2`). 2.8 의 "핸들 단계 0 → 0 + 글 NULL → `Connect(NULL)`" 길의 **진짜 끝**: rpcss `_Connect +0x186` 이 winsta 글 NULL 이면 상태 `0x57` → `CheckStatus` → `CoInitializeSecurity` = `0x80070057`. 이 글의 combase 쪽 줄은 그대로 맞다. 본문은 그대로 두었다 → [run149 해설](run149-com-rpcss-해설.md) 2.3 · 2.5 · 2.7 · 2.10

> 이 글은 [run136 문서](run136-winsta-name.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q250** — PE_emulator 요청 "창 스테이션 · 데스크톱 이름 묻기 (`GetUserObjectInformationW` 종류 2 · 1) 의 출력 꼴과, combase 가 그 답을 쓰는 법" (`_workspace/plan.md`, 이번에 **끝 (run136)**)
  - 원 요청 `324_internals-winsta-name.md`, 튜닝 `324_tune14.md` **T1**, 샘플 489 · 병목 120 · `CoInitializeSecurity` `0x8007000e` 63 (요청 쪽 에뮬레이터 숫자).
  - 사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 넷째 (마지막).
  - 결과서 판정 **분석성공** — 질문 1 · 2 · 3 모두 줄 근거로 답했다. 질문 3 은 예상과 달리 win32kbase 하나로 안 닫혀서 **win32kfull 도 처음 analyze** 했다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q256** (이름 대소문자 · 핸들 값 · 종류 1 `dwFlags` / `fInherit`, 에뮬레이터 PEB `+0x58`)
    - 정적 → **Q257** (다음 벽 `Connect` → epmapper, 핸들 묻기 커널 본문, `E_ACCESSDENIED` 갈래)
- 근거:
  - run 문서 [`run136-winsta-name.md`](run136-winsta-name.md) (가장 중요)
  - 요청서 [`_workspace/sast/134_winsta-name.md`](../../_workspace/sast/134_winsta-name.md) · 결과서 [`134_winsta-name_result.md`](../../_workspace/sast/134_winsta-name_result.md)
    - **처음 analyze 둘**: win32kbase (약 3분) · win32kfull (약 2분), 한 번에 하나.
    - user32 · combase · win32u 는 다시 analyze 안 하고 `^이름$` export 만.
    - combase 의 부르는 쪽 셋 (`CoInitializeSecurity` · `GetConnection` · 람다 `operator()`) 은 **일회용 objdump** 로 읽었다 (2.9 에서 이유).
    - 스크립트 문제 없음.
  - flow.md 변경: docs 커밋 `a941bf3` (`git show a941bf3 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다.**
   - 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) 에 user32 · combase 가 없다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run131 과 같은 줄).
   - test.exe 의 `_EPROCESS.Win32WindowStation` 은 K1 ~ P2 내내 NULL 이다 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:151`, 다른 일곱 장도 같은 칸). 곧 **창 스테이션 자체가 없다.**
   - `sample/*.c` 에 COM · 창 API 가 없다 `[소스]` (`sample/log.h:3`).
   - 그래서 이번 이야기도 "**COM 보안을 여는 다른 프로그램이라면**" 이다. 그림 · 짐 지도는 그대로다.
2. **요청 쪽 로그는 에뮬레이터 것이다.**
   - `0x20000` · `0x20400`, 샘플 수 (489 · 63 · 5 …), "Exception.ToString()이 실패" 는 모두 PE_emulator 쪽 이야기다. `[관찰]` 이 아니다.
   - **r13 · r14** 는 요청 쪽 에뮬레이터의 **판 번호** 다.
     - r13 = 창 스테이션 핸들을 **0** 으로 돌려주던 판.
     - r14 = 핸들을 `0x20000` 으로 돌려주게 고친 지금 판. 이번 막힘은 r14 에서 나왔다.
   - 에뮬레이터가 본 `0x8007000e` · `0x8007007a` 두 값이 `.asm` 줄에서 그대로 나왔다. "오프셋 셈 · 바이너리를 믿어도 된다" 는 근거다. 진짜 Windows `[관찰]` 은 여전히 아니다.
3. **오프셋 읽는 법** 은 run131 해설과 같다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 "`+0xc8 GetUserObjectInformationW`" 처럼 부르는 함수 이름 **앞** 숫자 = **call 자리**.
   - "`+0x238 MOV EBX,0xe`" 처럼 명령이 붙은 숫자 = 함수 안 **줄**.
   - 콜스택으로 본 적은 없다.
4. **`GetThreadWinstaDesktop` 은 몸이 흩어져 있다.**
   - 조각 (`FUN_1800824e3` 등), 멀리 떨어진 범위 (`+0x1d8eb6 ~`, `+0x1d9007 ~`, `+0x1d90ae ~`), 주인 없는 덩어리로 나뉘어 있다.
   - 그래서 `+0x1d9044` 같은 큰 오프셋이 나온다. 다른 함수가 아니다. 오프셋은 모두 함수 시작 (`0x180082330`) 기준이다.
   - run131 의 `RegisterOleWndClass +0x1397..` 와 같은 사정이다. WinDbg 에선 이름으로 안 보일 수 있어서, 동적 요청은 `combase+rva` 로 건다 (4절).
5. **같은 이름 함수가 둘 있다.**
   - `EditionGetUserObjectInformationEntryPoint` 는 **win32kbase (중계, rva `0x12cc64`)** 와 **win32kfull (본체, rva `0xbfc60`)** 둘 다 있다. 이 글도 모듈 이름을 꼭 같이 쓴다.
6. **win32k 는 구조체 타입이 없다.**
   - 두 PDB 다 공개 심볼이라 `USEROBJECTFLAGS` · `WINDOWSTATION` 같은 타입이 0개다.
   - 그래서 커널 객체 칸은 "코드가 읽는 오프셋" 으로만 말한다. `fInherit` · `dwFlags` 같은 칸 이름은 공개 문서 이름을 빌린 것이라 `[추측]` 이다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- user32 = **창 담당 건물**, win32u = **옆 건물 단축번호 버튼판**, 커널 콜백 · PEB `KernelCallbackTable` (`+0x58`) = 안내판의 **내선 번호표** → [run85 해설](run85-user32-init-해설.md) "비유" · 2.1
- win32k = 관리사무소의 **그래픽 부서**, 번호 `0x1000` 이상 = **옆 건물 번호부** → [run51 해설](run51-eprocess-later-해설.md) 2.9 · [run56 해설](run56-syscall-args-해설.md) 2.9
- combase = 가게에 들인 **COM 담당 건물**, `E_OUTOFMEMORY` (`0x8007000e`) = "**메모리 부족**" 도장, 임시 stub → [run131 해설](run131-user32-regclass-해설.md) 1절 · 2.7
- HRESULT = COM 쪽 32비트 결과 코드, "음수면 실패" → [run89 해설](run89-dotnet-start-해설.md) ("새 말 — HRESULT")
- TEB `LastErrorValue` (`+0x68`) → [run54 해설](run54-teb-fill-해설.md), `SetLastError` = **고장 메모** → [run91 해설](run91-bcrypt-init-해설.md) 2.6
- 프로세스 매개변수 `DesktopInfo` → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- 지연 가져오기 = "**필요하면 그때 부를게요**" 메모 → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 2.5, api-set = **별명** → [run57 해설](run57-nls-apiset-해설.md)
- 프로세스 힙 = 가게 **창고** → [run46 해설](run46-process-heap-해설.md)
- rpcrt4 = 가게 안 **전화 부서**, epmapper = **114 안내** → [run99 해설](run99-rpc-caller-해설.md) · [run129 해설](run129-winnsi-rpc-해설.md) 2.7 · [run133 해설](run133-lrpc-wire-해설.md)

이번에 처음 나오는 말 — **창 스테이션 · 데스크톱**, **`CoInitializeSecurity` · RPCSS**, **썽크**, **종류 번호 (`nIndex`)**, **필요 길이 (`lpnLengthNeeded`) · 두 번 묻기**, **`0x7a`**, **묵은 LastError**, **람다 `operator()`**, **win32kbase 껍데기 · win32kfull 본체**, **객체 이름 끝 조각**, **종류 1 (`USEROBJECTFLAGS`)** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, win32k = 관리사무소 **그래픽 부서**, syscall = **신청 창구**, 힙 = **창고**.
- user32 = **창 담당 건물**, win32u = **단축번호 버튼판**, combase = **COM 담당 건물**, rpcrt4 = **전화 부서**, epmapper = **114 안내**.
- LastError = 스레드 수첩의 **고장 메모** 칸.
- 이번에 새로 붙는 그림 (run 문서 "결과" 비유를 따름 — run 문서는 이 비유를 `[추측: 비유]` 로 둔다):
  - 창 스테이션 = 가게가 세 든 **상가 건물**, 데스크톱 = 그 건물 안의 **방**.
    - run 문서는 그냥 "건물" 이라 불렀다. 이 시리즈엔 이미 "창 담당 건물" (user32) 이 있어서, 헷갈리지 않게 이 글은 **상가 건물** 로 부른다. 같은 것이다.
  - RPCSS = 동네 **COM 관리실**. combase 는 거기에 전화를 걸 때 "저는 `WinSta0` 건물 `Default` 방에서 걸어요" 라는 **주소** 를 붙인다.
  - 주소를 알려고 그래픽 부서에 "이 건물 이름이 뭐예요?" 하고 묻는다.
    - run 문서는 이 자리를 "**경비실**" 이라 불렀다. 이 글은 시리즈대로 **그래픽 부서** 로 쓴다.
  - 그래픽 부서는 이름을 종이에 적어 주거나, 종이가 작으면 "**몇 칸 필요해요**" 메모와 함께 "**종이 모자람 (`0x7a`)**" 고장 메모를 준다.

### 한 줄로 먼저 — 무엇을 얻었나

> **combase 는 COM 관리실 (RPCSS) 에 전화하기 전에, 창 스테이션 · 데스크톱 이름을 한 번씩 묻고 `WinSta0\Default` 같은 주소 글을 만들어 전화의 둘째 인자로 붙인다.**
> **user32 는 끼어들지 않는다 — 커널 답이 그대로 온다. 진짜 커널 (win32kfull) 은 "TRUE + UTF-16 이름 + NUL, 필요 길이 = 바이트" 로 답하고, 종이가 작으면 "FALSE + `0x7a` + 필요 길이" 로 답한다.**
> **에뮬레이터가 막힌 까닭: "FALSE 인데 고장 메모는 0, 주소 글은 아직 NULL" 이면 combase 가 `0x8007000e` 를 만들어 `CoInitializeSecurity` 까지 그대로 돌려준다. 묵은 메모가 `0x7a` 면 `0x8007007a` 가 된다.**
> **그래서 stub 은 TRUE + `WinSta0` / `Default` + NUL (16 B) 을 주고, 필요 길이를 성공 · 실패 모두 써 주면 된다. 그러면 다음 벽은 `Connect` (RPCSS) 다.**

- 넷 다 `[정적]`. 단 진짜 이름의 대소문자 (`WinSta0`) 는 정적 근거가 가리키는 값이고, 실제 객체 이름은 `[공백]` (Q256) 이다 (2.10).

요청서 질문에 대한 답 (run 문서 "요약" 기준):

| 요청 질문 | 답 | 이 글 |
|---|---|---|
| 1 W 는 썽크인가 | **예.** `JMP [0xb6d90]` 한 줄 (6 B) → win32u `NtUserGetObjectInformation` (`0x1065`). 반환 · LastError 를 user32 가 안 건드린다 `[정적]` | 2.3 |
| 1 A 인자 차림 · 돌아온 뒤 | 인자 5개, 길이 · 필요 길이 모두 **바이트**. A 는 종류 2 · 3 에서만 UTF-16 → ANSI. combase 는 A 를 안 부름 `[정적]` | 2.3 |
| 2 묻는 차례 · 크기 | `GetProcessWindowStation` → `GetThreadDesktop` → 이름 묻기 2번 (종류 2, `0x40`) `[정적]` | 2.4 |
| 2 버퍼 모자랄 때 | `GetLastError() == 0x7a` 면 `len` 만큼 힙을 얻어 **그 묻기만 한 번 더** `[정적]` | 2.4 |
| 2 실패 HRESULT | `GetLastError` → `0x8007xxxx`. 단 이름 단계에서 0 + 글 NULL 이면 **`0x8007000e`** `[정적]` | 2.6 · 2.7 |
| 2 만드는 글 · 가는 곳 | `<창 스테이션>\<데스크톱>` → `_pwszWinstaDesktop` → **`Connect` 둘째 인자** `[정적]` | 2.9 |
| 2 핸들 0 (r13) | 이름을 안 묻고 **0 + 글 NULL** → `Connect(NULL)` `[정적]` | 2.8 |
| 3 커널 종류 2 · 1 | win32kfull `_GetUserObjectInformation`. 종류 2: 끝 조각 + NUL, `Length + 2` 바이트. 종류 1: 12 B `[정적]` | 2.5 |
| (PE_emulator) | stub 이 돌려줄 값 | 2.11 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

test.exe 흐름 밖, **옆길** 이다. run131 (창 클래스 등록) 과 같은 combase 의 **COM 보안 쪽** 이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         └─ dll 전원 켜기
 │               ├─ (test.exe) dll2 → dll1
 │               └─ (user32 를 올리는 프로세스라면) user32 DllMain        ← run85 (PEB+0x58 을 놓는 곳)
 │
 │  … 어느 때, 프로그램이 COM 을 쓰면
 │        ├─ CoInitializeEx(STA) → 창 클래스 등록                      ← run131
 │        └─ CoInitializeSecurity → 창 스테이션 · 데스크톱 이름 묻기     ← 이번 (run136)
 │                                    └─ 주소 글 → Connect (RPCSS)      ← 다음 벽 (Q257)
 │
 └─ … → NtContinue (은퇴) → exe 진입점 → main
```

- `CoInitializeSecurity` 를 **누가 · 언제** 부르는지는 이번 근거에 없다. 그래서 그림에 "어느 때" 로만 둔다.
- run85 가 놓는 PEB `+0x58` 을 이번에 combase 가 **읽는다** (2.10). 그래서 run85 와 줄이 하나 이어졌다.

#### 새 말 — 창 스테이션 · 데스크톱

- 창 스테이션 = 클립보드 · 데스크톱들을 묶는 **커널 객체**. 데스크톱 = 그 안에서 창들이 사는 **커널 객체**. 대화형 기본 이름이 `WinSta0` · `Default` 다 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거에서 확인된 것:
  - win32kfull 은 둘을 `ExWindowStationObjectType` · `ExDesktopObjectType` 형식으로 가른다 (`_GetUserObjectInformation +0x90 ~ +0xaa`) `[정적]`.
  - 커널 쪽 상수 글은 `WinSta0` (`.rdata` `0x36fee8`) · `\Windows\WindowStations\WinSta0` (`0x36f1c0`) 이다 `[정적]`.
- 비유: 가게가 세 든 **상가 건물** (창 스테이션) 과 그 건물 안의 **방** (데스크톱).

#### 새 말 — `CoInitializeSecurity` · RPCSS

- `CoInitializeSecurity` = 프로세스의 COM 보안 설정을 정하는 combase 함수. RPCSS = COM 을 관리하는 시스템 서비스 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거에서 확인된 것: `CoInitializeSecurity` (rva `0x7e880`) → `GetConnection` → 람다 → `Connect` (rva `0x12f318`) — run 문서는 `Connect` 를 "RPCSS 쪽 RPC 클라이언트 스텁" 이라 부른다. **`Connect` 안은 안 읽었다** `[정적]` (그 안 → Q257).
- 비유: 동네 **COM 관리실** 에 거는 첫 전화.

### 왜 이 질문이 지금 왔나

- PE_emulator 에서 샘플 **489** 개가 같은 자리를 지난다 (요청 쪽 에뮬레이터 로그 r14 — 진짜 Windows 관찰 아님):
  1. `NtUserGetProcessWindowStation` → `0x20000`
  2. `NtUserGetThreadDesktop` → `0x20400`
  3. `NtUserGetObjectInformation(0x20000, 2, buf, 0x40)` → **기본 대답 0 (버퍼 안 씀)**
  4. 끝 글 "Exception.ToString()이 실패" → 종료
- 이 가운데 `CoInitializeSecurity` 가 `0x8007000e` 로 실패한 것이 63, `0x8007007a` 가 5 (요청 쪽 숫자).
- 그 전 판 r13 (핸들 0) 에선 이름을 안 묻고 바로 `\RPC Control\epmapper` 연결로 갔다.
- 요청 쪽이 알고 싶은 것: 새 stub `nt_user_get_object_information.cpp` 가 **무엇을, 어느 칸에, 얼마나** 써 줘야 하나.

### 무엇이 궁금했나

1. **user32** — `GetUserObjectInformationW` 가 정말 바로 넘기는가. `A` 판의 인자 차림, 돌아온 뒤 읽는 것, LastError 는 누가 정하나.
2. **combase** — `GetThreadWinstaDesktop` 이 몇 번 · 무엇을 묻나, 종이가 작으면, 실패 HRESULT, 만드는 글과 그 글이 가는 곳, 핸들 0 이면.
3. **커널 (되면)** — 종류 1 · 2 의 출력 칸, 모자랄 때 반환 · LastError.

### 바퀴 전에 알던 것

- `[정적]` (run131) combase 를 처음 열었다. `RegisterOleWndClass` 에 "`GetLastError` 0 → `0x8007000e`" 꼴이 있었다 — **같은 꼴이 이번에도 나온다.**
- `[정적]` (run85) user32 `DllMain` 이 PEB `+0x58` (`KernelCallbackTable`) 을 놓는다.
- `[정적]` (run129 · run133) rpcrt4 가 `\RPC Control\epmapper` 에 연결하는 길과 LRPC 선 위 형식. 이번 글이 가는 `Connect` 의 다음 몫이다.
- `[관찰]` (run44) test.exe 의 매개변수 `DesktopInfo` = `Winsta0\Default` (`ms/logs/04_baggage-read_P2.txt:3243`).
- win32u 번호는 이미 있었다: `NtUserGetObjectInformation` `0x1065` · `NtUserGetProcessWindowStation` `0x101f` · `NtUserGetThreadDesktop` `0x107b`.
- win32kbase 는 run86 때 `objdump` 로만 봤고, win32kfull 은 **한 번도** 안 열었다.

---

## 2. 찾은 것

### 2.1 한눈에 — `CoInitializeSecurity` 에서 커널 본체까지 `[정적]`

run 문서 "호출 관계" 를 줄였다 (오프셋은 ret 자리).

```
combase!CoInitializeSecurity (0x7e880)
 └─ +0x482  CRpcResolver::GetConnection (0xa14d0)
             └─ +0xc3  operator() (0x18b9c0)
                        ├─ +0x99  GetThreadWinstaDesktop (0x82330)     ← 주소 글이 아직 NULL 일 때만
                        │          ├─ +0x6b  GetProcessWindowStation ⇢ user32 썽크 ── SYSCALL [0x101f]
                        │          ├─ +0x91  GetThreadDesktop        ⇢ user32 썽크 ── SYSCALL [0x107b]
                        │          ├─ +0xcf  GetUserObjectInformationW(창 스테이션, 2, 0x40 B, 0x40, &len)
                        │          │           ⇢ user32 썽크 → NtUserGetObjectInformation ── SYSCALL [0x1065]
                        │          │              ⇒ win32kbase (껍데기 · 중계) → win32kfull … _GetUserObjectInformation
                        │          ├─ +0xfe  GetUserObjectInformationW(데스크톱, 2, 0x40 B, 0x40, &len)
                        │          ├─ (FALSE · LastError 0x7a) 그 묻기만 한 번 더
                        │          └─ +0x141 HeapAlloc · 글 잇기 → _pwszWinstaDesktop = "<창 스테이션>\<데스크톱>"
                        ├─ (음수)   +0xba Return_Hr → 반환 (Connect 안 감)
                        └─ (0 이상) +0x833 Connect (0x12f318)   RDX = _pwszWinstaDesktop (NULL 가능)
```

- `⇢` = 지연 가져오기 발판 (`ext-ms-win-ntuser-windowstation-l1-1-0`). 그 안은 안 읽었다.
- `⇒` = syscall → win32k 다리. 이름 · export 표 · 함수 표 칸 번호가 맞는 것이 근거다 — 다리 코드 자체는 안 읽어서 `[추측]`.
- 콜스택으로 본 적은 없다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ +0xcf GetUserObjectInformationW(창 스테이션, 2, 0x40 B, 0x40, &len)`

- **누가**: combase `GetThreadWinstaDesktop` (rva `0x82330`) 가
- **어디서**: 함수 안 `+0xc8` 에서 부르고, 돌아오는 자리는 `+0xcf` (나무 숫자)
- **누구를**: 지연 가져오기 칸 `0x180363578` → user32 `GetUserObjectInformationW` (한 줄 썽크) → win32u `NtUserGetObjectInformation` → 번호 `0x1065` 로 그래픽 부서에 들어간다
- **무엇을 위해**: 창 스테이션의 **이름** 을 `[RBP-0x49]` 의 `0x40` 바이트 종이에 받으려고
  - "2" = 종류 번호 2 (이름). 코드는 `+0xc1 LEA EDX,[RBX-0x3e]` 로 만든다 — `EBX` 가 `0x40` 이라 2 가 된다.
  - "&len" = `[RBP-0x59]`. 부르기 **전에** `0x40` 을 먼저 넣어 둔다 (`+0xae`). 이 작은 사실이 2.7 에서 중요해진다.

#### 줄 2: `└─ (0 이상) +0x833 Connect (0x12f318) RDX = _pwszWinstaDesktop`

- **누가**: 람다 `operator()` (rva `0x18b9c0`) 가
- **어디서**: `+0x82e` 에서 부르고, `+0x833` 으로 돌아온다
- **누구를**: `Connect` (rva `0x12f318`) — RPCSS 쪽 RPC 클라이언트 스텁. 이 함수를 부르는 곳은 여기 **하나뿐** 이다 (`refs 0x12f318`)
- **무엇을 위해**: COM 관리실에 첫 전화를 걸려고. **둘째 인자 (RDX)** 에 주소 글을 실어 보낸다 (`+0x742 MOV RDX,[_pwszWinstaDesktop]`).
  - "(0 이상)" = `GetThreadWinstaDesktop` 이 실패 (음수) 가 아닐 때만 여기 온다 (`+0x9d JNS +0x100`).
  - "NULL 가능" = 0 을 돌려받았는데 글이 NULL 인 경우도 그대로 넘어간다 (2.8).

### 2.3 user32 는 끼어들지 않는다 — 썽크 `[정적]`

#### 새 말 — 썽크

- 자기는 아무 일도 안 하고 **다른 함수로 바로 뛰는** 한 줄짜리 함수다.
- 이번 것: `GetUserObjectInformationW.asm +0x0 JMP [0x1800b6d90]` 가 **전부** (6 B). 그 칸 = win32u `NtUserGetObjectInformation`.
- `GetProcessWindowStation` (`→ 0xb6e00`) · `GetThreadDesktop` (`→ 0xb6db8`) 도 같은 꼴이다.
- 비유: 창 담당 건물 입구의 **돌림 단추**. 누르면 곧장 단축번호 버튼판 (win32u) 으로 넘어간다. 건물 사람은 아무것도 안 고친다.

그래서:

- 다섯 인자 (RCX · RDX · R8 · R9 · 스택 다섯째) 가 **그대로** 커널로 가고, 커널 반환이 **그대로** 돌아온다.
- **LastError 도 user32 가 안 쓴다.** 실패 때 보이는 `TEB.LastErrorValue` 는 커널이 쓴 값이다 (2.5).
- 곧 에뮬레이터 stub 이 내놓는 답이 combase 에게 **아무 손질 없이** 보인다. user32 가 "모자란 걸 채워 주는" 일은 없다.

#### 인자 다섯 (run 문서 표 줄임)

| # | 이름 (공개 문서 자리) | 뜻 | 입력/출력 | 단위 |
|---|---|---|---|---|
| 1 | `hObj` | 창 스테이션 · 데스크톱 핸들 | 입력 | 핸들 |
| 2 | `nIndex` | 종류 (1 FLAGS · 2 NAME · 3 TYPE …) | 입력 | — |
| 3 | `pvInfo` | 출력 종이 | **출력** | 바이트 |
| 4 | `nLength` | 종이 크기 | 입력 | **바이트** |
| 5 | `lpnLengthNeeded` | 필요 크기를 받는 칸 | **출력** | **바이트** |

- 줄 근거는 `A` 판 (`GetUserObjectInformationA`) 에서 읽었다 (run 문서 1절 표).

#### 새 말 — 종류 번호 (`nIndex`)

- 같은 함수 하나로 "이름 줘" · "깃발 줘" · "형식 줘" 를 다 묻는다. 무엇을 물을지 고르는 번호가 둘째 인자다.
- 이번에 중요한 건 **2 = 이름** 과 **1 = 깃발 (12 B)** 둘이다.
- 비유: 그래픽 부서 신청서의 "**무엇을 알려 드릴까요?**" 칸.

#### `A` 판은? — 이번 길과 무관, 그래도 한 가지 주의

- `A` 는 종류 2 · 3 일 때만 끼어든다: 임시 종이 `nLength × 2` 에 UTF-16 으로 받고, 성공이면 NUL 까지 세어 ANSI 로 바꾸고 필요 길이를 반으로 (`+0x82 SHR [RBP],1`) 만든다. 실패면 그대로 0.
- **combase 는 A 를 안 부른다.**
- 다만 주의 하나 (run 문서): A 는 커널 글이 **NUL 로 끝난다고 믿고** 센다 (`+0x92 ~ +0x9f`). NUL 없이 종이를 꽉 채운 stub 이면 이 세기가 **종이 끝을 넘는다.** stub 이 NUL 을 꼭 써야 하는 이유 하나다.

### 2.4 combase 가 묻는 차례 `[정적]`

`GetThreadWinstaDesktop` (rva `0x82330`, 909 B). run 문서 2절 차례 표를 줄였다.

| 차례 | 하는 일 | 대표 줄 |
|---|---|---|
| ① | 정해 둔 글 (`g_explicitWinstaDesktopString`) 이 있으면 그것으로 끝 (0) | `+0x3e JNZ +0x2d2` |
| ② | user32 가 없거나 PEB `+0x58` 이 0 이면 → 핸들 없이 `ResolveProcessDesktopNoUser` (2.10) | `+0x4b JZ` · `+0x5a CMP [RAX+0x58],R12` |
| ③ | 창 스테이션 핸들 `GetProcessWindowStation()` → RDI. **0 이면 ⓐ** | `+0x73 TEST` · `+0x76 JZ +0x23f` |
| ④ | 데스크톱 핸들 `GetThreadDesktop(GetCurrentThreadId())` → R14. **0 이면 ⓑ** | `+0x99 TEST` · `+0x9c JZ +0x31c` |
| ⑤ | `len = 0x40` 먼저 → 창 스테이션 이름 묻기. FALSE 면 → `+0x358` | `+0xae` · `+0xc8` · `+0xd6 JZ +0x358` |
| ⑥ | `len = 0x40` 다시 → 데스크톱 이름 묻기. FALSE 면 → `+0x39d` | `+0xe0` · `+0xf7` · `+0x105 JZ +0x39d` |
| ⑦ | 두 글 길이 세고 `cch = A + B + 2` → `HeapAlloc(g_hHeap, 0, cch × 2)` → `_pwszWinstaDesktop`. 할당 실패 `EBX = 0xe` | `+0x13a` · `+0x146` · `+0x152` |
| ⑧ | `StringCchCopyW(A)` → `StringCchCatW(L"\\")` → `StringCchCatW(B)`. 실패면 지우고 `EBX = 0x6f` | `+0x169` · `+0x187` · `+0x1a1` · `+0x2b1` |
| ⑨ | 성공 `EBX = 0` | `+0x1ae` |
| ⑩ | **이름 단계 출구** (2.6) | `+0x1d6 ~ +0x1ff` |

- 비유: COM 담당 건물이 COM 관리실에 전화하기 전에, ① 미리 적어 둔 주소가 있나 보고, ② 이 가게가 창 담당 건물을 안 들였으면 **가게 서류** (매개변수) 의 주소를 쓰고, ③ ④ 아니면 상가 건물 · 방의 **열쇠** (핸들) 를 받은 다음, ⑤ ⑥ 그래픽 부서에 **건물 이름 · 방 이름** 을 묻고, ⑦ ⑧ "건물\방" 주소를 수첩 한 칸에 적는다.

#### 새 말 — 필요 길이 (`lpnLengthNeeded`) · 두 번 묻기

- **필요 길이** = 커널이 "이 답을 다 적으려면 몇 바이트가 필요해요" 를 적어 주는 출력 칸 (다섯째 인자).
- **두 번 묻기** = 처음엔 적당한 종이로 묻고, "모자람" 이면 필요 길이만큼 종이를 새로 얻어 **한 번 더** 묻는 꼴.
- 이번 combase 의 실제 꼴 (`+0x366 CMP EAX,0x7a` · `+0x369 JZ` / `+0x3ab` · `+0x3ae`):
  1. ⑤ 나 ⑥ 이 FALSE 면 `GetLastError()` 를 본다.
  2. **`0x7a` 일 때만** `HeapAlloc(g_hHeap, 0, len)` (len = `[RBP-0x59]`, 커널이 써 준 필요 길이).
  3. 같은 핸들 · 종류 2 · 크기 `len` 으로 **그 묻기만** 한 번 더 (`+0x1d902c MOV R9D,[RBP-0x59]`).
  4. 성공이면 다음 차례로, 또 실패면 `GetLastError` → ⑩ 출구.
- 그래서 **최대 네 번** 묻는다 (`+0xc8` · `+0xf7` · `+0x1d9044` · `+0x1d90eb`). combase 에서 `GetUserObjectInformationW` 를 부르는 곳은 **이 넷뿐** 이다.
- 비유: 종이를 내밀었는데 "몇 칸 필요해요 + 종이 모자람" 메모가 오면, 창고에서 그만큼 큰 종이를 꺼내 **같은 질문을 한 번 더** 한다.

#### 새 말 — `0x7a`

- "주신 종이가 작아요" 라는 Win32 오류 번호다. 요청서는 이것을 `ERROR_INSUFFICIENT_BUFFER` (122 = `0x7a`) 라고 불렀다 `[추측: 공개 이름]`.
- 이번 근거에서 확인된 것: win32kfull 이 종이가 작을 때 `+0x18a MOV ECX,0x7a` 로 이 값을 고장 메모에 쓴다 `[정적]` (2.5).

#### 요청 쪽의 다른 꼴은 combase 가 아니다

- 요청 쪽 로그엔 `(…, 1, buf, 0xc)` 353 번 · `(0x20400, 2, buf, 0x208)` 1 번도 있다.
- combase 는 종류 **늘 2**, 처음 크기 **늘 `0x40`** 이다. 그래서 이 둘은 **다른 모듈** 이 부른 것이다 `[정적]`.
- 어느 모듈인지는 정적 자료에 없다 `[공백]` (`.NET` 런타임 쪽일 수 있음 `[추측]`) → Q256 ③.

### 2.5 ★ 커널의 진짜 답 — win32kfull `_GetUserObjectInformation` `[정적]`

#### 새 말 — win32kbase 껍데기 · win32kfull 본체

- 그래픽 부서 (win32k) 는 이번 길에서 **두 모듈** 로 나뉜다.
  - `win32kbase!NtUserGetObjectInformation` (rva `0x1223b0`, 135 B) = **껍데기**. 함수 표를 두 번 읽는다.
    - 표 `+0x388` = **문지기** 칸. 0 이상이면 통과, 칸이 비었거나 음수면 FALSE (LastError 안 씀). win32kfull 쪽에선 "0 을 돌려주는 공용 조각" (rva `0x1ed1d0`) 이다.
    - 표 `+0x390` = 다음으로 넘길 곳 — `win32kfull!EditionGetUserObjectInformationEntryPoint` (rva `0xbfc60`).
  - `win32kfull!EditionGetUserObjectInformationEntryPoint` = **입구 검사** (쓰기 검사 · 핸들 잡기 · 락).
  - `win32kfull!_GetUserObjectInformation` (rva `0xc119c`, 981 B) = **종류별 본문**.
- 비유: 그래픽 부서의 **접수대** (win32kbase) 가 서류를 받아 안쪽 **실무 담당** (win32kfull) 에게 넘긴다. 접수대엔 "오늘 이 업무 하나요?" 를 묻는 **문지기 칸** 이 하나 있다.

```
win32u!NtUserGetObjectInformation  SYSCALL 0x1065
  ⇒ win32kbase!NtUserGetObjectInformation (0x1223b0)  껍데기
      +0x47 (CFG) [작은 표+0x388]  문지기
      +0x84 win32kbase!Edition…EntryPoint (0x12cc64)  중계
              +0x55 (CFG) [작은 표+0x390] = win32kfull!Edition…EntryPoint (0xbfc60)
                       +0x10d _GetUserObjectInformation (0xc119c)   ← 종류별 갈래
                       *lpnLengthNeeded = 로컬 길이 (+0x183 ~ +0x18f)
```

(run 문서 3절 나무 줄임. 오프셋은 ret 자리.)

#### 종류 2 (이름) — 무엇을 적어 주나

| 무엇 | 쓰는 값 | 줄 |
|---|---|---|
| 글 | 객체 이름 **끝 조각** (`WinSta0` · `Default`) 을 `Length` 바이트 복사 | `+0x111 ~ +0x146` · `+0x168` |
| 끝 | 글 뒤 `0x0000` (NUL) | `+0x16d ~ +0x178` |
| 필요 길이 | **`Length + 2`** (바이트, NUL 포함) | `+0x14a LEA EAX,[R8+2]` |
| 종이 모자람 | `Length + 2 > nLength` → **FALSE · LastError `0x7a` · 필요 길이는 씀 · 종이엔 안 씀** | `+0x152` · `+0x155 JA` · `+0x18a` |
| 이름 없음 | **TRUE · 필요 길이 0 · 종이엔 안 씀** | `+0x144 JZ +0x181` |

##### 새 말 — 객체 이름 끝 조각

- 커널 객체는 `\Windows\WindowStations\WinSta0` 처럼 **긴 경로 이름** 을 갖는다 `[추측: 일반적으로 알려진 설명]`.
- win32kfull 은 `ObQueryNameInfo` 가 주는 이름 (`+0x8` `Name`) 을 복사한다. 이것이 **마지막 조각만** (`WinSta0` · `Default`) 이고, `\Windows\WindowStations\` 는 안 붙는다 `[정적]`.
- 비유: 주소록 전체가 아니라 **문패** 에 적힌 이름만 읽어 준다.

##### 크기 예 — 왜 "16 B" 인가

- `WinSta0` · `Default` 둘 다 7 글자. run 문서가 적은 크기는 **16 바이트** (UTF-16 글 + NUL 2 바이트).
- combase 의 첫 종이 `0x40` 바이트 안에 들어간다. 그래서 진짜 Windows 에선 **두 번 묻기 (`0x7a`) 길을 안 탄다** `[정적]`.

#### 필요 길이는 "바깥" 이 쓴다 — 성공이든 실패든

여기가 stub 에 중요하다. 한 걸음씩 (run 문서 3절 · flow-writer 대조):

1. win32kfull 입구 `EditionGetUserObjectInformationEntryPoint` 는 `_GetUserObjectInformation` 에 호출자 칸이 아니라 **자기 로컬 칸** (`[RSP+0x30]`) 을 넘긴다. 이 로컬은 `+0x28` 에서 0 으로 비워 둔다.
2. 본문이 거기에 `Length + 2` (또는 종류 1 이면 `0xc`) 를 적는다.
3. 돌아온 뒤, 호출자 포인터가 NULL 이 아니면 **성공이든 실패든** 그 로컬 값을 호출자 칸에 옮긴다 (`+0x124 ~ +0x12f JNZ +0x183` → `+0x18f MOV [RCX],EAX`).
4. 그래서:
   - 성공 → 필요 길이 씀.
   - `0x7a` 실패 → 필요 길이 씀.
   - **이른 실패** (핸들 잡기 실패 · 형식 아님 · 지원 안 하는 종류 `0x57`) → 로컬이 0 그대로라 **필요 길이 = 0** (flow-writer 대조 — 결과서는 "늘 쓰는 것처럼" 읽혀서 run 문서가 바로잡음).
   - 입구에서 핸들 참조 자체가 실패하면 (`+0xcc JS +0x1a3`) → FALSE, LastError 도 필요 길이도 **안 씀**.

비유: 실무 담당은 "몇 칸 필요" 를 **자기 메모지** 에 적고, 접수 창구가 그 메모를 손님 종이에 옮겨 적는다. 실무 담당이 일찍 손을 놓으면 메모지는 빈칸 (0) 그대로 옮겨진다.

#### 고장 메모는 커널이 직접 쓴다

- `win32kfull!UserSetLastError` (rva `0x48390`): `+0x18 MOV RAX,GS:[0x30]` (커널 GS `+0x30` = 현재 TEB `[추측: KPCR 칸 이름]`) → `+0x21 MOV [RAX+0x68],EBX` (`TEB.LastErrorValue`).
- WoW64 면 `+0x3e` 에서 `+0x2034` 에도 쓴다.
- 남의 프로세스에 붙어 있으면 (`KeIsAttachedProcess`, `+0x16 JNZ`) 안 쓴다 — 이번 길과는 무관 (flow-writer 보탬).
- 비유: 그래픽 부서는 대답만 주는 게 아니라, **손님 수첩의 고장 메모 칸에 직접** "종이 모자람" 이라 적어 준다. 창 담당 건물 (user32) 은 그 수첩을 만지지 않는다.

#### 종류 1 (깃발, 12 B) — 짧게

- `USEROBJECTFLAGS` 라는 공개 이름의 12 B 꼴 `{fInherit, fReserved, dwFlags}` 다 (칸 이름 `[추측: 공개 이름]`).

| 칸 | 쓰는 값 |
|---|---|
| `+0x0` `fInherit` | 핸들 속성 비트 `0x2` (`OBJ_INHERIT`) 를 0 / 1 로 |
| `+0x4` | 0 (`+0x4 ~ +0xb` 8 바이트를 0 으로) |
| `+0x8` `dwFlags` | **창 스테이션**: 객체 `+0x40` 비트 `0x4` 가 꺼져 있으면 **1** (공개 상수 `WSF_VISIBLE` 와 같은 값 `[추측: 이름]`), 켜져 있으면 0. **데스크톱**: `CheckHandleFlag(…)` 참이면 1 (이름 `[공백]`) |
| 필요 길이 | 늘 `0xc`. `nLength < 0xc` 면 FALSE + `0x7a` |

- 이 종류는 **combase 가 안 쓴다.** 요청 쪽 353 번은 다른 모듈 (2.4).

### 2.6 ★ 실패 값을 만드는 출구 — "0 인데 글이 없으면 `0xe`" `[정적]`

#### HRESULT 바꾸기 규칙

`GetLastError` 값 `e` 로 HRESULT 를 만든다 (`+0x1f4 ~ +0x1f9`):

| `e` | 돌려주는 값 |
|---|---|
| 양수 | `0x80070000 \| (e & 0xffff)` — 예: `0x7a` → `0x8007007a` |
| 음수 | 그대로 |
| 0 | **출구에 따라 다르다** ↓ |

#### 이름 단계 출구 (⑩) — 한 걸음씩

1. `+0x1f0 TEST EBX,EBX` — 오류값이 0 인가?
2. 0 이면 `+0x1f2 JZ +0x22f` → `+0x22f CMP [_pwszWinstaDesktop],R12` — 주소 글이 있나?
3. 있으면 (`+0x236 JNZ +0x1ff`) → 0 반환 (성공).
4. **없으면 `+0x238 MOV EBX,0xe`** → 양수라서 → `0x8007000e` → 반환.

- 곧 "**실패라는데 고장 메모가 비었고, 주소도 못 만들었다**" 면 combase 는 오류를 **`0xe` 로 갈아 끼운다.**
- run 문서 말: 요청 쪽 `[추측]` "묵은 `GetLastError` 를 HRESULT 로 바꿈" → 코드가 그렇게 쓰여 있다. 다만 0 일 때는 "바꿈" 이 아니라 **갈아 끼움** 이다.
- run131 `RegisterOleWndClass +0x13980b` 와 **같은 꼴** 이다 ([run131 해설 2.7](run131-user32-regclass-해설.md)).

비유: 그래픽 부서가 "몰라요" 하면서 고장 메모 칸을 비워 두면, COM 담당 건물은 "이유도 없이 안 됐네 — **메모리가 모자랐나 보다**" 하고 "메모리 부족" 도장 (`0x8007000e`) 을 찍는다. 그리고 **전화를 포기한다** (2.7).

### 2.7 ★ 에뮬레이터 막힘 — 줄로 따라가기 `[정적]`

요청 쪽 r14 로그 (에뮬레이터 — `[관찰]` 아님) 를 코드 줄에 맞췄다 (run 문서 4절).

| # | 일 | 그때 값 | 줄 |
|---|---|---|---|
| 1 | `GetProcessWindowStation` | `0x20000` (0 아님) → 통과 | `+0x73` · `+0x76` |
| 2 | `GetThreadDesktop` | `0x20400` → 통과 | `+0x99` · `+0x9c` |
| 3 | `GetUserObjectInformationW(0x20000, 2, bufA, 0x40, &len)` | stub: **반환 0 · 종이 안 씀 · `*len` 안 씀 · TEB `+0x68` 안 씀** | `+0xc8` · `+0xd6 JZ +0x358` |
| 4 | `GetLastError()` | TEB `+0x68` 에 남아 있던 값 — **0** 이면 | `+0x358` · `+0x366 CMP EAX,0x7a` (아님) |
| 5 | 출구 | `EBX = 0` 인데 주소 글은 아직 NULL | `+0x1f0` · `+0x1f2 JZ +0x22f` · `+0x236` |
| 6 | **`0x8007000e` 만들기** | `EBX = 0xe` | **`+0x238`** · `+0x1f9` · `+0x1ff` |
| 7 | 람다 | 음수 → **`Connect` 안 부름** | `operator() +0x9d` (안 뜀) · `+0xb5 Return_Hr` |
| 8 | `GetConnection` → `CoInitializeSecurity` | **같은 값 그대로 반환** | `GetConnection +0xc5` · `CoInitializeSecurity +0x486 JS` · `+0x8a9` |

- 3번이 핵심이다. stub 이 "0" 만 돌려주고 **아무 칸도 안 썼다.** 그래서:
  - combase 는 2.6 의 "0 인데 글이 없음" 출구로 간다.
  - `Connect` 까지 못 가고, `CoInitializeSecurity` 가 `0x8007000e` 로 실패한다 (요청 쪽 63 샘플).
- 8번의 "그대로" — `GetConnection +0xc5` 가 `0x80070005` (`E_ACCESSDENIED`) 만 따로 보는데, 그 갈래는 `[공백]` 이다 (Q257 ③). `0x8007000e` 는 거기 안 걸리고 그대로 올라간다.

#### 새 말 — 묵은 LastError

- 고장 메모 칸 (`TEB +0x68`) 은 **새로 쓰지 않으면 지난번 값이 남는다.**
- 그래서 "실패했는데 아무도 메모를 안 썼다" 면, `GetLastError()` 는 **이번 실패와 상관없는 예전 값** 을 돌려준다. 이것을 "묵은" 값이라 부른다.
- 비유: 아무도 안 지운 **지난주 메모** 를 오늘 고장 이유로 읽는 것.

#### `0x8007007a` 샘플 (요청 쪽 5) — 묵은 메모가 `0x7a` 였을 때

한 걸음씩:

1. 4번에서 `GetLastError()` 가 마침 `0x7a` 였다 — 앞서 누가 남긴 값이다. 누가 남겼는지는 근거에 없다.
2. combase 는 "종이 모자람" 으로 믿고 **두 번 묻기** 로 간다 (`+0x369 JZ`).
3. `len` 은 stub 이 안 써서 combase 가 먼저 넣은 **`0x40` 그대로** 다 (`+0xae`). 그래서 `HeapAlloc(0x40)` 뒤 다시 묻기도 `(0x20000, 2, buf, 0x40)` 꼴로 보인다 (flow-writer 보탬).
4. 또 0 → `GetLastError()` 는 여전히 `0x7a` → 양수라 바꾸기 규칙 → **`0x8007007a`**.

- 정리: 같은 "stub 이 아무것도 안 씀" 이 **묵은 메모에 따라** `0x8007000e` 도, `0x8007007a` 도 된다. 두 값 다 `.asm` 줄에서 그대로 나온다.

### 2.8 ★ r13 은 왜 달랐나 — 핸들 0 이면 출구가 다르다 `[정적]` (epmapper 이음은 `[추측]`)

같은 "0 + 고장 메모 0" 인데, **핸들 단계** 에서 나오면 결과가 다르다.

#### 근거 — 한 걸음씩

1. ③ 창 스테이션 핸들이 0 → `+0x76 JZ +0x23f`.
2. `+0x23f GetLastError` → 0 → `+0x24f JLE +0x2ce` → `EBX = 0`.
3. **`+0x27c JMP +0x1ff`** — ⑩ 의 `+0x1f0` (글 NULL 검사) 을 **안 거치고** 바로 반환 줄로 간다.
4. 그래서 **0 반환 + 글 NULL**.
5. 람다 `+0x9d JNS` — 음수가 아니니 계속 → `+0x742` 에서 NULL 을 RDX 에 싣고 → **`Connect(NULL)`**.

- ④ 데스크톱 핸들 0 (ⓑ) 도 같은 꼴이다: `+0x1d8fe7 ~ +0x1d8ff0` → `+0x1f4 JLE` — 역시 글 NULL 검사 뒤로 들어간다.
- 핸들 0 이어도 고장 메모가 **양수** 면 `0x8007xxxx` 로 멈춘다.
- `0x7f` (지연 로딩 실패) 면 user32 가 있나 · win32k 호출 금지 정책을 따로 본다 (`0x800704ec` 등) — 이번 막힘과는 무관.

#### 그래서

- 요청 쪽 r13 의 "**이름 안 묻고 바로 `\RPC Control\epmapper` 연결**" 과 맞는다.
- 출구 문이 둘인데, **이름 단계 문에만** "주소 없으면 `0xe`" 검사가 붙어 있다. 왜 그렇게 짰는지는 코드가 말하지 않는다.

#### 아직 확인 아님

- `Connect` → epmapper 로 이어진다는 것은 `[추측]` 이다. `Connect` 안을 안 읽었다 (Q257 ①).

비유: **상가 건물 열쇠부터 없으면** (핸들 0) COM 담당 건물은 "주소 없이" 그냥 COM 관리실에 전화한다. 그런데 **열쇠는 받았는데 이름을 못 알아내면** "메모리 부족" 도장을 찍고 전화를 포기한다. r13 은 앞 길, r14 는 뒤 길이었다.

#### 그러니 주의

- r14 에서 핸들을 `0x20000` 으로 고친 것 **자체는 맞다** (2.11). 그 덕에 이름 묻기까지 왔고, 거기서 **새 벽** (stub 의 빈 대답) 을 만난 것이다.
- 핸들을 다시 0 으로 돌리면 이번 막힘은 피하지만, 진짜 Windows 와 다른 길 (`Connect(NULL)`) 로 간다.

### 2.9 주소 글이 가는 곳 — `Connect` 둘째 인자 `[정적]` (일회용 objdump)

#### 새 말 — 람다 `operator()` · 일회용 objdump

- 람다 = 함수 안에 이름 없이 만든 작은 함수. C++ 컴파일러는 이것에 `operator()` 라는 이름을 붙인다 `[추측: 일반적으로 알려진 설명]`.
- 이번 근거: combase 에 `operator()` 가 **70개** 라 `export` 로 하나만 못 뽑았다. 그래서 `ms/disassemble/combase/_oneoff/` 에 **일회용 objdump** 셋 (`operator_0x18b9c0` · `GetConnection_0xa14d0` · `CoInitializeSecurity_0x7e880`) 을 만들어 읽었다 `[정적]`.

#### 길 — 한 걸음씩

1. `CoInitializeSecurity +0x47d GetConnection` — `gGotSecurityData` 가 0 일 때 (`+0xa6` · `+0xac JE`).
2. `GetConnection +0xbe operator()`.
3. 람다 `+0x80 CMP [_pwszWinstaDesktop],R15` · `+0x92 JNE +0x100` — **이미 있으면 안 묻는다.**
4. 없으면 `+0x94 GetThreadWinstaDesktop` → `+0x9d JNS +0x100`.
5. 성공 갈래: `GetModuleFileNameW(NULL, buf, 519)` (`+0x123`) → … → **`+0x742 MOV RDX,[_pwszWinstaDesktop]`** · `+0x751 LEA R8,[RBP+0x150]` (exe 경로 버퍼) → **`+0x82e CALL Connect`**.

- 곧 `Connect` 에는 적어도 **주소 글 (둘째)** 과 **exe 경로 (셋째)** 가 같이 간다.
- 글이 한 번 정해지면 다시 안 묻는다. **NULL 로 남으면 `GetConnection` 마다 다시 묻는다** (`operator() +0x80`).
- 같은 전역을 읽는 곳이 더 있다 (`DelegateActivationToSCM +0x1a1` · `Irot*` 다섯 · `IMgot*` 셋) — RPCSS 에 말 걸 때 같이 보내는 것으로 보인다 `[추측: 인자 자리 안 봄]`.

비유: COM 담당 건물 수첩에 **"우리 주소" 칸** (`_pwszWinstaDesktop`, rva `0x338118`) 이 하나 있다. 칸이 비어 있으면 그래픽 부서에 물어 채우고, 채워지면 **COM 관리실에 거는 전화마다** 그 주소를 붙인다. 이번에 막힌 건 **첫 전화를 걸기도 전** 이었다.

#### 한 가지 더 — 정해 둔 글 (`SetExplicitWinstaDesktopString`)

- `CoInitializeSecurity` 가 `dwCapabilities` 비트 14 (`0x4000`) 일 때만 부른다 (`+0x905`).
- 받은 글이 `WinSta0\Default` 면 (`_wcsicmp`) 상수 포인터를 쓴다. 그 밖 갈래는 run 문서 2절.
- 이번 막힘과 무관 (보통 이 비트를 안 켬 `[추측]`).

### 2.10 user32 없는 프로세스 · 대소문자 `[정적]` (test.exe 값은 `[관찰]`)

#### ② 갈래 — 핸들 대신 가게 서류

- `IsGetProcessWindowStationPresent()` 가 0 이거나 **PEB `+0x58` (`KernelCallbackTable`) 이 0** 이면 → `ResolveProcessDesktopNoUser` (rva `0x82a04`).
- 이 함수는 매개변수 `DesktopInfo` (`PEB+0x20` → `+0xc0`) 를 본다:
  1. 기본 이름과 **대소문자 무시** 로 같으면 (`RtlEqualUnicodeString`, `+0x51 MOV R8B,1`) → combase 상수 **`WinSta0\Default`** (`.rdata 0x1802c24e0`, 대문자 S) 를 전역에 두고 0.
  2. 다르면 `\` 가 맨 앞이 아닐 때 `DesktopInfo` 를 그대로 복사.
  3. 길이 모자람 · `\` 없음 · 맨 앞 `\` 이면 → `IsGetProcessWindowStationPresent()` 가 참일 때 **`0x32`** ("핸들로 물어라" → ③), 거짓이면 상수 글 + 0.
- 비유: 창 담당 건물을 안 들인 가게라면 그래픽 부서에 안 묻고, **가게 등록 서류** 에 적힌 주소를 그대로 쓴다.

#### run85 와 이어진 줄

- PEB `+0x58` 은 run85 의 user32 `DllMain` 이 놓는 칸이다 (run85 해설 "커널 콜백").
- 이번에 combase 가 그 칸을 "**user32 가 정말 일을 시작했나**" 의 표시로 읽는다는 게 드러났다 (`+0x5a CMP [RAX+0x58],R12`).
- 요청 쪽 로그가 `NtUserGetProcessWindowStation` 에 닿았으니, 에뮬레이터 PEB `+0x58` 은 0 이 아니라고 **거꾸로 읽힌다** `[추측]` (Q256 ④).

#### 대소문자 — `WinSta0` 인가 `Winsta0` 인가

- 커널 객체 이름은 **만든 쪽이 준 글자 그대로** 라, win32k 의 이름 읽기 코드가 정하지 않는다.
- 근거로 가리키는 값:
  - win32kfull `.rdata` `WinSta0` (`0x36fee8`, `xxxResolveDesktop +0x3f4` 등이 읽음) · `\Windows\WindowStations\WinSta0` (`0x36f1c0`) — **대문자 S** → `[정적 근거로는 WinSta0]`.
  - run44 이름 있는 객체 목록의 `WinSta0_DesktopSwitch` · `CicLoadWinStaWinSta0` `[관찰]` — 다른 객체 이름이라 근거는 아니고, 대문자 S 이름이 있다는 정도 `[추측]`.
- 실제 창 스테이션 객체 이름은 `[공백]` → Q256.
- test.exe 의 `DesktopInfo` `Winsta0\Default` (소문자 s) `[관찰]` 는 **프로세스 매개변수 글** 이다. 커널 객체 이름과 **별개** 다. 헷갈리기 쉬운 곳.

### 2.11 ★ stub 이 돌려줄 값 — PE_emulator 쪽 (결과서 판정, run 문서 표)

`../PE_emulator` 코드는 flow-writer 가 안 읽었다. 고칠지는 사용자 결정이다.

#### 지금 것 맞나 · 틀리나

| 자리 | 판정 | 까닭 |
|---|---|---|
| `NtUserGetProcessWindowStation` → `0x20000` | **맞다** | combase 는 0 인지만 본다 (`+0x73`). 진짜 값은 `TODO(VM)` 유지 (Q256) |
| `NtUserGetThreadDesktop` → `0x20400` | **맞다** | `+0x99`. 같은 이유 |
| `NtUserGetObjectInformation` 기본 대답 "반환 0 · 종이 안 씀" | **틀림 — 막힘의 원인** | 2.7 |

#### 종류 2 (이름) — 새 stub 이 할 일

- **종이가 넉넉할 때** (`nLength ≥ 16`):
  - 반환 **1 (TRUE)**.
  - 종이에 UTF-16 **`WinSta0`** (창 스테이션) / **`Default`** (데스크톱) + **`0x0000`** → **16 B**.
  - `*lpnLengthNeeded = 16`.
- **종이가 작을 때** (`nLength < 16`):
  - 반환 **0**.
  - TEB `+0x68` = **`0x7a`**.
  - `*lpnLengthNeeded = 16`. 종이엔 **안 쓴다.**
- 필요 길이는 **성공 · `0x7a` 실패 모두** 쓴다 (`Edition… +0x183 ~ +0x18f`). 포인터가 NULL 이면 안 쓴다.
- 대소문자는 정적 근거로 `WinSta0`. 데스크톱 정확한 글자는 `[공백]`. 둘 다 확정은 Q256.

#### 그 밖

- **종류 1** (`0xc`, 353 샘플 — combase 아님): 반환 1 · `{fInherit (핸들 속성 — [공백], Q256), 0, dwFlags = 1 [추측]}` · `*lpnLengthNeeded = 0xc`. 모자라면 0 + `0x7a` + `0xc`.
- **`(0x20400, 2, buf, 0x208)` · `(0x20000, 2, buf, 0)`** — 위 규칙으로 된다. `0x208` → 16 B 쓰고 TRUE. 길이 0 → FALSE + `0x7a` + 16 (combase 면 `HeapAlloc(16)` 뒤 다시 묻기).
- **실패를 흉내 낼 때**: 고장 메모는 **TEB 칸에 직접** (`+0x68`, WoW64 면 `+0x2034` 도). user32 는 안 써 준다 (2.3). 안 쓰면 묵은 값이 읽힌다 (2.7).
- **핸들을 0 으로 돌릴 때 (r13)**: 길은 맞다. 0 + 고장 메모 0 → `Connect(NULL)`. 고장 메모 > 0 이면 `0x8007xxxx` 로 멈춘다.
- **PEB `+0x58`**: **영향 있음.** 0 이면 핸들을 안 묻고 `DesktopInfo` 길 (상수 `WinSta0\Default`). 지금은 0 아님으로 읽힌다 `[추측]` (Q256 ④).

한 줄로 (run 문서): **stub 은 "종류 2 → TRUE + UTF-16 이름 + NUL + 필요 길이 (바이트, NUL 포함)", 길이 모자라면 "FALSE + TEB `+0x68` = `0x7a` + 필요 길이" 를 돌려주면 이 막힘을 넘는다. 다음 벽은 `Connect` (RPCSS) — Q257.**

비유: 그래픽 부서 흉내를 낼 때는 **문패 이름 + 끝 표시** 를 종이에 적어 주고, "몇 칸 썼어요" 메모도 꼭 붙여 준다. 종이가 작으면 "몇 칸 필요해요" 메모 + 수첩에 "종이 모자람" 을 **직접** 적어 준다. 그러면 COM 담당 건물은 "`WinSta0\Default`" 주소를 만들어 COM 관리실에 전화를 걸러 간다. 그 전화가 다음 벽이다.

### 2.12 flow-writer 대조 · 보탬 (결과서와 다른 곳)

run 문서 "flow-writer 대조" 를 그대로 옮긴다. 결과서 오프셋은 이것 말고 **모두 일치** 했다.

- `_GetUserObjectInformation` 의 이른 실패는 필요 길이 줄 (`+0x37e`) 을 **안 지난다** → 바깥이 로컬 0 을 써서 그때 `*lpnLengthNeeded = 0` (2.5).
- `UserSetLastError` 는 남의 프로세스에 붙어 있으면 안 쓴다 (보탬).
- `Cleanup +0x108` 은 `_pwszWinstaDesktop` 에 **쓴다** (결과서는 `Cleanup +0xd2` 를 "읽기만" 에 넣었다).
- 다시 묻기의 크기: stub 이 필요 길이를 안 쓰면 `0x40` 그대로 (2.7, 보탬). 요청 쪽 `(…, 2, buf, 0)` 8 샘플은 그대로 `[추측]`.
- `GetUserObjectInformationA +0xa1 TEST EDI,EDI` · `+0xa3 JNS` — 결과서는 `+0xa3` 만 적음, 뜻 같음.
- `refs 0x363578` 넷째 줄 이름이 `LAB_18025b3de+0x3d (함수 밖)` 로 나오는데 `.asm` 의 `+0x1d90eb` 와 같은 자리.

### 2.13 정리 — 비유로 한 번 더

- COM 담당 건물 (combase) 은 COM 관리실 (RPCSS) 에 첫 전화를 걸 때 **"건물\방" 주소** 를 붙인다.
- 주소를 알려고 상가 건물 · 방의 **열쇠** (핸들) 를 받고, 그래픽 부서에 **이름** 을 한 번씩 묻는다. 종이는 `0x40` 바이트.
- 창 담당 건물 (user32) 은 **돌림 단추** 일 뿐이라, 그래픽 부서의 대답이 그대로 온다.
- 진짜 그래픽 부서는 **문패 이름 + 끝 표시** (16 B) 를 적어 주고, 종이가 작으면 "몇 칸 필요" + "종이 모자람 (`0x7a`)" 을 준다. 고장 메모는 **손님 수첩에 직접** 쓴다.
- 에뮬레이터 그래픽 부서는 "**몰라요**" 만 하고 아무것도 안 적었다 → 수첩엔 묵은 메모 (0 이나 `0x7a`) → "메모리 부족" (`0x8007000e`) 또는 "종이 모자람" (`0x8007007a`) 도장 → **전화 포기**.
- 열쇠부터 없으면 (r13) 주소 없이 그냥 전화하러 간다 — 출구 문이 달라서다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git show a941bf3 -- runs/03_sprint/flow.md`)

그림은 안 바뀌었다. 바뀐 곳:

| 절 | 전 | 후 |
|---|---|---|
| 기준 user32 · win32u · combase 줄 | run131 까지 | run136 몫 덧붙임 — user32 썽크 셋 + A, win32u 스텁 둘 (`0x1065` · `0x101f`), combase export 3 + depth 1 · refs 8 · 일회용 objdump |
| 기준 win32kbase 줄 | run86 `objdump` 만 | **처음 analyze** (`_index.tsv` 9,144줄 · `FUN_` 0), export 3 |
| 기준 win32kfull 줄 | 없음 | **새 줄** — 처음 analyze (`_index.tsv` 10,099줄 · `FUN_` 1), 구조체 타입 없음, export 8 · refs 4 · 함수 표 칸 `+0x388` · `+0x390` |
| 근거 줄 | — | run136: 새 사진 읽기 없음, run44 덤프 읽기 로그에서 비교 값만 다시 짚음 |
| 3장 run85 절 | PEB `+0x58` 을 user32 가 놓는다 | + 한 줄: **combase 가 읽는다** (`GetThreadWinstaDesktop +0x5a`) `[정적]` |
| 3장 run131 절 | `RegisterOleWndClass` 의 "`GetLastError` 0 → `0x8007000e`" | + 한 줄: **같은 꼴이 COM 보안 쪽에도** (`+0x238`) |
| 3장 끝 | — | **새 절 "창 스테이션 · 데스크톱 이름 묻기 (COM 보안)"** `[정적]` — 비유 한 줄, 나무 하나, 요약 여섯 (user32 안 낌 · 묻는 꼴 · 실패 값 · 커널 답 · 막힘 · user32 없는 프로세스) |
| 4장 run86 정적 ① ② | win32k 안쪽은 PE_emulator · 사용자 결정 | + 한 마디: 이제 analyze 돼 있어 하게 되면 export 만 |
| 4장 run131 정적 4 | combase 지연 가져오기 공용 꼬리 안과 요청 이름 `l1-1-0` ↔ 스키마 `l1-1-1` 버전 허용 (`[추측]`) | + 한 마디: run136 에도 같은 자리 (`ext-ms-win-ntuser-windowstation-l1-1-0` ↔ 스키마 `l1-1-2`) |
| 4장 | Q250 대기 | **Q250 닫힘 줄** + run136 새 줄 셋 (동적 Q256 · 정적 Q257 · PE_emulator) |
| 5장 근거 run | — | run136 두 줄 |

- 태그 변화: 이번 절은 처음부터 `[정적]` 이다. 이전 run 정정은 없다.

### 큰 그림에서 어떻게 이어지나

- **run131 과 짝이다.** combase 의 두 입구 — `CoInitializeEx` (창 클래스 등록) · `CoInitializeSecurity` (주소 글) — 가 둘 다 "커널이 0 + 고장 메모 0 이면 `0x8007000e`" 함정을 갖고 있다. 에뮬레이터는 **stub 이 실패를 흉내 낼 때 고장 메모를 직접 써야 한다** 는 규칙을 두 번 만났다.
- **run85 와 한 줄 이어졌다.** user32 `DllMain` 이 놓는 PEB `+0x58` 이 combase 의 갈림길 (② 갈래) 이 된다.
- **run129 · run133 쪽으로 다음 걸음.** 막힘을 넘으면 `Connect` → (아마) `\RPC Control\epmapper` → RPCSS 다. 그 연결 틀 (첫인사 · 상태 표) 은 이미 run129 · run133 이 봤다. `Connect` 가 그 틀에 무엇을 싣는지가 Q257 이다.
- **win32kbase · win32kfull 이 이제 열려 있다.** run86 의 남은 일 (Q163 ① ②) 이나 Q257 ② (핸들 묻기 커널 본문) 는 analyze 없이 export 만으로 간다.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q256** (동적, VM 묶음 — 미룸)

test.exe 가 아니라 **user32 를 올리고 COM 보안을 여는 GUI 프로세스** 가 필요하다. 고르기는 사용자 결정이다. 조각 안 주소는 `combase+rva` 로 건다.

1. **이름 대소문자 · 핸들 값**
   - 무엇을 모르나: 진짜 창 스테이션 · 데스크톱 이름의 글자 (`WinSta0`?), 핸들 값, 필요 길이 16 이 맞나, 만든 주소 글.
   - 왜 못 알았나: 객체 이름은 **만든 쪽이 준 글자** 라 win32k 읽기 코드에 없다. 핸들 값은 실행 때 값이다.
   - 멈춤점 예: `combase+0x823a3` (`+0x73`) `r rdi` · `combase+0x823ff` (`+0xcf`) `du @rbp-0x49` · `dd @rbp-0x59 L1` · `combase+0x8252f` (`+0x1ff`) `du poi(combase+0x338118)`.
2. **종류 1 진짜 값**
   - 무엇을 모르나: `fInherit` (핸들 속성) 과 창 스테이션 객체 `+0x40` 비트 `0x4` 가 실제로 꺼져 있나 (→ `dwFlags` 1).
   - 왜 못 알았나: 객체 안 값 · 핸들 속성은 실행 때 값이다.
   - 멈춤점: `win32kfull!_GetUserObjectInformation +0x31b` · `+0x33a`. 세션 드라이버라 GUI 프로세스 문맥 (`.process /i`) 이 필요하다.
3. **종류 1 (`0xc`) · `0x208` 을 부른 모듈**
   - 왜 못 알았나: combase 가 아니라는 것까지만 정적으로 알 수 있다 `[공백]`.
   - 에뮬레이터 샘플에서 호출 스택 (`caller+0x…`) 으로.
4. **에뮬레이터 PEB `+0x58` 이 0 이 아닌지** — 에뮬레이터 쪽에서 한 번.

### 정적으로 남은 것 → **Q257**

1. **다음 벽 `Connect`** (rva `0x12f318`, `operator() +0x82e`)
   - 무엇을 모르나: 바인딩 핸들 · exe 경로 · 주소 글이 어떤 꼴로 가나. 같은 람다의 `CreateFromBindingString` (`+0x33c`) · `CreateFromBindingHandle` 셋이 `\RPC Control\epmapper` (run129 · run133) 로 어떻게 이어지나.
   - 왜 못 알았나: 이번 범위는 "묻기 · 출력 꼴 · 실패 값 · 글 만들기까지" 였다. `Connect` 안은 안 읽었다. 그래서 r13 의 "핸들 0 → 바로 epmapper" 와 `Connect` 의 이음도 지금 `[추측]`.
2. **`NtUserGetProcessWindowStation` · `NtUserGetThreadDesktop` 커널 본문** — 핸들 값 · 핸들 속성 (`fInherit`) 이 어디서 정해지나. win32kbase · win32kfull 은 이제 analyze 돼 있어 export 만.
3. **`GetConnection +0xc5` `E_ACCESSDENIED` (`0x80070005`) 갈래** · `CoInitializeSecurity` 앞부분 (`+0x7a` `Query` · `+0x87` `IsApartmentInitialized`).

### 대기열 밖 — `[공백]` 으로 남은 것

- 데스크톱 종류 1 `dwFlags` 비트 이름.
- `_GetUserObjectInformation` 종류 4 · 5 · 6 · 8 출력.
- syscall → win32k.sys 스텁 → win32kbase 다리 · 함수 표 건네기 (이름 · 칸 번호만 근거).
- 지연 가져오기 `l1-1-0` → `l1-1-2` 허용 (run131 정적 4 와 같은 자리).

---

## 5. 더 읽기

- run 문서: [`run136-winsta-name.md`](run136-winsta-name.md)
  - "정적 — 호출 지점" 표 (call · ret 짝 전부)
  - 2절 차례 표 (①~⑩, ⓐ ⓑ), 3절 종류 1 · 2 출력 칸 표, 4절 막힘 표, "PE_emulator 쪽" 표
- 요청서: [`_workspace/sast/134_winsta-name.md`](../../_workspace/sast/134_winsta-name.md)
- 결과서: [`_workspace/sast/134_winsta-name_result.md`](../../_workspace/sast/134_winsta-name_result.md) — "먼저 읽는 한 줄 답", 2-2 묻는 것 표, 3-4 출력 칸 표, "동적으로 확인할 것"
- flow.md: [`flow.md`](flow.md) 3장 끝 "창 스테이션 · 데스크톱 이름 묻기 (COM 보안)" 절, 4장 run136 줄 셋, 3장 run85 · run131 절의 run136 한 줄
- 앞 해설: [run131 해설](run131-user32-regclass-해설.md) (같은 `0x8007000e` 꼴), [run85 해설](run85-user32-init-해설.md) (PEB `+0x58`), [run129 해설](run129-winnsi-rpc-해설.md) · [run133 해설](run133-lrpc-wire-해설.md) (다음 벽 쪽 전화 틀)
- 대기열: `_workspace/plan.md` Q250 (끝) · Q256 · Q257

# run92 bcrypt-ksecdd — 해설

> 이 글은 [run92 문서](run92-bcrypt-ksecdd.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q172** — PE_emulator 요청 32 (bcrypt.dll `DllMain` attach 가 `\Device\KsecDD` 에서 꼭 받아야 하는 답) (`_workspace/plan.md`, 이번에 **끝**)
  - **한 바퀴로 끝났다.** 사용자 결정 (2026-11-14): Q104 VM 단계 앞 멈춤 자리에서 끼워 **지금 먼저**, 정적만.
  - 다음 항목: **[멈춤 — Q104 VM 단계 앞, 사용자 확인 대기]**.
  - 이번에 남은 것이 간 곳 (4절):
    - 정적 → 새 **Q173** (작은 것 넷 + `NtTraceControl` × 4 구성)
    - 동적 → 새 **Q174** (test.exe 로는 못 본다. bcrypt 를 올리는 샘플 고르기는 **사용자 결정**. Q171 과 묶기 후보)
- 근거:
  - run 문서 [`run92-bcrypt-ksecdd.md`](run92-bcrypt-ksecdd.md) (가장 중요)
  - 요청서 [`_workspace/sast/88_bcrypt-ksecdd.md`](../../_workspace/sast/88_bcrypt-ksecdd.md) · 결과서 [`88_bcrypt-ksecdd_result.md`](../../_workspace/sast/88_bcrypt-ksecdd_result.md)
    - 판정 **분석성공**. 요청서 질문 5개 모두 `.asm` 줄로 답했다.
    - bcrypt 를 **처음 analyze** 했다 (PDB `5F1077ED8D3ABC83AD7D378F94EA41901`, 함수 311, 지도 `_map/entry_*` 106 함수 · 깊이 10, 38개 뽑음).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · 흐름도 앞 글 · **3장 새 절** · 4장 · 5장. **그림은 안 바뀜** (3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 새 짐 칸 없음, 부르는 쪽 메모 하나 (마흔여덟째판)
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없다.
  - `[관찰]` 로 적힌 값은 flow-writer 가 run44 사진 로그를 **다시 읽은 것** 이다 (2.14).

### 이번 바퀴에서 조심할 곳 여섯

1. **바로 앞 run91 과 "같은 꼴, 다른 DLL" 이다.**
   - run91 = **bcryptprimitives**.dll, 장치 `\Device\CNG`, 주문서 `0x390008`.
   - run92 = **bcrypt**.dll, 장치 `\Device\KsecDD`, 주문서 `0x390400`.
   - 이름이 비슷해서 헷갈리기 쉽다. 이 글에서 "bcrypt" 는 늘 **bcrypt.dll** 이다.
2. **test.exe 는 bcrypt 를 안 올린다.**
   - run44 사진의 초기화 끝 (P2) 모듈 6개에 없다 `[관찰]`. `\KnownDlls` 목록에도 없다 `[관찰]`.
   - sample 소스에도 `crypt` · `bcrypt` · `ksecdd` 글자가 없다 `[소스]`.
   - 그래서 이번 이야기도 **"이 DLL 을 올리는 다른 프로세스라면"** 이다. 흐름도 **그림은 그대로** 다.
3. **"로그" 는 진짜 Windows 로그가 아니다.**
   - 요청서의 syscall 차례는 PE_emulator 가 흉내 낸 실행의 로그다 `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]`.
   - 이 글의 결론은 전부 코드를 읽은 것 `[정적]` 이다 (→ 실행 확인은 Q174).
4. **이름이 둘이다: Ghidra `entry` = PDB `DllMain`** (rva `0x11140`). run91 과 같은 함정이다.
   - 이 글은 `DllMain` 으로 쓴다.
5. **비유 하나 주의 — 핸들은 "손잡이", 이벤트는 "신호등".**
   - flow.md 3장 새 절의 비유는 은행 창구 · 핸들 = "**번호표**" · 이벤트 = "**벨**" 이다.
   - 이 해설 시리즈에서 "번호표" 는 이미 시스템 콜 번호 ([run29 해설](../02_sprint/run29-continue-start-해설.md)), "신호등" 은 이미 이벤트 ([run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등)) 다.
   - 그래서 이 글은 run91 해설처럼 **손잡이**, 그리고 **신호등** 으로 부른다. 그림만 다르고 사실은 같다.
6. **결과서와 run 문서가 다르게 적은 곳이 한 줄 있다 → run 문서를 따른다** (2.15 ①). 결론은 같다.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, [핸들](../02_sprint/run9-create-user-process-해설.md#핸들) = **손잡이**
- 전원 켜기 = DLL 시작 함수 (`DllMain`) 부르기, 이유 1 = 프로세스에 처음 붙음, 꼬리 `JMP` → [run25 해설](../02_sprint/run25-dll-entry-해설.md)
- `0xc0000142` = DLL 초기화 실패, "최소 조건 표" 라는 꼴 → [run85 해설](run85-user32-init-해설.md) 2.6 · [run86 해설](run86-gdi32-init-해설.md)
- **출력 칸 (핸들 칸)**, 바구니 · 손잡이 걸이 (캐시), `\Device\CNG`, 환경값 (`g_TrustedEnvironment`) · 트러스트렛, 점프 표, WPP → [run91 해설](run91-bcrypt-init-해설.md) 2.2 · 2.3 · 2.4 · 2.9 · 2.11
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = **번호 붙은 주문서** → [run66 해설 2.7](run66-apphelp-cache-해설.md)
- 이벤트 = **신호등** → [run13 해설 2.3](../02_sprint/run13-resume-thread-해설.md#23-멈춤-은-줄에서-빼는-게-아니다--쪽지--신호등), 자동 이벤트 (`Type 1`) → [run77 해설](run77-thread-wake-해설.md)
- 세마포어 = 횟수를 세는 신호등 → [run53 해설](run53-ethread-create-해설.md)
- 크리티컬 섹션 (임계영역) = 한 번에 한 명만 들어가는 방의 **열쇠** → [run68 해설 2.7](run68-dll-init-rest-해설.md#27-락--새-말-크리티컬-섹션)
- 독점 잠금 / 공유 잠금 → [run23 해설](../02_sprint/run23-load-dependent-해설.md) ("새 말 — 독점 잠금 / 공유 잠금")
- 고정 (pin) = 명부 줄에 "**철거 금지**" 도장 → [run21 해설 2.9](../02_sprint/run21-load-dll-internal-해설.md)
- `LdrDisableThreadCalloutsForDll` · `DontCallForThreads` → [run68 해설 2.12](run68-dll-init-rest-해설.md)
- ETW = **방송국**, 등록 = **채널** 올리기 → [run62 해설](run62-tp-etw-init-해설.md)
- 예외 보호 구역 · **착지점** → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일)
- `\KnownDlls` = 동네 **공용 창고**, KnownDll 길 · 파일 길 → [run21 해설](../02_sprint/run21-load-dll-internal-해설.md) 2.4
- 사진 P1a · P2, raw 읽기 → [run44 해설 2.8](run44-baggage-snap-해설.md)

이번에 처음 나오는 말 — **`\Device\KsecDD`**, **기능 번호**, **`0xc0000008`**, **`-1` "없음" 표시**, **`STATUS_PENDING`**, **읽기·쓰기 잠금 (`RTL_RESOURCE`)**, **주소로 찾기 (`RtlPcToFileHeader`)**, **DOS 경로**, **C++ 꾸민 이름** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 커널 = **관리사무소**, syscall = 관리사무소 **신청 창구**, ntdll = 가게 안 **관리실**.
- DLL = 가게에 들이는 **건물**. 시작 함수 (`DllMain`) = 건물 **전원 켜기**. DLL 전역 = 건물마다 가진 **수첩**.
- 드라이버 = 관리사무소 건물 안 **전문 상담실**, IOCTL = 상담실에 넣는 **번호 붙은 주문서**. 핸들 = **손잡이**. 이벤트 = **신호등**.
- run91 에서 붙인 그림: 창구에 **바구니** 를 내밀어 손잡이를 받고, 받은 손잡이는 건물 벽 **손잡이 걸이** (캐시) 에 건다.
- 이번에 새로 붙는 그림:
  - **bcrypt = "암호 앞쪽 건물"**. flow.md 기준 줄이 bcrypt 를 "CNG 공개 API 층" 이라고 적었다. 프로그램이 암호 일을 부탁할 때 먼저 말을 거는 건물이다.
    - run91 의 bcryptprimitives (**암호 부품 건물**) 와 다른 건물이다.
  - 이 건물은 전원을 켤 때, 관리사무소 건물 안 **KsecDD 상담실** 에 **신청서** 한 장을 낸다: "**설정이 바뀌면 이 신호등을 켜 주세요.**"
  - 이번엔 **바구니가 따로 없다.** 창구에 **벽의 손잡이 걸이를 통째로** 내민다 (2.3).

### 한 줄로 먼저 — 무엇을 얻었나

**이번 글의 중심은 이 한 줄이다.**

> **status 는 "성공" 인데 핸들 칸이 비어 있다 → (run91 처럼 "못 열었다" 로 돌아서지 않고) 빈 칸을 그대로 들고 상담실에 간다 → 커널이 `0xc0000008` → 다시 해 보지 않고 네 층을 올라가 `DllMain` FALSE.**

뿌리는 run91 과 **같다** — 엔진의 `NtOpenFile` 이 "status 0 + 핸들 칸 비움" 을 준 것 하나 `[정적]`.
**모양이 다르다.** 먼저 다른 점 넷을 눈에 담고 시작하자 (자세히는 2.12).

| | run91 bcryptprimitives | **run92 bcrypt** | 이 글 |
|---|---|---|---|
| ① 열고 나서 핸들 0 검사 | **한다** (호출자 `GetSeedFromKernelState +0x29`) | **안 한다** — 빈 칸 그대로 IOCTL 로 → 커널이 `0xc0000008` | 2.4 |
| ② 재시도 | 5번 | **없음** (한 번 열고 한 번 IOCTL) | 2.5 |
| ③ IOCTL 출력 | 시드로 씀. `0x103` 이면 kernelbase 가 기다림 | **버림**. `0x103` 도 안 기다림 | 2.7 |
| ④ `GetModuleHandleExW(5, …)` 실패 | TRUE | **FALSE** (bcrypt 만의 둘째 FALSE 갈래) | 2.10 |

요청서 질문에 대한 답 (run 문서 "원문 질문이 어디서 답해졌나"):

| 요청 32 질문 | 답 (run 문서 그대로) |
|---|---|
| 1 여는 함수 · 무엇을 보나 · FALSE 자리 · 칸 이름 | `_IoOpenDevice` (`IoCallKernelDriver +0x1b5`) 가 연다. 보는 건 `NtOpenFile` status 와 IOCTL status, **핸들 칸은 안 봄**. 칸 = **`g_hIoDevice`**. FALSE 는 `DllMain +0x189` 한 점, 오는 길 둘 `[정적]` (2.3 ~ 2.5, 2.10) |
| 2 IOCTL `0x390400` 입력 · 출력 · TRUE 조건 · 실패 | 입력 `0x68` = 머리만 (상수 `0x1a2b3c4d` · 기능 `0x10500` · "없음" 표시 `-1` 여럿 · **이벤트 핸들 `+0x60`**). 출력 8B 는 **안 읽음**. 성공 status 만. 실패면 FALSE `[정적]` (2.6 · 2.7) |
| 3 TRUE 뒤 첫 사용 (이름만) | `IoCallKernelDriver` · **`BCryptResolveProviders`** (기능 `0x20000`, **출력을 읽음**) · `IoCallKernelDriverQueryLoop` · `IoShutdown` `[정적]` (3절) |
| 4 NtQIP `0x2b` · 세마포어 · 이벤트 · `0x56` | `0x2b` 는 **결과를 버림**. 세마포어 둘 = 읽기·쓰기 잠금. 이벤트 = IOCTL 입력에 들어감. `0x56` = run91 과 같은 `IsEnclave` `[정적]` (2.8 · 2.9) |
| 5 최소 조건 표 | 바깥이 꼭 맞출 것은 **둘** — `NtOpenFile` 이 0 아닌 핸들을 칸에 쓰고, 그 핸들로 IOCTL 이 status ≥ 0. 그리고 `GetModuleHandleExW` 성공 `[정적]` (2.11) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

**DLL 전원 켜기** 구간이다 (flow.md 2.7). test.exe 에는 없는 건물이라 **옆길** 이다. run85 · run86 · run91 과 같은 자리다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스 · 주소 공간 · PEB · 첫 스레드
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk
 │    └─ LdrpInitializeProcess
 │         ├─ (2.6) import DLL 들이기 — test.exe 는 dll1 · dll2
 │         │     (bcrypt 를 올리는 프로세스라면: \KnownDlls 에 없으니 파일 길 [추측: 실행 안 봄])
 │         └─ (2.7) DLL 전원 켜기 — LdrpCallInitRoutine
 │               ├─ (test.exe) dll2 → dll1
 │               └─ (bcrypt 를 올리는 프로세스라면) bcrypt!DllMain (이유 1)   ← 이번 (run92)
 │                     FALSE 면 → 로더가 DLL 을 내림 → 0xc0000142
 └─ … → NtContinue (은퇴) → exe 진입점 → main
```

- flow.md 3장 새 절: "bcrypt 를 올리는 프로세스에서 2.7 의 `LdrpCallInitRoutine` 이 부르는 진입점 (이유 1) 이 하는 일".
- 3장에서 바로 위 bcryptprimitives 절 (run91) 뒤, 3장 끝에 놓였다.

### 왜 이 질문이 지금 왔나

- PE_emulator 요청 32 다.
- PE_emulator 샘플 **221개** (악성 57 / 정상 164) 가 같은 자리에서 막힌다.
  - bcrypt `DllMain` (이유 1) 이 **FALSE** → 로더가 `0xc0000142`.
  - 같은 기본 대답을 받은 샘플은 531개다 (요청서 배경).
- 사용자 결정 (2026-11-14): Q104 VM 단계 앞 멈춤 자리에서 끼워 **지금 먼저**, 정적만. 커널 `ksecdd.sys` 의 `0x390400` 처리는 범위 밖.

### 무엇이 궁금했나

요청서 질문 다섯.

1. **`\Device\KsecDD` 를 여는 함수와 FALSE 자리** — 그 함수가 보는 것이 **`NtOpenFile` status / 핸들 칸 (`+0x25ae0`) / IOCTL status** 중 무엇인가. 핸들 칸의 PDB 이름.
2. **IOCTL `0x390400`** — 입력 `0x68` 바이트에 무엇이 들어가나 (세마포어 둘 · 이벤트가 들어가나), 출력 8B 를 어디에 쓰나, **TRUE 가 되려면 답이 무엇이어야 하나**, 실패면 어디로 가나.
3. **TRUE 뒤 첫 사용** (이름만).
4. (작게) NtQIP class `0x2b` 이름 · 결과가 TRUE/FALSE 에 닿나, 세마포어 · 이벤트의 쓰임, class `0x56` 이 run91 과 같은가.
5. **최소 조건 표** — run91 표와 나란히 볼 수 있게.

### 바퀴 전에 알던 것

- `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]` attach 안 syscall 차례 (221/221 같은 꼴):
  1. `NtTraceControl` 여럿
  2. `NtQueryInformationProcess(-1, 0x56, …, 0xb0)` (run91 `IsEnclave` 꼴)
  3. `NtQueryInformationProcess(-1, 0x2b, …, 0x218)`
  4. `NtTraceControl` × 4
  5. `NtCreateSemaphore` × 2 (접근 `0x100003`, 처음 0, 한도 `0x7fffffff`)
  6. `NtCreateEvent` (접근 `0x1f0003`, 넷째 인자 1)
  7. **`NtOpenFile(\Device\KsecDD)`** — 핸들 칸 = **bcrypt + `0x25ae0`**. 접근 `0x100003` · 공유 7 · 옵션 `0x20` · 속성 0 · 루트 0. 엔진은 **status 0 을 주지만 핸들 칸은 안 씀**.
  8. **`NtDeviceIoControlFile(핸들 0, …, 코드 0x390400, 입력 0x68, 출력 8)`** → 핸들 0 이라 `0xc0000008`.
  9. `NtClose` × 5 → `DllMain` FALSE.
- `[정적]` (run91) bcryptprimitives `GetCngHandle` 은 `NtOpenFile` status 만 보고 핸들 칸 값을 그대로 돌려준다. IOCTL `0x390008` 출력 내용은 안 본다. **bcrypt 가 같은 꼴인지는 몰랐다.**
- `[추측]` (요청서) 세마포어 둘 · 이벤트는 KsecDD 와 나누는 무언가 — IOCTL 입력에 핸들로 들어갈 것.
- 우리 repo 에 bcrypt 분석은 0 이었다.

---

## 2. 찾은 것

### 2.1 한눈에 — attach 줄기

run 문서 "호출 관계" 를 줄였다. 모두 `[정적]`.

- `InitializeCNG` 안 줄은 **실행 차례** 와 조금 다르다. 실제로는 `+0x13` 에서 환경값이 0 이면 `+0x66` 으로 가서 `GetTrustedEnvironment` 를 부르고, 그 뒤 `+0x2e` 부터 이어간다 (run 문서 차례 표 6 → 8).
- ★ = 이번 막힘이 지나가는 줄.

```
bcrypt!DllMain (Ghidra entry, rva 0x11140)   +0x23 / +0x26  이유 1 만 아래로
 ├─ +0x2c  ntdll!LdrDisableThreadCalloutsForDll(자기 hModule)        syscall 없음 (2.15 ③)
 ├─ +0x80  WppInitUm · +0x85 McGenEventRegister_EtwEventRegister       반환 안 봄
 ├─ +0x8a  InitializeCNG (rva 0xb1c0)                                                      ★
 │     ├─ +0x66  GetTrustedEnvironment → IsEnclave +0x44 NtQIP(0x56) · IsTrustlet +0x34 NtQIP(0)   (run91 과 같음)
 │     ├─ +0x2e  InitializeLoader                                       늘 0
 │     ├─ +0x40  InitializeConfig                                       늘 0
 │     │     ├─ +0x31  InitializeProcessImageNameUM → kernelbase +0xb1 NtQIP(0x2b, 0x218)   결과 버림
 │     │     ├─ +0x46  TlgRegisterAggregateProviderEx                   반환 안 봄
 │     │     └─ +0x62  [꼬리 JMP] IoStartup → +0x10 g_hIoDevice = 0
 │     └─ +0x52  InitializeSystemPreferredCache (rva 0x59e0)                               ★
 │           ├─ +0x38  RtlInitializeResource → ntdll +0xb3 · +0xe2 NtCreateSemaphore ×2
 │           ├─ +0x98  RtlAllocateHeap(0x28) · +0xb7 RtlInitializeCriticalSection
 │           └─ +0xd0  BcpRegisterConfigChangeNotifyNoLogging (rva 0x5bf4)                 ★
 │                 ├─ +0x68   CreateEventW → kernelbase +0x76 NtCreateEvent        → rva 0x25b10
 │                 ├─ +0xda · +0x136  IoPack_Configuration_ParamBlock   입력 0x68 채움
 │                 └─ +0x15a  IoCallKernelDriver(0x10500, 입력, 0x68, NULL, NULL) (rva 0x7f50)   ★
 │                       ├─ +0x1b5  _IoOpenDevice (rva 0x8154) → +0x7a NtOpenFile(&g_hIoDevice, …)  ★
 │                       │    +0x1d0 TEST R12D · +0x1d3 JNS +0x8e   ← status 만 봄                 ★
 │                       └─ +0xc2   NtDeviceIoControlFile([g_hIoDevice], 0x390400, 입력 0x68, 출력 8)  ★
 │                                  +0xd0 JS +0x15c → 음수 반환              ★ (로그: 0xc0000008)
 │  +0x93 JZ +0xca  → 아니면 +0x189 FALSE ①                                                 ★ (막힘)
 ├─ +0x12d  GetModuleHandleExW(5, &DllMain, &h) → kernelbase +0x88 RtlPcToFileHeader · +0x130 LdrAddRefDll
 │  +0x13b JNZ +0x1c5  → 아니면 +0x189 FALSE ②     (bcrypt 만 — 2.10)
 └─ +0x1c5  MOV EAX,1   TRUE
```

- 첫눈에 run91 과 다른 점: **줄기가 깊다.** `NtOpenFile` 까지 `DllMain` → `InitializeCNG` → `InitializeSystemPreferredCache` → `BcpRegister…` → `IoCallKernelDriver` → `_IoOpenDevice`, 다섯 계단을 내려간다.
  - 그래서 결과서는 지도 (`entry` 뿌리, 깊이 10) 로 길을 먼저 찾고, 그 줄기만 정규식으로 뽑았다 (run 문서 "방법").
- 그 밖에 `DllMain` 이 부르는 것 — `+0x41 _DllMainCRTStartupForGS2` · `+0x46 SafeAllocaInitialize` · `InitializeCNG +0x6 SymCryptInitEnvWindowsUsermodeWin8_1nLater` — 은 반환을 안 본다 (run 문서 차례 표 3 · 6).

### 2.2 한 줄씩 읽는 법

#### 줄 1: `→ +0x7a NtOpenFile(&g_hIoDevice, …)` (`_IoOpenDevice` 안)

- **누가**: bcrypt 의 `_IoOpenDevice` (rva `0x8154`) `[정적]`.
  - 이 함수는 `IoCallKernelDriver +0x1b5` 에서 불리고 `+0x1ba` 로 돌아간다.
- **어디서**: `+0x7a` 에서 부르고 `+0x81` 로 돌아온다.
- **누구를**: ntdll `NtOpenFile` → 신청 창구.
- **무엇을**: `\Device\KsecDD` 를 연다. 인자를 하나씩 보면 (run 문서 호출 지점 표 · 결과서 1-b):
  - **첫째 `&g_hIoDevice`** = 핸들을 받을 칸의 주소 (`+0x66 LEA RCX,[g_hIoDevice]`). ← 이번 이야기의 주인공 (2.3).
  - `0x100003` = 접근 (`+0x5a`). 결과서 풀이로 `SYNCHRONIZE | FILE_READ_DATA | FILE_WRITE_DATA`.
  - OA = 이름 `\Device\KsecDD` (`+0x2a RtlInitUnicodeString`, 문자열 `.rdata` rva `0x1f098`) · 크기 `0x30` (`+0x5f`) · **속성 0** · 루트 0.
    - run91 의 CNG 는 속성 `0x40` 이었다. 여기는 0 이다.
  - 공유 `7` (`+0x6d`), 옵션 `0x20` (`+0x4a`, 결과서 풀이로 `FILE_SYNCHRONOUS_IO_NONALERT`).
  - **에뮬레이터 로그 값 (`0x100003` · 7 · `0x20` · 속성 0 · 루트 0) 과 다 같다.** 결과서는 이걸 "로그와 코드가 같은 DLL" 이라는 증거로 썼다.
- **무엇을 위해**: 신청서 (IOCTL) 를 넣을 **상담실 손잡이** 를 얻으려고.

**새 말 — `\Device\KsecDD`**

- [`\Device\CNG`](run91-bcrypt-init-해설.md) (run91 2.2) 와 같은 종류 — 커널 이름 공간의 **장치 이름** 이다. 이 이름으로 열면 그 장치를 맡은 드라이버에게 말을 걸 수 있다 `[추측]` (일반적으로 알려진 설명).
- 결과서 · run 문서는 이 장치의 `0x390400` 처리 쪽을 `ksecdd.sys` 로 적었다. 그 안은 **범위 밖** 이라 안 읽었다 (Q173 ③, 후순위).
- 비유: 관리사무소 건물 안 **또 다른 상담실** — "KsecDD 상담실" 문패. run91 의 "난수 상담실" (`\Device\CNG`) 과는 다른 방이다.

#### 줄 2: `└─ +0xc2 NtDeviceIoControlFile([g_hIoDevice], 0x390400, 입력 0x68, 출력 8)` (`IoCallKernelDriver` 안)

- **누가**: `IoCallKernelDriver` (rva `0x7f50`).
- **어디서**: `+0xc2` 에서 부르고 `+0xc9` 로 돌아온다. `+0x1d3 JNS +0x8e` 로 뛰어 온 뒤다.
- **누구를**: ntdll `NtDeviceIoControlFile` → 신청 창구를 거쳐 상담실로.
  - run91 은 kernelbase `DeviceIoControl` 을 거쳤다. 여기는 bcrypt 가 **직접** 부른다.
- **무엇을**: 인자 열 개 (결과서 2-a):

| 인자 | 값 | 줄 |
|---|---|---|
| 핸들 | **`[g_hIoDevice]` — 칸 값 그대로** | `+0x8e MOV RCX,…` |
| Event / APC / Context | 0 / 0 / 0 | `+0xae` · `+0xa7` · `+0x9f` (`XOR`) |
| IOSB | 지역 `[RSP+0x50]` | `+0x95` |
| 코드 | **`0x390400`** (늘 같은 상수) | `+0xb5` |
| 입력 / 길이 | `BcpRegister…` 의 지역 버퍼 `[RSP+0x80]` / `0x68` | `+0xb0` · `+0xaa` |
| 출력 / 길이 | 지역 `[RSP+0x60]` / 8 | `+0x73` · `+0x78` |

- **무엇을 위해**: "설정이 바뀌면 이 신호등을 켜 주세요" 신청서를 넣으려고 (2.6).

### 2.3 ★ 먼저 이것 — 바구니가 곧 걸이다

run91 해설 2.3 에서, `NtOpenFile` 의 대답은 **두 군데** 로 온다고 했다.

- **status** → 반환값 (`EAX`) — 결과 **도장**
- **핸들** → 첫 인자가 가리키는 칸 — 실제로 받은 **손잡이**

이번 엔진도 run91 과 똑같이 **도장만 찍고 칸은 안 썼다** `[관찰 — 에뮬레이터 로그]`.
그런데 **그 칸이 어디인가** 가 run91 과 다르다 `[정적]`.

| | run91 bcryptprimitives | run92 bcrypt |
|---|---|---|
| `NtOpenFile` 첫 인자 | 지역 칸 `[RBP+0x10]` (**바구니**) | **전역 `g_hIoDevice`** (rva `0x25ae0`) 의 주소 |
| 받은 뒤 | 바구니 값을 `LOCK CMPXCHG` 로 걸이 (`g_hCNGDriver`) 에 걸고, 그 값을 **돌려줌** | 옮기는 일 없음 — 이미 **걸이에 들어가 있음** |
| 누가 0 인지 보나 | 호출자 (`GetSeedFromKernelState +0x29`) 가 본다 | **아무도 안 본다** (2.4) |

- 즉, bcrypt 는 **바구니를 따로 쓰지 않고, 벽의 걸이를 통째로 창구에 내민다.** 창구가 걸이에 손잡이를 걸어 주면 그걸로 끝이다.
- 결과서: "지역 변수에 받아서 옮기는 게 아니라 **전역에 곧바로 쓴다**" (1-b).
- 이 걸이는 한 번 차면 계속 쓴다. TRUE 뒤에도 `IoCallKernelDriver` 는 다음 IOCTL 마다 같은 핸들을 쓴다 — "한 번 열면 다시 안 엶" (결과서 3).

**"이미 열었나" 는 두 번 본다 — 열기 전에만** `[정적]`

| 자리 | 하는 일 |
|---|---|
| `IoCallKernelDriver +0x7e ~ +0x88` | 걸이 (`g_hIoDevice`) 를 읽고, **0 이면 `+0x88 JZ +0x196`** 으로 (열러 감) |
| `+0x196 ~ +0x1cb` | `EnterCriticalSection(g_csIoInitialize)` — [열쇠](run68-dll-init-rest-해설.md#27-락--새-말-크리티컬-섹션)를 쥐고 `+0x1a9` 에서 **다시 0 인지** 본다 → 그래도 0 이면 `+0x1b5 _IoOpenDevice` → `NtOpenFile` → `LeaveCriticalSection` |

- 걸이를 보는 건 여기까지다. **열고 난 뒤엔 걸이를 다시 보지 않는다** (다음 절).
- 왜 두 번 보나 `[추측]`: 열쇠 밖에서 한 번, 열쇠 안에서 한 번 보는 꼴은 두 스레드가 동시에 여는 것을 막으려는 흔한 모양으로 알려져 있다 (일반적으로 알려진 설명). run91 은 같은 일을 `LOCK CMPXCHG` 로 했다.
- 걸이를 처음 0 으로 만드는 곳은 `IoStartup +0x10` 이다. `InitializeConfig +0x62` 에서 꼬리 `JMP` 로 들어간다. 이 열쇠 (`g_csIoInitialize`) 도 여기서 만든다 (`+0x1e`).

**새 말 — C++ 꾸민 이름**

- 걸이의 PDB 이름은 그냥 `g_hIoDevice` 가 아니라 **`?g_hIoDevice@@3REAXEA`** 로 적혀 있다 (run 문서 "환경").
- C++ 로 짠 코드에서 컴파일러가 이름에 **종류 · 모양 정보를 덧붙여 만든 긴 이름** 으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- `IoCallKernelDriver` 도 PDB 에는 `?IoCallKernelDriver@@YAJKPEAEK0PEAK@Z` 하나뿐이다 (`--where` 로 확인).
- 비유: 이름표에 이름만이 아니라 생년월일 · 주소까지 붙여 쓴 **정식 이름**. 부를 땐 짧은 이름으로 부르지만, 명부엔 긴 이름으로 적혀 있다.
- 이게 왜 중요하나 → WinDbg 멈춤점 이름 (2.16).

### 2.4 ★ `IoCallKernelDriver` 안 — 도장만 보고, 빈 걸이를 든 채 상담실로

에뮬레이터 실행을 그대로 따라간다. 처음엔 걸이가 0 이다 (`IoStartup +0x10`).

| 자리 | 코드가 하는 일 `[정적]` | 걸이 `g_hIoDevice` |
|---|---|---|
| `+0x55` · `+0x5b` | 입력 `+0x0` ← `0x1a2b3c4d`, `+0x4` ← 기능 번호 `0x10500` (2.6) | 0 |
| `+0x5e ~ +0x78` | 출력 칸 고르기 — 지역 `[RSP+0x60]` 8B 로 (2.7) | 0 |
| `+0x7e ~ +0x88` | 걸이가 0 → `+0x196` 으로 | 0 |
| `+0x196 ~ +0x1a9` | 열쇠 쥐고 다시 0 인지 → 0 | 0 |
| **`+0x1b5`** | **`_IoOpenDevice` → `+0x7a NtOpenFile(&g_hIoDevice, …)`** — 엔진은 status 0, 칸 안 씀 `[관찰 — 에뮬레이터 로그]` | **0 그대로** |
| `+0x1ba` | `MOV R12D,EAX` — status 를 `R12D` 에 보관 | 0 |
| **`+0x1d0` · `+0x1d3`** | **`TEST R12D,R12D` · `JNS +0x8e`** — status 가 음수인가? 아니다 → `+0x8e` 로 뜀 | 0 |
| **`+0x8e`** | **`MOV RCX,[g_hIoDevice]`** — 걸이 값을 **검사 없이** 꺼낸다 | 0 → `RCX` = 0 |
| **`+0xc2`** | **`NtDeviceIoControlFile(0, …)`** → 커널이 `0xc0000008` (엔진도 같은 답) | 0 |
| `+0xce` · **`+0xd0`** | `TEST EAX,EAX` · **`JS +0x15c`** — 음수 → 실패 갈래 | 0 |
| `+0x15c ~ +0x168` | `0xc0000023` 이면 `0xc00000e5` 로 바꾸는 갈래 — 아니다. `+0x168 JNZ +0x101` → **`0xc0000008` 반환** | 0 |

결과: **`IoCallKernelDriver` 는 `0xc0000008` 을 돌려준다.**

**새 말 — `0xc0000008`**

- 결과서가 `STATUS_INVALID_HANDLE` 로 적은 값이다. "그 손잡이는 못 쓰는 손잡이예요" 라는 뜻으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 맨 앞 비트가 1 인 음수 = 실패 status 다.
- 비유: 빈손으로 상담실 문을 열려고 하니, 상담실이 "**손잡이가 없는데요**" 하고 돌려보낸다.

여기서 눈여겨볼 점 셋.

1. **코드는 status 만 본다.**
   - `+0x1d0` · `+0x1d3` 가 보는 건 `NtOpenFile` 의 status 뿐이다.
   - 걸이가 비었는지는 **열고 난 뒤 따로 묻지 않는다.** `+0x85 TEST RAX,RAX` · `+0x88 JZ` 는 열기 **전** 의 "이미 열었나" 검사다.
2. **빈 걸이를 발견하는 건 bcrypt 가 아니라 커널이다.**
   - run91 은 건물 직원 (`GetSeedFromKernelState +0x29`) 이 "빈손이네" 하고 **스스로** 돌아섰다. 그래서 IOCTL 이 로그에 0번이었다.
   - bcrypt 는 빈손인 줄 모르고 상담실 문까지 간다. 그래서 로그에 IOCTL 이 **핸들 0** 으로 찍혔다.
3. **엔진이 틀린 곳은 하나뿐이다** (결과서 1-c, run 문서).
   - `NtOpenFile` 이 status 0 만 주고 `*FileHandle` 을 안 쓴 것.
   - 그 뒤 "핸들 0 → `0xc0000008`" 은 **진짜 커널과 같은 답** 이라 맞다.

**왜 코드가 걸이를 다시 확인 안 하나** `[추측]`

- 근거: run91 해설 2.4 에서도 같은 질문이 나왔다. 진짜 커널은 성공이면 늘 핸들을 쓰는 것으로 보인다 `[추측]` (run91 문서).
- 그래서: 이번에도 "도장이 성공이면 걸이도 찼다" 는 창구의 약속을 믿는 꼴로 보인다. 걸이가 비면 어차피 커널이 IOCTL 에서 막아 준다.
- 아직: 진짜 핸들 모양은 Q174 의 `_IoOpenDevice+0x81` 멈춤점에서 본다.

### 2.5 ★ 다시 해 보지 않는다 — 네 층을 올라가 FALSE

#### `0xc0000008` 이 올라가는 길 `[정적]`

run 문서 "FALSE 로 가는 자리" 아래 세 줄을 한 줄로 이어 붙였다.

1. **`BcpRegisterConfigChangeNotifyNoLogging`**
   - `+0x15a` 에서 부르고 `+0x15f` 로 돌아옴 → **`+0x163 JS +0x1e1`**.
   - → `+0x1e9 CloseHandle(이벤트)` — 방금 만든 신호등을 치운다 → **음수 그대로 반환**.
2. **`InitializeSystemPreferredCache`**
   - `+0xd0` 에서 부르고 `+0xd5` 로 돌아옴 → **`+0xd9 JNS +0x157` 안 탐**.
   - → `+0x145 TEST` · `+0x147 JNS` 안 탐 → `+0x149 CleanupSystemPreferredCache` (자물쇠 · 세마포어 치우기) → **그 status 반환**.
3. **`InitializeCNG`**
   - `+0x52` 에서 부르고 `+0x57` 로 돌아옴 → `+0x59 TEST` · **`+0x5b JNZ +0x6d`**.
   - → `+0x86 UninitializeCNG` → `+0x8b JMP +0x25` → **그 status 반환**.
   - `InitializeCNG` 에서 0 아닌 값이 나오는 곳은 **여기 하나뿐** 이다. `InitializeLoader` · `InitializeConfig` 는 늘 0 (run 문서 "확인된 것").
4. **`DllMain`**
   - `+0x8a` 에서 부르고 `+0x8f` 로 돌아옴 → `+0x91 TEST` · **`+0x93 JZ +0xca` 안 탐**.
   - → `+0xa6 JZ` / `+0xb0 JZ` → **`+0x189 XOR EAX,EAX`** → `+0x18b JMP +0x1ca` → **FALSE ①**.
   - (WPP 기록이 켜져 있으면 `+0xc5 JMP +0x170` → `+0x184 WPP_SF_sDsd` 를 들렀다 같은 `+0x189` 로 온다.)
5. **로더 (ntdll)** — `DllMain` 이 FALSE 면 로더가 DLL 을 내리고 `0xc0000142` (요청서 배경).

- 층마다 **자기가 만든 것만 치우고** 같은 status 를 위로 넘긴다. 바꿔 끼우지 않는다.
- 결과서: 로그의 FALSE 는 "`+0x5b JNZ` → `+0xd9` → `+0x163 JS` → `+0xd0 JS` 길 그대로" — **엔진이 맞게 재현했다**.

#### 고리가 없다 — 그래서 `NtOpenFile` 이 1번

- run91 은 `InitUmRootRngState +0x129 ~ +0x137` 에 **"5번까지 다시"** 고리가 있었다 → `NtOpenFile` × 5.
- bcrypt 의 이 길엔 그런 고리가 없다 `[정적]` — 한 번 열고 한 번 IOCTL, 실패하면 바로 위로.
- 그래서 로그의 `NtOpenFile` 이 **1번** 이다. run 문서: "PE_emulator 로그의 `NtOpenFile` 1번 + `NtDeviceIoControlFile`(핸들 0) 과 한 칸도 안 어긋난다."

#### 로그 모양으로 어디서 막혔나 가르기

run91 해설 2.5 처럼 경우를 나란히 놓아 보자. 각 줄은 run 문서 차례 표 · FALSE 자리 표의 갈래를 읽은 것이다 `[정적]`.

| 경우 | `NtOpenFile` | `NtDeviceIoControlFile` | 끝 |
|---|---|---|---|
| A. status 0 + **칸 비움** (이번) | 1번 | 1번 (**핸들 0** → `0xc0000008`) | FALSE ① |
| B. status 음수 (열기 실패) | 1번 | **0번** (`+0x1d9` 에서 그 status 반환) | FALSE ① |
| C. 손잡이 받음, IOCTL 이 음수 | 1번 | 1번 (핸들 ≠ 0) | FALSE ① |
| D. 손잡이 받음 + IOCTL ≥ 0 | 1번 | 1번 | 다음 줄 (`GetModuleHandleExW`, 2.10) 로 |

- run91 에선 **횟수** (5번 / 1번) 로 경우를 갈랐다.
- bcrypt 에선 A · C · D 의 횟수가 같다. 그래서 로그에서 A 를 알아보는 건 **IOCTL 의 핸들 인자가 0 인가** 다.

### 2.6 주문서 `0x390400` — 용건은 주문서 안에 적는다

#### 새 말 — 기능 번호

- run91 의 주문서 `0x390008` 은 번호 자체가 용건이었다 ("씨앗 48개 주세요").
- bcrypt 의 `0x390400` 은 **늘 같은 번호** 다 (`IoCallKernelDriver +0xb5`, 상수). 진짜 용건은 **입력 버퍼 `+0x4` 칸** 에 적는다. 이것을 run 문서는 **기능 번호** 라고 부른다 `[정적]`.
  - attach 가 적는 기능 번호 = **`0x10500`** (`BcpRegister… +0x155 MOV ECX,0x10500` → `IoCallKernelDriver +0x5b`). 결과서 이름은 "설정 바뀜 알림 등록".
  - 같은 `0x390400` 으로 `BCryptResolveProviders` 는 **`0x20000`** 을 적는다 (`+0x590`). 결과서는 `0x390400` 을 "CNG 설정 · 공급자 질의용 **만능 통로**" 라고 불렀다.
- 비유: 상담실 주문서 **양식은 한 가지** 다. 맨 위 "**용건 번호**" 칸에 무엇을 적느냐로 일이 달라진다. 전원 켜기 때 적는 용건은 "설정 바뀌면 알려 주세요" 하나다.

#### 입력 `0x68` 바이트 — 칸 표 (run 문서 그대로, attach 의 실제 인자로 계산)

| 오프셋 | 크기 | 값 | 출처 |
|---|---|---|---|
| `+0x00` | 4 | `0x1a2b3c4d` (고정) | `IoCallKernelDriver +0x55` |
| `+0x04` | 4 | **`0x10500`** (기능 번호) | `IoCallKernelDriver +0x5b` ← `BcpRegister… +0x155` |
| `+0x08` · `+0x18` · `+0x38` | 4 | 0 | `IoPack_Configuration_ParamBlock +0xd2` · `+0xe0` · `+0xe6` |
| `+0x0c` | 4 | 0 | 안 씀 (버퍼 `memset`, `BcpRegister… +0x2d ~ +0x40`) |
| `+0x10` · `+0x20` · `+0x28` · `+0x30` | 8 | `-1` (문자열 넷 없음) | `IoPack… +0x11a` · `+0x12a` · `+0x136` · `+0x142` |
| `+0x40` · `+0x48` | 8 | `-1` (`CRYPT_CONTEXT_CONFIG` · `…_FUNCTION_CONFIG` 없음) | `IoPack…` |
| `+0x50` | 4 | 0 (덩어리 크기) | `IoPack… +0x10a` |
| `+0x58` | 8 | `-1` (덩어리 없음) | `IoPack… +0xf4` · `+0x172 IoPack_Buffer` |
| **`+0x60`** | 8 | **이벤트 핸들** (rva `0x25b10` 의 값) | `IoPack… +0xf8` · `+0xff` ← `BcpRegister… +0xa8` |

**새 말 — `-1` "없음" 표시**

- 이 양식에는 "문자열이나 덩어리가 **어디 붙어 있나**" 를 적는 칸이 여럿 있다. 붙일 게 없으면 그 칸에 `-1` 을 적는다 (`IoPack_Buffer +0xd OR [RCX],-1`) `[정적]`.
- 비유: 서류의 "첨부 서류" 칸마다 "**없음**" 도장.
- 이번엔 첨부가 하나도 없어서 양식이 **머리만** 이다 — 합 `0x68` (`IoPack… +0x4a MOV EDI,0x68`). 로그의 입력 길이 `0x68` 과 같다.

#### 요청서의 짐작은 반만 맞았다

- 요청서 `[추측]`: "세마포어 둘 · 이벤트가 IOCTL 입력에 핸들로 들어갈 것".
- 이번 `[정적]`:
  - **이벤트 하나만 들어간다** (`+0x60`).
  - **세마포어 둘은 안 들어간다.** 세마포어는 전혀 다른 일에 쓴다 (2.8).
  - **PID · 스레드 ID · 함수 주소도 안 들어간다.** 입력을 만드는 어디에도 `GetCurrentProcessId` · 코드 포인터가 없다 (결과서 2-b).
- 엔진 입장: **입력은 읽을 필요가 없다.** 만들 것은 입력이 아니라 **답** (status) 이다 (run 문서).

### 2.7 ★ 출력은 버린다 — 그리고 기다리지도 않는다

#### 출력 칸은 어디서 오나 (run 문서 보탬 ②) `[정적]`

`BcpRegister…` 는 `IoCallKernelDriver` 에 **출력 버퍼도, 출력 길이 포인터도 NULL** 로 넘긴다 (결과서: `+0x14a XOR R9D,R9D`, `+0x150 MOV [RSP+0x20],R15` = 0).
그러면 `IoCallKernelDriver` 가 자기 지역 칸을 대신 쓴다.

1. `+0x5e TEST R14` · `+0x61 JZ +0x121` — 출력 길이 포인터 (5번째 인자 `R14`) 가 NULL → `+0x121`.
2. `+0x121 R15D = 0` · `+0x124 JMP +0x6a`.
3. `+0x6a TEST RDI` · `+0x6d JNZ` 안 탐 — 출력 버퍼 (`RDI`) 도 NULL.
4. `+0x73 LEA RSI,[RSP+0x60]` · `+0x78 R15D = 8` — **제 지역 8B 를 출력 칸으로**.

- 그래서 로그의 "출력 길이 8" 이 나온다. 부른 쪽이 원한 출력이 아니라, `IoCallKernelDriver` 가 **혼자 마련한 받침 접시** 다.

#### 성공 길에서 출력을 안 본다 `[정적]`

- IOCTL 이 status ≥ 0 이면 `+0xd6 TEST R14,R14` · **`+0xd9 JZ +0xff`** — 출력 길이 포인터가 NULL 이니 곧장 `+0xff` → **`XOR EAX,EAX` (0 반환)**.
- 출력 8B 도, IOSB (`[RSP+0x50]`) 도 **아무도 안 읽는다.** 전역에 저장하는 줄도 없다 (결과서 2-c).
- 그래서 "출력 값 모양까지 보나" → **안 본다**. "출력이 0 이어도 되나" → **된다**.

비유: 상담실이 "**접수됐어요**" 라고만 하면, 건물 직원은 받침 접시에 놓인 영수증을 **보지도 않고 버린다**.

#### 새 말 — `STATUS_PENDING` (`0x103`)

- "아직 처리 중이에요, 끝나면 알려 줄게요" 라는 뜻의 status 로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 맨 앞 비트가 0 이라 **음수가 아니다**.
- bcrypt 는 `+0xd0 JS` 로 **부호만** 본다 → `0x103` 도 **성공으로 친다** `[정적]`.
- 그리고 **기다리지 않는다** `[정적]`:
  - Event 인자가 0 이다 (기다릴 대상이 없다).
  - attach 길에 `NtWaitForSingleObject` 가 없다 (지도 · `_calls.tsv` 에 없음).
- run91 과 비교: kernelbase `DeviceIoControl` 은 `0x103` 이면 `+0x1de NtWaitForSingleObject` 로 **기다렸다** (run91 해설 2.7).
- 결과서 `[추측]`: 동기 열기 (`0x20`) 라서 진짜 Windows 에선 I/O 관리자가 알아서 기다려, PENDING 이 안 올 것.
- 그래도 엔진은 **0 이 안전** 하다 (run 문서).

### 2.8 이벤트 · 세마포어 — 무엇에 쓰나

#### 이벤트 하나 — 상담실에 맡기는 신호등 `[정적]`

- `BcpRegister… +0x68` (ret `+0x6f`) `CreateEventW` → kernelbase `+0x76` (ret `+0x7d`) **`NtCreateEvent(&h, 0x1f0003, NULL, 1, 0)`** (로그 6번).
  - 넷째 인자 1 = **자동** 이벤트 ([run77 해설](run77-thread-wake-해설.md): 지나가면서 저절로 빨강), 다섯째 0 = 처음엔 **꺼짐** (run 문서 "자동 · 꺼짐").
- `+0x74` 에서 칸 `[RSI]` (= 전역 rva `0x25b10`, PDB 이름 없음) 에 저장한다.
- `+0x77 TEST RAX` · `+0x7a JZ +0x1af` — NULL 이면 `GetLastError` → `WinErrorToNtStatus` → 음수 → FALSE ①.
- 이 핸들이 **입력 `+0x60`** 에 들어간다 (2.6).
  - 결과서 `[추측]`: 커널 (ksecdd) 이 "설정이 바뀌었다" 를 알리려고 받는 신호.
  - 나중에 `RegisterReopenWait +0x49/+0x50 RegisterWaitForSingleObjectEx` 로 기다린다. **attach 안에선 안 기다린다.**
- 엔진은 이 신호등을 **켜 주지 않아도 된다** (결과서 4).

비유: 건물이 신호등을 하나 세우고, 그 **손잡이** 를 신청서에 끼워 상담실에 맡긴다. "설정 바뀌면 이걸로 불 켜 주세요."

#### 세마포어 둘 — 건물 수첩 한 쪽을 지키는 자물쇠 `[정적]`

- `InitializeSystemPreferredCache +0x19` (ret `+0x20`) `RtlAllocateHeap(…, 0x60)` → 전역 **`g_pCachedRngRWLock`** (rva `0x25bc8`).
- `+0x38` (ret `+0x3f`) `RtlInitializeResource` → ntdll **`+0xb3` · `+0xe2 NtCreateSemaphore`** (로그 5번, 접근 `0x100003` · 처음 0 · 한도 `0x7fffffff`). `+0xc4 JS` 로 실패를 본다.

**새 말 — 읽기·쓰기 잠금 (`RTL_RESOURCE`)**

- `RtlInitializeResource` 가 만드는 잠금이다. 결과서는 "**세마포어 2개를 든 `RTL_RESOURCE`**" 라고 적었다 (사용처 표).
- 일반적으로 "여럿이 **같이 읽기**" 와 "혼자 **고치기**" 를 나눠 허락하는 잠금으로 알려져 있다 `[추측]` (일반적으로 알려진 설명) — [run23 해설](../02_sprint/run23-load-dependent-해설.md) 의 공유 · 독점 잠금과 같은 생각이다.
- 세마포어 ([횟수를 세는 신호등](run53-ethread-create-해설.md)) 둘이 그 안에서 줄 선 사람을 깨우는 데 쓰인다고 보면 된다 `[추측]`.
- 비유: 건물 수첩의 **어떤 한 쪽** 에 다는 자물쇠. 읽는 사람은 여럿 같이, 고치는 사람은 혼자.
- 그 "한 쪽" 이 무엇인가: 결과서는 시스템 기본 난수 공급자 핸들 두 칸 (rva `0x25ac8`, `0x25ad0`) 을 지키는 잠금으로 봤다 `[추측]`.
  - 쓰는 곳: `BCryptGenRandom +0x12a` · `GetSystemPreferredRngHandle +0x45` · `ReopenSystemPreferredRngHandle` · `CleanupSystemPreferredCache` (결과서 3).
- **KsecDD · IOCTL 과는 상관없다.**

#### 그 밖 — 힙 `0x28` 열쇠 하나

- `+0x98` `RtlAllocateHeap(…, 0x28)` → rva `0x25b18`, `+0xb7 RtlInitializeCriticalSection` — [열쇠](run68-dll-init-rest-해설.md#27-락--새-말-크리티컬-섹션) 하나 (syscall 없음).
- 결과서: 위 `RegisterReopenWait` 의 대기 등록을 감싼다.

### 2.9 NtQIP 두 번 — `0x56` 은 run91 그대로, `0x2b` 는 답을 안 본다

#### class `0x56` — run91 과 같은 함수, 같은 오프셋 `[정적]`

- `GetTrustedEnvironment +0x4 IsEnclave` → **`+0x44` (ret `+0x4b`) `NtQueryInformationProcess(-1, 0x56, buf, 0xb0)`**, 0 이면 `+0xd IsTrustlet` → `+0x34 (-1, 0, buf, 0x40)`.
- run91 해설 2.9 에서 본 것과 **같다** (같은 오프셋). 진짜 26100.9457 커널은 class `0x56` 에 `0xc0000003` 을 준다 → class 0 을 한 번 더 → 환경값 1.
- 엔진의 "성공 + 0 칸" 도 환경값 1 이 나와서 괜찮다 (결과서 4).

**다른 점 하나 — 환경값이 갈래를 가르는 자리** `[정적]`

| 환경값 | bcryptprimitives (run91) | **bcrypt (run92)** |
|---|---|---|
| 1 (일반) | 이번 이야기 | 이번 이야기 |
| 8 · `0x10` (엔클레이브) | 시드 함수 바꿈 / `0x10` 은 FALSE | **`InitializeCNG +0x15 TEST AL,0x18` · `+0x17 JZ`** 안 탐 → `+0x19` 로 **초기화를 통째로 건너뛰고 0 → TRUE** (KsecDD 안 건드림) |
| 4 (트러스트렛) | FALSE | **`BcpRegister… +0x8e TEST AL,1` · `+0x90 JZ +0x16e`** 로 **IOCTL 을 건너뜀** |

- run 문서: 이 갈래들은 정상 프로세스 모양이 아니라 엔진이 **쓰지 말 것**. 즉 class `0x56` 이 성공이면 첫 DWORD 가 1 · `0x10` · `0x11` 이 아니어야 하고, class 0 답의 `+0x38` bit 7 은 0 이어야 한다.

#### class `0x2b` — 가게 이름을 묻고, 답은 안 본다 `[정적]`

- 길: `InitializeConfig +0x31` (ret `+0x36`) → `InitializeProcessImageNameUM +0xb9` (ret `+0xc0`) → kernelbase `QueryFullProcessImageNameW +0xb1` (ret `+0xb8`) → **`NtQueryInformationProcess(-1, 0x2b, buf, 0x218, &len)`** (로그 3번).
  - 환경 `& 1` 일 때만 이 길로 간다 (`InitializeConfig +0x1f / +0x22` · `+0x2d / +0x2f`). 일반 프로세스는 간다.
- `.asm` 에 숫자 `0x2b` · `0x218` 이 바로 안 보인다 — 계산해서 만든다 (결과서 4):
  - class = `+0xae LEA EDX,[RBX+0x1b]`, `EBX` = `0x10` → `0x2b`.
  - 길이 = `+0x74 LEA R15D,[0x10+RCX*2]` → `0x10 + 2×0x104` = `0x218` (UNICODE_STRING 머리 `0x10` + 글자 260개).
- **답이 무엇이든 TRUE/FALSE 에 안 닿는다** — `InitializeConfig` 는 `EAX` 를 안 본다. `QueryFullProcessImageNameW` 가 실패해도 로그만 남기고 끝난다 (run 문서 "결과 버림").
- 이름: PDB 에 `PROCESSINFOCLASS` 열거형이 없다 (PDB 4개 모두 0건). 공개 헤더 기억으로 `ProcessImageFileNameWin32` `[추측]`.
  - 뒷받침 `[정적]`: 커널 `NtQueryInformationProcess` 의 class `0x2b` 처리기 (`+0x1424`, 점프 표로 계산) 가 `+0x148a PsReferenceProcessFilePointer` → **`+0x14b9 IoQueryFileDosDeviceName`** 을 부른다.
  - 비교로 class `0x1b` (`+0xe21`) 는 `+0xe84 PsQueryFullProcessImageName` 을 부른다 (결과서: NT 경로).

**새 말 — DOS 경로**

- `C:\…` 처럼 드라이브 글자로 시작하는, 사람이 흔히 보는 파일 경로로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 커널 안에서 쓰는 NT 경로 (`\Device\…` 꼴) 와 대비된다.
- `IoQueryFileDosDeviceName` 이라는 이름이 "DOS 쪽 이름을 묻는다" 는 뜻이라, class `0x2b` 가 DOS 경로를 주는 칸이라는 짐작의 근거가 된다.
- 비유: 가게 주소를 "관리사무소 장부 번지" 가 아니라 "**우편 주소**" 로 달라는 것.

### 2.10 ★ bcrypt 만의 FALSE ② — `GetModuleHandleExW(5, &DllMain)`

#### 무엇을 하나 `[정적]`

- `InitializeCNG` 가 0 을 돌려주면 `DllMain +0x93 JZ +0xca` 로 성공 길을 탄다.
- (WPP 기록 갈래 `+0xca ~ +0x11c` 를 지나) `+0x121` · `+0x126` 에서 인자를 싣고 **`+0x12d` (ret `+0x134`) `GetModuleHandleExW(5, &DllMain, &h)`**.
- `+0x139 TEST` · **`+0x13b JNZ +0x1c5`** — 성공이면 `+0x1c5 MOV EAX,1` → **TRUE**.
- 실패면 `+0x141 GetLastError` → `+0x159 JZ` / `+0x15f JZ` → `+0x189` → **FALSE ②**.

플래그 **5** = 고정 (PIN) + 주소로 찾기 (run 문서).

- **고정** = 명부 줄에 "**철거 금지**" 도장 ([run21 해설 2.9](../02_sprint/run21-load-dll-internal-해설.md)). attach 가 TRUE 로 끝나면 "DLL 은 고정 (PIN) 된다" (run 문서).
- **새 말 — 주소로 찾기 (`RtlPcToFileHeader`)**:
  - 보통은 DLL **이름** 으로 손잡이를 찾는다. 여기선 이름 대신 **자기 함수 주소** (`&DllMain`) 를 준다.
  - kernelbase 안에서 `+0x88 RtlPcToFileHeader` 가 "이 주소 → 이미지 시작" 을 찾고, `+0x130 LdrAddRefDll` 이 고정을 한다 (run 문서).
  - 비유: 건물 안 **자기 방 위치** 를 대며 "이 방이 든 건물이 어느 건물이죠? 그 건물에 철거 금지 도장 찍어 주세요."
- ntdll 쪽 본문 (`RtlPcToFileHeader` · `LdrAddRefDll`) 은 **안 읽었다** `[공백]` → Q173 ①.

#### 왜 "다음 벽 후보" 인가 — 단계별로

1. 근거 `[정적]`: bcrypt 는 이게 실패하면 FALSE 다 (`+0x13b JNZ` 안 탐 → `+0x189`).
2. 근거 `[정적]` (run91 ③): bcryptprimitives 는 실패해도 **TRUE** 였다. → bcrypt 만 이 줄이 위험하다.
3. 근거 `[관찰 — 에뮬레이터 로그]` + `[정적]`: 엔진은 `+0x93` 에서 이미 FALSE 라 **이 줄까지 온 적이 없다**.
4. 그래서: 엔진이 KsecDD 를 고치면 **처음 닿는 새 줄** 이 이것이다 (run 문서).
5. 아직 확인 아님: 엔진의 `RtlPcToFileHeader` · `LdrAddRefDll` 은 ntdll 코드라, 로더 표 (모듈 목록 · 그래프 노드) 가 바르면 될 것 `[추측: ntdll 본문 안 읽음]` (run 문서 "PE_emulator 확인거리").

### 2.11 ★ TRUE 최소 조건 — 바깥이 맞출 것

일반 프로세스 기준이다 (환경값 `g_TrustedEnvironment = 1`). run 문서 "최소 조건 표" 10줄을 셋으로 묶었다. 모두 `[정적]`.

**꼭 맞출 것 둘** (이번 막힘)

| # | syscall | 입력 | 기대 답 |
|---|---|---|---|
| 7 | **`NtOpenFile`** | `\Device\KsecDD`, `0x100003`, 공유 7, 옵션 `0x20`, 속성 0, 루트 0, **칸 = bcrypt `+0x25ae0`** | **status ≥ 0 + 그 칸에 0 아닌 핸들** |
| 8 | **`NtDeviceIoControlFile`** | 7 의 핸들, Event · APC 0, `0x390400`, 입력 `0x68`, 출력 8B | **status ≥ 0** (0 권장). 출력 · IOSB 안 봄. `0x103` 도 성공으로 침 (안 기다림) |

**만들어 주기만 하면 되는 것** (핸들 ≠ 0, 성공)

| # | 무엇 | 안 맞으면 |
|---|---|---|
| 5 | 힙 둘 (`0x60` · `0x28`) + `NtCreateSemaphore` × 2 | 힙 NULL → `0xc0000017` → FALSE ①. 세마포어 실패 → 예외 → 착지점 → `0xc00000e5` → FALSE ① `[추측]` |
| 6 | `NtCreateEvent(&h, 0x1f0003, NULL, 1, 0)` — status ≥ 0 + `*h` ≠ 0 | FALSE ① |
| 9 | `GetModuleHandleExW(5, &DllMain, …)` (ntdll · kernelbase 코드, `RtlPcToFileHeader` · `LdrAddRefDll(1, 이미지 시작)`) | **FALSE ②** |

**아무 답이나 되지만, 하나씩 조심할 것**

| # | syscall | 조심할 것 |
|---|---|---|
| 1 · 2 | NtQIP `0x56` (그리고 실패면 class 0) | `0x56` 성공이면 첫 DWORD 가 1 · `0x10` · `0x11` 이 **아니어야**. class 0 답 `+0x38` bit 7 = 0 (2.9) |
| 3 | `NtTraceControl` × 여럿 | 없음 (반환 안 봄, 2.13) |
| 4 | NtQIP `0x2b` | 없음 (결과 버림, 2.9) |
| 10 | `NtClose` | TRUE 길엔 **호출 없음** |

- 세마포어 실패 줄의 "착지점" — `InitializeSystemPreferredCache +0x46 ~ +0x84` 는 점프로 안 닿는 틈이라 결과서가 PE 바이트로 읽었다. `RtlInitializeResource` 가 예외를 던질 때 [착지점](../02_sprint/run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일)으로 와서 `0xc00000e5` 를 만든다. 착지 조건은 `[추측]` (run 문서).

이걸 다 맞추면 attach 는 `+0x1c5 MOV EAX,1` (TRUE) 로 끝난다. 그때 남는 것 `[정적]` (결과서):

- `g_hIoDevice` ≠ 0 (**열린 KsecDD 핸들 1** — 걸이에 손잡이가 걸림)
- 이벤트 핸들 (rva `0x25b10`, 1) — 상담실에 맡긴 신호등
- `g_pCachedRngRWLock` (rva `0x25bc8`, 힙 `0x60` — 세마포어 2) · rva `0x25b18` (힙 `0x28` 임계영역)
- rva `0x25ac4` = 1 ("알림 등록됨" 표시, `InitializeSystemPreferredCache +0x157`)
- `g_TrustedEnvironment` = 1 · `g_fLoadCNGDone` = 1 · `g_hInstance+8` |= `0xe`
- DLL 은 고정 (PIN) 된다.

### 2.12 run91 과 나란히 — 뿌리는 같고, 모양이 다르다

run 문서 "run91 (bcryptprimitives) 과 나란히" 표 그대로다. 모두 `[정적]`.

| | bcryptprimitives (run91) | bcrypt (run92) |
|---|---|---|
| 장치 이름 | `\Device\CNG` | `\Device\KsecDD` |
| `NtOpenFile` 접근 / 속성 | `0x100001` / `0x40` | `0x100003` / 0 |
| 핸들을 받는 곳 | 지역 칸 `[RBP+0x10]` → 캐시 `g_hCNGDriver` | **전역 `g_hIoDevice` 에 바로** |
| 열고 나서 핸들 0 검사 | **한다** (호출자 `GetSeedFromKernelState +0x29`) | **안 한다** (IOCTL 로 가서 커널이 `0xc0000008`) |
| 재시도 | 5번 | 없음 |
| IOCTL 코드 / 입력 / 출력 | `0x390008` / 없음 / `0x30` | `0x390400` / **`0x68` (이벤트 핸들 1)** / 8 |
| IOCTL 출력 | 시드로 씀 (내용 검사 없음) | **버림** |
| IOCTL status | ≥ 0 이면 성공 (`0x103` 이면 kernelbase 가 기다림) | ≥ 0 이면 성공 (`0x103` 도 안 기다림) |
| `IsEnclave` · `GetTrustedEnvironment` | 같은 꼴 | 같은 꼴 (같은 오프셋) |
| `GetModuleHandleExW(5, …)` 실패 | TRUE | **FALSE** |
| 로그의 막힘 모양 | `NtOpenFile` × 5 | `NtOpenFile` × 1 + `NtDeviceIoControlFile`(핸들 0) |
| 엔진이 고칠 것 | `NtOpenFile` 이 핸들 칸에 쓰기 | **같음** (+ IOCTL `0x390400` 에 status 0) |

비유로 한 줄씩:

- **run91 건물**: 바구니로 손잡이를 받아 → 빈 바구니를 보고 **스스로** 돌아섬 → 다섯 번 다시 → 포기.
- **run92 건물**: 걸이를 통째로 내밀어 → 빈 걸이를 **안 보고** 상담실로 → 상담실이 "손잡이 없는데요" → 한 번에 포기.
- **둘 다 고치는 법은 같다**: 창구가 "처리됐어요" 할 때 **진짜 손잡이를 걸어 주는 것**.

### 2.13 로그 끝 `NtClose` × 5, 그리고 `NtTraceControl` — 실패 길 · 방송국 몫

#### `NtClose` × 5 — 셋은 코드로 설명된다 `[정적]`

| 몇 개 | 누가 닫나 |
|---|---|
| 1 | 이벤트 — `BcpRegister… +0x1e9 CloseHandle` (2.5 의 1층) |
| 2 | 세마포어 둘 — `CleanupSystemPreferredCache +0x8e RtlDeleteResource` → ntdll `+0x12` · `+0x1b NtClose` (2.5 의 2층) |
| 나머지 2 | **bcrypt 코드에서 못 찾음** `[공백]`. 정리 길의 ntdll 쪽일 것 `[추측]` |

- `g_hIoDevice` 는 0 이라 `IoShutdown` 이 `+0x2a JZ` 로 **안 닫는다** (결과서 4).
- 결과서 `[추측]`: 나머지 2 는 정리 길의 `TlgUnregisterAggregateProvider` · `CloseThreadpoolTimer` 쪽 ntdll 안일 수 있다.
- **TRUE 길에서는 attach 가 아무것도 안 닫는다.** 그래서 엔진이 고쳐지면 이 다섯은 안 나와야 한다 (Q174 에서 확인).
- → Q173 ④ (후순위, run91 의 Q170 ① 과 같이).

#### `NtTraceControl` — 반환을 아무도 안 본다 `[정적]`

[방송국 채널](run62-tp-etw-init-해설.md) 올리기다.

- 로그 1번 (여럿) 의 후보: `DllMain +0x80 WppInitUm` → `+0x5f EtwRegisterTraceGuidsW`, `+0x85 McGenEventRegister_EtwEventRegister` → `+0x27 EtwEventRegister`.
  - `+0x85` 는 바로 다음 줄이 또 call 이라 `EAX` 를 안 본다.
- 로그 4번 (× 4) 의 후보: `InitializeConfig +0x46 TlgRegisterAggregateProviderEx` → 안에서 `TraceLoggingRegisterEx_EventRegister_EventSetInformation` 을 부른다 (지도 depth 4: `+0x2a` · `+0x6d` · `+0xa8`).
- × 4 가 정확히 어떻게 나오는지는 `[추측]` — `TlgRegisterAggregateProviderEx` 안 몇 번인지 안 읽었다 → Q173.

### 2.14 test.exe 쪽 — 사진과 맞춰 보기

새 실행은 없다. flow-writer 가 run44 덤프 읽기 로그를 다시 읽었다.

| 무엇 | 값 | 뜻 |
|---|---|---|
| 초기화 끝 (P2) 모듈 목록 | `test.exe` · `ntdll.dll` · `KERNEL32.DLL` · `KERNELBASE.dll` · `dll1.dll` · `dll2.dll` — **6개, bcrypt 없음** (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run91 과 같은 줄) | test.exe 는 초기화 동안 이 DLL 을 안 올린다 `[관찰]` |
| `\KnownDlls` 목록 | `bcryptPrimitives.dll` 은 있고 (`_P2.txt:1394`) **`bcrypt.dll` 은 없다** (`_P2.txt:1377 ~ 1419`, P1a · P1b 같은 줄) | 다른 프로세스가 bcrypt 를 올리면 [공용 창고](../02_sprint/run21-load-dll-internal-해설.md)가 아니라 **파일 길** (2.6) 로 올라올 것 `[추측: 실행 안 봄]` |
| run44 사진 전부 | `bcrypt.dll` 글자 0건 | 이 DLL 칸 (`g_hIoDevice` 등) 값은 사진에 없다 |

- 여기도 run91 과 다른 점이 하나 있다. bcryptprimitives 는 공용 창고에 칸이 있었고, bcrypt 는 **없다**.
- sample 소스 · 빌드 스크립트에도 `crypt` · `bcrypt` · `ksecdd` 글자가 없다 `[소스]`. 진입점 뒤에 올리는지는 사진 범위 밖이다.
- 그래서 **test.exe 로는 이번 길을 실행해 볼 수 없다** → Q174 는 다른 샘플이 필요하다.

### 2.15 결과서와 다르게 적은 곳 · 보탠 것

**다르게 적은 곳 하나** — run 문서를 따른다. 결론은 같다.

| # | 결과서 | run 문서 (맞는 쪽) |
|---|---|---|
| ① | 차례 표 1-a 12: `InitializeConfig` 가 `+0x36` · `+0x46` · **`+0x59`** 를 차례로 부름 | `+0x22 JZ +0x4d` 는 환경 `& 1` == 0 일 때의 갈래다. 일반 (환경 1) 은 `+0x31` → `+0x36 RtlDllShutdownInProgress` → (`+0x44 JNZ` 안 타면) `+0x46 TlgRegisterAggregateProviderEx` → **`+0x4b JMP +0x5e`** 로 `+0x59` 를 **건너뛴다**. 대신 `TlgRegisterAggregateProviderEx` 가 안에서 같은 함수를 부른다. 둘 다 반환을 안 봐서 결론 같음. 그래서 `NtTraceControl` × 4 구성은 `[추측]` |

**보탠 것** (flow-writer 가 `.asm` · 사진으로, 짧게):

- ② 출력 칸을 고르는 길 `IoCallKernelDriver +0x5e ~ +0x78` (2.7).
- ③ **`DllMain +0x2c LdrDisableThreadCalloutsForDll`** — 결과서의 `[공백]` 을 닫았다.
  - `RCX` 는 진입 때 그대로 (`+0x11` 에서 `g_hInstance` 에 저장, `+0x20 MOV RDI,RCX`) 라 **bcrypt 자기 항목** 이다.
  - ntdll 본문 (`+0x0 ~ +0x50`) 은 `LdrpFindLoadedDllByHandle` · `LdrpDereferenceModule` 만 부르고 **syscall 줄이 없다**.
  - 하는 일은 `+0x3b OR byte [항목+0x6a],4` (`Flags` 비트 18 `DontCallForThreads`, 항목 `TlsIndex` 가 0 일 때만) — [run68 해설 2.12](run68-dll-init-rest-해설.md) 에서 kernel32 가 부른 **같은 함수** 다.
  - 짐 지도 3.5 그 줄에 "bcrypt 도 부른다" 메모가 붙었다 (3절).
- ④ `DllMain +0xca ~ +0x11c` WPP 갈래 — `[WPP_MAIN_CB+0x1c] & 0x20` 이면 `+0xed GetModuleFileNameW` · `+0x117 WPP_SF_qS` 를 하고 돌아온다. ETW 세션이 켤 때만이라 TRUE / FALSE 에 안 닿는다 (run91 ④ 와 같은 꼴).
- ⑤ `InitializeCNG` 실패 정리 길 — `+0x6d ~ +0x84` WPP 검사 → `+0x86 UninitializeCNG` → `+0x8b JMP +0x25`. 엔클레이브 건너뛰기 `+0x19` 와 성공 끝 `+0x64 JMP +0x19` 은 **같은 자리** (`g_fLoadCNGDone = 1`).
- 이전 run 이 틀린 것은 없다 (bcrypt 는 처음) — 정정 없음.

### 2.16 디버거 · 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 `BeingDebugged` 같은 디버거 칸을 읽는 줄은 없다 `[정적: 읽은 줄 안에서]`.
  - ETW · WPP 세션이 이 공급자를 켜 두면 `DllMain +0xca ~ +0x11c` · `+0x95 ~ +0xc5` · `InitializeCNG +0x6d ~ +0x84` 의 `WPP_SF_*` 줄이 탄다. TRUE / FALSE 엔 안 닿는다.
- **이름 함정 둘** (멈춤점 걸 때):
  1. **Ghidra `entry` 는 WinDbg 심볼이 아니다** (run91 과 같은 함정). `bcrypt!DllMain+0x8f` 나 rva (`bcrypt+0x111cf`) 로 걸면 될 것 `[추측]`.
  2. **C++ 꾸민 이름** (2.3). `IoCallKernelDriver` · `_IoOpenDevice` 를 WinDbg 가 `bcrypt!IoCallKernelDriver` 로 받을지는 모른다. `x bcrypt!*IoCallKernelDriver*` 로 **먼저 확인** `[추측]`.
  - flow-writer 가 windbg-kd-runbook pitfalls 에 추가를 제안했다. 하네스라서 사용자에게 따로 물을 일이다 (plan 결정 기록).

### 2.17 정리 — 비유로 한 번 더

1. 어떤 가게가 **암호 앞쪽 건물** (bcrypt) 을 들인다. 공용 창고엔 없으니 파일 길로 온다 `[추측]`. 관리실이 전원을 켠다 (`DllMain`, 이유 1).
2. 건물은 "여기가 보통 가게인가, 금고방인가" 를 묻는다 (`IsEnclave` · `IsTrustlet`) — run91 건물과 똑같은 질문, 똑같은 답 (보통, 1).
3. "우리 가게 우편 주소가 뭐죠?" 도 묻는다 (`0x2b`). 답이 뭐든 다음 줄로 간다.
4. 방송국 채널을 올린다 (ETW). 답은 안 본다.
5. 수첩 한 쪽에 **자물쇠** 를 단다 (읽기·쓰기 잠금, 세마포어 둘). 열쇠도 하나 더 만든다 (임계영역).
6. 이제 **KsecDD 상담실** 에 신청서를 낸다. 먼저 **신호등** 을 하나 세우고 (`NtCreateEvent`), 그 손잡이를 신청서 `+0x60` 칸에 끼운다. 용건 칸엔 `0x10500` ("설정 바뀌면 알려 주세요"), 첨부 칸엔 전부 "없음".
7. 벽의 손잡이 걸이를 본다. 비어 있다. 열쇠를 쥐고 한 번 더 본다. 여전히 비어 있다.
8. 창구에 **걸이를 통째로** 내밀며 "상담실 손잡이 주세요" (`NtOpenFile`).
9. 흉내 창구는 "**처리됐어요**" 도장만 찍고 걸이를 **빈 채** 돌려준다.
10. 건물 직원은 도장만 보고, 걸이는 안 보고, 상담실로 간다. 빈손으로 문을 연다.
11. 상담실: "**손잡이가 없는데요**" (`0xc0000008`).
12. 다시 줄 서지 않는다. 신호등을 치우고 → 자물쇠를 치우고 → 건물 정리하고 → **전원 켜기 실패 (FALSE)** → 관리실이 건물을 내린다 (`0xc0000142`).

고치는 법도 run91 과 같은 한 줄이다: **창구가 "처리됐어요" 할 때 걸이에 진짜 손잡이를 걸어 주고, 그 손잡이로 넣은 신청서에 "접수됐어요" (status 0) 만 해 주면 된다.**
그러면 건물은 13번째 걸음 — "**자기 방 위치로 철거 금지 도장 받기**" (`GetModuleHandleExW`) — 에 처음 닿는다. 그게 다음 벽 후보다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 줄 | bcrypt 줄 없음 | **새 줄** `bcrypt.pdb 5F1077ED…` (처음 analyze, 함수 311, `DllMain` = Ghidra `entry` rva `0x11140`, 지도 106 함수 · 깊이 10, 38개 뽑음, 핸들 칸 `?g_hIoDevice@@3REAXEA`, test.exe 는 안 올림). ntkrnlmp · kernelbase · ntdll 줄에 run92 몫 (NtQIP class `0x2b` 처리기 · `QueryFullProcessImageNameW` · `CreateEventW` · `CreateEventExW` · `GetModuleHandleExW` · `RtlInitializeResource` · `RtlDeleteResource`) |
| 기준 — 근거 로그 | — | run92 줄: 모듈 6 · `\KnownDlls` 목록에 bcrypt 없음 · 사진에 `bcrypt.dll` 글자 0건 (새 실행 없음) |
| 흐름도 앞 글 | — | run92 문단 하나: **그림은 안 고쳤다** (run91 과 같은 까닭) |
| 그림 | — | **그대로** |
| 3장 | bcryptprimitives 절이 끝 | **새 절 "bcrypt 시작 함수 (`DllMain`, Ghidra `entry`)"** — 비유 한 줄 (은행 창구) + 트리 하나 + 줄 일곱 (막힘 · 최소 조건 · 입력 `0x68` · NtQIP `0x2b` · `NtClose` · 다르게 적은 곳 · 남은 것 → Q174 / Q173) |
| 4장 | — | run92 새 줄 셋: 동적 **Q174** · 정적 **Q173** · PE_emulator 쪽 · 결과서와 다르게 적은 곳 · 보탠 것 |
| 5장 | — | run 표 · 03_sprint 요청서 번호에 run92 줄 |

태그가 바뀐 것 (요청서 · 결과서 → 이번):

- IOCTL 입력에 든 핸들: "세마포어 둘 · 이벤트" `[추측]` → **이벤트 하나 (`+0x60`), 세마포어 없음** `[정적]`.
- 핸들 칸 `bcrypt + 0x25ae0`: 이름 모름 → **`g_hIoDevice`** `[정적]` (PDB).
- 로그의 "IOCTL 핸들 0 → `0xc0000008`": 에뮬레이터 로그로만 앎 `[관찰 — 에뮬레이터 로그]` → **코드로 설명됨** `[정적]` (`+0x1d3 JNS` → `+0x8e` → `+0xc2` → `+0xd0 JS`).
- `LdrDisableThreadCalloutsForDll` 의 syscall: 결과서 `[공백]` → **없음** `[정적]` (flow-writer).

### 짐 지도 `baggage.md` 에서 바뀐 것 (마흔여덟째판)

- **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로** — test.exe 는 이 DLL 을 안 올리니까.
- 3.5 모듈 항목 `DontCallForThreads` 줄에 (run92) 메모 하나: **bcrypt `DllMain +0x2c` 도 이 함수를 부른다** — `RCX` = bcrypt 자기 hModule, bcrypt 항목만, 조건부라 test.exe 는 안 탐.
- 이 DLL 이 놓는 칸 (`g_hIoDevice` · `g_pCachedRngRWLock` 등 bcrypt `.data`, 열린 KsecDD · 이벤트 · 세마포어 핸들, 힙 `0x60` · `0x28`) 은 **체크리스트 밖 · test.exe 안 탐** 이라 안 셌다 (run91 과 같은 판단).
- 기준 run92 줄 · 판 목록 · 8장 두 표.

### PE_emulator 쪽 — 무엇을 고치면 되나 (우리 대기열 아님)

run 문서 "PE_emulator 확인거리" 를 그대로 옮긴다.

1. 엔진 `NtOpenFile` 몸이 `\Device\KsecDD` 도 알아보고 **핸들 칸에 쓰는지** (run91 의 `\Device\CNG` 와 같은 고침). `NtDeviceIoControlFile` 몸이 코드 `0x390400` 에 status 0 을 주는지.
2. 고친 뒤 막힘이 `GetModuleHandleExW(5, &DllMain)` 로 옮겨 가는지 (FALSE ②).
3. 프로그램이 `BCryptOpenAlgorithmProvider` 를 부르면 `BCryptResolveProviders` 가 같은 핸들로 기능 `0x20000` IOCTL 을 하고 **출력을 해석한다** — "출력 0" 으로는 공급자를 못 찾을 수 있다.

### 큰 그림에서 어떻게 이어지나

- 우리 지도 (test.exe) 는 **안 바뀐다**. 이 DLL 은 "**다른 프로세스라면**" 의 옆길이다. 3장에 user32 · gdi32 · bcryptprimitives 와 나란히 놓였다.
- 이 DLL 을 올리는 프로세스에서 순서는 이렇다:
  - 2.6 에서 파일 길로 올라옴 `[추측: 실행 안 봄]` → 2.7 `LdrpCallInitRoutine` → **이번 `DllMain`** → TRUE 면 다음 DLL, FALSE 면 `0xc0000142`.
- 엔진을 고쳐 TRUE 가 되면, 다음에 막힐 수 있는 곳은 둘이다 (run 문서):
  - `DllMain` 안 `GetModuleHandleExW(5, &DllMain)` (FALSE ②) → Q173 ①.
  - attach **뒤** 첫 사용 — 걸이 (`g_hIoDevice`) 를 다시 꺼내 쓰는 곳 (결과서 3, 이름만):

| 다시 꺼내 쓰는 곳 | 한 줄 |
|---|---|
| `IoCallKernelDriver` | 다음 IOCTL 마다 같은 핸들 |
| **`BCryptResolveProviders`** | `NtDeviceIoControlFile` 을 **직접** 부름 (기능 `0x20000`), **출력을 `IoUnpack_ProviderRef` 로 읽음** ← 다음 요청 후보 (Q173 ②) |
| `IoCallKernelDriverQueryLoop` | 설정 질의 반복 |
| `IoShutdown` | 정리 때 닫음 |

  - 이번 attach 에선 "출력은 버린다" 였지만, 같은 주문서 양식 (`0x390400`) 이 다른 용건 (`0x20000`) 으로 가면 **출력을 읽는다**. 엔진이 "출력 0" 으로 버틸 수 있는 건 attach 까지다.

---

## 4. 아직 모르는 것

### 정적으로 남은 것 → **Q173** (작음)

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 |
|---|---|---|
| ntdll `LdrAddRefDll` (고정) · `RtlPcToFileHeader` — `DllMain` 안에서 `GetModuleHandleExW(5, &DllMain)` 가 syscall 없이 성공하는 조건 | 이번엔 kernelbase 쪽 호출 줄까지만 봄. ntdll 본문은 안 읽음 `[공백]` | Q173 ① (KsecDD 가 풀리면 **다음 벽 후보** — FALSE ②) |
| attach 뒤 첫 막힘: `BCryptOpenAlgorithmProvider +0x259` → `BCryptResolveProvidersForBcoapCached` → `BCryptResolveProviders` IOCTL (`0x390400`, 기능 `0x20000`) 입출력 모양 (`IoUnpack_ProviderRef` 가 읽는 칸) | 요청 범위는 attach 만. 질문 3 은 이름만 봄. 이 호출이 실제로 일어나는 조건도 안 따라감 `[공백]` | Q173 ② |
| `ksecdd.sys` 의 `0x390400` 처리 | 범위 밖. attach 의 `0x10500` 은 "status 0" 으로 충분해서 필요 없을 가능성이 큼 | Q173 ③ (후순위) |
| 실패 길 `NtClose` × 5 의 나머지 2 | bcrypt 코드에 없음. 정리 길 ntdll 쪽 `[추측]` | Q173 ④ (후순위, Q170 ① 과 같이. TRUE 가 되면 필요 없음) |
| `NtTraceControl` × 4 구성 | `TlgRegisterAggregateProviderEx` 안 몇 번인지 안 읽음 (2.15 ①) | Q173 |

### 실행해 봐야 아는 것 → **Q174** (동적)

- **test.exe 로는 못 본다** — 초기화 동안 안 올림 `[관찰]`, 진입점 뒤도 sample 에 그런 API 없음 `[소스]`.
- bcrypt 를 올리는 프로세스가 필요하다. **샘플 고르기는 사용자 결정** 이다 (멈춤점은 그 프로세스 조건으로). Q104 에 안 붙였다.
- bcryptprimitives (Q171) 와 같은 프로세스에서 볼 수 있으면 **묶는 게 좋다** `[추측: 두 DLL 을 함께 올리는 프로세스가 흔할 것]`.
- 추천 멈춤점 다섯 (run 문서, WinDbg 모듈 이름 `bcrypt` — 이름 함정은 2.16):

| 멈춤점 | 볼 것 | 무엇을 확인하나 |
|---|---|---|
| `bcrypt!DllMain+0x8f` (rva `0x111cf`, `InitializeCNG` 반환 직후) | `eax` | 0 이면 성공 길 |
| `bcrypt!_IoOpenDevice+0x81` (`NtOpenFile` 반환 직후) | `eax` · `dq bcrypt+0x25ae0 L1` | 진짜 status 와 **진짜 핸들 모양** (4의 배수 작은 수 `[추측]`) |
| `bcrypt!IoCallKernelDriver+0xc9` (IOCTL 반환 직후) | `eax` · IOSB `dq @rsp+0x50 L2` · 출력 `dq @rsp+0x60 L1` | 진짜 IOCTL status · 버려지는 출력 값. 입력은 `+0xc2` 에서 `dq poi(@rsp+0x30) L0xd` (정적 예상은 2.6 표) |
| `bcrypt!DllMain+0x134` (`GetModuleHandleExW` 반환 직후) | `eax` | 진짜 Windows 에서 `DllMain` 안 고정이 성공하는가 |
| `bcrypt!IsEnclave+0x4b` | `eax` · `dd @rsp+0x30 L1` | 정적으론 `0xc0000003` |

- 그 밖: TRUE 로 끝나는 진짜 실행에서 attach 중 `NtClose` 가 **0번** 인지, `NtTraceControl` 실제 개수 · 인자.

### 대기열 밖 — 이름 · 짐작으로 남은 것

- class `0x2b` 이름 (`ProcessImageFileNameWin32`) — PDB 에 열거형이 없어 `[추측]`. DOS 경로라는 건 커널 처리기가 부르는 함수로 뒷받침 `[정적]`.
- 이벤트를 커널 (ksecdd) 이 "설정 바뀜" 알림에 쓴다는 것, 세마포어 둘이 난수 공급자 핸들 두 칸을 지킨다는 것 — 결과서 `[추측]`.
- 진짜 Windows 에서 PENDING 이 안 온다는 것 (동기 열기 `0x20`) — 결과서 `[추측]`.
- WinDbg 가 C++ 꾸민 이름 함수를 짧은 이름으로 받는지 — `[추측]` (2.16). pitfalls 추가 제안은 사용자 확인 대기.

---

## 5. 더 읽기

- run 문서: [`run92-bcrypt-ksecdd.md`](run92-bcrypt-ksecdd.md) — "PE_emulator 쪽에서 쓸 것" (요청 32 결론 · attach 차례 표 · FALSE 자리 · 입력 칸 표 · 최소 조건 표 · run91 과 나란히), "호출 관계", "사진 대조", "결과서와 다르게 적은 곳 · 보탠 것"
- 결과서: [`_workspace/sast/88_bcrypt-ksecdd_result.md`](../../_workspace/sast/88_bcrypt-ksecdd_result.md) — 1-c "이 함수가 보는 것", 2-b "입력 칸 표", 2-c "출력 8바이트", 3 "TRUE 뒤 첫 사용", 5 "최소 조건 표"
- 요청서: [`_workspace/sast/88_bcrypt-ksecdd.md`](../../_workspace/sast/88_bcrypt-ksecdd.md) — 배경 (에뮬레이터 로그 syscall 차례)
- flow.md: [3장 "bcrypt 시작 함수"](flow.md) 절, 4장 run92 줄 셋, 2.7 (DLL 전원 켜기 틀)
- 짐 지도: [`baggage.md`](baggage.md) 3.5 `DontCallForThreads` 줄
- 대기열: [`_workspace/plan.md`](../../_workspace/plan.md) Q172 (끝) · Q173 · Q174 (그리고 묶기 후보 Q171)
- 앞 해설:
  - [run91](run91-bcrypt-init-해설.md) — **바로 이웃** (bcryptprimitives, 같은 꼴 다른 DLL). 2.3 · 2.4 (바구니 · 걸이), 2.9 (class `0x56`)
  - [run85](run85-user32-init-해설.md) · [run86](run86-gdi32-init-해설.md) — 같은 꼴 ("test.exe 는 안 올리는 DLL 의 `DllMain` 최소 조건")
  - [run68](run68-dll-init-rest-해설.md) 2.7 (크리티컬 섹션) · 2.12 (`LdrDisableThreadCalloutsForDll`)
  - [run21](../02_sprint/run21-load-dll-internal-해설.md) 2.9 — 고정 (철거 금지 도장)
  - [run62](run62-tp-etw-init-해설.md) — ETW 등록 · `NtTraceControl`
  - [run66](run66-apphelp-cache-해설.md) 2.7 — 드라이버 · IOCTL (상담실 · 주문서)
  - [run13](../02_sprint/run13-resume-thread-해설.md) 2.3 · [run77](run77-thread-wake-해설.md) — 이벤트 (신호등), 자동 이벤트

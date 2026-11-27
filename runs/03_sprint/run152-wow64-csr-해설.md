# run152 wow64-csr — 해설

> 이 글은 [run152 문서](run152-wow64-csr.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q287** — PE_emulator 요청 `374_internals-wow64-csr.md` "WoW64 (x86) 프로세스의 32비트 csrss 첫 연결" (`_workspace/plan.md`, 이번에 **끝 (run152)**)
  - 원 요청: `../PE_emulator/_workspace/05_tuning_planner/374_internals-wow64-csr.md`, 튜닝 `374_tune19.md` X2.
  - 사용자 결정 2026-11-27 (PE_emulator D179) — **380 묶음 정적 차례 ①** (가장 먼저), **정적만**.
  - 결과서 판정 **분석성공** — 원문 물음 셋 (3 · 1 · 2 차례로) 을 `.asm` 줄로 닫았다.
  - 남은 것이 간 곳 (4절):
    - 동적 → 새 번호 없이 **Q84 · Q253d · Q262** 에 얹음
    - 정적 → **Q295** (run152 남은 정적, `_workspace/plan.md`)
- 근거:
  - run 문서 [`run152-wow64-csr.md`](run152-wow64-csr.md) (가장 중요)
  - 요청서 [`_workspace/sast/147_wow64-csr.md`](../../_workspace/sast/147_wow64-csr.md) · 결과서 [`147_wow64-csr_result.md`](../../_workspace/sast/147_wow64-csr_result.md)
    - **처음 analyze 하나**: `SysWOW64\KernelBase.dll` (x86, 2,974,168 B, PDB `wkernelbase.pdb`). 우리 모듈 이름은 `kernelbase32`.
    - ntdll32 · wow64 · csrsrv 는 있던 프로젝트에 함수만 더 뽑았다. 64비트 ntdll · kernelbase · basesrv 는 있던 `.asm` 을 읽기만 했다.
  - flow.md 변경: docs 커밋 `3c51406` (`git show 3c51406 -- runs/03_sprint/flow.md`). **그림은 안 바뀜.** 짐 지도 `baggage.md` 는 7.6.17 에 메모 두 줄만 (셈은 그대로).
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 여섯

1. **test.exe 는 이 길을 안 탄다.** run138 ~ run140 과 같다.
   - EPROCESS `WoW64Process : (null)` `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:160` · `_P2.txt:160`, run106 이 맞춘 줄), 64비트 ntdll `UseWOW64` 0 `[관찰]` (flow.md 2.4 run44).
   - test.exe 의 csrss 연결은 [run68](run68-dll-init-rest-해설.md#24--csrss-연결--이웃-인사팀-사무실에-처음-거는-전화) 의 64비트 길 그대로다. 이번 글은 "x86 exe 였다면" 의 이야기다.
2. **요청 쪽 숫자는 에뮬레이터 것이다.**
   - "seq 647 ~ 658", rip `0x7ffafb7c14a2`, `NtOpenSection = 0xc000003a`, "x86 34 / 34" 는 모두 PE_emulator 로그다. 진짜 Windows 를 디버거로 본 게 아니라 `[관찰]` 이 아니다. 이 글도 "에뮬레이터 로그와 맞음" 으로만 쓴다 (run 문서 "flow-writer 대조" 8).
3. **같은 이름 함수가 두 몸으로 있다.**
   - `CsrClientConnectToServer` 는 **32비트 ntdll (`ntdll32`) 에 62 B 짜리 껍데기**, **64비트 ntdll 에 진짜 몸** 이 따로 있다.
   - 그래서 이 글은 함수 이름 앞에 늘 모듈 이름 (`ntdll32!` / `ntdll!` (64비트) / `kernelbase32!`) 을 붙인다.
4. **오프셋 읽는 규칙** 은 앞 해설들과 같다.
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리**.
   - 본문의 "`+0x206/+0x20c`" = call 자리 / ret 자리. 대상 없는 숫자 = 그 함수 안 **줄**.
   - 점프 목적지 (`JS +0x4d1`) 도 **함수 시작에서 센 거리** 다. run151 처럼 주소 끝자리와 섞인 곳은 없다 (run 문서 "flow-writer 대조").
   - `PEB32 +0x4c` 처럼 덩어리 이름과 붙은 숫자 = 그 덩어리 맨 앞에서 센 **칸 자리**.
   - 콜스택으로 본 적은 없다 (정적만).
5. **32비트 함수 이름엔 `@12` 같은 꼬리가 붙어 있다** (PDB 이름 꼴, [run140 해설 2.4](run140-wow64-ntdll32-해설.md#24-사슬--64비트와-같은-이름-다른-숫자)). 그리고 kernelbase32 는 바깥 `_KernelBaseBaseDllInitialize@12` 와 안쪽 `__KernelBaseBaseDllInitialize@12` (밑줄 둘) 가 따로 있다. **일은 안쪽이 한다.** 이 글에서 `kernelbase32 +0x…` 라고만 쓰면 안쪽 함수 기준이다.
6. **run 문서 · flow.md 의 비유 "주민센터 · 전입 신고 · 대리인" 은, 이 글에서 run68 의 "인사팀 사무실 · 첫 전화" 와 run138 의 "통역사" 다.** 가리키는 건 같다. 앞 해설 그림을 그대로 이어 쓰려고 바꿨다.

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**, 그 전역 = 관리실 **칠판**, syscall = 관리사무소 **신청 창구** → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, [run10 해설](../02_sprint/run10-allocate-process-해설.md)
- PEB = 가게 **안내판** → [run10 해설](../02_sprint/run10-allocate-process-해설.md#peb)
- KERNELBASE = 가게에 들인 **건물**, 그 시작 함수 (DllMain) = 건물 **전원 켜기**, 그 DLL 전역 = 건물 **수첩** → [run67 해설](run67-kb-heap-init-해설.md), [run68 해설](run68-dll-init-rest-해설.md)
- **csrss** = 이웃 **인사팀 사무실**, basesrv = 그 사무실 **1번 창구**, csrss 연결 = 그 사무실에 거는 **첫 전화** (syscall 여섯) → [run68 해설 2.4](run68-dll-init-rest-해설.md#24--csrss-연결--이웃-인사팀-사무실에-처음-거는-전화)
- **공유 섹션** (`SharedSection`) = 사무실 **게시판** (사무실 벽에도 한 판, 우리 벽에도 한 판), **`BaseStaticServerData`** (BSSD, `0xa00` B) = 그 게시판에 1번 창구가 붙인 **안내문**, "사무실 번지 → 우리 번지" 옮겨 적기 (`C` = 사무실 벽 게시판 시작, `R` = 우리 벽 게시판 시작) → [run71 해설 2.4 · 2.5](run71-named-obj-dir-해설.md#24-안내문은-누가-언제-붙이나--새-말-공유-섹션)
- **WoW64** = 64비트 가게 건물 안의 **옛날식 사무실** (32비트), wow64.dll = **통역사**, wow64cpu.dll = 사무실 **문**, 통역사 창구 `Wow64SystemServiceEx` 와 **안내판 4장**, 통역 담당 `whNt*` → [run138 해설 2.1 · 2.13](run138-wow64-dlls-해설.md#21-큰-그림-먼저--층이-넷-길이-둘)
- **썽크** = 받은 부탁을 그대로 넘기는 **얇은 다리** → [run136 해설 2.3](run136-winsta-name-해설.md)
- **PEB32** = **옛날식 안내판**, TEB32 = **옛날식 개인 안내판** (TEB64 뒷장), 옛날식 짐은 **낮은 층 (4GB 아래)** 에만, `HighestUserAddress` = 땅의 **울타리** → [run139 해설 2.5 · 2.6](run139-wow64-kernel-해설.md#25-4gb-상한의-뿌리--highestuseraddress-c8)
- 32비트 ntdll = 옛날식 사무실 안의 **옛날식 관리실**, 32비트 `Nt*` 스텁 = "번호표 들고 **문으로**" (`mov eax,N · call edx · ret n`) → [run140 해설 2.10 · 2.11](run140-wow64-ntdll32-해설.md#210-nt-스텁-꼴--바이트-한-줄씩-b3)
- DllMain 이 FALSE 면 로더가 `0xc0000142` 를 정한다 (켜기 담당 `LdrpInitializeNode`) → [run27 해설](../02_sprint/run27-init-graph-해설.md), [run28 해설 2.6](../02_sprint/run28-tls-init-call-해설.md)
- `NtRaiseHardError` = 사용자에게 띄우는 오류 창 쪽으로 보이는 syscall `[추측]` → [run26 해설](../02_sprint/run26-map-dll-file-해설.md)

**이번 글의 큰 그림 하나** (run 문서 "결과" 비유를 앞 해설 그림으로 옮긴 것):

- run138 에서 통역사는 **옛날 말 신청서 한 장 → 새 말 신청서 한 장** 으로 바꿔 창구에 내는 사람이었다.
- 그런데 인사팀 사무실에 거는 **첫 전화** 만은 다르다.
  - 옛날식 직원은 통역사에게 **"인사팀에 전화 좀 걸어 줘" 쪽지 한 장** (번호표 `0x1e9`) 만 건넨다.
  - 통역사는 전화 절차를 한 줄씩 통역하지 않는다. 대신 **64비트 관리실이 쓰는 "첫 전화 절차서" 를 통째로 빌려서 그대로 밟는다.**
  - 끝나면 받아 적은 게시판 번지 셋을 64비트 안내판에서 **옛날식 안내판으로 옮겨 적어 준다.**
- 이게 이번 바퀴의 줄기다. 2.4 에서 천천히 푼다.

### 한 줄로 먼저 — 무엇을 얻었나

| 원문 질문 | 답 (run 문서 그대로) |
|---|---|
| ③ 32비트 KERNELBASE 는 연결 뒤 무엇을 읽고, 어디서 FALSE 로 나가나 | BSSD = `[[PEB32+0x54]+8] − [PEB32+0x248] + [PEB32+0x4c]` (32비트 셈). 읽는 BSSD 칸은 64비트와 **같다**, 폭만 반. FALSE 가지 = 안쪽 다섯 + `entry` 둘 + `PcwDllMain` 하나. 원문 막힘은 **F2 (`+0x20e`)** 로 가서 `0xc0000142` `[정적]` (2.7 ~ 2.10) |
| ① 32비트 `CsrClientConnectToServer` → `CsrpConnectToServer` 는 64비트 여섯과 같나 | **32비트엔 `CsrpConnectToServer` 가 없다.** 32비트는 syscall **하나** (`0x1e9`), 그 몸 wow64 가 **64비트 `CsrClientConnectToServer` 를 통째로 불러** run68 의 여섯이 같은 순서 · 같은 인자로 나온다 `[정적]` (2.3 · 2.4) |
| ② wow64 썽크 넷은 무엇을 바꾸나 | **이 길에 안 낀다.** 읽어 둔 꼴은 표로 남겼다. 4GB 요구는 `whNtMapViewOfSection` 하나뿐 `[정적]` (2.11) |
| (되면) csrsrv 의 WoW64 줄 | **없다** (읽은 세 함수 안) `[정적]` (2.12) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

test.exe 지도 위에서는 **갈라지는 옆길 (WoW64)** 이다. 그 옆길 안에서도, run140 이 본 **32비트 ntdll 의 가게 세팅** 중 "kernel32 들이고 전원 켜기" 의 안쪽이다.

```
cmd.exe → CreateProcessInternalW → NtCreateUserProcess … NtResumeThread           (run9 ~ run14)
 (x86 exe 면 커널이 32비트 짐도 더 짓는다 — PEB32 · TEB32 · 울타리 4GB …)          (run139)

 유저, 64비트 ntdll
  LdrpInitializeProcess  +0x1766 UseWOW64 ?
     ├─ 0 (test.exe) → … → kernelbase 전원 켜기 → csrss 첫 전화 (run68) → … → main
     └─ ≠ 0 (x86 exe) → wow64!Wow64LdrpInitialize ⇢ 문 ═▶ 32비트               (run138)

 유저, 32비트 ntdll (옛날식 관리실)
  LdrInitializeThunk → … → LdrpInitializeProcess                                    (run140)
     └─ kernel32 들이기 → _LdrpInitializeNode → 32비트 KERNELBASE 전원 켜기
           └─ __KernelBaseBaseDllInitialize@12
                 +0x206  CsrClientConnectToServer   ← 이번 (csrss 첫 전화, 32비트 판)
                 +0x22d ~ +0x346  BSSD 읽기          ← 이번
                 FALSE 면 → 0xc0000142               ← 이번 (원문 막힘)
```

- 64비트 길에서 같은 자리는 run68 의 `_KernelBaseBaseDllInitialize +0x2d2/+0x2d9` 다. 32비트 `+0x206/+0x20c` 가 그 짝이다 `[정적]`.

### 무엇이 궁금했나

에뮬레이터 쪽이 x86 exe 를 돌리다 막혔다 (에뮬레이터 로그):

- `seq 647 NtQuerySystemInformation 0x3e` → **`648 NtOpenSection("\Sessions\1\Windows\SharedSection", 4)` 가 `0xc000003a`** → 651 · 654 32비트 KERNELBASE · KERNEL32 내림 → `657 NtRaiseHardError 0xc0000145` → `658 NtTerminateProcess 0xc0000142`.

그래서 원문은 셋을 물었다 (요청서 "분량이 크면 3 → 1 → 2" — 3 이 에뮬레이터를 막는 줄).

1. 32비트 `CsrClientConnectToServer` → `CsrpConnectToServer` 의 syscall 차례 · 인자가 64비트 여섯과 같나. 연결 정보 블록 크기 · 칸 (포인터 폭 4 / 8 B), 쓰는 PEB32 칸 · 32비트 `Csr*` 전역.
2. wow64 썽크 넷 (`whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort` / `whNtSecureConnectPort` · `whNtMapViewOfSection`) 은 무엇을 32 ↔ 64 로 바꾸나, 4GB 아래 요구는 있나, 상태를 바꾸나.
3. 32비트 `_KernelBaseBaseDllInitialize` 가 연결 뒤 읽는 칸, 64비트와 다른 칸, **FALSE 로 나가는 가지 전부** (원문 `0xc000003a` 는 어디로 가나).

### 바퀴 전에 알던 것

- `[정적]` (run68) 64비트 `CsrClientConnectToServer` → `CsrpConnectToServer` 가 **syscall 여섯** (`NtOpenSection` · `NtCreateSection` · `NtConnectPort` · `NtClose` · `NtMapViewOfSection` · `NtClose`) 으로 64비트 ntdll `Csr*` 전역 **9개** 와 PEB `+0x88` · `+0x98` · `+0x380` 을 채운다.
- `[정적]` (run71) 안내문 (BSSD) 의 칸 표, 옮겨 적기 식. `[관찰]` (run44 사진, run71 이 옮김) 진짜 프로세스에선 `R = 0x7ff4ecf70000` ≠ `C = 0x7ff5575f0000`.
- `[정적]` (run138) 통역사 창구 `Wow64SystemServiceEx` 가 번호로 안내판에서 `whNt*` 를 찾고, `whNt*` 는 64비트 ntdll `Nt*` 를 import 로 부른다 — **이번에 이 줄이 좁혀졌다** (2.4 끝).
- `[정적]` (run139) 커널이 PEB32 에 쓰는 칸 27개. 울타리 `HighestUserAddress` = `0x7fff0000` / LAA `0xffff0000`.
- `[정적]` (run140) 32비트 `Nt*` 스텁은 `mov eax,N · call edx · ret n` 꼴로 **문** 으로 간다.
- 원문의 두 전제 (이번에 둘 다 틀린 걸로 드러남):
  - "32비트 ntdll `CsrClientConnectToServer` → `CsrpConnectToServer`" — 32비트에도 그 함수가 있다는 전제.
  - "wow64 가 64비트 syscall 로 바꿈 (로그 rip `0x7ffafb7c14a2`)" — 통역사가 한 장씩 번역한다는 전제.

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계 (flow.md 새 절 나무를 줄인 것, ret 자리)

```
kernelbase32!entry (DllMain, 이유 1)                                                [정적]
 └─ +0x1b  _KernelBaseBaseDllInitialize@12 → +0x2a __KernelBaseBaseDllInitialize@12
            ├─ +0x1ba  NtQuerySystemInformation(0 → wow64 가 0x3e)                   F1
            ├─ +0x20c  ntdll32!CsrClientConnectToServer(dir, 1, &{CtrlRoutine,0}, 8, &b)   F2
            │           └─ +0x1b  NtWow64CsrClientConnectToServer (0x1e9) ⇢ 문 ⇢ 통역사 창구
            │                      └─ wow64!whNtWow64CsrClientConnectToServer
            │                          ├─ +0x23  ntdll!CsrClientConnectToServer (64비트)
            │                          │          └─ +0x118 CsrpConnectToServer ⇒ syscall 여섯 (run68)
            │                          └─ +0x63 · +0x7b · +0x8e  PEB32 세 칸 ← PEB64 세 칸 (상태 안 봄)
            │           +0x28  _CsrClientProcess ← 1   (ntdll32, 성공일 때)
            │  +0x248  BaseStaticServerData ← [[PEB32+0x54]+8] − [PEB32+0x248] + [PEB32+0x4c]
            ├─ +0x3ba  BaseNlsDllInitialize · +0x3d8 RtlInitializeCriticalSection · +0x3f0 ConsoleInitialize   F3 · F4 · F5
            └─ (FALSE) entry → ntdll32!_LdrpInitializeNode +0x247 0xc0000142 → … NtRaiseHardError(0xc0000145)
```

읽을 때 주의:

- `⇢` 는 간접 · syscall 다리, `⇒` 는 syscall → 커널 (run 문서 범례).
- `entry` 의 다른 두 가지 (`ARI::Globals::Initialize` · `PcwDllMain`) 는 2.9 표에 있다.
- 대표만 남겼다. 전체는 run 문서 "정적 — 호출 지점" 표와 "호출 관계" 나무.

### 2.2 한 줄씩 읽는 법

**줄 1: `├─ +0x20c  ntdll32!CsrClientConnectToServer(dir, 1, &{CtrlRoutine,0}, 8, &b)`**

- 누가: 32비트 KERNELBASE 의 안쪽 전원 켜기 함수 `__KernelBaseBaseDllInitialize@12` (rva `0x15b185`, 1251 B).
- 어디서: call `+0x206`, ret `+0x20c`. import 칸 `[0x1028db38]` 에서 주소를 꺼내 부른다 `[정적]`.
- 누구를: **32비트 ntdll** 의 `CsrClientConnectToServer` (rva `0x72ab0`, 62 B).
- 인자 다섯 `[정적]`:
  - 디렉터리 이름 (지역 버퍼 `[EBP-0x204]`. 세션 번호가 PEB32 `+0x1d4` 와 같으면 `\Windows`, 다르면 `+0x1a3` `StringCbPrintfW` 가 만든 `\Sessions\<n>\Windows`)
  - 서버 번호 `1` (= 1번 창구 BASESRV)
  - 블록 `[EBP-0x21c]` = `{CtrlRoutine (0x102408b0), 0}` 8 B
  - 크기 `8` (값)
  - 출력 바이트 `[EBP-0x205]`
- 무엇을 위해: 인사팀 사무실 1번 창구와 **첫 전화** 를 맺으려고. run68 의 64비트 `+0x2d2/+0x2d9` 와 같은 일이다.
- 바로 뒤 `+0x20e JS +0x4d1` — 결과가 음수면 **F2** 로 FALSE (2.9).

**줄 2: `├─ +0x23  ntdll!CsrClientConnectToServer (64비트)`** — 이번 바퀴의 핵심 줄

- 누가: wow64 의 통역 담당 `whNtWow64CsrClientConnectToServer` (rva `0x23200`, 158 B). 통역사 nt 안내판 `sdwhnt32` 의 **칸 `0x1e9`** 주인이다 `[정적]` (flow-writer 가 PE 표로 다시 읽음).
- 어디서: call `+0x1c` (`CALL [0x18003c5f8]`), ret `+0x23`.
- 누구를: import 칸 `0x3c5f8` = **`ntdll.dll!CsrClientConnectToServer`** — **64비트** ntdll 의 진짜 연결 함수 (rva `0xdb7a0`) `[정적: PE import 표]`.
  - `Nt*` 가 아니다. syscall 스텁이 아니라 **보통 함수** 다.
- 무엇을 위해: 32비트가 건넨 쪽지 (인자 다섯) 를 64비트 꼴로 펴서, **64비트 관리실의 첫 전화 절차를 통째로** 돌리려고.

### 2.3 ★ 32비트 ntdll 에는 "전화 거는 몸" 이 없다 (물음 1)

**원문 전제부터 확인** `[정적]`

- 원문 · 요청서는 32비트 ntdll 에도 `CsrpConnectToServer` (와 `CsrpCaptureCsrPortServerInfo` 류) 가 있다고 봤다.
- 그런데 32비트 ntdll `_index.tsv` 의 `Csr*` 는 **`CsrClientConnectToServer` · `CsrClientCallServer` · `CsrAllocateCaptureBuffer`… 썽크 12개와 `NtWow64Csr*` 10개뿐** 이다.
- `pdb_aliases.py --where ntdll32.dll CsrpConnectToServer` 도 "공개 심볼에 없음" (`Csrp*` 0개).
- 원문이 "internals 의 `ms/decomp/ntdll32` 에 `Csr*` 가 없다" 고 한 것도, 빠뜨린 게 아니라 **원래 그 정도뿐** 이었다.

**32비트 `CsrClientConnectToServer` 62 B 가 하는 일** — 줄마다 `[정적]`

1. `+0x6 LEA EAX,[EBP-1]` · `+0x9 PUSH EAX` — **자기 지역 바이트** 의 주소를 다섯째 자리로 넣는다.
2. `+0xa ~ +0x13` — 받은 인자 **1 ~ 4** (`[EBP+0x8 ~ +0x14]`) 를 다시 push.
3. `+0x16` — syscall 스텁 `NtWow64CsrClientConnectToServer` 를 부른다 (ret `+0x1b`).
4. `+0x1f JS +0x38` — 음수면 그대로 돌아간다 (`+0x3b RET 0x14`).
5. 성공이면 `+0x21 MOV CL,[EBP-1]` (출력 바이트) → `+0x26 JNZ +0x2f` → 바이트가 0 이면 **`+0x28 _CsrClientProcess ← 1`**.
6. `+0x2f ~ +0x36` — 부른 쪽의 다섯째 인자 (`[EBP+0x18]`) 가 있으면 거기에 그 바이트를 적는다.

- 결과서는 "인자 5개를 그대로 다시 push" 라고 적었는데, flow-writer 가 `.asm` 으로 다시 보니 **넷 + 자기 바이트** 였다 (run 문서 "flow-writer 대조" 3). 결론 (출력 바이트가 `_CsrClientProcess` 를 정함) 은 같다.

**syscall 스텁** `NtWow64CsrClientConnectToServer` (rva `0x7ba30`) `[정적]`

- `+0x0 MOV EAX,0x1e9` · `+0xa CALL EDX` (`_Wow64SystemServiceCall`) · `+0xc RET 0x14`.
- run140 의 스텁 꼴 그대로다. 번호표 `0x1e9`, 인자 바이트 `0x14` = 5개.
- 이 번호는 통역사 nt 안내판의 칸 번호다. 칸 `0x1e9` = `whNtWow64CsrClientConnectToServer` (안내판 함수 배열 `0x3a550` 에서 PE 로 읽음).

비유:

- 옛날식 관리실에는 **"전화 거는 법" 책이 없다.** 있는 건 "인사팀 전화는 통역사에게 부탁" 이라는 **메모 한 장** 뿐이다.
- 그 메모가 하는 일은 셋: 쪽지 접어 건네기 (인자 push) → 문으로 가기 (`0x1e9`) → 돌아오면 "통화 됐음" 표시 (`_CsrClientProcess ← 1`).

### 2.4 ★ 큰 줄기 — 32비트가 64비트 몸을 통째로 빌려 쓴다 (원문의 "번역한다" 가 틀린 까닭)

이번 글에서 가장 중요한 절이다. 천천히 간다.

#### 두 그림을 나란히

원문이 떠올린 그림과 코드에 있는 그림을 같은 틀에 놓아 보자.

```
 원문의 그림 ("번역한다")                      코드에 있는 그림 ("통째로 빌린다")
 ─────────────────────────                     ─────────────────────────────────
 32비트 CsrClientConnectToServer               32비트 CsrClientConnectToServer (62 B)
  └ 32비트 CsrpConnectToServer                   └ 32비트 NtWow64CsrClientConnectToServer (0x1e9)
     ├ 32비트 NtOpenSection (0x37)                   ⇢ 문 ⇢ 통역사 창구
     │   ⇢ whNtOpenSection → 64비트 NtOpenSection        └ whNtWow64CsrClientConnectToServer
     ├ 32비트 NtCreateSection (0x4a)                          └ 64비트 CsrClientConnectToServer
     │   ⇢ whNtCreateSection → 64비트 …                          └ 64비트 CsrpConnectToServer
     ├ 32비트 NtConnectPort (0xa4)                                   ├ 64비트 NtOpenSection
     │   ⇢ whNtConnectPort → 64비트 …                                ├ 64비트 NtCreateSection
     └ 32비트 NtMapViewOfSection (0x28)                              ├ 64비트 NtConnectPort
         ⇢ whNtMapViewOfSection → 64비트 …                           ├ NtClose · NtMapViewOfSection · NtClose
                                                                     (run68 여섯 그대로)
 문을 지나는 횟수: 여섯 번                       문을 지나는 횟수: 한 번
```

- 왼쪽: 전화 절차의 **한 단계마다** 통역사를 거친다. 32비트 코드가 절차를 쥐고 있고, 통역사는 단계마다 통역만 한다.
- 오른쪽: 32비트 코드는 **"전화 걸어 줘" 한 마디** 만 한다. 절차 전체는 **64비트 관리실의 절차서 (64비트 ntdll 코드)** 가 쥐고 있다.

#### 왜 오른쪽이 맞나 — 단계별로

1. **근거 1** `[정적]`: 32비트 ntdll 에 `CsrpConnectToServer` 가 없다 (2.3). 왼쪽 그림의 둘째 줄부터 **설 자리가 없다**.
2. **근거 2** `[정적]`: 32비트 `CsrClientConnectToServer` 가 내는 syscall 은 `0x1e9` **하나** 다. 32비트 `NtOpenSection` (`0x37`) · `NtConnectPort` (`0xa4`) 등을 부르는 줄이 그 62 B 안에 없다.
3. **근거 3** `[정적]`: 칸 `0x1e9` 의 담당 `whNtWow64CsrClientConnectToServer +0x1c` 가 부르는 곳은 import 칸 `0x3c5f8` = **64비트 `ntdll.dll!CsrClientConnectToServer`** 다. `Nt*` 하나가 아니라 **연결 함수 한 덩어리** 다.
4. **그래서**: run68 의 64비트 `CsrClientConnectToServer +0x113` → `CsrpConnectToServer` 가 **그대로** 돈다. syscall 여섯은 **64비트 ntdll 안에서, 같은 순서 · 같은 인자로** 나온다 `[정적]`.
5. **그래서**: `whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort` · `whNtMapViewOfSection` 은 **이 길에서 안 불린다** `[정적]`. 64비트 코드가 64비트 `Nt*` 를 바로 부르니 통역할 게 없다.

#### 에뮬레이터 로그는 왜 왼쪽처럼 보였을까

- 로그의 `NtOpenSection` rip `0x7ffafb7c14a2` 끝자리 `0x1614a2` 는 64비트 ntdll `NtOpenSection` 스텁 (`0x161490`) `+0x12` 의 `SYSCALL` 자리다 (결과서 — 에뮬레이터 로그 값과 맞춘 것).
- 그런데 **두 그림 모두** 이 자리에 닿는다. 왼쪽 그림에서도 `whNtOpenSection +0x4a` 가 64비트 `NtOpenSection` 을 부르기 때문이다 `[정적]` (run 문서 호출 지점 표).
- 그러니 rip 하나만으로는 둘을 못 가른다. 가르는 건 **누가 그 스텁을 불렀나** (`CsrpConnectToServer +0x1b6` 인가 `whNtOpenSection +0x4a` 인가) 이고, 그건 코드 (근거 1 ~ 3) 가 답했다 `[추측]` — 이 문단은 내가 run 문서 표의 두 줄을 이어 읽은 것이다.

#### 비유로 — "통역" 이 아니라 "대리 통화"

- 옛날식 직원이 인사팀 사무실에 처음 전화를 걸어야 한다. 그런데 옛날식 사무실엔 전화 거는 법 책이 없다.
- 직원은 문 밖 통역사에게 쪽지 한 장을 준다: "**1번 창구에 전화 좀. 비상 연락처는 이거 (`CtrlRoutine32`).**"
- 통역사는 쪽지를 한 줄씩 옮겨 말하지 않는다. 바로 옆 **64비트 관리실의 전화 거는 절차서를 펴고, 64비트 관리실 사람이 하듯 처음부터 끝까지 그대로** 한다.
  - 게시판 찾기 → 메모장 만들기 → 전화 걸기 → 메모장 손잡이 내려놓기 → 게시판 펼치기 → 게시판 손잡이 내려놓기 (run68 의 여섯).
  - 사무실 쪽에서 보면 **64비트 손님이 건 전화** 와 똑같다 (2.12 — csrss 에 WoW64 갈래가 없음).
  - 받아 적는 칸도 **64비트 관리실 칠판** (`Csr*` 9개) 과 **64비트 안내판** (PEB64 세 칸) 이다.
- 전화를 끊고 나서야 통역사가 옛날식 안내판에 **게시판 번지 셋을 옮겨 적는다** (2.6).
- 그러니 "번역한다" 는 말이 틀린 까닭은 하나다: **번역할 32비트 절차가 애초에 없다.** 32비트가 가진 건 "부탁" 한 마디이고, 일하는 몸은 64비트 것이다.

#### 이 차이가 왜 중요한가 — 세 가지

| | 왼쪽 그림이었다면 | 실제 (오른쪽) |
|---|---|---|
| 연결 정보 블록 | 32비트 꼴 블록을 통역사가 64비트로 늘려야 함 | 64비트 ntdll 이 **64비트 꼴 `0x30` B** 를 직접 짓는다. 늘릴 것 없음 (2.5) |
| 4GB 아래 요구 | `whNtMapViewOfSection` 이 `ZeroBits ← Wow64pMaxAddress` 로 채워 줌 | 그 썽크를 안 거친다. 64비트 `NtMapViewOfSection` 은 `ZeroBits = 0` — **커널 울타리에만** 기댐 (2.8) |
| 결과가 적히는 곳 | 32비트 ntdll 칠판 · PEB32 | **64비트** ntdll 칠판 · PEB64. PEB32 세 칸은 통역사가 **나중에 옮겨 적음** (2.6) |

#### 이전 글 한 줄이 좁혀졌다 — run138 "통역 담당은 64비트 `Nt*` 를 부른다"

- run138 은 "`whNt*` 는 64비트 ntdll `Nt*` 를 import 해서 부른다" 고 적었다 `[정적]`. 모든 담당이 그런 것처럼 읽혔다.
- 이번에 고쳐 읽기: **대부분은 그렇다. 하지만 칸 `0x1e9` 담당은 `Nt*` 가 아닌 `CsrClientConnectToServer` 를 통째로 부른다.** `whNtContinue` → `Wow64NtContinue` (run140) 처럼 `Nt*` 를 바로 안 부르는 담당도 있다.
- run138 문서 · 해설 맨 위에 "고쳐 읽기 (run152)" 한 줄씩, flow.md 3장 "syscall 다리" 줄에 `(run152)` 한 마디가 붙었다. 원문은 지우지 않았다.

### 2.5 연결 정보 블록 — 64비트 꼴 그대로, 32비트가 건네는 건 8 B 쪽지 한 장

run68 에서 전화 걸 때 넘기는 **연결 블록 `0x30` B** 를 봤다 (사무실이 답을 거기 적어 줌). WoW64 에서도 그 블록은 **64비트 ntdll 이 짓는다** (`CsrpConnectToServer +0x295 ~ +0x2df`) → 크기 `0x30` B, 포인터 칸 8 B 그대로다 `[정적]`.

| 블록 칸 | 크기 | 방향 | 뜻 | WoW64 에서 |
|---|---|---|---|---|
| `+0x00` | 8 | 응답 | **C** (사무실 벽 게시판 시작) → PEB64 `+0x380` | 같음 |
| `+0x08` | 8 | 응답 | 표 **T** (사무실 번지) → PEB64 `+0x98 = T − C + R` | 같음 |
| `+0x10` | 8 | 응답 | csrss PID → `CsrProcessId` | 같음 |
| `+0x18` | 8 | — | 0 (읽는 줄 없음) | 같음 |
| `+0x20` | 4 | 요청 | 서버 번호 = 1 | 같음 |
| `+0x24` | 4 | 응답 | 1번 창구 connect 루틴 결과 (0 = 성공) | 같음 |
| `+0x28` | 8 | 요청 | **사용자 정보** = 부른 쪽 블록 8 B 복사 | **`{CtrlRoutine32, 0}`** |

- 32비트가 실제로 건네는 건 맨 아래 **8 B 하나** 다: kernelbase32 가 `+0x1ec` · `+0x1ff` 에서 `[EBP-0x21c]` = `0x102408b0` (`CtrlRoutine`, rva `0x2408b0`), `[EBP-0x218]` = 0 을 쓴다 `[정적]`.

**새 말 — 0 확장**

- 32비트 주소 (4 B) 를 8 B 칸에 넣을 때 **위 4 B 를 0 으로 채우는 것**. 비유: 4자리 번지 `1234` 를 8자리 칸에 `0000 1234` 로 적기.
- 그래서 `{CtrlRoutine32, 0}` 8 B 를 64비트 쪽이 한 덩어리 (qword) 로 읽으면 **0 확장된 32비트 주소** 가 된다.

**왜 굳이 8 B 를 만드나** — 단계별로 `[정적]`

1. 사무실 1번 창구 `basesrv!BaseClientConnectRoutine` 은 **크기가 8 인지** 본다 (`+0x0 CMP [R8],8`). 아니면 `0xc000000d` (`+0x1e`).
2. 8 이면 그 8 B 를 qword 로 받아 csrss 쪽 프로세스 기록에 둔다 (`+0x6 ~ +0x10`), 결과 0 (`+0x13`).
3. 64비트 ntdll 도 크기 인자가 8 이 아니거나 서버 번호가 1 이 아니면 (`+0x282` · `+0x28c`) 상태 칸 `+0x24` 에 `0xc000000d` 를 미리 넣고 사용자 정보는 안 복사한 채 연결은 그대로 한다 (`+0x411`).
4. 그래서 → 32비트 KERNELBASE 는 주소가 4 B 여도 **일부러 8 B 쪽지** (`{주소, 0}`) 를 만들어 크기 8 로 넘긴다.

비유: 사무실 1번 창구는 "비상 연락처 칸은 **8칸짜리**" 만 받는다. 옛날식 직원 전화번호는 4자리라, 앞에 0 네 개를 붙여 8칸을 채워 낸다.

**통역사가 인자를 펴는 법** (`whNtWow64CsrClientConnectToServer +0x4 ~ +0x17`) `[정적]`

- `RCX` = 디렉터리 포인터 (0 확장) · `EDX` = 서버 번호 · `R8` = 블록 포인터 (0 확장) · `R9D` = 크기 값 8 · `[RSP+0x20]` = 출력 바이트 포인터 (0 확장).
- 포인터는 모두 32비트 스택 · 데이터를 가리킨다. 그러니 **64비트 코드가 4GB 아래 주소를 그냥 읽는다** (예 `CsrpConnectToServer +0x295 MOV RAX,[R15]` 8 B).
- 출력 바이트 포인터는 32비트 ntdll **자기 지역 바이트** 를 가리킨다 (2.3 의 1번).

### 2.6 PEB32 세 칸 — 통역사가 "상태를 안 보고" 옮겨 적는다

전화가 끝나면 (64비트 `CsrClientConnectToServer` 가 돌아오면) 통역사가 옛날식 안내판에 세 칸을 쓴다 `[정적]`.

| 옛날식 안내판 칸 | 크기 | 값 | 줄 |
|---|---|---|---|
| PEB32 `+0x4c ReadOnlySharedMemoryBase` (**R**, 우리 벽 게시판) | 4 | PEB64 `+0x88` 의 하위 4 B | `+0x63` |
| PEB32 `+0x54 ReadOnlyStaticServerData` (표 **T**, 우리 번지) | 4 | PEB64 `+0x98` 의 하위 4 B | `+0x7b` |
| PEB32 `+0x248 CsrServerReadOnlySharedMemoryBase` (**C**) | **8** | PEB64 `+0x380` 통째 | `+0x8e` |

- "하위 4 B" = 8 B 값의 아래 4 B 만 남기기. 위 4 B 가 0 이 아니면 그 부분은 **잘려 사라진다** (2.8 에서 중요).
- `+0x248` 만 8 B 다. 옛날식 안내판에서도 이 칸은 8 B 칸이다.

**"상태를 안 본다" 는 게 무슨 뜻인가** — 단계별로 `[정적]`

1. `+0x28` — 64비트 연결 결과 (상태) 를 `[RSP+0x40]` 에 넣어 둔다.
2. `+0x2c ~ +0x8e` — 그 상태를 **검사하지 않고** 바로 PEB32 찾기 → 세 칸 쓰기.
3. `+0x95 MOV EAX,[RSP+0x40]` — 넣어 둔 상태를 그대로 돌려준다.
4. 그래서 → 전화가 **실패해도** 세 칸에 PEB64 값 (그때는 0) 을 옮겨 적는다.
5. 통역사 창구 (`Wow64SystemServiceEx`) 도 이 상태를 안 바꾼다. nt 안내판의 오류 종류 칸이 0 이라서다 (run138).

비유: 통역사는 통화가 됐든 안 됐든 **받아 적은 메모장의 번지 칸을 옛날식 안내판에 그대로 베낀다.** 통화가 안 됐으면 빈칸 (0) 을 베낀다.

**옛날식 안내판은 어떻게 찾나** — `+0x2c ~ +0x4b` `[정적]`

- `TEB32 = TEB64 + [TEB64+0x180c]` → `PEB32 = [TEB32+0x30]`.
- `[TEB64+0x180c]` 는 `0x2000` 이다 (run139 `MmCreateTeb +0x14a`). 옛날식 개인 안내판이 64비트 것 바로 뒷장이라는 run139 그림 그대로다.
- 그 칸이 **0 이면** `RAX = 0` 인 채 `+0x4b MOV EDX,[RAX+0x30]` → 주소 `0x30` 을 읽다 접근 위반. **음수면** `RAX = TEB64` 그대로 (TEB64 를 TEB32 로 읽음) — flow-writer 가 `.asm` 으로 결과서 말을 더 정확히 고친 곳 (run 문서 "flow-writer 대조" 4).

**이 세 칸은 커널이 안 쓴다** `[정적]`

- run139 의 "커널이 PEB32 에 쓰는 27칸" 에 없다. 그러니 연결 전에 읽으면 0 이다.
- 짐 지도 7.6.17 에 이 사실이 메모로 붙었다 (3절).

**32비트 `Csr*` 전역은 하나만** `[정적]`

| 32비트 ntdll 전역 | 이 길에서 |
|---|---|
| `_CsrClientProcess` (rva `0x13911c`, 1 B) | **`+0x28` 이 1 을 씀** (성공이고 출력 바이트 0 일 때) |
| `_CsrInitOnceDone` (`0x13911d`) · `_CsrServerApiRoutine` (`0x139118`) | 아무도 안 씀 (`RtlRegisterThreadWithCsrss` 가 읽기만) |

- 진짜 `Csr*` **9개** (`CsrHeap` · `CsrPortHandle` · `CsrPortName` …) 는 **64비트 ntdll 안** 에 있다 (run68 그대로).
- 덤: 64비트 `CsrHeap` ← **PEB64.ProcessHeap** (`+0xf2`). 그래서 WoW64 프로세스의 **PEB64 `+0x30` 에 쓸 수 있는 힙이 먼저 있어야** 64비트 연결이 통과한다 (`CsrpConnectToServer +0x119 RtlAllocateHeap`). 에뮬레이터 로그가 648 까지 갔으니 거기엔 있다 (에뮬레이터 로그, `[관찰]` 아님).

### 2.7 32비트 KERNELBASE 가 읽는 것 — 같은 안내문, 폭만 반 (물음 3 앞쪽)

전화가 돌아오면 32비트 KERNELBASE 는 run68 · run71 의 64비트 판처럼 **안내문 위치를 우리 번지로 옮겨 수첩에 적는다**.

**안내문 위치 셈** (`+0x21f ~ +0x248`) `[정적]`

| 줄 | 하는 일 | 64비트 짝 |
|---|---|---|
| `+0x22d MOV EAX,[EAX+0x54]` | PEB32 `+0x54` = 표 시작 **T** (4 B) | PEB64 `+0x98` (8 B) |
| `+0x230 MOV EDI,[EAX+0x8]` | 표 **칸 1** (BASESRV 번호 1) 의 **하위 4 B** = 안내문의 사무실 번지 | `[T+8]` 8 B |
| `+0x239 SUB EDI,[ECX+0x248]` | − PEB32 `+0x248` (**C**, 하위 4 B) | PEB64 `+0x380` |
| `+0x23f ADD EDI,[EAX+0x4c]` | + PEB32 `+0x4c` (**R**) | PEB64 `+0x88` |
| `+0x248` | 수첩 `_BaseStaticServerData` (rva `0x2848cc`) ← 그 값 | rva `0x3aedb8` |

- 표 칸은 **8 B 단위** 다 (사무실이 64비트라서). 그래서 칸 1 은 `+8` — 64비트와 같은 자리다. 32비트는 그 8 B 중 **아래 4 B 만** 읽는다.
- 식은 run68 과 같다: **사무실 번지 − 사무실 벽 시작 + 우리 벽 시작.**

**안내문의 어느 줄을 읽나 — 64비트와 다른 줄은 없다** `[정적]`

- 읽는 BSSD 칸: `+0x0` · `+0x2` · `+0x8/+0xc` · `+0x10` · `+0x12` · `+0x18/+0x1c` · `+0x38` · `+0x9e8`, 그리고 `+0x178` 은 **주소만** (`_BaseNlsDllInitialize@12 +0xe ~ +0x16` → `_gpServerNlsUserInfo`). 전부 run71 표에 이미 있는 줄이다.
- 다른 건 넷뿐이다:
  1. **폭** — 8 B 칸을 아래 4 B 로 읽는다.
  2. **쪽지 줄이기** — `UNICODE_STRING` ([run71 해설](run71-named-obj-dir-해설.md) 의 "몇 글자, 어디부터" 쪽지) 16 B 를 8 B 로 줄여 수첩에 넣는다. 예: `+0x25f` · `+0x268` 이 `Length` · `MaximumLength` 를 따로 옮긴다 (64비트는 `MOVUPS` 16 B 한 번).
  3. **PEB 칸 자리** 셋 (`+0x4c` · `+0x54` · `+0x248`).
  4. **수첩 (`KernelBaseGlobalData`) 안 자리** — `+0x8 · +0xc · +0x10 · +0x12 · +0x14` 로 앞당겨진다 (64비트 `+0x8 · +0x10 · +0x18 · +0x20`).
- 쪽지 안 주소 (`Buffer`) 도 run71 의 `+0x9e8` 옮기기를 그대로 한다: `Buffer 우리 번지 = [BSSD+8] + BSSD 우리 번지 − [BSSD 우리 번지 + 0x9e8]`. `Buffer` 8 B 가 0 이면 (`+0x272` · `+0x275` 에서 아래 · 위 둘 다 봄) 옮김을 건너뛴다.

비유: 옛날식 직원도 **같은 게시판의 같은 안내문, 같은 줄** 을 읽는다. 다만 번지를 **4자리 수첩** 에 적으니, 8자리 번지는 아래 4자리만 받아 적고, 긴 쪽지는 짧은 쪽지로 접어 넣는다.

### 2.8 4GB — 왜 R 은 낮은 층이어야 하고, 누가 그걸 지키나

**R 은 4GB 아래여야 한다** `[정적]`

1. 32비트 코드는 PEB32 `+0x4c` 의 **4 B** 로 R 을 안다.
2. 그리고 그 값으로 만든 BSSD 우리 번지로 안내문을 **직접** 읽는다 (`[EDI+0x38]` 등).
3. 그러니 R 의 위 4 B 가 0 이 아니면 (4GB 위) 2.6 에서 **잘려 사라지고**, 32비트 코드가 엉뚱한 곳을 읽는다.
4. 그래서 → **공유 섹션 뷰 R 은 4GB 아래** 여야 한다.

**C 는 높아도 된다** `[정적]` — 차이만 쓰기 때문

- 셈에서 C 는 늘 "빼기" 로만 나온다 (T − C, S − C — S = 안내문의 사무실 번지).
- 그래서 **T − C 와 S − C 가 4GB 안이기만 하면**, 아래 4 B 끼리 빼도 진짜 차이와 같다 (run 문서).
- 숫자 놀이로 보면 (실제 값 아님, 설명용):
  - 수첩이 **4자리** 만 적는다고 하자. C = `9 9990`, T = `10 0010` (진짜 차이 = 20).
  - 아래 4자리만 적으면 C → `9990`, T → `0010`.
  - `0010 − 9990` 은 4자리 안에서 한 바퀴 돌아 `0020` 이 된다. **차이 20 이 그대로 나온다.**
  - 그다음 R (낮은 층, 4자리로 다 적히는 수) 에 20 을 더하면 맞는 우리 번지다.
- 진짜 프로세스에서도 R ≠ C 다 (run44 사진 `[관찰]`, run71). WoW64 에선 C 가 64비트 csrss 의 높은 번지라 **C ≠ R 이 정상** 이다 (2.12).

**그런데 "4GB 아래로" 를 외치는 사용자 코드는 없다** `[정적]`

- 2.4 표대로, 이 길은 `whNtMapViewOfSection` (`+0xbe CMOVZ RDI,[Wow64pMaxAddress]` — 4GB 요구가 있는 유일한 썽크) 을 **안 거친다**.
- 64비트 `CsrpConnectToServer +0x338` 의 `NtMapViewOfSection` 은 `ZeroBits = 0` (`+0x305 XOR R9D,R9D`) 이다. "몇 번지 아래로" 라는 요구가 없다.
- 그럼 누가 지키나 → **커널의 울타리** `HighestUserAddress` (`0x7fff0000` / LAA `0xffff0000`, [run139 해설 2.5](run139-wow64-kernel-해설.md#25-4gb-상한의-뿌리--highestuseraddress-c8)). 뷰 매핑 담당들이 울타리 안쪽만 고르니, 따로 외치지 않아도 낮은 층에 놓인다.

비유: 옛날식 직원은 **4자리 번지만** 찾아갈 수 있다. 우리 벽 게시판 (R) 은 그래서 낮은 층에 있어야 한다. 그런데 64비트 관리실 절차서엔 "낮은 층에 붙여라" 라는 말이 없다. 대신 **관리사무소가 이 가게 울타리를 4GB 안쪽에 쳐 두었기 때문에** 어디에 붙이든 저절로 낮은 층이다. 사무실 벽 (C) 은 이웃 건물이라 높아도 상관없다 — 우리는 거기서 "몇 칸 떨어졌나" 만 가져오니까.

### 2.9 FALSE 가지 — 전원 켜기가 실패하는 곳 전부 (물음 3 뒤쪽)

**FALSE** = 전원 켜기 함수 (DllMain) 가 "실패" (0) 를 돌려주는 것. 그러면 로더가 `0xc0000142` 로 프로세스 시작을 접는다 ([run27 해설](../02_sprint/run27-init-graph-해설.md)).

안쪽 함수의 FALSE 는 모두 `+0x4d1` 로 모인다 → `+0x4d4 MOV AL,BL` (BL 은 `+0x3d` 에서 0) → `+0x4e0 RET 4`. 그때 `_RegKrnInitialize` (`+0x4ca`) 는 건너뛴다 `[정적]`.

| # | 자리 | 조건 | 아래에서 왜 실패하나 (대표) |
|---|---|---|---|
| F1 | `+0x1bc JS` | `NtQuerySystemInformation(0, …, 0x2c)` 음수 | 통역사가 클래스 0 을 `0x3e` 로 바꿔 묻고 (`…_SystemBasicInformation +0x19b`) 결과를 그대로 올림 |
| **F2** | **`+0x20e JS`** | **`CsrClientConnectToServer` 음수** | **원문 막힘 (2.10)**. 힙 `0xc0000017` · `NtConnectPort` · `NtMapViewOfSection` 실패도 같은 길 |
| F3 | `+0x3bc JZ` | `_BaseNlsDllInitialize@12` 가 0 | `_NlsProcessInitialize@0` 이 0 아닌 값 (`_AllocTables@0` · `_SetupMainNlsFiles@4` · `_MakeLocHashNode@4(0x7f)` 가 0 이면 `0x57`) |
| F4 | `+0x3da JS` | `RtlInitializeCriticalSection` 음수 | 메모리 부족 급 |
| F5 | `+0x3f4 JZ` | `_ConsoleInitialize@0` 가 0 | 콘솔 쪽 넷 (C1 락 · C2 연결 객체 · C3 할당 · C4 표준 입출력 정리) |
| E1 | `entry +0x5a JNZ +0x113` | 안쪽 결과 FALSE | F1 ~ F5 가 여기로 모여 0 반환 |
| E2 | `entry +0x89 JNS` 아니면 `+0x8b` | `ARI::Globals::Initialize` 음수 | **패키지 앱이 아니면 늘 0** (`+0xe0 MOV AL,[PEB32+3]` · `+0xe3 TEST AL,0x10` · `+0xe5 JZ +0x39d`) `[정적]`. 음수는 `AppModelPolicy_GetPolicy` 실패 때 `[추측]` |
| E3 | `entry +0x10c/+0x111` | `_PcwDllMain@12` 가 0 | `NtQueryInformationProcess(−1, 0x1a = ProcessWow64Information, …)` 음수면 (`+0x2a JS`) |

- 모두 `[정적]` (E2 의 음수 까닭만 `[추측]`).
- F5 줄은 결과서가 "`+0x3f2 MOV BL,AL`" 로 적었는데, `.asm` 은 **`+0x3f0 MOV BL,AL` · `+0x3f2 TEST BL,BL` · `+0x3f4 JZ`** 다 (run 문서 "flow-writer 대조" 2).
- E3 의 `NtQueryInformationProcess` 는 **32비트 초기화의 syscall 하나 더** 다. 64비트 test.exe 엔 없는 줄이다.

비유: 전원 켜기 체크리스트에서 **"여기서 빨간 불이면 전원 끔"** 칸이 여덟 개다. 안쪽 다섯 (F1 ~ F5) 은 모두 같은 비상 출구 (`+0x4d1`) 로 나가고, 바깥 `entry` 에 셋이 더 있다.

### 2.10 원문 막힘 — `0xc000003a` 한 번이 `0xc0000142` 가 되기까지

에뮬레이터의 `NtOpenSection(…\SharedSection, 4) = 0xc000003a` 를 한 계단씩 따라간다. 모두 `[정적]` (`.asm` 에서 다시 봄).

1. **64비트 ntdll `CsrpConnectToServer`** — `+0x1bb MOV EDI,EAX` · `+0x1bf JS +0x46a` → `+0x47a/+0x47f RtlFreeHeap(CsrHeap, CsrPortName.Buffer)` → `+0x481 JMP +0xd9` (맺음).
   - 덤: `CsrPortName.Buffer` 는 풀렸지만 전역 값은 0 으로 안 지운다.
2. **64비트 `CsrClientConnectToServer`** — `+0x118 TEST` · `+0x11a JS +0xca`. 출력 바이트는 안 쓴 채 돌아간다.
3. **wow64 `whNtWow64CsrClientConnectToServer`** — 상태를 넣어 두고 (`+0x28`), **안 보고** PEB32 세 칸에 0 을 옮긴 뒤 (2.6), `+0x95` 로 같은 상태를 돌려준다.
4. **32비트 ntdll `CsrClientConnectToServer`** — `+0x1f JS +0x38` (`_CsrClientProcess` 안 씀) → `+0x3b RET 0x14`.
5. **32비트 KERNELBASE** — `+0x20c TEST` · `+0x20e JS +0x4d1` → **F2** → AL 0 → 바깥 `+0x2a ~ +0x2d RET 4` → `entry +0x24 MOV DL,AL` · `+0x5a JNZ +0x113` → 0 반환 (E1).
6. **32비트 ntdll 로더** — `_LdrpInitializeNode@4 +0x189 MOV [EBP-0x1a],AL` → `+0x21e CMP [EBP-0x1a],0` → **`+0x247 MOV [EBP-0x28],0xc0000142`** → 올라가 `__LdrpInitialize@8 +0x2a6 _LdrpInitializationFailure@4` → `+0x5a PUSH 0xc0000145` · `+0x5f NtRaiseHardError`.
   - `+0x21e` · `+0x247` 은 함수 몸이 아니라 점프로 가는 **조각** `FUN_4b2b857e` 안 줄이다 (조각 `+0x15` · `+0x3e`). `.asm` 주석이 함수 기준으로 바꿔 준 값을 썼다.

- 에뮬레이터 로그 657 (`NtRaiseHardError 0xc0000145`) · 658 (`NtTerminateProcess 0xc0000142`) 과 **같은 꼴** 이다. 658 쪽 (`NtTerminateProcess`) 은 안 따라갔다 `[추측]`.
- 그래서 결론: **지금 에뮬레이터의 `0xc0000142` 뿌리는 `NtOpenSection(SharedSection)` 한 번의 실패** 이고, 그 실패는 **F2 하나** 로 모인다 `[정적]`.
- 이걸 고치면 다음에 막힐 곳은 F3 (NLS) · F5 (콘솔) 차례일 것이다 `[추측]` (F4 는 ntdll 메모리 몫).

비유: 게시판 찾기 (첫 단계) 에서 "그런 게시판 없음" 을 듣자, 64비트 절차서는 메모지를 버리고 전화를 접는다 → 통역사는 빈칸을 베끼고 "안 됐어요" 를 그대로 전한다 → 옛날식 관리실 메모도 "안 됐어요" → KERNELBASE 전원 켜기가 빨간 불 (F2) → 로더가 "이 건물 전원이 안 켜짐 (`0xc0000142`)" → 오류 창 신호 (`0xc0000145`).

### 2.11 썽크 넷 — 이 길엔 안 끼지만, 읽어 둔 꼴 (물음 2, 짧게)

원문이 물은 썽크 넷은 **CSR 길엔 안 낀다** (2.4). 그래도 32비트 코드가 이 넷을 **직접** 부를 때를 위해 꼴을 읽어 두었다 `[정적]`.

- 입구 `RCX` = 32비트 스택 인자 위치. **핸들은 부호 확장** (맨 위 비트를 위 빈칸에 복사해 채우기 — 음수 핸들이 음수로 남게 `[추측]`), 포인터 · 정수는 0 확장.
- 구조체 늘이기 · 줄이기 (`whNtConnectPort` · `whNtSecureConnectPort` 가 `Wow64pThunkLegacy*` 넷으로):

| 구조체 | 32비트 | 64비트 | 들어갈 때 | 나올 때 |
|---|---|---|---|---|
| `PORT_VIEW` (같이 쓸 메모장 정보) | `0x18` | `0x30` | `Length == 0x18` 일 때만 늘림, 아니면 포인터 그대로 | 하위 4 B 로 자름 — **4GB 넘는지 안 봄** |
| `REMOTE_PORT_VIEW` | `0xc` | `0x18` | `Length == 0xc` 일 때 늘림 | 하위 4 B |

- 썽크마다 한 줄씩:

| 썽크 | 4GB 아래 요구 | 상태를 바꾸나 |
|---|---|---|
| `whNtOpenSection` | 없음 | 이름 (OA) 변환 실패일 때만 |
| `whNtCreateSection` | 없음 | 같음 |
| `whNtConnectPort` | **없음** (뷰 주소는 커널이 정하고, 나올 때 자르기만) | 뷰 변환 실패일 때만. 연결 정보 · 길이 포인터는 **그대로**. 성공이고 옛 LPC 포트면 핸들 `\|= 1` 표시 |
| `whNtSecureConnectPort` | 없음 | 위와 같음 (`\|= 1` 표시는 없음) |
| `whNtMapViewOfSection` | **있음** — `*Base == 0` 이고 `ZeroBits == 0` 이면 `ZeroBits ← Wow64pMaxAddress` (`+0xbe`) | 기계 불일치면 `0x4000000e` 등. `ViewSize` 는 4GB 넘으면 `0xffffffff` 로 줄임 |

- 정리: **포트 뷰를 4GB 아래로 요구하는 줄은 없다** (넘으면 조용히 잘림). 요구가 있는 곳은 `whNtMapViewOfSection +0xbe` 하나 `[정적]`.
- 덤: 이름 바꾸기 `RedirectPath` 는 `\Sessions\1\Windows\SharedSection` 같은 이름을 **안 바꾼다** (`0xc0000272` 를 주지만 OA 썽크가 무시) `[정적]`. `0xc0000272` 이름 뜻은 `[공백]`.
- 덤: `whNtConnectPort` 의 상태 줄에 `RemoteViewOut` 실패 (`+0x1e5/+0x1ea` → `+0x1f2 JNS` 아니면 `_local_unwind`) 도 있다 — 결과서가 빠뜨린 것을 flow-writer 가 보탰다 (run 문서 "flow-writer 대조" 5).

### 2.12 csrss 쪽 — WoW64 손님을 따로 다루지 않는다 (짧게)

- 읽은 세 함수 (`CsrApiHandleConnectionRequest` · `CsrpHandleInlineUserConnect` · `CsrProcessLazyRegister`) 안에 **WoW64 갈래가 없다** `[정적]`.
  - `ProcessWow64Information (0x1a)` 질문도, 기계 `0x14c` 비교도 없다. csrsrv 함수 129개 이름에 `wow` · `32` 도 없다 (결과서).
  - `CsrProcessLazyRegister` 의 `NtQueryInformationProcess` 둘은 `0x18` (세션) · `0x25` (이미지 정보) 다.
- 그래서 → 64비트 손님과 32비트 손님이 **같은 길** 이다. 2.4 비유의 "사무실 쪽에서 보면 64비트 손님 전화와 똑같다" 가 이 뜻이다.
- 게시판은 64비트 csrss 의 벽에 붙어 있어 C 가 높은 번지다 → WoW64 손님은 **C ≠ R 이 정상** (2.8).

### 2.13 PE_emulator 쪽 — 무엇을 대답해야 하나 (짧게)

run 문서 "PE_emulator 쪽" 을 줄였다. 고칠지 · 어느 길로 갈지는 **사용자 · PE_emulator 팀이 정한다**. flow-writer 는 원문과, 결과서가 적은 PE_emulator 줄 번호가 있는지만 봤다.

**지금 코드와 맞대기**

- 64비트 프로세스에선 엔진이 연결 결과를 **직접 써 왔다** (`Src/ntdll/BaseDllInit.h:30 ~ :31`, syscall 없음). **WoW64 엔 그대로 못 쓴다** — 32비트 KERNELBASE · wow64 · 64비트 ntdll 이 진짜 코드로 돌아 **진짜 syscall 여섯이 `TeamSyscall` 로 들어온다**.
- `MapCsrSharedSectionView` · `MapCsrPortView` (`Src/nt/AddressSpace.cpp:1421 ~ 1450`) 의 위 끝이 `SYSTEM_MAXIMUM_USER_MODE_ADDRESS` 다 → WoW64 엔 **`qwHighestUserAddress − 1`** 로 (뷰 R < 4GB, 2.8).
- `NtConnectPort` 쪽엔 이번에 필요한 대답 (응답 블록 · 포트 뷰) 이 없다. `csr_user_connect_stub.cpp` 는 USERSRV (서버 3) 만 대답해서 **이 길이 아니다**.

**stub 이 채울 최소 목록 (길 A — 64비트 syscall 모양 그대로)**

| # | syscall | 돌려줄 것 | 핵심 조건 |
|---|---|---|---|
| 1 | `NtOpenSection` (`\Sessions\<세션>\Windows\SharedSection`) | 성공 + 핸들 | **`0xc000003a` 면 2.10 길로 `0xc0000142`** |
| 2 | `NtCreateSection` (이름 없는 `0x10000` B) | 성공 + 핸들 | — |
| 3 | `NtConnectPort` (`…\Windows\ApiPort`, 서버 1, 블록 `0x30` B) | 성공 + 핸들 · `ViewBase` · `ViewRemoteBase` · 블록 `+0x00` C · `+0x08` T · `+0x10` PID · `+0x24` 0 | 포트 뷰 읽기 · 쓰기 (그 안에 힙이 생김) |
| 4 | `NtClose` | 성공 | — |
| 5 | `NtMapViewOfSection` (읽기 전용) | 성공 + `*R` · `*크기` | **R < 4GB 필수** |
| 6 | `NtClose` | 성공 | — |

- 뷰 안 내용은 run71 "최소 칸 목록" 과 같다 ([run71 해설 2.6](run71-named-obj-dir-해설.md#26--pe_emulator-가-지을-최소-칸)). 다만 32비트 4 B 셈이 맞으려면 **T − C · S − C 가 4GB 안**.
- R == C 로 줄여도 되는데, 그러면 WoW64 에선 C 도 4GB 아래여야 한다.
- 먼저 있어야 하는 칸: TEB64 `+0x180c` = `0x2000` · TEB32 `+0x30` = PEB32 · PEB64 `+0x10` = 32비트 exe · PEB64 `+0x30` 힙 · PEB32 `+0x1d4 SessionId`.

**길 A / 길 B**

- **길 A**: 위 여섯을 `TeamSyscall` 이 대답. 진짜 코드가 다 돈다 (지금 로그와 같은 길).
- **길 B**: `0x1e9` 하나를 가로채 PEB64 세 칸 · PEB32 세 칸 · 64비트 `Csr*` 9개를 엔진이 직접 쓴다. wow64 몸을 건너뛰는 선택이라 "진짜 코드" 원칙과 어긋날 수 있다 `[추측: 팀 규칙 확인 필요]`.
- 2.4 비유로 말하면: 길 A 는 "64비트 절차서를 진짜로 밟게 하고, 사무실 역할을 해 준다", 길 B 는 "통역사가 전화한 척하고 번지만 적어 준다".

### 2.14 그 밖에 — 대조 · 정정 (짧게)

- **결론을 바꾸는 어긋남은 없다** (run 문서 "flow-writer 대조"). 표기만 고친 곳: `Wow64InitializeEmulatedSystemInformation` 의 call `+0x33` / ret `+0x3a` (결과서 `+0x2f/+0x33`), F5 줄 (2.9), ntdll32 인자 넷 + 자기 바이트 (2.3), TEB64 `+0x180c` 0 · 음수일 때 (2.6), `whNtConnectPort` 상태 줄 보탬 (2.11), `_LdrpInitializeNode` 조각 (2.10).
- **에뮬레이터 값은 `[관찰]` 아님**: 결과서의 "`[관찰 ↔ 정적]`" (rip) · "`[정적 순서 ↔ 관찰]`" (seq 647 → 648) · "648 까지 갔으니 있음 `[관찰]`" 은 요청 쪽 에뮬레이터 로그와 맞춘 것이다. run 문서는 태그를 안 붙였다 (조심할 곳 2).
- **run138 좁힘** — 2.4 끝.
- **run68 · run139 · run140 은 어긋남 없음**: run68 의 여섯 · PEB 세 칸 · `Csr*` 9개는 "WoW64 에서도 같은 64비트 코드가 낸다" 로 **넓어질 뿐** 이다.
- run152 를 쓰는 중 **run151 문서도 대조** 했다 (combase 오프셋 세 벌 · `+0xd8` 두 칸). 그 이야기는 run151 몫이라 여기서 풀지 않는다.

### 2.15 비유로 처음부터 끝까지 한 번 더

1. x86 exe 가게. 관리사무소가 울타리를 4GB 안쪽에 치고, 옛날식 안내판 (PEB32) 을 세운다. 다만 **게시판 번지 칸 셋은 비워 둔다** (run139 27칸 밖).
2. 통역사가 옛날식 사무실을 꾸미고 직원을 들여보낸다 (run138). 옛날식 관리실이 가게를 세팅하다가 KERNELBASE 건물 전원을 켠다 (run140).
3. 전원 켜기 중간에 인사팀 사무실에 **첫 전화** 를 걸어야 한다 (`+0x206`).
4. 옛날식 관리실엔 전화 거는 법 책이 없다. **"통역사에게 부탁" 메모** (62 B) 대로, 8칸짜리 비상 연락처 쪽지 (`{CtrlRoutine32, 0}`) 를 접어 번호표 `0x1e9` 로 문을 나선다.
5. 통역사 (`whNtWow64CsrClientConnectToServer`) 는 **64비트 관리실의 첫 전화 절차서를 통째로 빌려** 그대로 밟는다 — 게시판 찾기 · 메모장 만들기 · 전화 · 내려놓기 · 게시판 펼치기 · 내려놓기. 받아 적는 칸은 64비트 칠판과 64비트 안내판이다.
6. 사무실은 64비트 손님 전화와 똑같이 받는다. 게시판은 우리 벽 낮은 층 (R) 에 붙는다 — 절차서가 시켜서가 아니라 **울타리 때문에**.
7. 통역사는 통화 결과를 **안 보고** 번지 셋을 옛날식 안내판에 베낀다.
8. 옛날식 관리실 메모는 "통화 됐음" (`_CsrClientProcess ← 1`) 을 적는다.
9. KERNELBASE 는 옛날식 안내판 번지로 **같은 안내문 같은 줄** 을 4자리 수첩에 옮겨 적는다.
10. 게시판 찾기에서 실패하면 → 빈칸 베끼기 → "안 됐어요" → F2 빨간 불 → `0xc0000142` → 오류 창 신호 `0xc0000145`. 에뮬레이터가 막힌 곳이 바로 여기다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

그림은 안 바뀌었다 (그림 1 · 2 에 WoW64 노드가 없다). 바뀐 건 글이다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (PDB 줄) | 32비트 KERNELBASE 줄 없음 | **새 줄 `wkernelbase.pdb`** (kernelbase32, 처음 analyze). wntdll · wow64 · csrsrv 줄에 run152 한 마디씩 (더 뽑은 함수) |
| 기준 근거 줄 | — | run152: 새 사진 읽기 없음. test.exe 가 이 길을 안 탄다는 앞 run 줄만 다시 짚음. 요청 쪽 값은 에뮬레이터 로그라 `[관찰]` 로 안 씀 |
| 3장 run138 절 "syscall 다리" | `whNt*` → 64비트 ntdll `Nt*` → `syscall` `[정적]` | 같은 줄 + **(run152) 좁힘** — 칸 `0x1e9` 는 64비트 `CsrClientConnectToServer` 를 통째로 부름 |
| 3장 run140 절 뒤 | 없음 | **새 절 "WoW64 (x86) 초기화 — 32비트 kernelbase 의 csrss 첫 연결"** (나무 + `[정적]` 줄 넷: 32비트엔 `CsrpConnectToServer` 없음 · 연결 블록 · PEB32 세 칸과 4GB · FALSE 가지) |
| 4장 | — | Q287 해결됨 줄 하나 + run152 새 줄 셋 (동적 Q84 · Q253d · Q262 얹기 / 정적 다음 후보 1 ~ 6 / PE_emulator 쪽) |
| 5장 | — | run152 근거 줄 둘 |

**짐 지도 (`baggage.md`)** — 셈은 그대로 (2장 숫자 · 3장 줄 수 · 7.6.17 "줄 35"). test.exe (x64) 엔 이 길이 안 생겨서다 `[관찰]`. 7.6.17 (라) 표 아래 메모 두 줄만:

1. PEB32 `+0x4c` · `+0x54` · `+0x248` 은 **wow64 가 쓴다** (커널 27칸 밖, 상태 안 봄). 32비트 `Csr*` 는 `_CsrClientProcess` 하나, 진짜 `Csr*` 9개 · PEB64 세 칸은 64비트 ntdll.
2. CSR 공유 섹션 뷰 R 은 **4GB 아래여야** — 사용자 코드엔 요구가 없고 커널 울타리에 기댐. "32비트 `Nt*` 스텁 → … → 64비트 `Nt*`" 놓는 법에 예외 하나 (`0x1e9`).

**이전 문서** — run138 문서 · 해설 맨 위 "고쳐 읽기 (run152)" 한 줄씩 (2.4 끝).

### 큰 그림에서 어떻게 이어지나

**WoW64 지도가 "문 안에서 처음 하는 바깥 일" 까지 왔다.**

```
 커널이 32비트 짐을 짓는다              (run139)
   PEB32 27칸 · TEB32 · 울타리 4GB …   ── 게시판 번지 칸 셋은 비워 둠
        ▼
 64비트 쪽 DLL 이 문까지 데려간다         (run138)
        ▼
 32비트 ntdll 이 가게를 세팅한다          (run140)
        ▼
 32비트 KERNELBASE 전원 켜기 → csrss 첫 전화   (run152)                ← 여기
   통역사가 64비트 몸을 통째로 빌림 → 번지 셋 베끼기 → BSSD 4 B 셈
        ▼
 그다음 전원 켜기 단계: NLS (F3) · 콘솔 (F5) …   (Q295 — 다음 벽 후보)
```

- 위 조각과 이어지는 곳: run139 가 "커널이 안 쓴다" 고 남긴 PEB32 칸 중 셋의 **주인이 wow64** 로 드러났다. run139 의 울타리가 이번에 **CSR 뷰 4GB 의 유일한 지킴이** 로 쓰였다.
- 옆 조각과 이어지는 곳: run138 의 "통역 담당 = 한 장씩 번역" 그림에 **예외 하나** 가 생겼다. 앞으로 다른 `whNt*` 를 볼 때도 "이 담당은 `Nt*` 를 부르나, 다른 64비트 함수를 통째로 부르나" 를 먼저 봐야 한다.
- 아래 조각과 이어지는 곳: F2 가 풀리면 에뮬레이터는 F3 (NLS) · F5 (콘솔) 로 갈 것이다 `[추측]`. 그 길이 Q295 의 앞 줄이다.
- test.exe 지도 자체는 그대로다. run68 의 64비트 연결 줄이 "WoW64 에서도 같은 코드가 돈다" 로 뜻이 넓어졌을 뿐이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **진짜 VM 에서 `NtOpenSection(SharedSection)` 이 성공하나**, 핸들 · 이름 | 정적만 | 동적 1 (`ntdll!CsrpConnectToServer+0x1bb`) → **Q84 · Q253d** |
| 응답 블록 값 · 포트 뷰가 4GB 아래인가 · `CsrPortHandle` | 사무실이 주는 실행 때 값 | 동적 2 (`+0x2e4`) → **Q84 · Q262** |
| **R 이 정말 4GB 아래인가**, R vs C | 울타리가 지킨다는 건 정적 추론, 값은 실행 때 | 동적 3 (`+0x33d`) → **Q262** (`MiComputeProcessUserVa+0xaf` 와 같이) |
| PEB32 세 칸이 연결 전 0 → `+0x95` 에서 채워지나 | 실행 때 값 | 동적 4 (`wow64!whNtWow64CsrClientConnectToServer+0x95`) → **Q253d** |
| 32비트 KERNELBASE 의 연결 인자 · 결과, BSSD 우리 번지 · `+0x9e8` · 표 `T[1]`, F3 · F5 · 안쪽 결과, E3 `0x1a` 결과 | 실행 때 값 | 동적 5 ~ 8 (`KERNELBASE!__KernelBaseBaseDllInitialize@12+0x20c/+0x248/+0x3ba/+0x3f0` · `entry+0x1b` · `_PcwDllMain@12+0x28`) → **Q84 · Q253d** |
| 32비트 모듈을 WinDbg 가 어떤 이름 (`.effmach x86` · `@N` · `ntdll_<기준>` 류) 으로 보일지 | VM 을 안 봄 | `[추측: 심볼 표기]` — 동적 때 확인 |
| **CSR 다음 벽** — 32비트 NLS (F3) · 콘솔 (F5) 아래 syscall, `whNtQueryInformationProcess` · `PcwDllMain`, 32비트 kernel32 BSSD 폭, 32비트 user32 csrss 연결, `RedirectPath` `0xc0000272` 이름 | 이번 범위 밖 | **Q295** (정적) |
| 커널의 옛 `NtConnectPort` → ALPC 변환 몸 | 안 읽음 `[공백]` | **Q295** (정적) |
| `ARI::Globals::Initialize` 가 음수를 내는 가지 | 비패키지는 늘 0 까지만 봄 | `[공백]` (E2 의 `AppModelPolicy_GetPolicy` 실패 `[추측]`) |
| 658 `NtTerminateProcess 0xc0000142` 쪽 길 | 안 따라감 | `[추측]` |
| 에뮬레이터 seq 648 이 왜 `0xc000003a` 였나 | 팀 코드 쪽 문제 | 에뮬레이터가 공유 섹션 이름을 객체 이름공간에 안 갖고 있어서일 것 `[추측]` — PE_emulator 쪽 |

- 동적 8줄은 모두 **x86 시험 exe** 가 있어야 한다 (사용자 결정). 새 번호 없이 Q84 (WoW64 판) · Q253d · Q262 에 얹었다.
- 디버거 없이 VM 으로 떠 올 것은 **없다** — 뷰 주소 · PID · 응답 블록 · 핸들이 모두 실행 중 값이라서 (결과서).
- 디버거가 붙어서 달라지는 점은 이번에 읽은 줄 안에선 못 봤다 `[정적: 읽은 줄 안에서]`.

---

## 5. 더 읽기

- run 문서: [`run152-wow64-csr.md`](run152-wow64-csr.md) — 호출 지점 표 전체, BSSD 칸 표, FALSE 가지 표, syscall 차례 표, 연결 블록 표, 쓰는 칸 표, 썽크 넷 표, PE_emulator 쪽 (stub 최소 목록 · 길 A / B), flow-writer 대조, 정정
- 결과서: [`_workspace/sast/147_wow64-csr_result.md`](../../_workspace/sast/147_wow64-csr_result.md) — "먼저 읽는 한 줄 답", 3-1 안쪽 함수 호출 차례 (a ~ m), (4-c) 콘솔 FALSE 넷, VM 멈춤점 8줄, 다음 요청 후보
- 요청서: [`_workspace/sast/147_wow64-csr.md`](../../_workspace/sast/147_wow64-csr.md)
- flow.md: [3장 "WoW64 (x86) 초기화 — 32비트 kernelbase 의 csrss 첫 연결"](flow.md), 3장 run138 절 "syscall 다리" 줄, 4장 run152 줄, 기준 `wkernelbase.pdb` 줄
- 짐 지도: [`baggage.md`](baggage.md) 7.6.17 메모 두 줄
- 앞 해설: [run68 해설 2.4](run68-dll-init-rest-해설.md#24--csrss-연결--이웃-인사팀-사무실에-처음-거는-전화) (64비트 첫 전화 · 여섯 · 번지 옮겨 적기), [run71 해설 2.4 ~ 2.6](run71-named-obj-dir-해설.md#24-안내문은-누가-언제-붙이나--새-말-공유-섹션) (게시판 · 안내문 · `+0x9e8` · 최소 칸), [run138 해설](run138-wow64-dlls-해설.md) (통역사 · 안내판 4장 · 맨 위 고쳐 읽기), [run139 해설 2.5 · 2.6](run139-wow64-kernel-해설.md#25-4gb-상한의-뿌리--highestuseraddress-c8) (울타리 · PEB32 27칸), [run140 해설 2.10 · 2.12](run140-wow64-ntdll32-해설.md#210-nt-스텁-꼴--바이트-한-줄씩-b3) (스텁 꼴 · `NtContinue` 도 64비트 `NtContinue` 가 아닌 예), [run27 해설](../02_sprint/run27-init-graph-해설.md) (`0xc0000142`)
- 원 요청: `../PE_emulator/_workspace/05_tuning_planner/374_internals-wow64-csr.md`

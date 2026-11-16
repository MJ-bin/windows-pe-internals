# run51 eprocess-later — 해설

> 이 글은 [run51 문서](run51-eprocess-later.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

> **고쳐 읽기 (run120)**: 2.8 의 `DeviceMap` "ntdll 쪽 호출자 `[공백]`" 은 이제 `[정적]` 이다 — `LdrpInitializeProcess +0x1517 → RtlpInitCurrentDir +0x32 → RtlpCreateNewDirectoryReference +0xbb NtOpenFile` (이름 `\??\` + 현재 디렉터리, `P1~P1a`). 탔는지는 동적 그대로 (F5). 2.6 의 `ModifiedPageCount` "어느 목록인지 `[추측]`" 은 **변경 목록 (`ModifiedPageList`) `[정적]`** 이고, 0 으로 되돌리는 줄 `MiUserFault +0x116` (16 넘을 때만, test.exe 는 안 탐) 이 더 있다. 4절 표의 Q99 줄은 끝났다. 본문은 그대로 두었다 → [run120 해설](run120-eprocess-locks-해설.md) 2.8 · 2.9

- 대기열: **Q78c2b**: Q78c2 (`_EPROCESS` 모르는 칸 241) 를 시간 순서로 나눈 **둘째 조각**. "만든 뒤" 몫 29칸 (`K2~K3` 3 · `K3~P0` 11 · `P0~P1` 9 · `P1~P1a` 5 · `P1a~P1b` 1) + `OwnerProcessId` 둘째 쓰기 + 옛 Q65 몫 (`_workspace/plan.md`, 이번에 **끝**)
  - 이걸로 `_EPROCESS` 의 모르는 칸은 **1칸** (`SharedCommitCharge`) 만 남았다.
  - 다음 항목: **Q78d** (첫 스레드) → Q78e → Q78f → Q78g → Q79 → Q80 → Q82.
  - 남은 질문이 간 곳: 덤프로 답할 것 F1·F7·F10 → **Q79** (출처 추가) · F12 → **Q97** (run50 의 E5 와 같은 질문) · 실행 중 멈춤 F2 ~ F13 나머지 → 새 **Q98** · 작은 정적 → 새 **Q99**.
- 근거:
  - run 문서 [`run51-eprocess-later.md`](run51-eprocess-later.md) (가장 중요)
  - 요청서 [`_workspace/sast/45_eprocess-later.md`](../../_workspace/sast/45_eprocess-later.md)
  - 결과서 [`_workspace/sast/45_eprocess-later_result.md`](../../_workspace/sast/45_eprocess-later_result.md) (판정: **분석성공**)
  - 짐 지도 [`baggage.md`](baggage.md) 의 고친 곳: 기준 · 쓰는 말 · 1장 · 2장·2.1 · 3장 읽는 법 · 3.2 새 줄 · **3.11 다시 씀** · 5장 · 7.1·7.2·7.4·7.5·7.6·7.8 · 새 7.14 · 8장
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (기준 · 2.1 `PspAllocateProcess`·`PspInsertProcess`·`PspInsertThread` · 2.2 csrss 절 · 2.3 트리와 `PspUserThreadStartup` 절 · 2.4 새 절 · 2.5 KERNELBASE 시작 함수 · 4장 · 5장. 그림은 안 바뀜)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 값 대조는 run44 때 찍어 둔 사진 구간으로만 했다. 그래서 이 글의 "사진" 은 전부 run44 사진이다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- `_EPROCESS` = 관리사무소의 **가게 장부**, 보통 길 / 복제 길 → [run10 해설 껍데기와 장부](../02_sprint/run10-allocate-process-해설.md#껍데기와-장부) · [보통 길과 복제 길](../02_sprint/run10-allocate-process-해설.md#보통-길과-복제-길)
- 상가 명부 (`PspInsertProcess`) → [run11 해설 2.8](../02_sprint/run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다)
- csrss = **인사팀 사무실** (run 문서는 "**주민센터**" 라고 부른다. 같은 것) → [run14 해설 2.6](../02_sprint/run14-create-process-internal-해설.md#26-3-그-사이-부모가-하는-일--검사--csrss-알림--후처리) · [run34 해설](../02_sprint/run34-parent-leftovers-해설.md)
- `PspUserThreadStartup` = 첫 직원의 **출근 도장 창구** (신분증 바꿔치기 금지 · "새 직원 왔어요" 알림 …) → [run15 해설 2.8](../02_sprint/run15-user-thread-startup-해설.md#28-pspuserthreadstartup-안--출근-도장-다섯-칸)
- `OwnerProcessId` = 장부의 **주인 란**, 그리고 옛 Q65 가 생긴 자리 → [run33 해설 2.6](../02_sprint/run33-first-resume-해설.md#26-이-검사는-무엇을-지키나--짐작의-고리-추측) · [run34 해설](../02_sprint/run34-parent-leftovers-해설.md)
- 토큰 = **신분증**, 운영 카드 (`_KPROCESS`), PPM 정책과 **BAM** (= 옆 건물 **절전 관리팀**) → [run49 해설](run49-kprocess-token-htable-해설.md) · [새 말: PPM 정책과 BAM](run49-kprocess-token-htable-해설.md#새-말-ppm-정책과-bam)
- 사진 지점 K1 · K2 · K3 · P0 · P1 · P1a · P1b · P2 와 "처음 값을 갖는 구간" → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가) · [2.10](run44-baggage-snap-해설.md#210-새-말--처음-값을-갖는-구간)
- "0" 의 세 가지 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀) → [run47 해설 2.4](run47-section-chain-해설.md#24-새-말-0-에는-세-가지가-있다-0-을-씀--아무도-안-씀)
- "범위 밖 길에서만 씀 `[추측]`", 오프셋 훑기의 오탐 → [run50 해설 2.4](run50-eprocess-create-해설.md#24-새-셈-범위-밖-길에서만-씀-추측) · [2.5](run50-eprocess-create-해설.md#25-새-말-오프셋-훑기의-오탐)
- IFEO (특별 지시 서랍) → [run46 해설 2.6](run46-process-heap-해설.md#26-ifeo-가-이-구간에서-처음-열린다)
- 쿠키 (`RtlpGetModifiedProcessCookie` → `NtQueryInformationProcess(0x24)`) → [run46 해설 2.12](run46-process-heap-해설.md#212-시스템-콜-12개--한-표로)

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, `_EPROCESS` = 가게마다 두는 **가게 장부**. cmd.exe = **부모 가게**.
- 첫 스레드 = **첫 직원**. `PspUserThreadStartup` = 첫 직원의 **출근 도장 창구** (run15).
- run 문서는 `_EPROCESS` 를 "영업 신고서", 관리사무소를 "본사" 라고 불렀다 (run50 비유 이어서). 이 글은 run10 부터 쓴 **가게 장부 / 관리사무소** 를 그대로 쓴다. 뜻은 같다.
- run 문서의 이번 비유는 그대로 가져온다. 장부를 채우는 사람이 넷이다:
  - **관리사무소 직원** (nt 직접)
  - **가게 주인 자신** (test.exe 의 syscall)
  - **인사팀 사무실 / 주민센터** (csrss)
  - **경비 업체** (드라이버)

### 한 줄로 먼저: 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| `_EPROCESS` 남은 모르는 칸 | **30 → 여전히 모름 1** (`SharedCommitCharge`, 후보만 좁힘) |
| 뒤 구간 29칸, 쓰는 줄 | **모두 nt 안** `[정적]`. 29칸 모두 `.asm` 에서 확인 |
| 그런데 누가 "부르나" | nt 직접 **12** · test.exe 가 부른 syscall 안 **5** · 다른 프로세스·드라이버가 부름 **12** |
| `OwnerProcessId` 둘째 쓰기 | **`NtSetInformationProcess` 클래스 `0x31` `+0x1ecd`**, 부르는 쪽 **`KERNELBASE!ConsoleCommitState +0xe9`** `[정적]` |
| 옛 Q65 | **닫힘.** `+0x1ecd` = 위 줄. `ObCloseHandleTableEntry +0x7a9` 인자는 짐에 영향 없음 |
| `OwnerProcessId` 의 뜻 | "부모 PID" 가 아니라 **`NtCreateUserProcess` 를 부른 프로세스의 PID**. 뒤에 **콘솔 호스트 PID `\|1`** 로 덮임 (정정) |
| 오기 하나 | `PspDisablePrimaryTokenExchange +0xa4` 는 없는 줄 → **`+0xc4`** (정정) |

이 글에서 붙잡아 둘 두 줄:

> **(1) 만든 뒤의 칸은 "쓰는 줄" 은 다 nt 안인데, "부르는 쪽" 은 셋으로 흩어진다.**
> 관리사무소가 스스로 하는 일 / 가게 주인 (test.exe) 이 창구에 와서 부탁한 일 / **다른 사람** (csrss·드라이버) 이 이 가게 장부를 고쳐 달라고 부탁한 일.
>
> **(2) 셋째 무리는 에뮬레이터에 "부탁하러 올 사람" 이 없다.**
> 그래서 누가 대신 채울지 (nt 엔진이 흉내 / 처음부터 값으로 넣기 / 비워 두기) 를 따로 정해야 한다 `[추측]` (2.11).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run49·run50 은 **장부를 만드는 순간** (`PspAllocateProcess` 안, 사진 `K1 이전`) 이었다. 이번엔 그 **뒤** 다. 장부가 만들어진 다음, 초기화가 끝날 때까지 칸이 어디서 채워지나.

그래서 이번 구간은 한 군데가 아니라 **여러 군데에 흩어져** 있다.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 커널, 부모 cmd 스레드 안
  NtCreateUserProcess
   ├─ +0xc36  PspAllocateProcess      장부 만들기 (run49·run50)        K1 이전
   ├─ +0xfc2  PspInsertProcess        상가 명부에 올리기               ← 이번 (K2~K3)
   └─ +0x101a PspInsertThread         첫 직원 달기                     ← 이번 (K2~K3)
 부모 마무리 ~ 자식 첫 스레드 출발 사이: csrss · 드라이버               ← 이번 (K3~P0)
 test.exe 첫 스레드, 커널: PspUserThreadStartup (출근 도장)            ← 이번 (P0~P1)
 test.exe 첫 스레드, ntdll: LdrpInitializeProcess 앞쪽의 syscall        ← 이번 (P1~P1a)
   KERNELBASE 시작 (ConsoleCommitState) · win32k                        ← 이번 (P1a~P1b)
   … NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리 (콜스택에서 보이는 숫자). run 문서 "호출 관계" 에서 대표만 골랐다.
- 오른쪽 `K2~K3` 같은 이름은 "그 칸이 **처음 값을 갖는 사진 구간**" 이다 (run44). 예: `P0~P1` = P0 사진엔 0, P1 사진엔 값이 있는 칸.

### 무엇이 궁금했나

run50 이 끝났을 때 `_EPROCESS` 모르는 칸이 **30** 남았다. `K1 이전` 1 (`SharedCommitCharge`) + 뒤 구간 29.

뒤 구간 29칸은 사진으로 "언제 생겼는지" 는 안다. 몰랐던 건 이것이다.

1. 칸마다 **쓰는 줄** (함수+오프셋).
2. 그 줄이 **누가 부른 길 안**인가: 커널 혼자 / test.exe 가 부른 시스템 콜 / 다른 프로세스·드라이버가 부른 시스템 콜. 정보 클래스 번호까지.
3. 짐 지도 3.11 머리의 "**Q78c2b 에서 볼 것**" 메모가 맞나.
4. `OwnerProcessId` 가 `P1a~P1b` 에 `0x19a8` → `0x729` 로 바뀌는 둘째 쓰기, 그리고 옛 Q65 남은 둘 (`NtSetInformationProcess +0x1ecd`, `ObCloseHandleTableEntry +0x7a9` 인자).

- 범위 밖: 이미 닫힌 칸, 사용자 모드 쪽 본문 (KERNELBASE·csrss·win32k·드라이버는 이름만), 종료 길.

### 바퀴 전에 알던 것

- `[관찰]` (run44) 29칸이 각각 어느 사진 구간에서 처음 값을 갖는지. `OwnerProcessId` `0x19a8` → `0x729`, 매개변수 `ConsoleHandle` 4 → `0x60` 이 `P1a~P1b` 에 바뀐다는 것.
- `[정적]` (run15) `PspUserThreadStartup` 정상 길 4개의 **자리**. 그런데 1번 (`PspDisablePrimaryTokenExchange`) 과 4번 (`PspNotifyThreadCreation`) 의 **본문**은 이름으로만 짐작했었다. (1번은 run49 가 일부 읽음.)
- `[정적]` (run49) `PspSetProcessPpmPolicy` 는 직접 부르는 줄이 없고, 함수표 `PspBamHostInterface` 로만 가리켜진다. 부르는 쪽 bam.sys 는 `[추측]`.
- `[정적]` (run50) 3.11 머리에 지나가며 본 쓰는 줄 메모 ("Q78c2b 에서 볼 것"): `PspInsertProcess +0x202`, `PspInsertThread +0x678`·`+0x67f`·`+0x77b`, `PspDisablePrimaryTokenExchange` 의 얼림 비트 줄 등.
- `[정적]` (run46) ntdll `RtlpGetModifiedProcessCookie` 가 `NtQueryInformationProcess` 클래스 `0x24` 를 부른다. 클래스 이름 `ProcessCookie` 는 `[추측]`.
- 옛 Q65 (run33 에서 생김): `OwnerProcessId` 를 쓰는 다른 줄 `NtSetInformationProcess +0x1ecd` 는 어느 정보 클래스인가.

---

## 2. 찾은 것

### 2.1 한눈에: 30칸은 어디로 갔나

run 문서 맨 앞 표 그대로다.

| 구간 | 칸 | nt 직접 | syscall 안 (test.exe 가 부름) | 다른 프로세스·드라이버가 부름 | 여전히 모름 |
|---|---|---|---|---|---|
| `K1 이전` | 1 | — | — | — | **1** (`SharedCommitCharge`, 후보 좁힘) |
| `K2~K3` | 3 | **3** | — | — | — |
| `K3~P0` | 11 | — | — | **11** (csrss 2 · `mssecflt.sys` 6 · bam 3 `[추측]`) | — |
| `P0~P1` | 9 | **9** | — | — | — |
| `P1~P1a` | 5 | — | **5** | — | — |
| `P1a~P1b` | 1 | — | — | **1** (win32k 콜아웃) | — |
| **합 (30)** | **30** | **12** | **5** | **12** | **1** |
| `OwnerProcessId` 둘째 쓰기 | +1 | — | **1** (KERNELBASE 가 부른 `NtSetInformationProcess`) | — | — |

열 뜻 (run 문서):

- **nt 직접** = 커널이 자기 길 (프로세스·스레드 넣기, 첫 스레드 시작) 에서 쓴다. 시스템 콜이 아니다.
- **syscall 안 (test.exe 가 부름)** = test.exe 자신의 ntdll·KERNELBASE 가 부른 시스템 콜 안에서 커널이 쓴다.
- **다른 프로세스·드라이버가 부름** = csrss 나 드라이버가 부른 시스템 콜 (또는 nt 공개 함수) 안에서 커널이 쓴다.

눈여겨볼 것:

- **구간과 무리가 거의 딱 맞는다.** `K2~K3`·`P0~P1` 은 전부 nt 직접, `P1~P1a` 는 전부 test.exe, `K3~P0` 는 전부 남. 사진 구간이 "누가 일하는 시간인가" 와 겹친다는 뜻이다 `[관찰 간접]` (run 문서 "확인된 것": 사진 구간이 코드 단계와 모두 맞다).
- "다른 프로세스·드라이버" 12칸 중 **부르는 쪽 이름이 `[정적]` 인 건 csrss 2칸뿐**이다. `mssecflt.sys`·`bam.sys`·win32k 는 호출 줄은 있지만, 이번 test.exe 실행에서 정말 그쪽이 불렀는지는 `[추측]` 이다 (동적 F2·F3·F9).

### 2.2 새 말: 정보 클래스

이번 바퀴 거의 모든 줄에 나오는 말이라 먼저 푼다.

- `NtSetInformationProcess` (프로세스 정보 설정) / `NtQueryInformationProcess` (프로세스 정보 조회) 는 **창구 하나에 일이 수십 가지**인 시스템 콜이다.
- 어떤 일인지는 둘째 인자 **번호**로 정한다. 이 번호가 **정보 클래스** 다.
- 비유: 관리사무소 **민원 창구** 하나. 신청서 맨 위에 "**신청 종류 번호**" 를 적는다. 8번은 "비상 연락처 등록", `0x31` 번은 "콘솔 담당자 등록" … 이런 식이다.

이번에 나온 번호 (run 문서 표):

| 번호 | 이름 `[추측]` | 칸 |
|---|---|---|
| `0x8` (설정) | `ProcessExceptionPort` | `ExceptionPort*` |
| `0x24` (조회) | `ProcessCookie` | `Cookie` |
| `0x31` (설정·조회) | `ProcessConsoleHostProcess` | `OwnerProcessId` |
| `0x35` (설정) | `ProcessDynamicFunctionTableInformation` | `InvertedFunctionTable` |
| `0x51` (조회) | `ProcessWakeInformation` | `WakeChannel`·`WakeInfo` |
| `0x57` (설정) | `ProcessEnableReadWriteVmLogging` | `Flags2` 로깅 |
| `0x60` (설정) | `ProcessEnableLogging` | `Flags2`·`Flags3` 로깅 |

- **번호는 `[정적]`**, **이름은 `[추측]`** 이다. 번호는 코드의 점프 표·비교 상수라 확실하다. 그런데 ntoskrnl PDB 에 `PROCESSINFOCLASS` 열거형 (번호 ↔ 이름 표) 이 **없다** (결과서가 `llvm-pdbutil dump -types` 로 확인). 이름은 공개 문서 기억과 코드 동작이 맞는 것만 붙였다.
- 그래서 run46 이 `[추측]` 으로 적은 `ProcessCookie` 는 **이름만 여전히 추측**, 번호 `0x24` 와 동작은 이번에 `[정적]` 으로 굳었다.

### 2.3 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 주인 란을 콘솔 호스트 번호로 덮는 줄

run 문서 "옛 Q65 남은 것" 이 인용한 명령 한 줄 (`NtSetInformationProcess +0x1ecd`, 오케스트레이터·flow-writer 둘 다 `.asm` 확인):

```
MOV [RCX+0x2d8],RDX
```

- **누가**: 커널 `NtSetInformationProcess` (민원 창구)
- **어디서**: `+0x1ecd`. 정보 클래스 `0x31` 갈래다 (`+0x1a32` 에서 클래스 `0x31` 로 갈라지고, 길이가 8 이 아니면 거절).
- **누구를**: `RCX` = `GS:[0x188]→+0xb8` = **지금 도는 프로세스** (= 부탁한 가게 자신). 그 `+0x2d8` 칸 = `OwnerProcessId` (주인 란).
- **무엇을**: `RDX` = 가게가 넘긴 8바이트 값. 단, 들어오기 전에 검사가 둘 있다 `[정적]`:
  - 핸들이 `-1` (자기 자신) 이 아니면 거절.
  - 값의 **아래 2비트가 `01`** 이 아니면 거절 (`+0x1eb0 ~ +0x1eb7`).
  - 권한 검사는 없다.
- **무엇을 위해**: "이 가게의 콘솔 담당은 몇 번 프로세스" 라고 적어 두려고. 조회 쪽 `NtQueryInformationProcess` 클래스 `0x31` 이 이 칸을 그대로 돌려준다 → **`OwnerProcessId` 는 "콘솔 호스트 프로세스" 표시이기도 하다** `[정적]`.
- 이게 옛 Q65 의 답이다 (2.10).

#### 한 줄 읽기 ②: 첫 출근 때 도장 두 개를 한 번에 찍는 줄

`DbgkCreateThread +0x85` `[정적]` (run 문서 칸별 표)

- **누가**: `DbgkCreateThread`. 출근 도장 창구 (`PspUserThreadStartup`) 의 4번 일 `PspNotifyThreadCreation` 이 `+0x1c` 에서 부른다 (ret `+0x21`).
- **어디서**: 자기 몸 `+0x85`.
- **무엇을**: 장부 `Flags` 에 `0x400001` 을 OR 한다 (`lock cmpxchg` 반복). 비트 0 = `CreateReported`, 비트 22 = `ImageNotifyDone`. 둘을 한 번에 켠다.
- **무엇을 위해**: "이 가게 생겼다고 알렸음" · "간판 알림 보냈음" 표시. 그리고 **옛** `ImageNotifyDone` 이 0 이었으면 (= 처음이면) 이미지 알림을 뿌린다 (2.6).

#### 한 줄 읽기 ③: 다른 프로세스가 이 가게 장부에 비상 연락처를 적는 줄

`NtSetInformationProcess +0x1128` (클래스 8) `[정적]`

- **누가**: 커널 `NtSetInformationProcess`. 그런데 **부른 사람은 test.exe 가 아니라 csrss** 다: `csrsrv!CsrpCreateProcess +0x34d → CsrSetExceptionPort (+0x4f / +0x7b) → NtSetInformationProcess(자식 핸들, 클래스 8)`.
- **어디서**: `+0x1128`.
- **무엇을**: 자식 (test.exe) 장부 `+0x240` (`ExceptionPortData`·`ExceptionPortValue`) 에 "csrss 의 `CsrApiPort` 포트 객체 주소 `|` 상태 3비트" 를 `lock cmpxchg` 로 넣는다.
- **무엇을 위해**: 예외 포트를 달려고 (2.7). 부르는 사람에게 `SeTcbPrivilege` 권한이 있어야 한다.
- 이 줄이 이번 바퀴의 핵심 그림이다. **쓰는 손은 nt, 시키는 사람은 남**.

### 2.4 핵심: 쓰는 줄은 다 nt, 부르는 쪽은 셋으로 흩어진다

#### 사실 (모두 근거 있음)

- `[정적]` 뒤 구간 29칸의 쓰는 줄은 **모두 nt 안**이다. 29칸 모두 `.asm` 에서 확인했다.
- `[정적]` 그런데 그 줄까지 오는 길의 **맨 처음 (부르는 쪽)** 은 셋이다:

| 무리 | 칸 수 | 언제 | 대표 |
|---|---|---|---|
| ① **관리사무소 직원** (nt 직접) | 12 | `K2~K3`, `P0~P1` | 상가 명부 도장 (`ProcessInserted`), 첫 출근 도장 (`CreateReported`·`ImageNotifyDone`·`LaunchPrefetched`·토큰 얼림) |
| ② **가게 주인 자신** (test.exe 의 syscall) | 5 (+ `OwnerProcessId` 둘째 1) | `P1~P1a` (+ `P1a~P1b`) | 쿠키·장치 맵·ETW·동적 함수표·WNF, 콘솔 담당 |
| ③ **다른 사람** (다른 프로세스·드라이버) | 12 | `K3~P0`, `P1a~P1b` | csrss (예외 포트), `mssecflt.sys` (로깅 비트), bam (wake 채널·전력 정책), win32k (`Win32Process`) |

#### 왜 이렇게 가르나

- 짐 지도 3장의 "**쓴 주인**" 칸은 규칙이 이미 있다: 쓰는 줄이 nt 안이면 **nt 로 센다** (run47 의 `nt (부르는 쪽 다른 드라이버)` 와 같은 규칙). 그래서 29칸 모두 "nt 가 씀".
- 그런데 에뮬레이터 입장에선 "nt 가 씀" 하나로 묶으면 정보가 모자란다. **누가 그 창구에 왔는지**가 다르기 때문이다 (2.11).
- 그래서 이번부터 "**놓는 법**" 칸을 둘로 가른다 (짐 지도 "쓰는 말"):
  - `syscall (test.exe 가 부름)`
  - `syscall (다른 프로세스·드라이버가 부름)`
  - 부르는 쪽 이름은 쓴 주인 칸 괄호 (`nt (부르는 쪽 csrss)`) 와 경로 칸에 적는다.
- 예외 둘: `BamPpmPolicy`·`Win32Process` 는 시스템 콜이 아니라 **nt 공개 함수·함수표**로 들어온다. 그래서 놓는 법은 `직접 (nt 함수, 부르는 쪽 …)` 이다. 그래도 무리로는 ③ 이다 (부른 사람이 남이니까).

#### 비유로

- 가게 장부는 **관리사무소 금고 안**에 있다. 펜을 쥐는 건 언제나 관리사무소 직원이다.
- 다만 직원이 펜을 드는 **이유**가 셋이다.
  - ① 자기 업무 순서라서 (명부 올리기, 출근 도장).
  - ② 가게 주인이 창구에 와서 "열쇠 하나 주세요" 해서.
  - ③ 주민센터나 경비 업체가 와서 "이 가게 장부에 이거 적어 주세요" 해서.

### 2.5 무리 ① (1): 상가 명부에 올릴 때 · 첫 직원을 달 때 (`K2~K3`, 3칸)

부모 cmd 스레드가 아직 `NtCreateUserProcess` 안에 있을 때다. 모두 `[정적]`, run50 메모가 **전부 맞았다**.

- **`Flags.ProcessInserted`** (비트 26): `PspInsertProcess +0x202` (`lock or 0x4000000`).
  - `ObInsertObjectEx` (ret `+0x1e3`) 가 성공한 뒤다. "상가 명부에 올렸음" 도장.
  - 작업 (`Job`) 이 있으면 `PspValidateJobAffinityState` (ret `+0x263`) 가 같은 비트를 대신 켠다. test.exe 는 `Job` = 0 (sast/43) → `+0x202` 길.
- **`ActiveThreadsHighWatermark`**: `PspInsertThread +0x678`.
  - `+0x65a` 에서 `ActiveThreads` 를 1 올린 뒤, 최고 기록보다 크면 따라 올린다. "직원 수 최고 기록" 칸.
  - 스레드를 달 때마다 돈다 (뒤에 일꾼 스레드 때도).
- **`Flags4.ThreadWasActive`** (비트 0): `PspInsertThread +0x67f` (`bts`), 판정 `+0x696`.
  - 옛 값 0 + `ActiveThreads == 1` 이면 "첫 활성 스레드" 표시.
  - 이 칸은 짐작이었는데 **정확**이 됐다. run12 의 "오프셋 미상" 이 채워졌다.

### 2.6 무리 ① (2): 첫 직원 출근 도장 (`P0~P1`, 9칸)

test.exe 첫 스레드가 커널에서 출발해 `LdrInitializeThunk` (P1) 에 닿기 전이다. **시스템 콜이 아니다.** 커널이 스레드를 처음 돌리는 길이다.

run15 의 "출근 도장 다섯 칸" 중 1번·4번의 **본문**이 이번에 열렸다.

```
① test.exe 첫 스레드, PspUserThreadStartup 안 (P0~P1)
 ├─ (1) +0x3c PspDisablePrimaryTokenExchange
 │        +0x22 PrimaryTokenFrozen · +0xbb/+0x111 AddressPolicyFrozen · +0xc4 SecurityFreezeComplete
 └─ (4) +0xa1 PspNotifyThreadCreation
          ├─ +0x21 DbgkCreateThread   +0x85 Flags |= 0x400001 (CreateReported·ImageNotifyDone)
          └─ +0xc1 LaunchPrefetched (처음일 때만) → +0xdf PfProcessCreateNotification
                   ├─ +0x75 PfCalculateProcessHash +0x137  ImagePathHash   (프리페치 설정 조건)
                   └─ +0x43 PfSnBeginAppLaunch … PfSnAddProcessTrace +0x42  PrefetchTrace (시나리오 허락될 때)
```

#### (1) 신분증 바꿔치기 금지 도장 셋 `[정적]`

- run49 해설 2.15 에서 본 대로, 이 함수는 신분증 (토큰) 에 쓰는 게 아니라 장부에 "**이제 신분증 교체 금지**" 도장을 찍는다.
- 이번에 줄과 순서가 굳었다:
  - `+0x16`: 이미 켜져 있으면 건너뜀 → 아니면 `+0x22` `Flags2.PrimaryTokenFrozen` (비트 15).
  - `+0x36`: `Flags3.SecurityFreezeComplete` (비트 22) 가 이미 켜져 있으면 끝.
  - 아니면 `+0xbb` (또는 KVA 그림자가 켜져 있으면 `+0x10c KeSynchronizeAddressPolicy` 뒤 `+0x111`) `AddressPolicyFrozen` (비트 14) → `+0xc4` `SecurityFreezeComplete` (비트 22).
- 주의: `PrimaryTokenFrozen` 을 켜는 줄은 둘이다. 그런데 `PspInsertThread +0x77b` 는 **둘째 스레드를 달 때만** (`ActiveThreads` 가 올린 뒤 정확히 2) 탄다. test.exe 첫 스레드는 `+0x22` 하나만 탄다 (정정, 2.12).

#### (4) "새 직원 왔어요" 알림의 속 `[정적]`

- `DbgkCreateThread +0x85` 가 `CreateReported`·`ImageNotifyDone` 을 한 번에 켠다 (2.3 한 줄 읽기 ②).
- 옛 `ImageNotifyDone` 이 0 이면 **이미지 알림**을 뿌린다.
  - **이미지 알림** (새 말): 드라이버가 미리 등록해 둔 "exe·DLL 이 메모리에 올라왔다" 소식 받기 함수 (`PsCallImageNotifyRoutines` 안 간접 호출) 를 차례로 부르는 것. 순서는 exe, 이어서 시스템 DLL 들이다. 드라이버 콜백 본문은 이름만, 안 읽었다.
  - 비유: 새 가게가 **간판을 달면** 계약된 경비 업체들에게 "간판 달렸음" 문자가 간다.
- 디버그 포트가 있으면 디버거에 CreateProcess·모듈 메시지를 보내는 갈래도 있다. test.exe 는 없다.
- 이어서 `+0xc1` 이 `Flags.LaunchPrefetched` (비트 19) 를 **처음일 때만** 켜고, 바로 `PfProcessCreateNotification` 를 부른다.

#### 새 말: 프리페처

- **프리페처** `[추측]` (일반적으로 알려진 설명): 앱이 시작할 때 읽었던 파일 조각을 기억해 두었다가, 다음번엔 **미리 읽어 오는** Windows 기능. 코드 이름 `Pf…` 가 이것이다.
- 비유: 단골 가게 주문을 기억해 두고, 다음에 오면 **미리 준비해 두는** 점원.
- 이번에 본 칸 둘 `[정적]`:
  - `ImagePathHash` (`+0x384`): `PfCalculateProcessHash +0x137`. 전체 이미지 경로를 **대문자로** 바꿔 해시한다 (`×0x25`, 시작 `0x4cb2f`). 프리페치 설정이 켜져 있을 때만.
  - `PrefetchTrace` (`+0x390`): `PfSnAddProcessTrace +0x42`. 프리페치 흔적 객체 포인터. **시나리오가 허락될 때만**.
  - 둘 다 "직접 (조건부)" 다. 이 VM 에서 정말 켜지는지는 F11 (Q98).

#### 하나 더: `ModifiedPageCount` `[정적]` (줄) / `[추측]` (뜻)

- `MiInsertPageInList +0x1fc` (`lock inc`) · `MiInsertPagesInList +0x64a`. 지금 도는 스레드의 프로세스 카운터를 올린다.
- 페이지를 목록에 넣을 때마다 오르는 **런타임 카운터**라 "한 번 쓰고 끝" 이 아니다. 어느 목록일 때인지는 `[추측]` (Q99). 이 두 줄은 `.asm` 없이 결과서 `objdump` 로 본 것이다.

#### "첫 스레드만" 이라는 점

- `PspUserThreadStartup` 은 **스레드마다** 같은 길을 돈다.
- 그런데 `ImageNotifyDone`·`LaunchPrefetched`·`PrimaryTokenFrozen`·`SecurityFreezeComplete` 에 "**이미 켜졌으면 건너뜀**" 검사가 있다 `[정적]`.
- 그래서 칸을 처음 켜는 건 첫 스레드뿐이고, 일꾼 스레드 (`P1a~P1b`) 때는 건너뛴다. 사진에서 이 칸들이 `P0~P1` 에 처음 값을 갖는 것과 맞다.

### 2.7 무리 ③ (1): 부모 마무리 ~ 자식 출발 사이의 손님들 (`K3~P0`, 11칸)

결과서 답 `[정적]`: 11칸 모두 `NtSetInformationProcess`·`NtQueryInformationProcess` 갈래와 `PspSetProcessPpmPolicy` 가 쓴다. 커널 안에서 이 칸들을 **스스로** 쓰는 호출자는 없다. → **전부 바깥이 부른다.**

```
K3~P0 (부모 쪽 마무리 ~ 자식 첫 스레드 시작 사이, 다른 프로세스·드라이버)
 ├─ csrss  CsrpCreateProcess +0x34d → CsrSetExceptionPort → NtSetInformationProcess(자식, 클래스 8) +0x1128  ExceptionPort*   [정적]
 ├─ mssecflt.sys rva 0xbef6 → ZwSetInformationProcess(클래스 0x60) +0x19f/+0x203 Flags3 · +0x20f/+0x23d Flags2 로깅 비트   [정적: 호출 줄 / 추측: 이번에 불림]
 ├─ bam.sys rva 0x1621b → ZwQueryInformationProcess(클래스 0x51) +0xd0d → PspAllocateAndQueryProcessNotificationChannel +0x11d  WakeChannel·WakeInfo   [같음]
 └─ (함수표 PspBamHostInterface 경유) PspSetProcessPpmPolicy +0xa5  BamPpmPolicy   [추측: 부르는 쪽]
```

- 이 넷 사이의 **순서는 모른다** (짐 지도 5장).
- 부모 cmd 의 KERNELBASE **자신은** 이 구간에 자식 칸을 안 쓴다 `[정적]`. `CreateProcessInternalW` 에 `NtSetInformationProcess` 호출 줄 자체가 없다.

#### csrss: 예외 포트 (2칸) `[정적]`

- **예외 포트** (새 말): 커널이 "이 가게에서 처리 못 한 사고" 같은 소식을 보낼 **연락 창구 (LPC 포트)** 를 장부에 적어 두는 칸 `[추측]` (일반적으로 알려진 설명. 코드로 본 건 "포트 객체 주소를 적는다" 까지).
  - 비유: 주민센터가 전입 신고를 받으면서 "**문제 생기면 우리한테 연락**" 하는 번호를 가게 장부에 적어 두는 것 (run 문서 비유).
- 길: 부모 KERNELBASE `CreateProcessInternalW` → `CsrClientCallServer` → csrss `BaseSrvCreateProcess2` → … → `CsrpCreateProcess +0x34d` → `CsrSetExceptionPort` → 클래스 8 (2.3 한 줄 읽기 ③).
- 왜 `K3~P0` 인가: 부모 쪽 csrss 호출이 `NtResumeThread` 앞이라서다. 사진과 맞다.
- 덤: 클래스 8 은 8바이트를 통째 `cmpxchg` 한다. 그래서 같은 8바이트 안 `ExceptionPortState` (비트 0:3) 도 **같이 쓴다**. run50 은 이 칸을 "아무도 안 씀" 으로 셌었다 (정정, 2.12).

#### `mssecflt.sys`: 로깅 비트 (6칸) — 호출 줄 `[정적]` / 이번에 불렸나 `[추측]`

- **보안 드라이버 `mssecflt.sys`** (새 말): 이름으로 보아 MS 보안 쪽 필터 드라이버 `[추측]`. 코드로 본 것 `[정적]`: `PsSetCreateProcessNotifyRoutineEx`·`PsSetLoadImageNotifyRoutine` 를 import 한다 (= 프로세스 생성·이미지 알림을 받으려고 등록한다).
  - 비유: 상가와 계약한 **경비 업체** (run 문서 비유). 새 가게가 생기면 "이 가게는 이런 행동을 기록해 주세요" 하고 스위치를 켜 달라고 한다.
- 길: `mssecflt.sys` rva `0xbef6` 에서 `ZwSetInformationProcess(클래스 0x60, 4바이트)`.
  - 커널은 먼저 `Flags3` 비트 19·20·28·29·31 을 **지우고** (`+0x19f`), 입력 비트로 **다시 켠다** (`+0x203`). `Flags2` 비트 24·25 도 같은 식 (`+0x20f` 지움 / `+0x23d` 켬).
  - 앞에 `SeDebugPrivilege` 또는 `SeTcbPrivilege` 검사가 있다.
- 왜 "이번에 불림" 은 `[추측]` 인가 — 단계별로:
  1. 근거 `[정적]`: 드라이버 폴더 전체에서 `ZwSetInformationProcess` 를 import 하는 건 `mssecflt.sys`·`condrv.sys` (클래스 `0x1f`)·`ahcache.sys` 셋뿐이다. 클래스 `0x60` 을 넘기는 줄은 `mssecflt.sys` 에서 찾았다.
  2. 근거 `[정적]`: 이 드라이버는 프로세스 생성 알림에 등록한다.
  3. 그래서 → K3 (`PspCallProcessNotifyRoutines`, "새 가게 생겼어요" 알림) 안에서 도는 콜백이 부른 것으로 **보인다** `[추측]`.
  4. 아직 확인 아님: 정말 이 실행에서 `mssecflt.sys` 가 불렀나, 입력 값은 → F2 (Q98).
- 사진 `Enable…Logging 비트 5개` 와 모양이 맞다 `[추측: 값 대조는 Q79]`.
- 덤: 비트 31 (`EnableProcessImpersonationLogging`) 도 같은 줄이 지우고 다시 켠다. run50 은 이것도 "아무도 안 씀" 이었다 (정정).

#### bam: wake 채널 (2칸) + 전력 정책 (1칸) — 호출 줄 `[정적]` / 시점 `[추측]`

- BAM 자체는 run49 해설에서 푼 **절전 관리팀**이다 ([링크](run49-kprocess-token-htable-해설.md#새-말-ppm-정책과-bam)).
- **wake 채널** (새 말): 이름 그대로 "깨우기 알림 채널". 코드로 본 것 `[정적]`: `WakeChannel` 이 0 이면 **알림 채널 이름을 새로 만들어** `+0x720` 에 적고, 결과 48바이트를 돌려준다. 그 칸을 읽는 쪽 `PspChargeProcessWakeCounter` 는 ALPC 메시지를 보낼 때 읽기만 한다. 무엇을 깨우는 데 쓰는지는 근거에 없다.
  - 비유: 절전 관리팀이 가게에 **초인종 줄**을 하나 달아 달라고 한다. 관리사무소는 "줄 있나요?" 하고 **묻기만 해도**, 없으면 그 자리에서 달아 준다.
- 이게 **게으른 만들기**의 첫 예다 (2.8 에서 다시).
- 길: `bam.sys` rva `0x1621b` → `ZwQueryInformationProcess(클래스 0x51, 길이 0x30)` → `+0xd0d` → `PspAllocateAndQueryProcessNotificationChannel +0x11d`. 길이 `0x30` = `WakeInfo` 크기와 같다.
- **`BamPpmPolicy`** (`+0x820`): `PspSetProcessPpmPolicy +0xa5`. 기능 플래그 둘 (`Feature_UserPresenceThrottling`·`Feature_DisableUserPresenceQosByPolicy`) 이 켜졌을 때만 쓴다.
  - 이 함수를 직접 부르는 커널 줄은 없다 → **함수표** `PspBamHostInterface` 경유 `[정적 부재 + 추측]` (run49 와 같음).
  - **함수표** (새 말): 한쪽이 "내 함수들 주소" 를 표로 묶어 다른 쪽에 넘겨, 그쪽이 그 표를 보고 부르게 하는 것. 그래서 코드에 "직접 부르는 줄" 이 안 남는다.
    - 비유: 관리사무소가 절전 관리팀에 건넨 **내선 번호표**. 팀은 번호표를 보고 전화한다. 관리사무소 쪽 통화 기록엔 "누가 걸었나" 가 안 남는다.

### 2.8 무리 ②: 가게 주인이 처음 필요할 때 받는 것 (`P1~P1a`, 5칸) — 게으른 만들기

test.exe 첫 스레드가 ntdll 로더 (`LdrpInitializeProcess`) 앞쪽을 돌 때다. 모두 `[정적]`.

#### 새 말: 게으른 만들기

- 칸을 미리 만들어 두지 않고, **처음 필요할 때** 만든다. 커널 코드는 거의 같은 모양이다: "비어 있나? (0 인가?) → 비었으면 만들어서 `lock cmpxchg` 로 한 번만 넣는다 → 경쟁에서 지면 만든 걸 버린다."
- 비유: 가게 주인이 처음 "**열쇠 하나 주세요**" 할 때, 관리사무소가 그 자리에서 열쇠를 깎아 준다 (run 문서 비유).
- 다섯 칸 모두 **자기 프로세스** (핸들 `-1` 또는 현재 프로세스) 에 대해서만, **처음 필요할 때** 만든다.

```
② test.exe 첫 스레드, ntdll 이 부른 syscall 안 (P1~P1a)
 LdrpInitializeProcess
 ├─ +0x9f2 RtlInitializeHeapManager → +0xa2 RtlpGetModifiedProcessCookie → +0x27 NtQueryInformationProcess(0x24)
 │        → nt +0x2b42 Cookie (0 이면 한 번만)
 ├─ +0xbe7 LdrpEnableUMGLTracingStateSync → … → RtlpWnfRegisterTpNotification +0x7c NtSetWnfProcessNotificationEvent
 │        → nt +0xad ExpWnfCreateProcessContext +0x104  WnfContext
 ├─ +0xc0b / +0xc28 EtwEventRegister → … → EtwpRegisterTpNotificationOnce +0x91 NtTraceControl(0x1b)
 │        → nt EtwpAddNotificationEvent +0x59 EtwpAddDataSource +0x49  EtwDataSource
 ├─ +0x11a2 LdrpInitializeNtdllDataTableEntry → … → RtlAddGrowableFunctionTable → NtSetInformationProcess(0x35)
 │        → nt RtlInsertDynamicUserFunctionTable +0x20c  InvertedFunctionTable   (ntdll 안 CFG 갈래에 따라 [공백])
 └─ ?  `\??\` 이름 찾는 첫 syscall → nt … ObpSetCurrentProcessDeviceMap +0xf8  DeviceMap   [공백: ntdll 호출자]
```

칸마다 한 줄씩:

- **`Cookie`** (`+0x2b8`): 클래스 `0x24` 조회. 읽어서 0 이면 시간·성능 카운터·난수 등을 섞어 **한 번만** 넣는다 (`+0x2b42`). [run46 해설 2.12](run46-process-heap-해설.md#212-시스템-콜-12개--한-표로) 의 쿠키 시스템 콜이 바로 이 줄을 탄다.
  - 덤: 복제 길에선 `PspAllocateProcess +0x2183` 이 부모 값을 복사한다. test.exe 는 안 탄다 (정정 보탬).
- **`DeviceMap`** (`+0x318`): 이름이 **`\??\`** 로 시작하는 이름 찾기가 처음 올 때, 비어 있으면 토큰의 장치 맵 (또는 사일로 기본값) 으로 그 자리에서 채운다 (`ObpSetCurrentProcessDeviceMap +0xf8`).
  - 만들기 길 (`ObInitProcess`) 에선 안 채운다 → 사진 `K1~P1` 동안 비어 있는 것과 맞다.
  - 어느 시스템 콜이 첫 번째인지, ntdll 쪽 호출자는 누군지는 `[공백]` (F5, Q99).
- **`EtwDataSource`** (`+0x320`): `NtTraceControl` 함수 코드 `0x1b` → `EtwpAddDataSource +0x49`. 풀 `0x28` 바이트 "알림 받는 자리" 를 만든다. ntdll 쪽 함수 이름 끝의 `Once` = 처음 한 번.
- **`InvertedFunctionTable`** (`+0x5c8`): 클래스 `0x35` → `RtlInsertDynamicUserFunctionTable +0x20c`. 풀 `0x10 + 24×N` 바이트 표를 처음 한 번 만든다.
  - 이것도 "함수표" 라고 부르지만 2.7 의 내선 번호표와는 **다른 것**이다. 이쪽은 **동적 함수표**: 실행 중에 등록한 코드 구역의 목록이다. 무엇에 쓰는지는 근거에 커널 함수 이름 (`…DynamicUserFunctionTable`) 과 읽는 쪽 `RtlpLookupDynamicUserFunctionTable` 정도만 있다. 일반적으로는 예외 처리 때 "이 주소는 어느 함수인가" 를 찾는 데 쓴다고 알려져 있다 `[추측]`.
  - PDB 칸 이름은 `InvertedFunctionTable`, 커널 함수 이름은 `…DynamicUserFunctionTable` 이다. 이름이 달라 헷갈리지 말 것.
  - ntdll 안 CFG 검사 두 갈래에 따라 정말 시스템 콜까지 가는지는 `[공백]` (test.exe `MitigationFlags` 에 CFG 없음) → F7 (Q79).
- **`WnfContext`** (`+0x5e8`): `NtSetWnfProcessNotificationEvent` → `ExpWnfCreateProcessContext +0x104`. 풀 `0x88` 바이트 문맥을 만들고 전역 목록에 단다. ntdll 쪽 길이 `RunOnce` 안이라 처음 한 번.

정리:

- 다섯 칸 모두 "**syscall 안 · 자기 프로세스 · 처음 필요할 때**".
- 이 무리는 test.exe 자신이 부탁한 것이라 에뮬레이터에서도 **같은 창구 (nt 엔진의 시스템 콜)** 로 들어온다 (2.11).

### 2.9 `P1a~P1b`: 콘솔 담당 등록, 그리고 `Win32Process`

KERNELBASE 시작 함수가 도는 구간이다.

#### 새 말: 콘솔 호스트 (conhost)

- **콘솔 호스트** `[추측]` (일반적으로 알려진 설명): 콘솔 프로그램 (test.exe 같은 까만 창 프로그램) 의 **창을 실제로 그리고 키 입력을 받아 주는** 별도 프로세스, `conhost.exe`.
- 비유: 가게 대신 **쇼윈도와 출입문을 관리해 주는 옆집**. 가게는 "우리 쇼윈도는 저 집이 맡아요" 라고 장부에 적어 둔다.

#### 같은 함수, 같은 호출에서 두 칸 `[정적]`

```
(kernel32 준비 안) KERNELBASE 시작 함수 _KernelBaseBaseDllInitialize +0x526 ConsoleInitialize → ConsoleCommitState
   +0x40 매개변수 ConsoleHandle (4 → 0x60)
   +0x91 NtDeviceIoControlFile(0x500023) → +0xd8 값 | 1 → +0xf0 NtSetInformationProcess(-1, 0x31) → nt +0x1ecd  OwnerProcessId (0x19a8 → 0x729)
```

- `KERNELBASE!ConsoleCommitState` 가:
  1. `+0x3c ~ +0x40` 에서 개업 서류 (매개변수) 의 `ConsoleHandle` 을 덮는다. `[관찰]` run44 4 → `0x60`.
  2. `+0x8a` 에서 `NtDeviceIoControlFile(…, 0x500023, …)` 로 8바이트를 받는다.
  3. `+0xd8` 에서 그 값에 `| 1` 을 붙인다.
  4. `+0xe9` 에서 `NtSetInformationProcess(-1, 0x31, &값, 8)` → 커널 `+0x1ecd` 가 주인 란을 덮는다 (2.3 한 줄 읽기 ①). `[관찰]` run44 `0x19a8` → `0x729`.
- 그래서 사진에서 두 칸이 **같은 구간에 같이** 바뀐 것이다.
- **`0x729` 의 뜻** `[추측]`: `0x729 = 0x728 | 1`. 그러면 IOCTL 이 돌려준 `0x728` 이 콘솔 호스트 (conhost.exe) 의 PID 일 것이다. 정말 그런지는 프로세스 목록으로 봐야 한다 → F10 (Q79).
- 짐 지도에서:
  - `OwnerProcessId` 둘째 쓰기 = 무리 ② (test.exe 가 부른 syscall, 쓴 주인 nt).
  - `ConsoleHandle` 둘째 쓰기 = **유저 칸이고 쓰는 줄이 KERNELBASE 안**이라 "**다른 DLL**" 에 센다 (3.2 새 줄).
  - 이 둘은 원래 Q78f 몫이었는데 여기서 닫혀서 Q78f 에서 다시 안 본다 (plan).

#### `Win32Process` (`+0x298`): 쓰는 줄 `[정적]` / 트리거 `[공백]`

- 쓰는 줄: `PsSetProcessWin32Process +0x86`. 새 값 ≠ 0, 현재 값 = 0, `Flags.ProcessDelete` 꺼짐일 때.
- 이 함수를 부르는 **커널 줄은 없다** (공개 함수). 부르는 곳은 win32k 쪽 (`win32kbase!AllocateW32ProcessImpl +0xe0` 이 켜는 쪽, `.asm` 없음, 공개 PDB 이름).
- 커널이 win32k 를 부르는 길은 `PsConvertToGuiThread +0x1e0 / +0x238` (콜아웃) 이다. 즉 프로세스가 **처음 win32k 시스템 콜을 부를 때** 변환 길에서 콜아웃이 도는 것으로 읽힌다 `[추측]`.
- 그런데 test.exe 는 user32 를 안 올린다 (모듈 4개). 그래서 **누가 첫 win32k 호출을 일으키는지는 정적으로 못 가른다** `[공백]` → F9 (Q98).
- 비유: 창 그리기 담당 부서 (win32k) 가 자기 장부를 따로 만들고, 그 **장부 번호**를 가게 장부에 적어 달라고 한다. 언제 그 부서가 처음 왔는지가 아직 안 보인다.

### 2.10 옛 Q65 가 닫혔다, 그리고 "주인 란" 의 뜻이 바로잡혔다

#### Q65 가 무엇이었나

- run33 이 `PspProcessClose` (가게 장부 반납 창구) 를 보다가 남긴 작은 정적 질문 셋이었다.
  1. `PspAllocateProcess +0x707` bit 9 의 뜻 → run34 에서 이미 닫힘 (KERNELBASE 가 `CREATE_SUSPENDED` 없을 때 켜는 ProcessFlags bit 9).
  2. `OwnerProcessId` 를 쓰는 다른 줄 `NtSetInformationProcess +0x1ecd` 는 어느 정보 클래스인가.
  3. `ObCloseHandleTableEntry +0x7a9` 간접 호출 앞에서 인자를 어떻게 채우나 (`PspProcessClose` 인자 이름).

#### 이번 답 `[정적]`

- (2) **`+0x1ecd` = `OwnerProcessId` 둘째 쓰기, 클래스 `0x31`.** 부르는 쪽 `KERNELBASE!ConsoleCommitState`. (2.3 ①, 2.9)
- (3) `+0x7a9` 은 개체 형식의 `CloseProcedure` 를 부르는 간접 호출 한 곳이다. 인자: `RCX` 핸들 표 주인 프로세스 · `RDX` 개체 · `R8D` 접근 권한 · `R9` 핸들 개수. 다섯째 인자는 이 줄 앞에서 안 채운다.
  - `PspProcessClose` 인자 이름 `(Process, Object, GrantedAccess, ProcessHandleCount, SystemHandleCount)` 은 `[추측]` (공개 `OB_CLOSE_METHOD` 모양과 일치).
  - **짐에 영향 없음.** 그래서 짧게만.

#### 정정: 주인 란은 "부모 PID" 가 아니다

- 전 (run33·run34): `OwnerProcessId` = 부모 PID.
- 후 `[정적]`: 만들 때 값은 **`NtCreateUserProcess` 를 부른 프로세스의 PID** 다. `PspAllocateProcess +0x6f1 ~ +0x710` 이 읽는 건 `GS:[0x188] → +0xb8` = **지금 도는 프로세스** 의 `+0x1d0` 이다.
  - cmd 가 부모이자 부른 쪽이라 test.exe 에선 값이 같다 (`0x19a8`).
  - `PROC_THREAD_ATTRIBUTE_PARENT_PROCESS` 로 부모를 바꿔도 **부른 쪽**이 들어간다 (결과서).
  - run50 해설 2.8 의 "세션은 부모가 아니라 서류 쓰는 사람의 것" 과 같은 모양이다.
- 그리고 이 칸은 뒤에 **콘솔 호스트 PID `|1`** 로 덮인다.
- 고친 곳: 짐 지도 3.11 `+0x2d8` 줄, flow.md 2.1 `PspAllocateProcess` 절, `run33-first-resume.md`·`run34-parent-leftovers.md` 맨 위 한 줄씩 (그 문서들 본문의 "부모 PID" 는 그대로 둠).

#### 같은 칸의 두 태그 `[정적]`

- `PspAllocateProcess +0x710` 의 `|2` = "부른 쪽 PID" 태그 (bit 9 가 켜져 있으면 안 붙음 → test.exe 의 `0x19a8` 은 `|2` 없음).
- `+0x1ecd` 의 `|1` = "콘솔 호스트" 태그.
- PID 는 4의 배수라 아래 2비트가 늘 0 이다. 그래서 태그가 붙으면 어떤 PID 와도 **절대 같지 않다**.
- 그래서 `PspProcessClose +0x22 ~ +0x30` 의 "닫는 쪽 PID 와 주인 란이 같은가" 비교가 안 걸린다.
- 비유: 주인 란에 번호 뒤에 **작은 도장**을 찍어 둔다. 도장이 있으면 "이건 진짜 주인 번호가 아님" 이라 반납 창구 검사가 그냥 지나간다.

### 2.11 핵심: 셋째 무리는 에뮬레이터에서 누가 채우나 (내 짐작)

여기는 run 문서·짐 지도의 사실 위에 내가 짐작을 덧붙이는 곳이다. 짐작은 모두 `[추측]` 이고 이유를 같이 쓴다. **어디까지 구현할지는 목록이 다 된 뒤 사용자와 정한다** (CLAUDE.md). 그 전에 고르는 게 아니라 "어떤 선택지가 있나" 만 적는다.

#### 먼저 사실만

- `[정적]` 29칸의 쓰는 줄은 모두 nt 안이다. 쓴 주인 = nt.
- `[정적]` 그중 12칸은 **다른 프로세스·드라이버**가 불러서 그 줄을 탄다 (csrss 2 `[정적]`, `mssecflt.sys`·bam·win32k 는 부르는 쪽 `[추측]`).
- 짐 지도 3.11 머리에 이미 이렇게 적혀 있다: "에뮬레이터 쪽에서 보면 앞 둘 (nt 자기 길, test.exe 의 syscall) 은 **nt 엔진이 할 일**, 뒤 둘 (csrss, 드라이버) 은 **바깥에서 들어온 값 (입력에 가까움)** 이다 `[추측]`."

#### 그래서 무리마다

- **무리 ① (nt 직접 12칸)**: nt 엔진이 자기 길 (명부 올리기, 출근 도장) 을 흉내 낼 때 같이 놓으면 된다 `[추측]`. 시키는 사람이 엔진 자신이다.
- **무리 ② (test.exe 의 syscall 5 + 1칸)**: test.exe 의 ntdll·KERNELBASE 가 에뮬레이터 안에서 그대로 돌며 **같은 시스템 콜을 부른다**. 그러니 nt 엔진의 그 시스템 콜 처리가 칸을 놓으면 된다 `[추측]`. 시키는 사람이 에뮬레이터 안에 있다.
  - 하나 조심할 것 `[추측]`: `OwnerProcessId` 의 **값** (`0x728`) 은 test.exe 가 지어낸 게 아니라 `NtDeviceIoControlFile(0x500023)` 의 **대답**에서 온다. 그러면 이 칸은 "그 IOCTL 에 누가 무엇을 대답하나" 에 달린다. `ConsoleHandle` `0x60` 도 같은 IOCTL/연결 결과다 (결과서).
- **무리 ③ (다른 프로세스·드라이버 12칸)**: 에뮬레이터엔 csrss·`mssecflt.sys`·bam·win32k 가 **없다**. 부탁하러 올 사람이 없으니, 그대로 두면 이 칸들은 아무도 안 채운다 `[추측]`.
  - 그래서 누군가 **대신** 정해야 한다. 생각할 수 있는 선택지 셋 `[추측]`:
    1. **nt 엔진이 그 순간을 흉내** 낸다. 예: K3 알림 자리 (`PspCallProcessNotifyRoutines`) 나 csrss 신고 자리에서, 바깥이 했을 일을 엔진이 대신 한다.
    2. **처음부터 값으로 넣는다** (사진 값을 입력으로). 짐 지도의 "입력에 가까움" 이 이 쪽이다.
    3. **비워 둔다** (0). test.exe 가 그 칸을 안 읽으면 문제가 없을 수 있다.
  - 칸마다 무엇이 맞을지는 "**누가 그 칸을 읽나**" 에 달려 있다. 근거에 있는 읽는 쪽은 이 정도다:
    - `ExceptionPortData`: 옛 값이 있으면 옛 포트에 알림 (`LpcRequestPort`). 값 자체가 csrss 의 커널 포트 객체 주소라, 커널 공간 (에뮬레이터 바깥) 안의 이야기다.
    - `WakeChannel`: `PspChargeProcessWakeCounter` 가 ALPC 메시지 보낼 때 읽기만. 그리고 **게으른 만들기**라, 아무도 안 물으면 원래 안 생기는 칸이다.
    - `Win32Process`: 트리거가 `[공백]` 이다. 언제 생기는지부터 F9 로 봐야 한다.
    - 로깅 비트·`BamPpmPolicy`: 읽는 쪽은 이번 근거에 없다.
  - 이 판단에 필요한 동적 확인이 F1·F2·F3·F9 다 (4절).

#### 비유로

- 진짜 상가엔 주민센터·경비 업체·절전 관리팀·창 그리기 부서가 있어서, 새 가게가 생기면 **알아서 와서** 장부에 한 줄씩 적어 달라고 한다.
- 에뮬레이터는 **가게 하나와 관리사무소만 있는 섬**이다. 손님이 안 온다.
- 그러니 관리사무소가 "주민센터가 왔다 치고" 직접 적을지, 처음부터 적힌 장부를 들고 갈지, 그 칸은 비워 둘지를 정해야 한다.

### 2.12 정정 (run 문서 "정정")

- **`OwnerProcessId` = 부른 프로세스 PID** (부모 PID 아님). → 2.10.
- **`PspInsertThread +0x77b` (`PrimaryTokenFrozen`) 는 둘째 스레드용** (`ActiveThreads` 가 올린 뒤 정확히 2). run50 이 "켜는 줄이 둘" 로 적은 것 중 test.exe 첫 스레드가 타는 건 `PspDisablePrimaryTokenExchange +0x22` 하나.
- **`PspDisablePrimaryTokenExchange +0xa4` → `+0xc4`**: `+0xa4` 는 `.asm` 의 **주소 끝자리** (`140910fa4` 의 `…fa4`) 를 오프셋으로 잘못 읽은 오기다. `SecurityFreezeComplete` 를 켜는 줄은 `+0xc4` 다 (`+0xa4` 라는 명령 시작은 없음). run49 와 결과서 1번 표의 `+0xa4` 를 고쳤다.
- **run50 "아무도 안 씀" 28 중 둘은 쓰는 줄이 있다**: `ExceptionPortValue.ExceptionPortState` (클래스 8 의 8바이트 `cmpxchg` 가 같이 씀) · `Flags3.EnableProcessImpersonationLogging` (클래스 `0x60` 이 지웠다 다시 켬).
  - run50 의 커널 전체 오프셋 훑기가 큰 함수 `NtSetInformationProcess` 안 갈래를 놓친 것이다. run50 해설 2.5 의 "오탐·빠짐" 경고가 실제로 걸린 예다.
  - 사진은 끝까지 0 이라 "0 을 씀 / 안 켬" 쪽이다. 짐 지도 안 씀 79 → 77.
- **`ThreadWasActive` 짐작 → 정확** (`PspInsertThread +0x67f`). → 2.5.
- **`Cookie` 복제 길** (`PspAllocateProcess +0x2183`, 부모 값 복사, test.exe 안 탐) 이 run50 "조건부 안 탐" 목록에 빠져 있었다. → 2.8.

### 2.13 남은 1칸: `SharedCommitCharge` (`K1 이전`) — 후보만 좁힘

- 쓰는 줄은 `MiInsertSharedCommitNode +0x33e` (`lock add`) 하나다 (sast/44).
- 그 함수를 부르는 곳을 커널 전체에서 다시 훑었다 (`objdump`): 후보에 **`MiMapViewOfImageSection +0x4e9`** (이미지 섹션 보기를 붙이는 함수) 가 새로 보였다.
- 왜 여기가 유력한가 — 단계별로:
  1. 근거 `[정적]`: 새 프로세스는 `MiMapProcessExecutable` 에서 exe 와 ntdll 을 붙인다 (K1 앞).
  2. 근거 `[관찰]`: `SharedCommitCharge` 는 `K1 이전` 에 값 (`0x2b`) 을 갖는다.
  3. 그래서 → K1 `0x2b` 는 여기서 올랐을 가능성이 가장 높다 `[추측]`.
  4. 아직 확인 아님: 어느 줄인지, `0x2b` 가 exe 몫인지 ntdll 몫인지는 인자 값이라 정적으로 못 가른다 → F12 (Q97).

### 2.14 정리: 비유로 한 번 더

1. 관리사무소가 새 가게 장부를 다 만들었다 (run49·run50).
2. **상가 명부**에 올리고 "올렸음" 도장을 찍는다. 첫 직원을 달며 "직원 수 최고 기록" 과 "첫 직원 있음" 을 적는다 (`K2~K3`).
3. 그 사이 **주민센터** (csrss) 가 와서 "문제 생기면 우리한테" 연락처를 적고 간다. **경비 업체** (`mssecflt.sys`) 는 기록 스위치를 켜 달라고, **절전 관리팀** (bam) 은 초인종 줄과 전기 절약 등급을 적어 달라고 한다 (`K3~P0`, 뒤 셋은 짐작).
4. 첫 직원이 **출근 도장**을 찍는다. "신분증 교체 금지" 도장 셋, "생겼다고 알림·간판 알림" 도장, "단골 미리 준비" 표시 (`P0~P1`). 둘째 직원부턴 이미 찍혀 있어 건너뛴다.
5. 가게 주인이 일을 시작하며 창구에 와서 하나씩 받는다: 쿠키 번호, 장치 지도, ETW 알림 자리, 동적 함수표, WNF 문맥. **처음 달라고 할 때** 그 자리에서 만들어 준다 (`P1~P1a`).
6. 주인이 "쇼윈도는 옆집 (conhost) 이 맡아요" 하고 등록한다. 주인 란이 콘솔 담당 번호 `|1` 로 바뀌고, 개업 서류의 콘솔 손잡이도 같이 바뀐다 (`P1a~P1b`). 창 그리기 부서 장부 번호도 이 무렵 적힌다 (누가 불렀는지는 아직 모름).
7. 펜은 늘 관리사무소가 쥔다. 다만 3번의 손님들은 에뮬레이터엔 **안 온다**.

### 2.15 곁가지: 도구 메모

- 결과서의 도구 제안 (하네스라 오케스트레이터가 사용자에게 물을 것):
  - **제안 1** (sast/43·44 에 이어 세 번째): 오프셋 쓰는 줄 훑기 (`field_writers.py`) + "누가 이 함수를 부르나" (`callers.py`). `_calls.tsv` 는 뽑은 함수의 호출만 있어서 거꾸로 찾기가 안 된다. 이번 "아무도 안 씀" 정정 둘도 이 훑기의 빠짐이다.
  - **제안 2**: `pdb_struct.py` 에 열거형 지원. `PROCESSINFOCLASS` 가 PDB 에 없음을 직접 확인해야 했다.
  - **제안 3**: `imports_callers.py` (IAT 호출 자리 + 앞쪽 `edx` 정보 클래스 같이).
- flow-writer 가 본 함정: 결과서 오프셋을 `.asm` 의 **주소 끝자리**와 섞어 읽는 일 (`+0xa4` vs 주소 `…fa4` = `+0xc4`). `.asm` 의 `+0x` 열로만 적자.
- 디버거가 붙어서 달라지는 점: 없음 (정적 + 이미 찍은 사진). `DbgkCreateThread` 의 디버그 포트 갈래는 프로세스 디버그 포트 이야기라 커널 디버거 (KDNET) 와는 상관없다 `[추측]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

그림 (mermaid) 은 안 바뀌었다. 새로 나온 호출은 **이미 그림에 있는 상자 안쪽의 칸 쓰기 자리**라 새 단계·새 caller 가 아니다. csrss·드라이버 쪽은 그림 1·2 밖이다.

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | PDB 목록에 run50 까지 | ntkrnlmp·kernelbase·ntdll·csrsrv 줄에 **run51** 더함, 동적 메모 한 줄 (사진 구간과 코드 단계 대조, 새 실행 없음) |
| 2.1 `PspAllocateProcess` | `OwnerProcessId` = "부모 PID" `[정적]` | **호출한 프로세스의 PID** 로 정정. 뒤에 콘솔 호스트 `\|1` 로 덮인다는 줄 `[정적]` |
| 2.1 `PspInsertProcess` | (칸 쓰기 없음) | `+0x202` `Flags.ProcessInserted` `[정적]` |
| 2.1 `PspInsertThread` | `ThreadWasActive` 오프셋 미상 | `+0x65a`·`+0x678`·`+0x67f`·`+0x696` 채움, `+0x77b` 는 둘째 스레드만 `[정적]` |
| 2.2 csrss 절 | `BaseSrvCreateProcess2` 까지 | **`CsrSetExceptionPort` → 클래스 8 `+0x1128`** (csrss 가 자식 커널 칸을 쓰는 자리) `[정적]`. 로깅 비트·`WakeChannel` 은 드라이버 쪽 줄 `[정적: objdump]`, 이번 실행에서 불렸나 `[추측]` |
| 2.3 트리 · `PspUserThreadStartup` 절 | (1)·(4) 본문 이름 짐작 (run15), (1) 은 run49 일부 | (1) 얼림 비트 줄, (4) `DbgkCreateThread +0x85`·`+0xc1`·`PfProcessCreateNotification` `[정적]`. `+0xa4` → `+0xc4` 정정 |
| 2.4 새 절 | 없음 | "**ntdll 이 부르는 syscall 이 커널 프로세스 객체 칸을 처음 만드는 자리**" (쿠키·WNF·ETW·동적 함수표·장치 맵) `[정적]`, 열쇠 비유 |
| 2.5 KERNELBASE 시작 함수 | `OwnerProcessId`·`ConsoleHandle` 쓰는 쪽 = "KERNELBASE 콘솔 준비 → `NtSetInformationProcess`" `[추측]` | **`ConsoleCommitState`** `+0x40` / `+0xe9` → 클래스 `0x31` `+0x1ecd` `[정적]` (`0x728` = conhost 는 `[추측]`) |
| 4장 공백 | 2.2 Q65 남은 둘, 2.3 (4) 본문, 2.5 (짐 지도) 쓰는 쪽, 2.1 (짐 지도) 뒤 구간 29칸 | 모두 **(해결됨)** 또는 **(일부 해결)**. 새 줄 둘: 실행 확인 (F1 ~ F13 → Q79·Q97·Q98), 작은 정적 (→ Q99) |
| 5장 | | 그림 2 nt 줄에 (1)·(4) 본문 칸 쓰기 메모, 근거 run 표에 run51 두 줄 |

### 짐 지도 `baggage.md` 에서 바뀐 것 (아홉째판)

| | nt 가 씀 | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|
| `_EPROCESS` (263칸) | 152 → **183** | 28 → **26** | 53 | 30 → **1** | 146 + 6 → **178 + 5** |

- 새 아는 칸 31 = 뒤 구간 29 + "안 씀" 에서 옮긴 2. 모두 정확 (쓰는 줄 `[정적]`).
- 옛 짐작 `ThreadWasActive` 가 정확으로 → 짐작 6 → 5 (`ThreadListHead`·`Job`·`JobLinks`·`CommitChargeJob`·`Flags.ProcessDelete`).
- 매개변수 (`_RTL_USER_PROCESS_PARAMETERS`): 다른 DLL 0 → **1** (`ConsoleHandle`, KERNELBASE).
- 짐 지도 전체 (구조체 21개, 직접 칸 1,456): 아는 칸 476 → **507** (정확 417 → **449** · 짐작 59 → **58**) + 아무도 안 씀 79 → **77** + 범위 밖 길에서만 씀 **53** + 모름 848 → **819**.
- "쓰는 말" 에 놓는 법 둘이 새로 생겼다: `syscall (test.exe 가 부름)` / `syscall (다른 프로세스·드라이버가 부름)`, 그리고 `(게으른)` 표시.
- 7.6 (칸 표 없는 짐) 에 새 후보만 기록: `_WNF_PROCESS_CONTEXT` (0x88B), ETW 데이터 소스 (0x28B), 동적 함수표 (`0x10 + 24×N`), `_PS_PROCESS_WAKE_INFORMATION` (0x30B), `ExceptionPort` 가 가리키는 csrss `CsrApiPort`.

### 큰 그림에서 어떻게 이어지나

- **`_EPROCESS` 는 1칸 빼고 완성이다.** Q78c (프로세스 객체) 가 Q78c1 (`_KPROCESS`·`_TOKEN`·`_HANDLE_TABLE`, run49) → Q78c2a (만들 때, run50) → Q78c2b (이번) 로 끝났다.
- 다음은 **Q78d 첫 스레드** (`_ETHREAD`·`_KTHREAD`·`_KTRAP_FRAME`·`_TEB`, 모르는 칸 약 564). plan 은 그 조각의 뿌리를 `PspUserThreadStartup` 으로 적었다. 이번에 본 출근 도장 창구가 거기서 다시 나온다.
- 이번에 처음 생긴 "**부르는 쪽 셋**" 구분은 앞으로 다른 구조체에도 쓴다. 특히 셋째 무리 (바깥 손님) 목록은, 나중에 사용자와 "어디까지 구현할지" 정할 때 따로 봐야 할 묶음이 된다 `[추측]`.
- Q78f (KERNELBASE·KERNEL32 구간) 는 `OwnerProcessId`·`ConsoleHandle` 을 다시 안 봐도 된다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **`SharedCommitCharge`** K1 `0x2b` 를 올린 줄 (exe 몫인가 ntdll 몫인가) | 줄 후보 (`MiMapViewOfImageSection +0x4e9`) 까지만. 어느 쪽인지는 인자 값 | **F12** → **Q97** (동적, 낮음. run50 E5 와 같은 질문) |
| **F1** `ExceptionPortData` 값이 정말 csrss `CsrApiPort` 객체인가 | 값은 실행 때 주소 | **Q79** (덤프: P0 `+0x240` & `!object`) |
| **F7** `InvertedFunctionTable` 이 정말 클래스 `0x35` 길로 왔나 | ntdll 안 CFG 갈래 두 개 `[공백]` | **Q79** (덤프: 값의 풀 내용) |
| **F10** `0x729` = conhost PID `0x728` `\| 1` 인가 | IOCTL 대답 값은 실행 때 | **Q79** (덤프: 프로세스 목록) |
| **F2** 로깅 비트를 쓴 주체가 `mssecflt.sys` 인가, 입력 값 | 호출 줄은 있지만 "이번 실행에서 불렸나" 는 동적 | **Q98** (실행 중 멈춤) |
| **F3** `WakeChannel` 을 만든 주체가 `bam.sys` 인가 | 같음 | **Q98** |
| **F9** `Win32Process` 를 켠 주체와 트리거 | test.exe 는 user32 를 안 올림. 첫 win32k 호출을 누가 일으키나 정적으로 못 가름 `[공백]` | **Q98** |
| F4 쿠키 · F5 `DeviceMap` 첫 syscall · F6 ETW · F8 WNF · F11 프리페처가 이 VM 에서 켜지나 · F13 `ModifiedPageCount` 첫 줄 | 줄은 다 있다. 확인용 (F5 는 ntdll 호출자 `[공백]`, F11 은 설정·시나리오 조건) | **Q98** (낮음) |
| 두 락이 초기화 길에서 잡혔다 놓이나: `InvertedFunctionTableLock` (`+0x5d0`) · `ThreadListLock` (`+0x5e0`) | 결과서에 잡는 줄이 없어서 run50 "범위 밖 길에서만 씀" 에서 안 옮김 | **Q99** (작은 정적. 맞으면 "nt 가 씀" 으로) |
| `DeviceMap` 을 처음 채우는 ntdll 쪽 호출자 · `ModifiedPageCount` 가 오르는 페이지 목록 | 후보가 너무 많음 / 인자 뜻 `[추측]` | **Q99** (정적) + F5·F13 |
| 정보 클래스 **이름** (`ProcessCookie` 등) | ntoskrnl PDB 에 `PROCESSINFOCLASS` 열거형이 없음 | 대기열 없음 (번호·동작은 `[정적]` 확정이라 짐 지도엔 충분) |
| 셋째 무리 12칸을 에뮬레이터에서 누가 대신 채우나 | 사실이 아니라 정할 일. 칸마다 "누가 읽나" 도 아직 다 안 봄 | 목록이 다 된 뒤 **사용자와** 정함 (CLAUDE.md) |

---

## 5. 더 읽기

- run 문서: [`run51-eprocess-later.md`](run51-eprocess-later.md) — 요약 표, 호출 관계, 정보 클래스 표, 뒤 구간 칸별 표, 정정, 아직 확인 필요 (F1 ~ F13 멈춤점까지)
- 결과서: [`_workspace/sast/45_eprocess-later_result.md`](../../_workspace/sast/45_eprocess-later_result.md) — 질문별 답 1 ~ 7, `SharedCommitCharge` 후보, 도구 평가
- 요청서: [`_workspace/sast/45_eprocess-later.md`](../../_workspace/sast/45_eprocess-later.md)
- 짐 지도: [`baggage.md`](baggage.md) 3.11 `_EPROCESS` (머리의 "값을 놓는 주체가 넷"), 3.2 `ConsoleHandle` 새 줄, 7.14 (F1 ~ F13)
- flow.md: [`flow.md`](flow.md) 2.1 `PspAllocateProcess`·`PspInsertProcess`·`PspInsertThread` · 2.2 csrss 절 · 2.3 `PspUserThreadStartup` · 2.4 "ntdll 이 부르는 syscall 이 커널 프로세스 객체 칸을 처음 만드는 자리" · 2.5 KERNELBASE 시작 함수 · 4장
- 앞 해설: [run49](run49-kprocess-token-htable-해설.md) (`_KPROCESS`·신분증·손잡이 목록) · [run50](run50-eprocess-create-해설.md) (`_EPROCESS` 만들 때)

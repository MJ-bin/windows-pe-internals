# run90 thread-leftovers — 해설

> 이 글은 [run90 문서](run90-thread-leftovers.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q142** — run74 가 남긴 정적 꼬리 ① ~ ⑤ (둘째 스레드 만들기, PE_emulator 요청 25 ㄱ 의 뒷정리) (`_workspace/plan.md`, 이번에 **끝**)
  - 사용자 지시 묶음 (Q150a → Q150b → Q151a → Q151b → Q152 → Q142) 의 **마지막** 바퀴다.
  - 다음 항목: **[멈춤]** — Q104 VM 단계 앞에서 사용자 확인을 기다린다. Q143 ~ Q147 과 Q104 의 순서는 그때 정한다.
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → **Q42** (run74 동적 1 ~ 6 과 **같은 실행**)
    - 정적 → 새 **Q168** (① `MiReserveUserMemory` 본문, ② `ctx+0x10` 비트 0 을 읽는 곳 넓게 훑기)
- 근거:
  - run 문서 [`run90-thread-leftovers.md`](run90-thread-leftovers.md) (가장 중요)
  - 요청서 [`_workspace/sast/77_thread-leftovers.md`](../../_workspace/sast/77_thread-leftovers.md) · 결과서 [`77_thread-leftovers_result.md`](../../_workspace/sast/77_thread-leftovers_result.md) — 판정 **분석성공**
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 흐름도 앞 글 · 2.1 · 2.5 · 3장 (새 하위 절 **"유저 쪽"**) · 4장 · 5장. **그림은 안 바뀜**
  - 짐 지도 [`baggage.md`](baggage.md): 새 짐 칸 없음, 근거만 (마흔여섯째판)
  - flow-writer 가 [run74 문서](run74-thread-create.md) 에도 "(→ run90 …)" 화살표를 몇 줄 붙였다 (3절).
- 이번 바퀴는 **정적만** 이다. 새 VM 실행도, 동적 판정도 없다.
  - 값 대조는 run44 때 찍어 둔 사진 로그를 **다시 읽어서** 했다 (2.12).

### 이번 바퀴에서 조심할 곳 다섯

1. **둘째 스레드는 지도 밖이다.**
   - `CreateThread` 는 `main` 안에서 불린다. 흐름도 범위 (진입점까지) 밖이다.
   - 그래서 그림은 그대로다. 줄은 flow.md 3장 · 2.1 · 2.5 에 글로만 들어갔다 (run74 와 같은 까닭).
2. **꼬리 다섯은 한 이야기가 아니다.**
   - ① ② ④ 는 한 줄기 위에 있다 — 유저가 낸 "직원 한 명 더" 신청이 커널로 들어가 작업대 말뚝까지 가는 길.
   - ⑤ 는 옆길 (파이버), ③ 은 "찾아봤는데 못 찾았다" 는 보고다.
   - 2.1 그림에서 다섯이 어디 붙는지 먼저 본다.
3. **"CreateFlags = 0" 은 무조건이 아니다.**
   - `dwCreationFlags` 가 0 이어도, 조건 셋 중 하나라도 참이면 kernelbase 가 일부러 **1** (멈춘 채) 로 바꾼다.
   - test.exe 는 셋 중 둘이 "거짓" 으로 닫혔고, **하나 (다) 만** 실행해 봐야 안다 (2.7).
4. **이름은 대부분 `[추측: 이름]` 이다.**
   - `NtCreateThreadEx` 인자 이름, 클래스 `0x29` 이름 `ProcessThreadStackAllocation`, 정보 칸 이름, `NO_INHERIT`, `CalledFromServer` …
   - 자리 · 상수 · 줄은 `.asm` 에서 읽은 `[정적]` 이다. 이름만 공개 헤더에 맞춘 것이다.
5. **③ 은 못 찾았지만 번호는 붙었다.**
   - `ctx+0x10` 비트 0 을 읽는 곳은 이번 훑기에서 못 찾았다 (2.11). 넓게 훑기는 **Q168 ②** 로 간다 (run 문서 · flow.md 같음, 4절).

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) · 인사 카드 (`_ETHREAD`) → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.3
- 작업대 (유저 스택) · 개인 안내판 (TEB) → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2
- 목수 (`RtlCreateUserStack`) · 쪽지 (`INITIAL_TEB`) → [run54 해설](run54-teb-fill-해설.md) 2.4
- 예약 = **말뚝**, 커밋 = **바닥**, 가드 = "밟으면 알려 줘" **줄** → [run46 해설 2.7](run46-process-heap-해설.md#27-새-말--예약--확정) · [run74 해설](run74-thread-create-해설.md) 2.5 (바)
- 작업대 **주문서** (StackInfo), "0 인 쪽만" 헤더로 메우기 (`CMOVZ`), 다듬기 ① ~ ⑤, `R9` 위 8비트 = 선호 노드 → [run74 해설](run74-thread-create-해설.md) 2.5
- `CreateFlags` = "한 명 더" 신청서의 **체크 칸** → 관리사무소 **내부 양식** → [run74 해설](run74-thread-create-해설.md) 2.6
- 속성 목록 = 신청서 뒤에 붙이는 **추가 요청 쪽지 묶음**, 커널이 받는 속성 다섯, 쪽지 검사 `PspBuildCreateProcessContext` → [run74 해설](run74-thread-create-해설.md) 2.7
- `KeSuspendThread` = **대기 팻말** → [run12 해설 2.10](../02_sprint/run12-insert-thread-해설.md#210-멈춘-직원도-대기-줄에-선다--그럼-언제-실제로-서-있나)
- `SameTebFlags` = 안내판 **표시 칸** → [run54 해설 2.8](run54-teb-fill-해설.md#28-sametebflags-첫-스레드는-늘-0x400-뿐)
- KERNELBASE · KERNEL32 = 가게에 들인 **건물**, 껍데기 함수 = kernel32 **앞 접수대**, KERNELBASE = **대행 책상**, 별명이 "누가 묻나" 로 풀림 → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.3
- api-set = **별명** → [run37 해설](../02_sprint/run37-k32-import-order-해설.md), IAT = 벽 **콘센트** → [run30 해설](../02_sprint/run30-exe-crt-start-해설.md) · [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.7
- csrss = 이웃 **인사팀 사무실**, csrss 연결 = 사무실에 거는 **전화**, DLL 전역 (`KernelBaseGlobalData`) = 건물 **수첩** → [run68 해설](run68-dll-init-rest-해설.md) 2.4
- "나는 서버 (csrss 쪽) 인가" 표시 (`gfServerProcess`), PE 머리 서브시스템 1 (NATIVE) → [run85 해설](run85-user32-init-해설.md) 2.11
- 활성화 문맥 = 건물 **작업 지침서** (설정 묶음), 활성화 문맥 스택 (TEB `+0x2c8`) = 직원이 손에 든 **지침서 더미** → [run54 해설](run54-teb-fill-해설.md) 2.10 · [run88 해설](run88-sxs-redirect-해설.md) 첫머리
- 정보 클래스 = 관리사무소 민원 창구의 **신청 종류 번호**, PDB 에 `PROCESSINFOCLASS` 없음 → [run51 해설](run51-eprocess-later-해설.md) 2.2
- 이상적 프로세서 = 직원의 **선호 자리**, 친화도 = **앉아도 되는 자리 목록** → [run54 해설](run54-teb-fill-해설.md) 2.9
- NUMA 노드 = 상가의 **구역** → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 3.4 표
- PRCB = CPU 마다 붙은 **계산대 장부** → [run75 해설](run75-thread-switch-해설.md) 2.3
- 오프셋 순서 ≠ 실행 순서 ("부록 보고 오기") → [run12 해설 2.3](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다)
- 사진 K2 · P2 → [run44 해설](run44-baggage-snap-해설.md) 2.8

이번에 처음 나오는 말 — **`CMOVNZ`**, **`SubProcessTag`**, **점프 표**, **이전 모드**, **ZeroBits**, **`KiProcessorBlock`**, **파이버**, **할당 단위** — 은 본문에서 처음 나올 때 한 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 가게 (프로세스) · 직원 (스레드) · 관리사무소 (커널) · 관리사무소 **신청 창구** (syscall).
- run74 에서 본 둘째 직원 뽑기: 이미 일하는 직원 (`main`) 이 **"한 명 더" 신청서** (`NtCreateThreadEx`) 를 창구에 낸다. 뒤에서 인사 카드 · 작업대 · 안내판을 만드는 담당은 첫 직원 때와 같다.
- 이번에 붙는 그림:
  - 직원은 신청서를 **직접** 쓰지 않는다. kernel32 건물 **앞 접수대** (`kernel32!CreateThread`) 에 말하면, KERNELBASE **대행 책상** (`CreateRemoteThreadEx`) 이 신청서 11칸과 추가 요청 쪽지 두 장을 써서 창구에 낸다 (① 2.3 ~ 2.7).
  - 특별한 손님일 땐 대행 책상이 새 직원을 **대기 팻말 건 채로** 받는다. 꼬리표를 달고 지침서 더미를 쥐여 준 뒤 **직접 팻말을 뗀다** (2.7).
  - 관리사무소 안에서는 목수가 작업대 땅에 **말뚝만** 박아 달라고 다른 창구 (클래스 `0x29`) 에 맡긴다. 그 창구는 정말 말뚝만 박는다. 64KB 눈금에 맞춰서 (② 2.8).
  - 선호 자리 쪽지가 있으면, 그 자리가 있는 **구역** 번호 + 1 을 목수에게 넘긴다 (④ 2.9).
  - 가게 관리실 (ntdll) 에도 **솜씨가 같은 목수** 가 한 명 더 있다. 그 목수를 부르는 건 파이버 만드는 곳 하나뿐이다 (⑤ 2.10).
- flow.md 3장 새 하위 절의 비유는 그림이 조금 다르다 ("가게 (kernel32) · 본사 (KERNELBASE) · 관청 (커널)"). 그림만 다르고 사실은 같다.

### 한 줄로 먼저 — 무엇을 얻었나

| 꼬리 | 답 (run 문서 그대로) |
|---|---|
| ① 유저 쪽이 커널에 주는 것 | `kernel32!CreateThread +0x36` → `KERNELBASE!CreateRemoteThreadEx +0x2e2` → `NtCreateThreadEx`. 스택 **(0, 0)** · ZeroBits 0 · 속성 **2개** (`CLIENT_ID` · `TEB_ADDRESS`, `TotalLength 0x48`) · 핸들 -1 · 접근 `0x1fffff` · `ObjectAttributes` NULL. CreateFlags 는 조건 셋이 다 거짓이면 **0**, 하나라도 참이면 **1** `[정적]` (2.3 ~ 2.7) |
| ② 클래스 `0x29` 가 정말 예약하나 | **예약 (`MEM_RESERVE`) 만** 한다. `NtSetInformationProcess +0x485` → `MmAllocateUserStack` → `MiAllocateVirtualMemory` → `MiReserveUserMemory`. 시작 64KB 정렬 `[정적]` (2.8) |
| ③ `ctx+0x10` 비트 0 을 누가 읽나 | **못 찾음** — 훑은 함수 일곱 안에 읽는 줄이 없다 `[정적: 훑은 범위 안에서]` (2.11) |
| ④ `PspAllocateThread +0x8c7` 노드 갈래 | "노드 ≠ 0" 이 아니라 **이상적 프로세서 속성이 있을 때** 탄다. 값 = **노드 + 1** (0 = 선호 없음). test.exe 는 0 `[정적]` (2.9) |
| ⑤ ntdll `RtlCreateUserStack` | 커널 것과 **같은 꼴** (다른 점은 2.10 표). 부르는 곳은 kernelbase `CreateFiberEx` 하나 — 스레드 만들기 길엔 안 쓰인다 `[정적]` (2.10) |
| (덤) `KernelBaseGlobalData+4` 의 뜻 | "이 프로세스가 csrss 같은 **서버 프로세스인가**". 결과서 `[추측]` → **`[정적]`** (run68 · run85 줄 + test.exe `Subsystem` 3 → 0) (2.7) |

- 그래서 → **Q42 의 동적 몫 "`CreateThread` 가 커널에 정말 (0, 0) 을 넘기나" 는 정적으로 닫혔다 (예).**
- 남은 건 CreateFlags 의 조건 (다) 하나다 (Q42 동적 1).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → 첫 스레드 → ntdll 로더 … → NtContinue 은퇴
 └─ 진입점 → (CRT) → main                                      ← 여기까지가 흐름도 지도
      └─ main: CreateThread(NULL, 0, Worker, NULL, 0, NULL)      [소스] test.c:34
           ├─ kernel32!CreateThread → KERNELBASE!CreateRemoteThreadEx   ← 이번 ① (유저 쪽, 처음 봄)
           └─ ntdll!NtCreateThreadEx ── syscall ──▶ nt!NtCreateThreadEx (run74)
                └─ PspCreateThread → PspAllocateThread              ← 이번 ④ (노드 갈래)
                     └─ PspSetupUserStack → RtlCreateUserStack (run74)
                          └─ ZwSetInformationProcess(0x29)          ← 이번 ② (말뚝 창구 안)
```

- 옆길: ntdll `RtlCreateUserStack` ← kernelbase `CreateFiberEx` (⑤, test.exe 는 안 탐).
- ③ 은 쪽지 검사 담당 `PspBuildCreateProcessContext` (run74 해설 2.7) 가 남긴 표시 한 비트의 행방이다.

### 왜 이 질문이 지금 왔나

- run74 (Q141a) 가 PE_emulator 요청 25 ㄱ 을 **커널 쪽에서** 닫으면서, 작은 꼬리 다섯을 Q142 로 남겼다.
- 가장 큰 건 ① 이다. "test.exe 의 `CreateThread(…, 0, …)` 가 커널에 정말 (0, 0) 을 넘기나" 가 `[추측]` 이었다. 유저 쪽 `CreateRemoteThreadEx` 를 안 봤기 때문이다.
  - ① 이 닫히면 Q42 동적 몫 하나가 VM 없이 닫힌다 (요청서 "배경").
- 정적 (sast/77) 은 2026-11-12 에 이미 끝났다. 사용자 결정으로 PE_emulator 요청 26 ~ 30 (run81 ~ run89) 을 먼저 하고, 문서만 이번에 썼다. 그래서 run 번호가 90 이다.
- 사용자 지시 묶음 ("vm 제외하고 Q142 까지") 의 마지막 바퀴다.

### 무엇이 궁금했나

한 문장으로: **run74 가 `[추측]` 으로 남긴 다섯 자리를 코드로 닫을 수 있나.**

| # | 질문 (요청서를 줄임) | 이 글 어디서 |
|---|---|---|
| ① | `CreateRemoteThreadEx` 가 `NtCreateThreadEx` 에 주는 `CreateFlags` · 스택 인자 (9 · 10번째) · `AttributeList`. 커널에 (0, 0) 이 가나. 부른 뒤 하는 일 | 2.3 ~ 2.7 |
| ② | 클래스 `0x29` 갈래가 부르는 함수, 정말 `MEM_RESERVE` 인가, 정렬, 정보 칸 풀이, PDB 이름 | 2.8 |
| ③ | `PspBuildCreateProcessContext +0x1876 ~ +0x1882` 가 켠 `ctx+0x10` 비트 0 을 누가 읽나 | 2.11 |
| ④ | `PspAllocateThread +0x8c7` 노드 갈래의 조건과 하는 일 | 2.9 |
| ⑤ | ntdll `RtlCreateUserStack` 이 커널 것과 같은 꼴인가, 누가 부르나 | 2.10 |

### 바퀴 전에 알던 것

- `[소스]` `sample/test.c:34` `CreateThread(NULL, 0, Worker, NULL, 0, NULL)` — 스택 크기 0, 플래그 0, 스레드 ID 안 받음.
- `[정적]` run74: 커널 `NtCreateThreadEx` 는 9 · 10번째 인자를 주문서 `+0x10` (커밋) · `+0x18` (예약) 에 **그대로** 복사한다 (`+0x15f` · `+0x16a`). 0 이면 맨 끝 목수가 **0 인 쪽만** exe 헤더로 메운다.
- `[정적]` run74: 속성 목록이 NULL 이 아니면 `NtCreateThreadEx +0xde` 에서 `PspBuildCreateProcessContext` 가 검사한다. 커널이 받는 속성은 다섯 (`0x10003` · `0x10004` · `0x3000c` · `0x3000e` · `0x3001e`).
- `[정적]` run74: `CreateFlags 0x1` → 내부 양식 `0x1` → `PspInsertThread +0x2e0 KeSuspendThread`. `CreateFlags = 0` 이면 `SameTebFlags` 를 안 쓴다 (`+0x66f/+0x673`).
- `[정적]` run74: 목수 `RtlCreateUserStack +0x104 ZwSetInformationProcess(-1, 0x29, 40바이트, 0x28)` 가 정보 칸 (`+0` 노드 · `+0x10` 크기 · `+0x18` ZeroBits · `+0x20` 출력) 으로 시작 주소 `A` 를 받는다.
- `[추측]` run74:
  - 그게 "예약" 이다 (본문 안 봄).
  - 노드는 보통 0.
  - 유저가 (0, 0) 을 넘긴다.
  - 유저 `CreateThread` 가 `NtCreateThreadEx` 로 온다 (run11 부터).
- `[정적]` run68 · run85:
  - kernelbase 시작 함수는 csrss 연결 결과로 `KernelBaseGlobalData+0x4` 를 쓴다 (`+0x337`).
  - ntdll `CsrClientConnectToServer` 는 다섯째 인자 칸에 클라이언트면 0 (`+0xc7`), 서버면 1 (`+0x1cb`) 을 쓴다.
  - 이 둘을 이어 **뜻** 으로 묶은 건 이번 바퀴 flow-writer 다 (2.7).
- `[관찰]` run44 K2: 첫 스레드 `DeallocationStack` `0x9b2fc00000`.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 다섯 꼬리가 붙는 자리

run 문서 "호출 관계" 를 줄였다. 호출 줄의 오프셋은 **돌아올 자리 (ret)** 다. `+0x120` 처럼 호출이 아닌 줄은 그 명령 자리다.

```
test!main                                                [소스] test.c:34
 └─ kernel32!CreateThread
      └─ +0x3d  CreateRemoteThreadEx                     (KERNELBASE 로 풀림은 [추측])   ★ 2.3
           │  +0x140 ~ +0x1bd  속성 2칸, TotalLength 0x48                                ★ 2.6
           ├─ (1) +0x218  RtlQueryInformationActivationContext  실패면 스레드 안 만듦    ★ 2.7
           │  +0x22e · +0x237 · +0x245  조건 셋 → 하나라도 참이면 CreateFlags = 1        ★ 2.7
           │  +0x280 ~ +0x291  (StackSize, MaximumStackSize) = (0, 0)                    ★ 2.5
           ├─ (2) +0x2e9  ntdll!NtCreateThreadEx ── syscall ──▶ nt!NtCreateThreadEx (run74)
           │        └─ … PspAllocateThread
           │             │  +0x1b9 → +0x886 → +0x8c7  노드 + 1                           ★ 2.9
           │             └─ … RtlCreateUserStack
           │                  └─ +0x109 ZwSetInformationProcess(0x29)
           │                       ⇢ NtSetInformationProcess +0x485 갈래
           │                          └─ +0x513 MmAllocateUserStack                       ★ 2.8
           │                               └─ … +0x339 MiReserveUserMemory  (본문 안 봄)
           ├─ (3) +0x348  NtResumeThread               멈춘 채 만든 경우만                ★ 2.7
           └─ (4) +0x4e8  RtlReleaseActivationContext  핸들 있을 때

KERNELBASE!CreateFiberEx
 └─ +0xd5  ntdll!RtlCreateUserStack                      (ntdll 쪽 유일 caller)          ★ 2.10
```

- 모두 `[정적]`. 실행해서 본 줄은 없다.
- `⇢` = `ZwSetInformationProcess` → `NtSetInformationProcess` 사이. 커널 안 시스템 콜 길이라 줄을 안 봤다 `[추측: Zw 스텁 → 서비스 표]`.
- ③ (`ctx+0x10` 비트 0) 은 이 그림에 자리가 없다. 읽는 줄을 못 찾았으니까 (2.11).
- 비유로 한 번: 앞 접수대 → 대행 책상 → 창구 → (인사 카드 담당) → 목수 → 말뚝 창구. run74 는 "창구 → 목수" 까지였고, 이번에 **앞쪽 두 칸** 과 **맨 끝 한 칸** 이 붙었다.

### 2.2 한 줄씩 읽는 법

#### 줄 1: `├─ (2) +0x2e9 ntdll!NtCreateThreadEx`

- **누가**: `KERNELBASE!CreateRemoteThreadEx` (rva `0xca3f0`, 2126바이트). 대행 책상.
- **어디서**: 부르는 자리 `+0x2e2`, 돌아올 자리 `+0x2e9`.
  - 모양은 `CALL qword ptr [0x18029b990]` — KERNELBASE 의 **콘센트 칸** (IAT) 을 거쳐 ntdll 로 간다 (결과서).
  - 실행 중 멈추면 콜스택에 `KERNELBASE!CreateRemoteThreadEx+0x2e9` 로 보일 자리다. 아직 안 봤다.
- **누구를**: `ntdll!NtCreateThreadEx`. 그 안에서 syscall 로 관리사무소 창구 `nt!NtCreateThreadEx` (run74) 에 들어간다. (ntdll 스텁 → syscall 은 `[추측]`, flow.md 3장)
- **무엇을 위해**: "직원 한 명 더" 신청서를 **실제로 내려고**. 인자 11개 (2.4 표).
- 이 한 줄이 run11 부터 `[추측]` 이던 "유저 `CreateThread` 가 `NtCreateThreadEx` 로 온다" 를 코드로 잇는다 `[정적]`.
  - 단 kernel32 의 콘센트 칸이 KERNELBASE 로 풀린다는 한 칸은 아직 `[추측]` 이다 (2.3).

#### 줄 2: `│ +0x280 ~ +0x291 (StackSize, MaximumStackSize) = (0, 0)`

호출이 아니라 **값 고르기** 줄이다. 핵심 두 명령 (결과서):

```
CreateRemoteThreadEx +0x28d  CMOVNZ RAX,RDX
CreateRemoteThreadEx +0x291  CMOVNZ RDX,RBX
```

- **새 말 — `CMOVNZ`**: [run74 해설](run74-thread-create-해설.md) 2.5 (다) 의 `CMOVZ` 와 반대다. 바로 앞 검사 결과가 **0 이 아닐 때만** 값을 옮기고, 아니면 그대로 둔다.
  - 비유: "체크 표시가 **있으면** 이 칸 내용을 저 칸으로 옮겨 적어라".
- 앞에서 준비해 둔 값:
  - `RAX` = 0 (`+0x280 MOV RAX,RBX`, `RBX` 는 0)
  - `RDX` = `dwStackSize` (`+0x285`)
  - 검사 = `+0x283 TEST ESI,ESI`. `ESI` 는 `+0x1c8` 에서 만든 RESERVATION 비트다.
- **누가 / 어디서**: 대행 책상, 신청서를 내기 직전.
- **무엇을 위해**: `dwStackSize` 를 9번째 (커밋 칸) 에 넣을지, 10번째 (예약 칸) 에 넣을지 고르려고.
- 자세한 풀이는 2.5.

#### 줄 3: 말뚝 창구 담당의 첫 줄 — `MmAllocateUserStack +0xcb`

```
MmAllocateUserStack +0xcb  MOV dword [RSP+0x20],0x2000
```

- **누가**: `nt!MmAllocateUserStack` (rva `0x904d94`, 536바이트). 말뚝 창구 (`NtSetInformationProcess` 클래스 `0x29`) 가 부르는 담당.
- **어디서**: `+0xcb`. 바로 뒤 `+0xd8` 에서 `MiAllocateVirtualMemoryPrepare` 를 부른다. `[RSP+0x20]` 은 그 함수의 **5번째 인자** 자리 — **할당 종류** 다 (결과서).
- **무엇을**: 할당 종류에 `0x2000` (`MEM_RESERVE`) **하나만** 넣는다. `0x1000` (`MEM_COMMIT`) 비트는 없다.
- **무엇을 위해**: 땅에 **말뚝만** 박으려고. 바닥 (커밋) 은 안 깐다.
- 이 한 줄이 run74 의 `[추측: 예약]` 을 `[정적]` 으로 바꾼 첫 근거다 (2.8).

### 2.3 ① 앞 접수대 — `kernel32!CreateThread` 는 세 칸만 채워 민다

#### test.exe 는 kernel32 에서 가져온다 `[정적]`

- test.exe 의 import 표에 `KERNEL32.dll!CreateThread` 가 있다 (flow-writer 가 PE 표를 직접 읽음, 보탠 것 ⑤).
- 그래서 유저 쪽 사슬 첫 칸은 `kernel32!CreateThread` 다.

#### 껍데기 — 세 칸만 채운다 `[정적]`

- [run34 해설 2.3](../02_sprint/run34-parent-leftovers-해설.md) 의 `kernel32!CreateProcessW` 처럼, `kernel32!CreateThread` (rva `0x315c0`, 71바이트) 도 **껍데기** 다.
  - 비유: kernel32 건물 **앞 접수대**. 서류를 받아 옆 KERNELBASE **대행 책상** 으로 민다.
- 다만 이번 접수대는 서류를 그냥 밀지 않고, `+0x36` (ret `+0x3d`) 에서 `CreateRemoteThreadEx` 를 부르기 전에 **세 칸** 을 채운다:
  - `hProcess = -1` (`+0x32`) — "지금 이 가게".
  - `dwCreationFlags & 0x10004` (`+0x11`) — `0x4` (`CREATE_SUSPENDED`) 와 `0x10000` (`STACK_SIZE_PARAM_IS_A_RESERVATION`) 두 비트만 남긴다.
  - `lpAttributeList = NULL` (`+0x1c`) — 추가 요청 쪽지 묶음은 **안 준다**. 늘.
- `KERNELBASE!CreateThread` (rva `0x18b3d0`, 64바이트) 도 몸이 같다. `+0x36` (ret `+0x3b`) 에서 바로 부른다 `[정적]`.
  - test.exe 는 kernel32 쪽 것을 쓴다 (위).

#### kernel32 → KERNELBASE 는 콘센트 칸 하나를 거친다 — 여기만 `[추측]`

- `kernel32!CreateThread +0x36` 의 콘센트 칸 (`[0x18008ba50]`) 이름은 `api-ms-win-core-processthreads-l1-1-0` 이다. KERNELBASE 가 아니라 **별명** 이다 `[정적]`.
- 이게 실행 때 KERNELBASE 로 풀린다는 건 `[추측]` 이다 (run 문서).
- 왜 그렇게 짐작하나 — 단계별로 (이 해설의 정리):
  1. 근거: [run34 해설 2.3](../02_sprint/run34-parent-leftovers-해설.md) 에서 **같은 별명** (`processthreads-l1-1-0`) 을 `CreateProcessW` 로 봤다. 별명 대조표 그 줄엔 "kernel32 가 물으면 kernelbase" 가 있었다 (run34, `[정적: PE 표]`).
  2. 그래서: kernel32 의 `CreateThread` 칸도 같은 별명 줄이니 KERNELBASE 로 풀릴 것이다.
  3. 아직 확인 아님: 이번 바퀴는 이 칸을 따로 풀어 보지 않았다.

### 2.4 ① 대행 책상이 쓰는 신청서 — 인자 11칸

`CreateRemoteThreadEx` 안에서 `RSP` 는 들어올 때보다 `0x708` 작다 (저장 7개 + `SUB RSP,0x6d0`). 스택 칸 번호를 읽을 때 이걸 빼고 본다 (결과서 "읽는 법").

| 칸 | test.exe 값 | 줄 | 닫힘 |
|---|---|---|---|
| 1 ThreadHandle | `&[RSP+0x100]` (핸들 받을 칸) | `+0x2da` | 닫힘 |
| 2 DesiredAccess | `0x1fffff` | `+0x2d5` | 닫힘 |
| 3 ObjectAttributes | NULL (`lpThreadAttributes == NULL`) | `+0x2d2` | 닫힘 |
| 4 ProcessHandle | -1 | `+0x2cf` | 닫힘 |
| 5 · 6 StartRoutine · Argument | `Worker` · NULL | `+0x2c2 ~ +0x2ca` · `+0x2b5 ~ +0x2bd` | 닫힘 |
| 7 CreateFlags | **0** / **1** (조건) | `+0x2b1` | 조건 값만 동적 |
| 8 ZeroBits | 0 | `+0x2ac` | 닫힘 |
| 9 StackSize | **0** | `+0x2a7` | 닫힘 |
| 10 MaximumStackSize | **0** | `+0x2a2` | 닫힘 |
| 11 AttributeList | `&[RSP+0x210]`, 항목 2 | `+0x295` | 닫힘 |

- 자리 · 상수는 `[정적]`. 인자 **이름** 은 공개 헤더 순서에 맞춘 `[추측: 이름]`.
- 비유: 대행 책상이 쓰는 "한 명 더" 신청서. 칸 11개 중 **10개는 test.exe 에서 값이 정해졌다.** 체크 칸 (7번) 하나만 상황에 따라 달라진다.
- 맨 앞 거르기 `[정적]`: `dwCreationFlags` 에 `0x4` · `0x10000` 말고 다른 비트가 있으면 `0xC000000D` 로 돌려보낸다 (`+0x120 TEST R14D,0xfffefffb` · `+0x127 JNZ`).
  - 앞 접수대가 이미 두 비트만 남겨 오니, `CreateThread` 길에선 걸릴 일이 없다.
- 5번 칸이 `Worker` 로 닫히면서, run74 해설 2.4 끝의 `[추측]` ("`CreateThread` 셋째 인자가 그대로 StartRoutine 으로 가나") 도 닫혔다.
- 8번 ZeroBits 는 2.8 에서 푼다.

### 2.5 ① 작업대 크기 — (0, 0) 이 정말 간다

#### RESERVATION 비트 하나가 칸을 고른다 `[정적]`

- `+0x1c5 ~ +0x1c8`: `ESI` = `dwCreationFlags & 0x10000` (RESERVATION 비트).
- `+0x280 ~ +0x291` (2.2 줄 2): 이 비트로 `dwStackSize` 가 들어갈 칸을 고른다.

| `0x10000` 비트 | 9번째 StackSize (`+0x2a7`) | 10번째 MaximumStackSize (`+0x2a2`) |
|---|---|---|
| 없음 | `dwStackSize` | **0** |
| 있음 | **0** | `dwStackSize` |

- run74 에서 본 대로 커널은 9번째 → 주문서 **커밋** 칸, 10번째 → **예약** 칸으로 그대로 옮긴다.
- 그러니 두 줄을 이어 읽으면 이렇다 (이 해설의 정리):
  - 비트 없음 → `dwStackSize` 는 **바닥 (커밋)** 크기로 간다.
  - 비트 있음 → `dwStackSize` 는 **말뚝 (예약)** 크기로 간다.
- 비유: 앞 접수대 서류의 "작업대 크기" 한 칸을, 체크 하나 보고 주문서 "바닥 크기" 칸에 옮겨 적을지 "말뚝 크기" 칸에 옮겨 적을지 정한다. 나머지 칸은 0 으로 비운다.

#### test.exe 는 `dwStackSize = 0` → 어느 쪽이든 (0, 0) `[정적]`

- 0 을 어느 칸에 옮겨 적어도 0 이다. 다른 칸은 원래 0. 그래서 **(0, 0)**.
- 8번째 ZeroBits 도 늘 0 (`+0x2ac`).
- 그다음은 run74 그대로 `[정적]`:
  1. 커널 `NtCreateThreadEx +0x15f/+0x16a` 가 주문서로 그대로 복사.
  2. 목수 `RtlCreateUserStack` 이 0 인 쪽마다 exe 헤더 값으로 메움.
  3. 다듬기 ① ~ ⑤.
- 그래서 Worker 작업대 = 예약 `0x100000` · 커밋 `0x1000` · 가드 `0x3000` — **첫 스레드와 같은 값** `[정적]` (run74 식, `PEB.MinimumStackCommit` 0 일 때).
- run74 해설 2.5 (사) 의 "(0, 0) 으로 간다고 **치면** `[추측]`" 은 이제 "친다" 가 필요 없다.

#### 덤 — 0 이 아닌 크기를 주면 (test.exe 아님)

- 결과서 (c): `dwStackSize ≠ 0` 이고 RESERVATION 비트가 없으면 (커밋 = 크기, 예약 = 0) 으로 간다.
- 그러면 커널이 예약을 헤더 값으로 메운 뒤, "커밋 ≥ 예약 이면 1MB 올림" 규칙 ([run74 해설](run74-thread-create-해설.md) 2.5 (마) ②) 을 탈 수 있다.

### 2.6 ① 추가 요청 쪽지는 딱 두 장

#### 쪽지는 대행 책상이 **직접** 쓴다 `[정적]`

- 앞 접수대는 `lpAttributeList = NULL` 을 넘겼다. 그런데 대행 책상이 커널에 보내는 11번째 인자는 NULL 이 **아니다**.
- `lpAttributeList == NULL` 이면 (`CreateThread` 는 늘 그렇다) 대행 책상이 자기 스택 `[RSP+0x210]` 에 쪽지 두 장을 직접 쓴다:

| 쪽지 | 번호 | Size | 값 (결과를 받아 적을 칸) | 줄 |
|---|---|---|---|---|
| 1 | `0x10003` (CLIENT_ID) | `0x10` | 새 직원 사번 (ClientId) 받는 16바이트 칸 | `+0x140` · `+0x14c` |
| 2 | `0x10004` (TEB_ADDRESS) | `8` | 새 직원 안내판 (TEB) 주소 받는 칸 | `+0x170` · `+0x17c` |

- 둘 다 ReturnLength 칸은 0.
- 묶음 길이: `+0x1a0 ~ +0x1bd` 가 `TotalLength = 2 × 0x20 + 8 = 0x48` 을 센다.
- 번호 · 크기는 `[정적]`, 이름 (CLIENT_ID 등) 은 `[추측: 이름]` ([run74 해설](run74-thread-create-해설.md) 2.7 표와 같다).
- 비유: "사번은 여기 적어 주세요", "안내판 위치는 여기 적어 주세요" 쪽지 두 장. 대행 책상이 **늘** 붙인다. 직원이 따로 쪽지를 안 줘도.

#### 커널 쪽지 검사도 통과한다 `[정적]`

run74 해설 2.7 의 규칙에 대 보면 (결과서):

- `TotalLength ≥ 0x28` — `0x48` 은 통과.
- `(TotalLength − 8) & 0x1f == 0` — `(0x48 − 8) & 0x1f == 0` 통과.
- 항목 `Size` — CLIENT_ID `0x10` · TEB_ADDRESS `8`. 맞다.
- 스레드 모드면 항목마다 THREAD 비트 (`0x10000`) — 둘 다 있다.
- INPUT 비트 항목은 ReturnLength NULL — 해당 없음.

#### 나머지 셋은 `CreateThread` 길엔 안 나온다 `[정적]`

- 커널은 다섯을 받는다. 그중 그룹 친화도 (`0x3000c`) · 이상적 프로세서 (`0x3000e`) · XState (`0x3001e`) 셋은 `CreateThread` 길에서 **안 쓰인다**.
- 그 셋이 나올 수 있는 길:
  - `lpAttributeList` 를 직접 주는 호출자 (`CreateRemoteThreadEx` 를 직접 부름) → `+0x1af JNZ` → `+0x662 BasepConvertWin32AttributeList` 가 같은 칸에 항목을 더 넣는다 (최대 `0x25` 칸). 무엇을 넣는지는 본문을 안 봤다.
  - `NtCreateThreadEx` 를 직접 부르는 다른 코드 `[추측]`.
- PE_emulator: [run74 해설](run74-thread-create-해설.md) 2.7 끝의 "'`CLIENT_ID` · `TEB_ADDRESS` 둘만' 은 좁다" 는 **커널 기준** 이었다. **`CreateThread` 길 기준으로는 딱 맞다.** 다섯을 다 받을지는 다른 exe 몫이고, 구현 범위는 사용자 결정이다 (run74 와 같음).

#### 받은 사번은 어디에 쓰나 `[정적]`

- 부른 뒤 `lpThreadId ≠ NULL` 이면 사번 칸의 `UniqueThread` 하위 4바이트를 호출자에게 복사한다 (`+0x30e` · `+0x315`, 결과서).
- test.exe 는 `lpThreadId = NULL` 이라 `+0x30c JZ` 로 건너뛴다.

### 2.7 ① 체크 칸 (CreateFlags) — "0 이면 0" 이 아니다

이번 바퀴에서 제일 손이 많이 가는 곳이다. 천천히 가자.

#### 바꾸는 줄은 하나 — `CREATE_SUSPENDED (0x4)` → `0x1` `[정적]`

- `+0x25b TEST R14B,0x4` · `+0x25f JNZ` → `+0x274 MOV ECX,1`. 안 켜졌으면 `+0x261 MOV ECX,EBX` (0).
- `0x10000` 비트는 체크 칸으로 안 간다 (작업대 크기 쪽, 2.5).
- 그러니 `dwCreationFlags = 0` 이면 보통 0 이다. 그런데 **"보통"** 이다.

#### 조건 셋 — 하나라도 참이면 무조건 1 `[정적]`

`+0x22e ~ +0x24d` 에서 셋을 본다. 하나라도 참이면 `+0x26c` 에서 "멈춘 채 만들었음" 표시 (`[RSP+0xf0] = 1`) 를 하고 `+0x274 ECX = 1`.

| 조건 | 보는 칸 | 줄 | test.exe |
|---|---|---|---|
| (가) | `KernelBaseGlobalData+0x4` (kernelbase rva `0x3aeec4`) ≠ 0 | `+0x22e` | **0** `[정적+샘플]` (아래 "조건 (가) 의 뜻") |
| (나) | TEB `+0x1720` (`SubProcessTag`) ≠ 0 | `+0x237` | 메인 스레드는 은퇴까지 0 `[관찰]`. `main` 에서 `CreateThread` 까지는 `[추측: 0 그대로]` |
| (다) | 지금 활성화 문맥 핸들 (`[RSP+0x118]`) ≠ 0 이고, 그 플래그 비트 0 (`NO_INHERIT` 쯤 `[추측: 이름]`) 이 꺼짐 | `+0x245` · `+0x4aa` | **모름** — 동적 (Q42 1) |

- **새 말 — `SubProcessTag`**: TEB `+0x1720` 칸의 PDB 이름. 결과서는 "서비스 호스트가 붙이는 칸" 으로 짐작한다 `[추측]`.
  - 비유: 직원 개인 안내판에 붙는 **소속 꼬리표**.
- (다) 의 활성화 문맥은 run54 · run88 해설의 **작업 지침서 (설정 묶음)** 다. (다) 는 "지금 손에 든 지침서가 있고, 그게 '물려주지 마' 표시가 아닌가" 를 묻는 셈이다.
  - 그 핸들은 바로 앞 `+0x211` (ret `+0x218`) `RtlQueryInformationActivationContext` 가 돌려준 값이다. 지금 프로세스일 때만 부른다 (`+0x1d9 CMP R12,-1`).

#### 멈춘 채 만드는 까닭 — 손보고 깨우기 `[정적]`

조건이 참이면 대행 책상은 새 직원을 **대기 팻말 건 채로** 받는다. (체크 칸 1 = run74 표의 `CREATE_SUSPENDED` 줄.) 그리고 커널에서 돌아오면 `+0x307 JNZ` → `+0x36d` 로 가서:

1. 메인 TEB 의 `SubProcessTag` 를 새 TEB 에 복사한다 (`+0x36d ~ +0x38c`) — 소속 꼬리표 옮겨 달기.
2. 활성화 문맥이 있고 NO_INHERIT 가 아니면:
   - `+0x41b RtlAllocateActivationContextStack` → 새 TEB `+0x2c8` 에 저장 (`+0x448`)
   - → `+0x46c RtlActivateActivationContextEx`
   - 새 직원 손에 **지침서 더미** 를 쥐여 주고, 맨 위에 지금 지침서를 올리는 셈이다.
3. 서버 프로세스면 (`+0x3a5`, 조건 (가) 와 같은 칸) `GetModuleHandleA("csrsrv")` (`+0x3b9`) → `GetProcAddress("CsrCreateRemoteThread")` (`+0x3c8`) → 간접 호출 (`+0x3e6`, CFG).
4. `dwCreationFlags` 에 `CREATE_SUSPENDED` 가 **없으면** `+0x341 NtResumeThread` — 대행 책상이 **직접 팻말을 뗀다**.

- 비유: 특별한 손님 (서버 가게 · 꼬리표 단 직원 · 지침서를 든 직원) 이 "한 명 더" 를 부탁하면, 대행 책상은 새 직원을 **잠깐 세워 둔 채** 받는다. 꼬리표를 달아 주고 지침서 더미를 쥐여 준 뒤에야 "이제 출근하세요" 한다.
  - 일반 손님이면 그냥 바로 출근시킨다 (아래 "test.exe 가 탈 길").
- 왜 굳이 세워 두나 — 새 직원이 움직이기 시작한 **뒤** 에 안내판을 고치면 늦으니까, 서 있는 사이에 손보는 것으로 보인다 `[추측]` (이 해설의 짐작. 근거는 "멈춘 채 만들고 → 손보고 → 깨운다" 는 줄 순서뿐).

#### 체크 칸이 1 이어도 안내판 표시 칸은 그대로 `[정적]` (flow-writer 보탬 ③)

- [run74 해설](run74-thread-create-해설.md) 2.6 (라): `CreateFlags = 0` 이면 커널이 `SameTebFlags` 에 펜을 안 댄다.
- 그럼 1 이면? flow-writer 가 `.asm` 으로 다시 봤다:
  1. `PspAllocateThread +0x54a ~ +0x5a0` 은 `R12W` 를 내부 양식의 `0x40` · `0x2` · `0x80` · `0x100` 비트로만 만든다.
  2. `0x1` (멈춤) 은 안 들어간다.
  3. 그래서 `R12W = 0` → `+0x66f/+0x673` 안 탐 → `+0x793` 건너뜀.
  4. → **0 이든 1 이든 같다.**
- 1 일 때 달라지는 건 둘이다:
  - 커널 쪽: run74 표대로 `PspInsertThread +0x2e0 KeSuspendThread` (대기 팻말) 가 선다. 그래서 run74 해설 2.6 (바) 의 "`+0x2e0` 은 보통 안 설 것 `[추측]`" 은 그대로 `[추측]` 이다 — 조건 (다) 에 달렸다. (두 문서의 줄을 이은 것.)
  - 유저 쪽: `NtResumeThread` syscall 이 하나 더 온다 (`CreateThread` 는 `CREATE_SUSPENDED` 를 안 줬으니까).

#### test.exe 가 탈 길 — 셋 다 거짓이면 `[정적]`

- `+0x307 JNZ` 안 탐
- → `lpThreadId` NULL 이라 `+0x30c JZ`
- → `+0x321 JZ +0x4d4`
- → 활성화 문맥 핸들이 있으면 `+0x4e1 RtlReleaseActivationContext`
- → 반환 = 스레드 핸들 (`+0x506`).
- **`NtResumeThread` · CSR 알림 · 활성화 없음.**

#### 하나 더 — 앞 단계가 실패하면 스레드를 아예 안 만든다 `[정적]` (flow-writer 보탬 ④)

- `RtlQueryInformationActivationContext` 가 실패하면 `+0x228 JS` → `+0x352` (`+0x363 DbgPrint` 실패 기록) → `+0x368 JMP +0x4d4`.
- **`NtCreateThreadEx` 를 안 부르고 끝난다.** 결과서 (e) 표엔 없던 길이다.
- 함수 안 `DbgPrint` 셋 (`+0x363` · `+0x4a3` · `+0x4ce`) 은 모두 이런 실패 기록이다. 앞이 각각 `+0x228 JS` · `+0x483 JS` · `+0x432 JS`.
- PE_emulator 확인거리 (우리 대기열 아님): 엔진에서 메인 스레드가 `CreateThread` 를 부를 때 이 함수 (진짜 ntdll 코드) 가 무엇을 돌려주는지 보면 된다.
  - 실패 → 스레드 없음.
  - 성공 + 핸들 ≠ 0 + 비트 0 꺼짐 → 체크 칸 1 길 (`+0x245 ~ +0x24d`, `+0x4aa ~ +0x4b8`).

#### 조건 (가) 의 뜻 — 결과서 `[추측]` 에서 `[정적]` 으로, 단계별로

결과서는 이 칸을 "서버끼리 호출 표시라, 서버가 아니면 0 일 것 `[추측]`" 으로 뒀다. flow-writer 는 **앞 바퀴 둘이 이미 답했다** 고 봤다 (보탠 것 ①).

1. 근거 `[정적]` (run68, sast/63): kernelbase 시작 함수가 `CsrClientConnectToServer` 다섯째 인자로 지역 칸 `&[RSP+0x30]` 을 넘긴다 (`_KernelBaseBaseDllInitialize +0x2c4`, 앞서 `+0x29` 에서 0). 돌아와서 `+0x326 CMP` · `+0x337 SETNZ` 로 그 값을 `KernelBaseGlobalData+0x4` 에 옮긴다.
2. 근거 `[정적]` (run85, sast/82): ntdll `CsrClientConnectToServer` 는 그 칸에
   - **클라이언트 길** 이면 0 (`+0xc7 MOV byte [RCX],0`),
   - **서버 길** 이면 1 (`+0x1cb MOV byte [RAX],R15B`, `R15 = 1`) 을 쓴다.
3. 근거 `[정적]`: 서버 길 = `CsrClientProcess == 0`. 그렇게 되는 건 둘이다.
   - exe `Subsystem` (NT 헤더 `+0x5c`) 이 1 (NATIVE) 일 때 (`+0x81 CMP` · `+0x86 JZ` → `+0x1df` → `+0x97 JZ +0x137`),
   - 또는 이미 `CsrServerApiRoutine` 이 선 csrss 안일 때 (`+0x125`).
4. 근거 `[정적+샘플]`: test.exe `Subsystem` = 3 (flow-writer 가 `sample/bin/test.exe` 를 직접 읽음, run68 결과서와 같음).
5. 그래서 → test.exe 는 클라이언트 길 → `KernelBaseGlobalData+0x4` = **0** → 조건 (가) 는 **거짓** `[정적+샘플]`.
6. 뜻 = "이 프로세스가 csrss 같은 **서버 프로세스인가**" `[정적]`. 이름 (`CalledFromServer` · `ServerToServerCall`) 만 `[추측: 이름]`.

- [run85 해설 2.11](run85-user32-init-해설.md) 의 user32 `gfServerProcess` 와 **같은 다섯째 인자** 다. kernelbase 도, user32 도 인사팀 사무실에 전화를 걸면서 "우리 가게가 사무실 자신이냐" 는 답을 자기 수첩에 받아 적는다.
- 비유: KERNELBASE 수첩의 `+0x4` 칸 = "**이 가게가 인사팀 사무실 자신인가**". 보통 가게는 0.
- 짐 지도 7.6.8 `KernelBaseGlobalData +0x4` 줄은 이미 `[정적]` 이었다. 이번엔 **뜻** 과 **읽는 곳** (`CreateRemoteThreadEx +0x22e` · `+0x3a5`) 이 붙었다.

#### 그래서 test.exe 의 체크 칸은

- (가) 0 `[정적+샘플]`, (나) 은퇴까지 0 `[관찰]`, (다) 모름.
- → **남은 건 (다) 하나.** `RtlQueryInformationActivationContext` 가 test.exe 메인 스레드에서 무엇을 돌려주는지는 정적으로 못 정한다 → Q42 동적 1.
  - 은퇴 때 메인 스레드 지침서 더미는 비어 있었다 `[관찰]` (2.12). 그래도 "더미가 비었을 때 이 함수가 무엇을 돌려주나" 는 안 봤다.

### 2.8 ② 말뚝 창구 (클래스 `0x29`) — 정말 말뚝만 박는다

#### 먼저 — 어떻게 그 창구를 찾았나: 점프 표

- `NtSetInformationProcess` (rva `0x9329b0`, 19144바이트) 는 [신청 종류 번호](run51-eprocess-later-해설.md) (정보 클래스) 마다 갈래가 있다. 그중 `0x29` 갈래만 보면 된다.
- **새 말 — 점프 표**: 번호를 칸 번호로 써서 표에서 "갈 곳" 을 꺼내 바로 뛰는 코드 꼴. 비교를 길게 늘어놓는 대신 쓴다.
  - 비유: 건물 로비의 **층별 안내판**. "민원 `0x29` 번 → ○층 ○호" 를 표에서 찾아 바로 간다.
- 여기서는 `+0x25b LEA EAX,[RDX-1]` ~ `+0x284 JMP RDX` 가 바이트 표 (rva `0x935914`) → 4바이트 표 (rva `0x935800`) 를 거친다.
- `.asm` 엔 "어느 번호가 어디로 가는지" 가 안 나온다. 결과서가 임시 스크립트로 PE 에서 직접 풀었다 (레포 밖, 읽기 전용).
  - → **클래스 `0x29` → `+0x485`**. 이 목적지로 오는 번호는 `0x29` 하나다 `[정적]`.

#### 창구 안 검사 — 정보 칸 40바이트 `[정적]`

`+0x485 ~ +0x528`. 들어오면 먼저 지역 40바이트를 0 으로 비우고 이렇게 본다 (run 문서 표):

| 칸 | 뜻 | 어기면 |
|---|---|---|
| (핸들) | **-1 (현재 프로세스) 만** (`+0x49f`) | `0xC000000D` |
| (길이) | `0x28` (`+0x4ac`). `0x18` 이면 옛 꼴 `{+0 크기, +8 ZeroBits, +0x10 출력}` (노드 0, `+0xe0b`) | 그 밖은 `0xC0000004` |
| `+0x0` (4B) | 노드 | `> 0x40` 이면 `0xC000000D` (`+0x4bf ~ +0x4c6`) |
| `+0x4 ~ +0xf` | 비워 둔 칸 — 모두 0 이어야 | `0xC000000D` (`+0x4cc ~ +0x4d8`) |
| `+0x10` (8B) | 예약 크기 `M` | 0 이면 `0xC000000D` (`+0x4de` · `+0x4e2 ~ +0x4e8`) |
| `+0x18` (8B) | ZeroBits | (0 이 아니면 아래에서 `MiValidateZeroBits`) |
| `+0x20` (8B) | 출력 = 예약 시작 주소 `A` | 부르기 전에 0 으로 (`+0x4f6`) |

- 칸 **이름** 은 공개 헤더 꼴 `{PreferredNode, Reserved[3], ReserveSize, ZeroBits, StackBase}` 에 맞춘 `[추측: 이름]`. 위치 · 검사는 `[정적]`.
- 권한 검사는 없다. 자기 가게 (-1) 만 건드리니까.
- 요청서는 "`+8` 크기" 로 적었는데 **`+0x10`** 이 맞다 (`+0x4de ADD R15,0x10` · `+0x4e2`). run74 문서 · flow.md · 짐 지도는 처음부터 `+0x10` 이라 고칠 곳이 없다 — 요청서만의 오기다 (보탠 것 ②).

#### 누가 불렀나에 따라 — 이전 모드

- **새 말 — 이전 모드**: 이 시스템 콜을 부른 쪽이 유저였나 커널이었나를 적어 둔 값. 유저가 불렀으면 커널은 넘겨받은 칸을 **의심하고** 복사 · 검사한다.
  - 비유: 창구 직원이 "이 서류, 손님이 직접 냈나, 사무소 안 동료가 넘겼나" 를 본다. 손님 서류면 복사본을 떠서 그걸로 일한다.
- 이 갈래에선 `+0x4b6 TEST DIL,DIL` (`DIL` = 이전 모드) 로 가른다 `[정적]`:
  - **유저가 부르면** (ntdll 목수, 파이버 길 — 2.10):
    - 점프 표 앞 사다리 (`+0x359 CMP EBX,0x29` → `+0x429`) 에서 칸이 8바이트 정렬이고 `칸 + 길이 ≤ 0x7fffffff0000` 인지 먼저 본다 (`+0x429 ~ +0x44c`).
    - 칸 40바이트를 지역으로 복사해 읽고, 끝에 출력만 사용자 칸에 되쓴다 (`+0x116d ~ +0x11a0`, `+0xff1`).
  - **커널이 부르면** (커널 목수 `RtlCreateUserStack +0x104`, 둘째 스레드 길): 이 복사 · 검사를 **건너뛴다**.

#### 부르는 담당 — `MmAllocateUserStack` `[정적]`

- `+0x50e` (ret `+0x513`) `MmAllocateUserStack(출력 칸, ZeroBits, &크기, 노드, 파티션 0)`.
- 실패면 그대로 돌려준다. 성공이면 (유저 모드일 때) 출력을 사용자 칸에 복사한다 (`+0xff1/+0xff5`).

#### 말뚝만 — 세 걸음 `[정적]`

1. `MmAllocateUserStack +0xcb` — 할당 종류 = `0x2000` (`MEM_RESERVE`) **하나** (2.2 줄 3).
   - 같이 넘기는 것: 보호 `4` (`PAGE_READWRITE`, 파티션 0 일 때), 프로세스 -1 (`+0x9a`), 원하는 시작 0.
   - `+0xd8` (ret `+0xdd`) `MiAllocateVirtualMemoryPrepare` 가 이 값들을 정리한다.
2. `+0x12f` (ret `+0x134`) `MiAllocateVirtualMemory` 가 실행한다. 그 안:
   - `+0x125` 할당 종류를 읽고 → `+0x12c TEST R13D(=0x2000),EAX` → `+0x12f JNZ → +0x31e`
   - → **`+0x334` (ret `+0x339`) `MiReserveUserMemory`**.
   - `MEM_RESERVE` 비트가 켜졌으니 예약 쪽으로 간다. 커밋 쪽으로는 안 간다.
3. 성공하면 `MmAllocateUserStack +0x157 ~ +0x15c` 시작 주소 → 출력 칸, `+0x15f ~ +0x163` 실제 크기 → 크기 칸.

- `MiReserveUserMemory` **본문은 안 읽었다** (이름만). 시작 주소를 실제로 고르는 곳이 여기다 → Q168 ①.

#### 정렬과 한도 `[정적]`

- **시작은 64KB 정렬**: `MiAllocateVirtualMemoryPrepare +0x10d MOV R8D,0x10000` → `+0x1bf` 정렬값 칸에 `0x10000`. 시작 주소는 `+0x490 ~ +0x495` 에서 64KB 로 내린다.
- **크기는 4KB 올림만**: `+0x4ba ~ +0x4c1`. 64KB 로 올리는 줄은 여기 없다 — 목수가 이미 올려서 온다 (run74 다듬기 ④).
- **위 한도**: ZeroBits 가 0 이면 `min(_EPROCESS.HighestUserAddress (+0x368) − 1, 0x7ffffffeffff)` (`+0x517 ~ +0x528`). 0 이 아니면 `+0x374 MiValidateZeroBits` 뒤 `−1 >> ZeroBits`.
  - **새 말 — ZeroBits**: "주소 맨 윗 비트 몇 개는 0 이어야 한다" 는 요구 (결과서: 보통의 "윗 비트 몇 개는 0" 뜻). 0 이면 따로 바라는 것 없음.
    - 비유: "너무 높은 번지는 싫어요" 쪽지.
- **노드**: `+0x560 ~ +0x567` 에서 `KeNumberNodes` 보다 크면 `0xC000000D`. 노드가 시작 주소 고르기에 어떻게 쓰이는지는 `MiReserveUserMemory` 안이라 안 봤다.

- 비유: 말뚝 창구는 **64KB 눈금이 그어진 땅** 에서만 말뚝 박을 자리를 고른다. 땅 크기는 4KB 단위로 맞춘다. 가게 땅 끝 근처 너무 높은 번지는 안 쓴다.

#### 사진과 대 보면 `[관찰]` + `[정적]`

- run44 K2 첫 스레드 `DeallocationStack` = `0x9b2fc00000` (`ms/logs/04_baggage-read_K2.txt:1856`).
- 아래 16비트가 0 — **64KB 정렬** 이다. 정렬값 `0x10000` 과 어긋나지 않는다.
- 단 한 번 본 값이라 "늘 그렇다" 는 아니다 (run 문서).

#### 왜 이제 `[정적]` 이라고 하나 — run74 와 나란히

[run74 해설](run74-thread-create-해설.md) 2.5 (바) 는 이렇게 짐작했다:

1. 함수 (목수) 어디에도 `MEM_RESERVE` 가 없다.
2. 그런데 뒤에서 `A + M − C` 를 커밋하고, 실패하면 `A` 를 `MEM_RELEASE` 로 푼다.
3. 그래서 이 줄이 예약한다고 **읽을 수 있다**.
4. 아직 확인 아님: `ZwSetInformationProcess` 본문을 안 봤다 → `[추측]`.

이번엔 4번 자리를 직접 봤다:

- 근거 `[정적]`: 말뚝 창구 `+0x485` → `MmAllocateUserStack` 의 할당 종류가 `0x2000` 하나 (`+0xcb`).
- 근거 `[정적]`: `MiAllocateVirtualMemory +0x12f` 가 그 비트를 보고 `MiReserveUserMemory` 로 간다.
- 그래서 → **"예약만 한다"** `[정적]`.
- 남은 `[추측]` 은 둘이다:
  - `ZwSetInformationProcess` → `NtSetInformationProcess` 사이 (시스템 콜 길, 줄 안 봄).
  - 클래스 이름 `ProcessThreadStackAllocation`.

#### 이름 — PDB 에 없다

- ntoskrnl · ntdll · kernelbase · kernel32 PDB 어디에도 `PROCESSINFOCLASS` 열거형도, `ProcessThreadStackAllocation` 글자도 없다 (결과서, `llvm-pdbutil` ENUM 4377개).
- [run51 해설](run51-eprocess-later-해설.md) 2.2 와 같은 사정이다. 번호와 동작은 `[정적]`, 이름은 `[추측: 이름]`.

#### PE_emulator 에게

- 클래스 `0x29` = "현재 프로세스 유저 공간에 `[A, A+M)` 을 `MEM_RESERVE` 로 잡고, `A` 를 칸 `+0x20` 에 돌려준다" 로 구현하면 된다 (결과서 판단 그대로). `A` 는 64KB 정렬, 위 한도 안, ZeroBits 지킴.
- 커밋 · 가드는 목수가 `ZwAllocateVirtualMemory` 로 따로 한다 (run74).
- 확인거리 (우리 대기열 아님): 엔진의 `NtSetInformationProcess(0x29)` 가 **커널 모드 호출** (커널 목수) 과 **유저 모드 호출** (ntdll 목수, 파이버) 을 둘 다 받는지 — 위 정보 칸 표와 검사 그대로.

### 2.9 ④ 노드 갈래 — "노드가 있으면" 이 아니라 "선호 자리 쪽지가 있으면"

#### run74 가 남긴 질문

- [run74 해설](run74-thread-create-해설.md) 2.5 (바) 끝: 목수에게 가는 `R9` 위 8비트가 **선호 노드** 다. 보통 0 일 것 `[추측]`.
- 그 값은 `PspAllocateThread +0x8c7` 에서 온다. 요청서는 그 근처를 "노드 ≠ 0 일 때 갈래" 로 불렀다.

#### 답 — 들어가는 문이 다르다 `[정적]`

**"노드 ≠ 0" 갈래가 아니라, 이상적 프로세서 속성 (`0x3000e`) 이 있을 때 타는 곳이다.**

- 노드 = [run10 해설](../02_sprint/run10-allocate-process-해설.md) 3.4 표의 "상가 어느 **구역**" 의 그 구역.
- 이상적 프로세서 = [run54 해설](run54-teb-fill-해설.md) 2.9 의 직원 **선호 자리**. 친화도 = **앉아도 되는 자리 목록**.
- **새 말 — `KiProcessorBlock`**: CPU 번호를 넣으면 그 CPU 의 [PRCB](run75-thread-switch-해설.md) (계산대 장부) 주소를 꺼내 주는 표. 이름은 결과서가 PDB 로 확인했다.
  - 비유: 계산대 번호 → 그 계산대 장부가 어디 꽂혀 있는지 적은 **장부 꽂이 목록**.

들어가는 길. 오프셋 순이 아니라 **실행 순** 이다 — 이 근처는 `.asm` 에서 덩어리가 떨어져 있다 ([부록 보고 오기](../02_sprint/run12-insert-thread-해설.md#23-오프셋-순서와-흐름-순서가-다르다)).

1. `+0x165 TEST RBX,RBX` — `ctx` (쪽지 읽은 결과) 가 NULL 이면 통째로 건너뜀.
2. `+0x170 MOV R8,[RBX+8]` — 받은 속성 번호 비트.
3. `+0x174 BT R8,0xc` — 그룹 친화도 (`0x3000c`) 쪽지가 있나.
4. `+0x17f MOV ESI,0x4000` · `+0x18b TEST RSI,R8` — 이상적 프로세서 (`0x3000e`) 쪽지가 있나.
   - 있으면 `+0x18e JNZ → +0xa03` 에서 쪽지에 적힌 CPU 번호 (`ctx+0x14`) 를 읽고 → `+0xa06` `KiProcessorBlock` 에서 그 CPU 의 PRCB 를 꺼낸다.
5. `+0x1b6 TEST RCX,RCX` · **`+0x1b9 JNZ → +0x886`** — PRCB 를 꺼냈으면 (= 쪽지가 있었으면) 노드 갈래로.

#### 갈래 안 `[정적]`

- 친화도 쪽지가 **없으면** (`+0x886 TEST RDX,RDX`):
  - 친화도 비트를 지어 넣고 (`+0x88f BTS R8,0xc` · `+0x894`),
  - 그 CPU 의 그룹 전체를 친화도로 채운다 (`+0x898 ~ +0x8b5`).
  - 비유: "선호 자리" 만 적어 냈으면, 관리사무소가 "앉아도 되는 자리 = 그 자리가 있는 **줄 전체**" 로 채워 준다.
- 친화도 쪽지도 **있으면**: 그룹이 같고 마스크가 겹쳐야 통과 (`+0xa5d ~ +0xa87`). 아니면 `0xC0000030`.
- 합류점:
  - `+0x8bc KeGetProcessorNodeNumber(PRCB)` (ret `+0x8c1`) — 그 CPU 가 속한 구역 번호.
  - `+0x8c1 MOVZX EDX,AX` · `+0x8c4 ADD EDX,R14D` — 이때 `R14D` 는 1 이다 (`+0x13b LEA R14D,[RDI+1]`. `+0x245 MOV R14D,EDI` 는 이 갈래 **뒤** 에서 실행된다).
  - **`+0x8c7 MOV [RSP+0x5c],EDX` = 노드 + 1.**

#### 값이 가는 곳 — 그리고 왜 "+1" 인가

- 가는 곳 `[정적]`: `+0x5c6` · `+0x5d2` → `PspSetupUserStack` 5번째 인자 → `R9 = 0x1000 | (값 << 56)` (run74) → 목수 정보 칸 `+0` → `MmAllocateUserStack` → `MiAllocateVirtualMemoryPrepare +0x560 ~ +0x567`.
- **값은 1부터, 0 = 선호 없음** `[정적]`.
- 왜 +1 일까 — 이렇게 하면 0 을 "아무 구역이나" 자리로 비워 둘 수 있다. 구역 번호 그대로 넘기면 "0번 구역 원함" 과 "선호 없음" 이 같은 값이 된다 `[추측]` (이 해설의 풀이. 근거는 "1부터, 0 = 선호 없음" 뿐).

#### test.exe — 0 `[정적]`

- 쪽지 두 장 (2.6) 에 이상적 프로세서가 없다 → `RCX = 0` → 갈래 안 탐 → `[RSP+0x5c] = 0` (`+0x11d` 에서 넣은 처음 값) → `R9 = 0x1000`.
- run74 해설 2.5 (바) 끝의 "보통 0 일 것 `[추측]`" 이 test.exe 에선 닫혔다.
- 첫 스레드 (`NtCreateUserProcess` 길) 도 이 속성이 없어 0 이다 (flow.md 2.1).
- 실행 확인용: `nt!PspAllocateThread+0x886` 은 **안 서야** 맞다 (Q42 동적 4).

### 2.10 ⑤ ntdll 의 목수 — 같은 솜씨, 다른 손님

#### 새 말 — 파이버

- **파이버 (fiber)**: 스레드 하나 안에서 프로그램이 **직접** 갈아타는 작은 실행 흐름으로 알려져 있다. 흐름마다 자기 스택을 따로 가진다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 직원 한 명이 책상에 **작업판 여러 개** 를 두고, 스스로 판을 바꿔 가며 일하는 것. 관리사무소는 판 바꾸는 걸 모른다.
- 판마다 작업대 (스택) 가 필요하다. 그 작업대를 **유저 쪽에서 직접** 짠다. 그래서 가게 관리실 (ntdll) 에도 목수가 있다.

#### 같은 꼴이다 `[정적]`

0 인 쪽만 헤더로 (`CMOVZ`), 다듬기 ① ~ ⑤, `NtSetInformationProcess(-1, 0x29, 칸, 0x28)` 예약, 커밋 `4`, 가드 `0x104` 3쪽, `INITIAL_TEB` 채우기, 실패 때 `MEM_RELEASE` — 순서 · 상수가 커널 목수와 같다.

다른 점만 (run 문서 표):

| 항목 | 커널 목수 (run74) | ntdll 목수 |
|---|---|---|
| exe 헤더 찾기 | 붙은 프로세스의 `_EPROCESS.SectionBaseAddress` → `RtlImageNtHeader` | **PEB `ImageBaseAddress`** (`+0x74` `GS:[0x60]` · `+0x9b [R13+0x10]`) → `RtlImageNtHeaderEx(1, …)` (`+0xa3`) |
| PE32 갈래 | 있음 (4바이트 칸) | **없음** — 8바이트 칸 `+0x68` · `+0x60` 만 (`+0xb8` · `+0xc8 CMOVZ`) |
| 5번째 인자 | 안 읽음 | **할당 단위 `g`** — 0 이거나 페이지보다 작으면 `0xC000000D` (`+0x5e ~ +0x6a`), 예약 올림을 `g` 로 (`+0x113 ~ +0x120`). 커널은 `0x10000` 고정 |
| 입력 검사 | 노드 `> 0x40` 만 | 노드 + **페이지 크기 0 거절** (`+0x4d`) + `g` 검사 둘 |
| 어느 PEB | 대상 프로세스 `EPROCESS.Peb` | **지금** 프로세스 (`GS:[0x60]`) — `MinimumStackCommit` 은 `+0x123 [R13+0x318]` |
| 프로세스에 붙기 | 부른 쪽이 `KiStackAttachProcess` | 없음 (자기 가게, 핸들 -1) |
| 부르는 것 | `Zw*` (커널 안) | **`Nt*` (syscall)**: `+0x16f NtSetInformationProcess` · `+0x1db` · `+0x227 NtAllocateVirtualMemory` · `+0x277 NtFreeVirtualMemory` |

- **새 말 — 할당 단위**: 예약 크기를 몇 바이트 단위로 올릴지. 커널 목수는 64KB 로 정해져 있고, ntdll 목수는 부른 쪽이 5번째 인자로 정한다.
- 결과서는 다른 점을 넷으로 묶는다:
  1. 헤더를 PEB + `RtlImageNtHeaderEx` 로 찾고, PE32 갈래가 없다.
  2. 할당 단위로 올림 단위를 부른 쪽이 정한다.
  3. 입력 검사가 더 있다.
  4. `Nt*` syscall 을 부르고, 프로세스에 안 붙는다.
- **값 계산은 같다.**
- 비유:
  - 관리사무소 목수는 어느 가게든 **그 가게 장부** (`_EPROCESS`) 를 보고 짠다.
  - 관리실 (ntdll) 목수는 **자기 가게 안내판** (PEB) 만 본다. 그리고 사무소 창구에 **손님으로서** 말뚝 · 바닥을 신청한다 (syscall).
  - 그래서 말뚝 창구는 이 목수의 서류를 의심하고 복사본을 뜬다 (2.8 "이전 모드").

#### 누가 부르나 — 파이버 하나뿐 `[정적]`

- ntdll 안엔 부르는 코드가 없다. `refs` 에 나오는 건 데이터 참조 셋 (내보내기 표 둘 · CFG 표 하나) 뿐이다.
  - `RtlCreateUserThread` 도 이 함수를 안 부른다.
- kernelbase 는 **`CreateFiberEx +0xce`** (ret `+0xd5`, 콘센트 칸 `[0x18029ba60]`) 하나.
- System32 의 `.dll` · `.exe` 중 이 글자가 있는 건 `KernelBase.dll` · `ntdll.dll` 둘뿐이다 (결과서 `grep -l -a`).
- 그래서:
  - **유저 쪽에서 작업대를 직접 짜는 길 = 파이버 (`CreateFiberEx`)**.
  - `CreateThread` 길은 크기 둘만 넘기고, **커널 목수** 가 짠다 (2.5).
- PE_emulator: 둘째 스레드 길엔 ntdll 목수가 안 쓰인다. 파이버를 지원할 때만 필요하다.

#### 도구 덤 — 콘센트 칸으로 찾기

- `refs KernelBase.dll RtlCreateUserStack` 은 이름이 import 일 때 **엉뚱한 칸** (`0x268d80`, 이름 표 쪽) 을 돌려줬다.
- 결과서가 PE import 표로 진짜 콘센트 칸 (`0x29ba60`) 을 찾고, `refs KernelBase.dll 0x29ba60` 로 `CreateFiberEx +0xce` 를 얻었다.
- [run30 해설](../02_sprint/run30-exe-crt-start-해설.md) 의 "import 함수를 부르는 곳은 이름이 아니라 **콘센트 칸 rva** 로 찾는다" 와 같은 요령이다. 도구를 고치자는 제안은 하네스라 사용자 검토 몫이다.

### 2.11 ③ `ctx+0x10` 비트 0 — 쓰는 사람만 있고, 읽는 사람은 못 찾았다

#### 쓰는 줄 `[정적]`

- 쪽지 검사 담당 `PspBuildCreateProcessContext` 가 끝에서 "스레드 모드" 표시를 남긴다:
  - `+0x186f CMP [RSP+0x90],ESI` · `+0x1876 SETNZ DL` — 셋째 인자 ≠ 0 (= 스레드 모드) 이면 1.
  - `+0x1879 ~ +0x1882` — `ctx+0x10` 바이트의 **비트 0 만** 바꿔 저장.
- 이 함수 안 `[R12+0x10]` 은 이 두 줄뿐이다.
- 같은 바이트의 **다른 비트** 는 프로세스 길이 쓴다 (flow.md 2.1):
  - `NtCreateUserProcess` — 비트 3 켜기 (`+0xaa1`) · 비트 4 읽기 (`+0xb4d`) · 비트 2 끄기 (`+0xad1`)
  - `PspDeleteCreateProcessContext +0x7d` — 비트 2 읽기
- 비유: 쪽지 검사표 귀퉁이의 **체크 박스 한 줄**. 칸마다 주인이 다르다. 0번 칸에 체크하는 사람은 찾았는데, 그 칸을 보는 사람은 못 찾았다.

#### 훑은 곳 — 일곱 함수, 비트 0 읽는 줄 0 `[정적: 훑은 범위 안에서]`

| 함수 | `ctx` 를 어떻게 쓰나 | 비트 0 읽기 |
|---|---|---|
| `NtCreateThreadEx` | 주소만 넘김. 직접 읽는 칸은 `+0x18` 하나 (`+0x2a8`) | 없음 |
| `PspCreateThread` | NULL 검사 (`+0x15f`) 뒤 `PspAllocateThread` · `PspInsertThread` 로 넘기기만 | 없음 |
| `PspAllocateThread` | `+8` · `+0x14` · `+0x140` · `+0x148` · `+0x1e0` 만. `[RBX+0x10]` 은 파일 전체에 0줄 | 없음 |
| `PspInsertThread` | `+0x20` · `+8` · `+0x14` · `+0x170`. 첫 스레드면 `+0x7f7` 에서 `PspCallProcessNotifyRoutines` 로 넘김 | 없음 |
| (이웃) `PspDeleteCreateProcessContext` | `+0x7d TEST byte [RBX+0x10],0x4` | **비트 2** (0 아님) |
| (이웃) `NtCreateUserProcess` | 비트 3 · 4 · 2 | 비트 0 안 만짐 |
| (이웃) `PspCallProcessNotifyRoutines` | `+0x40` · `+0xb8` · `+8` · `+0xf0` · `+0xd8` · `+0x1a0` | 없음 |

#### 왜 "없다" 가 아니라 "못 찾았다" 인가

- `ctx` 를 받아 가는 더 아래 함수 (`KeInitThread` · `KeStartThread` · `MmCreateTeb` 등) 는 안 읽었다.
  - 결과서 짐작: 그쪽엔 `ctx` 전체가 아니라 `+0x140` · `+0x1e0` 같은 **부분 포인터** 를 넘기니 이 비트는 안 보일 것 `[추측]`.
- ntoskrnl 전체에서 "`[reg+0x10]` 바이트의 비트 0 시험" 줄을 훑은 것도 아니다.
- 그래서 flow.md 2.1 은 `[공백]` 으로 남겼다.
- PE_emulator: 지금은 흉내 안 내도 될 것 `[추측: 읽는 곳을 못 찾았을 뿐]`.
- 더 볼지 → Q168 ② (4절).

### 2.12 사진 대조 — run44 로그 다시 읽기 (새 실행 없음)

둘째 스레드는 `main` 뒤라 사진 범위 밖이다. 대신 **같은 함수를 쓰는** 첫 스레드와 메인 스레드 칸만 다시 읽었다 (flow-writer).

| 무엇 | 값 `[관찰]` | 어디 | 무엇과 맞춰 봤나 |
|---|---|---|---|
| 첫 스레드 `DeallocationStack` (예약 시작 `A`) | `0x9b2fc00000` — 아래 16비트 0 | `ms/logs/04_baggage-read_K2.txt:1856` | 64KB 정렬 (2.8) — 어긋나지 않음 |
| 메인 TEB `SubProcessTag` (`+0x1720`) | (null) | `_K2.txt:1866` · `_P2.txt:2750` | 조건 (나) — 은퇴까지 거짓 |
| 메인 지침서 더미 (TEB `+0x2c8` → `0x9b2fa67290`) | 첫 8B (`ActiveFrame`) 0, 틀 목록 빈 리스트 | `_P2.txt:4567 ~ 4569` | 조건 (다) — 은퇴 때 올린 틀 없음. 그래도 (다) 의 값은 이것만으론 못 정함 |

- 셋 다 "어긋나지 않는다" 까지다. 둘째 스레드를 만드는 **그 순간** 의 값은 Q42 에서 본다.

### 2.13 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

| # | 무엇 | 이 글 어디서 |
|---|---|---|
| ① | `KernelBaseGlobalData+0x4` 의 뜻: 결과서 `[추측]` → `[정적]` (run68 · run85 줄 + test.exe `Subsystem` 3) | 2.7 "조건 (가) 의 뜻" |
| ② | 요청서 "정보 칸 `+8` 크기" 는 오기 — **`+0x10`**. 다른 문서는 처음부터 `+0x10` | 2.8 |
| ③ | CreateFlags 1 이어도 `SameTebFlags` 안 씀 (`R12W` 에 `0x1` 없음) | 2.7 |
| ④ | `RtlQueryInformationActivationContext` 실패 → 스레드 안 만듦. `DbgPrint` 셋은 실패 기록 | 2.7 |
| ⑤ | test.exe import 표 `KERNEL32.dll!CreateThread` | 2.3 |
| ⑥ | 사진 셋 | 2.12 |

- 결과서가 요청 범위 밖으로 더 뽑은 것 (`MiAllocateVirtualMemoryPrepare` · `MiAllocateVirtualMemory` · `CreateThread` 둘 · refs) 은 그대로 받아 썼다.
- 디버거가 붙어서 달라지는 점: 이번에 읽은 줄 안엔 디버거 여부를 보는 줄이 없다 `[정적: 읽은 줄 안에서]`. `CreateThread` 는 `NtCreateThreadEx` CreateFlags `0x4` (디버거에서 숨김) 를 안 켠다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 | ntkrnlmp · kernelbase · kernel32 · ntdll pdb 줄 … | + run90 몫. 근거 로그에 run90 줄 (사진 셋, 새 실행 없음) |
| 흐름도 앞 글 | … run89 문단 | + run90 문단: **그림을 안 고쳤다** (둘째 스레드는 지도 밖, run74 와 같은 까닭) |
| 2.1 머리 | … run79 | + run90 |
| 2.1 `PspBuildCreateProcessContext` 절 | — | 새 줄: `ctx+0x10` 비트 0 ← 스레드 모드, 같은 바이트 다른 비트의 주인, **읽는 줄 못 찾음 `[공백]`** |
| 2.1 `PspAllocateThread` 절 | `CreateFlags = 0` 이면 `+0x793` 건너뜀 | + `R12W` 를 만드는 비트 (`0x1` 없음) → 1 이어도 같음. + 새 줄 **노드 갈래** (이상적 프로세서 → 노드 + 1) |
| 2.1 `RtlCreateUserStack` 절 1 | 노드 > `0x40` 이면 거절 | + 노드 값의 출처 (속성 있을 때만 노드 + 1, 아니면 0) |
| 2.1 `RtlCreateUserStack` 절 4 | 예약 `[추측: 본문 안 봄]` | ~~`[추측: 본문 안 봄]`~~ → **`[정적]` 예약만** + 사슬 · 정렬 · 한도 · 검사, 첫 스레드 `A` 64KB 정렬 `[관찰]` |
| 2.1 `RtlCreateUserStack` 절 끝 | — | 새 줄: ntdll 같은 이름 = 같은 꼴, 부르는 곳 `CreateFiberEx` 하나 |
| 2.5 머리 | … run88 | + run90 |
| 2.5 csrss 연결 절 | — | 새 줄: `KernelBaseGlobalData +0x4` 의 **뜻** (서버 프로세스 표시) · test.exe 0 · 읽는 곳 |
| 3장 스레드 출발 절의 `NtCreateThreadEx` 길 두 줄 | 유저 `CreateThread` → `NtCreateThreadEx` 는 `[추측]` | → (run90 `[정적]`) `test!Worker` 쪽은 닫힘. ntdll 일꾼은 그대로 `[추측]` |
| 3장 "둘째 스레드를 만드는 커널 길" 머리 · 나무 첫 줄 | `… → ntdll!NtCreateThreadEx (syscall) [추측] 유저 쪽 길은 안 봄` | `kernel32!CreateThread +0x3d → CreateRemoteThreadEx +0x2e9 → ntdll!NtCreateThreadEx` `[정적]` (스텁 → syscall 은 `[추측]`) |
| 3장 같은 절 스택 줄 | (0, 0) 을 넘긴다면 `[추측]` | ~~`[추측]`~~ → `[정적]` (`+0x280 ~ +0x291`) |
| 3장 같은 절 동적 줄 | Q42 몫 | + 정적으로 닫힌 셋 · 새로 더해진 셋 |
| 3장 **새 하위 절** | — | **"유저 쪽 — `kernel32!CreateThread` → `KERNELBASE!CreateRemoteThreadEx` → `NtCreateThreadEx`"**: 나무 · 넘기는 것 · CreateFlags 조건 셋 · 나머지 속성 셋 · 끝길 · 비유 |
| 4장 run74 동적 줄 (Q42) | 3 · 5 · 6 열림 | 각 자리에 "(해결됨, 정적 → run90)" |
| 4장 run74 정적 줄 (Q142) | ① ~ ⑤ | ① ② ④ ⑤ 해결됨, ③ **남음 `[공백]` → Q168 ②** |
| 4장 새 줄 셋 | — | Q42 동적 1 ~ 4 · 정적 Q168 ① · ② · PE_emulator 쪽 / 다르게 적은 곳 / 보탠 것 |
| 5장 | … run89 | + run90 (run 표 · 요청서 번호 표) |

태그가 바뀐 곳 (한 줄씩):

- 유저 `CreateThread` → `NtCreateThreadEx` (run11 · run74): `[추측]` → **`[정적]`**. kernel32 콘센트 → KERNELBASE 풀이만 `[추측]`.
- `CreateThread(…, 0, …)` 가 (0, 0) 을 넘기나 (run74): `[추측]` → **`[정적]`**.
- 클래스 `0x29` 가 예약인가 (run74): `[추측: 본문 안 봄]` → **`[정적]`**.
- 목수 `R9` 위 8비트 (노드) "보통 0" (run74): `[추측]` → **`[정적]`** (속성 있을 때만 노드 + 1, test.exe 0).
- `KernelBaseGlobalData+0x4` 의 뜻 (sast/77 결과서): `[추측]` → **`[정적]`**.
- ntdll `RtlCreateUserStack` 이 같은 꼴인가 (run74): `[추측]` → **`[정적]`**.
- 그대로인 것: ③ `[공백]`, 클래스 · 인자 이름 `[추측: 이름]`, Zw → Nt 시스템 콜 길 `[추측]`.

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **마흔여섯째판** — 새 짐 칸 없음, 칸 수 · 2장 숫자 그대로.
  - 주제가 둘째 스레드라 은퇴 뒤 (범위 밖) 다. 같은 함수를 쓰는 **첫 스레드** 줄의 근거만 고쳤다.
- 3.3 `DeallocationStack` ① 줄:
  - `A` 출처 ~~`[추측: 예약, 본문 안 봄]`~~ → `[정적]` (`MmAllocateUserStack` → `MiReserveUserMemory`, 64KB 정렬, 사진도 정렬).
  - 경로 칸에 `A` 사슬: `… → ZwSetInformationProcess (호출 +0x104) → (시스템 콜) NtSetInformationProcess → MmAllocateUserStack (호출 +0x50e) → MiAllocateVirtualMemory (호출 +0x12f) → MiReserveUserMemory (호출 +0x334)`.
  - 근거 칸에 `sast77 · run90`.
- 3.1 `MinimumStackCommit` ① 줄: 읽는 곳에 ntdll 목수 `+0x123` (파이버 길) 을 더함.
- 7.6 C8 유저 스택: 예약 확정 · 둘째 스레드 (0, 0) 한 마디. 시작 주소 고르기는 Q168 ①.
- 7.6.8 `KernelBaseGlobalData +0x4`: 뜻 · 1 이 되는 길 · 읽는 곳.
- 기준 run90 줄 · 판 목록 · 8장 두 표.
- 짐 지도 규칙 ("줄기 위에서는 끝까지") 으로 보면:
  - run74 해설 3절은 "꼬리표 하나 남음 — 예약은 `[추측: 예약]`" 이었다. 그 꼬리표가 떨어졌다.
  - 유저 스택 줄기는 이제 `MiReserveUserMemory` 앞까지 코드로 닿았다. 그 안 (시작 주소 고르기) 만 이름 수준이다 (Q168 ①).

### run74 문서 · 해설도 같이 고쳐 읽기

- flow-writer 가 [run74 문서](run74-thread-create.md) 에 화살표를 붙였다: ntdll `RtlCreateUserStack` 줄 끝, "정적 — 다음 바퀴 후보" 네 줄 끝에 "(→ run90 …)".
- 이 해설을 쓰면서 [run74 해설](run74-thread-create-해설.md) 맨 위에 **"고쳐 읽기 (run90)"** 한 줄을 붙였다. run74 해설의 `[추측]` 중 이번에 닫힌 자리 (유저 사슬 · (0, 0) · 예약 · 노드 0 · 쪽지 수 · Q142 ④ ⑤) 와, 아직 `[추측]` 인 자리 (2.6 (바) `+0x2e0`) 를 이 글로 가리킨다.

### 큰 그림에서

- **지도 쪽**:
  - 그림은 그대로다.
  - 둘째 스레드 만들기가 이제 **`main` → 앞 접수대 → 대행 책상 → 창구 → 인사 카드 담당 → 목수 → 말뚝 창구** 까지 한 줄로 이어진다.
  - 빈칸은 둘 정도다: `MiReserveUserMemory` 안 (주소 고르기), 체크 칸 조건 (다) 의 실제 값.
- **PE_emulator 쪽**:
  - run74 "지금 우리" 가정 둘이 닫혔다 — "스택 크기 0 → exe 헤더" 는 **유저 쪽까지 맞다**, "속성 `CLIENT_ID` · `TEB_ADDRESS` 둘" 은 `CreateThread` 길엔 **딱 맞다**.
  - 클래스 `0x29` 는 "예약만, 64KB 정렬, `A` 는 `+0x20`" 으로 구현.
  - 엔진은 체크 칸 **0 을 받는 게 보통** 이다. 1 이 와도 `SameTebFlags` 는 같고, `NtResumeThread` 가 하나 더 온다.
- **다음**: 사용자 지시 묶음이 끝났다 → **[멈춤]**. Q104 VM 단계 앞에서 사용자 확인을 기다린다.

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 남은 것은 두 무리다.

- **동적** → **Q42** (run74 동적 1 ~ 6 과 같은 실행). 대상은 test.exe 둘째 스레드 하나.
  - 공통 조건: 현재 프로세스 = test.exe, 그리고 StartRoutine = `test!Worker`.
  - run74 동적 중 정적으로 닫힌 몫: 3 의 "유저가 (0, 0) 을 넘겼나" (예), 5 의 "예약인가" (예 — `A` 모양만 남음), 6 의 "`AttributeList` 가 NULL 인가 · 항목 수" (NULL 아님, 2). 멈춤점 자체는 값 대조용으로 그대로 둔다.
- **정적** → 새 **Q168** (선택 · 작음).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 의 CreateFlags 가 0 인가 1 인가 — 조건 (다) 의 값 | `RtlQueryInformationActivationContext` 가 돌려주는 값은 실행 때 값. 은퇴 때 더미가 빈 건 봤지만, 그때 이 함수가 무엇을 돌려주는지는 안 봄 | **Q42 동적 1** (`KERNELBASE!CreateRemoteThreadEx+0x218` `r eax` · `dq @rsp+0x118 L2`, `+0x22e` 에서 `db kernelbase+0x3aeec4 L1` · `dq @$teb+0x1720 L1` — 둘 다 0 기대) |
| 조건 (나) `SubProcessTag` 가 `main` 에서 `CreateThread` 까지도 0 인가 | 은퇴 뒤 코드는 안 봄 `[추측: 0 그대로]` | **Q42 동적 1** (위 `+0x22e` 줄) |
| `NtCreateThreadEx` 에 실제로 가는 인자 전부가 정적 답과 같은가 | 실행 때 값 | **Q42 동적 2** (`+0x2e2` 에서 `r ecx` · `dq @rsp+0x20 L7` · `dd poi(@rsp+0x50) L1` (`0x48`) · `dq poi(@rsp+0x50)+8 L8`, `nt!NtCreateThreadEx+0x70` 의 `r13d` · `rdi` 와 맞나) |
| 예약 시작 `A` 의 실제 모양 (64KB 정렬인가, 크기 `M`) | 시작 주소를 고르는 `MiReserveUserMemory` 본문을 안 읽음. 정렬은 첫 스레드 한 번만 봄 | **Q42 동적 3** (`nt!NtSetInformationProcess+0x485` · `nt!MmAllocateUserStack+0xd8` · `+0x134` + `!address`) |
| 노드 갈래를 정말 안 타나 | 정적 답 (`0x3000e` 없음) 의 실행 확인 | **Q42 동적 4** (`nt!PspAllocateThread+0x886` 이 **안 서야** 맞다) |
| `MiReserveUserMemory` 가 시작 주소를 어떻게 고르나 (아래/위 방향, 64KB 칸 고르기, 노드 선호) | 본문 안 읽음 (이름만) | **Q168 ①** (정적, 선택 — 동적 3 이 이상하게 나올 때만) |
| `ctx+0x10` 비트 0 을 누가 읽나 | 훑은 일곱 함수 밖 (`KeInitThread` · `KeStartThread` · `MmCreateTeb` 등, 또는 ntoskrnl 전체 바이트 비트 시험) 을 안 봄 | **Q168 ②** (정적, run 문서 · flow.md 같음) |
| kernel32 콘센트 칸 (`api-ms-win-core-processthreads-l1-1-0`) 이 정말 KERNELBASE 로 풀리나 | 이번엔 따로 안 풀어 봄 `[추측]` | 대기열 항목 없음 |
| `BasepConvertWin32AttributeList` 가 넣는 항목 | 본문 안 봄 (`CreateThread` 는 안 탐) | 대기열 항목 없음 |
| `ZwSetInformationProcess` → `NtSetInformationProcess` 시스템 콜 길 | 줄 안 봄 `[추측: Zw 스텁 → 서비스 표]` | 대기열 항목 없음 |
| 클래스 `0x29` · 정보 칸 · 인자 이름 | PDB 에 없음 | `[추측: 이름]` 그대로 |

- PE_emulator 확인거리 (우리 대기열 아님):
  - 엔진에서 `RtlQueryInformationActivationContext` 가 무엇을 돌려주나 — 실패면 `CreateThread` 가 스레드 없이 끝나고, 핸들 ≠ 0 · 비트 0 꺼짐이면 CreateFlags 1 + `NtResumeThread` 길 (2.7).
  - 엔진의 `NtSetInformationProcess(0x29)` 가 커널 모드 호출과 유저 모드 호출을 둘 다 받는지 (2.8).
- 도구 제안 둘 (`.asm` 머리에 점프 표 목적지 붙이기 · `refs` 가 import 이름이면 콘센트 칸 찾아 주기) 은 하네스라 사용자 검토 몫이다 (plan.md 에 보류로 적힘).

---

## 5. 더 읽기

- run 문서: [`run90-thread-leftovers.md`](run90-thread-leftovers.md) — 맨 위 한 줄 결론, "PE_emulator 쪽에서 쓸 것" (run74 가정 표 · 클래스 `0x29` 정보 칸 표 · 확인거리), 호출 지점 표, ① 인자 표 · 조건 셋, ② 갈래 · 예약 · 정렬, ③ 훑은 표, ④ 들어가는 길, ⑤ 다른 점 표, 호출 관계 그림, 사진 대조, 결과서와 다르게 적은 곳 ① ~ ⑥, 아직 확인 필요 (Q42 · Q168)
- 결과서: [`_workspace/sast/77_thread-leftovers_result.md`](../../_workspace/sast/77_thread-leftovers_result.md) — `CreateRemoteThreadEx` 인자 자리 표, (a) ~ (e), 한 줄 답 표, 클래스 `0x29` 갈래 본문 표, `MmAllocateUserStack` 인자, ③ 함수별 표, ④ 갈래 안, ⑤ 비교표 · callers, 동적 확인 · 추천 멈춤점. (조건 1 의 "서버가 아니면 0 일 것 `[추측]`" 은 run 문서 "보탠 것 ①" 로 고쳐 읽기)
- 요청서: [`_workspace/sast/77_thread-leftovers.md`](../../_workspace/sast/77_thread-leftovers.md) (질문 2 의 "`+8` 크기" 는 `+0x10` 으로 읽기)
- flow.md: [기준](flow.md) · 흐름도 앞 글 run90 문단 · 2.1 `PspBuildCreateProcessContext` · `PspAllocateThread` (노드 갈래) · `RtlCreateUserStack` 절 · 2.5 csrss 연결 절 · **3장 "둘째 스레드를 만드는 커널 길" 과 하위 절 "유저 쪽"** · 4장 run74 · run90 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.1 `MinimumStackCommit` · 3.3 `DeallocationStack` · 7.6 C8 · 7.6.8 `KernelBaseGlobalData +0x4`
- 앞 이야기:
  - [run74 해설](run74-thread-create-해설.md) — 둘째 직원 뽑기 커널 쪽 (주문서 · 목수 · 체크 칸 · 쪽지 묶음). **이 글의 바로 앞**
  - [run54 해설](run54-teb-fill-해설.md) 2.4 · 2.8 · 2.9 · 2.10 — 목수 · 쪽지, `SameTebFlags`, 선호 자리 · 앉아도 되는 자리, 지침서 더미
  - [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.3 — 앞 접수대 · 대행 책상, 별명이 "누가 묻나" 로 풀림
  - [run68 해설](run68-dll-init-rest-해설.md) 2.4 · [run85 해설](run85-user32-init-해설.md) 2.11 — csrss 전화, 서버 표시
  - [run88 해설](run88-sxs-redirect-해설.md) — 활성화 문맥 (설정 묶음 · 더미)
  - [run51 해설](run51-eprocess-later-해설.md) 2.2 — 정보 클래스 (신청 종류 번호), PDB 에 이름 없음
  - [run46 해설 2.7](run46-process-heap-해설.md#27-새-말--예약--확정) — 말뚝 · 바닥
  - [run12 해설](../02_sprint/run12-insert-thread-해설.md) 2.10 — 대기 팻말

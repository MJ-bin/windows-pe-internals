# run81 exception-kernel — 해설

> 이 글은 [run81 문서](run81-exception-kernel.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q148a**: PE_emulator 요청 26 의 커널 쪽 — CPU 예외를 유저 예외로 넘기기 (`_workspace/plan.md`, 이번에 **끝**)
  - Q148 (요청 26 전체) 은 시작할 때 둘로 나눴다.
    - 커널 쪽 = 이번 (**Q148a**).
    - 유저 쪽 = 다음 (**Q148b**, run82) — ntdll `KiUserExceptionDispatcher` 아래 · kernelbase `UnhandledExceptionFilter` · `RtlRaiseException`.
  - 사용자 결정 2026-11-12: 요청 26 ~ 30 이 먼저이고, 26 이 가장 급하다 (요청서: PE_emulator 첫 실험 회차 샘플 9,176 개의 병목 후보).
  - 다음 항목: **Q148b** → Q149a → Q149b → Q150 → Q151 → Q152 → [멈춤] (Q104 VM 단계 앞).
  - 이번에 남은 것은 넷으로 갈렸다 (4절):
    - 동적 → **Q104** (VM 단계)
    - 작은 정적 → 새 **Q155** (② ~ ⑦)
    - 예외를 일부러 내는 새 샘플이 있어야 하는 동적 → 새 **Q156** (할지 말지는 사용자 결정)
    - 유저 쪽에서 같이 볼 것 (①) → **Q148b**
- 근거:
  - run 문서 [`run81-exception-kernel.md`](run81-exception-kernel.md) (가장 중요)
  - 요청서 [`_workspace/sast/78_exception-kernel.md`](../../_workspace/sast/78_exception-kernel.md) · 결과서 [`78_exception-kernel_result.md`](../../_workspace/sast/78_exception-kernel_result.md)
    - 판정 **분석성공**. 여섯 질문 모두 줄로 답했다. 스크립트 문제 없음 (참고 2건).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 세 곳 · 2.3 제목과 `PspInitializeThunkContext` 6번 아래 한 줄 · 2.4 제목과 "일찍 끝나는 길" 끝 한 마디 · 3장 **새 절 "CPU 예외를 유저 예외로 넘기기"** · 4장 run80 줄 둘에 번호 + 새 줄 셋 · 5장
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음, 서른일곱째판)
  - run48 문서 맨 위 **정정** 한 줄
- 이번 바퀴는 **정적만**이다.
  - test.exe 정상 길엔 CPU 예외가 없다 `[추측]`. `sample/*.c` 에 `__try`·`RaiseException`·`DebugBreak`·`__debugbreak`·`int3`·0 나누기 글자가 없다 `[소스]`.
  - 그래서 값을 대 볼 "예외 순간 사진" 이 없다. run44 사진의 공용 게시판 (KUSD) 줄 몇 개만 다시 읽었다 (flow-writer, 새 실행 없음, 2.17).
- **이번 바퀴에서 조심할 곳 넷**:
  1. 유저 스택 표의 오프셋은 **새 `RSP` (`C`) 기준** 이다. 예외 순간의 원래 `RSP` (`S0`) 기준이 아니다. `C` 는 `S0` 에서 정렬에 따라 조금씩 다르게 떨어진다 (2.10).
  2. `CONTEXT_EX` 셋째 칸이 run 문서 표에선 `{A − X, S0 − A}`, 최소 규칙 5 에선 `{S0 − X, 0}` 이다. **XSTATE 가 없으면 `A = S0`** 라서 같은 값이다 (2.11).
  3. run 문서 "정정" 절엔 셋이 있는데, **우리 문서를 고친 건 run48 하나** 다. `Process.cpp:1953` 주석은 PE_emulator 쪽이 고칠 몫이고, run16 은 고칠 게 없었다 (2.18 · 2.19).
  4. flow.md 3장 새 절은 커널을 **"관리실"** 이라고 불렀다. 이 해설 시리즈에선 관리실 = ntdll, 커널 = 관리사무소다. 비유만 다르고 사실은 같다 (아래 "비유").

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- ntdll = 가게 안 **관리실**, KUSD = **공용 게시판**, 그 안 `ProcessorFeatures` = 기능 체크리스트 → [run70 해설 "비유"](run70-cpu-features-해설.md#비유--그대로-이어-쓴다)
- 커널 스택 = **관리사무소 안 작업대** → [run52 해설 "비유"](run52-kthread-create-해설.md#비유-그대로-이어-쓴다)
- 유저 스택 = 가게 안 **작업대** → [run11 해설 2.2](../02_sprint/run11-allocate-thread-해설.md#22-한-줄씩-읽는-법)
- CONTEXT = **업무 시작 메모**, 유저 스택 맨 위에 맡긴 메모 자리 = **사물함**, `LdrInitializeThunk` = **오리엔테이션실** → [run16 해설 2.3](../02_sprint/run16-thunk-context-해설.md#23-비유-하나로-처음부터-끝까지)
- `ContextFlags` = 메모 맨 위 **체크 칸** (`0x100001` CONTROL …) → [run80 해설 2.3](run80-thread-setcontext-해설.md) · [run42 해설 2.3](../02_sprint/run42-continue-detail-해설.md)
- XState = 메모 뒤에 붙는 **별지**, KUSD `+0x3d8` XState 칸 → [run42 해설 2.3](../02_sprint/run42-continue-detail-해설.md) · [run55 해설 2.4](run55-context-length2-해설.md)
- 트랩 프레임 = 직원 책상의 **출발 메모**, `IRETQ` = 출발 메모대로 **순간 이동** → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- 예외 프레임 (비휘발 레지스터) = 직원 **개인 서랍** → [run42 해설 "비유"](../02_sprint/run42-continue-detail-해설.md) · [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.4
- `SYSRET` = **돌아갈 문 쪽지** (`RCX`) 대로 나가는 보통 출구, **답 봉투** (`RAX`) → [run56 해설 2.10](run56-syscall-args-해설.md)
- 출구 셋 (`SYSRET` / `IRETQ`) → [run77 해설 2.14](run77-thread-wake-해설.md) (run80 정정 반영해서 읽기)
- `PsNtdllExports` = 관리사무소 **벽 주소록** → [run16 해설 2.9](../02_sprint/run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다)
- 유저 APC 를 유저 스택에 쌓는 꼴 (`KiInitializeUserApc`), `KiUserApcDispatcher` = 관리실 **쪽지 읽는 창구** (벽 주소록 `+0x28`), `ProbeForWrite` = 빈 쪽에 **찢어진 데 없나 보기** → [run42 해설 2.5](../02_sprint/run42-continue-detail-해설.md)
- 예외 = **사고**, 예외 보호 구역 (`__try`) = 사고 나면 **정해진 착지점** 으로, 비상구 → [run17 해설 2.8](../02_sprint/run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일)
- 필터 · 비상 대피도 (unwind 범위 표) → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) · [run27 해설 2.9](../02_sprint/run27-init-graph-해설.md)
- 풀어 보기 (unwind) · `UNWIND_INFO` 설명서 → [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.3, 풀림 정보 `UnwindData` → [run59 해설](run59-init-leftovers-해설.md)
- `__fastfail` = 프로세스를 바로 끝내는 장치 → [run18 해설 2.10](../02_sprint/run18-proc-init-setup-해설.md#210-일찍-끝나는-길--19--5)
- 가게 문 닫기 (`PspExitLastThread`), `NtTerminateProcess` 자기 가게 길 → [run79 해설](run79-thread-exit-해설.md) 2.13 · 2.14 (b)
- 바닥 깔기 (커밋) → [run46 해설 2.7](run46-process-heap-해설.md)
- Pico → [run11 해설 2.4](../02_sprint/run11-allocate-thread-해설.md#24-갈림길-작업대개인-안내판은-언제-만드나)
- 사진 K1 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

### 비유 — 그대로 이어 쓴다, 새로 셋

- 이 글도 앞 해설들처럼:
  - CPU = **계산대**, 스레드 = **직원**, 프로세스 = **가게**, 커널 = **관리사무소**, ntdll = 가게 안 **관리실**.
  - 예외 = 직원이 일하다 **사고** 가 난 것 (run17 해설에서 쓰던 말).
- 새로 나오는 그림 셋:
  - **사고 접수 창구** = 커널 입구 함수 (`KiPageFault` 등). 사고 종류마다 창구가 따로 있다 (2.4).
  - **사고 보고서 묶음** = 커널이 유저 스택에 한 번에 내려놓는 넷 (2.11):
    - 사고 순간 메모 (`CONTEXT`) · 목차 쪽지 (`CONTEXT_EX`) · 사고 기록 카드 (`EXCEPTION_RECORD`) · "사고 직전 위치" 표지 (머신 프레임). 있으면 별지 (XSAVE 꼬리) 도.
  - **사고 처리 창구** = ntdll `KiUserExceptionDispatcher`. 벽 주소록 `+0x20` 칸에 적혀 있다.
    - run42 의 **쪽지 읽는 창구** (`KiUserApcDispatcher`, `+0x28`) 바로 옆 칸이다.
- flow.md 3장 새 절과 다른 점 (비유만):
  - flow.md 는 커널 = "관리실", 유저 스택 = "직원 책상", 프로세스 종료 = "직원 퇴근" 으로 그렸다.
  - 이 글은 앞 해설 그림대로 커널 = **관리사무소**, 유저 스택 = 가게 **작업대**, 프로세스 종료 = **가게 문 닫기** (run79 해설) 로 쓴다.
    - "직원 퇴근" 은 run79 에서 스레드 **하나** 끝내기 몫이었다. 이번 끝내기는 가게 **통째로** 다.
  - "응급 코드" · "사고 처리 창구" 는 flow.md 와 같은 말이다. flow.md 의 "사고 경위서" = 이 글의 "사고 보고서 묶음".

### 한 줄로 먼저

- 유저 코드가 넘어졌는데 (CPU 예외) 메모리 담당·분류 담당이 그 자리에서 못 고친 사고면, 커널은 **바로 끝내지 않는다** `[정적]`.
  - 사고 보고서 묶음을 **그 직원의 가게 작업대 (유저 스택) 위에** 내려놓고,
  - 직원을 ntdll 의 **사고 처리 창구** (`KiUserExceptionDispatcher`) 로 순간 이동시킨다.
- 묶음 모양 (`C` = 새 `RSP`) `[정적]`:
  - `C+0` `CONTEXT` (0x4d0) → `C+0x4d0` `CONTEXT_EX` (0x20) → `C+0x4f0` `EXCEPTION_RECORD` (0x98) → `C+0x590` 머신 프레임 (`Rip`, `+0x5a8` 에 `Rsp`).
  - 원래 `RSP` (`S0`) 에서: `C = ((A − 0x28) & ~0xF) − 0x590`. XSTATE 가 없으면 `A = S0`.
- 커널은 출발 메모 (트랩 프레임) 의 **넷만** 바꾼다 — `EFlags` TF 지움 · `Rsp = C` · `SegCs = 0x33` · `Rip = PsNtdllExports+0x20`. 그리고 `IRETQ` `[정적]`.
- `int3` 의 사고 장소는 **`int3` 자기 자리** 다. 기록 카드 주소도, 사고 순간 메모 `Rip` 도 −1 한다 `[정적]`.
- 아무도 사고를 안 받으면 (둘째 기회), 커널이 **가게 문을 닫는다**: `ZwTerminateProcess(-1, 사고 코드)` `[정적]`. 끝 코드 = 사고 코드 그대로.
- `__fastfail` 은 사고 처리 창구에 **한 번도 안 보내고** 곧장 그 끝으로 간다 (`0xC0000409`) `[정적]`.
- 그래서 PE_emulator 의 "CPU 예외면 멈춤" 은 **틀린 게 아니라 없는 것** 이다 `[정적]`.
  - 커널 쪽 흉내 하나 (최소 규칙 9단계) 를 더하면, 그 뒤는 진짜 ntdll 이 한다 (Q148b).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → … → 첫 스레드 출발
 │
 ├─ 커널 PspInitializeThunkContext (flow.md 2.3 6번)        — 사물함에 원래 메모 맡기기 (run16)
 │    └─ 실패하면 "오류 길"                                   ← 이번: 첫 기회 0 → 가게 문 닫기 (2.18)
 │
 ├─ ntdll 로더 LdrpInitializeProcess … (flow.md 2.4)
 │    └─ "일찍 끝나는 길" 의 __fastfail                       ← 이번: 그 커널 쪽 끝 (2.16)
 │
 └─ … NtContinue 은퇴 → 진입점 → main

 (지도 밖, 아무 스레드에서나 아무 때나) 유저 코드가 넘어지면      ← 이번 본론
   CPU 예외 → 커널 입구 → KiDispatchException → 유저 스택에 보고서 묶음 → ntdll 사고 처리 창구
                                                                          └─ 그 안 → Q148b (run82)
```

- 본론은 흐름도 범위 (진입점까지) 의 **정상 길이 아니다.** 어느 스레드든 넘어지면 타는 공용 길이다.
  - 그래서 flow.md 그림엔 안 넣고, 3장에 절 하나로 적었다.
- 지도 안에서 닿는 곳은 둘이다.
  - 2.3 `PspInitializeThunkContext` 6번의 **"오류 길"** — run16 때 이름만 있고 비어 있던 곳.
  - 2.4 로더의 **`__fastfail`** 자리 — 커널 쪽에서 어떻게 끝나는지.
- test.exe 가 낼 수 있는 예외는 **소프트웨어 예외 하나** 다 `[정적]` (run71).
  - `KERNELBASE!OutputDebugStringA +0x77 RaiseException(0x40010006)` — TLS 콜백·`DllMain`·`main` 의 `Log` 마다.
  - 이게 커널을 거치는지는 유저 쪽 (`RtlRaiseException`) 몫이라 **Q148b** 다.
- 그럼 왜 봤나?
  - PE_emulator 는 은퇴 (`NtContinue`) 뒤 진짜 ntdll·kernelbase 를 Unicorn 안에서 돌린다.
  - 그런데 CPU 예외가 나면 그냥 멈춘다. 프로그램 안 `__try` 가 받아 줄 기회가 없다.
  - 이게 첫 실험 회차 샘플 9,176 개의 병목 후보라서 가장 급했다 (요청서).
  - 커널 쪽 (보고서를 쌓고 사고 처리 창구로 보내기) 은 Unicorn **밖** 의 nt 엔진이 흉내 내야 한다. 그러니 쌓는 모양이 정확해야 한다.

### 무엇이 궁금했나

한 문장으로: **유저 모드에서 CPU 예외가 나면, 커널은 유저 스택에 무엇을 어떤 모양으로 쌓고 어디로 보내나. 아무도 안 받으면 어떻게 끝나나.**

- 계기 — PE_emulator 의 지금 (요청서 "배경"):
  - CPU 예외 = Unicorn 오류 (`UC_ERR_READ_UNMAPPED` · `UC_ERR_INSN_INVALID` 등) 로 멈춤.
  - 인터럽트 훅이 없어서 `int3` · 0 나누기도 멈춤.
  - 메모리 syscall 이 잘못된 유저 포인터를 만나면 `STATUS_ACCESS_VIOLATION` 을 돌려주고 멈춤.
    - `Src/nt/Process.cpp:1953` 주석엔 "진짜는 SEH `+0x2ab` → 유저 예외 `KiDispatchException`" 이라고 적혀 있었다 `[추측]` (PE_emulator 쪽 짐작).
- 질문은 여섯 (sast/78 번호. 원문 요청 26 번호와 달라서 run 문서 표는 둘 다 적었다):

| # | 질문 | 이 글 어디서 |
|---|---|---|
| 1 | 입구 → 디스패치 사슬, `#PF` 의 `MmAccessFault` 갈림, `KiPreprocessFault` 자리 | 2.4 · 2.5 · 2.8 |
| 2 | 예외마다 사고 기록 카드 — `int3` 주소, 접근 위반 0 / 1 / 8 | 2.6 · 2.7 |
| 3 | **유저 스택에 쌓는 모양** (가장 중요), 쓰기 실패, 출발 메모에서 바뀌는 칸 | 2.9 ~ 2.14 |
| 4 | 둘째 기회 (커널 쪽) — 어떻게 끝나나 | 2.15 |
| 5 | `__fastfail` (`int 0x29`) | 2.16 |
| 6 | (덤) 메모리 syscall 의 포인터 실패 · `Process.cpp:1953` 주석 | 2.18 |

### 바퀴 전에 알던 것

- `[정적]` run59: `_PS_NTDLL_EXPORTS +0x20` = `KiUserExceptionDispatcher` (ntdll rva `0x164e10`).
- `[정적]` run42: 유저 APC 는 `KiDeliverApc +0x5ee → KiInitializeUserApc +0x1fb KeContextFromKframes(트랩, 예외, RDI)` 로 원래 문맥을 유저 스택 `CONTEXT` 에 쌓고, 출발 메모 `Rip` 를 `PsNtdllExports+0x28` (`KiUserApcDispatcher`) 로 바꾼다.
- `[정적]` run55: 비 CET 스레드 `CONTEXT` `0x4d0`, 맡긴 문맥 길이 `0x4ff`.
- `[정적]` run77: syscall 출구는 출발 메모 `Rax` 칸을 다시 읽어 `SYSRET`. run80: 헤더 비트 27 → `KiRestoreSetContextState` → `IRETQ`.
- `[정적]` run16: `PspInitializeThunkContext` 6번 — "쓰기 실패면 SEH 착지점 `+0x2ab` → 오류 길". "오류 길" 이 무엇인지는 비어 있었다.
- `[정적]` run18 · run19 · run35: 로더 안 `__fastfail` 자리 (강제 종료). 그 장치가 "프로세스를 바로 끝낸다" 는 건 일반적인 설명 `[추측]` 이었다.
- `[정적]` run79: `NtTerminateProcess` 자기 가게 길 (`+0x243 PspTerminateAllThreads` 뒤 `+0x161 PspExitThread`).
- `[정적]` run70: `KeFeatureBits` 비트 23 · KUSD `ProcessorFeatures[17]` 을 부팅 때 켜는 줄.
- `[정적]` run71: test.exe 의 `OutputDebugStringA → RaiseException(0x40010006)`.
- `[추측]` PE_emulator: 메모리 syscall 의 잘못된 포인터 → 진짜는 SEH → 유저 예외.
- `[소스]` test.exe 에는 CPU 예외를 일부러 내는 코드가 없다.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 사고가 나면 세 걸음 (그리고 넷째)

자세히 들어가기 전에, 이번 스프린트에서 처음 보는 "예외 디스패치" 를 한 장으로 보자.

```
① CPU 가 걸린다          유저 코드가 못 하는 일을 함 → CPU 가 멈추고 커널 입구로 뜀
② 커널이 보고서를 쌓는다  사고 순간 메모 · 목차 · 기록 카드 · 표지 → 유저 스택에 한 덩어리
③ ntdll 로 보낸다         출발 메모를 고쳐 IRETQ → KiUserExceptionDispatcher (사고 처리 창구)
   ─────────────────────────────── 여기까지 이번 (커널 쪽) ───────────────────────────────
   ntdll 안에서 처리기 찾기 (VEH → SEH) → 받으면 NtContinue 로 이어 가기                (Q148b)
④ 아무도 안 받으면        ntdll 이 NtRaiseException(…, 0) 으로 커널에 다시 → 가게 문 닫기  (이번 2.15)
```

- 모두 run 문서의 호출 관계를 큰 덩어리로 묶은 것이다. ①②③④ 의 줄은 `[정적]` 이고, ntdll 안 (VEH·SEH) 은 이번에 안 봤다.

이 그림에 나오는 새 말 넷:

- **예외** — "이대로는 다음 명령으로 못 간다" 는 알림.
  - CPU 가 내는 것 (잘못된 주소 읽기, `int3`, 0 나누기 …) 과 프로그램이 일부러 내는 것 (`RaiseException`) 이 있는 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이번엔 **CPU 쪽** 이다.
  - 비유: 직원이 일하다 넘어짐 = **사고**.
- **폴트 · 트랩** — CPU 예외의 두 꼴로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 폴트: 명령을 **못 마치고** 멈춘다. 남는 `Rip` = 그 명령 자리 (고친 뒤 다시 해 볼 수 있게).
  - 트랩: 명령을 **마치고** 알린다. 남는 `Rip` = 다음 명령 자리.
  - 비유: 폴트 = 계단에서 발을 헛디뎌 **그 계단에** 멈춤. 트랩 = 한 계단 **올라선 뒤** "여기 표시 있어요" 하고 알림.
  - 이 차이가 2.7 (`int3` 은 왜 −1 인가) 과 닿는다.
- **첫 기회 · 둘째 기회** — 같은 사고를 커널 디스패치 (`KiDispatchException`) 에 **두 번** 돌릴 수 있다.
  - 코드에선 `KiDispatchException` 의 다섯째 인자 (스택 `[RSP+0x20]`) 가 1 이냐 0 이냐다 `[정적]`.
  - 첫 기회 (1): 유저 처리기들에게 먼저 보낸다 → ③.
  - 둘째 기회 (0): 처리기가 아무도 안 받았을 때, 또는 처음부터 받을 기회를 안 줄 때 → ④.
  - 비유: 첫 기회 = 가게 담당들에게 "이 사고 처리할 사람?" 돌려 보기. 둘째 기회 = 아무도 손 안 들었을 때 관리사무소가 하는 **마지막 처리**.
- **VEH · SEH** (이름만 — 풀이는 Q148b) — 사고 처리 창구 뒤에서 처리기를 찾는 두 줄로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 대기열 Q148b 가 `RtlpCallVectoredHandlers` (VEH) → SEH (`RtlLookupFunctionEntry` · `RtlpExecuteHandlerForException`) 차례로 볼 예정이다.
  - VEH = 가게 **전체** 에 등록해 두는 처리기 목록. 비유: 가게 전체 **비상 연락망**.
  - SEH = 함수마다 붙은 `__try`/`__except`. run17 의 **예외 보호 구역**, run27·run28 의 **비상 대피도** 를 콜스택 따라 찾아가는 것.

### 2.2 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다.
가지의 `+0x..` 는 **돌아올 자리 (ret)**, 가지 밖 `+0x..` 는 그 줄 자리다. 괄호 번호는 run 문서 그대로라 중간이 빈다. ★ 는 이 글 절.

```
[유저 모드에서 CPU 예외 — 예: 잘못된 주소 읽기]
nt!KiPageFault   (입구 asm, 트랩 프레임 = RBP − 0x80)                                       ★ 2.4 · 2.5
 │  +0x348 CR2 · +0x358 FaultAddress · +0x37e ~ +0x383 FaultIndicator (0 / 1 / 8)
 ├─ (1) +0x38b  MmAccessFault(오류 코드, CR2, 모드, 트랩)     +0x38d JGE ≥ 0 → 예외 없이 나감
 │  +0x3f2 ~ +0x428  0xC0000005 → ECX = 0x10000004 (응급 코드), EDX = 2
 └─ (2) +0x442  KiExceptionDispatch(ECX, EDX, R8 = Rip, R9 = 0/1/8, R10 = CR2)   돌아오지 않음
      │  +0x67 ~ +0x81 레코드 · +0x8c 이전 모드 · +0x90 첫 기회 1 · +0x9f 유저면 곧장
      ├─ (1) +0x145  KiDispatchException(레코드, 예외 프레임, 트랩, 1, 1)                     ★ 2.3
      │    │  +0xa2 이름표 0x10001f · +0x2d4 KeFeatureBits 비트 23 → +0x2e5 0x10005f          ★ 2.8 · 2.17
      │    ├─ (4) +0x142  KeContextFromKframes(트랩, 예외 프레임, 커널 버퍼)                  ★ 2.8
      │    ├─ (5) +0x163  KiPreprocessFault        0x10000004 → 0xC0000005 (+0x9b)
      │    │  +0x16e 유저 갈래 · +0x250 첫 기회
      │    ├─ (7) +0x2c3  KdTrap                   DebugPort 0 · KdIgnoreUmExceptions 0 일 때    ★ 2.9
      │    ├─ (8) +0x3db  DbgkForwardException(레코드, 1, 0)    포트 없음 → 거짓
      │    │  +0x3e3 CLI · +0x3e4 트랩 EFlags TF 지움
      │    │  +0x430 ~ +0x452  M · E · X · C 계산 · +0x460 ~ +0x49c CONTEXT_EX 만들기  __try 시작 +0x41a   ★ 2.10
      │    ├─ (9) +0x4b0  ProbeForWrite(C, S0 − C, 16)                                        ★ 2.12
      │    │  +0x4b4 [M+0x18] = S0 · +0x4c3 [M] = 문맥 Rip
      │    ├─ (10) +0x4d1 KeCopyExceptionRecord(E, 레코드)
      │    ├─ (11) +0x534 RtlpCopyExtendedContext(1, X, …)    CONTEXT (C) · XSAVE 꼬리
      │    │  +0x53b · +0x545 CONTEXT_EX 0x20
      │    │  +0x578 트랩 Rsp = C · +0x583 SegCs = 0x33 · +0x593 Rip = PsNtdllExports+0x20        ★ 2.13
      │  +0x1b4 유저 · +0x1e9 KiInitiateUserApc 루프 · +0x22a KiRestoreSetContextState (비트 27) · +0x262
      └─ ══▶ 조각 +0x4b1 SWAPGS · +0x4b4 IRETQ   (KVA 그림자면 +0x49a KiKernelExit)
ntdll!KiUserExceptionDispatcher   RSP = C (CONTEXT), RSP + 0x4f0 = 레코드          → Q148b (run82)
 ├─ (1) +0x2e  RtlDispatchException(RSP + 0x4f0, RSP)
 └─ (2) +0x53  NtRaiseException(…, R8B = 0)        RtlDispatchException 거짓일 때

[스택 쓰기 실패]  처리기 +0x652 (틈): 0xC00000FD → 레코드 덮고 +0x419 다시 · 그 밖 → +0x7a5   ★ 2.14

[둘째 기회 — NtRaiseException(…, 0) · __fastfail · PspInitializeThunkContext 실패 · 쓰기 실패]   ★ 2.15
nt!KiDispatchException  +0x250 JZ → +0x7a5
 ├─ (1) +0x7b3  DbgkForwardException(레코드, 1, 1)
 ├─ (2) +0x7c8  DbgkForwardException(레코드, 0, 1)
 └─ (3) +0x7dc  ZwTerminateProcess(-1, ExceptionCode)   → run79 NtTerminateProcess 길

[들어오는 쪽 셋]
nt!NtRaiseException  +0x85 첫 기회 → +0x96 KiRaiseException → … → +0x103 KiDispatchException        ★ 2.15
nt!KiRaiseSecurityCheckFailure → +0x368 KiFastFailDispatch  +0xb2 첫 기회 0 → +0xc6 KiDispatchException ★ 2.16
nt!PspInitializeThunkContext  +0x2fc EBX < 0 → … +0x3b4 첫 기회 0 → +0x3c8 KiDispatchException      ★ 2.18
```

- 모두 `[정적]` 이다.
- 이 그림에서 처음 나오는 이름 (자세히는 각 절):
  - `KiExceptionDispatch` = 입구들이 공통으로 부르는 **다리**. 사고 기록 카드를 채우고 본 담당에게 넘긴다 (2.3 · 2.4).
  - `KiDispatchException` = 사고를 어디로 보낼지 정하는 **본 담당** (2.8 ~ 2.15).
  - `KiPreprocessFault` = 응급 코드를 진짜 이름으로 바꾸는 **분류 담당** (2.4).
- 입구 여섯은 손으로 쓴 asm 이라 Ghidra 가 조각 (`FUN_…`) 으로 쪼갰다. 이 글의 입구 오프셋은 run 문서처럼 `.asm` 줄 끝 `(= KiPageFault +0x…)` 값 (함수 시작 기준) 이다.
  - WinDbg 가 조각 주소를 `nt!KiPageFault+0x…` 로 보여 줄지는 `[추측]` (run 문서 "오프셋 읽는 법").

### 2.3 한 줄씩 읽는 법

#### 줄 1: `├─ (1) +0x145 KiDispatchException(레코드, 예외 프레임, 트랩, 1, 1)`

- **누가**: `KiExceptionDispatch` — 입구 여섯이 공통으로 부르는 다리.
- **어디서**: 부르는 자리 `+0x140`, 돌아올 자리 `+0x145`.
  - 멈추면 콜스택에 `nt!KiExceptionDispatch+0x145` 로 보일 자리다. 아직 안 봤다.
- **누구를**: `KiDispatchException` — 본 담당.
- **인자 다섯** `[정적]` (`KiExceptionDispatch +0x90 ~ +0x9c`):
  - `RCX` = `RSP+0x138` — 바로 앞 `+0x67 ~ +0x81` 에서 채운 **사고 기록 카드** (0x98).
  - `RDX` = `RSP` — 예외 프레임 (**개인 서랍**).
  - `R8` = `RBP−0x80` — 트랩 프레임 (**출발 메모**).
  - `R9B` = 이전 모드 — `+0x8c AND R9B,1` (`SegCs & 1`, 유저면 1).
  - `[RSP+0x20]` = **1** — 첫 기회 (`+0x90 MOV byte [RSP+0x20],1`).
- **무엇을 위해**: 유저 모드에서 난 사고면 `+0x9f JNZ` 로 검사 없이 곧장 본 담당에게.
  - 커널 모드면 IRQL·스택 검사 (`+0xa5 ~ +0x12d`) 를 거쳐 `+0x139 KiExceptionDispatchOnExceptionStack` 쪽으로 갈 수 있다 — 이번 범위 밖.
- **그다음**: 본 담당은 `+0x145` 로 돌아온다. 하지만 `KiExceptionDispatch` 는 **입구로 돌아가지 않는다.** 개인 서랍을 되살린 뒤 `IRETQ` 로 곧장 유저로 나간다 (2.13).

#### 줄 2: `+0x452 LEA R9,[R8-0x4d0]` — 호출이 아니라 **계산** 줄

- **누가**: `KiDispatchException`.
- **무엇을**: `R8` = `X` (목차 쪽지 자리, 바로 앞 `+0x447 LEA R8,[RBX-0x20]`) 에서 `0x4d0` 내린 주소 → `R9` = **`C`**.
- **뜻**: `C` = 사고 순간 메모 (`CONTEXT`) 가 놓일 자리. 동시에 직원이 사고 처리 창구에서 시작할 **새 `RSP`** 다.
- 같은 계산 (`X − 0x4D0`) 이 나중에 `+0x571 LEA R10,[RDI-0x4d0]` 에서 다시 나오고, `+0x578` 이 그 값을 출발 메모 `Rsp` 칸에 넣는다 (2.13).
- 비유: 가게 작업대에서 "묶음 맨 위 장" 이 올라갈 자리를 연필로 표시하는 줄.

#### 줄 3: 출발 메모 `Rip` 를 바꾸는 두 줄

```
KiDispatchException +0x58c  MOV RAX,[PsNtdllExports+0x20]
KiDispatchException +0x593  MOV [R12+0x168],RAX
```

- **누가**: `KiDispatchException`. `R12` = 트랩 프레임.
- **어디에**: 출발 메모 `+0x168` = `Rip`.
- **무엇을**: 벽 주소록 `+0x20` 칸 = `KiUserExceptionDispatcher` (ntdll rva `0x164e10`, run59).
- **무엇을 위해**: 유저로 돌아갈 때 사고 난 자리가 아니라 **사고 처리 창구** 에서 시작하게.
- 원래 값 (사고 난 자리) 은? 이미 사고 순간 메모 `Rip` 칸 (`C+0xf8`) 과 "사고 직전 위치" 표지 (`C+0x590`) 에 베껴 두었다 `[정적]`.
- 짐 지도 7.6.2 벽 주소록 짝 표에 이 읽는 줄 (`+0x58c`) 이 더해졌다.

### 2.4 사고 접수 창구 — 입구는 "응급 코드" 를 담아 보낸다 (질문 1)

#### 입구 여섯 `[정적]`

| 입구 | CPU 예외 (`#..` = CPU 예외 이름 약자) | `ECX` 에 담는 코드 | `KiExceptionDispatch` 부르는 곳 (call / ret) |
|---|---|---|---|
| `KiPageFault` | 페이지 폴트 `#PF` | `0x10000004` (실패일 때만, 2.5) | `+0x43d` / `+0x442` |
| `KiGeneralProtectionFault` | 일반 보호 `#GP` | `0x10000001` | `+0x360` / `+0x365` |
| `KiBreakpointTrap` | `int3` `#BP` | `0x80000003` (진짜 코드 그대로) | `+0x356` / `+0x35b` |
| `KiDivideErrorFault` | 0 나누기 `#DE` | `0x10000003` | `+0x34a` / `+0x34f` |
| `KiInvalidOpcodeFault` | 잘못된 명령 `#UD` | `0x10000002` | `+0x34a` / `+0x34f` |
| `KiDebugTrapOrFault` | 한 걸음 `#DB` | `0x80000004` | `+0x600` / `+0x605` |

- 입구 ↔ CPU 예외 번호 (`#PF` = 14 등) 는 **이름으로** 짝지었다 `[추측: 이름]`. CPU 가 예외마다 어느 입구로 뛸지 적어 둔 표 (IDT) 의 바이트는 안 봤다 (Q155 ④).
- 입구가 `KiExceptionDispatch` 에 넘기는 약속 `[정적]`:
  - `ECX` = 코드 · `EDX` = 덧붙임 개수 · `R8` = 사고 장소 · `R9`/`R10`/`R11` = 덧붙임 `[0]`/`[1]`/`[2]`.
  - 트랩 프레임 = `RBP − 0x80` (입구가 `RBP = RSP + 0x80` 로 잡아 둠).
- 입구 머리 `[정적]`: 오류 코드가 없는 입구 (`#BP`·`#DE`·`#UD`·`#DB`) 는 첫 줄이 `SUB RSP,8` (가짜 칸), 있는 입구 (`#PF`·`#GP`) 는 바로 `PUSH RBP`. 이어서 `SUB RSP,0x158` · `LEA RBP,[RSP+0x80]` 로 출발 메모 자리를 만든다.
  - 공통 머리 셋 (`KiFlushBhbDuringTrapEntryOrExit` · `KiSaveDebugRegisterState` · `KiSynchronizeUserIsolationDomainExit`) 은 줄기 밖이라 이름만.

#### 새 말 — 응급 코드 (안쪽 표시 코드)

- 입구들은 **진짜 사고 이름을 바로 안 넘긴다** `[정적]`.
  - `#PF`·`#GP`·`#UD`·`#DE`·`#NM` 은 `0x10000004`·`0x10000001`·`0x10000002`·`0x10000003`·`0x10000010` 같은 **안쪽 표시 코드** 를 담는다.
  - 본 담당 안의 `KiPreprocessFault` 가 진짜 코드로 바꾼다. 유저는 이 표시 코드를 **못 본다**.
- 비유: 접수 창구는 "4번 응급" 같은 **응급 코드** 만 적어 넘긴다. 안쪽 **분류 담당** 이 "접근 위반" 같은 진짜 사고 이름을 붙인다.
- 응급 코드를 꾸며 보내는 길은 막혀 있는 것으로 보인다 `[추측: 의도]`.
  - 근거 `[정적]`: `KiRaiseException +0xe6 BTR [레코드],0x1c` — `NtRaiseException` 으로 들어온 코드의 비트 28 (`0x10000000`) 을 지운다.

#### 분류 담당 `KiPreprocessFault` 는 어디에 끼나 `[정적]`

- 입구에도, `KiExceptionDispatch` 에도 없다. **`KiDispatchException` 안** 이다.
  - 사고 순간 메모를 만든 **뒤** (`+0x13d KeContextFromKframes`),
  - 유저·커널 갈림 (`+0x167 ~ +0x16e`) **앞** — `+0x15e` (ret `+0x163`).
- 돌려준 `AL ≠ 0` 이면 "처리됨" (`+0x165 JNZ`):
  - `+0x1b8 KeContextToKframes` 로 메모를 출발 메모에 되돌려 쓰고, **사고 없이** 나간다.
  - 예: ATL thunk 흉내, XSTATE 켜기. `#NM` (XSTATE 첫 사용) 은 `KiEnableOptionalXStateFeatures` 로 켜는 데 성공하면 "처리됨" 이라 예외가 유저로 안 간다.
- 비유: 분류 담당이 보고 "이건 사고가 아니네, 내가 고쳤다" 하면 그냥 일터로 돌려보낸다.

### 2.5 `#PF` 는 먼저 메모리 담당에게 — 실패일 때만 사고 (질문 1)

#### 새 말 둘

- **`CR2`** — CPU 가 페이지 폴트 때 "못 닿은 주소" 를 적어 두는 레지스터로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이번 코드에서 `+0x348 MOV RCX,CR2` 로 읽고 사고 카드 `[1]` 이 된다 `[정적]`.
- **`MmAccessFault`** — 메모리 담당. 돌려준 값이 0 이상이면 "해결", 음수면 "실패" 로 갈린다 `[정적]`.
  - 일반적으로는 아직 메모리에 안 올라온 쪽처럼 **고칠 수 있는 폴트** 를 여기서 해결한다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 직원이 바닥 안 깔린 곳을 밟으면 (run46 "바닥 깔기"), 메모리 담당이 먼저 와서 "깔아 줄 수 있는 자리면 깔고 끝". 깔 수 없는 자리일 때만 사고로 접수한다.

#### 걸음 `[정적]`

1. `+0x342` 오류 코드를 가져온다.
2. `+0x348 MOV RCX,CR2` → `+0x358` 출발 메모 `FaultAddress` (`+0xd0`) 에 저장.
3. `+0x37e ~ +0x383` 출발 메모 `FaultIndicator` (`+0x2a`) 에 읽기 0 / 쓰기 1 / 실행 8 (2.6).
4. `+0x386` (ret `+0x38b`) `MmAccessFault(오류 코드, CR2, 모드, 트랩)`.
5. **`+0x38d JGE`** — 해결 (≥ 0) 이면 **사고 아님**. 예외 없이 나간다 (`+0x46e` · `+0x489`).
6. 실패 (음수) 일 때만 아래로:
   - `+0x393` 유저 CS 인가 · `+0x39c SegCs == 0x33` 인가 (아니면 WoW64 쪽).
   - `+0x3a6 ~ +0x3c8` 유저 GS 베이스가 기대와 다르면 고치고 예외 없이 돌아감 `[추측: 망가진 GS 베이스 구제]` (Q155 ⑦).
7. `+0x3f2 ~ +0x428` 메모리 담당의 상태 → 코드:
   - `0xC0000005` → `ECX = 0x10000004` (응급 코드), `EDX = 2`.
   - `0x80000001` (가드 페이지) · `0xC00000FD` (스택 넘침) → 그대로, `EDX = 2`.
   - 그 밖 → `0xC0000006` (페이지 읽기 실패), `EDX = 3`, `R11D` = 원래 상태.
   - `0xD0000006` → 예외 아님, 버그체크 `0xA` 길.
8. `+0x42d R10 = CR2` · `+0x431 R9 = 0/1/8` · `+0x436 R8 = Rip` → `+0x43d` (ret `+0x442`) `KiExceptionDispatch`.

- 덤 `[정적]`: `+0x364 JNC` — 불린 쪽의 인터럽트 깃발 (IF) 이 꺼져 있으면 `MmAccessFault` 도 안 부르고 버그체크 길. 유저 코드는 IF 가 늘 켜져 있어 해당 없음.
- 짐 지도 쪽: 3.15 머리에 "유저 예외 줄" 로 `FaultAddress` ← `+0x358`, `FaultIndicator` ← `+0x383` 이 적혔다. 7.2 의 "누가 썼는지 모름" 칸 후보다 `[추측]` (3절).

### 2.6 사고 기록 카드 — `EXCEPTION_RECORD` (질문 2)

#### 새 말 — `EXCEPTION_RECORD` (0x98 바이트)

- 사고 하나를 적는 카드. 칸 (run 문서 "칸 이름"):
  - `ExceptionCode +0x0` — 사고 이름 (코드)
  - `ExceptionFlags +0x4` — 표시 (계속할 수 있나 등)
  - `ExceptionRecord +0x8` — 이어진 다른 카드 (링크)
  - `ExceptionAddress +0x10` — **사고 장소**
  - `NumberParameters +0x18` — 덧붙임 개수
  - `ExceptionInformation[15] +0x20` — 덧붙임 칸 15개
- 비유: **사고 기록 카드**. 이름 · 표시 · 이어진 카드 · 장소 · 덧붙임 개수 · 덧붙임 칸.
- 공통 `[정적]`:
  - `ExceptionFlags` = 0 (`KiExceptionDispatch +0x6b`). 단 `__fastfail` 만 **1** (`KiFastFailDispatch +0x6b` — 계속 불가 표시, 2.16).
  - 링크 = 0.

#### 대표 표 — 유저가 보는 값 (분류 담당이 푼 뒤) `[정적]`

| 예외 | `ExceptionCode` | `ExceptionAddress` | 개수 | `ExceptionInformation` |
|---|---|---|---|---|
| 접근 위반 (`#PF`) | `0xC0000005` | `Rip` | 2 | `[0]` 읽기 **0** / 쓰기 **1** / 실행 **8**, `[1]` = `CR2` |
| 가드 페이지 | `0x80000001` | `Rip` | 2 | 〃 |
| 스택 넘침 | `0xC00000FD` | `Rip` | 2 | 〃 |
| 페이지 읽기 실패 | `0xC0000006` | `Rip` | **3** | 〃 + `[2]` = `MmAccessFault` 의 NTSTATUS |
| 특권 명령 (`#GP` — `hlt` `cli` `in` `out` `mov cr` 등) | `0xC0000096` | `Rip` | **0** | 없음 |
| 그 밖의 `#GP` (정규화 안 된 주소 등) | `0xC0000005` | `Rip` | 2 | `[0]` = **0**, `[1]` = **`0xFFFFFFFFFFFFFFFF`** |
| `ud2`·잘못된 명령 (`#UD`) | `0xC000001D` | `Rip` | 0 | 없음 |
| 0 나누기 (`#DE`) | **`0xC0000094`** (몫 넘침은 `0xC0000095`) | `Rip` | 0 | 없음 |
| `int3` (`#BP`) | `0x80000003` | **`int3` 자기 자리** | **1** | `[0]` = 0 |
| 한 걸음 (`#DB`) | `0x80000004` | `Rip` | 0 (분기 추적이면 2) | 없음 |
| `__fastfail` (`int 0x29`) | `0xC0000409`, 플래그 1 | `int 0x29` 자기 자리 | 1 | `[0]` = `RCX` |

- 여기 `Rip` = CPU 가 멈춘 자리 (출발 메모 `Rip`). 접근 위반이면 **못 한 명령 자리** 다 (`KiPageFault +0x436 MOV R8,[RBP+0xe8]`).
- 나머지 (가드 외 x87·SIMD·정렬·`into`·`bound`·`int 0x2c`·`int 0x2d`·`#NM`) 는 run 문서 "예외 코드 표" 에 있다.

#### 접근 위반 `[0]` 0 / 1 / 8 — 어떻게 만드나 (단계별로)

1. `[정적]` `KiPageFault +0x37e SHR EAX,1` — CPU 가 준 오류 코드를 한 칸 오른쪽으로 민다.
2. `[정적]` `+0x380 AND EAX,9` — 비트 0 과 비트 3 만 남긴다.
3. 그래서 → 오류 코드 비트 1 (쓰기) 이 값 비트 0 으로, 비트 4 (명령 가져오기) 가 값 비트 3 으로 간다.
   - **0 = 읽기, 1 = 쓰기, 8 = 실행.** (9 는 이론상 쓰기 + 실행.)
4. `[정적]` `+0x383` 출발 메모 `FaultIndicator` 에 적고, `+0x431` 에서 `R9` 로 → 카드 `[0]`.
5. `[정적]` "없는 쪽" 인지 "권한 없는 쪽" 인지 (`P` 비트) 는 **안 본다**.
6. 덤 `[정적]`: 분류 담당은 일반 접근 위반이면 **코드만** `0xC0000005` 로 바꾸고 개수·`[0]`·`[1]` 은 그대로 둔다 (`KiPreprocessFault +0x57` · `+0x91 ~ +0x9b`). 실행 + 유저면 ATL thunk 확인 갈래 (`+0x186 ~ +0x18b`) 가 있다.

#### 0 나누기 두 코드 — `KiOp_Div` (단계별로)

1. `[정적]` `KiPreprocessFault +0x22b` · `+0x231` — 응급 코드 3 → `0xC0000094`.
2. `[정적]` `KiOpDecode` 가 명령을 읽어 나누기면 `KiOp_Div` 로 간다.
3. `[정적]` `KiOp_Div +0x21 CMP [레코드],0xc0000094` → 나누는 값을 읽어 `+0xdd TEST RCX,RCX` → 0 이 아니면 **`+0x104 MOV [RDI],0xc0000095`**.
4. 그래서 → 0 으로 나누면 `0xC0000094`. 나누는 값은 0 이 아닌데 CPU 가 `#DE` 를 낸 경우 (`INT_MIN / −1` 같은 몫 넘침) 면 `0xC0000095`.

#### 특권 명령과 그 밖의 `#GP` `[정적]`

- 입구는 `ECX 0x10000001`, `EDX 2`, `R9` = 선택자 오류 코드, `R10 0` (`+0x33e ~ +0x356`).
- 분류 담당이 `+0xc3 KiOpDecode` 로 명령을 읽는다.
  - 특권 명령이면 `+0x1d8 KiOp_Priv` → `0xC0000096`, 개수 0 (`KiOp_Priv +0x4 AND [레코드+0x18],0` · `+0x8`).
  - 해석 못 하거나 특권이 아니면 `+0x1e2 ~ +0x20b` (`+0x1f8 · +0x1ff · +0x203`) → `0xC0000005`, 개수 2, `[0] = 0`, `[1] = −1`.
  - 디코더 결과 칸 `[RBP+0x28]` 의 뜻은 `[추측]` (Q155 ⑤).
- "정규화 안 된 주소" = 64비트 주소의 위쪽 비트가 규칙에 안 맞는 주소로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

#### PE_emulator 에게 (run 문서 표 질문 2 줄)

- 지금은 이 표 전부가 없다 (멈춤).
- `UC_ERR_READ_UNMAPPED` / `WRITE_UNMAPPED` / `FETCH_UNMAPPED` ↔ `[0]` 0 / 1 / 8.
- `UC_ERR_INSN_INVALID` 하나로 `#UD` (`0xC000001D`) 와 특권 명령 (`0xC0000096`) 을 Unicorn 이 갈라 주는지는 모른다 `[추측]`. 못 가르면 명령 바이트를 직접 읽어야 한다.
- 0 나누기는 나누는 값이 0 인지 보고 `0xC0000094` / `0xC0000095` 를 가른다.

### 2.7 `int3` 의 사고 장소 = 자기 자리 (질문 2)

- 질문: `int3` 을 밟았을 때 사고 장소는 **`int3` 자리** 인가, **다음 명령** 인가?
- 답: **자기 자리** `[정적]`. 두 군데에서 1 을 뺀다.

```
KiBreakpointTrap    +0x34d  DEC R8                  ← 카드 ExceptionAddress
KiDispatchException +0x707  DEC qword [RBX+0xf8]    ← 사고 순간 메모 Rip
```

#### 단계별로

1. `[정적]` `KiBreakpointTrap +0x34d DEC R8` — `R8` (사고 장소로 넘길 값) = 출발 메모 `Rip` − 1.
2. `[추측]` (일반적으로 알려진 설명) `int3` 은 1바이트 명령이고 **트랩** 이라, CPU 가 남긴 `Rip` 는 `int3` **다음** 바이트다. 그래서 1 을 빼면 `int3` 자리가 된다 (2.1 폴트·트랩).
3. `[정적]` 카드 나머지: 코드 `0x80000003` 그대로 (응급 코드 없음), 개수 1, `[0]` = 0 (`+0x350 MOV R9D,0`).
4. `[정적]` 본 담당 `KiDispatchException +0x147 CMP [레코드],0x80000003` · `+0x14e JZ` → `+0x707 DEC [문맥+0xf8]` — **사고 순간 메모 `Rip` 도 −1**.
5. `[정적]` 그 뒤 `+0x716` 로 돌아와 **그래도** 분류 담당 (`KiPreprocessFault`) 을 부른다. (`KiDynamicTraceMask` 비트 1 이 켜졌으면 `KiTpHandleTrap` 갈래 — 이름만.)
6. `[정적]` 출발 메모 `Rip` 은 그대로 두었다가, `+0x593` 에서 사고 처리 창구 주소로 덮는다 (2.13).
7. 그래서 → 유저가 보는 메모 `Rip` = `int3` 자리다. 처리기가 `Rip` 를 안 고치고 "계속" 하면 **같은 `int3` 을 또 실행** 한다 `[정적]` (run 문서).

- 비유: 한 계단 올라선 뒤 알린 사고인데, 보고서엔 **표시가 있던 그 계단** 을 사고 장소로 적는다. 처리 담당이 "다음 계단부터 가" 라고 고쳐 적어 주지 않으면, 직원은 같은 계단으로 돌아가 또 표시를 밟는다.
- 견줄 것 `[정적]`:
  - `__fastfail` (`int 0x29`) 도 자기 자리 — `KiRaiseSecurityCheckFailure +0x0 SUB [RSP],2` (2.16).
  - `int 0x2c` 도 자기 자리 (`+0x0 SUB [RSP],2`).
  - `int 0x2d` 는 반대로 저장 `Rip` 를 1 **올리고** (`+0x0 INC [RSP]`) `DEC` 가 없다.
- PE_emulator 규칙 1: 주소 = `int3` 자리, **문맥 `Rip` 도 `int3` 자리**.

### 2.8 관리사무소 작업대에서 먼저 쓴다 — 사고 순간 메모 (질문 3 앞)

#### 핵심 `[정적]`

- 사고 순간 메모 (`CONTEXT`) 를 **커널 스택에서 먼저** 만들고, 유저 스택엔 나중에 **한꺼번에 복사** 한다.

#### 걸음 `[정적]`

1. `+0xa2 R15D = 0x10001f` · `+0xa8 [RBP+4]` — 체크 칸 기본값.
2. 유저면 (`+0xae CMP` · `+0xb2 JNZ`) `+0x2d4 TEST [KeFeatureBits],0x800000`:
   - 켜져 있으면 `+0x2e5 R15D = 0x10005f`.
   - 꺼져 있으면 `+0x2df JZ` 로 `0x10001f` 그대로.
   - (`+0xb8 ~ +0xc4` 의 `0x10009f` 는 CET 용 **커널 모드 갈래** 라 유저에선 안 탄다.)
3. `+0xe3 RtlGetExtendedContextLength3` — 메모 + 별지 전체 길이. 넷째 인자 = KUSD `+0x3d8` (XState 칸).
4. `+0x103` 스택 늘리기 확인 (같은 주소 이름 2개, 후보 `__chkstk`·`_alloca_probe`) · `+0x108 SUB RSP,RAX` — **관리사무소 작업대** 에 그만큼 자리.
5. `+0x12b RtlInitializeExtendedContext2` — 빈 메모 틀, 체크 칸을 넣는다.
6. **`+0x13d` (ret `+0x142`) `KeContextFromKframes(트랩, 예외 프레임, 버퍼)`** — 출발 메모 · 개인 서랍 → 메모.
7. `+0x15e` (ret `+0x163`) `KiPreprocessFault` — 응급 코드 → 진짜 이름 (2.4).

#### 체크 칸 풀이 (run 문서, run80 과 같은 숫자)

| 값 | 묶음 |
|---|---|
| `0x100001` | CONTROL |
| `0x100002` | INTEGER |
| `0x100004` | SEGMENTS |
| `0x100008` | FLOATING_POINT |
| `0x100010` | DEBUG_REGISTERS |
| `0x100040` | XSTATE |

- `0x10001f` = 앞 다섯 모두. `0x10005f` = 거기에 XSTATE (별지) 까지.
- run42 의 `0x10001b` (시동 메모·유저 APC) 와 견주면 차이는 `0x4` (SEGMENTS) 하나다.
- `KeContextFromKframes` 는 체크 칸을 **받은 버퍼 `+0x30`** 에서 읽고 (`+0x47`), 묶음마다 켜졌나 보고 채운다 `[정적]`:
  - CONTROL `+0x53` · SEGMENTS `+0x99` · INTEGER `+0xb5` · XSTATE `+0x188` · FLOATING_POINT `+0x259` · DEBUG `+0x35a`.

#### run42 (유저 APC) 와 무엇이 다른가

- `[정적]` 유저 APC (`KiInitializeUserApc +0x1fb`) 도 **같은** `KeContextFromKframes` 를 쓴다. 다만 거기선 **유저 스택에 바로** 만든다.
- `[정적]` 예외는 **커널 버퍼에 먼저 만든 뒤 복사** 한다.
- 왜 다른지는 근거 문서에 이유가 적혀 있지 않다. 이 해설의 짐작 `[추측]`:
  1. 근거 `[정적]`: 메모를 만든 직후 분류 담당 (`+0x15e`) 이 "처리됨" 으로 끝낼 수 있다 (2.4). 그다음엔 디버거에 먼저 보여 주는 줄 (`+0x2be` · `+0x3d6`) 이 있다 (2.9).
  2. 유저 스택에 쓰는 줄 (`+0x4ab` ~) 은 그 **뒤** 에야 나온다.
  3. 그래서 → "유저 쪽으로 보낼지" 가 정해지기 전엔 가게 작업대를 건드리지 않으려는 것으로 보인다.
  4. 아직 확인 아님 — 코드가 이유를 말해 주지는 않는다.
- 비유: 관리사무소는 사고 순간 메모를 **자기 작업대에서 먼저** 쓴다. 분류 담당이 "사고 아님" 하면 그 메모로 출발 메모를 되돌려 놓고 끝. 사고가 맞을 때 그제야 가게 작업대로 옮긴다. 쪽지 (유저 APC) 때는 처음부터 가게 작업대 위에 바로 베꼈다.

### 2.9 디버거 없는 첫 기회 — 감독관에게 먼저 묻기 (질문 3 앞)

#### 흐름 `[정적]` (run 문서 "디버거 없는 첫 기회 흐름")

1. `+0x22e ~ +0x24a` — 보통 64비트 프로세스인가. 아니면 WoW64·최소 프로세스 갈래 `+0x5d1 ~ +0x62a` (이름만).
2. `+0x250` — 첫 기회인가. 아니면 `+0x7a5` (2.15).
3. `+0x267` (ret `+0x26c`) `KdIsThisAKdTrap`.
4. `+0x27f ~ +0x2a1` — `KdIgnoreUmExceptions` · 프로세스 `+0x308` DebugPort · `0x80000033` 검사.
5. DebugPort 0 이고 `KdIgnoreUmExceptions` 도 0 이면 `+0x2be` (ret `+0x2c3`) **`KdTrap`**.
6. `KdTrap` 이 0 을 돌려주면 `+0x3d6` (ret `+0x3db`) **`DbgkForwardException(레코드, 1, 0)`**.
   - 디버그 포트가 없으면 곧바로 거짓 (`DbgkForwardException +0x161 TEST RSI,RSI` · `+0x169 JNZ` → `+0x268 XOR AL,AL`).
7. `+0x3dd JNZ` 안 탐 → `+0x3e3` 부터 유저 처리기로 보낼 준비 (2.10 ~).

- 덤 `[정적]`: 첫 기회일 때만 `+0x9c JNZ` 가 먼저 Pico 프로세스인지 본다 (`[프로세스+0x640]` `PicoContext`). 보통 프로세스는 0 이라 `+0x1f9 JZ` 로 안 탄다.

#### 새 말 둘

- **`KdTrap`** — 이름으로 보아 **커널 디버거** 에게 사고를 알리는 함수 `[추측: 이름]`. 비유: 관리사무소 **감독관** (KDNET 으로 붙은 WinDbg).
- **디버그 포트 · 예외 포트** — 유저 디버거가 붙거나 오류 보고 장치가 받을 때 쓰는 연락 창구로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이번 코드에선 프로세스 `+0x308` (DebugPort) · `+0x240` (예외 포트) 칸이 0 이냐로 갈린다 `[정적]`.
  - 비유: 가게 **감독관 연락선** · 사고 **신고센터 연락선** (2.15).

#### 디버거가 붙어서 달라지는 점 `[정적]` (run 문서 "디버거 메모")

- KDNET 이 붙어 있으면 유저 사고 첫 기회에 `+0x2be KdTrap` 을 **먼저** 부른다 (`DebugPort == 0` · `KdIgnoreUmExceptions == 0` 일 때).
- WinDbg 가 거기서 멈추면 `KiUserExceptionDispatcher` 까지 안 가는 것처럼 보일 수 있다 `[추측]`.
- → Q104 동적 5 에서 `dd nt!KdIgnoreUmExceptions L1` 을 먼저 읽는다. windbg-kd-runbook pitfalls 에 넣자는 하네스 제안 ⑩ 은 보류 중 (plan.md).
- PE_emulator: 디버거 갈래 · `KdTrap` 은 흉내 내지 않아도 된다 (규칙 9).

### 2.10 유저 스택 배치 ① — 얼마나 내려가나 (질문 3, 가장 중요)

#### 이름 다섯 `[정적]`

- `S0` = 사고 순간의 유저 `RSP` (= 메모 `Rsp`, `+0x22e MOV R15,[RBX+0x98]`).
- 위에서 아래로 (주소가 작아지는 쪽으로) 차례로 자리를 잡는다. 줄은 `+0x430 ~ +0x452` (공통), XSTATE 일 때만 앞에 `+0x62a ~ +0x640`.

| 이름 | 계산 | 줄 | 크기·정렬 | 비유 |
|---|---|---|---|---|
| `A` (XSAVE 꼬리 시작) | XSTATE 없음: **`A = S0`**. 있음: **`A = (S0 − XState.Length) & ~0x3F`** | `+0x62e` · `+0x635` · `+0x63c` | 64바이트 정렬 | 별지 시작 |
| `M` (머신 프레임) | **`M = (A − 0x28) & ~0xF`** | `+0x430 LEA RDI,[R15-0x28]` · `+0x434 AND RDI,-0x10` | 0x28, 16 정렬 | "사고 직전 위치" 표지 |
| `E` (예외 레코드) | `E = M − 0xA0` | `+0x43c LEA RBX,[RDI-0xa0]` | 0x98 (칸 0xA0) | 사고 기록 카드 |
| `X` (`CONTEXT_EX`) | `X = E − 0x20` | `+0x447 LEA R8,[RBX-0x20]` | 0x20 | 목차 쪽지 |
| `C` (`CONTEXT` = **새 `RSP`**) | **`C = X − 0x4D0`** | `+0x452 LEA R9,[R8-0x4d0]` | 0x4D0, 16 정렬 | 사고 순간 메모 |

- `XState.Length` = 관리사무소 작업대의 확장 메모 `[+0x14]` 값.

#### 한 줄로

- **새 `RSP` = `C` = `((A − 0x28) & ~0xF) − 0x590`.**
- 정렬 때문에 `S0` 에서의 거리가 조금 갈린다 (XSTATE 없을 때, run 문서):
  - `S0` 가 16의 배수 + 8 (함수 안에서 보통) 이면 `C = S0 − 0x5B8`.
  - `S0` 가 16의 배수면 `C = S0 − 0x5C0`.
- 꾸민 숫자 예 (결과서): `S0 = …F6E8` → `M = …F6C0`, `E = …F620`, `X = …F600`, `C = …F130`.
- **레드 존 없음** — `S0` 바로 아래부터 쓴다 `[정적]`.
  - 레드 존 = 일부 환경에서 `RSP` 바로 아래 몇 바이트를 함수가 쓰도록 비워 두는 약속으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 여기선 그런 빈칸이 없다.

#### 교차 확인 — ntdll 이 적어 둔 설명서와 맞는다 `[정적]`

- 결과서가 `.pdata` 로 직접 읽은 ntdll `KiUserExceptionDispatcher` 의 `UNWIND_INFO`:
  - `UWOP_ALLOC_LARGE 0x590` + `UWOP_PUSH_MACHFRAME` (오류 코드 없음).
  - 뜻 = "시작 `RSP` 에서 `0x590` 위에 머신 프레임이 있다".
- 위 계산의 `M − C = 0x590` 과 **같다**.
- 비유: 관리사무소가 내려놓는 묶음 크기와, 사고 처리 창구 문에 붙은 **"내 작업대 위 묶음은 이렇게 생겼음" 설명서** (run15 의 `UNWIND_INFO`) 가 딱 맞물린다. 두 쪽이 따로 적혀 있는데 숫자가 같다.

### 2.11 유저 스택 배치 ② — `C` 기준 표 (원문 3번 "표 하나")

#### 새 말 셋 — 먼저

- **머신 프레임** — `Rip` · `SegCs` · `EFlags` · `Rsp` · `SegSs` 다섯 칸 (8바이트씩, 0x28).
  - CPU 가 인터럽트·예외로 들어올 때 스택에 쌓는 다섯 칸과 같은 모양으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 여기선 커널이 **`Rip` 과 `Rsp` 만** 쓴다 `[정적]`.
  - 풀어 보기 (unwind) 가 `KiUserExceptionDispatcher` 에서 한 칸 위로 거슬러 오를 때 "사고 난 자리는 여기, 그때 `RSP` 는 여기" 를 읽는 자리로 보인다 `[추측]`. `UWOP_PUSH_MACHFRAME` 이 이 자리를 가리킨다는 것까지가 `[정적]` 이다.
  - 비유: 묶음 맨 아래 붙은 **"사고 직전 위치" 표지**. 나중에 되짚어 보는 사람이 "직원은 원래 어디 서 있었나" 를 여기서 읽는다.
- **`CONTEXT_EX`** — 메모 바로 뒤 0x20 바이트. `{Offset 4B, Length 4B}` 네 쌍이다 `[정적: 값]`.
  - "메모 전체 · 기본 메모 · 별지 · (넷째)" 가 어디서 시작해 얼마나 긴지 적는다.
  - 칸 이름 (`All` · `Legacy` · `XState` · `KernelCet`) 은 공개 PDB 에 이 구조체가 없어서 `[추측: 이름]` 이다 (`pdb_struct.py` → 없음).
  - 비유: 메모 뒤에 붙인 **목차 쪽지**.
- **XSAVE · XSAVE 꼬리** — CPU 의 덧붙은 넓은 레지스터 상태를 한 번에 메모리로 쏟아 내는 명령이 XSAVE 계열로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 그 결과가 메모 뒤 **별지** (run42 의 XState) 다. 이번엔 `S0` 바로 아래 (`A ~ S0`) 에 놓인다 `[정적]`.

#### 표 — 시작 `RSP` (= `C`) 기준 `[정적]`

| `C` 기준 | 크기 | 무엇 | 값 / 누가 쓰나 | 비유 |
|---|---|---|---|---|
| `+0x000` | 0x30 | `P1Home ~ P6Home` | **안 씀** (원래 스택에 있던 값) | 메모 맨 앞 빈칸 |
| `+0x030` | 4 | `ContextFlags` | **`0x10001f`** (XSTATE 켜짐이면 **`0x10005f`**) | 체크 칸 |
| `+0x034` | 4 | `MxCsr` | 출발 메모 `+0x2c` | |
| `+0x038 ~ +0x047` | 0x10 | 세그먼트 여섯 · `EFlags` | `SegCs 0x33` · `SegSs 0x2b` · `EFlags` = **TF 지우기 전 사진**, `SegDs`·`SegEs`·`SegGs` = `0x2b`, `SegFs` = `0x53` 고정 | |
| `+0x048 ~ +0x077` | 0x30 | `Dr0 ~ Dr7` | 출발 메모의 디버그 레지스터 (`Dr7` 켜진 비트 없으면 0) | |
| `+0x078 ~ +0x0f7` | 0x80 | `Rax ~ R15` | `Rax ~ R11`·`Rbp` = 출발 메모, `Rbx`·`Rdi`·`Rsi`·`R12 ~ R15` = **개인 서랍**, `Rsp` (`+0x98`) = `S0` | |
| `+0x0f8` | 8 | `Rip` | 사고 자리 (`int3` 이면 `int3` 자리) | |
| `+0x100 ~ +0x2ff` | 0x200 | `FltSave` (x87·XMM) | `FXSAVE` 결과 + `Xmm0 ~ 5` = 출발 메모, `Xmm6 ~ 15` = 개인 서랍 | |
| `+0x300 ~ +0x4af` | 0x1b0 | `VectorRegister`·`VectorControl`·`DebugControl` | **안 씀** (스택 쓰레기) | |
| `+0x4b0 ~ +0x4cf` | 0x20 | `LastBranch*`·`LastException*` | 디버그 묶음에 같이 복사 | |
| **`+0x4d0`** | **0x20** | `CONTEXT_EX` | **XSTATE 없어도 늘 씀** (아래) | 목차 쪽지 |
| **`+0x4f0`** | 0x98 (칸 0xA0) | `EXCEPTION_RECORD` | 0x98 을 0 으로 지운 뒤 앞 0x20 + `개수 × 8` 만 복사 | 사고 기록 카드 |
| **`+0x590`** | 8 | 머신 프레임 `Rip` | 메모 `Rip` 와 같은 값 | 표지 |
| `+0x598` · `+0x5a0` | 8 · 8 | 머신 프레임 `SegCs` · `EFlags` | **안 씀** | |
| **`+0x5a8`** | 8 | 머신 프레임 `Rsp` | `S0` | 표지 |
| `+0x5b0` | 8 | 머신 프레임 `SegSs` | **안 씀** | |
| `+0x5b8 ~ A − C` | 0 ~ 0xF | 정렬 빈틈 | 안 씀 | |
| `A − C ~ S0 − C` | `S0 − A` | XSAVE 꼬리 (XSTATE 일 때만) | 앞 0x40 은 `+0x4da ~ +0x502` 가 직접, 나머지는 `RtlpCopyExtendedContext +0x330 RtlpCopyXStateChunk` | 별지 |
| `S0 − C` | — | 사고 순간의 원래 `RSP` | 커널은 안 건드림 | |

- "안 씀" 칸에는 유저 스택에 원래 있던 값 (쓰레기) 이 그대로 남는다. ntdll 이 그 칸을 읽나는 Q148b ① 이다.

#### 목차 쪽지 (`CONTEXT_EX`) 네 칸 — 한 칸씩 `[정적: 값]`

| 자리 | 값 `{Offset, Length}` | 읽는 법 |
|---|---|---|
| `C+0x4d0` | `{−0x4D0 (0xfffffb30), S0 − C}` | 메모 **전체**: 목차 자리 (`X`) 에서 `0x4D0` 앞 (= `C`) 부터, 원래 `RSP` 까지 |
| `C+0x4d8` | `{−0x4D0, 0x4D0}` | **기본 메모**: `C` 부터 `0x4D0` |
| `C+0x4e0` | `{A − X, S0 − A}` | **별지**: `A` 부터 `S0` 까지 |
| `C+0x4e8` | `{0, 0}` | 넷째 칸 — 함수 앞머리 `+0x60 XORPS` · `+0x63` · `+0x6a` 가 0 으로 지운 것 (flow-writer) |

- `Offset` 은 목차 자리 `X` 에서 잰 거리로 읽힌다 — `−0x4D0` 이면 `C`, `A − X` 면 별지 시작.
- **XSTATE 가 없으면 `A = S0`** 다 (2.10 표). 그래서 셋째 칸은 `{S0 − X, 0}` (길이 0) 이 된다. 최소 규칙 5 의 `{S0−X, 0}` 이 바로 이 꼴이다. 표와 규칙이 다른 말을 하는 게 아니다.
- 만들기 `+0x60 ~ +0x6a` (0) · `+0x460 ~ +0x49c`, 쓰기 `+0x534 ~ +0x545` (16바이트 두 번).
- 별지가 없어도 이 0x20 바이트는 **늘 쓴다** `[정적]`. 읽는 쪽이 체크 칸 비트 `0x40` 만 보고 건너뛰는지는 Q148b ①.

### 2.12 쓰는 차례 — 찢어진 데 없나 보고, 한 번에 내려놓기

#### 차례 `[정적]` (모두 `__try` 안, 2.14)

1. `+0x4ab` (ret `+0x4b0`) **`ProbeForWrite(C, S0 − C, 16)`** — 쓸 자리 전체를 먼저 확인.
2. 머신 프레임 `Rsp` · `Rip` (`+0x4b4 ~ +0x4c3`).
3. `+0x4cc` (ret `+0x4d1`) `KeCopyExceptionRecord(E, 레코드)` — 사고 기록 카드.
4. (XSTATE 일 때) XSAVE 머리 0x40 (`+0x4da ~ +0x502`).
5. `+0x52f` (ret `+0x534`) `RtlpCopyExtendedContext(1, X, 설명 [RBP+0x140], 이름표, 커널 확장 문맥)` — 메모 (`C`) 와 별지 나머지.
6. 목차 쪽지 0x20 (`+0x534 ~ +0x545`).

- 쓰는 손 `[정적]`: `ProbeForWrite` · `KeCopyExceptionRecord` (안쪽 메모리 복사 — 같은 주소 이름 4개, 후보 `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) · `RtlpCopyExtendedContext`, 그리고 `KiDispatchException` 이 직접 `MOV`/`MOVUPS`. **`KiCopy*` 는 안 쓴다.**

#### `ProbeForWrite(주소, 길이, 정렬)` — 무엇을 보나 `[정적]`

- 길이 0 → 끝.
- 정렬이 어긋나면 → `+0x67 ExRaiseDatatypeMisalignment` (`0x80000002`).
- 끝 주소가 감기거나 `≥ 0x7fffffff0000` → `+0x61 ExRaiseAccessViolation` (`0xC0000005`).
- 아니면 쪽마다 한 바이트 읽고 다시 쓴다 (`+0x40 ~ +0x56`). 아직 안 올라온 스택 쪽은 여기서 `MmAccessFault` 가 올려 주는 것으로 보인다 `[추측]`.
- `C` 는 계산상 늘 16 정렬이다 (`0x590` 이 16의 배수).
- 이 셋 중 하나라도 걸리면 **새 사고** 가 난다 → 2.14 처리기로 간다.

#### 비유

- 가게 작업대 아래쪽에 빈 쪽을 잡고 **찢어진 데 없나** 본다 (run42 해설의 그 그림).
- 그다음 관리사무소 작업대에서 써 둔 묶음을 **한 번에** 옮겨 놓는다.
  - 순서: 맨 아래 표지 → 기록 카드 → (별지 머리) → 메모와 별지 → 목차 쪽지.

### 2.13 출발 메모 넷만 고치고 `IRETQ` — 사고 처리 창구에서 시작 (질문 3)

#### 바뀌는 칸 `[정적]` (첫 기회 · 디버거 없음 · 64비트 유저)

| 출발 메모 칸 | 새 값 | 줄 | 원래 값 |
|---|---|---|---|
| `EFlags` (`+0x178`) | **TF (비트 8) 지움** | `+0x3e3 CLI` · `+0x3e4 BTR [R12+0x178],8` | 나머지 비트 그대로. 메모 `EFlags` (`C+0x44`) 는 지우기 **전** 사진 |
| **`Rsp` (`+0x180`)** | **`C`** (= `X − 0x4D0`) | `+0x571 LEA R10,[RDI-0x4d0]` · `+0x578` | `S0` |
| **`SegCs` (`+0x170`)** | **`0x33`** | `+0x580 LEA EAX,[RBX+0x32]` (`RBX = 1`) · `+0x583` | 이미 `0x33` |
| **`Rip` (`+0x168`)** | **`PsNtdllExports+0x20`** = `KiUserExceptionDispatcher` | `+0x58c` · `+0x593` | 사고 자리 (메모·표지에 이미 복사됨) |
| `SegSs` · `Rax ~ R11` · `Rbp` · `Xmm0 ~ 5` · 나머지 | **그대로** | — | 입구가 만든 값 |

- **TF** — `EFlags` 비트 8, 한 명령마다 멈추게 하는 "한 걸음" 깃발로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 왜 지우는지는 근거 문서에 이유가 없다. 짐작 `[추측]`: 사고 처리 창구 코드가 한 걸음마다 또 `#DB` 로 멈추지 않게 하려는 것으로 보인다.

#### 그다음 `[정적]`

- 역시 `__try` 안:
  - `+0x56c KePopulateContinuationContext(원래 Rip)` — CET 스레드 (`KTHREAD +0x74` 비트 20) 만 그림자 스택에 한 칸. 비 CET 는 `+0x15 RET`.
  - `+0x59e KiSetupForInstrumentationReturn` — 프로세스 계측 콜백 (`[프로세스+0x168]`) 이 있을 때만 `Rip` 를 콜백으로, 원래 `Rip` 는 `R10`.
  - test.exe 는 둘 다 안 탄다고 본다 (계측 콜백은 `[동적]`, Q104 동적 8).
- `+0x649 [RBP+0xc] = 0` · `JMP` — **`KeContextToKframes` 를 건너뛴다.** 그건 "처리됨" 길 (`+0x1b8`) 에서만 쓴다.
  - 뜻: 메모를 출발 메모에 되돌려 쓰지 **않는다**. 출발 메모는 위 넷만 바뀐 채 나간다.
- 나가는 길:
  - `KiExceptionDispatch +0x145` 로 돌아와 개인 서랍을 되살림 → `+0x1b4` 유저인가 → `+0x1e9 KiInitiateUserApc` 루프 → `+0x22a KiRestoreSetContextState` (헤더 비트 27, run80) → `+0x262 KiRestoreDebugRegisterState`
  - → 조각 `FUN_1406c2b18` (= `+0x418`): `+0x49a JMP KiKernelExit` (KVA 그림자 켜짐) 또는 **`+0x4b1 SWAPGS` · `+0x4b4 IRETQ`**.
- **`SYSRET` 이 아니라 `IRETQ`** `[정적]`.
  - run56 · run77 해설의 그림으로: `SYSRET` 은 **돌아갈 문 쪽지** (`RCX`) 대로 나가는 보통 출구다.
  - 여기는 그 쪽지 대신 출발 메모대로 **순간 이동** 한다. 그리고 `RCX` 같은 레지스터도 사고 순간 값 그대로다 (아래, run 문서).

#### `KiUserExceptionDispatcher` 가 시작할 때 `[정적]` (ntdll `.asm` `+0x0 ~ +0x2e`)

- 입력은 **`RSP` 하나** (= `C` = 사고 순간 메모).
- 첫 줄 `CLD`. `Wow64PrepareForException` 이 0 이 아니면 부른다 (`+0x1 ~ +0x1a`).
- `+0x1c MOV RCX,RSP` · `+0x1f ADD RCX,0x4f0` (카드) · `+0x26 MOV RDX,RSP` (메모) → `+0x29` (ret `+0x2e`) `RtlDispatchException(카드, 메모)`.
  - `RCX`·`RDX` 는 입력이 아니라 **바로 덮는다**.
- `RIP` = 진입점, `CS` = `0x33`. 그 밖 레지스터는 사고 순간 값 그대로다 (그 뒤 안 읽음).
- `RtlDispatchException` 이 거짓이면 `+0x4b XOR R8B,R8B` · `+0x4e` (ret `+0x53`) `NtRaiseException(…, 0)` — 둘째 기회 (2.15). 풀이는 run82.
- 비유: 직원은 순간 이동해서 사고 처리 창구 앞에 선다. 손엔 아무것도 없다. 창구는 "네 작업대 맨 위를 봐" 하고, 거기서 메모 (`RSP`) 와 카드 (`RSP+0x4f0`) 를 집어 든다.

#### run42 (쪽지) 와 나란히

| | 유저 APC (run42) | 사고 (이번) |
|---|---|---|
| 메모를 만드는 곳 | 가게 작업대에 바로 | 관리사무소 작업대 → 한 번에 복사 |
| 체크 칸 | `0x10001b` (CET `0x10005b`) | `0x10001f` (비트 23 이면 `0x10005f`) |
| 쓸 자리 확인 | `ProbeForWrite` | `ProbeForWrite` |
| 출발 메모 `Rip` | `PsNtdllExports+0x28` `KiUserApcDispatcher` (쪽지 읽는 창구) | `PsNtdllExports+0x20` `KiUserExceptionDispatcher` (사고 처리 창구) |

- 같은 틀 `[정적]`: "원래 문맥을 유저 스택에 메모 꼴로 남기고, 출발 메모를 ntdll 창구로 바꿔 붙인다".

### 2.14 쓰기가 실패하면 — 스택 넘침만 한 번 더 (질문 3 끝)

#### 예외 보호 구역 `[정적]`

- 유저 스택에 쓰는 구간이 `__try` 안이다 (결과서가 `.pdata` scope table 로 읽음):
  - 범위 `[+0x41a, +0x5d1)` · `[+0x62a, +0x652)`.
  - 필터 `KiDispatchException$filt$0` — 새 사고 카드를 `[RBP+0xa0]` 에 복사하고 **늘 1** (늘 처리기로).
  - 처리기 = **`+0x652`** (`+0x652 ~ +0x6cf`, 126바이트).
- 이 처리기는 `.asm` 에 "읽지 않은 틈" 으로만 나온다. 결과서가 `.pdata` → `UNWIND_INFO` → `C_SCOPE_TABLE` 을 직접 읽어 자리를 찾고 objdump 로 풀었다.
  - (도구 메모: `__except` 본문을 scope table `JumpTarget` 으로 붙이자는 하네스 제안 ③ 에 보탬, plan.md.)

#### 처리기 — 단계별로 `[정적]`

1. `[RBP+0xc]` 가 켜져 있으면 `SpecialApcDisable` 을 되돌린다.
2. 새 사고 코드가 **`0xC00000FD` (스택 넘침)** 이면:
   - 원래 카드를 **스택 넘침 카드로 덮는다** (`ExceptionAddress` 는 원래 것 유지).
   - `R15 = S0` 로 되돌려 **`+0x419` 부터 한 번 더** (배치를 다시 계산해 씀).
3. **그 밖** (접근 위반 `0xC0000005` · 정렬 `0x80000002` · 가드 페이지 `0x80000001` 등) 이면:
   - `EDI = 1` 로 **`+0x7a5` 둘째 기회** 로 곧장. 유저 처리기 기회 없음 (2.15).

#### 그래서

- **상태 코드를 돌려주는 길은 없다** `[정적]`. 원래 사고 코드로 둘째 기회에 가서 가게가 닫힌다.
- 잘못된 `RSP` (감김, `≥ 0x7fffffff0000`, 16 정렬 아님) 도 같다: `ProbeForWrite` 가 새 사고를 일으킴 → 이 처리기 → 둘째 기회.
- 되풀이를 막는 줄은 없다. 두 번째 시도에서도 `0xC00000FD` 면 또 처리기로 간다 `[동적]` (Q104 동적 6 · Q155 ⑥).
- 비유:
  - 묶음을 내려놓다가 **"작업대가 꽉 찼다" (스택 넘침)** 면, 사고 이름을 "작업대 꽉 참" 으로 바꿔 적고 한 번 더 내려놓아 본다.
  - **다른 이유로** 못 내려놓으면, 가게 담당에게 묻지 않고 곧장 마지막 처리로 간다.
- PE_emulator: 지금 "멈춤" → 둘째 기회 (종료) 로 바꾸면 진짜와 같다 (run 문서 표).

### 2.15 아무도 안 받으면 — 둘째 기회, 가게 문 닫기 (질문 4)

#### 들어오는 길 — `NtRaiseException(…, FirstChance = 0)` `[정적]`

1. ntdll 사고 처리 창구에서 `RtlDispatchException` 이 거짓 → `KiUserExceptionDispatcher +0x4b XOR R8B,R8B` · `+0x4e NtRaiseException` (풀이는 Q148b).
2. **`NtRaiseException`** (손으로 쓴 asm): `RCX` 카드 · `RDX` 메모 · **`R8B` 첫 기회**.
   - `+0x15` 출발 메모 `Rax` 칸을 0 으로 (run77 — 성공으로 나가게).
   - `+0x85 [RSP+0x20] = R8B` → `+0x91` (ret `+0x96`) `KiRaiseException`.
3. **`KiRaiseException`**:
   - 유저 호출이면 (`+0x4a`) `+0x71` 개수를 읽어 **`> 15` 면 `0xC000000D`** (`+0x79 · +0x7c · +0x9c`).
   - 아니면 `+0xb3` 카드 `0x20 + 8 × 개수` 만 커널로 복사.
   - `+0xd8` (ret `+0xdd`) `KyRaiseException` — 유저 메모를 읽고 검증, `+0x16f ExceptionActive = 1`, `+0x18d KeContextToKframes` 로 출발 메모에 써 넣는다.
   - `+0xe6 BTR` 비트 28 지움 (2.4) → `+0xfe` (ret `+0x103`) `KiDispatchException`.
4. **본 담당, 둘째 기회** (`[RBP+0x1d0] == 0`): 첫 기회 갈래를 건너뛰고, 메모·분류 담당까지는 같다 → `+0x250 JZ +0x7a5`.

#### `+0x7a5` 부터 — 마지막 처리 `[정적]`

1. `+0x7ae` (ret `+0x7b3`) **`DbgkForwardException(레코드, 1, 1)`** — 디버그 포트 (가게 감독관 연락선).
   - 없으면 거짓.
   - 둘째 기회면 그 안에서 먼저 프로세스 `Flags3.Crashed` (비트 2) 를 켜고 `PspRecordCrashedProcessIntoBlackbox` · `PsSetProcessTelemetryAppState(…, 4)` 를 부른다.
2. `+0x7c3` (ret `+0x7c8`) **`DbgkForwardException(레코드, 0, 1)`** — 예외 포트 (`[프로세스+0x240]`) 쪽 (사고 신고센터 연락선).
   - 예외 포트가 0 이면 (`DbgkForwardException` 안) `+0x39c DbgkpSendErrorMessage` 로 보내고, 그 반환이 0 이상이면 "처리됨" (참).
   - 보내기 실패면 거짓.
3. 둘 다 거짓이면 → **`+0x7d0 EDX = [레코드]`** (사고 코드) · **`+0x7d3 RCX = −1`** (지금 프로세스) · **`+0x7d7` (ret `+0x7dc`) `ZwTerminateProcess`**.

- 끝 코드 = **사고 코드 그대로** `[정적]`. 응급 코드는 이미 분류 담당이 푼 뒤라 진짜 코드다.
- `ZwTerminateProcess(-1, …)` 은 run79 의 `NtTerminateProcess` **자기 가게 길** 이다 (`+0x243 PspTerminateAllThreads` 뒤 `+0x161 PspExitThread`). 그래서 `+0x7dc` 로 돌아오는 건 실패 때뿐이다 `[정적, run79]`.
- 예외 포트가 있는 보통 데스크톱 (WER) 이면 2 에서 "처리됨" 으로 끝날 수 있다. 그 뒤 가게가 어떻게 끝나는지는 이번 범위 밖이다 (Q104 동적 5).
  - **WER** (Windows Error Reporting) — "문제가 발생했습니다" 같은 오류 보고를 맡는 Windows 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 사고 **신고센터**.

#### 비유 — 한 이야기로

1. 가게 담당 아무도 손을 안 들었다. 직원이 관리사무소 창구에 "못 받았어요" 를 다시 낸다 (`NtRaiseException(…, 0)`).
2. 관리사무소는 받은 카드가 이상하지 않은지 보고 (덧붙임 15개 넘으면 거절), 직원 메모를 출발 메모에 다시 붙인다.
3. 본 담당이 마지막 처리를 한다:
   - 가게 감독관 연락선 — 없음.
   - 사고 신고센터 연락선 — 없음.
4. 그러면 **가게 문을 닫는다.** 문 앞 안내문엔 사고 이름을 그대로 적는다.

#### PE_emulator 에게 (run 문서 표)

- 은퇴 뒤 진짜 ntdll 이 `NtRaiseException(…, FALSE)` 를 부르면, 엔진은 (디버거·WER 이 없으니) `ZwTerminateProcess(-1, ExceptionCode)` 로 가면 된다 (run79 끝내기 길).
- `NtRaiseException(…, TRUE)` 로 다시 오면 (`RtlRaiseException`) 유저 메모를 출발 메모에 써 넣고 배치를 새로 만든다 (규칙 8).

### 2.16 `__fastfail` 은 바로 끝 (질문 5)

#### 걸음 `[정적]`

1. `KiRaiseSecurityCheckFailure +0x0 SUB [RSP],2` — 돌아올 `Rip` 를 **`int 0x29` 자기 자리** 로. (`CS = 0x23`, WoW64 면 `Rip` 위 32비트 지움 `+0x5 ~ +0xd`.)
   - 2 를 빼는 건 `int 0x29` 가 2바이트 명령이라서로 보인다 `[추측]` (일반적으로 알려진 설명).
2. 조각 끝: `+0x34e R9 = [RBP−0x48]` (출발 메모 `Rcx` = 실패 코드) · `+0x352 ECX = 0xC0000409` · `+0x357 EDX = 1` · `+0x35c R8 = Rip` → `+0x363` (ret `+0x368`) `KiFastFailDispatch`.
3. `KiFastFailDispatch +0x6b` — 카드 `ExceptionFlags` = **1** (EXCEPTION_NONCONTINUABLE, 계속 불가).
4. `+0x89 ~ +0x98` 이전 모드:
   - 커널이면 `+0xad KiBugCheckDispatch(0x139)`.
   - 유저면 **`+0xb2 [RSP+0x20] = 0` (첫 기회 0)** → `+0xc1` (ret `+0xc6`) `KiDispatchException`.
5. 본 담당은 `+0x250` 에서 곧장 `+0x7a5` (2.15) → `ZwTerminateProcess(-1, 0xC0000409)`.

#### 그래서

- 사고 처리 창구 (`KiUserExceptionDispatcher`) 에 **한 번도 안 닿는다** `[정적]`. 유저 처리기 (VEH·SEH) 는 받을 기회가 없다.
- 카드 `[0]` = 실패 코드 (`RCX`), 개수 1.
- 같은 길을 쓰는 입구 (이름만): `KiControlProtectionFault` (유저면 `0xC0000409`, `R9 0x39`), `KiBoundFault` 한 갈래 (`R9 0x1c`).
- 비유: **비상 정지 버튼.** 카드에 "계속 불가" 도장을 찍고, 가게 담당에게 묻는 단계 (첫 기회) 를 아예 건너뛰고 문을 닫는다.

#### 지도와 닿는 곳 `[정적]`

- `LdrpInitializeProcess` 강제 종료 `__fastfail(3)` 2곳 · `__fastfail(0x45)` 1곳 (run18·run19), `LdrpInsertDataTableEntry +0x46 / +0x4b INT 0x29` (run35).
- 그 **커널 쪽 끝** 이 이 길이다. flow.md 2.4 "일찍 끝나는 길" 끝에 한 마디가 붙었다.
- test.exe 는 안 탈 것이다 `[추측]` (flow.md 2.4).
- PE_emulator: `int 0x29` 는 인터럽트 훅으로 받아 `Rcx` 를 실패 코드로 하고 곧장 종료 (`0xC0000409`) 하면 진짜와 같다.

### 2.17 이 VM 은 "별지 쪽" (`0x10005f`) 으로 보인다 — flow-writer 덧붙임 ①

#### 단계별로

1. `[정적]` `KiDispatchException +0x2d4 TEST [KeFeatureBits],0x800000` — 비트 23 하나로 체크 칸이 `0x10001f` / `0x10005f` 로 갈린다 (2.8).
2. `[정적]` (run70, 짐 지도 7.6.10) 이 비트는 부팅 때 `KiInitializeKernel +0x2e5 BTS RBX,0x17` 이 KUSD `+0x3d8` (`XState.EnabledFeatures`) ≠ 0 일 때 켠다. 같은 비트가 KUSD `ProcessorFeatures[17]` (XSAVE_ENABLED) 가 된다.
3. `[관찰]` run44 사진 (KUSD 줄):
   - `+0x285` (= `ProcessorFeatures[17]`) **1**
   - `+0x3d8` `EnabledFeatures` **`0xe7`**, `+0x3e0` `EnabledVolatileFeatures` `0xe7`
   - `+0x3e8` `Size` **`0x980`**, `+0x3ec` `ControlFlags` **3** (`OptimizedSave`·`CompactionEnabled`)
   - (`ms/logs/04_baggage-read_supp_K1.txt:327 · 348 · 349`, `_P2.txt:4165 · 4186 · 4187`. 칸 이름은 `pdb_struct.py` `_XSTATE_CONFIGURATION`.)
4. 그래서 → 비트 23 이 켜졌을 것이고, 이 VM 의 유저 사고 메모는 **`0x10005f` + 별지** 쪽이다 `[추측: 사진 간접]`.
5. 아직 확인 아님:
   - `KeFeatureBits` 값 자체는 사진에 없다 → Q104 동적 1 (짐 지도 7.29 1 과 같은 읽기).
   - 별지 길이는 `RtlGetExtendedContextLength3` (`+0xe3`) 가 정하는 런타임 값이라 이번엔 숫자로 못 정했다.

- 결과서는 `0x800000` 을 "XSAVE 계열" `[추측]` 으로만 적었다. flow-writer 가 run70 의 줄로 이었다 (2.21 ②).
- 비유: 관리사무소가 모든 가게 앞에 붙인 공용 게시판에 **"별지 기능 있음" 체크** 가 돼 있다 (`ProcessorFeatures[17]` = 1). 그 체크는 관리사무소 **내부 장부** (`KeFeatureBits`) 의 같은 칸을 베낀 것이다. 그러니 내부 장부 칸도 켜져 있을 것 — 사고 메모엔 별지가 붙는 쪽.

#### PE_emulator 에게 (run 문서 덧붙임 ①)

- 규칙 2 의 `0x10001f` 는 **"XSAVE 를 흉내 내지 않는 쉬운 길"** 이다. 이 길도 커널 코드에 있다 (비트 23 꺼짐 갈래) `[정적]`.
- 진짜 VM 과 같게 하려면 `0x10005f` + XSAVE 꼬리 (규칙 3 에 `A = (S0 − 길이) & ~0x3F`).
- KUSD 를 진짜 값 (`0xe7`) 으로 두면서 `0x10001f` 로 쌓아도 ntdll 쪽이 괜찮은지는 **Q148b**.

### 2.18 헷갈리기 쉬운 곳 ① — syscall 안 포인터 실패, 그리고 `Process.cpp:1953` (질문 6)

질문 6 엔 **서로 다른 이야기 둘** 이 붙어 있다. 천천히 나누자.

#### (가) 메모리 syscall 이 잘못된 유저 포인터를 만나면 — 상태 코드를 돌려주는 게 기본 `[정적]`

1. 그 사고는 **커널 모드에서** 난다. `KiPageFault` 의 이전 모드 = 커널 (`SegCs & 1 = 0`).
2. 본 담당 `KiDispatchException +0x16e` 에서 유저 갈래로 **안 간다** → `+0x174 ~ +0x19c KdTrap` → `+0x367 ~ +0x372 RtlDispatchException(레코드, 문맥)` 이 **커널 프레임의 SEH** 를 찾는다 → 그 `__except` 로 가서 syscall 이 돌아온다.
3. 없으면 `+0x37a ~ +0x395 KdTrap` 뒤 `+0x3b1 ECX = 0x1e` · `+0x3b6 KeBugCheckEx` (KMODE_EXCEPTION_NOT_HANDLED).
4. 어느 쪽도 유저 처리기 (사고 처리 창구) 는 **안 거친다**.

- 예 `[정적]` (결과서 `.pdata`): `NtAllocateVirtualMemory` 의 `__try` `[+0x79, +0xc2)` · `[+0x257, +0x261)`.
  - 필터 `$filt$0` 는 `PreviousMode (+0x232) ≠ 0` 이면 1 — **유저가 부른 syscall 안에서 난 사고만** 자기 처리기가 잡는다. (`$filt$1` 은 같은 꼴 `[추측]`.)
  - 처리기 본문이 상태를 어떻게 돌려주는지는 안 읽었다 `[추측]` (Q155 ③, Q104 동적 7).
- 비유: 관리사무소 직원이 **창구 업무 중에** 넘어진 것이다. 관리사무소 **자기 비상구** (커널 SEH) 로 나와서 손님에게 "처리 실패" **답 봉투** 를 준다. 가게의 사고 처리 창구엔 가지 않는다.
- PE_emulator: 메모리 syscall 이 잘못된 포인터에 상태 코드를 돌려주는 건 **맞다**. 다만 코드는 `0xC0000005` 하나가 아니다 (결과서: 정렬이면 `0x80000002`, 페이지 읽기 실패면 `0xC0000006`).
- 참고 (이름만): `KiSystemServiceUser +0xa1` (ret `+0xa6`) 도 `KiExceptionDispatch` 를 부르지만 `ECX = 0xC000001C` 라 포인터 실패와 무관하다.

#### (나) `Process.cpp:1953` 주석은 **다른 일** 의 이야기다

- 주석: "`STATUS_ACCESS_VIOLATION` … 진짜는 SEH `+0x2ab` → 유저 예외 `KiDispatchException` [정적] `PspInitializeThunkContext.asm` `+0x37b`, `+0x3c3`".
- 이 줄은 PE_emulator 의 `RtlCopyContext` 흉내 안에 있다. 곧 **프로세스 시작 때 커널이 유저 스택에 원래 메모를 맡기는 `PspInitializeThunkContext`** 이야기다 (run16 해설의 **사물함** 장면). 메모리 syscall 이야기가 **아니다**.

#### `PspInitializeThunkContext` 실패 갈래 — 단계별로 `[정적]`

1. `__try` `[+0x22f, +0x2ab)`, 필터 상수 1, 처리기 `+0x2ab` (결과서 `.pdata`, 15바이트 본문은 안 읽음) — 주석의 `+0x2ab` **맞다**.
2. `EBX` 를 정하는 줄: `+0x26e` (`RtlInitializeExtendedContext2` 반환) · `+0x285` (`RtlCopyContext` 반환) · `+0x2db` (`PspSetContextThreadInternal` 반환) · 처리기 (무엇을 넣는지 `[공백]` — 예외 코드 `[추측]`).
3. `+0x2fa TEST EBX,EBX` · `+0x2fc JS +0x37b` — 음수 (실패) 면.
4. `+0x37b ~ +0x391` 카드를 만든다: 코드 = `EBX`, 플래그 0, 링크 0, 주소 = **`PsNtdllExports+0x8`** (`LdrInitializeThunk`, `+0x37f` · `+0x386`), 개수 0.
5. `+0x39b` · `+0x3a8 PspGetBaseTrapFrame` — 출발 메모 · 개인 서랍 자리.
6. **`+0x3b4 [RSP+0x20] = 0` — 첫 기회 0.** `+0x3b9 R9B = R14B` (= 1 유저, `+0x121 MOV R14D,1`).
7. `+0x3c3` (ret `+0x3c8`) `KiDispatchException` → 둘째 기회 (2.15) → `ZwTerminateProcess(-1, EBX)`.

- 그래서 →
  - 주석의 **줄 번호** (`+0x2ab` · `+0x37b` · `+0x3c3`) 는 **맞다**.
  - "유저 예외 `KiDispatchException`" 도 **반은 맞다** — 거기로 가는 건 맞다.
  - 다만 **첫 기회 0** 이라 유저 처리기 기회 없이 **둘째 기회 = 가게 문 닫기** 다. 새 스레드는 `LdrInitializeThunk` 에 가기도 전에 끝난다.
  - "에뮬레이터 규칙: 멈춘다" 는 결과가 같다. 주석은 "첫 기회 0 인 유저 예외 = 종료" 로 고치면 정확하다 (결과서).
  - **고치는 건 PE_emulator 몫** 이다. 우리 문서가 아니다.

#### 두 이야기 나란히

| | (가) 메모리 syscall 안 포인터 실패 | (나) `PspInitializeThunkContext` 실패 |
|---|---|---|
| 언제 | 은퇴 뒤 아무 syscall 에서나 | 프로세스·스레드 시작 때 (원래 메모 맡기기) |
| 커널이 하는 일 | 커널 모드 사고 → `KiDispatchException` **커널 갈래** (`+0x16e`) | 실패 상태 `EBX` 로 카드를 **새로 만들어** `KiDispatchException` 에 유저·**첫 기회 0** 으로 |
| 결과 | 커널 SEH → syscall 이 **상태를 돌려줌** | 둘째 기회 → `ZwTerminateProcess(-1, EBX)` |
| 유저 처리기 | 안 거침 | 안 거침 |
| PE_emulator | 상태 코드 돌려주기 **맞음** (코드 종류만 더) | "멈춤" 과 결과 같음, 주석만 고칠 것 |

- 비유로 한 번 더:
  - (가) 관리사무소 직원이 창구 일 하다 넘어짐 → 관리사무소 비상구 → 손님에게 "실패" 답 봉투.
  - (나) 새 직원 사물함에 메모를 넣다 실패 → 사고 카드를 쓰긴 쓰는데 "첫 기회 없음" → 곧장 가게 문 닫기.

#### 우리 지도 쪽 — run16 은 그대로

- run16 의 ③ 표 6번 "쓰기 실패면 SEH 착지점 `+0x2ab` (원본 바이트) → 오류 길" 과 9번 `+0x2f5` (ret `+0x2fa`) 는 **맞다**.
- "오류 길" 이 무엇인지만 비어 있었고, 이번에 위 (나) 로 채웠다.
- 그래서 run16 문서는 고치지 않았고 정정 안내도 안 넣었다. flow.md 2.3 6번 아래에 (run81) 한 줄만 붙었다.

### 2.19 헷갈리기 쉬운 곳 ② — run48 정정

- **전** (run48 "① 에서 읽는 자리" 표 `PsNtdllExports` 줄):
  - "`PspInitializeThunkContext +0x1a1 · +0x37f` (`+0x8` → 새 스레드 `Rip`)"
- **후** `[정적]` (flow-writer `.asm`, 오케스트레이터 대조):
  - `+0x1a1` **만** 새 스레드 `Rip` (`ctx2.Rip`, run16).
  - `+0x37f` 는 실패 갈래 (`+0x2fc JS +0x37b`) 에서 같은 `+0x8` (`LdrInitializeThunk`) 을 **사고 카드 `ExceptionAddress`** (`+0x386 [RBP+0x40]`) 로 읽는 줄이다 (2.18 (나) 4번).
- 왜 헷갈리기 쉬운가:
  1. 두 줄 모두 벽 주소록의 **같은 칸** (`+0x8`, 오리엔테이션실 주소) 을 읽는다.
  2. 두 줄 모두 **같은 함수** `PspInitializeThunkContext` 안이다.
  3. 그런데 **쓰는 곳이 다르다** — 하나는 새 직원 출발 메모 `Rip`, 하나는 (실패할 때) 사고 카드의 "사고 장소" 칸.
- 반영: run48 문서 맨 위에 정정 안내 한 줄. 짐 지도 7.6.2 짝 표의 같은 줄 (옛 글 "`+0x1a1 · +0x37f` → 트랩 `Rip`") 도 고쳤다.
- 비유: 벽 주소록의 "오리엔테이션실 주소" 칸을 두 번 베낀다. 한 번은 새 직원 **출발 메모** 에, 한 번은 실패했을 때 **사고 카드의 장소 칸** 에. run48 은 둘 다 출발 메모로 적었었다.

### 2.20 PE_emulator 에게 — 최소 규칙 아홉 (run 문서 그대로, 한 줄씩)

조건: CPU 예외가 나서 은퇴 뒤 Unicorn 에서 **유저 모드** 코드가 멈췄을 때. 디버거·WER 없음, 64비트 프로세스, 비 CET, 계측 콜백 없음.

| # | 규칙 (줄임) | 이 글 |
|---|---|---|
| 1 | 코드·주소·인자 정하기 — `#PF` `0xC0000005` (개수 2, `[0]` 0/1/8, `[1]` 주소), 특권 `0xC0000096` (개수 0), `#UD` `0xC000001D`, `#DE` `0xC0000094`/`0xC0000095`, `int3` `0x80000003` (주소 = `int3` 자리, 개수 1, `[0] = 0`, **문맥 `Rip` 도 `int3` 자리**), `ExceptionFlags = 0` | 2.6 · 2.7 |
| 2 | 메모 만들기 — `ContextFlags = 0x10001f`, `SegCs 0x33`, `SegSs`·`SegDs`·`SegEs`·`SegGs` `0x2b`, `SegFs 0x53`, `EFlags`/`Rsp`/`Rip` = 순간 값 (`int3` 만 `Rip − 1`), `MxCsr`·`XMM`·`FltSave` 채움, `P1Home ~ P6Home` 안 씀 | 2.8 · 2.11 · 2.17 |
| 3 | 배치 계산 — `S0 = RSP`, `M = (S0 − 0x28) & ~0xF`, `E = M − 0xA0`, `X = E − 0x20`, `C = X − 0x4D0` | 2.10 |
| 4 | 쓸 수 있나 — `C` 16 정렬, `[C, S0)` 끝 `< 0x7fffffff0000`, 쓰기 가능 (안 올라온 스택 쪽은 올려 줌). 스택 넘침 `0xC00000FD` 면 그 코드로 바꿔 3 부터 한 번 더, 그 밖은 둘째 기회로 곧장 (7) | 2.12 · 2.14 |
| 5 | 유저 스택에 쓰기 (차례 상관없음) — `C` (`CONTEXT`) · `C+0x4d0` (`CONTEXT_EX`: `{−0x4D0, S0−C}, {−0x4D0, 0x4D0}, {S0−X, 0}, {0, 0}`) · `C+0x4f0` (카드: 0x98 을 0 으로, 앞 0x20 + 개수 × 8) · `C+0x590` `Rip` · `C+0x5a8` `Rsp` (= `S0`) | 2.11 |
| 6 | 레지스터 바꾸기 — `RSP = C`, `RIP = KiUserExceptionDispatcher` (`PsNtdllExports+0x20`, ntdll rva `0x164e10`), `CS = 0x33`, `EFLAGS = 순간 값 & ~0x100`. **다른 레지스터는 그대로** (`RCX`/`RDX` 필요 없음). 그 뒤 Unicorn 을 이어 돌림 | 2.13 |
| 7 | 둘째 기회 (`NtRaiseException(…, FALSE)` · `__fastfail` · 쓰기 실패) — 유저 쪽으로 안 보내고 `ZwTerminateProcess(-1, ExceptionCode)` (run79) | 2.14 · 2.15 · 2.16 |
| 8 | `NtRaiseException(레코드, 문맥, TRUE)` — 개수 `≤ 15` 아니면 `0xC000000D`, 코드 비트 28 지우고, 유저 메모를 출발 메모로 읽어 들인 뒤 1 ~ 6 을 새로 | 2.15 |
| 9 | 안 흉내 내도 되는 것 — 디버거 갈래, `KdTrap`, WoW64, Pico, CET 그림자 스택, 계측 콜백, `KiPreprocessFault` 의 ATL thunk·XSTATE 켜기·보안 PCI | 2.4 · 2.9 · 2.13 |

- 규칙 3 · 5 는 **XSTATE 없음** (`A = S0`) 꼴이다. 진짜 VM 처럼 `0x10005f` 로 가려면 2.17.
- flow-writer 덧붙임 넷 (규칙에 넣을지는 PE_emulator 판단):
  - ① 이 VM 은 `0x10005f` 쪽으로 보인다 `[추측: 사진 간접]` (2.17).
  - ② `_CONTEXT_EX` 는 공개 PDB 에 없다 — 칸 이름 `[추측: 이름]`, 값은 줄에서 읽음 (2.11).
  - ③ test.exe 도 초기화 중 **소프트웨어 예외 하나** 를 낸다: `KERNELBASE!OutputDebugStringA +0x77 RaiseException(0x40010006)` (run71 `[정적]`). 이게 커널 (`NtRaiseException` → `KiDispatchException` 첫 기회) 을 거치는지는 **Q148b**. 거친다면 동적 1·2·5 를 새 샘플 없이 test.exe 로 볼 수 있다 `[추측]`.
  - ④ 둘째 기회를 부르는 유저 줄 = `KiUserExceptionDispatcher +0x4b XOR R8B,R8B` · `+0x4e` (ret `+0x53`) `NtRaiseException` (2.13 · 2.15).

### 2.21 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

- ① `CONTEXT_EX` 넷째 칸 `{0, 0}` 의 출처 = 앞머리 `+0x60 XORPS XMM0,XMM0` · `+0x63` · `+0x6a` 가 `[RBP+0x140 ~ +0x15f]` 를 지운 것. 그리고 `_CONTEXT_EX` 는 공개 PDB 에 없어 칸 이름 `[추측: 이름]` (2.11).
- ② 결과서 `[추측]` "`KeFeatureBits` `0x800000` = XSAVE 계열" → 비트 23 = `KiInitializeKernel +0x2e5` = KUSD `ProcessorFeatures[17]` `[정적]` (run70) + 사진 값 → 이 VM 은 켜짐 쪽 `[추측: 사진 간접]` (2.17).
- ③ `PspInitializeThunkContext` 실패 갈래 `EBX` 출처에 `+0x2db` (`PspSetContextThreadInternal`) 를 더함. 이전 모드 1 = `+0x121`, 프레임 = `+0x39b` · `+0x3a8 PspGetBaseTrapFrame` (2.18).
- ④ 원문 5 의 "`FirstChance` 거짓" 유저 줄 = `KiUserExceptionDispatcher +0x4b` · `+0x4e` (2.13).
- ⑤ `_calls.tsv` 에 `alias:` 로 찍힌 callee 셋 (`KiDispatchException +0x103` · `KiRaiseException +0xb3` · `KeCopyExceptionRecord +0x5c`) 은 하는 일 (스택 늘리기 확인 · 메모리 복사) 로 적고 후보 이름을 붙였다.
- ⑥ 짐 지도 3.15 머리에 "유저 예외 줄" (3절).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 | … run80 까지 | + run81 (입구 여섯 · `KiExceptionDispatch` · `KiDispatchException` · `KiPreprocessFault` 등 처음 뽑음, 모두 depth 0, 새 지도 `_map/KiDispatchException_*` · `KiExceptionDispatch_*`) |
| 기준 — 사진 메모 | run80 줄까지 | + run81 줄: 예외 순간 사진은 없음, run44 KUSD 줄 (`ProcessorFeatures[17]` · `XState`) 만 다시 읽음 |
| 기준 — "예외 착지점은 Ghidra 가 안 따라간다" 메모 | run27 · run28 (`pefile` + 원본 바이트) | + (run81) 처리기 `+0x652 ~ +0x6cf` · `PspInitializeThunkContext +0x2ab` · `NtAllocateVirtualMemory` scope table, `KiUserExceptionDispatcher` `UNWIND_INFO` 를 결과서가 `.pdata` 로 직접 읽음. `_CONTEXT_EX` 는 PDB 에 없어 칸 이름 `[추측: 이름]` |
| 2.3 제목 | … run79 | + run80, run81 |
| 2.3 `PspInitializeThunkContext` 6번 아래 | "오류 길" (무엇인지 비어 있음) | + (run81) **첫 기회 0 인 유저 예외 → 가게 문 닫기** `[정적]`. run16 은 틀리지 않았다 (빈 곳만 채움) |
| 2.4 제목 | … run70 | + run81 |
| 2.4 "일찍 끝나는 길" | `__fastfail` 3곳 (커널 쪽 끝은 안 봄) | + `KiRaiseSecurityCheckFailure` → `KiFastFailDispatch +0xb2` 첫 기회 0 → `ZwTerminateProcess(-1, 0xC0000409)` `[정적]` |
| 3장 **새 절** "CPU 예외를 유저 예외로 넘기기" | — | 비유 (사고 경위서·응급 코드·사고 처리 창구) + 트리 + 배치 표 + `[정적]` 여섯 + 사진 `[관찰 간접]` 하나 + 동적 남은 것. 그림엔 안 넣음 |
| 4장 run80 줄 둘 | 대기열 번호 없음 | **Q154** · **Q153** |
| 4장 새 줄 셋 (run81) | — | 실행 확인 (Q104 VM 단계) · 정적 남은 것 ① ~ ⑦ · 결과서와 다르게 적은 곳·보탠 것·run48 정정 |
| 5장 | run80 줄 빠짐 | run80 줄 채움 + run81 줄 (표 둘) |

태그가 바뀐 곳:

- flow.md 2.3 6번 "오류 길" (빈칸) → 첫 기회 0 → `ZwTerminateProcess(-1, EBX)` `[정적]`.
- `__fastfail` "프로세스를 바로 끝낸다" (run18 해설의 일반 설명 `[추측]`) → 커널 쪽 끝까지 줄로 `[정적]`.
- PE_emulator `[추측]` "메모리 syscall 의 잘못된 포인터 → 유저 예외" → 커널 SEH 로 상태를 돌려주는 게 기본 `[정적]`, 주석 줄은 `PspInitializeThunkContext` 이야기 `[정적]`.
- run48 `PsNtdllExports` 줄의 `+0x37f` "새 스레드 `Rip`" → 실패 갈래 카드 `ExceptionAddress` `[정적]`.
- 결과서 `[추측]` "`KeFeatureBits 0x800000` = XSAVE 계열" → 비트 23 `[정적]` (run70) + 이 VM 은 켜짐 쪽 `[추측: 사진 간접]`.

### 짐 지도 · run48 문서에서 바뀐 것

- 짐 지도 (`baggage.md`): **새 짐 칸 없음, 칸 수·2장 숫자 그대로.** 짐 지도 바퀴가 아니다 — test.exe 정상 길엔 CPU 예외가 없고 `[추측]`, 유저 예외는 은퇴 뒤 일이라서다. 이미 있는 줄에 근거만 붙었다.
  - 7.6.2 벽 주소록 짝 표: 읽는 줄 `KiDispatchException +0x58c` (`+0x20` → 유저 예외 때 출발 메모 `Rip`) 더함, `PspInitializeThunkContext +0x37f` 뜻 고침 (2.19).
  - 7.6.10 `KeFeatureBits`: 읽는 줄 `KiDispatchException +0x2d4` (비트 23 → `0x10005f` / `0x10001f`).
  - 7.29 1: 같은 `dq nt!KeFeatureBits` 읽기로 비트 23 도 닫힌다.
  - 3.15 머리 "유저 예외 줄" (은퇴 뒤라 표에 안 넣음): 바뀌는 칸 넷 (2.13), `ExceptionActive` ← `KyRaiseException +0x16f`.
    - 그리고 7.2 의 `P1~P1a` "누가 썼는지 모름" 칸 중 `FaultAddress` (← `KiPageFault +0x358`) · `FaultIndicator` (← `+0x383`) 의 쓰는 줄 후보 `[추측: 유저 모드 페이지 폴트도 같은 맨 아래 트랩 프레임]` → Q78d4.
  - 판 목록 **서른일곱째판**, 기준 근거 목록·8장 두 표에 한 줄씩.
- run48 문서: 맨 위 정정 안내 한 줄 (2.19).
- run16 문서: 고칠 곳 없음 (2.18 끝).
- 같이 한 일 (이 해설을 쓰며): [run77 해설](run77-thread-wake-해설.md) 맨 위에 "고쳐 읽기 (run80)" 한 줄을 붙였다. run80 에서 정정된 2.13 · 2.14 의 몇 줄을 run80 문서 "정정" 절과 run80 해설 2.16 ① 로 가리키기만 한다.

### 큰 그림에서

- **지도 쪽**:
  - 본론 (CPU 예외 넘기기) 은 흐름도 범위 밖 공용 길이다. 그래도 지도 안 **두 곳의 "끝"** 이 이 길로 이어졌다.
    - 2.3 `PspInitializeThunkContext` 의 "오류 길" → 첫 기회 0 → 가게 문 닫기.
    - 2.4 로더 `__fastfail` → 첫 기회 0 → 가게 문 닫기.
  - 짐 지도 3.15 의 모름 칸 둘 (`FaultAddress` · `FaultIndicator`) 에 쓰는 줄 후보가 생겼다 (Q78d4 에서 봄).
- **같은 틀이 세 번째로 나왔다** — 출발 메모 `Rip` 를 벽 주소록 칸으로 바꿔 ntdll 창구로 보낸다:
  - 시동 (run16): `+0x8` `LdrInitializeThunk` (오리엔테이션실). `PspInitializeThunkContext +0x1a1`.
  - 쪽지 (run42): `+0x28` `KiUserApcDispatcher` (쪽지 읽는 창구).
  - 사고 (이번): `+0x20` `KiUserExceptionDispatcher` (사고 처리 창구).
  - 셋 다 원래 메모는 유저 스택에 남겨 두고 간다 (사물함 · 수첩 · 작업대 위 묶음).
- **PE_emulator 쪽**:
  - "멈춤" 대신 **9단계 규칙** (2.20) 만 넣으면 그 뒤는 진짜 ntdll 이 한다. 쌓는 모양은 ntdll 의 `UNWIND_INFO` 와 `+0x1f ADD RCX,0x4f0` 이 이미 맞물려 있다 (2.10).
  - 둘째 기회 · `__fastfail` · 쓰기 실패는 모두 **같은 끝** (`ZwTerminateProcess(-1, 코드)`) 하나로 묶인다.
  - 메모리 syscall 의 상태 코드 돌려주기는 그대로 두면 된다 (코드 종류만 더).
- **다음**: **Q148b** (run82) — 사고 처리 창구 안. `RtlDispatchException` → VEH → SEH → `NtContinue` / `NtRaiseException`, `RtlRaiseException` 이 커널을 거치나 (test.exe 의 `OutputDebugStringA` 예외와 닿음), kernelbase `UnhandledExceptionFilter`.

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 아래 동적 멈춤점은 **예외가 실제로 나야** 선다. test.exe 정상 길엔 CPU 예외가 없다 `[추측]`. 후보는 셋 (run 문서):
- (가) `OutputDebugStringA` 의 `RaiseException(0x40010006)` 이 커널을 거친다면 그것 — Q148b 가 정하면 동적 1·2·5 를 test.exe 로.
- (나) 조건 없이 다른 프로세스의 예외.
- (다) 아래 Q156 의 새 샘플.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 실제 체크 칸 (`0x10001f` / `0x10005f`), `XState.Length` · 별지 크기, `KeFeatureBits` 비트 23 | 정적으론 갈리는 줄까지. 값은 런타임, `KeFeatureBits` 는 사진에 없음 | **Q104** 동적 1 — `nt!KiDispatchException+0x142` 뒤 `dd @rbp+4 L1` 또는 `KiUserExceptionDispatcher` 입구 `dd @rsp+0x30 L1`, `dq nt!KeFeatureBits L1` (짐 지도 7.29 1 과 같은 읽기) |
| 새 `RSP` 가 정말 `((A − 0x28) & ~0xF) − 0x590` 인지, `RSP+0x590` · `+0x5a8` 값 | 예외가 실제로 나야 섬 | **Q104** 동적 2 — `nt!KiDispatchException+0x5a3` (`dq @r12+0x168 L1` · `+0x180` · `dw @r12+0x170`), `KiUserExceptionDispatcher` 입구 `dq @rsp+0x590 L1` · `dq @rsp+0x5a8 L1` |
| 응급 코드 `0x10000004` → `0xC0000005` 가 바뀌는 순간 | 〃 | **Q104** 동적 3 — `+0x163` 앞뒤 `dd poi(@r14) L1` |
| `#GP` 의 `[1] = −1`, `#DE` 의 `0xC0000095` 갈래, `int3` 의 메모 `Rip` (= `int3` 자리) | test.exe 에 그런 코드가 없다 `[소스]` | **Q156** — 예외를 일부러 내는 작은 프로그램 (잘못된 주소 읽기·쓰기·실행, `ud2`, `hlt`, 0 나누기, `INT_MIN / −1`, `int3`, `__fastfail` 를 `__try` 로 하나씩). 만들지·언제는 **사용자 결정** (Q154 와 같이 정할 만함). 있으면 동적 1·2·3·5·6 도 같이 닫힘 |
| 둘째 기회 `DbgkForwardException(…, 0, 1)` 이 WER 있는 VM 에서 참인지, 그 뒤 어떻게 끝나나. KDNET 이 `+0x2c3 KdTrap` 으로 먼저 가로채나 | 런타임 · 디버거 영향 | **Q104** 동적 5 — `+0x7c8` 뒤 `al`, 거짓이면 `+0x7dc` 종료 상태, `dd nt!KdIgnoreUmExceptions L1` |
| 스택 넘침 되풀이 (`+0x652` → `+0x419`) 가 끝나나 | 처리기에 막는 줄이 없다 | **Q104** 동적 6 · 정적 **Q155 ⑥** |
| 메모리 syscall `__except` 본문 — 상태를 어떻게 돌려주나 | 처리기 본문 안 읽음 `[추측]` | **Q104** 동적 7 · 정적 **Q155 ③** |
| 계측 콜백 · CET 이 정말 안 타나 | 런타임 칸 값 | **Q104** 동적 8 — `dq <프로세스>+0x168 L1` = 0, `KTHREAD +0x74` 비트 20 = 0 기대 (run16 `+0x20e` 갈래와 같은 칸이라 한 번에) |
| `PspInitializeThunkContext +0x2ab ~ +0x2b9` 처리기 15바이트 — `EBX` (= 가게 끝 코드) 에 무엇을 넣나 | `.asm` 틈, 본문 안 읽음 `[공백]` | 정적 **Q155 ②** (objdump 한 번) |
| CPU 예외 번호 ↔ 입구 함수 (IDT 표) | 이름으로 짝지음 `[추측: 이름]` | 정적 **Q155 ④** (Q144 ① 의 IDT 표와 묶을 만함) |
| `KiOpDecode` 해석기 표, `#GP` 디코더 칸 `[RBP+0x28]` 뜻 — 어떤 명령이 `0xC0000096` / `0xC0000005` 로 가나 | 줄기 밖 `[추측]` | 정적 **Q155 ⑤** — Unicorn 이 `#UD`·특권을 못 가르면 PE_emulator 에 필요 |
| `KiPageFault +0x3a6 ~ +0x3c8` GS 베이스 구제 갈래 뜻, WoW64·Pico·CET 갈래 | `[추측]`, 이름만 | 정적 **Q155 ⑦** (PE_emulator 가 x64 만이면 불필요) |
| ntdll 쪽: `CONTEXT_EX` 0x20 을 읽는 쪽 (비트 `0x40` 만 보고 건너뛰나), `0x10001f` + KUSD 진짜 `XState` (`0xe7`) 조합이 괜찮나, 안 쓴 칸 (`P1Home ~ P6Home` · `VectorRegister`) 을 읽나, `RtlRaiseException` 이 커널을 거치나, 짐 지도 3.8 `RtlpContinuationContextMachineFrameEntries` (`KiUserExceptionDispatcher` 의 `UnwindData`, run59) 가 머신 프레임 (`+0x590`) 과 어떻게 이어지나 | 유저 쪽이라 이번 범위 밖 | **Q148b** (run82, 다음 바퀴) — 진행 중인 sast/79 에 이미 보냄 (plan.md) |
| 짐 지도 7.2 `FaultAddress` · `FaultIndicator` 모름 칸이 정말 유저 모드 페이지 폴트가 쓴 값인가 | 후보 줄만 `[추측]` | **Q78d4** |

- 추천 멈춤점 (run 문서, **모두 test.exe 일 때만** 조건 — 커널 멈춤점은 모든 프로세스에 걸린다):
  - `nt!KiExceptionDispatch+0x140` (`rcx` = 카드)
  - `nt!KiDispatchException+0x163` · `+0x4b0` · `+0x571` · `+0x5a3` · `+0x7c8` · `+0x7dc`
  - 유저 쪽 `ntdll!KiUserExceptionDispatcher`

---

## 5. 더 읽기

- run 문서: [`run81-exception-kernel.md`](run81-exception-kernel.md) — "PE_emulator 쪽에서 쓸 것" 질문별 표, **유저 스택에 쌓는 모양 (가) 배치 계산 · (나) `RSP` 기준 표**, 트랩 프레임 바뀌는 칸, **예외 코드 표 (전부)**, 최소 규칙 아홉, flow-writer 덧붙임 ① ~ ④, 호출 지점 표, 질문 1 ~ 6 절, 호출 관계 그림, 결과서와 다르게 적은 곳 · 보탠 것, **정정** (run48 · `Process.cpp:1953` · run16 확인), 동적 여덟 · 정적 일곱, 디버거 메모
- 결과서: [`_workspace/sast/78_exception-kernel_result.md`](../../_workspace/sast/78_exception-kernel_result.md) — "먼저 알아 둘 것" (응급 코드 · 인자 약속 · 출발 메모 칸), 입구 조각 표 (rva 같이), 3-1 ~ 3-6, 스크립트 문제 2건
- 요청서: [`_workspace/sast/78_exception-kernel.md`](../../_workspace/sast/78_exception-kernel.md)
- flow.md: [3장 새 절 "CPU 예외를 유저 예외로 넘기기"](flow.md) · 2.3 `PspInitializeThunkContext` 6번 run81 줄 · 2.4 "일찍 끝나는 길" 끝 · 4장 run81 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 7.6.2 `PsNtdllExports` 짝 · 7.6.10 `KeFeatureBits` · 7.29 1 · 3.15 머리 "유저 예외 줄"
- 정정이 들어간 문서: [`run48-system-dll-boot.md`](run48-system-dll-boot.md) 맨 위
- 앞 이야기:
  - [run16 해설](../02_sprint/run16-thunk-context-해설.md) — 사물함 · 오리엔테이션실 · 벽 주소록, `PspInitializeThunkContext` (이번 2.18 (나) 의 무대)
  - [run42 해설](../02_sprint/run42-continue-detail-해설.md) — 유저 APC 를 유저 스택에 쌓는 꼴, 체크박스 `0x10001b`, 별지, `ProbeForWrite` (이번 2.13 의 짝)
  - [run79 해설](run79-thread-exit-해설.md) — 가게 문 닫기, `NtTerminateProcess` 자기 가게 길 (이번 둘째 기회의 끝)
  - [run70 해설](run70-cpu-features-해설.md) — 공용 게시판 체크리스트, `KeFeatureBits` 비트 23 (이번 2.17)
  - [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.10 · [run35 해설](../02_sprint/run35-ntdll-entry-해설.md) — 로더 `__fastfail` 자리
  - [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.8 · [run28 해설](../02_sprint/run28-tls-init-call-해설.md) — 예외 보호 구역 · 비상 대피도 · 필터 (Q148b 에서 다시 만남)
  - [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) — 출발 메모 · 순간 이동 · 풀어 보기 · `UNWIND_INFO`
  - [run56 해설](run56-syscall-args-해설.md) · [run77 해설](run77-thread-wake-해설.md) 2.14 — `SYSRET` 과 `IRETQ` 출구 (run77 은 run80 정정 반영해서 읽기)
  - [run59 해설](run59-init-leftovers-해설.md) — `PsNtdllExports +0x20`, `RtlpContinuationContextMachineFrameEntries`

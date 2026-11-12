# run82 exception-user — 해설

> 이 글은 [run82 문서](run82-exception-user.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q148b**: PE_emulator 요청 26 의 유저 쪽 — 사고 처리 창구 (`KiUserExceptionDispatcher`) 안, `RtlRaiseException`, 처리 안 된 예외 (`_workspace/plan.md`, 이번에 **끝**)
  - Q148 (요청 26 전체) 은 둘로 나눠서 했다.
    - 커널 쪽 = **Q148a** (run81, 앞 바퀴).
    - 유저 쪽 = **Q148b** (이번).
  - 그래서 **요청 26 은 이번 바퀴로 끝** 이다.
  - 다음 항목: **Q149a** (요청 27 콘솔) → Q149b → Q150a → Q150b → Q151a → Q151b → Q152 → [멈춤] (Q104 VM 단계 앞).
  - 이번에 남은 것은 셋으로 갈렸다 (4절):
    - 지금 test.exe 로 볼 수 있는 동적 → **Q104** (VM 단계)
    - 예외를 일부러 내는 새 샘플이 있어야 하는 동적 → **Q156** (run81 때 생긴 줄에 이번 것을 보탬. 만들지·언제는 사용자 결정)
    - 작은 정적 → 새 **Q157**
- 근거:
  - run 문서 [`run82-exception-user.md`](run82-exception-user.md) (가장 중요)
  - 요청서 [`_workspace/sast/79_exception-user.md`](../../_workspace/sast/79_exception-user.md) · 결과서 [`79_exception-user_result.md`](../../_workspace/sast/79_exception-user_result.md)
    - 판정 **분석성공**. 여섯 질문 + run81 이 넘긴 덧붙임 셋에 모두 줄로 답했다. 스크립트 문제 없음 (참고 4건).
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 네 곳 · 2.4 · 2.5 · 2.10 두 곳 · 3장 "CPU 예외를 유저 예외로 넘기기" 절 (제목·첫 문단·**새 하위 절 "유저 쪽"**) · 4장 · 5장 (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 근거만 (새 짐 칸 없음, 서른여덟째판)
  - run81 문서 "아직 확인 필요" 에 "(해결됨 → run82)" 표시와 대기열 번호 (Q155 · Q156) 가 붙었다
- 이번 바퀴는 **정적만** 이다.
  - 새 VM 실행은 없다.
  - flow-writer 가 run44 덤프 읽기 로그에서 갈래를 가르는 칸 셋만 다시 읽었다: PEB `BeingDebugged` · PEB `CrossProcessFlags` · KUSD `KdDebuggerEnabled`.

### 이번 바퀴에서 조심할 곳 다섯

1. **`RtlRestoreContext` 가 둘이다.**
   - rva `0x123e20` = 진짜 `RtlRestoreContext`. 이 글에서 그냥 `RtlRestoreContext` 라고 하면 이것이다.
   - rva `0xa59f0` = PDB 이름 **`RtlGuardRestoreContext`**. Ghidra 는 이것도 `RtlRestoreContext` 로 잡아서 파일 이름이 `RtlRestoreContext__a59f0.asm` 이다. 진짜 앞에 붙은 **얇은 막** 이다 (2.4).
2. **스택을 거꾸로 거슬러 가는 일이 두 꼴로 나온다** (2.7).
   - **풀어 보기** (`RtlpxVirtualUnwind`) = 사본 위에서 "한 층 위는 어디" 를 계산만 한다. 실제로는 아무 데도 안 간다.
   - **되감기** (`RtlUnwindEx`) = 받아 줄 층이 정해진 뒤, 그 층까지 **진짜로** 가서 착지한다.
   - flow.md 2.4 새 줄의 "되감기가 `PUSH_MACHFRAME` 을 만나" 는 이 글 말로 **풀어 보기** 쪽이다. 같은 일을 다른 말로 부른 것이다.
3. **질문 번호가 두 벌이다.** 원문 요청 26 번호와 sast/79 번호. run 문서 표는 둘 다 적었다 (예: "4 · 1" = 원문 4, sast/79 1). 이 글은 **sast/79 번호** 를 쓴다.
4. **`ContextFlags` 를 run 문서는 "문맥 이름표" 라고 부른다.** 앞 해설들의 메모 맨 위 **체크 칸** 과 같은 것이다. 이름이 바뀐 까닭은 2.8.
5. flow.md 3장 새 하위 절의 비유는 커널 = **"관리실"**, 함수 = "층 책임자" 로 그렸다.
   - 이 해설 시리즈에선 관리실 = ntdll, 커널 = 관리사무소다 (run81 해설과 같은 사정).
   - 비유만 다르고 사실은 같다.

이미 푼 말은 **링크만** 건다.

- 관리사무소 (커널) · 가게 (프로세스) · 직원 (스레드) → [run11 해설 2.3](../02_sprint/run11-allocate-thread-해설.md#23-첫-직원-뽑기-다섯-걸음)
- ntdll = 가게 안 **관리실**, KUSD = **공용 게시판** → [run70 해설 "비유"](run70-cpu-features-해설.md#비유--그대로-이어-쓴다)
- ntdll 전역 = 관리실 **칠판**, 역함수 표 = **동네 지번 색인**, `LdrInitState` = 가게 **팻말** (0 = 문 열기 전, 3 = 연 뒤) → [run60 해설 "비유" · 2.5](run60-func-table-mrdata-해설.md) · [run19 해설 2.12](../02_sprint/run19-proc-init-modules-해설.md)
- 칠판에 **암호로 적기** (`RtlEncodePointer`) · 풀기 (`RtlDecodePointer`) → [run59 해설 2.7](run59-init-leftovers-해설.md)
- 칠판의 **"마지막 담당"** (`RtlpUnhandledExceptionFilter` ← kernelbase `UnhandledExceptionFilter`) → [run68 해설 2.5](run68-dll-init-rest-해설.md)
- 사고 처리 창구 (`KiUserExceptionDispatcher`) · **사고 보고서 묶음** (사고 순간 메모 `CONTEXT` · 목차 쪽지 `CONTEXT_EX` · 사고 기록 카드 `EXCEPTION_RECORD` · "사고 직전 위치" 표지 = 머신 프레임) · 첫 기회 / 둘째 기회 · **비상 정지 버튼** (`__fastfail`) · 사고 **신고센터** (WER) · 가게 감독관 연락선 (디버그 포트) · 관리사무소 감독관 (커널 디버거, `KdTrap`) → [run81 해설](run81-exception-kernel-해설.md) 2.1 · 2.9 · 2.11 · 2.15 · 2.16
- 메모 맨 위 **체크 칸** (`ContextFlags`, `0x100001` CONTROL …) → [run81 해설 2.8](run81-exception-kernel-해설.md) · [run42 해설 2.3](../02_sprint/run42-continue-detail-해설.md) (거기선 "체크박스")
- 출발 메모 (트랩 프레임), `IRETQ` = 출발 메모대로 **순간 이동** → [run15 해설 2.6](../02_sprint/run15-user-thread-startup-해설.md#26-커널--유저-전환--iretq)
- 풀어 보기 (unwind) · `UNWIND_INFO` 설명서 → [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.3, 풀림 정보 (`UnwindData`) · `RUNTIME_FUNCTION` 목차 줄 → [run59 해설 2.9](run59-init-leftovers-해설.md)
- `NtContinue` = "제 출발 메모를 이걸로 바꿔 주세요" (안 돌아오는 창구) → [run29 해설 2.3](../02_sprint/run29-continue-start-해설.md)
- CET 그림자 스택 = **경비실 장부** → [run29 해설 2.9](../02_sprint/run29-continue-start-해설.md)
- 예외 = **사고**, 예외 보호 구역 (`__try`) → [run17 해설 2.8](../02_sprint/run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일)
- 착지점 = **착지 매트**, unwind 범위 표 (scope table) = **비상 대피도** → [run27 해설 2.9](../02_sprint/run27-init-graph-해설.md)
- 필터 = 대피도에서 "이 사고 여기서 잡을까?" 를 정하는 짧은 함수 → [run28 해설 2.10](../02_sprint/run28-tls-init-call-해설.md)
- CFG = **교환원** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md)
- delay import = "필요하면 그때 부를게요" 메모 → [run25 해설 2.5](../02_sprint/run25-dll-entry-해설.md), ApiSet = 별명 → 진짜 이름 → [run23 해설 2.5](../02_sprint/run23-load-dependent-해설.md)
- 가게 문 닫기 (`NtTerminateProcess` 자기 가게 길) → [run79 해설](run79-thread-exit-해설.md) 2.13 · 2.14
- `OutputDebugStringA` 가 하는 일 → [run71 해설 2.1](run71-named-obj-dir-해설.md)
- 사진 K1 ~ P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

### 비유 — 그대로 이어 쓴다, 새로 넷

- 이 글도 앞 해설들처럼:
  - 프로세스 = **가게**, 스레드 = **직원**, 커널 = **관리사무소**, ntdll = 가게 안 **관리실**.
  - 예외 = 직원이 일하다 난 **사고**. 사고 보고서 묶음은 직원의 가게 **작업대** (유저 스택) 위에 놓여 있다 (run81).
- 함수 호출이 쌓인 모양 (콜스택) 은 flow.md 3장 새 하위 절처럼 **건물 층** 으로 그린다.
  - 사고 난 함수 = 사고 난 층. 그 함수를 부른 쪽 = 한 층 위.
  - 맨 꼭대기 층 = 스레드가 처음 시작한 `RtlUserThreadStart`.
- 새로 나오는 그림 넷:
  - **층 안내원** = 언어 핸들러 (`__C_specific_handler`). 층마다 붙은 비상 대피도를 읽어 준다 (2.6).
  - **관리실 복원 담당** = `RtlRestoreContext`. 메모대로 직원을 다시 세운다 — 혼자 하거나, 관리사무소에 맡기거나 (2.8).
  - **꼭대기 층 마지막 그물** = `RtlUserThreadStart` 의 `__except` + 칠판의 "마지막 담당" (2.11).
  - **비상 연락망** (VEH) — run81 2.1 에서 이름만 꺼냈던 것을 이번에 푼다 (2.5).

### 한 줄로 먼저

- 사고 처리 창구 (`KiUserExceptionDispatcher`) 는 작업대 맨 위 (`RSP`) 하나만 보고 일을 시작한다 `[정적]`.
- 먼저 **비상 연락망** (VEH) 에 묻는다. 등록한 사람이 없으면 (PEB `+0x50` 비트 2 꺼짐) **락도 안 잡고** 지나간다 `[정적]`.
- 다음, **층마다 대피도** (SEH) 를 본다. 사고 난 층부터 한 층씩 위로 올라가며, 층 안내원이 필터에게 "여기서 잡을까?" 묻는다 `[정적]`.
  - 이 걷기 본체 (`RtlDispatchException`) 에는 syscall 이 **하나도 없다** `[정적]`.
- 끝맺음은 메모 맨 위 **체크 칸 (이름표) 하나** 가 가른다 `[정적]`:
  - `__except` 가 받으면 → 되감기가 **새로 쓴** 메모 (`0x10000f`) → 관리실이 가게 안에서 혼자 순간 이동 (유저 `IRETQ`). **syscall 0 개**.
  - 커널이 쌓은 메모 (`0x10001f`) 를 **그대로** 되돌릴 때만 (VEH −1 · 필터 −1) → 관리사무소 창구 `NtContinue`.
  - 아무 데서도 못 받으면 → `NtRaiseException(…, 0)` → 커널 둘째 기회 (run81).
- 그런데 평소 "아무도 안 받음" 은 커널 둘째 기회까지 **안 간다** `[정적]`.
  - 꼭대기 층 `RtlUserThreadStart` 의 `__except` 가 마지막 그물이다 → kernelbase `UnhandledExceptionFilter` → 1 → 되감기 → **`NtTerminateProcess(-1, 예외 코드)`**.
- 프로그램이 일부러 낸 사고 (`RaiseException` → `RtlRaiseException`) 는 디버거가 없으면 **커널을 안 거치고** 바로 창구 안 일 (`RtlDispatchException`) 부터 한다 `[정적]`.
  - test.exe 의 `OutputDebugStringA` 사고가 이 길이다 `[정적]` + 값 `[관찰]`. 그래서 커널 쪽 길 (run81) 은 test.exe 로는 못 보고 새 샘플 (Q156) 이 있어야 본다.
- 그래서 PE_emulator 의 nt 엔진이 새로 흉내 낼 것은 **run81 쌓기 + syscall 셋 (`NtContinue` · `NtRaiseException` · `NtTerminateProcess`)** 이 전부다 `[정적]` (run 문서 맨 위 한 줄).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess → … → ntdll 로더 → NtContinue 은퇴 → 진입점 → main
 │
 ├─ ntdll LdrInitializeThunk 앞머리 InitSpecialMachineFrames (flow.md 2.4, run59)
 │     표 RtlpContinuationContextMachineFrameEntries 를 채움            ← 이번: 누가 읽나, 뜻은 (2.15)
 ├─ kernelbase 시작 함수 +0x4ab (flow.md 2.5, run68)
 │     칠판에 "마지막 담당" 을 암호로 적어 둠                           ← 이번: 누가 읽나 (2.11)
 ├─ 스레드 맨 위 RtlUserThreadStart (flow.md 2.10)
 │     __try 로 감싸고 출발                                            ← 이번: 그 __except = 마지막 그물 (2.11 ~ 2.13)
 └─ TLS 콜백 · DllMain · main 의 Log → OutputDebugStringA (flow.md 2.10 run71 절)
       RaiseException(0x40010006)                                       ← 이번: 커널을 거치나 (2.10)

 (지도 밖, 아무 스레드에서나) 유저 코드가 넘어지면
   CPU 예외 → 커널이 묶음을 쌓음 (run81) → ntdll 사고 처리 창구
                                              └─ 창구 안: VEH → SEH → 끝맺음      ← 이번 본론 (2.4 ~ 2.8)
   소프트웨어 예외 (RaiseException) ──────────────┘ 창구 앞을 건너뛰고 바로        ← 이번 (2.9)
```

- 본론은 run81 처럼 흐름도 범위 (진입점까지) 의 **정상 길이 아니다.** 어느 스레드든 넘어지면 타는 공용 길이다.
  - 그래서 flow.md 그림엔 안 넣고, 3장 "CPU 예외를 유저 예외로 넘기기" 절에 **"유저 쪽" 하위 절** 하나로 적었다.
- 지도 안에서 닿는 곳은 넷이다 (위 그림 ← 표시). 넷 다 이미 지도에 있던 자리인데, 그 **안쪽 길** (누가 읽나 · 무엇을 하나 · 어떻게 끝나나) 이 비어 있었다.
  - 예: `RtlUserThreadStart` 는 "`__try` → 필터 → 착지 → `NtTerminateProcess`" 한 줄이 이미 있었다 (아래 "바퀴 전에 알던 것"). 그 사이 길이 이번에 채워졌다.
- 그럼 왜 봤나?
  - PE_emulator 는 은퇴 (`NtContinue`) 뒤 진짜 ntdll·kernelbase 를 Unicorn 안에서 돌린다.
  - 그러니 유저 쪽은 흉내 낼 게 아니라, **"진짜 ntdll 이 이 길에서 부르는 syscall 과, nt 엔진이 줄 답"** 이 필요하다 (요청서 "배경").
  - run81 이 "커널이 묶음을 어떻게 쌓나" 를 정했으니, 이번엔 "그 묶음을 받은 ntdll 이 무엇을 하고 언제 다시 커널을 부르나" 를 정한다.

### 무엇이 궁금했나

한 문장으로: **사고 처리 창구에 들어온 사고를 ntdll 은 누구에게 어떤 차례로 묻고, 어떻게 끝내나. 그 길에서 부르는 syscall 은 무엇이고, nt 엔진은 무슨 답을 줘야 하나.**

- 계기 — PE_emulator 의 지금 짐작 (요청서 "배경"):
  - "소프트웨어 예외 (`RtlRaiseException`) 는 유저 안에서 돈다" `[추측]`.
- 질문은 여섯 + 덧붙임 셋 (sast/79 번호. 원문 요청 26 번호와 달라서 run 문서 표는 둘 다 적었다):

| sast/79 # | 원문 # | 질문 | 이 글 어디서 |
|---|---|---|---|
| 1 | 4 | `KiUserExceptionDispatcher` — 입력, 갈래, 실패면 | 2.4 |
| 2 | 4 | `RtlDispatchException` — VEH · SEH · 핸들러 검사 · syscall 있나 · VCH | 2.5 ~ 2.8 |
| 3 | 7 | `RtlRaiseException` — 커널을 거치나, 몇 번째 기회로 | 2.9 · 2.10 |
| 4 | 5 (유저 몫) | 아무도 안 받을 때 — `UnhandledExceptionFilter` 의 syscall | 2.11 ~ 2.13 |
| 5 | 6 (유저 몫) | `RaiseFailFastException` 과 `int 0x29` 는 같은 길인가 | 2.14 |
| 6 | 4 · 5 | nt 엔진이 줄 답 — syscall 표 | 2.16 |
| 덧붙임 1 · 2 · 3 | run81 덧붙임 ① 등 | `CONTEXT_EX` · 머신 프레임 칸 · `Entries` 표 · test.exe 사고가 커널을 거치나 | 2.15 · 2.10 |

### 바퀴 전에 알던 것

- `[정적]` run81: 커널이 유저 스택에 사고 보고서 묶음을 쌓고 (`C` = 새 `RSP`, `C+0x4f0` 기록 카드, `C+0x590` 표지 `Rip`, `+0x5a8` 표지 `Rsp`), 체크 칸 `0x10001f` (비트 23 이면 `0x10005f`), 출발 메모 `Rip` = `KiUserExceptionDispatcher`.
- `[정적]` run81: `KiUserExceptionDispatcher +0x29` (ret `+0x2e`) `RtlDispatchException(RSP+0x4f0, RSP)`, 거짓이면 `+0x4e` (ret `+0x53`) `NtRaiseException(…, 0)` → 커널 둘째 기회 → `ZwTerminateProcess(-1, 코드)`.
- `[정적]` run59: `InitSpecialMachineFrames` 가 표 `RtlpContinuationContextMachineFrameEntries` 를 채운다. 뜻 ("커널 → 유저 진입 함수의 풀림 정보인가") 은 `[추측]`.
- `[정적]` run63: 함수 표 (`RtlInitializeHistoryTable`) 에 `RtlUnwindEx` · `__C_specific_handler` · `RtlpExecuteHandlerForException` · `RtlDispatchException` · `RtlRaiseException` · `KiUserExceptionDispatcher` 이름이 있다.
- `[정적]` run68: kernelbase 시작 함수 `+0x4ab` (ret `+0x4b2`) `RtlSetUnhandledExceptionFilter(UnhandledExceptionFilter)` → 칠판 `RtlpUnhandledExceptionFilter` 에 암호로.
- `[정적]` flow.md 2.10 (앞 바퀴들): `RtlUserThreadStart` 전체가 `__try` (`+0x7~+0x4a`). 필터 `$filt$0` 가 칠판 `RtlpUnhandledExceptionFilter` 를 풀어 있으면 그걸, 없으면 `RtlUnhandledExceptionFilter2` 를 부른다. 착지 `+0x4a` → `+0x50` (ret `+0x55`) `NtTerminateProcess(-1, 예외 코드)`. 그 사이 (필터 안 · 착지까지 어떻게 가나) 는 비어 있었다.
- `[정적]` run71: `OutputDebugStringA +0x77 RaiseException(0x40010006)`, 그 `__except` 착지 몸통 `+0x81`. "디버거가 받으면 아래 안 탐".
- `[정적]` run29 · run42: `NtContinue` 길. run79: `NtTerminateProcess` 자기 가게 길.
- `[관찰]` run44 사진 (은퇴까지) — 이번에 PEB · KUSD 칸 몇 개를 다시 읽었다.
- `[추측]` PE_emulator: `RtlRaiseException` 은 유저 안에서만 돈다.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 창구 안 다섯 걸음

자세히 들어가기 전에 한 장으로 보자.

```
① 묶음 집기       KiUserExceptionDispatcher: RSP = 사고 순간 메모, RSP+0x4f0 = 사고 기록 카드
② 비상 연락망     VEH — 등록된 사람이 없으면 곧장 지나감
③ 층마다 대피도   SEH — 사고 난 층부터 위로: 설명서 찾기 → 한 층 풀어 보기 → 층 안내원 → 필터
④ 끝맺음         받음   → (되감기) → RtlRestoreContext: 체크 칸 보고 유저 IRETQ / NtContinue
                 못 받음 → NtRaiseException(…, 0) → 커널 둘째 기회 (run81)
⑤ 꼭대기 그물     평소 "아무도 안 받음" 은 ③ 의 맨 꼭대기 층 RtlUserThreadStart 에서 잡힘
                 → UnhandledExceptionFilter → 1 → NtTerminateProcess(-1, 예외 코드)
 ── 옆문 ──  소프트웨어 예외: RaiseException → RtlRaiseException → ② 부터 (① 과 커널을 건너뜀)
```

- 모두 run 문서 호출 관계를 큰 덩어리로 묶은 것이다. 줄은 다 `[정적]`.
- ⑤ 는 사실 ③ 의 **마지막 층** 에서 일어나는 일이다. 그 층이 특별해서 따로 뗐다.

이 그림에 나오는 새 말 둘:

- **VEH** (vectored exception handler) — 프로그램이 프로세스 **전체** 에 걸어 두는 처리기 목록으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 이번 코드에서 확인된 것: 이 목록을 **SEH 보다 먼저** 부른다 `[정적]` (`RtlDispatchException +0x200` → 그 뒤 `+0x24b` 부터 SEH).
  - 비유: 가게 전체 **비상 연락망**. 어느 층에서 난 사고든 먼저 이 연락망부터 돌린다.
- **SEH** (structured exception handling) — 함수마다 "내 구간에서 사고 나면 이렇게" 를 PE 파일 표에 적어 두고, 사고가 나면 콜스택을 따라 그 표를 찾아가는 방식으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). C 의 `__try`/`__except` 가 이걸로 만들어진다.
  - 비유: 층마다 벽에 붙은 **비상 대피도** (run27) 를, 사고 난 층부터 한 층씩 올라가며 확인하는 것.

### 2.2 한눈에 — 호출 관계

run 문서 "호출 관계" 에서 대표만 남겼다.
가지의 `+0x..` 는 **돌아올 자리 (ret)**, 가지 밖 `+0x..` 는 그 줄 자리다. 괄호 번호는 run 문서 그대로라 중간이 빈다. ★ 는 이 글 절.

```
[CPU 예외 — 커널이 쌓은 뒤 (run81)]
ntdll!KiUserExceptionDispatcher (0x164e10)   RSP = 메모 (0x10001f) · RSP + 0x4f0 = 카드         ★ 2.4
 │  +0x1 Wow64PrepareForException · +0xb JZ (64비트 = 0 → 건너뜀)
 ├─ (1) +0x2e  RtlDispatchException(카드, 메모)                                  → AL
 │    ├─ (1) +0x205  RtlpCallVectoredHandlers(…, 0)   VEH. PEB+0x50 비트 2 꺼지면 곧장 0         ★ 2.5
 │    ├─ (2) +0x261  RtlpGetStackLimits               TEB +0x10 · +0x8, 지금 RSP 가 그 안인가      ★ 2.6
 │    ├─ (3) +0x330  RtlInitializeExtendedContext2    지역 사본 0x10000b
 │    ├─ (4) +0x33b  RtlpCopyContext                  사본 & 0x10000f
 │    │  ── 층 고리 +0x370 ──
 │    ├─ (5) +0x37f  RtlLookupFunctionEntry           LdrInitState == 3 → syscall 없음
 │    ├─ (6) +0x3e8  RtlpxVirtualUnwind(1, …)          한 층 풀어 보기
 │    ├─ (7) +0x5f5  RtlpExecuteHandlerForException    +0xd CALL RAX — 층 안내원 (CFG 없음)         ★ 2.6 · 2.7
 │    │  1 다음 층 · 0 TRUE · 2 중첩 · 3 충돌 · 끝 → +0x64b FALSE
 │    └─ (8) +0x21d  RtlpCallVectoredHandlers(…, 1)   VCH. TRUE · FALSE 둘 다                     ★ 2.5
 ├─ (2) +0x3c  RtlGuardRestoreContext(메모, NULL)    TRUE (VEH −1 · SEH ContinueExecution)         ★ 2.4
 │       ══▶ +0x1b6 JMP RtlRestoreContext (0x123e20)   이름표 0x10001f ≠ 0x10000f
 │              └─ +0x18a  NtContinue(메모, 0)   ══▶ SYSCALL 0x43, 돌아오지 않음                     ★ 2.8
 ├─ (2) +0x53  NtRaiseException(카드, 메모, 0)        FALSE → 커널 둘째 기회 (run81)
 └─ (3) +0x5a  RtlRaiseStatus(상태)                   (2) 가 돌아오면 (실패)

[__except 가 받을 때 — 위 (7) 안]                                                                 ★ 2.7
ntdll!__C_specific_handler (0x124670)
 ├─ (2) +0x93   필터 (CALL RAX — 예: RtlUserThreadStart$filt$0 · OutputDebugStringA$filt$0)   < 0 → 0 · 0 → 다음 · > 0 ↓
 └─ (3) +0xd6   RtlUnwindEx(EstablisherFrame, 착지, 카드, ReturnValue = 코드, 메모, 히스토리)
       ├─ (2) +0x897  RtlCaptureContext2                 +0xf3 OR 이름표 0x10000f
       │  목적 층: +0x3f9 Rax = ReturnValue · +0x40c Rip = 착지 · +0x41b 코드 0xC0000027
       └─ (4) +0x60b  RtlRestoreContext(메모, 카드)      +0x181 JZ (0x10000f) → +0x300 IRETQ   ← syscall 0   ★ 2.8

[소프트웨어 예외 — 디버거 없음]                                                                    ★ 2.9
KERNELBASE!RaiseException (0xc4140)
 └─ +0x8a  (IAT) ntdll!RtlRaiseException (0xf700)    +0x5f 플래그 | 0x80 · +0x64 이름표 0x10000b
      ├─ (2) +0xeb   RtlpCaptureContext2       → 0x10000f
      ├─ (4) +0x14c  RtlVirtualUnwind          자기 층 한 칸 → Rip = RaiseException +0x8a
      │  +0x17e CMP PEB.BeingDebugged · +0x182 JNZ +0x29f
      ├─ (5) +0x18d  RtlDispatchException(카드, 메모)      커널 안 거침
      ├─ (6) +0x1a0  RtlRestoreContext(메모, 카드)          TRUE → +0x300 IRETQ (syscall 0)
      └─ (6) +0x2a7  NtRaiseException(카드, 메모, 0)        FALSE   (디버거면 +0x29f R8B = 1 로 곧장)

[아무도 안 받음 — 스레드 꼭대기 층]                                                               ★ 2.11 ~ 2.13
ntdll!RtlUserThreadStart (0xacac0)     __try [+0x7, +0x4a) · 층 안내원 __C_specific_handler
 │  RtlUserThreadStart$filt$0 (0x169b61)
 │   ├─ (1) +0x25  RtlDecodePointer([RtlpUnhandledExceptionFilter 0x1d28e8])     ← KERNELBASE +0x4ab 가 넣은 값 (run68)
 │   ├─ (2) +0x3f  (CFG 간접) KERNELBASE!UnhandledExceptionFilter (0x10ed90)       널이 아니면 → 기본 1
 │   └─ (2) +0x51  RtlUnhandledExceptionFilter2 (0x122300)                          널이면 → 보통 0
 │  1 → __C_specific_handler +0xd6 RtlUnwindEx → +0x60b RtlRestoreContext → IRETQ → 착지 +0x4a
 └─ +0x55  NtTerminateProcess(-1, 예외 코드)       +0x4a MOV EDX,EAX · +0x4c OR RCX,-1

[__fastfail 유저 쪽]                                                                             ★ 2.14
KERNELBASE!RaiseFailFastException (0x10a020)    코드 0xC0000602 · NONCONTINUABLE
 └─ (4) +0x153  NtRaiseException(카드, 메모, 0)    유저 SEH · VEH 안 거침 → 커널 둘째 기회 (run81)
```

- 모두 `[정적]` 이다.
- `UnhandledExceptionFilter` 안의 syscall 줄은 2.12 표로 따로 뺐다.

### 2.3 한 줄씩 읽는 법

#### 줄 1: `├─ (1) +0x2e RtlDispatchException(카드, 메모) → AL`

- **누가**: `KiUserExceptionDispatcher` — 사고 처리 창구.
- **어디서**: 부르는 자리 `+0x29`, 돌아올 자리 `+0x2e`.
  - CPU 예외 때 멈추면 콜스택에 `ntdll!KiUserExceptionDispatcher+0x2e` 로 보일 자리다. 아직 안 봤다 (Q156 — test.exe 에선 이 창구가 안 불린다, 2.10).
- **누구를**: `RtlDispatchException` — 받을 사람을 찾는 본 담당.
- **인자 둘** `[정적]` (`+0x1c ~ +0x26`):
  - `RCX` = `RSP + 0x4f0` — 사고 기록 카드.
  - `RDX` = `RSP` — 사고 순간 메모.
  - 둘 다 `RSP` 에서 만든다. 창구에 들어올 때 `RCX`·`RDX` 에 뭐가 있었든 **안 읽고 덮는다**.
- **무엇을 위해**: VEH → SEH 차례로 받을 사람을 찾는다. 돌아온 `AL` 이 0 이 아니면 "받았다" (TRUE), 0 이면 "아무도 안 받았다" (FALSE).
- 비유: 창구 직원이 작업대 맨 위 묶음에서 메모와 카드를 집어 **본 담당에게 넘기는** 줄.

#### 줄 2: `RtlRestoreContext` 의 비교 세 줄 — 호출이 아니라 **갈림** 줄

```
RtlRestoreContext +0x174  MOV EAX,[RCX+0x30]
RtlRestoreContext +0x17c  CMP EAX,0x10000f        (그 앞 +0x177 AND EAX,0xffffff3f)
RtlRestoreContext +0x181  JZ +0x1a3
```

- **누가**: `RtlRestoreContext` (rva `0x123e20`) — 관리실 복원 담당.
- **무엇을**: `RCX` = 되돌릴 메모. `+0x30` = 메모의 체크 칸 (`ContextFlags`).
  - 체크 칸에서 비트 몇 개를 지운 뒤 (`AND 0xffffff3f`) `0x10000f` 와 같은지 본다.
- **갈래**:
  - 같으면 → `+0x1a3` 로 가서 레지스터를 직접 싣고 **`+0x300 IRETQ`** (syscall 없음).
  - 다르면 → 그 아래 `+0x183 XOR EDX,EDX` · `+0x185 CALL NtContinue` (ret `+0x18a`).
- **무엇을 위해**: 이 한 줄이 "가게 안에서 끝나나, 관리사무소를 부르나" 를 정한다. 2.8 에서 천천히 본다.
- 비유: 복원 담당이 메모 맨 위 체크 칸을 보고 **"내 손으로 할 수 있는 메모인가"** 를 가르는 줄.

#### 줄 3: `└─ +0x55 NtTerminateProcess(-1, 예외 코드)`

- **누가**: `RtlUserThreadStart` — 스레드 꼭대기 층.
- **어디서**: 부르는 자리 `+0x50`, 돌아올 자리 `+0x55`.
- **그 바로 앞 두 줄** `[정적]`:
  - `+0x4a MOV EDX,EAX` — 둘째 인자 = `EAX` (= 예외 코드, 왜 그런지는 2.13).
  - `+0x4c OR RCX,-1` — 첫째 인자 = −1 (지금 프로세스).
- `+0x4a` 는 이 층 `__except` 의 **착지점** 이다 (scope table, 2.11). 평소 길로는 닿지 않고, 되감기가 여기로 착지시킬 때만 온다.
- **무엇을 위해**: 아무도 사고를 못 받았을 때 가게를 닫는다. 끝 코드 = 예외 코드.
- 비유: 꼭대기 층 착지 매트 바로 옆에 붙은 **"가게 문 닫기" 버튼**.

### 2.4 사고 처리 창구 — 묶음 집기와 세 갈래 (질문 1)

#### 창구 92바이트, 줄마다 `[정적]` (`KiUserExceptionDispatcher`, rva `0x164e10`)

| 줄 | 하는 일 |
|---|---|
| `+0x0 CLD` | 첫 줄 |
| `+0x1 MOV RAX,[Wow64PrepareForException]` · `+0xb JZ +0x1c` | 이 전역이 0 이면 건너뜀. 0 이 아니면 `+0x1a` 에서 간접 호출 (ret `+0x1c`) |
| `+0x1c MOV RCX,RSP` · `+0x1f ADD RCX,0x4f0` · `+0x26 MOV RDX,RSP` | **입력은 `RSP` 하나** — 카드 = `RSP+0x4f0`, 메모 = `RSP` |
| **`+0x29`** (ret `+0x2e`) `RtlDispatchException` | 받을 사람 찾기 → `AL` |
| `+0x2e TEST AL,AL` · `+0x30 JZ +0x3e` | 갈림 |
| TRUE: `+0x32 MOV RCX,RSP` · `+0x35 XOR EDX,EDX` · **`+0x37`** (ret `+0x3c`) `RtlGuardRestoreContext` | `(메모, 카드 = NULL)` → 꼬리로 `RtlRestoreContext` → 이름표 `0x10001f` 라 **`NtContinue(메모, 0)`** |
| FALSE: `+0x3e ~ +0x48` (같은 두 주소) · `+0x4b XOR R8B,R8B` · **`+0x4e`** (ret `+0x53`) `NtRaiseException` | **`NtRaiseException(카드, 메모, FirstChance = 0)`** (syscall `0x174`) |
| `+0x53 MOV ECX,EAX` · **`+0x55`** (ret `+0x5a`) `RtlRaiseStatus` · `+0x5b RET` | 위 둘 중 어느 쪽이든 **돌아오면** (= 실패) 그 상태로 |

- `Wow64PrepareForException` 은 64비트 프로세스에서 **0** 이다 `[정적]`.
  - 근거: `_refs/Wow64PrepareForException.tsv` 에 그 칸을 **쓰는** 코드가 없다. 읽는 곳은 이 줄 하나와 WoW64 초기화용 포인터 표 한 칸 (`0x173848`) 뿐.
  - 그래서 `+0xb JZ` 는 늘 건너뛴다.
- 이 함수 안에 `NtContinue` 를 **직접** 부르는 줄은 없다 `[정적]`. `NtContinue` 는 두 칸 아래 `RtlRestoreContext` 안에 있다.

#### 얇은 막 `RtlGuardRestoreContext` (rva `0xa59f0`) `[정적]`

- 이름 이야기 (조심할 곳 1): PDB 이름이 `RtlGuardRestoreContext` 다. `pdb_aliases.py --where ntdll.dll RtlGuardRestoreContext` 로 확인했다 (flow-writer).
- 하는 일 — 카드 자리가 NULL 이면:
  1. `+0x178 CMP [LdrSystemDllInitBlock+0xb8],0` — 시작 정보 블록 `+0xb8` 칸을 본다.
  2. test.exe 는 이 칸이 **0** 이다 (sast/55 결과서가 옮긴 run44 값).
  3. 그래서 → **`+0x1b6 JMP RtlRestoreContext`** — 메모를 그대로 들고 진짜 복원 담당에게 넘긴다.
- `+0xb8 ≠ 0` 인 프로세스만 `RtlGuardIsValidStackPointer` (`+0x192`) 로 메모의 `Rsp` 를 검사하고, 실패면 `+0x210 INT 0x29` (ECX `0xd`) 다.
  - `+0xb8` 은 짐 지도 3.9 에서 "CFG 비트맵 주소" 칸이다.
- 비유: 복원 담당 방 앞의 **문 경비**. test.exe 문 앞엔 "검사 없음" 이 걸려 있어서 그냥 통과다.

#### 실패하면 — `RtlRaiseStatus` 의 되풀이 `[정적]`

`NtContinue` 도 `NtRaiseException` (거짓) 도 **원래는 돌아오지 않는** 창구다 (run29 · run81). 돌아왔다는 건 뭔가 잘못됐다는 뜻이다.

1. `RtlRaiseStatus` (rva `0x12ce0`, 107B) 가 새 카드를 만든다: 코드 = 받은 상태, 플래그 `0x81`, 주소 −1, 개수 0.
2. `+0x4a` (ret `+0x4f`) `RtlRaiseNoncontinuableException(카드, 메모, 1)`.
3. 거기서 셋째 인자가 0 이 아니고 디버거가 없으면 → `+0x60 JMP RtlDispatchException` (유저 SEH 에 먼저 맡김).
4. 그것도 돌아오면 둘째엔 셋째 인자 = 0 → `+0x6b JMP NtRaiseException` → `NtRaiseException(…, FALSE)` → 커널 둘째 기회 (run81).
5. 그것까지 돌아오면 `RtlRaiseStatus` 를 다시 (되풀이).

- 비유: "메모대로 돌려 놔 주세요" 나 "못 받았어요" 를 냈는데 창구가 **답을 돌려줬다** 면 그 자체가 새 사고다. 그래서 "이어 갈 수 없음 · 일부러 낸 사고" 도장 (`0x81` = NONCONTINUABLE | SOFTWARE_ORIGINATE, 결과서) 을 찍은 카드를 새로 쓴다.

#### PE_emulator 에게 (run 문서 표 질문 1 줄)

- 이 함수는 진짜 ntdll 이 Unicorn 안에서 그대로 돈다.
- 엔진 몫 = run81 쌓기 + `NtContinue` · `NtRaiseException` 구현 (지금 없음).
- 진입 `RSP` **아래** 가 쓸 수 있는 스택이어야 한다. 이 창구는 `RSP` 아래로 `CALL` 을 쌓는다.

### 2.5 비상 연락망 — VEH, 비어 있으면 락도 안 잡는다 (질문 2 (a) · (e))

#### 걸음 `[정적]`

1. `RtlDispatchException +0x200` (ret `+0x205`) `RtlpCallVectoredHandlers(카드, 메모, 0)`.
   - 셋째 인자 0 = **예외 목록** (VEH). 1 이면 **계속 목록** (VCH, 아래).
2. 연락망 안 (`RtlpCallVectoredHandlers`, rva `0xcc0c0`, 983B):
   - `+0x1d LEA EBP,[R8+2]` — 볼 비트 번호 = 목록 번호 + 2.
   - `+0x3b MOV EAX,[PEB+0x50]` — PEB `CrossProcessFlags`.
   - **`+0x3e BT EAX,EBP` · `+0x45 JNC +0x134`** — 그 비트가 꺼져 있으면 **락도 안 잡고** 0 을 돌려준다.
   - 목록 0 이면 **비트 2 (`ProcessUsingVEH`)**, 목록 1 이면 **비트 3 (`ProcessUsingVCH`)**.
   - 비트가 켜져 있어도 목록이 비면 `+0x6c JZ +0x122` 로 빠진다.
3. `RtlDispatchException +0x207 JZ +0x24b` — 0 이면 SEH 로 (2.6).

#### 그 비트는 누가 켜나 `[정적]`

- 켜는 곳: `RtlpAddVectoredHandler +0x1af BTS.LOCK [PEB+0x50],ECX`.
- 끄는 곳: `RtlpCallVectoredHandlers +0x262 BTR.LOCK` (마지막 노드가 빠질 때).
- 전역 목록 `LdrpVectorHandlerList` (rva `0x1ea578`): 두 칸 × `0x18` = `{SRW 락, 목록 머리}`. 칸 0 예외, 칸 1 계속. **파일 값부터 빈 원형 목록** 이다.
- **ntdll 안에 `RtlAddVectoredExceptionHandler` · `…ContinueHandler` 를 부르는 코드가 없다** (`_refs`: 내보내기 표 · `.pdata` 뿐).
- 그래서 → test.exe 가 직접 안 부르면 비트 2 · 3 은 0 이다.

#### 사진 값 `[관찰]` (run44, flow-writer 대조)

- `CrossProcessFlags` 여덟 장 모두 **0 또는 2** (`ms/logs/04_baggage-read_K1.txt:1568` ~ `_P1.txt:1576` 0 · `_P1a.txt:1743` · `_P1b.txt:2441` 2 · `_P2.txt:2453` 0).
  - 값 2 는 비트 1 (`ProcessInitializing`) 자리다. 비트 2 면 4, 비트 3 이면 8 이다.
  - 그래서 **VEH · VCH 비트는 꺼져 있다** (run 문서).

#### 등록된 사람이 있을 때 `[정적]`

- 락 → 노드마다 참조 올림 → 락 풀고 → `+0xa6 RtlDecodePointer([노드+0x20])` (처리기 주소도 칠판처럼 **암호로** 적혀 있다) → `+0xd1` CFG 간접 호출 (교환원 거침).
- **`+0x112 CMP [RSP+0xa8],-1` · `+0x11a JNZ +0x69` · `+0x120 MOV BL,1`**:
  - 처리기가 **−1** (CONTINUE_EXECUTION) 을 돌려주면 그 자리서 멈추고 TRUE.
  - 그 밖 (0 도 1 도) 은 다음 노드.
- TRUE 면 `RtlDispatchException` 은 **SEH 를 건너뛰고** 바로 TRUE (`+0x207` → `+0x209`).

#### VCH — 끝에서, 받았든 못 받았든 `[정적]` (질문 2 (e))

- 공통 꼬리 `+0x20c MOV R8D,1` ~ **`+0x218`** (ret `+0x21d`) `RtlpCallVectoredHandlers(…, 1)` → `+0x21d MOVZX EAX,DIL` (반환).
- 여기로 오는 길이 셋: VEH 가 받음 (`+0x209`) · SEH 가 ContinueExecution (`+0x747 JZ +0x209`) · **못 받음 (FALSE) 도** (`+0x651` · `+0x75e`).
- 즉 **TRUE · FALSE 둘 다** 끝에서 돈다. 비트 3 이 꺼지면 곧장 돌아온다. 반환값은 버린다.
- 비유: 연락망은 둘이다. **"사고 났어요" 연락망** (VEH) 은 맨 처음에, **"찾기 끝났어요" 연락망** (VCH) 은 맨 끝에 — 받은 사람이 있든 없든 — 돌린다. 둘 다 가입자가 없으면 수화기도 안 든다.

#### PE_emulator 에게

- 진짜 ntdll 이 돈다. 엔진은 PEB `+0x50` 비트 2 · 3 = 0 이면 된다. 틀린 곳 없음 (run 문서 표).

### 2.6 층마다 대피도 ① — SEH 걷기 (질문 2 (b) · (c) · (d))

#### 새 말 둘 — 먼저

- **`.pdata`** — PE 파일 안에서, 함수마다 "이 번지 범위 함수의 설명서 (`UNWIND_INFO`) 는 여기" 줄 (`RUNTIME_FUNCTION`) 을 모아 둔 구역으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - `RtlLookupFunctionEntry` 가 사고 자리 (`Rip`) 로 이 줄을 찾는다 `[정적]`.
  - 비유: 건물 (모듈) 입구에 붙은 **층별 안내 목차**. "몇 번지 ~ 몇 번지 방은 이 층, 이 층 대피도는 저기."
  - run59 해설의 "목차 줄 (`RUNTIME_FUNCTION`)", run60 해설의 "동네 지번 색인" (어느 건물인지) 다음 칸이다.
- **언어 핸들러** — 설명서 (`UNWIND_INFO`) 에 "이 층 사고는 이 함수에게 물어라" 하고 적힌 함수. 컴파일러 · 언어마다 따로 있어서 이렇게 부르는 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - C 의 `__try`/`__except` 층이면 **`__C_specific_handler`** (rva `0x124670`) 다 `[정적]` (`RtlUserThreadStart` · `OutputDebugStringA` 의 설명서).
  - 비유: 층마다 있는 **층 안내원**. 대피도 (scope table) 를 읽고, 필터에게 묻고, 착지까지 데려다준다 (2.7).

#### 걸음 `[정적]` (`RtlDispatchException`, rva `0xcb730`, 2197B)

1. **준비 — 작업대 범위**
   - `+0x256 AND ESI,0x81` · **`+0x25c`** (ret `+0x261`) `RtlpGetStackLimits`.
   - TEB `+0x10` (낮은 쪽, `StackLimit`) · TEB `+0x8` (높은 쪽, `StackBase`) 을 받는다.
   - `RtlpGetStackLimits` (rva `0xcc530`, 43B) 는 **지금 `RSP` 가 그 사이인지도** 본다 (`+0x1b` · `+0x1e` · `+0x20` · `+0x23`).
   - 못 받으면 `+0x263 JZ +0x758` — 카드 플래그에 `0x8` (STACK_INVALID) + FALSE.
2. **준비 — 지역 사본**
   - `+0x32b RtlInitializeExtendedContext2` · `+0x336 RtlpCopyContext` — 메모의 **사본** 을 자기 스택에 만든다.
   - 사본 체크 칸: `0x10000b` (CET 일 때만 `0x10004b`, test.exe 는 앞), `RtlpCopyContext` 가 `& 0x10000f` 로 맞춘다.
   - 이 사본 위에서 층을 걷는다. 원래 메모는 건드리지 않는다.
3. **층 고리** (`+0x370`) — 한 바퀴 = 한 층:
   - **`+0x37a`** (ret `+0x37f`) `RtlLookupFunctionEntry(Rip, &ImageBase, &히스토리)` — 목차에서 이 층 줄 찾기.
     - `LdrInitState == 3` (가게 문 연 뒤) 이면 색인 책을 펴서 찾으므로 **syscall 없음** (run60).
   - **`+0x3e3`** (ret `+0x3e8`) `RtlpxVirtualUnwind(1, …)` — 설명서대로 **한 층 풀어 보기**. 사본의 `Rip` · `Rsp` 가 한 층 위 것으로 바뀐다.
     - `RtlpxVirtualUnwind` 는 PDB 이름이고, `RtlVirtualUnwind` (rva `0x16bc0`) 는 그 얇은 겉포장이다. 이 고리는 안쪽을 직접 부른다.
     - `.pdata` 줄이 없는 함수 (리프 — 다른 함수를 안 부르는 끝 함수로 알려져 있다 `[추측]`) 면 `RtlpxVirtualUnwind +0x3a TEST R9,R9` · `+0x3d JZ +0x6ba` 로 따로 처리 `[정적: 분기만]`.
4. **검사**
   - `+0x3ea JS +0x64b` — 풀어 보기 실패 → FALSE.
   - `+0x3f0 ~ +0x40b` — 이 층의 스택 자리 (`EstablisherFrame`) 가 8 정렬이고 작업대 범위 안인가. 아니면 `+0x890` 플래그 `0x8` → FALSE.
   - `+0x418 JZ +0x622` — 이 층에 언어 핸들러가 없으면 다음 층으로.
5. **층 안내원 부르기**
   - `+0x424 ~ +0x466` 안내원에게 줄 쪽지 (`DISPATCHER_CONTEXT`) 를 `[RBP+0x60]` 에 채운다.
   - **`+0x5f0`** (ret `+0x5f5`) `RtlpExecuteHandlerForException(카드, EstablisherFrame, 원래 메모, &쪽지)`.
   - 그 안 (rva `0x1654f0`, 21B): `+0x9 MOV RAX,[R9+0x30]` (쪽지의 `LanguageHandler`) · **`+0xd CALL RAX`** — **CFG 검사 없이** 부른다.
6. **안내원이 돌려준 값** (`EXCEPTION_DISPOSITION`)
   - **1** ContinueSearch ("여기 아님") → `+0x622` 사본 `Rsp` (`+0x98`) · `Rip` (`+0xf8`) 범위 검사 → `+0x646 JMP +0x370` 다음 층.
   - **0** ContinueExecution ("메모대로 이어 가") → `+0x743 TEST SIL,1`. 이어 갈 수 없는 사고 (NONCONTINUABLE) 면 `+0x74d RtlRaiseStatus(0xC0000025)`, 아니면 `+0x209` → **TRUE**.
   - **2** Nested → `+0x763` 플래그 `0x10`.
   - **3** Collided → `+0x670 ~ +0x720` (CET 일 때만 `+0x6dd` 검사 → 어긋나면 `+0x7f2 INT 0x29`).
   - 그 밖 → `+0x7f4 RtlRaiseStatus(0xC0000026)`.
7. **끝까지 못 찾으면** — **`+0x64b XOR DIL,DIL`** · `+0x64e` 플래그 저장 · `+0x651 JMP +0x20c` → FALSE (끝에서 VCH, 2.5).

- 덤 `[정적]`: `RtlpExecuteHandlerForException` 자신도 설명서에 예외 처리기 `0x1654c0` 을 달고 있다 (함수로 안 잡혀 결과서가 objdump). 되감는 중 (`& 0x66`) 이면 1, 아니면 2 (중첩) 를 돌려준다.
  - 뜻: 안내원이 일하다 **또** 사고가 나면 "중첩 사고" 로 표시하는 장치로 보인다 `[추측]`. 줄은 `[정적]`, 뜻은 코드가 말해 주지 않는다.

#### 비유로 한 번 더

- 본 담당은 메모를 **복사** 해 들고, 사고 난 층부터 한 층씩 올라간다.
- 층마다: 건물 입구 목차에서 이 층 대피도를 찾고 (`RtlLookupFunctionEntry`) → 대피도대로 "한 층 위는 어디" 를 사본에 적고 (`RtlpxVirtualUnwind`) → 이 층에 안내원이 있으면 묻는다.
- 안내원이 "여기 아님" (1) 이면 다음 층. "메모대로 이어 가" (0) 면 거기서 끝.
- 이 모든 걸 **종이 (사본) 위에서** 한다. 직원은 아직 사고 처리 창구에 서 있다.

#### 핸들러 검사 — SafeSEH 같은 건 없다 `[정적]` (질문 2 (c))

- `RtlDispatchException` 에는 SafeSEH 류 검사가 **없다**. x64 엔 SafeSEH 가 없고, 안내원은 CFG 없는 `CALL RAX` 로 불린다 (run 문서).
- 검사로 볼 만한 것 셋:
  1. 스택 · 정렬 → 플래그 `0x8` + FALSE.
  2. 돌려준 값 → `0xC0000025` · `0xC0000026`.
  3. 되감기 때 `RtlGuardCheckExceptionHandler` (rva `0xa5d00`, 뜻은 `[추측]` EH 이어하기 표 검사) — **첫 줄 `+0x18 CMP [LdrSystemDllInitBlock+0xb8],0` · `+0x2a JZ +0x195` 로 곧장 끝** (test.exe `+0xb8 = 0`).

#### syscall 은 — 본체엔 0 개 `[정적]` (질문 2 (d))

- **`RtlDispatchException` 본체에는 `syscall` 도 `Nt…` 호출도 없다.** 평소 길 (VEH 없음, 색인 빠른 길) 은 0.
- 안쪽에서 syscall 로 이어질 수 있는 것 (평소엔 안 탐, 대표만):

| 어디서 | syscall | 언제 |
|---|---|---|
| `RtlLookupFunctionEntry` 안 (`RtlpxLookupFunctionTable +0x2d0` / `+0x2d5`) | `NtQueryVirtualMemory` (`0x23`) | `LdrInitState ≠ 3` (가게 문 열기 전) 일 때만 (run60) |
| 같은 함수 색인 락 경합 | `NtAlertThreadByThreadIdEx` (`0x72`) · `NtSetInformationThread` (`0xd`) · `NtWaitForAlertByThreadId` (`0x1e3`) | 색인 락을 다른 직원과 다툴 때 |
| `RtlpCallVectoredHandlers +0xa6` → `RtlDecodePointer` | `NtQueryInformationProcess` (`0x19`, 클래스 `0x24`) | 암호 열쇠 캐시가 0 일 때 한 번. 초기화 중에 이미 채워짐 (run59) |

- 나머지 (`NtProtectVirtualMemory` — VEH 빼기, `RtlRaiseStatus` 오류 갈래) 는 run 문서 질문 2 (d) 표.

#### PE_emulator 에게

- 진짜 ntdll 이 돈다. 엔진은 `LdrInitState` = 3 (로더 끝), TEB `+0x8` · `+0x10` (작업대 범위) 이 새 `RSP` 아래까지 포함하면 된다 (run 문서 표 · 최소 규칙 3).

### 2.7 층마다 대피도 ② — 층 안내원 · 필터 · 되감기 (질문 4 앞부분)

이번 바퀴에서 가장 중요한 길 — **`__except` 가 사고를 받을 때** — 이다. 천천히 간다.

#### 필터 — 다시 한 번

- run28 해설 2.10 에서 처음 나왔다: 대피도에서 **"이 사고를 여기서 잡을까?"** 를 정하는 짧은 함수.
- 돌려주는 값 셋 (run 문서 "칸 이름"):
  - **1** EXECUTE_HANDLER — "여기서 잡는다" → 착지점으로.
  - **0** CONTINUE_SEARCH — "여기 아님" → 다음 항목 · 다음 층.
  - **−1** CONTINUE_EXECUTION — "사고 전 그 자리에서 그냥 이어 가" → 메모대로 되돌림.

#### 층 안내원 `__C_specific_handler` 의 걸음 `[정적]`

1. `+0x2b __except_validate_context_record` (ret `+0x30`).
2. `+0x3e TEST [카드+4],0x66` — 지금 되감는 중이 아니면 아래로.
3. 대피도 (범위 표) 고리 `+0x5d ~ +0xdf` — 사고 자리가 이 `__try` 구간 안인 항목을 찾는다.
4. **`+0x91 CALL RAX`** (ret `+0x93`) — 그 항목의 **필터** 를 부른다.
   - 인자: `RCX` = `&{카드, 메모}`, `RDX` = `EstablisherFrame`.
   - 필터 값이 상수 1 로 적힌 항목이면 부르지도 않는다 (`+0x7b` · `+0x80`).
5. 필터 값으로 갈림:
   - `< 0` (`+0x95 JS +0xea`) → 0 (ContinueExecution) 을 돌려준다 → 본 담당 TRUE → 창구 → `NtContinue` (2.8).
   - `0` (`+0x97 JLE +0xdb`) → 다음 항목.
   - `> 0` → `+0xa9 _NLG_Notify` → **`+0xd1`** (ret `+0xd6`) `RtlUnwindEx(EstablisherFrame, 착지, 카드, ReturnValue = (int64)코드, 메모, 히스토리)`.
     - `ReturnValue` = 예외 코드 (`+0xbc MOVSXD R9,[R12]`). 이게 나중에 착지점의 `EAX` 가 된다 (2.13).

#### 새 말 — 되감기 (unwind) 의 두 꼴

- **되감기 (unwind)** = 콜스택을 **거꾸로** 거슬러 가는 일. run15 해설의 **풀어 보기** 와 같은 말이다. 이번엔 두 꼴로 나온다.

| | 풀어 보기 (계산만) | 되감기 (진짜로 감) |
|---|---|---|
| 누가 | `RtlpxVirtualUnwind` (`RtlDispatchException +0x3e3`) | **`RtlUnwindEx`** (`__C_specific_handler +0xd1`) |
| 언제 | 받을 사람을 **찾는 동안** | 받을 층이 **정해진 뒤** |
| 무엇 위에서 | 메모의 **사본** | `RtlUnwindEx` 가 **새로 쓴 메모** |
| 중간 층 | 그냥 지나감 | 중간 층의 `__finally` 를 차례로 실행 (`RtlpExecuteHandlerForUnwind`) |
| 끝 | "한 층 위 `Rip` · `Rsp`" 숫자만 | 목적 층 착지점으로 **실제로 이동** (`RtlRestoreContext`) |

- **`__finally`** — `__try` 구간을 어떻게 나가든 (사고로 나가도) 꼭 하는 정리 코드로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유:
  - 풀어 보기 = 비상 대피도를 보며 **손가락으로** 층을 짚어 올라가 보는 것.
  - 되감기 = 정해진 층까지 **진짜로 걸어 올라가며**, 지나는 층마다 켜 둔 불을 끄고 (정리), 그 층 착지 매트에 내려서는 것.

#### `RtlUnwindEx` (rva `0x13380`) 의 걸음 `[정적]`

1. `+0xa7` (ret `+0xac`) `RtlpGetStackLimits` — 실패면 `+0xb4 ~ +0xb9 RtlRaiseStatus(0xC0000028)`.
2. `+0x886 RtlInitializeExtendedContext2` · **`+0x892`** (ret `+0x897`) **`RtlCaptureContext2`** — 지금 레지스터를 **새 메모** 에 받아 적는다. 체크 칸 = **`0x10000f`** (`RtlCaptureContext2 +0xf3`).
3. 층마다: `RtlLookupFunctionEntry` (`+0xf5`) · `RtlpCopyContext` (`+0x107`) · `RtlpxVirtualUnwind` (`+0x21e`) · 중간 층 `__finally` 는 `RtlpExecuteHandlerForUnwind` (`+0x2fb` / ret `+0x300`).
4. 목적 층 (`+0x3e5 CMP RCX,R12`) 에서 새 메모를 고친다:
   - `Rax` = `ReturnValue` (`+0x3f9`)
   - `Rip` = 착지점 (`+0x40c`)
   - 카드 코드를 `0xC0000027` 로 덮음 (`+0x41b`)
5. `+0x5aa` (ret `+0x5af`) `RtlGuardCheckExceptionHandler` — test.exe 에선 아무것도 안 함 (2.6).
6. **`+0x606`** (ret `+0x60b`) **`RtlRestoreContext(새 메모, 카드)`** → 체크 칸 `0x10000f` → **유저 `IRETQ`, syscall 없음** (2.8).
- 목적 층을 못 찾고 스택 끝이면: `+0x72c ~ +0x743 NtRaiseException(…, FALSE)` (ret `+0x748`).

#### 여기서 꼭 짚을 것

- CPU 예외 (커널이 쌓은 메모, `0x10001f`) 라도 `__except` 가 받으면, 되돌리는 메모는 **커널이 쌓은 것이 아니라 `RtlUnwindEx` 가 새로 쓴 것 (`0x10000f`)** 이다 `[정적]` (run 문서 최소 규칙 6 첫 갈래).
- 비유: 관리사무소가 작업대에 내려놓은 사고 보고서 묶음은 **읽기만** 하고, 착지할 때 쓰는 메모는 관리실이 **제 손으로 새로** 쓴다. 그래서 다음 2.8 의 갈림에서 "관리실 혼자" 쪽으로 간다.

### 2.8 ★ 끝맺음 — 체크 칸 (이름표) 하나가 가른다

#### 새 말 둘

- **문맥 이름표** (`ContextFlags`, 메모 `+0x30`) — 메모 맨 위 **체크 칸** 이다 (run42 · run80 · run81 해설).
  - 이번 바퀴에선 이 체크 모양이 **"누가 쓴 메모인가"** 를 알려 준다 (아래 표 — run 문서 "어디서 만든 문맥 | 이름표"). 그래서 run 문서는 "이름표" 라고 부른다. 이 글도 따른다.
  - 체크 칸 풀이 (run81 해설 2.8 표): `0x10000f` = CONTROL · INTEGER · SEGMENTS · FLOATING_POINT 넷. `0x10001f` = 거기에 DEBUG_REGISTERS (`0x10`) 까지.
- **`RtlRestoreContext`** (rva `0x123e20`) — 메모대로 CPU 레지스터를 되돌려 싣고 그 자리로 보내는 ntdll 함수 `[정적: 하는 일]`.
  - 비유: 관리실 **복원 담당**. 메모를 받아 직원을 메모대로 다시 세운다. 혼자 할 수 있으면 혼자, 못 하면 관리사무소에 맡긴다.

#### 누가 쓴 메모가 어떤 이름표인가 `[정적]` (run 문서 표)

| 어디서 만든 메모 | 이름표 | 끝맺음 |
|---|---|---|
| 커널이 유저 스택에 쌓은 것 (run81) | **`0x10001f`** (XSTATE 면 `0x10005f`) — `0x10` (DEBUG_REGISTERS) 포함 | **`NtContinue(메모, 0)`** |
| `RtlRaiseException` 이 받아 적은 것 | 처음 `0x10000b` (KUSD `+0x3d8` ≠ 0 이면 `0x10004b`) → 적은 뒤 **`0x10000f`** (`0x10004f`) | **유저 `IRETQ`** |
| `RtlUnwindEx` 가 받아 적은 것 (`__except` 로 되감을 때) | **`0x10000f`** (`RtlCaptureContext2`, call `+0x892`) | **유저 `IRETQ`** |
| `RtlDispatchException` 의 지역 사본 | `원본 & 0x10000f` | (걷기용, 끝맺음 아님) |

- 받아 적는 손은 둘 다 **`RtlCaptureContext2 +0xf3 OR [ctx+0x30],0x10000f`** 다 (`+0xc0 AND [ctx+0x30],0x100040` 뒤).
  - `RtlRaiseException` 은 `RtlpCaptureContext2` 를 부르고, 그게 `+0x6c` 에서 `RtlCaptureContext2 +0x3c` 로 점프한다.

#### 복원 담당의 걸음 `[정적]` (`RtlRestoreContext`, rva `0x123e20`)

1. **CET 검사** `+0x150 ~ +0x172` — 시작 정보 블록 `+0xa8` 의 비트 `0x3c` · `0xc` 중 하나라도 켜지면 `+0x183` (`NtContinue`) 으로.
   - 줄: `+0x150 LEA RAX,[LdrSystemDllInitBlock+0xa0]` · `+0x157 MOV RAX,[RAX+8]` · `+0x15b BT RAX,0x3c` · `+0x160 JC +0x183` (그리고 `+0x162 ~ +0x172` 가 비트 `0xc` 로 한 번 더).
   - test.exe 는 둘 다 꺼짐 — `+0xa8 = 0` `[관찰]` (run44 K1 · P1a · P1b · P2, sast/55 결과서).
2. **이름표 비교** `+0x174 ~ +0x181` (2.3 줄 2):
   - `+0x177 AND EAX,0xffffff3f` 로 `0x40` (XSTATE) 같은 비트를 지운 뒤 `0x10000f` 와 비교.
   - 그래서 `0x10004f` 도 같은 쪽으로 간다.
3. **같으면 — 가게 안에서 혼자** (`+0x181 JZ +0x1a3`):
   - 이름표에 `0x40` 이 있을 때만 `XRSTOR` (`+0x1a3 ~ +0x1e3`).
   - `+0x1e7 FXRSTOR` · XMM0 ~ 15 · `+0x266 LDMXCSR` · 레지스터들 · **`+0x300 IRETQ`**.
4. **다르면 — 관리사무소에 맡김**:
   - **`+0x183 XOR EDX,EDX` · `+0x185 CALL NtContinue`** (ret `+0x18a`) — `NtContinue(메모, TestAlert = 0)`.
   - 돌아오면 `+0x18a CMP EAX,0xc000060a` → 같으면 `+0x196 INT 0x29` (ECX `0x30`), 아니면 그냥 돌아간다 (사고 처리 창구에서 왔으면 창구가 `RtlRaiseStatus` 로, 2.4).

#### 유저 모드 `IRETQ` — 관리실이 직접 하는 순간 이동

- run15 해설에서 `IRETQ` 는 관리사무소가 출발 메모대로 직원을 가게로 **순간 이동** 시키는 명령이었다 (커널 → 유저).
- 여기선 **관리실이 가게 안에서 가게 안으로** 같은 명령을 쓴다 `[정적]` (`+0x300`).
- 왜 `RET` 이 아니라 `IRETQ` 인가는 근거 문서에 이유가 적혀 있지 않다. 짐작 `[추측]`:
  - `IRETQ` 는 `Rip` · `CS` · `EFlags` · `Rsp` · `SS` 를 **한 번에** 바꾸는 명령으로 알려져 있다 (일반적으로 알려진 설명).
  - 메모대로 "서 있던 자리 (`Rsp`) 와 할 일 (`Rip`) 을 동시에" 되돌리려면 이게 편해서로 보인다.
- PE_emulator 쪽 숙제 (run 문서): 유저 `IRETQ` (`CS = 0x33`, `SS` = 메모 값) 를 Unicorn 이 그대로 실행할 수 있어야 한다.

#### 왜 커널이 쌓은 메모만 관리사무소에 맡기나 — 단계별로

1. `[정적]` 두 이름표의 차이는 `0x10` (DEBUG_REGISTERS) **한 칸** 이다.
2. `[정적]` `NtContinue` 가 기대하는 답 칸 (run 문서 syscall 표): "`0x10` 비트면 `Dr0 ~ 7`" 까지 복원 (run29 · run42).
3. `[추측]` (일반적으로 알려진 설명) 디버그 레지스터 (`Dr0 ~ 7`) 는 유저 모드 명령으로는 못 바꾸는 것으로 알려져 있다.
4. 그래서 → 디버그 레지스터까지 들어 있는 메모를 그대로 되돌리려면 관리사무소 창구에 맡겨야 하는 것으로 보인다 `[추측]`.
5. 아직 확인 아님 — 코드는 "이름표가 다르면 `NtContinue`" 까지만 말한다. 이유를 말해 주지는 않는다.

#### 그래서 — 가장 흔한 길은 syscall 0 개

- `[정적]` **`__except` 가 받는 길** (CPU 예외든 소프트웨어 예외든): 되감기 → `RtlRestoreContext(0x10000f)` → **유저 `IRETQ`. `NtContinue` 도 안 부른다.**
- `[정적]` `RtlRaiseException` 이 받아졌을 때도 같다 (2.9).
- `[정적]` `NtContinue` 가 필요한 건 **커널이 쌓은 메모를 사고 처리 창구가 그대로 되돌릴 때** — VEH −1, 필터 −1 (SEH `ExceptionContinueExecution`) — 뿐이다.
- 비유: 관리실 복원 담당은 **관리실이 쓴 메모** 면 혼자 직원을 순간 이동시킨다. **관리사무소가 쓴 메모** (디버그 칸까지 체크된 것) 를 그대로 되살려야 할 때만 "제 출발 메모를 이걸로 바꿔 주세요" 창구 (run29) 에 낸다.

### 2.9 일부러 낸 사고 — `RtlRaiseException` 은 커널을 안 거친다 (질문 3)

#### 걸음 `[정적]` (`RtlRaiseException`, rva `0xf700`, 685B)

1. `+0x5f BTS [카드+4],7` — 카드 플래그에 `0x80` (SOFTWARE_ORIGINATE, "일부러 낸 사고").
2. `+0x64 MOV R14D,0x10000b` — 이름표 처음 값. (`+0x8d` KUSD `+0x3d8` 이 0 이 아니면 `+0x1e2 MOV R14D,0x10004b`.)
3. `+0xa0 RtlGetExtendedContextLength3` · `+0xc2 __chkstk` · `+0xdc RtlInitializeExtendedContext2` — 메모 자리 만들기.
4. **`+0xe6`** (ret `+0xeb`) `RtlpCaptureContext2` — 지금 레지스터를 메모에 받아 적음. 이름표 → `0x10000f`.
5. `+0x111` (ret `+0x116`) `RtlLookupFunctionEntry` + **`+0x147`** (ret `+0x14c`) `RtlVirtualUnwind` — **자기 층을 한 칸 풀어 본다.** 그래서 메모의 `Rip` · `Rsp` = 부른 쪽 것.
   - `+0x119 JZ +0x1d2`: 자기 함수 줄을 못 찾으면 곧장 `RtlRaiseStatus`.
   - `+0x14c CMP [카드],0x80000033` 이면 `+0x152 JZ +0x242` 로 한 칸 더.
6. `+0x158 ~ +0x15f` — 카드 사고 장소 = 메모 `Rip`.
7. `+0x16a RtlpGuardSynchronizeRestorePc` (한 줄 `RET` 자리).
8. **`+0x16f MOV RAX,GS:[0x60]` · `+0x17e CMP byte [RAX+2],R15B` · `+0x182 JNZ +0x29f`** — PEB `+0x2` `BeingDebugged` 를 본다.
9. 0 이면 → **`+0x188`** (ret `+0x18d`) **`RtlDispatchException`** — 커널을 안 거치고 직접.

#### 갈래 `[정적]`

| 경우 | 줄 | 결과 |
|---|---|---|
| **받음 (TRUE)** | `+0x195 ~ +0x19b` (ret `+0x1a0`) `RtlRestoreContext(메모, 카드)` | 이름표 `0x10000f` → **유저 `IRETQ`** (syscall 0). ret 자리로 돌아오면 오류 |
| **못 받음 (FALSE)** | `+0x294 XOR R8D,R8D` · `+0x29d JMP` → **`+0x2a2`** (ret `+0x2a7`) `NtRaiseException(…, 0)` | 커널 둘째 기회 (run81). 돌아오면 `+0x1d4 RtlRaiseStatus` |
| **디버거가 붙어 있음** (`BeingDebugged ≠ 0`) | `RtlDispatchException` 을 건너뛰고 `+0x29f MOV R8B,1` → `+0x2a2` `NtRaiseException(…, 1)` | 커널이 디버거에게 먼저 묻고 (run81 3 · 4), 안 받으면 `KiUserExceptionDispatcher` 로 보내 그때 유저 디스패치 |

- 그래서 "몇 번째 기회로 들어가나" 의 답 `[정적]`:
  - **디버거 없음 → 유저가 먼저 (커널 안 감).** 못 받아 커널로 갈 때만 `FirstChance = 거짓`.
  - **`FirstChance = 참` 은 디버거가 붙었을 때만.**

#### kernelbase `RaiseException` 이 여기로 온다 `[정적]` (rva `0xc4140`, 177B)

- `+0x3a ~ +0x59` 스택에 카드를 만든다:
  - `[RSP+0x20]` 코드 · `[RSP+0x24]` 플래그 `& 1` · `[RSP+0x28]` 0 · **`[RSP+0x30]` = 자기 주소** (`+0x4a LEA`) · 개수 `min(n, 15)` (`+0x5b ~ +0x70`) · `+0x79 memcpy`.
- **`+0x83`** (ret `+0x8a`) `[IAT] RtlRaiseException`.
- 최종 사고 장소 = **`RaiseException +0x8a`** — `RtlRaiseException +0x15f` 가 "부른 쪽 돌아올 자리" 로 덮는다 (위 5 · 6).
- 받아지면 `RaiseException +0x8a` 로 돌아가 `+0x9a` 쿠키 검사 뒤 `RET`. 비휘발 레지스터는 `RtlVirtualUnwind` 가 `RaiseException` 의 값으로 되돌려 둔 상태다.

#### 비유

- CPU 사고는 관리사무소가 먼저 알고 묶음을 쌓아 내려보냈다 (run81).
- **일부러 낸 사고** 는 다르다. 직원이 스스로 손을 들면:
  1. 관리실이 **제 자리에서** 메모를 직접 쓰고 (`RtlpCaptureContext2`),
  2. 한 칸 풀어 "손 든 자리" (`RaiseException +0x8a`) 를 사고 장소로 적고,
  3. **가게 감독관** (유저 디버거) 이 붙어 있는지 본다.
  4. 없으면 관리사무소에 안 가고 곧장 창구 안 일 (VEH → SEH) 부터 한다.

#### PE_emulator 에게 (run 문서 표 질문 3 줄)

- 짐작 "소프트웨어 예외는 유저 안에서 돈다" **맞다** (디버거가 없을 때 = `PEB.BeingDebugged` 0).
- 틀린 점 둘:
  1. 못 받으면 `NtRaiseException(…, FALSE)` 로 커널에 간다 — 엔진 구현 필요.
  2. 유저 `IRETQ` (`CS = 0x33`, `SS` = 메모 값) 를 Unicorn 이 그대로 실행해야 한다.

### 2.10 test.exe 의 진짜 사고 — `OutputDebugStringA` 는 커널까지 안 간다 (덧붙임 3)

run81 해설 2.20 ③ 에서 "이게 커널을 거치는지는 Q148b" 로 넘겼던 질문이다.

#### 단계별로

1. `[정적]` `OutputDebugStringA` (kernelbase rva `0xc3df0`) 의 대피도 (결과서 `.pdata`):
   - **`__try [+0x42, +0x81)`**, 필터 `OutputDebugStringA$filt$0` (rva `0x1995f4`), 착지 `+0x81` (DBWin 몸통, run71).
   - 필터: `+0x1a CMP ECX,0x40010006` · `+0x20 SETZ AL` → **코드가 `0x40010006` 이면 1, 아니면 0**.
   - (그 뒤 `__try [+0x1a4, +0x29b)` 는 필터 `$filt$1` — 스택 넘침 `0xC00000FD` 만 빼고 다 받음 — 와 정리 `$fin$2`.)
2. `[정적]` `+0x6e MOV ECX,0x40010006` · `+0x73 LEA R8D,[RDX+2]` (개수 2) · **`+0x77`** (ret `+0x7c`) `RaiseException` → `RaiseException +0x83/+0x8a` → `RtlRaiseException` (2.9). 플래그 0 (이어 갈 수 있음).
3. `[정적]` `RtlRaiseException +0x17e` 는 **`PEB.BeingDebugged` 만** 본다.
4. `[관찰]` run44 (커널 디버거 KDNET 만 붙은 실행) 의 PEB `BeingDebugged` 는 **0** 이다.
   - `ms/logs/04_baggage-read_P1a.txt:1723` · `_P1b.txt:2421` · `_P2.txt:2433`.
   - 첫 `OutputDebugStringA` 가 있는 P1b ~ P2 구간도 포함된다.
5. 그래서 → 그 실행에서 `+0x188 RtlDispatchException` 을 직접 불렀을 것이다 `[정적 추론]`.
6. `[관찰]` VEH 비트는 꺼져 있다 (`CrossProcessFlags` 0 · 2, 2.5).
7. 그래서 → SEH 걷기는 `OutputDebugStringA` 자기 `__except` 에서 잡힌다 (run 문서: 첫 프레임) → `$filt$0` = 1 → `__C_specific_handler` → `RtlUnwindEx` → `RtlRestoreContext(0x10000f)` → **유저 `IRETQ`** → 착지 `+0x81`.
8. **중간에 커널 · `NtContinue` · `NtRaiseException` 없음.** `nt!KiDispatchException` · `KiUserExceptionDispatcher` 는 이 길에서 한 번도 안 불린다.

- 아직 확인 아님: 사진은 은퇴 (P2) 까지다. `main` 의 `OutputDebugStringA` 때 `BeingDebugged` 값은 `[추측]` 이다 (같은 칸을 바꿀 코드가 이 길엔 없음, run 문서 ⑤).

#### 그래서 무엇이 바뀌나

- run71 의 "디버거가 받으면 아래 안 탐" 은 **맞다** — 디버거가 없으면 자기 `__except` 가 받아 `+0x81` 몸통으로 간다 (run 문서 "정정" 절: 고칠 곳 없음).
- flow.md 2.10 run71 절의 "`[관찰 간접]` 커널 디버거는 `0x40010006` 을 삼키지 않았다" 는, 더 정확히 말하면 **커널까지 안 갔다** `[정적]`. 그 줄 뒤에 (run82) 한 줄이 붙었다.
- **"test.exe 로 예외 길을 VM 에서 볼 수 있나" 의 답: 유저 쪽만.** 커널 쪽 (run81 동적 1 · 2 · 5) 은 **새 샘플 (Q156)** 이 있어야 본다.
  - 볼 수 있는 자리 (결과서): `ntdll!RtlRaiseException+0x188` · `RtlDispatchException+0x205` · `RtlpExecuteHandlerForException+0xd` · `__C_specific_handler+0x93` · `RtlUnwindEx+0x606` · `RtlRestoreContext+0x181`.
- 비유: test.exe 직원이 `Log` 할 때마다 "디버거 있어요?" 하고 손을 든다 (run71). 가게 감독관이 없으니 관리실이 받아서, 그 층 안내원이 "아, 이 손은 내가 받는 손" (`0x40010006` → 1) 하고 바로 그 층 착지 매트로 데려다준다. 관리사무소는 이 일을 **모른다**.

### 2.11 아무도 안 받으면 ① — 꼭대기 층의 마지막 그물 (질문 4)

#### 꼭대기 층의 대피도 `[정적]` (`RtlUserThreadStart`, rva `0xacac0`)

- 결과서 `.pdata` scope table:
  - **`__try [+0x7, +0x4a)`** · 필터 `RtlUserThreadStart$filt$0` (rva `0x169b61`) · **착지 `+0x4a`** · 층 안내원 `__C_specific_handler` (rva `0x124670`).
- `__try` 안에서 부르는 것: call `+0x27` / `+0x3d` 의 시작 함수 (`BaseThreadInitThunk` 또는 사용자 시작 주소) 와 `+0x44 RtlExitUserThread` (run29).
  - 즉 스레드가 하는 **모든 일** 이 이 `__try` 안에서 돈다. 그래서 아무 층도 안 받은 사고는 걷기의 맨 끝에서 이 층에 닿는다.

#### 필터 `$filt$0` `[정적]`

1. `+0x19 MOV RCX,[RtlpUnhandledExceptionFilter]` (rva `0x1d28e8`) — 칠판의 **"마지막 담당"** 칸.
2. **`+0x20`** (ret `+0x25`) `RtlDecodePointer` — 암호를 푼다 (run59).
3. 널이 아니면 **`+0x3a`** (ret `+0x3f`) CFG 간접 호출 (`RCX = EXCEPTION_POINTERS`) — 풀린 함수를 부른다.
4. 널이면 **`+0x4c`** (ret `+0x51`) `RtlUnhandledExceptionFilter2(포인터, 빈 문자열)`.

#### 그 칠판엔 누가 적었나 `[정적]` — run68 과 이어짐

- kernelbase 시작 함수 `_KernelBaseBaseDllInitialize +0x4ab` (ret `+0x4b2`) `RtlSetUnhandledExceptionFilter(UnhandledExceptionFilter)` (run68).
- 그래서 kernelbase 가 올라온 프로세스에서 `$filt$0` 은 **kernelbase `UnhandledExceptionFilter`** (rva `0x10ed90`) 를 부른다.
- 사용자가 `SetUnhandledExceptionFilter` 로 바꾸는 건 이 ntdll 칠판이 **아니다**. kernelbase 의 `BasepCurrentTopLevelFilter` 다 (2.12 표 7번).
- `RtlUnhandledExceptionFilter2` 는 칠판이 **널일 때만** 탄다.
  - 디버거가 없으면 `RtlIsAnyDebuggerPresent` (`PEB+2` 또는 `KUSD+0x2d4 & 3 == 3`) 가 거짓 → `+0x391` → 코드 `0xC0000409` 일 때만 `RtlReportException`.
  - 반환 = `코드 ≠ 0xC0000194 ? 0 : −1` = 보통 0.
  - (이 VM 은 KUSD 칸 값 때문에 이 판정이 달라질 수 있다 — 2.17.)

#### 비유

- run68 해설 그대로: kernelbase 가 문을 열 때 관리실 칠판에 **"사고 나면 부를 마지막 담당 = ○○"** 을 암호로 적어 뒀다.
- 그 칠판을 **읽는 곳** 은 건물 꼭대기 층 대피도의 필터다. flow.md 2.10 에 한 줄로만 있던 이 읽기가, 이번에 2.5 의 쓰는 줄과 이어졌다 (flow.md 2.5 에 run82 줄).
- 꼭대기 층에 올라온 사고는 칠판의 암호를 풀어 그 마지막 담당 (`UnhandledExceptionFilter`) 에게 넘겨진다. 그 담당이 안에서 무엇을 하는지가 이번에 처음 채워졌다 (2.12).

### 2.12 아무도 안 받으면 ② — `UnhandledExceptionFilter` 안 (대표만)

조건: 디버거 없음 · 보통 코드 · secure 아님 · 사용자 필터 없음. `call / ret`. 번호는 run 문서 표 그대로라 중간이 빈다.

| # | 자리 | 하는 일 → syscall | 결과로 갈리는 곳 |
|---|---|---|---|
| 0 | `+0x5a` · `+0x5e` | 카드 플래그 `0x10` (중첩) 검사 | 켜져 있으면 **0** 로 끝 |
| 3 | `+0xb8/+0xbf` | **delay-load** `CheckForReadOnlyResourceFilter` (첫 호출 때 풀림. kernelbase 가 부르면 호스트 `kernel32.dll`) | −1 이면 **−1** 로 끝 |
| 4 | `+0xd5/+0xda` | `BasepIsDebugPortPresent` → **`NtQueryInformationProcess(-1, 7)`** (8B) | ≠ 0 이면 **0** 로 끝 |
| 6 | `+0x110/+0x117` | **`NtSetInformationProcess(-1, 0x3f, 값 8)`** (클래스 이름 `[추측]`) | 반환값 안 봄 |
| 7 | `+0x126/+0x12d` · `+0x145/+0x14c` | 락 · `RtlDecodePointer(BasepCurrentTopLevelFilter)` | 널이면 넘어감. 널이 아니면 **사용자 필터** 를 CFG 간접 호출 (`+0x1ee`) |
| 9 | `+0x282/+0x289` | **`NtSetInformationProcess(-1, 0x3f, 값 1)`** | 반환값 안 봄 |
| 10 | `+0x28e` · `+0x294` | `BasepIsSecureProcess` | secure 면 **0** 로 끝 |
| 11 | `+0x29a/+0x29f` | `GetErrorMode` → **`NtQueryInformationProcess(-1, 0xc)`** (4B) | **`+0x29f TEST AL,2` · `+0x2a1 JNZ +0x5a6`** → 비트 `0x2` (SEM_NOGPFAULTERRORBOX) 면 **1** 로 끝 (WER 안 감) |
| 13 | `+0x2d1/+0x2d8` | **`NtQueryInformationJobObject(NULL, 2, 버퍼 0x40, NULL)`** | 실패면 무시. 성공이고 `LimitFlags & 0x400` 이면 **1** 로 끝 |
| 14 ~ 16 | `+0x2f8` · `+0x315` · `+0x345/+0x34c` | apiset 있나 → `LoadLibraryExW(…errorhandling…)` → **delay-load `BasepReportFault`** = WER 본체 (**이름만**) | 반환 `ESI` 로 17 |
| 17 | `+0x380 ~ +0x38d` · `+0x39d/+0x3a2` | `ESI ∈ {3, 9, 10}` 이거나 WER 를 못 불렀으면 → `RaiseFailFastException` (2.14). `ESI == 8` → **0** 로 끝 | |
| 18 ~ 22 | `+0x3e0` ~ `+0x59d` | (WER 가 안 정함, `ESI == 4`) 고전 길: … **`+0x4ec/+0x4f3 NtRaiseHardError(0xD0000144, 4, …)`** … | 기대 답 = 성공 + 응답 6 (OK). 오류 심각도 상태면 응답을 6 으로 침 |
| 끝 | **`+0x59d MOV EAX,[RSP+0x3c]`** · `+0x5a1 JMP +0x20d` | `[RSP+0x3c]` 는 `+0x64` 에서 1 로 시작 (AeDebug 를 띄운 경우만 `+0x562` 에서 0) | **기본 반환 = 1** |

- 모두 `[정적]`. 빠진 줄 (1 · 2 · 5 · 8 · 12 · 19 · 21 등) 은 run 문서 질문 4 표.
- 표에서 처음 나오는 이름:
  - **WER** = 사고 **신고센터** (run81 해설 2.15). 이번에도 본체는 이름만 봤다.
  - **잡 (job)** — 프로세스 여럿을 한 묶음으로 관리하는 커널 객체로 알려져 있다 `[추측]` (일반적으로 알려진 설명). `NtQueryInformationJobObject(NULL, …)` 의 NULL = 내 프로세스의 잡 (run 문서 syscall 표). `0x400` 은 결과서가 DIE_ON_UNHANDLED_EXCEPTION 이라고 적었다.
  - secure 프로세스 · SEM_NOGPFAULTERRORBOX 는 이름 그대로의 뜻으로 보인다 `[추측: 이름]` — 각각 "특별히 보호된 프로세스", "오류 상자를 띄우지 마".
  - delay-load 는 [run25 해설 2.5](../02_sprint/run25-dll-entry-해설.md) 의 delay import 다. 도구 메모: 지도가 이걸 보통 import (`external`) 로 적는다 — "이 호출이 로더 일을 부를 수 있다" 를 놓치기 쉬워서 하네스 제안으로 올라갔다 (run 문서 "도구").

#### 돌려주는 값 — 기본은 1 `[정적]`

- 디버거가 없으면 **기본 1 (EXECUTE_HANDLER, "여기서 잡는다")**.
- 예외로 **0**: 디버거 포트 · secure · 중첩 · AeDebug 띄움 · `ESI == 8`.
- 예외로 **−1**: 읽기 전용 자원 필터 · 코드 `0xC0000194`.

#### 흔한 단축 `[정적]`

- 4 뒤 **11 의 비트 `0x2` 가 켜지면** syscall 4개 (클래스 7 · `0x3f` · `0x3f` · `0xc`) 로 끝난다. WER · 로드 없음.
- 꺼지면 14 부터 WER 길 (이름만), 또는 apiset 이 없을 때 고전 길 (18 ~ 22).
- 11 의 값 출처: `EPROCESS.DefaultHardErrorProcessing` (짐 지도 `+0x388`) — 부모 값 복사 / 부모 없으면 5 (결과서). 비트 `0x2` 는 **보통 꺼져 있다고 보는 게 안전** 하다 — 부모 cmd 값에 달렸다 `[동적]` (Q157 ② · Q156 1).

#### 비유

- 마지막 담당 (`UnhandledExceptionFilter`) 은 사고를 받으면 차례로 확인한다:
  - 가게 감독관 연락선 있나 (클래스 7) → 있으면 "감독관한테 넘겨" (0).
  - 관리사무소에 신청 두 번 (클래스 `0x3f`, 값 8 · 1 — 무엇을 바꾸는지는 아직 모름, Q157 ③).
  - "오류 상자 띄우지 마" 설정인가 (클래스 `0xc`) → 그렇다면 조용히 "내가 잡는다" (1).
  - 아니면 신고센터 (WER) 에 알리고, 신고센터가 못 정하면 관리사무소에 **"처리 안 된 사고" 알림** (`NtRaiseHardError`) 을 띄운다.
  - 결국 대부분 **"내가 잡는다" (1)** 로 끝난다.

### 2.13 아무도 안 받으면 ③ — 1 → 되감기 → 가게 문 닫기

#### 걸음 `[정적]`

1. 필터 `$filt$0` 이 1 을 돌려준다 (`UnhandledExceptionFilter` 기본).
2. 층 안내원 `__C_specific_handler +0xd1 RtlUnwindEx(…, 착지 = +0x4a, …, ReturnValue = 예외 코드, …)` (2.7).
3. `RtlUnwindEx` 가 새 메모를 쓰고, 목적 층 (꼭대기) 에서 `Rax = ReturnValue` · `Rip = +0x4a` 로 고친다 (`+0x3f2 ~ +0x3f9` · `+0x40c`).
4. `+0x606 RtlRestoreContext` → 이름표 `0x10000f` → **유저 `IRETQ`** → 착지 `RtlUserThreadStart +0x4a`.
5. **`+0x4a MOV EDX,EAX` · `+0x4c OR RCX,-1` · `+0x50 CALL NtTerminateProcess`** (ret `+0x55`) = **`NtTerminateProcess(-1, 예외 코드)`**.

#### 왜 `EAX` 가 예외 코드인가 — 단계별로 `[정적]`

1. 층 안내원이 `+0xbc MOVSXD R9,[R12]` 로 `ReturnValue` = 카드의 예외 코드를 만든다.
2. `RtlUnwindEx` 가 목적 층 메모의 `Rax` 칸 (`+0x78`) 에 그 값을 넣는다 (`+0x3f2 ~ +0x3f9`).
3. `IRETQ` 로 착지하면 그 메모대로 `RAX` = 예외 코드.
4. 그래서 착지 첫 줄 `MOV EDX,EAX` 가 끝 코드 = 예외 코드를 만든다.

- 비유: 되감기가 꼭대기 층 착지 매트에 직원을 내려놓으면서 **손에 사고 이름 쪽지** 를 쥐여 준다. 직원은 매트 옆 "가게 문 닫기" 버튼을 누르며 그 쪽지를 문 앞에 붙인다.

#### 커널 둘째 기회까지 안 간다 — run81 에 보탬

- `[정적]` `RtlUserThreadStart` 의 `__except` 가 있으니 **평소 "아무도 안 받음" 은 커널 둘째 기회까지 안 간다.**
- 그럼 커널 둘째 기회 (`NtRaiseException(FALSE)` → run81 → `ZwTerminateProcess`) 는 언제 타나 (run 문서 최소 규칙 12):
  - 필터가 0 을 돌려줄 때 (디버거 포트 있음 · secure · 중첩 · WER 답 8)
  - 걷기가 FALSE 일 때
  - `RaiseFailFastException` · `int 0x29` (2.14)
- run81 의 최소 규칙 7 ("둘째 기회 → 종료") 은 **맞다.** 틀린 게 아니라 빠진 것이라, run81 문서는 안 고치고 이번 최소 규칙 12 로 보탰다 (run 문서 "정정" 절).
- `[정적]` 필터가 −1 이면 (`__C_specific_handler +0xea` → 0) → 본 담당 TRUE → 창구 → `NtContinue` (메모는 커널이 쌓은 원래 것).

#### 두 끝 나란히

| | 꼭대기 그물 (이번) | 커널 둘째 기회 (run81) |
|---|---|---|
| 언제 | 평소 "아무도 안 받음" (`UnhandledExceptionFilter` 가 1) | 필터 0 · 걷기 FALSE · `RaiseFailFastException` · `int 0x29` · 묶음 쓰기 실패 |
| 누가 끝내나 | ntdll `RtlUserThreadStart +0x50` `NtTerminateProcess(-1, 코드)` | nt `KiDispatchException +0x7d7` `ZwTerminateProcess(-1, 코드)` |
| 끝 코드 | 예외 코드 | 예외 코드 |

- 끝 상태는 **둘 다 예외 코드** 라 결과는 같다 (run 문서 "정정" 절). 다른 건 **누가** 문을 닫느냐다.
- 비유: 평소엔 가게 **꼭대기 층 직원이 스스로** 문을 닫는다. 관리사무소가 와서 닫는 건 마지막 담당까지 "난 못 잡아" 했을 때, 또는 비상 정지 버튼을 눌렀을 때다.

### 2.14 비상 정지 버튼은 둘 — `RaiseFailFastException` 과 `int 0x29` (질문 5)

- `[정적]` **같은 길이 아니다.**

| | `int 0x29` (`__fastfail`) | kernelbase `RaiseFailFastException` (rva `0x10a020`, 493B) |
|---|---|---|
| 들어가는 문 | CPU 명령 → 커널 `KiRaiseSecurityCheckFailure` → `KiFastFailDispatch` (run81 2.16) | **보통 syscall** `NtRaiseException(…, FALSE)` (`+0x14c` / ret `+0x153`) |
| 코드 | `0xC0000409` | 카드를 안 주면 `0xC0000602` (`+0x6f`), 플래그 1 NONCONTINUABLE (`+0x77`). 카드를 주면 `+0x18f` 에서 NONCONTINUABLE 켬 |
| 그 앞에 하는 일 | 없음 | WER 신호: `+0xc0 SignalStartWerSvc` · `+0xd9 NtQuerySystemInformation(0x73)` · `+0x12e WaitForWerSvc`. 하나라도 실패 · 시간 초과면 (플래그에 따라) `NtRaiseHardError` (`+0x113`) 뒤 `TerminateProcess` |
| 유저 VEH · SEH | 안 거침 | 안 거침 (`FirstChance` 0, `+0x13e XOR R8D,R8D`) |
| 끝 | 커널 둘째 기회 → `ZwTerminateProcess(-1, 0xC0000409)` | 커널 둘째 기회 → `ZwTerminateProcess(-1, 0xC0000602)` (카드를 안 줬을 때, run81 질문 4) |

- ntdll 안의 `INT 0x29` 들 (`RtlFailFast2` · `RtlpCallVectoredHandlers +0x15e` · `RtlDispatchException +0x7f2` · `RtlRestoreContext +0x196` 등) 은 왼쪽 길이다.
- 부르는 쪽: `UnhandledExceptionFilter +0x39d` (WER 답 3 · 9 · 10, 또는 WER 를 못 불렀을 때 — 2.12 표 17).
- 비유:
  - `int 0x29` = 관리사무소 **직통 비상 정지 버튼** (run81). 누르면 곧장 문 닫힘.
  - `RaiseFailFastException` = **신고센터에 먼저 알리고** (WER 신호) 관리사무소 창구에 "이어 갈 수 없는 사고, 처리 담당 묻지 마" 를 내는 버튼. 끝은 같다.
- PE_emulator: `int 0x29` 는 run81 규칙 그대로 (훅 → `0xC0000409` 종료). 이건 `NtRaiseException(FALSE)` → 종료만 있으면 된다 (run 문서 표).

### 2.15 run81 이 넘긴 덧붙임 닫기 — `CONTEXT_EX` · 머신 프레임 · `Entries` (덧붙임 1 · 2)

run81 해설 2.11 · 2.17 에서 "ntdll 이 그 칸을 읽나는 Q148b" 로 넘겼던 것들이다.

#### (가) 목차 쪽지 (`CONTEXT_EX`, 메모 `+0x4d0`) — 이름표에 `0x100040` 이 다 있을 때만 읽는다 `[정적]`

- 읽는 곳 다섯이 모두 같은 문턱을 둔다:
  - `RtlpSanitizeContext` (rva `0xcbfd0`) `+0x39 AND EAX,0x100040` · `+0x3e CMP` · `+0x43 JNZ +0xa0` (끝).
  - `RtlpCopyContext` (rva `0xccbe0`) `+0x1f` · `+0x25` · `+0x2b` — 주는 쪽 · 받는 쪽 둘 다 `0x40` 일 때만.
  - `RtlpVirtualPopShadowStack` (rva `0xd4760`) — `0x100040` 이고 CET_U (`0x800`) 일 때만.
  - `RtlpUnwindPrologue +0x3b6` — 풀고 있는 **지역 사본** 의 것.
  - `RtlRestoreContext +0x1a3 ~ +0x1e3` — `0x40` 이 있을 때만 `XRSTOR`.
- 그래서 → **커널이 `0x10001f` 로 쌓으면 문턱이 다 거짓이라 `+0x4d0` 을 안 읽는다.**
- KUSD `XState` 가 진짜 값 `0xe7` 이어도 상관없다:
  - 사본 이름표는 `RtlDispatchException` 이 CET 일 때만 `0x10004b` 로 정한다.
  - `RtlRaiseException` 이 KUSD 를 보는 건 **자기** 메모 크기를 정할 때뿐이다 (`+0x8d`).
  - `0x10001f` 자체는 `RtlpValidateContextFlags2` 가 허용한다 (`+0xa → +0x15`).
  - (`0xe7` 에 비트 `0x800` 도 없다. KUSD `+0x708` 값은 모른다.)
- 비유: 관리실은 메모 체크 칸에 **"별지 있음"** (`0x40`) 이 체크돼 있을 때만 목차 쪽지를 들춰 본다. 체크가 없으면 쪽지가 붙어 있든 말든 안 본다.

#### (나) "사고 직전 위치" 표지 (머신 프레임, `C+0x590`) — `Rip` · `Rsp` 둘만 `[정적]`

- 읽는 손: `RtlpUnwindPrologue` 의 `UWOP_PUSH_MACHFRAME` 처리 (`+0x404 ~ +0x474`).
  - `ctx.Rip = [표지 +0x0]` (`+0x45f`), `ctx.Rsp = [표지 +0x18]` (`+0x469`) — **둘만**. (오류 코드 있는 꼴이면 `+0x8` · `+0x20`.)
  - 대신 두 칸 주소가 `[StackLimit, StackBase − 8]` 안인지 본다 (`+0x440 ~ +0x561`). 벗어나면 `0xC0000028`.
- **`CS` · `EFlags` · `SS` 칸은 안 읽는다.** run81 표의 "`+0x598` · `+0x5a0` · `+0x5b0` 안 씀" 과 맞다.
- 쓰임: 풀어 보기가 사고 처리 창구 층 (`KiUserExceptionDispatcher`, 설명서에 `PUSH_MACHFRAME`) 을 지날 때, 이 표지로 **사고 난 함수 층** 으로 넘어간다 (run 문서 최소 규칙 6).
- 비유: run81 해설 2.11 의 "되짚어 보는 사람" 이 실제로 나타났다. 그는 표지에서 **"원래 서 있던 자리 (`Rsp`)" 와 "하던 일 (`Rip`)"** 두 줄만 읽는다.

#### (다) `RtlpContinuationContextMachineFrameEntries` — CET 그림자 스택용 `[정적]`

- 표 (rva `0x1cc208`, 8B × 2): `InitSpecialMachineFrames +0x46` 이 쓴다 (run59: `[0]` = `KiUserExceptionDispatcher` 설명서 주소, `[1]` = `KiUserApcDispatcher`).
- **읽는 곳은 하나**: `RtlpIsContinuationContextMachineFrameEntry` (rva `0xee350`, 36B). 그걸 부르는 곳도 **하나**: `RtlpUnwindPrologue +0x477` (ret `+0x47c`).
- 차례:
  1. `PUSH_MACHFRAME` 을 만나 `Rip` · `Rsp` 를 꺼낸 **뒤**,
  2. `+0x474 MOV RCX,RDI` (지금 풀고 있는 함수의 설명서) → `+0x477`,
  3. 참이면 `+0x487 RtlpVirtualPopShadowStack(메모)`, 거짓이면 `+0x47e JZ` 로 건너뜀.
- `RtlpVirtualPopShadowStack` 은 이름표에 `0x40` 이 없거나 CET_U 가 꺼지면 첫 줄들에서 `RET`.
- 그래서 → **test.exe (CET 꺼짐, `+0xa8 = 0`) 에선 결과에 영향 없다.** 엔진은 이 표를 신경 쓸 필요가 없다 (진짜 ntdll 이 `LdrpInitialize +0x22 → InitSpecialMachineFrames` 로 스스로 채움).
- 정리 (run 문서): 표지 `+0x590` 은 **걷기를 가능하게 하는 값**, `Entries[0]` 은 "이 층은 연속 문맥 층" 표시로 **그림자 스택 처리에만** 쓰인다.
- 비유: run59 해설에서 "두 방의 비상구 지도 위치" 를 적어 둔 표였다. 그 표는 **경비실 장부** (CET 그림자 스택, run29) 를 맞출 때만 펼친다. 경비실이 없는 가게 (test.exe) 에선 펼칠 일이 없다.
- flow.md 2.4 의 이 표 줄 뜻이 `[추측]` → `[정적]` (CET 전용) 으로 풀렸다 (3절).

#### PE_emulator 에게 (run 문서 덧붙임 ①)

- run81 규칙의 "XSAVE 없이 `0x10001f`" 쉬운 길은 ntdll 쪽에서도 **괜찮다** `[정적]`.
- 쌓을 때 꼭 맞춰야 할 표지 칸은 `C+0x590` (`Rip`) · `C+0x5a8` (`Rsp`) 둘이다 (최소 규칙 2).

### 2.16 PE_emulator 에게 — syscall 답 표와 요청 26 전체 최소 규칙

#### nt 엔진이 줄 답 (run 문서 syscall 표, 대표만) `[정적]`

| syscall (번호) | 언제 | 인자 | 진짜가 기대하는 답 |
|---|---|---|---|
| **`NtContinue`** (`0x43`) | 커널이 쌓은 메모 (`0x10001f`) 를 되돌릴 때 — VEH −1 · SEH ContinueExecution (`RtlRestoreContext +0x185`) | `RCX` = 메모 (= 창구 `RSP`, 처리기가 고친 값 포함), `EDX = 0` | **안 돌아옴.** 메모대로 레지스터 복원 (`0x10` 비트면 `Dr0 ~ 7` 도) |
| **`NtRaiseException`** (`0x174`) | 창구 `+0x4e` (FALSE) · `RtlRaiseException +0x2a2` (FALSE, 디버거면 TRUE) · `RtlUnwindEx +0x743` · `RtlRaiseNoncontinuableException +0x6b` · `RaiseFailFastException +0x14c` 등 | `RCX` 카드, `RDX` 메모, `R8B` FirstChance | 거짓: **안 돌아옴** (run81 → `ZwTerminateProcess(-1, 코드)`). 참: run81 질문 3 · 4 |
| **`NtTerminateProcess`** (`0x2c`) | `RtlUserThreadStart +0x50` · UEF `+0x578` · kernelbase `TerminateProcess` | `RCX = -1`, `EDX` = 예외 코드 | **안 돌아옴** (run79) |
| `NtQueryInformationProcess` (`0x19`) | UEF 클래스 7 · `0xc`, `RtlDecodePointer` 클래스 `0x24` | `RCX = -1` | 7 → **0** (디버거 없음) · `0xc` → `DefaultHardErrorProcessing` · `0x24` → 쿠키 |
| `NtSetInformationProcess` (`0x1c`) | UEF `+0x110` (값 8) · `+0x282` (값 1) | 클래스 `0x3f`, 8B | 성공 (반환값 안 봄) |
| `NtQueryInformationJobObject` (`0x156`) | UEF `+0x2d1` | `(NULL, 2, 0x40)` | 잡에 안 속하면 오류 |
| `NtRaiseHardError` (`0x175`) | UEF `+0x4ec` (WER 가 안 정한 길) · `RaiseFailFastException +0x113` | `0xD0000144`, 4개, … | 성공 + 응답 **6 (OK)** |

- 실패 · 다른 답이면 어떻게 되나는 run 문서 표 마지막 칸.
- **syscall 이 아예 없는 길** (표에 안 나오는 가장 중요한 것) — `__except` 가 받는 길, `RtlRaiseException` 이 받아진 길. 둘 다 유저 `IRETQ` 로 끝난다 (2.8).
- **꼭 만들 것은 첫 세 줄** 이다. 은퇴용 `NtContinue` 가 이미 있으면 같은 구현을 `0x10001f` (`Dr0 ~ 7` 포함) 메모에 쓰면 된다. 나머지는 "UEF 까지 가는 프로그램" 에서만 (run 문서).

#### 요청 26 전체 최소 규칙 열둘 — 한 줄씩 (run 문서 그대로, 줄임)

전제: 64비트 exe, 유저 모드 사고, 디버거 없음, WER 없음 (또는 `ProcessDefaultHardErrorMode & 2`), 비 CET (`+0xa8 = 0`, `+0xb8 = 0`), 계측 콜백 · Pico 없음. **[nt]** = 엔진이 직접, **[ntdll]** = 진짜 ntdll 이 Unicorn 안에서.

| # | 규칙 (줄임) | 어디서 풀었나 |
|---|---|---|
| 1 | [nt] CPU 예외 → 코드 · 주소 · 덧붙임 정하기 | run81 해설 2.6 · 2.7 |
| 2 | [nt] 메모 · 배치 만들기 (`C = X − 0x4D0`, 이름표 `0x10001f`). **`C+0x590` = `Rip`, `C+0x5a8` = `Rsp` 두 칸만 읽힌다** | run81 해설 2.10 · 2.11, 이 글 2.15 |
| 3 | [nt] 레지스터 바꾸기 (`RSP = C`, `RIP = KiUserExceptionDispatcher` …). TEB `+0x8` · `+0x10` 이 새 `RSP` 아래를 포함해야 함 | run81 해설 2.13, 이 글 2.6 |
| 4 | [ntdll] 창구 — `Wow64PrepareForException` 0 → `RtlDispatchException(RSP+0x4f0, RSP)` | 2.4 |
| 5 | [ntdll] VEH — 비트 2 가 0 이면 곧장. 켜졌고 −1 이면 TRUE | 2.5 |
| 6 | [ntdll] SEH 걷기 → 갈래 셋: 필터 1 → `IRETQ` (syscall 없음) / VEH −1 · ContinueExecution → **`NtContinue`** [nt] / FALSE → **`NtRaiseException(FALSE)`** [nt] | 2.6 ~ 2.8 |
| 7 | [ntdll → nt] 꼭대기 그물 → UEF (syscall 몇 개) → 1 → 되감기 → **`NtTerminateProcess(-1, 코드)`** [nt] | 2.11 ~ 2.13 |
| 8 | [nt] 둘째 기회 → `ZwTerminateProcess(-1, 코드)`. 쓰기 실패 · `int 0x29` 도 같은 끝 | run81 해설 2.15, 이 글 2.13 |
| 9 | [ntdll] 소프트웨어 예외 — 커널 안 거침. `RaiseFailFastException` 은 곧바로 `NtRaiseException(FALSE)` | 2.9 · 2.14 |
| 10 | 엔진 syscall 최소 집합 (필수 셋 + UEF 길) + 유저 `IRETQ` 실행 | 위 표 |
| 11 | 안 흉내 내도 되는 것 — 디버거 갈래, WoW64, CET 그림자 스택, XSTATE 꼬리, `RtlGuardCheckExceptionHandler`, 예외 로그, WER 본체 | 2.4 · 2.15 · 2.17 |
| 12 | run81 규칙 7 에 보탬 — 평소 "아무도 안 받음" 은 커널이 아니라 `RtlUserThreadStart` `__except` 에서 끝 | 2.13 |

### 2.17 디버거가 붙으면 달라지는 것 `[정적]` (run 문서 "디버거 메모")

- **`RtlRaiseException` 은 `PEB.BeingDebugged` 하나로 갈린다** (`+0x17e`).
  - 유저 디버거가 붙으면 `NtRaiseException(…, TRUE)` 로 커널이 먼저 본다.
  - KDNET 만 붙으면 (run44 사진 0) 커널을 안 거친다.
  - → windbg-kd-runbook pitfalls 에 넣자는 제안: "소프트웨어 예외는 유저 디버거가 없으면 커널을 안 거친다 — `nt!KiDispatchException` 멈춤점은 안 선다. ntdll 멈춤점으로 본다". plan.md 에 하네스 제안 ⑪ (보류) 로 적혔다.
- **KUSD `+0x2d4 KdDebuggerEnabled` = 3** `[관찰]` (KDNET, `ms/logs/04_baggage-read_supp_K1.txt:215` · `ms/logs/04_baggage-read_P2.txt:4053`).
  - 그래서 `RtlIsAnyDebuggerPresent` (`PEB+2` 또는 `KUSD+0x2d4 & 3 == 3`) 가 이 VM 에선 **참** 이 된다 `[정적 추론]`.
  - 영향받을 수 있는 곳: `RtlUnhandledExceptionFilter2` (칠판이 널일 때만), UEF 1번 줄 (코드 `0xC0000409` 일 때만). VM 에서 디버거 갈래로 갈 수 있다 `[추측]`.
  - test.exe 정상 길엔 영향 없다. 다만 **디버거 없는 PE_emulator 와 VM 이 다를 수 있는 칸** 이다.
- 비유: 이 VM 의 공용 게시판엔 "관리사무소 감독관 있음" (`KdDebuggerEnabled` = 3) 이 붙어 있다. 그런데 가게 감독관 자리 (`BeingDebugged`) 는 비어 있다. 일부러 낸 사고는 **가게 감독관 자리만** 보고 갈래를 정하니 관리사무소로 안 간다. 게시판 쪽을 보는 곳은 몇 군데뿐이다.

### 2.18 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

모두 flow-writer 가 `.asm` · 도구로 다시 보고 보탠 것이다 (run 문서 그 절).

- ① `RtlRestoreContext` CET 검사 줄 = `+0x150 LEA` · `+0x157 MOV RAX,[RAX+8]` · `+0x15b BT RAX,0x3c` · `+0x160 JC +0x183` (+ `+0x162 ~ +0x172` 비트 `0xc`). 결과서는 "`+0x15b BT [..+0xa8]`" 로 줄여 썼다. 뜻은 같다 (2.8).
- ② UEF 7번 `RtlDecodePointer(BasepCurrentTopLevelFilter)` 의 call 은 **`+0x145`** (ret `+0x14c`). 결과서의 `+0x13e` 는 인자 싣는 줄이다 (2.12).
- ③ UEF `+0xac` callee (rva `0x129be0`) 는 같은 주소 이름 **4개** (`pdb_aliases.py --lookup`). 결과서 "별칭 3개" = Ghidra 이름 + 3. 글엔 하는 일 ("apiset 있나 묻기") 로 적었다.
- ④ `RtlGuardRestoreContext` (rva `0xa59f0`) 는 PDB 이름, Ghidra 는 `RtlRestoreContext` (2.4, 조심할 곳 1).
- ⑤ 결과서 `[추측]` "커널 디버거만 붙으면 `BeingDebugged` 0" → run44 사진 0 (P1a · P1b · P2) `[관찰]`. `main` 때 값은 여전히 `[추측]` (2.10).
- ⑥ 디버거 영향: KUSD `KdDebuggerEnabled` = 3 `[관찰]` → `RtlIsAnyDebuggerPresent` 참 `[정적 추론]` (2.17).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 기준 — PDB 목록 (ntdll · kernelbase) | … run81 까지 | + run82 (ntdll `KiUserExceptionDispatcher` · `RtlDispatchException` · `RtlRaiseException` depth 1, `RtlpCallVectoredHandlers` · `RtlUserThreadStart` · `__C_specific_handler` · `RtlUnwindEx` 등 depth 0 새로, 새 지도 둘 / kernelbase `UnhandledExceptionFilter` · `RaiseException` · `RaiseFailFastException` depth 1 새로) |
| 기준 — 사진 메모 | run81 줄까지 | + run82 줄: 새 실행 없음, run44 로그에서 `BeingDebugged` · `CrossProcessFlags` · `KdDebuggerEnabled` 다시 읽음 |
| 기준 — "예외 착지점은 Ghidra 가 안 따라간다" 메모 | … run81 | + (run82) `RtlUserThreadStart` · `OutputDebugStringA` scope table, `RtlpExecuteHandlerForException` 의 처리기 `0x1654c0`, delay-load 스텁, `apisetschema.dll` 표를 결과서가 직접 읽음 |
| 2.4 제목 · `InitSpecialMachineFrames` 아래 | 표 뜻 `[추측]` | + run82, **읽는 곳 하나 · CET 그림자 스택용** `[정적]` |
| 2.5 제목 · 예외 필터 줄 아래 | 칠판에 적는 줄만 (run68) | + run82, 읽는 곳 = `RtlUserThreadStart$filt$0 +0x19` · `+0x20` → kernelbase `UnhandledExceptionFilter`. 사용자 필터는 `BasepCurrentTopLevelFilter` |
| 2.10 제목 · `RtlUserThreadStart` 줄 아래 | "`__try` → 필터 → 착지 `+0x4a` → `+0x50 NtTerminateProcess`" 한 줄 (사이 길 없음) | + run82 한 줄: scope table → 필터 → UEF 기본 1 → `RtlUnwindEx` → `IRETQ` (`NtContinue` 없음) → 착지 → `+0x50 NtTerminateProcess`. **커널 둘째 기회까지 안 감** |
| 2.10 run71 절 (`OutputDebugStringA`) | "`[관찰 간접]` 커널 디버거는 `0x40010006` 을 삼키지 않았다" | + run82 한 줄: **커널까지 안 갔다** `[정적]` + 값 `[관찰]` |
| 3장 "CPU 예외 …" 절 제목 · 첫 문단 | 커널 쪽 (run81) 만, "커널을 거치는지는 Q148b" | + 유저 쪽 run82. "디버거가 없으면 **커널을 안 거친다**" |
| 3장 같은 절 트리 끝 줄 · 동적 남은 것 | `(Q148b)` · 새 샘플 번호 없음 | `(아래 유저 쪽, run82)` · **Q156** |
| 3장 **새 하위 절 "유저 쪽"** | — | 비유 + 트리 + `[정적]` 다섯 (이름표 갈림 · VEH 비트 · `RtlRaiseException` · 꼭대기 그물 · 덧붙임 ①) + 동적 남은 것 |
| 4장 run81 줄 둘 | 후보 (가) · ① 열림, 번호 없음 | (가) · ① **(해결됨 → run82)**, **Q155** · **Q156** 번호 |
| 4장 새 줄 셋 (run82) | — | 실행 확인 (Q104 · Q156) · 정적 남은 것 ① ~ ④ · 결과서와 다르게 적은 곳 · 디버거 영향 |
| 5장 | run81 까지 | run81 줄에 Q155 · Q156 + run82 줄 (표 둘) |

태그가 바뀐 곳:

- flow.md 2.4 `RtlpContinuationContextMachineFrameEntries` 뜻 ("커널 → 유저 진입 함수의 풀림 정보인가") `[추측]` → **CET 그림자 스택용 표시** `[정적]` (2.15 (다)).
- flow.md 2.10 run71 `[관찰 간접]` "커널 디버거는 삼키지 않았다" → **커널까지 안 갔다** `[정적]` + run44 값 `[관찰]` (2.10).
- run81 동적 후보 (가) "`OutputDebugStringA` 가 커널을 거친다면 그것으로" → **해결됨: 안 거친다** — 커널 쪽 동적은 Q156 으로 (run81 문서 · flow.md 4장에 표시).
- PE_emulator `[추측]` "소프트웨어 예외는 유저 안에서 돈다" → **맞다** `[정적]` (디버거가 없을 때) + 틀린 점 둘 (2.9).
- 결과서 `[추측]` "KDNET 만 붙으면 `BeingDebugged` 0" → run44 사진 0 `[관찰]` (은퇴까지. `main` 때는 여전히 `[추측]`).

### 짐 지도 · run81 문서에서 바뀐 것

- 짐 지도 (`baggage.md`): **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로.** 짐 지도 바퀴가 아니다 — 유저 예외는 은퇴 뒤 일이고, test.exe 의 `OutputDebugStringA` 사고도 짐을 새로 안 놓는다. 이미 있는 줄에 근거만 붙었다.
  - 3.8 `RtlpUnhandledExceptionFilter` 둘째 줄 — 읽는 곳 (`RtlUserThreadStart$filt$0 +0x19` · `+0x20`) 과 쓰임 (2.11).
  - 3.9 `LdrSystemDllInitBlock` — `+0xa8` (CET 비트) · `+0xb8` 를 예외 길이 읽는 줄 (2.4 · 2.8).
  - 7.2 `_PEB` `끝까지 0` — `ProcessUsingVEH` · `ProcessUsingVCH` 쓰는 줄 둘과 "초기화 동안 아무도 안 부름" (2.5). 3장으로 옮길지는 짐 지도 바퀴에서.
  - 판 목록 **서른여덟째판**, 8장 두 표에 한 줄씩.
- run81 문서 "아직 확인 필요": (가) · ① 에 **"(해결됨 → run82)"**, 새 샘플 줄에 **Q156**, 정적 후보에 **Q155** 번호. 틀린 걸 고친 게 아니라 표시만 붙였다.
- 정정: **고칠 run 없음.** run81 규칙 7 에는 보탬만 (2.13).

### 큰 그림에서

- **지도 쪽**: 진입점까지의 정상 길은 안 바뀌었다. 대신 지도 안 **네 자리의 안쪽 길** 이 채워졌다.
  - 2.4 `InitSpecialMachineFrames` 표 → 그림자 스택용 (test.exe 는 안 씀).
  - 2.5 칠판의 "마지막 담당" → 꼭대기 그물의 필터가 읽음 (2.10 의 줄과 이어짐).
  - 2.10 `RtlUserThreadStart` 의 `__try` → 필터에서 가게 문 닫기까지 가운데 길, 그리고 "커널 둘째 기회까지 안 간다".
  - 2.10 `OutputDebugStringA` 의 `RaiseException` → 커널을 안 거치는 유저 예외 길.
- **사고 뒤 이어 가는 길이 셋** 으로 정리됐다:
  - 관리실 혼자 순간 이동 (유저 `IRETQ`) — 가장 흔함, syscall 0.
  - 관리사무소 창구 `NtContinue` — 은퇴 때 쓰던 그 창구 (run29) 를 다시 씀.
  - 가게 문 닫기 `NtTerminateProcess` — 꼭대기 층에서 스스로, 또는 관리사무소가 (둘째 기회).
- **PE_emulator 쪽**: 요청 26 의 답이 한 줄로 모였다 (4절 끝 정리).
- **다음**: **Q149a** (요청 27 콘솔).

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 동적은 두 무리로 갈렸다 (run 문서 "아직 확인 필요"). 멈춤점은 모두 test.exe 조건.

- **지금 test.exe 로 되는 것** → Q104. test.exe 의 `OutputDebugStringA` (dll2 TLS 콜백부터 `Log` 마다) 가 이 유저 길을 탄다 (2.10).
- **예외를 일부러 내야 되는 것** → Q156 (새 샘플 — run81 동적 4 와 같은 샘플에 `__try` 없는 경우 · VEH 거는 경우를 더하면 닫힘).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe `OutputDebugStringA` 길이 정말 `RtlRaiseException+0x188 → RtlDispatchException+0x205 → __C_specific_handler+0x93 → RtlUnwindEx+0x606 → RtlRestoreContext+0x181` 차례인지, `NtRaiseException` · `NtContinue` · `nt!KiDispatchException` 이 **한 번도 안 서는지** | 정적으론 줄까지. 실제 차례는 실행해야 | **Q104** 동적 9 |
| `RtlRestoreContext+0x174` 뒤 `eax` 가 `0x10000f` 인지, `+0x300 IRETQ` 의 `rsp` 틀 | 런타임 값 | **Q104** 동적 3 (앞 반) |
| `RtlRaiseException+0xa0` 의 `ecx` — `0x10000b` / `0x10004b` (KUSD `+0x3d8` `0xe7` 이면 뒤) | 〃 | **Q104** 동적 4 |
| `RtlRaiseException+0x188` 에서 카드 `+0x10` = `KERNELBASE!RaiseException+0x8a` 인지 | 〃 | **Q104** 동적 8 |
| `LdrSystemDllInitBlock +0xa8` · `+0xb8` 가 test.exe **밖** 프로세스에서도 0 인지 | test.exe 값만 run44 에 있음 | **Q104** 동적 2 (`dq ntdll!LdrSystemDllInitBlock+a0 l4`) |
| `main` 때 `BeingDebugged` 가 KDNET 만일 때 정말 0 인지 | 사진은 은퇴 (P2) 까지 `[추측]` | **Q104** 디버거 영향 줄 (`RtlRaiseException+0x17e` 에서 `db @rax+2 l1`) |
| `UnhandledExceptionFilter` 가 정말 1 로 끝나는 길과 syscall 차례, WER 갈림 | test.exe 엔 처리 안 된 사고가 없음 | **Q156** 동적 1 (`+0xb1` · `+0x29f` · `+0x2d8` · `+0x34c` · `+0x4f3` · `+0x59d`) |
| CPU 예외에서 `RtlRestoreContext+0x185 NtContinue` 가 정말 불리는지 (VEH −1 · 필터 −1) | 〃 | **Q156** 동적 3 (뒤 반) |
| VEH 를 건 프로그램에서 `RtlDispatchException+0x205` `al` · `+0x5f5` `ecx`, PEB `+0x50` 비트가 켜지는 때 | test.exe 는 VEH 를 안 건다 | **Q156** 동적 5 |
| 스택 넘침에서 `RtlpGetStackLimits` 가 실패하나 (`+0x263`), 커널이 스택을 늘릴 때 TEB `+0x10` 도 낮추나 | 〃 (run81 동적 6 과 이음) | **Q156** 동적 6 |
| 첫 UEF 에서 delay-load 가 호스트 `kernel32.dll` 로 풀리나, 새 DLL 을 올리나 | 이름은 `[정적: apisetschema.dll 표 + kernel32 _index.tsv]`, 실제 풀림은 실행 | **Q156** 동적 7 |
| kernel32 WER 호스트 `CheckForReadOnlyResourceFilter` · `BasepReportFault` 안 — "WER 없음" 흉내 때 어떤 답이면 4 를 받나 | 이번엔 이름만 | 정적 **Q157 ①** |
| `DefaultHardErrorProcessing` 비트 `0x2` 가 부모 → 자식으로 오는 길 (UEF 11번 WER 갈림을 정함) | 결과서 "부모 값 복사 / 부모 없으면 5" 까지 | 정적 **Q157 ②** (짐 지도 3.11 줄과 맞춰 볼 것) |
| `NtSetInformationProcess` 클래스 `0x3f` (값 8 · 1) 가 무엇을 바꾸나 | 커널 쪽 안 봄, 클래스 이름 `[추측]` | 정적 **Q157 ③** |
| `RtlpxVirtualUnwind +0x6ba` 리프 처리 안쪽, `RtlpLookupDynamicFunctionEntry` (동적 함수표), KUSD `+0x708` 뜻 (CET_U) | 줄기 밖 | 정적 **Q157 ④** |
| 7.2 `_PEB` `끝까지 0` 의 `ProcessUsingVEH` · `ProcessUsingVCH` 를 "쓰는 줄은 있으나 초기화 동안 안 부름" 으로 옮길지 | 짐 지도 바퀴 몫 | 짐 지도 바퀴 후보 (run 문서) |
| run81 이 물은 "안 쓴 칸 (`P1Home ~ P6Home` · `VectorRegister`) 을 ntdll 이 읽나" | 결과서가 따로 답하지 않았다 — 읽는 곳 목록 (`RtlpSanitizeContext` 등) 엔 없음 (flow.md 4장 run81 ① 메모) | 대기열 줄 없음 |

- 추천 멈춤점 (결과서): `ntdll!KiUserExceptionDispatcher` (입구 `RSP` · `RSP+0x4f0`), `ntdll!RtlDispatchException+0x205` · `+0x5f5`, `ntdll!__C_specific_handler+0x93`, `ntdll!RtlUnwindEx+0x606`, `ntdll!RtlRestoreContext+0x174` · `+0x185` · `+0x300`, `ntdll!RtlRaiseException+0x188` · `+0x2a2`, `kernelbase!RaiseException+0x83`, `kernelbase!UnhandledExceptionFilter+0x29f` · `+0x34c` · `+0x4f3`.
- 같은 요청 26 의 run81 몫으로 남은 것은 그대로다: 동적 → Q104 · Q156, 작은 정적 → **Q155** (run81 해설 4절).

### 요청 26 두 바퀴 돌아보기

요청 26 은 "PE_emulator 가 CPU 예외에서 그냥 멈춘다" 는 데서 시작했다. 두 바퀴로 나눠 봤다. run81 (커널 쪽) 은 CPU 가 걸린 뒤 커널이 무엇을 하는지 — 사고 보고서 묶음을 작업대에 어떤 모양으로 쌓고, 출발 메모 넷만 고쳐 사고 처리 창구로 보내는지, 아무도 안 받으면 둘째 기회로 가게 문을 닫는지 — 를 정했다. run82 (이번, 유저 쪽) 는 그 창구 안에서 ntdll 이 하는 일 — 비상 연락망 (VEH) 을 돌리고, 층마다 대피도 (SEH) 를 보고, 체크 칸 (이름표) 하나로 끝맺음을 가르는 것 — 을 정했다. 가장 흔한 길 (`__except` 가 받음) 은 관리실 혼자 순간 이동으로 끝나 syscall 이 없고, 아무도 안 받는 길은 꼭대기 층의 마지막 그물에서 가게 문을 닫는다. 그래서 run 문서 맨 위 한 줄 ("PE_emulator 쪽에서 쓸 것" 첫 줄) 이 요청 26 전체의 결론이다: **nt 엔진이 새로 흉내 낼 것은 커널 쪽 한 덩어리 쌓기 (run81 규칙 1 ~ 6) 와 syscall 셋 (`NtContinue` · `NtRaiseException` · `NtTerminateProcess`) 이 전부고, VEH · SEH · 되감기 · 필터는 진짜 ntdll · kernelbase 가 Unicorn 안에서 한다** `[정적]`. 남은 것은 실행해서 맞춰 보는 일 — test.exe 로 되는 유저 길은 Q104, 예외를 일부러 내야 하는 커널 길과 처리 안 된 예외 길은 Q156 — 과 작은 정적 (Q155 · Q157) 이다.

---

## 5. 더 읽기

- run 문서: [`run82-exception-user.md`](run82-exception-user.md) — "PE_emulator 쪽에서 쓸 것" 질문별 표, **문맥 이름표 표 · `RtlRestoreContext` 갈림**, **nt 엔진이 줄 답 — syscall 표 (전부)**, **요청 26 전체 최소 규칙 열둘**, 호출 지점 표, 질문 1 ~ 5 절 (`UnhandledExceptionFilter` 차례 표 전부), 덧붙임 1 ~ 3, 호출 관계 그림, 결과서와 다르게 적은 곳 · 보탠 것, 정정 (없음 · run81 에 보탬 · run71 확인), 동적 Q104 · Q156 목록, 디버거 메모 · 도구 메모
- 결과서: [`_workspace/sast/79_exception-user_result.md`](../../_workspace/sast/79_exception-user_result.md) — "먼저 알아 둘 것" (이름표 표 · `RtlRestoreContext` 갈림 · ntdll 이 읽는 입력 칸 목록), 질문별 답, "지금 우리 (PE_emulator)" 줄들, 스크립트 문제 (참고 4건)
- 요청서: [`_workspace/sast/79_exception-user.md`](../../_workspace/sast/79_exception-user.md)
- flow.md: [3장 "CPU 예외를 유저 예외로 넘기기" 절의 새 하위 절 "유저 쪽"](flow.md) · 2.4 `InitSpecialMachineFrames` 아래 run82 줄 · 2.5 예외 필터 아래 run82 줄 · 2.10 `RtlUserThreadStart` · run71 `OutputDebugStringA` 아래 run82 줄 · 4장 run82 줄 셋
- 짐 지도: [`baggage.md`](baggage.md) 3.8 `RtlpUnhandledExceptionFilter` · 3.9 `LdrSystemDllInitBlock` · 7.2 `_PEB` `끝까지 0` 메모
- 앞 이야기:
  - [run81 해설](run81-exception-kernel-해설.md) — 짝 바퀴 (커널 쪽). 사고 보고서 묶음 · 사고 처리 창구 · 둘째 기회 · 비상 정지 버튼
  - [run68 해설](run68-dll-init-rest-해설.md) 2.5 — 칠판의 "마지막 담당" 을 적는 쪽 (이번 2.11 의 짝)
  - [run59 해설](run59-init-leftovers-해설.md) 2.7 · 2.9 — 칠판 암호, `InitSpecialMachineFrames` 표 (이번 2.15 (다))
  - [run71 해설](run71-named-obj-dir-해설.md) 2.1 — `OutputDebugStringA` 가 하는 일 (이번 2.10)
  - [run29 해설](../02_sprint/run29-continue-start-해설.md) 2.3 · 2.9 — `NtContinue` 창구, 경비실 장부 (CET 그림자 스택)
  - [run79 해설](run79-thread-exit-해설.md) 2.13 · 2.14 — 가게 문 닫기
  - [run60 해설](run60-func-table-mrdata-해설.md) — 동네 지번 색인, 팻말 (`LdrInitState`)
  - [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.8 · [run27 해설](../02_sprint/run27-init-graph-해설.md) 2.9 · [run28 해설](../02_sprint/run28-tls-init-call-해설.md) 2.10 — 예외 보호 구역 · 착지 매트 · 비상 대피도 · 필터
  - [run15 해설](../02_sprint/run15-user-thread-startup-해설.md) 2.3 · 2.6 — 풀어 보기 · `UNWIND_INFO` · `IRETQ` 순간 이동

# run62 tp-etw-init — 해설

> 이 글은 [run62 문서](run62-tp-etw-init.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q118** — PE_emulator 요청 10: "`LdrpInitializeProcess +0xb83 ~ +0xc2d` (스레드 풀·ETW 등록·힙 로깅) 함수 본문 — 쓰는 칸, syscall 이름·횟수·조건" (`_workspace/plan.md`, 이번에 **Q118 끝**)
  - 사용자 결정 (2026-11-03): Q104 VM 단계 앞, 급함 보통. PE_emulator 54 바퀴가 도는 동안 끝나면 55 바퀴가 쓴다.
  - 다음 항목: **Q104 VM 단계** → Q78d4 → Q78e → … (plan "다음 항목")
  - 남은 질문이 간 곳: 동적 D1 ~ D13 → **Q104 VM 단계** (D10 은 Q47 과도 이어짐), 남은 작은 정적 → 새 **Q119**.
- 근거:
  - run 문서 [`run62-tp-etw-init.md`](run62-tp-etw-init.md) (가장 중요)
  - 요청서 [`_workspace/sast/57_tp-etw-init.md`](../../_workspace/sast/57_tp-etw-init.md)
  - 결과서 [`_workspace/sast/57_tp-etw-init_result.md`](../../_workspace/sast/57_tp-etw-init_result.md) (판정: **분석성공**, PDB 일치)
  - flow.md 변경: 기준 · 그림 2 `j1` 상자 글 · 2.4 트리 두 줄 · 2.4 묶음 8 표 줄 · 2.4 run59 줄 · 2.4 병렬 로딩 줄 · 2.4 새 절 · 2.4 run51 절 · 3장 일꾼 · 4장 · 5장
  - 고쳐진 옛 문서: [`run51-eprocess-later.md`](run51-eprocess-later.md) · [`run59-init-leftovers.md`](run59-init-leftovers.md) (둘 다 맨 위 정정 한 줄)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - `[관찰]` 로 적힌 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_<K1 ~ P2>.txt`) 를 결과서가 **다시 읽은 것** 이다. 새 실행이 아니다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- `LOCK CMPXCHG` (발판에 먼저 오른 한 명만 0 → 1 로 고친다) → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.6
- **한 번만 하기 (run-once)** = 뚜껑의 **팻말** (0 아직 / 1 하는 중 / 2 다 함) → [run61 해설](run61-random-ex-init-해설.md) 2.2
- `TpInitializePackage` = 일꾼 **대기실** → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) (묶음 8)
- 스레드 풀 일꾼, 일감 개체 = 대기실 벽의 **호출 버튼** → [run24 해설](../02_sprint/run24-work-queue-해설.md)
- `+0x1a42 LdrpEnableParallelLoading` = "일꾼이 생길 수 있는 가장 이른 자리" (run19 의 옛 결론) → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.9
- 게으른 만들기, 커널 `EPROCESS` 의 `EtwDataSource` · `WnfContext` → [run51 해설](run51-eprocess-later-해설.md) 2.8
- `KUSER_SHARED_DATA` (KUSD) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 프로세스 힙 = **창고** → [run46 해설](run46-process-heap-해설.md)
- 꼬리 JMP (콜스택에 안 보이는 넘겨주기) → [run46 해설](run46-process-heap-해설.md)
- 사진 P1 · P1a · P1b · P2 → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

이번에 처음 나오는 말 (**기본 스레드 풀**, **일꾼 공장**, **IO 완료 포트**, **TP 개체**, **대기 패킷**, **WNF**, **ETW 공급자 · 등록 · RegHandle**, **TraceLogging**, **기능 상태 검사**, **설정 구역**, **스냅 버퍼**) 은 본문에서 처음 나오는 자리에서 한 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소 **신청 창구**, 프로세스 = **가게**, ntdll = 가게 안 **관리실**, ntdll 전역 = 관리실 **칠판**.
- run18 해설은 `TpInitializePackage` 를 일꾼 **대기실** 이라고 했다. run 문서는 기본 스레드 풀을 **일꾼 사무실** 이라고 부른다. 이 글은 둘을 이렇게 잇는다:
  - `TpInitializePackage` = 대기실 **명부를 펴 두는 것** (아직 일꾼도, 사무실도 없다).
  - 기본 스레드 풀 = 가게 전체가 같이 쓰는 **일꾼 사무실**. 이번에 이 사무실을 **처음 차리는 자리** 가 밝혀졌다.
  - 로더 전용 풀 = 짐 들이기 팀만 쓰는 **두 번째 사무실** (run19 · run24 의 그 일꾼).
- ETW = **방송국** (run 문서 비유). 등록 = 방송국에 **채널** 하나 올리기.
- "Once" = 방송국이 **알림 벨 + 벨 지키는 사람** 을 **딱 한 번** 놓아 주는 것 (run 문서 비유). 벨 지키는 사람은 이미 차린 일꾼 사무실 사람을 빌려 쓴다.
- 팻말 (run-once) 은 run61 해설의 주사위 상자 뚜껑 팻말과 **같은 모양** 이다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| `+0xb83 ~ +0xc2d` 에서 syscall 은 몇 번? | **30번** = `TpInitializePackage` 3 + `LdrpEnableUMGLTracingStateSync` 18 + ETW 등록 9 `[정적]` (2.5) |
| 그 30 에 빠졌을 수 있는 것? | `TpAllocPoolInternal +0x443` 갈래. 타면 **31** — 타는지는 `[공백]` (2.5 끝) |
| 이 프로세스의 첫 스레드 풀은 어디서? | `+0xbe2` `LdrpEnableUMGLTracingStateSync` 안 (기본 풀). run19 의 `+0x1a42` 는 **로더 전용 풀** 로만 맞다 `[정적]` (2.3 · 2.4) |
| `EtwEventRegister` 는 몇 번? | **4번** (A · B · C · D), 4번 모두 syscall 까지 간다 `[정적]` (2.6) |
| ETW "Once" 는 어디서? | **첫 등록 A (`+0xbee`) 한 번뿐**. run51 이 적은 `+0xc06`·`+0xc23` 아래가 아니다 — **정정** `[정적]` (2.7 · 2.8) |
| 앞 구간 `+0xaad ~ +0xb05` 는? | syscall `1 + 2k` (k = 0 ~ 4, 동적). 이 프로세스 **첫 기능 상태 검사** 라 설정을 받아 온다. run59 의 "`RtlpFcProcessManager` 전부 0" 은 끝 값이 아니다 — **정정** (2.9) |
| PE_emulator 54 의 고정값 셋 | (a) "켜짐" 고정 **맞다** · (b) 스냅 버퍼 건너뛰기는 **실제와 다르다** · (c) 예외 기록은 **안 탄다** `[정적]` + `[관찰]` (2.9) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

`LdrpInitializeProcess` 의 묶음 7 끝 ~ 묶음 8 앞이다. 창고 (프로세스 힙) 를 지은 **바로 뒤**, kernel32 를 부르기 **훨씬 전** 이다.

```
cmd.exe 의 부탁 (CreateProcess)
 │  커널: 프로세스·주소 공간·PEB·첫 스레드 만들기
 │  첫 스레드 출발 → ntdll!LdrInitializeThunk                      (사진 P1)
 │    └─ LdrpInitializeProcess
 │         ├─ +0x9f2  RtlInitializeHeapManager · +0xa50 창고 짓기   (run46 · run61)
 │         ├─ +0xab2 ~ +0xb05  기능 상태 검사 둘 · 스냅 버퍼        ← 이번 바퀴 (앞)
 │         ├─ +0xb88  TpInitializePackage   일꾼 대기실 명부         ← 이번 바퀴
 │         ├─ +0xbe7  LdrpEnableUMGLTracingStateSync  일꾼 사무실 처음 ← 이번 바퀴
 │         ├─ +0xbf3 ~ +0xc2d  ETW 등록 넷  방송국 채널                ← 이번 바퀴
 │         ├─ +0xc32  RtlpInitEnvironmentBlock (다음 구간)
 │         ├─ …       kernel32 준비                     (사진 P1a, +0x199d)
 │         ├─ +0x1a47 LdrpEnableParallelLoading  로더 전용 풀 (run19)
 │         └─ … DLL 올리기 · 켜기 → NtContinue 은퇴      (사진 P2)
 └─ RtlUserThreadStart → (CRT) → main    (범위 밖)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준).
- 이번 구간은 사진 **P1 과 P1a 사이** 다. 그래서 사진은 "끝났을 때 값" 만 보여 주고, 순서는 정적으로 정한다.

### 무엇이 궁금했나

PE_emulator 는 55 바퀴에서 이 구간을 C++ 로 짓는다. 그런데 이 구간 함수 대부분이 `.asm` 본문이 없어서 "공사 중" 빈 함수로 남을 처지였다 (요청서).

PE_emulator 에는 "**호출 약속**" 덫이 있다. 함수가 syscall 을 몇 번, 무엇을 부르는지 미리 약속하고 실행에서 맞춰 본다. 그래서 물은 것은:

- 이 구간 함수마다 (1) 쓰는 칸 (2) syscall 이름·횟수·조건 (3) 하위 호출 (4) test.exe 가 타는 갈래.
- 특히 `EtwEventRegister` 가 몇 번 불리고, 그중 run51 이 적은 "Once" 는 **어느 호출** 에서 타는가.
- 54 바퀴가 "공사 중" 으로 남긴 셋 (`+0xaad ~ +0xb05`) 의 고정값이 맞는가.

### 바퀴 전에 알던 것

- `[정적]` (run18) `+0xb83` `TpInitializePackage` = 일꾼 대기실, ETW 등록 3곳 (이름만).
- `[정적]` (run19) "스레드 풀을 만드는 첫 자리 = `+0x1a42 LdrpEnableParallelLoading`" (callee 이름 `TpAllocPoolInternal` 등).
- `[정적]` (run51) `LdrpEnableUMGLTracingStateSync → … → RtlpWnfRegisterTpNotification +0x7c NtSetWnfProcessNotificationEvent` (커널 `WnfContext`), 그리고 `+0xc06`·`+0xc23` 의 `EtwEventRegister → … → EtwpRegisterTpNotificationOnce +0x91 NtTraceControl(0x1b)` (커널 `EtwDataSource`).
- `[정적]` (run59) `RtlpFcProcessManager` 는 `+0x21c ~ +0x286` 에서 "전부 0".
- `[관찰]` (run44) `RtlpSrwLockAllowImplicitUpgrade` 끝까지 0, 일꾼 2개는 P1a ~ P1b 사이에 생긴다 (`ActiveThreads` 1 → 3).

---

## 2. 찾은 것

### 2.1 한눈에 — 이 구간의 큰 줄기

run 문서 "호출 관계" 를 대표만 줄였다. `+0x..` 는 ret 자리, `⇒` 는 꼬리 JMP.

```
LdrpInitializeProcess
 ├─ +0xab2  Feature_…DisallowSharedLockImplicitUpgrade…   첫 기능 상태 검사 → 설정 받기  1 + 2k
 ├─ +0xac5  Feature_…LoaderSnapsBuffer…  (참) → +0xace LdrpInitializeSnapsBuffer   (syscall 0)
 │  (+0xb05 RtlInitializeExceptionLog — 안 탐)
 ├─ +0xb88  TpInitializePackage                               syscall 3
 │  (+0xba7 TpSetDefaultPoolCpuSets · +0xbbe MaxThreadsOverride — 안 탐)
 ├─ +0xbe7  LdrpEnableUMGLTracingStateSync                    syscall 18
 │        └─ +0x7f RtlpSubscribeWnfStateChangeNotificationInternal
 │                 ├─ +0x18e RtlpInitializeWnf
 │                 │        ├─ +0x59 TpAllocTimer → … → TpAllocPoolInternal   ← 기본 풀 처음 (12) + 첫 개체 (1)
 │                 │        └─ +0x9d RtlpWnfRegisterTpNotification            (4)
 │                 └─ +0xe3 RtlpAddWnfUserSubToNameSub → NtSubscribeWnfStateChange (1)
 ├─ +0xbf3  TraceLoggingRegisterEx… (A)   → EtwEventRegister → … EtwpRegisterProvider
 │                                          Once 4 + NtTraceControl(0xf) + NtTraceControl(0x1e)   (6)
 ├─ +0xc0b  EtwEventRegister (B)           NtTraceControl(0xf)   (1)
 ├─ +0xc28  EtwEventRegister (C)           NtTraceControl(0xf)   (1)
 └─ +0xc2d  RtlInitializeHeapLogging → EtwEventRegister (D)   NtTraceControl(0xf)   (1)
```

- 괄호 안 숫자 = 그 줄기의 syscall 수 `[정적]` (결과서 syscall 표).
- 아래에서 하나씩 천천히 본다. 먼저 스레드 풀 (2.2 ~ 2.4), 그다음 30 세기 (2.5), ETW (2.6 ~ 2.8), 앞 구간 (2.9).

### 2.2 새 말 — 스레드 풀 한 벌의 부품

스레드 풀 자체는 [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.9 에서 "일꾼을 모아 두고 일감을 나눠 맡기는 장치" `[추측: 일반적으로 알려진 설명]` 로 풀었다. 이번엔 그 **부품** 이 syscall 이름으로 나왔다. 하나씩.

- **기본 스레드 풀** (전역 풀, `TppPoolpGlobalPool`): 따로 풀을 만들지 않은 TP 개체가 **다 같이 쓰는** 풀. ntdll 전역 칸 `TppPoolpGlobalPool` 이 가리킨다 `[정적]`.
  - 비유: 가게 전체가 같이 쓰는 **일꾼 사무실**.
- **TP 개체**: 풀에 매다는 "일 한 건" 의 손잡이. 이번 구간엔 **타이머** (`TpAllocTimer`) 와 **대기** (`TpAllocWait`) 가 나온다 `[정적]`.
  - 비유: run24 해설의 대기실 벽 **호출 버튼** 과 같은 종류. 버튼마다 "이게 울리면 이 일을 해 줘" 가 붙어 있다.
- **일꾼 공장** (worker factory, `NtCreateWorkerFactory`): run 문서가 쓴 이름. 시작 루틴이 `TppWorkerThread` (`0x18001a2e0`) 다 `[정적]`. 일반적으로는 커널이 일꾼 스레드를 필요할 때 만들어 주는 개체로 알려져 있다 `[추측: 일반적으로 알려진 설명]`.
  - 비유: 사무실에 딸린 **일꾼 파견소**. 일이 쌓이면 파견소가 일꾼을 보낸다.
- **IO 완료 포트** (`NtCreateIoCompletion`): 일반적으로 "끝난 일 소식이 쌓이는 줄" 이고, 일꾼은 여기서 다음 일을 받아 간다고 알려져 있다 `[추측: 일반적으로 알려진 설명]`.
  - 비유: 사무실 앞 **우편함**.
- **대기 패킷** (`NtCreateWaitCompletionPacket` 로 만들고 `NtAssociateWaitCompletionPacket` 로 잇는다): 이름으로 보아, 이벤트나 타이머가 울리면 그 소식을 우편함에 넣어 주는 장치다 `[추측: 이름]`.
  - 비유: 벨에 매단 **줄**. 벨이 울리면 줄이 당겨져 우편함에 쪽지가 떨어진다.

### 2.3 기본 풀은 `+0xbe2` 에서 처음 생긴다 — 단계별로

사용자가 헷갈리기 쉬운 첫째 곳이다. "풀을 만들자" 고 부른 게 아닌데 풀이 생긴다. 사슬을 한 칸씩 따라간다 `[정적]`.

1. **`+0xbe2` (ret `+0xbe7`) `LdrpEnableUMGLTracingStateSync`** — 부르는 조건은 `UseWOW64 == 0` 이고 `LdrpIsSecureProcess == 0` (`+0xbd0 ~ +0xbe0`, R15 = 0). test.exe 는 **탄다**.
   - 하는 일: **WNF** 이름 `WNF_ETW_UMGL_TRACING_CHANGE` 를 **구독** 한다.
   - **WNF** (새 말): 이름 붙은 "상태 알림" 을 구독해 두면, 그 상태가 바뀔 때 알림을 받는 장치. 보통 Windows Notification Facility 의 줄임으로 알려져 있다 `[추측: 일반적으로 알려진 설명]`. 비유: 관리사무소 **알림 신청서**.
2. **`+0x7a` (ret `+0x7f`) `RtlpSubscribeWnfStateChangeNotificationInternal`** — 구독 함수.
   - 그 안 `+0x57` (ret `+0x5c`) `RtlRunOnceBeginInitialize(0x1ce1f8)` — WNF 준비용 팻말. 이 프로세스에서 **처음** 이라 팻말이 0 이다.
   - 그래서 `+0x189` (ret `+0x18e`) **`RtlpInitializeWnf`** 를 부른다 (WNF 첫 한 번).
3. **`RtlpInitializeWnf +0x54` (ret `+0x59`) `TpAllocTimer`** — 여기서 **타이머 TP 개체** 를 하나 만든다.
4. TP 개체는 **풀이 있어야** 붙일 수 있다. 사슬을 더 내려가면:
   - `TpAllocTimer +0xbd` → `TppWorkInitialize +0x2c` → `TppCleanupGroupMemberInitialize +0x15a` → **`TppPoolpReferenceGlobalPool`**
5. **`TppPoolpReferenceGlobalPool`** — "기본 풀 주세요". 풀이 **없을 때만** `+0xc2` (ret `+0xc7`) **`TpAllocPoolInternal`** 로 새로 만든다 (`+0x4d CMP [RBX],0`).
   - 이 순간이 이 프로세스의 **첫 스레드 풀** 이다.
6. 풀을 만들면서 부르는 syscall (`TpAllocPoolInternal` 안 + 돌아와서) = **12번** (2.5 에서 셈).
7. 풀이 생긴 뒤 `TpAllocTimer +0x16d` (ret `+0x172`) `NtSetInformationWorkerFactory(클래스 3, 값 1)` — **풀의 첫 TP 개체** 라서 (옛 개수 0) 한 번 부른다.

- 비유: 가게 관리실이 관리사무소에 **알림 신청서** (WNF) 를 처음 낸다. 알림이 오면 받아서 처리할 사람이 있어야 하니, 그 자리에서 **일꾼 사무실** (기본 풀) 을 처음 차린다. 사무실엔 우편함 · 파견소 · 알람 시계 둘이 딸려 온다.
- 아직 확인 아님: 실제로 `TpAllocPoolInternal` 입구가 **딱 한 번**, 이 자리에서만 걸리는지는 동적 (D9 → Q104).

#### 한 줄 읽기 — `TppPoolpReferenceGlobalPool +0xc2` (call) / `+0xc7` (ret)

- **누가**: `TppPoolpReferenceGlobalPool` (기본 풀을 빌려 주는 함수).
- **어디서**: call `+0xc2`, 콜스택에 보일 자리는 ret `+0xc7` `[정적]`.
- **누구를**: `TpAllocPoolInternal` — 풀을 실제로 짓는 함수.
- **무엇을 위해**: 기본 풀이 **아직 없을 때만** 짓는다. 이미 있으면 이 줄을 건너뛰고 있는 풀을 돌려준다.
  - 그래서 뒤에 오는 TP 개체들 (WNF 의 대기 개체, ETW Once 의 대기 개체) 은 **풀을 또 짓지 않는다** `[정적]`. 이게 2.7 의 "Once 4번" 을 정한다.

#### 사무실 크기 — 이 VM 에서

- 매개변수 블록 `+0x43c` (`DefaultThreadpoolThreadMaximum`) 가 0 이라 `+0xbbe` 덮어쓰기는 안 탄다 `[관찰]` (사진 8곳).
- 그래서 기본값: 최대 = `ActiveProcessorCount*8` (최소 `0x300`) → 이 VM **`0x300`**, 소프트 한도 `*4` (최소 `0x180`) → **`0x180`** `[정적]` (이 VM `ActiveProcessorCount` 2 `[관찰]`).
  - 이 둘을 `TppPoolpReferenceGlobalPool +0x140`·`+0x181` 의 `NtSetInformationWorkerFactory` (클래스 5 · 14) 로 커널에 알린다.
- 일꾼 공장을 만들 때 넘기는 최대는 `0x200` 이다 `[정적]` (flow.md 2.4 새 절). 위 `0x300` 과는 다른 자리의 숫자다.

### 2.4 기본 풀 (`+0xbe2`) vs 로더 전용 풀 (`+0x1a42`) — 무엇이 다른가

이게 사용자가 헷갈리기 쉬운 둘째 곳이다. **풀이 둘** 이다.

| | 기본 (전역) 풀 | 로더 전용 풀 |
|---|---|---|
| 만드는 자리 | `+0xbe2` (ret `+0xbe7`) `LdrpEnableUMGLTracingStateSync` 안, 여러 단 아래 `TpAllocPoolInternal` | `+0x1a42` (ret `+0x1a47`) `LdrpEnableParallelLoading` |
| 가리키는 칠판 칸 | `TppPoolpGlobalPool` | `LdrpThreadPool` |
| 누가 쓰나 | 따로 풀을 안 정한 TP 개체 전부 — 이번엔 WNF 타이머·대기, ETW Once 대기 | 로더의 병렬 로딩 일감 (`LdrpMapAndSnapWork`) |
| 만드는 함수 | `TpAllocPoolInternal` | `TpAllocPoolInternal` (run19 callee 이름) |
| 사진 (run44) | `TppPoolpGlobalPool` `≤P1a` 에 값 `[관찰]` | `LdrpThreadPool` P1a ~ P1b 에 0 → 값 `[관찰]` |
| 근거 | `[정적]` run62 | `[정적]` run19 · run24 |

- 시간 순서: 기본 풀이 **먼저** (`+0xbe2`), 로더 전용 풀이 **나중** (`+0x1a42`).
- 비유: 가게를 열 때 **공용 일꾼 사무실** 을 먼저 차린다 (알림 처리용). 한참 뒤 짐 (DLL) 들이기 직전에 **짐 들이기 팀 전용 사무실** 을 하나 더 차린다. 같은 건축 업체 (`TpAllocPoolInternal`) 가 지었지만 다른 사무실이다.

#### run19 의 결론은 어떻게 되나 (정정)

- run19: "스레드 풀을 만드는 첫 자리 = `+0x1a42`" `[정적]`.
- run62: 그건 **로더 전용 풀의 첫 자리** 로만 맞다. 이 프로세스의 **첫 스레드 풀** (기본 풀 + 일꾼 공장) 은 그보다 앞 `+0xbe2` 다 `[정적]`.
- 왜 run19 때는 못 봤을까: run19 는 `LdrpInitializeProcess` 가 **직접** 부르는 함수의 callee 이름으로 골랐다. `+0xbe2` 아래 풀 만들기는 `TpAllocTimer → TppWorkInitialize → TppCleanupGroupMemberInitialize → TppPoolpReferenceGlobalPool → TpAllocPoolInternal` 로 여러 단 밑에 묻혀 있었다. 그래서 이름 훑기에 안 걸렸을 것으로 보인다 `[추측]`.
- flow.md 는 이 스프린트 문서만 고쳤다. 02_sprint 의 run19 문서는 안 고친다 (run 문서 "정정").

#### 그럼 run5 의 일꾼 2개는 어느 사무실 사람인가 — 모른다

1. 근거 `[관찰]` (run44): `ActiveThreads` 는 P1a 에 아직 **1**, P1b 에 **3**. 일꾼 공장은 `+0xbe2` 에서 이미 생겼는데도 P1a (`+0x199d`) 까지 살아 있는 일꾼은 없다.
2. 그래서: 기본 풀이 생긴 순간 바로 일꾼이 붙지는 않았다 (적어도 P1a 까지 살아 있진 않다). 만들어졌다 끝났을 가능성은 사진으로 못 지운다 `[추측]`.
3. 그리고 두 풀 다 `TpAllocPoolInternal` 로 만들어서 일꾼 시작 루틴이 같은 `TppWorkerThread` 일 것이라, run4 의 시작 주소로는 **어느 풀 일꾼인지 못 가른다** `[추측]`.
4. 아직 확인 아님: `[공백]` → 동적 D10 (Q104), Q47.

### 2.5 syscall 30 은 어떻게 나오나 — 3 + 18 + 9

이게 셋째로 헷갈리기 쉬운 곳이다. 한 덩어리씩 센다 (결과서 syscall 표 번호 그대로). 전제 셋 (결과서):

- ETW 세션이 이 공급자들을 켜 두지 않았다.
- 버퍼 모자람 (`0xc0000023`) 되풀이가 없다.
- 힙이 커지느라 부르는 `NtAllocateVirtualMemory` 는 안 센다.

#### 덩어리 ① — `TpInitializePackage` = **3** (대기실 명부 펴기)

| 표 # | 자리 (call / ret) | syscall | 횟수 |
|---|---|---|---|
| 1 | `TpInitializePackage +0x61 / +0x66` | `NtQuerySystemInformation` (클래스 `0x37`) | 1 |
| 2 | `TpInitializePackage +0x80 / +0x85` → `TppQueryMaximumGroupCount +0x44 / +0x49` | `NtQuerySystemInformationEx` (클래스 `0x6b`, 입력 4) | **2** |

- 왜 2번인가: 1번째는 **길이만** 받는다 (`0xc0000004` = 버퍼가 작다). 그 길이로 버퍼를 잡고 2번째가 본 호출이다 (`+0x8b JMP → +0x1c` 로 되돌아감) `[정적]`.
  - 비유: 창구에 "서류 몇 장이에요?" 먼저 묻고, 그만큼 봉투를 들고 다시 가서 받는다.
- 1 + 2 = **3**.
- 덤: 이 함수는 PEB `+0x388` `TppWorkerpListLock` = 0, `+0x390` `TppWorkerpList` = 빈 목록을 쓴다 `[정적]`. 사진도 P1 `[0 - 0]` → P1a 자기 주소 둘 `[관찰]`. 빈 명부를 펴 둔 것이다.

#### 덩어리 ② — `LdrpEnableUMGLTracingStateSync` = **18** (= 12 + 1 + 4 + 1)

2.3 의 사슬을 따라 나오는 순서대로.

**(가) 풀 만들기 = 12**

| 표 # | 자리 | syscall | 횟수 |
|---|---|---|---|
| 3 | `TpAllocPoolInternal +0x240 / +0x245` → `TppPoolUpdateNodeRelation +0x4b / +0x50` | `NtQuerySystemInformationEx` (`0x6b`, 입력 6) | 2 |
| 4 | `TpAllocPoolInternal +0x292 / +0x297` | `NtCreateIoCompletion` (우편함) | 1 |
| 5 | `TpAllocPoolInternal +0x303 / +0x308` | `NtCreateWorkerFactory` (파견소) | 1 |
| 6 | `TpAllocPoolInternal +0x38e / +0x393` → `TppInitializeTimerQueue` → `TppInitializeTimerSubQueue` (둘) | `NtCreateTimer2` · `NtCreateWaitCompletionPacket` · `NtAssociateWaitCompletionPacket` | 각 2 = **6** |
| 7 | `TppPoolpReferenceGlobalPool +0x140 / +0x145`, `+0x181 / +0x186` | `NtSetInformationWorkerFactory` (클래스 5 = `0x300`, 클래스 14 = `0x180`) | 2 |

- 2 + 1 + 1 + 6 + 2 = **12**.
- 표 6 이 6인 까닭: 타이머 하위 큐가 **둘** 이고, 큐 하나마다 "알람 시계 (`NtCreateTimer2`) + 줄 (대기 패킷) 만들기 + 줄 잇기" 세 번이다. 3 × 2 = 6 `[정적]`.

**(나) 첫 TP 개체 = 1**

| 표 # | 자리 | syscall | 횟수 |
|---|---|---|---|
| 8 | `TpAllocTimer +0x16d / +0x172` | `NtSetInformationWorkerFactory` (클래스 3, 값 1) | 1 |

- 풀의 **첫** TP 개체라서 (옛 개수 0) 부른다. 두 번째 개체부터는 안 부른다 (`TpAllocWait` 의 같은 갈래는 옛 개수 ≥ 1 이라 건너뜀) `[정적]`.

**(다) WNF 알림 벨 = 4**

| 표 # | 자리 | syscall | 횟수 |
|---|---|---|---|
| 9 | `RtlpInitializeWnf +0x98 / +0x9d` → `RtlpWnfRegisterTpNotification +0x28 / +0x2d` | `NtCreateEvent` (벨) | 1 |
| 10 | 〃 `+0x47 / +0x4c` → `TpAllocWait +0xba / +0xbf` | `NtCreateWaitCompletionPacket` (줄) | 1 |
| 11 | 〃 `+0x77 / +0x7c` | `NtSetWnfProcessNotificationEvent` | 1 |
| 12 | 〃 `+0x92 / +0x97` → `TpSetWaitEx +0x168 / +0x16d` | `NtAssociateWaitCompletionPacket` (줄 잇기) | 1 |

- 표 11 이 커널 `EPROCESS.WnfContext` 를 처음 만드는 syscall 이다 (run51, [run51 해설](run51-eprocess-later-해설.md) 2.8).

**(라) WNF 구독 = 1**

| 표 # | 자리 | syscall | 횟수 |
|---|---|---|---|
| 13 | `RtlpSubscribeWnf… +0xde / +0xe3` → `RtlpAddWnfUserSubToNameSub +0xd5 / +0xda` | `NtSubscribeWnfStateChange` | 1 |

- 12 + 1 + 4 + 1 = **18**.
- 덤: 같은 함수 안 `+0xa5` (ret `+0xaa`) `RtlpEnumProcessHeaps` 는 syscall 없음. KUSD `UserModeGlobalLogger` (`0x7ffe038e`) 가 0 이라 `RtlpWnfETWEventSubscribe` 는 안 탄다 `[관찰]` + `[정적]`.

#### 덩어리 ③ — ETW 등록 넷 = **9** (= 6 + 1 + 1 + 1)

자세한 뜻은 2.6 · 2.7. 여기선 숫자만.

| 표 # | 누구 | syscall | 횟수 |
|---|---|---|---|
| 14 ~ 17 | A 의 **Once** | `NtCreateEvent` · `NtCreateWaitCompletionPacket` · `NtAssociateWaitCompletionPacket` · `NtTraceControl(0x1b)` | 4 |
| 18 | A · B · C · D 각각 `EtwpRegisterProvider +0x14d / +0x152` | `NtTraceControl(0xf)` | 4 |
| 19 | A 만 `EtwpSetProviderTraits +0xdd / +0xe2` | `NtTraceControl(0x1e)` | 1 |

- 등록별로 묶으면: A = 4 + 1 + 1 = **6**, B = **1**, C = **1**, D = **1** → 6 + 1 + 1 + 1 = **9**.
- 표 별로 묶으면: 4 + 4 + 1 = 9. 같은 숫자다.

#### 합계

```
  ①  3   (TpInitializePackage)
+ ② 18   (12 풀 + 1 첫 개체 + 4 WNF 벨 + 1 WNF 구독)
+ ③  9   (A 6 + B 1 + C 1 + D 1)
= 30
```

시간 순서 (run 문서): `TpInitializePackage` 3 → 풀 만들기 (3 ~ 7) → 첫 개체 (8) → WNF 알림 (9 ~ 12) → WNF 구독 (13) → A 의 Once 넷 → A `0xf` → A `0x1e` → B `0xf` → C `0xf` → D `0xf`.

- 남는 핸들 (결과서): 이벤트 2 · 대기 패킷 4 · IO 완료 포트 1 · 일꾼 공장 1 · 타이머 2 = **10개** `[정적]`. run44 핸들 수 P1 4 → P1a 26 의 증가분 22 중 일부로 보인다 `[추측]`.

#### 그런데 31 일 수도 있다 — `TpAllocPoolInternal +0x443`

결과서의 "풀 만들기 12" 에 **없는 줄** 을 flow-writer 가 `.asm` 에서 찾았다 (run 문서 "결과서와 다르게").

1. 근거 `[정적]`: `NtCreateWorkerFactory` (`+0x303`) 바로 뒤 `+0x316 CMP [RSP+0xb8],R15D` → `+0x31e JNZ → +0x42e` 일 때만 `+0x443` (ret `+0x448`) `NtSetInformationWorkerFactory` (클래스 `0xd`) 를 부른다. 돌아와 `+0x460 JMP → +0x324`.
2. 그래서: 이 갈래를 타면 풀 만들기가 12 → 13, 덩어리 ② 가 18 → 19, 합계가 **30 → 31** 이 된다.
3. 아직 모름: `[RSP+0xb8]` 에 무엇이 들어가는지 안 봤다 → test.exe 가 타는지 `[공백]`.
4. 결과서 숫자 (30) 는 고치지 않았다. 닫는 길 둘: 정적 **Q119 (a)** (`[RSP+0xb8]` 출처), 동적 D9 멈춤점에 `ntdll+0xe5e43` 을 같이 걸기 (Q104).

- PE_emulator 에 주는 뜻: 호출 약속은 "30, 단 `+0x443` 갈래를 타면 31" 로 읽어야 한다.

### 2.6 새 말 — ETW 공급자와 등록, 그리고 A · B · C · D

- **ETW**: 사건 기록 장치 `[추측]` ([run12 해설](../02_sprint/run12-insert-thread-해설.md#212-그-밖에-확인된-것) 에서 쓴 말). 비유: **방송국**.
- **ETW 공급자** (provider): 사건을 내보내는 쪽 하나. GUID (16바이트 이름표) 로 구별한다. 비유: 방송국의 **채널** 하나.
- **등록** (`EtwEventRegister`): "이 GUID 로 방송할게요" 하고 방송국에 채널을 올리는 일. 끝에 커널에 `NtTraceControl(0xf)` 로 알린다 `[정적]`.
- **RegHandle**: 등록하면 돌려받는 8바이트 손잡이. `(순번 << 32) | 칸 번호` 로 만든다 (`EtwNotificationRegister +0x184 ~ +0x193`) `[정적]`. 비유: 채널 **번호표**.
- **TraceLogging**: 이번 근거에서 본 모양은, 이름·GUID 를 담은 **공급자 구조체** (A 는 `0x38`B, 메타데이터 `0x1801a3190` 을 가리킴) 를 넘겨 등록하는 것이다. 이번엔 `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` 이 그 일을 한다 — 이름 그대로 `EtwEventRegister` + `EtwEventSetInformation` 두 걸음 `[정적]`.

이 구간의 등록 넷 (결과서 6 · 7번 표) `[정적]`:

| | 부른 자리 (call / ret) | 공급자 | 콜백 | 번호표 칸 | syscall |
|---|---|---|---|---|---|
| **A** | `+0xbee / +0xbf3` → `TraceLoggingRegisterEx… +0x55 / +0x5a` | `EtwUserModeDiagnosticProvider` (구조체 `0x1801cc6f0`) | — | 구조체 `+0x20` | **6** |
| **B** | `+0xc06 / +0xc0b` | `PrivateLoggerNotificationGuid` | 0 | `g_hPrivLoggerNotificationProvider` | 1 |
| **C** | `+0xc23 / +0xc28` | `UserDiagnosticGuid` | `UserDiagnosticProviderCallback` | `g_hUserDiagnosticProvider` | 1 |
| **D** | `+0xc28 / +0xc2d` → `RtlInitializeHeapLogging +0x1e / +0x23` | `WindowsHeapSnapshotProvider` | `RtlpHpStackTraceEtwCallback` | `RtlpHpStackTraceProviderHandle` | 1 |
| (E) | `RtlInitializeHeapLogging +0x3e / +0x43` | `Microsoft.Windows.Heap` (`0x1cc670`) | — | — | **안 탐** `[관찰]` |

- E 가 안 타는 까닭: `RtlInitializeHeapLogging +0x23 TEST [RtlpHpHeapFeatures],8` 인데 `RtlpHpHeapFeatures` 가 끝까지 0 `[관찰]`.
- `LdrpInitializeProcess` 는 B · C 의 반환을 **검사하지 않는다** `[정적]`.
- A 만 syscall 이 6인 까닭은 둘이다 (2.7):
  - 첫 등록이라 **Once 4** 가 붙는다.
  - TraceLogging 이라 둘째 걸음 `EtwEventSetInformation +0x2e` (꼬리 JMP) → `EtwpSetProviderTraits +0xdd` (ret `+0xe2`) `NtTraceControl(0x1e)` 가 붙는다.
- 번호표 계산값: A `0x1_00000000` · B `0x2_00000011` · C `0x3_00000021` · D `0x4_00000031` `[정적]` (계산값. 확인은 D5).

### 2.7 "Once" 는 왜 첫 등록 한 번뿐인가 — 단계별로

이게 이번 바퀴의 갈림길이다. 천천히 본다.

#### 먼저 — "Once" 가 무엇인가

- 결과서의 정확한 뜻: 등록 넷 **모두** `NtTraceControl(0xf)` 를 1번씩 부른다. "Once" 는 그 **위에 얹히는** "TP 알림 등록 한 덩어리" 다 `[정적]`:
  - `EtwpRegisterTpNotificationOnce` (rva `0x748f0`) 안
  - `+0x32` (ret `+0x37`) `NtCreateEvent` — 알림 **벨**
  - `+0x4d` (ret `+0x52`) `TpAllocWait` (콜백 `EtwpNotificationThread`) → `NtCreateWaitCompletionPacket` — 벨 **지키는 사람** (대기 개체)
  - `+0x64` (ret `+0x69`) `TpSetWaitEx` → `NtAssociateWaitCompletionPacket` — 벨에 줄 잇기
  - `+0x8c` (ret `+0x91`) `NtTraceControl(0x1b, 입력 4B = 이벤트 핸들)` — 커널에 "이 벨로 알려 주세요"
- 커널 쪽에선 이 `0x1b` 가 `EPROCESS.EtwDataSource` 를 만든다 (run51, [run51 해설](run51-eprocess-later-해설.md) 2.8).
- 2.5 의 WNF 벨 (표 9 ~ 12) 과 모양이 같다: 벨 + 지키는 사람 + 커널에 알리기 + 줄 잇기. ETW 판이 하나 더 있는 셈이다.

#### 왜 한 번뿐인가

1. **근거 ① — 팻말 칸이 따로 있다** `[정적]`. `EtwpRegisterTpInitOnce` (`.data` `0x1d2268`, RTL_RUN_ONCE 8B, **파일 값 0**). run61 해설의 주사위 상자 팻말과 같은 꼴이다.
2. **근거 ② — 매 등록이 이 팻말을 먼저 본다** `[정적]`. `EtwpRegisterProvider` (모든 등록이 `EtwEventRegister +0x1e → EtwNotificationRegister +0x15c` 를 거쳐 여기로 온다):
   - `+0x4f` `PebLdr.ShutdownInProgress` (`0x1d28c8`) 가 0 인지 본다 (문 닫는 중이 아니면 계속).
   - `+0x67` 팻말을 읽는다. **2 (다 함)** 면 `+0x84 JZ → +0xd7` 로 Once 를 **건너뛴다**.
   - **0 (처음)** 이면 `+0x95 CMPXCHG.LOCK` 으로 0 → 1. 이긴 쪽만 `+0xaf` (ret `+0xb4`) `EtwpRegisterTpNotificationOnce` 를 부른다.
   - 끝나면 `+0xc8` `RtlRunOnceComplete` 로 팻말 → **2**.
3. **근거 ③ — 팻말과 상관없이 매번 하는 것** `[정적]`: `+0x14d` (ret `+0x152`) `NtTraceControl(0xf)`. 그래서 넷 모두 1번씩.
4. **그래서**: 팻말이 0 인 채로 `EtwpRegisterProvider` 에 처음 들어오는 등록 **하나만** Once 4 를 탄다. 나머지는 팻말 2 를 보고 지나간다.
5. **첫 번째는 누구인가 — 근거 ④** `[정적]`:
   - 이 구간 안 순서: `+0xbee` (A) → `+0xc06` (B) → `+0xc23` (C) → `+0xc28` 안 (D). **A 가 가장 앞이다.**
   - 이 구간보다 **앞** 에서 test.exe 가 `EtwNotificationRegister` 에 닿는 길은 없다: `SbSelectProcedure` (힙 관리자 안) 는 `RtlpHpOptIntoSegmentHeap` 이 0 이라 안 탐, `SbObtainTraceHandle` 은 뒤쪽 `+0x19c8`.
   - 그러니 **첫 번째 = A** (`+0xbee → TraceLoggingRegisterEx… +0x55 → EtwEventRegister`).
6. **덤 — Once 안의 TP 개체 둘은 풀을 또 짓지 않는다** `[정적]`. 기본 풀은 2.3 에서 이미 생겼다. `TpAllocWait +0x115 XADD` 옛 값이 ≥ 1 이라 `NtSetInformationWorkerFactory` 도 안 부른다 (CALL `+0x31b` / ret `+0x320`). 그래서 Once 가 딱 **4** 다.
   - 만약 순서가 반대였다면 (ETW 가 먼저) Once 쪽에서 풀을 지었을 것이고 숫자가 달라졌을 것이다 — 결과서가 "이 순서가 Once 쪽 횟수 4번을 정한다" 고 적은 뜻이다.
7. **아직 확인 아님**: 팻말이 처음 0 → 첫 호출 뒤 2 인지, 사진 아닌 멈춤으로 본 건 아니다 → D3 (Q104). 사진은 `EtwpRegisterTpInitOnce` 가 `≤P1a` 에 값이 있다는 것까지 `[관찰]`.

- 비유: 방송국에 채널을 올리러 오는 사람이 넷이다. 방송국 창구 옆 **팻말** 이 "벨 아직 안 놓음" 이면, 그날 처음 온 사람만 **알림 벨 + 벨 지키는 사람** 을 놓고 팻말을 "다 놓음" 으로 뒤집는다. 벨 지키는 사람은 아까 차린 일꾼 사무실에서 빌려 온다. 그 뒤 셋은 팻말만 보고 채널 등록 (`0xf`) 만 한다. 그날 처음 온 사람은 **TraceLogging 채널 (A)** 이었다.

#### 한 줄 읽기 — `EtwpRegisterProvider +0x95`

- **누가**: `EtwpRegisterProvider` (rva `0x72a70`).
- **어디서**: `+0x95`, 명령 `CMPXCHG.LOCK` `[정적]`.
- **누구를**: 칠판 칸 `EtwpRegisterTpInitOnce` (`0x1d2268`).
- **무엇을 위해**: "팻말이 아직 0 이면 1 로, 그 사이 누가 바꿨으면 바꾸지 마라" 를 한 번에. 이긴 스레드 **하나만** `+0xaf` 로 내려가 벨을 놓는다.
  - run61 의 `RtlRandom +0x34` 와 같은 꼴, run17 해설 2.6 의 "발판에 먼저 오른 한 명" 과도 같다.
- **조건**: `+0x4f` 문 닫는 중 아님, `+0x67` 팻말이 0.
- **결과**: 팻말 1 → (Once 끝) `RtlRunOnceComplete` 로 2.

### 2.8 run51 정정 — 무엇이 틀렸고 무엇은 맞나

| | run51 (전) | run62 (후) |
|---|---|---|
| Once 를 타는 자리 | `+0xc06` / `+0xc23` (ret `+0xc0b` / `+0xc28`) 의 `EtwEventRegister` 아래 | **`+0xbee`** (ret `+0xbf3`) `TraceLoggingRegisterEx… +0x55` (ret `+0x5a`) → `EtwEventRegister` 아래, **한 번** |
| `+0xc06` · `+0xc23` 이 부르는 것 | Once (`NtTraceControl(0x1b)`) | `NtTraceControl(0xf)` 만 |
| 사슬 아래쪽 `EtwEventRegister +0x1e → EtwNotificationRegister +0x15c → EtwpRegisterProvider +0xaf → EtwpRegisterTpNotificationOnce` | — | **그대로 맞다** |
| 커널 쪽 결과 (`EPROCESS.EtwDataSource`) | `NtTraceControl(0x1b)` → `EtwpAddDataSource` | 같다 (자리만 바뀜) |

- 왜 틀렸을까: run51 표는 `LdrpInitializeProcess` 가 `EtwEventRegister` 를 **직접** 부르는 자리 (`+0xc06` · `+0xc23`) 만 적었다. A 는 `TraceLoggingRegisterEx…` 라는 포장 함수 안에서 부르니 직접 호출 목록에 안 보였을 것이다 `[추측]`. 그리고 "Once = 첫 번째만" 이라는 팻말 갈래를 그때는 안 봤다.
- 고친 곳 (run 문서 "정정"): run51 문서 맨 위 한 줄, flow.md 2.4 run51 절 트리 줄 + 정정 단락, 짐 지도 3.11 `EtwDataSource` 줄 · 5장 ② 줄.
- 참고: [run51 해설](run51-eprocess-later-해설.md) 2.8 의 그림 줄 `+0xc0b / +0xc28 EtwEventRegister → … → EtwpRegisterTpNotificationOnce` 는 옛 내용 그대로다. 이 글은 고치지 않았다 (보고에 적음).

### 2.9 앞 구간 `+0xaad ~ +0xb05` — 첫 기능 상태 검사 (54 공사 중 셋)

여기는 이번 질문의 곁가지지만, run59 정정이 붙어서 따로 본다.

#### 새 말 둘

- **기능 상태 검사** (`Feature_…__private_IsEnabled…`): "이 기능 켜졌나?" 를 묻는 작은 함수. 상태 칸에 답을 **캐시** 해 두고, 처음엔 꼬리 `…Fallback` 으로 넘어가 `RtlQueryFeatureConfiguration` 까지 가서 알아 온다 `[정적]`.
  - 비유: 관리실 칠판 구석의 **기능 스위치 표**. 처음 물을 땐 본사 목록을 찾아보고, 답을 칠판에 적어 둔다. 다음부터는 칠판만 본다.
- **설정 구역**: 그 "본사 목록" 이 들어 있는 기능 설정 버퍼. 커널에서 구역 핸들을 받아 `NtMapViewOfSection` 으로 붙이고 핸들을 닫는다 `[정적]`. 이걸 담아 두는 칠판 칸이 `RtlpFcProcessManager` (`0x1d3c40`, `0x118`B, 버퍼 두 벌 + 도장).
  - `Fc` = Feature Configuration 으로 보인다 `[추측: 이름]` (flow.md 2.4).

#### 왜 `+0xaad` 에서 syscall 이 붙나 — 단계별로

1. 근거 `[정적]`: init 지도에서 `Feature_…IsEnabled` 를 부르는 곳은 `+0xaad` · `+0xac0` 둘이 **처음** 이다. 그러니 `+0xaad` 가 이 프로세스의 첫 기능 상태 검사다.
2. 근거 `[정적]` + `[관찰]`: 버퍼가 낡았는지는 버퍼 도장 (`RtlpFcProcessManager +0xe0`) 과 KUSD `+0x720` `FeatureConfigurationChangeStamp` 를 견준다 (`RtlpFcReferenceFeatureConfigurationBuffers +0x6e`). 도장은 run59 `+0x21c ~ +0x286` 이 0 으로 만든 직후라 **0**, KUSD 는 **1** `[관찰]`. `0 < 1` → 받아 온다.
3. 받아 오는 길: `RtlpFcUpdateLocalConfiguration` 안
   - `+0xad` (ret `+0xb2`) `NtQuerySystemInformationEx` (클래스 `0xd3`) — **1번**
   - 구역 4종마다, 커널이 준 핸들이 0 이 아니면 `RtlpFcMapSingleBuffer +0x57` (ret `+0x5c`) `NtMapViewOfSection` — **k번**
   - `+0xff` (ret `+0x104`) `RtlpFcBufferManagerUpdateBuffers` — 여기서 `RtlpFcProcessManager` 가 **채워진다**
   - 남은 핸들마다 `+0x137` (ret `+0x13c`) `NtClose` — **k번**
4. 그래서 합 = **`1 + 2k`** (k = 0 ~ 4). k 는 실행 때 커널이 몇 개 주느냐라 **동적** (D7).
5. 다음 검사 (`+0xac0`) 부터는 받아 둔 버퍼를 써서 이 syscall 이 다시 안 붙는 게 정상이다 `[정적]`.

- 비유: 관리실이 처음으로 "이 기능 켜졌나?" 를 물으려는데, 본사 목록 사본에 찍힌 날짜 도장 (0) 이 로비 공용 게시판의 최신 도장 (1) 보다 낡았다. 그래서 창구에서 최신 목록을 받아 와 (`0xd3` + 구역 붙이기 k + 핸들 닫기 k) 칠판 서랍 (`RtlpFcProcessManager`) 에 넣는다.

#### run59 정정

- run59: `RtlpFcProcessManager` 는 "전부 0, `+0x108` 목록 머리 자기 주소" `[정적]`.
- run62: 그건 `+0x21c ~ +0x286` **직후 값** 으로는 맞다. 같은 함수 뒤 `+0xaad` 가 버퍼 두 벌과 도장을 채운다. 사진 P1a 의 값은 이쪽에서 온 것이다 `[정적]`. 그 길의 syscall 도 짐 지도에 없었다.

#### PE_emulator 54 의 고정값 셋

| | 54 의 고정 | run62 답 | 근거 |
|---|---|---|---|
| (a) `Feature_…DisallowSharedLockImplicitUpgrade` | "켜짐" | **맞다**. `+0xaba` 가 `RtlpSrwLockAllowImplicitUpgrade` ← `(EAX == 0)`. 사진 P1a (`+0x199d`, 이 줄 **뒤**) ~ P2 모두 0 → 함수가 0 아닌 값 (켜짐) 을 돌려줬다 | `[정적]` + `[관찰]` |
| (b) `Feature_…LoaderSnapsBuffer` → `LdrpInitializeSnapsBuffer` | 건너뜀 | **실제와 다르다**. test.exe 에서 참 — `LdrpSnapsUnicodeString{,2}` 가 `≤P1a` 에 값 | `[관찰]` |
| (c) `RtlInitializeExceptionLog` | — | **안 탄다**. `+0xaf4 TEST [RDI+0xbc],0x800000` (PEB `NtGlobalFlag`) 가 0 | `[관찰]` |

- (b) 의 **스냅 버퍼** (새 말): 로더가 남기는 글 (로그) 을 쌓아 두는 4 KiB 칸. `LdrpInitializeSnapsBuffer +0x1b` (ret `+0x20`) 가 창고 (프로세스 힙) 에서 `0x1000` 바이트를 받아 두 문자열 칸에 같이 건다 `[정적]`. 비유: 관리실 **일지 노트**.
  - 건너뛰면 syscall 수는 **같다** (`RtlAllocateHeap` 하나뿐). 대신 ① 창고에서 `0x1000` 블록 하나가 빠져 뒤 힙 주소·사용량이 달라지고 ② 전역 4칸이 0 으로 남고 ③ 뒤 `LdrpLogInternal` 이 서식 만들기를 통째로 건너뛴다 (`+0x85 ~ +0x8c` 가 NULL 이면) `[정적]`.
  - (b) 안에는 "사용량 보고" 갈래 (`RtlpFcNotifyFeatureUsageTarget` 안 `NtQueryWnfStateData` · `NtUpdateWnfStateData`) 도 있다. 이 VM 설정에서 켜졌는지는 동적 (D13). 그래서 run 문서 표에서 이 줄은 "0 또는 각 1 이상 `[추측]`".

### 2.10 이름 없는 자리 둘 — Ghidra 주석을 믿으면 안 되는 이유

- `0x1801cc6f0` — Ghidra 주석은 `NormalizationListHead+0x48`. 실제로는 **이름 없는 TraceLogging 공급자 구조체** (`0x38`B, 이름 `EtwUserModeDiagnosticProvider`, `+0x20` 번호표) `[정적]`. A 가 넘기는 그 구조체다.
- `0x1d23e0` — Ghidra 주석은 `g_pShimmedModuleList+0x8`. 실제로는 **이름 없는 run-once 칸** (쓰는 곳 `LdrpEnableUMGLTracingStateSync` 뿐, `+0x35 CMPXCHG.LOCK`) `[정적]`.
- 왜 헷갈리나: Ghidra 주석은 "**가장 가까운 앞 이름 + 거리**" 일 뿐이다. PDB 에 이름이 없는 칸은 앞 이웃 이름에 붙어 나온다.
  - 비유: 이름표 없는 상자를 "옆집 상자 + 48칸" 이라고 부른 것. 옆집 상자가 아니다.
- 짐 지도 7.3 의 `g_pShimmedModuleList` · `NormalizationListHead` 가 `≤P1a` 로 보인 까닭이 이 이름 없는 자리일 수 있다 `[추측]` (7.5).

### 2.11 사진과 맞춰 보기 (run44, 결과서가 다시 읽음)

대표만. 전부는 run 문서 "사진 대조".

| 칸 | 사진 | 뜻 |
|---|---|---|
| PEB `TppWorkerpList` (`+0x390`) | P1 `[0 - 0]` → P1a 자기 주소 둘 | `TpInitializePackage` 성공 갈래 `[관찰]` |
| 매개변수 `+0x430 ~ +0x440` | 8곳 모두 0 | `TpSetDefaultPoolCpuSets` · `+0xbbe` 안 탐 `[관찰]` |
| `TppPoolpGlobalPool` · `TppNumberNodes` · `TppMaximumGroups` | `≤P1a` 값 있음 | 풀이 P1a 전에 생김 `[관찰]` |
| `EtwpRegisterTpInitOnce` · `g_hPrivLoggerNotificationProvider` · `g_hUserDiagnosticProvider` · `RtlpHpStackTraceProviderHandle` 등 | `≤P1a` 값 있음 | 네 등록이 P1a 전에 성공 `[관찰]` |
| `EtwpGuidEntryTable` · `EtwpFreeRegistrationList` · `g_isErrorOriginProviderEnabled` | 끝까지 0 | 켜짐 정보로 콜백이 불린 흔적 없음 `[관찰]` |
| `RtlpSrwLockAllowImplicitUpgrade` | P1a ~ P2 0 | (a) 켜짐 `[관찰]` |
| KUSD `+0x720` | 1 | 설정 버퍼를 받아 옴 `[관찰]` |

- 사진은 "끝 값" 이다. **순서** (풀이 ETW 보다 먼저, A 가 첫 등록) 는 사진이 아니라 정적으로 정했다.

### 2.12 디버거가 붙어서 달라지는 길 (CLAUDE.md "따로 적기")

- 이 구간은 `PEB.BeingDebugged` 를 안 읽는다 `[정적]`.
- 다만 커널 디버거·ETW 세션이 이 공급자들을 **켜 두면**, 등록 때 콜백 (`EtwpUpdateEnableInfoAndCallback +0xae`) 과 `0xc0000023` 되풀이가 생길 수 있다 `[정적]`. 그러면 30 이 늘어난다.
  - `NtTraceControl(0xf)` 는 버퍼가 모자라면 큰 버퍼로 되풀이, `0x1e` 는 최대 16번까지 되풀이할 수 있다 (결과서).
- → 동적 D2 · D4 (Q104). 2.5 의 "전제 셋" 이 바로 이 이야기다.

### 2.13 결과서와 run 문서가 다르게 적은 곳 (뜻은 같거나, run 문서가 하나 더 봄)

- `TpAllocWait` 의 `NtSetInformationWorkerFactory`: 결과서 `+0x300` 은 그 갈래의 첫 줄, CALL 은 **`+0x31b` / ret `+0x320`** (run 문서). 뜻은 같다.
- `TpAllocPoolInternal +0x443` 갈래: 결과서에 없음, run 문서가 짚음 (2.5 끝). 합계 30 은 그대로 두고 `[공백]`.
- `RtlRunOnceBeginInitialize(0x1ce1f8)`: 결과서 `+0x4b ~ +0x5c` 범위, CALL 은 `+0x57` / ret `+0x5c` (`+0x4e` 는 칸 주소 `LEA`).
- 이 글의 숫자는 모두 run 문서 쪽이다.

### 2.14 정리 — 비유로 한 번 더

1. 창고를 지은 관리실이 처음으로 "기능 켜졌나?" 를 묻는다. 본사 목록이 낡아서 창구에서 새로 받아 온다 (`1 + 2k`). 일지 노트 (스냅 버퍼) 도 하나 마련한다.
2. 일꾼 **대기실 명부** 를 편다 (`TpInitializePackage`, 3). CPU 몇 개·그룹 몇 개인지 창구에 묻는다.
3. 관리사무소에 **알림 신청서** (WNF) 를 처음 낸다. 알림을 받아 줄 사람이 필요하니 그 자리에서 **공용 일꾼 사무실** (기본 풀) 을 처음 차린다 — 우편함 · 파견소 · 알람 시계 둘 (12). 첫 호출 버튼을 달고 (1), 알림 벨을 놓고 (4), 신청서를 낸다 (1). 합 18.
4. **방송국에 채널 넷** 을 올린다. 처음 온 TraceLogging 채널 (A) 만 방송국 **알림 벨 + 벨 지키는 사람** 을 놓고 팻말을 뒤집는다 (Once 4). 지키는 사람은 3 에서 차린 사무실에서 빌려 온다. A 는 채널 등록 (1) + 채널 특성 알리기 (1) 까지 6. 나머지 B · C · D 는 등록만 1씩. 합 9.
5. 한참 뒤 짐 들이기 직전에 **짐 들이기 팀 전용 사무실** (로더 전용 풀, `+0x1a42`) 을 또 차린다. run19 가 "첫 사무실" 이라고 적은 건 이쪽이었다.
6. run51 은 "벨은 B · C 때 놓았다" 고 적었다. 실제로는 **A 때 한 번** 이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntdll.pdb 목록에 run62 없음 | run62 추가 + "(run62) 정적 바퀴, run44 덤프 읽기 로그 다시 읽음" |
| 그림 2 `j1` 상자 글 | "스레드 풀 + 일감 개체 … **일꾼이 생길 수 있는 첫 자리**" | "**로더 전용 풀 LdrpThreadPool** + 일감 개체 … run62: 기본 풀·일꾼 공장은 앞 +0xbe2 에서 이미 생김". 상자·화살표 수는 그대로 |
| 2.4 `LdrpInitializeProcess` 트리 | — | 두 줄: `+0xab2 · +0xac5` 기능 상태 검사 둘 · `+0xace` 스냅 버퍼 ⑦, `+0xbe7` 기본 풀 처음 · ETW 등록 넷 (Once 는 `+0xbf3` 만) ⑧ |
| 2.4 묶음 8 표 줄 | "스레드 풀, ETW 등록 3곳" | 뒤에 "(run62: 등록 4번, 기본 스레드 풀은 `+0xbe2` 에서 처음)" |
| 2.4 run59 `RtlpFcProcessManager` 줄 | "전부 0 … `Fc` 뜻은 `[공백]`" | "→ (run62) 끝 값 아님, `+0xaad` 가 채움. `Fc` = Feature Configuration 으로 보인다 `[추측: 이름]`" |
| 2.4 병렬 로딩 줄 | "스레드 풀을 만드는 첫 자리는 `+0x1a47`" | ~~취소선~~ → "**로더 전용 풀** 을 만드는 자리, 첫 풀은 `+0xbe2`" (run62 정정) |
| 2.4 새 절 | 없음 | "기능 상태 검사·스레드 풀·ETW 등록 — `+0xaad ~ +0xb05` · `+0xb83 ~ +0xc2d`" 트리 + `[정적]` 단락 여섯 + 디버거 갈래 |
| 2.4 run51 절 트리 | `+0xc0b / +0xc28 EtwEventRegister → … → EtwpRegisterTpNotificationOnce` | `+0xbf3 TraceLoggingRegisterEx… → +0x5a EtwEventRegister → …` (run62 정정) + 정정 단락 |
| 3장 "ntdll 일꾼 2개" | run19 "첫 자리 `+0x1a42`" | → 로더 전용 풀로만 맞음 + 새 run62 줄 (기본 풀 `+0xbe2`, `ActiveThreads` P1a 1 `[관찰]`, 일꾼이 어느 풀 것인지 `[공백]`) |
| 4장 공백 표 | — | Q118 **(해결됨)** 줄 + 새 줄 넷 (동적 D1 ~ D13, 정적 남은 것, 정정, 결과서와 다르게) |
| 5장 | — | run62 줄 둘 |

태그 변화 대표:

- 이 프로세스 첫 스레드 풀: `+0x1a42` (`[정적]` run19) → **`+0xbe2`** (`[정적]` run62). `+0x1a42` 는 로더 전용 풀로 남음.
- ETW Once 자리: `+0xc0b / +0xc28` (`[정적]` run51) → **`+0xbf3` 한 번** (`[정적]` run62).
- `RtlpFcProcessManager`: "전부 0" (`[정적]` run59) → **`+0xaad` 에서 채워짐** (`[정적]` run62). `Fc` 뜻 `[공백]` → `[추측: 이름]`.
- 그림 2 에 새 상자는 **안 넣었다**. 넣으면 `LdrpInitializeProcess` 의 `(n)` 번호가 다 밀리는데, 이 구간은 칸·syscall 만 늘고 흐름 (모듈·스레드) 은 안 바뀌어서다. 넣을지는 사용자와 정한다 (Q119 (e)).

### 짐 지도 `baggage.md` 에서 바뀐 것 (열여덟째판)

- 3.1 PEB 에 **2줄** (`TppWorkerpListLock` `+0x388` · `TppWorkerpList` `+0x390`, "Q118 로 닫힘"). PEB 모르는 칸 75 → **73**, ntdll 15 → **17**.
- 3.8 `.data` 에 이름 있는 전역 **28개** (`≤P1a` 16 · `끝까지 0` 12) + `RtlpFcProcessManager` 둘째 줄 + 목록 밖 자리 **5줄** (`0x1d23e0` · `0x1ce1f8` · `0x1ce200` · `0x1ce2a0 ~ 0x1ce2e8` · `0x1cc6f0`). `.data` 아는 것 94 → **122**, 모름 344 → **316**.
- 3.11 `EtwDataSource` 줄 ntdll 사슬 고침 (정정), `WnfContext` 줄에 풀 만들기 한 마디.
- 4장 짝 3줄, 5장 ② 의 `+0xaad ~ +0xc2d` 순서, 6장 부품 4줄, 7.2 · 7.3 목록, 7.5 실마리, 7.6 옆 짐 (기본 풀 구조 · 커널 개체 10 핸들 · 설정 구역 뷰), 새 **7.22** (동적).

### 큰 그림에서 어떻게 이어지나

- **PE_emulator 55 바퀴가 이 구간을 지을 재료가 생겼다.** "공사 중" 빈 함수 대신, syscall 30 (또는 31) 의 이름·순서·조건이 있다.
  - 54 바퀴 쪽은 하나 고칠 거리가 생겼다: 스냅 버퍼를 건너뛰면 창고 모양이 실제와 달라진다.
- **스레드 지도가 바뀌었다.** "일꾼 사무실은 언제 처음 생기나" 의 답이 `+0x1a42` 에서 `+0xbe2` 로 앞당겨졌다. 그런데 일꾼 **사람** 이 P1a 까지 없다는 사진과 맞춰 보면, "사무실이 생긴 때" 와 "일꾼이 출근한 때" 가 다르다. 3장 일꾼 2개의 정체 (Q47) 를 볼 때 이 둘을 갈라 봐야 한다.
- **커널 짐 두 칸** (`EtwDataSource` · `WnfContext`) 은 그대로인데, 그 칸을 만드는 ntdll 사슬이 바로잡혔다. 에뮬레이터가 "어느 syscall 에서 커널 짐을 만들어 줄지" 정할 때 이 자리가 쓰인다.
- 다음 구간은 `+0xc2d RtlpInitEnvironmentBlock` (환경 블록·매개변수 블록) 이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **`TpAllocPoolInternal +0x443` 갈래** 를 test.exe 가 타나 (타면 30 → 31) | `+0x316` 의 `[RSP+0xb8]` 에 무엇이 들어가는지 안 봄 (결과서에 없던 줄, flow-writer 가 짚음) `[공백]` | **Q119 (a)** (정적) 또는 D9 에 `ntdll+0xe5e43` (Q104) |
| **D1** 구간 syscall 실제 개수·종류 (기대 30 · 앞 `1 + 2k`) | 정적 셈. 실제 실행 순서는 안 봄 | **Q104 VM 단계** (동적) |
| **D2** `NtTraceControl` 코드 (`0xf` ×4 · `0x1b` ×1 · `0x1e` ×1) 와 `0xc0000023` 되풀이 | 켜진 세션 여부는 실행 때 값 | Q104 |
| **D3** Once 팻말 (`ntdll+0x1d2268`) 이 처음 0 → 첫 호출 뒤 2 인가 | 사진은 끝 값만 | Q104 |
| **D4** 등록 콜백까지 가나 (안 걸려야 정상) | 켜짐 정보는 커널이 줌 | Q104 |
| **D5** 번호표 값 넷 (`0x100000000` · `0x200000011` · `0x300000021` · `0x400000031`) | 정적 계산값 | Q104 |
| **D6** `RtlpHpHeapFeatures & 8` = 0 (E 안 탐) | 사진 뒷받침만 | Q104 |
| **D7** 설정 구역 수 **k** | 커널이 몇 개 주느냐는 실행 때 값 | Q104 |
| **D8** `TppQueryMaximumGroupCount` 가 정말 2번인가 (또 모자라 되풀이?) | 실행 때 값 | Q104 |
| **D9** `TpAllocPoolInternal` 입구가 1번만, `+0xbe2` 안에서만 걸리나 | 정적 | Q104 |
| **D10** `+0xbe2` 직후 일꾼 스레드가 생기나, run5 일꾼 2개는 어느 풀 것인가 | 사진 `ActiveThreads` P1a 1 은 "P1a 에 살아 있는 일꾼 없음" 까지만 말함 `[공백]` | Q104 · **Q47** |
| **D11 ~ D13** (a) `ecx` = 0, `NtGlobalFlag` 비트, (b) 참·스냅 블록 주소, (b) 사용량 보고 갈래가 서나 | 갈래 확인용, (b) 보고 갈래는 이 VM 설정 | Q104 |
| `RtlQueryInformationActivationContext` (`TppCleanupGroupMemberInitialize +0xf3`) 안 syscall | "없음" 으로 셈, 정밀 추적 안 함 | Q119 (b) (정적) |
| (a) 기술자 의존 목록 `0x180174ac8` 안 | 안 봄 `[공백]` | Q119 (c) (정적) |
| `TpInitializePackage` 실패 뒤 `_LdrpInitialize` 쪽 처리 | test.exe 는 안 탐, 범위 밖 `[공백]` | Q119 (d) (정적) |
| 그림 2 에 `LdrpEnableUMGLTracingStateSync` 상자를 넣을지 | 번호가 다 밀림 — 사용자 결정 거리 | Q119 (e) (사용자와 정함) |

- 대기열 순서 (plan): **Q104 VM 단계** → Q78d4 → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q119 는 맨 뒤.
- 정적과 사진이 대부분 맞아서 동적은 급하지 않다 (flow.md 4장).

---

## 5. 더 읽기

- run 문서: [`run62-tp-etw-init.md`](run62-tp-etw-init.md) — "PE_emulator 쪽에서 쓸 것" (1 syscall 표, 2 공사 중 셋, 3 닫힘/동적), 호출 지점 표, 질문별 답, 호출 관계, 사진 대조, 결과서와 다르게, 정정, 짐 지도 반영, 아직 확인 필요 (D1 ~ D13 멈춤점 주소)
- 결과서: [`_workspace/sast/57_tp-etw-init_result.md`](../../_workspace/sast/57_tp-etw-init_result.md) — 한 줄 결론, 질문 4 (`LdrpEnableUMGLTracingStateSync`), 6 (`EtwEventRegister` · Once), 8 (공사 중 셋), syscall 표
- 요청서: [`_workspace/sast/57_tp-etw-init.md`](../../_workspace/sast/57_tp-etw-init.md) — "왜 묻나" (호출 약속 덫)
- 고친 옛 문서: [`run51-eprocess-later.md`](run51-eprocess-later.md) 맨 위, [`run59-init-leftovers.md`](run59-init-leftovers.md) 맨 위
- 흐름도: [`flow.md`](flow.md) — 그림 2 `j1`, 2.4 `#### 기능 상태 검사·스레드 풀·ETW 등록` 절, 2.4 run51 절, 3장 "ntdll 일꾼 2개", 4장 run62 줄들, 5장 run62 줄
- 짐 지도: [`baggage.md`](baggage.md) — 3.1 PEB 2줄, 3.8 전역 28 + 목록 밖 5, 3.11, 7.22
- 진행표: `_workspace/plan.md` Q118 (끝) · Q104 · Q119 · Q47, 결정 기록 2026-11-03
- 앞 해설: [run18](../02_sprint/run18-proc-init-setup-해설.md) (일꾼 대기실) · [run19](../02_sprint/run19-proc-init-modules-해설.md) 2.9 (옛 "첫 자리 `+0x1a42`") · [run24](../02_sprint/run24-work-queue-해설.md) (일꾼 · 호출 버튼) · [run51](run51-eprocess-later-해설.md) 2.8 (게으른 만들기, `EtwDataSource` · `WnfContext`) · [run59](run59-init-leftovers-해설.md) (`RtlpFcProcessManager`) · [run61](run61-random-ex-init-해설.md) 2.2 (팻말)

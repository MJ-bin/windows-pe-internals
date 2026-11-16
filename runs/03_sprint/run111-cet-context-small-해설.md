# run111 cet-context-small — 해설

> 이 글은 [run111 문서](run111-cet-context-small.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q75**: 2.10 `NtContinue` 길에 남은 CET 정적 셋 (a) · (b) · (c) (`_workspace/plan.md`). 이번에 **끝났다**. 한 바퀴로 닫았다.
  - Q75 는 [run42](../02_sprint/run42-continue-detail-해설.md) (sast/37) 이 남긴 "새 정적 공백" 셋이다.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q57** (run111 몫이 덧붙음, 새 번호 없음. **CET 스레드가 있는 환경에서만** 쓸모 있음)
    - 정적 → 새 **Q199 ① ②** (둘 다 낮음)
  - 다음 문서 차례: **Q78d4** (sast/108). plan.md "다음 항목" 줄 순서다.
- 근거:
  - run 문서 [`run111-cet-context-small.md`](run111-cet-context-small.md) (가장 중요)
  - 요청서 [`_workspace/sast/106_cet-context-small.md`](../../_workspace/sast/106_cet-context-small.md) · 결과서 [`106_cet-context-small_result.md`](../../_workspace/sast/106_cet-context-small_result.md). 판정은 **분석성공**이고 스크립트 문제는 없었다.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다.** 글만 바뀌었다: 기준 · 2.1 · 2.3 · 2.10 · 3장 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): 이번 CET 결과로는 **새 짐 칸이 없다.** 다만 같은 바퀴에 run110 이 넘긴 **PEB `NtGlobalFlag2` 를 7.2 → 3.1 로 옮겼다.** 그래서 2장 숫자가 바뀌었다 (2.12, 3절).
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - ntoskrnl 은 다시 analyze 하지 않았다. depth 0 로 13개를 새로 뽑았고, 나머지는 이미 있던 `.asm` 을 읽기만 했다.
  - 이 글의 `[관찰]` 은 모두 **앞 run 의 값** (run44 사진 · sast/44) 이다. 새로 찍은 사진이 아니다.
- **이번에 run42 를 정정했다** (2.10). [run42 해설](../02_sprint/run42-continue-detail-해설.md) 맨 위에 "고쳐 읽기 (run111)" 한 줄을 붙였다. 본문은 그대로 두었다.

---

### 이번 글의 새 말: 본문에서 처음 나올 때 한 줄씩 푼다

- **`XSAVES`** → 2.6
- **XState 성분** → 2.7
- **S0** (이 글에서 쓰는 이름) → 2.4
- 이미 푼 말은 링크만 건다:
  - **CET 그림자 스택** (run 문서는 "섀도 스택" 이라고도 쓴다. 같은 것이다) = **경비실 장부** → [run29 해설 2.9](../02_sprint/run29-continue-start-해설.md#29-cet-그림자-스택--미리-올려-둔-한-칸과-맞춰-보기)
  - **예약 줄** (`KePopulateContinuationContext` 가 장부에 미리 적는 "본업 자리" 한 줄) → 같은 곳
  - **MSR** (CPU 안 번호 붙은 특수 칸), MSR `0x6a7` = 장부의 **책갈피**, **`WRUSSQ`** (그림자 스택에 한 칸 직접 적기), **`XRSTORS`** (별지를 통째로 CPU 에 되돌려 끼우기) → [run42 해설 2.4](../02_sprint/run42-continue-detail-해설.md)
  - **XState** = 업무 시작 메모 뒤 **별지**, **CET_U 칸** = 별지의 **"책갈피 위치" 칸**, 사물함 메모와 임시 메모 → [run42 해설 2.3](../02_sprint/run42-continue-detail-해설.md)
  - **XSAVE 영역** (스레드가 쉴 때 CPU 확장 상태를 넣어 두는 자리 = **개인 짐 상자**) → [run52 해설 2.5](run52-kthread-create-해설.md) "새 말: `StateSaveArea`"
  - **`.pdata`** (함수 범위 표 = 책의 **목차**), `UNW_FLAG_CHAININFO` ("나는 떨어져 나온 조각, 부모는 저기"), **조각** → [run33 해설 2.10](../02_sprint/run33-first-resume-해설.md)
  - **enclave (엔클레이브)** = 일반 프로그램이 못 들여다보는 **잠긴 방** → [run106 해설 2.6](run106-snap-map-small-해설.md)
  - **IFEO** = 가게 이름별 특별 지시 → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 3.4
  - `NtGlobalFlag2` = 가게 **규칙판** → [run110 해설 2.6](run110-tls-lock-small-해설.md)

### 이번 바퀴에서 조심할 곳 셋

1. **이번 사슬은 test.exe 에서 안 돈다.** 이 VM 의 첫 스레드는 CET 스레드가 아니다 `[관찰]` run44. 이 글의 (a) · (b) 는 모두 "**CET 스레드였다면** 이렇게 된다" 는 코드 읽기다. 그래서 **그림이 안 바뀌었다** (2.11).
2. **요청서 가정 셋이 틀렸다.** 결과서가 먼저 바로잡았다 (2.3). 특히 run42 가 이름을 댄 `KeContextFromKframes` 는 시동 길에 없다.
3. **`XSAVES` · `XRSTORS` 가 MSR `0x6a7` 을 읽고 쓴다는 부분만 `[추측: CPU 설명서]` 이다.** 코드에는 "명령을 실행한다" 까지만 보인다. 명령이 CPU 안에서 무엇을 하는지는 코드 밖 지식이다. 나머지는 `[정적]` 이다.

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 스레드 = **직원**, CONTEXT = **업무 시작 메모**, `ContextFlags` = 메모 맨 위 **체크박스**
- 유저 스택에 맡긴 원래 메모 = **사물함 메모** (run16), 커널 버퍼에 먼저 받는 메모 = **임시 메모** (run42)
- CET 그림자 스택 = **경비실 장부** (돌아갈 주소를 몰래 한 벌 더 적어 두는 장부. 아무나 못 고친다)
- 유저 SSP (MSR `0x6a7`) = 장부에 꽂힌 **책갈피** ("지금 맨 위 줄은 여기")
- XState = 메모 뒤 **별지**, CET_U 칸 = 별지의 **"책갈피 위치" 칸**
- `KePopulateContinuationContext` = 장부에 **예약 줄** ("다음 목적지 = 본업 자리") 한 줄 적기
- **이번에 더하는 비유**:
  - `XSAVES` = 지금 CPU 상태를 별지에 **사진 찍듯 한 번에 베끼기**
  - `PspSetupUserShadowStack` = 새 직원에게 **새 장부를 걸어 주고** "여기서 시작" 자리를 처음 메모 별지에 적는 담당
- run 문서는 그림자 스택을 "계단 옆 **비상 메모판**" 으로 비유했다. 이 글은 앞 해설들과 맞추려고 **경비실 장부**로 부른다. 그림은 같다.

### 한 줄로 먼저: 무엇을 얻었나

| 질문 | 답 | 태그 |
|---|---|---|
| (a) 사물함 메모 별지의 "책갈피 위치" 칸 (CET_U SSP) 값은 어디서 오나? | 시동 스레드가 커널에 있는 그 순간 **`PspGetContext +0x33d RtlXSaveS` 의 `XSAVES`** 가 담은 값 **S0**. `RtlCopyContext` 는 그 칸을 **바이트 그대로 복사만** 한다. S0 의 처음 값은 스레드 만들 때 **`PspSetupUserShadowStack`** 이 넣는다 | `[정적]` (MSR 을 오가는 부분만 `[추측: CPU]`) |
| 그래서 칸 값은 예약 줄 적기 **전**인가 후인가? | **전** (S0). 첫 유저 SSP 는 S0 − 8 | `[정적, 순서]` |
| (b) `PspGetSetContextInternal` 이 run42 의 "5번째 칸" 을 쓰나? | **X (안 쓴다).** 그 칸은 여기서도 죽은 값이다 | `[정적]` |
| (c) 조각 `FUN_1406f9e12` 는 어느 함수 것인가? | **`PopFxIdleDevicesFromSx` 꼬리.** run42 가 본 MSR 줄 (`nt+0x6f9f06`) 은 다른 조각 = **`RtlCreateEnclaveReturnFrame`** (엔클레이브용) | `[정적]` (run42 정정) |
| test.exe 에선? | 이 사슬 전체가 **안 돈다.** 첫 스레드가 비-CET (`0x10001b`) | `[관찰]` run44 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 질문은 **세 곳**에 걸쳐 있다. 스레드를 만들 때 (2.1), 출발할 때 (2.3), 로더 끝 `NtContinue` (2.10) 이다. 세 곳을 **한 칸 (별지의 "책갈피 위치")** 이 지나간다.

```
cmd.exe 의 부탁 (CreateProcess)
 └─ 커널: 프로세스·스레드 만들기 (2.1)
      PspAllocateThread
        (CET 면) PspSetupUserShadowStack — 새 장부 걸기, 처음 메모 별지에 S0     ← (a) 처음 값
        KeInitThread → KiInitializeContextThread — 별지 칸을 짐 상자로 옮김       ← (a) 옮기기
 └─ 커널: test.exe 첫 스레드 출발 (2.3)
      PspInitializeThunkContext
        PspGetContextThreadInternal → … → PspGetContext — 임시 메모에 XSAVES     ← (a) 사진 찍기
        RtlCopyContext — 임시 메모 → 사물함 메모 (별지 칸 그대로)                ← (a) 복사
        (CET 면) KePopulateContinuationContext — 예약 줄, 책갈피 S0 → S0−8
 └─ ntdll: 로더 (오리엔테이션실, 2.4 ~ 2.9)
 └─ 로더 끝: NtContinue(사물함 메모) (2.10)
      XRSTORS — 별지 칸 값으로 책갈피를 되돌림 (run42)
 └─ BaseThreadInitThunk → (CRT) → main

 곁가지 (지도 밖): SetThreadContext 쪽 PspGetSetContextInternal          ← (b)
                  엔클레이브 · 전원 관리 쪽 조각                           ← (c)
```

### 무엇이 궁금했나

- **(a)** CET 스레드라면 사물함 메모의 체크박스는 `0x10005b` 이고, 별지에 "책갈피 위치" 칸이 있다. 그 칸의 값은 **누가 채우나**? 그리고 `RtlCopyContext` 는 그 칸을 **어떻게 다루나**?
  - run42 에서 남은 짐작: 그 값이 예약 줄 적기 **전** 책갈피라면, `NtContinue` 뒤 책갈피는 `RtlUserThreadStart` 칸 **위** 자리로 되돌아간다.
- **(b)** run42 에서 "경비가 +8 해서 적은 메모지" (5번째 칸) 는 `NtContinue` 길에선 버려졌다. `SetThreadContext` 쪽 `PspGetSetContextInternal` 은 그 칸을 **쓰나**?
- **(c)** run42 가 커널 전체를 바이트로 훑다가 찾은 이름 없는 조각 `FUN_1406f9e12 +0xf4` (`WRUSSQ` + `WRMSR 0x6a7`) 는 **어느 함수 것인가**?

### 바퀴 전에 알던 것

- `[정적]` (run42) 사물함 메모 체크박스 = `0x10001b`, CET 스레드면 `0x10005b` (`0x40` = 별지 있음).
- `[정적]` (run42) 별지 칸은 `RtlCopyContext` (`+0x280`) 가 임시 메모에서 복사한다. 이 복사는 예약 줄 적기 (`KePopulateContinuationContext`, `+0x295`) 보다 **앞**이다.
- `[추측]` (run42) 그러니 칸 값이 "예약 줄 적기 전" SSP 라면, `NtContinue` 뒤 SSP 는 `RtlUserThreadStart` 칸 위 자리가 된다. 칸을 누가 채우는지는 **안 읽었다**.
- `[정적]` (run42) 유저 SSP 를 바꿀 수 있는 곳은 `KxContextToKframes +0x211 XRSTORS` 하나다. 그게 MSR 로 간다는 건 `[추측: CPU 설명서]`.
- `[관찰]` (run44 P1) 이 VM 의 맡긴 `_CONTEXT` `ContextFlags` = **`0x10001b`**. XState `0x40` 이 없다.

---

## 2. 찾은 것

### 2.1 한눈에: 한 칸이 지나가는 길

run 문서 "호출 관계" 를 줄인 것이다. 숫자는 **ret 오프셋**이다 (콜스택에 찍히는 자리).

```
[스레드 만들 때 — 부모 cmd.exe 스레드, 2.1]  (CET 스레드만)
PspAllocateThread
 ├─ +0xb65  PspSetupUserShadowStack                      ← 먼저 (+0x601 JNZ)
 │           ├─ +0xfb   PspReserveAndCommitUserShadowStack   +0x136 SSP = 시작 + 크기 − 0x1000
 │           └─ +0x126  RtlLocateExtendedFeature(…, 0xb)     → +0x140 [칸+8] = S0 · +0x144 [칸] = 1
 └─ +0x6f9  KeInitThread ─ +0x301 KiInitializeContextThread
                            └─ +0x3e1 … +0x3e5  처음 메모 칸 16바이트 → 스레드 저장 영역

[첫 실행 — test.exe 스레드, 2.3]
PspInitializeThunkContext
 ├─ +0x16d  PspGetContextThreadInternal ─ … ─ +0x2f3 PspGetContext
 │                                             ├─ +0x309 KiGetAllocatedXSaveArea
 │                                             └─ +0x342 RtlXSaveS   +0x3d XSAVES → 칸 [+8] = S0
 ├─ +0x285  RtlCopyContext
 │           ├─ +0x121  RtlpCopyXStateChunk     성분 통째 복사 (CET_U 칸도 그대로)
 │           └─ +0x1b8  RtlpCopyKernelCetChunk  (커널 CET 칸 — 시동엔 안 탐)
 └─ +0x29a  KePopulateContinuationContext       RDMSR 0x6a7 → −8 → WRUSSQ → WRMSR  (S0 → S0−8)

[로더 끝 NtContinue, 2.10 — run42 그대로]
KxContextToKframes +0x211 XRSTORS   칸 값 S0 을 되돌림 [추측: MSR 로]
```

- 모두 `[정적]` 이다. 단 `XSAVES` / `XRSTORS` 가 MSR `0x6a7` 을 읽고 쓴다는 부분은 `[추측: CPU 설명서]`.
- `PspSetupUserShadowStack` 이 `KeInitThread` 보다 **먼저** 돈다는 것은 결과서에 없었다. flow-writer 가 `.asm` 으로 보탰다 `[정적]` (2.4).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 사진 찍는 줄

```
 │                                             └─ +0x342 RtlXSaveS   +0x3d XSAVES → 칸 [+8] = S0
```

- **누가**: `PspGetContext`. 출발 때 "지금 스레드 상태를 임시 메모에 받아 와" 하는 사슬의 맨 끝이다.
- **어디서**: call `+0x33d`, ret `+0x342`.
- **누구를**: `RtlXSaveS`. 그 안 `+0x3d` 에 `XSAVES [영역]` 한 줄이 있다.
- **무엇을 위해**: 임시 메모 별지에 **그 순간 CPU 상태를 통째로** 담으려고. 별지 안 "책갈피 위치" 칸도 이때 채워진다. 코드에 있는 것은 `[정적]` 이고, 그 칸에 MSR `0x6a7` 값이 들어간다는 것은 `[추측: CPU 설명서]` 다.

#### 한 줄 읽기 ②: 복사만 하는 줄

```
 │           ├─ +0x121  RtlpCopyXStateChunk     성분 통째 복사 (CET_U 칸도 그대로)
```

- **누가**: `RtlCopyContext`. 임시 메모를 사물함 메모로 베끼는 함수다 (run16 · run42).
- **어디서**: call `+0x11c`, ret `+0x121`. 앞의 `+0xdb TEST BPL,2` 가 "별지 비트가 있나" 를 본다.
- **누구를**: `RtlpCopyXStateChunk`.
- **무엇을 위해**: 별지 내용을 사물함 메모 별지로 **옮기려고**. 값을 새로 만들지도, 고치지도 않는다 `[정적]` (2.7).
- 요청서는 `+0x11c` · `+0x1b3` 을 `PspGetContextThreadInternal` 의 오프셋으로 적었다. 실제로는 **`RtlCopyContext` 의 오프셋**이다 (결과서가 바로잡음, 2.3).

#### 한 줄 읽기 ③: 조건 하나로 갈리는 줄

```
 ├─ +0xb65  PspSetupUserShadowStack                      ← 먼저 (+0x601 JNZ)
```

- **누가**: `PspAllocateThread`. 부모 cmd.exe 의 스레드가 새 직원 (test.exe 첫 스레드) 을 만드는 중이다.
- **어디서**: call `+0xb60`, ret `+0xb65`. 그 앞 `+0x5fa TEST [thread+0x74],0x100000` / `+0x601 JNZ +0xb49` 가 "이 직원이 CET 스레드인가" (스레드 `+0x74` bit 20) 를 본다.
- **누구를**: `PspSetupUserShadowStack` (rva `0x904838`).
- **무엇을 위해**: 새 그림자 스택을 잡고 처음 책갈피 자리 S0 을 처음 메모 별지에 적으려고 `[정적]`. CET 스레드가 아니면 이 줄로 **안 간다**.

### 2.3 먼저 바로잡기: 요청서 가정 셋이 틀렸다

결과서 "중요: 요청서 가정과 다른 점" 이다. 셋 다 `[정적]`.

1. **시동 길에 `KeContextFromKframes` 는 없다.**
   - 이 함수를 부르는 곳은 `KiDispatchException +0x13d` 와 `KiInitializeUserApc +0x1fb` **둘뿐**이다 (`_calls.tsv`). 예외 길과 유저 APC 길이다.
   - 시동 길은 `PspGetContextThreadInternal` → `PspGetSetContextSpecialApc` → `PspGetSetContextInternal` → **`PspGetContext`** 다. 임시 메모 별지 칸은 `PspGetContext` 가 채운다.
   - run42 해설 4절 표 (a) 줄이 `KeContextFromKframes` 를 후보로 적었는데, 그 후보가 빠진다.
2. **`+0x11c RtlpCopyXStateChunk` · `+0x1b3 RtlpCopyKernelCetChunk` 는 `RtlCopyContext` 의 오프셋이다.** 둘 다 값을 만들지 않고 복사만 한다 (2.7).
3. **`FUN_1406f9e12` 에는 `+0xf4` 에 `WRMSR` 이 없다.** 이 조각은 짧은 다른 함수 꼬리다. run42 가 본 줄은 그 뒤쪽 다른 조각에 있다 (2.10).

- 비유: 질문지에 "이 서류는 A 창구에서 만든다" 고 적혀 있었는데, 가 보니 A 창구는 다른 일 (예외 · 쪽지) 에만 쓰는 곳이었다. 실제 서류는 B 창구 (`PspGetContext`) 에서 나왔다.

### 2.4 (a) ① 처음 값 S0: 스레드를 만들 때 새 장부를 건다

- 새 말: **S0** = 이 글 (과 run 문서) 에서 쓰는 이름이다. "**새 그림자 스택의 처음 책갈피 자리**" 를 짧게 부르려고 붙인 이름이다. 공식 이름이 아니다.

#### 단계별로 `[정적]` (CET 스레드일 때만)

1. **CET 스레드인가 정하기.** `PspAllocateThread +0x5b9 ~ +0x5c0` 이 프로세스 `+0x754` bit 14 (`CetUserShadowStacks`) 를 본다. 켜져 있으면 `+0xb3f BTS [thread+0x74],0x14` 로 스레드 쪽 `CetUserShadowStack` 을 켠다.
2. **갈림.** `+0x5fa TEST` / `+0x601 JNZ +0xb49` → `+0xb60` `PspSetupUserShadowStack`.
3. **장부 잡기.** 그 안 `+0xf6` (ret `+0xfb`) `PspReserveAndCommitUserShadowStack` (rva `0x904a0c`) 이 메모리를 잡는다.
   - 안에서 `+0x93` `MmAllocateUserStack`, `+0xdd` · `+0x11d` `ZwAllocateVirtualMemory` 를 부른다 (이름만).
   - 돌려주는 SSP = **섀도 스택 시작 + 크기 − 0x1000** (`+0x12c LEA RCX,[R14-0x1000]` · `+0x133 ADD RCX,RBX` · `+0x136 MOV [RAX],RCX`).
   - `R14` 를 "크기" 로 읽은 것은 `[추측]` 이다 (`MmAllocateUserStack` 이 돌려준 칸이라서 그렇게 읽었다).
4. **처음 메모 별지에 적기.** 처음 메모 플래그가 `0x100040` 이면 (`+0x105 ~ +0x111`):
   - `+0x121` (ret `+0x126`) `RtlLocateExtendedFeature(문맥+0x4d0, 0xb)` 로 별지 안 CET_U 칸을 찾는다 (`0xb` = 11번 기능).
   - `+0x132 BTS` 로 별지 마스크 bit 11 을 켠다.
   - `+0x13c ~ +0x140 [칸+8] = SSP`, `+0x144 [칸] = 1`. 이 값이 **S0** 이다.
   - 섀도 스택 시작은 `+0x14f MOV [R14+0x40],RAX` 로 따로 저장하고, `+0x153 OR byte [R15],8`.

#### 순서: `KeInitThread` 보다 먼저 `[정적]` (flow-writer 보탬)

- 결과서는 `+0xb60 PspSetupUserShadowStack` 과 `+0x6f4 KeInitThread` 중 어느 쪽이 먼저인지 안 적었다.
- flow-writer 가 `.asm` 으로 따라가 보니:
  1. `+0x601 JNZ +0xb49` → `+0xb60` (ret `+0xb65`)
  2. `+0xb65 JMP +0x9eb` (`MOV R14D,EAX`, 돌려받은 상태값)
  3. `+0x9f2 JMP +0x60c` (`TEST R14D,R14D` / `+0x60f JS` 실패 길)
  4. 그 뒤에 `+0x6f4 KeInitThread`
- 그래서 다음 단계 (2.5) 에서 `KiInitializeContextThread` 가 읽는 별지 칸은 **이미 채워져 있다.** 앞뒤가 맞는다.

- 비유: 새 직원 등록 서류 (스레드) 를 만들 때, 경비실 담당이 먼저 **새 장부를 하나 걸고**, 처음 업무 메모 별지에 "책갈피는 여기서 시작 (S0)" 을 적는다. 그다음 직원 책상 (`KeInitThread`) 을 꾸린다.

### 2.5 (a) ② 별지 칸을 스레드 저장 영역으로 옮긴다

`[정적]` (CET 스레드일 때만):

- `PspAllocateThread +0x6f4` (ret `+0x6f9`) `KeInitThread` → `+0x2fc` (ret `+0x301`) `KiInitializeContextThread`.
- 그 안에서:
  - `+0x2a8` · `+0x2af` 가 스레드 저장 영역 머리에 bit 11 (CET_U) 을 켠다.
  - `+0x3a7 ~ +0x3b5` 처음 메모 플래그가 `0x100040` 이면,
  - `+0x3c6` (ret `+0x3cb`) `RtlLocateExtendedFeature` 로 처음 메모 칸을 찾고,
  - `+0x3dc` (ret `+0x3e1`) `RtlLocateSupervisorFeature` 로 저장 영역의 CET_U 자리를 찾아,
  - **`+0x3e1 MOVUPS` · `+0x3e5 MOVDQU`** 로 16바이트를 복사한다.
  - 또 `+0x233 ~ +0x25b` 가 스레드 `+0x250` (`NpxState`) 에 `0x800` 을 켠다.
- 직원이 **처음 일하러 나갈 때** (교대, run75): `SwapContext` 가 들어오는 스레드에 `XRSTORS [RSI+0x60]` 을 한다 (조각 `FUN_1406b686f +0x1b1` = `SwapContext +0x4e0`). 마스크에 `[RSI+0x250]` 이 들어간다 `[정적]`.
  - 그때 CET_U 칸 값이 MSR (책갈피) 로 올라간다는 것은 `[추측: CPU 설명서]`. 마스크 조건 전부는 안 읽었다 (4절, Q199 ②).

- 비유: 처음 메모 별지의 "책갈피 위치" 를 직원의 **개인 짐 상자** (관리사무소 쪽 보관함) 에 옮겨 둔다. 직원이 처음 교대해 들어올 때 짐 상자를 열면, 책갈피가 그 자리 (S0) 에 꽂힌다 (이 마지막 부분이 `[추측]`).

### 2.6 (a) ③ 출발 직전: 임시 메모에 `XSAVES` 로 사진을 찍는다

이제 test.exe 첫 스레드가 **자기 자신**의 상태를 임시 메모에 받는 장면이다 (run16 의 `PspInitializeThunkContext` 2번).

#### 사슬 `[정적]`

1. `PspInitializeThunkContext +0x168` (ret `+0x16d`) `PspGetContextThreadInternal(현재 스레드, 임시 버퍼, 0, 1, 0)`.
2. **자기 자신이라 바로 간다.** `PspGetContextThreadInternal +0xe9 CMP R13,RDI` / `+0xec JNZ` 가 안 뛴다. 그래서 쪽지 (APC) 없이 `+0x11e` (ret `+0x123`) `PspGetSetContextSpecialApc` 를 **바로** 부른다.
   - 비유: 다른 직원 메모를 받으려면 쪽지를 보내야 하지만, 자기 메모는 직접 베끼면 된다.
3. `PspGetSetContextSpecialApc +0x64` (ret `+0x69`) `PspGetSetContextInternal` → 읽기 갈래 `+0x2ee` (ret `+0x2f3`) `PspGetContext`.
4. **`PspGetContext` 의 별지 갈래.** 조건 둘:
   - 플래그 `0x100040` (`+0x171 ~ +0x17b`)
   - 유저에서 온 트랩 프레임 (`+0x2e6 TEST byte [트랩+0x170],1`)
5. `+0x304` (ret `+0x309`) `KiGetAllocatedXSaveArea`. 돌려준 값에 따라 둘로 갈린다:
   - **0 이면 (보통)**: 마스크 = (KUSD `+0x708` `EnabledUserVisibleSupervisorFeatures` | KUSD `+0x3d8` `EnabledFeatures`) & `~3` → **`+0x33d` (ret `+0x342`) `RtlXSaveS`**.
   - **0 이 아니면**: `+0x3f7` (ret `+0x3fc`) `RtlCopyXStateArea`. 스레드 `+0x198` (`_KTHREAD.XStateSave`) 사슬 끝 `[+0x28]` 에 저장돼 있던 영역을 복사한다. 이때 칸의 SSP 는 그 영역에 저장돼 있던 값이다.

#### 새 말: `XSAVES`

- **`XSAVES`** = CPU 의 확장 상태 묶음 (XState) 을 **한 번에 메모리에 담는** 명령으로 알려져 있다 `[추측: CPU]` (일반적으로 알려진 설명).
  - `XRSTORS` ([run42 해설 2.4](../02_sprint/run42-continue-detail-해설.md)) 의 **반대 방향**이다. `XRSTORS` 는 메모리 → CPU, `XSAVES` 는 CPU → 메모리.
  - 비유: 지금 CPU 상태를 별지에 **사진 찍듯 한 번에 베끼기**. 별지에 "책갈피 위치" 칸이 있으면, 그 순간 꽂혀 있는 책갈피 자리도 같이 찍힌다.

#### `RtlXSaveS` 안 `[정적]`

- `+0x20 OR [영역+0x208],bit63`
- `+0x29 XRSTORS [영역]` (RFBM 0)
- `+0x2c AND R8,[영역+0x208]`
- **`+0x3d XSAVES [영역]`**
- KUSD `ControlFlags` bit 1 이 꺼져 있으면 (압축 저장 안 씀) `+0x46` (ret `+0x4b`) `RtlXSave` 로 간다.

#### 그래서 무엇이 담기나

- `[정적]` 그 순간의 CPU 상태가 임시 메모 별지에 담긴다.
- `[추측: CPU 설명서]` CET_U 칸 `[+8]` = 그 순간 MSR `0x6a7` 값 = **S0** (2.5 에서 올라간 책갈피).
- 이 길에 **`RDMSR` 줄은 없다** `[정적]`. 책갈피를 "읽는" 명령이 따로 없고, 사진 한 장으로 같이 찍힌다는 뜻이다.

#### 덤: `KeContextFromKframes` 도 같은 두 갈래 `[정적]`

- 시동 길은 아니지만 (예외 · APC 길), 같은 모양이다.
- 스레드 `+0x198` 이 0 이면 `+0x207 ~ +0x225` 에서 `RtlXSaveS` 와 같은 일을 직접 풀어 쓴다 (`+0x225 XSAVES`). 0 이 아니면 `+0x562 ~ +0x5a1` `RtlCopyXStateArea`.

### 2.7 (a) ④ 사물함으로 옮길 때는 복사만 한다

#### `RtlpCopyXStateChunk`: 값을 안 만든다 `[정적]`

- 이 함수 안에는 `RDMSR` · `WRMSR` · `XSAVE*` · `XRSTOR*` 가 **하나도 없다** (grep 0줄).
- 하는 일, 순서대로:
  1. 두 쪽 별지 길이를 읽는다 (`+0x18` · `+0x39`). 둘 다 `0x40` 이상이어야 한다 (`+0x43 ~ +0x4e`). 아니면 `0x80000005`.
  2. 두 쪽 별지 영역 주소를 구한다 (`+0x54` · `+0x62`).
  3. 원본 마스크 `& ~3` (`+0x83` · `+0x89`). KUSD 의 켜진 기능에 없는 비트면 `0xc000000d`.
  4. 성분 번호 **2 부터 63 까지** 돌며 (`+0xa2`, `+0x1b1 ~ +0x20a`), 원본 · 대상 칸 자리를 KUSD 배치 표 (`0xfffff780000003f0` · `…3f4` · `…604`) 로 계산한다.
  5. **`+0x277` (ret `+0x27c`) 메모리 복사 함수**로 성분마다 통째로 복사하고, `+0x28d BTR` 로 비트를 지운다.
- 새 말: **XState 성분** = 별지 안을 기능별로 나눈 **칸 하나하나**. 번호가 붙어 있고, CET_U 는 **11번** 성분이다 (마스크 bit 11 = `0x800`).
  - 비유: 별지가 여러 칸으로 된 양식이고, 복사기는 칸 번호 순서대로 **한 칸씩 그대로** 옮겨 적는다. "책갈피 위치" 칸 (11번) 도 다른 칸과 똑같이 옮긴다.
- 그래서 사물함 메모 별지 칸 = 임시 메모 별지 칸 = **S0** `[정적]`.
- `+0x277` 의 대상 이름: Ghidra 는 `alias:RtlCopyMemory+3` 으로 적었다. PDB 공개 이름은 같은 주소 (`0x6c3640`) 에 `memcpy` · `memmove` 둘이다. 그래서 run 문서는 결과서의 "`RtlCopyMemory`" 대신 **"메모리 복사 함수"** 로 적었다. 이 글도 run 문서를 따른다.

#### `RtlpCopyKernelCetChunk`: 다른 칸이다 `[정적]`

- 이름이 비슷해서 헷갈리기 쉽다. 이건 **유저 CET_U 칸이 아니라 커널 CET (`KernelCet`) 칸** 복사기다.
  - `CONTEXT_EX` 의 `KernelCet` 칸 (`+0x18` 오프셋, `+0x1c` 길이) 이 양쪽 다 `0x18` 이상일 때 24바이트를 복사한다 (`+0x22 ~ +0x32`). 모자라면 `+0x3a` `0x80000005`.
- **시동 문맥에선 안 불린다.**
  - `RtlCopyContext +0xe1 TEST BPL,4` 의 bit 2 는 `RtlpValidateContextFlags2 +0xa1 ~ +0xb9` 에서 **`플래그 & 0x100080 == 0x100080`** 이고 `KiKernelCetEnabled` 일 때만 켜진다.
  - 시동 문맥은 `0x10005b & 0x100080 = 0x100000` 이라 조건이 안 맞는다.
- 비유: 같은 복사기 안에 "**경비실 장부 (유저) 칸**" 과 "**관리사무소 내부 장부 (커널) 칸**" 을 옮기는 손이 따로 있다. 사물함 메모에는 내부 장부 칸 표시가 없어서 둘째 손은 안 움직인다.

### 2.8 (a) ⑤ 순서가 답을 정한다: S0 이냐 S0 − 8 이냐

이제 run42 의 `[추측]` 을 닫을 수 있다. 단계별로 본다.

1. `[정적]` 임시 메모 별지 칸 = S0 (2.6, `PspInitializeThunkContext +0x168`).
2. `[정적]` 사물함 메모 별지 칸 = 임시 메모 칸 그대로 = S0 (2.7, `+0x280 RtlCopyContext`).
3. `[정적]` 그다음 `+0x295` (ret `+0x29a`) `KePopulateContinuationContext(원래 Rip)`:
   - `+0xc` 스레드 bit 20 이면
   - `+0x17 ~ +0x1c RDMSR 0x6a7` → `+0x25 SUB RAX,8` → `+0x30 WRUSSQ [RAX], Rip` → `+0x36 WRMSR`.
   - 곧 예약 줄 ("본업 자리") 한 칸을 적고 책갈피를 **S0 → S0 − 8** 로 옮긴다.
4. `[정적, 순서]` 3번은 2번 **다음**이다. 그래서:
   - 사물함 메모 칸 = **예약 줄 적기 전 S0**
   - 유저 코드가 처음 도는 SSP = **S0 − 8**
5. `[추측: XRSTORS 가 MSR 로 간다]` 로더 끝 `NtContinue` 에서 `KxContextToKframes +0x211 XRSTORS [R8]` (조건 `+0x193 ~ +0x1a5`) 가 칸 값 S0 을 책갈피로 올린다.
   - 그러면 `NtContinue` 뒤 책갈피는 S0 = `RtlUserThreadStart` 칸 **위** 자리다 (flow.md 2.10).
6. `[정적]` 앞뒤 확인: sast/37 의 `KiVerifyContextXStateCetUEnabled` 는 "칸 SSP ≥ 현재 SSP" 를 본다. S0 ≥ S0 − 8 이라 **통과한다.** 어긋나지 않는다.

- 비유로 잇자면:
  - 출근 직전 관리사무소가 직원 메모를 사진 찍어 (`XSAVES`) 사물함에 그대로 넣는다. 이때 별지에는 "책갈피 = 시작 자리 (S0)" 가 찍혀 있다.
  - 그 **뒤에** 경비실 장부에 예약 줄을 적고 책갈피를 한 칸 옮긴다 (S0 − 8).
  - 오리엔테이션이 끝나고 사물함 메모를 되돌려 끼우면 (`XRSTORS`), 책갈피는 사진 속 자리 (S0) 로 돌아간다. 예약 줄은 경비가 맞춰 본 뒤 **지나간 줄**이 된다.

### 2.9 (b) `PspGetSetContextInternal` 은 5번째 칸을 안 쓴다

#### "5번째 칸" 이 뭐였나

- run42 에서 경비 `KiVerifyContextIpForUserCet` 가 장부를 맞춰 본 뒤 "+8 한 새 SSP" 를 적는 **메모지** 다. 그 함수의 5번째 인자가 그 메모지 주소다.
- 결과서는 이것을 `KeVerifyContextIpForUserCet` 의 4번째 인자 (`R9`, `+0x5d MOV [RSP+0x20],R9`) 로 읽었다. `PspGetSetContextInternal` 쪽에선 지역 칸 `[RBP-0x78]` 이다.
  - `PspGetSetContextInternal` **자신**의 인자는 3개 (`RCX` · `RDX` · `R8`) 뿐이다. 헷갈리지 않게: "5번째" 는 이 함수의 인자 번호가 아니다.

#### 칸의 일생 `[정적]` (쓰기 갈래 = `SetThreadContext` 쪽)

1. `+0x37d CMP byte [R14+0x58],1` → 쓰기 갈래.
2. `+0x38f MOV [RBP-0x78],0` → 칸을 0 으로.
3. `+0x397` (ret `+0x39c`) `KeVerifyContextXStateCetU(스레드, 문맥, &칸)`.
   - 그 안 **`+0x242 ~ +0x24b RDMSR 0x6a7 → [RBX]`**: 지금 책갈피를 칸에 적는다.
   - 문맥에 CET_U 칸이 없으면 `+0x2c0 ~ +0x2d4` 가 문맥에 칸을 만들고 `[칸] = 1`, `[칸+8] = 지금 SSP`.
4. `+0x3a4 TEST [R14+0x59],4` → `+0x564` → `+0x568 LEA R9,[RBP-0x78]` → `+0x587` (ret `+0x58c`) `KeVerifyContextIpForUserCet`. 안쪽이 맞으면 칸을 SSP + 8 로 고친다 (run42).
5. **`+0x58c` 뒤에 `[RBP-0x78]` 을 읽는 줄이 없다.**
   - 이 칸이 나오는 줄은 전부: `+0x6c` (인자 저장) · `+0x2c0` (칸 쓰기 전 읽기) · `+0x388` · `+0x568` (주소만 넘김) · `+0x38f` (0 쓰기) · `+0x602` (`VslGetSetSecureContext` 갈래. `+0xa4 JNZ +0x5e6` 로 가는 별개 길이라 칸을 쓴 뒤에 오지 않는다).
   - 이어지는 `PspSetContext` (`+0x3ea`) · 조각 `PspSetContextState` 도 이 칸을 인자로 안 받는다 (`+0x3e0 ~ +0x3e7`).
- 다른 갈래: `KeVerifyContextRecord` (`+0x552`) 는 `R8 = R9 = 0` 으로 불러서 (`+0x549` · `+0x54f XOR`) 안쪽 검사를 안 탄다 (`KeVerifyContextRecord +0xa8 TEST R14,R14` / `+0xab JZ`).
- 시동 길은 읽기 갈래라 이 칸을 아예 안 만진다.

#### 그래서

- `[정적]` run42 의 `NtContinue` 길과 **같은 꼴**이다. 경비가 +8 해서 적은 메모지는 **어느 길에서도 버려진다** (죽은 값).
- 비유: 경비가 "다음 줄은 여기" 라고 메모지에 적지만, 그 메모지를 들고 책갈피를 옮기러 가는 사람은 창구 어디에도 없다.

### 2.10 (c) 조각 `FUN_1406f9e12`: 목차를 보니 두 개였다 (run42 정정)

#### run42 가 본 것

- run42 는 커널 전체를 바이트로 훑어 `MOV ECX,0x6a7` 뒤 `WRMSR` 이 있는 곳을 찾았다.
- 그중 하나를 "이름 없는 조각 `FUN_1406f9e12 +0xf4` (`nt+0x6f9f06`), `RDMSR` → −8 → `WRUSSQ` → `WRMSR`" 로 적었다. run42 **문서** 표에는 "`KeUserModeCallback` 쪽 `[추측]`" 이라고 적혀 있었다.

#### 이번에 본 것 `[정적]`

1. `pdb_aliases.py --where ntoskrnl.exe 0x6f9e12` · `0x6f9f06` → 둘 다 "PDB 공개 심볼에 없음". 공개 PDB 는 조각에 이름을 안 준다.
2. 그래서 **목차 (`.pdata`)** 를 읽었다 (스크래치 `pefile` 스크립트, 커밋 안 함):

| `.pdata` 항목 | 크기 | 부모 (`UNW_FLAG_CHAININFO`) | 안에 있는 것 |
|---|---|---|---|
| `0x6f9e12 ~ 0x6f9e39` (= `FUN_1406f9e12`) | 0x27바이트 | **`PopFxIdleDevicesFromSx`** (rva `0x4b580c`) | `KeAbPostRelease` · `KeLeaveCriticalRegion` 부르고 `RET`. **`RDMSR`/`WRMSR` 없음** |
| `0x6f9e98 ~ 0x6f9f31` (`nt+0x6f9f06` 이 여기) | 0x99바이트 | **`RtlCreateEnclaveReturnFrame`** (rva `0x4b5f88`) | `RDMSR` → −8 → `WRUSSQ` → `WRMSR 0x6a7` |

3. **왜 헷갈렸나.** Ghidra 가 `0x6f9f06` 을 앞쪽 가까운 이름 `FUN_1406f9e12` 에서 **바이트 거리만 세서** 보여 줬다 (`0x6f9e12 + 0xf4 = 0x6f9f06`). 그 사이에 다른 `.pdata` 항목이 끼어 있었다.
   - 비유: 목차를 안 보고 "3장 제목에서 40쪽 뒤" 라고 적었는데, 그 사이에 4장이 시작돼 있었다. 실제로는 **4장 안의 쪽**이다.
   - run33 은 반대 경우였다. "떨어진 조각" 이라 여긴 곳이 실은 한 함수 한가운데였다. 이번엔 "한 조각 안" 이라 여긴 곳이 **다른 함수의 조각**이었다. 어느 쪽이든 **목차를 먼저 봐야** 안다.

#### 두 부모는 무엇을 하나 `[정적]`

- `PopFxIdleDevicesFromSx` 쪽: `+0xc2 JZ` · `+0xd1 JMP` 가 조각 `FUN_1406f9e12` 로 뛴다. 꼬리 정리 (`KeAbPostRelease` · `KeLeaveCriticalRegion` · `RET`) 뿐이다. 결과서는 이 쪽을 **전원 관리** 쪽으로 분류했다.
- `RtlCreateEnclaveReturnFrame` 쪽:
  - 본체 `+0x1f JNZ` (`+0x18 CMP word [RCX+0x170],DI`, DI = `0x33`, 트랩 프레임 `SegCs`) 와 `+0x2c JNZ` (`+0x25 CMP [RCX+0x168],R8`, 트랩 `Rip` 가 인자와 다름) 가 조각 (= 본체 `+0x243f10`) 으로 뛴다.
  - 조각은 트랩 `Rsp` − 0x50 에 유저 스택 프레임을 만들고, 트랩 `Rsp` 를 그 값으로 바꾼다 (`+0x243f10 ~ +0x243f69`).
  - 스택 인자 `[RSP+0x78]` 이 0 이 아니면 (`+0x243f77 CMP` · `+0x243f7c JZ`): **`+0x243f7e MOV ECX,0x6a7` · `+0x243f83 RDMSR` · `+0x243f8c SUB RAX,8` · `+0x243f90 WRUSSQ [RAX],R9`** (R9 = 트랩 `Rip`) **· `+0x243f9d WRMSR`**.
  - `+0x243f9f JMP` 로 본체 `+0x3c` 로 돌아간다.
  - 곧 **잠긴 방 (엔클레이브) 에서 돌아올 때 장부에 "돌아올 자리" 한 칸을 적는 일**이다. `KePopulateContinuationContext` 와 같은 모양이다.
  - `RtlCreateEnclaveReturnFrame` 을 직접 `CALL` 하는 줄은 `_calls.tsv` 에 없다 (간접).

#### 정정: 전과 후

| | run42 | run111 |
|---|---|---|
| 그 MSR 줄의 주인 | 이름 없는 조각 `FUN_1406f9e12 +0xf4`, "`KeUserModeCallback` 쪽" `[추측]` | **`RtlCreateEnclaveReturnFrame +0x243f7e`** (엔클레이브용) `[정적]` |
| `FUN_1406f9e12` 자체 | (위와 같은 것으로 봄) | `PopFxIdleDevicesFromSx` 꼬리, MSR 줄 없음 `[정적]` |
| `NtContinue` 길인가 | 아니다 | **아니다 (그대로 맞음)** |

- 그러니 run42 의 결론 "`NtContinue` 길엔 `WRMSR 0x6a7` 이 없다" 는 **그대로**다. 틀린 것은 그 줄에 붙인 **이름표**뿐이다.
- run42 해설은 표에 이 줄을 따로 넣지 않았고 4절 표에만 "어느 함수 조각인가" 로 남겼다. 그래서 해설 쪽은 4절 (c) 줄과 2.4 "시동 사물함 메모에 대 보면" 6번을 고쳐 읽으면 된다. 맨 위 "고쳐 읽기 (run111)" 에 적었다.

### 2.11 test.exe 에서는: 이 사슬 전체가 안 돈다

#### 근거 (새로 본 것 아님, 앞 run 값)

- `[관찰]` run44 P1 맡긴 `_CONTEXT` `ContextFlags` = **`0x10001b`** (XState `0x40` 없음, flow.md 2.3).
- `[관찰]` 짐 지도 3.14 `_KTHREAD.MiscFlags.CetUserShadowStack` (비트 20) 사진 `끝까지 0` (K2 ~ P2).
- `[관찰]` 프로세스 `MitigationFlags2` K1 `0x40000000` (비트 14 `CetUserShadowStacks` 없음, sast/44).

#### 그래서

- 2.4 의 갈림 (`+0x601 JNZ`) 에서 test.exe 첫 스레드는 **안 간다**. 새 장부가 없고, S0 도 없다.
- 사물함 메모에 별지가 없다. `XSAVES` 갈래도 `RtlpCopyXStateChunk` 도 안 탄다.
- `NtContinue` 뒤 책갈피는 **MSR 그대로**다 (run42 해설 2.4 "정리" 의 (가) 경우).
- 이것이 **그림이 안 바뀐 이유**다. 이번 길은 지도의 굵은 화살표가 아니라 "CET 였다면" 곁가지다.

#### PE_emulator 쪽 (결과서 "지금 우리 맞나")

- `../PE_emulator` 코드는 flow-writer 가 직접 안 읽었다. 아래는 결과서가 읽은 줄이다. 고칠지는 사용자 결정이다.
- (a) **맞음 (비-CET 갈래)**: `Src/nt/Process.cpp:1980` 이 `ContextFlags 0x10001b` 로 고정, `Process.cpp:2006` 이 `KePopulateContinuationContext` 를 안 함, `Src/nt/Continue.cpp:17` 이 XState 꼬리를 안 읽음.
- (b) **맞음**: `Process.cpp:2006` 이 `PspSetContextThreadInternal → … → PspSetContext` 중간을 줄이고, `Continue.cpp:62` 가 `KiVerifyContextIpForUserCet` 을 안 함. 그 칸은 죽은 값이라 넣을 것이 없다.
- (c) **해당 없음**: 엔클레이브 · 전원 관리 쪽.

### 2.12 같은 바퀴의 짐 지도 일: `NtGlobalFlag2` 를 3.1 로 옮겼다

이번 CET 결과와는 **별개**다. [run110](run110-tls-lock-small-해설.md) 이 찾은 쓰는 줄을, 오케스트레이터 결정 (기본값) 으로 이번 바퀴에 옮겼다.

#### 무엇을 옮겼나 `[정적]` (근거 run110 · sast/107)

- PEB `+0x7c4` `NtGlobalFlag2` (가게 **규칙판**). 그동안 7.2 "모르는 칸 · `끝까지 0`" 에 있었다.
- 쓰는 줄이 둘 나와서 3.1 에 **2줄**로 들어갔다:
  - ① nt `MmCreatePeb +0x4ac · +0x4b2`: **늘** 쓴다. 커널 전역 `NtGlobalFlag2` (nt rva `0xfc3a00`) 의 사본이다.
  - ② ntdll `LdrpInitializeExecutionOptions +0x59c RtlQueryApplicationKeyOption → +0x5a9`: IFEO `GlobalFlag2` 값이 있을 때만 덮어쓴다 (**조건부, test.exe 는 안 탐**).
- 사진 값: 8장 모두 0 (`끝까지 0` 그대로). nt 가 0 을 썼고 (전역도 0 `[관찰 간접]`), ntdll 은 안 썼다 `[추측: test.exe IFEO 키 없음]`.
- 쓰는 곳이 nt · ntdll 두 군데라 **양쪽에 센다.** run70 `MinimumStackCommit` 과 같은 규칙이다. 그래서 아는 칸은 1 늘지만, nt 줄 수와 ntdll 줄 수가 각각 1씩 는다.
- 비유: 관리사무소 전체 규칙판 (커널 전역) 을 새 가게 규칙판 (PEB) 에 **베껴 걸고**, 가게 이름별 특별 지시 (IFEO) 가 있으면 가게 안에서 고쳐 쓴다. test.exe 는 특별 지시가 없어 베낀 그대로다.

#### 바뀐 숫자 (`baggage.md` 2장)

| 칸 | 전 | 후 |
|---|---|---|
| `_PEB` nt · ntdll · 모름 | 41 · 24 · 63 | **42 · 25 · 62** |
| 유저 구조체 7개 합 nt · ntdll · 모름 | 117 · 135 · 195 | **118 · 136 · 194** |
| 구조체 21개 아는 칸 (정확) · 모름 | 738 (707) · 301 | **739 (708) · 300** |
| `끝까지 0` 인 모르는 칸 (구조체) | 127 | **126** |

- 그 밖: 4장 짝 1줄 새로 (커널 전역 `NtGlobalFlag2` ↔ PEB), 7.2 · 7.41 `(해결됨)`.

### 2.13 곁가지: 도구 메모 둘

- **Ghidra 가 `WRUSSQ` 를 `WRUSSD dword ptr` 로 보여 준다.** 바이트 (`66 4c 0f 38 f5`, REX.W) 로 보면 8바이트짜리 `WRUSSQ` 다 (`KePopulateContinuationContext +0x30`, `RtlCreateEnclaveReturnFrame +0x243f90`, `objdump` 확인). run56 의 `RDSSPQ` · `CLRSSBSY` 오독과 같은 종류다. 그림자 스택 명령 줄은 **바이트로 크기를 확인**한다.
- **Ghidra 가 다른 함수의 조각을 앞쪽 `FUN_` 이름 + 바이트 거리로 보여 줄 수 있다** (2.10). 조각 오프셋을 문서에 쓰기 전에 `.pdata` 체인 부모를 먼저 본다 (run33 방식).
- 둘 다 ghidra-headless-export 쪽 함정 목록에 넣자는 **하네스 제안**으로 올라갔다 (plan.md 결정 기록). 아직 하네스는 안 고쳤다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 절 | 전 | 후 |
|---|---|---|
| 기준 | ntoskrnl pdb 줄 근거에 run111 없음 | **run111** 추가. "Ghidra 가 함수로 안 잡은 곳" 줄에 `WRUSSQ` 오독 · `FUN_`+거리 함정 메모 |
| 2.1 `PspAllocateThread` · `KeInitThread` | CET 갈래 설명 없음 | `[정적]` CET 갈래 자리 (`+0xb60 PspSetupUserShadowStack`, `KeInitThread` 보다 먼저, S0 을 처음 메모 칸에) · `KiInitializeContextThread +0x3e5` 가 칸을 저장 영역으로. MSR 로 올라가는 건 `[추측: CPU 설명서]` |
| 2.3 `PspInitializeThunkContext` 2 · 6 · 7 | 임시 메모 · 복사 · 예약 줄 (run16 · run42) | 2: `[정적]` `PspGetContext +0x33d RtlXSaveS` (`XSAVES`) 사슬, 시동 길에 `KeContextFromKframes` 없음. 6: `[정적]` CET_U 칸은 복사만, `RtlpCopyKernelCetChunk` 는 시동엔 안 탐. 7: `[정적, 순서]` 칸 = S0, 첫 유저 SSP = S0 − 8 |
| 2.10 `KiContinuePreviousModeUser` 6 | `[추측]` "칸 값이 넣기 전 SSP 라면 … `RtlUserThreadStart` 칸 위" (출처 Q75, 실제 값 Q57) | 취소선 → **`[정적, 순서]`** 칸 값 = 넣기 전 S0. MSR 오가는 부분만 `[추측: CPU 설명서]`. 이 VM 은 칸이 없어 MSR 그대로. 실제 값은 동적 Q57 |
| 3장 "남의 스레드에 문맥 넣기" | 5번째 칸 언급 없음 | `[정적]` 쓰기 갈래의 CET 검사 칸은 죽은 값 (`+0x58c` 뒤 안 읽음) |
| 4장 2.10 Q75 줄 | (a) · (b) · (c) 정적 공백 | `(해결됨, 정적)` → run111. (a) 의 `KeContextFromKframes` 전제 정정, (c) "`KeUserModeCallback` 쪽" 정정 |
| 4장 새 줄 셋 | 없음 | CET 문맥 칸 실행 확인 1 ~ 5 · 정적 남은 것 ① ② · PE_emulator 쪽 |
| 5장 두 표 | — | run111 줄 |

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- CET 쪽: **칸 수 그대로.** 3.14 `CetUserShadowStack` 줄에 "켜지면 타는 길 (run111)" 메모, 4장 맡긴 문맥 짝 줄에 CET_U 칸 근거만.
  - 섀도 스택 자체 · 스레드 저장 영역 CET_U 자리 · 맡긴 문맥 별지 칸은 **칸 표 없는 짐**이다. CET 스레드일 때만 생기고, test.exe 에는 안 생긴다 (7.42).
- `NtGlobalFlag2`: 3.1 두 줄, 2장 숫자 바뀜 (2.12).
- 새 **7.42** (Q75 가 넘긴 질문 모음).

### 큰 그림에서 어떻게 이어지나

- **별지 "책갈피 위치" 칸의 일생이 처음부터 끝까지 이어졌다** (CET 스레드일 때):
  - 만들 때 S0 을 적고 (2.1) → 짐 상자로 옮기고 → 첫 교대 때 책갈피로 → 출발 때 사진 찍어 (`XSAVES`) 사물함으로 복사 → 예약 줄로 S0 − 8 → 로더 끝 `XRSTORS` 로 S0.
  - 코드로 이어진 고리는 `[정적]`, CPU 안에서 MSR 로 오가는 고리 (교대 · `XSAVES` · `XRSTORS`) 만 `[추측: CPU 설명서]` 다.
- run29 · run42 · run111 세 바퀴로 `NtContinue` 길 CET 정적 질문은 **다 닫혔다.** 남은 것은 "CET 스레드에서 정말 그 값인가" 를 보는 **동적 한 번** (Q57) 과 낮은 정적 둘 (Q199) 이다.
- 에뮬레이터 쪽으로는: 이 VM 이 비-CET 라 지금 구현 (CET 갈래 안 탐) 이 맞다. 나중에 CET 를 켠 환경을 흉내 낼 일이 생기면, 이번 사슬이 "어디서 무엇을 채우나" 의 지도가 된다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| CET 스레드에서 정말 S0 → S0 − 8 인가 (`nt+0x91097d` / `nt+0x910982`, `PspInitializeThunkContext +0x295` / `+0x29a` 앞뒤 `rdmsr 6a7`) | 정적은 순서까지만. 실제 값은 실행해야 보인다. 이 VM 은 비-CET 라 이 길이 안 돈다 | 동적 **Q57** (run111 몫 1, CET 환경에서만) |
| `XSAVES` 가 담은 칸 값이 그 순간 MSR 과 같은가 (`nt+0x910850`, `+0x168` 직전 `rdmsr 6a7` vs 임시 메모 `[칸+8]`) | `XSAVES` 가 MSR `0x6a7` 을 읽는다는 건 CPU 설명서 지식 `[추측]` | 동적 **Q57** (run111 몫 2) |
| `KiGetAllocatedXSaveArea` 가 정말 0 을 돌려주나 (`nt+0x9b83d9`, `PspGetContext +0x309` 의 `rax` · `dq @$thread+0x198 L1`) | 실행 때 값 | 동적 **Q57** (run111 몫 3) |
| 처음 값 S0 의 실제 값 (`nt+0x90497c` `PspSetupUserShadowStack +0x144` 직후 `[rax]` · `[rax+8]`, `nt+0x904b42` 의 `rcx`, `nt+0x3e9f55` `KiInitializeContextThread +0x3e5` 16바이트) | 실행 때 값 | 동적 **Q57** (run111 몫 4) |
| `PspReserveAndCommitUserShadowStack +0x12c` 의 `R14` 가 정말 "크기" 인가 | `MmAllocateUserStack` 이 돌려준 칸이라 그렇게 읽었을 뿐 `[추측]` | 번호 없음 (결과서가 따로 확인 거리로 올리지 않음) |
| WinDbg 가 두 조각을 어떤 이름으로 보여 주나 (`ln nt+0x6f9f06` · `ln nt+0x6f9e12`) | 공개 PDB 엔 조각 이름이 없다. 실행 없이 디버거에서 한 번이면 됨 | 동적 **Q57** (run111 몫 5) |
| CET 스레드의 처음 메모에 CET_U 칸이 **늘** 있게 누가 보장하나 (`PspAllocateThread` 조각 `+0x2caa12 RtlGetExtendedContextLength` · `+0x2caae1 RtlLocateExtendedFeature`). `KiInitializeContextThread +0x3e1` 은 `0x100040` 이 없으면 `R14 = 0` 인 채로 읽는 모양이라 앞에서 보장돼야 한다 | 이번엔 안 읽음 | 정적 **Q199 ①** (낮음, CET 환경에서만) |
| 첫 교대 때 CET_U 가 정말 MSR 로 올라가나: `SwapContext` `XRSTORS` 마스크 조건 전부 (`[RSI+0x250]` 의 `0x800`, `BT RBP,0x26`) | 마스크 조건을 다 읽지 않았다. 지금은 `[추측]` | 정적 **Q199 ②** (낮음) |
| `XSAVES` / `XRSTORS` 가 CPU 안에서 MSR `0x6a7` 을 읽고 쓰는 것 자체 | 코드에 안 보이는 CPU 설명서 지식 | 코드로는 못 닫는다. `[추측: CPU 설명서]` 그대로. 동적 Q57 몫 1 · 2 가 간접 확인 |
| `+0x277` 메모리 복사 함수의 이름 (`memcpy` / `memmove`) | 같은 주소에 PDB 이름이 둘 | 고르지 않음 (일은 같다: 통째 복사) |
| `RtlCreateEnclaveReturnFrame` 을 누가 부르나 | 직접 `CALL` 줄이 없다 (간접) | 번호 없음. 지도 밖 (엔클레이브) |

- 결과서 판단: Q199 ① · ② 가 필요 없으면 Q75 는 이번으로 닫아도 된다. plan.md 도 Q199 를 "필요 없으면 닫아도 됨" 으로 적었다.
- 덤 (plan.md 결정 기록, 번호 없음): flow-writer 가 baggage 7.1 "아는 칸 중 끝까지 0 이 구조체 102" 가 run49 뒤로 안 고쳐진 숫자로 보인다고 메모했다. 다시 세려면 짐 지도 바퀴 하나가 필요하고, 뒤로 미뤘다.

---

## 5. 더 읽기

- run 문서: [`run111-cet-context-small.md`](run111-cet-context-small.md): "정적 — 호출 지점" 표, "호출 관계" 트리, "(a) · (b) · (c)", "결과서와 다르게 적은 곳 · 보탠 것", "정정"
- 요청서 · 결과서: [`_workspace/sast/106_cet-context-small.md`](../../_workspace/sast/106_cet-context-small.md) · [`106_cet-context-small_result.md`](../../_workspace/sast/106_cet-context-small_result.md): "중요: 요청서 가정과 다른 점", 질문별 답, "동적분석으로 확인해야 할 것" 0 ~ 5, "다음 요청 후보"
- 흐름도: [`flow.md`](flow.md): 2.1 `PspAllocateThread` · `KeInitThread`, 2.3 `PspInitializeThunkContext` 2 · 6 · 7, 2.10 `KiContinuePreviousModeUser` 6, 3장 "남의 스레드에 문맥 넣기", 4장 Q75 줄 · run111 새 줄 셋
- 짐 지도: [`baggage.md`](baggage.md): 2장 합, 3.1 `NtGlobalFlag2` 두 줄, 3.14 `CetUserShadowStack`, 4장 짝, 7.2 · 7.41 · 7.42
- 앞 바퀴 짝:
  - [run42 해설](../02_sprint/run42-continue-detail-해설.md): 체크박스 `0x10005b` · 별지 · 버려지는 +8 (이번에 맨 위 "고쳐 읽기 (run111)" 를 붙임)
  - [run29 해설 2.9](../02_sprint/run29-continue-start-해설.md#29-cet-그림자-스택--미리-올려-둔-한-칸과-맞춰-보기): 경비실 장부 · 예약 줄 · 맞춰 보기
  - [run16 해설 2.6](../02_sprint/run16-thunk-context-해설.md#26-장면--바꿔-끼우기--pspinitializethunkcontext): 사물함 메모 · 바꿔 끼우기
  - [run33 해설 2.10](../02_sprint/run33-first-resume-해설.md): `.pdata` 목차로 조각 부모 찾기
  - [run110 해설 2.6](run110-tls-lock-small-해설.md): `NtGlobalFlag2` 출처
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md): Q75 (끝), Q57, Q199, 결정 기록 2026-11-16 "Q75 끝"

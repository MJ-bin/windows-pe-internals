# run105 ntdll-small — 해설

> 이 글은 [run105 문서](run105-ntdll-small.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q66 + Q67** 묶음 (`_workspace/plan.md`, 이번에 **둘 다 끝**)
  - **Q66** — 2.2 부모 쪽 작은 정적 둘: `0x7ffe0308` 칸 이름, `CsrClientCallServer` 가 csrss 답을 기다리나 (run34 가 남긴 것)
  - **Q67** — 2.4 ntdll 항목 주변 작은 정적 셋: (a) ntdll 예외 표 등록, (b) `+0x1625` 가 끼우는 항목 정체, (c) 핫패치 문맥 (run35 가 남긴 것)
  - 대기열에서 연달아 있는 작은 ntdll 정적이라 **한 요청 · 한 바퀴** 로 묶었다. 짐 지도 바퀴다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q43** (run105 몫, 핫패치 쪽), **Q19** (부모 쪽 csrss 알림)
    - 정적 → 새 **Q192 ① ~ ④** (② 는 다음 Q78g1 바퀴에서 닫힌다 — 여기선 이름만)
- 근거:
  - run 문서 [`run105-ntdll-small.md`](run105-ntdll-small.md) (가장 중요)
  - 요청서 [`_workspace/sast/99_ntdll-small.md`](../../_workspace/sast/99_ntdll-small.md) · 결과서 [`99_ntdll-small_result.md`](../../_workspace/sast/99_ntdll-small_result.md) (판정 **분석성공**)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림 1 · 2 의 상자 글 · 라벨만** 바뀜, 새 상자는 없음 (3절)
- 이번 바퀴는 **정적만** 이다. VM 은 안 돌렸다.
  - 값 대조는 run44 사진 로그 (`ms/logs/04_baggage-read_*.txt`) 를 **다시 읽은 것** 뿐이다.
  - 그래서 이 글의 `[관찰]` 은 전부 run44 사진에서 온다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **동기 요청** · **시간 제한** — 2.4
- **App Verifier** — 2.6
- **핫패치** (run17 해설에서 한 번 풀었다, 여기선 한 줄만 다시) — 2.7
- **핫패치 문맥 (ctx)** — 2.7

### 이번 바퀴에서 조심할 곳 넷

1. **질문 다섯이 서로 다른 자리에 있다.**
   - 둘 (Q66) 은 **부모 cmd.exe 스레드** 쪽이다. test.exe 가 아직 태어나기 전이다.
   - 셋 (Q67) 은 **test.exe 첫 스레드** 의 ntdll 로더 안이다.
   - 그래서 1절 그림에 ← 표시가 다섯 개 붙는다.
2. **test.exe 는 다섯 중 셋을 안 탄다.**
   - App Verifier 복제 항목 (b) — 안 탐.
   - 핫패치 로딩 (c) — 안 탐.
   - csrss 를 거치지 않는 옆 갈래들 (Q66 ②) — 부모가 안 탐.
   - "코드에 이런 길이 있다" 와 "test.exe 가 이 길을 탔다" 를 이 글에서 계속 나눠 쓴다.
3. **run 문서가 결과서보다 한 발 더 나간 곳이 있다.**
   - 결과서는 "test.exe 에서 핫패치가 켜졌는지는 **정적으로 못 정한다**" 고 했다.
   - run 문서는 run44 사진에 `LdrpIsHotPatchingEnabled` 값이 **이미 있다** 는 걸 찾아서, "**안 탔다**" 로 좁혔다 `[정적+관찰]`. 이 글은 run 문서를 따른다 (2.8).
4. **오프셋은 call 자리다.** ret 자리 (부른 함수가 돌아오는 곳) 는 run 문서 "정적 — 호출 지점" 표에 있다.
   - 단, run 문서 "호출 관계" 트리와 (b) 트리는 **ret 자리** 로 적혀 있다. 이 글의 트리도 그걸 그대로 옮겼다. 표와 트리 숫자가 조금씩 다르면 call / ret 차이다.

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소 **신청 창구**, ntdll = 가게 안 **관리실** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) "비유"
- ntdll 시스템 콜 스텁 = 관리실 **전화기**, `INT 0x2e` = 관리사무소의 **옛 쪽문** → [run34 해설 2.5](../02_sprint/run34-parent-leftovers-해설.md) · [run56 해설 2.8](run56-syscall-args-해설.md) (스텁 494곳)
- KUSD (`_KUSER_SHARED_DATA`) = 건물 로비의 **공용 게시판** → [run34 해설 2.5](../02_sprint/run34-parent-leftovers-해설.md)
- csrss = 관리사무소와 따로 있는 **인사팀 사무실**, `0x1001d` = "1번 창구 - `0x1d` 번 업무" → [run34 해설 2.4](../02_sprint/run34-parent-leftovers-해설.md)
  - run 문서는 같은 것을 "**주민센터에 전입 신고**" 로 적었다. **같은 것**이다.
- ALPC 포트 = 프로세스끼리 거는 **전화선 끝** → [run68 해설 2.4 "새 말 하나 — ALPC 포트"](run68-dll-init-rest-해설.md#새-말-하나--alpc-포트)
- 모듈 항목 = 건물 **명부 줄**, 역함수 표 = "이 번지는 어느 건물?" 을 찾는 **동네 지번 색인** → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md) · [run35 해설 2.5](../02_sprint/run35-ntdll-entry-해설.md)
- App Verifier 묶음 = **점검관이 온 날에만** 하는 일 → [run19 해설 2.5](../02_sprint/run19-proc-init-modules-해설.md) "묶음 하나씩"
- 핫패치 = 가게 문을 안 닫고 **선반 하나만 새것으로 갈아 끼우기** → [run17 해설 2.7](../02_sprint/run17-ldr-init-entry-해설.md#27-같은-모양이-한-번-더--ldrpinitializeinternal-의-핫패치-깃발)
- ntdll 전역 = 관리실 **칠판** → [run68 해설](run68-dll-init-rest-해설.md) "비유"
- 사진 P1a · P1b · P2 · K1 ~ K3 → [run44 해설 2.8](run44-baggage-snap-해설.md)
- `INT 0x29` = **빠른 실패** (그 자리에서 프로세스를 끝냄) → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) · [run35 해설](../02_sprint/run35-ntdll-entry-해설.md)

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) | 표시 |
|---|---|---|
| Q66 ① 스텁이 보는 `0x7ffe0308` | 공용 게시판 `+0x308` **`SystemCall`** (4B). 스텁은 bit 0 만 본다. 사진 8장 모두 0 → test.exe 의 syscall 은 **모두 `SYSCALL` 길** | `[정적+관찰]` |
| Q66 ② 부모가 csrss 답을 기다리나 | **기다린다.** 받는 칸 = 보낸 칸, 시간 제한 없음. 답은 메시지 `+0x34`. 실패면 `CreateProcess` 도 실패 | `[정적]` |
| Q67 (a) ntdll 예외 표 | `+0x415` 등록이 **먼저**, `InExceptionTable` 켜기 (`+0x119d` 안) 가 뒤 — 두 번 등록 막기 | `[정적]` |
| Q67 (b) `+0x1625` 의 항목 | exe 도 ntdll 도 아닌 **`AvrfpLoaderEntry` 의 복제**. App Verifier 만, test.exe 는 안 탐 | `[정적]` (안 탐은 `[정적+관찰]`) |
| Q67 (c) 핫패치 문맥 | `[ctx+0]` = 패치 ntdll 시작 주소. 그리고 **핫패치가 꺼져도** 전역 `LdrpNtdllHotPatchContext` 에 스택 주소가 써진다. test.exe 는 꺼짐 | `[정적]` · 사진 값 `[관찰]` · 꺼짐 `[정적+관찰]` |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 두 스레드에 걸쳐 있다.

```
(부모 cmd.exe 스레드 — 그림 1)
cmd.exe 의 부탁 (CreateProcess)
 └─ KERNELBASE!CreateProcessInternalW
     ├─ ntdll 스텁 → 커널: 프로세스·주소 공간·PEB·첫 스레드 만들기 (멈춘 채로)
     │     ↑ 스텁이 보는 0x7ffe0308 칸은 무엇인가            ← Q66 ①
     ├─ ntdll!CsrClientCallServer — csrss 에 새 가게 신고      ← Q66 ②
     │     ↑ 답을 기다리나?
     └─ NtResumeThread — 첫 스레드 풀어 주기

(test.exe 첫 스레드 — 그림 2)
ntdll!LdrInitializeThunk
 └─ LdrpInitializeInternal
     ├─ 핫패치 준비                                            ← Q67 (c)
     └─ _LdrpInitialize
         └─ LdrpInitializeProcess
             ├─ +0x415 역함수 표에 ntdll 올리기                 ← Q67 (a)
             ├─ … 명부 첫 줄 (ntdll), 둘째 줄 (exe) …
             ├─ +0x1625 App Verifier 일 때만 끼우는 항목          ← Q67 (b)
             └─ … DLL 들이기 · 전원 켜기 …
 → NtContinue 로 은퇴 → exe 진입점 → main
```

### 무엇이 궁금했나

다섯 가지다. 다 "앞 바퀴가 짐작으로 남긴 한 줄" 이다.

1. **Q66 ①** ntdll 전화기 (스텁) 가 걸기 전에 `0x7ffe0308` 의 bit 0 을 본다. 그 칸 이름은? 누가 쓰나? 실제 값은?
2. **Q66 ②** 부모가 csrss 에 신고서를 내고 **답을 기다리나**, 아니면 던지고 바로 가나? 답은 어디서 읽나? 실패를 돌려주나?
3. **Q67 (a)** `+0x415 RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)` 가 정말 ntdll 을 색인에 올리는 줄인가? 명부 줄의 `InExceptionTable` 표시보다 먼저인가 뒤인가?
4. **Q67 (b)** `+0x1625 LdrpInsertDataTableEntry` 가 명부에 끼우는 항목은 누구 것인가? (인자 `[RSP+0x60]`)
5. **Q67 (c)** `LdrpLoadPatchedNtdll` 이 `[ctx+0]` 에 무엇을 쓰나? `LdrpNtdllHotPatchContext` 와 어떻게 이어지나? test.exe 에서 타나?

### 바퀴 전에 알던 것

- 스텁 모양 `[정적]` (run34 · run56): `+0x8 TEST byte [0x7ffe0308],1` → 꺼지면 `+0x12 SYSCALL`, 켜지면 `+0x10 JNZ → +0x15 INT 0x2e`. ntdll 에 이런 스텁이 **494곳** (run56).
  - 칸 이름은 `SystemCall` 로 **보인다** `[추측]`. 어느 길인지는 실행 값이라 동적 (Q19).
- csrss 알림 `[정적]` (run14 · run34): `CreateProcessInternalW +0x466b` 가 `CsrClientCallServer(메시지, 0x1001d, 0x218)` 를 부른다. csrss 쪽은 `BaseSrvCreateProcess2` 가 명부에 등록한다.
  - "부모는 답을 받을 때까지 기다린다" 는 `[추측: 일반 지식]` 이었다. 기다리는 줄을 안 봤다.
- `+0x415` 는 run18 이 `[정적]` 으로 적었고, run60 이 역함수 표 칸 0 · 1 을 닫았다.
- `InExceptionTable` 미리 켜기 (`LdrpInitializeNtdllDataTableEntry +0x93`) 는 run35 `[정적]`. "왜 미리 켜나" 는 `[추측]` (이미 색인에 올라가 있어서로 보임).
- App Verifier 묶음은 `AvrfAppVerifierMode == 0` 이면 건너뛴다 `[정적]` (run19). 무엇을 복제하는지는 "verifier 공급자 모듈?" `[추측]`.
- 핫패치 준비 `[정적]` (run17): `LdrpIsHotPatchingEnabled` 가 켜져야 `LdrpLoadPatchedNtdll(ntdll 시작, &지역 칸)` 을 부른다. 켜졌는지는 동적 (Q43).
- 둘째 `LdrpInitializeNtdllDataTableEntry` (`+0x126a`) = "패치된 ntdll" 줄 `[정적]` (run35). 핫패치가 꺼진 보통 프로세스는 안 탄다 `[추측]`.

---

## 2. 찾은 것

### 2.1 한눈에 — 대표 줄만 (ret 자리)

run 문서 "호출 관계" 에서 대표만 옮겼다. 숫자는 **ret 자리** 다.

```
(부모 cmd.exe 스레드)
KERNELBASE!CreateProcessInternalW
 └─ +0x4672  ntdll!CsrClientCallServer(msg = [RSP+0xbe0], 0x1001d, 0x218)
              └─ +0xf2  NtAlpcSendWaitReceivePort(CsrPortHandle, 0x20000, msg, 0, msg, &0x3b8, 0, 0)   답 올 때까지
                    ┄┄▶ csrsrv!CsrApiRequestThread … 처리기 → +0x656 [msg+0x34] = status
              +0x135 EAX = [msg+0x34]
 +0x4677  [RSP+0xc14] (= msg+0x34) 음수 → BaseSetLastNTError → 실패 끝 (+0xb61)

(test.exe 첫 스레드)
LdrpInitializeInternal
 │  +0xa7  LdrpNtdllHotPatchContext = &ctx (지역 16B, 0)      ← 켜짐 · 꺼짐 상관없이
 ├─ +0x98  LdrpInitializeHotPatching → LdrpIsHotPatchingEnabled
 │  +0xb3  꺼짐 → 건너뜀                                       ← test.exe
 ├─ +0x5a  _LdrpInitialize
 │          └─ LdrpInitializeProcess
 │              ├─ +0x41a   RtlInsertInvertedFunctionTable(ntdll, SizeOfImage)   ← 먼저
 │              ├─ +0x11a2  LdrpInitializeNtdllDataTableEntry   (안에서 InExceptionTable 켬)
 │              │  +0x1568  AvrfAppVerifierMode == 0 → +0x1753                   ← test.exe
 │              └─ +0x162a  LdrpInsertDataTableEntry (AvrfpLoaderEntry 복제 항목)
 └─ +0x5e  LdrpNtdllHotPatchContext = 0
```

- 모두 `[정적]`. "← test.exe" 줄은 사진 값까지 합친 `[정적+관찰]` 이다.
- `┄┄▶` 는 **다른 프로세스 (csrss) 의 스레드** 로 넘어간다는 뜻이다. 그림 1 에서도 상자가 아니라 노란 메모로 둔다 (한 그림 = 한 스레드).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 부모가 csrss 에 전화 거는 줄

run 문서 호출 지점 표의 이 줄:

> `ntdll!CsrClientCallServer` | `+0xed` | `+0xf2` | `ntdll!NtAlpcSendWaitReceivePort` (syscall `0x8e`) | `CsrClientProcess ≠ 0` (클라이언트 길). Flags `0x20000`, 보내는 칸 = 받는 칸, 시간 제한 0

- **누가**: `ntdll!CsrClientCallServer` (rva `0xdbdd0`, 449B). 부모 cmd.exe 스레드 안의 ntdll 이다.
- **어디서**: `+0xed` 에서 부르고, 돌아오면 `+0xf2` 로 온다.
- **누구를**: `NtAlpcSendWaitReceivePort` — 이름 그대로 "보내고 (Send) · 기다리고 (Wait) · 받기 (Receive)" 를 한 번에 하는 syscall (번호 `0x8e`).
- **무엇을 위해**: csrss 에 "새 가게 생겼어요" 신고서를 내고, **같은 종이에 답을 받아 오려고**.
- **조건**: `CsrClientProcess ≠ 0` 일 때만 이 길로 온다. 0 이면 전화 대신 옆 갈래로 간다 (2.4 "갈래 셋").
- 비유: 관리실이 인사팀 사무실에 전화를 건다. 신고서를 읽어 주고, **수화기를 들고 답을 기다린다.**

#### 한 줄 읽기 ② — 호출이 아니라 "값이 바뀌는 순간" 줄

run 문서 (c) 의 이 줄:

> `+0xa7 MOV [LdrpNtdllHotPatchContext],RAX`

- **누가**: `ntdll!LdrpInitializeInternal` — test.exe 첫 스레드가 로더에 들어와 지나는 **중간 방** (run17).
- **어디서**: `+0xa7`. 바로 앞 `+0x9f LEA RAX,[RSP+0x30]` 이 RAX 에 **자기 스택의 16바이트 칸 주소** 를 담았다.
- **무엇을**: 그 주소를 ntdll 전역 (칠판) `LdrpNtdllHotPatchContext` (rva `0x1d2908`) 에 적는다.
  - 바로 뒤 `+0xae MOVUPS [RSP+0x30],XMM0` 가 그 16바이트를 0 으로 채운다.
- **무엇을 위해**: 핫패치 쪽 결과를 담을 **메모지** 를 만들고, 칠판에 "메모지는 저기" 라고 적어 두려고.
- 이 줄은 **조건 없이** 지난다 (왜 그런지는 2.7). 그래서 핫패치가 꺼진 test.exe 에서도 칠판에 주소가 적힌다.

#### 한 줄 읽기 ③ — 갈림길이 붙은 줄

> `ntdll!LdrpInitializeProcess` | `+0x1577` · `+0x1625` | `+0x157c` · `+0x162a` | `LdrpAllocateModuleEntry(0)` · `LdrpInsertDataTableEntry(RBX)` | `AvrfAppVerifierMode ≠ 0` (`+0x1568` · `+0x156f JZ → +0x1753`) — test.exe 안 탐

- 한 칸에 call 이 둘 들어 있다. 순서대로 읽는다: `+0x1577` 에서 빈 명부 줄을 만들고 (`RBX`), `+0x1625` 에서 그 줄을 명부에 끼운다.
- "조건 · 뜻" 칸이 **먼저** 다: `+0x1568` 에서 `AvrfAppVerifierMode` 를 보고, 0 이면 `+0x156f JZ` 로 `+0x1753` 까지 **통째로 건너뛴다.**
- 그래서 이 줄은 "test.exe 가 지나간 줄" 이 아니라 "**코드에 있는 길**" 이다 (2.6).

### 2.3 Q66 ① `0x7ffe0308` = 공용 게시판의 `SystemCall` 칸

#### 칸 이름 — PDB 타입으로 `[정적]`

- `_KUSER_SHARED_DATA` 타입 (크기 `0xa80`) 의 **`+0x308 SystemCall`**, `unsigned long` (4바이트).
  - ntdll PDB 와 ntoskrnl PDB 가 같은 칸을 갖는다.
  - 비트마다 이름은 **없다.** 그냥 4바이트 정수다.
  - 이웃: `+0x300 QpcFrequency` (8B) · `+0x30c Reserved2` (4B).
- run34 해설이 "`SystemCall` 로 보인다 `[추측]`" 이라고 한 이름이 이제 `[정적]` 이다.

#### bit 0 의 뜻 — 스텁이 보는 것만 `[정적]`

- ntdll 스텁은 모두 `TEST byte [0x7ffe0308],1` — **bit 0 하나만** 본다.
  - 꺼짐 → `+0x12 SYSCALL` (보통 문)
  - 켜짐 → `+0x10 JNZ → +0x15 INT 0x2e` (옛 쪽문)
- ntdll 전체에서 이 칸을 읽는 곳은 **494곳, 전부 24바이트 스텁** 이다.
  - run56 이 센 스텁 수와 같다.
  - 스텁 밖에서 이 칸을 읽는 ntdll 코드는 **없다.** kernel32 · user32 는 0곳.
- 비유: 로비 공용 게시판에 칸이 하나 있다. "오늘은 옛 쪽문 쓰세요" 표시다. 관리실 전화기 494대가 **전부** 걸기 직전에 이 칸만 흘끗 본다. 다른 데서는 아무도 이 칸을 안 본다.

#### 누가 쓰나 — "이 방법으로는 안 보임" `[정적, 한계 있음]`

여기는 천천히 읽어야 한다. "없다" 가 아니다.

1. 커널 쪽 이 칸의 주소는 `0xfffff78000000308` (커널이 보는 같은 게시판).
2. 결과서는 ntoskrnl Ghidra 프로젝트를 열 수 없었다 (다른 decompiler 가 쓰는 중). 그래서 `objdump` 로 바이너리 전체를 훑었다.
3. 게시판 주소 `0xfffff78000000XXX` 를 레지스터에 올리는 곳이 **1026곳**. 각각 뒤 150줄 안에서 `+0x308` 을 읽거나 쓰는 줄 → **0곳**.
4. 직접 주소 `0xfffff78000000308` 도 0곳, 유저 주소 `0x7ffe0308` 도 0곳.
5. 같은 방법이 `+0x300` · `+0x320` 은 **잘 잡는다.** 그러니 방법이 아예 눈먼 건 아니다.
6. 하지만 레지스터를 여러 번 옮기거나, 포인터를 넘겨 다른 함수가 쓰는 길은 이 방법이 **못 본다.**
7. 그래서 → "쓰는 곳 없음" 이 아니라 "**이 방법으로는 안 보임**" 까지다.

- 덤: 게시판 페이지를 만드는 `MiInitializeSharedUserData` (rva `0xc5c878`) 는 할당 · PTE · 매핑만 하고 **칸을 안 채운다** (결과서 `objdump`).
- 쓰는 쪽은 Q192 ② 로 넘어갔다 (4절).

#### 실제 값 — run44 사진 `[관찰]`

- 커널 주소로 본 게시판: 사진 **8장 모두** `+0x308 SystemCall : 0` (`ms/logs/04_baggage-read_supp_<K1 ~ P2>.txt:246`).
- 유저 주소로 본 게시판: P1a · P1b · P2 도 0 (`04_baggage-read_P1a.txt:2954` · `_P1b.txt:4072` · `_P2.txt:4084`).

**그래서** (단계별로):

1. 스텁은 bit 0 이 꺼지면 `SYSCALL` 로 간다 `[정적]`.
2. 사진마다 그 칸이 0 이다 `[관찰]`.
3. → **test.exe 의 syscall 은 모두 `SYSCALL` 길** 이다 `[정적+관찰]`.
4. 이 칸이 디버거 때문에 바뀐다는 증거는 못 봤다. (사진은 디버거가 붙은 상태였는데 값은 0.)

- run34 해설 4절의 "`INT 0x2e` 쪽이면 커널 입구가 같은가?" 질문은 **test.exe 에선 필요 없는 질문** 이 됐다. `INT 0x2e` 갈래의 커널 입구 (`KiSystemService`) 자체는 run56 정적 남은 것으로 그대로 남는다.

### 2.4 Q66 ② 부모는 인사팀 창구 앞에서 답을 기다린다

#### 인자를 하나씩 — `+0xed NtAlpcSendWaitReceivePort` `[정적]`

| 인자 | 값 | 어느 줄 |
|---|---|---|
| 포트 | `CsrPortHandle` (rva `0x1d4e20`) | `+0xbb` |
| Flags | **`0x20000`** | `+0x48 MOV R10D,0x20000` → `+0xdc MOV EDX,R10D` |
| 보내는 메시지 · 속성 | 메시지 칸 · 0 | `+0xd4 MOV R8,RBX` · `+0xcc XOR R9D,R9D` |
| **받는 메시지** | **같은 칸** | `+0xdf MOV [RSP+0x20],RBX` |
| 받는 크기 칸 | `[RSP+0x40]` = `0x3b8` | `+0xc2` · `+0xd7` · `+0xe4` |
| 받는 속성 · **시간 제한** | 0 · **0 (없음)** | `+0xc7` · `+0xcf` |

- `CsrPortHandle` 은 run68 에서 본 csrss 연결 때 받은 **전화선 끝** 이다.

#### 왜 "기다린다" 고 말할 수 있나 — 단계별로

1. **받는 칸이 있다.** 받을 메시지 자리에 보낸 메시지와 **같은 칸** (`RBX`) 이 들어 있다 `[정적]`.
   - 받을 생각이 없으면 이 칸을 비워 둘 것이다.
2. **시간 제한이 0 (없음)** 이다 `[정적]`.
   - **시간 제한** = "이만큼 기다려도 답이 없으면 그냥 돌아온다" 는 마감. 이게 없으면 답이 올 때까지 돌아오지 않는다.
3. 그래서 → **"보내기만" 일 수 없다.** 답이 와야 `+0xf2` 로 돌아온다 `[정적]`.

- `0x20000` 의 이름은 공개 헤더에서 빌린 **`ALPC_MSGFLG_SYNC_REQUEST`** 다 `[추측: 이름]`. 이 PDB 엔 그 이름이 없다. **값은 `[정적]`.**
  - **동기 요청** = 보내고, 답이 올 때까지 **그 자리에서 기다리는** 요청. 비유: 창구에 서류를 내고 **번호표 받고 집에 가는 게 아니라, 창구 앞에 서서 도장 받을 때까지** 있는 것.
  - 위 1 ~ 3 은 이 이름이 없어도 성립한다. 이름은 덤이다.
- 알림 가능 대기 비트 (`0x200000` 류) 는 값에 없다 `[추측: 비트 이름]`.
- 비유: 같은 신고서 한 장을 내밀고, 인사팀이 **그 종이 위에** 결과를 적어 돌려줄 때까지 창구 앞에 서 있다. 마감 시간도 없다.

#### 답은 어디에 — 메시지 `+0x34` `[정적]`

메시지 종이의 칸 (코드에서 읽은 만큼):

| 칸 | 내용 |
|---|---|
| `+0x0` | 길이 둘 (데이터 `R9+0x18` · 전체 `R9+0x40`) |
| `+0x28` | 캡처 버퍼 |
| `+0x30` | API 번호 (`+0x51` 이 씀 — 여기 `0x1001d`) |
| **`+0x34`** | **반환 status** |

돌아오는 길:

1. `+0xf2 MOV R8D,EAX` — syscall 결과를 쥔다.
2. `+0x12c TEST` → 음수가 아니면 `+0x12f JNS → +0x135`.
3. **`+0x135 MOV EAX,[RBX+0x34]`** — 종이의 `+0x34` 를 반환값으로.
4. `+0x147 RET`.

- **전화 자체가 실패하면** (ALPC 결과가 음수) `+0x131 MOV [RBX+0x34],R8D` 가 그 실패 값을 `+0x34` 에 **먼저 적고**, 같은 `+0x135` 로 간다.
- 그래서 반환값은 **늘 `+0x34` 안의 값** 이다: "인사팀이 적은 결과" 또는 "전화가 안 됐다" 중 하나.
- 비유: 창구가 결과 칸에 도장을 찍어 주든, 전화가 끊겨서 관리실이 직접 "연결 실패" 라고 쓰든, **결과는 늘 같은 칸** 에 있다.

#### csrss 쪽 짝 — 이름만 `[정적]`

- `csrsrv!CsrApiRequestThread` 가 처리기를 부르고, `+0x656 MOV [RSP+0xe4],EAX` 로 결과를 적는다.
  - 이 자리 메시지는 `[RSP+0xb0]` 에 있으니 `0xb0 + 0x34 = 0xe4`. **두 쪽이 `+0x34` 로 맞는다.**
  - 모르는 번호가 오면 `+0x751` · `+0x790` 이 `0xc00000af`.
- 주의: 처리기 자리가 둘 (`+0x4e1` · `+0x651`) 인데, run 문서가 `.asm` 을 다시 보니 **`+0x4e1` 길은 `+0x4e6 JMP → +0x4fb` 로 `+0x656` 을 안 지난다.** 그 길에서 누가 `+0x34` 를 쓰는지는 안 봤다 `[공백]`.
  - 그러니 "두 쪽이 `+0x34` 로 맞는다" 는 **`+0x651` 길 기준** 이다.

#### 부모는 반환값 대신 종이를 다시 본다 `[정적]`

- `CreateProcessInternalW +0x4677 MOV ECX,[RSP+0xc14]` — `0xc14 = 0xbe0 + 0x34`. 메시지 (`[RSP+0xbe0]`) 의 `+0x34` 칸이다.
- `+0x4680 JNS` — 음수가 아니면 계속.
- 음수면 `+0x4682 BaseSetLastNTError` → `+0x4695 JMP → +0xb61` (실패 끝맺음).
- → **csrss 신고가 실패하면 `CreateProcess` 전체가 실패로 끝난다.**
- 비유: 관리실이 "결과 칸 봤어요" 하고 넘겨 준 말 (`EAX`) 을 안 믿는 게 아니라, 부모가 **종이를 직접 다시 들여다본다.** 같은 칸이라 결과는 같다.

#### 갈래 셋 — 코드엔 있고, test.exe 부모는 안 탄다 `[정적]`

| 자리 | 조건 | 가는 곳 |
|---|---|---|
| `+0x14` → `+0x1e JNZ` | `LdrpIsSecureProcess ≠ 0` | `+0x1b8` `0xc00000bb` — **안 보냄** |
| `+0x6f` → `+0x76 JZ` | `CsrClientProcess == 0` | 전화 대신 **같은 프로세스 안** `CsrServerApiRoutine` 간접 호출 (`+0x182`, `ClientId` 16B 를 `[msg+8]` 에) — csrss 자신 · 네이티브용 |
| `+0x69 JNZ` | PEB `+0x3` 비트 1 (`IsProtectedProcess`) | `+0x149` (서버 번호 2 면 `0xc0000022` 등) |

- 둘째 갈래가 중요하다. 왜 부모는 이 길을 안 타나:
  1. `CsrClientProcess` 는 `CsrClientConnectToServer +0x8f` 가 쓴다. exe `Subsystem ≠ 1` 이면 1 (`+0x81`) `[정적]`.
  2. 부모 = cmd.exe (CUI) `[소스: 실행 방법]`. 부모 헤더 자체는 안 읽었다.
  3. → CUI · GUI 부모는 `CsrClientProcess = 1` → **전화 (ALPC) 길**.
  - 비유: 인사팀 사무실 **자기 자신** 은 자기한테 전화를 안 건다. 옆 책상으로 서류를 바로 넘긴다. 다른 가게만 전화를 건다.
- 셋째 갈래: 부모가 보호 프로세스일 일은 없다고 본다 `[추측]`.
- 덤: 캡처 버퍼가 있으면 `CsrPortMemoryRemoteDelta` (`+0x81`) 로 버퍼 안 포인터를 서버 쪽 주소로 바꿔 보내고 (`+0x8c ~ +0xb9`), 돌아와 되돌린다 (`+0xfa ~ +0x12a`). run68 해설 2.4 "주소 옮겨 적기" 와 같은 짝이다.

#### "csrss 등록이 부모 `NtResumeThread` 보다 먼저 끝난다" — 고리 넷

flow.md 2.2 에 `[추측]` 으로 있던 말이다. run 문서가 이걸 **고리 넷** 으로 쪼갰다. 한 고리씩 보면 어디까지 확인됐는지 보인다.

1. 부모가 답을 기다린다 — `[정적]` (이번, 위).
2. `+0x4672` 를 안 지나고는 `NtResumeThread` (`+0x5bd3`) 에 못 간다 — `CREATE_SECURE_PROCESS` 길 빼고 `[정적]` (run14 흐름 그래프).
3. csrss 처리기 안 `BaseSrvCreateProcess2 → CsrCreateProcess → CsrInsertProcess` 는 동기 호출이다 `[정적]` (run34).
4. csrss 가 처리기가 **돌아온 뒤에** 답을 보낸다 — 처리기 뒤 `+0x656` 이 status 를 쓰고, 답 보내기 후보는 같은 함수 `+0x183` · `+0x6ed`. 다만 `+0x183` 의 `R14` 가 방금 처리한 메시지인지는 안 따라갔다 `[추측: 줄 순서]`.

- 1 · 2 · 3 은 `[정적]`. **4 하나만 남았다.** 그래서 전체 말은 아직 `[추측]` 이다.
- 4 가 닫히면 flow.md 2.2 의 이 말이 `[정적]` 이 된다 → Q192 ④ (4절).
- 결과서가 "다음 요청 후보" 로 적은 "`+0x4672` 와 `NtResumeThread` 의 앞뒤" 는 2 번 고리로 이미 답이 있다 (run 문서 "보탠 것").

### 2.5 Q67 (a) ntdll 을 지번 색인에 먼저 올리고, 명부 줄 표시는 나중에

#### `+0x415` 줄이 맞나 — 맞다 `[정적]`

1. `+0x2d MOV RSI,RDX` — `RSI` = ntdll 시작 (커널이 넘긴 값).
2. `+0x3f9 ~ +0x405 RtlImageNtHeaderEx(3, RSI, 0, …)` — ntdll 설계도 (NT 헤더) 를 편다.
3. `+0x40d MOV EBX,[RAX+0x50]` — 헤더 `+0x50` = `SizeOfImage`.
4. `+0x410 ~ +0x415 RtlInsertInvertedFunctionTable(RSI, EBX)` (ret `+0x41a`).

- 뒤 `+0x43a ~ +0x470` 의 칸 0 · 빼기는 run60 그대로다.

#### 어느 쪽이 먼저인가 `[정적]`

이 구간은 갈래 없이 위에서 아래로 내려간다. 그러니 코드 순서 = 실행 순서다.

1. **`+0x415`** — 지번 색인에 ntdll 을 올린다.
2. `0xd88` 바이트 뒤 **`+0x119d`** (ret `+0x11a2`) `LdrpInitializeNtdllDataTableEntry` — ntdll 명부 줄을 만든다.
   - 그 안 `+0x93 OR [RBX+0x68],0x204` — 비트 2 `ImageDll` + 비트 9 `InExceptionTable`. 새로 켜지는 건 비트 9 하나 (비트 2 는 할당 때 이미, run35).
3. 같은 함수 `+0x11f` (ret `+0x124`) `LdrpProcessMappedModule` 안:
   - `+0x15e BT EDX,9` → 비트 9 가 켜져 있으니 `+0x16f JZ → +0x185` 로 **`+0x177 RtlInsertInvertedFunctionTable` 을 건너뛴다.**

#### 왜 미리 켜나 — 코드가 보여 주는 만큼 `[정적]`

- 일반 DLL 은 `LdrpProcessMappedModule` 이 "**비트 9 꺼짐일 때만**" 색인에 올리고 (`+0x15e ~ +0x177`), 올린 뒤 `+0x17f BTS EDX,9` 로 켠다.
- ntdll 은 이미 `+0x415` 에서 **손으로** 올렸다.
- 그래서 명부 줄을 만들 때 비트를 미리 켜 둔다 → `LdrpProcessMappedModule` 이 **같은 건물을 색인에 두 번 넣지 않는다.**
- run35 해설의 "이미 올림 표시를 미리 켜 두는 것" `[추측]` 이 코드로 확인됐다.
- 비유: 관리실 건물은 명부 줄을 쓰기 **전에** 지번 색인에 먼저 올렸다. 그래서 명부 줄을 쓸 때 "**색인 등록 끝**" 칸에 미리 체크한다. 그러면 다음 단계 담당이 "체크돼 있네" 하고 또 올리지 않는다.

#### 아직 짐작인 것

- "**왜 ntdll 만** 명부 줄도 없을 때 일찍 손으로 올리나" 는 `[추측]` 이다.
  - 명부 줄이 생기기 전에도 ntdll 안 예외 · 되감기가 색인을 써야 해서로 보인다. 증거는 없다.

### 2.6 Q67 (b) `+0x1625` 가 끼우는 항목 = `AvrfpLoaderEntry` 의 복제

#### 새 말 — App Verifier

- **App Verifier** = 개발자가 버그를 잡으려고 **켜는** 검사 도구로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이 코드에선 **켜야만 도는 길** 이다: `AvrfAppVerifierMode ≠ 0` 일 때만 이 묶음에 들어온다 `[정적]`.
- 비유: run19 해설의 **점검관이 온 날에만** 하는 일. 점검관이 안 온 날 (test.exe) 엔 이 묶음 전체를 건너뛴다.

#### 정체 `[정적]`

- ntdll `.data` 전역 **`AvrfpLoaderEntry`** (rva `0x1d4520`) 의 **복제본** 이다.
- exe 항목도 ntdll 항목도 아니다.

#### `[RSP+0x60]` 을 채운 줄까지 거슬러 — 단계별로

`[RSP+0x60]` 는 `LdrpInitializeProcess` 의 지역 칸 (8바이트 포인터) 이다. 이 칸의 **주소** 가 세 함수를 그대로 타고 내려간다. (숫자는 run 문서 (b) 트리 그대로, 호출은 ret 자리.)

```
LdrpInitializeProcess
 │  +0xe8   [RSP+0x60] = 0
 ├─ +0x536  LdrpInitializeExecutionOptions(…, 6번째 = &[RSP+0x60])
 │           └─ +0x763  LdrpInitializeApplicationVerifierPackage(…, 그 주소)
 │                       └─ +0xf7  AVrfInitializeVerifier(…, R9D = 0, 그 주소)
 │                                  +0x3d9 R15 = &AvrfpLoaderEntry
 │                                  ├─ +0x3f7  AvrfMiniLoadDll(…, [RSP+0x20] = R15)
 │                                  │  +0x3fd  성공이면 [RSI] = &AvrfpLoaderEntry    ← 이 줄
 │                                  │  +0x414  AvrfAppVerifierMode = R14D (1 또는 2)
 │  +0x1568 AvrfAppVerifierMode == 0 → +0x156f JZ → +0x1753  (test.exe)
 ├─ +0x157c LdrpAllocateModuleEntry(0) → RBX
 │           +0x15b0 ~ +0x161e  [RSP+0x60] 에서 칸 복사
 ├─ +0x162a LdrpInsertDataTableEntry(RBX)
```

1. `+0xe8` 에서 칸을 0 으로 비워 두고 시작한다.
2. 칸 **주소** 를 `LdrpInitializeExecutionOptions` 의 6번째 인자로 넘긴다 (`+0x507` · `+0x50c`).
3. 그 함수는 받은 주소를 그대로 `LdrpInitializeApplicationVerifierPackage` 에 넘긴다 (`+0x3f` · `+0x73d` · `+0x74c`).
4. 그 함수도 그대로 `AVrfInitializeVerifier` 에 넘긴다 (`+0xc8` · `+0xd3`). 이때 4번째 인자는 0 (`+0xe7`).
5. `AVrfInitializeVerifier` 는 4번째 인자 0 길 (`+0x43 JZ → +0x2ac`) 에서:
   - `+0x2ac` `RSI` = 그 주소, `+0x2bf [RSI] = 0`
   - `+0x3d9` `R15` = `&AvrfpLoaderEntry`
   - `+0x3f2` (ret `+0x3f7`) `AvrfMiniLoadDll(…, R15)` 이 **성공하면**
   - **`+0x3fd MOV [RSI],R15`** — 이 줄이 `[RSP+0x60]` 에 `&AvrfpLoaderEntry` 를 쓴다.
6. 같은 함수 `+0x414` 가 `AvrfAppVerifierMode` 를 1 또는 2 로 쓴다. 이 전역을 **쓰는 곳은 여기 하나** 다.
7. 다시 `LdrpInitializeProcess` 로 돌아와, 모드가 0 이 아니면:
   - `+0x1577` 빈 명부 줄 `RBX` 를 만들고,
   - `+0x15b0 ~ +0x161e` 에서 `[RSP+0x60]` 이 가리키는 항목의 `Flags` · `OriginalBase` · `FullDllName` · `BaseDllName` · `TimeDateStamp` · `CheckSum` · `DllBase` 를 **베낀다**,
   - `+0x1625` 에서 명부에 끼운다 (뒤이어 `+0x166b` 색인 · `+0x1679 LdrpProcessMappedModule` · `+0x168f EntryPoint` 복사 · `+0x16b0 State = 7`).

- 한 줄로: **"임시 항목 → 정식 항목으로 갈아끼우기"** 다 `[정적]`.
- 비유: 점검관이 정식 명부가 펴지기도 전에 와서, 자기 건물 이름을 **임시 쪽지** (`AvrfpLoaderEntry`) 에 적어 둔다. 명부가 준비되면 관리실이 그 쪽지를 보고 **정식 명부 줄로 옮겨 적는다.**

#### 아직 짐작인 것

- `AvrfMiniLoadDll` 이 loader 준비 전에 verifier 쪽 DLL 을 직접 붙이며 `AvrfpLoaderEntry` 를 채운다고 **보인다** — 채우는 줄은 이번에 안 읽었다 `[추측: 결과서 설명 + 복사하는 칸 모양]`.
  - 칸 모양은 맞는다: `AVrfInitializeVerifier +0x18b` 가 `[AvrfpLoaderEntry+0x30]` 을 `DllBase` 로 읽는다.
- 붙이는 DLL 이름 (`verifier.dll`) 도 `[추측]`.
- 채우는 줄은 Q192 ③ (4절).

#### run19 짐작은 반만 맞았다

- run19 는 "verifier **공급자** 모듈?" 이라고 짐작했다.
- 실제로 공급자를 올리는 건 **4번째 인자 1 길** (`AVrfInitializeVerifier +0x49`, `LdrpInitializeProcess +0x1708` 의 둘째 호출) 이다.
- 복제되는 건 **첫째 호출 (4번째 인자 0) 이 채운 임시 항목** 이다.

#### test.exe 는 안 탄다 `[정적+관찰]`

- `+0x1568 CMP [AvrfAppVerifierMode],R15D` (`R15D = 0`, 마지막 대입은 `+0x11a2`) → `+0x156f JZ → +0x1753` `[정적]`.
- 사진 `[관찰]` (run44):
  - `AvrfAppVerifierMode` 0 (flow.md 2.4 run44 "갈림 값")
  - PEB `NtGlobalFlag = 0` (`04_baggage-read_P1a.txt:1770` · `_P2.txt:2480`)
  - `IFEOKey (null)` (`_P2.txt:2452`)
  - `AvrfpLoaderEntry` 는 `끝까지 0*` (짐 지도 7.3)
- → 점검관은 오지 않았다.

### 2.7 Q67 (c) 핫패치 문맥 — 메모지 주소는 늘 칠판에 적는다

#### 새 말 — 핫패치

- **핫패치** = 프로그램을 끄지 않고, 실행 중인 코드를 **고친 판으로 바꿔 끼우는** 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유는 run17 해설 그대로: 가게 문을 안 닫고 **선반 하나만 새것으로 갈아 끼우기**. 여기선 "선반" 이 **ntdll 자체** 다 — 고친 ntdll 판을 따로 붙인다.

#### 새 말 — 핫패치 문맥 (ctx)

- **ctx** = `LdrpInitializeInternal` 의 **스택 지역 16바이트** `[RSP+0x30]` `[정적]`.
  - 핫패치 쪽 결과 (패치 판이 붙은 주소, 결과 status, "패치 있음" 표시) 를 담는 **메모지** 다.
- 전역 **`LdrpNtdllHotPatchContext`** (rva `0x1d2908`) = 그 메모지의 **주소** `[정적]`.
  - 비유: 메모지는 직원 (스레드) 책상 위에 있고, 관리실 칠판엔 "메모지는 저 책상 위" 라고만 적는다. 다른 단계는 칠판을 보고 메모지를 찾아간다.

#### `+0xa7` 은 켜짐 · 꺼짐과 상관없다 — 왜? `[정적]`

이게 이번 (c) 의 핵심이다. 줄 순서로 본다.

1. `+0x98 CMP [LdrpIsHotPatchingEnabled],0` — 핫패치 켜졌나 **비교만** 한다. 결과는 CPU 플래그에 남는다.
2. `+0x9f LEA RAX,[RSP+0x30]` — 메모지 주소.
3. `+0xa4 XORPS` — 0 만들기.
4. `+0xa7 MOV [LdrpNtdllHotPatchContext],RAX` — 칠판에 메모지 주소.
5. `+0xae MOVUPS [RSP+0x30],XMM0` — 메모지 16B 를 0 으로.
6. `+0xb3 JZ → +0xc6` — **여기서야** 1번의 비교 결과로 뛴다. 꺼짐이면 `LdrpLoadPatchedNtdll` 을 건너뛴다.

- 3 ~ 5 는 플래그를 안 바꾸는 명령이다. 그래서 1번 비교 결과가 6번까지 그대로 살아 있다.
- → 갈림길 (6) 이 **메모지 준비 (4 · 5) 뒤에** 있다. 그러니 꺼져도 칠판엔 주소가 적힌다.
- 끝: `_LdrpInitialize` 에서 돌아오면 `+0x5e AND [LdrpNtdllHotPatchContext],0` 로 칠판을 지운다 (첫 스레드라 `+0x5c JNZ` 안 탐).
  - 그래서 이 칠판 칸은 **초기화 도중에만** 스택 주소를 가진다.

#### 사진이 그대로 보여 준다 `[관찰]` (run44)

| 사진 | `LdrpNtdllHotPatchContext` (`ntdll+0x1d2908`) | 근거 |
|---|---|---|
| P1a | **`0x9b2fcff9d0`** | `_P1a.txt:5130` |
| P1b | **`0x9b2fcff9d0`** | `_P1b.txt:6248` |
| P2 | **0** | `_P2.txt:6260` |

- `0x9b2fcff9d0` 은 메인 스레드 TEB 의 `StackBase 9b2fd00000` · `StackLimit 9b2fcfe000` (`_P1a.txt:3426`) **사이** — 즉 메인 스레드 스택 안이다.
- 같은 사진 P1a 에서 `LdrpIsHotPatchingEnabled` (`ntdll+0x1d26f0`) 는 **0** (`_P1a.txt:5097`).
- → **꺼짐인데 칠판엔 스택 주소** — 위 1 ~ 6 그대로다. 은퇴 전 (P2) 엔 지워져 있다 `[정적]` 과 `[관찰]` 이 맞는다.
- 메모지 16B 자체 (`9b2fcff9d0` 자리) 는 사진에 안 찍혔다 (스택 덤프는 `dq 9b2fcffa40 …` 뒤쪽만, `_P1a.txt:3230`). → 동적 (4절).

#### 메모지 칸 `[정적]`

| 칸 | 크기 | 뜻 | 쓰는 줄 | 읽는 줄 |
|---|---|---|---|---|
| `+0x0` | 8B | **패치 ntdll 판을 붙인 시작 주소** | `LdrpLoadCustomNtdllWithSectionHandle +0x49 NpMapViewOfSection` 의 `BaseAddress` 출력 (없거나 실패면 0) | `LdrpInitializeProcess +0x1250 ~ +0x1256` (0 이면 패치 명부 줄 안 만듦) |
| `+0x8` | 4B | `LdrpLoadPatchedNtdll` 반환 status | `LdrpInitializeInternal +0xc2` | `+0x123a` (음수 + 켜짐이면 `+0x124e INT 0x29` 빠른 실패, 코드 `0x45`) · `+0x1904` |
| `+0xc` | 1B | "패치 있음" 표시 | `LdrpLoadPatchedNtdll +0x57` | `+0x18f6` (1 이면 `+0x1916 LdrpLogEtwHotPatchStatus`) |

- `+0xc` 를 읽는 쪽이 부르는 `LdrpLogEtwHotPatchStatus` 는 이름 그대로 ETW (**방송국**, run62 해설) 에 핫패치 상태를 알리는 쪽이다.
- 질문 (c) 의 답 "`[ctx+0]` 에 무엇을 쓰나": `LdrpLoadPatchedNtdll` 이 **직접** 쓰는 건 0 뿐이다 (`+0x19`, 실패 때 `+0x157`). **진짜 값은** 그 안에서 부르는 `LdrpLoadCustomNtdllWithSectionHandle` 의 `NpMapViewOfSection` 이 출력 칸으로 바로 쓴다 (`+0x18 MOV R8,RDX`).
  - `Np*` = ntdll 자체 스텁. `NpMapViewOfSection` 은 `NtMapViewOfSection` 과 같은 번호 `0x28` 이다.
  - 비유 (run38 · run57 섹션 비유 이어서): 관리사무소 창고의 고친 ntdll **원본 물건** 을 가게 땅에 **한 판 펼치고**, 펼친 자리 번지를 메모지 첫 칸에 적는다.

#### `LdrpLoadPatchedNtdll` 순서 — 켜짐일 때만 온다 `[정적]`

1. `[ctx+0] = 0` → `LdrpQueryCurrentPatch` → 안에서 `NtManageHotPatch(8, …, 0x30)`:
   - `0xc0000023` → 표시 1 (패치 있음)
   - `0` · `0xc0000018` → 표시 0
   - 그 밖 → 0 + `0xc00004ac`
2. 표시를 `[ctx+0xc]` 에. **표시 0 이면 `+0x5d` 에서 끝** (`[ctx+0]` 은 0 그대로).
3. 표시 1 이면 `NtManageHotPatch(0xa)` 로 섹션 핸들 → `LdrpLoadCustomNtdllWithSectionHandle` (붙이기 · 필요하면 재배치 · 헤더 검사) → `NtManageHotPatch(7)` ("이 패치를 적용" 알림으로 보임 `[추측: 뜻]`) → `NpClose`. 실패면 `NpUnmapViewOfSection` 후 `[ctx] = 0`.

- 즉 메모지 첫 칸이 채워지려면 **켜짐 + 패치 있음** 둘 다 필요하다.

#### 켜짐 조건 `[정적]`

`LdrpIsHotPatchingEnabled` 를 **쓰는 곳은 둘뿐** 이다 (`_refs`).

1. `LdrpInitializeHotPatching +0x55 MOV [flag],BL`
   - `+0x18 TEST [ProcessParameters+8],0x2000000` 켜짐이면 → 0 (`+0x53`).
   - 아니면 `+0x40 NtManageHotPatch(9, {1}, 8)` 결과가 **`0xc00000bb` · `0xc0000002` 면 0, 그 밖 (성공 포함) 이면 1**.
2. `LdrpInitializeProcess +0x587` — `LdrpImageExpansionMitigation == 2` 일 때 0. 이 줄은 패치 로딩 **뒤** 에 있다.

- (참고, 결과서 `objdump` — 확인 필요) 커널 `nt!NtManageHotPatch` (rva `0xa47e60`) 는 전역 dword (rva `0xe375f4`) bit 0 이 꺼져 있으면 **모든 클래스에 `0xc00000bb`**. 그 bit 는 부팅 때 `MiInitializeHotPatches +0x82` 가 켠다.

### 2.8 test.exe 는 핫패치가 꺼져 있었다 — 사진으로 좁힌 추론

결과서는 여기서 "정적으로 못 정한다" 고 멈췄다. run 문서가 사진 값을 더해 한 칸 더 갔다. **단계별로** 본다.

1. **근거 ①** P1a 사진은 `LdrpInitializeProcess +0x199d` 에서 찍었다 (run44). 이 자리는 `+0x587` 보다도, `+0x1233` 보다도 **뒤** 다.
2. **근거 ②** P1a 에서 `LdrpIsHotPatchingEnabled` = **0** (`_P1a.txt:5097`) `[관찰]`.
3. **그래서** P1a 전에 누군가 0 을 썼거나, 처음부터 0 이었다. 0 을 쓸 수 있는 곳은 둘뿐 (2.7 "켜짐 조건").
4. **근거 ③** 그중 `+0x587` 은 `LdrpImageExpansionMitigation == 2` 여야 타는데, P1a 에서 그 값은 **0** (`ntdll+0x1d2978`, `_P1a.txt:5137`) `[관찰]` → `+0x587` 은 **안 탔다.**
5. **그래서** 남은 하나, `LdrpInitializeHotPatching +0x55` 가 0 을 쓴 것이다.
6. `+0x55` 는 `LdrpInitializeInternal +0xb3` 갈림길 **앞** 이다 → `+0xb3 JZ` 가 뛰었다 → **`LdrpLoadPatchedNtdll` 은 안 불렸다** `[정적+관찰]`.
7. → 메모지 첫 칸 `[ctx+0]` 은 0 그대로 → `+0x1256 JZ` 로 **`+0x126a` 패치 명부 줄도 안 만들었다.**

- 다른 사진 값도 같은 쪽을 가리킨다 (결과서): PEB `CrossProcessFlags.ProcessImagesHotPatched = 0` (`_P2.txt:2461`), EPROCESS `MmHotPatchContext (null)` (`:298`), 모듈 6개 `HotPatchState 0`.
- **아직 모르는 한 가지**: `+0x55` 가 0 을 쓴 **까닭** — `NtManageHotPatch(9)` 가 `0xc00000bb` 를 줬나, `0xc0000002` 를 줬나, 아니면 `+0x18` 매개변수 비트 갈래였나.
  - run 문서: 사진 K1 의 매개변수 `Flags` 가 `0x4001` 이라 비트 25 (`0x2000000`) 는 꺼져 있다. 그럼 `NtManageHotPatch` 를 탔을 것이다 `[추측]`.
  - 이건 "켜졌나" 가 아니라 "**왜 꺼졌나**" 라서 확인용이다 → Q43 (4절).
- 비유: 선반 갈아 끼우기 스위치는 꺼져 있었다. 스위치를 끌 수 있는 손은 둘인데, 한 손 (`+0x587`) 은 조건이 안 맞아 움직이지 않았다. 그러니 다른 손 (`+0x55`) 이 껐다. 그 손이 **왜** 껐는지만 아직 모른다.

### 2.9 PE_emulator 와 작게 다른 곳 하나 (동작은 같음)

run 문서가 결과서 "지금 우리 맞나" 를 요약했다 (`../PE_emulator` 코드는 flow-writer 가 직접 안 읽음).

- **맞음**: KUSD `SystemCall` 0 (`Src/nt/AddressSpace.cpp:505`), `RtlInsertInvertedFunctionTable` 을 `+0x415` 근거로 부름 (`Src/ntdll/LdrInit.cpp:1419`), `AvrfAppVerifierMode` 안 읽음 (`LdrInit.cpp:5845~5846`), `CsrClientProcess` 값 (`BaseDllInit.cpp:1139~1140`).
- **해당 없음**: 부모 · csrss 는 에뮬레이터에 없다 (`Src/main.cpp:268~269`, 의도).
- **작게 틀림**: `Src/ntdll/LdrInit.cpp:7084~7092` 주석은 "`LdrpNtdllHotPatchContext` 안 씀 … 비우기도 안 함" 이다.
  - 진짜는 2.7 처럼 **꺼져도 `+0xa7` 이 스택 주소를 쓰고 `+0x5e` 가 지운다.**
  - 읽는 쪽 셋은 메모지 16B 가 0 이라 건너뛰니, **결과는 같다.**
  - 다른 건 초기화 **도중** 의 칠판 값 (스택 주소 vs 0) 뿐이다. 그 사이 메모리를 찍어 비교할 때만 보인다.
  - 고칠지는 **사용자 결정** 이다.

### 2.10 비유로 다시 한 번

- 부모 가게 관리실이 인사팀 사무실에 전화를 걸어 새 가게를 신고한다. **수화기를 든 채 기다린다.** 마감 시간도 없다. 인사팀은 **같은 신고서의 결과 칸 (`+0x34`)** 에 도장을 찍어 돌려준다. 부모는 그 칸을 직접 다시 보고, 빨간 도장이면 가게 열기를 포기한다.
- 관리실 전화기 494대는 걸기 직전에 로비 게시판의 한 칸 (`SystemCall`) 을 본다. 이 VM 에선 늘 0 → **늘 보통 문 (`SYSCALL`)**. 그 칸을 누가 쓰는지는 이번 방법으로는 안 보였다.
- 관리실은 관리실 건물을 지번 색인에 **먼저** 올리고, 나중에 명부 줄을 쓸 때 "색인 등록 끝" 칸에 미리 체크한다. 그래야 두 번 안 올린다.
- 점검관이 온 날에만, 점검관이 미리 적어 둔 **임시 쪽지** 를 정식 명부 줄로 옮겨 적는다. test.exe 날엔 점검관이 안 왔다.
- 선반 갈아 끼우기 (핫패치) 는 스위치가 꺼져 있어도, 직원이 책상 위에 **빈 메모지** 를 놓고 칠판에 "메모지는 저기" 라고 적는다. 메모지는 끝까지 비어 있어서 읽는 쪽 셋이 다 지나간다. 은퇴 전에 칠판을 지운다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

**그림** — 새 상자는 없다. 상자 글과 라벨만 바뀌었다.

| 자리 | 전 | 후 |
|---|---|---|
| 그림 1 `nd2` (`CsrClientCallServer`) | "정적: csrss 알림. ApiNumber 0x1001d" | + "run105: `+0xed NtAlpcSendWaitReceivePort` Flags 0x20000 — 받는 칸 = 보낸 칸, 시간 제한 없음 → 답 올 때까지 기다림. 답 status = 메시지 +0x34, 부모 +0x4677 이 다시 읽어 음수면 CreateProcess 실패" |
| 그림 1 `nd1 ==> a0` 라벨 | "`+0x12 SYSCALL` (정적. `0x7ffe0308` bit0 면 `+0x15 INT 0x2e`)" | "… **KUSD SystemCall** `0x7ffe0308` bit0 면 … — **사진 8장 0**, run105" |
| 그림 2 `p5` (둘째 `LdrpInitializeNtdllDataTableEntry`) | "핫패치 컨텍스트가 있을 때만" | "핫패치 문맥 **ctx+0 (패치 ntdll 시작) ≠ 0** 일 때만. run105: 핫패치 꺼지면 ctx 16B 0 그대로 → 안 탐. **test.exe 안 탐** (사진 P1a 꺼짐)" |
| 그림 1 · 2 제목 | … | run105 추가 |

**절과 태그** — 확정 사실을 고친 곳은 없다 (run 문서 "정정"). `[추측]` 이 올라갔을 뿐이다.

| 절 | 전 | 후 |
|---|---|---|
| 2.2 csrss 쪽 "기다리나" | 부모는 답을 받을 때까지 기다린다 `[추측: 일반 지식]` | **기다린다** `[정적]` + 답 칸 `+0x34` + 갈래 셋 + "`NtResumeThread` 보다 먼저" 고리 넷 (넷째만 `[추측]`) |
| 2.2 `CreateProcessInternalW` ② csrss 알림 | 부르는 줄만 | + 부모가 `+0x4677` 에서 `+0x34` 를 다시 읽고 음수면 실패 `[정적]` |
| 2.2 스텁 절 | `SystemCall` 칸으로 보임 `[추측]` · 보통은 SYSCALL 쪽 `[추측]` | 칸 = `+0x308 SystemCall` 4B `[정적]`, 494곳 스텁만 읽음, 쓰는 커널 줄은 안 보임 `[정적, 한계 있음]`, 사진 0 `[관찰]` |
| 2.4 `LdrpInitializeInternal` 핫패치 | 켜짐 조건만 (run17) | + 켜짐 규칙 `[정적]`, test.exe 꺼짐 `[정적+관찰]`, ctx = 지역 16B · 전역 = 그 주소 · `+0xa7` 은 늘 씀 `[정적]` + 사진 `0x9b2fcff9d0` `[관찰]` |
| 2.4 둘째 `+0x126a` | 핫패치 꺼진 보통 프로세스는 안 탄다 `[추측]` | `[정적]` (꺼지면 ctx 0 → `+0x1256` 뜀), test.exe 는 `[정적+관찰]` |
| 2.4 `+0x587` 줄 | 조건만 | + 패치 로딩 **뒤** 라 로딩 자체는 못 막음 `[정적]`, test.exe `LdrpImageExpansionMitigation` 0 `[관찰]` |
| 2.4 AppVerifier 복제 항목 | 무엇을 복제하나 (verifier 공급자 모듈?) `[추측]` | **`AvrfpLoaderEntry` 복제** `[정적]` + 사슬 + 공급자 쪽이 아님 |
| 2.4 `LdrpInitializeNtdllDataTableEntry` | `InExceptionTable` 미리 켬 | + 순서 (`+0x415` 먼저) 와 "두 번 등록 막기" `[정적]`, "왜 ntdll 만 일찍" 은 `[추측]` |
| 4장 공백 표 | Q66 · Q67 줄 열림 | "해결됨, 정적 → run105". run105 새 줄 셋 (동적 · 정적 · PE_emulator) |

**짐 지도 (baggage.md)** — run 문서 "반영한 것" 대로 한 줄만:

- 3.8 에 **`AvrfpLoaderEntry`** 새 줄 (짐작). ntdll 전역 아는 것 188 → **189**, 모름 305 → **304**.
- KUSD `SystemCall` 은 **쓰는 쪽을 몰라** 아직 "모름" 에 남는다.

### 큰 그림에서 어떻게 이어지나

- **부모 쪽 순서가 한 고리만 남았다.**
  - "csrss 에 신고 → 답 → 첫 스레드 풀기" 에서 이번에 **"답을 기다린다"** 가 코드로 섰다.
  - 남은 건 "csrss 가 처리를 **다 끝낸 뒤** 답을 보내나" (넷째 고리) 하나다.
  - 이게 닫히면 그림 1 에서 csrss 노란 메모가 부모의 `NtResumeThread` **앞에** 있다는 게 `[정적]` 이 된다.
- **test.exe 의 syscall 문이 하나로 정해졌다.**
  - 지도의 굵은 화살표 (ntdll ==> nt) 는 이제 "`SYSCALL` 길" 하나만 생각하면 된다 (이 VM, 이 실행 기준).
- **로더 입구에서 "안 타는 길" 둘이 확실해졌다.**
  - 핫패치 로딩과 App Verifier 복제 항목. 둘 다 test.exe 에선 건너뛴다.
  - 그래서 명부에는 여전히 **ntdll 줄 하나 + exe 줄** 로 시작한다 (패치 ntdll 줄 · verifier 복제 줄 없음).
- **짐 지도 쪽에선 "늘 쓰는데 늘 비어 있는" 짐 하나가 생겼다.**
  - `LdrpNtdllHotPatchContext` 는 초기화 도중에만 스택 주소를 갖는다. 에뮬레이터가 이 순간을 흉내 낼지는 사용자 결정이다 (2.9).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `NtManageHotPatch(9)` 의 실제 status (`0xc00000bb` / `0xc0000002`), 아니면 `+0x18` 매개변수 비트 갈래였나 | 실행 때 값. 꺼짐 자체는 사진으로 정해졌고, 이건 "왜 꺼졌나" 확인용 | **Q43** (동적): `ntdll+0x8bc19` (`LdrpInitializeHotPatching +0x45`) `eax` |
| 메모지 16B 내용이 정말 0 · 0 인가 | 칠판 값 (`0x9b2fcff9d0`) 은 사진에 있지만, 그 자리 스택은 사진에 안 찍혔다 | **Q43** (동적): `ntdll+0x8d637` (`LdrpInitializeProcess +0x1233`) 에서 `dq poi(ntdll+0x1d2908) L2` |
| (읽기만) 커널 핫패치 켜짐 bit, `AvrfAppVerifierMode` | 실행 값 | **Q43** (동적): `dd nt+0xe375f4 L1` bit 0, `dd ntdll+0x1e7044 L1` (0 이어야) |
| 부모가 정말 `CsrClientCallServer` 에서 기다리나, 돌아온 `+0x34` 값, 그 사이 csrss 의 `BaseSrvCreateProcess2` 가 서나 | 부모 cmd.exe · csrss 는 다른 프로세스. 정적으로는 "기다리는 모양" 까지 | **Q19** (동적, 부모 쪽): `ntdll+0xdbebd` (`+0xed`, `[rbx+0x30] == 0x1001d` 일 때만) → `ntdll+0xdbec2` (`+0xf2`) `eax` · `dd @rbx+0x34 L1`, `db ntdll+0x1d4df8 L1` (`CsrClientProcess` = 1) |
| 커널 `nt!NtManageHotPatch` 클래스 8 · 9 가 무엇을 돌려주나, `0xe375f4` bit 0 을 켜는 입력 (`0xfc6ac8` bit 14 · `0xfc5260`) | ntoskrnl Ghidra 프로젝트를 다른 decompiler 가 쓰는 중이라 `objdump` 로만 훑었다 | **Q192 ①** (정적, ntoskrnl) |
| KUSD `+0x308 SystemCall` 을 쓰는 커널 줄 | `objdump` 훑기 방법의 한계 (레지스터 옮김 · 포인터 넘김) | **Q192 ②** (정적) — 다음 **Q78g1** 문서 바퀴에서 닫힌다 (내용은 그 바퀴에서) |
| `AvrfMiniLoadDll` (rva `0xe094c`) 이 `AvrfpLoaderEntry` 를 채우는 줄, 붙이는 DLL 이름 | 이번엔 본문을 안 읽었다. App Verifier 만이라 test.exe 와 무관 | **Q192 ③** (정적, 작음) — 닫히면 짐 지도 짐작 → 정확 |
| csrss 가 처리기 **뒤에** 답을 보내나 (`CsrApiRequestThread +0x183` 의 `R14` 가 처리한 메시지인가), `+0x4e1` 길에서 `+0x34` 를 쓰는 쪽 | 줄 순서만 봤고 `R14` 출처는 안 따라갔다 `[추측: 줄 순서]` · `+0x4e1` 길 `[공백]` | **Q192 ④** (정적, csrsrv) — 닫히면 flow.md 2.2 "csrss 등록이 `NtResumeThread` 보다 먼저" 가 `[정적]` |
| "왜 ntdll 만 명부 줄 전에 손으로 색인에 올리나" | 코드는 "두 번 등록 막기" 까지만 보여 준다 | 따로 번호 없음 (`[추측]` 으로 둠) |

---

## 5. 더 읽기

- [run105 문서](run105-ntdll-small.md) — "정적 — 호출 지점" 표, Q66 ① · ② / Q67 (a) · (b) · (c) 절, "사진 로그 대조" 표, "결과서와 다르게 적은 곳 · 보탠 것"
- 요청서 [`_workspace/sast/99_ntdll-small.md`](../../_workspace/sast/99_ntdll-small.md) · 결과서 [`99_ntdll-small_result.md`](../../_workspace/sast/99_ntdll-small_result.md)
- [flow.md](flow.md)
  - 그림 1 (`nd2`, `nd1 ==> a0` 라벨) · 그림 2 (`p5`)
  - 2.2 `CreateProcessInternalW` ② csrss 알림 · 스텁 절 · csrss 쪽 절
  - 2.4 `LdrpInitializeInternal` 핫패치 줄 · `LdrpInitializeProcess` 둘째 `+0x126a` · `+0x587` · AppVerifier 복제 항목 · `LdrpInitializeNtdllDataTableEntry` `InExceptionTable` 줄
  - 4장 2.2 · 2.4 줄, run105 새 줄 셋
- [baggage.md](baggage.md) 3.7 (KUSD), 3.8 (`AvrfpLoaderEntry` · `LdrpIsHotPatchingEnabled` · `LdrpNtdllHotPatchContext`), 3.9 (`AvrfAppVerifierMode`), 7.37
- 앞 바퀴 해설
  - [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.4 (`0x1001d`) · 2.5 (스텁, 공용 게시판, 옛 쪽문)
  - [run68 해설](run68-dll-init-rest-해설.md) 2.4 (csrss 연결, ALPC 포트, 주소 옮겨 적기)
  - [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.7 (핫패치 깃발)
  - [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.4 (역함수 표 = 지번 색인)
  - [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.5 · 2.6 (AppVerifier 묶음)
  - [run35 해설](../02_sprint/run35-ntdll-entry-해설.md) 2.5 (`InExceptionTable`) · 2.9 (패치된 ntdll 줄)
  - [run56 해설](run56-syscall-args-해설.md) 2.8 (스텁 494곳)
  - [run44 해설](run44-baggage-snap-해설.md) 2.8 (사진 8 지점)

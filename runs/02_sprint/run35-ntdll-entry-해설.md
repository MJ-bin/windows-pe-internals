# run35 ntdll-entry — 해설

> 이 글은 [run35 문서](run35-ntdll-entry.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q45** — 2.4 `ntdll!LdrpInitializeNtdllDataTableEntry` 본문 (633바이트): 적재·메모리 순서 목록에 넣는 곳, 색인, 둘째 호출(핫패치 ntdll)의 정체 (`_workspace/plan.md`)
- 근거:
  - run 문서 [`run35-ntdll-entry.md`](run35-ntdll-entry.md) (가장 중요)
  - 요청서 [`_workspace/sast/30_ntdll-entry.md`](../../_workspace/sast/30_ntdll-entry.md)
  - 결과서 [`_workspace/sast/30_ntdll-entry_result.md`](../../_workspace/sast/30_ntdll-entry_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q45, Q44, Q67 과 결정 기록
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`. 다시 analyze 하지 않은, flow.md 기준과 같은 분석본이다.
  - 칸 이름·열거 값(`_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_STATE`, `Flags` 비트 위치 …)은 **PDB 타입**으로 읽었다.
  - 문자열 상수 `\SYSTEM32\`·`ntdll.dll` 은 PE `.rdata` 에서 **원본 바이트**로 읽었다.

이 글은 [run18 해설](run18-proc-init-setup-해설.md) 2.7 "명부 첫 줄" 의 **안쪽 이야기**다.

- 기초 말은 [run9 해설](run9-create-user-process-해설.md)·[run10 해설](run10-allocate-process-해설.md) 2절에 있다. 여기선 링크만 건다.
- 비유는 그대로 이어 쓴다.
  - 프로세스 = **가게**, ntdll = 가게 안 **관리실**, exe = **본 건물**, DLL = **건물** (run10·run18)
  - PEB.Ldr = 가게 **건물 명부**, 모듈 항목 = **명부 한 줄** (run18 2.6)
  - DDAG 노드 = 명부 줄마다 붙은 **연결 카드**, `State` = 카드의 **진행 도장** (run19·run21)
  - 해시 표 `LdrpHashTable` = **이름 색인 카드함** (run20 2.3)
  - `UNICODE_STRING` = "몇 글자, 어디부터" 를 적은 **쪽지** (run19 2.1)
  - 역함수 표 = 예외 때 "이 번지는 어느 건물?" 을 찾는 **동네 지번 색인** (run18 묶음 3)
  - 핫패치 = 가게 문을 안 닫고 **선반 하나만 새것으로 갈아 끼우기** (run17 2.7)
- run 문서는 "아파트·입주자 명부·카드" 비유를 썼다. 이 글은 run18 처럼 **가게 그림 그대로** 옮긴다. 가리키는 것은 같다.
  - 입주자 명부 → **건물 명부**, 카드 한 장 → **명부 한 줄** (+ 거기 붙은 연결 카드)

한 줄로 먼저 말하면:

- run18 은 "`+0x119d` 에서 **명부 첫 줄에 관리실(ntdll) 이름을 쓴다**" 까지만 봤다. 그 함수 안은 안 열었다.
- 이번에 열어 보니, 관리실은 **자기 명부 줄을 새로 한 장 만들고** (`+0x67`), **명부 두 권 + 이름 색인 카드함** (`+0xf2`), **색인함 둘** (`+0x111`) 에 꽂고, 진행 도장을 처음부터 **"다 끝남"(9)** 으로 찍는다 (`+0x150`) `[정적]`.
- run18 이 `[추측]` 으로 남긴 둘 — "명부 두 권에 넣는 곳은 이 안", "둘째 호출은 패치 ntdll 항목" — 이 **코드로 닫혔다** `[정적]`.
- 덤: 같은 "명부 넣기" 함수가 **본 건물(exe) 만 맨 앞**에 끼운다. 그래서 명부 순서는 "**exe, ntdll**" 이다 `[정적]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessW … NtCreateUserProcess … NtResumeThread     (run9 ~ run14, run34)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → … ═▶ IRETQ                        (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk → 로더 입구 네 함수 → 첫 스레드 판정                     (run17)
   └─ LdrpInitializeProcess   (가게 전체 세팅)
        ├─ 앞쪽 "살림 차리기"                                                    (run18)
        │    ├─ +0x1c1   PEB.Ldr = &PebLdr  (빈 명부)
        │    ├─ +0x11a2  LdrpInitializeNtdllDataTableEntry  (명부 첫 줄 = ntdll)   ← 이번 (run35), 그 안
        │    ├─ +0x126f  같은 함수 둘째 호출 (핫패치 때만)                         ← 이번, 그 안
        │    └─ +0x1283 ~ +0x12d7  초기화 순서 목록에 끼우기
        └─ 뒤쪽: exe 항목 (+0x1455) → kernel32 → import DLL → TLS             (run19 ~ run28)
   └─ NtContinue ═▶ RtlUserThreadStart → … → main                               (run16, run29·run30)
```

- 위치는 **커널이 test.exe 와 ntdll 을 땅에 놓아 준 뒤**, 유저 쪽 로더가 **아직 DLL 하나도 안 들여온** 때다.
- flow.md 로는 **2.4** 의 `LdrpInitializeProcess` 절, 그리고 그림 2 `(0) 프로세스 준비` 상자 안 `p4`·`p5` 상자다.

### 무엇이 궁금했나

run 문서 "목적" 의 여섯 질문이다.

1. 이 함수는 항목을 **새로 만드나**, 이미 있는 전역 칸을 **채우기만** 하나.
2. 명부의 **적재 순서·메모리 순서** 목록에 ntdll 을 넣는 곳이 `+0xf2 LdrpInsertDataTableEntry` 인가 (run18 `[추측]`).
3. `+0x111` 의 "색인" 은 무엇인가.
4. `+0x11f LdrpProcessMappedModule` 를 왜 ntdll 에도 부르나. 진행 도장(`State`)은 몇이 되나.
5. 둘째 호출 (`+0x126a`, 핫패치 때만) 은 무엇을 만드나.
6. 보통 프로세스에서 이 함수 뒤 명부에 **ntdll 하나뿐**인가.

### 바퀴 전에 알던 것

- `[정적]` (run18) `LdrpInitializeProcess +0x119d` (ret `+0x11a2`) 에서 이 함수를 **조건 없이** 부른다. 둘째 `+0x126a` (ret `+0x126f`) 는 `[ctx+0] ≠ 0` 일 때만.
- `[정적]` (run18) 명부(`PebLdr`)는 `+0x1c1` 에서 **빈 목록 셋**으로 PEB 에 걸린다. 초기화 순서 목록(`PebLdr+0x30`)엔 `+0x1283 ~ +0x12d7` 이 직접 끼운다.
- `[추측]` (run18) 적재·메모리 순서 목록(`+0x10`, `+0x20`)에 넣는 건 이 함수 안의 `LdrpInsertDataTableEntry` 로 **보인다**. 본문은 안 읽었다.
- `[추측]` (run18) 핫패치 때 둘째 호출은 "패치된 ntdll" 항목을 하나 더 만든다.
- `[관찰]` (run7) 그래프 켜기 때 **ntdll 카드는 층(GR 프레임)을 안 만들었다.** → ntdll 도장이 7 이 아니었다는 뜻, 9 로 짐작 `[추측]` (run27 해설 2.5).
- `[정적]` (run19·run26) 같은 짝 — `LdrpInsertDataTableEntry` → `LdrpInsertModuleToIndex(LockHeld)` → `LdrpProcessMappedModule` — 이 exe 항목과 dll1·dll2 길에도 나온다. 다만 "목록·색인에 넣는 일" 은 **이름으로만** 읽었다 `[추측: 이름]`. 색인이 무엇으로 찾는 표인지는 몰랐다 (run26 해설 2.4).

---

## 2. 찾은 것

### 2.1 한눈에 — 첫째 호출이 하는 일

run 문서 "하는 일 순서" 를 대표만 남겼다. 번호 `(1)~(4)` 는 flow.md 그림 2 의 화살표 번호와 같다.

```
LdrpInitializeNtdllDataTableEntry (0x7015c, 633B)     ← LdrpInitializeProcess +0x11a2 (첫째)
 ├─ +0x65   RtlImageNtHeaderEx                  ntdll NT 헤더 찾기
 ├─ (1) +0x6c   LdrpAllocateModuleEntry(0)      새 명부 줄 (0x138) + 연결 카드 (0x50)
 │  +0x78 ~ +0x93  "철거 금지" 표시, Flags |= 0x204
 │  +0xb6 ~ +0xd7  이름 둘 (FullDllName, BaseDllName)
 │  +0xdc   DllBase = ntdll 시작
 ├─ (2) +0xf7   LdrpInsertDataTableEntry        ★ 명부 두 권 (꼬리) + 이름 색인 카드함
 ├─ (3) +0x116  LdrpInsertModuleToIndex         ★ 색인함 둘 (잠금 안)
 ├─ (4) +0x124  LdrpProcessMappedModule(entry, 0, 1)   안에서 도장 2
 │  +0x150  DdagNode.State = 9                  ★ 값이 바뀌는 순간
 │  +0x154  LdrpNtDllDataTableEntry = 새 항목
 └─ +0x187  RET
```

- 오프셋은 **돌아올 자리(ret)** 다. 부르는 자리(call)는 5 바이트 앞이다: `+0x67`, `+0xf2`, `+0x111`, `+0x11f` `[정적]`.
- 비유로 한 줄: 관리실이 **자기 명부 줄을 한 장 만들어** → 이름·번지를 적고 → **명부 두 권과 이름 카드함**에 꽂고 → **색인함 두 개**에도 꽂고 → 다른 건물이 받는 **마무리 점검**을 받은 뒤 → 도장을 **"다 끝남"** 으로 찍는다.

### 2.2 한 줄씩 읽는 법

**줄: `├─ (2) +0xf7   LdrpInsertDataTableEntry`**

- 누가: `LdrpInitializeNtdllDataTableEntry`.
- 어디서: 부르는 자리 `+0xf2`, 돌아올 자리 **`+0xf7`**.
- 누구를: `LdrpInsertDataTableEntry` (rva `0xb380`, 274B).
- 무엇을 위해: 방금 만든 ntdll 명부 줄을 **적재 순서 목록(`PebLdr+0x10`)·메모리 순서 목록(`PebLdr+0x20`)** 의 꼬리와, **해시 표 `LdrpHashTable`** 에 끼운다 `[정적]`.
  - 이 줄이 바로 run18 이 `[추측]` 으로 남긴 그 자리다. 이번에 callee 본문까지 읽어서 `[정적]` 이 됐다 (2.5).
  - 이 순간부터 명부에 모듈이 **하나** 생긴다 (그 전엔 셋 다 빈 목록).

**줄: `│  +0x150  DdagNode.State = 9`**

- 누가: 같은 함수가 **호출 없이 직접** 쓴다. `+0x150 MOV [RCX+0x38],R14D` 이고, R14D 는 `+0xe0` 에서 9 로 잡아 둔 값이다 `[정적]`.
- 어디에: 연결 카드(`DdagNode`)의 `+0x38` 칸 = `State` (진행 도장).
- 무엇을 위해: ntdll 카드에 **"다 끝남"(9, `LdrModulesReadyToRun`)** 도장을 찍는다. 바로 앞 `(4)` 안에서 2 가 찍혔는데, 그걸 덮는다 (2.7).

### 2.3 질문 1 — 새로 만든다. 전역 칸은 "가리키는 쪽지" 다

**근거** `[정적]`

1. `+0x67` (ret `+0x6c`) `LdrpAllocateModuleEntry(0)` 가 새 명부 줄을 돌려준다 (RAX).
2. 그 RAX 가 RBX 가 되고, 그 뒤 칸 쓰기는 **모두 `[RBX+…]`** 다.
3. 전역 `LdrpNtDllDataTableEntry` 는 그 줄을 **가리키는 포인터 칸(8B)** 이다. 성공 길 끝 `+0x154` 에서 **딱 한 번** 새 줄 주소를 적는다.
   - 중간에 실패하면 `+0x159 JS` 로 빠져서 안 적는다.
4. caller 도 이 칸을 포인터로 쓴다: `LdrpInitializeProcess +0x127c` 에서 값을 읽고, `+0x1283 ADD RAX,0x20` 으로 줄 안의 "초기화 순서 연결 칸" 을 찾는다.

**그래서**

- 답: **새로 만든다.** 전역은 "명부 줄 그 자체" 가 아니라 "**명부 줄은 저기 있음**" 이라고 적은 쪽지다.
- 비유: run19 에서 본 건물(exe)도 같은 식이었다. `LdrpImageEntry` 라는 벽 주소록에 "본 건물 명부 줄은 여기" 를 한 번 적었다 ([run19 해설 2.6](run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1)). 관리실 것은 `LdrpNtDllDataTableEntry` 다.
- `LdrpAllocateModuleEntry` 는 **명부 줄(0x138)과 연결 카드(0x50)를 한 쌍으로** 만든다 ([run22 해설 2.7](run22-map-snap-dep-해설.md)). 인자 0 = 작업 지시서(로드 문맥) 없음 `[정적]`.

### 2.4 이름 두 개 — 그리고 run18 정정 하나

명부 줄에는 이름 칸이 둘 있다. 둘 다 [쪽지(`UNICODE_STRING`)](run19-proc-init-modules-해설.md#21-run18-정정-하나--exe-폴더가-아니라-exe-파일-이름) 모양이다.

**`FullDllName` (`+0x48`) — 전체 경로** `[정적]`

1. 넷째 인자 R9 가 가리키는 **쪽지 칸** (caller 의 `[RSP+0xc0]`) 을 값으로 통째 복사한다 (`+0xb6 ~ +0xc7`, 16바이트).
   - 쪽지만 베끼니까 **글자 버퍼는 caller 의 힙 버퍼를 같이 쓴다.**
2. `+0xcb` (ret `+0xd0`) `RtlAppendUnicodeStringToString` 으로 그 뒤에 `ntdll.dll` 을 붙인다.
3. 결과 = `<RtlGetNtSystemRoot() 값>\SYSTEM32\ntdll.dll`. 루트 값 자체는 실행 때 값이다 (동적).

그 쪽지 칸은 caller 가 미리 만들어 둔다 (`LdrpInitializeProcess +0xcdc ~ +0xd4a`) `[정적]`:

- `RtlGetNtSystemRoot` → 힙 버퍼 (크기 = 루트 길이 + `0x28`) → 루트 문자열 + 상수 **`\SYSTEM32\`** (대문자, 20바이트).
- 여유 `0x28` = `\SYSTEM32\`(20) + `ntdll.dll`(18) + 널(2).
- 즉 caller 가 **`ntdll.dll` 이 붙을 자리까지 미리 비워 둔** 버퍼를 넘긴다. 그래서 이 함수는 새 버퍼 없이 뒤에 붙이기만 하면 된다.
- 비유: 이름 칸이 넉넉한 **빈 명찰**을 받아서, 끝에 "ntdll.dll" 만 써 넣는 것.

**`BaseDllName` (`+0x58`) — 파일 이름만** `[정적]`

- ntdll `.rdata` 의 상수 `NtDllName` (`ntdll.dll`) 쪽지를 복사한다 (`+0xd0 ~ +0xd7`). 글자는 `.rdata` 를 그대로 가리킨다.

**run18 정정 — R9 는 "문자열" 이 아니라 "쪽지 칸의 주소"** `[정적]`

- run18 (과 그 해설 2.3 줄 2) 은 넷째 인자를 "`<NtSystemRoot>\System32\` **문자열**" 이라고 적었다.
- 정확히는: R9 = **`UNICODE_STRING` 칸의 주소** (`&[RSP+0xc0]`). 이 함수는 그 칸의 헤더(쪽지)를 값으로 복사해서 `FullDllName` 으로 쓴다.
- 폴더 상수도 소문자 `System32` 가 아니라 대문자 **`\SYSTEM32\`** 다 (`SlashSystem32SlashString`, `.rdata` 원본 바이트).
- 비유: "명찰을 건넸다" 가 아니라 "**명찰이 놓인 서랍 번호**를 건넸다" 였다. 받은 쪽은 서랍을 열어 명찰 내용을 베낀다.

### 2.5 새 줄 손질 — 비트 하나, 도장 둘

`+0x78 ~ +0x93` 에서 몇 칸을 손본다 `[정적]`.

**`Flags |= 0x204` — 실제로 새로 켜지는 건 하나**

- `0x204` = 비트 2 `ImageDll` + 비트 9 `InExceptionTable` (PDB 비트 위치).
- 그런데 `ImageDll` 은 `LdrpAllocateModuleEntry +0x11a` 가 **이미 켠다.** 그러니 새로 켜지는 건 **`InExceptionTable` 하나**다.
- 이 비트가 하는 일 `[정적]`: 뒤 `(4) LdrpProcessMappedModule` 안 `+0x15e BT EDX,9` 에서 이 비트를 보고, **`+0x177 RtlInsertInvertedFunctionTable` (역함수 표 등록) 을 건너뛴다.**
- 왜 건너뛰나 `[추측]`: ntdll 은 **동네 지번 색인(역함수 표)** 에 이미 올라가 있다고 보인다. 그래서 두 번 올리지 않게 "이미 올림" 표시를 미리 켜 두는 것.
  - 후보 둘:
    - 결과서: `LdrpInitializeProcess +0x45e` 근처 `LdrpInvertedFunctionTables` 쓰기
    - flow-writer: run18 이 이미 적은 `+0x415` / `+0x41a RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)` — [run18 해설 묶음 3](run18-proc-init-setup-해설.md) 의 "관리실 건물부터 지번 색인에 올린다" 가 바로 그것
  - 어느 쪽인지는 **Q67 (a)**.

**"철거 금지" 도장 — exe 와 같다**

- `LoadCount = -1`, `ObsoleteLoadCount = 0xffff`.
- "안 내려가는 모듈" 표시로 보인다 `[추측]`. exe 항목도 같은 값이다 (run19). [run19 해설 2.6](run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1) 의 "철거 금지" 도장과 같은 것.

**`LoadReason` — 안 쓴다 → 0**

- [적재 이유 칸](run21-load-dll-internal-해설.md#210-run20-질문-둘이-풀렸다--상태-칸-그리고-넷째-인자-4) 은 이 함수가 안 쓴다. 그래서 0 (`LoadReasonStaticDependency`) `[정적]`.
- `LdrpAllocateModuleEntry +0x16b` 가 4 를 쓰는 길은 지시서가 있을 때만이고, 인자 0 이라 `+0x7d JZ` 로 건너뛴다 `[정적]`.

**나머지 칸은 0**

- 새 줄의 다른 칸은 0 이다. 힙 플래그 `|8` 을 "0 으로 채움" 으로 읽은 데 기댄 것이라 `[추측: 공개 문서]`.

**곁가지 — `OriginalBase` 를 두 번 쓴다**

- 첫째 호출은 `+0x9f ~ +0xa6` 에서 `OriginalBase` 에서 `LdrSystemDllInitBlock+0x10` 을 뺀다 `[정적]`.
- 그런데 곧 `LdrpProcessMappedModule +0x97` 이 같은 칸을 NT 헤더 `ImageBase` 로 덮는다 `[정적]`.
- 코드 순서로는 뺀 값이 안 남는 것으로 읽힌다 `[추측]`. 실제 값은 **Q44 (f)**.

### 2.6 질문 2 — 명부 두 권에 꽂는 곳 = `+0xf2` — **예**

이제 `LdrpInsertDataTableEntry` 본문 안이다 (rva `0xb380`, 274B) `[정적]`.

**하는 일, 순서대로**

1. `+0x6` / `+0xd`: `Flags` 비트 6 `InLegacyLists` 가 **이미 켜져 있으면** 아무것도 안 하고 끝. (같은 줄을 두 번 꽂지 않게)
2. `+0xf ~ +0x22`: 이름 해시값 칸(`BaseNameHashValue`, `+0x108`)이 0 이면 `+0x1d LdrpHashUnicodeString(&BaseDllName)` 으로 구해 둔다.
3. `+0x28 ~ +0x7a`: **이름 색인 카드함 `LdrpHashTable`** 에 꽂는다.
   - **새 말 — 버킷**: 해시 표의 **칸 하나**. 칸 번호 = 해시값 `& 0x1f` → 32칸 중 하나 (칸당 16바이트) `[정적]`.
   - 비유: 카드함의 **서랍 32개**. 이름에서 서랍 번호를 계산하고, 그 서랍 **맨 뒤**에 카드를 넣는다 (`HashLinks`, `+0x70`).
4. `+0x69 CMP RBX,[LdrpImageEntry]`: 이 줄이 **본 건물(exe) 줄인가** 를 본다.
   - **아니면** (`+0x87 ~ +0xaf`, `+0x4d ~ +0x57`): 적재 순서 목록(`PebLdr+0x10`)과 메모리 순서 목록(`PebLdr+0x20`)의 **꼬리**에 끼운다.
   - **맞으면** (`+0xbd ~ +0x10e`): 두 목록의 **맨 앞**에 끼운다.
5. `+0x5e OR [RBX+0x68],0x40`: `InLegacyLists` 비트를 켠다 ("명부에 꽂음" 표시).
6. 끼우는 중 이웃 연결이 안 맞으면 `+0x4b INT 0x29` → `__fastfail(3)` (프로세스를 바로 끝냄, [run18 해설 2.10](run18-proc-init-setup-해설.md#210-일찍-끝나는-길--19--5)).

- 비유: 명부 줄 하나를 받으면, **이름 서랍**에 카드를 넣고, 명부 **두 권**(들어온 순서·메모리 순서)에 줄을 꿰어 넣는다. 그런데 **본 건물만은 두 권 다 맨 앞 장**에 꿴다.
- **초기화 순서 목록(`PebLdr+0x30`, 셋째 권)은 이 함수가 안 만진다.** 그건 caller `LdrpInitializeProcess +0x1283 ~ +0x12d7` 이 직접 한다 (run18).

**ntdll 은 꼬리, exe 는 맨 앞 — 왜 그렇게 되나, 단계별로**

1. 근거 `[정적]`: `LdrpImageEntry` 를 **처음 쓰는 곳**은 `LdrpInitializeProcess +0x1374` 다 (run19).
2. 근거 `[정적]`: ntdll 이 이 함수를 타는 건 `+0x119d` 안쪽이다. `+0x1374` 보다 **앞**이다.
3. 그래서: ntdll 때는 `LdrpImageEntry` 가 아직 0 → `+0x69` 비교가 "다름" → **꼬리** 쪽.
   - 목록이 비어 있으니 꼬리에 넣어도 결과는 "ntdll 하나뿐" 이다.
4. 근거 `[정적]`: exe 줄은 `+0x1374` 에서 `LdrpImageEntry` 에 들어간 **뒤** `+0x1450` (ret `+0x1455`) 에서 이 함수를 탄다.
5. 그래서: exe 때는 `+0x69` 비교가 "같음" → **맨 앞**.
6. 결론 `[정적]`: **`+0x1455` 뒤 적재·메모리 순서 = exe, ntdll.**
7. 나머지 DLL (dll1·dll2 등, `LdrpMapDllWithSectionHandle +0x146`) 은 꼬리 쪽이다 `[정적: 코드 모양, 어느 모듈이 타는지는 Q23]`.

- 비유: 관리실이 먼저 와서 명부 첫 장에 이름을 썼다. 그런데 나중에 온 본 건물은 **"내 자리는 무조건 맨 앞"** 규칙이 있어서, 관리실 **앞에** 끼어든다. 그 뒤 들어오는 건물들은 차례로 뒤에 붙는다.
- 이건 run19 해설의 "**ntdll + exe**" 를 **좁힌** 것이다. 들어온 **시각**은 ntdll 이 먼저지만, 명부 두 권의 **줄 순서**는 exe 가 먼저다.

### 2.7 질문 3 — 색인 = 붉은-검은 트리 둘

**껍데기와 몸통** `[정적]`

- `LdrpInsertModuleToIndex(entry, NT 헤더)` (rva `0x703dc`, 61B) 는 **잠금만 잡아 주는 껍데기**다.
  1. `+0x10 ~ +0x17` `LdrpModuleDatatableLock` 을 혼자 잡는다 (`RtlAcquireSRWLockExclusive`).
  2. `+0x22` (ret `+0x27`) `LdrpInsertModuleToIndexLockHeld` — 실제 일.
  3. `+0x38` 꼬리 `JMP RtlReleaseSRWLockExclusive` 로 잠금을 푼다.
- `...LockHeld` = **잠금을 이미 쥔 쪽**이 바로 부르는 몸통. dll1·dll2 길의 `LdrpMapDllWithSectionHandle` 은 잠금을 스스로 잡고 이쪽을 바로 부른다 (`+0x151`, run26).
- [잠금](run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다) 비유: 색인함 **열쇠**. 관리실 것은 껍데기가 열쇠를 대신 잡아 주고, 건물 담당(`LdrpMapDllWithSectionHandle`)은 이미 열쇠를 쥐고 와서 바로 넣는다.

**몸통이 하는 일** `[정적]`

1. NT 헤더의 `TimeDateStamp`·`CheckSum`·`SizeOfImage` 를 명부 줄로 옮긴다 (`+0xa ~ +0x2e`, 결과서).
2. **`+0x91 RtlRbInsertNodeEx(&LdrpMappingInfoIndex, …)`** — 키 = (`TimeDateStamp`, `SizeOfImage`). 줄 안 칸 `MappingInfoIndexNode` (`+0xe0`).
3. **`+0xe7 RtlRbInsertNodeEx(&LdrpModuleBaseAddressIndex, …)`** — 키 = `DllBase`. 줄 안 칸 `BaseAddressIndexNode` (`+0xc8`).
4. `+0xec BTS [RDI+0x68],7` — `Flags` 비트 7 `InIndexes` 켬 ("색인에 꽂음" 표시).

- 두 트리 이름은 PDB 전역 이름이다 (`.asm` 주석). ntdll 에서 둘째 인자 NT 헤더는 `+0x109` 가 실은 `[RSP+0x30]` (= `+0x60 RtlImageNtHeaderEx` 결과) `[정적]`.

**새 말 — 붉은-검은 트리**

- 값을 **크기 순으로 정렬해 두고**, 넣거나 찾을 때 한쪽으로 너무 기울지 않게 스스로 균형을 맞추는 나무 모양 자료 구조라고 알려져 있다 `[추측]` (일반적으로 알려진 설명). 그래서 항목이 많아도 찾는 데 오래 안 걸린다.
- 여기서 확인된 건 "`RtlRbInsertNodeEx` 로 넣는다" 와 "줄 안에 넣을 칸이 따로 있다" 까지다 `[정적]`.
- 비유: 명부 두 권은 **처음부터 넘겨 보는 책**이다. 색인함 둘은 **정렬된 카드 상자**다.
  - **번지 순 색인함** (`LdrpModuleBaseAddressIndex`): "이 번지에 있는 건 어느 건물 줄?" 을 빨리 찾는다.
  - **준공일·평수 색인함** (`LdrpMappingInfoIndex`): 건물의 **만든 날(TimeDateStamp)과 크기(SizeOfImage)** 로 "이 건물 이미 명부에 있나?" 를 찾는다.
- 주의 — **run18 의 "동네 지번 색인"(역함수 표) 과는 다른 것**이다.
  - 역함수 표 = `RtlInsertInvertedFunctionTable` 로 넣는 표 (예외 처리 쪽, 2.5).
  - 번지 순 색인함 = `RtlRbInsertNodeEx(&LdrpModuleBaseAddressIndex, …)` 로 넣는 트리 (로더 명부 쪽).
  - 둘 다 "번지 → 건물" 처럼 보이지만, 넣는 함수도 칸도 다르다 `[정적]`.

**run26 의 물음표가 풀렸다**

- [run26 해설](run26-map-dll-file-해설.md) 2.4 는 dll1·dll2 길의 `+0x151` "색인" 이 **무엇으로 찾는 표인지 근거에 없다**고 했다.
- 같은 몸통(`LockHeld`)이니, 그 색인도 **이 트리 둘**이다 `[정적]` (함수 본문 기준).

### 2.8 질문 4 — 마무리 점검을 같이 받고, 도장은 9

**왜 ntdll 에도 `LdrpProcessMappedModule` 을 부르나**

- 인자 `(entry, 0, 1)` (`+0x116 ~ +0x11b`) `[정적]`.
- 이유 한 줄: **DLL 이 메모리에 올라오면 누구나 거치는 마무리를 ntdll 줄도 같은 함수로 하려는 것.** ntdll 은 이미 올라와 있으니 "올리기" 단계만 건너뛰고 여기부터 탄다 `[추측: 뜻, 줄은 정적]`.
- 비유: 다른 건물은 **배달 → 설치 → 마무리 점검** 순서다 (run26). 관리실은 커널이 이미 설치해 줬으니, **마무리 점검만** 같은 점검원에게 받는다.

**점검원이 ntdll 줄에 하는 일** (결과서가 훑은 줄, 본문 전부는 아님) `[정적]`

1. `+0x4a ~ +0x78` `EntryPoint`(`+0x38`) = 시작 주소 + `AddressOfEntryPoint`.
   - **ntdll 의 `AddressOfEntryPoint` 는 0** (PE 헤더) → `+0x68` 에서 0 쪽 → **`EntryPoint = 0`**.
   - 비유: 관리실 건물엔 "**전원 스위치**"(DLL 진입점)가 없다.
2. `+0x83 LdrpValidateEntrySection`, `+0x97 OriginalBase` = NT 헤더 `ImageBase`, `+0xa6 LoadTime`.
3. `LdrInitSecurityCookie`·`LdrpCfgProcessLoadConfig` (결과서 `+0xd9 ~ +0x107`, `+0x145`).
4. `+0x15e` 비트 9 (`InExceptionTable`) 가 켜져 있어 `RtlInsertInvertedFunctionTable` 을 **건너뜀** (2.5).
5. 잠금 안 **`+0x1c2 State = 2`** (`LdrModulesMapped`, "설치됨" 도장).
   - `+0x1dd LdrpSignalModuleMapped` (부모에게 도착 알림, run24) 는 `LoadContext ≠ 0` 일 때만이라 ntdll 은 건너뜀. 지시서가 없으니 알릴 부모도 없다.

**최종 도장 = 9 — 단계별로**

1. 근거 `[정적]`: 점검원이 안에서 2 를 찍는다 (`+0x1c2`).
2. 근거 `[정적]`: 돌아온 뒤 `+0x150 MOV [RCX+0x38],R14D` (R14D = 9) 가 **2 를 덮는다.**
3. 그래서 `[정적]`: ntdll 카드의 최종 도장 = **9 (`LdrModulesReadyToRun`, "다 켜짐")**.
4. 다른 건물과 비교:
   - exe·DLL 은 도장 **2 에서 시작**해서, 그래프 순회로 한 단계씩 올라간다 ([run21 해설 2.6 도장 표](run21-load-dll-internal-해설.md#26-연결-카드의-진행-도장--노드-상태-번호-5--9)).
   - **ntdll 은 처음부터 "다 끝난" 모듈로 명부에 오른다.**
5. 이게 run7 `[관찰]` 과 맞물린다:
   - 그래프 켜기(GR)는 **도장 7 인 자식만** 재귀한다 (run27 `[정적]`).
   - ntdll 카드는 9 니까 GR 이 층을 안 만든다.
   - 그래서 run7 콜스택에 "ntdll 노드 GR 프레임이 없었다" `[관찰]` 이 이제 **코드로 설명된다.** run27 해설 2.5 의 "9 로 짐작 `[추측]`" 이 `[정적]` 이 됐다.

- 비유: 다른 건물은 연결 카드에 "설치됨" 도장부터 찍고 하나씩 올라간다. 관리실 카드는 점검원이 "설치됨" 을 찍고 나가자마자 관리실이 그 위에 **"다 켜짐"** 을 덮어 찍는다. 그래서 나중에 전원 켜기 담당이 카드판을 돌 때 관리실 카드는 **그냥 지나친다.**

### 2.9 질문 5 — 둘째 호출 = "패치된 ntdll" 줄 — **예**

**먼저 — 뒤쪽 `+0x1ab ~ +0x23e` 은 한 덩어리가 아니라 네 길이다** `[정적]`

같은 함수 안의 연속 범위다 (`.asm` 에 조각 머리 없음). 요청서는 "함수 끝 뒤 cold 블록" 으로 짐작했지만 아니었다.

| 길 | 어디서 오나 | 하는 일 |
|---|---|---|
| 실패 로그 `+0x189 ~ +0x1b5` | `+0x72 JZ` — 줄 만들기 실패 | 상태 `0xc0000017` 로 끝. **핫패치와 무관** |
| 패치 이름 `+0x1b7 ~ +0x20d` | `+0xb0 JZ` — **R9 = 0** (둘째) | 이름을 **메모리에 올라온 파일 이름**에서 얻음 → `+0xdc` 공통 길로 합류 |
| 의존 간선 `+0x22c ~ +0x275` | `+0x143 JNZ` — **R8 ≠ 0** (둘째) | 원래 ntdll → 패치 줄 간선, `HotPatchState` 쓰기 |
| 정리 `+0x212 ~ +0x21b` | `+0x159 JS` / `+0x249 JS` — 뒤 단계 실패 | `FullDllName` 버퍼 풀고 반환 |

- 표 읽는 법: "어디서 오나" 의 R8·R9 가 **둘째 호출의 표시**다. 이 함수 안의 "패치 모드" 판단은 인자 R8·R9 로만 한다 `[정적]`.

**둘째 호출에서 인자가 어떻게 다른가** `[정적]`

| 인자 | 첫째 (`+0x119d`) | 둘째 (`+0x126a`) |
|---|---|---|
| RCX = 이미지 시작 | ntdll 시작 | 패치 이미지 시작 (`[LdrpNtdllHotPatchContext]` 의 `[+0]`) |
| RDX = 새 줄 받을 칸 | `&LdrpNtDllDataTableEntry` (전역) | `&[RSP+0x160]` (caller 지역) |
| R8 = 원래 ntdll 줄 | 0 | `[LdrpNtDllDataTableEntry]` 값 |
| R9 = 이름 쪽지 칸 주소 | `&[RSP+0xc0]` | 0 |

- 인자 이름은 결과서가 붙인 것이다 `[추측: 이름만, 쓰임은 코드]`.

**둘째 호출이 첫째와 다른 점** `[정적]`

1. **이름 출처가 파일이다.**
   - `+0x1df NtQueryVirtualMemory(-1, 패치 시작, 종류 2, 버퍼 0x218, …)` → `+0x1fe LdrpResolvePatchDllName`.
   - `FullDllName` = 파일 이름 전체의 힙 복사, `BaseDllName` = 끝 이름.
   - 종류 2 는 `+0x1cd MOV R8D,2` 다. 그 이름이 `MemoryMappedFilenameInformation` 이라는 건 `[추측: 공개 문서]`.
2. `OriginalBase` 빼기를 안 한다 (`+0x9d JNZ`).
3. `LoadReason` = 9 (`LoadReasonPatchImage`, PDB) — `+0xe9 JNZ` → `+0x220`.
4. **의존 간선**: `+0x23e LdrpRecordModuleDependency(원래 ntdll 줄, 패치 줄, 0, &상태)`.
   - [간선](run22-map-snap-dep-해설.md) = 연결 카드 사이에 긋는 **끈**. 원래 쪽 `Dependencies`, 패치 쪽 `IncomingDependencies` 에 하나씩.
   - 비유: "원래 관리실은 **새 선반**에 기댄다" 는 끈을 긋는다.
5. 성공하면 (`+0x24b ~ +0x275`):
   - 패치 줄 `ParentDllBase` = 원래 `DllBase`
   - 원래 줄 `HotPatchState` = 3 (`LdrHotPatchAppliedForward`), `ActivePatchImageBase` = 패치 `DllBase`
   - 패치 줄 `HotPatchState` = 2 (`LdrHotPatchAppliedReverse`)
   - (`HotPatchState` = 줄마다 "핫패치에서 내 역할" 을 적는 칸. 값 이름은 PDB 열거 `_LDR_HOT_PATCH_STATE`.)
6. 결과를 전역이 아니라 caller 지역 칸에 돌려준다.

**같은 점** `[정적]`

- 패치 줄도 `+0xf2` (명부 두 권 + 이름 카드함) 와 `+0x111` (색인함 둘) 을 **똑같이 탄다** (`+0x220` 에서 `+0xef` 로 되돌아옴).
- 그래서 핫패치가 켜지면 명부·카드함·색인함에 ntdll 계열 줄이 **둘**이다.

**타는 조건** — caller 쪽 하나뿐이다: `+0x1250 MOV RCX,[RAX]` / `+0x1256 JZ` (`[ctx+0] ≠ 0`, run18 과 같음) `[정적]`.

- 보통 프로세스(핫패치 꺼짐)에서 안 탄다는 건 아직 `[추측]` 이다. test.exe 때 값은 **Q44 (e)**.
- 비유: [run18 해설 2.7](run18-proc-init-setup-해설.md#27-명부-첫-줄--ntdll-항목-그리고-두-번-부르는-이유) 의 "선반 갈아 끼운 새 관리실이 따로 와 있으면 그 이름도 쓴다" 가 맞았다. 그 새 관리실 이름은 **문서(상수)가 아니라 실제로 놓인 선반의 라벨(파일 이름)** 에서 읽고, 원래 관리실과 끈으로 잇는다.

**run18 정정 — 패치 줄은 초기화 순서 목록에서 ntdll "앞"** `[정적]`

1. 근거: `+0x1283 ~ +0x12d7` 의 두 끼우기 — `+0x129f ~ +0x12aa` (ntdll), `+0x12cc ~ +0x12d7` (패치) — 는 **둘 다** `PebLdr+0x30` 의 `Flink` 를 새 줄로 쓴다. 곧 **머리 끼우기**다.
2. 근거: 패치 쪽이 **나중**이다.
3. 그래서: 맨 앞에 마지막으로 끼어든 패치 줄이 ntdll **앞**에 선다.
4. 정리 (핫패치 때):
   - 적재·메모리 순서 (`+0xf2`, 꼬리 끼우기): ntdll → 패치 (패치가 **뒤**)
   - 초기화 순서 (`+0x1283 ~ +0x12d7`, 머리 끼우기): 패치 → ntdll (패치가 **앞**)

- run18 은 "둘째 항목이 있으면 그것도 끼운다" 라고만 해서 **자리**가 빠져 있었다. sast/13 결과서의 "둘째로 끼움" 은 "두 번째 호출로" 라는 뜻으로만 맞다.
- 비유: 줄 맨 앞에 "끼어들기" 를 두 번 하면, **나중에 끼어든 사람이 맨 앞**이다.

### 2.10 질문 6 — 보통 프로세스에서 명부에 ntdll 하나 — **맞다**

**단계별로** `[정적]`

1. 명부 머리는 `LdrpInitializeProcess +0x16d ~ +0x1a5` 에서 빈 목록으로 생기고, `+0x1c1` 에서 PEB.Ldr 에 걸린다 (run18).
2. **첫 꽂기가 이 함수 `+0xf2`** 다.
3. `LdrpInsertDataTableEntry` 의 다른 호출 자리 셋 — `LdrpInitializeProcess +0x1450`·`+0x1625`, `LdrpMapDllWithSectionHandle +0x141` — 은 모두 `+0x119d` 보다 **뒤**다.
4. `PebLdr+0x10`·`+0x18` 을 쓰는 줄도 이 함수(`+0xeb`·`+0xaf`)와 초기화뿐이다 (`_refs/PebLdr.tsv`, 결과서).
5. 그래서: **"ntdll 뿐" 은 `+0x11a2` 부터 `+0x1450` 까지**다. `+0x1455` 뒤엔 "exe, ntdll".

- run18 결론 "앞쪽 끝 명부엔 ntdll 뿐" 이 **맞았고, 구간이 더 정확해졌다.**
- 핫패치 때 "패치 ntdll 하나 더" 는 `[추측]` → `[정적]` (2.9).

### 2.11 정정 모음 — 짧게

run 문서 "정정" 절 그대로다. 앞 문서들 맨 위에 한 줄씩 들어갔다.

| 어디 | 예전 | 이번 |
|---|---|---|
| run18 첫째 호출 R9 | `<NtSystemRoot>\System32\` **문자열** | **`UNICODE_STRING` 칸의 주소** (`&[RSP+0xc0]`), 상수는 대문자 `\SYSTEM32\` (2.4) |
| run18 초기화 순서 목록의 둘째 항목 | "그것도 끼운다" (자리 없음) | 머리 끼우기 둘 → 패치 줄이 ntdll **앞** (2.9) |
| run18 "명부 두 권은 첫째 호출 안", "핫패치 땐 하나 더" | `[추측]` | `[정적]` — 틀린 건 아니고 태그만 |
| run22 (와 flow.md 2.6) `LdrpAllocateModuleEntry` caller 목록 | `LdrpInitializeNtdllDataTableEntry +0x67` (ret 로 적힘) | `+0x67` 은 **call** 자리. 같은 기준(ret)이면 **`+0x6c`** |

### 2.12 비유로 다시 한 번

1. 관리실은 커널이 **이미 땅에 놓아 준** 건물이다. 그래도 명부엔 자기 줄이 있어야 한다.
2. 그래서 **새 명부 줄 + 연결 카드를 한 쌍** 만든다 (`+0x67`).
3. 이름은 caller 가 넉넉히 비워 준 명찰 끝에 `ntdll.dll` 을 써 넣고 (`FullDllName`), 짧은 이름은 상수에서 베낀다 (`BaseDllName`).
4. "**지번 색인엔 이미 올림**" 표시(`InExceptionTable`)를 미리 켠다.
5. **명부 두 권(꼬리) + 이름 서랍** 에 꽂는다 (`+0xf2`). 이 순간 명부에 관리실 하나.
6. **번지 순·준공일 순 색인함** 에도 꽂는다 (`+0x111`).
7. 다른 건물과 같은 **마무리 점검**을 받는다 (`+0x11f`). 전원 스위치는 없다 (`EntryPoint` 0). 점검원이 "설치됨"(2) 도장을 찍는다.
8. 관리실이 그 위에 **"다 켜짐"(9)** 을 덮어 찍는다 (`+0x150`).
9. 벽 주소록 `LdrpNtDllDataTableEntry` 에 "관리실 줄은 여기" 라고 적는다 (`+0x154`).
10. 나중에 온 본 건물(exe)은 "**맨 앞 자리 규칙**" 으로 관리실 앞에 끼어든다 (`+0x1455`).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 그림 2 `p4` 상자 | "(정적: ntdll 모듈 항목, 조건 없음)" | + 매핑 안 함, 이름 `루트\SYSTEM32\ntdll.dll`, `EntryPoint` 0, `+0x150 State = 9`, `+0x154 LdrpNtDllDataTableEntry = 새 항목` |
| 그림 2 `p4` 아래 | 없음 | `(정적)` 상자 넷: `(1) +0x6c LdrpAllocateModuleEntry` / `(2) +0xf7 LdrpInsertDataTableEntry` / `(3) +0x116 LdrpInsertModuleToIndex` / `(4) +0x124 LdrpProcessMappedModule` |
| 그림 2 `p5` (둘째 호출) | "패치 ntdll 항목 **추정**" | "패치 ntdll 항목 — 이름은 매핑 파일, `LoadReason` 9, 원래 ntdll → 패치 의존 간선" (추정 뗌. 타는지는 그대로 동적) |
| 그림 2 `p6` (exe 항목) | `BaseDllName = test.exe` 까지 | + "`+0x1455` 명부 넣기에서 exe 만 두 목록 맨 앞" |
| 2.4 "앞쪽 끝 모듈 목록 = ntdll 뿐" | 두 목록에 넣는 건 그 함수 안으로 보임 `[추측]` | `[정적]` `+0xf2 LdrpInsertDataTableEntry`. "ntdll 뿐" 구간 = `+0x11a2 ~ +0x1450` |
| 2.4 둘째 `+0x126a` | 핫패치 때만, 안 탄다 `[추측]` | + `[정적]` 타면 패치 ntdll 줄. 적재·메모리 순서에선 ntdll 뒤, 초기화 순서에선 ntdll **앞** |
| 2.4 exe 항목 | 목록·색인에 넣는 일로 보임 `[추측: 이름]` | `[정적]` (callee 본문). exe 는 두 목록 맨 앞 → 순서 **exe, ntdll** |
| 2.4 새 `####` 절 셋 | 없음 | `LdrpInitializeNtdllDataTableEntry` / `LdrpInsertDataTableEntry` / `LdrpInsertModuleToIndex` |
| 2.6 dll1·dll2 명부 넣기 | `+0x146` 이 그 순간 `[정적]` | + 그 함수 본문: 꼬리 + 해시 표 (exe 만 머리) |
| 2.6 `LdrpAllocateModuleEntry` caller | `LdrpInitializeNtdllDataTableEntry +0x67` | **`+0x6c`** (정정) |
| 2.7 run7 "ntdll 노드는 GR 프레임 없음" | 9 로 짐작 `[추측]` | `[정적]` `+0x150` State = 9. `[관찰]` 과 맞음 |
| 4장 2.4 Q45 줄 | 할 일 | **해결됨**. 새 줄: Q67 (작은 정적 셋) |

### 큰 그림에서 어떻게 이어지나

- run18 은 "명부 첫 줄에 관리실 이름" 을 **바깥에서** 봤다. 이번엔 **안에서** 봤다.
- 이번 결과로 **같은 네 단계가 세 번** 나온다는 게 보인다.
  - 관리실(ntdll): `+0x6c` 줄 만들기 → `+0xf7` 명부 → `+0x116` 색인 → `+0x124` 마무리 점검 (run35)
  - 본 건물(exe): `+0x135e` → `+0x1455` → `+0x14d2` → `+0x14ee` (run19)
  - 다른 건물(dll1·dll2): `LdrpMapDllWithSectionHandle` 안 `+0x146` 명부 → `+0x151` 색인 → … → `+0x26c` 마무리 (run26)
  - 다른 점은 **명부 자리**(exe 만 맨 앞)와 **시작 도장**(ntdll 은 처음부터 9) 정도다 `[정적]`.
- 다음 구간과의 연결:
  - kernel32 를 들일 때 이름 카드함을 뒤지면 아직 **관리실·본 건물 두 장뿐**이다 (run20). 그 두 장이 어디서 꽂혔는지가 이제 둘 다 코드로 보인다.
  - 전원 켜기(그래프 순회, run27) 가 ntdll 카드를 **건너뛰는 이유**가 이번 `+0x150` 이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 첫째 호출의 실제 인자 — RCX = `lm m ntdll` 시작인가, RDX = `LdrpNtDllDataTableEntry` 주소, R8 = 0, R9 = 스택 주소, 쪽지 내용(루트 + `\SYSTEM32\`) | 루트 문자열·주소는 실행 때 값 | **Q44 (a)** (동적, `ntdll+0x7015c` 입구) |
| 새 줄이 막 생겼을 때 모양 (이름 비어 있음, 도장 0) | 실행 때 값 | **Q44 (b)** (`+0x6c`, `@rax`) |
| `+0xf7` 직후 명부 두 권이 정말 ntdll 한 칸씩인가, 이름 서랍 번호 | 목록 내용·해시값은 실행 때 값 | **Q44 (c)** (`+0xf7`, 새 줄은 **RBX**) |
| 끝나기 직전 ntdll 줄 전체 (이름 둘, 도장 9, `Flags` 비트 2·6·7·9, `EntryPoint` 0, `LoadReason` 0) | 실행 때 값 | **Q44 (d)** (`+0x171`) |
| test.exe 에서 둘째 호출을 정말 안 타나 | `[ctx+0]` 값은 실행 때 값 | **Q44 (e)** (`+0x126a` 에 안 서는지, 또는 `+0x1250` 에서 `rcx` = 0) |
| `OriginalBase` 의 두 번 쓰기 — 뺀 값이 정말 안 남나 | 코드 순서로만 읽음 `[추측]` | **Q44 (f)** (덤) |
| ntdll 예외 표(역함수 표)를 등록하는 곳 — `InExceptionTable` 을 미리 켜는 이유의 짝 | 후보 둘 중 어느 쪽인지 이번 범위 밖 | **Q67 (a)** (정적. run18 의 `+0x41a` 면 거의 풀린 셈) |
| `LdrpInitializeProcess +0x1625` (ret `+0x162a`) 의 `LdrpInsertDataTableEntry` 가 꽂는 줄의 정체 (AppVerifier 묶음의 복제 항목?) | 그 자리 본문은 이번 질문 밖 | **Q67 (b)** (정적) |
| `LdrpLoadPatchedNtdll` (run17) 이 `[ctx+0]` 에 쓰는 것과 패치 줄·`LdrpNtdllHotPatchContext` 의 연결 | 핫패치 켜진 경우만, 이번 범위 밖 | **Q67 (c)** (정적) |
| dll1·dll2 가 정말 꼬리 쪽으로 들어가나, 실제 명부 순서 | 어느 모듈이 그 줄을 타는지는 실행 때 | **Q23** (동적) |

- 디버거로 볼 때 주의 (run 문서 "디버거 메모"):
  - `+0xf7` 시점에 새 줄은 RAX 가 아니라 **RBX** 다 (RAX 는 `LdrpInsertDataTableEntry` 반환값). `+0x6c` 에선 RAX.
  - `+0x154` 전에는 `LdrpNtDllDataTableEntry` 가 0 이라, 그 칸으로 ntdll 줄을 찾을 수 없다.

---

## 5. 더 읽기

- run 문서: [`run35-ntdll-entry.md`](run35-ntdll-entry.md) — 호출 지점 표, 인자 표, "하는 일 순서" 트리, 질문 2~6 절, "정정"
- 결과서: [`_workspace/sast/30_ntdll-entry_result.md`](../../_workspace/sast/30_ntdll-entry_result.md) — 질문별 답, 대표 지점, 동적 확인거리
- 요청서: [`_workspace/sast/30_ntdll-entry.md`](../../_workspace/sast/30_ntdll-entry.md)
- flow.md: [`flow.md`](flow.md)
  - 그림 2 `(0) 프로세스 준비` 안 `p4`·`p4a ~ p4d`·`p5`·`p6`
  - 2.4 `#### ntdll!LdrpInitializeNtdllDataTableEntry`, `#### ntdll!LdrpInsertDataTableEntry`, `#### ntdll!LdrpInsertModuleToIndex` 절
  - 2.6 dll1·dll2 명부 넣기 줄, 2.7 run7 GR 프레임 줄
- 앞 해설:
  - [`run18-proc-init-setup-해설.md`](run18-proc-init-setup-해설.md) — 2.6 건물 명부가 생기는 순간, 2.7 명부 첫 줄과 두 번 부르는 이유, 묶음 3 지번 색인
  - [`run19-proc-init-modules-해설.md`](run19-proc-init-modules-해설.md) — 2.1 쪽지(`UNICODE_STRING`), 2.6 exe 명부 줄·연결 카드
  - [`run20-kernel32-load-해설.md`](run20-kernel32-load-해설.md) — 2.3 이름 색인 카드함 (해시 표)
  - [`run21-load-dll-internal-해설.md`](run21-load-dll-internal-해설.md) — 2.6 진행 도장 번호표, 2.10 적재 이유
  - [`run26-map-dll-file-해설.md`](run26-map-dll-file-해설.md) — 2.4 dll1·dll2 가 명부에 들어가는 순간
  - [`run27-init-graph-해설.md`](run27-init-graph-해설.md) — 2.5 도장 7 인 카드만 재귀

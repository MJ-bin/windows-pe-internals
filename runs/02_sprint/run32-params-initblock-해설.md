# run32 params-initblock — 해설

> 이 글은 [run32 문서](run32-params-initblock.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q36** — 2.1 `nt!PspCopyAndFixupParameters` (PEB `+0x20` 프로세스 매개변수를 누가 쓰나), `nt!PspPrepareSystemDllInitBlock` (ntdll 로 넘기는 시작 정보) (`_workspace/plan.md`, 이번에 **끝**)
  - **단계 3 "작은 정적부터 몰아서"** 의 바퀴다 (사용자 결정, 2026-10-23). run31(Q33) 다음이다.
  - 새 항목: **Q63** (정적, 작은 것 묶음 (a) ~ (f)). 출처를 더한 항목: **Q35** (동적, 이번 동적 확인거리 1 ~ 4), **Q47** (동적, `PsDefaultLoaderThreads` 값).
  - 다음 항목: **Q40** (plan.md).
- 근거:
  - run 문서 [`run32-params-initblock.md`](run32-params-initblock.md) (가장 중요)
  - 요청서 [`_workspace/sast/27_params-initblock.md`](../../_workspace/sast/27_params-initblock.md)
  - 결과서 [`_workspace/sast/27_params-initblock_result.md`](../../_workspace/sast/27_params-initblock_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 연 바이너리는 ntoskrnl (`ntkrnlmp.pdb`, flow.md 기준과 같은 GUID).
  - 구조체 칸 이름은 PDB 타입 덤프로 맞췄다. 단 **`LdrSystemDllInitBlock` 의 타입은 공개 PDB 에 없다.** 그래서 그 블록의 칸 이름은 전부 `[추측]` 후보다 (2.9).

이 글은 run31 처럼 **앞으로 되감기**다.

- run10 에서 안내판(PEB)을 달 때, 관리사무소가 **"영업 안내" 칸(`+0x20`) 하나를 비워 두고** 갔다.
- 그런데 관리실(ntdll)은 출근하자마자 그 칸을 읽는다 (run17·run18). 누가 사이에 채우나? 그게 이번 바퀴다.
- 가 보니, 그 칸만이 아니었다. 관리사무소는 **관리실 책상 위 수첩**에도 미리 칸을 채워 두고 간다. ntdll 쪽에서 "어디서 왔지?" 하던 값 몇 개(run18 `+0xa0`, run20 `0x20000000`, run28 `+0xb8`)의 출처가 **한꺼번에** 여기서 나왔다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 비트·플래그, 핸들(**손잡이**) → [run9 해설](run9-create-user-process-해설.md) 2절
- PEB(**안내판**), `_EPROCESS`(관리사무소 쪽 **장부**), 구조체 칸 오프셋 → [run10 해설 "PEB"](run10-allocate-process-해설.md#peb)
- 남의 땅에 잠깐 들어가기(`KiStackAttachProcess` / `KiUnstackDetachProcess`) → [run10 해설](run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)
- `MmCreatePeb` 가 안 쓰는 칸 `+0x20` → [run10 해설 3.7](run10-allocate-process-해설.md#37-안내판에-쓰는-것-안-쓰는-것)
- `PsNtdllExports`(관리사무소 **벽 주소록**) → [run16 해설 2.9](run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다)
- IFEO(**가게 이름별 특별 지시**) → [run10 해설 3.4](run10-allocate-process-해설.md#34-144줄-중-어디쯤인가)
- WoW64 (64비트 Windows 에서 32비트 프로그램을 돌리는 장치) → [run17 해설 2.9](run17-ldr-init-entry-해설.md#29-나중-스레드-길--짧게)
- CFG(**교환원**) → [run28 해설 2.9](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)

비유도 그대로 이어 쓴다.

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**
- 프로세스 = **가게**, 주소 공간 = **빈 땅**, 스레드 = **직원**, PEB = 가게 **안내판**, `_EPROCESS` = 관리사무소 쪽 **장부**
- 새 프로세스에 붙기 = 관리사무소 직원이 **새 가게 땅 안으로 들어가기** (run10)
- `PsNtdllExports` = 관리사무소 **벽 주소록** (run16)
- **이번에 더하는 비유** (flow.md 2.1 과 같다):
  - 프로세스 매개변수 = 부모가 써 준 **개업 서류** (명령줄·exe 경로·환경 변수·표준 입출력 …)
  - `LdrSystemDllInitBlock` = 관리실 책상 위 **업무 수첩**
  - CFG 비트맵 = 교환원이 보는 **출입 명단** ("이 번호는 걸어도 됨")
  - 주의: run15 해설의 "시작 정보 묶음 = 관리사무소 **서류철**" 은 커널 안 스레드 시작 정보라 **이번 업무 수첩과 다른 물건**이다. 이름이 비슷해서 적어 둔다.

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 다섯**:

1. **PEB `+0x20` 은 `PspCopyAndFixupParameters` 가 쓴다** `[정적]`. 개업 서류의 커널 사본을 새 가게 안 빈 방에 옮기고, 안쪽 주소 12개를 새 방 기준으로 고친 뒤(Fixup), 안내판에 "서류는 이 방에" 를 적는다 → 2.4 ~ 2.6
2. 같은 함수가 **콘솔·표준 입출력 손잡이(핸들)** 도 새 가게용으로 복사해 준다 `[정적]` → 2.7
3. **`PspPrepareSystemDllInitBlock` 은 새 가게 안 ntdll 의 `LdrSystemDllInitBlock` 에 직접 쓴다** `[정적]`. run18 의 `+0xa0`, run28 의 `+0xb8` 이 **여기서 채워진다** → 2.8 ~ 2.10
4. 덤: run20 의 kernel32 건너뜀 비트 **`0x20000000` 은 커널이 켠다** (`PspSetupUserProcessAddressSpace +0xe6`). 사용자는 못 켠다 `[정적]` → 2.11
5. 주의: 커널이 `LoaderThreads` 가 0 이면 **`PsDefaultLoaderThreads` 로 먼저 채운다** `[정적]`. 그래서 run24 의 `[추측]` "IFEO 없으면 0 → 4" 와 **어긋날 수 있다.** 값은 동적 Q47 → 2.12

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess (9번째 인자 = 개업 서류)   (run14·run9)

 커널, 부모 스레드 안: NtCreateUserProcess          (아래는 오프셋 순서로 놓았다)
  ├─ +0x93c  MmCreateSpecialImageSection     조립식 세트 준비                         (run31)
  ├─ +0xacb  PspCaptureProcessParameters     개업 서류를 관리사무소로 받아 둠         ← 이번 (조금)
  ├─ +0xc36  PspAllocateProcess              가게 짓기                                 (run10)
  │     ├─ +0x2095 MmInitializeProcessAddressSpace   빈 땅, 건물 두 채                (run10)
  │     ├─ +0x2314 MmCreatePeb                       안내판 달기 (+0x20 은 비워 둠)    (run10)
  │     └─ +0x239c PspSetupUserProcessAddressSpace   가게 안 마무리
  │           ├─ +0x1b9 PspCopyAndFixupParameters    개업 서류 옮기기, 안내판 +0x20   ← 이번
  │           └─ +0x1e6 PspPrepareSystemDllInitBlock 관리실 업무 수첩 채우기         ← 이번
  └─ +0xd22  PspCreateUserContext            업무 시작 메모                            (run16)

 test.exe 메인 스레드 (나중): 관리실이 출근해서 안내판·수첩을 읽는다
  LdrpInitialize +0x5a                    서류 Flags 맨 윗 비트                         (run17)
  LdrpInitializeProcess 묶음 4            수첩 +0xa0                                    (run18)
  LdrpInitializeProcess +0x6c5, +0xc3f    서류 다듬기                                   (run18)
  LdrpInitializeKernel32Functions +0x1ff  서류 Flags 0x20000000                         (run20)
  LdrpEnableParallelLoading (+0x1a47)     서류 LoaderThreads                            (run19·run24)
  LdrpDispatchUserCallTarget              수첩 +0xb8 (CFG 출입 명단)                    (run28)
```

- 시간으로는 **부모 cmd.exe 스레드 안, 가게를 다 짓고 안내판까지 단 직후**다.
- flow.md 로는 **2.1** 의 `nt!PspSetupUserProcessAddressSpace` 절과 그 아래 새 절 셋, 그림 1 의 `p8` 상자 아래다.
- 아래쪽 "test.exe 메인 스레드" 줄들은 **이미 문서에 있던 읽는 쪽**이다. 이번 바퀴는 ntdll 을 새로 안 읽었다. 대신 **쓰는 쪽을 찾아서 짝을 지었다** (2.10).

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. **PEB `+0x20` (`ProcessParameters`) 를 누가 쓰나?** `MmCreatePeb` 는 안 쓴다 (run10).
   - flow.md 2.1 엔 `[추측]` "`PspCopyAndFixupParameters` 가 매개변수를 새 프로세스로 복사하고 PEB `+0x20` 에 잇는 곳" 이 있었다.
2. **`PspPrepareSystemDllInitBlock` 은 ntdll 로 넘기는 시작 정보를 만드나?** 역시 `[추측]` (이름으로 본 것).
3. 그리고 커널이 쓴 칸과 ntdll 이 읽는 줄(run17·run18·run19·run20·run28 에 이미 적힌 것)을 **짝짓기**.

### 바퀴 전에 알던 것

- `[정적]` (run10) `MmCreatePeb` 는 PEB `+0x18`, `+0x20`, `+0x30` 을 **쓰지 않는다.**
- `[정적]` (run10) `PspSetupUserProcessAddressSpace` 안 순서: 새 땅에 들어감(`+0xfa`) → `ZwAllocateVirtualMemory` `+0x19c` → `PspCopyAndFixupParameters` `+0x1b9` → `PspPrepareSystemDllInitBlock` `+0x1e6` → `MmMapApiSetView` `+0x255` → `PspMapSiloSharedDataView` `+0x263`. 본문은 안 봤다.
- `[추측]` (run14) 부모 `BasepCreateProcessParameters` 가 `NtCreateUserProcess` 9번째 인자(매개변수)를 만든다.
- `[정적]` (run18) ntdll `LdrpInitializeProcess` 가 `LdrSystemDllInitBlock+0xa0` ("무결성 설정 비트") 을 읽는다. (run28) `LdrpDispatchUserCallTarget` 이 `LdrSystemDllInitBlock+0xb8` (CFG 비트맵) 을 본다.
- `[정적]` (run17·run18 등) ntdll 이 `PEB.ProcessParameters` 의 `Flags`, `LoaderThreads`(+0x40c) 등을 읽는다.

---

## 2. 찾은 것

### 2.1 한눈에 — 가게 안 마무리 한 바퀴

run 문서 "호출 관계" 를 줄였다. 오프셋은 ret. 모두 `[정적]`.

```
PspAllocateProcess
 └─ +0x239c  PspSetupUserProcessAddressSpace
       │  +0xa0 ~ +0xea  (붙기 전) 커널 사본의 Flags 비트 15·29, LoaderThreads 기본값 손보기
       ├─ +0xfa   KiStackAttachProcess              새 가게 땅으로 들어감
       ├─ +0x19c  ZwAllocateVirtualMemory           빈 방 하나 잡기 (Dst)
       ├─ (1) +0x1b9  PspCopyAndFixupParameters
       │     ├─ +0xbc / +0x130  ObDuplicateObject     콘솔 손잡이 / 표준 손잡이 3개
       │     └─ +0x1a2  기능 스위치 → 둘 중 하나:
       │           꺼짐: +0x424 복사 → 주소 12개 고침 → +0x52b  PEB+0x20 저장
       │           켜짐: 주소 고침 → +0x329 RtlCopyToUser → +0x34c RtlWriteULong64ToUser(PEB+0x20)
       ├─ (2) +0x1e6  PspPrepareSystemDllInitBlock(0, 만들기 정보)    보안 프로세스면 건너뜀
       │     │  수첩 = [PsNtdllExports+0x0] = 새 가게 안 ntdll LdrSystemDllInitBlock
       │     ├─ +0x100  ExGenRandom             → 수첩 +0x98
       │     │  +0x12b/+0x138 → 수첩 +0xa0 (완화 옵션),  +0x15a → 수첩 +0xb8 (CFG 비트맵)
       │     └─ +0x19b  PspGetScpCfgFunctions   → 수첩 +0xf0 ~
       ├─ +0x255  MmMapApiSetView
       ├─ +0x263  PspMapSiloSharedDataView
       └─ +0x26f  KiUnstackDetachProcess            나옴
```

- 한 줄 요약: **(붙기 전) 사본 손보기 → 들어가기 → 방 잡기 → 서류 옮기고 안내판에 적기 → 수첩 채우기 → 나오기.**
- `(1)`·`(2)` 는 그림 1 에 새로 넣은 **`(정적)` 점선 상자 두 개**의 번호다 (3절).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 서류를 옮기는 호출

트리의 `(1) +0x1b9  PspCopyAndFixupParameters` 줄.

- **누가**: `PspSetupUserProcessAddressSpace`
- **어디서**: `+0x1b4` 에서 부르고, 돌아올 자리는 `+0x1b9`
- **누구를**: `PspCopyAndFixupParameters` (rva `0xae57e4`, 1340바이트)
- **무엇을 위해**: 바로 앞 `+0x19c` 에서 잡은 빈 방(`Dst`)에 **개업 서류 사본을 옮기고, 안내판 `+0x20` 에 그 방 주소를 적으려고.** `[정적]`
  - 이 줄이 돌아오면 PEB `+0x20` 은 이미 채워져 있다. 부르는 쪽(`PspSetupUserProcessAddressSpace`) 자신은 PEB 를 건드리는 줄이 **하나도 없다** (`.asm` 에 `EPROCESS.Peb` `+0x2e0` 읽기가 없음) `[정적]`.

#### 한 줄 읽기 ② — 수첩에 CFG 명단 주소를 적는 줄

트리의 `+0x15a → 수첩 +0xb8` 줄. CALL 이 아니라 **값을 쓰는 줄**이다.

- **누가**: `PspPrepareSystemDllInitBlock`
- **어디서**: `+0x15a`
- **무엇을**: 새 프로세스 장부 `EPROCESS+0x410` (PDB `Vm.Instance.VmWorkingSetList`) 이 가리키는 구조의 `+0x4a0` 값을, 수첩(`LdrSystemDllInitBlock`) `+0xb8` 칸에 쓴다 `[정적]`. `+0x4a0` 칸의 이름은 PDB 에 없다.
- **무엇을 위해**: 관리실이 나중에 `LdrpDispatchUserCallTarget` 에서 이 칸을 **CFG 비트맵 주소**로 본다 (run28). 곧 run28 이 읽는 칸의 **출처**가 이 줄이다 `[정적]`.

### 2.3 새 말 — 프로세스 매개변수 블록

- **프로세스 매개변수 블록** = 부모가 "이 프로그램을 이렇게 시작해 줘" 하고 건네는 정보 묶음. PDB 타입 이름은 `_RTL_USER_PROCESS_PARAMETERS` `[정적]`.
  - 안에 든 것 (이번에 나온 칸 이름, PDB): 명령줄 `CommandLine`, exe 경로 `ImagePathName`, 환경 변수 `Environment`, 현재 폴더 `CurrentDirectory`, DLL 경로 `DllPath`, 창 제목 `WindowTitle`, 표준 입출력 손잡이 `StandardInput/Output/Error`, `Flags`, `LoaderThreads` …
  - PEB `+0x20` (`ProcessParameters`) 칸이 이 블록을 **가리킨다** (주소를 담는다) `[정적]`.
- 비유: 부모가 써 준 **개업 서류**. "가게 이름(exe 경로), 첫날 할 말(명령줄), 쓸 비품 목록(환경 변수), 손님 받는 창구 열쇠(표준 입출력)…" 가 적혀 있다.
  - 안내판(PEB) `+0x20` 칸 = **"개업 서류는 이 방에"** 라는 한 줄.

### 2.4 핵심 ① 서류가 지나는 길 — 세 정거장

개업 서류는 **세 군데**를 거친다.

```
부모 cmd.exe 메모리  ──(1)──▶  관리사무소 쪽 사본  ──(2)──▶  새 가게 안 빈 방  ──(3)──▶  안내판 +0x20 에 "이 방"
   (9번째 인자)          PspCaptureProcessParameters     ZwAllocateVirtualMemory     PspCopyAndFixupParameters
                         NtCreateUserProcess +0xacb       … +0x19c                    … +0x1b9
```

#### 정거장 (1) — 관리사무소가 사본을 받아 둔다 `[정적]`

- `NtCreateUserProcess +0xac6` (ret `+0xacb`) `PspCaptureProcessParameters` 가 **부모가 준 9번째 인자**를 받는다.
- 사용자 모드에서 부른 거면 `+0x3a` (ret `+0x3f`) 에서 `PspCaptureUserProcessParameters` 를 불러 **커널 풀에 새 사본**을 만든다.
  - **커널 풀** = 커널이 자기 일에 쓰는 메모리 더미 `[추측]` (일반적으로 알려진 설명). 비유: 관리사무소 **서랍**. (run31 의 "서랍" = 컨트롤 영역과는 다른 서랍이다. 여기선 그냥 "사무소 안 보관함".)
- 커널 모드에서 부른 거면 받은 포인터를 그대로 쓴다 (`+0x58 ~ +0x5a`).
- 사본 주소는 **만들기 정보 구조** `+0xd8` 에 둔다 (`PspCopyAndFixupParameters +0x2a` 가 거기서 꺼냄). 이게 `Src` 다.
  - **만들기 정보 구조** = `NtCreateUserProcess` 가 자기 지역 변수로 만들어 `PspAllocateProcess` 에 11번째 인자로 넘기는 구조 `[정적]` (결과서·run 문서). 이번 바퀴에 계속 나오니 이름만 기억해 두자. 비유: 이번 개업 건의 **작업 지시서 한 장**.
- 왜 굳이 사본을 만드나? 부모 메모리는 부모가 언제든 바꿀 수 있으니, 관리사무소가 **자기 손에 한 부** 쥐고 일하는 것으로 보인다 `[추측]` (일반적으로 알려진 설명. 이번 코드가 이유를 말해 주진 않는다).
- 부모 쪽에서 9번째 인자를 만드는 게 `BasepCreateProcessParameters` 라는 건 **여전히 run14 `[추측]`** 이다. 이번 코드로 확인한 건 "커널이 유저가 준 포인터를 가져온다" 까지다.

#### 정거장 (2) — 새 가게 안에 빈 방을 잡는다 `[정적]`

- `PspSetupUserProcessAddressSpace +0x197` (ret `+0x19c`) `ZwAllocateVirtualMemory(현재 프로세스, &[구조+0xe0], …, 0x3000, 4)`.
  - "현재 프로세스" = 지금 **붙어 있는** 새 프로세스다 (`+0xfa` 에서 들어갔으니).
  - 주소 칸을 `+0x17f` 에서 0 으로 비워 두고 부른다 → **자리는 시스템이 고른다.**
  - `0x3000`·`4` 는 `MEM_COMMIT|MEM_RESERVE`·`PAGE_READWRITE` 와 같은 숫자다 (읽고 쓸 수 있는 방).
- 방 주소는 만들기 정보 구조 `+0xe0` 에 들어간다. 이게 `Dst` 다 (`PspCopyAndFixupParameters +0x40`).
- 방 크기 = `MaximumLength` + `EnvironmentSize` (+ 스위치 켜짐이면 사용자 속성 목록).
  - `Flags & 0x1000060` 이 0 이고 `0x20000` 보다 작으면 **`0x20000` 으로 올린다** (`+0x15c ~ +0x16b`).

#### 정거장 (3) — 옮기고, 안내판에 적는다 `[정적]`

- `PspCopyAndFixupParameters` 가 `Src` → `Dst` 로 옮기고, 안쪽 주소를 고친다 (2.5). 그다음 **PEB `+0x20` 에 `Dst` 를 쓴다** (2.6).
- 새 PEB 는 어떻게 찾나:
  - 현재 스레드(`GS:[0x188]`) → `+0xb8` (지금 붙은 프로세스) → `+0x2e0` (`_EPROCESS.Peb`) → `+0x20` (`_PEB.ProcessParameters`).
  - 부모 cmd.exe 스레드인데도 "현재 프로세스 = 새 프로세스" 가 되는 건 **새 가게 땅에 들어가 있는 동안**이라서다 ([남의 땅에 잠깐 들어가기](run10-allocate-process-해설.md#남의-땅에-잠깐-들어가기)).
- 비유로: 관리사무소 직원이 서랍의 사본을 들고 **새 가게 안으로 들어가**, 빈 방에 서류를 넣고, 문 앞 안내판의 비어 있던 칸에 "서류는 이 방에" 를 적는다.

### 2.5 핵심 ② Fixup — 서류 안 "몇 쪽 참고" 쪽지를 새 방 기준으로 고친다

#### 새 말 — 정규화, Fixup

- 매개변수 블록 안의 문자열 칸(명령줄 등)은 글자를 직접 담지 않고, **글자가 있는 곳의 주소**(`Buffer`)를 담는다.
- 그 주소를 적는 방식이 두 가지로 알려져 있다 `[추측]` (일반적으로 알려진 설명):
  - **정규화 안 된 꼴**: "블록 시작에서 몇 바이트 뒤" (오프셋)만 적는다. 블록을 어디로 옮겨도 안 바뀐다.
  - **정규화된 꼴**: 진짜 주소를 적는다. 블록을 옮기면 **고쳐야** 한다.
  - `Flags` 비트 0 이 "정규화됨" 표시다 (run 문서가 "정규화 비트(`Flags` 비트 0)" 라고 부름).
- **Fixup** = 블록을 옮긴 뒤 안쪽 주소들을 **새 자리 기준으로 고쳐 쓰는 일**. 함수 이름의 "Fixup" 이 이것이다 `[정적]` (아래).
- 비유: 서류 안에 "자세한 건 **3층 5호 서랍**에" 같은 쪽지가 12장 붙어 있다. 서류를 다른 방으로 옮기면 쪽지 속 위치도 **새 방 기준으로** 고쳐 써야 한다.

#### 고치는 공식 `[정적]`

- 칸 = `Dst + (칸 − Src)`. 칸이 0 이면 건너뛴다.
- 풀어 읽으면: "옛 블록 시작에서 몇 바이트 떨어져 있었나(`칸 − Src`)" 를 구해서, **새 블록 시작(`Dst`)에 더한다.**
- 설명용 가짜 숫자로 해 보면 (지어낸 값, 근거 아님): 사본이 1000번지, 새 방이 5000번지, 명령줄 글자가 1100번지에 있었다면 → 5000 + (1100 − 1000) = **5100번지**로 고친다.
- 결과는 **진짜 주소(절대 주소)** 다. 오프셋으로 되돌리지 않는다 `[정적]`.

#### 고치는 칸 12개 `[정적]` (PDB `_RTL_USER_PROCESS_PARAMETERS`)

| 오프셋 | 칸 | 오프셋 | 칸 |
|---|---|---|---|
| `+0x40` | `CurrentDirectory.DosPath.Buffer` | `+0xc8` | `DesktopInfo.Buffer` |
| `+0x58` | `DllPath.Buffer` | `+0xd8` | `ShellInfo.Buffer` |
| `+0x68` | `ImagePathName.Buffer` | `+0xe8` | `RuntimeData.Buffer` |
| `+0x78` | `CommandLine.Buffer` | `+0x418` | `RedirectionDllName.Buffer` |
| `+0x80` | `Environment` | `+0x428` | `HeapPartitionName.Buffer` |
| `+0xb8` | `WindowTitle.Buffer` | `+0x430` | `DefaultThreadpoolCpuSetMasks` |

- 안 고치는 포인터 칸도 있다: `PackageDependencyData` (+0x400), `CurrentDirectores[32]` 안쪽. **왜인지는 모른다** (Q63 (f)).

#### 정규화 비트는 여기서 안 건드린다 — 대신 "들어올 때" 검사한다

이 부분이 처음엔 헷갈린다. 단계별로 가 보자.

1. `[정적]` `PspCopyAndFixupParameters` 가 `Flags` 를 바꾸는 줄은 **`+0x82 BTS [RBX+8],0xe` 하나**뿐이다 (비트 14 켜기, 만들기 정보 `+0x10` 비트 6 켜짐 · `+0x11` 비트 0 꺼짐일 때). 비트 0 은 읽지도 쓰지도 않는다.
2. `[정적]` 대신 한 단계 위, 정거장 (1) 의 `PspCaptureUserProcessParameters +0x8f ~ +0xa8` 이 **조건으로 검사**한다:
   - 비트 14 를 지운 뒤 `(Flags ^ 1) & 0xf4010e11 ≠ 0` 이면 `0xC000000D` 로 **실패**.
   - 곧 들어오는 블록은 **비트 0 이 켜진(정규화된) 꼴**이어야 하고, 마스크의 다른 비트는 꺼져 있어야 한다.
3. 그래서 → 관리사무소 사본은 **처음부터 "진짜 주소가 든 꼴"** 이다. 고치는 공식이 진짜 주소 기준인 것과 맞아떨어진다.
4. 아직 모르는 것:
   - 마스크 `0xf4010e11` 각 비트의 이름 (Q63 (a)).
   - ntdll `LdrpInitializeProcess +0x6c5 RtlNormalizeProcessParams` (run18) 가 **이미 정규화된** 블록을 받아서 무엇을 하는지. ntdll 본문을 안 읽어 모른다 `[추측: 할 일 없음]` (Q63 (d)).

- 비유: 관리사무소는 "**쪽지에 진짜 위치가 적힌 서류만** 받습니다" 하고 창구에서 거른다. 그러니 옮길 때도 진짜 위치 기준으로만 고치면 된다.

### 2.6 핵심 ③ 안내판에 적는 줄은 두 갈래 — 기능 스위치

#### 새 말 — 기능 스위치

- **기능 스위치** = 같은 Windows 빌드 안에서 새 기능을 켜고 끄는 전역 값 `[추측]` (일반적으로 알려진 설명). 이 바이너리에선 `Feature_…` 로 시작하는 이름으로 보인다.
- 이번 스위치: 전역 `Feature_CreateProcessSegmentHeapEnablement__private_featureState` (rva `0xfc4110`) 의 비트 4·0 `[정적]`. `+0x19d` (ret `+0x1a2`) 에서 물어본다.
- 비유: 관리사무소 벽의 **"새 업무 방식 시범 운영" 스위치**. 켜져 있으면 일하는 순서가 조금 달라진다.

#### 두 갈래 `[정적]`

| 갈래 | 조건 | PEB `+0x20` 쓰는 줄 | 복사 방식 |
|---|---|---|---|
| 스위치 꺼짐 | `+0x1a2` 반환 0 → `+0x1a4 JZ` | **`+0x514 ~ +0x52b`** (`MOV [RAX+0x20],RDI`, 그냥 저장) | `+0x41f` (ret `+0x424`) 메모리 복사로 먼저 옮기고 → **복사본 안에서** 주소 고침 |
| 스위치 켜짐 | 반환 ≠ 0 | **`+0x347` (ret `+0x34c`) `RtlWriteULong64ToUser(PEB+0x20, Dst)`** | 원본 앞 0x450 바이트 백업 → **원본 쪽에서** 주소 고침 → `+0x324` (ret `+0x329`) `RtlCopyToUser` → PEB 에 씀 → 백업 되돌림 |

- 두 갈래의 차이는 "**옮기고 고치나 / 고치고 옮기나**" 다. 켜짐 쪽은 원본을 고치니까 미리 백업해 두고 끝에 되돌린다.
- 두 갈래 **공통** `[정적]`:
  - PEB 에 쓰는 때는 **복사가 끝난 뒤**다.
  - 쓰기 결과는 검사하지 않는다 (`+0x34c` 뒤에 `TEST` 없음).
- 비유:
  - 꺼짐 = 서류를 새 방에 먼저 놓고, **새 방에서** 쪽지를 고친다.
  - 켜짐 = 사무소에서 **사본 위에** 쪽지를 고쳐 쓰고(원래 쪽지는 사진으로 찍어 둠), 고친 서류를 새 방에 넣고, 사진대로 사본을 원래대로 되돌려 둔다.
- 어느 갈래인지는 **스위치 값이라 실행 때 정해진다** → 동적 Q35 (`dd nt+0xfc4110 L1`).
- 이름의 "SegmentHeap" 이 이 일과 왜 묶였는지는 **모른다** `[공백]` (Q63 (f)).

### 2.7 같은 함수가 손잡이도 복사해 준다 — 표준 핸들

#### 새 말 — 표준 핸들

- **표준 핸들** = 프로그램이 기본으로 쓰는 입력·출력·오류 통로 셋(표준 입력·표준 출력·표준 오류)의 손잡이 `[추측]` (일반적으로 알려진 설명). 콘솔 프로그램이면 보통 그 콘솔 창으로 이어진다.
- 매개변수 블록 안 칸: `StandardInput/Output/Error` (`+0x20`/`+0x28`/`+0x30`), 그리고 `ConsoleHandle` (`+0x10`) `[정적]` (PDB).
- 손잡이는 **가게마다 따로** 쥐어야 한다. 부모 손잡이 번호를 그대로 적어 두면 새 가게에선 안 맞는다. 그래서 **복제**(`ObDuplicateObject`, 부모 → 새 프로세스)해서 새 번호를 적는다 `[정적: 복제 줄]`, `[추측: 이유]`.
- 비유: 부모 가게의 **창구 열쇠**를 새 가게용으로 하나 더 깎아서, 서류에 새 열쇠 번호를 적어 준다.

#### 두 자리 `[정적]`

| 자리 (ret) | 무엇 | 조건 |
|---|---|---|
| `+0xbc` | `ConsoleHandle` (`Src+0x10`) 을 복제해 같은 칸에 되씀 | 이미지 `SubSystemType` 이 3 (콘솔 `[추측: 값 이름]`) 이고 핸들 값 `> 0` |
| `+0x130` | 표준 입력·출력·오류 (`Src+0x20`/`+0x28`/`+0x30`), 고리 **세 번** | 복제할 핸들 목록이 있을 때 |

- 그 "목록" 은 어디서 오나 (`PspSetupUserProcessAddressSpace +0x41 ~ +0x73`) `[정적]` — 만들기 정보 `+0x11` 비트 2 ~ 3 값에 따라:
  - **1**: 부모의 표준 핸들 목록 (`PspGetStandardHandleList`, `+0x8d` / ret `+0x92`, **붙기 전**에 부름)
  - **2**: 매개변수 안 칸 그대로
  - **0·3**: 없음
  - 2 일 때만 복제 실패가 **전체 실패**다.
- `[추측]` 그 값은 공개 문서의 `PS_STD_HANDLE_INFO.StdHandleState` (0 안 함 / 1 요청 시 / 2 늘) 와 모양이 같다. `+0x11` 을 채우는 줄은 안 봤다 → Q63 (c).

### 2.8 새 말 — 시작 정보 블록, 그리고 "어디에 쓰나"

#### 새 말 — 시작 정보 블록 (`LdrSystemDllInitBlock`)

- **시작 정보 블록** = 커널이 새 프로세스의 ntdll 에게 "이렇게 시작해" 하고 미리 적어 두는 정보 묶음. 이 빌드에선 **ntdll 안 전역 변수 `LdrSystemDllInitBlock`** (0x128 바이트) 이다 `[정적]`.
- 비유: 관리실(ntdll) **책상 위 업무 수첩**. 관리사무소가 가게 문 열기 전에 **직접 들어와서** 칸을 채워 두고 간다. 관리실은 출근해서 펴 보기만 한다.

#### 커널 쪽 표가 아니라 **새 가게 안 ntdll 의 수첩**이다 `[정적]`

이게 이번 질문 2 의 답이다.

- 첫 인자 `ECX` 로 "어느 ntdll 이냐" 를 고른다:
  - `ECX == 0` (64비트 ntdll): `+0x214 MOV RBX,[PsNtdllExports]` — 벽 주소록 `PsNtdllExports` 의 `+0x0` 칸 (PDB `_PS_NTDLL_EXPORTS.LdrSystemDllInitBlock`).
    - run16 에서 본 **그 벽 주소록**이다. 부팅 때 ntdll 이름표에서 주소를 찾아 적어 둔 표. 이번엔 그 첫 칸이 "관리실 수첩은 몇 호" 였다.
  - `ECX == 1` (32비트 ntdll): `+0x2dd` (ret `+0x2e2`) `PspWow64GetSharedInformation(1)` 이 돌려준 표의 `+0x38` 칸 (PDB 열거형 `SharedNtdll32LdrSystemDllInitBlock = 7`).
- "새 프로세스" 는 현재 스레드가 **붙어 있는** 프로세스로 고른다 (`+0x35 ~ +0x3e`). 그래서 부르는 쪽이 붙은 뒤에 부른다.
- 그런데 부팅 때 적어 둔 주소록 주소로 쓰면 어떻게 **새 가게의** 수첩에 써지나?
  - run 문서의 답 `[정적]`: 이 함수는 **새 가게 땅에 들어가 있는 동안** 불리고, 그 상태에서 그 주소에 쓴다. 그래서 "새 프로세스 안 ntdll 의 `LdrSystemDllInitBlock`" 이다.
  - 왜 부팅 때 적은 주소가 새 가게에서도 맞나는, ntdll 세트는 부팅 때 **한 벌** 만들어 두고 가게마다 같은 걸 붙인다는 run10 의 짐작과 이어진다 `[추측]` ([run10 해설 3.10](run10-allocate-process-해설.md#310-ntdll-쪽-조금-더)). 실제 주소는 동적 Q35 에서 본다.
- 크기 확인 `[정적]`: 수첩 맨 앞 4바이트가 `0x128` 이 아니면 (`+0x98 ~ +0x9f`) `0xC0000059` 로 끝 (`+0x1ee`).
  - 비유: 수첩 표지에 "0x128 쪽짜리" 가 아니면 "판이 다른 수첩이네" 하고 손 안 댄다.

### 2.9 수첩에 무엇을 적나 — 대표 칸

블록 오프셋·쓰는 줄·값 출처는 `[정적]`. **칸 이름은 PDB 에 이 타입이 없어서, 결과서가 공개 비공식 헤더에서 빌린 후보 = `[추측]`** 이다.

| 수첩 칸 | 쓰는 줄 | 값 출처 `[정적]` | 이름 후보 `[추측]` | 비유 |
|---|---|---|---|---|
| `+0x98` | `+0x100` | `ExGenRandom(1)` (`+0xfb`, ret `+0x100`) | RngData | 오늘의 **난수** 하나 |
| **`+0xa0 ~ +0xb7`** (24B) | **`+0x12b`, `+0x138`** | 만들기 정보 `+0x150 ~ +0x167` (← `PspAllocateProcess` 완화 옵션, 아래) | MitigationOptionsMap | **안전 규칙** |
| **`+0xb8`** | **`+0x15a`** | `EPROCESS+0x410` 이 가리키는 구조의 `+0x4a0` | CfgBitMap | CFG **출입 명단** 위치 |
| `+0xc0` | `+0x169` | 같은 구조 `+0x4a8` | CfgBitMapSize | 명단 크기 |
| `+0xd8 ~ +0xef` (24B) | `+0x145`, `+0x152` | 만들기 정보 `+0x1c0 ~ +0x1d7` (`PspAllocateProcess +0x1f61 ~ +0x1f72`) | MitigationAuditOptionsMap | |
| `+0xf0 ~ +0x127` (7칸) | `+0x1a3 ~ +0x1e5` | `+0x196` (ret `+0x19b`) `PspGetScpCfgFunctions` 가 돌려준 전역 표 `PspNtdllScpFunctions` 7칸 (NULL 이면 안 채움) | CFG 검사·분배 함수 주소 류 | |

- 그 밖에 `+0x8`·`+0x10` (시스템 DLL 기록의 `+0x38` 값), `+0x18 ~ +0x97` (WoW64 공유 정보 16칸), `+0x9c` (플래그 비트 0), `+0xc8`·`+0xd0` (WoW64 만) 도 채운다. 전체 표는 run 문서.

#### 새 말 — 완화 옵션

- **완화 옵션(mitigation)** = 공격을 어렵게 하려고 프로세스마다 켜는 보안 설정 묶음 `[추측]` (일반적으로 알려진 설명).
- 비유: run10 해설 3.4 표 10번 "완화 정책 = **안전 규칙**" 과 같은 말이다. 가게마다 "이 가게는 이런 안전 규칙을 지킨다" 를 정해 둔 것.

#### `+0xa0` 의 출처 — 단계별로 `[정적]`

1. `PspAllocateProcess +0x1eb3` (ret `+0x1eb8`) `PspApplyMitigationOptions` 에 지역 24바이트를 넘긴다.
2. `PspAllocateProcess +0x1f3f ~ +0x1f50` 이 그 24바이트를 **만들기 정보 `+0x150`** 에 복사한다.
3. `PspPrepareSystemDllInitBlock +0x12b`·`+0x138` 이 그걸 **수첩 `+0xa0`** 에 옮긴다.
4. 관리실 `LdrpInitializeProcess` 묶음 4 (`+0x4a8 ~ +0x579`) 가 그 칸을 읽는다 (run18, "무결성 설정 비트 두 개").

- 그래서 run18 이 "무결성 설정 비트" 라고 부른 칸은 **관리사무소가 정한 안전 규칙 24바이트**에서 온다.
- 단, **그 두 비트의 정확한 읽는 줄과 뜻은 그대로 모른다** (ntdll 쪽, Q63 (d)).
- 비유: 관리사무소가 개업 심사 때 정한 안전 규칙을 작업 지시서에 옮겨 적고, 그걸 다시 관리실 수첩에 베껴 둔다. 관리실은 출근해서 그중 두 줄을 읽는다.

#### `+0xb8` 의 출처 `[정적]` + 아직 `[추측]` 인 앞쪽

- 쓰는 줄은 2.2 한 줄 읽기 ② 에서 봤다 (`+0x15a`).
- 그 값(작업 집합 구조 `+0x4a0`)을 **누가 만들어 두나**:
  - 지도상 `MmInitializeProcessAddressSpace +0x1c9 → MiMapProcessExecutable +0xe3 → MiCfgInitializeProcess` 로 **보인다**.
  - `PspAllocateProcess +0x2095` (`MmInitializeProcessAddressSpace`) 가 `+0x239c` 보다 앞이라 **시간 순서는 맞다** `[정적]`.
  - 하지만 `MiCfgInitializeProcess` 가 정말 `+0x4a0` 칸을 쓰는지는 **안 봤다** → Q63 (b).
- 비유: 교환원(CFG)이 보는 **출입 명단**은 가게 땅을 고를 때(run10 `+0x2095`) 미리 만들어지는 것 같고, 관리사무소는 그 명단이 **어디 있는지**만 수첩에 적어 준다.

#### `+0xf0 ~` 의 출처 `[정적]` (짧게)

- `PspGetScpCfgFunctions` 는 전역 표 `PspNtdllScpFunctions` 의 주소를 돌려주는데, `ZwQueryVirtualMemory` 결과나 `PsGetScpCfgPageTypeForProcess` 값에 따라 **NULL** 을 줄 수도 있다. 그러면 이 칸들은 안 채운다.
- 표를 채우는 건 부팅 때 `PspLocateNtdllAddressesForScpCfg` (`+0x1bf`·`+0x1d6`·`+0x1ec`·`+0x202`, 표 앞 4칸) 이다.
- 그 값이 ntdll 안 **어떤 함수**인지, "SCP" 가 무슨 뜻인지는 **모른다** (Q63 (b)·(f)).

### 2.10 핵심 ④ 짝 표 — 커널이 쓰고, ntdll 이 읽는다

이번 바퀴의 가운데 그림이다. flow.md 2.1 의 새 절 "**커널이 쓰고 ntdll 이 읽는 칸 — 짝 표**".

- 왼쪽(쓰는 줄)은 이번에 본 커널 코드 `[정적]`.
- 오른쪽(읽는 줄)은 **이미 문서(flow.md)에 적혀 있던 ntdll 줄만** 가져왔다. ntdll 을 새로 읽지 않았다.

| 칸 | 커널이 쓰는 줄 `[정적]` | ntdll 이 읽는 줄 (문서) | 짝 |
|---|---|---|---|
| **PEB `+0x20`** | `PspCopyAndFixupParameters +0x52b` 또는 `+0x347` (ret `+0x34c`) | `LdrpInitialize +0x5a` (`Flags` 맨 윗 비트, run17). `LdrpInitializeProcess +0x6c5 RtlNormalizeProcessParams`, `+0xc3f RtlpInitParameterBlock` (run18) | **맞음** |
| `Flags` 비트 29 (`0x20000000`) | `PspSetupUserProcessAddressSpace +0xe6` → `+0xea` 저장 | `LdrpInitializeKernel32Functions` "이미 있음" 길 `+0x1ff` (run20) | **맞음 + 이름 단서** (2.11) |
| `Flags` 비트 15 (`0x8000`) | `PspSetupUserProcessAddressSpace +0xb1` (조건 `PspGlobalFlags` 비트 0) | 문서에 없음 | 빈 곳 (Q63 (d)) |
| `Flags` 비트 14 (`0x4000`) | `PspCopyAndFixupParameters +0x82` | 문서에 없음 | 빈 곳 (Q63 (d)) |
| `LoaderThreads` (+0x40c) | `PspSetupUserProcessAddressSpace +0xb8 ~ +0xc7` (0 이면 `PsDefaultLoaderThreads`) | `LdrpInitializeProcess +0x1a47 LdrpEnableParallelLoading` 의 `ECX` (run19·run24). IFEO 는 `LdrpInitializeExecutionOptions` (ret `+0x536`) 가 같은 칸에 씀 (run18) | **맞음** (2.12) |
| **수첩 `+0xa0`** (24B) | `PspPrepareSystemDllInitBlock +0x12b`, `+0x138` | `LdrpInitializeProcess` 묶음 4 `+0x4a8 ~ +0x579` (run18) | **맞음 (구간)** |
| **수첩 `+0xb8`** | `PspPrepareSystemDllInitBlock +0x15a` | `LdrpDispatchUserCallTarget` `+0x0 ~ +0x2f` (run28) | **맞음** |
| 수첩 그 밖 (`+0x8`·`+0x10`·`+0x18 ~ +0x97`·`+0x98`·`+0x9c`·`+0xc0 ~ +0xd0`·`+0xd8 ~ +0xef`·`+0xf0 ~`) | 2.9 표 | 문서에 없음. `+0x98` 난수 ↔ run18 `LdrInitializePerThreadRng` 는 **이름만 본 후보** `[추측]` | 빈 곳 (Q63 (d)) |

#### 한 줄씩 — 관리실이 출근해서 펴 보는 순서로

(관리실 쪽 순서는 이미 문서에 있는 ntdll 줄 순서다.)

1. **안내 데스크** `LdrpInitialize +0x5a` (run17) — 안내판 `+0x20` 이 가리키는 서류의 `Flags` **맨 윗 비트**를 본다.
   - 이번에 알게 된 것: 그 서류는 **관리사무소가 `+0x1b9` 에서 옮겨 둔 것**이다 `[정적]`. run17 의 "누가 채우는지 모름" 이 풀렸다.
   - 단, 그 **맨 윗 비트(비트 31)를 누가 켜는지는 여전히 모른다.** 사용자는 못 켜고(입력 마스크 `0xf4010e11` 에 들어 있음), 이번에 읽은 커널 줄은 비트 14·15·29 만 켠다 → Q63 (e).
2. **수첩 `+0xa0`** — `LdrpInitializeProcess` 묶음 4 (run18) 가 안전 규칙 칸을 읽는다. 출처는 2.9.
3. **서류 다듬기** — `+0x6c5 RtlNormalizeProcessParams`, `+0xc3f RtlpInitParameterBlock` (run18). 받는 서류는 이미 정규화된 꼴이다 (2.5).
4. **`0x20000000`** — kernel32 준비 `+0x1ff` (run20). 이 비트는 관리사무소가 켠다 (2.11).
5. **`LoaderThreads`** — `+0x1a47 LdrpEnableParallelLoading` (run19·run24). 관리사무소가 기본값을 먼저 깔아 둔다 (2.12).
6. **수첩 `+0xb8`** — 교환원 `LdrpDispatchUserCallTarget` (run28) 이 출입 명단 주소로 쓴다.

- 비유로 한 줄: **관리사무소는 쓰고, 관리실은 읽는다.** 이번 표로 관리실이 읽던 칸 가운데 **여섯 줄의 "쓰는 손"** 이 정해졌다. 나머지는 쓰는 손은 보이는데 **읽는 손이 아직 문서에 없다** (빈 곳).

### 2.11 덤 — run20 의 kernel32 건너뜀 비트 `0x20000000` 은 커널이 켠다

run20 에서 남겨 둔 수수께끼다.

#### run20 에서 알던 것

- `[정적]` (run20) `LdrpInitializeKernel32Functions` 가 kernel32 를 "이미 있음" 으로 찾은 길(`+0x1ff`)에서, `PEB.ProcessParameters.Flags` 의 **`0x20000000`** 이 켜져 있으면 kernel32 쪽 (4) ~ (6) 을 **건너뛴다** ([run20 해설 2.3](run20-kernel32-load-해설.md#23-이미-있나-확인--그리고-시작-때-kernel32-는-없다)).
- 비트 이름은 PDB 에 없었다. 뜻은 `[추측: 특수 모드의 예외 길]`.

#### 이번에 본 것 — 단계별로 `[정적]`

1. `PspSetupUserProcessAddressSpace +0xe6 BTS EBX,0x1d` → `+0xea` 에 저장. `0x1d` = 29, 곧 `0x20000000` 비트를 켠다.
2. 언제 켜나: `PsGetProcessServerSilo` (ret `+0xd5`) → `PsGetServerSiloGlobals` (ret `+0xdd`) → 결과 `+0x3f8` 바이트가 0 이 아니면. 그 칸 이름은 PDB 로 **`_ESERVERSILO_GLOBALS.OneCoreForwardersEnabled`**.
   - **silo** = 컨테이너 같은 격리 칸 `[추측]` (일반적으로 알려진 설명, [run10 해설](run10-allocate-process-해설.md) 3.11 에서 한 줄로 봄). 비유: 상가 안의 **구역**. 구역마다 따로 정한 규칙이 있다.
3. 어디에 켜나: **붙기 전, 관리사무소 사본**에 켠다 (`+0xa0 ~ +0xea` 구간). 그 사본이 `+0x1b9` 에서 그대로 새 가게로 옮겨진다.
4. 사용자는 이 비트를 켜서 넘길 수 없다: 들어올 때 검사 마스크 `0xf4010e11` 에 비트 29 가 들어 있어서, 켜서 넘기면 실패한다 (`PspCaptureUserProcessParameters +0x8f ~ +0xa8`).

그래서 →

- 이 비트는 **커널만 켜는 비트**다 `[정적]`.
- 뜻은 "이 silo 는 OneCore forwarders 가 켜져 있다" 를 서류에 옮겨 적은 것으로 **보인다** `[추측: 이름]`. "OneCore forwarders" 가 무엇인지는 이번 근거에 없다.
- 그래서 run20 의 그 길은 "**kernel32 가 이미 올라와 있고 + 그런 silo 일 때**" `BaseThreadInitThunk` 찾기 등을 건너뛰는 길로 보인다 `[추측: 이름 뜻]`.
- 보통 데스크톱에서 돌린 test.exe 때 그 silo 값이 켜져 있는지는 **실행 때 값**이다 → 동적 Q35.

- 비유: 관리실 업무 매뉴얼에 "**표시가 있으면** 이 단계는 건너뛰라" 는 줄이 있었다 (run20). 그 표시는 손님(부모)이 칠할 수 없는 표시이고, **관리사무소가 "이 가게는 특수 구역 소속" 일 때만** 서류에 찍어 준다.

#### 같은 구간의 다른 비트 두 개 (짧게)

- 비트 15 (`0x8000`): `+0xb1` 이 켠다 (조건 `PspGlobalFlags` 비트 0) `[정적]`.
- 비트 14 (`0x4000`): `PspCopyAndFixupParameters +0x82` 가 켠다 `[정적]` (2.5).
- 둘 다 ntdll 이 **읽는 줄이 문서에 없다** → Q63 (d).

### 2.12 주의 — `LoaderThreads` 처음 값이 0 이 아닐 수 있다

run24 의 짐작 하나가 흔들린다. 틀렸다고 정해진 건 아니다. 단계별로 보자.

#### run24 에서 짐작한 것

- `[정적]` (run24) `LdrpEnableParallelLoading` 은 인자(`LoaderThreads`)가 **0 이면 스레드 수 4**, 아니면 `min(값, 16)` 으로 정한다.
- `[추측]` (run24) IFEO 를 따로 안 줬으면 `LoaderThreads` = 0 → 스레드 수 4 → 일감 개체가 생긴다 ([run24 해설](run24-work-queue-해설.md#호출-버튼은-누가-다나-정적)).
  - 이 짐작의 바탕은 "IFEO 가 없으면 **아무도 그 칸에 안 쓴다**" 였다.

#### 이번에 본 것 `[정적]`

- `PspSetupUserProcessAddressSpace +0xb8 ~ +0xc7`: 관리사무소 사본의 `LoaderThreads` 가 **0 이면, 전역 `PsDefaultLoaderThreads` 값으로 채운다** (`+0xc1` 읽기, `+0xc7` 쓰기).
- 이것도 **붙기 전, 사본에** 하는 일이라, `+0x1b9` 에서 새 가게로 같이 넘어간다.
- 그 뒤 ntdll 이 IFEO `MaxLoaderThreads` 로 **덮을 수 있다** (run18).

#### 그래서 무엇이 달라지나

- 칸에 **쓰는 손이 하나 더 있었다.** "IFEO 가 없으면 0" 이 아니라, "IFEO 가 없으면 **`PsDefaultLoaderThreads` 값**" 이다.
- run24 의 "0 → 4" 는 **그 전역이 0 일 때의 이야기**로 좁혀진다.
- 그 전역 값이 실제로 무엇인지는 정적으로 못 정한다 → 동적 **Q47** (`dd nt!PsDefaultLoaderThreads L1`, plan.md). Q35 와 같은 실행에서 볼 수도 있다.
- run5 `[관찰]` 의 ntdll 일꾼 2개는 어느 쪽과도 아직 부딪치지 않는다 (값을 모르니까).

- flow.md 는 run24 의 `[추측]` 줄을 지우지 않고 **"단 커널이 먼저 손본다"** 는 줄을 바로 아래에 붙였다 (3장 두 곳). 정정할지는 값을 본 뒤에 정한다 (plan.md 결정 기록 "정정은 값 보고").

- 비유: 관리실 매뉴얼엔 "일꾼 정원이 **비어 있으면** 4명" 이라고 적혀 있었다. 그런데 관리사무소가 서류를 넘기기 전에 빈칸을 **본사 기본 정원**으로 채워 둔다. 그러면 관리실이 받을 때는 빈칸이 아닐 수 있다. 본사 기본 정원이 몇 명인지는 장부를 열어 봐야 안다.

### 2.13 test.exe 는 어느 자리를 타나 — 짐작을 단계별로

`PspPrepareSystemDllInitBlock` 을 부르는 자리는 **4곳**이다 `[정적]` (요청서는 3곳이라고 했다).

| 자리 (ret) | 언제 |
|---|---|
| **`PspSetupUserProcessAddressSpace +0x1e6`** | 64비트 ntdll, 새 프로세스가 **보안 프로세스가 아닐 때** (`+0x1c3 ~ +0x1da`) |
| `PspSetupUserProcessAddressSpace +0x219` | 32비트 ntdll, `EPROCESS.WoW64Process ≠ 0` (32비트 exe 만) |
| `PspAllocateProcess +0x25dc` | `PspSetupUserProcessAddressSpace` 를 **안 부른** 갈래 (둘째 인자 0) |
| `PspAllocateProcess +0x25fc` | 위 갈래 + WoW64 |

- **보안 프로세스** = 장부(`_EPROCESS`) 의 `Pcb.SecureState` 안 `SecureProcess` 비트가 켜진 프로세스 `[정적: 비트 자리]`. 이 비트가 켜졌으면 `+0x1e6` 을 건너뛴다.

짐작의 고리:

1. `[정적]` `PspAllocateProcess +0x236c ~ +0x2397` 의 갈림길: 묶음 플래그(스택 `[RSP+0x84]`) **비트 7 켜짐** + 만들기 정보 구조 **≠ NULL** + 묶음 플래그 **비트 1 꺼짐** → `PspSetupUserProcessAddressSpace` (ret `+0x239c`). 아니면 `+0x2574` 갈래 → `+0x25dc` (+ WoW64 면 `+0x25fc`).
2. `[정적]` 비트 7 은 `+0x20d2 BTS EBX,7` 이 켠다. `MmInitializeProcessAddressSpace` 성공 뒤, **섹션 있는 보통 실행**일 때.
3. `[정적]` `NtCreateUserProcess` 는 11번째 인자로 지역 구조(만들기 정보)를 넘기고 (`+0xbe6`), 7번째 인자로 exe 섹션을 넘긴다.
4. 그래서 → test.exe 는 **`+0x1e6` 한 자리**일 것이다 `[정적 조건 + 추측]`. `+0x219`·`+0x25dc`·`+0x25fc` 는 안 설 것 (64비트, 보통 실행).
5. 아직 확인 아님: 실제 플래그 값은 실행 때 값이다 → 동적 Q35 (`nt+0x9395d0` 입구에서 `[rsp]` = `nt+0xae5f1a` 인가).

- 반대쪽(`+0x25d7`) 갈래를 타는 프로세스는 `PspCopyAndFixupParameters` 를 **안 부르니까 PEB `+0x20` 이 0 으로 남는다** `[정적]`. 비유: 서류 없이 문을 연 가게 — 안내판의 "서류는 이 방에" 칸이 빈 채다.

### 2.14 순서 — run10 이 맞았다, 사이에 몇 개 더

| 순서 | ret | 하는 일 | 비고 |
|---|---|---|---|
| 0 | `+0x92` | `PspGetStandardHandleList` | 표준 핸들 상태가 1 일 때만 |
| 0' | — | 사본 `Flags` 비트 15·29, `LoaderThreads` 기본값 (`+0xa0 ~ +0xea`) | **붙기 전, 복사 전** |
| 1 | `+0xfa` | `KiStackAttachProcess` | 들어감 |
| 2 | `+0x19c` | `ZwAllocateVirtualMemory` | 방 잡기 |
| 3 | **`+0x1b9`** | `PspCopyAndFixupParameters` | **PEB `+0x20`** |
| 4 | **`+0x1e6`** | `PspPrepareSystemDllInitBlock` (64비트) | 보안 프로세스면 건너뜀 |
| 4' / 4'' | `+0x1f7` / `+0x219` | `PspWow64SetupUserProcessAddressSpace` / 32비트 수첩 | 4'' 는 WoW64 만 |
| 5 | `+0x227` / `+0x234` | `KeCopyXfdMaskToPeb` / `PspUpdatePebForAffinityChange` | |
| 6 | `+0x255` | `MmMapApiSetView` | 보안 프로세스면 건너뜀 |
| 7 | `+0x263` | `PspMapSiloSharedDataView` | |
| 8 | `+0x26f` | `KiUnstackDetachProcess` | 나옴 |

- run10 순서 **할당 → 매개변수 복사 → 시작 정보 블록 → ApiSet 뷰** 가 맞다 `[정적]`.
- 3번과 4번은 서로의 결과를 **안 읽는다.** 순서는 코드 순서일 뿐이다 `[정적]`. (서류와 수첩은 따로 노는 물건.)
- 0' 이 재밌다: 관리사무소는 **가게에 들어가기 전에** 사무소 사본에 표시(비트 15·29)와 정원(`LoaderThreads`)을 미리 적어 두고, 들어가서는 **그대로 옮기기만** 한다.

### 2.15 정리 — 비유로 한 번 더

1. 부모가 **개업 서류**를 써서 관리사무소 창구에 낸다 (9번째 인자).
2. 창구는 "쪽지에 진짜 위치가 적힌 서류만" 받고, **사무소 서랍에 사본**을 만든다 (`PspCaptureProcessParameters`, `+0xacb`).
3. 가게를 짓고 안내판을 단다. 안내판의 "서류는 이 방에" 칸은 **아직 비어 있다** (run10 `MmCreatePeb`).
4. 들어가기 전에, 사본에 **관리사무소만 찍는 표시**(비트 29 등)와 **기본 일꾼 정원**을 적어 둔다 (`+0xa0 ~ +0xea`).
5. 새 가게 땅에 들어가서 **빈 방**을 잡는다 (`+0x19c`).
6. 사본을 방에 옮기고, 쪽지 12장을 **새 방 기준**으로 고치고, 창구 열쇠(표준 핸들)를 **새로 깎아** 주고, 안내판 빈칸에 **"서류는 이 방에"** 를 적는다 (`+0x1b9`).
7. 관리실 책상 위 **업무 수첩**을 펴서 난수, 안전 규칙, CFG 출입 명단 위치, 검사 담당 함수 주소를 채운다 (`+0x1e6`).
8. 나온다 (`+0x26f`).
9. 나중에 관리실이 출근해서 **안내판과 수첩을 읽기만** 한다 (run17 ~ run28).

### 2.16 곁가지 — 짧게

#### 근거끼리 다른 곳 — run 문서를 따랐다

- 요청서는 `PspCopyAndFixupParameters` 의 호출을 "9" 라고 했지만 실제 **8곳**, `PspPrepareSystemDllInitBlock` 을 부르는 자리는 "3" 이 아니라 **4곳** (`+0x219` 추가, 지도엔 이미 있었음) (결과서).
- 결과서 글자 두 곳을 flow-writer 가 `.asm` 과 맞춰 고쳐 읽었다 (run 문서 "방법"):
  - 결과서 2-b 표 `+0x8` 줄의 "`+0xc3` call (ret `+0xc8`)" 은 `PspWow64GetSharedInformation` 이다. 종류를 정하는 호출은 그 앞 `+0xb7` (ret `+0xbc`) `PsWow64GetProcessNtdllType`.
  - 결과서가 `PspGetScpCfgFunctions` 자리로 적은 `+0x18e` 는 조건 검사 줄이고, 호출은 call `+0x196` / ret `+0x19b`.
  - 이 해설은 run 문서 쪽 숫자를 썼다.

#### 동적 함정 후보 (run 문서 "디버거 메모")

- `PspCopyAndFixupParameters`·`PspPrepareSystemDllInitBlock` 은 **부모 cmd.exe 스레드가 새 프로세스에 붙어 있는 동안** 돈다.
- 그 사이 쓰는 주소(PEB, `Dst`, `LdrSystemDllInitBlock`)는 **새 프로세스의 유저 주소**라, 디버거가 부모 문맥으로 읽으면 엉뚱한 값을 볼 수 있다 `[추측]`. `.process /r /p <새 EPROCESS>` 가 필요할 수 있다 `[추측]`.
- 비유: 관리사무소 직원이 **새 가게 안에서** 적은 번지를, 밖(부모 가게)에서 찾아가면 엉뚱한 집이 나온다.

#### 도구 메모

- PDB 구조체 칸 표를 뽑는 명령이 스킬에 없어서, 결과서가 이번에도 `llvm-pdbutil` 덤프를 임시 스크립트로 읽었다 (plan 하네스 후보 (40) 과 같은 이야기).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb 쓴 run 목록에 run32 없음, 정적 근거 run9 ~ run31 | run32 추가, 정적 근거 run9 ~ run32 (ntoskrnl 에 run32) |
| 그림 1 `p8` | "정적" | "붙기 전 커널 사본 Flags·LoaderThreads 손봄, 붙은 뒤 오른쪽 둘" |
| 그림 1 `p8` 아래 | 없음 | **`(정적)` 상자 둘**: `p9` `PspCopyAndFixupParameters` (화살표 `(1) +0x1b9`), `p10` `PspPrepareSystemDllInitBlock` (화살표 `(2) +0x1e6`). 그림 앞 설명 단락 추가 |
| 2.1 트리 | `(4) +0x239c PspSetupUserProcessAddressSpace [정적]` 한 줄 | 그 아래 `(1) +0x1b9`, `(2) +0x1e6` 두 줄 |
| 2.1 순서 설명 | 주소 공간 → exe → ntdll → PEB | + (run32) 그다음 **매개변수 복사(+ PEB `+0x20`) → ntdll 시작 정보 블록** |
| 2.1 `MmCreatePeb` 절 | `[정적]` "+0x20 은 누가 채우는지 아직 모른다" | 취소선 → **`PspCopyAndFixupParameters` 가 쓴다 `[정적]`**. `+0x18` 은 ntdll `+0x1c1`, `+0x30` 은 ntdll `+0xa50` 쪽 (run18) 이라고 정리. 안내판 비유에 "서류는 이 방에" 칸 |
| 2.1 `PspSetupUserProcessAddressSpace` 절 | `[추측]` 두 줄 (매개변수 복사·PEB `+0x20`, ntdll 시작 정보) | 둘 다 취소선 → **맞다 `[정적]`**. 순서 전체(call/ret), 부르는 조건, test.exe 는 `+0x1e6` `[추측]`, 비유 추가 |
| 2.1 새 절 셋 | 없음 | `nt!PspCopyAndFixupParameters`, `nt!PspPrepareSystemDllInitBlock`, **"커널이 쓰고 ntdll 이 읽는 칸 — 짝 표"** |
| 2.4 `LdrpInitialize` | `Flags` 맨 윗 비트 `[공백]` | + 그 블록은 커널이 복사해 건 것 (run32), 비트 31 켜는 곳은 모름 → Q63 (e) |
| 2.4 `LdrpInitializeProcess` | 묶음 4 의 `+0xa0` | + 그 칸은 커널 `+0x12b`·`+0x138`, 출처 완화 옵션 (run32) |
| 2.5 kernel32 | `0x20000000` `[추측: 특수 모드의 예외 길]` | + **커널 `+0xe6` 이 켠다 `[정적]`**, 조건 silo `OneCoreForwardersEnabled`, 사용자는 못 켬 |
| 2.7 CFG | 칸 값 → 동적 Q27 | + 비트맵 주소 칸 `+0xb8` 은 커널 `+0x15a` 가 채움 (run32) |
| 3장 일꾼 (두 곳) | run24 `[추측]` "0 → 4" | 지우지 않고 아래에 **"단 커널이 먼저 `PsDefaultLoaderThreads` 로 채움"** 추가 (Q47·Q35) |
| 4장 | 2.1·2.4 "PEB `+0x20` 누가 쓰나", 2.1 "`PspPrepareSystemDllInitBlock` 안쪽" | 둘 다 **해결됨 (정적)**. 새 줄: Q35 (동적 1 ~ 4), Q63 (작은 정적). 2.4 `+0xa0` 비트·2.5 `0x20000000`·3장 일꾼 줄에 **(일부, 정적) → run32** |
| 4.1 | `+0x239c` 에서 PEB `+0x20` 이 채워졌는지 | + 부르는 조건. 새 줄: 상자 둘 확인 방법 (Q35 멈춤점, 안 서야 하는 세 자리) |
| 5장 근거 표 | run31 까지 | run32 줄 추가 |

- 상자를 **둘** 둔 이유 (run 문서): 둘 다 ntdll 이 나중에 읽는 칸을 쓰는 **대표 지점**이라서. 할당·ApiSet 뷰 등 나머지 호출은 run10 때처럼 글에만 둔다.
- `(정적)` 점선 상자인 이유: 코드로는 봤지만 **실행은 아직 안 봤다.** 어느 갈래(기능 스위치)로 쓰는지·실제 값은 동적 Q35.

### 큰 그림에서 어떻게 이어지나

- **커널 지도와 ntdll 지도가 칸 단위로 이어졌다.**
  - 단계 1(nt) 에서는 "관리사무소가 무엇을 하나" 를, 단계 2(ntdll) 에서는 "관리실이 무엇을 읽나" 를 따로 봤다.
  - 이번 짝 표로 **"관리실이 읽는 칸을 관리사무소 어느 줄이 썼나"** 가 여섯 줄 맞춰졌다 (2.10).
  - run31 이 진입점 값 **한 가닥**을 처음부터 끝까지 이었다면, 이번엔 **안내판과 수첩이라는 두 통로**가 이어졌다.
- **run10 에서 남긴 질문이 닫혔다.** 안내판 `+0x20` 빈칸 → `PspCopyAndFixupParameters` `[정적]`.
- **run20·run24 의 `[추측]` 두 개에 커널 쪽 근거가 붙었다.** 하나(`0x20000000`)는 켜는 손이 정해졌고, 하나(`LoaderThreads`)는 짐작의 바탕이 흔들렸다. 둘 다 마지막 확인은 실행 값이다 (Q35·Q47).
- **동적 단계 준비**: Q35 에 이번 확인거리가 붙어서, `NtCreateUserProcess` 한 번 안에서 `+0x1b9`(서류 옮긴 직후)·`+0x1e6`(수첩 채운 뒤)를 같이 보면 된다. run31 의 Q34·Q61 과도 같은 실행이다 (plan.md 결정 기록).
- 다음 바퀴: 단계 3 정적 묶음의 다음 항목 **Q40** (plan.md).

---

## 4. 아직 모르는 것

멈춤점은 모두 커널이라 **모든 프로세스에 걸린다.** test.exe 일 때만 서게 조건이 필요하다 (dast-mentor 몫).

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 기능 스위치가 켜졌나 → PEB `+0x20` 을 `+0x52b` 로 쓰나 `+0x347` 로 쓰나 | 스위치는 실행 때 전역 값 | 동적 **Q35**: `dd nt+0xfc4110 L1` (비트 4·0) |
| test.exe 가 정말 `+0x1e6` 한 자리로 서나 | 묶음 플래그·인자 9 비트 11 은 실행 때 값 | 동적 **Q35**: `nt+0x9395d0` 입구에서 `[rsp]` = `nt+0xae5f1a` 인가. `nt+0xa04b44`·`nt+0xa04b64`·`nt+0xae5f4d` 는 안 서야 함 |
| 복사 직후 실제 값: 새 PEB `+0x20`, `Flags` 의 `0x1`·`0x4000`·`0x8000`·`0x20000000`, `LoaderThreads`, 문자열 `Buffer` 가 `Dst` 안인가 | 실행 때 값 | 동적 **Q35**: `nt+0xae5eed` (`+0x1b9`). 붙은 상태라 읽기 문맥 주의 `[추측]` (2.16) |
| 수첩 채운 뒤 값: `+0x98` 난수, `+0xa0` 24B, `+0xb8`/`+0xc0`, `+0xf0 ~` 이 ntdll 어느 함수인가 | 실행 때 값 | 동적 **Q35**: `nt+0xae5f1a` (`+0x1e6`) 에서 블록 전체, 함수 주소는 `ln` |
| 보통 데스크톱 test.exe 때 silo `OneCoreForwardersEnabled` 가 켜져 있나 (= `0x20000000` 이 켜지나) | 실행 때 값 | 동적 **Q35** (위 `Flags` 확인과 같이) |
| `PsDefaultLoaderThreads` 실제 값 (= IFEO 없을 때 ntdll 이 받는 `LoaderThreads`). run24 `[추측]` "0 → 4" 가 맞나 | 전역의 실행 때 값 | 동적 **Q47**: `dd nt!PsDefaultLoaderThreads L1` (Q35 같은 실행에서도 가능) |
| (a) 입력 검사 마스크 `0xf4010e11` 각 비트 이름, `Environment` 배치 | `PspCaptureUserProcessParameters` 는 머리와 호출 이름만 읽음 | 정적 **Q63 (a)** |
| (b) `MiCfgInitializeProcess` 가 작업 집합 구조 `+0x4a0`/`+0x4a8` (수첩 `+0xb8`/`+0xc0` 출처) 을 쓰나, `PspLocateNtdllAddressesForScpCfg` 가 넣는 ntdll 함수 이름 | 지도로만 본 길, 범위 밖 | 정적 **Q63 (b)** |
| (c) 만들기 정보 `+0x11` (표준 핸들 상태) 를 누가 채우나 | `PspBuildCreateProcessContext` 를 안 봄 | 정적 **Q63 (c)** |
| (d) 수첩의 문서 없는 칸과 `Flags` `0x4000`·`0x8000` 을 ntdll 이 읽는 줄, `RtlNormalizeProcessParams` 가 이미 정규화된 블록에서 하는 일, `+0xa0` 두 비트의 정확한 읽는 줄·뜻 | 이번엔 ntdll 을 새로 안 읽음 (문서에 있던 줄만 짝지음) | 정적 **Q63 (d)** — ntdll 새로 읽기라 **할지는 차례 때 사용자와 정함** |
| (e) `Flags` 비트 31 (run17 `LdrpIsSecureProcess` 비트) 을 누가 켜나 | 사용자는 못 켜고, 이번에 읽은 커널 줄은 비트 14·15·29 만 켬 | 정적 **Q63 (e)** |
| (f) `PackageDependencyData`(+0x400)·`CurrentDirectores[32]` 안쪽을 안 고치는 이유, 스위치 이름의 "SegmentHeap", "SCP" 의 뜻 | 코드가 이유·이름 뜻을 말해 주지 않음 | 정적 **Q63 (f)** |
| 부모 쪽 9번째 인자를 `BasepCreateProcessParameters` 가 만드나 | 이번엔 커널 쪽만 봄 | run14 `[추측]` 그대로 (번호 없음) |

---

## 5. 더 읽기

- run 문서: [`run32-params-initblock.md`](run32-params-initblock.md) — 호출 지점 표, `PspSetupUserProcessAddressSpace` 안 순서, PEB `+0x20` 두 갈래, Src/Dst, Fixup 12칸, 핸들 복제, 수첩 칸 전체 표, 부르는 자리 4곳과 갈림 조건, **짝 표**, 호출 관계 트리, flow.md 판정, 동적 확인거리, 디버거·도구 메모
- 정적 결과서: [`_workspace/sast/27_params-initblock_result.md`](../../_workspace/sast/27_params-initblock_result.md) — 질문 1 ~ 4 답, 갈래 별 줄 범위, `PspAllocateProcess` 묶음 플래그 정하는 줄, 추천 멈춤점
- 요청서: [`_workspace/sast/27_params-initblock.md`](../../_workspace/sast/27_params-initblock.md)
- flow.md: [`flow.md`](flow.md) — 1장 그림 1 (`p8` 아래 `p9`·`p10`), 2.1 `nt!MmCreatePeb` · `nt!PspSetupUserProcessAddressSpace` · `nt!PspCopyAndFixupParameters` · `nt!PspPrepareSystemDllInitBlock` · **"커널이 쓰고 ntdll 이 읽는 칸 — 짝 표"**, 2.4 `LdrpInitialize`·`LdrpInitializeProcess` 묶음 4, 2.5 `LdrpInitializeKernel32Functions`, 2.7 CFG 중개, 3장 일꾼, 4장 2.1·2.4·2.5·3 줄, 4.1
- 앞 해설과 잇기:
  - [run10 해설 3.7](run10-allocate-process-해설.md#37-안내판에-쓰는-것-안-쓰는-것) — 안내판 `+0x20` 빈칸을 처음 본 곳
  - [run16 해설 2.9](run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다) — 벽 주소록 `PsNtdllExports` (이번엔 그 `+0x0` 칸)
  - [run17 해설 2.5](run17-ldr-init-entry-해설.md#25-ldrpinitialize0xe1448-가-비켜서기-전에-하는-잡일) — `Flags` 맨 윗 비트를 읽는 안내 데스크
  - [run18 해설 "묶음 하나씩"](run18-proc-init-setup-해설.md#묶음-하나씩--무엇을-비유로) — 묶음 4 `LdrSystemDllInitBlock+0xa0`
  - [run20 해설 2.3](run20-kernel32-load-해설.md#23-이미-있나-확인--그리고-시작-때-kernel32-는-없다) — `0x20000000` 건너뜀 길
  - [run24 해설 "호출 버튼은 누가 다나"](run24-work-queue-해설.md#호출-버튼은-누가-다나-정적) — `LoaderThreads` 0 → 4 짐작
  - [run28 해설 2.9](run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다) — CFG 교환원, 비트맵 칸
  - [run31 해설](run31-image-section-해설.md) — 바로 앞 바퀴 (단계 3 첫 바퀴, 진입점 값의 처음 출처)

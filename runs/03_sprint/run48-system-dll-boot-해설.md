# run48 system-dll-boot — 해설

> 이 글은 [run48 문서](run48-system-dll-boot.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

> **고쳐 읽기 (run119)**: 2.4 "덤" · 4장 표의 "`\SystemRoot` 링크 만드는 곳 — 후보 `Phase1InitializationDiscard +0xc31 CreateSystemRootLink` 는 이름 짐작 `[추측]`" 은 이제 `[정적]` 이다 — 부르는 곳은 그 한 곳뿐이고, `CreateSystemRootLink +0x2b4 NtCreateSymbolicLinkObject` 가 `\SystemRoot` (→ `\Device\BootDevice` + `NtBootPathName` 끝 한 글자 뺌) 를 만든다. `PsLocateSystemDlls` (`+0x10f5`) 보다 늘 먼저다. 뒤의 `IopReassignSystemRoot` (`+0x1139`) 는 `\Device\BootDevice` 만 다시 만들고 `\SystemRoot` 는 안 바꾼다 (Q95 끝). 본문은 그대로 두었다 → [run119 해설](run119-systemroot-link-해설.md) 2.4 · 2.9 · 2.10

- 대기열: **Q93** — 부팅 준비 갈래, 시스템 DLL (`_workspace/plan.md`, 이번에 **끝**)
  - 원래 대기열에 없던 주제다. 사용자가 2026-10-29 "다음 분석" 으로 직접 꺼냈다. 그래서 맨 뒤에 Q93 으로 넣고, **Q78c 앞에** 먼저 했다 (결정 기록).
  - 다음 항목: **Q78c** (프로세스 객체) → Q78d → Q78e → Q78f → Q78g → Q79 → Q80 → Q82.
  - 남은 동적 질문: D1·D2 는 원래 있던 **Q42** 에 합쳤다. D3 ~ D12 는 새 항목 **Q94** (D9 ~ D12 는 낮은 우선순위). 작은 정적 하나가 새 **Q95** (`\SystemRoot` 링크 만드는 곳).
- 근거:
  - run 문서 [`run48-system-dll-boot.md`](run48-system-dll-boot.md) (가장 중요)
  - 요청서 [`_workspace/sast/42_system-dll-boot.md`](../../_workspace/sast/42_system-dll-boot.md)
  - 결과서 [`_workspace/sast/42_system-dll-boot_result.md`](../../_workspace/sast/42_system-dll-boot_result.md) (판정: **분석성공**)
  - 짐 지도 [`baggage.md`](baggage.md) 의 고친 곳: "쓰는 말" (`Ⓑ`) · 1 · 2 · 3.8 · 3.15 · 4 · 5 · 6 · 7.11 · 8장, 새 절 **7.6.2**
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (기준 · 2.1 `PsMapSystemDlls` 절 · 2.3 끝 절 · 4장 · 5장. 그림은 안 바뀜)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - `PsNtdllExports` 값을 맞춰 보려고 run44 K1 덤프 읽기 로그 (`ms/logs/04_baggage-read_K1.txt` 1531 ~ 1542줄) 를 **다시 읽었다**. 새로 VM 을 돌린 건 없다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 핸들(**손잡이**), 비트와 플래그 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 이미지 섹션 (**조립식 건물 세트**) → [run9 해설 "이미지 섹션"](../02_sprint/run9-create-user-process-해설.md#이미지-섹션)
- 매핑 (**건물 갖다 놓기**), 관리실 (ntdll) 건물, WOW64 → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 2절 · [3.10](../02_sprint/run10-allocate-process-해설.md#310-ntdll-쪽-조금-더)
- 섹션 사슬 다섯 고리, 컨트롤 영역 (서류함의 **서랍**), 섹션 객체 (**주문 꾸러미**), 서명 재시도, ASLR·재배치 델타 → [run31 해설](../02_sprint/run31-image-section-해설.md) 2.4 · [2.5](../02_sprint/run31-image-section-해설.md#새-말--컨트롤-영역) · [2.6](../02_sprint/run31-image-section-해설.md#새-말--섹션-객체) · 2.10
- exe 섹션 묶음을 칸 단위로 본 것 → [run47 해설](run47-section-chain-해설.md)
- `PsNtdllExports` (관리사무소 **벽 주소록**), 내보내기 표 (ntdll 건물의 **호실 이름표**), `KeBugCheckEx` (시스템을 멈춤) → [run16 해설 2.9](../02_sprint/run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다)
- `LdrSystemDllInitBlock` (**업무 수첩**), init 블록의 재배치 칸 → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- 짐 · 짐 지도, "쓰는 말" (공간 · 단계 · 놓는 법 · 쓴 주인), `입력` → [run43 해설 2.2](run43-baggage-checklist-해설.md#22-읽기-0단계--짐-지도-맨-위-쓰는-말)
- 사진 지점 K1, "처음 값을 갖는 구간" (`K1 이전`) → [run44 해설 2.10](run44-baggage-snap-해설.md#210-새-말--처음-값을-갖는-구간)

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 주소 공간 = 가게의 **빈 땅**, 매핑 = **건물 갖다 놓기**, 짐 = **개업 준비물**.
- 이미지 섹션 = **조립식 건물 세트**, 섹션 객체 = 그 세트의 **주문 꾸러미** (run9 · run31).
- ntdll = 가게마다 하나씩 들어가는 **관리실 건물** (run10).
- `PsNtdllExports` = 관리사무소 **벽 주소록**, 부팅 = 관리사무소가 **문 여는 날** (run16).
- run10 해설은 이미 이렇게 짐작했다: "exe 세트는 가게마다 그때그때 짠다. 관리실 세트는 **한 벌** 있고, 가게마다 그걸 갖다 붙인다" `[추측]`. 이번 바퀴는 그 짐작을 코드로 확인한 바퀴다.
- run 문서가 이번에 쓴 비유 ("아파트 관리실", "공용 열쇠 꾸러미") 는 이 글에서 이렇게 바꿔 부른다:
  - **아파트 관리실 → 관리사무소**. 이 글들에서 "관리실" 은 ntdll 이라서다 (run10). 둘이 섞이면 헷갈린다.
  - **공용 열쇠 꾸러미 → 관리실 세트 한 벌** (ntdll 섹션).
  - **꾸러미 번호표 (`PspSystemDlls` 칸) → 세트 대장의 한 줄** (2.5 에서 푼다).
  - **자주 쓰는 문 위치 목록 → 벽 주소록** (run16 과 같은 말).
  - run10 은 그 한 벌이 "본사 창고" 에 있다고 했지만, run18·run46 해설에서 **창고 = 프로세스 힙**이 됐다. 그래서 이 글에선 **관리사무소 보관함**이라고 부른다.

### 한 줄로 먼저: 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| 사용자 짐작 "ntdll 섹션·기본 주소·`PsNtdllExports` 는 부팅 때 준비, 프로세스마다는 붙이기만" | **맞다** `[정적]`. 프로세스마다는 **읽기·비교·올리기**뿐이다 |
| 언제 준비하나 | 부팅 `Phase1` 에서 **한 번**. `IoInitSystemPreDrivers +0x10f5` (ret `+0x10fa`) → `PsLocateSystemDlls` |
| 몇 개 준비하나 | 표 6칸 중 셋: `[0]` ntdll · `[1]` SysWOW64 ntdll · `[4]` vertdll (표 처음 값 기준) |
| 세트 만드는 법 | exe 와 **같은** `MmCreateSpecialImageSection` 사슬. 인자만 다름 (토큰 NULL · 서명 수준 `0x0c` · 보호 이미지 패킷 플래그) |
| 기본 주소 칸 `+0x20` 을 쓰는 곳 | `PspMapSystemDll` 떨어진 조각 rva `0xb18c2c` (인자4 = 1 일 때) |
| 프로세스마다 하는 일 | 같은 `PspMapSystemDll` 을 인자4 = 0 으로 → `+0x172` 에서 "`+0x20` 과 같은 주소에 붙었나" **비교만** |
| `PsNtdllExports` 를 쓰는 곳 | `PspInitializeSystemDlls +0x6b` 하나. 21칸 중 19칸 (`+0x38`·`+0x98` 은 안 채움, run44 사진도 0) |
| 부팅 안 순서 | 섹션·기본 주소 (`PsLocateSystemDlls`) **먼저** → 벽 주소록 (`PspInitPhase2`) **나중** (run16 의 `[공백]` 닫힘) |
| 실패하면 | 필수 칸 (ntdll) 은 `KeBugCheckEx(0x6b, …)` — 부팅이 멈춘다. 사이트 7곳 |
| 디버거가 붙어서 달라지는 점 | 칸마다 `ZwSystemDebugControl(0x26)` 을 부른다. 디버깅 부팅이면 "끌어온 파일 표" 까지 간다 |
| 정정 | run16: `PsLocateSystemDlls +0x89` 는 별칭 칸 전용 (이 빌드엔 안 탐). run10: 부팅 때 System 프로세스에 올리는 caller 는 `PspInitPhase3` 가 아니라 `PspLocateSystemDll +0x233` |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 구간은 지도 **바깥**, 그것도 **맨 앞**이다. test.exe 를 실행하기 한참 전, 컴퓨터를 켤 때다.

```
(컴퓨터 켜기 = 관리사무소 문 여는 날)
 부팅 Phase1 스레드
  ├─ IoInitSystem → IoInitSystemPreDrivers → PsLocateSystemDlls    관리실 세트 한 벌 만들기          ← 이번 (Ⓑ1)
  └─ Phase1InitializationIoReady → PspInitPhase2                    벽 주소록 적기                    ← 이번 (Ⓑ2)
      ⋮   (로그인, cmd.exe 실행 …)
──────────────── 흐름도·짐 지도 범위는 여기부터 ────────────────
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 커널, 부모 스레드 안
  └─ PspAllocateProcess → … MiMapProcessExecutable → PsMapSystemDlls → PspMapSystemDll   관리실 세트 갖다 붙이기 (비교만)
 test.exe 메인 스레드: ntdll 로더 … NtContinue 은퇴 → (CRT) → main
```

- 흐름도 범위는 "프로세스 생성 → 진입점" 이다. 이번 사슬은 그보다 **앞 (부팅)** 이라 **그림에는 안 넣었다** (run 문서).
- 대신 flow.md **2.1** `PsMapSystemDlls` 절과 **2.3** 끝 절 ("커널이 `LdrInitializeThunk` 주소를 아는 법") 에 메모로 붙었다.
- 짐 지도로는 새 절 **7.6.2** 다. 새 단계 표시 `Ⓑ` 가 생겼다 (2.3).

### 무엇이 궁금했나

짐 지도에는 프로세스 생성 (①) 때 커널이 **읽기만** 하는 짐이 있었다. 이름하여 `입력`.

- `PspSystemDlls` 표 · ntdll 섹션 · 기본 주소 (`+0x20`) · `PsNtdllExports`.
- 그런데 이것들을 **누가, 언제** 만들었는지는 몰랐다. run10·run16 은 "부팅 때 준비" 라고만 적었다.

요청서 질문은 넷이다.

1. 부팅에서 누가 `PsLocateSystemDlls` 를 부르나, 순서는?
2. `PspLocateSystemDll` 이 줄마다 하는 일 (파일 열기, 섹션 만들기, 버그체크 조건, 핸들 닫기)
3. `PspMapSystemDll` 의 부팅 갈래. 기본 주소 `+0x20`·`+0x28` 의 출처, `PsNtdllExports` 를 채우는 줄
4. 이 구간이 쓰는 상태, 그리고 프로세스 생성 때 그걸 **읽는** 자리와 짝

그리고 사용자 짐작 `[추측]` 하나: "ntdll 섹션·기본 주소·`PsNtdllExports` 는 부팅 때 준비되고, 프로세스마다는 붙이기만 한다." 이게 맞나.

- 범위 밖: 부팅 전체 순서 (이름만), `PspInitPhase3` VBS 갈래 본문, WOW64 ntdll 매핑 내용 (도는지만).

### 바퀴 전에 알던 것

- `[정적]` (run10) `PsMapSystemDlls` 는 `PspSystemDlls` 표를 최대 6칸 돈다. 0번 칸은 늘 `PspMapSystemDll` 을 부른다. `PspMapSystemDll` 은 `+0x40 PspReferenceSystemDll` 로 섹션을 잡고 `+0x11d MmMapViewOfSectionEx` 로 올린다.
- `[정적]` (run10) `PspMapSystemDll` 을 부르는 곳은 넷: `PsMapSystemDlls` · `PspInitPhase3` · `PsLocateSystemDlls` · `PspLocateSystemDll`. 나머지 셋은 "부팅 때 준비 쪽" 일 것 `[추측]`.
- `[정적]` (run16) 칸 `+0x20` 은 부팅 때 `PspMapSystemDll` 이 **인자 1** 로 불릴 때 채운다. 프로세스마다는 `+0x172` 에서 그 주소에 붙었는지만 검사한다.
- `[정적]` (run16) 벽 주소록 `PsNtdllExports` 는 부팅 때 `PspInitPhase2 +0x177 PspInitializeSystemDlls` 가 이름으로 찾아 적는다.
- `[공백]` (run16) 부팅 안에서 `PsLocateSystemDlls` 와 `PspInitPhase2` 의 **앞뒤 순서**.
- `[관찰]` (run44) 새 스레드 첫 트랩 프레임 `Rdx` = ntdll 실제 베이스 `7ffafb660000`. `PsNtdllExports` 21칸 값도 사진에 있다. 단 `PspSystemDlls` 칸 자체는 안 읽었다.

---

## 2. 찾은 것

### 2.1 한눈에: 문 여는 날 하는 일 두 덩어리

run 문서 "호출 관계" 를 줄였다. 모두 `[정적]`. `+0x..` 는 돌아올 자리 (ret).

```
Phase1Initialization                                     부팅 Phase1 스레드
 ├─ +0x3b  IoInitSystem
 │    └─ +0x17  IoInitSystemPreDrivers
 │         │  +0x10e3 ~ +0x10ef  NtGlobalFlag |= 0x40000 (임시)
 │         ├─ +0x10fa  PsLocateSystemDlls                 표 6칸 고리
 │         │     └─ +0xa3  PspLocateSystemDll             [0] ntdll · [1] SysWOW64 ntdll · [4] vertdll
 │         │           ├─ +0x7e   ZwSystemDebugControl(0x26)
 │         │           ├─ +0xc5   ZwOpenFile               파일 열기
 │         │           ├─ +0xf4   MmCreateSpecialImageSection   세트 만들기 (exe 와 같은 사슬)
 │         │           ├─ +0x183  MmGetSectionInformation(2) → 칸 +0x38
 │         │           ├─ +0x1b3  ObInitializeFastReference   → 칸 +0x0 이 가리키는 자리
 │         │           └─ +0x238  PspMapSystemDll(현재 프로세스, 칸, 0, 1)   → 칸 +0x20 · +0xc · +0x28
 │         │  +0x10fa  NtGlobalFlag 되돌림
 └─ +0x66  Phase1InitializationIoReady
      ├─ +0x1da  PsInitSystem(2) → PspInitPhase2
      │            └─ +0x177  PspInitializeSystemDlls      +0x6b  PsNtdllExports 에 씀
      ├─ +0x1e7  MmInitSystemDll                           +0x22  MiState.Processes.SystemDllBase
      └─ +0x20f  PsInitSystem(3) → PspInitPhase3           VslVsmEnabled 일 때만
```

- 한 줄 요약: **관리실 세트를 한 벌 만들고 (`PsLocateSystemDlls`) → 그 세트에서 벽 주소록을 적는다 (`PspInitPhase2`).**
- 두 덩어리의 주인이 다르다:
  - 세트 · 세트 대장 칸 (`PspSystemDlls`) → `PspLocateSystemDll` 과 그 안의 `PspMapSystemDll`
  - 벽 주소록 (`PsNtdllExports`) → `PspInitializeSystemDlls`

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 시스템 DLL 찾기가 시작되는 줄

run 문서 요약 표 3번 줄: `IoInitSystemPreDrivers → PsLocateSystemDlls | +0x10f5 | +0x10fa`.

- **누가**: `IoInitSystemPreDrivers` (드라이버 올리기 전 준비, 4821바이트)
- **어디서**: `+0x10f5` 에서 부르고, 돌아올 자리는 `+0x10fa`
- **누구를**: `PsLocateSystemDlls`
- **무엇을 위해**: 시스템 DLL (ntdll 등) 을 찾아 세트를 만들려고 `[정적]`.
- 눈여겨볼 점 둘:
  - 부르기 **바로 앞** (`+0x10e3 ~ +0x10ef`) 에 `NtGlobalFlag` 에 비트 `0x40000` 을 켜고, 돌아오자마자 (`+0x10fa`) 되돌린다. 왜 그러는지는 2.14.
  - `PsLocateSystemDlls` 를 부르는 곳은 **여기 하나뿐**이다 (`_refs`: 코드 호출 1줄 + `.pdata` 1줄) `[정적]`.
- run16 이 적은 `+0x10f5` 는 call 자리다. WinDbg 콜스택에 보일 값은 ret `+0x10fa` 다 (run 문서 정정).

#### 한 줄 읽기 ②: 세트 대장 칸을 채우는 줄

요약 표 7l 줄: `PspLocateSystemDll → PspMapSystemDll (부팅 갈래) | +0x233 | +0x238`.

- **누가**: `PspLocateSystemDll` (표 한 칸을 맡아 처리하는 함수)
- **어디서**: `+0x233` 에서 부르고, 돌아올 자리는 `+0x238`
- **누구를**: `PspMapSystemDll`, 인자 = (현재 프로세스, 칸, 0, **1**)
- **무엇을 위해**: 방금 만든 세트를 **지금 프로세스에** 한 번 갖다 놓고, 그 결과로 칸 `+0x20` (기본 주소) · `+0xc` (`SizeOfImage`) · `+0x28` (뷰 시작) 을 **채우려고** `[정적]`.
- 눈여겨볼 점: 프로세스 생성 때도 **같은 함수**를 부른다. 다른 건 넷째 인자 하나 (1 / 0). 이게 이번 바퀴의 핵심이다 → 2.9.

#### 한 줄 읽기 ③: 벽 주소록에 적는 줄

CALL 이 아니라 **값을 쓰는 줄**이다. 요약 표 10d 줄: `PspInitializeSystemDlls +0x6b`.

- **누가**: `PspInitializeSystemDlls`
- **어디서**: `+0x6b` (`MOV [RCX],RAX`)
- **무엇을**: 이름으로 찾은 ntdll 함수 주소를 `PsNtdllExports` 칸에 쓴다 `[정적]`.
- 이 한 줄이 `PsNtdllExports` 의 **유일한 쓰기 줄**이다 (`_refs/PsNtdllExports.tsv` 쓰기 1줄). 19칸이 모두 이 줄을 지난다 → 2.11.

### 2.3 새 말: 부팅 단계 `Ⓑ`

#### 뜻

- 짐 지도의 단계는 원래 둘이었다: `①` 커널 단계 (`NtCreateUserProcess` ~ 유저로 넘어가기), `②` ntdll 단계 (`LdrInitializeThunk` ~ `NtContinue`).
- 이번에 셋째가 생겼다. **`Ⓑ` = 부팅 단계 = 프로세스 생성 전, 컴퓨터를 켤 때 한 번** (짐 지도 "쓰는 말").
  - 짐 지도 범위 (①·②) **앞**이다. 그래서 여기 놓인 짐은 ① 에서 보면 `입력` 이다.
  - "`⓪` 이전" 이라고 안 부른 이유: `⓪` 은 이미 사진 지점 `P0` (`PspUserThreadStartup` 입구) 이름이라 섞인다 (sast/42 의견).
- 비유: 관리사무소가 **문 여는 날** 하는 일. 가게 (프로세스) 가 하나도 없을 때 미리 해 두는 준비다. 가게가 생길 때마다 하는 일 (①) 과 따로 센다.

#### 쪼갬 (짐 지도 그대로)

| 표시 | 언제 | 대표 짐 |
|---|---|---|
| `Ⓑ0` | 링크 때 (PE 파일에 박힌 처음 값) | `PspSystemDlls` 표 `[0][1][4]`, 칸의 경로·플래그·머신·짧은 이름, 이름 표 `NtdllExportInformation` |
| `Ⓑ1` | `Phase1` 안 `PsLocateSystemDlls` 구간 | 섹션 객체, 칸 `+0x20`·`+0xc`·`+0x28`·`+0x38`, System 프로세스 안 ntdll 뷰 |
| `Ⓑ2` | `PspInitPhase2` (`PspInitializeSystemDlls`) | `PsNtdllExports` 19칸, `PsWowX86SharedInformation` |
| `Ⓑ3` | `MmInitSystemDll` | `MiState.Processes.SystemDllBase` |
| `Ⓑ4` | `PspInitPhase3` (VBS 일 때만) | `PsSecureSystemProcess` 안 ntdll·vertdll 뷰 |

- `Ⓑ0` 이 따로 있는 게 재밌다. 표의 몇 칸은 **누가 쓰는 게 아니라** ntoskrnl.exe 파일 안에 처음부터 적혀 있다. 결과서가 PE 파일 바이트를 직접 읽어서 알았다 `[정적]`.
- 짐 지도 2장 숫자 (체크리스트 21종 칸 세기) 에는 안 넣는다. 체크리스트 밖 커널 전역이라서다. 7.6.2 안에서만 따로 센다 (3장).

### 2.4 부팅 안 순서: 세트가 먼저, 주소록이 나중

run16 이 비워 둔 `[공백]` 이 이번에 닫혔다.

#### 단계별로 `[정적]`

1. `Phase1Initialization` 이 세 함수를 이 순서로 부른다: `Phase1InitializationDiscard` (`+0x1e`) → `IoInitSystem` (`+0x36`) → `Phase1InitializationIoReady` (`+0x61`).
2. `PsLocateSystemDlls` 는 둘째 `IoInitSystem` 안에 있다 (2.2 ①).
3. `PspInitPhase2` 는 셋째 `IoReady` 안에 있다 (`+0x1d5` `PsInitSystem(2)`).
4. 그 뒤 IoReady 안에서 `MmInitSystemDll` (`+0x1e2`) → `PsInitSystem(3)` (`+0x20a` → `PspInitPhase3`).
5. 그래서 → **`PsLocateSystemDlls` → `PspInitPhase2` → `MmInitSystemDll` → `PspInitPhase3`**.

#### 왜 이 순서여야 하나

- `PspInitializeSystemDlls` 는 칸마다 `PsQuerySystemDllInfo` 를 먼저 부른다. 이 함수는 칸 `+0x20` 이 **0 이면 NULL** 을 준다. 그러면 그 칸은 건너뛴다 (`+0x38 JZ`) `[정적]`.
- 칸 `+0x20` 은 `PsLocateSystemDlls` 쪽에서 채운다 (2.2 ②).
- 그러니 세트를 먼저 만들어 칸을 채워 둬야, 주소록 적기가 **의미가 있다.** 코드 순서도 그렇다.
- 비유: 관리실 세트를 먼저 짜서 보관함에 넣고, 대장에 "몇 번지 기준" 을 적는다. 그다음에야 그 세트의 호실 이름표를 보고 벽 주소록을 적을 수 있다.

#### 덤: 파일 시스템 쪽 앞뒤

- `PsLocateSystemDlls` 는 `IoInitSystemPreDrivers` 안에서 `IopInitializeBootDrivers` (`+0xf3b`) **뒤**, `IopReassignSystemRoot` (`+0x1139`) **앞**이다 `[정적]`.
- 칸의 경로가 `\SystemRoot\…` 로 시작하니, 이 호출 전에 `\SystemRoot` 링크가 이미 있어야 한다 (run 문서).
  - `\SystemRoot` = Windows 폴더를 가리키는 커널 안 이름 `[추측]` (일반적으로 알려진 설명).
  - 그 링크를 만드는 곳은 아직 안 봤다. 후보 `Phase1InitializationDiscard +0xc31 CreateSystemRootLink` 는 이름으로 짐작한 것 `[추측]` → **Q95** (4장).

### 2.5 새 말: 시스템 DLL 표 `PspSystemDlls`

#### 뜻

- **`PspSystemDlls`** = 커널 전역에 있는 **포인터 6칸짜리 표** (rva `0xfd8130`) `[정적]`. 칸 하나가 시스템 DLL 하나의 **정보 카드** (0x40바이트) 를 가리킨다.
- 비유: 관리사무소 **세트 대장**. 줄이 6개 있고, 줄마다 "어느 세트인지, 설계도는 어디 있는지, 몇 번지 기준으로 놓았는지" 를 적는다.
  - 0번 줄 = 관리실 (ntdll) 세트.

#### 이 빌드의 처음 값 `[정적]` (PE 바이트)

| 표 칸 | 가리키는 카드 | 경로 (`+0x10`) | 짧은 이름 (`+0x30`) | 플래그 (`+0x8`) | 머신 (`+0xa`) |
|---|---|---|---|---|---|
| `[0]` | `PspNativeSystemDllData` | `\SystemRoot\System32\ntdll.dll` | `ntdll.dll` | `0x0013` | `0x8664` |
| `[1]` | `PspWowX86SystemDllData` | `\SystemRoot\SysWOW64\ntdll.dll` | `ntdll32.dll` | `0x0008` | `0x014c` |
| `[4]` | `PspVsmEnclaveRuntimeDllData` | `\SystemRoot\System32\vertdll.dll` | `vertdll.dll` | `0x0012` | `0x8664` |
| `[2]` `[3]` `[5]` | 0 | — | — | — | — |

- 그래서 이 빌드에선 `PspLocateSystemDll` 을 **세 번** 부르는 코드 길이 있다 `[정적]` (표 처음 값 기준). 실제로 셋 다 돌았는지는 D7·D9.
- `[2][3][5]` 는 이 빌드에서 안 바뀐다. 표에 쓰는 줄이 `PsLocateSystemDlls +0x8d` 하나뿐이라서다 `[정적]`. (레지스터+오프셋 쓰기는 `_refs` 가 못 잡으니 값은 D1.)

#### 새 말 둘: SysWOW64 ntdll, vertdll

- **SysWOW64 ntdll** = 32비트 프로그램용 ntdll. 머신 칸이 `0x014c` (32비트 x86) 다 `[정적]`. WOW64 자체는 [run10 해설 3.10](../02_sprint/run10-allocate-process-해설.md#310-ntdll-쪽-조금-더) 에서 "64비트 Windows 에서 32비트 프로그램을 돌리는 장치" `[추측]` 로 풀었다.
  - 비유: 관리실 세트의 **소형판**. 32비트 가게에만 갖다 놓는다.
- **vertdll** = 표 이름이 `PspVsmEnclaveRuntimeDllData` 인 칸의 DLL. 이름으로 보면 VBS 의 "엔클레이브" (따로 떼어 낸 보호 구역) 안에서 쓰는 런타임이다 `[추측]` (이름 + 일반적으로 알려진 설명). VBS 는 2.12 에서 푼다.
  - 비유: 관리사무소 안 **금고방** 전용 세트.
- 중요한 점: **부팅 때는 프로세스 종류 조건이 없다.** 칸이 있으면 다 찾는다. "이 가게엔 어느 세트를 붙이나" 는 프로세스 생성 때 `PsMapSystemDlls` 가 정한다 (2.13) `[정적]`.

#### 카드 한 장 (0x40바이트) 의 칸 `[정적]`

PDB 에 `_PS_SYSTEM_DLL_INFO` 라는 **이름만** 있고 칸 정의는 없다 (`pdb_struct.py` "없음"). 그래서 오프셋으로만 쓴다.

| 칸 | 뜻 | 누가 놓나 |
|---|---|---|
| `+0x0` | 섹션을 넣어 둘 "빠른 참조" 자리 (16바이트) 를 가리키는 포인터 | 파일 값 (`Ⓑ0`) |
| `+0x8` · `+0xa` | 플래그 · 머신 | 파일 값 (`Ⓑ0`) |
| `+0xc` | `SizeOfImage` | nt (`Ⓑ1`) |
| `+0x10` | 경로 `UNICODE_STRING` (버퍼 `+0x18`) | 파일 값 (`Ⓑ0`) |
| `+0x20` | **기본 주소** | nt (`Ⓑ1`) |
| `+0x28` | 뷰 시작 (System 프로세스 안) | nt (`Ⓑ1`) |
| `+0x30` | 짧은 이름 | 파일 값 (`Ⓑ0`) |
| `+0x38` | 재배치량 | nt (`Ⓑ1`) |

- 짐 지도 7.6.2 의 셈: 칸 9 = 파일 값 5 + nt 가 씀 4. 모르는 칸 0.
- 비유로 읽으면: 대장 한 줄 중 **"설계도 위치·종류·이름"** 은 대장 양식에 미리 인쇄돼 있고, **"몇 번지 기준·크기·얼마나 밀었나"** 는 문 여는 날 직원이 손으로 적는다.

#### 플래그 비트 (동작으로만 읽은 것, 이름 없음 `[추측]`)

이 구간 코드가 플래그를 보고 하는 일 (run 문서):

- `0x1` **필수** — 실패하면 버그체크 (2.8). 이 빌드에선 `[0]` ntdll 만 켜짐.
- `0x8` 올릴 주소 상한 풀기 (WOW 칸)
- `0x10` VSL 비트4 를 섹션 만들기에 넘김
- `0x20` **별칭 칸** — 파일을 새로 찾지 않고 0번 칸 섹션을 쓰는 칸. 이 빌드 표엔 없어서 그 갈래 (`PsLocateSystemDlls +0x84 / +0x89`) 는 **안 탄다** `[정적]`.
- `0x40` 보안 프로세스에게도 올릴 칸 (`PsMapSystemDlls` 가 봄)

#### 통째로 건너뛰는 경우

- `ExVerifySuite(6)` 이 참이고 `PsEmbeddedNTMask` 비트0 이 켜져 있으면 고리 전체를 건너뛴다 (`+0x1d ~ +0x25` → `+0x145`) `[정적]`.
- 6번 = EmbeddedNT 제품군으로 짐작 `[추측]`. 일반 Win11 Edu 는 아닐 것.

### 2.6 칸 하나 처리하기 (`PspLocateSystemDll`): 열고, 짜고, 손잡이는 놓고, 꾸러미는 쥔다

요약 표 7a ~ 7l 를 걸음으로 풀었다. 모두 `[정적]`.

1. `+0x7e` `ZwSystemDebugControl(0x26)` — 디버거 쪽 일. 반환값은 안 본다 → 2.14.
2. `+0xc5` `ZwOpenFile` — 칸 `+0x10` 경로로 DLL 파일을 연다. 실패하면 상태값을 그대로 돌려준다.
3. `+0xf4` `MmCreateSpecialImageSection` — **세트를 짠다** (이미지 섹션). exe 와 같은 사슬 → 2.7.
4. `+0x11c` `ObCloseHandle` — **파일** 손잡이를 놓는다.
5. `+0x146` `ObReferenceObjectByHandle` — 섹션 손잡이로 **꾸러미 (섹션 객체)** 를 직접 쥔다.
6. `+0x16e` `ObCloseHandle` — **섹션** 손잡이도 놓는다. 쥔 꾸러미는 그대로.
7. `+0x183` `MmGetSectionInformation(섹션, 2, …)` — 재배치량을 물어 **칸 `+0x38`** 에 적는다 (`+0x1aa`).
8. `+0x1b3` `ObInitializeFastReference` — 쥔 꾸러미를 칸 `+0x0` 이 가리키는 자리에 넣는다. 락 칸 `+0x8` = 0 (`+0x1b6`).
9. (필수 칸만) `+0x1d2` · `+0x1ea` · `+0x1fa` — 확인 셋 (2.8).
10. `+0x238` `PspMapSystemDll(…, 0, 1)` — 한 번 갖다 놓고 칸을 채운다 (2.9).

#### 새 말: 빠른 참조 자리

- 칸 `+0x0` 이 가리키는 곳은 16바이트 = `_EX_FAST_REF` 8 + 락 8 이다 (`PspNativeSystemDll` 등) `[정적]`.
- `ObInitializeFastReference` 는 꾸러미에 참조를 **15개 더** 얹고, 값 `객체 | 0xf` 를 그 자리에 쓴다 (결과서) `[정적]`.
- 일반적으로 알려진 설명 `[추측]`: 포인터 아래 몇 비트에 "미리 잡아 둔 참조 개수" 를 같이 적어 두는 칸이다. 여럿이 자주 집어 갈 때 매번 객체 카운터를 건드리지 않으려고 쓴다.
- 비유: 보관함에 꾸러미를 넣으면서 **번호표 15장**을 같이 뜯어 꽂아 둔다. 가게마다 세트를 빌려 갈 때 번호표 한 장만 뽑아 간다 (`PspReferenceSystemDll`), 다 쓰면 돌려준다 (`ObFastDereferenceObject`, 태그 `PsSd`).

#### 꾸러미는 영원히 산다

- 손잡이 둘 (파일·섹션) 은 **둘 다 닫는다**.
- 그런데 5번에서 쥔 꾸러미 참조를 돌려주는 줄이 이 함수에 **없다**. 그 참조가 8번에서 빠른 참조 자리로 들어간다.
- 그래서 → 섹션 객체는 영구히 산다 `[정적]` (run 문서).
- 비유: 창구에서 주문 꾸러미를 받자마자 손잡이는 떼어 버리고, 꾸러미 자체를 **관리사무소 보관함에 넣어 잠근다.** 부팅이 끝나도 꺼내 버리지 않는다.
- 짐 지도로 보면 손잡이 둘은 "열었다 닫음 → 남는 것 없음" 이다 (7.6.2). 남는 짐은 **꾸러미 하나**다.

### 2.7 세트 짜기: exe 와 같은 길, 다른 인자

#### 같은 길 `[정적]`

- `PspLocateSystemDll +0xef` 의 `MmCreateSpecialImageSection` 은 exe (`NtCreateUserProcess +0x937`, run31·run47) 와 **같은 함수 → 같은 안쪽 사슬**이다 (`MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap …`).
- 그러니 run31·run47 에서 본 서랍·배치표·꾸러미 이야기가 ntdll 세트에도 **그대로** 적용된다. 짐 지도도 "exe 묶음 칸 표를 그대로 쓸 수 있음 `[추측]`" 이라고 적었다 (7.6.2).

#### 다른 인자 `[정적]`

| 인자 | exe | 시스템 DLL |
|---|---|---|
| 섹션 객체 이름 | (run31) | 없음 (`+0xd8` 에서 `ObjectName` NULL) |
| 토큰 (3번째) | 프로세스 쪽 토큰 | **NULL** (`R8D=0`) |
| 서명 수준 (4번째) | 부모 쪽에서 구한 값 | **`0x0c`** (`+0xe4 MOV R9B,0xc`) |
| 플래그 비트0 | 켜짐 → 할당 속성 `SEC_IMAGE 0x01000000` | **꺼짐** → 할당 속성 `0x02400000` |

- 한 줄 읽기: 토큰 NULL. run31 해설 2.6 에서 이 창구는 "부모가 새 가게의 **신분증과 보안 등급을 대신 들고** 온다" 고 했다. 시스템 DLL 세트는 **가게가 아예 없을 때** 짜니 들고 올 신분증이 없다. 그래서 NULL 이다 (비유. 이유를 코드가 말해 주진 않는다).

#### 새 말: 서명 수준, 보호된 이미지

- **서명 수준** = 이 이미지의 서명이 얼마나 믿을 만한 쪽에서 왔어야 하는지를 나타내는 숫자 `[추측]` (일반적으로 알려진 설명). run31 해설에서 "보안 등급" 이라고 불렀던 그것이다.
  - 이번 값 `0x0c` 의 이름 `SE_SIGNING_LEVEL_WINDOWS` 는 SDK 기억이다 `[추측]`.
  - 비유: "**본사 직인**이 찍힌 설계도만 받는다."
- **보호된 이미지** = 할당 속성 `0x02400000` 이 결국 바뀌는 모양이다. `MiInitializeCreateSectionPacket +0x346 ~ +0x35a` 에서 **`SEC_IMAGE` + 패킷 플래그 `0x10` + 서명 수준 칸 (패킷 `+0x18`) `0x0c`** 로 바뀐다 `[정적]`.
  - 이름 `SEC_PROTECTED_IMAGE` · `SEC_NO_CHANGE` 는 SDK 기억 `[추측]`.
  - 비유: 세트 꾸러미에 "**수정 금지, 본사 직인 확인됨**" 도장을 같이 찍는다.
  - 이 도장이 서명 검사에 실제로 어떤 영향을 주는지 (`SeGetImageRequiredSigningLevel` 본체) 는 안 따라갔다 `[공백]`.
- 그 밖: 플래그 비트2 (`4`) 는 칸 `0x10` + VSL 비트4 일 때 → `MiCreateSection` 플래그 `8`. 비트1 (`2`) 은 켜지지만 `MmCreateSpecialImageSection` 이 안 읽는다 `[정적]`.
- 상태 `0xc0000054` 면 `KeDelayExecutionThread` 로 쉬고 다시 (`+0xdd ~ +0xea`). run31 에서 본 것과 같은 재시도다.

### 2.8 새 말: 버그체크 — 필수 세트가 없으면 문을 못 연다

#### 뜻

- **버그체크** = `KeBugCheckEx` 로 시스템을 **멈추는** 것. 블루스크린으로 알려져 있다 `[추측]` (run16 해설 2.9 와 같은 일반적 설명).
- 이 구간의 버그체크는 모두 **코드 `0x6b`**, 넷째 인자 0 이다 `[정적]`. 둘째 인자로 **어느 자리에서** 멈췄는지 번호 (run 문서 "사이트") 를 넘긴다.
- 비유: 문 여는 날 관리실 세트를 못 짜면, 관리사무소는 **문을 아예 안 연다.** 가게를 하나도 못 받으니까.

#### 사이트 표 (run 문서 그대로)

| 어디 (call / ret) | 사이트 | 조건 |
|---|---|---|
| `PsLocateSystemDlls +0xcf / +0xd4` | 2 (3번째 = 칸 번호) | `PspLocateSystemDll` 음수 (실질 `ZwOpenFile` 실패). **필수 칸만** — 이 빌드엔 0번 ntdll 만 |
| `PspLocateSystemDll +0x10b / +0x110` | 3 | 세트 짜기 실패. 모든 칸 |
| `+0x15d / +0x162` | 4 | 꾸러미 쥐기 실패. 모든 칸 |
| `+0x19a / +0x19f` | 8 | 재배치량 묻기 실패. 모든 칸 |
| `+0x211 / +0x216` | 9 | 필수 칸 확인 셋 중 하나라도 어긋남 |
| `+0x24f / +0x254` | 5 | `PspMapSystemDll` 실패. 모든 칸 |
| `PspInitializeSystemDlls +0xa0 / +0xa5` | 6 | 이름 찾기 실패 (`0xc000007a` = 이름 없음). 이름 표의 모든 이름 |

#### 읽는 법: "열기 전" 과 "연 뒤" 가 다르다

1. 파일을 **못 열면** (사이트 2): 필수 칸만 멈춘다. 필수 아닌 칸 (WOW ntdll · vertdll) 은 **조용히 빠진다.** 칸 `+0x20` 이 0 으로 남는다 `[정적]`.
2. 일단 **연 뒤에는**: 어느 칸이든 세트·꾸러미·올리기 실패가 부팅을 멈춘다 (사이트 3·4·8·5) `[정적]`.
3. 그래서 → "32비트용 파일이 아예 없는 Windows" 는 괜찮지만, "파일은 있는데 망가진" 경우는 문을 못 연다.
- 비유: 소형판 설계도가 서고에 **없으면** "소형판은 안 받습니다" 로 넘어간다. 그런데 설계도를 **꺼냈는데** 세트가 안 짜지면, 뭔가 크게 잘못된 거라 문 열기를 멈춘다.

#### 필수 칸 확인 셋 (사이트 9) — 확인만 한다

- 필수 칸 (플래그 `0x21` 중 하나, 곧 0번 ntdll) 일 때만 `[정적]`:
  - `MmGetSectionInformation(섹션, 4, …)` — 이미지 정보 + 부가 비트 `0x200` 확인
  - `MiSectionControlArea` — 세트의 서랍 (컨트롤 영역) `+0x38` 비트 `0x20` 확인
  - `MiDoesImageContainFunctionOverrideFixups` — 서랍의 로드 구성에 "함수 덮어쓰기 고침" 이 있나
- 결과를 어디에도 **안 쓴다.** 전역·표 칸에 남는 게 없다. 짐이 아니다 `[정적]`.
- 뜻: "필수 시스템 DLL 은 함수 덮어쓰기 (DVRT 쪽) 정보가 있는 이미지여야 한다" `[추측]`.
- 비유: 관리실 설계도에 **특정 부록 페이지**가 붙어 있나만 넘겨 보고 덮는다. 메모는 안 남긴다.

### 2.9 핵심: 같은 함수, 넷째 인자 하나 — "채우기" 와 "비교만"

이번 바퀴에서 가장 중요한 그림이다. 천천히 간다.

#### `PspMapSystemDll` 을 부르는 네 자리 `[정적]`

인자는 `.asm` 으로 셌다: `(프로세스, 칸, 인자3, 인자4)`. (`PsMapSystemDlls.c` 는 인자를 하나만 그린다 — `.c` 함정.)

| 부르는 자리 (call / ret) | 언제 | 프로세스 | 인자3 | 인자4 |
|---|---|---|---|---|
| `PspLocateSystemDll +0x233 / +0x238` | 부팅 (칸마다) | 현재 스레드의 프로세스 | 0 | **1** |
| `PsLocateSystemDlls +0x84 / +0x89` | 부팅 (별칭 칸만 — 이 빌드엔 없음) | 〃 | 0 | **1** |
| `PsMapSystemDlls +0xbb / +0xc0` | **프로세스마다** | 새 프로세스 | 0 (`MiMapProcessExecutable +0xd7` 에서) | **0** |
| `PspInitPhase3 +0x6b` · `+0xa0` | 부팅 (VBS 일 때만) | `PsSecureSystemProcess` | 0 | **0** |

#### 인자4 가 하는 일 `[정적]`

세트를 땅에 갖다 놓는 줄 (`+0x118 MmMapViewOfSectionEx`) 은 어느 쪽이든 **똑같이** 탄다. 다른 건 **놓은 뒤**다 (`+0x169` 에서 인자4 검사).

- **인자4 ≠ 0 (부팅)**: `+0x1a5` 로 가서 **칸을 채운다**. 돌려줄 값은 0.
  - 놓인 뷰에서 NT 헤더를 찾고 (`+0x1b3 RtlImageNtHeader`), 그 헤더의 `ImageBase` 를 칸 `+0x20` 에, `SizeOfImage` 를 `+0xc` 에, 뷰 시작을 `+0x28` 에 쓴다 (2.10).
- **인자4 = 0 (프로세스마다)**: `+0x16e ~ +0x176` 에서 **칸 `+0x20` 이 방금 놓인 주소와 같은지 비교**한다. 다르면 `0xc0000001` (`+0x178`).
- 덤: 인자4 ≠ 0 이면 놓을 때 할당 종류에 비트 `0x40000` 을 더한다 (`+0xbb BTS ECX,0x12`). MM 안에서 무슨 뜻인지는 안 따라갔다 `[공백]`.

#### 비유로

- 문 여는 날: 관리사무소가 관리실 세트를 **시험 삼아 한 번** 자기 땅 (System 프로세스) 에 놓아 본다. 놓인 자리를 보고 세트 대장에 "**이 세트는 ○○번지 기준**" 이라고 적는다.
- 가게가 생길 때마다: 같은 세트를 가게 땅에 놓는다. 그리고 대장을 펴서 "**○○번지에 놓였나?**" 만 확인한다. 아니면 거절.
- 대장에 적는 사람은 문 여는 날 한 명뿐이다. 가게마다 오는 직원은 **읽기만** 한다.

#### 왜 "같은 번지" 가 지켜지나 — 단계별로

1. `[정적]` 새 기본 주소를 고르는 건 **세트를 짤 때** MM 이다 (`MiCreateNewSection +0x3da → MiRelocateImage → MiSelectImageBase`, `ExGenRandom`).
2. `[정적]` 세트는 부팅 때 **한 번** 짠다 (2.6). 꾸러미는 보관함에서 영원히 산다.
3. `[정적]` 모든 가게가 **같은 꾸러미**를 빌려 가서 놓는다 (`PsMapSystemDlls → PspMapSystemDll +0x40 PspReferenceSystemDll`).
4. 그래서 → 모든 프로세스가 같은 주소를 받는다 `[정적]` (run 문서).
5. `+0x172` 비교는 그게 **정말** 지켜졌는지 확인하는 안전장치다.

### 2.10 기본 주소 `+0x20` 은 어디서 오나

#### 쓰는 줄 `[정적]`

- `PspMapSystemDll` 의 **떨어진 조각** (rva `0xb18c28 ~ 0xb18c3a`) 이 쓴다. 떨어진 조각 = 함수 본문에서 멀리 떨어진 곳에 있는 그 함수의 일부 (run31 의 `MiRelocateImage` 조각과 같은 모양).
  1. `+0x1a5` 놓인 주소를 잡는다.
  2. `+0x1b3 RtlImageNtHeader` 로 NT 헤더를 찾는다.
  3. `Magic` 이 PE32 (`0x10b`, WOW ntdll) 면 `[RAX+0x34]`, 아니면 `[RAX+0x30]` (`ImageBase`).
  4. **`+0x207054` `MOV [RBX+0x20],RDX`** (rva `0xb18c2c`) — 칸 `+0x20`
  5. `+0x20705b` `MOV [RBX+0xc],EAX` (rva `0xb18c33`) — 칸 `+0xc` (`SizeOfImage`)
  6. `+0x20705e` `MOV [RBX+0x28],R14` (rva `0xb18c36`) — 칸 `+0x28` (뷰 시작)
- WinDbg 가 이 자리를 `PspMapSystemDll+0x207…` 로 보일지 다른 이름으로 보일지는 확인 전이다 `[추측]` (rva 는 확실).

#### 그래서 `+0x20` 의 정체 — 근거 → 그래서 → 아직 확인 아님

1. `[정적]` `+0x20` = **System 프로세스에 놓은 뷰의 헤더 `ImageBase` 값**이다. 파일 속 `0x180000000` 을 그대로 읽는 줄이 **아니다.** 놓은 뒤 메모리의 헤더를 읽는다.
2. `[정적]` 새 주소는 그보다 앞, 세트 짤 때 MM 이 무작위로 골랐다 (2.9 의 1번).
3. `[정적]` 프로세스마다 `+0x172` 가 "`+0x20` == 놓인 주소" 를 요구한다.
4. `[관찰]` (run44) 첫 트랩 프레임 `Rdx` = ntdll 실제 베이스 `7ffafb660000` (파일 값 아님). 그리고 `Rdx` 는 `PspSystemDlls[0]+0x20` 에서 온다 (run16 `[정적]`).
5. 그래서 → `+0x20` = 부팅 때 고른 **실제 기본 주소**다.
6. 아직 확인 아님:
   - 뷰 헤더의 `ImageBase` 칸이 새 주소로 **바뀌는 MM 줄**은 안 따라갔다 `[공백]` → D5.
   - 칸 `+0x20` 을 실제로 읽은 값은 아직 없다 → D1 (Q42).
- 비유: 대장에 적는 번지는 설계도 표지의 "권장 번지" 가 아니다. 세트를 짤 때 이미 번지수를 고쳐 쓴 **실제 배치표**를 보고 적는다. 그 고쳐 쓰기 ([run31 해설 2.10 "번지수 고쳐 쓰기"](../02_sprint/run31-image-section-해설.md)) 가 어느 줄에서 표지까지 고치는지는 아직 못 봤다.

#### `+0x28` 과 `+0x20` 은 왜 따로 있나

- `+0x28` = **System 프로세스 안** 뷰 시작. `+0x20` = **가게들이 받을** 기본 주소.
- 코드에 둘이 다를 수 있다는 흔적이 있다 `[정적]`: `PspLookupEntryPoint` 는 `+0x28` 뷰에서 이름을 찾고, 결과에 `(+0x20) − (+0x28)` 을 더해 (`+0x2f ~ +0x3a`) 사용자 쪽 주소로 바꾼다.
  - 같다면 더하는 값은 0 이다. 실제로 같은지는 D1.
- run16 해설 2.9 의 4번 걸음 "정보 칸 `+0x18`·`+0x20` 차이" 가 바로 이것이다. `PsQuerySystemDllInfo` 가 돌려주는 포인터가 **칸 주소 + 8** 이라서 (결과서 2.2), 그 기준으로 적으면 오프셋이 8 씩 작게 보인다.
- 비유: 관리사무소 자기 땅에 놓은 세트에서 호실을 찾고, "가게 땅에선 ○○번지 기준" 으로 번지를 옮겨 적는다.

#### 그런데 정말 System 프로세스에 놓나

- **놓는 코드가 있다** `[정적]`: 프로세스 인자 = 현재 스레드의 프로세스 (`GS:[0x188]` → `+0xb8`), 이 갈래에 `MmUnmapViewOfSection` 은 없다.
- **System 프로세스** = 부팅 때 커널이 처음 만드는 자기 몫 프로세스 (PID 4) `[추측]` (일반적으로 알려진 설명). 비유: 관리사무소 **자기 땅**.
- `Phase1Initialization` 스레드가 그 프로세스 소속이라는 건 코드가 **가정**하는 것이다 (`+0x195` 에서 `PsInitialSystemProcess` 와 비교) `[추측]` → D3.

### 2.11 벽 주소록 `PsNtdllExports`: 19칸만 적는다

run16 해설 2.9 에서 "문 여는 날 벽 주소록을 적는다" 까지 봤다. 이번엔 **몇 칸을** 적는지가 칸 단위로 맞았다.

#### 이름 표 `[정적]` (PE 바이트)

- 이름 표 `NtdllExportInformation` (rva `0xc7ff90`) — 칸마다 `{표, 개수}`:
  - `[0]` ntdll **19개** → `PsNtdllExports`
  - `[1]` WOW **8개** → `PsWowX86SharedInformation` (rva `0xfda900`)
  - `[2~5]` **0개** (vertdll 도 0 → 이 구간에서 vertdll 함수 주소는 안 찾음)
- run16 의 "`NtdllExportInformation` 0번 = {`NtdllExports`, 19개}" 와 같다.

#### 21칸 중 둘이 빈다

- `PsNtdllExports` 는 `_PS_NTDLL_EXPORTS` (0xa8, PDB 21칸) 다.
- 이름 표 19개가 채우는 건 19칸. **표에 없는 둘** `+0x38 KiUserCallbackDispatcherReturn` · `+0x98 KiUserEmulationDispatcher` 은 이 구간이 **안 채운다** `[정적]`.
- `[관찰]` run44 K1 로그 (`ms/logs/04_baggage-read_K1.txt` 1531 ~ 1542줄 `dq nt!PsNtdllExports L0x15`): 21칸 중 **`+0x38`·`+0x98` 두 칸만 0**, 나머지 19칸은 `00007ffa…` 주소. 정적 계산과 **칸마다 맞는다** (run 문서에서 대조).
- 짐 지도 7.6.2 의 셈: 21칸 = nt 가 씀 (`Ⓑ2`) 19 + **아무도 안 씀** 2. 모르는 칸 0.
  - "아무도 안 씀" 은 run47 에 생긴 말이다 ([run47 해설 2.4](run47-section-chain-해설.md)): 쓰는 줄이 없고, 처음 0 그대로.
- 비유: 벽 주소록 양식엔 줄이 21개 인쇄돼 있는데, 문 여는 날 들고 온 **이름 목록엔 19개**만 있다. 나머지 두 줄은 끝까지 빈칸이다.

#### WOW 쪽

- `[1]` 칸 8개는 `PsWowX86SharedInformation` 으로 간다. 그리고 `+0x30` 은 이름 표가 아니라 `+0x77 PspWow64GetSharedInformation` 뒤 `+0x80` 에서 WOW 칸 `+0x20` 을 복사한다 `[정적]`.
- WOW 칸을 못 찾았으면 (`+0x20 = 0`) 이것도 안 채운다 (2.4) `[정적]`.

### 2.12 뒤쪽 둘: `MmInitSystemDll`, 그리고 VBS 일 때만 도는 `PspInitPhase3`

#### `MmInitSystemDll` `[정적]`

- `Phase1InitializationIoReady +0x1e2` (ret `+0x1e7`) `MmInitSystemDll` 의 `+0x22` 가 `MiState.Processes.SystemDllBase` (rva `0xe2db80` = `MiState+0x1000`) 에 **0번 칸 `+0x20`** 을 복사한다.
  - PDB 이름 `_MI_PROCESS_STATE +0x0` `[정적] PDB`.
- 읽는 곳은 `MmGetSectionInformation` 하나만 봤다. 비유: 대장의 관리실 번지를 **메모리 담당 부서 칠판에도 한 번 베껴 적는다.**

#### 새 말: VBS

- **VBS** = 가상화 기반 보안 (Virtualization-Based Security) `[추측]` (일반적으로 알려진 설명). 하이퍼바이저 힘을 빌려, 보통 커널도 못 건드리는 **따로 떼어 낸 보호 구역**을 만드는 기능이다.
  - 이번 코드에서 보이는 스위치는 `VslVsmEnabled` 다 `[정적]`. 이름 앞의 `Vsl` 도 이쪽이다 (`VslGetNestedPageProtectionFlags` 등).
- 비유: 관리사무소 안에 **금고방**을 하나 더 둔다. 금고방엔 금고방 전용 직원 (`PsSecureSystemProcess`) 이 있다.

#### `PspInitPhase3` `[정적]`

- `VslVsmEnabled` 일 때**만**: `PsSecureSystemProcess` 에 0번 (ntdll) · 4번 (vertdll) 칸을 놓는다 (`+0x6b/+0x70`, `+0xa0/+0xa5`, 인자3·4 = 0·0). 그 뒤 `VslpEnterIumSecureMode` (`+0xea`).
- 인자4 가 **0** 이다. 즉 금고방에 놓을 때도 칸을 다시 채우지 않고 **비교만** 한다 (2.9). 대장은 이미 적혀 있으니까.
- 본문은 범위 밖. 이 VM 에서 VBS 가 켜져 있는지는 D8 (`dd nt!VslVsmEnabled L1`).

### 2.13 짝 맞추기: 부팅 때 쓴 것 ↔ 가게마다 읽는 자리

run 문서 "프로세스 생성 때 이 짐을 읽는 자리" 표에서 대표만.

| 부팅 때 쓴 것 | 프로세스 생성 (①) 때 읽는 자리 |
|---|---|
| 섹션 객체 (칸 `+0x0`) | `MiMapProcessExecutable +0xdc → PsMapSystemDlls +0xc0 → PspMapSystemDll +0x40 PspReferenceSystemDll` → `+0x11d MmMapViewOfSectionEx` (인자4 = 0) |
| 칸 `+0x20` (기본 주소) | `PspMapSystemDll +0x172` (비교), `PspInitializeThunkContext +0x196 · +0x19d` (새 스레드 `Rdx`) |
| 칸 `+0x38` (재배치량) | `PspPrepareSystemDllInitBlock +0xa5/+0xac` (→ init 블록 `+0x10`), `+0xcb ~ +0xd6` (WOW → `+0x8`) |
| 칸 `+0x8` 플래그 (파일 값) | `PsMapSystemDlls +0x12a`, `PspMapSystemDll +0x68` |
| `PsNtdllExports` | `PspInitializeThunkContext +0x1a1 · +0x37f` (`+0x8` → 새 스레드 `Rip`), `PspPrepareSystemDllInitBlock +0x214` (`+0x0`), `NtCreateUserProcess +0xd10` (`+0x10` `RtlUserThreadStart`) 등 — 읽기 약 40줄 |

#### 한 줄 읽기: `PsNtdllExports` 줄

- 문 여는 날 `PspInitializeSystemDlls +0x6b` 가 적은 벽 주소록을, 가게가 생길 때마다 세 사람이 읽는다:
  - `PspInitializeThunkContext` — "첫 출근 장소 = `LdrInitializeThunk`" (run16)
  - `PspPrepareSystemDllInitBlock` — "업무 수첩 위치 = `LdrSystemDllInitBlock`" (run32)
  - `NtCreateUserProcess` — "본업 자리 = `RtlUserThreadStart`" (run16)
- 이 셋은 모두 이미 아는 ① 짐이다. 이번에 **값이 어디서 왔는지** 가 이어졌다.

#### 어떤 가게에 어느 세트를 붙이나 `[정적]`

- 2.5 에서 "부팅 때는 칸이 있으면 다 찾는다" 고 했다. 고르는 건 여기다 (`PsMapSystemDlls +0x7c ~ +0xa4`, `+0x12a`):
  - 0번 (ntdll) — **늘**
  - WOW 프로세스 (`EPROCESS +0x310` ≠ 0) 면 `PsWow64GetProcessNtdllType` 번호 칸
  - 보안 프로세스 (`+0x170` 비트0) 면 플래그 `0x40` 칸
- `PsMapSystemDlls` 자체도 `MiMapProcessExecutable` 플래그 `0x10` 이 꺼졌을 때만 불린다 (`+0xce ~ +0xd0`).
- test.exe 가 둘 다 아니라서 0번만 붙는다는 건 그대로 `[추측]` (flow.md 2.1).

#### 사용자 짐작 판정

- **맞다** `[정적]`. 섹션 · 기본 주소 · `PsNtdllExports` 셋 다 부팅 `Ⓑ` 에서 **한 번** 쓰고, 프로세스마다는 **읽기 · 비교 · 올리기**뿐이다.
- 단서 하나 (run 문서): 프로세스마다 **읽은 값으로 새로 만드는 것**은 있다. init 블록 (업무 수첩, run32), 새 스레드 문맥 (run16). 이건 이미 다룬 ① 짐이다.
- 비유: 대장과 벽 주소록은 문 여는 날 한 번 적고 끝. 가게마다는 그걸 **보고 베껴서** 가게 안 수첩과 출근 메모를 새로 쓴다.

### 2.14 디버거가 붙어서 달라지는 점: `ZwSystemDebugControl(0x26)`

CLAUDE.md "디버거가 붙어서 달라지는 점이 보이면 따로 적는다" 에 해당한다.

#### 새 말: 명령 `0x26` — 커널 디버거에서 파일 끌어오기

- `PspLocateSystemDll +0x79` (ret `+0x7e`) 가 `ZwSystemDebugControl` 을 **명령 `0x26`** 으로 부른다. 입력 = 칸의 경로 16바이트 `[정적]`.
- 명령 `0x26` 은 "커널 디버거 쪽에서 원격 파일 끌어오기" 다. 함수 이름 (`ExpKdPullRemoteFileForUser`, `KdPullRemoteFile`) 과 분기 사슬로 읽은 것이다 `[정적]`. 번호 이름 표는 안 봤다.
- 비유: 문 여는 날, 설계도를 꺼내기 직전에 **본사 (디버거 PC) 에 전화**를 건다. "이 설계도 새 판 있으면 보내 주세요." 전화선이 없으면 바로 끊긴다.

#### 늘 부른다 — 임시 비트 때문에 `[정적]`

1. 이 호출은 `NtGlobalFlag & 0x40000` 일 때만 탄다 (`+0x49`).
2. 그런데 `IoInitSystemPreDrivers` 가 `PsLocateSystemDlls` 를 부르는 **동안만** 그 비트를 켜 둔다 (`+0x10ef` 켬, `+0x10fa` 되돌림) (2.2 ①).
3. 그래서 → 칸마다 **늘** 부른다.
- `NtGlobalFlag` = 커널이 들고 있는 전역 디버그 설정 비트 묶음 `[추측]` (일반적으로 알려진 설명). 비유: 관리사무소 **설정 스위치판**. 이 구간 동안만 스위치 하나를 올렸다 내린다.
- ntdll 쪽에도 같은 비트로 `NtSystemDebugControl` 을 타는 갈래가 있었다 ([run26 해설](../02_sprint/run26-map-dll-file-해설.md) 2.7, Q44). 같은 모양이 커널 부팅에도 있다는 것이다.

#### 두 부팅이 갈리는 곳 `[정적]`

| | 커널 디버깅을 **안** 켠 부팅 | 커널 디버깅을 **켠** 부팅 (우리 타깃 VM — KDNET) |
|---|---|---|
| 문턱 | `KdPitchDebugger` 켜짐, `KdLocalDebugEnabled` 꺼짐 → `NtSystemDebugControl` 앞에서 `0xc0000354` 로 끝 | 디버그 권한 검사 면제 (`+0xc8 CMP EBX,0x26`), 입력 `0x10`·출력 `0` → `ExpKdPullRemoteFileForUser` |
| 그 뒤 | 남는 것 없음 | 경로를 소문자로 → SHA-256 해시 → "이미 끌어온 파일 표" `ExpSysDbgPulledFileTable` 에 없으면 `KdPullRemoteFile`. 표가 새로 생길 수 있다 (풀 `0x4010` 바이트) |

- 호스트가 실제로 파일을 넘겨주는지는 연결 상태에 달렸다 `[공백]`.
- `PspLocateSystemDll` 은 **반환값을 안 본다** (`+0x7e` 다음 `+0x81` 로 그냥 이어짐) `[정적]`.
- 그래서 → 두 부팅의 차이는 `ExpSysDbgPulledFileTable` 같은 **디버거 쪽 상태뿐**이고, 대장 칸 값에는 영향이 없을 것 `[추측]`. (`KdPullRemoteFile` 이 성공해 다른 파일을 열게 되는 길이 있는지는 안 봤다.)
- 짐 지도는 `ExpSysDbgPulledFileTable` 을 "**커널 디버깅 켠 부팅만**" 줄로 따로 둔다 (7.6.2). run 문서: 에뮬레이터가 디버거 없는 부팅을 흉내 내면 이 짐은 없다.

### 2.15 정정 둘

#### run16 — "칸 `+0x20`·`+0x28` 은 `PspLocateSystemDll +0x238` / `PsLocateSystemDlls +0x89` 에서 채운다"

- 반만 맞다 (run 문서).
  - `PsLocateSystemDlls +0x89` 는 **별칭 칸 (플래그 `0x20`) 전용** 갈래의 ret 자리다. 이 빌드 표엔 그런 칸이 없어 **안 탄다** `[정적]`.
  - 이 빌드에서 칸을 채우는 길은 `PspLocateSystemDll +0x238` **하나**다.
  - 같은 줄들이 `+0xc` (`SizeOfImage`) 도 같이 채운다.
- run16 문서 맨 위에 정정 한 줄이 들어갔다. flow.md 는 이 스프린트 문서라 바로 고쳤다.
- [run16 해설 2.9](../02_sprint/run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다) 의 "`Rdx` 로 넘긴 ntdll 칸도 부팅 때 정해진다" 첫 줄에도 같은 두 자리가 적혀 있다. 읽을 때 **`PspLocateSystemDll +0x238` 하나**로 읽으면 된다.

#### run10 — "`PspMapSystemDll` 입구는 부팅 때 System 프로세스에도 걸린다 (caller 에 `PspInitPhase3`)"

- **이유가 틀렸다** (run 문서).
  - 부팅 때 System 프로세스 (현재 프로세스) 에 놓는 caller 는 `PspLocateSystemDll +0x233` (와 안 타는 별칭 갈래 `PsLocateSystemDlls +0x84`) 다.
  - `PspInitPhase3` 는 VBS 일 때만, **`PsSecureSystemProcess`** 에 놓는다 `[정적]`.
- 결론 ("새 프로세스만 보려면 `PsMapSystemDlls` 쪽이 낫다") 은 그대로다.
- 덤: 이 부팅 호출들은 모두 로그인 전이라, 보통 KDNET 세션에서는 **이미 지나간 뒤**다 `[추측]` (시점으로 짐작).
- run10 문서 맨 위에 정정 한 줄이 들어갔다. [run10 해설 3.10](../02_sprint/run10-allocate-process-해설.md#310-ntdll-쪽-조금-더) 의 "멈춤점 팁" 도 같은 말이라, 괄호 안 이유만 위처럼 바꿔 읽으면 된다.

#### 보탬 (틀린 말은 아님)

- flow.md 2.1 의 `[추측]` 두 줄이 `[정적]` 이 됐다 (3장).
- `PsMapSystemDlls` 나머지 칸 조건에 보안 프로세스 (`+0x170` 비트0 + 칸 플래그 `0x40`) 가 더해졌다.

### 2.16 이것이 에뮬레이터에 왜 중요한가 (내 짐작)

여기서부터는 run 문서가 아니라 **내 짐작**이다. 그래서 전부 `[추측]` 이고, 이유를 같이 쓴다.

#### 에뮬레이터에는 "문 여는 날" 이 없다

- `[추측]` 이유:
  1. CLAUDE.md: 에뮬레이터 (`../PE_emulator`) 는 진짜 nt·ntdll 과 **같은 이름·호출 관계**로 짓고, 흐름도 범위는 "**프로세스 생성 → 진입점**" 이다.
  2. 이번 사슬은 그 범위 **앞 (부팅 `Ⓑ`)** 이다 (run 문서).
  3. 그런데 ① 은 이 짐을 **읽기만** 한다 (2.13). 쓰는 줄이 ① 에 없다.
  4. 그래서 → 에뮬레이터가 ① 부터 시작한다면, `Ⓑ` 짐은 **누가 미리 만들어 둬야** 한다. 안 그러면 ① 의 `PsMapSystemDlls` 가 잡을 꾸러미도, `PspInitializeThunkContext` 가 읽을 벽 주소록도 없다.
- 짐 지도 말로: 이 짐들은 ① 에서 보면 `입력` 이다 ("쓰는 말"). 에뮬레이터 쪽에선 "구현할 함수가 있는 짐" 이 아니라 "**시작 전에 넣어 줄 입력**" 목록에 먼저 들어갈 것 같다. (run47 해설 2.12 의 `SectionObjectPointer` 와 같은 처지다.)
- 비유: 모형 사무소 (에뮬레이터) 는 **이미 문을 연 상태**로 시작한다. 그러니 문 여는 날 적었어야 할 대장과 벽 주소록을 **미리 적어 둔 채로** 들여놔야 한다.

#### 미리 적어 둬야 할 것으로 보이는 것 (근거 줄)

| 짐 | 왜 필요해 보이나 (`[추측]`) | 근거 |
|---|---|---|
| ntdll 세트 꾸러미 (칸 `+0x0` 자리) | ① 이 `PspReferenceSystemDll` 로 빌려 간다 | 2.13 `[정적]` |
| 칸 `+0x20` (기본 주소) | ① 의 `+0x172` 비교와 새 스레드 `Rdx` 가 읽는다 | 2.9 · 2.13 `[정적]` |
| 칸 `+0x38` (재배치량) | ① 의 init 블록 (업무 수첩) 이 읽는다 | 2.13 `[정적]` |
| `PsNtdllExports` 19칸 | ① 의 새 스레드 `Rip`·업무 수첩 위치 등이 읽는다 | 2.11 · 2.13 `[정적]` |
| `MiState.Processes.SystemDllBase` | `MmGetSectionInformation` 이 읽는다 (그 밖은 안 봄) | 2.12 `[정적]` |

#### 맞춰 둬야 할 것: 번지 셋이 한 몸이다

- `[추측]` 이유: 아래 셋은 서로 묶여 있다 (모두 `[정적]` 근거).
  - 칸 `+0x20` 과 실제로 놓인 주소가 다르면 ① 이 `0xc0000001` 로 거절한다 (`+0x172`).
  - `PsNtdllExports` 값은 `+0x20` 기준 사용자 주소다 (`(+0x20) − (+0x28)` 보정, 2.10).
  - ntdll 의 `LdrpSystemDllBase` 는 새 스레드 `Rdx` = 칸 `+0x20` 에서 온다 (짐 지도 3.8 출처).
- 그래서 → 에뮬레이터가 ntdll 을 몇 번지에 놓든 **세 곳에 같은 번지**가 들어가야 앞뒤가 맞을 것 같다. 진짜 Windows 는 그 번지를 부팅 때 무작위로 고르니 (2.9), 에뮬레이터는 아무 번지나 하나 정해도 될 것 같다. 다만 고른 번지 하나로 셋을 같이 채워야 한다.

#### 없어도 될 것으로 보이는 것

- `[추측]` 이유: 남는 게 없거나, 디버거 쪽 짐이다.
  - 파일 · 섹션 **손잡이**: 열었다 닫는다 (2.6, 7.6.2 "남는 것 없음").
  - `NtGlobalFlag` 임시 비트: 켰다 되돌린다 (2.14).
  - `ExpSysDbgPulledFileTable`: 커널 디버깅 켠 부팅만 (run 문서도 "에뮬레이터는 디버거가 없는 부팅을 흉내 내면 이 짐이 없다").
  - 필수 칸 확인 셋: 결과를 안 쓴다 (2.8).

#### 아직 정할 수 없는 것

- System 프로세스 안 ntdll 뷰 (`+0x28`): 진짜 Windows 에선 벽 주소록을 적을 때 이 뷰에서 이름을 찾는다 (2.10). 에뮬레이터가 이걸 흉내 낼지, 주소록을 바로 적어 넣을지는 모른다. 뷰가 정말 있는지도 아직 D3 다.
- 어디까지 흉내 낼지는 **목록이 다 된 뒤 사용자와 정한다** (CLAUDE.md "지금 목표"). 여기선 "이런 짐이 ① 앞에 있다" 를 **표시만** 한 것이다.

### 2.17 정리: 비유로 한 번 더

1. 관리사무소가 **문 여는 날** (부팅 `Phase1`), 드라이버를 들이기 전에 **세트 대장** (`PspSystemDlls`) 을 편다. 대장 양식엔 줄 셋 (ntdll · 소형판 · 금고방용) 이 미리 인쇄돼 있다 (`Ⓑ0`).
2. 줄마다 (`PspLocateSystemDll`):
   - 본사에 "새 판 있어요?" 전화를 한 번 건다 (`ZwSystemDebugControl`). 디버거가 있는 날만 실제로 통화가 된다.
   - 서고에서 설계도를 꺼내 (`ZwOpenFile`), exe 때와 **같은 창구**에서 세트를 짠다 (`MmCreateSpecialImageSection`). 다만 신분증은 없고, "본사 직인·수정 금지" 도장을 찍는다.
   - 손잡이는 다 떼고, 꾸러미는 **보관함에 넣어 잠근다** (번호표 15장과 같이).
   - 세트를 자기 땅에 한 번 놓아 보고, **"○○번지 기준"** 을 대장에 적는다 (`PspMapSystemDll`, 인자4 = 1).
   - 관리실 세트를 못 짜면 문을 아예 안 연다 (버그체크).
3. 그다음 관리실 세트의 호실 이름표를 보고 **벽 주소록** 19줄을 적는다 (`PspInitializeSystemDlls`). 양식 21줄 중 두 줄은 빈칸으로 남는다.
4. 메모리 부서 칠판에도 번지를 베껴 적는다 (`MmInitSystemDll`). 금고방이 있으면 금고방에도 세트를 놓는다 (`PspInitPhase3`).
5. 문을 연 뒤 가게가 생길 때마다 (`PsMapSystemDlls`), 보관함의 **같은 꾸러미**를 빌려 가게 땅에 놓고, 대장을 보며 **"○○번지에 놓였나?"** 만 확인한다 (인자4 = 0). 벽 주소록은 읽기만 한다.

### 2.18 곁가지: 도구 메모

- 이번에 칸 처음 값 (`PspSystemDlls` 표, 경로 문자열, 이름 표) 은 결과서가 **PE 파일 바이트**를 임시 파이썬으로 읽었다. PDB 공개 이름으로 주소를 이름에 맞췄다.
- 결과서 제안 둘 (하네스라 오케스트레이터가 사용자에게 보임):
  - `pe_data.py <바이너리> <이름|0xRVA> <바이트수>` — 짐 지도 `입력 (파일 값)` 칸을 닫을 때마다 필요하다.
  - `pdb_struct.py --list` 가 "이름만 있고 칸 정의가 없는 타입" (`_PS_SYSTEM_DLL_INFO`) 을 따로 표시.
- `.c` 함정: `PsMapSystemDlls.c` 는 `PspMapSystemDll` 인자를 하나만 그린다 (`.asm` 은 넷). `MiInitializeCreateSectionPacket.c` 는 `0x02000000` 갈래 흐름이 틀어진다. 인자·갈래는 `.asm` 으로 셀 것.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

그림은 안 바뀌었다 (부팅은 흐름도 범위 앞).

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb 쓴 run 목록에 run48 없음 | run48 추가. "(run48) 정적 바퀴, `PsNtdllExports` 값 대조에 K1 로그 1531 ~ 1542줄 다시 읽음" 한 줄, 정적 근거에 run48 ("**흐름도 범위 앞 (부팅)**") |
| 2.1 절 제목 | … run34, run47 | … run47, **run48 부팅 메모** |
| 2.1 `PsMapSystemDlls` 절 — 나머지 칸 조건 | "WOW64 조건이 붙는다" 만 | `[정적]` 조건 둘: WOW 프로세스 (`+0x310`) · 보안 프로세스 (`+0x170` 비트0 + 플래그 `0x40`) |
| 〃 — 다른 caller 3곳 | `[추측]` 부팅 때 ntdll 섹션 준비 쪽 | **`[정적]`** 셋 다 부팅 쪽: `+0x238` (칸마다) · `+0x89` (별칭, 이 빌드엔 없음) · `PspInitPhase3` (VBS 만) |
| 〃 — "섹션은 부팅 때 한 번, 프로세스마다 붙이기만" | `[추측]` (run16 "모양은 맞다") | **`[정적]`** (run48). run16 의 `+0x89` 는 정정 |
| 〃 — 새 줄 | — | **부팅 때 준비** 한 줄: `Phase1Initialization +0x3b … +0x10fa PsLocateSystemDlls → +0xa3 PspLocateSystemDll → … +0x238 PspMapSystemDll(…, 0, 1)`, 뒤에 `PspInitializeSystemDlls +0x6b` |
| 2.3 끝 절 ("`LdrInitializeThunk` 주소를 아는 법") | `[공백]` 부팅 안 앞뒤 순서 | **(해결됨)** `[정적]` `PsLocateSystemDlls` 먼저 → `PspInitPhase2` 나중. 그리고 `+0x38`·`+0x98` 안 채움 `[정적]` + `[관찰]` run44 K1 |
| 4장 | 부팅 순서 `[공백]` 줄 | 그 줄 **(해결됨)**. 새 줄: 부팅 준비 짐 실제 값 → Q42 (D1·D2) · Q94 (D3 ~ D12), 정적 `[공백]` (뷰 헤더 `ImageBase` 고치는 줄, `0x40000` 뜻, `\SystemRoot` 링크) |
| 5장 | — | run48 줄 둘 |

태그 변화 대표:

- "ntdll 섹션은 부팅 때 한 번 만들고, 프로세스마다 같은 섹션을 붙이기만" — run10 `[추측]` → run16 "모양은 맞다" → 이번에 **`[정적]`**.
- 부팅 안 순서 — `[공백]` → **`[정적]`**.
- `PsNtdllExports` 두 빈칸 — 처음 다룸 → **`[정적]` + `[관찰]`** (run44 로그와 칸마다 맞음).

### 짐 지도 `baggage.md` 에서 바뀐 것 (여섯째판)

- **"쓰는 말"** 에 단계 `Ⓑ` (2.3). `입력` 설명에 "부팅에서 온 시스템 DLL 짐은 누가 놓았는지가 7.6.2 에" 한 줄.
- **새 절 7.6.2** — `PspSystemDlls` 표 · 카드 칸 9 · 부팅 때 놓인 커널 전역 (섹션 객체, System 프로세스 뷰, `NtGlobalFlag` 임시 비트, `ExpSysDbgPulledFileTable` (디버깅 부팅만), `PsNtdllExports`, `PsWowX86SharedInformation`, `MiState.Processes.SystemDllBase`, `PspInitPhase3` 뷰). 경로 줄임말 `Ⓑ…` · `ⒷL…` · `Ⓑ2…` 이 생겼다.
- **2장 숫자는 그대로**다 (run47 숫자). 7.6.2 안에서만 센다: 카드 칸 9 = 파일 값 5 + nt 4, `PsNtdllExports` 21 = nt 19 + 아무도 안 씀 2.
- **3.8 · 3.15 · 4장**: ① 짐 중 값이 이번 짐에서 오는 칸 (트랩 프레임 `Rip`·`Rdx`, ntdll `LdrpSystemDllBase`, `LdrSystemDllInitBlock`) 은 쓴 주인을 그대로 두고, 근거 칸 끝에 **"출처 `Ⓑ…`"** 를 달았다.
  - 4장 "ntdll 함수 주소" 줄의 "같을 것 `[추측]`" 은 `+0x172` (같은 주소 강제) 와 합쳐 `[정적]` 이 됐다.
- **5장**: `Ⓑ` 순서 (`Ⓑ0` → `Ⓑ1` → `Ⓑ2` → `Ⓑ3` → `Ⓑ4`).
- **6장 공용 부품**: 세 줄 (섹션 사슬은 exe·시스템 DLL 이 같음, 빠른 참조 셋, `RtlFindExportedRoutineByName`·`RtlImageNtHeader`).
- **7.11**: PDB 에 칸 정의가 없는 타입에 `_PS_SYSTEM_DLL_INFO` 추가.

### 큰 그림에서 어떻게 이어지나

- **짐 지도의 `입력` 한쪽이 처음으로 "출처" 를 얻었다.** 지금까지 `입력` 은 "밖에서 온 값" 이라고만 적혀 있었다. 이제 그중 시스템 DLL 짐은 **부팅의 어느 함수, 어느 줄**이 놓았는지 있다.
- **run9 → run31 → run47 의 섹션 이야기가 ntdll 까지 넓어졌다.** exe 세트와 관리실 세트는 같은 창구에서 짠다. 다른 건 **언제 (가게마다 / 문 여는 날 한 번)** 와 도장 (인자) 뿐이다.
- **Q42 와 바로 이어진다.** 부팅 뒤 보통 KDNET 세션에서 `dq` 로 칸만 읽으면 2.10 의 `+0x20` 이야기를 `[관찰]` 로 닫을 수 있다. 결과서 추천: `nt!PspMapSystemDll+0x172` 에서 `dq @rbx L8` 과 `rax` 를 같이 보면, test.exe 한 번에 "칸 `+0x20` == 놓인 주소" 를 본다.
- 다음 항목은 **Q78c (프로세스 객체)** 다. 원래 줄로 돌아간다.

---

## 4. 아직 모르는 것

부팅 때 도는 함수라 **로그인 뒤 `bp` 는 이미 지나간 뒤**다. 그래서 동적 질문을 둘로 갈랐다 (결과서). 멈춤점·조건은 dast-mentor 몫.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **D1** `PspSystemDlls` 실제 칸 값: `[0][1][4]` 만 0 이 아닌가, `+0x20` = `+0x28` 인가, `+0x20` = 사용자 프로세스 `lm m ntdll` 인가, `+0x20 − 0x180000000` = `+0x38` 인가, `+0xc` = `0x267000` / `0x1bf000` / `0x36000` 인가 | 정적은 "쓰는 줄" 까지. 칸 자체는 run44 에서 안 읽었다 (실마리: `lm m ntdll` 크기 `0x267000`) | 동적 **Q42** (부팅 뒤 `dq`). 또는 `nt!PspMapSystemDll+0x172` 에서 `dq @rbx L8`·`rax` |
| **D2** `PsWowX86SharedInformation` (`+0x30` = WOW ntdll 기본 주소) | `PsNtdllExports` 는 run44 로그에 이미 있지만 WOW 쪽은 안 읽음 | 동적 **Q42** (`dq nt!PsWowX86SharedInformation L9`) |
| **D3** System 프로세스 (PID 4) 에 정말 ntdll 뷰가 있나, 시작 = `+0x28` 인가, 소형판·금고방 뷰도 있나 | "현재 프로세스 = System" 은 코드의 가정 `[추측]` | 동적 **Q94** (`!process 4 0` → `.process /p /r` → `lm m ntdll` / `!vad`) |
| **D4** `MiState+0x1000` = `poi(PspSystemDlls)+0x20` 인가 | 값 | 동적 **Q94** |
| **D5** 뷰 헤더 `ImageBase` 가 새 주소로 바뀌어 있나 | 고치는 MM 줄을 정적으로 못 따라감 `[공백]` | 동적 **Q94** (D3 문맥에서 `!dh`) |
| **D6** `NtGlobalFlag` 비트 `0x40000` 이 되돌려졌나, `ExpSysDbgPulledFileTable` 이 생겼나 (**디버거가 붙어서 달라지는 점**) | 값. 표가 0 이어도 두 경우를 못 가린다 `[공백]` | 동적 **Q94** |
| **D7** `PspLocateSystemDll` 이 실제로 몇 칸 돌았나 | 표 처음 값 기준 "세 번 부르는 길이 있다" 까지 | 동적 **Q94** (D1 의 `[1]`·`[4]` `+0x20`·`+0x28` 로) |
| **D8** `VslVsmEnabled` — `Ⓑ4` 를 탔나 | 값 | 동적 **Q94** (`dd nt!VslVsmEnabled L1`) |
| (낮은 우선순위, **부팅 디버깅** 필요) D9 호출 순서·인자, D10 `MmMapViewOfSectionEx` 반환 (`0` / `0x40000003`), D11 `ZwSystemDebugControl` 반환·임시 비트, D12 세트 짜기 실제 인자 | 반환값·인자는 그 순간에만 보인다. VM 을 다시 켜며 첫 접속에서 멈춘 뒤 `bp` 를 걸어야 한다 | 동적 **Q94** |
| `\SystemRoot` 링크를 만드는 곳 | 후보 `Phase1InitializationDiscard +0xc31 CreateSystemRootLink` 는 이름 짐작 `[추측]`, 본문 안 읽음 | 정적 **Q95** (작음) |
| MM 안 뜻들: 할당 종류 `0x40000`·`0x20000000`, `MiGetControlAreaLoadConfig` 칸 이름 (사이트 9), 서명 검사 본체 (`SeGetImageRequiredSigningLevel`) | 줄기 밖 MM 내부 `[공백]` | 결과서 4장 (대기열 줄은 따로 없음) |
| `PsInitSystem(1)` 인자가 정말 1 인가, `PspInitPhase3` VBS 본문, `PsWowX86SharedInformation` 을 읽는 자리 | 범위 밖 / 이름만 | 결과서 4장 |

- 대기열 순서 (plan): **Q78c** → Q78d → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q42·Q94 는 동적 차례에, Q95 는 작은 정적 차례에.

---

## 5. 더 읽기

- run 문서: [`run48-system-dll-boot.md`](run48-system-dll-boot.md) — 부팅 준비 호출 사슬 표, 호출 관계 트리, `PspSystemDlls` 표와 칸, 섹션 인자 비교, `PspMapSystemDll` 네 caller, 기본 주소 출처, `PsNtdllExports`, 버그체크 표, 읽는 자리 짝, 디버거 영향, 정정, 아직 확인 필요 (D1 ~ D12), 도구 메모
- 결과서: [`_workspace/sast/42_system-dll-boot_result.md`](../../_workspace/sast/42_system-dll-boot_result.md) — 0장 맨 앞 요약, 1장 한눈 결론, 2.1 ~ 2.5 질문별 답 (단계 `Ⓑ` 의견, baggage 양식 표), 3장 동적 질문 (A·B 갈래), 4장 `[공백]` 9건, 5장 `.c` 주의
- 요청서: [`_workspace/sast/42_system-dll-boot.md`](../../_workspace/sast/42_system-dll-boot.md)
- 짐 지도: [`baggage.md`](baggage.md) — "쓰는 말" (`Ⓑ`, `입력`), 7.6.2, 5장 `Ⓑ` 순서, 4장 짝 줄 "출처 `Ⓑ…`", 6장, 7.11
- 흐름도: [`flow.md`](flow.md) — 2.1 `nt!PsMapSystemDlls` → `nt!PspMapSystemDll` 절, 2.3 "커널이 `LdrInitializeThunk` 주소를 아는 법 — 부팅 때" 절, 4장 run48 줄, 5장
- 진행표: `_workspace/plan.md` Q93 (끝) · Q42 · Q94 · Q95, 결정 기록 2026-10-29
- 앞 해설:
  - [run10](../02_sprint/run10-allocate-process-해설.md) 3.10 — 관리실 세트 "한 벌" 짐작 (이번에 `[정적]`), 멈춤점 팁 (이번 정정)
  - [run16](../02_sprint/run16-thunk-context-해설.md) 2.9 — 벽 주소록, 문 여는 날 (이번 글의 바탕)
  - [run31](../02_sprint/run31-image-section-해설.md) — 섹션 사슬, 서랍, 꾸러미, 서명 재시도, 재배치 델타
  - [run32](../02_sprint/run32-params-initblock-해설.md) — 업무 수첩 (`LdrSystemDllInitBlock`), init 블록 재배치 칸
  - [run43](run43-baggage-checklist-해설.md) 2.2 — 짐 지도 "쓰는 말"
  - [run44](run44-baggage-snap-해설.md) — 사진 K1, `PsNtdllExports` 값
  - [run47](run47-section-chain-해설.md) — exe 섹션 묶음 칸, "아무도 안 씀", 에뮬레이터가 대신 채울 칸

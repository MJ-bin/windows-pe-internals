# run26 map-dll-file — 해설

> 이 글은 [run26 문서](run26-map-dll-file.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **정정 (run38)**: 2.3 걸음 4 의 "`DllBase`·지시서 `+0xa8` 을 `LdrpMinimalMapModule` 이 채운다 `[추측]`" 은 `[정적]` 으로 조금 다르게 닫혔다 — `LdrpMinimalMapModule` 은 두 칸의 **주소**를 `+0x174 NtMapViewOfSection` (ret `+0x179`) 에 넘기고, **시스템 콜이 직접** 채운다. `+0xa8` 은 ViewSize 칸이다 (2.8 표 "맵 크기일 것 `[추측]`" 도 확정). 4절 "재배치 후보 둘 `[추측]`" 은 **첫째가 맞았다** — `LdrpCompleteMapModule +0x31b LdrpRelocateImage`, 맵 결과가 정확히 `0x40000003` 일 때만 `[정적]`. 둘째 (`LdrpIsExecutableRelocatedImage`) 는 안 봤다. 본문은 그대로 두었다 → [run38 해설 2.5·2.8](run38-map-module-해설.md#25-dllbase-는-관리사무소가-직접-적는다--run26-의-추측-이-닫혔다-정적)

- 대기열: **Q14** — 2.6 dll1·dll2 를 **파일로** 올리는 길의 본문: `LdrpMapDllSearchPath` → `LdrpMapDllNtFileName` (`NtOpenFile`·`NtCreateSection`) → `LdrpMapDllWithSectionHandle` (`_workspace/plan.md`, 이번에 **끝**)
  - 원래 Q14 는 더 넓은 질문이었는데, 2026-10-23 사용자 결정으로 "파일 길 본문" 으로 좁혔다.
  - 새 항목 둘 (둘 다 정적):
    - **Q52** — 실제로 메모리에 붙이고 마무리하는 쪽 (`LdrpMinimalMapModule`, `LdrpCompleteMapModule`). **재배치**가 어느 쪽인지.
    - **Q53** — DLL 검색 경로 (`LdrpSearchPath`, `LdrpInitializeDllPath`). **exe 폴더**가 들어가는 자리.
  - 동적으로 볼 것은 새 번호 없이 **Q23** (dll1·dll2 가 올라오는 순간) 과 **Q44** (`NtGlobalFlag`) 에 출처를 더했다.
  - 다음 항목: **Q15** (plan "다음 항목").
- 근거:
  - run 문서 [`run26-map-dll-file.md`](run26-map-dll-file.md) (가장 중요)
  - 요청서 [`_workspace/sast/21_map-dll-file.md`](../../_workspace/sast/21_map-dll-file.md)
    - 주의: 요청서는 몇 곳에서 **call 자리와 ret 자리를 섞어** 적었다 (예: `+0x141` / `+0x146`). 결과서가 `_calls.tsv` 로 바로잡았고, 이 글은 run 문서(= 결과서)를 따른다.
  - 결과서 [`_workspace/sast/21_map-dll-file_result.md`](../../_workspace/sast/21_map-dll-file_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (run17 ~ run24 와 같은 파일).
  - 본문을 읽은 건 **세 함수**다:

| 함수 | rva | 크기 | 직접 호출 |
|---|---|---|---|
| `ntdll!LdrpMapDllSearchPath` | `0x8a50` | 1102바이트 | 23곳 |
| `ntdll!LdrpMapDllNtFileName` | `0xd210` | 1261바이트 | 21곳 |
| `ntdll!LdrpMapDllWithSectionHandle` | `0x9e50` | 967바이트 | 22곳 |

  - 그 아래 함수들(대상 3 + callee = 48개 중 나머지)은 **본문을 안 읽었다.** 이름·인자·결과만.

이 글은 [run25 해설](run25-dll-entry-해설.md) 의 **다음 편**이다. 자리는 다시 **짐 들이기** 쪽으로 돌아온다.

- run25 는 들여온 건물의 **전원 스위치**(시작 함수) 이야기였다.
- 그 전, run22 ~ run24 는 건물을 들이는 **순서**(목록 → 배달 기사 → 게시판 → 도착 알림) 이야기였다.
- 그런데 run24 그림에 칸 하나가 `?` 로 남아 있었다. "게시판 지시서를 떼어 **가져오기**" 를 하면, **실제로 파일이 어떻게 메모리까지 오나.**
- 이번엔 그 `?` 칸을 펼친다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출, 비트와 플래그 → [run9 해설](run9-create-user-process-해설.md) 2절
  - 오프셋 순서 ≠ 실행 순서 → [run9 해설](run9-create-user-process-해설.md) 3.7 (이번 2.5 에서 다시 중요해진다)
- [핸들](run9-create-user-process-해설.md#핸들) (**손잡이**), [이미지 섹션](run9-create-user-process-해설.md#이미지-섹션) (**조립식 건물 세트**)
- [매핑 올리기](run10-allocate-process-해설.md#매핑-올리기) (세트를 땅에 **갖다 놓기**, 붙인 범위 = 뷰), [PEB](run10-allocate-process-해설.md#peb), rva, 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
- 모듈 목록(PEB.Ldr)·모듈 항목 → [run18 해설 2.6](run18-proc-init-setup-해설.md#26-pebldr--건물-명부가-생기는-순간-0x1c1)
- 잠금 → [run11 해설](run11-allocate-thread-해설.md) 2.8
- 로드 문맥(**작업 지시서**), DDAG 노드(**연결 카드**) → [run19 해설](run19-proc-init-modules-해설.md) 2.6
- 지도 이름 사슬, KnownDll 길(**공용 창고**)과 파일 길(**설계도를 펴서 새로 짜기**), 설치 담당 `LdrpMapDllWithSectionHandle` → [run21 해설](run21-load-dll-internal-해설.md) 2.4
- 노드 상태 번호(**진행 도장**) → [run21 해설](run21-load-dll-internal-해설.md) 2.6
- 설치 담당이 마지막에 하는 일, `CorILOnly` → [run22 해설](run22-map-snap-dep-해설.md) 2.5
- 남은 자식 수 `+0x6c` (**체크판**) → [run22 해설](run22-map-snap-dep-해설.md) 2.6
- 게시판, `LdrpProcessWork` 가 도장 보고 하나만 하기, `LdrpSignalModuleMapped`(**도착 알림**), test.exe → dll1 → dll2 이야기 그림 → [run24 해설](run24-work-queue-해설.md) 2.6 · 2.7 · 2.10

비유도 그대로 이어 쓴다.

- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- DLL = **건물**, PEB.Ldr = 가게 **건물 명부**, 모듈 항목 = **명부 한 줄**, DDAG 노드 = **연결 카드**, `State` = 카드의 **진행 도장**
- 로드 문맥 = **작업 지시서**, 부모 지시서 `+0x6c` = **체크판**
- 섹션 = **조립식 건물 세트**, 매핑 = 세트를 땅에 **갖다 놓기**, 핸들 = **손잡이**
- KnownDll 길 = **공용 창고**에서 꺼내 오기, 파일 길 = **설계도 파일을 펴서 세트를 새로 짜기**
- `LdrpMapAndSnapDependency` = **건물 담당**, `LdrpMapDllWithSectionHandle` = **설치 담당**
- `LdrpModuleDatatableLock` = **명부실 열쇠** (run24)
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번에 두 담당이 새로 생긴다 (비유 이름일 뿐이다):
  - `LdrpMapDllSearchPath` = **찾기 담당** (어디 있나 찾고, 이미 있나 본다)
  - `LdrpMapDllNtFileName` = **파일 담당** (파일을 열고 세트를 짠다)

한 줄로 먼저 말하면 — **이번 바퀴의 핵심 셋**:

1. **파일에서 메모리까지 한 줄** — 찾기(`LdrpSearchPath`) → 파일 열기(`NtOpenFile`) → 세트 짜기(`NtCreateSection`, `SEC_IMAGE`) → 땅에 갖다 놓기(`LdrpMinimalMapModule`) → 명부에 적기. run24 일감 그림의 마지막 `?` 칸이 이걸로 채워졌다 `[정적]` → 2.3
2. **PEB.Ldr 명부에 들어가는 순간** = `LdrpMapDllWithSectionHandle +0x146 LdrpInsertDataTableEntry` `[정적]`. 갖다 놓은 **바로 뒤**, 명부실 열쇠 안에서 → 2.4
3. **순서가 요청서 짐작과 반대였다** — 마무리(`CompleteMap` → `ProcessMapped` → 도착 알림)를 **먼저** 하고, 자기 import 펼치기(재귀)는 **맨 나중**이다 `[정적]`. 그래서 부모 체크판은 자식이 자기 목록을 펼치기 **전에** 지워진다 → 2.5

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 모두 **돌아올 자리(ret)** 다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)              (run17 ~ run19)
        ├─ +0x19a2  LdrpInitializeKernel32Functions → … (kernel32 준비)             (run20·run21·run25)
        ├─ +0x1aa9  LdrpMapAndSnapDependency (exe 지시서)                            (run22)
        │     └─ +0x34e 배달 기사 [dll1 추정] → +0x182c LdrpQueueWork               (run23)
        ├─ +0x1ab1  LdrpDrainWorkQueue(1)                                           (run24)
        │     └─ +0x187 LdrpProcessWork  도장 0 → 가져오기
        │           └─ +0x6a LdrpMapDllSearchPath                                  ← 이번
        │                 └─ +0x25c LdrpMapDllNtFileName                           ← 이번
        │                       └─ +0x28c LdrpMapDllWithSectionHandle              ← 이번
        │                             ├─ … +0x146 명부에 적기                      ← 이번
        │                             ├─ +0x26c … +0x1e2 도착 알림                 (run24)
        │                             └─ +0x1c8 LdrpMapAndSnapDependency (재귀)    (run22, 이번에 "맨 나중")
        ├─ +0x1af2  LdrpPrepareModuleForExecution (exe 항목)                         (run21·run24)
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16·run25, 뒤는 추측)
```

- 이번 자리도 **짐 들이기**(`+0x1aa9` ~ `+0x1ab1`) 안쪽이다.
- run24 가 "게시판 지시서를 누가 떼어 가나" 였다면, 이번은 **떼어 간 지시서로 실제로 건물을 가져오는 손**이다.
- flow.md 로는 **2.6** (`[공백]` import DLL 이 메모리에 올라오는 순간) 이 중심이다. **2.5** 에도 줄이 더해졌다 (kernel32 도 같은 설치 담당을 쓴다).

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. run24 일감 그림 `LdrpProcessWork +0x6a` → `LdrpMapDllSearchPath` → **`?`** → 매핑 함수 — 이 `?` 를 코드로 채운다.
2. dll1·dll2 가 **PEB.Ldr 명부에 들어가는 순간**은 어디인가.
3. 설치 담당(`LdrpMapDllWithSectionHandle`) 안에서 **"자기 import 펼치기(재귀)" 와 "마무리" 중 무엇이 먼저**인가.

### 바퀴 전에 알던 것

- `[정적: 지도]` (run21) 파일 길 이름 사슬: `LdrpProcessWork +0x6a` → `LdrpMapDllSearchPath +0x25c` → `LdrpMapDllNtFileName` 안 `+0x137 NtOpenFile` / `+0x194 NtCreateSection` / `+0x28c LdrpMapDllWithSectionHandle` → `+0x4e LdrpMinimalMapModule` → `+0x179 NtMapViewOfSection`.
  - 이름만 이어 본 것이다. **본문은 안 읽었다.** 그래서 run21 해설은 "열기 → 짜기 → 갖다 놓기" 순서도 `[추측]` 으로 남겼다.
- `[정적: 지도]` (run20·run21) 명부에 끼우는 `LdrpInsertDataTableEntry` 의 호출자 중 하나가 `LdrpMapDllWithSectionHandle +0x141` → "명부에 넣는 자리도 설치 담당 안" 이라고만 알았다.
- `[정적]` (run18) PEB.Ldr 의 적재·메모리 순서 목록(`+0x10`/`+0x20`)에 쓰는 함수는 `LdrpInsertDataTableEntry` 뿐이다 (`refs PebLdr`).
- `[정적]` (run22·run24) 설치 담당의 호출 순서 `+0x26c LdrpProcessMappedModule` → `+0x1c8 LdrpMapAndSnapDependency`. 그 안 `+0x1e2 LdrpSignalModuleMapped` 가 부모 체크판을 지운다 (run24).
- `[정적]` (run24) dll1·dll2 같은 맨 이름은 `LdrpProcessWork` 의 기본 길 `+0x6a LdrpMapDllSearchPath` 를 탈 것 — 이건 `[추측]` (맨 이름이라는 근거뿐).

---

## 2. 찾은 것

### 2.1 한눈에 — 세 담당이 차례로

run 문서 "호출 관계" 를 줄였다. 숫자는 run 문서 그대로. 모두 `[정적]`. 잡일(로그·ETW·버퍼 놓기)과 안 탈 갈림은 뺐다 (2.7).

```
LdrpProcessWork +0x6a  (도장 0, 기본 길 — run24)
└─ 찾기 담당  LdrpMapDllSearchPath(지시서)                              rva 0x8a50
   ├─ 찾아볼 곳 목록 = 보통 지시서 +0x10
   ├─ (1) +0x131 LdrpSearchPath → NT 경로 A · 전체 이름 · 기본 이름
   │        못 찾음 (0xc0000135) → 파일 안 열고 오류로 끝
   ├─ (2) 열쇠 안 +0x1fd  이미 명부에 있나?  있음 → +0x312 바꿔치기, 끝
   └─ (3) +0x25c 파일 담당  LdrpMapDllNtFileName(지시서, A)            rva 0xd210
        ├─ +0x4b  LdrpCheckForRetryLoading       참이면 0xc000022d 로 끝
        ├─ +0x137 NtOpenFile                     파일 열기
        ├─ +0x194 NtCreateSection                세트 짜기 (속성 0x1000000 = SEC_IMAGE)
        ├─ +0x2ea LdrpCodeAuthzCheckDllAllowed
        ├─ +0x28c 설치 담당  LdrpMapDllWithSectionHandle(지시서, 섹션)   rva 0x9e50
        │    ├─ +0x4e  LdrpMinimalMapModule        땅에 갖다 놓기
        │    ├─ 열쇠 안: +0x102 이름으로 / +0x134 매핑으로 이미 있나?
        │    │     있음 → +0x3c3 바꿔치기, 끝
        │    │     없음 → +0x146 LdrpInsertDataTableEntry  ★ 명부에 적기
        │    │            → +0x151 LdrpInsertModuleToIndexLockHeld  색인
        │    ├─ +0x24f LdrpCompleteMapModule
        │    ├─ +0x26c LdrpProcessMappedModule     도장 2 + 도착 알림 (run24)
        │    ├─ +0x282 LdrpLogNewDllLoad · ParentDllBase
        │    └─ +0x1c8 LdrpMapAndSnapDependency    ★ 맨 마지막 — 자기 import 펼치기
        └─ +0x2a0 NtClose(섹션) · +0x2a9 NtClose(파일)   (지시서 +0xb0 == 0 일 때)
   (파일 담당 결과가 0x4000000e 면 (1) LdrpSearchPath 부터 다시)
```

- 왼쪽 `(1) (2) (3)` 은 **찾기 담당 안에서의 순서**다. 번호는 run 문서 트리와 같다.
  - flow.md 그림 2 는 설치 담당 아래 번호를 따로 매겼다: `(1) +0x4e` → `(2) +0x146` → `(3) +0x151` → `(4) +0x24f` → `(5) +0x26c` → `(6) +0x1c8`. 가리키는 건 같다.
- `★` 둘이 이번 핵심 ② · ③ 이다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 찾기 담당이 파일 담당을 부르는 줄

`└─ (3) +0x25c 파일 담당  LdrpMapDllNtFileName(지시서, A)`

- **누가**: 찾기 담당 `LdrpMapDllSearchPath`.
- **어디서**: 돌아올 자리 `+0x25c` (call 은 `+0x257`).
- **누구를**: 파일 담당 `LdrpMapDllNtFileName`. 인자는 지시서와 **NT 경로 `A`** (방금 `LdrpSearchPath` 가 찾아 준 파일 위치).
- **무엇을 위해**: "이 경로의 파일을 열어서 세트로 짜 와." 
- **언제만**: 앞에서 찾았고 `(1)`, 열쇠 안에서 봤더니 명부에 **아직 없을 때** `(2)` 만. 이때 결과가 `0xc0000135` 다 (run 문서 표 "조건").
  - 헷갈리지 말 것: `0xc0000135` 는 보통 "DLL 을 못 찾음" 으로 쓰이는 값인데, 여기선 **"명부에서 못 찾음 = 아직 안 올라옴"** 이라 오히려 계속 가는 신호다 `[정적]` (값의 이름은 `[추측]`).
- 이 줄이 run24 그림의 `?` 화살표 **첫 칸**이다.

#### 한 줄 읽기 ② — 설치 담당이 명부에 적는 줄

`│    │     없음 → +0x146 LdrpInsertDataTableEntry  ★ 명부에 적기`

- **누가**: 설치 담당 `LdrpMapDllWithSectionHandle`.
- **어디서**: 돌아올 자리 **`+0x146`** (call 은 `+0x141`).
  - 예전 문서·해설(run20·run21, flow.md 2.5)에 적힌 `+0x141` 은 **call 자리**였다. 그림 라벨 규칙(ret)으로는 `+0x146`. flow.md 에는 이번에 둘을 같이 적었다 (run 문서 "정정").
- **누구를**: `LdrpInsertDataTableEntry(E)` — `E` 는 이 지시서의 명부 줄 (`ctx+0x38`).
- **무엇을 위해**: 방금 땅에 갖다 놓은 건물을 **PEB.Ldr 명부에 한 줄 적는다** (2.4 에서 근거를 단계별로).
- **언제만**: 명부실 열쇠(`LdrpModuleDatatableLock`) 안에서, 이름으로도(`+0x102`) 매핑으로도(`+0x134`) 같은 건물을 못 찾았을 때. 그리고 지시서 `+0x20` 비트 `0x800000` 이 꺼져 있을 때.

### 2.3 핵심 ① 파일에서 메모리까지 — 다섯 걸음

dll1 하나를 따라간다고 치고, 한 걸음씩 본다. 각 걸음의 근거는 모두 `[정적]` 이다. **dll1·dll2 가 정말 이 길을 타는지**는 `[추측]` 이다 (끝에서 다시).

#### 걸음 1 — 찾기: `+0x131 LdrpSearchPath`

**새 말 — DLL 검색 경로**: DLL 이름(`dll1.dll`)만 받았을 때 "**어느 폴더들을, 어떤 순서로 뒤져 볼지**" 를 적은 목록이다.

- 비유: 건물 이름만 적힌 쪽지를 받았을 때 들고 가는 **"찾아볼 곳 목록"**. (flow.md 비유는 "주소록" 이라고 불렀다. run20 의 "벽 주소록" 과는 다른 것이다.)

코드에서 본 것 `[정적]`:

- 목록은 보통 **지시서 `+0x10`** 칸에 있다.
  - 이건 프로세스 시작 때 만든 것이다: `LdrpInitializeProcess +0x1313 LdrpInitializeDllPath(0,0,&경로)` 로 만들고, `+0x1320` 이 exe 지시서 `+0x10` 에 넣는다 (결과서).
  - 예외: 부모 명부 줄의 `DependentLoadFlags` 가 마스크 `0x7b00` (정책 비트에 따라 `0x7f00`) 와 겹치면 `+0x335 LdrpInitializeDllPath` 로 **새로** 만든다.
  - sample `build.bat` 에 `/DEPENDENTLOADFLAG` 가 없어서, dll1·dll2 쪽은 `+0x10` 을 쓸 것이다 `[추측: 링크 옵션 근거]`.
- `LdrpSearchPath` 가 찾으면 문자열 **셋**을 돌려준다:
  - `A` = NT 경로 → 다음 걸음의 파일 이름
  - `C` = 전체 이름 → 명부 줄 `FullDllName`
  - `D` = 기본 이름 → 명부 줄 `BaseDllName` (해시해서 `E+0x108`)
- **못 찾으면** (`0xc0000135`): shim(옛 프로그램용 호환성 고치기 장치로 알려진 것 `[추측: 일반적으로 알려진 설명]`)이 살려 주지 않는 한, **파일 담당은 부르지도 않고** 오류로 끝난다.
- 그다음 열쇠 안에서 "**이미 명부에 있나**" 를 한 번 본다 (`+0x1fd`). 있으면 새로 안 가져온다 (2.6).

- **exe 폴더(test.exe 가 있는 폴더)가 이 목록에 들어가는 줄은 이 세 함수 어디에도 없다** `[정적]`. 목록을 만드는 `LdrpInitializeDllPath`, 실제로 폴더를 도는 `LdrpSearchPath` 안쪽 몫이다 → **Q53** (4절).

#### 걸음 2 — 파일 열기: `+0x137 NtOpenFile`

- 파일 담당이 받은 NT 경로로 파일을 연다. 손에 **파일 손잡이**(핸들)가 생긴다.
- 숫자 (`.asm` 그대로) `[정적]`: 원하는 권한 `0x100021`, 공유 `5`, 옵션 `0x60`.
  - 이름으로 읽으면 "읽기 + 실행 + 동기화 / 읽기·삭제 공유 허락 / 폴더 말고 파일만·동기 I/O" 다 `[추측: SDK 상수]`.
- 실패하면:
  - "이름 없음·경로 없음" (`0xc0000034`·`0xc000003a`) → **`0xc0000135` 로 바꿔** 돌려준다.
  - "접근 거부" (`0xc0000022`) → 조건부로 **한 번만** 다시 연다.
- 그 앞의 `+0x4b LdrpCheckForRetryLoading` 은 늘 부르고, 참이면 파일을 안 열고 `0xc000022d` 로 끝난다. 안의 조건은 안 읽었다.
- 비유: 공용 창고에 없는 건물이니, **설계도 파일이 든 서류함을 연다.** 손에 서류함 손잡이가 쥐어진다.

#### 걸음 3 — 세트 짜기: `+0x194 NtCreateSection` (`SEC_IMAGE`)

**새 말 — `SEC_IMAGE`**: 섹션을 만들 때 "**이 파일을 실행 파일(PE) 모양대로 짜 달라**" 고 붙이는 표시. 이걸 붙여야 [이미지 섹션](run9-create-user-process-해설.md#이미지-섹션)(조립식 건물 세트)이 된다. 붙이지 않으면 그냥 파일 내용 그대로의 섹션이다 `[추측: 일반적으로 알려진 설명]`.

- 비유: 설계도를 펴서 "**조립식 건물 세트로 짜 주세요**" 라고 주문서에 도장을 찍는 것.

코드에서 본 것 `[정적]`:

- 숫자: 원하는 권한 `0xd`, 페이지 보호 `0x10`, 할당 속성 **`0x1000000`**, 파일 = 걸음 2 에서 연 것.
  - `0x1000000` = `SEC_IMAGE`, `0x10` = `PAGE_EXECUTE` 라는 이름은 `[추측: SDK 상수]`. 숫자 자체는 `[정적]`.
  - WOW64(32비트 프로세스) 이고 지시서 비트 `0x800` 이 꺼져 있으면 `0x1100000` 으로 바뀐다. test.exe(64비트)와는 상관없는 갈래일 것이다 `[추측: 64비트]`.
- 실패하면: 몇 코드는 `+0x499 LdrAppxHandleIntegrityFailure`, 자원 부족류는 로그만, **그 밖은 `+0x206 NtRaiseHardError(0xc000007b, …)`** — 사용자에게 띄우는 오류 창 쪽으로 보인다 `[추측]`. 그다음 파일 손잡이를 닫고 돌아간다.
- 성공하면 `+0x2ea LdrpCodeAuthzCheckDllAllowed` (실행 허락 검사로 보이는 이름 `[추측: 이름]`) 를 지나 다음 걸음으로.

#### 걸음 4 — 땅에 갖다 놓기: `+0x28c` → `+0x4e LdrpMinimalMapModule`

- 파일 담당이 **세트 손잡이(섹션 핸들)** 를 설치 담당에게 넘긴다 (`+0x28c`).
- 설치 담당이 맨 먼저 `+0x4e LdrpMinimalMapModule(지시서, 섹션)` 을 부른다. **여기서 메모리에 붙는다** `[정적]`.
  - 그 안의 `+0x179 NtMapViewOfSection` 은 **지도 이름**으로만 안다 (run21). 본문은 안 읽었다 → Q52.
  - 바로 아래에서 명부 줄 `DllBase` 와 지시서 `+0xa8` 을 읽는다. 이 둘을 `LdrpMinimalMapModule` 이 채운다는 건 `[추측]`.
- 결과가 `0x4000000e` (이미지 **기계 종류 불일치**) 면 그대로 돌려준다. 그러면 찾기 담당이 **`LdrpSearchPath` 부터 다시** 찾는다. 두 번째에도 못 찾으면 `0xc000007b`.
  - 비유: 세트를 땅에 놓아 보니 **다른 규격(다른 CPU용)** 건물이다. 그러면 찾아볼 곳 목록의 **다음 곳**을 다시 뒤진다. (다음 후보를 어떻게 고르는지는 `LdrpSearchPath` 안쪽 `[추측]`.)

#### 걸음 5 — 명부에 적기: `+0x146 LdrpInsertDataTableEntry`

- 2.4 에서 따로 천천히 본다.

#### 끝 — 손잡이 둘을 놓는다

- 설치 담당이 돌아오면, 파일 담당이 **세트 손잡이 → 서류함 손잡이** 순으로 닫는다 (`+0x2a0`, `+0x2a9`) `[정적]`.
  - 예외: 지시서 `+0xb0 ≠ 0` 이면 닫지 않고 지시서 `+0xb8`(파일)·`+0x18`(섹션)에 맡긴다. `+0xb0` 의 뜻은 **모른다** `[공백]`.
- "손잡이를 놓아도 이미 땅에 놓인 건물(뷰)은 그대로 남는다" 는 일반 지식이다 `[추측]` (이 코드의 근거는 아니다).

#### 다섯 걸음을 비유로 한 번에

- 쪽지("dll1.dll 가져와")를 받은 **찾기 담당**이 찾아볼 곳 목록을 들고 건물 위치를 찾는다. (걸음 1)
- 명부를 한 번 들춰 "이미 있나" 본다. 없으면 **파일 담당**에게 넘긴다.
- 파일 담당은 서류함을 열고(걸음 2), 설계도를 조립식 세트로 짜게 한다(걸음 3).
- 세트를 **설치 담당**에게 넘기면, 설치 담당이 땅에 갖다 놓고(걸음 4), 명부에 이름을 적는다(걸음 5).
- 다 끝나면 파일 담당이 손잡이 둘을 내려놓는다.

#### 이걸로 run24 그림의 `?` 가 채워졌다

run24 해설 "Q12 세 바퀴 한 장으로" 의 ④ 칸이 이렇게 바뀐다 (숫자는 run 문서·flow.md 그대로):

```
전 (run24)
├─ ④ 올리기 ── 도장 0
│    +0x6a LdrpMapDllSearchPath → ? (지도 이름 사슬) → 설치 담당 LdrpMapDllWithSectionHandle
│      +0x26c LdrpProcessMappedModule ...
│      +0x1c8 LdrpMapAndSnapDependency ...

후 (run26)
├─ ④ 올리기 ── 도장 0
│    +0x6a LdrpMapDllSearchPath
│      +0x131 LdrpSearchPath → (명부에 없으면) +0x25c LdrpMapDllNtFileName
│        +0x137 NtOpenFile → +0x194 NtCreateSection (SEC_IMAGE) → +0x28c 설치 담당
│          +0x4e  LdrpMinimalMapModule        갖다 놓기
│          +0x146 LdrpInsertDataTableEntry    명부  → +0x151 색인
│          +0x24f LdrpCompleteMapModule
│          +0x26c LdrpProcessMappedModule     도장 2 + 도착 알림 (⑤ 로)
│          +0x1c8 LdrpMapAndSnapDependency    맨 마지막 (① 로, 손자)
```

- `?` 가 없어졌다. 칸마다 `[정적]` (본문). run21 의 이름 사슬이 **본문으로도 맞다.**
- 다만 **실행으로 확인한 칸은 여전히 없다.** dll1·dll2 가 정말 이 길(KnownDll 이 아닌 파일 길)을 타는지는 `[추측: 모듈 대응]` → 동적 Q23.
  - 이 세 함수 안엔 **KnownDll 갈림이 없다** `[정적]`. KnownDll 길(`LdrpLoadKnownDll +0xf1`, run21)은 설치 담당 `LdrpMapDllWithSectionHandle` 에서 **이 길과 만난다.** 그래서 걸음 4·5 는 공용 창고에서 꺼내 온 건물도 똑같이 지난다 `[정적]`.

### 2.4 핵심 ② PEB.Ldr 명부에 들어가는 순간 — `+0x146`

이 레포의 큰 질문 하나가 "**dll1·dll2 가 언제 명부에 이름을 올리나**" 였다. 답은 `LdrpMapDllWithSectionHandle +0x146 LdrpInsertDataTableEntry` 다 `[정적]`. 왜 그렇게 말할 수 있는지 단계별로.

#### 근거 → 그래서 — 단계별로

1. `[정적]` (run18) `refs PebLdr` 로 보면, PEB.Ldr 의 **적재 순서 목록(`+0x10`)·메모리 순서 목록(`+0x20`)** 에 쓰는 함수는 `LdrpInsertDataTableEntry` **하나뿐**이다.
   - 즉 "명부에 줄을 끼우는 손" 은 이 함수 하나다.
2. `[정적]` (run20) 이 함수를 부르는 곳은 **4곳**이다: ntdll 항목, exe 항목, AppVerifier 묶음 안, 그리고 `LdrpMapDllWithSectionHandle` (DLL 매핑 뒤).
   - 앞의 셋은 ntdll·exe·검증기용이다. **보통 DLL 이 명부에 들어갈 길은 넷째뿐**이다.
3. `[정적]` (run26) 넷째 자리의 본문을 이번에 읽었다. 순서가 이렇다:
   - `+0x4e` 땅에 갖다 놓기 → `+0xa6 RtlImageNtHeaderEx` (헤더 찾기) → 열쇠 잡기(`+0xd9`) → 이름으로 찾기(`+0x102`) → (없으면) 매핑으로 찾기(`+0x134`) → **둘 다 없으면 `+0x146 LdrpInsertDataTableEntry(E)`** → `+0x151` 색인 → 열쇠 놓기(`+0x15d`).
4. 그래서 → **보통 DLL 이 명부에 들어가는 순간 = `+0x146`.** 갖다 놓은 **바로 뒤**, 명부실 열쇠 안이다.
5. 아직 확인 아님 → 이 줄이 **정말 dll1·dll2 에서** 불리는지는 `[추측]` 이다 (dll1·dll2 가 이 파일 길을 탄다는 것부터 추측이라서). 동적 Q23 에서 `LdrpInsertDataTableEntry` call 자리 `ntdll+0x9f91` 에 멈추고 `lm` 과 비교하면 된다.

#### 같이 오는 "색인" — `+0x151 LdrpInsertModuleToIndexLockHeld`

- 명부에 적은 **바로 뒤**, 같은 열쇠 안에서 색인에도 끼운다 `[정적]`.
- 무슨 색인인지(무엇으로 찾는 표인지)는 이번 근거에 없다. 이름으로 "명부 줄을 빨리 찾게 하는 표" 로 읽힐 뿐이다 `[추측: 이름]` (run19 에서도 같은 짝을 "목록·색인에 넣는 일로 보임 `[추측: 이름]`" 이라 했다).
- 참고: run20 해설은 `LdrpInsertDataTableEntry` 를 "**이름 색인 카드함**(`LdrpHashTable`)에 카드 끼우기" 로 불렀다. run18 근거로는 같은 함수가 PEB.Ldr 두 목록에도 쓴다. 이번 `+0x151` 은 **그것과 다른 함수**다. 헷갈리지 않게 이 글에선 `+0x146` = "**명부에 적기**", `+0x151` = "**색인**" 으로만 부른다.

#### 명부에 있다 ≠ 준비 끝

여기서 하나 짚고 가자. 코드 순서로 보면 `+0x146` (명부) 은 `+0x24f` (마무리), `+0x26c` (도장 2), `+0x1c8` (import 펼치기) **보다 앞**이다 `[정적]`.

- 그러니 dll1 이 명부에 보이는 순간, dll1 은 **땅에 놓이기만** 했다. 마무리도, 전선 꽂기(snap)도, 전원 켜기도 아직이다.
- 비유: 이삿짐 트럭이 건물을 내려놓자마자 **명부에 이름부터 적는다.** 수도·전기는 그다음이다.
- 그래서 나중에 디버거로 "명부에 dll1 이 있나" 를 볼 때, **있다고 해서 다 준비된 건 아니다** — 이건 이 순서에서 바로 나오는 내 정리다 (코드 순서는 `[정적]`, 실제 멈춤 시점의 모습은 동적).
- run8 `[관찰]` "`+0x1bb6` 에서 dll1·dll2 가 **이미** 올라와 있었다" 와는 어긋나지 않는다. 짐 들이기는 `+0x1ab1 Drain(1)` 이 돌아오기 전에 다 끝나기 때문이다 (run24).

### 2.5 핵심 ③ 순서 — 마무리 먼저, import 펼치기 나중

사용자가 예전에 "너무 빠르다" 고 한 부분이 이런 순서 이야기였다. 그래서 여기선 아주 천천히 간다.

#### 먼저 — 두 가지 일

설치 담당이 건물을 땅에 놓고 명부에 적은 뒤, 해야 할 일이 크게 **두 가지** 남는다.

- **(가) 마무리** — `+0x24f LdrpCompleteMapModule` → `+0x26c LdrpProcessMappedModule`.
  - `ProcessMapped` 안에서 카드 도장을 **2 (Mapped, 땅에 놓였음)** 로 찍고, 바로 `+0x1e2 LdrpSignalModuleMapped` 로 **부모들에게 도착 알림**을 보낸다 (run24 `[정적]`).
- **(나) 자기 import 펼치기** — `+0x1c8 LdrpMapAndSnapDependency(지시서)`.
  - 이 건물이 **또 기대는 건물 목록**을 펼친다. 건물 담당(run22)이 이 건물을 부모로 삼아 **처음부터 다시** 돈다. 그래서 "재귀" 다.
  - 비유: 방금 들인 건물 dll1 이 "나는 dll2 가 있어야 해" 하고 목록을 내민다.

질문은 **(가)와 (나) 중 무엇이 먼저냐**였다.

#### 요청서의 짐작 — (나) 먼저

- 요청서(sast/21) 4번은 "`MapAndSnap` 재귀 → `CompleteMap` → `ProcessMapped`" 순서를 예상했다.
- 곧 "자기 import 를 **먼저** 다 펼치고, **그다음** 마무리·알림" 이다.

#### 코드 — (가) 먼저 `[정적]`

```
+0x24f LdrpCompleteMapModule
+0x26c LdrpProcessMappedModule   (도장 2, +0x1e2 도착 알림)
+0x282 LdrpLogNewDllLoad · ParentDllBase (+0x28f)
+0x1c8 LdrpMapAndSnapDependency  ← 맨 마지막
```

- 결과서·run 문서 판정: **요청서 짐작과 반대**다.
- 마무리·알림이 먼저고, import 펼치기가 맨 나중이다.

#### 왜 헷갈리기 쉬웠나 — 주소 순서 ≠ 실행 순서

- 오프셋 숫자만 보면 `+0x1c8` 이 `+0x24f`·`+0x26c` 보다 **작다.** `_calls.tsv` 를 오프셋 순으로 읽으면 `MapAndSnap` 이 먼저 나온다.
- 그런데 코드에서는 `+0x1c0 ~ +0x1cc` 블록(`MapAndSnap` 을 부르는 곳)이 주소로는 위에 있지만, **`+0x2c7 JNC` 에서 거꾸로 뛰어 들어온다** (`.asm` 160줄, 목적지 `0x18000a010 − 0x180009e50 = +0x1c0`).
  - `+0x2c7` 은 `+0x282` (로그) 보다도 뒤다. 그러니 실제로는 **맨 마지막**에 탄다.
- 비유: 책의 앞쪽 페이지에 "부록" 이 인쇄돼 있는데, 본문 끝에서 "부록으로 가시오" 라고 해서 **마지막에 읽게 되는** 모양이다.
- 요청서가 오프셋 순서를 보고 그렇게 짐작했을 것이라는 건 내 짐작이다 `[추측]`. 이런 일은 [run9 해설](run9-create-user-process-해설.md) 3.7 에서도 한 번 봤다.

#### run24 와 이어 보기 — 부모 체크판은 언제 지워지나

이제 run24 의 체크판 이야기와 붙인다. 한 단계씩.

1. `[정적]` (run22) exe 건물 담당은 exe 체크판(`+0x6c`)에 "**자식 수 + 1**" 을 적는다. test.exe 는 자식 dll1 하나라 `1 + 1 = 2`.
2. `[정적]` (run22·run24) 고리 끝에서 exe 담당이 자기 몫 1 을 지운다 → 1. dll1 은 아직 안 왔으니 여기선 0 이 안 된다 `[추측: 일꾼 없을 때]`.
3. `[정적]` (run24) 남은 1 은 **dll1 이 땅에 놓이는 순간**(도장 2) `LdrpSignalModuleMapped` 가 지운다 → 0 → exe 도장 4, "**exe 전선 꽂아 주세요**" 지시서를 게시판에 붙인다.
4. `[정적]` (run26) 그 `Signal` 은 `+0x26c ProcessMapped` 안이고, `+0x26c` 는 **dll1 의 `+0x1c8` (dll2 펼치기) 보다 앞**이다.
5. 그래서 → dll1 하나를 처리하는 동안 게시판에 붙는 순서는 이렇다 `[정적: 코드 순서]`:
   - 먼저 "**exe 전선 꽂아 주세요**" (3번, dll1 의 `+0x26c` 안)
   - 그다음 "**dll2 가져와 주세요**" (dll1 의 `+0x1c8` → 배달 기사 → `+0x182c`)
6. 이게 run24 이야기 그림의 `큐: [exe 꽂기, dll2 가져오기]` 가 생기는 이유다. flow.md 도 이번에 이 줄을 `[정적]` 으로 적었다: "부모(예: exe)의 snap 일감은 자식(dll1)이 **자기 import 를 펼치기 전에** 큐에 들어갈 수 있다".

run24 해설 그림에서 dll1 부분만 다시 (숫자는 run24·run26 그대로, 일꾼이 없다고 칠 때 `[추측: 일꾼 없음]`):

```
(1) +0x187 ProcessWork(dll1)  도장 0 → +0x6a 찾기 담당 → +0x25c 파일 담당 → +0x28c 설치 담당
      +0x4e  갖다 놓기
      +0x146 명부에 dll1                                                         ← run26
      +0x24f CompleteMap                                                         ← run26
      +0x26c ProcessMapped: dll1 도장 = 2
        +0x1e2 Signal(dll1) → exe 체크판 1 → 0 → +0x5e QueueWork(exe)            큐: [exe 꽂기]
      +0x1c8 MapAndSnap(dll1)   ← 맨 마지막                                       ← run26 에서 확인
        +0x34e 배달 기사("dll2.dll") → +0x182c QueueWork(dll2)                   큐: [exe 꽂기, dll2 가져오기]
```

#### 만약 반대였다면 — 가정 이야기

- 요청서 짐작대로 (나)가 먼저였다면, dll1 은 dll2 를 **먼저** 게시판에 붙이고, 그다음에 exe 체크판을 지웠을 것이다.
- 그러면 큐는 `[dll2 가져오기, exe 꽂기]` 로, exe 전선 꽂기가 **한 칸 늦어진다.**
- 이건 코드에 없는 순서로 해 본 **가정**이다. 내 정리일 뿐이다 `[추측: 내 정리]`. 실제 코드는 (가)가 먼저다.

#### 그래서 무엇이 중요하나

- 부모는 "**자식이 땅에 놓였나**" 만 기다린다. **손자(dll2)까지 기다리지 않는다** — run24 해설 2.7 (가) 에서 본 말이, 이번엔 설치 담당 안의 **호출 순서**로도 받쳐졌다 `[정적]`.
  - 손자는 자식 dll1 자신의 체크판이 따로 센다 (run24).
- 왜 그래도 되나: exe 가 전선을 꽂는 상대는 dll1 의 **호실 이름표(export)** 라서, dll1 이 땅에 놓이기만 하면 될 것이다 `[추측: 일반적으로 알려진 설명]`. dll2 는 dll1 이 꽂을 상대다.
- 비유: 본 건물(exe)의 전기 기사는 **옆 건물(dll1)이 도착하자마자** 일을 시작할 수 있다. 옆 건물이 자기 뒤 건물(dll2)을 주문하는 건 **그 건물 사정**이다.
- **어느 쪽이든 snap 은 한 번**이다. 마지막으로 체크판을 지우는 쪽이 시작한다 (run24). 순서는 큐 자리만 바꾼다.

#### 펼치기를 아예 안 하는 경우 `[정적]`

- 지시서 비트 `0x800000` 이 꺼져 있고 **그리고** 명부 줄 `Flags` 비트 2 (`ImageDll`) 도 꺼져 있으면 → 펼치지 않고 도장 **9** (`ReadyToRun`) 만 찍고 0 을 돌려준다 (`+0x387 ~ +0x3b3`). 패치 이미지 + 부모가 exe 인 경우만 예외.
  - run22 해설 2.5 의 "이미지로 안 올린 것은 `State = 9` 만" 이 이것이다.
- `CorILOnly` (비트 24) 가 켜져 있으면 `+0x1c8` 대신 `+0x2d5 LdrpCorProcessImports`.
- dll1·dll2 는 보통 DLL 이니 `+0x1c8` 쪽일 것이다 `[추측]`.

### 2.6 이미 있는 건물은 두 번 거른다

`[정적]` 같은 건물을 두 번 가져오지 않으려고 **두 군데**서 본다. 둘 다 명부실 열쇠 안이다.

| 언제 | 어디서 (ret) | 찾으면 |
|---|---|---|
| 파일 열기 **전** | 찾기 담당 `+0x1fd LdrpFindLoadedDllByNameLockHeld` | `+0x312 LdrpLoadContextReplaceModule`, 매핑 안 함 |
| 땅에 놓은 **뒤** | 설치 담당 `+0x102` (이름) · `+0x134` (매핑) | `+0x3c3 LdrpLoadContextReplaceModule`, 끝 — `CompleteMap`·`ProcessMapped`·`MapAndSnap` 다 안 탐 |

- `LdrpLoadContextReplaceModule` = 이 지시서의 명부 줄을 **이미 있는 줄로 바꿔치기** 하는 것으로 읽힌다 `[추측: 이름]`.
- 왜 두 번이나 보나: "그 사이 다른 직원(스레드)이 같은 건물을 먼저 들였을 때" 를 위한 것이라는 이유는 `[추측]`. 코드엔 이유가 안 쓰여 있다. 열쇠 안이라 그럴 수 있을 뿐이다.
- 뒤쪽에서 걸러질 때, **이미 땅에 놓은 세트(뷰)를 어떻게 치우는지**는 안 읽었다 `[공백]`.
- 비유: 쪽지를 받으면 명부부터 들춰 본다. 그리고 세트를 땅에 놓은 뒤 **열쇠를 잡고 한 번 더** 들춰 본다. 그새 누가 같은 건물을 들였으면, 내 쪽지를 그 줄로 바꿔 적고 끝낸다.

### 2.7 test.exe 길에서 안 탈 갈림 — 짧게

코드엔 있지만 **조건이 안 맞으면 안 타는** 호출들이다 (run 문서 정리). 실제 값은 동적이다.

| 호출 (ret) | 타는 조건 | test.exe 짐작 |
|---|---|---|
| `NtFileName +0x10e NtSystemDebugControl` | `PEB.NtGlobalFlag & 0x40000` | 값에 따라 (동적 **Q44**) |
| `NtFileName +0x40c LdrpValidateIntegrityContinuity` | 정책 바이트 `LdrpAuditIntegrityContinuity ≠ 0` | 안 탈 것 `[추측]` (값은 Q23) |
| `NtFileName +0x483 LdrpSetModuleSigningLevel` | 지시서 비트 `0x1000000` + 패키지 프로세스 아님 | 안 탈 것 `[추측]` |
| `NtFileName +0x206 NtRaiseHardError` · `+0x499 LdrAppxHandleIntegrityFailure` | `NtCreateSection` 실패 | 정상이면 안 탐 `[정적: 조건]` |
| `SearchPath +0x3b9` (간접) · `+0x412 LdrpResolveDllName` | shim 켜짐 | 안 탈 것 `[추측]` (값은 Q23) |
| `WithSectionHandle +0x313 NtQueryVirtualMemory` 등 패치 갈래 | `LoadReason == 9` (패치 이미지) | 안 탈 것 `[추측: LoadReason 0]` |

- **탈 것**: `LdrpCheckForRetryLoading` (부르는 건 늘, 결과는 모름), `NtOpenFile`, `NtCreateSection`, `LdrpCodeAuthzCheckDllAllowed` (지시서 비트 `0x100` 이 0 이면).
- **디버거와 상관있는 갈래 하나**: `NtSystemDebugControl` 은 `NtGlobalFlag` 비트를 본다. 이 비트 이름은 gflags 의 "디버거 심볼 적재 알림" 쪽으로 알려진 값이다 `[추측: SDK 상수]`. 커널 디버거만 붙인 상태에서 값이 어떤지는 Q44 에서 본다. (CLAUDE.md "디버거가 붙어서 달라지는 점" 후보.)

### 2.8 곁가지 — 지시서 칸 몇 개를 더 알게 됐다

작업 지시서(`_LDRP_LOAD_CONTEXT`)는 PDB 에 칸 이름이 없다. 그래서 **오프셋 + 역할**로만 쓴다 (run22·23 표에 더함). 대표만:

| 칸 | 역할 | 태그 |
|---|---|---|
| `+0x10` | 찾아볼 곳 목록(검색 경로) 포인터 | `[정적]` |
| `+0x18` | 섹션 손잡이 보관 칸 (`+0xb0 ≠ 0` 일 때만) | `[정적]` |
| `+0x20` | 플래그. 이번에 본 비트 `0x8`·`0x20`·`0x100`·`0x800`·`0x10000`·`0x800000`·`0x1000000` | 쓰임 `[정적]`, 뜻 `[추측]`, 이름 모름 |
| `+0xa8` | 맵 크기일 것 | `[추측]` |
| `+0xb0` | **뜻 모름.** `≠ 0` 이면 겹침 검사를 건너뛰고, 손잡이를 안 닫고 맡긴다 | 쓰임 `[정적]`, 뜻 `[공백]` |
| `+0xb8` | 파일 손잡이 보관 칸 (`-1` 로 시작, run22) | `[정적]` |

- 또 하나: 버퍼 놓기 4곳(`SearchPath +0x29e`·`+0x2c2`·`+0x3da`·`+0x42c`)은 Ghidra 가 고른 이름이 엉뚱해서 "버퍼 놓기" 로만 적었다. 같은 주소(rva `0x3d630`)에 이름이 9개 있는 함수다 ([run9 해설](run9-create-user-process-해설.md) 2절 ICF 별칭).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 맨 위 기준 | ntdll pdb 줄 run17 ~ run24, 정적 근거 run9 ~ run25 | + **run26** |
| 그림 2 (1)~(2) 사이 — 찾기 담당 뒤 | `LdrpMapDllSearchPath` → **`?`** → `LdrpMapDllWithSectionHandle` (dll1·dll2 각각) | `?` 화살표 둘을 지우고 `(1) +0x131 LdrpSearchPath` / `(2) +0x25c LdrpMapDllNtFileName` → `(1) +0x137 NtOpenFile` / `(2) +0x194 NtCreateSection` / `(3) +0x28c 설치 담당`. 새 상자는 다 점선 `(정적)` |
| 그림 2 — 설치 담당 아래 | `(1) +0x26c` ProcessMapped → `(2) +0x1c8` MapAndSnap | `(1) +0x4e LdrpMinimalMapModule` → `(2) +0x146 LdrpInsertDataTableEntry` → `(3) +0x151` 색인 → `(4) +0x24f LdrpCompleteMapModule` → `(5) +0x26c` → `(6) +0x1c8`. 새 상자 넷 (dll1·dll2 각각) |
| 2.5 파일 길 사슬 | `[정적: 지도]` 이름 사슬만. `LdrpInsertDataTableEntry` 호출자 `+0x141` | 그 아래 `[정적]` (run26) "**본문으로 맞다**" 줄. `+0x141` 은 call 자리, ret `+0x146` 병기 |
| 2.6 첫 글머리 | 명부에 들어가는 자리 = "설치 담당 안" `[정적: 지도]` | **`+0x146 LdrpInsertDataTableEntry`** `[정적]`. dll1·dll2 가 정말 이 줄을 타는지는 `[추측: 모듈 대응]` → Q23 |
| 2.6 새 절 "파일로 올리는 길" | 없음 | 세 함수 표·트리, 순서 "마무리 먼저", 두 번 거르기, 검색 경로, 손잡이, 안 탈 갈림, 재배치 후보 `[추측]` |
| 2.6 "일감 한 바퀴" · "이야기" 그림 | `… → ? … → LdrpMapDllWithSectionHandle` | `?` 자리에 run26 줄들 (`← run26`). `+0x1c8` 에 "맨 마지막" |
| 4장 | 2.6 "`LdrpMapDllSearchPath` → `LdrpMapDllWithSectionHandle` 사이 (지도 이름만)" | **(해결됨, 정적)**. 새 줄 넷: 재배치 쪽 (Q52), 검색 경로 (Q53), 파일 길 실행 값 (Q23), 이름·뜻 못 정한 것 (run26) |

태그로 보면:

- **파일 길 사슬** (`SearchPath` → `NtFileName` → `WithSectionHandle` → `MinimalMap`): `[정적: 지도]` → **`[정적]`** (본문).
- **명부에 들어가는 자리**: `[정적: 지도]` "같은 함수 안" → **`[정적]`** `+0x146` + 조건. dll1·dll2 대응은 여전히 `[추측]`.
- **마무리 → 펼치기 순서**: run22·run24 의 `[정적]` (호출 흐름) 이 **그대로 맞았다.** 이번엔 앞에 네 칸(갖다 놓기·명부·색인·CompleteMap)이 더해지고, "맨 마지막" 이 점프 근거(`+0x2c7`)까지 붙었다. run 문서 말대로 **이전 run 문서를 고칠 곳은 없다.** 틀렸던 건 요청서 짐작뿐이다.
- **재배치**: 새 `[추측]` 후보 둘 (4절).
- 2.6 절 제목의 `[공백]` 은 **그대로**다. 실행으로 본 칸이 아직 없어서다.

### 큰 그림에서 어떻게 이어지나

- 짐 들이기(`+0x1aa9` ~ `+0x1ab1`)는 이제 노선도로 **처음부터 끝까지** 이어졌다:
  - 건물 담당(run22) → 배달 기사(run23) → 게시판(run24) → **찾기 → 파일 → 설치(run26)** → 도착 알림(run24) → 전선 꽂기(run24, 안쪽은 Q50).
- 남은 정적 칸은 **양 끝 안쪽**이다:
  - 땅에 놓는 손 `LdrpMinimalMapModule` 과 마무리 `LdrpCompleteMapModule` 안 (Q52)
  - 찾아볼 곳 목록을 만들고 도는 `LdrpInitializeDllPath`·`LdrpSearchPath` 안 (Q53)
  - 전선 꽂기 `LdrpSnapModule` 안 (Q50)
- 나머지 큰 질문은 **버스 타 보기**다: dll1·dll2 가 정말 이 길로, 어느 직원(메인 / 일꾼)이, 어떤 순서로 올라오나 (Q23·Q47).
- 다음 정적 항목은 **Q15** (`LdrpInitializeGraphRecurse` — dll2 → dll1 전원 켜기 쪽).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **재배치(주소 고치기)는 누가 하나.** 새 말: 재배치 = 건물이 원래 예정된 기준 주소가 아닌 곳에 놓였을 때, 안에 적힌 주소들을 새 자리에 맞게 고치는 일. 비유: 다른 번지에 놓였으니 건물 안 **안내판의 번지수를 고쳐 쓰기**. 이 세 함수 안엔 그런 줄이 **없다** `[정적]`. 후보 둘 `[추측]`: ① `LdrpCompleteMapModule` (`MinimalMap` 결과 코드를 셋째 인자로 받는다) ② `LdrpProcessMappedModule` (안에 `+0x24d LdrpIsExecutableRelocatedImage`, 지도 이름) | 두 후보 본문을 안 읽었다 (callee) | 정적 **Q52** (`LdrpMinimalMapModule`·`LdrpCompleteMapModule`). run8 `[관찰]` "dll 주소는 실행마다 바뀔 수 있다" 와 이어진다. 동적으로는 `LdrpMinimalMapModule` ret `ntdll+0x9e9e` 에서 `r eax` (Q23) |
| `LdrpMinimalMapModule` 이 `DllBase`·`ctx+0xa8` 을 채우나, 결과 코드 종류, `NtMapViewOfSection` 인자 | 본문 안 읽음 | 정적 **Q52** |
| **exe 폴더가 검색 경로에 들어가는 자리**, 후보가 여럿일 때 순서, `0x4000000e` 뒤 다음 후보를 고르는 법 | 이 세 함수 밖 (`LdrpInitializeDllPath` 가 만들고 `LdrpSearchPath` 가 돈다) | 정적 **Q53** |
| dll1·dll2 가 정말 이 파일 길을 타나 (KnownDll 아님), `NtOpenFile` 파일 이름이 test.exe 폴더의 dll1·dll2 인가 | 실행 때 값. 모듈 대응은 `[추측]` | 동적 **Q23**: 입구 `ntdll+0x8a50`, `LdrpSearchPath` call `ntdll+0x8b7c`; `NtFileName` 입구 `ntdll+0xd210` 에서 `du poi(@rdx+8)`, `NtOpenFile` call `ntdll+0xd342` |
| 섹션 속성이 정말 `0x1000000` 인가 | 실행 때 값 | 동적 **Q23**: `NtCreateSection` call `ntdll+0xd39f` 에서 `dd @rsp+0x28 L1` |
| 명부에 들어가는 실제 순간·순서 | 실행 때 | 동적 **Q23**: `LdrpInsertDataTableEntry` call `ntdll+0x9f91` (dll 이름·주소를 `lm` 과 비교) |
| `CompleteMap` → `ProcessMapped` → `MapAndSnap` 이 같은 DLL 에서 **이 순서로** 서나 | 정적 순서만 봤다 | 동적 **Q23**: call `ntdll+0xa09a` → `ntdll+0xa0b7` → `ntdll+0xa013` |
| 지시서 `+0xb0` 값 (손잡이를 닫나) | 뜻도 값도 모름 `[공백]` | 동적 **Q23**: `ntdll+0x8ba8` (`SearchPath +0x158`) 에서 `dq @rbx+0xb0 L1` |
| 안 탈 갈림이 정말 안 타나 (정책·shim 바이트) | 실행 때 값 | 동적 **Q23**: 정책 `ntdll+0x1d2918`(Audit)·`ntdll+0x1d2900`(Enforce), shim `ntdll+0x1d23cc`·`ntdll+0x1d23cf` |
| `NtGlobalFlag & 0x40000` 이 켜져 있나 (켜지면 `NtSystemDebugControl` 을 탄다) | 실행 때 값, 디버거와 상관있을 수 있다 | 동적 **Q44**: `dd @$peb+0xbc L1` |
| kernel32·kernelbase 도 이 파일 길인가 (KnownDll 아닌가) | 실행 때 | 동적 **Q20** (flow.md 2.5) |
| 뒤쪽 "이미 있음" 으로 걸러질 때 붙인 뷰를 치우는 곳 | 안 읽음 `[공백]` | flow.md 4장 "이름·뜻 못 정한 것 (run26)" (대기열 줄 없음) |
| 이름·뜻 못 정한 것: 지시서 `+0x20` 비트 `0x8`·`0x100`·`0x800000`·`0x1000000` 의 이름, `0xc000022d` 의 이름, `LdrpCheckForRetryLoading` 안 조건, `+0x151` 색인이 무엇으로 찾는 표인지 | PDB 에 지시서 칸 이름이 없다. callee 본문 안 읽음 | 같은 4장 줄 (대기열 줄 없음) |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run26-map-dll-file.md`](run26-map-dll-file.md) — 특히 "호출 지점" 표, "먼저 알아 둘 칸"(명부 줄·지시서), 세 함수 절, "재배치는 어느 호출 몫인가", "호출 관계" 트리, "확인된 것", "정정", "아직 확인 필요"(멈춤점 목록), "도구 메모"(call/ret 섞임)
- 정적 결과서: [`_workspace/sast/21_map-dll-file_result.md`](../../_workspace/sast/21_map-dll-file_result.md) — 질문 1 ~ 5, "`?` 칸 채움 목록", "그림 2 상자 후보", "동적으로 확인할 것"
- 정적 요청서: [`_workspace/sast/21_map-dll-file.md`](../../_workspace/sast/21_map-dll-file.md) — 4번 예상 순서 (이번에 반대로 확인됨)
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - 그림 2 의 `jf0`~`jf3`·`jm1`·`ji1`·`ji2`·`jm2` (dll1), `jg0`~`jg3`·`jn1`~`jn4` (dll2) 상자
  - "2.5" 의 run26 줄 (파일 길 사슬 본문, `+0x141`/`+0x146`)
  - "2.6" 첫 글머리, 새 절 "파일로 올리는 길", "일감 한 바퀴", "test.exe → dll1 → dll2 이야기"
  - 4장 2.6 줄 (Q52·Q53·Q23 멈춤점, 이름·뜻 못 정한 것)
- 앞 바퀴 해설:
  - [run21 해설](run21-load-dll-internal-해설.md) 2.4 — 지도 이름 사슬, 두 길이 만나는 설치 담당 (이번에 본문으로 맞음)
  - [run22 해설](run22-map-snap-dep-해설.md) 2.5 — 설치 담당이 마지막에 하는 일 (이번 순서와 같음)
  - [run24 해설](run24-work-queue-해설.md) 2.6 · 2.7 · 2.10 — 도장 보고 하나만, 도착 알림과 체크판, 이야기 그림의 큐 순서
  - [run25 해설](run25-dll-entry-해설.md) — 바로 앞 편
- 01_sprint: [`runs/01_sprint/run8-dllcheck.md`](../01_sprint/run8-dllcheck.md) — "DLL 주소는 실행마다 바뀔 수 있다" (재배치 질문의 출발점)

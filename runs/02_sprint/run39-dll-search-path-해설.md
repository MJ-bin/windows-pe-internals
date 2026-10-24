# run39 dll-search-path — 해설

> 이 글은 [run39 문서](run39-dll-search-path.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q53** — 2.6 DLL 검색 경로: `ntdll!LdrpSearchPath`, `LdrpInitializeDllPath` — exe 폴더가 검색 경로에 들어가는 자리, dll1·dll2 를 어디서 찾나 (`_workspace/plan.md`, 이번에 **끝**)
  - 원래 [run26](run26-map-dll-file-해설.md) 4절에서 넘긴 질문이다 ("exe 폴더가 검색 경로에 들어가는 줄은 이 세 함수 어디에도 없다").
  - 실행 때 확인할 거리는 새 번호 없이 **Q23** (dll1·dll2 가 올라오는 순간, 동적) 에 출처로 붙었다.
  - 새 정적 공백은 **Q71** (검색 경로 주변 작은 정적) 로 맨 아래에 붙었다.
  - 다음 항목: **Q54** (plan "다음 항목").
- 근거:
  - run 문서 [`run39-dll-search-path.md`](run39-dll-search-path.md) (가장 중요)
  - 요청서 [`_workspace/sast/34_dll-search-path.md`](../../_workspace/sast/34_dll-search-path.md)
  - 결과서 [`_workspace/sast/34_dll-search-path_result.md`](../../_workspace/sast/34_dll-search-path_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`. flow.md 기준과 같은 분석본이다.
  - 요청서가 고른 다섯 함수에서 시작했는데, 목록을 **실제로 짓는 곳**이 그 아래 "주소로 건네지는 함수" 라서 요청서 밖으로 세 번 더 뽑았다 (2.4 에서 왜 그런지).
  - 순서 표(`ntdll+0x17e340`)와 문자열 상수(`PATH`, `.`, `SafeDllSearchMode` 등)는 결과서가 **파일의 원본 바이트**로 읽었다. 그래서 "코드가 표의 몇째 줄을 고르나" 는 `[정적]` 이고, 표에 적힌 숫자 자체는 "파일에 적힌 값" 이다 (flow.md 기준 절).

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](run9-create-user-process-해설.md) 2절 · rva → [run10 해설](run10-allocate-process-해설.md) 2절
- 로드 문맥 = **지시서**, 지시서 `+0x10` = 찾아볼 곳 목록, 파일 길 다섯 걸음 → [run26 해설](run26-map-dll-file-해설.md) 2.3 (걸음 1 "찾기") · 2.8
- exe 명부 줄 (`LdrpImageEntry`, `FullDllName`) → [run19 해설 2.6](run19-proc-init-modules-해설.md) · `ImagePathName` 을 잘라 **파일 이름**을 만든 이야기 → [run19 해설 2.1](run19-proc-init-modules-해설.md#21-run18-정정-하나--exe-폴더가-아니라-exe-파일-이름)
- 프로세스 매개변수 블록 (**개업 서류**, `ProcessParameters`) → [run32 해설 2.3](run32-params-initblock-해설.md#23-새-말--프로세스-매개변수-블록)
- 열쇠 (유저 쪽 잠금) → [run18 해설](run18-proc-init-setup-해설.md) 2.4 · 일꾼 스레드, 주소로 건네지는 함수 → [run24 해설 2.5](run24-work-queue-해설.md#25-일꾼-쪽--ldrpworkcallback-과-그-복사본)
- 레지스트리 (관리실 **설정 장부**) → [run20 해설](run20-kernel32-load-해설.md) 2.8 · 프로세스 힙 (**공용 메모리 창고**) → [run18 해설](run18-proc-init-setup-해설.md) 2.3

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소의 **신청 창구**
- 프로세스 = **가게**, ntdll = 가게 안 **관리실**, DLL = **건물**
- 모듈 항목 = **명부 한 줄**, 로드 문맥(`ctx`) = 건물 하나 들일 때 들고 다니는 **지시서** (run26)
- **DLL 검색 경로** = 건물 이름만 적힌 쪽지를 받았을 때 펼쳐 보는 **주소록** (run26 의 "찾아볼 곳 목록", flow.md 의 "주소록").
  - 주의: run19·run20 의 "관리실 **벽** 주소록" (전역 변수) 과는 다른 것이다. 이 글의 주소록은 **"어느 폴더들을, 어떤 순서로 뒤져 볼지" 적은 책 한 권**이다.

이번에 이 주소록 그림에 네 가지를 더한다 (run 문서 "결과" 의 비유 그대로).

1. **주소록은 미리 만들지 않는다.** 가게를 열 때는 **빈 표지만** 만들어 둔다. 안쪽 페이지는 **누가 처음 펼칠 때** 채운다.
2. **첫 줄 = exe 가 사는 폴더.**
3. 찾을 땐 **위에서부터 한 줄씩** "그 폴더에 `dll1.dll` 있어요?" 하고 관리사무소 창구에 묻는다. **처음 "있어요" 에서 멈춘다.**
4. **exe·dll1·dll2 의 지시서가 주소록 한 권을 같이 들고 다닌다.** 그래서 페이지는 한 번만 채운다.

한 줄로 먼저 말하면:

- 프로세스 시작 때 `LdrpInitializeDllPath(0, 0, &P)` 는 주소록 `P` 를 **0 으로 밀기만** 한다 `[정적]`.
- 처음 `LdrpSearchPath` 가 "비었네?" 하면 `LdrpComputeLazyDllPath` → … → **`RtlpComputePath`** 가 목록 글자를 짓는다 `[정적]`.
- 첫 칸 exe 폴더는 **`RtlpGetDirPath`** 가 exe 명부 줄 `FullDllName` 의 **마지막 `\` 앞까지** 잘라서 만든다 `[정적]`.
- `LdrpSearchPath` 는 칸마다 **`NtQueryAttributesFile`** 로 "파일 있나" 를 묻고, **처음 찾은 칸에서 멈춘다**. 끝까지 없으면 `0xc0000135` `[정적]`.
- dll1·dll2 는 **첫 칸 (exe 폴더)** 에서 찾힐 것이다 — 이건 아직 `[추측]` (코드 순서 + sample 이 한 폴더에 빌드된다는 소스 근거).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

오프셋은 **돌아올 자리(ret)** 다. 숫자는 앞 run 들 그대로.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessW … NtCreateUserProcess … NtResumeThread           (run9 ~ run14, run34)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)                  (run17 ~ run19)
        ├─ +0x1065 ~ +0x1161  시스템 폴더 글자 짓기 (RtlpSystemDirs)                  ← 이번 (조금)
        ├─ +0x1318  LdrpInitializeDllPath(0, 0, &P)   주소록 빈 표지                   ← 이번
        ├─ +0x135e … +0x1374 LdrpImageEntry   exe 명부 줄 (FullDllName 채움 +0x13b9) (run19)
        ├─ +0x19a2  kernel32 준비                                                       (run20·run21·run25)
        ├─ +0x1ab1  LdrpDrainWorkQueue(1) → LdrpProcessWork +0x6a                       (run24)
        │     └─ LdrpMapDllSearchPath  (찾기 담당, dll1·dll2 지시서)                     (run26)
        │           ├─ +0x131 LdrpSearchPath   주소록 펼치기 (처음이면 페이지 채우기)  ← 이번
        │           └─ +0x25c LdrpMapDllNtFileName → … 열기·짜기·놓기               (run26·run38)
        ├─ +0x1b77  LdrpInitializePerUserWindowsDirectory   (뜻 모름, 곁가지)           ← 이번 (refs 에서 봄)
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)                     (run27)
   └─ NtContinue ═▶ RtlUserThreadStart → … → main                                       (run16, run29·run30)
```

- 위치는 run26 의 **걸음 1 "찾기"** 안쪽이다. run26 은 "찾기 담당이 `LdrpSearchPath` 를 부른다" 까지만 봤다.
- 그리고 프로세스 시작 쪽 `+0x1318` 한 줄 — run26 이 "여기서 경로를 **만든다**" 고 적은 그 줄 — 을 이번에 열었다.
- flow.md 로는 **2.6** 의 새 절 "검색 경로" 가 중심이고, **2.4** 에 줄 몇 개가 더해졌다.

### 무엇이 궁금했나

run 문서 "목적" 의 여섯 질문을 줄이면:

1. `+0x1313 LdrpInitializeDllPath(0, 0, &P)` 는 주소록을 **그 자리에서** 채우나, "나중에" 표시만 하나.
2. 실제 목록 글자는 **어디서** 짓나. 요청서가 짐작한 사슬 `LdrpComputeLazyDllPath` → `LdrpGetDllPath` → `RtlpGetCachedPath` 가 맞나.
3. exe 폴더는 **어떤 값**에서 오나. 어디서 잘라 폴더로 만드나.
4. 기본 목록의 **순서**.
5. `LdrpSearchPath` 는 목록을 어떻게 도나. 처음 찾은 곳에서 멈추나. 못 찾으면 무엇을 돌려주나.
6. dll1·dll2 는 목록의 **몇째 칸**에서 찾히나. dll2 도 같은 주소록을 쓰나.

### 바퀴 전에 알던 것

- `[정적]` (run26) 찾기 담당은 지시서 `+0x10` 칸이 가리키는 주소록(경로 구조체)을 쓴다. exe 의 것은 `LdrpInitializeProcess +0x1313 LdrpInitializeDllPath` 가 다루고 `+0x1320` 이 exe 지시서 `+0x10` 에 넣는다.
  - 예외: 부모 명부 줄 `DependentLoadFlags` 가 마스크와 겹치면 찾기 담당이 **새** 주소록을 만든다 (`LdrpMapDllSearchPath +0x330`).
- `[정적]` (run26) **exe 폴더가 목록에 들어가는 줄은** 찾기·파일·설치 담당 세 함수 **어디에도 없다** → Q53.
- `[정적]` (run26) 파일 담당 결과가 `0x4000000e` 면 찾기 담당이 **`LdrpSearchPath` 부터 다시** 찾는다. 그때 "다음 후보를 어떻게 고르나" 는 몰랐다.
- `[정적]` (run19) exe 명부 줄은 `+0x1374` 에서 전역 `LdrpImageEntry` 에 적히고, 그 줄의 `FullDllName` 은 `+0x13aa` 에서 `ImagePathName` (exe 전체 경로) 으로 채워진다.
- `[추측]` (flow.md) "dll1·dll2 는 KnownDll 이 아니라 exe 폴더에서 `LdrpSearchPath` 로 찾는다." 코드 쪽 근거는 없었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 빈 표지, 그리고 처음 펼칠 때

run 문서 "호출 관계" 를 줄였다. 오프셋은 **ret 자리**. 모두 `[정적]`. 로그·버퍼 정리 같은 잡일은 뺐다.

```
LdrpInitializeProcess
 ├─ +0x1318  LdrpInitializeDllPath(0, 0, &P)      P 0x80 바이트를 0 으로. 호출·할당 없음    ← 빈 표지
 │           → +0x1320  exe 지시서 +0x10 = &P
 └─ +0x135e … exe 명부 줄 → +0x1374 LdrpImageEntry, +0x13b9 FullDllName = ImagePathName 사본

… LdrpMapDllSearchPath (dll1 지시서, +0x10 = 같은 &P)
   └─ +0x131 LdrpSearchPath(지시서, P, …)
        ├─ (1) [P+0 == 0 일 때만]  +0x372 LdrpComputeLazyDllPath(P)                         ← 페이지 채우기
        │      └─ +0x67 LdrpGetDllPath
        │           └─ +0xad RtlpGetCachedPath(&RtlpDllSearchPath, RtlpComputeDllPath, 0, 0)  캐시 창구
        │                └─ (캐시 없으면) +0xc3 RtlpComputeDllPath(0, 0)                      순서 표 한 줄 고르기
        │                     ├─ (1) +0x50 RtlpLookupCurDirSetting("SafeDllSearchMode")
        │                     └─ (2) +0x7f RtlpComputePath(줄 1 = 5 2 4 3 8, 5칸)            ★ 글자 짓기
        │                              ├─ 칸 5: +0x55a RtlpGetDirPath(0)                       ★ exe 폴더
        │                              └─ 칸 3: +0x2c1 / +0x329 RtlQueryEnvironmentVariable("PATH")
        └─ (2) 칸마다: "폴더" + '\' + 이름 → +0x249 LdrpResolveDllName(후보)
                 ├─ (1) +0x29d LdrpGetFullPath                       전체 경로
                 └─ (2) +0x140 LdrpGetNtPathFromDosPath
                          └─ +0xf3 NtQueryAttributesFile             ★ 파일 있나 (시스템 콜)
               0 이상 → 멈춤.  못 찾음 계열 → 다음 칸.  목록 끝 → 0xc0000135
```

- 위쪽 덩어리 = **가게 열 때** (한 번). 아래쪽 덩어리 = **DLL 을 찾을 때마다**.
- 아래쪽 `(1)` 은 주소록이 **비었을 때만** 탄다. 한 번 채우면 다음부턴 `(2)` 만 탄다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 페이지를 채우는 줄

`├─ (1) [P+0 == 0 일 때만]  +0x372 LdrpComputeLazyDllPath(P)`

- **누가**: `LdrpSearchPath` (주소록을 펼쳐 한 줄씩 보는 담당).
- **어디서**: 돌아올 자리 `+0x372` (call 은 `+0x36d`).
- **누구를**: `LdrpComputeLazyDllPath`. 인자는 주소록 `P` 하나.
- **무엇을 위해**: "주소록 안쪽이 비었으니 지금 채워 줘."
- **언제만**: 바로 앞 `+0xe4 CMP [R12],RBP` / `+0xe8 JZ` 에서 `P+0` (목록 글자 칸) 이 **0 일 때만**. 0 이 아니면 이 줄을 건너뛴다 `[정적]`.
- 채우다 실패하면 (음수) 그 오류로 돌아가고, 성공하면 이어서 칸을 돈다 (`+0x376 JNS`).

#### 한 줄 읽기 ② — exe 폴더를 첫 칸에 넣는 줄

`├─ 칸 5: +0x55a RtlpGetDirPath(0)`

- **누가**: `RtlpComputePath` (목록 글자를 실제로 짓는 담당).
- **어디서**: 돌아올 자리 `+0x55a` (call 은 `+0x555`).
- **누구를**: `RtlpGetDirPath`. 인자 **0** = "부모 경로 따로 안 줌 → exe 경로를 써라".
- **무엇을 위해**: 칸 코드 5 ("응용 프로그램 폴더") 를 채운다. 우리 경우 그게 **exe 폴더**다.
- **언제만**: 지금 짓는 칸의 코드가 5 이고, 부모 경로 인자가 없을 때. 기본 순서 표에서 칸 5 는 **맨 앞**이다 (2.6).
- 같은 함수를 `+0x3fb` 에서도 부르는데, 그건 부모 경로를 줄 때다 — 우리 길이 아니다 `[정적]`.

### 2.3 핵심 ① 주소록은 미리 만들지 않는다 — `LdrpInitializeDllPath(0, 0, &P)` `[정적]`

**새 말 — 나중에 짓기 (lazy)**: 필요할지 모르는 것을 미리 만들지 않고, **처음 실제로 쓰는 순간에** 만드는 방식이다.

- 비유: 이사 오자마자 주소록을 다 쓰지 않는다. **빈 표지만** 꽂아 두고, 처음 누구 집을 찾아야 할 때 그때 페이지를 채운다.
- 코드에서 "아직 안 지음" 표시는 따로 깃발이 있는 게 아니라 **목록 칸 `P+0` 이 0 인 것** 그 자체다.

**하는 일 — 단계별로** (`LdrpInitializeDllPath`, rva `0x899a0`, 167바이트)

1. `+0x13 ~ +0x35` — `MOVUPS` 8번으로 `P` 0x80 바이트를 **전부 0 으로** 민다.
2. `+0x3a TEST DL,1` — 둘째 인자의 비트 0 을 본다.
3. 우리 경우 인자가 `(0, 0)` 이라: `+0x3d JZ +0x56` → `+0x56 TEST RDX,RDX` → `+0x59 JZ +0x3f` → `P+0x20 = 0` (`+0x42`), `P+0x18 = 0` (`+0x46`) → `+0x54 RET`.
4. **호출 없음, 할당 없음.** 목록 칸 `P+0` 은 0 그대로 = "아직 안 지음".

**다른 두 갈래** (우리 길 아님, 있다는 것만)

- 비트 0 이 켜짐 → 부모 경로·플래그만 적어 둔다. run26 의 "부모 `DependentLoadFlags` 가 겹칠 때 새 주소록" (`LdrpMapDllSearchPath +0x330`) 이 이 갈래다.
- 둘째 인자가 0 이 아니고 비트 0 이 꺼짐 → 밖에서 지어 온 글자를 그대로 `P+0` 에 받는다 (`+0x5b`). 누가 쓰는지는 shim·verifier 쪽일 것 `[추측]`, 범위 밖.

**왜 "나중" 이어야 하나 — 근거 → 그래서**

- 근거 1 `[정적]`: 첫 칸 exe 폴더는 **exe 명부 줄 (`LdrpImageEntry`)** 에서 읽는다 (2.5).
- 근거 2 `[정적]`: 그런데 `LdrpInitializeProcess` 는 `+0x1313` 에서 이 함수를 부른 **뒤에야** `+0x1359 LdrpAllocateModuleEntry` → `+0x1374` 에서 exe 명부 줄을 만든다.
- 그래서 `[정적]`: `+0x1313` 시점엔 **읽을 명부 줄이 아직 없다.** 그 자리에서 짓고 싶어도 못 짓는다.
- 비유: 주소록 첫 줄에 "우리 집 주소" 를 써야 하는데, 표지를 만들 때는 아직 **입주 신고(명부 줄)** 를 안 했다. 그러니 표지만 만들고 기다린다.

**정정 아닌 정정**

- run26·flow.md 2.6 의 "`+0x1313 LdrpInitializeDllPath` 로 **만든** 경로" 는 "**빈 표지를** 만든" 이 정확하다. 틀린 말은 아니라 이전 문서는 안 고쳤다 (run 문서 "정정").

**주소록 `P` 의 칸 — 대표만** `[정적]`

`P` 는 0x80 바이트짜리다. **PDB 에 타입이 없어서** 칸 이름은 없고, "어느 줄이 어느 칸을 쓰나" 로만 적는다 (run 문서). exe 의 `P` 는 `LdrpInitializeProcess` 스택 `[RSP+0x380]` 이다.

| 칸 | 뜻 | 쓰는 곳 |
|---|---|---|
| `+0x00` | **목록 글자** (`"폴더;폴더;…;"`). **0 이면 "아직 안 지음"** | `LdrpComputeLazyDllPath +0x91` |
| `+0x10` | 패키지 앱 폴더 목록. 보통 프로세스는 0 `[추측]` | `LdrpComputeLazyDllPath +0x94` |
| `+0x18` | 플래그 (부모 `DependentLoadFlags` 에서 온 값). `(0, 0)` 이면 0 | `LdrpInitializeDllPath +0x46` |
| `+0x20` | 부모 모듈 `FullDllName.Buffer`. `(0, 0)` 이면 0 | `LdrpInitializeDllPath +0x42` |
| `+0x7c` | 지음 끝 표시 (1) | `LdrpComputeLazyDllPath +0x8d` |

나머지 칸 (`+0x08`, `+0x28 ~ +0x78`) 은 run 문서 표.

### 2.4 핵심 ② 처음 펼칠 때 짓는다 — 다섯 담당 사슬 `[정적]`

요청서 짐작 사슬은 **맞았다.** 다만 글자를 **짓는** 곳은 그보다 **두 칸 더 아래**였다 (`RtlpComputeDllPath` → `RtlpComputePath`).

담당을 하나씩 내려가 본다. call 오프셋으로 적는다.

**① `LdrpSearchPath +0xe4` — "비었나?"**

- `P+0` 이 0 이면 `+0x36d LdrpComputeLazyDllPath(P)` (2.2 한 줄 읽기 ①).

**② `LdrpComputeLazyDllPath` — 열쇠 잡고, 한 번 더 확인**

- `+0x27` 에서 열쇠 `LdrpPathLock` 을 **혼자 쓰기**로 잡는다 (독점).
- `+0x2c CMP [RBX],RDI / +0x2f JNZ` — **열쇠 안에서 `P+0` 을 한 번 더** 본다. 이미 채워져 있으면 건너뛴다.
  - 왜 두 번 보나: 열쇠를 기다리는 사이에 **다른 스레드가 먼저 채웠을 수** 있어서일 것이다 `[추측]` (일반적으로 알려진 "잠그고 다시 확인" 방식. run24 의 일꾼 스레드가 같은 주소록을 동시에 펼칠 수 있다는 점과 맞는다).
- `+0x62 LdrpGetDllPath(…)` → 성공이면 `P+0` 등을 채우고 (`+0x84`·`+0x8d`·`+0x91`·`+0x94`), 로그 "DLL search path computed: %ws" (`+0xba`), `+0xc6` 열쇠를 푼다.

**③ `LdrpGetDllPath` — 어느 계산기를 쓸까**

- 플래그 `P+0x18` 로 고른다. **0 (우리 경우)** 이면 `+0xa8 RtlpGetCachedPath(&RtlpDllSearchPath, RtlpComputeDllPath, 0, 0)`.
- 플래그가 있으면 `…WithOptions` 짝을 쓴다 (`+0x141 ~ +0x192`). 우리 길 아님.

**④ `RtlpGetCachedPath` — 캐시 창구**

**새 말 — 캐시**: 한 번 만든 결과를 **보관해 두고**, 같은 걸 또 달라고 하면 새로 안 만들고 꺼내 주는 것이다.

- 비유: 관리실 책장에 꽂아 둔 **주소록 인쇄 원본.** 누가 달라 하면 원본이 있고 판이 같으면 그걸 주고, 없으면 새로 인쇄한다.
- 원본 자리 = 전역 `RtlpDllSearchPath` (`ntdll+0x1d3050`) `[정적]`.

하는 일 `[정적]`:

- 셋째·넷째 인자가 **둘 다 0 일 때만** 캐시를 쓴다 (`+0x27`·`+0x2c`). 우리 경우 0, 0.
- 원본이 있고 (`+0x43`), 패키지 판 `LdrpAppPackagesPathVersion` 이 같고 (`+0x54`), 환경 블록 판 (`ProcessParameters +0x3f8`) 이 같으면 (`+0x78`) → **참조 수만 올려서** 그대로 준다.
- 아니면 `+0xbe` 에서 **`RtlpComputeDllPath` 를 직접** 부른다.

**새 말 — 콜백**: 함수를 부를 때 "이걸로 계산해 줘" 하고 **다른 함수의 주소를 인자로** 건네는 것. [run24 해설 2.5](run24-work-queue-해설.md#25-일꾼-쪽--ldrpworkcallback-과-그-복사본) 의 "주소로 건네지는 함수" 와 같은 것이다.

- 여기선 `LdrpGetDllPath` 가 `RtlpComputeDllPath` 의 **주소**를 창구에 건넨다. 창구는 그 주소를 아는 값과 비교해 (`+0xb9 CMP RBP,RAX`) 같으면 **직접 call** 로 부른다 `[정적]`.
- 그래서 요청서가 정한 "depth 1" (직접 부르는 함수 한 단계) 로는 **짓는 곳까지 안 닿았다.** 결과서가 요청서 밖으로 세 번 더 뽑은 이유다.

**⑤ `RtlpComputeDllPath` — 순서 표에서 한 줄 고르기**

- SetDllDirectory 폴더가 있으면 (`+0x1b`, `+0x2e JNZ`) → 줄 3.
- 없으면 담아 둔 SafeDllSearchMode 값 (`ntdll+0x1d2380`) 을 보고, 0 이면 `+0x4b RtlpLookupCurDirSetting` 으로 레지스트리를 읽는다 → 줄 0 이나 줄 1 (2.6).
- 그리고 `+0x7a RtlpComputePath(그 줄, 5칸, …)`.

**⑥ `RtlpComputePath` — 실제로 글자를 짓는 곳** (rva `0xba7a0`, 1885바이트)

두 번 돈다 `[정적]`:

1. **1차** (`+0x7c ~ +0xd9`): 칸마다 **글자 바이트 수만** 센다.
   - 합이 `0xfffe` 를 넘으면 (`+0xe3 JA`) `0xc0000106` 으로 포기한다.
2. `+0xfb RtlAllocateHeap` — 센 크기만큼 **프로세스 힙**에서 자리를 빌린다.
3. **2차** (`+0x8f3 ~ +0x9a6`): 칸마다 글자를 **복사하고 `;` 를 붙인다.**

- 비유: 먼저 몇 쪽인지 세고 → 종이를 그만큼 사 오고 → 그다음에 옮겨 적는다.

**주소록 페이지는 "베낀 것" 이 아니라 "원본에 끼운 책갈피"** `[정적]`

- `P+0` 은 따로 복사한 글자가 아니다. 캐시 원본 블록의 `+0x80` 을 **가리킨다** (`LdrpGetDllPath +0xf0 LEA RCX,[RAX+0x80]` → `+0xf7`).
- 원본 블록은 `RtlpComputePath` 가 힙에 만든 것이고, `RtlpDllSearchPath` 가 가리킨다.

**이름이 비슷한 다른 창구** `[정적: 호출 관계]`

- `RtlGetExePath`·`RtlGetSearchPath` 도 **같은 창구** `RtlpGetCachedPath` 를 쓰지만, 원본 자리 (`RtlpExeSearchPath`·`RtlpSearchPath`) 와 계산기가 다르다 = **다른 주소록**이다.
- 로더의 DLL 찾기는 이 둘을 안 쓴다.

### 2.5 핵심 ③ 첫 칸 = exe 폴더 — `RtlpGetDirPath` `[정적]`

**어떤 값에서 오나 — 거꾸로 따라가기**

1. `RtlpGetDirPath +0x28 MOV RAX,[LdrpImageEntry]` — exe 명부 줄을 꺼낸다.
2. `+0x2f MOV RBX,[RAX+0x50]` — 그 줄의 `FullDllName.Buffer` (글자가 놓인 곳).
3. 그 `FullDllName` 은 어디서 왔나: `LdrpInitializeProcess +0x13aa MOVAPS XMM0,[RSP+0xf0]` → `+0x13b9 MOVDQU [RCX+0x48],XMM0` (`RCX` = exe 명부 줄).
4. `[RSP+0xf0]` 은 **`ProcessParameters.ImagePathName`** (exe 전체 경로) 이다 (sast/14).

→ `FullDllName` 은 16바이트 쪽지(`UNICODE_STRING`)의 **사본**이고, 글자 버퍼는 **개업 서류(`ProcessParameters`) 의 것을 그대로 가리킨다** `[정적]`.

- 그러니 exe 폴더의 출처는 `GetModuleFileName` 같은 다른 길이 아니라, 결국 **부모가 넘겨준 개업 서류의 exe 경로**다.

**어디서 자르나**

- `+0x33 MOV EDX,0x5c` (`\` 글자) → `+0x3b wcschr` (**첫** `\` 찾기) → `+0x50 wcsrchr` (**마지막** `\` 찾기).
- `+0x55 CMP` / `+0x58 LEA RCX,[RAX+2]` / `+0x5c CMOVNZ` → `+0x65 SUB RCX,RBX` → `+0x6b MOV [RDI],RCX` (길이 적기).
- 정리하면:
  - 첫 `\` 와 마지막 `\` 가 **다르면**: 길이 = 마지막 `\` 앞까지. 끝 `\` 는 빠진다.
  - **같으면** (`C:\test.exe` 처럼 루트): `\` 까지 포함.
- 예: `C:\a\bin\test.exe` → `C:\a\bin`.
- 끝의 `\` 는 여기서 안 붙인다. 나중에 `LdrpSearchPath +0x202 ~ +0x211` 이 후보를 만들 때 붙인다 (끝 글자가 `\`·`/` 가 아니면).

**run19 와 짝으로 보기**

- [run19 해설 2.1](run19-proc-init-modules-해설.md#21-run18-정정-하나--exe-폴더가-아니라-exe-파일-이름) 에서는 같은 `ImagePathName` 의 마지막 `\` **뒤**를 잘라 **파일 이름** (`BaseDllName` = test.exe) 을 만들었다.
- 이번엔 같은 경로의 마지막 `\` **앞**을 잘라 **폴더**를 만든다.
- 비유: 같은 주소 쪽지 한 장에서, run19 는 **건물 이름표**를 오렸고 이번엔 **동네 이름**을 오린다. run18 해설이 `LdrpInitializeProcess` 안에서 찾았다고 잘못 읽었던 "동네 이름 오리기" 는, 이번에 **`RtlpGetDirPath` (첫 DLL 검색 때)** 에서 찾았다.

**곁가지 하나** `[정적]` + `[추측]`

- `+0x1a ~ +0x26` 에서 TEB `GS:[0x18]` (`NtTib.SubSystemTib`) 가 0 이 아니면 다른 칸에서 경로를 읽는 갈래가 있다. 보통 Win32 프로세스는 0 일 것 `[추측]`.

### 2.6 핵심 ④ 순서 — 표의 한 줄 = 칸 코드 다섯 개 `[정적]`

**순서는 코드가 아니라 표에 있다**

- `RtlpComputeDllPath +0x65 LEA RAX,[ntdll+0x17e340]` — ntdll 안의 **순서 표**를 가리킨다.
- 표 한 줄 = 숫자(칸 코드) 다섯 개. `RtlpComputePath` 는 그 숫자를 하나씩 보고 "이 칸엔 무엇을 넣지" 를 정한다.
- 비유: 주소록 **목차 양식**이 몇 장 인쇄돼 있고, 관리실은 그날 설정을 보고 **양식 한 장**을 골라 그 순서대로 채운다.

**양식 네 장** (run 문서 표 그대로. 표 값은 파일 값)

| 줄 | 칸 코드 | 뜻 | 언제 |
|---|---|---|---|
| 0 | `5 4 2 3 8` | exe 폴더, **현재 폴더**, 시스템, PATH, 패키지 | SafeDllSearchMode = 0 |
| 1 | `5 2 4 3 8` | exe 폴더, 시스템, **현재 폴더**, PATH, 패키지 | SafeDllSearchMode = 1 (**기본**) |
| 2 | `1 2 3 8 9` | — | `RtlpComputeDllPath` 로는 못 닿음 |
| 3 | `5 0 2 3 8` | exe 폴더, **SetDllDirectory 폴더**, 시스템, PATH, 패키지 (현재 폴더 빠짐) | SetDllDirectory 버퍼가 있을 때 |

칸 코드 중 우리 길에 나오는 것만 (전체는 run 문서):

| 코드 | 넣는 것 |
|---|---|
| 5 | 응용 프로그램 폴더 — 부모 경로 인자가 없으면 **exe 폴더** (`RtlpGetDirPath(0)`) |
| 2 | **`RtlpSystemDirs`** 한 덩어리 = `<System32>;<Windows>\system;<Windows>;` |
| 4 | 글자 `.` (현재 폴더) |
| 3 | 환경 변수 **`PATH`** (없으면 빈 칸) |
| 8 | 패키지 앱 폴더 (길이 0 이면 건너뜀) |

- **새 말 — 환경 변수 `PATH`**: 프로세스가 들고 다니는 "이름 = 값" 설정 중, 프로그램을 찾아볼 폴더들을 `;` 로 이어 적은 값으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 코드는 `RtlQueryEnvironmentVariable("PATH")` 로 읽는다 `[정적]`.

**그래서 기본 목록은**

```
exe 폴더 ; <System32> ; <Windows>\system ; <Windows> ; . ; (PATH 값) ; (패키지: 보통 빈칸)
 [칸 5]    └──────── 칸 2 (RtlpSystemDirs 한 덩어리) ─────────┘  [칸 4] [칸 3]   [칸 8]
```

- 이건 **코드가 정한 칸 순서**다 `[정적]`. 실제 글자 (드라이브, PATH 값) 는 동적에서 본다.
- **세 줄(0·1·3) 모두 첫 칸은 exe 폴더** (첫 코드 5) 다. 설정이 어떻든 exe 폴더가 맨 앞이다 `[정적]`.

**새 말 — SafeDllSearchMode**: 위 표에서 보듯 **현재 폴더 `.` 를 둘째로 둘지, 시스템 폴더 뒤로 미룰지** 정하는 설정 값이다 `[정적]`.

- 왜 미루나: 지금 작업 중인 폴더에 같은 이름의 가짜 DLL 이 있어도 시스템 것이 먼저 잡히게 하려는 안전 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 읽는 법 `[정적]` (`RtlpLookupCurDirSetting`):
  - 보안 프로세스면 그냥 1.
  - 아니면 설정 장부(레지스트리) `…\Control\Session Manager` 의 `SafeDllSearchMode` 를 읽는다 (`+0x5f NtOpenKey`, `+0xa1 NtQueryValueKey`).
  - 값이 0 이나 1 이면 그 값, **없거나 이상하면 1** (`+0xe3 ~ +0xef`).
  - 한 번 읽으면 `ntdll+0x1d2380` 에 담아 둔다. 다음부턴 장부를 다시 안 연다.
- 그래서 특별히 값을 안 넣었다면 **줄 1 (기본)** 일 것이다.

**시스템 폴더 덩어리는 어디서 오나** `[정적]`

- `LdrpInitializeProcess +0x1065 ~ +0x1161` 에서 미리 짓는다. `+0x109e` 에서 `RtlpSystem32Dirs`·`LdrpSystem32`, `+0x1161` 에서 **`RtlpSystemDirs`**.
- 조건부 덧붙임·빼기 (`ProcessParameters.Flags & 0x20000000` 이면 `forwarders` 폴더가 붙음, `LdrpPolicyBits & 1` 이면 Windows 두 조각이 빠짐) 가 있다. 보통 어떤지는 동적.
- `<Windows>` 글자의 정확한 출처 줄은 안 봤다 `[추측: NtSystemRoot]`.

**안 탈 것 둘**

- **시스템 폴더 대체 모드**: `RtlpComputePath +0x21 / +0x2b / +0x32` 가 `LdrSystemDllInitBlock+0xa0` (run32 의 **ntdll 수첩**) 비트 60~61 을 보고, 1 이면 **목록 전체를 시스템 폴더 하나로** 바꾼다 `[정적]`. 보통 0 일 것 `[추측]`.
- 줄 2 는 `EBX` (줄 번호) 가 0·1 뿐이라 이 계산기로는 못 닿는다 `[정적]`.

### 2.7 핵심 ⑤ 위에서부터 한 줄씩, 처음 "있어요" 에서 멈춘다 — `LdrpSearchPath` `[정적]`

**한 바퀴 — 순서대로**

1. **어디서 시작하나**:
   - "이어하기 표시" (인자 `R13`) 첫 칸이 0 이 아니면 → **지난번 멈춘 칸 다음**부터 (`+0xfc`).
   - 아니면 패키지 목록 `P+0x10` 이 있으면 그것부터 (`+0x102`), 없으면 `P+0` (`+0x10c`).
2. `+0x113 ~ +0x157` 가장 긴 칸 길이를 구해 `+0x172` 에서 후보 버퍼를 **한 번** 잡는다.
3. **칸 고리** (`+0x1b1`):
   - 빈 칸은 건너뛴다 (`+0x1c6`).
   - `;` 까지 복사하고, 끝에 `\` 를 붙이고 (`+0x202 ~ +0x211`), `+0x225` 에서 이름을 붙인다 → `<폴더>\dll1.dll`.
   - **`+0x244 LdrpResolveDllName(후보)`** — "이 파일 있나?"
4. **대답 보고 갈림**:
   - **0 이상 → 멈춘다** (`+0x256 JNS +0x282`).
   - `0xc0000135`·`0xc0000022`·`0xc0000043`·`0xc000006d` → **다음 칸** (`+0x26d JMP +0x1b1`).
   - 그 밖 음수 → 그 자리에서 그 오류로 끝 (`+0x4ea`).
   - 목록 끝까지 없음 → **`+0x27d` `0xc0000135`**.

- 비유: 주소록을 위에서부터 짚어 가며 창구에 "**`C:\a\bin` 동네에 `dll1.dll` 건물 있어요?**" 하고 묻는다. "있어요" 한 번이면 주소록을 덮는다. "없어요" 면 다음 줄. 끝까지 "없어요" 면 "못 찾았습니다(`0xc0000135`)".

**"파일 있나" 는 누가 묻나 — 실행 순서 주의**

- `LdrpResolveDllName` 안에서, 코드 순서로는 `+0x92 TEST …,0x200` → `+0x9d JZ +0x292` → **`+0x298 LdrpGetFullPath`** (전체 경로 만들기) → … → **`+0x13b LdrpGetNtPathFromDosPath`** 다.
- 오프셋은 뒤가 **작지만** `GetFullPath` 가 **먼저**다 (run 문서가 `.asm` 으로 확인). run26 해설 2.5 의 "주소 순서 ≠ 실행 순서" 와 같은 함정이다.
- `LdrpGetNtPathFromDosPath` 가 `+0x6c` 에서 NT 경로로 바꾸고, **`+0xee NtQueryAttributesFile`** 로 창구에 묻는다 (ret `+0xf3`).
  - **새 말 — `NtQueryAttributesFile`**: "이 경로에 파일이 있나" 를 묻는 시스템 콜이다 (run 문서: "파일 있나 — 시스템 콜"). 파일을 열지 않고 속성만 알려 주는 창구로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 비유: 건물에 들어가지 않고 **창구에 "그 번지에 건물 있어요?" 만 묻기.** 실제로 여는 건 다음 걸음 `NtOpenFile` (run26 걸음 2) 이다.
  - DOS 경로 (`C:\…` 꼴) 를 관리사무소가 알아듣는 NT 경로 꼴로 바꾼 뒤 묻는 것으로 보인다 `[추측: 함수 이름]`.
- "없음" 계열 상태 9개 (`0xc000000f`·`0xc0000034`·`0xc000003a` 등) 는 `+0x28b` 에서 `0xc0000135` 로 **하나로 모아** 돌려준다. 그래서 `LdrpSearchPath` 는 "다음 칸으로 갈지" 를 몇 개 값으로만 판단하면 된다.

**run26 이 남긴 질문 — "다시 찾을 때 어디부터?"**

- run26: 파일 담당 결과가 `0x4000000e` (기계 종류 불일치) 면 `LdrpSearchPath` 부터 **다시** 찾는다.
- 이번 답 `[정적]`: `0x4000000e` 같은 **경고값도 "0 이상" 이라 성공 취급**해서 멈춘다. 멈추기 전에 `+0x2b7`·`+0x2bb` 에서 이어하기 표시에 "**이 목록 / 찾은 칸 다음**" 을 적는다.
- 그래서 다시 부르면 1번의 `+0xfc` 갈래로 **찾은 칸 다음부터** 이어서 찾는다.
- 비유: 찾아간 건물이 맞는 종류가 아니면, 주소록의 **그 줄에 손가락을 끼워 둔 채** 돌아와서 **다음 줄부터** 다시 찾는다.

**이 함수는 "맨 이름" 일 때만** `[정적: 앞선 결과]`

- `LdrpSearchPath` 안엔 이름에 `\` 가 있는지 보는 줄이 없다 (`0x5c` 비교는 `+0x202` 뿐).
- 경로가 붙은 이름은 부르는 쪽에서 다른 길로 간다 (sast/18 flags `0x400`, sast/19 `LdrpMapDllFullPath`). 그래서 주소록은 `dll1.dll` 처럼 **이름만** 받았을 때 펼친다.

**우리 길에 안 끼는 것** `[정적]`

- `+0x4f7 RtlpLookupSafeCurDirList` ("현재 폴더에서 받아도 되는 목록" 검사) 는 "지금 칸 == `P+8` (`.` 칸)" 일 때만 간다. 그런데 `P+8` 은 플래그 `0x2000` 일 때만 채워지고, 우리 플래그는 0 이다. → test.exe 길에선 안 탄다.
- 이 함수를 부르는 곳은 찾기 담당 말고도 `LdrpFindLoadedDllInternal +0x226`·`LdrpFindLoadedDll +0x2f5` 가 있다 (이미 올라온 모듈 찾기 — 범위 밖, 있다는 것만).

### 2.8 핵심 ⑥ dll1·dll2 — 주소록 한 권, 첫 칸에서 찾힐 것

#### (가) 주소록은 한 권을 같이 쓴다 `[정적]` (조건 하나는 `[추측]`)

- 근거 1 `[정적]`: 자식 지시서를 만들 때 부모 지시서의 `+0x10` 을 **그대로 옮겨 적는다** — `LdrpLoadDependentModuleInternal +0x959 MOV RAX,[RAX+0x10]` (부모) → `+0x95d [RSP+0xf0]` → `+0x10f8 MOV [RBX+0x10],RAX` (새 지시서).
- 근거 2 `[정적]`: 찾기 담당은 부모 명부 줄 `DependentLoadFlags` (`+0x118`) 가 마스크 `0x7b00` (정책에 따라 `0x7f00`) 와 **안 겹치면** `+0xe1 MOV R14,[RBX+0x10]` — 지시서가 들고 온 `P` 를 그대로 쓴다.
- 근거 3 `[추측: 링크 옵션]`: `build.bat` 에 `/DEPENDENTLOADFLAG` 가 없어서 test.exe·dll1 명부 줄의 `+0x118` 은 0 일 것. (그 칸에 값을 넣는 줄은 이번에 못 찾았다.)
- 그래서: exe → dll1 → dll2 지시서의 `+0x10` 이 모두 `LdrpInitializeProcess` 스택의 **같은 `&P`** 다.
- 그러니 **목록 글자는 한 번만 짓는다** `[정적]`. 처음 `LdrpSearchPath` 에서 `P+0` 이 채워지면, 그 뒤 호출은 `+0xe4` 에서 짓기를 건너뛴다.
- 비유: exe·dll1·dll2 지시서에 "주소록은 **관리실 그 서랍**" 이라고 같은 위치가 적혀 있다. 처음 꺼낸 사람이 페이지를 채우면, 다음 사람은 **채워진 주소록**을 그냥 편다.

- 아직 모르는 것: 그 "처음" 이 **dll1 을 찾을 때**인지, 이 함수를 부르는 다른 담당 (`LdrpFindLoadedDll*`) 쪽이 먼저인지 → 동적.

#### (나) dll1·dll2 는 첫 칸에서 찾힐 것 — 왜 그렇게 짐작하나

- 근거 1 `[정적]`: 기본 목록의 맨 앞은 SafeDllSearchMode 가 0 이든 1 이든, SetDllDirectory 가 있든 없든 **exe 폴더**다 (줄 0·1·3 첫 코드 5).
- 근거 2 `[소스]`: `sample/build.bat` 가 dll2·dll1·test.exe 를 **모두 `bin\` 한 폴더**에 만든다.
- 그래서 `[추측: 위 둘 + 실행 파일이 그 폴더에서 돈다고 칠 때]`: 첫 후보 `<bin>\dll1.dll` 에서 `NtQueryAttributesFile` 이 성공 → `+0x256 JNS` 로 바로 나간다. `+0x244` 는 **DLL 하나당 한 번**. dll2 도 같다.
- 순서 표가 줄 0 이냐 1 이냐로 갈리는 건 **둘째 칸부터**라, 이번 sample 에선 상관없다.
- 아직 확인 아님: 실제로 몇 번 묻고 몇째 칸에서 멈췄는지는 디버거로 봐야 한다 (4절, Q23).

### 2.9 곁가지 — 시스템 폴더 글자를 쓰는 둘째 곳 `[정적: refs 줄]`

- 결과서의 `RtlpSystemDirs` 참조 목록에 `LdrpInitializeProcess +0x1161` 말고 **`LdrpInitializePerUserWindowsDirectory +0x157`** 도 쓰는 줄로 나온다. `RtlpSystem32Dirs` 쪽도 같은 함수 `+0x164` 에 버퍼 칸 쓰기가 있다.
- 그 함수는 `LdrpInitializeProcess +0x1b72` (ret `+0x1b77`) 에서 불린다 — dll1·dll2 올리기 (`+0x1ab1`) **뒤**, 그래프 켜기 (`+0x1bb6`) **앞**.
- 무엇을 바꾸는지, test.exe 에서 타는지, 그 뒤 캐시 원본이 다시 지어지는지는 모른다 `[공백]`.
- 캐시를 새로 짓는 조건 (`RtlpGetCachedPath +0x54`·`+0x78`) 에 `RtlpSystemDirs` 는 없다 `[정적]`.
  - 그래서 dll1·dll2 검색엔 영향이 없을 것이고, 뒤에 오는 `LoadLibraryExW` ×3 (run7) 검색에는 영향이 있을 수 있다 `[추측]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 그림 2 `LdrpSearchPath [dll1 추정]` 상자 글 | "검색 경로 = 보통 문맥 +0x10. 못 찾으면 파일 안 열고 오류. **exe 폴더 자리는 안쪽 Q53**" | `P` = exe 문맥과 같은 것, `;` 로 한 칸씩, 처음 찾은 칸에서 멈춤, 첫 칸 = exe 폴더라 첫 후보에서 찾을 것 — **추측**, 끝까지 없으면 `0xc0000135` |
| 그림 2 dll1 상자 아래 | 없음 | `(정적)` 상자 **일곱** (목록 짓기 사슬, `(1) +0x372` 부터 `RtlpGetDirPath` 까지) + **셋** (`(2) +0x249` 후보 확인 → `NtQueryAttributesFile`) |
| 그림 2 dll2 상자 | "(정적)" 만 | "같은 코드, 같은 `P`, 짓기는 건너뜀 — `P` 공유 추측" + 후보 확인 상자 셋. **짓기 상자는 안 그렸다** (한 번만 짓기 때문) |
| 2.4 트리 `+0x1318` | `LdrpInitializeDllPath [뒤1]` | "`(0, 0, &P)` — `P` 를 0 으로만, 목록은 첫 DLL 검색 때" |
| 2.4 트리 | 없음 | `+0x1b77 LdrpInitializePerUserWindowsDirectory` (뜻 모름) |
| 2.4 앞쪽 표 9 | 시스템 경로 (`LdrpSystem32`) | + `RtlpSystem32Dirs`·**`RtlpSystemDirs`** = 검색 목록의 시스템 폴더 덩어리 |
| 2.6 | 검색 경로 절 없음 | 새 절 "검색 경로" — 트리, 나중에 짓기, `P` 칸, 짓기 사슬, exe 폴더, 순서 표, 도는 법, dll1·dll2, 곁가지 |
| 4장 2.6 "exe 폴더 자리 → Q53" 줄 | 열림 | **(해결됨, 정적)** + 새 줄 둘 (실행 값 → Q23, 작은 정적 → Q71) |
| 4.1 표 | — | 그림 2 `(1)~(2) 사이` 새 줄 둘 (짓기 사슬 상자 일곱, 후보 확인 상자 여섯) |
| 기준 절 | — | 순서 표·문자열 상수는 원본 바이트로 읽었다 (고르는 줄은 `[정적]`, 표 값은 파일 값) |

**태그가 어떻게 바뀌었나**

- "dll1·dll2 는 exe 폴더에서 `LdrpSearchPath` 로 찾음" — 전에는 근거 없는 `[추측]`. 이제는 **코드 순서 `[정적]` + 빌드 폴더 `[소스]`** 라는 근거가 붙었다. 다만 실행으로 본 게 아니라 **여전히 `[추측]`** 이다.
- "검색 경로에 exe 폴더가 들어가는 자리" — 전에는 `[공백]` (세 함수 안에 없음). 이제 `RtlpComputePath +0x555 RtlpGetDirPath` `[정적]`.
- `LdrpInitializeDllPath` 를 그림에 **안 넣은** 이유 (flow.md): 호출이 없는 짧은 함수이고, 넣으면 `LdrpInitializeProcess` 아래 호출 번호가 다 밀린다.

### 큰 그림에서 어떻게 이어지나

dll1 하나를 따라가면 이제 이렇게 이어진다:

1. 배달 기사가 지시서를 만든다. `+0x10` 칸에 exe 의 주소록 위치를 그대로 옮겨 적는다 (run23 + 이번).
2. 일감으로 찾기 담당에 닿는다 (run24).
3. **찾기 담당 → `LdrpSearchPath`**: 주소록이 비었으면 지금 채우고 (첫 칸 exe 폴더), 위에서부터 묻는다 → `<exe 폴더>\dll1.dll` 에서 "있어요" `[추측]` (이번).
4. 그 경로로 파일을 열고 (`NtOpenFile`), 세트를 짜고, 땅에 놓고, 명부에 적는다 (run26·run38).
5. dll1 의 import 를 펼쳐 dll2 지시서를 만들 때 **같은 주소록 위치**가 넘어간다 → dll2 는 짓기 없이 바로 묻는다 (이번).
6. 주소 잇기 (run36) → 켜기 (run27).

- 이걸로 "**이름만 적힌 `dll1.dll` 이 어떻게 실제 파일 경로가 되나**" 의 빈 칸이 코드로 채워졌다.
- 남은 건 실제 값 — 주소록에 정말 무슨 글자가 적혔나, 정말 첫 줄에서 멈췄나 — 이고, 이건 디버거 몫이다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **실제로 지어진 목록 글자** (기대 `<exe 폴더>;<System32>;<Windows>\system;<Windows>;.;<PATH>;`) | 드라이브·폴더·PATH 값은 실행 때 정해진다 | 동적 **Q23** — `ntdll+0xb9908` (`LdrpComputeLazyDllPath +0x98`) 에서 `du @rcx` |
| **처음 짓는 호출이 누구인가** (dll1 찾기인가 `LdrpFindLoadedDll*` 인가), dll1·dll2 길에서 **많아야 한 번** 서나 | 누가 먼저 부르는지는 실행 순서 | 동적 **Q23** — `ntdll+0xb9870` 입구 `k`. dll1·dll2 둘 다에서 서면 `P` 가 갈라진 것 |
| 고른 순서 표 줄 (1 기대), exe 폴더 글자 | SafeDllSearchMode 레지스트리 값·exe 경로는 실행 값 | 동적 **Q23** — `ntdll+0xba5be` `ebx`, `ntdll+0xb96a3` `du @rax L(@rcx/2)` |
| dll1·dll2 가 **첫 후보에서** 찾히나, 몇 번 묻나 | 정적으로는 순서만 안다. 파일이 정말 그 폴더에 있는지는 실행 환경 | 동적 **Q23** — `ntdll+0x90f4` `dS @rcx`, `ntdll+0x90f9` `r eax` (둘 다 한 번에 0 기대) |
| 부모 `DependentLoadFlags` 가 정말 0 인가 (= `P` 공유) | 그 칸을 쓰는 줄을 못 찾았다. 근거가 링크 옵션뿐 | 동적 **Q23** (`LdrpMapDllSearchPath +0xd8` `[부모+0x118]`) · 정적 **Q71 (c)** |
| `LdrpInitializePerUserWindowsDirectory` 가 무엇을 바꾸나, test.exe 에서 타나, 그 뒤 캐시가 다시 지어지나 | 결과서 범위 밖. 참조 목록 줄로만 봤다 | 정적 **Q71 (a)** |
| `LdrSystemDllInitBlock+0xa0` 비트 60~61 의 뜻 (1 이면 목록이 시스템 폴더 하나) | ntdll PDB 에 타입이 없다. 쓰는 쪽은 커널 | 정적 **Q71 (b)** (run32 `PspPrepareSystemDllInitBlock` 과 짝) · 실행 값은 Q23 |
| `<Windows>` 글자 출처 줄, `ProcessParameters.Flags & 0x20000000` (forwarders 폴더) 이 run32 비트 29 와 같은가 | 이번에 그 줄을 안 읽었다 | 정적 **Q71 (d)** |
| `RtlpComputeDllPathWithOptions` 안, `P+0x28 ~ +0x77` 사본을 읽는 곳, `.` 칸이 PEB `CurrentDirectory` 로 풀리나 | 우리 길에 안 타거나 (플래그 0, 첫 칸에서 끝) 영향이 없다 | 번호 없이 남김 (Q71 "작은 것") |
| `LoadLibraryExW` ×3 (run7) 은 어떤 주소록을 쓰나 | 이번 범위는 dll1·dll2 | `[추측]` 다른 구조체일 수 있다 — dast 때 따로 셀 것 (run 문서 멈춤점 메모) |

- 동적 멈춤점은 모두 **ntdll 유저 주소**라 **test.exe 조건이 꼭 필요**하다. 그리고 `LoadLibraryExW` 때도 서니까 `LdrpSearchPath` 입구에서 이름(`dS @rcx`)을 같이 찍어 가려 읽는다 (run 문서 "멈춤점 메모").

---

## 5. 더 읽기

- run 문서: [`run39-dll-search-path.md`](run39-dll-search-path.md) — `P` 칸 전체 표, 칸 코드 0~8 전체, 호출 지점 표 전체, 동적 멈춤점 표
- 결과서: [`_workspace/sast/34_dll-search-path_result.md`](../../_workspace/sast/34_dll-search-path_result.md) — 캐시 원본 블록 칸, `RtlpComputePath` 점프 표
- 요청서: [`_workspace/sast/34_dll-search-path.md`](../../_workspace/sast/34_dll-search-path.md)
- flow.md: [2.6 "검색 경로" 절](flow.md) · 2.4 트리 `+0x1318`·`+0x1b77`, 앞쪽 표 9 · 그림 2 `(1)~(2) 사이` · 4장 2.6 run39 줄 · 4.1 표
- 앞 해설: [run26](run26-map-dll-file-해설.md) (찾기 담당, 걸음 1) · [run19](run19-proc-init-modules-해설.md) (exe 명부 줄) · [run38](run38-map-module-해설.md) (그다음 — 땅에 놓기)

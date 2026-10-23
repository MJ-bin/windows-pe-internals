# run38 map-module — 해설

> 이 글은 [run38 문서](run38-map-module.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q52** — 2.6 실제로 메모리에 붙이고 마무리하는 쪽: `ntdll!LdrpMinimalMapModule`, `LdrpCompleteMapModule` (재배치 후보) (`_workspace/plan.md`, 이번에 **끝**)
  - 새 항목: **Q70** (정적, 작은 것 둘 — `ctx+0x20` 비트를 켜는 줄, `0x40000036` 의 이름).
  - 실행 때 확인할 거리는 새 번호 없이 **Q20** (kernel32·kernelbase) · **Q23** (dll1·dll2) 에 출처로 붙었다.
  - 다음 항목: **Q53** (DLL 검색 경로, 정적).
- 근거:
  - run 문서 [`run38-map-module.md`](run38-map-module.md) (가장 중요)
  - 요청서 [`_workspace/sast/33_map-module.md`](../../_workspace/sast/33_map-module.md)
  - 결과서 [`_workspace/sast/33_map-module_result.md`](../../_workspace/sast/33_map-module_result.md) (판정: **분석성공**)
    - 결과서 맨 위에 **정정 줄**이 있다. 점프 목적지 몇 곳을 "주소 끝 세 자리" 에서 **함수 오프셋**으로 고쳤다 (예: `+0x774` → `+0x1b4`). 이 글의 오프셋은 정정된 값 = run 문서 값이다.
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`. flow.md 기준과 같은 분석본이다.
  - 본문을 읽은 함수: `LdrpMinimalMapModule`, `LdrpCompleteMapModule` (둘 다 이미 뽑아 둔 것), `LdrpRelocateImage`, `LdrpProcessMachineMismatch` (새로 뽑음). `LdrpProtectAndRelocateImage` 는 요청서 밖에서 하나 더 뽑아 **이름·호출 자리만** 봤다.
  - 칸 이름은 PDB 타입이다. 단 로드 문맥(`_LDRP_LOAD_CONTEXT`)은 PDB 에 칸 정보가 없어서 **오프셋 + 이번에 읽은 역할**만 있다.

### 태그 셋 — 이번에 헤더 값을 어떻게 적나

- 코드 줄은 `[정적]`.
- 우리가 빌드한 **sample 파일**(`sample/bin/` 의 test.exe·dll1·dll2) 헤더 값은 `[정적+샘플]` ([run36 해설](run36-snap-module-해설.md) 에서 처음 씀).
- **MS 파일**(kernel32) 헤더 값은 `[정적: PE 표]` ([run37 해설](run37-k32-import-order-해설.md) 맨 위).
- 둘 다 "파일에 적힌 값" 이지 실행을 본 건 아니다. 그리고 sample 파일이 타깃 VM 에서 실제로 돈 파일과 같은지는 확인 안 했다 (run 문서).

---

이 글은 [run26 해설](run26-map-dll-file-해설.md) 2.3 **걸음 4 "땅에 갖다 놓기"** 의 안쪽 이야기다.

- run26 은 설치 담당 `LdrpMapDllWithSectionHandle` 이 **누구를 어떤 순서로** 부르는지까지 봤다.
- 그런데 첫 칸 (1) `LdrpMinimalMapModule` (갖다 놓기) 과 넷째 칸 (4) `LdrpCompleteMapModule` (마무리) **안은 안 열었다.**
- 그래서 "**재배치는 누가 하나**" 가 `[추측]` 후보 둘로 남았다 ([run26 해설 4절](run26-map-dll-file-해설.md#4-아직-모르는-것)).
- 이번에 그 두 칸을 열었다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](run9-create-user-process-해설.md) 2절 · rva → [run10 해설](run10-allocate-process-해설.md) 2절
- 이미지 섹션 (**조립식 건물 세트**) → [run9 해설 "이미지 섹션"](run9-create-user-process-해설.md#이미지-섹션)
- 매핑 = 세트를 땅에 **갖다 놓기** → [run10 해설 "매핑 올리기"](run10-allocate-process-해설.md#매핑-올리기)
- 파일 길 다섯 걸음, 지시서(로드 문맥) 칸 → [run26 해설 2.3·2.8](run26-map-dll-file-해설.md#23-핵심--파일에서-메모리까지--다섯-걸음)
- ASLR, 델타, 커널 쪽 `MiRelocateImage` → [run31 해설 2.10](run31-image-section-해설.md#210-재배치-델타--다른-번지에-놓을-거면-그-칸을-고쳐-쓴다)
- exe 재배치 갈림 (`LdrpInitializeProcess +0x1419 ~ +0x1430`) → [run19 해설 2.6 의 5번](run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1)
- `CorILOnly` (.NET 중간 코드만 든 모듈 표시) → [run22 해설 2.5](run22-map-snap-dep-해설.md#25-핵심--같은-함수가-올라온-dll-마다-다시-불린다)
- ETW (**관리실 작업 일지**) → [run20 해설 2.11](run20-kernel32-load-해설.md#211-ldrploaddll--ldrploaddllinternal--넘기는-9개와-상태-칸)

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = 관리사무소의 **신청 창구**
- 프로세스 = **가게**, 주소 공간 = 가게의 **빈 땅**, ntdll = 가게 안 **관리실**
- DLL = **건물**, 이미지 섹션 = **조립식 건물 세트**
- 모듈 항목(`_LDR_DATA_TABLE_ENTRY`) = **명부 한 줄**, 로드 문맥(`ctx`) = 건물 하나 들일 때 들고 다니는 **지시서** (run26)
- **붙이기(매핑)** = 세트를 땅에 **갖다 놓기**.
  - 대화에서는 "**가구를 집에 들여놓기**" 라고 설명했다. 같은 일이다.
- **재배치** = 원래 자리가 아닌 곳에 놓였을 때, 안에 적힌 **주소(번지수)를 고쳐 쓰기**.
  - run26·run31 해설의 "다른 번지에 놓였으니 안의 번지수를 고쳐 쓰기" 와 같은 그림이다.
- **`0x40000003`** = 창구 직원의 대답 "**놓았어요. 그런데 원래 자리가 아닌 곳에 놓였어요.**"

run 문서는 "이사" 비유를 썼다. 이 글은 위 그림으로 옮긴다 (가리키는 건 같다).

| run 문서 (이사) | 이 글 |
|---|---|
| 짐 꾸러미 (섹션) | 조립식 건물 세트 |
| 관리실 (커널) | **관리사무소** (이 시리즈에서 "관리실" 은 ntdll 이라 겹치지 않게 바꿨다) |
| 집 장부의 `DllBase` 칸 | 명부 줄의 `DllBase` 칸 |
| 방 번호 | 번지 |
| 주소 쪽지를 새 방 번호로 고치기 | 번지수 고쳐 쓰기 (재배치) |

한 줄로 먼저 말하면:

- 갖다 놓는 손 `LdrpMinimalMapModule` 은 관리사무소 창구 **`NtMapViewOfSection` 하나**로 세트를 땅에 놓는다 (`+0x174`, ret `+0x179`) `[정적]`. 놓인 번지는 **관리사무소가 명부 줄 `DllBase` 칸에 직접** 적는다 `[정적]`.
- 번지수 고쳐 쓰기(재배치)는 마무리 담당 **`LdrpCompleteMapModule` 이 맞다** — `+0x31b LdrpRelocateImage` `[정적]`. run26 의 후보 둘 중 **첫째**.
- 다만 문이 좁다. **창구 대답이 정확히 `0x40000003`** 일 때만 고친다. 대답이 `0` 이면 안 고친다 `[정적]`.
- 그 대답을 무엇으로 줄지는 **관리사무소(커널) 안쪽** 일이라 ntdll 코드로는 못 정한다. 그래서 "보통 DLL 을 ntdll 이 실제로 고치나" 는 아직 `[추측]` 이고, 동적(Q20·Q23)으로 본다.
- exe 는 이 길을 안 탄다. exe 고쳐 쓰기는 `LdrpInitializeProcess +0x1430` 이 **다른 문**으로 한다 `[정적]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

숫자는 앞 run 들 그대로다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessW … NtCreateUserProcess … NtResumeThread         (run9 ~ run14, run34)
      ├─ +0x93c MmCreateSpecialImageSection → … MiCreateNewSection +0x3da MiRelocateImage
      │         (exe 세트 짜기. 새 기본 주소를 골랐을 때만 — 커널 쪽 번지 고치기)   (run31)
      └─ … exe 세트를 땅에 놓기 (MiMapProcessExecutable)                           (run10)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess                               (run17 ~ run19)
        ├─ +0x1430 (조건부) exe 번지 고치기 LdrpProtectAndRelocateImage           ← 이번 (질문 4)
        ├─ +0x19a2 LdrpInitializeKernel32Functions → kernel32·kernelbase 들이기   (run20 ~ run25, run37)
        └─ … dll1·dll2 들이기                                                      (run22 ~ run26, run36)
              (네 DLL 모두 같은 설치 담당 LdrpMapDllWithSectionHandle 을 지난다 — run21·run26)
               ├─ (1) +0x4e   LdrpMinimalMapModule      땅에 갖다 놓기             ← 이번 (질문 1)
               ├─ (2)(3)      명부·색인에 적기
               ├─ (4) +0x24f  LdrpCompleteMapModule     마무리                     ← 이번 (질문 2·3)
               ├─ (5) +0x26c  LdrpProcessMappedModule
               └─ (6) +0x1c8  LdrpMapAndSnapDependency → … 주소 잇기 (IAT, run36)
   └─ NtContinue ═▶ RtlUserThreadStart → … → main                                  (run16, run29·run30)
```

- 위치는 **DLL 하나를 들일 때마다** 도는 "설치 담당" 안이다. kernel32·kernelbase·dll1·dll2 가 다 여기를 지난다 (run 문서 "호출 관계").
- flow.md 로는 **2.6** 의 "파일로 올리는 길" 절과 그림 2 의 `(1)~(2) 사이` 상자들이다. exe 쪽은 **2.4**, 커널 쪽 짝은 **2.1**.

### 무엇이 궁금했나

run 문서 "목적" 의 네 질문이다.

1. `LdrpMinimalMapModule` 은 **어떤 시스템 콜로** DLL 을 땅에 놓나. 무엇을 건네나. 명부 줄의 `DllBase`·`SizeOfImage` 는 누가 채우나. 창구 대답(결과 코드)을 어떻게 다루나.
2. `LdrpCompleteMapModule` 이 **재배치 담당인가.** 언제 부르나.
3. 커널이 세트를 짤 때 하는 번지 고치기 (run31 `MiRelocateImage`) 와는 **어떤 사이**인가.
4. 이 둘이 **exe** 에도 불리나.

### 바퀴 전에 알던 것

- `[정적: 지도]` (run21) `LdrpMinimalMapModule +0x179 → NtMapViewOfSection` — 이름 사슬로만 알았다. 본문은 안 읽었다.
- `[정적]` (run26) 설치 담당은 `(1) +0x4e LdrpMinimalMapModule` 의 대답이 음수나 `0x4000000e` 면 끝내고, 나머지는 `(4) +0x24f LdrpCompleteMapModule` 의 **셋째 인자**로 넘긴다.
- `[추측]` (run26) 명부 줄 `DllBase` 와 지시서 `+0xa8` 은 `LdrpMinimalMapModule` 이 채울 것. `+0xa8` 은 "맵 크기일 것".
- `[추측]` (run26) 재배치 후보 둘: ① `LdrpCompleteMapModule` ② `LdrpProcessMappedModule +0x24d LdrpIsExecutableRelocatedImage`.
- `[정적]` (run31) 커널은 exe 세트를 짤 때, **새 기본 주소를 골랐으면** `MiCreateNewSection +0x3da` 에서 `MiRelocateImage` 를 부른다. 그 안 조각 하나가 진입점 칸에 델타를 더한다. 함수 전체는 안 읽었다.
- `[정적]` (run19) exe 는 `LdrpInitializeProcess +0x1419 ~ +0x1430` 에서 "헤더 `ImageBase` ≠ `PEB.ImageBaseAddress`" 일 때만 `LdrpProtectAndRelocateImage` 를 부른다. 보통은 건너뛸 것 `[추측]`.
- `[관찰]` (run6·run8, 01_sprint) DLL 주소는 실행마다 바뀔 수 있다 — 같은 dll1 이 `0x7ffd814e0000` 이었다가 `0x7ffd4f190000` 이었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 두 칸을 열면

run 문서 "호출 관계" 트리를 줄였다. 오프셋은 **ret 자리**(콜스택에 보일 숫자)다. 모두 `[정적]`.

```
LdrpMapDllWithSectionHandle (설치 담당, 네 DLL 공통)
 ├─ (1) +0x4e  LdrpMinimalMapModule(지시서, 세트 손잡이)          ← 갖다 놓는 손
 │      ├─ +0x179  NtMapViewOfSection                            ★ 창구에 "땅에 놓아 주세요"
 │      │          (확장 인자가 있을 때만 +0x371 NtMapViewOfSectionEx — 같은 +0x179 로 합침)
 │      ├─ 대답이 0x40000003·0x40000036 → [조건 맞을 때만] +0x2ae LdrpCheckForRetryLoading
 │      ├─ 대답이 0x4000000e          → +0x304 LdrpProcessMachineMismatch
 │      └─ 대답이 음수·0x4000000e     → +0x2d9 NtUnmapViewOfSection, DllBase = 0
 │      돌려주는 값 = 창구 대답 (보통 받은 그대로)
 ├─ (2)(3) 명부·색인                                              (run26)
 └─ (4) +0x24f LdrpCompleteMapModule(지시서, NT 헤더, 창구 대답)  ← 마무리 담당
        └─ [DLL 비트 · 서명 조건 · CorILOnly 아님 · 대답 == 0x40000003]
           +0x320 LdrpRelocateImage                              ★ 번지수 고쳐 쓰기 담당
                └─ +0x14f LdrpProtectAndRelocateImage
                     └─ +0x93 LdrRelocateImageWithBias            실제로 펜을 드는 곳

LdrpInitializeProcess (exe — 다른 문)
 └─ 헤더 ImageBase ≠ PEB.ImageBaseAddress … 이면 +0x1435 LdrpProtectAndRelocateImage
```

| 함수 | rva | 크기 | 이번에 본 범위 |
|---|---|---|---|
| `ntdll!LdrpMinimalMapModule` | `0xa220` | 915바이트 | 본문 (이미 뽑아 둔 것) |
| `ntdll!LdrpCompleteMapModule` | `0xa5c0` | 940바이트 | 본문 (이미 뽑아 둔 것) |
| `ntdll!LdrpRelocateImage` | `0xe3984` | 359바이트 | 본문 (새로) |
| `ntdll!LdrpProcessMachineMismatch` | `0x10af3c` | 185바이트 | 본문 (새로) |
| `ntdll!LdrpProtectAndRelocateImage` | `0xe3af4` | 418바이트 | **이름·호출 자리만** |

- 이름 하나 주의: `LdrpMinimalMapModule +0x257` 이 부르는 함수를 Ghidra 는 `LdrQueryImageFileKeyOption` 으로 잡았다. PDB 이름은 `RtlQueryImageFileKeyOption` 이다. 이 글은 PDB 이름을 쓴다 (run 문서).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 땅에 놓아 달라는 줄

| caller | call | ret | callee |
|---|---|---|---|
| `ntdll!LdrpMinimalMapModule` | `+0x174` | **`+0x179`** | **`ntdll!NtMapViewOfSection`** |

- **누가**: `LdrpMinimalMapModule` — 설치 담당이 `+0x4e` 에서 부른 "갖다 놓는 손".
- **어디서**: 그 함수 시작에서 `0x174` 바이트 떨어진 자리에서 부르고, 끝나면 `+0x179` 로 돌아온다.
  - 그래서 실행 중 이 순간을 잡으면 콜스택에 `LdrpMinimalMapModule+0x179` 로 보일 것이다.
  - 디버거 주소로는 `ntdll+0xa399` 다 (run 문서: `0xa220 + 0x179`).
- **누구를**: `NtMapViewOfSection` — 관리사무소 **신청 창구**.
- **무엇을 위해**: "이 세트(섹션)를 **우리 가게 땅에 놓아 주세요.** 놓은 번지는 이 명부 줄 `DllBase` 칸에 적어 주세요."

#### 한 줄 읽기 ② — 번지수를 고치러 가는 줄

| caller | call | ret | callee | 조건 |
|---|---|---|---|---|
| `ntdll!LdrpCompleteMapModule` | `+0x31b` | **`+0x320`** | **`ntdll!LdrpRelocateImage`** | 창구 대답 == `0x40000003` 만 |

- **누가**: `LdrpCompleteMapModule` — 설치 담당이 `+0x24f` 에서 부른 마무리 담당.
- **어디서**: `+0x31b` 에서 부르고 `+0x320` 으로 돌아온다. 디버거로는 call 자리 `ntdll+0xa8db` (run 문서).
- **누구를**: `LdrpRelocateImage` — 번지수 고쳐 쓰기 담당.
- **무엇을 위해**: 창구가 "**원래 자리가 아닌 곳에 놓였어요**" (`0x40000003`) 라고 대답했을 때, 건물 안에 적힌 번지수를 새 자리에 맞게 고치려고.
- 넘기는 것 넷: 놓인 번지(`E.DllBase`), 놓인 크기(`ctx+0xa8` 값), NT 헤더, DLL 전체 이름(`&E.FullDllName`).

### 2.3 질문 1 — 창구는 `NtMapViewOfSection` 하나 (`Ex` 는 곁길) `[정적]`

**새 말 — 뷰(view)**: 섹션을 어떤 프로세스 땅에 **실제로 펼쳐 놓은 한 판**으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). `NtMapViewOfSection` = 뷰를 만드는 창구, `NtUnmapViewOfSection` = 뷰를 걷는 창구.

- 비유: 세트(섹션) 자체는 창고에 있는 물건이고, **뷰는 그걸 우리 땅에 실제로 세워 놓은 모습**이다. 같은 세트를 여러 가게 땅에 세울 수 있다.

코드가 하는 일:

- **기본 길은 `+0x174 NtMapViewOfSection`** 이다.
- **`+0x36c NtMapViewOfSectionEx`** 는 "확장 인자" (덧붙이는 조건 쪽지) 가 하나라도 만들어졌을 때만 탄다.
  - 지시서 `+0x20` 칸의 비트 `0x800` 이 켜지면 (`+0x11d`) "주소 상한" 쪽지를 만든다.
  - 같은 칸 비트 `0x800000` 이 켜지면 (`+0x132`) 다른 종류(유형 5) 쪽지를 만든다.
  - `+0x13f`·`+0x141` 에서 쪽지가 하나라도 있으면 `Ex` 길로 간다. 없으면 `+0x174`.
- 두 길은 **`+0x179` 에서 다시 만난다** (`Ex` 쪽이 `+0x371` 에서 그리로 점프).
  - 그래서 **멈춤점 하나 `ntdll+0xa399`** 로 두 길을 다 잡는다. 동적 단계에서 편해지는 대목이다.
- 두 비트를 **켜는 줄은 못 찾았다.** 뽑아 둔 ntdll 함수들에서 그 칸에 비트를 켜는 줄이 0건이다.
  - test.exe 길에서 켜지는지는 실행 값 → 동적 (Q20·Q23). 켜는 줄 찾기 → 정적 **Q70**.

### 2.4 창구에 내는 신청서 — 인자 열 개 `[정적]`

run 문서 표 그대로다. 처음 넷은 레지스터, 나머지는 스택으로 건넨다 (x64).

| 순서 | 이름 (SDK) | 건네는 값 | 비유 |
|---|---|---|---|
| 1 | SectionHandle | 이 함수의 **둘째 인자** (지시서 칸이 아님) | 어느 세트인지 — 세트 손잡이 |
| 2 | ProcessHandle | `-1` (자기 프로세스) | "우리 가게 땅에" |
| 3 | BaseAddress | **`&E.DllBase`** (명부 줄 `+0x30`, 처음엔 0) | "놓은 번지를 **여기** 적어 주세요" (빈칸 0 = 아무 데나) |
| 4·5·6 | ZeroBits · CommitSize · SectionOffset | 0 · 0 · `NULL` | 따로 바라는 것 없음 |
| 7 | **ViewSize** | **`&ctx+0xa8`** (호출 전 0 = "전체") | "펼친 넓이를 **여기** 적어 주세요" |
| 8 | InheritDisposition | `1` | |
| 9 | AllocationType | 기본 **`0x800000`** (조건 따라 `0x40000` 더함, 큰 페이지면 `0x20000000`) | |
| 10 | Win32Protect | 기본 **`0x80`** (조건 따라 `2`) | |

- 3번과 7번이 핵심이다. 둘 다 **값이 아니라 칸의 주소**를 건넨다. 그러면 창구가 일을 끝내고 그 칸에 직접 써 넣는다 (2.5).
- 상수 이름 — `0x800000` = `MEM_DIFFERENT_IMAGE_BASE_OK`, `0x20000000` = `MEM_LARGE_PAGES`, `0x80` = `PAGE_EXECUTE_WRITECOPY`, `2` = `PAGE_READONLY`, `1` = `ViewShare` — 은 SDK 기억으로 맞춘 것 `[추측: 이름]`.
  - 이름대로라면 `0x800000` 은 "**헤더에 적힌 기준 주소가 아닌 곳에 놓아도 괜찮아요**" 라는 허락이다. 그렇다면 이 허락이 있어야 `0x40000003` 같은 대답이 나올 수 있다 `[추측]`.
  - 비유: 신청서에 "원래 번지가 아니어도 됩니다" 칸에 체크해서 낸다. 체크가 없으면 다른 번지에 놓지 않고 거절할 수도 있다는 짐작이다 (확인 아님).
- 곁다리: 창구에 가기 직전 `TEB.NtTib.ArbitraryUserPointer` (`TEB+0x28`) 칸을 **DLL 전체 이름**으로 바꿔 두고 (`+0xe2 ~ +0xea`), 돌아오면 되돌린다 (`+0x17b ~ +0x17f`) `[정적]`.
  - 놓는 동안 "지금 놓는 건 이 이름의 DLL" 이라고 커널(또는 디버거)에 알려 주는 자리로 보인다 `[추측]`.
  - 비유: 창구에 가면서 **이름표**를 잠깐 가슴에 달고 가는 것.

### 2.5 `DllBase` 는 관리사무소가 직접 적는다 — run26 의 `[추측]` 이 닫혔다 `[정적]`

run26 은 "`DllBase` 와 지시서 `+0xa8` 은 `LdrpMinimalMapModule` 이 채울 것 `[추측]`" 이라고 했다. 열어 보니 조금 달랐다.

1. 근거: 3번 인자가 `&E.DllBase` (`+0x13b LEA R8,[RDI+0x30]`), 7번 인자가 `&ctx+0xa8` 이다.
2. 그리고 이 함수가 `DllBase` 칸에 쓰는 줄은 **지울 때 0 으로 되돌리는 `+0x2d9` 하나뿐**이다.
3. 그래서 → 놓인 번지(`DllBase`)와 펼친 넓이(`ctx+0xa8`)는 **시스템 콜(관리사무소)이 직접** 써 넣는다. `LdrpMinimalMapModule` 은 빈칸의 주소를 건넸을 뿐이다.

- 그래서 `ctx+0xa8` 의 정체도 정해졌다: **ViewSize 칸** (인자 7). run26 2.8 의 "맵 크기일 것 `[추측]`" 이 확정이다.
  - 이 값은 나중에 `LdrpCompleteMapModule +0x309` 가 읽어서 `LdrpRelocateImage` 둘째 인자로 넘긴다.
- **`SizeOfImage` (`E+0x40`) 는 여기서 안 채운다.**
  - 쓰는 곳은 `LdrpInsertModuleToIndexLockHeld +0x2e` (NT 헤더 값), 그리고 `ctx+0x20 & 0x800000` 길의 `LdrpMapDllWithSectionHandle +0x222`.
  - 곧 명부의 `SizeOfImage` 는 "창구가 알려 준 넓이" 가 아니라 **설계도(헤더)에 적힌 넓이**다.
- 비유: 관리실은 명부의 **번지 칸을 비워 둔 채** 창구에 내민다. 창구 직원이 세트를 놓고 **그 칸에 번지를 직접 적어** 돌려준다. 건물 크기 칸은 나중에 관리실이 설계도를 보고 따로 적는다.

### 2.6 창구의 대답 가르기 `[정적]`

**새 말 — 경고 코드**: `0x4` 로 시작하는 결과 코드. "**실패는 아니지만 알려 줄 게 있다**" 쪽 대답으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 실패는 음수(`0xc…`) 쪽이다.

- 코드도 그렇게 다룬다 `[정적]`: 음수(와 `0x4000000e`)일 때만 뷰를 걷고, **성공·경고 코드면 놓은 세트를 그대로 둔다.**

`NtMapViewOfSection` 의 대답(`EBX`)을 `+0x190 ~ +0x1aa` 에서 가른다.

| 대답 | 무엇을 하나 | 돌려주는 값 |
|---|---|---|
| `0` | 그대로 | `0` |
| `0x40000003` · `0x40000036` | 지시서 `+0xb0 == 0` 이고 `LdrpMapAndSnapWork ≠ 0` 일 **때만** `+0x2a9 LdrpCheckForRetryLoading`. 참이면 `0xc000022d`. 거짓이고 kernel32 갈래(`R14B`)면 `0xc0000018`. 조건이 아니면 검사 없이 그대로 | 보통 **받은 값 그대로** |
| `0x4000000e` | `+0x2ff LdrpProcessMachineMismatch` | 그 결과 |
| 그 밖 | 그대로 | 받은 값 |

- 마지막 정리 (`+0x1b0`, `+0x2c0 ~ +0x2de`): `DllBase ≠ 0` 이고 (음수 또는 `0x4000000e`) 면 `+0x2d4 NtUnmapViewOfSection` 하고 `DllBase = 0`.
- 위로 올라가면: 설치 담당은 음수·`0x4000000e` 면 끝내고, 나머지는 마무리 담당 셋째 인자로 넘긴다 (run26). 그래서 **마무리 담당이 받는 값은 `0`, `0x40000003`, `0x40000036`, 그 밖의 양수** 중 하나다.
- 이름들:
  - `0x40000003` — 요청서·결과서가 `STATUS_IMAGE_NOT_AT_BASE` 로 적었다.
  - `0x40000036` — **이름 못 정함.** `STATUS_IMAGE_AT_DIFFERENT_BASE` 로 기억 `[추측]` → 정적 Q70.
  - `0xc000022d` (`STATUS_RETRY`)·`0xc0000018` (`STATUS_CONFLICTING_ADDRESSES`) 도 SDK 기억 `[추측]`.
- 비유: 창구 대답은 세 부류다.
  - "놓았어요" (`0`)
  - "놓았어요. 그런데 원래 자리가 아니에요" (`0x40000003`) · 이름 모를 비슷한 대답 (`0x40000036`)
  - "규격이 다른 세트예요" (`0x4000000e`) — 이건 걷어 내고 다른 곳을 찾는다 (run26 걸음 4).

#### 규격이 다를 때 — `LdrpProcessMachineMismatch` (rva `0x10af3c`, 185바이트) `[정적]`

- `.NET` 디렉터리(14번)를 보고 **IL 전용**이면, `ctx+0x20 |= 0x200000` 하고 `0` 을 돌려준다 (`+0x37`, `+0x3c`). "기계 종류가 달라도 IL 전용 .NET 이면 받아 준다."
- 아니면 exe 헤더 `MajorSubsystemVersion` (`+0x56`) 을 본다.
  - 3 보다 크면 `0x4000000e` 그대로 (`+0x5c`) → 위 정리에서 뷰를 걷는다.
  - 3 이하면 `+0x90 NtRaiseHardError` 로 사용자에게 묻는다.
- test.exe 는 `MajorSubsystemVersion` 6 `[정적+샘플]`. dll1·dll2·kernel32 는 모두 x64 라 이 갈래는 안 탈 것 `[정적 조건 + 헤더 값]`.

### 2.7 test.exe 에선 안 탈 갈래 둘 — 짧게

#### 큰 페이지 갈래 (`+0x257`, `+0x278`, `+0x37a`)

**새 말 — 큰 페이지**: 보통보다 훨씬 큰 단위로 메모리를 잡는 방식으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

- `[정적]` 전역 `LdrpLargePageDllKeyHandle ≠ 0` 일 때만 탄다 (`+0xbb`, `+0x238`).
- `[정적]` 그 핸들은 `LdrpInitializeExecutionOptions +0x389` 가 `PEB.ImageUsesLargePages` (PEB+3 비트 0) 일 때만 `+0x3c6 NtOpenKey` 로 연다 (키 이름 `LargePageDLLs`). 없으면 `+0x3d4` 에서 0. `LdrpInitializeProcess +0x178d ~ +0x179e` 가 닫고 0 으로 쓴다. 쓰는 곳은 이 둘뿐 (`refs`).
- `[정적]` 핸들이 있으면 DLL 이름과 같은 값을 읽고, 0 이 아니면 권한을 잡아 할당 값을 `0x20000000` 으로 바꾼다.
- `[소스]` sample 에 큰 페이지 설정이 없다 → 핸들 0, `+0x238` 에서 건너뛸 것 `[추측]`.

#### kernel32 만의 갈래 (`R14B`)

**새 말 — LAA (`LARGE_ADDRESS_AWARE`)**: exe 헤더 `Characteristics` 의 `0x20` 비트. "큰 주소도 다룰 줄 안다" 는 표시로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

- `[정적]` DLL 이름이 `KERNEL32.DLL` (대소문자 무시, `+0x92 RtlEqualUnicodeString`) 이고 exe 가 LAA 면 `R14B = 1`.
- `[정적+샘플]` test.exe `Characteristics` = `0x22` (LAA 켜짐) → kernel32 를 들일 때 `R14B = 1` `[정적 조건 + 헤더 값]`.
- 그러면 큰 페이지 갈래는 건너뛰고, "재시도 검사 거짓 → `0xc0000018`" 갈래가 열린다.
- 그런데 그 갈래는 `LdrpMapAndSnapWork ≠ 0` 일 때만 닿는다. kernel32 는 병렬 켜기 (`LdrpInitializeProcess (8) +0x1a47`) **앞에서** 들어오고 그때 그 값은 0 이다 (run24) → **kernel32 에선 안 탄다** `[정적: 코드 + run24 순서]`.
  - `LdrpMapAndSnapWork` = run24 의 **호출 버튼** (병렬 일꾼 개체). [run24 해설 2.3](run24-work-queue-해설.md#23-핵심--게시판--ldrpqueuework-는-붙이기만-한다)
- kernel32 만 따로 다루는 **이유는 모른다** (run 문서).

### 2.8 질문 2 — 재배치 담당 = `LdrpCompleteMapModule`: **예.** 단 문이 다섯 `[정적]`

- `LdrpRelocateImage` 를 부르는 코드는 **`+0x31b` 하나뿐**이다 (`refs`).
- 인자: `RCX` = 지시서, `RDX` = NT 헤더, **`R8D` = 창구 대답**. 들어오면 `R14` = 명부 줄 `E`, `RDI` = `E.DllBase` (낮은 두 비트를 떼어 냄).

부르기까지 **다 참이어야** 하는 문 다섯 (run 문서 순서):

| 문 | 무엇을 보나 | 줄 | 아니면 |
|---|---|---|---|
| 1 | NT `Characteristics & 0x2000` — **DLL 비트** | `+0xbc ~ +0xc6` | `+0x2ba` 에서 `E.Flags` 의 `ImageDll` 을 끄고 끝 |
| 2 | 지시서 `ctx+0x20 & 0x80` 이 꺼졌거나, 켜졌다면 `DllCharacteristics & 0x80` (`FORCE_INTEGRITY`) 이 있다 | `+0xcc`, `+0x2c4` | `+0x2fa` 로그 → `+0x2ff` `0xc0000428` |
| 3 | `E.Flags` 의 `CorILOnly` (비트 24) 꺼짐 | `+0xd7` | 끝 |
| 4 | 대답이 `0x40000003` **또는** `0x40000036` | `+0xe4`, `+0xec` | 끝 (**`0` 은 여기서 끝**) |
| 5 | 대답이 **정확히 `0x40000003`** | `+0x1b4 CMP R12D,0x40000003` | `0x40000036` 이면 `+0x1c1` 로 건너뜀 (ETW 만) |

- 5번을 통과하면 `+0x309 ~ +0x31b` 에서 `LdrpRelocateImage(E.DllBase, ctx+0xa8 값, NT, &E.FullDllName)`.
- 6. 결과가 음수면 `+0x338 LdrpLogError` 하고 그 값을 돌려준다 → **번지 고치기 실패 = DLL 들이기 실패.**
- **문이 아닌 것**: `.reloc` 이 있나, `DYNAMIC_BASE` (`DllCharacteristics 0x40`) 가 켜졌나 — 이 함수는 **안 본다.**
  - 곧 이 함수는 "얼마나 옮겨졌나" 를 스스로 재지 않는다. **창구 대답만 믿는다.**
- 평범한 native DLL 은 .NET 디렉터리가 없어서 `+0xaf` 로 곧장 가 1~5 만 본다 `[정적: 경로 따라감]`.
- 곁가지: 번지 고치기 **앞뒤 ETW** (`0x1490` 앞 `+0x39e`, `0x1491` 뒤 `+0x24f`, 둘 다 `LdrpLogEtwEvent`) 는 `PEB.TracingFlags` 비트 2 와 공유 데이터 `UserModeGlobalLogger` 칸이 켜졌을 때만 남는다 `[정적]`.
  - `RtlGetCurrentServiceSessionId` ×4 는 그 칸을 `KUSER_SHARED_DATA` 에서 읽을지 silo 사본에서 읽을지 고르는 것 `[정적]`.
  - `0x40000036` 일 때도 이 일지는 남는다 (결과서) — 고치지는 않으면서 일지만 쓴다.

#### sample 길에서 문 1 ~ 5 는? — 단계별로

1. 근거 `[정적+샘플]`: dll1·dll2 는 `Characteristics 0x2022` (DLL 비트 있음), `DllCharacteristics 0x160` (`FORCE_INTEGRITY` 없음), .NET 없음. kernel32 도 `0x2022`, .NET 없음 `[정적: PE 표]`.
2. 그래서 → 문 1·3 은 통과한다.
3. 문 2 는 지시서 `ctx+0x20 & 0x80` 이 **꺼져 있다고 칠 때** 통과한다 `[추측]` — sample DLL 엔 `FORCE_INTEGRITY` 가 없으니, 그 비트가 켜져 있으면 오히려 거절된다.
4. 문 4·5 (창구 대답) **만 실행에 달렸다.** → 2.10

### 2.9 실제로 펜을 드는 곳은 두 단계 아래 `[정적]`

#### `LdrpRelocateImage` (rva `0xe3984`, 359바이트)

**새 말 — 재배치 디렉터리 (`.reloc`)**: PE 헤더 데이터 디렉터리의 **5번** 칸이 가리키는 곳. "이미지 안에서 **번지가 적힌 자리들의 목록**" 으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).

- 비유: 건물 안 "**번지수가 적힌 안내판 위치 목록**". 다른 번지에 놓이면 이 목록을 보고 안내판을 하나씩 고친다.

**새 말 — `RELOCS_STRIPPED`**: NT `Characteristics` 의 `1` 비트. 이름대로면 "재배치 목록을 **떼어 냈음**" 표시 `[추측: 이름]`.

코드가 하는 일:

- `+0x7d TEST [R8+0x16],1` — `RELOCS_STRIPPED` 를 본다.
  - **꺼져 있으면** `+0x95` 로 재배치 디렉터리(5번)를 찾는다. 없거나 크기 0 이면 **아무것도 안 하고 `0`** (성공) 을 돌려준다.
  - **켜져 있으면** 그 검사를 건너뛰고 다음으로 간다 (`+0x81 JNZ`). 그 뒤에 무슨 일이 생기는지는 안 읽은 함수 안쪽이다.
- `+0x12b` 로 .NET 디렉터리(14번)를 보고, IL 전용이면 `0` (안 고침).
- 아니면 `+0x142 LdrpLogDllRelocationEtwEvent` (일지) → **`+0x14a LdrpProtectAndRelocateImage(DllBase)`** → 그 결과를 돌려준다.

#### `LdrpProtectAndRelocateImage` (rva `0xe3af4`, 418바이트) — 이름·호출만

- `+0x4a NtQueryVirtualMemory` → **`+0x8e LdrRelocateImageWithBias(DllBase)`** ← **실제 번지 고치기** → `+0xbc`·`+0x16b LdrpSetProtection` (보호 바꾸기 둘) + 예외 필터 `LdrpGenericExceptionFilter`.
- 보호를 고치기 **앞**에 바꾸나 **뒤**에 바꾸나는 `[추측]` 이다. 본문 (델타 계산·블록 처리) 은 안 읽었다.
- 내 짐작 `[추측]`: 보호 바꾸기 줄이 **둘**이고 함수 이름에 "Protect" 가 붙어 있어서, "안내판 잠금을 풀고 → 번지수를 고치고 → 다시 잠그기" 모양일 수 있다. 하지만 둘 중 어느 쪽이 앞이고 뒤인지조차 코드로 안 봤다 (run 문서도 `[추측]`). 그러니 그림으로만 들고 있자.

#### 누가 누구를 부르나 — 개수 (`refs`)

- `LdrRelocateImageWithBias` 를 부르는 곳: `LdrpProtectAndRelocateImage +0x8e` **하나**.
- `LdrpProtectAndRelocateImage` 를 부르는 곳: **셋**.
  - `LdrpRelocateImage +0x14a` (DLL)
  - `LdrpInitializeProcess +0x1430` (exe — 2.11)
  - `LdrpLoadCustomNtdllWithSectionHandle +0x5a` (custom ntdll)
- `LdrpRelocateImage` 를 부르는 곳: `LdrpCompleteMapModule +0x31b` **하나**.
- 그림으로: 펜(`LdrRelocateImageWithBias`)은 하나고, 펜을 쥐여 주는 손(`LdrpProtectAndRelocateImage`)도 하나다. **그 손을 부르는 문이 셋**(DLL·exe·custom ntdll) 있다.

### 2.10 질문 3 — 커널 재배치(run31)와 유저 재배치는 어떤 사이인가

이번 바퀴에서 제일 헷갈리기 쉬운 곳이라 천천히 간다.

#### 먼저 용어 둘을 떼어 놓기

- **ASLR** = 놓을 **번지를 고르는** 일 ([run31 해설 2.10](run31-image-section-해설.md#210-재배치-델타--다른-번지에-놓을-거면-그-칸을-고쳐-쓴다)). run31 에서 본 건 커널이 exe 세트를 짤 때 "**새 기본 주소를 골랐으면**" `MiRelocateImage` 를 부르는 줄이다 `[정적]`.
- **재배치** = 고른 번지에 맞게 건물 안 **번지수를 고쳐 쓰는** 일.
- 둘은 다른 일이다. "어디에 놓을까" 와 "놓은 뒤 안내판을 고칠까" 다.
- 이번 질문은 뒤의 것이다: **고쳐 쓰는 펜을 누가 드나.**

#### 사실 ① — "원래 자리가 아닌 곳에 놓였다" 는 거의 틀림없다

1. `[정적+샘플]` dll1·dll2 헤더 `ImageBase` = `0x180000000`, test.exe = `0x140000000`. 셋 다 `DYNAMIC_BASE` + `.reloc` 있음, .NET 없음.
2. `[정적: PE 표]` kernel32 헤더 `ImageBase` 도 `0x180000000`, `DllCharacteristics 0x4160`, `.reloc` 있음.
   - 같은 번지 `0x180000000` 을 dll1·dll2·kernel32 가 **다 원한다.** 한 번지엔 한 건물만 놓을 수 있으니, 적어도 몇은 어차피 다른 번지로 간다 (결과서 힌트 `[정적: 헤더 값]`).
3. `[관찰]` (run6·run8) 실제로 놓인 번지:

| 모듈 | 헤더 `ImageBase` | 관찰 번지 |
|---|---|---|
| dll1 | `0x180000000` | `0x7ffd814e0000` / `0x7ffd4f190000` |
| dll2 | `0x180000000` | `0x7ffd80e70000` / `0x7ffd4ea20000` |
| test.exe | `0x140000000` | `0x7ff70ee10000` |

4. 그래서 → **헤더 번지와 다르다.** 다른 번지에 놓였다는 건 거의 틀림없다.
   - 다만 run6~8 은 디버거 VM 빌드라 `sample/bin` 과 같은 파일인지는 모른다. 헤더 `ImageBase` 는 같은 링크 기본값일 것 `[추측]` (run 문서).

#### 사실 ② — 펜을 들 수 있는 사람이 둘 보인다

| 누가 | 언제 | 근거 | 무엇을 봤나 |
|---|---|---|---|
| **관리사무소** (커널) `MiRelocateImage` | 세트를 **짤 때** (서류 단계) | run31 `[정적]` | exe 세트 길에서. 진입점 칸에 델타를 더하는 **조각 하나**만 봤다. 함수 전체는 안 읽었다 |
| **관리실** (ntdll) `LdrpCompleteMapModule → LdrpRelocateImage → … LdrRelocateImageWithBias` | 세트를 땅에 **놓은 뒤** | 이번 `[정적]` | 부르는 조건(문 다섯)과 호출 자리. 펜 본문은 안 읽었다 |

- 주의: run31 에서 본 커널 길은 **exe 세트**를 짜는 길이다. DLL 세트(run26 `NtCreateSection`)도 같은 `MiRelocateImage` 를 지나는지는 이번 근거에 없다 — **모른다.**

#### 사실 ③ — 둘을 잇는 끈은 "창구 대답" 하나다 `[정적]`

- 관리실이 펜을 드는 문은 **대답이 정확히 `0x40000003`** 인 것 하나뿐이다 (2.8 문 5).
- `0` 이면 펜을 안 든다. `0x40000036` 도 문 4 는 통과하지만 펜은 안 든다. 코드가 두 값을 **일부러 가른다** (`MinimalMap +0x192`/`+0x1a7`, `Complete +0xe4`/`+0x1b4`).
- 그리고 **창구가 어떤 대답을 줄지는 ntdll 코드로 안 정해진다.** 시스템 콜 안쪽(관리사무소) 일이다.

| 창구 대답 | 관리실이 하는 일 `[정적]` | 뜻 |
|---|---|---|
| `0` | 안 고친다 (문 4 에서 끝) | "놓았어요" |
| `0x40000003` | **고친다** — `+0x31b LdrpRelocateImage` | "놓았어요. 그런데 원래 자리가 아닌 곳이에요" (`STATUS_IMAGE_NOT_AT_BASE`, 요청서 이름) |
| `0x40000036` | 안 고친다 (문 5 에서 빠짐, 일지만) | 이름 모름 (`STATUS_IMAGE_AT_DIFFERENT_BASE` 기억 `[추측]`) |

- 비유: 관리실은 건물을 직접 재 보지 않는다. **창구 직원이 "원래 자리 아니에요" 라고 말해 줄 때만** 안내판을 고치러 간다.

#### 짐작 — 보통 DLL 은 관리실이 안 고칠 수도 있다 `[추측]` (근거 약함)

run 문서의 짐작을 단계로 나누면:

1. **만약** 관리사무소가 세트를 짤 때 이미 델타를 더해 둔다면 (run31 `MiRelocateImage` 쪽)
2. ASLR 로 옮겨진 보통 DLL 은 창구에서 `0` 이나 `0x40000036` 을 받을 것이고
3. 그러면 관리실은 **펜을 안 든다.**

이 짐작이 기대는 다리 **둘** — 둘 다 정적으로 확인 못 했다:

- ① `0x40000036` 의 이름 기억 ("다른 주소지만 이미 고쳐져 있음" 쪽이라는 기억, 결과서).
- ② 관리사무소가 **메모리 속 헤더의 `ImageBase`** 도 새 번지로 고쳐 두는지.
  - 이게 왜 중요하냐면, exe 쪽 줄 `LdrpInitializeProcess +0x1419` 가 "**메모리 헤더 `ImageBase` 와 실제 번지가 다르면** 고친다" 이기 때문이다 (2.11).
  - 결과서는 이 줄을 "평소엔 같다고 기대하는 코드로 보임" 이라고 읽었다 — 곧 커널이 헤더도 고쳐 둔다는 쪽 `[추측]`.

그래서 지금 확실한 것과 아닌 것:

| | 태그 |
|---|---|
| DLL 은 헤더 번지가 아닌 곳에 놓인다 | 거의 틀림없음 (`[관찰]` + `[정적+샘플]`, 같은 빌드인지는 `[추측]`) |
| 관리실은 창구 대답이 `0x40000003` 일 때만 고친다 | `[정적]` |
| 창구가 실제로 무엇을 대답하나 | **모름** (커널 안쪽) |
| 그래서 보통 DLL 은 관리실이 안 고친다 | `[추측]` (근거 약함) |

#### 어떻게 판가름하나 — 멈춤점 둘 (Q20·Q23, 같은 실행)

- `ntdll+0xa399` (`LdrpMinimalMapModule +0x179`): `r eax` 가 `0` / `0x40000003` / `0x40000036` 중 무엇인가. 이 한 곳에서 kernel32·kernelbase·dll1·dll2 를 다 본다.
  - 같은 자리에서 붙은 번지 `poi(@rdi+0x30)` 와 **메모리 헤더 `ImageBase`** 를 비교하면 다리 ② 도 본다 (같으면 커널이 헤더도 고친 것).
- `ntdll+0xe3984` (`LdrpRelocateImage` 입구): **안 서면** "보통 DLL 은 유저 재배치 안 함" 확정.
- 판정이 둘로 깔끔하게 갈린다:
  - 대답 `0x40000003` + `ntdll+0xe3984` 섬 → 관리실이 펜을 든다.
  - 대답 `0` 또는 `0x40000036` + `ntdll+0xe3984` 안 섬 → 관리사무소가 다 해 두고, 관리실은 손 안 댄다.

### 2.11 질문 4 — exe 는 다른 문으로 `[정적]`

- 이번 네 함수(`MinimalMap`·`CompleteMap`·`LdrpRelocateImage`·`Mismatch`)는 부르는 곳이 **각 1곳**이고, 모두 DLL 들이기 사슬 안이다 (`_nodes.tsv` `callers_total`).
- exe 는 커널이 이미 땅에 놓아 주었다 (run10 `MiMapProcessExecutable`). 그래서 이 길로 안 온다.
- exe 고쳐 쓰기는 따로 있다 — [run19 해설 2.6 의 5번](run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1) 의 그 줄이다:
  1. `LdrpInitializeProcess +0x1419 CMP [NT+0x30], PEB.ImageBaseAddress` — 메모리 헤더 `ImageBase` 와 실제 번지 비교
  2. 다르고, IL 전용이 아니고, `+0x1424` `UseWOW64 == 0` 이면
  3. **`+0x1430` (ret `+0x1435`) `LdrpProtectAndRelocateImage(PEB.ImageBaseAddress)`**
- 이번에 이어진 것: run19 는 callee 이름까지만 봤다. 이번에 그 아래 `+0x8e LdrRelocateImageWithBias` 까지 이어졌다. **DLL 과 같은 손, 같은 펜**이다.
- 다른 점: exe 는 `LdrpRelocateImage` 와 `0x40000003` 문을 **건너뛴다.** 창구 대답 대신 "**헤더 번지 ≠ 실제 번지**" 를 직접 비교한다.
  - 비유: 본 건물(exe)은 관리실이 창구에 안 갔다 (관리사무소가 미리 놓아 줌). 그래서 들을 대답이 없다. 대신 설계도 번지와 실제 번지를 **직접 맞대 본다.**
- 그 뒤 exe 명부 줄은 `+0x1450` (ret `+0x1455`) `LdrpInsertDataTableEntry` 로 명부에 들어간다 (run19·run35).
- 보통 이 줄을 타나? — 다리 ② 와 같은 질문이다.
  - `[관찰]` test.exe 실제 번지 `0x7ff70ee10000` ≠ 헤더 `0x140000000` `[정적+샘플]`.
  - 그래도 이 줄을 타는지는 **메모리 속 헤더** 값에 달렸다. 커널이 고쳐 두면 같아서 안 탄다 `[추측]`.
  - 동적: `ntdll+0x8d834` (`+0x1430` call) 이 **안 서야** 커널이 헤더도 고친 것 (run31 Q61 과 같은 실행이면 좋다).

### 2.12 비유로 다시 한 번

1. 관리실(ntdll)의 설치 담당이 건물 하나를 들이려 한다.
2. 갖다 놓는 손(`LdrpMinimalMapModule`)이 관리사무소 **창구**(`NtMapViewOfSection`)에 신청서를 낸다.
   - "이 세트를 우리 땅에 놓아 주세요. 원래 번지가 아니어도 됩니다(`0x800000` `[추측: 이름]`). **놓은 번지는 명부 이 칸에, 넓이는 지시서 이 칸에** 적어 주세요."
3. 창구가 세트를 놓고, 두 칸을 **직접** 채워 돌려준다. 그리고 한마디 대답을 붙인다.
4. 대답이 "규격이 달라요"(`0x4000000e`)·실패(음수)면 걷어 낸다. 나머지는 그대로 마무리 담당에게 넘긴다.
5. 마무리 담당(`LdrpCompleteMapModule`)은 문 다섯을 본다. 대답이 **"원래 자리 아니에요"(`0x40000003`)** 일 때만 번지 고치기 담당(`LdrpRelocateImage`)을 부른다.
6. 고치기 담당은 "번지 적힌 자리 목록"(`.reloc`)이 있나 보고, 있으면 손(`LdrpProtectAndRelocateImage`)에게 펜(`LdrRelocateImageWithBias`)을 쥐여 준다.
7. 본 건물(exe)은 창구를 안 거쳤으니, 관리실이 설계도 번지와 실제 번지를 직접 맞대 보고 다를 때만 같은 손·같은 펜을 쓴다.
8. 남은 수수께끼: **창구가 실제로 "원래 자리 아니에요" 라고 말하나, 아니면 관리사무소가 다 고쳐 두고 "놓았어요" 라고만 하나.** 버스를 타 봐야 안다 (Q20·Q23).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 그림 2 앞 설명 | "재배치를 어느 상자가 하는지는 아직 모른다 (`(4)` 또는 `(5)` 후보)" | 취소선 → **`(4)` `LdrpCompleteMapModule`** `[정적]` + run38 문단 (붙이기·재배치 상자 설명) |
| 그림 2 `LdrpMinimalMapModule` 상자 (dll1·dll2) | "(정적: 메모리에 붙음. 지도상 안에 +0x179 NtMapViewOfSection)" | 섹션 = 둘째 인자, 기준 주소 칸 = `DllBase` (커널이 채움), 보기 크기 = `ctx+0xa8`, 대답 `0x40000003` 은 그대로 위로, `Ex` 조건 |
| 그림 2 새 상자 | 없음 | `(정적)` 상자 넷: dll1·dll2 각각 **`NtMapViewOfSection`** (`+0x179` 화살표), **`LdrpRelocateImage`** (`+0x320` 화살표) |
| 그림 2 `LdrpCompleteMapModule` 상자 | "(정적: 매핑 결과 코드를 받음. 재배치 후보 추측, Q52)" | "재배치 담당. 맵 결과가 정확히 `0x40000003` 일 때만. `0` 이면 안 고침" |
| 2.1 `MiRelocateImage` 절 | 커널 쪽만 | "**유저 쪽 짝**" 한 줄: exe `+0x1419`/`+0x1430`, 커널이 헤더도 고치면 안 탐 `[추측]` |
| 2.4 `LdrpInitializeProcess` | run19 재배치 갈림 | 조건 줄 + 그 아래 `LdrRelocateImageWithBias`. `[관찰]` test 번지 ≠ 헤더, 그래도 타는지는 동적 |
| 2.5 kernel32 준비 | — | kernel32 도 같은 `+0x174 NtMapViewOfSection`, kernel32 갈래(`R14B`), kernel32 헤더 값 `[정적: PE 표]` |
| 2.6 제목·트리 | "붙이기·재배치" 없음, `+0x24f LdrpCompleteMapModule` 설명 없음 | 제목에 "붙이기·재배치 run38", 트리에 `+0x179 NtMapViewOfSection`, "맵 결과 == `0x40000003` 일 때만 `+0x320 LdrpRelocateImage`" |
| 2.6 새 절 | 없음 | "붙이기 `LdrpMinimalMapModule` · 마무리 `LdrpCompleteMapModule` · 재배치 `LdrpRelocateImage`" — 트리, 인자, 대답 표, 문 다섯, 커널 재배치와의 관계, 동적 확인거리 |
| 2.6 재배치 후보 줄 | 후보 둘 `[추측]` | "(해결됨, 정적) → run38: `LdrpCompleteMapModule` 이다." 둘째 후보는 `[공백]` |
| 4장 | 2.6 재배치 쪽 (Q52) | 새 줄: 맵 대답·유저 재배치 실행 값 (Q20·Q23 출처), **Q70**, 번호 없는 정적 남은 것. 그림 2 대응 표에 새 상자 두 줄 |
| 맨 위 근거·표시 | run37 까지 | run38 ntdll 추가. 헤더 값 태그 규칙 (`[정적+샘플]` / `[정적: PE 표]`) 한 줄 |

태그 변화만 모으면:

- `DllBase`·`ctx+0xa8` 을 누가 채우나: `[추측]` (`MinimalMap` 이 채울 것) → **`[정적]`** (시스템 콜이 직접 씀, `ctx+0xa8` = ViewSize).
- 재배치 담당: `[추측]` 후보 둘 → **`[정적]` `LdrpCompleteMapModule`** (첫째).
- `LdrpMinimalMapModule +0x179 NtMapViewOfSection`: `[정적: 지도]` → **`[정적]`** (본문).
- 새 `[추측]` (근거 약함): 보통 DLL 은 유저 재배치를 안 할 것 → 동적.

### 큰 그림에서 어떻게 이어지나

- DLL 하나가 **파일 → 세트 → 땅 → 번지 고치기 → 주소 잇기** 까지 가는 길이 이제 **코드로 끊김 없이** 이어졌다.
  - 파일·세트 (run26) → **땅에 놓기·번지 고치기 (이번)** → 명부·마무리 (run26) → 주소 잇기 IAT (run36).
- 남은 건 거의 다 **"실행 때 무엇이 오나"** 다. 그리고 그 질문들이 **멈춤점 몇 개로 모인다.**
  - 특히 `ntdll+0xa399` 하나에서 네 DLL 의 대답·번지·넓이·이름을 다 본다. run21 이 Q20 용으로 이미 정한 자리와 **같은 주소**다.
  - run 문서 메모: 이 자리는 DLL 마다 한 번씩 선다 (kernel32·kernelbase·dll1·dll2 + `LoadLibraryExW` 셋). 그리고 유저 주소라 **다른 프로세스의 ntdll 에도 걸리므로 test.exe 조건은 꼭** 붙인다.
- 커널 쪽 짝(run31 Q61) 과 같은 실행으로 묶으면 "**관리사무소가 고쳤나, 관리실이 고쳤나**" 를 한 번에 판가름할 수 있다.
- 다음 정적 항목은 **Q53** — DLL 검색 경로 (exe 폴더가 들어가는 자리).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 창구가 kernel32·kernelbase·dll1·dll2 에 **`0` / `0x40000003` / `0x40000036` 중 무엇**을 대답하나 (곧 관리실이 실제로 번지를 고치나) | 대답은 커널 안쪽에서 정해진다. ntdll 코드로 안 정해진다 | 동적 **Q20** (kernel32·kernelbase) · **Q23** (dll1·dll2): `ntdll+0xa399` 에서 `r eax`, `ntdll+0xe3984` 가 서나. 서면 `r rcx`, `du poi(@r9+8)`, `ntdll+0xe3b82` |
| 커널이 **메모리 속 헤더 `ImageBase`** 도 새 번지로 고쳐 두나 (짐작의 다리 ②) | 실행 때 메모리 값 | 동적 Q20·Q23: `ntdll+0xa399` 에서 붙은 번지 `poi(@rdi+0x30)` 와 메모리 헤더 `ImageBase` 비교. exe 는 `ntdll+0x8d834` 가 안 서나 (Q23 출처, run31 Q61 과 같은 실행) |
| DLL 세트도 커널 `MiRelocateImage` 를 지나나, 그 함수가 이미지 안 번지수 전체를 고치나 | run31 은 exe 세트 길에서 조각 하나만 봤다. 이번 run 문서는 "정적으로 확인 못 함 → 동적" 으로만 넘겼다 | **딱 맞는 대기열 줄은 없다.** 가장 가까운 건 run31 **Q61** (exe 세트 길에서 `MiRelocateImage` 조각이 서나). DLL 쪽은 위 Q20·Q23 의 `r eax` 결과로 간접적으로 갈린다 |
| `0x40000036` 의 이름 (짐작의 다리 ①) | SDK 기억뿐. PDB/헤더로 안 봄 | 정적 **Q70** |
| 지시서 `ctx+0x20` 비트 `0x800`·`0x800000` (→ `Ex` 길) 을 켜는 줄, test.exe 에서 켜지나 | 뽑아 둔 함수에 켜는 줄 0건. 켜지는지는 실행 값 | 켜는 줄 → 정적 **Q70**. 켜지나 → 동적 Q20·Q23 (`ntdll+0xa58c` 가 **안 서야** 기본 길) |
| 마무리 담당이 받은 값이 창구 대답과 같나 | 실행 값 | 동적 Q20·Q23: `ntdll+0xa5c0` 입구 `r r8d` |
| 안 탈 갈래가 정말 안 타나 (재시도 검사·뷰 걷기·규격 불일치) | 실행 값 | 동적 Q20·Q23: `ntdll+0xa4c9`·`ntdll+0xa4f4`·`ntdll+0xa51f` 가 안 서야 정상 |
| 큰 페이지 핸들이 0 인가, `LdrpMapAndSnapWork` 가 kernel32 때 0 · dll1 때 ≠ 0 인가 | 실행 값 | 동적 Q20·Q23: `dq ntdll+0x1d2780 L1`, `dq ntdll+0x1d2640 L1` |
| `LdrpProcessMappedModule +0x24d LdrpIsExecutableRelocatedImage` 가 무엇을 묻나 (run26 둘째 후보) | 이번 범위 밖 | 번호 없음 `[공백]` (flow.md 4장 2.6 줄) |
| `LdrRelocateImageWithBias`·`LdrpSetProtection` 본문 (보호를 고치기 앞/뒤 어느 쪽에서 바꾸나), `LdrpCheckForRetryLoading` 조건, `LdrpCorValidateImage`·`LdrpCorFixupImage` 본문 | callee 본문 안 읽음 | 번호 없음 (같은 4장 줄) |
| kernel32 만 따로 다루는 이유 (`R14B`) | 코드에 이유가 안 적혀 있다 | 모름 |
| `CompleteMap` 입구 `BL` (낮은 비트 = 데이터 파일 맵 표시 `[추측]`), `ctx+0x20` 비트 `0x80` (서명 요구 `[추측]`), `ArbitraryUserPointer` 의 쓰임 `[추측]` | 이름·모양으로 본 것 | 번호 없음 |

---

## 5. 더 읽기

- run 문서: [`run38-map-module.md`](run38-map-module.md) — 특히 "호출 지점" 표, 두 함수 "구간" 표, "호출 관계" 트리, 확인된 것 1~8, "정정" (run26 후보 판정, 결과서 점프 목적지), "아직 확인 필요" (멈춤점 목록), 멈춤점 메모
- 결과서: [`_workspace/sast/33_map-module_result.md`](../../_workspace/sast/33_map-module_result.md) — "먼저 알아 둘 칸" (명부 줄·PEB·NT 헤더 오프셋, 지시서 칸, 상태 코드 표), 질문별 답, "샘플 헤더" 표, 동적 표
- 요청서: [`_workspace/sast/33_map-module.md`](../../_workspace/sast/33_map-module.md)
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - 그림 2 앞 run38 문단, 그림 2 `(1)~(2) 사이` 상자
  - 2.1 `nt!MiRelocateImage` 절의 "유저 쪽 짝"
  - 2.4 `LdrpInitializeProcess` exe 재배치 줄
  - 2.5 kernel32 갈래
  - 2.6 "붙이기 · 마무리 · 재배치" 절
  - 4장 2.5·2.6 줄 (Q20·Q23 멈춤점, Q70, 번호 없는 것)
- 앞 해설:
  - [run26 해설](run26-map-dll-file-해설.md) — 파일 길 다섯 걸음, 재배치 후보 둘 (이번에 첫째로 판정)
  - [run31 해설 2.10](run31-image-section-해설.md#210-재배치-델타--다른-번지에-놓을-거면-그-칸을-고쳐-쓴다) — 커널 쪽 `MiRelocateImage`, ASLR·델타
  - [run19 해설 2.6](run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1) — exe 재배치 갈림
  - [run21 해설 2.4](run21-load-dll-internal-해설.md#24-이름-사슬-한-칸씩-읽는-법--knowndll-길-파일-길) — `ntdll+0xa399` 를 Q20 멈춤점으로 정한 곳
  - [run36 해설](run36-snap-module-해설.md) — 다음 걸음, 주소 잇기 (IAT)
- 01_sprint: [`runs/01_sprint/run8-dllcheck.md`](../01_sprint/run8-dllcheck.md) — "DLL 주소는 실행마다 바뀔 수 있다" (이번 질문의 출발점)

# run37 k32-import-order — 해설

> 이 글은 [run37 문서](run37-k32-import-order.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q51** — 2.5 kernel32 import 서술자 **순서**: kernelbase 를 처음 들이는 import 바퀴가 이름 그대로 `KERNELBASE.dll` 항목인가, 앞쪽 api-set 항목(→ ApiSet 으로 KERNELBASE)인가 (`_workspace/plan.md`, 이번에 **끝**)
  - 새 대기열 번호는 없다. 실행 때 확인할 거리는 **Q20** 에 출처로 붙었다.
  - 다음 항목: **Q52** (`LdrpMinimalMapModule`·`LdrpCompleteMapModule`, 정적).
- 근거:
  - run 문서 [`run37-k32-import-order.md`](run37-k32-import-order.md) (가장 중요)
  - 요청서 [`_workspace/sast/32_k32-import-order.md`](../../_workspace/sast/32_k32-import-order.md)
  - 결과서 [`_workspace/sast/32_k32-import-order_result.md`](../../_workspace/sast/32_k32-import-order_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다.
  - 그리고 이번엔 **Ghidra 도 안 썼다.** 함수 몸통은 하나도 안 읽었다.
  - `pefile` (PE 파일의 표를 읽어 주는 파이썬 도구) 로 MS 바이너리 세 개의 **표만** 직접 읽었다: `kernel32.dll`, `KernelBase.dll`, `apisetschema.dll`.
  - 서술자는 원본 바이트로 20바이트씩 한 번 더 읽어서, `pefile` 이 준 순서와 같은지 맞춰 봤다.

### 태그 하나 — `[정적: PE 표]` 와 `[정적+샘플]` 은 무엇이 다른가

- **`[정적: PE 표]`** = **MS 바이너리**(kernel32·KernelBase·apisetschema)의 표를 직접 읽은 값. sample 파일은 안 섞었다.
- **`[정적+샘플]`** (run36) = 우리가 빌드한 **sample 파일**(test.exe·dll1·dll2)의 표까지 섞어서 센 값.
- 한 줄로: 둘 다 코드 줄이 아니라 "파일에 적힌 값" 인데, **누구 파일이냐**가 다르다 — MS 것만이면 `PE 표`, 우리 sample 이 끼면 `+샘플`.
- 공통점: 둘 다 **실행 때 정말 그 표대로 갔는지는 모른다.** (run28 의 `[정적: PE 파일]` 도 `[정적: PE 표]` 와 같은 종류라고 flow.md 표시 규칙에 적혔다.)
- run 문서의 표 "표시" 칸은 그냥 `[정적]` 으로 뒀다. 이 글 본문에선 `[정적: PE 표]` 로 쓴다.

---

이 글은 [run25 해설](run25-dll-entry-해설.md) 2.5 의 **뒷이야기**다.

- run25 에서 kernel32 의 "기대는 건물 목록" 에 **별명 102줄 + 본명 2줄**(`ntdll.dll`, `KERNELBASE.dll`)이 있다는 걸 셌다.
- 그런데 **줄 순서**를 안 봐서, "kernelbase 를 **처음** 부르는 줄이 어느 것인가" 가 `[공백]` 으로 남았다.
- 이번엔 그 목록을 **위에서부터 순서대로** 펴 본 것이다. 작은 바퀴라 짧게 간다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, 태그 → [run9 해설](run9-create-user-process-해설.md) 2절 · rva → [run10 해설](run10-allocate-process-해설.md) 2절
- import 표(**기대는 건물 목록**), import 서술자(그 목록의 **한 줄**), 세는 고리·체크판(`+0x6c`) → [run22 해설](run22-map-snap-dep-해설.md) 2.1·2.4·2.6
- 배달 기사 `LdrpLoadDependentModuleInternal`, 간선(카드 사이 줄), "줄을 안 긋는 경우 셋", api-set 풀이 `+0x2ff` → [run23 해설](run23-load-dependent-해설.md) 2.3·2.5
- kernel32 import 표 개수, delay import → [run25 해설](run25-dll-entry-해설.md) 2.5
- 별명 사전의 "누가 묻나" 예외 (`processthreads`) → [run34 해설](run34-parent-leftovers-해설.md) 2.3
- ntdll 은 시작 때 이미 명부에 있다, ntdll 카드 도장 9 → [run35 해설](run35-ntdll-entry-해설.md#210-질문-6--보통-프로세스에서-명부에-ntdll-하나--맞다) · [2.8](run35-ntdll-entry-해설.md#28-질문-4--마무리-점검을-같이-받고-도장은-9)
- IAT(**빈 주소록**), forwarder(**다른 건물에 있다는 안내문**), `[정적+샘플]` → [run36 해설](run36-snap-module-해설.md) 맨 위·2.12

비유도 그대로 이어 쓴다.

- 프로세스 = **가게**, ntdll = 가게 안 **관리실**, DLL = **건물**
- 모듈 항목 = **명부 한 줄**, DDAG 노드 = **연결 카드**, `State` = **진행 도장**
- `LdrpMapAndSnapDependency` = **건물 담당**, `LdrpLoadDependentModuleInternal` = **배달 기사**
- import 표 = **기대는 건물 목록**, 서술자 = 그 목록의 **한 줄**
- IAT = 건물마다 가진 **빈 주소록** (run36)
- **api-set = 별명**, **`apisetschema.dll` (ApiSet 맵) = 별명 사전** (사용자에게 설명한 그림 그대로)
  - 앞 해설(run23·run34)은 같은 것을 "별명 대조표" 라고 불렀다. 가리키는 건 같다.
  - 실행 때 관리실이 펴 보는 건 PEB 에 붙은 사전(`PEB.ApiSetMap`)이다. 이번에 읽은 `apisetschema.dll` 은 **같은 OS 의 사전 파일**이다 (run 문서).
- run 문서는 이번에 "**이삿짐 목록**·**동네 안내판**" 비유를 썼다. 이 글은 위 그림으로 옮긴다 (가리키는 건 같다).
  - 이삿짐 목록 = 기대는 건물 목록 (import 표)
  - 동네 안내판 = 별명 사전 (ApiSet 맵). 이 시리즈에서 "안내판" 은 PEB 라서 겹치지 않게 바꿨다.
  - 심부름꾼 = 관리실 (ntdll) 의 건물 담당·배달 기사

한 줄로 먼저 말하면:

- kernel32 의 목록을 위에서부터 보니, **kernelbase 를 처음 부르는 줄은 3번(0부터), 본명 그대로 `KERNELBASE.dll`** 이다 `[정적: PE 표]`.
- 앞의 0·1·2번 줄은 다 관리실(ntdll) 이야기다. 별명으로 kernelbase 를 가리키는 첫 줄은 4번이다.
- 그리고 덤으로: kernel32 를 들일 때 **새로 오는 건물은 kernelbase 하나**뿐이고, kernelbase 는 관리실 말고 **아무도 더 안 부른다** `[정적: PE 표]`.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

숫자는 앞 run 들 그대로다.

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessW … NtCreateUserProcess … NtResumeThread      (run9 ~ run14, run34)

 test.exe 메인 스레드 (커널) → IRETQ                                          (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess                            (run17 ~ run19)
        └─ +0x19a2  LdrpInitializeKernel32Functions                            (run20)
              └─ +0xc0 LdrLoadDll("KERNEL32.DLL") → … kernel32 갖다 놓기       (run20 ~ run21)
                    └─ LdrpMapDllWithSectionHandle +0x1c8
                         LdrpMapAndSnapDependency (kernel32 문맥)              (run22)
                          └─ 서술자 고리: 줄마다 +0x34e LdrpLoadDependentModuleInternal   (run22·run23)
                               ← 이번: 그 "줄마다" 의 **순서**
        └─ … dll1·dll2 짐 들이기 → … → main
```

- flow.md 로는 **2.5** (kernel32 준비) 의 "kernelbase 가 따라 올라온다" 줄이다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. **본 질문**: kernelbase 를 처음 들이는 줄은 본명 `KERNELBASE.dll` 줄인가, 그보다 앞의 별명 줄인가?
2. 덤 넷:
   - kernelbase·ntdll 말고 **다른 건물**로 풀리는 줄이 있나? (있으면 그 건물도 같이 와야 한다)
   - IAT 첫 칸이 0 인 줄이 있나? (run22 세는 고리가 빼는 것)
   - kernelbase 의 목록에는 무엇이 있고, 다른 건물을 더 부르나?
   - 이 순서대로라면, 나중에 디버거로 볼 때 **어떤 순서로 이름이 찍혀야** 하나?

### 바퀴 전에 알던 것

- `[정적]` (run22) 건물 담당은 목록을 **파일에 적힌 순서대로** 한 줄씩 돌고, 매 줄 **조건 없이** `+0x34e` 에서 배달 기사를 부른다 (고리 `+0x1d0 ~ +0x3a3`).
- `[정적]` (run23) 별명은 배달 기사 안 `+0x2ff ApiSetpResolveHost` (옛 사전 모양이면 `+0x22b5`/`+0x22e0`) 에서 진짜 이름이 된다.
- `[정적]` (run25) kernel32 일반 import 104줄 = 별명 102 + `ntdll.dll`(366 함수) + `KERNELBASE.dll`(93 함수). **순서는 안 봤다.**
- `[정적: PE 표]` (run34) `api-ms-win-core-processthreads-l1-1-*` 는 보통 `kernel32.dll` 로 풀리지만, **묻는 쪽이 kernel32 면** `kernelbase.dll` 로 풀린다.
- `[추측]` (run25) 앞쪽 별명 줄이 KERNELBASE 로 풀려서 본명 줄보다 **먼저** 들일 수도 있다. `[공백]` 어느 쪽인지 모름.

이 바퀴는 **이 마지막 줄 하나를 닫는 것**이 목적이었다. 코드 쪽 전제(run22·run23)는 이번에 새로 읽지 않았다.

---

## 2. 찾은 것

### 2.1 한눈에 — 목록 앞 다섯 줄

run 문서 표에서 앞 다섯 줄만 옮긴다. 모두 `[정적: PE 표]`. 줄 번호는 **0부터**, 줄 #N 의 파일 자리는 `0xb535c + 0x14×N` 이다.

| # | 파일 | 목록에 적힌 이름 | 함수 수 | 진짜 건물 | 어떻게 |
|---|---|---|---|---|---|
| 0 | `0xb535c` | `api-ms-win-core-rtlsupport-l1-1-0.dll` | 12 | `ntdll.dll` | 별명 → 기본 |
| 1 | `0xb5370` | `api-ms-win-core-rtlsupport-l1-2-2.dll` | 1 | `ntdll.dll` | 별명 → 기본 |
| 2 | `0xb5384` | **`ntdll.dll`** | 366 | `ntdll.dll` | 본명 |
| **3** | `0xb5398` | **`KERNELBASE.dll`** | 93 | `KERNELBASE.dll` | **본명** |
| 4 | `0xb53ac` | `api-ms-win-core-processthreads-l1-1-3.dll` | 4 | `kernelbase.dll` | 별명 → **kernel32 예외** |

- 그 뒤로 #5 ~ #100 은 다 kernelbase 쪽, #101 은 다시 ntdll (`apiquery`), #102·#103 은 kernelbase. 맨 끝은 #103 이다.
- 104줄 전부는 결과서에도 없다 (요청서대로 앞쪽·경계·끝만).
- **새 말 — 끝 표시**: 서술자 줄들 맨 뒤에 붙은 **빈 한 줄**. 여기서 목록이 끝난다는 표시다. kernel32 목록 크기 `0x834` = 서술자 104줄 + 끝 표시 1줄 (한 줄 `0x14` = 20바이트) `[정적: PE 표]`.
  - 비유: 장부 마지막 줄에 그어 둔 **빈 줄 한 칸**.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — #3 줄

| # | 파일 | 이름 | 함수 수 | 진짜 DLL | 풀린 방법 |
|---|---|---|---|---|---|
| 3 | `0xb5398` | `KERNELBASE.dll` | 93 | `KERNELBASE.dll` | 진짜 이름 (문자열 rva `0xba1a2`, 대문자) |

- **누가**: kernel32 의 기대는 건물 목록.
- **어디서**: 목록 시작 `0xb535c` 에서 20바이트 × 3 뒤인 `0xb5398`. 0부터 세서 **넷째 줄**이다.
- **누구를**: 건물 이름 칸이 rva `0xba1a2` 를 가리키고, 거기 적힌 글자가 대문자 `KERNELBASE.dll` 이다. 별명이 아니라 **본명**.
- **무엇을 위해**: 이 줄에서 kernelbase 의 함수 **93개**를 가져온다.
- 원본 바이트로도 읽었다 (결과서): `905b0b00 00000000 00000000 a2a10b00 88a50800`.
  - 다섯 칸 중 넷째가 이름 rva `0xba1a2`, 다섯째가 FirstThunk(IAT, 빈 주소록) `0x8a588`, 첫째가 OriginalFirstThunk(이름 쪽지 목록) `0xb5b90` 이다.
  - 바이트가 거꾸로 보이는 건 작은 자리부터 적는 방식이라서다 (`a2a10b00` → `0x000ba1a2`).

#### 한 줄 읽기 ② — #4 줄

| # | 파일 | 이름 | 함수 수 | 진짜 DLL | 풀린 방법 |
|---|---|---|---|---|---|
| 4 | `0xb53ac` | `api-ms-win-core-processthreads-l1-1-3.dll` | 4 | `kernelbase.dll` | api-set **kernel32 예외** (기본은 `kernel32.dll`) |

- **누가**: 같은 목록, 바로 다음 줄.
- **누구를**: 별명 `processthreads-l1-1-3`. 별명 사전을 펴면 보통은 `kernel32.dll` 인데, **묻는 쪽이 kernel32** 라서 `kernelbase.dll` 로 풀린다.
- **무엇을 위해**: 함수 4개를 가져온다.
- 이게 **별명으로 kernelbase 를 가리키는 첫 줄**이다. 그런데 #3 보다 **뒤**다. 2.3 에서 이게 왜 중요한지 본다.

### 2.3 질문 답 — 처음 들이는 줄은 3번, 본명 그대로

run25 해설 2.5 에서 길이 둘이었다.

- 길 A: 본명 `KERNELBASE.dll` 줄
- 길 B: 그보다 **앞에 있는** 별명 줄이 KERNELBASE 로 풀림

한 단계씩:

1. 건물 담당은 목록을 **위에서부터** 한 줄씩 배달 기사에게 맡긴다 `[정적]` (run22).
2. #0·#1 은 별명이지만 사전을 펴면 **ntdll** 이다. #2 는 본명 `ntdll.dll` 이다 `[정적: PE 표]`.
3. ntdll 은 가게가 시작할 때 이미 명부에 있다 `[정적]` (run35). 그러니 이 세 줄은 **새 건물을 안 부른다.**
4. #3 이 본명 `KERNELBASE.dll` 이다 `[정적: PE 표]`. 여기서 처음으로 kernelbase 를 찾는다.
5. 별명으로 kernelbase 를 가리키는 첫 줄은 #4 다 `[정적: PE 표]`. #3 보다 뒤다.
6. → **길 A 다.** kernelbase 를 처음 들이는 건 #3, 본명 줄이다 `[정적: PE 표]`.
7. → run25 의 `[추측]` "앞쪽 별명 줄이 먼저 들일 수도" 는 **아니었다.**

- 결과서가 하나 더 짚은 전제: kernel32 를 들이기 **전에** 다른 건물이 KERNELBASE 를 먼저 올려 두지 않았을 것. test.exe 의 목록은 이름만 봤는데 `dll1.dll`, `KERNEL32.dll` 둘이라 KERNELBASE 를 직접 부르지 않는다. dll1·dll2 쪽은 run36 이 센 것을 믿었다.

비유로 한 번 더:

- 배달 기사가 목록 위에서부터 짐표를 한 장씩 든다.
- 1·2·3번째 짐표는 다 "관리실" 행이다. 관리실은 원래 가게 안에 있으니 바로 끝.
- **4번째 짐표에 "KERNELBASE 건물" 이 본명으로** 적혀 있다. 여기서 처음 그 건물을 부른다.
- 5번째부터는 별명 짐표인데, 사전을 펴 보면 "KERNELBASE" 다. 이미 4번째 짐표 때 부른 건물이다.

#### 코드 길로 그리면

호출 자리는 run22·run23 `[정적]`, 줄 순서·이름은 이번 PE 표 `[정적: PE 표]`. 실행은 안 봤다.

```
LdrpMapDllWithSectionHandle [kernel32]
 └─ +0x1c8  LdrpMapAndSnapDependency (kernel32 문맥)
      └─ 서술자 고리 (파일 순서, 104 바퀴)
           ├─ #0  +0x34e  배달 기사("…rtlsupport-l1-1-0")    → +0x2ff 별명 풀이 → ntdll (이미 있음)
           ├─ #1  +0x34e  배달 기사("…rtlsupport-l1-2-2")    → +0x2ff 별명 풀이 → ntdll (이미 있음)
           ├─ #2  +0x34e  배달 기사("ntdll.dll")             별명 아님 → ntdll (이미 있음)
           ├─ #3  +0x34e  배달 기사("KERNELBASE.dll")        별명 아님 → ★ 처음 들임 (새 명부 줄)
           ├─ #4  +0x34e  배달 기사("…processthreads-l1-1-3") → +0x2ff 별명 풀이 → kernelbase (#3 이 만든 줄)
           └─ … #103
```

- `+0x2ff` 를 타느냐는 run23 의 조건 ("이름이 api-set 이면") 으로 짐작한 것이다 `[추측]`. #2·#3 은 이름이 `api-`/`ext-` 로 시작하지 않는다.
- 그래서 flow.md 2.5 의 "kernelbase 가 따라 올라오는" 사슬도 처음 들이는 바퀴 기준으로 고쳐 썼다: `+0x34e LdrpLoadDependentModuleInternal("KERNELBASE.dll", ..)` → (`+0x2ff` 안 탐 `[추측: run23 조건]`) → 새 명부 줄 → KnownDll 이면 `+0x1792` 안에서 올라옴.

### 2.4 별명 사전의 예외 줄 — 누가 묻느냐

#### 새 말 — host, 예외 host

- **host** = 별명이 가리키는 **진짜 건물(DLL) 이름**. 별명 사전에서 별명 한 줄의 "값" 이다.
- **예외 host** = 같은 별명 줄에 붙은 "**단, ○○ 가 물으면 이 건물**" 값 (run34 해설 2.3 에서 본 그 칸).
- 이번 결과서는 별명을 풀 때 **묻는 쪽 = kernel32** 로 풀었다. 예외 목록에 kernel32 가 있으면 예외 값, 없으면 기본 값 (run 문서 "방법").
- 비유: 사전의 "OO 접수처 → 보통은 kernel32 건물. **단, kernel32 건물이 물으면 KERNELBASE 건물**" 줄.

#### 이번에 본 것 `[정적: PE 표]`

- kernel32 목록에서 **예외가 실제로 맞은 별명은 7줄**뿐이다.
  - #4 ~ #7 `processthreads` (4줄)
  - #25·#26 `io-l1-1-0/1` (2줄)
  - #57 `util-l1-1-0` (1줄)
- 나머지 별명 줄은 사전에 값이 **하나뿐**이다. 묻는 쪽과 상관없다.
- 예외가 없었다면 이 일곱 줄은 `kernel32.dll` — **자기 자신**으로 풀렸을 것이다.
  - 결과서 말로는 이 7개 예외가 "자기 자신으로 풀리는 것" 을 **막아 준다.** 실제로 kernel32 목록엔 자기 자신으로 풀리는 줄이 하나도 없다.

#### 사전을 두 방법으로 찾아봤다

- **새 말 — 해시로 찾기**: 이름을 숫자 하나로 바꿔(해시) 그 숫자로 사전 칸을 바로 찾는 방법. 이 사전은 해시 표가 `+0xaa30`, 해시 곱이 `0x1f` 다 `[정적: PE 표]`.
  - 비유: 이름 첫 글자로 바로 그 쪽을 펴는 **색인 탭**.
- 결과서는 이 방법과 **이름을 하나씩 비교하는 방법**을 둘 다 했다.
- 찾을 이름 105개 (kernel32 102 + kernelbase 3) 가 **둘 다 같은 사전 줄**로 나왔다. 못 찾은 이름은 없다 `[정적: PE 표]`.
- 사전은 버전 6, 항목 984개 `[정적: PE 표]`.
- 그리고 run25 숫자(104 = 102 + 2, delay import 3개 이름), run34 규칙(`processthreads` 예외)과 **둘 다 맞았다.** 같은 빌드 파일을 본 게 맞다는 확인이다.

### 2.5 새로 오는 건물은 kernelbase 하나

104줄을 다 풀면 이렇게 된다 `[정적: PE 표]`.

| 진짜 건물 | 줄 수 | 번호 | 함수 수 |
|---|---|---|---|
| `ntdll.dll` | 4 | #0·#1·#2·#101 | 380 (12+1+366+1) |
| `KERNELBASE.dll`/`kernelbase.dll` | 100 | #3 (본명, 93) + 별명 99개 (#4 ~ #100, #102, #103) | 894 (93+801) |
| 그 밖 | **0** | — | — |
| 합 | 104 | | 1274 |

- 별명 102 = kernelbase 99 + ntdll 3. run25 의 102 와 같다.
- 없는 것 넷 `[정적: PE 표]`:
  - 다른 건물로 풀리는 줄
  - **빈 host** (사전 값이 비어서 풀 곳이 없는 줄)
  - 사전에 없는 별명
  - 자기 자신(`kernel32.dll`)으로 풀리는 줄
- → **kernel32 를 들일 때 새로 와야 할 건물은 KERNELBASE 하나**다. ntdll 은 이미 있다.
- 서수(번호만) import 도 kernel32 엔 없다. 다 이름 쪽지다.

#### 덤 — delay import 셋 (이번 이야기엔 안 끼임)

[delay import](run25-dll-entry-해설.md) 는 "필요하면 그때 부를게요" 메모다. 일반 import 고리엔 안 나온다. 결과서가 덤으로 kernel32 셋을 풀어 봤다 `[정적: PE 표]`:

- `ext-ms-win-oobe-query-l1-1-0.dll` → **빈 host**
- `ext-ms-win-packagevirtualizationcontext-l1-1-0.dll` → `daxexec.dll`
- `RPCRT4.dll` → 이름 그대로

실제로 언제 부르는지는 이번 범위 밖이다.

### 2.6 빈 줄 없음 — 세는 고리는 104 를 다 센다

[run22 해설](run22-map-snap-dep-해설.md) 2.4 에서, 건물 담당이 배달 기사를 부르기 전에 **먼저 줄 수를 세는 짧은 고리**(`+0x122 ~ +0x14d`, 호출 없음)를 봤다. 그 고리는 IAT(빈 주소록) 첫 칸이 0 인 줄을 "실제로 올릴 수" 에서 뺀다.

이번에 본 것 `[정적: PE 표]`:

- kernel32 104줄 **모두** 빈 주소록 첫 칸이 파일에서 0 이 아니다. (파일 속에선 이름 쪽지 목록 첫 칸과 같은 값이 들어 있다.)
- 함수 수도 모두 1 이상이다 (가장 작은 게 1).
- 이름 rva 나 주소록 rva 가 0 이라 **중간에 끊기는 줄**도 없다. 끝 표시는 #104 자리 `0xb5b7c` 하나뿐.

그래서 `[정적: PE 표 + run22 코드]`:

- 세는 고리는 서술자 수 **104**, 실제로 올릴 수 **104** 로 끝난다. 빼는 게 0개.
- run22 의 `+0x17b` 규칙대로면:
  - 자식 칸 배열 = 8바이트 × 104
  - 지시서 `+0x68` = 104
  - 체크판 `+0x6c` = **105** (핫패치 꺼진 경우. 켜졌으면 106)
- 번호가 건너뛰지 않으니 **서술자 번호 = 자식 칸 번호**다.
- 비유: 짐표 104장 중 **빈 짐표가 한 장도 없다.** 체크판엔 "짐 104 + 나 1" 이 적힌다.

### 2.7 kernelbase 의 목록 — 자기 자신을 부르는 별명 하나

kernelbase 도 짧게 봤다 `[정적: PE 표]`. 목록은 **4줄뿐**이다 (크기 `0x64` = 4줄 + 끝 표시).

| # | 파일 | 이름 | 함수 수 | 진짜 건물 | 어떻게 |
|---|---|---|---|---|---|
| 0 | `0x3a1b1c` | `ntdll.dll` | 743 (서수 1개 포함) | `ntdll.dll` | 본명 |
| 1 | `0x3a1b30` | `api-ms-win-eventing-provider-l1-1-0.dll` | 5 | **`kernelbase.dll` (자기 자신)** | 별명, 값 하나뿐 |
| 2 | `0x3a1b44` | `api-ms-win-core-apiquery-l1-1-0.dll` | 1 | `ntdll.dll` | 별명 → 기본 |
| 3 | `0x3a1b58` | `api-ms-win-core-apiquery-l1-1-2.dll` | 1 | `ntdll.dll` | 별명 → 기본 |

- 함수 합 750. 빈 주소록 첫 칸 0 인 줄 없음.
- 눈여겨볼 건 **#1** 이다. kernelbase 가 **자기 자신의 별명**을 목록에 적어 뒀다. 사전 값이 `kernelbase.dll` 하나뿐이다 (항목 `+0x1730`, 값 `+0x7010`).
  - 비유: KERNELBASE 건물 목록에 "OO 행사 담당" 이라는 별명이 있는데, 사전을 펴면 **"KERNELBASE 건물"** — 자기 건물이다.
- 그래서 kernelbase 가 더 부르는 건물은 **없다.**
- 사슬이 이렇게 닫힌다 `[정적: PE 표]`:

```
kernel32   → { ntdll, kernelbase }
kernelbase → { ntdll, (자기 자신) }
```

- kernelbase 의 delay import 는 56개다 (`ext-ms-win-…`/`api-ms-win-…` 54개 + `WINTRUST.dll`, `OLEAUT32.dll`). 일반 import 고리엔 안 나온다.

### 2.8 이어 붙인 짐작 — 간선은 하나일 것

여기부터는 run 문서 "이어 붙인 짐작" 이다. 이번 PE 표와 앞 run 의 코드를 **이은 것**이라 전부 `[추측]` 이다.

[run23 해설](run23-load-dependent-해설.md) 2.3 의 "줄(간선)을 안 긋는 경우 셋" 을 다시 꺼낸다 `[정적]`:

1. 부모 카드 == 자식 카드
2. 자식 `State == 9` 이고 (`LoadCount == -1` 이거나 `ProcessStaticImport` 비트)
3. 부모 카드에 **이미 같은 자식 줄이 있음**

여기에 이번 표를 대 보면:

- **kernelbase 쪽 100바퀴**: #3 이 처음 줄을 긋는다. 나머지 99바퀴는 3번 "이미 같은 줄 있음" 에 걸린다 `[추측]`.
- **ntdll 쪽 4바퀴**: ntdll 카드 도장은 9 다 (run35 `[정적]`). 그래서 2번에 걸릴 **수** 있다. 다만 나머지 조건(`LoadCount == -1` 또는 `ProcessStaticImport`) 값은 안 봤다 `[추측]`.
- → kernel32 카드에 남는 줄은 **KERNELBASE 하나**일 것이다 `[추측]`.
- 이건 run7 `[관찰]` 의 재귀 모양 (KERNEL32 아래 자식 하나만 재귀) 과 맞는다.
- kernelbase 의 #1 (자기 자신 별명) 바퀴는 1번 "부모 == 자식" 에 걸려 줄이 안 생길 것이다 `[추측]`. ntdll 이 "자기 자신으로 풀린 이름" 을 실제로 어떻게 다루는지는 **안 읽었다.**

비유: KERNELBASE 건물 카드와 kernel32 카드 사이엔 **끈이 한 가닥**만 매인다. 같은 건물 짐표가 99장 더 와도 끈을 두 번 매지 않는다. 관리실 행 짐표 4장은 "다 켜진 관리실" 이라 끈을 안 맬 수도 있다.

### 2.9 run36 셈에 보탬 하나

- [run36 해설](run36-snap-module-해설.md#212-testexe-에서는--파일을-세어-본-값) 2.12 에서, kernel32·kernelbase 의 import 를 "**별명은 전부 KernelBase 로 풀린다**" 고 **가정하고** 안내문(forwarder)을 셌다. 결론은 "안내문 대상은 NTDLL 뿐" 이었다 `[정적+샘플, 가정 있음]`.
- 이번 표로 보면 그 가정은 **거의** 맞지만 정확하진 않다 `[정적: PE 표]`:
  - kernel32 별명 3줄 (#0·#1·#101, 함수 14개)
  - kernelbase 별명 2줄 (#2·#3, 함수 2개)
  - → 이 다섯 줄은 KernelBase 가 아니라 **ntdll** 로 풀린다.
- 이 16개 함수가 ntdll 이름표에서 안내문인지는 **아무도 안 셌다.**
- 그래서 run36 결론을 **바꿀 근거도, 받칠 근거도 이번엔 없다.** 그대로 Q68 에서 본다.

### 2.10 앞 해설에 붙인 정정

이번에 닫히거나 좁혀진 말이 예전 그대로 남은 해설 셋의 맨 위에 `> **정정 (run37)**: …` 한 줄씩 넣었다. 본문은 그대로다.

| 해설 | 예전 말 | 이번 |
|---|---|---|
| [run25](run25-dll-entry-해설.md) 2.5·4절 | "102개 별명 중에도 KERNELBASE 로 풀리는 게 있을 수 있다 `[추측]`", "어느 길로 **처음** 들어오나 `[공백]`" | 99개 맞음, 처음은 **길 A (#3 본명)** `[정적: PE 표]` |
| [run23](run23-load-dependent-해설.md) 2.5 | 남은 `[추측]` "그게 `KERNELBASE.dll` 로 풀리나", 사슬 그림의 "api-set 이름 ? → `+0x2ff` 에서 KERNELBASE 로" | 같은 OS 사전 파일로는 99개 kernelbase · 3개 ntdll `[정적: PE 표]`. 처음 들이는 바퀴는 별명이 아니라 #3 본명 |
| [run36](run36-snap-module-해설.md) 2.12 | "api-set 호스트를 KernelBase 로 가정" | 별명 5줄(함수 16개)은 ntdll 로 풀림. 결론은 그대로 Q68 |

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 자리 | 전 | 후 |
|---|---|---|
| 맨 위 정적 근거 | run9 ~ run36 | run9 ~ run37. "run37 kernel32·KernelBase·apisetschema **PE 표만** (Ghidra 안 씀)" + 서술자 순서·예외 host 를 `pefile`·해시/이름 비교로 읽었다는 줄 |
| 표시 규칙 표 | 없음 | 새 줄 **`[정적: PE 표]`** — MS 바이너리 PE 표를 직접 읽은 값, sample 안 섞음, run28 `[정적: PE 파일]` 과 같은 뜻 |
| 2.5 제목 | … run28 | … run28, **run37** |
| 2.5 "kernelbase 가 따라옴" | `[정적]` run25: 본명 줄이 있다 | + `[정적: PE 표]` **따라오는 건 kernelbase 하나뿐.** ntdll 4 + kernelbase 100, 다른 건물·빈 host·자기 자신 없음. 예외 host 7곳. kernelbase 는 ntdll + 자기 자신만. 세는 고리 104/104 (`+0x6c` = 105) |
| 2.5 delay import 줄 | 이름 셋만 | + 빈 host / `daxexec.dll` / 이름 그대로, kernelbase delay 56개 |
| 2.5 run36 안내문 근사 줄 | "host = 전부 KernelBase" 가정 | + 보탬: 별명 5줄(함수 16개)은 ntdll. 결론 그대로 Q68 |
| 2.5 "102개 중에도 KERNELBASE 로 풀리는 게" | `[추측]` | **(해결됨, 정적)** 맞다, 99개. 바퀴 순서 #0·#1 → ntdll, #2 `ntdll.dll`, **#3 `KERNELBASE.dll`**, #4 부터 kernelbase |
| 2.5 사슬 "api-set 이름 ? → KERNELBASE" | 처음 바퀴 `[공백]` | **(해결됨, 정적)** 직접 이름 항목. 사슬을 `("KERNELBASE.dll")` 로 고쳐 씀 (`+0x2ff` 안 탐 `[추측]`). "그게 KERNELBASE 로 풀리나" 는 **(일부, 정적)** — 실행 때 사전이 같은지는 Q20 |
| 2.5 간선 | 없음 | 새 `[추측]` 줄: kernel32 카드에 남는 간선은 KERNELBASE 하나일 것 (2.8) |
| 4장 공백 표 2.5 | "처음 들이는 바퀴" `[공백]` (Q51) | **(해결됨, 정적)**. 새 줄: 실행 때도 이 순서인가 → **Q20** 덧붙임 |
| 4장 그림 2 (1) | run28 까지 근거 | + run37: KERNEL32 아래 새 자식은 KERNELBASE 하나, 그 아래 없음 → 재귀 한 단 모양과 맞음 `[추측]` |
| 근거 표·작업 기록 | — | run37 줄 |

태그로 보면:

- **kernelbase 를 처음 들이는 줄**: `[공백]` → `[정적: PE 표]` (#3 본명).
- **별명 102줄이 어디로 풀리나**: `[추측]` → `[정적: PE 표]` (같은 OS 사전 파일 기준). 실행 때 사전 값은 여전히 동적.
- **kernel32 를 들일 때 오는 건물**: "kernelbase 가 따라온다" → "**kernelbase 하나만** 온다" `[정적: PE 표]`.
- **간선 하나**: 새 `[추측]` (두 정적을 이은 것).

### 큰 그림에서 어떻게 이어지나

- 2.5 (kernel32 준비) 는 이제 노선도(정적)로 볼 수 있는 표는 **다 봤다.**
  - 무엇이 올라오나 (kernel32 + kernelbase, 끝) — 이번
  - 어느 줄에서 처음 오나 (#3) — 이번
  - 어디서 주소록을 채우나 (`+0x3e7`) — run36
  - 어디서 전원을 켜나 (진입점) — run25
- 남은 건 거의 다 **버스 타 보기** (Q20·Q21·Q22) 다. 특히 Q20 에서 이 순서가 그대로 찍히는지 보면 된다 (4절).
- 다음 정적 항목은 **Q52** — 실제로 메모리에 붙이고 마무리하는 쪽 (`LdrpMinimalMapModule`·`LdrpCompleteMapModule`).

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 실행 때도 이 순서로 배달 기사가 불리나 | 파일 표만 봤다. 실행은 안 봤다 | 동적 **Q20** (덧붙임): `ntdll+0x21ab0` (`LdrpLoadDependentModuleInternal` **입구**) 을 부모 KERNEL32 일 때만 `dS @rcx`. 기대 `[추측: 이 표대로면]`: `…rtlsupport-l1-1-0` → `…rtlsupport-l1-2-2` → `ntdll.dll` → **`KERNELBASE.dll`** → `…processthreads-l1-1-3` … |
| 바퀴 번호를 디버거에서 어떻게 셀까 | 입구 인자 위치를 계산한 것뿐 | Q20: 5번째 인자 `[RSP+0x28]` 에서 `poi(@rdx+0x58)` 를 빼고 8로 나눈 값 `[추측: 입구 인자 위치 계산]` (run22: 칸 배열이 지시서 `+0x58`) |
| 별명 풀이가 정말 #3 바퀴엔 안 타고 #4 부터 타나 | run23 조건으로 짐작한 것 | Q20: `ApiSetpResolveHost` ret `ntdll+0x21daf` 이 4번째 바퀴엔 **안 서고**, 1·2번째와 5번째부터 설 것 `[추측]` |
| 실행 때 사전(PEB `ApiSetMap`)이 이 파일과 같나 | 이번에 읽은 건 같은 OS 의 **파일**이다. 부팅 때 같은 파일을 맵한 것일 것 `[추측]` | Q20: 5번째 바퀴 `ntdll+0x21daf` 에서 `dS @rsp+0x108` 가 `…\kernelbase.dll` 이면 kernel32 예외 host 가 실행 때도 맞는 것 |
| 부모 이름 비교가 대소문자를 무시하나 (`kernel32.dll` vs 명부의 `KERNEL32.DLL`) | `ApiSetpResolveHost` 본문은 범위 밖 | 위 Q20 5번째 바퀴 값으로 같이 닫힌다 |
| 자기 자신으로 풀린 별명 (kernelbase #1) 을 ntdll 이 어떻게 다루나 | 그 코드를 안 읽었다. "부모 == 자식" 예외일 것 `[추측]` | Q20 같은 입구, kernelbase 문맥 2번째 바퀴 |
| ntdll 로 풀리는 별명 5줄(함수 16개)이 안내문인가 | 아무도 안 셌다 | **Q68** (run36 결론과 같이) |
| delay import 를 언제 부르나 | 범위 밖 | 대기열 줄 없음 (run 문서도 "범위 밖" 으로만 둠) |

- 디버거 메모 (run 문서): `ntdll+0x21ab0` 은 kernel32 문맥에서만 **104번** 선다 (kernelbase 4번, dll1·dll2·exe 도 따로). 부모 조건 + 횟수 제한이 꼭 필요하다. plan 의 하네스 후보 (50) 에 더해 뒀다.

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run37-k32-import-order.md`](run37-k32-import-order.md) — 특히 "정적 — kernel32 일반 import 서술자 순서", "호출 관계", "이어 붙인 짐작", "아직 확인 필요"
- 결과서: [`_workspace/sast/32_k32-import-order_result.md`](../../_workspace/sast/32_k32-import-order_result.md) — 질문별 답 1 ~ 5, "flow.md 2.5 고치기 (제안)"
- 요청서: [`_workspace/sast/32_k32-import-order.md`](../../_workspace/sast/32_k32-import-order.md)
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - 맨 위 정적 근거·표시 규칙 표 (`[정적: PE 표]`)
  - "2.5" 의 run37 줄들 (따라오는 건 kernelbase 하나, 바퀴 순서, 간선 `[추측]`)
  - 4장 공백 표 2.5 줄 (Q51 해결됨, Q20 덧붙임), 그림 2 (1) 줄
- 이전 해설: [run25 해설](run25-dll-entry-해설.md) 2.5 (바로 앞 이야기), [run23 해설](run23-load-dependent-해설.md) 2.3·2.5 (간선, 별명 풀이), [run34 해설](run34-parent-leftovers-해설.md) 2.3 (누가 묻나 예외), [run36 해설](run36-snap-module-해설.md) 2.12 (`[정적+샘플]`)

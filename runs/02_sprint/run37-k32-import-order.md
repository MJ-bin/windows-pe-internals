# 2026-10-24 run37 — kernel32 import 서술자 **순서**: kernelbase 를 처음 들이는 건 **#3 `KERNELBASE.dll` (이름 그대로)**, 다른 DLL 로 풀리는 서술자 없음, kernelbase 는 ntdll + 자기 자신만 (정적: PE 표)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- **Ghidra 안 씀.** 결과서가 `pefile` 2024.8.26 으로 MS 바이너리의 PE 표를 직접 읽었다. 서술자는 20바이트씩 원본 바이트로 한 번 더 읽어 `pefile` 순서와 같음을 확인했다. 임시 스크립트·표 전체는 세션 임시 폴더에만 있다 (커밋 안 함).
- 파일 (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`):

| 파일 | 읽은 표 | 자리 |
|---|---|---|
| `kernel32.dll` | 일반 import 디렉터리 (delay import 는 개수·이름만) | rva `0xb535c` (`.rdata`, rva = 파일 오프셋), 크기 `0x834` = 서술자 104 + 끝 표시 1. 바운드 import 없음 |
| `KernelBase.dll` | 일반 import 디렉터리 | rva `0x3a1b1c`, 크기 `0x64` = 서술자 4 + 끝 표시 1 |
| `apisetschema.dll` | `.apiset` 구역 (ApiSet 맵) | rva `0x3000` (파일 `0x2000`). 버전 6, 항목 984개, 항목 표 `+0x98`, 해시 표 `+0xaa30`, 해시 곱 `0x1f` |

- 버전 대조 (결과서, Ghidra 함수가 없어서 PE 표끼리): **일치.**
  - run25 숫자와 같다: 일반 import 104 = api-set 102 + `ntdll.dll`(366 함수) + `KERNELBASE.dll`(93 함수). delay import 3개 이름도 같다.
  - run34 규칙과 같다: `api-ms-win-core-processthreads-l1-1-*` 는 기본 `kernel32.dll`, 가져오는 쪽이 kernel32 면 `kernelbase.dll` (항목 `…processthreads-l1-1-8`, 구역 `+0xba8`).
  - ApiSet 이름 찾기를 해시 방식과 이름 하나씩 비교 방식 두 번 했고, 찾을 이름 105개 (kernel32 102 + kernelbase 3) 가 **둘 다 같은 항목**으로 나왔다. 못 찾은 이름 없음.
- 표시: 이 문서의 근거는 코드 줄이 아니라 **MS 바이너리 PE 표를 읽은 값**이다. 그래서 `[정적: PE 표]` 로 적는다. run36 의 `[정적+샘플]` (sample 빌드 파일까지 센 값) 과는 다르다. 표의 "표시" 칸은 `[정적]` 으로 둔다.

## 목적

- Q51 (run25 flow-writer 제안, flow.md 4장 2.5 `[공백]`): kernel32 를 들일 때 **kernelbase 를 처음 들이는 import 바퀴**가 이름 그대로 `KERNELBASE.dll` 항목인가, 앞쪽 api-set 항목(→ ApiSet 으로 KERNELBASE)인가.
- 덤 질문 넷:
  1. kernelbase·ntdll 말고 다른 DLL 로 풀리는 서술자가 있나 (있으면 그 DLL 도 같이 올라와야 함).
  2. IAT 첫 칸이 0 인 서술자가 있나 (run22 의 세는 고리 `+0x122 ~ +0x14d` 가 빼는 것).
  3. kernelbase 의 일반 import 는 무엇이고, 다른 DLL 을 더 끌어오나.
  4. 서술자 순서 = ntdll 고리가 도는 순서이니, 동적으로 볼 때 기대 순서는.

## 방법

- 정적: `_workspace/sast/32_k32-import-order.md` — kernel32·KernelBase PE import 디렉터리, `apisetschema.dll` `.apiset` 구역. depth 없음 (함수 아님).
- 결과서: `_workspace/sast/32_k32-import-order_result.md` (**분석성공**)
- api-set 은 **가져오는 쪽 = kernel32** (kernelbase 표는 가져오는 쪽 = kernelbase) 로 풀었다. host 예외 목록에 그 이름이 있으면 예외 값, 없으면 기본 값.
- 코드 쪽 전제는 이번에 새로 읽지 않았다:
  - ntdll 이 서술자를 **파일 순서대로** 돌고, 매 바퀴 조건 없이 `+0x34e LdrpLoadDependentModuleInternal` 을 부른다 — run22 `[정적]` (`LdrpMapAndSnapDependency` 고리 `+0x1d0 ~ +0x3a3`).
  - api-set 이름은 그 안의 `+0x2ff ApiSetpResolveHost` (옛 맵 `+0x22b5`/`+0x22e0`) 에서 진짜 이름이 된다 — run23 `[정적]`.

## 결과

비유 (이사 = 모듈 올리기):

- kernel32 의 import 표는 **이삿짐 목록**이다. 줄마다 "어느 가게에서 가져올 물건" 이 적혀 있다.
- 가게 이름 중엔 진짜 상호(`KERNELBASE.dll`) 도 있고, **별명**(`api-ms-win-…`) 도 있다. 별명은 동네 안내판(ApiSet 맵)을 봐야 진짜 상호를 안다.
- 안내판엔 "보통은 A 가게, 단 **kernel32 가 물으면 B 가게**" 같은 예외 줄도 있다.
- 심부름꾼(ntdll)은 목록을 **위에서부터** 한 줄씩 처리한다. 그러니 "kernelbase 를 처음 부르는 줄" 은 목록에서 kernelbase 로 가는 **첫 줄**이다.
- 목록을 펴 보니: 0·1·2번 줄은 다 관리실(ntdll, 이미 있음), **3번 줄이 진짜 상호 `KERNELBASE.dll`**. 별명으로 kernelbase 를 가리키는 첫 줄은 4번이다.

### 정적 — kernel32 일반 import 서술자 순서 (일부, `[정적: PE 표]`)

서술자 #N 은 파일 위치 `0xb535c + 0x14×N`. 번호는 0부터.

| # | 파일 | 이름 | 함수 수 | 진짜 DLL | 풀린 방법 | 표시 |
|---|---|---|---|---|---|---|
| 0 | `0xb535c` | `api-ms-win-core-rtlsupport-l1-1-0.dll` | 12 | `ntdll.dll` | api-set 기본 (항목 `rtlsupport-l1-1-1`) | [정적] |
| 1 | `0xb5370` | `api-ms-win-core-rtlsupport-l1-2-2.dll` | 1 | `ntdll.dll` | api-set 기본 | [정적] |
| 2 | `0xb5384` | **`ntdll.dll`** | 366 | `ntdll.dll` | 진짜 이름 | [정적] |
| **3** | `0xb5398` | **`KERNELBASE.dll`** | 93 | `KERNELBASE.dll` | **진짜 이름** (문자열 rva `0xba1a2`, 대문자) | [정적] |
| 4 | `0xb53ac` | `api-ms-win-core-processthreads-l1-1-3.dll` | 4 | `kernelbase.dll` | api-set **kernel32 예외** (기본은 `kernel32.dll`) | [정적] |
| 5 | `0xb53c0` | `api-ms-win-core-processthreads-l1-1-2.dll` | 6 | `kernelbase.dll` | 예외 | [정적] |
| 6 | `0xb53d4` | `api-ms-win-core-processthreads-l1-1-1.dll` | 11 | `kernelbase.dll` | 예외 | [정적] |
| 7 | `0xb53e8` | `api-ms-win-core-processthreads-l1-1-0.dll` | 37 | `kernelbase.dll` | 예외 | [정적] |
| 8 | `0xb53fc` | `api-ms-win-core-registry-l1-1-0.dll` | 42 | `kernelbase.dll` | 기본 | [정적] |
| 9 | `0xb5410` | `api-ms-win-core-heap-l1-1-0.dll` | 13 | `kernelbase.dll` | 기본 | [정적] |
| … | | #10 ~ #100 | | `kernelbase.dll` | 기본 (예외는 #25·#26 `io-l1-1-0/1`, #57 `util-l1-1-0`) | [정적] |
| 101 | `0xb5b40` | `api-ms-win-core-apiquery-l1-1-0.dll` | 1 | `ntdll.dll` | api-set 기본 (항목 `apiquery-l1-1-2`) | [정적] |
| 102 | `0xb5b54` | `api-ms-win-core-delayload-l1-1-1.dll` | 1 | `kernelbase.dll` | 기본 | [정적] |
| 103 | `0xb5b68` | `api-ms-win-core-appcompat-l1-1-1.dll` | 2 | `kernelbase.dll` | 기본 (맨 끝) | [정적] |

- 104줄 전부는 결과서에도 없다 (요청서대로 앞쪽·경계·끝만). 표 전체는 결과서 임시 파일 `k32_imports.txt` (커밋 안 함).
- 근거 바이트 (결과서): 서술자 #3 `905b0b00 00000000 00000000 a2a10b00 88a50800` (OriginalFirstThunk `0xb5b90`, Name `0xba1a2`, FirstThunk `0x8a588`), #4 Name `0xba6c4` → `…processthreads-l1-1-3.dll`.

### 정적 — kernel32 합계 (`[정적: PE 표]`)

| 진짜 DLL | 서술자 수 | 번호 | 함수 수 |
|---|---|---|---|
| `ntdll.dll` | 4 | #0·#1·#2·#101 | 380 (12+1+366+1) |
| `KERNELBASE.dll`/`kernelbase.dll` | 100 | #3 (직접, 93) + api-set 99개 (#4 ~ #100, #102, #103) | 894 (93+801) |
| 그 밖 | **0** | — | — |
| 합 | 104 | | 1274 |

- api-set 합 = kernelbase 99 + ntdll 3 = 102 (run25 와 같음). 서수(번호만) import 는 kernel32 에 없다.
- kernel32 용 **예외 host 가 실제로 맞은 api-set 은 7개**뿐: #4 ~ #7 processthreads, #25·#26 io, #57 util. 예외가 없었다면 이 일곱은 `kernel32.dll` (자기 자신) 으로 풀렸을 것이다. 나머지는 항목에 값이 하나뿐이다.

### 정적 — kernelbase 일반 import (`[정적: PE 표]`)

| # | 파일 | 이름 | 함수 수 | 진짜 DLL | 풀린 방법 | 표시 |
|---|---|---|---|---|---|---|
| 0 | `0x3a1b1c` | `ntdll.dll` | 743 (서수 1개 포함) | `ntdll.dll` | 진짜 이름 | [정적] |
| 1 | `0x3a1b30` | `api-ms-win-eventing-provider-l1-1-0.dll` | 5 | **`kernelbase.dll` (자기 자신)** | api-set, 값 하나뿐 (항목 `+0x1730`, 값 `+0x7010`) | [정적] |
| 2 | `0x3a1b44` | `api-ms-win-core-apiquery-l1-1-0.dll` | 1 | `ntdll.dll` | api-set 기본 | [정적] |
| 3 | `0x3a1b58` | `api-ms-win-core-apiquery-l1-1-2.dll` | 1 | `ntdll.dll` | api-set 기본 | [정적] |

- 함수 합 750. IAT 첫 칸이 0 인 서술자 없음.
- delay import 는 56개 (`ext-ms-win-…`/`api-ms-win-…` 54개 + `WINTRUST.dll`, `OLEAUT32.dll`). 일반 import 고리엔 안 나온다 (로드 시점은 범위 밖).

### 정적 — 세는 고리 값 (`[정적: PE 표 + run22 코드]`)

- kernel32 104개 모두 FirstThunk(IAT) 첫 칸이 파일에서 0 이 아니다 (OriginalFirstThunk 첫 칸과 같은 이름 쪽지 값). 함수 수도 모두 1 이상.
- 이름 rva·FirstThunk 가 0 이라 중간에 끊기는 서술자도 없다. 끝 표시는 #104 자리 (`0xb5b7c`) 하나뿐.
- 그래서 run22 의 세는 고리(`+0x122 ~ +0x14d`)는 서술자 수 104, 실제로 올릴 수 104 로 끝난다 (빼는 것 0).
  - run22 의 `+0x17b` 규칙대로면: 자식 칸 배열 8바이트×104, `ctx+0x68` = 104, `ctx+0x6c` = 105 (핫패치 꺼진 경우. 켜졌으면 106).
  - 번호가 건너뛰지 않으니 **서술자 번호 = 자식 칸 번호** 다.

### 호출 관계 (kernel32 문맥의 import 고리, 앞 다섯 바퀴)

호출 자리는 run22·run23 `[정적]`, 바퀴 순서·이름은 이번 PE 표 `[정적: PE 표]`. 실행은 안 봤다.

```
LdrpMapDllWithSectionHandle [kernel32]
 └─ +0x1c8  LdrpMapAndSnapDependency(kernel32 문맥)                        run22
      └─ 서술자 고리 (파일 순서, 104 바퀴)
           ├─ #0  +0x34e  LdrpLoadDependentModuleInternal("…rtlsupport-l1-1-0")  → +0x2ff ApiSet → ntdll (이미 있음)
           ├─ #1  +0x34e  LdrpLoadDependentModuleInternal("…rtlsupport-l1-2-2")  → +0x2ff ApiSet → ntdll (이미 있음)
           ├─ #2  +0x34e  LdrpLoadDependentModuleInternal("ntdll.dll")           api-set 아님 → ntdll (이미 있음)
           ├─ #3  +0x34e  LdrpLoadDependentModuleInternal("KERNELBASE.dll")      api-set 아님 → ★ 처음 들임 (새 항목)
           ├─ #4  +0x34e  LdrpLoadDependentModuleInternal("…processthreads-l1-1-3") → +0x2ff ApiSet → kernelbase (#3 이 만든 항목)
           └─ … #103
```

- `+0x2ff` 를 타는지는 run23 의 조건 ("이름이 api-set 이면") 으로 짐작한 것 `[추측]`. #2·#3 은 이름이 `api-`/`ext-` 로 시작하지 않는다.

## 확인된 것

- `[정적: PE 표]` **kernelbase 를 처음 들이는 서술자는 #3, 이름은 그대로 `KERNELBASE.dll`** 이다. 앞쪽 api-set 항목이 아니다.
  - #0·#1 은 api-set 이지만 `ntdll.dll` 로 풀리고, #2 는 `ntdll.dll` 이름 그대로. ntdll 은 프로세스 시작 때 이미 명부에 있다 (run35 `[정적]`).
  - kernelbase 로 풀리는 첫 api-set 은 #4 (`processthreads-l1-1-3`, kernel32 예외 host). #3 보다 뒤라 그 바퀴엔 kernelbase 항목이 이미 있다.
  - run25 의 `[추측]` "앞쪽 api-set 항목이 KERNELBASE 로 풀려 먼저 들일 수도" 는 **아니었다.**
- `[정적: PE 표]` kernel32 서술자 104개는 `ntdll.dll` 4개 + `KERNELBASE.dll`/`kernelbase.dll` 100개로 끝난다. **다른 DLL 로 풀리는 것, 빈 host, 맵에 없는 이름, 자기 자신(`kernel32.dll`) 으로 풀리는 것 모두 없다.** → kernel32 를 들일 때 새로 올라와야 할 DLL 은 KERNELBASE 하나다.
- `[정적: PE 표]` IAT 첫 칸 0 인 서술자 없음 → 세는 고리가 104 를 다 센다.
- `[정적: PE 표]` kernelbase 일반 import 4개 = `ntdll.dll` + api-set 셋 (ntdll 둘, **자기 자신 kernelbase 하나**). kernelbase 아래로 더 올라오는 DLL 은 없다.
  - 그래서 일반 import 사슬은 **kernel32 → { ntdll, kernelbase }**, **kernelbase → { ntdll, (자기 자신) }** 로 닫힌다.
- 덤 (delay import, 일반 import 고리엔 안 닿음): kernel32 의 셋을 kernel32 가 가져오는 쪽으로 풀면 `ext-ms-win-oobe-query-l1-1-0.dll` → **빈 host**, `ext-ms-win-packagevirtualizationcontext-l1-1-0.dll` → `daxexec.dll`, `RPCRT4.dll` → 이름 그대로.

## 이어 붙인 짐작 (두 정적을 이은 것, `[추측]`)

- kernelbase #1 (자기 자신으로 풀리는 api-set) 은 run23 의 "간선 안 쓰는 경우 셋" 중 **부모 = 자식** 에 걸려 간선이 안 생길 것이다. ntdll 이 자기 자신으로 풀린 이름을 실제로 어떻게 다루는지는 안 읽었다.
- kernel32 노드의 `Dependencies` 에 남는 간선은 KERNELBASE 하나일 것이다. ntdll 쪽 4바퀴는 "자식 `State == 9` 이고 (`LoadCount == -1` 또는 `ProcessStaticImport`)" 에 걸릴 수 있고 (ntdll `State 9` = run35, `LoadCount` 값은 안 봄), KERNELBASE 쪽 99바퀴는 "이미 같은 간선이 있음" 에 걸린다. run7 `[관찰]` 의 GR 재귀 모양 (KERNEL32 아래 자식 하나만 재귀) 과 맞는다.
- run36 의 `[정적+샘플]` 근사 (kernel32·kernelbase forwarder 셈) 는 "api-set host = 전부 KernelBase" 로 가정했다. 실제로는 kernel32 api-set 3개 (#0·#1·#101, 함수 14개) 와 kernelbase api-set 2개 (#2·#3, 함수 2개) 가 ntdll 로 풀린다. 이 16개 함수의 forwarder 여부는 아무도 안 셌다. 결론 ("forwarder 대상은 NTDLL 뿐") 을 바꿀 근거도, 받칠 근거도 이번엔 없다 → 그대로 Q68 에서.

## 아직 확인 필요

새 대기열 번호 없음. 동적 확인거리는 오케스트레이터가 **Q20** 출처로 붙였다 (같은 실행에서 볼 수 있음).

- **Q20** (동적, 덧붙임):
  - `ntdll+0x21ab0` (`LdrpLoadDependentModuleInternal` 입구, run23) 을 부모가 KERNEL32 일 때만, 이름 `dS @rcx` 를 순서대로 찍기.
    - 기대 `[추측: 이 표대로면]`: 1 `api-ms-win-core-rtlsupport-l1-1-0.dll` → 2 `…rtlsupport-l1-2-2.dll` → 3 `ntdll.dll` → **4 `KERNELBASE.dll`** → 5 `api-ms-win-core-processthreads-l1-1-3.dll` …
    - 바퀴 번호가 필요하면 5번째 인자 `[RSP+0x28]` 에서 `poi(@rdx+0x58)` 를 빼고 8로 나눈 값 `[추측: 입구 인자 위치 계산]` (run22: 칸 배열이 문맥 `+0x58`).
  - `ntdll+0x21daf` (`ApiSetpResolveHost` ret, Q20 에 이미 있음): 4번째 (`KERNELBASE.dll`) 바퀴엔 **안 서고**, 1·2번째와 5번째부터는 설 것 `[추측]`. 5번째에서 `dS @rsp+0x108` 가 `…\kernelbase.dll` 이면 kernel32 예외 host 가 실행 때도 맞는 것.
- 정적으로 못 닫은 것:
  - 실행 때 쓰는 건 PEB 의 `ApiSetMap` 이다. 이 파일은 "같은 OS 의 표" 일 뿐 (부팅 때 같은 파일을 맵한 것일 것 `[추측]`).
  - 부모 이름 비교가 대소문자를 무시하는지 (`kernel32.dll` vs 명부의 `KERNEL32.DLL`) — `ApiSetpResolveHost` 본문은 범위 밖. 위 5번째 바퀴 값으로 닫힌다.
  - 자기 자신으로 풀린 api-set (kernelbase #1) 을 ntdll 이 어떻게 다루나 — 위 "이어 붙인 짐작". 동적이면 kernelbase 문맥에서 같은 입구 2번째 바퀴.

## 디버거 메모 / 도구 메모

- 디버거 안 씀.
- 도구: 하네스 스크립트를 안 썼다. PE 표만 보는 질문은 Ghidra 없이 `pefile` 로 끝났다 (결과서 "스크립트 문제 없음").
- 멈춤점 메모 (다음 dast 용, 제안): `ntdll+0x21ab0` 은 kernel32 문맥만 104번 선다 (kernelbase 4번, dll1·dll2·exe 도 따로). 부모 조건 + 횟수 제한이 필요하다. run36 pitfalls 후보 (50) 와 같은 종류.

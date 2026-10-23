# 32 k32-import-order — 정적 분석 결과

- 요청서: `_workspace/sast/32_k32-import-order.md` (대기열 Q51)
- 날짜: 2026-10-24
- 방법: **Ghidra 안 씀.** `pefile` 2024.8.26 으로 PE 표를 직접 읽음 (서술자는 20바이트씩 원본 바이트로도 한 번 더 읽어 `pefile` 순서와 같음을 확인). 임시 스크립트는 세션 임시 폴더에만 있음 (`.../scratchpad/apiset32.py`, `imports32.py`, 표 전체 `k32_imports.txt`·`kb_imports.txt`). 레포에 남기는 건 이 결과서뿐.
- 파일 (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`):
  - `kernel32.dll` — import 디렉터리 rva `0xb535c` (`.rdata` 안, rva = 파일 오프셋), 크기 `0x834` = 서술자 104 + 끝 표시 1 (×0x14). 바운드 import 없음.
  - `KernelBase.dll` — import 디렉터리 rva `0x3a1b1c`, 크기 `0x64` = 서술자 4 + 끝 표시 1.
  - `apisetschema.dll` — `.apiset` 구역 (rva `0x3000`, 파일 `0x2000`). **버전 6**, 항목 984개, 항목 표 구역 `+0x98`, 해시 표 `+0xaa30`, 해시 곱 `0x1f`.
- 새로 analyze 한 것 없음, 지도·뽑기 없음.
- 버전 확인 (Ghidra 함수가 없어서 "알려진 사실" 표 대신 PE 표끼리 대조): **일치.**
  - run25 숫자와 같다: 일반 import 104 = api-set 102 + `ntdll.dll`(366 함수) + `KERNELBASE.dll`(93 함수). delay import 3개 이름도 같다.
  - run34 규칙과 같다: `api-ms-win-core-processthreads-l1-1-*` 는 기본 `kernel32.dll`, 가져오는 쪽이 `kernel32.dll` 이면 `kernelbase.dll` (항목 `…processthreads-l1-1-8`, 구역 `+0xba8`, 값 둘: `+0x664c` / `+0x6660`).
  - 해시로 찾은 항목과 이름을 하나씩 비교해 찾은 항목이 kernel32 102 + kernelbase 3 = **105개 전부 같다.** 못 찾은 이름 없음. 요청한 버전 번호가 항목 버전보다 큰 이름도 없음.

## 스크립트 문제

없음 (이번엔 하네스 스크립트를 안 썼다).

## 한눈에

1. kernel32 일반 import 104개 중 **kernelbase 를 처음 들이는 건 #3, 이름이 그대로 `KERNELBASE.dll`** 이다. 앞쪽 api-set 항목이 아니다. (#0·#1 은 api-set 이지만 `ntdll.dll` 로 풀리고, #2 는 `ntdll.dll` 이름 그대로.)
2. **kernelbase·ntdll 말고 다른 DLL 로 풀리는 서술자는 없다.** 빈 host 도, 풀리는 곳 없음도 없다.
3. **IAT 첫 칸이 0 인 서술자는 없다.** 104개 전부 세는 고리에 센다.
4. kernelbase 의 일반 import 는 4개뿐: `ntdll.dll`, api-set 셋 (`ntdll.dll` 둘 + **자기 자신 `kernelbase.dll`** 하나). 다른 DLL 을 끌어오지 않는다.

## 질문별 답 [정적: PE 표]

### 1. kernel32.dll 일반 import 서술자 104개의 파일 속 순서

- 답 `[정적: PE 표]`: 서술자 #N 은 파일 위치 `0xb535c + 0x14×N` (아래 표의 "파일"). 번호는 0부터이고, ntdll 의 `LdrpMapAndSnapDependency` 고리가 도는 순서와 같다 (run22 `[정적]`).
- 가져오는 쪽 = `kernel32.dll` 로 api-set 을 풀었다. 풀이가 "기본" 이면 항목 값이 하나뿐이었거나 kernel32 용 예외가 없는 것, "예외" 면 kernel32 용 예외 값이 맞은 것.

| # | 파일 | 이름 | 함수 수 | 진짜 DLL | 풀린 방법 |
|---|---|---|---|---|---|
| 0 | `0xb535c` | `api-ms-win-core-rtlsupport-l1-1-0.dll` | 12 | `ntdll.dll` | api-set 기본 (항목 `rtlsupport-l1-1-1`) |
| 1 | `0xb5370` | `api-ms-win-core-rtlsupport-l1-2-2.dll` | 1 | `ntdll.dll` | api-set 기본 |
| 2 | `0xb5384` | **`ntdll.dll`** | 366 | `ntdll.dll` | 진짜 이름 |
| **3** | `0xb5398` | **`KERNELBASE.dll`** | 93 | `KERNELBASE.dll` | **진짜 이름** (이름 문자열 rva `0xba1a2`, 대문자 `KERNELBASE.dll`) |
| 4 | `0xb53ac` | `api-ms-win-core-processthreads-l1-1-3.dll` | 4 | `kernelbase.dll` | api-set **kernel32 예외** (기본은 `kernel32.dll`) |
| 5 | `0xb53c0` | `api-ms-win-core-processthreads-l1-1-2.dll` | 6 | `kernelbase.dll` | 예외 |
| 6 | `0xb53d4` | `api-ms-win-core-processthreads-l1-1-1.dll` | 11 | `kernelbase.dll` | 예외 |
| 7 | `0xb53e8` | `api-ms-win-core-processthreads-l1-1-0.dll` | 37 | `kernelbase.dll` | 예외 |
| 8 | `0xb53fc` | `api-ms-win-core-registry-l1-1-0.dll` | 42 | `kernelbase.dll` | 기본 |
| 9 | `0xb5410` | `api-ms-win-core-heap-l1-1-0.dll` | 13 | `kernelbase.dll` | 기본 |
| … | | #10 ~ #100 | | `kernelbase.dll` | 기본 (예외 셋: #25·#26 `io-l1-1-0/1`, #57 `util-l1-1-0`) |
| 101 | `0xb5b40` | `api-ms-win-core-apiquery-l1-1-0.dll` | 1 | `ntdll.dll` | api-set 기본 (항목 `apiquery-l1-1-2`) |
| 102 | `0xb5b54` | `api-ms-win-core-delayload-l1-1-1.dll` | 1 | `kernelbase.dll` | 기본 |
| 103 | `0xb5b68` | `api-ms-win-core-appcompat-l1-1-1.dll` | 2 | `kernelbase.dll` | 기본 (맨 끝) |

- 합계 `[정적]`:
  - `ntdll.dll` 로 가는 서술자 **4개** (#0·#1·#2·#101), 함수 `12+1+366+1` = 380.
  - `KERNELBASE.dll`/`kernelbase.dll` 로 가는 서술자 **100개**: 직접 이름 #3 (93 함수) + api-set 99개 (#4~#100, #102, #103; 함수 801). api-set 합 = 99 + 3 = 102 (run25 와 같음).
  - 모든 서술자 함수 합 1274 = 380 + 93 + 801. 서수(번호만) import 는 kernel32 에 없음 (이름 쪽지만).
- kernel32 용 "예외 host" 가 실제로 맞은 api-set 은 **7개**뿐 (#4~#7 processthreads, #25·#26 io, #57 util). 예외가 없었다면 이 일곱은 `kernel32.dll` (= 자기 자신) 로 풀렸을 것이다. 이 일곱 말고는 항목에 값이 하나뿐이다.
- 근거: `kernel32.dll` 파일 `0xb535c` ~ `0xb5b7c` (서술자 배열), 서술자 #3 원본 바이트 `905b0b00 00000000 00000000 a2a10b00 88a50800` (OriginalFirstThunk `0xb5b90`, Name `0xba1a2`, FirstThunk `0x8a588`), #4 `a8710b00 … c4a60b00 a0bb0800` (Name `0xba6c4` → `…processthreads-l1-1-3.dll`); `apisetschema.dll` `.apiset` 항목 `…processthreads-l1-1-8` (`+0xba8`)·`…eventing-provider-l1-1-0` (`+0x1730`)·`…apiquery-l1-1-2` (`+0x2a8`)·`…rtlsupport-l1-1-1` (`+0xe60`).

### 2. kernelbase 를 처음 들이는 서술자

- 답 `[정적: PE 표]`: **#3 번 (0부터), 이름은 이름 그대로 `KERNELBASE.dll`** (대문자). 앞쪽 api-set 항목이 아니다.
  - #0·#1 은 api-set 인데 `ntdll.dll` 로 풀린다. #2 는 `ntdll.dll` 이름 그대로. 셋 다 ntdll 이다 (ntdll 은 프로세스 시작 때 이미 명부에 있다, run35).
  - kernelbase 로 풀리는 **api-set 의 첫 항목은 #4** (`api-ms-win-core-processthreads-l1-1-3.dll`, kernel32 예외) — #3 보다 뒤라서, 그 바퀴가 돌 때 kernelbase 는 이미 새 항목이 만들어진 뒤다.
  - 그래서 run25 의 `[추측]` ("앞쪽 api-set 항목이 먼저 들일 수도") 은 **아니었다.**
- 전제 둘 (이번에 코드를 새로 읽은 건 아님):
  - ntdll 이 서술자를 **파일 순서대로** 도는 것 = run22 `[정적]` (`LdrpMapAndSnapDependency` `+0x1d0~+0x3a3`, 서술자 순서·매 바퀴 `+0x34e` 조건 없음).
  - kernel32 를 들이기 전에 다른 모듈이 KERNELBASE 를 먼저 올려 두지 않았을 것. test.exe 의 import 표는 이름만 봤다: `dll1.dll`, `KERNEL32.dll` (KERNELBASE 를 직접 부르지 않음). dll1·dll2 쪽은 run36 이 센 것을 믿고 이번엔 안 봤다.
- 근거: 질문 1 표 #0~#4, `kernel32.dll` 서술자 #3 의 Name 문자열 rva `0xba1a2` = `KERNELBASE.dll`.

### 3. kernelbase·ntdll 말고 다른 DLL 로 풀리는 서술자

- 답 `[정적: PE 표]`: **없다.** 104개가 `ntdll.dll` 4개 + `KERNELBASE.dll`/`kernelbase.dll` 100개로 끝난다.
  - 빈 host (풀 곳 없음) 로 풀리는 항목: **없다.**
  - schema 에 항목이 없는 api-set 이름: **없다** (102개 전부 찾음).
  - 자기 자신 (`kernel32.dll`) 으로 풀리는 항목: **없다** (위 7개 예외가 막아 준다).
  - 그러므로 kernel32 를 들일 때 새로 올라와야 할 DLL 은 **`KERNELBASE.dll` 하나**다 (ntdll 은 이미 있음).
- 참고 (안 물은 것, delay import — 실제 로드 시점은 요청서 범위 밖): kernel32 delay import 셋을 kernel32 가 가져오는 쪽으로 풀면
  - `ext-ms-win-oobe-query-l1-1-0.dll` → schema 값이 **빈 이름** (빈 host).
  - `ext-ms-win-packagevirtualizationcontext-l1-1-0.dll` → `daxexec.dll`.
  - `RPCRT4.dll` → 진짜 이름 그대로.
  - 이 셋은 일반 import 가 아니라서 위 고리에 안 닿는다.
- 근거: 질문 1 표와 합계. `apisetschema.dll` 의 위 항목들.

### 4. IAT 첫 칸이 0 인 서술자

- 답 `[정적: PE 표]`: **없다.** kernel32 104개 모두 FirstThunk(IAT) 첫 칸이 파일에서 0 이 아니고 (OriginalFirstThunk 첫 칸과 같은 이름 쪽지 값), 함수 수도 모두 1 이상 (가장 작은 게 1).
- 덤으로 `[정적: PE 표 + run22 코드]`:
  - 이름 rva 나 FirstThunk 가 0 이라 고리가 **중간에 끊기는 서술자도 없다.** 끝 표시는 #104 자리 (`0xb5b7c`) 하나뿐.
  - 그래서 세는 고리 `+0x122~+0x14d` 는 `EBX` = **104**, `R15D` = **104** 로 끝난다 (빼는 게 0개). run22 의 `+0x17b` 줄 규칙대로면 자식 칸 배열은 8바이트×104, `ctx+0x68` = 104, `ctx+0x6c` = **105** (`R15D+1`, 핫패치 꺼진 경우). 핫패치가 켜졌으면 106.
  - 번호가 건너뛰는 일도 없어서 "서술자 번호 = 칸 번호" 가 그대로다.
- 근거: `kernel32.dll` 서술자 배열 전부 (`0xb535c`~`0xb5b7c`), 각 FirstThunk 첫 8바이트.

### 5. kernelbase.dll 의 일반 import

- 답 `[정적: PE 표]`: **서술자 4개, 첫 항목은 `ntdll.dll`.** 다른 DLL 로 풀리는 건 없다.

| # | 파일 | 이름 | 함수 수 | 진짜 DLL | 풀린 방법 |
|---|---|---|---|---|---|
| 0 | `0x3a1b1c` | `ntdll.dll` | 743 (서수 import 1개 포함) | `ntdll.dll` | 진짜 이름 |
| 1 | `0x3a1b30` | `api-ms-win-eventing-provider-l1-1-0.dll` | 5 | **`kernelbase.dll` (자기 자신)** | api-set, 값이 하나뿐 (예외 없음) |
| 2 | `0x3a1b44` | `api-ms-win-core-apiquery-l1-1-0.dll` | 1 | `ntdll.dll` | api-set 기본 |
| 3 | `0x3a1b58` | `api-ms-win-core-apiquery-l1-1-2.dll` | 1 | `ntdll.dll` | api-set 기본 |

  - 함수 합 750. IAT 첫 칸이 0 인 것 없음.
  - #1 이 눈여겨볼 점: kernelbase 가 **자기 자신의 api-set 이름**을 import 한다 (schema 값이 `kernelbase.dll`). 그래서 kernelbase 가 끌어오는 새 DLL 은 없다. ntdll 이 "자기 자신으로 풀린 이름" 을 어떻게 다루는지는 이번에 안 읽었다 (flow.md 2.5 의 "자기 자신 간선 제외" 줄과 맞물릴 것 `[추측]`).
  - 그래서 사슬은 이렇게 닫힌다 `[정적: PE 표]`: **kernel32 → { ntdll, kernelbase }**, **kernelbase → { ntdll, (자기 자신) }**. kernelbase 아래에서 더 올라오는 DLL 은 없다.
  - 참고 (delay import, 로드 시점은 범위 밖): kernelbase 는 delay import 가 **56개** (`ext-ms-win-…`/`api-ms-win-…` 54개 + `WINTRUST.dll`, `OLEAUT32.dll`). 일반 import 고리엔 안 나온다.
- 근거: `KernelBase.dll` 파일 `0x3a1b1c` ~ `0x3a1b6c` (서술자 4 + 끝 표시), `apisetschema.dll` 항목 `…eventing-provider-l1-1-0` (`+0x1730`, 값 `+0x7010` 하나 = `kernelbase.dll`).

## flow.md 2.5 kernel32 → kernelbase 줄 고치기 (제안)

- `flow.md` 2269·2295·2304줄 근처 (`[추측]`/`[공백]` 로 남은 곳):
  - "kernelbase 를 **처음** 들이는 import 바퀴가 직접 이름 항목인지 api-set 항목인지 `[공백]`" → **"`[정적: PE 표]` (run37) 직접 이름 항목이다. kernel32 import 서술자 파일 순서 #0·#1 api-set → ntdll, #2 `ntdll.dll`, **#3 `KERNELBASE.dll`**, #4 부터 processthreads api-set (kernel32 예외 host 로 kernelbase). 실행 확인은 Q20."**
  - "102개 api-set 바퀴 중에도 KERNELBASE 로 풀리는 게 있을 수 있다 `[추측]`" → **"api-set 102개 중 99개가 kernelbase.dll, 3개가 ntdll.dll 로 풀린다 `[정적: PE 표]` (kernel32 용 예외 host 7곳 포함). 그 99개 바퀴는 #3 뒤라서 이미 있는 kernelbase 를 만난다."**
- 4장 3486행 (2.5 "처음 들이는 import 바퀴") 은 닫을 수 있다. Q51 완료.
- run25 의 "kernel32 를 들이면 kernelbase 가 따라 들어온다" 에 한 줄: 따라 올라오는 건 kernelbase 하나뿐이고 (질문 3), kernelbase 는 ntdll 만 더 기댄다 (질문 5).

## 동적분석으로 확인해야 할 것

- **새 dast 항목은 필요 없다.** Q20 (+ Q23·Q47 과 같은 입구)와 **같은 실행에서** 볼 수 있다. dast 요청 필요: Q20 에 덧붙일 것 —
  - 입구 `ntdll+0x21ab0` (`LdrpLoadDependentModuleInternal`, run23) 을 부모가 `KERNEL32` 일 때만: 이름 `dS @rcx` 를 순서대로. 기대 `[추측: 이 표대로면]`: 1번째 `api-ms-win-core-rtlsupport-l1-1-0.dll`, 2번째 `…l1-2-2.dll`, 3번째 `ntdll.dll`, **4번째 `KERNELBASE.dll`**, 그 뒤 `api-ms-win-core-processthreads-l1-1-3.dll` …
  - 바퀴 번호가 필요하면 5번째 인자 `[RSP+0x28]` 에서 `poi(@rdx+0x58)` 을 빼고 8로 나눈 값 `[추측: 입구 인자 위치 계산]` (run22: 칸 배열이 문맥 `+0x58`).
  - `ApiSetpResolveHost` ret `ntdll+0x21daf` (Q20 에 이미 있음): 기대 `[추측]`: **4번째(`KERNELBASE.dll`) 바퀴에선 안 걸림** (이름이 `api-`/`ext-` 로 시작하지 않으므로, run23 조건), 1·2번째(rtlsupport)와 5번째~(processthreads)에선 걸림. 5번째 바퀴에서 `dS @rsp+0x108` 가 `…\kernelbase.dll` 이면 kernel32 용 예외가 실행 때도 맞는 것.
- 정적으로 못 닫은 것:
  - 실행 때 쓰는 건 PEB 의 `ApiSetMap` 인데 이 파일은 "같은 OS 의 표" 다 (같은 파일을 부팅 때 맵한 것일 것 `[추측]`).
  - 부모 이름 비교가 대소문자를 무시하는지 (`kernel32.dll` 소문자 vs 로더 명부의 `KERNEL32.DLL` 일 수 있음) — `ApiSetpResolveHost` 본문은 이번 범위 밖. 위 5번째 바퀴 값으로 닫힌다.
- 추천 멈춤점: `ntdll+0x21ab0` (조건: 부모 = KERNEL32, 이름·번호 찍고 계속), `ntdll+0x21daf` (ApiSetpResolveHost ret, 이미 Q20 에 있음).

## 판정

**분석성공.** 다섯 질문 모두 PE 표 값(파일 오프셋·rva)으로 답했다. 숫자는 run25·run34 와 맞고, ApiSet 찾기는 해시와 이름 비교 두 방법이 105/105 같았다. kernelbase 를 처음 들이는 건 **#3 `KERNELBASE.dll` (이름 그대로)**.

# run101 k32-leftovers — 해설

> 이 글은 [run101 문서](run101-k32-leftovers.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q59 + Q60** — 둘 다 kernel32 에 남은 작은 정적 항목 (`_workspace/plan.md`, 이번에 **둘 다 끝**)
  - Q59 — `BaseThreadInitThunk` 첫 인자 ≠ 0 갈래 안의 `BasepInitializeTermsrvFpns` 가 무엇을 하나.
  - Q60 — test.exe 가 부르는 `KERNEL32!LoadLibraryExW` 가 어떻게 `KERNELBASE!LoadLibraryExW` 로 가나.
  - 둘 다 kernel32 이고 작고 대기열에서 연달아 있어서 **한 요청 · 한 바퀴** 로 묶었다 (오케스트레이터 기본값).
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 **첫 묶음** 이다.
  - 이번에 남은 것이 간 곳 (4절):
    - 동적 → **Q48** (run101 몫), **Q79** (사진 덤프 오프라인 읽기), **Q31** (덤)
    - 정적 → 새 **Q187 ① ~ ③**
- 근거:
  - run 문서 [`run101-k32-leftovers.md`](run101-k32-leftovers.md) (가장 중요)
  - 요청서 [`_workspace/sast/97_k32-leftovers.md`](../../_workspace/sast/97_k32-leftovers.md) · 결과서 [`97_k32-leftovers_result.md`](../../_workspace/sast/97_k32-leftovers_result.md) — 판정 **분석성공**
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 근거 로그 · **그림 2** (+ 그림 메모) · 2.4 · 2.5 · 2.10 · 4장 · 4.1 · 5장
  - 짐 지도 [`baggage.md`](baggage.md): 7.6.8 표에 **새 짐 한 줄** (+ 0 그대로 한 줄, 고친 줄 하나), 새 7.33
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없다.
  - 값 대조는 run44 때 찍어 둔 사진 로그를 **다시 읽어서** 했다 (2.4).

이 글은 두 편의 **뒷이야기** 다.

- [run25 해설 2.6](../02_sprint/run25-dll-entry-해설.md#26-핵심--basethreadinitthunk-는-kernel32-안-진짜-코드다) — `BaseThreadInitThunk` (**출근 인사 담당**) 를 처음 소개했다. 첫 인자 ≠ 0 이면 "본업 없이 터미널 서비스 쪽 준비만 한다" 까지, 이름만 봤다.
- [run30 해설 2.7 "작은 주의 — 이름표가 둘이다"](../02_sprint/run30-exe-crt-start-해설.md#작은-주의--이름표가-둘이다) — test.exe 는 `KERNEL32!LoadLibraryExW` 를 부르는데, run7 이 멈춘 곳은 `KERNELBASE!LoadLibraryExW` 였다. "그 사이는 근거에 없다" 고 남겼다. **이번에 풀렸다.**

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **스텁** (다른 곳으로 바로 넘기기만 하는 짧은 조각) — 2.9
- **TS 인식 표시** (exe 머리의 `DllCharacteristics` 비트 15) — 2.5. run68 해설에서 이미 나온 말이라 짧게만.
- **ApiSet** 은 이미 푼 말이다 → [run23 해설 2.5](../02_sprint/run23-load-dependent-해설.md#25-핵심--apiset--가상-이름을-진짜-이름으로-바꾸는-자리) · [run37 해설](../02_sprint/run37-k32-import-order-해설.md) (**별명**, `apisetschema.dll` = **별명 사전**). 2.9 에서 링크만 건다.

### 이번 바퀴에서 조심할 곳 넷

1. **두 질문은 서로 다른 시간에 있다.**
   - Q59 는 **프로세스 초기화 중** (`LdrpInitializeProcess` 뒤쪽, 사진 P1a ~ P1b 사이).
   - Q60 은 **진입점 뒤** (CRT 가 `LoadLibraryExW` 를 부를 때). 지도 경계 바깥에서 ntdll 로 다시 들어오는 화살표 자리다.
   - 같은 kernel32 라서 묶었을 뿐, 이야기는 둘이다. 2절도 Q59 (2.3 ~ 2.8) 와 Q60 (2.9 ~ 2.12) 으로 나눈다.
2. **"갈래로 들어간다" 와 "실제로 들어갔다" 는 다르다.**
   - 갈래를 가르는 값 (`SuiteMask 0x110` 등) 은 run44 사진의 `[관찰]` 이다.
   - 그 값으로 길을 따라간 건 `[정적]` 이다.
   - 호출이 정말 일어난 순간은 **아직 아무도 못 봤다** → Q48.
3. **새 짐 칸의 값은 사진에 없다.**
   - run44 는 ntdll `.data` 만 읽었다. kernel32 `.data` 의 새 칸은 안 읽었다 → `[확인 필요]`, Q79.
4. **오프셋은 call 자리다.** ret 자리는 run 문서 "정적 — 호출 지점" 표에 있다.

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- rva, PEB (**안내판**) → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 2절
- 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
- DLL = **건물**. kernel32 = **기본 설비 건물**, KERNELBASE = 그 옆 **본 설비 건물** → [run25 해설](../02_sprint/run25-dll-entry-해설.md) 첫머리
- `BaseThreadInitThunk` = kernel32 의 **출근 인사 담당**, 칸 `Kernel32ThreadInitThunkFunction` = 관리실 벽 주소록의 **"출근 인사 담당" 칸** → [run20 해설 2.5](../02_sprint/run20-kernel32-load-해설.md) · [run25 해설 2.6](../02_sprint/run25-dll-entry-해설.md#26-핵심--basethreadinitthunk-는-kernel32-안-진짜-코드다)
- export 표 = 건물 입구 **호실 이름표**, forwarder = **"옆 건물 몇 호로 가세요" 안내문** → [run16 해설 2.9](../02_sprint/run16-thunk-context-해설.md#29-커널은-ntdll-주소를-어떻게-아나--부팅-때-이름으로-찾아-적어-둔다) · [run20 해설](../02_sprint/run20-kernel32-load-해설.md) 2.5
- IAT = 벽 **콘센트** (건물마다 가진 빈 주소록) → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.7 · [run30 해설](../02_sprint/run30-exe-crt-start-해설.md)
- api-set = **별명**, ApiSet 맵 = **별명 사전** → [run37 해설](../02_sprint/run37-k32-import-order-해설.md)
- 꼬리 점프 (콜스택에 안 쌓이는 넘어가기) → [run17 해설 2.4](../02_sprint/run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리)
- CFG 간접 호출 = **교환원** → [run28 해설 2.9](../02_sprint/run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)
- KUSD (`KUSER_SHARED_DATA`) = 건물 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 짐 = **개업 준비물**, 짐 지도 = 준비물 **점검표**, 상태 사진 = 준비 도중 찍은 **사진** (P1a · P1b · P2 …) → [run43 해설](run43-baggage-checklist-해설.md) · [run44 해설](run44-baggage-snap-해설.md)
- DLL 전역 = 건물 **수첩** → [run68 해설](run68-dll-init-rest-해설.md) 2.4
- 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 | 표시 |
|---|---|---|
| 초기화 때 `(1,0,0)` 으로 부르는 칸의 함수는? | **`KERNEL32!BaseThreadInitThunk`** | `[정적]` (이번에 `[추측]` 에서 올라감) |
| test.exe 가 그 갈래 안 `BasepInitializeTermsrvFpns` 로 들어가나? | **들어간다.** `SuiteMask 0x110` 이라 비트 `0x10` 이 켜짐 | 값 `[관찰]` run44 + 길 `[정적]` |
| 들어가서 무엇을 하나? | 맨 앞에서 **무조건** 수첩 한 칸 (`pfnTermsrvSyncUserIniFile`) 을 채우고, exe 가 TS 인식이라 **곧 0 을 돌려주고 나감** | `[정적]` |
| 짐 지도에 새 칸이 생기나? | **1칸.** kernel32 `.data` rva `0xc1d08` | `[정적]` |
| `KERNEL32!LoadLibraryExW` 는 무엇인가? | **7바이트 `JMP` 스텁.** forwarder 아님. kernel32 IAT `0x8b3e8` → ApiSet → `KERNELBASE!LoadLibraryExW` | `[정적]` |
| 왜 콜스택에 kernel32 가 없었나? | `CALL` 이 아니라 `JMP` 라서 돌아올 주소가 안 쌓임. run7 `[관찰]` 과 맞음 | `[정적]` + `[관찰]` |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

숫자는 앞 run 들 그대로다. 이번 자리는 **둘** 이다 (← 표시).

```
cmd.exe 의 부탁 → 커널 (프로세스 · 스레드 만들기)
 → ntdll LdrInitializeThunk → LdrpInitialize … → LdrpInitializeProcess
     ├─ ●P1a  +0x199d  LdrpInitializeKernel32Functions   (kernel32 준비, 칸을 채움)
     ├─ +0x1b59  칸 Kernel32ThreadInitThunkFunction(1,0,0)        ← Q59 (이번)
     │            = KERNEL32!BaseThreadInitThunk → BasepInitializeTermsrvFpns
     ├─ +0x1b72  LdrpInitializePerUserWindowsDirectory
     ├─ ●P1b  +0x1bb6  LdrpInitializeGraphRecurse        (dll 시작 함수들)
     └─ …
 → NtContinue → RtlUserThreadStart → BaseThreadInitThunk(0, 진입점, PEB)
 → test.exe 진입점 mainCRTStartup  ─ 여기부터 CRT (지도 밖, 점선 상자)
     └─ try_get_function_slow +0x6d  CALL [test IAT 0x6e250]     ← Q60 (이번)
          = KERNEL32!LoadLibraryExW → … → KERNELBASE!LoadLibraryExW ×3
 → main
```

- Q59 는 사진 **P1a 와 P1b 사이** 다 (run 문서 "사진 로그 대조").
- Q60 은 진입점 **뒤** 다. 지도 경계 밖이지만, ntdll 로더로 **다시 들어오는** 화살표라서 그림에 남겨 둔 자리다 ([run30 해설 2.1](../02_sprint/run30-exe-crt-start-해설.md#21-먼저--지도의-경계가-정해졌다)).

### 무엇이 궁금했나

- **Q59**: run25 에서 이름만 본 `BasepInitializeTermsrvFpns` 가 무엇을 하고 어느 전역을 쓰나. test.exe 가 이 갈래를 타나. 짐 지도에 넣을 칸이 생기나.
- **Q60**: test.exe 가 import 하는 `KERNEL32!LoadLibraryExW` 가 run7 콜스택의 `KERNELBASE!LoadLibraryExW` 로 **어떻게** 가나. forwarder 인가, 스텁인가.

### 바퀴 전에 알던 것

- `[정적]` (run20) 칸 `Kernel32ThreadInitThunkFunction` 은 `LdrpInitializeKernel32Functions` 가 kernel32 에서 **이름 `BaseThreadInitThunk`** 로 찾은 주소로 채운다.
- `[정적]` (run25) export `BaseThreadInitThunk` 는 forwarder 가 아니고 rva `0x2cd70` 의 진짜 코드다.
  - 첫 인자 ≠ 0 이면 `+0x26 RtlGetSuiteMask` → (`AL & 0x10`) `+0x36 BasepInitializeTermsrvFpns`. **안쪽은 이름만.**
- `[관찰]` (run44) 사진에서 그 칸 값은 `KERNEL32+0x2cd70`.
- `[추측]` (flow.md 2.4 · 4장) 그러니 `LdrpInitializeProcess +0x1b59` 의 `(1,0,0)` 호출은 `BaseThreadInitThunk` 일 것 (동적 Q48).
- `[관찰]` (run7) CRT 쪽 콜스택에 찍힌 이름은 `KERNELBASE!LoadLibraryExW`, 돌아갈 곳은 test.exe 안 `…67723`.
- `[정적]` (run30) test.exe 는 `KERNEL32.dll` 의 `LoadLibraryExW` 를 import 한다 (IAT `0x6e250`). 둘 사이의 고리는 근거에 없었다.
- (단서, run36 `[정적+샘플]`) test.exe 의 KERNEL32 import 중 forwarder 는 `NTDLL.Rtl…` 10개뿐이고 `LoadLibraryExW` 는 없다 → forwarder 는 아닐 것.

---

## 2. 찾은 것

### 2.1 한눈에 — 두 그림

run 문서 "호출 관계" 를 줄였다. 전부는 run 문서에.

**Q59** (프로세스 초기화 때 한 번):

```
ntdll!LdrpInitializeProcess
 └─ +0x1b59 (간접, CFG)  칸(1, 0, 0) = KERNEL32!BaseThreadInitThunk          [정적] run101
     ├─ +0x26  ntdll!RtlGetSuiteMask   → KUSD SuiteMask = 0x110                [정적] + 값 [관찰] run44
     └─ +0x36  KERNEL32!BasepInitializeTermsrvFpns   (AL & 0x10 켜짐)
             │  +0x1b  pfnTermsrvSyncUserIniFile ← kernel32+0x72b60       ← 새 짐 (무조건)
             ├─ +0x22  IsTerminalServerCompatible → 1  (exe 가 TS 인식)
             │         +0x2b  0 을 돌려주고 나감                           ← test.exe 는 여기까지
             └─ (안 탐) IsSystemLUID · IsTSAppCompatEnabled · tsappcmp.dll 올리기 · GetProcAddress ×12
     돌아옴: EAX = 0 → +0x1b72 LdrpInitializePerUserWindowsDirectory
```

**Q60** (진입점 뒤, CRT 가 부름):

```
test!try_get_function_slow
 └─ +0x6d  CALL [test IAT 0x6e250]  = KERNEL32!LoadLibraryExW  (7B 스텁)     [정적] run30 · run101
             ══▶ +0x0 JMP [kernel32 IAT 0x8b3e8]                            [정적] run101
                  = api-ms-win-core-libraryloader-l1-2-0!LoadLibraryExW → (ApiSet) kernelbase.dll
                  KERNELBASE!LoadLibraryExW (rva 0x21150) → ntdll 로더         [관찰] run7
                  RET → 곧장 test!try_get_function_slow +0x73 (kernel32 프레임 없음)
```

- `──` 는 `CALL` (돌아올 주소가 쌓임), `══▶` 는 `JMP` (안 쌓임) 다. 이 차이가 Q60 의 답 전부다 (2.10).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — `BasepInitializeTermsrvFpns +0x1b` (새 짐을 놓는 줄)

run 문서 표 "`BasepInitializeTermsrvFpns` 가 쓰는 칸" 의 첫 줄이다.

```
+0x14  LEA RAX,[0x180072b60]
+0x1b  MOV [0x1800c1d08],RAX
```

- **누가**: kernel32 의 `BasepInitializeTermsrvFpns`
- **어디서**: 함수 안 `+0x14` 에서 주소를 손에 들고, `+0x1b` 에서 적는다.
- **누구를**: 부르는 게 아니라 **쓴다.** kernel32 `.data` 의 칸 `pfnTermsrvSyncUserIniFile` (rva `0xc1d08`) 에.
- **무엇을**: kernel32 + `0x72b60` = 같은 건물 안 함수 `TermsrvSyncUserIniFile` 의 주소.
- **무엇을 위해**: 나중에 ini 파일 API 가 이 칸을 보고 그 함수로 간다 (2.8).
- 이 두 줄은 갈림 (`+0x29`) **앞** 이다. 그래서 이 함수에 들어오면 **무조건** 실행된다 `[정적]`.
- 비유: kernel32 건물 수첩에 "ini 파일 맞추기 담당 = 우리 건물 `0x72b60` 호" 를 **들어오자마자** 적어 둔다. 손님이 누구든 상관없이.

#### 한 줄 읽기 ② — `KERNEL32!LoadLibraryExW +0x0` (스텁 한 줄)

run 문서 "Q60 스텁" 표 첫 줄이다.

```
+0x0  JMP qword ptr [0x18008b3e8]      ; 바이트 48 FF 25 91 F6 04 00
```

- **누가**: kernel32 의 `LoadLibraryExW` (rva `0x3bd50`, 7바이트, 이게 함수 전부)
- **어디서**: `+0x0` — 첫 줄이자 마지막 줄.
- **누구를**: kernel32 자기 IAT 칸 `0x8b3e8` 에 적힌 주소 = `KERNELBASE!LoadLibraryExW`.
- **무엇을 위해**: 아무 일도 안 하고 **그대로 넘긴다.** `CALL` 이 아니라 `JMP` 라서 돌아올 주소를 안 남긴다.
- 비유: kernel32 건물의 "LoadLibraryExW 호" 문을 열면 방이 없고, **옆 건물로 바로 통하는 복도** 만 있다.

#### 한 줄 읽기 ③ — `LdrpInitializeProcess +0x1b59` (부르는 쪽 줄)

- **누가**: ntdll 관리실의 `LdrpInitializeProcess`
- **어디서**: `+0x1b59` (ret `+0x1b5e`)
- **누구를**: 벽 주소록 "출근 인사 담당" 칸 (`Kernel32ThreadInitThunkFunction`) 에 적힌 함수. 교환원 (CFG) 을 거치는 간접 호출.
  - `+0x1b45` 에서 칸을 읽고, 칸이 비었으면 (`NULL`) `+0x1b4f JZ` 로 건너뛴다.
- **무엇을**: 인자 `ECX = 1` (`+0x1b56 LEA ECX,[RDX+1]`), `EDX = 0` (`+0x1b54`), `R8 = 0` (`+0x1b51`).
- **무엇을 위해**: 직원 출근 때 부르는 그 담당을, 가게 세팅 때 **첫 인자 1** 로 한 번 불러 "본업 말고 준비만" 시킨다.
- 돌아오면 `+0x1b64 JS` (음수면 실패 꼬리) 를 보고, 아니면 `+0x1b72 LdrpInitializePerUserWindowsDirectory` 로 간다.

---

## Q59 — 출근 인사 담당의 "준비만" 갈래

### 2.3 칸의 함수 = `BaseThreadInitThunk` — `[추측]` 에서 `[정적]` 으로

#### 단계별로 — 근거 → 그래서

1. `[정적]` (run20, 이번에 다시 확인) `LdrpInitializeKernel32Functions +0xfa` (ret `+0xff`) 가 `LdrGetProcedureAddressForCaller(kernel32, "BaseThreadInitThunk", 0, &칸, …)` 로 칸 `0x1d2760` 을 채운다.
   - 이름 글자는 ntdll rva `0x173ba8` 의 ANSI_STRING (길이 19) 이다 — 결과서가 PE 에서 직접 읽었다.
2. `[정적]` (run25) 그 이름의 export 는 forwarder 가 아닌 kernel32 안 코드 (rva `0x2cd70`).
3. `[관찰]` (run44) 사진 속 칸 값 = `KERNEL32+0x2cd70`.
4. 그래서 → **`+0x1b59` 가 부르는 건 `KERNEL32!BaseThreadInitThunk`** `[정적]`.
5. 아직 확인 아님 → "그 호출이 **실제로 일어났나**" 의 직접 관찰 (동적 Q48).

- 비유: 벽 주소록 "출근 인사 담당" 칸에 적힌 번호를 **누가 어떤 이름으로 적었는지** 노선도로 확인했고, 사진 속 번호도 같았다. 남은 건 "그날 정말 전화를 걸었나" 를 버스 타서 보는 것뿐이다.

#### 이 갈래는 프로세스 초기화 때 **한 번뿐** `[정적]`

- 칸을 읽는 ntdll 코드는 **둘** 이다 (`_refs`).
  - `LdrpInitializeProcess +0x1b45` — 첫 인자 **1**
  - `RtlUserThreadStart +0x7` — `+0x25 XOR ECX,ECX` 로 첫 인자 **0** (새 스레드 출근)
- 그러니 "첫 인자 ≠ 0" 갈래는 가게 세팅 때 한 번만 탄다. 둘째 스레드가 출근할 땐 안 탄다.

#### 이 갈래에서 부르는 건 둘뿐 `[정적]`

- `+0x26` `RtlGetSuiteMask`, `+0x36` `BasepInitializeTermsrvFpns`. 그 밖엔 없다.
- 본업 (시작 함수) 은 안 부른다. `RDX`·`R8` 은 이 갈래에서 안 쓰인다 (결과서 질문 3).
- 돌려주는 값은 **0 또는 `BasepInitializeTermsrvFpns` 가 준 음수 NTSTATUS** 뿐이다 (`+0x3d CMOVS`).
  - 비트가 꺼져 있으면 `+0x34 JZ +0x49` → `XOR EAX,EAX` → `RET 0`.

### 2.4 갈래 고르기 — 공용 게시판의 `SuiteMask`

#### `RtlGetSuiteMask` 는 어디를 읽나 `[정적]`

- 두 곳 중 하나다 (`RtlGetSuiteMask.asm`).
  1. `+0x9` 에서 `PEB.SharedData` (`+0x90`) 를 본다.
  2. 그게 NULL (`+0x13 JZ`) 이거나 가리키는 첫 4바이트가 0 (`+0x18 JZ`) 이면 → `+0x2f` 에서 **KUSD `SuiteMask` (`0x7ffe02d0`)** 를 읽어 돌려준다.
  3. 아니면 `+0x2a` 에서 `[SharedData + 0x14]` 를 돌려준다.
- 즉 **공용 게시판에서 읽는 게 기본** 이고, 안내판 (PEB) 에 다른 게시판이 붙어 있으면 그쪽을 본다.

#### test.exe 를 돌린 VM 값 `[관찰]` (run44 사진 로그를 다시 읽음)

| 무엇 | 값 | 로그 |
|---|---|---|
| KUSD `SuiteMask` | `0x110` | `ms/logs/04_baggage-read_P1a.txt:2922` · `_P1b.txt:4040` · `_P2.txt:4052` |
| `vertarget` | `suite: TerminalServer SingleUserTS` | `_P1a.txt:6` |
| `PEB.SharedData` | `(null)` | `_P1a.txt:1764` · `_P1b.txt:2462` · `_P2.txt:2474` |

#### 그래서 갈래를 탄다 — 단계별로

1. 근거 `[관찰]`: P1a (`+0x199d`) 와 P1b (`+0x1bb6`) 두 사진 모두 `SharedData` NULL, `SuiteMask 0x110`.
2. 근거 `[정적]`: 호출 자리 `+0x1b59` 는 그 두 사진 **사이** 다.
3. 그래서 → 호출 때도 같은 값이었다고 본다. `RtlGetSuiteMask` 는 KUSD 갈래로 `0x110` 을 돌려주고, `AL = 0x10` → `+0x32 TEST AL,0x10` 켜짐 → `+0x34 JZ` 안 탐 → `+0x36 BasepInitializeTermsrvFpns` 로 간다.
4. 아직 확인 아님 → 실제로 그 순간 `AL` 을 본 사람은 없다 (Q48).

- 비트 이름 `0x10` = TERMINAL, `0x100` = SINGLEUSERTS 는 `[추측]` 이다.
  - 왜 그렇게 짐작하나: `IsTSAppCompatEnabled` 가 두 비트를 따로 보고, `vertarget` 글 `TerminalServer SingleUserTS` 와 숫자가 맞기 때문이다.
- 비유: 로비 공용 게시판에 "이 건물은 **터미널 서버** 형 (단, 혼자 쓰는 판)" 이라고 붙어 있다. 출근 인사 담당은 이걸 보고 "그럼 터미널 서비스 준비를 해야지" 하며 준비 담당 (`BasepInitializeTermsrvFpns`) 을 부른다.

### 2.5 들어가자마자 한 칸 쓰고, TS 인식 확인 뒤 바로 나간다

#### 새 말 — TS 인식 표시

- exe 머리 `DllCharacteristics` 의 비트 15 (`0x8000`) 다. "나는 터미널 서버 환경을 알고 만들어졌다" 는 표시다. [run68 해설 2.11 (다)](run68-dll-init-rest-해설.md#다-getregistryextensionflags--exe-가-터미널-서버-인식-표시를-달고-있어서) 에서 이미 한 번 나왔다 (`TERMINAL_SERVER_AWARE`).
- 비유: 가게 간판 옆에 붙은 "**여러 손님 겸용 매장 인증**" 스티커. 이 스티커가 있으면 옛날 가게용 보정 작업을 안 해 준다.

#### 함수 안 차례 (test.exe 길) `[정적]`

| 자리 | 하는 일 | test.exe |
|---|---|---|
| `+0xf` | 지역 변수 하나를 0 으로 | 탐 |
| `+0x14` · `+0x1b` | **`pfnTermsrvSyncUserIniFile` ← kernel32 + `0x72b60`** | **탐 (무조건)** |
| `+0x22` (ret `+0x27`) | `IsTerminalServerCompatible` — exe 의 TS 인식 비트를 봄 | 1 을 돌려줌 |
| `+0x29` | `JZ` — 0 일 때만 아래로 감 | 안 탐 |
| `+0x2b` | `XOR EAX,EAX` → 정리 → `RET` (0 반환) | **여기서 나감** |

#### `IsTerminalServerCompatible` 은 어떻게 보나 `[정적]`

1. `+0x4` · `+0xd` — 안내판 (PEB) 의 `ImageBaseAddress` (exe 가 놓인 자리) 를 꺼낸다.
2. `+0x11` (ret `+0x18`) — ntdll `RtlImageNtHeader` 로 exe 머리를 찾는다. 메모리만 읽는다 (syscall 없음).
3. `+0x27 CMP word [NT헤더+0x5e],0` · `+0x2b SETL` — `DllCharacteristics` 를 부호 있는 16비트로 보고 "0 보다 작나" = **비트 15 가 켜졌나**.
4. test.exe `DllCharacteristics = 0x8160` → 비트 15 켜짐 → **1**.
   - `0x8160` 은 `sample/bin/test.exe` 머리에서 읽은 값 `[정적+샘플]` 이고, 실행 때도 `_SECTION_IMAGE_INFORMATION.DllCharacteristics = 0x8160` 이었다 `[관찰]` (run44, `_P1a.txt:1634` · `_P1b.txt:2332`).

#### 그래서 test.exe 에선

- syscall **0개**.
- `tsappcmp.dll` 을 **안 올린다.**
- 새로 바뀐 전역은 **딱 하나** — `+0x1b` 의 `pfnTermsrvSyncUserIniFile` `[정적]`.

- 비유: 준비 담당은 문을 열자마자 수첩에 한 줄 적는다. 그다음 간판 옆 스티커를 확인하고 "아, 인증 매장이네. 보정 작업은 필요 없겠다" 하고 돌아간다.

### 2.6 test.exe 가 안 타는 나머지 — 이름만

test.exe 는 `+0x2b` 에서 나가서 아래는 전부 안 탄다 `[정적]`. "만약 탔다면" 의 길이다.

1. `+0x3e` `IsSystemLUID` — 토큰을 열어 SYSTEM 계정인지 본다 (`NtOpenProcessToken` · `NtQueryInformationToken` · `NtClose`). 바로 아래 (depth 1) 함수 중 syscall 을 부르는 건 이것뿐이다 (결과서). SYSTEM 이면 나감.
2. `+0x4c` `IsTSAppCompatEnabled` — `GetVersionExW` 로 판 종류를 보고 "TS 비트 켜짐 · SINGLEUSERTS 꺼짐 · **서버**" 일 때만 지역 변수에 1.
3. `+0x6b` — 그 지역 변수가 1 이면 `LoadLibraryExW(L"tsappcmp.dll", NULL, 0x800)`.
4. `+0x90 ~ +0xc5` — 12바퀴 돌며 `GetProcAddress` 로 `tsappcmp.dll` 함수 12개를 찾아 kernel32 수첩 **12칸** 에 적는다 (`+0xbb`).
   - 무엇을 어느 칸에 적는지는 kernel32 `.rdata` rva `0x89310` 의 표 (16바이트 × 12 = `{이름, 칸 주소}`) 에 있다. 12칸 목록은 [run 문서](run101-k32-leftovers.md) "`BasepInitializeTermsrvFpns` 가 쓰는 칸" 표.

#### 하나 더 — 스티커가 없었어도 안 올렸을 것 `[정적]`

- 이 VM 은 SINGLEUSERTS 가 켜진 판 (서버 아님) 이다.
- 그래서 TS 인식 비트가 꺼진 exe 였어도 `IsTSAppCompatEnabled` 가 지역 변수에 0 을 줘서 (`+0x55 ~ +0x5a`) `tsappcmp.dll` 은 안 올렸을 것이다.
- 즉 test.exe 길은 **두 겹으로** `tsappcmp.dll` 과 멀다.

#### 헷갈리지 말 것 — run68 의 9칸과는 다른 칸이다 `[정적]`

- run68 에서 본 `InitializeRegTermsrvFpns` 도 비슷한 이름의 칸 9개를 채운다 (`0xc1cc0 ~ 0xc1d00`, `_BaseDllInitialize +0x364`).
- 이번 12칸 + `0xc1d08` 은 **그 9칸과 겹치지 않는 다른 칸** 이다. `0xc1cb8` 은 이번 함수 몫 (표 4번).
- test.exe 에선 9칸 · 12칸 **모두 0 그대로** 다.
- 비유: kernel32 수첩에 "터미널 서비스 관련" 페이지가 두 장 있다. 한 장은 건물 전원 켤 때 (run68), 한 장은 출근 인사 담당이 (이번). test.exe 에선 둘 다 빈 페이지로 남고, 이번 장의 **맨 윗줄 하나만** 채워진다.

### 2.7 덤 — run25 의 `[추측]` 하나가 닫혔다

- run25 에서 ntdll 이 kernel32 에서 이름으로 찾는 함수 셋 중 하나가 `TermsrvGetWindowsDirectoryW` 였다. 이 함수는 수첩 칸 하나를 보고, 비어 있으면 그냥 돌아온다. run25 는 "그 칸을 채우는 쪽은 터미널 서비스 쪽일 것" 이라고 `[추측]` 했다.
- 이번에 그 칸 = `pfnTermsrvGetWindowsDirectoryW` (rva `0xc1d10`) 다. 쓰는 줄은 2.6 의 12바퀴 루프 **하나뿐** 이다 (`_refs`: 읽기 둘 + 표 칸 하나) `[정적]`.
- 그래서 → test.exe 에선 **0 그대로** `[정적]`. run25 의 짐작이 맞았다.
- 이 함수가 `+0x1b72 LdrpInitializePerUserWindowsDirectory` 로 넘어가 무엇을 돌려주는지는 이번에 안 봤다 (run 문서 "결과서와 다르게 적은 곳 · 보탠 것").

### 2.8 새 짐 한 칸 — 짐 지도에 적는 다섯

| 적는 것 | 값 |
|---|---|
| 칸 | kernel32 `.data` **`pfnTermsrvSyncUserIniFile`** (rva `0xc1d08`, 8바이트) |
| 공간 | **유저** (에뮬레이터 가상 주소 공간 안, kernel32 데이터) |
| 단계 | **②** — P1a ~ P1b 사이 (뒤7 `+0x1b59` 호출 안) |
| 놓는 법 | **직접** (`BasepInitializeTermsrvFpns +0x1b` 의 `MOV`), 쓴 주인은 다른 DLL (KERNEL32) |
| 경로 | `LdrpInitializeProcess +0x1b59 → BaseThreadInitThunk (호출 +0x36) → BasepInitializeTermsrvFpns +0x1b` |

- 값은 **주소** 다: kernel32 가 놓인 자리 + `0x72b60` (`TermsrvSyncUserIniFile`, 1386바이트).
  - 그래서 에뮬레이터에선 "kernel32 를 붙인 자리 + `0x72b60`" 이 들어가야 진짜와 같다.
  - 실제 값은 아직 아무도 안 봤다 → `[확인 필요]` (Q79).
- 이 칸을 **읽는 곳** 은 하나다 `[정적]`:
  - `TermsrvSyncUserIniFileExt +0x0` (17바이트) — 칸을 읽어 비어 있지 않으면 교환원을 거쳐 그 함수로 간다 (`+0xa`), 비었으면 `+0x10 RET`.
  - 그걸 부르는 곳은 `BaseDllReadWriteIniFileOnDisk +0x166` 하나 (+ export 표 칸) — ini 파일 API 다. **초기화가 끝난 뒤** (은퇴 뒤) 의 길이다.
- 비유: 개업 준비물 점검표에 한 줄이 늘었다. "kernel32 수첩 맨 윗줄에 담당 호실 번호 하나." 준비 때 무조건 적히지만, 실제로 그 번호를 쓰는 건 개업한 뒤 손님이 ini 파일을 만질 때다.

#### 짐 지도의 큰 숫자는 그대로다

- 이 칸은 다른 DLL 의 `.data` 라 **체크리스트 밖** 이다. baggage.md 2장 숫자는 안 바뀌었다 (run 문서 "flow.md · 짐 지도에 반영한 것").

---

## Q60 — `KERNEL32!LoadLibraryExW` 는 복도다

### 2.9 forwarder 가 아니라 스텁이다

#### 새 말 — 스텁

- **스텁** = 자기는 아무 일도 안 하고 **다른 곳으로 바로 넘기기만 하는 짧은 조각**.
- 비유: 문을 열면 방이 아니라 **옆 건물로 이어진 복도** 만 있는 호실.
- forwarder (**"옆 건물 몇 호로 가세요" 안내문**) 와 무엇이 다르나:
  - forwarder 는 **이름표 자체** 가 "다른 건물" 을 적어 둔다. 관리실이 짐을 들일 때 그 안내문을 읽고 콘센트에 **처음부터** 옆 건물 번호를 꽂는다 `[추측: 일반적으로 알려진 설명]`.
  - 스텁은 이름표가 **이 건물 안 진짜 호실** 을 가리킨다. 그 호실에 들어가 보면 복도가 있을 뿐이다.

#### forwarder 가 아닌 근거 `[정적: PE 표]`

1. kernel32 export 디렉터리는 rva `0xa65f0 ~ 0xb535c` 다.
2. `LoadLibraryExW` (서수 1020) 의 주소 칸 값은 `0x3bd50` — **디렉터리 범위 밖** 이다.
3. 범위 밖이면 안내문 글자가 아니라 **코드 주소** 다 (run25 해설 2.6 과 같은 판단법). pefile 도 `forwarder = None`.
4. 그래서 → forwarder 아님.

#### 그 코드 주소엔 무엇이 있나 `[정적]`

- 7바이트 `48 FF 25 91 F6 04 00` = `JMP qword ptr [rip+0x4f691]` 한 줄.
- 계산하면 가리키는 칸은 kernel32 **IAT `0x8b3e8`** 이다 (결과서: `0x3bd50 + 7 + 0x4f691 = 0x8b3e8`).

#### 그 콘센트는 어디에 꽂혀 있나

- 칸 `0x8b3e8` 의 import 이름은 `KERNELBASE.dll` 이 아니라 **별명** `api-ms-win-core-libraryloader-l1-2-0.dll!LoadLibraryExW` 다 `[정적: PE 표]`.
  - 별명 (api-set) 과 별명 사전 (ApiSet 맵) 은 [run23 해설 2.5](../02_sprint/run23-load-dependent-해설.md#25-핵심--apiset--가상-이름을-진짜-이름으로-바꾸는-자리) · [run37 해설](../02_sprint/run37-k32-import-order-해설.md) 에서 풀었다.
- 같은 OS 의 별명 사전 파일 (`apisetschema.dll` `.apiset` v6 표, 984 항목) 에서 찾으면:
  - 항목 `api-ms-win-core-libraryloader-l1-2-3` → 이름 없는 기본값 하나 **`kernelbase.dll`**.
  - 별명 끝 버전 숫자 (`-0`·`-1`·`-3`) 는 찾을 때 안 본다는 규칙이라 `l1-2-0` 도 이 항목으로 간다.
    - 이 규칙은 결과서가 **일반 규칙대로** 푼 것이다. ntdll `ApiSetpSearchForApiSet` 코드는 안 읽었다 → `[추측]` (Q187 ②).
    - 그래도 최종 결과 (kernelbase) 는 run7 `[관찰]` 콜스택이 받쳐 준다.
- 끝: `KERNELBASE!LoadLibraryExW` (rva `0x21150`, 480바이트, forwarder 아님).

### 2.10 왜 콜스택에 kernel32 가 없나 — `CALL` 과 `JMP`

꼬리 점프 원리는 [run17 해설 2.4](../02_sprint/run17-ldr-init-entry-해설.md#24-꼬리-점프--콜스택에서-함수-하나가-지워지는-원리) 와 같다. 이번 자리에 맞춰 단계별로만.

1. `[정적]` (run30) test.exe `try_get_function_slow +0x6d` 가 `CALL [IAT 0x6e250]`. 여기서 돌아올 주소 `+0x73` 이 쌓인다.
2. `[정적]` 그 칸엔 kernel32 `LoadLibraryExW` (스텁) 주소가 꽂혀 있다.
3. `[정적]` 스텁은 `JMP` — 돌아올 주소를 **안 쌓는다.**
4. `[정적]` `KERNELBASE!LoadLibraryExW` 가 일을 마치고 `RET` 하면, 쌓여 있던 유일한 주소 = test.exe `+0x73` 로 곧장 돌아간다.
5. 그래서 → 콜스택에는 `KERNELBASE!LoadLibraryExW` 바로 위가 test.exe 다. kernel32 칸은 **없다.**
6. `[관찰]` (run7) `KERNELBASE!LoadLibraryExW ← 0x7ff70ee67723` (= test.exe 안, `try_get_function_slow +0x73`). **맞는다.**

- 비유: 손님이 kernel32 건물 문으로 들어갔지만 복도를 지나 KERNELBASE 방에 앉았다. 방문 기록 (콜스택) 엔 "어디서 왔나" 만 적히는데, 복도는 기록을 남기지 않는다. 그래서 기록엔 "test.exe 에서 KERNELBASE 로" 만 보인다.

### 2.11 run30 의 "이름표가 둘" — 둘 다 맞았다

- run30 해설은 "호출 지점 표는 `KERNEL32!LoadLibraryExW`, 트리 · flow.md 는 `KERNELBASE!LoadLibraryExW`. 그 사이는 근거에 없다" 고 짚었다.
- 이번 답 `[정적]`: **둘 다 맞다.**
  - `KERNEL32!LoadLibraryExW` = test.exe 가 **import 하는 이름** (콘센트 이름표)
  - `KERNELBASE!LoadLibraryExW` = 실행이 **실제로 닿는 함수** (콜스택에 보이는 이름)
  - 사이에 스텁 `JMP` 하나가 낀다.
- 그래서 정정은 없다. 앞 문서가 틀린 게 아니라 **비어 있던 고리** 가 채워진 것이다 (run 문서 "이전 run 에서 닫은 것").

#### kernel32 자기 코드는 스텁을 안 거친다 `[정적]`

- kernel32 안 코드 (`BasepInitializeTermsrvFpns +0x6b`, run68 `InitializeRegTermsrvFpns +0x78`) 는 자기 스텁이 아니라 **같은 IAT 칸 `0x8b3e8` 을 직접** 부른다.
- 비유: 건물 직원은 복도 입구 호실을 거치지 않고, 자기 벽 콘센트로 바로 옆 건물에 전화한다.

### 2.12 `LoadLibraryW` · `LoadLibraryA` 도 같은 꼴

| 함수 (kernel32) | rva · 크기 | 가리키는 IAT 칸 | 별명 | 스텁? |
|---|---|---|---|---|
| `LoadLibraryExW` | `0x3bd50` · 7B | `0x8b3e8` | `…libraryloader-l1-2-0` | O |
| `LoadLibraryW` | `0x3ee30` · 7B | `0x8b400` | `…libraryloader-l1-2-1` | O |
| `LoadLibraryA` | `0x428f0` · 7B | `0x8b408` | `…libraryloader-l1-2-1` | O |

- 셋 다 forwarder 아님, 셋 다 kernelbase 로 넘긴다 `[정적]`.
- 덤 `[정적: PE 바이트]` (`.asm` 은 안 뽑음): `KERNELBASE!LoadLibraryW` (10바이트) 는 `xor r8d,r8d; xor edx,edx; jmp KERNELBASE!LoadLibraryExW` → `LoadLibraryW(x)` = `LoadLibraryExW(x, NULL, 0)`.
- test.exe 는 `LoadLibraryExW` 만 쓴다. 그래서 나머지 둘의 kernelbase 쪽 `.asm` 은 "필요할 때만" (Q187 ①).

### 2.13 도구 메모 — Ghidra `.c` 함정 둘

- `ms/decomp/kernel32/LoadLibraryExW.c` 는 `JMP [IAT]` 스텁을 "**자기 자신을 부르는 함수**" 처럼 그린다. `.asm` 이 맞다.
- `IsSystemLUID.c` 는 반환값을 버린 `void` 로 그린다. 실제로는 `.asm` `+0xac` · `+0x8b` · `+0xb3 ~ +0xbb` 에서 값을 돌려준다.
- 새 함정은 아니다. run30 "import 이름으로 찾으면 0줄 — IAT 칸 rva 로 찾는다" 와 같은 무리라 run 문서는 따로 제안하지 않았다.

### 2.14 정리 — 비유로 한 번 더

- **Q59**: 가게 세팅 중, 관리실이 벽 주소록 "출근 인사 담당" 칸 번호로 전화를 건다 (첫 인자 1 = "본업 말고 준비만").
  - 출근 인사 담당은 로비 게시판을 보고 "터미널 서버형 건물" 이라 준비 담당을 부른다.
  - 준비 담당은 들어오자마자 kernel32 수첩 맨 윗줄에 호실 번호 하나를 적고 (**새 짐**), 가게 간판의 인증 스티커를 보고는 그냥 돌아간다.
  - `tsappcmp.dll` 건물은 안 들인다. 수첩의 나머지 12줄은 빈칸이다.
- **Q60**: CRT 가 kernel32 의 `LoadLibraryExW` 호실 문을 연다. 그 호실은 KERNELBASE 로 가는 **복도** 다. 복도는 방문 기록을 안 남겨서, 기록엔 "test.exe → KERNELBASE" 만 보인다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 — 그림 2

그림 2 의 `LdrpInitializeProcess` 뒤쪽과 진입점 쪽이 바뀌었다.

| 노드 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| `z1` | **회색 점선** 상자 "`Kernel32ThreadInitThunkFunction` 칸의 함수" — 모듈 모름 상자에 있었다 | **KERNEL32 (초록) 점선** 상자 "`KERNEL32!BaseThreadInitThunk (1,0,0)`" 로 옮김. 상자 글에 `+0x26 RtlGetSuiteMask` → KUSD `0x110` → `AL & 0x10` 켜짐 |
| `kt1` (새) | 없음 | `z1 -->|"+0x36"| kt1` — **`KERNEL32!BasepInitializeTermsrvFpns`** (점선). 상자 글: `+0x1b` 짐 1칸, `+0x22` → 1 → `+0x2b` 0 반환 |
| `ks1` (새) | 없음 — `m1 -->|"(1) ?"| m2` 로 진입점에서 KERNELBASE 로 바로 갔다 | `m1 -->|"(1) ?"| ks1` 다음 **`ks1 ==> m2`** "`+0x0 JMP 칸 0x8b3e8`". `ks1` = **`KERNEL32!LoadLibraryExW`** 스텁 (점선) |
| `m2` | `KERNELBASE!LoadLibraryExW ×3` (test.exe CRT 가 부름 — run7 13번) | 글에 "import 는 KERNEL32 — 그 스텁에서 JMP 로 옴, 정적 run101" 이 더해짐 |

#### 그림을 읽을 때 짚을 것

- **색은 "어느 건물인가", 점선은 "실행으로 봤나".**
  - `z1` 은 이번에 "KERNEL32 건물" 로 확정돼서 회색 → 초록이 됐다.
  - 하지만 호출이 실제로 일어난 건 아직 못 봤으니 **점선 테두리는 그대로** 다 (flow.md 그림 메모).
- **화살표 두 종류.**
  - `-->` 는 부르기 (`CALL`), `==>` 굵은 화살표는 꼬리 점프 (`JMP`) 다. 그림에 이미 있던 `bm1 ==> bm2` 와 같은 표시다.
  - 돌아옴 점선 `m2 -.-> m1` 은 그대로다. 스텁은 프레임이 없어서 KERNELBASE 가 **곧장** test.exe 로 돌아오기 때문이다 (2.10).
- **같은 함수가 두 상자.**
  - `RtlUserThreadStart` 쪽에 이미 있던 `s1` (`KERNEL32!BaseThreadInitThunk`, 첫 인자 0) 과 이번 `z1` (첫 인자 1) 은 **따로 둔다.** 두 번 불리고 첫 인자도 다르기 때문이다 (flow.md 그림 메모의 R7).
- **일부러 상자로 안 늘린 것.**
  - `+0x26 RtlGetSuiteMask` 는 게시판 한 칸을 읽기만 하는 갈림이라 `z1` 상자 글에만 적었다.
  - `IsTerminalServerCompatible` 등 안쪽도 test.exe 가 `+0x2b` 에서 나가서 상자로 안 늘렸다.
  - dll1 · dll2 쪽 `LoadLibraryExW ×3` (`m4` · `m5`) 은 dll 의 import 모듈을 이번에 안 봐서 그대로다.

### flow.md 에서 바뀐 것 — 글

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 2.4 뒤7 `+0x1b5e` | 대상은 `KERNEL32!BaseThreadInitThunk` `[추측]` (동적 Q48) | 그 줄에 취소선, **(run101 `[정적]`) 대상 = `KERNEL32!BaseThreadInitThunk`** + 인자 · 돌아온 뒤 길 |
| 2.5 `TermsrvGetWindowsDirectoryW` 칸 | 칸을 채우는 쪽은 터미널 서비스 쪽일 것 `[추측]` | (run101 `[정적]`) 칸 = `pfnTermsrvGetWindowsDirectoryW` (`0xc1d10`), test.exe 에선 **0 그대로** |
| 2.10 `BaseThreadInitThunk` 안쪽 | `(1,0,0)` 호출이 이 갈래다 `[추측]` | 취소선 → **(run101 `[정적]`) 맞다** + 이 갈래가 부르는 둘, `RtlGetSuiteMask` 읽는 법, `BasepInitializeTermsrvFpns` 안쪽 |
| 2.10 CRT 안쪽 `LoadLibraryExW` | forwarder 인가 스텁인가 `[확인 필요]` Q60 | 취소선 → **(run101 `[정적]`) 스텁이다** + 바이트 · IAT 칸 · ApiSet. 트리에 `KERNEL32!LoadLibraryExW (7B 스텁)` 한 줄이 끼었다 |
| 4장 | 2.4 · 2.9 줄, 2.10 줄 둘에 이번 질문이 열려 있었음 | 셋 다 이번 몫은 **(해결됨, 정적) run101**. 2.4 · 2.9 줄은 "남은 건 이 호출의 실행 관찰뿐", 2.10 `BaseThreadInitThunk` 줄은 원래 있던 동적 Q30 (첫 인자 0 쪽 간접 호출 대상) 만 남음. 새 줄 둘 (run101 동적 ① ~ ③, 정적 ① ~ ④) |
| 4.1 | 그림 2 회색 상자 줄, 진입점 → `LoadLibraryExW` 줄 | 둘 다 run101 `[정적]` 줄이 더해짐 |

### 짐 지도 `baggage.md` 에서 바뀐 것

- 7.6.8 (다른 DLL `.data`) 표에 KERNEL32 줄 **둘** 이 더해졌다.
  - **`pfnTermsrvSyncUserIniFile`** — 새 짐 (2.8)
  - `BasepInitializeTermsrvFpns` 함수 포인터 12칸 — 0 그대로 (2.6)
- run68 의 9칸 줄에 "칸 범위 `0xc1cc0 ~ 0xc1d00`, 이번 12칸 · `0xc1d08` 과는 다른 묶음" 이 보태졌다.
- 5장 ② 에 뒤7 한 줄, 새 7.33 (이번 바퀴가 넘긴 질문). 2장 숫자는 그대로 (체크리스트 밖).

### 큰 그림에서 어떻게 이어지나

- **초기화 쪽 (Q59)**: `LdrpInitializeProcess` 뒤쪽에 "모듈 모름" 으로 남아 있던 간접 호출 하나가 이름을 얻었다. 이제 뒤7 은 `+0x199d` (ret `+0x19a2`) kernel32 준비 → `+0x1b59` 출근 인사 담당 (준비만) → `+0x1b72` `LdrpInitializePerUserWindowsDirectory` 로 이어진다.
  - 바로 다음 함수 `LdrpInitializePerUserWindowsDirectory` 는 Q71 (sast/102) 이 이미 정적으로 답했다. 다음 바퀴에서 문서로 이어진다.
- **에뮬레이터 쪽**: 이 갈래에서 에뮬레이터가 맞춰야 할 짐은 **kernel32 수첩 한 칸** 뿐이다. syscall 은 없다.
- **진입점 쪽 (Q60)**: CRT 가 ntdll 로더로 다시 들어오는 화살표의 **첫 마디** 가 확정됐다 — test.exe → kernel32 스텁 → (JMP) → kernelbase → ntdll 로더. `LoadLibraryExW` ×3 이 무엇을 올리는지는 여전히 실행 몫이다 (Q31).

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 (동적)

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `+0x1b59` 호출이 **실제로** 일어나고, 인자가 `1, 0, 0` 인가. `BaseThreadInitThunk+0x2d` 에서 `al` 이 `0x10` 켜짐인가. `BasepInitializeTermsrvFpns` 입구에 **한 번만** 서나. `+0x2b` 로 나가고 `BaseThreadInitThunk+0x3b` 의 `eax` 가 0 인가 | 정적은 길만 안다. 값은 사진 두 장 사이를 이어 짐작했을 뿐이다 | **Q48** (동적, run101 몫) |
| 새 짐 `pfnTermsrvSyncUserIniFile` 의 **실제 값** (P1a 0 → P1b · P2 `kernel32+0x72b60` 기대), 12칸 · 9칸이 정말 0 인가 | run44 는 ntdll `.data` 만 읽었다. kernel32 `.data` 는 안 읽었다 → `[확인 필요]` | **Q79** (사진 덤프 오프라인 읽기 — 새로 돌리지 않아도 됨) |
| `KERNELBASE!LoadLibraryExW` 입구 `k` 에서 바로 위가 `test!try_get_function_slow+0x73` 인가 (kernel32 칸 없음) | run7 `[관찰]` 과 맞는다고 봤지만, 스텁에 멈춤점을 걸고 직접 확인한 적은 없다 | **Q31** (덤) |

### 정적으로 남은 것

run 문서 "아직 확인 필요 — 정적" 은 네 개다. 대기열 번호와 순서가 조금 다르니 주의.

| run 문서 번호 | 무엇 | 대기열 |
|---|---|---|
| 정적 1 | `LdrpInitializeProcess +0x1b72 LdrpInitializePerUserWindowsDirectory` — 바로 뒤 함수, flow.md 2.4 · 2.6 "뜻 모름" | **Q71** (sast/102) 이 이미 답했다 — 다음 바퀴 몫 (이 글에선 안 다룸) |
| 정적 2 | (필요할 때만) kernelbase `LoadLibraryW` · `LoadLibraryA` `.asm` — 지금은 PE 바이트만 | **Q187 ①** |
| 정적 3 | (낮음) ntdll `ApiSetpSearchForApiSet` — "끝 버전 숫자는 안 본다" 규칙 (지금 `[추측]`) | **Q187 ②** |
| 정적 4 | (낮음) test.exe 의 다른 KERNEL32 import (`OutputDebugStringA` 등) 도 같은 7B 스텁인가 — run36 forwarder 목록 밖이면 같은 꼴일 것 `[추측]` | **Q187 ③** |

### 이번 바퀴가 일부러 안 본 것

- `TermsrvGetWindowsDirectoryW` 가 test.exe 에서 **무엇을 돌려주나** (칸 0 일 때 "그냥 돌아옴" 의 값) — run25 결과서 몫이라 이번에 안 봤다 (run 문서 "보탠 것").
- 비트 이름 `0x10` = TERMINAL · `0x100` = SINGLEUSERTS — `[추측]` 그대로.

---

## 5. 더 읽기

- run 문서: [`run101-k32-leftovers.md`](run101-k32-leftovers.md)
- 요청서 · 결과서: [`_workspace/sast/97_k32-leftovers.md`](../../_workspace/sast/97_k32-leftovers.md) · [`97_k32-leftovers_result.md`](../../_workspace/sast/97_k32-leftovers_result.md)
- flow.md: [그림 2 · 그림 메모](flow.md), 2.4 (뒤7 `+0x1b5e`), 2.5 (`TermsrvGetWindowsDirectoryW` 칸), 2.10 (`BaseThreadInitThunk` 안쪽 · CRT 안쪽 `LoadLibraryExW`), 4장 · 4.1
- 짐 지도: [`baggage.md`](baggage.md) 7.6.8 · 7.33
- 앞 바퀴 짝:
  - [run25 해설 2.6](../02_sprint/run25-dll-entry-해설.md#26-핵심--basethreadinitthunk-는-kernel32-안-진짜-코드다) — `BaseThreadInitThunk` 첫 소개
  - [run30 해설 2.7](../02_sprint/run30-exe-crt-start-해설.md#27-핵심--run7-의-67723-은-누구인가--아래-16비트-맞추기) — `…67723` 과 "이름표가 둘"
  - [run68 해설 2.11](run68-dll-init-rest-해설.md#211--testexe-에서-안-타는-셋--까닭은-모두-갈림길의-값) — 다른 9칸 (`InitializeRegTermsrvFpns`) 과 TS 인식 표시
  - [run44 해설](run44-baggage-snap-해설.md) — 사진 P1a · P1b · P2

# run89 dotnet-start — 해설

> 이 글은 [run89 문서](run89-dotnet-start.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q152** — PE_emulator 요청 30 (.NET exe 시작 길) (`_workspace/plan.md`, 이번에 **끝**)
  - 한 바퀴로 끝났다. 이것으로 **PE_emulator 요청 26 ~ 30 이 모두 끝났다**.
  - 다음 항목: **Q142** (정적은 sast/77 로 끝, 문서만 남음) → [멈춤] (Q104 VM 단계 앞).
  - 이번에 남은 것이 간 곳 (4절):
    - 정적 → 새 **Q166** ① ~ ⑦
    - 동적 → 새 **Q167** (.NET exe 가 있어야 봄. 무엇을 언제 돌릴지는 **사용자 결정 몫**)
- 근거:
  - run 문서 [`run89-dotnet-start.md`](run89-dotnet-start.md) (가장 중요)
  - 요청서 [`_workspace/sast/86_dotnet-start.md`](../../_workspace/sast/86_dotnet-start.md) · 결과서 [`86_dotnet-start_result.md`](../../_workspace/sast/86_dotnet-start_result.md)
    - 판정 **분석성공**. 단 질문 3 은 덤프에 파일이 없어 이름 수준 `[추측]`, 질문 2 의 syscall 이름은 API 수준 `[추측]`.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 흐름도 앞 글 · 2.4 (새 절 **"CLR 헤더 갈래"**) · 2.6 · 2.10 · 4장 · 4.1 · 5장. **그림은 안 바뀜** (자세히는 3절 표)
  - 짐 지도 [`baggage.md`](baggage.md): 새 짐 칸은 없고, **모르던 칸 넷을 닫음** (마흔다섯째판) + 7.1 묵은 숫자 바로잡음
- 이번 바퀴는 **정적만** 이다. 새 VM 실행은 없고 동적 판정도 없다.
  - 대신 run44 사진 로그를 **다시 읽어서** test.exe 쪽 값 몇 개에 `[관찰]` 을 붙였다 (2.12).

### 이번 바퀴에서 조심할 곳 다섯

1. **test.exe 는 이 길을 하나도 안 탄다.**
   - 이번 이야기는 거의 다 "**.NET exe 라면**" 이다.
   - test.exe 쪽은 "정말 안 탔나" 만 확인했다 — 사진 끝까지 `UseCOR` 0 `[관찰]` (2.12).
   - 그래서 흐름도 **그림은 그대로** 다. 갈래는 flow.md 2.4 에 글로만 들어갔다.
2. **"덤프" 가 두 뜻으로 나온다.**
   - "덤프에 있나 · 덤프에 더할 파일" 의 덤프 = **Windows 파일을 떠 온 모음** (`ms/bin`, PE_emulator 가 쓰는 것과 같은 꼴). 메모리 사진이 아니다.
   - "사진 · 사진 로그" = run44 에서 멈춘 순간 찍은 **메모리 사진** ([run44 해설](run44-baggage-snap-해설.md) 2.2).
3. **첫 답은 지도 수준이다** (사용자 결정 2026-11-12).
   - ntdll 갈래와 mscoree 의 첫 단계는 `[정적]` 으로 줄까지 봤다.
   - 그 뒤 (`mscoreei.dll` → `clr.dll`) 는 덤프에 파일이 없어서 **이름 · 경로까지만** `[추측]` 이다.
4. **`+0x1a7f` 는 `LdrpInitializeProcess` 안의 call 자리다** (결정 기록 정정).
   - plan.md 결정 기록 (2026-11-13 "Q152 정적 끝" 줄) 에 "IL-only 면 import 표를 안 걸음 (`LdrpCorProcessImports +0x1a7f`)" 라고 적혀 있다. 그렇게 읽으면 `LdrpCorProcessImports` 함수 **안** 오프셋처럼 보인다.
   - 그 함수는 97B 뿐이라 `+0x1a7f` 가 있을 수 없다.
   - run 문서를 따른다: `LdrpInitializeProcess +0x1a7f` (call) / `+0x1a84` (ret). "import 를 안 걷는" 진짜 자리는 **갈림 `+0x1a73`** 과 **`+0x1a87 JMP`** 다 (2.8).
5. **mscoree 에는 같은 이름 함수가 둘씩 있다.**
   - `_CorExeMain` 은 rva `0x7850` (첫 몸, 58B) 과 `0x2f6e8` (둘째 몸, 91B). `LoadLibraryShim` 은 `0x2740` 과 `0x23ee0`.
   - 오프셋을 읽을 때 **어느 몸인지** 같이 봐야 한다. `_calls.tsv` 는 둘을 한 묶음으로 섞어서, 줄은 `.asm` 으로 읽었다 (run 문서 도구 메모).

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) = 가게 **안내판**, TEB = 직원 **개인 안내판** → [run11 해설](../02_sprint/run11-allocate-thread-해설.md) 2.2
- CONTEXT = **업무 시작 메모** ("어디서부터 (`Rip`)", "**처음 손에 쥘 것 (`Rcx`)**"), 유저 스택에 맡긴 원래 메모 = **사물함 메모** (= 맡긴 문맥), `LdrInitializeThunk` = **오리엔테이션실**, `RtlUserThreadStart` = **본업 자리** → [run16 해설](../02_sprint/run16-thunk-context-해설.md) 2.3
- `NtContinue` (은퇴) = 사물함 메모를 내고 본업 자리로 순간 이동 → [run29 해설](../02_sprint/run29-continue-start-해설.md)
- `LdrpInitializeProcess` 의 앞쪽 10묶음 (묶음 5 ".NET 검사") → [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.4, 뒤쪽 10묶음 (뒤1 · 뒤4 · 뒤6 …) · `[RSP+0xe0]` = 받은 CONTEXT · `+0x18b4` 짐작 → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.2 · 2.5
- WoW64 (64비트 Windows 에서 32비트 프로그램을 돌리는 장치) → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.9
- DLL = **건물**, 모듈 항목 = **명부 한 줄**, 로드 문맥 = **작업 지시서** → [run23 해설](../02_sprint/run23-load-dependent-해설.md)
- import 마다 배달 기사에게 맡기기 (`LdrpMapAndSnapDependency`) → [run22 해설](../02_sprint/run22-map-snap-dep-해설.md) 2.4, `CorILOnly` (명부 줄 비트 24) → 같은 해설 2.5
- DDAG 노드 = **연결 카드**, 노드 `State` = **진행 도장** → [run27 해설](../02_sprint/run27-init-graph-해설.md) 2.1
- 재배치 = **번지수 고쳐 쓰기**, 데이터 디렉터리 5번 (재배치), DLL 쪽 `.NET` 디렉터리 14 → [run38 해설](../02_sprint/run38-map-module-해설.md) 비유 · 2.7 · 2.8
- TLS 디렉터리 → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) 2.3
- 이미지 = 설계도로 짠 **조립식 건물 세트**, PE 머리 `Magic` `0x10b` / `0x20b` → [run31 해설](../02_sprint/run31-image-section-해설.md) 2.3 · 2.7, 진입점 값이 흘러가는 길 → 같은 해설 2.11
- `KUSER_SHARED_DATA` (KUSD) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- KUSD `Cookie` 로 섞어 두고 풀기 = 금고 번호를 **암호로 적어 둔 쪽지** → [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.10, 프로세스 쿠키 (**열쇠**) · 포인터 섞기 → [run59 해설](run59-init-leftovers-해설.md) 2.3 · 2.7
- 꼬리 점프 (돌아오지 않는 `JMP`, 콜스택에 안 남음) → [run17 해설](../02_sprint/run17-ldr-init-entry-해설.md) 2.4
- CFG 중개 (**교환원**) → [run28 해설](../02_sprint/run28-tls-init-call-해설.md) 2.9
- 지연 import = **"필요하면 그때 부를게요" 메모** → [run25 해설](../02_sprint/run25-dll-entry-해설.md)
- 사진 K1 ~ P2, raw `dq`, `끝까지 0` → [run44 해설](run44-baggage-snap-해설.md) 2.8 · 2.9 · 2.10, "조건부 안 탐 · 0 을 씀" → [run47 해설](run47-section-chain-해설.md)

이번에 처음 나오는 말 — **.NET · CLR · IL**, **IL-only · 혼합 모드**, **CLR 헤더 (데이터 디렉터리 14)**, **AnyCPU**, **shim (심)**, **앱호스트**, (짧게) **HRESULT** — 은 본문에서 처음 나올 때 한두 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, PEB = **안내판**, TEB = **개인 안내판**, ntdll = 가게 안 **관리실**, 커널 = **관리사무소**.
- exe · DLL 이미지 = 설계도로 짠 **조립식 건물 세트**, PE 머리 = 설계도 **첫 장**.
- CONTEXT = **업무 시작 메모**, 맡긴 문맥 = **사물함 메모**, `Rcx` 칸 = "**처음 손에 쥘 것**". 보통은 가게 **입구 번지** (exe 진입점) 가 적힌다.
- KUSD = 로비 **공용 게시판**.
- 이번에 새로 붙는 그림. flow.md 2.4 새 절의 "딱지 · 통역사" 그림을 이 시리즈 그림에 맞춰 잇는다.
  - .NET exe 는 업무 지시가 **외국어 (IL)** 로 쓰여 있다. 직원 (CPU) 이 그대로는 못 읽는다.
  - 그래서 설계도 첫 장에 **"통역 필요" 딱지** (CLR 헤더) 가 붙어 있다.
  - 관리실은 딱지를 보면 **통역사 소개 창구** (mscoree) 를 직접 들인다. 그리고 사물함 메모의 "처음 손에 쥘 것" 칸을 **창구 번호** 로 고쳐 쓴다.
  - 창구는 동네에서 **통역사 사무실** (`mscoreei.dll`) 을 찾아 연결해 준다. 진짜 **통역사** 는 `clr.dll` 이다 `[추측]` (2.9).
  - flow.md 는 줄여서 mscoree 를 "통역사" 라고 불렀다. 그런데 이번 결과를 보면 mscoree 는 직접 통역하지 않고 **누구를 부를지 골라 넘겨줄 뿐** 이다. 그래서 이 글은 "소개 창구" 라고 부른다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 (요청서 번호) | 답 (run 문서 그대로) |
|---|---|
| 1 ntdll 로더의 CLR 갈래 | ntdll 은 PE 머리 **두 칸** (Magic · 데이터 디렉터리 14) 만 보고 `UseCOR` 를 세운다. 그러면 `MSCOREE.DLL` 을 **ntdll 이 직접** 올리고 (`+0x184d`), 맡긴 문맥 `Rcx` 를 `_CorExeMain` 으로 **덮는다** (`+0x18b4`). IL-only 면 exe import 표를 **안 걷는다** (`+0x1a73`) `[정적]` (2.4 ~ 2.8) |
| 2 `_CorExeMain` → 런타임 고르기 | `InitShimImpl` 이 환경변수 셋 · 레지스트리 두 키 · `mscoree.dll.local` 을 보고 `<InstallRoot>\<버전>\mscoreei.dll` 을 찾아 `LoadLibraryExW(…, 8)`. 못 찾으면 상태 3 → 옛 길 `GetInstallation` → `ExitProcess` `[정적]`. API → NT 함수 짝은 `[추측]` (2.9) |
| 3 `clr.dll` 시작까지 | 덤프에 파일이 없어 mscoree 경계까지만 `[정적]`, 그 뒤는 이름만 `[추측]` (2.9 끝) |
| 4 Framework / Core 가르기 | 로더 기준 ① Magic `0x10b` + 비 WoW64 → AnyCPU ② 디렉터리 14 → Framework x64 ③ 둘 다 아님 → 네이티브 (앱호스트 포함). **"mscoree import 가 있나" 로 가르면 틀린다** (2.10) |
| 5 덤프에 더할 것 · 할 만한가 | 더할 것 = `mscoreei.dll` · `clr.dll` 외 + 레지스트리 값. **1단계 (ntdll 갈래 + mscoree `InitShimImpl`) 는 할 만하고, 2단계 (mscoreei → clr) 는 지금은 아님** (2.11) |
| (덤) test.exe | 이 갈래를 하나도 안 탄다 — `UseCOR` 등 끝까지 0 `[관찰]` run44, 디렉터리 14 없음 `[정적+샘플]` (2.12) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

run88 은 DLL 을 들이는 구간 (flow.md 2.5 · 2.6) 이었다.

이번엔 같은 `LdrpInitializeProcess` 안이지만, 한 곳이 아니라 **여기저기 흩어진 자리들** 이다. 그리고 은퇴 뒤 진입점 자리까지 이어진다.

```
cmd.exe 의 부탁 (CreateProcess)
 ├─ 커널: 프로세스 · PEB · 첫 스레드 (멈춘 채)
 │    └─ PspAllocateProcess +0xa8c PspSelectMachineForProcess    어느 기계로 띄울지   ← 이번엔 이름만
 └─ 첫 스레드 깸 → ntdll!LdrInitializeThunk → … → LdrpInitializeProcess (flow.md 2.4)
      ├─ 앞쪽  +0x334 UseCOR = 0 → +0x602 · +0x662 머리 보고 판정               ← 이번 ① (2.4 · 2.5)
      ├─ 뒤1   +0x13e0 · +0x13ec  exe 명부 줄에 도장                            ← 이번
      ├─ 뒤4   +0x184d mscoree 들이기 → +0x18b4 사물함 메모 Rcx 고쳐 쓰기        ← 이번 ② (2.6 · 2.7)
      ├─ 뒤6   +0x1a73 import 걷기 갈림                                          ← 이번 ③ (2.8)
      … → NtContinue (은퇴) → RtlUserThreadStart(rcx) → BaseThreadInitThunk
             ├─ test.exe:  exe 진입점 → (CRT) → main
             └─ .NET exe:  mscoree!_CorExeMain → InitShimImpl → (mscoreei → clr)  ← 이번 ④ (2.9, 지도 밖)
```

- 커널 쪽 `PspSelectMachineForProcess` 는 **호출 줄만** 봤다 (`_calls.tsv`). 하는 일은 `[추측]` 이다 (Q166 ①).
- `+0x602` · `+0x662` 는 run18 해설 2.4 의 **묶음 5** (`+0x57a ~ +0x773`, ".NET 검사") 안이다.
- 뒤1 · 뒤4 · 뒤6 은 run19 해설 2.5 의 뒤쪽 묶음 번호다.
- 맨 아래 mscoree 쪽은 진입점 **뒤** 라 흐름도 지도 밖이다 (flow.md 2.4 새 절 끝). 줄 · 표는 run 문서에만 있다.

### 왜 이 질문이 지금 왔나

- PE_emulator 요청 30 이다. 원문 급함은 **낮음**, 요청 26 ~ 30 중 5순위 — 마지막이다.
- 샘플 중 CLR 헤더가 있는 x64 exe 가 **310개 (2%)** 다.
- PE_emulator 는 .NET exe 를 돌려 본 적이 없고, 로더 흉내에 CLR 갈래가 없다 `[추측]` (요청서).
- 사용자 결정 (2026-11-12): 첫 답은 **"무엇이 필요한가" 지도** 다. 할 만한지 판단하려고.

### 무엇이 궁금했나

요청서 질문 다섯.

1. **ntdll 로더의 CLR 갈래**
   - 어디서 CLR 헤더를 보나, IL-only 검사는 어디서 하나.
   - `LdrpCorValidateImage` · `LdrpCorFixupImage` · `LdrpCorProcessImports` 를 누가 · 언제 부르나.
   - **진입점을 `mscoree!_CorExeMain` 으로 바꾸는 자리** — exe `AddressOfEntryPoint` 를 무시하나, 시작 문맥 `Rcx` 를 바꾸나.
   - **mscoree 를 언제 · 어떻게 올리나** — exe import 로? 로더가 따로?
   - x64 IL-only exe 와 혼합 모드 exe 는 어디서 갈리나.
2. **`_CorExeMain` → 런타임 고르기 첫 단계** — 읽는 레지스트리 · 파일 · API · syscall 목록. mscoreei 가 없으면 어디서 무슨 오류로 끝나나.
3. **`clr.dll` 시작까지 큰 줄기** — 이름 · 경로만.
4. **.NET Core / 5+ exe 와 Framework exe 를 PE 머리로 가르는 법.**
5. **덤프에 더할 파일 목록** 과 **"할 만한가" 한 줄**.

### 바퀴 전에 알던 것

- `[정적]` (run18) 앞쪽 묶음 5 에 ".NET 검사" 가 있다. `+0x627 LdrpCorFixupImage` 는 PE32 (`0x10b`) 머리 + `TEB.WowTebOffset <= 0` 일 때만 부른다. x64 exe (PE32+) 면 건너뛴다.
- `[정적]` (run19) 뒤쪽 .NET 자리들이 이름으로 보였다.
  - `+0x184d LdrpCorInitialize` — `UseCOR != 0` 일 때.
  - `+0x193c` 그래프 — `[RSP+0x178]` ≠ 0 일 때만.
  - `+0x1a73 TEST R12B ; JZ` — IL-only 가 아니면 import 걷기, IL-only 면 `+0x1a7f LdrpCorProcessImports`.
- `[정적]` + `[추측]` (run19 해설 2.2)
  - `+0x18b4 MOV [RBX+0x80],RAX` 가 CONTEXT 의 `Rcx` 칸 쓰기라는 건 `[정적]` 이었다.
  - ".NET exe 는 메모의 '처음 손에 쥘 것' 칸을 진입점 대신 `LdrpCorExeMainRoutine` 으로 바꿔 쓰는 것으로 보인다" 는 `[추측]` 이었다 (그 칸 뒤의 일은 안 봤다).
- `[정적]` (run22 · run26 · run38) DLL 쪽:
  - 명부 줄 `CorILOnly` 면 import 대신 `LdrpCorProcessImports`.
  - `LdrpProcessMachineMismatch` 는 디렉터리 14 를 보고, IL 전용이면 기계 종류가 달라도 받아 준다.
  - `LdrpRelocateImage` 는 IL 전용이면 안 고친다.
- `[추측]` (flow.md 2.4) test.exe 면 `UseCOR` = 0, `R12B` = 0 일 것 (동적 Q44 · Q46).
- 짐 지도: `UseCOR` · `LdrpCorExeMainRoutine` · 명부 줄 `CorImage` · `CorILOnly` 는 사진엔 끝까지 0 이었다. 그런데 **쓰는 자리를 몰라서** "모르는 칸" (7.2 · 7.3) 에 있었다.

---

## 2. 찾은 것

### 2.1 큰 그림 먼저 — 한 함수 안에서 두 길이 갈라지는 자리

run 문서 "우리 지도에서 어디서 갈라지나" 그림을 줄였다.

- `─` = test.exe 길, `═` = .NET 만.
- 모두 `[정적]` 이고, 끝값 하나만 `[관찰]` 이다.
- 라벨은 대부분 **ret 자리** 다 (예: `+0x62c` ← call `+0x627`).

```
LdrpInitializeProcess (flow.md 2.4)
 │  +0x334  UseCOR = 0 · IL-only 표시 = 0             ← test.exe 도 지남. 끝값 0 [관찰] run44
 │  +0x602  Magic == 0x10b 이고 WowTebOffset ≤ 0 ?
 │     ═ 예 → +0x611 UseCOR = 1 · IL-only = 1 → +0x62c LdrpCorFixupImage          (AnyCPU)
 │  +0x667  RtlImageDirectoryEntryToData(exe, TRUE, 14)    (UseWOW64 0 · UseCOR 0 일 때만)
 │     ═ NULL 아님 → +0x66c UseCOR = 1 · +0x67f IL-only = (Flags & 1)               (x64 / 혼합)
 │  +0x13e0 · +0x13ec  exe 명부 줄 Flags 비트 22 CorImage · 비트 24 CorILOnly
 │  +0x183c  UseCOR ?
 │     ═ 예 → +0x1852 LdrpCorInitialize (MSCOREE.DLL 들임) → +0x186c LdrpCorValidateImage (IL-only)
 │            → +0x18b4 맡긴 CONTEXT.Rcx ← _CorExeMain (첫 스레드)
 │  +0x1941  LdrpInitializeGraphRecurse (mscoree 노드 — [RSP+0x178] ≠ 0 일 때만)
 │  +0x1a73  IL-only ?
 │     ═ 예 → +0x1a84 LdrpCorProcessImports → +0x1a87 JMP +0x1af2   (import · 일감 · 실행 준비 다 건너뜀)
 │     ─ 아니오 → +0x1aa9 LdrpMapAndSnapDependency → … → +0x1af2    (flow.md 2.6 — test.exe · 혼합 모드)
 └─ (은퇴, 2.10) NtContinue → RtlUserThreadStart(rcx)    rcx = exe 진입점 (test.exe) / _CorExeMain (.NET)
```

읽는 요령 하나.

- **판정하는 곳** (`+0x602`, `+0x662`) 은 위쪽에 있다.
- 그 판정을 **쓰는 곳** (`+0x13e0`, `+0x183c`, `+0x1a73`) 은 멀리 아래에 있다.
- 둘을 이어 주는 건 전역 `UseCOR` 와 **IL-only 표시** 다. IL-only 표시는 스택 `[RSP+0x58]` 에 두었다가 `+0x13d2` 에서 `R12D` 로 다시 싣는다.

**새 말 — .NET · CLR · IL**

- **.NET (Framework)** = Windows 에 들어 있는 프로그램 실행 틀로 알려져 있다. 이 틀용 exe 는 기계어 대신 **IL** 을 담는다 `[추측]` (일반적으로 알려진 설명).
- **IL** (중간 언어) = CPU 가 바로 못 읽는 중간 단계 코드다. 실행할 때 **CLR** (공용 언어 런타임) 이 기계어로 바꿔 돌린다 `[추측]` (일반적으로 알려진 설명). IL 로 된 코드를 **관리 코드** 라고도 부른다.
- 이번 근거에서 CLR 의 몸은 `clr.dll` 이라는 **이름까지만** 나온다 `[추측]` (2.9 끝).
- 비유: 업무 지시가 **외국어** 로 쓰인 가게다. 직원은 그대로 못 읽고, **통역사** 가 옆에서 풀어 줘야 한다.

**새 말 — IL-only · 혼합 모드**

- **IL-only** = 안에 IL **만** 든 이미지. CLR 헤더 `Flags` 비트 0 (ILONLY) 으로 표시한다 `[정적]` (`+0x673 TEST [RAX+0x10],R15B` 가 `Flags & 1` 을 봄). run19 해설에선 이름만 한 줄로 봤다.
- **혼합 모드** (C++/CLI) = 기계어와 IL 이 **같이** 든 exe. `Flags` 비트 0 이 꺼져 있다. 덤프의 `FileHistory.exe` 가 `Flags = 0x8` 이다 `[정적: PE 표]`.
- 비유: IL-only = 지시서가 **전부** 외국어. 혼합 모드 = 모국어 지시서에 외국어 장이 **섞여** 있다.

---

### 2.2 한 줄씩 읽는 법

#### 줄 1: `═ 예 → +0x1852 LdrpCorInitialize (MSCOREE.DLL 들임)`

- **누가**: 첫 스레드의 ntdll 로더 `LdrpInitializeProcess` (뒤4).
- **어디서**: CALL 이 `+0x184d`, 돌아올 자리가 `+0x1852`.
- **언제만**: `+0x183c CMP UseCOR` · `+0x1843 JZ` — `UseCOR` 가 0 이면 건너뛴다. test.exe 는 건너뛴다.
- **누구를**: `LdrpCorInitialize` (rva `0x8a808`). 이 함수를 부르는 곳은 여기 **하나** 다 (결과서).
- **무엇을 넘기나**: `&[RSP+0x178]` — mscoree 명부 줄을 받을 칸.
- **무엇을 위해**: `MSCOREE.DLL` 을 들이고, 그 안 `_CorExeMain` 주소를 찾아 섞어 둔다 (2.6).
- **돌아온 뒤**: `+0x1856 TEST` · `+0x1858 JS` — 음수면 프로세스 초기화 실패 출구로 간다.

#### 줄 2: `→ +0x18b4 맡긴 CONTEXT.Rcx ← _CorExeMain (첫 스레드)`

이 줄은 call 이 아니라 **값이 바뀌는 순간** 이다.

- **누가**: 같은 `LdrpInitializeProcess`.
- **어디서**: `+0x18b4 MOV [RBX+0x80],RAX`.
- **어디에 쓰나**:
  - `RBX = [RSP+0xe0]` (`+0x18ac`) = 함수 입구에서 받아 둔 **CONTEXT**. 곧 사물함 메모다.
  - `+0x80` = PDB `_CONTEXT` 의 **`Rcx` 칸** (run19 해설 2.2).
- **무슨 값을**: `RAX` = 전역 `LdrpCorExeMainRoutine` 을 KUSD `Cookie` 로 풀어 낸 `mscoree!_CorExeMain` 주소 (`+0x188c ~ +0x18a9`).
- **언제만**: `UseCOR` 이고, TEB `SameTebFlags & 0x400` (`InitialThread` — 첫 스레드) 일 때 (`+0x1878 ~ +0x188a`).
- **무엇을 위해**: 은퇴 뒤 본업 자리가 **exe 진입점 대신 `_CorExeMain`** 을 부르게 하려고 (2.7).

#### 줄 3: `═ 예 → +0x1a84 LdrpCorProcessImports → +0x1a87 JMP +0x1af2`

- **누가**: 같은 함수, 뒤6.
- **어디서**: 갈림 `+0x1a73 TEST R12B,R12B` · `+0x1a76 JZ +0x1a89`. IL-only 면 아래로 떨어져 CALL `+0x1a7f` / ret `+0x1a84`.
- **누구를**: `LdrpCorProcessImports` (rva `0xb7e0`, 97B).
- **무엇을 위해**: exe import 를 걷지 않고, **진행 도장만 6 → 7** 로 찍고 알림 셋을 보낸다 (2.8).
- **돌아온 뒤**: `+0x1a87 JMP +0x1af2` — import 걷기부터 실행 준비까지를 **한 번에 건너뛴다**.

---

### 2.3 비유 하나로 처음부터 끝까지 (.NET exe 라면)

자세히 들어가기 전에, [run16 해설](../02_sprint/run16-thunk-context-해설.md) 2.3 의 이야기에 이번 갈래를 끼워 넣어 본다.

오른쪽 태그는 **코드 쪽 사실** 에 붙은 것이다. 비유 자체엔 태그가 없다.

| # | 이야기 | 코드 | 태그 |
|---|---|---|---|
| 0 | 관리사무소가 가게를 차릴 때 "어느 크기 가게로?" 를 정한다. 그 안에 .NET 정보를 묻는 갈래가 있다. | `PspAllocateProcess +0xa87` `PspSelectMachineForProcess` → (떨어진 조각) `+0x10a860` `PspQueryComPlusRunUnderWoW` | 호출 줄 `[정적]`, 하는 일 `[추측]` |
| 1 | 새 직원 업무 시작 메모의 "처음 손에 쥘 것" 칸엔 **가게 입구 번지** (exe 진입점) 가 적힌다. | 커널이 `Rcx` = exe 진입점 | `[정적]` (run16 · run31) |
| 2 | 오리엔테이션실 (ntdll 로더) 이 설계도 첫 장에서 **딱지 칸 두 개** 를 본다. | `+0x602` Magic · `+0x662` 디렉터리 14 | `[정적]` |
| 3 | 첫 장이 작은 가게 (32비트) 양식인데 큰 가게 (64비트) 로 왔으면, 그 자리에서 첫 장을 큰 가게 양식으로 **고쳐 쓴다**. | `+0x627 LdrpCorFixupImage` | `[정적]` |
| 4 | 딱지가 있으면 **통역사 소개 창구** 를 관리실이 직접 들인다. 가게 짐 목록 (import) 을 보고 들이는 게 아니다. | `+0x184d LdrpCorInitialize` → `LdrpLoadDll` (`MSCOREE.DLL`) | `[정적]` |
| 5 | 창구 번호 (`_CorExeMain` 주소) 를 **공용 게시판 열쇠로 암호화** 해서 칠판에 적어 둔다. | `LdrpCorInitialize +0x1bb` → `LdrpCorExeMainRoutine` | `[정적]` |
| 6 | 첫 직원이면 사물함 메모의 "처음 손에 쥘 것" 칸을, 암호를 풀어 **창구 번호로 고쳐 쓴다**. 가게 입구 번지는 묻힌다. | `+0x18b4 MOV [RBX+0x80],RAX` | `[정적]` |
| 7 | 지시서가 전부 외국어 (IL-only) 면, 가게 건물의 **짐 목록 따라 건물 들이기 · 전선 꽂기** 를 통째로 건너뛴다. | `+0x1a73` 갈림 → `+0x1a84` → `+0x1a87 JMP +0x1af2` | `[정적]` |
| 8 | 오리엔테이션이 끝나 사물함 메모대로 본업 자리로 간다. 직원이 처음 손에 쥔 건 **창구 번호** 다. | `NtContinue` → `RtlUserThreadStart(rcx = _CorExeMain)` → `BaseThreadInitThunk` → `mscoree!_CorExeMain` | `[정적]` (flow.md 2.10) |
| 9 | 창구가 환경변수 · 레지스트리 · 파일을 보고 **통역사 사무실** (`mscoreei.dll`) 을 찾아 연결한다. | `InitShimImpl` → `LoadLibraryExW(…, 8)` | `[정적]` |
| 10 | 사무실에서 진짜 통역사 (`clr.dll`) 가 나와 일을 시작한다. | mscoreei → clr | `[추측]` (덤프에 파일 없음) |
| 9' | 동네에 사무실이 없으면 (지금 덤프) "못 찾음" 도장 → 옛날 방식으로 한 번 더 → 그래도 없으면 가게 문을 닫는다. | 상태 3 → `_CorExeMain` 둘째 몸 → `GetInstallation` → `ExitProcess` | 줄은 `[정적]`, 실제로 이 끝에 닿는 건 `[추측]`, 끝 값은 `[공백]` |

- 핵심은 **4 · 6 · 7** 이다.
  - mscoree 를 들이는 손도, 진입점을 바꾸는 손도, import 를 안 걷게 하는 손도 **모두 ntdll 로더** 다.
- 1번 칸에 x64 IL-only exe 는 무엇이 적히나?
  - 그 exe 는 `AddressOfEntryPoint = 0` 이다 `[정적: PE 표]`.
  - 커널이 그런 exe 의 처음 `Rcx` 로 무엇을 넣는지는 **모른다** → Q166 ⑥ · Q167 1.

---

### 2.4 ★ 판정 — 설계도 첫 장의 두 칸만 본다

**새 말 — CLR 헤더 (데이터 디렉터리 14)**

- PE 머리 안에는 번호 붙은 **목차 칸** (데이터 디렉터리) 들이 있다. 5번 = 재배치 (run38 해설), 9번 = TLS (run28 해설) 처럼.
- **14번 칸이 CLR 헤더** 를 가리킨다. .NET 이미지라는 표시와 그 정보를 담은 작은 표다 (요청서는 "COM descriptor" 라고도 적었다).
- 이번에 쓰는 CLR 헤더 칸은 **`Flags`** (`+0x10`) 하나다 `[정적]`.
  - 비트 0 = **ILONLY** (`LdrpInitializeProcess +0x673` 이 `Flags & 1` 을 봄)
  - 비트 1 = **32BITREQUIRED** (`LdrpCorFixupImage +0x69 TEST [RAX+0x10],2`)
- 덤프의 관리 코드 exe 11개는 모두 CLR 헤더 크기 (`cb`) 72, 런타임 버전 `2.5`, 메타데이터 버전 글자 `v4.0.30319` 였다 (결과서, 바이트 읽음).
- 비유: 설계도 첫 장의 **"통역 필요" 딱지**.

#### 판정은 두 길 `[정적]`

| 길 | 조건 | 결과 | 줄 |
|---|---|---|---|
| (a) PE32 길 | 머리 `Magic == 0x10b` (PE32) **이고** `TEB.WowTebOffset ≤ 0` (WoW64 아님) | `UseCOR = 1`, IL-only = 1, 곧바로 `LdrpCorFixupImage`. **디렉터리 14 는 안 본다** | `+0x602 CMP [R15+0x18],0x10b` · `+0x60d TEST ECX` · `+0x60f JG` → `+0x611` · `+0x618` · `+0x61b` → `+0x627 / +0x62c` |
| (b) 디렉터리 14 길 | (a) 가 아니고 `UseWOW64 == 0` · `UseCOR == 0` 일 때만 | `RtlImageDirectoryEntryToData(base, TRUE, 14)` 가 NULL 이 아니면 `UseCOR = 1`, IL-only = `Flags & 1` | `+0x63b` · `+0x644` → `+0x662 / +0x667` → `+0x66c` · `+0x673` · `+0x67b CMOVNZ` · `+0x67f` |

- 전역 `UseCOR` (ntdll `.data` rva `0x1d292c`, 1B) 를 **쓰는 곳은 셋뿐** 이다 `[정적]` (`_refs/UseCOR.tsv`).
  - `+0x334` (0) · `+0x611` (1) · `+0x66c` (1).
  - 그래서 다른 곳에서 몰래 켜질 일은 없다.
- IL-only 표시는 `[RSP+0x58]` 에 저장됐다가 `+0x13d2` 에서 `R12D` 로 다시 실린다. 그 뒤 `+0x1a73` 까지 `R12` 를 쓰는 줄이 없다 `[정적]` (flow-writer 확인).
- (a) 길의 실패: `LdrpCorFixupImage` 가 음수를 돌려주면 `+0x630 JMP` → 실패 출구 `+0xa7b`.
- **import 표는 판정에 안 쓴다.**
- 덤: ntdll 에서 디렉터리 14 를 직접 묻는 곳은 넷이다 — exe 판정 `+0x662`, `LdrpCorFixupImage +0x42`, `LdrpProcessMachineMismatch +0x27`, `LdrpRelocateImage +0x12b` (결과서 `[정적: 바이트 확인]`).

#### 왜 (a) 는 디렉터리 14 를 안 보고 바로 믿나 — 단계별로

1. **근거** `[정적]`: (a) 길의 조건은 두 개뿐이다. "머리는 32비트 모양 (PE32)" + "프로세스는 64비트 (WoW64 아님)".
2. **근거** (결과서): AnyCPU (PE32) 가 `UseWOW64 = 0` 으로 들어온다는 건 **커널이 이 프로세스를 64비트로 띄웠다** 는 뜻이다.
3. **그래서** `[추측]`: ntdll 은 "32비트 모양 exe 가 64비트 프로세스로 왔다면 .NET AnyCPU 뿐" 이라고 **믿는 꼴** 로 짜여 있다. 코드가 이유를 적어 두진 않는다.
4. **그 믿음이 깨지면** `[정적]`: `LdrpCorFixupImage` 는 디렉터리 14 결과를 **NULL 검사 없이** 읽는다 (`+0x42/+0x47` → `+0x69`). CLR 헤더가 없는 PE32 exe 가 64비트로 오면 이 줄에서 **널 읽기** 다 (run 문서 ③).
5. **아직 확인 아님**: 커널이 그런 exe 를 64비트로 안 띄운다는 것은 `[추측]` 이다 → **Q166 ①** (커널 `PspSelectMachineForProcess`).

**새 말 — AnyCPU**

- 32비트 · 64비트 어느 쪽으로도 돌 수 있게 만든 .NET exe 다.
- 겉은 PE32 (32비트 머리, Machine `0x14c`) 지만, IL 만 들었고 `32BITREQUIRED` 가 꺼져 있다 `[정적: PE 표]` (덤프 예 `ScriptRunner.exe`).
- 비유: 겉 라벨은 "작은 가게용" 인데 속은 외국어 지시서뿐이라, 큰 가게에서도 통역만 붙이면 되는 **프리사이즈 세트**.

#### 판정 뒤 — 명부 줄에 도장 둘 `[정적]`

- `+0x13d7 CMP UseCOR` → `+0x13e0 BTS EAX,0x16` · `+0x13e4` 저장 = exe 명부 줄 `Flags` 비트 22 **`CorImage`**.
- `+0x13e7 TEST R12B` → `+0x13ec BTS EAX,0x18` · `+0x13f0` 저장 = 비트 24 **`CorILOnly`**.
- DLL 쪽도 같은 비트를 켠다 — `LdrpCompleteMapModule +0x263 ~ +0x26b` · `+0x279 ~ +0x27d` (2.13 짐 지도).

---

### 2.5 AnyCPU 첫 장 고쳐 쓰기 — `LdrpCorFixupImage`

`LdrpCorFixupImage` (rva `0x109374`, 388B). 모두 `[정적]`.

#### 고치는 길 — 조건 셋이 다 맞을 때

- 조건:
  - `Magic == 0x10b` (`+0x4b ~ +0x54`)
  - `Machine == 0x14c` (`+0x5a ~ +0x63`)
  - CLR `Flags` 비트 1 (32BITREQUIRED) **꺼짐** (`+0x69 ~ +0x6d`)
- 차례:
  1. 머리가 든 첫 쪽을 쓰기 가능으로 — `+0xcc / +0xd1 NtProtectVirtualMemory(-1, 첫 쪽, PAGE_READWRITE)`
  2. 선택 헤더를 `0x10` 바이트 뒤로 밀기 — `+0xe7 / +0xec memcpy`, `+0xf3` `SizeOfOptionalHeader += 0x10`
  3. 몇 칸을 8바이트로 넓히기 — `+0x106 ~ +0x12d` (결과서: `ImageBase` · 스택/힙 크기)
  4. **`+0x135 Magic = 0x20b`**
  5. 보호 되돌림 — `+0x144 / +0x149 NtProtectVirtualMemory`
- 선택 헤더가 쪽 안에 안 들어가면 `+0xad JA` → `0xc000007b` (결과서 이름 `STATUS_INVALID_IMAGE_FORMAT`).
- 그래서 → **메모리에 놓인 머리를 PE32 → PE32+ 로 바꾼다.**
- 복사 함수는 같은 주소 이름 2개 (`memcpy` · `memmove`) 다 — run88 과 같다.
- 비유: 가게 땅에 놓인 설계도 첫 장의 덮개를 열고, 칸을 넓혀 **큰 가게 양식으로 다시 적고**, 덮개를 닫는다.

#### 고치지 않는 길 (`+0x15b`) — 공용 게시판의 "받는 기계 범위" 칸

- 조건이 하나라도 안 맞으면 exe Machine 을 KUSD 두 칸 범위로 본다 `[정적]`.
  - `+0x15f CMP [0x7ffe002c]` (`ImageNumberLow`) · `+0x167 JNC`
  - `+0x177 CMP [0x7ffe002e]` (`ImageNumberHigh`) · `+0x17f JA`
  - 범위 밖이면 `0xc000007b`, 안이면 0.
- 이 VM 의 값은 **`0x8664` · `0x8664`** 다. run44 사진 8장 모두 같다 `[관찰]` (결과서 "동적 3" 이 이걸로 닫혔다).
- 그래서 → 이 길은 **Machine 이 `0x8664` 일 때만 통과** 한다.
  - 32BITREQUIRED 가 선 PE32 (`0x14c`) 가 64비트 프로세스로 오면 `0xc000007b` 로 끝난다 `[정적 조건 + 관찰 값]`.
  - 그런 exe 를 커널이 WoW64 로 보내는지는 커널 쪽이다 → Q166 ①.
- 비유: 공용 게시판에 "이 동네가 받는 기계 종류: x64 부터 x64 까지" 라는 칸이 있다. 범위가 **딱 한 종류** 다.

- 이 함수를 부르는 곳은 둘이다.
  - `LdrpInitializeProcess +0x627 / +0x62c` (exe)
  - `LdrpCompleteMapModule +0x2a6 / +0x2ab` (DLL — `LdrpProcessMachineMismatch` 가 켠 비트 `0x200000` 일 때, run38)

---

### 2.6 ★ mscoree 는 ntdll 이 직접 들인다 — `LdrpCorInitialize`

#### 답부터

- `UseCOR` 면 ntdll 이 **`MSCOREE.DLL` 을 직접 올린다** `[정적]`. exe 의 import 로 올리는 게 아니다.
- 그 말이 맞을 수밖에 없는 증거가 하나 더 있다 `[정적: PE 표]`.
  - x64 IL-only exe (`tzsync.exe`) 는 **import 표가 아예 없다** — 디렉터리 1 = `(0, 0)`.
  - import 로 올린다면 이 exe 는 mscoree 를 부를 길이 없다.

#### 안의 차례 `[정적]` (`LdrpCorInitialize`, rva `0x8a808`)

라벨은 ret 자리다.

```
LdrpCorInitialize
 ├─ +0x73   RtlEnterCriticalSection(FastPebLock)
 ├─ +0x98   RtlQueryEnvironmentVariable  COMPLUS_InstallRoot   (있나만 묻기)
 ├─ +0xc5   RtlQueryEnvironmentVariable  COMPLUS_Version       (위가 있을 때만)
 ├─ +0xda   RtlLeaveCriticalSection
 ├─ +0x10b  LdrpBuildSystem32FileName(…, MSCOREE.DLL)   둘 다 있으면 건너뛰고 이름만
 ├─ +0x16f  LdrpLoadDll(이름, …, 1, &명부 줄)            실패면 그 상태로 반환
 ├─ +0x17e  LdrpReleaseDllPath
 ├─ +0x19f  LdrpGetProcedureAddress(mscoree, "_CorExeMain")
 │   +0x1bb LdrpCorExeMainRoutine = ROR(Cookie ^ 주소, Cookie & 0x3f)
 │   +0x1c2 명부 줄을 부른 쪽 칸 ([RSP+0x178]) 에
 └─ +0x1cd  LdrpDereferenceModule      (이름을 못 찾았으면 대신 +0x1dc LdrpDecrementModuleLoadCountEx)
```

#### "있나만 묻는" 요령 `[정적]`

- `RtlQueryEnvironmentVariable(NULL, 이름, 길이, NULL, 0, …)` — 값을 담을 버퍼 크기를 **0** 으로 넘긴다.
- 변수가 있으면 "버퍼가 작다" (`0xc0000023`) 가 돌아온다 (`+0x98 ~ +0x9f`). 그 대답으로 "있다" 를 잰다.
- 이 동안 `FastPebLock` 을 잡고 있다 (`+0x6e` 잡기 · `+0xd5` 놓기). 이 자물쇠는 앞쪽 묶음 6 에서 만들어 PEB `+0x38` 에 매단 것이다 (run18 해설 2.4).
- 비유: **빈손** 을 내밀며 "그거 있어요?" 하고 묻는다. "손이 작아서 못 드려요" 라는 대답이 오면 → 있다는 뜻.

#### 이름은 보통 System32 전체 경로 `[정적]`

- 처음 `+0x49` 에서 `BL = 1`.
- 두 환경변수가 **둘 다** 있으면 `+0xca CMOVZ` 로 `BL = 0`.
- `+0xf2 TEST BL` · `+0xf4 JZ +0x1de` — `BL` 이 0 이면 `LdrpBuildSystem32FileName` 을 건너뛰고 이름 `MSCOREE.DLL` 그대로 (`+0x1de LEA RCX,[LdrpMscoreeDllName]`).
- 그래서 → **보통은 System32 전체 경로**, 환경변수 둘 다 있을 때만 이름만이다.
- 왜 그렇게 짰는지는 코드가 말하지 않는다. 이름만 넘겼을 때 어디서 찾게 되는지도 이번 근거엔 없다.

#### `_CorExeMain` 주소를 섞어 둔다 `[정적]`

- `+0x1a5` KUSD `Cookie` (`+0x330`) → `+0x1b3 XOR` → `+0x1b8 ROR (Cookie & 0x3f)` → **`+0x1bb`** 전역 `LdrpCorExeMainRoutine` (rva `0x1d25f0`, 8B) 에 저장.
- 이 전역을 **쓰는 곳은 이 하나**, 읽는 곳은 둘이다 (`_refs/LdrpCorExeMainRoutine.tsv`).
  - `LdrpInitializeProcess +0x189d` (2.7)
  - `LdrpInitializeThread +0x448` (2.7 끝)
- 열쇠는 **KUSD `Cookie`** 다. 프로세스 쿠키 (`RtlEncodePointer` 의 열쇠, run59 해설 2.3) 와는 **다른 열쇠** 다 (run 문서).
  - 섞는 모양 (XOR 뒤 ROR) 은 run59 해설 2.7 의 식과 같다. 열쇠만 다르다.
- 비유: 창구 번호를 **로비 공용 게시판의 열쇠** 로 암호화해 칠판에 적어 둔다. 가게 열쇠 (프로세스 쿠키) 가 아니다.

#### 실패하면

- 돌려주는 값 = `LdrpLoadDll` 이나 `LdrpGetProcedureAddress` 가 낸 상태다.
- 부른 쪽 `+0x1856 ~ +0x1858` 에서 음수면 **프로세스 초기화가 실패** 한다 (결과서).
- 그래서 → mscoree 파일이 없는 시스템이라면 .NET exe 는 **여기서** 끝난다. (지금 덤프엔 mscoree 가 **있다** — 2.11.)

#### 덤 — .NET exe 는 kernel32 가 더 일찍 올라올 것 `[추측]`

1. **근거** `[정적: PE 표]`: mscoree 의 정적 import 는 `KERNEL32.dll` **하나** 다 (+ 지연 import 6개).
2. **근거** `[정적]` (run20 ~ run26): `LdrpLoadDll` 은 의존 DLL 도 같이 올린다.
3. **그래서** → `+0x184d` (mscoree) 때 kernel32 · kernelbase 가 같이 올라오고, `+0x193c` 그래프에서 kernelbase → kernel32 → mscoree 차례로 켜질 것이다.
   - test.exe 길에선 kernel32 가 `+0x19a2` (flow.md 2.5) 에서 처음이다. 그보다 **앞** 이 된다.
4. **아직 확인 아님** → **Q167 7**.

---

### 2.7 ★ 진입점 바꾸기 — 사물함 메모의 한 칸을 고쳐 쓴다

#### run19 짐작이 `[정적]` 으로

- run19 해설 2.2 는 이렇게 적었다: ".NET exe 는 메모의 '처음 손에 쥘 것' 칸을 진입점 대신 `LdrpCorExeMainRoutine` 으로 바꿔 쓰는 것으로 보인다 `[추측]` (이 칸 뒤의 일은 안 봤다)."
- 이번에 **앞뒤를 다 이었다**. 모두 `[정적]`.

1. 커널이 첫 스레드 업무 시작 메모에 `Rcx` = exe 진입점을 넣는다 (`NtCreateUserProcess +0xd09` 의 `TransferAddress`, sast/11 · 69 — 결과서).
   - 이 메모가 사물함에 맡겨져 `LdrpInitializeProcess` 까지 그대로 온다 (run16 · run17).
   - 결과서 버전 확인: `_LdrpInitialize +0x167 / +0x16c` 가 `LdrpInitializeProcess` 를 부르기 전에 `RCX = R12 = 받은 CONTEXT` 다.
2. `LdrpCorInitialize +0x1bb` 가 `_CorExeMain` 주소를 섞어 `LdrpCorExeMainRoutine` 에 둔다 (2.6).
3. `+0x1878 ~ +0x188a`: TEB `SameTebFlags & 0x400` (`InitialThread`) — **첫 스레드인가** 를 본다.
4. `+0x188c` KUSD `Cookie` 읽기 → `+0x189d` `LdrpCorExeMainRoutine` 읽기 → `+0x18a4 ROR` · `+0x18a9 XOR` 로 **풀기**.
5. `+0x18ac RBX ← [RSP+0xe0]` (받은 CONTEXT) → **`+0x18b4 MOV [RBX+0x80],RAX`** — `Rcx` 칸을 `_CorExeMain` 으로.
6. 은퇴 (`NtContinue`) 뒤 `RtlUserThreadStart(rcx)` 가 그 값을 `BaseThreadInitThunk` 를 거쳐 부른다 (flow.md 2.10).
7. **그래서** → **exe `AddressOfEntryPoint` 는 쓰이지 않는다.** 커널이 넣은 진입점은 그대로 **묻힌다** (flow.md 2.4 새 절: 2.1 "진입점 값이 흘러가는 길" 의 값).

- **혼합 모드도 똑같이 덮인다.** 이 줄은 IL-only 를 안 보고 `UseCOR` 만 본다 `[정적]`.
- IL-only 면 명부 줄 `EntryPoint` (`+0x38`) 도 **안 적는다** `[정적]`.
  - `LdrpProcessMappedModule +0x54 AND ECX,0x1000004` · `+0x5a CMP ECX,4` · `+0x5d JNZ` — `ImageDll` 만 켜지고 `CorILOnly` 가 꺼졌을 때만 적는다.
- `AddressOfEntryPoint` 값이 **0 이어도** 로더는 막지 않는다 `[정적]`.
  - `+0x6c JZ` → `+0x235` → `+0x78` 에서 0 을 적고 지나간다.
  - 실제로 덤프의 x64 IL-only exe 셋은 AEP 가 0 이다 `[정적: PE 표]`.

비유: 오리엔테이션실이 사물함을 열어, 메모의 "처음 손에 쥘 것: 가게 입구 번지" 를 지우고 "**소개 창구 번호**" 로 고쳐 적는다. 직원은 그 사실을 모른 채 메모대로 출근한다.

#### 같은 꼴이 한 곳 더 — `LdrpInitializeThread`

- `+0x40 CMP UseCOR` · `+0x47 JNZ +0x421` → `+0x426 TEST [RBX+0x17ee],0x400` → `+0x448` 풀기 → `+0x458 MOV [R9+0x80],RAX` `[정적]`.
- 이건 **나중 스레드 길** 함수인데 `InitialThread` 를 다시 본다. 첫 스레드가 이 함수로 오는 때가 있어야 뜻이 있다.
- **언제인지 정적으로 못 정했다** → Q166 ⑤ · Q167 4.

---

### 2.8 ★ IL-only 면 import 를 안 걷는다

#### 갈림 `[정적]`

```
+0x1a73  TEST R12B,R12B     R12 = +0x13d2 에서 다시 실은 IL-only 표시
+0x1a76  JZ +0x1a89         IL-only 아님 → import 걷기 (test.exe · 혼합 모드)
(IL-only) +0x1a7f CALL LdrpCorProcessImports (ret +0x1a84) → +0x1a87 JMP +0x1af2
```

#### 건너뛰는 것 `[정적]`

`+0x1a87 JMP +0x1af2` 한 번으로 아래 다섯을 넘는다.

| 자리 | 함수 | 앞 바퀴에서 본 일 |
|---|---|---|
| `+0x1a89` | `LdrpThreadTokenSetMainThreadToken` | (이번 근거에 없음) |
| `+0x1aa4` | `LdrpMapAndSnapDependency` | exe import 마다 배달 기사에게 맡김 (run22) |
| `+0x1aac` | `LdrpDrainWorkQueue` | 일감 처리 · 일꾼 기다림 (run24) |
| `+0x1abd` | `LdrpThreadTokenUnsetMainThreadToken` | (이번 근거에 없음) |
| `+0x1aed` | `LdrpPrepareModuleForExecution` | 실행 준비 (run21) |

- `+0x1af2` 는 `LdrpPrepareModuleForExecution` 호출 (`+0x1aed`) 의 **ret 자리** 다. 그래서 JMP 한 번이 import 걷기 ~ 실행 준비를 **통째로** 넘는다.
- 결과서는 이 중 `LdrpPrepareModuleForExecution` 만 적었고, 나머지 넷은 flow-writer 가 보탰다 (run 문서 ②).

#### `LdrpCorProcessImports` 안 — import 를 하나도 안 읽는다 `[정적]`

- rva `0xb7e0`, 97B (`+0x0 ~ +0x60`).
- 차례:
  1. `+0x14` 노드 `State` = 6
  2. `+0x1b / +0x20 AVrfDllLoadNotification`
  3. `+0x2e / +0x33 LdrpSendDllNotifications(명부 줄, 1)`
  4. `+0x41 / +0x46 LdrpLogDllState`
  5. `+0x4d` 노드 `State` = 7
- 비유: 건물 들이기 · 전선 꽂기는 안 한다. 연결 카드에 **진행 도장만 6 → 7** 로 찍고 "들어왔어요" 알림만 돌린다.
- DLL 쪽도 같은 함수를 쓴다 — `LdrpMapDllWithSectionHandle +0x2d0 / +0x2d5` (DLL 이 `CorILOnly` 일 때, run26 · sast/21).
- 왜 IL-only 면 import 를 안 걷는지 코드가 이유를 적진 않는다. 다만 x64 IL-only exe 는 **걸을 표가 아예 없고** (2.6), mscoree 는 **이미 따로 들였다** (2.6) — 이 둘은 `[정적]` 사실이다.

#### 결정 기록 정정 — 어디가 "안 걷는 자리" 인가

- plan.md 결정 기록 (2026-11-13 "Q152 정적 끝" 줄, 오케스트레이터) 에 "`LdrpCorProcessImports +0x1a7f`" 로 적혀 있다.
- 실제로는:
  - `+0x1a7f` = `LdrpInitializeProcess` 안의 **call 자리** (ret `+0x1a84`).
  - `LdrpCorProcessImports` 자체는 97B 라 그런 오프셋이 **없다**.
  - "import 를 안 걷는" 일은 그 함수 **안** 이 아니라 **바깥** 의 갈림 `+0x1a73` · `+0x1a76` 과 `+0x1a87 JMP` 가 한다.
- run 문서 ① 이 이렇게 고쳤고, plan.md 결정 기록 run89 줄에도 정정이 적혔다. 이 글은 run 문서를 따른다.

#### IL-only 가 빼는 것 — 세 종류 비교 (결과서 표)

| | x64 IL-only (PE32+) | AnyCPU IL-only (PE32) | 혼합 모드 (PE32+, ILONLY 꺼짐) |
|---|---|---|---|
| 판정 | 디렉터리 14 + `Flags & 1` | Magic `0x10b` + 비 WoW64 | 디렉터리 14 만 |
| `LdrpCorFixupImage` | 안 부름 | 부름 (머리를 PE32+ 로) | 안 부름 |
| `CorImage` / `CorILOnly` | 둘 다 | 둘 다 | `CorImage` 만 |
| 명부 줄 `EntryPoint` | 안 적음 | 안 적음 | 적음 |
| exe 재배치 | ntdll 이 안 함 | 〃 | 함 |
| TLS 디렉터리 | 있으면 `0xc000007b` | 〃 | 허용 |
| exe import 걷기 | **안 걸음** | 〃 | 걸음 |
| mscoree 들임 · `Rcx` 덮기 | 함 | 함 | 함 |

- **TLS 거부** `[정적]`: `LdrpCorValidateImage` (rva `0x10e4b4`, 37B) 가 `+0x11 / +0x16 RtlImageDirectoryEntryToData(…, 9)` 로 TLS 디렉터리를 본다. 있으면 `0xc000007b`. exe 는 IL-only 일 때만 부른다 (`+0x185e / +0x1861` → `+0x1867 / +0x186c`).
- **재배치 건너뜀** `[정적]`: `+0x141f TEST R12B` · `+0x1422 JNZ` → `+0x1430 / +0x1435 LdrpProtectAndRelocateImage` 를 건너뛴다.
  - 누가 대신 고치나는 결과서가 `[추측: CLR]`. 커널이 이미 고친 상태인지는 → Q166 ⑦.
- **LoadConfig 건너뜀** `[정적]`: WoW64 이거나, `UseCOR` 이고 (a) PE32 길로 왔으면 `+0x6ef LdrImageDirectoryEntryToLoadConfigEx` 를 안 부른다 (`+0x6d5` · `+0x6de` · `+0x6e7 TEST EBX` — `EBX` = `+0x620` 의 1).
- **혼합 모드** 는 `+0x1a73` 에서 test.exe 와 **같은 길** (import 걷기) 로 간다. 다만 그 앞에 mscoree 가 이미 올라와 있고, `Rcx` 도 이미 덮여 있다.

---

### 2.9 은퇴 뒤 — 소개 창구가 통역사 사무실을 찾는다 (mscoree)

이 절은 진입점 **뒤** 다. 흐름도 지도 밖이지만, PE_emulator 가 알아야 해서 결과서가 줄까지 봤다.

- mscoree 는 이번에 **처음 analyze** 했다 — pdb `C5C034E4B3EDB32A23F05CE2A83C18431`, 함수 1334, 지도 `_CorExeMain` 뿌리 314 노드.
- 함수 시작 주소는 PE 내보내기 표와 맞춰 봤다 (`_CorExeMain` `0x7850` · `LoadLibraryShim` `0x23ee0` · `CLRCreateInstance` `0x13d0` · `_CorExeMain2` `0x26d40` 모두 같음).

**새 말 — shim (심)**

- 진짜 일을 하는 쪽 **앞에 끼워 둔 얇은 중개 층**. 여기선 mscoree 가 "어느 버전 런타임을 부를지" 골라서 **넘겨주기만** 한다 `[정적: 코드가 하는 일]`.
- 함수 이름 `ShellShim__CorExeMain` · `InitShimImpl` 에 "Shim" 이 들어 있다.
- 주의: 앞 바퀴들에 나온 "shim" (run19 해설 2.10 의 `LdrpGetShimEngineInterface` 쪽, run87 의 PEB `pShimData`) 과는 **다른 코드** 다. 같은 낱말을 다른 데 쓴다.
- 비유: 통역사 **소개 창구**. 창구 직원은 통역을 못 한다. 맞는 사무실을 찾아 **연결만** 해 준다.

#### 줄기 `[정적]` (라벨은 ret, `⇒` = 꼬리 점프)

```
RtlUserThreadStart(rcx = _CorExeMain) → BaseThreadInitThunk
[mscoree] _CorExeMain (rva 0x7850, 58B — 첫 몸)
 ├─ +0xb   ShellShim__CorExeMain (0x7894)
 │     ├─ +0xa8  InitShimImpl (0x27b0, 808B)            상태 0 일 때
 │     │     ├─ +0x36  FindLatestVersion                버전 고르기
 │     │     ├─ +0x74  GetShimDllPathName("mscoreei.dll")   경로 짜기
 │     │     ├─ +0xa2  FindFirstFileW                   있나? (없으면 → +0x31a UpdateShimImplStatus(3))
 │     │     ├─ +0xec  LoadLibraryShim → +0x36 LoadLibraryWrapper → +0x1d LoadLibraryExW(…, 8)
 │     │     └─ +0x113 · +0x12c · +0x145 · +0x167  GetProcAddress ×4 → 콜백 등록 → 상태 2
 │     ├─ 상태 2: ⇒ +0x9e / +0x88  mscoreei!_CorExeMain_RetAddr / _CorExeMain
 │     └─ 상태 3: ⇒ +0xb2  _CorExeMain 둘째 몸 (0x2f6e8)
 │                     ├─ +0x1d GetInstallation → (성공) +0x36 GetProcAddress ⇒ +0x45
 │                     └─ (실패) +0x5c ExitProcess(HRESULT)
 └─ (돌아오면) 상태 0 이면 +0x1c InitShimImpl, 상태 1 이면 +0x32 DisplayMessageBoxForNoShimImpl
```

- `⇒` 는 **꼬리 점프** 다. 그래서 상태 2 로 넘어가면 콜스택에 mscoree 줄이 **안 남는다** (run17 해설 2.4). 상태 2 의 점프는 CFG 검사 (교환원, run28 해설 2.9) 를 거친다.

#### 상태 값 `g_shimImplStatus` — 창구의 진행 표시 `[정적]`

| 값 | 뜻 (코드에서 읽은 것) | 그다음 |
|---|---|---|
| 0 | 아직 안 함 | `InitShimImpl` 부름 |
| 1 | 파일은 있는데 올리기 · 내보내기 찾기 실패 | `0x80131701` 반환 → 첫 몸이 메시지 상자 (`+0x2d`). 코드 이름은 `CLR_E_SHIM_RUNTIMEEXPORT` 로 보임 `[추측: 이름]` |
| 2 | `mscoreei` 올림 | `mscoreei!_CorExeMain_RetAddr` (없으면 `_CorExeMain`) 로 꼬리 점프 |
| 3 | 버전이나 파일을 못 찾음 | `_CorExeMain` 둘째 몸 (옛 길) 으로 꼬리 점프 |

#### 무엇을 보고 고르나 `[정적]`

**환경변수** — `EnvGetString` = `GetEnvironmentVariableW("COMPlus_" + 이름)`. PEB 의 환경 블록만 보고 syscall 은 없다 (결과서).

| 이름 | 어디서 (call) | 뜻 |
|---|---|---|
| `COMPlus_Version` · `COMPlus_InstallRoot` | `FindLatestVersion +0x24` · `+0x3e` | **둘 다** 있고 그 경로에 `mscoreei.dll` 이 있으면 레지스트리는 안 봄 (`+0x163 CheckShimImplCallback`) |
| `COMPlus_InstallRoot` | `GetShimDllPathName +0xc0` | 설치 폴더. 없거나 빈 값이면 레지스트리 |
| `COMPlus_DefaultVersion` | `FindLatestVersionWithCallback +0x719` | 레지스트리로 못 골랐을 때의 기본 버전 |

- 글자 대소문자는 근거 그대로 옮겼다. ntdll 쪽 (2.6) 은 `COMPLUS_…`, mscoree 쪽은 `COMPlus_…` 로 적혀 있다.

**레지스트리** (`HKLM`)

| 키 / 값 | 어디서 (call) | 하는 일 |
|---|---|---|
| `Software\Microsoft\.NETFramework\Policy\` 의 하위 키 | `FindLatestVersionWithCallback +0xdd` (`RegOpenKeyExW`) · `+0x155` (`RegQueryInfoKeyW`) · `+0x1e7` (`RegEnumKeyExW`) | 하위 키를 거꾸로 돌며 후보 버전 고르기 |
| `…\Policy\<하위 키>` 의 값 이름들 | `+0x3cf` (`RegOpenKeyExW`) · `+0x3fd FindLatestBuild` | 값 이름을 숫자로 바꿔 **가장 큰 빌드** |
| `Software\Microsoft\.NETFramework` 의 값 `InstallRoot` | `GetShimDllPathName +0x13f` (열기) · `+0x176` (값) · `+0x1e0` (닫기) | 설치 폴더 (문자열 형만) |

- 레지스트리 함수는 모두 **지연 import** (ADVAPI32) 다. 처음 부를 때 `ADVAPI32.dll` 이 올라온다 (결과서).

**파일 · 경로**

- `mscoree.dll.local` 이 있으면 mscoree 가 있는 폴더를 런타임 폴더로 쓴다 — `GetModuleFileNameW` + `GetFileAttributesW` (`GetShimDllPathName +0x4be` · `+0x4f2` · `+0x537` …).
  - 비유로만 잇자면, run88 해설의 `.local` ("우리 가게 선반 먼저" 표지) 과 비슷한 그림이다.
- 후보 경로 = `<InstallRoot>` + `<버전>` + `\mscoreei.dll`.
  - 이름 글자는 `InitShimImpl +0x68 LEA` → `0x18004e988` = `mscoreei.dll` (flow-writer 가 파일 바이트로 다시 읽음).
  - 짠 경로는 `GetFullPathNameW` 로 정규화한 것과 같아야 한다 (`_wcsicmp`).
- 있나: `FindFirstFileW` / `FindClose` (`InitShimImpl +0x9b` · `+0xcc`).
- 올리기: `LoadLibraryShim` (rva `0x2740`) → `+0x31 LoadLibraryWrapper` → `+0x16 LoadLibraryExW(경로, NULL, 8)`.
  - 플래그 8 은 `LOAD_WITH_ALTERED_SEARCH_PATH` 로 보인다 `[추측: 이름]`.
  - `LoadLibraryExW` 는 kernelbase `+0xf8/+0xff LdrLoadDll` 로 간다 `[정적]` — 평소 DLL 올리는 길 (run20 ~ run30).

**API → NT 함수 짝**

- `LoadLibraryExW → LdrLoadDll` 만 `[정적]` 이다.
- `RegOpenKeyExW → NtOpenKey(Ex)` 같은 나머지 짝은 kernelbase 본문을 안 봐서 `[추측]` → Q166 ②.

#### 지금 덤프에선 — 상태 3 → 옛 길 → 끝

1. 덤프엔 `mscoreei.dll` · `Microsoft.NET` 폴더가 없다.
2. 그래서 `InitShimImpl` 의 실패 갈래 (`+0x52 JS` · `+0x76 JS` · `+0xab JZ`) 가 모두 `+0x309` 로 모인다 → `+0x315 / +0x31a UpdateShimImplStatus(3, "mscoreei.dll")` `[정적]`.
3. `ShellShim__CorExeMain +0x37 CMP EAX,3` · `+0x3a JZ` → `+0xb2` 꼬리 점프 → `_CorExeMain` **둘째 몸** (rva `0x2f6e8`, 91B) `[정적]`.
4. 둘째 몸: `+0x18 / +0x1d GetInstallation(1, &모듈, 0)` — 옛 런타임 고르기 길 (CLR 2.0 시절, 결과서). 음수 (`+0x21 JS`) 면 `+0x55 / +0x5c ExitProcess(그 HRESULT)` `[정적]`.
5. 지금 덤프가 실제로 `ExitProcess` 까지 간다는 건 `[추측: GetInstallation 이 실패한다고 칠 때]` 다.
6. **어떤 HRESULT 로 끝나는지, 메시지 상자가 뜨는지는 `[공백]`** → Q166 ③ · Q167 6.
   - 결과서: 메시지 상자는 `NoGuiFromShim()` 이 0 일 때만 뜬다. 그 설정 이름은 Ghidra 가 틀리게 그려 `[공백]`. 뜨려면 USER32 지연 로드 + `MessageBoxW` 가 필요하다.

**새 말 — HRESULT** (짧게)

- COM 쪽에서 쓰는 32비트 결과 코드 꼴로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이 코드에서 본 건 "**음수면 실패**" 로 가른다는 것뿐이다 (`+0x21 JS`) `[정적]`.

비유: 소개 창구가 동네를 다 뒤져도 통역사 사무실이 없다. "못 찾음" 도장을 찍고, **옛날 안내 책자** 로 한 번 더 찾아본다. 그래도 없으면 가게 문을 닫는다.

#### `clr.dll` 시작까지 — 이름만 (질문 3)

- `[정적]` mscoree 가 mscoreei 에서 꺼내 쓰는 이름: `RegisterShimImplCallback` · `RegisterShimImplCleanupCallback` · `SetShellShimInstance` · `OnShimDllMainCalled` · `_CorExeMain_RetAddr` · `_CorExeMain` · `LoadLibraryShim` · `LoadLibraryShim_RetAddr`.
- `[정적]` mscoree 가 밖에 내보내는 `LoadLibraryShim` (rva `0x23ee0`) 도 같은 `GetShimDllPathName` 으로 `<InstallRoot>\<버전>\<파일이름>` 을 짠다.
  - 그래서 → `clr.dll` 도 같은 규칙의 경로로 올릴 것이다 `[추측: 호출자는 mscoreei 쪽 코드]`.
- `[추측]` mscoreei 안에서: exe CLR 헤더의 요구 버전 · `exe.config` 를 읽고 → `clr.dll` 을 올리고 → 관리 코드 시작.
- `[추측]` 그 뒤 필요할 만한 것: `clrjit.dll` · `mscorlib.dll` (+ NGen 이미지) · `clrcompression.dll` · `Config\machine.config`.
- 덤프에 파일이 없어서 근거는 여기까지다.

---

### 2.10 Framework 와 앱호스트를 머리로 가르기 (질문 4)

**새 말 — 앱호스트 (apphost)**

- .NET Core / .NET 5+ 앱을 켜는 **작은 네이티브 exe** 로 알려져 있다. 실행 때 `hostfxr.dll` 을 찾아 올린다 `[추측: 일반 지식]` (덤프에 샘플이 없음).
- 로더 입장에선 **그냥 네이티브 exe** 다. 디렉터리 14 가 없으니 `UseCOR = 0` 이고, 이번 갈래와 상관없다.
- 비유: "통역 필요" 딱지 없이 들어온 평범한 직원이, **출근한 뒤에** 스스로 통역사를 부르는 꼴. 관리실은 따로 하는 게 없다.

#### 가르는 차례 (이 저장소 코드 기준) `[정적]`

1. Magic `0x10b` (PE32) **이고** WoW64 가 아님 → **Framework AnyCPU** (`UseCOR`, `LdrpCorFixupImage`)
2. 디렉터리 14 가 있음 → **Framework x64**. ILONLY 비트로 IL-only / 혼합 모드
3. 둘 다 아님 → **네이티브** (앱호스트 포함)

#### 덤프에 있는 모양 — 관리 코드 exe 11개

| 종류 | 머리 모양 (대표) | 덤프 예 |
|---|---|---|
| x64 IL-only | PE32+, Machine `0x8664`, 디렉터리 14 (크기 72), `Flags = 0x9`, **AEP 0 · import 디렉터리 비어 있음** | `tzsync.exe`, `AppVStreamingUX.exe`, `powershell_ise.exe` (x64) |
| AnyCPU IL-only | PE32, `0x14c`, 디렉터리 14, `Flags = 0x1` 또는 `0x9` (32BITREQUIRED 꺼짐), import = `mscoree.dll!_CorExeMain` 하나, 진입점 = 그 import 로 뛰는 `FF 25` 껍데기 | `ScriptRunner.exe` (AEP `0x47fa`, `Flags 0x9`), `UevAppMonitor.exe`, `Microsoft.Uev.SyncController.exe` |
| x86 전용 IL-only | PE32, `0x14c`, `Flags` 에 32BITREQUIRED (`0x3` · `0xb`) | `UevTemplate*Generator.exe` 셋, `powershell_ise.exe` (x86) |
| 혼합 모드 | PE32+, 디렉터리 14, `Flags = 0x8` (ILONLY 꺼짐), 평범한 import + `mscoree.dll!_CorExeMain` · `CorBindToRuntimeEx` | `FileHistory.exe` (AEP `0x80f6`, import 6개 DLL) |
| 앱호스트 | 네이티브 PE32+, 디렉터리 14 없음, mscoree import 없음 | 덤프에 없음 `[추측: 일반 지식]` |

- 머리 값은 `[정적: PE 표]` 다 (결과서 + flow-writer 가 `pefile` 로 `tzsync.exe` · `ScriptRunner.exe` · `FileHistory.exe` 를 다시 셈).
- `Flags` 의 `0x8` 비트 뜻은 이번 근거에 없다. 이 글은 비트 0 (ILONLY) · 비트 1 (32BITREQUIRED) 만 쓴다.

#### 주의 — "mscoree import 가 있나" 로 가르면 틀린다

- x64 IL-only exe 는 import 표가 **아예 없다** (`tzsync.exe` 디렉터리 1 = `(0, 0)`) `[정적: PE 표]`.
- 로더도 판정에 import 를 안 쓴다 (2.4).
- 덤으로 짐작 하나 `[추측]`: AnyCPU exe 의 `FF 25` 껍데기와 mscoree import 는, 64비트 프로세스로 뜰 때는 쓰이지 않는 것으로 보인다.
  - 이유: `Rcx` 를 덮으니 진입점 껍데기를 안 부르고 (2.7), IL-only 라 import 도 안 걷는다 (2.8). 두 `[정적]` 을 이은 짐작이다.
  - 32비트 (WoW64) 로 뜰 때는 이번 범위 밖이다.

---

### 2.11 PE_emulator 쪽 — 무엇이 필요하고, 할 만한가 (질문 5)

#### 0 ~ 10 단계 (run 문서 "한눈에" 를 줄임)

IL-only 꼴 .NET Framework exe (x64 IL-only, 또는 AnyCPU) 를 켠다고 칠 때 차례대로. "덤프" 열 = 지금 `ms/bin` 에 있나.

| # | 누가 | 하는 일 | 덤프 | 표시 |
|---|---|---|---|---|
| 0 | 커널 | 어느 기계로 띄울지 (`PspSelectMachineForProcess` → `PspQueryComPlusRunUnderWoW`) | 코드 | 호출 줄 `[정적]`, 하는 일 `[추측]` |
| 1 | ntdll `LdrpInitializeProcess` | 머리 보고 `UseCOR` · IL-only (2.4) | 코드 | `[정적]` |
| 2 | ntdll `LdrpCorFixupImage` | (AnyCPU 만) 머리 PE32 → PE32+ (2.5) | 코드 | `[정적]` |
| 3 | ntdll `LdrpCorInitialize` | `MSCOREE.DLL` 직접 올림 + `_CorExeMain` 섞어 둠 (2.6) | **있음** | `[정적]` |
| 4 | ntdll `LdrpCorValidateImage` | (IL-only 만) TLS 있으면 거부 (2.8) | 코드 | `[정적]` |
| 5 | ntdll | (첫 스레드면) 맡긴 문맥 `Rcx` ← `_CorExeMain` (2.7) | 코드 | `[정적]` |
| 6 | ntdll | mscoree 켜기 · IL-only 면 exe import 안 걷기 (2.8) | 있음 | `[정적]` (mscoree `DllMain` 본문은 `[추측]`) |
| 7 | 첫 스레드 | `NtContinue` → `RtlUserThreadStart(rcx = _CorExeMain)` → mscoree | — | `[정적]` |
| 8 | mscoree `InitShimImpl` | 런타임 폴더 · 버전 고르기 (2.9) | 레지스트리 값은 덤프 밖 | `[정적]` |
| 9 | mscoree | `mscoreei.dll` 찾아 `LoadLibraryExW` (2.9) | **없음** | `[정적]` |
| 10 | mscoreei → clr | 진짜 런타임 시작 | **없음** | `[추측]` |

- 덤프에 더해야 하는 건 **9 · 10** (+ 레지스트리 값) 이다. 0 ~ 8 은 지금 덤프로 된다.

#### 할 만한가 — 한 줄

**1단계 (ntdll CLR 갈래 + mscoree `InitShimImpl` 까지) 는 할 만하다. 2단계 (mscoreei → clr 로 진짜 관리 코드 시작) 는 덤프에 .NET Framework 폴더가 없어서 지금은 아니다.**

- 1단계가 작은 까닭 `[정적]`:
  - 엔진 (로더 흉내) 이 할 일은 `LdrpInitializeProcess` 안 갈래 자리들 — 판정 `+0x602` · `+0x662`, 도장 `+0x13e0` · `+0x13ec`, 재배치 건너뛰기 `+0x141f`, mscoree 올림 `+0x184d`, 검증 `+0x1867`, `Rcx` 덮기 `+0x18b4`, 그래프 `+0x193c`, import 갈림 `+0x1a73` — 와 작은 함수 넷 (`LdrpCorFixupImage` 388B · `LdrpCorValidateImage` 37B · `LdrpCorInitialize` · `LdrpCorProcessImports` 97B) 이다.
  - 은퇴 뒤 mscoree 는 **진짜 코드** 그대로 돈다. 덤프에 `System32\mscoree.dll` · `MUI\0409\mscorees.dll` 이 있다.
  - 엔진은 레지스트리 · 파일 묻기에 **대답만** 하면 된다 (syscall 쪽).
  - 1단계만 해도 .NET exe 가 **"런타임 못 찾음" 으로 정상적으로 끝나는 데까지** 간다 (2.9 끝).
- 2단계가 지금 아닌 까닭:
  - Framework 폴더를 통째로 더해야 한다.
  - 정확한 파일 목록은 VM 에서만 정해진다 (Q167 5).
  - CLR 안쪽이 부르는 syscall 양은 이 요청 범위 밖이라 비용을 못 쟀다.
  - 대상 샘플은 전체의 2% (310개) 다.

#### 덤프에 더할 것 (확실한 차례)

1. `Windows\Microsoft.NET\Framework64\v4.0.30319\mscoreei.dll` — 이름 · 경로 짜는 법은 `[정적]`, 폴더 글자는 `[추측]` (레지스트리 값이 정함)
2. 레지스트리 `HKLM\Software\Microsoft\.NETFramework` 값 `InstallRoot`, `…\Policy\` 하위 키 · 값 (예: `v4.0` 키 아래 `30319`) — 키 · 값 **이름** 은 `[정적]`, **실제 값** 은 `[확인 필요]`
3. `…\v4.0.30319\clr.dll` — `[추측]`
4. `clrjit.dll` · `mscorlib.dll` (+ NGen 이미지) · `clrcompression.dll` · `Config\machine.config` — `[추측]`
5. 돌려 볼 exe 가 쓰는 어셈블리 (`System.dll` 등) — `[추측]`

- 이미 있음: `System32\mscoree.dll`, `System32\MUI\0409\mscorees.dll`, `SysWOW64\mscoree.dll`, 시험용 관리 코드 exe 11개.
- 확정하려면 VM 에서 `ScriptRunner.exe` (AnyCPU) · `tzsync.exe` (x64 IL-only) 를 한 번 돌려 파일 열기 목록과 `InstallRoot` 값을 뽑아야 한다 → Q167 5.

#### PE_emulator 확인거리 (우리 대기열 아님)

- 엔진 KUSD 의 `ImageNumberLow` · `ImageNumberHigh` (`+0x2c` · `+0x2e`) 가 진짜와 같은 **`0x8664` · `0x8664`** 인가. 2.5 의 고치지 않는 길이 이 범위를 본다. 범위가 다르면 결과가 달라진다.
- `Rcx` 를 섞는 열쇠는 KUSD `Cookie` (`+0x330`) 다. 넣는 쪽 · 푸는 쪽이 모두 ntdll 안이라 값은 아무거나 되지만, **그 사이에 바뀌면 안 된다** `[정적]`.
- 엔진의 PE 읽는 쪽이 `AddressOfEntryPoint = 0` · import 디렉터리 `(0, 0)` 인 exe 를 거르지 않는지, 커널 흉내가 그런 exe 의 시작 `Rcx` 로 무엇을 넣는지 (진짜 커널 쪽은 Q166 ⑥).

---

### 2.12 test.exe 는 정말 안 탔나 — run44 사진 다시 읽기

새 실행은 없다. flow-writer 가 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_*.txt`) 에서 이 갈래의 칸만 다시 찾았다. 줄 번호는 run 문서 "사진 대조" 표에 있다.

| 칸 | 값 | 뜻 |
|---|---|---|
| ntdll `UseCOR` (rva `0x1d292c`, 1B) | **0** (P1a · P1b · P2) | 판정 갈래 안 탐 `[관찰]` |
| ntdll `LdrpCorExeMainRoutine` (rva `0x1d25f0`, 8B) | **0** (P1a · P1b · P2) | `LdrpCorInitialize +0x1bb` 안 탐 `[관찰]` |
| 명부 줄 6개 `Flags.CorImage` · `CorILOnly` | 모두 `0y0` | 도장 안 찍힘 `[관찰]` |
| KUSD `ImageNumberLow` · `ImageNumberHigh` | `0x8664` · `0x8664` (8장 같음) | 2.5 범위 검사 값 `[관찰]` |
| KUSD `Cookie` (`+0x330`) | `0xe8e5ccd6` | 섞기 열쇠 (값은 아무거나 됨) `[관찰]` |
| KUSD `ComPlusPackage` (`+0x2e0`) | `1` | 쓰는 쪽 모름. 이름이 맞닿는 커널 함수 `ExpReadComPlusPackage` 는 이름만 (Q166 ①) `[관찰]` |

- `[정적+샘플]` 로도 맞는다: sample 셋 (`test.exe` · `dll1.dll` · `dll2.dll`) 은 모두 PE32+ (Magic `0x20b`) · 디렉터리 14 없음 (flow-writer `pefile`) → 2.4 의 (a) · (b) 둘 다 아니오.
- 그래서 flow.md 2.4 의 "test.exe 면 `UseCOR` = 0 `[추측]`" 이 **`[관찰]`** 이 됐다.
- Q104 (test.exe 로 볼 것) 에 새로 붙일 건 없다. 이 갈래의 test.exe 쪽 값은 이미 사진으로 `[관찰]` 이다 (run 문서).

#### 한 줄 읽기 — 1바이트 `UseCOR` 를 8바이트 줄에서 찾기

`UseCOR` 는 1바이트라 `dq` 줄 (8바이트씩 숫자로 찍은 줄, [run44 해설](run44-baggage-snap-해설.md) 2.9) 에서 골라 읽었다. P1a 로그 5132번째 줄:

```
00007ffa`fb832920  00000000`00000000 00000000`00000002
```

- 줄 맨 앞 `…832920` 은 첫 숫자 묶음 (8바이트) 의 주소다. 둘째 묶음은 그다음 8바이트, 곧 `…832928 ~ …83292f` 다.
- 둘째 묶음 값은 `0x2` 다.
- x64 는 **낮은 바이트가 앞 주소** 에 온다 `[추측]` (일반적으로 알려진 설명, [run54 해설](run54-teb-fill-해설.md) 2.9). 그래서 숫자 **맨 오른쪽 두 자리** (`02`) 가 `…928` 바이트다.
  - `…928` = `LdrpHotPatchInitialized` = 2.
  - `…92c` = `UseCOR` = 오른쪽에서 다섯째 바이트 = `00`.
- run 문서 "사진 대조" 표가 이 읽기를 그대로 적었다: "낮은 바이트 `+0x928` 은 `LdrpHotPatchInitialized` 2, `+0x92c` 는 0".

---

### 2.13 짐 지도 — 모르던 칸 넷이 닫혔다

- 짐 지도 바퀴지만 **새 짐 칸은 없다**. test.exe 가 갈래를 안 타니 놓는 짐이 없다.
- 대신, 사진엔 끝까지 0 인데 **쓰는 자리를 몰라** "모르는 칸" 에 있던 넷이 쓰는 줄을 찾아 "아는 칸" 으로 옮겼다. 모두 정확, 사진 `끝까지 0`.

| 칸 | 짐 지도 절 | 쓰는 줄 | test.exe |
|---|---|---|---|
| 명부 줄 `Flags.CorImage` (비트 22) | 3.5 | exe `LdrpInitializeProcess +0x13e0` · `+0x13e4` / DLL `LdrpCompleteMapModule +0x267` · `+0x26b` | 조건부 안 탐 |
| 명부 줄 `Flags.CorILOnly` (비트 24) | 3.5 | exe `+0x13ec` · `+0x13f0` / DLL `+0x279` · `+0x27d` | 조건부 안 탐 |
| `.data` `UseCOR` | 3.8 | `+0x334` (0) · `+0x611` · `+0x66c` (1) | **0 을 씀** (`+0x334`) |
| `.data` `LdrpCorExeMainRoutine` | 3.8 | `LdrpCorInitialize +0x1bb` | 조건부 안 탐 |

- "0 을 씀 / 조건부 안 탐" 은 사진의 `끝까지 0` 을 **코드로 가른 것** 이다. `끝까지 0` 만으로는 "아무도 안 씀" 과 구별이 안 된다 (run44 해설 2.10).
- 숫자 (run 문서): 명부 줄 아는 칸 39 → 41 · 모름 26 → 24, `.data` ntdll 159 → 161 · 모름 277 → 275, 구조체 아는 칸 736 → 738 (정확 682 → 684) · 모름 303 → 301, 전역 아는 것 186 → 188 · 모름 307 → 305. **마흔다섯째판**.
- 이 갈래가 놓는 다른 짐 (mscoree 명부 줄 · 노드, 맡긴 문맥 `Rcx`, 메모리 속 PE32 머리 고치기) 은 .NET exe 만이라 3장 칸 수 **밖** 이다.
- 그 밖에 한 줄씩 보탬: 3.7 KUSD "ntdll 은 읽기만" 줄에 읽는 줄 (`ImageNumberLow/High` · `Cookie`), 7.2 KUSD 실마리 (`0x8664` · `ComPlusPackage` 1), 4장 진입점 짝 줄에 `Rcx` 덮기, 5장 ② 한 줄.

#### 묵은 숫자 바로잡기 — 7.1 전역 셈

- 7.1 의 "전역 끝까지 0 중 **모르는 칸 266**" 은 run60 뒤로 안 고쳐진 숫자였다.
- 그 사이 run62 (12) · run65 (6) · run66 (5) · run68 (3) · run69 (1) 이 끝까지 0 전역 **27개** 를 닫았는데, 266 에서 안 뺐다.
- 그래서 → run88 뒤 **239** 가 맞고 (2.1 의 "끝까지 0 239" · 7.3 의 `.data` 217 + `.mrdata` 22 와 같음), run89 뒤엔 **237** 이다 (이번에 둘 닫음).
- 같은 절 "아는 칸 중 끝까지 0 전역 **30**" 도 따라 고쳤다.
  - 이 숫자는 "사진 8장 모두 0 인 전역 296 − 그중 모르는 칸" 이다.
  - 그래서 run88 뒤 296 − 239 = **57**, run89 뒤 **59**.
- 짐 지도 장부 셈이라 run 문서 맨 위 정정 줄은 안 넣었다 (run88 의 구조체 셈 정정과 같은 꼴).

---

### 2.14 결과서와 다르게 적은 곳 · 보탠 것 (짧게)

| # | 무엇 | 한 줄 |
|---|---|---|
| ① | `+0x1a7f` 자리 표기 | 결정 기록 "`LdrpCorProcessImports +0x1a7f`" → `LdrpInitializeProcess` 안 call 자리. 안 걷는 자리는 `+0x1a73` 갈림 + `+0x1a87 JMP` (2.8) |
| ② | (보탬) 건너뛰는 것 | 결과서는 `LdrpPrepareModuleForExecution` 하나 → 넷 더 (2.8) |
| ③ | (보탬) `LdrpCorFixupImage` NULL 미검사 | 디렉터리 14 결과를 검사 없이 읽음 + PE32 길은 디렉터리 14 를 안 봄 → CLR 헤더 없는 PE32 가 64비트로 오면 널 읽기 `[정적]`, 커널이 막는지는 `[추측]` (2.4) |
| ④ | (보탬) KUSD `ImageNumberLow/High` | `0x8664` · `0x8664` `[관찰]` run44 — 결과서 "동적 3" 닫힘 (2.5) |
| ⑤ | `FindLatestVersionWithCallback` | 결과서 `+0x712 / +0x719` (call/ret 꼴) → `+0x712` 는 이름 글자 `LEA` (`DefaultVersion`), call 은 **`+0x719`** / ret `+0x71e` |
| ⑥ | (보탬) 사진 값 | `UseCOR` · `LdrpCorExeMainRoutine` · 도장 둘 끝까지 0, KUSD `ComPlusPackage` 1 · `Cookie` (2.12) |
| ⑦ | (보탬) PE 머리 다시 셈 | `tzsync.exe` · `ScriptRunner.exe` · `FileHistory.exe` · `mscoree.dll` 은 결과서와 같음. sample 셋은 결과서에 없던 것 (2.12) |
| ⑧ | (보탬, `[추측]`) | .NET exe 는 kernel32 · kernelbase 가 더 일찍 (2.6 끝, Q167 7) |
| ⑨ | (보탬) DLL 쪽 도장 줄 | 결과서 "`+0x263 ~ +0x279` 에서 켬" → `+0x263 ~ +0x26b` · `+0x279 ~ +0x27d` |
| ⑩ | 이름 | mscoree 의 `alias:…+1` 은 같은 함수의 C++ 꾸민 이름 하나라 이름 그대로. ntdll 복사는 같은 주소 이름 2개 `memcpy` · `memmove` |

- 디버거가 붙어서 달라지는 점: 이번에 읽은 줄 안에서 디버거 여부를 보는 줄은 없다 `[정적: 읽은 줄 안에서]`. `+0x188c` 의 KUSD `+0x330` 은 쿠키라 디버거와 상관없다 (run19 해설 2.13 에서 이미 정리).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 기준 — ntdll pdb 줄 | … run88 | + run89 몫 |
| 기준 — mscoree pdb | 없음 | **새 줄** `C5C034E4B3EDB32A23F05CE2A83C18431` (처음 analyze, 함수 1334, 지도 314 노드, export 네 번. 같은 이름 둘은 `.asm` 으로) |
| 기준 — 근거 로그 | — | run89 줄: run44 로그에서 다시 읽은 `UseCOR` · `LdrpCorExeMainRoutine` · 도장 둘 · KUSD 칸 |
| 흐름도 앞 글 | — | run89 문단: **그림을 안 고쳤다** (test.exe 는 갈래를 안 탐). 갈라지는 자리 `+0x602` · `+0x662` · `+0x184d` · `+0x18b4` · `+0x1a73` 는 2.4 새 절에 글로만 |
| 2.4 머리 | … run86 | + run89 |
| 2.4 `LdrpInitializeProcess` 나무 | `+0x5f2 UseWOW64` 줄 다음에 `UseCOR` 줄 없음 | 새 줄 `+0x611 / +0x66c UseCOR = 1 (…) test.exe 안 탐` |
| 〃 `LdrpInitializeTls` 줄 | "(.NET 이면 `+0x1852 LdrpCorInitialize`, `+0x1941` 그래프)" | "(.NET 이면 앞에 `+0x1852` · `+0x186c LdrpCorValidateImage` · `+0x18b4` 맡긴 `CONTEXT.Rcx` ← `_CorExeMain`, 뒤에 `+0x1941` 그래프)" |
| 〃 `LdrpMapAndSnapDependency` 줄 | — | 갈림 `+0x1a73` 한 마디 |
| 2.4 "뒤쪽에서 쓰는 앞쪽 값" | test.exe 면 `UseCOR` = 0, `R12B` = 0 일 것 `[추측]` | `UseCOR` = 0 · `R12B` = 0 은 `[정적+샘플]` + **`[관찰]`** run44 (`UseWOW64` 쪽은 그대로) |
| 2.4 새 절 | — | **"CLR 헤더 갈래 (.NET exe) — test.exe 는 안 탐"**: 갈래 그림, 판정 두 칸, mscoree 직접 올림, `Rcx` 덮기 (+ `LdrpInitializeThread`), IL-only 가 빼는 것, `LdrpCorFixupImage`, test.exe 안 탐 근거, kernel32 더 일찍 `[추측]` |
| 2.6 머리 · `+0x1aa4` 후보 줄 | … run88 | + ".NET IL-only 갈림 run89", 갈림 `+0x1a73` 한 마디 |
| 2.6 SxS 절 | `+0xb0d RtlpInitializeThreadActivationContextStack` | `LdrpInitializeProcess +0xb0d` (ret `+0xb12`) — run88 뒷정리 (2.4 나무와 맞춤) |
| 2.10 머리 · `RtlUserThreadStart` 절 | (해당 줄 없음) | + run89, "**.NET exe (`UseCOR`) 면 첫 스레드 `rcx` 는 `mscoree!_CorExeMain`**" 한 줄 |
| 4장 | — | run89 새 줄 셋: 동적 **Q167**, 정적 **Q166**, PE_emulator 쪽 · 결과서와 다르게 적은 곳 · 짐 지도 셈 정정 |
| 4.1 그림 2 `(9)` 줄 | (9) 는 IL-only 가 아닐 때 | + 갈림 `+0x1a73` · `+0x1a76`, IL-only 면 (9) ~ (11) 을 다 건너뜀 |
| 5장 | … run88 | + run89 (run 표 · 요청서 번호 표) |

태그 · 판단이 바뀐 곳:

- flow.md 2.4 "test.exe 면 `UseCOR` = 0 · `R12B` = 0" — `[추측]` → **`[정적+샘플]` + `[관찰]`** run44.
- run19 해설 2.2 "`+0x18b4` 는 .NET exe 의 '처음 손에 쥘 것' 칸을 바꿔 쓰는 것으로 보인다" — `[추측]` → **`[정적]`** (앞뒤 줄을 다 이음, 2.7).
- run18 해설 묶음 5 ".NET 검사" — 이름만이던 것이 **판정 두 길** 로 `[정적]` (2.4).

### 짐 지도 (`baggage.md`) 에서 바뀐 것

- **마흔다섯째판** — 새 짐 칸 없음, 모르던 칸 넷 닫음 (3.5 둘 · 3.8 둘), 7.1 셈 정정 (2.13).
- 3.7 · 4장 · 5장 · 7.2 · 7.3 · 8장에 한 줄씩.

### 큰 그림에서

- **test.exe 흐름도는 그대로다.** 이번 바퀴는 지도에 **갈림길 표지** 만 세웠다 — "여기서 .NET 이면 저쪽".
- 그래도 이어지는 곳이 셋 있다.
  - 진입점: flow.md 2.1 "진입점 값이 흘러가는 길" (run31) 의 그 값이, .NET 이면 `+0x18b4` 에서 **묻힌다** (2.7).
  - kernel32: 2.5 kernel32 준비 (`+0x19a2`) 때 .NET exe 면 kernel32 가 이미 올라와 있을 것이다 `[추측]` (2.6 끝).
  - import: 2.6 import 걷기는 IL-only 면 **통째로 없다** (2.8).
- **PE_emulator 요청 26 ~ 30 이 다 끝났다.** 요청 30 은 "1단계는 할 만, 2단계는 지금 아님" 으로 돌려준다.
- **다음**: Q142 (정적은 sast/77 로 끝, 문서만) → [멈춤] — Q104 VM 단계 앞.

---

## 4. 아직 모르는 것

**먼저 알아 둘 것** — 남은 것은 세 무리다.

- **정적** → 새 **Q166** ① ~ ⑦.
- **동적** → 새 **Q167**.
  - `.NET exe` 가 있어야 본다 — `ScriptRunner.exe` (AnyCPU) · `tzsync.exe` (x64 IL-only). **test.exe 로는 못 본다.**
  - 멈춤점은 그 exe 일 때만 걸리게 조건을 붙인다.
  - 무엇을 언제 돌릴지는 **사용자 결정 몫** 이다 (plan.md: Q154 · Q156 · Q160 · Q162 와 같이 정할 만함).
  - 번호는 결과서 "동적" 목록 번호 그대로다. 3 (`ImageNumberLow/High`) 은 run44 사진으로 닫혀 빠졌다.
- **test.exe 로 볼 것** → **없다** (Q104 에 안 붙임). 이 갈래의 test.exe 값은 이미 `[관찰]` 이다.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| ① 커널이 기계 종류를 고르는 길 — `PspSelectMachineForProcess` 본문, `PspQueryComPlusRunUnderWoW`, `ExpReadComPlusPackage` (KUSD `ComPlusPackage` 를 쓰나), 섹션의 CLR 정보. AnyCPU 를 64비트로 띄우는 근거, CLR 헤더 없는 PE32 · 32BITREQUIRED PE32 가 64비트로 안 오는지 (2.4 ③) | 이번 범위 밖 — 커널은 호출 줄만 봄 | **Q166 ①** (정적) |
| ② kernelbase `Reg*` · `GetFileAttributesW` · `FindFirstFileW` → NT 함수 이름 | kernelbase 본문을 안 봄 (다른 decompiler 가 쓰는 중이었음) | **Q166 ②** (정적) |
| ③ 상태 3 옛 길 (`GetInstallation` · `FindVersionedRuntime` · `GetInstallRoot` · `ComputeVersionString`) 이 끝나는 HRESULT, `NoGuiFromShim` 설정 이름 | 본문을 안 읽음, Ghidra `.c` 가 설정 이름을 틀리게 그림 `[공백]` | **Q166 ③** (정적) · **Q167 6** (동적) |
| ④ mscoree `DllMain` (진입점 rva `0x9780`) 이 하는 일 | 본문 안 봄 — `+0x193c` 에서 불린다는 것도 `[추측]` | **Q166 ④** (정적) |
| ⑤ `LdrpInitializeThread +0x40` 갈래를 첫 스레드가 타는 때 (`_LdrpInitialize +0x3a` 옛 값 2 인데 `InitialThread` 인 경우) | 정적으로 못 정함 | **Q166 ⑤** (정적) · **Q167 4** (`LdrpInitializeThread+0x421` 에 서나) |
| ⑥ 커널이 `AddressOfEntryPoint = 0` exe 를 받는 길 — `MiVerifyImageHeader` (run31) 에 0 검사가 있나, 그때 `TransferAddress` (= 처음 `Rcx`) 가 `ImageBase` 인가 | 이번 범위 밖 | **Q166 ⑥** (정적) · **Q167 1** (`+0x18b4` 직전 `dq @rbx+0x80 L1`) |
| ⑦ IL-only exe 재배치를 누가 하나 — 커널 `MiRelocateImage` (run31 · run73) 가 이미 고친 상태인가 | ntdll 은 `+0x141f` 로 건너뛸 뿐. 결과서 `[추측: CLR]` | **Q166 ⑦** (정적) |
| 1 `Rcx` 가 바뀌는 순간의 값 — `+0x18b4` 에서 `r rax` (= `mscoree!_CorExeMain`), 직전 `[RBX+0x80]`, 이어 `NtContinue` 직전 CONTEXT 와 비교 | 실행 때 값, test.exe 는 안 탐 | **Q167 1** |
| 2 AnyCPU 가 정말 64비트로 오나 — `ScriptRunner.exe` TEB `+0x180c` (`WowTebOffset`) 0, `+0x627` 에 서나, `LdrpCorFixupImage` 앞뒤 `Magic` | 실행 때 값 | **Q167 2** |
| 5 mscoree 가 실제로 읽는 값 · 경로 — `InstallRoot` 값, `Policy\v4.0` 아래 값, 고른 버전 글자, `LoadLibraryExW` 에 넘어가는 전체 경로 (`mscoree!LoadLibraryWrapper+0x16` 에서 `du @rcx`), 파일 · 레지스트리 열기 목록 | 실행 때 값, 레지스트리 값은 덤프 밖 → "덤프에 더할 파일" 확정이 여기 달림 | **Q167 5** |
| 6 mscoreei 가 없을 때의 끝 — `_CorExeMain` 둘째 몸 `+0x55` (`ExitProcess`) 의 `ecx`, 메시지 상자가 뜨나 | 정적으로 HRESULT 를 못 정함 `[공백]` | **Q167 6** |
| 7 .NET exe 는 kernel32 · kernelbase 가 `+0x184d` 에서 먼저 올라오고 `+0x193c` 에서 먼저 켜지나 (`LdrpInitializeKernel32Functions` 입구에서 `lm`) | `[추측]` (2.6 끝) | **Q167 7** |
| 앱호스트 표식 (`hostfxr.dll` 찾기, 응용 dll 경로 자리, 단일 파일 번들) | 덤프에 샘플이 없어 코드로 못 봄 `[추측: 일반 지식]` | 대기열 없음 (로더 갈래와 상관없음) |
| PE_emulator 엔진 KUSD `ImageNumberLow/High` 값, `AEP 0` exe 처리 | PE_emulator 쪽 사정 | PE_emulator 확인거리 (2.11) |

- 추천 멈춤점 (결과서): `ntdll!LdrpCorInitialize` 입구 · `+0x16a` (`du` 이름) · `+0x1bb`, `ntdll!LdrpInitializeProcess+0x18b4`, `mscoree!InitShimImpl` 입구 · `+0x6f` · `+0xab` · `+0xe7`, `mscoree!ShellShim__CorExeMain+0x9e` · `+0x88` · `+0xb2` (상태 `g_shimImplStatus`). 모두 .NET exe 일 때만.

---

## 5. 더 읽기

- run 문서: [`run89-dotnet-start.md`](run89-dotnet-start.md) — 맨 위 한 줄 결론, "PE_emulator 쪽에서 쓸 것" (0 ~ 10 단계 · 할 만한가 · 덤프에 더할 파일 · 머리로 가르는 법 · 우리 지도에서 어디서 갈라지나 · 원문 질문이 어디서 답해졌나 · PE_emulator 확인거리), 호출 지점 표 (ntdll · mscoree), 호출 관계 그림, 사진 대조, 확인된 것, 결과서와 다르게 적은 곳 · 보탠 것 ① ~ ⑩, 정정 (짐 지도 7.1), 아직 확인 필요 (Q166 · Q167)
- 결과서: [`_workspace/sast/86_dotnet-start_result.md`](../../_workspace/sast/86_dotnet-start_result.md) — 한눈에 지도, Q1 차례 표 (1 ~ 17) · `LdrpCor*` 넷 · DLL 쪽 · 디렉터리 14 읽는 곳 넷 · 세 종류 비교표, Q2 (환경변수 · 레지스트리 · 파일 · 상태 값 · 없으면), Q3, Q4 (덤프 exe 11개), Q5, 동적으로 확인할 것, 다음 요청 후보. (`FindLatestVersionWithCallback +0x712 / +0x719` 는 call `+0x719` / ret `+0x71e` 로, DLL 쪽 "`+0x263 ~ +0x279` 에서 켬" 은 `+0x263 ~ +0x26b` · `+0x279 ~ +0x27d` 로 읽기)
- 요청서: [`_workspace/sast/86_dotnet-start.md`](../../_workspace/sast/86_dotnet-start.md)
- flow.md: [기준](flow.md) (mscoree pdb 줄 · run89 근거 로그) · 흐름도 앞 글 run89 문단 · **2.4 `LdrpInitializeProcess` 나무 · "뒤쪽에서 쓰는 앞쪽 값" · "CLR 헤더 갈래" 절** · 2.6 `+0x1aa4` 줄 · 2.10 `RtlUserThreadStart` 절 · 4장 run89 줄 셋 · 4.1 그림 2 `(9)` 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.5 `CorImage` · `CorILOnly` · 3.7 KUSD 머리 · 3.8 `UseCOR` · `LdrpCorExeMainRoutine` · 4장 진입점 짝 · 7.1 (셈 정정) · 7.2 · 7.3
- 앞 이야기:
  - [run16 해설](../02_sprint/run16-thunk-context-해설.md) 2.3 — 업무 시작 메모 · 사물함 메모 이야기 (이번 2.3 · 2.7 의 바탕)
  - [run18 해설](../02_sprint/run18-proc-init-setup-해설.md) 2.4 — 앞쪽 10묶음 (묶음 5 ".NET 검사")
  - [run19 해설](../02_sprint/run19-proc-init-modules-해설.md) 2.2 · 2.5 · 2.10 — `+0x18b4` 짐작, 뒤쪽 10묶음, KUSD `Cookie` 로 풀기
  - [run22 해설](../02_sprint/run22-map-snap-dep-해설.md) 2.4 · 2.5 — import 걷기, `CorILOnly`
  - [run27 해설](../02_sprint/run27-init-graph-해설.md) 2.1 — 연결 카드 · 진행 도장
  - [run31 해설](../02_sprint/run31-image-section-해설.md) 2.11 — 진입점 값이 흘러가는 길
  - [run38 해설](../02_sprint/run38-map-module-해설.md) 2.7 · 2.8 — DLL 쪽 .NET 디렉터리 14
  - [run44 해설](run44-baggage-snap-해설.md) 2.9 · 2.10 — raw `dq`, `끝까지 0`
  - [run59 해설](run59-init-leftovers-해설.md) 2.3 · 2.7 — 쿠키 둘, 포인터 섞기

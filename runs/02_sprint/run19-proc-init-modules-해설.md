# run19 proc-init-modules — 해설

> 이 글은 [run19 문서](run19-proc-init-modules.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **정정 (run36)**: 2.7 의 새 말 snap "IAT 칸마다 상대 DLL 함수의 실제 주소를 채워 넣는 일로 알려져 있다 `[추측]`" 은 `[정적]` 이 됐다. `LdrpSnapModule +0x3e7` 한 줄이 IAT 칸마다 상대 DLL 함수 주소를 쓴다 (이름·서수·forwarder 가 다 이 줄로 모임). 본문은 그대로 두었다 → [run36 해설 2.3](run36-snap-module-해설.md#23-질문-1--주소록-칸을-채우는-줄은-딱-하나)

- 대기열: **Q9b** — 2.4 `ntdll!LdrpInitializeProcess` 뒤쪽 `+0x12de ~ 끝` callee (호출 75곳 — 모듈 목록·디버거·WOW64·kernel32·import·그래프·TLS) (`_workspace/plan.md`)
  - run18 에서 Q9 를 둘로 나눈 **뒤쪽**이다. 앞쪽(Q9a)은 run18.
- 근거:
  - run 문서 [`run19-proc-init-modules.md`](run19-proc-init-modules.md) (가장 중요)
  - 요청서 [`_workspace/sast/14_proc-init-modules.md`](../../_workspace/sast/14_proc-init-modules.md)
  - 결과서 [`_workspace/sast/14_proc-init-modules_result.md`](../../_workspace/sast/14_proc-init-modules_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q9b(끝), 새 동적 Q46·Q47·Q48, 출처를 더한 Q10·Q11·Q14·Q15·Q16·Q23·Q24, 결정 기록
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll (`ntdll.dll`, 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`). run17·run18 과 같은 파일이다. analyze·export 는 다시 안 했다.
  - 구조체 칸 이름(`_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_CONTEXT`, `_KUSER_SHARED_DATA` …)은 **PDB 타입**으로 맞췄다.
  - 이 함수가 부르는 함수(callee)는 **이름만** 봤다 (지도 `_edges.tsv`). 본문은 안 읽었다. 그래서 이 글에서 "callee 이름으로 보면" 이 나오면, 그 뒤의 "하는 일" 은 `[추측: 이름]` 쪽이다.

이 글은 [run18 해설](run18-proc-init-setup-해설.md) 의 **이어지는 편**이다. 같은 함수(`LdrpInitializeProcess`)의 나머지 절반을 본다.

- 기초 말은 다시 안 푼다. **링크만** 건다.
  - 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출, 비트와 플래그 → [run9 해설](run9-create-user-process-해설.md) 2절
  - PEB, 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
  - 전역 변수 → [run16 해설 2.4](run16-thunk-context-해설.md)
  - 모듈 목록(PEB.Ldr)·모듈 항목 → [run18 해설 2.6](run18-proc-init-setup-해설.md#26-pebldr--건물-명부가-생기는-순간-0x1c1), 에필로그·`__fastfail` → [run18 해설 2.10](run18-proc-init-setup-해설.md#210-일찍-끝나는-길--19--5)
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**
  - PEB = 가게 **안내판**, TEB = 직원 **개인 안내판** (run10·run11)
  - ntdll = 가게 안 **관리실**. 관리사무소는 test.exe 건물과 관리실까지만 놓아 주고, **나머지 건물은 관리실 직원이 들여온다** (run10)
  - CONTEXT = **업무 시작 메모**, 유저 스택에 맡긴 원래 메모 = **사물함** (run11·run16). 커널이 넘긴 `Rcx` = 그 **사물함 메모의 주소**, `Rdx` = "**ntdll 건물 번지**" (run16·run17·run18)
  - 첫 스레드 = **가게 전체 세팅 담당** (run17)
  - PEB.Ldr = 가게 **건물 명부**, 모듈 항목 = **명부 한 줄** (run18)
  - 프로세스 힙 = **창고**, IFEO = **가게 이름별 특별 지시**, `TpInitializePackage` = 일꾼 **대기실** (run18)
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- 이번 run 문서엔 새 비유가 붙었다: run18 의 앞쪽이 "공책 펴고 창고 만들기" 였다면, 뒤쪽은 **"이삿짐 들이고 전원 켜기"** 다. 이 글은 그 말을 **가게 그림**으로 옮겨 쓴다 (가리키는 건 같다).
  - **짐 들이기** = 새 건물(DLL)을 가게 땅에 들여놓기 (run10 의 "건물 갖다 놓기")
  - **전기 연결** = 본 건물(exe)이 쓰는 옆 건물 함수 주소를 이어 주기 (2.7 의 snap)
  - **전원 켜기** = 건물마다 개업 스위치 켜기 (import DLL 초기화, 2.8)
  - **일꾼**(병렬 로딩 스레드 풀)은 짐이 들어오기 **바로 전**에 부른다 (2.9)

한 줄로 먼저 말하면:

- 뒤쪽 75곳은 **10묶음**이다. 순서는 이렇다 `[정적]`:
  - 명부 둘째 줄에 exe 쓰기 → TLS 칸 마련 → kernel32 들이기 → 일꾼 부를 준비 → **exe 가 필요한 DLL 들이고 연결** → 실행 준비 → (kernel32 thunk 한 번) → **DLL 전원 켜기** → exe TLS 콜백 → 마무리
- 01_sprint 에서 본 세 점(`+0x19a2`, `+0x1bbb`, `+0x1c99`)이 **코드 순서로도** 그 순서다 `[정적]` + `[관찰]`.
- 이번 바퀴의 **핵심 셋**:
  1. dll1·dll2 를 들여오는 자리가 **`+0x1aa4 LdrpMapAndSnapDependency`** 로 좁혀졌다 (자리는 `[정적]`, 하는 일은 `[추측: 이름]`) → 2.7
  2. `LdrpInitializeGraphRecurse` 를 부르는 세 자리 중 test.exe 가 타는 건 **`+0x1bb6` 하나**다 (`[정적: 조건상]`) → 2.8
  3. 일꾼 스레드가 생길 수 있는 **가장 이른 자리는 `+0x1a42`** 다 (`[정적: 자리]`) → 2.9
- **run18 정정 둘**: "exe 폴더" 는 사실 exe **파일 이름**(`BaseDllName`, test.exe) 이었고, 입구 `RCX` "로더 문맥" 은 **CONTEXT 레코드**(사물함 메모) 다 → 2.1·2.2

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

```
cmd.exe 창에서 "test.exe" 입력
 └─ cmd.exe (부모): CreateProcessInternalW → NtCreateUserProcess … NtResumeThread   (run9 ~ run14)

 test.exe 메인 스레드 (커널)
  KiStartUserThread → PspUserThreadStartup → PspInitializeThunkContext ═▶ IRETQ    (run15·run16)

 test.exe 메인 스레드 (유저, ntdll)
  LdrInitializeThunk
   └─ +0xe  로더 입구 네 함수 → 첫 스레드 판정                                   (run17)
             └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)
                  ├─ 앞쪽 +0x0 ~ +0x12dd    살림 차리기 (94곳)                  (run18)
                  └─ 뒤쪽 +0x12de ~ 끝      짐 들이고 전원 켜기 (75곳)          ← 이번 (run19)
                       (+0x19a2 / +0x1bbb / +0x1c99 는 01_sprint 에서 [관찰])
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16, 뒤는 추측)
```

- run18 이 이 함수의 **앞쪽 절반**을 봤다. 이번엔 **나머지 절반**이다.
- 그래서 이번 바퀴로 `LdrpInitializeProcess` 를 **처음부터 끝까지** 코드로 한 번 훑은 셈이다. flow.md 4장 공백 표의 "이 함수가 무엇을 어떤 순서로 하나" 줄이 **"(해결됨, 정적)"** 이 됐다.
- flow.md 로는 2.4 `LdrpInitializeProcess` 절의 뒤쪽 `[공백: 정적으로 아직 안 봄, Q9b]` 자리다. 결과가 2.5·2.6·2.7·2.9·2.10, 3장(일꾼)까지 퍼졌다 (3절).

### 무엇이 궁금했나

run 문서 "목적" 그대로.

1. **뒤쪽 75곳이 무엇을 어떤 순서로 하나** — 하는 일별 묶음으로. 관찰된 세 점(`+0x19a2`, `+0x1bbb`, `+0x1c99`)이 어느 묶음에 드나.
2. 덤으로:
   - exe 모듈 항목은 어디서 만드나
   - 다른 DLL 을 올리는 일은 **어느 호출로** 내려가나
   - 병렬 로딩(일꾼 스레드)은 어디서 켜지나
   - 간접 호출 4곳의 대상
   - 디버거 때문에 갈리는 길
   - `LdrInitState` 값과 함수의 끝

### 바퀴 전에 알던 것

- `[관찰]` (run7·run8) 메인 스레드에서 `+0x19a2` kernel32 준비 → `+0x1bbb` import DLL 초기화 → `+0x1c99` exe TLS 콜백, 이 순서.
- `[관찰]` (run8) `+0x1bb6`(call 자리)에서 멈췄을 때 **이미 dll1·dll2 가 메모리에 있었다** (`lm m dll*`). 그리고 dll1·dll2 는 `LdrpLoadDll` 을 **타지 않았다**.
- `[관찰]` (run7) `+0x19a2` 안에서 `LdrLoadDll` 이 두 번 불렸다.
- `[관찰]` (run8) exe TLS 콜백은 그래프 순회가 아니라 이 함수가 `+0x1c99` 에서 **직접** 부른다.
- `[관찰]` (run8) ntdll 일꾼 2개가 import DLL 초기화가 시작될 무렵(BEFORE-GRAPH 직후) 유저 쪽에 도착한다.
- `[정적]` (run18) 앞쪽은 아무것도 메모리에 올리지 않는다. 앞쪽 끝의 명부엔 **ntdll 뿐**이다. exe 항목은 뒤쪽 `+0x1359` 에서 만든다.
- `[정적]` (run18) 입구 `RCX` 는 `[RSP+0xe0]` 에 저장만 한다. 처음 읽는 곳은 뒤쪽 `+0x17f4`.
- `[추측]` (flow.md 2.6, run18) 올리는 일은 `+0x12de` 뒤, `+0x1bb6` 앞에 일어난다.
- `[추측]` (flow.md 3장, run18) 일꾼은 뒤쪽 준비(`+0x12de`) 뒤에 만들어진다.
- `[추측]` (flow.md 2.5·2.6) kernel32·dll1·dll2 를 올리면서 의존 관계 그래프를 만들고, `LdrpInitializeGraphRecurse` 가 그 그래프 순서대로 켠다.

---

## 2. 찾은 것

### 2.1 run18 정정 하나 — exe 폴더가 아니라 exe 파일 이름

run18 해설에서 먼저 고칠 곳부터 보자. run18 을 읽은 그림이 틀린 채로 남아 있으면 2.6 이 헷갈린다.

**run18 해설에 틀리게 쓴 곳**

- 2.4 표 묶음 10 요약: "exe 폴더, …"
- 2.4 "묶음 10 — 명부 첫 줄 쓰기" 첫 줄: "exe 폴더를 잘라 `[RSP+0x130]` 에 두기"
- (출처는 run18 문서 묶음 10 표와 sast/13 질문 4 의 "exe 폴더 잘라 두기" 다.)

**실제로 코드가 하는 일** `[정적]` — 단계별로

1. 앞쪽 `+0x11b0 ~ +0x1224` 는 `ImagePathName`(exe 전체 경로) 의 **끝에서 앞으로** `\` 를 찾아 간다.
2. 찾은 자리 **바로 뒤부터 끝까지** 를 `UNICODE_STRING` 하나로 만든다.
   - `+0x11f4`: 길이 = 끝 − 그 자리
   - `+0x1218`: 버퍼 = 그 자리
   - `\` 가 하나도 없으면 `+0x1222` 에서 전체를 쓴다.
3. 그러니 잘라 둔 건 `\` **앞**(폴더)이 아니라 `\` **뒤**(파일 이름) 다.
4. 그 값을 뒤쪽 `+0x1408` 이 exe 모듈 항목의 **`BaseDllName`**(+0x58, PDB) 칸에 넣는다 (2.6).

→ `[RSP+0x130]` = **exe 의 `BaseDllName` = 파일 이름 부분 (test.exe)** `[정적]`.

- **새 말 — `UNICODE_STRING`**: 글자 수와 "글자가 놓인 곳 주소" 를 같이 적어 둔 문자열 묶음 `{Length, MaximumLength, Buffer}` 다 (결과서).
  - 비유: 글자를 직접 들고 다니는 게 아니라 "**몇 글자, 어디부터**" 라고 적은 **쪽지**. 그래서 원래 경로 한가운데(`\` 바로 뒤)를 가리키기만 해도 "파일 이름 쪽지" 가 된다.

**안 바뀐 것**

- 같은 묶음의 "**exe 폴더 종류 검사**"(`+0x117e LdrpCheckAppDirType`) 는 그대로 맞다. 폴더를 **검사**하는 것과 이름을 **잘라 두는** 것은 따로였다.

**어디를 고쳤나**

- run18 문서 맨 위 정정 한 줄, flow.md 2.4 앞쪽 표 묶음 10 ("exe 파일 이름 잘라 두기 … ~~exe 폴더~~ run19 정정").
- run18 해설은 맨 위에 정정 안내 한 줄만 넣었다. 본문은 그대로다.
- 실행 값으로 맞춰 보기는 **Q46** (`+0x1455` 에서 `dt … BaseDllName`, test.exe 인가).

- 비유: run18 에선 "본 건물 주소에서 **동네 이름**을 오려 둔다" 로 읽었다. 실제로는 **건물 이름표**를 오려 둔 것이었다. 그 이름표가 명부 둘째 줄(exe 항목)의 "짧은 이름" 칸에 붙는다.

### 2.2 run18 정정 둘 — 입구 RCX 는 CONTEXT 레코드

**run18 해설에 쓴 곳**

- 4절 표 "`RCX`(로더 문맥)를 무엇에 쓰나".
- 같은 해설 2.5 는 run16 그대로 "`Rcx` = 사물함에 맡긴 원래 메모" 라고 맞게 썼다. 4절 표에만 "로더 문맥" 이라는 다른 이름이 들어갔다 (run18 문서·sast/14 요청서의 말).

**근거** `[정적]` — 단계별로

1. 입구에서 `RCX` 를 `[RSP+0xe0]` 에 넣어 둔다 (run18).
2. 뒤쪽 `+0x18b4` 에 `MOV [RBX+0x80],RAX` 가 있다. 이때 `RBX = [RSP+0xe0]` 이다.
3. PDB `_CONTEXT` 에서 **`Rcx` 칸이 `0x80`** 이다.
4. 그러니 `[RSP+0xe0]` 가 가리키는 건 CONTEXT 모양이다 → **CONTEXT 레코드 (첫 스레드의 맡긴 문맥)**.

- run16·run17 이 말한 "맡긴 문맥이 `LdrpInitializeProcess` 까지 그대로 온다" 와 맞는다.
- run17 문서는 "맡긴 문맥" 이라고 맞게 썼고, run18 이 다른 말로 옮긴 것이다. **뜻이 틀린 건 아니라** run18 문서는 맨 위 정정 한 줄에 같이 적었다. flow.md 2.4 의 "로더 문맥" 은 "CONTEXT 레코드" 로 고쳤다.

**`+0x18b4` 가 하는 일 — 업무 시작 메모의 한 칸 고쳐 쓰기**

- [run16 해설 2.4](run16-thunk-context-해설.md) 표에서 CONTEXT `Rcx +0x80` 칸 = **exe 진입점** 이었다 `[정적]`.
- 결과서: `+0x188c`(풀기)·`+0x18b4`(쓰기) 는 **.NET 이고 첫 스레드(`TEB.SameTebFlags & 0x400`)일 때** `LdrpCorExeMainRoutine` 포인터를 풀어서 `CONTEXT.Rcx` 에 넣는 자리다 `[정적]`.
- 두 사실을 이으면: .NET exe 는 메모의 "처음 손에 쥘 것" 칸을 진입점 대신 `LdrpCorExeMainRoutine` 으로 바꿔 쓰는 것으로 보인다 `[추측]` (이 칸 뒤의 일은 안 봤다). test.exe 는 .NET 이 아니라 안 탈 것 `[추측]`.

**`RCX` 를 쓰는 곳 셋** `[정적]` (run 문서 입구 레지스터 표)

| 자리 | 무엇에 | 어느 길 |
|---|---|---|
| `+0x17f4` | 간접 호출 `g_LdrpWow64LdrpInitialize` 의 인자 | WOW64 길 안 |
| `+0x18b4` | 위 `CONTEXT.Rcx` 칸 쓰기 | .NET 길 안 |
| `+0x1b83` | 전역 `LdrpProcessInitContextRecord` 에 저장 (import DLL 초기화 동안, 2.8) | 묶음 8 |

- 앞의 두 줄은 각각 WOW64·.NET 길 안에 있다 `[정적]`. test.exe 가 둘 다 아니면 남는 건 `+0x1b83` 이다 `[추측]` (`UseWOW64`·`UseCOR` 실제 값은 Q44·Q46).

- 비유: 세팅 담당 직원이 책상 서랍(`[RSP+0xe0]`)에 넣어 둔 쪽지는 **사물함 메모 그 자체**였다. run18 에서 "관리실 업무 메모" 라고 이름표를 잘못 붙였을 뿐이다.

### 2.3 한눈에 — 뒤쪽 대표 호출

run 문서 "호출 관계" 를 더 줄였다. 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)). ①~⑩ 은 2.5 의 묶음 번호다.

```
_LdrpInitialize +0x16c
 └─ LdrpInitializeProcess (0x8c404)        ← RCX = CONTEXT 레코드 → [RSP+0xe0], RDX = ntdll 주소 (run18)
      │  앞쪽 +0x0 ~ +0x12dd  [정적] run18 (생략)
      │  뒤쪽 +0x12de ~ 끝    [정적] run19
      ├─ +0x12e3 LdrpInitParallelLoadingSupport   ① 이벤트·임계 구역
      ├─ +0x135e LdrpAllocateModuleEntry          ① exe 항목 → +0x1374 LdrpImageEntry
      ├─ +0x157c LdrpAllocateModuleEntry          ② AvrfAppVerifierMode != 0 일 때만
      │  +0x1766 UseWOW64 ?  켜짐 → WOW64 길 ③ (끝 정리 +0x1d1b 로 점프)
      │                     꺼짐 → +0x1806 네이티브 길 (아래)
      ├─ +0x18d0 LdrpInitializeTls                ④
      ├─ +0x19a2 LdrpInitializeKernel32Functions  ⑤ Subsystem 2/3 일 때    [관찰] run7
      ├─ +0x1a47 LdrpEnableParallelLoading        ⑥ 스레드 풀·일감 → +0x1a4c LdrInitState=1
      ├─ +0x1aa9 LdrpMapAndSnapDependency         ⑥ RCX = 로드 문맥 (IL-only 아니면)
      ├─ +0x1af2 LdrpPrepareModuleForExecution    ⑥ RCX = LdrpImageEntry → +0x1b1e LdrInitState=2
      ├─ +0x1b40 LdrpDoDebuggerBreak              ⑦ BeingDebugged 일 때만
      ├─ +0x1b5e (간접) Kernel32ThreadInitThunkFunction(1,0,0)   ⑦ 값 있을 때
      ├─ +0x1bbb LdrpInitializeGraphRecurse       ⑧ RCX = exe DdagNode    [관찰] run7·run8
      ├─ +0x1c99 LdrpCallTlsInitializers          ⑨ TlsIndex != 0 일 때   [관찰] run7·run8
      ├─ +0x1d1b (간접) PEB.PostProcessInitRoutine  ⑩ 값 있을 때
      └─ +0x1e2d 정상 반환 (EAX = 0) → 에필로그 +0xa7b
```

읽을 때 주의:

- 75곳 중 **대표만** 남겼다. 전부는 `_calls.tsv` 의 `LdrpInitializeProcess` 줄 (커밋 안 됨), 조금 더 긴 트리는 run 문서 "호출 관계".
- `+0x1766`, `+0x1e2d` 두 줄은 **호출이 아니다.** `+0x1766` 은 갈림길 검사 자리, `+0x1e2d` 는 반환 명령 자리다.
- `[관찰]` 이 붙은 세 줄은 01_sprint 에서 디버거로 본 것이다. 이번에 **코드로도** 같은 자리·같은 순서인 걸 확인했다 (run 문서 표에선 `[정적] + [26100.9457 확인]`).

### 2.4 한 줄씩 읽는 법

**줄 1: `├─ +0x1aa9 LdrpMapAndSnapDependency   ⑥`**

- 누가: `LdrpInitializeProcess` (메인 스레드 = 세팅 담당).
- 어디서: 부르는 자리 `+0x1aa4`, 돌아올 자리 **`+0x1aa9`** (콜스택에 찍힐 숫자).
- 누구를: `LdrpMapAndSnapDependency`.
- 무엇을 위해: 이름을 그대로 읽으면 "의존하는 것(Dependency)을 올리고(Map) 연결한다(Snap)" 다 `[추측: 이름]`. **exe 의 import 를 올리고 연결하는 가장 유력한 자리**다.
  - 넘기는 것: `RCX = &[RSP+0x2b0]` = **로드 문맥** (2.6 에서 만든 지역 구조체). exe 항목이 **아니다** `[정적]`.
  - 조건: exe 가 IL-only .NET 이 아니면 탄다 (`+0x1a73 TEST R12B ; JZ +0x1a89`) `[정적]`.
- 자세한 추론은 2.7.

**줄 2: `├─ +0x1bbb LdrpInitializeGraphRecurse   ⑧`**

- 누가: `LdrpInitializeProcess`.
- 어디서: 부르는 자리 `+0x1bb6`, 돌아올 자리 **`+0x1bbb`**. run7 콜스택(`[관찰]`)과 `_calls.tsv`(`[정적]`) 둘 다 같은 숫자다.
- 누구를: `LdrpInitializeGraphRecurse`.
- 무엇을 위해: import DLL 초기화 (dll2 → dll1) `[관찰]` — 가게 비유로 **전원 켜기**.
  - 이번에 새로 본 것: 넘기는 것 `RCX` = **exe 항목의 `DdagNode`** (`[LdrpImageEntry+0x98]`, `+0x1baf`), `RDX` = 상태 칸 주소(`[RSP+0x2d8]`), `R8` = 바이트 0 칸 주소(`&[RSP+0x56]`) `[정적]`.
  - 조건 없음. 로더 락 안에서 부른다 `[정적]`.
- 자세한 추론은 2.8.

### 2.5 10묶음 표 — 천천히

#### 표 읽는 법

- 칸은 [run18 해설 2.4 "표 읽는 법"](run18-proc-init-setup-해설.md#표-읽는-법--칸-하나씩) 과 같다 (# / 범위 / 요약 / 대표 호출 call·ret / 호출 수(잡일)).
- 다른 점 셋:
  - **"표시" 칸**이 하나 더 있다. 관찰된 세 점이 든 묶음 5·8·9 는 `[정적] + [26100.9457 확인]` 이다. `[26100.9457 확인]` = 이 빌드에서 디버거로 확인했다는 run 문서 표의 표시다 (CLAUDE.md).
  - **잡일 종류**가 앞쪽과 다르다: `LdrpLogInternal`, `LdrpLogDllState`, `NtClose`, 문자열 버퍼 해제(`+0x1563`), `LdrpLogEtwHotPatchStatus`. `memset` 은 뒤쪽에 없다. 합도 12 로, 앞쪽 30 보다 훨씬 적다.
  - 요약 안의 **괄호 ( )** 는 "~일 때만" 이다. 뒤쪽은 AppVerifier·WOW64·.NET·shim 같은 조건부 일이 많다.
- flow.md 2.4 에선 앞쪽 표와 헷갈리지 않게 이 묶음들을 **`뒤1 ~ 뒤10`** 이라고 부른다.

#### 표 (run 문서와 같은 숫자)

| # | 범위 | 요약 | 대표 호출 (call / ret) | 호출 수 (잡일) | 표시 |
|---|---|---|---|---|---|
| 1 | `+0x12de ~ +0x1568` | 병렬 로딩 지원 만들기, 일감 비우기, DLL 검색 경로, **exe 모듈 항목 만들기** (→ 전역 `LdrpImageEntry`), 현재 폴더 초기화 | `LdrpAllocateModuleEntry` `+0x1359 / +0x135e` | 13 (4) | [정적] |
| 2 | `+0x1569 ~ +0x1752` | **AppVerifier 가 켜졌을 때만**: 둘째 모듈 항목(복제) 만들어 올리고, 그래프 초기화 또는 `AVrfInitializeVerifier` | `LdrpAllocateModuleEntry` `+0x1577 / +0x157c` | 9 (2) | [정적] |
| 3 | `+0x1753 ~ +0x1805` | 진행 중 개수 내리기, CPU 기능 표, **WOW64 갈림**: WOW64 면 `LdrpLoadWow64` → 디버거 멈춤 → `LdrInitState=3` → `g_LdrpWow64LdrpInitialize` 호출 → 끝 정리로 점프 | `LdrpLoadWow64` `+0x17c1 / +0x17c6` (WOW64 만) | 8 (3) | [정적] |
| 4 | `+0x1806 ~ +0x195d` | **네이티브 길**: CFG·스케줄러 공유 데이터, (.NET 이면 `LdrpCorInitialize`·검증), **TLS 준비**, (핫패치 ETW 로그), (.NET 그래프) | `LdrpInitializeTls` `+0x18cb / +0x18d0` | 8 (1) | [정적] |
| 5 | `+0x195e ~ +0x1a41` | import 리다이렉션, **kernel32 준비**, 일감 비우기, (shim 데이터 있으면) shim 엔진 | `LdrpInitializeKernel32Functions` `+0x199d / +0x19a2` | 5 (0) | [정적] + [26100.9457 확인] run7 |
| 6 | `+0x1a42 ~ +0x1b23` | **병렬 로딩 켜기**, `LdrInitState=1`, exe 의 import 를 **올리고 연결**, 일감 비우기, 실행 준비. 끝에 `LdrInitState=2` | `LdrpMapAndSnapDependency` `+0x1aa4 / +0x1aa9`, `LdrpPrepareModuleForExecution` `+0x1aed / +0x1af2` | 7 (0) | [정적] |
| 7 | `+0x1b24 ~ +0x1b8b` | 패치 데이터, **디버거면 멈춤**, 진행 중 개수 내리기, **kernel32 스레드 시작 thunk 를 `(1,0,0)` 으로 한 번 호출**, 사용자 Windows 폴더 | 간접 `Kernel32ThreadInitThunkFunction` `+0x1b59 / +0x1b5e` | 5 (0) | [정적] |
| 8 | `+0x1b8c ~ +0x1c24` | 일감 비우기, 로더 락 잡고 **exe 그래프 초기화**, 락 풀기, 로드 문맥·DLL 경로 정리 | `LdrpInitializeGraphRecurse` `+0x1bb6 / +0x1bbb` | 7 (0) | [정적] + [26100.9457 확인] run7·run8 |
| 9 | `+0x1c25 ~ +0x1ca7` + 예외 조각 | SMT 지연 잠 초기화, **exe TLS 콜백** (활성화 컨텍스트 켜고 끔) | `LdrpCallTlsInitializers` `+0x1c94 / +0x1c99` | 4 + 조각 1 (0) | [정적] + [26100.9457 확인] run7·run8 |
| 10 | `+0x1ca8 ~ +0x1e46` | shim 후처리, **`PEB.PostProcessInitRoutine` 호출**, 핸들 닫기, CPU 기능·SymCrypt 정하기, 정상 반환 | 간접 `[RDI+0x230]` `+0x1d16 / +0x1d1b` | 8 (2) | [정적] |
| 합 | | | | **75 (12)** | |

- 합 75 = 본체 74 + 예외 정리 조각 1. run18 이 미리 센 "뒤쪽 75" 와 같다.
- 잡일 12 = 묶음 1 에 `LdrpLogInternal` 2 + `LdrpLogDllState` 1 + 문자열 해제 1, 묶음 2 에 `LdrpLogDllState` 2, 묶음 3 에 `NtClose` 3, 묶음 4 에 `LdrpLogEtwHotPatchStatus` 1, 묶음 10 에 `NtClose` 2.
- 이름 주의: `+0x1563` (ret `+0x1568`) 문자열 버퍼 해제는 같은 주소에 이름이 3개다 (`RtlFreeAnsiString`, `RtlFreeUTF8String`, `RtlFreeUnicodeString`). [ICF 별칭](run9-create-user-process-해설.md#한-주소에-이름-여럿-icf-별칭) 과 같은 이야기다.
- 묶음은 **오프셋 순서로** 자른 것이다. 실제로 이 순서대로 서는지는 버스를 타 봐야 안다 (Q46). 다만 관찰된 세 점은 코드 순서 `+0x19a2 → +0x1bbb → +0x1c99` 가 run7·run8 관찰 순서와 같았다 `[정적] + [관찰]`.

#### 한 줄을 끝까지 읽어 보기 — 묶음 6

핵심 셋 중 둘(①, ③)이 이 묶음에 있다. 그래서 이 줄을 고른다.

```
| 6 | +0x1a42 ~ +0x1b23 | 병렬 로딩 켜기, LdrInitState=1, exe 의 import 를 올리고 연결, 일감 비우기, 실행 준비. 끝에 LdrInitState=2 | LdrpMapAndSnapDependency +0x1aa4 / +0x1aa9, LdrpPrepareModuleForExecution +0x1aed / +0x1af2 | 7 (0) | [정적] |
```

- **범위**: 함수 시작에서 `0x1a42` 바이트째부터 `0x1b23` 바이트째까지.
- **호출 수 7 (0)**: 이 안에 call 이 7곳이고, **잡일은 0** 이다. 7곳 모두 "진짜 일" 이다.
- **대표 호출이 둘**인 유일한 묶음이다. 하나는 "들이고 연결", 하나는 "실행 준비".
- **안에서 일어나는 순서** `[정적]` (run 문서):
  1. `+0x1a42` `LdrpEnableParallelLoading` — 일꾼 부를 준비 (2.9)
  2. `+0x1a4c` `LdrInitState = 1` (2.12)
  3. `+0x1a60` exe DDAG `State` = 2, `+0x1a71` 상태 칸 = 0
  4. `+0x1a89` `LdrpThreadTokenSetMainThreadToken`
  5. **`+0x1aa4` `LdrpMapAndSnapDependency`** (2.7)
  6. `+0x1aac` `LdrpDrainWorkQueue(1)` — 인자 1 은 여기뿐
  7. (토큰이 있으면) `+0x1abd` 토큰 풀기
  8. `+0x1aed` `LdrpPrepareModuleForExecution(exe 항목, &상태)`
  9. `+0x1b1e` `LdrInitState = 2` (`+0x1aed` 뒤 상태가 음수가 아님을 확인한 다음)
- (IL-only .NET 이면 5번 대신 `+0x1a7f LdrpCorProcessImports` 를 탄다. 7곳에 이것도 들어 있다.)
- **요약 한 줄로**: "일꾼 부를 준비를 하고, 팻말을 '짐 들이는 중' 으로 바꾸고, exe 가 필요한 건물을 들여 전기를 잇고, 일감이 끝나길 기다린 뒤 `[추측]`, exe 를 실행 준비시키고, 팻말을 '짐 다 들임' 으로 바꾼다."

#### 묶음 하나씩 — 무엇을, 비유로

한 번에 한 묶음씩. 모두 `[정적]` 이고, 따로 적은 것만 태그가 다르다. 큰 것(묶음 1·6·8·9)은 아래 절에서 따로 천천히 본다.

**묶음 1 — 명부 둘째 줄 (`+0x12de ~ +0x1568`, 13곳)**

- 병렬 로딩 지원(이벤트·임계 구역) → 일감 비우기 → DLL 검색 경로 → **exe 모듈 항목** → 현재 폴더.
- 자세히는 2.6 (exe 항목), 2.9 (병렬 로딩 지원).
- 비유: 일꾼 연락망(신호등·열쇠)을 깔고, **명부 둘째 줄에 본 건물 이름**을 쓴다.

**묶음 2 — 점검관이 오는 날만 (`+0x1569 ~ +0x1752`, 9곳)**

- **새 말 — AppVerifier**: 프로그램이 위험한 짓을 하는지 붙어서 검사하는 진단 도구로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 이 묶음은 `AvrfAppVerifierMode != 0` 일 때만 들어간다. 0 이면 통째로 건너뛴다. 2.6 끝에서 조금 더.
- 비유: **점검관이 온 날에만** 하는 일. 점검용 복사 줄을 하나 더 만든다.

**묶음 3 — 32비트 손님 통로 (`+0x1753 ~ +0x1805`, 8곳)**

- 진행 중 개수 내리기(`LdrpDropLastInProgressCount`), CPU 기능 표.
- **WOW64 갈림** (`+0x1766`). [WoW64](run17-ldr-init-entry-해설.md#29-나중-스레드-길--짧게) 면:
  - `LdrpLoadWow64` → (`BeingDebugged` 면 디버거 멈춤) → `LdrInitState = 3` → `g_LdrpWow64LdrpInitialize(CONTEXT)` 간접 호출 → `+0x1801 JMP` 로 끝 정리 `+0x1d1b` 까지 건너뛴다.
  - 그러면 TLS·shim·`PostProcessInitRoutine` 을 **안 거친다** `[정적]`.
- x64 test.exe 는 `UseWOW64` = 0 이라 `+0x1806` 네이티브 길로 갈 것 `[추측]` (Q44).
- 비유: 32비트 손님이면 **다른 통로**로 안내하고, 여기 통로는 끝까지 건너뛴다.

**묶음 4 — TLS 칸 마련 (`+0x1806 ~ +0x195d`, 8곳)**

- 네이티브 길의 시작. CFG·스케줄러 공유 데이터, **TLS 준비**(`LdrpInitializeTls`).
  - CFG = 간접 호출 대상이 허락된 주소인지 확인하는 보호 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- (.NET 이면) `LdrpCorInitialize`·검증·그래프 — 전역 `UseCOR` 값으로 갈린다. 그 값은 앞쪽 `+0x64d ~ +0x66c` 에서 정한다 (COM 디렉터리가 없으면 안 켬, 결과서). run18 해설 묶음 5 의 ".NET(COR) 이미지 검사" 가 그 자리다.
  - **IL-only** = .NET 중간 언어만 들어 있는 exe 로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 앞쪽 `+0x13d2` 부터 `R12B` 에 그 표시가 있다 `[정적]`.
- **눈여겨볼 것**: TLS 준비(`+0x18cb`)가 kernel32 준비(`+0x199d`)보다 **앞**이다. 이때 명부엔 ntdll 과 exe 뿐이라, exe 의 TLS 칸을 먼저 만들어 두는 것으로 보인다 `[추측]` (callee 본문 안 봄, Q16).
- 비유: 본 건물 직원마다 쓸 **개인 사물칸(TLS)** 자리를 미리 마련한다.

**묶음 5 — kernel32 들이기 (`+0x195e ~ +0x1a41`, 5곳, 잡일 0)**

- import 리다이렉션(`+0x195e`), **kernel32 준비**(`+0x199d`), 일감 비우기(`+0x19c1`), (shim 데이터가 있으면) shim 엔진(`+0x19ef`).
- kernel32 준비는 **exe 헤더 `Subsystem` 이 2(GUI) 또는 3(콘솔)일 때만** 부른다 (`+0x1980 ~ +0x1993`) `[정적]`. import 목록은 안 본다 (flow.md 2.5).
- **새 말 — shim**: 오래된 프로그램이 새 Windows 에서도 돌게 끼워 넣는 호환성 보정 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). test.exe 는 shim 데이터가 없으면 안 탄다.
- `[관찰]` run7: `+0x19a2` 안에서 `LdrLoadDll` 두 번.
- 비유: 본 건물이 뭘 쓰든 상관없이, 가게라면 꼭 필요한 **기본 설비 건물(kernel32)** 부터 관리실이 직접 주문해서 들인다.

**묶음 6 — 일꾼 부르고 짐 들이고 연결 (`+0x1a42 ~ +0x1b23`, 7곳)**

- 위에서 한 줄 끝까지 읽은 그 묶음이다. 2.7·2.9·2.12.

**묶음 7 — 문 열기 직전 (`+0x1b24 ~ +0x1b8b`, 5곳, 잡일 0)**

- 패치 데이터(`LdrpInitializePatchData`) → (`BeingDebugged` 면) **디버거 멈춤** (2.13) → 진행 중 개수 내리기 → **kernel32 thunk `(1,0,0)`** (2.10) → 사용자 Windows 폴더 (`+0x1b72 LdrpInitializePerUserWindowsDirectory`, 인자는 묶음 5 의 `+0x199d` 가 채운 출력 칸 `[RSP+0x188]`).
- 비유: 점검원이 와 있으면 잠깐 멈추고, kernel32 의 "직원 출근 인사" 를 한 번 미리 해 본다.

**묶음 8 — 전원 켜기 (`+0x1b8c ~ +0x1c24`, 7곳, 잡일 0)**

- 일감 비우기 → 로더 락 → **exe 그래프 초기화** → 락 풀기 → 로드 문맥·DLL 경로 정리. 2.8.

**묶음 9 — 본 건물 TLS 콜백 (`+0x1c25 ~ +0x1ca7` + 예외 조각, 4 + 1곳)**

- SMT 지연 잠 초기화(`+0x1c25 LdrpInitializeSmtDelayedSleep`), **exe TLS 콜백**. 2.11.

**묶음 10 — 마무리하고 뒷문으로 (`+0x1ca8 ~ +0x1e46`, 8곳)**

- shim 후처리 → `PEB.PostProcessInitRoutine` → 핸들 2개 닫기 → CPU 기능·SymCrypt → 정상 반환. 2.10·2.14.

#### 비유로 다시 한 번 — 뒤쪽 10묶음

1. 일꾼 연락망 깔기, **명부 둘째 줄에 본 건물(exe)**
2. (점검관 오는 날만) 점검용 복사 줄
3. (32비트 손님이면) 다른 통로로
4. **개인 사물칸(TLS) 마련**
5. **기본 설비 건물(kernel32) 들이기**
6. **일꾼 부르기 → 짐(dll1·dll2) 들이고 전기 연결 → 실행 준비**
7. (점검원 있으면 멈춤), kernel32 출근 인사 한 번
8. **전원 켜기 (dll2 → dll1)**
9. **본 건물 TLS 콜백**
10. 마무리하고 뒷문으로

→ run18 앞쪽이 "**안에서 쓸 것 준비**" 였다면, 뒤쪽은 "**바깥에서 들여와 잇고 켜기**" 다.

### 2.6 exe 모듈 항목 — 명부 둘째 줄 (묶음 1)

[run18 해설 2.7](run18-proc-init-setup-해설.md#27-명부-첫-줄--ntdll-항목-그리고-두-번-부르는-이유) 에서 명부 첫 줄에 관리실(ntdll) 이름을 썼고, "본 건물 이름은 다음 장" 이라고 했다. 그 다음 장이다.

**순서대로** `[정적]`

| 순서 | 자리 | 하는 일 |
|---|---|---|
| 1 | `+0x1351` | 지역 구조체 `[RSP+0x2b0]` (= **로드 문맥**) 만들기 |
| 2 | `+0x1359 / +0x135e` | `LdrpAllocateModuleEntry(로드 문맥)` — **조건 없음** |
| 3 | `+0x1374` | 돌려받은 항목을 **전역 `LdrpImageEntry`** 에 저장. 0 이면 `0xc0000017` 반환 |
| 4 | `+0x1391 ~ +0x144c` | 항목 칸 채우기 (아래) |
| 5 | `+0x1419 ~ +0x1430` | (조건부) 재배치 |
| 6 | `+0x1455` → `+0x14d2` → `+0x14ee` (ret) | `LdrpInsertDataTableEntry` → `LdrpInsertModuleToIndex(항목, exe NT 헤더)` → `LdrpProcessMappedModule(항목, 0, UseWOW64==0)` |
| 7 | `+0x1508` | 성공하면 DDAG `State` = 9 |
| 8 | `+0x1517 / +0x151c` | `RtlpInitCurrentDir` (현재 폴더 `[추측]`) — **실패해도 로그만 찍고 계속** |

**1번 — 새 말: 로드 문맥**

- run 문서가 지역 구조체 `[RSP+0x2b0]` 에 붙인 이름이다. 들어 있는 것 `[정적]`:
  - `+0x2b0` exe 경로 사본
  - `+0x2c0` DLL 경로 버퍼
  - `+0x2d0` 플래그 0x200
  - `+0x2d8` 상태 칸 주소 (`&[RSP+0x50]`)
- 비유: **짐 들이기 작업 지시서.** "누구 짐인가(exe 경로), 어디서 찾나(DLL 경로), 결과는 여기 적어라(상태 칸)". 이 지시서가 2.7 에서 다시 나온다.

**3번 — 전역 `LdrpImageEntry`**

- `refs` 로 보면 이 전역은 46줄에 나온다. 그중 **쓰기는 이 한 곳뿐**이고, 나머지 45줄은 읽기다 `[정적]`.
- 비유: 관리실 벽 주소록에 "**본 건물 명부 줄은 여기**" 라고 딱 한 번 적는다. 그 뒤로는 다들 이 주소록을 보고 찾아간다 (2.8 의 `+0x1baf`, 2.11 의 `+0x1c94` 가 그렇다).

**4번 — 칸 채우기** `[정적]` (칸 이름은 PDB `_LDR_DATA_TABLE_ENTRY` / `_LDR_DDAG_NODE`)

| 칸 | 값 | 자리 |
|---|---|---|
| `FullDllName` (+0x48) | `ImagePathName` (전체 경로) | `+0x13aa` |
| **`BaseDllName`** (+0x58) | **파일 이름 부분 (test.exe)** — 2.1 의 그 값 | `+0x1408` |
| `DllBase` (+0x30) | `PEB.ImageBaseAddress` (커널이 놓아 준 자리, run10) | `+0x144c` |
| DDAG `LoadCount` | -1 | `+0x1391` |
| 항목 `ObsoleteLoadCount` | 0xffff | `+0x13a4` |
| `Flags` | `ImageDll` 켬. 패키지 프로세스면 `PackagedBinary`, .NET 이면 `CorImage`(+IL-only 면 `CorILOnly`) | `+0x13c1 ~ +0x13f7` |

- `LoadCount` = -1, `ObsoleteLoadCount` = 0xffff → "**절대 안 내려가는 모듈**" 이다 (run 문서).
  - 비유: 명부 줄에 **"철거 금지"** 도장.
- `+0x1391` 은 같은 줄을 `R12`=2 번 반복한다. 효과는 한 번과 같고, 왜 그런지는 `[추측]` (앞쪽 값을 반복 횟수로 재사용한 모양).

**새 말 — DDAG 노드 (`DdagNode`)**

- 모듈 항목마다 하나씩 딸린 짝 구조체다 (`_LDR_DDAG_NODE`, PDB). 항목의 `+0x98` 칸 `DdagNode` 가 가리킨다 `[정적]`.
- 이번에 본 칸: `LoadCount`(+0x18), `State`(+0x38) `[정적]`.
- 이름의 DDAG 는 모듈끼리의 **의존 관계 그래프의 마디**를 뜻한다고 알려져 있다 `[추측]` (일반적으로 알려진 설명). flow.md 2.5·2.6 의 `[추측]` "올리면서 의존 관계 그래프를 만든다" 의 그 그래프다.
- 비유: 명부 줄마다 붙은 **연결 카드.** "이 건물은 어느 건물에 기대나" 를 그린 그림의 동그라미 하나. 2.8 에서 **전원 켜기는 이 카드를 들고 시작한다.**

**5번 — 재배치 갈림** `[정적]`

- 헤더의 `ImageBase` 가 `PEB.ImageBaseAddress` 와 **다르고**, IL-only 도 아니고, `UseWOW64 == 0` 일 때만 `LdrpProtectAndRelocateImage`.
- 보통은 커널이 이미 맞춰 둬서 건너뛸 것 `[추측]` → Q46 (`+0x1424` 에서 두 값 비교).

**6·7번 — 목록·색인에 넣기**

- 이름으로 보면 명부 목록과 색인에 넣는 일이다 `[추측: 이름]`.
- 성공하면 DDAG `State` = 9 (`LdrModulesReadyToRun`, PDB 열거). 그런데 뒤 `+0x1a60` 에서 2 (`LdrModulesMapped`) 로 **다시 쓴다** `[정적]`.
  - 왜 9 를 썼다가 2 로 되돌리는지는 이번 범위로는 모른다 → Q11·Q12.

**8번 — 뒤쪽 유일의 "실패해도 계속"**

- 뒤쪽의 다른 실패는 전부 함수를 끝내는데 (2.14), `RtlpInitCurrentDir` 실패만 로그를 찍고 계속 간다 `[정적]`.

**그래서**

- 명부: 앞쪽 끝 "**ntdll 뿐**" → 묶음 1 뒤 "**ntdll + exe**" `[정적]` (flow.md 2.4).
- 이 일은 kernel32 준비(`+0x199d`)보다 **앞**이다.
- 비유: 본 건물은 이미 땅 위에 서 있었다 (run10). 이제야 **명부 둘째 줄**에 전체 주소(`FullDllName`), 이름표(`BaseDllName`), 번지(`DllBase`), "철거 금지" 도장(`LoadCount` -1)을 적는다.

**곁가지 — 묶음 2 의 둘째 항목 (AppVerifier 일 때만)**

- `+0x1568 CMP AvrfAppVerifierMode,0 ; JZ +0x1753` — 0 이면 둘째 묶음 전체를 건너뛴다 `[정적]`.
- `AvrfAppVerifierMode` 를 쓰는 곳은 `AVrfInitializeVerifier +0x414` 한 곳이다 (`refs`) `[정적]`.
- 둘째 묶음은 빈 항목을 만들어 `[RSP+0x60]` 이 가리키는 **다른 항목**을 복제한다. 그 칸은 `LdrpInitializeExecutionOptions` 의 출력 칸이고, 그 안에서 `LdrpInitializeApplicationVerifierPackage` 로 넘어간다 `[정적]`. 그래서 "verifier 공급자 모듈 항목 복제" 로 보이지만 `[추측]`.
- WOW64·패치·.NET 과는 관계없다. 조건은 이 값 하나다 `[정적]`.
- **test.exe 는 첫째 묶음만** `[정적: 조건상]`. 값이 정말 0 인지는 Q46.

### 2.7 핵심 ① dll1·dll2 를 들여오는 자리 — `+0x1aa4` 로 좁혀졌다

질문은 flow.md 2.6 의 `[공백]` "import DLL 이 메모리에 올라오는 순간" 이다. 한 단계씩 가 보자.

**출발점 — 알던 것**

- `[관찰]` (run8) `+0x1bb6` 에서 멈췄을 땐 이미 dll1·dll2 가 있었다. `LdrpLoadDll` 은 안 탔다.
- `[정적]` (run18) 앞쪽엔 올리는 호출이 없다.
- `[추측]` (flow.md 2.6) 그러니 `+0x12de` 뒤, `+0x1bb6` 앞.

**단계 1 — "자리" 가 맞나** `[정적]`

- 뒤쪽에서 callee 이름으로 보아 **다른 DLL 을 올리는 일로 이어질 수 있는 호출**(후보)은 **전부 `+0x17c1 ~ +0x1aed`** 에 있다.
- `+0x1bb6` 은 그 뒤 "켜기" 쪽이다.
- → flow.md 2.6 의 `[추측]` "`+0x12de` 뒤, `+0x1bb6` 앞" 은 **코드 자리로 맞다** `[정적]`.

**단계 2 — 후보를 조건으로 거르기** `[정적]` (callee 는 이름만)

| 후보 (call / ret) | 지도의 callee 이름 (일부) | 탄다 조건 | test.exe |
|---|---|---|---|
| `LdrpLoadWow64` `+0x17c1 / +0x17c6` | `LdrpLoadDll`, `LdrGetProcedureAddress` | `UseWOW64 != 0` | 아니오 |
| `LdrpCorInitialize` `+0x184d / +0x1852` | `LdrpLoadDll`, `LdrpGetProcedureAddress` | `UseCOR != 0` | 아니오 |
| `LdrpInitializeImportRedirection` `+0x195e / +0x1963` | `LdrpBuildImportRedirection`, `LdrpLoadDll` | 조건 없음 | 부름 / 올리는지 모름 |
| `LdrpInitializeKernel32Functions` `+0x199d / +0x19a2` | `LdrLoadDll` ×2, `LdrGetProcedureAddressForCaller` ×3 | `Subsystem` 2 또는 3 | 예 (kernel32 쪽, `[관찰]` run7) |
| `LdrpInitShimEngine` `+0x19ef / +0x19f4` | `LdrpLoadDll`, `LdrpLoadShimEngine` | shim 데이터가 있을 때 | 아니오 (shim 없으면) |
| `LdrpCorProcessImports` `+0x1a7f / +0x1a84` | `AVrfDllLoadNotification`, `LdrpSendDllNotifications` | IL-only .NET 일 때만 | 아니오 |
| **`LdrpMapAndSnapDependency` `+0x1aa4 / +0x1aa9`** | `LdrpGetImportDescriptorForSnap`, `LdrpLoadDependentModuleInternal`, `LdrpSnapModule`, `LdrpQueueWork`, `LdrpPrepareImportAddressTableForSnap` | IL-only 아님 | **예** |
| `LdrpPrepareModuleForExecution` `+0x1aed / +0x1af2` | `LdrpNotifyLoadOfGraph`, `LdrpCondenseGraph`, `LdrpInitializeGraphRecurse` | `+0x1aa4` 뒤 상태가 음수 아님 | 예 |

- 표는 run 문서 표를 줄인 것이다. callee 이름 전부는 run 문서.
- test.exe 칸은 "보통 x64 콘솔 exe" 가정이다. `UseWOW64`·`UseCOR`·`Subsystem` 실제 값은 Q44·Q46.

**단계 3 — 남은 넷 중 dll1·dll2 쪽은 어느 것인가**

test.exe 조건으로 남는 건 `+0x195e`, `+0x199d`, `+0x1aa4`, `+0x1aed` 넷이다. 하나씩:

- `+0x199d` (kernel32 준비): 부르는 조건이 `Subsystem` 뿐이고 **exe 의 import 목록은 안 본다** `[정적]`. 그러니 "exe 가 무엇을 import 하든 kernel32 는 들인다" 쪽이다. dll1 은 exe 의 import 다.
- `+0x195e` (import 리다이렉션): callee 에 `LdrpLoadDll` 이 있지만, 실제로 올리는지는 callee 안이다. 모른다.
- `+0x1aed` (실행 준비): callee 이름(`LdrpNotifyLoadOfGraph`, `LdrpCondenseGraph`, `LdrpInitializeGraphRecurse`, `LdrpAddNodeServiceTag`, 로더 락)에 **올리는 이름이 없다.** "연결 끝낸 뒤 실행 준비" 로 보인다 `[추측]`.
- **`+0x1aa4`**: callee 이름이 하나같이 import 쪽이다.
  - `GetImportDescriptor…` = exe 의 import 목록 꺼내기, `LoadDependentModule…` = 의존 모듈 올리기, `SnapModule` = 모듈 연결, `PrepareImportAddressTable…` = import 주소 표 준비 `[추측: 이름]`.
  - **새 말 — snap (연결)**: exe 의 import 주소 표(IAT) 칸마다 상대 DLL 함수의 실제 주소를 채워 넣는 일로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: **전기 연결.** 본 건물 벽 콘센트(IAT 칸)마다 옆 건물 전선을 꽂는다. 꽂기 전엔 콘센트가 비어 있다.

→ 그래서 **1순위 = `+0x1aa4 LdrpMapAndSnapDependency`** `[추측: 이름·callee 이름 기준]`. 자리 자체는 `[정적]` 이다.

**단계 4 — run8 관찰과 맞나**

- `+0x1aa4` 의 callee 에 **`LdrpLoadDll` 은 없고** `LdrpLoadDependentModuleInternal` 이 있다 `[정적]`.
- `[관찰]` run8 "dll1·dll2 는 `LdrpLoadDll` 을 안 탔다" 와 **어긋나지 않는다.**
- 대비해 보면 재밌다:
  - kernel32 는 관리실이 `LdrLoadDll` 로 **직접 주문**한다 (`[관찰]` run7).
  - dll1·dll2 는 exe 의 **import 목록을 따라** 들여오는 쪽으로 보인다 `[추측]`.

**단계 5 — 주의: 첫 인자는 exe 항목이 아니라 로드 문맥** `[정적]`

- `+0x1aa4` 의 `RCX = &[RSP+0x2b0]` = 2.6 의 **로드 문맥**(작업 지시서) 이다.
- 바로 앞 `+0x1a94` 에서 그 `+0x2d0` 플래그에 1 을 더 켠다.
- exe 항목(`LdrpImageEntry`)은 오히려 `+0x1aed`(`RCX`)와 `+0x1c94`(`RDX`)에 **직접** 간다.
- 그래도 둘은 이어져 있을 것이다 `[추측]`. 근거 셋:
  1. `LdrpMapAndSnapDependency +0x5e2` 가 `LdrpImageEntry` 를 읽는다 (`refs`) `[정적]`.
  2. 항목에 `LoadContext`(+0xb0, PDB) 칸이 있다 `[정적]`.
  3. 뒤에 `LdrpFreeLoadContextOfNode` 가 불린다 (`+0x1be2`) `[정적]`.
  - 확인은 정적 Q11·Q12·Q14, 동적 Q23 (`+0x1aa4` 에서 `r rcx` vs `poi(poi(ntdll+0x1d2828)+0xb0)`).
- 비유: 짐 들이기 담당에게 건네는 건 **명부 줄이 아니라 작업 지시서**다. 지시서와 명부 줄이 서로 묶여 있을 것으로 보인다.

**단계 6 — 아직 확인 아님**

- 정말 이 호출 안에서 올라오나 → **Q23** (동적): `lm m dll*` 를 `+0x1aa4` 직전(`ntdll+0x8dea8`)·ret `+0x1aa9`(`ntdll+0x8dead`)·ret `+0x1af2`(`ntdll+0x8def6`) 에서 비교. 정적은 **Q14** (이 함수 본문).
- 메인 스레드가 직접 들이나, 일꾼이 들이나 → 2.9.

**비유로 정리**

- 짐 들이기 후보 여덟 군데 중, 32비트 통로·.NET·점검·shim 전용을 빼니 넷이 남았다.
- 그중 kernel32 는 "가게라면 무조건" 이라 따로고, 실행 준비는 짐이 다 온 뒤의 일이다.
- 남은 **`+0x1aa4`** 가 "본 건물 주문서(import 목록)를 보고 짐을 들여 콘센트에 꽂는" 자리로 가장 유력하다.

### 2.8 핵심 ② 전원 켜기 — 세 자리 중 test.exe 는 `+0x1bb6` 하나

`LdrpInitializeProcess` 가 `LdrpInitializeGraphRecurse` 를 **직접** 부르는 곳은 셋이다. 첫 인자는 모두 **모듈 항목의 `DdagNode`** (2.6 의 연결 카드) 다 `[정적]`.

| 자리 (call / ret) | 갈림 조건 | 첫 인자 `RCX` | test.exe |
|---|---|---|---|
| `+0x16e6 / +0x16eb` | 묶음 2 안 (`AvrfAppVerifierMode != 0`) + `NtGlobalFlag & 0x100` 이 0 + `AvrfAppVerifierMode & 2` 가 0 | 복제 항목의 `DdagNode` (`[RBX+0x98]`) | 아니오 |
| `+0x193c / +0x1941` | `[RSP+0x178] != 0` — `UseCOR` 일 때 `LdrpCorInitialize` 가 채우는 칸 | .NET 쪽 항목의 `DdagNode` | 아니오 |
| **`+0x1bb6 / +0x1bbb`** | **조건 없음** (`+0x1b8c` 일감 비우기, `+0x1b91` 로더 락 뒤) | **exe 항목의 `DdagNode`** (`[LdrpImageEntry+0x98]`, `+0x1baf`) | **예 — 이 길 하나** |

**왜 `+0x1bb6` 하나인가 — 단계별로**

1. `+0x16e6` 은 묶음 2 **안**에 있다. 묶음 2 는 `AvrfAppVerifierMode != 0` 이어야 들어간다 (2.6 끝). 점검관이 안 오면 들어갈 수조차 없다.
2. `+0x193c` 는 `[RSP+0x178]` 칸이 0 이 아니어야 한다. 그 칸은 `UseCOR` 일 때 `LdrpCorInitialize(&[RSP+0x178])` 가 채우고, 아니면 앞쪽에서 0 으로 시작한다 (결과서). .NET 이 아니면 0 이다.
3. `+0x1bb6` 은 조건이 없다.

→ test.exe 는 **`+0x1bb6` 하나만** 탄다 `[정적: 조건상]`.

- "조건상" 이 붙는 이유: `AvrfAppVerifierMode`·`UseCOR` 가 **실행 때 값**이라서다. 값 확인은 Q46 (그리고 `+0x1bb6` 에서 `r rcx` 가 `poi(poi(ntdll+0x1d2828)+0x98)` 인지).

**"맨 바깥 = test.exe" 가 코드로 뒷받침됐다**

- flow.md 2.7 은 재귀 단계를 "맨 바깥이 test.exe, 그 안이 dll1, 가장 안쪽이 dll2" 로 그렸다 `[추측]`.
- 이번에 맨 바깥 호출의 첫 인자가 **exe 항목의 `DdagNode`** 인 걸 봤다 `[정적]`.
- 그러니 "맨 바깥 = test.exe" 는 코드로 뒷받침된다. **안쪽 단계 ↔ dll1·dll2 대응은 여전히 `[추측]`** (Q24).
- 비유: 전원 켜기 담당에게 **본 건물의 연결 카드**를 건넨다. 담당은 카드에 그려진 선을 따라 가장 끝 건물(dll2)부터 켜고 돌아오는 것으로 보인다 (순서 dll2 → dll1 은 `[관찰]`, "선을 따라" 는 `[추측]`).

**앞뒤에 하는 일** `[정적]`

```
+0x1b83 전역 LdrpProcessInitContextRecord = CONTEXT → +0x1b8c 일감 비우기 → +0x1b91 로더 락 잡기
 → +0x1bb6 그래프 초기화 → +0x1bc7 락 풀기 → +0x1be2 LdrpFreeLoadContextOfNode(exe DdagNode, 상태 칸) → +0x1bec 전역 = 0
```

- **로더 락** = 이름 그대로 로더가 쓰는 [잠금](run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다) 이다 `[추측: 이름]`. 비유: 관리실 **열쇠**. 전원 켜는 동안 이 열쇠를 쥐고 있다.
- 실패면 `+0x1c0e` 에서 로그 후 반환 (2.14).

**CONTEXT 를 곁에 두고 전원을 켠다** `[정적]`

- `+0x1b83` 에서 전역 `LdrpProcessInitContextRecord` 에 **CONTEXT 레코드**(2.2)를 넣고, 끝나면 `+0x1bec` 에서 0 으로 지운다.
- 그 전역을 읽는 곳은 **`LdrpInitializeNode +0x17a` 하나**다 (`refs`). `Flags.ProcessStaticImport` 모듈에서 `LdrpCallInitRoutine` 에 넘기는 문맥이다 (run 문서 Q15 줄).
- → **import DLL 초기화는 첫 스레드의 CONTEXT 를 곁에 두고 돈다** `[정적]`. 그걸 무엇에 쓰는지는 Q15.
- 비유: 전원 켜는 동안만 **사물함 메모를 벽 게시판에 붙여 두었다가**, 끝나면 뗀다.

**헷갈리지 말 것 — 다른 자리가 하나 더 있다**

- `+0x1aed LdrpPrepareModuleForExecution` 의 callee 에도 `LdrpInitializeGraphRecurse` 가 있다 (`+0xea`, 지도) `[정적]`.
- "test.exe 는 하나" 는 `LdrpInitializeProcess` 가 **직접 부르는 세 자리** 중의 이야기다.
- dll1·dll2 의 `LdrpInitializeNode` 는 run7 콜스택상 `+0x1bbb` 아래였다 `[관찰]`. 두 자리가 일을 어떻게 나누는지는 정적 Q15, 동적 Q24.

### 2.9 핵심 ③ 일꾼이 생길 수 있는 가장 이른 자리 — `+0x1a42`

**출발점 — 알던 것**

- `[관찰]` (run8) ntdll 일꾼 2개(`ntdll!TppWorkerThread`)가 BEFORE-GRAPH 직후 유저 쪽에 도착한다.
- `[정적]` (run18) 앞쪽 `+0xb83` 에 `TpInitializePackage`(일꾼 대기실), 뒤쪽 첫 호출 `+0x12de` 에 `LdrpInitParallelLoadingSupport`.
- `[추측]` (flow.md 3장) 일꾼은 `+0x12de` 준비 뒤에 만들어진다.

**새 말 둘**

- **병렬 로딩** = DLL 여러 개를 여러 스레드가 나눠서 올리는 방식으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). flow.md 3장 `[추측]` "DLL 여러 개를 동시에 불러오기 위한 도우미" 의 그 일이다.
- **스레드 풀과 일감(work)** = 일꾼 스레드들을 모아 두고, 일감이 들어오면 나눠 맡기는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 일감 = 일꾼에게 맡기는 일 한 건.
  - 비유: 대기실(run18)에 **일꾼을 실제로 부르는 것** = 풀 만들기, **게시판에 일감 쪽지 붙이기** = `LdrpQueueWork`, **게시판 비우기** = `LdrpDrainWorkQueue`.

**순서** `[정적]` (run 문서 병렬 로딩 표)

| 순서 | 자리 (call / ret) | 하는 일 |
|---|---|---|
| 1 | `LdrpInitParallelLoadingSupport` `+0x12de / +0x12e3` | callee `RtlInitializeCriticalSectionEx`, `LdrpCreateLoaderEvents` → 임계 구역·이벤트. **스레드 만드는 이름 없음** |
| 2 | `LdrpDrainWorkQueue(0)` `+0x1302 / +0x1307` | 일감 비우기 |
| 3 | `LdrpDrainWorkQueue(0)` `+0x19c1 / +0x19c6` | kernel32 준비 뒤 |
| 4 | **`LdrpEnableParallelLoading(ECX)` `+0x1a42 / +0x1a47`** | callee `TpAllocPoolInternal`, `TpSetPoolWorkerThreadIdleTimeout`, `TpSetPoolMaxThreads`, `TpAllocWork`, `LdrpDetectDetour`, `RtlGetSuiteMask` → **스레드 풀과 일감을 만든다** |
| 5 | `LdrpMapAndSnapDependency` `+0x1aa4` → `LdrpDrainWorkQueue(1)` `+0x1aac / +0x1ab1` | 앞의 callee 에 `LdrpQueueWork`, 뒤의 callee 에 `LdrpProcessWork`, `NtWaitForSingleObject` |
| 6 | `LdrpDrainWorkQueue(0)` `+0x1b8c / +0x1b91` | 그래프 초기화 직전 |

**왜 `+0x1a42` 인가 — 단계별로**

1. `+0x12de` 는 callee 이름이 **열쇠(임계 구역)·신호등(이벤트)** 뿐이다. 스레드를 만드는 이름이 없다 `[정적]`.
   - 그래서 run18 의 `[추측]` "`+0x12de` 뒤" 는 **더 좁혀진다.**
2. `+0x1302`·`+0x19c1` 일감 비우기는 풀이 아직 없을 때라, "빈 게시판 확인" 정도일 것 `[추측]` (결과서).
3. `+0x1a42` 의 callee 에 **`TpAllocPoolInternal`, `TpSetPoolMaxThreads`, `TpAllocWork`** 가 있다 → 여기서 스레드 풀과 일감을 만든다 `[정적]`.
4. 그 앞엔 풀을 만드는 호출이 없다.
5. → **일꾼 스레드가 생길 수 있는 가장 이른 자리 (이 함수 안) = `+0x1a42` 뒤** `[정적: 자리]`.

**`+0x1a42` 에 넘기는 값** `[정적]`

- `ECX = ProcessParameters.LoaderThreads` (+0x40c, PDB).
  - 이 칸엔 IFEO 값 `MaxLoaderThreads` 를 `LdrpInitializeExecutionOptions` 가 써 둔다 (결과서).
  - `PEB.AppCompatFlags` 비트 28 이 켜져 있으면 1 로 고정 (`+0x1a26 ~ +0x1a39`).
- 비유: **특별 지시서(IFEO)** 에 적힌 "일꾼 몇 명까지" 를 들고 일꾼을 부른다.

**아직 모르는 것 하나**

- 풀을 만들 때 **바로** 스레드가 생기는지, 일감(`+0x1aa4` 안 `LdrpQueueWork`)이 **올라올 때** 생기는지는 callee 안이라 정적으로 못 정한다 → **Q47** (동적: `+0x1a42` 에서 `r ecx`, 이어 `+0x1a47`·`+0x1aa9`·`+0x1ab1`·`+0x1b91` 에서 스레드 수).

**맞춰 보기**

- run8 `[관찰]` "BEFORE-GRAPH(`+0x1bb6`) 무렵 도착" 과 어긋나지 않는다. `+0x1a42` 가 그보다 앞이다.
- kernel32 준비(`+0x199d`)는 `+0x1a42` **앞**이다. 그러니 kernel32·kernelbase 는 일꾼 없이 메인 스레드 혼자 들일 것 `[추측]` (kernel32 준비는 `LdrLoadDll` 직접 호출, run7 `[관찰]`).
- 그리고 2.7 의 남은 질문 "dll1·dll2 를 **누가** 들이나" 는 여기에 달렸다. 풀은 바로 앞에서 만들고, 일감은 `+0x1aa4` 안에서 붙일 수 있다. 메인 스레드가 직접 들이는지, 일꾼이 들이는지는 `LdrpQueueWork` 에 달렸다 (flow.md 2.6) → Q23·Q47.

**비유로 정리**

- 일꾼 **대기실**은 앞쪽(`+0xb83`)에 만들었다.
- **연락망**(신호등·열쇠)은 뒤쪽 첫 줄(`+0x12de`)에 깔았다.
- 실제로 **일꾼을 부르는 건** kernel32 를 들인 다음, 짐(dll1·dll2)을 들이기 **바로 전**(`+0x1a42`)이다.

### 2.10 간접 호출 4곳 — 칸에서 주소를 꺼내 부르기

[간접 호출](run9-create-user-process-해설.md#직접-호출과-간접-호출) 은 부를 곳을 코드에 박지 않고 **칸에서 꺼내** 부르는 호출이다. 그래서 정적으로는 "**어느 칸에서 꺼내나**" 까지만 보인다.

| 자리 (call / ret) | 꺼내는 칸 | 조건 | test.exe |
|---|---|---|---|
| `+0x17fc / +0x17fe` | 전역 `g_LdrpWow64LdrpInitialize` (인자 CONTEXT) | `UseWOW64 != 0` | 아니오 |
| `+0x1b59 / +0x1b5e` | 전역 **`Kernel32ThreadInitThunkFunction`** (인자 `1,0,0`) | 값이 0 이 아닐 때 | 예 (kernel32 준비가 됐으면) |
| `+0x1cdf / +0x1ce4` | 전역 `g_pfnSE_InstallAfterInit` 을 쿠키로 푼 값 | `g_ShimsEnabled != 0` | 아니오 (shim 없으면) |
| `+0x1d16 / +0x1d1b` | **`PEB.PostProcessInitRoutine`** (`[RDI+0x230]`, PDB 오프셋 560) | 값이 0 이 아닐 때 | 값을 누가 넣는지 이 함수 안에 없음 |

모두 `[정적]`. 하나씩:

**`+0x1b59` — kernel32 의 "출근 인사" 를 미리 한 번**

1. 칸을 **채우는 곳**: `LdrpInitializeKernel32Functions +0xe4` (칸 주소를 넘김, 묶음 5 에서) `[정적]`.
2. 칸을 **읽는 곳**: 여기, 그리고 **`RtlUserThreadStart +0x7`** (`refs`) `[정적]`.
3. 그러니 이 호출은 **새 스레드가 시작할 때 `RtlUserThreadStart` 가 부르는 그 함수를, 프로세스 초기화 때 `(1,0,0)` 으로 한 번 부르는 것**이다 `[정적]`.
4. 대상은 `KERNEL32!BaseThreadInitThunk` 일 것 `[추측]`.
   - 이유: flow.md 2.10 의 `[추측]` 근거에 "Worker 스레드 종료 때 콜스택에 `RtlUserThreadStart+0x2c` → `BaseThreadInitThunk+0x20` 이 보였다 (메인 스레드에선 직접 안 봄)" 가 있다. 같은 칸을 읽는 `RtlUserThreadStart` 가 부르는 함수이니 그것일 것이다.
5. kernel32 준비가 안 됐으면(`Subsystem` 이 GUI·콘솔이 아님) 칸이 0 이라 건너뛴다. 음수면 반환 `[정적]`.

- `1, 0, 0` 이 무슨 뜻인지는 근거에 없다.
- 확인은 **Q48** (`+0x1b59` 에서 `ln @rax`).
- 비유: [본업 자리](run16-thunk-context-해설.md#23-비유-하나로-처음부터-끝까지)(`RtlUserThreadStart`)가 직원마다 거쳐 가게 하는 "kernel32 출근 인사" 를, 세팅 담당이 개업 전에 **한 번 미리** 부른다.

**`+0x17fc` — WOW64 통로의 문**

- `+0x17ed` 에서 `RAX = g_LdrpWow64LdrpInitialize`, `+0x17f4` 에서 `RCX` = CONTEXT 레코드 → `CALL RAX` `[정적]`. 칸을 채우는 곳은 `LdrpLoadWow64 +0xe8` (run17).
- 대상 이름은 `[추측: wow64 쪽 시작 함수]`.
- 주의: `CALL RAX` 는 **2바이트**라 ret 이 `+0x17fe` 다. 다른 간접 호출(5바이트)과 계산이 다르다 (run 문서 도구 메모, plan 하네스 후보 (19)).

**`+0x1cdf` — 쿠키로 풀어서 부르기**

- **새 말 — 쿠키로 풀기**: 함수 주소를 칸에 그대로 두지 않고 **비밀 값과 섞어** 두었다가, 쓸 때 풀어서 쓰는 방식으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 이번 코드: `KUSER_SHARED_DATA.Cookie`(+0x330, PDB) 의 낮은 6비트만큼 회전 후 XOR (`+0x1cb0 ~ +0x1cd1`, 결과서) `[정적]`.
  - 비유: 금고 번호를 **암호로 적어 둔 쪽지.** 쓸 때만 풀어 읽는다.
- 칸을 쓰는 곳은 `LdrpGetShimEngineInterface +0x4d9` 하나 (`refs`). shim 이 켜졌을 때만이라 test.exe 는 안 탈 것.

**`+0x1d16` — `PEB.PostProcessInitRoutine`**

- `RDI` = PEB (앞쪽 `+0x14c` 부터 안 바뀜), `+0x230` = PDB `_PEB.PostProcessInitRoutine` `[정적]`.
- 값이 있을 때만 부르고, 인자는 따로 안 채운다.
- 그 칸에 **누가 값을 넣는지는 이 함수 안에 없다** → Q48 (`dq @$peb+0x230 L1`, 0 이면 안 부름).
- flow.md 2.9 의 정리: exe TLS 와 `main` 사이에 유저 코드를 부를 수 있는 자리는 **이 함수 안에선 이것 하나**다 `[정적]`.
- 비유: 안내판의 "**세팅 끝나면 부를 사람**" 칸. 적혀 있으면 부르고, 비어 있으면 그냥 간다.

### 2.11 exe TLS 콜백 — 전원 다 켠 뒤에

**새 말 — TLS 콜백**: 모듈이 "스레드·프로세스가 붙을 때 먼저 불러 달라" 고 적어 두는 함수로 알려져 있다 `[추측]` (일반적으로 알려진 설명). test.exe 에는 exe TLS 콜백이 있다 `[소스]`.

**틀** `[정적]`

1. `+0x1c31`: exe 항목의 `TlsIndex`(+0x6e, PDB) 가 **0 이 아닐 때만** 들어간다.
2. `+0x1c85 / +0x1c8a`: 항목의 `EntryPointActivationContext`(+0x88) 를 **켠다**.
3. **`+0x1c94 / +0x1c99` `LdrpCallTlsInitializers(ECX=1, RDX=LdrpImageEntry)`**.
   - `ECX = 1` = `DLL_PROCESS_ATTACH` (결과서), 곧 "프로세스에 처음 붙음" (flow.md 2.9).
   - `RDX` = 2.6 의 exe 항목. 벽 주소록(`LdrpImageEntry`)에서 꺼낸다.
4. `+0x1ca2 / +0x1ca7`: 활성화 컨텍스트를 **끈다**.

- **새 말 — 활성화 컨텍스트**: 모듈마다 "어떤 버전의 부품(DLL)을 쓸지" 적어 둔 설정을 잠깐 켜 두는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 본 건물 **전용 작업 지침서**를 펴 놓고 TLS 콜백을 부르고, 끝나면 덮는다.

**예외가 나면** — 예외 정리 조각 `LdrpInitializeProcess$fin$0`

- 하는 일은 `RtlDeactivateActivationContextUnsafeFast` 하나뿐이다 (ret `$fin$0+0x15`) `[정적]`.
- TLS 콜백에서 예외가 빠져나올 때 **같은 프레임을 끄는 정리**로 보인다 `[추측: 이름·오프셋 일치]`. 정상 길에선 4번이 같은 일을 한다.
- [예외 보호 구역](run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일) 의 비유로: 비상구로 나가더라도 **지침서는 덮고 나간다.**
- 같은 틀이 `LdrpInitializeNode +0x145 ~ +0x166` 에도 있다 (그쪽은 DLL 마다) `[정적]`.

**순서** `[정적]`

- exe TLS 는 import DLL 초기화(`+0x1bb6`)가 끝나고 로더 락을 푼 **뒤**다. run7·run8 `[관찰]` "DLL 초기화가 전부 끝난 뒤" 와 같다.
- 비유: 옆 건물들 전원을 다 켠 다음에야 **본 건물 직원 첫 안내**(TLS 콜백)를 한다.

**test.exe 는 조건을 통과했나**

- sample 에 exe TLS 콜백이 있고 `[소스]`, run7·run8 에서 `+0x1c99` 가 실제로 불렸다 `[관찰]`. 그러니 `TlsIndex ≠ 0` 조건을 통과했을 것 `[추측]`. 값 확인은 Q46 (`+0x1c31` 에서 `dw poi(ntdll+0x1d2828)+0x6e L1`).
- `TlsIndex` 를 **누가** 항목에 쓰나 → 묶음 4 의 `+0x18cb LdrpInitializeTls` 쪽으로 보인다 `[추측]` (Q16).

### 2.12 팻말 — `LdrInitState` 1 → 2 (→ 3)

[run17 해설 2.8](run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일) 표에서 `_LdrpInitialize` 가 이 함수 **앞**에 `LdrInitState = 0` 을, **뒤**(`+0x205`)에 `= 3` 을 쓰는 걸 봤다 ("세팅 중" 팻말). 이번엔 그 **사이**를 봤다.

**쓰기 3곳** `[정적]` (뜻은 공개 문서 기준 `[추측]`)

| 자리 | 값 | 언제 | 뜻 `[추측]` |
|---|---|---|---|
| `+0x1a4c` | 1 | `LdrpEnableParallelLoading` 바로 뒤 | import 올리는 중 |
| `+0x1b1e` | 2 | `+0x1aed` 뒤 상태가 음수 아님을 확인한 다음 | import 올림 끝 |
| `+0x17dd` | 3 | WOW64 길만, `g_LdrpWow64LdrpInitialize` 부르기 직전 | 초기화 끝 |

- 네이티브 길의 3 은 이 함수 **밖** `_LdrpInitialize +0x205` 에서 쓴다 (돌아간 뒤) `[정적]`.
- 그러니 test.exe 길에서 팻말은 **0 → 1 → 2 → (돌아가서) 3** 이 된다 `[정적: 조건상]`.
- kernel32 준비(`+0x199d`)는 **0 일 때**다 (`+0x1a4c` 전).

**왜 신경 쓰나**

- 이 값을 **읽는** 함수들이 상태에 따라 다르게 움직인다 `[정적]`: `LdrpAllocateModuleEntry +0xc2` (1 과 비교), `LdrpLoadDllInternal +0x29b` (0 과 비교), `LdrpLoadShimEngine +0x111` (2 와 비교).
- 즉 같은 "DLL 올리기" 함수라도 **팻말이 몇일 때 불렸나**에 따라 길이 갈릴 수 있다. 자세히는 Q10·Q11.

- 비유: 가게 문 팻말 "**세팅 중(0)**" → "**짐 들이는 중(1)**" → "**짐 다 들임(2)**" → "**세팅 끝(3)**". 숫자와 자리는 코드로 봤고, 팻말 글귀는 공개 문서에서 빌린 짐작이다.

### 2.13 디버거 때문에 갈리는 길 (CLAUDE.md "따로 적기")

**`PEB.BeingDebugged`(PEB+0x2) 를 읽는 곳 두 곳** `[정적]` — 둘 다 `LdrpDoDebuggerBreak` 로 간다

| 읽는 곳 | 호출 (call / ret) | 어느 길 | 그 뒤 |
|---|---|---|---|
| `+0x17d2` | `+0x17d8 / +0x17dd` | **WOW64 길 안**에서만 | `LdrInitState = 3`, `PEB.CrossProcessFlags` 의 `ProcessInitializing` 비트 끄기 (`+0x17e7`) |
| **`+0x1b35`** | **`+0x1b3b / +0x1b40`** | **네이티브 길** — `LdrpInitializePatchData`(`+0x1b24`) 성공 뒤 | 묶음 7 계속 |

- 이름으로 보면 `+0x1b3b` 는 "**초기 중단점**" 이다 `[추측]`. 유저 디버거가 붙은 프로세스가 처음 멈추는 곳이라는 뜻이다 (run 문서).
- 네이티브 길에서 `ProcessInitializing` 비트를 끄는 줄은 이 함수 안에 **없다** `[정적]`. run17 해설 2.8 표 8번처럼 돌아간 뒤 `_LdrpInitialize` 가 끈다.
- **커널 디버거만** 붙었을 때 `BeingDebugged` 가 켜지는지는 PEB 를 만드는 커널 쪽 값이라 못 정한다 `[추측: 안 켜짐]` → Q46 (`+0x1b35` 에서 `db @$peb+2 L1`).

**run18 이 넘긴 `KUSER_SHARED_DATA+0x330` — 디버거 표시가 아니었다** `[정적]`

- run18 해설 2.11 은 뒤쪽 `+0x188c`·`+0x1cb0` 의 `KUSER_SHARED_DATA` 읽기를 "디버거 쪽으로 볼 곳" 으로 넘겼다.
- 실제로 `+0x330` 은 PDB `_KUSER_SHARED_DATA.Cookie` 다. 두 곳 다 **포인터 풀기**(2.10 의 쿠키)다:
  - `+0x188c`: .NET 첫 스레드에서 `LdrpCorExeMainRoutine` 풀기 (2.2)
  - `+0x1cb0`: shim 의 `g_pfnSE_InstallAfterInit` 풀기
- 둘 다 test.exe 는 안 탈 것 `[추측]`.

**`PEB.NtGlobalFlag & 0x100`** (`+0x16b7`) `[정적]`

- AppVerifier 묶음(묶음 2) **안에서만** 본다. 켜져 있으면 그래프 초기화 대신 `AVrfInitializeVerifier`(`+0x1713 / +0x1718`).
- 비트 이름은 `FLG_APPLICATION_VERIFIER` `[추측: 공개 문서]`.

**그래서**

- 뒤쪽에서 디버거 부착으로 **직접** 갈리는 곳은 `BeingDebugged` 두 자리뿐이고, 네이티브 길은 **`+0x1b35` 하나**다 `[정적]`.
- 비유: 점검원(디버거)이 와 있으면 "잠깐 멈춤" 버튼을 누르는 자리가 이 통로엔 하나 있다. 우리가 붙인 커널 디버거가 그 점검원으로 쳐지는지는 버스를 타 봐야 안다.

### 2.14 끝 — 정상 반환과 실패 길

**TLS 뒤 순서** `[정적]`

1. `+0x1ca7` `g_ShimsEnabled` 검사 → (켜졌으면) `+0x1cdf` shim 후처리 → 실패면 `+0x1cf9 / +0x1cfe LdrUnloadDll`(shim 엔진)
2. `+0x1d16` `PostProcessInitRoutine` (있으면)
3. `+0x1d28`·`+0x1d37` `NtClose` 2 (IFEO 쪽 핸들, 0 이면 건너뜀)
4. `+0x1d3c ~ +0x1d94` CPUID 로 `x86_cpu_enable_ssse3`·`_simd` → `+0x1d99 / +0x1d9e RtlIsProcessorFeaturePresent(0x29)` → `x86_cpu_enable_avx512` (비트·기능 번호 41 이름은 `[추측]`)
5. `+0x1def / +0x1df4 SymCryptDetectCpuFeaturesByCpuid` (`g_SymCryptFlags` 비트 0 이 꺼졌을 때만)
6. **정상 반환 `+0x1e2d XOR EAX,EAX ; JMP +0xa7b`** → 공용 [에필로그](run18-proc-init-setup-해설.md#210-일찍-끝나는-길--19--5)(뒷문) → `_LdrpInitialize +0x16c`

- 비유: 문 닫고(핸들), 가게 기계 성능표(CPU 기능)를 적고, **뒷문으로 나가** 세팅 담당 자리로 돌아간다.

**함수 끝에 있지만 뒤쪽 길이 아닌 것** `[정적]`

- `+0x1e40 RtlRaiseStatus(0x80000002)`·`+0x1e34 __fastfail(3)` 은 뒤쪽 흐름에선 **안 탄다.**
- 거기로 뛰는 점프는 앞쪽 `+0x784`, `+0xb22`, `+0x1299`, `+0x12c6` 뿐이다 (run18 "강제 종료 5곳").
- 호출 75곳에는 세지만, **놓인 자리만 뒤쪽**이다. [오프셋 순서 ≠ 실행 순서](run9-create-user-process-해설.md#37-오프셋이-큰데-먼저-실행되는-줄--오프셋-순서--실행-순서) 의 또 한 예다.

**뒤쪽 실패 길 20개** `[정적]` — 모두 에필로그 `+0xa7b` 로 상태 반환

| 종류 | 개수 | 예 |
|---|---|---|
| 로그 후 반환 | 7 | `+0x12e9`(병렬 로딩 지원), `+0x18d8`(TLS 준비), `+0x1947`(.NET 그래프), `+0x1969`(import 리다이렉션), `+0x19a8`(kernel32 준비), `+0x1b02`(올리기·실행 준비), `+0x1c0e`(exe 그래프 초기화) |
| `0xc0000017` | 2 | `+0x1361`(exe 항목 할당 실패), `+0x1582`(복제 항목, AppVerifier 만) |
| 상태 그대로 | 11 | `+0x143b`, `+0x14f4`, `+0x165a`, `+0x1684`, `+0x174e`, `+0x17cc`(WOW64), `+0x1858`, `+0x1872`(.NET), `+0x1b2f`, `+0x1b64`(kernel32 thunk), `+0x1b7d` |

- AppVerifier 전용 4, WOW64 1, .NET 3 을 빼면 **test.exe 가 갈 수 있는 건 12개**다.
- 대표: `+0x1361` (exe 항목 할당 실패), **`+0x1b02` (import 올리기 실패 — 예: dll 없음)**, `+0x1c0e` (DLL 초기화 실패).
  - `+0x1b02` 는 2.7 의 "짐 들이기" 바로 뒤다. dll1 이 없으면 여기서 접는 그림이다 (예시는 run 문서).
- 돌아간 뒤엔 [run17 해설 2.8](run17-ldr-init-entry-해설.md#28-첫-스레드-길--ldrpinitializeprocess-앞뒤로-하는-일) 의 "세팅이 실패하면 가게 문을 닫는다" 로 이어진다.
- `+0x1522` 현재 폴더 초기화 실패만 실패 길이 **아니다** (2.6 8번).

### 2.15 그 밖에 확인된 것

- **버전 대조 일치** `[정적]` — `_calls.tsv` 의 `+0x1bb6 → ret +0x1bbb`, `+0x199d → ret +0x19a2`, `+0x1c94 → ret +0x1c99`, `_LdrpInitialize +0x167 → ret +0x16c` 가 run7·run8 콜스택 숫자와 같다.
- **앞쪽 레지스터 주의** `[정적]` — 앞쪽 `+0x5bb` 부터 `RSI`·`R14` 는 로그 문자열·로그 문맥 주소다. ntdll 시작 주소가 아니다. ntdll 시작 주소는 `[RSP+0xd8]` 에 있다 (결과서).
  - [run18 해설 2.5](run18-proc-init-setup-해설.md#25-입구의-두-짐--rdx-는-쓰고-rcx-는-보관만) 의 "`+0x2d MOV RSI,RDX` (손에 쥠)" 은 맞다. 다만 그 손(`RSI`)은 `+0x5bb` 부터 다른 걸 쥔다. 서랍(`[RSP+0xd8]`) 쪽이 끝까지 간다.
- **뒤쪽에서 쓰는 앞쪽 값** `[정적]` — `RDI` = PEB, `R13` = exe NT 헤더(`LdrpAppHeaders`), `R12B` = .NET IL-only 표시, `[RSP+0xd8]` = ntdll 시작 주소, `[RSP+0xe0]` = CONTEXT 레코드.
- **범위를 딱 한 곳 넓혔다** — `[RSP+0x60]` 의 출처 하나만 `LdrpInitializeExecutionOptions.asm` (`+0x73d ~ +0x75e`) 에서 봤다 (2.6 끝).
- **바퀴 중간에 한 번 끊겼다** — sast/14 decompiler 가 사용량 한도(429)로 중간에 멈춰, 같은 에이전트를 이어 받아 끝냈다. 실패 횟수로는 안 셌다 (plan 결정 기록).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만.

| 자리 | 전 (run18 뒤) | 후 (run19) |
|---|---|---|
| 기준 | ntdll.pdb (run17, run18), 정적 근거 run9 ~ run18 | ntdll.pdb (run17, run18, **run19**), 정적 근거 **run9 ~ run19** |
| 두 그림 사이 설명 | 앞쪽 절반만 훑음, 뒤쪽은 아직 | **앞·뒤 모두** 훑음. `(0)` 상자 = 앞쪽 + 뒤쪽 첫 묶음(exe 항목). `(1)` 과 `(2)` 사이에 `(정적)` 상자 셋. 간접 호출 둘은 회색 상자 |
| 그림 읽는 법 | ntdll 상자 안 `(0)` `(1)` `(2)` `(3)` | `(0)` `(1)` **`(1)~(2) 사이`** `(2)` `(3)` |
| 그림 2 `c4` 상자 | "+0x1c1 PEB.Ldr" | + "**+0x1374 에서 전역 LdrpImageEntry = exe 항목**" |
| `(0) 프로세스 준비` 상자 | 앞쪽만 ("뒤쪽은 아직 안 봄") | + `p6` `LdrpAllocateModuleEntry` (exe 모듈 항목, 조건 없음, `BaseDllName` = test.exe) |
| 새 묶음 상자 `(1)~(2) 사이` | — | `j1` `LdrpEnableParallelLoading` / `j2` `LdrpMapAndSnapDependency` / `j3` `LdrpPrepareModuleForExecution` |
| 새 회색 상자 "모듈 모름 — 간접 호출" | — | `z1` `Kernel32ThreadInitThunkFunction` 칸의 함수 / `z2` `PEB.PostProcessInitRoutine` 칸의 함수 (회색 점선 = 대상이 실행 때 정해짐) |
| `g1`·`w1`·`x1` 상자 | 이름만 | + 정적 메모 (첫 인자 = exe `DdagNode` / 일꾼 첫 자리는 `LdrpEnableParallelLoading` 뒤 / `TlsIndex ≠ 0` 일 때만) |
| 화살표 번호 | `(6) +0x19a2`, `(7) +0x1bbb`, `(8) +0x1c99` | **`(6) +0x135e`**, `(7) +0x19a2`, **`(8) +0x1a47`, `(9) +0x1aa9`, `(10) +0x1af2`, `(11) +0x1b5e`**, `(12) +0x1bbb`, `(13) +0x1c99`, **`(14) +0x1d1b`** |
| 2.4 `_LdrpInitialize` 디버거 길 | "같은 호출 2곳은 뒤쪽이라 Q9b" | + run19: `+0x17d8` 은 WOW64 길만, **`+0x1b3b` 가 네이티브 길** |
| 2.4 `LdrpInitializeProcess` 절 | 뒤쪽 `[공백: Q9b]` + `[관찰]` 3줄 | 뒤쪽 트리 `[뒤1]~[뒤10]`, 둘째 10묶음 표, `[정적]` 줄 여러 개 (exe 항목, AppVerifier, 그래프 세 자리, 올리기 후보, 병렬 로딩, 간접 4곳, `LdrInitState`, 끝, 실패 길 20, 디버거 길) |
| 2.4 입구 인자 | "`RCX`(로더 문맥)" | ~~로더 문맥~~ → **CONTEXT 레코드** (`+0x18b4`, `_CONTEXT.Rcx` 칸) |
| 2.4 앞쪽 표 묶음 10 | "exe 폴더" | "exe 파일 이름 잘라 두기 (`[RSP+0x130]` → 뒤쪽 `BaseDllName`. ~~exe 폴더~~ run19 정정)" |
| 2.5 | `[추측]` kernel32 는 스스로·뒤쪽에서 | + `[정적]` 부르는 조건 = `Subsystem` 2/3 (import 목록 안 봄), 부르는 때 = 병렬 로딩 **앞**(`LdrInitState` 0), `Kernel32ThreadInitThunkFunction` 칸을 채움, `+0x19a2` 앞 후보는 셋뿐 |
| 2.6 | `[추측]` `+0x12de` 뒤 `+0x1bb6` 앞 | `[정적]` **자리는 맞다**, 후보 1순위 `+0x1aa4`, 첫 인자 = 로드 문맥, 메인 스레드냐 일꾼이냐는 `LdrpQueueWork` 에 달림 |
| 2.7 | `[추측]` 맨 바깥 = test.exe | `[정적]` 첫 인자 = exe `DdagNode` 로 뒷받침, 세 자리 중 `+0x1bb6`, CONTEXT 를 곁에 두고 돎 |
| 2.9 | `[관찰]` 이 함수가 직접 부름 | + `[정적]` 같은 코드, `TlsIndex` 조건, 활성화 컨텍스트 틀, TLS 뒤에 하는 일 (`PostProcessInitRoutine` 하나) |
| 2.10 | — | + `[정적]` (`refs` 만) `RtlUserThreadStart +0x7` 이 `Kernel32ThreadInitThunkFunction` 을 읽음 |
| 3장 일꾼 | `[추측]` `+0x12de` 준비 뒤 | `[정적]` **좁혀짐**: `+0x12de` 는 이벤트·임계 구역뿐, 풀 만드는 첫 자리 `+0x1a42` |
| 4장 공백 표 | 이 함수 순서 "(일부 해결)" 등 | **해결됨**: 이 함수 순서, `[RSP+0x130]` 정정. 새 줄: 뒤쪽 실행 값 (Q46), 간접 호출 대상 (Q48), 이름·뜻 못 정한 것 (run19), 일꾼 첫 시점 (Q47), 2.7 두 자리 역할 (Q15·Q24), 2.9 `TlsIndex` (Q16) |
| 4.1 | — | 새 줄: `(0)` 의 `(6)`, `(1)~(2) 사이`, 회색 상자 |
| 5장 | run18 까지 | run19 줄, 요청서 번호 표 run19 줄 |

태그로 보면 핵심 변화는 다섯이다:

- **`LdrpInitializeProcess` 가 하는 일**: 앞쪽 `[정적]` + 뒤쪽 `[공백]` → **전부 `[정적]`**. 실행 순서로 맞춰 보는 건 Q44·Q46.
- **2.6 dll1·dll2 올리는 곳**: `[추측]` "어디쯤" → **자리 `[정적]`**, 정확한 호출은 1순위 `+0x1aa4` (`[추측: 이름]`).
- **2.7 맨 바깥 = test.exe**: `[추측]` → **`[정적]` 근거** (첫 인자 exe `DdagNode`). 안쪽 단계는 여전히 `[추측]`.
- **3장 일꾼 첫 자리**: `[추측: +0x12de 뒤]` → **`[정적: +0x1a42 뒤]`**. 실제로 언제 생기나는 동적.
- **입구 `RCX` 이름**: "로더 문맥" → **CONTEXT 레코드 `[정적]`**.

그림 2 를 볼 때 주의 둘:

- **화살표 번호가 또 밀렸다.** kernel32 준비 `(6)` → `(7)`, 그래프 초기화 `(7)` → `(12)`, exe TLS `(8)` → `(13)`. 상자 번호(단계 이름)와 화살표 번호(호출 순서)가 따로라서다 (flow.md "그림 읽는 법", [run18 해설 3절](run18-proc-init-setup-해설.md#flowmd-에서-바뀐-것)).
- **회색 점선 상자**는 새로 생긴 색이다. "어느 모듈의 어느 함수인지 실행 때 정해지는 간접 호출" 이라 모듈 상자 밖 "모듈 모름" 칸에 따로 뒀다.

### 큰 그림에서 어떻게 이어지나

- run17 에서 칠판을 0 → 1 로 고친 직원이 **가게 전체 세팅 방**에 들어갔다.
  - run18: 방의 앞쪽 절반 — **살림 차리기** (명부·특별 지시·창고·공용 창고 주소·명부 첫 줄).
  - run19: 뒤쪽 절반 — **짐 들이고 전원 켜기** (명부 둘째 줄 → kernel32 → 일꾼 → dll1·dll2 → 전원 → exe TLS).
  - 이걸로 세팅 방을 **처음부터 끝까지** 한 번 걸어 봤다 (노선도로).
- 방을 나서면 (run17): `_LdrpInitialize` 가 팻말을 3 으로, 칠판을 2 로 고치고 종을 친다 → `LdrInitializeThunk +0x18` `NtContinue` → `RtlUserThreadStart` (run16) → `Kernel32ThreadInitThunkFunction` 칸의 함수 (`[추측]` `BaseThreadInitThunk`, 2.10) → … → `main`.
  - 이번 2.10 덕에 `RtlUserThreadStart` 와 kernel32 사이가 `refs` 한 줄로 이어졌다 (flow.md 2.10).
- **다음 구간** (`plan.md` "다음 항목"): **Q10 — `LdrpInitializeKernel32Functions`, `LdrLoadDll`, `LdrpLoadDll` callee** (정적).
  - 이번에 넘긴 연결점: 칸 `Kernel32ThreadInitThunkFunction` 을 `+0xe4` 가 채움, 출력 칸 `[RSP+0x188]` → `+0x1b72`, 이 호출은 `LdrInitState` = 0·`Subsystem` 2/3 일 때.
  - 그 뒤 Q11·Q12·Q14 (올리기·그래프·`+0x1aa4` 안), Q15 (`+0x1bb6` 안), Q16 (TLS) 이 이번 결과를 출발점으로 쓴다.
- 버스를 탈 때(동적) 볼 것은 Q46 (뒤쪽 실행 값), Q47 (일꾼), Q48 (간접 호출 대상), Q23 (dll1·dll2 가 올라오는 호출), Q24 (재귀 단계) 에 정리됐다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 때 갈림 값: `AvrfAppVerifierMode` = 0, `Subsystem` = 3, `BeingDebugged`(커널 디버거만), `+0x1bb6` 의 `rcx` = exe `DdagNode`, `TlsIndex` ≠ 0, exe 항목 칸(`BaseDllName` = test.exe 등), 재배치 건너뛰나, 반환 `rax` = 0 | 조건과 자리만 `[정적]`. 값은 실행 때 | **Q46** (동적). `UseWOW64` 는 **Q44** |
| dll1·dll2 가 **정말** `+0x1aa4` 안에서 올라오나, 로드 문맥 = 항목 `LoadContext` 인가 | callee 본문을 안 봤다 (이름만) | **Q23** (동적, `lm m dll*` 세 자리 비교), **Q14** (정적) |
| 일꾼이 **언제·몇 개** 생기나 (풀 만들 때 / 일감 올릴 때), 메인 스레드가 직접 들이나 일꾼이 들이나 | `+0x1a42`·`LdrpQueueWork` 안쪽은 callee 본문 | **Q47** (동적), Q23 |
| 간접 호출의 실제 대상: `+0x1b59` 칸의 함수(`[추측]` `BaseThreadInitThunk`), `PEB.PostProcessInitRoutine` 값과 그걸 넣는 쪽, shim 이 꺼져 있나 | 칸 값은 실행 때 정해진다 | **Q48** (동적) |
| `LdrpInitializeNode` 가 `+0x1aed` 안 `LdrpInitializeGraphRecurse` 에서 먼저 불리나, `+0x1bb6` 에서 불리나 | 두 자리 모두 callee 안쪽 | **Q24** (동적, `k`), **Q15** (정적) |
| `LdrpInitializeNode +0x17a` 가 CONTEXT 레코드를 무엇에 쓰나 | 읽는 자리만 `refs` 로 봤다 | **Q15** (정적) |
| 로드 문맥 ↔ `LoadContext`(+0xb0) ↔ `LdrpFreeLoadContextOfNode` 관계, DDAG `State` 9 → 2 이유, `LdrpPrepareModuleForExecution` 안 | callee 본문은 범위 밖 | **Q11·Q12** (정적) |
| `LdrpInitializeKernel32Functions` 안: `+0xe4` 칸 채우기, 출력 칸 `[RSP+0x188]`, `LdrInitState` 0 일 때의 길 | 이번엔 부르는 쪽만 봤다 | **Q10** (정적, 다음 항목) |
| exe 항목 `TlsIndex` 를 누가 쓰나 (`[추측]` `+0x18cb LdrpInitializeTls`) | callee 본문 | **Q16** (정적) |
| 10묶음이 정말 이 순서로 실행되나 | 묶음은 오프셋 순서로 자른 것 | **Q46** / flow.md 4.1 (그림 2 `(0)`·`(1)~(2) 사이` 줄의 멈춤점) |
| 이름·뜻 못 정한 것: AppVerifier 묶음이 복제하는 항목, `LdrInitState` 1·2·3 의 뜻, `+0x1391` 의 `R12`=2 번 반복, `NtGlobalFlag & 0x100` 이름, CPU 기능 비트·기능 번호 41, `$fin$0` 가 TLS 예외 정리인지, `g_LdrpWow64LdrpInitialize` 대상 | PDB 에 이름이 없거나 공개 문서 기준이라 `[추측]` | flow.md 4장 공백 줄 (대기열 줄은 따로 없다) |

- Q46 은 `plan.md` 에 "Q43·Q44 와 같은 기준점 — 연달아 있지는 않음" 이라고 적혀 있다. 버스를 한 번 탈 때 같은 `ntdll+0x8c404+오프셋` 방식으로 run17·18·19 의 실행 값을 함께 볼 수 있다는 뜻이다.

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run19-proc-init-modules.md`](run19-proc-init-modules.md) — 특히 "뒤쪽 큰 순서", "exe 모듈 항목", "`LdrpInitializeGraphRecurse` 세 자리", "다른 DLL 을 올리는 호출 후보", "병렬 로딩", "간접 호출 4곳", "정정"
- 정적 요청서: [`_workspace/sast/14_proc-init-modules.md`](../../_workspace/sast/14_proc-init-modules.md)
- 정적 결과서 (질문별 답, 그림 2 상자 후보 5개, 동적 확인 제안, Q10 ~ Q16 연결점): [`_workspace/sast/14_proc-init-modules_result.md`](../../_workspace/sast/14_proc-init-modules_result.md)
  - 결과서의 동적 제안 번호(Q46 ~ Q50)는 plan 에서 다시 매겼다: 제안 Q46+Q47 → **Q46**, Q49 → **Q47**, Q50 → **Q48**, Q48 → 기존 **Q23**(+ **Q24**). 이 글은 plan 번호를 쓴다.
- 앞 바퀴 해설:
  - [`run18-proc-init-setup-해설.md`](run18-proc-init-setup-해설.md) — 같은 함수 앞쪽, 10묶음 표 읽는 법, 2.6 명부, 2.7 명부 첫 줄, 2.10 에필로그 (맨 위에 이번 정정 안내)
  - [`run17-ldr-init-entry-해설.md`](run17-ldr-init-entry-해설.md) — 2.8 첫 스레드 길·예외 보호 구역·`LdrInitState` 0/3, 2.9 WoW64, 2.10 `RCX`·`RDX` 의 행방
  - [`run16-thunk-context-해설.md`](run16-thunk-context-해설.md) — 2.3 사물함 메모 이야기, 2.4 CONTEXT 칸(`Rcx` = 진입점)·전역 변수
  - [`run11-allocate-thread-해설.md`](run11-allocate-thread-해설.md) — 2.6 CONTEXT, 2.8 잠금
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — 부르는 자리와 돌아올 자리, 직접 호출과 간접 호출, ICF 별칭, 3.7 오프셋 순서 ≠ 실행 순서
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "두 그림 사이 설명" run19 문단, "그림 읽는 법"
  - "그림 2" ntdll 상자 `(0)`(`p6`), `(1)~(2) 사이`(`PH12`: `j1`~`j3`), 회색 상자(`T2CB`: `z1`·`z2`)
  - "2.4" `#### ntdll!LdrpInitializeProcess` 절 (뒤쪽 트리, 둘째 10묶음 표, `[정적]` 줄들)
  - "2.5"·"2.6"·"2.7"·"2.9"·"2.10" run19 줄, "3" ntdll 일꾼 2개, "4. 공백 목록", "4.1"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q9b(끝), Q10(다음), Q11·Q14·Q15·Q16·Q23·Q24 출처, Q46·Q47·Q48(새), 결정 기록, 하네스 후보 (19)

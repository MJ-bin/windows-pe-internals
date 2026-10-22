# run21 load-dll-internal — 해설

> 이 글은 [run21 문서](run21-load-dll-internal.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q11** — 2.5 `LdrpLoadDllInternal` (`+0x284` 앞 = 올리기 쪽), `LdrpPrepareModuleForExecution` callee (`_workspace/plan.md`, 이번에 **끝**)
- 근거:
  - run 문서 [`run21-load-dll-internal.md`](run21-load-dll-internal.md) (가장 중요)
  - 요청서 [`_workspace/sast/16_load-dll-internal.md`](../../_workspace/sast/16_load-dll-internal.md)
  - 결과서 [`_workspace/sast/16_load-dll-internal_result.md`](../../_workspace/sast/16_load-dll-internal_result.md) (판정: **분석성공**)
  - flow.md 변경: 이번 바퀴 `git diff -- runs/02_sprint/flow.md` (아직 커밋 전)
  - 진행표: `_workspace/plan.md` 의 Q11(끝). **새 항목은 없다.** 출처를 더한 곳은 Q12·Q15·Q20·Q24·Q49. 다음 항목은 **Q12**.
- 이번 바퀴도 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - ntdll (`ntdll.dll`, 26100.9457, pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1`). run17 ~ run20 과 같은 파일이다.
  - 본문을 읽은 건 두 함수다: `LdrpLoadDllInternal` (rva `0x1fc30`, 1304바이트, 직접 호출 33줄), `LdrpPrepareModuleForExecution` (rva `0x86e4c`, 307바이트, 호출 8줄).
  - 그 아래 함수들은 **본문을 안 읽었다.** 지도에서 **이름과 호출 자리만** 따라갔다 (2.4 에서 읽는 법).
  - 구조체 칸 이름(`_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_LDR_DDAG_STATE`, `_LDR_DLL_LOAD_REASON`, TEB `SameTebFlags`)은 **PDB 타입**으로 맞췄다.

이 글은 [run20 해설](run20-kernel32-load-해설.md) 의 **다음 편**이다. run20 에서 kernel32 준비 함수가 주문서 두 장(`LdrLoadDll` 두 번)을 내는 걸 봤다. 이번엔 그 주문서가 내려가는 **배달 창구 `LdrpLoadDllInternal`** 의 안을 열었다.

- 기초 말은 다시 안 푼다. **링크만** 건다.
  - 오프셋, call/ret, 콜스택, 태그, 직접·간접 호출, 비트와 플래그 → [run9 해설](run9-create-user-process-해설.md) 2절
  - 섹션(조립식 건물 세트) → [run9 해설 "이미지 섹션"](run9-create-user-process-해설.md#이미지-섹션), 매핑(갖다 놓기) → [run10 해설 "매핑 올리기"](run10-allocate-process-해설.md#매핑-올리기)
  - PEB, 구조체 칸 오프셋 → [run10 해설](run10-allocate-process-해설.md) 2절
  - `\KnownDlls`(동네 공용 창고) → [run18 해설](run18-proc-init-setup-해설.md) 2.4 묶음 9
  - 모듈 목록(PEB.Ldr)·모듈 항목 → [run18 해설 2.6](run18-proc-init-setup-해설.md#26-pebldr--건물-명부가-생기는-순간-0x1c1)
  - 로드 문맥(작업 지시서), DDAG 노드(연결 카드) → [run19 해설 2.6](run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1)
  - 로더 락(관리실 열쇠), 전원 켜기 `+0x1bb6` → [run19 해설](run19-proc-init-modules-해설.md) 2.8
  - 병렬 로딩, 일감·게시판(`LdrpQueueWork`, `LdrpDrainWorkQueue`) → [run19 해설](run19-proc-init-modules-해설.md) 2.9
  - 팻말 `LdrInitState` (0 → 1 → 2 → 3) → [run19 해설 2.12](run19-proc-init-modules-해설.md#212-팻말--ldrinitstate-1--2--3)
  - 두 `LdrLoadDll` 의 이름과 "순서 문제" → [run20 해설](run20-kernel32-load-해설.md) 2.4
  - 상태 칸(결과 메모지), `LdrpLoadDll` 의 인자 9개 → [run20 해설](run20-kernel32-load-해설.md) 2.11
  - `0xc0000135` (`STATUS_DLL_NOT_FOUND`) → [run20 해설](run20-kernel32-load-해설.md) 2.12
- 비유도 그대로 이어 쓴다.
  - 커널 = **관리사무소**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**
  - PEB.Ldr = **건물 명부**, 모듈 항목 = **명부 한 줄**, `LdrpHashTable` = **이름 색인 카드함** (run18·run20)
  - DDAG 노드 = 명부 줄마다 붙은 **연결 카드**, 로드 문맥 = **작업 지시서** (run19)
  - 짐 들이기 = DLL 올리기, 전원 켜기 = DLL 시작 함수 부르기 (run19)
  - `LdrLoadDll` 호출 = 관리실이 내는 **주문서**, 그 아래 함수들 = **배달 기사** (run20)
  - 섹션 = **조립식 건물 세트**, 매핑 = 세트를 땅에 **갖다 놓기**, `\KnownDlls` = **동네 공용 창고** (run9·run10·run18)
  - 정적분석 = **노선도** 읽기, 동적분석 = **버스 타 보기**
- run 문서는 이번에 "**택배 접수 창구**" 비유를 썼다. 이 글은 그걸 run20 의 배달 그림으로 옮긴다 (가리키는 건 같다).
  - "이미 받은 물건인가?" = 명부에 이미 있나 빨리 보기 (빠른 끝)
  - 송장 만들기 = 찾거나 준비 (`LdrpFindOrPrepareLoadingModule`)
  - 실제 배송 = 올리기 (`LdrpProcessWork` 등 그 아래)
  - "도착했으니 전원 켜세요" 연락 = 실행 준비 (`LdrpPrepareModuleForExecution`)

한 줄로 먼저 말하면:

- `LdrpLoadDllInternal` 은 이런 순서다 `[정적]`:
  - 이미 있으면 **바로 돌려준다** → 없으면 **자리를 만들고 실제로 올린다** → 다 올리면 **켜기 준비**를 부른다 → 프로세스 세팅 초반이면 **고정**한다 → 작업 지시서를 치운다.
- 메모리에 올리는 일(파일 열기·섹션·매핑)은 이 함수의 **`+0x284` 앞** 두 호출 아래에 있다 `[정적: 지도]`.
- 이번 바퀴의 **핵심 셋**:
  1. run20 의 "**순서 문제**" (KERNEL32 를 먼저 주문했는데 전원은 KERNELBASE 가 먼저 켜짐)에 **코드 길이 생겼다.** 올리기 길 아래에 의존 DLL 을 처리하는 함수 `LdrpLoadDependentModuleInternal` 이 있어서, **같은 호출 안에서** kernelbase 가 따라 올라올 수 있다. 아직 `[정적: 지도]` 이고, 실제로 그렇게 되는지는 **Q20** → 2.5
  2. 둘째 `LdrLoadDll("KERNELBASE.dll")` 이 **시작 함수를 안 부르는 이유**가 코드에 두 길로 있다. (가) 빠른 끝, (나) 끝까지 가지만 "이미 켜짐" 이라 바로 돌아옴. **둘 다 run7 관찰과 맞는다** → 2.8
  3. 같은 `LdrpPrepareModuleForExecution` 이 kernel32 때는 **그 자리에서 켜고** (run7), exe import 때는 **미루는** (run8) 이유 = **`LdrpInitializeProcess +0x1a94` 가 세우는 비트 하나** → 2.7

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
   └─ _LdrpInitialize +0x16c  LdrpInitializeProcess   (가게 전체 세팅)              (run17 ~ run19)
        ├─ +0x19a2  LdrpInitializeKernel32Functions                                (run20)
        │     ├─ +0xc0   LdrLoadDll("KERNEL32.DLL")
        │     │     └─ +0x170 LdrpLoadDll
        │     │          └─ +0x100 LdrpLoadDllInternal                             ← 이번 (run21)
        │     │               └─ +0x284 LdrpPrepareModuleForExecution               ← 이번 (run21)
        │     │                    └─ +0xef LdrpInitializeGraphRecurse … (KERNELBASE·KERNEL32 켜기)
        │     └─ +0x156  LdrLoadDll("KERNELBASE.dll")
        │           └─ +0x170 LdrpLoadDll
        │                └─ +0x100 LdrpLoadDllInternal                             ← 이번 (둘째 호출)
        ├─ +0x1aed  LdrpPrepareModuleForExecution (exe 항목)                        ← 이번 (같은 함수, 다른 caller)
        └─ +0x1bbb  LdrpInitializeGraphRecurse … (dll2 → dll1 켜기)
   └─ +0x18 NtContinue ═▶ RtlUserThreadStart → … → main                         (run16, 뒤는 추측)
```

- run20 은 이 사슬을 "**주문서를 내는 쪽**" 에서 봤다. 이번엔 주문서를 **받아 처리하는 창구** 안이다.
- flow.md 로는 2.5 (`LdrLoadDll` 안쪽, `[공백]` 올리는 순간) 와 2.7 (켜기 두 자리의 역할 나눔) 이다.

### 무엇이 궁금했나

run 문서 "목적" 그대로.

1. flow.md 2.5 `[공백]` "kernel32·kernelbase 를 **메모리에 올리는 순간**" 이 `LdrpLoadDllInternal` 안 **어느 호출 아래**인가.
2. 덤으로:
   - 둘째 `LdrLoadDll("KERNELBASE.dll")` 은 이 함수 안 어디서 끝나나.
   - 같은 `LdrpPrepareModuleForExecution` 이 kernel32 때는 그래프를 켜고 (run7), exe 때는 안 켜는 (run8) 까닭.

### 바퀴 전에 알던 것

- `[관찰]` (run7) 첫째 `LdrLoadDll` 안 사슬: `LdrpLoadDll +0x100` → `LdrpLoadDllInternal +0x284` → `LdrpPrepareModuleForExecution +0xef` → `LdrpInitializeGraphRecurse`. 그 안에서 KERNELBASE → KERNEL32 순으로 시작 함수가 불렸다.
- `[관찰]` (run7) 둘째 `LdrLoadDll` 에선 시작 함수가 **안 불렸다.** `LdrpLoadDll` 까지만 봤다.
- `[관찰]` (run8) dll1·dll2 의 켜기는 `LdrpInitializeProcess +0x1bbb` 아래였다.
- `[정적]` (run20) 두 주문서 이름은 첫째 `KERNEL32.DLL`, 둘째 `KERNELBASE.dll`.
- `[정적]` (run20) `LdrpLoadDll` 은 `LdrpLoadDllInternal` 에 9개를 넘긴다 (넷째 = `4`, 상태 칸 포함). 돌려주는 건 반환값이 아니라 **상태 칸 값**. 그 칸을 실제로 채우는지는 Q11.
- `[정적]` (run19) `LdrpInitializeProcess +0x1aed` 도 `LdrpPrepareModuleForExecution` 을 부른다 (exe 항목). 로드 문맥 `[RSP+0x2b0]` 은 `+0x1351` 에서 만든다.
- `[추측]` (flow.md 2.5) `+0x284` **앞부분**에서 kernel32 를 올리고, kernelbase 도 **따라** 올린다.
- `[추측]` (run20) 둘째 `LdrLoadDll("KERNELBASE.dll")` 은 이미 올라온 것을 **다시 찾는** 호출이다.

---

## 2. 찾은 것

### 2.1 한눈에 — 트리

run 문서 "호출 관계" 에서 대표만 남겼다. 오프셋은 모두 **돌아올 자리(ret)** 다 ([부르는 자리와 돌아올 자리](run9-create-user-process-해설.md#부르는-자리와-돌아올-자리)). 번호는 **코드 흐름 순서**다.

```
LdrpLoadDll +0x100
 └─ LdrpLoadDllInternal (0x1fc30)          이유 = 4 / 결과: 결과 항목 칸, 상태 칸
      ├─ (1) +0xb8   LdrpFastpthReloadedDll        ── 0 이상 또는 0xc00004c0 → 로그 뒤 RET (아래 전부 건너뜀)
      ├─ (2) +0x18a  LdrpDrainWorkQueue(0)          LoadOwner 가 아닐 때만
      ├─ (3) +0x1a0  LdrpThreadTokenSetMainThreadToken
      ├─ (4) +0x1c0  LdrpDetectDetour
      ├─ (5) +0x1f2  LdrpFindOrPrepareLoadingModule    (지도) 아래 KnownDll 길 → NtMapViewOfSection
      ├─ (6) +0x210  LdrpProcessWork(LoadContext, 1)   (5) 가 0xc0000135 일 때만. (지도) 아래 파일 길
      ├─ (7) +0x21a  LdrpDrainWorkQueue(1)           늘
      ├─ (8) +0x317  LdrpThreadTokenUnsetMainThreadToken   토큰 ≠ 0
      ├─ (9) +0x240  LdrpHandleReplacedModule
      ├─ (10) +0x262 LdrpCondenseGraph               LoadContext ≠ 0
      ├─ (11) +0x284 LdrpPrepareModuleForExecution    상태 ≥ 0              [관찰] run7
      │     └─ (조건이 맞을 때만) +0xef LdrpInitializeGraphRecurse          [관찰] run7
      ├─ (12) +0x295 LdrpBuildForwarderLink
      ├─ (13) +0x337 LdrpPinModule                    LdrInitState == 0 일 때만
      └─ (14) +0x2c8 LdrpFreeLoadContextOfNode       → 정리 → 로그 → RET
```

읽을 때 주의:

- `[관찰]` 이 붙은 두 줄만 run7 에서 디버거로 본 자리다. 나머지는 **전부 `[정적]`** 이다.
  - 실행 때 탄 게 확인된 건 `(11)` 과 그 안 `+0xef` 뿐이다 (run 문서).
  - 그 두 숫자는 이번에도 `_calls.tsv` 와 **똑같았다.** 버전 대조 일치.
- 33줄 중 빠진 건 잡일과 갈래들이다: 로그 `LdrpLogInternal` 6곳, 패치 전용 갈래 (이유가 9 일 때만, 2.10), 실패 때 참조 내리기 (`+0x2eb`, `+0x2f3`), 예외 조각 `$fin$0`.
- **번호가 또 하나 있다.** flow.md 그림 2 의 화살표 `(1)~(5)` 는 그림에 올린 것만 센 번호다: `(1) +0xb8`, `(2) +0x1f2`, `(3) +0x210`, `(4) +0x284`, `(5) +0x337`. flow.md 2.5 트리는 `[a]`~`[h]` 를 쓴다. 헷갈리면 **오프셋**으로 맞추면 된다.

**새 말 — 결과 항목 칸, 상태 칸**: 이 함수는 반환값이 없다 (void). 대신 caller 가 건넨 **칸 두 개**에 적는다 `[정적]`.

- 결과 항목 칸 (일곱째 인자) = "올린 DLL 의 명부 줄" 을 적는 칸.
- 상태 칸 (여덟째 인자) = 성공·실패 번호를 적는 칸. run20 의 **결과 메모지**다.
- 비유: 배달 기사가 말로 대답하지 않고, 주문서에 붙은 **송장 칸**과 **결과 메모지**에 적어서 돌려준다.

### 2.2 한 줄씩 읽는 법 — 대표 넷

"누가 / 어디서(+0x) / 누구를 / 무엇을 위해" 로 읽는다. **누가**는 모두 `LdrpLoadDllInternal` (배달 창구) 이다.

**(1) `+0xb8 LdrpFastpthReloadedDll`** `[정적]`

- 어디서: 부르는 자리 `+0xb3`, 돌아올 자리 `+0xb8`.
- 누구를: `LdrpFastpthReloadedDll(이름, 플래그, 부모 항목, 결과 항목 칸)`.
- 무엇을 위해: "이미 올라온 DLL 이면 **빨리 돌려주기**" 로 보인다 `[추측: 이름·callee 이름]`. 자세히는 2.3.
- 갈림: 결과가 0 이상이거나 `0xc00004c0` 이면 → 상태 칸에 쓰고 마지막 로그 뒤 `RET`. **`(11) +0x284` 까지 안 간다.**

**(5) `+0x1f2 LdrpFindOrPrepareLoadingModule`** `[정적]`

- 어디서: 부르는 자리 `+0x1ed`, 돌아올 자리 `+0x1f2`.
- 누구를: 인자 7개 — 이름, 경로 구조체, 플래그, 이유, 패치용 항목, **&지역 항목**, 상태 칸.
- 무엇을 위해: 명부에서 찾고, 없으면 **새 명부 줄을 만들어** "이제 올려라" 를 알리는 것으로 보인다 `[추측: callee 이름]`. 2.3.
- 갈림: 결과가 `0xc0000135` 면 → (6). 아니면 (6) 을 건너뛴다.

**(11) `+0x284 LdrpPrepareModuleForExecution`** `[정적]` + `[관찰]` run7 (자리)

- 어디서: 부르는 자리 `+0x27f`, 돌아올 자리 **`+0x284`** (run7 콜스택 숫자와 같다).
- 누구를: `LdrpPrepareModuleForExecution(항목, 상태 칸)`.
- 무엇을 위해: 올라온 DLL 의 **켜기 준비**. 조건이 맞으면 안에서 `+0xef` 로 전원을 켠다. 2.6·2.7.
- 갈림: **상태 칸이 0 이상일 때만** 부른다 (`+0x274`). 앞에서 실패했으면 건너뛴다.

**(13) `+0x337 LdrpPinModule`** `[정적]`

- 어디서: 부르는 자리 `+0x332`, 돌아올 자리 `+0x337`.
- 누구를: `LdrpPinModule(항목)`.
- 무엇을 위해: 이름으로는 모듈을 "**고정**" 한다 `[추측: 이름]`. 2.9.
- 갈림: `+0x29b` 에서 **`LdrInitState == 0` 일 때만**. 이 함수에서 `LdrInitState` 를 읽는 곳은 이 한 곳뿐이다.

#### 비유로 다시 한 번 — (1) ~ (14)

1. 명부를 슬쩍 보고 "이미 있네" 면 바로 돌려준다 (빠른 끝)
2. ~ 4. 게시판 정리, 메인 직원 표시, 우회 장치 검사 (2.11)
5. 송장을 만든다 — 명부에서 찾거나, 새 줄을 준비한다
6. 새로 들여야 하면 **실제로 배송**한다
7. ~ 10. 게시판 다시 정리, 바뀐 건물 처리, 연결 그림 정리
11. "**도착했으니 전원 켤 준비**" 연락
12. 다른 건물로 넘기는 연결 만들기
13. 세팅 초반이면 "**고정**" 도장
14. 작업 지시서 치우고 끝

### 2.3 앞쪽 세 상자 — 빠른 끝, 찾거나 준비, 올리기

flow.md 그림 2 에 새로 들어간 `(정적)` 상자 `kf1`·`kf2`·`kf3` 이 이 셋이다. 하나씩 보자.

#### 빠른 끝 — `+0xb8 LdrpFastpthReloadedDll`

**새 말 — 빠른 길 (fast path)**: 흔한 경우를 짧게 처리하고 바로 빠져나가는 길로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 함수 이름의 `Fastpth` 가 그 줄임으로 보인다 `[추측: 이름]`.

- 비유: 창구 직원이 주문서를 받자마자 **색인 카드함**을 한 번 본다. 카드가 있으면 "이미 있어요" 하고 송장에 번지만 적어 돌려준다.

코드에 있는 것 `[정적]`:

1. 이유가 9 (`PatchImage`) 면 이 호출을 건너뛴다 (`+0x9e`). 이유 4 인 `LdrLoadDll` 길은 탄다.
2. 결과가 **0 이상**이거나 **`0xc00004c0`** 이면 → 상태 칸에 쓰고 (`+0xd0`) → 마지막 로그 2개 (`+0x113`, `+0x143`) → `RET`.
3. 그 밖의 음수면 → 느린 길 `+0x15f` 로 간다 (아래 전부).

"이미 올라온 DLL" 로 읽는 근거는 **이름뿐**이다 `[추측]`:

- 함수 이름 자체 (`ReloadedDll`)
- 지도에서 본 callee 이름: `LdrpFindLoadedDllByName`, `LdrpIncrementModuleLoadCount`
- 본문은 안 읽었다.

#### 찾거나 준비 — `+0x1f2 LdrpFindOrPrepareLoadingModule`

느린 길로 왔을 때, 그 사이에 잡일 셋을 거친 뒤 (2.11) 이 호출이 온다 `[정적]`.

- 결과로 세 갈래다 `[정적]`:

| 결과 | 가는 곳 | 상태 칸 |
|---|---|---|
| `0xc0000135` | **(6) `+0x210 LdrpProcessWork`** 를 탄다 | 안 쓴다 |
| 0 이상, 또는 `0xc000022d` | (6) 건너뛰고 (7) | 안 쓴다 |
| 그 밖의 음수 | 상태 칸에 쓰고 (7) | 씀 |

- 눈여겨볼 것: `0xc0000135` 는 run20 에서 본 `STATUS_DLL_NOT_FOUND` 값이다. 여기선 **실패가 아니라 "다음 단계로 가라" 신호**처럼 쓰인다.
  - "명부에 없어서 새 줄을 만들었다 → 이제 실제로 올려라" 라는 뜻으로 보인다 `[추측: callee 이름 LdrpAllocateModuleEntry·LdrpLoadKnownDll·LdrpFindLoadedDllByName]`.
- 비유: 송장 담당이 명부를 찾아보고, 없으면 **빈 명부 줄과 작업 지시서**를 새로 만든다. 그리고 "아직 물건 없음 — 배송 출발" 표를 붙여 넘긴다.

#### 올리기 — `+0x210 LdrpProcessWork`

- `(5)` 가 `0xc0000135` 일 때**만** 부른다. 인자는 `지역 항목->LoadContext(+0xb0)` 와 `1` `[정적]`.
  - 즉 **(5) 가 만든 명부 줄의 작업 지시서**를 들고 배송을 시작한다.
- 이 아래가 실제 올리기라는 건 **지도 이름 사슬**로 본 것이다 `[정적: 지도]`. 다음 절에서 한 칸씩 읽는다.

### 2.4 이름 사슬 한 칸씩 읽는 법 — KnownDll 길, 파일 길

#### 먼저 — "지도 이름 사슬" 이 뭔가

- **지도** = Ghidra 로 미리 뽑아 둔 "**누가 누구를 부르나**" 표다 (`_map/LdrInitializeThunk_edges.tsv`). 한 줄에 caller → callee 하나, 라벨은 ret 자리.
- **이름 사슬** = 그 표의 줄을 이어 붙여, 한 함수에서 시스템 콜까지 **가장 짧은 길**을 찾은 것 (결과서: BFS).
- **깊이** = 화살표 개수. `LdrpLoadDllInternal` 에서 몇 번 내려가야 닿나.
- 비유: 노선도에서 "A 역에서 B 역까지 몇 정거장" 을 세는 것.

꼭 기억할 것 셋:

- 사슬은 "**이 길이 코드에 있다**" 까지다. "실행 때 **이 길로 갔다**" 가 아니다.
- callee 본문을 안 읽었으니, 각 칸에서 **어떤 조건일 때** 다음 칸으로 가는지는 모른다.
- 가장 짧은 길만 골랐다. 다른 길이 없다는 뜻도 아니다.
- 그래서 태그는 `[정적: 지도]` 다.

#### 한 칸 읽는 법

사슬의 한 토막 `LdrpLoadDllInternal +0x1f2 → LdrpFindOrPrepareLoadingModule +0x5e5` 를 풀면:

- `LdrpLoadDllInternal` 안, 돌아올 자리 `+0x1f2` 인 호출이 `LdrpFindOrPrepareLoadingModule` 을 부른다.
- 그리고 `+0x5e5` 는 **`LdrpFindOrPrepareLoadingModule` 안**의 돌아올 자리다. 거기서 **다음 칸**을 부른다.
- 즉 **`+0x` 숫자는 늘 그 왼쪽 함수의 것**이고, "여기서 오른쪽 함수를 부른다" 는 뜻이다.
- 실행 때 정말 이 길로 갔다면, 그 순간 콜스택에 이 숫자들이 **거꾸로** 쌓여 보일 것이다 (`LdrpMinimalMapModule+0x179` 가 맨 위 쪽). 그걸 보는 게 Q20 이다.

#### (가) KnownDll 길 — 공용 창고에서 꺼내 오기 (깊이 5)

```
LdrpLoadDllInternal +0x1f2 → LdrpFindOrPrepareLoadingModule +0x5e5 → LdrpLoadKnownDll +0xf1
 → LdrpMapDllWithSectionHandle +0x4e → LdrpMinimalMapModule +0x179 → NtMapViewOfSection
```

한 칸씩:

| 칸 | 누가 (ret 자리) | 다음 칸 | 이름으로 읽은 뜻 |
|---|---|---|---|
| 1 | `LdrpLoadDllInternal +0x1f2` | `LdrpFindOrPrepareLoadingModule` | 찾거나 준비 (2.3) |
| 2 | `LdrpFindOrPrepareLoadingModule +0x5e5` | `LdrpLoadKnownDll` | KnownDll 로 올리기 `[추측: 이름]` |
| 3 | `LdrpLoadKnownDll +0xf1` | `LdrpMapDllWithSectionHandle` | 섹션 핸들로 매핑 `[추측: 이름]` |
| 4 | `LdrpMapDllWithSectionHandle +0x4e` | `LdrpMinimalMapModule` | 최소한의 매핑 `[추측: 이름]` |
| 5 | `LdrpMinimalMapModule +0x179` | **`NtMapViewOfSection`** | 커널에 "갖다 놓아 주세요" (시스템 콜) |

- 옆 가지 `[정적: 지도]`: `LdrpLoadKnownDll +0x63` → `LdrpFindKnownDll +0xd9` → **`NtOpenSection`**, 그리고 `LdrpLoadKnownDll +0xfd` → `NtClose`.
- 이 길엔 **`NtOpenFile` 도 `NtCreateSection` 도 없다.** 파일을 열지 않고, `\KnownDlls` 에 이미 있는 섹션을 **연다** `[정적: 지도]`.
- run18 이 열어 둔 `\KnownDlls` 핸들(`LdrpKnownDllDirectoryHandle`)을 읽는 `LdrpFindKnownDll` 이 바로 이 길 위에 있다 (flow.md 2.5).

**새 말 — 섹션 열기 vs 섹션 만들기**

- `NtOpenSection` = **이미 만들어 둔** 섹션을 이름으로 여는 것, `NtCreateSection` = 파일을 보고 섹션을 **새로 만드는** 것으로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유:
  - 섹션 = 조립식 건물 세트 (run9).
  - KnownDll 길 = **동네 공용 창고**(run18)에 이미 짜 둔 세트를 **꺼내 와서** 우리 땅에 갖다 놓기. 설계도를 펼칠 필요가 없다.

#### (나) 파일 길 — 설계도를 펴서 새로 짜기 (깊이 4 ~ 6)

```
LdrpLoadDllInternal +0x210 → LdrpProcessWork +0x6a → LdrpMapDllSearchPath +0x25c → LdrpMapDllNtFileName
   LdrpMapDllNtFileName 안: +0x137 NtOpenFile / +0x194 NtCreateSection / +0x28c LdrpMapDllWithSectionHandle
     → LdrpMapDllWithSectionHandle +0x4e → LdrpMinimalMapModule +0x179 → NtMapViewOfSection
```

한 칸씩:

| 칸 | 누가 (ret 자리) | 다음 칸 | 깊이 |
|---|---|---|---|
| 1 | `LdrpLoadDllInternal +0x210` | `LdrpProcessWork` | 1 |
| 2 | `LdrpProcessWork +0x6a` | `LdrpMapDllSearchPath` (검색 경로로 찾기 `[추측: 이름]`) | 2 |
| 3 | `LdrpMapDllSearchPath +0x25c` | `LdrpMapDllNtFileName` | 3 |
| 4a | `LdrpMapDllNtFileName +0x137` | **`NtOpenFile`** (파일 열기) | 4 |
| 4b | `LdrpMapDllNtFileName +0x194` | **`NtCreateSection`** (세트 새로 짜기) | 4 |
| 4c | `LdrpMapDllNtFileName +0x28c` | `LdrpMapDllWithSectionHandle` → `+0x4e LdrpMinimalMapModule` → `+0x179 NtMapViewOfSection` | 4 → 6 |

- 4a·4b·4c 는 같은 함수 `LdrpMapDllNtFileName` 안의 **세 호출**이다. 오프셋 순서로는 열기 → 짜기 → 갖다 놓기다.
  - 실행도 그 순서일 것 `[추측]` — 오프셋 순서가 실행 순서와 늘 같은 건 아니고 ([run9 해설 3.7](run9-create-user-process-해설.md)), 이 함수 본문은 안 읽었다.
- `LdrpProcessWork` 안의 다른 갈래 이름 `[정적: 지도]`: `+0x144 LdrpMapDllFullPath`, `+0x14e LdrpMapDllRetry`, `+0x158 LdrpMapDllPatchImage`, `+0x131 LdrpSnapModule`.
- 비유: 공용 창고에 없는 건물이면, **설계도 파일을 꺼내(`NtOpenFile`) 세트를 새로 짜서(`NtCreateSection`) 갖다 놓는다.**

#### 두 길이 만나는 곳 — `LdrpMapDllWithSectionHandle`

- 두 길 모두 `LdrpMapDllWithSectionHandle +0x4e → LdrpMinimalMapModule +0x179 → NtMapViewOfSection` 으로 끝난다 `[정적: 지도]`.
- 지도상 `LdrpMapDllWithSectionHandle` 을 부르는 곳은 셋이다: `LdrpLoadKnownDll`, `LdrpMapDllNtFileName`, `LdrpMapDllPatchImage`.
- run20 에서 본 "**색인 카드함에 카드 끼우기**" (`LdrpInsertDataTableEntry`) 의 호출자 하나가 `LdrpMapDllWithSectionHandle +0x141` 이었다. 같은 함수다.
  - → 갖다 놓은 뒤 **명부·색인에 끼우는 자리**도 이 함수 안이다 `[정적: 지도]`.
- 비유: 창고에서 꺼내 왔든 새로 짰든, 마지막엔 같은 **설치 담당**이 땅에 갖다 놓고, 명부에 이름을 적는다.

#### kernel32 는 어느 길로?

- **모른다.** 어떤 DLL 이 KnownDll 로 가는지는 정적으로 못 정한다 (run 문서).
- 확인: **Q20** — `LdrpMinimalMapModule` ret `+0x179` (`ntdll+0xa399`) 에서 `k`. 콜스택에 `LdrpFindOrPrepareLoadingModule +0x5e5` 가 보이면 (가), `LdrpProcessWork +0x6a` 가 보이면 (나).

### 2.5 핵심 ① run20 의 "순서 문제" 에 코드 길이 생겼다

#### run20 에서 남은 문제 — 다시 한 번

- 코드 `[정적]`: 주문서 순서는 KERNEL32 (`+0xc0`) → … → KERNELBASE (`+0x156`).
- 디버거 `[관찰]`: 전원은 **첫째 주문서 안에서** KERNELBASE → KERNEL32 순으로 켜졌다.
- run20 이 낸 짐작 `[추측]`: 첫째 주문 안에서 kernel32 가 **기대는** kernelbase 도 **따라** 들어온다. 그런데 "따라 들어오는 길" 이 코드에 있는지는 몰랐다 (그 안을 안 읽어서).

#### 이번에 본 것 — 의존 DLL 로 내려가는 길 `[정적: 지도]`

**새 말 — 의존 DLL 처리**: 어떤 DLL 이 import 하는 (기대는) 다른 DLL 을 찾아 올리는 일. 지도에 이런 이름이 있다 `[정적]`: `LdrpLoadDependentModuleInternal`, `LdrpLoadDependentModuleW`, `LdrpMapAndSnapDependency`, `LdrpSnapModule`. (`LdrpLoadDependentModule` 이라는 이름 하나짜리는 없다.)

- 이름의 `Dependent` = "기대는 쪽", `Snap` = 함수 주소 이어 주기 ([run19 해설](run19-proc-init-modules-해설.md) 2.7) 로 읽힌다 `[추측: 이름]`.

길이 둘 있다.

**길 1 — KnownDll 길 안쪽에서** (깊이 4 ~ 5)

```
… LdrpLoadKnownDll +0xf1 → LdrpMapDllWithSectionHandle +0x1c8 → LdrpMapAndSnapDependency
     LdrpMapAndSnapDependency 안: +0x34e LdrpLoadDependentModuleInternal / +0x4c2 LdrpSnapModule / +0x5d8 LdrpLoadDependentModuleW
```

- 2.4 의 "설치 담당" `LdrpMapDllWithSectionHandle` 이 갖다 놓는 일(`+0x4e`) 말고 `+0x1c8` 에서 `LdrpMapAndSnapDependency` 도 부른다.
- 그 안에서 의존 DLL 처리 `LdrpLoadDependentModuleInternal` 로 간다 (`+0x5d8 LdrpLoadDependentModuleW` 도 안에서 `+0xae` 로 같은 함수를 부른다).

**길 2 — 파일 길 쪽에서** (깊이 3)

```
LdrpLoadDllInternal +0x210 → LdrpProcessWork +0x131 → LdrpSnapModule +0x5df → LdrpLoadDependentModuleInternal
```

**그리고 되돌아오기 — 재귀 모양**

- `LdrpLoadDependentModuleInternal` 안에 이런 호출이 있다 `[정적: 지도]`:
  - `+0x1792 LdrpLoadKnownDll` — 2.4 (가) 의 그 함수
  - `+0x1cae LdrpProcessWork` — 2.4 (나) 의 그 함수
  - `+0x115c LdrpAllocateModuleEntry` — 새 명부 줄 만들기 `[추측: 이름]`
  - `+0x16ee LdrpFindLoadedDllByName` — 색인 카드함 보기 `[추측: 이름]`
  - `+0x182c LdrpQueueWork` — 게시판에 일감 붙이기 (run19)
- 즉 의존 DLL 처리가 **같은 올리기 함수들로 되돌아간다.** 의존 DLL 의 의존 DLL 도 같은 방식으로 내려갈 수 있는 모양이다.

#### 그래서 — 단계별로

1. kernel32 를 올리는 `LdrpLoadDllInternal` 의 호출 트리 **안쪽**에 의존 DLL 처리 함수가 있다 `[정적: 지도]`.
2. 그 함수는 다시 `LdrpLoadKnownDll`·`LdrpProcessWork` 로 내려가 `NtMapViewOfSection` 에 닿을 수 있다 `[정적: 지도]`.
3. 그러니 kernel32 의 import (kernelbase) 를 **새 `LdrLoadDll` 주문서 없이, 같은 호출 안에서** 올릴 수 있는 코드 모양이다 `[정적: 지도]`.
4. 이게 run20 의 순서 문제를 푸는 그림과 맞는다: 첫째 주문 안에서 kernelbase 가 따라 올라오고 → 기대는 쪽(kernelbase)부터 켜진다 (run7 `[관찰]`).
5. **하지만 아직 `[추측]` 이다.**
   - 지도는 "길이 있다" 까지다. 실행 때 이 길로 갔는지는 모른다.
   - "kernel32 가 kernelbase 를 import 한다" 자체가 아직 `[추측]` 이다 (kernel32 의 import 표는 안 봤다, run20 해설 2.4).
   - 확인: **Q20** — `ntdll+0xa399` 에서 `k`. KERNELBASE 를 올리는 순간의 콜스택이 첫째 `LdrLoadDll +0xc0` **아래**이고, 그 사이에 `LdrpLoadDependentModuleInternal` 이 보이면 맞다.

#### 덤 — exe 의 dll1·dll2 도 같은 함수다

- `LdrpMapAndSnapDependency` 는 run19 에서 본 `LdrpInitializeProcess +0x1aa4` (exe import, dll1·dll2 들이기 후보 1순위) 와 **같은 함수**다 `[정적: 지도]`.
- 그러니 "dll1·dll2 도 kernel32 와 같은 방식으로 올라온다" 는 flow.md 2.6 의 `[추측]` 이 **이름으로** 맞는다. 실제 순간은 Q23.
- 다른 점은 **켜기**다 — 2.7 에서.

#### 하나 남는 것 — 게시판 갈래

- 같은 곳에 `LdrpQueueWork` (`LdrpLoadDependentModuleInternal +0x182c`, `LdrpMapAndSnapDependency +0x4f7`) 가 있다. 일을 **일꾼 게시판**에 붙이는 갈래다 (run19 2.9).
- kernel32 준비 때는 아직 병렬 로딩을 켜기 전(`LdrpInitializeProcess +0x1a42` 앞)이다. 그때 게시판에 붙은 일이 **어디서 도는지**는 모른다 → Q47·Q20.

#### 비유로 정리

- 관리실이 "기본 설비 건물(KERNEL32) 주세요" 주문서를 낸다.
- 배달 기사가 건물을 갖다 놓고 나서, 그 건물의 **"기대는 건물 목록"(import)** 을 본다.
- 목록에 옆 건물(KERNELBASE)이 있으면, 기사는 **새 주문서를 받지 않고** 같은 방법(공용 창고에서 꺼내거나, 설계도로 새로 짜기)으로 그 건물도 가져와 세운다.
- 옆 건물에도 기대는 건물이 있으면 또 같은 일을 한다 (재귀 모양).
- 이번에 확인한 건 **기사의 업무 매뉴얼에 그런 절차가 적혀 있다**는 것까지다. 그날 정말 그렇게 했는지는 버스를 타 봐야 안다 (Q20).

### 2.6 연결 카드의 진행 도장 — 노드 상태 번호 (5 ~ 9)

다음 두 절(2.7·2.8)을 읽으려면 이 표가 필요하다.

**새 말 — 노드 상태 (`State`)**: DDAG 노드 (연결 카드) 의 `+0x38` 칸이다. 그 DLL 이 지금 **어느 단계까지 왔나**를 번호로 적는다. 번호 이름은 PDB 열거 `_LDR_DDAG_STATE` 에서 읽었다 `[정적]`.

- 비유: 연결 카드에 찍는 **진행 도장**.

| 번호 | PDB 이름 | 이름으로 읽은 뜻 `[추측: 이름]` | `LdrpPrepareModuleForExecution` 이 이 번호를 보면 `[정적]` |
|---|---|---|---|
| 5 | `Snapped` | 함수 주소 잇기 끝 | `+0x12f LdrpCondenseGraph` 한 뒤 다음 단계로 |
| 6 | `Condensed` | 연결 그림 정리 끝 | 다음 단계로 |
| 7 | `ReadyToInit` | 켤 준비 됨 | 알림·shim 을 건너뛰고 **비트 검사**(2.7 6번)로 바로 |
| 8 | `Initializing` | 켜는 중 | **바로 `return 0`** |
| 9 | `ReadyToRun` | 다 켜짐 | **바로 `return 0`** |
| 그 밖 | — | — | `0xc00000e5` 를 돌려줌 |

- 결과서는 다른 번호 이름도 적었다 `[정적]`: 1 `Mapping`, 2 `Mapped`, 4 `Snapping`.
- run19 에서 본 exe 항목 `State` 9 (`+0x1508`) → 2 (`+0x1a60`) 도 이 번호표다. 왜 9 를 썼다가 2 로 되돌리는지는 여전히 모른다 (Q12).
- 7 일 때 알림·shim 을 왜 건너뛰는지는 모른다 (run 문서 "남은 `[추측]`", flow.md 4장).
- 비유로 표를 한 줄로: 도장이 "**켜는 중**"(8) 이거나 "**다 켜짐**"(9) 이면, 켜기 준비 담당은 아무것도 안 하고 돌아간다. 이게 2.8 핵심 ② 의 열쇠다.

### 2.7 핵심 ③ 같은 `LdrpPrepareModuleForExecution` — kernel32 땐 켜고, exe 땐 미룬다

#### 이 함수의 순서 `[정적]`

| 순서 | 자리 | 하는 일 | 비유 |
|---|---|---|---|
| 1 | `+0x20 ~ +0x2b` | 지금 직원이 전역 `LdrpDllNotificationLock` 의 주인(`+0x10` 칸)이면 **바로 `return 0`** | "알림 담당 본인이 또 왔네" 하고 돌려보냄 (뜻은 `[추측]`) |
| 2 | `+0x31 ~ +0x40` | 노드 `State` 로 갈림 (2.6 표) | 진행 도장 보기 |
| 3 | `+0x6f` | `LdrpAddNodeServiceTag` (조건부) | 서비스 꼬리표 붙이기 `[추측: 이름]` |
| 4 | `+0x7b` | `LdrpNotifyLoadOfGraph` — 음수면 끝 | "새 건물들 왔어요" 알림 `[추측: 이름]` |
| 5 | `+0x8d` | `LdrpDynamicShimModule` — 음수면 `+0xbd` 로그 후 끝 | 호환 처리 |
| 6 | **`+0xbf ~ +0xcf`** | **`항목->LoadContext(+0xb0) ≠ 0` 이고 `LoadContext +0x20` 의 비트 0 이 0** 이어야 7 로. 아니면 지금 값으로 끝 (`+0xcf JNZ +0xfe`) | **작업 지시서 확인** |
| 7 | `+0xd6` → **`+0xef`** → `+0xfe` | `LdrpAcquireLoaderLock` → **`LdrpInitializeGraphRecurse(노드, 상태 칸, &바이트 0)`** → `LdrpReleaseLoaderLock` | 열쇠 쥐고 **전원 켜기**, 열쇠 놓기 |

- **새 말 — shim**: 옛 프로그램이 새 Windows 에서 돌도록 끼워 넣는 호환용 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 5번 실패 로그 글귀가 "Failed to load for appcompat reasons" (appcompat = 앱 호환) 라서 그쪽 일로 보인다 `[추측]`.
- 7번의 `+0xef` 가 run7 에서 실제로 본 자리다 `[관찰]`.
- 락 풀기 `EDX` 값이 여기선 2, `LdrpInitializeProcess +0x1bc7`(call) 에선 9 다 `[정적]`. 호출 자리 표시 값으로 보인다 `[추측]`.

#### `+0xef` 켜기를 **안** 타는 조건 — 코드에 쓰인 것 전부 `[정적]`

- (a) 1번: 알림 락 주인 직원이면
- (b) 2번: 도장이 8·9 이거나, 5 ~ 9 밖이면
- (c) 4·5번: 알림·shim 이 음수면
- (d) 6번: 작업 지시서(`LoadContext`)가 없으면
- (e) 6번: **작업 지시서 `+0x20` 의 비트 0 이 1 이면**

#### 두 caller 를 나란히

| | kernel32 길 | exe 길 |
|---|---|---|
| 부르는 곳 | `LdrpLoadDllInternal +0x284` | `LdrpInitializeProcess +0x1aed` (call) |
| 작업 지시서(로드 문맥)를 만든 곳 | `LdrpFindOrPrepareLoadingModule` (본문 안 읽음) | `LdrpInitializeProcess` 지역 `[RSP+0x2b0]` (`+0x1351`, run19) |
| 지시서 `+0x20` 비트 0 | 이 두 함수 안엔 **켜는 줄이 없다** `[정적, 이 두 함수 안]`. 실제 값은 동적 | **`+0x1a94` 에서 켠다** `[정적]` |
| `Prepare` 안에서 | `+0xef` 켜기 | `+0xcf` 에서 **건너뜀** `[정적]` |
| 켜기는 누가 | `Prepare +0xef` — run7 `[관찰]` | `LdrpInitializeProcess +0x1bbb` — run8 `[관찰]` |

#### exe 길을 한 단계씩 — `+0x1a94` 의 비트 하나

1. `LdrpInitializeProcess +0x1351` 에서 지역 `[RSP+0x2b0]` 에 **작업 지시서**를 만들고, 그걸로 exe 명부 줄을 만든다 (`LdrpAllocateModuleEntry`, run19) `[정적]`.
   - 이 지시서가 exe 항목의 `LoadContext`(+0xb0) 칸에 들어간다는 건 run19(sast/14) 의 연결이다.
2. 지시서의 `+0x20` 칸 = `[RSP+0x2d0]` 이다. run19 해설 2.6 에서 이 칸을 "플래그" 칸으로 봤다 `[정적]`.
3. `+0x1a8e` 에서 `R12D = 1`, 이어서 이 한 줄 `[정적]`:
   ```
   +0x1a94  OR dword ptr [RSP+0x2d0], R12D
   ```
   → 플래그 칸의 **비트 0 을 켠다.**
4. 그 **뒤**에 `+0x1aa4 LdrpMapAndSnapDependency` (dll1·dll2 들이기 후보) 와 `+0x1aed LdrpPrepareModuleForExecution(exe 항목, …)` 을 부른다 `[정적]`.
5. `Prepare` 는 6번에서 비트 0 이 1 인 걸 보고 `+0xcf` 에서 **켜기를 건너뛴다** `[정적]`.
6. 켜기는 나중에 `LdrpInitializeProcess` 가 직접 한다: `+0x1b91` 로더 락 → **`+0x1bbb LdrpInitializeGraphRecurse`** `[정적]`.
7. run8 이 dll1·dll2 켜기를 `+0x1bbb` 아래에서 본 것 `[관찰]` 과 **맞는다.**

#### kernel32 길은 왜 켜나

1. `LdrpLoadDllInternal +0x284` 가 넘기는 항목의 작업 지시서는 `LdrpFindOrPrepareLoadingModule` 이 만든다 `[추측: 역할]`.
2. `LdrpLoadDllInternal`·`LdrpPrepareModuleForExecution` 두 함수 안에는 그 비트를 켜는 줄이 **없다** `[정적, 이 두 함수 안]`.
3. 비트가 0 이면 6번을 통과해 `+0xef` 에서 켠다 `[정적]`.
4. run7 이 `+0xef` 에서 켜기를 본 것 `[관찰]` 과 **어긋나지 않는다.**
5. 다만 비트 값은 아래 callee (본문 안 읽음) 가 정하니 **실제 0 인지는 확인 전**이다 → **Q24** (`ntdll+0x86e4c` 입구에서 `db poi(@rcx+0xb0)+0x20 L1`).

#### 이 비트의 이름은?

- **모른다.** 작업 지시서 구조체 `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없다 (앞선 선언만).
- 뜻 "켜기는 부른 쪽이 따로 한다" 는 `[추측]` 이다 (run 문서).
- 왜 exe 길만 미루나 — 내 짐작 `[추측]`: `+0x1bbb` 는 **exe 의 연결 카드 하나를 들고** 그래프를 한 번에 돈다 (run19 해설 2.8 `[정적]`). 그리고 실제 순서가 dll2 → dll1 이었다 (run8 `[관찰]`). 그러니 import DLL 들을 하나씩 켜지 않고 **다 들인 뒤 한꺼번에 의존 순서대로** 켜려는 것으로 보인다. 코드로 확인한 건 아니다.

#### 비유로 정리

- 켜기 준비 담당(`Prepare`)은 일을 시작하기 전에 **작업 지시서**를 본다.
- 세팅 담당(`LdrpInitializeProcess`)이 exe 지시서에는 미리 "**전원은 내가 직접 켬**" 도장(`+0x1a94`, 비트 0)을 찍어 두었다.
- 그래서 exe 때 준비 담당은 알림만 돌리고 전원은 안 건드린다. 세팅 담당이 나중에 **관리실 열쇠를 쥐고 한꺼번에** 켠다 (`+0x1bbb`).
- kernel32 지시서에는 그 도장이 없어 보인다. 그래서 준비 담당이 **그 자리에서** 켠다 (`+0xef`).
- 같은 사람, 같은 절차. 지시서의 **도장 하나**가 다르다.

### 2.8 핵심 ② 둘째 `LdrLoadDll("KERNELBASE.dll")` 은 왜 시작 함수를 안 부르나

#### 출발점

- `[관찰]` (run7) 둘째 호출에선 시작 함수(`LdrpCallInitRoutine`)가 **안 불렸다.**
- `[추측]` (run20) 둘째는 "이미 있는 걸 다시 찾기" 일 것이다.
- 이번엔 그 "다시 찾기" 가 `LdrpLoadDllInternal` 안 **어디서 끝나는지** 봤다. 답은: 정적으로는 **두 길이 다 가능**하다.

#### 길 (가) — 빠른 끝 `+0xb8`

1. `(1) LdrpFastpthReloadedDll` 이 "이미 있음" 으로 0 이상을 준다.
2. → 상태 칸에 쓰고, 로그, `RET` `[정적]`.
3. → `(11) +0x284` 까지 **아예 안 간다.** 그러니 켜기도 없다.

- callee 이름(`LdrpFindLoadedDllByName`, `LdrpIncrementModuleLoadCount`)으로는 이쪽이 어울린다 `[추측]`.
- 비유: 창구 직원이 색인 카드함을 보고 "이미 있어요" 하고 번지만 적어 준다. 끝.

#### 길 (나) — 끝까지 가지만, 도장이 "다 켜짐"

1. `(1)` 이 빠른 끝을 안 준다 → 느린 길.
2. `(5) LdrpFindOrPrepareLoadingModule` 이 **이미 있는 줄**을 찾는다 → 0 이상 (`0xc0000135` 아님) → `(6)` 배송 안 함 `[정적]`.
3. → `(7)` 게시판 정리 → `(9)` 바뀐 건물 처리 → (작업 지시서가 없으면) `(10)` 건너뜀 → **`(11) +0x284` 까지 간다** `[정적]`.
4. `Prepare` 안 2번에서 노드 `State` 가 9 (`ReadyToRun`) 면 `+0x40` 에서 **바로 `return 0`** `[정적]`.
5. → `+0xef` 도, 그 아래 `LdrpCallInitRoutine` 도 안 불린다.

- 여기서 "kernelbase 노드가 9" 는 내가 붙이는 전제다 `[추측]`. 이유: kernelbase 의 시작 함수는 이미 첫째 호출 안에서 불렸다 (run7 `[관찰]`). 그러니 둘째 호출 땐 "다 켜짐" 도장일 것이다.
- 비유: 창구 직원이 바로 돌려보내진 않고 송장까지 만든다. 그런데 켜기 준비 담당이 연결 카드를 보니 "**다 켜짐**" 도장. 아무것도 안 하고 돌아간다.

#### 두 길 모두 관찰과 맞는다

| | (가) 빠른 끝 | (나) 끝까지 + 도장 9 |
|---|---|---|
| 어디서 끝나나 | `+0xb8` 뒤 `RET` | `+0x284` 까지 감, `Prepare +0x40` 에서 돌아옴 |
| `+0x284` 에 서나 | 안 섬 | 섬 |
| 시작 함수 | 안 불림 | 안 불림 |
| run7 `[관찰]` 과 | 맞음 | 맞음 |
| `+0x337` 고정 | 안 탐 | `LdrInitState == 0` 이라 다시 불릴 수 있음 `[정적]` |

- 가르는 값은 **`+0xb8` 의 `eax`** 하나다 → **Q49** (`ntdll+0x1fce8` 에서 `r eax`, 첫째·둘째 비교. 그리고 둘째 동안 `ntdll+0x1feb4` (`+0x284`) 에 서는지).
- (나) 에서 `+0x337` 이 다시 불리면 이미 고정된 걸 또 고정하는 건지는 callee 본문이라 모른다 (결과서).
- flow.md 그림 2 엔 둘째 `LdrpLoadDll` 아래에 `LdrpLoadDllInternal` 상자 `k15` 가 `(정적)` 으로 새로 생겼다. **둘째 호출 아래에서 이 함수를 콜스택으로 본 적은 아직 없다** (flow.md 4.1).

### 2.9 고정 — `+0x337 LdrpPinModule`, 팻말이 0 일 때만

- `+0x29b` 에서 `LdrInitState` 가 0 이면 `+0x337 LdrpPinModule(항목)` 을 부른다 `[정적]`.
  - 이 함수에서 `LdrInitState` 를 읽는 곳은 **여기 하나**다 (`_refs/LdrInitState.tsv`).
  - 닿으려면 `(11)` `Prepare` 와 `(12)` `LdrpBuildForwarderLink` 가 **둘 다 0 이상**이어야 한다.
- 팻말([run19 해설 2.12](run19-proc-init-modules-해설.md#212-팻말--ldrinitstate-1--2--3))은 `LdrpInitializeProcess +0x1a4c` 에서 1 이 된다. kernel32 준비(`+0x19a2`)는 그 **앞**이라 0 이다.
  - → **kernel32 준비 때 이 함수로 들인 항목은 고정된다** `[정적]`.

**새 말 — 고정 (pin)**: 이름으로는 모듈을 "**못 내리게 박아 두기**" 다 `[추측: 이름]`.

- 비유: 명부 줄에 "**철거 금지**" 도장. run19 에서 exe 줄에 `LoadCount` -1 로 찍은 도장과 비슷한 뜻으로 보이지만, 같은 장치인지는 모른다 `[추측]`.
- 왜 하필 팻말 0 일 때만인지도 코드로는 안 보인다. 세팅 초반에 들인 기본 설비(kernel32 등)는 가게가 끝날 때까지 두려는 것으로 보인다 `[추측]`.
- 실행 확인: **Q49** — `ntdll+0x1fecb` (`+0x29b`) 에서 `dd ntdll+0x1d23c8 L1` 이 0 인지, `LdrpPinModule` 이 불리는지.

### 2.10 run20 질문 둘이 풀렸다 — 상태 칸, 그리고 넷째 인자 `4`

#### 상태 칸을 채우는 쪽이 여기다 `[정적]`

- run20: "`LdrpLoadDll` 은 상태 칸 값을 돌려준다. **칸에 실제로 쓰는지는 Q11**."
- 이번: `LdrpLoadDllInternal` 이 상태 칸에 쓰는 자리 **7곳** — `+0xd0`(빠른 끝), `+0x30b`, `+0x31c`, `+0x35f`, `+0x372`, `+0x284` 결과, `+0x295` 결과.
- 그리고 이 칸엔 들어올 때 이미 `LdrpPreprocessDllName` 결과가 들어 있다. `+0x274` 에서 그 값이 0 이상이어야 `Prepare` 를 탄다 `[정적]`.
- 비유: 결과 메모지는 창구가 **여러 군데에서** 고쳐 적는다. 마지막 로그(`Status: 0x%08lx`)도 그 메모지를 읽어 찍는다.

#### 넷째 인자 `4` = 적재 이유 자리 `[정적]`

**새 말 — 적재 이유 (`LoadReason`)**: "이 DLL 을 **왜** 올리나" 를 번호로 적는 칸. 명부 줄(`_LDR_DATA_TABLE_ENTRY`)의 `+0x10c` 칸이고, 번호 이름은 PDB 열거 `_LDR_DLL_LOAD_REASON` 에 있다 `[정적]`.

- 이번에 나온 번호 둘: 4 = `DynamicLoad`, 9 = `PatchImage`.
- 단계별로:
  1. run20: 넷째 인자는 `4`. 이게 "적재 이유" 인지는 `[추측: 값이 같다는 것만]` 이었다.
  2. 이번: 이 인자가 함수 안에서 **9 와 비교되는 곳이 6곳**이다 (`+0x9e`, `+0xda`, `+0x18a`, `+0x2a8`, `+0x2d1`, `+0x356`) `[정적]`.
  3. `+0x240` 교체 처리 뒤, 새 명부 줄의 `LoadReason(+0x10c) == 9` 와도 대조된다 (맞으면 상태 칸에 `0xc00004c0`, `+0x35f`) `[정적]`.
  4. 9 = `PatchImage` 이고, 9 일 때만 패치 전용 갈래(`LdrpFindLoadedDllByHandle`·`LdrpQueryCurrentPatch`·`LdrpApplyPatchImage`)를 탄다 `[정적]`.
  5. → 이 자리는 **적재 이유로 쓰인다** `[정적]`. 다만 4 의 이름 `DynamicLoad` 는 값이 같다는 것뿐 `[추측]`.
- 비유: 주문서 구석의 "**주문 사유**" 칸. `4` = 일반 주문, `9` = 수리(패치) 주문. 수리 주문일 때만 다른 창구로 돈다.
- kernel32 준비는 이유 4 라 패치 갈래는 안 탄다 `[정적]`.

### 2.11 잡일 셋 — 게시판, 메인 직원 표시, 우회 검사

`(2)`·`(3)`·`(4)`, `(7)`·`(8)` 이다. 짧게.

**새 말 — `LoadOwner`**: 직원 개인 안내판(TEB) `SameTebFlags` 칸의 `0x1000` 비트, PDB 이름이다 `[정적]`. 이름으로는 "이 직원이 지금 **짐 들이기 주인**" 표시 `[추측: 이름]`.

- 순서 `[정적]`:
  - `+0x15f` `LoadOwner` 가 꺼져 있으면 `(2) +0x18a LdrpDrainWorkQueue(0)` (게시판 비우기). 켜져 있으면 건너뛴다.
  - `(3) +0x1a0 LdrpThreadTokenSetMainThreadToken()` — 이름으로 "메인 직원 표 달기" `[추측: 이름]`.
  - `(4) +0x1c0 LdrpDetectDetour()` — 이름으로 "우회(가로채기) 장치 검사" `[추측: 이름]`.
  - `(7) +0x21a LdrpDrainWorkQueue(1)` — **늘** 탄다 (`(6)` 을 안 탄 길도).
  - `(8) +0x317` 전역 `LdrpMainThreadToken ≠ 0` 이면 Unset.
- **kernel32 준비 때도 지나가나?** `[정적]`
  - 이 함수에서 `LdrInitState` 조건은 `+0x29b` 하나뿐이다.
  - 게시판·토큰은 `LoadOwner` 와 이유 9 로만 갈린다.
  - → 병렬 로딩을 켜기 전(`+0x1a42` 앞)인 kernel32 준비 때도 이 호출 자리들을 **지나가는 코드 모양**이다.
  - Set 이 전역 토큰을 **실제로 쓰는지**는 callee 본문이라 모른다 → **Q49** (`ntdll+0x1fdd0` 뒤 `dq ntdll+0x1d2618 L1`).
- `LdrpInitializeProcess` 에도 같은 짝이 있다 `[정적]`: `+0x1a8e` Set → `+0x1ab1 LdrpDrainWorkQueue(1)` → `+0x1ab4` 토큰 읽기 → Unset. 같은 모양의 앞뒤 세트다.
- 비유: 창구에 앉기 전에 **게시판을 한 번 비우고**, "메인 직원 근무 중" 표를 걸고, 배송 뒤 게시판을 **다시 비우고** 표를 뗀다.

### 2.12 그 밖에 확인된 것

- **caller 는 둘**이다 `[정적: 지도]`: `LdrpLoadDll +0x100`, `LdrpLoadForwardedDll +0x2e6`. 요청서는 "3" 이라 했지만 지도상 둘이다. run 문서에 쓴 적 없는 요청서 숫자라 **정정은 없다.**
- **경로 구조체(`RDX`)는 `(5) FindOrPrepare` 로만 간다** `[정적]`. `(1) Fastpth` 엔 안 간다.
  - 그래서 run20 이 남긴 `LdrLoadDll` "경로 해제" 바이트 (`[RSP+0xbc]`) 를 켜는 곳은 이 함수 안이 아니고 그 아래일 것 `[추측]`. 아직 못 찾았다.
- **실패 길**: 상태가 음수면 결과 항목 칸 = 0, 참조 내리기 (`+0x2eb LdrpDecrementModuleLoadCountEx`, `+0x2f3 LdrpDereferenceModule`) `[정적]`.
- **예외 조각 `LdrpLoadDllInternal$fin$0`** (rva `0x1683b0`) `[정적]`: 예외가 나면 상태 `0xc0000005`, `LdrpLogError` → `LdrpDrainWorkQueue(1)` → (토큰 있으면) Unset → (`BL == 0` 이면) `LdrpDropLastInProgressCount`. 본체 정리와 **같은 순서의 마무리 세트**다.
  - 비유: 배송 중 사고가 나도 게시판 정리·표 떼기는 꼭 하고 퇴근한다.
- **작업 지시서 치우기** `(14) +0x2c8 LdrpFreeLoadContextOfNode` 는 exe 쪽 `LdrpInitializeProcess +0x1be7` 에서도 불린다 — 켜기 뒤에 지시서를 치우는 같은 짝 `[정적]`.
- **그래프 다루는 이름은 올리기 뒤·켜기 앞에 있다** `[정적]`: `(10) +0x262 LdrpCondenseGraph`, `Prepare +0x12f LdrpCondenseGraph`·`+0x7b LdrpNotifyLoadOfGraph`. 그래프를 **만드는** 함수는 다음 바퀴 Q12 (2.5 의 후보 이름들).
- **구조체 칸 이름**은 이번에 스크립트가 아니라 `llvm-pdbutil` 로 PDB 타입을 직접 읽었다. 도구 문제는 없었다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

이번 바퀴 `git diff -- runs/02_sprint/flow.md` 에서 바뀐 것만.

| 자리 | 전 (run20 뒤) | 후 (run21) |
|---|---|---|
| 기준 | ntdll.pdb (run17 ~ run20), 정적 근거 run9 ~ run20 | + **run21** |
| 두 그림 사이 설명 | run20 문단까지 | + run21 문단: `LdrpLoadDllInternal` 에 코드 순서 상자, 둘째 호출 상자, exe 쪽 `Prepare` 가 켜기를 건너뛰는 이유 |
| 그림 2 `(1) kernel32 준비` | `k4 LdrpLoadDllInternal` → `+0x284` → `k5` 하나 | 새 `(정적)` 상자 넷: `kf1` Fastpth, `kf2` FindOrPrepare, `kf3` ProcessWork, `kf4` PinModule. 화살표 `(1) +0xb8` ~ `(5) +0x337`. `k5` 상자에 켜기 조건 글 |
| 그림 2 둘째 `LdrLoadDll` | `k13` → `k14 LdrpLoadDll` 까지 | + `k15 LdrpLoadDllInternal` `(정적)` ("빠른 끝, 또는 State 9 라 바로 돌아옴 — 어느 쪽인지 동적") |
| 그림 2 `(1)~(2) 사이` `j3` | "exe 항목 실행 준비" | + "`+0x1a94` 가 로드 문맥 비트 0 을 켜 둬서 그래프 켜기 `+0xef` 는 건너뜀" |
| 2.5 "올리기" 칸 | `[공백]` + 부르는 호출·이름 `[정적]` (run20) | 실행은 `[공백]`, **올리기가 일어나는 호출 자리와 시스템 콜까지의 이름 사슬** `[정적]` (run21) |
| 2.5 트리 | `LdrpLoadDllInternal` → `+0x284` 한 줄 | `[a]` ~ `[h]` 코드 순서를 관찰 콜스택 사이에 끼움. 둘째 `LdrpLoadDll` 아래 줄 추가 |
| 2.5 `LdrLoadDll` 안쪽 | 인자 9개·상태 칸 (run20) | + 상태 칸 7곳, 적재 이유, `LdrpLoadDllInternal` `[a]` ~ `[h]` 설명, caller 둘 |
| 2.5 "올리는 순간" 제목 | `[공백]` | "코드 길 `[정적]` run21, 실행은 `[공백]`" + KnownDll 길·파일 길·만나는 곳·의존 DLL 길·둘째 호출 두 길·Q20 멈춤 자리 `ntdll+0xa399` |
| 2.6 | `[추측]` "kernel32 와 같은 방식" | + run21 `[정적]`: 같은 `LdrpMapAndSnapDependency` 가 kernel32 올리기 길 안에도 있음. 같은 Set·Drain 짝 |
| 2.7 | 두 자리 역할 나눔은 Q15·Q24 | + run21 `[정적]`: `Prepare` 순서 4단계, **exe 길이 켜기를 건너뛰는 이유 = `+0x1a94` 비트** |
| 4장 공백 표 | 2.5 올리는 순간·그래프 줄 | 기존 줄에 run21 (일부 해결) 덧붙임 + 새 줄: (해결됨) `LdrpLoadDllInternal` 순서, 실행 값 (Q49·Q20·Q24), 이름·뜻 못 정한 것 (run21). run20 "이름·뜻" 줄의 `4` 는 해결됨 |
| 4.1 | `(1)~(2) 사이` 줄 | + run21 (`(10)` 이 켜기를 건너뛰는 코드), 새 줄 둘 (`LdrpLoadDllInternal` 상자들, 둘째 `LdrpLoadDllInternal`) |
| 5장 | run20 까지 | run21 줄, 요청서 번호 표 run21 줄 |

태그로 보면 핵심 변화는 여섯이다:

- **`LdrpLoadDllInternal` 이 무엇을 어떤 순서로 하나**: 모름 → **`[정적]`** (4장 "해결됨" 줄).
- **"`+0x284` 앞부분에서 올린다"**: 머리 태그 `[추측]` 그대로, 밑에 **`[정적]` "코드 순서로 맞다"** 근거가 붙었다. 실행은 Q20.
- **"kernelbase 가 같은 `LdrLoadDll` 안에서 따라 올라온다"**: `[추측]` 그대로, **`[정적: 지도]` "그 길이 있다"** 가 붙었다 (핵심 ①).
- **둘째 `LdrLoadDll(KERNELBASE)`**: `[추측]` "다시 찾기" → **`[정적]` "두 길 중 하나, 어느 쪽이든 시작 함수는 안 불림"** (핵심 ②). 어느 쪽인지는 Q49.
- **켜기 두 자리의 역할 나눔 (2.7)**: 공백 → **(일부 해결, `[정적]`) `+0x1a94` 비트** (핵심 ③). 비트 이름과 kernel32 쪽 비트 값은 남음.
- **넷째 인자 `4`·상태 칸**: `[추측]`·"Q11" → **`[정적]`** (적재 이유 자리, 상태 칸 7곳). 4 의 이름만 `[추측]`.

그림 2 를 볼 때 주의:

- `k4` 에서 나가는 화살표 `(1)~(5)` 중 **`(4) +0x284` 만 관찰**된 것이다. 나머지 넷은 `(정적)` 상자다.
- `k15` (둘째 `LdrpLoadDllInternal`) 는 **코드상 늘 부르는 자리**라 그렸지만, 콜스택으로 본 적은 없다.

### 큰 그림에서 어떻게 이어지나

- run20 은 kernel32 준비 방의 **주문서**를 봤고, run21 은 그 주문서가 가는 **창구**를 봤다.
  - 창구의 앞쪽(`+0x284` 앞) = 짐 들이기, `+0x284` = 전원 켜기 준비, 뒤쪽 = 고정·정리.
- 이제 2.5 의 사슬이 **코드로는** 이렇게 이어진다:
  - `LdrpInitializeKernel32Functions +0xc0` → `LdrLoadDll` → `LdrpLoadDll` → `LdrpLoadDllInternal` → (찾거나 준비 / 배송) → … → **`NtMapViewOfSection`** (지도)
  - 그리고 같은 창구의 `+0x284` → `Prepare +0xef` → 켜기 (run7 `[관찰]`)
- 켜기 쪽은 2.7 과 이어졌다: **kernel32 는 창구에서 바로 켜고, exe import 는 세팅 담당이 `+0x1bbb` 에서 한꺼번에 켠다.** 그 차이가 비트 하나라는 게 코드로 보였다.
- **다음 구간** (`plan.md` "다음 항목"): **Q12 — 올리면서 의존 관계 그래프를 만드는 함수** (정적).
  - 이번에 넘긴 후보 이름: `LdrpAllocateModuleEntry`, `LdrpMapAndSnapDependency`/`LdrpSnapModule`/`LdrpLoadDependentModuleInternal`, `LdrpCondenseGraph` → `LdrpCondenseGraphRecurse` → `LdrpMergeNodes`.
  - 그 뒤 Q15 에서 그래프를 **도는** `LdrpInitializeGraphRecurse` 본문을 본다. 두 자리(`Prepare +0xea`, `LdrpInitializeProcess +0x1bb6`)가 **같은 3 인자**로 부른다는 게 이번에 나왔다 `[정적]`.
- 버스를 탈 때(동적) 볼 것은 새 번호 없이 **Q20** (올리기 순간), **Q24** (`Prepare` 입구의 도장·지시서·비트), **Q49** (두 `LdrLoadDll` 안 실행 값) 에 붙었다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| kernel32·kernelbase 가 **실제로** 어느 길(KnownDll / 파일)로 올라오나, kernelbase 가 첫째 `LdrLoadDll +0xc0` 아래에서 의존 DLL 길로 올라오나 | 지도는 "길이 있다" 까지. 각 칸의 조건은 callee 본문이라 안 읽었다 | **Q20** (동적): `ntdll+0xa399` 에서 `k`, `ntdll+0x1fe22` (`+0x1f2`) 에서 `r eax` (`0xc0000135` 인가), `du poi(poi(@rsp+0x40)+0x60)` |
| 둘째 `LdrLoadDll(KERNELBASE)` 이 (가) 빠른 끝인가 (나) `+0x284` 까지 가나 | `LdrpFastpthReloadedDll` 결과는 실행 때 값 | **Q49** (동적): `ntdll+0x1fce8` (`+0xb8`) `r eax` 첫째·둘째 비교, `ntdll+0x1feb4` (`+0x284`) 에 서나 |
| kernel32 때 작업 지시서 `+0x20` 비트 0 이 정말 0 이고, exe 때 1 인가. `+0xef` 가 어느 모듈 호출에서 오나 | 비트는 callee(`FindOrPrepare`)가 정한다 | **Q24** (동적): `ntdll+0x86e4c` 입구에서 `dd poi(@rcx+0x98)+0x38 L1`, `dq @rcx+0xb0 L1`, `db poi(@rcx+0xb0)+0x20 L1`, `du poi(@rcx+0x60)`; `ntdll+0x86f3b` (`+0xef`) |
| `LdrpThreadTokenSetMainThreadToken` 이 전역 토큰을 실제로 쓰나 | callee 본문 | **Q49** (동적): `ntdll+0x1fdd0` 뒤 `dq ntdll+0x1d2618 L1` |
| `+0x29b` 에서 `LdrInitState` 가 0 이고 `LdrpPinModule` 이 불리나 | 실행 때 값 | **Q49** (동적): `ntdll+0x1fecb` 에서 `dd ntdll+0x1d23c8 L1` |
| 의존 관계 그래프를 **만드는** 함수와 시점, exe `State` 9 → 2 의 까닭 | 이번 범위 밖 (후보 이름만 모음) | **Q12** (정적, 다음 항목) |
| `LdrpInitializeGraphRecurse` 가 어느 노드부터 어떻게 도나 (kernel32 노드 vs exe 노드) | 그 본문은 Q15 몫 | **Q15** (정적) |
| `LdrpQueueWork` 로 붙인 일감이 병렬 로딩 전엔 어디서 도나 | callee 본문 | **Q47**·**Q20** (동적) |
| `LdrpFastpthReloadedDll`·`LdrpFindOrPrepareLoadingModule`·`LdrpProcessWork` 의 역할, 작업 지시서 `+0x20` 비트 0·`+0xc8` 칸 이름, `State` 7 때 알림·shim 을 건너뛰는 이유, `Prepare` 첫 검사의 뜻, 락 풀기 `EDX` 2 / 9 의 뜻 | 본문을 안 읽었거나, PDB 에 칸 정보가 없다 (`_LDRP_LOAD_CONTEXT`) | flow.md 4장 "이름·뜻 못 정한 것 (run21)" 줄 (대기열 줄은 따로 없다) |
| `LdrLoadDll` "경로 해제" 바이트 `[RSP+0xbc]` 를 켜는 곳 | 이 함수 안엔 없다. 경로 구조체는 `FindOrPrepare` 로만 간다 | 그 아래일 것 `[추측]`. 대기열 줄은 따로 없다 (flow.md 4장 run20 줄) |
| kernel32 가 정말 kernelbase 를 import 하나 | kernel32 의 import 표는 아직 안 봤다 | run20 해설 2.4 와 같음. Q20 콜스택으로 간접 확인 |

---

## 5. 더 읽기

- run 문서: [`runs/02_sprint/run21-load-dll-internal.md`](run21-load-dll-internal.md) — 특히 "`LdrpLoadDllInternal` 순서와 갈림", "인자 9개가 가는 곳", "지도 이름 사슬", "`LdrpPrepareModuleForExecution` 순서와 갈림", "둘째 `LdrLoadDll("KERNELBASE.dll")`", "호출 관계"
- 정적 요청서: [`_workspace/sast/16_load-dll-internal.md`](../../_workspace/sast/16_load-dll-internal.md)
- 정적 결과서 (질문 1 ~ 6, 구조체 칸 이름, 그림 2 상자 후보, 동적 멈춤점 표, Q12·Q15 연결점): [`_workspace/sast/16_load-dll-internal_result.md`](../../_workspace/sast/16_load-dll-internal_result.md)
- 앞 바퀴 해설:
  - [`run20-kernel32-load-해설.md`](run20-kernel32-load-해설.md) — 2.4 순서 문제, 2.11 상태 칸·인자 9개, 2.12 `0xc0000135`
  - [`run19-proc-init-modules-해설.md`](run19-proc-init-modules-해설.md) — 2.6 작업 지시서·연결 카드, 2.7 `+0x1aa4`, 2.8 `+0x1bb6` 켜기·로더 락, 2.9 일꾼·게시판, 2.12 팻말
  - [`run18-proc-init-setup-해설.md`](run18-proc-init-setup-해설.md) — 2.4 묶음 9 동네 공용 창고(`\KnownDlls`)
  - [`run10-allocate-process-해설.md`](run10-allocate-process-해설.md) — 매핑 올리기 (갖다 놓기)
  - [`run9-create-user-process-해설.md`](run9-create-user-process-해설.md) — 부르는 자리와 돌아올 자리, 이미지 섹션, 3.7 오프셋 순서 ≠ 실행 순서
- flow.md: [`runs/02_sprint/flow.md`](flow.md)
  - "두 그림 사이 설명" run21 문단
  - "그림 2" `(1) kernel32 준비` 의 `kf1`~`kf4`·`k5`·`k15`, `(1)~(2) 사이` 의 `j3`
  - "2.5" 트리 `[a]`~`[h]`, `LdrLoadDll` 안쪽, "kernel32·kernelbase를 메모리에 올리는 순간"
  - "2.6" run21 줄, "2.7" run21 줄 (`Prepare` 순서, `+0x1a94`)
  - "4. 공백 목록" 2.5·2.6·2.7 줄들, "4.1"
  - 이번 바퀴에 바뀐 것만 보려면 `git diff -- runs/02_sprint/flow.md` (커밋 뒤엔 그 커밋의 `git show`)
- 진행표: [`_workspace/plan.md`](../../_workspace/plan.md) — Q11(끝), Q12(다음), Q15·Q20·Q24·Q49 출처

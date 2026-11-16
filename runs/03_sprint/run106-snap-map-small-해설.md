# run106 snap-map-small — 해설

> 이 글은 [run106 문서](run106-snap-map-small.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

> **고쳐 읽기 (run123)**: 2.9 · 4절의 "`0x40000036` 이름은 `[추측: 기억]` 그대로" 는 닫힘 — **`[정적]` `STATUS_IMAGE_AT_DIFFERENT_BASE`** (`0x40000003` = `STATUS_IMAGE_NOT_AT_BASE`). PDB 엔 여전히 없고, 같은 빌드 `nltest.exe` 의 값 → 이름 표 (rva `0x64770` · `0x64440`) 에서 바이트로 확정. 뜻 ("fixup 은 자동") 과 ntdll 이 재배치를 안 하는 갈래는 그대로. 본문은 그대로 두었다 → [run123 해설](run123-run58-60-leftovers-해설.md) 2.3

- 대기열: **Q69 + Q70** 묶음 (`_workspace/plan.md`, 이번에 **둘 다 끝**)
  - **Q69**: snap 실패 길 작은 정적. 노드 `State = −3` (`LdrModulesSnapError`) 을 **처음** 쓰는 곳은 어디인가, 그리고 `LdrpSnapModule` 실패 반환이 `*(ctx+0x28)` 에 들어간 뒤 누가 그걸 State 로 바꾸나. run36 이 남긴 질문이다.
  - **Q70**: 매핑 갈래 작은 정적. 로드 문맥 `ctx+0x20` 비트 `0x800` · `0x800000` 을 켜는 줄, 그리고 결과 코드 `0x40000036` 의 이름. run38 이 남긴 질문이다.
  - 둘 다 ntdll 로더 쪽 작은 정적이고 대기열에서 붙어 있어서 **한 요청 · 한 바퀴** 로 묶었다.
  - 남은 것이 간 곳 (4절):
    - 동적: **Q20** (run106 몫)
    - 새 샘플: **Q194** (snap 실패 변종 dll. 만들지는 사용자가 정한다)
    - 정적: **Q193 ① ~ ③**, **Q188 ⑤** (`0x40000036` 커널 쪽)
- 근거:
  - run 문서 [`run106-snap-map-small.md`](run106-snap-map-small.md) (가장 중요)
  - 요청서 [`_workspace/sast/100_snap-map-small.md`](../../_workspace/sast/100_snap-map-small.md) · 결과서 [`100_snap-map-small_result.md`](../../_workspace/sast/100_snap-map-small_result.md) (판정 **분석성공**)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 그림에서는 **그림 2 `jm1` 상자 글만** 바뀌었다. 글 쪽은 2.4 · 2.6 · 2.7 · 4장에 줄이 붙었다 (3절).
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` (기준과 같다). 다시 analyze 하지 않았다.
  - 값 대조는 run44 사진 로그를 **다시 읽은 것**뿐이다. 그래서 이 글의 `[관찰]` 은 전부 run44 사진에서 온다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **전파 (propagate)** → 2.4
- **enclave** → 2.6
- **메시지 표** → 2.9
- **대조군** → 2.3

### 이번 바퀴에서 조심할 곳 넷

1. **"코드에 이런 길이 있다" 와 "test.exe 가 이 길을 탔다" 는 다르다.**
   - 이번 질문 넷 중 셋은 test.exe 가 **안 타는 길**이다. snap 실패 길, WoW64 길, enclave 길.
   - 그래서 이 글은 계속 둘을 나눠 쓴다. "길이 이렇게 생겼다" 는 `[정적]`, "test.exe 는 안 탔다" 는 `[정적+관찰]` 이나 `[정적+소스]` 다.
2. **run 문서가 결과서보다 한 발 더 나간 곳이 하나 있다.**
   - 결과서는 `UseWOW64` 값을 "x64 면 0 `[추측: 값 안 봄]` → VM 에서 보자" 로 적었다.
   - run 문서는 그 값이 **run44 사진에 이미 0 으로 있다**는 걸 찾아서 `[정적+관찰]` 로 좁혔다. 이 글은 run 문서를 따른다 (2.8).
3. **같은 숫자 `0x800000` 이 서로 다른 두 가지로 나온다.** 하나는 지시서 비트, 하나는 창구 신청서의 "할당 종류" 칸 값이다. 2.7 에서 따로 떼어 놓는다.
4. **오프셋은 call 자리다.** ret 자리 (부른 함수가 돌아오는 곳) 는 run 문서 "정적 — 호출 지점" 표에 있다.
   - 다만 run 문서 "호출 관계" 트리는 **ret 자리**로 적혀 있다. 이 글 2.1 트리도 그대로 옮겼다. 본문과 트리 숫자가 조금씩 다르면 call / ret 차이다.

### 비유 — 그대로 이어 쓴다

이미 푼 말은 **링크만** 건다.

- 커널 = **관리사무소**, 시스템 콜 = **신청 창구**, ntdll = 가게 안 **관리실** → [run38 해설 "비유"](../02_sprint/run38-map-module-해설.md#비유--그대로-이어-쓴다)
- DLL = **건물**, 모듈 항목 = **명부 한 줄**, DDAG 노드 = 명부 줄마다 붙은 **연결 카드** → [run19 해설 2.6](../02_sprint/run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1)
- 노드 `State` = 카드의 **진행 도장** → [run21 해설 2.6](../02_sprint/run21-load-dll-internal-해설.md) · [run27 해설 2.1 "새 말 — 노드 State"](../02_sprint/run27-init-graph-해설.md#새-말--노드-state-진행-도장-이번에-쓰는-번호)
  - 도장 −3 `LdrModulesSnapError` = **주소록 채우기 (snap) 실패**, −4 `LdrModulesInitError` = **전원 켜기 실패** (run27 해설 같은 절)
- 로드 문맥 (`ctx`) = **작업 지시서** → [run19 해설 2.6](../02_sprint/run19-proc-init-modules-해설.md#26-exe-모듈-항목--명부-둘째-줄-묶음-1)
- 상태 칸 `*(ctx+0x28)` = **결과 메모지** → [run20 해설 2.11](../02_sprint/run20-kernel32-load-해설.md)
- snap = 건물마다 가진 **빈 주소록 (IAT) 칸을 채우는 일** → [run36 해설](../02_sprint/run36-snap-module-해설.md)
- `LdrpMapAndSnapDependency` = **건물 담당**, `LdrpLoadDependentModuleInternal` = **배달 기사**, `LdrpProcessWork` = **게시판 쪽지 처리** → [run36 해설](../02_sprint/run36-snap-module-해설.md) · [run24 해설](../02_sprint/run24-work-queue-해설.md)
- `LdrpCondenseGraph` = **그래프 정리** (서로 기대는 고리를 하나로) → [run24 해설 2.9](../02_sprint/run24-work-queue-해설.md)
- `LdrpPrepareModuleForExecution` = **전원 켜기** 쪽 (실행 준비) → [run21 해설](../02_sprint/run21-load-dll-internal-해설.md)
- 이미지 섹션 = **조립식 건물 세트**, 매핑 = 세트를 땅에 **갖다 놓기**, `NtMapViewOfSectionEx` 의 확장 인자 = 신청서에 붙이는 **조건 쪽지** → [run38 해설 2.3](../02_sprint/run38-map-module-해설.md)
- 재배치 = **번지수 고쳐 쓰기**, `0x40000003` = 창구 대답 "**놓았어요. 그런데 원래 자리가 아닌 곳이에요**" → [run38 해설 2.6](../02_sprint/run38-map-module-해설.md#26-창구의-대답-가르기-정적) · [run102 해설 2.7](run102-image-reloc-해설.md)
- WoW64 = 64비트 Windows 에서 32비트 프로그램을 돌리는 장치로 알려져 있다 `[추측]` (일반적으로 알려진 설명). 이 글에선 **32비트 손님** → [run17 해설 2.9](../02_sprint/run17-ldr-init-entry-해설.md#29-나중-스레드-길--짧게)
- App Verifier 묶음 = **점검관이 온 날에만** 하는 일 → [run105 해설](run105-ntdll-small-해설.md)
- 사진 P1a · P1b · P2 → [run44 해설](run44-baggage-snap-해설.md)

run 문서는 이번에 이런 비유를 썼다. 이 글도 같은 그림이다.

- 주소 잇기 (snap) 가 실패하면 카드의 진행 도장은 **"잇는 중 (4)" 그대로**다. "잇기 실패 (−3)" 도장을 **처음** 찍는 사람은 없다.
- 실패는 지시서의 **결과 메모지**에만 적힌다. 그 메모지는 맨 처음 일을 받은 사람 (`LdrpLoadDllInternal`) 의 것이다.
- `0x800` · `0x800000` 은 지시서에 **중간에 덧칠하는 표시가 아니라**, 지시서를 처음 쓸 때 **받아 온 표시**다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) | 표시 |
|---|---|---|
| Q69 ① 도장 −3 을 **처음** 찍는 줄 | **없다.** ntdll 전체에서 −3 을 쓰는 줄은 전파 `LdrpCondenseGraphRecurse +0x14e` 하나뿐 | `[정적]` |
| Q69 ② 결과 메모지에 실패가 적힌 뒤 | **도장으로 바꾸는 사람이 없다.** 카드는 `State 4` 그대로. 바깥 `LdrpLoadDllInternal +0x277` 이 메모지를 보고 전원 켜기를 통째로 건너뛰고 뒤처리로 간다 | `[정적]` |
| Q70 ③ 비트 `0x800` · `0x800000` 을 켜는 줄 | `OR` / `BTS` 로 켜는 줄은 **없다.** 지시서를 만들 때 받은 flags 로 들어온다. 뿌리는 WoW64 (`LdrpLoadWow64 +0x98`) · enclave 둘. 의존 DLL 은 물려받는다 | `[정적]` |
| Q70 ③ test.exe 에서 타나 | **안 탄다.** `UseWOW64` 0, enclave 없음 | `[정적+관찰]` · `[정적+소스]` |
| Q70 ④ `0x40000036` 의 이름 | **PDB 에 이름이 없다.** ntdll 메시지 표 글 = "다른 주소에 붙었지만 fixup 은 자동으로 된다". 그래서 ntdll 은 이 값으론 재배치를 안 한다 | `[정적]` (이름은 여전히 `[추측: 기억]`) |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

전부 test.exe **첫 스레드의 ntdll 로더 안**이다. 커널 쪽 일은 이번에 새로 안 읽었다.

```
cmd.exe 의 부탁 → 커널 (가게 · 첫 직원 만들기, 01_sprint ~ 02_sprint)
 → 첫 직원이 ntdll 로 들어감 → LdrpInitializeProcess
     ├─ +0x1766  UseWOW64 를 보고 갈림 (32비트 손님 준비)            ← Q70 ③ (0x800 의 뿌리)
     └─ exe 의 import 건물 들이기 (dll1 · dll2 · ...)
          └─ LdrpLoadDllInternal (주문 받은 사람, 결과 메모지 주인)
               ├─ 건물 갖다 놓기  LdrpMinimalMapModule
               │     → 창구 NtMapViewOfSection  /  …Ex (조건 쪽지가 있을 때)   ← Q70 ③
               ├─ 마무리         LdrpCompleteMapModule (재배치 문)              ← Q70 ④
               └─ 주소록 채우기   LdrpSnapModule  — 실패하면 그다음은?           ← Q69 ① ②
 → … → NtContinue → exe 진입점 → main
```

- run36 은 주소록 채우기 (snap) 의 **성공 길**을 봤고, run38 은 갖다 놓기 · 마무리를 봤다.
- 이번엔 그 두 바퀴가 "작은 것" 으로 남긴 넷을 한꺼번에 닫는다.

### 무엇이 궁금했나

- **Q69**: 주소록 채우기가 실패하면 카드에 "실패 (−3)" 도장을 누가 **처음** 찍나? 그리고 실패가 결과 메모지에 적힌 뒤, 누가 그걸 보고 무엇을 하나?
- **Q70**: 갖다 놓기 창구를 `Ex` 쪽으로 돌리는 지시서 비트 `0x800` · `0x800000` 은 **누가 켜나**? test.exe 에서 켜지나? 그리고 창구 대답 `0x40000036` 의 **이름**은?

### 바퀴 전에 알던 것

- `[정적]` (run36) `LdrpSnapModule` 은 실패해도 도장을 안 쓴다. 시작 때의 4 (`Snapping`) 그대로다. 실패는 **반환값으로만** 나가고, caller 가 그걸 `*(ctx+0x28)` 에 옮긴다 (run22 · run24) → [run36 해설 2.8](../02_sprint/run36-snap-module-해설.md#28-질문-3--도장-5-는-한-곳-끝맺음-뒤)
- `[공백]` (run36) 그럼 −3 은 누가 처음 찍나? `.asm` 모음에서 −3 을 쓰는 줄은 `LdrpCondenseGraphRecurse +0x14e` 하나뿐이었다. 이건 "자식이 −3 이면 부모에게 옮기는" 줄이다.
- `[정적]` (run38) 갖다 놓기는 보통 `+0x174 NtMapViewOfSection`. 지시서 `+0x20` 비트 `0x800` · `0x800000` 이 켜졌을 때만 `+0x36c NtMapViewOfSectionEx`. 그런데 **켜는 줄은 뽑아 둔 함수에 0건**이었다 → [run38 해설 2.3](../02_sprint/run38-map-module-해설.md)
- `[정적]` (run38) 관리실 (ntdll) 은 창구 대답이 **정확히 `0x40000003`** 일 때만 번지수를 고친다. `0x40000036` 은 이름을 못 정했다 (`STATUS_IMAGE_AT_DIFFERENT_BASE` 기억 `[추측]`).
- `[정적]` (run102) 관리사무소 (커널) 쪽에서 `0x40000036` 이 나오는 자리 = `MiMapViewOfImageSection +0xc1b ~ +0xc82`. `0x40000003` 갈래 안에서 `BL` 비트 1 · 3 이 같이 켜질 때. `BL` 출처는 `[공백]` → [run102 해설 2.7](run102-image-reloc-해설.md)
- `[관찰]` (run44 사진) `UseWOW64` 는 끝까지 0. 노드 `State` 에 음수는 없다 (짐 지도 3.6 머리).

---

## 2. 찾은 것

### 2.1 한눈에 — 호출 관계 (run 문서 트리를 줄인 것, ret 자리)

```
(snap 실패 길 — test.exe 는 안 탐)
LdrpLoadDllInternal                         결과 메모지 = 이 함수의 상태 변수 (+0x1c0 / +0x1c8 → 지시서 +0x28)
 ├─ +0x21a  LdrpDrainWorkQueue
 │           └─ LdrpProcessWork              +0x20 메모지 < 0 → 남은 일감은 할 일 없이 끝
 │               └─ +0x131 LdrpSnapModule    실패: +0xbc1 EAX = 음수, 도장 안 씀 (4 그대로)
 │                  +0x13b  메모지 = 음수
 ├─ +0x262  LdrpCondenseGraph               도장 4 라 +0xa3 의 "6" 쓰기를 건너뜀
 │  +0x277  메모지 < 0 → +0x2b1             (+0x284 LdrpPrepareModuleForExecution 건너뜀)
 ├─ +0x2c8  LdrpFreeLoadContextOfNode
 ├─ +0x2eb  LdrpDecrementModuleLoadCountEx(항목, 0)
 │           └─ +0xb4 LdrpUnloadNode (지역 ≠ 0)   도장 4 → … → (조건) +0x1fb 도장 −2 ?
 └─ +0x2f3  LdrpDereferenceModule(항목)

(지시서 +0x20 비트가 태어나는 곳)
LdrpInitializeProcess  +0x1766 UseWOW64 == 0 → +0x176d 건너뜀   ← test.exe (사진 0)
 └─ +0x17c6 LdrpLoadWow64 → +0xad LdrpLoadDll(…, 0x800, …)
LdrLoadEnclaveModule            +0xe5  0x800000
LdrpEnclaveAddDependentModule   +0x9a  0x800000
LdrpLoadDependentModuleInternal +0x1e2 AND 0x1800800  (의존 DLL 이 물려받음)

(창구 대답)
LdrpCompleteMapModule  +0x1b4 정확히 0x40000003 → +0x320 LdrpRelocateImage
                       (0x40000036 은 +0x1c1 로, 재배치 안 함)
```

- 전체 표 (call / ret 둘 다) 는 run 문서 "정적 — 호출 지점".

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 도장을 옮겨 찍는 줄 (값이 바뀌는 순간)

`LdrpCondenseGraphRecurse +0x14e MOV [RDI+0x38], −3` `[정적]`

- **누가**: 그래프 정리 담당 `LdrpCondenseGraphRecurse`.
- **어디서**: `+0x14e`. 호출 줄이 아니라 **메모리에 쓰는 줄**이다.
- **누구에게**: `RDI` = 지금 보는 카드 (부모). `+0x38` = 카드의 `State` 칸 (진행 도장).
- **무엇을 위해**: 바로 앞 `+0x148` 에서 **자식** 카드 (`RSI`) 도장이 −3 인지 본다. 맞으면 (`+0x14c JNZ` 를 안 뛰면) 부모 도장도 −3 으로 찍는다.
- 곧 이 줄은 **자식이 이미 −3 이어야** 켜진다.

#### 한 줄 읽기 ② — 결과 메모지를 보고 갈리는 줄

`LdrpLoadDllInternal +0x274 CMP [RBX],0` → `+0x277 JL +0x2b1` `[정적]`

- **누가**: 주문을 처음 받은 `LdrpLoadDllInternal`.
- **어디서**: `+0x277`. 일감을 다 비운 뒤 (`+0x215 LdrpDrainWorkQueue`, ret `+0x21a`) 다.
- **무엇을 보나**: `RBX` 가 가리키는 곳 = 이 함수 자기 상태 변수 = 결과 메모지.
- **무엇을 위해**: 메모지가 음수 (실패) 면 `+0x2b1` 로 뛴다. 그러면 그 사이에 있는 `+0x27f LdrpPrepareModuleForExecution` (전원 켜기 쪽) 을 **통째로 건너뛴다.**

#### 한 줄 읽기 ③ — 비트가 태어나는 줄

`LdrpLoadWow64 +0x98 MOV R8D,0x800` → `+0xa8 LdrpLoadDll(이름, 경로, 0x800, &핸들)` (ret `+0xad`) `[정적]`

- **누가**: 32비트 손님 준비 담당 `LdrpLoadWow64`.
- **어디서**: `+0xa8` 에서 `LdrpLoadDll` 을 부른다. 그 앞 `+0x98` 에서 셋째 인자 (`R8D`) 에 `0x800` 을 담는다.
- **무엇을 위해**: "이 DLL 은 WoW64 용으로 올려라" 는 표시를 **주문서에 처음부터** 적어 보낸다. 이 값이 나중에 지시서 `+0x20` 에 들어간다 (2.6).

### 2.3 "없다" 를 어떻게 말할 수 있나 — 찾는 방법부터

이번 바퀴 답 둘이 "**그런 줄은 없다**" 다. 없다는 말은 있다는 말보다 어렵다. 그래서 먼저 어떻게 찾았는지 본다.

1. 이미 뽑아 둔 `.asm` 은 ntdll 함수 **4594개 중 683개**뿐이다. 이걸로는 "전부" 를 못 본다.
2. 그래서 결과서는 `objdump -d -M intel` 로 ntdll `.text` **전체**를 한 번 풀었다 (421,500줄, 스크래치 파일 — 커밋 안 함). 주소 → 함수 + 오프셋은 `_index.tsv` 로 붙였다.
3. 명령 모양 검색에 더해 **바이트 꼴** (`C7 /0 … FD FF FF FF` = "메모리에 −3 을 쓰는 명령" 의 바이트) 검색도 같이 했다.
4. **대조군**으로 방법을 맞춰 봤다.
   - **새 말 — 대조군**: 답을 이미 아는 것에 같은 방법을 써 보고, 방법이 제대로 찾는지 확인하는 것.
   - 비유: 금속 탐지기로 "이 모래밭엔 동전이 없다" 고 말하기 전에, **일부러 묻어 둔 동전** 하나를 잘 찾는지 먼저 시험한다.
   - 여기선 도장 −5 (`LdrpMergeNodes +0xd9`, `[RDI−8]` 꼴 — 카드 `+0x40` 을 기준으로 8 앞을 쓰는 모양이 다른 줄) 를 같은 방법이 잘 찾았다.
- **한계** (run 문서 · 결과서):
  - 다른 DLL (shim · verifier 등) 이 카드 칸을 직접 쓰는 경우는 못 잡는다.
  - 실행 중에 만든 주소로 쓰는 경우도 못 잡는다.
  - 숫자는 "**검색한 꼴 기준**" 이다 (run41 · run72 와 같은 방식 → [run41 해설](../02_sprint/run41-tls-lock-detail-해설.md)).

### 2.4 Q69 ① 도장 −3 — 처음 찍는 줄은 없고, 옮겨 찍는 줄만 있다

**새 말 — 전파 (propagate)**: 아래 노드 (자식 카드) 의 실패 표시를 위 노드 (부모 카드) 로 **옮겨 적는 것**.

- 비유: 반 학생 하나가 "결석" 이면 조 출석부에도 "결석자 있음" 이라고 옮겨 적는 것. 옮겨 적는 사람은 있어도, **처음 "결석" 이라고 쓰는 사람**은 따로 있어야 한다.

이번 답 `[정적]`:

- 카드의 도장 칸 = `_LDR_DDAG_NODE +0x38` (PDB, 카드 크기 `0x50`).
- ntdll 전체 `.text` 에서 "메모리에 `0xfffffffd` (= −3) 를 쓰는 줄" 을 기준 레지스터 · 오프셋 가리지 않고 찾으니 **1곳**: `LdrpCondenseGraphRecurse +0x14e`. 바로 위 2.2 ① 의 전파 줄이다.
- −3 을 **읽는** 줄도 `+0x148` 하나뿐이다.
- 레지스터에 −3 을 담아 두었다가 쓰는 길도 봤다.
  - `MOV r32,−3` 은 ntdll 전체에 `inflate` (zlib, 압축 풀기) 두 곳뿐이다.
  - 카드 `+0x38` 에 레지스터 값을 쓰는 줄 중 카드를 가리킬 수 있는 건 `LdrpInitializeNode +0x2ba` 하나. 그 값은 9 또는 −4 다 (`+0x2a5 MOV ECX,9` / `+0x2aa MOV EAX,0xfffffffc` 를 `CMOVNZ` 로 고름).

요청서가 의심한 후보 넷도 다 아니다 `[정적]`:

| 후보 | 도장 칸에 쓰는 줄 |
|---|---|
| `LdrpDoPostSnapWork` | 없음 (`+0xf` 는 지시서 `+0x38` 읽기) |
| `LdrpSnapModule` | `+0xb87` 도장 5 하나 (성공 쪽) |
| `LdrpProcessWork` | 없음. 쓰는 건 결과 메모지 `+0x13b` 뿐 |
| `LdrpMapAndSnapDependency` | `+0x4ac` 도장 4, `+0x5ae` 도장 5 |

그래서 어떻게 읽나 — 단계별로:

1. 전파 줄은 "자식이 이미 −3" 일 때만 켜진다.
2. 그런데 ntdll 안에는 **처음 −3 을 찍는 줄이 없다** `[정적]`.
3. 그러면 ntdll 코드만으로는 전파 줄이 켜질 출발점이 없다. run 문서 말로 "**−3 은 전파 줄을 위한 값만 남아 있다**" `[정적]`.
4. 다만 다른 DLL 이 카드에 −3 을 직접 쓰는 길이 있는지는 2.3 의 한계 밖이다 → 전파 줄이 실제로 켜질 길은 `[공백]`.

- run36 의 `[공백]` "처음 찍는 곳을 못 찾았다" 는 **못 찾아서가 아니라 없어서**였다. 닫혔다.

덤 — 도장 번호별로 찍는 줄 전수 표 (`2` ~ `9`, `−1` ~ `−5`) 가 run 문서에 있다. 여기선 −3 줄만 옮긴다.

| 도장 | 찍는 줄 |
|---|---|
| **−3** | **`LdrpCondenseGraphRecurse +0x14e` (전파) 뿐** |

- 표 안의 `LdrpInitializeProcess +0x16b0` (7) · `+0x1735` (9) 는 App Verifier 묶음 (`+0x1568 ~ +0x1753`, run105) 안 오프셋이다. 점검관이 온 날에만 도는 곳이라 test.exe 는 건너뛴다 `[정적: 오프셋 범위]`.

### 2.5 Q69 ② 실패는 결과 메모지로만 위로 간다 — 카드는 도장 4 그대로

#### 단계 1 — 주소록 채우기가 음수를 돌려준다 `[정적]`

- `LdrpSnapModule` 끝: `+0xbbc LdrpLogError(…, 0x19, …)` (ret `+0xbc1`) → `+0xbc1 MOV EAX,R12D`. 음수를 돌려준다.
- 도장은 안 쓴다. 이 함수가 도장을 쓰는 줄은 성공 쪽 `+0xb87` 하나다 (run36 그대로).

#### 단계 2 — 부른 쪽 둘이 **같은 메모지**에 적는다 `[정적]`

snap 을 부르는 자리는 둘이다 (run36). 둘 다 같은 칸 `*(ctx+0x28)` 에 적는다.

- **직접 길** (건물 담당):
  - `LdrpMapAndSnapDependency +0x4bd` (ret `+0x4c2`) → `+0x4c4 TEST ESI` → `+0x4c6 JNS` (음수면 안 뜀) → `+0x4d1 JMP +0x69` → `+0x69 MOV RAX,[RBX+0x28]` → **`+0x6d MOV [RAX],ESI`**.
  - 이 길은 부모 지시서가 **없을 때만** (`+0x4b6 CMP [R9+0x30],0`). 있으면 `+0x4bb JNZ` → `+0x4f2 LdrpQueueWork` (게시판에 쪽지).
- **일감 길** (게시판 쪽지 처리):
  - `LdrpProcessWork +0x12c` (ret `+0x131`) → `+0x133 TEST ESI` → `+0x135 JNS +0x15d` (음수면 안 뜀) → `+0x137 MOV RAX,[RBX+0x28]` → **`+0x13b MOV [RAX],ESI`**.
- 곁가지 둘:
  - 안내문 (forwarder) 대상 올리기가 실패하면 `LdrpSnapModule +0xdff` → `+0xe03` 이 **주소록 주인 쪽 지시서**의 메모지에 적는다. 역시 도장은 안 쓴다.
  - 예외 정리 함수 `LdrpProcessWork$fin$0 +0x12 / +0x16` 은 같은 칸에 `0xc0000005` 를 적는다.

#### 단계 3 — 그 메모지는 맨 바깥 사람의 것이다 `[정적]`

- `LdrpLoadDllInternal +0x1c0 MOV RBX,[RSP+0xc8]` → `+0x1c8 MOV [RSP+0x30],RBX` 로, 자기 상태 변수 주소를 지시서 만드는 함수에 넘긴다. 그게 지시서 `+0x28` 이 된다.
- 그래서 **안쪽 어디서 적든, 바깥 `LdrpLoadDllInternal` 이 그 값을 본다.**
- 비유: 일을 맡길 때 맨 처음 주문 받은 사람이 **자기 메모지 한 장**을 지시서에 끼워 보낸다. 아래 누가 실패하든 그 한 장에 빨간 숫자를 적는다. 카드 도장은 아무도 안 건드린다.

#### 단계 4 — 누가 읽고 무엇을 하나 `[정적]`

1. 같은 게시판의 **다른 쪽지**: `LdrpProcessWork +0x1c` → `+0x20 CMP [RAX],0` → `+0x23 JL`. 메모지가 이미 빨간 숫자면 **아무것도 안 하고 끝**.
2. 바깥 `LdrpLoadDllInternal` (일감을 다 비운 뒤):
   - `+0x25d LdrpCondenseGraph` (그래프 정리) 는 (항목 `+0xb0` ≠ 0 이면, `+0x24c`) **실패여도 돈다.** 메모지 검사 `+0x274` 보다 앞이라서다.
     - 그래도 도장 4 인 실패 카드는 `LdrpCondenseGraphRecurse +0x96 CMP [RDI+0x38],5` → `+0xa1 JNZ` 로 `+0xa3` 의 "도장 6" 쓰기를 건너뛴다.
   - **`+0x274` · `+0x277`**: 메모지가 음수면 전원 켜기 쪽 (`+0x27f LdrpPrepareModuleForExecution`) 을 통째로 건너뛴다 (2.2 ②).
   - 뒤처리: `+0x2c3 LdrpFreeLoadContextOfNode` (ret `+0x2c8`) → `+0x2c8 CMP [RBX],0` · `+0x2cb JGE` 안 뜀 → `+0x2da` 돌려줄 모듈 핸들 = 0 → `+0x2e6 LdrpDecrementModuleLoadCountEx(항목, 0)` → `+0x2ee LdrpDereferenceModule(항목)`.
3. `LdrpDecrementModuleLoadCountEx`: `+0x8d LdrpDecrementNodeLoadCountLockHeld(노드, 0, &지역)` → `+0xa1` · `+0xa5` → 지역 칸 ≠ 0 이면 **`+0xaf LdrpUnloadNode`** (카드 치우기 쪽).
4. `LdrpUnloadNode` (rva `0x865c0`): 입구 `+0x1e` 에서 도장을 읽는다.
   - −4 면 `+0x32` 에서 −1. 7 · 9 는 따로 (`+0x29a` · `+0x2a3`).
   - **그 밖 (4 포함)** 은 `+0x2a6 JNE +0x138` 길. 그 길은 조건 (`+0x141` · `+0x144` 등) 에 따라 `+0x1fb` 도장 −2 에 닿을 수 **있다**.
   - **이 갈래 본문은 안 읽었다** `[공백]`. 도장 4 카드가 정말 −2 가 되는지는 모른다.

- 비유로 한 번에: 메모지에 빨간 숫자가 적히면, 주문 받은 사람은 **"전원 켜기" 단계를 건너뛰고 짐을 도로 싼다.** 카드 도장은 "잇는 중 (4)" 그대로 두고, 치우기 담당에게 넘긴다.

#### 덧 — 만약 전원 켜기에 그런 카드가 갔다면 `[정적]`

- `LdrpPrepareModuleForExecution` 은 도장이 5 ~ 9 밖이면 `+0x40 SUB R8D,5 … +0x123 MOV EBX,0xc00000e5` ("An internal error occurred.") 로 끝난다 (결과서).
- 실제로는 위 `+0x277` 이 건너뛰어서 **안 닿는다.**

#### test.exe 는 이 길을 탔나 — 사진과 맞춰 보기

- run44 사진의 카드 도장: exe P1a 9 → P1b 7 → P2 9, dll1 · dll2 P1b 7 → P2 9, ntdll · k32 · kb 9 (짐 지도 3.6 머리) `[관찰]`.
- **음수 도장이 없다.** test.exe 는 snap 실패 길을 안 탔다 — 위 정적 결과와 맞는다.

### 2.6 Q70 ③ 비트 `0x800` · `0x800000` — 덧칠이 아니라 처음부터 받아 온 표시

#### 먼저 — 켜는 줄은 없다 `[정적: objdump 꼴]`

- ntdll `.text` 전체에서 `OR` · `BTS` 로 `0x800` · `0x800000` (또는 `BTS 0xb` / `0x17`) 을 넣는 로더 쪽 줄은 `LdrpGetDllPath +0x162 OR EBX,0xa00` 하나다.
  - 이건 **DLL 검색 경로 옵션** 레지스터라 지시서에 안 들어간다.
- 지시서 `+0x20` 에 비트를 켜는 로더 줄은 여섯이다. 그런데 **전부 다른 비트**다.
  - `LdrpMapDllSearchPath +0x3c4 OR 0x10000` · `LdrpAppCompatRedirect +0xb1 BTS 0x10` · `LdrpMapAndSnapDependency +0xd6 OR 0x2000000` · `LdrpProcessMachineMismatch +0x37 BTS 0x15` · `LdrpMapDllPatchImage +0x70 BTS 0x1a` · `LdrpLoadDependentModuleInternal +0x1ca1 BTS 0x1a`
- 나머지 `MOV [ctx+0x20], 레지스터` 는 **지시서를 새로 만들 때 한 번 채우는 줄**이다.
- 그래서 결론: 두 비트는 중간에 켜지는 게 아니라, **지시서를 만들 때 받은 값**에 이미 들어 있다.

#### 새 말 — enclave

- **enclave** = 일반 프로그램이 못 들여다보는 **잠긴 방**에서 도는 코드로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 비유 (run 문서): **금고방**. 그 방에 들일 DLL 은 금고방 담당이 따로 주문한다.

#### 비트가 태어나는 곳 `[정적]`

| 비트 | 태어나는 줄 | 언제 |
|---|---|---|
| `0x800` | `LdrpLoadWow64 +0x98 MOV R8D,0x800` → `+0xa8 LdrpLoadDll(이름, 경로, 0x800, &핸들)` | WoW64 프로세스 (32비트 손님) 가 64비트 wow64 DLL 을 올릴 때 |
| `0x800000` | `LdrLoadEnclaveModule +0xe5` (→ `+0xff` → `+0x133 LdrpAllocatePlaceHolder`), `LdrpEnclaveAddDependentModule +0x9a` (→ `+0xc1` → `+0xe9 LdrpFindOrPrepareEnclaveModule`) | enclave (금고방) 모듈 올리기 |

- 비유 (run 문서): 표시를 처음 주는 사람은 "**32비트 손님 준비 담당**" 과 "**금고방 담당**" 뿐이다.

#### 그 값이 지시서 `+0x20` 에 들어가는 길 — 단계별로 `[정적]`

1. **보통 길**: `LdrpLoadDll +0x2d` (flags 보관)
   → `+0xbd LdrpPreprocessDllName` (여기서 더하는 건 `0x4` · `0x20` · `0x200` · `0x600` 뿐 — 두 비트는 안 더함)
   → `+0xca` → `+0xfb LdrpLoadDllInternal`
   → `+0x1ed LdrpFindOrPrepareLoadingModule` → `+0x37 MOV ESI,R8D` → `+0x40c BTS ESI,0xf` → **`+0x427 MOV [RAX+0x20],ESI`** (새 지시서).
2. **enclave 길**: `LdrpAllocatePlaceHolder +0x2c` → `+0x7c BTS R15D,0xf` → `+0x81 MOV [RAX+0x20],R15D` (결과서 줄).
3. **의존 DLL 에 물려줌**: 배달 기사 `LdrpLoadDependentModuleInternal`
   → `+0x3b MOV R15,RDX` (주소록 주인 = importer 의 지시서)
   → **`+0x1de MOV ESI,[R15+0x20]` → `+0x1e2 AND ESI,0x1800800`** (importer 의 `0x800` · `0x800000` · `0x1000000` 셋만 남김)
   → `+0x947` → `+0x10e7 BTS R15D,0xf` → `+0x10ec MOV [RAX+0x20],R15D`.
   - enclave 면 `+0x8f8 BT ESI,0x17` 로 enclave 쪽 길.

- 그래서: 비트는 **뿌리 (`LdrpLoadWow64` · enclave 둘) 에서 한 번 들어오면, 그 뿌리가 부르는 import 에 대물림된다.**
- 비유: 손님 담당이 첫 지시서에 "32비트 손님용" 도장을 찍어 보내면, 배달 기사가 그 건물이 기대는 건물들 지시서를 새로 쓸 때 **그 도장 셋만 골라 베껴** 찍는다.
- run23 이 flow.md 2.6 에 적은 "부모 `+0x20 & 0x1800800` 에서 시작" 과 같은 줄이다. 이번에 그 마스크의 **세 비트가 어디서 오는지**가 채워졌다.

#### 사용자가 부르는 `LdrLoadDll` 로는 못 켠다 `[정적]`

- `LdrLoadDll +0x64 ~ +0xa5` 가 사용자 값 `*DllCharacteristics` 를 지시서 flags 로 **한 비트씩 옮긴다.**
  - `0x4 → 0x8`, `0x2 → 0x40`, `0x800000 → 0x80`, `0x1000 → 0x100`, 부호 → `0x400000`
- 결과는 `{0x8, 0x40, 0x80, 0x100, 0x400000}` 안이다. 그래서 `0x800` · `0x800000` 은 **못 나온다** (`+0x16b LdrpLoadDll`, ret `+0x170`).
- 눈여겨볼 곳: 사용자 쪽 `0x800000` 은 지시서에선 **`0x80`** 으로 바뀐다. 숫자가 같아도 같은 표시가 아니다.

#### `LdrpLoadDll` 을 부르는 다른 곳의 flags (결과서 `objdump`)

| 부르는 곳 | flags |
|---|---|
| `LdrpInitializeImportRedirection +0xc2` | `0x1000001` (비트 `0x1000000` — 이 비트의 출처) |
| `LdrpLoadShimEngine +0x1c9` · `LdrpCorInitialize +0x16a` · `AVrfpLoadAndInitializeProvider +0xec` | 1 |
| `LdrpInitShimEngine +0xcd` · `LdrpGetProcApphelpCheckModule +0x255` | 0 |
| `LdrpLoadForwardedDll` (`+0x27b` · `+0x27d`) · `LdrpLoadPatchImage` (`+0xbf` 앞) | 0 |

- 곧 마스크 `0x1800800` 의 셋째 비트 `0x1000000` 의 뿌리도 생겼다.

#### 덤 — run26 의 "이름 못 정한 비트" 에 출처가 생겼다 `[정적: 결과서 objdump]`

- 이름은 PDB 에 여전히 없다. 대신 어디서 오는지가 생겼다.
  - `0x8` · `0x100` (· `0x40` · `0x80` · `0x400000`) = `LdrLoadDll +0x64 ~ +0xa5` 의 사용자 `DllCharacteristics` 옮기기
  - `0x800000` = enclave
  - `0x1000000` = `LdrpInitializeImportRedirection +0xc2`

### 2.7 같은 숫자 `0x800000` 이 둘 — 섞지 말 것

같은 함수 `LdrpMinimalMapModule` 안에 `0x800000` 이 두 번 나온다. **다른 물건**이다 `[정적]`.

| 어디 | 무엇 |
|---|---|
| `+0xa7 MOV R12D,0x800000` | 창구 (`NtMapViewOfSection`) 신청서의 **할당 종류** 칸 값. run38 이 `MEM_DIFFERENT_IMAGE_BASE_OK` 로 짐작 `[추측: 이름]` |
| `+0xee MOV R8D,[RSI+0x20]` → `+0xf2 AND R8D,R12D` | 지시서 `+0x20` 의 **비트 `0x800000`** (enclave 표시) 를, 마침 같은 숫자가 든 `R12D` 로 걸러 읽는 자리 |

- 헷갈리기 쉬운 이유: 지시서 비트를 읽을 때 **할당 종류 값이 든 레지스터를 체로 쓴다.** 숫자만 같다.
- 비유: 신청서의 "원래 번지 아니어도 됨" 체크 칸 번호와, 지시서의 "금고방용" 도장 번호가 우연히 같은 것.
- `Ex` 로 가는 갈림 줄은 `+0x11d TEST [RSI+0x20],0x800` · `+0xee ~ +0xf2` (`& 0x800000`) → `+0x13f TEST ECX,ECX` · `+0x141 JNZ` 다.
  - 결과서는 "`+0x141 TEST ECX,ECX`" 라고 적었는데, run 문서는 `.asm` 으로 **`+0x13f TEST` · `+0x141 JNZ`** 로 맞췄다 (run38 과 같음). 이 글은 run 문서를 따른다.

### 2.8 test.exe 는 두 비트 길을 안 탄다 — 단계별로

#### `0x800` 쪽 `[정적+관찰]`

1. `[정적]` `LdrpLoadWow64` 를 부르는 자리는 `LdrpInitializeProcess +0x17c1` (ret `+0x17c6`) 이다.
2. `[정적]` 그 앞 `+0x1766 CMP [UseWOW64],R15D` → `+0x176d JZ +0x1806`. **`UseWOW64` 가 0 이면 건너뛴다.**
3. `[관찰]` `UseWOW64` 값은 **run44 사진에 이미 있다** — 0. (flow.md 2.4 run44 "갈림 값", 짐 지도 3.8 사진 `끝까지 0*`)
   - EPROCESS `WoW64Process : (null)` (`04_baggage-read_P1a.txt:160` · `_P2.txt:160`), `Wow64VaSpace4Gb : 0y0` (`_P1a.txt:106`) 도 같은 쪽.
4. 그래서 → **`LdrpLoadWow64` 안 불림 → `0x800` 안 생김** `[정적+관찰]`.

- 여기가 결과서보다 run 문서가 한 발 더 나간 곳이다 (맨 위 "조심할 곳" 2).
  - 결과서: "x64 면 0 `[추측: 값 안 봄]` → VM 에서 `dd ntdll!UseWOW64` 로 보자"
  - run 문서: 사진에 이미 0 이 있으니 그 VM 확인은 **필요 없어졌다** (확인용으로만).

#### `0x800000` 쪽 `[정적+소스]`

1. `[정적]` `LdrLoadEnclaveModule` 은 내보내기 함수인데, 이 빌드 ntdll 안에서 직접 부르는 곳이 없다 (결과서).
2. `[정적]` `LdrpEnclaveAddDependentModule` 은 `LdrpEnclaveAddDelayloadModules +0x9e` · `LdrpEnclaveAddForwarderModules +0xf7` 에서만 불린다.
3. `[소스]` sample 에 enclave 가 없다 (`sample/` grep 0건).
4. 그래서 → **안 탄다** `[정적+소스]`.

#### 그러면 test.exe 의 지시서 `+0x20` 은 `[정적 조건]`

- exe: `LdrpInitializeProcess +0x1328 MOV [RSP+0x2d0],0x200` → `+0x1a8e` / `+0x1a94` `OR …,R12D(1)` = **`0x201`** (결과서).
- DLL 들: 배달 기사 `+0x1e2` 마스크로 세 비트가 **0**.
- → `LdrpMinimalMapModule` 은 **`+0x174 NtMapViewOfSection` 길** (`Ex` 아님).
- 다만 지시서는 사진에 칸이 없다. 실제 값은 **동적으로** 본다 (4절).

### 2.9 Q70 ④ `0x40000036` — 이름은 없고, 글은 "fixup 은 자동"

#### 이름 `[정적]`

- ntdll PDB 의 타입 · 공개 이름 (`llvm-pdbutil dump -types` · `-publics`) 에 `STATUS_IMAGE` · `DIFFERENT_BASE` 0건. `strings` 로도 없고, ntdll 본문 문자열에도 없다.
- 그래서 run 문서 결론: **"PDB 에 이름 없음."**
- `STATUS_IMAGE_AT_DIFFERENT_BASE` 는 **기억에서 나온 이름**이다 `[추측: 기억]`. 이 태그는 이번에도 안 올라갔다.

#### 뜻 `[정적: 메시지 표]`

**새 말 — 메시지 표 (`RT_MESSAGETABLE`)**: DLL 안에 들어 있는 "**결과 코드 번호 → 사람이 읽는 글**" 표. 번호와 글만 있고 `STATUS_…` 같은 이름은 없다.

- 비유: 창구 대답 번호를 찾아보는 **번호별 안내 책자**. 책자에 "36번 = 이런 뜻" 은 있지만, 36번의 별명은 안 적혀 있다.
- 결과서가 ntdll 자기 메시지 표 (영어) 를 작은 파서로 읽었다.

| 번호 | 글 (요지) |
|---|---|
| `0x40000036` | "{Image Relocated} 이미지가 파일에 적힌 주소와 **다른 주소에 붙었지만**, fixup 은 **여전히 자동으로 된다**." |
| `0x40000003` (비교) | "{Image Relocated} 이미지를 파일에 적힌 주소에 **못 붙였다**. **로컬 fixup 을 해야 한다**." (ko-KR `ntdll.dll.mui` 도 같은 뜻) |

- 원문 (run 문서 그대로): `0x40000036` = "… but **fixups will still be automatically performed** on the image.", `0x40000003` = "… **Local fixups must be performed** on this image."

#### 코드에서 이 값이 쓰이는 법 `[정적]` (run38 줄 그대로 + 글)

- ntdll 에 리터럴 `0x40000036` 은 **없다.** `0x40000003 + 0x33` 모양으로만 나온다.
  - `LdrpMinimalMapModule +0x192 SUB ECX,0x40000003` → `+0x198 JZ` · `+0x1a7 CMP ECX,0x28` → `+0x1aa JZ`. 둘 다 같은 `+0x288` (재시도 검사 갈래) 로 간다.
  - `LdrpCompleteMapModule +0xe4 SUB` · `+0xec CMP ECX,0x33` → `+0xef JZ`.
- **갈림**: `LdrpCompleteMapModule +0x1b4 CMP R12D,0x40000003` → `+0x1bb JZ` 는 **정확히 `0x40000003` 일 때만** `+0x31b LdrpRelocateImage`.
  - `0x40000036` 은 `+0x1c1` 로 내려가서 재배치를 **안 한다.**
- 글과 코드가 맞는다:
  - `0x40000003` = "로컬 fixup 을 해야 한다" → 관리실이 **펜을 든다.**
  - `0x40000036` = "fixup 은 자동으로 된다" → 관리실은 **펜을 안 든다.**

#### run102 (커널 쪽) 와 어긋나지 않나 — 단계별로

1. run102 는 관리사무소에서 이 값이 나오는 자리를 `MiMapViewOfImageSection +0xc1b ~ +0xc82` 로 적었다. "`0x40000003` 갈래 안에서 `BL` 비트 1 · 3 이 같이 켜짐" 이다.
2. flow.md 2.6 은 "`0x40000036` 은 `0x40000003` 갈래 안의 다른 비트 조합이라, '섹션 만들 때 고쳐 둠' 의 표시가 아니다" 로 적었다 (run102).
3. 이번 글은 두 부분으로 읽힌다.
   - "다른 주소에 붙었다" → `0x40000003` 갈래 안이라는 run102 와 같다.
   - "fixup 은 자동으로 **된다**" → 유저 (ntdll) 가 할 일이 아니라는 뜻이다. ntdll `+0x1b4` 와 같다.
4. 그래서 run 문서 결론: **어긋나지 않는다.** 글은 "섹션 만들 때 **이미** 고쳐 둠" 이라는 뜻이 아니다.
5. 남는 것: 자동 fixup 을 **커널 어느 줄이 하나**, `BL` 비트 1 · 3 은 무엇인가 → `[공백]` (4절).

- run38 해설 2.10 의 짐작 다리 ① 은 "이름 기억 — '다른 주소지만 이미 고쳐져 있음' 쪽" 이었다 → [run38 해설 2.10](../02_sprint/run38-map-module-해설.md).
  - 이번에 이름은 그대로 `[추측: 기억]` 이다.
  - 뜻은 글로 확인됐다 `[정적: 메시지 표]`. 다만 글은 "자동으로 **된다**" 지 "**이미** 고쳐져 있다" 가 아니다. 위 4번처럼 읽는다.
  - 창구가 dll1 · dll2 에 실제로 무엇을 대답하는지는 여전히 동적 (run38 해설 2.10 "어떻게 판가름하나" 그대로).

### 2.10 사진과 맞춰 보면 (run44, 새로 돌린 것 아님)

| 정적 | 사진 값 | 맞나 |
|---|---|---|
| `0x800` 은 `UseWOW64 ≠ 0` 일 때만 (`+0x176d`) | `UseWOW64` 0, EPROCESS `WoW64Process : (null)`, `Wow64VaSpace4Gb : 0y0` | **맞음** — `0x800` 안 생김 |
| 실패 길 (도장 4 그대로 · −3 없음) | 카드 도장에 음수 없음 (2.5 끝) | **맞음** — test.exe 는 실패 길 안 탐 |
| 지시서 `+0x20` (`0x201` / DLL 은 세 비트 0) | 지시서는 사진에 칸이 없다 | **못 봄** — 동적 |

### 2.11 비유로 다시 한 번

1. 관리실이 건물 하나를 들일 때, 주문 받은 사람 (`LdrpLoadDllInternal`) 이 **자기 메모지 한 장**을 지시서에 끼워 보낸다.
2. 건물 담당이나 게시판 쪽지 처리가 **주소록 채우기**를 시킨다.
3. 채우다 실패하면, 그 사람은 **메모지에 빨간 숫자만** 적는다. 카드 도장은 "잇는 중 (4)" 그대로다. "잇기 실패 (−3)" 도장을 **처음 찍는 사람은 관리실에 없다.**
4. 게시판의 다른 쪽지들은 메모지가 빨가면 그냥 손을 놓는다.
5. 주문 받은 사람은 그래프 정리는 한 번 돌리지만 (실패 카드는 6 이 안 된다), **전원 켜기는 건너뛰고 짐을 도로 싼다.** 치우기 담당이 그 카드를 어떻게 처리하는지는 아직 안 읽었다.
6. 따로, 지시서의 "32비트 손님용" (`0x800`) · "금고방용" (`0x800000`) 도장은 **지시서를 처음 쓸 때부터** 받아 온다. 손님 담당 · 금고방 담당만 찍고, 배달 기사가 기대는 건물 지시서에 베껴 준다.
7. test.exe 가게엔 32비트 손님도 금고방도 없다. 그래서 dll 들은 **보통 창구** (`NtMapViewOfSection`) 로 놓인다.
8. 창구가 "36번" (`0x40000036`) 으로 대답하면 안내 책자에는 "다른 자리에 놓였지만 번지수는 **자동으로 고쳐진다**" 고 적혀 있다. 관리실은 그 말대로 펜을 안 든다. "3번" (`0x40000003`) 이면 펜을 든다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 — 그림 2

| 자리 | 전 | 후 |
|---|---|---|
| 그림 2 `jm1` (`LdrpMinimalMapModule [dll1 추정]`) | "… ctx+0x20 비트 0x800·0x800000 이면 대신 +0x371 Ex." | 같은 글 + "**(run106: 두 비트는 WoW64 · enclave 뿌리만 넣음 — test.exe 안 탐)**" |
| 그림 2 제목 | … run105 | … run105, **run106** |

- 새 상자는 없다. 상자 글 한 줄이 붙었을 뿐이다.

### flow.md 에서 바뀐 것 — 글

| 절 | 전 | 후 |
|---|---|---|
| 2.4 `+0x5f2` `UseWOW64` 줄 | "x64 test.exe 면 0 `[추측]`" | 아래에 한 줄: run44 사진 0 `[관찰]`. 이 칸이 켜져야 `LdrpLoadWow64` 가 불리고, 그 안 `+0x98` 이 `0x800` 의 **유일한 뿌리** — test.exe 는 건너뜀 `[정적+관찰]` |
| 2.6 `LdrpSnapModule` 절 −3 줄 | "처음 쓰는 곳은 못 찾았다 `[공백]` → Q69" | "**처음 쓰는 줄은 없다**" `[정적]`. 실패면 도장 4 그대로 |
| 2.6 **새 하위 절** "snap 실패 뒤" | 없음 | 2.5 의 사슬 트리 + 정적 셋 (−3 은 전파뿐 · 실패는 메모지로만 · `+0x277` 건너뜀) |
| 2.6 붙이기 시스템 콜 줄 | "두 비트를 켜는 줄은 뽑아 둔 함수에 없다 → Q70" | 켜는 줄은 ntdll 전체에 없고 뿌리가 인자로 넣음 `[정적]`, test.exe 안 탐 `[정적+관찰]` |
| 2.6 결과 코드 줄 | "`0x40000036` 이름은 못 정함 (기억 `[추측]`, Q70)" | "**PDB 에 이름 없음**" `[정적]`, 이름은 `[추측: 기억]` 그대로, 메시지 표 글 확인 |
| 2.6 `CompleteMap` 5번 줄 | — | 메시지 표 글과 `+0x1b4` 갈림이 맞는다는 한 줄 + run102 와 어긋나지 않음 |
| 2.6 run23 `+0x10ec` 줄 | 마스크 `0x1800800` 만 | 세 비트 출처 (`0x800` WoW64 · `0x800000` enclave · `0x1000000` `LdrpInitializeImportRedirection +0xc2`) |
| 2.7 `LdrpCondenseGraphRecurse` −3 줄 | "처음 쓰는 곳은 아직 모른다 `[공백]` → Q69" | "**처음 쓰는 곳은 없다**" `[정적]` — 이 전파 줄 하나 |
| 4장 | Q69 · Q70 줄, `UseWOW64` 줄, run26 비트 이름 줄 | 넷 다 "(해결됨)" / 출처 보탬. run106 새 줄 셋 (동적 · 정적 · PE_emulator) |
| 4.1 | `LdrpMinimalMapModule → NtMapViewOfSection` 줄 | `Ex` 를 부르는 두 비트의 뿌리 · test.exe 안 탐 보탬 |

- 바뀐 태그를 한 줄로: `[공백]` · `[추측]` 이던 것들이 `[정적]` · `[정적+관찰]` 이 됐다. **확정 사실을 고친 곳은 없다** (run 문서 "정정").
- baggage.md: 3.6 머리 메모만 (도장 쓰는 줄 전수 · −3 처음 쓰는 줄 없음 · 실패면 4 그대로). **칸 수 · 줄 수 · 2장 숫자는 그대로**다.
  - 실패 길은 test.exe 가 안 타서 짐 줄로 안 더했다.
  - 지시서 `+0x20` 은 체크리스트 밖 힙 개체 칸이라 표에 안 넣었다.

### 큰 그림에서

- **짐 지도 쪽**: 이번 넷은 다 "test.exe 가 안 타는 길" 이거나 "값의 뜻" 이다. 그래서 짐 개수는 안 바뀌었다. 대신 "**혹시 빠뜨린 짐이 이 길에 있나**" 하는 걱정이 줄었다.
  - 실패 도장 −3 · WoW64 · enclave 비트는 초기화 동안 test.exe 에서 **생기지 않는다** `[정적+관찰]` · `[정적+소스]`.
- **다음 구간과의 이음**: dll 들이 **보통 창구** (`NtMapViewOfSection`) 로 놓인다는 게 정적으로 굳었다 `[정적 조건]`.
  - 그래서 Q20 · Q23 동적에서 멈춤점 자리가 깔끔해진다. 같은 실행에서 `ntdll+0xa30e` (지시서 비트) 와 `ntdll+0xa399` (창구 대답) 를 같이 보면 된다.
- **에뮬레이터 쪽** (결과서 "지금 우리" — `../PE_emulator` 코드는 flow-writer 가 안 읽음):
  - 도장 −3 을 쓰는 코드가 없는 것 — **맞음** (만들 것도 없다. 로더 구간은 진짜 ntdll 코드가 돈다).
  - exe 지시서 `0x200` → `|= 1`, 두 비트 안 켬 — **맞음**.
  - `0x40000036` — 우리가 이 값을 돌려주면 ntdll 은 재배치를 안 하고 (`+0x1b4`), `0x40000003` 이면 한다. 상수 주석에 글 한 줄만 보태면 된다.
  - 커널이 **언제** `0x40000036` 을 고르나 (`AddressSpace.cpp:752~759` `TODO(VM)`) 는 커널 쪽 질문이라 그대로 남는다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 지시서 `+0x20` 의 **실제 값** (`0x800` · `0x800000` 비트가 정말 0 인가, 그래서 `NtMapViewOfSection` 길인가) | 지시서는 힙 개체라 run44 사진에 칸이 없다. 정적으론 "조건상 0" 까지만 | 동적 **Q20** (run106 몫) — `ntdll+0xa30e` (`LdrpMinimalMapModule +0xee`, `RSI` = 지시서) 에서 `dd @rsi+0x20 L1`. run73 의 `ntdll+0xa399` `r eax` (`0` / `0x40000003` / `0x40000036`) 와 같은 실행에서 |
| snap 실패 길이 **실제로** 이렇게 도나 (메모지 음수 · 도장 4 유지 · 치우기 담당에 들어가나) | test.exe 정상 흐름엔 snap 실패가 없다. 길을 세우려면 실패를 일부러 만든 dll (없는 export 를 import) 이 있어야 한다 | 새 샘플 **Q194** (만들지는 **사용자 결정**) — 볼 자리: `ntdll+0x21562` / `ntdll+0x17271` `r esi`, `ntdll+0x1fea7` `dd @rbx L1`, `ntdll+0x86431` 뒤 |
| 도장 4 카드가 `LdrpUnloadNode` 안에서 −2 가 되나, 지역 칸이 언제 ≠ 0 인가 | `+0x138 ~ +0x1fb` 갈래 본문을 안 읽었다 `[공백]`. 초기화 실패 길이라 짐 지도 범위 밖 (낮음) | 정적 **Q193 ①** (`LdrpUnloadNode` rva `0x865c0` · `LdrpDecrementNodeLoadCountLockHeld` rva `0x86530`) |
| `0x800000` 이 `LdrpAllocatePlaceHolder` 의 `R8D` 로 넘어가는 enclave 길 끝까지 | 이번엔 `LdrLoadEnclaveModule +0x133` 쪽 줄만 확인 | 정적 **Q193 ②** (낮음) |
| 다른 DLL (shim · verifier) 이 카드 도장 칸을 직접 쓰나 → 전파 줄이 실제로 켜질 길이 있나 | `objdump` 꼴 훑기는 ntdll 안만 본다. 다른 DLL · 실행 중 만든 주소는 정적 한계 밖 | 정적 **Q193 ③** |
| `0x40000036` 의 "자동 fixup" 을 **커널 어느 줄이 하나**, `BL` 비트 1 · 3 은 무엇인가 | 커널 쪽 질문이다. 이번엔 ntdll 만 봤다 | 정적 **Q188 ⑤** (run 문서는 "run102 정적 ⑥" 이라 적었다 — run102 문서의 정적 목록 6번이 대기열 Q188 ⑤ 다) |
| `0x40000036` 의 **이름** | PDB · ntdll 어디에도 이름이 없다. 메시지 표엔 글만 | 이름은 `[추측: 기억]` 그대로. 따로 대기열 없음 |

---

## 5. 더 읽기

- run 문서: [`run106-snap-map-small.md`](run106-snap-map-small.md) — "정적 — 호출 지점" 표, State 값별 쓰는 줄 전수 표, "사진 로그 대조", "결과서와 다르게 적은 곳"
- 결과서: [`_workspace/sast/100_snap-map-small_result.md`](../../_workspace/sast/100_snap-map-small_result.md) — 전수 검색 방법, Q69 ② 사슬 전체, `LdrpLoadDll` 을 부르는 곳 전부
- 요청서: [`_workspace/sast/100_snap-map-small.md`](../../_workspace/sast/100_snap-map-small.md)
- flow.md: [`flow.md`](flow.md) — 2.4 `LdrpInitializeProcess` (`+0x5f2` `UseWOW64` 줄), 2.6 (붙이기 시스템 콜 줄 · 결과 코드 줄 · `LdrpSnapModule` 절 · **"snap 실패 뒤"** 절), 2.7 `LdrpCondenseGraphRecurse`, 4장 run106 줄
- 짐 지도: [`baggage.md`](baggage.md) — 3.6 머리 메모, 3.8 `UseWOW64`
- 앞 바퀴 해설: [run36 (주소록 채우기)](../02_sprint/run36-snap-module-해설.md) · [run38 (갖다 놓기 · 마무리, `0x40000003`)](../02_sprint/run38-map-module-해설.md) · [run102 (커널 재배치)](run102-image-reloc-해설.md) · [run27 (진행 도장 −4)](../02_sprint/run27-init-graph-해설.md) · [run24 (그래프 정리 · 전파 줄)](../02_sprint/run24-work-queue-해설.md)

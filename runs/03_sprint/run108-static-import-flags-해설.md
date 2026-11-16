# run108 static-import-flags — 해설

> 이 글은 [run108 문서](run108-static-import-flags.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q72** — 2.5·2.7 Q54 남은 전수 확인 (`_workspace/plan.md`, 이번에 **끝**, 한 바퀴로)
  - Q54 는 2026-10-24 사용자 결정으로 "새 바퀴 없이 문서로" 닫았었다 ([run40 해설 2.10](../02_sprint/run40-dll-crt-entry-해설.md#210-같은-바퀴에-닫은-것--q54-새-run-없이-문서로)). 그때 "정말 그게 **전부**인가" 는 안 봤다. 그 남은 숙제가 Q72 다.
  - 같이 한 문서 일: **Q123** — run57 트리 한 줄의 오프셋 (2.17).
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q79** (이미 있는 덤프를 오프라인으로 읽기), **Q44** (안 서야 할 멈춤점)
    - 정적 → 새 **Q196 ① ~ ③**
    - 짐 지도 셈 (`LdrpDllNotificationLock` 을 7.3 "모름" 에서 3.8 로 옮길지) → **사용자 결정 대기**
- 근거:
  - run 문서 [`run108-static-import-flags.md`](run108-static-import-flags.md) (가장 중요)
  - 요청서 [`_workspace/sast/103_static-import-flags.md`](../../_workspace/sast/103_static-import-flags.md) · 결과서 [`103_static-import-flags_result.md`](../../_workspace/sast/103_static-import-flags_result.md) (판정 **분석성공**)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다** (노드·화살표 그대로). 글 쪽은 2.4 · 2.5 · 2.6 · 2.7 · 2.8 · 4장 · 5장에 줄이 붙거나 고쳐졌다 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): **새 줄 없음**, 고친 줄 3 + 메모 1, 새 7.39.
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` (기준과 같다). 다시 analyze 안 했다.
  - 이 글의 `[관찰]` 은 전부 **예전 사진 로그 (run44)** 를 다시 읽은 것이다.

---

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **전수 검색** → 2.3
- **알림 락 주인 칸** (`OwningThread`) → 2.5
- **바꿔치기된 항목** → 2.8
- **죽은 길** → 2.11
- **되살리는 관용구** → 2.12
- 이미 푼 말은 링크만 건다:
  - **간선** (연결 카드 사이 **끈**), 끈 상자 칸 `+0x00 ~ +0x18`, **열쇠고리** (`_LDRP_CSLIST`) → [run23 해설 2.3](../02_sprint/run23-load-dependent-해설.md#23-핵심--줄간선은-여기서-긋는다)
  - `ProcessStaticImport` = 명부 줄의 **"개업 동기" 스티커** → [run40 해설 2.10](../02_sprint/run40-dll-crt-entry-해설.md#210-같은-바퀴에-닫은-것--q54-새-run-없이-문서로), 그 스티커가 진입점 넷째 인자를 가른다 → [run27 해설 2.8](../02_sprint/run27-init-graph-해설.md#28-진입점-넷째-인자--벽에-붙여-둔-context-의-쓰임)
  - `LdrInitState` = 관리실 **팻말** (0 → 1 → 2 → 3) → [run19 해설 2.12](../02_sprint/run19-proc-init-modules-해설.md#212-팻말--ldrinitstate-1--2--3)
  - **핫패치**, 핫패치 문맥 → [run105 해설 2.7](run105-ntdll-small-해설.md)
  - 전달 항목 (**forwarder**, 포워더) → [run20 해설 2.5](../02_sprint/run20-kernel32-load-해설.md), 코드로 본 것 → [run36 해설 2.6](../02_sprint/run36-snap-module-해설.md#26-질문-2--다른-dll-에-있다는-안내문-forwarder)
  - 그래프 정리 · 고리 합치기 (`LdrpMergeNodes`) → [run24 해설 2.9](../02_sprint/run24-work-queue-해설.md)
  - App Verifier → [run105 해설 2.6](run105-ntdll-small-해설.md)
  - "없다" 를 말하는 법 (`objdump` 로 ntdll 전체 풀기, 대조군) → [run106 해설 2.3](run106-snap-map-small-해설.md#23-없다-를-어떻게-말할-수-있나--찾는-방법부터)

### 이번 바퀴에서 조심할 곳 넷

1. **정정이 하나 있다.** 예전에 적은 "끈 비트 0 을 켜는 곳 둘 · 지우는 곳 하나, 뜻은 대비용 칸 표시" 는 **모자랐고, 뜻 짐작은 근거를 잃었다** (2.16).
   - 그래도 **test.exe 결과는 안 바뀐다.** test.exe 의 import 끈은 비트 0 = 0 이고, 순서 담당 GR 은 끈을 전부 따라간다. 바뀐 건 근거의 힘이다 (`[추측]` → `[정적]`).
2. **"코드에 이런 길이 있다" 와 "test.exe 가 탔다" 는 다르다.**
   - 비트 0 을 켜는 곳 넷 가운데 test.exe 가 타는 곳은 **없다** (2.10). 그래도 길 모양은 `[정적]` 으로 다 읽었다.
3. **`[정적 추정]` 이라는 표시가 나온다.** run 문서가 쓴 표시를 그대로 옮긴다.
   - `+0x17d9` 길 하나에 붙어 있다 (2.11). 코드 모양에서 끌어낸 결론인데, 실행으로는 아직 안 봤다는 뜻으로 읽으면 된다.
4. **오프셋은 call 자리다.** ret 자리는 run 문서 "정적 — 호출 지점" 표에 있다.
   - 다만 1절 큰 그림과 2.1 트리는 앞 해설들처럼 **ret 자리**로 적었다. 숫자가 5 쯤 다르면 call / ret 차이다 (예: `+0x1aa4` call ↔ `+0x1aa9` ret).

### 비유 — 그대로 이어 쓴다

- 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**, DLL = **건물** → [run23 해설](../02_sprint/run23-load-dependent-해설.md)
- 모듈 항목 = **명부 한 줄**, DDAG 노드 = 명부 줄마다 붙은 **연결 카드**, 간선 = 카드 사이 **끈** → [run23 해설](../02_sprint/run23-load-dependent-해설.md) · [run27 해설](../02_sprint/run27-init-graph-해설.md)
  - 끈 한 가닥 = `0x20` 바이트 **끈 상자** 하나. 양 끝 고리를 부모 카드 앞면 열쇠고리 (`Dependencies`) 와 자식 카드 뒷면 열쇠고리 (`IncomingDependencies`) 에 건다.
- `LdrpInitializeGraphRecurse` (GR) = **순서 담당** (끈을 따라 돌며 "누구부터 켤까" 를 정한다) → [run27 해설](../02_sprint/run27-init-graph-해설.md)
- `ProcessStaticImport` (명부 줄 `Flags` 비트 5, `0x20`) = **"개업 동기" 스티커** — 개업 날 처음부터 같이 들인 건물 표시 → [run40 해설 2.10](../02_sprint/run40-dll-crt-entry-해설.md#210-같은-바퀴에-닫은-것--q54-새-run-없이-문서로)
- `LdrInitState` = 관리실 **팻말** → [run19 해설 2.12](../02_sprint/run19-proc-init-modules-해설.md#212-팻말--ldrinitstate-1--2--3)
- 유저 쪽 잠금 = **열쇠** → [run18 해설 2.4](../02_sprint/run18-proc-init-setup-해설.md). `LdrpDllNotificationLock` = **알림 방 열쇠**, 그 주인 칸 = "지금 열쇠를 쥔 직원 번호" (2.5).
- 사진 P1a · P1b · P2 → [run44 해설](run44-baggage-snap-해설.md)

run 문서는 이번에 **다른 낱말**로 같은 그림을 그렸다. 가리키는 건 같다. 이 글은 위의 옛 낱말을 이어 쓴다.

| run 문서 비유 | 이 글 (옛 해설 이어 쓰기) |
|---|---|
| 손님 명찰 | 명부 한 줄 |
| "초대 손님" 도장 | "개업 동기" 스티커 |
| "본 행사 손님 맞이 시간" (`LdrInitState == 1`) | 팻말이 1 인 동안 |
| 안내 방송실 (알림 락) | 알림 방 열쇠 |
| 두 손님을 잇는 끈 | 연결 카드 사이 끈 |
| 끈에 붙인 "따라가지 마" 꼬리표 (비트 0) | 같다 — **"따라가지 마" 꼬리표** |

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) | 표시 |
|---|---|---|
| (a) 스티커 (`Flags` 비트 5) 를 붙이는 곳이 `LdrpAllocateModuleEntry +0x194` 하나뿐인가 | **예, ntdll 전체에서 하나.** 떼는 줄도 하나 (`LdrpFreeReplacedModule +0x15`) | `[정적]` |
| (a) 조건 칸 `ntdll+0x1cc920` 은 무엇인가 | **`LdrpDllNotificationLock.OwningThread`** — 알림 방 열쇠를 지금 쥔 직원 번호 | `[정적]` (뜻은 `[추측]`) |
| (a) kernel32 · kernelbase 는 왜 스티커가 없나 | 팻말이 1 이 되기 (`+0x1a4c`) **전에** 올라오기 때문 (`+0x199d`) | `[정적+관찰]` |
| (b) 끈 비트 0 을 켜는 곳 · 지우는 곳 | **켬 넷 · 지움 둘** (예전 목록은 켬 둘 · 지움 하나). 켬 하나 (`+0x17d9`) 는 **탈 수 없는 길로 보임** | `[정적]` · `[정적 추정]` |
| (b) test.exe 의 import 끈은 | **비트 0 = 0** — GR 이 전부 따라간다 | `[정적]` |
| (c) `LdrpRecordModuleDependency` 를 부르는 곳 | **정말 하나** (`LdrpInitializeNtdllDataTableEntry +0x23e`, 핫패치 갈래만) | `[정적]` |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

숫자는 ret 자리, 앞 run 들 그대로다. 이번 자리는 `LdrpInitializeProcess` 안 여러 군데다 (← 표시).

```
cmd.exe 의 부탁 → 커널 (프로세스 · 스레드 만들기)
 → ntdll LdrInitializeThunk → … → LdrpInitializeProcess
     ├─ +0x11a2  LdrpInitializeNtdllDataTableEntry   ntdll 명부 줄 (팻말 0)            ← (a)
     ├─ +0x126f  둘째 LdrpInitializeNtdllDataTableEntry  (핫패치일 때만, test.exe 안 탐) ← (c)
     ├─ +0x135e  LdrpAllocateModuleEntry             exe 명부 줄 (팻말 0)              ← (a)
     ├─ +0x19a2  LdrpInitializeKernel32Functions     kernel32 · kernelbase (팻말 0)    ← (a) 중심
     │  +0x1a4c  팻말 ← 1
     ├─ +0x1aa9  LdrpMapAndSnapDependency            dll1 · dll2 명부 줄 · 끈           ← (a) (b) 중심
     │  +0x1b1e  팻말 ← 2
     ├─ +0x1bbb  LdrpInitializeGraphRecurse          끈을 따라 dll 시작 함수들 (run27)  ← (b) 읽는 쪽
     └─ …
 → NtContinue → … → test.exe 진입점 → (CRT) LoadLibraryExW ×3 → main
```

- 이번 바퀴의 무대는 **"팻말이 0 인 구간" 과 "팻말이 1 인 구간" 의 경계** 다.
- 스티커는 팻말 1 구간에서만 붙는다. 그래서 "누가 그 구간 **안**에서 명부 줄을 만들었나" 가 답이 된다.
- 끈 이야기는 `+0x1aa9` 안 (끈을 만드는 쪽) 과 `+0x1bbb` (끈을 따라가는 쪽) 를 잇는다.

### 무엇이 궁금했나

run 문서 "목적" 을 줄이면:

1. **(a)** 명부 줄 `Flags (+0x68)` 에 `0x20` (`ProcessStaticImport`) 을 켜는 곳이 **정말** `LdrpAllocateModuleEntry +0x194` 하나뿐인가. 조건 칸 `ntdll+0x1cc920` 은 무엇인가.
2. **(b)** 끈 상자 칸 `+0x18` 의 비트 0 을 켜는 곳 · 지우는 곳 · 읽는 곳이 **전부** 무엇인가. 같은 `+0x18` 을 쓰는 다른 칸과 어떻게 가르나.
3. **(c)** `LdrpRecordModuleDependency` 를 부르는 곳이 **정말** `LdrpInitializeNtdllDataTableEntry +0x23e` 하나인가 (직접 호출 + 함수 포인터 둘 다).

### 바퀴 전에 알던 것

- `[정적]` (run22, sast/17) 스티커를 붙이는 줄 `LdrpAllocateModuleEntry +0x194`. 조건은 "팻말 == 1 이고, 지금 직원이 `LdrpDllNotificationLock` 주인이 아닐 때".
  - 다른 데서도 붙이는지는 안 셌다.
- `[정적]` (run27) 켜기 담당 `LdrpInitializeNode` 가 이 스티커를 보고 진입점 넷째 인자를 정한다. 스티커가 있으면 첫 스레드 CONTEXT 포인터, 없으면 0.
  - dll1 · dll2 에 스티커가 정말 붙었는지는 `[추측]` 이었다 (Q27 로 미룸).
- `[정적]` (run23 · run27) 끈 비트 0:
  - 켬 — `LdrpRecordModuleDependency +0x11a`, `LdrpLoadDependentModuleInternal +0x17d9` (`+0x17c2` 에서 잡은 **대비용** 끈 상자).
  - 지움 — `+0xede` (보통 끈 상자).
  - 읽음 — GR `+0x3a` (켜진 끈은 건너뜀).
  - 뜻 — "대비용 칸 표시" `[추측]`.
- `[추측]` (run40, Q54 정리) `LdrpRecordModuleDependency` 를 부르는 곳은 지도상 핫패치 갈래 하나 → test.exe 에선 GR 이 건너뛸 끈이 **없을 것**.
- `[관찰]` (run44) 사진 P1a · P1b · P2 에 6 모듈의 명부 줄 `Flags` 가 들어 있었다. 하지만 그걸 이 질문과 맞대어 본 적은 없었다.

---

## 2. 찾은 것

### 2.1 한눈에 — 트리 하나

run 문서 "호출 관계" 에서 대표만 옮겼다 (ret 자리). 전체는 run 문서.

```
(스티커 — 팻말과 시간 순서)
LdrpInitializeProcess
 ├─ +0x11a2  LdrpInitializeNtdllDataTableEntry(R8 = 0)
 │            └─ +0x6c   LdrpAllocateModuleEntry        ntdll 줄 — 팻말 0 → 안 붙임
 ├─ +0x135e  LdrpAllocateModuleEntry                     exe 줄 — 팻말 0
 ├─ +0x19a2  LdrpInitializeKernel32Functions             ← 팻말 0 인 채로
 │            ├─ +0xc0   LdrLoadDll (kernel32)  → … → LdrpAllocateModuleEntry   안 붙임
 │            └─ +0x156  LdrLoadDll (kernelbase) → …                            안 붙임
 │  +0x1a4c  팻말 ← 1
 ├─ +0x1aa9  LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal (dll1 · dll2)
 │            └─ +0x115c LdrpAllocateModuleEntry
 │                        +0x187 지금 직원 == 알림 방 열쇠 주인 ? → 같으면 건너뜀
 │                        +0x194 Flags |= 0x20                     ← dll1 · dll2 스티커
 │  +0x1b1e  팻말 ← 2
 └─ +0x1bbb  LdrpInitializeGraphRecurse                 +0x3a 꼬리표 붙은 끈 건너뜀

(끈 비트 0 — test.exe 정적 import 끈)
LdrpLoadDependentModuleInternal
 ├─ +0x92e   RtlAllocateHeap(0x20)          끈 상자 — 쓰레기 값으로 시작
 │  +0xed5 ~ +0xede  비트 0 지움             ← 조건 없음
 │  +0xf4d  RCX == 0 → +0x179d  (RCX 는 +0xed8 에서 이미 쓰임 → 안 탐, 정적 추정)
 │  +0xf8f ~ +0xfa5  +0x18 = 출발 카드 (옛 비트 0 되살림 — 0 이라 그대로)
 └─ (끈을 두 열쇠고리 꼬리에 — +0xf69 / +0xf8b, run23)
```

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 스티커를 붙이는 줄 (값이 바뀌는 순간)

트리의 `+0x194 Flags |= 0x20` 줄.

- **누가**: 명부 줄 · 연결 카드를 한 쌍으로 만드는 `LdrpAllocateModuleEntry` ([run22 해설 2.7](../02_sprint/run22-map-snap-dep-해설.md)).
- **어디서**: 그 함수 `+0x194`. 명령은 `OR dword [RBX+0x68],0x20` — RBX 가 지금 만드는 명부 줄이고, `+0x68` 이 `Flags` 칸이다.
- **누구를**: 호출이 아니다. 메모리 칸 하나에 비트를 켠다.
- **무엇을 위해**: 이 줄이 "개업 동기" 건물이라고 표시한다. 나중에 켜기 담당이 이 스티커를 보고 진입점에 CONTEXT 포인터를 건넨다 (run27).

#### 한 줄 읽기 ② — 꼬리표를 떼는 줄 (값이 바뀌는 순간)

트리의 `+0xed5 ~ +0xede 비트 0 지움` 줄.

- **누가**: 의존 DLL 하나를 처리하는 배달 기사 `LdrpLoadDependentModuleInternal` ([run23 해설](../02_sprint/run23-load-dependent-해설.md)).
- **어디서**: `+0xed5 MOV RCX,[R8]` (6번째 인자가 가리키는 칸에서 끈 상자 주소 꺼냄) → `+0xed8 MOV EAX,[RCX+0x18]` → `+0xedb AND EAX,0xfffffffe` → `+0xede MOV [RCX+0x18],EAX`.
- **누구를**: 역시 호출이 아니다. 끈 상자 `+0x18` 칸의 **맨 아래 비트 하나만** 끈다.
- **무엇을 위해**: 이 상자는 `+0x929` 에서 갓 잡은 것이라 안에 쓰레기 값이 들어 있다. 꼬리표 자리가 우연히 1 이면 GR 이 끈을 건너뛰어 버린다. 그래서 일부러 지우는 것으로 보인다 `[추측: 뜻]` (run 문서).

### 2.3 "하나뿐" 을 어떻게 말할 수 있나 — 전수 검색

이번 바퀴 답 셋이 모두 "**이것 말고는 없다**" 꼴이다. 없다는 말은 있다는 말보다 어렵다. 그래서 찾는 법부터 본다.

**새 말 — 전수 검색**: 모듈 전체 기계어를 한 줄도 빼지 않고 다 풀어서, 찾는 꼴의 줄을 **빠짐없이** 찾는 것.

- 비유: 동네 건물 하나하나를 다 들어가 보고 "개업 동기 스티커를 붙이는 사람은 이 사람 하나뿐" 이라고 말하기. 소문 (지도에 이미 있는 함수) 만 듣고 말하는 것과 다르다.
- 한계: 그 동네 **안**만 본다. 옆 동네 사람이 몰래 와서 붙이는 건 못 잡는다. 그리고 "찾는 꼴" 을 내가 정하니, 생각 못 한 모양으로 쓰는 줄은 빠질 수 있다.

이번에 한 것 (run 문서 "방법", 결과서 1-G · 2-G):

1. 이미 뽑아 둔 `.asm` 모음은 ntdll 함수 일부뿐이라 "전부" 를 못 본다.
2. 그래서 sast/100 이 만든 `objdump -d` 풀이 (`ntdll.dis`, 421,500줄, 커밋 안 함) 를 다시 썼다. run106 · run107 과 같은 방법이다 ([run106 해설 2.3](run106-snap-map-small-해설.md#23-없다-를-어떻게-말할-수-있나--찾는-방법부터)).
3. 스티커 (`Flags`) 쪽은 네 가지로 찾았다.
   - `[reg+0x68]` 과 명부 줄 안 고리 6종 기준 같은 칸에 **쓰는 줄 전부**
   - `lea reg,[x+0x68]` (칸 주소를 들고 다니는 꼴)
   - 명부 줄 **통째 복사** (크기 `0x138`)
   - **바이트 꼴로 한 번 더** (`OR imm` / `AND imm` 에 `disp8=0x68`) — 명령 이름이 아니라 기계어 바이트로 교차 확인
4. 끈 쪽은 여섯 가지로 찾았다 (`[reg+0x18]` 비트 연산 전부, `mov` 뒤 8줄 안 비트 연산, 되살리기 관용구, 크기 `0x20` + `NtdllBaseTag` 힙 할당, 로더 함수의 `[reg+0x18]` 접근 하나씩 배정, 들어오는 고리 쪽 `[reg+0x8]` · 마스크 `-2` / `-8`).
5. caller 쪽은 `call` / `jmp` / 조건 점프 / 주소 상수 / 데이터 바이트 (8바이트 VA · 4바이트 RVA) 를 **전 구역**에서 찾았다.

**한계** (결과서 1-G · 2-G 그대로):

- 이 풀이는 **ntdll 코드 구역만**이다. kernelbase · apphelp · shimeng · 검증기 DLL 이 로더 목록을 직접 걸어 칸을 쓰는 건 범위 밖이다.
- `[reg+idx*N+0x68]` 같은 **색인 주소** 꼴은 로더에서 못 봤지만, 꼴 자체를 다 막은 건 아니다.
- `objdump` 가 코드 사이 데이터를 잘못 풀 수 있다 → 바이트 꼴 교차로 그 위험을 줄였다.
- 끈 쪽: 끈 상자 주소를 인자로 받아 안에서 쓰는 작은 함수, 마스크 없이 비교만 하는 줄은 놓칠 수 있다. 그래서 카드 `+0x30` 고리를 도는 함수들을 따로 봤다.

### 2.4 (a) 스티커를 붙이는 줄은 하나 — 조건 길을 한 걸음씩

#### 답 `[정적]`

- ntdll 전체에서 `Flags` 비트 5 를 **켜는 줄은 `LdrpAllocateModuleEntry +0x194` 하나**다.
- 바이트 꼴 검색에서도 비트 5 를 켜는 `OR imm` 은 이 줄 (rva `0x6fc44`) 하나뿐이었다 (결과서 1-G).

#### 그 줄에 닿는 길 — 단계별로 `[정적]`

1. `+0xc2 CMP [LdrInitState],1` — 팻말이 1 인가?
2. `+0x105 JZ +0x17a` — 1 이면 `+0x17a` 로 간다. 아니면 이 갈래를 통째로 안 탄다.
3. `+0x17a MOV RAX,GS:[0x30]` — 지금 직원의 TEB 를 꺼낸다.
4. `+0x183 MOV RCX,[RAX+0x48]` — TEB `ClientId.UniqueThread`, 곧 **지금 직원 번호**.
5. `+0x187 CMP [ntdll+0x1cc920],RCX` — 그 번호가 조건 칸 값과 같은가?
6. `+0x18e JZ +0x107` — **같으면 건너뛴다.**
7. 다르면 `+0x194` 에서 스티커를 붙이고, `+0x198 JMP +0x107` 로 합류한다.

- 줄이면: **팻말 == 1 이고, 지금 직원이 알림 방 열쇠 주인이 아닐 때.**
- 이건 2026-10-24 결정문 · flow.md 2.6 의 말과 같다. 이번에 달라진 건 "**이것 하나뿐**" 이 전수로 확인된 것이다.

#### 다른 `Flags` 쓰기는? `[정적]`

- 명부 줄 `Flags` 를 쓰는 줄은 많다. 그런데 비트 0 · 1 · 2 · 3 · 6 · 7 · 8 · 9 · 10 · 13 · 15 · 19 · 20 · 21 · 22 · 24 · 26 · 28 이다. 비트 5 와 상관없다 (결과서 1-E 표).
- 헷갈리기 쉬운 하나: App Verifier 복제 `LdrpInitializeProcess +0x15b8 OR [RBX+0x68],ECX`.
  - 원본 줄의 `Flags` 를 **옮기는** 줄이다. 원본에 `0x20` 이 없으면 안 옮긴다.
  - test.exe 는 `AvrfAppVerifierMode` 0 이라 이 갈래 자체를 안 탄다 (짐 지도 3.9).

#### 명부 줄을 만드는 곳 6 — 그때 팻말은? `[정적]`

`LdrpAllocateModuleEntry` 를 부르는 곳은 `objdump` 전수와 `_calls.tsv` 가 똑같이 6곳이다.

| 부르는 곳 | 무엇의 줄 | 그때 팻말 |
|---|---|---|
| `LdrpInitializeNtdllDataTableEntry +0x67` | ntdll | 0 |
| `LdrpInitializeProcess +0x1359` | exe | 0 |
| `LdrpInitializeProcess +0x1577` | App Verifier 복제 (`AvrfAppVerifierMode ≠ 0` 일 때만) | 0 |
| `LdrpLoadDependentModuleInternal +0x1157` | DLL | 그 순간 값 |
| `LdrpFindOrPrepareLoadingModule +0x475` | DLL | 그 순간 값 |
| `LdrpAllocatePlaceHolder +0xd0` | 자리표시 | 그 순간 값 |

- `.rdata` 에 이 함수 주소 `0x6fab0` 이 4번 나오지만, 전부 풀기 (unwind) 사슬 칸이라 **함수 포인터가 아니다** (결과서).

### 2.5 조건 칸 `ntdll+0x1cc920` = 알림 방 열쇠의 주인 칸

**새 말 — 알림 락 주인 칸 (`OwningThread`)**: 유저 쪽 잠금 (`RTL_CRITICAL_SECTION`, `0x28` 바이트) 안의 `+0x10` 칸. 지금 그 잠금을 잡은 스레드의 ID 가 들어간다.

- 비유: 알림 방 문 옆 **"지금 열쇠 가진 직원: ___번"** 칸. 아무도 안 쥐고 있으면 0.

#### 어떻게 이름을 붙였나 — 단계별로 `[정적]`

1. `pdb_aliases.py --where ntdll.dll 0x1cc920` → 공개 심볼에 **이 주소 이름은 없다.**
2. 바로 앞 이름이 `LdrpDllNotificationLock` (rva `0x1cc910`), 바로 다음 이름이 `LdrpTlsList` (`0x1cc938`).
3. 그러니 `LdrpDllNotificationLock` 의 덩치는 `0x28` 이다. 이게 `RTL_CRITICAL_SECTION` 크기와 같다.
4. `0x1cc920 − 0x1cc910 = 0x10` 이니 그 구조체의 `+0x10` = `OwningThread` 다.
5. 덤: 그 앞 `0x1cc8e0` 에 `LdrpDllNotificationLock_DEBUG` 가 있다. 사진의 `DebugInfo` 칸이 바로 이 주소를 가리킨다 (2.7).

- run21 해설에서 본 "알림 담당 본인이 또 왔네" 하고 돌려보내는 줄 ([run21 해설](../02_sprint/run21-load-dll-internal-해설.md), `LdrpPrepareModuleForExecution` 의 1번) 이 **같은 칸**을 본다.

#### 누가 이 칸을 쓰나 `[정적]`

- 칸 주소를 **직접** 쓰는 줄은 없다 (`_refs/LdrpDllNotificationLock.tsv` 20줄 — `mov [rip+..]` 꼴 0).
- 쓰는 건 `RtlEnterCriticalSection` / `RtlLeaveCriticalSection` 이 포인터로 한다.
- 열쇠를 잡는 쪽: `LdrpSendDllNotifications` · `LdrpSendPostSnapNotifications` · `LdrRegisterDllNotification` · `LdrUnregisterDllNotification` · `LdrpUnloadNode` · `LdrpDynamicShimModule` · `LdrpLoadShimEngine` (결과서 1-B).
- "지금 직원 == 주인 칸?" 을 **읽는** 줄은 다섯이다: `LdrpAllocateModuleEntry +0x187`, `LdrpMapDllWithSectionHandle +0x232`, `LdrpPrepareModuleForExecution +0x24`, `LdrGetProcedureAddressForCaller +0x18e` · `+0x925`.

#### 뜻은? — 짐작 `[추측]` (run 문서)

- 근거: `LdrpSendDllNotifications` 가 이 열쇠를 **쥔 채로** 알림 콜백을 부른다.
- 그래서: "주인 == 지금 직원" 이면 = **알림 콜백 안에서 부른 로드**다. 그때는 스티커를 안 붙인다.
- 아직 확인 아님: 왜 그때 안 붙이는지는 코드가 말해 주지 않는다. 다른 네 줄의 뜻도 같이 → 정적 Q196 ②.

### 2.6 kernel32 · kernelbase 에 스티커가 없는 이유 — 시간 순서

이게 이번 바퀴에서 가장 "아하" 하는 대목이다.

#### 단계별로 `[정적]`

1. 스티커는 **팻말 == 1** 일 때만 붙는다 (2.4).
2. 팻말이 1 이 되는 줄은 `LdrpInitializeProcess +0x1a4c`, 2 가 되는 줄은 `+0x1b1e` 다 (`_refs/LdrInitState.tsv`). 그러니 "팻말 1 구간" = **`+0x1a4c` ~ `+0x1b1e`**.
3. kernel32 · kernelbase 는 `+0x199d` 의 `LdrpInitializeKernel32Functions` 가 올린다.
   - 그 안 `+0xbb LdrLoadDll` (kernel32), `+0x151 LdrLoadDll` (kernelbase 이름).
   - kernelbase 는 kernel32 의 import 로 같이 올라온다 ([run20 해설 2.4](../02_sprint/run20-kernel32-load-해설.md)).
4. `+0x199d` 는 `+0x1a4c` 보다 **앞**이다. → 그 둘의 명부 줄이 만들어질 때 팻말은 아직 0 이다.
5. 팻말 1 구간 안에서 명부 줄이 새로 생기는 건 `+0x1aa4 LdrpMapAndSnapDependency` (exe 의 import 올리기) 쪽, 곧 **dll1 · dll2 뿐**이다.

- 그러니 test.exe 의 6 모듈은 이렇게 갈린다 `[정적]`:

| 모듈 | 줄이 생길 때 팻말 | 스티커 |
|---|---|---|
| test.exe | 0 | 없음 |
| ntdll | 0 | 없음 |
| kernel32 | 0 (`+0x1a4c` 앞) | 없음 |
| kernelbase | 0 (`+0x1a4c` 앞) | 없음 |
| dll1 | 1 | **있음** |
| dll2 | 1 | **있음** |

- 비유: kernel32 · kernelbase 는 **개업 준비 날** 먼저 들어와 있던 건물이다. 팻말이 "개업 중" 으로 바뀐 뒤 들어온 dll1 · dll2 만 "개업 동기" 스티커를 받는다.
- 이름만 보면 "import 로 붙은 모듈" 표시 같다 (run27 해설 `[추측: 이름]`). 그런데 kernel32 도 exe 가 쓰는 DLL 인데 스티커가 없다. 코드가 보는 건 이름이 아니라 "**팻말 1 구간에, 알림 콜백 밖에서 만든 줄인가**" 다 `[정적]`. 이름과 코드가 조금 다르게 읽힌다는 점만 기억해 두면 된다 (이 글의 짐작 `[추측]`).

### 2.7 사진과 맞대 보기 — 6 모듈 전부 맞다 `[관찰]`

새로 돌린 게 아니다. run44 사진 덤프 읽기 로그를 flow-writer 가 다시 읽었다.

| 정적 추론 | 사진 값 | 맞나 |
|---|---|---|
| exe · ntdll 0 | P1a `ProcessStaticImport : 0y0` 둘 (`04_baggage-read_P1a.txt:2696` · `2801`) | **맞음** |
| exe · ntdll · KERNEL32 · KERNELBASE 0, dll1 · dll2 1 | P1b `0y0` · `0y0` · `0y0` · `0y0` · **`0y1`** · **`0y1`** (`_P1b.txt:3394` · `3499` · `3604` · `3709` · `3814` · `3919`) | **맞음** (6/6) |
| 같은 값이 초기화 끝까지 | P2 같은 순서 (`_P2.txt:3406 ~ 3931`) | **맞음** — P1b → P2 사이 안 바뀜 |
| 알림 방 열쇠 주인 0, 알림 목록 빔 | 세 장 모두: `OwningThread` 0, `LockCount` `0xffffffff` (−1), `RecursionCount` 0. `LdrpDllNotificationList` (`ntdll+0x1cc8c0`) = 자기 자신 둘 | **맞음** |

- 마지막 줄이 중요하다. 열쇠 주인이 **0** 이니, 지금 직원 번호와 같을 수가 없다. → 2.4 의 "같으면 건너뜀" 갈래는 test.exe 에서 **설 자리가 없다** `[정적+관찰]`.
- 덤: 이 40바이트 값이 **파일 안 값과도 같다** (결과서). 바이너리가 타깃 VM 것과 같다는 근거 하나가 더 붙었다.
- 그래서 flow.md 2.8 의 오래된 질문 "dll1 · dll2 줄이 그 조건에서 만들어졌나 (Q27)" 의 **스티커 쪽**이 닫혔다. 진입점 넷째 인자에 실제로 실리는 값 자체는 Q27 그대로다.

### 2.8 스티커를 떼는 줄도 하나 — 바꿔치기된 명부 줄

**새 말 — 바꿔치기된 항목**: 같은 DLL 이 다른 명부 줄로 바꿔치기됐을 때, 밀려나 버려질 옛 명부 줄 (run 문서 표 끝 줄의 조건).

- 비유: 같은 건물 명부 줄이 둘 생겨서 하나를 지울 때, 지울 줄의 스티커를 먼저 떼고 버린다.

#### 떼는 줄 `[정적]`

- `LdrpFreeReplacedModule +0x15 AND dword [RBX+0x68],0xffffffdf` — 비트 5 만 끈다. 함수 안에서는 조건 없다.
- 그 뒤 `+0x1c ReferenceCount := 1` → `+0x2b` 에서 꼬리 JMP 로 `LdrpDereferenceModule` (풀기).
- 이 함수를 부르는 곳 다섯: `LdrpLoadDllInternal +0x33c` · `LdrpLoadDependentModuleInternal +0x133c` · `LdrpSnapModule +0xfc` · `LdrpFreeLoadContext +0x9d` · `LdrpHandlePendingModuleReplaced +0x27` (마지막은 결과서 `objdump`, `.asm` 없음).
- 바이트 꼴 검색에서도 비트 5 를 끄는 `AND imm` 은 이 줄 (rva `0xd755d`) 하나였다.
- 짐 지도는 이 줄을 "조건부 — 안 탐" 으로 적었다. 6 모듈 값이 P1b → P2 내내 그대로였다는 사진과도 어긋나지 않는다.

### 2.9 (b) 끈 상자 — 칸 배치와 "쓰레기 값으로 시작"

#### 칸 배치 (run23 과 같음) `[정적]`

| 칸 | 무엇 |
|---|---|
| `+0x0` | 출발 카드 `Dependencies` 열쇠고리 |
| `+0x8` | 도착 카드 |
| `+0x10` | 도착 카드 `IncomingDependencies` 열쇠고리 |
| `+0x18` | 출발 카드 포인터 — **맨 아래 비트에 표지** |

- PDB 에 이 구조체 이름은 없다.
- 포인터로 쓰는 쪽은 `AND r,-8` 로 맨 아래 3비트를 가린다 (2.13) `[정적]`. 카드 주소는 8 의 배수라 그 3비트가 늘 비어 있고, 그 빈자리를 꼬리표 자리로 쓰는 모양으로 보인다 `[추측]` (일반적으로 알려진 포인터 꼬리표 기법).

#### 상자를 만드는 곳 5 `[정적]`

모두 `RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 태그, 0x20)`:

| 곳 | 태그 |
|---|---|
| `LdrpLoadDependentModuleInternal +0x929` | `0x280000` (test.exe import 끈의 "미리 챙긴 끈") |
| `LdrpRecordModuleDependency +0x10d` | `0x240000` |
| `LdrpLoadDependentModuleInternal +0x17bd` | `0x240000` |
| `LdrpFastpthReloadedDll +0x198` | `0x240000` |
| `LdrpBuildForwarderLink +0x10b` | `0x240000` |

- **다섯 다 `HEAP_ZERO_MEMORY` 가 없다.** 곧 새 상자 안에는 **쓰레기 값**이 들어 있다.
- 그래서 비트 0 은 "누가 켜나" 만큼 "**누가 확실히 끄나**" 도 중요하다. 아무도 안 끄면 우연에 맡기게 된다.

### 2.10 꼬리표를 붙이는 곳 넷 — 그리고 test.exe 는 하나도 안 탄다

| 함수 + 오프셋 | 언제 | test.exe |
|---|---|---|
| `LdrpRecordModuleDependency +0x11a` | 호출자가 상자를 안 줬을 때 (`+0x10d` 로 새로 잡음) | 핫패치 갈래만 → 안 탐 (2.15) `[정적+추측]` |
| **`LdrpFastpthReloadedDll +0x1a5`** (새로) | 부모 ≠ NULL · 부모 `LoadCount ≠ 0` · 자식 ≠ 부모 · 이미 같은 끈 없음 등 | `LdrLoadDll` 길에선 안 탐 `[정적]` |
| **`LdrpBuildForwarderLink +0x11c`** (새로) | 부모 ≠ NULL (`+0x17` · `+0x1a JE +0xc5`) · 같은 꼴 조건 | `LdrLoadDll` 길에선 안 탐 `[정적]`. 포워더 길은 **안 따라감** |
| `LdrpLoadDependentModuleInternal +0x17d9` | `+0xf4d JZ` (RCX == 0) 로만 들어옴 | **탈 수 없는 길로 보임** `[정적 추정]` (2.11) |

#### 새로 찾은 둘이 왜 test.exe 에서 안 타나 — 단계별로 `[정적]`

1. 둘 다 "부모" 인자가 **NULL 이 아니어야** 끈을 만든다 (`+0x95` / `+0x1a` 에서 NULL 이면 끝).
2. 그 부모 인자는 이 둘을 부르는 `LdrpLoadDllInternal` 의 **6번째 인자**다 (`+0xb3` → `LdrpFastpthReloadedDll`, `+0x290` → `LdrpBuildForwarderLink`).
3. `LdrpLoadDll` 은 그 자리에 **0 을 넣는다**: `+0x71 XOR R12D,R12D` → `+0xf1` / `+0x1f8` / `+0x243 MOV [RSP+0x28],R12`.
4. `LdrLoadDll` 길 = kernel32 준비 (2.6) 와 진입점 뒤 `LoadLibraryExW` ×3 다. → 그 길에선 이 둘이 끈을 **안 만든다.**
5. 부모가 있는 길은 따로 있다: `LdrpLoadForwardedDll +0x2e1` · `LdrpLoadPatchImage +0xbf` → `LdrpLoadDllInternal` (결과서).
   - 포워더로 **다른 DLL 을 새로 올리는** 길이다. test.exe 가 이 길을 타는지는 **이번에 안 따라갔다** → 정적 Q196 ①.

- 비유: "따라가지 마" 꼬리표를 붙이는 사람 셋은 모두 "**누가 이 건물을 불렀나 (부모)**" 를 아는 사람이다. 손님이 직접 주문한 `LoadLibrary` 길에는 부모가 없어서 끈 자체를 안 만든다.

### 2.11 `+0x17d9` 는 죽은 길로 보인다 — 왜 그렇게 짐작하나

**새 말 — 죽은 길**: 코드엔 갈래가 있는데, 앞 줄들의 모양 때문에 실제로는 **들어갈 수 없어 보이는** 갈래.

- 비유: 지도엔 그려져 있는데, 입구에 이미 벽이 서 있는 골목.

#### 단계별로 `[정적]` → 결론은 `[정적 추정]`

1. `+0x17d9` (`+0x17d6 MOV EAX,[RAX+0x18]` · `OR EAX,1` · `+0x17dc MOV [RCX+0x18],EAX`) 로 오는 길은 **하나**다: `+0xf4d JZ +0x179d` — "RCX == 0 이면" 간다. 그 안 `+0x17bd` 에서 대비용 상자를 잡는다.
2. 그런데 그 RCX 는 앞에서 이미 쓰였다.
   - `+0xed5 MOV RCX,[R8]` 로 읽고,
   - `+0xed8 MOV EAX,[RCX+0x18]` 로 **그 주소를 따라 메모리를 읽었다** (2.2 한 줄 읽기 ②).
3. RCX 가 0 이었다면 `+0xed8` 에서 이미 주소 0 을 읽다가 사고가 났을 것이다. 거기를 무사히 지나 `+0xf4d` 에 왔다면 RCX 는 0 이 아니다.
4. → `+0xf4d` 의 "RCX == 0" 은 **설 수 없어 보인다.** `+0x17bd` 할당도, `+0x17d9` 꼬리표 붙이기도 같이 안 탄다.
5. 아직 확인 아님: 실행으로 안 봤다. 그래서 `[정적 추정]` → 동적 D2 (Q44) 의 멈춤점 `ntdll!LdrpLoadDependentModuleInternal+0x179d` 가 **안 서면** 맞다.

- 이게 정정의 핵심이다. run23 해설은 이 길을 "**대비용** 끈 (거의 안 오는 길 `[추측]`)" 으로 읽고, 비트 0 의 뜻을 "대비용 칸 표시" 로 짐작했다. 길 자체가 죽은 길로 보이니 **그 짐작은 근거를 잃었다** (2.16).

### 2.12 꼬리표를 떼는 곳 둘 — 그리고 되살리는 관용구

#### 떼는 곳 `[정적]`

1. **`LdrpLoadDependentModuleInternal +0xede`** — 2.2 한 줄 읽기 ②. `+0x929` 로 만든 상자의 비트 0 만 끈다. 조건 없다.
   - 예전 문서들엔 "`+0xede`" 만 있고 **함수 이름이 빠져** 있었다. 이번에 `LdrpLoadDependentModuleInternal` 로 적었다.
2. **`LdrpMergeNodes +0xe3`** (새로) — `MOV qword [RAX+0x18],RBX`.
   - 합쳐져 없어질 카드의 끈 `+0x18` 을 **합친 카드로 통째로 덮어쓴다** → 맨 아래 비트도 전부 0.
   - 부르는 곳은 `LdrpCondenseGraphRecurse +0xd3` 하나. 서로 기대는 고리 (순환 의존) 를 합칠 때다 ([run24 해설 2.9](../02_sprint/run24-work-queue-해설.md)).
   - test.exe 는 고리가 없다고 보인다 `[정적: 코드 모양]` (run24).

#### 되살리는 관용구 4곳 — 순효과 없음 `[정적]`

**새 말 — 되살리는 관용구**: `+0x18` 에 새 포인터를 통째로 쓰면서, 옛 비트 0 은 **그대로 되살려 두는** 짧은 명령 묶음 (`xor; and 1; xor` 꼴, 결과서 2-G).

- 비유: 끈 이름표를 새로 갈아 쓰되, 이름표 구석에 붙어 있던 꼬리표는 떼었다가 다시 붙인다. 꼬리표 상태는 안 바뀐다.
- 자리: `LdrpRecordModuleDependency +0xb8 ~ +0xce`, `LdrpLoadDependentModuleInternal +0xf8f ~ +0xfa5`, `LdrpFastpthReloadedDll +0x1dd ~ +0x1f6`, `LdrpBuildForwarderLink +0x154 ~ +0x16d`.
- 켜지도 끄지도 않으니 "켜는 곳 · 지우는 곳" 셈에는 안 넣었다.

### 2.13 꼬리표를 읽는 곳 — 그리고 "같은 `+0x18`" 가르기

#### 읽는 곳 `[정적]`

- **보고 건너뛰는 곳 둘**:
  - `LdrpInitializeGraphRecurse +0x3a` (`TEST byte [RBX+0x18],1` · `+0x3e JNZ`) — run27 그대로.
  - `LdrpInitializeShimDllDependencies +0xa6` (`+0xaa JNZ`) — **새로.** `ShimDll` 명부 줄의 `Dependencies` 를 돌며, `+0xcd` 에서 GR 을 부른다.
- **포인터로 읽는 곳** (맨 아래 3비트를 `AND r,-8` 로 가림):
  - `LdrpLoadContextReplaceModule +0x7c` / `+0x80` — 끈을 떼고 `+0xe7 RtlFreeHeap`.
  - `LdrpSignalModuleMapped +0x2c` / `+0x30` — 출발 카드 카운터를 1 줄이고, 0 이면 `State := 4` · `LdrpQueueWork` (run24 2.7).
- **가리지 않고 비교 · 옮김**: `LdrpMergeNodes +0x1b4` / `+0x25c` — `+0xe3` 이 이미 덮어써서 표지가 없다.

#### 같은 `+0x18` 인데 끈이 아닌 칸 `[정적]` (결과서 2-F)

`+0x18` 은 흔한 오프셋이라, `[reg+0x18]` 을 만지는 줄이 다 끈 상자는 아니다.

- 카드 `LoadCount` — 포인터가 명부 줄 `+0x98 DdagNode` 에서 온다. `OR [카드+0x18],-1` 은 "비트 0 켬" 처럼 보이지만 **다른 칸**이다.
- 그 밖: 내보내기 디렉터리 `NumberOfNames`, 로드 문맥 `+0x18`, 스택 홈 칸, 스레드풀 등.
- 가르는 근거 셋:
  1. 그 주소가 `0x20` 상자 + `NtdllBaseTag` 할당 (2.9) 에서 왔나
  2. 카드 `+0x28` / `+0x30` 열쇠고리를 돌아 얻었나
  3. `+0x18` 에 카드 포인터를 쓰고 비트 0 을 만지는 모양인가

### 2.14 test.exe 정적 import 끈은 비트 0 = 0 — 한 줄로 이으면

`[정적]`, 단계별로:

1. 배달 기사 `LdrpLoadDependentModuleInternal` 이 `+0x929` 로 끈 상자를 잡는다 (쓰레기 값으로 시작).
2. `+0xede` 에서 비트 0 을 **조건 없이** 끈다.
3. `+0xf96 ~ +0xfa5` 에서 `+0x18` 에 출발 카드를 쓰며 옛 비트 0 을 되살린다 — 옛 값이 0 이라 0 그대로.
4. 그 끈을 두 열쇠고리 꼬리에 건다 (`+0xf69` / `+0xf8b`, run23).
5. → GR `+0x3a` 에서 꼬리표가 없으니 **전부 따라간다.** dll2 → dll1 → exe 순서 (run27) 가 그대로 선다.

- 초기화 동안 꼬리표 붙은 끈이 생길 길은 셋뿐이다: 핫패치 (`LdrpRecordModuleDependency`), 포워더로 다른 DLL 을 올리는 길 (`LdrpLoadForwardedDll`), 부모가 있는 `LdrpLoadDllInternal` 호출.
  - 첫째는 test.exe 에서 안 탄다 (2.15). 셋째는 `LdrLoadDll` 길에서 안 탄다 (2.10).
  - 둘째 (포워더 길) 하나만 `[정적 추정]` 으로 남는다 → Q196 ①.
- 사진 쪽은 아직 못 봤다. 사진 로그엔 끈 상자 `+0x18` 칸이 없다 (카드의 열쇠고리 꼬리 포인터만 있다). 덤프를 다시 읽으면 볼 수 있다 → Q79 (4절).

### 2.15 (c) `LdrpRecordModuleDependency` 를 부르는 곳 — 정말 하나

#### 꼴마다 셌다 `[정적]`

| 꼴 | 결과 |
|---|---|
| 직접 `CALL` | **1곳** — `LdrpInitializeNtdllDataTableEntry +0x23e` (ret `+0x243`) |
| 꼬리 `JMP` · 조건 점프 | 0 |
| 코드가 주소를 꺼냄 (`LEA` / `MOV`) | 0 |
| 데이터가 주소를 들고 있음 | 0 (`.pdata` `RUNTIME_FUNCTION` 시작 한 줄뿐) — 전 구역 8바이트 VA · 4바이트 RVA, CFG 함수 표 포함 |

- 함수 포인터로 부르는 길까지 찾아본 셈이다. 데이터 쪽 검색에 CFG 함수 표도 들어 있었다 (교환원 = [CFG 간접 호출](../02_sprint/run28-tls-init-call-해설.md#29-cfg-중개--교환원은-통화-기록에-이름을-안-남긴다)). 한계는 2.3 과 같다 (ntdll 밖은 안 봄).

#### 그 한 곳은 언제 타나 — 단계별로 `[정적]`

1. `+0x23e` 로 가려면 `+0x140 TEST RBP,RBP` · `+0x143 JNZ +0x22c` — **셋째 인자 ≠ 0** 이어야 한다.
2. 셋째 인자가 0 이 아닌 건 **둘째** `LdrpInitializeNtdllDataTableEntry` 호출 (`LdrpInitializeProcess +0x126a`, `R8 = [LdrpNtDllDataTableEntry]`) 뿐이다. 첫째 (`+0x119d`) 는 `R8 = 0`.
3. 둘째 호출은 `+0x1253 TEST RCX,RCX` · `+0x1256 JZ +0x127b` — **핫패치 문맥 첫 8바이트 ≠ 0** 일 때만 간다 (run105).
4. run105 가 정리한 대로 test.exe 는 핫패치가 꺼져 그 칸이 0 그대로다 (`LdrpIsHotPatchingEnabled` 0 `[관찰]` run44).
5. → **test.exe 에선 `LdrpRecordModuleDependency` 가 안 불릴 것** `[정적+추측]`. 그 칸 값 자체는 동적 D3 (= Q44 와 같은 값).

- 탔다면: `+0x22c RCX = [LdrpNtDllDataTableEntry]` (원래 ntdll) · `RDX` = 새 (패치된 ntdll) 줄 · `R8 = 0` 으로 부른다. 원래 ntdll → 패치 ntdll 끈 하나에 꼬리표를 붙이는 모양이다 ([run35 해설 2.9](../02_sprint/run35-ntdll-entry-해설.md)).

### 2.16 정정 — 예전 설명의 무엇이 바뀌었나

옛 설명이 적힌 곳: run23 · run27 · run40 문서, flow.md 2.6 · 2.7 · 4장 2.7 줄. (오케스트레이터가 짝으로 준 run41 에는 이 줄들이 없었다 — run 문서 "결과서와 다르게 적은 곳".)

| | 전 | 후 (run108) |
|---|---|---|
| 꼬리표 켜는 곳 | 둘: `LdrpRecordModuleDependency +0x11a` · `LdrpLoadDependentModuleInternal +0x17d9` | **넷**: + `LdrpFastpthReloadedDll +0x1a5` · `LdrpBuildForwarderLink +0x11c`. 그중 `+0x17d9` 는 탈 수 없는 길로 보임 `[정적 추정]` |
| 꼬리표 지우는 곳 | 하나: `+0xede` (함수 이름 없이) | **둘**: `LdrpLoadDependentModuleInternal +0xede` · `LdrpMergeNodes +0xe3` |
| 보고 건너뛰는 곳 | GR `+0x3a` | + `LdrpInitializeShimDllDependencies +0xa6` |
| 꼬리표의 뜻 | "대비용 칸 표시" `[추측]` | **`[공백]`** — 아는 건 "꼬리표 붙은 끈은 그래프 켜기가 따라가지 않는다" 뿐 |
| test.exe 에서 GR 이 건너뛸 끈 | 없을 것 `[추측]` | 없음 `[정적]` (포워더 길 하나는 `[정적 추정]`) |

- **test.exe 실행 흐름 · 지도 결과는 안 바뀐다.** 바뀐 건 목록과 근거다.
- run23 · run27 · run40 문서 맨 위에 정정 한 줄씩 들어갔다. 그 셋의 해설서 맨 위에도 "고쳐 읽기 (run108)" 한 줄씩 넣었다 (본문은 그대로).
- 뜻 짐작 하나는 run 문서가 남겨 두었다 `[추측]`: 켜는 셋 (`LdrpRecordModuleDependency` · `LdrpFastpthReloadedDll` · `LdrpBuildForwarderLink`) 이 공통으로 "**부모 쪽이 따로 있는 끈**" 이라는 것. 굳힐 근거는 아직 없다 → Q196 ③.

### 2.17 Q123 — run57 트리 한 줄

- run57 문서 "호출 관계" 트리의 `└─ +0x27  ExInitializeNls` 가 **`+0x2c`** 로 고쳐졌다.
- 이유: 트리는 **ret 자리**로 쓰는 게 규칙인데, 이 줄만 call 자리였다 (`Phase1InitializationIoReady.asm` call `+0x27`, ret `+0x2c`).
- 같은 문서 "정적 — 호출 지점" 표의 그 줄 (call `+0x27`, ret `—`) 은 그대로다. ret 칸이 비었을 뿐 틀린 값은 아니다.
- run57 해설에도 같은 트리 줄이 있어서 맨 위에 "고쳐 읽기 (run108)" 한 줄을 넣었다.

### 2.18 PE_emulator 쪽 — 짧게 (결과서 요약, flow-writer 는 코드를 안 읽음)

- (a) **맞음** — 진짜 `LdrpAllocateModuleEntry` 를 부르고, 팻말 ← 1 을 진짜 `+0x1a4c` 자리에서 쓴다. `0x20` 을 직접 쓰는 줄은 없다.
  - **확인 필요 하나**: kernel32 · kernelbase 올리기가 팻말 ← 1 보다 앞인가 (줄 대조는 안 함). 2.6 이 바로 이 순서에 기대니 중요하다.
- (b) **맞음** — 끈을 만드는 쪽 · 읽는 쪽 모두 진짜 코드. **확인 필요**: `LoadLibraryExW` ×3 (팻말 3) 길도 진짜 로더로 돌리는가.
- (c) **맞음** — 첫 `LdrpInitializeNtdllDataTableEntry` 만 부르고, 둘째 갈래는 "문맥 0 이라 안 탄다" 로 줄였다.
- 고칠지는 사용자 결정이다.

### 2.19 비유로 다시 한 번

- "개업 동기" 스티커는 **한 사람** (`LdrpAllocateModuleEntry`) 만 붙인다. 팻말이 "개업 중 (1)" 일 때, 그리고 지금 직원이 알림 방 열쇠를 쥔 사람이 아닐 때만.
- kernel32 · kernelbase 는 팻말이 바뀌기 **전에** 먼저 들어와 있던 건물이라 스티커가 없다. dll1 · dll2 는 팻말이 바뀐 뒤 들어와서 스티커가 있다. 사진 여섯 장이 그대로 보여 준다.
- 스티커를 떼는 사람도 하나 — 같은 건물 줄이 둘 생겨 옛 줄을 버릴 때.
- 끈의 "따라가지 마" 꼬리표는 넷이 붙일 수 있고 둘이 뗀다. 붙이는 넷 중 하나는 입구가 막힌 골목에 서 있다.
- test.exe 의 import 끈은 배달 기사가 만들 때 **일부러 꼬리표를 떼서**, 순서 담당이 빠짐없이 따라가는 끈이다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 절 | 전 | 후 |
|---|---|---|
| 기준 | — | ntdll pdb 줄에 run108, 근거 로그 줄 (run44 사진 다시 읽음) |
| 2.4 `LdrpInitializeNtdllDataTableEntry` 트리 `+0x243` 줄 | caller 하나 (지도상) | **유일한 caller** `[정적]` (직접 1 · 꼬리 0 · 주소 상수 0 · 데이터 0) |
| 2.5 `LdrpLoadDllInternal` `[a]` · `[g]` 줄 | 끈 이야기 없음 | 부모 인자가 NULL 이 아니면 끈을 만들고 꼬리표를 켬. `LdrpLoadDll` 은 0 을 넣으니 `LdrLoadDll` 길에선 안 만듦 `[정적]` |
| 2.6 끈 절 | 켬 둘 · 지움 하나 · "대비용 칸" `[추측]` | **정정**: 켬 넷 · 지움 둘 · `+0x17d9` 죽은 길 `[정적 추정]` · 뜻 `[공백]` · test.exe 비트 0 = 0 `[정적]` |
| 2.6 `LdrpAllocateModuleEntry` `0x20` 줄 | 켜는 곳 · 조건 | + 전수로 하나 · 조건 칸 = 알림 방 열쇠 주인 · kernel32 순서 |
| 2.7 GR 절 비트 0 줄 | 건너뛸 끈 없을 것 `[추측]` | `[정적]` + `LdrpInitializeShimDllDependencies +0xa6` 하나 더 |
| 2.8 넷째 인자 줄 | dll1 · dll2 스티커는 실행 값 (Q27) | **해결됨** — 사진 P1b · P2 `0y1` `[관찰]`, `[정적+관찰]` |
| 4장 | 2.6 · 2.7 이름·뜻 줄 | run27 줄 해결됨 + 정정, run108 새 줄 셋 (동적 · 정적 · PE_emulator) |
| 그림 | — | **안 바뀜** (새 함수들은 대표 지점 밖) |

짐 지도: 3.5 `ProcessStaticImport` 줄 (켜는 줄 전수 · 떼는 줄 · kernel32 · kernelbase 0 의 까닭 · 사진 줄), 3.6 머리 메모 (끈 상자 · 비트 0 전수) + `Dependencies` · `IncomingDependencies` 줄 근거 한 마디, 새 7.39. **칸 수 · 줄 수 · 2장 숫자는 그대로**다.

### 큰 그림에서 어떻게 이어지나

- **진입점 넷째 인자 사슬이 앞쪽 끝까지 닫혔다.** run40 해설 2.10 의 "봉투 이야기" 다섯 고리 중 첫 고리 (스티커를 붙인다) 가 "어디서 · 누구에게 · 정말 그것뿐" 까지 서고, 사진으로 dll1 · dll2 에 붙은 것도 봤다.
  - 남은 건 그 순간 실제로 실리는 값 (Q27) 하나다.
- **팻말의 경계가 의미를 갖게 됐다.** "kernel32 준비 (`+0x199d`) → 팻말 1 (`+0x1a4c`) → exe import (`+0x1aa4`)" 라는 순서가 그냥 순서가 아니라 **명부 줄 표시를 가르는 선**이다. 에뮬레이터가 이 순서를 바꾸면 kernel32 · kernelbase 에 스티커가 붙어 버릴 것이다 `[추측]` (2.4 조건에서 끌어낸 것 — 2.18 의 확인 필요 하나가 이것).
- **그래프 켜기 (2.7) 의 입력이 깨끗해졌다.** GR 이 건너뛸 끈이 test.exe 엔 없다는 게 `[정적]` 이 되어, run27 의 dll2 → dll1 → exe 순서가 끈 쪽에서도 받쳐진다.
- 짐 지도로 보면 새 짐은 없다. 이미 센 칸 (`ProcessStaticImport`, `Dependencies`, `IncomingDependencies`) 의 "누가 · 왜" 가 단단해졌다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| test.exe 끈 상자 `+0x18` 의 실제 비트 (기대: 모두 0) | 사진 로그엔 그 칸이 없다 (카드 열쇠고리 꼬리 포인터만) | 동적 **Q79** — 이미 있는 덤프 (`ms/dump/03_baggage-snap/` P1b · P2) 를 오프라인으로 읽으면 새 실행 없이 닫힌다 (D1) |
| `+0x179d` (→ `+0x17d9`) 가 정말 죽은 길인가, 새로 찾은 켜는 곳 둘 · `LdrpMergeNodes +0xe3` 가 정말 안 타나 | 정적 모양 추론 `[정적 추정]` | 동적 **Q44** — 멈춤점 넷, 하나도 안 서야 맞다 (D2) |
| 둘째 `LdrpInitializeNtdllDataTableEntry` (`+0x126a`) 가 정말 안 타나 (→ `LdrpRecordModuleDependency` 안 불림) | 핫패치 문맥 칸 값은 실행 값 | 동적 **Q44** 와 같은 값 (D3) |
| (선택) `LdrpAllocateModuleEntry +0x194` 가 정확히 두 번 (dll1 · dll2) 서나 | 사진이 이미 같은 결과라 급하지 않음 | 동적 **Q44** (D4, 선택) |
| test.exe 의 포워더가 **다른 DLL 을 새로 올리는** 길을 타나 (= `LdrpBuildForwarderLink` 끈이 생기나), `LdrpLoadForwardedDll +0x2e1` 이 주는 부모 인자 | 이번 범위 밖이라 안 따라감 — "꼬리표 끈 0개" 의 남은 `[정적 추정]` | 정적 **Q196 ①** |
| 알림 방 열쇠 주인일 때 건너뛰는 갈래들의 뜻 | test.exe 는 알림 등록이 없어 안 탐 (중요도 낮음) | 정적 **Q196 ②** |
| 꼬리표 (비트 0) 의 **뜻** | 옛 짐작의 근거가 사라짐 → `[공백]` | 정적 **Q196 ③** |
| `LdrpDllNotificationLock` 을 짐 지도 7.3 "모름" 에서 3.8 로 옮길지 (2장 숫자가 바뀜) | 칸을 직접 쓰는 줄은 없고 파일 값 = 사진 값이라는 것까지는 알았다. 옮기는 건 셈 규칙 문제 | **사용자 결정 대기** |
| 진입점 넷째 인자에 실제로 실리는 값 | 스티커 쪽은 닫혔지만 실리는 순간은 실행 값 | 동적 **Q27** (그대로) |

---

## 5. 더 읽기

- run 문서: [`run108-static-import-flags.md`](run108-static-import-flags.md) — 호출 지점 표, 사진 로그 줄 번호, 전체 트리
- 결과서: [`_workspace/sast/103_static-import-flags_result.md`](../../_workspace/sast/103_static-import-flags_result.md) — 1-E (`Flags` 다른 비트 쓰기 전부), 1-G · 2-G (검색 방법과 한계), 2-F (같은 `+0x18` 가르기)
- flow.md: [2.6 import DLL 이 올라오는 순간](flow.md) 의 끈 절 · `LdrpAllocateModuleEntry` 줄, 2.5 `LdrpLoadDllInternal` `[a]` · `[g]`, 2.7 GR 절, 2.8 넷째 인자 줄, 4장 run108 줄
- 짐 지도: [`baggage.md`](baggage.md) 3.5 `ProcessStaticImport`, 3.6 머리 메모, 7.39
- 앞 해설: [run23 해설 2.3](../02_sprint/run23-load-dependent-해설.md#23-핵심--줄간선은-여기서-긋는다) (끈 상자), [run27 해설 2.6 · 2.8](../02_sprint/run27-init-graph-해설.md) (건너뛰는 끈 · 넷째 인자), [run40 해설 2.10](../02_sprint/run40-dll-crt-entry-해설.md#210-같은-바퀴에-닫은-것--q54-새-run-없이-문서로) (Q54), [run105 해설 2.7](run105-ntdll-small-해설.md) (핫패치 문맥)

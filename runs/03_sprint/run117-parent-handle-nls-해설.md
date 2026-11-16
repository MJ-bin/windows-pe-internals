# run117 parent-handle-nls — 해설

> 이 글은 [run117 문서](run117-parent-handle-nls.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: 두 항목을 한 요청·한 바퀴로 돌았다 (`_workspace/plan.md`, 둘 다 이번에 **끝**)
  - **Q88** — `PspCreateObjectHandle` · `PspUpdateCreateInfo` 가 **부모 쪽에** 쓰는 것 (run45 일 33: 부모용 핸들·출력 칸)
  - **Q89** — `nt!NtGetNlsSectionPtr` 커널 쪽 (run45 일 47: NLS VAD 의 주인)
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례다. **짐 지도 바퀴**다.
  - 남은 것이 간 곳 (4절): 동적 → 새 **Q206** (D1 ~ D7) · 정적 → 새 **Q207** (① ~ ⑤)
- 근거:
  - run 문서 [`run117-parent-handle-nls.md`](run117-parent-handle-nls.md) (가장 중요)
  - 요청서 [`_workspace/sast/112_parent-handle-nls.md`](../../_workspace/sast/112_parent-handle-nls.md) · 결과서 [`112_parent-handle-nls_result.md`](../../_workspace/sast/112_parent-handle-nls_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림 1 에 노드 하나** (`s9`) · 라벨 둘 · 번호 하나. 글은 2.1 · 2.4 · 4장 · 5장 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): **2장 숫자 그대로.** 새 **7.6.14** (부모 쪽 짐, 체크리스트 밖) · 3.17 **노드 단위 줄 3** · 3장 줄임 셋.
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 `[관찰]` 은 모두 **run44 때 찍어 둔 사진** 이다 (P1a VAD 줄 `04_baggage-read_P1a.txt:1539` · `:1540`, P1b 둘째 NLS 뷰 줄 `04_baggage-read_P1b.txt:2236` · `:2238`).
- 앞 바퀴 짝:
  - [run45 해설](run45-work-list-해설.md) — 일 33 · 47 이 처음 목록에 오른 곳
  - [run57 해설](run57-nls-apiset-해설.md) — NLS 커널 쪽을 처음 연 곳 (이번엔 거기에 **손잡이 · 구획 장부** 이야기를 보탰다)
  - [run14 해설](../02_sprint/run14-create-process-internal-해설.md) — 부모 `CreateProcessInternalW` (신청서를 내는 쪽)

---

### 이미 푼 말 — 링크만

- 관리사무소 (커널) · 신청 창구 (시스템 콜) · 가게 (프로세스) · 직원 (스레드) · 부모 cmd.exe = 새 가게를 **신청하는 쪽 가게** → [run14 해설](../02_sprint/run14-create-process-internal-해설.md) 첫머리
- [핸들](../02_sprint/run9-create-user-process-해설.md#핸들) = **손잡이**
  - run9 해설에 이미 "프로세스 핸들 `+0x10a7` · 스레드 핸들 `PspInsertThread +0x525`" 가 있다 `[정적]`.
- `ObInsertObjectEx` 는 부모에게 줄 손잡이가 **아니다** (등록만) → [run11 해설 2.9](../02_sprint/run11-allocate-thread-해설.md#29-obinsertobjectex-는-부모에게-줄-손잡이가-아니다)
- 핸들 표 = 가게의 **손잡이 목록** → [run43 해설](run43-baggage-checklist-해설.md) (`ObjectTable` 줄)
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) = 가게 문 앞 **안내판**
- VAD = 땅의 **구획 장부** → [run10 해설 "껍데기와 장부"](../02_sprint/run10-allocate-process-해설.md#껍데기와-장부)
- NLS = 관리실 책상의 **글자 사전**, 섹션 = 관리사무소 **창고의 원본 물건**, 뷰 = 그걸 가게 땅에 **펼쳐 놓은 한 판** → [run57 해설](run57-nls-apiset-해설.md) 1절 "새 말 넷"
- silo = 상가 안의 **구역** → [run57 해설 2.4](run57-nls-apiset-해설.md)
- 체크리스트 밖 짐 C1 ~ C12, 그중 **C12 = 옆집** (부모 가게 · 인사팀) 에 맡긴 물건 → [run45 해설 2.6](run45-work-list-해설.md)
- 매니페스트 = exe 에 붙인 **설정 쪽지** → [run57 해설 2.8](run57-nls-apiset-해설.md) · [run87 해설](run87-sxs-create-해설.md)

### 이번 글의 새 말 — 본문에서 처음 나올 때 한 줄씩 푼다

- **부모 핸들 표** → 2.3
- **핸들 만들기 블록** · **접근 상태** (`_ACCESS_STATE`) → 2.5
- **`PS_CREATE_INFO`** · **만들기 문맥** → 2.7
- **커널 핸들** → 2.11
- **노드 단위 줄** → 3절 (짐 지도)

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, 시스템 콜 = **신청 창구**, 프로세스 = **가게**, 스레드 = **직원**, ntdll = 가게 안 **관리실**.
- 부모 cmd.exe = 새 가게를 **신청하는 쪽 가게**. 짐 지도 말로는 **옆집** (C12).
- 핸들 = **손잡이**, 핸들 표 = **손잡이 목록**.
- **하나만 바꿔 부른다.**
  - run 문서는 이번에 부모를 "본점", 커널을 "관공서", 핸들 표를 "열쇠고리", 핸들을 "열쇠" 라고 불렀다.
  - 앞 해설들에선 이미 **관리사무소 · 손잡이 · 손잡이 목록** 이라 이 글은 그 말을 그대로 쓴다. 뜻은 같다.
  - (앞 해설에서 "열쇠" 는 잠금 칸 (락) 이었다 — [run116 해설](run116-ntdll-globals-zero-해설.md) 비유. 그래서 핸들엔 안 쓴다.)
- run 문서 그림을 말만 바꿔서 가져오면:
  - 신청하는 가게 (cmd) 가 "가게 하나 차려 줘" 하고 창구에 **신청서** (`PS_CREATE_INFO`) 를 낸다.
  - 관리사무소는 가게를 다 만든 뒤 **신청한 가게의 손잡이 목록** 에 손잡이 두 개를 건다 — **직원 손잡이 (스레드) 가 먼저**, 가게 손잡이 (프로세스) 가 나중.
  - 그리고 신청서의 **답 칸** 을 채우고, 맨 마지막에 **"처리 완료 (6)" 도장** 을 찍는다.
  - 글자 사전 (NLS) 은 새 가게 관리실이 **직접 전화로** 달라고 한다. 관리사무소는 창고에서 사전을 **관리사무소 자기 손잡이로** 꺼내 읽기 전용으로 펼쳐 주고, 손잡이는 바로 놓는다. 새 가게 손잡이 목록엔 아무것도 안 걸린다. 펼친 한 판 = 구획 장부 한 장.

### 한 줄로 먼저 — 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| 부모 손잡이 목록에 걸리는 손잡이 | **둘** — 스레드 (`PspInsertThread +0x520`) **먼저**, 프로세스 (`NtCreateUserProcess +0x10a2`) 나중 `[정적]` |
| 어느 목록인가 | **부모의 유저 핸들 표** (속성 `& 0x1df2` — 커널 핸들 비트 `0x200` 없음) `[정적]` |
| 권한은 어디서 | `PspCreateObjectHandle` 인자가 **아니라** 미리 채운 접근 상태 (`DesiredAccess` 0) `[정적]`. 최종 숫자는 동적 |
| 부모 출력 칸 둘 | `ThreadHandle` (`PspInsertThread +0x573`) → `ProcessHandle` (`NtCreateUserProcess +0x10f6`) `[정적]` |
| 신청서 답 칸 | `+0x1124 PspUpdateCreateInfo(6, …)` 한 자리. **`State` 6 은 맨 마지막** (`+0x191`) `[정적]` · 칸 이름 `[추측-이름]` |
| 조건부로 더 | 이미지 이름 속성 (`0x20005`) 이 있으면 파일 · 섹션 손잡이 **사본 둘** 이 부모 목록에 더 `[정적]` — 실제로 주는지는 동적 |
| NLS 손잡이 | **커널 핸들로 열고 바로 닫음** → test.exe 손잡이 목록은 그대로 `[정적]` |
| NLS 구획 장부 | `MiMapViewOfDataSection` 이 `_MMVAD` (0x88) **한 장 = 호출 한 번** `[정적]`. test.exe 호출 2번 (`0xb` + `0xe`) |
| 정정 | 요청서 "12 = 대문자 표" 틀림 (대문자 = `0xe`) · sast/52 `+0x2e1` 은 오타 (`+0x2e6`) |
| 그림 1 | 새 노드 `s9` (스레드 손잡이), `KeReadyThread` 번호 (5) → (6) |
| 짐 지도 2장 | **숫자 그대로** (부모 짐은 체크리스트 밖 7.6.14, VAD 는 노드 단위 줄) |

이 글에서 붙잡아 둘 세 줄:

> **(1) 손잡이는 신청한 가게 (cmd) 쪽에 걸린다. 그리고 직원 손잡이가 먼저다.**
> 직원 등록 (`PspInsertThread`) 안에서 직원 손잡이를 만들고, 그 상자에서 나온 뒤에 가게 손잡이를 만든다. 둘 다 cmd 의 **유저** 손잡이 목록이다.
>
> **(2) 신청서 답 칸은 "도장" 을 맨 마지막에 찍는다.**
> 답 칸을 다 채운 뒤에야 `State` = 6 을 쓴다. 답을 받으려는 표시 (비트) 가 꺼져 있으면 도장만 찍는다.
>
> **(3) 사전 전화는 새 가게 손잡이 목록을 건드리지 않는다.**
> 관리사무소가 자기 손잡이로 꺼내고 바로 놓는다. 가게에 남는 건 **펼친 한 판 + 구획 장부 한 장** 뿐이다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번엔 **떨어진 두 곳** 을 봤다. 하나는 부모 cmd 스레드의 끝 무렵, 하나는 test.exe 첫 스레드의 NLS 준비.

```
cmd.exe 창에서 "test.exe" 입력
 └─ KERNELBASE!CreateProcessInternalW  (부모가 신청서를 냄 — run14)
     └─ 시스템 콜 NtCreateUserProcess           ── ① 부모 cmd 스레드, 커널
          ├─ … PspAllocateProcess  (가게 · 안내판 만들기)
          ├─ +0xfc2  PspInsertProcess          등록만 (손잡이 없음)
          ├─ +0x101a PspInsertThread
          │          └─ +0x525 PspCreateObjectHandle   직원 손잡이       ← 이번 (Q88)
          ├─ +0x10a7 PspCreateObjectHandle             가게 손잡이       ← 이번 (Q88)
          └─ +0x1129 PspUpdateCreateInfo(6)            신청서 답 칸     ← 이번 (Q88)
     └─ (부모가 할 일: csrss 알림 … NtResumeThread 로 풀어 주기 — run14)
 ─ ─ ─
 test.exe 첫 스레드                              ── ② ntdll
  LdrpInitializeProcess
   └─ LdrpInitializeNlsInfo (호출 +0x1c8)
       ├─ RtlpInitCodePageTables   → NtGetNlsSectionPtr (0xb)
       └─ RtlpInitUppercaseTables  → NtGetNlsSectionPtr (0xe)
            ══▶ syscall 0x102  nt!NtGetNlsSectionPtr (커널 안)              ← 이번 (Q89)
                 └─ MiMapViewOfSection → MiMapViewOfDataSection  구획 장부 한 장
  → NtContinue 은퇴 → (CRT) → main
```

- `NtCreateUserProcess` 아래 callee 앞 `+0x..` 는 **ret 자리** 다 (run 문서 "호출 관계" 그대로). call 자리는 2.2 · 2.3 에 따로 적는다.
- Q88 은 **부모 쪽** 이다. test.exe 가 아니라 cmd 의 손잡이 목록 · cmd 의 메모리에 남는 짐이다.
- Q89 는 **test.exe 쪽** 이다. 다만 일을 하는 건 커널 안 (`nt!NtGetNlsSectionPtr`) 이다.

### 무엇이 궁금했나

run45 일 목록에서 "갈 곳이 정해지지 않은 정적 거리" 로 남은 둘이다 ([run45 해설](run45-work-list-해설.md) 4절 표 5 · 6번).

- **Q88 (일 33)**: `NtCreateUserProcess` 가 끝 무렵 부모 쪽에 **무엇을 어디에** 쓰나.
  - 부모 손잡이 목록에 거는 손잡이 둘의 권한 · 속성.
  - 부모가 준 출력 칸 (`ProcessHandle` · `ThreadHandle`) 에 쓰는 줄.
  - `PspUpdateCreateInfo` 가 신청서 (`PS_CREATE_INFO`) 의 어느 칸을 쓰나, 값은 어디서 오나.
- **Q89 (일 47)**: test.exe 의 NLS 구획 장부 (`C_949.NLS` · `l_intl.nls`) 를 **누가, 어떤 길로** 만드나.
  - 종류 번호별 갈래, test.exe 가 타는 갈래.

### 바퀴 전에 알던 것

- `[정적]` (run9) 프로세스 손잡이: `NtCreateUserProcess +0x10a2` (ret `+0x10a7`) `PspCreateObjectHandle`. 스레드 손잡이: `PspInsertThread +0x520` (ret `+0x525`) 같은 함수.
  - 그런데 그림 1 엔 **프로세스 손잡이만** 노드였다. 스레드 쪽은 2.1 나무에 괄호 메모 한 줄 (`+0x525 PspCreateObjectHandle 스레드 핸들`) 뿐이었다.
- `[정적]` (run9) `+0x1124 PspUpdateCreateInfo` 에 6 을 넘긴다. 6 = "성공" 은 `[추측]` (이름 확인 안 함).
- `[정적]` (run11 · run12) `ObInsertObjectEx` 는 손잡이를 안 만든다 (등록만).
- `[정적]` (run57) `NtGetNlsSectionPtr` 는 `\NLS\…` 이름으로 섹션을 열어 (`+0x16d ZwOpenSection`) **지금 가게에** 읽기 전용 뷰를 펼친다 (`+0x2e6 MiMapViewOfSection`). 열 때 쓴 손잡이는 `+0x1bb ZwClose` 로 놓는다.
  - 하지만 그 손잡이가 **어느 목록** 에 걸리는지, 구획 장부를 **누가** 만드는지는 몰랐다.
- `[관찰]` (run44 P1a) VAD 에 `C_949.NLS` `25996d20 ~ 25996d50` · `l_intl.nls` `25996d60 ~ 25996d62`, 둘 다 `Mapped READONLY`.

---

## 2. 찾은 것

### 2.1 한눈에 — 두 시간대 나무 (대표만)

run 문서 "호출 관계" 를 줄인 것이다. callee 앞 `+0x..` 는 ret 자리, 그냥 `+0x..` 는 쓰는 줄 자리.

```
[① 부모 cmd 스레드 — NtCreateUserProcess 안]
NtCreateUserProcess
 │  +0x251 ~ +0x266  프로세스 손잡이 속성 = 부모가 준 속성 & 0x1df2 (유저)
 ├─ +0xfc2  PspInsertProcess          등록만 · +0x1a5 SeCreateAccessStateEx (접근 상태 채움)
 ├─ +0x101a PspInsertThread           +0x3aa SeCreateAccessStateEx
 │          └─ +0x525 PspCreateObjectHandle (스레드)        ← 직원 손잡이 먼저
 │                    └─ +0x3d ObOpenObjectByPointer → +0x1bf ObpCreateHandle
 │             +0x573  부모 ThreadHandle 칸 ← 손잡이 값
 ├─ +0x1048 PsGetProcessServerSilo    +0x104f 같을 때만 계속
 ├─ +0x10a7 PspCreateObjectHandle (프로세스)                 ← 가게 손잡이
 │  +0x10bd ~ +0x10e4  이미지 정보 출력 칸 복사
 │  +0x10f6            부모 ProcessHandle 칸 ← 손잡이 값
 └─ +0x1129 PspUpdateCreateInfo(6, 문맥, 새 프로세스)
            │  (조건부) +0x8f · +0xad PspPropagateHandle → ObDuplicateObject  사본 둘
            │  +0xbf ~ +0x172  답 칸들
            │  +0x191          State = 6   ← 맨 마지막

[② test.exe 첫 스레드 — ntdll 이 부른 시스템 콜 0x102, 커널 안]
nt!NtGetNlsSectionPtr (종류 0xb · 0xe)
 ├─ +0x102  RtlpInitNlsSectionName    \NLS\NlsSectionCP949 / \NLS\NlsSectionCASE
 │  +0x11d  속성 0x2d0 (커널 핸들 포함)
 ├─ +0x172  ZwOpenSection
 ├─ +0x1b4  ObReferenceObjectByHandle  모드 0 (커널)
 ├─ +0x1c0  ZwClose                    ← 손잡이 바로 놓음
 ├─ +0x2eb  MiMapViewOfSection         지금 가게 = test.exe
 │          └─ +0x2de MiMapViewOfDataSection
 │                    ├─ +0x1d7 MiAllocatePool  (0x88B, 'Vad ') — _MMVAD 한 장
 │                    ├─ +0x2fb MiSelectUserAddress
 │                    └─ +0x461 MiInsertVad
 └─ +0x308  ObfDereferenceObject       섹션 참조 놓음
```

- 전부 `[정적]` (sast/112). `MiMapViewOfDataSection` 안쪽은 **이름 · 호출 자리만** 봤다 (칸 단위는 안 읽음).

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `nt!PspInsertThread | +0x520 | +0x525 | PspCreateObjectHandle` (run 문서 "정적 — 호출 지점" 표)

- **누가**: `PspInsertThread` — 직원을 정식 등록하는 함수. `NtCreateUserProcess +0x1015` 에서 불린다.
- **어디서**: call `+0x520`, 돌아올 자리 `+0x525`.
- **누구를**: `PspCreateObjectHandle` — 객체 하나에 **손잡이를 만들어 지금 프로세스 목록에 거는** 함수.
- **무엇을 위해**: 새 **직원 (스레드)** 의 손잡이를 부모 cmd 목록에 건다. 손잡이 값은 조금 뒤 `+0x573` 에서 부모 `ThreadHandle` 칸에 써 준다.
- 이 줄이 이번 그림의 새 노드 `s9` 다 (3절).

#### 줄 ②: `PspUpdateCreateInfo +0x191` — `State (+0x8) = 6` (run 문서 3절 표 끝 줄)

- **누가**: `PspUpdateCreateInfo`. `NtCreateUserProcess +0x1124` 에서 6 을 받고 불렸다 (`+0x111f MOV ECX,6`).
- **어디서**: `+0x191`. 함수 본체가 아니라 **떨어진 조각** 안의 줄이다 (`FUN_140a48f2d +0x8` = 함수 `+0x191`).
- **누구를**: 부른 게 아니라 **쓴** 줄이다. 대상은 부모 메모리의 신청서 `+0x8` 칸.
- **무엇을 위해**: "처리 완료" 도장. **다른 답 칸을 다 쓴 뒤** 에 쓴다. 앞에서 실패했으면 (`+0x189 TEST EDI` / `+0x18b JS`) 건너뛴다.

#### 줄 ③: `nt!NtGetNlsSectionPtr | +0x1bb | +0x1c0 | ZwClose`

- **누가**: `NtGetNlsSectionPtr` — test.exe 관리실이 부른 사전 창구.
- **어디서**: call `+0x1bb`, ret `+0x1c0`. 바로 앞 `+0x1af` 에서 `ObReferenceObjectByHandle` (모드 0) 로 섹션 객체를 **직접 붙잡았다**.
- **누구를**: `ZwClose` — 손잡이 놓기.
- **무엇을 위해**: 손잡이는 필요 없어졌다. 물건 (섹션 객체) 을 이미 손에 직접 쥐었으니까. 그래서 test.exe 손잡이 목록엔 아무것도 안 남는다 (2.11).

### 2.3 Q88 ① — 직원 손잡이가 먼저 걸린다 `[정적]`

#### 새 말: 부모 핸들 표

- **부모 핸들 표** = 만드는 쪽 (cmd) 이 새 프로세스 · 스레드를 **손에 쥐는 손잡이** 를 담는 표. **test.exe 의 짐이 아니다.**
  - 비유: 신청한 가게 (cmd) 카운터 뒤의 **손잡이 목록**. 새 가게를 열어 주면, 관리사무소가 그 목록에 "새 가게 손잡이" · "새 직원 손잡이" 를 걸어 준다. 그래야 cmd 가 나중에 새 가게를 가리킬 수 있다.

#### 차례 (run 문서 요약 첫 줄)

1. `+0xfbd` `PspInsertProcess` — 가게를 **등록만**. 손잡이는 안 만든다 ([run11 해설 2.9](../02_sprint/run11-allocate-thread-해설.md#29-obinsertobjectex-는-부모에게-줄-손잡이가-아니다) 그대로).
2. `+0x1015` `PspInsertThread` — 직원 등록. **그 안** `+0x520` 에서 직원 손잡이 → `+0x573` 부모 `ThreadHandle` 칸.
3. `+0x1043 ~ +0x104f` 서버 silo 검사 — 새 가게의 구역이 기대값과 같을 때만 계속. 다르면 `0xc000022d`.
4. `+0x10a2` 가게 손잡이.
5. `+0x10bd ~ +0x10e4` 이미지 정보 출력 칸 복사.
6. `+0x10f6` 부모 `ProcessHandle` 칸.
7. `+0x1124` `PspUpdateCreateInfo(6, …)` — 신청서 답 칸.

#### 왜 "직원이 먼저" 인가 — 단계별로

- 근거: 직원 손잡이를 만드는 자리 (`+0x520`) 는 `PspInsertThread` **안** 에 있다. 그 상자는 `NtCreateUserProcess +0x1015` 에서 불린다.
- 가게 손잡이를 만드는 자리 (`+0x10a2`) 는 `NtCreateUserProcess` **본체** 에 있고, `PspInsertThread` 가 돌아온 **뒤** (`+0x101a` 다음) 다.
- 그래서: 흐름 순서로 **직원 → 가게**. 출력 칸도 `ThreadHandle` (`+0x573`) 이 `ProcessHandle` (`+0x10f6`) 보다 먼저 쓰인다.
- 아직 확인 아님: **손잡이 번호** 도 직원 쪽이 작은지는 모른다. 번호는 목록의 빈 칸 규칙에도 좌우된다 (결과서) → 동적 D2.

비유: 관리사무소가 직원 등록 서류를 처리하면서 **그 자리에서** 직원 손잡이를 걸어 준다. 가게 손잡이는 모든 등록이 끝나고 창구로 돌아와서 건다.

### 2.4 Q88 ② — 걸리는 곳은 cmd 의 "유저" 손잡이 목록 `[정적]`

#### 근거 → 그래서

- 근거 1 — **속성**: 손잡이 속성 = 부모가 준 개체 속성 `& 0x1df2` (유저 호출일 때).
  - 프로세스: `NtCreateUserProcess +0x251 ~ +0x266` (`+0x261 AND EAX,0x1df2`).
  - 스레드: `PspAllocateThread +0x203` · 조각 `+0x2ca883` · `+0x20c` (같은 마스크).
  - 커널이 부른 경우엔 `& 0x11ff2` 다. 개체 속성이 NULL 이면 0.
- 근거 2 — `0x1df2` 에는 **`0x200` (커널 핸들) 비트가 없다.** 부모가 커널 핸들을 달라고 해도 걸러진다.
  - 비트 이름 (`0x200` = 커널 핸들 등) 은 PDB 가 아니라 공개 헤더 값이다 `[추측-이름]`.
- 근거 3 — **모드**: `PreviousMode` (부른 쪽이 유저면 1). 프로세스 `+0x1eb`, 스레드 `PspAllocateThread +0x1db`.
- 근거 4 — **어느 프로세스의 목록**: 지금은 cmd 가 부른 시스템 콜 안이고, 아직 새 가게로 "건너가지" 않았다. 그래서 "지금 프로세스" = 부모 cmd.
- 그래서: 손잡이 둘은 **부모 cmd 의 유저 핸들 표** 에 걸린다.
- 아직 확인 아님: 실제 속성 값 · 손잡이 값 → 동적 D2.

### 2.5 Q88 ③ — 권한은 인자로 안 넘긴다, 미리 적어 둔 서류에 있다 `[정적]`

#### 새 말 둘

- **핸들 만들기 블록** = `PspCreateObjectHandle` 의 둘째 인자. **0x190 바이트** 짜리 묶음. `NtCreateUserProcess` 의 지역 변수 둘 (프로세스 `RBP+0x400`, 스레드 `RBP+0x590`) 이고 `+0xc1 MOV EBX,0x190` 으로 0 을 채워 시작한다.
  - 비유: 손잡이 하나 **발급 신청 봉투**. 안에 "무슨 권한" · "어떤 손잡이" · "결과는 여기" 가 다 들어 있다.
- **접근 상태** (`_ACCESS_STATE`, PDB 타입) = 그 봉투 맨 앞 0xa0 바이트. "어떤 권한을 원했나 · 이미 준 권한은 뭔가" 를 적는 칸들이 있다 (`RemainingDesiredAccess` `+0x10` · `PreviouslyGrantedAccess` `+0x14` · `OriginalDesiredAccess` `+0x18` 등 — 결과서).
  - 비유: 봉투 안 **권한 신청 쪽지**.

봉투 안 모양 (run 문서 1절 표):

| 블록 칸 | 크기 | 뜻 |
|---|---|---|
| `+0x00 ~ +0x9f` | 0xa0 | `_ACCESS_STATE` (PDB) |
| `+0xa0 ~ +0x17f` | 0xe0 | `_AUX_ACCESS_DATA` (PDB) |
| `+0x180` | 4 | 손잡이 속성 |
| `+0x184` | 1 | 접근 모드 (`PreviousMode`) |
| `+0x188` | 8 | 손잡이 값 출력 (커널 스택 칸) |

- 봉투 전체 타입은 PDB 에 없다. 뒤 세 칸 이름은 쓰임으로 붙인 것 `[정적 — 이름 없음]`.
- 앞 두 구조체 경계는 `PspInsertProcess +0x199 LEA R9,[RSI+0xa0]` 와 PDB 크기로 맞췄다.

#### 권한이 흘러가는 길

1. 부모가 준 `ProcessDesiredAccess` (`[RBP+0x54]`) · `ThreadDesiredAccess` (`[RBP+0x50]`).
2. 보호 프로세스 호환 검사 (`+0xe94 PsTestProtectedProcessIncompatibility`):
   - **0 이면** 그대로 (`+0xe96 JZ`).
   - **1 이면** `MAXIMUM_ALLOWED` (비트 25) 일 때 표 `RtlProtectedAccess2` 로 줄이고, 비트 0 을 켠다 (`+0xf08 ~ +0xf51`, `+0xf2c OR R8D,R14D`). 비트 0 의 이름은 `[추측]` (`PROCESS_TERMINATE`).
   - test.exe 가 어느 쪽인지 → 동적 D4.
3. `PspInsertProcess +0x1a5` / `PspInsertThread +0x3aa` `SeCreateAccessStateEx` 가 봉투의 **권한 신청 쪽지** 를 채운다.
4. `PspCreateObjectHandle +0x38` `ObOpenObjectByPointer` 는 `DesiredAccess` 를 **0** 으로 넘긴다. 쪽지가 이미 봉투에 있으니까.
   - `ObOpenObjectByPointer` 는 쪽지가 NULL 일 때만 `DesiredAccess` 로 새 쪽지를 만든다 (결과서).
5. 손잡이 칸에 들어가는 최종 값은 공용 부품 `ObpCreateHandle +0x3cc ~ +0x431` 이 정한다 — `MAXIMUM_ALLOWED` → `GENERIC_ALL` 로 바꿔 형식의 일반 매핑으로 풀고, 형식의 유효 마스크로 자른다.
   - 이 갈래 조건은 결과서가 안 봤다 `[추측: 핸들 열기 길]`.
   - 최종 숫자 → 동적 D2.

비유: 손잡이 창구 직원은 봉투만 받는다. "무슨 권한?" 은 따로 묻지 않는다 — 봉투 안 쪽지를 그대로 읽는다.

- 덤: 권한을 썼고 유저 호출이면 `+0x98 SepAdtPrivilegeObjectAuditAlarm` 로 **감사 기록** 만 남긴다. 값은 안 바꾼다.

### 2.6 Q88 ④ — 부모 출력 칸 둘 `[정적]`

| 부모 칸 (부모 유저 메모리 8B) | 값 | 쓰는 줄 | 미리 확인 |
|---|---|---|---|
| `ThreadHandle` (인자 2) | 스레드 봉투 `+0x188` | **`PspInsertThread +0x573`** | 입구 `+0x219 ~ +0x22e` |
| `ProcessHandle` (인자 1) | `[RBP+0x588]` = 프로세스 봉투 `+0x188` | **`NtCreateUserProcess +0x10f6`** | 입구 `+0x201 ~ +0x216` |

- "미리 확인" = 시스템 콜 입구에서 그 칸을 **읽고 같은 값을 다시 써 보는** 식으로 쓸 수 있는 칸인지 건드려 본다 (결과서).
- 스레드 쪽은 예외 필터 `PspInsertThread$filt$1` 이 받쳐 준다 — **유저 호출일 때만** 예외를 삼킨다.
  - (예외 필터 = 잘못된 주소에 쓰다 넘어질 때 받아 주는 **그물** `[추측]` (일반적으로 알려진 설명).)
  - 쓰기가 실패하면 (`+0x55e JS +0xa4a`) 방금 건 직원 손잡이를 **도로 뗀다** (`ObCloseHandle`).
- 프로세스 쪽 쓰기 (`+0x10f6`) 가 `NtCreateUserProcess$filt$0` 그물 안인지는 예외 표를 안 봐서 `[공백]`.

### 2.7 Q88 ⑤ — 신청서 답 칸과 "맨 마지막 도장" `[정적]` · 칸 이름 `[추측-이름]`

#### 새 말 둘

- **`PS_CREATE_INFO`** = 부모가 **만들기 결과를 받으려고 넘기는 서류**. 크기 `0x58`.
  - **PDB 에 타입이 없다.** 그래서 칸 이름 (`State` · `OutputFlags` …) 은 공개 헤더 기억으로 붙인 짐작이다 `[추측-이름]`. 오프셋 · 크기 · 값 출처 · 분기 모양은 `.asm` 근거 `[정적]`.
  - 비유: cmd 가 창구에 낸 **신청서**. 아래쪽에 관리사무소가 채울 **답 칸** 이 있다.
- **만들기 문맥** = 이번 만들기 동안 커널이 들고 다니는 **작업 메모** (0x260 바이트, `RBP+0x1a0`, `PspBuildCreateProcessContext` 가 채움 — 결과서).
  - 비유: 창구 직원 책상 위의 **처리 메모지**. 신청서 위치도 여기 적혀 있다.

#### 답 칸은 cmd 메모리 "그 자체" 다

- 문맥 `+0x30` = 부모 유저 메모리에 있는 신청서 **주소** (커널 사본 아님). `PspCaptureCreateInfo` 가 심어 둔다 (그때 크기 `0x58` · 상태 0 검사).
- `PspUpdateCreateInfo +0x1d` 에서 그 주소를 꺼내 **곧장** 쓴다. 이 함수엔 그물 (`try`) 이 없다.

비유: 관리사무소가 신청서를 복사해 두고 복사본에 답을 쓰는 게 아니다. **cmd 카운터에 있는 원본 신청서** 에 직접 손을 뻗어 쓴다.

#### 상태 6 (test.exe 의 부모 쪽) 에서 쓰는 칸 — 대표만

| 신청서 칸 | 이름 `[추측-이름]` | 값 · 출처 | 줄 |
|---|---|---|---|
| `+0x10` (4B) | `OutputFlags` | 먼저 0 → 비트 다섯 (보호 프로세스 · Light · 주소 공간 덮어쓰기 · `PspGlobalFlags` 비트 0 · 매니페스트 찾음) | `+0xbf ~ +0x112` |
| `+0x48` · `+0x50` | `ManifestAddress` · `ManifestSize` | 매니페스트를 **찾았을 때만** | `+0x11a ~ +0x12b` |
| `+0x18` · `+0x20` | `FileHandle` · `SectionHandle` | 부모 목록으로 옮긴 손잡이 값 (아니면 0) — 2.8 | `+0x133` · `+0x13c` |
| `+0x28` (8B) | `UserProcessParametersNative` | 새 가게 안 매개변수 사본 주소 | `+0x140 ~ +0x147` |
| `+0x34` (4B) | `CurrentParameterFlags` | 매개변수 `Flags` | `+0x154 ~ +0x15e` |
| `+0x38` (8B) | `PebAddressNative` | `EPROCESS.Peb` (`+0x2e0`) = 새 가게 **안내판 주소** | `+0x161 ~ +0x168` |
| **`+0x08` (4B)** | **`State`** | **6 — 맨 마지막** | **`+0x191`** |

- `+0x30` · `+0x40` (WOW64 쪽, 64비트면 0) 도 쓴다 — 전체 표는 [run 문서 3절](run117-parent-handle-nls.md) · 짐 지도 7.6.14 (나).
- 짝: `+0x28` · `+0x38` 은 새 가게의 `_PEB.ProcessParameters` · `EPROCESS.Peb` 와 **같은 주소여야** 하는 값이다 (짐 지도 7.6.14). 사진에 부모가 없어 값 대조는 `[공백]`.

#### 두 가지 갈림 — 단계별로

1. **"답을 받겠다" 표시가 꺼져 있으면 도장만**:
   - 근거: `+0x4b JZ +0x189` — 문맥 `+0x10` 비트 1 이 꺼지면 끝 (`State` 쓰기) 으로 바로 뛴다.
   - 그 비트는 `PspCaptureCreateInfo` 가 부모 신청서 `+0x10` 비트 0 을 옮겨 둔 것. 이름은 `WriteOutputOnExit` 로 읽힌다 `[추측-이름]`.
   - 아직 확인 아님: 부모 KERNELBASE 가 이 비트를 켜는지는 nt 쪽에서 안 보인다 → 동적 D1.
2. **도장은 맨 마지막**:
   - 근거: `State` 쓰기 (`+0x191`) 가 다른 답 칸 쓰기 (`+0xbf ~ +0x172`) **뒤** 다.
   - 왜 그 순서인지는 근거 문서에 적혀 있지 않다. 내 짐작 `[추측]`: 신청서를 읽는 부모가 "도장 (6) 이 찍혔으면 답 칸을 믿어도 된다" 고 볼 수 있게 하려는 순서 같다. 부모 코드가 실제로 그렇게 읽는지는 KERNELBASE 쪽이라 이번에 안 봤다.

#### 다른 상태 번호 (짧게)

- 1 · 2 = 파일 열기 · 섹션 만들기 실패 (`NtCreateUserProcess +0x75a`), 3 = 금지 이미지 특성 (`PspAllocateProcess +0xa06`), 6 = 성공 (`+0x1124`).
- 번호의 이름은 PDB 가 없어서 그대로 `[추측-이름]`. 다만 **6 이 성공 길 한 곳으로만 가는 모양** 은 `[정적]`.
- 4 · 5 를 부르는 자리는 이 파일들 안에서 못 찾았다 `[공백]`.

### 2.8 Q88 ⑥ — 조건부: 손잡이 사본 둘이 더 걸린다 `[정적]` · 조건 값 `[공백]`

- 조건: `PspUpdateCreateInfo +0x75 TEST byte [RSI+8],0x20` / `+0x79 JZ`.
  - 문맥 `+0x8` 은 `PspBuildCreateProcessContext` 가 본 **속성 번호별 비트**. 비트 5 = **이미지 이름 속성 `0x20005`**.
  - 곧 부모가 신청서에 이 속성을 붙였을 때만.
- 하는 일: `+0x8a` · `+0xa8` `PspPropagateHandle` → `+0x3d ObDuplicateObject` (옵션 `2` = 같은 접근).
  - exe **파일 손잡이** (문맥 `+0xb0`) · exe **섹션 손잡이** (문맥 `+0xc0`) 의 **사본** 을 부모 목록에 하나씩 더 건다.
  - "원본 닫기" (옵션 1) 는 없다 → **원본은 그대로**.
  - 그 값을 신청서 `+0x18` · `+0x20` 답 칸에 쓴다.
- 원본은 커널 핸들일 것 `[추측]` — 파일을 열 때 속성이 `0x640` / `0x240` (`NtCreateUserProcess +0x56e ~ +0x57e`, `0x200` 포함 `[정적]`).
- 원본을 누가 닫나: `PspDeleteCreateProcessContext` (`+0x11d0`) 일 것 `[추측]` → 정적 Q207 ② · 동적 D3.
- 둘째 사본이 실패하면 첫째를 뗀다 (`+0xb1 JS +0x1d1`).
- test.exe 때 정말 생기나 → 동적 D1.

비유: 신청서에 "가게 설계도 손잡이도 같이 주세요" 칸이 체크돼 있으면, 관리사무소는 자기 손잡이는 그대로 쥐고 **복제 손잡이** 를 하나 깎아 cmd 목록에 걸어 준다.

### 2.9 Q88 ⑦ — 실패하면 거꾸로 뗀다 `[정적]`

- `PspUpdateCreateInfo` 가 실패하면 `+0x1159` · `+0x1191 ObCloseHandle` — **가게 손잡이 → 직원 손잡이** 순서로 뗀다.
- 앞쪽 (등록 · silo 검사) 에서 실패하면 손잡이 만들기 · 답 칸 쓰기까지 안 온다. 그때 신청서 `State` 는 처음 값 (0) 그대로 `[정적 추론]` (결과서).

### 2.10 Q89 ① — 종류 번호 다시 맞추기 (요청서 정정) `[정적]`

| 종류 | 섹션 이름 | test.exe 가 타나 |
|---|---|---|
| **`0xb` 코드 페이지** | `\NLS\NlsSectionCP<10진>` | **예** — ANSI (`RtlpInitCodePageTables +0x46`), ANSI ≠ OEM 이면 OEM (`+0x6c`) 도 |
| **`0xe` 대문자 표** | `\NLS\NlsSectionCASE` | **예** — `UseCaseMapping ≠ 0` (`RtlpInitUppercaseTables +0x3b`) |
| `0xc` 정규화 표 | `\NLS\NlsSectionNORM%.8x` | 아니오 |
| 그 밖 (`0xd` 포함) | — | `0xc00000ef` · `0xc0000001` |

- 요청서는 "12 = 대문자 표" 라고 썼는데 **틀렸다.** 대문자 표는 **`0xe` (14)**, `0xc` (12) 는 정규화 표다.
  - 근거: `RtlpInitNlsSectionName +0xd SUB ECX,0xb` / `+0x10 JZ` · `+0x12 SUB ECX,1` / `+0x15 JZ` · `+0x1b CMP ECX,2` / `+0x1e JNZ`.
  - flow.md · 짐 지도엔 이 틀린 값이 없었다 (확인만). run57 해설도 처음부터 `0xe` 였다.
- test.exe 호출은 **2번**:
  - ACP = OCP = 949 (`0x3b5`) → `0xb` 1번.
  - `UseCaseMapping` = `0x6a5` (≠ 0) → `0xe` 1번.
  - 값은 앞 run 사진 `[관찰]`, 갈래는 `[정적]`.

### 2.11 Q89 ② — 관리사무소 자기 손잡이로 꺼내고 바로 놓는다 `[정적]`

#### 새 말: 커널 핸들

- **커널 핸들** = 속성에 `0x200` 비트를 켜고 연 손잡이. **그 가게 (프로세스) 의 손잡이 목록에 걸리지 않는** 손잡이다 (run 문서 "프로세스 핸들 표 안 건드림").
  - 어느 목록에 걸리는지까지는 근거 문서에 없다. 일반적으로는 시스템 쪽 목록에 들어간다고 알려져 있다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 관리사무소가 **자기 허리춤에 차는 손잡이**. 가게 카운터 목록에는 안 걸린다.

#### 길 (run 문서 4절)

1. `+0xfd RtlpInitNlsSectionName` — 이름표 만들기.
2. `+0x11d` 속성 `0x2d0` = 영구 · 대소문자 무시 · 있으면 열기 · **커널 핸들**. `+0x163` 접근 4.
3. `+0x16d ZwOpenSection` — 이름으로 열기.
4. `+0x1af ObReferenceObjectByHandle` — 모드 `R9D = 0` (커널), 형식 `MmSectionObjectType`. 손잡이로 물건을 **직접 붙잡는다**.
5. `+0x1bb ZwClose` — **손잡이는 바로 놓는다.**
6. `+0x2e6 MiMapViewOfSection` — 지금 가게 (test.exe) 에 펼치기 (2.12).
7. `+0x303 ObfDereferenceObject` — 섹션을 붙잡은 것도 놓는다 (남는 참조는 뷰 쪽 `[추측]`).
8. `+0x316` 넷째 출력 ← 뷰 주소. `+0x31e` 다섯째 ← 크기 (ntdll 은 NULL 을 넘겨 안 받음).

#### run57 에서 무엇이 더해졌나

- run57 해설 2.7 은 이미 "손잡이로 물건을 붙잡고, 손잡이는 놓는다" 까지 적었다.
- 이번에 더해진 것 (run 문서 ★): 그 손잡이가 **커널 핸들** 이라 **test.exe 손잡이 목록은 처음부터 끝까지 그대로** 라는 것.
- 짐 지도 말로: **test.exe 핸들 표에는 NLS 짐이 없다** (7.6.3).

### 2.12 Q89 ③ — 펼친 한 판 = 구획 장부 한 장 `[정적]`

#### `MiMapViewOfSection` 안 갈림 (새로 본 것)

- `+0x299` 에서 섹션 쪽 플래그를 읽고:
  - `PhysicalMemory` (`+0x29d`) 켜짐 → `MiMapViewOfPhysicalSection` (`+0x409`)
  - `Image` (`+0x2a7`) 켜짐 → 조각 → `MiMapViewOfImageSection`
  - **둘 다 꺼짐** → **`+0x2d9 MiMapViewOfDataSection`**
- NLS 섹션은 파일 기반 데이터 섹션이라 셋째 갈래일 것이다 `[정적 추정 — 섹션 플래그 값은 안 봄]`.

#### `MiMapViewOfDataSection` 안 (이름만)

- `+0x1b3 MOV EDX,0x88` · `+0x1b8` 태그 `Vad ` → **`+0x1d2 MiAllocatePool`**: `_MMVAD` (긴 꼴, 0x88B — PDB 크기와 같음) **한 장** 을 받는다.
- 이어서 `+0x2f6 MiSelectUserAddress` (주소 정하기) → `+0x33c MiUpdateVadStartVpn` → `+0x3c6 MiWriteVadFlags2` → `+0x45c MiInsertVad` (장부철에 끼우기) → `+0x76d MiSetVadFlags`.
- 장부 칸에 무슨 값을 쓰는지는 본문을 안 읽어 `[공백]` → 정적 Q207 ③.

#### 그래서 "한 장 = 한 번"

- 근거: `MiAllocatePool` 로 **부를 때마다 새 장부 한 장** 을 받는다.
- 그래서: test.exe 호출 2번 → NLS 구획 장부 2장.
- `[관찰]` run44 P1a: `C_949.NLS` (`25996d20 ~ 25996d50`, 0x31쪽) · `l_intl.nls` (`25996d60 ~ 25996d62`, 3쪽), 둘 다 `Mapped READONLY`. **2장** 이 맞는다.
  - 읽기 전용 · 섹션 전체 · 64KB 정렬 (시작 `…d20` · `…d60` 이 `0x10` 쪽 정렬) 도 정적 값과 맞는다 (run 문서 "정적과 동적이 다른 곳" — 없음).
- 아직 확인 아님: **그 두 장을 이 호출이 만들었다** 는 건 콜스택을 안 봐서 여전히 `[추측]`.

#### P1b 의 둘째 쌍은 이 길이 아니다

- `[관찰]` P1b 부터 같은 두 이름이 한 장씩 더 있다 (`25996eb0` 0x31쪽 · `25996dc0` 3쪽).
- 이 함수에서 뷰를 펼치는 자리는 `+0x2e6` **하나**, 부르는 ntdll 쪽도 두 사본뿐이다 (결과서). 한 장 = 한 번이니 위 두 호출로는 4장이 안 된다.
- 그래서 둘째 쌍의 주인은 `[공백]` (KERNELBASE 쪽 `[추측]`) → 정적 Q207 ④ · 동적 D6.

비유: 관리실이 전화를 한 번 걸 때마다 관리사무소는 땅에 사전을 한 판 펼치고 **구획 장부에 새 장을 한 장** 끼운다. 장부에 사전 두 판이 두 번 나오면, 전화가 네 번 왔다는 뜻이다 — 관리실 전화는 두 번뿐이니, 나머지 두 번은 **다른 사람이** 건 것이다.

### 2.13 정정 둘 · 보탬 하나 (run 문서 "정정")

- **sast/52 결과서 "`MiMapViewOfSection` (`+0x2e1` call)" 은 오타** → call **`+0x2e6`** / ret `+0x2eb`. (`+0x2e1` 은 그 앞 `MOV RCX,[RSP+0x48]`.)
  - run57 문서 · flow.md 2.4 · 짐 지도 7.6.3 · 6장은 이미 `+0x2e6` 이라 고칠 줄 없음 (확인만).
- **요청서 "타입 12 = 대문자 표"** 틀림 → 2.10.
- flow.md 2.1 의 "결과를 복사하고, 핸들 값을 쓴다 (`+0x10b1 ~ +0x10f6`)" 를 둘로 나눴다:
  - `+0x10b1 ~ +0x10e4` = 이미지 정보 출력 칸 복사
  - `+0x10e8 ~ +0x10f6` = `ProcessHandle` 칸 쓰기
  - 틀린 게 아니라 정정 대신 보탬.
- 짐 지도 절 번호: 결과서는 "7.6.13" 을 제안했지만 7.6.13 은 run109 가 이미 써서 **7.6.14** 로 넣었다.

### 2.14 PE_emulator 에게 (run 문서 "PE_emulator 쪽", 짧게)

- flow-writer 는 `../PE_emulator` 코드를 직접 안 읽었다. 결과서가 읽은 것이다. 고칠지는 사용자가 정한다.
- **첫 프로세스 길** (`PspCreateObjectHandle` · `PspUpdateCreateInfo` 빈 함수): **맞음** — 부모가 없으니 쓸 칸이 없다.
- **게스트가 `NtCreateUserProcess` 를 부르는 길**: 부분 일치.
  - 맞음: 속성 `& 0x1df2`, `MAXIMUM_ALLOWED` → 모든 권한 (방향).
  - **다름 (a) 손잡이 순서**: 에뮬레이터는 가게 → 직원, 진짜는 **직원 먼저**.
  - **다름 (b) 신청서 답 칸**: 에뮬레이터는 `Size` · `State=6` · `PebAddressNative` 만. 진짜는 `OutputFlags` · `+0x28` · `+0x34` 도 (조건부로 파일 · 섹션 손잡이 · 매니페스트도).
  - **다름 (c)** 손잡이 사본을 안 만든다.
- **Q89**: 종류별 이름 · 뷰 값 · **손잡이 안 만듦** · 부를 때마다 새 뷰 — 맞음. "뷰 크기 = 쪽 올림" · 주소 고르는 순서 · 간격은 확인 필요 (D6).

### 2.15 디버거가 붙어서 달라지는 점

- 없음 `[추측]` — 손잡이 · 뷰 길에 디버거 깃발을 보는 갈래가 없다 (run 문서).

### 2.16 정리 — 비유로 한 번 더

1. cmd 가 창구에 **신청서** 를 낸다.
2. 관리사무소는 가게를 만들고 **등록** 한다 (손잡이 없음).
3. 직원을 등록하면서 **그 자리에서 직원 손잡이** 를 cmd 목록에 걸고, cmd 의 "직원 손잡이 칸" 에 번호를 적는다.
4. 구역 검사를 통과하면 **가게 손잡이** 를 걸고, cmd 의 "가게 손잡이 칸" 에 적는다.
5. 신청서 답 칸을 채우고 (원하면 설계도 손잡이 복제본 둘도), **맨 마지막에 "처리 완료 (6)" 도장**.
6. 따로, 새 가게 관리실이 사전을 **두 번** 전화로 달라고 한다. 관리사무소는 **자기 허리춤 손잡이** 로 꺼내고 바로 놓은 뒤, 가게 땅에 읽기 전용으로 펼치고 **구획 장부를 한 장씩** 끼운다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

#### 그림 1 — 노드 하나 · 라벨 둘 · 번호 하나

`PspInsertThread` (`a6`) 에서 나가는 화살표가 이렇게 바뀌었다:

```
바뀌기 전                              바뀐 뒤
a6 ─(1) +0x5e2─▶ KeStartThread         a6 ─(1) +0x5e2─▶ KeStartThread
a6 ─(2) +0x2e5─▶ KeSuspendThread       a6 ─(2) +0x2e5─▶ KeSuspendThread
a6 ─(3) +0x807─▶ PspCallProcessNotify… a6 ─(3) +0x807─▶ PspCallProcessNotify…
a6 ─(4) +0x4fa─▶ PspCallThreadNotify…  a6 ─(4) +0x4fa─▶ PspCallThreadNotify…
                                       a6 ─(5) +0x525─▶ PspCreateObjectHandle (s9, 스레드 핸들)  ← 새
a6 ─(5) +0x5a6─▶ KeReadyThread         a6 ─(6) +0x5a6─▶ KeReadyThread                            ← 번호만
```

- **새 노드 `s9`**: `nt!PspCreateObjectHandle` — "스레드 핸들 — 프로세스 핸들보다 먼저. 부모 유저 핸들 표, 부모 ThreadHandle 칸 쓰기 `PspInsertThread +0x573`".
  - 이 함수는 이미 그림에 `s7` (프로세스 쪽, `NtCreateUserProcess +0x10a7`) 로 있었다. 하지만 **두 번 불리는 함수도 부르는 자리마다 노드를 따로 그린다** 는 규칙 (run 문서 "R7") 이라 스레드 쪽을 따로 그렸다.
  - 전에는 2.1 나무에 괄호 메모 `(+0x525 PspCreateObjectHandle 스레드 핸들, …)` 로만 있었다.
- **`KeReadyThread` 번호**: (5) → (6). 사이에 `s9` 가 끼었기 때문이다 (번호 = 코드 순서). 5장 대표 지점 표의 그 줄도 "(6) `KeReadyThread` (run117 전 (5))" 로 바뀌었다.
- **라벨 둘**:
  - `s7`: "(정적: 프로세스 핸들)" → "(정적: 프로세스 핸들 — 부모 유저 핸들 표 (속성 & 0x1df2, run117), 부모 ProcessHandle 칸 쓰기 +0x10f6)"
  - `s8`: "(정적: 성공 길 끝)" → "(정적: 성공 길 끝. 부모 PS_CREATE_INFO 출력 칸 — State 6 은 맨 마지막 +0x191, run117)"
- 태그는 그대로 `[정적]` 이다. 올라간 것은 없다.
- **NLS 쪽은 그림에 안 넣었다.** 그림 2 는 test.exe 유저 스레드 그림이라, 커널 안쪽 (`MiMapViewOfDataSection` 등) 은 2.4 NLS 절 글에만 둔다 (run 문서 "지도 범위").

#### 글

| 절 | 바뀌기 전 | 바뀐 뒤 |
|---|---|---|
| 2.1 나무 | `PspInsertThread` 아래 괄호 메모 한 줄, (11) · (12) 이름만 | (5) `+0x525` 스레드 손잡이 + `+0x573` 줄, (6) `KeReadyThread`, (11) 프로세스 핸들 + `+0x10bd ~ +0x10e4` · `+0x10f6`, (12) 신청서 답 칸 · `State` 맨 마지막 |
| 2.1 `PspInsertThread` 흐름 10번 | "`+0x525` 스레드 핸들" | + call `+0x520`, 쓰기 `+0x573`, "프로세스 핸들보다 **먼저**" |
| 2.1 `PspCreateObjectHandle` 절 | 스레드 핸들은 `+0x525` 에서 같은 함수로 | + 차례 · 봉투 0x190 · 유저 핸들 표 · 권한 길 · 실패 정리 |
| 2.1 `PspUpdateCreateInfo` 절 | "6 = 성공" `[추측]` | 그대로 `[추측]` 위에, "6 이 성공 분기 한 곳으로만" `[정적]` + 답 칸 표 · 사본 둘 조건 |
| 2.4 NLS 절 | 열기 → 뷰 → 돌려주기 | + 커널 핸들 열고 닫음 · `MiMapViewOfDataSection` · 한 장 = 한 번 · 종류 번호 확인 |
| 4장 | run45 줄에 "일 33 갈 곳 없음" | `(해결됨 → run117)` + 새 줄 셋 (동적 D1 ~ D7 · 정적 1 ~ 5 · PE_emulator) |
| 5장 | (5) `KeReadyThread` | (6) `KeReadyThread`, (11)(12) 줄 보탬, `s9` 새 줄 (`PspInsertThread+0x56b` 에서 `r rcx`) |

### 짐 지도 `baggage.md` — 숫자는 왜 그대로인가

#### 부모 짐 14줄 → 체크리스트 밖 7.6.14

- 7.6.14 = 부모 핸들 표 (커널) **5줄** + 부모 유저 메모리 **9줄**.
  - (가) 직원 손잡이 · 가게 손잡이 · 파일 손잡이 사본 · 섹션 손잡이 사본 · 부수 칸 (핸들 수 · 참조 수 등)
  - (나) `ThreadHandle` · `ProcessHandle` · 신청서 답 칸 6줄 · `PS_ATTRIBUTE` 출력 칸
- **2장 칸 수 · 3장 줄 수에 안 센다.** 까닭: 이건 test.exe 의 짐이 아니라 **옆집 (cmd) 의 짐** 이다 (C12).
- 에뮬레이터로 옮길 때 (7.6.14 머리): 첫 프로세스 (부모 없음) 면 이 짐은 없다. 게스트가 `NtCreateUserProcess` 를 부를 때만 — 그때 손잡이는 게스트 목록, 답 칸은 게스트 메모리.
- 3장 줄임 표에 셋이 더해졌다: `①OT…` (스레드 손잡이 사슬) · `①OP…` (프로세스 손잡이 사슬) · `①UC…` (`PspUpdateCreateInfo`).

#### NLS 장부 → 3.17 "노드 단위 줄 3"

- **새 말 — 노드 단위 줄** = 짐 지도의 보통 줄은 **칸 하나 = 한 줄** 인데, 이번 줄은 **VAD 장부 한 장 (노드) = 한 줄** 이다.
  - 비유: 장부의 **칸마다** 누가 썼는지 적는 대신, "이 **장** 은 누가 끼웠나" 만 적은 줄.
  - 그래서 칸 수 · "아는 칸" · 2장 숫자에 안 센다 (`_MMVAD` 9 · `_MMVAD_SHORT` 14 모름 그대로).
- 세 줄:
  1. `C_949.NLS` 장 — 주인 nt, syscall `NtGetNlsSectionPtr` (부르는 쪽 ntdll)
  2. `l_intl.nls` 장 — 같음
  3. P1b 이후 둘째 쌍 — 주인 `[공백]`
- 칸 단위 줄이 생기면 (Q207 ③) 이 노드 줄은 머리 줄로 옮기고 그때 2장 숫자를 바꾼다 (run 문서 "도구 메모").
- 3.18 (`_MMVAD_SHORT`) 엔 "NLS 뷰는 긴 꼴 (0x88)" 메모만.

### 큰 그림에서

- **부모 쪽 (①)**: `NtCreateUserProcess` 의 **마지막 걸음들** 이 이제 "무엇을 어디에" 까지 이어졌다. 이 뒤로 부모는 커널에서 돌아와 csrss 알림 · `NtResumeThread` 로 풀어 주기를 한다 ([run14 해설](../02_sprint/run14-create-process-internal-해설.md)). 신청서 답 칸 · 손잡이 둘이 그 뒷일의 재료다 `[추측]` — 부모가 그 칸들을 실제로 읽는지는 KERNELBASE 쪽이라 확인 필요 (run 문서 PE_emulator (b)).
- **test.exe 쪽 (②)**: NLS 는 test.exe **손잡이 목록에 아무 흔적이 없고**, 남는 건 구획 장부 2장 + 칠판 (`GlobalRtlNlsState`) 의 주소뿐이라는 게 정적으로 섰다. 에뮬레이터가 "손잡이 안 만듦" 으로 짠 것이 맞다는 뜻이다.
- **다음 정적의 큰 이득**: 이번에 구획 장부를 끼우는 함수 이름 (`MiMapViewOfDataSection`) 을 찾았다. 그 본문을 칸 단위로 읽으면 (Q207 ③) NLS · ApiSet · ntdll · exe 뷰 **모두에 공용** 이라, 짐 지도 7.2 의 VAD 23칸 (`_MMVAD` 9 · `_MMVAD_SHORT` 14) 을 닫을 길이 된다.

---

## 4. 아직 모르는 것

### 동적 → Q206 (D1 ~ D7)

멈춤점은 프로세스 조건이 필요하다: 부모 cmd 의 `NtCreateUserProcess` 때, 이어서 test.exe.

| # | 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|---|
| D1 | 부모가 이미지 이름 속성 (`0x20005`) · "답 받겠다" 비트 (`WriteOutputOnExit` `[추측-이름]`) 를 켜는지 → 사본 둘이 생기나, `OutputFlags` 값 | 켜는 쪽이 KERNELBASE — nt 정적으론 안 보임 | `PspUpdateCreateInfo` 입구 (`ecx == 6`) 의 신청서 0x58B 를 입구 · `+0x1129` 직후 비교 |
| D2 | 두 손잡이의 값 (번호) · 최종 권한 · 속성, 정말 직원이 먼저 번호를 받나 | 실행 때 값. 번호는 빈 칸 규칙에도 좌우 | `PspCreateObjectHandle` 입구 · 출구 봉투 칸 → 부모 문맥 `!handle` |
| D3 | 사본의 원본이 정말 커널 핸들인지, 누가 닫나 | 원본 값 · 닫는 함수 안 봄 | `PspPropagateHandle` 입구 |
| D4 | 보호 프로세스 호환 검사가 test.exe 때 0 인지, 부모가 `MAXIMUM_ALLOWED` 를 쓰나 | 실행 때 값 | `NtCreateUserProcess+0xe94` 직후 |
| D5 | 부모 두 칸에 정말 그 값이 들어가나 | 실행 확인 없음 | `+0x10f6` · `PspInsertThread+0x573` 직후 |
| D6 | NLS 뷰 주소 · 실제 크기 (쪽 올림인지), 다른 호출자가 있나 | ntdll 은 크기를 안 받음, 콜스택 안 봄 | `NtGetNlsSectionPtr+0x2eb` (짐 지도 7.18 (1) 과 한 번에) |
| D7 | test.exe 때 "열기 실패 → 파일로 만들기" 갈래를 안 타는지 | 정적으론 안 탈 것 `[추측]` | `NtGetNlsSectionPtr+0x176 JS` |

### 정적 → Q207 (① ~ ⑤)

| # | 무엇을 모르나 | 왜 이번에 못 알았나 |
|---|---|---|
| ① | `PS_ATTRIBUTE` `ClientId` · `TebAddress` 출력 칸을 쓰는 줄 (짐 지도 7.6.14 마지막 줄 `[공백]`) | 포인터 자리만 봄 (`PspAllocateThread` 안 `[추측]`) |
| ② | `PspDeleteCreateProcessContext` (`+0x11d0`) 가 문맥의 커널 핸들 (파일 · 섹션 · IFEO) 을 언제 닫나 | 이번 범위 밖 (D3 짝) |
| ③ | **`MiMapViewOfDataSection` 본문 칸 단위** — 장부 칸 (`StartingVpn` · `EndingVpn` · `u.VadFlags` · 보호 · `Subsection` 등) | 이름 · 호출 자리만 봄. **이득이 가장 크다** — 짐 지도 3.17 · 3.18 칸 단위 줄과 7.2 VAD 23칸을 닫을 길 |
| ④ | KERNELBASE `NlsProcessInitialize` 의 `NtGetNlsSectionPtr` 자리 — P1b 둘째 NLS 쌍의 주인 | 다른 모듈 (이번은 ntoskrnl 만) |
| ⑤ | (낮음) 접근 상태의 주체 토큰 (부모 대 새 가게) | D2 로 갈리면 볼 것 |

- Q207 엔 작은 것 둘이 더 붙어 있다 (plan.md): `PspUpdateCreateInfo` 상태 4 · 5 를 부르는 자리 (`[공백]`, 2.7), `+0x10f6` 이 `$filt$0` 그물 안인지 (`[공백]`, 2.6).

### 그대로 남은 `[추측]`

- run57 7.6.3 "섹션 참조 — 남는 참조는 뷰 쪽" `[추측]` — 이번에도 칸 단위로 안 봤다.
- NLS 장부 2장을 이 호출이 만들었다는 것 `[추측]` — 콜스택은 D6.

---

## 5. 더 읽기

- run 문서: [`run117-parent-handle-nls.md`](run117-parent-handle-nls.md) — "정적 — 호출 지점" 표 · 1 ~ 4절 · "아직 확인 필요"
- 결과서: [`_workspace/sast/112_parent-handle-nls_result.md`](../../_workspace/sast/112_parent-handle-nls_result.md) — Q88 "한눈에" 표 · 질문 1-2 성공 쓰기 표 · Q89 질문 3-2 · 3-3
- 요청서: [`_workspace/sast/112_parent-handle-nls.md`](../../_workspace/sast/112_parent-handle-nls.md)
- flow.md: [그림 1](flow.md) (`s9` · `s7` · `s8`) · 2.1 `PspInsertThread` 흐름 · `PspCreateObjectHandle` 절 · `PspUpdateCreateInfo` 절 · 2.4 NLS 절 · 4장 run117 줄 · 5장
- 짐 지도: [`baggage.md`](baggage.md) 7.6.14 · 3.17 노드 단위 줄 · 7.6.3 · 3장 줄임 표 (`①OT…` · `①OP…` · `①UC…`) · 7.48
- 앞 해설: [run9](../02_sprint/run9-create-user-process-해설.md) (핸들) · [run11 2.9](../02_sprint/run11-allocate-thread-해설.md#29-obinsertobjectex-는-부모에게-줄-손잡이가-아니다) · [run12](../02_sprint/run12-insert-thread-해설.md) (`PspInsertThread` 흐름) · [run14](../02_sprint/run14-create-process-internal-해설.md) (부모 쪽) · [run45](run45-work-list-해설.md) (일 33 · 47, C12) · [run57](run57-nls-apiset-해설.md) (NLS)
- 진행표: `_workspace/plan.md` Q88 · Q89 (끝) · Q206 · Q207

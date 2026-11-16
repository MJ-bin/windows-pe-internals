# run50 eprocess-create — 해설

> 이 글은 [run50 문서](run50-eprocess-create.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

> **고쳐 읽기 (run120)**: 2.4 "범위 밖 길에서만 씀 `[추측]`" 표의 `ThreadListLock` (`+0x5e0`) 과 같은 52칸 안의 `InvertedFunctionTableLock` (`+0x5d0`) 은 범위 밖이 **아니다** — 둘 다 초기화 길에서 잡고 풀어 0 으로 돌아온다 (① `PspInsertThread +0x6c3 BTS` … `+0x24e XADD −1`, ② `RtlInsertDynamicUserFunctionTable +0xbc BTS` … `+0x125 XADD −1`). 짐 지도에선 "잡고 푸는 잠금 칸 2" 로 아는 칸에 옮겼다 (범위 밖 53 → 51, 끝까지 0 가름의 범위 밖 52 → 50). 본문은 그대로 두었다 → [run120 해설](run120-eprocess-locks-해설.md) 2.4 ~ 2.7

> **고쳐 읽기 (run123)**: 2.11 "다섯 걸음" 2번 "시스템 기본·**부모**와 합치기" 의 "부모" 는 틀림 — `PspInheritMitigationOptions` (`+0x1aea` · `+0x1be0`) 의 A 는 시스템 전역 `PspSystemMitigationOptions` (부팅 때 레지스트리 `Session Manager\Kernel` `MitigationOptions`), B 는 IFEO · 사용자 속성이고, 이 길에 부모 값은 없다. 같은 절의 "`MitigationFlags2` `0x40000000` 의 뿌리는 `PspSystemMitigationOptions` 로 보인다 `[추측]`" 도 흔들렸다 — 사진 셋째 칸 비트 16~17 (옵션 36) 이 0 이라, "0 이면 `PspApplyMitigationOptions +0x6b0` EBX 비트 19 꺼짐일 때 켬" 갈래일 수 있다 `[추측]` (EBX 뜻은 `[공백]`, Q216 ①). 본문은 그대로 두었다 → [run123 해설](run123-run58-60-leftovers-해설.md) 2.8 · 2.10

- 대기열: **Q78c2a** — Q78c2 (`_EPROCESS` 모르는 칸 241) 를 시간 순서로 나눈 **첫 조각**: "만들 때" 몫 212칸 (`K1 이전` 52 + 끝까지 0 160) (`_workspace/plan.md`, 이번에 **끝**)
  - 나눈 법 (오케스트레이터 기본값 2026-10-30): Q78c2a = 만들 때, Q78c2b = 그 뒤 구간 29칸 (`K2~K3` 3 · `K3~P0` 11 · `P0~P1` 9 · `P1~P1a` 5 · `P1a~P1b` 1) + `OwnerProcessId` + 옛 Q65 몫.
  - 다음 항목: **Q78c2b** → Q78d → Q78e → Q78f → Q78g → Q79 → Q80 → Q82.
  - 남은 동적 질문: E1·E2·E9 는 **이미 있는 덤프로** 답할 수 있어서 **Q79** 에 출처로 붙였다. E4 는 **Q35** 에 붙였다. E3·E5 ~ E8 은 새 항목 **Q97** (낮은 우선순위) 로 맨 뒤에 붙었다.
- 근거:
  - run 문서 [`run50-eprocess-create.md`](run50-eprocess-create.md) (가장 중요)
  - 요청서 [`_workspace/sast/44_eprocess-create.md`](../../_workspace/sast/44_eprocess-create.md)
  - 결과서 [`_workspace/sast/44_eprocess-create_result.md`](../../_workspace/sast/44_eprocess-create_result.md) (판정: **분석성공**)
  - 짐 지도 [`baggage.md`](baggage.md) 의 고친 곳: 기준 · 쓰는 말 · 1장 · 2장·2.1 · 3.2 `Flags` 줄 · **3.11 다시 씀** · 5장 · 6장 · 7.1·7.2·7.4·7.5·7.6·7.8 · 새 7.13 · 8장
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (기준 · 2.1 트리와 `PspAllocateProcess`·`MmCreateProcessAddressSpace`·`PspCopyAndFixupParameters` 절 · 2.4 프로세스 힙 절 한 줄 · 4장 · 4.1 · 5장. 그림은 안 바뀜)
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - 값을 맞춰 보려고 run44 덤프 읽기 로그 8장 (`ms/logs/04_baggage-read_<K1~P2>.txt` 의 `01c EPROCESS_RAW`) 을 **다시 읽었다**. 새로 VM 을 돌린 건 없다.
  - 그래서 이 글의 "사진" 은 전부 run44 때 찍어 둔 사진이다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- `_EPROCESS` (관리사무소의 **가게 장부**), 보통 길 / 복제 길, 페이지 표 (**지적도**) · VAD (**구획 장부**) → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 2절 · [보통 길과 복제 길](../02_sprint/run10-allocate-process-해설.md#보통-길과-복제-길) · [껍데기와 장부](../02_sprint/run10-allocate-process-해설.md#껍데기와-장부)
- 상가 명부 (`PspInsertProcess`) → [run11 해설 2.8](../02_sprint/run11-allocate-thread-해설.md#28-상가-명부에-올리기--호출-없이-직접-한다)
- 프로세스 매개변수 = 부모가 써 준 **개업 서류**, `PspCopyAndFixupParameters` (서류 옮기기) → [run32 해설 2.3](../02_sprint/run32-params-initblock-해설.md#23-새-말--프로세스-매개변수-블록)
- IFEO (관리사무소 서랍의 **가게 이름별 특별 지시** 폴더), ntdll 이 `Flags 0x4000` 때문에 IFEO 를 건너뛴다는 것 → [run46 해설 2.6](run46-process-heap-해설.md#26-ifeo-가-이-구간에서-처음-열린다)
- 예약 / 확정 (**말뚝 박기 / 바닥 깔기**) → [run46 해설 2.7](run46-process-heap-해설.md#27-새-말--예약--확정)
- 짐 · 짐 지도 · 줄기 · 공용 부품, "쓰는 말" (단계 · 놓는 법 · 쓴 주인 · 경로) → [run43 해설 2.2](run43-baggage-checklist-해설.md#22-읽기-0단계--짐-지도-맨-위-쓰는-말)
- 사진 지점 K1 · K2 · K3 · P0 …, "처음 값을 갖는 구간" (`K1 이전` · `끝까지 0`) → [run44 해설 2.10](run44-baggage-snap-해설.md#210-새-말--처음-값을-갖는-구간)
- "0" 의 세 가지 (0 을 씀 / 조건부 안 탐 / 아무도 안 씀) → [run47 해설 2.4](run47-section-chain-해설.md#24-새-말-0-에는-세-가지가-있다-0-을-씀--아무도-안-씀)
- 객체 만들기 `ObCreateObjectEx` (**공용 서류철 발급 창구**) → [run47 해설 2.5](run47-section-chain-해설.md#새-말-객체-만들기-obcreateobjectex)
- 토큰 = **신분증**, 핸들 표 = **손잡이 목록**, 운영 카드 (`_KPROCESS`), 몸통 0 채움, "새 가게는 부모 것을 복사해 받는다" → [run49 해설](run49-kprocess-token-htable-해설.md) · [몸통은 0 으로 채워져 온다](run49-kprocess-token-htable-해설.md#몸통은-0-으로-채워져-온다) · [2.14](run49-kprocess-token-htable-해설.md#214-핵심-새-가게는-부모-것을-복사해-받는다--에뮬레이터에는-무엇을-뜻하나-내-짐작)

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, 프로세스 = **가게**, 주소 공간 = 가게의 **빈 땅**, 짐 = **개업 준비물**.
- `_EPROCESS` = 관리사무소가 가게마다 두는 **가게 장부** (run10). 맨 앞 몇 쪽이 **운영 카드** (`_KPROCESS`, run49).
- cmd.exe = test.exe 를 낳는 **부모 가게**. 토큰 = **신분증**, 핸들 표 = **손잡이 목록** (run49).
- run 문서는 이번에 `_EPROCESS` 를 "**영업 신고서**" 라고 불렀다. 이 글은 run10 부터 쓴 **가게 장부** 를 그대로 쓴다. 뜻은 같다.
- run 문서의 다른 비유는 그대로 가져온다: 칸을 채우는 출처 넷 = **부모 가게에서 베낌 / 받은 서류 / 땅 다지기 / 상수**.
- 이 글이 더하는 비유는 새 말이 나올 때 하나씩 붙인다.

### 한 줄로 먼저: 무엇을 얻었나

| 무엇 | 답 (run 문서 그대로) |
|---|---|
| `_EPROCESS` 만들 때 몫 모르는 칸 | **212 → 여전히 모름 1** (`SharedCommitCharge`) |
| `K1 이전` 52칸 | **50칸** 은 쓰는 줄을 찾음 — `PspAllocateProcess` 와 callee 몇 개. 1칸 (`NumberOfPrivatePages`) 은 범위 밖 길 `[추측]`, 1칸은 모름 |
| 끝까지 0 160칸 | 0 을 씀 **50** · 조건부 안 탐 **30** · 아무도 안 씀 **28** · 범위 밖 길 **52** `[추측]` |
| test.exe 는 IFEO 키가 있나 | **없다.** 커널이 IFEO 값을 하나도 안 읽고, 그 표시가 매개변수 `Flags 0x4000` 이 되어 ntdll 까지 간다 `[정적]` |
| `Session` 은 누구 것 | 부모가 아니라 **지금 도는 프로세스 (호출한 쪽)** 의 세션 `[정적]`. cmd 가 띄우면 둘이 같다 |
| `PriorityClass` 2 를 처음 넣는 줄 | **`PspAllocateProcess +0x1931`** (run49 의 "못 찾음" 닫힘) `[정적]` |
| 간접 호출 두 곳 | 장부 칸은 **안 쓴다** `[정적]`. 대상은 그대로 Q35 |
| 할당 함수 이름이 어긋났던 것 | **둘 다 맞고 층만 다르다** (겉포장 `ExAllocatePoolWithTag` → 실제 `ExAllocatePool2`) |
| 짐 지도 새 셈 | **"범위 밖 길에서만 씀 `[추측]`" 53** — 모름에서 빼고 따로 한 줄 |

이 글에서 붙잡아 둘 두 줄:

> **(1) 가게 장부는 "만들 때" 거의 다 정해진다.**
> 212칸 중 130칸이 만들기 길 안에 쓰는 줄이 있다. 대부분 `PspAllocateProcess` 한 함수와 그 callee 몇 개다.
>
> **(2) "IFEO 키가 없다" 는 사실 하나가 커널에서 ntdll 까지 한 비트로 전해진다.**
> 커널 `+0xa6c` 가 메모에 `0x40` 을 켜고 → `PspCopyAndFixupParameters +0x82` 가 개업 서류 `Flags` 에 `0x4000` 을 켜고 → ntdll 이 그걸 보고 IFEO 를 건너뛴다. run46 이 본 `Flags = 0x4001` 의 출처가 이것이다.

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 구간은 **부모 cmd 스레드 안, 커널**이다. run49 와 같은 `PspAllocateProcess` 안인데, 이번엔 장부의 **나머지 칸** (`_EPROCESS` 자기 칸) 을 본다.

```
cmd.exe 의 부탁 (CreateProcessInternalW → NtCreateUserProcess)
 커널, 부모 cmd 스레드 안
  NtCreateUserProcess
   ├─ +0xc36  PspAllocateProcess                 가게 장부 만들고 칸 채우기             ← 이번 (거의 다)
   │    ├─ +0xa61  RtlpOpenImageFileOptionsKeyEx  특별 지시 서랍 열어 보기 (없음)        ← 이번
   │    ├─ +0x163c MmCreateProcessAddressSpace    땅 다지기 첫 단계 (세션·목록)          ← 이번
   │    ├─ +0x1eb8 PspApplyMitigationOptions      안전 규칙 켜기                        ← 이번
   │    ├─ +0x2095 MmInitializeProcessAddressSpace 구획 장부·최고 주소                   ← 이번
   │    └─ +0x239c PspSetupUserProcessAddressSpace
   │         └─ +0x1b9 PspCopyAndFixupParameters  개업 서류에 "IFEO 없음" 비트            ← 이번
   └─ +0xfc2  PspInsertProcess                    상가 명부에 올리기 (디버그 포트 0 등)   ← 이번 (조금)
 test.exe 메인 스레드: PspUserThreadStartup → ntdll 로더
   … LdrpInitializeExecutionOptions 가 Flags 0x4000 을 보고 IFEO 건너뜀 (run46)        ← 이번 결과가 이어지는 곳
   … NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다 (콜스택에서 보이는 숫자). run 문서 "호출 관계" 에서 대표만 골랐다.
- 사진 지점으로 보면 이번 칸은 모두 **`K1 이전`** 또는 **끝까지 0** 이다. K1 = `PspAllocateThread` 입구, 즉 `PspAllocateProcess` 가 끝난 직후다.

### 무엇이 궁금했나

run49 가 장부 앞부분 세 덩어리 (운영 카드 · 신분증 · 손잡이 목록) 를 닫았다. 남은 건 장부 **자기 칸** 241 이다. 이번엔 그중 "만들 때" 몫 212 를 봤다.

요청서 질문은 넷이었다.

1. `K1 이전` 52칸: 칸마다 쓰는 줄 (함수+오프셋) 과 **값의 출처** (부모 / 인자 / 섹션 / IFEO / 계산 / 상수).
2. 끝까지 0 160칸: 만들기 길 안에서 **0 을 씀 / 조건부 안 탐 / 아무도 안 씀** 으로 가르기.
3. 갈림길과 입력: IFEO 읽기가 어느 칸에 들어가나. **test.exe 는 IFEO 키가 없는 길인가** (run46 ntdll 쪽과 같은 결과인가). 간접 호출 두 곳이 쓰는 칸.
4. 칸마다 짐 지도 3장 양식.

- 범위 밖: 뒤 구간 29칸 (Q78c2b), `Vm`·`VadRoot` 안쪽 내용, 페이지 표 내용.

### 바퀴 전에 알던 것

- `[정적]` (sast/04 · run10) `PspAllocateProcess` 가 무엇을 부르는지 (이름과 자리).
- `[정적]` (run49) 몸통 0 채움 (`ObpAllocateObject +0x191` → `ExAllocatePoolWithTag +0x48`), `Token`·`ObjectTable` 쓰는 줄. 짐 지도 3.11 에 "**(run49) Q78c2 에서 볼 것**" 메모 (지나가며 본 쓰는 줄).
- `[관찰]` (run44) `K1 이전` 52칸의 값이 K1 덤프에 있다.
- `[관찰]` (run44 P1) 매개변수 `Flags = 0x4001`. `[정적]` (run46) ntdll `LdrpInitializeExecutionOptions` 는 `0x4000` 이면 IFEO 를 건너뛴다. 그런데 **이 `0x4000` 을 누가 켜는지는 몰랐다.**
- 짐 지도 3.11 `_EPROCESS` 는 아는 칸 **22** 였다 (263칸 중).

---

## 2. 찾은 것

### 2.1 한눈에: 장부 한 권을 한 사람이 거의 다 채운다

run 문서 "호출 관계" 를 대표만 줄였다. `+0x..` 는 ret, 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄. 모두 `[정적]`.

```
NtCreateUserProcess
 ├─ +0xc36  PspAllocateProcess
 │    │  +0x539 ObCreateObjectEx (0 채움) → +0x564 memset → +0x564~+0x611 목록 머리 자기 연결·락 0
 │    │  +0x7a0 ExitStatus=0x103 · +0x7d6 InheritedFromUniqueProcessId · +0x799 DefaultHardErrorProcessing
 │    │  +0x7fe Flags · +0x819 Flags2   (부모 우선순위 비트만 바꿔 같은 DWORD 되쓰기)
 │    │  +0x88a PartitionObject · +0x95f SectionObject · +0xa37 ImageFilePointer
 │    ├─ +0xa61  RtlpOpenImageFileOptionsKeyEx   IFEO 키 없음 → +0xa6c 메모 |= 0x40, +0xad0 값 읽기 건너뜀
 │    │  +0x1043 Machine
 │    ├─ +0x15dc PspInheritQuota                 QuotaBlock = 부모 쿼터 블록
 │    ├─ +0x163c MmCreateProcessAddressSpace     +0x1ed CommitCharge=4 · …
 │    │    ├─ +0x1df PsReserveSessionMembership  지금 도는 프로세스의 세션
 │    │    ├─ +0x208 MiCreateNewProcessTopLevelMappings  MmProcessLinks · +0x72 Flags 비트 11
 │    │    └─ +0x21f PsJoinSession               +0x17 Session · +0x21 Flags 비트 16
 │    │  +0x169c Flags 비트 18 (HasAddressSpace)
 │    ├─ +0x1927 PspInitializeProcessSecurity    SecurityDomain · ParentSecurityDomain
 │    │  +0x1931 PriorityClass=2 → +0x194f 부모 클래스가 1·5 면 복사
 │    ├─ +0x1eb8 PspApplyMitigationOptions       +0xd1 MitigationFlags 0x20 · +0x5a5 MitigationFlags2
 │    ├─ +0x206a PspInitializeFullProcessImageName
 │    │    └─ +0x7c PspSetProcessShortName       ImageFileName "test.exe"
 │    ├─ +0x2095 MmInitializeProcessAddressSpace +0x117 Flags 비트 10 · HighestUserAddress · VAD
 │    ├─ +0x239c PspSetupUserProcessAddressSpace
 │    │    └─ +0x1b9 PspCopyAndFixupParameters   메모 0x40 → 매개변수 Flags |= 0x4000
 │    │  +0x27f5 · +0x280c · +0x281a  만든 시각 셋
 └─ +0xfc2  PspInsertProcess
      └─ +0x143 DbgkCopyProcessDebugPort         +0x27 DebugPort = 0
```

비유로 한 번:

- 관리사무소가 빈 장부를 받아 (`ObCreateObjectEx`) **지우개로 한 번 더 민다** (`memset`).
- 그다음 칸을 위에서부터 채운다. 출처는 넷이다 (run 문서 비유):
  - **부모 가게 (cmd) 에서 베낌** — 부모 번호, 입출력·페이지 우선순위 비트, 오류 처리 방식, 쿼터 장부 (같이 씀), 보안 구역 번호.
  - **받은 서류 (인자)** — 섹션 객체, 이미지 파일 객체, 기계 종류.
  - **땅 다지기 (주소 공간)** — 페이지 표 시작, 작업 집합, VAD, 최고 주소, 커밋 양.
  - **상수** — 종료 상태 `0x103` ("아직 안 끝남"), 우선순위 클래스 `2` ("보통").
- 중간에 **특별 지시 서랍** (IFEO) 을 열어 보는데, test.exe 쪽지가 없다. 그래서 서랍에서 읽는 칸은 하나도 없다.
- 하나만 예외: **세션** 은 부모가 아니라 "지금 서류를 쓰는 사람 (호출한 쪽)" 의 것이다 (2.8).

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: "보통" 우선순위를 처음 넣는 줄

run 문서가 인용한 명령 한 줄 (`PspAllocateProcess +0x1931`, 오케스트레이터·run 문서 둘 다 `.asm` 확인):

```
MOV byte [R14+0x347],2
```

- **누가**: `PspAllocateProcess` (가게 장부를 만드는 함수)
- **어디서**: 자기 몸 `+0x1931`. 트리 순서로 `PspInitializeProcessSecurity` (ret `+0x1927`) 다음이다.
- **무엇을**: `R14` = 새 장부. 그 `+0x347` 칸 = `PriorityClass`. 거기에 상수 `2` ("보통") 를 쓴다.
- **무엇을 위해**: 우선순위 클래스의 **기본값**을 넣으려고. 이어서 `+0x194f` 가 부모 클래스가 `1` 또는 `5` 일 때만 부모 값으로 덮는다.
- 왜 중요했나: run49 때 "K1 값 2 를 처음 넣는 줄은 못 찾음" 이었다. `PspSetProcessPriorityClass +0x72` 는 속성 모음 비트 8 일 때만 타서 (안 탐) 답이 아니었다. 이 한 줄로 닫혔다.

#### 한 줄 읽기 ②: "IFEO 키 없음" 표시를 켜는 줄

`PspAllocateProcess +0xa6c` `OR byte [R15+0x10],0x40` `[정적]`

- **누가**: `PspAllocateProcess`
- **어디서**: `+0xa6c`. 바로 앞 `+0xa5c` 에서 `RtlpOpenImageFileOptionsKeyEx` (IFEO 키 열기) 를 부르고 (ret `+0xa61`), 실패 코드가 `0xC0000034` (키 없음) 일 때 이 줄로 온다.
- **무엇을**: `R15` = `CreateInfo`. 그 `+0x10` 바이트에 `0x40` 비트를 켠다.
  - **`CreateInfo`** (새 말): `PspAllocateProcess` 가 11번째 인자로 받는 "만들기 정보" 묶음이다. run 문서는 "**속성 모음**" 이라고도 부른다. 이미지 이름 (`+0xf0`), 이미지 파일 객체 (`+0xb8`), `DllCharacteristics` (`+0x66`) 같은 게 들어 있다 `[정적]`.
    - 비유: 개업 신청서에 붙은 **관리사무소 내부 메모지**. 신청인 (부모) 이 보는 서류가 아니라 사무소 직원끼리 돌리는 쪽지다.
- **무엇을 위해**: "이 가게는 특별 지시 쪽지가 없음" 을 메모지에 적어 두려고. 이 메모가 나중에 개업 서류로 옮겨진다 (2.12).

#### 한 줄 읽기 ③: 메모를 개업 서류로 옮기는 줄

`PspCopyAndFixupParameters +0x82` `BTS dword [RBX+0x8],0xe` `[정적]`

- **누가**: `PspCopyAndFixupParameters` (개업 서류를 새 가게 안으로 옮기는 함수, run32)
- **어디서**: `+0x82`. 앞 `+0x74` 가 `CreateInfo+0x10` 의 `0x40` 비트를 보고, `+0x7b` 가 `CreateInfo+0x11` 비트 0 이 꺼졌는지 본 뒤 이 줄로 온다.
- **무엇을**: `RBX` = 새 매개변수 블록 (개업 서류). `+0x8` 칸 = `Flags`. 비트 14 (`0xe`) 를 켠다 → `Flags |= 0x4000`.
- **무엇을 위해**: 메모지의 "쪽지 없음" 을 **ntdll 이 읽는 서류**로 옮기려고. ntdll 은 커널 메모지를 볼 수 없지만, 개업 서류는 새 가게 안에 있어서 볼 수 있다.
- run32 해설은 이 함수가 `Flags` 를 바꾸는 줄이 "`+0x82` 하나 (비트 14, 조건부)" 라고 했다. 이번에 그 **조건의 뜻**이 풀렸다.

### 2.3 212칸은 어디로 갔나

#### 표 (run 문서·결과서 맨 앞 표 그대로)

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `K1 이전` | 52 | **50** | — | — | — | 1 (`NumberOfPrivatePages`) | 1 (`SharedCommitCharge`) |
| 끝까지 0 | 160 | — | **50** | **30** | **28** | **52** | 0 |
| **합** | **212** | **50** | **50** | **30** | **28** | **53** | **1** |

- "아는 칸 (nt)" = 사진에서 값이 있고, 그 칸을 쓰는 줄을 찾았다. `*` 칸 (사진에서 뒤에 또 바뀜) 은 **처음 값을 쓰는 줄만** 닫았다.
- "0 을 씀 / 조건부 안 탐 / 아무도 안 씀" 은 [run47 해설 2.4](run47-section-chain-해설.md#24-새-말-0-에는-세-가지가-있다-0-을-씀--아무도-안-씀) 의 셋 그대로다.
- "범위 밖 길에서 씀" 은 **이번에 새로 생긴 열**이다 → 2.4.

#### 한 줄 읽기: 끝까지 0 줄

- 160칸 모두 사진 8장 내내 0 이었던 칸이다.
- 그중 50칸은 만들기 길이 **일부러 0 을 쓴다**. 30칸은 켜는 줄이 있는데 test.exe 에선 **조건이 안 맞는다**.
- 28칸은 아무도 안 쓴다. 52칸은 **만들기 길에선 안 쓰고, 다른 길이 쓰는 것 같다** `[추측]`.
- "여전히 모름" 이 0 이다. 끝까지 0 칸은 다 어딘가로 갔다.

#### 짐 지도로 옮길 때 (run47·run49 규칙 + 하나 더)

- "아는 칸" 50 + "0 을 씀" 50 + "조건부 안 탐" 30 = **130** → 짐 지도 "nt 가 씀" (줄이 있는 칸).
- "아무도 안 씀" **28** → 2장 표 아래 줄 (run47·run49 와 같음).
- "범위 밖 길에서 씀 `[추측]`" **53** → **모름에서 빼고, "아무도 안 씀" 옆에 따로 한 줄** (2.4).
- "여전히 모름" 1 (`SharedCommitCharge`) → 모름에 남김.

| | nt 가 씀 | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|
| `_EPROCESS` (263칸) | 22 → **152** | 0 → **28** | 0 → **53** | 241 → **30** | 20 + 2 → **146 + 6** |

- 정확·짐작은 짐 지도 3장 규칙 ("**한 칸에 정확 줄이 하나라도 있으면 정확**") 으로 flow-writer 가 셌다.
- 새 짐작 4 (run 문서):
  - `Job`·`JobLinks`·`CommitChargeJob`: 부르는 줄 (`PspInsertProcess +0xd4`) 과 조건만 안다. 칸을 실제로 쓰는 줄은 callee 안이라 안 읽었다.
  - `Flags.ProcessDelete`: 켜는 줄이 정말 이 칸인지 이름이 어색하다 `[추측]`.
- 남은 모름 30 = `K1 이전` 1 (`SharedCommitCharge`) + 뒤 구간 29 (→ **Q78c2b**).
- 짐 지도 전체 (구조체 21개, 직접 칸 1,456): 아는 칸 346 → **476** (정확 291 → **417** · 짐작 55 → **59**) + 아무도 안 씀 51 → **79** + 범위 밖 길에서만 씀 0 → **53** + 모름 1,059 → **848**.

### 2.4 새 셈: "범위 밖 길에서만 씀 `[추측]`"

#### 새 말

- **범위 밖 길에서만 씀** = 초기화 동안 (만들기 길 + 부른 함수) 에는 쓰는 줄이 **없고**, 종료·작업·타이머·페이지 오류 같은 **다른 길**의 함수가 쓰는 것으로 보이는 칸 (짐 지도 "쓰는 말").
  - 비유: 장부의 **"폐업일" 칸**. 개업할 때 비어 있는 건 당연하다. 그 칸은 폐업 담당이 나중에 채운다.
  - 실제로 이 52칸 안에 `ExitTime`·`LastThreadExitStatus` 가 있다 (쓰는 곳 `PspExitThread`·`PspExitLastThread` `[추측]`).

#### 대표 (run 문서 표에서)

| 칸 | 쓰는 곳 (범위 밖) `[추측]` | 어떤 길 |
|---|---|---|
| `LastThreadExitStatus` · `ExitTime` · `ThreadListLock` | `PspExitThread`, `PspExitLastThread`, `PspInsertThread` | 종료 (락은 잡았다 놓음) |
| `Read/Write/OtherOperationCount` · `…TransferCount` (6) | `IopUpdateIrpTransferCount`, `KeFoldProcessStatisticsThread` | 입출력 셈 |
| `TimerResolutionLink` 등 7 | `NtSetTimerResolution`, `ExCleanTimerResolutionRequest` | 타이머 |
| `Flags3.Crashed` · `.HangCount` · … | `PsSetProcessFaultInformation`, `DbgkForwardException` | 사고 기록 |
| `Flags3.JobVadsAreTracked` · `.VadTrackingDisabled` | `MmLinkJobProcess +0x12f` | 작업 (Job) |

- 그리고 `K1 이전` 의 `NumberOfPrivatePages` 1칸도 여기 들어갔다. 이건 K1 값이 `0x20` 이라 0 이 아니다.
  - 페이지 오류 처리 `MiCompletePrivateZeroFault +0xc4a`·`+0x1144` 의 `lock add/inc [+0x288]` 가 올린 것으로 보인다 `[추측]`.
  - 그래서 짐 지도 7.4 "짐작이라 다시 볼 것" 에도 올렸다 (동적 E6).
- 52 + 1 = **53**.

#### 왜 따로 한 줄인가 (run 문서 이유)

- "nt 가 씀" 에 넣으면? → 3장 `—` 단계 줄과 섞인다. 그 줄들은 run46 `LdrpDllDirectory` 처럼 쓰는 줄이 `[정적]` 이다. 이번 53 은 `[추측]` 이다.
- "아무도 안 씀" 에 넣으면? → **정말로** 아무도 안 쓰는 28 과 섞인다.
- 그런데 "모름" 으로 둘 수도 없다. 초기화 동안엔 쓰는 줄이 없다는 점은 `[정적]` 부재로 확인됐다. 그래서 더는 Q78 거리가 아니다.
- 그러니 **"초기화엔 안 씀 (확인) + 누가 쓰는지는 짐작"** 을 따로 센다.

### 2.5 새 말: 오프셋 훑기의 오탐

"범위 밖 길" 과 "아무도 안 씀" 의 근거가 왜 약한지 알아 두자.

#### 어떻게 찾았나

- 결과서가 `objdump -d` 로 커널 전체를 한 번 풀고, 임시 스크립트 (레포 밖) 로 **`[레지스터+칸오프셋]` 에 쓰는 명령**을 다 찾았다.
- 예: `SecurityPort` 칸 오프셋이 `0x348` 이면, 커널 어디서든 `[무엇+0x348]` 에 쓰는 줄을 모은다.

#### 왜 오탐이 나나

- **오탐** = 진짜가 아닌데 걸린 것.
- 숫자 `0x348` 은 `_EPROCESS` 만 쓰는 게 아니다. `KTHREAD`·PEB·`ERESOURCE` 같은 다른 구조체에도 같은 오프셋 칸이 있다 (run 문서).
- 명령만 봐서는 레지스터가 **어느 구조체를 가리키는지** 모른다.
- 비유: 장부 "12쪽 셋째 줄" 에 적는 사람을 찾으려고 동네 서류를 다 뒤져 "12쪽 셋째 줄에 적는 손" 을 모았다. 그런데 직원 카드에도, 안내판에도 12쪽 셋째 줄이 있다. 그 손이 **어느 서류**에 적었는지는 따로 봐야 안다.

#### 실제로 걸린 오탐 (run 문서)

- `SecurityPort +0x348` → 세션 전역·PEB 칸이었다.
- `WorkingSetWatch +0x2c0` → `MmCreatePeb` 의 PEB 칸이었다.
- 결과서 도구 평가: `KeWaitForMutexObject`·`MmCreatePeb`·`ExReleaseResourceLite` 가 걸렸다.
- 그래서 둘 다 "아무도 안 씀" 으로 갔다. 걸린 게 오탐뿐이었으니까.

#### 그래서

- "범위 밖 길 52" 와 "아무도 안 씀 28" 사이 **경계가 약하다** (run 문서).
- 줄을 `.asm` 로 한 줄씩 확인하면 단단해진다. 결과서 제안 `field_writers.py` 가 있으면 쉬워진다 — **레지스터가 어디서 왔는지** (예: `MOV R14,RCX` 가 첫 인자인지) 까지 같이 보면 오탐을 많이 줄일 수 있다 (2.19).

### 2.6 핵심 (1): 장부는 "만들 때" 거의 다 정해진다

#### 근거 → 그래서 → 아직 확인 아님

1. `[정적]` `K1 이전` 52칸 중 **50칸**의 처음 값을 쓰는 줄이 `PspAllocateProcess` 본문과 callee 몇 개 안에 있다 (run 문서 "확인된 것" 목록: `MmCreateProcessAddressSpace` · `MiCreateNewProcessTopLevelMappings` · `PsReserveSessionMembership` · `PsJoinSession` · `MmInitializeProcessAddressSpace` · `MiComputeProcessUserVa` · `MiInsertProcessVads` · `MiInsertVad` · `PspInheritQuota` · `PspInitializeFullProcessImageName` · `PspSetProcessShortName` · `PspInitializeProcessSecurity` · `PspApplyMitigationOptions`).
2. `[정적]` 끝까지 0 160칸 중 0 을 씀 **50** 과 조건부 안 탐 **30** 도 만들기 길 안에 쓰는 줄이 있다.
3. `[정적]` + 사진: K1 값과 코드가 **칸마다 맞는다** (짐 지도 3.11). 예:
   - `Flags` `0x10050c00` = 비트 10·11 (`AddressSpaceInitialized` 3) · 16 (`ProcessInSession`) · 18 (`HasAddressSpace`) · 28 (`DefaultIoPriority` 2). 켜는 줄 넷과 정확히 같다.
   - `ImageFileName` `"test.exe"`, `Machine` `0x8664`, `HighestUserAddress` `0x7fffffff0000`, `ExitStatus` `0x103`.
4. `[관찰]` 결과서가 사진 8장을 칸마다 비교했더니, `K1 이전` 칸 중 **여덟 장 내내 안 바뀐 칸**이 많다: `SectionObject` · `InheritedFromUniqueProcessId` · `Session` · `QuotaBlock` · `ImageFilePointer` · `ImageFileName` · `PriorityClass` · `HighestUserAddress` · `ExitStatus` · `Machine` · 완화 4 · `PartitionObject` · 보안 도메인 2 · 시각 3 등.
   - 바뀌는 칸 (`*`) 은 `Flags`·`Flags2` (뒤에 비트 추가, Q78c2b) · 쿼터 · 가상 크기 · VAD · `CommitCharge` · `Vm` · `SharedCommit*` 같은 **"쓸수록 쌓이는" 칸**이다.
- **그래서**: 212칸 중 130칸이 "만들 때" 정해지고, 그중 많은 칸은 그 뒤로 **한 번도 안 바뀐다**. 장부 대부분은 개업 신고 때 한 번 적고 끝이다.
- **아직 확인 아님**: 값이 정말 부모에게서 왔는지는 부모 cmd 장부와 맞춰 봐야 한다 (E1, 덤프 Q79).

#### 칸을 채우는 출처 (run 문서 "읽는 입력" 에서 대표)

| 출처 | 대표 칸 | 쓰는 줄 `[정적]` |
|---|---|---|
| **부모 (cmd)** | `InheritedFromUniqueProcessId` (부모 PID `0x19a8`) | `PAP +0x7d6` |
| | `Flags` 비트 27~29 (`DefaultIoPriority`) · `Flags2` 비트 12~14 (`DefaultPagePriority`) | `PAP +0x7fe` · `+0x819` |
| | `DefaultHardErrorProcessing` | `PAP +0x799` |
| | `QuotaBlock` (같이 씀) | `PspInheritQuota` (2.7) |
| | `SecurityDomain`·`ParentSecurityDomain` (기본) | `PspInitializeProcessSecurity +0x183 ~ +0x1a7` |
| **인자·속성 모음** | `SectionObject` (7번째 인자) | `PAP +0x95f` |
| | `ImageFilePointer` (`CreateInfo+0xb8`) | `PAP +0xa37` |
| | `Machine` | `PAP +0x1043` (← `PspSelectMachineForProcess`) |
| **땅 다지기** | `CommitCharge` 처음 값 `4` · `MmProcessLinks` · `Session` | `MmCreateProcessAddressSpace` 와 그 callee |
| | `VadRoot` · `VadCount` · `VirtualSize` · `HighestUserAddress` | `MmInitializeProcessAddressSpace` 와 그 callee |
| **상수** | `ExitStatus` = `0x103` · `PriorityClass` = `2` · `MmSlabIdentity` = `0xff` · `PageCombineSequence` = `1` | `PAP +0x7a0` · `+0x1931` · `MmCreateProcessAddressSpace +0x84` · `+0x73` |
| **PE 헤더** | `MitigationFlags` `0x20` | `PspApplyMitigationOptions +0xd1` (2.11) |
| **IFEO** | (test.exe 는 없음) | 2.12 |

- `PAP` = `PspAllocateProcess` (결과서 약어).
- run49 해설 2.14 의 "새 가게는 부모 것을 복사해 받는다" 가 장부 자기 칸에서도 이어진다. 신분증·손잡이 목록뿐 아니라 **우선순위 비트·오류 처리·쿼터·보안 구역 번호**도 부모에게서 온다.

### 2.7 새 말: 쿼터 — 부모와 같은 한도 통장을 쓴다

#### 새 말

- **쿼터** (quota) = 프로세스가 커널 메모리 (풀) 를 **얼마나 쓸 수 있나** 의 한도와, 지금까지 **얼마나 썼나** 의 셈 `[추측]` (일반적으로 알려진 설명). 이 코드에서 쓰는 부품 이름도 `PsChargeProcessPagedPoolQuota`·`PsChargeProcessNonPagedPoolQuota` 다.
  - 비유: 가게가 관리사무소 공용 창고를 쓸 수 있는 **한도 통장**. 쓰면 통장에서 빠진다.

#### 찾은 것 `[정적]`

- `QuotaBlock` (`+0x2f8`): `PspInheritQuota` (호출 `+0x15d7`) 가 **부모의 쿼터 블록을 같이 가리키게** 한다. 블록의 참조 두 칸을 올리기만 한다 (`+0xc` · `+0x13` · `+0x1a`).
  - 비유: 새 통장을 만드는 게 아니라 **부모 가게 통장의 공동 명의**로 들어간다.
  - 새 블록을 만드는 갈래 `PspAssignProcessQuotaBlock` (호출 `+0x158f`) 는 12번째 인자 (토큰 핸들 속성) 가 0 이 아닐 때만이다 (`+0x1574`). test.exe 는 안 탄다.
- `ProcessQuotaUsage` · `ProcessQuotaPeak` (`+0x200` · `+0x210`): 공용 부품 `PspChargeQuota` 가 사용량을 **더하고** (`+0x95`), 최대값을 올린다 (`+0xeb`). 부르는 쪽은 주소 공간 만들기 · 핸들 표 · VAD 셋이다.
  - 처음 사진 값은 `0x4d8` · `0x2998`. 어느 종류 번호가 어느 칸인지는 안 맞췄다 (동적 E8).

### 2.8 새 말: 세션 — 부모가 아니라 "서류 쓰는 사람" 의 세션

#### 새 말

- **세션** = 로그인한 사용자 화면 하나에 딸린 프로세스 묶음 `[추측]` (일반적으로 알려진 설명). 같은 세션의 프로세스끼리 화면·창 같은 것을 같이 쓴다.
  - 비유: 상가 건물의 **층**. 같은 층 가게들은 그 층의 복도·게시판을 같이 쓴다. 새 가게도 어느 층에 들어갈지 정해야 한다.

#### 찾은 것 `[정적]`

- `PsReserveSessionMembership` (`MmCreateProcessAddressSpace` 호출 `+0x1da`) 이 **지금 도는 스레드** (`GS:[0x188]`) 의 프로세스 `+0x2e8` 을 읽고 참조를 올린다.
- 그 값을 `PsJoinSession` (호출 `+0x21a`) 이 받아:
  - `+0x17` `Session` 칸에 쓰고
  - `+0x21` `Flags.ProcessInSession` (비트 16) 을 켜고
  - `+0x41 ~ +0x65` `SessionProcessLinks` 로 세션의 프로세스 목록 꼬리에 단다.

#### 왜 "부모" 가 아니라 "호출한 쪽" 이라고 굳이 말하나

- 다른 칸 (`InheritedFromUniqueProcessId`, 우선순위 비트) 은 `PspAllocateProcess` 첫 인자 = **부모 프로세스**에서 읽는다.
- 세션만 **지금 이 코드를 돌리는 프로세스**에서 읽는다. 부모 핸들이 따로 있어도 같다 (결과서).
- cmd 가 test.exe 를 띄우면 "부모" 와 "지금 도는 프로세스" 가 **둘 다 cmd** 라서 결과는 같다.
- 비유: 개업 신청서의 "부모 가게" 란에 누가 적혀 있든, **층은 지금 창구에서 서류를 쓰는 사람의 층**으로 정해진다. 보통은 그 사람이 부모 가게 주인이라 차이가 안 난다.
- 세션이 다른 갈래 `PspAttachSession` (호출 `+0x151f`) 은 토큰 핸들 속성이 있을 때 등이라 test.exe 는 안 탄다.

### 2.9 땅 다지기 표시 두 비트, 그리고 커밋 양

#### `Flags.AddressSpaceInitialized` 두 비트는 다른 사람이 켠다 `[정적]`

- 비트 11 = `MiCreateNewProcessTopLevelMappings +0x72` (`lock or 0x800`) — 최상위 페이지 표 (지적도 첫 장) 를 만든 직후.
- 비트 10 = `MmInitializeProcessAddressSpace +0x117` (`lock or 0x400`) — 작업 집합·VAD 준비가 끝났을 때.
- 둘 다 켜지면 칸 값이 3 이다. 사진 K1 값 3 과 맞다.
- 비유: 땅 다지기 **1차 검사 도장** 과 **2차 검사 도장**. 다른 검사관이 따로 찍는다.
- run49 메모는 "비트 11 ← `MiCreateNewProcessTopLevelMappings +0x72`" 까지였다. 맞았고, 비트 10 이 다른 함수라는 걸 보탰다.
- 그 밖에 `+0x169c` 가 `HasAddressSpace` (비트 18) 를 켠다. `MmCreateProcessAddressSpace` 가 성공한 뒤다.

#### 새 말: 커밋 양 (`CommitCharge`)

- **커밋 양** = 이 가게가 **확정** ([run46 해설 2.7](run46-process-heap-해설.md#27-새-말--예약--확정)) 받은 메모리를 센 값 `[추측]` (일반적으로 알려진 설명, 단위는 쪽 수로 보인다).
  - 비유: 빈 땅 중 **바닥을 깐 넓이의 합계**를 적는 칸.
- 찾은 것 `[정적]`:
  - 처음 값 **`4`** 를 `MmCreateProcessAddressSpace +0x1ed` 가 쓴다 (`MOV [+0x3d8],4`). 결과서는 "주소 공간 만들 때 최상위 페이지 표 하나치" 라고 적었다.
  - 그 뒤 `MiChargeProcessCommitment` 가 더하고 최대값 (`CommitChargePeak`) 을 올린다. 사진 K1 값은 `0x58` 이다.
- run49 의 "이름 모름" 셋이 풀렸다: `+0x540` = `MmProcessLinks` (전역 프로세스 목록 꼬리), `+0x3d8` = `CommitCharge`, `+0x410` = `Vm.Instance.VmWorkingSetList`.
- 비슷한 이름 `SharedCommitCharge` 는 이번에 **유일하게 못 닫은 칸**이다 (4장).

### 2.10 보안 구역 번호: 기본은 부모 값 복사

- `SecurityDomain`·`ParentSecurityDomain` (`+0x760`·`+0x768`) 은 `PspInitializeProcessSecurity` (호출 `+0x1922`) 안 `+0x183 ~ +0x1a7` 이 쓴다 `[정적]`.
- **기본은 부모의 `SecurityDomain` 복사**, 둘에 같은 값이다.
- 새 번호 (`PsNextSecurityDomain` +1) 는 예외 갈래 셋일 때만이다 `[정적]`:
  - ⓐ 작업 breakaway 플래그
  - ⓑ 부모 `MitigationFlags` 부호 비트 (`IsolateSecurityDomain`)
  - ⓒ 새 프로세스 `Flags3.VmProcessorHost` 이면서 부모 값이 0
- 사진 K1 값은 둘 다 `0x100000040`. 이게 부모 값과 같은지는 아직 안 맞춰 봤다 (E1).
- run49 메모는 "부모 값 또는 `+1`" 이었다. 조건을 붙여 고쳤다 (2.17).

### 2.11 새 말: 완화 옵션 — test.exe 의 `0x20` 은 PE 헤더에서 왔다

#### 새 말

- **완화 옵션** (Mitigation) = 공격을 어렵게 하려고 켜는 보안 설정 묶음 `[추측]` (일반적으로 알려진 설명). 주소 무작위 배치 (ASLR) 나 CFG 같은 것.
  - 비유: run10 해설이 부른 대로 가게의 **안전 규칙**. "문은 두 겹으로", "금고 자리는 매번 바꾸기" 같은 규칙들.

#### 다섯 걸음 `[정적]`

`PspAllocateProcess` 안에서 이 순서로 간다.

1. IFEO 에서 읽기: `PspReadIFEOMitigationOptions` (호출 `+0x1a8b`) · `PspReadIFEOMitigationAuditOptions` (`+0x1afa`)
2. 시스템 기본·부모와 합치기: `PspInheritMitigationOptions` (`+0x1aea` · `+0x1be0`)
3. 굳히기: `PspHardenMitigationOptions` (`+0x1c82`)
4. **켜기**: `PspApplyMitigationOptions` (호출 `+0x1eb3`, ret `+0x1eb8`) — `MitigationFlags` 는 `+0xd1`, `MitigationFlags2` 는 `+0x5a5`
5. test.exe 는 IFEO 키가 없어서 1번은 **값 없음**이다 (2.12).

#### test.exe 의 `MitigationFlags = 0x20` 은 어디서 왔나

- `0x20` = `HighEntropyASLREnabled` (결과서).
- 출처는 IFEO 가 아니라 **exe 파일의 PE 헤더** `DllCharacteristics` (`CreateInfo+0x66`) 다 `[정적]`. 이 비트가 `PspApplyMitigationOptions` 인자 비트가 되어 `+0xd1` 에서 켜진다.
- 버전 대조 (결과서): `sample/bin/test.exe` 의 `DllCharacteristics` 는 `0x8160` 이다. HIGH_ENTROPY_VA (`0x20`) 는 있고 CFG (`0x4000`) 는 없다. 사진 값과 맞다.
- 다만 같은 값을 켤 수 있는 줄이 하나 더 있다: `MiInitializeProcessBottomUpEntropy +0x19d` (전역 바이트 조건). 값이 같아서 사진으로는 어느 쪽인지 못 가른다 (E7). 결과서는 ⓐ 쪽은 "확실히 켠다" 고 했다.
- `MitigationFlags2` `0x40000000` (비트 30 `CetDynamicApisOutOfProcOnly`) 은 합친 옵션에서 왔다. 그 뿌리는 시스템 기본값 `PspSystemMitigationOptions` 로 보인다 `[추측]` (E2).

### 2.12 핵심 (2): "IFEO 키 없음" 이 한 비트로 ntdll 까지 간다

이번 바퀴에서 가장 깔끔하게 이어진 줄이다. 천천히 한 걸음씩.

#### 걸음 1. 커널이 서랍을 열어 본다 `[정적]`

- `PspAllocateProcess +0xa5c` 가 `RtlpOpenImageFileOptionsKeyEx` 를 부른다 (ret `+0xa61`).
- 이미지 이름 (`CreateInfo+0xf0`) 으로 IFEO 아래 test.exe 키를 열려 한다. 성공하면 키 핸들이 `CreateInfo+0xc8` 에 들어간다.
- 비유: 관리사무소 직원이 **특별 지시 폴더**에서 "test.exe" 쪽지를 찾는다.

#### 걸음 2. 없으면 메모지에 적는다 `[정적]`

- 실패 코드 `0xC0000034` (키 없음) 이면:
  - `+0xa6c` `CreateInfo+0x10 |= 0x40` (2.2 한 줄 읽기 ②)
  - `+0xa71` 키 핸들 칸을 0 으로.

#### 걸음 3. 서랍에서 읽는 일을 통째로 건너뛴다 `[정적]`

- `+0xad0` 이 `CreateInfo+0xc8` 을 읽어 0 이면 `+0xfa5` 로 **IFEO 값 읽기 구간 전체를 건너뛴다**.
- 완화 옵션 읽기 아래 `PspReadOptionsMapFromIFEO` 도 `+0xc8` 이 0 이면 `0xC000000D` 로 바로 돌아간다 (`+0x1f ~ +0x2b`, `.asm` 없음).
- 그래서 test.exe 장부 칸 중 **IFEO 에서 온 값은 하나도 없다.**

키가 있었다면 무엇이 어디로 갔을까 (결과서 3.1 표, test.exe 는 다 안 탐):

| IFEO 값 | 들어가는 곳 |
|---|---|
| `Debugger` · `AppExecutionAliasRedirect` | 만들기 포기 (칸 없음) |
| `PerfOptions` (`IoPriority`·`PagePriority`) | `Flags.DefaultIoPriority` · `Flags2.DefaultPagePriority` · `PriorityClass` |
| `NodeOptions` | `KPROCESS.IdealGlobalNode`, `Flags2.PropagateNode` |
| `MitigationOptions` · `MitigationAuditOptions` | `MitigationFlags*` 등 |
| `ImageExpansionMitigation` = 2 | `Flags4.ImageExpansionDisable` |

#### 걸음 4. 메모가 개업 서류로 옮겨진다 `[정적]`

- 한참 뒤 `PspSetupUserProcessAddressSpace +0x1b4` → `PspCopyAndFixupParameters` 가 개업 서류를 새 가게로 옮긴다 (run32).
- 그 안에서 (2.2 한 줄 읽기 ③):
  - `+0x74` `TEST byte [R8+0x10],0x40` — 메모지에 "쪽지 없음" 이 있나
  - `+0x7b` `CreateInfo+0x11` 비트 0 이 꺼졌나 (이 비트의 뜻은 근거에 없다)
  - `+0x82` 매개변수 `Flags |= 0x4000`
- 여기서 하나만 짐작이 섞인다: 이 함수가 받는 `R8` 이 `CreateInfo` 라는 연결은 `.asm` 인자 흐름으로 본 것이라 `[정적 + 추측]` 이다.

#### 걸음 5. 사진에 찍혔다 `[관찰]`

- run44 사진 P1 매개변수 `Flags = 0x4001`. 이 중 `0x4000` 이 걸음 4 의 비트다.
- 나머지 `0x1` 은 정규화 비트 (flow.md `PspCopyAndFixupParameters` 절, 이 함수는 안 건드림).

#### 걸음 6. ntdll 이 보고 건너뛴다 `[정적]` (run46)

- ntdll `LdrpInitializeExecutionOptions` 가 `Flags & 0x4000` 이면 IFEO 읽기를 건너뛴다.
- 그래서 **커널 쪽 결과와 ntdll 쪽 결과가 같다**: 이 exe 는 IFEO 키가 없다.

#### 헷갈리기 쉬운 것 둘

- **`0x40` 과 `0x4000` 은 다른 칸의 다른 비트다.** `0x40` 은 커널 메모지 (`CreateInfo+0x10`) 의 비트, `0x4000` 은 개업 서류 (`Flags`) 의 비트 14 다. 뜻은 같다 ("키 없음").
- **ntdll 힙 관리자는 따로 서랍을 연다.** run46 이 본 대로 힙 관리자는 IFEO **바탕 키**를 직접 열어 `GCInterval` 을 읽는다. 이건 이번 커널 읽기와 **별개**다.

#### 비유로 정리

- 관리사무소 직원이 특별 지시 폴더에서 test.exe 쪽지를 못 찾았다.
- 내부 메모지에 "쪽지 없음" 이라고 적는다. 그래서 폴더에서 이것저것 읽어 오는 일을 다 건너뛴다.
- 개업 서류를 새 가게에 넣어 줄 때, 메모지의 그 한 줄을 서류 귀퉁이에 **도장으로 옮겨 찍는다**.
- 새 가게 관리실 (ntdll) 은 그 도장을 보고 "폴더 다시 볼 필요 없네" 하고 넘어간다.
- [run46 해설 2.6](run46-process-heap-해설.md#26-ifeo-가-이-구간에서-처음-열린다) 은 이걸 "부모가 개업 서류에 표시해 줬다" 고 그렸다. 이번에 보니 도장을 찍는 손은 **관리사무소 (커널)** 다. 부모 cmd 의 스레드 안에서 도는 일이라 "부모 쪽" 인 건 맞다.

### 2.13 끝까지 0 160칸 (대표)

판단 기준 (결과서 한 줄): 만들기 길 함수의 `.asm` 에 그 칸을 쓰는 줄이 있으면 → 0 을 쓰거나 같은 DWORD 를 읽어 되쓰면 **0 을 씀**, 켜는 줄인데 조건이 안 맞으면 **조건부 안 탐**. 줄이 없으면 커널 전체 훑기로 → 이름 있는 쓰는 함수가 보이면 **범위 밖 길 `[추측]`**, 안 보이면 **아무도 안 씀**.

#### 0 을 씀 50 `[정적]`

- 명시 0 저장 **11** — 락·목록 칸들. 예: `RundownProtect` (`PAP +0x58c`), `memset` 직후 묶음 `PAP +0x5e7 ~ +0x611` (`EnclaveLock`·`PpmPolicyLock` 등 6), `DebugPort` (`DbgkCopyProcessDebugPort +0x27`).
- 입력값 0 을 씀 **4** — `Protection`·`SignatureLevel`·`SectionSignatureLevel` (`PAP +0x5c4`·`+0x5d2`·`+0x5e0`), `ServerSilo` (`PAP +0x97f`). 값은 인자로 받는데, cmd 가 보호 프로세스가 아니라 0 이다 `[관찰 간접]`.
- 같은 DWORD 되쓰기 **35** — `Flags2` 19비트 (`PAP +0x819`) · `Flags` 16비트 (`PAP +0x7fe`).
  - 비유: 부모 장부의 "우선순위" 몇 칸만 베껴 **한 줄을 통째로** 새로 적었다. 그 줄의 다른 칸들은 자연히 빈칸이 된다. 빈칸도 "적은" 것으로 센다.

#### 조건부 안 탐 30 `[정적]` (갈래별로)

- 복제 길 (섹션 없음): `ForkInProgress` 등
- WoW64 x86: `WoW64Process` 등 — `Machine` 이 `0x8664` 라 안 탐
- 작업 (Job): `Job`·`JobLinks`·`CommitChargeJob` — 부모 `Job` = 0
- 디버그 포트: `Flags.NoDebugInherit`
- IFEO 값: `Flags2.ForceWakeCharge` 등 — 키 없음
- 세션: `Flags2.CrossSessionCreate`, `Flags4.SessionFirstProcess` (세션 새로 만들기 — smss 만)
- CPU 집합 (그룹 2개 이상 / IFEO) 4, 속성 없음 2, 부모에 없음 (`SyscallProvider` 등) 3, 전역 조건 (`Flags.VmTopDown`, `LockedPagesList`), 그 밖에 (`LargePrivateVadCount`, `MitigationFlags3`·`Values`)

#### 아무도 안 씀 28

- 예: `MmReserved`, `Spare0`·`Spare1`, `CloneRoot`, `Win32WindowStation`, `SecurityPort`, `WorkingSetWatch`, `DxgProcess` 등 (전체 목록은 run 문서).
- `DxgProcess` 는 `dxgkrnl.sys` 가 쓸 칸으로 보인다 `[추측]`.

### 2.14 test.exe 가 안 타는 큰 갈래 (대표)

| 갈래 | 가르는 곳 `[정적]` | test.exe |
|---|---|---|
| 작업 (Job) | `PspInsertProcess +0x7d` 부모 `Job` | 안 탐 (사진 `Job` = 0) |
| 디버그 포트 | `DbgkCopyProcessDebugPort` 가 부모 것 복사 | 안 탐 (사진 `DebugPort` = 0) |
| WoW64 | `Machine == 0x14c` | 안 탐 (`Machine` = `0x8664`) |
| 보호 프로세스 | `Protection`·`SignatureLevel` 입력 | 값 0 을 씀 |
| 피코 | `PspCreatePicoProcess` — 다른 시스템 콜 | `NtCreateUserProcess` 길 아님 |
| 복제 (섹션 없음) | 지역 플래그 비트 `4` (`PAP +0x2ec ~ +0x317`) | 안 탐 (섹션 있음) |
| 세션 새로 만들기 | ProcessFlags 비트 7 | 안 탐 (smss 만) |
| 쿼터 블록 새로 | 12번째 인자 ≠ 0 (`+0x1574`) | 안 탐 |

- 지역 플래그 비트 `4` 는 "**복제 (섹션 없음)**" 이지 "부모가 있음" 이 아니다 (결과서 정정 7). test.exe 는 부모 (cmd) 가 **있는데도** 이 비트가 0 이다. 섹션이 있기 때문이다.

### 2.15 간접 호출 두 곳: 장부 칸은 안 쓴다

- `PAP +0x1215` (ret `+0x121a`) — `SeCiCallbacks+0xc8` 을 읽어 부른다. 실패·정리 블록 안이라 지역 버퍼를 비우는 호출로 보인다 `[추측]`.
- `PAP +0x17bf` (ret `+0x17c4`) — `SeCiCallbacks+0xc0`. 이미지 파일 객체 (`CreateInfo+0xb8`) 와 지역 버퍼를 넘긴다. 결과 버퍼가 `PspInitializeProcessSecurity` 의 입력 (토큰 쪽) 이 된다.
- 둘 다 **장부 칸을 직접 쓰지 않는다** `[정적]`.
- 대상은 CI 가 등록한 콜백으로 보이지만 `[추측]` — 표를 통째로 복사해서 채우기 때문에 쓰는 줄이 안 보인다. 그래서 대상은 그대로 **Q35**.
- 비유: 사무소 직원이 바깥 **검사 업체**에 전화를 두 번 거는데, 장부엔 아무것도 안 적는다. 받은 답은 신분증 쪽 서류에 쓰인다. 어느 업체 번호인지는 전화번호부 (표) 가 실행 때 채워져서 모른다.

### 2.16 할당은 두 층: 겉포장과 실제 (정정 보탬)

#### 무엇이 어긋났었나

- run49 는 "`ObpAllocateObject +0x191` → `ExAllocatePoolWithTag +0x48`" 로 0 채움을 설명했다.
- 그런데 문서 사이에서 할당 함수 이름이 `ExAllocatePoolWithTag` 로도, `ExAllocatePool2` 로도 불렸다. 결과서는 이걸 "할당 함수 이름이 어긋났던 것" 이라 불렀다.

#### 풀이: 둘 다 맞다, 층만 다르다 `[정적, .asm 없음 — objdump]`

```
PspAllocateProcess +0x534 ObCreateObjectEx
 → ObCreateObjectEx +0x148 ObpAllocateObject → ObpAllocateObject +0x197 ExAllocatePoolWithTag (겉포장)
 → ExAllocatePoolWithTag +0x9f ExAllocatePool2 (실제 할당)
```

- **겉포장 / 실제** (새 말): 겉포장 함수는 받은 요청을 조금 고쳐서 **진짜 일하는 함수에 넘기기만** 하는 얇은 함수다.
  - 비유: 공용 서류철 발급 창구 (`ObCreateObjectEx`, run47) 뒤에 **접수 데스크** (`ExAllocatePoolWithTag`) 가 있고, 그 뒤에 **실제 창고** (`ExAllocatePool2`) 가 있다. 접수 데스크는 "빈 종이로 주세요" 같은 표시만 붙여서 창고로 넘긴다.
- 0 채움 비트:
  - `ObpAllocateObject +0x191` `BTS ECX,0xa` 가 "0 채움" 비트를 켠다.
  - `ExAllocatePoolWithTag +0x48` `BT EBX,0xa; CMOVB` 가 그 비트로 "초기화 안 함" 을 **안 붙이는** 쪽으로 가른다.
- 그러니 run49 의 `+0x48` 은 **그대로 맞다**. 그 줄은 0 채움을 **가르는** 줄이고, 실제 할당은 한 층 아래 `+0x9f` 다.
- `ObpAllocateObject` 가 `ExAllocatePoolWithTag` 를 **부르는** 자리는 `+0x197` 이다 (`+0x191` 은 비트를 켜는 줄).
- run49 문서는 안 고쳤다 (그 문장 그대로 맞음).

#### 덤

- 그 위에 `PspAllocateProcess +0x55f memset` 이 몸통 + 확장 영역을 **한 번 더** 0 으로 민다. 겹치는 안전장치다.
- `PspAllocateProcess` 자기 안의 다른 할당은 `ExAllocatePool2` 를 **직접** 부른다: `+0xd75` `'PsCs'` (허용 CPU 집합, IFEO 갈래), `+0xfc4` `'WowP'` (WoW64, 안 탐), `+0x213a` `'SePa'` (복제 길, 안 탐).

### 2.17 run49 메모를 확인해서 옮겼다 (정정)

짐 지도 3.11 "(run49) Q78c2 에서 볼 것" 메모 중 고친 셋:

| 메모 (전) | 확인 (후) `[정적]` |
|---|---|
| `SecurityDomain`·`ParentSecurityDomain` "부모 값 또는 `PsNextSecurityDomain` +1" | **기본은 부모 값 복사**, +1 은 예외 갈래 셋 (2.10) |
| `PriorityClass` "K1 값 2 를 처음 넣는 줄은 못 찾음" | `PspAllocateProcess +0x1931` (2.2 ①) |
| `Flags.AddressSpaceInitialized` "bit 11 ← `MiCreateNewProcessTopLevelMappings +0x72`" | 맞음. **bit 10 은 다른 함수** `MmInitializeProcessAddressSpace +0x117` (2.9) |

- 짐 지도는 이 스프린트 문서라 메모 자리를 바로 고쳤다.
- 그 밖 (run 문서 "정정"):
  - 결과서 1.1 의 `MitigationFlags2` 경로 "`PspInheritMitigationOptions → PspApplyMitigationOptions`" 는 `_calls.tsv` 로 보면 **둘 다 `PspAllocateProcess` 가 부른다** (`+0x1aea`·`+0x1eb3`). 짐 지도엔 나란히 적었다.
  - 결과서 정정 7 (지역 플래그 비트 `4`) 은 run49 문서·짐 지도에서 고칠 곳을 못 찾았다. flow.md 2.1 의 "복제 길. 부모 있음, 섹션 없음" 은 "섹션 없음" 이 가르는 조건이라 틀린 말은 아니다.

### 2.18 정리: 비유로 한 번 더

1. 부모 가게 (cmd) 가 관리사무소에 "새 가게 내 주세요" 한다 (`NtCreateUserProcess`).
2. 관리사무소는 빈 장부를 받아 **지우개로 한 번 더 민다** (`ObCreateObjectEx` → 접수 데스크 → 창고, 그리고 `memset`).
3. **부모 장부에서 몇 칸을 베낀다**: 부모 번호, 우선순위 비트, 오류 처리 방식, 보안 구역 번호. 우선순위 줄은 통째로 다시 적어서 나머지 칸은 빈칸이 된다.
4. **받은 서류**에서 섹션·이미지 파일·기계 종류를 옮겨 적는다.
5. **특별 지시 폴더**를 연다. test.exe 쪽지가 없다. 내부 메모지에 "쪽지 없음" 을 적고, 폴더에서 읽을 일을 다 건너뛴다.
6. **한도 통장**은 부모 가게 통장에 공동 명의로 들어간다.
7. **땅을 다진다.** 지적도 첫 장 → 1차 도장, 상가 층 (세션) 배정 — 층은 서류를 쓰는 사람의 층으로 —, 동네 목록에 이름 올리기, 바닥 깐 넓이 처음 값 4.
8. 우선순위 클래스에 **"보통" (2)** 을 넣는다.
9. **안전 규칙**을 켠다. test.exe 는 설계도 (PE 헤더) 에 "주소 무작위 넓게" 표시가 있어서 `0x20` 이 켜진다.
10. 간판 이름 칸에 **"test.exe"** 를 적는다.
11. 구획 장부를 쓰고 → 2차 도장.
12. 개업 서류를 새 가게에 넣어 주면서, 메모지의 "쪽지 없음" 을 서류 귀퉁이에 **도장 `0x4000`** 으로 옮겨 찍는다.
13. 개업 시각 셋을 적는다.
14. 상가 명부에 올릴 때 디버그 포트 칸에 0 을 적는다.
15. 나중에 새 가게 관리실 (ntdll) 이 도장 `0x4000` 을 보고 폴더 확인을 건너뛴다 (run46).

### 2.19 곁가지: 도구 메모

run 문서 "도구 메모" 에서 — 하네스 제안이라 오케스트레이터가 사용자에게 묻는다 (미결).

- **제안 `field_writers.py`** (sast/43 제안 1 을 다시, 더 구체적으로; plan 결정 기록엔 "세 번째"): 구조체의 직접 칸 오프셋 전체에 대해 `[reg+오프셋]` 쓰기 명령 (mov/or/and/bts/btr/xadd/cmpxchg, `lock` 포함) 을 `칸 / 함수+오프셋 / 명령 / 지도 깊이` 표로 뽑자는 것. 2.5 의 오탐을 줄이려면 **레지스터가 어디서 왔는지**도 같이.
- `pdb_struct.py --expand` (앞 바퀴 제안) 는 이번에도 `Flags`·`Flags2`·`Flags3`·`MitigationFlags*` 비트에 필요했다.
- 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 | ntkrnlmp.pdb 쓴 run 목록에 run50 없음 | run50 추가. "(run50) 정적 바퀴, 덤프 읽기 로그 8장 다시 읽음" 한 줄 |
| 2.1 제목 | … run49 | … **run50** |
| 2.1 트리 (`PspAllocateProcess` 아래) | 없음 | `+0xa61 RtlpOpenImageFileOptionsKeyEx` · `+0x15dc PspInheritQuota` · `+0x1eb8 PspApplyMitigationOptions` · `+0x206a PspInitializeFullProcessImageName → +0x7c PspSetProcessShortName` (모두 `[정적]`, "그림엔 없음") |
| 2.1 트리 (`MmCreateProcessAddressSpace` 아래) | 없음 | `+0x1df PsReserveSessionMembership` · `+0x21f PsJoinSession` `[정적]` |
| 2.1 트리 `PspCopyAndFixupParameters` 줄 | "매개변수 복사, PEB +0x20 쓰기 (run32)" | + "**IFEO 키 없음 → Flags 0x4000** (run50)" |
| 2.1 `PspAllocateProcess` 절 | — | 새 묶음 "`_EPROCESS` 칸 대부분도 이 함수와 callee 몇 개가 쓴다" `[정적]`: 몸통 (할당 두 층) · IFEO 키가 없는 길 · 쿼터 · 세션 · 표시 두 비트 · 우선순위 클래스 · 완화 옵션 · 이미지 이름 |
| 2.1 간접 호출 두 곳 | 대상 "동적으로만 앎" | + `[정적]` 읽는 전역 = `SeCiCallbacks+0xc8`·`+0xc0`, **장부 칸 안 씀**. 대상은 그대로 Q35 |
| 2.1 `MmCreateProcessAddressSpace` 절 | — | + `[정적]` 세션 = 지금 도는 프로세스 것, `MmProcessLinks`, 비트 11, 커밋 처음 값 4 |
| 2.1 `PspCopyAndFixupParameters` 절 | `+0x82` 비트 14 켜기 "조건부" 만 | + `[정적]` 조건 = `+0x74` 메모 `0x40` (**IFEO 키 없음**, `PAP +0xa6c` 가 켬) · `+0x7b` `+0x11` 비트 0 꺼짐. test.exe 는 켜짐 |
| 2.4 프로세스 힙 절 | ntdll 이 `0x4000` 때문에 IFEO 건너뜀 (`[관찰]` 값 + `[정적]` 사용) | + `[정적]` (run50) 그 `0x4000` 은 **커널이 IFEO 키를 못 찾았다는 표시** |
| 4장 | 간접 호출 줄, run47 섹션 짐 줄, run49 `_EPROCESS` 줄 | 간접 호출 줄에 `[정적]` 보탬, run47 줄 **(일부 해결)**, run49 줄 **(해결됨)**. 새 줄: (해결됨) Q78c2a · (run50 새로) 실행 확인 E1 ~ E9 → Q79 / Q35 / Q97 · (run50 새로) 뒤 구간 29칸 → Q78c2b |
| 4.1 | 간접 호출 "대상이 전역 포인터" | + "(run50: `SeCiCallbacks+0xc8`·`+0xc0`, EPROCESS 칸 안 씀 `[정적]`)" |
| 5장 | — | run50 줄 둘 |

- 그림 (mermaid) 은 안 바뀌었다. 새 단계·새 caller 가 없고, `PspAllocateProcess` 안 대표 지점이 늘었을 뿐이다 (run 문서 "그림").

태그 변화 대표:

- 매개변수 `Flags 0x4000` 의 **출처**: 몰랐음 (값은 `[관찰]`, 쓰임은 ntdll `[정적]`) → **`[정적]` `PspAllocateProcess +0xa6c` → `PspCopyAndFixupParameters +0x82`** (단 `R8` = `CreateInfo` 연결은 `[정적 + 추측]`).
- `PriorityClass` 처음 값 줄: 못 찾음 → **`[정적]` `+0x1931`**.
- 간접 호출 두 곳: "동적으로만 앎" → 전역 이름·"칸 안 씀" 은 **`[정적]`**, 대상은 여전히 `[추측]` (Q35).
- `_EPROCESS` 211칸: 모름 → `[정적]` 줄 (130) / 부재 (28) / `[추측]` (53).

### 짐 지도 `baggage.md` 에서 바뀐 것 (여덟째판)

- **3.11 `_EPROCESS` 를 다시 썼다**: 17줄 → **87줄** (새 줄 70). 줄 끝 "**Q78c2a 로 닫힘**".
- "쓰는 말" 에 새 셈 **"범위 밖 길에서만 씀 `[추측]`"** 이 생겼다 (2.4). 2장 표 아래에 "아무도 안 씀" 옆 한 줄로 센다 (커널 53, `_EPROCESS` 만).
- `_EPROCESS` = nt 152 + 안 씀 28 + 범위 밖 53 + 모름 30 = 263.
- 7.2 의 닫힌 칸은 `(해결됨 → run50)` 으로, 남은 `SharedCommitCharge` 는 7.2·7.5, `NumberOfPrivatePages` 는 7.4 "짐작이라 다시 볼 것".
- 7.6 에 칸 표 없는 짐 후보 기록만: 확장 영역 안 `_PROCESS_DISK_COUNTERS` · `_PO_PROCESS_ENERGY_CONTEXT` · `_PROCESS_NETWORK_COUNTERS`, 풀 블록 `_MMWSL_INSTANCE` (`'MiP2'`), 쿼터 블록 `_EPROCESS_QUOTA_BLOCK` (부모와 공유), 세션 구조 `_PSP_SESSION_SPACE`.
- 새 7.13 = 이번 동적 질문 (plan Q79·Q35·Q97).
- 2장 숫자는 2.3 표 그대로.

### 큰 그림에서 어떻게 이어지나

- **커널과 ntdll 이 한 줄로 이어졌다.** run46 은 ntdll 쪽에서 "`0x4000` 이라 IFEO 를 건너뛴다" 를 봤고, 이번엔 커널 쪽에서 "왜 `0x4000` 인가" 를 봤다. flow.md 2.1 과 2.4 가 서로를 가리키게 됐다.
  - 에뮬레이터 쪽에서 보면 `[추측]`: IFEO 키 없는 exe 를 흉내 내려면 커널 몫으로 매개변수 `Flags` 에 `0x4000` 을 켜 둬야 ntdll 이 같은 길을 간다. 이 비트 하나가 커널 몫 IFEO 읽기 결과를 대신 전하는 셈이라서다.
- **가게 장부가 거의 다 열렸다.** run49 가 장부 앞부분 세 덩어리를, 이번이 장부 자기 칸 중 만들 때 몫을 닫았다. `_EPROCESS` 모름이 241 → **30** 이 됐다.
- **"부모에서 온다" 가 더 굵어졌다.** run49 해설 2.14 의 짐작 (부모가 없는 에뮬레이터는 이 값들을 입력으로 넣어 줘야 할 것 같다 `[추측]`) 에 우선순위 비트·오류 처리·쿼터·보안 구역 번호가 더해졌다. 세션은 "부모" 가 아니라 "호출한 쪽" 이라는 단서도 붙었다.
- 다음 조각 **Q78c2b** 는 그 뒤 구간 29칸이다. 이번에 지나가며 본 줄을 넘겼다:
  - `Flags.ProcessInserted` (`K2~K3`, 비트 26) ← `PspInsertProcess +0x202` (`.asm` 확인, 닫을 수 있음)
  - `ActiveThreadsHighWatermark` ← `PspInsertThread +0x678`, `Flags4.ThreadWasActive` ← `+0x67f`
  - `Flags2.PrimaryTokenFrozen` 을 켜는 줄이 둘 — `PspDisablePrimaryTokenExchange +0x22` 와 `PspInsertThread +0x77b`
  - `ExceptionPortData`·`Enable…Logging` 비트·`WakeChannel` 등은 만들기 길에 없음 → `NtSetInformationProcess`·프로세스 알림 쪽

---

## 4. 아직 모르는 것

멈춤점은 모두 커널이라 **모든 프로세스에 걸린다.** test.exe 일 때만 서게 하는 조건은 dast-mentor 몫이다 (run 문서). **급한 것은 없다** — 이것들을 안 닫아도 칸 표는 완성돼 있다.

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| **E1** 자식 값이 정말 부모 (cmd) 값 복사인가: `SecurityDomain`·`ParentSecurityDomain`, `Flags` 비트 27:3, `Flags2` 비트 12:3, `DefaultHardErrorProcessing`, `PriorityClass`, `QuotaBlock`, `Session`, `Job`, `DebugPort`, `SyscallProvider`, `Protection`/`SignatureLevel` | 정적으로는 "복사하는 줄" 까지. 부모 장부를 열어 맞춰 본 적은 없음 | 덤프 **Q79**: K1 덤프에서 `!process 19a8 0` → `dt nt!_EPROCESS` 후 자식 K1 값과 대조 (sast/43 D1 과 같은 덤프) |
| **E2** 커널 전역 값: `PspSystemPartition` · `PspSystemMitigationOptions` 24B (`MitigationFlags2` 비트 30 의 뿌리) · `nt+0xfc6ac8` 바이트 · `MmRegistryState+0x20` · `MmTrackLockedPages` | 실행 때 값 | 덤프 **Q79**: `dq`/`dd`/`db` 한 번씩 |
| **E9** 부모 `Job`·`DebugPort` 가 정말 0 인가 (안 탐 확정) | 위와 같음 | 덤프 **Q79** (E1 과 같은 덤프) |
| **E3** IFEO 키 없음의 **직접** 증거 (`eax` = `0xC0000034`, 메모 비트 `0x40`) | 인자·상태는 사진에 안 남음. 결과는 사진 P1 `Flags=0x4001` 로 간접 확인됨 | 멈춤 **Q97**: `nt!PspAllocateProcess+0xa61` 에서 `r eax`, `db @r15+0x10 L1`. 조건: 지금 도는 프로세스 = cmd.exe (새 가게는 아직 이름이 없음) |
| **E4** 간접 호출 `SeCiCallbacks+0xc0`/`+0xc8` 의 대상과 시점 | 표를 통째로 복사해 채워서 정적으로 안 보임 | 멈춤 **Q35** 그대로 |
| **E5** `SharedCommitCharge` K1 `0x2b` 를 올린 줄 (**유일한 "여전히 모름"**) | 줄 후보만 둘 (`MiInsertSharedCommitNode +0x33e` / `MiRemoveSharedCommitNode +0x173`). 부르는 쪽도 복제 (`MiAllocateChildVads`)·작업 (`MmLinkJobProcess`)·섹션 열기 (`MiSectionOpen`) 로 갈림 | 멈춤 **Q97**: `nt!MiInsertSharedCommitNode+0x33e` 에서 `k` (조건: `rcx` = 새 EPROCESS). 정적은 Q78c2b 가 같이 |
| **E6** `NumberOfPrivatePages` K1 `0x20` 이 정말 페이지 오류로 오른 것인가 | 만들기 길에 쓰는 줄 없음, 훑기로 본 짐작 `[추측]` | 멈춤 **Q97**: 새 프로세스를 만든 직후 `ba w8 <eproc>+0x288` 콜스택 |
| **E7** `MitigationFlags = 0x20` 을 실제로 켠 줄 (`PspApplyMitigationOptions +0xd1` / `MiInitializeProcessBottomUpEntropy +0x19d`) | 두 줄이 같은 값을 씀 | 멈춤 **Q97**: 두 줄에 걸고 먼저 오는 쪽 |
| **E8** 쿼터 두 칸 (`0x4d8`·`0x2998`) 의 종류 번호 ↔ 칸 대응, 어느 호출이 만들었나 | 부르는 쪽이 셋, 순서는 실행 때 | 멈춤 **Q97**: `nt!PspChargeQuota+0x95` 에서 `r8d`·`r9` |
| 뒤 구간 29칸 (`K2~K3` · `K3~P0` · `P0~P1` · `P1~P1a` · `P1a~P1b`), `OwnerProcessId` 바뀜, 옛 Q65 몫 | 이번 범위 밖 (시간 순서로 나눈 둘째 조각) | 정적 **Q78c2b** |
| 작은 정적 셋: `Flags.ProcessDelete` 켜는 줄 (`PAP +0xf93` → `+0x13f6`) 이 정말 이 칸인지 · `Job`·`JobLinks`·`CommitChargeJob` 을 실제로 쓰는 줄 · 전역 프로세스 목록 머리 `0x140e2db98` 의 PDB 이름 | 이름이 어색함 `[추측]` / callee 안을 안 읽음 / 이름을 안 찾음 | 정적 **Q78c2b** (plan 출처에 붙음) |
| "범위 밖 길" 52 · "아무도 안 씀" 28 의 쓰는 함수를 `.asm` 로 한 줄씩 | 지금 근거는 오프셋 숫자 훑기 (오탐 가능, 2.5) | 갈 곳은 오케스트레이터. `field_writers.py` 가 생기면 쉬워짐 |
| 칸 표 없는 짐 후보 (확장 영역 셋 · `_MMWSL_INSTANCE` · `_EPROCESS_QUOTA_BLOCK` · `_PSP_SESSION_SPACE`) | 체크리스트 21종 밖 | 짐 지도 7.6 에 **기록만** |
| `Vm`·`VadRoot` 안쪽 내용, 페이지 표 내용 | 범위 밖 | (정하지 않음) |

- 대기열 순서 (plan): **Q78c2b** → Q78d → Q78e → Q78f → Q78g → Q79 → Q80 → Q82. Q97 은 동적 차례에, 낮은 우선순위.

---

## 5. 더 읽기

- run 문서: [`run50-eprocess-create.md`](run50-eprocess-create.md) — 요약 표, 호출 지점 표, 호출 관계 트리, 할당 두 층, IFEO 키가 없는 길, 끝까지 0 160칸 가르기 (범위 밖 길 52 표), 안 타는 큰 갈래, 간접 호출 두 곳, 읽는 입력, 확인된 것, 정정, 아직 확인 필요 (E1 ~ E9), 도구 메모
- 결과서: [`_workspace/sast/44_eprocess-create_result.md`](../../_workspace/sast/44_eprocess-create_result.md) — 판단 기준, 1.1 칸마다 표 (3장 양식), 1.2 run49 메모 확인, 2.1 ~ 2.5 끝까지 0 가르기, 3.1 IFEO 값 → 칸 표, 3.2 간접 호출, 4 줄기와 경로, 동적 질문, Q78c2b 로 넘길 것, 정정 1 ~ 8
- 요청서: [`_workspace/sast/44_eprocess-create.md`](../../_workspace/sast/44_eprocess-create.md)
- 짐 지도: [`baggage.md`](baggage.md) — "쓰는 말" (범위 밖 길에서만 씀), 2장 (여덟째판 숫자), 3.11 `_EPROCESS`, 7.2 · 7.4 · 7.5 · 7.6 · 7.13
- 흐름도: [`flow.md`](flow.md) — 2.1 트리, `nt!PspAllocateProcess` 절 (`_EPROCESS` 칸 묶음, 간접 호출), `nt!MmCreateProcessAddressSpace` 절, `nt!PspCopyAndFixupParameters` 절, 2.4 프로세스 힙 절 (IFEO 줄), 4장 run50 줄, 4.1, 5장
- 진행표: `_workspace/plan.md` Q78c2 · Q78c2a (끝) · Q78c2b · Q79 · Q35 · Q97, 결정 기록 2026-10-30
- 앞 해설:
  - [run10](../02_sprint/run10-allocate-process-해설.md) — `PspAllocateProcess`, 가게 장부, 보통 길 / 복제 길
  - [run32](../02_sprint/run32-params-initblock-해설.md) — 개업 서류와 `PspCopyAndFixupParameters`
  - [run46](run46-process-heap-해설.md) 2.6 · 2.7 — ntdll 쪽 IFEO 건너뜀, 예약 / 확정
  - [run47](run47-section-chain-해설.md) 2.4 · 2.5 — 0 의 세 가지, `ObCreateObjectEx`
  - [run49](run49-kprocess-token-htable-해설.md) — 장부 앞부분 세 덩어리, 몸통 0 채움, 2.14 부모에서 오는 값

# run123 run58-60-leftovers — 해설

> 이 글은 [run123 문서](run123-run58-60-leftovers.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q112 + Q113 + Q115** 묶음 — run58 · run59 · run60 이 남긴 **작은 정적** 셋을 한 요청 · 한 바퀴로 (`_workspace/plan.md`). 이번에 **셋 다 끝**났다.
  - 사이에 있는 **Q114** 는 동적이라 이번 바퀴에 안 넣었다.
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다.
  - 남은 것이 간 곳 (4절): 동적 → 새 **Q215 ① ③ ④ ⑤** (모두 선택, ② 는 **Q114 (4)** 와 같아서 그쪽에서), 정적 → 새 **Q216 ① ~ ⑦**.
  - 이 바퀴 뒤로 분석은 **멈춤**이다 (사용자 결정 2026-11-16 "여기까지만, 나머지는 추후로"). 이 바퀴는 해설 · 커밋까지만 닫는다.
- 근거:
  - run 문서 [`run123-run58-60-leftovers.md`](run123-run58-60-leftovers.md) (가장 중요)
  - 요청서 [`_workspace/sast/119_run58-60-leftovers.md`](../../_workspace/sast/119_run58-60-leftovers.md) · 결과서 [`119_run58-60-leftovers_result.md`](../../_workspace/sast/119_run58-60-leftovers_result.md). 판정 **분석성공**, 스크립트 문제 없음, 동적 필수 없음.
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): **그림은 안 바뀌었다.** 이미 있는 상자 **안쪽** 글만 늘었다 (3절).
  - 짐 지도 [`baggage.md`](baggage.md): 이미 있는 3장 줄 8개를 고치고 새 7.53 을 붙였다. **2장 숫자는 그대로**다 (2.12).
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 이 글의 `[관찰]` 은 모두 **run44 사진에 이미 있던 값**이다 (업무 수첩 `+0xa0 ~ +0xb8` 두 줄). 새로 찍은 사진이 아니다.
  - 새 Ghidra 실행도 0번이다. 있던 `.asm` 과 스크래치 objdump 풀이만 읽었다.
- 결과서와 run 문서가 다른 곳이 둘 있다. 이 글은 **run 문서**를 따른다.
  - 멈춤점 주소: 결과서 `nt+0xa04380` → run 문서 **`nt+0xa04385`** (4절 Q215 ①).
  - "무결성 둘을 2장에 +2" → run 문서는 **안 더한다** (2.12).

---

### 이번 글의 말: 이미 푼 것은 링크만

이번 바퀴의 두 주인공 말, **완화 옵션**과 **`.mrdata`** 는 앞 해설에서 이미 풀었다. 한 줄씩만 다시 적고 링크를 건다.

- **완화 옵션 (mitigation options)** = 공격을 어렵게 하려고 프로세스마다 켜는 보안 설정 묶음 `[추측]` (일반적으로 알려진 설명). 비유는 가게의 **안전 규칙** → [run32 해설](../02_sprint/run32-params-initblock-해설.md) "새 말 — 완화 옵션" · [run50 해설 2.11](run50-eprocess-create-해설.md) ("새 말: 완화 옵션")
  - 커널은 이걸 **옵션 40개 × 4비트짜리 24바이트 표**로 들고 다닌다 → [run59 해설 2.12](run59-init-leftovers-해설.md) "나머지 C 묶음" ⓗ (이번 2.6 에서 다시 천천히)
- **`.mrdata`** = 평소엔 읽기만 되는, 가끔 잠깐 열어 고치는 ntdll 데이터 칸. 비유는 **유리 덮개 씌운 칠판** → [run43 해설 2.9](run43-baggage-checklist-해설.md)
  - 덮개를 여닫는 `LdrProtectMrdata` 와 옆의 **숫자판** (몇 명이 열어 달라고 했나) → [run60 해설 2.6](run60-func-table-mrdata-해설.md) ("`.mrdata` 열고 닫기 — 덮개 옆 숫자판") · [run63 해설 2.11](run63-cfg-func-table-해설.md)

그 밖에 이번에 다시 나오는 말:

- **IFEO** = **가게 이름별 특별 지시** (exe 이름별 실행 옵션 레지스트리 자리 `[추측]`) → [run10 해설](../02_sprint/run10-allocate-process-해설.md) 3.4
- **만들기 정보** = 이번 개업 건의 **작업 지시서** (`NtCreateUserProcess` 가 만들어 `PspAllocateProcess` 에 넘기는 구조), 프로세스 매개변수 블록 = **개업 서류** → [run104 해설](run104-params-block-해설.md) · [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- `LdrSystemDllInitBlock` (시작 정보 블록) = 관리실 **업무 수첩** → [run32 해설](../02_sprint/run32-params-initblock-해설.md)
- **속성 목록** = 신청서 뒤에 붙이는 **추가 요청 쪽지 묶음** (머리 8바이트 + `0x20` 바이트 항목들) → [run74 해설](run74-thread-create-해설.md) 2.7 "새 말 — 속성 목록"
- **기능 스위치** = 빌드 안에서 새 기능을 켜고 끄는 전역 값 `[추측]` → [run32 해설 2.6](../02_sprint/run32-params-initblock-해설.md) · [run103 해설](run103-image-share-해설.md) ("한 비트짜리 기능 스위치")
- **메시지 표** = DLL 안 "결과 코드 번호 → 사람이 읽는 글" 표 (이름은 없음) → [run106 해설 2.9](run106-snap-map-small-해설.md)
- **copy-on-write** = 같이 보던 쪽을 누가 쓰려는 순간에만 그 사람 몫 사본을 만들어 주는 방식 → [run103 해설](run103-image-share-해설.md) ("새 말 — copy-on-write")
- **CFG 내보내기 억제** → [run60 해설 2.10](run60-func-table-mrdata-해설.md)
- **silo** = 컨테이너처럼 따로 떼어 둔 이름 공간 `[추측]`, 상가 안의 **구역** → [run104 해설](run104-params-block-해설.md)
- 확장 import 슬롯의 기본값 = **"아무 말 없이 끊는 번호"** (`ret 0` 스텁, rva `0x4a10c0`) → [run59 해설 2.12](run59-init-leftovers-해설.md) "나머지 C 묶음" ⓖ 아래
- run44 사진 K1 · P1a … → [run44 해설 2.8](run44-baggage-snap-해설.md#28-8-지점--어디서-멈췄고-정말-그-순간인가)

이번에 **처음 나오는 말** 은 넷이다. 본문에서 처음 나올 때 한 줄씩 푼다.

- **값 → 이름 표** → 2.3
- **코어 격리 정책** → 2.7
- **매개변수 덮어쓰기 구조** → 2.11
- **쓰기만 있는 칸** → 2.14

### 비유: 그대로 이어 쓴다

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**, 프로세스 = **가게**, 시스템 콜 = 관리사무소 **신청 창구**.
- 완화 옵션 = 가게 **안전 규칙**. 24바이트 표는 run 문서 말로 **안전 규칙 카드**다. 칸마다 "켬 · 끔 · 모름" 을 적는다.
- 만들기 정보 = **작업 지시서**, 매개변수 블록 = **개업 서류** (run 문서는 "서류철"), 업무 수첩 = `LdrSystemDllInitBlock`.
- ntdll 전역 = 관리실 **칠판**, `.mrdata` = **유리 덮개 칠판**, `LdrProtectMrdata` 의 카운터 = 덮개 옆 **숫자판**.
- IFEO = **가게 이름별 특별 지시**.
- **이번에 더하는 그림**:
  - 안전 규칙 카드 셋째 줄의 "끔" 두 개는 손님이 낸 서류에서 온 게 아니다. **사무소가 마지막에 도장으로 직접 찍는다** — 하나는 늘, 하나는 이 건물에 그 설비가 없을 때 (run 문서 그림 그대로).
  - 카드의 출발점은 "부모 가게 카드" 가 아니라 **관리사무소 기본 규칙** (부팅 때 정함) 이다.
  - 개업 서류 맨 끝 칸 `+0x448` = **"손님이 따로 맡긴 부탁 목록" 자리**. 사무소가 목록을 서류 바로 뒤에 붙이고 그 자리를 이 칸에 적는다. test.exe 가게엔 목록이 없다.

### 한 줄로 먼저: 무엇을 얻었나

| 대기열 | 질문 | 답 (run 문서 그대로) | 태그 |
|---|---|---|---|
| Q112 (1) | `0xc000090b` 이름 | **`STATUS_BAD_DATA`**. PDB 엔 없고, 같은 빌드 바이너리 셋의 "값 → 이름" 표 + ntdll 메시지 글 "Bad data." | `[정적]` |
| Q112 (2) | 커널 사본 `RtlpQueryNlsSystemCodePages` 의 글 | ntdll 것과 **같다**. 다른 건 `Zw*` · `RtlInitUnicodeString` 호출 꼴뿐 | `[정적]` |
| Q112 (3) | 같은 `.rdata` 글을 쓰는 다른 ntdll 함수 | **없다** — 두 함수 안 8줄이 전부 | `[정적]` |
| Q113 (a) | 옵션 셋째 칸 "2" 는 어디서 · 32 ~ 39 이름 | 비트 12~13 = `PspAllocateProcess +0x1e03 ~ +0x1e15` **무조건**. 비트 24~25 = `+0x1e1d ~ +0x1e30` (코어 격리 정책이 안 될 때). 이름은 PDB `PS_MITIGATION_OPTION` | `[정적]` |
| Q113 (b) | 만들기 정보 `+0x1d8` 을 읽는 함수 | **둘** — `PspCaptureProcessParameters +0x26` · `PspDeleteCreateProcessContext +0x11a` | `[정적]` |
| Q113 (c) | 결과서 밖 전역 셋을 2장에 셀까 | 근거만 옮김. **셈은 안 바꿈** (run114 가 이미 셌다) | `[정적]` |
| Q115 (a) | `PspCopyUserAttributes` → `+0x448` | **닫힘** — 사슬 ① ~ ⑥, 끝은 `PspCopyAndFixupParameters +0x3f8 ~ +0x402` | `[정적]` |
| Q115 (b) | `LdrpSuppressedExportOverrideListPtr` 를 읽는 곳 | **ntdll 안에 없다** | `[정적]` |
| Q115 (c) | 덤 다섯 | 속성 `0x22` = `PsAttributeHeapFeatures` · `LdrpMakePermanentImageCommit` = 쪽마다 같은 값 되쓰기 · `+0xdc4` `ECX` = **1** · `LdrEnsureMrdataHeapExists` 는 CFG 켜짐 + 힙 없음일 때만 · `.mrdata` 여닫는 자리 50 / 함수 21 | `[정적]` (이유는 `[추측]`) |

이 글에서 붙잡아 둘 세 줄:

> **(1) 업무 수첩 `+0xb0` 의 `0x02002002` 는 test.exe 와 상관없이 관리사무소가 찍는 값이다.** 바뀔 수 있는 건 비트 24~25 (이 컴퓨터) 와 비트 0~1 (이 가게의 CET) 둘뿐.
>
> **(2) "부모에게서 물려받은 안전 규칙" 은 없었다.** 합치는 재료는 관리사무소 기본 규칙 · IFEO · 손님이 준 속성이다.
>
> **(3) 이번 바퀴는 "모른다" 칸을 지우는 바퀴였다.** 그림도, 짐 지도 2장 숫자도 그대로다. 대신 앞 해설 여섯에 고쳐 읽을 줄이 생겼다 (2.16).

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 바퀴는 질문이 셋이라 자리도 여러 곳이다. 한 장에 표시하면 이렇다.

```
cmd.exe 의 부탁 → 커널 NtCreateUserProcess (부모 cmd 스레드 안)
 ├─ (1) PspBuildCreateProcessContext        작업 지시서 채우기 (+0x448 사슬 ①)      ← Q115 (a)
 ├─ +0xab8  PspGetProcessParameterOverrides  지시서 +0x1d8 쓰기 (run59)
 ├─ +0xacb  PspCaptureProcessParameters      지시서 +0x1d8 읽기                       ← Q113 (b)
 ├─ +0xc36  PspAllocateProcess               안전 규칙 카드 만들기                     ← Q113 (a)
 │        └─ … PspSetupUserProcessAddressSpace
 │              ├─ PspCopyAndFixupParameters    개업 서류 +0x448                    ← Q115 (a)
 │              └─ PspPrepareSystemDllInitBlock 업무 수첩 +0xa0/+0xb0 (run59)
 └─ +0x11d5 PspDeleteCreateProcessContext    지시서 +0x1d8 읽기 (치우기)              ← Q113 (b)
 → NtResumeThread
test.exe 첫 스레드 (유저)
 ntdll!LdrInitializeThunk → LdrpInitializeProcess
   ├─ 묶음 3 뒤: CFG 억제 확인 (+0x48d) · .mrdata 여닫기 (+0xda1 ~ +0xdc4 등)        ← Q115 (b) (c)
   └─ 레지스트리 두 함수 (RtlpQueryNlsSystemCodePages · RtlpGetNtProductTypeFromRegistry) ← Q112
 → … NtContinue 은퇴 → (CRT) → main
```

- `+0x..` 는 ret 자리다. `(1)` 은 flow.md 그림 1 의 번호 (코드 순서) 다.
- flow.md 로는 **2.1** (커널, 부모 cmd 스레드 안) 과 **2.4** (ntdll 로더 시작) 다. 2.6 은 결과 코드 이름 한 줄만.
- 모두 이미 그림에 있는 상자의 **안쪽**이다. 새 단계 · 새 caller 가 없어 **그림은 안 바꿨다** (run 문서 "지도 범위").

### 왜 이 질문이 지금 왔나

- run58 · run59 · run60 이 각각 "정적으로 작게 남은 것" 을 넘겼다. 대기열에서 Q112 · Q113 · Q115 가 됐다.
- 셋 다 작고, 같은 함수들 (`PspAllocateProcess`, `LdrpInitializeProcess` 근처) 을 다시 보면 되는 일이라 **한 요청으로 묶었다**.
- 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례였다.

### 무엇이 궁금했나

요청서 질문 아홉을 셋으로 묶으면:

- **Q112 (run58 몫)**: 상태값 `0xc000090b` 의 이름은? 커널에 있는 같은 이름 함수의 레지스트리 글은 ntdll 것과 같나? 그 글을 쓰는 다른 ntdll 함수가 있나?
- **Q113 (run59 몫)**: 업무 수첩 `+0xb0` (사진 `0x02002002`) 의 "2" 들은 **처음 어디서** 오나? 옵션 번호 32 ~ 39 의 이름은? 작업 지시서 `+0x1d8` 을 **읽는** 함수는? 결과서 밖 전역 셋을 2장에 셀까?
- **Q115 (run60 몫)**: 손님 부탁 목록이 개업 서류 `+0x448` 까지 가는 줄은? `LdrpSuppressedExportOverrideListPtr` 를 읽는 곳은? 그리고 싸게 되는 덤 다섯.

### 바퀴 전에 알던 것

- `[정적]` (run58) `RtlpGetNtProductTypeFromRegistry +0x1b0 MOV EBX,0xc000090b` — 제품 종류 값이 이상하면 이 값을 돌려준다. 이름은 확인 안 했다.
- `[관찰]` (run44) test.exe 는 KUSD `ProductTypeIsValid` = 1 이라 그 레지스트리 길을 **안 탄다**.
- `[정적]` (run59) 옵션 표는 옵션 40개 × 4비트. 셋째 8바이트 = 옵션 32 ~ 39. 비트 0~1 의 2 는 `PspApplyMitigationOptions` 가 쓴다.
  - 비트 12~13 · 24~25 의 2 는 "**입력 표가 갖고 온 값**, 출처 `[공백]`" 이라고 적었다. 그리고 "옵션 번호 ↔ 이름은 PDB 에 열거형이 없다" 고 적었다. (둘 다 이번에 틀린 것으로 판명 — 2.7 · 2.6.)
- `[정적]` (run50 · run59) `PspInheritMitigationOptions` 는 "시스템 기본 · **부모**와 합치기" 로 불렸다. (이번에 "부모" 가 틀린 것으로 판명 — 2.8.)
- `[관찰]` (run44) 사진 8장 업무 수첩 `+0xa0 = 0` · `+0xa8 = 0` · `+0xb0 = 0x02002002` · `+0xb8 = 0`.
- `[정적]` (run59) 작업 지시서 `+0x1d8` 을 **쓰는** 곳은 `PspGetProcessParameterOverrides +0x4a` 하나. 읽는 쪽은 `[공백]`.
- `[정적]` (run104) `+0x3f8 ~ +0x402` 가 개업 서류 `+0x448` 을 새 목록 주소로 덮는다. 그 앞 사슬은 안 봤다.
  - run104 는 커널 사본의 **셋째 인자 구조**를 "속성 `0x20` 대 (`PsAttributeHeapFeatures` 류)" 로 짐작했다 `[추측]`. (이번에 틀린 것으로 판명 — 2.11.)
- `[정적]` (run60) `+0xdc4` 의 `ECX` 가 1 (닫기) 이라는 건 `RBX` 를 안 따라가서 `[추측]`. 속성 `0x22` 의 공개 이름은 "PDB 에 없음" 이라 적었다.
- `[정적]` (run106) `0x40000036` 이름 `STATUS_IMAGE_AT_DIFFERENT_BASE` 는 `[추측: 기억]`.

---

## 2. 찾은 것

### 2.1 한눈에

run 문서 "호출 관계" 를 줄였다. Q113 (커널, 안전 규칙 카드) 과 Q115 (커널, `+0x448`) 두 그림이다.

```
(Q113 — 커널, 새 프로세스 만들 때)
NtCreateUserProcess
 ├─ +0xab8  PspGetProcessParameterOverrides      +0x4a  지시서 +0x1d8 ← 확장 import (스텁이면 0)
 ├─ +0xacb  PspCaptureProcessParameters          +0x26  +0x1d8 읽기 → R8
 ├─ +0xc36  PspAllocateProcess
 │        ├─ +0x1a90  PspReadIFEOMitigationOptions
 │        ├─ +0x1aef  PspInheritMitigationOptions   A = PspSystemMitigationOptions · B = IFEO
 │        ├─ +0x1be5  PspInheritMitigationOptions   (사용자 속성 있을 때)
 │        ├─ +0x1c87  PspHardenMitigationOptions
 │        ├─ +0x1c8c  KeIsUserCetAllowed            거짓 → 둘째 칸 비트 60~61 := 2 (test.exe 안 탐)
 │        │  +0x1e03 ~ +0x1e15  셋째 칸 비트 12~13 := 2   (무조건)
 │        ├─ +0x1e22  KeIsCoreIsolationMitigationPolicyEnforceable
 │        │  +0x1e26 ~ +0x1e30  거짓 → 비트 24~25 := 2   (이 VM)
 │        ├─ +0x1eb8  PspApplyMitigationOptions     +0x510 ~ +0x523 비트 0~1 := 2 · +0x5ef/+0x5f4 되씀
 │        │  +0x1f37 ~ +0x1f50  지시서 +0x150/+0x160 ← 표
 │        └─ … PspPrepareSystemDllInitBlock +0x12b/+0x138 → 수첩 +0xa0/+0xb0 (run59)
 └─ +0x11d5 PspDeleteCreateProcessContext        +0x11a  +0x1d8 읽기 (0 이면 끝)

(Q115 — 커널, +0x448 이음. 스위치 켜짐 갈래만 — test.exe 는 꺼짐/개수 0)
PspBuildCreateProcessContext
 └─ +0x15a4  PspProcessUserCopyAttribute         지시서 +0x230/+0x250/+0x254/+0x258
PspSetupUserProcessAddressSpace
 ├─ +0x135   PspCalculateUserAttributeListSize   덩어리 크기에 목록 자리
 └─ +0x1b9   PspCopyAndFixupParameters
          │  +0x3c7  개수 > 0 일 때만
          ├─ +0x3f0  PspCopyUserAttributes        블록 + 환경 뒤 (8 올림) 에 PS_ATTRIBUTE_LIST
          └─ +0x407  RtlWriteULong64ToUser        ProcessParameters +0x448 = 목록 주소
```

- 모두 `[정적]` 이다. 디버거로 이 줄들에 서 본 적은 없다.
- 호출 기호 (`├─`, `└─`) 옆 `+0x..` 는 ret 자리, 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄이다.
- ntdll 쪽 (Q112 · Q115 (b) (c)) 은 짧아서 해당 절 (2.3 ~ 2.5, 2.14 · 2.15) 에서 따로 본다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ①: `│  +0x1e03 ~ +0x1e15  셋째 칸 비트 12~13 := 2   (무조건)`

- **누가**: `PspAllocateProcess` — 새 가게의 장부와 규칙을 꾸리는 관리사무소 담당자.
- **어디서**: 자기 안 `+0x1e03 ~ +0x1e15` 줄 넷. 누구를 부르는 줄이 아니라 **직접 고치는** 줄이다 (그래서 호출 기호가 없다).
- **누구를**: 아무도 안 부른다. 자기 지역 변수 `[RSP+0x410]` (= 안전 규칙 카드의 셋째 8바이트) 를 꺼내 비트 12 를 끄고 (`BTR`) 비트 13 을 켜고 (`BTS`) 다시 넣는다.
- **무엇을 위해**: 옵션 35 (`POINTER_AUTH_USER_IP`) 칸에 **2** 를 적으려고. 비트 12~13 이 `10` (2진) = 2 가 된다.
- "(무조건)" 인 이유는 2.7 의 6번에서 단계별로 본다. 태그 `[정적]`.

#### 한 줄 읽기 ②: `└─ +0x407  RtlWriteULong64ToUser        ProcessParameters +0x448 = 목록 주소`

- **누가**: `PspCopyAndFixupParameters` — 개업 서류를 새 가게 땅으로 옮겨 적는 사람 (run32).
- **어디서**: `+0x402` 에서 부르고 `+0x407` 로 돌아온다. 바로 앞 `+0x3f8 LEA RCX,[RDI+0x448]` 이 "어디에" 를, `+0x3ff MOV RDX,RBX` 가 "무엇을" 을 담는다.
- **누구를**: `RtlWriteULong64ToUser` — 유저 쪽 메모리에 8바이트 하나를 쓰는 공용 부품.
- **무엇을 위해**: 새 가게 개업 서류 `+0x448` (PDB 이름 `AttributeList`) 칸에 **손님 부탁 목록이 놓인 주소**를 적으려고. 목록은 바로 앞 `+0x3eb` `PspCopyUserAttributes` 가 서류 + 환경 바로 뒤에 붙여 둔 것이다.
- 단 이 줄은 **스위치 켜짐 · 개수 > 0** 일 때만 지난다. test.exe 는 안 지난다 (2.13). 태그 `[정적]`.

#### 한 줄 읽기 ③: `└─ +0x11d5 PspDeleteCreateProcessContext        +0x11a  +0x1d8 읽기 (0 이면 끝)`

- **누가**: `NtCreateUserProcess` — 개업 접수 창구 전체.
- **어디서**: `+0x11d0` 에서 부르고 `+0x11d5` 로 돌아온다.
- **누구를**: `PspDeleteCreateProcessContext` — 작업 지시서를 치우는 함수. 그 안 `+0x11a` 가 지시서 `+0x1d8` 을 읽어 `+0x121` `PspDestroyProcessParameterOverrides` 에 넘긴다.
- **무엇을 위해**: `+0x1d8` 에 붙어 있던 "수정 쪽지" (2.11) 를 버리려고. 0 이면 아무것도 안 한다 (`+0x4 TEST RCX,RCX` · `+0x7 JNZ`). 태그 `[정적]`.

---

### Q112 — run58 이 남긴 것

### 2.3 `0xc000090b` = `STATUS_BAD_DATA` — 이름은 "남의 바이너리" 안 표에서 찾았다

#### 무엇을 찾았나 `[정적]`

- `RtlpGetNtProductTypeFromRegistry +0x1b0 MOV EBX,0xc000090b` 의 그 값은 **`STATUS_BAD_DATA`** 다.
- 언제 나오나 (run58 7번 그대로): 제품 종류 값이 `REG_SZ` 가 아니거나, 너무 짧거나, `WinNt` · `LanmanNt` · `ServerNt` 셋 다 아닐 때 → `+0x1b0` → `+0x1b5 JMP`.
- test.exe 는 KUSD `ProductTypeIsValid` = 1 이라 **이 길을 안 탄다** (run58 · run44 `[관찰]`).

#### 어떻게 찾았나 — 단계별로

1. **PDB 를 먼저 봤다** → 없다. 이 빌드의 ntdll · ntoskrnl PDB 엔 NTSTATUS 이름이 아예 없다 (sast/100 과 같음).
2. **ntdll 메시지 표를 봤다** → 글은 있다. `0xc000090b` = "Bad data.", 이웃 `0xc000090a` = "Bad key.", `0xc000090c` = "Key does not exist.". 그런데 메시지 표엔 **이름이 없다** (run106 에서 본 그대로).
3. **같은 빌드의 다른 바이너리 안 "값 → 이름" 표를 봤다** → 이름이 있다.

**새 말 — 값 → 이름 표**: 어떤 프로그램이 오류 코드를 사람에게 보여 주려고 자기 안에 넣어 둔 **"번호 ↔ `STATUS_…` 이름" 대응표**. 여기서는 칸 하나가 16바이트 (값 + 이름 글 주소) 다.

- 비유: 관리사무소 안내 책자 (메시지 표) 엔 "36번 = 서류가 이상해요" 같은 **설명 글**만 있고 공식 명칭이 없다. 그런데 이웃 가게 몇 곳이 벽에 붙여 둔 **"번호 → 공식 명칭" 대조표**에는 명칭이 있었다.
- 찾은 곳 셋 (같은 빌드 System32): `nltest.exe` (rva `0x68ce0`) · `certca.dll` (`0x557c0`) · `tdhres.dll` (`0x72ce0`).
- 셋이 **서로 독립**인데 값 · 이름이 다 같다. 이웃 `STATUS_BAD_KEY` · `STATUS_NO_KEY` 도 같다. 그리고 메시지 글 ("Bad data.") 과 뜻도 맞는다 → 그래서 확정.
- 표를 바르게 읽었는지 검산: 같은 표에서 `0xc0000017` = `STATUS_NO_MEMORY` 가 맞게 나왔다.
- 이 바이너리들은 Ghidra 를 안 거치고 **바이트만** 읽었다 (결과서).

#### 덤 — `0x40000036` 이름도 같이 닫혔다 `[정적]`

- 같은 `nltest.exe` 표에: **`0x40000036` = `STATUS_IMAGE_AT_DIFFERENT_BASE`** (rva `0x64770`), `0x40000003` = `STATUS_IMAGE_NOT_AT_BASE` (`0x64440`).
- run106 이 `[추측: 기억]` 으로 남긴 이름이 이제 `[정적]` 이다. 뜻 ("다른 주소에 붙었지만 fixup 은 자동") 과 ntdll 이 재배치를 안 하는 갈래는 run106 그대로다 → [run106 해설 2.9](run106-snap-map-small-해설.md).
- 쓰는 곳 전수 (`mov reg,0xc000090b`): ntdll 6곳 · ntoskrnl 9곳 (이름은 결과서).

### 2.4 커널 사본 `RtlpQueryNlsSystemCodePages` — 글이 ntdll 것과 똑같다

`[정적]` 커널에도 같은 이름 함수가 있다 (rva `0x78af7c`, 452B, 부팅 때 `ExInitializeNls +0x79` 가 부름).

| 쓰임 | 커널 줄 | 글 | ntdll (run58) |
|---|---|---|---|
| 키 | `+0x48 LEA RDX` → `+0x5f` | `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` | 같음 |
| 값 1 | `+0xa3 LEA RDX` → `+0xb9` | `ACP` | 같음 |
| 값 2 | `+0x11c LEA RDX` → `+0x127` | `OEMCP` | 같음 |

- **모양도 같다**: 시작 때 두 칸에 `0xfde9`, 속성 `0x240`, 접근 `0x80000000`, 클래스 2 · 버퍼 `0x24`, 10진 변환.
- **갈래도 같다**: `ACP` 형식이 틀리면 둘째 읽기로 (`+0xf2 JNZ +0x11c`), `OEMCP` 형식이 틀리면 닫기로 (`+0x15e JNZ +0x185`), 실패면 두 칸 `0xfde9`. **늘 0 을 돌려준다** (`+0x19e XOR EAX,EAX`).
- **다른 점은 딱 하나**: 창구 이름. ntdll 은 `Nt*` 를, 커널 사본은 `ZwOpenKey` · `ZwQueryValueKey` · `ZwClose` 를 부른다. 그리고 글 길이를 `RtlInitUnicodeString` 으로 잰다 (`+0x5f` · `+0xb9` · `+0x127`).
- 비유: 관리사무소와 관리실이 **같은 양식의 조회 쪽지**를 따로 한 장씩 갖고 있다. 쪽지 글자도 같고, 실패하면 하는 일도 같다. 관리사무소는 안쪽 문 (`Zw*`) 으로, 관리실은 창구 (`Nt*`) 로 갈 뿐이다.

#### 곁 사실 — 묻지 않았지만 결과서가 같이 본 것

- `[정적]` 커널에 `RtlpGetNtProductTypeFromRegistry` 사본도 있다 (rva `0xaa5c98`, 글 5개가 ntdll 것과 같음, `+0x173` 이 `0xc000090b`).
- 부르는 곳은 `RtlGetNtProductType +0x51` · `PspSiloInitializeUserSharedData +0x12a`.
- 둘째는 **서버 silo 쪽 KUSD 사본** 길이다 `[정적: 부르는 곳까지만]`. 짐 지도 3.7 의 진짜 KUSD `NtProductType` 줄과는 다른 길이고, test.exe 는 silo 가 아니다 `[추측]`.
  - (결과서는 이 둘째를 "KUSD `NtProductType` 를 채우는 쪽" 이라 적었는데, run 문서는 "silo 쪽 사본 길, 진짜 KUSD 줄과는 다른 길" 로 좁혀 적었다. 이 글은 run 문서를 따른다.)

### 2.5 같은 `.rdata` 글을 쓰는 다른 ntdll 함수 — 없다

`[정적]` 결과서가 세 가지로 서로 덮어 훑었다.

1. **코드**: ntdll 전체 풀이에서 "RIP 상대 목적지가 이 글 범위 안" 인 줄 = **8줄**. 모두 두 함수 안이다.
   - `RtlpQueryNlsSystemCodePages` `+0x3c` · `+0xc4` · `+0x171`
   - `RtlpGetNtProductTypeFromRegistry` `+0x2d` · `+0x5d` · `+0x74` · `+0x7f` · `+0x8a`
2. **데이터**: 이 글을 가리키는 8 · 4바이트 값 = **0**. 함수 포인터 표나 다른 칸이 몰래 가리키는 일도 없다.
3. **같은 글자 복사본**: `ProductType` · `WinNt` · `LanmanNt` · `ServerNt` · `OEMCP` · `ProductOptions` 는 각 1곳 (원본). `ACP` 는 9곳인데 나머지 8곳은 **다른 글 속 부분 문자열**이다.

- 그래서 run58 이 "가진 `.asm` 중엔 두 함수뿐" 이라 남긴 빈칸이 **ntdll 전체로** 닫혔다.
- 왜 세 가지를 다 보나: 코드 훑기 (1) 만으로는 "어느 표가 이 글 주소를 들고 있다가 나중에 꺼내 쓰는" 꼴을 놓친다. 그래서 (2) 를 같이 본다. (3) 은 링커가 같은 글을 다른 자리에 또 둘 수 있어서다.

---

### Q113 — run59 가 남긴 것

### 2.6 안전 규칙 카드 다시 읽기 — 옵션 번호와 이름

run59 에서 본 카드를 다시 천천히 본다.

#### 카드 읽는 법 `[정적]`

- 카드 = 24바이트 = 8바이트 칸 셋. 옵션 하나당 **4비트**.
- 옵션 번호 n 은 **`n / 16` 번째 8바이트**의 **비트 `(n % 16) × 4`** 에서 시작한다 (0번째부터 셈).
  - 4비트 중 비트 0~1 = **상태** (0 · 1 · 2 · 3), 비트 2 = **고정**.
- 그래서 셋째 8바이트 (`n / 16 = 2`) = 옵션 **32 ~ 39**. 업무 수첩으로는 `+0xb0` 칸이다.
- 비유: 안전 규칙 카드 셋째 줄에 여덟 칸이 있다. 칸마다 "모름(0) · 켬(1) · 끔(2)" 같은 상태 동그라미와, "바꾸지 마시오" 고정 표시가 하나 있다.

#### 셋째 줄 여덟 칸의 이름 `[정적]` (PDB `PS_MITIGATION_OPTION`, ntdll · ntoskrnl 같음)

| n | PDB 이름 | 셋째 칸 비트 | `== 1` 일 때 하는 일 (결과서) |
|---|---|---|---|
| 32 | `USER_CET_SET_CONTEXT_IP_VALIDATION` | 0~1 | (감사표) `MitigationFlags2 \|= 0x40000` |
| 33 | `BLOCK_NON_CET_BINARIES` | 4~5 | `0x200000` (3 이면 `+0x400000`) |
| 34 | `RESERVED_WAS_XFG` | 8~9 | 읽는 줄 없음 |
| 35 | `POINTER_AUTH_USER_IP` | **12~13** | `MitigationFlags2 \|= 0x8000000` (0 이면 1 로 고쳐 같은 효과) |
| 36 | `CET_DYNAMIC_APIS_OUT_OF_PROC_ONLY` | 16~17 | `MitigationFlags2 \|= 0x40000000` (`+0x5a5`) — 비트 이름은 `[추측: 비트 순서]` |
| 37 | `REDIRECTION_TRUST` | 20~21 | `PspSetRedirectionTrustPolicy(…, 1)` |
| 38 | `RESTRICT_CORE_SHARING` | **24~25** | `MitigationFlags3 \|= 1` |
| 39 | `FSCTL_SYSTEM_CALL_DISABLE` | 28~29 | `MitigationFlags3 \|= 6` (`[추측: 비트 순서]`) |

- 핵심 하나: **`2` 를 "효과 있음" 으로 보는 줄은 어디에도 없다.** 코드는 `== 1` (켬) 만 본다 (옵션 33 의 3, 옵션 35 의 0 은 따로).
  - "2 = 일부러 끔" 이란 공개 이름 (`…_ALWAYS_OFF`) 은 기억에서 나온 것 `[추측: 기억]`.
- run59 는 "PDB 에 옵션 열거형이 없다" 고 적었다. **틀렸다** — `PS_MITIGATION_OPTION` 이 있다 (run107 이 일부를 먼저 썼다). 2.16 정정.
- 참고로 앞 두 칸의 몇 이름: 3 `HEAP_TERMINATE` · 10 `CONTROL_FLOW_GUARD` · 15 `IMAGE_LOAD_PREFER_SYSTEM32` · 17 `LOADER_INTEGRITY_CONTINUITY` · 19 `RESTRICT_SET_THREAD_CONTEXT` · 31 `CET_USER_SHADOW_STACKS`.

### 2.7 "2" 는 어디서 오나 — 카드가 관리사무소를 지나는 길

이번 바퀴의 한가운데다. `PspAllocateProcess` 안에서 카드는 지역 변수 `[RSP+0x400]` (24B) 에 있다. 셋째 8바이트는 `[RSP+0x410]` 이다. 차례대로 따라간다. 모두 `[정적]`.

1. **`+0x1a8b` IFEO 읽기** → 가게 이름별 특별 지시에서 카드를 읽는다. 없으면 0.
2. **관리사무소 기본 규칙과 합치기** → `+0x1ab2 ~ +0x1ac9` 가 시스템 전역 `PspSystemMitigationOptions` (rva `0xfc7140`, 24B) 를 베끼고, `+0x1aea` 에서 `PspInheritMitigationOptions(A = 시스템, B = IFEO)`. (이 합치기 규칙은 2.8 에서.)
3. **손님이 옵션을 직접 냈으면 한 번 더 합치기** → 작업 지시서 `+0x8` 비트 16 이 켜져 있으면 `+0x1be0` 에서 (A = 앞 결과, B = 지시서 `+0x150` · `+0x160`).
   - 그 B 는 `PspBuildCreateProcessContext +0xad8 ~ +0xb7b` 가 속성 `PsAttributeMitigationOptions` (16번) 24B 를 `PspValidateMitigationOptions` 로 검사해 넣은 것.
   - 손님 쪽 입구는 kernelbase `CreateProcessInternalW +0x22a3` 의 "이미지에 박힌 정책". test.exe 는 없어서 입력 0 `[추측: 매니페스트 · 로드 구성 안 봄]`.
4. **`+0x1c82` 굳히기** (`PspHardenMitigationOptions`) → 셋째 칸은 **안 건드린다** (`+0x132 ~ +0x136`).
5. **`+0x1c87` `KeIsUserCetAllowed`** → 거짓이면 `+0x1ca4 ~ +0x1cb2` 가 **둘째** 칸 비트 60~61 (옵션 31) 을 2 로. 사진 `+0xa8 = 0` 이라 이 길은 안 탔다 `[정적 + 관찰]`.
6. **`+0x1e03 ~ +0x1e15` — 비트 12~13 := 2. 무조건.**
   - 왜 "무조건" 인가 — 근거 → 그래서:
     - 근거: 이 구간으로 들어오는 길은 `+0x1deb JZ` 와 **바로 위에서 아래로 떨어지는 길**, 이 둘뿐이다 (flow-writer 가 `.asm` 으로 확인). 이 구간을 건너뛰는 갈래가 없다.
     - 그래서: 앞에서 IFEO · 기본 규칙 · 손님 옵션이 무엇을 적었든 **여기서 덮어쓴다**.
   - 왜 늘 끄는지는 코드에 이유가 없다. 결과서는 "x64 에는 포인터 인증이 없음" 이라고 이유를 달았다 `[추측]` (코드에 적힌 게 아니라 결과서의 해석).
7. **`+0x1e1d ~ +0x1e30` — 비트 24~25 := 2. 코어 격리 정책이 안 될 때만.**
   - `+0x1e1d` 에서 `KeIsCoreIsolationMitigationPolicyEnforceable` 를 부르고, 거짓이면 `+0x1e26 ~ +0x1e30` 이 `BTR 0x18` · `BTS 0x19`.

   **새 말 — 코어 격리 정책**: 옵션 38 (`RESTRICT_CORE_SHARING`) 처럼 "CPU 코어를 다른 쪽과 나눠 쓰지 않게" 하는 규칙을 **이 컴퓨터가 지킬 수 있나** 를 묻는 질문 `[추측]` (이름으로 본 일반적인 설명. 코드가 보는 건 아래 전역 하나뿐이다). 비유: "이 건물에 **방화 칸막이 설비**가 있나?" 없으면 그 규칙 칸에 "끔" 도장을 찍는다.

   - 그 함수는 다른 함수를 안 부르는 11바이트짜리 작은 함수다. 전역 `KiCoreIsolationEnforceable` (rva `0xfc6ed0`) ≠ 0 인지만 돌려준다.
   - 그 전역은 `KeOptimizeSpecCtrlSettings +0x48` 이 하이퍼바이저 연결 · `HvlpFlags` · `HvlpSchedulerType` 를 보고 정한다 `[정적: 갈래만]`.
   - 사진이 2 였으니 → **이 VM 에선 그 전역이 0 이었다**는 뜻이다 (정적 길 + 사진으로 미루어 본 것. 전역 값을 직접 읽은 적은 없다 → 4절 Q215 ①).
8. **`+0x1eb3` `PspApplyMitigationOptions`** (카드대로 가게 장부에 실제로 켜는 사람):
   - 위 두 "2" 는 `== 1` 이 아니라서 **효과 없이 그대로** 지나간다.
   - **비트 0~1 의 2** 는 여기서 적는다: `+0x500` 에서 가게 `CetUserShadowStacks` 가 꺼져 있으면 `+0x510 ~ +0x523`.
   - 끝에 `+0x5ef` · `+0x5f4` 가 카드를 제자리에 되쓴다.
9. **`+0x1f37 ~ +0x1f50`** 카드를 작업 지시서 `+0x150` · `+0x160` 에 되돌림 → `PspPrepareSystemDllInitBlock +0x64 ~ +0x78` 이 읽어서 → `+0x12b` · `+0x138` 이 **업무 수첩 `+0xa0` · `+0xb0`** 에 쓴다 (run59).

- 비유로 한 번 더: 관리사무소가 기본 규칙 카드를 꺼내 → 가게 이름별 특별 지시를 겹쳐 보고 → (손님 요청이 있으면 또 겹치고) → 마지막에 **셋째 줄 두 칸에 "끔" 도장**을 직접 찍는다. 하나는 늘, 하나는 이 건물에 방화 칸막이가 없을 때. 그 카드를 가게 장부에 반영하고 (비트 0~1 도 여기서), 작업 지시서를 거쳐 관리실 업무 수첩에 베껴 둔다.
- run59 는 "12~13 · 24~25 의 2 는 **입력 표가 갖고 온 값**" 이라 적었다. 그때는 `PspApplyMitigationOptions` 만 보고 "여기서 안 쓰니 들어올 때부터 있었겠지" 로 본 것이다. 실제로는 **그 앞, 부른 쪽 `PspAllocateProcess`** 가 쓴다. 2.16 정정.

### 2.8 "부모" 가 아니라 "관리사무소 기본 규칙" — 합치기 규칙

`[정적]` `PspInheritMitigationOptions(A, B)` 는 옵션 40개를 하나씩 보며 이렇게 고른다:

- **A 의 비트 2 (고정) 가 켜져 있거나, B 의 아래 2비트가 0 (안 정함) 이면 → A 를 따른다.**
- **아니면 → B 를 따른다.**

그리고 A 가 누구냐가 중요하다.

- 이름에 "Inherit (물려받다)" 가 있어서 앞 문서들 (run50 · run59) 은 A 를 **부모 가게** 값으로 읽었다.
- 실제 A 는 **시스템 전역 `PspSystemMitigationOptions`** 다 (부모 아님). B 는 IFEO, 두 번째 합치기에선 손님 속성이다.
  - 이 전역은 부팅 때 레지스트리 `…\Session Manager\Kernel` 값 `MitigationOptions` 에서 채워진다. 근거는 커널 INIT 표 (rva `0xc825a0 ~ 0xc825b8`) 에 키 글 · 값 이름 · 자료 칸 · 길이 칸이 나란히 있다는 것. **그 표를 읽는 함수는 안 따라갔다** `[추측]` → Q216 ④.
  - 파일 처음 값은 24B 0 이고, `PspInitPhase0 +0x108 ~ +0x19e` 가 길이를 맞추고 검증한다.
- 그래서 이 길 어디에도 **부모 값은 없다.**
- 비유: "본사 (관리사무소) 기본 규칙에 '바꾸지 마시오' 가 찍혀 있거나, 가게 특별 지시 칸이 비어 있으면 본사 것. 아니면 특별 지시 것." 부모 가게 (cmd) 의 카드는 아예 꺼내 보지 않는다.

### 2.9 사진 `0x02002002` 를 칸으로 풀기

`[관찰]` run44 8장 모두 업무 수첩 `+0xa0 = 0` · `+0xa8 = 0` · `+0xb0 = 0x02002002` · `+0xb8 = 0` (로그 줄 번호는 run 문서 "도구 메모").

| 셋째 칸 비트 | 값 | 옵션 | 누가 적었나 (2.7) | test.exe 에 따라 바뀌나 |
|---|---|---|---|---|
| 0~1 | 2 | 32 | 8번 `PspApplyMitigationOptions +0x510 ~ +0x523` (가게 CET 꺼짐) | 바뀔 수 있음 (가게 CET) |
| 12~13 | 2 | 35 | 6번 `+0x1e03 ~ +0x1e15` (무조건) | 안 바뀜 |
| 24~25 | 2 | 38 | 7번 `+0x1e1d ~ +0x1e30` (이 컴퓨터 `KiCoreIsolationEnforceable` = 0) | 컴퓨터마다 |

- `0x02002002` 를 16진 자리로 보면 `0x2` (비트 1) · `0x2000` (비트 13) · `0x02000000` (비트 25) 셋이 켜진 것이다 → 세 칸 모두 상태 "2".
- 둘째 칸 `+0xa8 = 0` 은 2.7 의 5번 길을 안 탔다는 것과 맞다.
- 정적 길과 사진이 **모순 없이 맞는다.** 그래서 run 문서 "정적과 동적이 다른 곳" 은 "없음" 이다 (아래 2.10 은 짐 지도 줄 하나가 사진과 안 맞아 보이는 별개 이야기).

### 2.10 덤 — `MitigationFlags2 = 0x40000000` 은 어느 갈래에서 왔나 (아직 `[공백]`)

flow-writer 가 `.asm` 을 읽다가 본 것이다 `[정적: flow-writer, 결과서 밖]`. 단계별로 본다.

1. **근거 ① (짐 지도)**: 3.11 `MitigationFlags2` 줄은 "조건 = 셋째 칸 비트 16~17 (옵션 36) = 1" 이라 적었다.
2. **근거 ② (사진)**: 그런데 사진 셋째 칸 비트 16~17 은 **0** 이다 (`0x02002002` 에 비트 16 · 17 없음). 그런데도 `MitigationFlags2` 는 `0x40000000` 이다 (K1 이전).
3. → 짐 지도 줄 대로라면 안 켜졌어야 한다. 어긋나 보인다.
4. **근거 ③ (`.asm`)**: `PspApplyMitigationOptions` 를 다시 보면 갈래가 셋이다.
   - 옵션 36 = 1 → `+0x5a5` 바로 (켬).
   - 옵션 36 = 2 · 3 → 건너뜀 (`+0x5a3 JNZ`).
   - **옵션 36 = 0 (안 정함)** → `+0x59a JZ` → `+0x6b0 BT EBX,0x13` · `+0x6b4 JNC +0x5a5` — **EBX 비트 19 가 꺼져 있으면 같은 `+0x5a5` 로 가서 켠다.**
5. 그래서: "0 이면 기본으로 켬" 갈래가 있다. 사진 값은 이 갈래에서 왔을 수 있다 `[추측]`.
6. **아직 확인 아님**: EBX 는 `+0x8f MOV EBX,[RBP+0x60]` (스택 인자로 보임 — 뜻은 안 봄 `[추측]`). EBX 비트 19 가 정말 꺼져 있었는지는 모른다 → **`[공백]`** (정적 **Q216 ①**, 동적 **Q215 ⑤**).

- 비유: 규칙 카드 칸이 비어 있으면 (0) 관리사무소가 "기본값으로 켜 둘까?" 를 다른 서류 (EBX) 의 한 칸을 보고 정한다. 그 서류 칸이 무엇인지는 아직 안 읽었다.
- run50 해설은 이 비트의 뿌리를 "시스템 기본값 `PspSystemMitigationOptions`" 로 짐작했다 `[추측]`. 이 짐작도 이번 덤으로 흔들렸다 (2.16 run50 고쳐 읽기).

### 2.11 작업 지시서 `+0x1d8` 을 읽는 함수 — 둘. 그리고 그 정체

#### 찾은 것 `[정적]`

결과서가 커널 전체에서 `[레지스터+0x1d8]` 211줄을 걸러 셋을 남겼다:

- 쓰기 `PspGetProcessParameterOverrides +0x4a` (run59).
- **읽기 1** `PspCaptureProcessParameters +0x26 MOV R8,[R8+0x1d8]` — 첫 인자 `CL ≠ 0` 갈래 (`+0x15` · `+0x17 JZ +0x58`) 에서, `+0x3a` 의 `PspCaptureUserProcessParameters` 에 **셋째 인자**로 넘긴다.
- **읽기 2** `PspDeleteCreateProcessContext +0x11a` → `+0x121` `PspDestroyProcessParameterOverrides` (0 이면 아무것도 안 함).

#### 그래서 run104 의 "셋째 인자 구조" 는 무엇이었나

**새 말 — 매개변수 덮어쓰기 구조**: 작업 지시서 `+0x1d8` 이 가리키는 작은 구조. 개업 서류를 옮겨 적을 때 **몇 칸을 이 값으로 바꿔 적으라**는 지시다 (run 문서 말, PDB 칸 이름 기준).

- 비유: 작업 지시서에 끼워 둔 **"수정 쪽지"**. "서류 옮겨 적을 때 이 칸들은 내가 적은 값으로 바꿔 주세요."
- 모양 `[정적]`:
  - `[+0x0]` 버전 (2 만 받음, run59)
  - `[+0x4]` 덮을 칸 비트:
    - 비트 0 → `DefaultThreadpoolCpuSetMasks` / `Count` (`+0x430` / `+0x438`)
    - 비트 1 → `RedirectionDllName` (`+0x410`)
    - 비트 2 → `HeapPartitionName` (`+0x420`) · `HeapMemoryTypeMask` (`+0x440`)
    - 비트 3 → `DefaultThreadpoolThreadMaximum` (`+0x43c`)
- 그러니 run104 가 `[추측]` 한 "속성 `0x20` 대 (`PsAttributeHeapFeatures` 류)" 는 **틀렸다.** 만들기 정보 칸도, 속성도 아니고 이 덮어쓰기 구조다. 2.16 정정.
- 누가 이 쪽지를 주나: `NtCreateUserProcess +0xab3` 의 `PspGetProcessParameterOverrides` 가 **확장 import** 로 받아 `+0x4a` 에 쓴다.
  - 그 확장 슬롯의 파일 값은 "아무 말 없이 끊는 번호" (`ret 0` 스텁) 다 (run59).

#### test.exe 는

- 슬롯이 파일 값 그대로 (스텁) 면 `+0x1d8 = 0` → 쪽지 없음 → 사용자 값 그대로 캡처하는 갈래. 사진 `+0x430 ~ +0x448 = 0` 과 맞는다.
- 치우기 (`PspDestroyProcessParameterOverrides`) 도 안 한다.
- 단 **슬롯이 실행 때도 정말 스텁인가** 는 정적으로 못 정한다 → 동적 **Q114 (4)** (= Q215 ②).

### 2.12 결과서 밖 전역 셋 — 근거만 옮기고, 셈은 안 바꾼다

`[정적]` 셋은 이미 앞 바퀴들이 가른 전역이다. 이번엔 근거만 다시 맞췄다.

| 전역 | 쓰는 줄 | test.exe 값 | 읽는 곳 |
|---|---|---|---|
| `RtlpUnhandledExceptionFilter` (`0x1d28e8`) | `LdrpInitializeProcess +0x134` (인코딩된 0) · `RtlSetUnhandledExceptionFilter +0x9` | 둘째 = 인코딩된 `kernelbase!UnhandledExceptionFilter` | 6곳, 모두 예외 · 실패 길 |
| `LdrpEnforceIntegrityContinuity` (`0x1d2900`) | `+0x4db SETZ` | **0 을 씀** | 2곳 |
| `LdrpAuditIntegrityContinuity` (`0x1d2918`) | `+0x4fe MOV` | **0 을 씀** | 1곳 |

#### 왜 2장 숫자에 +2 를 안 하나 — 단계별로

1. 결과서는 "무결성 둘을 2장에 센다: 아는 쪽 +2 (189 → 191, 모름 304 → 302, `끝까지 0` 214 → 212)" 라고 적었다.
2. 그런데 지금 짐 지도를 보면:
   - 3.8 에 두 이름의 줄이 **이미 있다** ("0 을 씀", 근거 `sast111 · run114`).
   - 7.3 에 run114 가 이미 빼서 셌다는 기록이 있다 (모름 304 → 268).
3. 즉 결과서의 304 · 189 · 214 는 **run114 전** 숫자다. 결과서가 본 "7.3 줄 1988 · 7.5 줄 2077" 도 옛 판 줄 번호다.
4. 결과서의 판단 ("셋 다 쓴 자리 아는 것") 과 **같은 결론이 이미 반영돼 있다.**
5. 그래서 → 다시 +2 하면 **같은 칸을 두 번 센다.** 이번엔 3.8 두 줄에 근거 (`sast119`) 만 보탰다.

- `RtlpUnhandledExceptionFilter` 는 run68 이 셌다 (결과서와 같음).
- 비유: 창고 재고표에 이미 "2개 입고" 줄이 적혀 있는데, 옛날 장부를 보고 온 사람이 "2개 더 적어야 해요" 한 것이다. 장부 판이 바뀐 걸 확인하고 안 적었다.

---

### Q115 — run60 이 남긴 것

### 2.13 손님 부탁 목록이 개업 서류 `+0x448` 까지 가는 길 — 닫힘

`[정적]` 사슬을 끝까지 이었다. 모두 **기능 스위치 `CreateProcessSegmentHeapEnablement…` 가 켜져 있고 목록 개수 > 0** 일 때의 이야기다.

| 순 | 어디서 | 하는 일 |
|---|---|---|
| ① | `PspBuildCreateProcessContext +0x1577 ~ +0x159f` | 스위치 확인 → `0x80000` 비트를 가진 속성 항목만 골라 `PspProcessUserCopyAttribute` 로 (번호 `0x22` 하나, run60) |
| ② | `PspProcessUserCopyAttribute` | 작업 지시서에 **개수 `+0x250` · 본문 길이 `+0x254` · 항목 `+0x230` (32B) · 본문 `+0x258`** 을 적는다 |
| ③ | `PspSetupUserProcessAddressSpace +0x115 ~ +0x150` | 새 가게 땅에 놓을 덩어리 크기에 목록 자리를 더한다 (`+0x130` `PspCalculateUserAttributeListSize`). 개수 0 이면 0 |
| ④ | `PspCopyAndFixupParameters +0x19d ~ +0x3eb` | 서류 복사 · PEB `+0x20` 쓰기 → `+0x3c7` 개수 > 0 확인 → 자리 = **서류 + 환경 바로 뒤 (8 올림)** → `PspCopyUserAttributes` |
| ⑤ | `PspCopyUserAttributes` | 그 자리에 `PS_ATTRIBUTE_LIST` 모양 (머리 · 항목 `0x20` × N · 본문) 을 만든다. 항목의 `Value` 주소를 새 자리에 맞게 고친다 |
| ⑥ | `PspCopyAndFixupParameters +0x3f8 ~ +0x402` | **`+0x448` (`AttributeList`) = ④ 의 목록 주소** (한 줄 읽기 ②) |
| ⑦ | ntdll `LdrpHandleUserCopyAttributeList` | 읽는 쪽 (run60). `+0x15` 에서 `+0x448` 읽고 0 이면 `+0x23 JZ +0x15b` |

- 비유: 손님이 낸 "따로 맡긴 부탁" 쪽지 중 **복사 표시 (`0x80000`) 가 붙은 것만** 작업 지시서에 옮겨 적고 (①②) → 새 가게 땅을 고를 때 그 쪽지 놓을 자리까지 셈해 두고 (③) → 개업 서류를 옮겨 적은 **바로 뒤에** 쪽지 묶음을 붙인 다음 (④⑤) → 서류 맨 끝 칸에 "부탁 목록은 저 뒤에 있어요" 라고 주소를 적는다 (⑥). 관리실은 출근해서 그 칸을 본다 (⑦).
- ② 에서 하나 더 `[정적: 일부]`: 항목 칸이 32B 라 **한 개만** 들어간다. 둘째가 오면 `+0x250` 을 덮는다. 같은 속성을 두 번 받는 걸 막는 줄은 안 봤다 → Q216 ⑤.

#### test.exe 는 — 스위치가 꺼져 있다

- ④ 의 `+0x1a4 JZ +0x416` 으로 `+0x3c7 ~ +0x402` 를 **안 지난다** (`+0x448` 은 사용자 값 그대로). ① 도 같은 스위치로 막혀 개수 0.
- 어느 쪽이든 test.exe 의 `+0x448 = 0` 이다. 사진과 맞다 (run44).

#### 2.11 과 헷갈리지 말 것 — 두 줄기는 별개

- run104 는 개업 서류 `+0x430` / `+0x438` 이 "속성 목록에서 온다" 고 볼 뻔했다. 아니다.
  - `+0x410 ~ +0x440` 몇 칸 ← **수정 쪽지** (`+0x1d8` 덮어쓰기 구조, 2.11).
  - `+0x448` 한 칸 ← **부탁 목록** (속성 `0x22`, 이 절).
- 같은 기능 스위치 이름 아래에 둘 다 보여서 헷갈렸지만, **들어오는 길이 다르다.**

### 2.14 `LdrpSuppressedExportOverrideListPtr` 를 읽는 곳 — ntdll 안에 없다

`[정적]` 결과서가 네 가지로 다 훑었다:

1. RIP 상대로 이 칸 (`0x1d2920`) 을 가리키는 줄 = **1줄** — `+0x49d` 쓰기뿐.
2. 이 칸을 가리키는 8 · 4바이트 값 = **0**.
3. 이웃 칸 (`0x1d28f0 ~ 0x1d2930`) 을 기준으로 `+8` 해서 접근하는 줄 = **0**.
4. 내보내기 2516개에 `Suppress` · `Override` 가 든 이름 없음. ntoskrnl · securekernel 본문에도 그 글 없음.

**새 말 — 쓰기만 있는 칸**: 적는 줄은 있는데, **같은 모듈 안에서 읽는 줄이 하나도 없는** 전역 칸.

- 비유: 관리실 칠판에 "억제 목록은 저기 있음" 이라고 **적기만 하고**, 관리실 안 누구도 그 줄을 안 본다. 바깥 누가 칠판 이름표를 보고 와서 읽을 수는 있다 `[추측]`.
- 쓰는 줄 (run60 그대로): `+0x48d` 확인 → `+0x492 CMP EAX,EBX` · `+0x494 JNZ +0x4a4` → (1 일 때만) `+0x496 LEA` · `+0x49d MOV`.
- 목록 값 (결과서): `0x1740f0` 의 첫 8B = ntdll `RtlMapSecurityErrorToNtStatus` 시작 주소. **한 칸짜리 목록으로 보인다** `[추측: 이름 뜻]`. 바로 뒤 `0x1740f8` 은 다른 변수다.
- test.exe 는 **쓰기도 안 탄다** (CFG 억제 꺼짐, run60) → 끝까지 0. 사진과 맞다.

### 2.15 덤 다섯

#### ① 속성 `0x22` = `PsAttributeHeapFeatures` `[정적]`

- PDB `_PS_ATTRIBUTE_NUM` 에 `PsAttributeHeapFeatures = 34` (= `0x22`) 가 있다.
- 기능 스위치 이름 (`CreateProcessSegmentHeapEnablement`) · 읽는 쪽 `RtlpHpOptIntoSegmentHeap` 과도 맞는다.
- 그래서 ntdll `.data 0x1ce268` = **"프로세스 만들 때 받은 힙 기능 요청 (세그먼트 힙 0/1)"** 칸.
- run60 은 이 이름을 "PDB 에 없음" 으로 남겼다. **틀렸다** — run104 결과서 f-2 가 이미 PDB 이름을 봤는데 run60 줄에 반영이 안 됐던 것. 2.16 정정.

#### ② `LdrpMakePermanentImageCommit` — 같은 값을 다시 쓴다

- `[정적]` (rva `0xa3e78`, 56B) 걸친 쪽마다 맨 앞 8B 를 **읽고 같은 값을 그 자리에 다시 쓴다** (`+0x24` · `+0x27`). 값은 안 바뀐다.
- 부르는 곳: `LdrpLocateMrdata +0x4d` (`.mrdata`) · `LdrpCfgProcessLoadConfig +0x144`.
- 왜 하나 `[추측]` (코드에 이유 없음): "쓰기가 되는 지금 [copy-on-write](run103-image-share-해설.md) 사본 쪽을 **미리 확보**해 둬서, 뒤에 덮개를 다시 열 때 실패하지 않게."
- 비유: 같이 보는 공용 칠판에 **같은 글자를 덧쓴다.** 보기엔 아무 변화가 없지만, 그 순간 이 가게 몫 사본이 생긴다 (라고 짐작한다).

#### ③ `+0xdc4` 의 `ECX` = 1 (닫기) `[정적]`

run60 은 `RBX` 를 안 따라가서 `[추측]` 이었다. 이번에 따라갔다.

1. `+0xd4f MOV EAX,2`
2. `+0xd65 LEA EBX,[RAX+0x2e]` → `EBX` = 2 + `0x2e` = **`0x30`**
3. `+0xdc1 LEA ECX,[RBX-0x2f]` → `ECX` = `0x30` − `0x2f` = **1**

- 그래서 `+0xda1` 열기 (0) 와 `+0xdc4` 닫기 (1) 가 짝으로, `\KnownDlls` 핸들 (`.mrdata` `LdrpKnownDllDirectoryHandle`) 쓰기를 감싼다. 유리 덮개를 열고 → 칠판에 핸들 번호 적고 → 닫는다.
- flow.md · run60 의 `[추측]` → `[정적]`.

#### ④ `LdrEnsureMrdataHeapExists` 가 쓰기까지 가는 조건 `[정적]`

- ① **CFG 켜짐** (`LdrControlFlowGuardEnforced` ≠ 0 — 수첩 `+0xb8` ≠ 0 그리고 `CfgOverride` 꺼짐) 그리고 ② **힙이 아직 없음**.
- 둘 다 맞으면: 예약 → 힙 만들기 → 열기 → 락 → `+0xe8` `LdrpMrdataHeapUnprotected` · `+0xef` `LdrpMrdataHeap` 쓰기 → 닫기.
- **test.exe 는 ① 에서 막힌다** (run44 수첩 `+0xb8 = 0`). 두 칸 모두 끝까지 0. run63 결론과 같고 줄만 보탰다.

#### ⑤ `.mrdata` 덮개를 여닫는 자리 전수 — 50자리 / 함수 21개 `[정적]`

초기화 줄기 안에서 (`LdrpInitializeProcess` 몸통 순서):

| 순 | 자리 | test.exe 가 타나 (정적 조건) |
|---|---|---|
| 1 | `+0x415` `RtlInsertInvertedFunctionTable` (안 `+0x4b` 열기 · `+0x67` 닫기) | 탐 |
| 2 | `+0x441` 열기 · `+0x470` 빼기 (안에서 한 쌍) · `+0x47c` 닫기 · **`+0x488` 닫기 → `NtProtectVirtualMemory`** | 탐 |
| 3 | `+0x531` `LdrpInitializeExecutionOptions` (안 `+0x373` / `+0x384`) | 안 탐 — IFEO 값 비트 0 일 때만 |
| 4 | `+0xda1` 열기 · `+0xdb8` `NtOpenDirectoryObject` · `+0xdc4` 닫기 | 탐 |
| 5 | `+0x1277` `RtlInitializeHistoryTable` (안 `+0x17` / 꼬리 `+0xb4`) | 탐 (조건 없음) |
| 6 | `+0x1713` `AVrfInitializeVerifier` | App Verifier 때만 `[추측: 조건 안 봄]` |
| 7 | `+0x17c1` `LdrpLoadWow64` | WoW64 때만 |
| 8 | `+0x1806` `LdrpInitializeCfgScpHelpers` (안 `+0x5a` / `+0x80`) | 안 탐 — CFG 꺼짐 |
| 9 | DLL 올릴 때마다 `RtlInsertInvertedFunctionTable` | 탐 (모듈 수만큼) |

- 2번의 `+0x488` 이 덮개가 **처음 진짜로 닫히는** 자리다 (run60 의 숫자판 이야기 — 1 에서 시작해 여기서 처음 0).
- 그 뒤 4 · 5 · 9 는 숫자판이 0 → 1 → 0 으로 오가니 **열 때 한 번, 닫을 때 한 번** 창구에 갈 것이다 (run60 규칙대로 셈한 **기대**값 — 본 것은 아니다). 그래서 run 문서의 동적 기대값이 "`+0x488` 1 + 짝마다 2 + DLL 마다 짝" 이다 (4절 Q215 ④).
- 줄기 밖 (프로그램이 부를 때) 12 함수와, 인자를 한 줄로 못 가른 자리 여섯은 결과서 9 (e) 그대로다 → 짐 지도 7.53.

---

### 2.16 정정 · 닫힘 — 앞 문서 여섯과 그 해설

run 문서가 앞 run 문서 맨 위에 한 줄씩 넣었다. 이 바퀴에서 **같은 틀린 문장이 있는 해설**에도 맨 위에 "고쳐 읽기 (run123)" 한 줄을 붙였다 (본문은 그대로).

| 앞 문서 | 무엇 | 해설에 붙인 줄 |
|---|---|---|
| run50 | 정정: 완화 옵션 "**부모** · 시스템 기본 · 이미지 헤더" 의 "부모" — 이 길에 부모 값은 없다 | [run50 해설](run50-eprocess-create-해설.md) 2.11 "부모와 합치기" · `MitigationFlags2` 뿌리 짐작 |
| run59 | 정정: "12~13 · 24~25 의 2 는 입력 표 값 `[공백]`" · "부모 상속" · "PDB 에 열거형 없음". 닫힘: `+0x1d8` 읽는 함수 둘 | [run59 해설](run59-init-leftovers-해설.md) 2.12 ⓗ · 4절 표 |
| run60 | 정정: 속성 `0x22` "PDB 에 없음". 닫힘: `+0x448` 사슬 · 억제 목록 읽는 곳 없음 · `+0xdc4` `ECX` = 1 · `LdrpMakePermanentImageCommit` 동작 | [run60 해설](run60-func-table-mrdata-해설.md) 2.6 · 2.10 · 2.11 · 4절 |
| run104 | 정정: "셋째 인자는 속성 `0x20` 대 (`PsAttributeHeapFeatures` 류)" `[추측]` → `+0x1d8` 덮어쓰기 구조 | [run104 해설](run104-params-block-해설.md) 4절 |
| run58 | 닫힘: 상태 이름 · 커널 사본 글 · 같은 글 쓰는 다른 함수 | [run58 해설](run58-registry-strings-해설.md) 2.14 · 4절 |
| run106 | 닫힘: `0x40000036` 이름 `[추측: 기억]` → `[정적]` | [run106 해설](run106-snap-map-small-해설.md) 2.9 · 4절 |

#### PE_emulator 쪽

결과서 "지금 우리 맞나" 를 run 문서가 옮긴 것이다. `../PE_emulator` 코드는 flow-writer 가 안 읽었다.

- Q112 셋 **맞음**. (`0x40000036` 주석에 이름을 붙일 수 있다.)
- Q113 (a) **다름 — 알고 줄인 것**: 업무 수첩 `+0xa0` 24B 를 0 으로 둔다. ntdll 이 `+0xb0` 을 스택 사본으로만 옮겨서 **동작은 같다.** 맞추려면 세 줄 (비트 12~13 · 24~25 · 0~1 := 2). 단 비트 24~25 는 컴퓨터마다 다르다.
  - **사진 값처럼 맞출지는 사용자 결정 대기**다 (plan.md 결정 기록 "사용자에게 물을 것").
- Q113 (b) **맞음 (줄임)** — `PspGetProcessParameterOverrides` 를 안 해서 `+0x1d8` = 0. 확장이 붙으면 확인 필요.
- Q115 (a) · (b) **맞음** (스위치 꺼짐 · 0). (c) `.mrdata` 여닫는 순서 · `NtProtectVirtualMemory` 수는 **확인 필요**.

### 2.17 정리: 비유로 한 번 더

1. 관리사무소가 새 가게의 **안전 규칙 카드**를 만든다. 출발은 **본사 기본 규칙** (부팅 때 레지스트리에서 정한 것) 이다. 부모 가게 카드는 안 본다.
2. 가게 이름별 특별 지시 (IFEO) 를 겹쳐 본다. 손님이 옵션을 직접 냈으면 한 번 더 겹친다.
3. 그다음 사무소가 셋째 줄에 **"끔" 도장 둘**을 직접 찍는다. 35번 칸은 늘, 38번 칸은 이 건물에 방화 칸막이 설비가 없을 때.
4. 카드대로 가게 장부를 켜는 사람이 32번 칸에도 "끔" 을 적는다 (이 가게는 CET 를 안 쓰니까). 그래서 셋째 줄은 `0x02002002`.
5. 카드는 작업 지시서를 거쳐 관리실 **업무 수첩**에 베껴진다. 관리실은 그 셋째 줄을 거의 안 읽는다.
6. 작업 지시서엔 **수정 쪽지** 자리 (`+0x1d8`) 도 있다. test.exe 는 쪽지가 없다. 서류 옮길 때 한 번, 작업 끝나고 치울 때 한 번 들여다본다.
7. 손님이 따로 맡긴 **부탁 목록**은 개업 서류 바로 뒤에 붙이고, 서류 끝 칸 (`+0x448`) 에 그 자리를 적는다. test.exe 는 스위치가 꺼져 목록이 없다.
8. 관리실 쪽: 칠판에 **적기만 하고 아무도 안 읽는 줄** (억제 목록 주소) 이 하나 있다. 유리 덮개를 여닫는 자리는 50곳이고, `\KnownDlls` 핸들을 적을 때도 열고 → 적고 → **닫는다** (`ECX` = 1).
9. 관리사무소와 관리실은 **같은 레지스트리 조회 쪽지**를 따로 한 장씩 갖고 있다. 실패 코드 `0xc000090b` 의 공식 명칭은 이웃 가게 벽의 대조표에서 찾았다: `STATUS_BAD_DATA`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 곳 | 전 | 후 |
|---|---|---|
| 기준 · pdb 목록 · 근거 로그 | — | **(run123) 줄 추가**: 사진 값은 run44 로그 8장의 수첩 `+0xa0 ~ +0xb8` 두 줄 (줄 번호 flow-writer 가 맞춤). 새 동적 실행 없음 |
| 2.1 `PspAllocateProcess` 완화 옵션 | "`+0x1aea` 시스템 기본 · **부모**와 합치기" | "부모" 취소선 + **정정 줄** `[정적]`: A = 시스템 전역, B = IFEO · 손님 속성, 셋째 칸에 "2" 를 직접 씀 (`+0x1e03 ~ +0x1e30`) |
| 2.1 같은 절 | `MitigationFlags2` `+0x5a5` 한 줄 | **새 줄**: 옵션 36 = 0 이면 `+0x6b0` EBX 비트 19 갈래로도 간다 — 사진은 이 갈래일 수 있다 `[추측]`, EBX 뜻 `[공백]` |
| 2.1 `PspPrepareSystemDllInitBlock` (run59 ⓗ 줄) | "12~13 · 24~25 의 2 는 **입력 표가 갖고 온 값** `[공백]`", "이름은 PDB 에 열거형 없어 `[공백]`" | 출처 `[정적]` (`+0x1e03 ~ +0x1e15` 무조건 · `+0x1e1d ~ +0x1e30`), 이름 표 `[정적]` (`PS_MITIGATION_OPTION` 32 ~ 39), 사진 `[관찰]` |
| 2.1 `PspCopyAndFixupParameters` | 셋째 인자 = "속성 `0x20` 대" `[추측]` (run104) | **셋째 인자 = `+0x1d8` 덮어쓰기 구조** `[정적]`. 속성 `0x22` = `PsAttributeHeapFeatures`. `+0x448` **사슬 끝까지** `[정적]` |
| 2.4 레지스트리 두 함수 | 상태 `0xc000090b` 이름 없음 | 커널 사본 글 같음 `[정적]`, **`STATUS_BAD_DATA`** `[정적]` |
| 2.4 `.mrdata` 여닫기 | `+0xdc4` 의 `ECX` = 1 은 `[추측]` | **`[정적]`** (`0x30 − 0x2f = 1`). 여닫는 자리 50 / 함수 21, `LdrpMakePermanentImageCommit` 동작 `[정적]` · 목적 `[추측]` |
| 2.4 CFG 억제 · `0x1ce268` | 읽는 곳 `[공백]` · 이름 `[추측]` | 읽는 곳 **없음** `[정적]` · `PsAttributeHeapFeatures` `[정적]` |
| 2.6 결과 코드 | `0x40000036` 이름 `[추측: 기억]` | **`[정적]`** `STATUS_IMAGE_AT_DIFFERENT_BASE` |
| 4장 run58 · run59 · run60 정적 줄 | 남은 것 | **(해결됨 → run123)** |
| 4장 새 줄 셋 | — | **(run123 새로)** 동적 ① ~ ⑤ (선택) · 정적 ① ~ ⑦ · PE_emulator 쪽 |
| 5장 근거 run · 요청서 표 | — | **run123 줄 둘** |
| 그림 (mermaid) | — | **안 바뀜** (이미 있는 상자 안쪽) |

- 태그 변화를 한 줄로: run59 가 `[공백]` 으로 남긴 "2 의 출처" 와 "옵션 이름" 이 둘 다 **`[정적]`** 이 됐다. `+0xdc4` `ECX` · `0x40000036` 이름은 `[추측]` → **`[정적]`**.
- 반대로 새 `[공백]` 하나가 생겼다: `MitigationFlags2` 가 어느 갈래에서 왔나 (2.10).

### 짐 지도에서 바뀐 것

- **2장 숫자 그대로.** 새 짐 칸도 새 줄도 없다. 결과서의 "+2" 는 두 번 셈이라 안 했다 (2.12).
- 이미 있는 3장 줄 8개의 근거 · 정정만 바꿨다:
  - 3.2 `AttributeList` (사슬 ① ~ ⑥)
  - 3.8 `0x1ce268` (`PsAttributeHeapFeatures`) · `RtlpUnhandledExceptionFilter` (읽는 곳 6) · 무결성 둘 (근거만) · `LdrpSuppressedExportOverrideListPtr` (읽는 곳 없음)
  - 3.9 `LdrpKnownDllDirectoryHandle` (`ECX` = 1) · `LdrSystemDllInitBlock` (셋째 칸 출처 · 이름)
  - 3.11 `MitigationFlags2` (합치는 재료에서 "부모" 지움, `+0x6b0` 갈래 `[공백]`)
- 3.9 머리 (`.mrdata` 여닫는 자리 50), 6장 레지스트리 줄, 7.4 · 7.6 (만들기 정보 `+0x1d8`), 7.19 · 7.20 · 7.36 의 해결 표시, **새 7.53** (이번 바퀴가 넘긴 질문).

### 큰 그림에서 어떻게 이어지나

- 업무 수첩 `+0xa0` 24바이트는 **커널이 K1 앞에 새 가게 안 ntdll 에 직접 써 주는 짐**이다 (짐 지도 3.9). 그 한 칸의 값이 이제 "누가 · 왜" 까지 다 설명된다.
  - 에뮬레이터 쪽에서 보면: 이 값은 test.exe 가 아니라 **컴퓨터 (비트 24~25) 와 가게 CET (비트 0~1)** 에 달린 값이다.
- `+0x448` 사슬이 닫혀서, "커널이 개업 서류를 꾸리는 일" (run32 · run104) 의 마지막 빈 줄이 메워졌다. test.exe 에선 꺼진 갈래라 흐름 숫자는 안 바뀐다.
- `.mrdata` 50자리 표는 에뮬레이터가 `NtProtectVirtualMemory` 를 몇 번 받아야 하는지 맞춰 볼 **기대값**이 된다 (Q215 ④).
- 이 바퀴로 run58 · 59 · 60 이 넘긴 정적 몫은 다 닫혔다. 다만 분석은 **사용자 결정으로 여기서 멈춘다** (plan.md 2026-11-16). 다시 시작하면 문서 남은 것 (Q124 + Q127) 부터다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| 비트 12~13 · 24~25 를 쓰는 순간을 디버거로 보면 정말 그 값인가, `KiCoreIsolationEnforceable` 가 정말 0 인가 | 정적 바퀴다. 사진은 결과 (`0x02002002`) 만 있고, 쓰는 순간 · 전역 값은 본 적 없다 | **Q215 ①** (동적, 선택). `nt!PspAllocateProcess+0x1e1d` (= **`nt+0xa04385`** — 결과서의 `nt+0xa04380` 은 명령 한가운데라 run 문서가 고침) · `+0x1e38` 에서 `dq @rsp+0x400 L3`, `+0x1e22` 뒤 `al` |
| 확장 import 슬롯 `__imp_PsGetProcessParameterOverrides` 가 실행 때도 스텁인가 (그래서 `+0x1d8` = 0 인가) | 부팅 때 바뀌는지 정적으로 못 정한다 | **Q114 (4)** (동적) — Q215 ② 는 같은 일이라 그쪽에서 |
| 레지스트리 `Session Manager\Kernel` 값 `MitigationOptions` 가 있나, `PspSystemMitigationOptions` 실제 값 | 실행 때 값이다 | **Q215 ③** (동적, 선택) |
| test.exe 한 번에 `.mrdata` 로 `NtProtectVirtualMemory` 가 몇 번 오나 | 정적으로는 기대값 (2.15 ⑤) 까지만 | **Q215 ④** (동적, 선택) |
| `MitigationFlags2 = 0x40000000` 이 `+0x6b0` 갈래 (옵션 36 = 0, EBX 비트 19 꺼짐) 에서 왔나 | flow-writer 가 `.asm` 을 읽다 본 덤이라 EBX 값 · 뜻을 안 봤다 | **Q215 ⑤** (동적, 선택) `r ebx` · **Q216 ①** (정적) EBX (`[RBP+0x60]`) 비트 19 뜻 · 출처 — 그때까지 `[공백]` |
| `LdrpSuppressedExportOverrideList` 의 실제 길이 · 누가 읽나 | ntdll 안엔 읽는 곳이 없다. 다른 모듈은 안 훑었다 | **Q216 ②** (정적) |
| `KiCoreIsolationEnforceable` 를 정하는 식 | 이번엔 갈래만 봤다 `[정적: 갈래만]` | **Q216 ③** (정적) `KeOptimizeSpecCtrlSettings` |
| `PspSystemMitigationOptions` 를 레지스트리에서 채우는 함수 | INIT 표만 봤고 그 표를 읽는 쪽은 안 따라갔다 `[추측]` | **Q216 ④** (정적) |
| `+0x448` 사슬에 항목이 정말 한 개만 들어가나 | 두 번 받는 걸 막는 줄을 안 봤다 `[정적: 일부]` | **Q216 ⑤** (정적, 켜짐 갈래를 만들 때) |
| `LdrProtectMrdata` 인자를 한 줄로 못 가른 자리 여섯 | 인자가 레지스터 값이라 한 줄로 안 갈린다 | **Q216 ⑥** (정적) |
| 커널 `RtlpGetNtProductTypeFromRegistry` 사본을 `PspSiloInitializeUserSharedData` 가 부르는 길 | 곁 사실이라 부르는 곳까지만 봤다 | **Q216 ⑦** (정적, 낮음) — test.exe 와 무관 `[추측]` |
| PE_emulator 업무 수첩 `+0xa0` 을 사진 값 (`0x02002002`) 처럼 맞출까 | 분석이 아니라 결정할 일이다 | **사용자 결정 대기** |

- 동적은 모두 **선택**이다 (결과서 "필수 없음"). 멈춤점은 test.exe 조건을 붙인다.
- 이번 바퀴 뒤로 분석은 멈춘다. 위 항목은 다시 시작할 때 대기열 순서대로 다룬다.

---

## 5. 더 읽기

- run 문서: [`run123-run58-60-leftovers.md`](run123-run58-60-leftovers.md) — "요약", "결과" 의 Q112 (1) ~ (3) · Q113 (a) (b) (c) · "Q113 덤" · Q115 (a) (b) (c), "정정", "아직 확인 필요", "도구 메모" (사진 로그 줄 번호)
- 요청서: [`_workspace/sast/119_run58-60-leftovers.md`](../../_workspace/sast/119_run58-60-leftovers.md)
- 결과서: [`_workspace/sast/119_run58-60-leftovers_result.md`](../../_workspace/sast/119_run58-60-leftovers_result.md) — "한눈에", 질문 1 ~ 9, "동적분석으로 확인해야 할 것", "다음 요청 후보" (멈춤점 주소 · 셈 제안은 run 문서가 고친 쪽을 볼 것)
- flow.md: [`flow.md`](flow.md) — 2.1 `PspAllocateProcess` 완화 옵션 · `PspPrepareSystemDllInitBlock` · `PspCopyAndFixupParameters`, 2.4 레지스트리 두 함수 · `.mrdata` · CFG 억제, 2.6 결과 코드 줄, 4장 run123 줄 셋, 5장 run123 줄
- 짐 지도: [`baggage.md`](baggage.md) — 2장 메모 (셈 판단), 3.2 · 3.8 · 3.9 · 3.11, **7.53**
- 앞 해설:
  - [run59 해설](run59-init-leftovers-해설.md) — 2.12 옵션 표 4비트 칸 (ⓗ), 확장 import 스텁 (ⓖ)
  - [run60 해설](run60-func-table-mrdata-해설.md) — 2.6 `.mrdata` 숫자판, 2.10 CFG 억제, 2.11 속성 `0xa0022`
  - [run58 해설](run58-registry-strings-해설.md) — 레지스트리 두 함수, 제품 종류 갈래
  - [run104 해설](run104-params-block-해설.md) — 개업 서류 칸 · 셋째 인자 · `+0x448` 첫 줄
  - [run50 해설 2.11](run50-eprocess-create-해설.md) — 완화 옵션 (`MitigationFlags 0x20` 은 PE 헤더에서)
  - [run106 해설 2.9](run106-snap-map-small-해설.md) — 메시지 표, `0x40000036` 뜻
  - [run32 해설](../02_sprint/run32-params-initblock-해설.md) — 업무 수첩, 안전 규칙, 기능 스위치

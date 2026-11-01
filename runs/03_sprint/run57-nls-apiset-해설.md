# run57 nls-apiset — 해설

> 이 글은 [run57 문서](run57-nls-apiset.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q108**: PE_emulator 요청 4-A — 2-I (kernel32·kernelbase 올리기) 전에 꼭 필요한 셋. (1) NLS 글자표, (2) ApiSet 뷰, (3) `PEB.ApiSetMap` 첫 읽기 알림 (`_workspace/plan.md`, 이번에 **끝**)
  - 사용자 결정 (2026-11-01) 으로 맨 앞에 끼운 항목이다 (Q104 VM 단계 앞).
  - 다음 항목: **Q109** (PE_emulator 요청 4-B~E, 정적) → Q104 VM 단계 → Q78d4 → …
  - 남은 질문이 간 곳: 결과서 동적 1 ~ 5 (5 = 전역 읽기 포함) → 새 **Q110** (동적). 6 (NLS 파일 내용·크기) → **Q104 VM 단계** (4절).
- 근거:
  - run 문서 [`run57-nls-apiset.md`](run57-nls-apiset.md) (가장 중요)
  - 요청서 [`_workspace/sast/52_nls-apiset.md`](../../_workspace/sast/52_nls-apiset.md)
  - 결과서 [`_workspace/sast/52_nls-apiset_result.md`](../../_workspace/sast/52_nls-apiset_result.md) (판정: **분석성공**, PDB 일치)
  - flow.md 변경: 2.1 (`MmCreatePeb` · `PspSetupUserProcessAddressSpace` · **새 절 `MmMapApiSetView`** · 짝 표), 2.4 (트리 · **새 절 NLS**), 2.5 (`LdrpPreprocessDllName`), 4장, 5장
  - baggage.md 변경: 3.1 · 3.8 · 4장 · 5장 · 7.6.3 · 7.18 · 2장 숫자 (run 문서 "짐 지도 반영")
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음).
  - `[관찰]` 로 적힌 값은 전부 **run44 사진 (덤프) 로그를 다시 읽은 것**이다. 새 실행이 아니다.
- **짐 지도 바퀴다.** `baggage.md` 가 바뀌었다 (2.14).
- 결과서와 run 문서가 다른 곳 다섯 (오프셋 숫자만). 넷은 flow-writer, 하나 (`LdrpPreprocessDllName +0xc6` 목적지) 는 오케스트레이터가 `.asm` 으로 고쳤다. 이 글은 **run 문서 숫자**를 따른다 (2.13).

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- [PEB](../02_sprint/run10-allocate-process-해설.md#peb) (**안내판**), [매핑 올리기](../02_sprint/run10-allocate-process-해설.md#매핑-올리기) (세트를 땅에 **갖다 놓기**) → run10 해설 2절
- 섹션 = **조립식 건물 세트**, 섹션 객체 = **주문 꾸러미** → [run31 해설](../02_sprint/run31-image-section-해설.md) 2.5·2.6
- 뷰 = 세트를 우리 땅에 **실제로 세워 놓은 모습** → [run38 해설 2.3](../02_sprint/run38-map-module-해설.md)
- api-set = **별명**, ApiSet 맵 = **별명 사전** → [run37 해설](../02_sprint/run37-k32-import-order-해설.md) 첫머리 비유
- silo = 상가 안의 **구역** → [run32 해설](../02_sprint/run32-params-initblock-해설.md) 2.11
- 코드 페이지 (**문자 번호표 몇 번 판**), `LdrpInitializeNlsInfo` 의 값 고르는 순서 → [run45 해설 2.10 (4)](run45-work-list-해설.md)
- 사진 8장 K1 · P1a · P1b · P2 … → [run44 해설 2.8](run44-baggage-snap-해설.md)
- syscall 인자 5번째부터 = **유저 스택** 칸 → [run56 해설](run56-syscall-args-해설.md)

오케스트레이터가 고른 처음 나오는 말 (NLS 글자표·코드 페이지, ApiSet 지도, 섹션·뷰, silo) 과, 이번 글에서만 쓰는 말 몇 개는 **본문에서 처음 나오는 자리**에서 한 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**, 프로세스 = **가게**, PEB = 가게 **안내판**.
- 시스템 콜 = 관리사무소 **신청 창구**, ntdll 스텁 = 관리실 **전화기** (run56).
- 부팅 = 관리사무소가 **문 여는 날** (run56).
- 섹션 = 관리사무소 **창고에 있는 원본 물건**, 뷰 = 그 물건을 가게 땅에 **펼쳐 놓은 한 판** (run38 그림을 그대로).
- run 문서가 이번에 쓴 그림 (그대로 가져오되 이름 하나만 맞춘다):
  - NLS 표 = **글자 사전**.
  - ApiSet 지도 = run 문서는 "**전화번호 바꿈표**" 라고 불렀다. 이 시리즈에선 run37 부터 **별명 사전** 이라고 불러 왔다. 가리키는 건 같다. 이 글은 **별명 사전** 으로 쓴다.
  - `GlobalRtlNlsState` = run 문서의 "책상 서랍". run56 에서 "개인 서랍" 은 레지스터였으니 겹치지 않게 이 글은 **글자 서랍** 이라고 부른다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| ApiSet 섹션은 누가 언제 만드나 | **부팅 때** `MiInitializeApiSets` 가 부트 로더가 넘긴 표를 **바이트 그대로 복사**한 pagefile 섹션 `[정적]` |
| PEB `+0x68 ApiSetMap` 은 누가 쓰나 | **`MmMapApiSetView +0x92`** (부르는 쪽 `PspSetupUserProcessAddressSpace` 가 아님). run45 까지 `[공백]` 이던 칸 `[정적]` |
| NLS 섹션은 누가 언제 만드나 | **부팅 때** `ExInitializeNls` 길이 `\NLS\NlsSectionCP<번호>`·`\NLS\NlsSectionCASE` **영구** 섹션을 파일에서 만든다 `[정적]` |
| `NtGetNlsSectionPtr` 는 유저에게 무엇을 주나 | 이름 붙은 섹션을 **열어서**, 부른 프로세스에 **읽기 전용 뷰**를 놓고 **뷰 주소만** 돌려준다 `[정적]` |
| 매니페스트 없는 exe 에서 `LdrpFusionManifestCodePages` 는 | **"없음" (`AL = 0`)**. 그래서 test.exe 코드 페이지는 레지스트리에서 온다 `[정적]` |
| PEB 코드 페이지 세 칸 K1 전 첫 주인 | **커널 `MmCreatePeb +0x546`·`+0x555`·`+0x564`**. ntdll 은 같은 값을 다시 쓴다 `[정적]` |
| `PEB.ApiSetMap` 이 0 이면 ntdll 이 죽나 | **예.** `LdrpPreprocessDllName +0x82 → +0xc3` 사이에 0 검사가 없다 `[정적]` |

이 글에서 붙잡아 둘 세 줄:

> **(1) NLS 사전과 ApiSet 별명 사전은 둘 다 관리사무소가 "문 여는 날" (부팅) 미리 만들어 둔 물건이다. test.exe 때는 새로 만들지 않고 펼쳐 놓기만 한다.**
>
> **(2) ApiSet 은 커널이 알아서 가게 벽에 붙이고 안내판에 적는다 (① 부모 cmd 스레드). NLS 는 관리실이 출근해서 직접 전화해 받아 온다 (② test.exe 첫 스레드).**
>
> **(3) 둘 다 뷰는 읽기 전용이고, 주소는 커널이 고른다.**

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 구간은 한 지점이 아니라 **세 시간대에 흩어져** 있다. 그중 하나는 흐름도 범위보다 앞 (부팅) 이다.

```
(부팅 — test.exe 실행보다 훨씬 전, 흐름도 범위 앞)
 InitBootProcessor → MmInitSystem → MiInitSystem → MiInitializeApiSets   ApiSet 섹션 만들기  ← 이번
 Phase1InitializationIoReady → ExInitializeNls → … NtGetNlsSectionPtr    NLS 섹션 만들기    ← 이번

cmd.exe 의 부탁 (그림 1, 부모 스레드 ①)
 NtCreateUserProcess → PspAllocateProcess
   ├─ MmCreatePeb                    PEB 코드 페이지 세 칸  ← 이번
   └─ PspSetupUserProcessAddressSpace
        └─ MmMapApiSetView           ApiSet 뷰 + PEB+0x68   ← 이번

test.exe 첫 스레드 (그림 2, ②)
 LdrInitializeThunk → … → LdrpInitializeProcess
   ├─ LdrpInitializeNlsInfo          NLS 뷰 받아서 글자 서랍에  ← 이번
   └─ … kernel32 준비 (2.5) → LdrpPreprocessDllName   ApiSetMap 첫 읽기 후보  ← 이번
 → (CRT) → main
```

- flow.md 로는 2.1 (커널, 부모 스레드 안), 2.4 (로더 시작), 2.5 (kernel32 준비) 세 곳에 걸친다.
- 부팅 쪽은 지도 범위 앞이다. 하지만 "섹션이 어디서 왔나" 를 끝까지 따라가려면 거기까지 가야 해서 같이 봤다.

### 왜 이 질문이 지금 왔나

- `../PE_emulator` 가 2-I (kernel32·kernelbase 올리기) 에 닿기 전이다.
- 그런데 kernel32·kernelbase 는 글자 변환과 DLL 별명 풀이를 쓴다. 그 둘의 재료가 이번 셋이다.
- PE_emulator 는 지금 이 셋을 "공사 중" 으로 두었다 (요청서 "왜").

### 새 말 넷 — 질문을 읽기 전에

- **NLS 글자표** = 글자를 바이트 ↔ 유니코드로 바꾸고, 대·소문자를 바꿀 때 보는 표 묶음 `[추측]` (일반적으로 알려진 설명).
  - 비유: 관리실 책상에 두는 **글자 사전**.
  - 이번 글에선 두 종류만 나온다: **코드 페이지 표** (종류 `0xb`) 와 **대문자 표** (종류 `0xe`) (run 문서 1번 표).
- **코드 페이지** = 그 사전이 **몇 번 사전**인지 정하는 번호. run45 해설의 "문자 번호표 몇 번 판" 과 같은 것.
  - 이번에 나오는 번호: `0x3b5` (= 949, 사진 값), `0xfde9` (= 65001, run 문서가 "UTF-8 표" 라고 부르는 것).
  - 949 가 한국어 쪽 번호라는 건 일반적으로 알려진 설명이다 `[추측]`.
- **ApiSet 지도** = `api-ms-win-…` 같은 **별명 DLL 이름**을 진짜 DLL 이름으로 바꿔 주는 표. 비유: **별명 사전** (run37).
  - 실행 중에 관리실이 펴 보는 건 PEB `+0x68 ApiSetMap` 이 가리키는 사본이다.
- **섹션·뷰** (다시 짧게) = 섹션은 관리사무소 **창고의 원본 물건**, 뷰는 그걸 어떤 가게 땅에 **펼쳐 놓은 한 판**. 같은 원본을 여러 가게에 펼칠 수 있다 (run38).
  - 이번엔 exe·DLL 같은 **이미지** 섹션이 아니라 **데이터** 섹션 (표가 담긴 물건) 이다.

### 무엇이 궁금했나

요청서 질문을 셋으로 묶으면:

1. **NLS 글자표**: `nt!NtGetNlsSectionPtr` 가 종류 `0xb`·`0xe` 일 때 어떤 섹션을 쓰나? 섹션은 이 함수가 처음 만드나, 부팅 때 누가 만들어 두나? 어떻게 펼치고 무엇을 돌려주나? ntdll 쪽 `RtlInitCodePageTable`·`LdrpFusionManifestCodePages` 는 무엇을 하나?
2. **ApiSet 뷰**: `MmMapApiSetView` 는 어떤 섹션을 펼치나? 섹션은 부팅 때 누가 만드나? **PEB `+0x68` 을 누가 쓰나?**
3. **알림 확인**: PE_emulator 는 "`LdrpPreprocessDllName +0x82`·`+0xc3` 이 `PEB.ApiSetMap` 을 0 검사 없이 읽는다" 고 했다. 맞나?

### 바퀴 전에 알던 것

- `[정적]` (run18·run45) `LdrpInitializeProcess +0x1cd LdrpInitializeNlsInfo(PEB)` 가 PEB `ActiveCodePage` (`+0x34c`)·`OemCodePage` (`+0x34e`) 를 쓰고, 이어서 `NtGetNlsSectionPtr` 로 NLS 섹션을 받는다.
- `[관찰]` (run44 사진) 그 두 칸 = `0x3b5`, **K1 부터 이미** 같은 값. → K1 전에 처음 쓴 쪽은 `[공백]` (`MmCreatePeb` 후보 `[추측]`).
- `[관찰]` (run44) VAD 에 `C_949.NLS`·`l_intl.nls` 가 보인다. 이게 NLS 섹션들일 것 `[추측]` (커널 쪽 본문은 안 읽음).
- `[정적]` (run10·run32) `PspSetupUserProcessAddressSpace +0x255` 에서 `MmMapApiSetView` 를 부른다. 그 안은 안 읽었다.
- `[공백]` (baggage.md) PEB `+0x68 ApiSetMap` 을 **쓰는 줄**.
- `[관찰]` (run44 K1) PEB `+0x68` = `25996cd0000`.

---

## 2. 찾은 것

### 2.1 한눈에 — 세 시간대 트리 (대표만)

run 문서 "호출 관계" 를 줄였다. `+0x..` 는 ret 자리 (콜스택 숫자). 들여쓴 `+0x..` (호출 기호 없음) 는 그 함수 안의 줄.

```
(부팅)
InitBootProcessor
 ├─ +0x111  RtlNlsInitState          커널 NLS 상태 = UTF-8 기본값
 └─ +0x59a  MmInitSystem(0) → +0x149 MiInitSystem(0)
              └─ +0x63d  MiInitializeApiSets         로더 표 → pagefile 섹션, 그대로 복사
Phase1InitializationIoReady
 └─ +0x27  ExInitializeNls
          ├─ +0x2d  ZwCreateDirectoryObject   \NLS
          ├─ +0x96  (커널 사본) RtlpInitCodePageTables  → NtGetNlsSectionPtr(0xb)  만들기
          └─ +0x9b  (커널 사본) RtlpInitUppercaseTables → NtGetNlsSectionPtr(0xe)  만들기

(부모 cmd 스레드 ①)
PspAllocateProcess
 ├─ +0x2314  MmCreatePeb          +0x546 · +0x555 · +0x564  PEB 코드 페이지 세 칸
 └─ +0x239c  PspSetupUserProcessAddressSpace
           └─ +0x255  MmMapApiSetView
                    ├─ +0x80  MmMapViewOfSection   읽기 전용 · 섹션 전체
                    │  +0x92  PEB+0x68 ApiSetMap = 뷰 주소

(test.exe 첫 스레드 ②)
LdrpInitializeProcess
 └─ +0x1cd  LdrpInitializeNlsInfo
          ├─ +0x2f  LdrpFusionManifestCodePages     test.exe: AL = 0
          ├─ +0x42  RtlpQueryNlsSystemCodePages     레지스트리
          ├─ +0x61  RtlpInitCodePageTables(ANSI, OEM)
          │        └─ +0x4b  NtGetNlsSectionPtr(0xb) ══▶ 열기 → 읽기 전용 뷰
          └─ ⇒ +0x6d  RtlpInitUppercaseTables
                   └─ +0x40  NtGetNlsSectionPtr(0xe)

(그 뒤 — kernel32 준비)
LdrpLoadDll +0xc2 → LdrpPreprocessDllName   +0x82 ApiSetMap 읽기 → +0xc3 첫 바이트 (0 검사 없음)
```

- `══▶` 는 syscall, `⇒` 는 꼬리 JMP (run 문서 기호 그대로).
- 전부 `[정적]` 이다. 시간 순서는 코드가 부르는 순서로 읽은 것이고, 실제 순서를 디버거로 본 건 아니다.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — `+0x92  PEB+0x68 ApiSetMap = 뷰 주소`

- **누가**: 커널 `MmMapApiSetView` (rva `0x930e78`, 273B).
- **어디서**: 그 함수 안 `+0x92` 줄. 명령은 `MOV [RCX+0x68],RAX` 다.
  - `RCX` = `+0x86` 에서 `EPROCESS+0x2e0` (= `Peb`) 를 읽어 둔 값 → **새 가게의 PEB**.
  - `RAX` = `+0x8d` 의 값 → 방금 펼친 **뷰 주소**.
- **누구를**: 호출이 아니다. 메모리에 쓰는 줄이다.
- **무엇을 위해**: 새 가게 안내판의 "별명 사전은 여기" 칸 (`+0x68`) 에 사전 위치를 적는다.
- 그 바로 앞 `+0x84 JS` 가 "펼치기 실패면 건너뛰기" 다. 실패하면 칸을 안 쓴다 `[정적]`.

#### 한 줄 읽기 ② — `+0x4b  NtGetNlsSectionPtr(0xb) ══▶ 열기 → 읽기 전용 뷰`

- **누가**: ntdll `RtlpInitCodePageTables` (rva `0xec2fc`, 349B).
- **어디서**: call 은 `+0x46`, 돌아오는 자리 (콜스택 숫자) 는 `+0x4b`.
- **누구를**: ntdll 스텁 `NtGetNlsSectionPtr` → syscall → 커널 `nt!NtGetNlsSectionPtr` (syscall 번호 `0x102`, 인자 5개 중 스택 1개 — run56 번호표).
- **무엇을 위해**: "**코드 페이지 표 (종류 `0xb`), 번호 = ANSI 코드 페이지**" 사전을 달라고 관리사무소에 전화한다.
  - 넷째 인자로 `&GlobalRtlNlsState+0x80` **칸 주소 자체**를 넘긴다. 커널이 뷰 주소를 거기 바로 써 준다 (run 문서 1번 표 "돌려주는 것").
- 비유: 관리실이 전화기로 "949번 사전 주세요, 받으면 서랍 `+0x80` 칸에 놓아 주세요" 라고 하는 장면.

### 2.3 ApiSet ① — 별명 사전은 부팅 때 만들어 둔다 `[정적]`

#### 누가 만드나

- `MiInitializeApiSets` (rva `0xc6920c`, 221B) 다.
- 부르는 길 (부팅 위상 0):
  - `InitBootProcessor +0x595 MmInitSystem(0)` → `MmInitSystem +0x144 MiInitSystem` → **`MiInitSystem +0x638`** (ret `+0x63d`).
  - **새 말 — 부팅 위상** = 부팅을 몇 단계로 나눈 것 중 몇 번째인지. 위상 0 = 아주 이른 단계 `[추측]` (일반적으로 알려진 설명). `MmInitSystem` 은 위상 0·2 가 같은 call 자리를 쓰고, `MiInitSystem` 안에서 위상 0 쪽일 때만 이 함수로 간다 (`+0x75 TEST ECX` / `+0x77 JNZ`).

#### 무엇으로 만드나

- 재료 = **부트 로더**가 넘긴 표. 로더 블록 `Extension` 의 `ApiSetSchema` (`+0xaa0`) 와 그 크기 `ApiSetSchemaSize` (`+0xaa8`).
- 순서:
  1. `+0x52 MmCreateSection` — 크기 = `ApiSetSchemaSize`, 보호 `4` (읽기·쓰기), **파일 없음**.
  2. `+0x73` 커널 쪽 시스템 공간에 뷰 (같은 주소에 이름 2개 — 후보 `MmMapViewInSessionSpace`·`MmMapViewInSystemSpace`).
  3. `+0xac` 메모리 복사 (같은 주소에 이름 4개 — 후보 `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) 로 로더 표를 **그대로** 붙여 넣는다.
- **새 말 — pagefile 섹션** = 뒤에 받치는 **파일이 없는** 섹션. 내용은 메모리에만 있고, 밀려나면 페이징 파일로 간다 `[추측]` (일반적으로 알려진 설명).
  - 비유: 설계도 (파일) 를 보고 짠 세트가 아니라, 관리사무소가 **빈 상자를 하나 만들어 받은 표를 옮겨 적은** 물건.
  - `[관찰]` run44 K1 VAD 도 "Pagefile section" 이라고 적혀 있다 (`ms/logs/04_baggage-read_K1.txt:1373`).
- 다 만든 걸 커널 전역 셋에 둔다: `nt+0xe2d818` (섹션 개체) · `+0xe2d820` (커널 뷰 주소) · `+0xe2d828` (크기). PDB 에 공개 이름이 없다. 이 셋을 쓰는 곳은 `MiInitializeApiSets` 하나뿐이다 (`_refs`).

#### 합치기는 없다

- 보통 (호스트) 쪽 섹션은 로더 표를 **복사만** 한다. 여러 표를 합치는 `ApiSetComposeSchema` 는 여기서 안 부른다.
- 그걸 부르는 곳은 서버 silo 쪽 `PspSiloLoadApiSets +0x4e` 하나다.

#### 그 표가 `apisetschema.dll` 그대로인가 — 아직 아니다

- 근거 → 그래서 → 아직 확인 아님:
  1. 서버의 `apisetschema.dll` `.apiset` = 171104B (버전 6, 개수 984). 쪽으로 세면 42쪽 = `0x2a`쪽.
  2. `[관찰]` run44 K1 의 ApiSet 뷰도 `0x2a`쪽 (`25996cd0 ~ 25996cf9`).
  3. 그래서 쪽 수는 맞는다. 하지만 쪽 수만 같다는 건 **약한 근거**다 `[추측]`.
  4. 로더 (`winload`) 가 넘긴 표가 파일 원본 그대로인지는 정적 범위 밖이다 `[공백]` → 머리말을 직접 대조 (Q110 (2), 4절).

### 2.4 ApiSet ② — 새 가게 벽에 붙이고 안내판에 적는다 `[정적]`

#### 어디서 부르나

- `PspAllocateProcess +0x2397 PspSetupUserProcessAddressSpace` (ret `+0x239c`) → 그 안 `+0x250 MmMapApiSetView` (ret `+0x255`). test.exe 가 타는 길 (`NtCreateUserProcess`) 이다.
- 부르는 자리가 하나 더 있다: `PspAllocateProcess +0x25ba` (ret `+0x25bf`). `PspCreateProcess` (`NtCreateProcessEx` 류) 로 만든 프로세스 길로 보인다 `[추측]`. test.exe 길은 아니다.

#### 어느 섹션을 고르나 — silo 갈림

- **새 말 — silo** (다시 짧게) = 컨테이너 같은 격리 칸. 비유: 상가 안의 **구역** (run32). 구역마다 따로 정한 규칙이 있다.
- `+0x1e PsGetProcessServerSilo` → `+0x29 PsIsHostSilo`:
  - **보통 가게 (silo 없음, test.exe)**: `+0x36` 에서 **위 전역 섹션 `[nt+0xe2d818]`** — 부팅 때 만든 그 별명 사전.
  - 서버 silo 안 가게: `+0x103 PsGetServerSiloGlobals` → `+0x108 [RAX+0x3e8]` — 그 구역 자기 사전 (만드는 쪽 `PspSiloLoadApiSets` 는 이름만 `[추측]`).

#### 어떻게 펼치나 — `+0x7b MmMapViewOfSection` (ret `+0x80`)

| 인자 | 값 |
|---|---|
| 프로세스 | 새 가게 (`+0x78`) |
| 시작 주소 | 0 → **커널이 고름** (`+0x73`) |
| 크기 | 0 → **섹션 전체** (`+0x56`) |
| 상속 | 1 (`+0x52`) |
| 할당 종류 | `0x400000` (`+0x45`) |
| 보호 | **`2` = 읽기 전용** (PAGE_READONLY, `+0x3d`) |

- `[관찰]` run44 K1: PEB `+0x68` = `25996cd0000`, VAD "Mapped READONLY, Pagefile section, shared commit 0x2a" — 위 값 (읽기 전용 · 섹션 전체) 과 맞는다.

#### 안내판에 적기 — `+0x92` (한 줄 읽기 ①)

- **PEB `ApiSetMap` 을 쓰는 줄은 여기 하나다.** 부르는 쪽 `PspSetupUserProcessAddressSpace` 는 PEB 를 안 쓴다 (run32 와 같음).
- **PEB 가 NULL 인지 검사하지 않는다.** 그래서 이 함수는 PEB 를 만든 뒤 (`MmCreatePeb`, `+0x2314`) 에만 불려야 한다. 코드 순서가 그렇다.
- WoW64 (32비트) 가게면 32비트 PEB 칸에도 적는다. test.exe 는 64비트라 안 탄다.

#### 때 (순서)

- `PspSetupUserProcessAddressSpace` 안: `+0x1e6 PspPrepareSystemDllInitBlock` 뒤 → `+0x255` (이것) → `+0x263 PspMapSiloSharedDataView` 앞.
- `[관찰]` run44 K1 에 PEB `+0x68` 이 이미 있다 — K1 은 이 뒤라 맞다.

### 2.5 ApiSet ③ — 관리실이 처음 펴 보는 곳, 0 검사 없음 `[정적]`

#### PE_emulator 말이 맞다

- `LdrpPreprocessDllName` 안:
  - `+0x62` `GS:[0x60]` = PEB 를 읽는다.
  - **`+0x82` `MOV RBX,[RAX+0x68]`** — `PEB.ApiSetMap` 을 읽는다.
  - **`+0xc3` `CMP byte ptr [RBX],7`** — 사전 **첫 바이트 (버전)** 를 읽는다. 7 이면 `+0xfe` (ret `+0x103`) `ApiSetpResolveHost`. 아니면 `+0xc6 JNZ +0x2b6`.
- `+0x82` 와 `+0xc3` 사이에 `RBX` 를 0 과 비교하는 줄이 **없다**.
  - 사이에 있는 `TEST` 는 `PEB.SharedData` (`+0x96`) · `0x7ffe0384` 바이트 (`+0xa2`) · `PEB +0x378` 비트 2 (`+0xb6`) 뿐이다 (결과서).
  - 그래서 → **칸이 0 이면 `+0xc3` 이 주소 0 을 읽는다.**
- 단, 입구 `+0x3e TEST [R12],0x800008` / `+0x46 JNZ +0x366` 이 켜지면 이 길 전체를 건너뛴다. 그 플래그 뜻은 안 정했다.
- PE_emulator 에 주는 뜻: 사전을 안 붙이면 (칸 0) kernel32 준비 때 진짜 Windows 도 그 자리에서 넘어진다. 칸을 꼭 채워야 한다.

#### 버전이 7 이 아니면 — 버전 6 은 자기 문이 따로 있다 `[정적]`

서버 `apisetschema.dll` 표는 **버전 6** 이다 (2.3). 그래서 `+0xc3` 의 "7 인가?" 에서 걸린다. 그다음 길:

1. `+0xc6 JNZ` → **`+0x2b6`** `CMP dword [RBX],6` / `+0x2b9 JZ +0x5a7` — "버전 6 인가?"
2. 6 이면 `+0x5a7` 에서 세 가지를 본다. 다 통과해야 한다:
   - `+0x5a7` 머리 `+0x10` 칸이 8 보다 큰가 (`JBE` 로 걸러냄)
   - `+0x5b1` 머리 `+0x1c` 바이트가 7 인가 (`JNZ`)
   - `+0x5bb` 머리 `+0x1e` 바이트에 비트 4 가 꺼져 있나 (`JNZ`)
3. 다 통과하면 `+0x5cd ADD RBX,0x1c` → `+0x5d1 JMP +0xd4`.
   - 지도 시작을 `+0x1c` 뒤로 옮겨서, **버전 7 길 (`+0xd4`) 에 합류**한다.
   - 비유: 옛 판형 사전이지만, 안쪽 `+0x1c` 쪽부터는 새 판형과 같은 모양이라 **그 쪽부터 펼쳐서** 새 사전처럼 읽는 것.
4. 하나라도 걸리면 (또는 6 도 7 도 아니면) `+0x2bf` → `+0x2c6 ~ +0x302` 에서 이름 앞 8바이트를 `API-`·`EXT-` 와 비교한다.
   - 그 뒤 함수 안 `+0x6ff ApiSetpSearchForApiSet`·`+0x734 ApiSetpSearchForApiSetHost` 호출이 어느 갈래 몫인지는 안 읽었다 `[공백]`.

test.exe 는 어느 쪽인가 (근거 → 그래서 → 아직 확인 아님):

- 서버 파일 머리 `+0x10` = 152 (> 8) → 첫 검사는 통과.
- `+0x1c`·`+0x1e` 바이트는 결과서가 덤프한 머리 범위 밖이라 **모른다**.
- 그래서 → 합류 (`+0xd4`) 하는지 `+0x2bf` 로 가는지 `[추측]` 으로 둔다. 메모리 표의 버전과 이 두 바이트는 Q110 (2) 에서 본다.
- run23 해설이 `LdrpLoadDependentModuleInternal` 쪽 "새 쪽" 조건을 "첫 바이트 7, **또는** 버전 6 이고 `+0x10 > 8`·`+0x1c == 7`·`+0x1e & 4 == 0`" 으로 적었는데, 여기도 같은 세 조건이다.

#### "처음" 은 어디인가 — 후보만

- ntdll 에서 이미 뽑은 함수 362개 안에서 `GS:[0x60]` → `[reg+0x68]` 꼴은 **4곳** (결과서, 더 있을 수 있음):
  - `LdrpPreprocessDllName +0x82`
  - `LdrpLoadDependentModuleInternal +0x278` (run23 의 `+0x2ff ApiSetpResolveHost` 앞)
  - `LdrpSnapKernelBaseExtensions +0x90`
  - `LdrpLogRelativePathWithAlteredSearchError +0x3e` (로그용)
- 코드 순서로 읽으면:
  1. `LdrpInitializeProcess +0x199d LdrpInitializeKernel32Functions` → (KERNEL32 를 못 찾았을 때) `+0xc0 LdrLoadDll` → `LdrpLoadDll +0xc2 LdrpPreprocessDllName` 가 **첫 읽기 후보**다. `[관찰]` run7 에서 이 `LdrLoadDll` 이 잡혔다.
  2. 그 뒤 `LdrpSnapKernelBaseExtensions`, `LdrpLoadDependentModuleInternal`.
  3. `LdrpInitializeImportRedirection` 안 `LdrpLoadDll` 이 더 앞일 수 있지만 조건부이고 test.exe 는 안 탄다 (run45).
- 그래서 → "첫 읽기 = `LdrpPreprocessDllName +0x82`" 는 **후보**다. 실제 실행 순서는 디버거로 봐야 한다 → Q110 (3).

### 2.6 NLS ① — 사전은 창고에 이름표를 달고 미리 있다 `[정적]`

#### 이름표와 원본 파일

| | `0xb` 코드 페이지 | `0xe` 대문자 표 |
|---|---|---|
| 섹션 이름 | `\NLS\NlsSectionCP<번호 10진>` (예 `\NLS\NlsSectionCP949`) | `\NLS\NlsSectionCASE` (고정) |
| 만들 때 원본 파일 | `\SystemRoot\System32\c_%.3d.nls` (예 `c_949.nls`) | `\SystemRoot\System32\l_intl.nls` (고정) |
| 둘째 인자 | 코드 페이지 번호 | 0 |

- 이름은 `RtlpInitNlsSectionName`, 파일 이름은 `RtlpInitNlsFileName` 이 만든다 (오프셋은 run 문서 1번 표).
- `[관찰]` run44 P1a VAD 의 `C_949.NLS`·`l_intl.nls` 가 이 파일 이름 꼴과 같다.

#### "커널 사본" 이라는 말

- **새 말 — 커널 사본** = ntoskrnl 안에도 ntdll 과 **같은 이름**의 함수가 넷 있다 (`RtlInitCodePageTable`·`RtlpInitCodePageTables`·`RtlpInitUppercaseTables`·`RtlpQueryNlsSystemCodePages`). 이름은 같지만 다른 모듈, 다른 오프셋이다.
  - run 문서는 모듈을 안 붙이면 **ntdll 쪽**, 커널 쪽은 "커널 사본" 이라고 붙인다. 이 글도 그렇게 쓴다.
  - 예: `NtGetNlsSectionPtr` 부르는 자리가 커널 사본은 `+0x69`·`+0x91`, ntdll 은 `+0x46`·`+0x6c`.
  - 비유: 관리사무소에도 관리실과 **같은 이름의 업무 수첩**이 따로 있다. 이름이 같다고 같은 수첩이 아니다.

#### 언제 만드나 — 부팅

- `Phase1InitializationIoReady +0x27` → `ExInitializeNls`:
  - `+0x28 ZwCreateDirectoryObject` → `\NLS` 폴더 (영구).
  - `+0x91` 커널 사본 `RtlpInitCodePageTables` → `NtGetNlsSectionPtr(0xb)` (커널 모드로 부름).
  - `+0x96` 커널 사본 `RtlpInitUppercaseTables` → `NtGetNlsSectionPtr(0xe)`.
- `NtGetNlsSectionPtr` 안에는 "**열기 실패 → 파일로 만들기**" 갈래가 있다:
  - `+0x176 JS +0x370` → `+0x3ac RtlpInitNlsFileName` → `+0x41d ZwOpenFile` → `+0x458 ZwCreateSection`.
  - 만든 섹션 모양: 파일 기반 · 크기 = 파일 크기 · 보호 `2` (읽기 전용) · `SEC_COMMIT` · 개체 속성 `0x2d0` (**영구** · 대소문자 무시 · 열려 있으면 열기 · 커널 핸들).
- **새 말 — 영구 (permanent) 개체** = 손잡이 (핸들) 를 다 놓아도 사라지지 않고 이름표를 단 채 남는 커널 물건 `[추측]` (일반적으로 알려진 설명).
  - 비유: 창고 선반에 **이름표를 붙여 둔 원본**. 빌려 간 사람이 다 돌려줘도 선반에서 안 치운다.
- 그래서 → 부팅 때 이 갈래로 첫 섹션들이 생긴다. **아직 없는 번호**가 나중에 요청되면 그때 같은 갈래로 만든다.

### 2.7 NLS ② — 전화를 받으면: 열고, 펼치고, 주소만 준다 `[정적]`

#### 원형 (쓰임으로 붙인 이름)

- `NtGetNlsSectionPtr(종류 ECX, 데이터 EDX, 섹션개체출력 R8, 뷰주소출력 R9, 뷰크기출력 [스택])`.
- 인자 이름은 PDB 가 아니라 **쓰임을 보고 붙인 것**이다 `[추측-이름]`.

#### test.exe (유저) 가 부를 때의 길

1. `+0xfd RtlpInitNlsSectionName` — 이름표 만들기 (2.6 표).
2. `+0x16d ZwOpenSection` (접근 `4`) — **이름으로 열기**. 성공하면 →
3. `+0x1af ObReferenceObjectByHandle` → `+0x1bb ZwClose` — 손잡이로 물건을 붙잡고, 손잡이는 놓는다.
4. `+0x1d7` 에서 부른 쪽이 유저인지 본다 (`KTHREAD.PreviousMode`). 유저면 →
5. **`+0x2e6 MiMapViewOfSection`** (ret `+0x2eb`) — **지금 이 가게**에 펼친다.
   - 공개 함수 `MmMapViewOfSection` 이 아니라 안쪽 `MiMapViewOfSection` 을 곧바로 부른다.

- 만들기 갈래 (2.6) 는 안 탄다 `[추측]` — 부팅 때 이미 있으니까. 실제로 안 타는지는 Q110 (1).
- 커널이 부를 때 (부팅, 커널 사본) 는 유저 뷰 대신 **시스템 공간**에 올린다 (`+0x46e ~ +0x4e3`).

#### 펼치는 모양

| 항목 | 값 | 줄 |
|---|---|---|
| 크기 | 0 = 섹션 전체 | `+0x27d` |
| 보호 | `2` = 읽기 전용 | `+0x25b` |
| 할당 종류 | `0x400000` (이름은 `SEC_NO_CHANGE` 로 보임 `[추측]`) | `+0x250` |
| 정렬 | 64KB | `+0x2ab` |
| 상속 | 1 | |
| 시작 주소 | 0 → 커널이 고름. 위쪽 한계 = `min(EPROCESS.HighestUserAddress − 1, 0x7ffffffeffff)` | `+0x288 ~ +0x2a3` |

- 주소를 아래부터 찾는지 위부터 찾는지는 `MiMapViewOfSection` 본문을 안 봐서 모른다 `[추측]`.
- `[관찰]` run44 P1a: `C_949.NLS` **0x31쪽** (`25996d20 ~ 25996d50`), `l_intl.nls` **3쪽** (`25996d60 ~ 25996d62`), 둘 다 "Mapped READONLY". `C_949.NLS` 는 ApiSet 뷰 `25996cd0` 바로 뒤쪽이다.

#### 돌려주는 것

- `+0x316` → **넷째 칸 (`*R9`) = 뷰 주소**.
- `+0x31e` → 다섯째 칸 = 뷰 크기. 하지만 **ntdll 은 늘 NULL 을 넘겨서 크기는 안 받는다.**
- 셋째 칸 (섹션 개체) 은 커널 호출자용이다. **유저가 NULL 아닌 값을 주면 `0xc00000f1`** (`+0xda` → `+0x4fb`).
- 실패 상태값:
  - `0xc00000ef` — 종류가 `0xb`·`0xc`·`0xe` 가 아님.
  - `0xc000000d` — 넷째·셋째 둘 다 NULL.
  - `0xc00000f1` — 위.
  - 그 밖은 아래 함수들 (`ZwOpenFile`·`ZwCreateSection`·`ObReferenceObjectByHandle`·`MiMapViewOfSection`) 상태 그대로.
- 다른 종류 (이름·번호만): `0xc` → `\NLS\NlsSectionNORM%.8x` (정규화 표). `0xd` 와 그 밖은 `0xc00000ef`.

비유로 정리: 관리사무소는 전화를 받으면 창고에서 **이름표로 사전을 찾아**, 전화한 가게 땅에 **읽기 전용으로** 펼쳐 주고, **"몇 번지에 펼쳤다"** 만 알려 준다. 사전을 빌려 간 기록 (섹션 개체) 은 유저에게 안 준다.

### 2.8 NLS ③ — 관리실은 몇 번 사전을 달라고 하나 (`LdrpInitializeNlsInfo`) `[정적]`

이 부분은 run45 해설 2.10 (4) 의 "값을 고르는 순서" 와 같은 줄기다. 이번엔 **매니페스트 쪽이 왜 비는지**를 끝까지 봤다.

#### 고르는 순서 (오프셋 붙여서)

1. `PEB.ProcessParameters.Flags` 에 `0x8000000` 이 켜져 있으면 → `+0x72 ~ +0x84` 로 곧장. `PEB +0x34c` 에 `0xfde9fde9` (`+0x77`, 두 칸 한 번에) → `RtlpInitCodePageTables(0xfde9, 0xfde9)`.
2. 아니면 `+0x2a LdrpFusionManifestCodePages` → `AL ≠ 0` 이면 그 값으로 (`+0x46`).
3. **`AL = 0` (test.exe)** 이면 `+0x3d RtlpQueryNlsSystemCodePages` (레지스트리).
4. 그것도 음수면 `+0x44 JS +0x86` → 둘 다 `0xfde9`.
5. 고른 두 번호의 낮은 16비트를 `PEB +0x34c` (`+0x4e`) · `+0x34e` (`+0x55`) 에 쓴다 → `+0x5c RtlpInitCodePageTables(ANSI, OEM)` → `+0x61` `PEB +0x350 UseCaseMapping` 읽기 → `+0x6d` 꼬리 JMP `RtlpInitUppercaseTables(UseCaseMapping)`.

- **새 말 — ANSI 코드 페이지 / OEM 코드 페이지** = 가게가 쓰는 사전 번호가 **두 개** 있다. 보통 창 프로그램용 (ANSI) 과 옛 콘솔용 (OEM) 으로 알려져 있다 `[추측]` (일반적으로 알려진 설명). test.exe 사진 값은 둘 다 `0x3b5`.

#### 매니페스트 없는 exe 는 "없음" — 왜 그런지 단계별로

- **새 말 — 매니페스트·활성화 문맥** = exe 에 붙여 넣는 **설정 쪽지** (XML) 와, 그 쪽지를 읽어 만든 **설정 묶음** `[추측]` (일반적으로 알려진 설명). 묶음 위치는 `PEB +0x2f8 ActivationContextData`.
  - 비유: 가게 개업 때 사장이 붙인 **"우리 가게는 이 사전을 쓴다" 메모**. 메모가 없으면 동네 규칙 (레지스트리) 을 따른다.
- `LdrpFusionManifestCodePages(ULONG *ACP, ULONG *OCP)`:
  1. 두 칸을 0 으로 (`+0x1b`·`+0x2e`).
  2. `+0x4e RtlQueryActivationContextApplicationSettings(0, 0, L"http://schemas.microsoft.com/SMI/2019/WindowsSettings", L"activeCodePage", …)` — 메모에서 `activeCodePage` 항목을 찾는다.
  3. 결과가 음수면 `+0x57 CMP [RBX],0` / `+0x5a SETNZ AL` → 칸이 0 이니 **`AL = 0`**.
- 왜 음수가 되나:
  1. 활성화 문맥 0 → `PEB +0x2f8` 이 NULL 이면 **정적 빈 데이터** `RtlpTheEmptyActivationContextData` (머리 `'Actx'`, 크기 `0x20`, 목차 오프셋 0) 를 쓴다.
  2. 목차가 비었으니 `RtlpLocateActivationContextSection +0x58 / +0x5d` 에서 `0xc0150001`.
  3. 바깥에서 `0xc0150008` 로 바꿔 돌려준다 → 음수.
  4. `+0x2f8` 이 있어도 "응용프로그램 설정 섹션" 이 없으면 똑같이 음수.
- 그래서 → test.exe (리소스 디렉터리 0, 매니페스트 없음) 의 코드 페이지는 **레지스트리** (`RtlpQueryNlsSystemCodePages`) 에서 온다 `[정적]`.
- 실제로 `PEB +0x2f8` 이 0 이고 `AL` 이 0 인지는 Q110 (4) 에서 본다.

### 2.9 NLS ④ — 받은 사전을 서랍에 꽂고 목차표를 만든다 `[정적]`

#### 글자 서랍 = `GlobalRtlNlsState`

- ntdll `.data` 전역 (rva `0x1ccf90`), PDB 타입 `_RTL_NLS_STATE` (`0xb8`B).
- 칸 (flow.md 2.4 NLS 절):
  - `+0x0` · `+0x40` — 목차표 둘 (`_CPTABLEINFO`, ANSI · OEM)
  - `+0x80` · `+0x88` — 코드 페이지 사전 뷰 주소 (ANSI · OEM)
  - `+0x90` · `+0x98` — 리드바이트 정보
  - `+0xa0` · `+0xa8` · `+0xb0` — 대소문자 표
- 비유: 관리실 책상의 **글자 서랍**. 칸마다 "사전 놓인 번지" 와 "사전 앞장을 보고 만든 목차표" 가 들어간다.

#### `RtlpInitCodePageTables` 가 채우는 순서

1. `+0x21 CMP SI,BP` / `+0x24 JZ +0x130` — ANSI 가 `0xfde9` 면 **전화를 안 건다**. `+0x80` 칸 그대로 (UTF-8 표).
2. `+0x46` 전화 ① → `+0x80 ActiveCodePageData` = 뷰 (커널이 써 줌, 2.2 한 줄 읽기 ②).
3. `+0x4f CMP DI,SI` / `+0x52 JZ +0x13c` — **ANSI == OEM 이면** 전화를 또 안 걸고 `+0x143` 에서 `+0x80` 값을 `+0x88` 에 복사. 다르면 `+0x6c` 전화 ② → `+0x88 OemCodePageData`.
4. `+0x90 RtlInitCodePageTable(+0x80 값, &서랍+0x0)` · `+0xa3 RtlInitCodePageTable(+0x88 값, &서랍+0x40)` — 목차표 둘 (`DefaultAcpTableInfo`·`DefaultOemTableInfo`).
5. 전역 몇 개: `+0xd6 NlsAnsiCodePage` · `+0xe8 NlsMbCodePageTag` · `+0xef` 서랍 `+0x90 LeadByteInfo` · `+0x10b` 서랍 `+0x98 OemLeadByteInfo` · `+0x11e NlsMbOemCodePageTag`.
6. 두 표 중 하나라도 `CodePage == 0xfde9` 면 (`+0xb2`·`+0xbf JZ +0x14f`) `+0x14f NlsAnsiCodePage = 0xfde9` · `+0x156 NlsMbCodePageTag`.

- **새 말 — 리드바이트** = 한 글자를 두 바이트로 적는 사전에서 "**이 바이트 다음에 한 바이트 더 붙는다**" 를 알리는 첫 바이트 `[추측]` (일반적으로 알려진 설명). 그래서 칸 이름에 `DBCS` (두 바이트 글자) 가 붙는다.

#### `RtlInitCodePageTable` — 사전 앞장을 읽어 목차표를 만드는 단말 함수

- **CALL 도 syscall 도 없다** (단말).
- 표가 NULL 이거나 워드`[1]` = `0xfde9` 면 → 전역 `Utf8TableInfo` (rva `0x1cd050`, 64B) 를 **통째 복사** (`+0x16 ~ +0x41`).
- 아니면 사전 앞장 → `_CPTABLEINFO` 칸 (대표만, 나머지는 run 문서 3절 표):

| 사전에서 읽는 곳 | 목차표 칸 (PDB) |
|---|---|
| 워드`[1]` | `+0x0 CodePage` |
| 워드`[2]` | `+0x2 MaximumCharacterSize` |
| 워드`[0]` = 머리 길이 `h` | `+0x20 MultiByteTable` = 표 + `(h+1)×2` |
| 워드`[h]` | `+0x28 WideCharTable` = 표 + `(h + 워드[h] + 1)×2` |

- 비유: 사전 **앞장 (머리)** 에 "본문은 몇 쪽부터, 거꾸로 찾기는 몇 쪽부터" 가 적혀 있다. 관리실은 그걸 보고 **목차표** 한 장을 만들어 서랍에 둔다. 사전 본문은 복사하지 않고 **번지만** 적는다.
- `+0x30 DBCSRanges` 는 안 쓴다. NLS 파일 내용 자체는 안 봤다 — 서버에 `.nls` 파일이 0개다 (Q104 VM 단계에서 복사).

### 2.10 NLS ⑤ — 대문자 표 (`RtlpInitUppercaseTables`) `[정적]`

- ntdll rva `0xf9804`, 143B. 인자 = `PEB.UseCaseMapping` (`+0x350`).
- `+0xd TEST CX,CX` / `+0x10 JNZ +0x29`:
  - **0 이면** → `+0x12 ~ +0x20` 에서 서랍 `+0xa8`·`+0xb0`·`+0xa0` 세 칸을 0 으로 하고 끝. 전화 안 건다.
  - **0 이 아니면** → `+0x3b` 전화 ③ `NtGetNlsSectionPtr(0xe, 0, 0, &지역, 0)` (ret `+0x40`).
- 받은 뷰를 서랍 세 칸에 나눠 둔다:
  - `+0xa0 CaseMappingData` = 뷰 (`+0x79`)
  - `+0xa8 UnicodeUpcaseTable844` = 뷰 + 4 (`+0x55`)
  - `+0xb0 UnicodeLowercaseTable844` = 뷰 + 4 + 워드`[1]` × 2 (`+0x6d`)
- 코드 페이지 쪽과 다른 점: 여기는 뷰 주소를 **지역 칸** (`[RSP+0x48]`) 으로 받은 뒤 서랍에 옮긴다. 코드 페이지 쪽은 서랍 칸 주소를 커널에 바로 넘겼다.

### 2.11 전화는 몇 번 거나 — "자리 3곳, 실행 2번" `[정적]` + `[추측]`

- `[정적]` ntdll 에서 `NtGetNlsSectionPtr` 를 부르는 **자리**는 3곳이다: `RtlpInitCodePageTables +0x46`·`+0x6c`, `RtlpInitUppercaseTables +0x3b`.
- 실제로 **몇 번 실행되나**는 조건에 달렸다:
  - ANSI == OEM 이면 코드 페이지 쪽은 1번 (`+0x6c` 안 탐).
  - `UseCaseMapping` 이 0 이면 대문자 쪽은 0번.
  - 둘 중 하나라도 `0xfde9` 면 섹션을 안 얻음.
- 왜 test.exe 는 2번이라고 짐작하나 (근거 → 그래서 → 아직 확인 아님):
  1. `[관찰]` 사진 값: ACP = OCP = `0x3b5`, `UseCaseMapping` = `0x6a5`.
  2. ACP == OCP 니까 → 코드 페이지 쪽 1번 (`+0x46`). `UseCaseMapping` ≠ 0 이니까 → 대문자 쪽 1번 (`+0x3b`).
  3. `[관찰]` P1a 에 이름이 맞는 NLS VAD 가 한 개씩 (`C_949.NLS` 1, `l_intl.nls` 1) 이라는 것과도 맞는다.
  4. 하지만 그 VAD 를 이 두 호출이 만들었다는 건 **콜스택을 안 봐서** `[추측]` 이다 → Q110 (1).

### 2.12 PEB 코드 페이지 세 칸 — 관리사무소가 먼저 적는다 `[정적]`

run45 가 남긴 "K1 전에 처음 쓴 쪽은 모름" 의 답이다.

| PEB 칸 | 커널 `MmCreatePeb` 쓰는 줄 | 값 출처 |
|---|---|---|
| `+0x34c ActiveCodePage` | `+0x546` | 커널 NLS 상태 `+0x408` (ANSI 목차표 `CodePage`) |
| `+0x34e OemCodePage` | `+0x555` | 같은 상태 `+0x448` (OEM 목차표 `CodePage`) |
| `+0x350 UseCaseMapping` | `+0x564` | 상태 `+0x4b0` (대문자 표 칸) 이 NULL 이 아니면 **상수 `0x6a5`**, NULL 이면 0 |

- "커널 NLS 상태" = 서버 silo 전역 `+0x408` 의 `_RTL_NLS_STATE`. **ntdll 글자 서랍과 같은 타입**의 관리사무소 쪽 서랍이다.
  - 부팅 때 `InitBootProcessor +0x10c RtlNlsInitState` 가 UTF-8 기본값 `0xfde9` 로 먼저 채우고, 뒤에 `ExInitializeNls` 가 레지스트리 코드 페이지로 채운다.
- `[관찰]` run44 사진 8장 모두 `0x3b5`·`0x3b5`·`0x6a5` — **K1 부터**. 위 표와 맞는다.
- 그래서 → ntdll `LdrpInitializeNlsInfo +0x4e`·`+0x55` 는 **같은 값을 다시 쓰는 것**이다 (같은 레지스트리에서 왔으니까). `+0x350` 은 ntdll 이 안 쓰고 읽기만 한다 (`+0x61`).
- 비유: 관리사무소가 가게를 지을 때 안내판에 "이 동네 글자는 949번" 을 **미리 적어 둔다**. 관리실은 출근해서 동네 규칙을 다시 보고 **같은 번호를 한 번 더** 적는다.

### 2.13 정정 — 숫자 바로잡기

#### PE_emulator 가 적은 자리 (그쪽 문서는 고치지 않음)

- PE_emulator: "`LdrpInitializeNlsInfo +0x46`·`+0x90`·`+0xa3`".
- 실제:
  - `LdrpInitializeNlsInfo` 는 143B (`0x8f`) 라 `+0x90`·`+0xa3` 은 **함수 밖**이다. 그 함수 `+0x46` 은 call 이 아니라 합류점 (`MOV ECX,[RSP+0x30]`).
  - 세 숫자는 **`RtlpInitCodePageTables`** (rva `0xec2fc`) 의 call 자리다.
  - 그리고 `+0x90`·`+0xa3` 이 부르는 건 `NtGetNlsSectionPtr` 가 아니라 **`RtlInitCodePageTable`** 이다.
- WinDbg 콜스택 숫자 (ret 자리) 로는: `+0x4b`·`+0x71` (syscall), `+0x95`·`+0xa8` (`RtlInitCodePageTable`), `RtlpInitUppercaseTables +0x40`.

#### run45 "`NtGetNlsSectionPtr` ×3"

- 틀린 게 아니라 "**부르는 자리 3곳**" 이었다. 실행은 2번으로 보인다 `[추측]` (2.11).
- run45 문서 맨 위에 정정 안내 한 줄, flow.md 2.4 의 "×2"·"×1" 도 "자리" 로 고쳐 썼다 (flow-writer).

#### 결과서 오프셋 다섯 (숫자만 — 다섯째는 갈래 뜻도 바뀜)

| 무엇 | 결과서 | run 문서 (이 글이 쓰는 값) | 까닭 |
|---|---|---|---|
| `NtGetNlsSectionPtr` → `MiMapViewOfSection` call | `+0x2e1` | **`+0x2e6`** (ret `+0x2eb`) | `+0x2e1` 은 call 앞 `MOV` 줄 |
| `RtlpInitCodePageTables` UTF-8 갈래 | `+0x44b` | **`+0x14f`** | 주소 끝자리를 적은 것 |
| `MmMapApiSetView` → `PsGetServerSiloGlobals` | `+0x100` | **`+0x103`** (ret `+0x108`) | call 앞 줄 |
| `ExInitializeNls` → `PsGetCurrentServerSiloGlobals` | `+0x6f` | **`+0x63`** (ret `+0x68`) | |
| `LdrpPreprocessDllName +0xc6 JNZ` 목적지 | `+0x236` ("옛 모양") | **`+0x2b6`** | 목적지 주소 `0x18003e236` 끝자리를 적은 것 (함수 시작 `0x18003df80`, 오케스트레이터 `.asm` 확인). 버전 6 은 자기 갈래 `+0x5a7` 가 있다 (2.5) |

- run56 에 이어 두 번째다. flow-writer 가 "결과서 오프셋은 `.asm` CALL 줄로 한 번 더 맞추자" 는 하네스 제안을 냈다 (사용자 검토 전, 안 고침).

### 2.14 짐 지도에 무엇이 늘었나 (`baggage.md`)

- 3.1 `_PEB`: 줄 3개 더함 — `+0x68 ApiSetMap` (① `MmMapApiSetView +0x92`), `+0x34c`·`+0x34e` (① `MmCreatePeb +0x546`·`+0x555`, 기존 ② ntdll 줄 앞), `+0x350 UseCaseMapping` (① `+0x564`). **모르던 칸 2개** (`ApiSetMap`·`UseCaseMapping`) 가 아는 칸이 됐다.
- 3.8 ntdll `.data`: 줄 9개 더함 — `GlobalRtlNlsState` 6줄 · `NlsAnsiCodePage` · `NlsMbOemCodePageTag` · `NlsMbCodePageTag`. 모르던 전역 4개가 "쓴 자리 아는 것" 이 됐다.
  - `+0x80`·`+0x88` 은 syscall 로 **커널이 써 준다** → 그 전역은 nt·ntdll 양쪽에 센다.
- 4장 짝: `PEB.ApiSetMap` 줄 (쓰는 줄 `[공백]` → `+0x92`) + NLS 짝 두 줄 (뷰 ↔ `\NLS` 섹션, PEB 코드 페이지 ↔ silo 전역 NLS 상태).
- 5장 단계: `Ⓑ` 에 둘 (`ⒷA` 위상 0 ApiSet, `ⒷN` `ExInitializeNls`), ① `MmCreatePeb`·`MmMapApiSetView`, ② NLS 순서.
- 7.6.3 새 절 (체크리스트 밖 — NLS·ApiSet 뷰와 부팅 때 놓인 섹션, C8 일부), 7.18 새 절 (동적 질문).
- 2장 숫자:

| 무엇 | 전 | 후 |
|---|---|---|
| `_PEB` nt / 모름 | 22 / 88 | 26 / 86 |
| `.data` nt / ntdll / 모름 | 1 / 64 / 373 | 2 / 68 / 369 |
| 구조체 아는 칸 / 모름 | 702 / 337 | 704 / 335 |
| 전역 쓴 자리 아는 것 / 모름 | 76 / 417 | 80 / 413 |

### 2.15 비유로 다시 한 번

- 관리사무소는 **문 여는 날** (부팅) 창고에 두 가지를 만들어 둔다.
  - **별명 사전** (ApiSet): 부트 로더가 들고 온 표를 빈 상자에 **그대로 옮겨 적은** 한 부. 선반 번지는 관리사무소 장부 (`nt+0xe2d818`) 에만 있다.
  - **글자 사전** (NLS): `\NLS` 선반에 **이름표** (`NlsSectionCP949`, `NlsSectionCASE`) 를 달고, 책 (`c_949.nls`, `l_intl.nls`) 으로 만든다. 이름표가 붙어 있어 아무도 안 빌려 가도 안 치운다.
- **새 가게를 지을 때** (① 부모 cmd 스레드):
  - 안내판 (PEB) 에 "이 동네 글자는 949번" 을 미리 적고 (`MmCreatePeb`),
  - 별명 사전 사본을 가게 벽에 **읽기 전용으로 펼치고**, 안내판에 "별명 사전은 여기" (`+0x68`) 를 적는다 (`MmMapApiSetView`).
- **관리실이 출근하면** (② test.exe 첫 스레드):
  - 사장 메모 (매니페스트) 가 없으니 동네 규칙 (레지스트리) 을 보고 번호를 다시 적는다.
  - 신청 창구에 전화해 "949번 사전", "대문자 사전" 을 달라고 한다. 관리사무소는 선반에서 이름표로 찾아 가게 땅에 **읽기 전용으로** 펼치고 번지만 알려 준다.
  - 관리실은 그 번지를 **글자 서랍**에 적고, 사전 앞장을 보고 **목차표**를 만든다.
- **kernel32 를 들일 때**: 관리실은 안내판 `+0x68` 의 별명 사전을 **확인 없이** 펼쳐 본다. 칸이 비어 있으면 거기서 넘어진다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것

| 절 | 전 | 후 |
|---|---|---|
| 2.1 `MmCreatePeb` | PEB 코드 페이지 칸 주인 모름 | `+0x546`·`+0x555`·`+0x564` 가 세 칸을 **처음** 씀 `[정적]`, 값 출처 = 커널 NLS 상태 |
| 2.1 `PspSetupUserProcessAddressSpace` | `+0x255 MmMapApiSetView` 이름만 | "`+0x68` 은 이 함수가 아니라 callee `+0x92` 가 씀" 메모 |
| 2.1 **새 절 `MmMapApiSetView`** | 없음 | 부르는 자리 2곳 · silo 갈림 · 읽기 전용 뷰 · **`+0x92` PEB `ApiSetMap`** · 부팅 때 `MiInitializeApiSets`·`ExInitializeNls` `[정적]` |
| 2.1 짝 표 | — | PEB `+0x68` 짝 (쓰는 쪽 `+0x92` ↔ 읽는 쪽 `LdrpPreprocessDllName +0x82` 등), PEB 코드 페이지 짝 (커널이 쓰고 ntdll 이 다시 씀) — 둘 다 "맞음" |
| 2.4 트리 `+0x1cd` | "PEB.ActiveCodePage·OemCodePage, NLS 섹션 (run45)" | "(커널이 먼저 씀, 같은 값), NLS 뷰 → GlobalRtlNlsState (run45, run57)" |
| 2.4 "×2"·"×1" | 호출 횟수처럼 읽힘 | "**부르는 자리 수**" 로 고침. 실행 2번 `[추측]` |
| 2.4 **새 절 NLS 글자표** | 없음 | ntdll 쪽 트리 + 커널 `NtGetNlsSectionPtr` 길 + `[관찰]` VAD 대조 `[정적]` |
| 2.5 `+0xc2 LdrpPreprocessDllName` | `[정적]` 이름만 | `+0x82` ApiSetMap 읽기 · `+0xc3` 첫 바이트 7? · **0 검사 없음** · 버전 6 은 `+0x2b6`/`+0x2b9 JZ +0x5a7` 판정 → `+0xd4` 합류 또는 `+0x2bf` `[정적]` (test.exe 쪽은 `[추측]`) |
| 4장 | run45 "K1 전 첫 주인 → Q78", "`NtGetNlsSectionPtr` 커널 쪽 갈 곳 없음" | 둘 다 "(해결됨, 정적 → run57)". 새 줄 셋: 동적 (1)~(5), 둘째 NLS 뷰 주인 `[공백]`, 정적으로 남은 것 |
| 5장 | — | run57 줄 둘 |

- 흐름도 **그림은 안 바뀌었다** (run 문서 5장 "그림은 안 바뀜").

### 큰 그림에서 어떻게 이어지나

- **PE_emulator 쪽**: 2-I (kernel32·kernelbase) 전에 "공사 중" 셋을 진짜 길로 채울 재료가 생겼다.
  - ApiSet: 부팅 때 만든 한 부를 새 프로세스에 읽기 전용으로 펼치고 PEB `+0x68` 에 적기 (① 단계, 커널).
  - NLS: `NtGetNlsSectionPtr` 종류 `0xb`·`0xe` → 이름표 섹션 열기 → 읽기 전용 뷰 → 뷰 주소만 (② 단계, syscall 하나).
  - ntdll 쪽은 진짜 ntdll 코드가 그대로 돌면 서랍을 채운다. 커널 쪽 (`NtGetNlsSectionPtr`) 이 맞게 대답만 하면 된다.
  - PEB `+0x68` 을 비워 두면 kernel32 준비 때 넘어진다는 것도 확인됐다 (2.5).
- **짐 지도 쪽**: PEB 의 모르던 칸 2개와 ntdll 전역 4개가 "쓴 자리 아는 것" 으로 넘어갔다 (2.14).
- **다음 바퀴**: Q109 (PE_emulator 요청 4-B~E, 정적). 그다음 Q104 VM 단계에서 NLS 파일을 복사해 온다.

---

## 4. 아직 모르는 것

run 문서 "아직 확인 필요" 번호 그대로.

| # | 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|---|
| (1) | test.exe 때 `nt!NtGetNlsSectionPtr` 가 **몇 번** 서나, 콜스택이 `RtlpInitCodePageTables +0x4b` · `RtlpInitUppercaseTables +0x40` 인가 (`+0x71` 은 안 와야 함). 만들기 갈래 (`+0x176 JS`) 를 타나. 돌아올 때 넷째 칸 값·`+0x332` `EBX` | 실행 횟수는 조건에 달림. 정적으로는 "안 탐" 까지만 | 동적 **Q110** (1) — 커널 멈춤점이라 test.exe 조건 필요 |
| (1 덤) | **P1b 부터 보이는 둘째 `C_949.NLS` (`25996eb0`, 0x31쪽) · `l_intl.nls` (`25996dc0`, 3쪽) 뷰는 누가 불렀나** `[공백]` | ntdll NLS 길 (`P1~P1a`) 이 아니다. 정적으로 부른 곳을 못 찾음. `P1a~P1b` 구간이라 KERNELBASE 쪽 `[추측]` | 동적 **Q110** (1) 의 콜스택 |
| (2) | ApiSet 커널 뷰 머리말이 `apisetschema.dll` `.apiset` 머리 (버전 6, 크기 171104, 플래그 0, 개수 984, 항목 오프셋 152, 해시 오프셋 43568, 해시 인자 31) 와 같은가 — 로더가 확장을 합쳤나 | 로더 (`winload`) 는 범위 밖 바이너리. 쪽 수만 맞음 (약한 근거) | 동적 **Q110** (2) — `dq nt+0xe2d818 L3` 로 세 칸 → 커널 뷰 첫 `0x20` 바이트. 같이 머리 **`+0x1c`·`+0x1e` 바이트** (`LdrpPreprocessDllName +0x5b1`·`+0x5bb` 판정 — 버전 6 표가 `+0xd4` 길로 합류하나, 2.5) |
| (3) | `PEB +0x68` 을 **실제로 맨 먼저** 읽는 자리 (`LdrpPreprocessDllName` · `LdrpSnapKernelBaseExtensions` · `LdrpLoadDependentModuleInternal` 중) | 코드 순서는 후보만 줌. `LdrpInitializeKernel32Functions +0xc0` 이 `LdrLoadDll` 을 타는지도 실행 값 | 동적 **Q110** (3) — 또는 test.exe 문맥 `ba r8 <PEB+0x68>` |
| (4) | `LdrpInitializeNlsInfo` 때 `PEB +0x2f8` (예상 0), `+0x2f` 직후 `AL` (예상 0) | 실행 때 값. baggage 7.9 D3 ("매니페스트 `heapType` 조회가 같은 길로 음수로 끝나나") 도 이 값으로 같이 닫힘 | 동적 **Q110** (4) |
| (5) | 전역 값: ntdll `GlobalRtlNlsState` (`dt ntdll!_RTL_NLS_STATE`) · `NlsAnsiCodePage`, 커널 silo 전역 `+0x408` | 정적으론 쓰는 줄만, 값은 실행 때 | 동적 **Q110** (5). P1a 덤프로도 읽을 수 있을 것 `[추측]` (`.data` 는 P1a 부터 읽힘) |
| (결과서 6) | NLS 파일 내용·크기 (`c_949.nls`·`l_intl.nls`·`locale.nls`). 관찰 뷰는 0x31쪽·3쪽 | 서버 `ms/bin` 에 `.nls` 파일이 0개 | **Q104 VM 단계** — VM 에서 복사 |

- 정리: 결과서 1 ~ 5 = **Q110**, 6 = **Q104 VM 단계** (plan.md Q110·Q104 줄, run 문서 "아직 확인 필요").

정적으로 남은 것 (대기열 없음, 필요해지면 — run 문서 그대로):

- 로더 (`winload`) 가 `ApiSetSchema` 에 확장을 합치는지 (범위 밖 바이너리 — 동적 (2) 로 대신).
- `PspAllocateProcess` 길 2 (`+0x2574` 갈래) 의 비트 8·4·1 뜻, `PsCreateMinimalProcess` 가 어느 길인지.
- `MiMapViewOfSection` 본문 (주소를 어느 방향으로 고르나).
- 할당 종류 `0x400000` 의 이름 (`SEC_NO_CHANGE` `[추측]`).
- `LdrpPreprocessDllName` 입구 플래그 `0x800008` 의 뜻.

ApiSet 버전 6 갈래에서 남은 것 (2.5):

- test.exe 의 메모리 표가 `+0x5a7` 세 검사를 통과해 `+0xd4` 새 길에 합류하나, `+0x2bf` (`API-`·`EXT-` 검사) 로 가나 `[추측]`.
  - 왜 못 알았나: 서버 파일 머리 `+0x10` = 152 (> 8) 까지만 알고, `+0x1c`·`+0x1e` 바이트는 결과서 머리 덤프 범위 밖이다. 메모리 표가 파일과 같은지도 아직 모른다 (2.3).
  - 어디서 보나: 동적 **Q110** (2) — 메모리 표 버전과 `+0x1c`·`+0x1e` 바이트.
- 함수 안 `+0x6ff ApiSetpSearchForApiSet`·`+0x734 ApiSetpSearchForApiSetHost` 호출이 어느 갈래 몫인지 `[공백]` (안 읽음, 대기열 없음).

---

## 5. 더 읽기

- run 문서: [`run57-nls-apiset.md`](run57-nls-apiset.md) — 1번 표 (NLS 종류 × 무엇), 2번 표 (ApiSet 한 장), 3절 (`_CPTABLEINFO` 칸 표 전체), 4절 (PEB 세 칸), "정정", "짐 지도 반영"
- 결과서: [`_workspace/sast/52_nls-apiset_result.md`](../../_workspace/sast/52_nls-apiset_result.md) — `NtGetNlsSectionPtr` 인자 구조체 칸, "동적분석으로 확인해야 할 것" 1~6
- 요청서: [`_workspace/sast/52_nls-apiset.md`](../../_workspace/sast/52_nls-apiset.md)
- flow.md: [2.1 `MmMapApiSetView` 절](flow.md) · [2.4 NLS 글자표 절](flow.md) · [2.5 `LdrpPreprocessDllName`](flow.md) · 4장 run57 줄
- baggage.md: [3.1 `_PEB` · 3.8 ntdll `.data` · 7.6.3 · 7.18](baggage.md)
- 앞 해설: [run45](run45-work-list-해설.md) 2.10 (4) (NLS 코드 페이지 두 칸) · [run44](run44-baggage-snap-해설.md) (사진 8장) · [run37](../02_sprint/run37-k32-import-order-해설.md) (별명 사전) · [run38](../02_sprint/run38-map-module-해설.md) (뷰) · [run32](../02_sprint/run32-params-initblock-해설.md) (silo, `PspSetupUserProcessAddressSpace`) · [run23](../02_sprint/run23-load-dependent-해설.md) (`ApiSetpResolveHost`) · [run56](run56-syscall-args-해설.md) (syscall 번호표)

# run119 systemroot-link — 해설

> 이 글은 [run119 문서](run119-systemroot-link.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서·짐 지도(`baggage.md`)를 따른다.

- 대기열: **Q95**: `\SystemRoot` 링크를 만드는 곳 (정적, 작음). run48 (sast/42) 이 남긴 공백 2 (`_workspace/plan.md`, 이번에 **끝**)
  - 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례로 돌았다. **짐 지도 바퀴**다.
  - 남은 것이 간 곳 (4절):
    - 동적 → **Q94** (부팅 뒤 읽기. "끝 한 글자" 맞춰 보기도 여기)
    - 정적 → 새 **Q209 ① ~ ⑤**
- 근거:
  - run 문서 [`run119-systemroot-link.md`](run119-systemroot-link.md) (가장 중요)
  - 요청서 [`_workspace/sast/114_systemroot-link.md`](../../_workspace/sast/114_systemroot-link.md) · 결과서 [`114_systemroot-link_result.md`](../../_workspace/sast/114_systemroot-link_result.md) (판정 **분석성공**, 스크립트 문제 없음)
  - flow.md 변경 (`git diff -- runs/03_sprint/flow.md`): 기준 · 1장 그림 메모 · 2.1 부팅 메모 · **3장 새 절** · 4장 · 5장. **그림은 안 바뀜** (3절)
  - 짐 지도 [`baggage.md`](baggage.md): 쓰는 말 (`ⒷT` · `ⒷT2`) · 3장 줄임 표 둘 · 5장 · **새 7.6.16 (9줄)** · 7.11 · 새 7.50. **2장 숫자 그대로**
- 이번 바퀴는 **정적만** 했다. VM 은 안 돌렸다.
  - 사진 줄 하나만 다시 봤다: run115 때 읽은 KUSD `NtSystemRoot` `"C:\WINDOWS"` (`ms/logs/04_baggage-read_supp_<K1 ~ P2>.txt:190`). 2.12 에서 쓴다.
- **run 문서가 결과서와 다르게 적은 곳이 있다** (2.11). 이 글은 **run 문서**를 따른다.
  - 단계 줄임: 결과서 `ⒷR` · `ⒷR2` → **`ⒷT` · `ⒷT2`**
  - `\ArcName\…` 링크들 줄의 단계: 결과서 `Ⓑ1` → **`Ⓑ`**
- 이번에 [run48 해설](run48-system-dll-boot-해설.md) 맨 위에 "고쳐 읽기 (run119)" 한 줄을 붙였다 (2.4).

---

### 이미 푼 말 (링크만)

- 커널 = **관리사무소**, 프로세스 = **가게**, 핸들 = **손잡이** → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- 부팅 단계 `Ⓑ` = 관리사무소가 **문 여는 날**. `Ⓑ1` = `PsLocateSystemDlls` 구간 → [run48 해설 2.3](run48-system-dll-boot-해설.md)
- 시스템 DLL 표 `PspSystemDlls`, 첫 칸 `\SystemRoot\System32\ntdll.dll` → [run48 해설 2.5](run48-system-dll-boot-해설.md)
- 버그체크 (`KeBugCheckEx`) = 필수 준비가 안 되면 문을 못 연다 → [run48 해설 2.8](run48-system-dll-boot-해설.md)
- **위상** (phase) = 문 여는 날의 **교시** → [run64 해설 2.1](run64-apiset-phase-해설.md)
- `ⒷR` = 문 연 뒤 · 가게 생기기 전 → [run115 해설 2.6](run115-kusd-boot-해설.md)
- 로더 블록 = 부트 로더가 넘겨준 **짐 꾸러미** `[추측]` (일반적으로 알려진 설명) → [run115 해설 2.5](run115-kusd-boot-해설.md)
- KUSD = 모든 가게 앞에 붙은 **공용 게시판**. 그 칸 `NtSystemRoot` → [run115 해설](run115-kusd-boot-해설.md)
- 이름 있는 객체 = **이름표 붙은 공용 사물함**, 디렉터리 = 사물함이 모인 **방** → [run71 해설 2.2](run71-named-obj-dir-해설.md)
- SD = 사물함 · 방마다 붙은 **출입 명부**, 허락 쪽 = DACL → [run97 해설 2.0](run97-rpc-sd-query-해설.md#20-새-말--sd-무결성-라벨-그리고-질문-체크박스)
- 넘긴 명부가 객체에 붙는 길 (`ObpAssignSecurity → SeAssignSecurityEx2`) → [run100 해설](run100-rpc-sd-assign-해설.md)
- 영구 개체 = 손잡이를 다 놓아도 선반에 남는 **이름표 붙은 원본** → [run57 해설 "언제 만드나 — 부팅"](run57-nls-apiset-해설.md#언제-만드나--부팅)
- smss = 부팅 때 가장 먼저 뜨는 유저 프로세스 (관리인) `[추측]` → [run98 해설](run98-rpc-dir-create-해설.md)
- 이름 따라가기, 장치에 닿으면 parse 프로시저에게 넘김 → [run47 해설 2.10](run47-section-chain-해설.md#210-대출-카드-사슬-이름-따라가기와-parse-프로시저)
- silo = 따로 떼어 둔 이름 공간 `[추측]` → [run104 해설](run104-params-block-해설.md)

### 이번 글의 새 말 (본문에서 처음 나올 때 한 줄씩 푼다)

- **객체 이름 공간 · 심볼릭 링크**: 바로 아래 "한 줄로 먼저"
- **ARC 이름**: 2.5
- **`ⒷT` · `ⒷT2`** (단계 줄임): 2.11

### 비유 (그대로 이어 쓴다)

- run98 해설에서 관리사무소 안 **이름 공간 = 동네**, 디렉터리 = 사물함이 모인 **방** 이라고 했다. 그대로 쓴다.
- 이번엔 그 동네에 **안내판** 이 등장한다. run47 해설 2.10 이 "별칭 표지판" 이라고 한 번 스친 그것이다.
- run 문서는 이번에 nt 를 **"관청"** 이라 불렀다. 앞 해설들과 섞이지 않게 이 글에선 **관리사무소** 로 옮긴다 (run115 해설과 같은 처리).
- run 문서의 "길 안내 표지판" 그림은 그대로 가져온다 (2.10).

### 한 줄로 먼저: 무엇을 얻었나

먼저 새 말 둘.

- **새 말: 객체 이름 공간** = 커널이 물건 (디렉터리 · 장치 · 섹션 …) 을 **이름으로 찾게** 해 두는 큰 나무. `\` 로 시작하는 이름이 나무의 길이다.
  - 비유: 관리사무소 안의 **동네** (run98 해설). 방 (디렉터리) 안에 방이 있고, 방 안에 물건이 있다.
  - 이 나무는 에뮬레이터 가상 주소 공간 밖, 커널 쪽에 있다 (짐 지도 "공간 = 커널").
- **새 말: 심볼릭 링크** (객체 이름 공간의) = 이름을 **다른 이름으로 돌려 주는 안내판**.
  - `\SystemRoot` 를 찾으러 오면, 안내판이 "진짜는 이 이름이에요" 하고 다른 이름을 내민다. 찾는 쪽은 그 이름으로 다시 따라간다.
  - 비유: 동네 갈림길의 **안내판**. 안내판엔 물건이 없다. "저쪽으로 가시오" 라는 글만 있다.
  - 안내판 다음에 또 안내판이 나올 수도 있다. 이번 이야기는 그렇게 **안내판이 이어지는 길** 이야기다.

**이번 글의 중심은 이 세 줄이다.** `[정적]`

> **(1) `\SystemRoot` 안내판은 부팅 위상 1 에 `Phase1InitializationDiscard +0xc31` 이 부르는 `CreateSystemRootLink` 가 단다.** 부르는 곳은 이 한 곳뿐이다.

> **(2) `\SystemRoot` 안내판엔 디스크가 안 적혀 있다.** "`\Device\BootDevice` 안내판을 따라가서 `\Windows`" 꼴이다. 디스크는 `\Device\BootDevice` 가 정한다.

> **(3) 나중에 `IopReassignSystemRoot` 가 고쳐 다는 건 `\Device\BootDevice` 하나뿐이다.** `\SystemRoot` 는 그대로다.

| 요청서 질문 | 한 줄 답 (run 문서 그대로) | 이 글 |
|---|---|---|
| 1 만드는 곳 · 대상 글 · 속성 · SD | `CreateSystemRootLink +0x2b4 NtCreateSymbolicLinkObject` (커널 안 직접 호출). 대상 = `\Device\BootDevice` + 로더 블록 `NtBootPathName` (끝 한 글자 뺌). 속성 `0x50` (영구 · 대소문자 무시). SD `SePublicDefaultUnrestrictedSd` | 2.3 · 2.6 · 2.7 |
| 2 시점 · `IopReassignSystemRoot` | `+0xc31` 맞음 (부르는 곳 하나). `PsLocateSystemDlls` 보다 **앞**. reassign 은 `\Device\BootDevice` 만 지우고 새로 만듦 (조건 없음) | 2.4 · 2.9 · 2.10 |
| 3 같이 만드는 것 | 한 함수가 **디렉터리 둘 + 링크 넷** (`\SystemRoot` 포함 여섯 개) | 2.5 |
| 4 짐 지도 줄 | 새 7.6.16, 9줄. 체크리스트 밖이라 **2장 숫자 그대로** | 3절 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 길은 test.exe 가 태어나기 **훨씬 앞**, 관리사무소가 문 여는 날 (부팅) 이다. run98 의 smss 보다도 앞이다 (smss 는 위상 1 뒤에 뜨는 프로세스, flow.md 3장 새 절).

```
(문 여는 날 — 부팅)
 위상 0   InitBootProcessor +0xb94 SeInitSystem → … SepInitSystemDacls     (출입 명부 SePublicDefaultUnrestrictedSd 준비)
 위상 1   Phase1Initialization
  ├─ +0x1e  Phase1InitializationDiscard
  │     ├─ +0xa96 ObInitSystem · +0xadb IoCreateObjectTypes                 (이름 공간 · 개체 종류 — 앞서 끝남)
  │     ├─ +0xc31 CreateSystemRootLink                                      ← 이번 ① (ⒷT)
  │     └─ +0xc65 MmInitSystem(1)                                           (ⒷA, run64)
  ├─ +0x36  IoInitSystem → +0x12 IoInitSystemPreDrivers
  │     ├─ +0xf3b  IopInitializeBootDrivers → +0x7f8 IopCreateArcNames      (\ArcName 방 안쪽 채우기)
  │     ├─ +0x10f5 PsLocateSystemDlls   \SystemRoot\System32\ntdll.dll 열기  (run48)
  │     └─ +0x1139 IopReassignSystemRoot                                    ← 이번 ② (ⒷT2)
  └─ +0x61  Phase1InitializationIoReady  … \SystemRoot\System32\c_%.3d.nls  (NLS, run57 · run117)
 (문 연 뒤) smss → \KnownDlls · \RPC Control 방 (run98)
 …
cmd.exe 의 부탁 → 커널 → ntdll → NtContinue (은퇴) → main                   (지도 범위 — 이번엔 안 바뀜)
```

- test.exe 와 닿는 곳은 **하나**다: 부팅 때 `PsLocateSystemDlls` 가 `\SystemRoot\System32\ntdll.dll` 이름으로 ntdll 을 여는 자리 (run48).
  - test.exe 가게에 붙는 관리실 (ntdll) 세트는 그때 그 이름으로 짠 **한 벌** 이다 (run48 해설).
  - 그 이름이 풀리려면 `\SystemRoot` 안내판이 **먼저** 서 있어야 한다. 그 안내판 이야기다.

### 무엇이 궁금했나

"`\SystemRoot` 안내판은 **누가 · 언제 · 무엇을 적어** 다나? `PsLocateSystemDlls` 보다 정말 먼저인가? 나중에 `IopReassignSystemRoot` 가 그 글을 바꾸나?"

- 요청서는 넷으로 나눴다 (위 "한 줄로 먼저" 표).

### 바퀴 전에 알던 것

- `[정적]` (run48) `PsLocateSystemDlls` 는 `IoInitSystemPreDrivers +0x10f5` 에서 불린다. `IopInitializeBootDrivers` (`+0xf3b`) 뒤, `IopReassignSystemRoot` (`+0x1139`) 앞이다.
- `[정적]` (run48) 표 첫 칸의 경로가 `\SystemRoot\…` 로 시작한다. 그래서 그 전에 `\SystemRoot` 가 있어야 한다 (호출 순서로만 말한 것).
- `[추측]` (run48) 만드는 곳 후보 `Phase1InitializationDiscard +0xc31 CreateSystemRootLink`. **이름만 보고 짐작한 것** 이었다. 본문은 안 읽었다.
- `[관찰]` (run115 때 다시 읽은 run44 사진) KUSD `NtSystemRoot` = `"C:\WINDOWS"`. 같은 로더 블록 `NtBootPathName` 에서 `IopStoreBootDriveLetter +0x136` 이 만든 글이다 `[정적]` (run115).
- `[정적]` (run57 · run117) 부팅 NLS 갈래도 `\SystemRoot\System32\c_%.3d.nls` 를 연다.
- 에뮬레이터 쪽 (오케스트레이터가 준 줄, 우리 관찰 아님): `namespace.cpp:31` 에 `\SystemRoot → \Device\BootDevice\Windows` 가 이미 있다. **모양은 있는데 근거는 없던** 상태였다.

---

## 2. 찾은 것

### 2.1 한눈에: 안내판 여섯 개 달기, 그리고 나중에 하나 갈아 달기

run 문서 "호출 관계" 를 대표만 줄였다. 오프셋은 **call 자리** 다 (부팅 길이라 콜스택으로 본 적 없음. ret 는 run 문서 "정적 — 호출 지점" 표).

```
Phase1Initialization                                                         [정적]
 ├─ (1) +0x1e  Phase1InitializationDiscard (RSI = 로더 블록)
 │        └─ +0xc31  CreateSystemRootLink                           ← ⒷT
 │              ├─ +0xac   NtCreateDirectoryObject      \ArcName
 │              ├─ +0x11e  NtCreateDirectoryObject      \Device
 │              ├─ +0x1e1  NtCreateSymbolicLinkObject   \Device\BootDevice   → \ArcName\<ArcBootDeviceName>
 │              ├─ +0x2b4  NtCreateSymbolicLinkObject   \SystemRoot          → \Device\BootDevice<NtBootPathName − 끝 한 글자>
 │              ├─ +0x37d  NtCreateSymbolicLinkObject   \Device\OSDataDevice → \ArcName\<ArcOSDataDeviceName (0 이면 ArcBootDeviceName)>
 │              └─ +0x406  NtCreateSymbolicLinkObject   \OSDataRoot          → \Device\OSDataDevice
 └─ (2) +0x36  IoInitSystem ─ +0x12 IoInitSystemPreDrivers
          ├─ +0xf3b   IopInitializeBootDrivers ─ +0x7f8 IopCreateArcNames     \ArcName\… 안쪽 (이름만)
          ├─ +0x10f5  PsLocateSystemDlls                                     (run48 — ntdll 열기)
          └─ +0x1139  IopReassignSystemRoot                          ← ⒷT2
                ├─ +0xbd ⟲ +0xdf  NtOpenSymbolicLinkObject · NtQuerySymbolicLinkObject   안내판 따라가기
                ├─ +0x189         NtMakeTemporaryObject                                  옛 \Device\BootDevice 떼기
                └─ +0x1d6         NtCreateSymbolicLinkObject                             새 \Device\BootDevice → \Device\<장치>
```

- 위 줄 (1) 이 **다 돌아온 뒤에** 줄 (2) 가 시작된다. 이게 2.9 "왜 늘 먼저인가" 의 뿌리다.

### 2.2 커널 안 "직접 호출": 창구를 안 거친다

- `CreateSystemRootLink` 는 `NtCreateSymbolicLinkObject` 를 **커널 안에서 직접** 부른다. 유저가 syscall 로 들어오는 길이 아니다 `[정적]`.
  - 비유: 보통 가게 (유저) 는 관리사무소 **창구** (syscall) 에 와서 "안내판 하나 달아 주세요" 한다. 이번엔 관리사무소 **직원이 자기 책상에서** 같은 서식을 바로 처리한다.
  - run98 의 smss 는 유저 쪽 관리인이라 창구를 거쳤다. 이번은 관리사무소 자신이다.
- 그 함수는 다시 `+0x196 ObCreateSymbolicLink` 를 부르고, 그 안에서 `+0x89 ObCreateObjectEx` · `+0x191 ObInsertObjectEx` 를 부른다 `[정적]`. 공용 부품이라 **이름만** 둔다.

### 2.3 한 줄씩 읽는 법

#### 한 줄 읽기 ①: 안내판을 다는 일이 시작되는 줄

`Phase1InitializationDiscard +0xc31 → CreateSystemRootLink` (ret `+0xc36`) `[정적]`

- **누가**: `Phase1InitializationDiscard`. 위상 1 본체 (`Phase1Initialization +0x1e` 가 부름).
- **어디서**: `+0xc31` 이 call 자리, 돌아올 자리는 `+0xc36`.
- **누구를**: `CreateSystemRootLink`.
- **무엇을 위해**: 이름 공간에 방 둘 · 안내판 넷을 단다.
- **인자**: 로더 블록. 함수 맨 앞 `+0x38 MOV RSI,RCX` 로 받아 둔 것을 `+0xc2e MOV RCX,RSI` 로 그대로 넘긴다.
  - 비유: 부트 로더가 넘겨준 짐 꾸러미를 **통째로** 들고 가서, 거기 적힌 주소들을 보고 안내판 글을 쓴다.

#### 한 줄 읽기 ②: `\SystemRoot` 안내판을 실제로 다는 줄

`CreateSystemRootLink +0x2b4 → NtCreateSymbolicLinkObject` (ret `+0x2b9`) `[정적]`

- **누가**: `CreateSystemRootLink`.
- **어디서**: `+0x2b4`.
- **누구를**: `NtCreateSymbolicLinkObject` (커널 안 직접 호출, 2.2).
- **무엇을 위해**: 이름 `\SystemRoot`, 대상 글 = 바로 앞에서 지은 글 (2.6).
- 그 앞 준비 세 줄:
  - `+0x218` `RtlInitUnicodeString`: 안내판 이름 `\SystemRoot`
  - `+0x266` `RtlStringCbPrintfW`: 대상 글 짓기
  - `+0x291` `RtlInitUnicodeString` → `+0x2a0` 길이 −2: 대상 글 마무리

#### 한 줄 읽기 ③: 나중에 가운데 안내판을 새로 다는 줄

`IopReassignSystemRoot +0x1d6 → NtCreateSymbolicLinkObject` (ret `+0x1db`) `[정적]`

- **누가**: `IopReassignSystemRoot` (`IoInitSystemPreDrivers +0x1139` 가 부름).
- **어디서**: `+0x1d6`.
- **누구를**: 같은 `NtCreateSymbolicLinkObject`.
- **무엇을 위해**: **같은 이름** `\Device\BootDevice` 로 안내판을 **새로** 단다. 대상 = 안내판 길을 끝까지 걸어 닿은 장치 이름 (`+0x1af LEA R9,[RSP+0x28]`).
- 바로 뒤 `+0x1e2` 손잡이 닫기 → `+0x1e7 MOV AL,1` (성공).

### 2.4 부르는 곳은 한 곳뿐: run48 의 짐작이 맞았다

- 결과서가 `CreateSystemRootLink` 를 가리키는 곳을 전부 찾았다 (`_refs/CreateSystemRootLink.tsv`) `[정적]`:
  - 코드 호출: `Phase1InitializationDiscard +0xc31` **1줄**
  - 나머지 1줄: `.pdata` (함수 밖 자료)
  - 함수 포인터 표 속 값: **0곳**
- 그래서 → "다른 길로 몰래 불리는 곳" 도 없다. 이 한 자리가 전부다.
- run48 의 후보 `[추측]` (이름으로만 본 것) → **`[정적]`** (만드는 줄로 확인).
  - 틀린 게 아니라 **근거가 생긴** 것이다. 그래서 정정이 아니라 태그 올림이다 (run 문서 "정정").
  - [run48 해설](run48-system-dll-boot-해설.md) 2.4 "덤" 의 그 `[추측]` 줄은 본문을 그대로 두고, 맨 위에 "고쳐 읽기 (run119)" 한 줄을 붙였다.

### 2.5 한 함수가 여섯 개를: 방 둘, 안내판 넷

먼저 새 말 하나. 표에 바로 나온다.

- **새 말: ARC 이름** = 부팅 로더가 디스크를 부르는 **옛 꼴 이름** `[추측]` (일반적으로 알려진 설명). 결과서는 `\ArcName` 안 항목의 예로 `multi(0)disk(0)rdisk(0)partition(N)` 꼴을 들었다 `[추측: 동적 3 에서 확인]`.
  - 비유: 동네 사람들이 쓰는 지금 주소가 아니라, **부트 로더가 들고 온 옛 지도** 의 주소. 관리사무소는 그 옛 주소를 모아 둘 방 `\ArcName` 을 따로 만든다.
  - 로더 블록 칸 `+0xb8 ArcBootDeviceName` · `+0x150 ArcOSDataDeviceName` 이 그 옛 주소다 (칸 이름은 PDB `_LOADER_PARAMETER_BLOCK`, 결과서 `pdb_struct.py`).

`CreateSystemRootLink` 가 이 순서로 만든다 (run 문서 표 1) `[정적]`:

| # | 종류 | 이름 | 대상 글 (안내판에 적힌 것) | 만드는 call | 실패 단계 |
|---|---|---|---|---|---|
| 1 | 방 (디렉터리) | `\ArcName` | (비어 있다가 나중에 `IopCreateArcNames` 가 채움) | `+0xac` | 1 |
| 2 | 방 (디렉터리) | `\Device` | | `+0x11e` | 2 |
| 3 | 안내판 | `\Device\BootDevice` | `\ArcName\` + `ArcBootDeviceName` | `+0x1e1` | 3 · 4 |
| 4 | 안내판 | **`\SystemRoot`** | `\Device\BootDevice` + `NtBootPathName` **끝 한 글자 뺌** | `+0x2b4` | 5 · 6 |
| 5 | 안내판 | `\Device\OSDataDevice` | `\ArcName\` + `ArcOSDataDeviceName` (0 이면 `ArcBootDeviceName`) | `+0x37d` | 7 · 8 |
| 6 | 안내판 | `\OSDataRoot` | `\Device\OSDataDevice` | `+0x406` | 9 |

- 왜 이 순서인가 `[정적]`: `\Device\BootDevice` 는 `\Device` 방 **안에** 서는 안내판이다. 방이 먼저 있어야 한다 (2 → 3).
- 안내판 길을 그림으로:
  - `\SystemRoot` → `\Device\BootDevice` → `\ArcName\<옛 주소>` → … → 진짜 디스크
  - 그러니 `\SystemRoot` 는 **디스크를 직접 모른다.** "가운데 안내판 따라가서 `\Windows`" 까지만 안다.
- 5번의 "0 이면 갈아끼움" `[정적]`: `+0x31f TEST R9,R9` → `+0x322 JNZ` → (비었으면) `+0x324 MOV R9,[RBX+0xb8]`. OS 데이터 장치 옛 주소가 따로 없으면 부팅 장치 옛 주소를 쓴다.
- 결과서가 덧붙인 것:
  - 여섯 이름 글은 INIT 구역 rva `0xc769e0` · `0xc76a00` · `0xc76a10` · `0xc76a60` · `0xc76a90` · `0xc76ac0` 에 있다 (파일 바이트로 확인).
  - `\Device\OSDataDevice` 글은 nt 파일 전체에서 **이 한 곳뿐**이다 (스캔).
- 이 함수가 부르는 건 7가지뿐이다: `RtlInitUnicodeString` · `NtCreateDirectoryObject` · `NtCreateSymbolicLinkObject` · `NtClose` · `RtlStringCbPrintfW` · `KeBugCheckEx` · `__security_check_cookie`. `IoAssignArcName` · `IoCreateSymbolicLink` 는 **안 부른다** `[정적]`.
  - 그래서 `\ArcName` **방 안의 옛 주소 안내판들** 은 여기서 안 만든다. 방만 만든다.

### 2.6 `\SystemRoot` 대상 글 짓기: 끝 한 글자를 왜 빼나

#### 글 짓기 `[정적]`

1. `+0x266 RtlStringCbPrintfW` 가 `"%s%S"` 모양으로 글을 짓는다 (상수 `0x140c76a80`, 파일 바이트로 확인).
2. 앞 `%s` (넓은 글자) = `\Device\BootDevice` (`+0x22a LEA R9,[0x140c76a10]`).
3. 뒤 `%S` (ANSI 글자) = 로더 블록 `+0xc8 NtBootPathName` (`+0x248 MOV RAX,[RBX+0xc8]`, `+0x252` 스택 5번째 인자).
4. 다 지은 글의 길이에서 **2바이트** 를 뺀다: `+0x296 MOV EAX,0xfffe` → `+0x2a0 ADD word ptr [RSP+0x68],AX`.
   - 넓은 글자 하나 = 2바이트라서, 2바이트 = **글자 하나** 다.

#### 왜 그렇게 짐작하나 (근거 → 그래서 → 아직 확인 아님)

- 근거: 코드는 **아무 확인 없이** 맨 끝 한 글자를 잘라낸다 `[정적]`.
- 그래서: `NtBootPathName` 이 `\Windows\` 처럼 **`\` 로 끝난다고 믿고** 짠 코드로 읽힌다 `[추측: 뜻]`. 끝 `\` 를 떼면 `\Device\BootDevice\Windows` 가 된다.
- 아직 확인 아님: 실제 `NtBootPathName` 값은 정적으로 모른다. 결과도 **예상** `\Device\BootDevice\Windows` `[추측]` 이다. 부팅 뒤 `!object \SystemRoot` 한 번이면 닫힌다 (Q94, 2.12).

#### 이 글이 디스크를 정하지 않는다는 것

- 대상 글 어디에도 디스크 번호가 없다. 디스크는 앞 `\Device\BootDevice` 안내판이 정한다 `[정적]`.
- 비유: "윈도우 폴더 가는 길" 안내판엔 **"부팅 장치 안내판 따라가서 `\Windows`"** 라고만 적혀 있다.

### 2.7 안내판에 붙는 것: 속성 · 권한 · 출입 명부

#### 속성 `0x50`: 영구 + 대소문자 무시 `[정적]`

- 여섯 개 모두 `OBJECT_ATTRIBUTES` 의 `Attributes = 0x50`.
  - `0x10` = 영구 (`OBJ_PERMANENT`): 손잡이를 다 놓아도 선반에 남는 원본 ([run57 해설](run57-nls-apiset-해설.md#언제-만드나--부팅)).
  - `0x40` = 대소문자 무시: 이름을 맞춰 볼 때 큰 글자 · 작은 글자를 안 가린다는 뜻 `[추측]` (일반적으로 알려진 설명).
  - `0x200` (커널 핸들) 은 **없다.**
- 근거: 첫째 `+0x42 MOV [RSP+0x50],0x50`. 그 뒤 `+0xa8 LEA R14D,[RDI+0x50]` 값을 `+0x114` · `+0x193` · `+0x25c` · `+0x31a` · `+0x3e3` 에서 다시 쓴다.
- 만든 뒤 손잡이는 곧장 `NtClose` 로 놓는다. 영구라서 이름은 남는다.
  - 비유: 안내판을 박아 놓고 망치는 내려놓는다. 안내판은 그대로 서 있다.
- 조심할 점 둘 (run 문서 그대로):
  - 대소문자 무시는 **만들 때 넘긴 값** 이다. 개체에 따로 남는 표시인지는 안 읽었다.
  - 영구 표시는 `_OBJECT_HEADER +0x1b Flags` 비트 4 `PermanentObject` 다 (PDB). 실제로 켜졌는지는 동적 (Q94).

#### 권한 `[정적]`

- 방 `0xf000f`, 안내판 `0xf0001` (결과서).

#### 출입 명부: `SePublicDefaultUnrestrictedSd` `[정적]`

- 여섯 개 모두 같은 **전역 명부** 를 붙여 달라고 넘긴다. 전역 포인터를 `+0x8a` · `+0xf9` · `+0x171` · `+0x231` · `+0x300` · `+0x3cd` 에서 읽는다.
- 그 명부는 **위상 0** 에 이미 채워져 있다:
  - `InitBootProcessor +0xb94 SeInitSystem` → `+0x33 SepInitializationPhase0` → `+0x6 SepVariableInitialization` → `+0x1923 SepInitSystemDacls` → `+0x683` 에서 씀.
  - 위상 1 의 `+0xc31` 보다 **훨씬 앞** 이다. 빈 명부를 붙일 일은 없다.
- 명부 모양 (결과서, `SepInitSystemDacls.c` 요약):
  - 허락 쪽 (DACL) 만 있다. 주인 · 소속 · 특수 쪽 (SACL) 은 없다.
  - 허용 넷: Everyone `GENERIC_EXECUTE` · Restricted `GENERIC_EXECUTE` · LocalSystem `GENERIC_ALL` · Administrators `GENERIC_ALL`.
  - 비유 (run 문서): "누구나 **지나갈** 수는 있고, **고치는** 건 관리인 · 관리자만".
- 위상 1 의 `SeInitSystem` (`Discard +0xbdf`) 은 다른 갈래다. `InitializationPhase = 1` 이라 `SepInitializationPhase1` 쪽으로 간다 (`SeInitSystem +0x10 CMP EAX,1` → `+0x2c`).
- 아직 모르는 것: 넘긴 명부가 개체에 **실제로 붙는 줄** (`ObCreateObjectEx` · `ObInsertObjectEx` 안) 은 안 읽었다 `[공백, 작음]`. run100 의 `ObpAssignSecurity → SeAssignSecurityEx2` 길과 같을 것 `[추측]`.
  - run98 해설 "조심할 곳 3" 과 같은 이야기다. **넘긴 명부** 와 **붙은 명부** 는 다를 수 있다.

### 2.8 하나라도 실패하면: 버그체크 `0x64`

- 단계 1 ~ 9 마다 실패하면 `KeBugCheckEx(0x64, status, 단계 번호, 0)` `[정적]`.
  - 호출 줄 9개: `+0xc7` · `+0x13a` · `+0x1b9` · `+0x1fd` · `+0x282` · `+0x2d0` · `+0x355` · `+0x399` · `+0x422`.
  - 2.5 표의 "실패 단계" 칸이 이 번호다. 링크마다 "글 짓기" 와 "만들기" 가 따로 번호를 가져서 링크 셋은 번호가 둘씩이다.
- 정상이면 `+0x432 XOR EAX,EAX` 로 0 을 돌려준다.
- 그래서 → 호출자 쪽 `Discard +0xc36 TEST EAX,EAX` 뒤의 실패 갈래 (`+0xc4c KeBugCheckEx`, 첫 인자 `0x64`) 는 **닿지 않는 코드** 로 보인다 `[추측: 반환값이 0 말고 있는 줄 못 찾음]`.
- 하나 더 읽히는 것: 같은 이름이 이미 있으면 만들기가 실패하고 버그체크다. 그러니 `\Device` · `\ArcName` 방은 **여기서 처음 생긴다** 고 읽힌다 `[추측: 충돌 반환값은 안 읽음]`.
  - 비유: 문 여는 날 "부팅 장치 안내판" 하나 못 달면 그날 문을 못 연다. 그만큼 필수 준비라는 뜻이다.

### 2.9 순서: 왜 ntdll 을 열 때 `\SystemRoot` 는 늘 서 있나

#### 단계별로 `[정적, 호출 순서]`

1. `Phase1Initialization` 이 `+0x1e` 에서 `Phase1InitializationDiscard` 를 부른다.
2. Discard 안 `+0xc31` 에서 안내판들을 단다 (`ⒷT`).
3. Discard 가 **다 돌아온 뒤** 에야 `Phase1Initialization +0x36` 이 `IoInitSystem` 을 부른다.
4. `IoInitSystem +0x12` → `IoInitSystemPreDrivers` → `+0x10f5 PsLocateSystemDlls` (부르는 곳 한 곳, sast/42 와 같음).
5. 그래서 → `PspLocateSystemDll` 의 `ZwOpenFile` 이 `\SystemRoot\System32\ntdll.dll` 을 열 때, `\SystemRoot` 는 **늘** 있다.

- 그 앞 Discard 안에서 `ObInitSystem` (`+0xa96`) · `IoCreateObjectTypes` (`+0xadb`) 가 이미 끝났다. 안내판 · 방 개체 종류가 이미 있다는 뜻으로 읽힌다 `[추측: 개체 종류 만드는 줄은 안 봄]`.

#### ntdll 을 여는 그 순간의 안내판 길 `[정적, 호출 순서 + 링크 대상 글]`

- `IopCreateArcNames` 는 `IoInitSystemPreDrivers +0xf3b` (`IopInitializeBootDrivers`) 안 `+0x7f8` 에서 불린다. 그 앞 `+0x7d3 PnpWaitForDevicesToStart`.
- `+0xf3b` 는 `+0x10f5` (`PsLocateSystemDlls`) 보다 앞이다. 그래서 ntdll 을 열 때는 `\ArcName` 방 안쪽도 이미 채워져 있다.
- 그 순간의 길:
  - `\SystemRoot` → `\Device\BootDevice` → `\ArcName\<ArcBootDeviceName>` → (`IopCreateArcNames` 가 단 안내판) → 장치
- 가운데 몇 단계인지, 이름이 뭔지는 동적이다 (Q94).

#### 다른 `\SystemRoot` 손님도 뒤다

- 부팅 NLS 갈래 (`NtGetNlsSectionPtr` → `\SystemRoot\System32\c_%.3d.nls`, run57 · run117) 도 `Phase1Initialization +0x61` 쪽이라 `+0x1e` 보다 뒤다 (run 문서 보탬).

### 2.10 `IopReassignSystemRoot`: 길을 끝까지 걸어 보고, 가운데 안내판만 갈아 단다

여기가 이번 글에서 단계가 가장 많다. 천천히 간다.

#### 먼저 비유로 (run 문서 그림 그대로)

- `\Device\BootDevice` 안내판은 처음엔 **"`\ArcName\…` 안내판으로 가시오"** (옛 지도 주소) 다.
- 디스크 장치가 다 준비된 뒤, 관리사무소가 그 길을 **끝까지 걸어가 본다.**
- 진짜 건물 (장치) 에 닿으면, `\Device\BootDevice` 안내판을 **떼고** "그 건물로 바로 가시오" 로 **새로 단다.**
- `\SystemRoot` 안내판은 그대로다. 가운데 안내판 하나만 바뀌어서 길이 한 칸 짧아진다.

#### 단계별로 `[정적]` (`IopReassignSystemRoot.asm`)

1. **출발점 글 짓기**: `+0x28 MOV R9,[RCX+0xb8]` → `+0x61 RtlStringCchPrintfW` 로 `\ArcName\<ArcBootDeviceName>`.
2. **안내판 따라가기 고리** (`+0x86` 부터):
   - `+0xbd NtOpenSymbolicLinkObject` 로 그 이름을 **안내판으로** 열어 본다 (속성 `0x240` = 대소문자 무시 + 커널 핸들).
   - 열리면 `+0xdf NtQuerySymbolicLinkObject` 로 안내판 글을 **같은 칸에** 읽는다.
   - `+0xed` 손잡이 닫기 → `+0xf4 JNS` 로 `+0x86` 에 돌아가, 방금 읽은 글을 **또 안내판으로** 열어 본다.
3. **장치에 닿았나**: 열기가 `0xc0000024` 로 실패하면 멈춘다 (`+0xc2 CMP` · `+0xc7 JZ` → `+0x11b`).
   - `0xc0000024` = `STATUS_OBJECT_TYPE_MISMATCH`. "그건 안내판 종류가 아니에요" 라는 답이다.
   - 그래서 → **안내판이 아닌 물건 (장치)** 에 닿았다는 뜻이다. 고리의 끝이다.
4. **이름 베껴 두기**: `+0x126 RtlUnicodeStringToAnsiString` 으로 그 장치 이름을 호출자가 준 `ANSI_STRING` 에 복사한다.
5. **옛 안내판 떼기**: `+0x177` 로 `\Device\BootDevice` 를 열고 → `+0x189 NtMakeTemporaryObject` → `+0x195` 손잡이 닫기.
   - run 문서는 이 두 줄을 "임시로 바꿔 닫기 = 지우기" 로 읽었다 `[정적]`.
   - 왜 이게 "떼기" 인가: 영구 표시를 **임시** 로 바꾼 뒤 마지막 손잡이를 놓으면, 선반에 남을 이유가 없어져 사라진다 `[추측]` (일반적으로 알려진 설명).
6. **새 안내판 달기**: `+0x1d6 NtCreateSymbolicLinkObject` 로 **같은 이름** 을 새로 만든다. 대상 = 3번에서 닿은 장치 이름.
7. `+0x1e2` 손잡이 닫기 → `+0x1e7 MOV AL,1` (성공).

#### 새 안내판은 처음 것과 다르다 `[정적]`

| | 처음 (`ⒷT`, `CreateSystemRootLink +0x1e1`) | 새로 단 것 (`ⒷT2`, `+0x1d6`) |
|---|---|---|
| 대상 | `\ArcName\<ArcBootDeviceName>` | `\Device\<장치>` (기대 `\Device\HarddiskVolumeN`, 동적) |
| 속성 | `0x50` (영구 · 대소문자 무시) | `0x250` (영구 · 대소문자 무시 · **커널 핸들**) |
| 명부 | `SePublicDefaultUnrestrictedSd` | **SD 칸 0** (`+0x1d0`) — 전역 명부를 안 준다 |

- SD 칸이 0 이면 어떤 기본 명부가 붙는지는 모른다 (기본 SD 계산 안 읽음) `[공백]`.

#### 조건이 없다 `[정적]`

- `IoInitSystemPreDrivers +0x1105 JNS` (`PsLocateSystemDlls` 성공) 뒤 **직선** 으로 온다.
- VHD · BitLocker 를 따로 보는 줄은 이 함수에 **없다.** VHD 확인은 `IopCreateArcName +0x489 VhdVerifyBootDisk` 쪽이다 (결과서).
- 갈림은 "안내판 길이 어떻게 이어지나" 뿐이다.
- 그래서 이 VM 에서도 부팅이 위상 1 을 지나면 탄다 `[추측: 실행 확인 없음]`. 끝까지 성공했는지는 부팅 뒤 `\Device\BootDevice` 대상 글 하나로 가린다 (Q94).

#### 실패하면 `[추측]`

- 열기가 다른 음수로 실패 · 변환 실패 · `\Device\BootDevice` 열기 실패 → `+0xf6 XOR AL,AL` (0).
- → `IoInitSystemPreDrivers +0x1140 JNZ` 안 뜀 → `+0x114c IopInitFailCode = 9`.
- → `IoInitSystem` 실패 → `Phase1Initialization` 쪽 버그체크 `0x69` 로 이어지는 것으로 **보인다** `[추측: 도착 뒤 줄은 안 읽음]`.

#### 남는 것 둘, 안 바뀌는 것 둘 `[정적]`

- 남는 것:
  - 새 `\Device\BootDevice` 안내판.
  - 장치 이름 ANSI 사본. 버퍼는 전역 `IoInitSystem_deviceNameBuffer` (256B, `IoInitSystemPreDrivers +0x4b` · `+0x56` 에서 연결). 이 버퍼를 읽는 다른 코드는 `refs` 에 없다.
- 안 바뀌는 것:
  - `\SystemRoot`. 이 함수는 손도 안 댄다.
  - `\Device\OSDataDevice`. `\ArcName\…` 꼴 대상 그대로 `[정적: 이 함수만 봄 — 다른 곳이 바꾸는지는 안 봄]`.
- 그래서 → test.exe 가 뜰 때 (`ⒷR` 뒤 · ①) 의 길은 **새로 단 뒤 모양** 이다: `\SystemRoot` → `\Device\BootDevice` → `\Device\<장치>` (짐 지도 7.6.16).

### 2.11 단계 이름: `ⒷT` · `ⒷT2` (결과서와 다르게 적은 곳)

- **새 말: `ⒷT` · `ⒷT2`** (짐 지도 "쓰는 말"):
  - **`ⒷT`** = 위상 1 `Phase1InitializationDiscard +0xc31 CreateSystemRootLink`. 방 둘 · 안내판 넷을 다는 때. T 는 링크가 가리키는 **대상 글** (target) 에서 땄다.
  - **`ⒷT2`** = `IoInitSystemPreDrivers +0x1139 IopReassignSystemRoot`. `\Device\BootDevice` 를 다시 다는 때. `Ⓑ1` 의 `PsLocateSystemDlls` **뒤**.
  - 비유: 문 여는 날 1교시 중간에 안내판을 달고 (`ⒷT`), 같은 날 장치가 다 준비된 뒤 가운데 안내판 하나를 갈아 단다 (`ⒷT2`).
- 줄임 사슬 (짐 지도 3장, 그대로 옮김):
  - `ⒷT…` = `ⒷP… → CreateSystemRootLink (호출 +0xc31)`
  - `ⒷT2…` = `Ⓑ… → IopReassignSystemRoot (호출 +0x1139)`
- 끼는 자리: `ⒷT` 는 `ⒷA` 의 위상 1 몫 (`+0xc65`) 보다 앞, `Ⓑ1` 보다 앞. `ⒷT2` 는 `Ⓑ1` 뒤.

#### 왜 결과서의 `ⒷR` · `ⒷR2` 를 바꿨나

1. 결과서는 `ⒷR` (`\SystemRoot` 의 R) · `ⒷR2` 를 제안했다.
2. 그런데 `ⒷR` 은 run115 가 이미 "부팅 뒤 · 가게 생기기 전" 이라는 **때** 로 썼다 ([run115 해설 2.6](run115-kusd-boot-해설.md)).
3. 같은 글자를 두 뜻으로 쓰면 헷갈린다. 그래서 **`ⒷT`** · **`ⒷT2`** 로 바꿨다.
4. O (Root 의 O) 도 피했다. `Ⓑ0` (숫자 0) 과 헷갈려서다.

run115 때 `ⒷL` → `ⒷR` 로 바꾼 것과 같은 이유다.

#### 그 밖에 결과서와 다르게 적은 곳 (뜻은 같음)

| 무엇 | 결과서 | run 문서 · 짐 지도 | 이유 |
|---|---|---|---|
| `\ArcName\…` 링크들 줄의 단계 | `Ⓑ1` | **`Ⓑ`** | `Ⓑ1` 은 "`PsLocateSystemDlls` 구간" 이다. 그보다 앞인 `IopInitializeBootDrivers` (`+0xf3b`) 는 그냥 `Ⓑ` (run115 KUSD `NtSystemRoot` 줄처럼) |
| `IopReassignSystemRoot` 의 손잡이 닫기 | "핸들 닫기" | `ObCloseHandle` (`+0xed` · `+0x195` · `+0x1e2`) | `.asm` 에 그 이름. `NtClose` 가 아님 |
| 명부 출처 사슬 방향 | `refs` 거꾸로 (`SepVariableInitialization +0x1923` → … → `SeInitSystem +0x33`) | 부르는 순서로 (`SeInitSystem +0x33` → … → `SepInitSystemDacls`) | 읽기 쉽게. 뜻은 같음 |

### 2.12 맞춰 볼 것 하나: "끝 한 글자" 와 KUSD `"C:\WINDOWS"`

모순이 아니라 **짐작 하나** 다. 단계별로 간다.

1. 근거 ① `[관찰]` (run115 사진 줄): KUSD `NtSystemRoot` = `"C:\WINDOWS"`. 끝에 `\` 가 **없다**.
2. 근거 ② `[정적]` (run115): 그 글은 `IopStoreBootDriveLetter +0x136` 이 `"%C:%S"` 로 같은 `NtBootPathName` 을 써서 만든다.
3. 근거 ③ `[정적]` (이번): `CreateSystemRootLink` 는 같은 글에서 **끝 한 글자를 뺀다.**
4. 그래서 둘이 함께 맞으려면 둘 중 하나다:
   - ① `NtBootPathName` 이 `\` 로 **안** 끝난다 → `\SystemRoot` 쪽이 `\Device\BootDevice\WINDOW` 가 된다.
   - ② `\` 로 **끝난다** → `IopStoreBootDriveLetter` 쪽이 `%S` 인자 (`+0x10c MOV R9,RSI`) 를 따로 다듬는다.
5. run 문서 짐작: **② 쪽** `[추측: 이 코드가 일부러 −2 를 함]`. `\WINDOW` 같은 이름을 일부러 만들 리는 없어 보인다는 뜻이다.
6. 아직 확인 아님. 닫는 길은 둘:
   - 동적: 부팅 뒤 `!object \SystemRoot` 대상 글 하나 (Q94).
   - 정적: `IopStoreBootDriveLetter +0x10c` 의 `RSI` 출처 (Q209 ⑤).

### 2.13 `\SystemRoot` 를 만드는 다른 곳: 서버 silo 용

- `PspSiloInitializeSystemRootSymlink` (글 `PspSystemRootSymlinkName`) 가 있다 `[정적]`.
- 부르는 곳은 `PspInitializeServerSiloDeferred +0x4c` 한 곳 (`_refs/PspSiloInitializeSystemRootSymlink.tsv` 4행).
- test.exe (호스트) 길과는 무관해 보인다. 본문은 안 읽었다 `[추측]`.
  - 비유: 상가 안 **따로 떼어 둔 구역** (silo) 마다 자기 "윈도우 폴더 가는 길" 안내판을 따로 다는 것으로 보인다 `[추측]`. 우리 동네 큰길 안내판과는 다른 것이다.

### 2.14 에뮬레이터 쪽에서 보면 (오케스트레이터가 준 줄)

아래는 우리 `[관찰]` · `[정적]` 이 아니라 **PE_emulator 쪽 코드 줄** 이다 (flow-writer 는 그 코드를 안 읽었다). 고칠지는 사용자 결정이다.

| 에뮬레이터 | 진짜 (이번 `[정적]`) | 상태 |
|---|---|---|
| `namespace.cpp:31` `\SystemRoot → \Device\BootDevice\Windows` | 같은 모양 (`ⒷT`) | 모양 맞음 |
| `:34` `\Device\BootDevice → \Device\HarddiskVolume3` | 처음엔 `\ArcName\…`, `ⒷT2` 뒤에 `\Device\<장치>` | **처음부터 새로 단 뒤 모양**. test.exe 시동은 `ⒷT2` 뒤라 맞는 쪽. 앞 모양을 보일지는 결정 |
| `:21` 방 목록에 `\Device` | 있음 | 맞음 |
| `\ArcName` · `\Device\OSDataDevice` · `\OSDataRoot` | 있음 | **없음**. test.exe 시동이 읽는지 확인 필요 |
| 이름 공간 `Entry` (링크 여부 · 이름 · 대상) | 영구 표시 · 명부도 있음 (`0x50` + 전역 명부, `ⒷT2` 는 `0x250` + SD 칸 0) | 칸 없음. 보안 묻기 · 지우기 시험이 이 값을 보는지 확인 필요 |

- 짧게: 에뮬레이터는 **문 여는 날을 건너뛰고 "문 연 뒤 모양" 을 그려 둔** 셈이다. test.exe 만 보면 그걸로 된다. 다만 안내판의 **명부 · 영구 표시** 는 그림에 없다.

### 2.15 디버거가 붙어서 달라지는 점

- 이 길에서 디버거 깃발을 보는 줄은 못 봤다 (결과서 줄 범위 안).
- 부팅 디버깅 (KDNET) 이 안내판 값을 바꿀 까닭은 안 보인다 `[추측]`.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (그림은 그대로)

| 절 | 전 | 후 |
|---|---|---|
| 1장 그림 메모 | (없음) | run119 도 **그림을 안 고쳤다**. 부팅 위상 1 이라 지도 앞, test.exe 와 닿는 곳은 `PsLocateSystemDlls` 하나 |
| 2.1 부팅 메모 | `PsLocateSystemDlls` 가 `\SystemRoot\…` 를 연다. 링크가 먼저 있어야 함 (호출 순서로만) | 하위 줄 하나 더 (run119 `[정적]`): `+0xc31 CreateSystemRootLink` 가 **그보다 먼저** 만든다 |
| 3장 | (없음) | **새 절** "부팅 때 nt 가 만드는 `\SystemRoot` 링크". 길 안내 표지판 비유 · 나무 · `[정적]` 6줄 · silo `[추측]` 1줄 |
| 4장 run48 줄 | `\SystemRoot` 링크 만드는 곳 = 후보 `+0xc31` `[추측]` | **해결됨** → run119 `[정적]`. 새 줄 넷 (해결 · 동적 · 정적 · PE_emulator) |
| 5장 | | 근거 run 표 · 03_sprint 요청서 번호 표에 run119 |

- 태그 변화 하나: run48 · flow.md 4장 · 짐 지도 7.11 의 "`+0xc31 CreateSystemRootLink` 후보 `[추측]`" → **`[정적]`**.
- 이전 run 이 틀린 곳은 없다 (run 문서 "정정").

### 짐 지도에서 바뀐 것

- **2장 숫자 그대로.** 안내판 · 방은 구조체 칸이 아니라 **커널 개체** 라 체크리스트 밖이다 (7.6 의 다른 "기록만" 줄과 같은 처리).
- **새 7.6.16, 9줄** = `ⒷT` 6 (방 2 · 안내판 4) + `Ⓑ` 1 (`\ArcName\…` 안내판들 — 안쪽 안 읽음) + `ⒷT2` 2 (새로 단 `\Device\BootDevice` · ANSI 사본).
- 쓰는 말에 단계 둘 (`ⒷT` · `ⒷT2`), 3장 줄임 표에 둘, 5장 `Ⓑ` 4번 문장 보탬 · 새 10번, 7.11 정적 공백 닫힘, 새 7.50.

### 큰 그림에서

- test.exe 가게가 문 열 때 받는 관리실 세트 (ntdll) 는 부팅 때 `\SystemRoot\System32\ntdll.dll` 이름으로 짠 것이다 (run48).
- 이번 바퀴로 그 이름의 **앞부분 `\SystemRoot` 가 어디서 와서 어떻게 풀리는지** 가 처음부터 이어졌다:
  - 로더 블록 (부트 로더 짐 꾸러미) → `ⒷT` 안내판 달기 → (`Ⓑ` 옛 주소 안내판들) → `Ⓑ1` ntdll 열기 → `ⒷT2` 가운데 안내판 갈아 달기 → … → test.exe
- 에뮬레이터로 옮길 때: test.exe 시동만 흉내 내면 **`ⒷT2` 뒤 모양** (`\SystemRoot` → `\Device\BootDevice` → `\Device\<장치>`) 이면 된다 (짐 지도 7.6.16). 개체마다 남는 건 이름 · 대상 · 영구 표시 · 명부다.

---

## 4. 아직 모르는 것

| 무엇을 모르나 | 왜 이번에 못 알았나 | 어디서 보나 |
|---|---|---|
| `\SystemRoot` 실제 대상 글 (`\Device\BootDevice\Windows` 인가), 영구 표시 (`_OBJECT_HEADER +0x1b` 비트 4), 붙은 명부 (`+0x28`). **"끝 한 글자" 맞춰 보기** (2.12) 도 여기서 닫힘 | `NtBootPathName` 은 실행 때 값. 정적으로는 "끝 한 글자 뺌" 만 앎 | 동적 **Q94** (부팅 뒤 읽기만, 전역 개체라 test.exe 조건 없음) |
| `\Device\BootDevice` 대상 (`\Device\HarddiskVolumeN` 인가) · 새 안내판 명부 | `ⒷT2` 가 끝까지 갔는지는 실행 결과 | 동적 **Q94** |
| `\ArcName` 방 안 항목 · 안내판 길 단계 수 | 안쪽 채우는 함수 본문 안 읽음 + 실행 때 값 | 동적 **Q94** · 정적 **Q209 ①** |
| `\Device\OSDataDevice` · `\OSDataRoot` 대상 | 실행 때 값 | 동적 **Q94** |
| (선택) ntdll 을 여는 순간 `\Device\BootDevice` 가 아직 `\ArcName\…` 인가 | 부팅 중 멈춰야 봄. 정적 순서로 충분하면 생략 | 동적 **Q94** (선택) |
| `\ArcName\…` 안쪽 안내판의 이름 꼴 · 대상, `IopCreateArcName` 이 불리는 길 `[공백]` | 요청서 범위 밖 (이름만 봄). `IoCreateSymbolicLink` 가 Ghidra 조각 (`FUN_140b44c9e` · `FUN_140b44da4`) 안이라 주인이 안 보임 | 정적 **Q209 ①** (작음) |
| `\Device` 가 정말 여기서 처음 생기나 (이름 공간 처음 모양) | `ObInitSystem` · `IopCreateRootDirectories` 안 읽음 | 정적 **Q209 ②** (낮음) |
| 서버 silo `\SystemRoot` | test.exe 와 무관해 보여 미룸 | 정적 **Q209 ③** (낮음) |
| 넘긴 명부가 개체에 붙는 줄, `ⒷT2` 안내판 (SD 칸 0) 이 받는 기본 명부 `[공백]` | `ObCreateObjectEx` / `ObInsertObjectEx` 안 안 읽음 | 정적 **Q209 ④** (작음) |
| `IopStoreBootDriveLetter +0x10c` 의 `RSI` 출처 | 2.12 를 정적으로 닫으려면 필요. 이번 범위 밖 | 정적 **Q209 ⑤** (작음) |
| 에뮬레이터 이름 공간에 `\ArcName` 등 · 영구 표시 · 명부를 둘지 | 우리 일이 아니라 결정 | 사용자 결정 (확인 필요) |

---

## 5. 더 읽기

- run 문서: [`run119-systemroot-link.md`](run119-systemroot-link.md). 특히 "정적 — 호출 지점" 표 (call · ret 전부), "결과서와 다르게 적은 곳 · 보탠 것", "정적과 동적이 다른 곳"
- 결과서: [`_workspace/sast/114_systemroot-link_result.md`](../../_workspace/sast/114_systemroot-link_result.md). 질문별 `.asm` 줄, "동적분석으로 확인해야 할 것" 1 ~ 5
- flow.md: [`flow.md`](flow.md) 2.1 부팅 메모 · 3장 "부팅 때 nt 가 만드는 `\SystemRoot` 링크" · 4장 run119 줄
- 짐 지도: [`baggage.md`](baggage.md) 쓰는 말 (`ⒷT` · `ⒷT2`) · 3장 줄임 표 · **7.6.16** · 7.50
- 앞 바퀴 해설:
  - [run48 해설](run48-system-dll-boot-해설.md): 문 여는 날 · 시스템 DLL 세트. 이번에 맨 위 "고쳐 읽기" 를 붙임
  - [run98 해설](run98-rpc-dir-create-해설.md): 이름 공간 = 동네, smss 가 다는 방 · 명부
  - [run115 해설](run115-kusd-boot-해설.md): 부팅 단계 줄임 (`ⒷR` 등), KUSD `NtSystemRoot`
  - [run47 해설 2.10](run47-section-chain-해설.md#210-대출-카드-사슬-이름-따라가기와-parse-프로시저): 이름 따라가기 · 안내판 (별칭 표지판) · parse 프로시저

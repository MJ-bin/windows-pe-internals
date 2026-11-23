# run134 reg-trailing-sep — 해설

> 이 글은 [run134 문서](run134-reg-trailing-sep.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q247** — PE_emulator 요청 `282_internals-reg-trailing-sep.md` "레지스트리 키 이름 끝에 `\` 가 붙으면 커널이 어떻게 대답하나" (`_workspace/plan.md`, 이번에 **끝 (run134)**)
  - 튜닝 `282_tune07.md` R1, 샘플 2,730 (병목 아님).
  - 사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 **첫째**.
  - 결과서 판정 **분석성공** (질문 셋 모두 `.asm` 줄로 답함, `[공백]` 셋 남김).
  - 남은 것이 간 곳 (4절): 동적 → **Q251**, 정적 → **Q252**.
- 근거:
  - run 문서 [`run134-reg-trailing-sep.md`](run134-reg-trailing-sep.md) (가장 중요)
  - 요청서 [`_workspace/sast/131_reg-trailing-sep.md`](../../_workspace/sast/131_reg-trailing-sep.md) · 결과서 [`131_reg-trailing-sep_result.md`](../../_workspace/sast/131_reg-trailing-sep_result.md)
    - ntoskrnl 은 **다시 analyze 안 하고** `^이름$` export 만 (5번). 스크립트 문제 없음.
  - flow.md 변경: `git diff -- runs/03_sprint/flow.md` (이번 바퀴, 아직 커밋 전). **그림은 안 바뀜.** 짐 지도 `baggage.md` 도 안 건드렸다.
- 이번 바퀴는 **정적만** 이다. VM 실행도, 동적 요청서도 없다.

### 이번 바퀴에서 조심할 곳 넷

1. **요청의 이름은 test.exe 것이 아니다.**
   - `…\Control\CommonGlobUserSettings\` 는 **에뮬레이터 로그**에서 왔다. test.exe 초기화 지도엔 없다.
   - 다만 test.exe 도 `NtOpenKey` 를 부른다 (2.4 `RtlpQueryNlsSystemCodePages`, run58). 그 이름도 커널 안에선 이번에 읽은 **같은 사슬**을 탄다 `[정적: 같은 코드, 그 이름으로 실행 본 적 없음]`.
2. **요청 쪽 말은 진짜 Windows 관찰이 아니다.**
   - "에뮬레이터가 `0xc0000033` 으로 답했다" 는 에뮬레이터 로그다. `[관찰]` 이 아니다.
   - PE_emulator 파일 이름·줄 번호 (`nt_open_key.cpp:47` 등) 는 결과서가 적은 것이고, flow-writer 는 그 코드를 안 읽었다.
3. **오프셋 읽는 법** (run 문서와 같은 규칙).
   - 나무 (`├─ +0x..`) 의 숫자 = **ret 자리** (콜스택에 찍힐 값).
   - 본문 "`+0x2e4 CmpDoParseKey`" 처럼 부르는 함수 이름 **앞** 숫자 = **call 자리**.
   - "`CmpParseKey +0x112`" 처럼 함수 이름 **뒤** 숫자 = 그 함수 안 **줄**.
   - 콜스택으로 본 적은 없다.
4. **status 값의 이름은 대부분 짐작이다.**
   - 숫자는 코드에서 읽은 것이다. 이름은 근거에 적힌 것만 옮긴다:
     - `0xc0000033` = "이름 잘못" (요청서)
     - `0xc000003b` = `STATUS_OBJECT_PATH_SYNTAX_BAD` (결과서)
     - `0xc0000034` = "그런 이름 없음" ([run129 해설](run129-winnsi-rpc-해설.md))
   - `0xc000000d` 는 근거에 이름이 없다. 일반적으로 "인자가 잘못됨" (`STATUS_INVALID_PARAMETER`) 으로 알려져 있다 `[추측]`.

이미 푼 말은 **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절, 핸들 = **손잡이**
- `UNICODE_STRING` = "몇 글자, 어디부터" 를 적은 **쪽지** `{Length, MaximumLength, Buffer}` → [run19 해설 2.1](../02_sprint/run19-proc-init-modules-해설.md#21-run18-정정-하나--exe-폴더가-아니라-exe-파일-이름)
- 레지스트리 = 관리실 **설정 장부**, 키 = 장부의 **서랍** → [run58 해설](run58-registry-strings-해설.md)
- 객체 이름 공간 = 관리사무소 안 **동네**, 심볼릭 링크 = **안내판** → [run119 해설 "한 줄로 먼저"](run119-systemroot-link-해설.md)
- `ObpLookupObjectName` = 주소를 따라가는 **안내 데스크**, parse 프로시저 = 건물 종류마다 있는 **담당자** → [run47 해설 2.10](run47-section-chain-해설.md#210-대출-카드-사슬-이름-따라가기와-parse-프로시저)
- `RootDirectory` = 이름을 "이 손잡이 기준으로" 읽게 하는 **기준 손잡이** 칸 → [run132 해설 2.9](run132-npfs-pipes-해설.md)

### 비유 — 그대로 이어 쓴다

- 레지스트리는 관리실 **설정 장부** 다 (run58). 키는 그 장부의 **서랍** 이다.
- 키 이름은 **"서랍장 › 칸 › 상자"** 같은 **주소** 다 (run 문서 비유). `\` 가 `›` 다.
- 주소를 받으면 먼저 동네의 **안내 데스크** (`ObpLookupObjectName`) 가 `\Registry` 까지 데려간다.
- `\Registry` 는 장부 건물이다. 건물 안 주소 ("`Machine › System › …`") 는 **장부 건물 담당자** (`CmpParseKey`) 에게 넘긴다. run47 에서 장치 건물 담당자가 `IopParseDevice` 였던 것과 같은 그림이다.
- 이번에 더하는 것:
  - 담당자는 주소 쪽지를 **한 장 베껴서** 그 사본에 **지우개질** 을 한다 — 양 끝에 붙은 `›` 를 다 지운다.
  - 그다음 **주소 읽는 사람** (`CmpComputeComponentHashes`) 이 주소를 칸칸이 끊어 읽는다. 가운데 `››` 처럼 두 번 찍힌 건 **한 번으로 읽는다**.
  - 그래서 끝에 `›` 가 하나 더 붙어도, 가운데 `›` 가 두 번 찍혀도 **같은 상자를 찾는다.**

### 한 줄로 먼저 — 무엇을 얻었나

**이번 글의 중심은 이 세 줄이다.** `[정적]`

> **(1) 요청서의 짐작 ("`CmpGetNextName` 이 이름을 자른다") 은 틀렸다.** `CmpDoParseKey` 는 `CmpGetNextName` 을 부르지 않는다. 이름을 다듬는 곳은 **`CmpParseKey` 앞머리** 와 **`CmpComputeComponentHashes`** 둘이다.

> **(2) 끝 `\` 하나 · 끝 `\\` 둘 · 가운데 `\\` → 다 "연다".** `CmpParseKey` 가 끝 `\` 를 **전부**, 앞 `\` 를 **전부** 떼고, `CmpComputeComponentHashes` 가 가운데 연속 `\` 를 **한 칸** 으로 읽는다. `\` 때문에 거절하는 줄이 이 길엔 없다.

> **(3) 예외는 둘.** 맨 앞 `\\Registry…` → **`0xc0000033`** (안내 데스크에서 막힘). 키 핸들 기준인데 이름이 `\` 로 시작 → **`0xc000003b`** (`CmpParseKey` 까지 못 감).

| 요청서 질문 | 한 줄 답 (run 문서 그대로) | 이 글 |
|---|---|---|
| 1 절대 이름 끝 `\` 하나 | **끝 `\` 를 떼고 그 키를 연다.** `0xc0000033` 아님 (`CmpParseKey +0x112` · `+0x118` → `+0x5fa`) | 2.3 ~ 2.5 |
| 2 끝 `\\` · 가운데 `\\` | **둘 다 연다.** 끝은 같은 루프가 다 떼고, 가운데는 `CmpComputeComponentHashes +0x25b ~ +0x278` 이 한 칸으로 | 2.5 ~ 2.7 |
| 3 상대 이름 `Key\` | **같은 줄로 연다.** `ObpLookupObjectName +0x20d` 이 이름 통째를 넘김. 덤: 빈 이름 → 같은 키 다시, `\` 시작 → `0xc000003b` | 2.9 ~ 2.11 |
| (넓힌 것) 링크 · 만들기 | 링크 다시 풀기는 끝 `\` 를 안 붙임. 만들기 (`NtCreateKey`) 도 같은 파싱 | 2.12 · 2.13 |

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 바퀴는 **test.exe 흐름의 한 구간이 아니다.** PE_emulator 가 물어 온 "커널 부품 하나의 규칙" 이다.

```
cmd.exe 의 부탁 → 커널 (프로세스 만들기) → ntdll (로더) → … → main
                                              │
                                              └─ 2.4 RtlpQueryNlsSystemCodePages +0xb7  NtOpenKey   (run58)
                                                     ⇒ 커널 안:  NtOpenKey → … → ObpLookupObjectName
                                                                 → CmpParseKey → CmpDoParseKey      ← 이번에 읽은 사슬
```

- test.exe 의 `NtOpenKey` 이름 (`\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage`) 은 **끝 `\` 가 없다.** 그래서 이번 질문의 갈래 (끝 `\` 떼기 루프) 는 그 이름에선 한 번도 안 돈다 (run 문서 "지도 범위").
- 커널 안 레지스트리 이름 풀이는 "대표 지점" 이 아니라서 **그림엔 안 넣었다.** flow.md 3장 끝에 따로 절을 두었다.

### 왜 이 질문이 지금 왔나

- 에뮬레이터 로그 (진짜 Windows 관찰 아님): 아래 부름에 에뮬레이터가 `0xc0000033` (이름 잘못) 으로 답했다.
  - `NtOpenKey("\Registry\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\")` — 맨 끝에 `\` 가 붙어 있다.
- 로케일을 물을 때마다 받는 대답이라, 틀리면 **넓게** 틀린다 (요청서).
- 진짜 VM 엔 그 키가 있다 (`[수집]` — 요청 원문).
- 에뮬레이터는 상대 이름 끝 `\` 를 `[추측]` + `TODO(VM)` 로 받아 둔 상태였다. 이번 답으로 닫으려 했다.

### 무엇이 궁금했나

1. 절대 이름 끝에 `\` 하나가 붙으면, 커널은 그걸 떼고 키를 여나? 아니면 어느 줄에서 어떤 status 를 내나?
2. 끝 `\\` 둘, 가운데 `\\` 도 같은 규칙인가?
3. 상대 이름 (키 손잡이 기준 + `Key\`) 도 같은 줄을 타나?

### 바퀴 전에 알던 것

- `[정적]` run47: `ObpLookupObjectName` 은 객체 형식의 Parse 함수를 비교해 보고 **직접 호출** 한다 (파일 = `IopParseDevice`).
- `[정적]` 요청서 "이미 아는 것": `ObpLookupObjectName.asm` 의 `+0x20d` · `+0x8bb` · `+0x8ce` — 이름을 손대지 않고 `CmpParseKey` 에 넘긴다.
- `[정적]` run58: test.exe 초기화의 `NtOpenKey` 키 이름 글자 전체.
- `[정적]` run89: mscoree 가 끝 `\` 이름으로 키를 열고 바로 하위 키를 센다 (`FindLatestVersionWithCallback +0xe4`). 다만 **성공한다는 직접 근거는 아니었다** (요청서).
- 요청서의 짐작: 이름은 `CmpParseKey → CmpDoParseKey → CmpGetNextName` 길에서 잘린다. → **이번에 틀린 것으로 밝혀짐** (2.3).

---

## 2. 찾은 것

### 2.1 한눈에 — 이름이 지나가는 길 `[정적]`

run 문서 "호출 관계" 를 줄였다. 숫자는 ret 자리.

```
NtOpenKey +0xc NtOpenKeyEx ⇢ CmOpenKey            /   NtCreateKey ⇢ CmCreateKey (만들기)
 └─ ObOpenObjectByNameEx
      ├─ +0x103  ObpCaptureObjectCreateInformation → ObpCaptureObjectName   홀수 길이 → 0xc0000033
      └─ +0x224  ObpLookupObjectName                                         (안내 데스크)
                  ├─ (상대) 이름 통째 = 남은 이름 (+0x20d) · 첫 글자 \ → 0xc000003b      ← 예외 ②
                  ├─ (절대) 폴더 걷기 \Registry · 길이 0 조각 (\\Registry) → 0xc0000033  ← 예외 ①
                  └─ +0x8d3  CmpParseKey                     끝 \ 전부 → 앞 \ 전부 떼기 (사본)
                              └─ +0x2e9  CmpDoParseKey
                                          ├─ +0x21c  CmpComputeComponentHashes   가운데 \\ 한 칸
                                          ├─ +0x1232 CmpReferenceKeyControlBlockUnsafe   조각 0 개 → 시작 키
                                          ├─ +0xbe6 · +0x17c6 · +0x2a722a CmpGetSymbolicLinkTarget   링크
                                          └─ +0x15a1 · +0x1cb8 · +0x1fa7 CmpCreateChild   (만들기)
(이 길 아님) CmpWalkUnicodeStringPath · CmpGetKnownHivePathNode · CmpFindHiveSubKey → CmpGetNextName
```

- `⇢` 는 **콜아웃 포인터로 가는 호출** 이다. `NtOpenKeyEx` 는 `CmOpenKey` 를 바로 부르지 않고, `KeExpandKernelStackAndCallout` 에 "이 함수 (`CmOpenKeyCallout`) 를 불러 줘" 하고 주소를 건넨다 (`+0x74 LEA` · `+0x7b`) `[정적]`. 그 안은 안 읽었다.
  - 이름으로 읽으면 "커널 스택을 넓힌 뒤 부르기" 다 `[추측: 이름]`. 비유: 직접 전화하지 않고, 넓은 회의실을 잡아 주는 사람에게 "거기서 이 사람 불러 주세요" 하고 맡기는 것.
- 비유로 다시:
  - 안내 데스크가 `\Registry` (장부 건물) 까지 데려간다.
  - 장부 건물 담당자 (`CmpParseKey`) 가 주소 쪽지 사본의 양 끝 `›` 를 지운다.
  - 그 안의 일꾼 (`CmpDoParseKey`) 이 주소 읽는 사람 (`CmpComputeComponentHashes`) 에게 주소를 칸칸이 끊게 한다.

### 2.2 한 줄씩 읽는 법

#### 줄 ①: `└─ +0x8d3  CmpParseKey   끝 \ 전부 → 앞 \ 전부 떼기 (사본)`

run 문서 호출 지점 표의 `nt!ObpLookupObjectName | +0x8ce | +0x8d3 | CmpParseKey` 줄.

- **누가**: `ObpLookupObjectName` (안내 데스크)
- **어디서**: call `+0x8ce`, ret `+0x8d3`
- **누구를**: `CmpParseKey` (장부 건물 담당자)
- **무엇을 위해**: `\Registry` 에 닿은 뒤 **남은 이름** 을 넘긴다. 남은 이름은 7번째 인자다 (`+0x8bb LEA RAX,[RSP+0x70]`) `[정적]`.
- **어떻게 골랐나**: `+0x871 LEA` (`CmpParseKey` 주소) · `+0x878 CMP` · `+0x885 JNZ` — "이 형식의 Parse 가 `CmpParseKey` 인가" 를 견주고, 맞으면 **직접** 부른다 `[정적]`.
  - run47 의 `IopParseDevice` (`+0xb75 LEA` → `+0xbcd`) 와 **같은 꼴** 이다. 파일이면 `IopParseDevice`, 레지스트리 키면 `CmpParseKey` — 같은 함수의 다른 자리다.

#### 줄 ②: `├─ +0x21c  CmpComputeComponentHashes   가운데 \\ 한 칸`

run 문서 표의 `nt!CmpDoParseKey | +0x217 | +0x21c | CmpComputeComponentHashes` 줄.

- **누가**: `CmpDoParseKey`
- **어디서**: call `+0x217`, ret `+0x21c`. 바로 앞 `+0x213 LEA RCX,[RBP-0x8]` 이 넘길 이름을 싣는다.
- **누구를**: `CmpComputeComponentHashes`
- **무엇을 위해**: `CmpParseKey` 가 다듬어 넘긴 이름 (`+0xe1` · `+0xef` → `[RBP-0x8]`) 을 **조각** 으로 나누고, 조각 수를 세고, 조각마다 해시를 만든다 `[정적]`.
  - **새 말 — 조각 (component)**: 이름을 `\` 로 끊은 한 칸. `Machine\System\Control` 이면 조각 셋.
    - 비유: 주소 "서랍장 › 칸 › 상자" 의 **한 칸 한 칸**.
  - **새 말 — 해시**: 조각마다 대문자로 바꿔 계산한 숫자 (`+0x216 ~ +0x23a`, `IMUL 0x25`) `[정적]`. 쓰임새는 일반적으로 "이름을 빨리 견주려고 만든 숫자" 로 알려져 있다 `[추측]`.
    - 비유: 서랍 이름표마다 붙인 **색깔 번호**. 글자를 하나하나 견주기 전에 색깔부터 보면 빠르다.

### 2.3 ★ 요청서의 짐작이 틀렸다 — `CmpGetNextName` 은 이 길이 아니다 `[정적]`

#### 요청서는 무엇을 짐작했나

- 길: `CmpParseKey → CmpDoParseKey → CmpGetNextName`.
- 그래서 "`CmpGetNextName` 이 끝 `\` 를 만나면 빈 조각을 어떻게 알리나" 를 물었다.
- 이름만 보면 그럴듯하다. "다음 이름 (조각) 꺼내기" 니까.

#### 근거를 한 걸음씩

1. `CmpDoParseKey` 가 부르는 함수를 `_calls.tsv` 로 다 셌다 — 호출 **264 줄**, 서로 다른 callee **101 개**. 그 안에 `CmpGetNextName` 이 **없다** (flow-writer 가 다시 셈).
2. 거꾸로 `CmpGetNextName` 을 부르는 곳을 찾았다 (refs). 코드 참조는 **셋뿐** 이다 (넷째는 `.pdata` 데이터 참조):
   - `CmpWalkUnicodeStringPath +0x39`
   - `CmpGetKnownHivePathNode +0x67`
   - `CmpFindHiveSubKey +0x56`
3. 그 셋의 위를 따라가면 (결과서 — `.c` 머리 `callers:`) 전부 **이름이 코드에 박힌 하이브 안 걷기** 다.
   - 예: `CmpWalkPath` ← `CmpGetSystemControlValues` · `CmpPreserveSystemHiveData` · `CmSelectQualifiedInstallLanguage`.
4. 그럼 사용자가 넘긴 이름은 누가 자르나? `CmpDoParseKey +0x217` (그리고 `+0x5d4`) 의 **`CmpComputeComponentHashes`** 다. 끝 `\` 를 떼는 건 그보다 앞인 **`CmpParseKey`** 다.

- 비유: 요청서는 "손님 주소는 **우편 분류기** 가 읽는다" 고 짐작했다. 알고 보니 그 분류기는 관리실 **안에서만 쓰는 고정 주소록** (커널이 미리 알고 있는 경로) 을 읽는 도구였다. 손님 주소는 **창구 담당자** 와 **주소 읽는 사람** 이 따로 다룬다.

#### 그래도 `CmpGetNextName` 규칙은 답해 두었다 (참고 — 이 길에선 안 쓰임) `[정적]`

- 앞 `\` 는 전부 건너뛴다 (`+0x2d ~ +0x50`).
- **빈 조각** = 반환 TRUE + 출력 조각 Length 0 · Buffer NULL + `last = 1` (`+0x52` · `+0x56` · `+0x58` · `+0x5c`).
- **마지막 조각** = 조각을 뗀 뒤 남은 길이가 0 일 때만 `last = 1` (`+0x98` · `+0xa4` · `+0xa7`).
  - 그래서 `Key\` 를 주면 `Key` (`last = 0`) 다음에 **빈 조각** (`last = 1`) 이 하나 더 나온다.
- 조각이 `0x200` 바이트보다 길면 반환 FALSE (`+0xaa` · `+0xb2`).
- `CmpWalkUnicodeStringPath` 는 빈 조각을 "경로 끝" 으로 본다 (`+0x3e` · `+0x42 JZ → +0xa6`).

> 요청서가 걱정한 "끝 `\` 뒤 빈 조각" 은 **이 도구라면** 실제로 생긴다. 그런데 손님 이름은 이 도구를 안 지나가니, 그 걱정은 손님 이름엔 해당이 없다.

#### 이 정정이 왜 중요한가

- PE_emulator 쪽이 "`CmpGetNextName` 규칙" 을 기준으로 흉내를 고치면 **엉뚱한 규칙** 을 베끼게 된다.
- 그래서 run 문서와 flow.md 4장 PE_emulator 줄에 "**요청서의 `CmpGetNextName` 규칙은 이 길이 아니니 기준으로 삼지 말 것**" 을 같이 적었다.
- 앞 run 문서 (run3 ~ run133) 엔 이 세 함수를 적은 곳이 없어서 고칠 run 은 없다 (run 문서 `grep`).

### 2.4 이름이 흘러가는 차례 — 절대 이름 끝 `\` 하나 `[정적]`

예: `\Registry\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\`

1. **`ObpCaptureObjectName`** — 이름을 **그대로** 복사한다 (`+0x1c3`).
   - 거르는 건 홀수 길이 · `0xfffe` 뿐이다 (`+0xc0 ~ +0xd0` → `+0x1d2` `0xc0000033`). `\` 는 안 본다.
2. **`ObpLookupObjectName`** (안내 데스크) — 남은 이름 칸 (`[RSP+0x70]`) 을 이름 통째로 채우고 (`+0xa3b` · `+0xa3e`), 폴더 걷기 (`+0xa61 ~ +0xae5`) 가 `Registry` **한 조각** 을 먹는다.
   - 남은 이름 = `\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\` — **끝 `\` 그대로**.
   - 안내 데스크는 끝 `\` 를 안 건드린다. `0x5c` (`\`) 비교 **10 줄** 이 전부 **앞 글자** 를 보고, 마지막 글자를 보는 줄이 없다.
3. **`+0x8ce CmpParseKey`** — 남은 이름을 장부 건물 담당자에게 넘긴다 (2.2 줄 ①).
4. **`CmpParseKey` 가 다듬는다** — 끝 `\` 전부 → 앞 `\` 전부 (2.5).
   - 결과: `Machine\System\CurrentControlSet\Control\CommonGlobUserSettings`
5. **`+0x2e4 CmpDoParseKey`** → **`+0x217 CmpComputeComponentHashes`** — 조각 **5 개**: `Machine` · `System` · `CurrentControlSet` · `Control` · `CommonGlobUserSettings`.

- 끝 `\` 는 4번에서 사라졌다. 그래서 5번에 **빈 조각이 안 생긴다.** 키가 있으면 성공이다.

### 2.5 `CmpParseKey` 앞머리 — 끝 `\` 전부, 그다음 앞 `\` 전부 `[정적]`

#### 사본에만 지우개질한다

- `+0x60` · `+0x86` · `+0x8c`: 7번째 인자 (남은 이름 쪽지) 를 **복사본** `[RSP+0x50]` 으로 옮긴다. 이 뒤로는 복사본만 만진다.
- 비유: 손님이 낸 주소 쪽지 원본엔 손대지 않고, **사본** 을 떠서 사본에 지우개질을 한다.

#### 순서

1. **길이 0 이면 건너뜀** — `+0xf7` · `+0xff`.
2. **끝 `\` 떼기 루프**:
   - `+0x112 CMP word [RDX+RAX*2-2],0x5c` — 마지막 글자가 `\` 인가 (결과서 풀이).
   - `+0x118 JZ +0x5fa` → `+0x5fa` 에서 Length −2 (한 글자 = 2 바이트).
   - `+0x603 JNZ +0x10c` — **처음으로 돌아가 다시 본다.** 그래서 끝 `\` 가 몇 개든 **전부** 떼인다.
   - 길이가 0 이 되면 `+0x609` 로 빠진다.
3. **앞 `\` 떼기 루프**:
   - `+0x128 CMP word [RDX],0x5c` — 첫 글자가 `\` 인가.
   - `+0x12c JZ +0x49c` → `+0x49c ~ +0x4b2` 에서 Buffer +2 · Length −2 · MaximumLength −2 (시작점을 한 글자 뒤로).
   - `+0x4ba JNZ +0x128` — 다시 본다 → 앞 `\` **전부**.
4. 다듬은 사본을 `+0x28f ~ +0x2dc` 로 옮겨 **`+0x2e4 CmpDoParseKey`** 에 넘긴다 (6번째 인자).

- 이 앞머리는 **status 를 만들지 않는다.** 떼기만 한다. 그래서 끝 `\` 이름이 `0xc0000033` 을 받을 자리가 없다.

#### 왜 끝 `\\` 둘도 되나

- 2번 루프가 `+0x603 JNZ +0x10c` 로 **되돌아가기** 때문이다. 하나를 떼고 다시 보면 또 `\` → 또 뗀다.
- 비유: 지우개질을 "`›` 가 안 보일 때까지" 계속한다. 한 번 하고 끝내지 않는다.

#### 왜 `\Registry\\Machine…` (루트 바로 뒤 `\\`) 도 되나

- 안내 데스크가 `Registry` 를 먹은 뒤 남은 이름은 `\\Machine…` 이다. 앞에 `\` 가 둘이다.
- 3번 루프가 앞 `\` 를 **전부** 떼니 `Machine…` 이 된다 (`+0x128 ~ +0x4ba`).

### 2.6 `CmpComputeComponentHashes` — 가운데 `\\` 는 한 칸 `[정적]`

예: `…\Control\\CommonGlobUserSettings`

- `CmpParseKey` 는 **가운데를 안 본다.** 양 끝만 지운다. 가운데 `\\` 는 그대로 넘어온다.
- `CmpComputeComponentHashes` 가 한 글자씩 읽는다:
  1. `+0x1a9 CMP word [R15],0x5c` · `+0x1ae JNZ +0x2a4` — `\` 가 아니면 한 글자 앞으로.
  2. `\` 를 만나면 `+0x1b4` 부터 **조각 하나를 기록** 한다 (길이 = 조각 시작 때 남은 길이 − 지금 남은 길이). 해시도 만든다.
  3. **`+0x25b CMP word [R15],0x5c` · `+0x262` Length −2 · `+0x266` Buffer +2 · `+0x278 JNZ +0x25b`** — 이어지는 `\` 를 **다 건너뛴다.**
  4. `\` 가 아닌 글자에서 `+0x27a` 로 나가 **새 조각** 을 시작한다.
- 그래서 `Control\\CommonGlobUserSettings` = 조각 **둘** (`Control` · `CommonGlobUserSettings`). 사이에 빈 조각이 안 생긴다.
- 비유: 주소 읽는 사람이 `››` 를 만나면 "칸 구분이구나" 하고 **구분 표시가 끝날 때까지** 넘긴 뒤 다음 칸을 읽는다.

#### 이 함수가 거절하는 것 — 숫자 상한 둘

- 조각 **32 개** 넘음 → `+0x19f CMP BX,0x20` · `+0x1a3` → `+0x3b6 ~ +0x3c0` `0xc000000d`.
- 조각 길이 **`0x200` 바이트 (256 글자)** 넘음 → `CmpDoParseKey +0x228 ~ +0x256` 검사 → `+0x2167` → `+0x216c` `0xc000000d`.
- 둘 다 **개수·길이** 문제다. `\` 모양 때문에 거절하는 줄은 여기 없다.

### 2.7 그래서 셋 다 "연다" — 왜 그런지 한 번에 `[정적]`

거절하려면 **"`\` 때문에 빈 조각이 생겼다" 를 보고 status 를 내는 줄** 이 있어야 한다. 그 줄이 어디 있나 단계로 보자.

1. **`\Registry` 앞** (안내 데스크의 폴더 걷기) — 빈 조각을 보면 `0xc0000033` 을 낸다 (2.9). 그런데 끝 `\` · 가운데 `\\` 는 여기까지 안 온다. 이 구간은 `Registry` 한 조각만 먹고 나머지는 통째로 넘기니까.
2. **`CmpParseKey`** — 양 끝 `\` 를 **떼기만** 한다. status 를 안 낸다.
3. **`CmpComputeComponentHashes`** — 가운데 연속 `\` 를 **건너뛰기만** 한다. 거절은 32 개 · 256 글자뿐.
4. → `\Registry` 뒤에서는 `\` 모양 때문에 거절하는 줄이 **없다.**

| 이름 꼴 (예) | 누가 다듬나 | 결과 |
|---|---|---|
| 끝 `\` `…\CommonGlobUserSettings\` | `CmpParseKey` 끝 루프 | **연다** |
| 끝 `\\` `…\CommonGlobUserSettings\\` | 같은 루프가 되돌아가 둘 다 (`+0x603`) | **연다** |
| 가운데 `\\` `…\Control\\CommonGlobUserSettings` | `CmpComputeComponentHashes +0x25b ~ +0x278` | **연다** |
| `\Registry\\Machine…` | `CmpParseKey` 앞 루프 | 연다 |

- 키가 **없으면** 어떻게 되나는 이번 범위 밖이다. `0xc0000034` 쪽인데, 그 값을 쓰는 자리가 여럿이라 최종 줄은 못 골랐다 → Q252 (4절).

### 2.8 다 떼고 나니 아무것도 안 남으면 — 시작 키를 다시 연다 `[정적]`

- 남은 이름 길이가 0 이면:
  - `CmpComputeComponentHashes +0x2f` · `+0x33` → `+0x15b ~ +0x162` — 조각 **0 개** 로 돌려준다.
  - `CmpDoParseKey +0x2df CMP R12W,R14W` · `+0x2e3 JZ` → **`+0x122d CmpReferenceKeyControlBlockUnsafe`** — **시작 키 자체** 를 연다. 오류가 아니다.
- 비유: "이 서랍에서 `›` 만 따라가세요" 하고 갈 칸이 하나도 없으면, **지금 서 있는 서랍** 을 준다.
- 이게 2.11 의 "상대 빈 이름 → 같은 키를 다시 연다" 의 바닥이다.

### 2.9 ★ 예외 ① — 맨 앞 `\\Registry…` → `0xc0000033` `[정적]`

#### 어디서 막히나

- 안내 데스크 (`ObpLookupObjectName`) 의 **폴더 걷기** 에서 막힌다. 장부 건물 담당자까지 못 간다.
- 걷기 규칙:
  1. `+0xa78` — 앞 `\` 를 **하나만** 뗀다.
  2. `+0xab0 ~ +0xaca` — 다음 `\` 까지 글자 수를 센다 (= 조각 길이).
  3. `+0xae2 TEST AX,AX` · `+0xae5 JZ` — 조각 길이가 0 이면 **`+0xe00` `0xc0000033`**.
- `\\Registry` 면: 1번에서 `\` 하나를 떼도 바로 다음 글자가 또 `\` 다. 2번에서 센 길이가 **0** → 3번에서 `0xc0000033`.

#### 왜 `\Registry` 앞에서만인가

- 폴더 걷기는 **`\Registry` 에 닿을 때까지만** 돈다. `\Registry` 가 장부 건물이라, 거기서 담당자에게 남은 이름을 통째로 넘기고 걷기를 멈춘다 (2.2 줄 ①).
- 그래서 이 "빈 조각 거절" 규칙은 **`\Registry` 조각 앞** 에만 걸린다. 그 뒤의 `\\` 는 담당자 몫이라 떼어진다 (2.5).
- 비유: 동네 안내 데스크는 "길과 길 사이에 `››` 처럼 빈 칸이 있는 주소" 를 받아 주지 않는다. 하지만 장부 건물 안 주소는 건물 담당자가 알아서 지우개질하니, 안내 데스크는 안을 들여다보지 않는다.
- 결과서 메모: 에뮬레이터 주석 (`nt_open_key.cpp:47`) 이 "빈 조각 `0xc0000033` 은 객체 폴더 갈래 규칙" 이라 한 것은 **맞다.** 다만 그 규칙은 `\Registry` 조각 **앞까지만** 이다.

#### 다른 `0xc0000033` 하나 — 홀수 길이

- `\` 와 상관없는 `0xc0000033` 이 하나 더 있다: 이름 길이가 **홀수** 바이트 (또는 `0xfffe`) 면 `ObpCaptureObjectName +0xc0 ~ +0xd0` → `+0x1d2` 에서 거절 `[정적]`.
- 글자 하나가 2 바이트라, 홀수 길이는 "글자 반 토막" 이 있다는 뜻이다. 에뮬레이터도 이미 이렇게 한다 (`nt_open_key.cpp:31`, 결과서 "맞음").

### 2.10 ★ 예외 ② — 키 손잡이 기준인데 이름이 `\` 로 시작 → `0xc000003b` `[정적]`

#### 어디서 막히나

- 이것도 안내 데스크 앞머리에서 막힌다. **`CmpParseKey` 까지 안 간다.**
- 차례:
  1. `+0x132 JNZ +0x1a0` — `RootDirectory` (기준 손잡이) 가 있다.
  2. `+0x1cb ObpReferenceObjectByHandleWithTag` — 손잡이 → 키 객체.
  3. `+0x1f9` · `+0x1fd` — 이름 첫 글자가 `\` 이면 `+0x1282` 로.
  4. `+0x1282` — 기준이 **파일 객체** 인지 본다 (`+0x129b CMP` · `+0x129e JZ` 면 통과).
  5. 파일 객체가 아니면 (키 손잡이면) **`+0x12ac` `0xc000003b`**.
- `\` 하나만 있어도, `\Key` 여도 같다.

#### 왜 이게 말이 되나 (뜻 풀이)

- `\` 로 시작하는 이름은 "**동네 맨 꼭대기부터**" 라는 뜻이다. 그런데 기준 손잡이를 같이 주면 "**이 서랍부터**" 라는 뜻이다. 둘이 부딪친다 `[추측: 일반적으로 알려진 설명 — 코드는 "첫 글자 `\` + 파일 객체 아님 → `0xc000003b`" 까지만 보여 준다]`.
- 그래서 안내 데스크가 "주소 쓰는 법이 틀렸어요" (`STATUS_OBJECT_PATH_SYNTAX_BAD`, 이름은 결과서) 로 돌려보낸다.
- 예외 ① 과 견주면:
  - ① 은 **빈 칸** 이 문제 → `0xc0000033`
  - ② 는 **시작점이 둘** 인 게 문제 → `0xc000003b`

#### 같은 값이 나는 쌍둥이 — 절대 이름인데 `\` 로 안 시작

- 기준 손잡이 **없이** 이름이 `\` 로 안 시작하거나 빈 이름이면 → `+0x144` · `+0x148 JNZ` → 같은 `+0x12ac` `0xc000003b`.
- 이번엔 반대로 "**시작점이 없다**" 가 문제다.

### 2.11 상대 이름 `Key\` — 같은 담당자, 같은 줄 `[정적]`

- `ObpLookupObjectName` (손잡이 기준이고 첫 글자가 `\` 가 아닐 때):
  - `+0x20a` · **`+0x20d MOVAPS [RSP+0x70],XMM1`** — 남은 이름 = **이름 전체**.
  - `+0x226 JMP` → 반복문 → `+0x8bb` · **`+0x8ce CmpParseKey`** — 절대 이름과 **같은 자리** 에서 부른다.
- 절대와 상대의 차이는 딱 하나다:

| | `CmpParseKey` 가 받는 남은 이름 |
|---|---|
| 절대 | `\Machine\…\Key\` (`\` 로 시작) |
| 상대 | `Key\` (`\` 로 안 시작) |

- 끝 `\` 는 둘 다 그대로 넘어가고, `CmpParseKey` 가 **같은 줄** (`+0xf7 ~ +0x4c0`) 로 `Key` 로 만든다.
- `CmpDoParseKey` 안에서도 "기준이 루트 키냐 아니냐" (`+0x1d6 ~ +0x1e5`, `+0x1e5 JNZ +0x4bf`) 만 다르고, `R15 == 0` 이면 둘 다 `+0x1f4` → `+0x217` 로 같이 간다 (`+0x4bf TEST R15,R15` · `+0x4c2 JZ +0x1f4`).
- 그래서 상대 `Key\\` 도 연다 (`+0x603` 되돌아가기).

#### 덤 — 상대 빈 이름 → 같은 키를 다시 연다

- `+0x1eb` · `+0x1ef` — 이름 길이 0 이면 `+0x1744`.
  - 기준이 **폴더 객체** 면 따로 처리한다 (`+0x1744` · `+0x1760`).
  - 아니면 `+0x203` 으로 돌아가 **길이 0** 으로 `CmpParseKey` 에 간다.
- 그다음은 2.8 — 조각 0 개 → **같은 키를 다시 연다** (새 손잡이).

#### 덤 — 이름과 상관없는 비교 하나

- 손잡이 기준일 때 `+0x1dc` · `+0x1e1 JNZ` → `+0x1ddc ~ +0x1e04` 에서 `0xc0000022` 가 날 수 있다 `[정적]`.
- 뜻은 "손잡이가 허용한 접근 vs 요청한 접근" 으로 읽었다 `[추측]`. 이번 질문 (`\` 모양) 과는 상관없다.

### 2.12 링크 (`CurrentControlSet` → `ControlSet001`) 를 따라 다시 풀 때

#### 새 말 — 레지스트리 링크 · 다시 풀기

- `CurrentControlSet` 은 **링크** 다 (run 문서). 진짜 서랍은 `ControlSet001` 쪽이다.
  - 비유: 장부 안에 붙은 **안내판** — "이 서랍은 `ControlSet001` 로 가세요" (run119 의 동네 안내판과 같은 생각).
- 링크를 만나면 이름을 **새로 이어 붙이고**, 새 패스에서 `CmpParseKey` 가 그 이름을 다시 다듬는다 (run 문서). 이번 바퀴는 이걸 **두 번째 패스** 라 부른다.
  - 비유: 안내판을 보고 주소를 **새로 적은 쪽지** 로 다시 찾아간다.

#### 이어 붙인 이름엔 끝 `\` 가 없다 `[정적]`

- `CmpDoParseKey +0xbe1` · `+0x17c1` (그리고 떨어진 조각 `+0x2a7225`) 이 `CmpGetSymbolicLinkTarget` 을 조각 번호 범위로 부른다.
- 그 함수는 링크 대상 뒤에 조각마다 **`\` 한 글자** (`+0x35b` · `+0xc55`) + **조각 이름** (`+0x77d` · `+0xc36 CmpGetComponentNameAtIndex`, `+0xc62 RtlUnicodeStringCat`) 만 붙인다.
- **끝에 `\` 를 덧붙이는 줄이 없다.** 이미 다듬어진 이름의 **조각 배열** 에서 다시 만들기 때문이다.
  - 비유: 지우개질한 사본을 칸칸이 끊어 둔 카드들을 "`›` + 카드" 순서로만 다시 붙이니, 끝에 `›` 가 남을 일이 없다.
- 성공하면 `CmpDoParseKey +0xc22` 가 status `0x104` 를 정한다 `[정적]`. "다시 풀어 주세요" (STATUS_REPARSE 꼴) 로 읽힌다 `[추측: 이름]`. 새 패스에서 `CmpParseKey` 가 다시 다듬는다.

#### 아직 모르는 갈래 — `R15 ≠ 0` (flow-writer 보탬)

- 두 번째 패스가 `CmpDoParseKey` 의 **`R15 ≠ 0` 갈래** (`+0x1ee JNZ +0x4f4` ~ `+0x5d4`) 를 탈 수도 있다.
- 그 갈래는 `CmpParseKey` 가 다듬은 이름이 아니라, **다른 이름에서 접두를 뗀 나머지** 로 조각을 나눈다:
  - `+0x53c RtlPrefixUnicodeString` (대소문자 무시) 로 접두인지 보고,
  - **앞 `\` 만** 떼는 루프 (`+0x580 ~ +0x59c`) 를 돈 뒤 `+0x5d4 CmpComputeComponentHashes`.
  - 이 구간에 **끝 `\` 를 떼는 줄이 없다** (`CmpDoParseKey.asm` 의 `0x5c` 비교는 `+0x580` 하나뿐) `[정적]`.
- 그래서 단계로:
  1. 근거: 이 갈래는 끝 `\` 를 안 뗀다 `[정적]`.
  2. 그래서: 이 갈래에 들어오는 이름이 끝 `\` 를 지니면, 2.6 의 읽는 사람이 **길이 0 마지막 조각** 을 만들 수 있다 `[추측]`.
  3. 아직 확인 아님: 실제로 이 갈래를 타는지, 그 이름에 끝 `\` 가 있는지는 실행해 봐야 안다 → Q251 · Q252 (4절).
- 결과서 쪽 읽기: 그 "다른 이름" 이 새로 이어 붙인 이름이라면 끝 `\` 가 없을 것이다 — 이것도 확인 전이다.

### 2.13 만들기 (`NtCreateKey`) 도 같은 파싱 `[정적]`

- 길: `NtCreateKey +0x4a` (콜아웃 `CmCreateKeyCallout`) · `+0x8a KeExpandKernelStackAndCallout` → `CmCreateKey` → 조각 `FUN_14099849f +0xa6` (= `CmCreateKey +0x3d5`) `ObOpenObjectByNameEx` → `ObpLookupObjectName` → `CmpParseKey` → `CmpDoParseKey`.
- `CmCreateKey` 에 이름 `\` 검사 줄이 없다 (grep).
- 마지막 조각이 없으면 `CmpDoParseKey` 가 `CmpCreateChild` 를 부른다 (`+0x159c` · `+0x1cb3` · `+0x1fa2`).
- 끝 `\` 는 `CmpParseKey` 가 먼저 떼므로 **만들 조각은 그 앞 이름** 이다.
  - (이름 예는 이 글이 든 것) `…\Control\NewKey\` 라면 `NewKey` 를 만드는 셈이다.
- 비유: "새 서랍 만들어 주세요" 주문서도 같은 창구를 지난다. 끝 `›` 는 지워지고, 마지막 칸 이름으로 서랍을 만든다.
- 앞단 넷 (`NtOpenKeyEx` · `CmOpenKey` · `NtCreateKey` · `CmCreateKey`) 엔 `0x5c` · `0xc0000033` · `0xc000003b` 가 **하나도 없다** (grep 0). 이름 모양 검사는 전부 뒤 (안내 데스크 · 담당자) 에서 한다.

### 2.14 PE_emulator 쪽 — 무엇이 틀렸나 (결과서 "지금 우리 맞나")

- `../PE_emulator` 코드는 flow-writer 가 안 읽었다. 고칠지는 **사용자 결정** 이다.

| 에뮬레이터 자리 (결과서) | 판정 | 까닭 |
|---|---|---|
| `_registry_common.hpp` `resolve_absolute` (끝 `\` · 가운데 빈 조각 → `0xc0000033`) | **틀림** | 진짜는 끝 · 앞 `\` 전부 떼고, 가운데는 한 칸. 맨 앞 `\\Registry` 만 진짜도 `0xc0000033` |
| `nt_open_key.cpp:47 ~ 48` 상대 갈래 (끝 `\` **하나만** 뗌) | **결과 반은 맞고 규칙이 좁음** | `Key\` 맞음. `Key\\` · `Control\\Key` 는 진짜는 열림 |
| 상대 첫 글자 `\` (`registry_path_syntax_unverified` 로 멈춤) | 닫을 수 있음 | 진짜는 `0xc000003b` (2.10) |
| 상대 빈 이름 → 시작 키 | **맞음** | 2.8 |
| `nt_create_key.cpp:48` · `:100` · `:72` | **틀림** | 만들기도 같은 `CmpParseKey` (2.13) |
| 홀수 길이 `nt_open_key.cpp:31` | **맞음** | 2.9 |

- 고칠 때 규칙 (결과서 8 — 위 줄에서 나온 차례):
  1. 이름 → 절대면 `\Registry` 뒤 나머지, 상대면 통째
  2. 끝 `\` **전부** 뗌
  3. 앞 `\` **전부** 뗌
  4. 가운데 연속 `\` 는 **한 칸**
  5. 조각 0 개면 **시작 키**
  6. 조각 32 개 넘음 · 조각 길이 256 글자 넘음 → `0xc000000d`
- 없는 키 `0xc0000034` 쪽은 범위 밖 (Q252).

### 2.15 flow-writer 대조 — 결과서와 다른 곳 (뜻은 같음)

결과서 오프셋은 `.asm` 과 **모두 맞았다.** 아래는 표기 · 셈 · 보탬이다.

- **멈춤점 `+0x5d9` 는 ret 자리다.** call 은 `+0x5d4`.
  - 넘기는 이름을 보려면 `+0x5d4` (call 자리 = 인자 보기), 돌아온 status · 조각 수를 보려면 `+0x5d9` (ret 자리 = 결과 보기).
  - 같은 결과서의 `+0x217` (call) · `+0x21c` (ret) 은 맞게 적혔다.
- **`0xc0000034` 는 23 줄** — status 칸 `[RSP+0x64]` 에 쓰는 줄 **10**, 같은 값을 `R8D` 에 싣는 줄 **12**, `+0x8e1 CMP EAX,0xc0000034` (`CmpWalkOneLevel` ret `+0x8dd` 바로 뒤) **1**. 최종 줄 고르기는 `+0x8e1` 부터 보면 될 것 `[추측]`.
- (보탬) **`R15 ≠ 0` 갈래는 끝 `\` 를 안 뗀다** — 2.12.
- (보탬) **`CmpGetSymbolicLinkTarget` 부르는 자리는 셋** — 결과서는 `+0xbe1` · `+0x17c1` 둘만 적었다. 셋째 `+0x2a7225` (ret `+0x2a722a`) 는 함수 크기 11350 B **밖** 의 떨어진 조각이다. 어느 갈래인지는 안 읽음 `[공백]`.
- (보탬) `CmpGetSymbolicLinkTarget +0x9cd` 도 `CmpComputeComponentHashes` 를 부른다 (ret `+0x9d2`) — 링크 대상 이름을 조각 내는 자리로 보인다 `[추측]`.
- (표기) `CmpGetNextName +0x8f ~ +0x9f` 는 출력 Length · MaximumLength 를 같은 값으로 쓴다. 결과서는 "출력 Length" 로만 적었다.

### 2.16 정리 — 비유로 한 번 더

- 손님이 "장부 › 서랍장 › 칸 › 상자 ›" 처럼 끝에 `›` 를 하나 더 붙인 주소를 냈다.
- **동네 안내 데스크** 는 `\Registry` (장부 건물) 까지만 데려가고, 건물 안 주소는 **건물 담당자** 에게 통째로 넘긴다. 안내 데스크는 끝 `›` 를 안 본다.
- **건물 담당자** (`CmpParseKey`) 는 주소 쪽지 **사본** 의 양 끝 `›` 를 안 보일 때까지 지운다. 거절은 안 한다.
- **주소 읽는 사람** (`CmpComputeComponentHashes`) 은 가운데 `››` 를 한 칸 구분으로 읽는다. 32 칸 넘거나 한 칸이 너무 길 때만 돌려보낸다.
- 그래서 끝 `›`, 끝 `››`, 가운데 `››` 는 모두 **같은 상자** 를 찾는다.
- 돌려보내는 건 둘뿐:
  - 동네 길에서 이미 빈 칸 (`››Registry`) → 안내 데스크가 `0xc0000033`.
  - "이 서랍부터" 라면서 "꼭대기부터 (`\`)" 로 쓴 주소 → 안내 데스크가 `0xc000003b`.
- 요청서가 짚은 **우편 분류기** (`CmpGetNextName`) 는 관리실 안 **고정 주소록** 을 읽는 도구라, 손님 주소와는 상관이 없었다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 것 (`git diff -- runs/03_sprint/flow.md`)

**그림은 그대로다.** 커널 안 레지스트리 이름 풀이는 대표 지점이 아니라서다. 짐 지도도 안 건드렸다. 이전 run 정정도 없다.

| 자리 | 전 | 후 |
|---|---|---|
| 기준 (ntkrnlmp 줄 · 근거 줄) | 앞 run 들 | run134 를 더함 (다시 analyze 안 함, 레지스트리 이름 풀이 함수들 export) |
| 2.1 run47 사슬 | `ObpLookupObjectName` → `IopParseDevice` 직접 호출 꼴만 | 한 줄 더: 레지스트리 키면 `+0x871 LEA` · `+0x878 CMP` → **`+0x8ce CmpParseKey` 직접 호출**, 같은 꼴 `[정적]` |
| 2.4 run58 레지스트리 줄 | `RtlpQueryNlsSystemCodePages` 의 `NtOpenKey` 이름까지 | 한 줄 더: 커널 안에선 `NtOpenKey` → … → `CmpParseKey` → `CmpDoParseKey` 로 풀림 `[정적: 같은 코드, 이 이름으로 실행 본 적 없음]`. `CurrentControlSet` 은 링크라 두 번째 패스가 있다 `[추측]` |
| 3장 끝 | 없음 | **새 절 "레지스트리 키 이름 풀이 (끝 `\`)"** — 나무, 비유, `[정적]` 줄 여섯 + `[정적 + 추측]` 두 번째 패스 한 줄 |
| 4장 | Q247 열림 | **Q247 닫힘 줄** + run134 새 줄 셋 (동적 Q251 · 정적 Q252 · PE_emulator 쪽) |
| 5장 | — | run134 두 줄 |

### 큰 그림에서 어떻게 이어지나

- **test.exe 쪽**: 초기화의 `NtOpenKey` (run58 NLS 코드 페이지 키) 가 커널 안에서 어떤 사슬을 타는지가 정적으로 이어졌다. 그 이름은 끝 `\` 가 없어서 떼기 루프는 안 돈다.
- **run47 과의 연결**: "안내 데스크가 건물 종류별 담당자를 **직접** 부른다" 는 꼴이 파일 (`IopParseDevice`) 에 이어 레지스트리 키 (`CmpParseKey`) 에서도 보였다. 같은 함수 `ObpLookupObjectName` 의 다른 자리다.
- **PE_emulator 쪽**: 로케일 묻기마다 받던 `0xc0000033` 이 **틀린 대답** 이라는 정적 근거가 생겼다. 고칠 규칙 다섯 줄 (2.14) 이 나왔다. 상대 갈래의 `[추측]` + `TODO(VM)` 도 **정적 근거까지는** 닫을 수 있다 (동적 확인은 Q251).
- 요청서의 길 가정 (`CmpGetNextName`) 이 틀렸다는 것도 같이 남겼다. 다음 레지스트리 질문이 같은 함수를 기준으로 삼지 않게.

---

## 4. 아직 모르는 것

### 실행해 봐야 아는 것 → **Q251** (동적, VM 묶음, 미룸)

- **무엇을**: 아래 넷이 진짜로 다 성공하나 (정적 예상: 넷 다 성공).
  1. 절대 `\Registry\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\`
  2. 절대 끝 `\\`
  3. 절대 가운데 `\\` (`…\Control\\CommonGlobUserSettings`)
  4. 상대 (`…\Control` 손잡이 + `CommonGlobUserSettings\`)
  - 덤: 상대 `\Key` → `0xc000003b`, 상대 빈 이름 → 성공 (새 손잡이).
- **두 번째 패스**: 링크를 따라간 뒤 `R15 ≠ 0` 갈래를 타는지, 탄다면 조각 끝에 길이 0 조각이 생기는지 (2.12).
- **왜 이번에 못 알았나**: 정적만 한 바퀴다. "실행 때 이 길로 간다" 와 두 번째 패스의 갈래는 실행 때 값에 달렸다.
- **어떻게**: test.exe 가 아니라 이름 꼴만 바꿔 `NtOpenKey` 를 부르는 작은 프로그램 (고르기는 사용자 결정). 커널 멈춤점은 시스템 전체 키 열기가 다 지나가니 **그 프로세스 조건 필수**.
- 멈춤점 (모두 `nt!`) — 인자는 call 자리, 결과는 ret 자리에서 본다:
  - `CmpParseKey` 입구 — 남은 이름에 끝 `\` 가 아직 있나
  - `CmpDoParseKey+0x217` (call) — 다듬은 이름
  - `CmpDoParseKey+0x21c` (ret) — status · 조각 수 (1번은 첫 패스 5 예상)
  - `+0x5d4` (call, 이름) / `+0x5d9` (ret, 조각 수) — 두 번째 패스
  - `+0x2a7225` 는 `ln` 으로 먼저 확인 (떨어진 조각이라 WinDbg 가 다른 이름으로 보일 수 있다 `[추측]`)

### 정적으로 남은 것 → **Q252** (정적, 낮음)

| 무엇을 모르나 | 왜 못 알았나 |
|---|---|
| ① 길이 0 조각이 `CmpWalkOneLevel` (rva `0x85b140`) 에 들어가면 어떻게 되나 + `CmpPerformCompleteKcbCacheLookup` | 이번 이름 꼴들은 이 길에 안 닿아서 안 읽었다. 다만 두 번째 패스 `R15 ≠ 0` 갈래는 끝 `\` 를 안 떼니 닿을 수 있다 `[추측]` |
| ② 없는 키에서 `0xc0000034` 가 나오는 **최종 줄** · 중간 키 없음 (`0xc000003a`?) 구별 · 만들기 `CmpCreateChild` 앞 조건 (`0xc0000035`) | status 칸에 쓰는 자리가 10 곳이라 갈래를 나눠야 한다. 요청 범위 밖. `+0x8e1` 부터 보면 될 것 `[추측]` |
| ③ `CmpParseKey +0x45a ~ +0x495` — 루트 바로 아래 한 글자 이름 (`\Registry\X…`) → `0xc0000022` (`[R14] & 0x40` 의 뜻) | 이번 질문 밖. 에뮬레이터가 루트 바로 아래 이름을 받을 때 영향이 있을 수 있다 `[추측]` |
| ④ 셋째 링크 자리 `CmpDoParseKey +0x2a7225` 가 어느 갈래인가 | 떨어진 조각이라 안 읽었다 `[공백]` |

### 대기열 밖 — 짐작으로 남은 것

- `KeExpandKernelStackAndCallout` 안 — 콜아웃 → `CmOpenKey` / `CmCreateKey` 는 결과서가 `.c` 머리 `callers:` 로 이었다 `[공백]`.
- status `0x104` 의 이름 (STATUS_REPARSE 꼴) `[추측: 이름]`.
- `0xc0000022` 접근 비교의 뜻 (2.11) `[추측]`.

---

## 5. 더 읽기

- run 문서: [run134-reg-trailing-sep.md](run134-reg-trailing-sep.md)
  - "정적 — 호출 지점" 표 (call · ret 전부)
  - "표: 이름 꼴 → 결과" (이 글 2.7 · 2.9 · 2.10 · 2.11 의 원본)
  - "flow-writer 대조" (이 글 2.15)
  - "PE_emulator 쪽" (이 글 2.14)
- 요청서: [`_workspace/sast/131_reg-trailing-sep.md`](../../_workspace/sast/131_reg-trailing-sep.md)
- 결과서: [`_workspace/sast/131_reg-trailing-sep_result.md`](../../_workspace/sast/131_reg-trailing-sep_result.md) — "먼저 읽는 한 줄 답", "`CmpGetNextName` 규칙", "동적으로 확인할 것"
- flow.md: [3장 끝 "레지스트리 키 이름 풀이 (끝 `\`)"](flow.md), 2.1 (run47 사슬 줄), 2.4 (run58 레지스트리 줄), 4장 Q247 · run134 줄
- 앞 해설:
  - [run47 해설 2.10](run47-section-chain-해설.md#210-대출-카드-사슬-이름-따라가기와-parse-프로시저) — 안내 데스크 · 건물 담당자 (`IopParseDevice`)
  - [run58 해설](run58-registry-strings-해설.md) — 설정 장부 · test.exe 가 여는 키 이름
  - [run119 해설](run119-systemroot-link-해설.md) — 동네 · 안내판 (객체 이름 공간의 링크)

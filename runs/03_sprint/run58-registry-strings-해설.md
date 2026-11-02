# run58 registry-strings — 해설

> 이 글은 [run58 문서](run58-registry-strings.md)를 쉬운 말로 풀어 쓴 것이다. 새 사실은 없다.
> 근거와 태그는 run 문서·결과서를 따른다.

- 대기열: **Q109 첫 일 (ⓠ-1·ⓠ-2)**: PE_emulator 요청 4 의 ⓠ — 레지스트리 키·값 이름 **글자 전체** (`_workspace/plan.md`, 이번에 ⓠ 만 **끝**)
  - 사용자 결정 (2026-11-02) 으로 Q109 안에서 맨 앞으로 옮긴 일이다. PE_emulator 가 이걸 기다리고 있었다.
  - Q109 의 나머지 (B ntdll 본문 · C ntoskrnl 비트 뜻 · D (1)~(3) 기록 정정 · E) 는 **다음 바퀴**.
  - 다음 차례: Q109 나머지 → Q111 → Q104 VM 단계 → Q78d4 → …
  - 남은 질문이 간 곳: 선택 동적 확인 → **Q110** 출처에 더함, 레지스트리 실제 값 → **Q104 VM 단계**, 작은 정적 셋 → **Q112** (4절).
- 근거:
  - run 문서 [`run58-registry-strings.md`](run58-registry-strings.md) (가장 중요)
  - 요청서 [`_workspace/sast/53_registry-strings.md`](../../_workspace/sast/53_registry-strings.md)
  - 결과서 [`_workspace/sast/53_registry-strings_result.md`](../../_workspace/sast/53_registry-strings_result.md) (판정: **분석성공**, PDB 일치)
  - flow.md 변경: 기준 줄 · 2.4 트리 한 줄 + 새 줄 하나 · 2.4 NLS 절 · 2.4 프로세스 힙 절 "안 타는 갈래" · 4장 두 줄 · 5장
  - baggage.md 변경: 새 짐 칸 없음. 근거 칸 덧붙임 (3.1 · 6장 · 7.6.3 · 7.18) — run 문서 "짐 지도 반영"
- 이번 바퀴는 **정적만** 했다. 디버거는 안 붙였다 (dast 없음). Ghidra 도 **한 번도 안 돌렸다** — 세 함수 `.asm`·`.c` 가 이미 있었다.
  - `[관찰]` 로 적힌 값은 run44 사진을 **다시 읽은 것** 이다. 새 실행이 아니다.
- 결과서와 run 문서가 다른 곳 (오프셋 줄만, 뜻은 같음) 은 2.15 에 모았다. 이 글은 **run 문서 숫자**를 따른다.

기초 말은 다시 안 푼다. **링크만** 건다.

- 오프셋, call/ret, 콜스택, 태그, 시스템 콜 → [run9 해설](../02_sprint/run9-create-user-process-해설.md) 2절
- `UNICODE_STRING` = "몇 글자, 어디부터" 를 적은 **쪽지** `{Length, MaximumLength, Buffer}` → [run19 해설 2.1](../02_sprint/run19-proc-init-modules-해설.md#21-run18-정정-하나--exe-폴더가-아니라-exe-파일-이름)
- 레지스트리 = 관리실 **설정 장부** → [run20 해설](../02_sprint/run20-kernel32-load-해설.md) 2.8
- KUSD (`KUSER_SHARED_DATA`) = 로비의 **공용 게시판** → [run34 해설](../02_sprint/run34-parent-leftovers-해설.md) 2.5
- 코드 페이지 (**몇 번 글자 사전**), `LdrpInitializeNlsInfo` 가 값을 고르는 순서, 매니페스트 → [run57 해설 2.8](run57-nls-apiset-해설.md)
- syscall 번호 (**단축 번호**), ntdll 스텁 (**전화기**) → [run56 해설](run56-syscall-args-해설.md)
- 사진 K1 · P1a … → [run44 해설 2.8](run44-baggage-snap-해설.md)

이번에 처음 나오는 말 (UTF-16LE, NUL, `.rdata`, `wcslen`, 즉치, 공용 꼬리, `REG_SZ`, 값 정보 클래스 등) 은 **본문에서 처음 나오는 자리**에서 한 줄로 푼다.

### 비유 — 그대로 이어 쓴다

- 커널 = **관리사무소**, ntdll = 가게 안 **관리실**, 프로세스 = **가게**, PEB = 가게 **안내판**.
- 시스템 콜 = 관리사무소 **신청 창구**, ntdll 스텁 = 관리실 **전화기** (run56).
- 레지스트리 = **설정 장부** (run20). 관리실은 장부를 직접 못 펼치고, 창구 (`NtOpenKey` · `NtQueryValueKey` · `NtClose`) 로 **빌려 본다**.
  - run 문서는 이번에 "본사 장부" 라고 불렀고, run57 해설은 "동네 규칙" 이라고 불렀다. 셋 다 같은 것이다. 이 글은 **설정 장부** 로 쓴다.
- run 문서가 이번에 쓴 그림 (그대로 가져온다):
  - 키 = 장부의 **서랍 이름**, 값 = 서랍 안 **칸 이름**.
  - 그 이름들은 관리실 **업무 수첩 맨 뒤에 인쇄된 글** (`.rdata`) 에 그대로 박혀 있다. 이번에 그 글을 한 글자씩 읽었다.

### 한 줄로 먼저 — 무엇을 얻었나

| 질문 | 답 (run 문서 그대로) |
|---|---|
| NLS 코드 페이지 키 이름 전체는 | **`\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage`** (63자). 값은 **`ACP`**, **`OEMCP`** `[정적]` |
| 제품 종류 키 · 값 · 비교 글은 | 키 **`\Registry\Machine\System\CurrentControlSet\Control\ProductOptions`** (65자), 값 **`ProductType`**, 비교 글 **`WinNt` / `LanmanNt` / `ServerNt`** `[정적]` |
| 글은 어떤 모양인가 | 여덟 개 모두 ntdll `.rdata` 의 **UTF-16LE + 끝 NUL** `[정적]` |
| 쪽지 길이는 어디서 정하나 | ⓠ-1 은 **실행 중 `wcslen`**, ⓠ-2 는 **명령에 박힌 상수** (`MOV qword`). 값은 같은 규칙 `[정적]` |
| ⓠ-1 이 돌려주는 값은 | **늘 0** (`+0x226`). 실패는 두 번호 칸이 `0xfde9` 인 것으로만 드러난다 `[정적]` |
| ⓠ-2 실패 때 `*type = 1` 은 누가 쓰나 | 이 함수가 아니라 **부른 쪽 `RtlGetNtProductType +0x69`** `[정적]` |

이 글에서 붙잡아 둘 세 줄:

> **(1) PE_emulator 가 알던 "길이 `0x80`" 은 NUL 까지 센 길이 (`MaximumLength`) 다. 글자만 센 길이 (`Length`) 는 `0x7e` 다.**
>
> **(2) 두 함수는 쪽지 길이를 다르게 정한다 (자로 재기 vs 미리 새긴 숫자). 결과 숫자는 같다. 다만 "장부에서 읽어 온 값 글" 의 길이도 서로 다르게 잰다.**
>
> **(3) 표 안의 `+0x..` 는 CALL 자리 · ret 자리 · 쓰는 줄이 섞여 있다. 콜스택·멈춤점은 ret, `.asm` 줄 찾기는 CALL 이다.**

---

## 1. 이번 바퀴의 질문

### 큰 그림에서 어디인가

이번 두 함수는 둘 다 **test.exe 첫 스레드의 로더 시작 (flow.md 2.4)** 안에 있다.

```
cmd.exe 의 부탁 → 커널 (NtCreateUserProcess …)   ← 이번엔 안 봄

test.exe 첫 스레드 (그림 2, ②)
 LdrInitializeThunk → … → LdrpInitializeProcess
   ├─ +0x1cd LdrpInitializeNlsInfo
   │    └─ +0x42 RtlpQueryNlsSystemCodePages     ← 이번 ⓠ-1 (test.exe 가 탄다 — run57 [정적] 판정)
   ├─ … 프로세스 힙 구간 (2.4 프로세스 힙 절, run46)
   │    └─ RtlGetNtProductType
   │         └─ +0x62 RtlpGetNtProductTypeFromRegistry   ← 이번 ⓠ-2 (test.exe 는 안 탄다 — [관찰] run44)
   └─ …
 → (CRT) → main
```

- ⓠ-1 = "이 가게는 **몇 번 글자 사전**을 쓰나" 를 설정 장부에서 찾아보는 함수.
- ⓠ-2 = "이 건물은 **어떤 종류**인가 (보통 / 서버 …)" 를 설정 장부에서 찾아보는 함수.
- 둘 다 장부를 여는 모양은 같다: 서랍 열기 → 칸 읽기 → 서랍 닫기.

### 왜 이 질문이 지금 왔나

- `../PE_emulator` 가 **레지스트리 값 읽기 흉내**를 만들고 있다.
  - 진짜 장부가 없으니, ntdll 이 `NtOpenKey`·`NtQueryValueKey` 를 부르면 에뮬레이터가 대신 답을 꾸며 줘야 한다. run 문서·요청서는 이 부분을 **"덫"** 이라고 부른다.
  - 그러려면 ntdll 이 **정확히 어떤 이름**을 들고 오는지 알아야 한다. 글자 하나, 대소문자 하나, 길이 숫자 하나가 다르면 "다른 서랍" 이 된다.
- 그런데 지금까지는 **글자 일부와 짐작**만 있었다 (요청서 "왜").
  - ⓠ-1 키는 앞 15자 `\Registry\Machi` 와 길이 `0x80` 만 알았다.
  - ⓠ-2 글은 sast/40 (run46) 에 적혀 있었지만, 디컴파일 글에서 읽었는지 바이트에서 읽었는지 안 적혀 있었다.
- 그래서 이번 목표는 **"`[정적] run58` 이라고 근거를 달 수 있는 글자 전체"** 를 남기는 것이다.

### 무엇이 궁금했나

요청서 질문을 둘로 묶으면:

1. **ⓠ-1 `RtlpQueryNlsSystemCodePages`** (ntdll rva `0xec890`, 606B):
   - 세 글 (`0x180ad0` · `0x180b50` · `0x180b58`) 의 글자 전체, UTF-16 인가, NUL 이 있나.
   - 쪽지의 `Length`·`MaximumLength` 는 어디서 정하나 — 함수 호출인가, 상수인가, 미리 만든 쪽지인가.
   - syscall 차례가 `NtOpenKey` → `NtQueryValueKey` ×2 → `NtClose` 인가.
   - PE_emulator 기록 확인: (a) **둘째 값만 다르게 처리하나** (`+0x121 → +0x171` vs `+0x1cf → +0x219`), (b) **모든 출구가 `+0x226 XOR EAX,EAX`** 로 모이나 (= 늘 0 을 돌려주나).
2. **ⓠ-2 `RtlpGetNtProductTypeFromRegistry`** (ntdll rva `0x5abd4`, 460B):
   - 글 다섯의 rva · 글자 전체 · 길이 출처.
   - syscall 차례. 주소로는 `NtClose +0xe5` 가 `NtQueryValueKey +0x138` 보다 **앞**인데, 실제로 먼저 닫나?
   - 실패 때 `*type = 1` 이 맞나, 누가 쓰나. 비교는 대소문자를 무시하나.

### 바퀴 전에 알던 것

- `[정적]` (run57, sast/52) `LdrpInitializeNlsInfo +0x3d` (ret `+0x42`) 가 `RtlpQueryNlsSystemCodePages` 를 부른다. 매니페스트 코드 페이지가 없을 때만이고, test.exe 는 이 길이다.
  - 그 함수가 "레지스트리에서 코드 페이지 번호를 읽는다" 는 것까지는 알았다. **어떤 글을 들고 가는지**는 run57 에 안 적혔다.
- `[정적]` (run46, sast/40) `RtlGetNtProductType +0x5d` → `RtlpGetNtProductTypeFromRegistry`. 이 길은 KUSD `ProductTypeIsValid` 가 0 이고 `PEB.SharedData` 가 비었을 때만 탄다.
  - sast/40 이 `NtOpenKey +0xd1` · `NtQueryValueKey +0x138` · `NtClose +0xe5` 를 적었지만 **순서**는 안 적었다.
- `[관찰]` (run44 P1a 사진) KUSD `ProductTypeIsValid` = 1 → test.exe 는 제품 종류 레지스트리 길을 **안 탄다**.
- PE_emulator 쪽 단서 (그쪽 `[정적]` 기록): ntdll PDB 에 글 상수 심볼 `??_C@…` 이 세 rva 에 있고, 키 이름 심볼 지문 (CRC) `0x83e3bb6f`, 길이 `0x80`.

---

## 2. 찾은 것

### 2.1 한눈에 — 글 8개와 두 함수 (대표만)

글 표 (run 문서 1절에서 칸 몇 개만 골랐다. 전체 표·바이트는 run 문서 1절):

| # | rva | 글자 전체 | 글자 수 | `Length` / `MaximumLength` | 어디서 정하나 |
|---|---|---|---|---|---|
| 1 | `0x180ad0` | `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` | 63 | `0x7e` / `0x80` | 실행 중 `wcslen` (`+0x60`) |
| 2 | `0x180b50` | `ACP` | 3 | `6` / `8` | 실행 중 `wcslen` (`+0xd3`) |
| 3 | `0x180b58` | `OEMCP` | 5 | `10` (`0xa`) / `12` (`0xc`) | 실행 중 `wcslen` (`+0x180`) |
| 4 | `0x17ab80` | `\Registry\Machine\System\CurrentControlSet\Control\ProductOptions` | 65 | `0x82` / `0x84` | 상수 `+0x9d` |
| 5 | `0x17ab28` | `ProductType` | 11 | `0x16` / `0x18` | 상수 `+0xa5` |
| 6 | `0x17ab40` | `WinNt` | 5 | `0xa` / `0xc` | 상수 `+0xbd` |
| 7 | `0x17ab50` | `LanmanNt` | 8 | `0x10` / `0x12` | 상수 `+0xad` |
| 8 | `0x17ab68` | `ServerNt` | 8 | `0x10` / `0x12` | 상수 `+0xb5` |

- 모두 `[정적]`. 모두 UTF-16LE + NUL, 구간 `.rdata`.
- #1 ~ #3 은 ⓠ-1 이, #4 ~ #8 은 ⓠ-2 가 쓴다.

두 함수 트리 (run 문서 "호출 관계" 에서 대표만. **라벨은 ret 자리**):

```
LdrpInitializeNlsInfo
 └─ +0x42  RtlpQueryNlsSystemCodePages                ← RCX = &ACP, RDX = &OEMCP
          │  +0x2f · +0x56  두 칸 = 0xfde9 (기본값)
          ├─ +0x65   wcslen   키 #1 → Length 0x7e / Max 0x80 (+0xa3 / +0xab)
          ├─ +0xbc   NtOpenKey        …\Control\Nls\CodePage
          ├─ +0x113  NtQueryValueKey  ACP   (클래스 2, 버퍼 0x24)
          ├─ +0x1c5  NtQueryValueKey  OEMCP
          └─ +0x222  NtClose
             +0x226  XOR EAX,EAX (늘 0) → +0x24f RET

RtlGetNtProductType
 └─ +0x62  RtlpGetNtProductTypeFromRegistry          둘 다 못 쓸 때만
          │  +0x9d ~ +0xbd  쪽지 머리 5개 = MOV qword 상수 (#4 ~ #8)
          ├─ +0xd6   NtOpenKey        …\Control\ProductOptions
          ├─ +0x13d  NtQueryValueKey  ProductType
          ├─ +0x179 · +0x18d · +0x1a1  RtlEqualUnicodeString  WinNt · LanmanNt · ServerNt → *type = 1 · 2 · 3
          └─ +0xea   NtClose (CALL +0xe5, 공용 꼬리)
    +0x69  실패면 *type = 1 (부른 쪽)
```

- 모두 `[정적]`. 빠진 줄 (값 글 `wcslen` · 번호 변환 · 쿠키 확인 등) 은 run 문서 "호출 관계" 와 1절 표.

### 2.2 한 줄씩 읽는 법

#### 한 줄 읽기 ① — 표 #2 `ACP` 줄

`| 2 | 0x180b50 | ACP | UTF-16LE + NUL | 3 | 6 / 8 | 6 / 8 | 런타임: wcslen (+0xd3) ×2 → +0xf1/+0xf9 | 첫째 NtQueryValueKey 의 ValueName |`

- **누가**: ntdll 의 `RtlpQueryNlsSystemCodePages`.
- **어디서**: 글은 ntdll 이미지 rva `0x180b50` (`.rdata`) 에 박혀 있다. 함수는 `+0xd3` 에서 `wcslen` 으로 길이를 재고, `+0xf1` 에 `Length`, `+0xf9` 에 `MaximumLength` 를 쓴다.
- **누구를**: 그 쪽지를 첫째 `NtQueryValueKey` 의 `ValueName` (값 이름) 인자로 넘긴다.
- **무엇을 위해**: 열어 둔 서랍 (`…\Nls\CodePage`) 에서 **`ACP` 칸**을 읽으려고.
- 숫자 읽기: 글자 3개 (`A`·`C`·`P`) × 2바이트 = **6** (NUL 뺀 것), NUL 2바이트까지 = **8**.
- 비유: 관리실이 수첩 맨 뒤에서 "ACP" 를 찾아 **자로 길이를 재고**, 쪽지에 "6칸, 칸 크기 8" 이라고 적어 창구에 낸다.

#### 한 줄 읽기 ② — 트리 `├─ +0x65   wcslen   키 #1 → Length 0x7e / Max 0x80 (+0xa3 / +0xab)`

- **누가 / 어디서**: `RtlpQueryNlsSystemCodePages` 안. 라벨 `+0x65` 는 **ret 자리** (`wcslen` 에서 돌아올 곳) 다. CALL 줄은 `+0x60` (2.8).
- **누구를**: `wcslen` 을 부른다. 인자는 `+0x3c` 에서 넣은 `.rdata` 글 #1 주소 (결과서).
- **무엇을 위해**: 키 이름 쪽지의 길이 두 칸을 채우려고.
  - 돌아오자마자 `+0x65 ADD RAX,RAX` 로 글자 수를 ×2 해서 바이트로 바꾼다 (결과서).
  - `+0xa3` 에 `Length` (`0x7e`), `+0xab` 에 `MaximumLength` (= `Length` + 2 = `0x80`) 를 쓴다.
- 읽는 법 요점: 같은 줄에 **ret 자리 하나 + 쓰는 줄 둘** 이 같이 적혀 있다. 셋은 종류가 다른 오프셋이다 (2.8).

#### 한 줄 읽기 ③ — `RtlpGetNtProductTypeFromRegistry +0x9d MOV qword [RBP-0x59],0x840082`

- **누가 / 어디서**: ⓠ-2 함수의 `+0x9d` 줄. 스택의 `[RBP-0x59]` 칸 = 키 이름 쪽지 자리.
- **무엇을**: 8바이트를 한 번에 쓴다. 숫자 `0x840082` 를 쪽지 머리 모양대로 쪼개면:
  - 아래 2바이트 `0x0082` = `Length` (130)
  - 그 위 2바이트 `0x0084` = `MaximumLength` (132)
  - 맨 위 4바이트 `0` = 빈칸
  - → 표 #4 의 `0x82` / `0x84` 와 같다.
- **무엇을 위해**: `NtOpenKey` 에 넘길 키 이름 쪽지의 머리를 만들려고. 글 주소 (`Buffer`) 는 따로 `+0x54` 에서 넣는다 (`Buffer` 칸 = 머리 + 8).
- 비유: 여기는 **자로 재지 않는다**. 치수가 처음부터 명령에 새겨져 있다 (2.7).

### 2.3 글 표 읽는 법 ① — UTF-16LE 와 NUL `[정적]`

#### 새 말 둘

- **UTF-16LE** = 글자 하나를 **2바이트**로 적고, 두 바이트 중 **작은 쪽을 먼저** 적는 방식.
  - 영어 글자는 뒤 바이트가 `00` 이 된다. 그래서 바이트로 보면 `글자 00 글자 00 …` 처럼 0 이 사이사이 끼어 보인다.
  - 비유: 글자마다 **두 칸짜리 상자**에 넣는데, 영어 글자는 앞 칸만 채우고 뒤 칸은 비워 둔 모양.
- **NUL** = 글 끝 표시. UTF-16 에서는 `00 00` 두 바이트.
  - 비유: 문장 끝 **마침표**. 길이를 따로 안 적어 둔 글은 이걸 보고 끝을 안다.

#### 실제 바이트로 보기

run 문서 1절 바이트 줄에서 짧은 것 둘:

- #2 `ACP` = `41 00 43 00 50 00 00 00`
  - `41 00` = `A`, `43 00` = `C`, `50 00` = `P`, 마지막 `00 00` = NUL.
  - 글자 3개 × 2 = 6바이트, NUL 까지 8바이트.
- #6 `WinNt` = `57 00 69 00 6e 00 4e 00 74 00 00 00`
  - 끝 두 글자가 `4e 00 74 00` = `N` `t` — **대문자 N + 소문자 t** (run 문서·결과서가 따로 짚었다).

#### 대소문자는 왜 짚나

- 요청서가 "`System` vs `SYSTEM` 등 대소문자 그대로" 를 부탁했다.
  - 바이트로 읽은 결과: 키는 `System` · `Control` · `Nls` · `CodePage`, 값은 `ACP` · `OEMCP` (모두 대문자) `[정적]`.
  - 비교 글은 `WinNt` / `LanmanNt` / `ServerNt` — 끝이 `NT` 가 아니라 `Nt` `[정적]`.
- 결과서 덧말: 장부의 실제 값은 `WinNT` 처럼 보일 수 있지만, ⓠ-2 비교가 대소문자 무시라 상관없다 (2.13).

### 2.4 글 표 읽는 법 ② — `Length` vs `MaximumLength` `[정적]`

#### 두 칸이 뜻하는 것

쪽지 (`UNICODE_STRING`) 에는 길이 칸이 **둘** 있다. 이번 표에서 둘은 늘 2 차이다.

- **`Length`** = 글자만 센 바이트 수. **NUL 은 안 센다.**
- **`MaximumLength`** = 글이 들어 있는 상자의 바이트 크기. 이번 글들은 **NUL 까지** 들어간 크기다.
- 비유: `Length` 는 "글씨가 쓰인 칸 수", `MaximumLength` 는 "종이 전체 칸 수". 이번 종이들은 글씨 끝에 마침표 한 칸만 더 있다.

#### PE_emulator 가 알던 "`0x80`" 은 어느 쪽인가

- 요청서: PE_emulator 는 키 #1 길이를 `0x80` 바이트로 알고 있었다.
- 결과서·run 문서: 그건 **NUL 포함 길이** = `MaximumLength`. 글자만 센 `Length` 는 **`0x7e`** (126) `[정적]`.
  - 63자 × 2 = 126 = `0x7e`, NUL 2바이트 더해 128 = `0x80`.
- PDB 심볼 쪽도 같은 얘기를 한다: 심볼 이름 속 길이 `IA` = `0x80` 은 NUL 을 넣은 길이다 (2.6).

#### 어디에 끼우나 (이유와 함께, 짐작)

- `[추측]` (내 짐작) PE_emulator 가 "들어온 키 이름이 이 서랍인가" 를 맞춰 볼 때는 **`Length` 쪽 (`0x7e`) 만큼의 글자**를 대 봐야 한다.
  - 이유: `Length` 가 "진짜 글자" 범위다 (위 정의). `0x80` 으로 대면 NUL 까지 비교하게 된다.
  - 이건 해설이 덧붙인 생각이고, run 문서·결과서에 적힌 말은 아니다.

### 2.5 글 표 읽는 법 ③ — 글이 놓인 자리 `[정적]`

- **새 말 — `.rdata`** = 실행 파일 안의 **읽기 전용 데이터 구간**. 프로그램이 바꾸지 않는 상수 (글 등) 가 들어간다.
  - 비유: 관리실 업무 수첩 **맨 뒤에 인쇄된 페이지** (run 문서 그림). 연필로 고쳐 쓰지 않는다.
- **rva** = ntdll 이미지 시작에서 잰 거리 (run 문서 범례).
  - 이번엔 `.rdata` 의 rva `0x173000` = 파일 오프셋 `0x173000` 이라 **rva 가 곧 파일 오프셋** 이다 (결과서). 그래서 `ms/bin` 의 ntdll.dll 파일을 그대로 읽어도 같은 바이트가 나온다.
- 자리 모양 (run 문서 1절):
  - #1 (`0x180ad0`..`0x180b4f`) **바로 뒤에** #2 (`0x180b50`..`0x180b57`), #3 (`0x180b58`..`0x180b63`) 가 붙어 있다.
  - #4 ~ #8 은 `0x17ab28` ~ `0x17ac03` 쪽에 8바이트 정렬로 모여 있다.
- 함수가 글 주소를 넣는 줄 (`LEA` 로 주소 계산 → `MOV` 로 쪽지 `Buffer` 칸에):
  - ⓠ-1: `+0x3c LEA RCX,[0x180180ad0]` → `+0x47 MOV [RBP-0x31],RCX` (키). `ACP` 는 `+0xc4` → `+0xcf`, `OEMCP` 는 `+0x171` → `+0x17c`.
  - ⓠ-2: 키 `+0x2d` → `+0x54`, `ProductType` `+0x5d` → `+0x70`, `LanmanNt` `+0x74` → `+0x7b`, `ServerNt` `+0x7f` → `+0x86`, `WinNt` `+0x8a` → `+0x91`.
  - `0x180180ad0` 같은 큰 숫자는 ntdll 이 기본 주소 `0x180000000` 에 놓였다고 친 Ghidra 주소다 `[추측]` (rva `0x180ad0` 에 `0x180000000` 을 더한 꼴이라). 실제 실행 때 주소는 다르다.

### 2.6 세 증인이 같은 말을 한다 — 바이트 · PDB 심볼 · 디컴파일 `[정적]`

"이 글이 정말 이 글자인가" 를 **세 군데**에서 따로 확인했다.

1. **바이트**: 결과서가 `pefile` 로 ntdll.dll 의 `.rdata` 를 직접 읽었다. 오케스트레이터가 #1 ~ #3 을 한 번 더 읽어 같음을 확인 (run 문서 "확인된 것").
2. **PDB 공개 심볼 `??_C@_1…`** 여덟 개:
   - **새 말 — 글 상수 심볼** = 컴파일러가 프로그램 속 글 상수에 붙여 둔 이름. 이름 안에 `_1` (와이드 글), **길이**, **CRC 지문**이 들어 있다 (run 문서 4절).
     - 비유: 인쇄 페이지 귀퉁이의 **바코드**. 바코드만 봐도 "몇 칸짜리, 어떤 글" 인지 대조할 수 있다.
   - 이름 속 CRC 와, 바이트로 계산한 **JamCRC** (`zlib.crc32 ^ 0xFFFFFFFF`) 가 여덟 개 모두 같다.
   - 이 CRC 는 **NUL 까지 넣고** 계산해야 맞는다. NUL 을 빼면 안 맞는다 → 심볼 길이에 NUL 이 들어 있다는 것과 같은 얘기 (run 문서 4절).
   - 예: `0x180ad0` → 길이 글자 `IA` = `0x80`, CRC `0x83e3bb6f` (`IDODLLGP`) = 바이트 JamCRC `0x83e3bb6f`. 요청서가 들고 온 PE_emulator 단서와 같다.
   - 자리도 맞는다: `.rdata` 시작 `0x173000` + PDB 자리 `0xdad0` = `0x180ad0`.
3. **디컴파일**: `RtlpQueryNlsSystemCodePages.c` 의 글과 같다.

- 그래서 sast/40 (run46) 의 ⓠ-2 글도 **이제 바이트로 확인됨** (대소문자까지) `[정적]`.
- 조심한 점 (run 문서 4절): 이 8개 주소를 담은 `.asm` 은 **가진 `.asm` 중엔** 두 함수뿐이었다. 폴더 밖 함수는 안 봤고, 링커가 같은 글을 합칠 수 있어서 "이 글을 쓰는 함수가 이것뿐" 이라고는 안 적었다.

### 2.7 두 함수는 쪽지 길이를 다르게 정한다 `[정적]`

이번 바퀴에서 제일 "왜?" 가 궁금한 부분이다. 천천히 나눠 본다.

#### 새 말 둘

- **`wcslen`** = UTF-16 글의 **글자 수**를 NUL 앞까지 세는 함수. 바이트가 아니라 글자 수를 돌려준다 (그래서 뒤에서 ×2).
  - 비유: **자**. 쪽지를 만들 때마다 글에 자를 대고 잰다.
- **즉치 (immediate)** = 명령 안에 **숫자가 그대로 박혀 있는** 것. `MOV qword [자리],0x840082` 의 `0x840082` 같은 것.
  - 비유: 치수를 재지 않고, 처음부터 **도장으로 찍어 둔 숫자**.

#### ⓠ-1 — 실행 중에 자로 잰다

- 키 #1 의 차례 (결과서 질문 3):
  1. `+0x60 CALL wcslen` → 글자 수 63.
  2. `+0x65 ADD RAX,RAX` → ×2 = 126 바이트.
  3. `+0x6f`·`+0x8f`·`+0x93`·`+0x9f` → 길이가 `0xfffe` 이상이면 `0xfffc` 로 자른다. 이 글은 짧아서 실제론 안 걸린다.
  4. `+0xa3` 에 `Length` = `0x7e`.
  5. `+0xa7`·`+0xab` 에 `MaximumLength` = `Length` + 2 = `0x80`.
- `ACP` (`+0xd3` …) · `OEMCP` (`+0x180` …) 도 같은 꼴이다.
- run 문서·결과서는 이 모양을 **`RtlInitUnicodeString` 을 함수 호출 없이 풀어 쓴 꼴** 이라고 불렀다. 실제로 부르는 건 `wcslen` 하나뿐이다.
  - `RtlInitUnicodeString` = 글 주소를 받아 쪽지 세 칸을 채워 주는 함수로 알려져 있다 `[추측]` (일반적으로 알려진 설명).
- 상수도 아니고, `.rdata` 에 미리 만들어 둔 쪽지도 아니다 (결과서 질문 3 답).

#### ⓠ-2 — 숫자를 미리 새겨 뒀다

- `wcslen` 을 **안 부른다**. 쪽지 머리 8바이트 (`Length` 2B + `MaximumLength` 2B + 빈칸 4B) 를 `MOV qword` 한 번으로 찍는다.

| 쪽지 | 줄 | 즉치 | `Length` | `MaximumLength` |
|---|---|---|---|---|
| 키 #4 | `+0x9d` | `0x840082` | `0x82` | `0x84` |
| `ProductType` #5 | `+0xa5` | `0x180016` | `0x16` | `0x18` |
| `LanmanNt` #7 | `+0xad` | `0x120010` | `0x10` | `0x12` |
| `ServerNt` #8 | `+0xb5` | `0x120010` | `0x10` | `0x12` |
| `WinNt` #6 | `+0xbd` | `0xc000a` | `0xa` | `0xc` |

- 결과서 확인: 다섯 숫자 모두 "바이트에서 센 글자 수 × 2" 와 "+2" 에 맞는다. 상수가 글과 어긋나지 않는다.

#### 결과는 같다

- 두 쪽 다 **`Length` = 글자 × 2, `MaximumLength` = `Length` + 2** (run 문서 "확인된 것").
- 그래서 에뮬레이터가 받는 쪽지 숫자는 어느 함수에서 오든 같은 규칙을 따른다.
- 왜 한 쪽은 재고 한 쪽은 새겼는지는 run 문서·결과서에 없다. 모른다.
  - `[추측]` (내 짐작) 컴파일러가 길이를 미리 계산해 넣었는지 여부의 차이로 보인다. 근거는 "결과는 같은 규칙인데 모양만 다르다" 는 것뿐이라, 확인은 아니다.

#### 그런데 "읽어 온 값 글" 의 길이는 다르게 잰다

여기가 진짜로 갈리는 곳이다. 서랍에서 **칸 내용 (값 글)** 을 받아 온 뒤:

- **ⓠ-1** — 받은 값 글도 **자로 잰다** (flow-writer 가 `.asm` 으로 더한 것, 결과서 질문 4 표엔 빠짐):
  - 결과 버퍼 끝 2바이트를 먼저 0 으로 박는다 (`+0x127` / 둘째 `+0x1d5`).
  - `+0x138` / `+0x1e6` 에서 `wcslen(RBP+0x13)` → ×2 → `0xfffe` 이상이면 `0xfffc` 로 자름 → `Length` (`+0x156` / `+0x206`), `MaximumLength` = `Length` + 2 (`+0x15e` / `+0x20e`).
  - 그래서 값 글은 **최대 11글자 + NUL** 이고, 장부가 알려 준 **`DataLength` 는 안 쓴다** `[정적]`.
- **ⓠ-2** — 받은 값 글은 **장부가 준 숫자를 믿는다**:
  - `Length` = `DataLength` − 2 (`+0x168 SUB` · `+0x170`), `MaximumLength` = `DataLength` (`+0x156`), `Buffer` = `RBP+0x33` `[정적]`.
  - `wcslen` 안 씀 — 값 길이를 레지스트리 `DataLength` 로 믿는다 (run 문서 3절).
- 비유: ⓠ-1 은 받아 온 종이에 **다시 자를 대고**, ⓠ-2 는 종이에 **적혀 온 치수를 그대로** 쓴다.
- `[추측]` (내 짐작) 흉내에 주는 뜻: ⓠ-2 로 답할 때는 `DataLength` 에 **NUL 2바이트까지** 넣어야 비교 글이 끝 글자를 안 잃는다.
  - 이유: `Length` = `DataLength` − 2 라서, NUL 없이 센 `DataLength` 를 주면 마지막 글자 하나가 잘린다. 그러면 비교가 안 맞는다.
  - test.exe 는 ⓠ-2 길을 안 타니 (2.12) 당장은 참고용이다.

### 2.8 CALL 자리와 ret 자리 — 오프셋이 세 종류 섞여 있다 `[정적]`

run 문서 1절 밑에 flow-writer 가 "**위 표의 오프셋 읽는 법**" 을 따로 붙였다. 이번 표에는 `+0x..` 가 **세 종류** 섞여 있어서다.

#### 세 종류

1. **CALL 자리** = `.asm` 에서 `CALL` 명령이 **놓인 줄**.
   - 예: `wcslen` `+0x60`·`+0xd3`·`+0x180`, 비교 ①②③ `+0x174`·`+0x188`·`+0x19c`.
2. **ret 자리** = 불린 함수가 끝나고 **돌아올 줄** (CALL 바로 다음 줄). WinDbg 콜스택에 `함수+0x..` 로 찍히는 건 이쪽이다.
   - 위 CALL 들의 ret: `+0x65`·`+0xd8`·`+0x185`, `+0x179`·`+0x18d`·`+0x1a1`.
3. **쓰는 줄** (`MOV`) = 값을 칸에 **써 넣는 줄**. 호출과 상관없다.
   - 예: `+0xa3`/`+0xab` (`MOV word` = `Length` / `MaximumLength`), `+0x9d` ~ `+0xbd` (`MOV qword` 상수).

- 비유: 직원이 옆 부서에 **심부름을 보내는 책상 자리** (CALL), 심부름꾼이 **돌아와서 서는 자리** (ret), 직원이 **장부에 숫자를 적는 자리** (쓰는 줄). 셋 다 "책상 몇 번째 칸" 으로 부르지만 뜻이 다르다.

#### 짝 표 (run 문서 1절에서 몇 줄)

| 함수 | CALL 줄 | `ret=` | 부르는 것 |
|---|---|---|---|
| `RtlpQueryNlsSystemCodePages` | `+0x60` | `+0x65` | `wcslen` (#1 키) |
| | `+0xb7` | `+0xbc` | `NtOpenKey` |
| | `+0x10e` | `+0x113` | `NtQueryValueKey` (`ACP`) |
| | `+0x1c0` | `+0x1c5` | `NtQueryValueKey` (`OEMCP`) |
| | `+0x21d` | `+0x222` | `NtClose` |
| `RtlpGetNtProductTypeFromRegistry` | `+0xd1` | `+0xd6` | `NtOpenKey` |
| | `+0xe5` | `+0xea` | `NtClose` (공용 꼬리) |
| | `+0x138` | `+0x13d` | `NtQueryValueKey` |
| `RtlGetNtProductType` | `+0x5d` | `+0x62` | `RtlpGetNtProductTypeFromRegistry` |

- 이 표의 짝은 모두 **5 차이**다 (`+0x60` → `+0x65` …). run 문서는 이 짝이 `_calls.tsv` 와 줄마다 같다고 적었다.
  - `[추측]` 5 는 이 모양 `CALL` 명령 한 줄의 바이트 길이다 (일반적으로 알려진 설명). 다른 꼴의 CALL 이면 차이가 다를 수 있으니, 숫자는 늘 `_calls.tsv` 에서 복사한다.

#### 한 줄에 둘이 같이 나오는 예 — `+0x65`

- `wcslen` 의 **ret 자리** 가 `+0x65` 다. 그리고 그 줄의 명령은 `ADD RAX,RAX` (×2) 다 (결과서).
- 즉 "`+0x65`" 는 "돌아올 자리" 이면서 "×2 하는 줄" 이다. 같은 숫자를 두 뜻으로 읽을 수 있다.
- 그래서 run 문서는 숫자 옆에 **무엇의 자리인지** (CALL / ret / 쓰는 줄) 를 같이 적는다.

#### 언제 어느 쪽을 쓰나 (run 문서 "도구 메모")

- **콜스택 숫자**로 쓸 때, **"돌아온 뒤" 멈춤점**을 걸 때 → **ret**.
  - 예: 선택 동적 확인 (4절) 이 `RtlpQueryNlsSystemCodePages+0xbc` 에 멈추자고 한다. `NtOpenKey` 가 **끝나고 돌아온 곳**이라, 거기서 `r eax` 하면 열기 결과 (상태) 를 볼 수 있다.
- **`.asm` 에서 줄을 찾을 때** → **CALL**.
- 이번 트리 (2.1) 의 라벨은 ret 이다. 그래서 `NtClose` 줄이 `+0xea` 로 적히고, 옆에 `(CALL +0xe5)` 를 따로 달았다.
- 결과서·대화에서 두 쪽이 섞이는 일이 run56·run57 에 이어 **세 번째**다. flow-writer 가 "결과서 양식에 call / ret 두 칸" 을 제안했지만 하네스는 안 고쳤다 (사용자 검토 전).
- 덧말: sast/40 (run46) 의 `NtOpenKey +0xd1` · `NtQueryValueKey +0x138` · `NtClose +0xe5` 는 셋 다 **CALL 자리**로 맞았다. 순서를 안 적었을 뿐이라 정정은 없다 (run 문서 "정정").

### 2.9 ⓠ-1 장부 읽는 차례 — 질문 4 `[정적]`

#### 들어가자마자 기본값부터

- `+0x2f MOV [RCX],0xfde9` · `+0x56 MOV [RDX],0xfde9` — 두 출력 칸 (ACP · OEMCP 번호 칸) 에 **`0xfde9`** (65001, UTF-8) 를 먼저 쓴다.
- 비유: 장부를 보기 전에 답 칸에 **연필로 "기본 사전"** 이라고 먼저 적어 둔다. 장부에서 못 찾으면 그대로 남는다.

#### syscall 차례 — `NtOpenKey` → `NtQueryValueKey` ×2 → `NtClose` (맞다)

| 순 | 부르는 것 (번호) | CALL / ret | 요점 |
|---|---|---|---|
| 1 | `NtOpenKey` (`0x12`) | `+0xb7` / `+0xbc` | 키 #1 열기. `DesiredAccess = 0x80000000` |
| 2 | `NtQueryValueKey` (`0x17`) — `ACP` | `+0x10e` / `+0x113` | 클래스 `2`, 버퍼 `RBP+7`, 크기 `0x24` |
| 2′ | `RtlUnicodeStringToInteger` | `+0x162` / `+0x167` | 값 글 → 10진 번호 → ACP 칸 |
| 3 | `NtQueryValueKey` — `OEMCP` | `+0x1c0` / `+0x1c5` | 위와 같고 `ValueName` 만 OEMCP |
| 3′ | `RtlUnicodeStringToInteger` | `+0x212` / `+0x217` | → OEMCP 칸 |
| 4 | `NtClose` (`0x0f`) | `+0x21d` / `+0x222` | 핸들 닫기 |

- syscall 번호 `0x12`·`0x17`·`0x0f` 는 결과서 (스텁 바이트) 와 run56 번호표가 같다.

#### 신청서 내용 — 흉내가 받을 값

- **새 말 — `OBJECT_ATTRIBUTES` (`ObjectAttributes`)** = "무엇을 열지" 를 적은 **신청서**. 안에 이름 쪽지 (`ObjectName`) 와 옵션이 들어간다.
  - ⓠ-1 신청서: `Length = 0x30` (`+0x68`) · `RootDirectory = 0` (`+0x74`) · `ObjectName` = 키 #1 쪽지 (`+0xb3`) · `Attributes = 0x240` (`+0x7b`) · 보안 칸 둘 0 (`+0x9a`).
  - `0x240` = `OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE`, `0x80000000` = `GENERIC_READ` — 이 이름들은 **공개 헤더 기준 해석**이다 (run 문서). 숫자는 `.asm`.
- **새 말 — 값 정보 클래스 `2`** = `NtQueryValueKey` 에게 "답을 **어떤 모양**으로 달라" 고 고르는 번호. `2` 는 공개 이름으로 `KeyValuePartialInformation` (결과서, 공개 헤더 해석).
  - 받는 버퍼 모양: 머리 12바이트 {`TitleIndex`, `Type`, `DataLength`} + 글 (`Data`). 함수가 `[RBP+0xb]` 를 `Type` 으로, `RBP+0x13` 을 글로 읽는 것이 이 모양과 맞다 `[정적]`.
  - 크기 `0x24` = 머리 12B + 글 24B.
- **새 말 — `REG_SZ`** = 장부 칸 형식 중 "**글**" 형식. 숫자로 `1`. 함수가 `Type` 을 `1` 과 비교한다 (`+0x11d` · `+0x1cb`).
  - 비유: 칸에 "글로 적힌 답" 인지 "숫자로 적힌 답" 인지 붙은 **표식**.
- 번호로 바꾸기: `RtlUnicodeStringToInteger` 에 `EDX = 10` → 값 글을 **10진수**로 읽어 번호 칸에 넣는다.

### 2.10 ⓠ-1 둘째 값만 다르게 간다 — 질문 5(a) `[정적]`

PE_emulator 가 asm 으로 본 "`+0x121 → +0x171` vs `+0x1cf → +0x219`" 는 **맞다**.

| | 첫째 (ACP) | 둘째 (OEMCP) |
|---|---|---|
| 읽기 실패 | `+0x117 JS +0x219` → `NtClose` (둘째 **안 읽음**) | `+0x1c9 JS +0x219` → `NtClose` |
| 형식이 `REG_SZ` 아님 | **`+0x121 JNZ +0x171`** → 번호 변환만 건너뛰고 **둘째 읽기로** | **`+0x1cf JNZ +0x219`** → **`NtClose` 로** |
| 번호 변환 실패 | `+0x16b JS +0x219` → `NtClose` | `+0x217 MOV EBX,EAX` → `+0x219` 로 떨어짐 |
| 변환 성공 | `+0x171` 로 떨어져 둘째 읽기 | 곧장 `+0x219` (`NtClose`) |

#### 왜 다르게 보이나 — 단계별로

1. 근거: 둘의 구조는 거의 같다. 다른 건 **"다음으로 가는 곳"** 하나다 (run 문서).
2. 첫째의 "다음" 은 **둘째 읽기** (`+0x171`), 둘째의 "다음" 은 **`NtClose`** (`+0x219`).
3. 그래서 "형식이 글이 아니면 이 칸은 넘어가자" 가, 첫째에선 둘째로, 둘째에선 닫기로 간다.
4. 비유: 질문지 1번이 이상하면 2번으로 넘어가고, 2번이 이상하면 (더 넘어갈 게 없으니) 시험지를 덮는다.

#### 실패 뒤 되돌림

- `NtClose` 뒤 `+0x222 TEST EBX,EBX` / `+0x224 JS +0x251` → `EBX < 0` 이면 `+0x251`·`+0x257` 에서 **두 번호를 다 `0xfde9` 로 되돌린다** → `+0x25d JMP +0x226`.
- 그래서:
  - 둘째 **읽기**가 실패하면, 잘 읽어 둔 ACP 도 `0xfde9` 로 돌아간다.
  - 둘째의 **형식 불일치**만은 실패로 안 친다 (`EBX` = `NtQueryValueKey` 상태 ≥ 0). 그래서 ACP 는 읽은 값 그대로, OEMCP 만 기본값.
- `NtOpenKey` 실패는 `+0xbe JS +0x251` — **`NtClose` 를 안 거치고** 같은 되돌림으로 간다.

### 2.11 ⓠ-1 은 늘 0 을 돌려준다 — 질문 5(b) `[정적]`

- **모든 길이 `+0x226 XOR EAX,EAX` 로 모이고, `RET` 은 `+0x24f` 하나뿐** 이다.
  - `XOR EAX,EAX` = 돌려줄 값 칸 (`EAX`) 을 0 으로 만드는 명령. 0 = `STATUS_SUCCESS`.
  - `+0x226` 로 오는 길은 둘: `+0x224 JS` 가 안 뛸 때 (떨어짐), `+0x25d JMP` (되돌림 뒤).
  - `+0x228 ~ +0x234` 쿠키 확인 → 복원 → `+0x24f RET`.
- 그래서 **실패는 돌려주는 값이 아니라, 두 번호 칸이 `0xfde9` 인 것으로만** 드러난다.
- 부른 쪽에 주는 뜻: `LdrpInitializeNlsInfo +0x42 TEST EAX,EAX` / `+0x44 JS +0x86` ("음수면 둘 다 `0xfde9`") 는 **이 함수 때문에는 안 뛴다** `[정적]`.
  - run57 에 적힌 그 갈래는 코드로는 맞다. 다만 이번에 "안 탄다" 가 더해졌다 (run 문서 "정정").
- 덧말: Ghidra 디컴파일은 이 함수의 반환형을 `void` 로 봤지만, `.asm` 은 `EAX = 0` 을 분명히 쓴다 (결과서). 디컴파일만 봤으면 놓칠 뻔한 곳이다.
- 비유: 이 직원은 장부를 못 찾아도 **"잘 됐습니다"** 라고만 보고한다. 대신 답 칸에 연필로 써 둔 "기본 사전" 을 안 지운다 (run 문서 결과 비유).

### 2.12 ⓠ-2 는 언제 타나 — test.exe 는 안 탄다

- `RtlGetNtProductType` (rva `0x5c7a0`, 111B) 은 답을 **세 군데** 에서 찾는다. 앞에서 찾으면 뒤로 안 간다 `[정적]`:
  1. `PEB+0x90` (`SharedData`) 가 있고 그 첫 `dword` 가 0 이 아니면 → `[그 칸+0x10]` 을 돌려준다 (`+0x12 ~ +0x21`, `+0x33`).
  2. 아니면 KUSD `0x7ffe0268` (`ProductTypeIsValid`) 가 0 이 아니면 → `0x7ffe0264` 를 돌려준다 (`+0x41 CMP` / `+0x49 JZ +0x5d`, `+0x4b`).
  3. 둘 다 아닐 때만 → `+0x5d` 에서 `RtlpGetNtProductTypeFromRegistry` (설정 장부).
- 비유: 건물 종류를 알고 싶으면 먼저 **안내판 옆 메모**, 그다음 **로비 공용 게시판**, 둘 다 비었을 때만 **장부**를 연다.
- `[관찰]` run44 P1a: `ProductTypeIsValid` = 1 → test.exe 는 2번에서 끝난다. **ⓠ-2 길을 안 탄다.**
- 그래도 이번에 본 이유: PE_emulator 가 같은 꼴의 흉내를 만들 때 쓸 수 있게 (요청서 ⓠ-2). flow.md 에선 2.4 프로세스 힙 절 "안 타는 갈래" 에 들어갔다.

### 2.13 ⓠ-2 실제 차례와 "공용 꼬리" `+0xe5` — 질문 7 `[정적]`

#### 주소 순서 ≠ 실행 순서

| 실행 순 | 부르는 것 | CALL | ret |
|---|---|---|---|
| 1 | `NtOpenKey` (`0x12`) | `+0xd1` | `+0xd6` |
| 2 | `NtQueryValueKey` (`0x17`) | `+0x138` | `+0x13d` |
| 3 | `NtClose` (`0x0f`) | **`+0xe5`** | `+0xea` |

- 주소로는 `NtClose` (`+0xe5`) 가 `NtQueryValueKey` (`+0x138`) 보다 **앞**이다. 그런데 실행은 맨 **나중**이다.

#### 새 말 — 공용 꼬리

- **공용 꼬리** = 여러 갈래가 **마지막에 다 같이 지나가는 정리 구간**. 컴파일러가 그 구간을 함수 앞쪽에 놓을 수도 있다.
  - 비유: 건물 **출구**가 복도 앞쪽에 있어서, 안쪽 방들에서 일을 마치면 다들 **앞으로 되돌아와** 같은 문으로 나가는 모양.

#### 단계별로 따라가기

1. `+0xd1` `NtOpenKey` → 돌아와서 `+0xd8 TEST` / `+0xda JNS +0x112`.
   - 열기 **성공** → 더 아래 줄 `+0x112` (읽기 준비) 로 **건너뛴다** (`NtClose` 줄 `+0xe5` 를 지나쳐 간다) → `+0x138` `NtQueryValueKey`.
   - 열기 **실패** → 그대로 `+0xdc` 로 떨어진다.
2. `+0xdc` = 공용 꼬리 입구: `+0xdc MOV RCX,[RBP-0x71]` (핸들) → `+0xe0 TEST` / `+0xe3 JZ +0xea`.
   - 핸들은 시작 때 `+0x43` 에서 0 으로 지워 둔 칸이다.
   - 열기 실패면 핸들 0 → **`NtClose` 를 건너뛴다**.
3. 읽기 뒤의 **모든 갈래** (`+0x141 JS`, `+0x1ab`·`+0x1b5`·`+0x1c0`·`+0x1c8 JMP`) 가 다시 `+0xdc` 로 **되돌아온다**.
   - 이번엔 핸들 ≠ 0 → **`+0xe5 NtClose`** → `+0xea MOV EAX,EBX` (상태를 돌려줌).
4. 그래서 → `+0xe5` 는 "열기 실패 길" 이 아니라 **열기 성공 뒤 모든 길이 지나는 공용 꼬리** 다. 열기 실패도 같은 줄을 지나지만 핸들 0 이라 건너뛴다.

#### 신청서 차이 — ⓠ-1 과 비교

- ⓠ-2 `NtOpenKey`: `DesiredAccess = 1` (`+0x58`, `KEY_QUERY_VALUE` 로 해석). ⓠ-1 은 `0x80000000` 이었다.
- 나머지 신청서 칸은 같은 꼴: `Length 0x30` (`+0x64`), `RootDirectory 0`, `Attributes 0x240` (`+0xc5`), 보안 칸 0 (`+0xcc`).
- `NtQueryValueKey` 도 클래스 `2` (`+0x123` → `+0x131`), 버퍼 `RBP+0x27` (`+0x11f`), 크기 `0x24` (`+0x129`) — ⓠ-1 과 같은 꼴이다.
- 그래서 baggage.md 6장 공용 부품에 "ntdll 레지스트리 값 읽기 꼴" 한 줄이 생겼다 (3절).

### 2.14 ⓠ-2 비교와 `*type` — 누가 무엇을 쓰나 `[정적]`

#### 읽은 뒤 갈래

1. `+0x143 CMP dword [RBP+0x2b],1` / `+0x147 JNZ +0x1b0` — 형식이 `REG_SZ` 아니면 불일치.
2. `+0x149 MOV RCX,[RBP+0x2f]` / `+0x14d CMP ECX,R15D` / `+0x150 JC +0x1b0` — `DataLength < 2` 면 불일치.
3. 비교 글 쪽지를 만든다 (2.7 끝: `Length = DataLength − 2`).
4. 비교 ① `WinNt` (CALL `+0x174`) → 같으면 **`*type = 1`** (`+0x1ba`).
5. 비교 ② `LanmanNt` (CALL `+0x188`) → 같으면 **`*type = 2`** (`+0x1c5`).
6. 비교 ③ `ServerNt` (CALL `+0x19c`) → 같으면 **`*type = 3`** (`+0x1a5`).
7. 불일치 셋 (형식 · 짧음 · 셋 다 아님) → `+0x1b0 MOV EBX,0xc000090b` → `+0x1b5 JMP +0xdc` (공용 꼬리).
   - 상태 `0xc000090b` 의 이름은 이 레포에서 확인 안 했다.

- **대소문자 무시**: 세 번 다 `RtlEqualUnicodeString` 의 셋째 인자 `R8B = 1` (`+0x165`·`+0x17d`·`+0x191`). 셋째 인자가 `CaseInSensitive` 라는 건 공개 시그니처 기준 해석이다.
  - 비유: 이름표를 대 볼 때 **큰 글씨 · 작은 글씨를 같은 글자로** 친다. `WinNT` 든 `winnt` 든 `WinNt` 와 같다.
- `1`·`2`·`3` 의 이름 (`NtProductWinNt`·`NtProductLanManNt`·`NtProductServer`) 은 공개 `NT_PRODUCT_TYPE` 기준 해석. 숫자는 `.asm`.

#### 실패 때 `*type = 1` 은 부른 쪽이 쓴다

- 이 함수는 **맞았을 때의 1/2/3 만** 쓴다. 실패 때는 `*type` 을 **안 건드리고** 상태 코드만 돌려준다.
- 부른 쪽 `RtlGetNtProductType`:
  1. `+0x62 SHR EAX,0x1f` / `+0x65 XOR AL,1` → `AL` = (상태 ≥ 0 인가).
  2. 성공이면 `+0x67 JNZ +0x56` → `*type` 그대로 (1/2/3), `TRUE`.
  3. 실패 (열기 · 읽기 · 불일치 모두) 면 **`+0x69 MOV dword [RBX],1`** → `*type = 1`, `FALSE`.
- 비유: 건물 종류 찾기를 맡은 직원은 못 찾으면 **"모르겠다"** 고만 말한다. 그러면 **부른 사람이** 칸에 "보통 건물 (1)" 을 적는다 (run 문서 결과 비유).
- ⓠ-1 과 나란히: ⓠ-1 은 못 찾아도 "잘 됐다" (늘 0) 고 하고 답 칸은 **자기가** 기본값으로 둔다. ⓠ-2 는 "모르겠다" 고 하고 기본값은 **부른 쪽**이 쓴다.

### 2.15 결과서와 run 문서가 다른 곳 (숫자만, 뜻은 같음)

이 글은 **run 문서 숫자**를 따랐다.

- 결과서 질문 7 "갈래 순서" 2번 줄: 결과서 `+0x14d MOV RCX,[RBP+0x2f]` / `+0x150 JC` → `.asm` 은 **`+0x149 MOV`** · `+0x14d CMP ECX,R15D` · `+0x150 JC +0x1b0` (run 문서 "정정"). 2.14 는 `.asm` 값.
- 결과서 질문 4 표에는 **값 글 `wcslen` 두 번** (`+0x138` / `+0x1e6`) 이 빠져 있다. flow-writer 가 `.asm` 으로 더했다 (2.7 끝).
- `Buffer` 를 넣는 자리: 결과서는 `MOV` 쪽 자리만 적었다. run 문서는 앞의 `LEA` 자리도 같이 적었다 (2.5).
- `LdrpInitializeNlsInfo +0x44 JS` 의 목적지: 결과서 5(b) 끝 줄 `+0x2ea` → **`+0x86`** (run 문서 "정정" 절에 있음). 이 글은 `+0x86` 으로 썼다.

---

## 3. 왜 중요한가

### flow.md 에서 바뀐 곳

- **2.4 트리 `LdrpInitializeNlsInfo` 아래 한 줄** (태그는 그대로 `[정적]`, 내용이 늘었다):
  - 전: `+0x42 RtlpQueryNlsSystemCodePages  AL = 0 일 때만. 레지스트리. 음수면 둘 다 0xfde9`
  - 후: `… 레지스트리 (run58: …\Nls\CodePage 의 ACP·OEMCP). 늘 0 반환`
  - 새 줄: `+0x44 JS +0x86 → 둘 다 0xfde9 (run58: 위 함수는 늘 0 이라 이 갈래는 안 탐. 실패는 그 안에서 0xfde9 로)`
  - 뜻: run57 까지는 "레지스트리가 실패하면 부른 쪽 `+0x44` 에서 기본값" 처럼 읽혔다. 이제 "실패해도 기본값은 **함수 안에서** 이미 정해진다" 로 바뀌었다.
- **2.4 NLS 절에 새 `[정적]` 묶음** (run58, ⓠ-1): 키·값 글, 길이 (`wcslen`), syscall 차례, 둘째 값 갈래, 늘 0, "test.exe 가 실제로 `NtOpenKey` 까지 가는지는 안 봤다".
- **2.4 프로세스 힙 절 "안 타는 갈래"** 에 ⓠ-2 한 묶음: 세 군데 찾는 순서, 글, 공용 꼬리 `+0xe5`, 대소문자 무시, `+0x69`.
  - 전: run46 은 "제품 종류 레지스트리 `NtOpenKey`·`NtQueryValueKey`·`NtClose` — KUSD 0 일 때" 까지만.
  - 후: 그 길의 **글자·차례·기본값 쓰는 쪽**까지 적혔다. 여전히 test.exe 는 안 탄다 (`[관찰]` run44).
- **4장** 두 줄: 선택 동적 확인 (Q110 쪽), 정적으로 남은 것 (4절).
- **기준·5장**: ntdll.pdb 줄에 run58, 5장에 run58 줄 둘.
- 그림 (mermaid) 은 안 바뀌었다 (plan.md 결정 기록).

### baggage.md (짐 지도)

- **새 짐 칸 없음 → 2장 숫자 그대로** (구조체 아는 칸 704 · 모름 335, 전역 쓴 자리 아는 것 80 · 모름 413).
- 근거만 보탰다:
  - 3.1 PEB `+0x34c ActiveCodePage` · `+0x34e OemCodePage` ② 줄 — 레지스트리 갈래 글, 늘 0 반환.
  - 6장 공용 부품 — "ntdll 레지스트리 값 읽기 꼴" (`NtOpenKey` → `NtQueryValueKey` 클래스 2 → `NtClose`), 두 함수, syscall 까지.
  - 7.6.3 커널 NLS 상태 줄 — run58 은 **ntdll 사본만** 봤다.
  - 7.18 — 선택 동적 확인 한 줄.
- 왜 칸이 안 늘었나: 이번에 찾은 건 "짐" 이 아니라 **짐을 고를 때 들고 가는 이름표**다. 짐 (PEB 코드 페이지 두 칸) 은 이미 세어져 있었다.

### 큰 그림에서

- **PE_emulator 쪽**: 레지스트리 흉내가 "어떤 서랍 · 어떤 칸 이름이 오나" 를 `[정적] run58` 근거로 맞출 수 있게 됐다.
  - 글자 전체, `Length`/`MaximumLength`, 신청서 칸 (`0x30`, `0x240`, 접근 권한), 값 정보 클래스 `2`, 버퍼 `0x24` 까지.
  - 늘 0 을 돌려준다는 PE_emulator 짐작도 맞았다 (plan.md 결정 기록).
- **test.exe 흐름에서**: 로더 시작 (2.4) 의 NLS 줄이 한 단계 더 끝까지 내려갔다 — `LdrpInitializeNlsInfo` → `RtlpQueryNlsSystemCodePages` → syscall 셋.
  - 이어지는 곳: 고른 번호로 `RtlpInitCodePageTables` 가 글자 사전을 받는다 (run57 해설 2.9). 그 뒤는 이번에 다시 안 봤다 (요청서 범위 밖).

---

## 4. 아직 모르는 것

1. **test.exe 가 실제로 ⓠ-1 의 `NtOpenKey` 까지 가나**
   - 왜 못 알았나: 지금은 run57 의 **정적 판정** (매니페스트 코드 페이지 0) 에 기댄다. 실행은 안 봤다.
   - 어떻게 보나 (선택, 필수 아님): `ntdll!RtlpQueryNlsSystemCodePages+0xbc` (`NtOpenKey` 돌아온 곳, test.exe 조건) · `+0x113` · `+0x1c5` (`NtQueryValueKey` 돌아온 곳) 에서 `r eax`. 신청서의 `ObjectName` 을 `dt _UNICODE_STRING` 으로 보면 `Length=0x7e`, `MaximumLength=0x80` 이어야 한다.
   - 대기열: **Q110** (동적, 선택). Q110 (4) `LdrpInitializeNlsInfo` 와 같은 순간이라 같이 볼 수 있다 `[추측]`.
2. **장부의 실제 값** (`ACP`·`OEMCP` 글, `ProductType` 값)
   - 왜 못 알았나: 값은 바이너리에 없고 VM 의 장부에 있다. 요청서 범위 밖.
   - 아는 것: ⓠ-1 은 값 글을 최대 11글자 + NUL 까지만 읽는다 (2.7).
   - 대기열: **Q104 VM 단계** (제품 종류 키가 있나, 값 종류·바이트 줄도 체크리스트에 더해짐).
3. **작은 정적 셋** (필요해지면)
   - 상태 `0xc000090b` 의 이름 (`RtlpGetNtProductTypeFromRegistry +0x1b0`).
   - 커널 사본 `nt!RtlpQueryNlsSystemCodePages` (`ExInitializeNls +0x79`, run57) 의 키·값 글이 ntdll 사본과 같은가 — 이번엔 ntdll 만.
   - 같은 `.rdata` 글을 쓰는 다른 ntdll 함수가 있나 — 가진 `.asm` 중엔 두 함수뿐, 폴더 밖은 안 봤다.
   - 대기열: **Q112** (정적).
4. **Q109 나머지** — B ntdll 본문 (`InitSecurityCookie` 등) · C ntoskrnl 비트 뜻 · D (1)~(3) 기록 정정 · E. 다음 바퀴 (정적).

---

## 5. 더 읽기

- run 문서: [`run58-registry-strings.md`](run58-registry-strings.md)
  - 1절 글 표 (결과서 그대로) · 오프셋 읽는 법 · CALL/ret 짝 표
  - 2절 ⓠ-1 차례·갈래·출구, 3절 ⓠ-2 차례·갈래·`*type`, 4절 PDB 심볼 대조
- 요청서: [`_workspace/sast/53_registry-strings.md`](../../_workspace/sast/53_registry-strings.md)
- 결과서: [`_workspace/sast/53_registry-strings_result.md`](../../_workspace/sast/53_registry-strings_result.md) — "run 문서에 그대로 옮길 글 표", 질문별 답
- flow.md: [`flow.md`](flow.md) 2.4 (트리 `LdrpInitializeNlsInfo` 줄 · NLS 절 · 프로세스 힙 절 "안 타는 갈래"), 4장
- baggage.md: [`baggage.md`](baggage.md) 3.1 · 6장 · 7.6.3 · 7.18
- 앞 바퀴 해설: [run57 해설](run57-nls-apiset-해설.md) (NLS 값 고르는 순서 2.8, 글자 서랍 2.9), [run46 해설](run46-process-heap-해설.md) (프로세스 힙 구간)

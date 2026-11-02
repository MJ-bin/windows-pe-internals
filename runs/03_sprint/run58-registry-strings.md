# 2026-11-02 run58 — 레지스트리 키·값 이름 글 8개 (Q109 첫 일 ⓠ-1·ⓠ-2, PE_emulator 요청 4): NLS 코드 페이지는 **`\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` 의 `ACP`·`OEMCP`**, 제품 종류는 **`…\Control\ProductOptions` 의 `ProductType`** 을 `WinNt`/`LanmanNt`/`ServerNt` 와 대소문자 무시로 비교한다. 글은 모두 ntdll `.rdata` 의 UTF-16LE + NUL. `RtlpQueryNlsSystemCodePages` 는 **늘 0 을 돌려준다** (`+0x226`), 제품 종류 실패 때 `*type = 1` 은 **부른 쪽 `RtlGetNtProductType +0x69`** 가 쓴다 (정적)

## PE_emulator 쪽에서 쓸 것

결과서 (`_workspace/sast/53_registry-strings_result.md`) 의 "run 문서에 그대로 옮길 글 표" 를 **글자 하나 안 바꾸고** 아래 1절에 옮겼다 (표·범례·밑 글머리·바이트까지). 그 뒤 절은 flow-writer 가 `.asm` 줄로 한 번 더 맞춘 것이다. 오프셋은 모두 `ms/disassemble/ntdll/<함수>.asm` 의 `+0x` 줄 (이 폴더는 커밋 안 함).

### 1. 글 표 `[정적]` (결과서 그대로)

범례: rva = ntdll 이미지 기준. 모두 구간 `.rdata`. 바이트 길이는 "NUL 뺀 / NUL 포함". 글은 따옴표 안 그대로 (대소문자 포함).

| # | rva | 글자 전체 | 인코딩 | 글자 수 | 바이트 (NUL 뺀 / 포함) | `Length` / `MaximumLength` (바이트) | 어디서 정하나 | 쓰는 자리 |
|---|---|---|---|---|---|---|---|---|
| 1 | `0x180ad0` | `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` | UTF-16LE + NUL | 63 | 126 (`0x7e`) / 128 (`0x80`) | `0x7e` / `0x80` | 런타임: `wcslen` (`+0x60`) ×2 → `+0xa3`/`+0xab` 에 씀 | `RtlpQueryNlsSystemCodePages` 의 `NtOpenKey` `ObjectAttributes.ObjectName` |
| 2 | `0x180b50` | `ACP` | UTF-16LE + NUL | 3 | 6 / 8 | `6` / `8` | 런타임: `wcslen` (`+0xd3`) ×2 → `+0xf1`/`+0xf9` | 첫째 `NtQueryValueKey` 의 `ValueName` |
| 3 | `0x180b58` | `OEMCP` | UTF-16LE + NUL | 5 | 10 / 12 | `10` (`0xa`) / `12` (`0xc`) | 런타임: `wcslen` (`+0x180`) ×2 → `+0x1a3`/`+0x1ab` | 둘째 `NtQueryValueKey` 의 `ValueName` |
| 4 | `0x17ab80` | `\Registry\Machine\System\CurrentControlSet\Control\ProductOptions` | UTF-16LE + NUL | 65 | 130 (`0x82`) / 132 (`0x84`) | `0x82` / `0x84` | **상수**: `RtlpGetNtProductTypeFromRegistry +0x9d MOV qword [RBP-0x59],0x840082` | `NtOpenKey` `ObjectName` |
| 5 | `0x17ab28` | `ProductType` | UTF-16LE + NUL | 11 | 22 / 24 | `0x16` / `0x18` | **상수**: `+0xa5 MOV qword [RBP-0x49],0x180016` | `NtQueryValueKey` `ValueName` |
| 6 | `0x17ab40` | `WinNt` | UTF-16LE + NUL | 5 | 10 / 12 | `0xa` / `0xc` | **상수**: `+0xbd MOV qword [RBP-0x39],0xc000a` | 비교 ① (`+0x174`) → 맞으면 `*type = 1` |
| 7 | `0x17ab50` | `LanmanNt` | UTF-16LE + NUL | 8 | 16 / 18 | `0x10` / `0x12` | **상수**: `+0xad MOV qword [RBP-0x29],0x120010` | 비교 ② (`+0x188`) → 맞으면 `*type = 2` |
| 8 | `0x17ab68` | `ServerNt` | UTF-16LE + NUL | 8 | 16 / 18 | `0x10` / `0x12` | **상수**: `+0xb5 MOV qword [RBP-0x19],0x120010` | 비교 ③ (`+0x19c`) → 맞으면 `*type = 3` |

- `UNICODE_STRING` 머리 (`Length` 2B + `MaximumLength` 2B + 빈칸 4B) 는 ⓠ-2 에선 위 `MOV qword` 한 번에 같이 써지고, `Buffer` 포인터는 별도 `MOV` 로 `.rdata` 글 주소를 넣는다 (`Buffer` 칸 = 머리 + 8).
  - `Buffer` 를 넣는 자리: 키 `+0x54`, `ProductType` `+0x70`, `LanmanNt` `+0x7b`, `ServerNt` `+0x86`, `WinNt` `+0x91`.
  - ⓠ-1 의 `Buffer` 는 `LEA RCX,[0x180180ad0]` (`+0x3c`) → `[RBP-0x31]` (`+0x47`), `[0x180180b50]` (`+0xc4`) → `[RBP-0x51]` (`+0xcf`), `[0x180180b58]` (`+0x171`) → `[RBP-0x51]` (`+0x17c`).
- 바이트 (앞쪽만, hex):
  - #1 `5c 00 52 00 65 00 67 00 69 00 73 00 74 00 72 00 79 00 5c 00 4d 00 61 00 …` 끝 `… 43 00 6f 00 64 00 65 00 50 00 61 00 67 00 65 00 00 00` (`\Registry\Machi…` ~ `CodePage` + NUL)
  - #2 `41 00 43 00 50 00 00 00`
  - #3 `4f 00 45 00 4d 00 43 00 50 00 00 00`
  - #4 `5c 00 52 00 65 00 67 00 …` 끝 `… 4f 00 70 00 74 00 69 00 6f 00 6e 00 73 00 00 00`
  - #5 `50 00 72 00 6f 00 64 00 75 00 63 00 74 00 54 00 79 00 70 00 65 00 00 00`
  - #6 `57 00 69 00 6e 00 4e 00 74 00 00 00` — 끝 글자가 `4e 00 74 00` = `N` `t` (소문자 `t`).
  - #7 `4c 00 61 00 6e 00 6d 00 61 00 6e 00 4e 00 74 00 00 00`
  - #8 `53 00 65 00 72 00 76 00 65 00 72 00 4e 00 74 00 00 00`
- 자리: #1 (`0x180ad0`..`0x180b4f`) 바로 뒤에 #2 (`0x180b50`..`0x180b57`), #3 (`0x180b58`..`0x180b63`) 가 붙어 있다. #4~#8 은 `0x17ab28`~`0x17ac03` 쪽 (8바이트 정렬; #4 는 `0x17ab80`..`0x17ac03`).

**위 표의 오프셋 읽는 법** (flow-writer, `.asm` `CALL … ret=` 줄로 맞춤). 표 안 오프셋은 세 종류가 섞여 있다:

- **CALL 자리** (콜스택 숫자 아님): `wcslen` `+0x60`·`+0xd3`·`+0x180`, 비교 ①②③ `+0x174`·`+0x188`·`+0x19c`. 돌아올 자리 (= WinDbg 콜스택 `+0x..`) 는 각각 `+0x65`·`+0xd8`·`+0x185`, `+0x179`·`+0x18d`·`+0x1a1`.
- **쓰는 줄** (`MOV`): `+0xa3`/`+0xab`·`+0xf1`/`+0xf9`·`+0x1a3`/`+0x1ab` (`MOV word` = `Length` / `MaximumLength`), `+0x9d`·`+0xa5`·`+0xad`·`+0xb5`·`+0xbd` (`MOV qword` 즉치 — `Length`·`MaximumLength`·빈칸 4B 를 한 번에).
- **표 밖에서 쓰는 syscall 자리는 아래 2·3절 표** 에 CALL / ret 둘 다 적었다.

| 함수 (`.asm`) | CALL 줄 | `ret=` | 부르는 것 | 표 몇 번 글 |
|---|---|---|---|---|
| `RtlpQueryNlsSystemCodePages` | `+0x60` | `+0x65` | `wcslen` | #1 키 |
| | `+0xb7` | `+0xbc` | `NtOpenKey` | #1 |
| | `+0xd3` | `+0xd8` | `wcslen` | #2 `ACP` |
| | `+0x10e` | `+0x113` | `NtQueryValueKey` | #2 |
| | `+0x138` | `+0x13d` | `wcslen` | (읽은 값 글 — 아래 2절) |
| | `+0x162` | `+0x167` | `RtlUnicodeStringToInteger` | (ACP 번호) |
| | `+0x180` | `+0x185` | `wcslen` | #3 `OEMCP` |
| | `+0x1c0` | `+0x1c5` | `NtQueryValueKey` | #3 |
| | `+0x1e6` | `+0x1eb` | `wcslen` | (읽은 값 글) |
| | `+0x212` | `+0x217` | `RtlUnicodeStringToInteger` | (OEMCP 번호) |
| | `+0x21d` | `+0x222` | `NtClose` | — |
| | `+0x22f` | `+0x234` | `__security_check_cookie` | — |
| `RtlpGetNtProductTypeFromRegistry` | `+0xd1` | `+0xd6` | `NtOpenKey` | #4 키 |
| | `+0xe5` | `+0xea` | `NtClose` | — (공용 꼬리) |
| | `+0xf3` | `+0xf8` | `__security_check_cookie` | — |
| | `+0x138` | `+0x13d` | `NtQueryValueKey` | #5 `ProductType` |
| | `+0x174` | `+0x179` | `RtlEqualUnicodeString` | #6 `WinNt` |
| | `+0x188` | `+0x18d` | `RtlEqualUnicodeString` | #7 `LanmanNt` |
| | `+0x19c` | `+0x1a1` | `RtlEqualUnicodeString` | #8 `ServerNt` |
| `RtlGetNtProductType` | `+0x5d` | `+0x62` | `RtlpGetNtProductTypeFromRegistry` | — |

- 위 CALL·ret 짝은 `ms/disassemble/ntdll/_calls.tsv` 와 줄마다 같다.
- `Buffer` 를 넣는 자리 (`LEA` → `MOV`): ⓠ-1 `+0x3c LEA RCX,[0x180180ad0]` → `+0x47 MOV [RBP-0x31],RCX` · `+0xc4 LEA [0x180180b50]` → `+0xcf` · `+0x171 LEA [0x180180b58]` → `+0x17c`. ⓠ-2 `+0x2d LEA RAX,[0x18017ab80]` → `+0x54` · `+0x5d LEA [0x18017ab28]` → `+0x70` · `+0x74 LEA [0x18017ab50]` → `+0x7b` · `+0x7f LEA [0x18017ab68]` → `+0x86` · `+0x8a LEA [0x18017ab40]` → `+0x91` (결과서는 `MOV` 쪽 자리만 적음).

### 2. ⓠ-1 `RtlpQueryNlsSystemCodePages` (ntdll rva `0xec890`, 606B) — 차례·갈래 `[정적]`

부르는 곳: `LdrpInitializeNlsInfo` CALL `+0x3d` / ret `+0x42` (매니페스트 코드 페이지가 없을 때, test.exe 는 이 길 — run57 `[정적]`).

**들어갈 때 먼저**: 두 출력 칸에 기본값 `0xfde9` (65001, UTF-8) — `+0x2f MOV [RCX],0xfde9` · `+0x56 MOV [RDX],0xfde9`.

**syscall 차례** (`NtOpenKey` → `NtQueryValueKey` ×2 → `NtClose` — 맞다):

| 순 | 부르는 것 | CALL | ret | 인자 (`.asm`) |
|---|---|---|---|---|
| 1 | `NtOpenKey` (syscall `0x12`) | `+0xb7` | `+0xbc` | `RCX = &hKey` (`+0x96 LEA RCX,[RBP-0x61]`) · `EDX = 0x80000000` (`+0x86`) · `R8 = &ObjectAttributes` (`+0x8b LEA R8,[RBP-0x29]`) |
| 2 | `NtQueryValueKey` (`0x17`) — `ACP` | `+0x10e` | `+0x113` | `RCX` = 핸들 (`+0xd8`) · `RDX = &ValueName` (`+0xe3`) · `R8D = 2` (`+0xea`, 클래스 2) · `R9 = RBP+7` 결과 버퍼 (`+0xdc`) · 다섯째 `0x24` (`+0x106`) · 여섯째 `&ResultLength` (`+0x101`) |
| 2′ | `RtlUnicodeStringToInteger` | `+0x162` | `+0x167` | `RCX = &값 글` (`+0x152`) · `EDX = 10` (`+0x147`) · `R8 = RSI` (= ACP 출력 칸, `+0x14b`) |
| 3 | `NtQueryValueKey` — `OEMCP` | `+0x1c0` | `+0x1c5` | 위와 같고 `ValueName` 만 OEMCP (`+0x18f`), `+0x193 ~ +0x1b8` |
| 3′ | `RtlUnicodeStringToInteger` | `+0x212` | `+0x217` | `EDX = 10` (`+0x1f9`) · `R8 = RDI` (OEMCP 출력 칸, `+0x1f6`) |
| 4 | `NtClose` (`0x0f`) | `+0x21d` | `+0x222` | `RCX` = 핸들 (`+0x219`) |

- syscall 번호 `0x12`·`0x17`·`0x0f` 는 결과서 (스텁 바이트) 와 `_workspace/sast/51_syscall-args_table.tsv` (run56) 가 같다.
- `ObjectAttributes`: `Length = 0x30` (`+0x68`) · `RootDirectory = 0` (`+0x74`) · `ObjectName` = 키 `UNICODE_STRING` (`+0xb3`) · `Attributes = 0x240` (`+0x7b`, `OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE` — 이름은 공개 헤더 기준 해석) · 보안 칸 둘 0 (`+0x9a MOVDQU`). `DesiredAccess = 0x80000000` (`GENERIC_READ`, 같은 해석).
- 결과 버퍼 (클래스 2 = `KEY_VALUE_PARTIAL_INFORMATION` 모양 `[정적]` + 공개 이름 해석): 시작 `RBP+7`, `Type` = `[RBP+0xb]` (`+0x11d`·`+0x1cb` 에서 `1` = `REG_SZ` 와 비교), 글 = `RBP+0x13`. 크기 `0x24` = 머리 12B + 글 24B.
- **읽은 값 글의 길이도 `wcslen` 으로 잰다** (flow-writer, `.asm`): 버퍼 끝 2바이트를 먼저 0 으로 (`+0x127 MOV word [RBP+0x29],R14W` / 둘째 `+0x1d5`) → `+0x138` / `+0x1e6` `wcslen(RBP+0x13)` → ×2 → `0xfffe` 이상이면 `0xfffc` 로 자름 → `[RBP-0x49]` `Length` (`+0x156` / `+0x206`), `MaximumLength = Length + 2` (`+0x15e` / `+0x20e`). 그래서 값 글은 최대 11글자 + NUL 이고, 레지스트리 `DataLength` 는 안 쓴다. 결과서 질문 4 표에는 이 `wcslen` 두 번이 빠져 있다 (CALL 자리 `_calls.tsv` 와 같음).

**갈래** (요청서 5(a) 기록 — 맞다):

| | 첫째 (ACP) | 둘째 (OEMCP) |
|---|---|---|
| 읽기 실패 (상태 < 0) | `+0x117 JS +0x219` → `NtClose` (둘째 **안 읽음**) | `+0x1c9 JS +0x219` → `NtClose` |
| 형식이 `REG_SZ` 아님 | **`+0x121 JNZ +0x171`** → 번호 변환만 건너뛰고 **둘째 읽기로 계속** (ACP 는 `0xfde9` 그대로) | **`+0x1cf JNZ +0x219`** → **`NtClose` 로** (OEMCP 는 `0xfde9` 그대로). 이때 `EBX` = `NtQueryValueKey` 상태 (≥ 0) 라 실패로 안 침 |
| 번호 변환 실패 | `+0x16b JS +0x219` → `NtClose` (둘째 안 읽음) | `+0x217 MOV EBX,EAX` → `+0x219` 로 떨어짐 (판정은 `NtClose` 뒤 `+0x224`) |
| 변환 성공 | `+0x171` 로 떨어져 둘째 읽기 | 곧장 `+0x219` (`NtClose`) |

- 차이는 **"다음으로 가는 곳"** 하나다 — 첫째의 다음은 둘째 읽기, 둘째의 다음은 `NtClose`.
- `NtClose` 뒤 `+0x222 TEST EBX,EBX` / `+0x224 JS +0x251` → `EBX < 0` 이면 `+0x251`·`+0x257` 에서 **두 번호 모두 `0xfde9` 로 되돌림** → `+0x25d JMP +0x226`. 둘째 읽기가 실패하면 잘 읽은 ACP 도 되돌아간다. 둘째의 형식 불일치만은 실패로 안 쳐서 ACP 는 읽은 값 그대로.
- `NtOpenKey` 실패 → `+0xbe JS +0x251` (**`NtClose` 안 거침**) → 같은 되돌림 → `+0x226`.

**출구** (요청서 5(b) — 맞다): **모든 길이 `+0x226 XOR EAX,EAX` 로 모이고, `RET` 은 `+0x24f` 하나뿐** → **늘 0 (STATUS_SUCCESS) 을 돌려준다.**

- `+0x226` 로 오는 길 둘: `+0x224 JS` 가 안 뛸 때 (떨어짐), `+0x25d JMP` (되돌림 뒤). `+0x228 ~ +0x234` 쿠키 확인 → 복원 → `+0x24f RET`.
- 실패는 반환값이 아니라 **두 출력 칸이 `0xfde9` 인 것으로만** 드러난다. 그래서 부른 쪽 `LdrpInitializeNlsInfo +0x42 TEST EAX,EAX` / `+0x44 JS +0x86` 은 이 함수 때문에는 안 뛴다 `[정적]`. (Ghidra 디컴파일은 반환형을 `void` 로 보지만 `.asm` 은 `EAX = 0` 을 명시 — 결과서.)

### 3. ⓠ-2 `RtlpGetNtProductTypeFromRegistry` (ntdll rva `0x5abd4`, 460B) — 차례·갈래 `[정적]`

부르는 곳: `RtlGetNtProductType` (rva `0x5c7a0`, 111B) CALL `+0x5d` / ret `+0x62`. 이 길은 `PEB+0x90` (`SharedData`) 가 NULL 이거나 그 첫 `dword` 가 0 이고 (`+0x12 ~ +0x21`, 아니면 `[그 칸+0x10]` 을 돌려줌 `+0x33`), KUSD `0x7ffe0268` (`ProductTypeIsValid`) 도 0 일 때만 탄다 (`+0x41 CMP` / `+0x49 JZ +0x5d`, 0 아니면 `0x7ffe0264` 를 돌려줌 `+0x4b`). `[관찰]` run44 P1a: `ProductTypeIsValid` = 1 → **test.exe 는 이 길을 안 탄다**.

**실행 차례** (주소 순서와 다르다):

| 순 | 부르는 것 | CALL | ret | 비고 |
|---|---|---|---|---|
| 1 | `NtOpenKey` (`0x12`) | `+0xd1` | `+0xd6` | `DesiredAccess = 1` (`+0x58 MOV EDX,1`, `KEY_QUERY_VALUE` 해석) · `ObjectAttributes` `Length 0x30` (`+0x64`) · `RootDirectory 0` · `ObjectName` (`+0x95`·`+0x99`) · `Attributes 0x240` (`+0xc5`) · 보안 칸 0 (`+0xcc`) |
| 2 | `NtQueryValueKey` (`0x17`) | `+0x138` | `+0x13d` | 클래스 2 (`+0x123 MOV R15D,2` → `+0x131`) · 버퍼 `RBP+0x27` (`+0x11f`) 크기 `0x24` (`+0x129`) · `ValueName` = `ProductType` (`+0x134`) |
| 3 | `NtClose` (`0x0f`) | **`+0xe5`** | `+0xea` | **공용 꼬리** — 주소가 `+0x138` 보다 앞인 건 컴파일러가 꼬리를 앞에 둔 것 |

- 열기 성공: `+0xd8 TEST` / `+0xda JNS +0x112` → 읽기 준비 → `+0x138`.
- 열기 실패: `+0xdc` 로 떨어짐 → `+0xdc MOV RCX,[RBP-0x71]` (핸들, 시작 때 `+0x43` 에서 0) → `+0xe0 TEST` / `+0xe3 JZ +0xea` → **`NtClose` 건너뜀**.
- 읽기 뒤 모든 갈래가 `+0xdc` 로 모인다 (`+0x141 JS`, `+0x1ab`·`+0x1b5`·`+0x1c0`·`+0x1c8 JMP`) → 핸들 ≠ 0 → **`+0xe5 NtClose`** → `+0xea MOV EAX,EBX` (상태 반환).
- → `+0xe5` 는 "열기 실패 길" 이 아니라 **열기 성공 뒤 모든 길이 지나는 공용 꼬리** (열기 실패도 같은 줄을 지나지만 핸들 0 이라 건너뜀).

**읽은 뒤 갈래** (읽기 성공):

1. `+0x143 CMP dword [RBP+0x2b],1` / `+0x147 JNZ +0x1b0` — 형식이 `REG_SZ` 아니면 불일치.
2. `+0x149 MOV RCX,[RBP+0x2f]` / `+0x14d CMP ECX,R15D` / `+0x150 JC +0x1b0` — `DataLength < 2` 면 불일치.
3. 비교 글을 만든다: `Buffer = RBP+0x33` (`+0x152`·`+0x15a`), `MaximumLength = DataLength` (`+0x156`), `Length = DataLength − 2` (`+0x168 SUB` · `+0x170`). `wcslen` 안 씀 — 값 길이를 레지스트리 `DataLength` 로 믿는다.
4. 비교 ① `WinNt` CALL `+0x174` (ret `+0x179`) → 같으면 `+0x17b JNZ +0x1ba` → **`*type = 1`** (`+0x1ba MOV dword [RDI],1`).
5. 비교 ② `LanmanNt` CALL `+0x188` (ret `+0x18d`) → `+0x18f JNZ +0x1c5` → **`*type = 2`** (`+0x1c5 MOV [RDI],R15D`).
6. 비교 ③ `ServerNt` CALL `+0x19c` (ret `+0x1a1`) → 같으면 `+0x1a5 MOV dword [RDI],3` → **`*type = 3`**. 아니면 `+0x1a3 JZ +0x1b0`.
7. 불일치 셋 (형식·짧음·셋 다 아님) → `+0x1b0 MOV EBX,0xc000090b` → `+0x1b5 JMP +0xdc`. 상태 이름은 이 레포에서 확인 안 함.

- 비교는 **대소문자 무시**: 세 번 다 `R8B = 1` (`+0x165`·`+0x17d`·`+0x191`), 함수 `RtlEqualUnicodeString` (ntdll rva `0x5aaa0`). 셋째 인자 = `CaseInSensitive` 는 공개 시그니처 기준 해석.
- 읽기 실패면 상태 = `NtQueryValueKey` 상태 그대로 (`+0x13d MOV EBX,EAX`), 맞음이면 0.

**`*type = 1` 은 누가 쓰나** — 이 함수는 맞음 갈래의 `1`/`2`/`3` 만 쓴다. **실패 때 `*type = 1` 은 부른 쪽 `RtlGetNtProductType +0x69`** 다:

- `+0x62 SHR EAX,0x1f` / `+0x65 XOR AL,1` → AL = (상태 ≥ 0).
- `+0x67 JNZ +0x56` → 성공이면 `*type` 그대로 (위 1/2/3) · `TRUE` 반환.
- 실패 (열기·읽기·불일치 모두) → **`+0x69 MOV dword [RBX],1`** → `+0x6f JMP +0x56` · AL = 0 (`FALSE`).
- `1`·`2`·`3` 의 이름 (`NtProductWinNt`·`NtProductLanManNt`·`NtProductServer`) 은 공개 `NT_PRODUCT_TYPE` 기준 해석, 숫자는 `.asm`.

### 4. PDB 심볼 대조 `[정적]` (결과서 그대로)

PDB 공개 심볼 `??_C@_1…` 8개의 자리·길이 글자·CRC 가 바이트와 맞다. 심볼 이름 속 CRC 는 `A`~`P` = 16진 0~15 로 풀었고, 바이트 쪽은 **글 바이트 전체 (NUL 포함) 의 JamCRC** (`zlib.crc32 ^ 0xFFFFFFFF`) 로 계산했다 (NUL 을 빼면 안 맞음 → 심볼 길이에 NUL 이 들어 있는 것과 같은 얘기).

| rva | PDB 자리 (`0009:`) | 심볼 길이 글자 (`@` 앞) | 풀이 | 이름 속 CRC | 바이트 JamCRC |
|---|---|---|---|---|---|
| `0x180ad0` | `56016` (`0xdad0`) | `IA` | `0x80` | `0x83e3bb6f` (`IDODLLGP`) | `0x83e3bb6f` |
| `0x180b50` | `56144` (`0xdb50`) | `7` | 8 | `0x3d2b9585` (`DNCLJFIF`) | `0x3d2b9585` |
| `0x180b58` | `56152` (`0xdb58`) | `M` | 12 | `0x4e177da9` (`EOBHHNKJ`) | `0x4e177da9` |
| `0x17ab28` | `31528` (`0x7b28`) | `BI` | `0x18` | `0x09f2c5f7` (`JPCMFPH`) | `0x09f2c5f7` |
| `0x17ab40` | `31552` (`0x7b40`) | `M` | 12 | `0xf5554bf7` (`PFFFELPH`) | `0xf5554bf7` |
| `0x17ab50` | `31568` (`0x7b50`) | `BC` | `0x12` | `0x72491391` (`HCEJBDJB`) | `0x72491391` |
| `0x17ab68` | `31592` (`0x7b68`) | `BC` | `0x12` | `0x6d96c7cb` (`GNJGMHML`) | `0x6d96c7cb` |
| `0x17ab80` | `31616` (`0x7b80`) | `IE` | `0x84` | `0x2f450b00` (`CPEFALAA`) | `0x2f450b00` |

- `.rdata` 구간 시작 `0x173000` 을 더하면 PDB 자리 = rva (`0x173000 + 0xdad0 = 0x180ad0` 등).
- 심볼 이름은 `_1` = 와이드 글, 그 뒤 길이 (`IA` = `0x80`) 와 CRC. 요청서의 "키 이름 CRC `0x83e3bb6f`, 길이 `0x80`" 와 바이트가 맞는다는 뜻이다.
- 바이트·PDB·디컴파일 (`RtlpQueryNlsSystemCodePages.c` 의 `L"\\Registry\\Machine\\System\\CurrentControlSet\\Control\\Nls\\CodePage"` 등) **셋이 같은 글**이다. sast/40 의 ⓠ-2 글도 이제 바이트로 확인됨 (sast/40 이 적은 `LanmanNt`/`ServerNt`/`WinNt` 대소문자 맞음).
- 이 8개 주소를 담은 `.asm` 은 내가 가진 `ms/disassemble/ntdll/*.asm` 중에서는 위 두 함수뿐이었다 (이 폴더에 없는 함수가 같은 글을 쓰는지는 안 봄 — 링커가 같은 글을 합칠 수 있으니 "이 글을 쓰는 함수가 이것뿐"이라고는 안 적는다).

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 03_sprint 열여섯째 바퀴. plan **Q109 첫 일 (ⓠ-1·ⓠ-2)** — 사용자 결정 2026-11-02 (PE_emulator 요청 4 의 ⓠ). **짐 지도 바퀴** — `baggage.md` 는 근거만 보탬 (아래 "짐 지도 반영"). Q109 나머지 (B·C·D (1)~(3)·E) 는 다음 바퀴.
- 정적: ntdll (`ntdll.pdb`) GUID+age `C3093720177DA6851519DCFFE8DF53FE1` — flow.md 기준과 같다. 결과서가 셋으로 맞춤: `ms/sym/ntdll.dll.pdb.info.txt`, PDB 안 (`llvm-pdbutil-14 dump --summary`, PDB 안쪽 age 4), PE CodeView 항목 (`pefile`, age 1).
- 버전 대조 (결과서): `_calls.tsv` `LdrpInitializeProcess` CALL `+0x1bb6` / ret `+0x1bbb` → `LdrpInitializeGraphRecurse`, `LdrpInitializeNlsInfo` CALL `+0x3d` / ret `+0x42` → `RtlpQueryNlsSystemCodePages`, `RtlGetNtProductType` CALL `+0x5d` / ret `+0x62` → `RtlpGetNtProductTypeFromRegistry`. 함수 rva·크기 `_index.tsv` 와 같음.

## 목적

- PE_emulator 가 레지스트리 값 읽기 덫을 만들 때 쓸 **키·값 이름 글자 전체** 를 `[정적] run58` 근거로 남긴다 (지금까진 글자 일부·짐작만).
  - ⓠ-1: NLS 코드 페이지 함수의 글 셋 (`0x180ad0`·`0x180b50`·`0x180b58`), `UNICODE_STRING` 길이를 어디서 정하나, syscall 차례, 둘째 값만 다른 처리·늘 0 을 돌려주나.
  - ⓠ-2: 제품 종류 함수의 글 다섯 (rva·글자·길이 출처), syscall 실제 차례 (`+0xe5` 가 왜 `+0x138` 보다 앞인가), 실패 때 `*type = 1` 은 누가 쓰나, 비교가 대소문자 무시인가.
- 범위 밖 (요청서): Q109 B·C·D (1)~(3)·E, 레지스트리 실제 값 (Q104 VM 단계), 돌려준 번호를 뒤에서 어떻게 쓰나 (run57).

## 방법

- 정적: `_workspace/sast/53_registry-strings.md` — ntdll `^RtlpQueryNlsSystemCodePages$`·`^RtlpGetNtProductTypeFromRegistry$`·`^RtlGetNtProductType$` depth 0 + `.rdata` 바이트.
  - 이미 analyze 된 프로젝트, 세 함수 `.asm`·`.c` 도 이미 있어 **Ghidra 실행 0번** (결과서).
  - 글 바이트: 결과서가 `pefile` 로 `ms/bin` 의 ntdll.dll 을 직접 읽음 (스크래치, 결과 파일 안 남김). `.rdata` 는 rva `0x173000` = 파일 오프셋 `0x173000` 이라 rva 가 곧 파일 오프셋.
  - PDB 공개 심볼 `??_C@_1…` 은 `llvm-pdbutil-14 dump --publics` (결과서).
- 결과서: `_workspace/sast/53_registry-strings_result.md` (**분석성공**, PDB 일치).
- 이 문서를 쓰며 다시 맞춘 것:
  - 오케스트레이터: `.asm` `RtlpQueryNlsSystemCodePages` `+0x60`·`+0xd3`·`+0x180 wcslen`, `+0xb7 NtOpenKey`, `+0x10e`·`+0x1c0 NtQueryValueKey`, `+0x121`·`+0x1cf JNZ`, `+0x21d NtClose`, `+0x226 XOR EAX,EAX`, `+0x24f RET` / `RtlpGetNtProductTypeFromRegistry` `+0x9d`·`+0xa5`·`+0xad`·`+0xb5`·`+0xbd` `MOV qword` 상수, `+0xd1 NtOpenKey`, `+0xe5 NtClose`, `+0x138 NtQueryValueKey`. ntdll 바이트 rva `0x180ad0`·`0x180b50`·`0x180b58` 의 글을 직접 읽음 — 결과서와 같음.
  - flow-writer: 세 `.asm` 의 CALL 줄 전부 (위 1절 표, `_calls.tsv` 와 같음), `RtlpQueryNlsSystemCodePages` `+0x2f ~ +0x106`·`+0x113 ~ +0x25d` (갈래·되돌림·출구), `RtlpGetNtProductTypeFromRegistry` `+0x29 ~ +0x1c8`, `RtlGetNtProductType` 전체.
- 문서: 이 run 문서, `flow.md` (기준, 2.4 NLS 절, 2.4 프로세스 힙 절 "안 타는 갈래", 4장, 5장), `baggage.md` (아래).

## 결과

비유: 관리실 (ntdll) 이 본사 장부 (레지스트리) 에서 "이 동네 글자 번호" 와 "이 건물 종류" 를 찾아본다.

- 장부 서랍 이름 (키) 과 칸 이름 (값) 은 관리실 **수첩 맨 뒤 인쇄된 글** (`.rdata`) 에 그대로 박혀 있다. 이번에 그 글을 한 글자씩 읽었다.
- 글자 번호 찾기는 **실패해도 "잘 됐다" 고만 말하고**, 대신 번호 칸에 기본값 (UTF-8) 을 남긴다.
- 건물 종류 찾기는 실패하면 "모르겠다" 고 말하고, **부른 사람이** 칸에 "보통 건물 (1)" 을 적는다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeNlsInfo` | `+0x3d` | `+0x42` | `ntdll!RtlpQueryNlsSystemCodePages` | [정적] |
| `ntdll!RtlpQueryNlsSystemCodePages` | `+0xb7` | `+0xbc` | `ntdll!NtOpenKey` | [정적] |
| `ntdll!RtlpQueryNlsSystemCodePages` | `+0x10e` | `+0x113` | `ntdll!NtQueryValueKey` (`ACP`) | [정적] |
| `ntdll!RtlpQueryNlsSystemCodePages` | `+0x1c0` | `+0x1c5` | `ntdll!NtQueryValueKey` (`OEMCP`) | [정적] |
| `ntdll!RtlpQueryNlsSystemCodePages` | `+0x21d` | `+0x222` | `ntdll!NtClose` | [정적] |
| `ntdll!RtlGetNtProductType` | `+0x5d` | `+0x62` | `ntdll!RtlpGetNtProductTypeFromRegistry` | [정적] |
| `ntdll!RtlpGetNtProductTypeFromRegistry` | `+0xd1` | `+0xd6` | `ntdll!NtOpenKey` | [정적] |
| `ntdll!RtlpGetNtProductTypeFromRegistry` | `+0x138` | `+0x13d` | `ntdll!NtQueryValueKey` (`ProductType`) | [정적] |
| `ntdll!RtlpGetNtProductTypeFromRegistry` | `+0xe5` | `+0xea` | `ntdll!NtClose` (공용 꼬리) | [정적] |

- `wcslen`·`RtlUnicodeStringToInteger`·`RtlEqualUnicodeString`·`__security_check_cookie` 줄은 1절 표.

### 호출 관계

라벨은 ret 자리. 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄.

```
LdrpInitializeNlsInfo
 └─ +0x42  RtlpQueryNlsSystemCodePages (0xec890, 606B)       ← RCX = &ACP, RDX = &OEMCP
          │  +0x2f · +0x56  두 칸 = 0xfde9 (기본값)
          ├─ +0x65   wcslen   키 #1 → Length 0x7e / Max 0x80 (+0xa3 / +0xab)
          ├─ +0xbc   NtOpenKey   \Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage
          │          실패 → +0xbe JS +0x251 (NtClose 안 거침)
          ├─ +0xd8   wcslen   #2 ACP → 6 / 8
          ├─ +0x113  NtQueryValueKey (클래스 2, 버퍼 0x24)
          │          실패 → +0x219 · REG_SZ 아님 → +0x121 JNZ +0x171 (둘째로 계속)
          ├─ +0x13d  wcslen (값 글) → +0x167 RtlUnicodeStringToInteger(10) → *ACP
          ├─ +0x185  wcslen   #3 OEMCP → 10 / 12
          ├─ +0x1c5  NtQueryValueKey
          │          실패 → +0x219 · REG_SZ 아님 → +0x1cf JNZ +0x219 (NtClose 로)
          ├─ +0x1eb  wcslen (값 글) → +0x217 RtlUnicodeStringToInteger(10) → *OEMCP
          └─ +0x222  NtClose
             +0x224 JS +0x251 → 두 칸 0xfde9 → +0x25d JMP +0x226
             +0x226  XOR EAX,EAX (늘 0) → +0x24f RET

RtlGetNtProductType (0x5c7a0, 111B)
 │  PEB+0x90 → [+0x10] (+0x33) · KUSD 0x7ffe0268 ≠ 0 → 0x7ffe0264 (+0x4b)   test.exe 는 KUSD 쪽 [관찰] run44
 └─ +0x62  RtlpGetNtProductTypeFromRegistry (0x5abd4, 460B)   둘 다 아닐 때만
          │  +0x9d ~ +0xbd  UNICODE_STRING 머리 5개 = MOV qword 즉치 (#4 ~ #8)
          ├─ +0xd6   NtOpenKey   \Registry\Machine\System\CurrentControlSet\Control\ProductOptions
          │          성공 → +0xda JNS +0x112 · 실패 → +0xdc (핸들 0 → NtClose 건너뜀)
          ├─ +0x13d  NtQueryValueKey   ProductType (클래스 2, 버퍼 0x24)
          │          REG_SZ · DataLength ≥ 2 → 비교 글 (Length = DataLength − 2)
          ├─ +0x179  RtlEqualUnicodeString(…, WinNt, 1)    → *type = 1 (+0x1ba)
          ├─ +0x18d  RtlEqualUnicodeString(…, LanmanNt, 1) → *type = 2 (+0x1c5)
          ├─ +0x1a1  RtlEqualUnicodeString(…, ServerNt, 1) → *type = 3 (+0x1a5)
          │          불일치 → +0x1b0 EBX = 0xc000090b
          └─ +0xea   NtClose (CALL +0xe5, 공용 꼬리 +0xdc) → EAX = EBX
    +0x69  실패면 *type = 1 (부른 쪽), FALSE
```

## 확인된 것

- `[정적]` 글 8개의 글자 전체·인코딩 (UTF-16LE + NUL)·길이 — 바이트 (`pefile`), PDB 공개 심볼 (`??_C@_1…` 자리·길이·CRC), 디컴파일 글 셋이 같다 (결과서). 오케스트레이터가 `0x180ad0`·`0x180b50`·`0x180b58` 을 따로 읽어 같음을 확인.
- `[정적]` 요청서 1 의 키는 짐작대로 `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` 가 **맞다** (63자). PE_emulator 가 아는 "길이 `0x80`" 은 **NUL 포함** 길이 (뺀 것 `0x7e`).
- `[정적]` `UNICODE_STRING` 길이 출처가 두 함수에서 다르다: ⓠ-1 은 **런타임 `wcslen`** (`RtlInitUnicodeString` 을 풀어 쓴 꼴, `Max = Length + 2`), ⓠ-2 는 **`MOV qword` 즉치 상수**. 값은 같은 규칙 (`Length` = 글자 ×2, `Max` = +2).
- `[정적]` ⓠ-1 syscall 차례 `NtOpenKey` → `NtQueryValueKey` (`ACP`) → `NtQueryValueKey` (`OEMCP`) → `NtClose`, 클래스 2. 둘째 값만 형식 불일치 행선지가 다르다 (`+0x121 → +0x171` vs `+0x1cf → +0x219`). **늘 0 을 돌려준다** (`+0x226`, `RET` 하나 `+0x24f`).
- `[정적]` ⓠ-2 실제 차례 `NtOpenKey` (`+0xd1`) → `NtQueryValueKey` (`+0x138`) → `NtClose` (`+0xe5`, 공용 꼬리). 비교는 `RtlEqualUnicodeString` 대소문자 무시. 실패 때 `*type = 1` 은 **`RtlGetNtProductType +0x69`**.
- `[관찰]` run44 P1a: KUSD `ProductTypeIsValid` = 1 → test.exe 는 ⓠ-2 길을 안 탄다. ⓠ-1 은 매니페스트 코드 페이지가 없어서 탄다 — 이것은 run57 `[정적]` 판정이고 실행은 안 봤다.

## 정정

- **결과서 질문 7 "갈래 순서" 2번 줄** (뜻은 같고 줄만): "`+0x14d MOV RCX,[RBP+0x2f]` / `+0x150 JC`" → `.asm` 은 **`+0x149 MOV RCX,[RBP+0x2f]`** · `+0x14d CMP ECX,R15D` · `+0x150 JC +0x1b0`. 이 문서 3절은 `.asm` 값으로 씀. (1절 글 표는 결과서 그대로 — 이 줄은 표 밖.)
- **결과서 질문 5(b) 끝 줄** (뜻은 같고 숫자만): "`LdrpInitializeNlsInfo +0x42 TEST EAX,EAX` / `+0x44 JS +0x2ea`" 의 목적지는 **`+0x86`** 이다 — `.asm` `+0x44  1800ec2a8  JS 0x1800ec2ea`, `+0x86  1800ec2ea  MOV EDX,0xfde9` (함수 시작 `1800ec264`). 주소 끝자리를 오프셋으로 읽은 것 (run57 `+0x236 → +0x2b6` 과 같은 종류, 오케스트레이터 확인). 이 문서·flow.md 는 처음부터 `+0x86` 으로 씀, 결과서 숫자는 안 고침.
- **이전 run 문서 정정 없음.** sast/40 (run46) 의 `NtOpenKey +0xd1`·`NtQueryValueKey +0x138`·`NtClose +0xe5` 는 셋 다 **CALL 자리**로 맞고, 순서를 적지 않았을 뿐이다. run57 의 `LdrpInitializeNlsInfo +0x44 JS +0x86` ("음수면 둘 다 `0xfde9`") 도 코드로는 맞다 — 다만 이번에 이 함수가 늘 0 을 돌려줘서 그 갈래는 이 함수 때문에는 안 탄다는 것이 더해졌다 (flow.md 2.4 NLS 절에 한 줄).

## 짐 지도 반영 (`baggage.md`)

- 새 짐 칸 없음 → **2장 숫자 그대로** (구조체 아는 칸 704 · 모름 335, 전역 쓴 자리 아는 것 80 · 모름 413).
- 3.1 `_PEB` `+0x34c`·`+0x34e` ② 줄 근거 칸에 레지스트리 갈래 글·늘 0 반환 한 줄 (run58).
- 7.6.3 "커널 NLS 상태 레지스트리 값" 줄 근거 칸: run58 은 ntdll 사본만 봤다는 한 줄.
- 6장 공용 부품에 한 줄: ntdll 레지스트리 값 읽기 꼴 (`NtOpenKey` → `NtQueryValueKey` 클래스 2 → `NtClose`) — 두 함수, syscall 까지.
- 7.18 에 선택 확인 한 줄 (아래 "아직 확인 필요" 1).
- 기준·8장에 run58.

## 아직 확인 필요

결과서 "동적분석으로 확인해야 할 것" — **필수 없음.** 글·길이·차례·갈래는 모두 코드와 `.rdata` 바이트에 있다.

1. (선택, 동적 **Q110**) **ⓠ-1 이 test.exe 에서 실제로 `NtOpenKey` 까지 가는가** — 지금은 run57 의 정적 판정 (매니페스트 코드 페이지 0) 에 기댄다. 멈춤점 `ntdll!RtlpQueryNlsSystemCodePages+0xbc` (`NtOpenKey` 돌아온 곳, test.exe 조건) · `+0x113` · `+0x1c5` (`NtQueryValueKey` 돌아온 곳) 에서 `r eax`. 입구에서 넘긴 `OBJECT_ATTRIBUTES` → `ObjectName` 을 `dt _UNICODE_STRING` 으로 보면 `Length=0x7e`, `MaximumLength=0x80` 이어야 한다. run57 동적 **Q110** (4) (`LdrpInitializeNlsInfo +0x2f` 직후 `r al`) 와 같은 순간이라 같이 볼 수 있다 `[추측]`.
2. **레지스트리 실제 값** (`ACP`·`OEMCP` 글, `ProductType` 값) — **Q104 VM 단계** 몫 (요청서 범위 밖). 값 글은 최대 11글자 + NUL 까지만 읽힌다 (2절).
3. 정적으로 남은 것 (정적 **Q112**): 상태 `0xc000090b` 의 이름, 커널 사본 `nt!RtlpQueryNlsSystemCodePages` (`ExInitializeNls +0x79`, run57) 의 키·값 글이 ntdll 사본과 같은지 (이번엔 ntdll 만), 같은 `.rdata` 글을 쓰는 다른 함수가 있는지 (결과서: 가진 `.asm` 중엔 두 함수뿐, 폴더 밖은 안 봄).
- Q109 의 나머지 (B ntdll 본문 `InitSecurityCookie` 등 · C ntoskrnl 비트 뜻 · D (1)~(3) 정정 · E) 는 다음 바퀴.

## 도구 메모

- 결과서 "스크립트 문제" 없음. 새 Ghidra 실행 없음.
- **결과서·대화의 오프셋은 CALL 자리와 ret 자리가 섞인다** (이번에도). 콜스택 숫자로 쓸 땐 ret, `.asm` 줄을 찾을 땐 CALL. 1절 표처럼 `.asm` 의 `CALL … ret=` 줄로 둘을 같이 적으면 헷갈리지 않는다. run56·run57 에 이어 세 번째 — 결과서 양식에 "call / ret 두 칸" 을 넣자는 제안은 보고에만 (하네스는 안 고침).
- `.rdata` 글을 읽을 때 PDB 공개 심볼 `??_C@_1<길이><CRC>@…` 로 자리·길이를 미리 맞출 수 있다 (CRC = NUL 포함 바이트의 JamCRC, 결과서). 다음에 글을 찾을 때 지름길.

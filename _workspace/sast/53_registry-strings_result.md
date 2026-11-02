# 53 registry-strings — 정적 분석 결과 (Q109 ⓠ-1·ⓠ-2)

- 요청서: `_workspace/sast/53_registry-strings.md`
- 모듈 / 바이너리: ntdll (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/ntdll.dll`)
- **PDB GUID: 일치.** 요청서 값 `C3093720177DA6851519DCFFE8DF53FE` = 아래 셋이 다 같음.
  - `ms/sym/ntdll.dll.pdb.info.txt` 의 `guid_age=C3093720177DA6851519DCFFE8DF53FE1` (뒤의 `1` 은 age)
  - PDB 파일 안 (`llvm-pdbutil-14 dump --summary`): GUID `{C3093720-177D-A685-1519-DCFFE8DF53FE}` (PDB 안쪽 age 는 4 — 심볼 서버 키는 PE 쪽 age 1 로 만듦, 문제 없음)
  - PE 의 CodeView 디버그 항목 (pefile): `C3093720177DA6851519DCFFE8DF53FE`, age 1, `ntdll.pdb`
- 분석: `.gpr` + `_index.tsv` + `SetPdb:` 줄 다 있음 → **다시 안 걸었다.** 함수도 **다시 안 뽑았다** (`.asm`·`.c` 가 이미 `ms/disassemble/ntdll/`·`ms/decomp/ntdll/` 에 있음). 새 Ghidra 실행 0번.
  - 읽은 파일: `ms/disassemble/ntdll/{RtlpQueryNlsSystemCodePages,RtlpGetNtProductTypeFromRegistry,RtlGetNtProductType,LdrpInitializeNlsInfo}.asm`, `ms/decomp/ntdll/RtlpQueryNlsSystemCodePages.c` (교차 확인용), `ms/disassemble/ntdll/_calls.tsv`.
  - 글 바이트: `pefile` 로 ntdll.dll 을 직접 읽음 (스크래치에서만, 결과 파일 안 남김). `.rdata` 는 rva `0x173000` = 파일 오프셋 `0x173000` (구간 머리 raw=VA) 이라 **rva 가 곧 파일 오프셋**.
  - PDB 공개 심볼은 `llvm-pdbutil-14 dump --publics` 로 읽음.
- **버전 확인: 일치.**
  - `_calls.tsv` `LdrpInitializeProcess` call `+0x1bb6` / ret **`+0x1bbb`** → `LdrpInitializeGraphRecurse`.
  - `LdrpInitializeNlsInfo` call `+0x3d` / ret **`+0x42`** → `RtlpQueryNlsSystemCodePages` (요청서·sast/52 와 같음).
  - `RtlGetNtProductType` call `+0x5d` / ret `+0x62` → `RtlpGetNtProductTypeFromRegistry` (요청서·sast/40 과 같음).
  - 함수 rva·크기: `_index.tsv` 와 요청서 일치 (`0xec890` 606B / `0x5abd4` 460B / `0x5c7a0` 111B).

## 스크립트 문제

없음.

---

## 한눈에 — 요청서 질문 답 (한 줄씩)

| 질문 | 한 줄 답 |
|---|---|
| 1 키 이름 전체가 `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` 인가 | **예.** 63자, 대소문자 그대로 (`System`·`Control`·`Nls`·`CodePage`). 값 이름은 `ACP`, `OEMCP` (전부 대문자). |
| 2 UTF-16 인가, NUL 있나 | **UTF-16LE, 끝에 NUL (`00 00`) 있음.** 여덟 글 전부. |
| 3 `Length`/`MaximumLength` 출처 | ⓠ-1: **런타임 계산** (`wcslen` 호출 → `×2` → 한계 클램프 → `Max = Length + 2`). 상수 아님, `.rdata` 에 미리 만든 `UNICODE_STRING` 아님. ⓠ-2: **`MOV` 즉치 상수** (`Length`·`Max` 를 8바이트 한 번에). |
| 4 syscall 차례 | `NtOpenKey` → `NtQueryValueKey`(ACP) → `NtQueryValueKey`(OEMCP) → `NtClose`. 사이에 `RtlUnicodeStringToInteger` ×2. 값 정보 클래스 **2**. 실패하면 일찍 `NtClose` 로 빠지는 갈래 있음 (아래). |
| 5a 둘째 값만 다르게? | **맞다.** 첫째: 형식이 `REG_SZ` 아니면 둘째 읽기로 **넘어감** (`+0x121 → +0x171`). 둘째: 형식이 `REG_SZ` 아니면 **`NtClose` 로 감** (`+0x1cf → +0x219`). 그 밖의 차이는 아래 표. |
| 5b 모든 출구가 `+0x226 XOR EAX,EAX` 인가 | **예. 늘 0.** `RET` 이 `+0x24f` 하나뿐이고 거기로 가는 길은 `+0x226` 앞을 지나야 함. 실패하면 두 번호를 `0xfde9` 로 되돌린 뒤 같은 `+0x226` 로 합류. |
| 6 제품 종류 글 | 키 `\Registry\Machine\System\CurrentControlSet\Control\ProductOptions` (65자), 값 `ProductType`, 비교 글 **`WinNt` / `LanmanNt` / `ServerNt`** (끝 두 글자 `Nt` — `NT` 아님). UTF-16LE. 길이는 `MOV` 즉치. |
| 7 syscall 차례·갈래 | 실행 차례는 **`NtOpenKey` → `NtQueryValueKey` → `NtClose`**. `+0xe5` 의 `NtClose` 는 **열기 실패 길이 아니라 모든 갈래가 모이는 공용 꼬리** (열기 실패 땐 핸들이 0 이라 건너뜀). 클래스 **2**. 비교는 **대소문자 무시** (`RtlEqualUnicodeString`, 셋째 인자 1). 열기·읽기·비교 실패 땐 이 함수가 상태 코드만 돌려주고 **`*type = 1` 은 부른 쪽 `RtlGetNtProductType +0x69` 가 씀**. |

---

## run 문서에 그대로 옮길 글 표 `[정적]` (26100.9457, ntdll)

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

### PDB 심볼 대조 (필수 아니라던 확인 — 했다. 전부 일치) `[정적]`

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

---

## 질문별 답 [정적]

### ⓠ-1 `RtlpQueryNlsSystemCodePages` (ntdll rva `0xec890`, 606B)

**1. 글자 전체** — 위 표 #1·#2·#3. 키는 질문에 적은 대로 `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` **맞다 (예)**. 앞 15자 `\Registry\Machi` + `ne\System\CurrentControlSet\Control\Nls\CodePage` (뒤 48자). 합 63자.
- 근거: `.rdata` rva `0x180ad0` 부터 바이트 128개 (`5c 00 52 00 … 65 00 00 00`), `0x180b50` 6+2, `0x180b58` 10+2. 함수 쪽 사용: `.asm` `+0x3c LEA RCX,[0x180180ad0]`, `+0xc4 LEA RCX,[0x180180b50]`, `+0x171 LEA RCX,[0x180180b58]`.

**2. 인코딩·NUL** — 전부 UTF-16LE (글자마다 `xx 00`), 끝에 NUL 한 글자 (`00 00`). 키는 NUL 까지 128바이트 = `0x80` (PE_emulator 가 아는 "길이 `0x80`" 은 **NUL 포함 길이**; 뺀 건 126 = `0x7e`).
- `wcslen` 으로 길이를 재므로 NUL 이 꼭 있어야 하고, 실제로 있다.

**3. `Length`/`MaximumLength` 출처** — **런타임 계산.** `RtlInitUnicodeString` 을 함수 호출 없이 직접 풀어 쓴 꼴이다 (`wcslen` 만 부른다). 상수 `MOV` 즉치도, `.rdata` 의 미리 만든 `UNICODE_STRING` 도 아니다.
- 키 (`ObjectName`, 스택 `[RBP-0x39]`):
  - `+0x60 CALL wcslen` (인자 = `+0x3c` 에서 넣은 `.rdata` 글), `+0x65 ADD RAX,RAX` (글자 수 ×2)
  - `+0x6f MOV EBX,0xfffc`, `+0x8f LEA R13D,[RBX+2]` (= `0xfffe`), `+0x93 CMP RAX,R13`, `+0x9f CMOVNC RAX,RBX` — 길이가 `0xfffe` 이상이면 `0xfffc` 로 자름 (글이 짧아서 실제론 안 걸림)
  - `+0xa3 MOV word [RBP-0x39],AX` = **`Length`**, `+0xa7 ADD AX,R12W` (`R12D` = 2, `+0x82`), `+0xab MOV word [RBP-0x37],AX` = **`MaximumLength` = `Length` + 2**
  - `Buffer`: `+0x47 MOV [RBP-0x31],RCX` (= `.rdata` 글 주소)
  - 값: **`Length` = `0x7e`, `MaximumLength` = `0x80`**
- `ACP` (스택 `[RBP-0x59]`): `+0xd3 wcslen`, `+0xe0 ADD RAX,RAX`, `+0xed CMOVNC`, `+0xf1 MOV word [RBP-0x59],AX`, `+0xf5/+0xf9 ADD AX,R12W; MOV word [RBP-0x57],AX`. 값 **6 / 8**. `Buffer` = `+0xcf MOV [RBP-0x51],RCX`.
- `OEMCP`: `+0x178 MOV [RBP-0x59],R14` (머리 8바이트를 0 으로 먼저 지움), `+0x180 wcslen`, `+0x185 ADD RAX,RAX`, `+0x1a3 MOV word [RBP-0x59],AX`, `+0x1a7/+0x1ab` 로 +2. 값 **10 / 12**. `Buffer` = `+0x17c MOV [RBP-0x51],RCX`.
- 디컴파일 (`ms/decomp/ntdll/RtlpQueryNlsSystemCodePages.c`) 도 같은 모양: `wcslen(L"…")` → `×2` → `0xfffd < len` 이면 `0xfffc` → `Length`, `Length + 2` (우리 말 요약).
- **`ObjectAttributes`** (참고, 에뮬레이터가 덫에서 읽을 값): `Length = 0x30` (`+0x68`), `RootDirectory = 0` (`+0x74` — 스택 `[RBP-0x21]` 에 `R14` = 0), `ObjectName` = 위 키 `UNICODE_STRING` 주소 (`+0xb3`), `Attributes = 0x240` (`+0x7b`) = `OBJ_CASE_INSENSITIVE (0x40) | OBJ_KERNEL_HANDLE (0x200)`, 보안 칸 두 개 0 (`+0x9a MOVDQU`). `NtOpenKey` 의 `DesiredAccess = 0x80000000` (`GENERIC_READ`, `+0x86 MOV EDX,0x80000000`).

**4. syscall 차례** — `NtOpenKey` → `NtQueryValueKey` ×2 → `NtClose` **맞다.** (call 위치 / `ret_off` — `_calls.tsv` 와 같은 값. WinDbg 콜스택 `+0x..` 는 `ret_off`.)

| 순 | 부르는 것 | call 위치 | ret_off | 인자 (`.asm` 근거) |
|---|---|---|---|---|
| 1 | `NtOpenKey` | `+0xb7` | **`+0xbc`** | `RCX = &hKey` (`+0x96 LEA RCX,[RBP-0x61]`), `EDX = 0x80000000` (`+0x86`), `R8 = &ObjectAttributes` (`+0x8b LEA R8,[RBP-0x29]`) |
| 2 | `NtQueryValueKey` (**ACP**) | `+0x10e` | **`+0x113`** | `RCX = [RBP-0x61]` (핸들, `+0xd8`), `RDX = ValueName` (`+0xe3 LEA RDX,[RBP-0x59]`), `R8D = 2` (`+0xea MOV R8D,R12D`, `R12D` = 2), `R9 = RBP+7` (`+0xdc`) 결과 버퍼, 스택 5번째 = `0x24` (`+0x106 MOV [RSP+0x20],0x24`), 6번째 = `&ResultLength` (`+0x101`) |
| 2′ | `RtlUnicodeStringToInteger` | `+0x162` | `+0x167` | `RCX = &ustr` (`+0x152`), `EDX = 10` (`+0x147 LEA EDX,[R14+0xa]`), `R8 = RSI` (= 첫째 출력 인자 = ACP 번호 칸, `+0x14b`) |
| 3 | `NtQueryValueKey` (**OEMCP**) | `+0x1c0` | **`+0x1c5`** | 위와 같고 `ValueName` 만 OEMCP (`+0x18f LEA RDX,[RBP-0x59]`, 버퍼/클래스/길이 같음, `+0x193~+0x1b8`) |
| 3′ | `RtlUnicodeStringToInteger` | `+0x212` | `+0x217` | `EDX = 10` (`+0x1f9`), `R8 = RDI` (둘째 출력 인자 = OEMCP 번호 칸, `+0x1f6`) |
| 4 | `NtClose` | `+0x21d` | **`+0x222`** | `RCX = [RBP-0x61]` (핸들, `+0x219`) |

- **값 정보 클래스 = `2`** (두 번 다). 공개 헤더의 `KeyValuePartialInformation`. `.asm` 근거: 결과 버퍼 안에서 `Type` 을 `[RBP+0xb]` (버퍼 시작 `RBP+7` 에서 +4) 로 읽어 `1` (`REG_SZ`) 과 비교하고 (`+0x11d`, `+0x1cb`), 글은 `RBP+0x13` (+0xc) 에서 읽는다 — `KEY_VALUE_PARTIAL_INFORMATION` {`TitleIndex`, `Type`, `DataLength`, `Data[]`} 모양과 맞다.
  - 버퍼 크기 `0x24` = 머리 12바이트 + 글 24바이트. 버퍼 맨 끝 2바이트를 0 으로 먼저 박는다 (`+0x127 MOV word [RBP+0x29],R14W`, 둘째는 `+0x1d5`) → 글은 최대 11글자 + NUL.
- **갈림 (실패 때)**: `NtOpenKey` 실패 → `+0xbe JS +0x251` (`NtClose` 안 부름, 아래 5b). 첫째 읽기 실패 → `+0x117 JS +0x219` (둘째 읽기를 **건너뛰고** `NtClose`). 번호 변환 실패 → `+0x16b JS +0x219` (같음).
- 참고로 syscall 번호 (스텁 바이트 `4c 8b d1 b8 NN 00 00 00 …` 에서 읽음, `ms/bin` ntdll): `NtOpenKey` = `0x12`, `NtQueryValueKey` = `0x17`, `NtClose` = `0x0f`.

**5. 기록 확인**

**(a) 둘째 값 처리가 다르다 — 맞다.** 두 줄 (`+0x121`, `+0x1cf`) 모두 요청서가 적은 대로다.

| | 첫째 (ACP) | 둘째 (OEMCP) |
|---|---|---|
| 읽기 실패 (상태 < 0) | `+0x117 JS +0x219` → `NtClose` (둘째 **안 읽음**) | `+0x1c9 JS +0x219` → `NtClose` |
| 형식이 `REG_SZ` 아님 | `+0x121 JNZ +0x171` → **번호 변환만 건너뛰고 둘째 읽기로 계속** (ACP 는 기본값 `0xfde9` 그대로) | `+0x1cf JNZ +0x219` → **`NtClose` 로** (OEMCP 는 기본값 `0xfde9` 그대로). 이때 `EBX` = `NtQueryValueKey` 상태 (0 이상) 라 실패로 안 침 |
| 번호 변환 실패 | `+0x16b JS +0x219` → `NtClose` (둘째 **안 읽음**) | `+0x217 MOV EBX,EAX` 후 `+0x219` 로 떨어짐 (`NtClose` 다음 `+0x224` 에서 판정) |
| 변환 성공 | `+0x171` 로 떨어져 둘째 읽기 | 곧장 `NtClose` (`+0x219`) |

- 정리: 둘의 구조는 거의 같고 **"다음으로 가는 곳"** 이 다르다 — 첫째는 다음이 둘째 읽기, 둘째는 다음이 `NtClose`. 그래서 형식 검사 실패의 행선지가 다르다.
- 실패 처리 (`+0x224 JS +0x251`): `NtClose` 뒤 `EBX < 0` 이면 **두 번호를 모두 `0xfde9` 로 되돌린다** (`+0x251`, `+0x257`). 둘째 읽기가 실패하면 앞서 잘 읽은 ACP 도 `0xfde9` 로 되돌아간다. 반면 둘째의 **형식 불일치만**은 실패로 안 쳐서 ACP 는 읽은 값 그대로, OEMCP 만 `0xfde9`.
- 기본값: 함수 맨 앞에서 두 출력 칸에 `0xfde9` (65001, UTF-8) 를 먼저 쓴다 (`+0x2f MOV [RCX],0xfde9`, `+0x56 MOV [RDX],0xfde9`).

**(b) 모든 출구가 `+0x226 XOR EAX,EAX` 로 모인다 — 맞다. 늘 0 을 돌려준다.**
- `RET` 은 `+0x24f` 하나뿐이고, 거기로 가는 길목이 `+0x226 XOR EAX,EAX` (`+0x228~+0x234` 쿠키 확인 → 복원 → `RET`).
- `+0x226` 로 들어오는 길 둘: ① `+0x224 JS` 가 안 뛰고 떨어질 때 (`NtClose` 뒤 성공 쪽), ② `+0x25d JMP +0x226` (`+0x251` 에서 두 번호를 `0xfde9` 로 되돌린 뒤).
- `+0x251` 로 오는 길 둘: `+0xbe JS` (`NtOpenKey` 실패, **`NtClose` 안 거침**), `+0x224 JS` (`NtClose` 거친 뒤 실패).
- 그래서 **"돌려주는 상태"는 늘 0 (STATUS_SUCCESS)**, 실패는 `*param_1`·`*param_2` 가 `0xfde9` 인 것으로만 드러난다. (Ghidra 디컴파일은 반환형을 `void` 로 보지만 asm 은 `EAX = 0` 을 명시한다.)
  - 부른 쪽 `LdrpInitializeNlsInfo +0x42 TEST EAX,EAX` / `+0x44 JS +0x2ea` 는 그러므로 이 함수만 놓고 보면 안 뛴다 `[정적]`.

---

### ⓠ-2 `RtlpGetNtProductTypeFromRegistry` (ntdll rva `0x5abd4`, 460B) + 부르는 쪽 `RtlGetNtProductType` (rva `0x5c7a0`, 111B)

**6. 글자 전체·인코딩·길이 출처** — 위 표 #4~#8. 요청서 sast/40 의 글과 **같다** (키 `\Registry\Machine\System\CurrentControlSet\Control\ProductOptions`, 값 `ProductType`, 비교 글 `LanmanNt`/`ServerNt`/`WinNt`). 이제 바이트로 확인됨.
- 대소문자 주의: 비교 글 셋의 끝은 **`Nt`** (대문자 N + 소문자 t). 레지스트리 실제 값은 `WinNT` 처럼 보일 수 있지만 비교가 대소문자 무시라 상관없다 (아래 7).
- **`Length`/`MaximumLength` 출처 = 상수.** ⓠ-1 과 달리 `wcslen` 안 부름. 즉치 `MOV qword [스택],0x…` 로 머리 8바이트 (Length 2B + Max 2B + 빈칸 4B) 를 한 번에 쓴다.

| `UNICODE_STRING` | 쓰는 자리 | 즉치 | `Length` | `MaximumLength` | `Buffer` (자리 → 글) |
|---|---|---|---|---|---|
| 키 (`[RBP-0x59]`) | `+0x9d` | `0x840082` | `0x82` (130) | `0x84` (132) | `+0x54` → `0x18017ab80` (#4) |
| `ProductType` (`[RBP-0x49]`) | `+0xa5` | `0x180016` | `0x16` (22) | `0x18` (24) | `+0x70` → `0x18017ab28` (#5) |
| `LanmanNt` (`[RBP-0x29]`) | `+0xad` | `0x120010` | `0x10` (16) | `0x12` (18) | `+0x7b` → `0x18017ab50` (#7) |
| `ServerNt` (`[RBP-0x19]`) | `+0xb5` | `0x120010` | `0x10` | `0x12` | `+0x86` → `0x18017ab68` (#8) |
| `WinNt` (`[RBP-0x39]`) | `+0xbd` | `0x0c000a` | `0xa` (10) | `0xc` (12) | `+0x91` → `0x18017ab40` (#6) |

- 모든 `Length` 는 바이트에서 센 글자 수 ×2 와 같고 (키 65자 → 130, 등) `MaximumLength` = `Length` + 2 (NUL 포함) 와 같다 — 상수가 글 바이트와 어긋나지 않는다.
- **`ObjectAttributes`** (`[RBP-0x9]`): `Length = 0x30` (`+0x64 MOV qword [RBP-9],0x30`), `RootDirectory = 0` (`+0x4c`), `ObjectName` = 키 `UNICODE_STRING` 주소 (`+0x95 LEA RAX,[RBP-0x59]`, `+0x99` → `[RBP+7]`), `Attributes = 0x240` (`+0xc5 MOV [RBP+0xf],0x240`), 보안 칸 0 (`+0xcc MOVDQU`). `NtOpenKey` `DesiredAccess = 1` (`KEY_QUERY_VALUE`, `+0x58 MOV EDX,1`).
- 비교 대상 `UNICODE_STRING` 은 값 읽은 뒤 `+0x152~+0x170` 에서 만든다: `Length` = `DataLength - 2` (`+0x168 SUB AX,R15W`, `R15D` = 2), `MaximumLength` = `DataLength` (`+0x156 MOV [RBP-0x67],CX`), `Buffer` = 결과 버퍼의 `Data` (`+0x152 LEA RAX,[RBP+0x33]`). `wcslen` 안 씀 → 레지스트리 값이 `REG_SZ` + NUL 포함 길이라고 믿는다.

**7. syscall 차례·갈래·돌려주는 값**

- **실행 차례**: `NtOpenKey` (call `+0xd1`, ret **`+0xd6`**) → `NtQueryValueKey` (call `+0x138`, ret **`+0x13d`**) → `NtClose` (call `+0xe5`, ret **`+0xea`**). 주소 순서가 `+0xe5` < `+0x138` 인 건 **컴파일러가 공용 꼬리를 앞쪽에 둔 것**일 뿐이다.
  - `+0xd8 TEST EAX,EAX` / `+0xda JNS +0x112` → 열기 성공이면 `+0x112` (읽기 준비) 로 점프 → `+0x138 NtQueryValueKey`.
  - 열기 실패면 `+0xdc` 로 떨어진다: `+0xdc MOV RCX,[RBP-0x71]` (핸들, 시작 때 0 으로 지움 `+0x43`), `+0xe0 TEST RCX,RCX`, `+0xe3 JZ +0xea` (`MOV EAX,EBX` 로 바로 감) → 핸들이 0 이라 **`NtClose` 건너뜀**.
  - 읽기 뒤 모든 갈래 (읽기 실패 `+0x141 JS +0xdc`, 형식 불일치/짧음/비교 불일치 `+0x1b0`, 맞음 `+0x1ba`·`+0x1c5`·`+0x1a5`) 가 `+0xdc` (= `MOV RCX,[RBP-0x71]` 줄, 공용 꼬리 입구) 로 합류 (`+0x1ab`·`+0x1b5`·`+0x1c0`·`+0x1c8` 의 `JMP` 가 모두 이리로) → 핸들이 0 아니므로 `+0xe5 NtClose` → `+0xea` 에서 `EAX = EBX` 로 상태 반환.
  - → **`+0xe5` 의 `NtClose` 는 "열기 실패 길" 이 아니라 열기 성공 뒤 모든 길이 지나는 공용 꼬리.** (열기 실패도 같은 줄을 지나지만 핸들 0 이라 건너뜀.)
- **값 정보 클래스 = `2`** (`KeyValuePartialInformation`, `+0x123 MOV R15D,2` → `+0x131 MOV R8D,R15D`). 결과 버퍼 = `RBP+0x27` (`+0x11f`), 크기 `0x24` (`+0x129 MOV [RSP+0x20],0x24`), `ResultLength` 칸 = `RBP-0x79` (`+0x116`, `+0x11a`). `ValueName` = `RBP-0x49` (`ProductType`, `+0x134`). 버퍼 안: `Type` = `[RBP+0x2b]` (`+0x143 CMP dword [RBP+0x2b],1` → `REG_SZ`), `DataLength` = `[RBP+0x2f]`, `Data` = `RBP+0x33`.
- **갈래 순서** (읽기 성공 뒤):
  1. `+0x143 CMP Type,1` / `+0x147 JNZ +0x1b0` → 형식이 `REG_SZ` 아니면 불일치 처리
  2. `+0x14d MOV RCX,[RBP+0x2f]` / `+0x150 JC +0x1b0` (`DataLength < 2`) → 짧으면 불일치 처리
  3. 비교 ① `WinNt` (`+0x174`, ret `+0x179`) → 같으면 `+0x17b JNZ +0x1ba` → **`*type = 1`** (`+0x1ba MOV [RDI],1`)
  4. 비교 ② `LanmanNt` (`+0x188`, ret `+0x18d`) → 같으면 `+0x18f JNZ +0x1c5` → **`*type = 2`** (`+0x1c5 MOV [RDI],R15D`, `R15D` = 2)
  5. 비교 ③ `ServerNt` (`+0x19c`, ret `+0x1a1`) → 같으면 `+0x1a5 MOV [RDI],3` → **`*type = 3`**
  6. 셋 다 아니면 `+0x1a3 JZ +0x1b0`
- 공통 불일치 값: `+0x1b0 MOV EBX,0xc000090b` → `+0x1b5 JMP +0xdc`. 즉 **형식 불일치·글 짧음·셋 다 불일치 → 상태 `0xc000090b`** (이 레포에서 상태 이름은 확인 안 함). 읽기 실패면 `NtQueryValueKey` 의 상태 그대로 (`EBX = EAX`, `+0x13d`).
- 맞았을 때: `EBX` = `NtQueryValueKey` 의 상태 = 0 (성공) 그대로 반환.
- **비교는 대소문자 무시**: 세 번 다 `R8B = 1` (`+0x165`·`+0x17d`·`+0x191`; `+0x165 MOV R8B,1`). 함수는 `RtlEqualUnicodeString` (ntdll rva `0x5aaa0`, `_calls.tsv` 와 `_index.tsv` 가 같은 이름). 셋째 인자 `CaseInSensitive = 1` 은 공개 시그니처 `RtlEqualUnicodeString(String1, String2, BOOLEAN CaseInSensitive)` 에 따른 해석.
- **`*type = 1` 이 맞나**: 부분적으로만. **이 함수 (`RtlpGetNtProductTypeFromRegistry`) 는 실패 때 `*type` 을 안 쓴다** — 쓰는 건 맞음 갈래의 `1`/`2`/`3` 뿐이다. 실패 때의 `*type = 1` 은 **부른 쪽 `RtlGetNtProductType`** 이 쓴다:
  - `RtlGetNtProductType +0x5d CALL` (ret `+0x62`) → `+0x62 SHR EAX,0x1f` / `+0x65 XOR AL,1` → AL = (상태 ≥ 0)
  - `+0x67 JNZ +0x56` → 성공이면 `*type` 은 건드리지 않고 **`TRUE`** (AL = 1) 반환 (`*type` 은 아까 1/2/3 으로 이미 써짐)
  - 실패 (상태 < 0: 열기 실패·읽기 실패·불일치 모두) 면 `+0x69 MOV dword [RBX],1` → **`*type = 1`** (`NtProductWinNt`), AL = 0 → **`FALSE`** 반환.
  - 값 이름 (`1` = `NtProductWinNt`, `2` = `NtProductLanManNt`, `3` = `NtProductServer`) 은 공개 `NT_PRODUCT_TYPE` 열거형 기준 해석이고, 숫자 `1/2/3` 은 `.asm` 근거.
- 참고 (sast/40 과 같음): `RtlGetNtProductType` 은 `PEB+0x90` 이 0 이 아니고 그 칸의 첫 `dword` ≠ 0 이면 (`+0x12`~`+0x21`) `[그 칸+0x10]` 을 돌려주고 (`+0x33`), 아니면 KUSD `0x7ffe0268` (`ProductTypeIsValid`) ≠ 0 일 때 `0x7ffe0264` (`NtProductType`) 를 돌려주고 (`+0x41`~`+0x52`), 둘 다 아닐 때만 `+0x5d` 로 레지스트리 길을 탄다.
  - `[관찰]` run44 P1a: KUSD `ProductTypeIsValid` = 1 → test.exe 는 이 레지스트리 길을 안 탄다 (요청서 "이미 아는 것").

---

## 동적분석으로 확인해야 할 것

- **필수인 것은 없다.** 글·길이·차례·갈래는 모두 코드와 `.rdata` 바이트에 있다.
- 선택 (덫 설계에 도움이 될 때만):
  - ⓠ-1 이 test.exe 에서 실제로 `NtOpenKey` 까지 가는지는 sast/52 의 정적 판정 ("매니페스트 코드 페이지 0 이라 이 길") 에 기댄다. 확인은 `ntdll!RtlpQueryNlsSystemCodePages` 입구 (또는 `+0xbc` ret 지점) 에서 `r8` 가 가리키는 `OBJECT_ATTRIBUTES` → `ObjectName` 을 `dt _UNICODE_STRING` 으로 보면 `Length=0x7e`, `MaximumLength=0x80` 이 나와야 한다.
  - 레지스트리 실제 값 (`ACP`·`OEMCP` 문자열, `ProductType` 값) 은 Q104 VM 단계 몫 (요청서 범위 밖).
- 추천 멈춤점: `ntdll!RtlpQueryNlsSystemCodePages+0xbc` (`NtOpenKey` 돌아온 곳, 조건 test.exe) 와 `+0x113`·`+0x1c5` (`NtQueryValueKey` 돌아온 곳). 실행 순서·`EAX`(상태) 를 한 번에 볼 수 있다.

## 판정

**분석성공.** PDB GUID 일치, 버전 대조 일치, 질문 1~7 전부 `.asm` 줄·바이트 근거와 함께 답함. 글 8개를 `.rdata` 바이트로 직접 읽어 PDB 심볼 (`??_C@…`) 과 디컴파일 글 둘 다 맞는 것까지 확인. 새 Ghidra 실행 없음. 동적분석 요청 필요 없음.

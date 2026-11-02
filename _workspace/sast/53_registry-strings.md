# 53 registry-strings — 정적 분석 요청

- 날짜: 2026-11-02
- 스프린트: 03_sprint / 예정 run: run58
- 대기열: **Q109 첫 일 (ⓠ-1·ⓠ-2)** — 사용자 결정 2026-11-02 (PE_emulator 요청). Q109 의 나머지 (B·C·D·E) 는 이번에 **안 한다** (다음 바퀴)
- 관련: sast/52 결과 (`LdrpInitializeNlsInfo +0x3d` → `RtlpQueryNlsSystemCodePages`, run57), sast/40 결과 질문 4·시스템 콜 표 (`RtlGetNtProductType` → `RtlpGetNtProductTypeFromRegistry`, run46)
- 왜: PE_emulator 가 레지스트리 값 읽기 덫을 만들 때 **키·값 이름 글자 전체**를 `[정적] run58` 근거로 쓴다. 지금은 글자 일부·짐작만 있다

## 알고 싶은 것

### ⓠ-1 `RtlpQueryNlsSystemCodePages` (ntdll rva `0xec890`, 606B) 의 글 셋
1. 세 글의 **글자 전체** (바이트에서 직접 읽어 확인):
   | rva | 쓰는 자리 | PE_emulator 가 아는 것 |
   |---|---|---|
   | `0x180ad0` | 키 이름 (`NtOpenKey` `ObjectAttributes.ObjectName`) | 앞 15자 `\Registry\Machi`, 길이 `0x80` 바이트. 전체가 `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` 인가? (예/아니오 + 실제 글) |
   | `0x180b50` | 첫째 값 이름 (`NtQueryValueKey`) | `ACP` |
   | `0x180b58` | 둘째 값 이름 | `OEMCP` |
   - 대소문자 그대로 적어 줄 것 (`System` vs `SYSTEM` 등).
2. 글이 **UTF-16 (wide)** 인가, 끝에 NUL 이 있나.
3. `UNICODE_STRING` 의 `Length`·`MaximumLength` 를 **어디서 정하나** — `RtlInitUnicodeString` 류 호출인가, 상수 (`MOV` 즉치값) 인가, `.rdata` 의 미리 만든 `UNICODE_STRING` 인가. 자리 (`+0x` 줄) 와 값.
4. 부르는 syscall 차례 (`NtOpenKey` → `NtQueryValueKey` ×2 → `NtClose` 인가), 각 `+0x` 줄, 값 정보 클래스 번호.
5. 기록 확인 (PE_emulator 가 asm 으로 본 것, 원래 Q109 D (4)): (a) **둘째 값만 다르게 처리** — `+0x121 → +0x171` (첫째) vs `+0x1cf → +0x219` (둘째) 가 맞나, 무엇이 다른가 (b) **모든 출구가 `+0x226 XOR EAX,EAX`** 로 모이나 (= 늘 0 을 돌려주나). 다르면 실제 출구.

### ⓠ-2 제품 종류 키 글 (`RtlpGetNtProductTypeFromRegistry`, ntdll rva `0x5abd4`, 460B)
6. sast/40 이 적은 키 `\Registry\Machine\System\CurrentControlSet\Control\ProductOptions`, 값 `ProductType`, 비교 글 `LanmanNt`/`ServerNt`/`WinNt` — 각 글의 **rva 와 바이트에서 읽은 글자 전체** (대소문자 그대로), UTF-16 인가, `Length`·`MaximumLength` 출처 (질문 3 과 같은 꼴).
7. syscall 차례 확인: sast/40 의 `NtOpenKey +0xd1` · `NtQueryValueKey +0x138` · `NtClose +0xe5` — 주소 순서와 실제 실행 순서가 다르게 보임 (`+0xe5` 가 `+0x138` 보다 앞). 실제 차례 (`NtOpenKey` → `NtQueryValueKey` → `NtClose` 인가) 와 갈래 (`+0xe5` 는 열기 실패 길인가 등). 값 정보 클래스 번호, 비교 실패·열기 실패 때 돌려주는 값 (`*type = 1` 이 맞나), 비교는 대소문자 무시인가 (어느 함수로 비교).

## 대상
| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^RtlpQueryNlsSystemCodePages$` | 0 | ⓠ-1 (`.asm` 이미 있음: `ms/disassemble/ntdll/RtlpQueryNlsSystemCodePages.asm` — 있으면 다시 뽑지 않아도 됨) |
| ntdll | `^RtlpGetNtProductTypeFromRegistry$` | 0 | ⓠ-2 (`.asm` 이미 있음) |
| ntdll | `^RtlGetNtProductType$` | 0 | ⓠ-2 부르는 쪽 (`+0x5d`), 참고만 |
| ntdll `.rdata` | rva `0x180ad0` (`0x80` 바이트), `0x180b50`, `0x180b58`, ⓠ-2 글들 | — | 바이트 직접 읽기 (`ms/bin` 의 ntdll.dll, pefile 등 — 결과 파일은 스크래치에만) |

## 지도에서 본 것
- `_index.tsv` `[정적]`: `RtlpQueryNlsSystemCodePages` rva `0xec890` 606B · `RtlpGetNtProductTypeFromRegistry` rva `0x5abd4` 460B · `RtlGetNtProductType` rva `0x5c7a0` 111B.
- 부르는 곳: `LdrpInitializeNlsInfo +0x3d` (ret `+0x42`) → `RtlpQueryNlsSystemCodePages` (sast/52, test.exe 는 매니페스트 코드 페이지 0 이라 이 길을 탐 `[정적]`). `RtlGetNtProductType +0x5d` → `RtlpGetNtProductTypeFromRegistry` (sast/40).

## 이미 아는 것
- `[정적]` sast/40 (run46): 제품 종류 길 = KUSD `ProductTypeIsValid` (`+0x268`) = 0 && `PEB.SharedData` null 일 때만. 키·값 이름은 위 6번 (어디서 읽었는지 — 디컴파일 글인지 바이트인지 — 결과서에 안 적힘. 이번에 바이트로 확인).
- `[관찰]` run44 P1a 사진: KUSD `ProductTypeIsValid` = 1, `NtProductType` = 1 → **test.exe 는 제품 종류 레지스트리 길을 안 탄다**. (키 값 자체는 Q104 VM 단계 몫 — 이번 범위 밖)
- `[정적]` sast/52 (run57): `RtlpQueryNlsSystemCodePages` 는 NLS 코드 페이지 번호를 레지스트리에서 읽는 함수. 글 셋은 run57 에 안 적힘.
- 확인용 단서 (PE_emulator, `[정적]` 그쪽 기록): ntdll PDB 공개 심볼에 컴파일러 글 심볼 `??_C@…` 이 세 rva 에 있다 (`0009:56016`·`56144`·`56152`). 키 이름 심볼 지문 (CRC) `0x83e3bb6f`, 길이 `0x80`. 셈법은 LLVM `MicrosoftMangle` 공개 소스. — PDB 심볼과 바이트가 맞는지 한 줄로 확인해 주면 좋음 (필수 아님).
- 원문: `../PE_emulator/_workspace/00_planner/45_registry-values.md` "글 근거" 절, `../PE_emulator/_workspace/00_planner/37_nls-codepage.md`.

## 범위 밖
- Q109 B (ntdll 본문 `InitSecurityCookie` 등)·C (ntoskrnl 비트 뜻)·D (1)~(3) 정정·E — 다음 바퀴.
- 레지스트리 실제 값 (VM) — Q104.
- `RtlpQueryNlsSystemCodePages` 가 돌려준 번호를 뒤에서 어떻게 쓰나 (run57 에 있음) — 다시 보지 않음.

## 받고 싶은 것
- 질문별 답 + 근거 (`.asm` 의 `+0x` 줄, 바이트 읽기는 rva·hex 몇 바이트).
- **run 문서에 그대로 옮길 글 표** (rva · 글자 전체 · 인코딩 · 바이트 길이 · `Length`/`MaximumLength` · 쓰는 자리) — PE_emulator 가 근거로 씀.
- PDB GUID 확인 (ntdll `C3093720177DA6851519DCFFE8DF53FE`).
- 동적이 필요한 것이 있으면 따로 (없을 것으로 봄).

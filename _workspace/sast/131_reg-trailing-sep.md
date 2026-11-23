# 131 reg-trailing-sep — 정적 분석 요청

- 날짜: 2026-11-23
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q247** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/282_internals-reg-trailing-sep.md` — 원문을 먼저 읽을 것. 튜닝 `282_tune07.md` R1, 샘플 2,730). 사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 첫째. **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 `ntoskrnl.exe` — **이미 analyze 됨**. 다시 analyze 하지 말고 `^이름$` export 만.
  - 색인에 있는 것 (`ms/decomp/ntoskrnl/_index.tsv`): `CmpParseKey` rva `0x8ba4e0` 1856 B · `CmpDoParseKey` rva `0x867cf0` **11350 B (큼)** · `CmpGetNextName` rva `0x7ef11c` 182 B. 셋 다 `.asm` 없음.
  - 이미 있는 것: `ms/disassemble/ntoskrnl/ObpLookupObjectName.asm` (`+0x20d`·`+0x8bb`·`+0x8ce` — 이름을 손대지 않고 `CmpParseKey` 로 넘김, 원문 "이미 아는 것").
- 주의: 같은 시간에 다른 decompiler 는 없음. flow-writer 가 runs/ 문서를 쓸 수 있음 (`ms/` 안 건드림).

## 왜 (요청한 쪽 말 요약)

- 에뮬레이터 로그 (진짜 Windows 관찰 아님): `NtOpenKey("\Registry\Machine\System\CurrentControlSet\Control\CommonGlobUserSettings\")` (접근 `0x2000000` · 속성 `0x240`) 에 에뮬레이터가 `0xc0000033` (이름 잘못) 으로 답함. 로케일 묻기마다 받는 대답이라 틀리면 넓게 틀림.
- 진짜 VM 에 그 키는 있음 (`[수집]` — 요청 원문).
- mscoree 가 끝 `\` 이름으로 열고 바로 하위 키를 셈 (`[정적] run89` `FindLatestVersionWithCallback +0xe4`) — 성공한다는 직접 근거는 아님.
- 에뮬레이터는 상대 이름 (키 핸들 기준) 끝 `\` 를 `[추측]` + `TODO(VM)` 로 받아 둠 → 이 답으로 닫으려 함.

## 알고 싶은 것

1. **절대 이름 끝 `\` 하나** (`\Registry\Machine\…\Key\`) — `CmpParseKey → CmpDoParseKey → CmpGetNextName` 이 끝 `\` 를 떼고(건너뛰고) 그 키를 여나? 예/아니오. 아니면 어느 줄에서 어떤 status (`0xc0000033` / `0xc0000034` / 그 밖) 인가.
   - `CmpGetNextName` 이 조각을 자르는 규칙: 앞 `\` 건너뛰기, 빈 조각 (길이 0) 을 무엇으로 알리나 (반환값·출력 칸), "마지막 조각" 표시를 어떻게 정하나.
   - `CmpDoParseKey` 가 빈 조각 / 남은 이름 길이 0 을 받았을 때 가는 갈래 (끝으로 보고 지금 키를 여나, 오류인가).
2. **끝 `\\` 둘 · 가운데 `\\`** (`…\Control\\CommonGlobUserSettings`) 도 같은 규칙인가 — 빈 조각을 건너뛰나, 거절하나. 거절이면 status.
3. **상대 이름** (`RootDirectory` = 키 핸들, `ObjectName` = `Key\`) 도 같은 줄을 타나 — `ObpLookupObjectName` 이 루트 핸들이 키일 때 `CmpParseKey` 로 넘기는 남은 이름 꼴이 절대 이름 때와 같은가 (이미 있는 `ObpLookupObjectName.asm` 읽기).
   - 덤: 이름이 `\` 하나뿐 / 빈 이름 (상대) 일 때 — 루트 키 자체를 다시 여나 (요청 원문엔 없음, 같은 줄로 바로 보이면 한 줄만).

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `CmpGetNextName` | 0 | 질문 1·2 (조각 자르기 규칙) |
| ntoskrnl | `CmpParseKey` | 1 | 질문 1·3 (남은 이름을 받는 첫 줄, 빈 이름 갈래) |
| ntoskrnl | `CmpDoParseKey` | 1 | 질문 1·2 — **크다 (11 KB).** 전부 읽지 말고 `CmpGetNextName` 을 부르는 자리들과 그 반환을 견주는 갈래, `0xc0000033`·`0xc0000034` 를 만드는 줄만 |
| ntoskrnl | `ObpLookupObjectName` (있음 — 읽기) | 0 | 질문 3 |

- 범위: 이름 자르기·빈 조각 갈래·status 까지. 하위 키 찾기 (`CmpFindSubKeyByName*`), 보안 검사, 심볼릭 링크 (`CurrentControlSet` → `ControlSet001`) 본문, KCB 캐시 본문은 이름만. 단, 링크를 따라 다시 파싱할 때 남은 이름 (`\CommonGlobUserSettings\`) 이 끝 `\` 를 지닌 채 넘어가는지는 한 줄 (같은 규칙이면 "같음").

## 받고 싶은 것

- 결과서 `_workspace/sast/131_reg-trailing-sep_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/ntoskrnl/<함수>.asm`. 근거는 `[정적] ms/disassemble/ntoskrnl/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄 — 예/아니오 + status).
  3. 표: `이름 꼴 (예 문자열) · 조각 차례 · 빈 조각 처리 · 결과 (연다 / status) · 줄` — 줄: 끝 `\` 하나 · 끝 `\\` · 가운데 `\\` · 상대 `Key\` · (덤) 상대 빈 이름 / `\`.
  4. PDB GUID 확인 줄 (ntoskrnl — 이미 쓰는 프로젝트와 같은지).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall` `src/registry/nt_open_key.cpp:31 ~ 47` (절대 이름 풀이 · I02 상대 갈래 — 지금 끝 `\` 에 `0xc0000033`) · `nt_open_key_ex.cpp` · `nt_create_key.cpp` (만들기도 같은 파싱인지 한 줄).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (원문: VM 에서 `NtOpenKey` 넷 — 멈춤점 `CmpDoParseKey+0x…`) 과 **"다음 요청 후보"**.

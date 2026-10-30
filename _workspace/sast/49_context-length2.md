# 49 context-length2 — 정적 분석 요청

- 날짜: 2026-10-31
- 스프린트: 03_sprint
- 대기열: **Q103** (PE_emulator 요청 1, 사용자 결정 2026-10-31로 Q78d4 앞)
- 관련: run16 `PspInitializeThunkContext +0xa2` (맡긴 문맥 길이), run29 `KiContinuePreviousModeUser +0x133` → `RtlGetExtendedContextLength3`, sast/03 (`NtCreateUserProcess +0xc85` → Length2), sast/11 (맡긴 문맥 저장 위치)

## 배경

- `../PE_emulator`(C++ 에뮬레이터)는 `PspInitializeThunkContext +0xa2`가 구하는 맡긴 문맥 길이를 `0x4ff`로 쓴다.
  - 셈 근거: `RtlGetExtendedContextLength3` asm (`+0x4d ~ +0x58`, `+0x7e`, `+0xe1 ~ +0xee`).
  - "Length2 → Length3 인자 그대로"는 `[추측]` 으로 달아 둠. run44 사진의 맡긴 문맥 주소와는 맞음.
- PE_emulator는 이 결과의 근거 태그만 고친다. 큰 분석은 필요 없다.

## 알고 싶은 것

1. `RtlGetExtendedContextLength2` 의 주소가 rva `0x495440` 이 맞나? (PE_emulator 플래너가 적은 값. `_index.tsv` 에는 `0x495440`, 24바이트로 있음)
2. 이 함수는 `RtlGetExtendedContextLength3` 에 무엇을 넘기나? — 예/아니오로:
   - (a) `rcx`(ContextFlags), `rdx`(길이 받을 곳)를 **그대로** 넘기나?
   - (b) Length3 의 셋째·넷째 인자(`r8`, `r9`)는 무엇으로 채우나? (상수? 호출자의 `r8` 그대로?) — 상수면 그 값.
   - (c) 꼬리 호출(JMP)인가, 보통 호출(CALL) 뒤 돌아와서 뭔가 더 하나? (지도 `_edges.tsv` 에는 `+0xe` CALL, ret `+0x13` 으로 나옴 → CALL 이면 돌아온 뒤 하는 일)
3. `PspInitializeThunkContext +0xa2` 호출 때 넘기는 값 (`ecx` = `R12D` = `0x10001b` 또는 `0x10005b`, `rdx` = `&[RBP]`, `r8d` = `R14D`)을 Length2 가 Length3 로 옮긴 뒤, Length3 가 `0x4ff` 를 내는지 — PE_emulator 의 셈 (`+0x4d ~ +0x58`, `+0x7e`, `+0xe1 ~ +0xee`) 이 Length2 가 넘긴 인자 기준으로도 그대로 성립하나? (예/아니오, 아니면 달라지는 곳)
   - `R14D` 가 무엇인지는 `PspInitializeThunkContext` asm 에서 `+0x98` 앞을 보면 됨 (이미 있음). 필요한 만큼만.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^RtlGetExtendedContextLength2$` | 0 | 질문 1·2 — asm 파일이 아직 없음. **새로 뽑을 것**: `ms/disassemble/ntoskrnl/RtlGetExtendedContextLength2.asm` (+ decomp C) |
| ntoskrnl | `^RtlGetExtendedContextLength3$` | 0 | 질문 3 — 이미 `ms/disassemble/ntoskrnl/RtlGetExtendedContextLength3.asm` 있음. 다시 뽑지 말고 읽기만 |
| ntoskrnl | `^PspInitializeThunkContext$` | 0 | 질문 3 — 이미 있음, `+0x60 ~ +0xa7` 만 읽기 |

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*`)

- `RtlGetExtendedContextLength2` rva `0x495440`, 24바이트, 깊이 1, caller 8곳, expanded.
- 직접 호출 1줄: `+0xe` CALL → `RtlGetExtendedContextLength3` (ret `+0x13`).
- `RtlGetExtendedContextLength3` rva `0x4ee888`, 265바이트, 깊이 2. 직접 호출 `RtlpValidateContextFlags2` (`+0x2a`), `RtlpRemoveArchDisallowedXStateFeatures` (`+0xc1`), `RtlpGetEntireXStateAreaLength2` (`+0xd1`).
- `PspInitializeThunkContext +0xa2` CALL `0x140495440` → Length2. 직전: `+0x98 MOV R8D,R14D`, `+0x9b LEA RDX,[RBP]`, `+0x9f MOV ECX,R12D`. `R12D` = `0x10001b` 또는 (`+0x84~+0x8d`) `+0x40`.

## 이미 아는 것

- `[정적]` run16: `PspInitializeThunkContext +0xa2` 의 결과가 맡긴 문맥 길이 (sast/11 결과서 146줄: `R15 = ([R13+0x98] - 길이) & ~0xf`).
- `[정적]` run29: `KiContinuePreviousModeUser +0x133` → `RtlGetExtendedContextLength3` 직접 호출.
- `[추측]` PE_emulator: Length2 → Length3 인자 그대로, 길이 `0x4ff`.
- `[관찰]` run44: 맡긴 문맥 주소가 `0x4ff` 셈과 맞음.

## 받고 싶은 것

- 질문별 답 + 근거 (`RtlGetExtendedContextLength2.asm` 의 `+0x` 줄, Length3 의 `+0x` 줄).
- PE_emulator 가 그대로 옮겨 쓸 **한 줄 요약** (예: "Length2 = Length3(ecx, rdx, r8, 0) 를 부르고 eax 를 그대로 돌려줌 — `+0x?` `+0x?`").
- 이것만으로 닫히면 동적 확인은 필요 없다고 적어 줄 것. 남는 게 있으면 "동적으로 확인할 것" 에 멈춤점.
- 범위 밖 (하지 말 것): Length3 의 XState 길이 계산 전체를 다시 풀기, 다른 caller 7곳 분석.

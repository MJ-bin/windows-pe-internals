# 60 apiset-phase — 정적 분석 요청

- 날짜: 2026-11-05
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run65
- 대기열: **Q120c** (PE_emulator 요청 11 의 8번 — 정정 후보, PE_emulator 77 planner). 사용자 결정 2026-11-05: Q120b·Q120c 둘 다 진행
- 관련: run57 (sast/52 nls-apiset — "`MiInitializeApiSets` 는 부팅 위상 0"), flow.md 2.x (1410줄 쯤), baggage.md `ⒷA` (36·283·1388·1948줄 쯤)
- **같이 도는 작업**: 같은 때 sast/59 (Q120b) decompiler 가 ntoskrnl 프로젝트를 쓸 수 있다. 이번 질문은 **이미 있는 `.asm`** (`ms/disassemble/ntoskrnl/MiInitSystem.asm`·`MmInitSystem.asm`·`Phase1InitializationDiscard.asm`·`InitBootProcessor.asm`·`MiInitializeApiSets.asm`) 과 PDB 타입으로 답할 수 있으면 Ghidra 를 열지 않는다. 꼭 export 해야 하면 먼저 `pgrep -fa analyzeHeadless` 로 ntoskrnl 프로젝트를 쓰는 작업이 있는지 보고, 있으면 끝난 뒤에 (프로젝트 잠금).

## 알고 싶은 것

1. **`MiInitializeApiSets` 는 부팅 몇 위상에서 불리나 — 위상 0 인가 1 인가**
   - `MiInitSystem` (rva `0xc5bea0`) 의 첫 인자 (ECX = 위상) 갈래: `+0x75 TEST ECX,ECX` · `+0x77 JNZ 0x140c5c2c8` (목적지 = `0x140c5c2c8 − 0x140c5bea0` = `+0x428`) · `+0x428 CMP ECX,EBX` (`+0x71 LEA EBX,[R14+0x1]` — EBX = 1 인가) · `+0x42a JNZ …` (목적지 오프셋은 직접 셀 것)
   - `+0x638/+0x63d CALL MiInitializeApiSets` 가 **어느 갈래 안**에 있나 (위상 0 길 `+0x7d ~` / 위상 1 길 `+0x430 ~` / 위상 2 길) — 그 자리에 닿는 점프·흐름을 `.asm` 으로 따라 근거 줄로
   - `MiInitSystem` 을 부르는 곳 전부와 그때 ECX: `MmInitSystem` 안 자리 (run57 은 `+0x144/+0x149` 에 "위상 0·2 가 같은 자리, `+0xb6 XOR ECX,ECX` / `+0x13c MOV ECX,2`" 라고 적음. PE_emulator 는 `MmInitSystem +0xf0` 이 위상 1 자리라고 봄) — `MmInitSystem` 의 인자 갈래 (위상 0/1/2) 와 각 갈래에서 `MiInitSystem` 을 부르는 call/ret
   - `MmInitSystem` 을 부르는 곳과 위상 값: `InitBootProcessor +0x595` (run57: 위상 0), `Phase1InitializationDiscard +0xc65` (PE_emulator: `MmInitSystem(1)`) 등
   - 결론: run57 의 "위상 0" 이 맞나 틀리나 (예/아니오) + 맞는 사슬 (함수 call/ret)
2. **ApiSet 커널 쪽 전역 칸 이름** — run57 은 "`ntoskrnl +0xe2d818` (섹션 개체) · `+0xe2d820` (커널 뷰 주소) · `+0xe2d828` (크기) — PDB 공개 이름 없음" 이라 적음. PE_emulator: `MiState` (`0x140e2cb80`) `+0x1c0 Sections` → `_MI_SECTION_STATE` `+0xad8 ApiSetSection` · `+0xae0 ApiSetSchema` · `+0xae8 ApiSetSchemaSize` (= `0x140e2d818`·`820`·`828`). PDB 타입 (`_MI_SYSTEM_INFORMATION`·`_MI_SECTION_STATE`) 로 확인: `MiState` rva, `Sections` 칸 오프셋, 세 칸 이름·타입. 셈 (`0xe2cb80 + 0x1c0 + 0xad8 = 0xe2d818`?) 맞나
3. **위상이 바뀌면 바뀌는 것** — run57·flow·baggage 에서 "위상 0" 을 바탕으로 쓴 다른 말이 있나 (예: "`Ⓑ1` 보다 앞", `RtlNlsInitState` 와 같은 위상, `ExInitializeNls` 와의 순서, `PsLocateSystemDlls` 보다 앞인지). 위상 1 안에서 `MmInitSystem(1)` 과 `PsLocateSystemDlls`·`ExInitializeNls` (run57 `ⒷN`) 의 순서를 `Phase1InitializationDiscard` call 자리로 (call/ret)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^MiInitSystem$`, `^MmInitSystem$`, `^Phase1InitializationDiscard$`, `^InitBootProcessor$` | 0 | 1·3 (`.asm` 있음 — 다시 뽑지 않음) |
| ntoskrnl | `^MiInitializeApiSets$` | 0 | 1·2 (`.asm` 있음) |
| ntoskrnl | PDB 타입 `_MI_SYSTEM_INFORMATION`, `_MI_SECTION_STATE`, 심볼 `MiState` | — | 2 |

## 지도·`.asm` 에서 본 것 (오케스트레이터)

- `MiInitSystem.asm`: `+0x71 LEA EBX,[R14 + 0x1]` · `+0x75 TEST ECX,ECX` · `+0x77 JNZ 0x140c5c2c8` · `+0x7d AND dword ptr [MmTrackLockedPages],EBX` · `+0x422 CALL MxInstallMoreMemory` · `+0x427 INT3` · `+0x428 CMP ECX,EBX` · `+0x42a JNZ 0x140c5c4ea` · `+0x430 MOV RAX,GS:[0x188]` · `+0x638 CALL MiInitializeApiSets ret=+0x63d` · `+0x63f JNS 0x140c5c524`
- run57 이 `+0x77 JNZ` 를 "위상 0 쪽" 으로 읽었는데, `JNZ` 는 ECX ≠ 0 일 때 `+0x428` 로 간다 — run57 이 목적지를 잘못 읽은 것으로 보임 `[추측]` (같은 꼴의 실수: run51, run56 ~ run61 주소 끝자리 읽기). 확정은 이번 결과서로

## 이미 아는 것

- `[정적]` run57 (sast/52): `MiInitializeApiSets` 가 로더 블록 스키마를 복사해 pagefile 섹션을 만들고, `MmMapApiSetView +0x92` 가 PEB `+0x68 ApiSetMap` 을 씀 — 이 부분은 이번 정정 대상 아님
- PE_emulator 원문: "`PsLocateSystemDlls` 보다 앞이라 PE_emulator 순서엔 영향 없음"

## 이번에 안 하는 것

- `MiInitializeApiSets` 본문 다시 보기 (run57 그대로)
- 위상 0·1·2 의 다른 일 (ApiSet·NLS 순서에 필요한 만큼만)

## 받고 싶은 것

- 질문별 답 (예/아니오·이름) + 근거 `+0x` 줄 (부르기는 call/ret 둘 다, 점프 목적지는 "목적지 − 함수 시작" — 결과서 쓰기 전에 `.asm` 줄과 다시 대조)
- **고칠 줄 목록**: run57·flow.md·baggage.md 에서 "위상 0"·"PDB 공개 이름 없음" 을 쓴 줄 (파일·줄 번호 쯤) → 고친 말
- 동적으로 확인할 것 (있으면 — 부팅 중이라 KDNET 초기 멈춤이 필요하면 그렇다고)

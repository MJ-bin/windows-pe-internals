# 97 k32-leftovers — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q59 + Q60** (둘 다 kernel32, 작음, 대기열에서 연달아 있음 — 오케스트레이터 기본값으로 한 요청·한 바퀴에 묶음). 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 첫 묶음. **정적만.**
- 빌드: 26100.9457. kernel32 analyze 됨 (PDB 붙음). `_index.tsv`: `BaseThreadInitThunk` rva `0x2cd70` (76 B) · `BasepInitializeTermsrvFpns` `0x2cdc4` (203 B) · `LoadLibraryExW` `0x3bd50` (7 B, **thunk**) · `LoadLibraryW` `0x3ee30` (7 B, thunk) · `LoadLibraryA` `0x428f0` (7 B, thunk). `BasepInitializeTermsrvFpns.asm` 은 이미 있을 수 있음 (이름만 뽑힘) — 있으면 그대로 읽는다.
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/96) 을 돌린다 — ntoskrnl 쪽은 건드리지 않는다.

## 알고 싶은 것

### Q59 — `BasepInitializeTermsrvFpns` 안쪽

- 관련: run25 (`runs/02_sprint/run25-dll-entry.md`) `BaseThreadInitThunk` 안쪽 depth 1 이름만 — 첫 인자 ≠ 0 이면 `+0x26` (ret `+0x2d`) ntdll `RtlGetSuiteMask` → `AL & 0x10` 이면 `+0x36` (ret `+0x3b`) `BasepInitializeTermsrvFpns`. 그 첫 인자 ≠ 0 호출은 `LdrpInitializeProcess +0x1b59` 의 `Kernel32ThreadInitThunkFunction(1, 0, 0)` 으로 보임 `[추측]` (flow.md 4장 2.4·2.9 줄).
1. `BasepInitializeTermsrvFpns` 가 하는 일: 부르는 것 (DLL 올리기? `GetProcAddress`? 전역 함수 포인터 채우기?) — 함수 이름·오프셋·쓰는 전역 (rva·이름). syscall 이 있나 (있으면 이름).
2. `RtlGetSuiteMask` 의 `0x10` 비트 뜻 (`VER_SUITE_TERMINAL` `0x10` `[추측]`) — 이 값은 KUSD `SuiteMask` 에서 오나 (ntdll `RtlGetSuiteMask` 한 줄만). test.exe 를 돌린 VM (Education, 콘솔 세션) 에서 이 갈래를 타나 — KUSD 값은 run44 사진 로그 (`ms/logs/` 의 KUSD `SuiteMask` 줄이 있으면) 로 확인, 없으면 `[VM]`.
3. `BaseThreadInitThunk(1, 0, 0)` 의 나머지 — 첫 인자 ≠ 0 갈래에서 `BasepInitializeTermsrvFpns` 말고 하는 일이 있나 (돌아오는 자리). 짐 지도에 넣을 전역 칸이 생기나 (`baggage.md` 7장 kernel32 전역에 있으면 그 줄).

### Q60 — `KERNEL32!LoadLibraryExW` → `KERNELBASE!LoadLibraryExW`

- 관련: test.exe 는 `KERNEL32.dll` 의 `LoadLibraryExW` 를 import (IAT `0x6e250`). run7 `[관찰]` 콜스택은 `KERNELBASE!LoadLibraryExW`. sast/31 계산: test.exe KERNEL32 import 중 forwarder 는 `NTDLL.Rtl…` 10개뿐 → `LoadLibraryExW` 는 forwarder 아님 → 짧은 스텁 `[추측]`.
4. kernel32 `LoadLibraryExW` (7 B thunk) 의 바이트 — `JMP [rip+X]` 인가, 그 `X` 는 kernel32 IAT 의 어느 칸이고 그 칸은 `KERNELBASE.dll!LoadLibraryExW` import 인가 (`api-ms-win-core-libraryloader-l1-*` ApiSet 이름이면 그 이름도). kernel32 export 표에서 `LoadLibraryExW` 가 forwarder 문자열인지 RVA 인지 (`objdump -p` 또는 `pefile`).
5. 그래서 test.exe → kernel32 스텁 → kernelbase 한 번 `JMP` (콜스택에 kernel32 프레임이 안 남는 까닭) 가 맞나 — 한 줄.
6. (작게) 같은 꼴로 `LoadLibraryW`·`LoadLibraryA` 도 스텁인가 (O/X 만).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernel32 | `^BasepInitializeTermsrvFpns$` | 1 | Q59 질문 1 |
| kernel32 | `^BaseThreadInitThunk$` | 0 | Q59 질문 3 — 첫 인자 ≠ 0 갈래만 |
| ntdll | `^RtlGetSuiteMask$` | 0 | Q59 질문 2 — 읽는 칸만 |
| kernel32 | `^LoadLibraryExW$`, `^LoadLibraryW$`, `^LoadLibraryA$` | 0 | Q60 — thunk 바이트, IAT 칸, export 표 |

- 범위: 위 질문만. `BasepInitializeTermsrvFpns` 가 채운 함수 포인터를 **누가 쓰나** 는 이름만 (caller 목록 길면 개수).
- 공용 부품은 이름만.

## 이미 아는 것

- `[정적]` run25: `BaseThreadInitThunk` 첫 인자 0 이면 `+0x12` CFG 거쳐 `RDX(R8)` 간접 호출 → `+0x19 RtlExitUserThread`.
- `[관찰]` run7: 콜스택 `KERNELBASE!LoadLibraryExW`.

## 받고 싶은 것

- 결과서 `_workspace/sast/97_k32-leftovers_result.md`. Q59 · Q60 절을 나눠 질문별 답 + 근거 (`.asm` `+0x` 줄, PE 표 값), `[정적]`.
- Q59: 하는 일 표 (오프셋 · 부르는 것 · 쓰는 전역), test.exe 가 이 갈래를 타나 (예/아니오/`[VM]`).
- Q60: 스텁 바이트 · IAT 칸 · import 이름 한 줄.
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

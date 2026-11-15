# 104 params-block — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q63a** (= Q63 (a)·(b)·(c)·(e)·(f), 커널 쪽). Q63 의 (d) (ntdll 새로 읽기 — 블록 문서 없는 칸·`Flags 0x4000/0x8000`·`RtlNormalizeProcessParams`) 는 "차례 때 사용자와 정함" 이라 **Q63b 로 떼어 사용자 결정 대기** (2026-11-15). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: 26100.9457. ntoskrnl analyze 됨. `_index.tsv`: `PspCaptureUserProcessParameters` `0x9f1110` (2071) · `MiCfgInitializeProcess` `0x90b634` (454) · `PspLocateNtdllAddressesForScpCfg` `0xc44708` (565) · `PspBuildCreateProcessContext` `0x840920` (6374) · `PspSetupUserProcessAddressSpace` `0xae5d34` (655) · `PspCopyAndFixupParameters` `0xae57e4` (1340) · `PspPrepareSystemDllInitBlock` `0x9395d0` (812).
- 관련: run32 (`runs/02_sprint/run32-params-initblock.md`, sast/27) — 매개변수 블록·시작 정보 블록. run16 (`runs/02_sprint/run16-thunk-context.md`) — `LdrInitializeThunk` 로 가는 `Rdx`. sast/102 결과서 — `Flags & 0x20000000` = run32 비트 29, `PspSetupUserProcessAddressSpace +0xe6/+0xea` 에서 켬.
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/103) 을 돌린다 — ntdll 쪽은 건드리지 않는다. 이미 있는 ntoskrnl `.asm`·`.c` 는 다시 뽑지 않고, 필요한 것만 `^이름$`.

## 알고 싶은 것

1. **(a) `PspCaptureUserProcessParameters` 의 `Flags` 마스크 `0xf4010e11`** — 각 비트 (0·4·9·10·11·16·26·28·29·30·31) 가 무엇인가 (PDB 열거나 비트 이름이 있으면 그 이름, 없으면 코드에서 그 비트를 읽는 곳으로 뜻 짐작 `[추측]`), 그리고 Environment 를 블록 안 어디에 두나 (뒤에 붙이나, 따로 할당하나 — 오프셋 계산 줄).
2. **(b) `MiCfgInitializeProcess`** 가 작업 집합 구조 `+0x4a0`/`+0x4a8` 을 쓰나 (O/X + 줄). `PspLocateNtdllAddressesForScpCfg` 가 넣는 ntdll 함수 이름들 (이름 문자열 → 어느 전역 칸).
3. **(c) `PspBuildCreateProcessContext`** 가 만들기 정보 `+0x11` (표준 핸들 상태) 를 채우는 줄과 값 뜻.
4. **(e) `ProcessParameters.Flags` 비트 31** (run17 `LdrpIsSecureProcess` 가 읽음) 을 누가 켜나 — 입력 마스크 `0xf4010e11` 에 있어 사용자가 줄 수 있는 비트인데 run32 가 읽은 커널 줄은 비트 14·15·29 만 켬. 커널 전수 (`_calls.tsv`·`.asm` 으로 매개변수 블록 `+0x8` 에 `0x80000000` 을 넣는 줄).
5. **(f) 작은 것**: `PackageDependencyData` (`+0x400`) · `CurrentDirectores[32]` 안쪽 포인터를 `PspCopyAndFixupParameters` 가 왜 안 고치나 (고치는 칸 목록 줄로 보이는 만큼); 스위치 이름의 "SegmentHeap"·"SCP" 뜻 (이름만, `[추측]` 표시); `LdrInitializeThunk` 로 가는 `Rdx` 출처 이름 — run16·flow.md `PspSystemDlls[0]+0x20` vs sast/27 `PspNativeSystemDllData +0x20` 이 같은 칸인가 (PDB 기호 주소 비교).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^PspCaptureUserProcessParameters$` | 0 | 질문 1 |
| ntoskrnl | `^MiCfgInitializeProcess$`, `^PspLocateNtdllAddressesForScpCfg$` | 0 | 질문 2 |
| ntoskrnl | `^PspBuildCreateProcessContext$` | 0 | 질문 3 — `+0x11` 쓰는 줄만 |
| ntoskrnl | 블록 `+0x8` 비트 31 쓰기 전수 | — | 질문 4 |
| ntoskrnl | `^PspCopyAndFixupParameters$` | 0 | 질문 5 |
| ntoskrnl PDB | `PspSystemDlls`·`PspNativeSystemDllData` 주소 | — | 질문 5 |

- 범위: 위 질문만. **ntdll 쪽 (Q63 (d)) 은 이번에 안 한다.**
- 공용 부품은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/104_params-block_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: 비트 표 (비트 · 이름/뜻 · 근거).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

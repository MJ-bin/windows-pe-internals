# 116 shimdata-read — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q90** (ntdll 이 PEB 칸을 **읽는** 곳 — 문맥 쪽 (`+0x2f8`·`+0x308`) 은 sast/85 로 닫힘, **`pShimData` (`+0x2d8`) 읽는 쪽만 남음**). 사용자 질문 2026-10-27 ("부모가 써 준 PEB 칸을 ntdll 이 쓰나"). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntdll.dll (PDB GUID 확인)
- 관련: sast/85 결과서 (`_workspace/sast/85_sxs-redirect_result.md` 311~312줄 — `pShimData` 읽는 곳 **후보**: `LdrpInitializeProcess +0x69f`, `LdrpCheckComponentOnDemandEtwEvent +0x6c`, `RtlpHpOptIntoSegmentHeap +0x184`, `SbSelectProc…` 등), sast/84 결과서 (106~109줄 — 부모 `CreateProcessInternalW +0x56bb ~ +0x5831` 이 `NtAllocateVirtualMemory` + `NtWriteVirtualMemory` ×3 으로 앱 호환 데이터를 쓰고 `+0x2d8` 에 주소, run45 사진 개인 영역 `25996d10000`), sast/40 결과서 D1 (`pShimData` 안 두 바이트 `+0x11aa` 비트 `0x40`·`+0x11b2` 비트 `0x4` — 세그먼트 힙 쪽, Q91 동적), sast/113 결과서 (`RtlpForceCSToUseEvents` 등 shim·AppCompat 조건).
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/115) 을 돌린다 — ntoskrnl 쪽은 건드리지 않는다. 이미 있는 ntdll `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`. 읽기 전수는 스크래치 objdump (`/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/ntdll.dis` 류) 로 PEB `+0x2d8` 읽기 꼴 전수.

## 알고 싶은 것

1. **`pShimData` 를 읽는 곳 전수** (초기화 범위 `LdrInitializeThunk` ~ `NtContinue` 안) — 함수+오프셋, 읽은 값 (블록 주소) 으로 무엇을 하나 (블록 안 칸 오프셋·뜻 — 세그먼트 힙 켜기 `+0x11aa`/`+0x11b2`, shim 엔진 켜기 `g_ShimsEnabled`, ETW, SwitchBack `SbSelectProcedure` 류), 사진 구간.
2. **블록 꼴** — ntdll 이 읽는 칸만으로 `pShimData` 블록 칸 표 (오프셋 · 크기 · 읽는 줄 · 뜻). 부모 (kernel32 `BasepInitAppCompatData`·kernelbase `CreateProcessInternalW`) 가 쓰는 꼴과 맞춰 볼 수 있으면 한 줄 (이미 있는 kernelbase/kernel32 `.asm` 읽기만).
3. **test.exe 에서 무엇이 갈리나** — 블록이 있을 때 (run45 사진: 있음) 각 읽기 갈래의 결과 (세그먼트 힙 안 씀·shim 안 켬 등) — 사진 값 (`ms/logs/04_baggage-read_*` 의 `25996d10000` 블록 덤프가 있으면) 으로.
4. 짐 지도: `pShimData` 블록 (유저, 부모가 놓음) 을 baggage 3.1·7.6 에 "읽는 쪽" 근거로 보탤 줄.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntdll | PEB `+0x2d8` 읽기 전수 (objdump) → 든 함수 | 0 | 질문 1 |
| ntdll | sast/85 후보 넷 (있으면 읽기만) | 0 | 질문 1·2 |
| kernel32·kernelbase | `BasepInitAppCompatData`·`CreateProcessInternalW` (있음 — 읽기만) | — | 질문 2 대조 |

- 범위: 읽는 쪽만. shim 엔진 본문 (apphelp·`SE_*`) 은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/116_shimdata-read_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`, 블록 칸 표, baggage 줄.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (에뮬레이터가 `pShimData` 를 0 으로 두면 어느 갈래가 달라지나 — 모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** (Q91 과 겹치면 표시) 과 **"다음 요청 후보"**.

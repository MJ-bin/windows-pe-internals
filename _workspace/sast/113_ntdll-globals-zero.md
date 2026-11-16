# 113 ntdll-globals-zero — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q78g2** — Q78g 의 나머지: ntdll `끝까지 0` 전역 + `.00cfg`. Q78 짐 지도 C 의 **마지막 조각**. 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntdll.dll (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` **7.3** `.data` `끝까지 0` (215) · `.mrdata` `끝까지 0` (22) · `.00cfg` `못 읽음` (4) — 이름 목록은 7.3 그대로 (결과서에 전부 적기).
  - **sast/111 결과서 389줄 아래 "Q78g2 (이어서)" 표** — `끝까지 0` 중 이름 칸에 쓰는 줄이 지도 안 (`m3~m10`) 에 있는 **53개** (`.data` 39 + `.mrdata` 14). 이 53 부터 가르고, 나머지 (약 180) 는 "쓰는 줄이 지도 밖 / 없음" 쪽이라 `_refs` 전수로 빠르게 가른다. sast/111 이 이미 닫은 8개 (`ShowSnaps`·`RtlpDisableHeapLookaside` 이름 칸 정정 등) 는 빼기.
  - sast/57·59·62·65·109·111 결과서 (같은 방법·같은 네 가지 가름, 셈 기준: 명시 0 저장·같은 칸 되쓰기만 "0 을 씀").
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/112) 을 돌린다 — ntoskrnl 쪽은 건드리지 않는다. 이미 있는 ntdll `.asm`·`.c` 는 다시 뽑지 않고 (sast/111 이 `RtlSetHeapInformation` 을 한 번 다시 쓴 일이 있음 — `^이름$` 로 정확히), 전역 쓰기 전수는 `_refs/` 와 스크래치 objdump 풀이 (`/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/` 의 `ntdll.dis`·`s111/` 도우미 재사용).

## 범위

- **`.data` `끝까지 0` 215** · **`.mrdata` `끝까지 0` 22** (7.3 그대로) — 사진에서 P2 까지 0 이었던 전역.
- **`.00cfg` 4** (`못 읽음`): __guard_xfg_check_icall_fptr, __guard_xfg_dispatch_icall_fptr, __guard_xfg_table_dispatch_icall_fptr, __castguard_check_failure_os_handled_fptr — 사진에서 못 읽은 칸. 파일 처음 값 + 쓰는 줄 (`LdrpCfgProcessLoadConfig`·`LdrpInitializeCfgScpHelpers` 류) 로 가름.

## 알고 싶은 것

1. **전역마다 가름** — 네 가지 (0 을 씀 / 조건부 안 탐 + 조건 / 아무도 안 씀 (초기화 길 안) / 범위 밖 길에서 씀 `[추측]` — ntdll 범위 밖 길이면 쓰는 함수 이름 하나, 예: 예외 처리·디버그·WER·shim·App Verifier·트랜잭션·파이버·종료 길). 끝까지 0 이므로 "아는 칸 (값을 씀)" 은 0 을 쓰는 경우뿐.
   - 큰 묶음은 묶어서: App Verifier (`AVrf*`·`Avrf*`), shim 엔진 콜아웃 (`g_pfnSE_*`), WOW64 (`Wow64*`·`g_LdrpWow64*`), SxS 추적 (`g_Sxs*`), 기능 스위치 보고 (`Feature_*__private_reporting`), WIL (`g_wil_*`), CRT — 묶음마다 조건 (test.exe 가 왜 안 타나) 한 줄과 대표 쓰는 줄.
2. **`.00cfg` 4칸** — 파일 값, 쓰는 줄 (있으면), test.exe 에서 P2 까지의 값 (파일 값 그대로 추정 근거).
3. **간접 호출** — 쓰는 줄이 간접 호출 대상 안이면 좁히고, 못 정하면 동적으로.
4. **줄기와 경로** — 0 을 쓰는 줄·조건부 줄만 baggage 3장 양식으로 (아무도 안 씀·범위 밖은 7장 목록 정리만).

## 받고 싶은 것

- 결과서 `_workspace/sast/113_ntdll-globals-zero_result.md`. sast/111 결과서 꼴:
  - 전역 241 전부의 가름 표 (구역 · 이름 · 가름 · 쓰는 줄/조건 · 묶음)
  - 셈 요약 (가름별 수), 7.3 라벨 정정이 있으면
  - baggage.md 3.8·3.9·3.10 에 넣을 줄 (3장 양식)과 7.3 남은 수
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"** — 이걸로 Q78 (짐 지도 C) 정적 조각이 다 끝나면 **baggage 2장 "아직 모름" 남은 수** (구조체 7.2 · 전역 7.3) 를 한 줄로.

# 105 dll-crt-small — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q73** (2.8 sample dll CRT 남은 것, 작음 — CRT 한 상자 원칙이라 대표 지점 목표엔 덜 중요). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: sample (`sample/` 소스와 같은 빌드의 dll1·dll2). dll1·dll2 analyze 됨. `_index.tsv` (dll1 / dll2 rva·크기): `__scrt_uninitialize_crt` `0x23c0`/`0x23b0` (43) · `dllmain_crt_dispatch` `0x1990`/`0x1980` (86) · `__scrt_dllmain_crt_thread_attach` `0x2110`/`0x2100` (41) · `__scrt_dllmain_crt_thread_detach` `0x2140`/`0x2130` (21) · `__acrt_execute_initializers` `0x50e88`/`0x50e78` (126) · `try_get_function_slow` `0x4f598`/`0x4f588` (433).
- 관련: run40 (`runs/02_sprint/run40-dll-crt-entry.md`, sast/35) — dll `_DllMainCRTStartup` → `dllmain_dispatch` → `dllmain_crt_dispatch` 이유 1 길. sast/81 결과서 (Q149b, `/MT` CRT lowio) 도 참고.
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/103) 과 **ntoskrnl** (sast/106) 을 돌린다 — 그쪽은 건드리지 않는다. dll1·dll2 둘은 같은 CRT 라 **dll1 을 읽고 dll2 는 오프셋 대조만** (다르면 적기).

## 알고 싶은 것

1. **(a) `FlsGetValue2` 길**: `__acrt_execute_initializers` 의 함수 포인터 표에서 `FlsGetValue2` 를 찾는 길이 dll 에서도 `try_get_function_slow` (`GetProcAddress` 류) 로 가나 — 이름 문자열·모듈 이름 (`kernel32`/`api-ms-win-core-fibers-l2-1-?`) 을 적기. 이 길이 은퇴 전 (P2 앞) syscall 을 내나 (`LoadLibraryExW` 로 ApiSet DLL 을 여나 — 플래그).
2. **(b) `__scrt_uninitialize_crt(ECX = (lpReserved ≠ 0), EDX = 0)`** 안쪽 — 이유 0 (DETACH) 에서 무엇이 달라지나 (첫 인자가 0/1 일 때 갈림). 범위가 프로세스 종료라 지도 밖이면 이름·갈림만.
3. **(c) `dllmain_crt_dispatch` 이유 2·3 길** (`__scrt_dllmain_crt_thread_attach/detach`) — 무엇을 부르나 (TLS·FLS 정리?), syscall 이 있나. sample 은 `CreateThread` 1개 → 이 길이 새 스레드 시작 때 탄다 (지도의 "새 스레드" 는 다음 범위라 이름 수준이면 충분).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| dll1 | `^__acrt_execute_initializers$`, `^try_get_function_slow$` | 1 | 질문 1 |
| dll1 | `^__scrt_uninitialize_crt$` | 1 | 질문 2 |
| dll1 | `^dllmain_crt_dispatch$`, `^__scrt_dllmain_crt_thread_attach$`, `^__scrt_dllmain_crt_thread_detach$` | 1 | 질문 3 |
| dll2 | 같은 이름 | 0 | 오프셋 대조만 |

- 범위: 위 질문만. CRT 깊은 곳 (locale, stdio) 은 이름만.
- 이미 있는 `.asm`·`.c` 는 다시 뽑지 않고 그대로 읽는다. 새로 필요한 것만 `^이름$`.

## 받고 싶은 것

- 결과서 `_workspace/sast/105_dll-crt-small_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** (Q31 과 겹치면 표시) 과 **"다음 요청 후보"**.

# 102 search-path-small — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q71** (2.4·2.6 검색 경로 주변 작은 정적). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 빌드: 26100.9457. ntdll analyze 됨. `_index.tsv`: `LdrpInitializePerUserWindowsDirectory` `0xfb04c` (388) · `RtlpComputeDllPathWithOptions` `0xba650` (315) · `LdrpInitializeDllPath` `0x899a0` (167) · `LdrpComputeLazyDllPath` `0xb9870` (295) · `LdrpGetDllPath` `0xb9f30` (635). `_refs/RtlpSystemDirs.tsv` · `_refs/RtlpSystem32Dirs.tsv` 있음.
- 관련: run39 (`runs/02_sprint/run39-dll-search-path.md`, sast/34) — DLL 검색 경로 짜기. run32 (`PspPrepareSystemDllInitBlock`, `ProcessParameters.Flags` 비트 29). sast/97 결과서 (`_workspace/sast/97_k32-leftovers_result.md`) — `BaseThreadInitThunk(1,0,0)` 뒤 `LdrpInitializeProcess +0x1b72 LdrpInitializePerUserWindowsDirectory` 로 이어짐.
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/101) 을 돌린다 — ntoskrnl 쪽은 건드리지 않는다. 이미 있는 ntdll `.asm`·`.c` 를 다시 쓰지 않게 요청한 함수만 `^이름$` 로 정확히. ntdll 전체 줄 찾기가 필요하면 `objdump -d` 전수 (sast/100 이 같은 방법을 씀) 로.

## 알고 싶은 것

1. **(a) `LdrpInitializePerUserWindowsDirectory`** (`LdrpInitializeProcess +0x1b72`): `RtlpSystemDirs`·`RtlpSystem32Dirs` 를 다시 쓰는 줄 (`+0x157`·`+0x164`) — 무엇을 바꾸나 (어떤 글로), 조건 (test.exe 에서 타나 — 조건 칸 값으로, 모르면 `[VM]`), 그 뒤 검색 경로 캐시 (`LdrpComputeLazyDllPath` 류 전역) 를 다시 짓나 / 무효로 하나. sample 의 `LoadLibraryExW` ×3 (진입점 뒤) 에 영향이 있나 `[추측]` — 코드로.
2. **(b) `LdrSystemDllInitBlock+0xa0` 비트 60~61** 의 뜻 (1 이면 목록이 시스템 폴더 하나 — run32 `PspPrepareSystemDllInitBlock` 짝). ntdll 에서 읽는 줄과 갈림.
3. **(c) 모듈 항목 `+0x118 DependentLoadFlags`** 를 쓰는 줄 (ntdll 전수) — 어디서 값을 가져오나 (PE load config `DependentLoadFlags` 칸인가). sample 값 0 의 근거가 링크 옵션뿐이었음.
4. **(d) `<Windows>` 문자열 출처 줄** (검색 경로의 Windows 폴더 글이 어디서 오나 — KUSD `NtSystemRoot`? PEB?), `ProcessParameters.Flags & 0x20000000` (forwarders 폴더) 이 run32 비트 29 와 같은가 (같은 칸·같은 비트인가).
5. (작은 것) `RtlpComputeDllPathWithOptions` 안쪽 — 옵션 비트별로 경로 조각 차례; 매개변수 사본 `P+0x28 ~ +0x77` 읽는 곳; `.` 칸이 PEB `CurrentDirectory` 로 풀리나.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpInitializePerUserWindowsDirectory$` | 1 | 질문 1 |
| ntdll | `^RtlpComputeDllPathWithOptions$` | 1 | 질문 5 |
| ntdll | `^LdrpInitializeDllPath$`, `^LdrpComputeLazyDllPath$`, `^LdrpGetDllPath$` | 0 | 질문 1·4 — 캐시·`<Windows>` 글 |
| ntdll | `+0x118` 쓰기 · `LdrSystemDllInitBlock+0xa0` 읽기 전수 | — | 질문 2·3 |

- 범위: 위 질문만. 검색 경로로 실제 파일을 찾는 길 (`LdrpSearchPath`·`RtlDosSearchPath_Ustr`) 은 run39 에서 읽음 — 다시 안 본다.
- 공용 부품은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/102_search-path-small_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: 바뀌는 글 · 조건 · 캐시 다시 짓기 표, test.exe 가 타나 (예/아니오/`[VM]`).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

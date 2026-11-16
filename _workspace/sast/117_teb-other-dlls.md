# 117 teb-other-dlls — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q102** (`_TEB` "범위 밖 길 — 다른 DLL" 39칸 확인, 작은 정적). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 — `ms/bin/.../System32/` 의 gdi32·gdi32full·user32 (analyze 됨, PDB 있음), opengl32·ws2_32·combase·imm32 등 (analyze 안 됨).
- 관련: **sast/48 결과서 168줄 아래 "다른 DLL (39)"** (칸 이름 · 쓰는 DLL 짐작 `[추측]` — 이름·objdump 근거, 깊이 안 팜) — 이 39칸이 범위. run54 (`runs/03_sprint/run54-*.md`), baggage 3.3 `_TEB` (범위 밖 다른 DLL 39). run85·86 (user32·gdi32 시작 함수 — 이미 본 TEB 칸이 있으면 근거 재사용). 초기화 동안 0 이라는 것 (`[관찰]` run44, K2~P2 일곱 장) 은 그대로.
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/116) 과 **ntoskrnl** (sast/118) 을 돌린다 — 그쪽은 건드리지 않는다.
- **방법 (가벼운 쪽 먼저)**: 새 analyze 를 하지 말고, 먼저 각 DLL 바이트를 `objdump -d` 로 풀어 **TEB 칸 쓰기 꼴** (`mov rax,gs:[0x30]` 뒤 `[rax+칸]` 에 쓰기, 또는 `gs:[칸]` 직접 쓰기) 을 칸 오프셋별로 찾는다. 찾은 자리는 PDB 가 있으면 함수 이름 (`pdb_aliases.py --where <모듈> <rva>` 또는 공개 심볼), 없으면 rva 만. **PDB 가 없는 DLL 에서 이름이 꼭 필요할 때만** `fetch_pdb.py` 로 PDB 를 받고 (analyze 는 안 함) 공개 심볼로 이름을 붙인다. Ghidra analyze 는 objdump 로 안 풀리는 칸이 남을 때만, 결과서에 이유를 적고.

## 알고 싶은 것

1. **39칸마다**: 짐작한 DLL 에 **쓰는 줄이 실제로 있나** (O/X, 함수 이름·rva), 짐작이 틀렸으면 실제로 쓰는 DLL. 그 함수가 **test.exe 초기화 동안 불리나** — test.exe 는 user32·gdi32 를 안 올림 (`[소스]` `sample/log.h:3`, run85) → 대부분 "범위 밖 (다른 DLL, 안 올림)" 그대로일 것. 올리는 DLL (kernel32·kernelbase) 에서 쓰는 칸이면 test.exe 길에서 타나.
2. 칸 묶음별로 묶어서 적어도 됨: `Gdi*` (gdi32full/win32u), `gl*` (opengl32), `WinSockData` (ws2_32), `ReservedForOle` (combase/ole32), `CsrClientThread` (user32/csrsrv), `Win32ThreadInfo` (win32k 커널 콜아웃 — 커널 쪽이면 이름만) 등.
3. baggage 3.3 의 "범위 밖 길 — 다른 DLL 39" 를 줄 단위로 바꿀 수 있게: 칸 · 쓰는 DLL · 함수 · test.exe 에서 타나.

## 받고 싶은 것

- 결과서 `_workspace/sast/117_teb-other-dlls_result.md`. 39칸 표 (칸 · 짐작 · 실제 DLL · 쓰는 자리 · 근거 방법 (objdump/PDB 공개 심볼/Ghidra) · test.exe 에서 타나), 셈 요약, 검색 한계 한 줄.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

# 150 gdi-font-dc — 정적 분석 요청

- 날짜: 2026-11-27
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 쪽 남은 일)
- 대기열: **Q278b** (대기열에 있던 항목). PE_emulator 380 묶음 차례 ④ (`../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 머리 "차례 (우리 D179)" — "④ Q278b · Q282 (대기열)"). 사용자 결정 2026-11-27. **정적만.**
- 먼저 읽는 것: `_workspace/plan.md` 의 **Q278b 줄** (할 일 목록 전부) · `_workspace/sast/145_gdi-display-dc_result.md` 전부 — 특히 **"win32k 몫 남음" 1 ~ 6** · "다음 요청 후보" (N2 · N3 · 필터 값 5 갈래) · "PE_emulator 쪽" · "stub 이 채울 최소 목록" · `runs/03_sprint/run147-gdi-display-dc.md` · `run144` (`_workspace/sast/141_gdi-startup_result.md`). 원 PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/367_internals-gdi-display-dc.md` "다음" 몫 (읽기만).
- 대상 빌드: 26100.9457 x64 `System32/`: `gdiplus.dll` · `gdi32.dll` · `gdi32full.dll` · `user32.dll` (`GetDC` 쪽만) · `win32kfull.sys` · `win32kbase.sys` — 모두 프로젝트 있음.
- 같은 시간에 도는 decompiler: sast/147 (ntdll32 · kernelbase32 · wow64 · csrsrv) · sast/148 (rpcss · combase · ntdll · sechost · advapi32) · sast/149 → 154 (rpcrt4 · winnsi) · sast/152 (windows.storage · shell32 · appinfo · kernelbase) · sast/153 (ntoskrnl · ntdll). **이 요청은 gdiplus · gdi32 · gdi32full · user32 · win32kfull · win32kbase 만.** 이 결과서가 나온 뒤 sast/151 (user32 · win32k) 이 같은 프로젝트를 연다. 새 export 전에 `free -g`. scratchpad 임시 파일은 `s150/` 하위.
- **스크립트 주의** (145 "스크립트 문제"): 하네스 a5e73e4 뒤로 겹친 이름은 `__<rva>` 가 늘 붙음 — 그래도 `^이름$` 로 뽑기 전에 같은 이름 `.asm` 이 있는지, 첫 줄 `rva=` 를 확인할 것 (gdiplus `Initialize` 류).

## 알고 싶은 것 (Q278b 줄 — 차례대로)

1. **N3 글 폭** — 메시지 상자 12 샘플이 막힌 길: `NtUserGetDCEx` DC 에서 `GdiGetCharDimensions` · `pcfLocateCFONT` · `bComputeTextExtent` 가 읽는 칸 (`DC_ATTR+0x114` · `+0x128` …) · 부르는 syscall · 0 / 실패면 어디로. 그리고 145 "win32k 몫" 5: `_GetDCEx` → `CreateCacheDC` (DCE 만들기) · `UserGetDesktopDC` UI 한도 (`CanPerformUIOperation` · `HasUILimit`) — `NtUserGetDCEx(0,0,3)` = 0 이 되는 조건.
2. **win32k 표 채우기** (145 "win32k 몫" 1 · 3): 세션 기본 `DC_ATTR` 틀 `[W32GetSessionState()+0x58]+0x90` (0x1b0 B) 처음 값 · 스톡 핸들 · `GDIINFO` (`PDEV+0x848`) · `pGdiDevCaps` 블록을 채우는 곳.
3. **필터 값 5 갈래**: `GetDeviceCaps +0x419` · user32 `GetDC +0x0` 의 `g_systemCallFilterId == 5` 길 — 그 값을 정하는 쪽 (`NtQueryInformationProcess` 어느 클래스) 과 5 일 때 무엇이 달라지나.
4. 145 "win32k 몫" 2 · 4 · 6: `GrepCreateCompatibleDC` (win32kbase rva `0x14950`) · DPI 칸 (`W32PROCESS+0x10c/+0x110` · `스레드+0x190/+0x198`) · 주 화면 판정 `[session+0xe338]+0x30` · `hdcOpenDCW`.
5. **N2**: gdiplus `InitImagingLibrary` (`+0x62b`) · `SysColorNotify` · `InitSystemFontsDirs` · `GetGpInstalledFontCollection` — 부르는 syscall · 레지스트리 · 파일 (읽는 키 · 경로 이름).
6. 그 밖 Q278b 줄: N5 `NtQueryInformationProcess(0x25)` 출처 · 표 칸 `+0x0`/`+0xf` 뜻 · `g_pEdgeGdi` 갈래 · `[GpDevice+0x628]` 조건.

- 범위 밖: `LoadIconW` → `NtGdiCreateBitmap` (Q274b `LoadIcoCur` 와 같이 — 이번에 안 함).
- 분량이 크면 **1 → 2 → 3 → 4 → 5 → 6** 차례로 쓰고 나머지는 "다음 요청으로".

## 받고 싶은 것

- 결과서 `_workspace/sast/150_gdi-font-dc_result.md`. 근거 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋`.
  1. 맨 위 **"먼저 읽는 한 줄 답"** (1 ~ 6).
  2. 표: 글 폭 길 지도 (145 (1) 꼴 — `차례 · 부르는 것 / syscall · 조건 · 반환을 보나 · 0 이면 · 읽는 / 쓰는 칸`) · 세션 `DC_ATTR` 틀 칸 표 · `GDIINFO` / `pGdiDevCaps` 채우는 곳 표 · 필터 5 갈래 표 · N2 가 읽는 레지스트리 · 파일 표.
  3. PDB GUID 확인 줄.
- **"PE_emulator 쪽 — 지금 맞나·틀리나"** (145 "PE_emulator 쪽" 에 이어): 메시지 상자 12 샘플을 풀려면 stub 이 무엇을 돌려줘야 하나 · "stub 이 채울 최소 목록" 에 이어 붙일 줄.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에:
  - **"VM 으로 확인할 것 — 디버거 필요"** (Q277 VM GUI 세션에 얹을 꼴)
  - **"VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것"** (글꼴 레지스트리 (`HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts` 등) · 글꼴 파일 · 화면 설정 · DPI 값 등 — 수집 스크립트 재료)
  - **"다음 요청 후보"**

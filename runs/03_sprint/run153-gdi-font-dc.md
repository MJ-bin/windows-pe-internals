# 2026-11-27 (작성일, 정적 분석 2026-11-27) run153 — 메시지 상자 글 폭 · 세션 기본 `DC_ATTR` 틀 · 필터 값 5 · gdiplus 시작 뒷몫 (Q278b, 정적만): 메시지 상자 12 샘플이 막힌 글 폭 길은 user32 `UserGetCharDimensionsEx` → gdi32full **`GdiGetCharDimensions` → `pcfLocateCFONT` → `bGetTextMetricsWInternal`** 이고, **글꼴 사본 (CFONT) 길이 막혀도 syscall 되돌이 (`NtGdiGetTextMetricsW` · `NtGdiGetTextExtent`) 로 지나간다** — 그래서 에뮬레이터는 사본을 안 만들어도 되고, DC 표 칸 (run147 목록 8 ~ 10) 위에 **`NtGdiGetTextMetricsW` · `NtGdiGetTextExtent` 를 "0 아님 + 값" 으로** 주면 풀린다. `NtUserGetDCEx(0,0,3)` 가 0 이 되는 길은 일반 프로세스 · 화면 있음이면 **없다** (12 샘플의 0 은 stub 탓). 세션 기본 `DC_ATTR` 틀은 `InitializeGre` → `InitializeDefaultDCAttributes` 가 채우고 (`+0x88 = 1` · `+0x40 = 0x4b` · 붓 · 펜 · 글꼴 = 스톡 0 · 7 · 13), `g_systemCallFilterId` 는 `EPROCESS+0x6e8` (부모가 준 Win32k 필터 속성) 하위 4 비트라 **일반 프로세스는 0**. gdiplus `+0x62b ~ +0x7b3` 은 **벽이 아니고** (`InitImagingLibrary` 늘 1 · 파일 · 레지스트리 안 읽음), 새로 나오는 것은 `BackgroundThreadStartup` 의 **이벤트 둘 + 스레드 하나**

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 점프 목적지도 **함수 시작 기준 `+0x…`** 로만 적었다 — 주소 끝자리와 안 섞음. 결과서가 `[objdump]` 로 적은 줄 (kernelbase · ntdll 쪽 · `apisetschema` · 기능 상태 파일 값) 은 그 표시를 그대로 둔다. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** (원문 `../PE_emulator/_workspace/05_tuning_planner/380_internals-r19-lanes.md` 차례 ④ · `367_internals-gdi-display-dc.md` "다음" 몫 — 읽기만) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 · gdi32 · gdiplus 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run144 · run147 과 같은 줄), 사진 PEB `GdiSharedHandleTable` (null) `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`, run86 · run144 · run147 과 같은 줄). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (gdi32 · gdi32full · gdiplus · user32 · win32kbase · win32kfull 줄 · 근거 줄) · 3장 gdi32 하위 절 한 줄 · run142 하위 절 한 줄 · run144 하위 절 한 줄 · run147 하위 절 한 줄 · 그 뒤 새 하위 절 "메시지 상자 글 폭 · …" · 4장 (run144 정적 줄 표시 넷 · run147 동적 한 마디 · run147 정적 줄 해결됨 · Q278b 닫힘 줄 · run153 새 줄 둘) · 5장 (run153 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q278b** (Q278 남은 몫 — 380 묶음 정적 차례 ④ 앞 몫, 사용자 결정 2026-11-27 (PE_emulator D179)). 결과서 판정 **분석성공** — 물음 1 ~ 5 · 6 ② ~ ④ 를 줄 근거로 닫음, 6 ① (N5 `NtQIP(0x25)` 를 누가 부르나) 은 반만 (`[공백]`).
> - 이어 쓰는 앞 run: **run147** (sast/145 — HDC 종류 비트 `0x10000` · 표 칸 · `DC_ATTR` `0x170` B · `GetDeviceCaps` · "win32k 몫 남음" 1 ~ 6 · stub 최소 목록 8 ~ 16), **run144** (sast/141 — 공유 핸들 표 · 6절 "표가 비면 syscall 없이 실패" · N2 ~ N5), **run142** (sast/139 — 메시지 상자 `SoftModalMessageBox +0xfc` · `+0x153` 벽), **run86** (PEB `+0xf8` 공유 메모리 · `GdiProcessSetup +0x161` 언어팩 칸), **run85** (user32 `psi`).
> - 앞 run 고쳐 읽기 둘: run144 · run147 문서 · 해설서 맨 위 한 줄씩 (아래 "앞 run 고쳐 읽기").
> - 남은 것 → 동적은 **Q277** (같은 VM GUI 세션에 얹을 꼴), 디버거 없이 떠 올 것은 380 **2 단계 VM 수집 스크립트** 재료, 정적은 결과서 "다음 요청 후보" (대기열 자리는 오케스트레이터가 정함).

## 요약

- **1 글 폭 길** `[정적]`: `SoftModalMessageBox +0xf5 NtUserGetDCEx(0,0,3)` → `+0x126 SelectObject(글꼴)` → `+0x149 UserGetCharDimensionsEx` → (`+0x72 GetTextFaceW` → syscall `NtGdiGetTextFaceW`) → `+0x92 GdiGetCharDimensions(hdc, 0, &cy)` → `+0x119 pcfLocateCFONT` → `+0x12e bGetTextMetricsWInternal` → (가변 폭 글꼴이면) `+0x1c0 GetTextExtentPointWInternal` → 다시 `SoftModalMessageBox +0x249` · `MB_GetMaxButtonSize +0x74` 의 `GetTextExtentPointW`.
  - **표 검사 실패면 `GdiGetCharDimensions` 는 syscall 없이 0** (`+0x19c`), `*cy` 도 안 쓴다 → `SoftModalMessageBox +0x153 JZ → +0x69f` → `MessageBoxW` 0 (run142 · run144 그대로).
  - 표가 맞으면 사본 길이 막혀도 (`pcfLocateCFONT` = NULL) **`NtGdiGetTextMetricsW` (`0x106e`) · `NtGdiGetTextExtent` (`0x108c`) 되돌이**로 지나간다. 0 을 받으면 그때 폭 0.
  - **`GetTextExtentPointW*` 는 표 검사 앞에서 `GetDCDpiScaleValue(hdc) == 1` 을 먼저 본다** — 1 이 아니면 곧장 `NtGdiGetTextExtent` (run144 6절 "`0x57` + 0" 을 좁힘 → 앞 run 고쳐 읽기).
- **1 `NtUserGetDCEx(0,0,3)` = 0 조건** `[정적]`: ① `hrgn == [세션+0xace8]` ② `flags & 0xffdaf800` (`3` 은 0) — 둘 다 `0x57` ③ UI 한도 걸린 프로세스 + 기능 꺼짐 + `ValidateHwnd(바탕 창)` 0 → `5` ④ `CreateCacheDC` 0 (주 화면 장치 `[세션+0xe338]+0x30` 없음 따위) · `GreSetDCOwnerEx` 실패. **일반 프로세스 · 화면 있음이면 0 이 될 길이 없다.**
- **2 세션 기본 `DC_ATTR` 틀** `[정적]`: `InitializeGre +0xb8 → InitializeDefaultDCAttributes` 가 `[W32GetSessionState()+0x58]+0x90` (0x1b0 B) 을 채운다. `+0x88 = 1` (DPI 비율) · `+0x98 = 0x10` (DC 사본엔 `0x12001f`) · `+0x40 = 0x4b` · `+0x20 = +0x2c = 16.0f` · `+0x154 = 0x1802` · `+0xf0 = 0` · `+0x108 = +0x114 = 0`. 붓 `+0xa0` = 스톡 0 · 펜 `+0xa8` = 스톡 7 · 글꼴 `+0x128` = 스톡 13 (마지막은 오프셋 일치로 짐작 `[추측]`). 스톡 핸들 = 객체 핸들에 `0x800000` 켠 값.
- **2 `GDIINFO` · `pGdiDevCaps`** `[정적]`: 블록 (`+0x180000 ~ +0x180090`) 은 `GreUpdateSharedDevCaps → vGetDeviceCaps` 가 `GDIINFO` (`PDEV+0x848`) 에서 옮겨 적는다. 가짜 화면 (`StubDisp*`) 이면 `bInitGdiInfo` 가 상수로 채움 (1024 × 768 · 32 비트 · 60 Hz), 진짜 드라이버 쪽은 간접 호출이라 `[공백]`. `+0x180094` (언어팩) 은 `InitializeGre` 가 레지스트리 `…\Control\LanguagePack` 의 DWORD 값으로, `+0x180098` 은 1.
- **3 필터 값 5** `[정적]` + `[objdump]`: `g_systemCallFilterId` = `GetProcessMitigationPolicy(정책 0xb)` 하위 4 비트 → kernelbase 가 `NtQueryInformationProcess(자기, 0x34, …)` 로 바꿔 부름 `[objdump]` → 커널 `PsGetWin32KFilterSet()` = `[EPROCESS+0x6e8]` ← `PspAllocateProcess +0x1f11` (부모가 준 속성). **일반 프로세스는 0.** 5 면 user32 `GetDC` 가 syscall 없이 가짜 HDC `0xFFFFFFFFDCDE5000` 을 돌려주고, gdi32 · user32 25 곳이 다른 길로 간다.
- **4 win32k 몫 2 · 4 · 6** `[정적]`: `GrepCreateCompatibleDC` 는 원본 DC 의 PDEV (없으면 주 화면 장치) 로 `GreCreateDisplayDC(…, 1, 0)` (메모리 DC). DPI 칸: `W32PROCESS+0x10c` ← `NtUserSetProcessDpiAwarenessContext +0xc4`, `+0x110` ← `InitUserScreen +0x396` · `SetDPIinSetup +0x492`, `스레드+0x190` ← `W32SetCurrentThreadDpiAwarenessContext +0x6d`, `DC_ATTR+0x88` ← `SetCachedDpiScaleValue` 만 (보통 1). 주 화면 장치 `[세션+0xe338]+0x30` ← `InitVideo +0x180` · `SetDPIinSetup +0x3e5`.
- **5 N2 gdiplus** `[정적]`: **벽이 아니다.** `InitImagingLibrary` 늘 1, `SysColorNotify` = `GetSysColor` ×4 (syscall 없음, `gpsi` 필요), `InitSystemFontsDirs` = 시스템 폴더 문자열 + `\fonts` (파일 · 레지스트리 안 건드림), `GetGpInstalledFontCollection` = 빈 표 할당뿐. 새로 나오는 것은 `+0x7b3 BackgroundThreadStartup` 의 **`CreateEventW` ×2 · `LoadLibraryExA` (자기) · `CreateThread`** — 하나라도 NULL 이면 `GdiplusStartup` 실패. 새 스레드는 숨은 창 · 팔레트 · `GetDC` 를 하고 메시지 고리에서 기다린다 (반환 안 봄).
- **6 그 밖** `[정적]` + `[objdump]`: N5 — kernelbase 안에서 `NtQueryInformationProcess(0x25)` 를 부르는 줄은 `GetProcessVersion` 뿐 `[objdump]` 이고 그 함수를 부르는 kernelbase 줄은 없다 → gdiplus 쪽 출처는 `[공백]`. 표 칸 `+0xf` 비트 `0x2` 는 `SelectObjectImpl +0x398` 이 본다 (`+0x0` 은 아무도 안 읽음). `g_pEdgeGdi` 는 보통 0 `[추측]`. `[GpDevice+0x628]` 은 32 비트 화면이면 안 읽는다.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백열한째 바퀴. plan **Q278b**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 없음.** gdiplus · gdi32 · gdi32full · user32 (`GetDC` · `ReleaseDC` · `GetSysColor` 쪽) · win32kfull · win32kbase 여섯 프로젝트에 `-readOnly` export · refs 만 (결과서). ntoskrnl · ntdll · kernelbase · combase 는 Ghidra 로 안 열고, 있던 `.asm` 읽기 또는 PE 파일 `objdump` / 파이썬 (`[objdump]`) 만 (결과서).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| gdi32 | `92C6EC2ABE478836C99BC83C2F3AE3551` | 같음 (run86 · run144 · run147) |
| gdi32full | `A6245E4F5C78EB06438EA4820F1F6EB41` | 같음 (run86 · run144 · run147) |
| gdiplus (`GdiPlus.dll`) | `DDF19AA09F285660C86065C38EF751A41` | 같음 (run144 · run147) |
| user32 | `CDB947D1B96B87977915DE04CF54279C1` | 같음 (run85 · run142 · run146) |
| win32kbase | `D22DFDA3D54B87E6BD0567A57877904C1` | 같음 (run136 · run142 · run147) |
| win32kfull | `5CD57181BCFDC5AE8F4819BEB1196F091` | 같음 (run136 · run142 · run147) |

- 버전 대조 (결과서 + flow-writer): run147 · 요청서에 적힌 줄 (`hdcCreateDCW +0xc9` · `InternalGdiplusStartup +0x280 · +0x296 · +0x60b · +0x62b · +0x6d5 · +0x73d · +0x775 · +0x7b3` · `GetDeviceCaps +0x419` · `GreCreateDisplayDC +0x231 / +0x243` · `DCMEMOBJ +0x1b8`) 은 `.asm` 과 **일치**. user32 `GetDC` rva `0x4a8c0` (run147 의 `[objdump]` 값) 은 이번에 `GetDC.asm` 첫 줄 `rva=0x4a8c0` 으로 확인.
- syscall 번호 (`ms/disassemble/win32u/_syscalls.tsv`, flow-writer 가 다시 봄): `NtGdiGetTextFaceW` `0x1078` · `NtGdiQueryFontAssocInfo` `0x10e1` · `NtGdiComputeXformCoefficients` `0x1083` · `NtGdiGetDCDpiScaleValue` `0x12be` · `NtGdiGetTextMetricsW` `0x106e` · `NtGdiGetTextExtent` `0x108c` · `NtGdiGetWidthTable` `0x1063` · `NtUserGetDCEx` `0x108a` · `NtUserReleaseDC` `0x151c` · `NtGdiSelectFont` `0x1034` · `NtGdiCreatePaletteInternal` `0x109e` — 이름 일치.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 숫자 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): 메시지 상자 12 샘플이 `NtUserGetDCEx(0,0,3)` 기본 대답 0 ×2 → `MessageBoxW` 0 으로 끝남 (run147 목적). run147 이 HDC · 표 칸 · `DC_ATTR` 을 닫았지만 그 HDC 로 **글 폭을 재는 길** (run144 N3) 은 남았다. 같이 남은 것: win32k 표 채우기 (N4 · run147 "win32k 몫" 1 ~ 6), 필터 값 5 갈래, gdiplus `+0x62b` 뒤 (N2), N5 · 표 칸 뜻 · `g_pEdgeGdi` · `[GpDevice+0x628]`. 요청서 `_workspace/sast/150_gdi-font-dc.md`:
  1. N3 글 폭 — `GdiGetCharDimensions` · `pcfLocateCFONT` · `bComputeTextExtent` 가 읽는 칸 · syscall · 실패면 어디로. `NtUserGetDCEx(0,0,3)` = 0 조건 (`_GetDCEx` → `CreateCacheDC` · UI 한도).
  2. 세션 기본 `DC_ATTR` 틀 처음 값 · 스톡 핸들 · `GDIINFO` · `pGdiDevCaps` 채우는 곳.
  3. 필터 값 5 — 출처와 5 일 때 무엇이 달라지나.
  4. `GrepCreateCompatibleDC` · DPI 칸 · 주 화면 판정 · `hdcOpenDCW`.
  5. N2 — gdiplus 시작 뒷몫이 부르는 syscall · 레지스트리 · 파일.
  6. N5 · 표 칸 `+0x0` / `+0xf` · `g_pEdgeGdi` · `[GpDevice+0x628]`.

## 방법

- 정적 sast/150 (결과서 `_workspace/sast/150_gdi-font-dc_result.md`, **분석성공**, 스크립트 오류 없음 — 쓰는 법 메모 하나 "`rva:` 와 정규식은 한 인자에 못 섞음" → 도구 메모):
  - gdi32full 새로 (깊이 0): `pcfLocateCFONT` · `bComputeTextExtent` · `bComputeTextExtentDBCS` · `bGetTextMetricsWInternal` · `GetTextExtentPointWInternal` · `GetDCDpiScaleValue` · `pcfCreateCFONT` · `pcfAllocCFONT` · `InsertCFontCache` · `vRemoveCFONTFromDcAttr` · `DeleteHDCCFont` · `bFillWidthTableForGTE` · `bFillWidthTableForGCW` · `GetTextFaceW`.
  - win32kbase 새로: `InitializeDefaultDCAttributes` · `InitializeGre` · `InitializeGreCSRSS` · `GdiProcessCallout` · `bInitGdiInfo` · `bInitICM` · `bInitBrush` · `bInitBRUSHOBJ` · `bInitPALOBJ` · `bInitBMOBJ` · `bInit` 둘 (`bInit__1094c0` · `bInit__1af630`) · `GreUpdateSharedDevCaps` · `vGetDeviceCaps` · `GreGetDeviceCaps` · `StubDispEnablePDEV` · `StubDispGetModes` · `MulEnablePDEV` · `EnablePDEV` · `LpkShapeQueryRoutine` · `InitVideo` · `InitUserScreen` · `SetDPIinSetup` · DPI 함수 열넷 · `MonitorFromHdev`.
  - win32kfull 새로: `bSetStockFont` · `bInitStockFontsInternal` · `bInitOneStockFontInternal` · `FinishStockFontInit` · `FinishStockFontInitInternal` · `vInitEmergencyStockFont`.
  - gdi32 새로: `IsPseudoDesktopDcForFilteredProcess` · `D3DKMTOpenAdapterFromGdiDisplayName`. user32 새로: `GetDC` · `ReleaseDC` · `GetSysColor`. gdiplus 새로: `GpInstalledFontCollection` (+ 깊이 1 `GpFontTable` 등) · `CreateThreadAndAddRefDll` · `AddRefModule` · `VGAHashRebuildTable` · `BackgroundThreadProc` · `DllRefCountSafeThreadThunk` · `InternalNotificationStartup`.
  - refs: gdi32full `0x10c59c` (`guintDBCScp`) · `0x108d20` (`gpwcANSICharSet`) · `0x108cf0` (`gbLpk`) · `0x1084ac` (글꼴 연결 상태) · user32 `0xd9970` · gdi32 `0x25780` (`g_systemCallFilterId`).
  - `[objdump]`: kernelbase `GetProcessMitigationPolicy` (rva `0x216f0`) · `GetProcessVersion` (`0x23dc0`) · `GetVersionExW` (`0xc4ae0`), `apisetschema.dll` 계약 `ext-ms-win-gdi-edgegdi-l1-1-0` 의 호스트 이름 (파이썬 파서), win32kbase `.data` `Feature_LearningModeExpansion__private_featureState` (rva `0x2b2938`) 파일 값.
- 같은 시간에 다른 decompiler 가 sast/147 · 148 · 149 → 154 · 152 · 153 을 돌았다. 이 바퀴가 연 프로젝트는 위 여섯뿐 (결과서). flow-writer 는 이 바퀴 `.asm` 을 읽기만 했다.
- flow-writer 가 `.asm` 으로 다시 본 줄 (결과서 offset 대조):
  - gdi32full **`GdiGetCharDimensions +0x48 · +0x58 · +0x6c · +0x7a · +0x80 · +0x91 · +0xa8 · +0xb7 · +0xd7 · +0xef · +0x119 · +0x128 · +0x12e · +0x148 · +0x14a · +0x14c · +0x155 · +0x15f · +0x165 · +0x167 · +0x16f · +0x173 ~ +0x17b · +0x19c · +0x1c0 · +0x1c7 · +0x1c9 · +0x1e5 · +0x1ec · +0x1f0 · +0x1f8`**; `bGetTextMetricsWInternal +0x4c · +0x104 · +0x114 · +0x125 · +0x13c · +0x177`; `GetTextFaceW +0x10 · +0x1c · +0x25`; **`pcfLocateCFONT +0x112 · +0x11d · +0x12c · +0x12f · +0x135 · +0x13b · +0x142 · +0x14e · +0x154 · +0x156 · +0x15c · +0x16a · +0x170 · +0x17a · +0x180 · +0x184 · +0x18a · +0x1a9 · +0x223 · +0x25e · +0x286 ~ +0x2a2 · +0x2bf · +0x2e3 · +0x341 ~ +0x35c · +0x366 ~ +0x389 · +0x3db · +0x3e4 · +0x40b · +0x4bb · +0x4e2 · +0x570 ~ +0x5cd`**; `pcfCreateCFONT +0x19 · +0x2d · +0x44 · +0x5f`; **`GetTextExtentPointWInternal +0x2b · +0x40 ~ +0x75 · +0x7e · +0x8e · +0xc8 · +0x119 · +0x11f · +0x195 · +0x1a8 · +0x1c9 · +0x1d5 · +0x20b · +0x247 · +0x24e ~ +0x26f · +0x281 · +0x288`**; **`GetTextExtentPointW +0x10 ~ +0x70 · +0x89 · +0x8e · +0x124 · +0x192 · +0x197 · +0x1b9 · +0x1be · +0x214 ~ +0x26e`**; `GetDCDpiScaleValue` 갈래 전부 (`+0xc ~ +0x11f`); `bComputeTextExtent +0x13 · +0x2e · +0x38 · +0x179 · +0x17b · +0x183`; `SelectObjectImpl +0x398 · +0x3a3 · +0x3b3 · +0x3bc · +0x3bf · +0x3c5`; `GdiProcessSetup +0xf7 ~ +0x138 · +0x161 · +0x168`.
  - user32 **`SoftModalMessageBox +0xf5 · +0xfc · +0x104 · +0x107 · +0x126 · +0x12d · +0x149 · +0x14e · +0x151 · +0x153 · +0x16f · +0x249 · +0x250 · +0x266 · +0x2e0 · +0x2ef · +0x2f6`**; `UserGetCharDimensionsEx +0x72 · +0x79 · +0x7e · +0x92 · +0x99`; `MB_GetMaxButtonSize +0x74 · +0x7b · +0x8e`; **`GetDC +0x0 · +0x7 · +0x9 · +0x15`**; `ReleaseDC +0xc · +0xe · +0x15 · +0x43 · +0x4a`; `ClientThreadSetup +0x14 · +0x45`; `_UserClientDllInitialize +0x42c · +0x463 · +0x487 · +0x48e · +0x494 · +0x7de · +0x7e7 · +0x7ec · +0x7f1`; `GetSysColor +0x4 · +0x9 · +0x13`.
  - gdi32 **`GetDeviceCaps +0x2b · +0x41 · +0x47 · +0x53 · +0x419 · +0x420 · +0x426`**; `Gdi32DllInitialize +0x4b · +0x62`; `IsPseudoDesktopDcForFilteredProcess +0x0 · +0x7 · +0xd`; `D3DKMTOpenAdapterFromGdiDisplayName +0x6 · +0x10 · +0x83`.
  - win32kfull **`NtUserGetDCEx +0x28 ~ +0x1a4`** (call · 점프 전부); `bInitStockFontsInternal +0x2f0 · +0x300`; `FinishStockFontInit +0x18d · +0x198`.
  - win32kbase **`_GetDCEx +0x5f · +0x2b9 · +0x2c6 · +0x2d5 · +0x2f1 · +0x402 · +0x4c5 · +0x52a · +0xa1c · +0xa21 · +0xa24`**; **`CreateCacheDC +0x30 · +0x33 · +0x38 · +0x3e · +0x72 · +0x83 · +0x88 · +0x90 · +0x2b0 · +0x2f6 · +0x2fe · +0x310 · +0x327`**; `UserGetDesktopDC +0xed · +0xf0 · +0x106 · +0x129 · +0x157 · +0x17e · +0x188 · +0x196 · +0x1a6 · +0x1a8 · +0x1d7 · +0x1e5`; **`InitializeGre +0x9a · +0xb8 · +0xbd · +0x4ac · +0x4b8 · +0x4bf · +0x4cb`**; **`InitializeDefaultDCAttributes +0x40 ~ +0x172`** (쓰는 줄 28 개를 `G` 오프셋 − 0x90 으로 틀 칸에 맞춤); `bInitBrush +0xa0`; `bInitBRUSHOBJ +0x46 · +0x278 · +0x286`; `bSetStockObject +0x33 · +0x47 · +0x79 · +0x80`; `GreUpdateSharedDevCaps +0x1a ~ +0x2a`; `vGetDeviceCaps +0x1b · +0x31 · +0x35 · +0x77 · +0x7d · +0xb3 · +0xe3 · +0x12d · +0x133 · +0x15a`; `bInitGdiInfo +0x20 · +0x58 · +0x72 · +0x16f · +0x171`; `StubDispEnablePDEV +0x33 · +0x38 · +0xaa · +0xdb`; `LpkShapeQueryRoutine +0x22 · +0x32`; `InitVideo +0x11a · +0x162 · +0x16a · +0x179 · +0x180 · +0x1ba · +0x1bf`; `SetDPIinSetup +0x3de · +0x3e5 · +0x492`; `InitUserScreen +0x396`; `GrepCreateCompatibleDC +0x28 · +0x38 · +0x126 ~ +0x16c · +0x1f9 · +0x227 · +0x22e · +0x256 · +0x25b · +0x293 · +0x2a4`; `NtUserSetProcessDpiAwarenessContext +0x73 · +0xa6 ~ +0xc4 · +0x1a7`; `W32SetCurrentThreadDpiAwarenessContext +0x6d · +0x98`; `SetCachedDpiScaleValue +0x97 · +0xd4`; `vUpdateCachedDPIScaleValue +0x16 · +0x1c`; `vSetDpiScaling +0x6 · +0x14`; `GreCreateDisplayDC +0xbf · +0x14c ~ +0x187 · +0x231 · +0x243`; `hdcOpenDCW +0x68 · +0xfa · +0x117 · +0x18a · +0x278 · +0x2ba · +0x426 · +0x470 · +0x492 · +0x4ab`; `NtGdiOpenDCW +0x171`.
  - ntoskrnl (있던 파일 읽기만) `NtQueryInformationProcess +0x1d38 ~ +0x1d53`; `PspAllocateProcess +0x1ed6 · +0x1f0e · +0x1f11`.
  - gdiplus **`InternalGdiplusStartup +0x60b · +0x62b · +0x632 · +0x6d5 · +0x6e1 · +0x72a · +0x73d · +0x742 · +0x744 · +0x775 · +0x781 · +0x78b · +0x7a8 · +0x7ad · +0x7b3 · +0x7ba · +0x7bc · +0x7d1`**; `InitImagingLibrary +0x28 · +0x2d · +0x32`; `InitSystemFontsDirs +0x3c · +0x43 · +0x51 · +0x75 · +0x96 · +0xed · +0x103 · +0x108 · +0x154`; **`BackgroundThreadStartup +0x16 · +0x1d · +0x28 · +0x36 · +0x3d · +0x48 · +0x83 · +0x8d · +0x8f · +0xb4`**; `CreateThreadAndAddRefDll +0x27 · +0x3c · +0x85 · +0x8c`; `AddRefModule +0x39 · +0x40 · +0x45 · +0x4c · +0x58 · +0x5f`; `InternalNotificationStartup` call 열여덟 (`+0x83 ~ +0x431`) · `+0x224 · +0x227 · +0x234`; `BackgroundThreadProc +0xd · +0x25 · +0x74 · +0x99 · +0xf8 · +0x109`; `GpDevice +0xa0 · +0x1e5 · +0x1ea`; `GpInstalledFontCollection +0x31 ~ +0x5f`; `GpFontTable +0x25`.
  - `_syscalls.tsv` 번호 열하나 (위 환경), `ms/sym/*.pdb.info.txt` 여섯.
  - `../PE_emulator*` 는 **열지 않았다** (결과서도 안 염 — "PE_emulator 쪽" 은 run147 때 읽은 파일 상태 기준의 판정).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유 (run147 을 이어):

- **HDC 는 "도화지 대여증"**, 공유 핸들 표는 **"출입증 대장"** 이다 (run144 · run147). 메시지 상자는 대여증을 받으면 "이 도화지에 쓸 글씨가 한 글자에 몇 칸이냐" 를 먼저 잰다.
- gdi32full 은 자로 재기 전에 **"글꼴 자 서랍" (CFONT 사본)** 을 연다. 서랍에 맞는 자가 있으면 본점 (커널) 에 안 묻고 바로 재고, 없거나 서랍이 잠겼으면 **본점에 전화** 해서 잰다 (`NtGdiGetTextMetricsW` · `NtGdiGetTextExtent`).
- 그러니 에뮬레이터는 서랍을 안 만들어도 된다. **전화를 받아 "몇 칸" 이라고 답만 해 주면** 된다. 다만 대여증이 대장에 없으면 창구가 아예 자를 안 꺼낸다 (폭 0) `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{user32,gdi32,gdi32full,gdiplus,win32kbase,win32kfull}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `user32!SoftModalMessageBox` (`0x21f20`) | **`+0xf5`** | **`+0xfc`** | **`NtUserGetDCEx(0,0,3)`** (`0x108a`) → `+0x104 TEST · +0x107 JZ → +0x636` | [정적] |
| 〃 | `+0x126` | `+0x12d` | `SelectObject(hdc, 글꼴)` — 이전 글꼴 저장만 | [정적] |
| 〃 | **`+0x149`** | **`+0x14e`** | **`UserGetCharDimensionsEx(hdc, 0, 0, &cy)`** → `+0x151 TEST EDX · +0x153 JZ → +0x69f` | [정적] |
| 〃 | `+0x16f` · `+0x249` | `+0x174` · `+0x250` | `MB_GetMaxButtonSize` · `GetTextExtentPointW(제목)` (`+0x266` 가 `cx` 읽음) | [정적] |
| 〃 | `+0x2e0` · `+0x2ef` | `+0x2e7` · `+0x2f6` | `DeleteObject(글꼴)` · `NtUserReleaseDC` (`0x151c`) | [정적] |
| `user32!UserGetCharDimensionsEx` (`0x22b7c`) | `+0x72` | `+0x79` | `GetTextFaceW(hdc, 0x20, 버퍼)` → `+0x7e CMP [버퍼],0x40` (`'@'`) | [정적] |
| 〃 | **`+0x92`** | **`+0x99`** | **gdi32 `GdiGetCharDimensions`** ⇒ gdi32full (`0x4db20`) | [정적] |
| `user32!MB_GetMaxButtonSize` (`0x22eb0`) | `+0x74` · `+0x8e` | `+0x7b` · `+0x93` | `GetTextExtentPointW` · `PSMGetTextExtent` (다른 갈래) | [정적] |
| `gdi32full!GetTextFaceW` (`0x583f0`) | `+0x10` `JMP` | — | `NtGdiGetTextFaceW` (`0x1078`) — 개수 ≤ 0 이면 `+0x25 GdiSetLastError(0x57)` | [정적] |
| `gdi32full!GdiGetCharDimensions` (`0x4db20`) | `+0x48` · `+0x6c` | `+0x4d` · `+0x73` | `HANDLE_TO_INDEX` · `GdiGetEntry` (표 검사, 실패 → `+0x19c` 0) | [정적] |
| 〃 | `+0xef` | `+0xf6` | `RtlEnterCriticalSection(semLocal)` | [정적] |
| 〃 | **`+0x119`** | **`+0x11e`** | **`pcfLocateCFONT`** (`0x1bbd0`) → `+0x128 MOV RDI,[RAX]` = 사본 | [정적] |
| 〃 | **`+0x12e`** | **`+0x133`** | **`bGetTextMetricsWInternal`** (`0x4dd30`) → `+0x148 TEST · +0x14a JZ` | [정적] |
| 〃 | `+0x1c0` | `+0x1c5` | `GetTextExtentPointWInternal(52 글자)` (`0x11b90`) — 가변 폭일 때만 | [정적] |
| `gdi32full!bGetTextMetricsWInternal` | `+0x4c` | `+0x51` | `memcpy` (사본에 메트릭 있음 — syscall 없음) | [정적] |
| 〃 | **`+0x104`** | **`+0x10b`** | **`NtGdiGetTextMetricsW(hdc, 버퍼, 0x44)`** (`0x106e`) → `+0x114 JZ` | [정적] |
| `gdi32full!pcfLocateCFONT` | `+0x11d` | `+0x122` | 반환값 기록용 껍데기 생성자 (`DC_ATTR+0x88` 를 넘김; 같은 주소 이름 2개, 후보: `ReturnValueTracer<int>` · `<long>` 생성자) → `+0x12c CMP 1 · +0x12f JG` | [정적] |
| 〃 | `+0x142` | `+0x149` | `NtGdiQueryFontAssocInfo(0)` (`0x10e1`) — 전역 `-1` 일 때 한 번 | [정적] |
| 〃 | `+0x355` | `+0x35c` | `NtGdiGetDCDpiScaleValue` (`0x12be`) — 표 실패 때 | [정적] |
| 〃 | `+0x40b` · `+0x4bb` | `+0x410` · `+0x4c0` | `pcfCreateCFONT` (`0x3d884`) · `GetCodePage` | [정적] |
| 〃 | **`+0x5ab`** | **`+0x5b2`** | **`NtGdiComputeXformCoefficients(hdc)`** (`0x1083`) — `[DC_ATTR+0x40]` 비트 0 이 0 일 때 | [정적] |
| `gdi32full!GetTextExtentPointWInternal` (`0x11b90`) | `+0x6d` | `+0x72` | **`GetDCDpiScaleValue(hdc)`** → `+0x72 CMP 1 · +0x75 JNZ → +0x20b` | [정적] |
| 〃 | `+0x7e` · `+0x195` · `+0x1c9` | `+0x83` · `+0x19a` · `+0x1ce` | `HANDLE_TO_INDEX` · `pcfLocateCFONT` · `bComputeTextExtent` (`0x1e2b4`) | [정적] |
| 〃 | `+0x247` · **`+0x281`** | `+0x24c` · **`+0x288`** | `LpkGetTextExtentExPoint` (`gbLpk ≠ 0`) · **`NtGdiGetTextExtent(…, 1)`** (`0x108c`) | [정적] |
| `gdi32full!GetTextExtentPointW` (`0x12ce0`) | `+0x69` | `+0x6e` | `GetDCDpiScaleValue` → `+0x6e CMP · +0x70 JNZ → +0x214` | [정적] |
| 〃 | `+0x192` · `+0x1b9` | `+0x197` · `+0x1be` | `pcfLocateCFONT` · `bComputeTextExtent` | [정적] |
| 〃 | `+0x242` · **`+0x262`** | `+0x247` · **`+0x269`** | `LpkGetTextExtentExPoint` · **`NtGdiGetTextExtent`** | [정적] |
| `gdi32full!GetDCDpiScaleValue` (`0x1c1d0`) | `+0x80` · `+0x107` | `+0x87` · `+0x10e` | `GdiGetEntry` (표) · `NtGdiGetDCDpiScaleValue` (표 실패 때) — 표가 맞으면 `+0xe0 [DC_ATTR+0x88]` | [정적] |
| `win32kfull!NtUserGetDCEx` (`0x23c180`) | `+0x28` · `+0x56` | `+0x2f` · `+0x5d` | `EnterSharedCrit` · `W32GetUserSessionState` → `+0x62 CMP RSI,[+0xace8]` | [정적] |
| 〃 | `+0x9f` | `+0xa4` | 지금 스레드의 스레드 정보 얻기 (같은 주소 이름 2개, 후보: `W32GetCurrentThread` · `PtiCurrent`) → `[+0x1f0]` 책상 | [정적] |
| 〃 | `+0xb3` · `+0xcc` · `+0xe7` | `+0xb8` · `+0xd3` · `+0xee` | `Feature_LearningModeExpansion…IsEnabled…` · `CanPerformUIOperation` · `ValidateHwnd` (기능 켜짐 갈래) | [정적] |
| 〃 | `+0x15f` · `+0x17a` | `+0x166` · `+0x181` | `HasUILimit` · `ValidateHwnd` (기능 꺼짐 갈래) | [정적] |
| 〃 | **`+0x13a`** | **`+0x141`** | **win32kbase `_GetDCEx(바탕 창, hrgn, flags)`** | [정적] |
| 〃 | `+0x198` | `+0x19d` | `UserSetLastError(0x57 / 5)` | [정적] |
| `win32kbase!_GetDCEx` (`0x67600`) | **`+0xa1c`** | **`+0xa21`** | **`CreateCacheDC(창, …, NULL)`** (`0x14370`) → `+0xa24 JZ → +0x2d5` … `+0x2f1` 0 | [정적] |
| 〃 | `+0x2b9` | `+0x2be` | `GreSetDCOwnerEx(hdc, 0x80000002, 0, 0)` | [정적] |
| `win32kbase!CreateCacheDC` | `+0x33` · **`+0x83`** | `+0x38` · **`+0x88`** | `Win32AllocPoolZInitImpl(0x100)` · **`GreCreateDisplayDC(hdev, 0, 2)`** → `+0x90 JZ → +0x2f6` | [정적] |
| `win32kbase!UserGetDesktopDC` (`0x3bbec`) | `+0x106` · `+0x129` · `+0x157` · `+0x196` · `+0x1d7` | `+0x10b` · `+0x12e` · `+0x15c` · `+0x19b` · `+0x1dc` | `_GetDCEx(…, 0x80800003)` · `CanPerformUIOperation` · `ValidateHwndEx` · `HasUILimit` · `ValidateHwndEx` | [정적] |
| `win32kbase!InitializeGre` (`0x2f93fc`) | **`+0xb8`** | **`+0xbd`** | **`InitializeDefaultDCAttributes`** (`0x1914e8`) — 세션 기본 틀 | [정적] |
| 〃 | `+0x4b8` | `+0x4bf` | `RtlQueryRegistryValues(…LanguagePack, 콜백 LpkShapeQueryRoutine)` — 앞 `+0x4ac` 블록 `+0x180094` = 0, 뒤 `+0x4cb` `+0x180098` = 1 | [정적] |
| `win32kbase!bInitBRUSHOBJ` | `+0x46` · `+0x278` | `+0x4b` · `+0x27d` | `bInitBrush(0, 0xffffff, …)` · `bSetStockObject(…, 7)` → `+0x286` 틀 펜 칸 | [정적] |
| `win32kbase!GreUpdateSharedDevCaps` (`0x14dd20`) | `+0x25` | `+0x2a` | `vGetDeviceCaps(PDEV, 블록)` (`0x14dd5c`) | [정적] |
| `win32kbase!InitVideo` (`0x1d0888`) | `+0x11a` · `+0x1ba` · `+0x1bf` | `+0x11f` · `+0x1bf` · `+0x1c4` | `DrvSetDisplayConfig` · `GreUpdateSharedDevCaps` · `InitUserScreen` | [정적] |
| `win32kbase!StubDispEnablePDEV` (`0x18c420`) | `+0x33` · `+0xaa` · `+0xdb` | `+0x38` · `+0xaf` · `+0xe0` | `bInitGdiInfo` (`0x1b6038`) · `memmove` · `EngCreatePalette` | [정적] |
| `win32kbase!GrepCreateCompatibleDC` (`0x14950`) | `+0x126` · `+0x256` | `+0x12b` · `+0x25b` | `GreCreateDisplayDC([원본+0x30], 1, 0)` · `GreCreateDisplayDC(주 화면, 1, 0)` | [정적] |
| 〃 | `+0x293` · `+0x2a4` | `+0x298` · `+0x2a9` | `dwSetLayout` · `vSetDpiScaling` | [정적] |
| `win32kbase!hdcOpenDCW` (`0x3a260`) | `+0xfa` · `+0x117` · `+0x278` · `+0x2ba` · `+0x426` · `+0x492` | `+0xff` · `+0x11c` · `+0x27d` · `+0x2bf` · `+0x42b` · `+0x497` | `DrvGetHDEV` · `UserGetMonitorDC` · `PDEVOBJ` · `GreCreateDisplayDC` · `DrvCreateMDEV` · `GreCreateDisplayDC` | [정적] |
| `user32!GetDC` (`0x4a8c0`) | `+0x9` `JMP` | — | `NtUserGetDC` — `+0x0 CMP 필터,5 · +0x7 JZ → +0x15` 가짜 HDC | [정적] |
| `gdi32!Gdi32DllInitialize` (`0x5ed0`) | `+0x4b` | `+0x52` | `GetProcessMitigationPolicy` → `+0x62` `g_systemCallFilterId` | [정적] |
| `user32!ClientThreadSetup` · `_UserClientDllInitialize` | `+0x45` · `+0x463` | `+0x4c` · `+0x46a` | `GetProcessMitigationPolicy` (값이 아직 `7` 일 때) | [정적] |
| `ntoskrnl!NtQueryInformationProcess` | `+0x1d3f` | `+0x1d44` | `PsGetWin32KFilterSet` → `+0x1d4b AND 0xf` · `+0x1d50` 출력 `[+4]` | [정적] |
| `gdiplus!InternalGdiplusStartup` (`0xe1380`) | `+0x62b` | `+0x630` | `InitImagingLibrary` (`0xe8e98`) → `+0x632 JZ → +0x7d1` (못 탐) | [정적] |
| 〃 | `+0x6d5` · `+0x6e1` · `+0x72a` | `+0x6da` · `+0x6e8` · `+0x731` | `SysColorNotify` (`0x92e0c`) · `GetModuleHandleW("gdi32.dll")` · `GetSystemDefaultLCID` | [정적] |
| 〃 | `+0x73d` · `+0x775` · `+0x78b` | `+0x742` · `+0x77a` · `+0x790` | `InitSystemFontsDirs` (`0xbe3c4`) → `+0x744 JZ` · `GetGpInstalledFontCollection` (`0x3465c`) → `+0x781 TEST` · `GpMallocEx(0x10)` → `+0x7a8 TEST` | [정적] |
| 〃 | **`+0x7b3`** | **`+0x7b8`** | **`BackgroundThreadStartup`** (`0xc58ac`) — `+0x7ad CMP [입력+0x10],0` · `+0x7ba JZ → +0x7d1` | [정적] |
| 〃 | `+0x7bc` | `+0x7c1` | `TraceLoggingRegisterEx_EventRegister_EventSetInformation` (반환 안 봄) | [정적] |
| `gdiplus!BackgroundThreadStartup` | **`+0x16` · `+0x36`** | **`+0x1d` · `+0x3d`** | **`CreateEventW(0,1,0,0)` ×2** → `+0x28 JZ → +0x8f` · `+0x48 JZ → +0xb4` | [정적] |
| `gdiplus!CreateThreadAndAddRefDll` (`0xc5978`) | `+0x27` · `+0x3c` · **`+0x85`** | `+0x2e` · `+0x43` · **`+0x8c`** | `GetProcessHeap` · `HeapAlloc(0x18)` · **`CreateThread(…, DllRefCountSafeThreadThunk, …)`** | [정적] |
| `gdiplus!AddRefModule` (`0xc5a44`) | `+0x39` · `+0x58` | `+0x40` · `+0x5f` | `GetModuleFileNameA` · `LoadLibraryExA(자기 경로)` | [정적] |
| `gdiplus!InternalNotificationStartup` (`0xa53d4`, 새 스레드) | `+0x83` · `+0x211` · `+0x291` · **`+0x2a7`** · `+0x2bf` · `+0x2d5` · `+0x2e4` | `+0x8a` · `+0x218` · `+0x298` · **`+0x2ae`** · `+0x2c6` · `+0x2dc` · `+0x2eb` | `RegisterClassW` · `CreateWindowExW` (→ `+0x227 JZ`) · `CreatePalette` · **`GetDC(숨은 창)`** · `SelectPalette` · `ReleaseDC` · `DeleteObject` | [정적] |

### 1. 글 폭 길 — `NtUserGetDCEx(0,0,3)` 의 DC 로 글자 폭을 재기까지 (물음 1) [정적]

| 차례 | 부르는 것 / syscall | 조건 | 0 / 실패면 | 읽는 · 쓰는 칸 |
|---|---|---|---|---|
| 0 | `SoftModalMessageBox +0xf5 NtUserGetDCEx(0,0,3)` | — | 한 번 더 (hwnd = `MSGBOXDATA+8`), 그래도 0 이면 `MessageBoxW` 0 (run142) | 3절 |
| 1 | `+0x126 SelectObject(hdc, 글꼴)` | run144 표 (DC · 글꼴 칸) | 글꼴 칸 없음 → 0 (오류 안 세움), DC 칸 없음 → `0x57` + 0. **반환은 저장만** | `DC_ATTR+0x128` 읽고 씀 (`SelectObjectImpl +0x3a3 · +0x3c5`), `+0x98` 에 `(& ~0x20) \| 0x10` (`+0x3b3 ~ +0x3bf`) |
| 2 | `UserGetCharDimensionsEx +0x72 GetTextFaceW` → `NtGdiGetTextFaceW` (`0x1078`) | 버퍼 · 개수 > 0 이면 syscall | 0 이면 버퍼가 0 그대로 → `'@'` 아님 → 일반 길 | 버퍼 첫 글자만 (`+0x7e CMP 0x40` — 세로 글꼴 표시 `[추측: 뜻]`) |
| 3 | `+0x92 GdiGetCharDimensions(hdc, 0, &cy)` | **표 검사** `+0x48 ~ +0xd7` (번호 · `GdiGetEntry` · 종류 `1` · 윗 16 · PID · `+0x10` 풀기) | **하나라도 실패 → `+0x19c XOR EAX,EAX` 로 0, `*cy` 안 씀, syscall 없음** | `DC_ATTR` 포인터. `semLocal` 임계 구역 (`+0xef ~`) |
| 4 | `+0x119 pcfLocateCFONT(hdc, DC_ATTR, …)` | 2절 가지 4a ~ 4h | **NULL → 사본 길 건너뜀 (조용히)** — 차례 5 가 syscall 로 | 2절 |
| 5 | `+0x12e bGetTextMetricsWInternal(hdc, 지역 0x3c B, 0x3c, 사본)` | 사본 ≠ NULL 이고 `[사본+0x1c] & 8` 이면 `+0x4c memcpy` (syscall 없음) | 아니면 **`+0x104 NtGdiGetTextMetricsW(hdc, 버퍼, 0x44)`** → `+0x114 JZ` → 0 이면 **폭 0 · `*cy` 안 씀** | 성공하면 지역 `TEXTMETRIC` 채움, 사본 있으면 `[사본+0x1c] \|= 8` · `+0x258 ~ +0x298` 저장 |
| 6 | `+0x14c TEST R12` → `+0x1f8` `*cy = tmHeight` | `cy ≠ NULL` | — | `TEXTMETRIC +0x0` |
| 7 | `+0x155 TEST [TEXTMETRIC+0x37],1` | **꺼져 있으면 `+0x16f` 폭 = `tmAveCharWidth` (`+0x14`) 로 끝** | — | `+0x37` 비트 0 = 가변 폭 `[추측: SDK 이름 tmPitchAndFamily]` |
| 8 | (가변 폭) 사본이 없거나 `[사본+0x1c] & 0x10` 꺼짐 → `+0x1c0 GetTextExtentPointWInternal("a…zA…Z" 52 자, 깃발 1)` | 켜져 있으면 `+0x167` 폭 = `[사본+0x254]` | `+0x1c7 JZ` → **폭 0** | 폭 = `(cx / 26 + 1) / 2` (`+0x1c9 ~ +0x1e5`), 사본에 `+0x1ec · +0x1f0` 저장 |
| 9 | `GetTextExtentPointWInternal` | `+0x2b` 개수 ≤ 0 (0 이면 `SIZE = 0` + 1) · **ASCII 검사** `+0x50 · +0x64 JNZ → +0x20b` · **`+0x6d GetDCDpiScaleValue == 1`** (`+0x75 JNZ → +0x20b`) · 표 검사 (`+0x7e ~ +0x119`, 실패 → `+0x8e 0x57` + 0) · `+0x11f gbLpk` 꺼짐 → `+0x195 pcfLocateCFONT` → `+0x1c9 bComputeTextExtent` | 사본 NULL (`+0x1a8`) · 계산 실패 (`+0x1d5`) → `+0x24e` 임계 구역 나옴 → **`+0x20b` 되돌이 → `+0x281 NtGdiGetTextExtent(hdc, 글, 개수, &SIZE, 1)`** | 사본 길은 2절과 같은 칸 + 사본 폭 표 |
| 10 | `SoftModalMessageBox +0x249` · `MB_GetMaxButtonSize +0x74` 의 `GetTextExtentPointW` (`0x12ce0`) | 9 와 **같은 차례 · 같은 조건** (`+0x2e` 개수 · ASCII `+0x4d ~ +0x60 JNZ → +0x214` · `+0x69 GetDCDpiScaleValue` · `+0x70 JNZ → +0x214` · `+0x192` · `+0x1b9`) | 되돌이 `+0x262 NtGdiGetTextExtent` | SoftModal 은 `[RBP-0x58]` (`cx`) 를 `+0x266` 에서 읽음 |
| 11 | `+0x2e0 DeleteObject(글꼴)` · `+0x2ef NtUserReleaseDC` | run144 | — | — |

- **`bComputeTextExtent`** (`0x1e2b4`, 사본 길로 잴 때) 가 읽는 칸: `DC_ATTR +0x11c · +0x118 · +0x120` (글 늘리기 — 처음 0 이면 영향 없음) · 사본 `+0x54 + 2*글자` (WORD 폭 표) · `+0x38` (float, `0.0625` 면 정수 반올림) · `+0x44` (고정 폭) · `+0x20` (→ `SIZE.cy`). 결과 `+0x179 SIZE.cx` · `+0x183 SIZE.cy`, 반환 1.
- **이 길에서 0 이 되는 곳은 셋**: 표 검사 (차례 3), `NtGdiGetTextMetricsW` = 0 (차례 5), 가변 폭일 때 `NtGdiGetTextExtent` = 0 (차례 8). 사본 길은 어디서 막혀도 0 을 안 만든다.

### 2. `pcfLocateCFONT` 안 — 사본을 포기하는 가지 (gdi32full `0x1bbd0`) [정적]

NULL 로 가는 길은 전부 **조용히** (`LastError` 안 세움) — 부른 쪽이 syscall 되돌이로 간다.

| 가지 | 줄 | 조건 → 결과 |
|---|---|---|
| 4a DPI 비율 | `+0x112 MOV EDX,[DC_ATTR+0x88]` · `+0x12c CMP EAX,1 · +0x12f JG → +0x389`. 표 실패면 `+0x355 NtGdiGetDCDpiScaleValue`. `hdc == 0` 이면 1 (`+0x4e2`) | **`> 1` 이면 NULL.** 0 · 1 은 통과 |
| 4b 글꼴 연결 | `+0x135 [전역 0x1801084ac]` · `+0x13b CMP -1` · `+0x142 NtGdiQueryFontAssocInfo(0)` · `+0x14e` 저장 · `+0x154 TEST · +0x156 JNZ → +0x389` | 반환 **≠ 0 이면 NULL**. 한 번만 부름 |
| 4c 포기 깃발 | `+0x15c TEST [DC_ATTR+0x98],0x20 · +0x16a JNZ → +0x389` | 켜져 있으면 NULL (글꼴을 다시 고르면 `SelectObjectImpl +0x3bc` 가 지움) |
| 4d 변환 계수 | `+0x170 TEST [+0x154],0xe000 · +0x17a JNZ → +0x5a8` · `+0x180 TEST [+0x40],1 · +0x184 JZ → +0x5a8` → `+0x5ab NtGdiComputeXformCoefficients` | **`+0x40` 비트 0 이 0 이면 syscall.** 0 을 받으면 `+0x5cd OR [+0x98],0x20` → NULL |
| 4e 한자권 코드 페이지 | `+0x18a CMP [guintDBCScp],-1` | `-1` 이면 건너뜀. 아니면 `+0x4bb GetCodePage(hdc)` 와 비교해 다르고 한자권 (`+0x366 ~ +0x37c` 비트표 `0x60011`) 이면 `+0x382 OR [+0x98],0x20` → NULL. `guintDBCScp` 는 `GdiProcessSetup` 이 ACP 가 `0x3a4 · 0x3b5 · 0x3b6 · 0x3a8` 일 때만 그 값 (`+0x132`), 아니면 `-1` (`+0x124`) — **타깃 VM ACP 가 949 (`0x3b5`) 면 이 길을 탄다** (VM 에서 `GetACP`) |
| 4f 글꼴 칸 | `+0x1a9 ~ +0x25e` — `DC_ATTR+0x128` 핸들을 표에서 다시 읽기 (종류 `+0xe == 0xa` `+0x223` · 윗 16 · PID · `+0x10`) | 하나라도 틀리면 `+0x3db OR [+0x98],0x20` → NULL |
| 4g 글꼴 attr | `+0x286 MOV RAX,[attr+8] · +0x28d JZ` · `+0x293 TEST [attr],1 · +0x296 JNZ` · `+0x29c` 목록 머리 | `[attr+8] = 0` 이거나 `[attr] & 1` 이면 NULL |
| 4h 목록 · 만들기 | `+0x2a2 JZ → +0x3e4` (비었음) → `+0x40b pcfCreateCFONT` — **`gpwcANSICharSet` ≠ 0 필요** (`pcfCreateCFONT +0x19 · +0x2d`) · 새 사본 `[+0x5c8]` ← 공유 `+0x180098` (`+0x44 ~ +0x5f`) · 폭 표 채우기 (`NtGdiGetWidthTable` `0x1063`) | 목록에서 찾는 조건: 소유자 `[사본+0x28] == hdc` 또는 (`== 0` 이고 DC 비트 1), `[사본+0x30] == [DC_ATTR+0x20]` (`+0x2bf`), `[사본+0x34] == [DC_ATTR+0x2c]`. 새 사본은 `+0x58d ~ +0x59c` 가 **`DC_ATTR+0x160` 목록 머리에 끼움** (유저가 씀). 참조 수는 부른 쪽이 돌려놓음 (`GdiGetCharDimensions +0x178` · `GetTextExtentPointW +0x1be DEC [+0x18]`) |

### 3. `NtUserGetDCEx(0,0,3)` 이 0 이 되는 길 (물음 1 끝) [정적]

`(hwnd 0, hrgn 0, flags 3)`. `3 = DCX_WINDOW | DCX_CACHE` 는 `[추측: 이름]`.

| 차례 | 줄 | 조건 | 결과 |
|---|---|---|---|
| 1 | `+0x3a TEST RSI · +0x3d JZ → +0x56` | `hrgn = 0` → `GreIsValidRegion` 건너뜀 | — |
| 2 | **`+0x62 CMP RSI,[세션+0xace8] · +0x69 JZ → +0x190`** | `hrgn` 이 세션의 특별 지역 핸들과 같음 (그 칸 뜻 `[공백]`) | `+0x190 MOV ECX,0x57` → `+0x198 UserSetLastError` → **NULL** |
| 3 | `+0x6f TEST EBP,0xffdaf800 · +0x75 JNZ → +0x190` | 모르는 깃발 (`3` 은 0) | 같이 `0x57` + NULL |
| 4 | `+0x7b TEST RBX · +0x7e JZ → +0x9f` → `+0xa4 MOV RCX,[pti+0x1f0]` | `hwnd = 0` → 지금 스레드의 바탕 창 | 책상이 없으면 널 검사 없이 읽음 → 접근 위반 `[추측]` |
| 5a | `+0xb3` 기능 검사 · `+0xc6 JZ → +0x15f` · `+0xcc CanPerformUIOperation` · `+0xda JNZ → +0x116` · `+0xe7 ValidateHwnd` | **기능 켜짐** | `ValidateHwnd` 가 0 이어도 **원격 측정 한 줄 (`+0x111`) 하고 계속** — NULL 아님 |
| 5b | `+0x15f HasUILimit(ppi, 1)` · `+0x16d JZ → +0x116` · `+0x17a ValidateHwnd` · `+0x189 JNZ` · `+0x18b LEA ECX,[RAX+5]` | **기능 꺼짐** + UI 한도 비트 `0x1` 이 걸린 프로세스 + `ValidateHwnd` 0 | `UserSetLastError(5)` → **NULL** |
| 6 | **`+0x13a _GetDCEx(바탕 창, hrgn, flags)`** · `+0x146` | 위를 다 지나면 | 반환 그대로 |
| 7 | `_GetDCEx`: 캐시 목록 `[세션+0xe338]+0x18` 훑기 (`+0x4c5 ~ +0x52a`) → 빈 칸 없으면 **`+0xa1c CreateCacheDC(창, …, NULL)`** · `+0xa24 JZ → +0x2d5` | 첫 부름은 거의 늘 | `CreateCacheDC` 0 → `+0x2f1 XOR EAX` → **0** |
| 8 | `CreateCacheDC`: `+0x33` 0x100 B 할당 (`+0x3e JZ → +0x2fe`) · **`+0x83 GreCreateDisplayDC(hdev, 0, 2)`** · `+0x90 JZ → +0x2f6` | 셋째 인자 0 이면 hdev = **`[세션+0xe338]+0x30` (주 화면 장치)** (`+0x72 JZ → +0x310 ~ +0x327`) | 장치 NULL → `GreCreateDisplayDC` 0 (run147 3절) → **NULL** |
| 9 | 성공 `CreateCacheDC +0x2b0 MOV RAX,[DCE+0x10]` · `_GetDCEx +0x402` 같은 칸 · `+0x2b9 GreSetDCOwnerEx` | `GreSetDCOwnerEx` 0 이면 `+0x2c6 BTR` · 정리 · **NULL** | HDC = `[DCE+0x10]` |

- **기능 상태**: `Feature_LearningModeExpansion__private_featureState` 파일 속 처음 값 `0x464d8` → 처음엔 **꺼짐** `[objdump]` (결과서). 실제 값은 런타임에 바뀔 수 있어 동적. 어느 쪽이든 **UI 한도가 없는 일반 프로세스는 5a · 5b 에서 안 막힌다.**
- `CanPerformUIOperation` / `HasUILimit` = 커널 `KfGetProcessEffectiveUILimits(프로세스)` 결과의 비트 `0x1` (결과서; 이름 job `UILIMIT` 는 `[추측]`).
- **`UserGetDesktopDC` 의 UI 한도** (run147 대조 6 이어서): 종류 `0` 이고 일반 프로세스면 `+0x106 _GetDCEx(바탕 창, 0, 0x80800003)` (책상 없으면 `+0xed TEST · +0xf0 JZ → +0x1e5` NULL). 기능 켜짐이면 `+0x129 CanPerformUIOperation` · `+0x157 ValidateHwndEx` 가 거짓이어도 원격 측정 (`+0x17e`) 만 하고 계속, 기능 꺼짐 (`+0x188`) 이면 `+0x196 HasUILimit` 참 · `+0x1d7 ValidateHwndEx` 0 일 때만 `+0x1e5` NULL. 종류 `2` (IC) 는 `+0x1a6 CMP · +0x1a8 JZ` 로 건너뜀.
- 그래서 **메시지 상자 12 샘플 · `CreateDCA` 5 샘플의 0 은 stub (기본 대답 0) 탓** — 진짜 길에선 일반 프로세스에 0 이 될 조건이 없다.

### 4. 세션 기본 `DC_ATTR` 틀 — 칸 표 (물음 2) [정적]

`G = [W32GetSessionState()+0x58]` (`InitializeGre +0x9a`), 틀 = `G+0x90` (0x1b0 B). **틀 칸 T = G 안 오프셋 − 0x90** (flow-writer 가 `InitializeDefaultDCAttributes` 쓰는 줄 28 개를 이 식으로 다시 맞춤 — 결과서와 일치). `DCMEMOBJ` 가 틀을 DC `+0x220` 으로 복사하고 `+0x1b8` 에서 `+0x98` 을 덮은 뒤, `RestoreAttributes` 가 앞 `0x160` B 를 유저 슬롯에 넣는다 (run147). 그래서 **유저 `DC_ATTR` 처음 값 = 아래 값** (+ `+0x00` 비트 1, `+0x98`).

| 칸 (T) | 처음 값 | 쓰는 줄 (`InitializeDefaultDCAttributes`, 따로 적은 것 빼고) | 읽는 쪽 · 뜻 `[추측: 이름]` |
|---|---|---|---|
| `+0x00` | 0. 비트 1 은 `GreCreateDisplayDC +0x243` (주 화면 장치) | — | run147 |
| `+0x20` · `+0x2c` (float) | **`16.0f`** | `+0xbc` · `+0xc9` | 사본 비교 (`pcfLocateCFONT +0x2bf ~ +0x2e3`). 28.4 고정소수 배율 `[추측]` |
| `+0x40` | **`0x4b`** (비트 0 켜짐) | `+0xdd` | `pcfLocateCFONT +0x180` — 비트 0 = 변환 계수 유효 `[추측]` |
| `+0x44 ~ +0x53` | `0.0625f` 들 | `+0xf4` (xmm) | 역배율 `[추측]` |
| `+0x64` · `+0x68` | `0x53` · `1` | `+0x12b` · `+0x14c` | `[공백: 뜻]` |
| **`+0x88`** | **`1`** | `+0x16b` | **DPI 비율** — `GetDCDpiScaleValue +0xe0` · `pcfLocateCFONT +0x112`. 뒤에 바꾸는 곳은 `SetCachedDpiScaleValue` 뿐 |
| `+0x98` | 틀 `0x10` → DC 사본 **`0x12001f`** | `+0x75` · `DCMEMOBJ +0x1b8` | run147 |
| `+0xa0` | **스톡 0 (WHITE_BRUSH) 핸들** | `bInitBrush +0xa0` (`bInitBRUSHOBJ +0x46` 이 부름) | 붓 |
| `+0xa8` | **스톡 7 (BLACK_PEN) 핸들** | `bInitBRUSHOBJ +0x286` (바로 앞 `+0x278 bSetStockObject(…, 7)`) | 펜 |
| `+0xb0 +0xb4 +0xc0 +0xc4` | `0x00ffffff` ×4 | `+0x40 · +0x46 · +0x4c · +0x52` | 색 `[추측]` |
| `+0xd0 · +0xd4 · +0xe0 · +0xe4 · +0xe8` | `1` · `0x101020d` · `2` · `1` · `1` | `+0x7f ~ +0xa1` | `[공백: 뜻]` |
| `+0xf0` | **`0`** | `+0xa8` | ICM 꺼짐 (run147) |
| `+0x100` · `+0x104` | `-1` · `-1` | `+0x5f` · `+0x65` | `[공백]` |
| `+0x108 · +0x114 · +0x118 · +0x11c · +0x120` | **`0`** (안 씀) | — | ICM 정보 · 언어팩 칸 · 글 늘리기 |
| **`+0x128`** | **스톡 13 (SYSTEM_FONT) 핸들** `[추측: 오프셋 일치]` | win32kfull `bInitStockFontsInternal +0x2f0 ~ +0x300` · `FinishStockFontInit +0x18d ~ +0x198` 가 `[G'+0x1b8]` 에 (`+0x1b8 − 0x90 = +0x128`) | 글꼴. 두 파일의 `G'` 가 같은 세션 전역인지는 포인터를 안 따라감 |
| `+0x13c +0x140 +0x14c +0x150` | `1` ×4 | `+0x153` · `+0x15a` | `[공백]` |
| `+0x154` | **`0x1802`** (`& 0xe000 = 0`) | `+0x161` | `pcfLocateCFONT +0x170` |
| `+0x160` · `+0x164 ~` · `+0x174 ~ +0x194` | `1` · `1.0f` 들 · `0` · `0x63` · `16.0f` ×2 · `0` | `+0xb5` · `+0x124` · `+0x10a ~ +0x146` | **유저에 복사되는 `0x160` B 밖.** 유저 슬롯 `+0x160` 은 0 으로 시작해 **gdi32full 이 글꼴 사본 목록 머리로 씀** (2절 4h) |

- **스톡 핸들** = 객체 핸들에 `0x800000` 켠 값 (`bSetStockObject +0x33 BTS 0x17`), `G+0xbc0` 배열의 `i` 번째 (`+0x47`), 글꼴 DPI 변종은 `G+0xbc8` (`+0x79 · +0x80`). 공유 메모리 `+0x1800b0 + 8*i` 와 같은 배열인지는 `[추측: 오프셋 · 번호 일치]`.
- **에뮬레이터는 이 값을 그대로 안 써도 된다** — 유저 쪽이 값 검사를 안 한다 (run147). 다만 `+0x88` 은 `GetTextExtentPointW*` 가 사본 길을 시도할지 정한다 (1 이면 시도 → NULL → 되돌이, 1 이 아니면 곧장 되돌이 — 어느 쪽도 syscall 로 끝남).

### 5. `GDIINFO` (`PDEV+0x848`) · `pGdiDevCaps` 블록 · 언어팩 칸 (물음 2) [정적]

- **블록 ← `GDIINFO`**: `GreUpdateSharedDevCaps(PDEV)` (`+0x1a ~ +0x1e` 블록 주소 = `[G+0x6e0]`) → `+0x25 vGetDeviceCaps`. 부르는 곳 `InitVideo +0x1ba` · `SetDPIinSetup` · `DrvChangeDisplaySettingsInternal` (결과서). `vGetDeviceCaps +0x1b` 가 `PDEV+0x848` 을 잡고 칸을 옮긴다:

| 블록 칸 (run147 지표 표) | ← `GDIINFO` 칸 · 식 | 줄 |
|---|---|---|
| `+0x00` · `+0x04` (TECHNOLOGY) | `+0x0` · `+0x4` | `+0x31` · `+0x35` |
| `+0x08` · `+0x0c` | `(+0x8 + 500) / 1000` · `(+0xc + 500) / 1000` | `+0x3e ~ +0x5c` |
| `+0x18` · `+0x1c` | `+0x10` · `+0x14` (해상도) | `+0x6b ~ +0x74` |
| `+0x20` (BITSPIXEL) | `+0x18` (15 면 16) | `+0x77 ~ +0x82` |
| `+0x24` · `+0x34` · `+0x4c` | `+0x1c` (판) · `+0x24` (래스터) · `+0x68` (팔레트 크기) | `+0x89` · `+0xb3` · `+0xe3` |
| `+0x44` · `+0x48` | `+0x28` · `+0x2c` (LOGPIXELS — 블록 길에선 안 씀, syscall) | `+0xd7 ~ +0xe0` |
| `+0x6c` · `+0x70` (DESKTOPHORZRES · VERTRES) | `+0x10` · `+0x14` | `+0x12d` · `+0x133` |
| `+0x8c` | 색 관리 능력 (기능 있을 때) 아니면 0 | `+0x15a` |

  - 나머지 칸 (NUMPENS · NUMFONTS · TEXTCAPS · VREFRESH · PHYSICAL* · SHADEBLENDCAPS 등) 은 결과서 (D1) 표 — 이 문서엔 줄이지 않았다.
- **`GDIINFO` 를 채우는 쪽**: 연결이 끊긴 세션의 가짜 화면 `StubDispEnablePDEV` → `+0x33 bInitGdiInfo` 가 상수로 (`TECHNOLOGY = 1` · 해상도 = DEVMODE (셋 다 0 이면 `0x400 × 0x300`) · `BITSPIXEL = 0x20` · `PLANES = 1` · LOGPIXELS = DEVMODE `dmLogPixels` · 60 Hz). DEVMODE 비트 수 ≠ 32 · 주파수 ≠ 60 이면 0 (`+0x58 ~ +0x72 → +0x171`). HORZSIZE · RASTERCAPS · 팔레트 크기 · PHYSICAL* 은 **안 채움 (0)**. `StubDisp` 가 "끊긴 세션용" 이란 뜻은 `[추측: 이름 · 힌트 함수 이름]`. **진짜 화면 드라이버**는 `PDEVOBJ` → `EnablePDEV` 가 드라이버를 **간접 호출** → 어느 드라이버가 이 VM 의 값을 채우는지 `[공백]` (VM 에서 `GetDeviceCaps` 표를 뜸).
- **언어팩 칸 `+0x180094`**: `InitializeGre +0x4ac` 가 0 으로 비우고 `+0x4b8 RtlQueryRegistryValues` 로 **`\Registry\Machine\System\CurrentControlSet\Control\LanguagePack` 아래 값마다** 콜백 `LpkShapeQueryRoutine` → `REG_DWORD` 면 `+0x32 OR [+0x180094], 1 << 값`. 키가 없거나 DWORD 가 없으면 0. **`+0x180098` = 1** (`+0x4cb`).
- **유저 쪽 뜻**: `GdiProcessSetup +0x161 CMP [공유+0x180094],0 · +0x168 JZ` → 0 이면 언어팩 초기화를 건너뛰어 `gbLpk = 0` → 글 폭 길에서 `DC_ATTR+0x114` · `LpkGetTextExtentExPoint` 를 **안 탄다** (run147 `+0x114` `[공백]` 닫힘).

### 6. 필터 값 5 갈래 (물음 3) [정적, kernelbase 줄 `[objdump]`]

- **출처** (위에서 아래로):
  - gdi32 `Gdi32DllInitialize +0x4b GetProcessMitigationPolicy(자기, 정책 0xb, &값, 4)` → `+0x62` 하위 4 비트를 `g_systemCallFilterId` (처음 0). user32 는 `ClientThreadSetup +0x14` · `_UserClientDllInitialize +0x42c` 가 값이 `7` (= 아직 안 알아봄) 일 때 `+0x45` · `+0x463` 에서 부르고 한 번만 저장 — 실패하면 `7` 그대로 (= 5 아님).
  - kernelbase `GetProcessMitigationPolicy` (rva `0x216f0`) 가 `NtQueryInformationProcess(자기, 0x34, {정책 0xb, 값}, 8)` 로 바꿔 부름 `[objdump]` (결과서 — `+0x47` call, 점프표 11 번).
  - ntoskrnl `NtQueryInformationProcess +0x1d3f PsGetWin32KFilterSet` → `+0x1d44 ~ +0x1d50` 하위 4 비트를 출력 `[+4]` 에 섞음 `[정적]`. `PsGetWin32KFilterSet` = `[EPROCESS+0x6e8]` (결과서).
  - `[EPROCESS+0x6e8]` ← **`PspAllocateProcess +0x1f0e MOV ECX,[속성+4] · +0x1f11 MOV [R14+0x6e8],ECX`** — 프로세스를 만들 때 부모가 준 "Win32k 필터" 속성 `[추측: 이름]`. **일반 프로세스는 0.**
- **5 일 때 달라지는 곳** (`[정적]`):

| 곳 | 줄 | 5 일 때 |
|---|---|---|
| user32 `GetDC` (`0x4a8c0`) | `+0x0 CMP · +0x7 JZ → +0x15` | 인자 무시, **`RAX = -0x2321b000` (`0xFFFFFFFFDCDE5000`)**, syscall 안 부름. 아니면 `+0x9 JMP NtUserGetDC` |
| user32 `ReleaseDC` (`0x4780`) | `+0xe CMP · +0x15 JZ → +0x43 · +0x4a JNZ` | 가짜 HDC 면 1, syscall 없음 |
| gdi32 `IsPseudoDesktopDcForFilteredProcess` (`0x1620`) | `+0x0 · +0x7 · +0xd CMP RCX,-0x2321b000` | 가짜 HDC 면 1 (`GdiReleaseDC` 가 씀) |
| gdi32 `GetDeviceCaps` (`0x17f0`) | `+0x47 CMP · +0x53 JZ → +0x419 · +0x420 JZ → +0x1e8 · +0x426 JMP → +0x59` | **가짜 HDC 만** 표 검사 없이 블록 길 (run147 5절) |
| gdi32 `D3DKMTOpenAdapterFromGdiDisplayName` (`0x2140`) | `+0x6 · +0x10 JZ → +0x83` | `0xC000001C` |
| user32 `_UserClientDllInitialize` | `+0x487 CMP · +0x48e JNZ` · `+0x494` 힙 `0x498` B · `+0x7de` → `+0x7e7 · +0x7ec CleanupWinpropManager` ×2 | 이 모드 전용 창 속성 · 메시지 처리 준비 · 정리 |
| user32 그 밖 25 곳 | `GetWindowData` · `GetMessageW/A` · `_PeekMessage` · `SendMessageWorker` · 훅 · `_SetWindowLongPtr` · `Set/Get/RemoveProp*` · `UnregisterClass*` · `RegisterIMEClass` (결과서 (E) 줄 자리) | 유저 쪽 대체 길 — 본문은 sast/151 몫 `[공백]` |

- **값이 0 이거나 `NtQueryInformationProcess` 가 실패해도 이 갈래는 하나도 안 탄다** — 에뮬레이터가 5 를 줄 이유가 없다.

### 7. N2 — gdiplus 시작 `+0x62b` 뒤 (물음 5) [정적]

| 차례 | 부르는 것 | 레지스트리 · 파일 | syscall | NULL 가지 |
|---|---|---|---|---|
| `+0x62b` | `InitImagingLibrary(깃발)` | 없음 | 없음 (임계 구역 초기화 = 유저 쪽) | **없음** — `+0x2d MOV EAX,1` 늘 1. `+0x632 JZ` 못 탐 |
| `+0x638 ~ +0x6cb` | `SystemColors` 16 칸 고정 색 | — | — | — |
| `+0x6d5` | `SysColorNotify` | — | **없음** — user32 `GetSysColor` ×4 = `[gpsi + 지표*4 + 0x11d8]` (`+0x9 · +0x13`) → **`gpsi` 가 0 이면 접근 위반** | 없음 |
| `+0x6e1 ~ +0x723` | `GetModuleHandleW("gdi32.dll")` · `GetProcAddress` ×2 (`ExtTextOutW` · `GdiIsMetaPrintDC`) | 이름만 | 없음 (이미 올라온 모듈) | 안 봄 |
| `+0x72a` | `GetSystemDefaultLCID` | 시스템 로케일 | `[공백: kernelbase 안 읽음]` | 안 봄 |
| `+0x73d` | `InitSystemFontsDirs` | **경로 이름만** — `+0x3c GetSystemDirectoryW` → 마지막 `\` 앞까지 + `\fonts` (`+0xed ~ +0x103`) → `FontsDirW`, `+0x108 · +0x154 WideCharToMultiByte` → `FontsDirA` | 없음 | `+0x744 JZ`: 시스템 폴더 길이 0 · 너무 김 · `\` 없음 (`+0x51 · +0x75`) · 할당 실패. **파일은 안 연다** |
| `+0x775` | `GetGpInstalledFontCollection` | **없음** — `GpInstalledFontCollection` 생성자 → `GpFontTable` 생성자 (`+0x25 HeapAlloc(0x10)`) — 빈 표만 (생성자 `+0x49 CMP [표+8],0 · +0x4e JNZ`, 0 이면 지우고 NULL) | 없음 | `+0x781` |
| `+0x78b` | `GpMallocEx(0x10)` → 글꼴 캐시 목록 | — | — | `+0x7a8` |
| **`+0x7b3`** | **`BackgroundThreadStartup`** — `+0x7ad CMP [입력+0x10],0` (`SuppressBackgroundThread` 가 0 일 때만 `[추측: 칸 이름]`) | `AddRefModule +0x39 GetModuleFileNameA` → `+0x58 LoadLibraryExA(그 경로)` (자기를 한 번 더 — 새 파일은 안 연다 `[추측]`). 경로 0 글자 · 너무 김 (`+0x45 ~ +0x4c`) 이면 실패 | **`CreateEventW(0,1,0,0)` ×2 (`+0x16` · `+0x36`) = `NtCreateEvent` ×2** · `HeapAlloc(0x18)` · **`CreateThread` (`CreateThreadAndAddRefDll +0x85`) = `NtCreateThreadEx`** | `+0x28 JZ → +0x8f` (첫 이벤트) · `+0x48 JZ → +0xb4` (둘째) · `+0x8d JNZ` 안 타면 (스레드 핸들 NULL) `+0x8f` → 0 → **`+0x7ba JZ → +0x7d1` 시작 실패** |
| `+0x7bc` | ETW 등록 | `[공백]` | 있음 `[공백]` | 반환 안 봄 |

- **새 스레드** (`DllRefCountSafeThreadThunk` → `BackgroundThreadProc` → `+0x25 InternalNotificationStartup`) 의 일 `[정적]`: `RegisterClassW` → `RegisterWindowMessageW` → **`+0x211 CreateWindowExW`** (NULL 이면 `+0x227 JZ → +0x35e` 정리하고 끝) → (UI 접근 프로세스면 `+0x3db SetWindowPos` TOPMOST) → **`+0x291 CreatePalette` → `+0x2a7 GetDC(숨은 창)` → `+0x2bf SelectPalette` → `+0x2d5 ReleaseDC` → `+0x2e4 DeleteObject`** (반환은 하나도 안 봄) → `+0x422 SetEvent` · `+0x431 CloseHandle` → `BackgroundThreadProc +0x74 MsgWaitForMultipleObjects` · `+0x99 PeekMessageW` … 고리에서 계속 기다림.
- **레지스트리 · 파일을 읽는 줄은 이 구간에 없다.** 글꼴 레지스트리 · 파일은 `GpInstalledFontCollection` 이 **처음 쓰일 때** 의 일 (이번 범위 밖).

### 8. win32k 몫 2 · 4 · 6 (물음 4) [정적]

- **`GrepCreateCompatibleDC`** (`0x14950`, `CreateCompatibleDC` 커널 쪽): 원본 있음 (`+0x28 [+0x68]` · `+0x38 JZ`) → **`+0x126 GreCreateDisplayDC([원본+0x30] = PDEV, 1, 0)`** (0 이면 `+0x131 JZ` NULL). 원본 `[+0x208] & 1` (DPI 비율 켜짐, `+0x14d ~ +0x157`) 이면 `+0x2a4 vSetDpiScaling` (`+0x6 OR [DC+0x208],5` · `+0x14 [DC+0x20c]`), 레이아웃 `[DC_ATTR+0x6c] & 7` (`+0x168 · +0x16c`) 이면 `+0x293 dwSetLayout`. 원본 NULL (`CreateCompatibleDC(0)`) 이면 `+0x227 · +0x22e` 주 화면 장치로 `+0x256 GreCreateDisplayDC(…, 1, 0)` — `HasUILimit` 결과는 안 씀 (결과서). 종류 1 이면 `GreCreateDisplayDC +0x14c CMP R14D,ESI (= 1, +0xbf)` · `+0x155 ~ +0x17e` 기본 비트맵 1 × 1 · `+0x187 bSetDefaultRegion`.
- **DPI 칸**:

| 칸 | 쓰는 곳 |
|---|---|
| `[W32PROCESS+0x10c]` (문맥) | `NtUserSetProcessDpiAwarenessContext` (`0x62350`) **`+0xc4`** — 앞 `+0x73 IsValidKernelDpiAwarenessContext` · `+0xaf OR · +0xb5 [+0x330]` ("한 번 정함" 비트, 이미 켜졌으면 `+0x1a7` 쪽 조건 `[정적: 결과서]`). 프로세스 만들 때 처음 값 `[공백]` |
| `[W32PROCESS+0x110]` (word, 시스템 DPI) | **`InitUserScreen +0x396`** (모든 프로세스 순회) · **`SetDPIinSetup +0x492`** (지금 프로세스) — 값 `[psi+0x1b56]` |
| `[스레드+0x190]` (문맥) | **`W32SetCurrentThreadDpiAwarenessContext` (`0x66a00`) `+0x6d`** · `+0x98` 유저 쪽 복사 칸 (`+0x198` 가 가리킴) |
| `DC_ATTR+0x88` (비율) | **`SetCachedDpiScaleValue` (`0x13140`) `+0xd4` (커널 사본) · `+0x97` (유저 슬롯)** — 부르는 쪽 `vUpdateCachedDPIScaleValue +0x1c`, "DPI 변환 없음" 이면 **1** (결과서) |

- **주 화면 장치 `[세션+0xe338]+0x30`**: **`InitVideo +0x179 · +0x180`** (`+0x11a DrvSetDisplayConfig` 가 준 목록 `+0x162 · +0x16a` 의 첫 장치) · `SetDPIinSetup +0x3de · +0x3e5` 가 다시 씀. 읽는 쪽: `GreCreateDisplayDC +0x231` · `_GetDCEx +0x5f` · `CreateCacheDC +0x310 ~ +0x327` · `GrepCreateCompatibleDC +0x22e` · `UserGetDesktopDC`.
- **`hdcOpenDCW`** (`0x3a260`, `NtGdiOpenDCW +0x171`, 이름 ≠ NULL): `+0xfa DrvGetHDEV(이름)` 로 화면 장치를 찾으면 `+0x117 UserGetMonitorDC`, 또는 `+0x18a` 둘째 `DrvGetHDEV` 뒤 `+0x2ba GreCreateDisplayDC`; 못 찾으면 `+0x278 PDEVOBJ` (프린터 · 그 밖 드라이버) → `+0x426 · +0x470 DrvCreateMDEV` → `+0x492 GreCreateDisplayDC` (실패 `+0x4ab DrvDestroyMDEV`). 하는 일 설명은 호출 이름에서 읽은 윤곽 `[추측]`, 줄은 `[정적]`. 프린터 길 뒷부분은 `[공백]`.

### 9. 그 밖 (물음 6) [정적, 표시 붙은 줄 `[objdump]`]

- **N5 `NtQueryInformationProcess(−1, 0x25)`**: kernelbase 에서 이걸 부르는 줄은 **`GetProcessVersion` (`0x23dc0`) `+0x3b` · `+0xd9`** `[objdump]`. kernelbase 안에서 `GetProcessVersion` 을 부르는 `call` 은 0 건 `[objdump]`, `GetVersionExW` (`0xc4ae0`) 는 `RtlGetVersion` + 선택 함수 표만 `[objdump]` → **gdiplus `InitVersionInfo` 에서 0x25 로 이어지는 줄은 못 찾음 `[공백]`** (run144 1절 차례 3 의 "kernelbase `GetVersionExA` 쪽 일 것" `[추측]` 은 확인 안 된 채로 남음 → 앞 run 고쳐 읽기).
- **표 칸 `+0xf` · `+0x0`**: 유저 쪽이 `+0xf` 를 읽는 곳은 하나 — **`SelectObjectImpl +0x398 TEST [항목+0xf],0x2`** (켜져 있으면 빠른 길을 건너뛰고 되돌이). `+0x0` 은 gdi32 · gdi32full 어디도 안 읽음 (항목 복사뿐) → 커널 객체 포인터 자리 `[추측]`.
- **`g_pEdgeGdi`**: `GetDeviceCaps +0x2b InitOnceExecuteOnce` (`+0x41 JNZ`) → API 세트 `ext-ms-win-gdi-edgegdi-l1-1-0` 가 있나 (`ApiSetQueryApiSetPresence`) → 있으면 `EdgeGdiInitialize` → `g_pEdgeGdi ≠ 0` 이면 GDI 호출이 Edge 쪽으로 (결과서). 이 빌드 `apisetschema.dll` 의 그 계약은 **호스트 이름이 빈 글** `[objdump]`, `edgegdi.dll` 은 System32 에 없음 → 보통 `g_pEdgeGdi == 0` `[추측: 빈 호스트 = 없음으로 취급]`.
- **`[GpDevice+0x628]`**: `GpDevice +0xa0` 이 0 으로 두고, `TECHNOLOGY == 1` 이고 `BITSPIXEL ≤ 8` 일 때만 `+0x1e5 PaletteChangeNotification` 뒤 `+0x1ea CMP [+0x628],0` 로 본다 (run147 대조 3). **32 비트 화면이면 안 읽는다.**

### 호출 관계

```
user32!SoftModalMessageBox (0x21f20)                                                      [정적]
 ├─ +0xfc   NtUserGetDCEx(0,0,3)  ── SYSCALL 0x108a     0 → 한 번 더 → 0 이면 MessageBoxW 0
 │           ⇒ win32kfull +0x62 [세션+0xace8] · +0x6f 깃발 → 0x57 · (기능 꺼짐 + UI 한도) +0x15f HasUILimit → 5
 │             +0x141 _GetDCEx   +0xa21 CreateCacheDC   +0x88 GreCreateDisplayDC([세션+0xe338]+0x30, 0, 2)
 │                                                       장치 없음 → 0 · 성공 → [DCE+0x10] = HDC
 ├─ +0x12d  SelectObject(hdc, 글꼴)        DC_ATTR+0x128 · +0x98 (& ~0x20 | 0x10)
 ├─ +0x14e  UserGetCharDimensionsEx       +0x79 GetTextFaceW ⇒ NtGdiGetTextFaceW ── SYSCALL 0x1078
 │           +0x99 gdi32full!GdiGetCharDimensions (0x4db20)
 │                  +0x48 ~ +0xd7 표 검사 실패 → +0x19c 0 (syscall 없음)            ← 표 칸 없으면 여기서 끝
 │                  +0x11e pcfLocateCFONT   4a +0x88 > 1 · 4b +0x149 NtGdiQueryFontAssocInfo ≠ 0 · 4c +0x98&0x20
 │                                          4d +0x5b2 NtGdiComputeXformCoefficients = 0 · 4e 한자권 · 4f 글꼴 칸
 │                                          4g attr · 4h 목록 (없으면 +0x410 pcfCreateCFONT)    → NULL (조용히)
 │                  +0x133 bGetTextMetricsWInternal   사본 메트릭 있으면 +0x51 memcpy
 │                                                    없으면 +0x10b NtGdiGetTextMetricsW(hdc, 버퍼, 0x44) ── SYSCALL 0x106e
 │                         0 → 폭 0 → +0x153 JZ → MessageBoxW 0
 │                  +0x155 [TEXTMETRIC+0x37] 비트 0 꺼짐 → 폭 = tmAveCharWidth
 │                         켜짐 → +0x1c5 GetTextExtentPointWInternal(52 자) → 폭 = (cx/26+1)/2
 ├─ +0x174  MB_GetMaxButtonSize  +0x7b GetTextExtentPointW
 ├─ +0x250  GetTextExtentPointW(제목)
 │           (둘 다) +0x6e GetDCDpiScaleValue ≠ 1 → +0x214 ─┐
 │                   표 · gbLpk 0 · +0x197 pcfLocateCFONT · +0x1be bComputeTextExtent ─ NULL / 실패 ─┤
 │                                                            +0x269 NtGdiGetTextExtent ── SYSCALL 0x108c
 └─ +0x2e7 DeleteObject · +0x2f6 NtUserReleaseDC ── SYSCALL 0x151c

gdiplus!InternalGdiplusStartup (0xe1380)  (run147 의 +0x630 뒤)
 ├─ +0x630  InitImagingLibrary   늘 1
 ├─ +0x6da  SysColorNotify       GetSysColor ×4 (gpsi 읽기)
 ├─ +0x742  InitSystemFontsDirs  GetSystemDirectoryW + "\fonts"   (파일 · 레지스트리 없음)
 ├─ +0x77a  GetGpInstalledFontCollection   빈 표
 └─ +0x7b8  BackgroundThreadStartup   +0x1d · +0x3d CreateEventW ×2 · AddRefModule (LoadLibraryExA 자기)
                                       CreateThreadAndAddRefDll +0x8c CreateThread     하나라도 NULL → +0x7d1 → 실패
             ⇢ 새 스레드 BackgroundThreadProc +0x2a InternalNotificationStartup
                  +0x218 CreateWindowExW · +0x298 CreatePalette · +0x2ae GetDC · +0x2c6 SelectPalette … (반환 안 봄)
                  +0x7b MsgWaitForMultipleObjects 고리
```

- (오프셋은 ret 자리, 검사 줄은 함수 안 줄. `⇒` 는 지연 가져오기 · syscall 너머, `⇢` 는 새 스레드. 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` 메시지 상자의 글 폭은 `GdiGetCharDimensions` 가 잰다. 표 검사가 실패하면 syscall 없이 0, 표가 맞으면 글꼴 사본 길이 막혀도 `NtGdiGetTextMetricsW` 되돌이가 있다. 0 을 만드는 곳은 표 검사 · `NtGdiGetTextMetricsW` = 0 · (가변 폭일 때) `NtGdiGetTextExtent` = 0 셋뿐.
- `[정적]` `pcfLocateCFONT` 의 NULL 가지 여덟 (4a ~ 4h) 은 모두 `LastError` 를 안 세우고, 몇 개 (4d · 4e · 4f) 는 `DC_ATTR+0x98` 에 `0x20` 을 켜 글꼴을 다시 고를 때까지 사본 길을 막는다.
- `[정적]` `GetTextExtentPointW` · `GetTextExtentPointWInternal` 은 표 검사 앞에서 `GetDCDpiScaleValue(hdc) == 1` 을 보고, 아니면 곧장 `NtGdiGetTextExtent` 로 간다 (`LastError` 안 세움).
- `[정적]` `NtUserGetDCEx(0,0,3)` 은 일반 프로세스 · 주 화면 장치 있음이면 0 이 될 길이 없다 (0 이 되는 넷: 특별 지역 · 모르는 깃발 · UI 한도 + 기능 꺼짐 + 바탕 창 무효 · `CreateCacheDC` / `GreSetDCOwnerEx` 실패).
- `[정적]` 세션 기본 `DC_ATTR` 틀은 `InitializeGre +0xb8 InitializeDefaultDCAttributes` 가 채우고, 붓 · 펜 칸은 스톡 0 · 7 (`bInitBrush` · `bInitBRUSHOBJ`), 글꼴 칸은 win32kfull 스톡 글꼴 초기화가 쓴다 (스톡 13 은 `[추측: 오프셋 일치]`).
- `[정적]` `pGdiDevCaps` 블록은 `vGetDeviceCaps` 가 `GDIINFO` 에서 옮겨 적고, `+0x180094` 는 레지스트리 `LanguagePack` 의 DWORD 값 비트, `+0x180098` 은 1.
- `[정적]` + `[objdump]` `g_systemCallFilterId` 는 `EPROCESS+0x6e8` 의 하위 4 비트이고 그 칸은 `PspAllocateProcess +0x1f11` 이 프로세스 생성 속성에서 쓴다. 0 이면 5 갈래는 하나도 안 탄다.
- `[정적]` gdiplus `+0x62b ~ +0x775` 에는 NULL 을 만드는 바깥 답이 없다 (`InitImagingLibrary` 늘 1, 나머지는 문자열 · 할당). `+0x7b3` 은 이벤트 둘 · 스레드 하나가 0 이 아니어야 한다.
- `[관찰]` test.exe 초기화 끝 모듈 6개에 user32 · gdi32 · gdiplus 가 없고 PEB `GdiSharedHandleTable` 은 (null) (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, `_P1a.txt:1779` · `_P2.txt:2489`) — 이 길은 test.exe 에선 안 생긴다.

## flow-writer 대조

결과서 오프셋을 `.asm` 과 맞춰 봤다 (위 "다시 본 줄"). **결론을 바꾸는 어긋남은 없다.** 점프 목적지 주소 끝자리와 함수 기준 `+0x…` 가 섞인 곳도 없다 (`+0x107 → +0x636` · `+0x153 → +0x69f` · `+0x420 → +0x1e8` · `+0x632 → +0x7d1` · `+0x69 → +0x190` · `+0xa24 → +0x2d5` · `+0x3e → +0x2fe` · `+0x72 → +0x310` 등 계산해 봄). 줄 표기만 고친 곳 일곱 (이 문서는 고친 값으로 씀):

1. **`pcfLocateCFONT +0x352 NtGdiGetDCDpiScaleValue` (ret `+0x35c`)** (결과서 (4a)) → `+0x352` 는 `MOV RCX,R12` (인자), **call 은 `+0x355`** (ret `+0x35c` 맞음).
2. **(4h) "`+0x178 DEC` 로 돌려놓음"** → `pcfLocateCFONT.asm` 엔 `+0x178` 줄이 없다. 그 줄은 **`GdiGetCharDimensions +0x178 DEC [RDI+0x18]`** (부른 쪽). `GetTextExtentPointW` 는 `+0x1be DEC [R13+0x18]`.
3. **(A) 차례 10 `GetTextExtentPointW` "`+0x40 JNZ → +0x214`"** → `GetTextExtentPointW.asm` 엔 `+0x40` 줄이 없다. ASCII 검사는 **`+0x4d TEST [글],0xff80 · +0x52 JNZ` (고리) → `+0x5e CMP ECX,ESI · +0x60 JNZ → +0x214`** (`GetTextExtentPointWInternal` 쪽 `+0x50 · +0x64` 는 결과서대로 맞음). 앞에 `+0x3b TEST R8D · +0x3e JZ → +0x214` 한 줄도 있다.
4. **(4e) "`GdiProcessSetup +0x124` 가 일본 · 한국 · 중국 코드 페이지일 때만 값을 씀"** → `+0x124` 는 **한자권이 아닐 때 `-1` 을 쓰는 줄** (`OR [guintDBCScp],-1`). 코드 페이지 값을 쓰는 줄은 **`+0x132 MOV [guintDBCScp],EDX`** (앞 `+0x12d vSetCheckDBCSTrailByte`, 뒤 `+0x138 guintAcp = 0x4e4`). 판정 줄은 `+0xf7 GetACP` · `+0x10b ~ +0x122`. 뜻 (한자권일 때만 값) 은 같다.
5. **(B) 차례 3 "`+0x190 MOV ECX,0x57` → `UserSetLastError` (ret `+0x19d`)"** → call 은 **`+0x198`** (ret `+0x19d` 맞음, `+0x195 MOV RBX,R15` 사이).
6. **(F) `+0x775` 칸 "`GpFontTable` 생성자 … `+0x49 CMP · +0x4e JNZ`"** → `+0x49` · `+0x4e` 는 **`GpInstalledFontCollection` 생성자 (`0xc3848`) 의 줄** (`GpFontTable.asm` 엔 `+0x49` 없음, 그쪽은 `+0x25 HeapAlloc`). 괄호가 어느 함수에 붙는지만 흐렸다.
7. **(G1) "`GreCreateDisplayDC +0x14c CMP R14D,1`"** → 실제 줄은 `CMP R14D,ESI` (`ESI` 는 `+0xbf MOV ESI,0x1`). (G2) "`+0xb5 [RDI+0x330] |= 0x400000`" → `+0xb5` 는 `MOV [RDI+0x330],RAX` 이고 OR 은 `+0xaf OR RAX,RCX` (비트 값은 결과서 읽음). 뜻 같음.

- 그 밖 결과서 오프셋 (한 줄 답 1 ~ 6 · 표 (A) (4a ~ 4i) (B) (C) (D1) (D3) (E) (F) (F2) (G1 ~ G4) 의 `+0x…` 거의 전부) 과 syscall 번호 열하나 · PDB GUID 여섯은 **일치**. `[objdump]` 줄 (kernelbase `GetProcessMitigationPolicy +0x47` · `GetProcessVersion +0x3b / +0xd9` · `GetVersionExW` · `apisetschema` · 기능 상태 파일 값) 은 flow-writer 가 다시 안 뽑았다 — 표시 그대로.
- `_calls.tsv` 에 `alias:` 로 나온 것 중 이름이 둘 이상 갈리는 건 둘 — `NtUserGetDCEx +0x9f` (같은 주소 이름 2개, 후보: `W32GetCurrentThread` · `PtiCurrent`) · `pcfLocateCFONT +0x11d` / `GetDCDpiScaleValue +0xeb` (같은 주소 이름 2개, 후보: `ReturnValueTracer<int>` · `ReturnValueTracer<long>` 생성자). 나머지 `alias:…+1` 은 같은 이름의 C++ 꾸민 이름 하나라 그대로 썼다.

## 앞 run 고쳐 읽기

틀린 줄이 아니라 길이 좁혀지거나 `[공백]` 이 닫힌 것이라 "정정" 이 아니라 "고쳐 읽기" 로 맨 위에 한 줄씩 넣었다 (원문은 그대로).

- **run144** (문서 · 해설서): ① 3절 "누가 칸의 무엇을 보나" · 6절 표 · 7절 "`GetTextExtentPointW(DC)` → 0 + `0x57`" — 그 `0x57` 줄 (`GetTextExtentPointW +0x89 · +0x8e`) 에 닿으려면 **앞에서 `+0x69 GetDCDpiScaleValue(hdc) == 1`** 을 지나야 한다. 표가 없으면 `GetDCDpiScaleValue` 가 `NtGdiGetDCDpiScaleValue` (`0x12be`) syscall 을 내고, 1 이 아니면 `+0x70 JNZ → +0x214` 로 곧장 `NtGdiGetTextExtent` (`0x108c`) — `LastError` 안 세움. 그러니 "syscall 없이 실패" 에서 `GetTextExtentPointW` 는 빠진다 (`GetTextMetricsW` · `GdiGetCharDimensions` 는 그대로). ② 1절 차례 3 "`NtQueryInformationProcess(0x25)` 는 kernelbase `GetVersionExA` 쪽일 것" `[추측]` → kernelbase 안 0x25 호출 줄은 `GetProcessVersion` 뿐이고 그걸 부르는 kernelbase 줄이 없다 — 출처 `[공백]` 그대로. ③ 다음 요청 후보 N2 (벽 아님) · N3 (글 폭) · N4 (win32k 표 일부) 닫힘, N5 반만.
- **run147** (문서 · 해설서): ① 4절 칸 표 `+0x114` "N3 몫 `[공백]`" → 닫힘: `gbLpk ≠ 0` 일 때만 읽고, `gbLpk` 는 공유 `+0x180094` (레지스트리 `LanguagePack` DWORD 값) 가 0 이 아닐 때만 켜짐. ② `+0x160` "0" → 0 으로 시작하지만 **gdi32full `pcfLocateCFONT +0x58d ~ +0x59c` 가 글꼴 사본 목록 머리로 씀**. ③ 요약 · 4절 · 목록 10 "`+0x98` = `0x12001f`" → 세션 틀 값은 `0x10`, `0x12001f` 는 DC 를 만들 때 덮는 값 (유저가 보는 처음 값은 `0x12001f` 그대로). ④ "세션 기본 틀 내용 `[공백]` · 어느 스톡 핸들인지 `[공백]`" → 닫힘: 붓 스톡 0 · 펜 스톡 7 · 글꼴 스톡 13 (`+0x88 = 1` · `+0x40 = 0x4b` 등, 이 문서 4절). ⑤ "win32k 몫 남음" 1 ~ 6 · 대조 6 의 "`+0x51 JZ` (기능 깃발 꺼짐) 갈래는 안 읽음" → 닫힘 (이 문서 3 · 5 · 8절).

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (user32 · gdi32 · gdiplus 안 올림) 이고 PEB `+0xf8` 은 test.exe 사진에서 (null) 이다. 커널 쪽 `EPROCESS+0x6e8` (Win32k 필터) 은 test.exe 도 가진 칸이지만 이번에 사진 값을 읽지 않았다 — 짐 목록에 이 칸이 있는지 · 값이 0 인지는 확인 안 함 `[공백]` (다음 바퀴 후보에 한 줄).

## 아직 확인 필요

동적 — 디버거 필요 (결과서 "동적으로 확인할 것" 1 ~ 7, 대기열 **Q277** 같은 VM GUI 세션에 얹을 꼴. test.exe 가 아니라 MessageBox · gdiplus 를 부르는 GUI 프로세스, 그 프로세스 조건):

1. 글 폭 사본 길: `gdi32full!GdiGetCharDimensions +0xd7` (`rbx` = `DC_ATTR` → `db @rbx L0x170`) · `+0x11e` (`[rax]` 사본 NULL?) · `+0x133` (`eax`) · `+0x1c5` · `+0x17b` (`esi` 폭). `pcfLocateCFONT +0x12f` · `+0x156` · `+0x184` · `+0x2a2` · `+0x413` · `+0x3db` 에 닿는지.
2. 그 DC 의 `DC_ATTR` 0x170 B — 4절 표와 같은가, 스톡 핸들이 `GetStockObject(0 · 7 · 13)` 과 같은가, 글꼴 항목 표 칸 `+0xf` · `+0x10`.
3. win32k: `win32kfull!NtUserGetDCEx +0x62` (`[rax+0xace8]`) · `+0xb8` · `+0xd3` · `win32kbase!_GetDCEx +0xa21` · `GreCreateDisplayDC +0x20a` · `+0x243` · `DCMEMOBJ +0x1b8` · 세션 틀 `[W32GetSessionState()+0x58]+0x90` 0x1b0 B · `[G+0x6e0]` 0x98 B · `[PDEV+0x848]` 0x140 B.
4. 언어팩: `InitializeGre +0x4bf` 직후 `[rcx+0x180094]` · `LpkShapeQueryRoutine +0x32` 횟수.
5. 필터: `ntoskrnl!PspAllocateProcess +0x1f11` (`ecx` — test.exe · 일반 GUI exe 둘 다 0?) · `kernelbase!GetProcessMitigationPolicy +0xcd` `[objdump 자리]`.
6. N5: `kernelbase!GetProcessVersion +0x3b` 콜스택 — gdiplus 시작 길에서 닿는지.
7. 스레드: `gdiplus!CreateThreadAndAddRefDll +0x8c` (`rax`) · `InternalNotificationStartup +0x218` · `+0x2ae` · `+0x2c6`.

디버거 없이 떠 올 것 (결과서 1 ~ 6 — 380 **2 단계 VM 수집 스크립트** 재료):

1. `GetDeviceCaps(화면 HDC, 0 ~ 121)` 전부 — `GDIINFO` 를 `StubDisp` 가 채웠는지 진짜 드라이버인지 가늠 (`HORZSIZE` · `RASTERCAPS` · `PHYSICAL*` 이 0 이고 1024 × 768 이면 StubDisp 쪽).
2. 메시지 상자 글꼴과 폭: `SystemParametersInfoW(SPI_GETNONCLIENTMETRICS)` 의 `lfMessageFont` → `CreateFontIndirectW` → `SelectObject` → `GetTextMetricsW` (`tmHeight` · `tmAveCharWidth` · `tmMaxCharWidth` · `tmPitchAndFamily` · `tmCharSet`) · `GetTextFaceW` · `GdiGetCharDimensions` · `GetTextExtentPoint32W` (52 자 · `"OK"` · `"확인"`) · `GetDpiForSystem` — **에뮬레이터 `NtGdiGetTextMetricsW` · `NtGdiGetTextExtent` 대답 재료.**
3. 글꼴 레지스트리 · 파일: `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts` · `FontSubstitutes` · `FontLink\SystemLink` · `HKCU\Control Panel\Desktop\WindowMetrics` · `HKCU\Control Panel\Desktop` (`LogPixels` · `Win8DpiScaling`) · `C:\Windows\Fonts` 목록.
4. 언어팩 · 로케일: `HKLM\SYSTEM\CurrentControlSet\Control\LanguagePack` 값 전부 · `GetSystemDefaultLCID` · **`GetACP`** (949 면 2절 4e 길) · `GetSystemDirectoryW`.
5. DPI · 필터: `GetProcessDpiAwareness` · `GetDpiForSystem` · `GetThreadDpiAwarenessContext` · `GetProcessMitigationPolicy(ProcessSystemCallFilterPolicy)` (test.exe · 일반 GUI exe 비교) · `ApiSetQueryApiSetPresence("ext-ms-win-gdi-edgegdi-l1-1-0")`.
6. 스톡 · 시스템 색: `GetStockObject(0 · 7 · 13)` · `GetCurrentObject(새 DC, …)` 비교 · `GetSysColor(1 · 15 · 16 · 20)` · `GetVersionExA`.

정적 (결과서 "다음 요청 후보" — 대기열 자리는 오케스트레이터가 정함):

1. N5 마무리 — kernelbase `GetVersionExW` 의 선택 함수 셋 · `GetProcessVersion` 을 부르는 kernelbase 밖 줄 (kernel32 등).
2. 글꼴 사본 "진짜 꼴" (`pcfCreateCFONT` 칸 · `NtGdiGetWidthTable` 인자 · win32kfull `NtGdiGetTextMetricsW` · `NtGdiGetTextExtent(ExW)` · `NtGdiHfontCreate`) — 에뮬레이터가 사본 길을 정말 필요로 할 때만.
3. 글꼴 열거 (gdiplus `GpInstalledFontCollection` 첫 사용 — 레지스트리 · 파일; 수집 3 번과 짝) — 다음 gdiplus 벽 후보.
4. `PspAllocateProcess` Win32k 필터 속성 (누가 넣나) · 필터 5 의 syscall 표 (ntoskrnl · win32kfull, sast/153 뒤).
5. `[세션+0xace8]` 뜻 · `UserGetMonitorDC` · `DrvGetHDEV` · `GreGetDeviceCapsInternal +0x1a9 ~ +0x1e4` (DPI 100% 아닐 때).
6. sast/151 (user32 · win32k) 로: user32 `g_systemCallFilterId == 5` 25 곳 본문 · `SoftModalMessageBox` DPI 쪽.
7. (flow-writer) test.exe 의 `EPROCESS+0x6e8` 값 — 사진 (`ms/logs/04_baggage-read_P1a.txt` 류) 에 그 칸이 찍혀 있는지 짐 지도 바퀴 때 한 번 보기.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- **스크립트 쓰는 법 메모 (결과서 — 오류 아님, 제안만)**: `run_ghidra.sh` export 의 정규식 자리에 `rva:` 를 `|` 로 섞으면 `rva:` 쪽이 **조용히 무시된다** (`'^(A|B)$|rva:0x1094c0'` → 이름 일곱 개만, 로그 한 줄로만 알 수 있음). 따로 두 번 불러야 한다. → **ghidra-headless-export `reference.md` · `SKILL.md` 에 "`rva:` 는 단독으로만" 한 줄 추가 제안** (flow-writer 는 `.claude/` 를 안 고침).
- 겹친 이름은 이번에도 `__<rva>` 로 정상 (`bInit__1094c0` · `bInit__1af630`; 기존 `GetTextMetricsW.asm` ≠ `GetTextMetricsW__4df30.asm` — 뒤는 이번 길과 무관).
- 같은 때 explainer 둘이 run151 · run152 해설서를, decompiler 가 sast/151 ~ 154 를 쓰고 있었다 — 이 문서는 그 파일을 안 건드렸다.

## run152 대조 (run153 쓰는 중 — 오케스트레이터 요청, explainer 가 찾은 것 둘)

run152 문서 맨 위에 "보탬 · 표기 (run153 때 대조)" 한 줄 + 본문 두 곳에 한 마디 (해설서는 다른 explainer 가 쓰는 중이라 안 건드림).

1. **F2 실패 값 `0xc000000d` 빠짐** → 맞다, 보탰다. 64비트 ntdll `CsrClientConnectToServer` (`0xdb7a0`) `+0x2f TEST R8 · +0x32 JZ` (블록 없음이면 건너뜀) · **`+0x34 TEST R9D · +0x37 JZ → +0x1d5 MOV EAX,0xc000000d`** → `+0x1da JMP → +0xca` (연결 없이 돌아감) `[정적]` — 결과서 147 3-3 "블록 있는데 크기 0" 과 같은 줄. kernelbase32 는 크기 8 을 주므로 실제로는 안 탄다.
2. **PEB32 세 칸 줄 범위** → 둘 다 맞다. `whNtWow64CsrClientConnectToServer` 의 `+0x4e` · `+0x66` · `+0x7e` 는 `MOV RAX,GS:[0x60]` (PEB64 주소 얻기), `+0x57` · `+0x6f` · `+0x87` 은 PEB64 칸 (`+0x88` · `+0x98` · `+0x380`) 읽기, `+0x63` · `+0x7b` · `+0x8e` 가 PEB32 쓰기. 결과서는 **PEB64 주소 얻기부터**, run152 는 **칸 읽기부터** 센 것 — 쓰는 자리는 같다. (그 앞 `+0x4b MOV EDX,[RAX+0x30]` 이 PEB32 주소.)

## PE_emulator 쪽 — 메시지 상자 12 샘플을 푸는 최소 (결과서 "PE_emulator 쪽 — 지금 맞나 · 틀리나")

결과서는 `../PE_emulator*` 를 **이번엔 열지 않았다** (run147 때 읽은 파일 상태 기준의 판정). flow-writer 도 안 열었다. 고칠지는 사용자 · PE_emulator 결정. 아래 "샘플" 숫자는 요청 쪽 에뮬레이터 숫자다 (`[관찰]` 아님).

### 판정

| 대상 | 맞나 · 틀리나 | 이유 |
|---|---|---|
| 메시지 상자 12 샘플 (`NtUserGetDCEx(0,0,3)` 기본 대답 0 ×2 → `MessageBoxW` 0) | **stub 탓** | 진짜 길에선 일반 프로세스 · 화면 있음이면 0 이 될 조건이 없다 (3절). **고칠 것 = HDC 를 돌려주기 + 아래 글 폭 대답** |
| run147 목록 13 `NtGdiGetDeviceCaps` 최소값 | **맞음** | `StubDisp` 상수 (1024 × 768 · 32 비트) 와 같은 꼴 — 값이 1920 × 1080 이어도 유저 쪽은 검사 안 함 |
| run147 목록 10 `DC_ATTR` 블록 | **더하기** | 진짜 틀 값 (4절) 이 있다. 다만 사본 길을 안 쓸 거면 **`+0x40` 을 0 으로 두는 편이 단순** (4d 에서 바로 되돌이). `+0x88` 은 0 이든 1 이든 끝은 syscall |
| `g_systemCallFilterId` (run147 목록 16) | **5 를 줄 이유 없음** | 출처가 `EPROCESS+0x6e8` (부모가 준 필터 속성) 이라 일반 프로세스는 0. `NtQueryInformationProcess(…, 0x34, …)` 가 "성공 + 0" 이든 실패든 5 는 안 나옴. 5 를 주면 `GetDC` 가 가짜 HDC `0xFFFFFFFFDCDE5000` 을 돌려주고 25 곳이 다른 길로 감 |
| gdiplus `+0x62b ~ +0x775` (run147 "그 뒤 벽") | **벽 아님** | `InitImagingLibrary` 늘 1, 나머지는 문자열 · 할당. 단 `SysColorNotify` 는 `gpsi` 가 있어야 안 죽음 (run85 csrss 답) |

### 메시지 상자 12 샘플을 푸는 최소 — 순서대로

전제: run147 목록 8 ~ 10 (DC 핸들 · 표 칸 · `DC_ATTR`) 이 있다. 이 셋이 없으면 아래를 다 줘도 `GdiGetCharDimensions` 가 표 검사에서 syscall 없이 0 을 돌려준다 (1절 차례 3).

1. **HDC 종류 비트** — `NtUserGetDCEx(0, 0, 3)` → run147 목록 8 꼴 HDC (**`번호 | 0x10000`**, 스톡 비트 0, 고유 0) + 표 칸 (`+0xe == 1` · `+0xc == H >> 16` · `+0x8 == PID & ~3` · `+0x10` = 가린 `DC_ATTR` 주소). `0x21000` 꼴 (종류 비트 2) 은 안 된다 (run147). 두 번 불려도 같은 HDC 면 됨. `NtUserReleaseDC` → 1.
2. **`NtGdiGetTextMetricsW(hdc, 버퍼, 0x44)` → 0 아님 + 버퍼 앞 `0x3c` B 채우기**: `+0x00` (높이) ≥ 1 · `+0x14` (평균 글자 폭) ≥ 1. 0 을 주면 `MessageBoxW` = 0.
3. **`TEXTMETRIC +0x37` 비트 0 은 0** (`tmPitchAndFamily` 의 가변 폭 비트 `[추측: SDK 이름]`) — 꺼 두면 52 글자 폭 계산 (1절 차례 8) 을 건너뛰고 `+0x14` 가 그대로 폭이 된다. 켤 거면 2 의 대답에 더해 `NtGdiGetTextExtent` 가 52 글자에 `cx ≥ 26` 을 줘야 폭이 1 이상.
4. **`NtGdiGetTextExtent(hdc, 글, 개수, SIZE*, 1)` → 0 아님 + `SIZE.cx` · `SIZE.cy`** (개수 > 0 이면 `cx ≥ 1`). 제목 · 단추 글 폭이 이걸로 잰다. 개수 0 이면 syscall 안 감.
5. 나머지는 **기본 대답 0 이어도 된다**:
   - `NtGdiGetTextFaceW` → 0 (버퍼 첫 글자가 `'@'` 만 아니면). 이름을 주려면 `L"Segoe UI"` 꼴.
   - `NtGdiQueryFontAssocInfo(0)` → 0. 0 이 아니면 사본 길이 영영 막힐 뿐 (되돌이로 지나감).
   - `NtGdiComputeXformCoefficients` → 0 — 사본 길을 막고 유저 코드가 직접 `DC_ATTR+0x98 |= 0x20` 을 쓴다 (그 뒤로는 되돌이만).
   - `NtGdiGetDCDpiScaleValue` → 0 · 1 아무거나 (표가 맞으면 안 부름).
   - `SelectObject` 의 글꼴 칸이 없어도 0 을 돌려줄 뿐 막히지 않는다 (반환 저장만).

### stub 이 채울 최소 목록 — run147 의 1 ~ 16 에 이어 붙임

17. **글 폭 되돌이 syscall** (메시지 상자 12 를 풀 최소, 위 1 ~ 5): `NtUserGetDCEx(0,0,3)` → 목록 8 HDC · `NtUserReleaseDC` → 1 · **`NtGdiGetTextMetricsW` ≠ 0 + `TEXTMETRIC` (`+0x00` · `+0x14` ≥ 1, `+0x37` 비트 0 = 0)** · **`NtGdiGetTextExtent` ≠ 0 + `SIZE`** · `NtGdiGetTextFaceW` · `NtGdiQueryFontAssocInfo` · `NtGdiComputeXformCoefficients` · `NtGdiGetDCDpiScaleValue` 는 기본 0 그대로.
18. **사본 길 흉내 (필요 없음 — 권하지 않음)**: 글꼴 항목 (종류 `0xa`) 의 attr `{+0 비트 0 꺼짐, +8 = 목록 머리 칸}` · `DC_ATTR+0x40` 비트 0 · 유저 전역 `gpwcANSICharSet` · `NtGdiGetWidthTable` 응답 · 공유 `+0x180098 = 1` — 하나라도 틀리면 `+0x98 |= 0x20` 으로 막힐 뿐이라 **안 만드는 쪽이 싸다.**
19. **조용한 부수 효과 · 값**: ① 공유 `+0x180094 = 0` (`gbLpk` 꺼짐) · `+0x180098 = 1` ② user32 `gpsi` 가 있어야 `GetSysColor` 가 안 죽음 ③ `NtQueryInformationProcess(자기, 0x34, {0xb, …})` → 성공 + 0 (또는 실패) — **5 금지** ④ `ApiSetQueryApiSetPresence("ext-ms-win-gdi-edgegdi-l1-1-0")` → 없음 (진짜 ntdll 을 쓰면 자동).
20. **gdiplus 백그라운드 스레드** (N2): `+0x7b3 BackgroundThreadStartup` 은 **`NtCreateEvent` ×2 (핸들 ≠ 0) · `NtCreateThreadEx` (핸들 ≠ 0)** 가 필요하다 — 못 만들면 `GdiplusStartup` 이 실패 코드로 끝난다 (`+0x7ba JZ → +0x7d1`). 새 스레드 안의 syscall (창 · 팔레트 · `GetDC` · 메시지 고리) 이 기본 대답 0 이어도 `GdiplusStartup` 반환은 안 막힌다 — `BackgroundThreadStartup` 은 스레드 핸들만 본다 (`+0x83 ~ +0x8d`). 다만 그 스레드는 메시지 고리에서 **영영 기다리는** 스레드라 에뮬레이터 스케줄러가 그걸 견뎌야 한다 `[추측: 에뮬레이터 쪽 영향]`. `SuppressBackgroundThread = TRUE` 로 부르는 앱이면 이 칸 전체가 없다.

**그 뒤 벽 한 줄**: gdiplus 시작은 위 20 까지면 끝까지 간다. 다음 벽 후보는 **글꼴 열거** (`GpInstalledFontCollection` 이 처음 쓰일 때 — 레지스트리 `…\CurrentVersion\Fonts` · 글꼴 파일) — 이번에 안 읽음.

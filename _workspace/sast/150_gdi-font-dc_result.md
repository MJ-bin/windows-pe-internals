# 150 gdi-font-dc — 정적 분석 결과

- 요청서: `_workspace/sast/150_gdi-font-dc.md` (대기열 Q278b). 먼저 읽은 것: `_workspace/plan.md` Q278b 줄 · `_workspace/sast/145_gdi-display-dc_result.md` 전부 · `141_gdi-startup_result.md` (표 검사 표) · `139_user32-connect_result.md` (메시지 상자 표) · `142_dialog-window_result.md` 일부. **정적만.** 진짜 핸들 · `DC_ATTR` 덤프는 안 함 (Q277 몫).
- 모듈 / 바이너리 (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`): `gdiplus` (`GdiPlus.dll`) · `gdi32` · `gdi32full` · `user32` (`GetDC` · `ReleaseDC` · `GetSysColor` 쪽만) · `win32kfull` · `win32kbase` — **전부 이미 analyze 됨** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 한 줄씩 확인), **다시 analyze 안 함.** 이 요청이 연 프로젝트는 위 여섯 개뿐 (`-readOnly` export · refs). 다른 decompiler 가 쓰는 `ntoskrnl` · `ntdll` · `kernelbase` · `combase` … 는 **Ghidra 로 안 열었다** — 그쪽은 (a) 이미 있는 `.asm` 읽기, (b) PE 파일 직접 `objdump` / 파이썬 (`[objdump]` 표시) 으로만 봤다.
- PDB GUID (`ms/sym/*.pdb.info.txt`, 145 · 141 · 139 와 **같음**):
  - gdi32 `92C6EC2ABE478836C99BC83C2F3AE3551` · gdi32full `A6245E4F5C78EB06438EA4820F1F6EB41` · gdiplus `DDF19AA09F285660C86065C38EF751A41`
  - win32kbase `D22DFDA3D54B87E6BD0567A57877904C1` · win32kfull `5CD57181BCFDC5AE8F4819BEB1196F091` · user32 `CDB947D1B96B87977915DE04CF54279C1`
  - (커널 GUID 확인은 이번엔 해당 없음 — ntoskrnl 을 안 열었다. 기존 `ntoskrnl/NtQueryInformationProcess.asm` · `PspAllocateProcess.asm` 파일은 읽기만.)
- 버전 대조: **일치.** 145 가 적은 줄을 다시 `.asm` 에서 찾음 — `hdcCreateDCW +0xc9 NtGdiOpenDCW (ret +0xd0)` · `InternalGdiplusStartup +0x280 CreateICA (ret +0x287) · +0x296 JZ · +0x60b · +0x62b InitImagingLibrary · +0x6d5 SysColorNotify · +0x73d InitSystemFontsDirs · +0x775 GetGpInstalledFontCollection · +0x7b3 BackgroundThreadStartup` · `GetDeviceCaps +0x419 CMP R9,-0x2321b000` · `GreCreateDisplayDC +0x231/+0x243` · `DCMEMOBJ +0x1b8 (0x12001f)` — 모두 같다. user32 `GetDC` rva `0x4a8c0` (145 의 `[objdump]` 값) 도 이번에 Ghidra 로 `GetDC.asm` 을 만들어 확인.
- 뽑은 것 (이번에 새로 만든 파일. 기존 파일은 덮지 않음 — 겹친 이름은 `__<rva>` 로 갈림):
  - gdi32full (깊이 0): `pcfLocateCFONT` `bComputeTextExtent` `bComputeTextExtentDBCS` `bGetTextMetricsWInternal` `GetTextExtentPointWInternal` `GetDCDpiScaleValue` `pcfCreateCFONT` `pcfAllocCFONT` `InsertCFontCache` `vRemoveCFONTFromDcAttr` `DeleteHDCCFont` `bFillWidthTableForGTE` `bFillWidthTableForGCW` `GetTextFaceW`
  - win32kbase: `InitializeDefaultDCAttributes` `InitializeGre` `InitializeGreCSRSS` `GdiProcessCallout` `bInitGdiInfo` `bInitICM` `bInitBrush` `bInitBRUSHOBJ` `bInitPALOBJ` `bInitBMOBJ` `bInit__1094c0` `bInit__1af630` `GreUpdateSharedDevCaps` `vGetDeviceCaps` `GreGetDeviceCaps` `StubDispEnablePDEV` `StubDispGetModes` `MulEnablePDEV` `EnablePDEV` `LpkShapeQueryRoutine` `InitVideo` `InitUserScreen` `SetDPIinSetup` `NtUserSetProcessDpiAwarenessContext` `W32SetCurrentThreadDpiAwarenessContext` `W32GetCurrentThreadDpiAwarenessContext` `NtUserGetProcessDpiAwarenessContext` `vSetDpiScaling` `vClearDpiScaling` `SetCachedDpiScaleValue` `vUpdateCachedDPIScaleValue` `GetCurrentThreadCompositedDpi` `GetDpiAwarenessContext` `GetDpiSetting` `DrvGetDisplayDriverDpiSetting` `DrvGetCurrentDpiInfoFromHDev` `UserGetDpiInfoFromPrimaryMonitor` `MonitorFromHdev`
  - win32kfull: `bSetStockFont` `bInitStockFontsInternal` `bInitOneStockFontInternal` `FinishStockFontInit` `FinishStockFontInitInternal` `vInitEmergencyStockFont`
  - gdi32: `IsPseudoDesktopDcForFilteredProcess` `D3DKMTOpenAdapterFromGdiDisplayName` · user32: `GetDC` `ReleaseDC` `GetSysColor` · gdiplus: `GpInstalledFontCollection` (깊이 1: `GpFontTable` 등) `CreateThreadAndAddRefDll` `AddRefModule` `VGAHashRebuildTable` `BackgroundThreadProc` `DllRefCountSafeThreadThunk` `InternalNotificationStartup`
  - refs (누가 읽고 쓰나): gdi32full `0x10c59c` (`guintDBCScp`) · `0x108d20` (`gpwcANSICharSet`) · `0x108cf0` (`gbLpk`) · `0x1084ac` (글꼴 연결 상태) · user32 `0xd9970` (`g_systemCallFilterId`) · gdi32 `0x25780` (같은 이름)
- 메모리: export · refs 매번 시작 전 `free -g` (available 22 ~ 28 GB, 한 번에 3 초 안쪽, 서른 번 남짓). 임시 파일 `scratchpad/s150/`.
- **`rva:` 와 정규식은 한 인자에 못 섞는다** (`'^(A|B)$|rva:0x1,0x2'` 로 불렀더니 이름 일곱 개만 뽑히고 `rva:` 쪽은 무시됨). 따로 두 번 불러야 함 — 오류는 아니고 쓰는 법 메모 (아래 "스크립트 문제").

## 먼저 읽는 한 줄 답

1. **(N3 글 폭 — 읽는 칸 · syscall · 실패면)** `[정적]` 메시지 상자 12 길은 `NtUserGetDCEx(0,0,3)` → `SelectObject` → user32 `UserGetCharDimensionsEx` → `GetTextFaceW` (syscall `NtGdiGetTextFaceW` `0x1078`) → **`GdiGetCharDimensions(hdc, NULL, &cy)`** → `pcfLocateCFONT` → `bGetTextMetricsWInternal` → (가변 폭 글꼴이면) `GetTextExtentPointWInternal` → `MB_GetMaxButtonSize` 안 `GetTextExtentPointW` 차례다. gdi32full 쪽이 **syscall 앞에서 보는 칸**:
   - 표 칸 (`+0x8 +0xc +0xe +0x10`, 145 의 다섯 검사) — 실패하면 `GdiGetCharDimensions` 는 **아무것도 안 쓰고 0** (`+0x19c`), `GetTextMetricsW` 도 0. 반면 `GetTextExtentPointW` · `GetTextExtentPointWInternal` 은 **표 검사 앞에서 먼저 `GetDCDpiScaleValue(hdc) == 1` 을 보고** 아니면 곧장 `NtGdiGetTextExtent` syscall 로 간다 (아래 "141 고쳐 읽기").
   - `DC_ATTR`: `+0x88` (DPI 비율, **`> 1` 이면 사본 길 포기**, `GetTextExtentPointW*` 는 **정확히 1** 이어야 사본 길) · `+0x98` 비트 `0x20` (켜지면 사본 길 포기 — 읽고 **씀**; `SelectObject(글꼴)` 이 `SelectObjectImpl +0x3b3 ~ +0x3bf` 에서 다시 지움) · `+0x40` 비트 0 · `+0x154 & 0xe000` · `+0x128` (글꼴 핸들) · `+0x20 +0x2c` (float, 사본 비교) · `+0x118 +0x11c +0x120` (글 늘리기 — `bComputeTextExtent`) · `+0x160` (DC 가 쥔 사본 목록 머리, **`pcfLocateCFONT` 가 씀**) · `+0x114` (**`gbLpk ≠ 0` 일 때만**).
   - syscall (전부 사본 길이 막혔을 때의 **되돌이**): `NtGdiQueryFontAssocInfo(0)` `0x10e1` (한 번, 전역에 저장) · `NtGdiComputeXformCoefficients` `0x1083` (`DC_ATTR+0x40` 비트 0 이 0 일 때) · `NtGdiGetDCDpiScaleValue` `0x12be` (표 실패 때) · **`NtGdiGetTextMetricsW` `0x106e` (`hdc`, 버퍼, `0x44`)** · **`NtGdiGetTextExtent` `0x108c` (`hdc`, 글, 개수, `SIZE*`, 깃발 1)** · (사본 새로 만들 때) `NtGdiGetWidthTable` `0x1063`.
   - **0 / 실패면**: `NtGdiGetTextMetricsW` 가 0 → `bGetTextMetricsWInternal` 0 → `GdiGetCharDimensions` 가 **폭 0 을 돌려주고 `*cy` 도 안 씀** → `SoftModalMessageBox +0x151 TEST EDX · +0x153 JZ → +0x69f` → `MessageBoxW` 0 (139). 반대로 **사본 길이 막혀도 (NULL) syscall 되돌이가 있어 지나간다** — 그래서 에뮬레이터는 **CFONT 사본을 안 만들어도 되고**, `NtGdiGetTextMetricsW` · `NtGdiGetTextExtent` 만 `≠ 0` + 값 채우기로 풀린다 (아래 "PE_emulator 쪽").
   - **`NtUserGetDCEx(0,0,3)` = 0 이 되는 조건** (win32kfull `NtUserGetDCEx` rva `0x23c180` → win32kbase `_GetDCEx` `0x67600` → `CreateCacheDC` `0x14370`): ① `hrgn` 이 `[세션+0xace8]` 과 같음 · ② `flags & 0xffdaf800` (`3` 은 0) — 둘 다 `LastError 0x57` + 0 · ③ **UI 한도 (job 의 `UILIMIT` 비트 0x1)** 가 걸린 프로세스 + 기능 꺼짐 (`Feature_LearningModeExpansion` 초기값 꺼짐) + `ValidateHwnd(바탕 창)` 실패 → `LastError 5` + 0 · ④ `CreateCacheDC` 0 (`GreCreateDisplayDC` 0 = **주 화면 장치 `[세션+0xe338]+0x30` 없음** 따위) 또는 `GreSetDCOwnerEx` 실패. **일반 프로세스 · 화면 있음이면 0 이 될 길이 없다** → 12 샘플의 0 은 stub (기본 대답 0) 때문 (표 (B)).

2. **(win32k 표 채우기)** `[정적]` 세션 기본 `DC_ATTR` 틀 `[W32GetSessionState()+0x58]+0x90` (0x1b0 B) 는 **`InitializeGre` (win32kbase `0x2f93fc`) 가 `InitializeDefaultDCAttributes` (`0x1914e8`, `+0xb8` 에서 부름) 로 채운다**: `+0x88 = 1` · `+0x98 = 0x10` (DC 를 만들 때 `DCMEMOBJ +0x1b8` 가 사본에 `0x12001f` 로 덮음) · `+0x40 = 0x4b` · `+0x20 = +0x2c = 16.0f` · `+0x154 = 0x1802` · `+0xf0 = 0` · `+0x00` = 0 · `+0x108 +0x114` = 0. 붓 · 펜 · 글꼴은 **스톡 핸들**: `+0xa0` = 스톡 0 (WHITE_BRUSH, `bInitBrush +0xa0`) · `+0xa8` = 스톡 7 (BLACK_PEN, `bInitBRUSHOBJ +0x286`) · `+0x128` = 스톡 13 (SYSTEM_FONT, win32kfull `bInitStockFontsInternal +0x300` · `FinishStockFontInit +0x198`). 스톡 핸들 = 객체 핸들에 **`0x800000` 비트를 켠 값** (`bSetStockObject +0x33 BTS R10,0x17`), 배열 `[세션+0xbc0]` (DPI 변종 `[+0xbc8]`). `GDIINFO` (`PDEV+0x848`): 연결이 끊긴 세션의 가짜 화면 (`StubDisp*`) 이면 `bInitGdiInfo` (`0x1b6038`) 가 상수로 채움 (1024 × 768 · 32 비트 · 60 Hz · `LOGPIXELS` = DEVMODE `dmLogPixels`); 진짜 화면 드라이버는 `EnablePDEV` 간접 호출 (`[공백]`). **`pGdiDevCaps` 블록 (`+0x180000 ~ +0x180090`) 은 `GreUpdateSharedDevCaps` (`0x14dd20`) → `vGetDeviceCaps` (`0x14dd5c`) 가 GDIINFO 에서 옮겨 적는다** (`InitVideo +0x1ba` · `SetDPIinSetup` · `DrvChangeDisplaySettingsInternal`); 같은 쪽 `+0x180094` (언어팩 비트) · `+0x180098` (=1) 은 `InitializeGre +0x4ac / +0x4cb`.

3. **(필터 값 5 갈래)** `[정적]` `g_systemCallFilterId` = `GetProcessMitigationPolicy(자기, 정책 0xb, &값, 4)` 의 **하위 4 비트** (`Gdi32DllInitialize +0x4b/+0x62` · user32 `ClientThreadSetup +0x45` · `_UserClientDllInitialize +0x463`). kernelbase 가 이걸 **`NtQueryInformationProcess(자기, 0x34 = ProcessMitigationPolicy, {정책 0xb, 값}, 8)`** 로 바꿔 부른다 (`[objdump]` kernelbase `GetProcessMitigationPolicy` rva `0x216f0` `+0x47 call [NtQueryInformationProcess]` (ret `+0x4e`, `edx = 0x34`), 점프표 11 번 (`+0xa5`) 이 `[RBP-0x30] = 0xb` 를 씀). 커널 (`NtQueryInformationProcess +0x1d38 ~ +0x1d53`) 은 **`PsGetWin32KFilterSet()` = `[EPROCESS+0x6e8]`** 의 하위 4 비트를 돌려준다 — 이 칸은 **프로세스를 만들 때 부모가 준 "Win32k 필터" 속성** (`PspAllocateProcess +0x1f0e/+0x1f11`, `[속성+4]` → `[EPROCESS+0x6e8]`) 이다. 일반 프로세스는 **0**. **5 일 때 달라지는 것**: user32 `GetDC(아무 hwnd)` 는 **syscall 없이 가짜 HDC `0xFFFFFFFFDCDE5000`** 을 돌려줌 (`GetDC +0x0 CMP · +0x7 JZ → +0x15 MOV RAX,-0x2321b000`) · `ReleaseDC` 는 그 값이면 1 (`+0x43 ~ +0x4c`) · gdi32 `GetDeviceCaps` 는 그 값이면 표 검사 없이 **블록 길** (`+0x419 CMP R9 · +0x420 JZ → +0x1e8`) · gdi32 `D3DKMTOpenAdapterFromGdiDisplayName` 은 `0xC000001C` · user32 은 창 속성 · 메시지 · 훅 · 클래스 지우기 · `SetWindowLongPtr` · IME 클래스가 **쓰는 쪽이 따로** (표 (E), 25 곳). **값이 0 이면 (또는 `NtQueryInformationProcess` 가 실패해 FALSE 가 와도) 이 갈래는 하나도 안 탄다.**

4. **(win32k 몫 2 · 4 · 6)** `[정적]` ① `GrepCreateCompatibleDC` (`0x14950`): 원본 DC 있으면 **`GreCreateDisplayDC(원본 DC 의 PDEV `[DC+0x30]`, 종류 1 (메모리), 0)`** (`+0x126`), 원본 NULL (`CreateCompatibleDC(0)`) 이면 **`GreCreateDisplayDC([세션+0xe338]+0x30, 1, 0)`** (`+0x256`). 레이아웃 비트 (`[dcattr+0x6c] & 7`) 면 `dwSetLayout` (`+0x293`), 원본 `[DC+0x208] & 1` (DPI 비율 켜짐) 이면 **`vSetDpiScaling(새 DC, [원본+0x20c])`** (`+0x2a4`). 메모리 DC 는 기본 비트맵 1 × 1 (`GreCreateDisplayDC +0x155 ~ +0x17e`, 종류 1). ② DPI 칸: `[W32PROCESS+0x10c]` (dword, DPI 인식 문맥) 를 **`NtUserSetProcessDpiAwarenessContext` (`0x62350`) `+0xc4` 가 씀**; `[W32PROCESS+0x110]` (word, 시스템 DPI) 를 **`InitUserScreen +0x396` (모든 프로세스 순회) · `SetDPIinSetup +0x492` (지금 프로세스)** 가 `[psi+0x1b56]` 로 씀; `[스레드+0x190]` 를 **`W32SetCurrentThreadDpiAwarenessContext +0x6d`** 가 씀 (`+0x198` 는 유저 모드 쪽 복사 포인터). DC 의 `DC_ATTR+0x88` (비율) 은 **`SetCachedDpiScaleValue`** (`0x13140`) 만 씀 — `vUpdateCachedDPIScaleValue` 가 "DPI 변환 없음" 이면 **1**. ③ 주 화면 판정 `[세션+0xe338]+0x30` 는 **`InitVideo +0x179 ~ +0x180`** (`DrvSetDisplayConfig` 가 준 목록의 첫 장치) 가 쓰고 `SetDPIinSetup +0x3e5` 가 다시 씀. ④ `hdcOpenDCW` (`0x3a260`, 이름 ≠ NULL): `DrvGetHDEV(이름)` 로 화면 장치를 찾으면 `UserGetMonitorDC` / `GreCreateDisplayDC`, 아니면 `PDEVOBJ` 를 새로 지어 (`+0x278`) `DrvCreateMDEV` (`+0x426 · +0x470`) → `GreCreateDisplayDC` (`+0x492`).

5. **(N2 gdiplus 벽)** `[정적]` **벽이 아니다.** `+0x62b InitImagingLibrary(깃발)` 은 전역 둘 · 임계 구역 하나 쓰고 **항상 1** (`+0x2d MOV EAX,1`) — `+0x632 JZ` 는 못 탄다. `+0x6d5 SysColorNotify` = user32 `GetSysColor` ×4 (`psi` 표 읽기, syscall 없음). `+0x73d InitSystemFontsDirs` = `GetSystemDirectoryW` → 마지막 `\` 앞까지 자르고 `\fonts` (소문자) 붙인 문자열 (`FontsDirW`) · `WideCharToMultiByte` ×2 (`FontsDirA`) — **파일 · 레지스트리 안 건드림.** `+0x775 GetGpInstalledFontCollection` = `GpMallocEx(0x18)` + 빈 `GpFontTable` (`HeapAlloc(0x10)`) **뿐** (글꼴 열거는 나중, 부를 때). 그 사이 `+0x6e1 GetModuleHandleW("gdi32.dll")` · `GetProcAddress` ×2 (`ExtTextOutW` · `GdiIsMetaPrintDC`) · `+0x72a GetSystemDefaultLCID` · `+0x78b GpMallocEx(0x10)`. 마지막 `+0x7b3 BackgroundThreadStartup` (`[입력+0x10]` = `SuppressBackgroundThread` 가 0 일 때) 은 **`CreateEventW` ×2 (수동 · 꺼짐) + `GetModuleFileNameA` · `LoadLibraryExA` (자기 자신 한 번 더) + `HeapAlloc(0x18)` + `CreateThread`** — 이게 **이 길에서 처음 나오는 새 syscall 꾸러미** (`NtCreateEvent` ×2 · `NtCreateThreadEx` · 로더). 하나라도 NULL 이면 `GdiplusStartup` 이 실패 코드. 새 스레드는 따로 숨은 창 · 팔레트 · `GetDC(숨은 창)` · `SelectPalette` 를 하고 메시지 고리에서 기다린다 (표 (F2), 반환값은 하나도 안 봄).

6. **(그 밖)** `[정적]` ① **N5**: `NtQueryInformationProcess(−1, 0x25 = ProcessImageInformation, 버퍼 0x40)` 을 부르는 줄은 kernelbase **`GetProcessVersion` (`0x23dc0`) `+0x3b` · `+0xd9`** `[objdump]` (pid 0 이면 `-1`). 그런데 **kernelbase 안에서 `GetProcessVersion` 을 부르는 줄이 없고** (`call` 전수 조사 0건) `GetVersionExA → GetVersionExW (0xc4ae0)` 는 `RtlGetVersion` + `SbSelectProcedure(0xabababab, 0, 표 0x1801aa300, 3)` 만 쓴다 → **gdiplus `InitVersionInfo` 에서 0x25 로 이어지는 줄은 못 찾음 `[공백]`** (141 의 "GetVersionExA 어딘가" 는 확인 안 된 말이었음). ② **표 칸 `+0x0` · `+0xf`**: 유저 쪽이 읽는 곳이 **글꼴 하나** — `SelectObjectImpl +0x398 TEST [항목 +0xf],0x2` (켜져 있으면 빠른 길 건너뛰고 되돌이) `[정적]`; `+0x0` 은 gdi32 · gdi32full 어디도 안 읽음 (항목 복사 `MOVUPS` 뿐) → 커널 객체 포인터 자리 `[추측]`. ③ **`g_pEdgeGdi` 갈래**: `GetDeviceCaps +0x2b ~ +0x41` (`InitOnceExecuteOnce(CheckIsEdgeGdiProcessOnce)`) → `IsEdgeGdiInitializePresent` (`0x6d70`) 가 **API 세트 `ext-ms-win-gdi-edgegdi-l1-1-0` 가 있나** (`ApiSetQueryApiSetPresence`) 를 물음 → 있으면 `EdgeGdiInitialize` → `g_pEdgeGdi` ≠ 0 이면 **모든 GDI 호출이 Edge 쪽으로 새**. 이 빌드의 `apisetschema.dll` 은 그 계약의 **호스트 이름이 빈 글** (`[objdump]` 파이썬 파서) 이고 `edgegdi.dll` 도 System32 에 없다 → 보통 `g_pEdgeGdi == 0` `[추측: 호스트 빈 글 = 없음으로 취급]`. ④ **`[GpDevice+0x628]`**: `GpDevice::GpDevice` 가 0 으로 두고 (`+0xa0`), **`TECHNOLOGY == 1` 이고 `BITSPIXEL ≤ 8` 일 때만** `PaletteChangeNotification` (`+0x1e5`) 뒤 `+0x1ea CMP [+0x628],0` 로 본다 — 0 이면 `[+8] = 0x4c494146` 로 GpDevice 가 못 쓰게 됨. 32 비트 화면이면 **안 읽는다.**

## (A) 글 폭 길 지도 — `NtUserGetDCEx(0,0,3)` 의 DC 로 글 폭을 재기까지

`[정적]` = `ms/disassemble/<모듈>/<함수>.asm +오프셋` (call 은 `(ret +…)`). syscall 번호 = `ms/disassemble/win32u/_syscalls.tsv`. 앞 두 줄은 139 · 141 에서 읽은 것을 이어 붙임.

| 차례 | 부르는 것 / syscall | 조건 | 반환을 보나 | 0 이면 | 읽는 / 쓰는 칸 `[정적]` |
|---|---|---|---|---|---|
| 0 | user32 `SoftModalMessageBox` `+0xf5 NtUserGetDCEx(0,0,3)` (ret `+0xfc`) | — | `+0x104 TEST · +0x107 JZ → +0x636` | 한 번 더 (hwnd = `MSGBOXDATA+8`), 그래도 0 이면 `MessageBoxW` 0 (139) | 표 (B) |
| 1 | `+0x126 SelectObject(hdc, 글꼴)` (ret `+0x12d`) | 141 표 (DC · 글꼴 칸) | 이전 글꼴을 `[RBP-0x68]` 에 저장만 | 글꼴 칸 없음 → 0 (오류 안 세움), DC 칸 없음 → `LastError 0x57` + 0 | `DC_ATTR+0x128` 읽고 씀 (`SelectObjectImpl +0x3a3 ~ +0x3c5`), `+0x98` 에 `(& ~0x20) \| 0x10` (`+0x3b3 ~ +0x3bf`) |
| 2 | `+0x149 UserGetCharDimensionsEx(hdc, 0, 0, &cy)` (ret `+0x14e`) → user32 `+0x72 GetTextFaceW(hdc, 0x20, 버퍼)` (ret `+0x79`) | 버퍼 ≠ NULL · 개수 > 0 이면 syscall, 개수 ≤ 0 이면 `GdiSetLastError(0x57)` + 0 (gdi32full `GetTextFaceW +0x1c ~ +0x25`) | 버퍼 첫 글자만: `+0x7e CMP [버퍼],0x40` (`'@'` = 세로 글꼴) | 0 이면 버퍼가 처음 0 그대로 → `'@'` 아님 → 일반 길 | syscall `NtGdiGetTextFaceW` (`0x1078`, gdi32full `GetTextFaceW +0x10 JMP [0x1800c1f78]`) |
| 3 | user32 `+0x92 GdiGetCharDimensions(hdc, 0, &cy)` (ret `+0x99`) — gdi32full `0x4db20` | **표 검사**: `+0x48 HANDLE_TO_INDEX` · `+0x58 JNC` (번호 ≥ 최대) · `+0x6c GdiGetEntry` (ret `+0x73`) · `+0x7a JS` · `+0x80 CMP 종류,1` · `+0x91 윗 16` · `+0xa8 PID (& ~1)` · `+0xb7 JZ (+0x10 = 0)` · `+0xd7 JZ (풀어서 0)` | `AX` = 폭 (SoftModal 이 `+0x151 TEST EDX`) | **표 검사 하나라도 실패 → `+0x19c XOR EAX,EAX` 로 그냥 0, `*cy` 안 씀, syscall 없음** | `DC_ATTR` 포인터 (표 `+0x10` 풀기). 이 안에서 임계 구역 `semLocal` (`+0xef`, ret `+0xf6` ~ `+0x13c`, ret `+0x143`) |
| 4 | `+0x119 pcfLocateCFONT(hdc, DC_ATTR, 0, 0, 0, 0)` (ret `+0x11e`) — 글꼴 사본 찾기 (gdi32full `0x1bbd0`) | 아래 (4a) ~ (4h) | `+0x128 MOV RDI,[RAX]` = **사본 포인터** (NULL 이면 사본 없음) | NULL → 사본 길 건너뜀 → 차례 5 가 **syscall 로** | (4a ~ 4h) |
| 5 | `+0x12e bGetTextMetricsWInternal(hdc, 지역 TEXTMETRIC (0x3c B), 0x3c, 사본)` (ret `+0x133`) — `0x4dd30` | 사본 ≠ NULL 이고 `[사본+0x1c] & 8` (메트릭 이미 저장) 이면 `+0x4c memcpy` (ret `+0x51`) 로 **syscall 없이** | `+0x12e` 반환 → `EBX`, `+0x148 TEST · +0x14a JZ → +0x173` | **0 → `ESI = 0` 폭 0 으로 돌아감 (`*cy` 도 안 씀)** | 아니면 **`+0x104 NtGdiGetTextMetricsW(hdc, 버퍼 0x44, 0x44)`** (ret `+0x10b`, `0x106e`) → `+0x114 JZ` → 0 이면 0 · 아니면 `+0x125 memcpy` (ret `+0x12a`) 로 지역 `TEXTMETRIC` 채우고, 사본 있으면 `+0x13c ~ +0x177` 에 **저장** (`[사본+0x1c] \|= 8`, `[사본+0x258 ~ +0x298]`) |
| 6 | `GdiGetCharDimensions +0x14c TEST R12 (cy 포인터)` → `+0x1f8 *cy = tmHeight (TEXTMETRIC +0x0)` | `lpcy ≠ NULL` | — | — | 쓰는 칸: 호출자 `cy` |
| 7 | `+0x155 TEST [TEXTMETRIC+0x37],1` (tmPitchAndFamily 의 **가변 폭 비트**) | 켜져 있으면 8 로, 꺼져 있으면 **`+0x16f ESI = tmAveCharWidth` (`TEXTMETRIC+0x14`)** 로 끝 | — | — | `TEXTMETRIC +0x37` · `+0x14` · `+0x0` |
| 8 | (가변 폭) 사본 없거나 (`+0x15f JZ`) 사본 `[+0x1c] & 0x10` 꺼짐 (`+0x165 JZ`) 이면 `+0x1c0 GetTextExtentPointWInternal(hdc, "abc…xyzABC…XYZ" (52 자), 0x34, &SIZE, 깃발 1)` (ret `+0x1c5`) | 사본 `[+0x1c] & 0x10` 켜져 있으면 `+0x167 ESI = [사본+0x254]` (이미 계산된 폭) | `+0x1c7 JZ → +0x173` | **0 → 폭 0** | 폭 = `(cx / 26 + 1) / 2` (`+0x1c9 ~ +0x1e5`, 정수 곱 `0x4ec4ec4f`), 사본에 `[+0x1c] \|= 0x10` · `[+0x254] = 폭` (`+0x1ec ~ +0x1f0`) |
| 9 | `GetTextExtentPointWInternal` (`0x11b90`): `+0x2b JLE` (개수 ≤ 0: 0 이면 `*SIZE = 0` + 1 반환 `+0x263 ~ +0x26f`, 음수면 `LastError 0x57`) · **글자 전부 ASCII** (`+0x50 TEST [글],0xff80`, 하나라도 아니면 `+0x64 JNZ → +0x20b`) · **`+0x6d GetDCDpiScaleValue(hdc)` (ret `+0x72`) `== 1`** (`+0x75 JNZ → +0x20b`) · `+0x7e HANDLE_TO_INDEX` · 표 검사 (`+0xc8 ~ +0x119`, 실패 → `+0x8e GdiSetLastError(0x57)` + 0) · `gbLpk` 꺼짐 (`+0x11f CMP [0x180108cf0]`) | 위 다 통과하면 `+0x195 pcfLocateCFONT` (ret `+0x19a`) → 사본 ≠ NULL 이면 `+0x1c9 bComputeTextExtent` (ret `+0x1ce`) | `+0x1a8 JZ → +0x24e` (사본 NULL) · `+0x1d5 JZ → +0x24e` (계산 실패) | **둘 다 `+0x20b` 되돌이 → `+0x281 NtGdiGetTextExtent(hdc, 글, 개수, &SIZE, 1)` (ret `+0x288`, `0x108c`)**; `gbLpk ≠ 0` 이면 `+0x247 LpkGetTextExtentExPoint` | 사본 길의 읽는 칸은 (4) 와 같음 + 사본 `[+0x54 + 2*글자]` 폭 표 |
| 10 | user32 `SoftModalMessageBox +0x249 GetTextExtentPointW(hdc, 제목, 길이, &SIZE)` (ret `+0x250`; `cx` 는 `+0x266` 이 `[RBP-0x58]` 로 읽음) · `+0x16f MB_GetMaxButtonSize` 안 `+0x74 GetTextExtentPointW` (ret `+0x7b`; 다른 갈래 `+0x8e PSMGetTextExtent`) — gdi32full `0x12ce0` | 9 와 **같은 차례 · 같은 조건** (`+0x2e JLE`, `+0x40 JNZ → +0x214`, `+0x69 GetDCDpiScaleValue` ret `+0x6e`, `+0x70 JNZ → +0x214`, `+0x192 pcfLocateCFONT` ret `+0x197`, `+0x1b9 bComputeTextExtent` ret `+0x1be`) | SoftModal 은 `[RBP-0x58]` (cx) 를 읽음 | 되돌이 `+0x262 NtGdiGetTextExtent` (ret `+0x269`) | 같음 |
| 11 | user32 `+0x2e0 DeleteObject(글꼴)` · `+0x2ef NtUserReleaseDC(hdc)` (ret `+0x2f6`, `0x151c`) | 141 | — | — | — |

### (4a ~ 4h) `pcfLocateCFONT` 안 — 사본을 포기하는 가지 (gdi32full `0x1bbd0`)

NULL 이 나오는 길은 전부 **조용히** (`LastError` 안 세움) 이고, 부른 쪽이 syscall 되돌이로 간다.

| 가지 | 줄 `[정적]` | 조건 → 결과 |
|---|---|---|
| 4a DPI 비율 | `+0x112 MOV EDX,[DC_ATTR+0x88]` · `+0x11d ReturnValueTracer<int>` (ret `+0x122`) · `+0x12c CMP EAX,1 · +0x12f JG → +0x389` · 표 검사 실패면 `+0x352 NtGdiGetDCDpiScaleValue` (ret `+0x35c`, `0x12be`); `hdc == 0` 이면 1 (`+0x4e2`) | **`> 1` 이면 NULL.** 0 · 1 은 통과 |
| 4b 글꼴 연결 상태 | `+0x135 MOV EAX,[0x1801084ac]` · `+0x13b CMP -1` · `+0x142 NtGdiQueryFontAssocInfo(0)` (ret `+0x149`, `0x10e1`) · `+0x14e` 저장 · `+0x154 TEST · +0x156 JNZ → +0x389` | 반환 **≠ 0 이면 NULL** (동아시아 글꼴 연결). 한 번만 부름 |
| 4c 포기 깃발 | `+0x15c TEST [DC_ATTR+0x98],0x20 · +0x16a JNZ → +0x389` | 켜져 있으면 NULL (글꼴을 다시 고르면 `SelectObjectImpl +0x3bc` 가 지움) |
| 4d 변환 계수 | `+0x170 TEST [+0x154],0xe000 · +0x17a JNZ → +0x5a8` · `+0x180 TEST [+0x40],1 · +0x184 JZ → +0x5a8` → `+0x5ab NtGdiComputeXformCoefficients(hdc)` (ret `+0x5b2`, `0x1083`) | **`[+0x40]` 비트 0 이 0 이면 syscall**; 0 을 돌려주면 `+0x5cd OR [+0x98],0x20` → NULL (같은 DC 에서 글꼴을 다시 고를 때까지). ≠ 0 이면 계속 |
| 4e 한자권 코드 페이지 | `+0x18a CMP [guintDBCScp],-1` (처음 `-1`, `GdiProcessSetup +0x124` 가 일본 · 한국 · 중국 코드 페이지 (`0x3a4 · 0x3b5/6 · 0x3a8`) 일 때만 값을 씀) | `-1` 이면 건너뜀. 아니면 `+0x4bb GetCodePage(hdc)` (gdi32full `0x3ddf0`, 본문은 안 읽음 `[공백]`) 와 비교해 다르고 한자권 (`0x3a4 · 0x3b5 · 0x3b6 · 0x3a8` 중 `0x60011` 비트표 `+0x366 ~ +0x382`) 이면 `+0x98 \|= 0x20` → NULL. **타깃 VM 의 ACP 가 한국어 (949) 면 `guintDBCScp = 949` 라 이 길을 탄다** (VM 에서 `GetACP`). 같은 사본 길의 DBCS 폭 계산은 `bComputeTextExtentDBCS` (`0x8bbec`, 이번에 안 읽음) |
| 4f 글꼴 칸 | `+0x1a9 ~ +0x25e` — `DC_ATTR+0x128` 핸들을 **표에서 다시 읽기**: 번호 · `+0xe == 0xa` (`+0x223`) · 윗 16 · PID · `+0x10` ≠ 0 · 풀어서 ≠ 0 | 하나라도 틀리면 `+0x3db OR [+0x98],0x20` → NULL (글꼴을 다시 고를 때까지) |
| 4g 글꼴 attr | `+0x286 MOV RAX,[attr+8]` · `+0x28d JZ → +0x3db` · `+0x293 TEST [attr],1 · +0x296 JNZ → +0x3db` · `+0x29c MOV RBX,[RAX]` (사본 목록 머리) | `[attr+8] = 0` 이거나 `[attr] & 1` 이면 NULL (글꼴을 다시 고를 때까지) |
| 4h 목록 훑기 · 만들기 | `+0x2a2 JZ → +0x3e4` (목록 비었음) → `+0x40b pcfCreateCFONT` (ret `+0x410`, `0x3d884`) — **`gpwcANSICharSet` (`0x108d20`) ≠ 0 필요** (`pcfCreateCFONT +0x19 CMP · +0x2d JZ`), `bGetANSISetMap` 이 씀 · 새 사본 `+0x44 [공유 메모리+0x180098]` 를 `[사본+0x5c8]` 에 복사 · `bFillWidthTableForGTE` / `GCW` (`NtGdiGetWidthTable` `0x1063`) | 사본이 있으면 `[사본+0x18]++` (참조), `+0x178 DEC` 로 돌려놓음. 목록에서 찾는 조건: 소유자 `[사본+0x28] == hdc` 또는 (`== 0` 이고 `DC_ATTR` 비트 1 켜짐), `[사본+0x30] == [DC_ATTR+0x20]`, `[사본+0x34] == [DC_ATTR+0x2c]` |

### (4i) `bComputeTextExtent` (gdi32full `0x1e2b4`) — 사본 길로 폭을 재는 쪽

`[정적]` 읽는 칸: `DC_ATTR +0x11c` (`+0x13`) · `+0x118` (`+0x2e`) · `+0x120` (`+0x38`) = 글 늘리기 (가름 글자 수 · 여분) — 처음 0 이면 영향 없음. 사본 `+0x54 + 2*글자` (WORD 폭 표, `0xFFFF` = 아직 안 채움 → 채우기는 `pcfLocateCFONT` 가 `bFillWidthTableForGTE` 로 미리) · `+0x38` (float `0.0625` = 1/16 이면 `(합 + 8) >> 4` 로 정수 폭, 아니면 `bFToL`) · `+0x44` (WORD, 고정 폭이면 글자 수 × 이 값) · `+0x46` · `+0x40` · `+0x4a` (쉼 글자) · `+0x20` (DWORD → **`SIZE.cy`**, `+0x17b ~ +0x183`) · `+0x28` · `+0x50` (비트 2). 결과: `SIZE.cx` (`+0x179`) · `SIZE.cy` (`+0x183`), 반환 1.

## (B) `NtUserGetDCEx(0,0,3)` 이 0 이 되는 길 — 줄 근거

호출 = `(hwnd = 0, hrgn = 0, flags = 3)`. `3 = DCX_WINDOW \| DCX_CACHE` `[추측: 이름]`. 함수 시작 = win32kfull `NtUserGetDCEx` rva `0x23c180`.

| 차례 | 줄 `[정적]` | 조건 | 결과 |
|---|---|---|---|
| 1 | `+0x28 EnterSharedCrit` (ret `+0x2f`) | 항상 | 세션 임계 구역 |
| 2 | `+0x3a TEST RSI · +0x3d JZ → +0x56` | `hrgn = 0` 이면 `GreIsValidRegion` 건너뜀 (`+0x42`, ret `+0x49`) | — |
| 3 | `+0x56 W32GetUserSessionState` (ret `+0x5d`) · **`+0x62 CMP RSI,[세션+0xace8] · +0x69 JZ → +0x190`** | `hrgn == [세션+0xace8]` (세션의 특별 지역 핸들, `[공백: 어디서 쓰는지]`) | `+0x190 MOV ECX,0x57` → `UserSetLastError` (ret `+0x19d`) → **NULL** |
| 4 | `+0x6f TEST EBP,0xffdaf800 · +0x75 JNZ → +0x190` | `flags & 0xffdaf800` (3 은 0) | 위와 같이 `0x57` + NULL |
| 5 | `+0x7b TEST RBX · +0x7e JZ → +0x9f` | `hwnd = 0` → 지금 스레드의 바탕 창: `+0x9f PtiCurrent` (ret `+0xa4`) → `[pti+0x1f0]` (책상) → `[+8]` → `[+0x18]` = 바탕 창 포인터 `RDI` | **책상이 없으면 접근 위반** `[추측]` (`+0xa4 MOV RCX,[RAX+0x1f0]` 후 널 검사 없음) |
| 6a | `+0xb3 Feature_LearningModeExpansion…` (ret `+0xb8`) · `+0xc6 JZ → +0x15f` | **기능 켜짐** (`+0xcc CanPerformUIOperation(ppi, 1)` ret `+0xd3`) | `TEST AL · +0xda JNZ → +0x116` 통과. 아니면 `+0xe7 ValidateHwnd(바탕 창)` (ret `+0xee`) 도 0 이면 **원격 측정 한 줄만 (`+0x111`) 하고 그냥 계속** — NULL 아님 |
| 6b | `+0x15f HasUILimit(ppi, 1)` (ret `+0x166`) · `+0x16d JZ → +0x116` | **기능 꺼짐** + UI 한도 비트 0x1 (**job 의 `UILIMIT`** `[추측: 이름]`) 이 걸린 프로세스 | `+0x17a ValidateHwnd([바탕 창])` (ret `+0x181`) 0 이면 `+0x18b LEA ECX,[RAX+5]` → `UserSetLastError(5)` → **NULL** |
| 7 | `+0x131 ~ +0x13a _GetDCEx(RDI = 바탕 창, RDX = hrgn, R8D = flags)` (ret `+0x141`) | 위를 다 지나면 | 반환을 그대로 돌려줌 (`+0x146 MOV RBX,RAX`) |
| 8 | win32kbase `_GetDCEx` (`0x67600`): 캐시 목록 `[세션+0xe338]+0x18` 훑기 (`+0x4c5 ~ +0x52a`, 조건 `[DCE+0x30] & 0x400002 == 2`) → **없으면 `+0xa1c CreateCacheDC(창, (flags & 0x4000) \| 0x802, NULL)` (ret `+0xa21)** · `+0xa24 JZ → +0x2d5` | 캐시 DC 가 하나도 비어 있지 않음 (첫 부름은 거의 항상) | **`CreateCacheDC` 가 0 이면 `+0x2f1 XOR EAX,EAX` 로 0 반환** |
| 9 | `CreateCacheDC` (`0x14370`): `+0x33 Win32AllocPoolZInitImpl(크기 0x100 (`+0x30 LEA ECX,[RDX+0x68]`), 앞 0x98 B 를 0 으로, 태그 'Usdc')` (ret `+0x38`) · `+0x3e JZ → +0x2fe` · **`+0x83 GreCreateDisplayDC(hdev, 0 (직접 화면), 2)`** (ret `+0x88`) · `+0x90 JZ → +0x2f6` (풀 해제) | 세 번째 인자 (`_GetDCEx` 는 0 을 줌) 가 있으면 그 `[+0x50]`, 0 이면 **`[세션+0xe338]+0x30` (주 화면 장치)** (`+0x72 JZ → +0x310 ~ +0x327`) | 장치 NULL 이면 `GreCreateDisplayDC` 가 0 → **NULL** (`GreCreateDisplayDC +0x57 JZ → +0x40d` — 145 (1c)) |
| 10 | 성공: `+0x2b0 MOV RAX,[RBX+0x10]` | — | **`[DCE+0x10]` = HDC** 를 돌려줌 (`_GetDCEx +0x402` 도 `MOV RAX,[RDI+0x10]`). `_GetDCEx +0x2b9 GreSetDCOwnerEx(hdc, 0x80000002, 0, 0)` 가 0 을 주면 `+0x2c6 BTR` · `DestroyCacheDC` · **NULL** |

- **`UserGetDesktopDC` (`0x3bbec`) UI 한도** (`NtGdiOpenDCW(NULL…)` 길, 145 (1b) 끝줄): 종류 `0` (DC) 이고 일반 프로세스면 `+0xaa ~ +0xe6` 에서 **`_GetDCEx(바탕 창, 0, 0x80800003)`** (`+0x106`, ret `+0x10b`) 로 캐시 DC 를 받아 감 (책상 없으면 `+0xed TEST RCX · +0xf0 JZ → +0x1e5 MOV RBX,R12(0)` NULL). **기능 켜짐**: 세 번째 인자 ≠ 0 이고 종류 ≠ 2 일 때 `+0x129 CanPerformUIOperation(ppi, 1)` 가 거짓 · `+0x157 ValidateHwndEx` 가 0 이어도 **원격 측정만 (`+0x17e`) 하고 `+0x5f` 로 계속**. **기능 꺼짐** (`+0x188`): `+0x196 HasUILimit(ppi,1)` 가 참 · `+0x1d7 ValidateHwndEx` 0 일 때만 `+0x1e5 MOV RBX,R12` → **NULL**. 종류 `2` (IC) 는 `+0x1a6 CMP ESI,EBX · +0x1a8 JZ → +0x5f` 로 이 검사를 건너뜀.
- **기능 상태**: `Feature_LearningModeExpansion__private_featureState` (win32kbase `.data` rva `0x2b2938`) 의 **파일 속 처음 값은 `0x464d8`** `[objdump]` → `+0x16 TEST AL,0x10` 켜짐 · 값 비트 0 은 **0** → **꺼짐**이 처음 상태. 런타임에 기능 설정이 바꿀 수 있어 실제 값은 동적 (아래 VM). 어느 쪽이어도 **UI 한도가 없는 일반 프로세스는 위 6a · 6b 에서 막히지 않는다.**
- `CanPerformUIOperation(ppi, 1)` (`0x1a4110`) / `HasUILimit(ppi, 1)` (`0x1aa140`) = **`KfGetProcessEffectiveUILimits(프로세스)`** (ntoskrnl 내보내기) 결과가 0 이면 "할 수 있음" / "한도 없음". `(한도 & 1) == 1` 일 때만 걸린다.

## (C) 세션 기본 `DC_ATTR` 틀 — 칸 표

`[W32GetSessionState()+0x58]` = 세션 전역 구조 `G`. 틀 = `G+0x90` (0x1b0 B). **틀 오프셋 T = (G 안 오프셋) − 0x90.** `DCMEMOBJ` (`0x69a6c`) 가 틀 0x1b0 B 를 새 DC 의 `+0x220` 으로 복사 (`+0x74 ~ +0xec`) 하고 `[DC+0x3d0]` 에 그 위치를 둠 (`+0xec`), `[+0x98] = 0x12001f` (`+0x1b8`) 로 덮음. `GreCreateDisplayDC` 가 `HmgAllocateDcAttr` (유저 슬롯, `+0x205`) 를 받고 `SetupDCAttributes` (`0x100748`, `+0x28`) → `RestoreAttributes` (`0x1c710`, `+0x10 ~ +0x126`) 가 **앞 `0x160` B 를 유저 슬롯으로 복사**. 그래서 **유저 `DC_ATTR` 처음 값 = 아래 틀 값** (+ `+0x00` 비트 1, `+0x98`).

채우는 곳: **`InitializeGre` (`0x2f93fc`) `+0xb8 CALL InitializeDefaultDCAttributes`** (ret `+0xbd`) — `[W32GetSessionState()+0x58]` 는 같은 함수 `+0x9a`. 아래 "쓰는 줄" 은 `InitializeDefaultDCAttributes.asm` (rva `0x1914e8`).

| DC_ATTR 칸 (T) | 처음 값 | 쓰는 줄 | 읽는 쪽 · 뜻 `[추측: 이름]` |
|---|---|---|---|
| `+0x00` (byte) | 0, **비트 1 은 `GreCreateDisplayDC +0x243` 이 켬** (장치 == 주 화면 장치) | — | 145 (2b) |
| `+0x20` · `+0x2c` (float) | **`16.0f`** (`0x41800000`) — 둘 다 | `+0xbc`(`G+0xb0`) · `+0xc9`(`G+0xbc`) | 사본 비교 (`pcfLocateCFONT +0x2bf ~ +0x2e3`) · 뜻 `[추측]`: 28.4 고정소수 배율 (16 = 2^4) |
| `+0x40` (dword) | **`0x4b`** (비트 0 켜짐) | `+0xdd` (`G+0xd0`) | `pcfLocateCFONT +0x180` — 비트 0 이 **변환 계수 유효** 표시 |
| `+0x44` · `+0x50` (float) | `0.0625f` (1/16) 둘 | `+0xf4` xmm (`G+0xd4`) | 역배율 |
| `+0x64` · `+0x68` | `0x53` · `1` | `+0x12b` · `+0x14c` | `[공백: 뜻]` |
| `+0x88` (dword) | **`1`** | `+0x16b MOV [G+0x118],1` | **DPI 비율**: `GetDCDpiScaleValue` · `pcfLocateCFONT +0x112` 가 읽음. 나중에 `SetCachedDpiScaleValue` 만 바꿈 |
| `+0x98` (dword) | 틀 `0x10` → **DC 사본 `0x12001f`** | `+0x75 MOV [G+0x128],0x10` · `DCMEMOBJ +0x1b8` | 145 (2b) |
| `+0xa0` (qword) | **스톡 0 (WHITE_BRUSH) 핸들** (`0x800000` 비트 켜짐) | `bInitBrush +0xa0 MOV [G+0x130],핸들` — `bInitBRUSHOBJ +0x46` 이 `bInitBrush(0, 0xffffff, …)` 로 부름 | 붓 (145 (2b)) |
| `+0xa8` (qword) | **스톡 7 (BLACK_PEN) 핸들** | `bInitBRUSHOBJ +0x286 MOV [G+0x138],RAX` (바로 앞 `+0x278 bSetStockObject(…, 7)`) | 펜 |
| `+0xb0 +0xb4 +0xc0 +0xc4` | `0x00ffffff` (흰색) ×4 | `+0x40 · +0x46 · +0x4c · +0x52` | 배경 · 글 색 `[추측]` |
| `+0xd0` · `+0xd4` · `+0xe0` · `+0xe4` · `+0xe8` | `1` · `0x101020d` · `2` · `1` · `1` | `+0x7f ~ +0xa1` | `[공백: 뜻]` (배경 모드 · 그리기 모드 따위 `[추측]`) |
| `+0xf0` (dword) | **`0`** (ICM 꺼짐) | `+0xa8 MOV [G+0x180],EDX(0)` | 145 (2b) |
| `+0x100` · `+0x104` | `-1` · `-1` | `+0x5f · +0x65` | `[공백]` |
| `+0x108` · `+0x114` · `+0x118` · `+0x11c` · `+0x120` | **`0`** (안 씀) | — | ICM 정보 · 언어팩 칸 · 글 늘리기 |
| `+0x128` (qword) | **스톡 13 (SYSTEM_FONT) 핸들** | win32kfull `bInitStockFontsInternal +0x2f0 ~ +0x300` (`[스톡 글꼴 배열 `[G'+0xbc0]` +0x68]` → `[G'+0x1b8]`) · `FinishStockFontInit +0x18d ~ +0x198` 이 같은 칸에 다시 씀 | 글꼴. `[추측: 두 파일의 "G'" 가 같은 세션 전역]` — 오프셋 `+0x1b8 − 0x90 = +0x128` 이 DC_ATTR 의 글꼴 칸과 맞는 것으로 짐작 |
| `+0x154` (dword) | **`0x1802`** (`& 0xe000 = 0`) | `+0x161 MOV [G+0x1e4],0x1802` | `pcfLocateCFONT +0x170 TEST 0xe000` |
| `+0x13c +0x140 +0x14c +0x150` | `1` ×4 | `+0x153 · +0x15a` (qword `0x100000001`) | `[공백]` |
| `+0x160` (dword) · `+0x164` · `+0x170` (float `1.0f`) | `1` · `1.0f` · `1.0f` | `+0xb5 (G+0x1f0)` · `+0x124 xmm (G+0x1f4)` | **유저에 복사되는 `0x160` B 밖** — 유저 슬롯 `+0x160` 은 `HmgAllocateDcAttr +0x99` 가 0 으로 두고 **gdi32full 이 글꼴 사본 목록 머리로 씀** (`pcfLocateCFONT +0x58d ~ +0x5a3`) |
| `+0x174 ~ +0x194` | `0` · `0x63` · `16.0f` ×2 · `0` | `+0x10a · +0x115 · +0x135 · +0x13c · +0x146` | `[공백]` |

- **유저 슬롯 `0x170` B 에서 복사 안 되는 끝** (`+0x160 ~ +0x16f`) 은 `0` 으로 시작, **`+0x160` 은 사본 목록 머리** (유저가 씀) — 145 (2b) "`+0x160` 은 0" 의 뜻이 이것.
- 스톡 핸들 번호 = **스톡 번호 `i`** (`GetStockObject(i)` 의 `i`; 0 WHITE_BRUSH · 7 BLACK_PEN · 13 SYSTEM_FONT) — `[공유 메모리+0x1800b0 + 8*i]` 에 있는 값과 **같은 핸들** (`GetStockObject +0x65 ~ +0x7a`, 141 (2c)). `bSetStockObject (0x16bb10)` 가 핸들에 `BTS 0x17` (`0x800000`) 켠 값을 `G+0xbc0` 가 가리키는 배열의 `i` 번째에 씀 (`+0x33 · +0x47`); 글꼴 DPI 변종 (`10 · 13 · 16`) 은 `G+0xbc8` (`+0x79 ~ +0x80`). `G+0xbc0` 가 공유 메모리 `+0x1800b0` 와 같은 메모리인지는 포인터를 안 따라가서 `[추측: 오프셋 · 번호 일치]`.
- **에뮬레이터는 이 값들을 그대로 안 써도 된다** (유저 쪽이 값 검사를 안 함, 145). 다만 **`+0x88 = 1`** 은 `GetTextExtentPointW*` 가 사본 길을 가려면 꼭 필요 (사본 길을 안 쓰면 아무 값이나 — 0 이어도 되돌이로 지나감).

## (D) `GDIINFO` (`PDEV+0x848`) · `pGdiDevCaps` 블록 — 채우는 곳

### (D1) `pGdiDevCaps` ← `GDIINFO` (`vGetDeviceCaps`, win32kbase `0x14dd5c`)

`GreUpdateSharedDevCaps(PDEV)` (`0x14dd20`): `+0x1a ~ +0x1e MOV RDX,[세션G+0x6e0]` (블록의 커널 쪽 주소) → `+0x25 vGetDeviceCaps` (ret `+0x2a`). 부르는 곳: `InitVideo +0x1ba` (ret `+0x1bf`) · `SetDPIinSetup` · `DrvChangeDisplaySettingsInternal` · 람다 하나. `vGetDeviceCaps`: `g = [PDEV]+0x848` (`+0x1b ADD RDI,0x848`), `d` = 블록.

| `d` 칸 (= gdi32 `GetDeviceCaps` 가 블록 길에서 읽는 칸) | ← `g` (GDIINFO) 칸 · 식 | 줄 (읽기 ~ 쓰기) | `GetDeviceCaps` 지표 (145 (3)) |
|---|---|---|---|
| `+0x00` | `g+0x0` | `+0x31 ~ +0x33` | `0` DRIVERVERSION |
| `+0x04` | `g+0x4` | `+0x35 ~ +0x38` | `2` **TECHNOLOGY** |
| `+0x08` · `+0x0c` | `(g+0x8 + 500) / 1000` · `(g+0xc + 500) / 1000` (곱 `0x10624dd3` 의 윗 32 비트 >> 6) | `+0x3e ~ +0x4e` · `+0x51 ~ +0x5c` | HORZSIZE · VERTSIZE (여긴 syscall 길이라 안 씀) |
| `+0x10` · `+0x14` | `g+0x8` · `g+0xc` | `+0x5f ~ +0x62` · `+0x65 ~ +0x68` | — |
| `+0x18` · `+0x1c` | `g+0x10` · `g+0x14` (**가로 · 세로 해상도**) | `+0x6b ~ +0x6e` · `+0x71 ~ +0x74` | 8 · 10 은 syscall (`GreGetDeviceCapsInternal`, 145 (3a)) |
| **`+0x20`** | `g+0x18` (**비트 수**, 15 면 16: `+0x7d CMP 0xf · +0x82`) | `+0x77 ~ +0x7a` | `12` **BITSPIXEL** |
| **`+0x24`** | `g+0x1c` (판 수) | `+0x89 ~ +0x8f` | `14` **PLANES** |
| `+0x28` | `g+0x20` 이 `-1` 이면 `-1`, 아니면 `× 5` | `+0x92 ~ +0x9d` · `+0x172` | `18` NUMPENS |
| `+0x2c` | `PDEVOBJ::cFonts` (`0x14df20`) | `+0xa3 ~ +0xa8` | `22` NUMFONTS |
| `+0x30` | `g+0x20` | `+0xad ~ +0xb0` | `24` NUMCOLORS |
| **`+0x34`** | `g+0x24` (**래스터 능력**) | `+0xb3 ~ +0xb6` | `38` **RASTERCAPS** |
| `+0x38` · `+0x3c` · `+0x40` | `g+0x40` · `g+0x44` · `g+0x48` | `+0xc5 ~ +0xc8` · `+0xcb ~ +0xce` · `+0xd1 ~ +0xd4` | `40 · 42 · 44` (syscall 길) |
| `+0x44` · `+0x48` | `g+0x28` · `g+0x2c` (**LOGPIXELS**) | `+0xd7 ~ +0xda` · `+0xdd ~ +0xe0` | `88 · 90` (syscall 길) |
| **`+0x4c`** | `g+0x68` | `+0xe3 ~ +0xe6` | `104` **SIZEPALETTE** |
| **`+0x50`** | `g+0x34 + g+0x38 + g+0x3c` (DAC 비트 합) | `+0xe9 ~ +0xf2` | `108` COLORRES |
| `+0x54` · `+0x58` · `+0x5c` · `+0x60` | `g+0x60` · `g+0x64` · `g+0x58` · `g+0x5c` | `+0xf5 ~ +0xf8` · `+0xfb ~ +0xfe` · `+0x101 ~ +0x104` · `+0x107 ~ +0x10a` | 110 · 111 · 112 · 113 PHYSICAL |
| `+0x64` | `g+0x30 \| 0x1807` (`g+0x4 ≠ 0` 이면 `\| 0x4000`) | `+0x10d ~ +0x115` · `+0x118 ~ +0x121` | `34` TEXTCAPS |
| `+0x68` | `g+0xf8` | `+0x124 ~ +0x12a` | `116` VREFRESH |
| **`+0x6c`** · **`+0x70`** | `g+0x10` · `g+0x14` | `+0x12d ~ +0x130` · `+0x133 ~ +0x136` | `118` **DESKTOPHORZRES** · `117` DESKTOPVERTRES |
| `+0x74` | `g+0xfc` | `+0x139 ~ +0x13f` | `119` BLTALIGNMENT |
| `+0x88` | `g+0x130` | `+0xb9 ~ +0xbf` | `120` SHADEBLENDCAPS |
| `+0x8c` | `GetColorManagementCapsWrap` (기능 있을 때, `+0x142 ~ +0x153`) 아니면 0 | `+0x15a` | `121` COLORMGMTCAPS |

### (D2) `GDIINFO` 를 누가 채우나

- **`StubDispEnablePDEV` (`0x18c420`)**: 로컬 0x140 B 비우고 `+0x33 bInitGdiInfo(DEVMODE, GDIINFO)` (ret `+0x38`) → 결과를 드라이버가 준 GDIINFO 칸에 `memmove` (`+0xaa`, 크기 `min(0x140, 호출자 크기)`) · 가짜 팔레트 `EngCreatePalette` (`+0xdb`, 마스크 `0xff0000/0xff00/0xff`). **연결이 끊긴 세션용 가짜 화면** `[추측: 이름 `StubDisp` 과 `StubDispGetModes` 의 `UserGetDisconnectDeviceResolutionHint`]` — `StubDispGetModes` (`0x1ef590`) 는 모드를 `0xdc` B DEVMODE 로 만들고 이름 `"StubDisp"`, 기본 **`0x400 × 0x300` (1024 × 768)**, 힌트가 있으면 (`> 0x320 × 0x258`) 그 해상도 (`+0x6b ~ +0xb2`), `dmFields = 0x7c0000`, `dmBitsPerPel = 0x20`, `dmDisplayFrequency = 0x3c`, `dmLogPixels` 는 표 `0x14027ff30` `[공백]`.
- **`bInitGdiInfo` (`0x1b6038`)** 가 쓰는 값 (`.asm +0x20 ~ +0x16f`): `ulVersion = 0x4000` · `TECHNOLOGY = 1` · `HORZRES/VERTRES = DEVMODE.dmPelsWidth/Height` (`[DEVMODE+0xac / +0xb0]`; **셋 다 0 이고 비트 수 · 주파수도 0 이면 `0x400 · 0x300`**) · `BITSPIXEL = 0x20` · `PLANES = 1` · `NUMCOLORS = -1` · `LOGPIXELSX/Y = DEVMODE.dmLogPixels` (`WORD [+0xa6]`) · `flTextCaps = 0x2000` · DAC 비트 `8 · 8 · 8` (`+0x34/38/3c`) · `ASPECTX/Y = 0x24` · `ASPECTXY = 0x33` · `VREFRESH = 0x3c` (`+0xf8`) · `BLTALIGNMENT = 1` (`+0xfc`) · 그 밖 상수. **`DEVMODE` 에 비트 수 ≠ 32 이거나 주파수 ≠ 60 이면 0 을 돌려줘 실패** (`+0x58 ~ +0x72 → +0x171`). `HORZSIZE` · `VERTSIZE` · `RASTERCAPS` (`g+0x24`) · `SIZEPALETTE` (`g+0x68`) · `PHYSICAL*` 는 이 함수가 **안 채운다 (0)**.
- **진짜 화면 (WDDM)**: `PDEVOBJ` 생성자 (`hdcOpenDCW +0x278`, `0xfa080`) → `EnablePDEV` (`0xfae04`, 137 B, `+0x7e` 에서 드라이버 `DrvEnablePDEV` 를 **간접 (`indirect:cfg`) 호출**) 가 드라이버가 준 값으로 채움; 여러 화면은 `MulEnablePDEV` (`0x15b170`, `vSpEnableMultiMon` `+0x435`). **어느 드라이버가 이 타깃 VM 의 GDIINFO 를 채우는지는 정적으로 못 정함 `[공백]`** → VM 에서 `GetDeviceCaps` 값 표를 직접 뜸 (아래).
- `GetDeviceCaps` 지표 `0x58 · 0x5a` (LOGPIXELS) 는 블록이 아니라 `GreGetDeviceCapsInternal` 이 `g+0x28 / +0x2c` + DPI 문맥으로 계산 (145 (3a)) — **DPI 문맥 칸 쓰는 곳** 은 한 줄 답 4 ②.

### (D3) 블록 쪽 나머지

- 블록 `+0x94` (**언어팩 비트**) = `InitializeGre +0x4ac MOV [shared+0x180094],R14D(0)` 으로 0 으로 비운 뒤 `+0x4b8 RtlQueryRegistryValues(RelativeTo = 0x80000003 (제어 키 + 선택), 경로 `LanguagePack`, 콜백 `LpkShapeQueryRoutine`)`: **`\Registry\Machine\System\CurrentControlSet\Control\LanguagePack` 아래 모든 값**에 대해 콜백이 불리고, **값이 `REG_DWORD`(4) 면 `[+0x180094] \|= 1 << 값`** (`LpkShapeQueryRoutine +0x22 ~ +0x32`). 키가 없거나 DWORD 값이 없으면 0.
- 블록 `+0x98` = `InitializeGre +0x4cb MOV [shared+0x180098],1` — **1.** `pcfCreateCFONT +0x44 ~ +0x5f` 가 사본에 복사.
- 유저 쪽 의미: `GdiProcessSetup +0x161 CMP [PEB+0xf8 값 +0x180094],0 · +0x168 JZ` → 0 이면 언어팩 초기화 (`GdiInitializeLanguagePack +0x5c gbLpk = 1`) **건너뜀** → `gbLpk == 0` → 글 폭 길에서 `DC_ATTR+0x114` · `CheckUpdateNLSScriptSettings` · `FontHasWesternScript` 를 **안 본다.**

## (E) 필터 값 5 갈래 표

`[정적]` 값을 읽는 곳: gdi32 `0x25780` (처음 0) · user32 `0xd9970` (**처음 `7` = 아직 안 알아봄**, `ClientThreadSetup +0x14 CMP 7` · `_UserClientDllInitialize +0x42c CMP 7` 이 `GetProcessMitigationPolicy` 를 부르고 `CMPXCHG` 로 한 번만 저장 — 실패하면 `7` 그대로 = 5 아님).

| 곳 | 줄 | 5 일 때 |
|---|---|---|
| user32 `GetDC` (`0x4a8c0`) | `+0x0 CMP · +0x7 JZ → +0x15` | 인자 무시하고 **`RAX = -0x2321b000` (`0xFFFFFFFFDCDE5000`)** 반환, `NtUserGetDC` **안 부름**. 아니면 `+0x9 JMP [NtUserGetDC]` |
| user32 `ReleaseDC` (`0x4780`) | `+0xe CMP · +0x15 JZ → +0x43` | `+0x43 CMP RBX,-0x2321b000 · +0x4a JNZ → 보통 길` — 가짜 HDC 면 **1 반환, syscall 없음**. `hdc == 0` 이면 0 (`+0xc JZ → +0x3a`) |
| gdi32 `GdiReleaseDC` → `IsPseudoDesktopDcForFilteredProcess` (`0x1620`, 27 B) | `+0x0 CMP · +0x7 JZ → +0xd · +0xd CMP RCX,-0x2321b000` | 가짜 HDC 면 1 — `GdiReleaseDC` 가 이걸 쓴다 (`.c` 머리 `callers: GdiReleaseDC`) |
| gdi32 `GetDeviceCaps` (`0x17f0`) | `+0x47 CMP g_systemCallFilterId,5 · +0x53 JZ → +0x419 · +0x419 CMP R9,-0x2321b000 · +0x420 JZ → +0x1e8 (블록 길) · +0x426 JMP → +0x59` | **가짜 HDC 만** 표 검사 없이 `pGdiDevCaps` 블록으로 감 (145 (3) 의 표로 읽음, 읽는 칸 `+0x88 ~` 까지). 다른 HDC 는 보통 길. **5 가 아닐 때 그 값을 넣으면** `+0x59 AND 0x7f0000 → +0x66 JNZ` 로 `pldcGet` 길 (0) |
| gdi32 `D3DKMTOpenAdapterFromGdiDisplayName` (`0x2140`) | `+0x6 CMP · +0x10 JZ → +0x83 MOV EAX,0xc000001c` | `STATUS_INVALID_SYSTEM_SERVICE` 반환 |
| user32 `_UserClientDllInitialize` | `+0x487 CMP · +0x48e JNZ → +0x568` (5 가 아니면 이쪽), 5 면 `+0x494 RtlAllocateHeap(pUserHeap, 0, 0x498)` + 디스패치 표 복사 (`+0x4c3 ~`) · `+0x7de CMP` → `+0x7e7 CleanupWinpropManager` ×2 (`+0x7ec`, `+0x7f1`) | 이 모드 전용 **창 속성 · 메시지 처리를 user32 안에서** (커널 안 부름) 하는 초기화 · 정리 |
| user32 그 밖 (`refs` 25 곳) | `GetWindowData +0xe5 · +0x1e2` · `GetMessageW +0x27` · `GetMessageA +0x4d` · `_PeekMessage +0x65` · `SendMessageWorker +0x116` · `CheckAndRunLocalHooks +0x6` · `FilterProcessWindowMessageHook +0x14` · `CallNextHookEx +0x4a` · `SetWindowsHookExAW +0x5a` · `UnhookWindowsHookEx +0x4` · `_SetWindowLongPtr +0x6a/+0x14d/+0x1c2` · `SetPropA/W +0x1c` · `GetPropA +0x2d` · `GetPropW +0x116` · `RemovePropA/W +0xf` · `UnregisterClassA +0x2e` · `UnregisterClassW +0x28` · `RegisterIMEClass +0x4a` | 각각 5 일 때 **커널 syscall 대신 유저 쪽 대체 길** (표 칸 읽지 않음). 이 요청은 줄 위치만 센 것 — 본문은 sast/151 (user32) 몫 `[공백]` |

- **값 5 의 출처 정리**: user32 · gdi32 → kernelbase `GetProcessMitigationPolicy(정책 0xb)` (`[objdump]` `0x216f0`: `+0x77` `cmp edx,0x12 jbe` 범위 검사, 점프표 `[0x180000000 + 정책*4 + 0x21afc]` → 11 번 `+0xb` · `+0x4 크기`) → `NtQueryInformationProcess(자기, 0x34, {정책 `0xb` @+0, 값 @+4}, 8, 0)` → ntoskrnl `NtQueryInformationProcess +0x1d38 ~ +0x1d53` (`PsGetWin32KFilterSet()` = `gs:[0x188]→[+0xb8]→[+0x6e8]`, 하위 4 비트를 출력 `[+4]` 의 하위 4 비트에 섞음) ← `EPROCESS+0x6e8` ← `PspAllocateProcess +0x1f0e MOV ECX,[속성+4] · +0x1f11 MOV [R14+0x6e8],ECX` (같은 곳 `+0x1ed6 ~ +0x1f00` 이 `[속성]` 의 한 비트 (`TEST R13B,AL`) · 비트 1 에 따라 `[EPROCESS+0x750] \|= 0x4000 / 0x8000` — 필터 켜짐 · 감사 `[추측: 이름]`).
- 에뮬레이터: **`NtQueryInformationProcess(…, 0x34, …)` 기본 대답이 "성공 + 0 채움" 이든 "실패" 든 5 는 안 나온다** — 5 를 줄 이유가 없음.

## (F) N2 — gdiplus 시작 길 `+0x62b` 이후의 레지스트리 · 파일 · syscall

`[정적]` `InternalGdiplusStartup` (gdiplus `0xe1170`) 줄. (145 표 (4) 다음 칸.)

| 차례 (`+0x…`) | 부르는 것 | 읽는 레지스트리 · 파일 · 경로 | syscall (뒤에 숨은 것) | NULL 가지 |
|---|---|---|---|---|
| `+0x60b` | `InitializeForGdiBitmap(g_pRemoteSurface, …)` | — | `ExtractPixelFormatFromHDC` 한 번 더 (145 (4)) | 없음 |
| `+0x62b` | **`InitImagingLibrary(깃발)`** (`0xe8e98`) | **없음** | 없음 (`InitializeCriticalSection` = 유저 쪽 구조 초기화) | **없음** (항상 1 — `+0x28 ~ +0x32 MOV EAX,1`; 깃발 비트 1 → `SuppressStoreCodecs`, 비트 0 → `SuppressDesktopCodecs` 전역) |
| `+0x638 ~ +0x6cb` | `SystemColors` 16 칸에 고정 색 (`0 · 0xffffff · 0xc0c0c0 …`) | — | — | — |
| `+0x6d5` | **`SysColorNotify`** (`0x92e0c`) | — | **없음** — user32 `GetSysColor(0x10 · 0xf · 0x14 · 1)` = `[gpsi + 지표*4 + 0x11d8]` (`GetSysColor +0x9 ~ +0x13`, `+0x4 CMP ECX,0x1e`) → **`gpsi` 가 0 이면 접근 위반** · 끝에 `VGAHashRebuildTable` (꼬리 점프, 할당만) | 없음 |
| `+0x6e1 ~ +0x723` | `GetModuleHandleW("gdi32.dll")` (문자열 rva `0x18d2a8`) · `GetProcAddress("ExtTextOutW")` (`0x18d2c0`) · `GetProcAddress("GdiIsMetaPrintDC")` (`0x18d2d0`) | 이름만 (로더 목록 조회) | 없음 (로더 유저 쪽, 이미 올라온 모듈) | 안 봄 |
| `+0x72a` | `GetSystemDefaultLCID` → `Globals::LanguageID` | 시스템 기본 로케일 (kernelbase, 정적으로 안 읽음) | `[공백: kernelbase 쪽 — 이 요청에서 안 열었다]` | 안 봄 |
| `+0x73d` | **`InitSystemFontsDirs`** (`0xbe3c4`) | **경로 이름만** — `+0x3c GetSystemDirectoryW(버퍼, 0x105)` (ret `+0x43`) → 마지막 `\` 앞까지 (`+0x57 ~ +0x71` 뒤에서 앞으로 찾기) + 14 바이트 상수 (`"\fonts"` 소문자 + NUL, rva `0x18a800`, `+0xed ~ +0x103` 이 복사) → `FontsDirW` (`GpMallocEx` `+0x96`) · `WideCharToMultiByte` ×2 (`+0x108` · `+0x154`) → `FontsDirA` | 없음 (`GetSystemDirectoryW` = kernelbase 가 가진 문자열 복사) | `+0x742 TEST EAX · +0x744 JZ → +0x7d1`: 시스템 폴더 길이 0 이거나 `> 0x103` 이거나 `\` 없음 (`+0x51 JA · +0x75 JZ`) 이거나 할당 실패면 0 → 시작 실패. **파일은 안 연다** |
| `+0x74a ~ +0x76f` | `TextCriticalSection` 초기화 · 전역 비움 | — | — | — |
| `+0x775` | **`GetGpInstalledFontCollection`** (`0x3465c`) | **없음.** `GpMallocEx(0x18)` → `GpInstalledFontCollection` 생성자 (`0xc3848`): `GpMallocEx(0x18)` → `GpFontTable` 생성자 (`0xc38b4`, `HeapAlloc(GpMemHeap, 0, 0x10)`) — **빈 표만** (`[표+8] = 0` 이면 지워서 NULL, `+0x49 CMP · +0x4e JNZ`) | 없음 (힙 할당) | `+0x781 JZ → +0x7d1` (표 만들기 실패) |
| `+0x78b ~ +0x7a8` | `GpMallocEx(0x10)` → `DWriteFontCacheLastRecentlyUsedList` | — | — | `+0x7a8 JZ` |
| `+0x7ad ~ +0x7ba` | **`BackgroundThreadStartup`** (`0xc58ac`) — `[입력+0x10] == 0` (= `SuppressBackgroundThread` FALSE) 일 때만 | **경로: `AddRefModule` (`0xc5a44`) 가 `GetModuleFileNameA(Globals::DllInstance, 버퍼, 0x104)` (`+0x39`, ret `+0x40`) → `LoadLibraryExA(그 경로, 0, 0)` (`+0x58`, ret `+0x5f`) 으로 자기를 한 번 더 올림 (이미 올라와 있어 목록 조회 + 참조 증가 `[추측: 새 파일 안 연다]`). 경로가 0 글자거나 `> 0x103` 이면 (`+0x45 ~ +0x4c`) 실패 → 시작 실패** | **`CreateEventW(0, 1, 0, 0)` ×2 (`+0x16` ret `+0x1d` · `+0x36` ret `+0x3d`) = `NtCreateEvent` ×2 (수동 · 꺼짐)** · `GetProcessHeap` + `HeapAlloc(0x18)` (`CreateThreadAndAddRefDll +0x27 · +0x3c`) · **`CreateThread(0, 0, 진입 `DllRefCountSafeThreadThunk` (rva `0xd77a0`), 인자 블록, 0, &ThreadId)`** (`+0x85`, ret `+0x8c`) = `NtCreateThreadEx`. 껍데기가 인자 블록의 `[+0]` 함수 (= `BackgroundThreadProc` rva `0xa52b0`) 를 부르고 `FreeLibraryAndExitThread` (`+0x31`) | **`+0x28 JZ → +0x8f`** (첫 이벤트 NULL → `BackgroundThreadShutdown` + 0) · `+0x48 JZ → +0xb4` (둘째 NULL → 첫 번째 `CloseHandle` 하고 `+0x8f`) · `+0x8d JNZ → +0xa2` 가 안 타지면 (스레드 핸들 NULL) `+0x8f` → 0 → `InternalGdiplusStartup +0x7ba JZ → +0x7d1` |
| `+0x7bc` | `TraceLoggingRegisterEx_EventRegister_EventSetInformation` | ETW 공급자 등록 (`EventRegister` → `NtTraceControl` 따위) `[공백]` | 있음 `[공백]` | 반환 안 봄 |

### (F2) 새 스레드 안 — `BackgroundThreadProc` (gdiplus `0xa52b0`) · `InternalNotificationStartup` (`0xa53d4`)

`[정적]` `GdiplusStartup` 이 돌아온 **뒤에 따로 도는** 스레드의 일 (gdiplus `_calls.tsv`). 이 요청이 읽은 이유: **`GetDC` · `SelectPalette` 가 여기서 나온다** (필터 5 · DC 칸과 직결).

| 줄 | 부르는 것 | 뜻 |
|---|---|---|
| `BackgroundThreadProc +0xd` (ret `+0x14`) · `+0x25 InternalNotificationStartup` (ret `+0x2a`) · `+0x33` | `EnterCriticalSection` · 알림 창 만들기 · `LeaveCriticalSection` | 시작 알림 |
| `InternalNotificationStartup +0x83 RegisterClassW` (ret `+0x8a`) | `NtUserRegisterClassExWOW` | 숨은 창 클래스 (이름은 `+0x1ab StringCchPrintfW` 로 만듦 — 모듈 파일 이름 `+0x11b GetModuleFileNameW`) |
| `+0x1be RegisterWindowMessageW` (ret `+0x1c5`) → `Globals::g_nAccessibilityMessage` | `NtUserRegisterWindowMessage` | 접근성 메시지 번호 |
| **`+0x211 CreateWindowExW(…)` (ret `+0x218`)** → `Globals::HwndNotify` (`+0x21d`) · `+0x224 TEST RAX · +0x227 JZ → +0x35e` | `NtUserCreateWindowEx` | **NULL 이면 `+0x35e InternalNotificationShutdown` 로 정리하고 끝** |
| `+0x22d IsUIAccessProcess` (ret `+0x232`) · **참이면** (`+0x234 JNZ → +0x3b8`) `+0x3db SetWindowPos(창, -1 (TOPMOST), 0, 0, 0, 0, 0x1b)` (ret `+0x3e2`) 하고 `+0x23a` 로 돌아옴 | `NtUserSetWindowPos` | UI 접근 프로세스일 때만 맨 위 창으로 |
| **`+0x291 CreatePalette(LOGPALETTE 0x404 B, 버전 0x300 · 256 칸)`** (ret `+0x298`) → **`+0x2a7 GetDC(HwndNotify)`** (ret `+0x2ae`) → **`+0x2bf SelectPalette(hdc, hpal, 0)`** (ret `+0x2c6`) → `+0x2d5 ReleaseDC(HwndNotify, hdc)` (ret `+0x2dc`) → `+0x2e4 DeleteObject(hpal)` (ret `+0x2eb`) | `NtGdiCreatePaletteInternal` · `NtUserGetDC` (필터 5 면 가짜 HDC, 표 (E)) · `NtGdiSelectPalette` · `NtUserReleaseDC` · `NtGdiDeleteObjectApp` | 팔레트를 한 번 골랐다 풀어 봄. 반환은 하나도 안 봄 (NULL 이어도 지나감 `[정적: TEST 없음]`) |
| `+0x422 SetEvent` (ret `+0x429`) · `+0x431 CloseHandle` (ret `+0x438`) | `NtSetEvent` · `NtClose` | `ThreadNotify` 로 시작 알림 |
| `BackgroundThreadProc +0x74 MsgWaitForMultipleObjects` (ret `+0x7b`) · `+0x99 PeekMessageW` (ret `+0xa0`) · `+0xf8 TranslateMessage` · `+0x109 DispatchMessageW` | `NtUserMsgWaitForMultipleObjectsEx` · `NtUserPeekMessage` … | **이후 이 스레드는 메시지 고리에서 영영 기다린다** (`ThreadQuitEvent` 가 신호를 받을 때까지) |

- **레지스트리 · 파일을 읽는 줄은 이 구간에 없다.** 글꼴 레지스트리 (`HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts` 등) · 글꼴 파일은 **`GpInstalledFontCollection` 이 처음 쓰일 때 (`GdipNewInstalledFontCollection` · `GdipCreateFontFamilyFromName` …)** 의 일 — 시작 길 밖 (다음 요청 후보).
- `GdiplusStartupInput` 인자 영향: `SuppressBackgroundThread = TRUE` 면 마지막 칸 전체를 건너뜀 (스레드 · 이벤트 없음). `SuppressExternalCodecs` 는 `InitImagingLibrary` 깃발로만.

## (G) 4 번 — win32k 몫 2 · 4 · 6 줄 근거

### (G1) `GrepCreateCompatibleDC` (win32kbase `0x14950`) — `CreateCompatibleDC` 의 커널 쪽

| 줄 `[정적]` | 하는 일 |
|---|---|
| `+0x28 MOV RDX,[RCX+0x68] · +0x35 TEST · +0x38 JZ → +0x1f9` | 원본 DC 핸들이 있나 (`XDCOBJ` 의 `+0x68`) — 없으면 **원본 NULL 길** (`CreateCompatibleDC(0)`) |
| `+0x50 vLock` (ret `+0x55`) · `+0x64 CMP word [객체+0xc],1 · +0x68 JNZ → +0x2d6` (원격 측정 한 줄) | 원본 DC 잠그기 · 잠금 수 확인 `[추측]` |
| `+0x111 ~ +0x126 GreCreateDisplayDC([RCX+0x30], 1, 0)` (ret `+0x12b`) | **원본 DC 의 `[+0x30]` (= `PDEV`) 로 종류 `1` (메모리 DC)** 를 만듦. 0 이면 `+0x131 JZ → +0x19b` 로 NULL |
| `+0x13a APIDCOBJ(새 DC)` · `+0x14d MOV R8D,[원본+0x208] · +0x154 TEST BL,R8B · +0x157 JNZ → +0x29d` → `+0x2a4 vSetDpiScaling(새 DC, [원본+0x20c])` (ret `+0x2a9`) | 원본이 **DPI 비율 켜짐** (`[+0x208] & 1`) 이면 새 DC 도 같은 비율 (`vSetDpiScaling +0x6 OR [DC+0x208],5`, `+0x14 [DC+0x20c] = 비율`, `vUpdateCachedDPIScaleValue` 로 `DC_ATTR+0x88` 갱신) |
| `+0x15d MOV RAX,[원본+0x3d0] · +0x164 MOV R8D,[RAX+0x6c] · +0x168 TEST R8B,7 · +0x16c JNZ → +0x290` → `+0x293 dwSetLayout(새 DC, -1)` (ret `+0x298`) | 원본 `DC_ATTR+0x6c` 하위 3 비트 (레이아웃) 가 있으면 새 DC 도 같은 레이아웃 |
| `+0x1f9 ~ +0x25b` (원본 NULL): `ReEnterLeaveCrit` · `+0x21b W32GetUserSessionState` · **`+0x227 MOV RCX,[세션+0xe338] · +0x22e MOV RSI,[RCX+0x30]` (주 화면 장치)** · 기능 검사 · `+0x256 GreCreateDisplayDC(RSI, 1, 0)` (ret `+0x25b`) | `CreateCompatibleDC(0)` = **주 화면 장치로 메모리 DC** (UI 한도 검사 결과 `HasUILimit` 는 **안 쓴다**, `+0x249` 뒤 바로 `+0x24e XOR R8D`) |
| `GreCreateDisplayDC` 종류 `1` 분기: `+0x14c CMP R14D,1 · +0x14f JNZ → +0x335` (아니면 건너뜀), 종류 1 이면 `+0x155 ~ +0x17e` | `[DC+0x200] = 0x1_00000001` (기본 비트맵 1 × 1) · `[DC+0x40] = [G]+0x478` (기본 면) · `bSetDefaultRegion` (`+0x187`) |
| `DC_ATTR+0x00` 비트 0 (DIB 선택됨) | **화면 · 메모리 DC 모두 처음 0** (틀 `+0x00` = 0, 켜는 줄은 커널의 `SelectBitmap` 쪽 `[공백]`). 비트 1 은 `GreCreateDisplayDC +0x231/+0x243` (장치 == 주 화면 장치) |

`GreCreateCompatibleDC` (`0x14880`) 는 이 함수를 감싸는 껍데기 (45 줄, 잠금 · 해제 `PopThreadGuardedObject`).

### (G2) DPI 칸이 써지는 곳 (145 "win32k 몫" 4)

| 칸 | 쓰는 곳 | 읽는 곳 (`GreGetDeviceCapsInternal`, 145 (3a)) |
|---|---|---|
| `[W32PROCESS+0x10c]` (dword) | `NtUserSetProcessDpiAwarenessContext` (`0x62350`) `+0xc4 MOV [RDI+0x10c],ESI` (`RDI = [pti+0x1d0]`) · 같은 줄 앞 `+0xb5 [RDI+0x330] \|= 0x400000` ("한 번 정함" 표시, 이미 켜져 있으면 `+0x1a7` 에서 `EBP ≠ 0 · EBX ≠ 0 · [+0x10c] == 0x12` 일 때만 다시) · 검사 `IsValidKernelDpiAwarenessContext` (`+0x73`) · 인식 종류 `ESI & 0xf == 1` 이면 `(ESI >> 8) & 0x1ff == [W32PROCESS+0x110]` 이어야 (`+0x168 ~ +0x19f`) | 스레드 칸이 0 이면 이 칸 (`+0xec`). 처음 값 (프로세스 만들 때) 은 이 요청이 안 읽은 곳 `[공백]` |
| `[W32PROCESS+0x110]` (word, **시스템 DPI**) | `InitUserScreen +0x396 MOV [RCX+0x110],BX` (모든 프로세스 목록 순회: `[세션+0x92c0]` 에서 `+0x180` 링크), `BX = [G+0x438]` (word) → 같은 곳에서 `[psi+0x1b56] = BX` · `[psi+0x1b66] += BP` · `SetDPIinSetup +0x492 MOV [RBX+0x110],AX` (`AX = [psi+0x1b56]`, 현재 프로세스) | 문맥 DPI 비트가 0 이고 `GDIINFO.+0x2c` 뒤에 `+0x110 ≠ 0` 이면 그 값 (`+0x11a ~ +0x126`) |
| `[스레드+0x190]` (dword, 문맥) · `[스레드+0x198]` (유저 쪽 복사 포인터) | **`W32SetCurrentThreadDpiAwarenessContext` (`0x66a00`) `+0x6d MOV [RAX+0x190],EDI`** (`EDI = 인자 < 0 ? 0 : 인자`) · `+0x98 MOV [RCX],EAX` (`+0x198` 가 가리키는 유저 칸에 복사) · 반환은 이전 값 (0 이면 프로세스 `+0x10c \| 0x80000000`, `+0xca ~ +0xd0`) | 있으면 이 값 (`+0xc8`) |
| `DC_ATTR+0x88` (비율) | **`SetCachedDpiScaleValue` (`0x13140`) `+0xd4 MOV [RAX+0x88],EDI`** (커널 사본) · `+0x97 MOV [RAX+0x88],EDI` (유저 쪽 `GreDecodeUserModePointer([표 항목+0x10])`) — 부르는 쪽 `vUpdateCachedDPIScaleValue` (`0x13e24`): `bDpiScaleTransform()` (`0xb8980`) 가 0 이면 **1** (`+0x16 ~ +0x1c`) | 유저 `GetDCDpiScaleValue` (`+0xe0`) |
| `[DC+0x208]` · `[DC+0x20c]` | `vSetDpiScaling +0x6 OR [RCX+0x208],5` · `+0x14 MOV [RCX+0x20c],RDX` / `vClearDpiScaling +0x14 ~ +0x2e` (`& ~3 \| 4`, `[+0x20c] = 0`) | `GrepCreateCompatibleDC +0x14d` |

`GreGetDeviceCapsInternal +0x1a9 ~ +0x1e4` (`EngMulDiv` 로 줄이는 갈래, `[PDEV+0x990]`) 은 이번에도 안 읽음 — DPI 100% 가정 `[공백]`.

### (G3) 주 화면 판정 `[세션+0xe338]+0x30`

| 줄 | 하는 일 |
|---|---|
| `InitVideo` (win32kbase `0x1d0888`) `+0x11a DrvSetDisplayConfig(…)` (ret `+0x11f`) → `+0x162 MOV RAX,[RSP+0xc0] · +0x16a MOV RBX,[RAX]` (목록의 **첫 장치**) → **`+0x179 MOV RCX,[세션+0xe338] · +0x180 MOV [RCX+0x30],RBX`** | 주 화면 장치 |
| `+0x198 ~ +0x19f MOV [RCX+0x10],목록` · `+0x1ba GreUpdateSharedDevCaps([RCX+0x30])` (ret `+0x1bf`) · `+0x1bf InitUserScreen` | 같은 목록 · 블록 갱신 · 화면 만들기 |
| `SetDPIinSetup` (`0x190570`) `+0x3de ~ +0x3e5 MOV [RCX+0x30],RBX` | DPI 가 바뀔 때 (기본 설정 중) 다시 씀 |
| 읽는 쪽: `GreCreateDisplayDC +0x231 CMP RDI,[RCX+0x30]` · `_GetDCEx +0x5f ~ +0x66` · `CreateCacheDC +0x310 ~ +0x327` · `GrepCreateCompatibleDC +0x22e` · `UserGetDesktopDC +0x3c ~ +0x43` | 같은 장치면 `DC_ATTR+0x00` 비트 1, 아니면 안 켬 |

### (G4) `hdcOpenDCW` (win32kbase `0x3a260`) — 145 (1b) 끝줄 뒤

`NtGdiOpenDCW` 1 째 인자 (이름) 가 NULL 이 **아닐 때** `+0x171 hdcOpenDCW` 로 옴. 아래 "하는 일" 은 호출 이름에서 읽은 윤곽 `[추측]` — 호출 줄 · 오프셋은 `_calls.tsv` 와 같다.

| 줄 `[정적]` | 하는 일 |
|---|---|
| `+0x68 RtlInitUnicodeString` (ret `+0x6f`) · `+0xad EnterCritInternal` (ret `+0xb2`) · `+0xd6 GreAcquireSemaphoreCommon` (ret `+0xdb`) | 이름 읽기 · 임계 구역 · 장치 목록 잠금 |
| `+0xfa DrvGetHDEV(이름)` (ret `+0xff`) → 있으면 `+0x117 UserGetMonitorDC` (ret `+0x11c`, win32kbase `0x67428`) | 화면 장치 이름 (`\\.\DISPLAYn`) 이면 **그 모니터의 DC** |
| `+0x18a DrvGetHDEV` 둘째 (ret `+0x18f`) · `+0x2ba GreCreateDisplayDC` (ret `+0x2bf`) | 화면 장치를 찾았을 때 `GreCreateDisplayDC(hdev, 종류, …)` |
| `+0x1c7 ~ +0x215` (`W32GetWin32kBaseApiSetTable` 표 `+0x5e8`, `+0x5f0` 간접 호출) · `+0x278 PDEVOBJ(LDEV, DEVMODE, …, GDIINFO, …)` (ret `+0x27d`) | 못 찾으면 **새 PDEV** (프린터 · 그 밖 드라이버) 를 지음 |
| `+0x426 DrvCreateMDEV` (ret `+0x42b`) · `+0x470 DrvCreateMDEV` (ret `+0x475`) · `+0x43d UserGetCurrentDesktopId` · `+0x492 GreCreateDisplayDC` (ret `+0x497`) · `+0x4ab DrvDestroyMDEV` (ret `+0x4b0`, 실패 시) | 여러 장치 묶음 (`MDEV`) 만들고 DC |
| `+0x3d0 EngSetLastError` (ret `+0x3d5`) · `+0x3e3 IsUMPD_ldevUnloadImageSupported` · `+0x3f3 UMPD_ldevUnloadImageWrap` | 실패 정리 (사용자 모드 프린터 드라이버 내리기) |

`[공백]`: 프린터 길의 뒷부분 · `\\.\DISPLAYn` 이 실제로 어느 `DrvGetHDEV` 결과가 되는지는 이번에 안 이었다 (145 가 "범위 밖" 으로 둔 것과 같다).

## (H) PE_emulator 쪽 — 지금 맞나 · 틀리나 (145 "PE_emulator 쪽" 에 이어)

(읽기만 — `../PE_emulator*` 는 안 열었다. 145 에서 읽은 파일 상태를 기준으로 한 판정.)

| 대상 | 맞나 · 틀리나 |
|---|---|
| 메시지 상자 12 샘플 (`NtUserGetDCEx(0,0,3)` 기본 대답 0 ×2 → `MessageBoxW` 0) | **stub 이 0 을 주기 때문.** 진짜 길에서는 0 이 될 조건이 일반 프로세스에는 없다 (표 (B)). **고쳐야 할 것 = HDC (종류 `0x10000`, 145 목록 8 ~ 10) 를 돌려주기 + 아래 글 폭 줄** |
| 145 목록 13 `NtGdiGetDeviceCaps` 최소값 (`2 → 1` · `0xc → 32` · `8 → 1920` · `0xa → 1080` · `0x58 = 0x5a → 0x60` …) | **맞음.** 이번에 StubDisp 상수 (1024 × 768 · 32 비트 · `LOGPIXELS` = DEVMODE) 와 같은 꼴임을 확인 — 값은 달라도 (1920 × 1080) 유저 쪽이 검사하지 않음 |
| 145 목록 10 (`DC_ATTR` 블록) | **더하기:** 진짜 틀 값 (표 (C)) 은 `+0x88 = 1` · `+0x40 = 0x4b` · `+0x20 = +0x2c = 16.0f` · `+0x154 = 0x1802` · `+0xa0/+0xa8/+0x128` = 스톡 0 / 7 / 13. **사본 길을 안 쓸 거면 `+0x40` 을 0 으로 두는 편이 안전** — 아래 목록 17 |

### stub 이 채울 최소 목록 — 145 의 1 ~ 16 에 이어 붙임

17. **글 폭 되돌이 syscall (메시지 상자 12 를 풀 최소)** — 145 목록 8 ~ 10 (HDC · 표 칸 · `DC_ATTR` 블록) 이 있다는 전제:
    - **`NtUserGetDCEx(0, 0, 3)` → 목록 8 의 HDC (≠ 0)** — 두 번 불리면 (139: 0 이면 한 번 더) 같은 HDC 를 줘도 됨. `NtUserReleaseDC` → 1.
    - **`NtGdiGetTextMetricsW(hdc, 버퍼, 0x44)` → `≠ 0`, 버퍼 앞 `0x3c` B (`TEXTMETRICW`) 채우기**: `+0x00 tmHeight` ≥ 1 · `+0x14 tmAveCharWidth` ≥ 1 · **`+0x37 tmPitchAndFamily` 비트 0 은 0** (가변 폭 비트를 꺼 두면 52 글자 폭 계산 (차례 8) 을 건너뛰고 `tmAveCharWidth` 가 폭이 된다). 0 을 주면 `MessageBoxW` = 0.
    - **`NtGdiGetTextExtent(hdc, 글, 개수, SIZE*, 1)` → `≠ 0`, `SIZE.cx` · `SIZE.cy` 채우기** (개수 > 0 일 때 `cx` ≥ 1). `GetTextExtentPointW` 가 이걸로 간다 (개수 0 이면 syscall 안 감, 차례 9). 가변 폭 비트를 켠 글꼴을 흉내 낼 때는 52 글자 `cx` 가 `26` 이상이어야 폭이 1 이상.
    - **`NtGdiGetTextFaceW(hdc, 0x20, 버퍼, 0)` → 0 (기본) 이어도 됨** (버퍼 첫 글자가 `'@'` 만 아니면). 이름을 주려면 `L"Segoe UI"` 꼴로.
    - `NtGdiQueryFontAssocInfo(0)` → **0** (기본). 0 이 아니면 사본 길이 영영 막힘 (그래도 되돌이로 지나감).
    - `NtGdiComputeXformCoefficients(hdc)` → **0** (기본). **0 이 사본 길을 막고 `DC_ATTR+0x98 \|= 0x20` 을 유저 코드가 직접 씀** — 그 뒤로는 (글꼴을 다시 고를 때까지) syscall 되돌이만.
    - `NtGdiGetDCDpiScaleValue(hdc)` → 0 · 1 어느 쪽이든 (표가 맞으면 안 부름). **`DC_ATTR+0x88 = 1`** 을 두면 `GetTextExtentPointW*` 가 표 검사 · 사본 시도 (→ NULL → 되돌이) 를 더 거침; 0 이면 곧장 되돌이. 값을 안 정했으면 1.
18. **사본 길을 흉내 내려면 (필요 없음 — 권하지 않음)**: 글꼴 항목 (종류 `0xa`) 의 `+0x10` 이 가리키는 attr `{+0 = 0 (비트 0 꺼짐), +8 = 목록 머리 칸 포인터}` · `DC_ATTR+0x40` 비트 0 · `gpwcANSICharSet` (유저 쪽 전역, `bGetANSISetMap` 이 채움) · `NtGdiGetWidthTable` 응답 · 공유 메모리 `+0x180098` (`= 1`). 하나라도 틀리면 `+0x98 \|= 0x20` 으로 막힐 뿐이라 **안 만드는 쪽이 싸다.**
19. **조용한 부수 효과 · 값**: ① 공유 메모리 `+0x180094` = 0 (`gbLpk` 꺼짐) · `+0x180098` = 1 ② `gpsi` (user32) 가 있어야 `GetSysColor` 가 안 죽음 ③ `NtQueryInformationProcess(자기, 0x34, {0xb, …})` → 성공 + 값 0 (또는 실패) — 5 는 금지 ④ `ApiSetQueryApiSetPresence("ext-ms-win-gdi-edgegdi-l1-1-0")` → 없음 (ntdll 쪽, 에뮬레이터가 ntdll 을 진짜로 쓰면 자동).
20. **gdiplus 시작 꾸러미 (N2)**: 145 목록 11 · 12 이후 `+0x7b3 BackgroundThreadStartup` 은 `NtCreateEvent` ×2 (핸들 ≠ 0) · `NtCreateThreadEx` (핸들 ≠ 0). 새 스레드 `BackgroundThreadProc` (gdiplus `0xa52b0`) 이 숨은 창 · 팔레트 · `GetDC` 를 하는 줄은 표 (F2). **스레드를 못 만들면 `GdiplusStartup` 이 `Win32Error` 로 끝** — 이게 12 · 129 샘플 다음 벽. (새 스레드의 syscall 이 기본 대답 0 이어도 `GdiplusStartup` 반환은 안 막힌다: `BackgroundThreadStartup` 은 스레드 핸들만 봄 `+0x83 ~ +0x8d`.)

**"그 뒤 벽" 한 줄**: 145 가 적은 `+0x62b ~ +0x7b3` 은 위 20 만 있으면 지나간다. 그 뒤 벽은 **글꼴 열거 (`GpInstalledFontCollection` 이 처음 쓰일 때)** — 이번 요청에서 안 읽음.

## 141 · 145 고쳐 읽기 (이번에 새로 맞춘 것)

1. **141 표 (2b) `GetTextExtentPointW (DC)` "표 실패 → `GdiSetLastError(0x57)` + 0"**: 그 줄 (`+0x89/+0x8e`) 에 닿으려면 **그 앞에서 `GetDCDpiScaleValue(hdc) == 1` 을 먼저 통과해야** 한다 (`+0x69` ret `+0x6e`, `+0x70 JNZ → +0x214`). 표가 없으면 `GetDCDpiScaleValue` 가 `NtGdiGetDCDpiScaleValue` (syscall `0x12be`, `+0x107` ret `+0x10e`) 를 부르고 **그 반환이 1 이 아니면 `+0x214` 로 곧장 `NtGdiGetTextExtent` syscall** — **`LastError` 를 안 세운다.** 현재 에뮬레이터 (표 없음, 기본 대답 0) 의 `GetTextExtentPointW` 는 `NtGdiGetDCDpiScaleValue` → `NtGdiGetTextExtent` 두 번 부르고 0. (`GetTextMetricsW` · `GdiGetCharDimensions` 는 141 대로 표 검사 → 0, syscall 없음.)
2. **145 (2b) `DC_ATTR +0x114` "글 쪽 (언어팩) · `gbLpk ≠ 0` 일 때만" · "N3 몫 `[공백]`"** → 닫음: `gbLpk` 는 `GdiProcessSetup +0x161` 이 공유 메모리 `+0x180094` 가 0 이 아닐 때만 켬 (표 (D3)). 일반 빌드 · 설정에서는 0.
3. **145 (2b) `+0x160` "0"** → 유저가 읽고 **씀**: `pcfLocateCFONT +0x58d ~ +0x5a3` 이 사본 목록 머리로 쓴다 (비트 1 꺼진 DC 만).
4. **145 목록 10 "`+0x98` = `0x12001f`"** → 틀 값은 `0x10` 이고 `0x12001f` 는 DC 만들 때 사본에 덮는 값 (표 (C)). 유저가 보는 처음 값은 `0x12001f` 로 같다.
5. **141 "GetVersionExA 어딘가 `NtQueryInformationProcess(0x25)`"** → 줄 못 찾음 (한 줄 답 6 ①). 그 문장은 "추측" 으로 내린다.

## 동적으로 확인할 것 — 디버거 필요 (Q277 VM GUI 세션에 얹을 꼴)

GUI 프로세스 (MessageBox 를 띄우는) 이름으로 조건. 커널 멈춤점은 모든 프로세스에 걸림.

1. **글 폭 사본 길**: `gdi32full!GdiGetCharDimensions +0xd7` (표 풀기 직후 `RBX` = `DC_ATTR` 포인터 → `db @rbx L0x170`) · `+0x11e` (`RAX` → `[RAX]` = 사본 포인터, NULL 인가) · `+0x133` (`EAX` = `bGetTextMetricsWInternal`) · `+0x1c5` (`EAX` · `[RBP-0x49]` = 52 글자 `cx`) · `+0x17b` (`ESI` = 폭). `gdi32full!pcfLocateCFONT +0x12f` (`EAX` = 비율) · `+0x156` (글꼴 연결) · `+0x184` (`[RBP+0x40]` 비트 0) · `+0x2a2` (목록 머리) · `+0x413` (새 사본 `RBX` → 사본 0x5c8+ B 덤프). `gdi32full!pcfLocateCFONT +0x3db` 에 닿는지 (`[DC_ATTR+0x98]` 에 `0x20` 이 켜지는 줄).
2. **그 DC 의 칸**: `DC_ATTR` 0x170 B 덤프 — 특히 `+0x00 +0x20 +0x2c +0x40 +0x88 +0x98 +0xa0 +0xa8 +0x114 +0x118 +0x128 +0x154 +0x160` 값이 표 (C) 와 같은가 · 스톡 핸들이 `GetStockObject(0 · 7 · 13)` 과 같은가 · 글꼴 항목 (`SelectObject` 한 HFONT) 의 표 칸 `+0xf` · `+0x10` 이 가리키는 attr.
3. **win32k**: `win32kfull!NtUserGetDCEx +0x62` (`[RAX+0xace8]` 값) · `+0xb8` (`EAX` = 기능 켜짐) · `+0xd3` (`AL`) · `win32kbase!_GetDCEx +0xa21` (`RAX` = `CreateCacheDC` 반환) · `GreCreateDisplayDC +0x20a` (유저 슬롯) · `+0x243` · `DCMEMOBJ +0x1b8` · `InitializeDefaultDCAttributes +0x172` (이미 끝난 뒤 — `[W32GetSessionState()+0x58]+0x90` 0x1b0 B 덤프) · `GreUpdateSharedDevCaps +0x2a` 뒤 `[세션G+0x6e0]` 0x98 B · `[PDEV+0x848]` 0x140 B (`GDIINFO`).
4. **언어팩 · 공유 메모리**: `InitializeGre +0x4bf` (콜백 뒤) 직후 `[RCX+0x180094]` · `LpkShapeQueryRoutine +0x32` 이 몇 번 불렸나 (키 안 값 개수).
5. **필터**: `ntoskrnl!PspAllocateProcess +0x1f11` (`ECX` = 새 프로세스의 `Win32KFilterSet`; test.exe · 일반 GUI exe 둘 다 0 인가) · `kernelbase!GetProcessMitigationPolicy +0xcd` (`mov eax,[rbp-0x2c]` — 돌려받은 값).
6. **N5**: `kernelbase!GetProcessVersion +0x3b` (NtQIP 호출) 에 **콜스택** — 누가 부르는지 (gdiplus 시작 길에서 닿는지).
7. **스레드**: `gdiplus!CreateThreadAndAddRefDll +0x8c` (ret 뒤 `RAX` = 스레드 핸들) · 새 스레드 `BackgroundThreadProc` (`0xa52b0`) 의 `InternalNotificationStartup +0x218` (숨은 창 `RAX`) · `+0x2ae` (`GetDC` 의 `RAX`, 필터 5 면 `0xFFFFFFFFDCDE5000`) · `+0x2c6` (`SelectPalette` 반환).

## 동적으로 확인할 것 — 디버거 없이 떠 올 수 있는 것 (VM 수집 스크립트 재료)

타깃 VM (26100.9457) 에서 일반 사용자 exe / 레지스트리 내보내기로:

1. **화면 지표 표**: `GetDeviceCaps(화면 HDC, 0 ~ 120)` 전부 (특히 `HORZSIZE 4 · VERTSIZE 6 · HORZRES 8 · VERTRES 10 · BITSPIXEL 12 · PLANES 14 · NUMPENS 18 · NUMFONTS 22 · NUMCOLORS 24 · TEXTCAPS 34 · RASTERCAPS 38 · ASPECTX/Y/XY 40/42/44 · LOGPIXELSX/Y 88/90 · SIZEPALETTE 104 · COLORRES 108 · PHYSICAL* 110 ~ 113 · VREFRESH 116 · DESKTOPVERTRES 117 · DESKTOPHORZRES 118 · BLTALIGNMENT 119 · SHADEBLENDCAPS 120 · COLORMGMTCAPS 121`) — **GDIINFO 를 채우는 쪽이 StubDisp 인지 진짜 드라이버인지** 가늠 (StubDisp 면 `HORZSIZE` = 0 · `RASTERCAPS` = 0 · `PHYSICAL*` = 0, 1024 × 768 이 아닌 해상도면 진짜 드라이버).
2. **메시지 상자 글꼴과 폭**: `SystemParametersInfoW(SPI_GETNONCLIENTMETRICS)` 의 `lfMessageFont` · 그 글꼴을 `CreateFontIndirectW` → `SelectObject` → `GetTextMetricsW` (`tmHeight` · `tmAveCharWidth` · `tmMaxCharWidth` · `tmPitchAndFamily` 비트 0 · `tmCharSet`) · `GetTextFaceW` · `gdi32!GdiGetCharDimensions` (gdi32 내보내기, `GetProcAddress`) 반환 · `GetTextExtentPoint32W` (`abc…ABC…XYZ` 52 자 · `"OK"` · `"확인"`). 이것이 에뮬레이터의 `NtGdiGetTextMetricsW` · `NtGdiGetTextExtent` 대답 재료. `GetDpiForSystem` · `GetSystemMetrics(SM_CXICON …)`.
3. **글꼴 레지스트리 · 파일** (시작 길 뒤 글꼴 열거 때 읽을 것): `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts` (값 이름 · 파일 이름 목록) · `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes` · `FontLink\SystemLink` · `HKCU\Control Panel\Desktop\WindowMetrics` (`MessageFont` 등 LOGFONT 바이너리) · `HKCU\Control Panel\Desktop` (`LogPixels` · `Win8DpiScaling` · `DpiScalingVer`) · 글꼴 폴더 `C:\Windows\Fonts` 파일 이름 목록 (이름 · 크기).
4. **언어팩 · 로케일**: `HKLM\SYSTEM\CurrentControlSet\Control\LanguagePack` 의 **값 이름 · 종류 · 데이터 전부** (`REG_DWORD` 가 있으면 `gbLpk` 켜짐) · `GetSystemDefaultLCID` · `GetACP` · `GetSystemDirectoryW`.
5. **DPI · 필터**: `GetProcessDpiAwareness` (`shcore`) · `GetDpiForSystem` · `GetThreadDpiAwarenessContext` · **`GetProcessMitigationPolicy(ProcessSystemCallFilterPolicy)` 값 (test.exe · 일반 GUI exe · Edge 렌더러 같은 걸 띄워 비교)** · `ApiSetQueryApiSetPresence` (ntdll 내보내기) 로 `ext-ms-win-gdi-edgegdi-l1-1-0` · `IsApiSetImplemented`.
6. **스톡 · 시스템 색**: `GetStockObject(0 · 7 · 13)` 의 핸들 값 · `GetCurrentObject(새 DC, OBJ_BRUSH · OBJ_PEN · OBJ_FONT)` 와 비교 (같으면 표 (C) 확인) · `GetSysColor(1 · 15 · 16 · 20)` · `GetVersionExA` 결과 (`dwMajorVersion` …).

## 다음 요청 후보

- **N5 마무리 (kernelbase 프로젝트가 비었을 때)**: `GetVersionExW` (`0xc4ae0`) 가 부르는 `SbSelectProcedure(0xabababab, 0, 표 0x1801aa300, 3)` 의 후보 함수 셋과 `GetProcessVersion` (`0x23dc0`) 을 부르는 줄 (kernelbase 밖 호출자 — kernel32 · 다른 DLL) 찾기. 정적: kernelbase export + `refs`.
- **글꼴 사본 "진짜 꼴"**: `pcfCreateCFONT` (`0x3d884`) 의 `CFONT` 칸 모양 (`+0x1c` 비트 · `+0x254` · `+0x258` 메트릭 · `+0x54` 폭 표 · `+0x5c8`) · `bFillWidthTableForGTE/GCW` 의 `NtGdiGetWidthTable` 인자 · win32kfull 쪽 `NtGdiGetTextMetricsW` · `NtGdiGetTextExtent(ExW)` · `NtGdiHfontCreate` · 글꼴 항목의 attr (`+0x10`) 을 잡는 곳. (에뮬레이터가 사본 길을 정말 필요로 할 때만.)
- **글꼴 열거** (gdiplus `+0x775` 의 뒤): `GpInstalledFontCollection` 처음 쓰일 때 — `GpFontTable` 채우기 · `GdipCreateFontFamilyFromName` · 레지스트리 · 파일 (VM 수집 3 번과 짝).
- **`PspAllocateProcess` Win32k 필터 속성** (`[R15+0x1b0]` 에 누가 `FilterSet` 을 넣나 — `NtCreateUserProcess` 속성 `0x10`?) 와 필터 5 의 user32 · win32kfull 쪽 syscall 표 (`W32pServiceTableFilterSet…`). ntoskrnl · win32kfull. (sast/153 이 ntoskrnl 을 놓은 뒤.)
- **`[세션+0xace8]` 가 무엇인가** (`NtUserGetDCEx +0x62` 비교) · `UserGetMonitorDC` (`0x67428`) · `DrvGetHDEV` (`0x3a960`) · `NtGdiGetDeviceCaps` 커널 쪽에서 `GreGetDeviceCapsInternal +0x1a9 ~ +0x1e4` (DPI 100% 아닐 때).
- **sast/151 (user32 · win32k) 로 넘길 것**: user32 `g_systemCallFilterId == 5` 25 곳 본문 (표 (E) 마지막 줄) · `SoftModalMessageBox` 의 DPI 쪽.

## 스크립트 문제

- **고쳐야 할 스크립트 오류 없음.** `run_ghidra.sh` export · refs 서른 번 남짓 모두 종료 코드 0, `SCRIPT ERROR` 0 줄.
- 쓰는 법 메모 (오류 아님, 제안만): `export` 의 정규식 자리에 `rva:` 를 `|` 로 섞으면 **`rva:` 쪽이 무시된다** (`'^(A|B)$|rva:0x1094c0'` → 이름 일곱 개만 뽑힘, 로그 한 줄 `이름으로 고른 함수 7개` 로만 알 수 있음). `reference.md` · `SKILL.md` 에 "`rva:` 는 단독으로만" 한 줄 있으면 좋겠다.
- 겹친 이름은 이번에도 `__<rva>` 로 정상 (`bInit__1094c0` · `bInit__1af630`, 기존 `GetTextMetricsW.asm` ≠ `GetTextMetricsW__4df30.asm` — 뒤는 전혀 다른 함수라 이번 길과 무관).

## 판정

**분석성공 (정적으로 닫은 것 + 못 닫은 것 표시).** 요청 1 ~ 6 모두 줄 근거와 함께 답함:
- 닫음: 1 (글 폭 길 · 읽는 칸 · syscall · 0 / 실패 갈래 · `NtUserGetDCEx(0,0,3)` = 0 조건), 2 (세션 `DC_ATTR` 틀 · 스톡 핸들 · `GDIINFO` · `pGdiDevCaps` 채우는 곳 · 언어팩 칸), 3 (필터 5 의 출처 · 갈래), 4 (`GrepCreateCompatibleDC` · DPI 칸 · 주 화면 판정 · `hdcOpenDCW` 윤곽), 5 (N2 — **벽 아님**), 6 ②~④ (표 칸 `+0xf` · `g_pEdgeGdi` · `GpDevice+0x628`).
- **못 닫음 `[공백]`**: N5 의 `NtQueryInformationProcess(0x25)` 호출자 (kernelbase 안에서는 `GetProcessVersion` 이 부르는 줄이라는 것만) · `[세션+0xace8]` 뜻 · 진짜 화면 드라이버의 `GDIINFO` · 필터 5 의 user32 본문 · 틀 안 몇 칸의 뜻 (`+0x64`, `+0xd0 ~ +0xe8`, `+0x100`…) · `StubDispGetModes` 의 `dmLogPixels` 표 · `GetSystemDefaultLCID` · ETW 등록 syscall.
- 이번에 읽은 MS 코드는 레포에 붙이지 않았다 (`ms/decomp` · `ms/disassemble` 경로와 `+0x…` 만). 새 파일은 `ms/decomp` · `ms/disassemble` 아래 위에 적은 함수들.

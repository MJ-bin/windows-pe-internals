# 2026-11-25 (작성일, 정적 분석 2026-11-25) run144 — GDI 시작과 공유 핸들 표 (Q270, 정적만): `GdiplusStartup` 이 1 (`GenericError`) 로 나가는 첫 가지는 **gdiplus `InternalGdiplusStartup +0x1c3 CreateRectRgn(0,0,1,1)` 이 NULL 을 받아 `+0x1d9 JZ → +0x7d1`** 로 가는 자리다. NULL 을 만드는 곳은 gdiplus 가 아니라 **gdi32 `CreateRectRgn` 안** — `NtGdiCreateRectRgn` 이 준 핸들을 **PEB `+0xf8` 공유 핸들 표에서 다시 읽어** 칸 종류 (`+0x19c` == 4) · 윗 16비트 (`+0x1ae`) · 주인 PID (`+0x1c0`) · 사용자 쪽 포인터 (`+0x1d4` · `+0x1f3`) 를 보고, 칸이 0 이면 `+0x116` 에서 `DeleteRegion` (= 에뮬레이터 로그의 `NtGdiDeleteObjectApp`) 을 부른 뒤 NULL 을 돌려준다. 표는 **PEB `+0xf8` 그대로 시작, 칸 0x18 B, 칸 번호 = 핸들 `& 0xffff`**. gdi32full `GdiValidateHandle` 은 칸 `+0xc` word == 핸들 윗 16비트 · 칸 `+0x8` (& ~1) == 이 프로세스 PID **또는 0** 만 본다 (종류 · 포인터는 안 봄). user32 `RegisterClassExWOWW` 는 배경 붓이 `0x1f` 보다 크면 이 함수를 부르고, 거절이면 `0x57` + 0 — PE_emulator AT03 실험 ("스톡 칸을 채우면 `RegisterClassEx` 가 실패") 은 **스톡 배열만 채우고 표 칸은 0 이라 윗 16비트 비교에서 FALSE** 로 코드가 설명한다

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("`GdiplusStartup` 이 무엇을 보고 1 로 나가나, 그리고 gdi32 가 GDI 핸들을 믿을 때 공유 핸들 표의 어느 칸을 보나", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/343_internals-gdi-startup.md`, 튜닝 `343_tune16.md` GP1 (`GdiplusStartup` 110 샘플 110 / 110 이 1) · ST1 (`GetStockObject` 0 179 샘플)) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 · gdi32 · gdiplus 를 안 올린다 — `sample/log.h:3` "user32(wsprintf)도 안 쓴다" `[소스]`, 초기화 끝 (P2) 모듈 6개에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run142 · run143 과 같은 줄), 사진 PEB `GdiSharedHandleTable` (null) `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`, run86 과 같은 줄). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (새 gdiplus 줄 · gdi32 · gdi32full · user32 · win32kbase 줄 · 근거 줄) · 3장 gdi32 하위 절 한 줄 · 창 API 하위 절 두 줄 · 그 뒤 새 하위 절 · 창 클래스 등록 절 한 줄 · 4장 (run86 정적 ⑤ · run142 정적 N4 표시 · Q270 닫힘 줄 · run144 줄 셋) · 5장 (run144 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다 (반영 거리 판단은 아래 "짐 지도 반영 거리").

> **대기열**: **Q270** (PE_emulator 요청 343, 사용자 결정 2026-11-25 — PE_emulator D154 의 넷째, 정적만). 결과서 판정 **분석성공** — 질문 1 · 2 닫음, 질문 3 은 읽은 함수 한정으로 닫음 (`CreateFontIndirectW` 몸 · `DC_ATTR` 전체 · 글꼴 사본 쪽은 `[공백]`).
> - 이어 쓰는 앞 run: **run86** (PEB `+0xf8` → GDI 공유 메모리 최소 `0x180150` B, `+0x180140` DC 붓 · `+0x180148` DC 펜 0 아님, `DllMain +0x2e NtGdiInit2` → gdi32 `gCookie`, `GdiProcessSetup` 이 `pGdiSharedMemory` · `pGdiSharedHandleTable` · `pGdiDevCaps` 를 씀, 스톡 칸 `+0x1800b0 + 8*i`), **run131** (`RegisterClassExWOWW` 차례 — "커널 부르기 전 거르는 `0x57` 다섯 경우" 중 "잘못된 브러시" 의 조건을 이번에 줄로 닫음, run131 은 고칠 것 없음), **run142** (sast/139 — `MessageBoxW` 길의 GDI 쪽 DC · 글꼴 · 글자 폭을 N4 로 이 바퀴에 넘김), **run121** (`GdiTebBatch` · `GdiBatchCount` 를 gdi32 에서는 `InternalDeleteObject` · `DeleteRegion` 둘만 씀 — 이번 줄과 같음), **run113** (PEB `GdiHandleBuffer` (`+0x140`) 쓰는 곳 win32kbase `GdiProcessCallout +0xfe`).
> - 앞 run 고쳐 읽기 둘: run86 · run142 맨 위 한 줄 (아래 "앞 run 고쳐 읽기").
> - 남은 동적 → **Q277 (동적: 결과서 동적 1~6 — PEB +0xf8 표 스톡 붓·영역·DC 칸·gCookie · NtGdiCreateRectRgn 핸들 비트 · GetStockObject 값 · RegisterClassExWOWW +0x2e4 EAX·RtlGetExpWinVer · TEB+0x78 · NtQIP(0x25)/NtProtectVirtualMemory ×2 부르는 줄 — Q162·Q273 과 합침)**, 남은 정적 → **Q278 (정적: 결과서 다음 요청 후보 — N1 CreateICA("DISPLAY")·NtGdiOpenDCW HDC·DC_ATTR·GetDeviceCaps(0x58/0x5a) (바로 다음 벽, 가장 급함) · InitImagingLibrary·InitSystemFontsDirs·GetGpInstalledFontCollection · DC_ATTR 전체·글꼴 사본 · win32k 표 채우기 · NtQIP(0x25) 출처)**.

## 요약

- **첫 가지** `[정적]`: `GdiplusStartup` (gdiplus rva `0xe1170`) → `+0x72 InternalGdiplusStartup` (`0xe1380`) → `+0x1c3 CreateRectRgn(0,0,1,1)` (gdi32) → 반환을 `Globals::CachedGdiRegion` 에 두고 `+0x1d6 TEST RAX,RAX` · **`+0x1d9 JZ → +0x7d1`** → `InternalGdiplusShutdown` → `MOV EAX,ESI` = **1**.
  - 그 앞 `Initialize` (`GpRuntime::OSInfo`) · `InitVersionInfo` 는 **반환이 void** 라 분기에 안 쓰인다. 그래서 `NtQueryInformationProcess(0x25)` 출력 칸 · `NtQueryLicenseValue` 값은 1 을 만들 수 없다.
  - `InternalGdiplusStartup` 의 실패 가지는 18곳이고 (flow-writer 셈) **모두 1** 을 돌려준다 (`ESI` 는 `+0x55` 에서 1 이 된 뒤 안 바뀜). 이 함수가 내는 실패 값은 1 하나다.
- **NULL 을 만드는 곳** `[정적]`: gdi32 `CreateRectRgn` (rva `0x2950`). `hGetPEBHandle` (PEB `+0x140` 캐시, 새 프로세스는 빈 캐시라 0) → `+0x16d NtGdiCreateRectRgn` → 핸들 `H` 로 표 칸을 복사 → **종류 `+0xe` == 4 · `+0xc` word == `H>>16` · (`+0x8` & ~1) == `gW32PID` (0 은 안 됨) · `+0x10` ≠ 0 이고 풀어도 ≠ 0** → 하나라도 아니면 `+0x116` → `DeleteRegion(H)` → NULL.
  - 에뮬레이터 로그의 차례 (`NtGdiCreateRectRgn` → `NtGdiDeleteObjectApp(0x20800)` → … → 1, `NtGdiOpenDCW` 없음) 와 줄 단위로 맞는다. `NtGdiDeleteObjectApp` 은 gdiplus 정리 쪽이 아니라 이 `DeleteRegion` 이다 (`InternalGdiplusShutdown +0x25d` 는 `CachedGdiRegion` 이 NULL 이면 `DeleteObject` 를 안 부름).
- **핸들 표** `[정적]`: 시작 = PEB `+0xf8` 값 그대로 (`GdiProcessSetup` 이 `pGdiSharedMemory` 와 `pGdiSharedHandleTable` 에 같은 값). 칸 = **0x18 B**, 칸 번호 = `H & 0xffff` (핸들 24 ~ 31비트가 0 이 아니면 칸 `+0xd` 바이트와 같을 때만, 아니면 `NtGdiGetEntry` syscall). 칸 `+0x8` 주인 · `+0xc` 윗 16비트 · `+0xe` 종류 · `+0xf` 플래그 · `+0x10` 가려진 사용자 쪽 포인터 (이름은 `[추측]`).
- **핸들 비트** `[정적]`: 0 ~ 15 칸 번호 · 16 ~ 22 종류 (`0x10000` DC · `0x40000` 영역 · `0x100000` 붓 · `0xa0000` 글꼴 …) · 23 스톡 (`0x800000`) · 24 ~ 31 고유 번호. 핸들 윗 16비트 = 칸 `+0xc` word (win32kbase `HmgpComputeHandleValue`).
- **`GdiValidateHandle`** (gdi32full `0x70180`) `[정적]`: 핸들 0 → TRUE. 아니면 [번호 < `gMaxGdiHandleCount` · `+0xc` == `H>>16` · `+0x8` & ~1 == PID 또는 0] 이면 TRUE. **종류 · 포인터는 안 본다.**
- **`RegisterClassExWOWW`** `[정적]`: `hbrBackground` (`+0x30`) 가 `0x1f` 보다 크면 `+0x2d8 GdiValidateHandle` → 거절 + 앱 서브시스템 버전 > 3.0 이면 `RtlSetLastWin32Error(0x57)` + 0. 3.0 이하면 붓을 0 으로 바꾸고 계속.
- **AT03 설명** `[정적]`: 스톡 칸 값 `0x21400 + i*0x400` → 칸 번호 `0x1400 + i*0x400` → 표 칸이 0 → `+0xc` word 0 ≠ `H>>16` = `0x0002` → FALSE → `0x57`. 칸이 0 이면 붓 값 0 이 `CMP RCX,0x1f` 에서 걸러져 검사를 안 한다. **원인은 "배열만 채우고 표 칸은 안 채움".**
- **에뮬레이터 영역 핸들 `0x20800` 은 종류 비트가 틀렸다** `[정적]`: 종류 (16 ~ 22비트) = `2`, 영역은 `4`. 표 칸만 채워서는 `CreateRectRgn` 을 넘어도 `DeleteObject` 가 `InternalDeleteObject +0x5c/+0x5e` (칸 `+0xe` == `(H>>16)&0x1f`) 에서 FALSE 가 된다. 꼴은 `번호 | 0x040000 | (고유 << 24)`.
- **표를 syscall 앞에서 보는 함수** `[정적]`: `DeleteObject` (칸 없으면 FALSE, syscall 없음) · `SelectObject` · `GetObjectType` · `GetDeviceCaps` · `GetTextMetricsW` · `GetTextExtentPointW` · `GdiGetCharDimensions`. 안 보는 것: `GetStockObject` (배열만) · `GetObjectW` (핸들 종류 비트만).
- **다음 벽** `[정적]`: 첫 가지를 넘기면 `+0x280 CreateICA("DISPLAY")` → `+0x296 JZ → +0x7d1` (Q278 N1).

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 백둘째 바퀴. plan **Q270**. **짐 지도 바퀴 아님.**
- 정적: **gdiplus 처음 analyze** (`System32/GdiPlus.dll`, PDB 받음, 50초 안쪽, 기본 `MAXMEM` 8G, 함수 5239 · `FUN_` 0 · `SetPdb` 줄 있음, 지도 `ms/disassemble/gdiplus/_map/GdiplusStartup_*`). 에뮬레이터가 올리는 파일이 `C:\WINDOWS\System32\gdiplus.dll` (원문) 이라 WinSxS 판은 안 봤다. gdi32 · gdi32full · user32 · win32kbase 는 다시 analyze 안 함 (`-readOnly` export · refs 만).

| 모듈 | PDB guid+age (`ms/sym/<이름>.pdb.info.txt`) | 기준 줄과 |
|---|---|---|
| gdiplus (`gdiplus.pdb`) | `DDF19AA09F285660C86065C38EF751A41` | **새 줄** (처음 analyze) |
| gdi32 | `92C6EC2ABE478836C99BC83C2F3AE3551` | 같음 (run86) |
| gdi32full | `A6245E4F5C78EB06438EA4820F1F6EB41` | 같음 (run86) |
| user32 | `CDB947D1B96B87977915DE04CF54279C1` | 같음 (run85 · run142) |
| win32kbase | `D22DFDA3D54B87E6BD0567A57877904C1` | 같음 (run136 · run142) |
| ntoskrnl | `C29EBFB06B78B3C020DCA66D99713F9E1` | 같음 (빌드 확인용) |

- 버전 대조 (결과서): `GdiProcessSetup +0x15a` · `+0x161` · `+0x456`, `DllMain +0x2e`, `GdiDllInitialize +0x16`, `GetStockObject +0x65 ~ +0x72`, 가져오기 칸 `0x1800c1fe8` = run86 과 같음. `RegisterClassExWOWW +0x2d8` = run131 · sast/139 와 같음. 내보내기 RVA (gdiplus `GdiplusStartup` `0xe1170` 서수 609 · gdi32full `GdiValidateHandle` `0x70180` · `GetStockObject` `0x25e90` · gdi32 `CreateRectRgn` `0x2950` · `DeleteObject` `0x1070` · `GdiGetEntry` `0x1010` · `GdiValidateHandle` `0xc5c0` · user32 `RegisterClassExW` `0x1c0d0`) 는 `objdump -p` 와 `_index.tsv` 가 같음.
- ntdll 은 `pdb_struct.py` (PDB 파일만) 로 `_PEB +0xf8 GdiSharedHandleTable` · `+0x140 GdiHandleBuffer` · `_TEB +0x40 ClientId` · `+0x78 Win32ThreadInfo` · `+0x2f0 GdiTebBatch` · `+0x1740 GdiBatchCount` 이름만 확인.

## 목적

- PE_emulator 쪽 막힘 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님, `[관찰]` 로 안 씀): `GdiplusStartup` 안 syscall 차례 `NtAllocateVirtualMemory` · `NtFreeVirtualMemory` · `NtAllocateVirtualMemory` → `NtQuerySystemInformation(0)` · `(1)` → `NtQueryInformationProcess(−1, 0x25)` (출력 안 씀) → `NtQueryLicenseValue` 둘 → `NtGdiCreateRectRgn(0,0,1,1)` (핸들 `0x20800`) → `NtGdiDeleteObjectApp(0x20800)` (0) → `NtProtectVirtualMemory` 둘 → `NtFreeVirtualMemory` → **1**. 그리고 AT03 실험 (스톡 붓 칸 `0x21400 + i*0x400` → `RegisterClassEx` 실패, "매개 변수가 틀립니다"). 요청서 `_workspace/sast/141_gdi-startup.md`:
  1. `GdiplusStartup` 이 1 로 나가는 첫 가지 — 무엇의 결과를 보나, 성공하려면 무엇이 있어야 하나.
  2. gdi32 가 핸들을 믿는 조건 — `GdiValidateHandle` 이 표에서 읽는 칸 · 칸 번호 식 · 통과 조건, `RegisterClassExWOWW` 배경 붓 검사, 스톡 핸들 꼴.
  3. (되면) `DeleteObject` · `SelectObject` · `GetObjectW` 가 syscall 앞에서 표를 보나.
- 쓰는 곳 (원문): 팀원 `ext/syscall/syscall_layer_cpp/src/gui/nt_gdi_init2.cpp` · `nt_gdi_create_rect_rgn.cpp` · `NtGdiDeleteObjectApp` 새 stub · 보류 패치 `_workspace/01_coder/AT03_stock-objects_held.patch`.

## 방법

- 정적 sast/141 (결과서 `_workspace/sast/141_gdi-startup_result.md`, **분석성공**, 스크립트 문제 하나 → "도구 메모"):
  - gdiplus: `GdiplusStartup` · `InternalGdiplusStartup` depth 1 (34개, 첫 가지를 찾은 뒤 더 안 넓힘).
  - gdi32: `CreateRectRgn` · `CreateRectRgnIndirect` · `DeleteObject` · `InternalDeleteObject` · `DeleteRegion` · `SelectObject` · `GetObjectW` · `GetObjectType` · `GetStockObject` · `GdiValidateHandle` (썽크) · `CreateICA` (썽크) · `CreateSolidBrush` + 도우미 (`HANDLE_TO_INDEX` · `GdiGetEntry` · `hGetPEBHandle` · `IS_INDEX_IN_USER_SHARED_ARRAY`) · `CheckIsEdgeGdiProcessOnce` · `GetDeviceCaps` · `GetTextMetricsW` · `GetTextExtentPointW` · `CreateFontIndirectW` · `GetCurrentObject` · `pldcGet`.
  - gdi32full: `GdiValidateHandle` · `GetObjectW` · `SelectObjectImpl` · `GetObjectType` (+ 도우미) · `CreateICA` · `bCreateDCA` · `hdcCreateDCW` · `GetDpiDepStockObjectPlateauSlot` · `GdiGetCharDimensions` · `GetTextMetricsW` (둘) · `GetTextExtentPointW` · `CreateFontIndirectW` · `GdiConvertAndCheckDC`.
  - win32kbase (이름 · 꼴 확인용, depth 0): `bSetStockObject` · `GreGetStockObject` · `HMValidateSharedHandle` · `HmgpComputeHandleValue` · `HmgModifyHandleType` · `HmgAllocateObjectAttr`.
  - refs: user32 `0xb51b8` (gdi32 `GdiValidateHandle` 가져오기 칸, 9곳) · gdi32 `0x27bf0` (그 지연 가져오기 칸).
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - gdiplus `GdiplusStartup +0x1e · +0x21 · +0x27 · +0x2a · +0x30 · +0x34 · +0x36 · +0x39 · +0x3f · +0x46 · +0x48 · +0x59 · +0x65 · +0x6d · +0x72 · +0x77 · +0x79 · +0x7b · +0xeb`; `InternalGdiplusStartup +0x23 · +0x2f · +0x39 · +0x3c · +0x55 · +0xe1 ~ +0x173 (기능 검사 9) · +0x18b · +0x19b · +0x1b4 · +0x1b9 ~ +0x1c3 · +0x1ca · +0x1cf · +0x1d6 · +0x1d9 · +0x1e6 ~ +0x265 · +0x280 · +0x28c · +0x293 · +0x296 · +0x2a4 · +0x2cb · +0x312 ~ +0x5dc · +0x62b · +0x630 · +0x632 · +0x6d5 ~ +0x73d · +0x742 · +0x744 · +0x775 · +0x77a · +0x781 · +0x784 · +0x7b3 · +0x7b8 · +0x7ba · +0x7bc · +0x7c1 · +0x7cc · +0x7d1 · +0x7d6` + 실패 가지 전부 (`0x1800e1b4c` · `0x1800e1b51` 로 가는 `JZ` 18곳) + `ESI` 쓰는 줄 전부; `InternalGdiplusShutdown +0x2c · +0x25d · +0x260 · +0x262 · +0x608`; `Initialize +0x30 · +0x65 · +0x7d`; `InitVersionInfo +0x1e · +0x2a`.
  - gdi32 `CreateRectRgn +0x6f · +0x74 · +0x7a · +0x80 · +0x9d · +0xbd · +0xc3 · +0xda · +0xe0 · +0x102 · +0x116 · +0x122 · +0x127 · +0x129 · +0x12b · +0x16d · +0x17c · +0x18f ~ +0x1f3 · +0x1f9 ~ +0x251 · +0x25d · +0x26e · +0x291`; `hGetPEBHandle +0x1a ~ +0x66 · +0x153 ~ +0x159`; `DeleteRegion +0x2e · +0x33 · +0xd7 · +0xe9 · +0x10b · +0x118 · +0x12a · +0x13c`; `DeleteObject +0x1d · +0x46 · +0x4f · +0x5b`; `InternalDeleteObject +0x5c · +0x5e · +0x6c · +0x73 · +0x80 · +0x86 · +0xcc · +0xd1 · +0xe4 · +0xea · +0x129 · +0x14b · +0x27a`; `GdiGetEntry +0x15 · +0x19 · +0x3b · +0x43`; `SelectObject +0x0 ~ +0x6e`; `GdiValidateHandle +0x0`; `GetDeviceCaps +0x1cb`.
  - gdi32full `GdiValidateHandle +0x0 ~ +0x84` (전부); `HANDLE_TO_INDEX +0x0 ~ +0x3c` (전부); `SelectObjectImpl +0xc1 · +0xc5 · +0xd0 · +0xdb · +0xf1 · +0x1ba · +0x1cc · +0x1e5 · +0x1e7 · +0x240 · +0x30f · +0x398 · +0x39d · +0x52a · +0x52f · +0x5ed`; `GetStockObject +0x11 · +0x14 · +0x65 · +0x72 · +0x7a · +0x7c · +0x87 · +0x8a · +0x124`; `GetTextMetricsW +0x22a`; `GetTextExtentPointW +0x262`; `GdiGetCharDimensions +0x12e · +0x1c0`; `GetObjectW +0x64`.
  - user32 `RegisterClassExWOWW +0x114 · +0x119 · +0x129 · +0x12d · +0x131 · +0x137 · +0x2d8 · +0x2e4 · +0x2e6 · +0x2ec · +0x2f2 · +0x2f4 · +0x2f8 · +0x326 · +0x32b · +0x337 · +0x33b`; `RtlGetExpWinVer +0x10 · +0x15 · +0x1c · +0x34 · +0x3f · +0x41`.
  - win32kbase `bSetStockObject +0x0 ~ +0x8b` (전부); `HmgpComputeHandleValue +0x4 ~ +0x1b`.
  - `pdb_aliases.py --lookup gdi32.dll 0xd4c8` (아래 "flow-writer 대조").
  - `../PE_emulator` 의 `nt_gdi_init2.cpp` (38줄) · `nt_gdi_create_rect_rgn.cpp` (24줄) · AT03 패치는 **줄 번호만 훑음** (읽기만, 고치지 않음).

## 결과

비유:

- **공유 핸들 표는 "출입증 대장"이다.** 커널 (win32k) 이 그림 도구 (붓 · 영역 · DC) 를 하나 만들 때마다 대장 한 줄에 "번호 · 주인 · 종류 · 보관함 열쇠" 를 적어 두고, 프로그램에는 번호가 찍힌 출입증 (핸들) 만 준다.
- gdi32 는 출입증을 받으면 **대장을 펴서 그 줄이 진짜인지** 먼저 본다. 대장이 비어 있으면 "이 출입증은 가짜" 라고 보고 버린다 — 그게 `CreateRectRgn` 의 NULL, `GdiplusStartup` 의 1 이다.
- AT03 은 "출입증만 잔뜩 찍어 나눠 주고 대장엔 안 적은" 꼴이었다. 그래서 창구 (`RegisterClassEx`) 가 대장을 펴 보자마자 거절했다 `[추측: 비유]`.

### 정적 — 호출 지점 (`.asm` 줄, `ms/disassemble/{gdiplus,gdi32,gdi32full,user32,win32kbase}/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `gdiplus!GdiplusStartup` (export, rva `0xe1170`) | `+0x59` | `+0x60` | `EnterCriticalSection` — `+0x65` `LibraryInitRefCount` 가 0 일 때만 안으로 (`+0x6d JNZ → +0xab`) | [정적] |
| 〃 | **`+0x72`** | **`+0x77`** | **`InternalGdiplusStartup`** (`0xe1380`) — 반환 ≠ 0 이면 그 값 그대로 (`+0x79` · `+0x7b`) | [정적] |
| `gdiplus!InternalGdiplusStartup` | `+0x23` | `+0x2a` | `HeapCreate(0,0,0)` → `GpMemHeap` — NULL 이면 `+0x3c JZ → +0x7cc` (1) | [정적] |
| 〃 | `+0xe1` ~ `+0x173` (9곳) | 각 `+5` | 기능 켜짐 검사 함수 아홉 (Ghidra `__private_IsEnabled` 류) — 결과를 전역에 저장만 | [정적] |
| 〃 | `+0x18b` | `+0x190` | `GpRuntime::OSInfo::Initialize` (`0xd4980`) — void. 안에서 `+0x30 GetSystemInfo` · `+0x65 GetVersionExA` · `+0x7d IsProcessorFeaturePresent` | [정적] |
| 〃 | `+0x19b` | `+0x1a2` | `GetModuleHandleA(NULL)` (`DllInstance` 가 0 일 때) | [정적] |
| 〃 | `+0x1b4` | `+0x1b9` | `InitVersionInfo` (`0xd4a64`) — void. 안에서 `+0x1e GetVersionExA` · `+0x2a GetACP` | [정적] |
| 〃 | **`+0x1c3`** | **`+0x1ca`** | **`CreateRectRgn(0,0,1,1)`** (gdi32) — `+0x1b9 ~ +0x1c1` 인자. `+0x1cf` → `CachedGdiRegion`, **`+0x1d9 JZ → +0x7d1`** | [정적] |
| 〃 | `+0x1e6` · `+0x1ff ~ +0x265` | `+0x1ed` · 각 `+7` | `GetModuleHandleA("user32.dll")` · `GetProcAddress` ×5 (반환 안 봄) | [정적] |
| 〃 | **`+0x280`** | **`+0x287`** | **`CreateICA("DISPLAY",0,0,0)`** → `DesktopIc`, **`+0x296 JZ → +0x7d1`** (다음 벽) | [정적] |
| 〃 | `+0x2a4` · `+0x2cb` | `+0x2ab` · `+0x2d2` | `GetDeviceCaps(hdc, 0x58 / 0x5a)` — ≤ 0 이면 96.0, 실패 아님 | [정적] |
| 〃 | `+0x312 ~ +0x5dc` | — | `GpMallocEx` · `GpDevice` · 드라이버 여섯 (`DriverGdi` ×2 · `DriverD3D` · `DriverInfo` · `DriverMeta` …) · `DpBitmap` ×2 · `CheckValid<…>` — NULL 이면 `+0x341 · +0x36c · +0x3b7 · +0x3f3 · +0x42d · +0x467 · +0x4a1 · +0x4db · +0x518 · +0x5e3` → `+0x7d1` | [정적] |
| 〃 | `+0x62b` · `+0x73d` · `+0x775` · `+0x7b3` | `+0x630` · `+0x742` · `+0x77a` · `+0x7b8` | `InitImagingLibrary` · `InitSystemFontsDirs` · `GetGpInstalledFontCollection` · `BackgroundThreadStartup` — 0 / NULL 이면 `+0x632` · `+0x744` · `+0x784` · `+0x7ba` → `+0x7d1` (그 밖 `+0x7ab` 하나) | [정적] |
| 〃 | `+0x7bc` | `+0x7c1` | TraceLogging 등록 → `+0x7c1 XOR EAX,EAX` (**0 = 성공**) | [정적] |
| 〃 | **`+0x7d1`** | **`+0x7d6`** | **`InternalGdiplusShutdown`** (`0xe85a8`) → `+0x7d6 MOV EAX,ESI` (= 1) | [정적] |
| `gdiplus!InternalGdiplusShutdown` | `+0x262` | `+0x269` | `DeleteObject(CachedGdiRegion)` — `+0x25d TEST RCX` · `+0x260 JZ` 로 NULL 이면 안 부름 | [정적] |
| 〃 | `+0x608` · `+0x2c` | `+0x60f` · `+0x33` | `HeapDestroy` · `EventUnregister` | [정적] |
| `gdi32!CreateRectRgn` (rva `0x2950`) | `+0x6f` | `+0x74` | `hGetPEBHandle(2, 0)` — 0 이면 `+0x7a JZ → +0x160` | [정적] |
| 〃 | **`+0x16d`** | **`+0x174`** | **`NtGdiCreateRectRgn`** (win32u, 가져오기 칸 `0x180010760`) → `+0x17c JMP → +0x80` (칸 검사) | [정적] |
| 〃 | `+0x102` | `+0x109` | `NtGdiGetEntry` — 칸 번호 ≥ `0x10000` 이고 `+0xd` 가 안 맞을 때 | [정적] |
| 〃 | **`+0x122`** | **`+0x127`** | **`DeleteRegion(H)`** — 실패 도착 `+0x116` 에서 `H ≠ 0` 일 때 | [정적] |
| 〃 | `+0x25d` · `+0x26e` | `+0x262` · `+0x275` | `GdiTrackHCreate` 가 있나 묻는 함수 (gdi32 `0xd4c8` — 같은 주소 이름 2개, 후보 `IsGdiTrackHCreatePresent` · `IsGdiTrackHDeletePresent`) · `GdiTrackHCreate` (지연 가져오기 칸 `0x180028048`) | [정적] |
| 〃 | `+0x291` | `+0x298` | `InitOnceExecuteOnce(CheckIsEdgeGdiProcessOnce)` → `g_pEdgeGdi` 0 이면 `RBX` 반환 | [정적] |
| `gdi32!DeleteRegion` (`0x1470`) | `+0x10b` | `+0x110` | `GdiGetEntry` → `+0x118` 종류 4 · `+0x12a` 윗 16비트 · `+0x13c` 주인 | [정적] |
| 〃 | `+0xe9` `JMP` | — | `NtGdiDeleteObjectApp` (`[0x180010738]`) — `+0x2e CMP [TEB+0x78],0` · `+0x33 JZ → +0xd7` (TEB `+0x78` 0 이면 배치 안 하고 syscall) | [정적] |
| `gdi32!DeleteObject` (`0x1070`) | `+0x46` | `+0x4b` | `InternalDeleteObject` (`0x10f0`) | [정적] |
| `gdi32!InternalDeleteObject` | `+0x129` | `+0x130` | `NtGdiDeleteObjectApp` — 칸 검사 통과 · 스톡 아님 · DC / 영역 / 글꼴 아님일 때 | [정적] |
| `gdi32!GdiValidateHandle` (`0xc5c0`) | `+0x0` `JMP` | — | `[0x180027bf0]` 지연 가져오기 → gdi32full `GdiValidateHandle` | [정적] |
| `gdi32full!GdiValidateHandle` (`0x70180`) | `+0x14` · `+0x3a` | `+0x19` · `+0x41` | `HANDLE_TO_INDEX` (`0x13c60`) · `GdiGetEntry` (gdi32, `[0x1800c1098]`) | [정적] |
| `gdi32!SelectObject` (`0x4150`) | `+0x2c` · `+0x62` | `+0x33` · `+0x69` | `InitOnceExecuteOnce` → `g_pEdgeGdi` 0 이면 `SelectObjectImpl` (gdi32full `0x26210`) | [정적] |
| `gdi32full!SelectObjectImpl` | `+0x240` · `+0x52f` · `+0x5ed` | `+0x247` · `+0x536` · `+0x5f4` | `NtGdiSelectBitmap` · `GdiSetLastError(0x57)` · `NtGdiSelectFont` | [정적] |
| `gdi32full!GetStockObject` (`0x25e90`) | `+0x124` | `+0x12b` | `NtGdiEnsureDpiDepDefaultGuiFontForPlateau` — `i = 17` 칸이 0 일 때만 | [정적] |
| `user32!RegisterClassExWOWW` (`0x1c99c`) | `+0x114` | `+0x119` | `RtlGetExpWinVer` (user32 `0x1cd60`) → `EDI` | [정적] |
| 〃 | **`+0x2d8`** | **`+0x2df`** | **gdi32 `GdiValidateHandle`** (`[0x1800b51b8]`) — `+0x12d CMP RCX,0x1f` · `+0x131 JA` 일 때만 | [정적] |
| 〃 | `+0x32b` | `+0x332` | `RtlSetLastWin32Error(0x57)` — 거절 · `EDI > 0x300` | [정적] |
| `win32kbase!bSetStockObject` (`0x16bb10`) | `+0x24` · `+0x56` | `+0x2b` · `+0x5b` | `W32GetSessionState` · `HmgModifyHandleType` (`0x3ea50`) — `+0x33 BTS R10,0x17` (스톡 비트) 뒤 세션 배열 `+0xbc0` / `+0xbc8` 에 씀 | [정적] |

### 1. `GdiplusStartup` 차례와 첫 가지 (질문 1) [정적]

| 차례 | 부르는 것 | 보는 결과 | 실패 조건 · 반환 |
|---|---|---|---|
| 0 | `GdiplusStartup` 입구 검사 | 포인터 셋 · 입력 `+0x0` 버전 · `+0x10` | `token` · `input` NULL, 또는 `+0x10 ≠ 0` 인데 `output` NULL → **2** (`+0xeb`). 버전이 1 ~ 3 아님 → **`0x11`** (`+0x48`) |
| 1 | `EnterCriticalSection` · `LibraryInitRefCount == 0` | 두 번째 부름부터는 안 들어감 | — |
| 2 | `InternalGdiplusStartup` → `HeapCreate` | NULL | → **1** (`+0x3c → +0x7cc`) |
| 3 | `OSInfo::Initialize` · `GetModuleHandleA(NULL)` · `InitVersionInfo` | **안 봄 (void)** | 에뮬레이터 로그의 `NtQuerySystemInformation(0)` · `(1)` · `NtQueryInformationProcess(0x25)` · `NtQueryLicenseValue` 둘은 이 안 (kernelbase `GetSystemInfo` · `GetVersionExA` 쪽) 일 것 `[추측: kernelbase 줄은 안 읽음]` — 어느 쪽이든 1 을 못 만든다 |
| **4** | **`CreateRectRgn(0,0,1,1)`** | **NULL** | **→ 1 (`+0x1d9 → +0x7d1`) ← 첫 가지** |
| 5 | `CreateICA("DISPLAY")` | NULL | → 1 (`+0x296`) — **다음 벽** (Q278 N1) |
| 6 | `GetDeviceCaps(0x58 / 0x5a)` | ≤ 0 이면 96.0 | 실패 아님 |
| 7 | `GpDevice` · 드라이버 · 표면 · `InitImagingLibrary` · `InitSystemFontsDirs` · `GetGpInstalledFontCollection` · `BackgroundThreadStartup` | NULL / 0 | → 1 (Q278) |
| 8 | TraceLogging 등록 | — | **0** |

- **에뮬레이터 로그와 맞물림** `[정적 + 요청 쪽 로그]`: 로그에서 `NtGdiCreateRectRgn` 바로 뒤 `NtGdiDeleteObjectApp` 이 나오고 `NtGdiOpenDCW` (`CreateICA` 안) 가 없다 — 위 차례 4 에서 멈춘 것과 맞는다.
- **로그의 나머지 syscall 출처** `[추측]`: 첫 `NtAllocateVirtualMemory · NtFreeVirtualMemory · NtAllocateVirtualMemory` 는 `HeapCreate` 안, 끝 `NtFreeVirtualMemory` 는 `InternalGdiplusShutdown +0x608 HeapDestroy`. `NtProtectVirtualMemory` ×2 는 지연 가져오기 풀기 꼴 (run86 A6) — `CreateRectRgn` 꼬리의 `GdiTrackHCreate` (지연 가져오기 칸) 쪽이 후보 `[공백: 정적으로 못 정함 → Q277]`.

### 2. 첫 가지 안쪽 — gdi32 `CreateRectRgn` (734 B) [정적]

gdi32full 의 `CreateRectRgn` 은 6 B 썽크 (`0x87520`), 몸은 gdi32 에 있다.

| 차례 | 하는 것 | 줄 |
|---|---|---|
| a | 좌표가 범위 밖이면 `TEB+0x68 = 0x57` 후 0 | `+0xa ~ +0x54` · `+0x130 ~ +0x140` |
| b | `hGetPEBHandle(2, 0)` — PEB `+0x140` (`GdiHandleBuffer`) 영역 캐시. 새 프로세스는 개수 칸 0 이라 0 반환 (그 사이 PEB `+0x140` 첫 qword 에 TEB 주소를 `CMPXCHG` 로 잠깐 썼다가 `+0x159` 에서 0) | `+0x6f` · `+0x7a JZ → +0x160` · `hGetPEBHandle +0x57` · `+0x62` · `+0x66` · `+0x159` |
| c | **`NtGdiCreateRectRgn(0,0,1,1)`** → `H` | `+0x16d` · `+0x17c JMP → +0x80` |
| d | 칸 번호 = `((H>>8) & 0xff0000) \| (H & 0xffff)`. `0x10000` 미만이면 그대로 | `+0x80 MOV R9,[pGdiSharedHandleTable]` · `+0x9d JC` |
| e | 번호 ≥ `gMaxGdiHandleCount` (`0x1000000`) → 실패 | `+0xbd` · `+0xc3 JNC → +0x116` |
| f | 칸 24 B 를 `표 + 번호*0x18` 에서 복사 (`≥ 0x10000` 이고 `+0xd` 가 안 맞으면 `NtGdiGetEntry`) | `+0xda` · `+0xe0` · `+0x102` · `+0x181 ~ +0x196` |
| g | **종류**: 칸 `+0xe` byte == **4** | `+0x19c` · `+0x1a1 JNZ → +0x116` |
| h | **윗 16비트**: 칸 `+0xc` word == `H >> 16` | `+0x1a7 ~ +0x1ae` · `+0x1b3 JNZ → +0x116` |
| i | **주인**: (칸 `+0x8` dword & ~1) == `gW32PID` — **0 은 안 됨** | `+0x1b9 ~ +0x1c0` · `+0x1c6 JNZ → +0x116` |
| j | **사용자 쪽 포인터**: 칸 `+0x10` ≠ 0, 풀어서 (`ROR (0x40 − (gCookie & 0x3f))` 뒤 `XOR gCookie`) 도 ≠ 0 | `+0x1cc ~ +0x1d4` · `+0x1da ~ +0x1f3` |
| k | 풀린 포인터의 0x18 B 에 영역 모양을 **씀** (`+4` 종류 · `+8 ~ +0x14` 사각형) — 그래서 쓰기 가능해야 함 | `+0x1f9 ~ +0x256` (`+0x227 · +0x233 · +0x23a · +0x241 · +0x245`) |
| **실패** | e · g · h · i · j 어디서든 **`+0x116`** → `H ≠ 0` 이면 `DeleteRegion(H)` → `RBX = 0` → NULL | `+0x116` · `+0x122` · `+0x129` · `+0x12b JMP → +0x25d` |
| 꼬리 | `GdiTrackHCreate` (켜져 있으면) · `InitOnceExecuteOnce(CheckIsEdgeGdiProcessOnce)` → `g_pEdgeGdi` 0 이면 반환 | `+0x25d ~ +0x2a7` |

- `DeleteRegion` (`0x1470`) 은 같은 칸 검사를 한 번 더 하고, **`TEB+0x78 ≠ 0` · 배치 자리 남음 · 칸 유효** 면 syscall 없이 TEB `GdiTebBatch` (`+0x2f0`) 에 쌓는다 (`GdiBatchLimit` 개 모이면 `NtGdiFlush`). 에뮬레이터는 `TEB+0x78 = 0` (run142) 이고 칸도 0 이라 `+0xe9` 에서 syscall `NtGdiDeleteObjectApp` 이 나간다 — 로그와 맞음.
- **성공하려면** (`H` = `NtGdiCreateRectRgn` 반환, `i = H & 0xffff < 0x10000`, 칸 = `PEB[+0xf8] + i*0x18`): `+0x8` = PID & ~3 (`gW32PID`) · `+0xc` = `H >> 16` · `+0xe` = 4 · `+0x10` = `ROR64(ptr ^ gCookie, gCookie & 0x3f)` (`ptr` = 쓰기 가능 ≥ 0x18 B) · `H` 24 ~ 31비트 = 칸 `+0xd` (0 이 쉬움). PEB `+0x140` 은 0 그대로 + 쓰기 가능.
- 진짜 Windows 에서 이 칸을 채우는 쪽 (이름만): `win32kbase!NtGdiCreateRectRgn` (`0xfe1c0`) → `HmgAllocateObjectAttr` (`0xfe380`) · `HmgInsertObjectInternal` (`0x2b4a0`) `[이름만]`.

### 3. 공유 핸들 표 — 칸 · 번호 식 · 핸들 비트 (질문 2) [정적]

- **표 시작 = PEB `+0xf8` 값 그대로.** `gdi32full!GdiProcessSetup +0x431 ~ +0x449` 가 gdi32 `pGdiSharedMemory` (`0x25778`) 와 `pGdiSharedHandleTable` (`0x257a8`) 에 같은 값, `pGdiDevCaps` 에 `+0x180000` (`+0x456`, run86).
- **칸 = 0x18 B** — 읽는 쪽이 모두 `LEA RAX,[idx+idx*2]` 뒤 `*8` (`GdiGetEntry +0x15 / +0x19`, `HANDLE_TO_INDEX +0x20 / +0x2c`). `+0x180000` 에서 DevCaps 가 시작하니 칸 수는 `0x10000` `[추측: 나눗셈으로만 — 코드의 상한 비교는 gMaxGdiHandleCount 0x1000000 뿐]`.
- **칸 번호 식** (`HANDLE_TO_INDEX` — gdi32 `0x2e70` · gdi32full `0x13c60`, 같은 식):
  - `ext = ((H >> 8) & 0xff0000) | (H & 0xffff)` (`+0x0 ~ +0xd`)
  - `ext < 0x10000` → `H & 0xffff` (`+0xf` · `+0x14 JC`)
  - 아니면 칸 `[H & 0xffff] + 0xd` byte == `ext >> 16` 일 때만 `H & 0xffff` (`+0x2c ~ +0x35`), 아니면 `ext` (표 밖 → `NtGdiGetEntry`)
- **핸들 비트**: 0 ~ 15 칸 번호 · 16 ~ 22 종류 · 23 스톡 · 24 ~ 31 고유 번호.
  - 종류 = `H & 0x7f0000`: `0x10000` DC · `0x40000` 영역 · `0x50000` 비트맵 · `0x80000` 팔레트 · `0x90000` 색공간 · `0xa0000` 글꼴 · `0x100000` 붓 · `0x300000` 펜 · `0x500000` 확장 펜 · `0x210000` `0x260000` `0x460000` `0x660000` 메타파일 쪽 (`GetObjectType` · `InternalDeleteObject` · `SelectObjectImpl` · `GetObjectW` 가 직접 가름).
  - 스톡 = `0x800000`: `win32kbase!bSetStockObject +0x33 BTS R10,0x17` 로 켜고 세션 배열 (`+0x58 → +0xbc0` / `+0xbc8`) 에 쓴 뒤 `+0x56 HmgModifyHandleType`.
  - **핸들 윗 16비트 = 칸 `+0xc` word**: `win32kbase!HmgpComputeHandleValue +0x4 ~ +0x15` = `(칸 +0xc word << 16) | 번호` (부호 확장).

| 칸 오프셋 · 크기 | 뜻 (이름 `[추측]`) | 검사 | 줄 |
|---|---|---|---|
| `+0x00` qword | 커널 쪽 객체 주소 `[추측]` | 유저 쪽은 복사만, 읽는 줄 못 찾음 — 0 이어도 됨 | `GdiGetEntry +0x3b ~ +0x43` |
| `+0x08` dword | **주인 프로세스** (하위 1비트 무시) | `& ~1 == gW32PID` (`= TEB +0x40 & ~3`, `GdiProcessSetup +0x142 ~ +0x14f`). **0 도 받나가 함수마다 다름** (아래 표) | `GdiValidateHandle +0x5c ~ +0x69` |
| `+0x0c` word | **핸들 윗 16비트** (종류 · 스톡 · 고유). 윗 바이트 `+0xd` 는 `HANDLE_TO_INDEX` 가 따로 읽음 | `== H >> 16` (`InternalDeleteObject` · `SelectObjectImpl` 객체 쪽은 스톡 비트 `0x80` 차이만 봐줌) | `GdiValidateHandle +0x4a ~ +0x53` · `InternalDeleteObject +0x6c ~ +0x73` · `SelectObjectImpl +0xd0 ~ +0xdb` |
| `+0x0e` byte | **종류 번호** (DC 1 · 영역 4 · 비트맵 5 · 팔레트 8 · 글꼴 `0xa` · 붓 · 펜 `0x10` …) | `== (H >> 16) & 0x1f` 또는 고정값. `GdiValidateHandle` 은 안 봄 | `InternalDeleteObject +0x5c / +0x5e` · `SelectObjectImpl +0xc1` · `CreateRectRgn +0x19c` |
| `+0x0f` byte | 비트 1 이 켜지면 `SelectObject(hdc, 글꼴)` 거절 `[뜻 공백]` | 비트 1 == 0 | `SelectObjectImpl +0x398` · `+0x39d` |
| `+0x10` qword | **사용자 쪽 속성 블록 포인터** (가려짐). 영역 → 0x18 B, DC → `DC_ATTR` (≥ 0x130 B — `+0x98` 바뀜 표시 · `+0xa0` 붓 · `+0xa8` 펜 · `+0x128` 글꼴) | 풀어서 ≠ 0 | `CreateRectRgn +0x1cc ~ +0x1f3` · `SelectObjectImpl +0x1f2 ~ +0x217` |

누가 칸의 무엇을 보나 (`✓` 검사 · `—` 안 봄 · "0 ok" = 주인 칸 0 도 통과):

| 함수 (모듈 rva) | `+0xe` 종류 | `+0xc` 윗 16비트 | `+0x8` 주인 | `+0x10` 포인터 | 통과 못 하면 |
|---|---|---|---|---|---|
| `GdiValidateHandle` (gdi32full `0x70180`) | — | ✓ 정확 | ✓ PID **또는 0** | — | FALSE (`+0x82`). 핸들 0 → TRUE (`+0x12 → +0x71`) |
| `CreateRectRgn` (gdi32 `0x2950`) | ✓ == 4 | ✓ 정확 | ✓ PID (0 안 됨) | ✓ | `DeleteRegion` + NULL |
| `DeleteRegion` (gdi32 `0x1470`) | ✓ == 4 | ✓ 정확 | ✓ PID (0 안 됨) | ✓ | 배치 못 하고 syscall |
| `InternalDeleteObject` (gdi32 `0x10f0`) | ✓ == `(H>>16)&0x1f` | ✓ 스톡 비트 빼고 | ✓ PID **또는 0** (`+0x86 → +0x284`) | 붓 · 펜 배치 때만 | **FALSE, syscall 없음** (`+0x14b`). 통과 + 스톡 비트 (`+0xcc BT RDI,0x17`) → **TRUE, syscall 없음** (`+0x27a`) |
| `GetObjectType` (gdi32full `0x311e0`) | — (핸들 비트) | ✓ 정확 | ✓ PID **또는 0** | — | 0, syscall 없음 (DC 만 `NtGdiGetDCDword`) |
| `SelectObjectImpl` 객체 (gdi32full `0x26210`) | ✓ == `(h>>16)&0x1f` | ✓ 스톡 비트 빼고 | ✓ PID **또는 0** | — | 0, `GetLastError` 안 세움 (`+0x30f`) |
| 〃 DC | ✓ == 1 (`+0x1ba`) | ✓ 정확 (`+0x1cc`) | ✓ PID (0 안 됨, `+0x1e7`) | ✓ | **`GdiSetLastError(0x57)` + 0** (`+0x52a`) |
| `GetDeviceCaps` · `GetTextMetricsW` · `GetTextExtentPointW` · `GdiGetCharDimensions` (DC) | ✓ == 1 | ✓ 정확 | ✓ PID | ✓ | 0 (+ `0x57` — `GetDeviceCaps` · `GetTextExtentPointW`) / 아무것도 안 씀 (`GdiGetCharDimensions`) |
| `GetStockObject` · `GetObjectW` | — | — | — | — | 표 안 봄 |

(줄 파일: `ms/disassemble/gdi32/{CreateRectRgn,DeleteRegion,InternalDeleteObject,GetDeviceCaps,GdiGetEntry,HANDLE_TO_INDEX}.asm` · `ms/disassemble/gdi32full/{GdiValidateHandle,GetObjectType,SelectObjectImpl,GetTextMetricsW,GetTextExtentPointW,GdiGetCharDimensions,HANDLE_TO_INDEX}.asm`. 결과서 표 (2b) 를 줄였다.)

### 4. 스톡 객체 — `GetStockObject` 가 읽는 배열 [정적]

- `i > 0x15` → 0 (`+0x11` · `+0x14 JA`).
- 붓 · 펜 · 팔레트 등: **`[PEB+0xf8 값 + 0x1800b0 + 8*i]` 를 그대로 돌려줌** (`+0x65 ~ +0x72`). 표 칸은 안 본다.
- 글꼴 `i = 10 · 13 · 16`: 스레드 DPI 인식이 없으면 `+0x180160 + 8*i` 를 먼저 (`+0x7c ~ +0x87`), 0 이면 위 배열 (`+0x8a JZ`).
- GUI 글꼴 `i = 17`: `GetDpiDepStockObjectPlateauSlot` 로 `+0x180138` · `+0x1801e8` · `+0x180210 + 8*슬롯` 중 하나. 칸이 0 이면 `+0x124 NtGdiEnsureDpiDepDefaultGuiFontForPlateau`.
- 읽는 끝: 붓 · 펜 · 팔레트만 `+0x180158` · 글꼴까지 `+0x1801e8` · GUI 글꼴까지 `+0x180290`.
- **스톡 핸들 꼴** `[정적: 비트 규칙으로 만든 예, 진짜 값 아님]`: `번호 | (종류 | 0x80) << 16 | 고유 << 24` — 붓 `0x900000` · 펜 `0xb00000` · 글꼴 `0x8a0000` · 팔레트 `0x880000`. 예: 번호 `0x10` · 고유 1 · 붓 → `0x01900010`. 칸 `+0xc` word = `H >> 16`.
- 그 칸을 채우는 쪽 (이름만): `win32kbase!GdiProcessCallout` (`0xe2010`, `+0x22b` PEB `+0xf8`, run86) · `bSetStockObject` (호출자 `bInitBrush` · `bInitBRUSHOBJ` · `bInitPALOBJ` · `bInitICM` · `bInitBMOBJ`) · `GreGetStockObject` (`0x65870`) · win32kfull `bInitStockFontsInternal` (`0x40962c`) · `bSetStockFont` (`0x128378`). 세션 배열 (`+0xbc0`) 과 공유 `+0x1800b0` 이 같은 메모리인지는 `[공백]` (Q278).

### 5. `RegisterClassExWOWW` 의 배경 붓 검사 · AT03 (질문 2) [정적]

| 차례 | 줄 | 뜻 |
|---|---|---|
| 1 | `+0x114 RtlGetExpWinVer` → `+0x119 MOV EDI,EAX` | 앱이 기대하는 버전 = hInstance PE 머리 `+0x48` (Major) `<< 8 \| +0x4a` (Minor). hInstance NULL · 머리 없음 · Major 1 이면 `0x30a` (`RtlGetExpWinVer +0x10 ~ +0x41`) |
| 2 | `+0x129 MOV RCX,[RBX+0x30]` · `+0x12d CMP RCX,0x1f` · **`+0x131 JA → +0x2d8`** | `hbrBackground`. **0 ~ `0x1f` (COLOR 번호 + 1) 은 검사 안 함** |
| 3 | **`+0x2d8 CALL [0x1800b51b8]`** = gdi32 `GdiValidateHandle` | 썽크 `JMP [0x180027bf0]` → 지연 가져오기 → gdi32full `0x70180` |
| 4 | `+0x2e4 TEST EAX,EAX` · `+0x2e6 JNZ → +0x137` | 통과 → 등록 계속 |
| 5 | `+0x2ec CMP EDI,0x300` · **`+0x2f2 JA → +0x326`** | 버전 > 3.0 → `+0x32b RtlSetLastWin32Error(0x57)` · `+0x337` 0 · `+0x33b JMP → +0x26d` = **0, `ERROR_INVALID_PARAMETER`** |
| 6 | `+0x2f4 MOV [RBX+0x30],R12` · `+0x2f8 JMP → +0x137` | 버전 ≤ 3.0 → 붓을 0 으로 바꾸고 계속 |

- 같은 `GdiValidateHandle` 을 부르는 user32 자리는 9곳 (`_refs/0xb51b8.tsv`): `DT_InitDrawTextInfo` · `RegisterClassExWOWW` · `RegisterClassExWOWA` · `ValidateMENUITEMINFO` ×3 · `SetClassLongPtrW` · `SetClassLongPtrA` · `ValidateMENUINFO`.
- run131 의 combase `OleMainThreadWndClass` 는 `hbrBackground = 2` 라 (run131 ①) 이 검사를 **안 탄다** — run131 에 고칠 것 없음.
- **AT03 (코드로)**: 스톡 붓 칸 값 `0x21400 + i*0x400` → `> 0x1f` → `+0x2d8` → 번호 `0x1400 + i*0x400` (< `0x10000`) → 표 칸 (`+0x1e000 + i*0x6000`) 이 0 → `GdiValidateHandle +0x4e` 에서 `+0xc` word `0` ≠ `H>>16 = 0x0002` → FALSE → 버전 > 3.0 → `0x57`. 칸을 0 으로 두면 `GetStockObject` 가 0 → `CMP RCX,0x1f` 에서 빠짐 → 성공. **원인 = 배열만 채우고 표 칸은 안 채움.**

### 6. 표가 비면 syscall 없이 실패하는 함수 (질문 3) [정적]

| 함수 | 표 칸이 0 이면 | syscall |
|---|---|---|
| `GdiValidateHandle` | FALSE | 없음 |
| `DeleteObject` → `InternalDeleteObject` | FALSE (오류 안 세움) | 없음 |
| `DeleteObject(스톡)` (칸 정상) | TRUE | 없음 |
| `SelectObject(hdc, 객체)` — 객체 칸 없음 | 0 (오류 안 세움) | 없음 |
| `SelectObject(hdc, 객체)` — DC 칸 없음 | 0, `0x57` | 없음 |
| `SelectObject(hdc, 붓 · 펜)` — 칸 정상 | 이전 붓 · 펜 (DC 사본 `+0xa0` / `+0xa8`) | **없음** (`+0x98` 에 바뀜 표시) |
| `SelectObject(hdc, 글꼴)` — 칸 정상 | 이전 글꼴 | 대개 없음 (TEB 배치, 아니면 `+0x5ed NtGdiSelectFont`) |
| `SelectObject(hdc, 비트맵)` | — | `+0x240 NtGdiSelectBitmap` |
| `GetObjectType` | 0 | 없음 (DC 만 `NtGdiGetDCDword`) |
| `GetObjectW` | 표 안 봄 — 글꼴 · 붓 · 펜 + `pv == NULL` 은 크기만, 영역 · DC · 메타파일 등은 `GdiSetLastError` + 0, 나머지 `+0x64 NtGdiExtGetObjectW` | 경우에 따라 |
| `GetStockObject` | 표 안 봄 | 없음 (`i = 17` 칸 0 일 때만) |
| `GetDeviceCaps(DC)` | 0, `TEB+0x68 = 0x57` | 없음 (통과해도 `0x58` · `0x5a` 는 `+0x1cb NtGdiGetDeviceCaps`) |
| `GetTextMetricsW(DC)` · `GetTextExtentPointW(DC)` · `GdiGetCharDimensions` | 0 (`GetTextExtentPointW` 는 `0x57`) · 아무것도 안 씀 | 없음 |
| `CreateRectRgn` | NULL | **있음** (`NtGdiCreateRectRgn` + `NtGdiDeleteObjectApp`) |
| `DeleteRegion` / `DeleteObject(영역)` — 칸 정상 + `TEB+0x78 ≠ 0` | TRUE | 없음 (TEB 배치 → `GdiBatchLimit` 개면 `NtGdiFlush`) |

### 7. sast/139 (run142) 에서 넘어온 GDI 몫 중 닫은 것 [정적]

`MessageBoxW` 길 (run142 최소 길 표 차례 8 · PE_emulator 6) — 값은 안 봤고 "표를 보는 자리" 만 닫았다.

- `NtUserGetDCEx(0,0,3)` 의 HDC 는 종류 `0x10000` 이고 표에 **DC 칸** (`+0xe == 1` · `+0xc == H>>16` · `+0x8 & ~1 == PID` 정확히 · `+0x10` 풀면 ≠ 0 인 `DC_ATTR`) 이 있어야 `SelectObject` · `GetTextExtentPointW` · `GetTextMetricsW` · `GdiGetCharDimensions` · `GetDeviceCaps` 가 읽는다. 없으면 `GdiGetCharDimensions` 가 말없이 돌아가 `SoftModalMessageBox +0x151 · +0x153 JZ → +0x69f` 가 폭 0 으로 `MessageBoxW` 를 0 으로 끝낸다.
- HFONT (`NtGdiHfontCreate`) 는 종류 `0xa0000` + **글꼴 칸** (`+0xe == 0xa` · `+0xc == H>>16` · 주인 PID 또는 0 · `+0xf` 비트 1 꺼짐) 이 있어야 `SelectObject(hdc, 글꼴)` 가 이전 글꼴을 돌려주고 DC 사본 `+0x128` 을 바꾼다.
- `DeleteObject(hfont)` 는 칸이 있으면 `DeleteFont` (gdi32full) 쪽 (`InternalDeleteObject +0xe4 CMP EBX,0xa0000` · `+0xea JZ → +0x14f`).
- 남은 것 (`pcfLocateCFONT` · `CFONT` · `bComputeTextExtent` · `bGetTextMetricsWInternal` 이 syscall 없이 폭을 주는 조건 · `DC_ATTR` 전체 · `NtUserGetDCEx` 가 DC 칸을 만드는 쪽) → Q278.

### 호출 관계

```
gdiplus!GdiplusStartup (export 0xe1170)                                                  [정적]
 ├─ (1) +0x60   EnterCriticalSection        LibraryInitRefCount == 0 일 때만 아래
 └─ (2) +0x77   InternalGdiplusStartup (0xe1380)           실패 가지 18곳 → 모두 1 (ESI = 1)
                 ├─ (1) +0x2a   HeapCreate(0,0,0)            NULL → +0x3c → +0x7cc (1)
                 ├─ (2) +0x190  OSInfo::Initialize           void (GetSystemInfo · GetVersionExA · IsProcessorFeaturePresent)
                 ├─ (3) +0x1b9  InitVersionInfo              void (GetVersionExA · GetACP)
                 ├─ (4) +0x1ca  gdi32!CreateRectRgn(0,0,1,1)        NULL → +0x1d9 → +0x7d1 (1)   ← 첫 가지
                 │               ├─ (1) +0x74   hGetPEBHandle(2,0)        PEB+0x140 캐시 비었음 → 0
                 │               ├─ (2) +0x174  NtGdiCreateRectRgn        ── SYSCALL (win32u)   → H
                 │               │              +0x80 ~ +0x1f3  PEB+0xf8 표 칸[H & 0xffff] (0x18 B) 검사
                 │               │                 +0x19c 종류 == 4 · +0x1ae +0xc == H>>16 · +0x1c0 주인 == PID · +0x1d4/+0x1f3 포인터
                 │               └─ (3) +0x127  DeleteRegion(H)    ← +0x116 (검사 실패)
                 │                               +0x2e TEB+0x78 == 0 → +0xe9 JMP NtGdiDeleteObjectApp  ── SYSCALL
                 ├─ (5) +0x287  CreateICA("DISPLAY")         NULL → +0x296 → +0x7d1 (1)   ← 다음 벽 (Q278)
                 ├─ (6) +0x2ab · +0x2d2  GetDeviceCaps(0x58 · 0x5a)
                 ├─ (7) +0x630 · +0x742 · +0x77a · +0x7b8  InitImagingLibrary · InitSystemFontsDirs · GetGpInstalledFontCollection · BackgroundThreadStartup
                 └─ (8) +0x7d6  InternalGdiplusShutdown (실패일 때)  +0x25d CachedGdiRegion NULL → DeleteObject 안 부름 · +0x60f HeapDestroy

user32!RegisterClassExWOWW (0x1c99c)                                                     [정적]
 ├─ (1) +0x119  RtlGetExpWinVer → EDI
 └─ (2) +0x2df  gdi32!GdiValidateHandle (JMP 지연) ⇒ gdi32full!GdiValidateHandle (0x70180)    ← +0x12d hbrBackground > 0x1f 일 때만
                 ├─ (1) +0x19   HANDLE_TO_INDEX
                 └─ (2) +0x41   gdi32!GdiGetEntry      표 칸 복사
                        +0x4e +0xc == H>>16 · +0x63 (+0x8 & ~1) == gW32PID 또는 0   아니면 FALSE
                 FALSE + EDI > 0x300 → +0x332 RtlSetLastWin32Error(0x57) → 0
```

- (오프셋은 ret 자리. `⇒` 는 지연 가져오기 썽크를 건너뜀. 콜스택으로 본 적 없음.)

## PE_emulator 쪽 (결과서 "지금 우리 맞나 · 틀리나")

flow-writer 는 `../PE_emulator` 를 줄 번호만 훑었다 (읽기만). 고칠지는 사용자 · PE_emulator 결정.

| 쓰는 곳 | 판정 | 이유 |
|---|---|---|
| `nt_gdi_init2.cpp` (공유 메모리 블록 — 표 0) | **모자람** | 블록 `0x180150` 은 `DllMain` 까지만 맞음 (run86). 표 (`+0x0 ~ +0x180000`, 칸 0x18 B) 가 0 이면 ① `CreateRectRgn` NULL (= `GdiplusStartup` 1) ② `GdiValidateHandle` FALSE (`RegisterClassEx` `0x57`) ③ DC 길 `0x57` / 0. `NtGdiInit2` 반환 (= `gCookie`) 은 0 아님 + **하위 6비트 0** 이면 포인터 가리기가 `ptr ^ gCookie` 로 끝남 |
| `nt_gdi_create_rect_rgn.cpp` (핸들 `0x20800`) | **틀림 (둘)** | ① 표 칸을 안 채움 (`+0x8` PID · `+0xc` `H>>16` · `+0xe` 4 · `+0x10` 가린 포인터). ② **종류 비트 틀림** — `0x20800` 의 16 ~ 22비트는 `2`, 영역은 `4`. 칸 `+0xe` 를 4 로 두면 `CreateRectRgn` 은 넘어도 `DeleteObject` 가 `InternalDeleteObject +0x5c / +0x5e` (`+0xe == (H>>16)&0x1f`) 에서 FALSE. 꼴은 **`번호 \| 0x040000 \| (고유 << 24)`** |
| `NtGdiDeleteObjectApp` 새 stub | **반환 1 이어야** | 유효한 객체에 0 이면 `DeleteObject` 가 FALSE (`InternalDeleteObject +0x129` 반환을 그대로 씀). `GdiplusStartup` 의 `DeleteRegion` 은 반환을 안 씀. 칸 지우기는 선택. `TEB+0x78 ≠ 0` 이 되면 영역 · 붓 · 펜 지우기는 TEB 배치 → `NtGdiFlush` 로 가서 이 stub 이 안 불림 (지금은 0 이라 매번 불림 — 맞음) |
| 보류 패치 `AT03_stock-objects_held.patch` | **원인 확인, 고칠 자리 있음** | 배열만 채우고 표 칸을 안 채움. 아래 최소 목록을 같이 넣으면 통과. 지금 값 `0x21400 + i*0x400` 은 윗 16비트 `0x0002` (붓 아님) — `GdiValidateHandle` 은 종류를 안 봐 통과엔 지장 없지만, `GetObjectType` · `DeleteObject` (스톡 비트 없음 → `+0xcc` 에서 TRUE 로 안 감) 가 달라지니 **`0x900000 \| 번호 \| 고유 << 24`** 꼴이 안전 |

**스톡 객체를 채우면서 `RegisterClassEx` 도 통과시키는 최소 표 칸** (붓 `i = 0 ~ 5` · `0x12` 기준, 펜 · 글꼴 · 팔레트는 종류만 다름):

1. PEB `+0xf8` → 읽기 가능 블록, 크기 ≥ `0x180160` (붓 · 펜 · 팔레트) · `0x1801e8` (+ 글꼴) · `0x180290` (+ GUI 글꼴).
2. `+0x1800b0 + 8*i` ← 스톡 핸들 `H_i`. `0x12` · `0x13` 은 0 아님 (`DllMain`).
3. `H_i` = 번호 `idx_i < 0x10000` (서로 다름) · 종류 `0x100000` · 스톡 `0x800000` · 고유 아무거나 (예 `0x01900010`).
4. 칸 `PEB[+0xf8] + idx_i*0x18`: `+0x08` = 0 (주인 0 은 `GdiValidateHandle` · `GetObjectType` · `DeleteObject` · `SelectObject(객체)` 가 받음) · `+0x0c` = `H_i >> 16` · `+0x0e` = `(H_i >> 16) & 0x1f` (붓 `0x10`) · `+0x00` · `+0x0f` · `+0x10` = 0.
5. 이미 되어 있어야 하는 것: `gMaxGdiHandleCount` (`GdiDllInitialize` 가 `0x1000000`) · `gW32PID` (`GdiProcessSetup`) · gdi32 `GdiValidateHandle` 지연 가져오기 풀기.
6. → `GdiValidateHandle(H_i)` TRUE → `RegisterClassExWOWW +0x2e6 JNZ` 통과.
7. (`GdiplusStartup` 첫 가지까지) 영역 핸들 `H_r = idx_r | 0x040000 | 고유 << 24`, 칸 `+0x08` = PID & ~3 · `+0x0c` = `H_r >> 16` · `+0x0e` = 4 · `+0x10` = `ROR64(ptr ^ gCookie, gCookie & 0x3f)` (`ptr` = 쓰기 가능 ≥ 0x18 B). PEB `+0x140` 은 0 그대로 + 쓰기 가능. 그 뒤 다음 벽 = `CreateICA("DISPLAY")`.

- **flow-writer 보탬 (`../PE_emulator` 줄 번호 훑기)**:
  - `nt_gdi_init2.cpp:33` 반환 `1` → `gCookie = 1` (하위 6비트 = 1). 위 7 의 가리기는 `ROR64(ptr ^ 1, 1)` 이 된다 — 결과서 권고 (하위 6비트 0) 를 따르면 `XOR` 만.
  - `nt_gdi_init2.cpp:28` 지금 스톡 `0x12` · `0x13` 칸 값이 `0x12` · `0x13` (≤ `0x1f`). `DllMain` 의 "0 아님" 은 넘지만, 그 값이 배경 붓으로 쓰이면 `RegisterClassExWOWW +0x12d` 에서 COLOR 번호로 읽혀 검사를 건너뛴다 `[정적: 비교만]`.
  - `nt_gdi_init2.cpp` 머리 상수 주석 "읽는 끝 `+0x180150` → 쪽 올림 `0x181000`" — 쪽 단위로 잡으면 위 1 의 `0x1801e8` · `0x180290` 은 이미 그 안이다 `[추측: 매핑 함수 몸은 안 읽음]`.
  - `nt_gdi_init2.cpp:27` "게스트엔 읽기 전용" — 표 칸은 유저 쪽이 **읽기만** 하니 괜찮다 `[정적: 읽은 줄 안에서]`. 다만 칸 `+0x10` 이 가리키는 영역 · `DC_ATTR` 블록은 gdi32 가 **쓴다** (`CreateRectRgn +0x227 ~ +0x245`) — 다른 쓰기 가능 메모리여야 한다.
  - `nt_gdi_create_rect_rgn.cpp:7` 의 "`0x400` 배수라 핸들 표가 안 주는 번호" 근거는 nt 객체 핸들 표 (`ExpInsertLowLevelTableIntoFreeList`) 규칙이다. GDI 핸들은 위 "핸들 비트" 규칙이라 그 근거와 무관하다 `[정적: 결과서 비트 규칙과 비교]`.

## 확인된 것

- `[정적]` `GdiplusStartup` = 1 의 첫 가지는 `InternalGdiplusStartup +0x1d9` (`CreateRectRgn` NULL). 그 앞 시스템 정보 · 버전 묻기는 결과를 분기에 안 쓴다. 실패 가지 18곳 모두 1.
- `[정적]` gdi32 `CreateRectRgn` 은 `NtGdiCreateRectRgn` 뒤 PEB `+0xf8` 표 칸을 검사하고 (종류 4 · 윗 16비트 · 주인 PID · 사용자 포인터), 실패면 `DeleteRegion` 을 부르고 NULL — 에뮬레이터 로그의 차례와 같다.
- `[정적]` 표 시작 = PEB `+0xf8`, 칸 0x18 B, 칸 번호 = `H & 0xffff` (24 ~ 31비트는 칸 `+0xd` 와 대조), 핸들 윗 16비트 = 칸 `+0xc`.
- `[정적]` `GdiValidateHandle` 통과 조건 = 핸들 0, 또는 [번호 < `0x1000000` · `GdiGetEntry` 성공 · `+0xc == H>>16` · `+0x8 & ~1` == PID 또는 0].
- `[정적]` `RegisterClassExWOWW` 는 `hbrBackground > 0x1f` 일 때만 검사, 거절 + 버전 > 3.0 → `0x57` + 0. AT03 은 표 칸이 비어서다.
- `[정적]` 스톡 비트 = `0x800000` (win32kbase `bSetStockObject +0x33`). `DeleteObject(스톡)` 은 syscall 없이 TRUE.
- `[정적]` `GetStockObject` · `GetObjectW` 는 표를 안 본다. `DeleteObject` · `SelectObject` · `GetObjectType` · DC 쪽 넷은 본다.
- `[관찰]` test.exe 사진 PEB `GdiSharedHandleTable` (null) (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`) — 이번 표는 test.exe 에선 안 생긴다 (run86 과 같은 줄).

## flow-writer 대조

결과서 오프셋을 `.asm` 과 맞춰 봤다 — **어긋난 줄 없음.** 덧붙임 · 표기 차이만:

1. **실패 가지 수와 반환값** (결과서엔 없음): `InternalGdiplusStartup` 에서 실패로 가는 `JZ` 는 18곳 — `+0x3c` 하나는 `+0x7cc MOV ESI,1` 로, 17곳 (`+0x1d9 · +0x296 · +0x341 · +0x36c · +0x3b7 · +0x3f3 · +0x42d · +0x467 · +0x4a1 · +0x4db · +0x518 · +0x5e3 · +0x632 · +0x744 · +0x784 · +0x7ab · +0x7ba`) 은 `+0x7d1` 로. `ESI` 를 쓰는 줄은 `+0x55 LEA ESI,[R12+0x1]` · `+0x7cc` · (복원) `+0x7dd` 뿐이라 **모두 1** 을 돌려준다.
2. **다음 요청 후보 N2 의 오프셋** (`+0x632` · `+0x744` · `+0x784`) 은 `JZ` 줄, 결과서 표 (1) 차례 11 의 `+0x62b/+0x630` · `+0x73d` · `+0x775` 는 call · ret 줄 — 같은 자리.
3. **alias**: `CreateRectRgn +0x25d` · `DeleteObject +0x4f` 의 callee (gdi32 `0xd4c8`) 는 같은 주소 이름 2개 (`pdb_aliases.py --lookup gdi32.dll 0xd4c8` → `IsGdiTrackHDeletePresent` · `IsGdiTrackHCreatePresent`). 결과서가 쓴 `IsGdiTrackHCreatePresent` 는 그중 하나 — 하는 일은 "`GdiTrackH*` API 세트가 있나 묻기".
4. **`GdiValidateHandle` 의 실패 가지 둘 더** (결과서 표엔 "번호 < `0x1000000`" 만): `+0x24 JNC` (번호 ≥ `gMaxGdiHandleCount`) · `+0x48 JS` (`GdiGetEntry` 음수 = `NtGdiGetEntry` 실패) → `+0x82` FALSE.
5. **gdi32 `SelectObject`** (`0x4150`) 는 `SelectObjectImpl` 앞에 `+0x2c InitOnceExecuteOnce` → `+0x38 g_pEdgeGdi` 를 보고 0 이 아니면 `+0x51` 간접 호출 (`[g_pEdgeGdi + 0x90]`, CFG) 로 간다 — `CreateRectRgn` 꼬리와 같은 "Edge GDI" 갈래. 0 이면 `+0x62 SelectObjectImpl`. 결과서는 이 갈래를 안 적었다 (보통 프로세스는 0 일 것 `[추측]`).
6. **`hGetPEBHandle` 의 PEB `+0x140` 쓰기**: 결과서 "TEB 포인터를 잠깐 씀" = `+0x57 LOCK CMPXCHG [PEB+0x140], GS:[0x30]` (잠금) · `+0x159 MOV [PEB+0x140],0` (풀기). 개수 칸은 `[PEB+0x140 + 8 + 4*종류]` (`+0x62`, 영역 = 종류 2 → `+0x150`).
7. `CreateRectRgn` 의 사용자 포인터 풀기 (`+0x1e1 MOV ECX,0x40` · `+0x1e8 AND EAX,0x3f` · `+0x1eb SUB` · `+0x1ed ROR` · `+0x1f0 XOR`) = `ROL(stored, gCookie & 0x3f) ^ gCookie`. 그래서 가리기 식 `ROR64(ptr ^ gCookie, gCookie & 0x3f)` 가 맞다. 하위 6비트 0 이면 `ROR 0x40` = 회전 없음.

## 앞 run 고쳐 읽기

틀린 것은 없고 `[추측]` · `[공백]` 이 닫힌 것이라 "정정" 이 아니라 "고쳐 읽기" 로 각 문서 맨 위에 한 줄씩 넣었다.

- **run86**: 표 B "`+0x1800b0 + 8*i` (스톡 객체 핸들 표 `[뜻 추측]`)" → 뜻은 `[정적]` — `GetStockObject` 가 그 값을 **그대로 핸들로** 돌려준다. 공유 핸들 표 자체는 같은 메모리 `+0x0 ~ +0x180000` (칸 0x18 B, 칸 번호 = `H & 0xffff`) 이고, "최소 `0x180150` B" 는 `DllMain` 까지만 맞는 말 (표 칸이 0 이면 `CreateRectRgn` · `GdiValidateHandle` · DC 길이 실패). 다음 요청 후보 ⑤ "핸들 표 항목 모양" 일부 닫힘.
- **run142**: 최소 길 표 차례 8 · PE_emulator 6 · N4 의 "GDI (PEB `+0xf8` — Q270)" → `GdiGetCharDimensions` · `GetTextExtentPointW` · `SelectObject` · `GetTextMetricsW` · `GetDeviceCaps` 는 syscall 앞에서 **DC 칸** (`+0xe == 1` · 주인 PID 정확히 · `DC_ATTR` 포인터) 을 본다. 칸이 없으면 `GdiGetCharDimensions` 가 말없이 돌아가 `SoftModalMessageBox +0x153` 이 폭 0 으로 0. DC 칸을 만드는 쪽 · `DC_ATTR` 전체 · 글꼴 사본은 Q278.

## 짐 지도 반영 거리 (판단만, `baggage.md` 안 고침)

- **새 짐 칸 없음, 2장 숫자 그대로일 것.** 이번 길은 test.exe 초기화 밖 (gdi32 · gdiplus 안 올림) 이다.
- 근거 보탬 거리 하나: 짐 지도 3.1 PEB "범위 밖 길에서만 씀 — 다른 DLL · 모듈 12" 의 `+0x140 GdiHandleBuffer` — 쓰는 쪽이 지금 win32kbase `GdiProcessCallout +0xfe` (run113) 뿐인데, gdi32 `hGetPEBHandle +0x57` (잠금으로 TEB 주소) · `+0x159` (0) 도 쓴다 `[정적]`. 칸 수는 그대로, 근거 칸에 한 마디 보탤 거리 — 다음 짐 지도 바퀴에 오케스트레이터가 정함.
- TEB `GdiTebBatch` (`+0x2f0`) 를 gdi32 `DeleteRegion` 이 쓰는 것은 run121 이 이미 셌다 (같은 줄).

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 7 — 대기열 **Q277 (동적: 결과서 동적 1~6 — PEB +0xf8 표 스톡 붓·영역·DC 칸·gCookie · NtGdiCreateRectRgn 핸들 비트 · GetStockObject 값 · RegisterClassExWOWW +0x2e4 EAX·RtlGetExpWinVer · TEB+0x78 · NtQIP(0x25)/NtProtectVirtualMemory ×2 부르는 줄 — Q162·Q273 과 합침)**. test.exe 로는 못 본다 — gdiplus 를 부르는 GUI 프로세스, 그 프로세스 조건, ret 자리):

1. PEB `+0xf8` 표 덤프 — 스톡 붓 칸 (`+0x8` 0 인가 · `+0xc` = `H>>16` · `+0xe` `0x10` · `+0x10` 0), 영역 칸 (`+0x10` 가려진 값), DC 칸. `gCookie` (gdi32 rva `0x257a0`) 와 풀린 포인터.
2. `win32u!NtGdiCreateRectRgn` 반환 비트 (종류 4 · 스톡 0 · 고유), `gdi32!CreateRectRgn +0x19c` 의 `[RSP+0x2e]` · `+0x1ae` 의 `[RSP+0x2c]`.
3. `GetStockObject(0..5, 0x12, 0x13)` 값이 `종류 | 0x800000` 꼴인가, 그 칸 `+0x8`.
4. `user32!RegisterClassExWOWW +0x2d8` 직전 `RCX` · `+0x2e4` `EAX` · `+0x119` `EDI` (일반 앱 `RtlGetExpWinVer`).
5. `DeleteObject(영역)` 때 `TEB+0x78` 이 0 인가 (아니면 TEB `+0x2f0` 배치).
6. `NtQueryInformationProcess(−1, 0x25)` · `NtProtectVirtualMemory` ×2 를 부르는 줄.
7. 멈춤점 후보: `gdi32!CreateRectRgn +0x19c` · `+0x116` · `gdi32full!GdiValidateHandle +0x4a` · `user32!RegisterClassExWOWW +0x2e4` · `gdiplus!InternalGdiplusStartup +0x1d6` · `+0x293`.

정적 (결과서 "다음 요청 후보" N1 ~ N5 — 대기열 **Q278 (정적: 결과서 다음 요청 후보 — N1 CreateICA("DISPLAY")·NtGdiOpenDCW HDC·DC_ATTR·GetDeviceCaps(0x58/0x5a) (바로 다음 벽, 가장 급함) · InitImagingLibrary·InitSystemFontsDirs·GetGpInstalledFontCollection · DC_ATTR 전체·글꼴 사본 · win32k 표 채우기 · NtQIP(0x25) 출처)**):

1. **N1 (바로 다음 벽, 가장 급함)**: `InternalGdiplusStartup +0x296` — gdi32 `CreateICA` (썽크 `0xace0`) → gdi32full `CreateICA` (`0x42c90`) → `bCreateDCA` (`0x42ce0`, `+0xd4`) → `hdcCreateDCW` (`0x42ef0`, `+0xc9 NtGdiOpenDCW`) 가 돌려줄 HDC 꼴 · DC 칸 · `DC_ATTR` 크기와 칸, `GetDeviceCaps(0x58 / 0x5a)` · `GpDevice::GpDevice(hdc)` 가 읽는 칸. (`hdcCreateDCW` 아래는 "도구 메모" 의 스크립트 문제로 일부만 뽑힘.)
2. **N2**: `InitImagingLibrary` (`+0x632`) · `InitSystemFontsDirs` (`+0x744`) · `GetGpInstalledFontCollection` (`+0x784`) 가 0 을 내는 조건 (지도 depth 3 ~ 4).
3. **N3**: DC 칸 · `DC_ATTR` 전체 (run142 N4 남은 몫) — `NtUserGetDCEx` · `NtGdiOpenDCW` 가 만드는 칸, `pcfLocateCFONT` · `CFONT`, `GetDCDpiScaleValue`, `bComputeTextExtent`.
4. **N4**: win32k 쪽 표 채우기 — `GdiProcessCallout` (`0xe2010`) · `HmgInsertObjectInternal` (`0x2b4a0`) · `bSetStockObject` 세션 배열 ↔ 공유 `+0x1800b0` (run86 정적 ① · Q163 ① 과 겹침 — "win32k 안쪽" 이라 사용자 결정).
5. **N5** (낮음): `NtQueryInformationProcess(0x25)` · `NtQueryLicenseValue` 를 부르는 kernelbase `GetVersionExA/W` 줄 (이번 분기엔 안 쓰임).

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 (gdiplus 시작 · gdi32 핸들 검사 · user32 붓 검사) 에서 디버거 여부를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`.
- **스크립트 문제 (기록만)**: `.claude/skills/ghidra-headless-export/scripts/ExportFunctions.java` — gdi32full `^(CreateICA|bCreateDCA|hdcCreateDCW)$` depth 1 export 중 이름이 255 B 를 넘는 C++ 템플릿 함수 (`~unique_storage<struct wil::details::resource_policy<…>>` 류) 를 파일 이름으로 그대로 써서 `java.io.FileNotFoundException … (File name too long)` (`run_ghidra.sh` 종료 코드 4, `ms/ghidra/gdi32full.export.s141b.stdout` 57줄 · `ms/ghidra/gdi32full.export.log`). 목표 셋의 `.c` · `.asm` · `_calls.tsv` 는 쓰였고 그 뒤 depth-1 callee 일부는 안 쓰였을 수 있다. 결과서 제안: 파일 이름을 ~120 자로 자르고 해시 8자리를 붙임 (gdi32full · kernelbase · rpcrt4 의 다른 C++ 함수에도 날 수 있음). → 하네스 제안 (오케스트레이터 · 사용자 몫, flow-writer 는 `.claude/` 를 안 고침).
- (스크립트 문제 아님) gdiplus analyze 로그 `WARN … WindowsResourceReference … osgi.ee=UNKNOWN` 한 줄은 Ghidra 자체 분석기 JDK 문제 (gdi32full 로그에도 같은 줄). `SetPdb` · `ExportIndex` 는 정상 (5239).
- 같은 때 explainer 가 run143 해설서를 썼다 (이 문서와 안 겹침).

# 141 gdi-startup — 정적 분석 결과

- 요청서: `_workspace/sast/141_gdi-startup.md` (원문 `../PE_emulator/_workspace/05_tuning_planner/343_internals-gdi-startup.md` 먼저 읽음, 직전 결과서 `_workspace/sast/139_user32-connect_result.md` 도 읽음)
- 대기열: Q270. 정적만. 진짜 핸들 값 · 표 칸 내용은 하지 않음 (Q162 몫).
- 모듈 / 바이너리 (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`):
  - **gdiplus** (`GdiPlus.dll`) — **처음 analyze** (PDB 받음). 50초 안쪽, `MAXMEM` 기본 8G 로 충분 (메모리 부족 없음). 함수 5239개, `FUN_` 이름 0개, `SetPdb` 줄 있음. 지도: `ms/disassemble/gdiplus/_map/GdiplusStartup_*`.
  - gdi32 · gdi32full · user32 · win32kbase (`win32kbase.sys`) — 이미 analyze 됨, 다시 안 함 (`-readOnly` export · refs 만).
  - 같은 시간에 도는 sast/138 (rpcrt4 · ntdll · ntoskrnl) · sast/140 (kernelbase · mountmgr) 프로젝트는 안 건드림. ntdll 은 `pdb_struct.py` (PDB 파일만 읽음) 로 `_PEB` · `_TEB` 칸 이름만 확인.
- PDB GUID (`ms/sym/*.pdb.info.txt`):
  - gdiplus `DDF19AA09F285660C86065C38EF751A41` (이번에 새로 받음, `gdiplus.pdb`)
  - gdi32 `92C6EC2ABE478836C99BC83C2F3AE3551` · gdi32full `A6245E4F5C78EB06438EA4820F1F6EB41` (둘 다 run86 에 적힌 값과 같음)
  - user32 `CDB947D1B96B87977915DE04CF54279C1` · win32kbase `D22DFDA3D54B87E6BD0567A57877904C1` (sast/139 와 같음)
  - ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준과 같음 (빌드 26100.9457 맞음)
- 뽑은 것 (`ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`; depth 는 gdiplus · gdi32 · gdi32full 의 첫 묶음이 1, 이름 · 꼴 확인용 (win32kbase, 글자 폭 쪽, `GetDpiDepStockObjectPlateauSlot`) 은 0):
  - gdiplus `GdiplusStartup` · `InternalGdiplusStartup` (34개, 첫 가지를 찾은 뒤라 더 안 넓힘)
  - gdi32 `CreateRectRgn` `CreateRectRgnIndirect` `DeleteObject` `InternalDeleteObject` `DeleteRegion` `SelectObject` `GetObjectW` `GetObjectType` `GetStockObject` `GdiValidateHandle`(썽크) `CreateICA`(썽크) `CreateSolidBrush` + 도우미 (`HANDLE_TO_INDEX` `GdiGetEntry` `hGetPEBHandle` `IS_INDEX_IN_USER_SHARED_ARRAY`) · `CheckIsEdgeGdiProcessOnce` · `GetDeviceCaps` · `GetTextMetricsW` `GetTextExtentPointW` `CreateFontIndirectW` `GetCurrentObject` `pldcGet`
  - gdi32full `GdiValidateHandle` `GetObjectW` `SelectObjectImpl` `GetObjectType` (+ 도우미 `HANDLE_TO_INDEX` `GdiGetEntry` …) · `CreateICA` `bCreateDCA` `hdcCreateDCW` · `GetDpiDepStockObjectPlateauSlot` · `GdiGetCharDimensions` `GetTextMetricsW` (둘) `GetTextExtentPointW` `CreateFontIndirectW` `GdiConvertAndCheckDC`
  - win32kbase `bSetStockObject` `GreGetStockObject` `HMValidateSharedHandle` `HmgpComputeHandleValue` `HmgModifyHandleType` `HmgAllocateObjectAttr` (이름 · 꼴 확인용, 깊이 안 들어감)
  - `refs`: user32 `0xb51b8` (gdi32 `GdiValidateHandle` 가져오기 칸), gdi32 `0x27bf0` (그 지연 가져오기 칸) → `_refs/` 아래.
  - 이름 안 맞은 것 없음 (`pdb_aliases.py --where` 안 써도 됨). 스크립트 문제 한 건 → 맨 아래 "스크립트 문제".
- 버전 대조 (gdiplus 는 "알려진 사실" 표에 없어서, 나머지 모듈의 run86 · run131 · sast/139 줄과 맞춤): **전부 일치**
  - `GdiProcessSetup +0x15a MOV RCX,[RAX+0xf8]` · `+0x161 CMP [RCX+0x180094],0` · `+0x456 ADD RCX,0x180000` · `DllMain +0x2e NtGdiInit2` · `GdiDllInitialize +0x16 → 0x1000000` · `GetStockObject +0x65~+0x72` · 가져오기 칸 `0x1800c1fe8` = run86 과 같음
  - 내보내기 표 RVA: gdiplus `GdiplusStartup` `0xe1170` (서수 609) · gdi32full `GdiValidateHandle` `0x70180` · `GetStockObject` `0x25e90` · gdi32 `CreateRectRgn` `0x2950` · `DeleteObject` `0x1070` · `GdiGetEntry` `0x1010` · `GdiValidateHandle` `0xc5c0` · user32 `RegisterClassExW` `0x1c0d0` — `objdump -p` 와 `_index.tsv` 가 같음
  - `user32!RegisterClassExWOWW +0x2d8 GdiValidateHandle` = run131 · sast/139 와 같음
  - PDB 로 `_PEB +0xf8 GdiSharedHandleTable` · `+0x140 GdiHandleBuffer` · `_TEB +0x40 ClientId` · `+0x78 Win32ThreadInfo` · `+0x2f0 GdiTebBatch` · `+0x1740 GdiBatchCount` 확인

## 먼저 읽는 한 줄 답

1. **`GdiplusStartup` 이 1 로 나가는 첫 가지** = `gdiplus!InternalGdiplusStartup +0x1c3 CreateRectRgn(0,0,1,1)` 이 NULL 을 돌려줘서 `+0x1d9 JZ → +0x7d1` (정리 후 1). NULL 이 나오는 자리는 gdiplus 가 아니라 **gdi32 `CreateRectRgn` 안** — `NtGdiCreateRectRgn` 이 준 핸들을 **공유 핸들 표 (PEB `+0xf8`) 에서 다시 읽어 검사**하는데 (`+0x19c` 종류 == 4 · `+0x1ae` 윗 16비트 · `+0x1c0` 주인 PID · `+0x1d4` 사용자 쪽 포인터 ≠ 0) 칸이 0 이라 `+0x116` 로 가서 `DeleteRegion` (= 로그의 `NtGdiDeleteObjectApp(0x20800)`) 을 부르고 NULL 을 돌려준다. 후보 중 **공유 핸들 표 칸이 정답**: 영역 핸들 값의 종류 비트는 안 보고, `NtGdiDeleteObjectApp` 반환 · `0x25` 출력 · 라이선스 값은 이 분기에 안 쓰인다. 로그의 syscall 차례가 이 길과 줄 단위로 맞는다 (`NtGdiOpenDCW` 가 없는 것도 설명됨 — 아직 `CreateICA` 에 못 갔다).
2. **gdi32 가 핸들을 믿는 조건**: 표 시작 = PEB `+0xf8` **그대로** (더하기 없음), 칸 = **0x18 바이트**, 칸 번호 = `핸들 & 0xffff` (핸들 24~31비트가 0 이 아니면 그 칸 `+0xd` 바이트와 같을 때만, 아니면 24비트 번호로 `NtGdiGetEntry` syscall). `GdiValidateHandle` 통과 조건 = 핸들 0 이거나 [번호 < `0x1000000` · 칸 `+0xc` word == 핸들 윗 16비트 · 칸 `+0x8` dword (`& ~1`) 가 `gW32PID` 또는 0]. **종류 (`+0xe`) 와 사용자 포인터 (`+0x10`) 는 안 본다.** `RegisterClassExWOWW +0x131` 이 배경 붓이 `0x1f` 보다 크면 `+0x2d8` 에서 `GdiValidateHandle` 을 부르고, 거절 + 앱이 서브시스템 버전 > 3.0 이면 `SetLastError(0x57)` + 0 (AT03 의 "매개 변수가 틀립니다"). **AT03 실험은 코드로 설명된다**: 칸 (스톡 배열) 만 채우고 표 칸은 0 이라 윗 16비트 비교 (`0x0002` ≠ 0) 에서 FALSE · 붓 값 0 은 `CMP RCX,0x1f` 에서 걸러져 검사 자체를 안 함.
3. **표를 syscall 앞에서 보는 함수**: `DeleteObject` (칸 없으면 **FALSE, syscall 없음**) · `SelectObject` (객체 칸 없으면 0 · DC 칸 없으면 `SetLastError(0x57)`+0, 붓·펜은 **syscall 없이** DC 사본만 고침) · `GetObjectType` (없으면 0) · `GetDeviceCaps` · `GetTextMetricsW` · `GetTextExtentPointW` · `GdiGetCharDimensions` (DC 칸 없으면 0 / 0x57). 안 보는 것: `GetStockObject` (칸 배열만 읽음) · `GetObjectW` (핸들 값의 종류 비트만 봄). 자세히는 표 (3).

## (1) `GdiplusStartup` 차례 표 (+ 실패 가지)

`GdiplusStartup` = gdiplus rva `0xe1170`, `InternalGdiplusStartup` = `0xe1380`. 반환 0 = `Ok`, 1 = `GenericError`, 2 = `InvalidParameter`, 0x11 = `UnsupportedGdiplusVersion`. `[정적]` = `ms/disassemble/gdiplus/*.asm` 줄. 에뮬레이터 로그의 syscall 이름은 요청서 원문 그대로 붙임.

| 차례 | 부르는 것 | 보는 결과 | 실패 조건 | 줄 `[정적]` |
|---|---|---|---|---|
| 0 | `GdiplusStartup` 입구 검사 | 포인터 셋 · 입력 구조체 `+0x0` (버전) · `+0x10` (백그라운드 스레드 끔) | `token` · `input` NULL, 또는 `+0x10 ≠ 0` 인데 `output` NULL → **2**. 버전 `-1` 이 `2` 보다 크면 (1~3 아님) → **0x11** | `GdiplusStartup +0x1e/+0x21 JZ → +0xeb` · `+0x27/+0x2a JZ → +0xeb` · `+0x30/+0x34 JZ → +0x3f` (`+0x10 == 0` 이면 통과) · `+0x36/+0x39 JZ → +0xeb` · `+0x3f~+0x46 JBE → +0x52` (버전 1~3 통과), 아니면 `+0x48 MOV EAX,0x11` |
| 1 | `EnterCriticalSection` → `Globals::LibraryInitRefCount == 0` 일 때만 안으로 | 두 번째 부름부터는 안 들어감 | — | `GdiplusStartup +0x59/+0x60` · `+0x65 MOV EDX,[0x1801b5c60]` · `+0x6d JNZ → +0xab` |
| 2 | `InternalGdiplusStartup` | 반환 ≠ 0 이면 그 값을 그대로 돌려줌 (`LeaveCriticalSection`) | — | `GdiplusStartup +0x72/+0x77` · `+0x79 TEST EAX` · `+0x7b JZ → +0x94` · `+0x7d~+0x92` |
| 2a | (안) `HeapCreate(0,0,0)` | 로그 첫 `NtAllocateVirtualMemory · NtFreeVirtualMemory · NtAllocateVirtualMemory` 가 이 안일 것 `[추측]` | NULL → **1** (`+0x7cc MOV ESI,1` 로 가서 정리) | `InternalGdiplusStartup +0x23/+0x2a` · `+0x2f MOV [GpMemHeap],RAX` · `+0x39 TEST` · `+0x3c JZ → +0x7cc` |
| 2b | `InitializeCriticalSection` ×3 | — | 분기 없음 | `+0x49 · +0x67 · +0x80` |
| 2c | (입력 버전 > 1 일 때) 기능 켜짐 검사 `__private_IsEnabled` ×9 | 결과를 `Globals::Feature_GdiPlusOptimizations_*` 에 저장만 | 분기 없음 | `+0xe1 · +0xed · +0xff · +0x111 · +0x121 · +0x13d · +0x14f · +0x161 · +0x173` |
| 3 | `GpRuntime::OSInfo::Initialize` (gdiplus `Initialize`, rva `0xd4980`) | `GetSystemInfo` (`+0x30`) · `GetVersionExA` (`+0x65`) · `IsProcessorFeaturePresent(0x28)` (`+0x7d`). 로그의 `NtQuerySystemInformation(0)` · `(1)` 과 `QueryLicenseValue` 하나가 여기 `[추측: kernelbase 쪽 줄은 이번에 안 읽음. ntdll `RtlGetVersion +0x1b3` 이 `NtQueryLicenseValue` 를 부르는 줄만 `_calls.tsv` 로 확인]` | **함수가 void** — 결과가 분기에 안 쓰임 | `InternalGdiplusStartup +0x18b/+0x190` · `Initialize.asm +0x30 · +0x65 · +0x7d` |
| 4 | `GetModuleHandleA(NULL)` (`DllInstance` 가 0 일 때) | syscall 없음 | — | `+0x19b/+0x1a2` |
| 5 | `InitVersionInfo` (gdiplus rva `0xd4a64`) | `GetVersionExA` (`+0x1e`) · `GetACP` (`+0x2a`). 로그의 `NtQueryInformationProcess(−1, 0x25)` · 둘째 `QueryLicenseValue` 는 `GetVersionExA` 어딘가 `[공백: 0x25 를 부르는 줄은 안 찾음]` | **void** — 결과 안 봄. 그래서 `0x25` 출력 칸 · 라이선스 값은 1 을 만들 수 없다 | `+0x1b4/+0x1b9` · `InitVersionInfo.asm +0x1e · +0x2a` |
| **6** | **`CreateRectRgn(0,0,1,1)`** (gdi32.dll) | `+0x1b9~+0x1c1` 이 `ECX=EDX=0, R8D=R9D=ESI=1` 을 만든다 (로그의 `(0,0,1,1)` 과 같음). 반환을 `Globals::CachedGdiRegion` 에 저장 | **NULL → `+0x1d6 TEST RAX,RAX` · `+0x1d9 JZ → +0x7d1` → `InternalGdiplusShutdown` → 1** ← **첫 가지** | `InternalGdiplusStartup +0x1c3/+0x1ca` · `+0x1cf` · `+0x1d6` · `+0x1d9` · `+0x7d1/+0x7d6 MOV EAX,ESI` |
| 6a | (gdi32 안) 아래 표 (1b) | | | |
| 7 | `GetModuleHandleA("user32.dll")` · `GetProcAddress` ×5 | 반환 안 봄 (함수 포인터를 `Globals` 에 둠) | 분기 없음 | `+0x1e6 · +0x1ff · +0x21c · +0x239 · +0x24f · +0x265` |
| 8 | `CreateICA("DISPLAY",0,0,0)` | HDC 를 `Globals::DesktopIc` 에 | **NULL → `+0x296 JZ → +0x7d1` → 1** (**다음 벽**, 이번 요청 밖. 아래 "다음 요청 후보") | `+0x280/+0x287` · `+0x28c` · `+0x293` · `+0x296` |
| 9 | `GetDeviceCaps(hdc, 0x58 / 0x5a)` | DPI X · Y | ≤ 0 이면 96.0 으로 — **분기 실패 아님** | `+0x2a4/+0x2ab · +0x2cb/+0x2d2 · +0x2e9~` |
| 10 | `GpDevice` · 드라이버 여섯 · 표면 둘 만들기 (`GpMallocEx`) · `CheckValid<…>` | | 모두 NULL → 1 (표 (1b)) | `+0x312~+0x632` |
| 11 | `InitImagingLibrary` · `SysColorNotify` · `GetModuleHandleW(gdi32.dll)` + `GetProcAddress` ×2 · `GetSystemDefaultLCID` · `InitSystemFontsDirs` · `GetGpInstalledFontCollection` · (입력 `+0x10 == 0` 이면) `BackgroundThreadStartup` | | `InitImagingLibrary` 0 · `InitSystemFontsDirs` 0 · `FontCollection` NULL · LRU 목록 NULL · `BackgroundThreadStartup` 0 → 1 | `+0x62b/+0x630` · `+0x6d5` · `+0x6e1~+0x731` · `+0x73d` · `+0x775` · `+0x78b` · `+0x7b3/+0x7b8` |
| 12 | `TraceLoggingRegisterEx_EventRegister_EventSetInformation` → `XOR EAX,EAX` → **0** | | — | `+0x7bc/+0x7c1 · +0x7c1 XOR EAX,EAX · +0x7c3 JMP → +0x7d8` |
| 실패 뒤 | `InternalGdiplusShutdown` (`0xe85a8`) → … `HeapDestroy` (`+0x608`) · `EventUnregister` (`+0x2c`) | `CachedGdiRegion` 이 NULL 이면 `DeleteObject` 를 **안** 부름 (`+0x25d TEST RCX · +0x260 JZ`). 로그의 `NtGdiDeleteObjectApp` 은 그래서 정리 쪽이 아니라 gdi32 `CreateRectRgn` 안 `DeleteRegion` 이다 (아래). `NtProtectVirtualMemory` ×2 · `NtFreeVirtualMemory` 의 출처 = `HeapDestroy` (Free) · Protect ×2 는 `[공백: 지연 가져오기 풀기일 가능성 — 아래 표 (1b) 꼬리 (`IsGdiTrackHCreatePresent`, `InitOnceExecuteOnce`) 가 후보]` | | `InternalGdiplusShutdown +0x25d · +0x262 · +0x608` |

### (1b) 첫 가지 안쪽 — gdi32 `CreateRectRgn` (gdi32.dll rva `0x2950`, 734B)

`gdi32full` 의 `CreateRectRgn` 은 6B 썽크 (`0x87520`). 몸은 gdi32 에 있다.

| 차례 | 하는 것 | 줄 `[정적]` `ms/disassemble/gdi32/CreateRectRgn.asm` |
|---|---|---|
| a | 좌표 정리. 범위 밖 (`< -0x8000000` · `> 0x7ffffff`) 이면 `TEB+0x68 = 0x57` 후 0 | `+0xa~+0x54` · `+0x130~+0x140` |
| b | `hGetPEBHandle(2, 0)` — PEB `+0x140` (`GdiHandleBuffer`) 안 영역 캐시. 새 프로세스는 칸 개수가 0 이라 **0 을 돌려줌** (그동안 PEB `+0x140` 에 TEB 포인터를 잠깐 썼다 지움) | `+0x6f/+0x74` · `+0x7a JZ → +0x160` · `hGetPEBHandle.asm` |
| c | **`NtGdiCreateRectRgn(0,0,1,1)`** (win32u) → 핸들 `H` | `+0x16d/+0x174` · `+0x17c JMP → +0x80` |
| d | `H` 에서 칸 번호: `((H>>8)&0xff0000) \| (H&0xffff)`. `0x10000` 미만이면 그대로 (`+0x9d JC → +0x2d8`), 아니면 칸 `[H&0xffff]+0xd` 바이트와 `H>>24` 가 같을 때만 `H&0xffff` | `+0x80 MOV R9,[pGdiSharedHandleTable]` · `+0x87~+0xb7` |
| e | 번호 ≥ `gMaxGdiHandleCount` (`0x1000000`) 이면 **실패** | `+0xbd CMP ECX,[0x180025798]` · `+0xc3 JNC → +0x116` |
| f | 칸 24바이트를 `표 + 번호*0x18` 에서 복사 (번호가 `0x10000` 이상이고 `+0xd` 가 안 맞으면 대신 `NtGdiGetEntry` syscall) | `+0xda CMP ECX,0x10000` · `+0xe0 JC → +0x181` · `+0x181~+0x196` (복사) · `+0xe6~+0x109` (`NtGdiGetEntry`) |
| g | **종류**: 칸 `+0xe` 바이트 == **4** | `+0x19c CMP byte [RSP+0x2e],4` · `+0x1a1 JNZ → +0x116` |
| h | **윗 16비트**: 칸 `+0xc` word == `H >> 16` | `+0x1a7~+0x1ae CMP word [RSP+0x2c],AX` · `+0x1b3 JNZ → +0x116` |
| i | **주인**: (칸 `+0x8` dword `& ~1`) == `gW32PID` (**0 은 안 받음**) | `+0x1b9~+0x1c0` · `+0x1c6 JNZ → +0x116` |
| j | **사용자 쪽 포인터**: 칸 `+0x10` qword ≠ 0, 풀어서 (`ROR 0x40−(gCookie&0x3f)`, `XOR gCookie`) 결과도 ≠ 0 | `+0x1cc~+0x1d4 JZ` · `+0x1da~+0x1f3` (`+0x1ed ROR R8,CL` · `+0x1f0 XOR R8,RDX` · `+0x1f3 JZ → +0x116`) |
| k | 풀린 포인터가 가리키는 **쓰기 가능한 0x18B** 에 `[+0]=0x30` · `[+4]=종류 (사각형 2 / 빈 영역 1)` · `[+8..+0x14]=사각형` 을 씀 | `+0x1f9~+0x256` (`+0x227 · +0x233 · +0x23a · +0x241 · +0x245 · +0x256`) |
| **실패** | 위 e·g·h·i·j 어디서든 **`+0x116`** 로 → `H ≠ 0` 이면 **`DeleteRegion(H)`** 부르고 `RBX = 0` → NULL 반환 | `+0x116 TEST RBX` · `+0x122/+0x127 DeleteRegion` · `+0x129 MOV EBX,EDI` · `+0x12b JMP → +0x25d` |
| 꼬리 | `IsGdiTrackHCreatePresent` (`ApiSetQueryApiSetPresence`) → 켜져 있으면 `GdiTrackHCreate(RBX)` (지연 가져오기 칸 `0x180028048`) · `InitOnceExecuteOnce(CheckIsEdgeGdiProcessOnce)` → `g_pEdgeGdi` 가 0 이면 `RBX` 반환 | `+0x25d/+0x262` · `+0x26e/+0x275` · `+0x291/+0x298` · `+0x29d~+0x2a7 JZ → +0x14d` |

그 안의 `DeleteRegion` (`0x1470`): 같은 칸 검사 (`+0x10b GdiGetEntry` · `+0x118` 종류 4 · `+0x12a` 윗 16비트 · `+0x13c` 주인) 를 **한 번 더** 하고, **`TEB+0x78 ≠ 0` 이고 배치 칸이 남았고 칸이 유효하면** `NtGdiDeleteObjectApp` 을 안 부르고 TEB 의 `GdiTebBatch` (`+0x2f0`) 에 쌓는다 (`GdiBatchLimit` 번 모이면 `NtGdiFlush`). 아니면 syscall (`+0x2e CMP [TEB+0x78],0` · `+0x33 JZ → +0xd7` · `+0xe9 JMP [0x180010738]` = win32u `NtGdiDeleteObjectApp`). 에뮬레이터는 `TEB+0x78 = 0` (sast/139) 이고 칸도 0 이라 syscall 이 나간다 — 로그와 맞음.

**성공하려면 그 자리에 무엇이 있어야 하나** (`H = NtGdiCreateRectRgn` 반환, `i = H & 0xffff`, 칸 = `PEB[+0xf8] + i*0x18`):

| 칸 (+) | 값 |
|---|---|
| `+0x08` dword | 이 프로세스 PID & ~3 (`= gW32PID`, 하위 1비트는 아무거나) |
| `+0x0c` word | `H >> 16` |
| `+0x0e` byte | **4** |
| `+0x10` qword | 쓰기 가능한 ≥ 0x18B 블록의 포인터를 **`ROR64(ptr ^ gCookie, gCookie & 0x3f)`** 로 가린 값 (`gCookie` = `NtGdiInit2` 반환값, `DllMain +0x3d/+0x44`). `gCookie` 하위 6비트가 0 이면 그냥 `ptr ^ gCookie` |
| 그 밖 | `i < 0x10000` · `H` 24~31비트는 칸 `+0xd` (= `+0xc` word 의 윗 바이트) 와 같게 (0 으로 두면 편함) |

이게 진짜 Windows 에서는 `win32kbase!NtGdiCreateRectRgn` (rva `0xfe1c0`) 이 `HmgAllocateObjectAttr` (`0xfe380`, 0x18B 칸을 "보안 사용자 메모리" 에서 나눠 줌) 와 `HmgInsertObjectInternal` (`0x2b4a0`) 로 채우는 칸이다 `[이름만; 본문은 안 봄]`.

## (2) 핸들 표 칸 표 + 핸들 값 → 칸 번호 식

**표 시작 · 크기** `[정적]`:
- 표 시작 = **PEB `+0xf8` 값 그대로**. `gdi32full!GdiProcessSetup +0x431~+0x449` 가 gdi32 의 `pGdiSharedMemory` (`0x25778`) 와 `pGdiSharedHandleTable` (`0x257a8`) 에 **같은 값** (PEB `+0xf8`) 을 쓰고, `pGdiDevCaps` ← 그 값 `+0x180000` (`+0x456`). 더하기 없음. `GdiProcessSetup +0x15a/+0x173` 도 PEB `+0xf8` 을 직접 읽음.
- 칸 크기 = **0x18**. 읽는 쪽이 모두 `LEA RAX,[idx+idx*2]` 후 `*8` (`GdiGetEntry +0x15/+0x19` · `CreateRectRgn +0xa6`). `+0x180000` 에서 DevCaps 가 시작하므로 칸 수는 `0x180000 / 0x18 = 0x10000` `[추측: 개수는 이 나눗셈으로만 — 상한 비교는 `gMaxGdiHandleCount` (`0x1000000`) 와만 함]`.
- `gMaxGdiHandleCount` = `0x1000000` (`gdi32full!GdiDllInitialize +0x16` 이 매번 씀). 이름은 gdi32 데이터 `0x25798`.

**핸들 값 → 칸 번호** (`gdi32!HANDLE_TO_INDEX` rva `0x2e70`, `gdi32full!HANDLE_TO_INDEX` rva `0x13c60`, 같은 식):

```
ext = ((H >> 8) & 0xff0000) | (H & 0xffff)         // H 의 24~31비트를 16~23비트 자리로
if ext < 0x10000                         → idx = H & 0xffff
elif 표[H & 0xffff].+0xd == (ext >> 16)  → idx = H & 0xffff     // 칸 윗 바이트 == H>>24
else                                     → idx = ext            // ≥ 0x10000: 표에 없음 → NtGdiGetEntry syscall
```

`GdiGetEntry` (gdi32 `0x1010`): `idx < 0x10000` 이거나 `표[idx&0xffff].+0xd == idx>>16` 이면 표에서 24바이트를 복사해 0 반환, 아니면 win32u `NtGdiGetEntry` (`JMP [0x180010728]`, syscall). 그래서 **에뮬레이터는 `idx < 0x10000` 이고 칸 `+0xc` word 의 윗 바이트 = `H>>24` 로 두면 표 읽기에 syscall 이 안 낀다.**

**핸들 비트 모양** `[정적]`: 0~15 칸 번호 · 16~22 종류 · 23 스톡 · 24~31 고유 번호.
- 종류 = `H & 0x7f0000` 을 `GetObjectType` / `InternalDeleteObject` / `SelectObjectImpl` / `GetObjectW` 가 직접 가름: `0x10000` DC · `0x40000` 영역 · `0x50000` 비트맵 · `0x80000` 팔레트 · `0x90000` 색공간 · `0xa0000` 글꼴 · `0x100000` 붓 · `0x300000` 펜 · `0x500000` 확장 펜 · `0x210000` `0x260000` `0x460000` `0x660000` 메타파일 쪽 (`GetObjectType +0xbe~` 줄, `[GetObjectType 반환값 매핑은 SDK 상수 — 코드 줄은 `GetObjectType.c`]`).
- 스톡 = `0x800000`. `win32kbase!bSetStockObject` (rva `0x16bb10`) 가 `핸들 | 0x800000` 을 세션 배열 (`세션+0x58 → +0xbc0` · `+0xbc8`) 에 쓰고 `HmgModifyHandleType` (`0x3ea50`) 로 칸 `+0xc` word 에 `H>>16` 을, 객체 첫 qword 에 `H` 를 쓴다 `[정적: 줄 확인]`.
- 핸들 값 만드는 곳: `win32kbase!HmgpComputeHandleValue` (`0x6b4a4`) = `(int)(칸+0xc word << 16) | 번호` (부호 확장). 즉 **핸들 윗 16비트 = 칸 `+0xc` word**.

**칸 24바이트의 모양** (아래 `+` = 칸 안 오프셋. 이름은 `[추측]`, 오프셋 · 검사 · 줄은 `[정적]`. PDB 에 이 구조체 타입은 없음):

| 칸 오프셋 · 크기 | 뜻 | 검사 (누가) | 줄 |
|---|---|---|---|
| `+0x00` qword | 커널 쪽 객체 주소 `[추측]` | **유저 쪽 어느 함수도 안 읽음** (복사만). 0 이어도 됨 `[정적: 읽는 줄 못 찾음]` | `GdiGetEntry +0x3b~+0x43` (복사) |
| `+0x08` dword | **주인 프로세스** (하위 1비트는 무시) | `(값 & ~1) == gW32PID` (`gW32PID` = TEB `+0x40` & `~3`, `GdiProcessSetup +0x142~+0x14f`). 함수마다 **0 도 받나** 가 다름 → 표 (2b) | `GdiValidateHandle +0x55~+0x6d` |
| `+0x0c` word | **윗 16비트** (= `H >> 16`: 종류 · 스톡 · 고유 번호). 윗 바이트 (`+0xd`) 는 `HANDLE_TO_INDEX` 가 따로 읽음 | `== H >> 16` (정확히. `InternalDeleteObject` · `SelectObjectImpl` 의 객체 쪽은 스톡 비트 `0x80` 차이만 봐줌 — `TEST …,0xffffff7f`) | `GdiValidateHandle +0x4a/+0x4e/+0x53` · `InternalDeleteObject +0x6c~+0x73` · `SelectObjectImpl +0xd0~+0xdb` |
| `+0x0e` byte | **종류** (객체 종류 번호: DC `1` · 영역 `4` · 비트맵 `5` · 팔레트 `8` · 글꼴 `0xa` · 붓 `0x10` · 펜 `0x10` …) | **== `(H >> 16) & 0x1f`** (`InternalDeleteObject +0x57~+0x5e` · `SelectObjectImpl +0xc1`) 또는 고정값 (영역 `4` · DC `1` · 붓 `0x10`). `GdiValidateHandle` 은 **안 봄** | 표 (2b) |
| `+0x0f` byte | 비트 1 이 켜지면 `SelectObject(hdc, 글꼴)` 이 0 으로 거절 `[뜻 공백]` | 비트 1 == 0 이어야 통과 | `SelectObjectImpl +0x398 TEST byte [RSP+0x47],2 · +0x39d JNZ` |
| `+0x10` qword | **사용자 쪽 속성 블록 포인터** (가려진 값). 영역 → 0x18B, DC → `DC_ATTR` (≥ 0x130B: `+0x98` 바뀐 표시 · `+0xa0` 붓 · `+0xa8` 펜 · `+0x128` 글꼴 · `+0xf0` ICM 표시) | 풀어서 ≠ 0. 식은 위 (`ROR`/`XOR gCookie`) | `CreateRectRgn +0x1da~+0x1f3` · `SelectObjectImpl +0x1fb~+0x217` |

### (2b) 누가 칸의 무엇을 보나 (표 · 함수 · 줄)

`✓` = 검사함, `—` = 안 봄, `0 ok` = 주인 칸이 0 이어도 통과.

| 함수 (모듈 rva) | 칸 번호 | `+0xe` 종류 | `+0xc` 윗 16비트 | `+0x8` 주인 | `+0x10` 포인터 | 통과 못 하면 |
|---|---|---|---|---|---|---|
| `GdiValidateHandle` (gdi32full `0x70180`) | ✓ `+0x14` · `+0x22` | — | ✓ 정확 `+0x4a~+0x53` | ✓ == PID **또는 0** `+0x55~+0x6d` | — | FALSE (`+0x82 XOR EAX,EAX`). 핸들 0 → TRUE (`+0x12 JZ → +0x71`) |
| `CreateRectRgn` (gdi32 `0x2950`) | ✓ | ✓ == 4 | ✓ 정확 | ✓ == PID **(0 불가)** | ✓ ≠ 0 | `DeleteRegion` + NULL |
| `DeleteRegion` (gdi32 `0x1470`) | ✓ | ✓ == 4 | ✓ 정확 | ✓ == PID (0 불가) | ✓ ≠ 0 | 배치 못 하고 syscall `NtGdiDeleteObjectApp` |
| `InternalDeleteObject` (gdi32 `0x10f0`, `DeleteObject +0x46` 이 부름) | ✓ `+0x23~+0x2e` | ✓ == `(H>>16)&0x1f` `+0x5c/+0x5e` | ✓ 스톡 비트 빼고 `+0x6c~+0x73` | ✓ == PID **또는 0** `+0x80/+0x86 → +0x284` | 붓 · 펜 (배치) 때만 | **FALSE, syscall 없음** (`+0x14b XOR EAX,EAX`). 통과하면: DC → `InternalDeleteDC` · 영역 → `DeleteRegion` · `H` 의 스톡 비트 (`+0xcc BT RDI,0x17`) 가 켜졌으면 **TRUE, syscall 없음** (`+0x27a MOV EAX,1`) · 그 밖 → `NtGdiDeleteObjectApp` (`+0x129`) |
| `GetObjectType` (gdi32full `0x311e0`) | ✓ `+0x26~+0x6e` | — (핸들 비트로 가름) | ✓ 정확 `+0x9f/+0xa4` | ✓ == PID **또는 0** `+0xb4/+0xb6` | — | 0, **syscall 없음** (DC 만 `NtGdiGetDCDword(h,7)` syscall) |
| `SelectObjectImpl` (gdi32full `0x26210`, gdi32 `SelectObject` 가 부름) — **객체** `h` | ✓ `+0x4f~+0x87` | ✓ == `(h>>16)&0x1f` `+0xc1/+0xc5` | ✓ 스톡 비트 빼고 `+0xd0~+0xdb` | ✓ == PID **또는 0** `+0xf1 → +0x4bf` | — | 0, **`GetLastError` 안 세움** (`+0x30f`) |
| 〃 — **DC** `hdc` | ✓ `+0x136~+0x189` | ✓ == 1 `+0x1ba` | ✓ 정확 `+0x1cc` | ✓ == PID (0 불가) `+0x1e5/+0x1e7` | ✓ ≠ 0, 풀어도 ≠ 0 `+0x1f2~+0x217` | **`GdiSetLastError(0x57)` + 0** (`+0x52a/+0x52f`) |
| `GetDeviceCaps` (gdi32 `0x17f0`) — DC | ✓ `+0xab` | ✓ == 1 `+0x163` | ✓ 정확 `+0x175` | ✓ == PID `+0x187` | ✓ | `TEB+0x68 = 0x57` (`+0xbc`) + 0. 통과 후 지표 `0x58`·`0x5a` 등은 `NtGdiGetDeviceCaps` syscall (`+0x1cb`) |
| `GetTextMetricsW` (gdi32full `0x1b460`) — DC | ✓ `+0xa6` | ✓ == 1 | ✓ 정확 | ✓ == PID `+0xcb` | ✓ | 0 (에러값 안 세움 `[공백: 도착 줄]`). 통과 후 글꼴 사본 (`pcfLocateCFONT`) 에 없으면 `NtGdiGetTextMetricsW` (`+0x22a`) |
| `GetTextExtentPointW` (gdi32full `0x12ce0`) — DC | ✓ `+0x79` · `+0xb8` | ✓ == 1 | ✓ 정확 | ✓ == PID `+0xdd` | ✓ | **`GdiSetLastError(0x57)` + 0** (`+0x89/+0x8e`). 통과 후 글자 폭 계산은 글꼴 사본 (`bComputeTextExtent`) 에서, 안 되면 `NtGdiGetTextExtent` (`+0x262`). 글자가 전부 ASCII 이고 `GetDCDpiScaleValue(hdc) == 1` 일 때 이 길 |
| `GdiGetCharDimensions` (gdi32full `0x4db20`) — DC | ✓ `+0x48` · `+0x6c` | ✓ == 1 | ✓ 정확 | ✓ == PID `+0x9b` | ✓ | **아무것도 안 쓰고 돌아감** → 부른 쪽이 폭 0 으로 읽음. 통과 후 `bGetTextMetricsWInternal` (`+0x12e`) · `GetTextExtentPointWInternal` (`+0x1c0`, 글자 52개 `abc…ABC…`) |
| `GetStockObject` (gdi32full `0x25e90`) | — | — | — | — | — | 표 안 봄. 칸 배열 읽기만 (아래 (2c)) |
| `GetObjectW` (gdi32full `0x107f0`) | — | — | — | — | — | 표 안 봄. 핸들의 종류 비트만 봄 (표 (3)) |

(위 `+0x…` 는 각 함수 `.asm` 줄. 근거 파일: `ms/disassemble/gdi32/{CreateRectRgn,DeleteRegion,InternalDeleteObject,GetDeviceCaps,GdiGetEntry,HANDLE_TO_INDEX}.asm` · `ms/disassemble/gdi32full/{GdiValidateHandle,GetObjectType,SelectObjectImpl,GetTextMetricsW,GetTextExtentPointW,GdiGetCharDimensions,HANDLE_TO_INDEX}.asm`.)

### (2c) 스톡 객체 칸 — `GetStockObject` 가 읽는 배열

`GetStockObject(i)` (gdi32 `0x4980` 은 껍데기, 몸은 gdi32full `0x25e90`):

- `i > 0x15` → 0 (`+0x11 CMP ECX,0x15 · +0x14 JA → +0x42`).
- 붓 · 펜 · 팔레트 등 (`i` 가 `10` · `13` · `16` · `17` 이 아닐 때): **`[PEB+0xf8 값 + 0x1800b0 + 8*i]` 를 그대로 돌려줌** (`+0x65~+0x7a`). 표 칸은 안 본다.
- `i = 10 · 13 · 16` (`+0x18 SUB EDX,0xa` · `+0x56 SUB EDX,3` · `+0x5b SUB EDX,3`): 스레드의 DPI 인식이 "안 함" (`TEST AL,0xf`, `+0x33/+0x35`) 이면 **`[… + 0x180160 + 8*i]` 를 먼저** 읽고 (`+0x7c~+0x87`), 0 이면 위와 같이 `+0x1800b0 + 8*i` (`+0x8a JZ → +0x37`). 인식이 있으면 바로 `+0x1800b0 + 8*i`.
- `i = 17` (`+0x60 CMP EDX,1 · +0x63 JZ → +0x91`): 시스템 DPI 와 스레드 DPI 를 보고 (`GetDpiDepStockObjectPlateauSlot`, gdi32full `0x7d6a0`) `+0x180138` (같은 DPI, 0 아님) · `+0x1801e8` (`0x60`) · `+0x180210 + 8*슬롯` (`0x78~0x1e0` 의 `0x18` 배수, 슬롯 0~15) 중 하나. 칸이 `0` 이면 `NtGdiEnsureDpiDepDefaultGuiFontForPlateau` syscall (`+0x124`). 그 밖 DPI 는 `GdiSetLastError(0x57)` 후 `+0x180138` (`+0xe2~+0xf3`).
- 그래서 **읽는 범위** = 붓 · 펜 · 팔레트만 쓰면 `+0x180160` 까지 (i ≤ 0x15 → `+0x180158`) · 글꼴 (10 · 13 · 16) 까지 쓰면 `+0x1801e8` · GUI 글꼴 (17) 까지 쓰면 `+0x180290` (`+0x180210 + 8*15 + 8`).
- `i = 0x12` (`+0x180140`) · `i = 0x13` (`+0x180148`) 은 `DllMain` 이 0 아닌지 본다 (run86). 값이 DC 붓 · DC 펜 핸들.

스톡 핸들 꼴 `[정적: 비트 규칙 + 식으로 만든 예. 진짜 값 아님]`: `H = 번호 | (종류 | 0x80) << 16 | 고유 << 24`. 붓 `0x100000 | 0x800000 = 0x900000` · 펜 `0xb00000` · 글꼴 `0x8a0000` · 팔레트 `0x880000`. 예: 번호 `0x10`, 고유 1, 붓 → `0x01900010`. 칸에 쓰는 값은 `H` 의 24~31비트에 맞춰 `+0xc` word = `H >> 16`.

그 칸을 채우는 쪽 (이름만): `win32kbase!GdiProcessCallout` (`0xe2010`, `+0x22b` 이 PEB `+0xf8` 에 씀, run86) · `bSetStockObject` (`0x16bb10`, 호출자 `bInitBrush` · `bInitBRUSHOBJ` · `bInitPALOBJ` · `bInitICM` · `bInitBMOBJ`) · `GreGetStockObject` (`0x65870`) · 글꼴은 win32kfull `bInitStockFontsInternal` (`0x40962c`) · `bSetStockFont` (`0x128378`). 세션 배열 (`+0xbc0`) 이 공유 메모리 `+0x1800b0` 와 같은 메모리인지는 이번에 안 이었다 `[공백]`.

## (2d) `RegisterClassExWOWW` 의 배경 붓 검사 (user32 `0x1c99c`)

`ms/disassemble/user32/RegisterClassExWOWW.asm`:

| 차례 | 줄 `[정적]` | 뜻 |
|---|---|---|
| 1 | `+0x114/+0x119 CALL 0x18001cd60 (user32 RtlGetExpWinVer)` → `+0x119 MOV EDI,EAX` | 이 앱이 기대하는 윈도 버전. `RtlGetExpWinVer` (user32 `0x1cd60`) = hInstance 의 PE 헤더 `+0x48` (MajorSubsystemVersion) `<< 8 \| +0x4a` (Minor). hInstance 가 NULL · 헤더 없음 · Major 1 이면 `0x30a` (`+0x10 · +0x15 · +0x1c · +0x34 · +0x3f · +0x41`) |
| 2 | `+0x129 MOV RCX,[RBX+0x30]` · `+0x12d CMP RCX,0x1f` · **`+0x131 JA → +0x2d8`** | `WNDCLASSEXW.hbrBackground` (+0x30). **`0` ~ `0x1f` (COLOR 번호 + 1) 는 이 검사를 건너뜀** |
| 3 | **`+0x2d8/+0x2df CALL [0x1800b51b8]` = gdi32 `GdiValidateHandle`** (인자 RCX 그대로) | gdi32 의 `GdiValidateHandle` (`0xc5c0`) 은 `JMP [0x180027bf0]` 썽크 → **지연 가져오기** → gdi32full `0x70180`. 처음 한 번은 `NtProtectVirtualMemory` ×2 `[run86 A6 과 같은 방식]` |
| 4 | `+0x2e4 TEST EAX,EAX` · `+0x2e6 JNZ → +0x137` | 통과 → 등록 계속 |
| 5 | 거절: `+0x2ec CMP EDI,0x300` · **`+0x2f2 JA → +0x326`** | 앱 버전이 `0x300` (3.0) 보다 크면 → `+0x326/+0x32b RtlSetLastWin32Error(0x57)` · `+0x337 MOVZX EAX,R12W (=0)` · `+0x33b JMP → +0x26d` = **0 반환, 오류 `0x57` (`ERROR_INVALID_PARAMETER`)**. 지금 나오는 앱은 거의 다 서브시스템 4.0 이상이라 이쪽 |
| 6 | 버전 `≤ 0x300`: `+0x2f4 MOV [RBX+0x30],R12 (=0)` · `+0x2f8 JMP → +0x137` | 붓을 **호출한 앱의 구조체 안에서 0 으로 고치고** 계속 |

- `GdiValidateHandle` 이 호출되는 user32 안 자리 전부 (`_refs/0xb51b8.tsv`, 9곳): `DT_InitDrawTextInfo +0x275` · `RegisterClassExWOWW +0x2d8` · `RegisterClassExWOWA +0x2d0` · `ValidateMENUITEMINFO +0x150 · +0x17b · +0x1d1` · `SetClassLongPtrW +0x96` · `SetClassLongPtrA +0xc8` · `ValidateMENUINFO +0x5a`. 같은 규칙이 모두에 적용된다.
- `RegisterClassW +0x6a` · `RegisterDefaultClass +0x1b8` · `RegisterClassExW +0x1a/+0x1f` 가 이 함수로 들어온다 (`_calls.tsv`). user32 가 자기 클래스를 등록할 때 붓은 COLOR 번호라 검사를 건너뜀.
- **AT03 설명 (코드로)**: 스톡 붓 칸에 `0x21400 + i*0x400` 를 넣음 → `> 0x1f` 라 `+0x2d8` 로 감 → `HANDLE_TO_INDEX` → 번호 `0x1400 + i*0x400` → 표 칸 (`+0x1e000 + …`) 이 전부 0 → `+0xc` word `0` ≠ `H>>16 = 0x0002` → FALSE → 앱 버전 > 3.0 → `0x57`. 칸을 0 으로 두면 `CMP RCX,0x1f` 에서 빠짐 → 성공. **칸 (표) 을 채우지 않고 배열만 채운 게 원인.**

## (3) syscall 없이 실패하는 함수 목록 (표가 비었을 때)

`H` = 표 칸이 0 인 핸들. "syscall" 은 win32k (`win32u`) 쪽.

| 함수 | 표가 비면 | syscall | 줄 |
|---|---|---|---|
| `GdiValidateHandle` (user32 가 부름) | FALSE | 없음 | `GdiValidateHandle +0x4a~+0x53 → +0x82` |
| `DeleteObject` → `InternalDeleteObject` | **FALSE** (오류 안 세움) | **없음** | `InternalDeleteObject +0x5e JNZ → +0x14b` · `+0x73 JNZ → +0x14b` (칸 0 이면 종류 · 윗 16비트가 안 맞음) |
| `DeleteObject(스톡 핸들)` (칸 정상) | **TRUE** | **없음** | `+0xcc BT RDI,0x17 · +0xd1 JC → +0x27a MOV EAX,1` |
| `SelectObject(hdc, 객체)` — 객체 칸 없음 | 0 (오류 안 세움) | 없음 | `SelectObjectImpl +0xc5/+0xdb → +0x30f` |
| `SelectObject(hdc, 객체)` — DC 칸 없음 | 0, **`GetLastError = 0x57`** | 없음 | `SelectObjectImpl +0x1b4/+0x1bf/+0x1d1/+0x1e7/+0x1f5/+0x217 → +0x52a` |
| `SelectObject(hdc, 붓 또는 펜)` — 칸 정상 | 이전 붓 · 펜 (DC 사본 `+0xa0` / `+0xa8`) | **없음** (사본에 쓰고 `+0x98` 에 바뀜 표시 `0x1000` / `0x2000`) | `SelectObjectImpl +0x59f~+0x5c8` · `+0x569~+0x584` |
| `SelectObject(hdc, 글꼴)` — 칸 정상 | 이전 글꼴 | 대개 **없음** (TEB `GdiTebBatch` 에 쌓음; `TEB+0x2f8` 에 다른 DC 가 있거나 배치가 차거나 `DC_ATTR` 비트 0 이면 `NtGdiSelectFont`, `+0x5ed`) | `+0x384~+0x4a8` · `+0x5ed` |
| `SelectObject(hdc, 비트맵)` | — | `NtGdiSelectBitmap` (`+0x240`) | |
| `GetObjectType` | 0 | 없음 (DC 만 `NtGdiGetDCDword`) | `GetObjectType +0x6e JNC → +0x109` · `+0xa4 JNZ` · `+0xb6 → +0x157` |
| `GetObjectW` | **표 안 봄.** 글꼴 + `pv==NULL` → `0x5c` · 붓 · 펜 + `pv==NULL` → `0x10` (**syscall 없음**) · 영역 · DC · 확장 펜 · 메타파일 (`0x40000` · `0x10000` · `0x210000` · `0x260000` · `0x460000`) → `GdiSetLastError` + 0 (**syscall 없음**) · 그 밖은 `NtGdiExtGetObjectW` (`+0x64`) | 위 외엔 있음 | `GetObjectW +0x64 · +0x96 · +0xa9 (GdiFixUpHandle) · +0xc0` |
| `GetStockObject` | 표 안 봄 | 없음 (`i=17` 칸 0 일 때만 `NtGdiEnsureDpiDepDefaultGuiFontForPlateau`) | 표 (2c) |
| `GetDeviceCaps(DC)` | 0, `TEB+0x68 = 0x57` | 없음 (통과해도 `0x58`·`0x5a` 등은 syscall) | `GetDeviceCaps +0xbc` |
| `GetTextMetricsW(DC)` | 0 | 없음 | `GetTextMetricsW +0xa6 · +0xcb` |
| `GetTextExtentPointW(DC)` | 0, `GetLastError = 0x57` | 없음 | `GetTextExtentPointW +0x89/+0x8e` |
| `GdiGetCharDimensions` | 아무것도 안 하고 돌아감 (부른 쪽은 폭 0 으로 읽음) | 없음 | `GdiGetCharDimensions +0x6c · +0x9b` |
| `CreateRectRgn` | NULL (그 전에 `NtGdiCreateRectRgn` + `DeleteRegion` 이 나감) | **있음** (둘) | 표 (1b) |
| `DeleteRegion` / `DeleteObject(영역)` — 칸 정상 + `TEB+0x78 ≠ 0` | TRUE | **없음** (TEB 배치) → `GdiBatchLimit` (`GetAppCompatFlags2` 가 `0x200` 안 켜면 `0x14`) 개 모이면 `NtGdiFlush` | `DeleteRegion +0x2e~+0x4e` · `GdiProcessSetup +0x464~+0x47e` |

(위 줄은 각 함수 `.asm` 의 `+0x…` — 표 (2b) 의 파일.)

## sast/139 에서 넘어온 GDI 답 (DC · 글꼴 · 글자 폭) 중 닫은 것

(`MessageBoxW` 길 — sast/139 표 (2) 의 차례 6 · 8 · 9. 값은 안 봤고, "표를 보는 자리" 만 닫음.)

- `NtUserGetDCEx(0,0,3)` 가 준 HDC 는 **종류 비트 `0x10000` (DC)** 여야 하고, 표에 **DC 칸** (`+0xe == 1` · `+0xc == H>>16` · `+0x8 & ~1 == PID` **정확히** · `+0x10` 풀면 ≠ 0 인 `DC_ATTR` 포인터) 이 있어야 `SelectObject` · `GetTextExtentPointW` · `GetTextMetricsW` · `GdiGetCharDimensions` · `GetDeviceCaps` 가 읽는다. 없으면 `SelectObject` · `GetTextExtentPointW` 는 `GetLastError = 0x57` 과 0, `GdiGetCharDimensions` 는 말없이 돌아가서 `SoftModalMessageBox +0x151 TEST EDX · +0x153 JZ → +0x69f` 가 폭 0 으로 `MessageBoxW` 를 0 으로 끝낸다.
- HFONT (`NtGdiHfontCreate` 반환) 는 종류 `0xa0000` 이고 표에 **글꼴 칸** (`+0xe == 0xa` · `+0xc == H>>16` · 주인 PID 또는 0 · `+0xf` 비트 1 꺼짐) 이 있어야 `SelectObject(hdc, 글꼴)` 가 이전 글꼴을 돌려주고 DC 사본 `+0x128` 을 바꾼다. 칸이 없으면 0 (오류 안 세움).
- `DeleteObject(hfont)` 는 칸이 있으면 `DeleteFont` (gdi32full) 쪽으로 (`InternalDeleteObject +0xe4 CMP EBX,0xa0000 · +0xea JZ → +0x14f`, `IsDeleteFontPresent`). 칸이 없으면 FALSE, syscall 없음.
- **남은 것** (이번에 안 읽음): `pcfLocateCFONT` (글꼴 사본 — `semLocal` 임계구역, `CFONT` 칸 모양) · `bComputeTextExtent` · `bGetTextMetricsWInternal` 이 syscall 없이 폭을 돌려주는 조건 · `DC_ATTR` 전체 모양 · `NtUserGetDCEx` 가 DC 칸을 만드는 쪽.

## 지금 우리 (PE_emulator) 맞나·틀리나

- **`nt_gdi_init2.cpp` (공유 메모리 블록, 핸들 표 0)**: **모자람.** 블록 크기 `0x180150` 은 `DllMain` 까지는 맞지만 (run86) 표 (`+0x0 ~ +0x180000`, 칸 0x18B) 가 전부 0 이면 ① `CreateRectRgn` 이 NULL (이게 `GdiplusStartup` = 1 의 첫 가지) ② `GdiValidateHandle` 이 FALSE (`RegisterClassEx` = `0x57`) ③ `SelectObject` · `GetTextExtentPointW` 등 DC 길이 `0x57` / 0. 스톡 글꼴 (10 · 13 · 16) 까지 읽으려면 크기도 `0x1801e8` 이상, GUI 글꼴 (17) 까지는 `0x180290` 이상. `NtGdiInit2` 반환값은 0 아님이 필요 (이미 아는 것) + **하위 6비트를 0 으로 두면** 사용자 포인터 가리기가 `ptr ^ gCookie` 하나로 끝난다.
- **`nt_gdi_create_rect_rgn.cpp` (핸들 `0x20800`)**: **틀림 (둘).** ① 핸들을 만들 때 **표 칸을 안 채움** — `+0x8` PID · `+0xc` `H>>16` · `+0xe` 4 · `+0x10` 가린 포인터 (쓰기 가능한 0x18B 이상). ② 핸들 꼴이 영역이 아님: `0x20800` 의 종류 비트 (16~22) 는 `2` 인데 영역은 `4` 라서, 칸 `+0xe` 를 `4` 로 (CreateRectRgn 요구) 두면 `InternalDeleteObject +0x5c/+0x5e` (칸 `+0xe` == `(H>>16)&0x1f`) 에서 `DeleteObject` 가 FALSE 가 된다. 꼴은 **`번호 | 0x040000 | (고유<<24)`** 으로.
- **`NtGdiDeleteObjectApp` 새 stub**: ① 유효한 객체에는 **0 이 아니라 0 아닌 값 (1)** 을 돌려줘야 `DeleteObject` 가 TRUE (`InternalDeleteObject +0x129` 반환을 `DeleteObject` 가 `(int)==0` 로 FALSE 판정). `GdiplusStartup` 의 영역 정리 (`CreateRectRgn` 실패 안) 는 반환을 안 쓴다. ② 칸을 지우는 것은 선택 (안 지우면 같은 핸들로 `DeleteObject` 를 두 번 불러도 둘 다 syscall). ③ **`TEB+0x78 ≠ 0` 으로 바뀌면** 영역 · 붓 · 펜 삭제는 syscall 없이 TEB 배치로 쌓이다가 `NtGdiFlush` 로 나간다 — 그때는 이 stub 이 안 불린다. 지금 `TEB+0x78 = 0` 이라 매번 부른다 (맞음).
- **보류 패치 `AT03_stock-objects_held.patch`**: **원인 확인됨, 고칠 자리 있음.** 배열 (`+0x1800b0 + 8*i`) 만 채우고 표 칸은 안 채워서 `GdiValidateHandle` 이 FALSE. 아래 최소 목록을 같이 넣으면 `RegisterClassEx` 도 통과한다. 단, 지금 `0x21400 + i*0x400` 의 윗 16비트가 `0x0002` (붓 아님) 인데 `GdiValidateHandle` 은 종류 비트를 안 봐서 **통과에는 지장 없음** — 다만 `GetObjectType` · `DeleteObject` (스톡 비트 `0x800000` 이 없어 `+0xcc` 에서 TRUE 로 안 감) 는 다르게 움직이니 **`0x900000 | 번호 | 고유<<24`** 꼴이 안전.

### 스톡 객체를 채우면서 `RegisterClassEx` 도 통과시키는 최소 표 칸 (한 목록)

(붓 `i = 0~5` 와 `0x12` (DC 붓) 기준. 펜 · 글꼴 · 팔레트도 같은 방식, 종류만 다름.)

1. PEB `+0xf8` → 읽고 쓸 수 있는 블록. 크기 ≥ `0x180160` (붓 · 펜 · 팔레트만) · `0x1801e8` (+ 글꼴 10·13·16) · `0x180290` (+ GUI 글꼴 17). 표는 `+0x0 ~ +0x180000`.
2. `+0x1800b0 + 8*i` ← 스톡 핸들 `H_i`. `i = 0x12` · `0x13` 은 반드시 0 아님 (`DllMain`).
3. `H_i` 모양: 번호 `idx_i < 0x10000` (서로 다르게), 종류 `0x100000` (붓), 스톡 `0x800000`, 고유 번호 아무거나 (0 이 가장 단순). 예: `idx 0x10, 고유 1 → 0x01900010`.
4. 칸 `PEB[+0xf8] + idx_i*0x18`:
   - `+0x08` dword = **0** (주인 0 은 `GdiValidateHandle` · `GetObjectType` · `DeleteObject` · `SelectObject(객체)` 가 받음. PID 를 넣어도 됨 — `gW32PID` 와 하위 1비트만 달라도 됨)
   - `+0x0c` word = `H_i >> 16` (스톡 비트 포함해서 그대로)
   - `+0x0e` byte = `(H_i >> 16) & 0x1f` (붓 `0x10`)
   - `+0x00` · `+0x0f` · `+0x10` = 0 (스톡 객체는 이 셋을 안 읽음 `[정적]`)
5. 이미 되어 있어야 하는 것: `gMaxGdiHandleCount` (`GdiDllInitialize` 가 `0x1000000` 으로 씀) · `gW32PID` (`GdiProcessSetup` 이 씀) · gdi32 `GdiValidateHandle` 썽크 지연 가져오기 풀기 (API 세트 → gdi32full, 첫 부름에 `NtProtectVirtualMemory` ×2).
6. 이것만 있으면 `GdiValidateHandle(H_i)` = TRUE → `RegisterClassExWOWW +0x2e6 JNZ` 로 통과. `RegisterClassEx` 의 나머지 (`NtUserRegisterClassExWOW`) 는 sast/131 · run131.

### `GdiplusStartup` 첫 가지를 넘기는 최소 (위 목록 + 영역 하나)

7. `NtGdiCreateRectRgn` 이 돌려줄 핸들 `H_r = idx_r | 0x040000 | 고유<<24` (`idx_r < 0x10000`), 칸 `+0x08` = PID & ~3, `+0x0c` = `H_r >> 16`, `+0x0e` = 4, `+0x10` = `ROR64(ptr ^ gCookie, gCookie & 0x3f)` (`ptr` = 쓰기 가능한 0x18B 이상).
8. PEB `+0x140` (`GdiHandleBuffer`) 은 0 그대로 (캐시 비었다는 뜻, `hGetPEBHandle` 이 0 을 돌려줘서 `NtGdiCreateRectRgn` 으로 감). 읽고 쓰기 가능이어야 함 (TEB 포인터를 잠깐 씀).
9. 이 뒤 **다음 벽** = `CreateICA("DISPLAY")` (`InternalGdiplusStartup +0x296`). 아래 "다음 요청 후보".

## 동적으로 확인할 것 (Q162 세션과 합치게)

1. 진짜 프로세스에서 PEB `+0xf8` 이 가리키는 표의 몇 칸을 덤프: **스톡 붓 하나의 칸** (`+0x8` 이 정말 0 인가, `+0xc` 가 `H>>16` 인가, `+0xe` 가 `0x10` 인가, `+0x10` 이 0 인가), **영역 하나의 칸** (`+0x10` 가 가려진 값인가), **DC 하나의 칸**. `gCookie` 값 (`gdi32` rva `0x257a0`) 과 풀린 포인터.
2. `win32u!NtGdiCreateRectRgn` 반환 핸들의 비트 (종류 `4` · 스톡 0 · 고유 번호), 그 직후 `gdi32!CreateRectRgn +0x19c` 에서 `[RSP+0x2e]` · `+0x1ae` 에서 `[RSP+0x2c]` 확인.
3. `GetStockObject(0..5, 0x12, 0x13)` 이 돌려주는 값 — 위 식 (`종류 | 0x800000`) 과 맞는가. 같은 값의 표 칸 (`+0x8` 주인).
4. `user32!RegisterClassExWOWW +0x2d8` 들어가기 직전 `RCX` (배경 붓) 와 `+0x2e4` 의 `EAX`. 일반 앱에서 `RtlGetExpWinVer` 가 돌려주는 값 (`+0x119` 의 `EDI`).
5. test.exe 같은 프로세스에서 `TEB+0x78` 이 `DeleteObject(영역)` 때 0 인가 아닌가 — 아니면 `NtGdiDeleteObjectApp` 이 안 나가고 TEB `+0x2f0` 에 쌓임 (표 (3) 마지막 줄).
6. `NtQueryInformationProcess(−1, 0x25)` 와 `NtProtectVirtualMemory` ×2 를 부르는 줄 (`GetVersionExA` 쪽 kernelbase · gdi32 `CreateRectRgn +0x25d/+0x26e` 꼬리 · `InternalGdiplusShutdown` 중 어느 쪽) — 정적으로는 못 정함.
7. 추천 멈춤점: `gdi32!CreateRectRgn +0x19c` (칸 종류 비교) · `+0x116` (실패 도착) · `gdi32full!GdiValidateHandle +0x4a` (윗 16비트 비교 앞) · `user32!RegisterClassExWOWW +0x2e4` (`EAX` = 검사 결과) · `gdiplus!InternalGdiplusStartup +0x1d6` (`RAX` = `CachedGdiRegion`) · `+0x293` (`RAX` = `DesktopIc`).

## 다음 요청 후보

- **N1 (바로 다음 벽)**: `gdiplus!InternalGdiplusStartup +0x296` — `CreateICA("DISPLAY")` : gdi32 `CreateICA` (썽크 `0xace0`) → 지연 → gdi32full `CreateICA` (`0x42c90`, 29B) → `bCreateDCA` (`0x42ce0`, `+0xd4`) → `hdcCreateDCW` (`0x42ef0`, `+0xc9 NtGdiOpenDCW`) 이 돌려줄 HDC 모양 (종류 `0x10000`, DC 칸, `DC_ATTR` 크기 · 칸) 과 `GetDeviceCaps(0x58/0x5a)` · `GpDevice::GpDevice(hdc)` 가 읽는 칸. 이 요청 그림에선 `.asm` 은 `CreateICA` · `bCreateDCA` · `hdcCreateDCW` 만 뽑음 (아래 "스크립트 문제" 때문에 `hdcCreateDCW` 아래는 일부만).
- **N2**: 그 뒤 벽 셋 — `InitImagingLibrary` (`+0x632`) · `InitSystemFontsDirs` (`+0x744`) · `GetGpInstalledFontCollection` (`+0x784`) 가 0 을 내는 조건. 지도 (`_map/GdiplusStartup_*`) 에서 depth 3~4.
- **N3**: DC 칸과 `DC_ATTR` 전체 모양 (sast/139 N4): `NtUserGetDCEx` · `NtGdiOpenDCW` 가 만드는 칸, `pcfLocateCFONT` · `CFONT`, `GetDCDpiScaleValue`, `bComputeTextExtent` 가 syscall 없이 폭을 돌려주는 조건.
- **N4**: win32k 쪽 표 채우기: `GdiProcessCallout` (`0xe2010`) · `HmgInsertObjectInternal` (`0x2b4a0`) · `bSetStockObject` 세션 배열 ↔ 공유 `+0x1800b0` 연결. 에뮬레이터가 "미리 채울 칸" 을 정확히 하려면 필요.
- **N5**: `NtQueryInformationProcess(0x25)` · `NtQueryLicenseValue` 를 부르는 kernelbase `GetVersionExA/W` 줄 (낮은 우선순위 — 이 요청의 분기에는 안 쓰임).

## 스크립트 문제

- 파일 · 증상: `.claude/skills/ghidra-headless-export/scripts/ExportFunctions.java` (줄 번호는 안 열어 봄). gdi32full `^(CreateICA|bCreateDCA|hdcCreateDCW)$` depth 1 export 중 **SCRIPT ERROR** (`run_ghidra.sh` 종료 코드 4): `java.io.FileNotFoundException: …/ms/decomp/gdi32full/_unique_storage_struct_wil__details__resource_policy_struct__RTL_CRITICAL_SECTION____ptr64_void____cdecl___… .c (File name too long)` (`ms/ghidra/gdi32full.export.s141b.stdout` 57줄, `ms/ghidra/gdi32full.export.log`).
- 원인 추정: `hdcCreateDCW` 가 부르는 `~unique_storage<struct wil::details::resource_policy<…>>` 처럼 이름이 255 바이트를 넘는 C++ 템플릿 함수를 파일 이름으로 그대로 쓴다.
- 영향: 목표 함수 셋 (`CreateICA` · `bCreateDCA` · `hdcCreateDCW`) 의 `.c` · `.asm` 과 `_calls.tsv` 는 쓰였지만 (확인함), 그 뒤에 쓰려던 depth-1 callee 일부는 안 쓰였을 수 있다. 이번 결과서는 **목표 함수 셋만** 근거로 썼다.
- 고칠 방법 제안: 파일 이름을 만들 때 길이를 ~120 자로 자르고 뒤에 해시 8자리 (예: `_ab12cd34`) 를 붙임. 같은 일이 gdi32full · kernelbase · rpcrt4 의 다른 C++ 함수에도 날 수 있다.
- (스크립트 문제 아님) gdiplus analyze 로그 `WARN … WindowsResourceReference … osgi.ee=UNKNOWN` 한 줄은 Ghidra 자체 분석기가 JDK 문제로 못 도는 것 (gdi32full analyze 로그에도 같은 줄). PDB 붙이기 (`SetPdb`) · `ExportIndex` 는 정상 (5239개).

## 판정

**분석성공.** 질문 1 은 `[정적]` 으로 닫음 — 첫 가지 = gdi32 `CreateRectRgn` 안의 공유 핸들 표 칸 검사 (`+0x19c~+0x1f3 → +0x116`), 에뮬레이터 로그의 syscall 차례 (`NtGdiCreateRectRgn` → `NtGdiDeleteObjectApp`, `NtGdiOpenDCW` 없음) 가 줄 단위로 맞음. 질문 2 는 닫음 — 표 시작 · 칸 크기 · 칸 번호 식 · 종류 · 윗 16비트 · 주인 · 통과 조건 · 스톡 핸들 꼴 · `RegisterClassExWOWW` 거절 (`0x57`) · AT03 실험 설명까지. 질문 3 은 닫음 (읽은 함수 한정 — `CreateFontIndirectW` 의 몸 · `DC_ATTR` 전체 · 글꼴 사본 쪽은 `[공백]`). 칸 이름 (`+0x8` 주인 등) 은 `[추측]`, 오프셋 · 검사 · 줄은 `[정적]`. 진짜 표 칸 값 · 진짜 핸들 값은 요청대로 안 함. 이번에 읽은 MS 코드는 레포에 붙이지 않았다 (`ms/decomp` · `ms/disassemble` 경로와 `+0x…` 로만).

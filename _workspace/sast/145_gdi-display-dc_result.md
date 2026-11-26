# 145 gdi-display-dc — 정적 분석 결과

- 요청서: `_workspace/sast/145_gdi-display-dc.md` (원문 `../PE_emulator/_workspace/05_tuning_planner/367_internals-gdi-display-dc.md` 먼저 끝까지 읽음. 먼저 읽는 것: `_workspace/sast/141_gdi-startup_result.md` 전부, `runs/03_sprint/run144-gdi-startup.md` 의 flow-writer 대조 · 고쳐 읽기)
- 대기열: Q278a (Q278 의 N1). **정적만.** 진짜 핸들 · `DC_ATTR` 덤프는 안 함 (Q277 몫).
- 모듈 / 바이너리 (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`) — **전부 이미 analyze 됨, 다시 analyze 안 함** (`-readOnly` export · refs 만):
  - gdi32 · gdi32full · gdiplus (`GdiPlus.dll`): asm 읽기 + 없는 함수만 `^이름$` export.
  - win32kbase (`win32kbase.sys`) · win32kfull: **"⚠ 조건"을 지켰다.** 시작할 때 `_workspace/sast/142_dialog-window_result.md` 가 없어서 gdi32 · gdi32full · gdiplus 일을 먼저 끝냈고, 그 사이 142 결과서가 생긴 것 (`2026-11-26 18:29:57`) 을 본 **뒤에야** win32kbase · win32kfull 을 열었다 (export 18:32 ~ 18:36, 끝난 뒤 `ps` 로 Ghidra 0개 확인). sast/143 (rpcrt4 · rpcepmap) · sast/144 (combase · rpcss) 프로젝트는 안 건드림. **sast/146 이 이 결과서가 생기길 기다렸다가 win32k 를 열기로 되어 있으므로, 이 파일이 있으면 win32k 프로젝트는 비어 있다.**
- PDB GUID (`ms/sym/*.pdb.info.txt` — export 한 모듈, 기존 프로젝트와 **같음**):
  - gdi32 `92C6EC2ABE478836C99BC83C2F3AE3551` · gdi32full `A6245E4F5C78EB06438EA4820F1F6EB41` · gdiplus `DDF19AA09F285660C86065C38EF751A41` (셋 다 run144 와 같음)
  - win32kbase `D22DFDA3D54B87E6BD0567A57877904C1` (sast/141 · 142 와 같음) · win32kfull `5CD57181BCFDC5AE8F4819BEB1196F091` (sast/142 와 같은 파일, 이번엔 `NtUserGetDCEx` · `NtUserGetWindowDC` 둘만 뽑음)
- 버전 대조 (알려진 사실 + 요청서가 적은 줄 — 전부 asm 에서 다시 찾음): **일치.** `hdcCreateDCW +0xc9 NtGdiOpenDCW` · `+0x1cd bLoadSpooler` · `+0x243 LoadUserModePrinterDriver` · `+0x339 pldcCreate` · `+0x5cc IcmInitLocalDC` · `+0x5dd` · `+0x5ed` · `+0x614` · `+0x693` · `+0x7a8`, `bCreateDCA +0xd4`, `CreateICA` rva `0x42c90`, `GetDeviceCaps +0x163 ~ +0x1cb` · `+0x1c0 TEST byte [..],0x2`, gdiplus `InternalGdiplusStartup +0x280 · +0x296` — 모두 요청서 · run144 와 같다.
- 뽑은 것 (이번에 새로 만든 파일; 기존 파일은 덮지 않음):
  - gdi32 `CreateCompatibleDC` `IsCreateCompatibleDCPresent` `CreateICW` `CreateDCA` `CreateCompatibleBitmap` `GetDIBits`
  - gdi32full `CreateCompatibleDC` `CreateCompatibleDCAdvanced` `IcmInitLocalDC` `CreateCompatibleBitmap` `GetDIBits`
  - gdiplus `EpScanGdiDci` `EpScanEngine` `ConvertBrushToGdi` (둘) `ConvertAlphaToGdi` `ExtractPixelFormatFromHDC` (+ depth 1)
  - win32kbase `NtGdiOpenDCW` `NtGdiGetDeviceCaps` `hdcOpenDCW` `GreGetDeviceCapsInternal` `UserGetDesktopDC` `GreCreateDisplayDC` `HmgAllocateDcAttr` `HmgFreeDcAttr` `SetupDCAttributes` `DCMEMOBJ` (둘) `RestoreAttributes` (둘) `CreateCacheDC` `GreCreateCompatibleDC` `GrepCreateCompatibleDC` `NtUserGetDC` `_GetDCEx` (+ 이름 확인용 callee 몇)
  - win32kfull `NtUserGetDCEx` `NtUserGetWindowDC`
  - refs: gdi32 `0x27600` (`CreateICA` 지연 가져오기 칸) → `ms/disassemble/gdi32/_refs/0x27600.tsv`
  - `.asm` 에 없는 몇 개는 `objdump -d` (PE, Ghidra 아님) 로만 읽음 — 표시는 `[objdump]`: gdiplus `GpDevice::IsValid` (rva `0xd4a50`) · `DpDriver` 빈 가상함수 (`0xda8e0`) · `GpSemaphore::Initialize` (`0x93668`) · user32 `GetDC` (`0x4a8c0`) · 문자열 · 데이터 칸 초깃값 (gdi32full `.rdata` / `.data`).
- 메모리: `free -g` available 27 GB, export 한 번에 3 초 안쪽. 임시 파일은 scratchpad `s145/` 아래.

## 먼저 읽는 한 줄 답

1. **(1a) `NtGdiOpenDCW` 인자 9 칸** `[정적]`: `(1 이름 포인터, 2 DEVMODE, 3 포트 이름 포인터, 4 종류, 5 화면 표시, 6, 7, 8, 9 출력 포인터)`. `CreateICA("DISPLAY",0,0,0)` = `(0, 0, 0, **2**, **1**, 0, 0, 0, &지역변수)`. `CreateDCA("DISPLAY",…)` 은 4 째만 **0**. 즉 **IC 표시 = 4 째 (`2`)**, 에뮬레이터 로그의 5 째 `1` 은 "화면" 표시 (`bCreateDCA` 가 "DISPLAY" 로 알아본 결과). **9 째 (출력 포인터) 는 화면 길에서 유저 쪽이 안 읽는다** — 읽는 줄은 프린터 길 (`hdcCreateDCW +0x413`) 하나뿐. 커널도 1 째가 NULL 이면 2 ~ 9 째를 통째로 안 본다 (`NtGdiOpenDCW +0x39 JZ → +0x1b8 UserGetDesktopDC`).
2. **(1b) 프린터 길 건너뛰기**: syscall **앞** = `bCreateDCA` 가 "DISPLAY" (`_stricmp`, 대소문자 무시) 또는 `\\.\DISPLAY` 로 시작하는 이름 (`_strnicmp` 11 글자) 이면 `EBX = 1` → `hdcCreateDCW +0x76 TEST R9D · +0x79 JZ` 가 안 뛰어서 `+0x12d ~` 프린터 문 (`bLoadSpooler` · `OpenPrinterW` · `LoadUserModePrinterDriver` · `DocumentEventEx`) 을 통째로 건너뜀. syscall **뒤** = 프린터 핸들 칸 (`[RSP+0x58]`) 이 0 이면 (화면 길은 항상 0) HDC 의 종류가 무엇이든 `+0x31c CMP · +0x322 JZ → +0xf1` 로 바로 돌려줌. `pldcCreate` · `IcmInitLocalDC` · ICM 칸 읽기는 **전부 "종류 ≠ `0x10000` 이고 프린터 핸들 ≠ 0"** 일 때만 닿는다.
3. **(1c) syscall 뒤 `hdcCreateDCW` 가 보는 것**: **반환이 0 인가** (`+0xd8 TEST · +0xdb JZ → +0x13a`) 와 **종류가 `0x10000` 인가** (`+0xe5 CMP`) 둘뿐. 그 밖에 읽는 칸 · ICM · 더 나가는 syscall **없음**. `+0x5cc IcmInitLocalDC` · `+0x5ed ~ +0x614` 칸 읽기 · `+0x693` · `+0x7a8 NtGdiDeleteObjectApp` 은 화면 길에서 **안 닿는다.** 화면 길의 NULL 은 **`NtGdiOpenDCW` 가 0 을 돌려줄 때 하나뿐.**
4. **(1d) HDC 꼴**: **IC 도 DC 와 똑같이 종류 비트 `0x10000`** (유저 쪽은 IC · DC 를 구분 못 함 — 4 째 인자 `2` 는 커널만 봄). 그 이유: 이후 `GetDeviceCaps` 가 `+0x59 AND 0x7f0000 · +0x66 JNZ` 로 `0x10000` 이 아니면 표를 보기도 전에 `pldcGet` 길 (→ 0 + `TEB+0x68 = 6`) 로 가기 때문. 스톡 비트 (`0x800000`) 는 꺼짐 (DC 는 스톡 아님 — 켜지면 `DeleteObject` 가 syscall 없이 TRUE). 고유 번호 (24 ~ 31 비트) 는 아무거나 — 0 이 가장 쉬움. run144 "핸들 비트" 규칙 그대로: `H = 번호(< 0x10000) | 0x10000 | 고유<<24`.
5. **(2) 표 칸**: run144 3절 **그대로.** 이번에 읽은 DC 쪽 함수 (`GetDeviceCaps` · `CreateCompatibleDC` · `CreateCompatibleBitmap` · `GetDIBits` · `pldcGet`) 가 모두 같은 다섯 검사 (`+0xe == 1` · `+0xc == H>>16` · `(+0x8 & ~1) == gW32PID` · `+0x10` ≠ 0 · 풀어도 ≠ 0). 실패했을 때 가는 곳만 함수마다 다르다 (아래 (2a)).
6. **(2) `DC_ATTR` 크기 · 잡는 쪽**: **`HmgAllocateObjectAttr` 이 아니다.** 그건 **0x18 B 칸** 170 개/쪽 짜리 (영역 등) — `+0xee MOV EDX,0xaa` · `+0x10b ADD RSI,0x18`. `DC_ATTR` 은 **`win32kbase!HmgAllocateDcAttr` (rva `0x2ad8c`)** — **슬롯 `0x170` B, 11 개/쪽** (`+0x18c MOV EDX,0xb` · `+0x1a9 ADD RDI,0x170`), 부르는 곳 `GreCreateDisplayDC +0x205`. 유저에게 복사되는 건 앞 **`0x160` B** (`RestoreAttributes +0x10 ~ +0x9e` 가 `0x90` + `0xd0`), `+0x160` qword 는 0 (`HmgAllocateDcAttr +0x99`). 유저 쪽 함수가 읽는 가장 먼 칸은 `+0x128` qword (→ 0x130 B).
7. **(2) `DC_ATTR +0x0` 비트 1 (`GetDeviceCaps +0x1c0`)**: 뜻 = **"이 DC 가 주 화면 장치 (primary display) 위에 있다 → 지표 몇 개는 공유 블록 `pGdiDevCaps` 에서 읽어도 된다."** 켜는 곳 = 커널 `GreCreateDisplayDC +0x231 CMP RDI,[..+0x30] · +0x243 OR [DC_ATTR],2` (장치 == 주 화면 장치일 때) — `NtGdiOpenDCW(NULL…)` · `NtUserGetDC*` (`CreateCacheDC`) · `NtGdiCreateCompatibleDC` (`GrepCreateCompatibleDC`) 가 **모두 `GreCreateDisplayDC` 를 부르니 같은 규칙.** 유저 쪽은 이 칸을 쓰는 줄이 없다. **0 이어도 되나: 된다** (그러면 지표 전부 `NtGdiGetDeviceCaps` syscall). **`+0x0` 비트 0** = "DIB 섹션이 골라져 있다" (`CreateCompatibleBitmap +0x12e` · `SelectObjectImpl +0x235`) — 화면 DC 는 0.
8. **(2) `+0x98` · `+0xa0` · `+0xa8` · `+0x128` 처음 값**: 유저 쪽 어디도 **값을 검사하지 않는다** (`SelectObjectImpl` 이 읽어서 "이전 값" 으로 돌려주고 새 값을 쓸 뿐). `+0x98` 의 처음 값은 **`0x12001f`** (`win32kbase!DCMEMOBJ +0x1b8`). 붓 · 펜 · 글꼴 처음 값은 **커널의 세션 기본 틀 `[W32GetSessionState()+0x58] + 0x90` (0x1b0 B)** 를 `DCMEMOBJ +0x74 ~ +0xec` 가 DC 안으로 복사한 것 → 틀 내용 (스톡 핸들인지) 은 `[공백]` (win32k 몫).
9. **(2) 다른 길 HDC**: 유저 쪽 검사는 **전부 같다** (gdi32 는 HDC 가 어디서 왔는지 모름). `NtUserGetDC` → `_GetDC` → `_GetDCEx` · `NtUserGetDCEx` / `NtUserGetWindowDC` (win32kfull) → `_GetDCEx`, `CreateCacheDC +0x83` · `GrepCreateCompatibleDC +0x126 / +0x256` · `UserGetDesktopDC +0x7b` 가 모두 `GreCreateDisplayDC` 를 부른다 `[정적]` (`_GetDCEx` 가 `CreateCacheDC` 를 직접 부르는 줄은 안 이음 `[공백]`). 다른 칸은 값뿐 — 메모리 DC (`CreateCompatibleDC`) 는 **원본 DC 의 `+0x108` 이 0 이어야** ICM 호출을 안 한다 (`CreateCompatibleDC +0x103`).
10. **(3a) `GetDeviceCaps(hdc, 0x58 / 0x5a)`**: 화면 DC 가 타는 쪽이 **어느 쪽이든 `NtGdiGetDeviceCaps` syscall.** `+0x1c0` 이 꺼져 있으면 모든 지표가 syscall 이고, **켜져 있어도 `0x58` · `0x5a` 는 블록 표에서 "case 1" (`+0x1c6 MOV EDX,EBX → +0x1cb NtGdiGetDeviceCaps`) 로 간다** (같은 길: `4 · 6 · 8 · 0xa · 0x28 · 0x2a · 0x2c · 0x58 · 0x5a`). 커널은 `GDIINFO (PDEV+0x848)` 의 `+0x28` (X) / `+0x2c` (Y) 에서 읽고 **DPI 가상화** 를 거친다 — 스레드 DPI 문맥 (`+0x190`) 또는 프로세스 (`+0x10c` 비트 8 ~ 16, `+0x110` word) 가 있으면 그 값 (`GreGetDeviceCapsInternal +0x7d · +0x14c · +0xc8 · +0xec · +0x11a · +0x132`). 유저 쪽은 **값을 안 검사한다**: `≤ 0` 이면 `96.0` 으로 바꿀 뿐 (`InternalGdiplusStartup +0x2f0 ~ +0x2f7`). "무엇과 같아야 하나" = **없음** (양수면 됨; 진짜와 맞추려면 프로세스 DPI 설정 `[W32PROCESS+0x110]` / `psi` 쪽 값).
11. **(3b) 지표 표**: 블록 쪽에서 읽는 칸 `0xc` → `+0x20` · `0xe` → `+0x24` · `0x26` → `+0x34` · `0x68` → `+0x4c` · `0x76` → `+0x6c` · `0x2` → `+0x4`, 그리고 **syscall 로 가는 지표**는 `0x8` · `0xa` (+ `0x58` · `0x5a` · `4` · `6` · `0x28` · `0x2a` · `0x2c`). 전체 표는 (3). `NtGdiGetDeviceCapsAll` (`+0x2f3`) 은 **종류 ≠ `0x10000` 인 DC (프린터 `0x210000` 등) 에서 `pldcGet` 이 LDC 를 찾고 그 캐시 플래그 `0x2000000` 가 꺼졌을 때만** — 화면 DC 길에서 안 닿는다.
12. **(3c) gdiplus 차례 5 ~ 7 (`+0x296 ~ +0x5e3`)**: `DesktopIc` 로 하는 GDI 일 = `GetDeviceCaps` ×2 (DPI) → **`GpDevice::GpDevice(hdc)` 안 `CreateCompatibleDC(hdc)` (`+0xaa`) · `GetDeviceCaps` 2 · (1 이면) 0xc · 8 · 0xa** → **`DpBitmap::InitializeForGdiBitmap` 안 `ExtractPixelFormatFromHDC(hdc)` = `CreateCompatibleBitmap(hdc,1,1)` · `GetDIBits` ×2 · `DeleteObject`** (한 번 더 `+0x60b` 에서). run144 의 NULL 열 곳 (`+0x341 ~ +0x5e3`) 중 **HDC · GDI 답에 달린 건 `+0x341` (GpDevice 무효) 하나**, 나머지 **아홉은 할당뿐** (앞의 `+0x296` CreateICA = 0 도 HDC 에 달림). `+0x341` 은 **`NtGdiCreateCompatibleDC` 가 0 이면** 또는 **`TECHNOLOGY == 1` 인데 `BITSPIXEL ≤ 8` 이면** 걸린다.
13. **통과 최소 (이 한 줄이 제일 중요)**: **`NtGdiOpenDCW` ≠ 0 과 `NtGdiCreateCompatibleDC` ≠ 0 만 있으면 `+0x5e3` 까지 간다.** 표 칸 · `DC_ATTR` 이 없어도 `GetDeviceCaps` 는 0 (+ 오류 코드 `0x57` 또는 `6`) 을 돌려줄 뿐이고 (DPI 0 → 96.0, `TECHNOLOGY` 0 → `BITSPIXEL` 안 봄), `ExtractPixelFormatFromHDC` 는 `CreateCompatibleBitmap` 이 0 이면 포맷 0 으로 조용히 끝난다. 다만 **"진짜 꼴" 로 채워 두는 게 N2 이후에 안전**하다 (아래 PE_emulator 쪽).
14. **"왜 362 샘플 `GetDeviceCaps` = 0" 보충**: 에뮬레이터의 `0x21000` 은 종류 비트 (16 ~ 22) 가 **`2`** (`0x21000 & 0x7f0000 = 0x20000`) 라서 `GetDeviceCaps +0x66 JNZ → +0x25d` 로 **표를 보기도 전에** 갈라지고, `pldcGet` 이 0 → **`TEB+0x68 = 6`** (`+0x281`) + 0. run144 6절의 "칸이 없으면 `0x57`" 은 종류 비트가 `0x10000` 일 때의 길이다.

## (1) 최소 길 지도 — `CreateICA("DISPLAY",0,0,0)` 부터 `+0x5e3` 까지

`[정적]` = `ms/disassemble/<모듈>/<함수>.asm +오프셋`. syscall 번호 = `ms/disassemble/win32u/_syscalls.tsv`. "반환을 유저 쪽이 보나" 칸이 이번 질문의 핵심.

### (1a) `CreateICA` 가 HDC 를 돌려줄 때까지

| 차례 | 부르는 것 / syscall (번호) | 조건 | 반환을 유저 쪽이 보나 | 0 이면 | 줄 `[정적]` |
|---|---|---|---|---|---|
| 0 | gdi32 `CreateICA` (rva `0xace0`, 썽크) → gdi32full `CreateICA` | 썽크는 `JMP [0x180027600]` 한 줄. 그 칸은 **지연 가져오기** (옆 `DelayLoad_CreateICA` rva `0xacec` 가 `LEA RAX,[0x180027600]`). 첫 부름에 풀림 — 풀기 syscall (`NtProtectVirtualMemory` ×2) 은 run144 `GdiValidateHandle` 과 같은 꼴 `[추측: 이번에 칸 안 풂]` | — | 풀기 실패면 예외 (NULL 이 아님) | `gdi32/CreateICA.asm +0x0` · `gdi32/_refs/0x27600.tsv` |
| 1 | gdi32full `CreateICA` (rva `0x42c90`, 29 B) | 5 째 인자 (스택) 에 **`1` (IC)** 을 넣고 `bCreateDCA` 부름. 끝에서 `GdiTrackHCreate` 로 꼬리 점프 | 안 봄 (그대로 넘김) | — | `gdi32full/CreateICA.asm +0x4 · +0xc (ret +0x11) · +0x18` |
| 2 | `bCreateDCA` (rva `0x42ce0`) | 이름 판정: ① 2 째 인자 (device) 가 NULL 이 아니면 `_strnicmp(device, "\\\\.\\DISPLAY", 11) == 0` → `EBX=1` · 그 이름으로 유니코드 문자열 만들기. ② 아니면 1 째 (driver) 가 NULL 이 아니고 `_stricmp(driver, "DISPLAY") == 0` → **`EBX = 1`, 문자열 안 만듦** (`RBP` = 0) ③ 아니고 `\\.\DISPLAY` 로 시작하면 `EBX=1` ④ 그 밖 = `EBX = 0` (프린터). 포트 (3 째) · DEVMODE (4 째) 가 NULL 이면 변환 건너뜀 | `RtlCreateUnicodeStringFromAsciiz` 반환 (AL) 은 봄 — 0 이면 NULL 돌려줌 — **"DISPLAY" + 나머지 NULL 이면 안 부름** | — | `+0x46 · +0x59 (ret +0x60) · +0x73 · +0x7e · +0x88 (ret +0x8f) · +0x9c · +0xa6 · +0xaf · +0xb8 · +0x134 · +0x195 · +0x1a5 · +0x1d2` |
| 3 | `bCreateDCA` → `hdcCreateDCW(RBP=0, R14=0, RSI=0, EBX=1, 5 째=1, 6 째=0)` | | **반환 `RAX` 를 저장 (`+0xd9`), 문자열 · DEVMODE 를 치우고 그대로 돌려줌** (검사 없음) | 그대로 0 | `bCreateDCA +0xd4 (ret +0xd9) · +0xe9 · +0xf5 ~ +0x121` |
| 4 | `hdcCreateDCW` 앞부분 | **`+0x76 TEST R9D,R9D · +0x79 JZ → +0x12d`**: `R9D` = 화면 표시 (`EBX`). 화면이면 안 뜀 → 프린터 문 (`+0x12d ~ +0x31c`: `bLoadSpooler` `+0x1cd` · `fpOpenPrinterW` `+0x1ec` · `LoadUserModePrinterDriver` `+0x243` · `DocumentEventEx` `+0x2f4`) 을 **통째로 건너뜀**. 화면 표시가 0 이고 1 째 칸이 NULL 이면 그냥 0 (`+0x12d · +0x130 · +0x136`) | — | — | `hdcCreateDCW.asm +0x76 · +0x79 · +0x12d · +0x1cd · +0x1ec · +0x243 · +0x2f4` |
| 5 | **`NtGdiOpenDCW`** (syscall `0x10c7` = 4295) | 인자 표 (1b). `RAX` → `RSI` | **`+0xd8 TEST RAX · +0xdb JZ → +0x13a`** | **NULL 반환** (아래 (1c)) | `hdcCreateDCW.asm +0x8a ~ +0xc6 (인자) · +0xc9 (ret +0xd0) · +0xd5 · +0xd8 · +0xdb` |
| 6 | (syscall 뒤) 종류 검사 | `ESI & 0x7f0000 == 0x10000` 이면 `+0xf1` 로 곧장. 아니어도 `+0x31c CMP [RSP+0x58],0 · +0x322 JZ → +0xf1` (프린터 핸들 0) | **종류만 봄** | 종류가 달라도 **그대로 돌려줌** (화면 길) | `+0xdd · +0xdf · +0xe5 · +0xeb · +0x31c · +0x322 · +0xf1 · +0xf7 · +0xfd · +0x100 · +0x106 (MOV RAX,RSI) · +0x12b RET` |
| 7 | `GdiTrackHCreate(hdc)` (gdi32full rva `0x40360`) | `RCX == 0` 이면 0 (`+0x9 · +0xc`). 아니면 전역 개수 `g_GDIHandleCount` (`0x18010c280`, 처음 0) 를 **`LOCK XADD` 로 1 올림** (`+0x14`). **1000 의 배수가 되면 `GetGuiResources(GetCurrentProcess(), 0)`** 를 부름 (`+0x39 JZ → +0x6e`, `+0x7f`) — 첫 DC 에서는 아님. 개수가 `g_DefaultHandleQuota` (처음 `0x2710`) 를 넘으면 그냥 돌려줌, 그 아래에서 추적기 (`g_GdiHandleLeakTracker`) 가 켜졌거나 개수 ≥ `g_LeakTrackingThreshold` (처음 `0x1f40`) 면 새는 핸들 추적 쪽으로 (`+0x9c ~ +0xf2` `InitOnceExecuteOnce` ×2) | HDC 를 **그대로 돌려줌** (`+0x5b MOV RAX,RBX`) | — | `gdi32full/GdiTrackHCreate.asm +0x9 · +0x14 · +0x39 · +0x3b · +0x44 · +0x52 · +0x7f · +0x9c` · 초깃값 `[objdump]` `.data` rva `0x1085a0`=`0x1f40` · `0x1085a4`=`0x2710`, `.bss` `0x10c280`=0 |
| 8 | gdiplus `+0x28c MOV [DesktopIc],RAX` → `+0x293 TEST RAX` · **`+0x296 JZ → +0x7d1`** | | **봄** | **1 로 나감** (`GdiplusStartup` 이 1) | `InternalGdiplusStartup.asm +0x280 (ret +0x287) · +0x28c · +0x293 · +0x296` |

- **syscall 은 `NtGdiOpenDCW` 하나뿐** (이 구간). 그 앞뒤로 나가는 것은 첫 부름의 지연 가져오기 풀기 (`NtProtectVirtualMemory` ×2 `[추측]`) 뿐.
- 이름 문자열 (`[objdump]` gdi32full `.rdata`): rva `0xc55d8` = `DISPLAY`, rva `0xc5928` = `\\.\DISPLAY`.

### (1b) `NtGdiOpenDCW` 인자 9 칸 (`hdcCreateDCW +0x8a ~ +0xc6` 가 만드는 값)

이름은 `[추측]` (공개 PDB 에 인자 이름 없음 — ReactOS 식 이름에 맞춘 것). **자리와 값은 `[정적]`.**

| 칸 | 값 (`CreateICA("DISPLAY")` / `CreateDCA("DISPLAY")`) | 어디서 | 유저 쪽이 syscall 뒤에 읽나 |
|---|---|---|---|
| 1 `RCX` 이름 (UNICODE_STRING*) | **NULL** — "DISPLAY" 는 문자열을 안 만든다 (`\\.\DISPLAYn` 이면 그 이름) | `+0xc6 MOV RCX,R13` ← `hdcCreateDCW` 1 째 | 안 읽음 |
| 2 `RDX` DEVMODE | `0` | `+0xc3 MOV RDX,RDI` | 프린터 길에서만 |
| 3 `R8` 포트 이름 | `0` | `+0xc0 MOV R8,R12` ← `[RSP+0x68]` | — |
| 4 `R9D` **종류** | **IC = `2`**, DC = `0` | `+0x8c ~ +0x97 NEG EAX · SBB R9D,R9D · AND R9D,2` (`EAX` = 5 째 인자) | — |
| 5 `[RSP+0x20]` **화면 표시** | **`1`** | `+0xb6 MOV [RSP+0x20],R12D` (`bCreateDCA` 의 `EBX`) | — |
| 6 `[RSP+0x28]` | `0` | `+0xae ~ +0xb2` ← `hdcCreateDCW` 6 째 (`bCreateDCA +0xb8` 이 0 을 넣음) | — |
| 7 `[RSP+0x30]` | `0` (프린터 길에선 프린터 핸들) | `+0xa9 MOV [RSP+0x30],RDX` (`XOR EDX,EDX` `+0x8a`) | — |
| 8 `[RSP+0x38]` | `0` (UMPD 포인터 `[RBX+0x10]`, 화면은 `RBX=0`) | `+0xa4 MOV [RSP+0x38],RCX` | — |
| 9 `[RSP+0x40]` **출력 포인터** | `&[RBP-0x78]` (스택 지역, 처음 0 — `+0x6f AND [RBP-0x78],RAX` 에서 `RAX = 0`) | `+0x9b LEA RAX,[RBP-0x78] · +0x9f MOV [RSP+0x40],RAX` | **프린터 길 `+0x413 MOV RAX,[RBP-0x78]` (→ LDC `+0x50`) 한 곳뿐.** 화면 길은 안 읽음 |

- 에뮬레이터 로그 (r18 `CreateDCA` 5 샘플) 의 `(0,0,0,0,1,0,0,0,포인터)` = 위 표의 DC 줄과 **같다.** `CreateICA` 는 4 째만 `2`.
- 커널 쪽 `NtGdiOpenDCW` (win32kbase rva `0x3a060`): **1 째가 NULL 이면** `+0x39 JZ → +0x1b8 ~ +0x1c1` 에서 **`UserGetDesktopDC(종류 = R9D, 0, 1)`** 만 부르고 2 ~ 9 째를 전혀 안 본다 (그러니 9 째에 쓰지도 않는다). 1 째가 NULL 이 아니면 `+0x171 hdcOpenDCW` 로 넘어가 9 째 (`+0x13a MOV RAX,[RSP+0xe0]`) 를 넘겨 줌 — `\\.\DISPLAYn` · 프린터 길.
- 다른 길 한 줄 (CreateDCA 5 샘플 참고): 종류 `0` 이면 `UserGetDesktopDC +0x5f ~` 가 **UI 한도 검사** (`CanPerformUIOperation` `+0x129` · `HasUILimit` `+0x196` · `ValidateHwndEx` `+0x157`) 를 거치고 안 되면 `+0x1e5 MOV RBX,R12` 로 **0 을 돌려줄 수 있다.** 종류 `2` (IC) 는 `+0x110 ~ +0x117` 에서 이 검사를 건너뛴다. 그러니 r18 `CreateDCA` = 0 5 샘플은 지금은 stub (기본 대답 0) 때문이고, 진짜 기준 실패가 아니다 `[정적: 조건만]`.

### (1c) NULL 로 나가는 가지 — `CreateICA("DISPLAY",0,0,0)` 안

| 가지 | 줄 `[정적]` | 조건 | 화면 길에서 닿나 |
|---|---|---|---|
| `NtGdiOpenDCW` 가 0 | `hdcCreateDCW +0xd8 · +0xdb JZ → +0x13a` → (`+0x14e TEST RSI` 가 0 이라 `+0x151 JNZ → +0x7a5` 를 안 탐) → `+0x169 XOR ESI,ESI · +0x16b JMP → +0xf1` → 0 반환 | 커널이 0 | **닿음 (유일)** |
| `+0x7a8 NtGdiDeleteObjectApp(RSI)` | `+0x13a` 안 `+0x14e TEST RSI · +0x151 JNZ → +0x7a5` | **HDC 를 이미 받은 뒤** 뭔가 실패했을 때 (`pldcCreate` NULL `+0x344` · DEVMODE 틀림 `+0x382` · 할당 실패 `+0x3d4 · +0x516 · +0x59b` · `DocumentEventEx = -1` `+0x2fc` · `UmpdPtr` 거짓 `+0x26c`) 모두 `+0x312` → `+0x13a` | **안 닿음** — 전부 "종류 ≠ `0x10000` 이고 프린터 핸들 ≠ 0" 블록 (`+0x31c ~`) 안 |
| `bCreateDCA` 의 NULL | `+0x142 JNZ`/`+0x148`, `+0x185 JZ → +0xdc1`, `+0x1dd JZ → +0xdc1` | 문자열 변환 · `GdiConvertToDevmodeW` 실패 | **안 닿음** ("DISPLAY" + 나머지 NULL) |
| `GdiTrackHCreate(0)` | `+0x9 · +0xc JZ → +0x65 XOR EAX` | 입력 0 | 위 첫 줄의 결과일 뿐 |
| 커널 쪽 | `UserGetDesktopDC +0x1e5` (UI 한도, 종류 0 만) · `GreCreateDisplayDC +0x2f8 → HmgFreeDcAttr` · `+0x40d XOR AL,AL` (세션 화면 장치 없음 `+0x57 JZ`) | 아래 win32k 몫 | 종류 `2` 에서는 UI 한도 안 탐 |

## (2) 칸 표 — 공유 핸들 표 칸 · `DC_ATTR`

### (2a) 표 칸 (run144 3절 그대로) + 이번에 읽은 DC 쪽 함수

표 시작 = PEB `+0xf8` 값 그대로, 칸 `0x18` B, 칸 번호 = `HANDLE_TO_INDEX(H)` (24 ~ 31 비트가 0 이면 `H & 0xffff`). DC 칸 = `+0x8` (`& ~1`) == `gW32PID` (**0 불가**) · `+0xc` word == `H >> 16` · `+0xe` byte == **1** · `+0x10` qword ≠ 0 이고 `ROR64(ptr ^ gCookie, gCookie & 0x3f)` 로 가린 값 (풀어도 ≠ 0) · `+0x0` · `+0xf` 는 안 읽음.

| 함수 | 검사 위치 | 실패하면 |
|---|---|---|
| `GetDeviceCaps` (gdi32 `0x17f0`) | `+0x163` (종류) · `+0x175` (윗 16) · `+0x187` (PID) · `+0x19b` (`+0x10` ≠ 0) · `+0x1ba` (풀어서 ≠ 0). **그 앞에 `+0x59 AND 0x7f0000 · +0x66 JNZ → +0x25d`** (종류 비트가 `0x10000` 아님) | 표 실패: `TEB+0x68 = 0x57` (`+0xbc`) + **0, syscall 없음**. 종류 비트 틀림: `0x660000` (메타) 이면 `TECHNOLOGY` 만 5 · 나머지 0 (`+0x318`), 그 밖은 `pldcGet` → 0 → `TEB+0x68 = 6` + 0 (`+0x281`) |
| `CreateCompatibleDC` (gdi32full `0x39470`) — **원본 DC 칸** | `+0xa7 GdiGetEntry` → `+0xb7` (종류) · `+0xc2` (윗 16) · `+0xd9` (PID) · `+0x101` (풀어서 ≠ 0) → **`+0x103 CMP [DC_ATTR+0x108],0`** | **실패해도 새 DC 를 그대로 돌려줌** (`+0x115` → `+0x122` `GdiTrackHCreate`). `[+0x108] ≠ 0` 이면 `+0x110 IcmEnableForCompatibleDC` 만 더 |
| `CreateCompatibleBitmap` (gdi32full `0x531b0`) | `+0xa7` · `+0xbb`(종류) · `+0xcd` · `+0xe6` · `+0xf6` · `+0x118` | **NULL (`+0x1fa`), syscall 없음.** 통과하고 `cx` 나 `cy` 가 0 이면 `PEB[+0xf8] + 0x180158` (스톡 비트맵 칸) 을 돌려줌 (`+0x120/+0x128 → +0x1e7`) |
| `GetDIBits` (gdi32full `0x37b10`) | `+0x19b ~ +0x215` | `GdiSetLastError(0x57)` + 0 (`+0xb5 · +0xba`), syscall 없음 |
| `pldcGet` (gdi32 `0x1d70`) | `+0x6d` · `+0x78` · `+0x86` · `+0x93` · `+0xb1` | 0. 통과하면 **`[DC_ATTR+0x90]`** (LDC 포인터) 를 돌려줌 (`+0xb3`) |

### (2b) `DC_ATTR` 칸 표 (이번 길 + `SelectObject` 가 읽고 쓰는 칸만)

크기: **슬롯 `0x170` B** (`HmgAllocateDcAttr`), 유저에 복사된 앞 `0x160` B, 유저 쪽이 읽는 가장 먼 칸 `+0x128` qword. 아래 이름은 `[추측]`, 오프셋 · 줄은 `[정적]`. 쓰는 곳이 "커널" 인 칸은 유저 쪽에 쓰는 줄이 없다.

| 오프셋 · 크기 | 뜻 `[추측]` | 읽는 줄 | 쓰는 줄 | 0 이어도 되나 / 필요한 값 |
|---|---|---|---|---|
| `+0x00` byte 비트 1 | 주 화면 장치 위의 DC (devcaps 블록 사용 가능) | `gdi32!GetDeviceCaps +0x1c0` | 커널 `GreCreateDisplayDC +0x243` (`OR [..],2`) | **0 이어도 됨** (지표 전부 syscall). 켜면 블록 `pGdiDevCaps` 가 읽힘 → (3) |
| `+0x00` byte 비트 0 | DIB 섹션이 골라짐 | `gdi32full!CreateCompatibleBitmap +0x12e` (켜지면 `NtGdiGetDCObject` `+0x147` + `GetObjectA` `+0x160` + `CreateDIBSectionInternal` 길) · `SelectObjectImpl +0x235 · +0x591` | 커널 | **0** (화면 DC) |
| `+0x90` qword | LDC 포인터 | `gdi32!pldcGet +0xb3` | 커널 / 프린터 길 | **0** (화면 DC 는 LDC 없음) |
| `+0x98` dword | 바뀜 표시 (커널과 맞추는 비트) | `SelectObjectImpl +0x3b3 · +0x758 (TEST 0x20000)` | `SelectObjectImpl +0x260 (OR 0x120000) · +0x3bc~+0x3bf (AND ~0x20, OR 0x10) · +0x569 · +0x647 (OR 0x2000) · +0x5ad (OR 0x1000)` | 처음 값 **`0x12001f`** (`win32kbase!DCMEMOBJ +0x1b8`). 유저 쪽은 0 이든 아니든 안 걸림 |
| `+0xa0` qword | 붓 | `SelectObjectImpl +0x5b7` | `+0x5c1` | 값 검사 없음 — **"이전 붓" 으로 돌려줌.** 0 이면 앱이 `SelectObject` 반환 0 (실패) 으로 읽을 수 있음 `[추측]` → 스톡 붓 권장 |
| `+0xa8` qword | 펜 | `+0x573 · +0x651` | `+0x57d · +0x658` | 같음 (스톡 펜 권장) |
| `+0xf0` dword | ICM 표시 (비트 0 = 켜짐, `& 0x11`) | `SelectObjectImpl +0x26a · +0x557 · +0x59f · +0x60a` · `GetDIBits +0x140~+0x146` (2 번째 호출부터, 아래) | 프린터 길 `IcmInitLocalDC +0x178 (BTS 0xd) · +0x1cc (BTS 0xc)` | **0** (켜지면 `IcmUpdateLocalDCColorSpace` · `IcmSelect*` 길로 감) |
| `+0xf8` qword | ICM 색 공간 칸 | `SelectObjectImpl +0x61a · +0x63a` (ICM 켜졌을 때만) | — | 0 |
| `+0x108` qword | ICM 정보 포인터 | `CreateCompatibleDC +0x103` · `SelectObjectImpl +0x277` · `GetDefaultDestinationProfile +0xc8` · `hdcCreateDCW +0x677` (프린터 길) | 커널 | **0** (≠ 0 이면 `IcmEnableForCompatibleDC`) |
| `+0x114` dword | 글 쪽 (언어팩) | `GetTextExtentPointW +0x124` (`gbLpk ≠ 0` 일 때만) | — | N3 몫 `[공백]` |
| `+0x128` qword | 글꼴 | `SelectObjectImpl +0x3a3` | `+0x3c5` | 값 검사 없음 (같은 글꼴이면 `NtGdiSelectFont` 안 부름 `+0x3aa`) |
| `+0x160` qword | — | — | `HmgAllocateDcAttr +0x99` 가 0 | 0 |

- **0 아닌 값이 *필요한* 칸은 없다.** (`+0x98` 의 `0x12001f` 는 커널이 채울 처음 값이지 유저가 요구하는 값이 아니다.)
- `GetDIBits` 의 `+0xf0` 읽기는 **첫 호출 (`biBitCount` = 0) 에서는 안 읽고** (`+0xdc` 에서 읽은 `biBitCount` 가 0 이라 `+0x13a TEST R15W · +0x13e JZ`), 둘째 호출 (`biBitCount` = 32 등) 에서만 읽는다.
- 처음 값 출처 `[정적]`: `DCMEMOBJ` (win32kbase rva `0x69a6c`) 가 `HmgAlloc(0x850, 종류 0x11)` (`+0x42 · +0x47`) 로 DC 객체를 잡고 **`[W32GetSessionState()+0x58]+0x90` 에서 `0x1b0` B 를 DC `+0x220` 으로 복사** (`+0x74 ~ +0xec`, 3 × 0x80 + 0x30), 그 위치를 DC `+0x3d0` (커널 쪽 `DC_ATTR` 포인터) 에 둠 (`+0xec`). 그 뒤 `GreCreateDisplayDC` 가 `HmgAllocateDcAttr` (유저 쪽 슬롯) 를 받고 `SetupDCAttributes +0x28` → `RestoreAttributes` 가 앞 `0x160` B 를 유저 슬롯에 복사, 표 칸 `+0x10` 에 **가린 포인터를 씀** (`SetupDCAttributes +0x6b ~ +0x82` — `gCookie` = `[Win32Process+0x118]`, 식은 run144 와 같음).
- 종류 `0x11` 객체는 `HmgModifyHandleType(.. | 0x210000)` 로 **프린터 ALTDC (`0x210000`)** 로 바뀔 수 있다 (`DCMEMOBJ +0x25b`, 인자 `ESI ≠ 0`) — 화면 길 아님.

## (3) `GetDeviceCaps` — 두 갈래 · 지표 표

`GetDeviceCaps` (gdi32 rva `0x17f0`) 차례: ① `+0x2b InitOnceExecuteOnce` (Edge GDI 판정) · `+0x41 JNZ → +0xd6` (Edge 길, 일반 프로세스는 `g_pEdgeGdi == 0`) ② `+0x47 CMP g_systemCallFilterId,5` (`Gdi32DllInitialize +0x62` 가 `GetProcessMitigationPolicy` 결과 `& 0xf` 를 씀 — 필터 없으면 0) ③ 종류 `0x10000` ④ 표 칸 검사 ⑤ **`+0x1c0 TEST byte [DC_ATTR],2`**.

| 갈래 | 줄 | 하는 일 |
|---|---|---|
| 비트 1 **꺼짐** | `+0x1c6 MOV EDX,EBX · +0x1cb NtGdiGetDeviceCaps` (syscall `0x12bf` = 4799) | 지표 값 그대로 syscall — **반환을 그대로** 돌려줌 |
| 비트 1 **켜짐** | `+0x1e8 MOV RDI,[pGdiDevCaps]` → `+0x1ef CMP EBX,2` (→ `[RDI+4]`) · `+0x208 CMP EBX,0xc` (→ `[RDI+0x20]`) · `+0x221 CMP EBX,0x79 · +0x224 JA → +0xb3` · `+0x231 · +0x239 · +0x243` 점프 표 | 지표 → 블록 칸 (아래 표). 표에 없는 지표는 `TEB+0x68 = 0x57` + 0 (`+0xb3 ~ +0xbc`) |
| (참고) 종류 ≠ `0x10000` · ≠ `0x660000` | `+0x26b pldcGet` → `+0x2d9 TEST [LDC+8],0x2000000` → `+0x2f3 NtGdiGetDeviceCapsAll` (syscall `0x12c0`) | 프린터 DC 등. LDC 안 `+0xa0` 에 캐시, 플래그 `+0x307 OR [LDC+8],0x2000000` |
| (참고) `g_systemCallFilterId == 5` 일 때 | `+0x419 CMP R9,-0x2321b000 · +0x420 JZ → +0x1e8` | 특별 HDC `0xFFFFFFFFDCDE5000` (user32 `GetDC` 가 같은 값을 `[objdump]` 돌려줌) 이면 블록 길. 일반 프로세스는 0 이라 안 탐 |

`pGdiDevCaps` = PEB `+0xf8` 값 `+ 0x180000` (`GdiProcessSetup +0x456`, run144). **지표 → 블록 칸** (`GetDeviceCaps` 안 점프 표를 `[objdump]` gdi32.dll rva `0x1c80` (dword 표 28 개) · `0x1cf0` (byte 표 0x7a 개) 에서 풀어 asm 줄에 맞춤):

| 지표 (십진 · 16 진) | 이름 (SDK) | 블록 칸 / 상수 | syscall 로 가나 |
|---|---|---|---|
| 0 · `0x0` | DRIVERVERSION | `+0x0` | |
| 2 · `0x2` | **TECHNOLOGY** | `+0x4` (`+0x1f4`) | |
| 4 · 6 · 8 · 10 (`0x4 · 0x6 · 0x8 · 0xa`) | HORZSIZE · VERTSIZE · **HORZRES · VERTRES** | — | **항상 syscall** (`+0x1c6`) |
| 12 · `0xc` | **BITSPIXEL** | `+0x20` (`+0x20d`) | |
| 14 · `0xe` | **PLANES** | `+0x24` | |
| 16 · `0x10` | NUMBRUSHES | 상수 `-1` | |
| 18 · `0x12` | NUMPENS | `+0x28` | |
| 22 · `0x16` | NUMFONTS | `+0x2c` | |
| 24 · `0x18` | NUMCOLORS | `+0x30` | |
| 28 · `0x1c` | CURVECAPS | 상수 `0x1ff` | |
| 30 · `0x1e` | LINECAPS | 상수 `0xfe` | |
| 32 · `0x20` | POLYGONALCAPS | 상수 `0xff` | |
| 34 · `0x22` | TEXTCAPS | `+0x64` | |
| 36 · `0x24` | CLIPCAPS | 상수 `1` | |
| 38 · `0x26` | **RASTERCAPS** | `+0x34` | |
| 40 · 42 · 44 (`0x28 · 0x2a · 0x2c`) | ASPECTX · Y · XY | — | **항상 syscall** |
| 88 · `0x58` | **LOGPIXELSX** | — | **항상 syscall** |
| 90 · `0x5a` | **LOGPIXELSY** | — | **항상 syscall** |
| 104 · `0x68` | **SIZEPALETTE** | `+0x4c` | |
| 106 · `0x6a` | NUMRESERVED | 상수 `0x14` | |
| 108 · `0x6c` | COLORRES | `+0x50` | |
| 110 · `0x6e` · 111 · `0x6f` | PHYSICALWIDTH · HEIGHT | `+0x54` · `+0x58` | |
| 112 · `0x70` · 113 · `0x71` | PHYSICALOFFSETX · Y | `+0x5c` · `+0x60` | |
| 116 · `0x74` | VREFRESH | `+0x68` | |
| 117 · `0x75` | DESKTOPVERTRES | `+0x70` | |
| 118 · `0x76` | **DESKTOPHORZRES** | `+0x6c` | |
| 119 · `0x77` | BLTALIGNMENT | `+0x74` | |
| 120 · `0x78` | SHADEBLENDCAPS | `+0x88` | |
| 121 · `0x79` | COLORMGMTCAPS | `+0x8c` | |
| 그 밖 (홀수 1 ~ 45 · 20 · 26 · 46 ~ 87 · 89 · 91 ~ 103 · 105 · 107 · 109 · 114 · 115 · `> 0x79`) | NUMMARKERS · PDEVICESIZE … | — | 블록 길에서는 `0x57` + 0 |

(지표 이름은 SDK 상수 — 칸 번호는 `[정적]`, 이름 짝은 `[추측]`.) 블록은 **`+0x0 ~ +0x90`**, 바로 뒤 `+0x94` 는 언어팩 칸 (run86), `+0x1800b0` 부터 스톡 배열 — 같은 쪽 (`0x180150` 안) 이다.

(3a) 화면 DC 에서 `0x58` / `0x5a` 값의 출처 (`NtGdiGetDeviceCaps`, win32kbase rva `0x2e700`):
- `+0x37 APIDCOBJ(hdc)` — HDC 를 못 잠그면 `+0xf2 EngSetLastError(6)` + 0 (`+0x43 JZ`). 잠그면 `[DC+0x30]` = 장치 (PDEV) → `+0x8e GreGetDeviceCapsInternal(pdev, 지표)` (rva `0x31ecc`).
- 지표 `0x58` → `GDIINFO.+0x28` (`+0x7d`), `0x5a` → `GDIINFO.+0x2c` (`+0x14c`), GDIINFO = `[PDEV]+0x848` (`+0x14 · +0x1a`). `0x8` → `+0x10`, `0xa` → `+0x14`, `0xc` → `+0x18` (15 면 16, `+0x248 ~ +0x254`).
- **화면 장치** (`[PDEV+0x28]` 비트 0, `+0x92 · +0x95 TEST CL,1`) 이면 DPI 가상화: 현재 스레드의 DPI 문맥 (`+0xc8 [스레드+0x190]`) 이 있으면 그 값, 없으면 프로세스 (`+0xec [W32PROCESS+0x10c]`), 둘 다 없으면 `0x12`. `(값 >> 8) & 0x1ff` 가 0 이 아니면 그것이 **DPI** (`+0xf6 ~ +0xff`), 0 이면 `GDIINFO.+0x2c` (`+0x101`) 이다가 프로세스 `+0x110` word 가 0 이 아니면 그 값 (`+0x11a ~ +0x126`). 지표 `0x58` · `0x5a` 는 **둘 다 이 DPI 값 하나** (`+0x128 ~ +0x132 MOV EBX,EBP`).
- 그래서 "96 고정" 이 아니라 **프로세스 / 스레드의 DPI 설정을 따라간다.** 에뮬레이터는 값을 정하니 **"한 프로세스 안에서 `0x58` = `0x5a` = 같은 DPI"** 와 "0 이 아님" 만 지키면 된다 (`GdiplusStartup` 은 `≤ 0` 이면 96.0 으로 돌림 — `+0x2f7`). 진짜와 맞추려면 `[W32PROCESS+0x110]` 가 써지는 곳 (`SetProcessDpiAwarenessContext` 길) 이 필요 → 아래 "win32k 몫 남음".
- `0x8` · `0xa` (해상도) 는 DPI 가 100 이 아닌 가상화일 때 `EngMulDiv` 로 줄이는 갈래가 있다 (`+0x1a9 ~ +0x1e4`). 에뮬레이터가 DPI 96 (`100%`) 이면 `GDIINFO.+0x10` · `+0x14` (1920 · 1080) 그대로 `[추측: 100% 가정에서 `[PDEV+0x990]` 갈래 안 읽음]`.

## (4) gdiplus 차례 5 ~ 7 — `DesktopIc` 로 하는 일 (`InternalGdiplusStartup +0x28c ~ +0x5e3`, 그리고 `+0x60b`)

`[정적] ms/disassemble/gdiplus/<함수>.asm`. NULL 가지는 모두 `+0x7d1` (→ 1) 로 간다.

| 차례 (`+0x…`) | 부르는 것 | GDI 호출 · syscall · 읽는 칸 | NULL 가지 · 조건 |
|---|---|---|---|
| `+0x28c ~ +0x2cb` | `Globals::DesktopIc = CreateICA`; `GetDeviceCaps(0x58)` · `(0x5a)` | syscall `NtGdiGetDeviceCaps` ×2 (또는 표 칸 실패 시 0). 값을 `float` 로 `DesktopDpiX/Y` 에 둠 | **`+0x296`** (HDC = 0 → **1**) · DPI `≤ 0` 은 가지 아님 (`+0x2e9 COMISS · +0x2f0 JNC · +0x2f5 JC` → `+0x2f7` 96.0) |
| `+0x30d ~ +0x33a` | `GpMallocEx(0x660)` → **`GpDevice::GpDevice(hdc)`** (rva `0x933e4`) → `CheckValid<GpDevice>` | 안: **`CreateCompatibleDC(hdc)` (`+0xaa`)** → `[+0x18]` · `GetDeviceCaps(hdc, 2)` (`+0xc2`) · 값 `== 1` (`+0xd3 · +0xd5 JZ → +0x1c5`) 이면 **`GetDeviceCaps(hdc, 0xc)`** (`+0x1cd`) 가 `≤ 8` 이면 `PaletteChangeNotification` (`+0x1e5`, 팔레트 길) · `GetDeviceCaps(8)` → `[+0x5f8]` (`+0xea`) · `GetDeviceCaps(0xa)` → `[+0x5fc]` (`+0x104`) · `HeapAlloc(0x5b8)` ×2 → `EpScanGdiDci(this)` ×2 (`+0x127 · +0x142 · +0x15a · +0x175`, 엔진 안 GDI 호출 없음) · `[+0x5e0] = hdc` (`+0x6a`) | **`+0x341`** ← `CheckValid<GpDevice>` = `[+0x8] == 0x76654431` (`IsValid` `[objdump]` rva `0xd4a50`). 그 값은 `+0x181 ~ +0x1a3`: `[+0x5d8] ≠ 0 · [+0x5d0] ≠ 0 · [+0x18] ≠ 0` 이면 `0x2a1c02eb + 0x4c494146 = 0x76654431`, 아니면 `0x4c494146`. → **HDC · GDI 답에 달림**: ① `CreateCompatibleDC` = 0 ② `TECHNOLOGY == 1` 이고 `BITSPIXEL ≤ 8` (`+0x1f7`) ③ 할당 실패 |
| `+0x34c ~ +0x36c` | `GpMallocEx(0x10)` → `DeviceList` | 할당뿐 | `+0x36c` — **할당뿐** |
| `+0x377 ~ +0x3b7` | `EngineDriver` (`DpDriver` 기본형, `GpMallocEx(0x28)`) | 할당뿐 (`[+0x8]` 에 표시값 `0x72446431` `+0x38b`) | `+0x3b7` — **할당뿐** |
| `+0x3bd ~ +0x3f3` | `DesktopDriver = DriverGdi(GpDevice)` (`HeapAlloc(0x68)`, rva `0xbf840`) | 안: `HeapAlloc(1)` · **`ConvertBrushToGdi()` → `GetStockObject(0)`** (rva `0xbf8bc`, `+0x1f`) — 반환은 칸에 저장만, 표시값 `0x72446431` 은 **무조건 씀** (`DriverGdi +0x68`) | `+0x3f3` — **할당뿐** (`CheckValid<DpDriver>` 는 표시값만 봄) |
| `+0x3f9 ~ +0x42d` | `GdiDriver = DriverGdi(GpDevice)` | 위와 같음 (`GetStockObject(0)` 한 번 더) | `+0x42d` — **할당뿐** |
| `+0x433 ~ +0x467` | `D3DDriver = DriverD3D` (rva `0xca2c0`) | `HeapAlloc(1)` 뿐 | `+0x467` — **할당뿐** |
| `+0x46d ~ +0x4a1` | `InfoDriver = DriverInfo` (`0xd4928`) | 칸 쓰기뿐 | `+0x4a1` — **할당뿐** |
| `+0x4a7 ~ +0x4db` | `MetaDriver = DriverMeta` (`0xbf7cc`) | `GpMallocEx(1)` · `ConvertBrushToGdi` (`GetStockObject(0)`) · **`ConvertAlphaToGdi` → `GetStockObject(4)`** (`0xbf91c`, `+0x1b`) | `+0x4db` — **할당뿐** |
| `+0x4e1 ~ +0x518` | `DesktopSurface = DpBitmap(NULL)` (`GpMallocEx(0x88)`, rva `0x92fc4`) | `hdc == NULL` 이라 GDI 안 부름 (`DesktopDpiX/Y` 만 읽음 `+0x20 ~ +0x3a`) | `+0x518` — **할당뿐** |
| `+0x51e ~ +0x563` | `GetSystemMetrics(0x4e · 0x4f)`, 0 이면 `(0)` · `(1)` | user32 (HDC 와 무관) | 가지 아님 (`+0x544 JZ · +0x548 JNZ`) |
| `+0x56e` | **`DpBitmap::InitializeForGdiScreen(Desktop 장치, w, h)`** (`0x932dc`, `+0x1e` → `InitializeForGdiBitmap` `0x93344`) | **`ExtractPixelFormatFromHDC([GpDevice+0x5e0])`** (`InitializeForGdiBitmap +0x46 · +0x4d`) — 아래 | 가지 아님 |
| `+0x588` | `DesktopDriver->vtbl[+0x10](DesktopSurface)` (간접 호출) | 대상 = `DpDriver` 빈 가상함수 (`0x1800da8e0`, `ret 0`) `[objdump]` — GDI 호출 없음 | — |
| `+0x5b5 ~ +0x5e3` | `g_pRemoteSurface = DpBitmap(NULL)` (`GpMallocEx(0x88)`) | GDI 안 부름 | `+0x5e3` — **할당뿐** |
| `+0x60b` (N2 앞) | `InitializeForGdiBitmap(g_pRemoteSurface, DesktopDevice, [+0x5f8], [+0x5fc])` | **`ExtractPixelFormatFromHDC` 한 번 더** (같은 GDI 일) | 가지 아님 (`+0x62b InitImagingLibrary` 가 N2) |

**`ExtractPixelFormatFromHDC(hdc)`** (gdiplus rva `0x17d60`, 556 B) — `[정적] gdiplus/ExtractPixelFormatFromHDC.asm`:

| 줄 | 하는 일 | syscall (win32u 번호) |
|---|---|---|
| `+0x41` | `memset` 으로 `BITMAPINFO` (0x424 B, `+0x44` 에서 시작) 비움 | — |
| `+0x4f` | **`CreateCompatibleBitmap(hdc, 1, 1)`** → 반환 0 이면 `+0x61 JZ → +0x18f` 로 **포맷 `0` 을 돌려줌 (조용히)** | `NtGdiCreateCompatibleBitmap` (`0x1046` = 4166) — 표 칸이 맞아야 나감 (`CreateCompatibleBitmap +0xa7 ~ +0x118`, 아니면 syscall 없이 NULL) |
| `+0x8e` | `GetDIBits(hdc, hbm, 0, 0, NULL, bmi(biSize=0x28), 0)` — 머리만 채움 | `NtGdiGetDIBitsInternal` (`0x107d` = 4221). `+0x9a MOVZX [bmi+0xe]` (**`biBitCount`**) · `+0xa8 CMP [bmi+0x10],3` (**`biCompression == BI_BITFIELDS`**) |
| `+0xd4` | (`biBitCount > 8` 이고 `biCompression == 3` 이면) `GetDIBits` 한 번 더 — 색 마스크 (`bmi+0x28 ..`) 읽음 `+0xe0 ~ +0xee` | 같은 syscall 한 번 더 (이때 `GetDIBits +0x140` 이 `[DC_ATTR+0xf0]` 를 읽음) |
| `+0x11b` 등 | 마스크 · 비트 수로 포맷 상수 (`0x22009` = 32bpp RGB 등) 만들기 | — |
| `+0x123` | `DeleteObject(hbm)` | 비트맵 표 칸이 맞으면 `NtGdiDeleteObjectApp` (`0x1020`), 아니면 syscall 없이 FALSE (run144 (3)) |

→ 포맷 값은 `DpBitmap[+0xc]` 에 저장될 뿐 **startup 의 NULL 가지에 안 쓰인다.**

## PE_emulator 쪽 — 지금 맞나·틀리나 (원문 "답이 오면 쓰는 곳")

(읽기만 — 경로 `../PE_emulator-BD/ext/syscall/syscall_layer_cpp/src/gui/` 의 머리 주석 · 반환값 줄만 봄. 고치지 않음.)

| 파일 | 맞나 · 틀리나 |
|---|---|
| `nt_gdi_init2.cpp` | **모자람 (run144 와 같음), DC 쪽 덧붙임:** 블록 `0x181000` 은 `pGdiDevCaps` (`+0x180000 ~ +0x180090`) 를 품는다 — 비트 1 을 켤 거면 거기에 (3) 의 칸을 채움. **안 켜면 블록 `0` 그대로여도 안전** (아래). 표 칸은 DC 한 줄 (아래 8 ~ 10) 이 더 필요 |
| `nt_gdi_create_rect_rgn.cpp` | run144 판정 그대로 (영역 칸 · 종류 4). **DC 일과 직접 관계 없음.** 같은 `HmgAllocateObjectAttr` (0x18 B 칸) 꼴을 쓰는 줄은 영역뿐 — DC 의 `DC_ATTR` 은 0x170 B 로 **따로** |
| 새 `NtGdiOpenDCW` | 만들 것. **인자 1 째 NULL 이고 4 째 `0` / `2`** 면 화면 HDC (종류 `0x10000`) 를 돌려줌. 9 째에 쓸 필요 없음. 1 째 ≠ NULL (`\\.\DISPLAYn` · 프린터) 은 이번 요청 밖 |
| 새 `NtGdiGetDeviceCaps` | 만들 것. (3) 의 지표 값. 기본 대답 0 이어도 `GdiplusStartup` 은 통과 (DPI 0 → 96.0) — 다만 `TECHNOLOGY` · `BITSPIXEL` · 해상도는 N2 이후 GDI+ 가 읽을 수 있음 |
| `nt_user_get_dc.cpp` (`0x21000`) | **틀림 (종류 비트).** `0x21000` 의 16 ~ 22 비트가 `2` 라 `GetDeviceCaps` 가 표 앞에서 갈라지고 (`+0x66`) `GetDC` 로 얻은 HDC 로 `GetDeviceCaps` · `SelectObject` · `GetTextExtentPointW` 가 전부 실패. **`번호 | 0x10000`** 꼴 + 표 칸 + `DC_ATTR` 필요 |
| `nt_gdi_create_compatible_dc.cpp` (`0x20c00`) | **gdiplus 에는 지금도 통과** (`GpDevice +0xb6` 는 0 아님만 봄, `CreateCompatibleDC` 가 원본 칸 검사에 실패해도 그대로 돌려줌 `+0x115`). **종류 비트 틀림 (`2`)** 은 이후 그 DC 로 하는 일에서 같은 실패 → 같은 꼴로 |
| 새 `NtUserGetDCEx` · `NtUserGetWindowDC` | 만들 것. 유저 쪽은 **`NtUserGetDC` 와 같은 꼴** (user32 `GetDC` 는 `NtUserGetDC` 로 바로 점프 — 필터 값 5 아니면; `GetDCEx` · `GetWindowDC` 는 win32u 로 점프하는 썽크뿐 `[objdump]`). 반환 0 이면 그대로 0 |

### stub 이 채울 최소 목록 (run144 목록 1 ~ 7 에 이어)

run144 1 ~ 7 (PEB `+0xf8` 블록 · 스톡 배열 · `0x12` · `0x13` · 영역 표 칸 · `PEB+0x140` 0) 은 **그대로.** 이어서:

8. **DC 핸들 `H_dc`** — 종류 `0x10000`, 스톡 비트 0, 번호 `idx < 0x10000` (표 칸이 `0x180000` 안), 고유 번호 0 (24 ~ 31 비트 0 이면 `HANDLE_TO_INDEX` 가 syscall 을 안 부름). 예 `0x00010c00` 이면 칸 번호 `0xc00`, 칸 위치 = `PEB[+0xf8] + 0xc00*0x18`. **`0x400` 배수를 유지**하면 기존 "NtClose 가 거절하는 꼴" 정책과 겹침 `[추측: 정책 몫]`.
9. **표 칸 (DC)** `PEB[+0xf8] + idx*0x18`: `+0x08` = `PID & ~3` (`gW32PID`) · `+0x0c` = `H >> 16` (= `0x0001`) · `+0x0e` = **1** · `+0x10` = `ROR64(블록주소 ^ gCookie, gCookie & 0x3f)` (`gCookie` 하위 6 비트 0 이면 `블록주소 ^ gCookie`). `+0x00` · `+0x0f` 는 0.
10. **`DC_ATTR` 블록** — 쓰기 가능 `0x170` B (읽는 쪽은 `0x130` B 까지), 0 으로 채우고: `+0x00` = **`0x02`** (비트 1 켜려면; 안 켜려면 0) · `+0x98` = `0x12001f` · `+0xa0` = 스톡 붓 핸들 · `+0xa8` = 스톡 펜 핸들 · `+0x128` = 스톡 글꼴 핸들 (값이 0 이어도 통과하지만 `SelectObject` 가 "이전 값" 으로 돌려줌 `[추측]`). **`+0x90` · `+0xf0` · `+0xf8` · `+0x108` 은 반드시 0.** 어느 스톡 핸들이어야 하는지는 `[공백]` (세션 기본 틀, win32k).
11. **`NtGdiOpenDCW`** → 8 ~ 10 의 HDC (**nonzero 필수**). IC (`2`) · DC (`0`) 를 유저 쪽은 구분 안 하니 같은 꼴. 9 째는 안 씀.
12. **`NtGdiCreateCompatibleDC(hdc)`** → **nonzero 필수** (`GpDevice +0xb6` → `[+0x18]` 가 0 이면 `+0x341` 에서 **1**). 가능하면 8 ~ 10 과 같은 DC 꼴. 원본 `DC_ATTR+0x108` 은 0 (아니면 `IcmEnableForCompatibleDC`).
13. **`NtGdiGetDeviceCaps(hdc, 지표)`** — 최소: `2 → 1` (`DT_RASDISPLAY`) · `0xc → 32` · `8 → 1920` · `0xa → 1080` · `0xe → 1` · `0x58` = `0x5a` → `0x60` · `0x68 → 0` · `0x26` (RASTERCAPS) · `0x76 → 1920` · 그 밖 0. **`2 → 1` 로 주면 `0xc` 는 반드시 `> 8`** (아니면 `+0x341`). 둘 다 0 으로 두는 건 안전 (`TECHNOLOGY` 0 이면 `BITSPIXEL` 을 안 물음). HDC 칸이 틀리면 커널 스텁도 `LastError 6` + 0 (`NtGdiGetDeviceCaps +0xf2`).
14. **(비트 1 을 켰다면) `pGdiDevCaps` 블록** `PEB[+0xf8] + 0x180000`: `+0x4 = 1` · `+0x20 = 32` · `+0x24 = 1` · `+0x34` (RASTERCAPS) · `+0x4c` (SIZEPALETTE 0) · `+0x6c` · `+0x70` (1920 · 1080) 등 (3) 표. **채우지 않으려면 비트 1 을 0 으로** — 블록이 0 이면 `TECHNOLOGY` 0 · `BITSPIXEL` 0 으로 읽혀 안전하지만 값이 의미 없다.
15. **안 만들어도 되는 것**: `NtGdiCreateCompatibleBitmap` · `NtGdiGetDIBitsInternal` · `NtGdiDeleteObjectApp` — 기본 대답 0 이어도 `ExtractPixelFormatFromHDC` 는 포맷 0 으로 조용히 통과. 비트맵 핸들을 줄 거면 run144 영역과 같은 방식 (종류 `0x50000` · 칸 `+0xe == 5`) + `GetDIBits` 의 `biBitCount` (`bmi+0xe`) · `biCompression` (`bmi+0x10` = 3) · 마스크를 채워야 함.
16. **조용한 부수 효과**: `g_systemCallFilterId` — `Gdi32DllInitialize +0x4b GetProcessMitigationPolicy(…, 0xb …)` 가 실패하거나 0 이면 0 (필터 없음). **`5` 를 돌려주면 `GetDeviceCaps` 가 다른 길** (`+0x419`).

**"그 뒤 벽" 한 줄**: `InternalGdiplusStartup +0x62b InitImagingLibrary` (→ 0 이면 `+0x632` — **N2**, DC 일 아님) · 이어 `+0x6d5 SysColorNotify` · `+0x73d InitSystemFontsDirs` · `+0x775 GetGpInstalledFontCollection` · `+0x7b3 BackgroundThreadStartup`.

## win32k 몫 남음

"⚠ 조건" 에 따라 시작은 목록만 적으려 했으나 **142 결과서가 생겨서 아래 일은 읽었다.** 읽은 것은 위 (1b) · (2b) · (3a) 에 들어 있다. **아직 못 읽은 것** (다음 요청 후보, 오케스트레이터가 부름):

1. **세션 기본 `DC_ATTR` 틀** `[W32GetSessionState()+0x58]+0x90` (0x1b0 B) 를 채우는 곳 — 스톡 붓 · 펜 · 글꼴 핸들 · `+0x0` · `+0xf0` · `+0x108` 의 처음 값. (`DCMEMOBJ +0x74 ~ +0xec` 가 복사하는 원본.)
2. `GrepCreateCompatibleDC` (win32kbase rva `0x14950`) 본문 — `GreCreateDisplayDC` 인자 (`+0x126` · `+0x256`) 와 메모리 DC 의 `DC_ATTR+0x0` 비트 (비트 0 은 언제 켜지나), `vSetDpiScaling` (`+0x2a4`).
3. `GDIINFO` (`PDEV+0x848`) 를 채우는 곳 (`+0x10 · +0x14` 1920 · 1080, `+0x28 · +0x2c` 96) 과 `pGdiDevCaps` 블록 (`+0x180000 ~ +0x180090`) 을 채우는 곳 — `GdiProcessCallout` 쪽 (run86 N4 와 겹침).
4. `[W32PROCESS+0x10c / +0x110]` · `[스레드+0x190 / +0x198]` (DPI 문맥) 이 써지는 곳 — `NtUserSetProcessDpiAwarenessContext` 길. `GreGetDeviceCapsInternal +0x1a9 ~ +0x1e4` (`EngMulDiv` 로 줄이는 갈래) 와 `[PDEV+0x990]`.
5. `_GetDCEx` → `CreateCacheDC` 로 가는 줄 (`DCE` 만들기) 과 `UserGetDesktopDC` 의 UI 한도 (`CanPerformUIOperation` · `HasUILimit`) 본문 — 메시지 상자 12 샘플 (`NtUserGetDCEx(0,0,3)` = 0) 과 `CreateDCA` 5 샘플.
6. `GreCreateDisplayDC` 조건 `[session+0xe338]→+0x30` (= 주 화면 장치) 가 세워지는 곳, `hdcOpenDCW` (프린터 · `\\.\DISPLAYn` 길, `UserGetMonitorDC`).

## 동적으로 확인할 것 (대기열 Q277 VM 에 얹을 꼴)

멈춤점은 **GUI 프로세스 (gdiplus 부르는) 이름으로 조건** (커널 멈춤점은 모든 프로세스에 걸림).

1. `gdi32full!hdcCreateDCW +0xd0` (`RAX` = HDC, `[RSP+0x58]` 프린터 핸들 = 0 인지) · `+0xf1` · `gdi32full!bCreateDCA +0xd9` (`RAX`) · `gdi32full!CreateICA +0x11`. HDC 비트: 종류 `0x10000` 인가, 스톡 비트, 고유 번호.
2. 그 HDC 의 표 칸 (`PEB+0xf8 + (H & 0xffff)*0x18`, 24 B: `+0x8 · +0xc · +0xe · +0xf · +0x10`) 과 **풀린 `DC_ATTR` 0x170 B 덤프** (특히 `+0x0 · +0x90 · +0x98 · +0xa0 · +0xa8 · +0xf0 · +0xf8 · +0x108 · +0x114 · +0x128 · +0x160`) — `gdi32!GetDeviceCaps +0x1c0` 에서 `R8` (= `DC_ATTR`), `[R8]` 비트 0 · 1.
3. `pGdiDevCaps` (`PEB+0xf8 + 0x180000`) 0x98 B 덤프 — 지표 `0x2 · 0xc · 0xe · 0x26 · 0x68 · 0x6c · 0x70 · 0x88 · 0x8c` 칸 값. `GetDeviceCaps` 끝 (`+0x1d2` · `+0x1f7` 등) 의 `EAX` — 지표마다. 특히 `0x58` · `0x5a` · `0x8` · `0xa`.
4. `gdi32full!CreateCompatibleDC +0x103` (`[R8+0x108]`) · `+0x115` (`RDI`) 와 그 DC 의 칸 · `DC_ATTR+0x0`. `gdi32full!CreateCompatibleBitmap +0x12e` (`[R8]` 비트 0) · `+0x1e0` (`RAX`) · `GetDIBits +0x133` (`EAX` · `biBitCount` · `biCompression`).
5. `gdiplus!InternalGdiplusStartup +0x293` (`RAX` = `DesktopIc`) · `+0x33f` (`EAX`) · `gdiplus!GpDevice +0x1a3` (`[RBX+8]` = `0x76654431` 인가) · `gdiplus!ExtractPixelFormatFromHDC +0x12f` (`EBX` = 포맷, 진짜는 `0x22009` 일 것 `[추측]`).
6. 커널 (같은 VM): `win32kbase!NtGdiOpenDCW +0x1c1` (`R9D` → 종류), `GreCreateDisplayDC +0x231` (hdev 비교) · `+0x243` (비트 1 켜는 자리) · `+0x20a` (`RAX` = 유저 `DC_ATTR` 슬롯), `DCMEMOBJ +0x1b8`, `SetupDCAttributes +0x82`, `NtGdiGetDeviceCaps +0x93` (`EAX`, 지표별), `GreGetDeviceCapsInternal +0xc8 · +0xec · +0x11a` (DPI 문맥 · 프로세스 칸), **`[W32GetSessionState()+0x58]+0x90` 0x1b0 B** 덤프 (스톡 핸들 값).
7. `gdi32full!GdiTrackHCreate +0x14` 의 `g_GDIHandleCount` (`0x18010c280`) — 처음 값이 0 인가, 1000 번째에서 `GetGuiResources` (`+0x7f`) 가 나가나 (이번 길과는 무관).

## 다음 요청 후보

- **N-win32k (위 "win32k 몫 남음" 1 ~ 6)** — 에뮬레이터가 DC 를 "진짜 꼴" 로 채우려면 1 · 3 이 필요, DPI 를 따라가려면 4. 급하지 않음 (D156: win32k 안 옮김).
- **N2**: `InitImagingLibrary` (`+0x62b`) · `SysColorNotify` · `InitSystemFontsDirs` · `GetGpInstalledFontCollection` (그대로).
- **N3**: 글 폭 (`GdiGetCharDimensions` · `pcfLocateCFONT` · `bComputeTextExtent`, `DC_ATTR+0x114` · `+0x128`) — 메시지 상자 12 는 N1 만으로는 안 풀림.
- (이번에 새로 보임) `GetDeviceCaps` 의 **`g_systemCallFilterId == 5` 길** (`+0x419`) 과 user32 `GetDC +0x0` 의 같은 검사 — 에뮬레이터의 `GetProcessMitigationPolicy` (`NtQueryInformationProcess` 쪽) 가 0 을 줄 때만 안전.

## 스크립트 문제

- **고쳐야 할 스크립트 오류 없음.** `run_ghidra.sh` export · refs 열 번 모두 종료 코드 0, 로그에 `SCRIPT ERROR` 0 줄 (`ms/ghidra/*.s145*.stdout` 검사).
- **주의 하나 (오류 아님, 제안만)**: `ExportFunctions.java` 의 파일 이름 (`used` 맵 `ExportFunctions.java:547`, `used.merge` `:559`, `__<rva>` 붙이기 `:561`) 은 **한 번의 실행 안에서** 같은 이름이 둘째부터 `__<rva>` 를 붙인다. 그래서 **정규식이 이미 `.asm` 이 있는 이름의 다른 함수까지 맞히면** 주소가 앞선 쪽이 평이름 파일을 가져가 **기존 파일 (다른 함수) 을 덮어쓸 수 있다.** 예: gdiplus `Initialize` 는 지금 `Initialize.asm` = `OSInfo::Initialize` (rva `0xd4980`, sast/141 · run144 가 줄을 인용) 인데 `GpSemaphore::Initialize` (rva `0x93668`) 가 주소가 더 앞이라 `^Initialize$` 로 뽑으면 뒤바뀔 것 `[추측: 이터레이션 순서는 안 확인]`. 이번엔 이 함수를 **뽑지 않고 `objdump` 로만 읽었다.** 제안: 같은 이름 파일이 이미 있고 첫 줄 `rva=` 가 다르면 `__<rva>` 를 붙이기. (`IsValid` · `PaletteChangeNotification` · `ConvertBrushToGdi` 도 gdiplus 에 같은 이름이 둘 이상 — `ConvertBrushToGdi` 는 새 파일이라 `ConvertBrushToGdi.asm` (rva `0xbf8bc`) + `ConvertBrushToGdi__df764.asm` 로 정상 갈렸다.)
- 지난 요청 (run144 "도구 메모") 의 `hdcCreateDCW` 아래 긴 이름 `FileNotFoundException` 은 이번에 **깊이 0 으로만 뽑아서 안 났다.**

## 판정

**분석성공.** 질문 1 · 2 · 3 (a)(b)(c) 모두 `[정적]` 줄 근거와 함께 닫음. 핵심 발견:
- 화면 길의 `CreateICA` 는 **syscall `NtGdiOpenDCW` 하나**를 부르고 그 반환은 **0 인가 · 종류가 `0x10000` 인가** 만 본다. 프린터 길 (`bLoadSpooler` · UMPD · `pldcCreate` · `IcmInitLocalDC` · `DocumentEventEx`) 은 **화면 표시 (`EBX = 1`) 때문에 syscall 앞에서, 프린터 핸들 0 때문에 syscall 뒤에서** 건너뛴다. 9 째 출력 포인터는 화면 길에서 안 읽는다.
- `DC_ATTR` 은 `HmgAllocateObjectAttr` 이 아니라 **`HmgAllocateDcAttr` (0x170 B 슬롯)**. `+0x0` 비트 1 = 주 화면 장치 위의 DC (커널 `GreCreateDisplayDC +0x243` 가 켬), 비트 0 = DIB 섹션.
- `GetDeviceCaps(0x58 / 0x5a)` 는 비트 1 이 켜져 있어도 **항상 syscall.** 커널은 DPI 문맥 · 프로세스 DPI 를 따라간다.
- gdiplus 차례 5 ~ 7 에서 **HDC · GDI 답에 달린 NULL 가지는 `+0x296` (CreateICA) 과 `+0x341` (GpDevice) 둘**, 열 곳 (`+0x341 ~ +0x5e3`) 의 나머지 아홉은 할당뿐. **`NtGdiOpenDCW` ≠ 0 · `NtGdiCreateCompatibleDC` ≠ 0 만으로 `+0x5e3` 까지 통과** (표 칸 · `DC_ATTR` 이 없어도).
- 에뮬레이터 `0x21000` · `0x20c00` 은 종류 비트가 `2` 라 `GetDeviceCaps` 가 표 앞에서 `LastError 6` + 0 으로 끝남 — run144 6절의 `0x57` 은 종류 비트가 `0x10000` 일 때의 길.

`[추측]` · `[공백]`: 칸 · 지표 이름 (SDK 상수 짝) · 인자 이름 · 스톡 핸들의 정확한 값 (세션 기본 틀) · 지연 가져오기 풀기 때 나가는 syscall · `_GetDCEx → CreateCacheDC` 줄 · DPI 100% 가 아닐 때의 `GreGetDeviceCapsInternal` 갈래. 진짜 핸들 · 표 칸 · `DC_ATTR` 덤프는 요청대로 안 했다 (Q277). 이번에 읽은 MS 코드는 레포에 붙이지 않았다 (`ms/decomp` · `ms/disassemble` 경로와 `+0x…` 로만).

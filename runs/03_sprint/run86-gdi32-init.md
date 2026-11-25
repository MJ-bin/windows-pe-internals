# 2026-11-13 run86 — gdi32 · gdi32full 시작 함수와 "창 안 만드는 프로세스" 최소 조건 (Q150b, PE_emulator 요청 28 마지막 조각): **요청 28 결론 — user32 를 import 만 한 프로세스가 진입점까지 가려면 엔진이 FALSE 를 막을 바깥 답 다섯 자리 (`NtQuerySystemInformation(0)` · `.mrdata` 열고 닫기 · csrss USERSRV 연결 · GDI 힙 · `NtGdiInit2`) 를 맞추고, 서로 다른 두 데이터 — csrss 답의 `gSharedInfo` / `psi` (run85) 와 PEB `+0xf8` 이 가리키는 GDI 공유 메모리 — 를 채워야 한다. gdi32 진입점은 syscall 하나 (실패해도 됨) 뿐이고 gdi32full 진입점은 이유 1 에서 아무것도 안 한다. 진짜 GDI 초기화는 user32 `DllMain +0x996 GdiDllInitialize` 가 지연 가져오기로 gdi32full 을 불러 CRT → gdi32full `DllMain` 으로 가는 길이고, 거기 `+0x2e NtGdiInit2` (win32u `0x12ee`) 가 그 스레드의 첫 win32k syscall 이다 (매니페스트 `uiAccess` 가 없어 user32 `+0x5d2` 를 안 타고 `gfServerProcess` 가 아닐 때). 엔진은 `NtGdiInit2` 에 0 아닌 값을 주고 PEB `+0xf8` 에 `0x180150`B 이상 읽을 수 있는 메모리 (스톡 핸들 `+0x180140` · `+0x180148` 은 0 아님) 를 걸어야 한다 — 그 칸을 쓰는 건 유저 DLL 이 아니라 win32k (`win32kbase!GdiProcessCallout +0x22b`) 다.** user32 `_UserClientDllInitialize +0x996` → gdi32 `GdiDllInitialize` (`JMP [0x280e0]`, 지연 가져오기) ⇒ gdi32full `GdiDllInitialize +0x29 InitializeGdiHeap` · `+0x43 _DllMainCRTStartup` ⇒ `dllmain_dispatch +0x89 DllMain` → `+0x2e NtGdiInit2` · `+0x67 GdiProcessSetup` (PEB `+0xf8`) · `+0x7c` · `+0x8b GetStockObject`

> **정정 (run121, sast/117)**: 아래 ⑧ 의 "짐 지도 3.3 의 `gdi32 6 (Gdi*)` 짐작은 그대로" — 그 짐작은 둘만 맞다. `GdiTebBatch` · `GdiBatchCount` 는 **gdi32full** 이 주로 쓰고 (gdi32 는 `InternalDeleteObject` · `DeleteRegion` 둘만), `GdiCachedProcessHandle` · `GdiClientPID` · `GdiClientTID` · `GdiThreadLocalInfo` 넷은 System32 어디에서도 쓰는 줄을 못 찾았다. "`DllMain` 줄기엔 `Gdi*` 쓰기가 없다" 는 그대로 맞다. 자세히는 `runs/03_sprint/run121-teb-other-dlls.md`.

> **고쳐 읽기 (run142, sast/139 — Q268)**: 표 A13 의 답 "아무 값 (0 이면 `GetAppCompatFlags2` 가 0)" — 정상 답은 **상수 1** 이다 (win32kfull `NtUserGetThreadState +0x11 CMP ECX,0xe` · `+0x14 JZ → +0x16c MOV EAX,1`, 칸을 안 읽음). 0 이면 user32 창 API 넷 (`MessageBox*` · `DialogBoxParam*` · `LoadIcon*` · `GetSystemMetrics`) 이 같은 꼴 가드로 실패한다. 데이터 칸 표의 TEB `+0x78` "포인터 — `NtGdiInit2` 가 GUI 로 바꾼 뒤 0 아님 `[추측]`" → 쓰는 곳은 `[정적]` win32kbase `xxxCreateThreadInfo +0x756` (값 = `PsGetThreadId` 반환, 포인터가 아닌 스레드 ID 로 보임 `[추측]`). 자세히는 `runs/03_sprint/run142-user32-connect.md`.

## PE_emulator 쪽에서 쓸 것

### 요청 28 전체 결론 (run85 + run86)

- **진입점까지 가려면 엔진이 꼭 맞춰야 할 바깥 답은 다섯 자리** `[정적]` (아래 표 A 의 A1 · A2 · A3 · A7 · A11). 나머지 syscall 은 실패해도 `DllMain` 이 FALSE 가 아니다.
  - A1 `NtQuerySystemInformation(0)` 성공 — user32 (run85).
  - A2 `.mrdata` 열고 닫기 (`NtProtectVirtualMemory` ×2) 성공 — user32 → ntdll `RtlInitializeNtUserPfn` (run85). 이 ntdll 함수는 프로세스당 한 번만 성공한다.
  - A3 csrss USERSRV 연결 답 — 보통 길은 `NtAlpcSendWaitReceivePort` 한 번 (run85). 그때 ntdll `CsrPortHandle` 이 0 이 아니어야 한다 (kernelbase 첫 연결 몫).
  - A7 GDI 힙 (`HeapCreate(0,0,0)` → `NtAllocateVirtualMemory`) 성공 — gdi32full (이번).
  - A11 **`NtGdiInit2` (win32u `0x12ee`) 가 0 아닌 값** — gdi32full `DllMain +0x2e` (이번).
- **데이터는 서로 다른 두 길에서 온다** `[정적]`.
  - user32 쪽: csrss 답 0x248B 의 `+8` 부터 → `gSharedInfo`, 첫 8B → `psi` (읽기 가능 `0x1b68`B, 0 으로 채워도 됨) (run85).
  - gdi32 쪽: **PEB `+0xf8` (`GdiSharedHandleTable`) 이 가리키는 GDI 공유 메모리** — 최소 `0x180150`B 읽기 가능, `+0x180140` (DC 브러시) · `+0x180148` (DC 펜) 은 0 아닌 값, `+0x180094` 는 0 (언어팩 건너뜀) (이번).
  - 이 둘은 같은 메모리가 아니다. `SharedSection` 뷰를 채운다고 GDI 쪽이 풀리지 않는다.
- **gdi32 · gdi32full 진입점은 거의 일을 안 한다** `[정적]`.
  - gdi32 진입점 (`Gdi32DllInitialize`) 의 syscall 은 `GetProcessMitigationPolicy` 안 `NtQueryInformationProcess(0x34)` 하나 — 실패해도 계속. win32k syscall 0, PEB `+0xf8` 안 읽음. FALSE 는 gdi32full (또는 gdi32min) 이 메모리에 없거나 그 안 이름을 못 찾을 때뿐.
  - gdi32full 진입점 (`entry`) 은 이유 1 에서 `_DllMainCRTStartup` 도 안 부르고 1 을 돌려준다.
  - **진짜 GDI 초기화는 user32 `DllMain` 의 `+0x996 GdiDllInitialize` 한 번** — gdi32 껍데기 → 지연 가져오기 → gdi32full `GdiDllInitialize` → CRT → gdi32full `DllMain`. "로더가 gdi32full `DllMain` 을 부른다" 고 흉내 내면 안 맞는다.
- **무조건 나가는 첫 win32k syscall = gdi32full `DllMain +0x2e NtGdiInit2`** `[정적]` — 매니페스트 `uiAccess` 가 없어 user32 `+0x5d2` 를 안 타고 `gfServerProcess` 가 아닐 때. user32 `DllMain` 본체의 win32k syscall 다섯은 모두 조건부 (run85) 다. 그중 `+0x5d2 NtUserSetProcessUIAccessZorder` (`0x157b`, 표 A5″) 하나만 `+0x996 GdiDllInitialize` 보다 **앞**이라, 그게 타면 그쪽이 첫 win32k syscall 이 된다. 나머지 넷은 `+0x9b8` 뒤 (A15). 최소 조건 (매니페스트 없음) 이면 다섯 다 안 나가니 결론은 같다. 커널은 이 안에서 스레드를 GUI 로 바꾸고, PEB `+0xf8` 을 잇고, `__ClientThreadSetup` 콜백을 부를 자리다 `[추측: 순서 · 때]`.
- **엔진이 `NtGdiInit2` 에서 할 최소 셋**:
  - ① 0 아닌 64비트 값 반환 (gdi32 `gCookie` 에 들어감) `[정적]`
  - ② PEB `+0xf8` 에 위 GDI 공유 메모리 연결 `[정적]`
  - ③ (선택) `KernelCallbackTable[84]` (`__ClientThreadSetup`) 콜백. 안 해도 `GdiProcessSetup` 첫 호출이 `DllMain +0x67` 로 옮겨질 뿐 진입점까지 갈 것으로 읽힌다 `[추측]`.
- **user32 가 FALSE 로 보는 건 음수뿐** `[정적]`: `GdiDllInitialize` 반환은 0 (성공 · 이미 함) · `0xc0000017` (힙) · `0xc0000001` (gdi32full `DllMain` FALSE). 뒤 둘이 user32 `+0x9af` 에서 FALSE → 로더 `0xc0000142`.
- **지연 가져오기를 처음 풀 때마다 `NtProtectVirtualMemory` ×2** `[objdump]` (`GdiDllInitialize` 한 번 · `GdiProcessSetup` 한 번). API 세트 세 이름이 PEB `ApiSetMap` 에서 `gdi32full.dll` 로 풀려야 한다.
- **DLL 넷은 `\KnownDlls` 에 있다** (`user32` · `win32u` · `gdi32` · `gdi32full`, run44 사진 `[관찰]` — run85 ⑤). win32u 는 진입점이 없다 (`AddressOfEntryPoint 0`).
- PE_emulator 가 지금 막힌 곳 (`NtOpenSection(SharedSection)` `0xc000003a`) 은 run85 대로 그때 `CsrPortHandle` 이 0 이었다는 뜻이다. 그걸 넘기면 다음으로 걸릴 곳은 A11 (`NtGdiInit2`) · PEB `+0xf8` 일 것이다 `[추측: 엔진이 지금 win32k syscall 에 무엇을 돌려주는지 모름]`.

### 원문 질문이 어디서 답해졌나

| 원문 요청 28 | 답한 곳 |
|---|---|
| 1 user32 `DllMain` 이 하는 일 · syscall 차례 · 답 · FALSE 자리 | run85 (차례 표 · FALSE 11 자리) + **run86** 표 A (A1 ~ A5″ · A15 가 user32 줄) |
| 2 공유 섹션 · `gSharedInfo` · `gpsi` 최소 칸 | run85 (데이터 칸 표) + **run86** 표 B "user32 쪽" 줄 |
| 3 GUI 스레드로 바뀌는 때 | run85 (그 스레드의 첫 win32k syscall 때 커널) + **run86** (그 첫 자리 = gdi32full `DllMain +0x2e NtGdiInit2` — 매니페스트 `uiAccess` 가 없어 user32 `+0x5d2` 를 안 타고 `gfServerProcess` 가 아닐 때. `uiAccess` 매니페스트면 user32 `+0x5d2` (`0x157b`) 가 먼저) |
| 4 gdi32 · gdi32full `DllMain` | **run86** (아래 질문마다 · 호출 관계) |
| 5 창 안 만드는 프로세스의 최소 조건 표 둘 | **run86** (아래 표 A · 표 B) |
| (지금 우리) win32u 이름표 | run85 (`ms/disassemble/win32u/_syscalls.tsv`) — 이번 길의 `0x12ee` · `0x1000` 도 그 표 |
| 끝 "다음 요청 후보" | **run86** (아래 "다음 요청 후보") |

### 질문마다 (결과서 sast/83 그대로, 근거 줄은 오케스트레이터 · flow-writer 가 `.asm` 으로 다시 봄)

| 질문 (원문 · sast/83 번호) | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 4 · 1 gdi32 `DllMain` | PE 진입점 = `Gdi32DllInitialize` (rva `0x5ed0`). 이유 1: `DisableThreadLibraryCalls` · `/GS` 쿠키 · `GetProcessMitigationPolicy(자기, 0xb, &v, 4)` (실패면 건너뜀, 성공이면 gdi32 `g_systemCallFilterId ← v & 0xf`) · `GetModuleHandleA("gdi32full.dll")` (없으면 `"gdi32min.dll"`) → `GetProcAddress("GdiDllInitializeWrapper")` → CFG 간접 호출 → 1. 그 주소는 gdi32full 의 **0 을 돌려주는 빈 함수** (`RET 0`, rva `0x71030`). win32k syscall 0, PEB `+0xf8` 안 읽음. FALSE 자리 둘: `+0x99 JZ` (이름 못 찾음) · `+0xc1` 안 탐 → `+0xc3` (gdi32full · gdi32min 둘 다 없음) | 정보 없음 (user32 에서 막혀 여기 못 옴). 엔진이 줄 답은 `NtQueryInformationProcess(0x34)` 하나, 실패도 됨 | `Gdi32DllInitialize` 전부 (flow-writer 읽음): `+0x4 · +0x7 · +0x14 · +0x20 · +0x2d · +0x4b · +0x59 · +0x62 · +0x6f · +0x7e · +0x8a · +0x99 · +0x9b · +0xa0 · +0xb2 · +0xc1 · +0xc3`, kernelbase `GetProcessMitigationPolicy +0x47/+0x4e` `[objdump]`, PE `AddressOfEntryPoint 0x5ed0` (`objdump -p`) |
| 4 · 2 gdi32full `DllMain` | 진입점 `entry` (rva `0x89720`) 은 이유 1 이면 `+0x2a JNZ → +0x3f` 로 아무것도 안 하고 1. 진짜 길: user32 `+0x996` → gdi32 `GdiDllInitialize` (`JMP [0x280e0]`, 지연) ⇒ gdi32full `GdiDllInitialize` (rva `0x82070`) → `InitializeGdiHeap` → `_DllMainCRTStartup(hmodUser32, 1, 0)` ⇒ `dllmain_dispatch` → **`DllMain` (rva `0x89600`)**. `DllMain` syscall: `K32GetModuleBaseNameW` 안 `NtQueryInformationProcess(0)` + `NtReadVirtualMemory` ×5 `[objdump]` · 레지스트리 셋 · **`NtGdiInit2` (`0x12ee`)** · `GetAppCompatFlags2` 안 `NtUserGetThreadState(0xe)` (조건부). FALSE 자리 여섯 (`+0x4a` · `+0x65` · `+0x6e` · `+0x77` · `+0x9e` · `+0xa3`) — `+0xa3` (`SETNZ` 로 0) 말고는 모두 `+0xaa` | 정보 없음. 알아둘 셋: ① `NtGdiInit2` 가 0 이면 `0xc0000142` ② 0 아닌 값이어도 PEB `+0xf8` 이 0 이면 `GdiProcessSetup +0x161` 접근 위반 (주소 `0x180094`) ③ gdi32full `DllMain` 은 로더가 아니라 user32 가 부른다 | `entry +0x21 · +0x26 · +0x28 · +0x2a · +0x2c · +0x3a`, `GdiDllInitialize` 전부, `DllMain` 전부, `dllmain_dispatch +0x6e · +0x89`, `GetStockObject +0x0 ~ +0x7a`, `GdiProcessSetup` (아래 호출 관계), refs `0xc1fe8` · `0xc1fe0` (flow-writer 읽음) |
| 5 · 3 최소 조건 표 둘 | 아래 표 A · 표 B | 맞음 (방향): 흉내 낼 핵심 하나가 `NtGdiInit2`. 바로잡을 점: "공유 핸들 표 = `SharedSection` 뷰" 같은 가정이 있으면 안 맞는다 — user32 `gSharedInfo` (csrss) 와 gdi32 GDI 공유 메모리 (win32k) 는 다른 길 | 아래 |
| 끝 · 4 다음 요청 후보 | 아래 "다음 요청 후보" | — | — |

### 표 A. syscall 답 표 (차례 순)

번호: `[ntdll]` 은 ntdll `Nt*` 스텁의 `MOV EAX,imm`, `[win32u]` 는 `ms/disassemble/win32u/_syscalls.tsv` (이번 스텁 셋 `.asm` 과 같음). "run85 차례 N" = run85 user32 차례 표 N번 줄.

| 차례 | DLL · 자리 | syscall (번호 · 이름) | nt 엔진이 줄 답 | 이게 없으면 |
|---|---|---|---|---|
| A0 | (로더 차례에 따라 user32 앞 · 뒤) gdi32 `Gdi32DllInitialize +0x4b/+0x52` → kernelbase `GetProcessMitigationPolicy +0x47/+0x4e` `[objdump]` | `[ntdll 0x19]` `NtQueryInformationProcess` 클래스 `0x34`, 입력 `{0xb}`, 길이 8 | 성공 + 플래그 dword 0 (실패도 됨) | 통과 (gdi32 `g_systemCallFilterId` 0 그대로) |
| A1 | user32 `+0x1d1/+0x1d8` (run85 차례 3) | `[ntdll 0x36]` `NtQuerySystemInformation(0, 0x40B)` | 성공 + 버퍼 `+0x28` `MaximumUserModeAddress` | **FALSE** (`+0x1df JS`) |
| A2 | user32 `+0x1f3` `InitializeNtdllUserPfn` → ntdll `RtlInitializeNtUserPfn` 안 `LdrProtectMrdata(0)` · `(1)` (run85 차례 4) | `[ntdll 0x50]` `NtProtectVirtualMemory` ×2 (`.mrdata` 열기 · 닫기) | 성공 | **FALSE** (`InitializeNtdllUserPfn` 0 → `+0x1fa JZ`, 결과서) |
| A3 | user32 `+0x2aa/+0x2b1` → ntdll `CsrClientConnectToServer +0xb5` → `CsrpClientConnectToServer +0x7f` → `CsrClientCallServer +0xed/+0xf2` (run85 차례 7) | `[ntdll 0x8e]` `NtAlpcSendWaitReceivePort` (서버 3 USERSRV) | 메시지 `+0x34` 0 + 캡처 버퍼 0x248B `+8` 부터 `SHAREDINFO` 모양, 첫 8B `psi` 는 `0x1b68`B 이상 읽기 가능 | 음수 → **FALSE** (`+0x2b8 JS`). `psi` 널 → `+0x38e` 접근 위반. `CsrPortHandle == 0` 이면 첫 연결 길 6 syscall (PE_emulator 가 본 `NtOpenSection` `0xc000003a`) |
| A4 | user32 `+0x463/+0x46a` `GetProcessMitigationPolicy` (run85 차례 12, user32 `g_systemCallFilterId == 7` 파일 값일 때) | A0 과 같은 모양 | 성공 + 0 (또는 실패) | 통과. 답의 `v & 0xf` 가 5 면 run85 차례 13 갈래 (`apfnDispatch` 힙 사본) |
| A5 | user32 `InitInstrument +0x98 ~ +0x156` · `InitProcessGdiScaling` · `InitProcessSystemDpiBehavior` (run85 차례 14 · 17) | `[ntdll 0x12]` `NtOpenKey` · `[0x17]` `NtQueryValueKey` · `[0xf]` `NtClose` (IFEO · `Error Message Instrument` · `Policies\…\Display`) | 키 없음 (`STATUS_OBJECT_NAME_NOT_FOUND`) 도 됨 | 통과 |
| A5″ | user32 `+0x5d2/+0x5d9` `NtUserSetProcessUIAccessZorder` (run85 차례 15 — `+0x576 InitInstrument` 뒤, `+0x5e5` A5′ 앞, **`+0x996` A6 보다 앞**). `+0x590 RtlQueryElevationFlags` 음수 아님 (`+0x59e JS`) · `[RSP+0x44]` 비트 0 꺼짐 (`+0x5a5 JNZ`) · `+0x5b9 RtlQueryInformationActiveActivationContext(5, …)` 성공 (`+0x5c7 JS`) · 매니페스트 `uiAccess` 참 (`+0x5c9 CMP` · `+0x5d0 JZ` 안 탐) 일 때만 | `[win32u 0x157b]` (조건부) — **타면 이게 이 스레드의 첫 win32k syscall** (A11 보다 앞) | 매니페스트 없음이면 안 부름 | FALSE 아님 |
| A5′ | (flow-writer 더함 — run85 보탠 것 ③) user32 TraceLogging 등록 ×3 (`+0x5e5` · `+0x5f1` · `+0x5fd`) | `NtTraceControl` `[추측: run62 · run68 꼴, user32 사슬은 안 봄]` | 성공 | 안 봄 (Q161 ⑦) |
| **A6** | user32 **`+0x996/+0x99d` `GdiDllInitialize`** → gdi32 껍데기 `+0x0 JMP [0x280e0]` → (첫 호출) ntdll `LdrResolveDelayLoadedAPI +0x188` → `LdrpHandleProtectedDelayload +0x3f0` · `+0x550` → `LdrpWriteBackProtectedDelayLoad +0x89` · `+0xf1` `[objdump]` | `[ntdll 0x50]` `NtProtectVirtualMemory` ×2 (gdi32 지연 가져오기 칸 쪽 열기 · 닫기 `[추측]`) | 성공 (칸 `0x280e0` 에 gdi32full `GdiDllInitialize` 주소가 쓰임). API 세트 `ext-ms-win-gdi-internal-uap-init-l1-1-0` → `gdi32full.dll` (PEB `ApiSetMap`) | 풀기 실패 → `LdrpRedirectDelayloadFailure` 갈래 `[추측]` |
| A7 | gdi32full `GdiDllInitialize +0x29` → `InitializeGdiHeap +0x18/+0x1f` `HeapCreate(0,0,0)` → ntdll `RtlCreateHeap` | `[ntdll 0x18]` `NtAllocateVirtualMemory` (횟수는 이번에 안 풂) | 성공 | `+0x32` **`0xc0000017`** → user32 FALSE |
| A8 | gdi32full `_DllMainCRTStartup +0x1c` `__security_init_cookie` → `+0x5b QueryPerformanceCounter` (쿠키가 파일 기본값 `0x2b992ddfa232` 일 때만 — `+0x1e CMP` · `+0x21 JNZ`, flow-writer `.asm`) | `[ntdll 0x31]` `NtQueryPerformanceCounter` — KUSD `QpcBypassEnabled` 가 꺼졌을 때만 `[추측]` | 성공 + 카운터 값 | 공통 (MSVC DLL 모두). run44 KUSD `QpcBypassEnabled` `0x83` `[관찰]` → 이 VM 에선 안 나갈 것 `[추측]` |
| A9 | gdi32full `DllMain +0x24/+0x29` → `AssertProcessIsNotFontdrvhost +0x56/+0x5d` → kernelbase `K32GetModuleBaseNameW` (call `+0x7d` · `+0xc2` · `+0x108` · `+0x159` · `+0x1ce` · `+0x241`, ret = call + 7) `[objdump]` | `[ntdll 0x19]` `NtQueryInformationProcess(자기, 0, 0x30B)` + `[ntdll 0x3f]` `NtReadVirtualMemory(−1)` ×5 | NtQIP: 성공 + `PebBaseAddress` (`+0x8`). NtRVM: 자기 주소 공간에서 그대로 (PEB `+0x10` · `+0x18`, `Ldr+0x20`, 로더 항목 0x88B, 이름 글자 0x22B) | 실패 → 이름 0 → 통과 (반환 안 봄). 이름이 `fontdrvhost.exe` 면 텔레메트리 갈래 |
| A10 | gdi32full `DllMain +0x29/+0x2e` `ReadDisableMetaFilesRegKey` (`+0xfe` · `+0x14c` · `+0x161`) | `[ntdll 0x12]` `NtOpenKey` (`…\CurrentVersion\GRE_Initialize`, `0x20019`) · `[0x17]` `NtQueryValueKey` (`DisableMetaFiles`, 클래스 2, `0x14`B) · `[0xf]` `NtClose` | 키 없음 가능, 있으면 REG_DWORD | 통과 (`gbDisableMetaFiles` 0) |
| **A11** | **gdi32full `DllMain +0x2e/+0x35`** (가져오기 칸 `0xc1fe8`) | **`[win32u 0x12ee]` `NtGdiInit2`** (인자 없음) — 이 스레드의 첫 win32k syscall (매니페스트 `uiAccess` 가 없어 user32 `+0x5d2` 를 안 타고 `gfServerProcess` 가 아닐 때. 아니면 A5″ `0x157b` 가 먼저) | **0 아닌 64비트 값** (`+0x44` gdi32 `gCookie` 에). 진짜 커널의 부수 효과: GUI 스레드 변환 · PEB `+0xf8` ← GDI 공유 메모리 (`win32kbase!GdiProcessCallout +0x22b` `[objdump + PDB 공개 이름]`) · `__ClientThreadSetup` 콜백 (A12) `[추측: 순서]` | 0 → `+0x4a JZ` **FALSE** → `0xc0000001` → user32 FALSE → `0xc0000142`. PEB `+0xf8` 이 0 이면 A13 에서 접근 위반 |
| A12 | (A11 안 커널 → 유저 콜백) user32 `__ClientThreadSetup` (PEB `+0x58` 표 84번째) → `ClientThreadSetup +0x93 GdiProcessSetup` (gdi32 `JMP [0x27b80]` — 처음이면 A6 처럼 `NtProtectVirtualMemory` ×2 더) · `+0x272` · `+0x29c` · `+0x356` … (run85) | `[win32u 0x151f]` · `[0x107b]` · `[0x1475]` · (`0x150a` 조건부) | run85 대로 | 엔진이 콜백을 안 부르면 `ClientThreadSetup` 이 안 돌아 `pdiLocal` · TEB `+0x820` 이 빈 채. 진입점 도달엔 필수 아님 `[추측]` (A13 이 `GdiProcessSetup` 첫 호출이 됨) |
| A13 | gdi32full `DllMain +0x67/+0x6c` `GdiProcessSetup` → **`+0x464/+0x46b`** user32 `GetAppCompatFlags2(0x400)` → `+0x45/+0x4c` | `[win32u 0x1000]` `NtUserGetThreadState(0xe)` — TEB `+0x78` 이 0 일 때만 | 아무 값 (0 이면 `GetAppCompatFlags2` 가 0) | 통과 |
| A14 | gdi32full `DllMain +0x7c` · `+0x8b` `GetStockObject(0x12 · 0x13)` | **없음** (공유 메모리 읽기) | — | 표 B |
| A15 | user32 `+0x9b8` 뒤 `InitProcessDpiAwareness` · 매니페스트 갈래 (run85 차례 19 · 20) | 조건부 win32k 넷 (`0x1576` · `0x1440` · `0x13ec` · `0x1579`) — `0x157b` 는 `+0x996` 앞이라 A5″ 로 옮김 | `psi` 0 채움 · 매니페스트 없음이면 안 부름 | — |

- **`DllMain` 이 FALSE 로 끝나는 syscall 자리는 A1 · A2 · A3 · A7 · A11 다섯.** 나머지는 실패해도 통과한다.
- A6 ~ A13 은 user32 `DllMain` 안에서 `GdiDllInitialize` 한 번 부르는 동안 한 스레드에서 일어난다.
- `GdiProcessSetup` 첫 호출의 `RtlEncodePointer(0)` ×25 (`+0x1d8 ~ +0x3d3`) 는 syscall 이 없다 — ntdll 쿠키 캐시를 `LdrpInitializeProcess +0x12f` 가 이미 채웠다 (run59 `RtlEncodePointer +0x43` 은 캐시 0 일 때만) `[정적: run59 줄 + 차례]`.

### 표 B. 데이터 칸 표

gdi32 · gdi32full PDB 는 공개 심볼만이라 구조체 타입이 0개다. **오프셋 · 크기 · 읽는 줄은 `[정적]`, GDI 공유 메모리 칸 이름 · 뜻은 `[추측]`**. `_PEB` · `_TEB` 이름은 ntdll PDB.

| 어디 | 구조체 · 칸 | 값 | 읽는 줄 | 이게 없으면 |
|---|---|---|---|---|
| **PEB `+0xf8`** `GdiSharedHandleTable` | 포인터 | **`NtGdiInit2` 가 끝날 때까지** GDI 공유 메모리 시작 주소. 유저 DLL 다섯 (gdi32 · gdi32full · user32 · ntdll · kernelbase) 은 안 씀, 쓰는 건 `win32kbase!GdiProcessCallout +0x22b` `[objdump + PDB 공개 이름]` | gdi32full `GdiProcessSetup +0x15a` · `+0x173` (첫 호출만) · `+0x438` (매번) | 0 → `+0x161 CMP [0+0x180094]` 접근 위반 |
| 그 메모리 `+0x180094` | dword | **0** (최소 — 언어팩 건너뜀) `[뜻 추측]` | `GdiProcessSetup +0x161` · `+0x17a` | 0 이 아니면 `+0x180 GdiInitializeLanguagePack` (→ `LpkInitialize` · user32 `InitializeLpkHooks`) 이 더 돌고, user32 `ClientThreadSetup` 의 `NtUserRegisterLPK` 조건이 된다 |
| 그 메모리 `+0x1800b0 + 8*i` (`i` = 0 ~ `0x15`) | qword ×22 (스톡 객체 핸들 표 `[뜻 추측]`) | **`+0x180140` (`i = 0x12`, DC 브러시) · `+0x180148` (`i = 0x13`, DC 펜) 은 0 아닌 값**. 나머지는 `DllMain` 이 안 읽음 | `GetStockObject +0x65 ~ +0x72` ← `DllMain +0x7c` · `+0x8b` | 둘 중 하나 0 → `+0x9e` / `+0xa3` **FALSE** |
| 그 메모리 `+0x180000` | (장치 정보 `[뜻 추측]`) | 주소만 계산해 `pGdiDevCaps` 에 (읽지 않음) | `GdiProcessSetup +0x456` | 읽는 건 그리기 쪽 (다음 요청 후보) |
| 그 메모리 크기 | | 최소 `0x180150`B 읽기 가능 (진짜는 더 큼 `[추측]`) | 위 칸들 | 작으면 접근 위반 |
| TEB `+0x78` `Win32ThreadInfo` | 포인터 | `NtGdiInit2` 가 GUI 로 바꾼 뒤 0 아님이 정상 `[추측]` | user32 `GetAppCompatFlags2 +0x12` | 0 이어도 `NtUserGetThreadState(0xe)` 한 번 더 (A13) |
| TEB `+0x810` · `+0x818` (`+0x800` `Win32ClientInfo` 안) | dword | 0 채움이어도 됨 → `GetAppCompatFlags2` 0 → `GdiBatchLimit` = `0x14` | `GetAppCompatFlags2 +0x22` · `+0x33` (TEB `+0x78` ≠ 0 일 때) — flow-writer `.asm` | |
| TEB `+0x40` `ClientId.UniqueProcess` | | 프로세스 ID | `GdiProcessSetup +0x142` → gdi32 `gW32PID` (`& ~3`) | |
| PEB `+0x10` · `+0x18`, `Ldr+0x20`, 로더 항목 (`DllBase +0x30` · `BaseDllName +0x58/+0x60`) | | 진짜 로더 목록 (exe 가 메모리 순서 목록 첫 칸) | kernelbase `K32GetModuleBaseNameW` 가 `NtReadVirtualMemory` 로 (A9) | 못 읽으면 이름 0 → 통과 |
| PEB `+0x58` `KernelCallbackTable` | 포인터 | user32 `+0x3fc` 가 `apfnDispatch` 로 (**`+0x996` 보다 먼저**), 84번째 칸 = `__ClientThreadSetup` (run85) | 커널이 A11 안에서 `[추측]` | 0 이면 콜백을 못 부름 |
| gdi32 데이터 칸 (gdi32full 이 가져오기 표로 씀) | `gMaxGdiHandleCount` (`0x25798`) · `gCookie` (`0x257a0`) · `pGdiSharedMemory` (`0x25778`) · `pGdiSharedHandleTable` (`0x257a8`) · `pGdiDevCaps` (`0x25788`) · `GdiBatchLimit` (`0x25770`) · `gW32PID` (`0x257b8`) · `ghICM` (`0x257b0`) · `fpClosePrinter` | `GdiDllInitialize +0x16` → `0x1000000` (매번) · `DllMain +0x44` → `gCookie` · `GdiProcessSetup` → 나머지 | gdi32full 가져오기 칸 `0xc10a0` · `0xc1088` · `0xc0fb8` · `0xc10a8` · `0xc1060` · `0xc1078` · `0xc1090` · `0xc1058` · `0xc1050` | gdi32 가 안 올라오면 로더 단계에서 끝 |
| gdi32full 전역 | `gbFirst` (`0x108474`) · `GdiDllFullyInitialized` (`0x10c590`) · `GdiHeap` (`0x109d20`) · `gbInitialized` (`0x108cf4`) · (flow-writer 더함) `ghbrDCBrush` (`0x10c578`) · `ghbrDCPen` (`0x10c580`) | 파일 값: `gbFirst` 1, 나머지 0 | `GdiProcessSetup +0x15`, `GdiDllInitialize +0x21`, `InitializeGdiHeap +0x8` | 메모리 이미지가 파일 값 그대로면 문제 없음 |
| gdi32 지연 가져오기 칸 `0x280e0` (`GdiDllInitialize`) · `0x27b80` (`GdiProcessSetup`) | | 처음엔 `DelayLoad_*` 껍데기 (`0xa765` · `0xc40c`), 풀린 뒤 gdi32full 주소 | gdi32 `GdiDllInitialize +0x0` · `GdiProcessSetup +0x0` | |
| PEB `ApiSetMap` | API 세트 | `api-ms-win-gdi-internal-uap-l1-1-0` · `ext-ms-win-gdi-internal-uap-init-l1-1-0` · `ext-ms-win-gdi-internal-desktop-l1-1-0` → `gdi32full.dll` | 로더 · `LdrpLoadForwardedDll` | gdi32 가 안 올라오거나 지연 풀기 실패 |
| 레지스트리 | `HKLM\Software\Microsoft\Windows NT\CurrentVersion\GRE_Initialize` · 값 `DisableMetaFiles` | 없어도 됨 | `ReadDisableMetaFilesRegKey +0xfe` · `+0x14c` | 통과 |
| **user32 쪽 (run85)** | csrss 답 버퍼 `+8 ~ +0x247` → `gSharedInfo`, 첫 8B → `gpsi` | `psi` = 읽기 가능 `0x1b68`B (0 채움): `+0x0` 비트 4 = 0 · `+0x8bc` 비트 `0x20` = 0 | `_UserClientDllInitialize +0x30b ~ +0x387` · `+0x38e` · `InitProcessDpiAwareness +0xa8` | `psi` 널 → 접근 위반 |
| user32 쪽 (run85) | ntdll `CsrPortHandle` (rva `0x1d4e20`) | 0 아님 (kernelbase 첫 연결 `NtConnectPort` 의 출력) | ntdll `CsrClientConnectToServer +0xa4` | 0 이면 첫 연결 길 (`NtOpenSection(SharedSection)` …) |
| user32 쪽 (run85) | ntdll `.mrdata` `NtUserPfn` 플래그 (`0x1e7340`) | 0 (파일 값) | `RtlInitializeNtUserPfn` | 1 이면 `0xc000000d` → user32 FALSE |
| user32 쪽 (run85) | PEB `+0x2c0` `SessionId` · `+0x90` `SharedData` · `+0x3` 보호 비트 | 세션 번호 · 널 · 0 | run85 데이터 칸 표 | 디렉터리 이름 · 보호 갈래가 바뀜 |

### 다음 요청 후보 (원문 "더 깊이 볼 것")

1. **win32k 쪽 — `win32kbase!GdiProcessCallout`** (rva `0xe2010`, `+0xca PsGetProcessPeb`, `+0x22b` PEB `+0xf8` 쓰기) 와 GDI 공유 메모리를 채우는 곳 (`+0x180094` · `+0x1800b0` 표 · `+0x180000`). 엔진이 흉내 낼 몫이라 가장 급하다. **원문 "win32k 커널 안쪽 깊이 — 이번에 넣지 않는 것" 의 예외 → PE_emulator · 사용자 결정** (run85 정적 ① `SHAREDINFO` · `SERVERINFO` 칸 이름과 같은 결정).
2. **`NtGdiInit2` 를 받는 win32kfull 쪽** — 돌려주는 값의 정체, GUI 변환 순서, `__ClientThreadSetup` 콜백이 `NtGdiInit2` 안 어느 때 도는지. 1 과 같은 결정.
3. **ntdll 지연 가져오기 길** — `LdrResolveDelayLoadedAPI` → `LdrpHandleProtectedDelayload` → `LdrpWriteBackProtectedDelayLoad` · `LdrpLoadForwardedDll` · `LdrpResolveProcedureAddress` (이번엔 `objdump` 로만). 엔진의 지연 가져오기 흉내에 바로 쓰인다.
4. **창 만들기** — `RegisterClassExW` · `CreateWindowExW` · `GetMessageW` / `DispatchMessageW` 가 읽는 `gSharedInfo` 칸, `ClientThreadSetup` 의 `InitClientDrawing` · `NtUserRegisterLPK` (run85 정적 ③ ④). 원문 "이번에 넣지 않는 것".
5. **GDI 그리기** — `CreateCompatibleDC` · `GetDC` · `SelectObject` · `BitBlt` 가 읽는 GDI 공유 메모리 칸 (핸들 표 항목 모양 · `+0x180000`), TEB `GdiTebBatch` (`+0x2f0`) 와 `GdiBatchLimit`, 첫 `NtGdi*` 몇 개의 번호 · 답. 원문 "이번에 넣지 않는 것".
6. **`LpkInitialize` · `InitializeLpkHooks`** — 언어팩 플래그 `+0x180094` 가 켜졌을 때 (최소 길엔 없음). run85 정적 ⑤ 와 같이.
7. **`msvcp_win.dll`** (gdi32full 이 정적으로 가져오는 C++ 런타임) 의 시작 함수 — 이번에 안 봄.

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적 (이번에 처음 analyze — 결과서):
  - gdi32 pdb `92C6EC2ABE478836C99BC83C2F3AE3551` (`ms/sym/gdi32.dll.pdb.info.txt`), `_index.tsv` 1319 함수
  - gdi32full pdb `A6245E4F5C78EB06438EA4820F1F6EB41` (`ms/sym/gdi32full.dll.pdb.info.txt`), `_index.tsv` 2649 함수
  - 두 PDB 모두 **공개 심볼만이라 타입이 0개** (`llvm-pdbutil dump -types` 0 records). GDI 공유 메모리 칸 이름은 PDB 로 못 붙인다.
- 읽기만 (다시 analyze 안 함): user32 pdb `CDB947D1B96B87977915DE04CF54279C1` · win32u pdb `6295787D0B7E537E98F77F31F4426D1C1` (run85).
- 프로젝트를 안 열고 파일을 `objdump` 로 읽음 (결과서 — 다른 decompiler 가 쓰는 중이었음): kernelbase · ntdll (이름은 `_index.tsv`), win32kbase (`.sys` + PDB 공개 이름, pdb `D22DFDA3D54B87E6BD0567A57877904C1` — `ms/sym/win32kbase.sys.pdb.info.txt`, Ghidra 프로젝트 없음).
- 버전 대조 (결과서 + flow-writer):
  - PE `AddressOfEntryPoint`: gdi32 `0x5ed0` = `Gdi32DllInitialize` 시작 (flow-writer `objdump -p` 도 같음), gdi32full `0x89720` = `entry`, win32u 0 (진입점 없음).
  - gdi32full `.asm` `CALL [0x1800c1fe8]` = 가져오기 표 `win32u!NtGdiInit2` 칸. gdi32 `JMP [0x1800280e0]` = 지연 가져오기 표 `GdiDllInitialize` 칸.
  - win32u 스텁 셋 `MOV EAX` 값 = `_syscalls.tsv` (`NtGdiInit2` `0x12ee` rva `0x7070` · `NtGdiInit` `0x12ed` `0x7050` · `NtUserGetThreadState` `0x1000` `0x12b0`) — flow-writer 다시 봄.
  - ntdll 알려진 사실 표 (`LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess`) 일치 (`_calls.tsv`, 결과서).

## 목적

- PE_emulator 요청 28 (user32 · gdi32 초기화 최소 길 — GUI 벽) 의 둘째 · 마지막 조각 (Q150b): 원문 4 (gdi32 · gdi32full `DllMain`), 5 (**창 안 만드는 프로세스가 진입점까지 가는 최소 조건 표 둘** — syscall 답 / 데이터 칸), 그리고 "다음 요청 후보". **"최소 길 지도" 수준** (사용자 결정 2026-11-12).
- 짝 바퀴 run85 (Q150a, sast/82) 의 "Q150b 로 넘길 것" 다섯에 답하고, sast/82 의 gdi32 쪽 짐작을 바로잡는다.

## 방법

- 정적: `_workspace/sast/83_gdi32-init.md` — gdi32 · gdi32full (새 analyze, PDB 붙음, `SCRIPT ERROR` 0개, 둘 다 1분 안쪽)
  - 뽑음 (`-readOnly`): gdi32 depth 2 (`Gdi32DllInitialize` · 껍데기 `GdiDllInitialize` · `GdiProcessSetup` · `LpkpInitializeEditControl` · `__security_init_cookie`), gdi32full depth 1 (`entry` · `GdiDllInitialize` · `_DllMainCRTStartup` · `dllmain_*` · `DllMain` · `GdiProcessSetup` · `GetStockObject` · `UspProcessAttach` · `GdiInitializeLanguagePack` · CRT · WIL 정적 초기화 12 + callee, `.asm` 79개). user32 depth 0 넷 (`GetAppCompatFlags2` · `GetThreadDpiAwarenessContext` · `InitializeLpkHooks` · `GetDpiForSystem`), win32u 스텁 셋 (`NtGdiInit2` · `NtGdiInit` · `NtUserGetThreadState`).
  - 지도 (새로): `ms/disassemble/gdi32full/_map/GdiDllInitialize_*` (90개) · `ms/disassemble/gdi32/_map/Gdi32DllInitialize_*`.
  - `refs`: gdi32full `0xc1fe0` · `0xc1fe8` · `0xc1ff0` (`NtGdiInit` · `NtGdiInit2` · `NtGdiInitSpool` 가져오기 칸) · `0x888e0` · `0x888f0`, gdi32 `0xa6e0` · `0xc400` · `0x280e0` · `0x27b80`, user32 `0xb4fe0` · `0xb4fd8`.
  - API 세트 풀이는 결과서가 `apisetschema.dll` 의 `.apiset` 를 직접 읽음 (scratchpad 임시 파이썬, 레포 밖).
- 결과서: `_workspace/sast/83_gdi32-init_result.md` (**분석성공 — 최소 길 지도 수준**. 질문 1 ~ 4 모두 `.asm` 줄로 답함. 공유 메모리 칸 이름 · 뜻은 `[추측]`, kernelbase · ntdll · win32kbase 줄은 `objdump`, 콜백 때 · 로더 차례 · 지연 풀기 횟수는 동적)
- 오케스트레이터가 `.asm` 으로 다시 맞춘 줄: gdi32 `Gdi32DllInitialize +0x99 JZ` · `+0xc1`, gdi32full `DllMain +0x2e NtGdiInit2` (win32u `0x12ee`) · `+0x4a JZ` · `+0x67 GdiProcessSetup`, user32 `_UserClientDllInitialize +0x996 GdiDllInitialize` (부르는 곳 하나). PEB `+0xf8` 를 쓰는 곳 `win32kbase!GdiProcessCallout +0x22b` `[objdump + PDB 공개 이름]`.
- flow-writer 도 다시 봤다:
  - `.asm` 전부: gdi32 `Gdi32DllInitialize` · `GdiDllInitialize` · `GdiProcessSetup` (껍데기), gdi32full `entry` · `GdiDllInitialize` · `DllMain` · `__security_init_cookie` (`+0x0 ~ +0xaf`), user32 `GetAppCompatFlags2`, win32u 스텁 셋.
  - `.asm` 줄만: gdi32full `GdiProcessSetup` (`+0xa` · `+0x15` · `+0x1b` · `+0x142 ~ +0x18e` · `+0x3e6 ~ +0x48e`), `GetStockObject +0x0 ~ +0x9d`, `AssertProcessIsNotFontdrvhost` call 줄, `ReadDisableMetaFilesRegKey` call 줄, `InitializeGdiHeap +0x18`, `_DllMainCRTStartup` · `dllmain_dispatch` · `dllmain_crt_dispatch` call · 점프 줄. gdi32 · gdi32full 뽑은 `.asm` 전체에서 `GS:` 줄 검색.
  - `refs` 파일: user32 `0xb4fe0` · `0xb4fd8`, gdi32full `0xc1fe0` · `0xc1fe8` · `0x888e0` · `0x888f0`, gdi32 넷.
  - PE 표: `objdump -p` 로 gdi32 진입점 · 가져오기 (`win32u.dll` · `api-ms-win-gdi-internal-uap-l1-1-0.dll`), gdi32full 가져오기 (`GDI32.dll` · `USER32.dll` · `win32u.dll`). (`pefile` 은 이 서버 파이썬에 없어 안 씀.)
  - 이름: `pdb_aliases.py --lookup` 로 이번에 이름을 쓴 함수 (gdi32full `DllMain` · `GdiDllInitialize` · `GdiProcessSetup` · `GetStockObject` · `entry` 등 14, gdi32 셋, user32 `GetAppCompatFlags2`) 가 별칭 무리가 아님을 확인. gdi32full rva `0x71030` 만 **같은 주소 이름 9개** (아래).
  - 사진 값 (run44 덤프 읽기 로그): PEB `GdiSharedHandleTable` · KUSD `QpcBypassEnabled` (아래 "보탠 것" ⑤ ⑥). 새 동적 실행은 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/{gdi32,gdi32full,user32,win32u}/_calls.tsv` · `.asm`, `[objdump]` 줄은 결과서)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `user32!_UserClientDllInitialize` | **`+0x996`** | `+0x99d` | `GdiDllInitialize` (IAT `0xb4fe0`) | 이유 1, 늘. refs 1줄 = 부르는 곳 하나 | [정적] (오케스트레이터 · flow-writer) |
| `user32!ClientThreadSetup` | `+0x93` | `+0x9a` | `GdiProcessSetup` (IAT `0xb4fd8`) | 첫 GUI 스레드 (run85). refs 1줄 | [정적] |
| `gdi32!GdiDllInitialize` (`0xa6e0`) | `+0x0` | (`JMP`) | `[0x280e0]` → gdi32full `GdiDllInitialize` (`0x82070`) | 지연 가져오기 칸 (처음 값 `DelayLoad_GdiDllInitialize` `0xa765`) | [정적] |
| `gdi32!GdiProcessSetup` (`0xc400`) | `+0x0` | (`JMP`) | `[0x27b80]` → gdi32full `GdiProcessSetup` (`0x81a00`) | 지연 가져오기 칸 (처음 값 `0xc40c`) | [정적] |
| `ntdll!LdrResolveDelayLoadedAPI` (`0xa47d0`) | `+0x188` | — | `LdrpHandleProtectedDelayload` (`0xa6100`) | `+0x17f test [항목+0x68],0x8000` 켜짐 (꺼지면 `+0x192 LdrpHandleUnprotectedDelayLoad`) | [objdump] |
| `ntdll!LdrpHandleProtectedDelayload` | `+0x13e` · `+0x2f7` · `+0x3f0` · `+0x550` | — | `LdrpLoadForwardedDll` · `LdrpResolveProcedureAddress` · `LdrpWriteBackProtectedDelayLoad` ×2 | | [objdump] |
| `ntdll!LdrpWriteBackProtectedDelayLoad` (`0xa45b0`) | `+0x89` · `+0xf1` | — | `NtProtectVirtualMemory` (`0x50`) | 지연 가져오기 칸 쓰기 열기 · 닫기 `[추측]` | [objdump] |
| `gdi32!Gdi32DllInitialize` (`0x5ed0`, PE 진입점) | `+0x14` · `+0x20` · `+0x2d` | `+0x1b` · `+0x25` · `+0x34` | `DisableThreadLibraryCalls` · `__security_init_cookie` · `GetCurrentProcess` | 이유 1 (`+0x4 CMP EDX,1` · `+0x7 JZ`) | [정적] |
| `gdi32!Gdi32DllInitialize` | **`+0x4b`** | `+0x52` | `GetProcessMitigationPolicy(자기, 0xb, &v, 4)` | `+0x59 JZ` (실패) 면 건너뜀, 아니면 `+0x62 g_systemCallFilterId ← v & 0xf` | [정적] |
| `kernelbase!GetProcessMitigationPolicy` (`0x216f0`) | `+0x47` | `+0x4e` | `NtQueryInformationProcess` (`0x19`) 클래스 `0x34` | | [objdump] |
| `gdi32!Gdi32DllInitialize` | `+0x6f` · `+0xb2` | `+0x76` · `+0xb9` | `GetModuleHandleA("gdi32full.dll")` · `("gdi32min.dll")` | 둘째는 첫째가 0 일 때만 (`+0x7e JZ → +0xab`). 둘 다 0 → `+0xc1` 안 탐 → `+0xc3` FALSE | [정적] |
| `gdi32!Gdi32DllInitialize` | `+0x8a` | `+0x91` | `GetProcAddress(…, "GdiDllInitializeWrapper")` | 0 → `+0x99 JZ → +0xc3` FALSE | [정적] (오케스트레이터) |
| `gdi32!Gdi32DllInitialize` | `+0x9b` | `+0xa0` | (CFG 간접 `__guard_dispatch_icall`) → gdi32full `0x71030` | 0 을 돌려주는 빈 함수 (`RET 0`) — 같은 주소 이름 9개, 후보: `GdiDllInitializeWrapper` (gdi32 가 찾는 이름) · `LpkPresent` · `GdiEntry12` · `GdiDisableUMPDSandboxing` · `GdiSetServerAttr` 등 (`pdb_aliases.py --lookup gdi32full.dll 0x71030`) | [정적] |
| `gdi32full!entry` (`0x89720`, PE 진입점) | `+0x2c` · `+0x3a` | `+0x31` · `+0x3f` | `_DllMainCRTStartup` (`0x86620`) · `UninitializeGdiHeap` | 이유 0 · 2 · 3 만 (`+0x21 JZ` · `+0x26 JZ` · `+0x28 CMP`). **이유 1 은 `+0x2a JNZ → +0x3f`** (아무것도 안 함, `EBX = 1`). 둘째는 이유 0 만 | [정적] |
| `gdi32full!GdiDllInitialize` (`0x82070`) | `+0x29` | `+0x2e` | `InitializeGdiHeap` (`0x93828`) | 이유 1 · 처음 (`+0x1f JNZ` · `+0x27 JZ` 안 탐). 0 → `+0x32 0xc0000017` | [정적] |
| `gdi32full!InitializeGdiHeap` | `+0x18` | `+0x1f` | `HeapCreate(0, 0, 0)` | `GdiHeap == 0` 일 때 | [정적] |
| `gdi32full!GdiDllInitialize` | **`+0x43`** | `+0x48` | `_DllMainCRTStartup(hmod 인자 = user32 hmod, 1, 0)` | 0 → `+0x4e/+0x53 UninitializeGdiHeap(0)` · `0xc0000001` | [정적] |
| `gdi32full!_DllMainCRTStartup` | `+0x1c` · `+0x38` | `+0x21` · (꼬리 `JMP`) | `__security_init_cookie` · `dllmain_dispatch` (`0x864e8`) | 쿠키는 이유 1 만 | [정적] |
| `gdi32full!dllmain_dispatch` | `+0x6e` · **`+0x89`** | `+0x73` · `+0x8e` | `dllmain_crt_dispatch` (`0x862f0`, 이유 1 → `+0x3c` 꼬리 → `dllmain_crt_process_attach` `0x86348`) · **`DllMain` (`0x89600`)** | `DllMain` 0 이면 `+0xa5 DllMain(0)` · `+0xb2` CRT 정리 뒤 0 | [정적] (flow-writer 줄) |
| `gdi32full!DllMain` | `+0x24` · `+0x29` | `+0x29` · `+0x2e` | `AssertProcessIsNotFontdrvhost` (`0x68700`) · `ReadDisableMetaFilesRegKey` (`0x5de34`) | 반환 안 봄 | [정적] |
| `gdi32full!AssertProcessIsNotFontdrvhost` | `+0x56` | `+0x5d` | `K32GetModuleBaseNameW(자기, NULL, buf, 0x11)` | `fontdrvhost.exe` 와 같을 때만 뒤 갈래 | [정적] |
| `kernelbase!K32GetModuleBaseNameW` (`0xb6900`) | `+0x7d` · `+0xc2` · `+0x108` · `+0x159` · `+0x1ce` · `+0x241` | call + 7 | `NtQueryInformationProcess` (클래스 0) ×1 · `NtReadVirtualMemory` (`0x3f`) ×5 | 어느 자리가 어느 것인지 결과서는 안 나눔 | [objdump] |
| `gdi32full!ReadDisableMetaFilesRegKey` | `+0xfe` · `+0x14c` · `+0x161` | `+0x105` · `+0x153` · `+0x168` | `NtOpenKey` · `NtQueryValueKey` · `NtClose` | 키 · 값 없어도 됨 (`+0x10c JS`) | [정적] |
| `gdi32full!DllMain` | **`+0x2e`** | `+0x35` | **`NtGdiInit2`** (IAT `0xc1fe8`, win32u `0x12ee`) | 늘. `+0x44 gCookie ← RAX`, 0 → `+0x4a JZ → +0xaa` FALSE | [정적] (오케스트레이터 · flow-writer) |
| `gdi32full!DllMain` | `+0x56` | `+0x5d` | `gdi32!LpkpInitializeEditControl(&LpkEditControlInternal, 0xe)` (gdi32 `0x6140`) | 1 아니면 `+0x65 JNZ` FALSE | [정적] |
| `gdi32full!DllMain` | **`+0x67`** | `+0x6c` | `GdiProcessSetup` (`0x81a00`) | 음수 → `+0x6e JS` FALSE | [정적] (오케스트레이터) |
| `gdi32full!DllMain` | `+0x70` | `+0x75` | `InitializeCFontCache` (`0x3f304`) | 0 → `+0x77 JZ` FALSE (늘 1) | [정적] |
| `gdi32full!DllMain` | `+0x7c` · `+0x8b` | `+0x81` · `+0x90` | `GetStockObject(0x12)` · `(0x13)` (`0x25e90`) | `ghbrDCBrush` · `ghbrDCPen` ← 반환. 앞이 0 → `+0x9e JZ` FALSE, 뒤가 0 → `+0xa3 SETNZ` 0 | [정적] |
| `gdi32full!GdiProcessSetup` | `+0x28 ~ +0xd2` (여섯) · `+0xf7` | `+0x2f ~ +0xd9` · `+0xfe` | `RtlInitializeCriticalSection` ×6 · `GetACP` | `gbFirst ≠ 0` (첫 호출). 음수 → `+0x489` 반환 | [정적] |
| `gdi32full!GdiProcessSetup` | `+0x180` | `+0x185` | `GdiInitializeLanguagePack` (`0x52550`) | `[PEB+0xf8]+0x180094 ≠ 0` (`+0x161` · `+0x168 JZ`) | [정적] |
| `gdi32full!GdiProcessSetup` | `+0x3e6` · `+0x3fa` · `+0x415` | `+0x3eb` · `+0x401` · `+0x41c` | `UspProcessAttach` (`0x6a1c4`) · `GetModuleHandleW(L"gdi32.dll")` · `GetProcAddress("DeleteEnhMetaFile")` | 첫 호출 | [정적] |
| `gdi32full!GdiProcessSetup` | **`+0x464`** | `+0x46b` | user32 `GetAppCompatFlags2(0x400)` | 늘 (`+0x428` 공통 끝) | [정적] (flow-writer — 결과서 한 곳 `+0x460`) |
| `user32!GetAppCompatFlags2` (`0x6100`) | `+0x45` | `+0x4c` | `NtUserGetThreadState(0xe)` (IAT `0xb65f8`, win32u `0x1000`) | TEB `+0x78` == 0 (`+0x12 CMP` · `+0x17 JZ`) | [정적] (flow-writer) |
| `win32kbase!GdiProcessCallout` (`0xe2010`) | `+0xca` | — | `PsGetProcessPeb` | 뒤 `+0x22b` 에서 PEB `+0xf8` 쓰기 꼴. 함수 크기 안 맞춤 | [objdump + PDB 공개 이름] |

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리. `⇒` 는 꼬리 · 껍데기 `JMP`.

```
[user32 시작 함수 안 — run85 차례 18. test.exe 는 user32 · gdi32 를 안 올림]
user32!_UserClientDllInitialize (0x6ba84)
 └─ +0x99d  GdiDllInitialize(hmodUser32, 1)       GDI32 가져오기 (IAT 0xb4fe0)        음수 → +0x9af JNS 안 탐 → +0x9b1 FALSE
      gdi32!GdiDllInitialize (0xa6e0)   +0x0 JMP [0x280e0]   ── 지연 가져오기 칸
        (첫 호출) ntdll!LdrResolveDelayLoadedAPI +0x188 → LdrpHandleProtectedDelayload +0x3f0 · +0x550
                  → LdrpWriteBackProtectedDelayLoad +0x89 · +0xf1 NtProtectVirtualMemory ×2      ── SYSCALL 0x50 [objdump]
      ⇒ gdi32full!GdiDllInitialize (0x82070)
           +0x16 *gMaxGdiHandleCount ← 0x1000000 · +0x1f 이유 ≠ 1 → 0 · +0x27 GdiDllFullyInitialized 이면 0
           ├─ (1) +0x2e  InitializeGdiHeap (0x93828) → +0x1f HeapCreate(0,0,0)          0 → +0x32 0xc0000017
           ├─ (2) +0x48  _DllMainCRTStartup (0x86620)(hmodUser32, 1, 0)                 0 → +0x53 0xc0000001
           │        +0x21 __security_init_cookie (쿠키가 파일 기본값일 때만 +0x5b QueryPerformanceCounter)
           │        └─ +0x38 JMP ▶ dllmain_dispatch (0x864e8)
           │              ├─ (1) +0x73  dllmain_crt_dispatch ⇒ +0x3c dllmain_crt_process_attach    CRT · _initterm 12 (syscall · win32k 없음)
           │              └─ (2) +0x8e  DllMain (0x89600)
           │                    ├─ (1) +0x29  AssertProcessIsNotFontdrvhost → +0x5d K32GetModuleBaseNameW
           │                    │                  └─ NtQueryInformationProcess(0) · NtReadVirtualMemory ×5   ── SYSCALL 0x19 · 0x3f [objdump]   (결과 안 봄)
           │                    ├─ (2) +0x2e  ReadDisableMetaFilesRegKey → +0x105 NtOpenKey(…\GRE_Initialize) · +0x153 NtQueryValueKey · +0x168 NtClose
           │                    ├─ (3) +0x35  NtGdiInit2            ── SYSCALL [win32u 0x12ee] — 이 스레드 첫 win32k syscall (`uiAccess` 없음 · 서버 아님일 때)
           │                    │        +0x44 gCookie ← RAX · 0 → +0x4a FALSE
           │                    │        (커널 안 — 이름만: GUI 변환 · win32kbase!GdiProcessCallout +0x22b PEB+0xf8 쓰기 · __ClientThreadSetup 콜백 [추측: 때])
           │                    ├─ (4) +0x5d  gdi32!LpkpInitializeEditControl(…, 0xe)          1 아니면 +0x65 FALSE
           │                    ├─ (5) +0x6c  GdiProcessSetup (0x81a00)                        음수 → +0x6e FALSE
           │                    │        +0x15 gbFirst == 0 → +0x1b JZ → +0x428 (둘째 호출)
           │                    │        첫 호출: 임계구역 ×6 · +0xfe GetACP · +0x142 gW32PID ← TEB+0x40 & ~3
           │                    │                 +0x161 [PEB+0xf8]+0x180094 ≠ 0 이면 +0x185 GdiInitializeLanguagePack · +0x18e gbFirst ← 0
           │                    │                 +0x1d8 ~ +0x3d3 RtlEncodePointer(0) ×25 · +0x3eb UspProcessAttach · +0x401 GetModuleHandleW · +0x41c GetProcAddress
           │                    │        공통 +0x428 ~ +0x45d  pGdiSharedMemory · pGdiSharedHandleTable ← PEB+0xf8 · pGdiDevCaps ← +0x180000
           │                    │        └─ +0x46b  user32!GetAppCompatFlags2(0x400) → (TEB+0x78 == 0) +0x4c NtUserGetThreadState(0xe)   ── SYSCALL [win32u 0x1000]
           │                    │                 +0x470 ~ +0x485 GdiBatchLimit ← 0x14 (플래그 0x200 꺼짐) / 0
           │                    ├─ (6) +0x75  InitializeCFontCache                             0 → +0x77 FALSE (늘 1)
           │                    └─ (7) +0x81 · +0x90  GetStockObject(0x12 · 0x13)  → [*pGdiSharedMemory + 0x180140 · +0x180148]   0 → +0x9e · +0xa3 FALSE
           └─ +0x5a GdiDllFullyInitialized ← 1 · +0x61 반환 EBX (0)

[GUI 변환 뒤 커널 콜백 — run85. 프로세스 첫 GUI 스레드면]
user32!ClientThreadSetup (0x4f810)
 └─ +0x9a  GdiProcessSetup (IAT 0xb4fd8) → gdi32!GdiProcessSetup (0xc400) +0x0 JMP [0x27b80]   (처음이면 지연 풀기 NtProtectVirtualMemory ×2)
           ⇒ gdi32full!GdiProcessSetup     — 이게 첫 호출이면 DllMain +0x6c 는 둘째 (+0x1b → +0x428)

[로더가 부르는 진입점 — 이유 1. 차례는 로더가 정함 (user32 → gdi32 → gdi32full → user32 순환)]
gdi32!Gdi32DllInitialize (0x5ed0)
 ├─ (1) +0x1b  DisableThreadLibraryCalls
 ├─ (2) +0x25  __security_init_cookie
 ├─ (3) +0x34  GetCurrentProcess
 ├─ (4) +0x52  GetProcessMitigationPolicy(자기, 0xb, &v, 4) → kernelbase +0x4e NtQueryInformationProcess(0x34)   ── SYSCALL 0x19 [objdump]
 │        +0x59 실패면 건너뜀 · +0x62 g_systemCallFilterId ← v & 0xf
 ├─ (5) +0x76  GetModuleHandleA("gdi32full.dll")      0 → +0x7e → +0xb9 GetModuleHandleA("gdi32min.dll") → 0 → +0xc3 FALSE
 ├─ (6) +0x91  GetProcAddress(…, "GdiDllInitializeWrapper")      0 → +0x99 JZ → +0xc3 FALSE
 └─ (7) +0xa0  (CFG 간접) → gdi32full 0x71030 = RET 0                 +0xa0 MOV EAX,1 (TRUE)
gdi32full!entry (0x89720)       이유 1 → +0x23 SUB EAX,2 (−1) · +0x28 CMP EAX,1 → +0x2a JNZ → +0x3f   아무것도 안 함 (1)
 ├─ (1) +0x31  _DllMainCRTStartup        이유 0 · 2 · 3 만
 └─ (2) +0x3f  UninitializeGdiHeap       이유 0 만
win32u — AddressOfEntryPoint 0 (진입점 없음)
```

### sast/82 "Q150b 로 넘길 것" 답 한눈에 (결과서)

| sast/82 가 넘긴 것 | 답 |
|---|---|
| ① `GdiDllInitialize` 안 `NtGdi*` syscall | **`NtGdiInit2` (`0x12ee`) 하나** (gdi32full `DllMain +0x2e`). `NtGdiInit` (`0x12ed`) 은 아님. 첫 win32k syscall (매니페스트 `uiAccess` 가 없어 user32 `+0x5d2` 를 안 타고 `gfServerProcess` 가 아닐 때) |
| ② `GdiSharedHandleTable` 을 어디서 받나 | gdi32full `GdiProcessSetup +0x15a` · `+0x173` · `+0x438` 이 PEB `+0xf8` 를 읽음. 쓰는 쪽은 `win32kbase!GdiProcessCallout +0x22b` |
| ③ 음수를 돌려주는 자리 | `0xc0000017` (`+0x32`, 힙) · `0xc0000001` (`+0x53`, `DllMain` FALSE) |
| ④ 두 번째 호출이 건너뛰는 것 | `GdiDllInitialize` 는 한 곳에서 한 번 (`GdiDllFullyInitialized`). 두 곳에서 불리는 건 `GdiProcessSetup` (`ClientThreadSetup +0x93` · `DllMain +0x67`) — `gbFirst` 로 큰 몫을 건너뜀 |
| ⑤ `GdiProcessSetup` (user32 가 gdi32 에서 가져오는 이름) | gdi32 껍데기 `JMP [0x27b80]` → 지연 가져오기 `ext-ms-win-gdi-internal-desktop-l1-1-0` → gdi32full `0x81a00` |

## 확인된 것

- `[정적]` gdi32 PE 진입점은 `Gdi32DllInitialize` (rva `0x5ed0`) 이고, 이유 1 에서 win32k syscall 을 안 부르며 PEB `+0xf8` 도 안 읽는다. FALSE 자리는 `+0x99` · `+0xc1 → +0xc3` 둘이다.
- `[정적]` gdi32full PE 진입점 `entry` (rva `0x89720`) 는 이유 1 에서 `_DllMainCRTStartup` 을 안 부르고 1 을 돌려준다 (`+0x23` · `+0x28` · `+0x2a`).
- `[정적]` gdi32 `GdiDllInitialize` 를 부르는 곳은 user32 `_UserClientDllInitialize +0x996` 하나다 (user32 refs `0xb4fe0` 1줄, gdi32 안에서 부르는 코드 없음 — refs `0xa6e0`). gdi32 `GdiDllInitialize` · `GdiProcessSetup` 은 지연 가져오기 칸으로 가는 6바이트 `JMP` 껍데기다.
- `[정적]` gdi32full `GdiDllInitialize` (rva `0x82070`) 는 이유 1 · 처음일 때만 `InitializeGdiHeap` → `_DllMainCRTStartup(hmod, 1, 0)` → (`dllmain_dispatch +0x89`) `DllMain` 을 탄다. 반환은 0 · `0xc0000017` · `0xc0000001` 셋뿐.
- `[정적]` gdi32full `DllMain` 에서 조건 없이 나가는 win32k syscall 은 **`+0x2e NtGdiInit2` (win32u `0x12ee`)** 하나다 (A13 `NtUserGetThreadState` `0x1000` 은 TEB `+0x78` 이 0 일 때만, ntdll syscall 은 A9 · A10 이 따로 있음). `NtGdiInit` (`0x12ed`) 가져오기 칸 `0xc1fe0` 을 읽는 건 gdi32full 의 내보내기 껍데기 `NtGdiInit+0x0` 뿐이다 (refs).
- `[정적]` gdi32full `DllMain` 의 FALSE 자리는 여섯 (`+0x4a` · `+0x65` · `+0x6e` · `+0x77` · `+0x9e` · `+0xa3`) 이다. 다섯은 `+0xaa XOR EAX,EAX` 로 모이고, `+0xa3` 은 `SETNZ BL` 로 0 을 만들어 `+0xa8 JMP` 로 나간다.
- `[정적]` gdi32full `GdiProcessSetup` 은 PEB `+0xf8` 를 읽어 그 값 `+0x180094` 를 보고 (`+0x161`, 첫 호출만), 그 값을 gdi32 `pGdiSharedMemory` · `pGdiSharedHandleTable` 에, `+0x180000` 을 `pGdiDevCaps` 에 넣는다 (`+0x428 ~ +0x45d`, 매번). `GetStockObject(0x12 · 0x13)` 은 `pGdiSharedMemory` 값 `+0x180140` · `+0x180148` 을 읽는다 (`+0x65 ~ +0x72`).
- `[정적]` `GdiProcessSetup` 은 user32 `ClientThreadSetup +0x93` 과 gdi32full `DllMain +0x67` 두 곳에서 불리고, `gbFirst` (파일 값 1, `+0x18e` 에서 0) 로 둘째는 `+0x1b JZ → +0x428` 로 간다.
- `[정적]` user32 `GetAppCompatFlags2` 는 TEB `+0x78` 이 0 이면 `+0x45 NtUserGetThreadState(0xe)` 를 부르고, 아니면 TEB `+0x810` · `+0x818` 을 읽는다.
- `[정적: PE 표]` gdi32 는 `win32u.dll` · `api-ms-win-gdi-internal-uap-l1-1-0.dll` 을, gdi32full 은 `GDI32.dll` · `USER32.dll` · `win32u.dll` 을 정적으로 가져온다 (flow-writer `objdump -p`). 그래서 gdi32full 은 gdi32 와 같이 올라오고, 셋은 user32 → gdi32 → gdi32full → user32 로 돈다.
- `[objdump + PDB 공개 이름]` PEB `+0xf8` 를 쓰는 코드는 유저 DLL 다섯에서 못 찾았고 `win32kbase!GdiProcessCallout +0x22b` 에 있다 (결과서 — 꼴 검색이라 하한).
- `[관찰]` test.exe 사진의 PEB `GdiSharedHandleTable` 은 (null) 이다 (`ms/logs/04_baggage-read_P1a.txt:1779`, `_P2.txt:2489`) — gdi32 를 안 올린 것과 맞다.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **`GetAppCompatFlags2` call = `GdiProcessSetup +0x464`** (ret `+0x46b`). 결과서 표 A13 은 `+0x464/+0x46b` 로 맞고, 질문 2 본문 한 곳의 `+0x460/+0x46b` 는 인자 싣는 줄 (`+0x460 MOV CX,0x400`) 이다 (flow-writer `.asm`).
- ② **gdi32full `DllMain` 을 부르는 줄은 `dllmain_dispatch +0x89/+0x8e`** 다. 결과서 문장 "… `dllmain_crt_dispatch` (…) → `+0x89/+0x8e` `DllMain`" 의 `+0x89` 는 `dllmain_dispatch` 안 오프셋이다 (`+0x6e` 도 같은 함수). 뜻은 같고 주인만 붙였다.
- ③ gdi32 `+0xc1 JNZ` 의 목적지는 `+0x80` (gdi32min 을 찾으면 같은 `GetProcAddress` 줄로 돌아감). 결과서 표와 뜻이 같다.
- ④ **`GdiProcessSetup` 의 `RtlEncodePointer(0)` ×25 는 syscall 이 없다** `[정적: run59 줄 + 차례]` — run59 의 `RtlEncodePointer +0x43 NtQueryInformationProcess(0x24)` 는 쿠키 캐시가 0 일 때만이고, 캐시는 `LdrpInitializeProcess +0x12f` 가 이미 채웠다.
- ⑤ **A8 은 두 조건이 겹쳐야 syscall** — gdi32full `__security_init_cookie` 는 `__security_cookie` 가 파일 기본값 `0x2b992ddfa232` 일 때만 (`+0x1e CMP` · `+0x21 JNZ → +0x9c`) `+0x5b QueryPerformanceCounter` 를 부른다 (flow-writer `.asm`). 그리고 run44 KUSD `QpcBypassEnabled` = `0x83` `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:2985`) — 결과서 조건 (`꺼졌을 때만`) 대로면 이 VM 에선 `NtQueryPerformanceCounter` 가 안 나간다 `[추측: 조건은 결과서 추측]`. 로더가 DLL `/GS` 쿠키를 먼저 바꿔 두는지는 안 봄.
- ⑥ **test.exe 사진**: PEB `GdiSharedHandleTable` (null) (`_P1a.txt:1779` · `_P2.txt:2489`) `[관찰]` — 짐 지도 7.2 "끝까지 0" 과 같다.
- ⑦ **TEB 0 채움일 때 `GdiBatchLimit`** — `GetAppCompatFlags2` 는 TEB `+0x78 ≠ 0` 이면 `+0x22 CMP EBX(0x400),[TEB+0x810]` 을 보고, `0x400` 이 더 작으면 (`+0x28 JC`) 0, 아니면 `+0x33` 의 `[TEB+0x818]` 을 돌려준다. 0 채움이면 0 → `GdiProcessSetup +0x470 ~ +0x485` 에서 `GdiBatchLimit = 0x14` (flow-writer `.asm`).
- ⑧ **gdi32 · gdi32full 이 TEB 를 건드리는 줄** (gdi32full 뽑은 `.asm` 79 + gdi32 여섯 안 `GS:` 검색, flow-writer): `GdiProcessSetup +0xa` (TEB 읽기 → `+0x142` `ClientId.UniqueProcess`) · `+0x151` · `+0x16a` · `+0x428` (PEB) · TLS 포인터 `GS:[0x58]` 셋 (`gCallerTLStorage` 초기화 · 정리, `__tlregdtor`) · `__scrt_acquire_startup_lock +0xd`. **TEB `Gdi*` 칸 쓰기는 이 줄기에 없다** — 짐 지도 3.3 의 "gdi32 6 (`Gdi*`)" 짐작은 그대로 `[추측]` (쓰는 때는 그리기 쪽일 것).
- ⑨ run85 보탠 것 ⑥ "gdi32 는 user32 `DllMain` 전에 이미 올라와 있다 `[추측: 의존하는 쪽이 먼저]`" — **올라오는 것 (매핑) 은 맞다** (위 PE 표). 다만 `DllMain` **차례**는 순환이라 "의존하는 쪽이 먼저" 로 정할 수 없고 로더가 실행 때 정한다 (동적 Q162 1). 차례가 달라도 이번에 읽은 줄 안에선 진입점까지 가는 데 걸릴 곳이 안 보인다 — gdi32full 진입점은 이유 1 에서 아무것도 안 하고, gdi32 진입점은 gdi32full 이 매핑만 돼 있으면 된다 `[추측: 읽은 줄 안에서]`.
- ⑩ **결과서 표 A 의 A15 차례** — 결과서는 `0x157b` (`NtUserSetProcessUIAccessZorder`) 를 `+0x9b8` 뒤 조건부 다섯에 묶었지만, 자리는 `_UserClientDllInitialize +0x5d2/+0x5d9` (run85 차례 15) 라 `+0x996 GdiDllInitialize` (A6) 보다 **앞**이다 (오케스트레이터 `.asm` 확인, flow-writer `.asm` `+0x590 ~ +0x5d9`). 그래서 A5″ 로 옮겼고, "`NtGdiInit2` = 첫 win32k syscall" 에는 "매니페스트 `uiAccess` 가 없어 `+0x5d2` 를 안 타고 `gfServerProcess` 가 아닐 때" 를 붙였다. 최소 조건 결론 (매니페스트 없으면 안 나감) 은 그대로.
- ⑪ 결과서 "먼저 읽을 것" 3 "gdi32full `DllMain` 의 syscall 은 `NtGdiInit2` 하나뿐" 은 **"무조건 나가는 win32k syscall 하나"** 로 읽어야 맞다 — 같은 `DllMain` 줄기에 ntdll syscall 도 있고 (A9 `NtQueryInformationProcess` · `NtReadVirtualMemory` ×5, A10 레지스트리 셋), A13 `NtUserGetThreadState` (`0x1000`) 는 조건부 win32k syscall 이다.
- ⑫ 표 B 의 "gdi32full 전역" 줄에 `ghbrDCBrush` · `ghbrDCPen` 을 더했다 (`DllMain +0x84` · `+0x97` 가 쓰고 `+0x90` 이 읽음 — `+0x9e` FALSE 갈래). 결과서 칸 (`gbInitialized` 포함) 과 "gdi32 데이터 칸" 줄의 `fpClosePrinter` · 가져오기 칸 `0xc1050` 은 그대로 둔다.
- `_calls.tsv` 의 `alias:` callee (`InitializeCFontCache` 의 `operator_new+1` 등) 는 이번 표에 이름으로 안 썼다. 이름을 쓴 함수는 `pdb_aliases.py --lookup` 로 별칭 무리가 아님을 확인했다 (gdi32full `0x71030` 만 9개 — 위 표).

## 정정

- **이전 run 이 틀린 것은 없다.** run85 는 아래 짐작을 옮기지 않고 "run86 에서 정정" 표시만 두었다 → 그 자리 (run85 win32u 번호표 `0x12ed · 0x12ee` 줄, run85 "정정" 절 두 줄) 에 정정 결과를 한 줄씩 이었다.
- **결과서 sast/82 의 Q150b 쪽 짐작 (run85 가 옮기지 않은 것)**:
  - "`GdiDllInitialize` 가 gdi32 `DllMain` 과 user32 `DllMain` 에서 두 번 (다른 `hmod`) 불린다" (sast/82 "Q150b 로 넘길 것" ④) — **틀림.** 부르는 곳은 user32 `+0x996` 하나다 (refs `0xb4fe0` 1줄). gdi32 `DllMain` 은 `GdiDllInitializeWrapper` (= `RET 0`) 를 부른다. **두 번 불리는 건 `GdiProcessSetup`** (`ClientThreadSetup +0x93` · gdi32full `DllMain +0x67`, `gbFirst` 로 둘째는 `+0x1b → +0x428`). `GdiDllInitialize` 자체도 `GdiDllFullyInitialized` 로 두 번째면 0.
  - "`NtGdiInit` (`0x12ed`) 이 gdi32 쪽 첫 win32k syscall 후보" (sast/82 질문 3 · win32u 번호표) — **틀림.** 실제는 **`NtGdiInit2` (`0x12ee`)**, gdi32full `DllMain +0x2e` (가져오기 칸 `0xc1fe8`). `NtGdiInit` 칸 `0xc1fe0` 은 내보내기 껍데기만 읽는다.
  - (flow-writer 더함) "gdi32 → gdi32full 지연 가져오기라 `+0x996` 에서 처음 로드될 수 있음" (sast/82 동적 질문, run85 는 동적 6 의 질문으로만 옮김) — **맞지 않음.** gdi32 가 `api-ms-win-gdi-internal-uap-l1-1-0` 을 정적으로 가져와 gdi32full 은 gdi32 와 같이 올라온다 `[정적: PE 표]`. 지연 가져오기는 `GdiDllInitialize` · `GdiProcessSetup` 두 이름의 **주소를 처음 풀 때** 몫이다 (실행 확인은 Q162 1 · 7).

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 늘리지 않음 — test.exe 는 user32 · gdi32 를 안 올린다):
  - 기준: gdi32 · gdi32full · win32kbase pdb 줄 새로, user32 · win32u · ntdll · kernelbase pdb 줄에 run86 몫, 근거 로그 run86 줄.
  - 2.4 머리 run 목록 · run63 CFG 절에 한 줄 (`ProtectDelayLoad 0x8000` 을 읽는 쪽 `LdrResolveDelayLoadedAPI +0x17f` `[objdump]`).
  - 3장 user32 절: 머리에 gdi32 쪽 이름, 본문 두 곳 (run86 가리킴 · "첫 후보" 답), **새 하위 절 "gdi32 · gdi32full 시작 함수와 최소 조건"**.
  - 4장: run86 새 줄 셋 (동적 Q162 · 정적 Q163 · PE_emulator · 보탠 것 · 정정), run85 줄 셋에 run86 결과 (동적 5 · 6 좁힘, 정적 ⑦ 일부 해결 · ① 같은 결정, 정정 표시 결과).
  - 5장: run 표 · 03_sprint 요청서 번호 run86 줄.
- 짐 지도 (짐 지도 바퀴 — **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**): 기준 run86 줄 · 판 목록 (마흔둘째판) · 3.3 TEB "범위 밖 길 — 다른 DLL 39" 근거 칸에 (run86) 메모 (gdi32 `Gdi*` 짐작 그대로 · `Win32ThreadInfo` 읽는 줄) · 7.2 `_PEB` `끝까지 0` 에 `GdiSharedHandleTable` 읽는 줄 · 쓰는 곳 메모 · 8장 두 표.

## 아직 확인 필요

동적 — 결과서 "동적분석으로 확인해야 할 것" 번호 그대로, 모두 대기열 **Q162** (run85 에서 만든 행 — user32 · gdi32 를 올리는 프로세스. 무엇으로 볼지 (notepad 류 / 새 GUI 샘플) 는 사용자 결정). **test.exe 로 볼 수 있는 것은 없다** — test.exe 는 user32 · gdi32 를 안 올린다 `[소스]` (`sample/log.h:3`) · `[관찰]` (사진 모듈 6개, PEB `+0xf8` null). 그래서 Q104 에 붙일 것은 없다.

- 1 `DllMain` 차례: `gdi32full!entry` · `gdi32!Gdi32DllInitialize` · `user32!UserClientDllInitialize` 입구와 `ntdll!LdrpCallInitRoutine`. `gdi32full!entry` 가 이유 1 에서 `+0x2a` 로 건너뛰나 (`rax = 1`, `_DllMainCRTStartup` 안 불림).
- 2 `gdi32full!DllMain+0x2e` 입구 · `+0x35` `rax` (cookie), 그 앞뒤 `dq @$peb+0xf8 L1`, 그 사이 `user32!ClientThreadSetup` 이 서나 (`kb`). 커널 `ba w8 @$peb+0xf8` (그 프로세스 조건) → `win32kbase!GdiProcessCallout+0x22b` 인가.
- 3 GDI 공유 메모리: `dd poi(@$peb+f8)+180094 L1` · `dq poi(@$peb+f8)+1800b0 L16` · `!address poi(@$peb+f8)` (크기), `+0x180140` · `+0x180148` 이 0 아닌가.
- 4 `gdi32full!GdiProcessSetup` 첫 호출자가 `user32!ClientThreadSetup+0x9a` 인가 `gdi32full!DllMain+0x6c` 인가, 둘째가 `+0x1b → +0x428` 인가.
- 5 `user32!GetAppCompatFlags2+0x12` 때 TEB `+0x78` 이 0 인가 → `NtUserGetThreadState` (`0x1000`) 가 불리나.
- 6 `kernelbase!K32GetModuleBaseNameW` 안 `NtReadVirtualMemory` 가 5번인가, 이름이 exe 이름인가.
- 7 `ntdll!LdrpWriteBackProtectedDelayLoad+0x89` · `+0xf1` 가 `GdiDllInitialize` · `GdiProcessSetup` 에서 서나, 칸 `gdi32+0x280e0` · `+0x27b80` 이 풀린 뒤 gdi32full 주소인가.
- 8 `NtQueryInformationProcess` 클래스 `0x34` 의 실제 답 (`gdi32!Gdi32DllInitialize+0x52`, `kernelbase!GetProcessMitigationPolicy+0x4e`).
- 9 `GRE_Initialize\DisableMetaFiles` 가 VM 에 있나 (이것만은 VM 에서 `reg query` 로 프로세스 없이도 볼 수 있다).
- 10 `+0x180094` 가 켜졌나 → `GdiInitializeLanguagePack` · `user32!InitializeLpkHooks` 가 도나.
- 11 `gdi32!Gdi32DllInitialize+0x9b` CFG 간접 호출 대상 (`gdi32full+0x71030`) 이 검사를 통과하나, `GetProcAddress` 가 내보내기 억제를 푸는 `NtSetInformationVirtualMemory` 가 따로 보이나 `[추측]`.
- 12 A6 ~ A13 syscall 순서를 그 프로세스 조건 syscall 추적으로 한 번에.
- 추천 멈춤점 (결과서 순서): `gdi32full!DllMain+0x2e` / `+0x35`, `gdi32full!GdiProcessSetup` 입구, `user32!ClientThreadSetup` 입구, `nt!PsConvertToGuiThread` (그 프로세스 조건), `win32kbase!GdiProcessCallout+0x22b`.
- **[엔진 쪽 — PE_emulator]** 지금 `NtGdiInit2` (`0x12ee`) 에 무엇을 돌려주나, PEB `+0xf8` 를 채우나 `[공백]`.

정적 — 다음 바퀴 후보, 모두 대기열 **Q163**:

- ① `win32kbase!GdiProcessCallout` (rva `0xe2010`) 와 GDI 공유 메모리를 채우는 곳 (`+0x180094` · `+0x1800b0` 표 · `+0x180000`).
- ② win32kfull 의 `NtGdiInit2` 쪽 — 반환값 · GUI 변환 순서 · `__ClientThreadSetup` 콜백 때.
  - ① ② 는 원문 "win32k 커널 안쪽 깊이 — 이번에 넣지 않는 것" 의 예외라 **PE_emulator · 사용자 결정** (Q161 ① 과 같은 결정).
- ③ ntdll 지연 가져오기 길 `LdrResolveDelayLoadedAPI` → `LdrpHandleProtectedDelayload` → `LdrpWriteBackProtectedDelayLoad` · `LdrpLoadForwardedDll` · `LdrpResolveProcedureAddress` 를 Ghidra `.asm` 으로 (이번엔 `objdump` 만 — ntdll 프로젝트가 쓰이는 중이었음). flow.md 2.4 run63 의 `0x8000` 과 run82 의 "첫 UEF 에서 delay-load" 와 같이 닫힌다.
- ④ 창 만들기 (`RegisterClassExW` · `CreateWindowExW` · 메시지 루프가 읽는 `gSharedInfo` 칸 — Q161 ③ ④ 와 겹침), ⑤ GDI 그리기 (공유 메모리 핸들 표 항목 · `+0x180000` · TEB `GdiTebBatch` · 첫 `NtGdi*`) — ④ ⑤ 는 원문 "이번에 넣지 않는 것" 이라 PE_emulator 가 새 요청으로 낼 때.
- ⑥ `LpkInitialize` · user32 `InitializeLpkHooks` (언어팩 플래그 `+0x180094` 켜짐 갈래 — Q161 ⑤ 와 같이).
- ⑦ `msvcp_win.dll` 시작 함수 (gdi32full 이 정적으로 가져옴).
- ⑧ (flow-writer 더함) 작은 것: `HeapCreate(0,0,0)` → `RtlCreateHeap` 의 syscall 횟수 (A7), kernelbase `K32GetModuleBaseNameW` 여섯 자리 짝 · `GetProcAddress` 의 CFG 내보내기 억제 갈래 (`NtSetInformationVirtualMemory` `[추측]`), 로더가 DLL `/GS` 쿠키를 먼저 바꿔 두나 (A8 — 바꿔 두면 QPC 도 안 부름).

짐 지도 결정 (오케스트레이터 · 사용자): PEB `GdiSharedHandleTable` (`+0xf8`) 을 "범위 밖 길 — 다른 모듈 (win32k)" 로 셀지 — run85 의 PEB user32 칸 넷과 같은 결정 (2장 PEB 모름 68 그대로 둠).

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음. 새 함정 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 gdi32 · gdi32full 줄기에서 디버거 여부 (`BeingDebugged` · `KdDebuggerEnabled`) 를 보는 줄은 없었다 `[정적: 읽은 줄 안에서]`. KUSD `QpcBypassEnabled` (A8) 는 디버거와 무관한 값이다 `[추측]`.
- 도구 (결과서): gdi32 · gdi32full PDB 에 타입이 없어 공유 메모리 칸 이름을 못 붙인다. Ghidra 는 gdi32 진입점을 PDB 이름 `Gdi32DllInitialize` 로, gdi32full 진입점을 `entry` 로 잡는다. PEB `+0xf8` 쓰는 곳 찾기는 `objdump` 꼴 검색 (유저 DLL: `gs:0x60` 뒤 8줄 안 `[reg+0xf8]`, win32kbase: `PsGetProcessPeb` 뒤 150줄 안 쓰기) 이라 하한이다. kernelbase · ntdll · win32kbase 줄은 `objdump` 라 ret 은 call + 5 / + 7.
- 도구 (flow-writer): 이 서버의 시스템 파이썬엔 `pefile` 이 없다 → PE 진입점 · 가져오기는 `objdump -p` 로 봤다 (지연 가져오기 표는 이 `objdump` 가 안 보여 결과서 값 그대로).

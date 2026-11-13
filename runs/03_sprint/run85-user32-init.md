# 2026-11-13 run85 — user32 시작 함수 (`DllMain`) 의 최소 길, user32 · win32u 쪽 (Q150a, PE_emulator 요청 28 첫 조각): **user32 `DllMain` 이 보통 프로세스에서 꼭 받아야 하는 바깥 답은 둘뿐이다 — `NtQuerySystemInformation(0)` 성공, 그리고 csrss 의 USERSRV 연결 답. 연결은 ntdll `CsrClientConnectToServer(…, 3, 0x248B 버퍼, …)` 이고, kernelbase 가 이미 연결해 두었으면 (run68) `NtAlpcSendWaitReceivePort` 한 번으로 끝난다. 그 답 0x248B 의 `+8` 부터 0x240B 가 `gSharedInfo`, 첫 8B (`psi`) 가 `gpsi` 이고, `psi` 는 읽을 수 있는 메모리 0x1b68B 이상이면 된다 (0 으로 채워도 길 끝까지 감). `NtOpenSection(\Sessions\N\Windows\SharedSection)` 은 user32 가 아니라 ntdll `CsrpConnectToServer +0x1b6` 에 있고 `CsrPortHandle == 0` (이 프로세스의 첫 csrss 연결) 일 때만 탄다 — 그러니 PE_emulator 가 user32 `DllMain` 에서 이 호출을 봤다면 그때 `CsrPortHandle` 이 0 이었다. `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 `DllMain` 에 없고 csrss 같은 서버 프로세스 갈래 (`ClientThreadSetup`) 에만 있다. `DllMain` 본체의 win32k syscall 다섯은 모두 조건부라 `psi` 0 · 매니페스트 · IFEO 없음이면 하나도 안 나간다. GUI 스레드 변환은 그 스레드의 첫 win32k syscall 때 커널이 하고, user32 `DllMain` 안에서 무조건 부르는 첫 후보는 `GdiDllInitialize` (`+0x996`) 쪽 — gdi32 · 최소 조건 표 둘은 run86 (Q150b).** user32 `UserClientDllInitialize +0x2c` ⇒ `_UserClientDllInitialize +0x1d1 NtQuerySystemInformation` · `+0x2aa CsrClientConnectToServer` → ntdll `CsrClientConnectToServer +0xab` (`CsrPortHandle ≠ 0`) → `+0xb5 CsrpClientConnectToServer` → `+0x7f CsrClientCallServer` → `+0xed NtAlpcSendWaitReceivePort` · `CsrpConnectToServer +0x1b6 NtOpenSection` (첫 연결만) · `_UserClientDllInitialize +0x996 GdiDllInitialize`

## PE_emulator 쪽에서 쓸 것

### 요청 28 user32 쪽 결론 (run85)

- **user32 `DllMain` 이 꼭 받아야 하는 바깥 답은 둘** `[정적]`.
  - ① `NtQuerySystemInformation(SystemBasicInformation = 0, 버퍼, 0x40, NULL)` 성공 (`_UserClientDllInitialize +0x1d1/+0x1d8`). 실패면 `+0x1df JS` → FALSE. 버퍼 `+0x28` (`MaximumUserModeAddress`) 가 user32 `gHighestUserAddress` 가 된다 (`+0x1e5 ~ +0x1ec`).
  - ② csrss USERSRV 연결 (`+0x2aa/+0x2b1`). 음수면 `+0x2b8 JS` → FALSE.
  - 나머지 syscall (레지스트리 읽기 · `.mrdata` 열고 닫기 · ETW 등록 · 조건부 win32k) 은 실패해도 FALSE 가 아니다. 다만 ntdll 코드 `RtlInitializeNtUserPfn` 은 **프로세스당 한 번만** 성공한다 (아래 표 4번).
- **csrss 연결 = 둘째 번 연결** `[정적]` (첫째 번은 run68 의 kernelbase BASESRV 연결).
  - 인자: 객체 디렉터리 (`\Sessions\1\Windows` 또는 `\Windows`) · 서버 번호 **3** (USERSRV) · 0 으로 채운 **0x248B** 버퍼 · 길이 `0x248` (값) · `&gfServerProcess`.
  - kernelbase 가 이미 연결했으면 (`CsrPortHandle ≠ 0`) ntdll `CsrClientConnectToServer +0xab JZ` 를 안 타고 **`+0xb5/+0xba CsrpClientConnectToServer`** 로 간다. syscall 은 `CsrClientCallServer +0xed/+0xf2` **`NtAlpcSendWaitReceivePort` (ntdll 번호 `0x8e`) 한 번**.
  - 엔진이 줄 답: ① syscall 성공 ② 메시지 `+0x34` (상태) = 0 ③ 캡처 버퍼 0x248B 의 `+8` 부터 0x240B 에 `SHAREDINFO` 모양 `[이름 추측]` — 첫 8B 가 `psi`.
  - `psi` 는 널이면 안 된다. 널이면 FALSE 가 아니라 `+0x38e TEST byte ptr [RAX],4` 에서 **접근 위반** 으로 터진다. 0 으로 채운 읽기 가능 `0x1b68`B 면 길 끝까지 간다 (읽는 칸은 `+0x0` · `+0x8bc` · 조건부 `+0x1b66` — 아래 데이터 칸 표).
  - 메시지 안 포인터는 보낼 때 `CsrPortMemoryRemoteDelta` 를 더해 csrss 쪽 주소로 바뀐다 (`CsrClientCallServer +0x81 ~ +0xb9`, 돌아오면 `+0xfa ~ +0x12a` 로 되돌림 — flow-writer `.asm`). 엔진이 캡처 버퍼에 답을 쓸 땐 이 delta 를 빼서 이 프로세스 주소로 쓴다 `[추측: 엔진 쪽 — delta 가 0 이면 그대로]`.
- **`NtOpenSection(SharedSection)` 은 정상 길이 아니다** `[정적]` — user32 `.asm` 어디에도 없다. ntdll `CsrpConnectToServer +0x1b6/+0x1bb` (첫 연결 길) 에만 있고, 그 길은 `CsrPortHandle == 0` 일 때만 탄다 (`CsrClientConnectToServer +0xa4 CMP` · `+0xab JZ → +0xfe` · call `+0x113`). 아래 "PE_emulator 가 본 `0xc000003a`" 절.
- **`NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 `DllMain` 에 없다** `[정적]`. user32 전체에서 두 IAT 칸을 읽는 곳은 `ClientThreadSetup` 하나 (`_refs/0xb63f8.tsv` · `0xb63f0.tsv`), 그것도 `gfServerProcess ≠ 0` (csrss 같은 native 이미지) 일 때만. 보통 프로세스의 `gSharedInfo` 는 위 csrss 답에서만 온다.
- **user32 `DllMain` 본체의 win32k syscall 다섯은 모두 조건부** `[정적]` — `psi` 0 채움 · 매니페스트 없음 · IFEO 없음 · TEB `Win32ClientInfo` 0 이면 하나도 안 부른다 (디컴파일 읽기, 실제 갈래는 동적).
- **GUI 스레드로 바뀌는 때 = 그 스레드의 첫 win32k syscall (번호 `0x1000` 이상)** `[정적, 커널은 이름만]`. user32 코드는 TEB `Win32ThreadInfo` (`+0x78`) 를 안 쓴다. 변환 뒤 커널이 user32 `__ClientThreadSetup` 을 콜백으로 부른다 — 엔진이 "첫 win32k syscall → `KernelCallbackTable[84]` 콜백" 을 못 하면 `ClientThreadSetup` 이 안 돈다 `[추측: 엔진 쪽]`. user32 `DllMain` 안에서 무조건 부르는 첫 후보는 `+0x996 GdiDllInitialize` 쪽 → **run86** (Q150b).
- **win32u 이름표**: 번호는 스텁 `MOV EAX, imm32` 그대로 (`0x1000 ~ 0x15da`, 1499개). 전체 표 `ms/disassemble/win32u/_syscalls.tsv` (열: 번호 16진 · 10진 · 이름 · rva · 크기 · 같은 주소 export 별칭). ntdll `Nt*` 이름표와 같은 방법 (스텁 바이트 `4C 8B D1 B8 <imm32>`) 으로 만든다.

### 원문 질문이 어디서 답해졌나

| 원문 요청 28 | 답한 곳 |
|---|---|
| 1 user32 `DllMain` 이 하는 일 · syscall 차례 · 답 · FALSE 자리 | **run85** (아래 차례 표 · FALSE 11 자리) |
| 2 공유 섹션 · `gSharedInfo` · `gpsi` 최소 칸 | **run85** (아래 데이터 칸 표). 창 API 가 읽는 칸은 "아직 확인 필요" 정적 ③ |
| 3 GUI 스레드로 바뀌는 때 | **run85** (첫 win32k syscall 때 커널. user32 `DllMain` 안 첫 후보는 gdi32 쪽 → run86) |
| (+ "지금 우리" 의 win32u 이름표 — 요청서 sast/82 질문 4) | **run85** (아래 이번 길 표 + 전체 `ms/disassemble/win32u/_syscalls.tsv`) |
| 4 gdi32 · gdi32full `DllMain` | **run86** (Q150b, sast/83) |
| 5 창 안 만드는 프로세스의 최소 조건 표 둘 (syscall 답 · 데이터 칸) | **run86** (Q150b, sast/83). 재료는 이 문서의 차례 표 · 데이터 칸 표 · FALSE 자리 |

### 질문마다 (결과서 sast/82 그대로, 근거 줄은 오케스트레이터 · flow-writer 가 `.asm` 으로 다시 봄)

| 질문 (원문 번호) | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1 진입 · 반환 | PE 진입점 = `UserClientDllInitialize` (rva `0x51120`, 49B). 이유 1 이면 `+0x18/+0x1d _DllMainCRTStartupForGS2` (`/GS` 쿠키) → `+0x2c` 꼬리 `JMP` → `_UserClientDllInitialize` (rva `0x6ba84`, 2745B) 의 `EAX` 가 그대로 반환. `+0xb5/+0xbc DisableThreadLibraryCalls` 라 스레드 attach/detach 는 안 옴 | — | `UserClientDllInitialize +0x14 · +0x18 · +0x2c` (flow-writer 전부 읽음), PE `AddressOfEntryPoint 0x51120` (flow-writer `pefile`) |
| 1(a) `CsrClientConnectToServer` 인자 · run68 과 다른 점 | 서버 3 · 0x248B (전부 0) · `0x248` · `&gfServerProcess`. run68 과 다른 점 둘: ① 이미 연결이라 `CsrpConnectToServer` (공유 섹션 열기 · `NtConnectPort` · 뷰 붙이기) 를 다시 안 탐 → `CsrpClientConnectToServer` 한 길 ② 연결 정보 8B → 0x248B | 이미 연결이면 엔진은 `NtAlpcSendWaitReceivePort` 하나만 대답하면 된다 | `_UserClientDllInitialize +0x2aa` (`RCX` 디렉터리 · `EDX 3` · `R8 RSP+0x60` · `R9D 0x248` · `[RSP+0x20]` `&gfServerProcess` — 결과서 `.asm`, Ghidra 의 import 서명 4개는 틀림), ntdll `CsrClientConnectToServer +0xa4 · +0xab · +0xb5/+0xba · +0x113/+0x118` |
| 1(b) `NtOpenSection(SharedSection)` 자리 | user32 에 없음. ntdll `CsrpConnectToServer +0x1b6/+0x1bb` (접근 4), 같은 함수 `+0x338/+0x33d NtMapViewOfSection` (읽기 전용 — run68 표) — `CsrPortHandle == 0` 일 때만 | **틀림 (바로잡을 점)** — 정상 길이 아님. 그때 `CsrPortHandle` 이 0 이었다 (아래 절) | `CsrpConnectToServer +0x1b6` · `+0x338` (flow-writer 다시 봄) |
| 1(c) win32u syscall | `DllMain` 본체는 다섯 모두 조건부 (아래 번호표 "이번 길에서" 칸). `NtUserProcessConnect` (`0x10e2`) · `NtUserInitializeClientPfnArrays` (`0x14aa`) 는 `ClientThreadSetup` 서버 갈래뿐 | 이름표가 없던 `0x1000` 대 번호는 위 전체 표로 붙일 수 있다 | `_UserClientDllInitialize +0x5d2 · +0xa17 · +0xa80`, `SetProcessDpiAwarenessContext +0x3a`, `GetDpiForCurrentProcess +0x28`, `ClientThreadSetup +0x120 · +0x228` |
| 1(d) FALSE 자리 | 11곳, 모두 목적지 `+0x9b1 XOR EAX,EAX` (아래 표) | `0xc0000142` (`STATUS_DLL_INIT_FAILED`) 는 이 11곳 중 하나와 이어진다 — `NtOpenSection` 갈래면 6번 (`+0x2b8`) | `+0x9b1` = `0x18006c435` (flow-writer 다시 봄) |
| 2 공유 데이터 최소 칸 | csrss 답 `+8` 부터 0x240B → `gSharedInfo` (rva `0xda350`), 첫 8B → `gpsi` (rva `0xda590`). 공유 섹션 뷰의 오프셋으로 받는 게 **아니다**. `DllMain` 이 읽는 `psi` 칸은 `+0x0` (비트 4) · `+0x8bc` (비트 `0x20`) · 조건부 `+0x1b66` (word) | `SharedSection` 만 채우는 쪽이면 모자라 보인다 — 필요한 건 `NtAlpcSendWaitReceivePort` 답 안의 `psi` 포인터 + 그 아래 읽기 가능 `0x1b68`B (`SharedSection` 뷰와 `psi` 가 같은 메모리인지는 정적으로 못 정함 `[추측]`) | `+0x30b ~ +0x37c` 복사 · `+0x380/+0x387` · `+0x38e`, `InitProcessDpiAwareness +0xa8`, `GetDpiForCurrentProcess +0x13` |
| 3 GUI 스레드로 바뀌는 때 | 그 스레드의 첫 win32k syscall 때 커널 (`KiConvertToGuiThread +0x4 PsConvertToGuiThread`, 이름만). user32 코드엔 TEB `+0x78` 쓰기 없음 → 쓰는 쪽은 win32k `[추측]`. 변환 뒤 `__ClientThreadSetup` 콜백 (`PEB+0x58` 표의 84번째 칸) | 정보 없음 — PE_emulator 에 GUI 스레드 흉내가 있는지 모름 | `ms/disassemble/ntoskrnl/KiConvertToGuiThread.asm` `+0x4`, `_refs/__ClientThreadSetup.tsv` (`apfnDispatch +0x2a0`), `__ClientThreadSetup +0x4 · +0x20` |
| (sast/82 질문 4) win32u 번호표 | 1499개, 중복 없음, Ghidra `.asm` 과 12/12 같음 | **맞음** — 우리 짐작대로 `0x1000` 대 | `ms/disassemble/win32u/_syscalls.tsv` |

### user32 `DllMain` 차례 표 (이유 1, 보통 프로세스: `gfServerProcess == 0`, PEB `+3` 보호 비트 0)

`call/ret` 는 `_UserClientDllInitialize` 안 오프셋 (`.asm` 의 `CALL … ret=` 줄). syscall 번호는 `[ntdll]` / `[win32u]` 로 나눈다.

| 차례 | 자리 call/ret | 부르는 것 | syscall (번호) | 다음으로 가려면 | 실패면 |
|---|---|---|---|---|---|
| 0 | `+0x9f` · `+0xae` | (코드) PEB `+0x2c0` (`SessionId`) 읽기, PEB `+0x7d0` (`Win32ClientProcInfo`) ← 0 | — | | |
| 1 | `+0xc8 ~ +0x185` | `RtlInitializeCriticalSection` ×10 (`gcsClipboard` 등) | 없음 | 상태 OR 가 음수 아님 | `+0x193 JL` → FALSE |
| 2 | `+0x1a0/+0x1a5` · `+0x1b4/+0x1b9` | `InitializeUMHandleList` ×2 (rva `0x53c9c`) | 없음 | 음수 아님 | `+0x1a7` / `+0x1bb JS` → FALSE |
| 3 | `+0x1d1/+0x1d8` | `NtQuerySystemInformation(0, 버퍼, 0x40, NULL)` | `[ntdll] 0x36` | 성공 | `+0x1df JS` → FALSE |
| 4 | `+0x1f3/+0x1f8` | `InitializeNtdllUserPfn` (rva `0x6b924`) → ntdll `RtlInitializeNtUserPfn` (`+0x3f/+0x46`) · `RtlRetrieveNtUserPfn` (`+0x62/+0x69`) | 직접 없음. 안에서 `LdrProtectMrdata(0)` · `(1)` 가 `.mrdata` 를 열고 닫음 (중첩 카운터 0 ↔ 1 일 때 `NtProtectVirtualMemory 0x50`, run60) | 1 반환. 표 셋 (`0xc0` · `0xc0` · `0x90`B) 을 ntdll `NtUserPfn` 으로 복사 — **프로세스당 한 번** (플래그 `0x1e7340`, 두 번째는 `0xc000000d`) | `+0x1fa JZ` → FALSE |
| 5 | `+0x248/+0x24f` | `WTSGetServiceSessionId` (kernelbase → ntdll `RtlGetCurrentServiceSessionId` 로 `JMP`) | 없음 | 값 = PEB `+0x90` (`SharedData`) 첫 dword, 널이면 0 | |
| 6 | `+0x282/+0x287` | `StringCchPrintfW(…, L"%ws\%ld%ws", L"\Sessions", SessionId, L"\Windows")` — `SessionId` 가 5 의 값과 **다를 때만** (`+0x25b CMP` · `+0x25d JZ`). 같으면 `+0x254` 의 `\Windows` | 없음 | | |
| 7 | `+0x2aa/+0x2b1` | **`CsrClientConnectToServer`** (ntdll `0xdb7a0`) | 이미 연결이면 `[ntdll] NtAlpcSendWaitReceivePort 0x8e` ×1. 처음이면 run68 첫 연결 6 syscall | 음수 아님 + 답 `+8` 부터 `SHAREDINFO` | `+0x2b8 JS` → FALSE |
| 8 | `+0x30b ~ +0x37c` · `+0x380 ~ +0x387` | (코드) 답 → `gSharedInfo` 0x240B, 첫 8B → `gpsi` | — | | 보호 프로세스 갈래만 `+0x2fe JNZ` → FALSE |
| 9 | `+0x38e ~ +0x3d6` | `psi` 첫 바이트 비트 4 면 IME 표 (`_InitializeImmEntryTable` · `GetImmFileName` · `GetModuleHandleW`). 늘 `+0x3d1/+0x3d6` CFG 간접 호출 `gImmApiEntries+0xa0` (rva `0xd9630`, 파일 값 `0x5a1a0` = 0 반환 3B) | 없음 | `psi` 읽기 가능 | (널이면 접근 위반) |
| 10 | `+0x3d8/+0x3dd` | `InitializeCsrUserPfn` — `gfServerProcess ≠ 0` 일 때만 | | | `+0x3df JZ` → FALSE (서버 길만) |
| 11 | `+0x3fc` · `+0x409` · `+0x410/+0x417` | (코드) PEB `+0x58` ← `apfnDispatch` (rva `0xb4720`), PEB `+0x230` ← 0, `RegisterWaitForInputIdle` | 없음 | | |
| 12 | `+0x41c ~ +0x437` · `+0x445/+0x44c` · `+0x463/+0x46a` | `hmodUser` · `pUserHeap` (← PEB `+0x30`) 저장. `g_systemCallFilterId == 7` (파일 처음 값) 이면 `GetProcessMitigationPolicy(자기, 0xb, &v, 4)` | kernelbase 안이라 안 풂 `[추측: NtQueryInformationProcess]` | 실패여도 계속 | |
| 13 | `+0x48e ~ +0x568` | `g_systemCallFilterId == 5` 일 때만 `apfnDispatch` 0x498B 를 힙 사본으로 PEB `+0x58` 교체 + `InitWinpropManager` ×2 | 없음 | 할당 성공 | `+0x4b5 JZ` → FALSE |
| 14 | `+0x576/+0x57b` | `InitInstrument` (rva `0x502c4`, `gfServerProcess == 0` 일 때) | `[ntdll] NtOpenKey 0x12` (`InitInstrument +0x98/+0x9f`, `…\Control\Error Message Instrument\`) · `NtQueryValueKey 0x17` · `NtClose 0xf` | 키 없어도 됨 | FALSE 아님 |
| 15 | `+0x590` · `+0x5b9` · `+0x5d2/+0x5d9` | `RtlQueryElevationFlags` · `RtlQueryInformationActiveActivationContext(5, …)` · 매니페스트 `uiAccess` 면 `NtUserSetProcessUIAccessZorder` | **`[win32u] 0x157b`** (조건부) | | FALSE 아님 |
| 16 | `+0x5e5` · `+0x5f1` · `+0x5fd` · `+0x710` · `+0x73b ~ +0x760` | TraceLogging 등록 ×3 (`TraceLoggingRegisterEx_EventRegister_EventSetInformation`), `WppInitUm`, `WilInitialize_Result(0)`, UserModeMenus 기능이면 `RtlAllocateHeap(pUserHeap, 8, 8)` → PEB `+0x7d0` | user32 직접은 없음 (결과서). 등록마다 ntdll `EtwEventRegister` 를 거치면 `NtTraceControl` (`0xf` · 트레잇 `0x1e`) `[추측: run62 · run68 꼴, user32 사슬은 안 봄]` | 할당 성공 | `+0x74a JZ` → FALSE |
| 17 | `+0x767/+0x76c` · `+0x76c/+0x771` | `InitProcessGdiScaling` · `InitProcessSystemDpiBehavior` | IFEO (`LdrQueryImageFileExecutionOptions`) · 정책 키 `…\Policies\Microsoft\Windows\Display` (`NtOpenKey` · `NtQueryValueKey` ×2 · `NtClose`) · `QueryActCtxSettingsW` | 실패해도 계속 | FALSE 아님 |
| 18 | `+0x996/+0x99d` | **`GdiDllInitialize(hmodUser32, 1)`** (GDI32 가져오기, IAT `0xb4fe0`) | **run86** | 반환이 음수 아님 (0 도 통과) | `+0x9af JNS` 안 타면 `+0x9b1` → FALSE |
| 19 | `+0x9b8/+0x9bd` | `InitProcessDpiAwareness` (rva `0x4cf68`) | 조건부 `[win32u] 0x1576` · `0x1440` (아래 번호표) | | FALSE 아님 |
| 20 | `+0x9e9` · `+0xa17/+0xa1e` · `+0xa4f` · `+0xa80/+0xa87` | 매니페스트 `disableWindowFiltering` · `forceFocusBasedMouseWheel` 읽기 (`QueryActCtxSettingsW`) → 참이면 win32k | **`[win32u] 0x13ec`** · **`0x1579`** (조건부) | | FALSE 아님 |
| 21 | `+0xa8c ~ +0xa93` | 반환 `EAX = (~GdiDllInitialize 결과) >> 31` | | | |

### FALSE 를 돌려주는 자리 (전부 목적지 `+0x9b1 XOR EAX,EAX`)

| # | 점프 | 조건 |
|---|---|---|
| 1 | `+0x193 JL` | 임계구역 10개 상태 OR 가 음수 |
| 2 · 3 | `+0x1a7 JS` · `+0x1bb JS` | `InitializeUMHandleList` #1 · #2 음수 |
| 4 | `+0x1df JS` | `NtQuerySystemInformation(0)` 음수 |
| 5 | `+0x1fa JZ` | `InitializeNtdllUserPfn` 0 (ntdll `RtlInitializeNtUserPfn` · `RtlRetrieveNtUserPfn` 실패 — 이미 한 번 불렸어도) |
| 6 | `+0x2b8 JS` | `CsrClientConnectToServer` 음수 |
| 7 | `+0x2fe JNZ` | 보호 프로세스 (PEB `+3` 비트 `0x2` 켜짐 · `0x40` 꺼짐) 인데 PEB `+0x58` 이 이미 0 아님 |
| 8 | `+0x3df JZ` | `InitializeCsrUserPfn` 0 (서버 프로세스만) |
| 9 | `+0x4b5 JZ` | `g_systemCallFilterId == 5` 갈래 `RtlAllocateHeap(0x498)` NULL |
| 10 | `+0x74a JZ` | UserModeMenus 갈래 `RtlAllocateHeap(8)` NULL |
| 11 | `+0x9af JNS` 안 탐 | `GdiDllInitialize` 음수 |

- `psi` 가 널이면 FALSE 가 아니라 `+0x38e` 접근 위반 (검사 없음).
- 반환은 `GdiDllInitialize` 가 음수인지만 본다 — gdi32 쪽이 0 (FALSE) 을 줘도 user32 `DllMain` 은 TRUE.

### 데이터 칸 표 (`DllMain` 이 읽고 쓰는 것만)

user32 · win32u PDB 는 공개 심볼만이라 구조체 타입이 0개다 (`llvm-pdbutil` 0 records). 그래서 **오프셋 · 크기 · 읽는 줄은 `[정적]`, 칸 이름은 `[추측]`**. `_PEB` · `_TEB` 이름은 ntdll PDB.

| 어디 | 칸 | 크기 | 쓰는 줄 / 읽는 줄 | 뜻 | 없으면 |
|---|---|---|---|---|---|
| csrss 답 버퍼 (호출자 스택 `RSP+0x60`) | `+0x0 ~ +0x7` | 8 | `DllMain` 은 안 읽음 | `[추측]` 개수 · 버전 | — |
| 같음 | `+0x8 ~ +0x247` | `0x240` | 복사 `+0x30b ~ +0x37c` | `SHAREDINFO` 모양 `[이름 추측]` | `gSharedInfo` 0 → `psi` 널 → 접근 위반 |
| `gSharedInfo` (user32 rva `0xda350`) | `+0x0` | 8 | 쓰기 `+0x31f`, 읽기 `+0x380` | `psi` (`SERVERINFO*`) `[이름 추측]` | 같음 |
| 같음 | `+0x8 ~ +0x23f` | `0x238` | 쓰기만 (복사) | 핸들 표 · 메시지 표 류 `[이름 추측]` | `DllMain` 은 통과. 창 API 쓸 때 필요 |
| `gpsi` (rva `0xda590`) | — | 8 | 쓰기 `+0x387`, 읽기 `InitProcessDpiAwareness +0x9f` · `GetDpiForCurrentProcess +0x6` | `psi` 사본 | |
| `psi` | `+0x0` 비트 `0x4` | 1 | `+0x38e TEST byte ptr [RAX],4` | 켜지면 IME 표 로드 `[뜻 추측]` | 0 이면 IME 건너뜀 (최소) |
| `psi` | `+0x8bc` 비트 `0x20` | 1 | `InitProcessDpiAwareness +0xa8` | 켜지면 DPI 판정 → win32k syscall 가능 | 0 이면 DPI 쪽 win32k syscall 안 감 |
| `psi` | `+0x1b66` | 2 | `GetDpiForCurrentProcess +0x13` (위 비트가 켜졌을 때만) | 시스템 DPI 캐시 `[이름 추측]` | |
| `gHighestUserAddress` (rva `0xda9f8`) | — | 8 | 쓰기 `+0x1ec` ← 버퍼 `+0x28` | `MaximumUserModeAddress` | |
| `gfServerProcess` (rva `0xda9e8`) | — | 4 (ntdll 은 1B 만 씀) | 쓰기 ntdll `CsrClientConnectToServer +0xc7` · `+0x1cb`, 읽기 `+0x2be` · `+0x568` · `InitProcessDpiAwareness` 등 | 0 = 보통 프로세스 (PE 헤더 서브시스템 ≠ 1) | 1 이면 서버 갈래 전부 |
| PEB `+0x58` (`KernelCallbackTable`) | — | 8 | 쓰기 `+0x3fc` ← `apfnDispatch` (rva `0xb4720`, 0x498B 표. 84번째 칸 `+0x2a0` = `__ClientThreadSetup`, rva `0xb49c0`) | 커널 → 유저 콜백 표 | 커널이 `__ClientThreadSetup` 을 못 부름 |
| PEB `+0x230` (`PostProcessInitRoutine`) | — | 8 | 쓰기 `+0x409` ← 0 | | |
| PEB `+0x7d0` (`Win32ClientProcInfo`) | — | 8 | 쓰기 `+0xae` ← 0, UserModeMenus 켜짐이면 `+0x760` ← `gpClientProcInfo` | | |
| PEB `+0x2c0` (`SessionId`) | — | 4 | 읽기 `+0x9f` | 디렉터리 이름 | |
| PEB `+0x90` (`SharedData`) | — | 8 | 읽기 ntdll `RtlGetCurrentServiceSessionId +0x9 ~ +0x17` | 첫 dword = 서비스 세션 번호 | 널이면 0 |
| PEB `+0x30` (`ProcessHeap`) | — | 8 | 읽기 → `pUserHeap` (rva `0xdaa10`) | | |
| PEB `+0x2d0` (`AppCompatFlagsUser`) | — | 8 | 읽고 쓰기 `InitProcessGdiScaling` · `InitProcessSystemDpiBehavior` · `InitProcessDpiAwareness` | DPI · GDI 스케일 비트 | |
| PEB `+0x3` 비트 `0x2` · `0x40` | — | 1 | 읽기 `+0x210` · `+0x21f` · `+0x2d9` · `+0x2eb` | 보호 프로세스 여부 | 0 이면 보통 갈래 |
| TEB `+0x800` (`Win32ClientInfo`) 비트 29, `+0x8e0` 비트 `0x10` · `0x80` | — | | 읽기 `InitProcessDpiAwareness +0xc1` 근처 | GUI 변환 전엔 0 | |
| `gfFirstThread` (rva `0xd996c`) | — | 4 | 파일 처음 값 1, `ClientThreadSetup +0x82/+0x8c` 가 읽고 0 으로 | 첫 GUI 스레드 표시 | |
| ntdll `.mrdata` `NtUserPfn` (rva `0x1e7130 ~ 0x1e733f`) · 플래그 `0x1e7340` | 3표 | `0xc0` · `0xc0` · `0x90` + 1 | 쓰기 ntdll `RtlInitializeNtUserPfn +0x97` · `+0xa9` · `+0xbf` (복사) · `+0xc9` (플래그 ← 1) | user32 함수 표 사본 `[뜻 추측]` | 플래그가 이미 1 이면 `0xc000000d` → 표 4번 FALSE |

### win32u syscall 번호표 (이번 길)

스텁 모양은 전부 `MOV R10,RCX; MOV EAX,imm; TEST byte ptr [0x7ffe0308],1; JNZ → INT 0x2e; SYSCALL; RET` (24B).

| 번호 | 이름 | win32u rva | 이번 길에서 |
|---|---|---|---|
| `0x1000` | `NtUserGetThreadState` | `0x12b0` | (참고) 가장 낮은 번호 |
| `0x107b` | `NtUserGetThreadDesktop` | `0x2210` | `ClientThreadSetup +0x29c` |
| `0x10e2` | `NtUserProcessConnect` | `0x2ef0` | `ClientThreadSetup +0x120`, **서버 프로세스 갈래만** |
| `0x12ed` · `0x12ee` | `NtGdiInit` · `NtGdiInit2` | `0x7050` · `0x7070` | 결과서가 gdi32 쪽 후보 둘로 적음 — **`NtGdiInit` 후보는 틀림, run86 (sast/83) 에서 정정** |
| `0x13ec` | `NtUserDisableProcessWindowFiltering` | `0x9030` | `DllMain +0xa17` (조건부) |
| `0x1440` | `NtUserGetDpiForCurrentProcess` | `0x9ab0` | `GetDpiForCurrentProcess +0x28` (조건부) |
| `0x1475` | `NtUserGetProcessUIContextInformation` | `0xa150` | `ClientThreadSetup +0x356` |
| `0x14a6` | `NtUserInitAnsiOem` | `0xa770` | `InitOemXlateTables +0x12f` (서버 갈래만) |
| `0x14aa` | `NtUserInitializeClientPfnArrays` | `0xa7f0` | `ClientThreadSetup +0x228`, 서버 갈래만 |
| `0x14ae` | `NtUserInitializeUserModeMenus` | `0xa870` | `ClientThreadSetup` 이 `GetProcAddress` 로 찾아 부름 (기능 켜짐일 때) |
| `0x150a` | `NtUserRegisterLPK` | `0xb3f0` | `ClientThreadSetup +0x2c0` (조건부) |
| `0x151f` | `NtUserRemoteConnectState` | `0xb690` | `ClientThreadSetup +0x272` |
| `0x1576` | `NtUserSetProcessDpiAwarenessContext` | `0xc170` | `SetProcessDpiAwarenessContext +0x3a` (조건부) |
| `0x1579` | `NtUserSetProcessMousewheelRoutingMode` | `0xc1d0` | `DllMain +0xa80` (조건부) |
| `0x157b` | `NtUserSetProcessUIAccessZorder` | `0xc210` | `DllMain +0x5d2` (조건부) |

- ntdll 쪽 번호 (같은 길): `NtQuerySystemInformation 0x36` · `NtOpenKey 0x12` · `NtQueryValueKey 0x17` · `NtClose 0xf` · `NtProtectVirtualMemory 0x50` · `NtCallbackReturn 0x5` · **`NtAlpcSendWaitReceivePort 0x8e`** (ntdll rva `0x161f60`). 첫 연결 길: `NtOpenSection 0x37` · `NtCreateSection 0x4a` · `NtMapViewOfSection 0x28` · `NtConnectPort 0xa4`.

### PE_emulator 가 본 `NtOpenSection(SharedSection)` `0xc000003a` — run68 과 이어 읽기

- `[정적]` 이 프로세스의 **첫** csrss 연결은 kernelbase 시작 함수가 한다 (run68): `_KernelBaseBaseDllInitialize +0x2d2/+0x2d9 CsrClientConnectToServer("\Sessions\1\Windows", 1 = BASESRV, …)` → `CsrPortHandle == 0` 이라 `+0x113/+0x118 CsrpConnectToServer` → `+0x1b6 NtOpenSection(SharedSection)` · `NtCreateSection` · `NtConnectPort` · `NtMapViewOfSection` … 6 syscall. **`CsrPortHandle` 은 `NtConnectPort` 의 출력 칸** (`CsrpConnectToServer +0x2d3 LEA RCX,[CsrPortHandle]` → `+0x2df/+0x2e4`, run68) — 커널 (= 엔진) 이 써 준다.
- `[정적]` user32 는 그 뒤에 서버 3 으로 **같은 함수를 또** 부른다 (`+0x2aa`). 이때 `CsrPortHandle ≠ 0` 이어야 `+0xb5 CsrpClientConnectToServer` (ALPC 한 번) 로 간다.
- 그러니 user32 `DllMain` 에서 `NtOpenSection(SharedSection)` 이 보였다는 건 **그 순간 ntdll `CsrPortHandle` (rva `0x1d4e20`) 이 0** 이었다는 뜻이다. 거기서 `0xc000003a` → `CsrpConnectToServer` 실패 → `CsrClientConnectToServer` 음수 → `_UserClientDllInitialize +0x2b8 JS` → FALSE (위 FALSE 표 6번) → 로더가 `0xc0000142`.
- 왜 0 이었나는 PE_emulator 쪽 사정이라 모른다 `[공백]`. 후보 `[추측]`:
  - ① kernelbase 의 첫 연결 (run68 stage 6) 을 **안 탔다** — 흉내가 그 DLL 시작을 건너뛰었거나, 덤프에서 이어 붙인 상태에 `CsrPortHandle` 이 비었거나.
  - ② 탔지만 `NtConnectPort` 몸이 성공을 돌려주면서 **출력 칸에 핸들을 안 썼다**.
  - kernelbase 가 같은 몸 (`NtOpenSection` → `0xc000003a`) 으로 첫 연결 길을 탔다면 그때 먼저 실패해 kernelbase 시작 함수가 FALSE 였을 것이다 (run68 "실패 길: 6 이 음수면 DllMain FALSE"). user32 에서 처음 막혔다는 건 ① 쪽에 가깝다 `[추측]`.
- 바로잡는 쪽 (둘 중 하나): (가) kernelbase 의 첫 연결이 성공해 `CsrPortHandle` 이 0 이 아니게 두고, user32 의 둘째 연결엔 `NtAlpcSendWaitReceivePort` 답 (위 ① ② ③) 을 준다 — 정상 Windows 와 같은 길. (나) 첫 연결 길 6 syscall 을 user32 때 대답한다 — 이 길에서 서버 3 의 연결 정보 0x248B 가 어떻게 가고 답이 어디로 오는지는 이번에 안 봤다 `[공백]` (아래 정적 ⑥).

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적 (이번에 처음 analyze — 결과서):
  - user32 pdb `CDB947D1B96B87977915DE04CF54279C1` (`ms/sym/user32.dll.pdb.info.txt`), `_index.tsv` 3248줄
  - win32u pdb `6295787D0B7E537E98F77F31F4426D1C1` (`ms/sym/win32u.dll.pdb.info.txt`), `_index.tsv` 1517줄
  - 두 PDB 모두 **공개 심볼만이라 타입이 0개** (`llvm-pdbutil dump -types` 0 records, `pdb_struct.py --list` 빈 결과). 구조체 칸 이름은 모두 `[추측]`, 칸 위치 · 값은 `.asm` 의 `[정적]`.
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` (기준과 같음 — 다시 analyze 안 함, 읽기 전용으로 depth 0 몇 개 더). kernelbase 는 프로젝트를 안 열고 `.dll` 을 `objdump` 로 8줄만 (`WTSGetServiceSessionId` 의 `JMP`).
- 버전 대조 (결과서): PE 헤더 `AddressOfEntryPoint` = `0x51120` = Ghidra `UserClientDllInitialize` 시작. `.asm` 의 `CALL qword ptr [0x1800b5b38]` 칸 = PE 가져오기 표의 `ntdll!CsrClientConnectToServer` 칸 `0xb5b38`. ntdll 알려진 사실 표 (`LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess`) 일치. flow-writer 도 `pefile` 로 진입점 · IAT 넷 (`0xb5ab0 NtCallbackReturn` · `0xb5b38 CsrClientConnectToServer` · `0xb4fe0 GdiDllInitialize` · `0xb4fd8 GdiProcessSetup`) 을 맞췄다.

## 목적

- PE_emulator 요청 28 (user32 · gdi32 초기화 최소 길 — GUI 벽) 의 첫 조각 (Q150a): 원문 1 (user32 `DllMain` 의 syscall 차례 · 답 · FALSE 자리), 2 (`gSharedInfo` · `gpsi` 최소 칸), 3 (GUI 스레드로 바뀌는 때), 그리고 win32u 이름표. **"최소 길 지도" 수준** (사용자 결정 2026-11-12).
- gdi32 · gdi32full (원문 4) 과 최소 조건 표 둘 (원문 5) 은 짝 바퀴 run86 (Q150b, sast/83).

## 방법

- 정적: `_workspace/sast/82_user32-init.md` — user32 · win32u (새 analyze, PDB 붙음, SCRIPT ERROR 0개), ntdll (읽기 전용)
  - 지도 (새로): `ms/disassemble/user32/_map/UserClientDllInitialize_{edges,nodes}.tsv` (기준 20 에서 함수 136개, 끊긴 끝 100 — 바깥 80 · CFG 간접 20). 결과서는 줄기 2단계까지만 읽음.
  - 뽑음 (user32 depth 0): `UserClientDllInitialize` · `_DllMainCRTStartupForGS2` · `_UserClientDllInitialize` · `InitializeUMHandleList` · `InitializeNtdllUserPfn` · `InitializeCsrUserPfn` · `ClientThreadSetup` · `__ClientThreadSetup` · `InitProcessGdiScaling` · `InitProcessSystemDpiBehavior` · `InitProcessDpiAwareness` · `InitInstrument` · `GetDpiForCurrentProcess` · `Parse*` · `IsDisplaySettingsGroupPolicyFlagSet` · `SetProcessDpiAwarenessContext` · `SetProcessDpiAwarenessInternal` · `InitClientDrawing` 등. win32u 스텁 12개 (`ms/disassemble/win32u/` — 결과서 "12/12 같음" 의 그 12개. 같은 폴더의 나머지 3개 `NtUserGetThreadState` · `NtGdiInit` · `NtGdiInit2` 는 나중에 sast/83 이 뽑은 것 — 파일 시각 · `ms/ghidra/win32u.export.sast83a.stdout`, run86 몫). ntdll depth 0 새로: `RtlInitializeNtUserPfn` · `RtlRetrieveNtUserPfn` · `RtlResetNtUserPfn` · `CsrAllocateCaptureBuffer` · `CsrAllocateMessagePointer` · `RtlGetCurrentServiceSessionId`.
  - `refs` (`ms/disassemble/user32/_refs/`): `0xda590` (`gpsi`) · `0xda350` (`gSharedInfo`) · `0xb63f8` (`NtUserProcessConnect` IAT) · `0xb63f0` (`NtUserInitializeClientPfnArrays`) · `0xb6408` · `0xb64d0` · `__ClientThreadSetup`.
  - win32u 전체 번호표 (원문엔 없고 요청서 질문 4 의 "싸게 뽑히면 전체"): `ms/disassemble/win32u/_syscalls.tsv` — 스텁 바이트 (`4C 8B D1 B8 imm32`) 를 직접 읽어 만듦, 1499개, 번호 중복 없음, Ghidra `.asm` 과 12/12 같음.
- 결과서: `_workspace/sast/82_user32-init_result.md` (**분석성공 — 최소 길 지도 수준**. 질문 1 ~ 4 모두 `.asm` 줄로 답함. 칸 이름 · "커널 누가 `Win32ThreadInfo` 를 쓰나" 는 `[추측]`, user32 `DllMain` 안 첫 win32k syscall 자리는 gdi32 에 달림)
- 오케스트레이터가 `.asm` 으로 다시 맞춘 줄: user32 진입점 rva `0x51120`, `_UserClientDllInitialize +0x2aa CsrClientConnectToServer` (서버 3), ntdll `CsrpConnectToServer +0x1b6 NtOpenSection` (`CsrPortHandle == 0` 일 때만).
- flow-writer 도 `.asm` 으로 다시 봤다: user32 `UserClientDllInitialize` (전부) · `__ClientThreadSetup` (전부) · `_UserClientDllInitialize` 의 call · 점프 줄 (`+0x9f` · `+0xae` · `+0xb5` · `+0x193` · `+0x1a0` · `+0x1a7` · `+0x1bb` · `+0x1d1` · `+0x1df` · `+0x1ec` · `+0x1f3` · `+0x1fa` · `+0x248` · `+0x254` · `+0x25b` · `+0x25d` · `+0x282` · `+0x2aa` · `+0x2b8` · `+0x2fe` · `+0x31f` · `+0x380` · `+0x387` · `+0x38e` · `+0x3d1` · `+0x3d8` · `+0x3df` · `+0x3fc` · `+0x409` · `+0x410` · `+0x445` · `+0x463` · `+0x4b5` · `+0x568` · `+0x576` · `+0x590` · `+0x5b9` · `+0x5d2` · `+0x5e5` · `+0x5f1` · `+0x5fd` · `+0x710` · `+0x74a` · `+0x760` · `+0x767` · `+0x76c` · `+0x996` · `+0x9af` · `+0x9b1` · `+0x9b8` · `+0x9e9` · `+0xa17` · `+0xa4f` · `+0xa80` · `+0xa8c`) · `InitializeNtdllUserPfn` call 줄 · `ClientThreadSetup +0x0 ~ +0xd3` · `+0x258` · `+0x263` · `+0x324` · `+0x41b` · `+0x429`. ntdll `CsrClientConnectToServer +0x6f ~ +0x123` · `CsrpClientConnectToServer` call 줄 · `CsrClientCallServer +0x0 ~ +0x12c` · `CsrAllocateMessagePointer` (전부) · `CsrpConnectToServer +0x1b6` · `+0x338` · `RtlInitializeNtUserPfn` (전부) · `RtlResetNtUserPfn` (전부) · `RtlGetCurrentServiceSessionId` (전부). ntoskrnl `KiConvertToGuiThread` (전부).
- 이름 확인 (flow-writer): `_calls.tsv` 에 `alias:` 로 나온 user32 함수 (`InitializeUMHandleList` · `InitializeNtdllUserPfn` · `InitializeCsrUserPfn` · `InitInstrument` · `InitProcess*` 등) 는 `pdb_aliases.py --lookup user32.dll <rva>` 로 **별칭 무리가 아님** (같은 이름의 C++ 꾸민 이름 하나) 을 확인하고 이름을 그대로 썼다. ntdll rva `0x137040` = `UninitUser32Proc` (21B, `_index.tsv`).
- 값 대조 (flow-writer): test.exe 는 user32 를 안 올리므로 user32 칸 값은 사진에 없다. 갈래를 가르는 칸과 user32 가 놓을 칸이 test.exe 에서 어떤지만 run44 덤프 읽기 로그에서 다시 읽었다 — 아래 "보탠 것" ④. `objdump -s` 로 ntdll 파일의 `NtUserPfn` 처음 값도 읽었다. 새 동적 실행은 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/{user32,ntdll,ntoskrnl}/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `user32!UserClientDllInitialize` (`0x51120`) | `+0x18` | `+0x1d` | `_DllMainCRTStartupForGS2` | 이유 1 (`+0x14 CMP EDI,1`) | [정적] |
| `user32!UserClientDllInitialize` | `+0x2c` | (꼬리 `JMP`) | `_UserClientDllInitialize` (`0x6ba84`) | 늘 | [정적] |
| `user32!_UserClientDllInitialize` | `+0xb5` | `+0xbc` | `DisableThreadLibraryCalls` | 이유 1 | [정적] |
| `user32!_UserClientDllInitialize` | **`+0x1d1`** | `+0x1d8` | `NtQuerySystemInformation` | 클래스 0 · `0x40`B. 음수 → `+0x1df JS` FALSE | [정적] |
| `user32!_UserClientDllInitialize` | `+0x1f3` | `+0x1f8` | `InitializeNtdllUserPfn` (`0x6b924`) | 0 → `+0x1fa JZ` FALSE | [정적] |
| `user32!InitializeNtdllUserPfn` | `+0x3f` · `+0x62` | `+0x46` · `+0x69` | `ntdll!RtlInitializeNtUserPfn` · `ntdll!RtlRetrieveNtUserPfn` | | [정적] |
| `user32!_UserClientDllInitialize` | `+0x248` | `+0x24f` | `WTSGetServiceSessionId` (→ ntdll `RtlGetCurrentServiceSessionId`) | | [정적] |
| `user32!_UserClientDllInitialize` | `+0x282` | `+0x287` | `StringCchPrintfW` | 세션이 서비스 세션과 다를 때 (`+0x25d JZ` 안 탐) | [정적] |
| `user32!_UserClientDllInitialize` | **`+0x2aa`** | `+0x2b1` | `ntdll!CsrClientConnectToServer` | 서버 3 · 0x248B. 음수 → `+0x2b8 JS` FALSE | [정적] (오케스트레이터 확인) |
| `ntdll!CsrClientConnectToServer` (`0xdb7a0`) | **`+0xb5`** | `+0xba` | `CsrpClientConnectToServer` (`0xdb990`) | `CsrPortHandle ≠ 0` (`+0xa4 CMP` · `+0xab JZ` 안 탐) — user32 의 보통 길. 또는 첫 연결 뒤 응답 상태 음수 (`+0x11c` · `+0x121 JL → +0xad`) | [정적] |
| `ntdll!CsrClientConnectToServer` | `+0x113` | `+0x118` | `CsrpConnectToServer` (`0xdbfe4`) | `CsrPortHandle == 0` (첫 연결 — run68) | [정적] |
| `ntdll!CsrpConnectToServer` | **`+0x1b6`** | `+0x1bb` | `NtOpenSection` | 첫 연결 길만 (run68 표) | [정적] (오케스트레이터 확인) |
| `ntdll!CsrpClientConnectToServer` | `+0x40` · `+0x57` · `+0x69` | `+0x45` · `+0x5c` · `+0x6e` | `CsrAllocateCaptureBuffer(1, 0x248)` · `CsrAllocateMessagePointer` · 메모리 복사 (호출자 → 캡처) | 할당 실패면 `0xc0000017` | [정적] |
| `ntdll!CsrpClientConnectToServer` | **`+0x7f`** | `+0x84` | `CsrClientCallServer(메시지, 캡처, API 0, 0x18)` | | [정적] |
| `ntdll!CsrpClientConnectToServer` | `+0x95` · `+0x9d` | `+0x9a` · `+0xa2` | 메모리 복사 (캡처 → 호출자) · `CsrFreeCaptureBuffer` | 복사는 음수 아닐 때 | [정적] |
| `ntdll!CsrClientCallServer` (`0xdbdd0`) | **`+0xed`** | `+0xf2` | `NtAlpcSendWaitReceivePort` | 포트 `CsrPortHandle` · 플래그 `0x20000` · 보내기 = 받기 = 같은 메시지 · 받는 길이 `0x3b8`. 보안 프로세스 (`+0x1e JNZ`) · 보호 프로세스 (`+0x69 JNZ`) · 서버 (`+0x76 JZ` → `+0x182` 간접 `CsrServerApiRoutine`) 는 다른 길 | [정적] |
| `user32!_UserClientDllInitialize` | `+0x3d1` | `+0x3d6` | (CFG 간접) `gImmApiEntries+0xa0` | 늘. 파일 값 = 0 반환 3B | [정적] |
| `user32!_UserClientDllInitialize` | `+0x3d8` | `+0x3dd` | `InitializeCsrUserPfn` | `gfServerProcess ≠ 0` 만 | [정적] |
| `user32!_UserClientDllInitialize` | `+0x463` | `+0x46a` | `GetProcessMitigationPolicy` | `g_systemCallFilterId == 7` (파일 처음 값) | [정적] |
| `user32!_UserClientDllInitialize` | `+0x576` | `+0x57b` | `InitInstrument` (`0x502c4`) | `gfServerProcess == 0` | [정적] |
| `user32!_UserClientDllInitialize` | `+0x5d2` | `+0x5d9` | `NtUserSetProcessUIAccessZorder` | 승격 아님 + 매니페스트 `uiAccess` | [정적] |
| `user32!_UserClientDllInitialize` | `+0x5e5` · `+0x5f1` · `+0x5fd` | `+0x5ea` · `+0x5f6` · `+0x602` | `TraceLoggingRegisterEx_EventRegister_EventSetInformation` ×3 | | [정적] |
| `user32!_UserClientDllInitialize` | `+0x767` · `+0x76c` | `+0x76c` · `+0x771` | `InitProcessGdiScaling` · `InitProcessSystemDpiBehavior` | | [정적] |
| `user32!_UserClientDllInitialize` | **`+0x996`** | `+0x99d` | `GdiDllInitialize` (GDI32 가져오기) | 늘 (이유 1). 음수 → `+0x9b1` FALSE — gdi32 쪽은 run86 | [정적] |
| `user32!_UserClientDllInitialize` | `+0x9b8` | `+0x9bd` | `InitProcessDpiAwareness` (`0x4cf68`) | | [정적] |
| `user32!_UserClientDllInitialize` | `+0xa17` · `+0xa80` | `+0xa1e` · `+0xa87` | `NtUserDisableProcessWindowFiltering` · `NtUserSetProcessMousewheelRoutingMode` | 매니페스트 값이 참일 때 | [정적] |
| `ntoskrnl!KiConvertToGuiThread` (`0x6affb0`) | `+0x4` | `+0x9` | `PsConvertToGuiThread` | 첫 win32k syscall 때 (이름만) | [정적] |
| `user32!__ClientThreadSetup` (`0x4f7d0`) | `+0x4` | `+0x9` | `ClientThreadSetup` (`0x4f810`) | 커널 콜백 (`apfnDispatch` 84번째 칸) | [정적] |
| `user32!__ClientThreadSetup` | `+0x20` | (꼬리 `JMP`) | `NtCallbackReturn` (IAT `0xb5ab0`) | 상태 = `ClientThreadSetup` 이 TRUE 면 0, FALSE 면 `0xc0000001` (`+0x9 ~ +0x15`) | [정적] (flow-writer) |
| `user32!ClientThreadSetup` | `+0x120` · `+0x228` | `+0x127` · `+0x22f` | `NtUserProcessConnect(-1, 0x240, buf)` · `NtUserInitializeClientPfnArrays` | **`gfServerProcess ≠ 0` (`+0xdd` · `+0x1b9`) + `gfFirstThread ≠ 0` 만** | [정적] |
| `user32!ClientThreadSetup` | `+0x93` | `+0x9a` | `GdiProcessSetup` (GDI32 가져오기, IAT `0xb4fd8`) | `gfFirstThread ≠ 0` (`+0x82 ~ +0x8a`, 그리고 `+0x8c` 에서 0 으로) — 프로세스 첫 GUI 스레드. gdi32 쪽은 run86 | [정적] (flow-writer 줄 확인) |
| `user32!ClientThreadSetup` | `+0x272` · `+0x285` · `+0x29c` · `+0x356` · `+0x374` | `+0x279` · `+0x28a` · `+0x2a3` · `+0x35d` · `+0x37b` | `NtUserRemoteConnectState` · `InitClientDrawing` · `NtUserGetThreadDesktop` · `NtUserGetProcessUIContextInformation` · `LoadAppInitDlls` | 보통 프로세스 (`+0xe5 JZ → +0x258` 쪽) 첫 GUI 스레드 (결과서) | [정적] |

### 호출 관계

가지의 `+0x..` 는 ret 자리 (콜스택에 보일 자리), 가지 밖 `+0x..` 는 그 줄 자리. 같은 번호 둘 = 값에 따라 둘 중 하나.

```
[user32 시작 함수 — 이유 1 (PROCESS_ATTACH). ntdll 2.7 LdrpCallInitRoutine 이 부름. test.exe 는 user32 를 안 올림]
user32!UserClientDllInitialize (0x51120, PE 진입점)
 ├─ (1) +0x1d   _DllMainCRTStartupForGS2 (0x71e80)          /GS 쿠키, 1 반환
 └─ (2) +0x2c   JMP ▶ _UserClientDllInitialize (0x6ba84)     EAX = DllMain 반환
      +0x9f SessionId · +0xae PEB+0x7d0 ← 0 · +0xbc DisableThreadLibraryCalls
      ├─ (1)  +0xc8 ~ +0x185  RtlInitializeCriticalSection ×10      +0x193 JL → FALSE
      ├─ (2)  +0x1a5 · +0x1b9  InitializeUMHandleList ×2             +0x1a7 · +0x1bb JS → FALSE
      ├─ (3)  +0x1d8  NtQuerySystemInformation(0, 0x40)       ── SYSCALL 0x36     +0x1df JS → FALSE · +0x1ec gHighestUserAddress
      ├─ (4)  +0x1f8  InitializeNtdllUserPfn (0x6b924)                           +0x1fa JZ → FALSE
      │        ├─ (1) +0x46  ntdll!RtlInitializeNtUserPfn (0x11f7c0)   +0x85 LdrProtectMrdata(0) · +0x97 · +0xa9 · +0xbf 표 셋 → NtUserPfn · +0xc9 플래그 ← 1 · +0xd0 LdrProtectMrdata(1)
      │        └─ (2) +0x69  ntdll!RtlRetrieveNtUserPfn (0x11fc30)
      ├─ (5)  +0x24f  WTSGetServiceSessionId → ntdll!RtlGetCurrentServiceSessionId    [PEB+0x90] 첫 dword (널이면 0)
      │       +0x25b SessionId ≠ 그 값 → +0x287 StringCchPrintfW("\Sessions\%ld\Windows")   (같으면 +0x254 "\Windows")
      ├─ (6)  +0x2b1  ntdll!CsrClientConnectToServer(디렉터리, 3, 버퍼 0x248B, 0x248, &gfServerProcess)    +0x2b8 JS → FALSE
      │        │   +0xc7 · +0x1cb *gfServerProcess ← 0 / 1 (PE 서브시스템 1 이면 1)
      │        ├─ (1) +0xba   CsrpClientConnectToServer (0xdb990)         ← CsrPortHandle ≠ 0 (+0xa4 · +0xab JZ 안 탐): 보통 길
      │        │        ├─ (1) +0x45  CsrAllocateCaptureBuffer(1, 0x248)    CsrPortHeap (포트 뷰 안)
      │        │        ├─ (2) +0x5c  CsrAllocateMessagePointer → 메시지 +0x48 · +0x6e 복사 (호출자 → 캡처) · 메시지 +0x40 ← 3
      │        │        ├─ (3) +0x84  CsrClientCallServer (0xdbdd0)(메시지, 캡처, API 0, 0x18)
      │        │        │        +0x81 ~ +0xb9 포인터 += CsrPortMemoryRemoteDelta
      │        │        │        └─ +0xf2  NtAlpcSendWaitReceivePort(CsrPortHandle, 0x20000, 메시지, 0, 메시지, &0x3b8, 0, 0)   ── SYSCALL 0x8e
      │        │        │        +0xfa ~ +0x12a 포인터 되돌림 · 음수면 +0x131 메시지 +0x34 ← 상태, 아니면 메시지 +0x34 반환
      │        │        ├─ (4) +0x9a  복사 (캡처 → 호출자 버퍼)
      │        │        └─ (5) +0xa2  CsrFreeCaptureBuffer
      │        └─ (1) +0x118  CsrpConnectToServer (0xdbfe4)              ← CsrPortHandle == 0 (+0xab JZ → +0xfe): 첫 연결 (run68)
      │                 └─ +0x1bb  NtOpenSection(…\SharedSection) · NtCreateSection · NtConnectPort · NtMapViewOfSection …   ── SYSCALL ×6 (run68)
      │                 +0x11c · +0x121 응답 상태 음수 → +0xad 로 위 CsrpClientConnectToServer
      │       +0x30b ~ +0x37c 답 +8 부터 0x240B → gSharedInfo (0xda350) · +0x387 gpsi ← 첫 8B · +0x38e psi 첫 바이트 (널이면 접근 위반)
      ├─ (7)  +0x3d6  (CFG 간접) gImmApiEntries+0xa0 → 0 반환 함수          psi 비트 4 면 IME 표 먼저
      │       +0x3d8 InitializeCsrUserPfn (서버만) · +0x3fc PEB+0x58 ← apfnDispatch (0xb4720) · +0x409 PEB+0x230 ← 0 · +0x410 RegisterWaitForInputIdle
      ├─ (8)  +0x46a  GetProcessMitigationPolicy(자기, 0xb, &v, 4)       g_systemCallFilterId == 7 (파일 값) · 5 면 +0x48e ~ +0x568 apfnDispatch 힙 사본
      ├─ (9)  +0x57b  InitInstrument (0x502c4) → +0x9f NtOpenKey(…\Error Message Instrument\)   ── SYSCALL 0x12 (키 없어도 됨)
      ├─ (10) +0x5d9  NtUserSetProcessUIAccessZorder                       ── SYSCALL [win32u] 0x157b (매니페스트 uiAccess 만)
      ├─ (11) +0x5ea · +0x5f6 · +0x602  TraceLoggingRegisterEx_EventRegister_EventSetInformation ×3     (→ NtTraceControl [추측])
      │       +0x710 WilInitialize_Result · +0x74a JZ → FALSE · +0x760 PEB+0x7d0 ← gpClientProcInfo (UserModeMenus 만)
      ├─ (12) +0x76c · +0x771  InitProcessGdiScaling · InitProcessSystemDpiBehavior         IFEO · 정책 레지스트리 · QueryActCtxSettingsW
      ├─ (13) +0x99d  GdiDllInitialize(hmodUser32, 1)        GDI32 가져오기 (IAT 0xb4fe0)     +0x9af JNS 안 타면 +0x9b1 FALSE   ── run86
      ├─ (14) +0x9bd  InitProcessDpiAwareness (0x4cf68)      psi+0x8bc 비트 0x20 · 매니페스트 · IFEO · TEB 비트일 때만
      │                 └─ SetProcessDpiAwarenessContext +0x41 NtUserSetProcessDpiAwarenessContext [win32u 0x1576] · GetDpiForCurrentProcess +0x2f NtUserGetDpiForCurrentProcess [0x1440]
      └─ (15) +0xa1e · +0xa87  NtUserDisableProcessWindowFiltering [0x13ec] · NtUserSetProcessMousewheelRoutingMode [0x1579]   (매니페스트 값 참일 때만)
      +0xa8c 반환 = GdiDllInitialize 결과가 음수 아니면 1

[GUI 스레드 변환 뒤 — 커널이 부르는 콜백 (그 스레드의 첫 win32k syscall 때)]
nt!KiConvertToGuiThread (0x6affb0)
 └─ +0x9  PsConvertToGuiThread                         (이름만 — sast/45 가 +0x1e0 · +0x238 win32k 콜아웃이라고 읽음)
user32!__ClientThreadSetup (0x4f7d0)                   ← apfnDispatch +0x2a0 (84번째 칸, rva 0xb49c0)
 ├─ (1) +0x9   ClientThreadSetup (0x4f810)
 │        +0x4c GetProcessMitigationPolicy (filter == 7) · +0x82 gfFirstThread ≠ 0 이면 +0x8c ← 0 → +0x9a GdiProcessSetup (gdi32 → run86)
 │        +0xca TEB+0x800 비트 8 이미 켜짐 → FALSE (같은 스레드 두 번째) · +0xd3 TEB+0x910 ← 2
 │        ├─ (서버 갈래: gfServerProcess ≠ 0 · 첫 스레드) +0x127 NtUserProcessConnect(-1, 0x240, buf) [0x10e2] · +0x22f NtUserInitializeClientPfnArrays [0x14aa]
 │        └─ (보통: +0xe5 JZ → +0x258) TEB+0x800 |= 8 · +0x263 TEB+0x918 · +0x279 NtUserRemoteConnectState [0x151f] · +0x28a InitClientDrawing
 │                 · +0x2a3 NtUserGetThreadDesktop [0x107b] · +0x324 TEB+0x820 ← pdiLocal · +0x35d NtUserGetProcessUIContextInformation [0x1475] · +0x37b LoadAppInitDlls
 └─ (2) +0x20  JMP ▶ NtCallbackReturn(0, 0, TRUE → 0 / FALSE → 0xc0000001)
```

## 확인된 것

- `[정적]` user32 의 PE 진입점은 `UserClientDllInitialize` (rva `0x51120`) 이고, 이유 1 이면 `/GS` 쿠키 뒤 `+0x2c` 꼬리 `JMP` 로 `_UserClientDllInitialize` 가 `DllMain` 몸이다. 반환값은 그 함수의 `EAX` 그대로.
- `[정적]` `_UserClientDllInitialize` 가 이유 1 에서 **무조건** 내는 바깥 호출 중 답이 맞아야 하는 것은 `NtQuerySystemInformation(0)` (`+0x1d1`) 과 `CsrClientConnectToServer(…, 3, …)` (`+0x2aa`) 둘뿐이다. FALSE 로 가는 자리는 11곳이고 모두 `+0x9b1 XOR EAX,EAX` 로 모인다.
- `[정적]` ntdll `CsrClientConnectToServer` 는 `CsrPortHandle` 이 0 이 아니면 (`+0xa4` · `+0xab`) `CsrpClientConnectToServer` (`+0xb5`) 로 가서 `CsrClientCallServer +0xed NtAlpcSendWaitReceivePort` 한 번으로 끝난다. 0 일 때만 `CsrpConnectToServer` (`+0x113`) 로 가서 `+0x1b6 NtOpenSection(SharedSection)` 등을 탄다. user32 안에는 `NtOpenSection` 이 없다.
- `[정적]` csrss 답 0x248B 중 `+8` 부터 0x240B 가 `gSharedInfo` (rva `0xda350`, 바로 뒤 `0xda590` 이 `gpsi`) 로 복사되고, 첫 8B 가 `gpsi` 가 된다. `DllMain` 이 `psi` 아래 읽는 칸은 `+0x0` · `+0x8bc` · 조건부 `+0x1b66` 셋이다.
- `[정적]` `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 user32 에서 `ClientThreadSetup` 의 서버 갈래 (`gfServerProcess ≠ 0`) 에서만 불린다. `gfServerProcess` 는 ntdll 이 PE 서브시스템이 1 (NATIVE) 일 때만 1 로 쓴다.
- `[정적]` user32 코드는 TEB `+0x78` (`Win32ThreadInfo`) 를 쓰지 않는다 (`_UserClientDllInitialize` · `ClientThreadSetup` 에 쓰기 줄 없음, 결과서). `__ClientThreadSetup` 은 `apfnDispatch` 의 84번째 칸에 있고 끝에서 `NtCallbackReturn` 으로 꼬리 점프한다 — 커널 콜백이다.
- `[정적]` user32 `+0x3fc` 가 PEB `+0x58` (`KernelCallbackTable`) 에 `apfnDispatch` (0x498B 표) 를 넣고, `+0x409` 가 PEB `+0x230` (`PostProcessInitRoutine`) 을 0 으로, `+0xae` 가 PEB `+0x7d0` (`Win32ClientProcInfo`) 를 0 으로 둔다.
- `[소스]` + `[정적+샘플]` + `[관찰]` **test.exe 는 user32 를 안 올린다**: `sample/log.h:3` "user32(wsprintf)도 안 쓴다", import 표 test.exe `dll1` · `KERNEL32` / dll1 `dll2` · `KERNEL32` / dll2 `KERNEL32` (flow-writer `pefile`), run44 P2 모듈 6개 (`test.exe` · `ntdll` · `KERNEL32` · `KERNELBASE` · `dll1` · `dll2`, `ms/logs/04_baggage-read_P2.txt:3398 · 3503 · 3608 · 3713 · 3818 · 3923`). 그래서 이 길은 흐름도 범위 (test.exe) 밖이고 그림엔 안 넣는다.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **`__ClientThreadSetup` 은 `NtCallbackReturn` 을 call 이 아니라 꼬리 `JMP` (`+0x20`) 로 부른다** (flow-writer `.asm`). 셋째 인자 (상태) 는 `+0x9 NEG EAX` · `+0xb SBB` · `+0x10 NOT` · `+0x15 AND 0xc0000001` 로 `ClientThreadSetup` 이 TRUE 면 0, FALSE 면 `0xc0000001`. 첫 두 인자는 0 (`+0xe` · `+0x13`). 결과서 "끝에서 `NtCallbackReturn` 을 부른다" 와 뜻은 같고 꼴만 붙였다. IAT `0xb5ab0` = `ntdll!NtCallbackReturn` (`pefile`).
- ② **`CsrClientCallServer` 의 포인터 옮기기** (flow-writer `.asm`): 캡처 버퍼가 있으면 보내기 전에 메시지 `+0x28` ← 캡처 버퍼 + `CsrPortMemoryRemoteDelta` (`+0x81 ~ +0x90`), 캡처 버퍼 안 포인터 목록마다 가리키는 칸에 delta 를 더하고 그 칸 위치를 메시지 기준 거리로 바꿔 둔다 (`+0x9f ~ +0xb9`). 돌아오면 거꾸로 (`+0xfa ~ +0x12a`). 곧 csrss 는 자기 쪽 주소를 받는다. 엔진이 답을 쓸 때 쓸 정보라 위 결론에 넣었다. `CsrAllocateMessagePointer` 는 캡처 버퍼 안 빈 자리를 4B 맞춤으로 잘라 주고 그 포인터 칸 위치를 목록에 적는다 (`+0x4 ~ +0x29`).
- ③ **차례 표 16번 "syscall 없음" 은 user32 직접만이다**. TraceLogging 등록 셋 (`+0x5e5` · `+0x5f1` · `+0x5fd`, user32 의 `TraceLoggingRegisterEx_EventRegister_EventSetInformation`) 이 ntdll `EtwEventRegister` · `EtwEventSetInformation` 을 거친다면 등록마다 `NtTraceControl(0xf)` (+ 트레잇 `0x1e`) 이 나간다 — run62 (ntdll 등록 B · C · D 하나씩 `0xf`) · run68 (kernelbase 등록 `0xf` · `0x1e`) 꼴 `[추측: user32 쪽 사슬은 안 봄]`. 최소 조건 표 (run86) 에서 엔진이 대답할 목록에 넣을 후보.
- ④ **test.exe 사진 대조** (flow-writer, run44 덤프 읽기 로그):
  - PEB `SessionId` 1 · `SharedData` 널 (`ms/logs/04_baggage-read_P1a.txt:1798 · 1764`, `_P2.txt:2508 · 2474`) `[관찰]` → user32 를 같은 프로세스에 올렸다면 표 5 · 6 은 `1 ≠ 0` 이라 `\Sessions\1\Windows` 갈래 `[정적 갈래 + 관찰 값]` — run68 kernelbase 연결과 같은 이름.
  - user32 가 놓을 칸이 test.exe 에선 모두 비었다 `[관찰]`: PEB `KernelCallbackTable` · `PostProcessInitRoutine` · `Win32ClientProcInfo` (null), TEB `Win32ThreadInfo` (null) · `Win32ClientInfo` (`[62] 0`) — P1a ~ P2 (`_P2.txt:2464 · 2505 · 2554 · 2694 · 2726`, `_P1a.txt:1754 · 1795 · 1844 · 1984 · 2016`).
  - ntdll `.mrdata` `NtUserPfn` (`0x1e7130 ~ 0x1e733f`, 66칸) 은 P1a · P2 모두 `0x7ffafb797040` = ntdll 시작 (`0x7ffafb660000`, `.mrdata` `0x7ffafb847000` − `0x1e7000`) + **`0x137040` = `UninitUser32Proc`**, 플래그 `0x1e7340` 은 0 (`_P1a.txt:5766 ~ 5799`, `_P2.txt:6896 ~ 6929`) `[관찰]`. ntdll 파일의 같은 자리도 66칸 모두 `0x180137040`, 플래그 0 이다 (flow-writer `objdump -s`). 곧 **test.exe 에선 아무도 안 쓰고 파일 처음 값 (재배치) 그대로** — `RtlInitializeNtUserPfn` (user32 가 부를 때만) 도, `RtlResetNtUserPfn` (플래그 ≠ 0 일 때만 `UninitUser32Proc` 로 되채움, `+0x8 ~ +0x5f`) 도 안 탔다. 짐 지도 7.3 의 `NtUserPfn` `≤P1a` 가 이것 (짐 지도 7.5 에 실마리로).
- ⑤ **user32 · win32u · gdi32 · gdi32full 은 `\KnownDlls` 섹션에 있다** `[관찰]` (run44 `\KnownDlls` 목록, `ms/logs/04_baggage-read_P2.txt:1387 · 1396 · 1408 · 1399`). user32 를 import 하는 프로세스는 2.6 의 KnownDll 길로 올릴 것이다 `[추측: 실행은 안 봄]`. PE_emulator 가 KnownDll 섹션을 흉내 낸다면 이 넷이 필요하다.
- ⑥ **user32 는 GDI32 를 정적으로 가져온다** (flow-writer `pefile`: IAT `0xb4fe0 GdiDllInitialize` · `0xb4fd8 GdiProcessSetup`, 둘 다 `GDI32.dll`) `[정적: PE 표]`. 그러니 gdi32 는 user32 `DllMain` 전에 이미 올라와 있다 `[추측: 2.7 초기화 순서 — 의존하는 쪽이 먼저]`. gdi32 의 몸 · gdi32full 지연 가져오기는 run86.
- `_calls.tsv` 의 `alias:` callee (`InitializeUMHandleList+1` 등 — 같은 이름 C++ 꾸민 이름 하나) 는 `pdb_aliases.py --lookup` 으로 별칭 무리가 아님을 확인하고 이름을 그대로 썼다. ntdll `alias:RtlCopyMemory+3` (같은 주소 이름 3개, 후보 `RtlMoveMemory` · `memcpy` · `memmove`) 은 "메모리 복사" 로 적었다.

## 정정

- 이전 run 이 틀린 것은 없다. run68 의 "정상이면 `CsrpClientConnectToServer` (`+0xb5/+0xba`) 는 안 탄다 — csrss 응답 상태가 음수일 때만" 은 **첫 연결 (kernelbase) 때** 이야기로 맞다. 이번에 같은 줄이 **이미 연결된 둘째 연결 (user32) 의 보통 길** 이기도 하다는 것을 더했다 (flow.md 2.5 run68 절에 한 줄).
- **결과서 sast/82 의 Q150b 쪽 짐작 가운데 둘은 run86 (sast/83) 에서 정정** — 이 문서엔 틀린 채로 옮기지 않았다:
  - "`GdiDllInitialize` 가 gdi32 `DllMain` 과 user32 `DllMain` 에서 두 번 (다른 `hmod`) 불린다" — 틀림 (run86).
  - "`NtGdiInit` (`0x12ed`) 이 gdi32 쪽 첫 win32k syscall 후보" — 틀림 (run86).

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 늘리지 않음 — test.exe 는 user32 를 안 올린다): 기준 (user32 · win32u pdb 두 줄 새로, ntdll · kernelbase pdb 줄에 run85 몫, 근거 로그 run85 줄) · 2.5 머리 run 목록 · run68 csrss 연결 글머리에 한 줄 (둘째 연결 길) · **3장 새 절 "user32 시작 함수"** · 3장 콘솔 절 새 샘플 → Q160 · 4장 (run84 줄 → Q159 · Q160, run85 새 줄 셋) · 5장 (run 표 · 03_sprint 요청서 번호, run84 줄 Q159 · Q160).
- 짐 지도 (짐 지도 바퀴 — **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**): 기준 run85 줄 · 판 목록 (마흔한째판) · 3.3 TEB "범위 밖 길 — 다른 DLL 39" 줄에 user32 `Win32ClientInfo` 쓰는 줄 · `Win32ThreadInfo` 는 user32 아님 · 7.2 `_PEB` `끝까지 0` 에 user32 가 쓰는 PEB 칸 넷 메모 · 7.3 `NtUserPfn` 가리킴 · 7.5 `PostProcessInitRoutine` 줄 덧붙임 + run85 실마리 (`NtUserPfn` = 파일 처음 값) · 8장 두 표.
- 뒷정리 (run84): run84 문서 · flow.md 4장 · 5장 run84 줄의 "정적 남은 것" → 대기열 **Q159**, "새 샘플 필요" (4 · 5 · 6 일부 · 7) → **Q160**.

## 아직 확인 필요

동적 — 결과서 "동적분석으로 확인해야 할 것" 번호 그대로, ~~모두 **Q104** 로~~ → 모두 대기열 **Q162** (커밋 전 붙음 — test.exe 로 못 보니 Q104 가 아니라 따로, run84 의 Q160 과 같은 꼴. notepad 류 / 새 GUI 샘플은 사용자 결정) (이번 묶음은 VM 안 함). **test.exe 로는 못 본다** (user32 안 올림 `[소스]`). 무엇으로 볼 수 있나를 앞에 붙였다:

- 1 **[엔진 쪽 — PE_emulator]** user32 `DllMain` 시점의 ntdll `CsrPortHandle` (rva `0x1d4e20`) 이 0 인가, kernelbase 시작 함수의 csrss 연결 (`_KernelBaseBaseDllInitialize +0x2d2`) 을 탔나, 탔다면 `NtConnectPort` 가 출력 칸을 채웠나 (위 "PE_emulator 가 본 `0xc000003a`" 절). **[VM — user32 를 올리는 프로세스]** 같은 값을 `user32!_UserClientDllInitialize+0x2aa` 에서 `dq ntdll+0x1d4e20 L1` (0 이 아니어야).
- 2 **[VM — user32 를 올리는 프로세스]** `NtAlpcSendWaitReceivePort` 가 받은 실제 답: 메시지 `+0x34`, 캡처 버퍼 0x248B 안 `psi` 값과 그 아래 `+0x0` · `+0x8bc` · `+0x1b66`. 멈춤점 `ntdll!CsrpClientConnectToServer+0x9a` (복사 직후).
- 3 같은 프로세스에서 `user32!gfServerProcess` 0 인지, `gSharedInfo` 0x240B 덤프.
- 4 `psi+0x8bc` 비트 `0x20` 이 켜져 있나 → `InitProcessDpiAwareness` 가 win32k syscall (`0x1576` · `0x1440`) 을 부르나.
- 5 그 프로세스의 첫 win32k syscall 이 `DllMain` 안인지 (gdi32 쪽 또는 DPI 쪽), `__ClientThreadSetup` 이 `KernelCallbackTable[84]` 로 불리는지, TEB `+0x78` 이 언제 채워지나. 멈춤점 `user32!ClientThreadSetup` 입구 · `nt!PsConvertToGuiThread` (그 프로세스 조건).
- 6 `gdi32full` 이 user32 `DllMain` 시점에 이미 올라와 있나 → run86 과 같이.
- 7 `g_systemCallFilterId` 가 5 로 바뀌나 (`+0x46a` 직후 `v & 0xf`).
- 8 UserModeMenus WIL 기능이 켜졌나 — 켜지면 PEB `+0x7d0` 채움 · `NtUserInitializeUserModeMenus`.
- (flow-writer 더함) user32 TraceLogging 등록 셋이 `NtTraceControl` 을 몇 번 부르나 (위 "보탠 것" ③).
- 볼 프로세스는 새 샘플 (user32 를 import 만 하는 콘솔 exe) 또는 이미 있는 GUI 프로그램 (예: notepad) 둘 다 된다 `[추측]`. 고르는 건 사용자 결정 (Q160 의 `/MD` 샘플과 묶을 수도).

정적 — 다음 바퀴 후보 (~~대기열 번호는 오케스트레이터가 붙임~~ → ① ~ ⑦ 모두 대기열 **Q161**). 결과서 "다음 요청 후보" 1 ~ 5 + flow-writer 더함:

- ① `SHAREDINFO` · `SERVERINFO` 칸 이름 — win32kbase · win32kfull 의 `NtUserProcessConnect` 구현 쪽을 보면 `psi` 를 어떻게 채우는지 (`+0x0` · `+0x8bc` · `+0x1b66` 의 뜻). "win32k 안쪽 깊이" 라 사용자 결정.
- ② csrss 가 USERSRV 연결 (`CsrSrvClientConnect`, ApiNumber 0, 서버 3) 답을 만드는 쪽 — `csrsrv` 는 run71 에서 봤으니 `CsrSrvClientConnect` 와 USERSRV 연결 함수만 (winsrv).
- ③ 창을 안 만드는 프로세스가 흔히 부르는 user32 API (`GetSystemMetrics` · `MessageBox` · `GetDpiForSystem` · `GetKeyboardLayout` 등) 가 읽는 `psi` 칸 (`ms/disassemble/user32/_refs/0xda590.tsv` 368줄).
- ④ `ClientThreadSetup` 의 `InitClientDrawing` · `LoadAppInitDlls` · `NtUserRegisterLPK` 깊이.
- ⑤ `InitializeLpkHooks` (rva `0x501e0`) 가 LPK 사용 표시 (`DAT_1800dac34`) 를 언제 정하나 — `NtUserRegisterLPK` 호출 조건.
- ⑥ (flow-writer 더함) 첫 연결 길에서 서버 3 연결 정보 0x248B 가 어떻게 가나 — `CsrpConnectToServer` 의 연결 블록 (`0x30B`, run68) 에 실리나, 아니면 `+0x11c` 응답 상태 음수로 `+0xad` → `CsrpClientConnectToServer` 를 또 타나. PE_emulator 가 kernelbase 연결을 건너뛴 경우 (위 후보 ①) 에만 의미.
- ⑦ (flow-writer 더함) user32 `TraceLoggingRegisterEx_EventRegister_EventSetInformation` 사슬 (`NtTraceControl` 몇 번) · `GetProcessMitigationPolicy(0xb)` 가 kernelbase 안에서 부르는 syscall (`[추측: NtQueryInformationProcess]`) — run86 최소 조건 표의 "엔진이 대답할 syscall" 목록을 닫으려면.

짐 지도 결정 (오케스트레이터 · 사용자): 7.5 의 `NtUserPfn` (파일 처음 값 · test.exe 에선 아무도 안 씀) 을 3.9 로 옮겨 2장 `.mrdata` 모름 26 → 25 로 셀지, PEB 의 user32 칸 (`KernelCallbackTable` · `PostProcessInitRoutine` · `Win32ClientProcInfo`) 을 TEB 처럼 "범위 밖 길 — 다른 DLL" 로 셀지.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음. 새 함정 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 user32 줄기에서 디버거 여부 (`BeingDebugged` · `KdDebuggerEnabled`) 를 보는 줄은 결과서 · flow-writer 가 읽은 줄 안에선 없었다 `[정적: 읽은 줄 안에서]`.
- 도구 (결과서): Ghidra 가 user32 의 `CsrClientConnectToServer` import 서명을 인자 4개로 잘못 붙여 디컴파일엔 다섯째 인자 (`&gfServerProcess`) 가 안 보인다 → 인자는 `.asm` 으로 읽었다. user32 · win32u PDB 에 타입이 없어 (`pdb_struct.py --list` 0개) 칸 이름은 모두 `[추측]`. kernelbase 프로젝트는 다른 decompiler 가 쓰는 중이라 안 열었다 (`objdump` 만).

# 2026-11-23 (작성일, 정적 분석 2026-11-23) run136 — 창 스테이션 · 데스크톱 이름 묻기 (Q250, 정적만): combase `GetThreadWinstaDesktop` 은 `GetProcessWindowStation` → `GetThreadDesktop` → `GetUserObjectInformationW(핸들, 2 = 이름, 0x40 B, 0x40, &len)` 을 **창 스테이션 · 데스크톱에 한 번씩** 묻고, 답 둘을 `<창 스테이션>\<데스크톱>` 글로 이어 RPCSS 연결 (`Connect`) 의 둘째 인자로 넘긴다. user32 `GetUserObjectInformationW` 는 6 B 썽크라 커널 답이 그대로 온다. **에뮬레이터 막힘 = 이름 묻기가 FALSE 인데 LastError 가 0 이고 글이 아직 NULL → combase 가 `0x8007000e` 를 만들어 `CoInitializeSecurity` 까지 그대로 돌려준다.** 커널 진짜 답은 win32kfull `_GetUserObjectInformation` — 이름은 UTF-16 · 끝 NUL 포함, 필요 길이 = 바이트 (`Length + 2`), 모자라면 FALSE + LastError `0x7a` + 필요 길이는 씀

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("창 스테이션 · 데스크톱 이름 묻기 (`GetUserObjectInformationW` 종류 2 · 1) 의 출력 꼴과, combase 가 그 답을 쓰는 법", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/324_internals-winsta-name.md`, 튜닝 `324_tune14.md` T1, 샘플 489 · 병목 120 · `CoInitializeSecurity` `0x8007000e` 63) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 · combase 를 안 올린다 — 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run131 과 같은 줄), test.exe 의 `_EPROCESS.Win32WindowStation` 은 K1 ~ P2 내내 NULL `[관찰]` (`ms/logs/04_baggage-read_P2.txt:151`, 다른 일곱 장도 같은 칸), `sample/*.c` 에 COM · 창 API 가 없다 `[소스]` (`sample/log.h:3`). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (user32 · win32u · combase 줄, win32kbase 줄 고침 · win32kfull 새 줄, 근거 줄) · 3장 run85 절 한 줄 · run131 절 한 줄 · 3장 끝 새 절 · 4장 (run86 줄 ① ② 에 한 마디 · run131 정적 4 에 한 마디 · Q250 닫힘 줄 · run136 줄 셋) · 5장 (run136 두 줄) 만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q250** (사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 넷째). 결과서 판정 **분석성공** — 질문 1 · 2 · 3 모두 줄 근거로 답함. 질문 3 은 요청서 예상과 달리 win32kbase 하나로 안 닫혀 **win32kfull 도 처음 analyze** (본문이 함수 표 너머에 있음).
> - 이어 쓰는 앞 run: **run131** (combase 를 처음 연 바퀴 — `RegisterOleWndClass` 의 "`GetLastError` 0 → `0x8007000e`" 꼴, 같은 꼴이 이번에도 나옴), **run85** (user32 `DllMain` 이 PEB `+0x58` `KernelCallbackTable` 을 놓는 줄 — 이번에 combase 가 그 칸을 읽음), **run129 · run133** (rpcrt4 `\RPC Control\epmapper` 연결 · LRPC 선 위 형식 — 이번 글이 가는 `Connect` 다음 몫), **run44** (`DesktopInfo` = `Winsta0\Default` `[관찰]`).
> - 남은 동적 → **Q256 (동적: 창 스테이션·데스크톱 이름 대소문자·핸들 값·종류 1 dwFlags/fInherit, 에뮬레이터 PEB+0x58)**, 남은 정적 → **Q257 (정적: combase `Connect` rva 0x12f318 과 CreateFromBinding* → epmapper · NtUserGetProcessWindowStation/NtUserGetThreadDesktop 커널 본문 · GetConnection +0xc5 E_ACCESSDENIED 갈래)**.

## 요약

- **user32 는 끼어들지 않는다** `[정적]`: `GetUserObjectInformationW` = `JMP [0xb6d90]` 한 줄 (6 B), 그 칸 = win32u `NtUserGetObjectInformation` (`0x1065`). `GetProcessWindowStation` · `GetThreadDesktop` 도 같은 꼴의 썽크. 반환값도 LastError 도 user32 가 안 건드린다.
  - `GetUserObjectInformationA` 는 종류 2 · 3 일 때만 끼어든다: 임시 버퍼 `nLength × 2` 에 UTF-16 으로 받고, 성공이면 NUL 까지 세어 ANSI 로 바꾸고 필요 길이를 반으로. 실패면 그대로 0. **combase 는 A 를 안 부른다** — 이번 길과 무관.
- **combase 가 묻는 차례** `[정적]` (`GetThreadWinstaDesktop`, rva `0x82330`):
  1. 정해 둔 글 (`g_explicitWinstaDesktopString`) 이 있으면 그것으로 끝.
  2. user32 가 없거나 PEB `+0x58` (`KernelCallbackTable`) 가 0 이면 핸들 없이 `ResolveProcessDesktopNoUser` (매개변수 `DesktopInfo` 로).
  3. 아니면 `GetProcessWindowStation` → `GetThreadDesktop(GetCurrentThreadId())` → `GetUserObjectInformationW(창 스테이션, 2, 0x40 B, 0x40, &len)` → `GetUserObjectInformationW(데스크톱, 2, 0x40 B, 0x40, &len)`.
  4. 각 묻기가 FALSE 이고 `GetLastError() == 0x7a` 면 `len` 만큼 힙을 얻어 **그 묻기만 한 번 더**.
  5. 성공이면 `<창 스테이션>\<데스크톱>` (예 `WinSta0\Default`) 을 힙에 만들어 전역 `CRpcResolver::_pwszWinstaDesktop` (rva `0x338118`) 에 둔다.
- **글이 가는 곳** `[정적]`: `CoInitializeSecurity` → `GetConnection` → 람다 `operator()` → `+0x742` 에서 그 전역을 읽어 → `+0x82e Connect` (RPCSS 쪽 RPC 클라이언트 스텁) 의 **둘째 인자**. `Connect` 안은 안 읽음 (→ Q257).
- **실패 값** `[정적]`: `GetLastError` 를 HRESULT 로 바꾼다 (양수 → `0x80070000 | (값 & 0xffff)`, 음수 → 그대로). 단 **이름 묻기 뒤 출구** 는 오류값이 0 인데 글이 NULL 이면 **`0xe` → `0x8007000e`** 로 바꾼다. 핸들 묻기 뒤 출구는 그 검사를 건너뛰어 **0 (성공) + 글 NULL** 로 끝난다.
- **커널 답 (win32kfull)** `[정적]`:
  - 종류 2 (이름) = 객체 이름 끝 조각 (`WinSta0` · `Default`) UTF-16 + `0x0000`. 필요 길이 = `Length + 2` 바이트 (둘 다 16).
  - 모자라면 FALSE + LastError `0x7a` + 필요 길이는 씀 · 버퍼는 안 씀.
  - 종류 1 (12 B) = `{fInherit, 0, dwFlags}` — 창 스테이션 `dwFlags` = 1 (객체 `+0x40` 비트 `0x4` 가 꺼져 있을 때). 필요 길이 늘 `0xc`.
  - LastError 는 **커널이 TEB `+0x68` 에 직접 쓴다** (`UserSetLastError +0x21`).
- **에뮬레이터 막힘 한 줄** `[정적]` (요청 쪽 로그와 줄이 맞음): stub 이 "반환 0 · 버퍼 안 씀 · LastError 안 씀" → combase `+0xd6 JZ` → `GetLastError() = 0` → `+0x1f2 JZ` → `+0x238 MOV EBX,0xe` → **`0x8007000e`** → `Connect` 를 안 부르고 `CoInitializeSecurity` 가 그 값으로 실패.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 아흔넷째 바퀴. plan **Q250**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze 둘** — win32kbase (약 3분) · win32kfull (약 2분), 둘 다 MAXMEM 10G, 한 번에 하나. 시작 때 `free -g` available 27 ~ 28 GB, analyze 뒤 24 ~ 25 GB (결과서). user32 · combase · win32u 는 다시 analyze 안 하고 `^이름$` export 만.

| 모듈 | 바이너리 (`System32\`) | PDB guid+age | 상태 |
|---|---|---|---|
| user32 | `user32.dll` (run85 때 analyze) | `CDB947D1B96B87977915DE04CF54279C1` | export 4 (썽크 셋 + A) |
| combase | `combase.dll` (run131 때 analyze) | `DA1DC0C4F35CEE58F672CBB20460D11D1` | export 3 + depth 1 (17개 중 14) · refs 8 · 일회용 objdump 셋 |
| win32u | `win32u.dll` (run85 때 analyze) | `6295787D0B7E537E98F77F31F4426D1C1` | 스텁 둘 새로 (`NtUserGetObjectInformation` · `NtUserGetProcessWindowStation`) |
| win32kbase | `win32kbase.sys` 3,420,160 B | **`D22DFDA3D54B87E6BD0567A57877904C1`** | **처음 analyze** (run86 은 `objdump` 만) — `_index.tsv` 9,144줄 · `FUN_` 0 · `SetPdb:` 줄 있음 |
| win32kfull | `win32kfull.sys` 4,390,912 B | **`5CD57181BCFDC5AE8F4819BEB1196F091`** | **처음 analyze** — `_index.tsv` 10,099줄 (`FUN_` 1) · `SetPdb:` 줄 있음 |

- win32k PDB GUID 는 `ms/sym/<모듈>.pdb.info.txt` 와 `fetch_pdb.py` 가 같은 값. 둘 다 공개 심볼이라 **구조체 타입이 없다** (`pdb_struct.py --list` 에 `USEROBJECTFLAGS` · `WINDOWSTATION` 0개) → 커널 객체 칸은 "코드가 읽는 오프셋" 으로만, 칸 이름은 공개 문서 이름 `[추측]`.
- 버전 대조 (결과서):
  - 에뮬레이터 로그의 syscall 번호 ↔ win32u 스텁 `MOV EAX,imm` — `0x1065` · `0x101f` · `0x107b` 같음.
  - user32 썽크 칸 ↔ `llvm-readobj --coff-imports`: `0xb6d90` = `NtUserGetObjectInformation` · `0xb6db8` = `NtUserGetThreadDesktop` · `0xb6e00` = `NtUserGetProcessWindowStation`.
  - combase 지연 가져오기 표 (`ext-ms-win-ntuser-windowstation-l1-1-0`, 시작 `0x363568`) ↔ `.asm` 의 `CALL [0x180363568/570/578]` 같음. `apisetschema.dll` 은 `…-l1-1-2 → user32.dll` (`l1-1-0` 요청이 `l1-1-2` 로 풀리는 허용은 `[추측]` — run131 과 같은 자리).
  - PDB `_PEB +0x58` = `KernelCallbackTable`, `_RTL_USER_PROCESS_PARAMETERS +0xc0` = `DesktopInfo` — combase 가 읽는 칸과 같음.
  - win32kbase 가 부르는 함수 표 칸 `+0x388` · `+0x390` ↔ win32kfull `ext_ms_win_moderncore_win32k_base_ntuser_l1_table` (rva `0x3b4c70`) 의 같은 칸 (`refs` `ptr` 줄 + 표 바이트).
  - 에뮬레이터가 본 `0x8007000e` · `0x8007007a` 둘 다 `.asm` 줄에서 그대로 나온다 — 오프셋 셈 · 바이너리를 믿어도 된다는 근거이지 진짜 Windows `[관찰]` 은 아니다.

## 목적

- PE_emulator 에서 샘플 489 개가 같은 자리를 지난다 (요청 쪽 에뮬레이터 로그 r14 — 진짜 Windows 관찰 아님): `NtUserGetProcessWindowStation → 0x20000` → `NtUserGetThreadDesktop → 0x20400` → `NtUserGetObjectInformation(0x20000, 2, buf, 0x40)` 기본 대답 0 (버퍼 안 씀) → 끝 글 "Exception.ToString()이 실패" → 종료. 요청서 `_workspace/sast/134_winsta-name.md`:
  1. user32 `GetUserObjectInformationW` 가 정말 썽크인가, `A` 의 인자 차림 · 돌아온 뒤 읽는 것 · LastError 를 누가 정하나.
  2. combase `GetThreadWinstaDesktop` — 묻는 차례 · 버퍼 모자랄 때 · 실패 HRESULT · 만드는 글 · 핸들 0 일 때 · 부르는 쪽이 실패를 받으면.
  3. (되면) win32k 쪽 종류 1 · 2 출력 칸과 모자랄 때 반환 · LastError.
- 쓰는 곳: PE_emulator T1 — 새 몸 `src/gui/nt_user_get_object_information.cpp` (종류 2 · 1 의 출력 · 길이 · 반환), `nt_user_get_process_window_station.cpp` · `nt_user_get_thread_desktop.cpp` 의 `TODO(VM)` 줄.

## 방법

- 정적 sast/134:
  - user32 `^이름$` export 4 (`GetUserObjectInformationW` · `GetUserObjectInformationA` · `GetProcessWindowStation` · `GetThreadDesktop`), refs `0xb6d90`.
  - win32u 스텁 둘 (depth 0). `NtUserGetThreadDesktop` 은 전부터 있음.
  - combase `GetThreadWinstaDesktop` · `ResolveProcessDesktopNoUser` · `SetExplicitWinstaDesktopString` + depth 1. refs 8 (`0x338118` · `0x338580` · `0x82330` · `0x82a04` · `0x363578` · `0x18b9c0` · `0xa14d0` · `0x12f318`).
  - **일회용 objdump 셋** (`ms/disassemble/combase/_oneoff/`: `operator_0x18b9c0` · `GetConnection_0xa14d0` · `CoInitializeSecurity_0x7e880`) — 부르는 쪽 람다 이름이 `operator()` 라 combase 에 같은 이름 70개, `export` 로 하나만 못 뽑음. 오프셋은 결과서가 함수 시작을 직접 빼서 적음.
  - win32kbase · win32kfull 처음 analyze → export (win32kbase 3 · win32kfull 8), refs (`EditionGetUserObjectInformationEntryPoint` · `IsEditionGetUserObjectInformationEntryPointSupported` · `0x36fee8` · `0x36f1c0`).
  - 일회용 읽기 (파일 안 남김): combase `.rdata` UTF-16 글 (`0x2cb830` `\` · `0x2c24e0` `WinSta0\Default` · `0x2791a0` `uszDefaultDesktopName`), `apisetschema.dll`, win32kfull 함수 표 칸 값, win32k 세 파일의 `winsta0` 글.
- 결과서: `_workspace/sast/134_winsta-name_result.md` (**분석성공**, 스크립트 문제 없음, 제안 1 — 아래 도구 메모).
- flow-writer 가 다시 본 것:
  - `.asm` · objdump 줄: user32 `GetUserObjectInformationW` · `GetProcessWindowStation` · `GetThreadDesktop` · `GetUserObjectInformationA` 전부, win32u 스텁 둘, combase `GetThreadWinstaDesktop` (본체 · 조각 `FUN_1800824e3` · 떨어진 범위 `+0x1d8eb6 ~` · 주인 없는 덩어리 여섯) · `ResolveProcessDesktopNoUser` · `SetExplicitWinstaDesktopString` 전부, `GetConnection +0xbe ~ +0xf8`, `operator() +0x80 ~ +0x123` · `+0x742 ~ +0x833` (`CreateFromBindingString +0x33c` · `CreateFromBindingHandle` 셋 자리도), `CoInitializeSecurity +0x37` · `+0x7a` · `+0x87` · `+0x9b ~ +0xac` · `+0x47d ~ +0x486` · `+0x89c` · `+0x8a9` · `+0x8fe ~ +0x905`, win32kbase 둘 전부, win32kfull `EditionGetUserObjectInformationEntryPoint` 전부 · `_GetUserObjectInformation +0x56 ~ +0x3de` · `UserSetLastError` 전부.
  - `_calls.tsv` (user32 · win32kbase · win32kfull) call · ret 짝, refs 표 (`0x363578` · `0x82330` · `0x18b9c0` · `0x12f318` · `0x338118` · win32kfull 둘).
  - `pdb_aliases.py --lookup`: combase `0x179470` (`IsGetProcessWindowStationPresent`) · `0x82a04` · `0x174934` · `0x18ce4c`, win32kfull `0xbfc60` · `0xc119c` · `0xc043c`, win32kbase `0x12cc64` 은 별칭 무리 아님 → PDB 이름 그대로. 무리인 것: combase `0x1466e4` (추적 글 남기기, 이름 3개 — `ComTraceMessageT` K · H · I 판) · `0x170160` (`FailFast_Unexpected` 2개), win32kfull `0x35c6c0` (`memcpy` · `memmove`) · `0x1ed1d0` (**0 을 돌려주는 공용 조각**, 이름 641개 — 후보 `IsEditionGetUserObjectInformationEntryPointSupported` · `Is_GetProcessWindowStationSupported` · `IsEditionGetThreadDesktopEntryPointSupported` …).
  - run44 덤프 읽기 로그: `DesktopInfo` (`ms/logs/04_baggage-read_P1a.txt:2533`, `_P2.txt:3243`), PEB `KernelCallbackTable` (`_P1a.txt:1754`, `_P2.txt:2464`), `_EPROCESS.Win32WindowStation` (`_P2.txt:151`, K1 · K2 · K3 · P0 · P1 · P1a · P1b 도 NULL).
  - 결과서와 다른 곳 → 아래 "flow-writer 대조".

## 결과

비유:

- **창 스테이션 = 건물, 데스크톱 = 그 건물 안의 방.** combase 가 RPCSS (COM 관리실) 에 전화할 때 "저는 `WinSta0` 건물 `Default` 방에서 걸어요" 라는 주소를 붙인다.
- 주소를 알려고 경비실 (커널) 에 "이 건물 이름이 뭐예요?" 하고 묻는다. 경비실은 이름을 적어 주거나, 종이가 작으면 "몇 칸 필요해요" 와 함께 "종이 모자람 (`0x7a`)" 쪽지를 준다.
- 에뮬레이터 경비실은 **"몰라요" 만 하고 쪽지 (LastError) 를 빈칸으로** 줬다. combase 는 "이유도 없이 안 됐네 — 메모리가 모자랐나 보다 (`0x8007000e`)" 로 적고 **전화를 포기**한다 `[추측: 비유]`.
- 반대로 **건물 열쇠 자체가 없으면** (핸들 0 + 쪽지 빈칸) combase 는 "주소 없이" 그냥 전화한다 — r13 판이 이 길이다.

### 정적 — 호출 지점 (`ms/disassemble/{user32,win32kbase,win32kfull}/_calls.tsv`, combase `.asm` · `_oneoff/`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `combase!CoInitializeSecurity` (rva `0x7e880`) | `+0x47d` | `+0x482` | `CRpcResolver::GetConnection` (rva `0xa14d0`) — `gGotSecurityData` 가 0 일 때 (`+0xa6 CMP [0x180338290],EDI` · `+0xac JE`, EDI = `+0x37 XOR EDI,EDI`) | [정적] (objdump) |
| 〃 | `+0x905` | `+0x90a` | `SetExplicitWinstaDesktopString(L"WinSta0\Default")` — `dwCapabilities` 비트 14 (`0x4000`) 일 때만 (`+0x9b BT R12D,14` · `+0xa0 JB +0x8fe`) | [정적] (objdump) |
| `combase!GetConnection` | `+0xbe` | `+0xc3` | 람다 `operator()` (rva `0x18b9c0`, 3,558 B) | [정적] (objdump) |
| `combase!operator()` (`0x18b9c0`) | **`+0x94`** | **`+0x99`** | `CRpcResolver::GetThreadWinstaDesktop` — `_pwszWinstaDesktop` 가 NULL 일 때만 (`+0x80` · `+0x92 JNE +0x100`) | [정적] (objdump) |
| 〃 | `+0xb5` | `+0xba` | `Return_Hr` (실패 기록, 그 뒤 반환) | [정적] (objdump) |
| 〃 | `+0x123` | `+0x129` | `GetModuleFileNameW(NULL, buf, 519)` (성공 갈래 `+0x100 ~`) | [정적] (objdump) |
| 〃 | **`+0x82e`** | **`+0x833`** | **`Connect`** (rva `0x12f318`) — RDX = `_pwszWinstaDesktop` (`+0x742`) | [정적] (objdump) |
| `combase!GetThreadWinstaDesktop` (rva `0x82330`, 909 B) | `+0x44` | `+0x49` | `IsGetProcessWindowStationPresent` (rva `0x179470`) | [정적] |
| 〃 | `+0x27e` | `+0x283` | `ResolveProcessDesktopNoUser` (rva `0x82a04`) — ② 갈래 | [정적] |
| 〃 | **`+0x64`** | **`+0x6b`** | `GetProcessWindowStation` (지연 칸 `0x180363570`) | [정적] |
| 〃 | `+0x7c` · **`+0x8a`** | `+0x83` · **`+0x91`** | `GetCurrentThreadId` · `GetThreadDesktop` (지연 칸 `0x180363568`) | [정적] |
| 〃 | **`+0xc8`** | **`+0xcf`** | `GetUserObjectInformationW` ① 창 스테이션 (지연 칸 `0x180363578`) | [정적] |
| 〃 | **`+0xf7`** | **`+0xfe`** | `GetUserObjectInformationW` ② 데스크톱 | [정적] |
| 〃 | `+0x358` · `+0x39d` | `+0x35f` · `+0x3a4` | `GetLastError` (① · ② 실패 뒤) | [정적] |
| 〃 | `+0x1d9014` · `+0x1d9044` · `+0x1d9054` | `+0x1d901b` · `+0x1d904b` · `+0x1d905b` | ① 다시: `HeapAlloc(len)` · `GetUserObjectInformationW` · `GetLastError` (떨어진 범위) | [정적] |
| 〃 | `+0x1d90bb` · `+0x1d90eb` · `+0x1d90ff` | `+0x1d90c2` · `+0x1d90f2` · `+0x1d9106` | ② 다시: 같은 셋 | [정적] |
| 〃 | `+0x13a` | `+0x141` | `HeapAlloc(g_hHeap, 0, cch × 2)` (글) | [정적] |
| 〃 | `+0x169` · `+0x187` · `+0x1a1` | `+0x16e` · `+0x18c` · `+0x1a6` | `StringCchCopyW` · `StringCchCatW(L"\\")` · `StringCchCatW` | [정적] |
| 〃 | `+0x23f` · `+0x31c` | `+0x246` · `+0x323` | `GetLastError` (창 스테이션 핸들 0 · 데스크톱 핸들 0 뒤) | [정적] |
| 〃 | `+0x2b6` · `+0x3ee` · `+0x40b` | `+0x2bd` · `+0x3f5` · `+0x412` | `HeapFree` (글 합치기 실패 · 다시 묻기 버퍼 둘) | [정적] |
| 〃 | `+0x1d8ebd` · `+0x1d8ef1` · `+0x1d8f56` | `+0x1d8ec4` · `+0x1d8ef8` · `+0x1d8f5b` | `GetModuleHandleW(L"user32.dll")` · `GetProcessMitigationPolicy(…, 4, …)` · `DiagnoseUser32LoadFailure` (LastError `0x7f` 일 때만) | [정적] |
| 〃 | 여러 곳 | — | `BreakOnCoInitializeSecurityRuntimeFailureIfRequested` · `IsWppLevelEnabled` · 추적 글 남기기 (rva `0x1466e4` — 같은 주소 이름 3개, 후보: `ComTraceMessageT` K · H · I 판) | [정적] |
| `combase!ResolveProcessDesktopNoUser` | `+0x32` | `+0x37` | `IsGetProcessWindowStationPresent` | [정적] |
| 〃 | `+0x5e` | `+0x65` | `RtlEqualUnicodeString(&DesktopInfo, &uszDefaultDesktopName, TRUE)` | [정적] |
| 〃 | `+0xaf` · `+0xd5` | `+0xb6` · `+0xda` | `HeapAlloc(Length + 2)` · `memcpy` | [정적] |
| `combase!SetExplicitWinstaDesktopString` (rva `0x175114`) | `+0x5b` | `+0x60` | `RpcETooLate` (`gGotSecurityData ≠ 0`) | [정적] |
| 〃 | `+0x6c` · `+0x7c` · `+0x8f` · `+0xa3` | `+0x73` · `+0x81` · `+0x94` · `+0xa8` | `_wcsicmp` · `IsGetProcessWindowStationPresent` · `DuplicateStringPrivMem` · `FreeExplicitWinstaDesktopString` | [정적] |
| `user32!GetUserObjectInformationW` (rva `0x72820`, 6 B) | `+0x0` (JMP) | — (썽크) | `win32u!NtUserGetObjectInformation` (칸 `0xb6d90`) | [정적] |
| `user32!GetProcessWindowStation` (`0x72740`) · `GetThreadDesktop` (`0x727d0`) | `+0x0` (JMP) | — (썽크) | `NtUserGetProcessWindowStation` (`0xb6e00`) · `NtUserGetThreadDesktop` (`0xb6db8`) | [정적] |
| `user32!GetUserObjectInformationA` (rva `0x51fa0`, 274 B) | `+0x41` · `+0x69` · `+0xb1` · `+0x105` | `+0x48` · `+0x70` · `+0xb8` · `+0x10c` | `RtlAllocateHeap(pUserHeap, 8, 2 × nLength)` · `NtUserGetObjectInformation` (종류 2 · 3) · `RtlFreeHeap` · `RtlUnicodeToMultiByteN` | [정적] |
| 〃 | `+0xe4` | `+0xeb` | `NtUserGetObjectInformation` (그 밖 종류 — 인자 그대로) | [정적] |
| `win32u!NtUserGetObjectInformation` (rva `0x1f50`) | `+0x12` SYSCALL | — | 번호 `0x1065` | [정적] |
| `win32u!NtUserGetProcessWindowStation` (rva `0x1690`) | `+0x12` SYSCALL | — | 번호 `0x101f` | [정적] |
| `win32kbase!NtUserGetObjectInformation` (rva `0x1223b0`, 135 B) | `+0x26` | `+0x2d` | `W32GetWin32kBaseApiSetTable` (win32k.sys 가 내보냄) | [정적] |
| 〃 | `+0x42` | `+0x47` | (CFG 간접) 표 `+0x388` = 문지기 — win32kfull 에서는 0 을 돌려주는 공용 조각 (rva `0x1ed1d0`, 같은 주소 이름 641개, 후보: `IsEditionGetUserObjectInformationEntryPointSupported` …) | [정적] |
| 〃 | `+0x7f` | `+0x84` | `win32kbase!EditionGetUserObjectInformationEntryPoint` (rva `0x12cc64`, 중계) | [정적] |
| `win32kbase!EditionGetUserObjectInformationEntryPoint` | `+0x1f` · `+0x50` | `+0x26` · `+0x55` | `W32GetWin32kBaseApiSetTable` · (CFG 간접) 표 `+0x390` = `win32kfull!EditionGetUserObjectInformationEntryPoint` | [정적] |
| `win32kfull!EditionGetUserObjectInformationEntryPoint` (rva `0xbfc60`, 411 B) | `+0x30` · `+0x41` · `+0x5e` | `+0x37` · `+0x46` · `+0x65` | `EnterCrit(0, 0)` · `AtomicExecutionCheck` 생성자 · `ProbeForWrite(pvInfo, nLength, 종류 1 이면 4 아니면 2)` | [정적] |
| 〃 | `+0xbe` · `+0xde` | `+0xc5` · `+0xe5` | `ObReferenceObjectByHandle(h, 0, NULL, UserMode, …)` · `SetHandleFlag(h, 2, 1)` | [정적] |
| 〃 | **`+0x108`** | **`+0x10d`** | **`_GetUserObjectInformation`** (rva `0xc119c`, 981 B) | [정적] |
| 〃 | `+0x118` · `+0x13b` · `+0x156` · `+0x19c` | `+0x11f` · `+0x142` · `+0x15d` · `+0x1a1` | `SetHandleFlag(h, 2, 0)` · `ObfDereferenceObject` · `UserSessionSwitchLeaveCrit` · `UserSetLastError(8)` (`SetHandleFlag` 실패 때) | [정적] |
| `win32kfull!_GetUserObjectInformation` | `+0x56` · `+0x72` · `+0x84` | `+0x5d` · `+0x79` · `+0x8b` | `ObReferenceObjectByHandle` (형식 안 정함) · `ObGetObjectType` · `ObfDereferenceObject` | [정적] |
| 〃 | `+0xe1` | `+0xe8` | `ObReferenceObjectByHandle` (형식 = 창 스테이션 / 데스크톱, 핸들 속성 받음) | [정적] |
| 〃 | `+0x114` · `+0x12c` · `+0x168` | `+0x11b` · `+0x133` · `+0x16d` | `ObQueryNameInfo` ×2 · `memmove` (rva `0x35c6c0` — 이름 2개 `memcpy` · `memmove`) — 종류 2 | [정적] |
| 〃 | `+0x18f` · `+0x3d9` · `+0x3bd` | `+0x194` · `+0x3de` · `+0x3c2` | `UserSetLastError(0x7a / 0x57)` · `UserSetLastError(1)` · `SetLastNtError` | [정적] |
| 〃 | `+0x359` | `+0x35e` | `CheckHandleFlag` (종류 1 · 데스크톱) | [정적] |
| `win32kfull!UserSetLastError` (rva `0x48390`) | `+0x8` · `+0x24` | `+0xf` · `+0x2b` | `KeIsAttachedProcess` · `PsGetCurrentProcessWow64Process` | [정적] |

### 1. user32 — 썽크와 A 판 [정적]

- **W 는 썽크** — `GetUserObjectInformationW.asm +0x0 JMP [0x1800b6d90]` 가 전부. win32u 스텁 `MOV R10,RCX` · `MOV EAX,0x1065` · `SYSCALL` (`+0x0 ~ +0x12`). 다섯 인자 (RCX · RDX · R8 · R9 · 스택 다섯째) 가 그대로 커널로 가고, 커널 반환이 그대로 돌아온다. `GetProcessWindowStation` · `GetThreadDesktop` 도 같은 한 줄 썽크.
- **syscall 인자 5개** (A 판 줄로 확인):

| # | 이름 (공개 문서 자리) | 뜻 | 입력/출력 | 단위 | 줄 |
|---|---|---|---|---|---|
| 1 | `hObj` | 창 스테이션 · 데스크톱 핸들 | 입력 | 핸들 | `+0x22 MOV R15,RCX` · `+0x63 MOV RCX,R15` |
| 2 | `nIndex` | 종류 (1 FLAGS · 2 NAME · 3 TYPE …) | 입력 | — | `+0x16 LEA EAX,[RDX-2]` · `+0x25 CMP EAX,1` · `+0x28 JA +0xd7` |
| 3 | `pvInfo` | 출력 버퍼 (A 는 2 · 3 일 때 자기 임시 버퍼) | **출력** | 바이트 | `+0x58 MOV R8,RAX` |
| 4 | `nLength` | 버퍼 크기 (A 는 2 · 3 일 때 `× 2`) | 입력 | **바이트** | `+0x35 LEA EBX,[R9+R9]` · `+0x55 MOV R9D,EBX` |
| 5 | `lpnLengthNeeded` | 필요 크기 받는 포인터 (A 도 호출자 것을 그대로) | **출력** | **바이트** | `+0x4d MOV RBP,[RSP+0x80]` · `+0x5b MOV [RSP+0x20],RBP` |

- **A 가 돌아온 뒤**:
  - 반환 0 → `+0x7b JZ +0xa5` → 임시 버퍼 해제 → 0. 필요 길이 반감도 변환도 안 한다 (그래서 실패 때 `*lpnLengthNeeded` 는 UTF-16 바이트 그대로).
  - 반환 ≠ 0 → `+0x82 SHR [RBP],1` (바이트 → 글자) → `+0x92 ~ +0x9f` 임시 버퍼를 **NUL 이 나올 때까지** 세고 +1 → `+0x105 RtlUnicodeToMultiByteN(호출자 버퍼, nLength, …, 임시 버퍼, 2 × 글자 수)` (반환 무시). 커널이 NUL 로 끝나는 UTF-16 을 준다고 믿고 읽는다 — NUL 없이 꽉 채운 stub 은 이 세기가 버퍼 끝을 넘는다.
  - 반환은 커널 값 그대로 (`+0xbd MOV EAX,EBX`). 종류 2 · 3 이 아니면 `+0xd7 ~ +0xf0` 로 인자 그대로 넘기고 반환도 그대로.
- **LastError 는 user32 가 안 정한다**: A 의 callee 는 `RtlAllocateHeap` · `NtUserGetObjectInformation` ×2 · `RtlFreeHeap` · `RtlUnicodeToMultiByteN` 뿐 (`_calls.tsv`). 실패 때 보이는 `TEB.LastErrorValue` 는 커널이 쓴 값 (3절).
- `RtlAllocateHeap` NULL 검사 없음 (`+0x48 ~ +0x58`) — 이 요청과 무관.

### 2. combase — `GetThreadWinstaDesktop` 차례 · 실패 값 · 글 [정적]

`ms/disassemble/combase/GetThreadWinstaDesktop.asm`. 함수가 조각 (`FUN_1800824e3` · `FUN_18025b325` · `FUN_18025b3d3`) · 떨어진 범위 (`+0x1d8eb6 ~`, `+0x1d9007 ~`, `+0x1d90ae ~`) · 주인 없는 덩어리로 흩어져 있다. 오프셋은 모두 함수 시작 (`0x180082330`) 기준.

| 차례 | 하는 일 | 줄 |
|---|---|---|
| ① 정해 둔 글 | `g_explicitWinstaDesktopString` (rva `0x338580`) 이 있으면 `_pwszWinstaDesktop` 에 넣고 **0** | `+0x31` · `+0x3b` · `+0x3e JNZ +0x2d2` → `+0x2d2` · `+0x2d9 XOR EAX,EAX` |
| ② user32 없는 프로세스 | `IsGetProcessWindowStationPresent()` 0 **또는** PEB `+0x58` (`KernelCallbackTable`) 0 → `ResolveProcessDesktopNoUser()`. `0x32` 면 ③ 으로, `≤ 0` 은 그대로, `> 0` 은 `0x8007xxxx` | `+0x44` · `+0x4b JZ +0x27e` · `+0x51 MOV RAX,GS:[0x60]` · `+0x5a CMP [RAX+0x58],R12` · `+0x5e JZ` · `+0x27e ~ +0x29c` |
| ③ 창 스테이션 핸들 | `GetProcessWindowStation()` → RDI. 0 이면 ⓐ | `+0x64` · `+0x70` · `+0x73 TEST RAX,RAX` · `+0x76 JZ +0x23f` |
| ④ 데스크톱 핸들 | `GetThreadDesktop(GetCurrentThreadId())` → R14. 0 이면 ⓑ | `+0x7c` · `+0x8a` · `+0x96` · `+0x99 TEST` · `+0x9c JZ +0x31c` |
| ⑤ 창 스테이션 이름 | `len = 0x40` 을 먼저 놓고 `GetUserObjectInformationW(RDI, 2, bufA [RBP-0x49], 0x40, &len)` | `+0xa2 MOV EBX,0x40` · `+0xae MOV [RBP-0x59],EBX` · `+0xb1` · `+0xc1 LEA EDX,[RBX-0x3e]` (= 2) · `+0xc8` · `+0xd4` · `+0xd6 JZ +0x358` |
| ⑥ 데스크톱 이름 | `len = 0x40` 다시, `GetUserObjectInformationW(R14, 2, bufB [RBP-0x9], 0x40, &len)` | `+0xe0` · `+0xe3` · `+0xeb` · `+0xef MOV EDX,2` · `+0xf4` · `+0xf7` · `+0x105 JZ +0x39d` |
| ⑦ 글 만들기 | 두 글 NUL 까지 세고 `cch = A + B + 2` → `HeapAlloc(g_hHeap, 0, cch × 2)` → `_pwszWinstaDesktop` (rva `0x338118`). 할당 실패 `EBX = 0xe` | `+0x10b ~ +0x124` · `+0x126` · `+0x12c` · `+0x136` · `+0x13a` · `+0x146` · `+0x152` |
| ⑧ 글 채우기 | `StringCchCopyW(bufA)` → `StringCchCatW(L"\\")` (`.rdata 0x1802cb830`) → `StringCchCatW(bufB)`. 음수면 `HeapFree` · 전역 NULL · `EBX = 0x6f` | `+0x169` · `+0x17d` · `+0x187` · `+0x1a1` · `JS +0x2a1` → `+0x2b1 MOV EBX,0x6f` · `+0x2c2` |
| ⑨ 성공 | `EBX = 0` | `+0x1ae` · `+0x1b1 JMP +0x1d6` |
| ⑩ 이름 묻기 뒤 출구 | 다시 묻기 버퍼 해제 → **`EBX == 0` 이면 `_pwszWinstaDesktop` 를 보고, NULL 이면 `EBX = 0xe`** → `EBX > 0` 이면 `0x80070000 \| (EBX & 0xffff)` → 반환 | `+0x1d6 ~ +0x1ea` · `+0x1f0 TEST EBX,EBX` · `+0x1f2 JZ +0x22f` → `+0x22f CMP [_pwsz…],R12` · `+0x236 JNZ +0x1ff` · **`+0x238 MOV EBX,0xe`** · `+0x1f4 JLE` · `+0x1f6 MOVZX` · `+0x1f9 OR EBX,0x80070000` · `+0x1ff MOV EAX,EBX` |
| ⓐ 창 스테이션 핸들 0 | `GetLastError` → 0 이면 `EBX = 0` → **`+0x27c JMP +0x1ff`** (⑩ 의 `+0x1f0` 검사를 안 거침) → **0 반환, 글 NULL**. 양수면 `0x8007xxxx`. `0x7f` (지연 로딩 실패) 면 user32 있나 · win32k 호출 금지 정책 → `0x800704ec` / `DiagnoseUser32LoadFailure` 값 | `+0x23f` · `+0x24d` · `+0x24f JLE +0x2ce` · `+0x2ce MOV EBX,EDX` · `+0x25a CMP EDX,0x7f` · `+0x275` · `+0x27c` · far `+0x1d8f24 MOV EBX,0x800704ec` |
| ⓑ 데스크톱 핸들 0 | `GetLastError` → `+0x1d8fe7 ~ +0x1d8ff0` → `+0x1f4 JLE` — 0 이면 **0 반환, 글 NULL** (ⓐ 와 같은 꼴), 양수면 `0x8007xxxx` | `+0x31c` · `+0x32f` · `+0x1d8ff0 JMP +0x1f4` |

- **다시 묻기** (⑤ · ⑥ 이 FALSE 일 때): `GetLastError()` 가 **`0x7a` 일 때만** (`+0x366 CMP EAX,0x7a` · `+0x369 JZ` / `+0x3ab` · `+0x3ae`) `HeapAlloc(g_hHeap, 0, len)` (len = `[RBP-0x59]`) → 같은 핸들 · 종류 2 · 크기 `len` 으로 한 번 더 (`+0x1d902c MOV R9D,[RBP-0x59]`). 성공이면 다음 차례로 (`+0x1d9099 MOV EBX,0x40` → `+0xdc`, `+0x1d90f9 JNZ +0x10b`). 다시 실패면 `GetLastError` → `EBX` → ⑩. 힙 실패는 `+0x152` → `0xe`. 최대 네 번 묻는다.
- **`GetUserObjectInformationW` 를 부르는 곳은 이 함수의 네 곳뿐** (`+0xc8` · `+0xf7` · `+0x1d9044` · `+0x1d90eb`) + 지연 로딩 발판 (`refs 0x363578`). 종류는 늘 2, 처음 크기는 늘 `0x40`.
  - → 요청 쪽 로그의 `(…, 1, buf, 0xc)` (353) · `(0x20400, 2, buf, 0x208)` (1) 은 **combase 가 아닌 다른 모듈** 이 부른 것. 어느 모듈인지는 정적 자료에 없다 `[공백]` (`.NET` 런타임 쪽일 수 있음 `[추측]`).
- **`ResolveProcessDesktopNoUser`** (② 갈래, 반환 = Win32 오류값):
  1. 매개변수 (`PEB+0x20`) `DesktopInfo` (`+0xc0`) 길이 `≥ 2` 이면 `RtlEqualUnicodeString(…, uszDefaultDesktopName, TRUE)` — **대소문자 무시** (`+0x51 MOV R8B,1`). 같으면 combase 상수 **`WinSta0\Default`** (`.rdata 0x1802c24e0`, 대문자 S) 를 전역에 두고 0 (`+0x6e ~ +0x7c`).
  2. 다르면 `\` 를 찾아 첫 글자가 아니면 `DesktopInfo` 를 그대로 복사 (`HeapAlloc(Length + 2)` · `memcpy` · NUL, `+0xa2 ~ +0xe0`). 할당 실패 `0xe`.
  3. 길이 모자람 · `\` 없음 · 맨 앞 `\` → `IsGetProcessWindowStationPresent()` 참이면 **`0x32`** ("핸들로 물어라" → ③), 거짓이면 상수 글 + 0 (`+0x32 ~ +0x3b`, `+0x39 JZ +0x6e`).
- **`SetExplicitWinstaDesktopString`**: `CoInitializeSecurity` 가 `dwCapabilities` 비트 14 일 때만 부른다. `gGotSecurityData ≠ 0` 이면 `RpcETooLate`, 받은 글이 `WinSta0\Default` (`_wcsicmp`) 면 상수 포인터, 아니면 user32 있을 때만 `DuplicateStringPrivMem` (실패 `0x8007000e`, user32 없으면 `0x80004021`) → `FreeExplicitWinstaDesktopString` → `g_explicitWinstaDesktopString` (`+0xaa`). 이번 막힘과 무관 (보통 이 비트를 안 켬 `[추측]`).

### 2-1. 글이 가는 곳 · 부르는 쪽이 실패를 받으면 [정적] (일회용 objdump)

- `CoInitializeSecurity +0x47d GetConnection` → `+0xbe operator()` → `+0x80 CMP [_pwszWinstaDesktop],R15` · `+0x92 JNE +0x100` (이미 있으면 안 부름) → **`+0x94 GetThreadWinstaDesktop`** → `+0x99 MOV EDI,EAX` · `+0x9b TEST` · **`+0x9d JNS +0x100`**.
  - **음수 (실패)**: `+0x9f ~` `Return_Hr` (`+0xb5`) → `EDI` 반환. **`Connect` 를 안 부른다.** `GetConnection +0xc5 CMP EAX,0x80070005` (`E_ACCESSDENIED` 만 따로 — 그 갈래 `[공백]`) → `+0xd2 JNS` 안 탐 → `Return_Hr` (줄 1032) → 반환. `CoInitializeSecurity +0x486 JS +0x89c` → `+0x8a9 MOV EAX,EDI` → **같은 HRESULT 반환.**
  - **0 이상 (성공, 또는 "0 + 글 NULL")**: `+0x100 ~` `memset(0x410)` · `GetModuleFileNameW(NULL, buf, 519)` → … → **`+0x742 MOV RDX,[_pwszWinstaDesktop]`** · `+0x751 LEA R8,[RBP+0x150]` (exe 경로 버퍼) · `+0x78c MOV RCX,[R14]` → **`+0x82e CALL Connect`**. NULL 도 그대로 넘어간다.
- `Connect` (rva `0x12f318`) 를 부르는 곳은 `operator() +0x82e` 하나 (`refs 0x12f318`). 그 안 (RPCSS 쪽 RPC · epmapper 길 — run129 · run133 의 rpcrt4 몫과 이어질 것 `[추측]`) 과 같은 람다의 `CreateFromBindingString` (`+0x33c`) · `CreateFromBindingHandle` 셋은 안 읽음 → Q257.
- 같은 전역을 읽는 다른 곳 (`refs 0x338118`): `DelegateActivationToSCM +0x1a1` · `Irot*` 다섯 · `IMgot*` 셋 — RPCSS 에 말 걸 때 같이 보내는 것으로 보임 `[추측: 인자 자리 안 봄]`. 쓰는 곳은 `GetThreadWinstaDesktop` · `ResolveProcessDesktopNoUser` · `Cleanup +0x108` (아래 대조).
- 글이 한 번 정해지면 다시 안 묻는다 (`operator() +0x80`). NULL 로 남으면 `GetConnection` 마다 다시 묻는다.

### 3. win32k — `NtUserGetObjectInformation` 본문 [정적]

```
win32u!NtUserGetObjectInformation  SYSCALL 0x1065
  ⇒ (win32k.sys __win32kstub_NtUserGetObjectInformation — 이름만, 서비스 표 안 읽음)
  → win32kbase!NtUserGetObjectInformation (0x1223b0)  껍데기
      +0x2d W32GetWin32kBaseApiSetTable → [표+0x30]
      +0x47 (CFG) [작은 표+0x388]  문지기 — 칸 비거나 음수면 FALSE (LastError 안 씀)
      +0x84 win32kbase!EditionGetUserObjectInformationEntryPoint (0x12cc64)  중계
              +0x55 (CFG) [작은 표+0x390] = win32kfull!EditionGetUserObjectInformationEntryPoint (0xbfc60)
                       +0x37 EnterCrit · +0x65 ProbeForWrite · +0xc5 ObReferenceObjectByHandle · +0xe5 SetHandleFlag(2,1)
                       +0x10d _GetUserObjectInformation (0xc119c)   ← 종류별 갈래
                       +0x11f SetHandleFlag(2,0) · *lpnLengthNeeded = 로컬 길이 (+0x183 ~ +0x18f) · +0x15d LeaveCrit
```

- (나무 오프셋은 ret 자리. `⇒` 는 안 읽은 다리 — syscall 번호 → win32k.sys 스텁 → win32kbase 는 이름 · export 표가 맞는 것이 근거 `[추측]`. 함수 표가 win32k.sys 를 거쳐 `[RAX+0x30]` 로 건네지는 코드도 안 읽음 — 칸 번호 일치가 근거.)
- 이름 주의: `EditionGetUserObjectInformationEntryPoint` 는 **win32kbase (중계, `0x12cc64`) 와 win32kfull (본체, `0xbfc60`) 둘 다** 있다. 글에서는 모듈 이름을 같이 쓴다.
- **win32kfull 입구** (`EditionGetUserObjectInformationEntryPoint`):
  - `ProbeForWrite` 정렬 = 종류 1 이면 4, 아니면 2 (`+0x47 ~ +0x55`). `lpnLengthNeeded` 가 있으면 읽고 되써서 쓰기 가능 검사 (`+0x6a ~ +0x92`).
  - 핸들 참조 실패 → **FALSE, LastError 안 씀, `*lpnLengthNeeded` 도 안 씀** (`+0xcc JS +0x1a3`). `SetHandleFlag` 실패 → FALSE + LastError **8** (`+0x199` · `+0x19c`).
  - `_GetUserObjectInformation` 의 다섯째 인자는 **자기 로컬** (`[RSP+0x30]`, `+0x28` 에서 0 으로 비움). 돌아온 뒤 호출자 포인터가 NULL 이 아니면 **성공이든 실패든 그 로컬 값을 쓴다** (`+0x124 ~ +0x12f JNZ +0x183` → `+0x18f MOV [RCX],EAX`).
- **`_GetUserObjectInformation`** (종류별):
  1. 형식 모른 채 참조 → `ObGetObjectType` → 놓기. 실패면 `SetLastNtError` · FALSE (`+0x69 JS +0x3bb`).
  2. 창 스테이션 (`ExWindowStationObjectType`) / 데스크톱 (`ExDesktopObjectType`) 이 아니면 LastError **1** · FALSE (`+0x90 ~ +0xaa` · `+0x3d6` · `+0x3d9`). 종류 5 · 6 은 데스크톱만 (`+0xb3 ~ +0xb9`).
  3. 형식 정해 다시 참조, 핸들 속성 받음 (`+0xc4` · `+0xe1`).
  4. 종류 갈래: 1 → `+0x2ed` · 2 → `+0x111` · 3 → `+0x261` · 4 · 5 · 6 · 8 (DWM) · 그 밖 LastError **`0x57`** (`+0xff ~ +0x10b` · `+0x340`).
  5. 끝: `*로컬 = [RSP+0x30]` (`+0x37e`) → 놓기 → 반환 `[RSP+0x34]` (처음 1).

| 종류 | 칸 | 쓰는 값 | 줄 |
|---|---|---|---|
| **2 (이름)** | 글 | 객체 이름 (`ObQueryNameInfo` `+0x8` `Name`) 을 `Length` 바이트 복사 — **끝 조각만** (`WinSta0` · `Default`, `\Windows\WindowStations\` 안 붙음) | `+0x111 ~ +0x146` · `+0x168` |
| 〃 | NUL | 글 뒤 `0x0000` | `+0x16d ~ +0x178 MOV word [RAX+RCX],DI` |
| 〃 | 필요 길이 | **`Length + 2` (바이트, NUL 포함)** | `+0x14a LEA EAX,[R8+2]` · `+0x14e` · `+0x37e` |
| 〃 | 모자람 | `Length + 2 > nLength` → **FALSE · LastError `0x7a` · 필요 길이는 씀 · 버퍼 안 씀** | `+0x152 CMP EAX,R15D` · `+0x155 JA` · `+0x18a MOV ECX,0x7a` · `+0x18f` · `+0x194` |
| 〃 | 이름 없음 | `ObQueryNameInfo` NULL → **TRUE · 필요 길이 0 · 버퍼 안 씀** | `+0x123 JZ` · `+0x144 JZ +0x181` · `+0x181` |
| **1 (FLAGS, 12 B)** | `+0x0` `fInherit` `[추측: 공개 이름]` | 핸들 속성 비트 `0x2` (`OBJ_INHERIT`) 를 0 / 1 로 | `+0x30c` · `+0x311 SHR EAX,1` · `+0x313 AND` · `+0x31b` |
| 〃 | `+0x4` `fReserved` | 0 (`+0x4 ~ +0xb` 8 바이트를 0 으로) | `+0x31d MOV [RBX+4],RDI` |
| 〃 | `+0x8` `dwFlags` | **창 스테이션**: 객체 `+0x40` 비트 `0x4` 가 꺼져 있으면 **1** (공개 상수 `WSF_VISIBLE` 와 같은 값 `[추측: 이름]`), 켜져 있으면 0. **데스크톱**: `CheckHandleFlag(…)` 참이면 1 (이름 `[공백]`) | `+0x321 ~ +0x33a MOV [RBX+8],R13D` · 데스크톱 `+0x34a ~ +0x362` |
| 〃 | 필요 길이 | 늘 `0xc` (먼저 놓음). `nLength < 0xc` 면 FALSE + `0x7a` | `+0x2ed` · `+0x2f5` · `+0x2f9 JC +0x18a` |
| 3 (TYPE, 덧붙임) | 글 | 객체 형식 (`OBJECT_TYPE +0x10` `Name`) 을 종류 2 와 같은 코드로 | `+0x261 LEA RAX,[R14+0x10]` · `+0x265 JMP` |

- 크기 예: `WinSta0` · `Default` 둘 다 7 글자 → **16 바이트**. combase 의 첫 크기 `0x40` 안이라 진짜 Windows 에서는 다시 묻기 (`0x7a`) 길을 안 탄다.
- 대소문자: 객체 이름은 만든 쪽이 준 글자 그대로라 win32k 코드가 정하지 않는다. 커널 쪽 상수는 대문자 S — win32kfull `.rdata` `WinSta0` (rva `0x36fee8`, `xxxResolveDesktop +0x3f4` · `xxxResolveDesktopForWOW +0x16b` 가 읽음) · `\Windows\WindowStations\WinSta0` (`0x36f1c0`). → `[정적 근거로는 WinSta0]`, 실제 객체 이름은 `[공백]` (Q256). run44 `[관찰]` 의 `Winsta0\Default` (소문자 s) 는 **프로세스 매개변수 글** (`ms/logs/04_baggage-read_P2.txt:3243`) 이라 이 출력과 별개.
- `[관찰]` (덧붙임) run44 P1b · P2 이름 있는 객체 목록에 `WinSta0_DesktopSwitch` (Event) · `CicLoadWinStaWinSta0` (Mutant) 가 있다 (`ms/logs/04_baggage-read_P1b.txt:2082` · `:1701`). 다른 객체의 이름이라 창 스테이션 객체 이름의 근거는 아니다 — 대문자 S 를 쓰는 이름이 있다는 정도 `[추측]`.
- **LastError 는 커널이 직접 쓴다**: `UserSetLastError +0x18 MOV RAX,GS:[0x30]` (커널 GS 의 `+0x30` = 현재 TEB `[추측: KPCR 칸 이름]`) · `+0x21 MOV [RAX+0x68],EBX` (`TEB.LastErrorValue`). WoW64 면 `+0x3e` 에서 `+0x2034` 도. 단 남의 프로세스에 붙어 있으면 (`KeIsAttachedProcess`, `+0x16 JNZ`) 안 쓴다.

### 4. 에뮬레이터 막힘 — 줄로 따라가기 [정적]

요청 쪽 r14 로그 (에뮬레이터 — `[관찰]` 아님) 를 코드 줄에 맞추면:

| # | 일 | 그때 값 | 줄 |
|---|---|---|---|
| 1 | `GetProcessWindowStation` → `NtUserGetProcessWindowStation` | `0x20000` (0 아님) → 통과 | `GetThreadWinstaDesktop +0x73` · `+0x76` |
| 2 | `GetThreadDesktop` → `NtUserGetThreadDesktop` | `0x20400` → 통과 | `+0x99` · `+0x9c` |
| 3 | `GetUserObjectInformationW(0x20000, 2, bufA, 0x40, &len)` → 썽크 → `0x1065` | stub: **반환 0 · 버퍼 안 씀 · `*len` 안 씀 · TEB `+0x68` 안 씀** | `+0xc8` · `+0xd4` · `+0xd6 JZ +0x358` |
| 4 | `GetLastError()` | TEB `+0x68` 에 남아 있던 값 — **0** 이면 | `+0x358` · `+0x364 MOV EBX,EAX` · `+0x366 CMP EAX,0x7a` (아님) |
| 5 | 출구 | `EBX = 0` 인데 `_pwszWinstaDesktop` 는 아직 NULL | `+0x1d2` · `+0x1d6 ~ +0x1ea` · `+0x1f0` · `+0x1f2 JZ +0x22f` · `+0x236` |
| 6 | **`0x8007000e` 만들기** | `EBX = 0xe` → `0x8007000e` | **`+0x238 MOV EBX,0xe`** · `+0x1f6` · `+0x1f9` · `+0x1ff` |
| 7 | 부르는 쪽 | 음수 → `Connect` 안 부름 | `operator() +0x9d` (안 뛰어) · `+0xb5 Return_Hr` |
| 8 | `GetConnection` → `CoInitializeSecurity` | 같은 값 그대로 반환 | `GetConnection +0xc5` · `CoInitializeSecurity +0x486 JS` · `+0x8a9 MOV EAX,EDI` |

- **`0x8007007a` 샘플 (5)** = 4 에서 묵은 LastError 가 `0x7a` 였던 경우: 다시 묻기로 가서 (`+0x369 JZ`) `HeapAlloc(len)` + 한 번 더 → 또 0 → `GetLastError()` 는 여전히 `0x7a` → `0x8007007a`. stub 이 `*len` 을 안 쓰면 `len` 은 combase 가 먼저 놓은 `0x40` 그대로라 (`+0xae`), 다시 묻기도 `(0x20000, 2, buf, 0x40)` 꼴로 보인다 (flow-writer 보탬 — 아래 대조).
- **같은 "0 + LastError 0" 인데 핸들 단계 (r13) 는 다르다**: 창 스테이션 핸들 0 → `+0x23f GetLastError` 0 → `+0x24f JLE +0x2ce` → `EBX = 0` → `+0x27c JMP +0x1ff` — ⑩ 의 `+0x1f0` (글 NULL 검사) 를 **안 거쳐서 0 반환 + 글 NULL** → `operator() +0x9d JNS` → `Connect(NULL)` 로 감. 요청 쪽 r13 의 "이름 안 묻고 바로 `\RPC Control\epmapper` 연결" 과 맞는다 (`Connect` → epmapper 이음은 `[추측]` — Q257).
- 요청 쪽 `[추측]` "묵은 `GetLastError` 를 HRESULT 로 바꿈" → **코드가 그렇게 쓰여 있다** (`+0x364` · `+0x1f4 ~ +0x1f9`). 다만 0 일 때는 "바꿈" 이 아니라 **`0xe` 로 갈아 끼움** (`+0x238`) 이다. run131 `RegisterOleWndClass +0x13980b` 과 같은 꼴.

### 호출 관계

```
combase!CoInitializeSecurity (0x7e880)                                                    [정적]
 ├─ +0x90a  SetExplicitWinstaDesktopString(L"WinSta0\Default")   ← dwCapabilities 비트 14 일 때만
 └─ +0x482  CRpcResolver::GetConnection (0xa14d0)                 ← gGotSecurityData == 0
             └─ +0xc3  operator() (0x18b9c0)
                        ├─ +0x99  CRpcResolver::GetThreadWinstaDesktop (0x82330)   ← _pwszWinstaDesktop == NULL 일 때만
                        │          ├─ ①  g_explicitWinstaDesktopString 있으면 그것 → 0
                        │          ├─ +0x49   IsGetProcessWindowStationPresent
                        │          ├─ +0x283  ResolveProcessDesktopNoUser (0x82a04)   ← user32 없음 또는 PEB+0x58 == 0
                        │          │           └─ +0x65 RtlEqualUnicodeString(DesktopInfo, "WinSta0\Default", 대소문자 무시)
                        │          ├─ +0x6b   GetProcessWindowStation ⇢ user32 썽크 → NtUserGetProcessWindowStation  ── SYSCALL [0x101f]
                        │          ├─ +0x91   GetThreadDesktop ⇢ user32 썽크 → NtUserGetThreadDesktop               ── SYSCALL [0x107b]
                        │          ├─ +0xcf   GetUserObjectInformationW(창 스테이션, 2, 0x40 B, 0x40, &len)
                        │          │           ⇢ user32 썽크 → NtUserGetObjectInformation                         ── SYSCALL [0x1065]
                        │          │              ⇒ win32kbase!NtUserGetObjectInformation +0x84 → Edition… (중계) +0x55
                        │          │                 → win32kfull!EditionGetUserObjectInformationEntryPoint +0x10d _GetUserObjectInformation
                        │          ├─ +0xfe   GetUserObjectInformationW(데스크톱, 2, 0x40 B, 0x40, &len)   (같은 길)
                        │          ├─ (FALSE · LastError 0x7a) +0x1d904b / +0x1d90f2  같은 묻기 한 번 더 (HeapAlloc(len))
                        │          └─ +0x141 HeapAlloc · +0x16e StringCchCopyW · +0x18c / +0x1a6 StringCchCatW  → _pwszWinstaDesktop
                        │             출구: 이름 단계 0 + 글 NULL → 0x8007000e (+0x238) / 핸들 단계 0 → 0 + 글 NULL (+0x27c)
                        ├─ (음수)  +0xba Return_Hr → 반환 (Connect 안 감)
                        └─ (0 이상) +0x129 GetModuleFileNameW · … · +0x833 Connect (0x12f318)  RDX = _pwszWinstaDesktop (NULL 가능)
```

- (나무 오프셋은 ret 자리. `⇢` 는 지연 가져오기 발판 (`ext-ms-win-ntuser-windowstation-l1-1-0`) — 그 안은 안 읽음. `⇒` 는 syscall → win32k 다리 (이름만). 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` `user32!GetUserObjectInformationW` · `GetProcessWindowStation` · `GetThreadDesktop` 은 한 줄 `JMP [가져오기 칸]` 썽크 — 칸 셋 = win32u `NtUserGetObjectInformation` (`0x1065`) · `NtUserGetProcessWindowStation` (`0x101f`) · `NtUserGetThreadDesktop` (`0x107b`).
- `[정적]` `NtUserGetObjectInformation` 인자 5개 — 길이 · 필요 길이 모두 **바이트**. user32 는 LastError 를 안 쓴다. A 판은 2 · 3 에서만 UTF-16 → ANSI 변환 + 필요 길이 반감 (성공 때만).
- `[정적]` combase 는 `GetThreadWinstaDesktop` 한 곳에서만 `GetUserObjectInformationW` 를 부른다 — 종류 2, 처음 크기 `0x40`, 창 스테이션 · 데스크톱 한 번씩, `0x7a` 면 그 묻기만 한 번 더.
- `[정적]` 만드는 글 = `<창 스테이션>\<데스크톱>` → `CRpcResolver::_pwszWinstaDesktop` → `operator() +0x742` → `Connect` 둘째 인자.
- `[정적]` 이름 단계가 "FALSE + LastError 0" 이면 `0x8007000e` (`+0x238`), 묵은 `0x7a` 면 `0x8007007a`. 실패면 `Connect` 를 안 부르고 `CoInitializeSecurity` 가 같은 값을 돌려준다. 핸들 단계가 "0 + LastError 0" 이면 0 + 글 NULL → `Connect(NULL)`.
- `[정적]` 커널 본문 = win32kfull `_GetUserObjectInformation`. 종류 2: UTF-16 끝 조각 + NUL, 필요 길이 `Length + 2` 바이트, 모자라면 FALSE + `0x7a` + 필요 길이 씀. 종류 1: 12 B `{fInherit, 0, dwFlags}`, 필요 길이 `0xc`. LastError 는 커널이 TEB `+0x68` 에.
- `[정적]` user32 가 없는 프로세스 (또는 PEB `+0x58` 0) 는 핸들을 안 묻고 매개변수 `DesktopInfo` 로 정한다. 기본 이름이면 combase 상수 `WinSta0\Default`.
- `[관찰]` test.exe 는 이 길을 안 탄다: user32 · combase 안 올림 (P2 모듈 6개), `_EPROCESS.Win32WindowStation` NULL (K1 ~ P2), PEB `KernelCallbackTable` NULL (`_P1a.txt:1754` · `_P2.txt:2464`, run85 줄), `DesktopInfo` = `Winsta0\Default` (`_P1a.txt:2533` · `_P2.txt:3243`).

### flow-writer 대조 (결과서와 다른 곳 · 보탬)

- **`_GetUserObjectInformation` 의 이른 실패는 필요 길이 줄 (`+0x37e`) 을 안 지난다.** 핸들 참조 실패 (`+0x3bd SetLastNtError`) · 형식 아님 (`+0x3d9 UserSetLastError(1)`) 은 `+0x3c2 XOR EAX,EAX` → `+0x3c4 JMP +0x393` (= `+0x38f` 바로 뒤) 로 나간다. 결과서 3-3 "끝" 줄은 늘 쓰는 것처럼 읽힌다. 대신 바깥 win32kfull `EditionGetUserObjectInformationEntryPoint +0x124 ~ +0x18f` 가 자기 로컬 (`+0x28` 에서 0) 을 써서 **그때 `*lpnLengthNeeded` = 0** 이 된다. 지원 안 하는 종류 (`0x57`) 는 `+0x340` → `+0x18f` → `+0x37e` 로 가서 마찬가지로 0.
- **`UserSetLastError` 는 붙은 상태면 안 쓴다** (`+0x8 KeIsAttachedProcess` · `+0x16 JNZ +0x46`) — 결과서엔 없음 (보탬). 이 요청 길과는 무관.
- **`Cleanup +0x108` 은 `_pwszWinstaDesktop` 에 쓴다** (`refs 0x338118` WRITE). 결과서 2-4 는 `Cleanup +0xd2` 를 "읽기만" 목록에 넣었다 — 읽기 (`+0xd2`) 와 쓰기 (`+0x108`) 둘 다.
- **다시 묻기의 크기** (보탬): combase 는 묻기 전에 `len = 0x40` 을 놓는다 (`+0xae` · `+0xe0`). stub 이 `*lpnLengthNeeded` 를 안 쓰면 다시 묻기도 `0x40` (`+0x1d902c MOV R9D,[RBP-0x59]`). 결과서 2-2 는 "stub 이 0 을 썼을 때 길이 0" 만 적었다. 요청 쪽 `(…, 2, buf, 0)` 8 샘플은 그대로 `[추측]`.
- `GetUserObjectInformationA +0xa1 TEST EDI,EDI` · `+0xa3 JNS` — 변환은 `nLength` 가 부호 있는 수로 0 이상일 때만 (결과서는 `+0xa3` 만 적음, 뜻 같음).
- `refs 0x363578` 넷째 줄 이름이 `LAB_18025b3de+0x3d (함수 밖)` 로 나오는데 `.asm` 의 `+0x1d90eb` 와 같은 자리 (`0x25b41b`).
- 그 밖 결과서의 오프셋 (user32 · combase 본체 · 조각 · objdump 셋 · win32kbase · win32kfull) 은 `.asm` / objdump 와 **모두 일치**.

## 요청 쪽 로그와 코드 (요청 쪽 에뮬레이터 로그 — 우리 `[관찰]` 아님)

- **맞는 것**: `(0x20000, 2, buf, 0x40)` = combase `+0xc8` 꼴 그대로 (W 직접, `nLength 0x40`). `0x8007000e` / `0x8007007a` 두 값 = 위 4절 줄. r13 핸들 0 → 이름 안 묻고 연결 = ⓐ 줄. 그 뒤 RPCSS 연결 대신 실패로 끝남 = `operator() +0x9d`.
- **combase 가 아닌 것**: `(0x20000, 1, buf, 0xc)` 353 · `(0x20400, 2, buf, 0x208)` 1 — combase 는 종류 1 도 `0x208` 도 안 쓴다. 호출자 `[공백]` (Q256 ③).
- **에뮬레이터 PEB**: 로그가 `NtUserGetProcessWindowStation` 에 닿았으니 에뮬레이터 PEB `+0x58` 은 0 이 아니라고 거꾸로 읽힌다 `[추측]` (② 를 안 탐). 0 이면 핸들 없이 `DesktopInfo` 길로 간다.

## PE_emulator 쪽 (결과서 "지금 우리 맞나·틀리나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 줄 (결과서가 적은 자리) | 판정 | 까닭 |
|---|---|---|
| `NtUserGetProcessWindowStation` → `0x20000` | **맞다** | combase 는 0 인지만 본다 (`+0x73`). 진짜 값은 `TODO(VM)` 유지 (Q256) |
| `NtUserGetThreadDesktop` → `0x20400` | **맞다** | `+0x99`. 같은 이유 |
| `NtUserGetObjectInformation` 기본 대답 = 반환 0 · 버퍼 안 씀 | **틀림 — 막힘의 원인** | 4절: LastError 0 + 글 NULL → `0x8007000e` → `CoInitializeSecurity` 실패 |
| 종류 2 · 창 스테이션 | **stub 이 돌려줄 값** | 반환 1 · 버퍼에 UTF-16 `WinSta0` + `0x0000` (16 B) · `*lpnLengthNeeded = 16`. `nLength < 16` 이면 반환 0 + TEB `+0x68 = 0x7a` + `*lpnLengthNeeded = 16` (버퍼 안 씀). 대소문자는 정적 근거로 `WinSta0`, 확정은 Q256 |
| 종류 2 · 데스크톱 | **stub 이 돌려줄 값** | 같은 꼴, `Default` (16 B). 정확한 글자 `[공백]` (Q256) |
| 종류 1 (`0xc`, 353 샘플) | **stub 이 돌려줄 값** | 반환 1 · `{fInherit (핸들 속성 — [공백], Q256), 0, dwFlags = 1 [추측]}` · `*lpnLengthNeeded = 0xc`. 모자라면 0 + `0x7a` + `0xc`. 이 호출은 combase 가 아님 |
| `(0x20400, 2, buf, 0x208)` · `(0x20000, 2, buf, 0)` | 위 규칙으로 됨 | `0x208` → 16 B 쓰고 TRUE. 길이 0 → FALSE + `0x7a` + 16 (combase 면 `HeapAlloc(16)` 후 다시 묻기) |
| 다섯째 인자 `lpnLengthNeeded` | **써야 한다** | 커널은 성공 · `0x7a` 실패 모두 쓴다 (`Edition… +0x183 ~ +0x18f`). NULL 이면 안 씀 |
| 실패를 흉내 낼 때 | **TEB 칸에 직접** | 커널이 `TEB+0x68` 에 쓴다 (`UserSetLastError +0x21`, WoW64 면 `+0x2034` 도). user32 는 안 씀 |
| 핸들을 0 으로 돌릴 때 (r13) | 길 맞음 | 0 + LastError 0 → combase 0 + 글 NULL → `Connect(NULL)`. LastError > 0 이면 `0x8007xxxx` 로 멈춤 |
| PEB `+0x58` | **영향 있음** | 0 이면 핸들을 안 묻고 `DesktopInfo` 길 (상수 `WinSta0\Default`). 지금은 0 아님으로 읽힘 `[추측]` (Q256 ④) |

- 한 줄로: **stub 은 "종류 2 → TRUE + UTF-16 이름 + NUL + 필요 길이 (바이트, NUL 포함)", 길이 모자라면 "FALSE + TEB `+0x68` = `0x7a` + 필요 길이" 를 돌려주면 이 막힘을 넘는다.** 다음 벽은 `Connect` (RPCSS) — Q257.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 4 — 대기열 **Q256 (동적: 창 스테이션·데스크톱 이름 대소문자·핸들 값·종류 1 dwFlags/fInherit, 에뮬레이터 PEB+0x58)**. test.exe 가 아니라 user32 를 올리고 COM 보안을 여는 GUI 프로세스 — 고르기는 사용자 결정. 유저 멈춤점 · 그 프로세스 조건 · ret 자리. 조각 안 주소는 WinDbg 가 이름으로 안 보여 줄 수 있으니 `combase+rva` 로 건다):

1. 이름 · 핸들 — `combase+0x823a3` (`GetThreadWinstaDesktop +0x73`) `r rdi` · `combase+0x823c9` (`+0x99`) `r r14` · `combase+0x823ff` (`+0xcf`) `r rax` · `du @rbp-0x49` (대소문자) · `dd @rbp-0x59 L1` (16) · `combase+0x8242e` (`+0xfe`) `du @rbp-0x9` · `combase+0x8252f` (`+0x1ff`) `du poi(combase+0x338118)` · `combase+0x8255d` (`+0x22d`) `r eax`.
2. 종류 1 진짜 값 — 커널 `win32kfull!_GetUserObjectInformation +0x31b` (`fInherit`) · `+0x33a` (닿으면 `+0x40` 비트 `0x4` 꺼짐) · `+0x335` `cl`. 세션 드라이버라 GUI 프로세스 문맥 (`.process /i`). 유저 쪽은 `user32!GetUserObjectInformationW` 입구 (`edx` 1 · `r9d` 0xc) + 반환 뒤 12 B.
3. 요청 쪽 `(…, 1, buf, 0xc)` · `0x208` 호출자 — 에뮬레이터 샘플에서 호출 스택 (`caller+0x…`).
4. 에뮬레이터 PEB `+0x58` 이 0 이 아닌지 (에뮬레이터 쪽 한 번).

정적 (결과서 "다음 요청 후보" 1 ~ 3 — 대기열 **Q257 (정적: combase `Connect` rva 0x12f318 과 CreateFromBinding* → epmapper · NtUserGetProcessWindowStation/NtUserGetThreadDesktop 커널 본문 · GetConnection +0xc5 E_ACCESSDENIED 갈래)**):

1. **다음 벽** `Connect` (rva `0x12f318`, `operator() +0x82e`) — 바인딩 핸들 · exe 경로 · `_pwszWinstaDesktop` 이 어떤 꼴로 보내지나, 같은 람다의 `CreateFromBindingString` (`+0x33c`) · `CreateFromBindingHandle` 셋이 `\RPC Control\epmapper` (run129 · run133) 로 어떻게 이어지나.
2. `NtUserGetProcessWindowStation` · `NtUserGetThreadDesktop` 커널 본문 (win32kbase 껍데기 → win32kfull `EditionGetProcessWindowStationEntryPoint` · `EditionGetThreadDesktopEntryPoint` · `_GetProcessWindowStation`) — 핸들 값 · 핸들 속성 (`fInherit`) 이 어디서 정해지나. win32kbase · win32kfull 은 이제 analyze 돼 있어 export 만. (문지기 칸 후보 이름 `Is_GetProcessWindowStationSupported` · `IsEditionGetThreadDesktopEntryPointSupported` 는 0 을 돌려주는 공용 조각 `0x1ed1d0` 의 이름 무리에 있음 — flow-writer 확인.)
3. `GetConnection +0xc5` `E_ACCESSDENIED (0x80070005)` 갈래 · `CoInitializeSecurity` 앞부분 (`+0x7a` `Query` · `+0x87` `IsApartmentInitialized`).
- 그 밖 `[공백]`: 데스크톱 종류 1 `dwFlags` 비트 이름, `_GetUserObjectInformation` 종류 4 · 5 · 6 · 8 출력, syscall → win32k.sys 스텁 → win32kbase 다리 · 함수 표 건네기 (이름 · 칸 번호만 근거), 지연 가져오기 `l1-1-0` → `l1-1-2` 허용 (run131 정적 4 와 같은 자리).

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서 제안 (기록만, 하네스 제안은 오케스트레이터 몫): `ExportFunctions.java` 가 이름 정규식만 받아 같은 이름 70개 (`operator()`) 중 하나를 못 뽑는다 → `rva:0x…` 선택자가 있으면 `_oneoff/` objdump 대신 `.asm` · `_calls.tsv` 를 얻는다.
- 작은 어긋남 (기록만): combase `.asm` 은 `0x1466e4` 를 `ALIASES(5)` 로 적는데 `pdb_aliases.py --lookup combase.dll 0x1466e4` 는 3개 (`ComTraceMessageT` K · H · I) 를 준다. 이 글에서는 `pdb_aliases.py` 쪽을 썼다.
- win32kbase analyze 로그의 `WindowsResourceReference` 스크립트 못 불러옴 경고는 Ghidra 기본 스크립트 쪽 — `SetPdb` · `ExportIndex` 는 정상 (결과서).

# 134 winsta-name — 정적 분석 결과

- 날짜: 2026-11-23
- 요청서: `_workspace/sast/134_winsta-name.md` (Q250, PE_emulator 요청 `324_internals-winsta-name.md`)
- 판정: **분석성공.** 요청서 물음 1·2·3 모두 줄 근거와 함께 답했다. 3 은 요청서 예상과 달리 **win32kbase 만으로는 안 닫혔다** — `NtUserGetObjectInformation` 은 win32kbase 에 있지만 본문은 함수 표를 거쳐 **win32kfull** (`EditionGetUserObjectInformationEntryPoint` → `_GetUserObjectInformation`) 에 있어서, 그 하나를 더 analyze 했다 (둘 다 `FUN_` 없는 PDB 이름으로 잡힘, 아래 3-1).
- 표기: 줄 근거의 `+0x…` 는 전부 `[정적]` (코드에 그렇게 쓰여 있다 — 실행해서 본 것이 아님). 함수 시작 기준 오프셋이고, `call` 은 ret 위치 (`caller+0x…` 와 같은 값) 를 `(ret +0x…)` 로 같이 적었다. `[추측]` · `[공백]` 은 따로 표시. `.asm` 파일 이름은 `ms/disassemble/<모듈>/<함수>.asm`.
- 스크립트 문제: **없음.** (개선 제안 1건은 맨 아래 "스크립트 제안".)
- 서버 메모리: 시작 때 `free -g` = total 46 / used 17 / **available 27~28 GB.** 다른 decompiler 둘의 Ghidra (advapi32 analyze → 뒤에 lsasrv analyze) 가 돌고 있어서 그 프로젝트는 안 건드렸다. 나는 user32·combase·win32u·win32kbase·win32kfull 을 **한 번에 하나씩** 만 돌렸다: export·refs 는 `MAXMEM=4G~6G`, 새 analyze 둘 (win32kbase 21:03~21:06, win32kfull 21:06~21:08) 은 `MAXMEM=10G` (analyze 시작 전 available 25~27 GB, 끝난 뒤 24~25 GB — 모자란 적 없음). 지금 내 Ghidra 프로세스는 없다.

## 모듈 / 바이너리 / PDB (GUID 확인)

| 모듈 | 바이너리 (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`) | PDB GUID+Age (`ms/sym/*.pdb.info.txt`) | 상태 |
|---|---|---|---|
| user32 | `user32.dll` | `CDB947D1B96B87977915DE04CF54279C1` (sast/128 과 같음) | 이미 analyze — export·refs 만 |
| combase | `combase.dll` | `DA1DC0C4F35CEE58F672CBB20460D11D1` (sast/128 과 같음) | 이미 analyze — export·refs 만 |
| win32u | `win32u.dll` | (이미 analyze) | 스텁 export (depth 0) |
| win32kbase | `win32kbase.sys` (3,420,160 B) | **`D22DFDA3D54B87E6BD0567A57877904C1`** (`ms/sym/win32kbase.sys.pdb.info.txt` = 바이너리 CodeView — `fetch_pdb.py` 가 같은 값을 "이미 있음" 으로 확인) | **이번에 새로 analyze** (약 3분, `MAXMEM=10G`, 21:03~21:06). 증거 셋: `ms/ghidra/win32kbase.gpr` · `ms/decomp/win32kbase/_index.tsv` (9,144줄, `FUN_` 0개) · `analyze.log` 의 `SetPdb: win32kbase.sys <- …win32kbase.sys.pdb` |
| win32kfull | `win32kfull.sys` (4,390,912 B) | **`5CD57181BCFDC5AE8F4819BEB1196F091`** (`.info.txt` 와 `fetch_pdb.py` 가 같은 값) | **이번에 새로 analyze** (약 2분, `MAXMEM=10G`, 21:06~21:08). 증거 셋: `.gpr` · `_index.tsv` (10,099줄) · `SetPdb: win32kfull.sys <- …` 줄 |

win32k PDB 둘은 공개 심볼이라 **구조체 타입이 없다** (`pdb_struct.py --list` 가 `USEROBJECTFLAGS` · `WINDOWSTATION` 0개). 그래서 3 절의 구조체 칸은 "코드가 읽는 오프셋" 으로만 적고, 칸 이름은 공개 문서 이름을 `[추측]` 으로 붙인다.

## 뽑은 것

- user32 (`ms/disassemble/user32/`, `ms/decomp/user32/`): `GetUserObjectInformationW` · `GetUserObjectInformationA` · `GetProcessWindowStation` · `GetThreadDesktop` (depth 1 로 넓혔지만 셋은 JMP 썽크, A 의 callee 는 가져오기 칸이라 더 안 늘었다 — 4개).
- win32u (`ms/disassemble/win32u/`): `NtUserGetObjectInformation` · `NtUserGetProcessWindowStation` 스텁 (`NtUserGetThreadDesktop` 은 전부터 있음).
- combase (`ms/disassemble/combase/`, `ms/decomp/combase/`): `GetThreadWinstaDesktop` · `ResolveProcessDesktopNoUser` · `SetExplicitWinstaDesktopString` + depth 1 로 딸려 온 14개 (`IsGetProcessWindowStationPresent` · `FreeExplicitWinstaDesktopString` · `DuplicateStringPrivMem` · `BreakOnCoInitializeSecurityRuntimeFailureIfRequested` · `StringCchCopyW` · `StringCchCatW` · `IsWppLevelEnabled` · `ComTraceMessageT*` 등 17개 중).
- win32kbase (`ms/disassemble/win32kbase/`, `ms/decomp/win32kbase/`): `NtUserGetObjectInformation` · `EditionGetUserObjectInformationEntryPoint` (win32kbase 쪽 중계 함수) · `_guard_dispatch_icall` (depth 1 로 딸려 옴 — 3개).
- win32kfull (`ms/disassemble/win32kfull/`, `ms/decomp/win32kfull/`): `EditionGetUserObjectInformationEntryPoint` · `_GetUserObjectInformation` + depth 1 로 딸려 온 6개 (`UserSetLastError` · `SetLastNtError` · `CheckHandleFlag` · `IsCurrentProcessDwm` · `AtomicExecutionCheck` · `memcpy`) — 8개. `refs` 로 함수 표 칸 확인 (`ms/disassemble/win32kfull/_refs/EditionGetUserObjectInformationEntryPoint.tsv` · `IsEditionGetUserObjectInformationEntryPointSupported.tsv`) · `WinSta0` 글 (`_refs/0x36fee8.tsv`).
- 누가 읽나 (`refs`, `ms/disassemble/combase/_refs/`): `0x338118` (`CRpcResolver::_pwszWinstaDesktop`) · `0x338580` (`g_explicitWinstaDesktopString`) · `0x82330` (`GetThreadWinstaDesktop`) · `0x82a04` (`ResolveProcessDesktopNoUser`) · `0x363578` (`GetUserObjectInformationW` 지연 가져오기 칸) · `0x18b9c0` · `0xa14d0` · `0x12f318`. user32: `0xb6d90` (`ms/disassemble/user32/_refs/`).
- **일회용 objdump** (Ghidra export 아님, `llvm-objdump-14` + `_index.tsv` 이름 붙임): `ms/disassemble/combase/_oneoff/` 의 `operator_0x18b9c0.objdump.txt` · `GetConnection_0xa14d0.objdump.txt` · `CoInitializeSecurity_0x7e880.objdump.txt`. 이유: `GetThreadWinstaDesktop` 의 부르는 쪽이 `operator()` 라는 이름인데 combase 에 같은 이름이 70개라 `export` 로는 하나만 못 뽑는다 (아래 "스크립트 제안"). 이 세 파일의 오프셋은 **함수 시작 주소를 내가 직접 빼서** 적었다 (`0x18018ba54 − 0x18018b9c0 = +0x94`).
- 일회용 읽기 (스크래치패드, 커밋 안 함): combase `.rdata` UTF-16 글 (`0x2cb830` `\`, `0x2c24e0` `WinSta0\Default`, `0x2791a0` `uszDefaultDesktopName`), `apisetschema.dll` 의 `.apiset` 에서 `ntuser-windowstation`, win32kfull 함수 표 칸 값 (`0x3b4ff8` · `0x3b5000`), win32k 세 파일의 UTF-16 `winsta0` 글 찾기.

## 버전 확인

user32·combase 는 "알려진 사실" 표에 없다. 대신 다음을 대조했고 **전부 맞다.**

- 에뮬레이터 로그의 syscall 번호 ↔ win32u 스텁 `.asm`: `NtUserGetObjectInformation` `MOV EAX,0x1065` (`+0x3`), `NtUserGetProcessWindowStation` `0x101f`, `NtUserGetThreadDesktop` `0x107b` — 요청서 번호와 같다.
- user32 썽크가 뛰는 가져오기 칸 ↔ `llvm-readobj --coff-imports` 이름 (win32u 가져오기 표 시작 `0xB5CD0` + 칸 번호 × 8): `0xb6d90` = 536번째 = `NtUserGetObjectInformation` · `0xb6db8` = 541번째 = `NtUserGetThreadDesktop` · `0xb6e00` = 550번째 = `NtUserGetProcessWindowStation`.
- combase 지연 가져오기 칸 ↔ 같은 도구: `ext-ms-win-ntuser-windowstation-l1-1-0.dll` 표 시작 `0x363568` → `+0x0` `GetThreadDesktop`, `+0x8` `GetProcessWindowStation`, `+0x10` `GetUserObjectInformationW` — `.asm` 의 `CALL [0x180363568/570/578]` 과 같다. API 세트 이름은 `apisetschema.dll` 에서 `ext-ms-win-ntuser-windowstation-l1-1-2 → user32.dll` (요청 `l1-1-0` 이 `l1-1-2` 로 풀리는 버전 허용은 ntdll 코드를 안 봐서 `[추측]`, sast/128 과 같은 자리).
- `PEB+0x58` = `KernelCallbackTable` (PDB `_PEB`), `RTL_USER_PROCESS_PARAMETERS+0xc0` = `DesktopInfo` (PDB `_RTL_USER_PROCESS_PARAMETERS`) — combase `GetThreadWinstaDesktop +0x5a`, `ResolveProcessDesktopNoUser +0x1a/+0x23` 이 읽는 칸과 같다. user32 가 그 칸을 쓰는 줄: `_UserClientDllInitialize +0x3fc MOV [RAX+0x58],RBX` (RBX = `+0x3ee LEA RBX,[apfnDispatch]`).
- win32k: win32kbase 가 부르는 함수 표 칸 `[표+0x388]` · `[표+0x390]` 가 win32kfull 의 `ext_ms_win_moderncore_win32k_base_ntuser_l1_table` (rva `0x3b4c70`) `+0x388` (= `0x3b4ff8`, 값 `0x1401ed1d0` = `xor eax,eax; ret` 공용 조각) · `+0x390` (= `0x3b5000`, 값 `0x1400bfc60` = `EditionGetUserObjectInformationEntryPoint`) 와 **칸 번호까지 일치** (`refs` 의 `ptr` 줄 + 표 바이트 읽기). 이름·export 표 (`llvm-readobj --coff-exports`: win32kbase 가 `NtUserGetObjectInformation` 을, win32kfull 이 `EditionGetUserObjectInformationEntryPoint` 를 export) 도 같다.
- 에뮬레이터가 본 `0x8007000e` / `0x8007007a` 가 아래 2 절 규칙으로 그대로 나온다 (두 값의 줄이 `.asm` 에 있음) — 스크립트 오프셋 셈과 바이너리가 에뮬레이터 로그와 어긋나지 않는다는 근거 (진짜 Windows 관찰은 아님).

---

## 먼저 읽는 한 줄 답

| # | 한 줄 답 |
|---|---|
| 1a | **예 — `GetUserObjectInformationW` 는 6 B `JMP [가져오기 칸]` 썽크이고 그 칸은 `win32u!NtUserGetObjectInformation` 이다.** 코드는 이게 전부 (`W.asm +0x0 JMP [0x1800b6d90]`). 다섯 인자가 그대로 syscall `0x1065` 로 간다. user32 는 반환값도 LastError 도 안 건드린다. |
| 1b | **`A` 는 종류 2·3 일 때만 끼어든다.** 그 밖 종류 (1·4·5…) 는 다섯 인자 그대로 `NtUserGetObjectInformation` 을 부르고 반환도 그대로 (`A.asm +0xd7~+0xf0`). 종류 2·3 은 **임시 버퍼 `nLength × 2` 바이트 (0 으로 채움)** 에 UTF-16 으로 받고 (`+0x41` `RtlAllocateHeap(pUserHeap, 8, nLength*2)` · `+0x69` 호출), 성공이면 **NUL 까지 세어** (`+0x96~+0x9f`) `RtlUnicodeToMultiByteN` 으로 ANSI 로 바꿔 호출자 버퍼에 쓴다 (`+0x105`). `lpnLengthNeeded` 는 **바이트** 이고 (UTF-16 바이트) A 는 성공 때 그 값을 **반으로** 줄인다 (`+0x82 SHR [RBP],1`). 반환은 커널 값 그대로, **user32 는 LastError 를 안 쓴다** (이 함수에 `RtlSetLastWin32Error` 호출 없음 — `_calls.tsv`). 실패 (반환 0) 면 변환·반감 없이 임시 버퍼만 풀고 0 반환. |
| 2a | **combase 는 `GetThreadWinstaDesktop` 에서 `GetProcessWindowStation` → `GetThreadDesktop` → `GetUserObjectInformationW(hWinsta, 2, 0x40 B 버퍼, 0x40, &len)` → `GetUserObjectInformationW(hDesk, 2, 0x40 B 버퍼, 0x40, &len)` 로 딱 두 번 묻는다** (종류는 둘 다 2). 각 호출이 실패하고 `GetLastError() == 0x7a` 이면 `len` (커널이 써 준 바이트 수) 만큼 `HeapAlloc` 해 **그 호출만 한 번 더** (최대 네 번). 종류 1 (`0xc`) · `0x208` · 길이 0 은 **이 함수의 줄이 아니다** — combase 에서 `GetUserObjectInformationW` 를 부르는 곳은 이 함수 한 곳뿐 (`refs 0x363578`). |
| 2b | **실패 HRESULT 는 `GetLastError` 변환 + 고정값 몇 개.** 변환: 값 > 0 이면 `0x80070000 \| (값 & 0xffff)`, ≤ 0 이면 그대로 (0 이면 0). 호출이 FALSE 인데 `GetLastError()` 가 **0** 이고 글이 아직 없으면 → **`0x8007000e`** (`+0x1f2 JZ` → `+0x238 MOV EBX,0xe`). `GetLastError()` 가 0x7a 로 남아 있으면 다시 불러도 0x7a → **`0x8007007a`**. 고정값: 메모리 못 얻음 `0x8007000e` · 글 합치기 실패 `0x8007006f` · user32 없음 + win32k 호출 금지 정책 `0x800704ec`. |
| 2c | **성공 글 = `<창 스테이션 이름>\<데스크톱 이름>`** (예 `WinSta0\Default`). `HeapAlloc((len(A)+len(B)+2) × 2)` 에 `StringCchCopyW` · `StringCchCatW(L"\\")` · `StringCchCatW` (`+0x13a~+0x1a1`). 결과 포인터는 전역 `CRpcResolver::_pwszWinstaDesktop` (rva `0x338118`) 에 두고 (`+0x146`), 부르는 쪽 (`GetConnection` 안 람다 `operator()`) 이 RPCSS 쪽 RPC 클라이언트 스텁 **`Connect` 의 둘째 인자** 로 넘긴다 (`operator()+0x742` → `+0x82e`). |
| 2d | **핸들이 0 이면 이름을 안 묻는다 (r13 과 맞음).** `GetProcessWindowStation` 이 0 → `GetLastError` 가 0 이면 **반환 0, 글 NULL 로 두고** (`+0x23f~+0x24f`, `+0x2ce`) 부르는 쪽이 NULL 을 `Connect` 에 그대로 넘겨 RPC 연결로 간다. 0 이 아니면 `0x8007xxxx` 로 **실패 반환 → 부르는 쪽 (`GetConnection` → `CoInitializeSecurity`) 이 그 값으로 멈춘다 (RPC 연결 안 감)** — r14 의 `0x8007000e` 와 맞음. |
| 3a | **종류 1 (12 B) = `{fInherit, fReserved, dwFlags}`.** `fInherit` = 핸들 속성의 `OBJ_INHERIT(0x2)` 비트, `fReserved` = 0, **`dwFlags` = 창 스테이션이면 `1` (= 공개 상수 `WSF_VISIBLE`) — 단 창 스테이션 객체 `+0x40` 의 비트 `0x4` 가 꺼져 있을 때만** (그 비트가 켜지면 0). 필요 길이는 항상 `0xc`, 모자라면 FALSE + LastError `0x7a` + `*lpnLengthNeeded = 0xc` (`_GetUserObjectInformation +0x2ed~+0x34a`). |
| 3b | **종류 2 = 객체 이름 (널 포함, UTF-16)**: 객체 관리자 머리의 `Name` (이름 끝 조각만 — 디렉터리 경로 아님) 을 `Length` 바이트 복사하고 뒤에 `0x0000` 을 붙인다. **`*lpnLengthNeeded = Length + 2` (바이트, 널 포함)**. `Length + 2 > nLength` 이면 **FALSE + LastError `0x7a` (122, `ERROR_INSUFFICIENT_BUFFER`) + `*lpnLengthNeeded` 는 쓴다** (`+0x146~+0x178`, `+0x18a`, `+0x37e`). 즉 `0x7a` 맞다. 창 스테이션 `WinSta0` 는 `14 + 2 = 16` 바이트, 데스크톱 `Default` 도 16 바이트. |
| 3c | **본문은 win32kbase 가 아니라 win32kfull.** `win32kbase!NtUserGetObjectInformation` (135 B) 은 함수 표를 두 번 읽는 껍데기 (`+0x388` 문지기 → `+0x390` 본체) 이고, 본체는 `win32kfull!EditionGetUserObjectInformationEntryPoint` (프로브 · 핸들 잡기 · 락) → `_GetUserObjectInformation` (종류별 갈래). LastError 는 **커널이 `TEB+0x68` 에 직접 쓴다** (`UserSetLastError +0x21 MOV [RAX+0x68],EBX`) — user32 는 안 건드림 (1b 와 이어짐). |
| 우리 | **`NtUserGetObjectInformation` 기본 대답 0 · 버퍼 안 씀은 틀림** (아래 "지금 우리"). 핸들 둘 (`0x20000` · `0x20400`) 은 combase 가 0 인지만 보므로 맞다. |

---

## 1 — user32 `GetUserObjectInformationW` / `A`

### 1-1. W — 썽크

`ms/disassemble/user32/GetUserObjectInformationW.asm`:

```
+0x0  180072820  JMP qword ptr [0x1800b6d90]
```

- 0x1800b6d90 (rva `0xb6d90`) = user32 가져오기 칸. 읽는 곳 셋: `A +0x69` · `A +0xe4` · `W +0x0` (`refs 0xb6d90`). 이름은 `llvm-readobj --coff-imports` 로 `win32u.dll` 표의 536번째 = **`NtUserGetObjectInformation`** (버전 확인 절). win32u 쪽 스텁은 `MOV R10,RCX · MOV EAX,0x1065 · SYSCALL` (`win32u/NtUserGetObjectInformation.asm +0x0 ~ +0x12`).
- 그래서 W 에서 user32 가 하는 일은 **없다**: 다섯 인자 (RCX, RDX, R8, R9, 스택 다섯째) 가 그대로 syscall 로 가고, 커널이 준 반환값이 그대로 돌아온다. `SetLastError` 같은 것도 없다.

### 1-2. 표 — `NtUserGetObjectInformation` (win32u `0x1065`, 인자 **5개**)

`GetUserObjectInformationA` 가 부르는 자리: 종류 2·3 → `+0x69` (call) / `+0x70` (ret). 그 밖 종류 → `+0xe4` / `+0xeb`. W 는 위 썽크.

| syscall 인자 | 차례 | 뜻 (문서 상의 GetUserObjectInformation 과 같은 자리) | 입력/출력 | 단위 | 줄 (A 판) |
|---|---|---|---|---|---|
| 1 `RCX→R10` | `hObj` | 창 스테이션 또는 데스크톱 핸들 | 입력 | 핸들 | A 는 `R15 = RCX` 로 보관 (`+0x22`), 호출 때 `MOV RCX,R15` (`+0x63`) |
| 2 `RDX` | `nIndex` | 종류. 1 = FLAGS, 2 = NAME, 3 = TYPE, … (A 가 보는 것은 **2·3 이냐** 뿐: `+0x16 LEA EAX,[RDX-2]` · `+0x25 CMP EAX,1` · `+0x28 JA +0xd7`) | 입력 | — | `R14D = EDX` (`+0x1f`), `MOV EDX,R14D` (`+0x60`) |
| 3 `R8` | `pvInfo` | 출력 버퍼. A 는 **자기 임시 버퍼** (`RtlAllocateHeap` 결과) 를 넘긴다 (`+0x58 MOV R8,RAX`). 그 밖 종류 (+0xd7 갈래) 는 호출자 버퍼 그대로 | **출력** | 바이트 | `+0x1c MOV R12,R8` (호출자 버퍼 보관) |
| 4 `R9` | `nLength` | 버퍼 크기. 2·3 이면 A 가 **`2 × nLength`** 로 키워 넘긴다 (`+0x35 LEA EBX,[R9+R9*1]` · `+0x55 MOV R9D,EBX`) — ANSI 1 바이트가 UTF-16 2 바이트라서. 그 밖은 그대로 | 입력 | **바이트** | `+0x19 MOV EDI,R9D` |
| 5 `[RSP+0x20]` (호출 때) | `lpnLengthNeeded` | 필요 바이트 수를 돌려받는 포인터. **A 는 자기 것이 아니라 호출자 포인터를 그대로 넘긴다** (`+0x4d MOV RBP,[RSP+0x80]` · `+0x5b MOV [RSP+0x20],RBP`) | **출력** (커널이 씀) | **바이트** (A 가 성공 때 반으로: `+0x7d TEST RBP,RBP` · `+0x82 SHR dword ptr [RBP],1`) | |

- 값 풀이: 요청서의 `(0x20000, 2, buf, 0x40)` 은 combase `GetThreadWinstaDesktop +0xc8` 의 호출 꼴 (W 를 직접 부름, `nLength = 0x40`) 과 **그대로 맞는다.** 같은 꼴이 A 를 거쳐 나오려면 호출자가 `nLength = 0x20` 을 줬어야 하는데 (A 가 2 배로 키움), combase 는 A 를 안 부른다. 로그의 `0x208` 이 W 직접인지 A 를 거친 `0x104` 인지는 정적 자료로 못 가린다 `[공백]` (2-2 의 설명 참고).

### 1-3. A 의 돌아온 뒤 — 읽는 것

| 갈래 | 줄 | 하는 일 |
|---|---|---|
| 종류 ∉ {2, 3} | `+0xd7 MOV RAX,[RSP+0x80]` · `+0xdf MOV [RSP+0x20],RAX` · `+0xe4 CALL` · `+0xf0 JMP +0xbf` | 다섯 인자 그대로 넘기고 반환 EAX 를 그대로 돌려줌 (`+0xbd MOV EAX,EBX` 를 안 거치고 복귀 줄 `+0xbf` 로 바로 — EAX 가 커널 값 그대로). |
| 반환 0 (실패) | `+0x75 XOR ECX,ECX` · `+0x77 MOV EBX,EAX` · `+0x79 TEST EAX,EAX` · `+0x7b JZ +0xa5` | `lpnLengthNeeded` 반감도 변환도 **건너뜀** → 임시 버퍼 `RtlFreeHeap` (`+0xb1`) → 0 돌려줌. 그래서 **실패 때 `*lpnLengthNeeded` 는 UTF-16 바이트 그대로** (반감 안 됨) — combase 가 이 값으로 `HeapAlloc` 하는 건 W 직접 호출이라 무관. |
| 반환 ≠ 0 (성공) | `+0x7d~+0x82` | `lpnLengthNeeded` 가 NULL 이 아니면 `SHR [RBP],1` (바이트 → 글자 수). |
|  | `+0x85 MOV [RSP+0x68],ECX(0)` · `+0x89 TEST EDI,EDI` · `+0x8b JZ +0xa5` · `+0x8d TEST RSI,RSI` · `+0x90 JZ +0xa5` | 호출자 `nLength` 가 0 이거나 임시 버퍼가 NULL 이면 변환을 **건너뜀** (그래도 성공 값 그대로 돌려줌). |
|  | `+0x92 OR RAX,-1` · `+0x96 INC RAX` · `+0x99 CMP word ptr [RSI+RAX*2],CX` · `+0x9d JNZ +0x96` · `+0x9f INC EAX` | 임시 버퍼를 **글자 (2 바이트) 단위로 NUL 이 나올 때까지** 세고 하나 더 (`INC EAX`) = **NUL 포함 글자 수**. → 커널이 **NUL 로 끝나는 UTF-16** 을 쓴다고 보고 읽는다. (커널이 성공할 때는 항상 NUL 을 같이 쓴다 — 3-4 — 그래서 이 세기는 `2×nLength` 안에서 끝난다. NUL 없이 버퍼를 꽉 채워 주는 stub 은 이 세기가 임시 버퍼 끝을 넘어 읽으니 **반드시 NUL 을 붙일 것.**) |
|  | `+0xa3 JNS +0xf2` · `+0xf2 ADD EAX,EAX` · `+0xfc MOV [RSP+0x20],EAX` · `+0xf9 MOV R9,RSI` · `+0xf4 LEA R8,[RSP+0x68]` · `+0x100 MOV EDX,EDI` · `+0x102 MOV RCX,R12` · `+0x105 CALL RtlUnicodeToMultiByteN` | `RtlUnicodeToMultiByteN(호출자 버퍼 R12, 호출자 nLength, &바이트수 (로컬), 임시 버퍼, 2 × (NUL 포함 글자 수))` — **NUL 까지 변환.** 이 함수의 반환은 **무시.** |
| 끝 | `+0xa5 MOV RCX,[pUserHeap]` · `+0xb1 CALL RtlFreeHeap` · `+0xbd MOV EAX,EBX` · `+0xd5 RET` | 임시 버퍼 해제 후 **커널이 준 반환 (EBX) 그대로**. |

- **LastError: user32 가 정하지 않는다.** A 의 호출 목록 (`ms/disassemble/user32/_calls.tsv`): `RtlAllocateHeap` · `NtUserGetObjectInformation` ×2 · `RtlFreeHeap` · `RtlUnicodeToMultiByteN` 뿐. `RtlSetLastWin32Error` 도 `SetLastError` 도 없다. 실패 때 `TEB.LastErrorValue` 는 **커널이 쓴 값** (3 절에서 확인) 그대로 호출자에게 보인다.
- `RtlAllocateHeap` 결과 NULL 검사는 **없다** (`+0x48~+0x58`) — 힙이 모자라면 NULL 버퍼가 커널에 간다. 이 요청과는 무관.
- **우리가 알아야 할 것 (한 줄):** 에뮬레이터가 부르는 쪽은 combase 의 W 직접 호출이라, **A 의 UTF-16 → ANSI 변환은 이번 길에 안 탄다.** stub 은 W 계약 (UTF-16, NUL 포함, 바이트 단위) 만 맞추면 된다.

---

## 2 — combase `GetThreadWinstaDesktop` (+ `ResolveProcessDesktopNoUser` · `SetExplicitWinstaDesktopString`)

### 2-0. 이름·자리 (PDB 이름으로 확인)

| PDB 이름 | rva | 크기 | 맡은 일 |
|---|---|---|---|
| `?GetThreadWinstaDesktop@CRpcResolver@@QEAAJXZ` | `0x82330` | 909 | 창 스테이션·데스크톱 이름 글을 만들어 `CRpcResolver::_pwszWinstaDesktop` 에 둔다. **전역에 쓰는 함수** (반환은 HRESULT). 조각 (`FUN_1800824e3` · `FUN_18025b325` · `FUN_18025b3d3`) 과 떨어진 범위 (`+0x1d8eb6~`) 가 많아 `.asm` 오프셋이 `+0x1d8…` 로 보이는 줄이 있다 (같은 함수) |
| `?ResolveProcessDesktopNoUser@CRpcResolver@@QEAAKXZ` | `0x82a04` | 248 | user32 없이 프로세스 매개변수 `DesktopInfo` 로 글을 정한다. 반환은 **Win32 오류값** (0 성공 · `0x32` = "이 길은 못 함, 핸들로 물어라" · `0xe` 메모리) |
| `?SetExplicitWinstaDesktopString@@YAJ_K@Z` (Ghidra 이름 `SetExplicitWinstaDesktopString`) | `0x175114` | 188 | 전역 `g_explicitWinstaDesktopString` (rva `0x338580`) 에 글을 정해 둔다 |
| `IsGetProcessWindowStationPresent` | `0x179470` | 78 | API 세트 `ext-ms-win-ntuser-windowstation-l1-1-0` 있나 (`ApiSetExists_…` 칸 1 = 있음, 2 = 없음, 0 = 미정) |
| `GetConnection` (`?GetConnection@CRpcResolver@@QEAAJXZ`) | `0xa14d0` | 366 | 람다 `operator()` (rva `0x18b9c0`, 3,558 B) 를 부른다 (`+0xbe` call / `+0xc3` ret) |
| `Connect` | `0x12f318` | — | RPCSS 로 가는 RPC 클라이언트 스텁. 부르는 곳 `operator() (0x18b9c0) +0x82e` 하나 (`refs 0x12f318`) |

### 2-1. 큰 갈래 (`GetThreadWinstaDesktop`)

`ms/disassemble/combase/GetThreadWinstaDesktop.asm` — 오프셋은 함수 시작 기준. 끝의 `+0x1ff MOV EAX,EBX` 는 반환값을 `EBX` 에서 옮기는 줄이고, `+0x201` 부터 쿠키 검사·복귀 (`+0x22d RET`).

| 차례 | 하는 일 | 줄 |
|---|---|---|
| ① 정해 둔 글이 있나 | `g_explicitWinstaDesktopString != NULL` 이면 그것을 `_pwszWinstaDesktop` 에 넣고 **0 반환** (핸들 안 물음) | `+0x31 MOV RAX,[g_explicit…]` · `+0x3b TEST RAX,RAX` · `+0x3e JNZ +0x2d2` → `+0x2d2 MOV [_pwszWinstaDesktop],RAX` · `+0x2d9 XOR EAX,EAX` · `+0x2db JMP +0x201` |
| ② user32 가 없는 프로세스인가 | `IsGetProcessWindowStationPresent()` 가 0 **또는** `PEB.KernelCallbackTable (PEB+0x58) == 0` 이면 `ResolveProcessDesktopNoUser()` 를 부름 | `+0x44` (ret `+0x49`) · `+0x4b JZ +0x27e` · `+0x51 MOV RAX,GS:[0x60]` · `+0x5a CMP [RAX+0x58],R12` · `+0x5e JZ +0x27e` |
| ②' 그 결과 | `0x32` 면 ③ 으로 (`+0x283 CMP EAX,0x32` · `+0x286 JZ +0x64`). `≤ 0` 이면 **그 값 그대로 반환** (`+0x28e JLE`). `> 0` 이면 `0x80070000 \| (값 & 0xffff)` 반환 (`+0x294 MOVZX` · `+0x297 OR`) | `+0x27e CALL` (ret `+0x283`) ~ `+0x29c JMP` |
| ③ 창 스테이션 핸들 | `GetProcessWindowStation()` → `RDI` | `+0x64 CALL [0x180363570]` (ret `+0x6b`) · `+0x70 MOV RDI,RAX` · `+0x73 TEST RAX,RAX` · `+0x76 JZ +0x23f` |
| ④ 데스크톱 핸들 | `GetCurrentThreadId()` → `GetThreadDesktop(tid)` → `R14` | `+0x7c` (ret `+0x83`) · `+0x8a CALL [0x180363568]` (ret `+0x91`) · `+0x96 MOV R14,RAX` · `+0x9c JZ +0x31c` |
| ⑤ 창 스테이션 이름 | `GetUserObjectInformationW(hWinsta, 2, bufA, 0x40, &len)`, `len` 은 먼저 `0x40` | `+0xa2 MOV EBX,0x40` · `+0xae MOV [RBP-0x59],EBX` · `+0xb1 LEA R8,[RBP-0x49]` (bufA, 0x40 B) · `+0xc1 LEA EDX,[RBX-0x3e]` (= **2**) · `+0xc8 CALL [0x180363578]` (ret `+0xcf`) |
| ⑥ 데스크톱 이름 | `GetUserObjectInformationW(hDesk, 2, bufB, 0x40, &len)`, `len` 다시 `0x40` | `+0xe0 MOV [RBP-0x59],EBX` · `+0xe3 MOV R9D,EBX` · `+0xeb LEA R8,[RBP-0x9]` (bufB, 0x40 B) · `+0xef MOV EDX,2` · `+0xf4 MOV RCX,R14` · `+0xf7 CALL` (ret `+0xfe`) |
| ⑦ 글 만들기 | NUL 까지 글자 수 세기 (`bufB` → `RCX`, `bufA` → `RAX`), `cch = len(A) + len(B) + 2`, `HeapAlloc(g_hHeap, 0, cch × 2)` → `_pwszWinstaDesktop` | `+0x10b~+0x124` · `+0x126 LEA RBX,[RAX+2]` · `+0x12c ADD RBX,RCX` · `+0x136 LEA R8,[RBX+RBX]` · `+0x13a CALL HeapAlloc` (ret `+0x141`) · `+0x146 MOV [_pwszWinstaDesktop],RAX` |
| ⑧ 글 채우기 | `StringCchCopyW(dst, cch, bufA)` → `StringCchCatW(dst, cch, L"\\")` (`.rdata 0x1802cb830` = `\`) → `StringCchCatW(dst, cch, bufB)`. 셋 중 하나라도 음수면 `HeapFree` + `_pwszWinstaDesktop = NULL` + 오류값 **`0x6f`** | `+0x169` (ret `+0x16e`) · `+0x17d LEA R8,[0x1802cb830]` · `+0x187` (ret `+0x18c`) · `+0x1a1` (ret `+0x1a6`) · 실패 `+0x170/+0x18e/+0x1a8 JS +0x2a1` → `+0x2b1 MOV EBX,0x6f` · `+0x2b6 CALL HeapFree` · `+0x2c2 MOV [_pwszWinstaDesktop],R12` |
| ⑨ 성공 | `EBX = 0` | `+0x1ae MOV EBX,R12D` · `+0x1b1 JMP +0x1d6` |
| ⑩ 정리·반환 | 임시 힙 버퍼 (bufA·bufB 를 대신한 것) `HeapFree`. 그리고 **`EBX` (Win32 오류값) → HRESULT**: `EBX == 0` 이면 `_pwszWinstaDesktop` 이 있으면 `0`, **없으면 `0xe` → `0x8007000e`**; `EBX < 0` 이면 그대로; `EBX > 0` 이면 `0x80070000 \| (EBX & 0xffff)` | `+0x1d6~+0x1ea` (`HeapFree` call `+0x3ee` / `+0x40b`) · `+0x1f0 TEST EBX,EBX` · `+0x1f2 JZ +0x22f` → `+0x22f CMP [_pwszWinstaDesktop],R12` · `+0x236 JNZ +0x1ff` · `+0x238 MOV EBX,0xe` · `+0x23d JMP +0x1f6` · `+0x1f4 JLE +0x1ff` · `+0x1f6 MOVZX EBX,BX` · `+0x1f9 OR EBX,0x80070000` · `+0x1ff MOV EAX,EBX` |

### 2-2. 표 — 묻는 것 (`차례 · 부르는 것 · 핸들 · 종류 · 버퍼 크기 · 실패 때 · 돌려주는 값 · 줄`)

| 차례 | 부르는 것 | 핸들 | 종류 | 버퍼 크기 (`nLength`) · `lpnLengthNeeded` | 실패 때 | 돌려주는 값 | 줄 |
|---|---|---|---|---|---|---|---|
| a | `GetProcessWindowStation()` | — | — | — | 0 → `GetLastError` 를 `EDX` 에. **0 이면 `EBX = 0` → 반환 0** (글 안 만듦, `_pwszWinstaDesktop` 그대로) · 음수면 그대로 · 양수면 `0x8007xxxx`. 값이 `0x7f` (지연 로딩 실패) 면 `GetModuleHandleW(L"user32.dll")` → 없고 win32k 호출 금지 정책 (`GetProcessMitigationPolicy(…, 4, …)` 의 0 비트) 이면 **`0x800704ec`**, 정책이 아니면 `DiagnoseUser32LoadFailure()` 값, user32 가 있으면 추적 글만 남기고 `0x8007007f` | 핸들 (`RDI`) | `+0x64 / +0x6b` · `+0x76 JZ +0x23f` · `+0x23f~+0x25d` · far `+0x1d8eb6~+0x1d8fb6` (`+0x1d8f24 MOV EBX,0x800704ec`) |
| b | `GetThreadDesktop(GetCurrentThreadId())` | — | — | — | 0 → `GetLastError` → `EBX` → `+0x1f4` 규칙 (0 이면 0, 양수면 `0x8007xxxx`) | 핸들 (`R14`) | `+0x7c / +0x83` · `+0x8a / +0x91` · `+0x9c JZ +0x31c` · `+0x31c~+0x353` · far `+0x1d8fe7~+0x1d8ff0` |
| c | `GetUserObjectInformationW` **(1)** | hWinsta (`RDI`) | **2** (`NAME`) | `bufA` **0x40 바이트** (32 글자), `nLength = 0x40`, `&len` 에 먼저 `0x40` | FALSE → `GetLastError`. **`0x7a`** 이면 (d) 로. 그 밖 → 추적 글 후 `EBX = 그 값` → ⑩ | 성공이면 `bufA` 에 NUL 끝 UTF-16 | `+0xc8 / +0xcf` · `+0xd4 TEST EAX,EAX` · `+0xd6 JZ +0x358` · `+0x358 CALL GetLastError` (ret `+0x35f`) · `+0x364 MOV EBX,EAX` · `+0x366 CMP EAX,0x7a` · `+0x369 JZ +0x1d9007` |
| d | (c) 가 `0x7a` 로 실패하면 `HeapAlloc(g_hHeap, 0, len)` 후 **(c) 를 한 번 더**: `GetUserObjectInformationW(hWinsta, 2, 힙 버퍼, len, &len)` | hWinsta | 2 | **`len` = 첫 호출에서 커널이 `&len` 에 써 준 값 (바이트)** 을 크기로 쓴다 | `HeapAlloc` 실패 → `EBX = 0xe` (`+0x152`) → **`0x8007000e`**. 둘째 호출도 FALSE 면 `GetLastError()` → `EBX` (그 값이 또 `0x7a` 면 **`0x8007007a`**, 0 이면 **`0x8007000e`**) | 성공이면 `+0x1d9099 MOV EBX,0x40` 후 (e) 로 | far `+0x1d9007~+0x1d9099` (`HeapAlloc` call `+0x1d9014` · 재호출 `+0x1d9044` ret `+0x1d904b` · `+0x1d9052 JNZ +0x1d9099` · 실패 `+0x1d9054` GetLastError) |
| e | `GetUserObjectInformationW` **(2)** | hDesk (`R14`) | **2** | `bufB` **0x40 바이트**, `nLength = 0x40`, `&len` 에 다시 `0x40` | (c) 와 같은 갈래 (`+0x39d CALL GetLastError` · `+0x3ab CMP EAX,0x7a` · `+0x3ae JZ +0x1d90ae`) | 성공이면 `bufB` 에 NUL 끝 UTF-16 | `+0xf7 / +0xfe` · `+0x103 TEST EAX,EAX` · `+0x105 JZ +0x39d` |
| f | (e) 도 `0x7a` 면 한 번 더 | hDesk | 2 | `len` 바이트 | (d) 와 같음 | | far `+0x1d90ae~+0x1d9138` (`HeapAlloc` call `+0x1d90bb` · 재호출 `+0x1d90eb` ret `+0x1d90f2` · `+0x1d90f9 JNZ +0x10b`) |

- **종류 1 (`0xc`) · `0x208` · 길이 0 은 이 함수에 없다.** `GetUserObjectInformationW` 지연 가져오기 칸 (`0x363578`) 을 읽는 곳은 `GetThreadWinstaDesktop` 의 위 네 곳 (`+0xc8` · `+0xf7` · `+0x1d9044` · `+0x1d90eb`) 과 지연 로딩 발판 `DelayLoad_GetUserObjectInformationW` 뿐 (`refs 0x363578`). → 로그의 `(…, 1, buf, 0xc)` (353) · `(0x20400, 2, buf, 0x208)` (1) 는 combase 가 아닌 다른 모듈의 호출이다 `[공백: 어느 모듈인지는 정적 자료에 없음]`. `[추측]` `(…, 1, buf, 0xc)` 는 USEROBJECTFLAGS 12 B 를 읽는 흔한 "대화형 창 스테이션인가" 검사 꼴 (`.NET` 런타임 쪽일 수 있음 — 이 레포에 없어 못 봄).
- 길이 0 (`(0x20000, 2, buf, 0)`, 8 샘플) 은 combase 가 **재호출 길에서** 만들 수는 있다: 재호출의 `nLength` 는 첫 호출이 `&len` 에 써 준 값이라 (`+0x1d902c MOV R9D,[RBP-0x59]`), **stub 이 `*lpnLengthNeeded` 에 0 을 써 주면** `HeapAlloc(…, 0)` + `(hWinsta, 2, buf, 0, &len)` 이 된다 `[추측: 에뮬레이터 stub 이 0 을 썼는지는 모름]`.

### 2-3. `ResolveProcessDesktopNoUser` — 핸들 없이 글 정하기

`ms/disassemble/combase/ResolveProcessDesktopNoUser.asm`. (`GetThreadWinstaDesktop` 에서 ② 의 두 조건 중 하나일 때만 탄다.)

| 차례 | 하는 일 | 줄 |
|---|---|---|
| 1 | `PEB.ProcessParameters` (`PEB+0x20`) 가 있으면 `RBX = &DesktopInfo` (`+0xc0`). 길이 `≥ 2` 바이트면 3 으로 | `+0xf MOV RAX,GS:[0x60]` · `+0x1a MOV RBX,[RAX+0x20]` · `+0x23 ADD RBX,0xc0` · `+0x2a LEA EDI,[RSI+2]` · `+0x2d CMP word [RBX],DI` · `+0x30 JNC +0x51` |
| 2 | 아니면 `IsGetProcessWindowStationPresent()` — **있으면 `0x32` 반환** (핸들로 물으라는 뜻), 없으면 기본 글 `WinSta0\Default` 를 두고 0 반환 | `+0x32 CALL` (ret `+0x37`) · `+0x39 JZ +0x6e` · `+0x3b MOV EAX,0x32` · `+0x6e LEA RAX,[0x1802c24e0]` · `+0x75 MOV [_pwszWinstaDesktop],RAX` |
| 3 | `RtlEqualUnicodeString(&DesktopInfo, &uszDefaultDesktopName, TRUE)` — **대소문자 무시** (`+0x51 MOV R8B,1`). 같으면 기본 글 (`.rdata 0x1802c24e0` = **`WinSta0\Default`**, 대문자 S) 을 `_pwszWinstaDesktop` 에 두고 0 반환 | `+0x54 LEA RDX,[0x1802791a0]` (`uszDefaultDesktopName`, 길이 30 B = `WinSta0\Default`) · `+0x5e CALL RtlEqualUnicodeString` (ret `+0x65`) · `+0x6c JZ +0x80` (다르면 4 로) · `+0x6e~+0x7c` |
| 4 | 다르면 `DesktopInfo` 안에서 `\` (`0x5c`) 를 찾는다. **첫 글자가 아닌 자리에 있으면** `DesktopInfo` 를 **그대로 복사** (`HeapAlloc(길이+2)` · `memcpy` · NUL) 해 `_pwszWinstaDesktop` 에 두고 0 반환. 없으면 2 의 `IsGetProcessWindowStationPresent` 갈래 (`+0x32`) 로 | `+0x80~+0xa0` · `+0xa2 MOV RCX,[g_hHeap]` · `+0xa9 LEA R8,[RDI+RDX]` · `+0xaf CALL HeapAlloc` (ret `+0xb6`) · `+0xbb MOV [_pwszWinstaDesktop],RAX` · `+0xd5 CALL memcpy` (ret `+0xda`) · `+0xe0 MOV [RDI+RAX*2],SI(0)` · 할당 실패 `+0xe6 MOV EBX,0xe` → `+0xf2 MOV EAX,EBX` |

- 반환은 **Win32 오류값** (`0x32` / `0` / `0xe`) 이고 HRESULT 변환은 부르는 쪽 `GetThreadWinstaDesktop +0x28e~+0x297`.
- **run44 `DesktopInfo = Winsta0\Default` 와의 관계:** 이 값은 대소문자 무시 비교로 `WinSta0\Default` 와 **같다** → ② 의 조건에 걸려 `ResolveProcessDesktopNoUser` 로 들어오면 combase 의 상수 `WinSta0\Default` 가 쓰인다 (프로세스 매개변수 글이 아니라 **combase 자기 상수** 의 대소문자). **그런데 에뮬레이터 로그는 `NtUserGetProcessWindowStation` 까지 갔으므로** 이 갈래를 안 탔다 → 에뮬레이터 PEB 의 `KernelCallbackTable` 이 0 이 아니라고 읽힌다 `[추측 — 로그에서 거꾸로 읽음]` (user32 `_UserClientDllInitialize +0x3fc` 가 쓰는 칸).

### 2-4. 글이 어디로 가나 + 부르는 쪽이 실패를 받으면

`ms/disassemble/combase/_oneoff/` 의 일회용 objdump. 사슬 (모두 `[정적]`, 호출 줄):

```
CoInitializeSecurity +0x47d (ret +0x482)  [gGotSecurityData == 0 일 때: +0xa6 CMP [0x180338290],EDI · +0xac JE +0x47d]
  → CRpcResolver::GetConnection (rva 0xa14d0) +0xbe (ret +0xc3)
    → 람다 operator() (rva 0x18b9c0, 3558 B)
        +0x80  CMP [_pwszWinstaDesktop],0 · +0x92 JNE +0x100      ← 글이 이미 있으면 아래 호출을 건너뜀
        +0x94  CALL GetThreadWinstaDesktop (ret +0x99)
        +0x99  MOV EDI,EAX · +0x9b TEST EAX,EAX · +0x9d JNS +0x100
                ↓ 음수 (실패)                           ↓ 0 이상 (성공 또는 "글 NULL 인 0")
        +0x9f~ Return_Hr(줄 725, hr) (call +0xb5) → 뒷정리 → EDI 반환      +0x100~ memset(buf, 0, 0x410) · GetModuleFileNameW(NULL, buf, 519) (call +0x123) · 힙 객체 · …
                                                              +0x742 MOV RDX,[_pwszWinstaDesktop]
                                                              +0x82e CALL Connect (rva 0x12f318) (ret +0x833)
```

- **글이 가는 곳:** `operator()+0x742 MOV RDX,[0x180338118]` (읽기) → `+0x82e CALL Connect` — **RPC 클라이언트 스텁 `Connect` 의 둘째 인자 (RDX).** 첫째 `RCX = [R14]` (람다 캡처 속 포인터 — 바인딩 핸들로 보이나 `[추측]`), 셋째 `R8 = RBP+0x150` = 바로 위에서 `GetModuleFileNameW(NULL, …)` 로 채운 **실행 파일 경로 버퍼**. NULL 도 그대로 넘어간다 (`[unique]` 문자열 꼴 `[추측]`). `Connect` 안에서 `\RPC Control\epmapper` 로 가는 길 (ept_map) 은 run129·run133 몫 — 이름만.
- **같은 글을 쓰는 다른 곳 (읽기만, `refs 0x338118`):** `DelegateActivationToSCM +0x1a1` · `Cleanup +0xd2` · `Irot*` (`IrotGetObject` · `IrotRegister` · `IrotIsRunning` · `IrotEnumRunning` · `IrotGetTimeOfLastChange`) · `IMgot*` (`IMgotGetObject` · `IMgotRegister` · `IMgotEnumRunning`). 모두 RPCSS 에 말을 걸 때 이 글을 같이 보낸다 `[추측: 호출 인자 자리는 안 봄]`.
- **부르는 쪽이 실패 (음수) 를 받으면 RPC 연결에 안 간다 — 멈춘다.** `operator()` 는 `Return_Hr` 로 기록하고 `EDI` 를 반환하고 (`Connect` 호출 안 함), `GetConnection` 이 그 값을 `Return_Hr (줄 1032)` 후 그대로 반환 (`+0xc5 CMP EAX,0x80070005` 로 `E_ACCESSDENIED` 만 따로 처리 `[공백 — 그 갈래 안 읽음]`), `CoInitializeSecurity +0x486 JS +0x89c` → `+0x8a9 MOV EAX,EDI` 로 **`CoInitializeSecurity` 가 같은 HRESULT 를 돌려준다.** → 요청서의 "`CoInitializeSecurity` 가 `0x8007000e` (63) / `0x8007007a` (5)" 와 줄이 맞는다.
- **반환 0 이고 글이 NULL 인 경우 (핸들 0 + LastError 0, r13 꼴)** 는 위 `JNS +0x100` 으로 계속 가서 NULL 을 `Connect` 에 넘긴다 → r13 의 "이름 안 묻고 바로 `epmapper` 연결" 과 맞는다.
- 한 번 글이 정해지면 (`_pwszWinstaDesktop != NULL`) 이후 `GetConnection` 은 `GetThreadWinstaDesktop` 을 **다시 안 부른다** (`+0x80`). 글이 NULL 로 남으면 `GetConnection` 때마다 다시 부른다.

### 2-5. `SetExplicitWinstaDesktopString` (글을 정해 두는 쪽, 이어져서 읽음)

`ms/disassemble/combase/SetExplicitWinstaDesktopString.asm`. `CoInitializeSecurity +0x8fe` 가 **`dwCapabilities` 의 14번 비트 (`0x4000`) 가 켜졌을 때만** `SetExplicitWinstaDesktopString(L"WinSta0\Default")` 를 부른다 (`+0x9b BT R12D,14` · `+0xa0 JB +0x8fe` · `+0x8fe LEA RCX,[0x1802c24e0]` · `+0x905 CALL`).

- `gGotSecurityData != 0` (이미 보안 자료를 받음) 이면 `RpcETooLate` (`+0x5b`) 반환 (`+0xa CMP [0x180338290],0` · `+0x14 JZ +0x62`).
- 아니면 `_wcsicmp(받은 글, L"WinSta0\Default")` 가 같으면 combase 상수 포인터를 그대로 `g_explicitWinstaDesktopString` 에 저장, 아니면 `IsGetProcessWindowStationPresent()` 가 거짓이면 `0x80004021`, 참이면 `DuplicateStringPrivMem` 으로 복사 (실패 `0x8007000e`) (`+0x6c` `+0x7c` `+0x8f` `+0xaa MOV [0x180338580],RBX`).
- 이 글이 있으면 `GetThreadWinstaDesktop` 의 ① 이 핸들 질문 없이 0 반환 (위 2-1).
- 일반 호출자는 보통 이 비트를 안 켠다 `[추측]` — **이번 요청의 막힘 (핸들로 묻기) 과는 무관**.

---

## 3 — win32k 쪽 (`NtUserGetObjectInformation`)

### 3-1. 사슬 — 어디에 본문이 있나

```
win32u!NtUserGetObjectInformation (syscall 0x1065)
  → win32k.sys  __win32kstub_NtUserGetObjectInformation        [이름만 — win32k.sys 는 안 열어 봄, `llvm-readobj` export 표에서 이름 확인]
  → win32kbase!NtUserGetObjectInformation  (rva 0x1223b0, 135 B)
       +0x26 CALL [0x1402cf0d8]  W32GetWin32kBaseApiSetTable()   ← win32k.sys 가 export, win32kbase 가 가져옴
       +0x32 MOV R10,[RAX+0x30]               (표 안의 작은 표)
       +0x36 MOV RAX,[R10+0x388] · +0x3d TEST · +0x40 JZ +0x4b   ← 칸이 비면 그냥 0 (FALSE) 반환
       +0x42 CALL (CFG) [R10+0x388]  ("문지기": 인자 없음, 0 이상이면 통과)  · +0x49 JNS +0x6a   ← 음수면 0 반환
       +0x7f CALL win32kbase!EditionGetUserObjectInformationEntryPoint (rva 0x12cc64, 중계)  (ret +0x84)
       +0x84 MOV EBX,EAX · +0x5a MOVSXD RAX,EBX · +0x68 RET        ← 반환 = 아래 함수가 준 EAX (BOOL) 를 부호 확장
  → win32kbase!EditionGetUserObjectInformationEntryPoint
       +0x1f CALL W32GetWin32kBaseApiSetTable · +0x2b MOV R10,[RAX+0x30] · +0x2f MOV RAX,[R10+0x390] · +0x39 JZ +0x6b (칸 비면 0)
       +0x3b~+0x50 다섯 인자 그대로 (다섯째는 [RSP+0x60]) CALL (CFG) [R10+0x390]  (ret +0x55)
  → win32kfull!EditionGetUserObjectInformationEntryPoint (rva 0xbfc60, 411 B)    ← 표 칸 +0x390 이 가리키는 곳 (refs 의 ptr 줄)
       +0x108 CALL _GetUserObjectInformation (rva 0xc119c, 981 B)  (ret +0x10d)
```

- **함수 표**: 칸 번호 `+0x388` · `+0x390` 은 win32kfull 의 `ext_ms_win_moderncore_win32k_base_ntuser_l1_table` (rva `0x3b4c70`) 안 `+0x388` (`0x3b4ff8`, 값 `0x1401ed1d0` = `xor eax,eax ; ret` — 이름 641개가 한 주소에 겹친 공용 조각, `IsEditionGetUserObjectInformationEntryPointSupported` 가 그 중 하나) · `+0x390` (`0x3b5000`, 값 `0x1400bfc60`). 표가 win32k.sys 를 거쳐 win32kbase 의 `[RAX+0x30]` 로 건네지는 연결 코드는 안 읽었다 `[추측]` — 칸 번호가 일치하고 이름이 맞는다는 것이 근거.
- 이름 주의: **`EditionGetUserObjectInformationEntryPoint` 가 win32kbase (중계, rva `0x12cc64`) 와 win32kfull (본체, rva `0xbfc60`) 둘 다 있다.** 문서·로그에서 모듈 이름을 같이 쓸 것.
- 실행 때 문지기가 0 미만이거나 칸이 비어 있으면 **FALSE 인데 LastError 도 안 쓴다** (`NtUserGetObjectInformation +0x40 JZ +0x4b` / `+0x49 JNS`) — win32kfull 이 아직 안 올라온 세션이면 이 길 (`[추측]`, 이번 요청의 막힘과는 무관).

### 3-2. `win32kfull!EditionGetUserObjectInformationEntryPoint` — 인자 · 입구 검사

`ms/disassemble/win32kfull/EditionGetUserObjectInformationEntryPoint.asm`. 인자 (`hObj`=RCX→RDI, `nIndex`=EDX→R14D, `pvInfo`=R8→RSI, `nLength`=R9D→EBX, `pnLengthNeeded`=`[RSP+0xb0]`).

| 차례 | 하는 일 | 줄 |
|---|---|---|
| 1 | 사용자 창 락 잡기 `EnterCrit(0,0)`, `AtomicExecutionCheck` 생성자 | `+0x30` (ret `+0x37`) · `+0x41` (ret `+0x46`) |
| 2 | `ProbeForWrite(pvInfo, nLength, 정렬)` — **정렬 = 종류 1 이면 4, 그 밖은 2** (`+0x47 MOV R8D,4` · `+0x4d LEA R15D,[R8-2]` · `+0x51 CMP R14D,1` · `+0x55 CMOVNZ R8D,R15D`) | `+0x5e CALL ProbeForWrite` (ret `+0x65`) |
| 3 | `pnLengthNeeded` 가 NULL 이 아니면 그 DWORD 를 읽고 되쓰기로 쓰기 가능 검사 (`MmUserProbeAddress` 이상이면 그 주소로 눌러서 예외 유도) | `+0x6a~+0x92` |
| 4 | `ObReferenceObjectByHandle(hObj, 0, NULL, UserMode, &obj)` — 형식 안 따짐. **실패 (음수) 면 FALSE · LastError 안 씀 · `*pnLengthNeeded` 도 안 씀** | `+0xbe` (ret `+0xc5`) · `+0xcc JS +0x1a3` · `+0x1a3 XOR EBX,EBX` · `+0x1a5 JMP +0x131` |
| 5 | `SetHandleFlag(hObj, 2, 1)` (핸들 사용 중 표시 `[추측]`). **실패면 FALSE + LastError `8` (`ERROR_NOT_ENOUGH_MEMORY`)** | `+0xde` (ret `+0xe5`) · `+0xec JZ +0x197` · `+0x199 LEA ECX,[RBX+8]` · `+0x19c CALL UserSetLastError` |
| 6 | **`_GetUserObjectInformation(hObj, nIndex, pvInfo, nLength, &로컬 길이)`** — 다섯째 인자는 이 함수의 로컬 `[RSP+0x30]` (먼저 0 으로 비움 `+0x28`) | `+0xf2 LEA RAX,[RSP+0x30]` · `+0xf7 MOV [RSP+0x20],RAX` · `+0x108` (ret `+0x10d`) · `+0x10d MOV EBX,EAX` |
| 7 | `SetHandleFlag(hObj, 2, 0)` 로 풀고, **`pnLengthNeeded` 가 NULL 이 아니면 로컬 길이를 그 포인터에 쓴다 (성공이든 실패든 6 을 지난 모든 경우)** | `+0x118` · `+0x124 MOV RCX,[RSP+0xb0]` · `+0x12c TEST RCX,RCX` · `+0x12f JNZ +0x183` · `+0x183~+0x18f MOV [RCX],EAX` |
| 8 | 객체 `ObfDereferenceObject`, `AtomicExecutionCheck` 해제, 락 풀기 `UserSessionSwitchLeaveCrit`, 반환 = `EBX` (`_GetUserObjectInformation` 의 BOOL) | `+0x13b` · `+0x147~+0x153` · `+0x156` · `+0x162 MOV EAX,EBX` |
| 예외 | 프로브·복사가 예외를 던지면 `SetLastNtError(예외 코드)` 후 처리 (`$filt$0` · `$filt$1`) — 이 길은 이번 질문 밖 | `seh:…$filt$0+0x1b` · `$filt$1+0x1b` |

### 3-3. `win32kfull!_GetUserObjectInformation` — 종류별 갈래

`ms/disassemble/win32kfull/_GetUserObjectInformation.asm`. 인자 (`hObj`=R12, `nIndex`=EBX, `pvInfo`=`[RSP+0xd0]`, `nLength`=R15D, 로컬 길이 포인터 = `[RSP+0xe0]`). 로컬: `[RSP+0x34]` = 결과 BOOL (처음 **1**), `[RSP+0x30]` = 필요 길이 (처음 0).

| 차례 | 하는 일 | 줄 |
|---|---|---|
| 1 | 형식을 모른 채 `ObReferenceObjectByHandle` → `ObGetObjectType` → 곧바로 `ObfDereferenceObject`. 실패 (음수) 면 `SetLastNtError(상태)` · FALSE | `+0x56` · `+0x72` · `+0x84` · `+0x69 JS +0x3bb` · `+0x3bd` |
| 2 | 형식이 **창 스테이션** (`ExWindowStationObjectType`) 이면 `EDX = 2`, **데스크톱** (`ExDesktopObjectType`) 이면 `EDX = 1`. **둘 다 아니면 `UserSetLastError(1)` (`ERROR_INVALID_FUNCTION`) · FALSE** | `+0x90 MOV RCX,[ExWindowStationObjectType]` · `+0x97 CMP R14,[RCX]` · `+0x9a JZ +0x3b1` · `+0xa0~+0xaa` · `+0x3d6 MOV ECX,R13D` · `+0x3d9 CALL UserSetLastError` |
| 3 | 종류 5·6 (힙 크기 · 입출력) 은 **데스크톱만** (창 스테이션이면 LastError 1) | `+0xb3 LEA EAX,[RBX-5]` · `+0xb6 CMP EAX,R13D` · `+0xb9 JBE +0x3c6` |
| 4 | 이번엔 **형식을 지정해** `ObReferenceObjectByHandle(hObj, 0, 그 형식, UserMode, &객체 [RSP+0x40], &핸들정보 [RSP+0x58])`. `[RSP+0x58]` 의 아래 DWORD = 핸들 속성 | `+0xe1` (ret `+0xe8`) · `+0xc4 LEA RAX,[RSP+0x58]` · `+0xf9 JS +0x3bb` |
| 5 | `nIndex` 갈래: 1 → `+0x2ed` · 2 → `+0x111` · 3 → `+0x261` · 4 → `+0x204` · 5 → `+0x2c6` · 6 → `+0x1bd` · 8 → `+0x26a` (DWM 전용) · 그 밖 → `UserSetLastError(0x57)` · FALSE | `+0xff SUB EBX,1 · JZ +0x2ed` · `+0x108~+0x1b7` · `+0x340 MOV ECX,0x57` |
| 끝 | **`*로컬길이포인터 = [RSP+0x30]`** → 객체 `ObfDereferenceObject` → 반환 = `[RSP+0x34]` | `+0x37e MOV [RDX],EAX` · `+0x383` · `+0x38f MOV EAX,[RSP+0x34]` · `+0x3af RET` |

- 반환 BOOL: 기본 1. 0 이 되는 곳 = 필요 길이 > `nLength` (`+0x18a` → `+0x194 MOV [RSP+0x34],EDI(0)`), 형식·핸들 실패 (`+0x3c2 XOR EAX,EAX`), 지원 안 하는 종류.

### 3-4. 표 — 종류 1 · 2 (· 3) 출력 칸

종류 1 (`UOI_FLAGS`, `nIndex = 1`) — **12 바이트**:

| 칸 | 크기 | 쓰는 값 | 줄 |
|---|---|---|---|
| `+0x0` `fInherit` `[추측: 공개 문서 이름]` | DWORD | **핸들 속성 비트 1 (`0x2`, `OBJ_INHERIT`)** 을 0/1 로: `(속성 >> 1) & 1` | `+0x30c MOV RAX,[RSP+0x58]` · `+0x311 SHR EAX,1` · `+0x313 AND EAX,R13D` · `+0x31b MOV [RBX],EAX` |
| `+0x4` `fReserved` | DWORD | **0** (`+0x4 ~ +0xb` 여덟 바이트를 0 으로 쓴 뒤 `+0x8` 만 고침) | `+0x31d MOV qword ptr [RBX+4],RDI` (RDI = 0) |
| `+0x8` `dwFlags` | DWORD | **창 스테이션**: 객체 `+0x40` 의 비트 `0x4` 가 **꺼져 있으면 1** (공개 상수 `WSF_VISIBLE` = 1 과 같은 값), 켜져 있으면 0. **데스크톱**: `CheckHandleFlag(0, 객체 첫 DWORD, hObj, 0)` 가 0 이 아니면 비트 0 (`1`) — 이름 `[공백]` (공개 문서의 `DF_ALLOWOTHERACCOUNTHOOK` = 1 과 같은 값 `[추측]`) | 창 스테이션 `+0x321 MOV RAX,[ExDesktopObjectType]` · `+0x328 CMP R14,[RAX]` · `+0x32b JZ +0x34a` (아니면) `+0x32d MOV RAX,[RSP+0x40]` · `+0x332 MOV ECX,[RAX+0x40]` · `+0x335 TEST CL,4` · `+0x338 JNZ +0x366` · `+0x33a MOV [RBX+8],R13D` ; 데스크톱 `+0x34a~+0x362` |
| 길이 | — | 필요 길이 **`0xc`** 를 **먼저** `[RSP+0x30]` 에 둔다 (`+0x2ed`) → `nLength < 0xc` 면 FALSE + LastError `0x7a` + `*길이 = 0xc` | `+0x2ed MOV [RSP+0x30],0xc` · `+0x2f5 CMP R15D,0xc` · `+0x2f9 JC +0x18a` |

- **대화형 창 스테이션 (`WinSta0`) 의 값**: 객체 `+0x40` 의 `0x4` 비트가 이 세션의 `WinSta0` 에서 꺼져 있는지는 정적으로 못 정한다 (런타임 값). 비트 이름도 구조체 타입이 없어 `[공백]`. **`[추측]`**: `0x4` 는 "입출력 없음 (non-interactive)" 비트라 대화형 `WinSta0` 에서는 꺼져 있고 `dwFlags = 1` (VM `GetUserObjectInformationW(hWinsta, 1, …)` 한 번으로 확정).

종류 2 (`UOI_NAME`) — **이름 글**:

| 칸 | 크기 | 쓰는 값 | 줄 |
|---|---|---|---|
| 글 | `Length` 바이트 | 객체 관리자 머리 (`ObQueryNameInfo(객체)` 결과 `+0x8` = `Name` UNICODE_STRING) 의 `Buffer` 를 `memmove` — **이름 끝 조각만** (`WinSta0`, `Default` — 디렉터리 `\Windows\WindowStations` 는 안 붙음) | `+0x111~+0x138 ADD RAX,0x8` · `+0x13c` · `+0x146 MOVZX R8D,word [RAX]` · `+0x168 CALL memmove` (ret `+0x16d`) |
| 널 | 2 바이트 | **글 뒤에 `0x0000` 을 쓴다** (`[버퍼 + Length] = 0`) | `+0x16d MOVZX ECX,word [RBX]` · `+0x170 MOV RAX,[RSP+0xd0]` · `+0x178 MOV word [RAX+RCX],DI` |
| 필요 길이 | DWORD | **`Length + 2` (바이트, 널 포함)** → `*lpnLengthNeeded` | `+0x14a LEA EAX,[R8+2]` · `+0x14e MOV [RSP+0x30],EAX` · `+0x37e MOV [RDX],EAX` |
| 모자랄 때 | — | `Length + 2 > nLength` (부호 없는 비교) → **FALSE · LastError `0x7a` · `*길이 = Length + 2` · 버퍼 안 씀** | `+0x152 CMP EAX,R15D` · `+0x155 JA +0x18a` · `+0x18a MOV ECX,0x7a` · `+0x18f CALL UserSetLastError` · `+0x194 MOV [RSP+0x34],EDI` |
| 이름 없음 | — | `ObQueryNameInfo` 가 NULL 이면 **TRUE · `*길이 = 0` · 버퍼 안 씀** (이름 없는 객체) | `+0x120 TEST RAX,RAX` · `+0x123 JZ +0x1fc` · `+0x144 JZ +0x181` · `+0x181 MOV [RSP+0x30],EDI` |

- 값 예 (정적으로 나오는 크기): `WinSta0` = 7 글자 = `Length 14` → 필요 **16 바이트**. `Default` = 7 글자 → **16 바이트**. combase 의 첫 호출 (`nLength = 0x40`) 안에 들어가므로 **재호출 (`0x7a` 길) 은 안 탄다.**
- 이름 글자의 대소문자: 객체 이름은 **그 객체를 만들 때 준 글자 그대로** (객체 관리자 이름) 라 win32k 코드가 정하지 않는다. 그래도 커널 쪽 상수는 전부 대문자 S: win32kfull `.rdata` 의 `WinSta0` (rva `0x36fee8`, `xxxResolveDesktop +0x3f4` · `xxxResolveDesktopForWOW +0x16b` 가 읽음) · `\Windows\WindowStations\WinSta0` (`0x36f1c0`) · `WinSta0_RIT` · `WinSta0_DesktopSwitch`. combase 의 기본 글도 `WinSta0\Default` (대문자 S, 2-3). → `[정적 근거로는 WinSta0]`, 실제 객체 이름은 `[공백 — VM 한 줄로 확정]`. run44 `[관찰]` 의 `Winsta0\Default` (소문자 s) 는 **프로세스 매개변수 글** (프로세스를 만들 때 준 값) 이라 별개이고, combase 의 비교 (`RtlEqualUnicodeString(…, TRUE)`) 도 대소문자를 안 따진다.

종류 3 (`UOI_TYPE`, 덧붙임 — combase 는 안 부름): `LEA RAX,[R14+0x10]` (`+0x261`) = **객체 형식 (`OBJECT_TYPE`) 의 `Name`** (`+0x10`) 을 종류 2 와 같은 코드로 복사 (널 포함, 필요 길이 = `Length + 2`). 창 스테이션 = `WindowStation` (공개 형식 이름 `[추측]` → 26 + 2 = 28 바이트), 데스크톱 = `Desktop` (14 + 2 = 16 바이트).

---

## 지금 우리 (PE_emulator) 맞나·틀리나

쓰는 곳: `ext/syscall` 갈래 gui 의 T1 — `src/gui/nt_user_get_object_information.cpp` (새 몸) · `nt_user_get_process_window_station.cpp` · `nt_user_get_thread_desktop.cpp` 의 `TODO(VM)` 줄. (이 레포에서 그 파일은 안 봤다 — 요청서가 준 설명만 대조.)

| 줄 | 지금 우리 | 판정 | 근거 |
|---|---|---|---|
| `NtUserGetProcessWindowStation` → `0x20000` | 0 아닌 핸들 | **맞다** | combase 는 0 인지만 본다 (`GetThreadWinstaDesktop +0x73 TEST RAX,RAX`), 값은 그대로 `GetUserObjectInformationW` 에 되넘길 뿐. 진짜 핸들 값은 VM `[공백]` (`TODO(VM)` 유지). |
| `NtUserGetThreadDesktop` → `0x20400` | 0 아닌 핸들 | **맞다** | `+0x99 TEST RAX,RAX`. 같은 이유. |
| `NtUserGetObjectInformation` 기본 대답 = **반환 0 · 버퍼 안 씀** | | **틀림 — 이게 막힘의 원인** | 반환 0 이면 user32 가 그대로 FALSE 를 주고 (`W` 는 썽크), combase 가 `GetLastError()` 를 보는데 stub 이 `TEB+0x68` 을 안 건드려 0 이다 → `+0x1f2 JZ` → `+0x238 MOV EBX,0xe` → **`0x8007000e`** → `operator() +0x99~` 실패 → `GetConnection` → `CoInitializeSecurity` 가 그 값으로 실패 (2-4). 묵은 LastError 가 `0x7a` 로 남아 있던 샘플은 같은 코드의 `0x8007007a` 갈래 (2-2 d·f). **"묵은 `GetLastError` 를 HRESULT 로 바꿈" `[추측]` 은 맞다 — 코드가 정확히 그렇게 쓰여 있다 (`+0x1f4~+0x1f9` · `+0x364 MOV EBX,EAX`).** |
| 종류 **2** (이름), 핸들 = 창 스테이션 | | **stub 이 돌려줄 값** | **반환 1 (TRUE)** · 버퍼에 **UTF-16 `WinSta0` + `0x0000` (16 바이트)** · `*lpnLengthNeeded = 16` · `nLength < 16` 이면 반환 0 + `TEB+0x68 = 0x7a` + `*lpnLengthNeeded = 16` (버퍼 안 씀). 근거 3-4. 대소문자는 `WinSta0` (정적 근거 쪽), 확정은 VM. |
| 종류 **2**, 핸들 = 데스크톱 | | **stub 이 돌려줄 값** | 같은 꼴, 글은 `Default` (16 바이트). `[공백: 데스크톱 객체 이름의 정확한 글자 — 객체 만든 쪽이 정함, VM 확인]` |
| 종류 **1** (`(0x20000, 1, buf, 0xc)` 353 샘플) | | **stub 이 돌려줄 값** | 반환 1 · 12 바이트 `{fInherit = 핸들 속성 (`[공백]`, 프로세스 창 스테이션 핸들이 상속 가능이냐 — VM), fReserved = 0, dwFlags = 1}` · `*lpnLengthNeeded = 0xc` · `nLength < 0xc` 면 반환 0 + LastError `0x7a` + `*길이 = 0xc`. `dwFlags = 1` 은 `[추측]` (3-4, 대화형 `WinSta0`). 이 호출은 combase 가 아니다 (2-2). |
| `(0x20400, 2, buf, 0x208)` · `(0x20000, 2, buf, 0)` | | 위 규칙으로 다 된다 | 0x208 바이트 버퍼 → 16 바이트 쓰고 TRUE. 길이 0 이면 FALSE + `0x7a` + `*길이 = 16` (combase 의 W 직접 길이면 이 값으로 `HeapAlloc(16)` 후 재호출 — 2-2 d). |
| 다섯째 인자 (`lpnLengthNeeded`) | 로그에 안 보임 | **써야 한다** | 커널은 **성공·`0x7a` 실패 모두** 이 포인터에 필요 길이를 쓴다 (`Edition… +0x12f JNZ +0x183 → MOV [RCX],EAX`). NULL 이면 안 씀. user32 `A` 는 성공 때 이 값을 반으로 줄이지만 W 직접 호출은 그대로. |
| 실패를 흉내 낼 때 LastError | | **TEB 칸에 직접 써야 한다** | 커널 코드가 `UserSetLastError` 로 `TEB+0x68` (`LastErrorValue`) 에 쓴다 (`UserSetLastError +0x18 MOV RAX,GS:[0x30]` · `+0x21 MOV [RAX+0x68],EBX`). 32비트 프로세스면 `+0x2034` 도 (`+0x3e`). user32 는 안 쓴다 (1-3). |
| 핸들을 0 으로 돌릴 때 (r13) | | 길 맞음 | 핸들 0 이고 LastError 0 → combase 가 **0 반환 + 글 NULL** (`+0x23f~+0x24f`, `+0x2ce`) → RPC 연결로 감 — r13 로그와 맞는다. 핸들 0 이고 LastError > 0 이면 `0x8007xxxx` 로 멈춘다. |
| PEB 칸 | | **영향 있음** | 에뮬레이터 PEB `+0x58` (`KernelCallbackTable`) 이 0 이면 combase 가 핸들 질문을 건너뛰고 `DesktopInfo` (`Winsta0\Default`) 로 `WinSta0\Default` 상수를 쓴다 (2-3). 로그가 `NtUserGetProcessWindowStation` 에 닿았으니 지금은 0 이 아니라고 읽힌다 `[추측]`. |
| 이름 글 대소문자 | `WinSta0` / `Winsta0` 미정 | **정적으로는 `WinSta0` 쪽** | 위 3-4. run44 값은 프로세스 매개변수 글 (소문자 s) 이라 `NtUserGetObjectInformation` 출력과 별개. 확정 `[공백]` (VM). |

**stub 한 줄 요약 (T1 새 몸):** `h ∈ {창 스테이션, 데스크톱}` 이고 종류 ∈ {1, 2, 3} 이면 위 값을 쓰고 1 을 돌려준다 · 길이가 모자라면 `*lpnLengthNeeded` 만 쓰고 0 + `TEB+0x68 = 0x7a` · 그 밖 (모르는 핸들) 은 0 + `TEB+0x68 = 1` (`ERROR_INVALID_FUNCTION`, 형식이 창 스테이션도 데스크톱도 아닐 때) — 이 마지막은 우리가 만든 가짜 핸들만 쓰는 한 안 탄다.

---

## 동적으로 확인할 것 (dast 요청 필요 — VM 한 번)

1. **진짜 이름 글·핸들 값 (VM 한 줄):** GUI 프로세스 (test.exe 처럼 user32 가 올라온 것) 에서 `CoInitializeSecurity` 를 한 번 타게 하거나 `GetUserObjectInformationW` 를 창 스테이션 · 데스크톱에 종류 2 로 한 번씩. 멈춤점은 combase `CRpcResolver::GetThreadWinstaDesktop` (시작 rva `0x82330`). 조각 안 주소는 WinDbg 가 함수 이름으로 안 보여 줄 수 있으니 **rva 로 건다** (`bp combase+0x…`):
   - `+0x73` (= `combase+0x823a3`) `r rdi` = **창 스테이션 핸들** · `+0x99` (= `combase+0x823c9`) `r r14` = **데스크톱 핸들** (`0x20000` · `0x20400` 인지).
   - `+0xcf` (= `combase+0x823ff`, 첫 `GetUserObjectInformationW` 반환 직후): `r rax` (1 이어야 함) · `du @rbp-0x49` (창 스테이션 이름 — **대소문자 확인**) · `dd @rbp-0x59 L1` (`lpnLengthNeeded`, 16 이어야 함).
   - `+0xfe` (= `combase+0x8242e`): `du @rbp-0x9` (데스크톱 이름) · `dd @rbp-0x59 L1`.
   - `+0x1ff` (= `combase+0x8252f`, `MOV EAX,EBX` — 조각 안): `du poi(combase+0x338118)` = 만들어진 글 (`WinSta0\Default` 로 보여야 함) · 이어서 `+0x22d RET` 직전 (= `combase+0x8255d`) `r eax` = 0.
2. **종류 1 의 진짜 값:** 커널 `win32kfull!_GetUserObjectInformation +0x31b` (`MOV [RBX],EAX` — `fInherit` 를 쓰는 줄, 조건 `nIndex == 1`) 와 `+0x33a` (`MOV [RBX+8],R13D` — `dwFlags` = 1 을 쓰는 줄; 이 줄에 **닿는지** 자체가 "객체 `+0x40` 의 `0x4` 비트가 꺼져 있다" 는 뜻). 닿았을 때 `+0x335 TEST CL,4` 의 `cl` 도 본다. 세션 공간 드라이버라 GUI 프로세스 문맥에서 (`.process /i`). 사용자 쪽에서는 `user32!GetUserObjectInformationW` 입구 (`rcx` = 핸들, `edx` = 1, `r9d` = 0xc) 와 반환 뒤 12 바이트 (`dd poi(@r8) L3` 를 반환 직후 부르는 쪽에서) 를 봐도 된다.
3. **로그의 `(…, 1, buf, 0xc)` · `0x208` 호출자:** 에뮬레이터 샘플에서 호출 스택 (`caller+0x…`) 을 남겨 어느 모듈 (`clr` 등) 인지 — 정적 자료에는 없다.
4. **PEB `+0x58` (`KernelCallbackTable`) 값:** 에뮬레이터 PEB 에서 0 이 아님을 한 번 확인 (2-3).

## 다음 요청 후보

1. **다음 벽 = `Connect` (rva `0x12f318`)** — `operator() (0x18b9c0) +0x82e` 가 부르는 RPCSS 쪽 RPC 클라이언트 스텁. 첫 인자 바인딩 핸들 · 셋째 인자 (exe 경로) · `_pwszWinstaDesktop` 이 어떤 모양으로 보내지는지, 그리고 그 앞 `CreateFromBindingString`·`CreateFromBindingHandle` (같은 람다 `+0x31c` 근처) 가 `\RPC Control\epmapper` 로 가는 길 (run129·run133) 과 어떻게 이어지는지. 이 요청의 2 절 답이 정확히 이 길로 이어진다.
2. `NtUserGetProcessWindowStation` · `NtUserGetThreadDesktop` 의 커널 본문 (win32kbase 껍데기 → win32kfull `EditionGetProcessWindowStationEntryPoint` · `EditionGetThreadDesktopEntryPoint` · `_GetProcessWindowStation`) — `TODO(VM)` 줄의 핸들 값 · 핸들 속성 (`fInherit`) 이 어디서 정해지나. 이번에 win32kbase·win32kfull 을 이미 analyze 해 둬서 export 만 하면 된다.
3. `GetConnection` (`0xa14d0`) 의 `E_ACCESSDENIED (0x80070005)` 갈래 (`+0xc5`) 와 `CoInitializeSecurity` 앞부분 (`+0x7a` `Query` · `+0x87` `IsApartmentInitialized`) — 이번엔 안 읽음.

## 스크립트 제안

- **제안 (고치지 않음):** `ExportFunctions.java` 는 이름 정규식만 받아서, 같은 이름이 여럿인 함수 (combase `operator()` 70개, 이번 `GetThreadWinstaDesktop` 의 부르는 쪽) 를 하나만 못 뽑는다. 이번엔 `llvm-objdump-14` 로 일회용 읽기 (`_oneoff/`) 를 했다. **인자로 `rva:0x18b9c0` 꼴을 받아 그 주소의 함수 하나만 뽑는 선택자** (또는 `ms/decomp/<모듈>/_index.tsv` 의 `rva` 열로 고르는 두 번째 인자) 가 있으면 `.asm` 의 `_calls.tsv`·조각·전역 주석을 그대로 얻을 수 있다.
- 로그 한 줄 (스크립트 오류 아님): win32kbase analyze 로그에 `WARN (java file: WindowsResourceReference) … GhidraScriptLoadException: The class could not be found` — Ghidra 기본 스크립트 하나가 안 불러진다는 경고이고 `SetPdb`·`ExportIndex` 는 정상 (`SCRIPT ERROR` 없음).

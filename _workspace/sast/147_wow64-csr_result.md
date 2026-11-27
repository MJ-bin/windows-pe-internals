# 147 wow64-csr — 정적 분석 결과

- 요청서: `_workspace/sast/147_wow64-csr.md` (대기열 Q287). PE_emulator 원문 `../PE_emulator/_workspace/05_tuning_planner/374_internals-wow64-csr.md` 는 읽기만 했다.
- 날짜: 2026-11-27. 정적만.
- **이 요청이 연 모듈 (범위 지킴)**: `ntdll32` · `kernelbase32` (처음 analyze) · `wow64` · `csrsrv` 넷뿐. 64 비트 `ntdll`·`kernelbase`·`basesrv` 는 **이미 있던 `.asm` 을 읽기만** 했다 (다시 뽑지 않음). 다른 decompiler 몫 (rpcss · combase · rpcrt4 · appinfo 등) 은 안 열었다.
- `free -g`: 처음 analyze 전 available 28 G, 이후 export 때마다 22 ~ 27 G (10 G 아래로 안 내려감).
- 새로 한 것 (이 요청 몫만):
  - `kernelbase32` PDB 받기 + 별칭 표 + analyze (약 1분 40초: 14:15:55 ~ 14:17:33, `SetPdb` 줄 `ms/ghidra/kernelbase32.analyze.log:347~348`, `_index.tsv` 약 7600 함수). 프로젝트 이름은 `kernelbase32`, PDB 는 `ms/sym/kernelbase32.dll.pdb`.
  - 뽑은 함수 (모두 `.asm` · `.c` 둘):
    - `kernelbase32` 41개 (폴더 새로 생김): `rva:0x15afb0,0x15b0d4,0x15b185,0x15b127` depth 1 + `rva:0x15babc` (`_NlsProcessInitialize@0`) depth 1.
    - `ntdll32` 15개 더: `CsrClientConnectToServer` · `NtWow64CsrClientConnectToServer` · `NtOpenSection` · `NtCreateSection` · `NtConnectPort` · `NtSecureConnectPort` · `NtMapViewOfSection` (+`Ex` 둘) · `RtlRegisterThreadWithCsrss` · `NtWow64CsrClientCallServer` · `CsrClientCallServer` (depth 0) + 로더 셋 (`_LdrpInitializeNode@4` · `_LdrpInitializationFailure@4` · `_LdrpCallInitRoutine@16`, depth 0).
    - `wow64`: 요청한 썽크 여섯 (`whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort` · `whNtSecureConnectPort` · `whNtMapViewOfSection` · `whNtWow64CsrClientConnectToServer`) depth 1 (도우미 포함 19개) + `whNtQuerySystemInformation` · `…_SystemBasicInformation` · `…_SystemEmulationBasicInformation` · `Wow64InitializeEmulatedSystemInformation` · `RedirectPath` (depth 0).
    - `csrsrv`: `CsrProcessLazyRegister` 하나 더 (depth 0).
  - `refs`: ntdll32 `_CsrClientProcess` · `_CsrInitOnceDone` · `_CsrServerApiRoutine`.
- 출력 위치: `ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/` (`ntdll32`, `kernelbase32`, `wow64`, `csrsrv`), `_refs/` (ntdll32).

## 스크립트 문제

없음. 하나만 적어 둔다 (요청서 표기).

- 요청서의 `kernelbase32=SysWOW64/KernelBase.dll` 은 `kernelbase32.dll=SysWOW64/KernelBase.dll` 로 써야 PDB 이름이 `ms/sym/kernelbase32.dll.pdb` (`ntdll32.dll` 과 같은 꼴) 가 된다. `.dll` 없이 부르면 `ms/sym/kernelbase32.pdb` 로 저장된다 (처음에 그렇게 받았다가 내가 지우고 다시 받음). 모듈 이름 (`ms/ghidra/kernelbase32`) 은 둘 다 `kernelbase32`.

## 먼저 읽는 한 줄 답

1. **32 비트에는 `CsrpConnectToServer` 가 없다.** 32 비트 `CsrClientConnectToServer` (62 B) 는 syscall 하나 `NtWow64CsrClientConnectToServer` (서비스 번호 `0x1e9`, 인자 5개) 만 부른다. 그 본문 wow64 `whNtWow64CsrClientConnectToServer +0x1c` 가 **64 비트 ntdll 의 `CsrClientConnectToServer` 를 import 로 직접 불러서** run68 의 `CsrpConnectToServer` syscall 여섯 (`NtOpenSection` · `NtCreateSection` · `NtConnectPort` · `NtClose` · `NtMapViewOfSection` · `NtClose`) 이 **같은 순서·같은 인자로** 64 비트 syscall 로 나온다. 연결 정보 블록은 64 비트 꼴 `0x30` B (포인터 칸 8 B); 32 비트 코드가 건네는 것은 `{CtrlRoutine32, 0}` 8 B 하나뿐. PEB32 는 wow64 가 연결 뒤 `+0x4c` (4 B) · `+0x54` (4 B) · `+0x248` (8 B) 에 PEB64 값을 옮겨 쓴다. 32 비트 ntdll `Csr*` 전역은 `_CsrClientProcess` (rva `0x13911c`) 하나만 쓴다.
2. **wow64 썽크 넷 (`whNtOpenSection` · `whNtCreateSection` · `whNtConnectPort`/`whNtSecureConnectPort` · `whNtMapViewOfSection`) 은 이 CSR 연결 길에 안 낀다** (여섯 syscall 은 wow64 를 안 거치고 64 비트 ntdll 에서 바로 나감). 본문은 읽어서 표로 남김: 포트 뷰 `0x18 ↔ 0x30` · 원격 뷰 `0xc ↔ 0x18` · 연결 정보·길이 포인터는 **그대로** 넘김 (크기 안 바꿈) · 4 GB 아래 요구는 `MapViewOfSection` 썽크만 (`ZeroBits` 0 이고 주소 `*Base` 0 이면 `Wow64pMaxAddress` 로 바꿈) · status 는 그대로, 바꾸는 곳은 아래 표 (3-4).
3. **32 비트 `__KernelBaseBaseDllInitialize@12 +0x22d ~ +0x346`**: `BaseStaticServerData` = `[[PEB32+0x54] + 8]` (표 인덱스 **1**, 칸 8 B 의 **하위 4 B**) − `[PEB32+0x248]` (하위 4 B) + `[PEB32+0x4c]` — 32 비트 셈. 읽는 BSSD 칸은 64 비트와 **같은 오프셋** (`+0x0` · `+0x2` · `+0x8/+0xc` · `+0x10` · `+0x12` · `+0x18/+0x1c` · `+0x38` · `+0x9e8`, `+0x178` 은 주소만) — 새 칸 없음, 폭만 다름 (8 B 칸을 하위 4 B 로 읽고 16 B `UNICODE_STRING` 을 8 B 로 줄여 `KernelBaseGlobalData` 에 넣음). **FALSE 로 나가는 가지 = 다섯** (`+0x1bc` · `+0x20e` · `+0x3bc` · `+0x3da` · `+0x3f4`, 모두 `+0x4d1` 로) + `entry` 둘 (`+0x5a` · `+0x8b`) + `PcwDllMain` 하나. 원문의 `NtOpenSection(SharedSection) = 0xc000003a` 는 **`+0x20e` (두 번째) 로 가서 FALSE** → `entry +0x113` 이 0 반환 → ntdll32 `_LdrpInitializeNode@4 +0x247` 이 `0xc0000142` 를 씀 → `_LdrpInitializationFailure@4 +0x5f` `NtRaiseHardError(0xc0000145)`. csrsrv 는 WoW64 클라이언트를 따로 다루는 줄이 **없다** (읽은 세 함수 안에서).

(3 이 에뮬레이터를 막는 줄이라 아래 본문도 **3 → 1 → 2** 순서로 썼다.)

## 버전·PDB 확인

| 항목 | 값 | 근거 |
|---|---|---|
| 32 비트 `ntdll.dll` PDB | `wntdll.pdb` GUID+Age `7C1F7D48576AAE47EB48E43D5FB752AD1` | `ms/sym/ntdll32.dll.pdb.info.txt`, `fetch_pdb.py --info ntdll32.dll=SysWOW64/ntdll.dll` 오늘 다시 같은 값, `ms/ghidra/ntdll32.analyze.log:54` `SetPdb` |
| 32 비트 `KernelBase.dll` PDB | `wkernelbase.pdb` GUID+Age `D14348CD34CC97EAEC7321B3CBCB001D1` | `ms/sym/kernelbase32.dll.pdb.info.txt`, `ms/ghidra/kernelbase32.analyze.log:347~353` (`SetPdb` → `PDB Types and Main Symbols Processing Terminated Normally`) |
| (참고) 이 요청이 같이 본 것 | `wow64.dll` `AED569E2D483C3CDC0C97DEC3691CD551` · `csrsrv.dll` `32C5AA2B9D0AF671C35069A02C5A0C041` · 64 비트 `ntdll.dll` `C3093720177DA6851519DCFFE8DF53FE1` · 64 비트 `KernelBase.dll` `72CCE506944DE0267C172AD64287F9BD1` | `ms/sym/*.pdb.info.txt` |

버전 대조 (모두 **일치**):

- 64 비트 `CsrpConnectToServer +0x1b6/+0x1bb → NtOpenSection` (run68 표) = `.asm` 같음. 같은 `NtOpenSection` 스텁은 rva `0x161490`, `SYSCALL` 이 `+0x12` → rva `0x1614a2`. PE_emulator 원문 로그 rip `0x7ffafb7c14a2` − `0x1614a2` = `0x7ffafb660000` (64 KB 정렬, ntdll 베이스로 맞음) `[관찰 ↔ 정적]`. → **로그의 `NtOpenSection` 은 64 비트 ntdll 의 `CsrpConnectToServer` 에서 나온 것**이다.
- 번호 맞물림 (서로 다른 두 바이너리): ntdll32 `NtOpenSection +0x0 MOV EAX,0x37` ↔ wow64 nt 표 칸 `0x37` = `whNtOpenSection`; `NtConnectPort` `0xa4` ↔ `whNtConnectPort`; `NtWow64CsrClientConnectToServer` `0x1e9` ↔ `whNtWow64CsrClientConnectToServer` (표 `sdwhnt32` 함수 배열 `0x3a550`, 인자 바이트 `0x14` = 5개). 표 읽기는 `ms/bin` 의 wow64.dll PE 바이트를 파이썬으로 읽음 (임시, scratchpad).
- PEB32 `+0x1d4 SessionId`: 커널이 씀 (run139 `MiInitializeWowPeb +0x141 ~ +0x144`) ↔ kernelbase32 이 읽음 (`__KernelBaseBaseDllInitialize@12 +0x13c MOV ESI,[ESI+0x1d4]`) — 칸 이름·자리 맞음.
- kernelbase32 의 3층 (`entry` → `_KernelBaseBaseDllInitialize@12` rva `0x15b0d4` → `__KernelBaseBaseDllInitialize@12` rva `0x15b185`) 이 64 비트 3층 (`KernelBaseBaseDllInitialize` → `_KernelBaseBaseDllInitialize`) 과 닮음. 32 비트 `+0x206/+0x20c` `CsrClientConnectToServer` 가 64 비트 `+0x2d2/+0x2d9` 자리.

## 3. 32 비트 `_KernelBaseBaseDllInitialize` 가 연결 뒤 읽는 것 (먼저)

### 3-0. 이름 짝

| 64 비트 (run68) | 32 비트 (이 요청) | rva |
|---|---|---|
| `KernelBaseBaseDllInitialize` | `_KernelBaseBaseDllInitialize@12` (77 B, 바깥) | `0x15b0d4` |
| `_KernelBaseBaseDllInitialize` | `__KernelBaseBaseDllInitialize@12` (1251 B, 안쪽 — 일은 여기) | `0x15b185` |
| `entry` (DllMain) | `entry` (286 B) | `0x15afb0` |

- `entry +0x16/+0x1b` → 바깥, 바깥 `+0x25/+0x2a` → 안쪽. 64 비트는 꼬리 `JMP`, 32 비트는 `CALL` (돌아옴). `[정적] ms/disassemble/kernelbase32/entry.asm` · `_KernelBaseBaseDllInitialize@12.asm`.
- 이 아래 `+` 오프셋은 따로 말 없으면 **안쪽 `__KernelBaseBaseDllInitialize@12`** 기준 (`.asm` 줄 = `kernelbase32/__KernelBaseBaseDllInitialize@12.asm`).

### 3-1. 안쪽 함수의 syscall · 호출 차례 (이유 1 = 붙음, `+0x103` 부터)

| # | 자리 (call / ret) | 무엇 | 인자 · 비고 |
|---|---|---|---|
| a | `+0x118 / +0x11d` | `_KernelBaseDllInitializeMemoryManager@0` | 안에서 `_SystemRangeStart` (`0x102857a4`) 가 0 이면 **`NtQuerySystemInformation(0x32, &x, 4, 0)`** (`.asm` `+0x27/+0x2d`) 하나, 이어 `RtlInitializeHandleTable(0xffff, 8, &…)` (`+0x43/+0x49`). 결과 상태는 안 봄 |
| b | `+0x131 / +0x137` | `RtlCreateTagHeap(PEB32.ProcessHeap, 0, "BASEDLL!", "TMP")` | 결과 → `_KernelBaseGlobalData+0x0` (rva `0x2848e0`) |
| c | `+0x142 / +0x148` | `RtlGetCurrentServiceSessionId` | PEB32 `+0x1d4` 와 같으면 `\Windows`, 다르면 `StringCbPrintfW` (`+0x1a3/+0x1a8`) 로 `%ws\%ld%ws` = `\Sessions\<번호>\Windows` (문자열은 `.rdata` 로 확인) |
| d | `+0x1b4 / +0x1ba` | **`NtQuerySystemInformation(0, &SysInfo, 0x2c, NULL)`** (`SysInfo` rva `0x284840`, 0x2c B) | **FALSE 가지 F1** (`+0x1bc JS +0x4d1`). wow64 가 클래스 0 을 **`0x3e` 로 바꿔** 부름 (`whNtQuerySystemInformation +0x62 TEST R10D` · `+0x65 JZ +0x79` · `+0x7c/+0x81` → `…_SystemBasicInformation +0x19b MOV ECX,0x3e` · `+0x1a0 JMP +0x64` · `+0x6f/+0x76` 네이티브 `NtQuerySystemInformation`) — 원문 로그 seq 647 `NtQuerySystemInformation 0x3e` 가 이 호출로 보인다 (바로 뒤가 연결 `NtOpenSection` 648 이라 순서가 맞음 `[정적 순서 ↔ 관찰]`) |
| e | `+0x1c9 / +0x1cf` | `RtlRandomEx` → `_BasepFiberCookie` | |
| f | **`+0x206 / +0x20c`** | **`CsrClientConnectToServer(dir, 1, &{CtrlRoutine32, 0}, 8, &byte)`** | 5 인자: 디렉터리 이름 (지역 버퍼 `[EBP-0x204]`), 서버 번호 1, 블록 (`[EBP-0x21c]` = `0x102408b0` = `CtrlRoutine`, `[EBP-0x218]` = 0), **크기 8 (값)**, 출력 바이트 (`[EBP-0x205]`). **FALSE 가지 F2** (`+0x20e JS +0x4d1`) |
| g | `+0x372 / +0x378` | `RtlUnicodeStringToAnsiString` | 실패해도 FALSE 아님 (`_BaseAnsiCommandLine` 0 으로) |
| h | `+0x38f / +0x395` · `+0x396 / +0x39c` | `RtlSetUnhandledExceptionFilter` · `RtlEncodePointer(0)` | |
| i | `+0x3b5 / +0x3ba` | `_BaseNlsDllInitialize@12(…, 1, BSSD)` | **F3** (`+0x3bc JZ`) |
| j | `+0x3d2 / +0x3d8` | `RtlInitializeCriticalSection(0x1028493c)` (= `KernelBaseGlobalData+0x5c`) | **F4** (`+0x3da JS`) |
| k | `+0x3eb / +0x3f0` | `_ConsoleInitialize@0` | **F5** (`+0x3f2 MOV BL,AL` → `+0x3f4 JZ`) |
| l | `+0x3fb / +0x400` | ETW 등록 (`_TraceLoggingRegisterEx_…@12`) | |
| m | `+0x4ca / +0x4cf` | `_RegKrnInitialize@12` | 이유 1 은 늘 TRUE (`_RegKrnInitialize@12 +0x61 ~ +0x83`, `+0x27 MOV AL,1`) |

`[정적]` 위 줄은 모두 `ms/disassemble/kernelbase32/__KernelBaseBaseDllInitialize@12.asm` 과 `_calls.tsv`. `+0x` 값은 `.asm` 줄에서 다시 확인했다.

### 3-2. 표 (4-a) 연결 뒤 읽는 칸 (32 비트 줄 ↔ 64 비트)

| 32 비트 줄 | 읽거나 쓰는 것 | 폭 | 64 비트 (run68 · run71) | 같음 / 다름 |
|---|---|---|---|---|
| `+0x22d MOV EAX,[EAX+0x54]` | PEB32 `+0x54 ReadOnlyStaticServerData` (표 시작 T) | 4 B | PEB64 `+0x98` (8 B) | 칸·폭 다름 |
| `+0x230 MOV EDI,[EAX+8]` | **표 인덱스 1** (= BASESRV 번호 1) 의 **하위 4 B** (표 칸은 8 B 단위) | 4 B | `[T+8]` 8 B | 인덱스 같음 (`+8` = 칸 1), 폭만 반쪽 |
| `+0x239 SUB EDI,[ECX+0x248]` | PEB32 `+0x248 CsrServerReadOnlySharedMemoryBase` 하위 4 B (칸은 8 B) | 4 B | PEB64 `+0x380` | 다름 |
| `+0x23f ADD EDI,[EAX+0x4c]` | PEB32 `+0x4c ReadOnlySharedMemoryBase` (R, 내 뷰) | 4 B | PEB64 `+0x88` | 다름 |
| `+0x248 MOV [0x102848cc],EDI` | `_BaseStaticServerData` ← 블록 지역 주소 (rva `0x2848cc`) | 4 B | rva `0x3aedb8` | 같은 역할 |
| `+0x242 CMP [EBP-0x205],BL` · `+0x252 SETNZ [0x102848e4]` | 연결 호출이 돌려준 `CalledFromServer` ≠ 0 → `KernelBaseGlobalData+0x4` | 1 B | `+0x326` · `+0x337` | 같음 |
| `+0x24e` · `+0x259` | BSSD `+0x38` (word) → `_BaseRCNumber` (rva `0x2848d0`) | 2 B | `+0x332` · `+0x342` | 같음 |
| `+0x25f` · `+0x268` | BSSD `+0x0` · `+0x2` (word 둘 = `Length`·`MaximumLength`) → `KernelBaseGlobalData+0x8` · `+0xa` | 2+2 B | `MOVUPS [R10]` 16 B → `+0x8` | 칸은 같고 **16 B → 8 B 로 줄임** |
| `+0x272 MOV EAX,[EDI+8]` · `+0x275 OR EAX,[EDI+0xc]` | BSSD `+0x8` `Buffer` 8 B 가 0 인지 (하위·상위 둘 다 읽음) | 4+4 B | `MOVQ` 8 B `TEST` | 같음 (0 이면 `+0xc` ← 0, 옮김 건너뜀) |
| `+0x282 ~ +0x2cf` | `KernelBaseGlobalData+0xc` ← `BSSD지역 + [EDI+8] − [BSSD지역+0x9e8]` (아래 식) | 4 B | `+0x363 ~ +0x3d3` | 식 같음, 4 B 로 셈 |
| `+0x2d5` · `+0x2df` | BSSD `+0x10` · `+0x12` (word 둘) → `KernelBaseGlobalData+0x10` · `+0x12` | 2+2 B | `MOVUPS [R10+0x10]` | 같음 |
| `+0x2e9 ~ +0x346` | BSSD `+0x18` `Buffer` (하위 `+0x18`, 상위 `+0x1c` 검사) → `KernelBaseGlobalData+0x14` (같은 옮김 식) | 4 B | `+0x3f0 ~ +0x469` | 같음 |
| `_BaseNlsDllInitialize@12 +0xe ~ +0x16` | `[EBP+8]` (= BSSD) `+ 0x178` 의 **주소만** → `_gpServerNlsUserInfo` (rva `0x285790`). 안 읽음 | 4 B | `+0x9 ~ +0x10` | 같음 |

- **옮김 식** (32 비트 셈, `+0x2b6` · `+0x32d` 에 `[BSSD지역+0x9e8]` 읽음): `BSSD지역 = [T+8] − C + R` (`C` = PEB32 `+0x248` 하위 4 B, `R` = PEB32 `+0x4c`), `Buffer지역 = [BSSD+8] + BSSD지역 − [BSSD지역+0x9e8]`. 64 비트와 같은 식이다. 그래서 **차이만 쓰고, `R` 이 4 GB 아래이기만 하면 `C` 는 64 비트 높은 주소여도 맞다** (아래 4 GB 줄).
- **64 비트 표 (run68 · run71) 와 다른 칸: 없다.** 32 비트가 읽는 BSSD 오프셋 = `+0x0 · +0x2 · +0x8 · +0xc · +0x10 · +0x12 · +0x18 · +0x1c · +0x38 · +0x9e8` (+ `+0x178` 주소). 전부 run71 표에 이미 있는 칸. 달라진 것은 **폭** (8 B 칸의 하위 4 B), **`UNICODE_STRING` 줄임** (16 B → 8 B), **PEB 오프셋** 셋, **`KernelBaseGlobalData` 안 자리** (`+0x8 · +0xc · +0x10 · +0x12 · +0x14` 로 앞당겨짐 — 64 비트 `+0x8 · +0x10 · +0x18 · +0x20`).
- **PEB32 `ReadOnlySharedMemoryBase`(`+0x4c`) 가 4 GB 아래여야 하는 까닭**: 32 비트 코드는 `R` 하위 4 B 로 BSSD 지역 주소를 직접 간접 참조 (`[EDI+0x38]` 등). 이 한계는 **사용자 코드엔 없고 커널 몫** — 64 비트 `CsrpConnectToServer +0x338` 의 `NtMapViewOfSection` 은 `ZeroBits = 0` (`R9D = 0`, `+0x305 XOR R9D,R9D`), `BaseAddress = NULL`, 종류 `0x500000` (= `MEM_TOP_DOWN | SEC_NO_CHANGE`) 이다. WoW64 프로세스의 뷰가 4 GB 아래로 앉는 것은 커널이 `HighestUserAddress` (`0x7fff0000` / LAA `0xffff0000`) 로 자르기 때문 `[정적: run139 8 절 · MiSelectUserAddress 줄]`. 에뮬레이터는 이것을 흉내 내야 한다 (아래 "PE_emulator 쪽").

### 3-3. 표 (4-b) FALSE 로 나가는 가지 전부 (이유 1)

`__KernelBaseBaseDllInitialize@12` 의 FALSE 는 `+0x4d1` (`MOV ECX,[EBP-4]`) 로 가서 `+0x4d4 MOV AL,BL` (BL = 0, `+0x3d XOR EBX,EBX`) → `+0x4e0 RET 4`. `_RegKrnInitialize` (`+0x4ca`) 는 **건너뛴다**.

| # | 자리 (`__KernelBaseBaseDllInitialize@12`) | 조건 | 그 아래에서 FALSE 가 되는 까닭 |
|---|---|---|---|
| F1 | `+0x1b4/+0x1ba` · `+0x1bc JS +0x4d1` | `NtQuerySystemInformation(0, …, 0x2c)` 음수 | wow64 `whNtQuerySystemInformation_SystemBasicInformation` 이 native `0x3e` 호출 결과를 그대로 올림 (`+0x7b MOV EBX,EAX` · `+0x81 TEST EBX` · `+0x83 JS +0x162`) |
| **F2** | `+0x206/+0x20c` · `+0x20e JS +0x4d1` | **`CsrClientConnectToServer` 음수** | 아래 3-4 (원문 `0xc000003a` 의 길). 그 밖에 `0xc000000d` (블록 있는데 크기 0) · `0xc0000017` (힙 실패) · `NtConnectPort`·`NtMapViewOfSection` 실패 상태도 같은 길 |
| F3 | `+0x3b5/+0x3ba` · `+0x3bc JZ +0x4d1` | `_BaseNlsDllInitialize@12` 가 AL = 0 | `_BaseNlsDllInitialize@12 +0x1b/+0x20 CALL _NlsProcessInitialize@0`, `+0x22 JNZ +0x5b XOR AL,AL`. `_NlsProcessInitialize@0` 이 0 아닌 값: `_AllocTables@0` (`+0x11/+0x16` · `+0x1a JNZ`), `_SetupMainNlsFiles@4` (`+0x26/+0x2b` · `+0x2f JNZ`), `_MakeLocHashNode@4(0x7f)` 가 0 이면 `0x57` (`+0x4f/+0x54` · `+0x5c JNZ` · `+0x69 PUSH 0x57`) |
| F4 | `+0x3d2/+0x3d8` · `+0x3da JS +0x4d1` | `RtlInitializeCriticalSection(KernelBaseGlobalData+0x5c)` 음수 | (ntdll, 메모리 부족 급) |
| F5 | `+0x3eb/+0x3f0` · `+0x3f4 JZ +0x4d1` | `_ConsoleInitialize@0` 가 AL = 0 | 아래 표 (4-c) 넷 |

`entry` (DllMain, 이유 1) 쪽 FALSE (`ms/disassemble/kernelbase32/entry.asm`):

| # | 자리 | 조건 | 비고 |
|---|---|---|---|
| E1 | `+0x16/+0x1b` · `+0x24 MOV DL,AL` · `+0x57 CMP DL,1` · `+0x5a JNZ +0x113` | 안쪽 결과가 FALSE | F1 ~ F5 가 전부 여기로 합쳐져 `+0x114 MOVZX EAX,DL` = 0 반환 |
| E2 | `+0x82/+0x87 Initialize` (`ARI::Globals::Initialize`, rva `0x159865`) · `+0x89 JNS +0x92` · `+0x8b XOR DL,DL` · `+0x8d JMP +0x113` | HRESULT 음수 | **비패키지 프로세스는 늘 0 반환**: `Initialize +0xe0 MOV AL,[PEB32+3]` · `+0xe3 TEST AL,0x10` (`IsPackagedProcess`) · `+0xe5 JZ +0x39d` → `XOR ESI,ESI` → 반환 0 `[정적]`. 음수는 `AppModelPolicy_GetPolicy` 실패 때 `[추측]` |
| E3 | `+0x10c/+0x111 PcwDllMain` · `+0x111 MOV DL,AL` | `_PcwDllMain@12` AL = 0 | 이유 1 에서 `+0x16 ~ +0x2a`: **`NtQueryInformationProcess(-1, 0x1a, &x, 4, 0)`** (`ProcessWow64Information`) 음수면 `+0x54 XOR AL,AL`. 이것도 32 비트 초기화의 syscall 하나 (`_PcwDllMain@12.asm`) |

(4-c) `_ConsoleInitialize@0` 의 FALSE (`ms/disassemble/kernelbase32/_ConsoleInitialize@0.asm`, 모두 `+0x16f XOR AL,AL`):

| # | 자리 | 조건 |
|---|---|---|
| C1 | `+0x28/+0x2e` · `+0x30 JS +0x16f` | `RtlInitializeCriticalSection(&ConsoleStateLock)` 음수 |
| C2 | `+0xf4/+0xf9 _ConsoleCreateConnectionObject` · `+0xfb JNS` 아니면 `+0xfd CMP EAX,0xc0000022` · `+0x102 JNZ +0x168` (또는 로우박스 검사 실패 `+0x10f JS` · `+0x116 JZ`) | 콘솔 연결 객체 만들기 실패 (`0xc0000022` 말고는 바로 FALSE) |
| C3 | `+0x125/+0x12a _ConsoleAllocate` · `+0x12c JS +0x168` | 새 콘솔 할당 실패 |
| C4 | `+0x156/+0x15b _ConsoleSanitizeStandardIoObjects` · `+0x15d JNS` 아니면 `+0x163 _ConsoleCleanupConnectionState` → `+0x168` | 표준 입출력 정리 실패 |

### 3-4. 원문 "막힌 자리" `NtOpenSection(SharedSection, 4) = 0xc000003a` 는 어디로 가나

`[정적]` 한 줄씩 (모두 `.asm` 에서 다시 확인):

1. 64 비트 ntdll `CsrpConnectToServer +0x1b6/+0x1bb` `NtOpenSection` → `+0x1bf JS +0x46a` (rva `0xdc44e`) → `+0x47a/+0x47f RtlFreeHeap(CsrHeap, CsrPortName.Buffer)` → `+0x47f MOV EAX,EDI` → `+0x481 JMP +0xd9` (맺음 코드, EAX = `0xc000003a`). `CsrPortName.Buffer` 는 풀렸는데 전역에 그대로 남음 (0 으로 안 지움).
2. 64 비트 `CsrClientConnectToServer +0x113/+0x118` → `+0x11a JS +0xca` (출력 바이트 `*CalledFromServer` 도 안 씀) → 반환.
3. wow64 `whNtWow64CsrClientConnectToServer +0x1c/+0x23` 가 EAX 를 `[RSP+0x40]` 에 저장 (`+0x28`) → **상태를 안 보고** `+0x63` `PEB32+0x4c ← PEB64+0x88` (0) · `+0x7b` `PEB32+0x54 ← PEB64+0x98` (0) · `+0x8e` `PEB32+0x248 ← PEB64+0x380` (0) → `+0x95 MOV EAX,[RSP+0x40]` 로 같은 상태 반환. (nt 표의 오류 종류 칸이 0 이라 `Wow64SystemServiceEx` 도 상태를 안 바꿈 — sast/136.)
4. ntdll32 `CsrClientConnectToServer +0x16/+0x1b` → `+0x1f JS +0x38` (`_CsrClientProcess` 안 씀) → `+0x3b RET 0x14` 로 `0xc000003a` 반환.
5. kernelbase32 `+0x20c TEST EAX,EAX` · `+0x20e JS +0x4d1` → **F2** → `AL = 0` → 바깥 `+0x2a ~ +0x2d RET 4` → `entry +0x1b/+0x24 MOV DL,AL` · `+0x57 CMP DL,1` · `+0x5a JNZ +0x113` → `+0x114 MOVZX EAX,DL` (0) `RET 0xc`.
6. ntdll32 `_LdrpInitializeNode@4 +0x184/+0x189 _LdrpCallInitRoutine@16` → `+0x189 MOV [EBP-0x1a],AL` → 조각 `+0x21e CMP byte [EBP-0x1a],0` · `+0x222 JNZ` 아니면 → **`+0x247 MOV [EBP-0x28],0xc0000142`** (`STATUS_DLL_INIT_FAILED`). 이 상태가 올라가면 `__LdrpInitialize@8 +0x2a6/+0x2ab → _LdrpInitializationFailure@4` → `+0x5a PUSH 0xc0000145` · `+0x5f/+0x64 NtRaiseHardError` (원문 로그 657, 658 과 맞는 꼴 `[추측: 658 `NtTerminateProcess` 쪽은 안 따라감]`).

즉 **지금 에뮬레이터의 `0xc0000142` 의 뿌리는 `NtOpenSection(SharedSection)` 한 번의 실패** 이고, 이 실패는 F2 (연결) 하나로 모인다. 고치면 다음에 막힐 곳은 F3 (NLS) · F5 (콘솔) 순서로 `[추측]` (F4 는 ntdll 메모리 몫).

## 1. 32 비트 `CsrClientConnectToServer` 길 (둘째)

### 1-0. 길 한눈에 (누가 누구를 어디서 부르나)

```
kernelbase32 __KernelBaseBaseDllInitialize@12 +0x206/+0x20c   (call dword [0x1028db38])
 └ ntdll32 CsrClientConnectToServer (rva 0x72ab0, 62 B)  +0x16/+0x1b
     └ ntdll32 NtWow64CsrClientConnectToServer (rva 0x7ba30)  +0x0 MOV EAX,0x1e9 · +0xa CALL _Wow64SystemServiceCall · +0xc RET 0x14
         └ (wow64cpu → wow64 Wow64SystemServiceEx → nt 표 칸 0x1e9)
             └ wow64 whNtWow64CsrClientConnectToServer (rva 0x23200, 158 B)  +0x1c/+0x23  (call [0x18003c5f8] = import ntdll!CsrClientConnectToServer)
                 └ 64 비트 ntdll CsrClientConnectToServer (rva 0xdb7a0)  +0x113/+0x118
                     └ 64 비트 ntdll CsrpConnectToServer (rva 0xdbfe4)   ← run68 의 syscall 여섯
```

- `[정적] ms/disassemble/ntdll32/CsrClientConnectToServer.asm +0x16` (`CALL … NtWow64CsrClientConnectToServer`) · `ms/disassemble/ntdll32/NtWow64CsrClientConnectToServer.asm +0x0 ~ +0xc` · `ms/disassemble/wow64/whNtWow64CsrClientConnectToServer.asm +0x1c` · wow64 PE 가져오기 표 (`0x3c5f8` = `ntdll.dll!CsrClientConnectToServer`, 파이썬으로 PE 표 읽음).
- **요청서·원문의 전제 "32 비트 ntdll `CsrpConnectToServer` / `CsrpCaptureCsrPortServerInfo`" 는 없다.** `ntdll32` `_index.tsv` 의 `Csr*` 는 `CsrClientConnectToServer` · `CsrClientCallServer` · `CsrAllocateCaptureBuffer`… 썽크 12개와 `NtWow64Csr*` 10개뿐, `pdb_aliases.py --where ntdll32.dll CsrpConnectToServer` 도 "공개 심볼에 없음" (`Csrp*` 0개).
- 32 비트 `CsrClientConnectToServer` 본문 (`.asm` 줄): `+0x6 ~ +0x13` 인자 5개를 그대로 다시 push, `+0x16` 부르기, `+0x1b MOV EDX,EAX` · `+0x1f JS +0x38`, 성공이면 `+0x21 MOV CL,[EBP-1]` (출력 바이트) · `+0x26 JNZ +0x2f` · **`+0x28 MOV [_CsrClientProcess],1`** (바이트가 0 일 때) · `+0x2f ~ +0x36` 다섯째 인자 포인터가 있으면 `*ptr = CL`.

### 1-1. 표 (1) 연결 syscall 차례 — 32 비트 / 64 비트 (run68) 나란히

0 번이 32 비트 쪽 syscall (하나), 1 ~ 6 은 **같은 64 비트 코드** 가 낸다.

| # | syscall | 부르는 자리 | 인자 (WoW64 에서) | 64 비트 프로세스 (run68) | 같음 / 다름 |
|---|---|---|---|---|---|
| 0 | `NtWow64CsrClientConnectToServer` (서비스 `0x1e9`) | ntdll32 `CsrClientConnectToServer +0x16/+0x1b` | 32 비트 스택 인자 5개 (`ret 0x14`) → wow64 가 `RCX = 인자 포인터` 로 받음 | 없음 (64 비트는 바로 `CsrClientConnectToServer`) | **32 비트만** |
| 1 | `NtOpenSection` | 64 비트 `CsrpConnectToServer +0x1b6/+0x1bb` | `(&h [RSP+0x50], 4, OA{Length 0x30, Root 0, 이름 &CsrPortName = 디렉터리 + \SharedSection, Attributes 0x40})` | 같음 | 같음 |
| 2 | `NtCreateSection` | `+0x23d/+0x242` | `(&h [RSP+0x68], 0xf001f, NULL, &{0x10000}, PAGE_READWRITE 4, SEC_COMMIT 0x8000000, file 0)` | 같음 | 같음 |
| 3 | `NtConnectPort` (legacy, ALPC 아님) | `+0x2df/+0x2e4` | `(&CsrPortHandle, &CsrPortName = 디렉터리 + \ApiPort (54 B), SQOS{0, 2, 1, 1}, ClientView{0x30,…}, ServerView{0x18,…}, &최대메시지 [RSP+0x5c], 블록 0x30 B, &길이 [RSP+0x58] = 0x30)` | 같음 | 같음 |
| 4 | `NtClose` | `+0x2eb/+0x2f0` | 만든 섹션 `[RSP+0x68]` | 같음 | 같음 |
| 5 | `NtMapViewOfSection` | `+0x338/+0x33d` | `(SharedSection, -1, &R (= [RSP+0x70], 0 에서 시작), ZeroBits 0, Commit 0, 오프셋 NULL, &크기 [RBP-0x40] (0 에서 시작), ViewUnmap 2, 0x500000, PAGE_READONLY 2)` | 같음 | 같음 — 다만 WoW64 에선 `R` 이 4 GB 아래로 앉음 (커널 몫) |
| 6 | `NtClose` | `+0x344/+0x349` | `SharedSection` 핸들 | 같음 | 같음 |
| — | (사용자 모드) `RtlpCreateHeap(0x8000, 포트 뷰, 0x10000, …)` | `+0x3f3/+0x3f8` | 포트 뷰 (아래) 안에 개인 힙 | 같음 | 같음 |

- `[정적] ms/disassemble/ntdll/CsrpConnectToServer.asm` 위 줄들, `CsrClientConnectToServer.asm +0x113/+0x118`. 인자 칸은 asm 스택 자리 (`RBP = RSP + 0x100`) 로 직접 셈: `PORT_VIEW` 는 `RBP-0x88`, 원격 뷰 `RBP-0x58`, 블록 `RBP-8`.
- 인자 변환 (wow64 → 64 비트 `CsrClientConnectToServer`): `whNtWow64CsrClientConnectToServer +0x4 ~ +0x17` — `RCX = [스택+0]` 디렉터리 포인터 (0 확장), `EDX = [+4]` 서버 번호, `R8 = [+8]` 블록 포인터 (0 확장), `R9D = [+0xc]` **크기 값 8**, `[RSP+0x20] = [+0x10]` 출력 바이트 포인터 (0 확장). 포인터들은 모두 32 비트 스택·데이터를 가리키므로 64 비트 코드가 4 GB 아래 주소를 직접 읽는다 (`CsrpConnectToServer +0x295 MOV RAX,[R15]` 8 B).
- 64 비트 `CsrClientConnectToServer` 가 서버 길이 아닌 클라이언트 길로 가려면 `PEB64.ImageBaseAddress` 의 PE 헤더 `Subsystem` ≠ 1 (`+0x5e ~ +0x81 RtlImageNtHeaderEx` · `+0x81 CMP [RAX+0x5c],1`). WoW64 에서는 이 칸이 **32 비트 exe 의 헤더** (같은 오프셋 `+0x5c`). test 계열 x86 exe (콘솔, 3) 는 클라이언트 길.

### 1-2. 표 (2) 연결 정보 블록 칸 (32 / 64)

`NtConnectPort` 로 가는 블록은 **64 비트 ntdll 이 짓는다** (`+0x295 ~ +0x2df`) → WoW64 도 같은 `0x30` B, **포인터 칸은 8 B**. 32 비트 코드가 넘기는 것은 아래 "사용자 정보" 8 B 뿐.

| 블록 칸 | 크기 | 방향 | 뜻 | 64 비트 프로세스 | WoW64 (32 비트 클라이언트) | 근거 |
|---|---|---|---|---|---|---|
| `+0x00` | 8 | 응답 | `SharedSectionBase` (**C**, csrss 쪽 뷰 주소) → PEB64 `+0x380` | 같음 | 같음 (PEB32 `+0x248` 8 B 로 다시 옮김) | `CsrpConnectToServer +0x364/+0x36d` ; csrsrv `CsrApiHandleConnectionRequest +0x9c ~ +0xa6` (메시지 `+0x28`) |
| `+0x08` | 8 | 응답 | `SharedStaticServerData` (표 **T**, csrss 쪽 주소) → PEB64 `+0x98 = T − C + R` | 같음 | 같음 | `+0x380 ~ +0x38d` ; csrsrv `+0xaa ~ +0xb1` (메시지 `+0x30`) |
| `+0x10` | 8 | 응답 | csrss 프로세스 ID → `CsrProcessId` | 같음 | 같음 | `+0x398 ~ +0x39c` ; csrsrv `+0xcc ~ +0xd0` (메시지 `+0x38` = `TEB.ClientId.UniqueProcess`) |
| `+0x18` | 8 | 안 씀 | 0 | 같음 | 같음 | 0 으로 채움 (`+0x7f ~ +0x8b`), 읽는 줄 없음 |
| `+0x20` | 4 | 요청 | 서버 번호 = 1 | 같음 | 같음 | `+0x29c MOV [RBP+0x18],EDI` ; csrsrv `CsrpHandleInlineUserConnect +0x2d CMP [RCX+0x48],1` |
| `+0x24` | 4 | 응답 | 서버 DLL connect 루틴 반환 (0 = 성공) → `CsrClientConnectToServer` 지역 상태 | 같음 | 같음 | `+0x2f8 MOV EAX,[RBP+0x1c]` · `+0x308 MOV [R13],EAX` ; csrsrv `+0x84 MOV [RBX+0x4c],EAX` |
| `+0x28` | 8 | 요청 | **사용자 정보** = 호출자 블록 8 B 를 그대로 복사 | `CtrlRoutine` 64 비트 주소 8 B | **`{CtrlRoutine32, 0}`** (하위 4 B = 32 비트 주소, 상위 4 B = 0 → 0 확장 값) | `+0x295 MOV RAX,[R15]` · `+0x298 MOV [RBP+0x20],RAX` ; kernelbase32 `+0x1ec` · `+0x1ff` ; basesrv `BaseClientConnectRoutine +0x0 CMP [R8],8` · `+0x6 MOV RAX,[RDX]` · `+0x10 MOV [RCX],RAX` |

- 블록 전체 길이 = `0x30` (`+0x251 LEA ECX,[RSI+0x30]` → `[RSP+0x58]` 와 `PORT_VIEW.Length`). 호출자 크기 인자가 8 이 아니거나 서버 번호가 1 이 아니면 `+0x282 CMP R12D,8` · `+0x28c CMP R14D,EDI` 가 `+0x411` (`MOV [RBP+0x1c],0xc000000d`) 로 가서 상태 칸 `+0x24` 에 `0xc000000d` 를 미리 넣고 사용자 정보는 복사 안 한 채 **연결은 그대로 한다** (csrss 가 `CsrpHandleInlineUserConnect +0x15 CMP [RCX+0x4c],0` 로 건너뜀).
- csrss 쪽 메시지 오프셋 = 블록 오프셋 + `0x28` (PORT_MESSAGE 머리). 두 쪽 줄이 맞물림 (`+0x20` ↔ `msg+0x48`, `+0x24` ↔ `msg+0x4c`, `+0x28` ↔ `msg+0x50`) `[정적]`. 커널의 legacy `NtConnectPort` → ALPC 변환 본문은 안 봄 `[공백]`.
- **포트 뷰 구조체 (클라이언트 · 서버 뷰)** — 64 비트 `CsrpConnectToServer` 가 짓는 것:
  - 클라이언트 `PORT_VIEW` (`0x30` B, `RBP-0x88`): `+0x0 Length = 0x30` · `+0x8 SectionHandle` (이름 없는 `0x10000` B 섹션) · `+0x10 SectionOffset` (4 B) = 0 · `+0x18 ViewSize = 0x10000` · `+0x20 ViewBase = 0` (**출력**) · `+0x28 ViewRemoteBase = 0` (**출력**).
  - 서버 `REMOTE_PORT_VIEW` (`0x18` B, `RBP-0x58`): `+0x0 Length = 0x18` · `+0x8 ViewSize = 0` · `+0x10 ViewBase = 0`. 읽는 줄 없음 (그냥 받아 둠).
  - 연결 뒤 읽는 것: `ViewRemoteBase − ViewBase` → `CsrPortMemoryRemoteDelta` (`+0x3a3 ~ +0x3ab`), `ViewSize` · `ViewBase` → `RtlpCreateHeap(0x8000, ViewBase, ViewSize, …)` (`+0x3b7 ~ +0x3f8`).

### 1-3. 표 (5) 쓰는 PEB32 칸 · 32 비트 `Csr*` 전역

| 칸 | 크기 | 쓰는 이 | 값 | 줄 |
|---|---|---|---|---|
| PEB32 `+0x4c ReadOnlySharedMemoryBase` | 4 | **wow64** (연결 뒤, 상태 안 보고) | `PEB64 +0x88` 하위 4 B | `whNtWow64CsrClientConnectToServer +0x4e ~ +0x63` (`MOV [RDX+0x4c],ECX`) |
| PEB32 `+0x54 ReadOnlyStaticServerData` | 4 | wow64 | `PEB64 +0x98` 하위 4 B | `+0x66 ~ +0x7b` |
| PEB32 `+0x248 CsrServerReadOnlySharedMemoryBase` | **8** | wow64 | `PEB64 +0x380` 통째 | `+0x7e ~ +0x8e` (`MOV [RDX+0x248],RCX`) |
| (PEB32 → 어디서 오나) | — | `RDX = [TEB32 + 0x30]`, `TEB32 = TEB64 + [TEB64+0x180c]` (`+0x2c ~ +0x4b`) | `[TEB64+0x180c]` 는 `0x2000` (run139 `MmCreateTeb +0x14a`) | |
| PEB64 `+0x88 · +0x98 · +0x380` | 8 · 8 · 8 | 64 비트 `CsrpConnectToServer` (run68 그대로) | `+0x379` · `+0x38d` · `+0x36d` | |
| 32 비트 ntdll `_CsrClientProcess` (rva `0x13911c`, 1 B) | 1 | ntdll32 `CsrClientConnectToServer +0x28` | 1 (연결 호출 성공이고 출력 바이트 = 0 일 때) | `_refs/_CsrClientProcess.tsv` (쓰기 1 · 읽기 1: `RtlRegisterThreadWithCsrss +0x54641`) |
| 32 비트 ntdll `_CsrInitOnceDone` (rva `0x13911d`, 1 B) | 1 | **이 길에선 아무도 안 씀** | — | `_refs`: 읽기 1 (`RtlRegisterThreadWithCsrss +0x1f`) |
| 32 비트 ntdll `_CsrServerApiRoutine` (rva `0x139118`, 4 B) | 4 | **이 길에선 아무도 안 씀** | — | `_refs`: 읽기 2 (`+0x5464d` · `+0x54672`) |
| **64 비트** ntdll `Csr*` 전역 9개 (WoW64 프로세스의 64 비트 ntdll 안) | | 64 비트 코드 (run68 그대로) | `CsrInitOnceDone` ← 1 (`+0xe7`) · `CsrHeap` ← **PEB64.ProcessHeap** (`+0xf2`) · `CsrClientProcess` ← 1 (`+0x8f`) · `CsrPortName` · `CsrPortHandle` · `CsrProcessId` · `CsrPortMemoryRemoteDelta` · `CsrPortHeap` · `CsrPortBaseTag` | `CsrClientConnectToServer`·`CsrpConnectToServer` 줄 (run68) |
| kernelbase32 쓰는 전역 | | `__KernelBaseBaseDllInitialize@12` | `_BaseStaticServerData` rva `0x2848cc` (`+0x248`) · `_BaseRCNumber` `0x2848d0` (`+0x259`) · `KernelBaseGlobalData` `0x2848e0` (+0x0 `+0x137`, +0x4 `+0x252`, +0x8 `+0x262`, +0xa `+0x26c`, +0xc `+0x2cf`, +0x10 `+0x2d9`, +0x12 `+0x2e3`, +0x14 `+0x346`) · `_BasepInitializationStage` `0x284954` (`+0x1d7` ← 0x64 · `+0x219` ← 0xc8 · `+0x3af` ← 0x190 · `+0x3cc` ← 0x1f4 · `+0x3e5` ← 0x258 · `+0x405` ← 0x2bc) | |

- **PEB32 의 이 세 칸은 연결 전에는 커널이 안 쓴다** (run139 의 27칸 목록에 없음) → 연결 전에 읽으면 0.
- 64 비트 `CsrHeap = PEB64.ProcessHeap` 이므로 WoW64 프로세스의 **64 비트 PEB `+0x30` 에 쓸 수 있는 힙이 이미 있어야** `CsrpConnectToServer +0x119 RtlAllocateHeap` 이 통과한다 (원문 로그는 648 까지 갔으니 있음 `[관찰]`).

## 2. wow64 썽크 넷 (셋째)

CSR 연결은 이 넷을 **거치지 않는다** (1-1 표). 32 비트 코드가 이 넷을 **직접** 부를 때의 변환이다 (예: 32 비트 로더가 DLL 섹션을 만들 때). 이 요청의 질문이라 본문을 읽어서 남긴다. `whNt*` 입구 `RCX` = 32 비트 스택의 인자 포인터. 핸들은 `MOVSXD` (부호 확장), 포인터·정수는 0 확장.

### 2-1. 표 (3-1) 포트 뷰 구조체 32 ↔ 64 변환

`whNtConnectPort` · `whNtSecureConnectPort` 가 쓴다 (`Wow64pThunkLegacy*` 도우미 넷).

| 구조체 | 32 비트 (칸) | 64 비트 (칸) | 안 (32→64) | 밖 (64→32) | 줄 |
|---|---|---|---|---|---|
| `PORT_VIEW` (클라이언트) | `0x18`: `+0 Length` · `+4 SectionHandle` · `+8 SectionOffset` · `+0xc ViewSize` · `+0x10 ViewBase` · `+0x14 ViewRemoteBase` | `0x30`: `+0 Length` · `+8 SectionHandle` · `+0x10 SectionOffset` (4 B) · `+0x18 ViewSize` · `+0x20 ViewBase` · `+0x28 ViewRemoteBase` | `Length == 0x18` 일 때만 변환 (`Wow64pThunkLegacyPortViewIn +0x2c`): `Length ← 0x30`, `SectionHandle` **부호 확장**, 나머지 **0 확장**. 임시 힙 `0x30` B (`+0x6d ~ +0x92`), 표시 바이트 ← 1 (`+0x99`). `Length` 가 0x18 이 아니면 **변환 없이 포인터 그대로** (`+0x2f JNZ +0xb5`) | `Wow64pThunkLegacyPortViewOut +0x8 CMP [RDX],0x18` 이면 `Length ← 0x18` 하고 **하위 4 B 로 자름** (`+0x13 ~ +0x3e`): `SectionHandle` · `SectionOffset` · `ViewSize` · `ViewBase` · `ViewRemoteBase`. 반환은 늘 0 (`+0x36 MOV EAX,R8D`) — **4 GB 넘는지 안 봄** | `Wow64pThunkLegacyPortViewIn.asm`, `…PortViewOut.asm` |
| `REMOTE_PORT_VIEW` (서버) | `0xc`: `+0 Length` · `+4 ViewSize` · `+8 ViewBase` | `0x18`: `+0 Length` · `+8 ViewSize` · `+0x10 ViewBase` | `Length == 0xc` 일 때 (`…RemoteViewIn +0x28`): `Length ← 0x18`, `ViewSize` ← `[+4]`, `ViewBase` ← `[+8]` (둘 다 0 확장). 임시 힙 `0x20` B | `…RemoteViewOut +0x8`: `Length ← 0xc`, 하위 4 B 로 자름 | `…RemoteViewIn.asm`, `…RemoteViewOut.asm` |
| `SECURITY_QUALITY_OF_SERVICE` | `0xc` | `0xc` (포인터 없음) | 임시 `0x10` B 에 `Length = 0xc`, `ImpersonationLevel` · `ContextTrackingMode` · `EffectiveOnly` 복사 (`Wow64ShallowThunkAllocSecurityQualityOfService32TO64_FNC +0x65 ~ +0x85`) | — | `…SecurityQualityOfService32TO64_FNC.asm` |
| 포트 이름 `UNICODE_STRING` | 8 B (`Length`·`MaximumLength`·`Buffer` 32) | 16 B | 썽크 안에서 바로 (`whNtConnectPort +0x8a ~ +0x9d`): `Length` · `MaximumLength` 복사, `Buffer` **0 확장** | — | `whNtConnectPort.asm`, `whNtSecureConnectPort.asm +0x8a ~ +0x9d` |

### 2-2. 표 (3-2) 썽크 넷 한 줄씩

| 썽크 (rva) | 32 비트 인자 → 64 비트 | 4 GB 아래 요구 | 연결 정보 | status 를 바꾸는 줄 | 핸들 쓰기 |
|---|---|---|---|---|---|
| `whNtOpenSection` (`0x27a0`, 슬롯 `0x37`, 인자 `0xc`) | `[0]` 핸들 포인터 (지역 8 B 에 받음) · `[4]` 접근 · `[8]` OA → `Wow64ShallowThunkAllocObjectAttributes32TO64_FNC` (`+0x33/+0x38`) | **없음** | — | OA 변환 실패 (`+0x3c TEST EAX` · `+0x3e JS +0x60`) → 핸들러 `MOV EAX,[RSP+0x50]` 로 **그 상태 반환** (`0xc000000d` OA `Length ≠ 0x18`, `0xc0000017` 임시 힙 실패). **그 밖엔 `NtOpenSection` 결과 그대로** (`+0x4a/+0x51`) | `+0x56 ~ +0x5c`: 출력 포인터가 있으면 **하위 4 B 를 상태와 무관하게** 씀 |
| `whNtCreateSection` (`0x2830`, `0x4a`, `0x1c`) | `[0]` 핸들 · `[4]` 접근 · `[8]` OA (변환) · `[0xc]` **최대 크기 포인터 그대로** · `[0x10]` 보호 · `[0x14]` 속성 · `[0x18]` 파일 핸들 (**부호 확장**) | 없음 | 최대 크기 `LARGE_INTEGER*` 는 포인터만 넘김 | OA 변환 실패 (`+0x47/+0x4c` · `+0x55 JS +0x8c`) 만 | `+0x82 ~ +0x88` 하위 4 B |
| `whNtConnectPort` (`0x1f870`, `0xa4`, `0x20`) | 표 2-1. `[0xc]` 클라이언트 뷰 · `[0x10]` 서버 뷰 · `[0x14]` 최대 메시지 길이 포인터 · `[0x18]` 연결 정보 포인터 · `[0x1c]` 연결 정보 길이 포인터 | **없음** (뷰 주소는 커널이 정해서 `ViewBase` 로 돌려줌, 위 표의 자르기만) | **포인터·길이 포인터 그대로 (0 확장)**, 길이 안 바꿈. 이름·QoS·뷰만 변환 | 뷰 변환 실패 (`+0x1aa/+0x1af` · `+0x1ca/+0x1cf`, `Wow64pThunkLegacy*In` 가 `0xc0000017` 만 줌) · `PortViewOut` 실패 (`+0x171/+0x176 → +0x17e JNS` 아니면 `_local_unwind`). **그 밖엔 `NtConnectPort` 결과 그대로** (`+0x10e/+0x115`, `+0x11a MOV EBX,EAX`) | `+0x120 ~ +0x128` 하위 4 B (성공이든 아니든 — 실패면 지역값 0). **성공이고 (뷰를 변환했거나 `Wow64pIsLegacyLpcPort(이름) == 1`) 면 핸들 `\|= 1`** (`+0x14d/+0x152`, `+0x159 CMP AL,1`, `+0x194 OR dword [R15],1`) — 옛 LPC 포트 표시 |
| `whNtSecureConnectPort` (`0x1f390`, `0x194`, `0x24`) | 위와 같고 `[0x10]` `RequiredServerSid` **포인터 그대로** (`+0x3f MOV R13D,[RCX+0x10]` → `+0x1a9 MOV [RSP+0x20],R13`), 서버 뷰 `[0x14]`, 길이들 `[0x18]·[0x1c]·[0x20]` | 없음 | 같음 (그대로) | SQOS (`+0xb2/+0xb7`) · 뷰 입력 (`+0x117/+0x11c`, `+0x156/+0x15b`) · 뷰 출력 (`+0x1ee/+0x1f3`, `+0x229/+0x22e`) 실패만. 그 밖엔 `NtSecureConnectPort` 결과 그대로 (`+0x1c1/+0x1c8`) | `+0x1d1 ~ +0x1d8` 하위 4 B. **`\|= 1` 표시는 없음** |
| `whNtMapViewOfSection` (`0x9640`, `0x28`, `0x28`) | `[0]` 섹션 · `[4]` 프로세스 (둘 **부호 확장**) · `[8]` `*BaseAddress` 포인터 (값은 지역 8 B 에 0 확장) · `[0xc]` ZeroBits · `[0x10]` 커밋 · `[0x14]` 오프셋 포인터 (그대로) · `[0x18]` `ViewSize` 포인터 (`Wow64ShallowThunkSIZE_T32TO64` `+0xa8/+0xad`) · `[0x1c]` 상속 · `[0x20]` 할당 종류 · `[0x24]` 보호 | **있음**: `*Base == 0` (주소를 커널이 고름) **이고** `ZeroBits == 0` 이면 `ZeroBits ← Wow64pMaxAddress` (`+0xb5 CMP [RBX],0` · `+0xbb TEST RDI` · **`+0xbe CMOVZ RDI,[0x180052010]`** · `+0xc6 MOV [RSP+0x78],RDI`). 이 값은 `Wow64InitializeEmulatedSystemInformation +0x2f/+0x33` `NtQuerySystemInformation(0x3e)` 의 `+0x28` (`+0x43/+0x48`) | — | 네이티브 결과 그대로 (`+0x1b5/+0x1bc`). 바꾸는 곳: (가) 성공이고 `TEB32 +0x14` (ArbitraryUserPointer) ≠ 0 이고 `RtlIsCurrentProcess` (`+0x255/+0x25c`) 이고 `NtQuerySection` (`+0x288/+0x28f`) 성공일 때만: 기계 불일치 `Wow64pIsCompatibleMachineType` (`+0x2c3/+0x2c8`) → **`0x4000000e`** (`+0x33b`), `CpuNotifyMapViewOfSection` (`+0x2f5/+0x2fa`) 실패 → `NtUnmapViewOfSection` (`+0x306/+0x30d`) 후 그 상태 | `+0x1ca/+0x1cc` `*Base` 하위 4 B (상태 무관). `ViewSize`: 4 GB 넘으면 **`0xffffffff` 로 줄임** (`+0x1d8 ~ +0x1e4 CMOVC`) — 자르기 아님 |

- 객체 이름 변환 (OA 썽크) 은 **`RootDirectory == 0` 이고 이름이 `\??\C:\Windows…` 로 시작하지 않으면 이름을 그대로 둔다**: `RedirectPath +0x7b ~ +0x92` 가 `WindirPath` 접두가 아니면 `0xc0000272` 를 돌려주고, OA 썽크는 그 값을 무시하고 성공 (`+0x352 TEST EAX` · `+0x35a ~ +0x36f` 로그 후 `EAX = [RSP+0x30]`). `\Sessions\1\Windows\SharedSection` 같은 이름은 안 바뀐다. (`0xc0000272` 이름 뜻 `[공백]`)
- `Wow64pIsLegacyLpcPort` 가 보는 이름 (`LegacyLpcPortName`, `\BaseNamedObjects\Global\%s` 꼴 — `%s` 는 `Wow64pGetLegacyLpcPortName +0x37 ~ +0x7e` 의 환경 변수) 은 이 CSR 길에 안 껴서 안 따라감 `[공백]`.
- 4 GB 조건 정리: **포트 뷰를 4 GB 아래로 요구하는 줄은 없다** (커널이 `HighestUserAddress` 로 정함, 변환은 자르기만 — 넘으면 조용히 잘림). 요구가 있는 곳은 `whNtMapViewOfSection` 의 `ZeroBits ← Wow64pMaxAddress` 하나, 그리고 **64 비트 ntdll 이 직접 부르는 `NtMapViewOfSection` (CSR 연결) 은 `ZeroBits = 0` 이라 이 요구 없이 커널의 `HighestUserAddress` 한계에만 기댄다.**

## csrsrv — WoW64 클라이언트를 따로 다루는 줄 (되면)

**없다** (읽은 범위 안).

- 읽은 것: `CsrApiHandleConnectionRequest` (586 B, 연결 요청을 받아 `NtAlpcAcceptConnectPort` 로 수락, 메시지에 `+0x28` C · `+0x30` T · `+0x38` csrss PID 를 씀) · `CsrpHandleInlineUserConnect` (197 B, 서버 번호 1 이고 상태 칸이 0 이면 `CsrLoadedServerDll` 의 connect 루틴을 `(프로세스, 메시지+0x50, &8)` 로 부름) · `CsrProcessLazyRegister` (1089 B, 보낸 쪽 프로세스를 `NtAlpcOpenSenderProcess` 로 열고 `NtQueryInformationProcess` 둘: **`0x18` 세션 번호** · **`0x25` 이미지 정보** — 세션이 csrss 와 같고 `SubSystemType − 2 ≤ 1` (GUI 2 · CUI 3) 일 때만 등록).
- `ProcessWow64Information (0x1a)` 질문도 **기계 종류 (`0x14c`) 비교도 없다**. csrsrv `_index.tsv` 129 개 함수 이름에도 `wow`·`32` 없음. 64 비트와 32 비트 클라이언트가 같은 길.
- basesrv `BaseClientConnectRoutine` (`ms/disassemble/basesrv/BaseClientConnectRoutine.asm`): 크기 8 이 아니면 `0xc000000d` (`+0x0 CMP [R8],8` · `+0x1e`), 8 이면 사용자 정보 qword 를 `*[CSR프로세스 +0x90]` 에 저장 (`+0x6 ~ +0x10`), `*크기 ← 0`, 반환 0. **WoW64 이면 그 qword = `{CtrlRoutine32, 0}` = 0 확장 32 비트 주소.** 그래서 kernelbase32 가 일부러 8 B 를 만들어 보낸다.
- 공유 섹션 만들 때 (`CsrSrvCreateSharedSection +0x17e ~ +0x1b8` `NtCreateSection(0xf001f, 이름 …\SharedSection, 크기, PAGE_READWRITE 4, 속성 0x4000000 = SEC_RESERVE)`, `+0x1f0 ~ +0x22c` csrss 자기 뷰 `NtMapViewOfSection(…, MEM_TOP_DOWN 0x100000, PAGE_READWRITE 4)`) — 64 비트 csrss 라 높은 주소 (**C**). 그래서 WoW64 클라이언트는 **C ≠ R** 이 정상이다 (원문 사진 64 비트도 `R = 0x7ff4ecf70000` ≠ `C = 0x7ff5575f0000`, run71).

## PE_emulator 쪽 — 지금 맞나·틀리나

원문 "답이 오면 쓰는 곳" (`Src/nt/TeamSyscall.cpp` · `Src/nt/AddressSpace.cpp` `MapCsrSharedSectionView` · `MapCsrPortView` · 팀원 `csr_user_connect_stub.cpp`) 에 쓸 꼴. PE_emulator 쪽은 읽기만 했고 아래 "지금" 줄은 그 코드 몇 줄을 본 것이다 (`[정적: PE_emulator 코드]`).

### 틀린 전제 셋 (원문 "이미 아는 것" 과 "알고 싶은 것 1")

1. "wow64 가 64 비트 syscall 로 **바꿈**" 은 **틀림**. wow64 가 바꾸는 게 아니라 **64 비트 ntdll 함수를 직접 부른다** (`whNtWow64CsrClientConnectToServer +0x1c`). `whNtOpenSection` 류 썽크는 이 연결에서 **안 불린다**.
2. "32 비트 ntdll `CsrpConnectToServer`" 는 **없다** (위 1-0).
3. `ms/decomp/ntdll32` 에 `Csr*` 가 모자란 게 아니라 **원래 그 정도뿐** (`CsrClientConnectToServer` 한 줄 + `NtWow64Csr*` 스텁).

### 지금 코드와 맞대기

| 자리 | 지금 (읽은 줄) | 판정 · 해야 할 것 |
|---|---|---|
| 64 비트 프로세스 | `Src/ntdll/BaseDllInit.h:30 ~ :31` — 연결 결과를 엔진이 직접 씀 (syscall 안 냄), 뷰는 `MapCsrSharedSectionView` · `MapCsrPortView` | **WoW64 에는 그대로 못 씀**: 32 비트 `__KernelBaseBaseDllInitialize@12` 는 진짜 코드로 돌고, 연결은 wow64 + **64 비트 ntdll 진짜 코드 (`CsrClientConnectToServer`)** 가 하므로 **진짜 syscall 여섯이 `TeamSyscall` 로 들어온다**. 즉 64 비트에선 한 번도 안 나온 `NtOpenSection(\Sessions\N\Windows\SharedSection)` · `NtCreateSection` · `NtConnectPort(…\ApiPort)` · `NtMapViewOfSection(공유 섹션)` 을 처음으로 대답해야 한다 |
| `MapCsrSharedSectionView` · `MapCsrPortView` (`Src/nt/AddressSpace.cpp:1421 ~ 1465`) | 위 끝이 3 인자 `FindFreeRangeDown` = `SYSTEM_MAXIMUM_USER_MODE_ADDRESS` (`Src/nt/VirtualMemory.cpp:1450 ~ 1453`) | **WoW64 프로세스에 쓰면 뷰가 4 GB 위에 앉을 수 있다** → PEB32 `+0x4c` 하위 4 B 가 잘려 32 비트 코드가 엉뚱한 주소를 읽는다. 위 끝을 `refProcess.qwHighestUserAddress − 1` (NtMapViewOfSection 쪽 `AddressSpace.cpp:739` 가 이미 그렇게 함) 로 |
| `NtConnectPort` (`ext/syscall/.../layer.cpp:322`, `alpc/nt_legacy_port_messages.cpp:124 ~ 195`) | 이름 조회 전 사용자 입력 확인만, "실제 연결 endpoint와 서버 응답은 만들지 않는다" | **이번에 필요한 대답이 없다**: csrss ApiPort 연결 (서버 번호 1, 블록 `0x30` B) 에 응답 블록을 써 주고 클라이언트 뷰를 붙이는 일 |
| `csr_user_connect_stub.cpp` | `csr_api_port(name)` 이 `\Sessions\<n>\Windows\ApiPort` 를 알아보지만 **USERSRV 연결 (서버 3 · `0x248` B · API 0)** 만 대답 (`NtAlpcSendWaitReceivePort`) | **이번 길 (BASESRV 연결, `NtConnectPort`, 서버 1, 블록 `0x30` B) 이 아님**. 32 비트 user32 의 연결은 별개 (아래 다음 요청 후보 5) |

### 선택 (사용자·팀 결정) — 두 가지 길

- **길 A (진짜 코드 그대로, 지금 로그와 같은 길)**: 아래 "stub 이 채울 최소 목록" 을 `TeamSyscall` 에서 6 syscall 로 대답. 엔진 코드 최소. 32 비트 쪽은 한 줄도 모방 안 함.
- **길 B (엔진이 결과만 씀)**: `NtWow64CsrClientConnectToServer` (서비스 `0x1e9`) 를 `TeamSyscall` 이 가로채고, **64 비트 PEB `+0x88 · +0x98 · +0x380` 과 PEB32 `+0x4c · +0x54 · +0x248` 을 직접 씀** + 64 비트 ntdll `Csr*` 전역 9개 (이후 `NtWow64CsrClientCallServer` 등이 읽음) 도 직접. wow64 본문을 건너뛰는 선택이라 "진짜 코드" 원칙과는 어긋남 `[추측: 팀 규칙 확인 필요]`. 이 문서의 표는 길 A 기준으로 쓴다.

### stub 이 채울 최소 목록 (길 A, WoW64 클라이언트)

모두 `[정적]` 위 표들.

| # | syscall (64 비트 모양) | 돌려줄 것 | 출력 칸 | 뷰 주소 조건 |
|---|---|---|---|---|
| 1 | `NtOpenSection(&h, 4, OA)` — OA 이름 = `\Sessions\<세션>\Windows\SharedSection` (`OBJ_CASE_INSENSITIVE`) | `STATUS_SUCCESS`. **`0xc000003a` 이면 위 3-4 의 `0xc0000142` 로 감** | `*h` = 핸들 (임의, 아래 5 · 6 이 씀) | — |
| 2 | `NtCreateSection(&h2, 0xf001f, NULL, &0x10000, 4, 0x8000000, 0)` | 성공 | `*h2` = 이름 없는 `0x10000` B 섹션 핸들 | — |
| 3 | `NtConnectPort(&CsrPortHandle, \…\Windows\ApiPort (54 B), SQOS{0,2,1,1}, ClientView{0x30, h2, 0, 0x10000, 0, 0}, ServerView{0x18,0,0}, &max, 블록 0x30 B, &0x30)` | 성공 | ① `CsrPortHandle` (ntdll64 rva `0x1d4e20`) ② `ClientView.ViewBase` (`+0x20`) ≠ 0 · `ViewRemoteBase` (`+0x28`) · (`ViewSize` 는 그대로 `0x10000`) — 뷰는 **읽기·쓰기** 가능해야 함 (`RtlpCreateHeap` 이 힙을 만듦) ③ 블록 응답: `+0x00 C` · `+0x08 T` (csrss 쪽 주소로 표현) · `+0x10` csrss PID · `+0x24` = 0 | 클라이언트 뷰는 64 비트 코드가 쓰니 4 GB 제한은 없지만 WoW64 프로세스는 커널이 `HighestUserAddress` 로 자름 → 같은 규칙 |
| 4 | `NtClose(h2)` | 성공 | — | — |
| 5 | `NtMapViewOfSection(h, -1, &R, 0, 0, NULL, &크기, 2, 0x500000, 2)` | 성공 | `*R` (**4 GB 아래, `HighestUserAddress` 아래**) · `*크기` | **R < 4 GB 필수** (PEB32 `+0x4c` 4 B 에 하위 4 B 만 들어감). 읽기 전용 뷰 |
| 6 | `NtClose(h)` | 성공 | — | — |

- **뷰 안 내용 (32 비트가 읽는 것)** — run71 "최소 칸 목록" 과 같다: 표 `T` (`R + (T − C)`) 의 `T[1]` (8 B) = `S` (BSSD 의 csrss 쪽 주소, `C + 오프셋`), BSSD `0xa00` B: `+0x38` word (0 가능) · `+0x0`/`+0x10` `UNICODE_STRING` (`Buffer` 가 0 이면 건너뜀) · **`+0x9e8` 8 B = `S`** (`Buffer` 가 0 아니면 필수) · `+0x178` 영역 (주소만 쓰임, 0 으로). 식 `BSSD지역 = [T+8] − C + R` 이 32 비트 하위 4 B 로 맞으려면 **`T − C` 와 `S − C` 가 4 GB 안**이어야 한다 (`C` 자체는 64 비트 높은 주소여도 됨).
- **R == C 로 두는 줄이기**: 가능하다. 단 WoW64 에서는 `C` 도 4 GB 아래여야 한다 (PEB32 `+0x248` 의 하위 4 B 와 `R` 하위 4 B 가 같아져야 옮김이 0).
- **이미 있어야 하는 칸 (AR04 에서 채운 것)**: TEB64 `+0x180c` = `0x2000` (`whNtWow64CsrClientConnectToServer +0x35 ~ +0x4b` 이 읽음, 0 이면 PEB32 를 못 찾음) · TEB32 `+0x30` = PEB32 · PEB64 `+0x10 ImageBaseAddress` = 32 비트 exe 헤더 (서브시스템 읽음) · PEB64 `+0x30 ProcessHeap` 유효 · PEB32 `+0x1d4 SessionId` (`\Sessions\<n>` 만드는 데 읽음, 디렉터리 이름이 syscall 1 ~ 3 의 이름이 됨).
- **그 뒤 32 비트 쪽이 요구하는 것**: `NtQuerySystemInformation(0x32, 4 B)` (SystemRangeStart) · `NtQuerySystemInformation(0 → 0x3e, 0x2c B)` 결과 (이미 통과함 `[관찰]`) · 이어서 NLS `NtGetNlsSectionPtr` (`_SetupMainNlsFiles@4 +0x3b/+0x41`) 와 `NtInitializeNlsFiles` (`RtlGetLocaleFileMappingAddress` 쪽, `+0x76/+0x7c`) · 콘솔 (`ConsoleCreateConnectionObject` 등) · `NtQueryInformationProcess(-1, 0x1a)` (`PcwDllMain`). 위 CSR 이 풀리면 NLS 에서 다시 막힐 수 있다 `[추측]`.

## 정적으로 못 닫은 줄

- `[공백]` 커널의 legacy `NtConnectPort` → ALPC 변환 (블록이 메시지 `+0x28` 부터가 되는 줄) — csrsrv 쪽 오프셋으로 역산만 함 `[추측: 맞물림은 확인됨]`.
- `[공백]` `_NlsProcessInitialize@0` 아래 (`_AllocTables@0` · `_SetupMainNlsFiles@4` · `_MakeLocHashNode@4`) 의 32 비트 syscall 과 wow64 NLS 썽크 (`whNtGetNlsSectionPtr` · `whNtInitializeNlsFiles`). F3 의 안쪽 상수 반환 줄은 안 엶.
- `[공백]` 32 비트 `_ConsoleInitialize@0` 아래 `ConsoleCreateConnectionObject` 의 syscall (콘솔 연결 길). C2 의 `0xc0000022` 갈래 안쪽.
- `[공백]` `Wow64pIsLegacyLpcPort` 의 이름 글, `RedirectPath` 의 `0xc0000272` 뜻.
- `[공백]` `ARI::Globals::Initialize` (E2) 안쪽 음수 가지 (패키지 앱만 탐 `[추측]`).
- `[추측]` seq 648 `NtOpenSection` 이 `0xc000003a` 가 된 까닭은 에뮬레이터가 공유 섹션 이름을 객체 이름공간에 안 갖고 있어서일 것 (팀 코드 쪽, 이번엔 안 봄).
- `[추측]` `NtQuerySystemInformation(0x32)` 의 wow64 처리 (`whNtQuerySystemInformation_SystemRangeStartInformation`, rva `0x188dc`) 는 안 읽음.

## VM 으로 확인할 것 — 디버거 필요

멈춤점은 "x86 시험 exe 일 때만" 조건 (커널 멈춤점 규칙). 32 비트 모듈은 WoW64 컨텍스트 (`.effmach x86`) 에서 `KERNELBASE!__KernelBaseBaseDllInitialize@12` 처럼 `@12` 이름이 나올 것 `[추측: 심볼 표기]`.

| 멈춤점 | 찍을 값 | 닫는 질문 | 얹을 곳 |
|---|---|---|---|
| `ntdll!CsrpConnectToServer+0x1bb` (64 비트 ntdll, NtOpenSection ret) | `r eax` · `dq @rsp+0x50 L1` (핸들) · 이름 `du poi(ntdll+0x1d4e08)` | 1 번 syscall 이 진짜로 `0` 인가 (VM 정상 값) | Q84 (WoW64 프로세스 판) · Q253d |
| `ntdll!CsrpConnectToServer+0x2e4` (NtConnectPort ret) | `r eax` · `dq @rbp-8 L6` (응답 블록: C · T · PID · 상태) · `dq @rbp-0x68 L2` (ViewBase · ViewRemoteBase) · `dq ntdll+0x1d4e20 L1` (CsrPortHandle) | 블록 응답 값 · 포트 뷰 주소 (**4 GB 아래인가**) | Q84 · Q262 |
| `ntdll!CsrpConnectToServer+0x33d` (NtMapViewOfSection ret) | `r eax` · `dq @rsp+0x70 L1` (**R**) · `dq @rbp-0x40 L1` (크기) | **R 이 4 GB 아래인가** · R vs C | Q262 (`MiComputeProcessUserVa+0xaf` 와 같이) · Q84 |
| `wow64!whNtWow64CsrClientConnectToServer+0x95` (PEB 옮긴 뒤) | `dq @$peb+0x88 L1` · `dq @$peb+0x98 L1` · `dq @$peb+0x380 L1` (PEB64) 과 PEB32 의 `dd +0x4c L1` · `dd +0x54 L1` · `dq +0x248 L1` (PEB32 = `dwo(@$teb + dwo(@$teb+0x180c) + 0x30)`) | PEB32 세 칸이 연결 전에 0 이었다가 이 줄에서 채워지는가 | Q253d (PEB32 처음 값) |
| `KERNELBASE!__KernelBaseBaseDllInitialize@12+0x20c` (32 비트, 연결 ret) | `r eax` · 직전 `+0x206` 에서 `dd esp L5` (인자 5개) · `db poi(esp) L0x40` (디렉터리 이름 UTF-16) · `dd poi(esp+8) L2` (블록 `{CtrlRoutine, 0}`) | 32 비트가 넘기는 인자 · 상태 | Q253d · Q84 |
| `KERNELBASE!__KernelBaseBaseDllInitialize@12+0x248` (BSSD 저장 직전) | `r edi` (BSSD) · `db edi L0x40` · `dd edi+0x9e8 L2` · `dd poi(@$peb32+0x54) L4` (표) | 32 비트가 만든 BSSD 지역 주소 · `+0x9e8` · 표 `T[1]` | Q84 · Q253d |
| `KERNELBASE!__KernelBaseBaseDllInitialize@12+0x3ba` · `+0x3f0` · `entry+0x1b` | `r al` | F3 · F5 · 안쪽 최종 결과가 VM 에서 모두 1 인가 | Q84 |
| `KERNELBASE!_PcwDllMain@12+0x28` (NtQueryInformationProcess ret) | `r eax` · `dd ebp-4 L1` | E3 의 `0x1a` 결과 (WoW64 정보 포인터) | Q253d |

## VM 으로 확인할 것 — 디버거 없이 떠 올 수 있는 것

**없음.** 이번 질문의 값 (뷰 주소 · csrss PID · 응답 블록 · 핸들 숫자) 은 모두 실행 중 값이다. (참고만: `\Sessions\1\Windows` 객체 폴더에 `SharedSection` (Section) · `ApiPort` (Port) 가 있는지는 WinObj 같은 도구로 볼 수 있지만, 이 요청의 질문에는 필요 없다.)

## 다음 요청 후보

1. **32 비트 NLS 준비** — `_NlsProcessInitialize@0` 아래 (`_AllocTables@0` · `_SetupMainNlsFiles@4` · `_SetupAnsiOemCodeHashNodes@0` · `_MakeLocHashNode@4`) 의 syscall 과 wow64 `whNtGetNlsSectionPtr` · `whNtInitializeNlsFiles` 본문 (F3, CSR 이 풀리면 바로 다음 막힘 후보). Q261 · Q266 몫과 닿음.
2. **32 비트 콘솔** — `_ConsoleInitialize@0` 아래 (`ConsoleCreateConnectionObject` · `ConsoleAllocate` · `ConsoleCommitState`) 의 syscall 과 wow64 썽크 (`whNtCreateFile` · `whNtDeviceIoControlFile`) (F5).
3. **wow64 `whNtQueryInformationProcess`** (이미 뽑아 둔 본문) 의 `0x1a` · `0x1c` 갈래 + `PcwDllMain` (E3) 와 `NtQuerySystemInformation(0x32)` 썽크.
4. **32 비트 kernel32 `_BaseDllInitialize` · `_Kernel32LegacyLib_BaseDllInitialize`** — BSSD 를 한 번 더 읽는 줄 (`+0x170` IniFileMapping 옮김, `BaseCSDVersion`, `+0x7f8` ) 의 32 비트 폭. kernelbase32 가 풀린 뒤 이어서 읽는 DLL.
5. **32 비트 user32 의 csrss 연결** — `_UserClientDllInitialize` 가 `CsrClientConnectToServer(…, 3, 0x248 B)` 로 가는 길이 WoW64 에선 `NtWow64CsrClientConnectToServer` · `whNtWow64CsrClientCallServer` · `whUserConnectToServer` (wow64con) 중 무엇을 타고, `0x248` B 블록이 32 비트 모양으로 바뀌는지 (run85 · run142 와 짝).
6. (낮음) wow64 `RedirectPath` 의 `0xc0000272` 이름 뜻 · `Wow64pIsLegacyLpcPort` 이름 글.

## 판정

**분석성공.** 세 질문 모두 `.asm` 줄로 닫았다. 가장 큰 발견은 요청서·원문의 전제 ("32 비트 ntdll `CsrpConnectToServer` 가 syscall 여섯") 가 **틀렸다는 것**: 32 비트는 syscall 하나, 나머지는 wow64 가 부르는 64 비트 ntdll 이다. 정적으로 못 닫은 것은 커널 쪽 legacy 포트 변환과 32 비트 NLS·콘솔 안쪽 (위 "못 닫은 줄", 다음 요청 후보).

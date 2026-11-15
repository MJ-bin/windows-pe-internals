# 104 params-block — 정적 분석 결과

- 요청서: `_workspace/sast/104_params-block.md` (Q63a, 커널 쪽, 정적만. ntdll (Q63b) 는 안 함)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`, 기준과 같음)
- analyze: 다시 안 함 (`.gpr` + `_index.tsv` + `ntoskrnl.analyze.log` 의 `SetPdb:` 줄 다 있음).
- 새로 뽑은 함수: 3개, 전부 depth 0
  - `^MiCfgInitializeProcess$` (요청서 대상) → `ms/ghidra/ntoskrnl.export.sast104a.stdout` (exit 0)
  - `^(MiReferenceCfgVad|MiMapSecurePureReserveView)$` (질문 2 의 쓰기가 한 단계 아래에서 일어남 → 지도 `_map/NtCreateUserProcess_edges.tsv` 에서 둘을 찾아 이 둘만 뽑음) → `…export.sast104b.stdout` (exit 0)
  - 위치: `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/` (`_calls.tsv` 에 줄이 더해짐)
- 이미 있던 `.asm`·`.c` 를 그대로 읽음 (다시 안 뽑음): `PspCaptureUserProcessParameters`, `PspCaptureProcessParameters`, `PspCopyAndFixupParameters`, `PspSetupUserProcessAddressSpace`, `PspBuildCreateProcessContext`, `PspLocateNtdllAddressesForScpCfg`, `PsInitializeScpCfgPages`, `PspCopyNtdllExport`, `MmGetScpCfgFunctionOffset`, `PspGetScpCfgFunctions`, `PspPrepareSystemDllInitBlock`, `PspCaptureCreateInfo`, `MmInitializeProcessAddressSpace`, `MiMapProcessExecutable`, `PsLocateSystemDlls`, `PspInitializeThunkContext`.
- 전수 검색 (질문 4): 스크래치 `objdump -d` 풀이 (`…/scratchpad/nt.dis`, 3,086,164줄, 커밋 안 함) 에 정규식과 작은 파이썬 (`…/scratchpad/s104/scan31.py`, `fnmap.py`) 으로 훑었다. ntoskrnl 만 요청 범위. securekernel·ntdll 은 같은 모양만 한 번 훑은 곁눈질 (아래 4번).
- 버전 확인: `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess` **일치**, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines` **일치** (`_calls.tsv`). 커널 PDB GUID 도 기준과 같다. → 스크립트 오프셋과 바이너리 둘 다 믿어도 된다.
- 스크립트 문제: 없음.

## 한눈에

| # | 한 줄 답 |
|---|---|
| 1 (a) 마스크 | `0xf4010e11` 은 **"사용자가 줄 수 없는 비트" 목록**이다 (검사 = `(Flags ^ 1) & 마스크 ≠ 0 → 실패`). 비트 0 만 **켜져 있어야** 하고, 나머지 4·9·10·11·16·26·28·29·30·31 은 **꺼져 있어야** 한다. **PDB 에 비트 이름은 없다.** 커널이 직접 켜는 건 29 (+ 마스크 밖의 14·15). 나머지는 커널이 읽는 줄도 없어 뜻은 ntdll 쪽 읽는 줄 (Q63b) 이 있어야 안다. |
| 1 (a) Environment | **뒤에 붙인다. 따로 할당하지 않는다.** 한 덩어리 `ExAllocatePool2(0x41, F + EnvironmentSize, 'PsPb')`. 환경은 `블록 + F` (F = `MaximumLength` = `Length`) 에서 시작해 `EnvironmentSize` 바이트. |
| 2 (b) `+0x4a0`/`+0x4a8` | `MiCfgInitializeProcess` 가 **직접 쓰지는 않는다 (X)**. **부르는 `MiReferenceCfgVad` 가 쓴다 (O, 간접).** CFG 가 안 켜진 프로세스면 `+0x1e` 에서 0 을 돌려주고 끝 (아무것도 안 씀). |
| 2 (b) ScpCfg | 이름 문자열은 **딱 하나**: `"RtlpScpCfgNtdllExports"` (커널 INIT 구역 rva `0xc7a0c0`). ntdll 의 데이터 표 (ntdll rva `0x1746f0`, 0x68바이트) 를 통째로 복사한다. 전역 칸 `PspNtdllScpFunctions` 넷에는 **함수 이름이 아니라 "ntdll 끝 뒤 쪽" 안의 주소** 가 들어간다. |
| 3 (c) `+0x11` | 이 함수는 `+0x11` 을 **두 곳**에서 쓴다. ① `+0x309` 비트 2~3 = 표준 핸들 상태 (0 안 줌 · 1 요청하면 줌 · 2 늘 줌 · 3 은 거부), ② `+0x1760` 비트 7 = 속성 `PsAttributeChpe` 값이 0 이 아님. |
| 4 (e) 비트 31 | **켜는 줄을 못 찾았다.** 입력으로도 못 준다 (마스크가 막음). ntoskrnl 전체에서 `[x+0x8]` 에 `bts …,0x1f` / `or …,0x80000000` 을 넣는 모양은 **무관한 3곳** 뿐. → 이 흐름에서 비트 31 은 꺼진 채다. |
| 5 (f) | 고치는 칸은 **12개** (두 갈래 모두 같다). `PackageDependencyData (+0x400)`·`CurrentDirectores[]` 는 **목록에 없고, 복사 때 사용자 값이 그대로** 간다. 이유는 코드로 못 정함 `[추측]`. `PspSystemDlls[0]+0x20` = `PspNativeSystemDllData+0x20` **같은 칸**. |

## 질문별 답 [정적]

### 1 (a) `PspCaptureUserProcessParameters` (rva `0x9f1110`, 2071B)

근거 파일: `ms/disassemble/ntoskrnl/PspCaptureUserProcessParameters.asm` / `.c`, `PspSetupUserProcessAddressSpace.asm`, `PspCopyAndFixupParameters.asm`.

**읽는 법 (헷갈리기 쉬운 점)**: 마스크 `0xf4010e11` 은 "받아 주는 비트" 가 아니라 **"켜져 있으면 안 되는 비트"** 다.
- `+0x8f MOV EDI,[RDX+8]` (사용자 블록 `Flags`) → `+0x92 BTR EDI,0xe` (**비트 14 를 먼저 지움**) → `+0xa0 XOR EAX,1` → `+0xa3 TEST EAX,0xf4010e11` → `+0xa8 JNZ` → `+0xf3 MOV EAX,0xc000000d` (`STATUS_INVALID_PARAMETER`).
- 뜻: 비트 0 은 `^1` 때문에 **켜져 있어야** 통과, 마스크의 다른 비트는 **꺼져 있어야** 통과.
- 둘째 검사 `+0xaa ~ +0xba`: `(Flags & 0xff000060) - 1) & Flags & 0x1000060 ≠ 0 → 실패` (`+0xba JNZ` → `+0xfd`). 풀어 쓰면 **비트 5 (0x20) · 6 (0x40) · 24 (0x1000000) 중 둘 이상 켜면 실패.** (5·6·24 는 마스크 밖이라 사용자가 줄 수 있지만 서로 배타.)
- 그러니 사용자가 줄 수 있는 비트 = 0 (필수) + 1·2·3·5·6·7·8·12·13·15·17~25·27 (이 중 5·6·24 는 하나만). 14 는 줘도 지워진다.

| 비트 | 값 | 입력 검사 | 커널이 읽거나 쓰는 줄 | 이름/뜻 |
|---|---|---|---|---|
| 0 | 0x1 | **켜져야 함** (`+0xa0`) | 복사본에 그대로 (`+0x40e MOV [RDI+8],EAX`) | "정규화됨" (run32). PDB 이름 없음. 공개 헤더 이름 `RTL_USER_PROC_PARAMS_NORMALIZED` 기억 `[추측]` (ntdll 에 `RtlNormalizeProcessParams` 가 있는 것과 맞음) |
| 4 | 0x10 | 꺼져야 | 본 커널 함수 안에 읽는 줄 없음 | `[공백]` |
| 9 · 10 · 11 | 0x200 · 0x400 · 0x800 | 꺼져야 | 없음 | `[공백]` |
| 16 | 0x10000 | 꺼져야 | 없음 | `[공백]` |
| 26 | 0x4000000 | 꺼져야 | 없음 | `[공백]` |
| 28 | 0x10000000 | 꺼져야 | 없음 | `[공백]` |
| **29** | 0x20000000 | 꺼져야 | **커널이 켬**: `PspSetupUserProcessAddressSpace +0xe6 BTS EBX,0x1d` → `+0xea MOV [RBP+8],EBX`. 조건 `+0xd0 ~ +0xe4` = `PsGetProcessServerSilo` → `PsGetServerSiloGlobals` → `[+0x3f8] ≠ 0` (PDB `_ESERVERSILO_GLOBALS.OneCoreForwardersEnabled`) | 서버 silo 의 "OneCore forwarders 켜짐" 을 옮겨 적은 비트 `[추측: 이름]` (run32, 이미 알던 것) |
| 30 | 0x40000000 | 꺼져야 | 없음 | `[공백]` |
| **31** | 0x80000000 | 꺼져야 | 켜는 줄 없음 (질문 4) | ntdll 이 `LdrpIsSecureProcess` (ntdll PDB 변수, rva `0x1d28e0`) 로 옮겨 담는 맨 윗 비트 (run17). "보안 프로세스" 표시 `[추측]` |
| (참고) 5 · 6 · 24 | 0x20 · 0x40 · 0x1000000 | 마스크 밖, 둘 이상 켜면 실패 | `PspSetupUserProcessAddressSpace +0xa6 AND R14D,0x1000060` → `+0x15c TEST R14D,R14D` · `+0x15f JNZ` — 셋 다 꺼져 있고 필요한 크기가 `0x20000` 보다 작으면 `+0x161 ~ +0x16b` 에서 `0x20000` 으로 올림 | 공개 헤더 이름 기억 `RESERVE_1MB` (0x20)·`RESERVE_16MB` (0x40) `[추측]` — 서로 배타인 것과 맞음. 0x1000000 은 `[공백]` |
| (참고) 14 | 0x4000 | 입력에서 **지움** (`+0x92`) | `PspCopyAndFixupParameters +0x82 BTS [RBX+8],0xe` (조건 `+0x74 [R8+0x10] & 0x40` 켜짐 · `+0x7b [R8+0x11] & 1` 꺼짐) | run32·run50 이 이미 정함 ("키 없는 IFEO 길", 공개 이름 `IMAGE_KEY_MISSING` 기억과 맞음 `[추측]`) |
| (참고) 15 | 0x8000 | 마스크 밖 | `PspSetupUserProcessAddressSpace +0xb1 BTS EBX,0xf` (조건 `+0xa0 [PspGlobalFlags] & 1`) | `[공백]` |

- **PDB 확인**: `_RTL_USER_PROCESS_PARAMETERS.Flags` 는 그냥 `unsigned long`. 타입 덤프에서 `NORMALIZED`·`USER_PROC_` 같은 열거형 이름을 못 찾음. 그래서 위 이름들은 코드가 말해 주는 만큼만 확정이고 나머지는 `[공백]`.
- **4·9·10·11·16·26·28·30 의 뜻**: 커널은 읽지 않고 **막기만** 한다. 읽는 쪽은 ntdll (`RtlNormalizeProcessParams`, `LdrpInitializeProcess` …) → **Q63b 가 있어야 이름을 얻는다.** 억지로 추측하지 않는다.

**Environment 를 어디에 두나** (모두 `PspCaptureUserProcessParameters.asm`)

| 줄 | 하는 일 |
|---|---|
| `+0xbc MOV RCX,[RDX+0x80]` · `+0xc8 MOV RAX,[RDX+0x3f0]` | 사용자의 `Environment` 포인터와 `EnvironmentSize` 를 읽음. `+0xd4 ~ +0xdb`: 크기가 0 이거나 홀수면 `+0x114` 로 실패. `+0xdd ~ +0xeb`: 끝 주소가 `0x7fffffff0000` 을 넘거나 한 바퀴 돌면 `+0x107` 에서 그 주소를 일부러 읽어 접근 위반을 냄 |
| `+0x26c ~ +0x2d4` | `F = (0x450 + 1 + 문자열 아홉 개의 MaximumLength 합 + 현재 폴더 칸) & ~1` (`+0x2ca ADD R13,0x451` · `+0x2d4 AND R13,-0x2`). 현재 폴더 칸은 이 길에서 `0x208` (`+0x625 MOV EAX,0x208` · `+0x635`) |
| `+0x7d6 ~ +0x7f5` | 사용자 속성 목록이 있으면 (`PS_CREATE_INFO` 비트 0) `F = ((F+3) & ~3) + 개수×8` |
| `+0x2e6 ~ +0x317` (ret `+0x31c`) | **`ExAllocatePool2(0x41, F + EnvironmentSize, 0x62507350)`** — 한 번만 할당. 꼬리표 `0x62507350` = `PsPb` |
| `+0x335 ~ +0x3a1` | 사용자 블록 앞 `0x450` 바이트를 통째로 복사 (`0x80`×8 + `0x50`) |
| `+0x40e` · `+0x411` · `+0x415` | `[RDI+8]` = `Flags` (비트 14 뺀 값), `[RDI+4]` = `[RDI]` = F (`Length` = `MaximumLength`) |
| `+0x418` | 문자열 놓을 커서 = `블록 + 0x450` (`[RSP+0x20]`) |
| `+0x43b ~ +0x528` | 문자열을 커서 자리에 차례로 `PspCopyUnicodeString` (ret `+0x440 +0x45d +0x47a +0x49b +0x4bf +0x4e3 +0x507 +0x528`): `+0x38`, `+0x50`, `+0x60`, `+0x70`, `+0xb0`, `+0xc0`, `+0xd0`, `+0x410`. 이어서 (`PS_CREATE_INFO` 비트 2 면) `+0x420` (ret `+0x849`), `RuntimeData` 는 `+0xe0` 머리 `+0x53b` + 본문 `+0x683 [RDI+0xe8]` |
| `+0x55e` 이후 (조각 `+0x13bcd0 ~ +0x13bd24`) | 속성 목록: 4바이트 올림 후 `[RDI+0x430]` = 위치, `[RDI+0x438]` = 개수, 복사 |
| `+0x567 ~ +0x570` (`+0x778 ~ +0x785`) | 커서가 `블록 + F` 에 모자라면 모자란 만큼 `memset 0` 로 채워 커서를 `블록 + F` 로 맞춤 |
| **`+0x576 ~ +0x583`** (ret `+0x588`) | **`RtlCopyMemory(커서 = 블록+F, 사용자 환경, EnvironmentSize)`** |
| `+0x5b3 ~ +0x5bb` (ret `+0x5c0`) | `PspValidateEnvironmentBlock` (`param_4 & 3 == 1` 이면 건너뜀) — `EnvironmentSize` 를 줄일 수 있음 |
| **`+0x5e1`** · `+0x5ed` · `+0x5f4` | **`[RDI+0x80]` (Environment) = 커서 = `블록 + F`**, `[RDI+0x3f0]` = 크기, `[RDI+0x3f8]` = 0 |

- 사용자 쪽에도 같다: `PspSetupUserProcessAddressSpace +0x115 ~ +0x11f` 가 `[RBP] + [RBP+0x3f0]` (= F + 환경 크기) 만큼 자리를 잡고, `PspCopyAndFixupParameters +0x36 ~ +0x39` 가 같은 값만큼 복사한다 (run32 와 같다).
- `param_4 & 1` (= 만들기 정보 `+0x10` 비트 3, `PspCaptureProcessParameters +0x1f ~ +0x31`) 이면 현재 폴더·`RuntimeData`·`DllPath`… 를 사용자 블록 대신 서버 silo 값에서 가져오는 갈래 (`+0x135 ~ +0x185`). 환경은 그래도 사용자 것을 복사한다 (이 갈래 이름은 모름 `[공백]`).
- **지금 우리 (PE_emulator)**: **맞음.** `Process.cpp` 의 부모 블록은 `Flags = 1` (마스크를 통과하는 유일한 값), 환경을 `base + dwLength` (= `블록 + F`) 에 놓고 `+0x80` 에 그 주소를 넣는다. 현재 폴더 칸 `0x208`·문자열 차례도 같다. 비트 14 는 `PspCopyAndFixupParameters` 줄임에서 켜서 사진 `0x4001` 과 같다 (주석에 인용된 run44, 원본 사진은 이번에 안 봄). 비트 15·29 는 안 켜는데 사진도 꺼짐이라 맞다. `PspCaptureUserProcessParameters` 자체는 "커널 모드 갈래" 로 줄였고 `Flags 1` 이 늘 통과라 문제없음.

### 2 (b) `MiCfgInitializeProcess` (rva `0x90b634`, 454B) 와 `PspLocateNtdllAddressesForScpCfg` (rva `0xc44708`, 565B)

근거 파일: `ms/disassemble/ntoskrnl/MiCfgInitializeProcess.asm`·`MiReferenceCfgVad.asm`·`MiMapSecurePureReserveView.asm`, `PspLocateNtdllAddressesForScpCfg.asm`·`PsInitializeScpCfgPages.asm`·`PspCopyNtdllExport.asm`·`MmGetScpCfgFunctionOffset.asm`·`PspGetScpCfgFunctions.asm`.

**(b-1) `+0x4a0` / `+0x4a8` 를 쓰나**
- **직접: X.** `MiCfgInitializeProcess` 안에서 메모리에 쓰는 줄은 지역 변수 `[RBP+0x30]`·`[RBP+0x38]`·`[RSP+0x20]` 뿐이다. `RDI` (= 새 프로세스 `EPROCESS`) 는 읽기만 하고 (`+0x13 [RCX+0x750]` · `+0x3e [RDI+0x368]` · `+0xa7 [RDI+0x6ec]`), 작업 집합 구조 (`EPROCESS+0x410`) 는 이 함수에서 한 번도 안 건드린다.
- **간접: O.** 이 함수가 `MiReferenceCfgVad(현재 스레드, 비트맵 주소, 번호)` 를 두 번 부른다 (`+0x98` ret `+0x9d` 번호 0 · `+0x131` ret `+0x136` 번호 1). 그 안에서:

| `MiReferenceCfgVad` 줄 (rva `0xaaf2e8`, 186B) | 하는 일 |
|---|---|
| `+0x16 MOV R12,[RCX+0xb8]` | 스레드의 프로세스 (= 지금 붙어 있는 새 프로세스. `MmInitializeProcessAddressSpace +0xb0 KiStackAttachProcess` 뒤 `+0x1c9/+0x1ce MiMapProcessExecutable` 안이라서) |
| `+0x33 LEA R15,[R14+0x25]` · `+0x37 SHL R15,5` · `+0x3b ADD R15,[R12+0x410]` | **칸 = `VmWorkingSetList + (번호 + 0x25) × 0x20`.** 번호 0 → **`+0x4a0`**, 번호 1 → `+0x4c0` |
| **`+0x71 MOV [R15],RBP`** | 칸 `+0x0` = 비트맵 시작 주소 → **`+0x4a0`** |
| `+0x43/+0x48 MiObtainReferencedVadEx` · `+0x5f/+0x64 MiUnlockVadShared` · `+0x4e ~ +0x7b` | 그 VAD 의 끝 페이지를 읽어 크기를 셈: `크기 = ((EndingVpn << 12) \| 0xfff) − 시작 + 1` |
| **`+0x82 MOV [R15+8],RBX`** | 칸 `+0x8` = 크기 → **`+0x4a8`** |
| `+0x77 MOV [R15+0x10],RDI` · `+0x7e MOV [R15+0x18],EAX` | `+0x10` = VAD 포인터, `+0x18` = 0 (번호 1 이고 `Machine == 0x1c4` 면 1, `+0xa4 ~ +0xb5`) |

- 그래서 **`PspPrepareSystemDllInitBlock +0x232/+0x239` 가 읽는 `[VmWorkingSetList+0x4a0]` = CfgBitMap 시작, `+0x4a8` = 그 크기** 가 시작 정보 블록 `+0xb8`/`+0xc0` 으로 간다 (run32 와 이어짐). 이름은 PDB 에 없지만 (`_MMWSL_INSTANCE` 는 0x100 바이트뿐이라 `+0x4a0` 은 그 밖), **같은 일을 하는 PDB 이름이 근처에 있다**: `MiMapSecurePureReserveView` 에 들어가는 두 전역이 PDB 로 `MiState.Sections.CfgBitMapSection` (`[0x140e2d840]`, `_MI_SECTION_STATE +0xb00`) 과 `MiState.Processes.NativeCfgBitmapPreferredAddress` (`[0x140e2dbc8]`, `_MI_PROCESS_STATE +0x48`) 이다.
- **언제 서나**: `+0x13 MOV EAX,[RCX+0x750]` · `+0x1c TEST AL,1` · `+0x1e JZ +0x179` → **`EPROCESS.MitigationFlags` 비트 0 (PDB `MitigationFlagsValues.ControlFlowGuardEnabled`) 이 꺼져 있으면 아무것도 안 쓰고 0 을 돌려준다.** 켜져 있으면: `+0x76/+0x7b MiMapSecurePureReserveView(프로세스, CfgBitMapSection, &기준주소=NativeCfgBitmapPreferredAddress, &크기, 0)` → 실패하고 기준주소가 0 이 아니면 `+0x156/+0x15b` 에서 기준주소 0 으로 한 번 더 → `+0x98/+0x9d MiReferenceCfgVad(…, 0)`. WoW64 프로세스 (`+0x9d CMP [RSI],0`, `RSI = RDI+0x310`) 이고 `Machine` (`+0xa7 [RDI+0x6ec]`) 이 `0x14c`/`0x1c4` 면 (`+0xb3 ~ +0xb6`, `+0x1a6`) `MiWowSubsystems6432` 표 (`+0xcf ~ +0xe2`) 에서 구역을 골라 한 번 더 `MiMapSecurePureReserveView` (`+0x119/+0x11e`) → `MiReferenceCfgVad(…, 1)` (`+0x131/+0x136`). 재시도 갈래는 `+0x138 ~ +0x15b`.
- `MmInitializeProcessAddressSpace` 안에도 `MiReferenceCfgVad` 를 직접 부르는 자리가 둘 더 있다 (`+0x4a5/+0x4aa` 번호 1 · `+0x4ba/+0x4bf` 번호 0). 어느 길인지는 안 봤다 `[공백]`.
- **지금 우리 (PE_emulator)**: **맞음 (test.exe 한정).** `Process.cpp` 가 `+0x4a0`·`+0x4a8` 을 0 으로 두고 `MiCfgInitializeProcess` 를 안 만든다. 위 `+0x1e` 갈래의 결과와 같다. 근거인 시작 정보 블록 `+0xb8`·`+0xc0` = 0 은 run44/run60 사진이라고 주석·run60 해설서에 있다 (`MitigationFlags` 비트 0 을 직접 본 값은 아님 → 아래 동적 3번).

**(b-2) `PspLocateNtdllAddressesForScpCfg` 가 넣는 것**
- 이름 문자열 = **하나뿐**: `+0x83 LEA R8,[0x140c7a0c0]` → 커널 `INIT` 구역에 있는 글 **`RtlpScpCfgNtdllExports`** (`objdump -s` 로 읽음). `+0x77 MOV [RSP+0x20],0x68` · `+0x8d CALL PspCopyNtdllExport` (ret `+0x92`) 가 ntdll 의 그 이름 (내보내기 표) 으로 가서 **0x68 바이트를 `param_4` 에 복사**한다 (`PspCopyNtdllExport +0x2a ~ +0x70`: `PspLookupEntryPoint` → 범위 검사 → `RtlCopyMemory`). 함수 이름 글은 이 함수에 없다. 함수 이름은 ntdll PDB 로만 안다.
- 복사된 표 (ntdll 파일 값 `0x1801746f0 ~`, 이름은 ntdll PDB):

| 칸 | 값 | ntdll PDB 이름 |
|---|---|---|
| `+0x00/+0x08` 쌍 0 | `0x18016e000 ~ 0x18016e2b0` | `ScpCfgHeader_Nop` ~ `ScpCfgEnd_Nop` (쪽 종류 `RtlScpCfgPageTypeNop` = 0) |
| `+0x10/+0x18` 쌍 1 | `0x18016c000 ~ 0x18016c2b0` | `ScpCfgHeader` ~ `ScpCfgEnd` (`…Default` = 1) |
| `+0x20/+0x28` 쌍 2 | `0x18016f000 ~ 0x18016f2b0` | `…_ES` (`…ExportSuppression` = 2) |
| `+0x30/+0x38` 쌍 3 | `0x18016d000 ~ 0x18016d2b0` | `…_Fptr` (`…Fptr` = 3) |
| `+0x40` | `0x1801e7428` (8B) | `LdrpGuardDispatchIcallNoESFptr` |
| `+0x48` | `0x1801e6000` (8B) | `__guard_dispatch_icall_fptr` |
| `+0x50` | `0x1801ea440` (8B) | `LdrpGuardCheckIcallNoESFptr` |
| `+0x58` | `0x1801e7000` (8B) | `__guard_check_icall_fptr` |
| `+0x60` | `0x180124420` (1B) | `LdrpHandleInvalidUserCallTarget` (함수) |

  쪽 하나 안 배치 (ntdll PDB): `+0x0` 머리 · `+0x40 ScpCfgDispatchUserCallTarget` · `+0xc0 …DispatchUserCallTargetES` · `+0x140 ScpCfgValidateUserCallTarget` · `+0x1c0 …ValidateUserCallTargetES` · `+0x240 …HandleInvalidCallTarget` · `+0x280 …ICallHandler` · `+0x2b0 …End`. `PsInitializeScpCfgPages +0xed ~ +0x104` 이 쪽 머리 4바이트 넷 (`0x40`·`0xc0`·`0x140`·`0x1c0`) 을 검사하는 것과 맞다.
- 이 함수가 하는 검사·계산: 쌍 4개 (`+0xa5 ~ +0x11b`): 길이 ≤ `0x1000` · ntdll 이미지 안 (`+0xbc ~ +0xe4`) → 아니면 `+0xf0 0xc000000d`. 통과하면 `+0x101 ~ +0x114` 로 `주소 − [RSI+0x18] + [RSI+0x20]` 으로 바꿔 둔다 (`[RSI]` = `PsQuerySystemDllInfo` 가 준 `PspNativeSystemDllData`). 나머지 다섯 칸 (`+0x40 ~ +0x60`) 은 **이미지 안에 있는지만 본다** (`+0x11d ~ +0x1a3`, 크기 8·8·8·8·1). 이 값들이 어디에 쓰이는지는 `PsInitializeScpCfgPages +0x118 ~ +0x152 PspFinalizeScpCfgPage` 쪽 `[공백]`.
- **전역 칸에 넣는 것** (`MmGetScpCfgFunctionOffset(ECX = 쪽 안 위치, EDX = SizeOfImage)` = `(SizeOfImage 쪽 올림) + [0x140e2d8e0] + 쪽 안 위치`, `+0x1ac ~ +0x202`, 기준 `R9 = [RSI+0x18]`):

| 전역 칸 (rva) | 줄 | 쪽 안 위치 | 가리키는 함수 (ntdll PDB 배치) |
|---|---|---|---|
| `PspNtdllScpFunctions+0x0` (`0xfc7640`) | `+0x1af/+0x1b4`, 저장 `+0x1bf` | `0x140` | `ScpCfgValidateUserCallTarget` 자리 |
| `+0x8` (`0xfc7648`) | `+0x1c6/+0x1cb`, 저장 `+0x1d6` | `0x1c0` | `…ValidateUserCallTargetES` 자리 |
| `+0x10` (`0xfc7650`) | `+0x1dd/+0x1e2`, 저장 `+0x1ec` | `0x40` | `ScpCfgDispatchUserCallTarget` 자리 |
| `+0x18` (`0xfc7658`) | `+0x1f3/+0x1f8`, 저장 `+0x202` | `0xc0` | `…DispatchUserCallTargetES` 자리 |

  `[0x140e2d8e0]` = PDB `MiState.Sections.ImageViewExtensionOffsets[0]` (`_MI_SECTION_STATE +0xba0`). 값이 0 이면 네 주소는 **"ntdll 이미지 끝을 쪽 올림한 곳 = ntdll 바로 뒤 첫 쪽" + 위치** 다. 그러니 네 칸은 **ntdll 코드 주소가 아니라, 커널이 ntdll 뒤에 따로 붙이는 쪽 안의 주소**다 (사진에 "ntdll 끝 뒤 주소" 로 나온 이유와 맞음, `Process.cpp` 주석의 run44). 이 표를 돌려주는 쪽은 `PspGetScpCfgFunctions +0x92 LEA RCX,[PspNtdllScpFunctions]` (`+0x85 PsGetScpCfgPageTypeForProcess` 결과가 3 이나 4 면 NULL).
- 이 함수와 호출자 `PsInitializeScpCfgPages` (rva `0xc44944`) 는 둘 다 **INIT 구역** (`0x140bec000 ~ 0x140c8ec1c`) 이라 **부팅 때 한 번만 돈다.** test.exe 를 돌릴 때는 이미 지나갔으니 멈춤점이 아니라 **결과 칸을 읽어야** 한다.
- **"SCP" 가 무슨 뜻인가** `[추측]`: 코드에 풀이가 없다. 단서만 — 커널 PE 구역 이름 `KSCP`, 함수 `MiMapKernelScp`·`RtlCreateKernelScpFunctionTable`·`RtlCopyKernelScpUnwindInfo`·`MiMapImageScpCfgPages`·`KscpCfgDispatchUserCallTargetEsSmep`. 곧 "커널이 이미지 밖에 따로 만들어 붙이는, CFG 검사·디스패치 코드가 든 작은 코드 쪽" 을 가리키는 이름으로 보인다. 약어 풀이는 모른다.
- **지금 우리 (PE_emulator)**: **틀리지 않음, 줄임.** `SystemDll.cpp` 가 이름 `"RtlpScpCfgNtdllExports"`, 쌍 0 의 검사 (`+0xbc ~ +0xe4`), 환산 (`+0x101/+0x105`) 을 그대로 하고, 쌍 1~3·다섯 칸 검사·전역 넷 채우기 (`+0x11d ~ +0x202`) 는 "공사 중 (D33)" 이라 안 한다. 위 표가 그 빠진 부분의 값 계산이다 (기준 `[RSI+0x18]` 이 어느 값인지는 동적 4번).

### 3 (c) `PspBuildCreateProcessContext` 가 `+0x11` 을 채우는 줄 (rva `0x840920`, 6374B)

근거 파일: `ms/disassemble/ntoskrnl/PspBuildCreateProcessContext.asm`. 이 함수 안에서 `[R12 + 0x11]` (R12 = 만들기 정보, `+0x45 MOV R12,R9`) 에 쓰는 줄은 **두 곳뿐** (`grep` 으로 전수).

속성 번호는 `+0x20a ~ +0x26e` 와 `+0xc9c ~ +0xcf8`·`+0x164d ~ +0x168e` 의 `CMP`/`SUB` 사슬로 갈라지고, 번호의 뜻은 PDB 열거형 `_PS_ATTRIBUTE_NUM` 으로 확인했다 (사슬 속 `0x6 = ImageInfo`, `0x20007 = MemoryReserve`, `0x60011 = ProtectionLevel (17)`, `0x6001c = MachineType (28)`, `0x6001f = SupportedMachines (31)` 이 각자 맞는 일을 하는 것으로 교차 확인).

| 줄 | 속성 | 하는 일 | 값 뜻 |
|---|---|---|---|
| `+0x282 ~ +0x2ff` (저장 **`+0x309 MOV [R12+0x11],DL`**) | `0x2000a` = `PsAttributeStdHandleInfo` (10) | 크기 8 검사 (`+0x282`), 값은 포인터 → 첫 8바이트 읽음 (`+0x2c6`). `+0x2d1 TEST AL,0x1c` → 비트 2~4 가 하나라도 켜지면 `0xc00000bb` (`STATUS_NOT_SUPPORTED`). `+0x2e2 AND R8D,R14D` (R14 = 3) 로 낮은 2비트 = **상태** 를 뽑아 `+0x2ed CMP R8B,R14B` 3 이면 `0xc000000d`. `+0x2f6 ~ +0x309`: **`+0x11 = (+0x11 & 0xf3) \| (상태 << 2)`**. 상태가 1 이면 윗 4바이트를 `[R12+0x120]` 에 저장 (`+0x318 ~ +0x31c`) | **`+0x11` 비트 2~3 = 표준 핸들 상태** (PDB `_PS_STD_HANDLE_STATE`): **0** `PsNeverDuplicate` (안 줌) · **1** `PsRequestDuplicate` (`[R12+0x120]` = 요청한 종류 값, 맞으면 부모 표준 핸들 목록을 가져옴) · **2** `PsAlwaysDuplicate` (사용자 매개변수 안의 표준 핸들 칸 그대로) · 3 (`PsMaxStdHandleStates`) 은 거부. 읽는 쪽 = `PspSetupUserProcessAddressSpace +0x41 ~ +0x71` (run32 와 같음: 1 이면 `+0x92 PspGetStandardHandleList`, 2 면 `RBP+0x20`, 0·3 이면 없음) |
| `+0x1740 ~ +0x1765` (저장 **`+0x1760 MOV [R12+0x11],CL`**) | `0x6001a` = **`PsAttributeChpe` (26)** | 크기 1 검사 (`+0x1740`), 값 자체를 읽어 (`+0x174b MOV RAX,[RDI+0x10]`) `+0x174f NEG RAX · +0x1752 SBB CL,CL · +0x1754 AND CL,0x80`, `+0x1757 ~ +0x1760` 에서 **`+0x11` 비트 7 = (값 ≠ 0)** | **`+0x11` 비트 7 = CHPE (ARM64X 호환 이미지) 속성 켜짐.** 이름은 PDB 열거형 이름, 의미 설명은 모름 `[공백]` |

- 이 함수는 `+0x11` 의 **비트 0·1·4~6 은 안 쓴다.** 비트 0 은 `PspCaptureCreateInfo +0x56 ~ +0x68` 이 (사용자 `PS_CREATE_INFO` 의 `+0x10` 비트 3 에서) 채우고, `PspCaptureProcessParameters +0x63 AND [RBX+0x11],0xf3` 이 비트 2~3 을 0 으로 지우는 갈래도 있다 (만들기 정보 `+0x10` 비트 3 켜짐 = 위 `param_4 & 1` 갈래).
- **지금 우리 (PE_emulator)**: **확인 필요.** `PspBuildCreateProcessContext` 는 비어 있고 (`Process.cpp:26`), `PspGetStandardHandleList` 는 "상태 1 일 때만" 이라 줄여 있다 (`Process.cpp:862`) — 해석은 위와 맞다. 그러나 test.exe 에서 상태 값이 실제로 몇인지는 정적으로 못 정한다 (동적 2번).

### 4 (e) `ProcessParameters.Flags` 비트 31 을 누가 켜나

근거: 위 1 (a) 의 `+0xa3` 마스크 + 아래 전수.

- **입력으로는 못 준다.** `0x80000000` 은 마스크 `0xf4010e11` 안에 있어서 켜서 넘기면 `0xc000000d`. 요청서의 "사용자가 줄 수 있는 비트인데" 는 **반대**다 (위 1 (a) "읽는 법").
- **커널이 `Flags` (`+0x8`) 를 쓰는 곳** (본 함수들): `PspCaptureUserProcessParameters +0x40e` (입력값 − 비트 14), `PspCopyAndFixupParameters +0x82` (비트 14), `PspSetupUserProcessAddressSpace +0xb1` (비트 15)·`+0xe6` (비트 29). 비트 31 은 없다.
- **ntoskrnl 전체 전수** (objdump 풀이, 명령 3,084,940개). 찾은 모양과 결과:

| 모양 | 맞은 줄 | 판정 |
|---|---|---|
| `bts DWORD PTR [reg+0x8],0x1f` | 1곳: `HvlAddPagesCallbackRoutine +0xdb` (`0x14058aa5b`) | 하이퍼바이저 계층 구조체. 매개변수 블록 아님 |
| `or DWORD PTR [reg+0x8],0x80000000` | 0곳 | — |
| `bts/or reg,{0x1f \| 0x80000000}` 뒤 15명령 안에 `mov DWORD PTR [reg+0x8],reg` | 3곳: `SmPageWrite +0x1f6`, `ExBuildPasidMsrForThread +0x2c`, `ExpUpdateDebugInfo +0x148` | 모두 무관 (스토어 키·PASID·디버그 정보) |
| `mov DWORD PTR [reg+0x8],0x8…~0xf…` (즉시값) | 28곳 (이 중 정확히 `0x80000000` 은 1곳: `FsRtlpOplockTryPrepForAckTimeout +0x49`) | 함수 이름으로 전부 확인: 오플록·`Kdp*`·`Hvl*`·`Cmp*` 등의 NTSTATUS 상수 (`0xc000…`, `0x8000000x`), `0xffffffff`, 음수 시간값 `0xfffe7960` (`PsWaitForAllProcesses`). 매개변수 블록 아님 |

  → **이 흐름에서 `Flags` 비트 31 을 켜는 커널 줄은 못 찾았다.** (레지스터로 값을 계산해 넣는 다른 모양은 이 검색으로 못 잡을 수 있어, "전혀 없다" 가 아니라 "이 모양들에는 없다".)
- 곁눈질 (요청 범위 밖, 같은 모양을 한 번 훑음, 읽기만): `securekernel.exe` (PDB 없음) 에서는 1번째·2번째·3번째 모양 모두 0곳 (즉시값 모양 1곳은 `0x80004001`, 무관). 이미 있던 `ntdll` 스크래치 objdump 에서도 `[reg+8]` 에 비트 31 을 넣는 모양 0곳. **누가 켜는지는 이 검색들로 못 찾았다** → 보안 프로세스 (trustlet) 만드는 길이 따로 있을 것이라는 `[추측]`. 일반 `NtCreateUserProcess` 길 (test.exe) 에서는 꺼진 채다.
- **지금 우리 (PE_emulator)**: **맞음.** 비트 31 을 안 켜고, 읽는 쪽 (`LdrInit.cpp:7123`, `LdrpInitialize +0x53/+0x58`) 도 0 으로 본다. 사진 `Flags 0x4001` 과 같다.

### 5 (f) 작은 것

**(f-1) 왜 `PackageDependencyData (+0x400)` · `CurrentDirectores[32] (+0xf0 ~ +0x3ef)` 안쪽 포인터를 안 고치나**
- 근거 `PspCopyAndFixupParameters.asm`: 고치는 칸은 **정확히 12개** 이고 두 갈래가 같다.
  - 커널 사본 먼저 고치고 사용자에 복사하는 갈래 (스위치 켜짐, `+0x1a4 JZ` 가 안 뛰어서 이어짐): `+0x22b ~ +0x314` → `+0x40 +0x58 +0x68 +0x78 +0xb8 +0xc8 +0xd8 +0xe8 +0x418 +0x428 +0x430 +0x80`, 그 뒤 `+0x324 RtlCopyToUser`, `+0x347 RtlWriteULong64ToUser(PEB+0x20)`.
  - 사용자에 먼저 복사하고 거기서 고치는 갈래 (스위치 꺼짐, `+0x416`): `+0x424 ~ +0x50d` 같은 12칸, 끝 `+0x52b MOV [RAX+0x20],RDI`.
  - 12칸 = `CurrentDirectory.DosPath` · `DllPath` · `ImagePathName` · `CommandLine` · `WindowTitle` · `DesktopInfo` · `ShellInfo` · `RuntimeData` 의 `Buffer` + `RedirectionDllName`·`HeapPartitionName` 의 `Buffer` + `DefaultThreadpoolCpuSetMasks` + `Environment`.
  - `+0x400` · `+0xf0 ~ +0x3ef` 는 **어느 갈래 목록에도 없다.**
- 코드가 알려 주는 것: 두 칸은 `PspCaptureUserProcessParameters` 가 앞 `0x450` 바이트를 통째로 복사할 때 (`+0x335 ~ +0x3a1`) **사용자 값 그대로** 들어오고, 그 뒤 쓰거나 검사하는 줄이 없다 (`+0x400` 에 대한 저장 0곳). 곧 값이 0 이면 아무 일 없고, 0 이 아니면 부모 프로세스 안 주소가 그대로 자식에게 간다.
- **왜 안 고치는지는 코드로 정할 수 없다** `[추측]`: 보통 만드는 쪽 (`CreateProcess`) 이 이 칸들을 0 으로 두고, 이름 그대로 패키지·드라이브별 현재 폴더 용이라 보통 exe 길에는 안 쓰는 칸이라서로 보인다. 이 빌드에서 실제 값은 동적 5번.
- 곁다리: `AttributeList (+0x448)` 는 별도 줄 (`+0x3c7 CMP [R15+0x250],R13D` 가 0 보다 클 때만 `+0x3f8 LEA RCX,[RDI+0x448]` → `+0x402 RtlWriteULong64ToUser` (ret `+0x407`)) 이 새 주소로 덮는다.
- **지금 우리 (PE_emulator)**: **맞음.** `_FixupProcessParameters` 의 열두 칸이 위와 같고, 부모 블록이 0 으로 채워져 두 칸은 0 이다 (`vecSource.assign(…, 0)`).

**(f-2) 스위치 이름의 "SegmentHeap"·"SCP"** (이름만, 뜻은 `[추측]`)
- 스위치 = `Feature_CreateProcessSegmentHeapEnablement__private_featureState` (rva `0xfc4110`), `…IsEnabledDeviceUsageNoInline` 함수로 읽는다 (`PspCopyAndFixupParameters +0x19d`, `PspSetupUserProcessAddressSpace +0x124` · `+0x1a6`). `Feature_…` 는 윈도 쪽 "기능 켜고 끄는 단계적 배포 스위치" 이름 모양이다 `[추측]`.
- "SegmentHeap" = 윈도의 새 힙 구현 이름 (옛 NT 힙과 구별) `[추측]`. 단서: 이 스위치가 켜질 때 쓰는 칸들이 블록 끝부분의 `HeapPartitionName (+0x420)` · `HeapMemoryTypeMask (+0x440)` · `DefaultThreadpoolCpuSetMasks (+0x430)` 이고, PDB 속성 열거형에 `PsAttributeHeapFeatures = 34` 가 있다. 곧 "프로세스 만들 때 힙 기능 속성을 받아 블록 뒷칸에 담는 기능" 이라는 이름으로 보이지만 확정 아님.
- "SCP": 위 (b-2) 마지막 항목. 약어 풀이는 모른다 `[공백]`.

**(f-3) `LdrInitializeThunk` 로 가는 `Rdx` 출처: `PspSystemDlls[0]+0x20` 과 `PspNativeSystemDllData+0x20` 은 같은 칸인가**
- **같은 칸이다.** PDB 주소: `PspSystemDlls` = rva `0xfd8130`, `PspNativeSystemDllData` = rva `0xfd8980`.
  - `PspSystemDlls` 는 포인터 6칸 표 (`PsLocateSystemDlls +0x40 CMP EDI,6`). 파일에 든 초기값: **`[0]` = `0x140fd8980` = `PspNativeSystemDllData`**, `[1]` = `0x140fd8940` (둘째 시스템 DLL 칸), `[4]` = `0x140fd89c0`, 나머지 0.
  - 그 표 칸 `[0]` 을 쓰는 줄은 `PsLocateSystemDlls +0x8d AND [RBX],0` 하나 (`PspMapSystemDll` 실패 때 비움, `_refs/PspSystemDlls.tsv`). 평소엔 초기값 그대로.
  - 그러므로 `PspInitializeThunkContext +0x196 MOV RAX,[PspSystemDlls]` · `+0x19d MOV RCX,[RAX+0x20]` = `[PspNativeSystemDllData+0x20]` = rva `0xfd89a0`. 같은 칸을 두 이름으로 부른 것뿐 (`PspGetScpCfgFunctions +0x1b ~ +0x36`, `MiRelocateImage +0x756 ~ +0x760` 도 같은 방식).
- **헷갈리지 말 것**: `_refs/PspSystemDlls.tsv` 의 `PspSystemDlls +0x20` (`PspInitPhase3 +0x89` 등, `MOV RDX,[0x140fd8150]`) 은 **표의 5번째 칸 `[4]`** 이고, `PspSystemDlls[0]+0x20` (표의 0번 칸이 가리키는 구조체의 `+0x20`) 과 다르다.
- 이 칸 값이 "ntdll 실제 베이스" 와 같은지, 바로 옆 `+0x18` 과 같은지는 정적으로 못 정한다: `PspLocateNtdllAddressesForScpCfg` 는 `+0x18` 을 기준 주소로, `+0x20` 을 바꿔 읽는 쪽으로 쓰는 것처럼 보이고 (`+0x101 ~ +0x114`, `PspCopyNtdllExport +0x3a ~ +0x42`), `PspGetScpCfgFunctions +0x36 ~ +0x51` 은 `+0x20` 을 `ZwQueryVirtualMemory(-1, …)` 주소로 쓴다. (동적 4번.)
- **지금 우리 (PE_emulator)**: **확인 필요.** `SystemDll.cpp:244` 에 같은 의문이 TODO(VM) 로 있다 (run48 D1). 이번 결과는 "같은 칸" 까지만 확정.

## 동적분석으로 확인해야 할 것

1. **`Flags` 값 세 지점** (비트 14·15·29 와 마스크 통과 확인): `nt!PspCaptureUserProcessParameters +0x40e` (`nt+0x9f151e`, `EAX` = 비트 14 뺀 입력값) · `nt!PspSetupUserProcessAddressSpace +0xed` (`nt+0xae5e21`, 비트 15·29 처리가 끝난 뒤 `EBX`) · 복사 직후 `+0x1b9` (`nt+0xae5eed`) 뒤 사용자 블록 `+0x8`. 사진 `0x4001` 과 같은지. 비트 31 이 정말 0 인지.
2. **`+0x11` 값**: `nt!PspSetupUserProcessAddressSpace +0x41` (`nt+0xae5d75`, `MOVZX ECX,[R9+0x11]`) 의 R9+0x11 바이트 — test.exe 가 표준 핸들 상태를 몇으로 받는지 (비트 2~3), 비트 7. 쓰는 줄로 보려면 `nt!PspBuildCreateProcessContext +0x309` (`nt+0x840c29`) · `+0x1760` (`nt+0x842080`) 이 서는지.
3. **CFG 켜짐 여부**: 새 프로세스 `dt nt!_EPROCESS <주소> MitigationFlagsValues.ControlFlowGuardEnabled` (= `+0x750` 비트 0). 0 이면 `MiCfgInitializeProcess +0x1e` (`nt+0x90b652`) 에서 일찍 끝나 `+0x4a0`/`+0x4a8` 이 0 인 사진과 맞다. 1 이면 `nt!MiReferenceCfgVad +0x71` (`nt+0xaaf359`) · `+0x82` (`nt+0xaaf36a`) 에서 `R15` (= `VmWorkingSetList+0x4a0`) 와 쓰는 값.
4. **ScpCfg 결과 칸** (INIT 구역이라 멈춤점 아님, 부팅 뒤 값만 읽음): `dq nt!PspNtdllScpFunctions L4` (기대: ntdll 끝 쪽 올림 뒤 + `0x140 / 0x1c0 / 0x40 / 0xc0`, `MiState.Sections.ImageViewExtensionOffsets[0]` (`nt+0xe2d8e0`) 이 0 일 때). `dq nt!PspNativeSystemDllData+0x18 L2` 와 `lm m ntdll` 로 `+0x18`·`+0x20` 이 같은지 (= `Rdx` 값, `SystemDll.cpp:244` 의 TODO).
5. **`PackageDependencyData`·`CurrentDirectores`**: `PspCopyAndFixupParameters +0x1b9` (`nt+0xae5eed`) 뒤 사용자 블록 `+0x400`, `+0xf0 ~ +0x3ef` 가 0 인지.
- 추천 멈춤점 (모두 `nt+rva`, 조건은 test.exe 프로세스 이름): 위 1~3 줄에 적은 주소. 콜스택 `ret` 값은 `_calls.tsv` 와 같다 (`MiCfgInitializeProcess +0x76/+0x7b`, `+0x98/+0x9d`, `+0x131/+0x136`).
- **dast 요청 필요: 위 1·2·3·5번 (한 번의 test.exe 실행에서 동시에 볼 수 있음), 4번은 부팅 뒤 읽기만.**

## 다음 요청 후보

1. **Q63b (ntdll, 사용자 결정 대기)**: `ProcessParameters.Flags` 비트 4·9·10·11·16·26·28·30·31 와 `0x2000000` (비트 25) 를 ntdll 이 읽는 줄 → 비트 이름을 얻을 유일한 단서. 지금 PDB·커널 모두 이름을 안 준다.
2. `PspFinalizeScpCfgPage` (`PsInitializeScpCfgPages +0x118 ~ +0x152` 의 호출 대상) + `PsGetScpCfgPageTypeForProcess`: 위 표의 다섯 칸 (`+0x40 ~ +0x60`) 이 쪽 안에서 어떻게 쓰이는지, 어떤 프로세스가 어떤 쪽 종류를 받는지. PE_emulator D33 (공사 중) 에 바로 쓰임.
3. (작음) `MmInitializeProcessAddressSpace +0x4a5/+0x4ba` 의 `MiReferenceCfgVad` 두 자리가 어느 길인지.
4. (비트 31 이 정말 필요하면) 보안 프로세스 만드는 길 (`Vsl…SecureProcess`·`PspAllocateProcess` 의 `PsAttributeSecureProcess` 갈래) 과 `securekernel.exe` (PDB 없음, Ghidra 로 새로 analyze 해야 함 — 사용자 결정 필요).

## 판정

**분석성공.** 요청서 질문 (a)~(f) 모두 코드 줄로 답했다. 단 (a) 의 비트 이름은 PDB 에 없어 4·9·10·11·16·26·28·30·31 은 `[공백]` 이고 (ntdll 필요), (e) 는 "이 모양 검색에서는 켜는 줄 없음" 까지, (f-1) 의 "왜" 는 `[추측]`. 버전 대조 (`NtCreateUserProcess +0xc36`, `PspInsertThread +0x807`) 와 PDB GUID 모두 일치.

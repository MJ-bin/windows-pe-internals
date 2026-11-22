# 120 run65-66-leftovers — 정적 분석 결과 (Q124 + Q127, 질문 5개 + 근거 이월 2개)

- 요청서: `_workspace/sast/120_run65-66-leftovers.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`). 덤으로 **이미 있던** ntoskrnl `.asm` 둘 (`PspSchedulerSharedDataRegionCreate`, `NtSetInformationProcess`) 을 읽기만 했다. PE 바이트는 `sample/bin/test.exe` (파일 값, VM 파일이 같다는 가정 — sast/59 와 같음).
- PDB GUID: **둘 다 기준과 일치**
  - ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`)
  - ntdll `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`, sast/119 와 같음)
- 분석: ntdll 은 이미 돼 있음 (`.gpr` + `_index.tsv` + `SetPdb:` 줄 1개). **다시 analyze 안 했다.** 시작할 때 돌고 있는 Ghidra 프로세스 없음.
- 이미 있던 `.asm`·`.c` (다시 안 뽑고 읽기만): `LdrpQueryIllegalCWDDevices` · `LdrpInitializeExecutionOptions` · `LdrpInitializeTls` · `LdrpAllocateTls` · `LdrpAllocateTlsEntry` · `LdrpHandleTlsData` · `LdrpAllocateSchedulerSharedData` · `LdrpSendPostSnapNotifications` · `LdrpNotifyLoadOfGraph` · `SbUpdateSwitchContextBasedOnDll` · `LdrpInitShimEngine` · `LdrpLoadShimEngine` · `LdrpGetShimEngineInterface` · `LdrpGetProcApphelpCheckModule` · `LdrpInitializePolicy` · `LdrpInitializeProcess` · ntoskrnl `PspSchedulerSharedDataRegionCreate` · `NtSetInformationProcess`.
- **새로 뽑은 함수 1개**: `^LdrInitShimEngineDynamic$` depth 0 (`ms/decomp/ntdll/`, `ms/disassemble/ntdll/`, 로그 `ms/ghidra/ntdll.export.sast120a.stdout`, exit 0, `_calls.tsv` 에 9줄 덧붙음). 이유: shim 엔진을 올리는 길의 세 번째 입구 (지도의 `LdrpLoadShimEngine` · `LdrpGetShimEngineInterface` 호출자인데 `.asm` 이 없었음).
- 전수 검색: 스크래치 `ntdll.dis`·`KernelBase.dis`·`kernel32.dis` (objdump 풀이, 커밋 안 함) + sast/103 의 `scan_flags.py` 재사용 + 이번에 만든 작은 도구 둘 (`s120/rda.py` = rva 에서 ANSI 글 읽기, `s120/scan_flags_any.py` = `scan_flags.py` 를 파일 이름 인자로 바꾼 것). 스크래치 = `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/`. 결과 파일은 레포에 안 남김.
- 버전 확인: **일치** (`_calls.tsv` `ret_off`)
  - `LdrpInitializeProcess +0x1bb6 / +0x1bbb` → `LdrpInitializeGraphRecurse` · `NtCreateUserProcess +0xc31 / +0xc36` → `PspAllocateProcess` · `_LdrpInitialize +0x167 / +0x16c` → `LdrpInitializeProcess` · `LdrpInitializeNode +0x166 / +0x16b` → `LdrpCallTlsInitializers`, `+0x197 / +0x19c` → `LdrpCallInitRoutine`.
- 스크립트 문제: **없음.**
- 오프셋 약속: `call +0x..` = CALL 이 있는 자리, `ret +0x..` = 다음 줄 (WinDbg `caller+0x..`), 그냥 `+0x..` = 그 명령이 시작하는 자리. 점프 목적지는 "목적지 주소 − 함수 시작" 으로 셈 (이번에 쓴 목적지는 모두 `.asm` 의 주소로 다시 뺐다).
- 이번에 안 한 것: **Q124 (e)** (그림 2 새 상자) — 사용자 결정 대기라서 건드리지 않음.

---

## 한눈에

| # | 질문 | 한 줄 답 `[정적]` | 지금 우리 (PE_emulator) |
|---|---|---|---|
| Q124-a (이월) | `LdrpPolicyBits` 를 3.8 로 | **옮기자.** 쓰는 줄 5개 (`LdrpInitializePolicy +0x7e · +0x26b · +0x305 · +0x31d · +0x329`) 전부 "패키지 앱 + `DllPath` 비어 있지 않음" 갈래 안. 이번에 `ntdll.dis` 로 다시 세어 같음 | 맞음 |
| Q124-b | `+0x733` `LdrpQueryIllegalCWDDevices` 가 IFEO 키 없어도 닿나 | **닿는다.** 키 없음 갈래 (`+0xad`) 도 `+0x604` 를 거쳐 `+0x71c` 에서 **일반 길과 합쳐진다.** 호출은 `RCX = 0`. 값은 test.exe 에선 **0** (KUSD 비트 0) | 맞음 |
| Q124-c | exe TLS 블록 `0x11`B 의 근거 줄 | **`(End − Start) + max(정렬, 0x10)`**: `LdrpAllocateTls +0xb7 SUB R15,[RDI]` · `+0xec LEA R8,[R15+1]` · `+0xf0 ADD R8,RBX`. test.exe 파일 값 `1 + 0x10 = 0x11`. **`SizeOfZeroFill` 은 어느 줄도 안 읽는다** | 맞음 |
| Q124-d | 스케줄러 공유 데이터 섹션 크기 | **커널이 정한다 — 상수 `0x2000` (2쪽)**: `PspSchedulerSharedDataRegionCreate +0x127 MOV R15D,0x2000`. ntdll 은 크기를 안 넘긴다 (`R9D = 8` 은 출력 핸들 칸 길이) | 맞음 (줄임) |
| Q127-a | `Flags.LoadNotificationsSent` 를 켜는 줄 전수 | **`LdrpSendPostSnapNotifications +0xce` 하나뿐.** 끄는 줄 0. 읽고-고쳐-되쓰는 줄 5곳은 새 비트를 안 만든다 | 맞음 |
| Q127-b | shim 엔진 올리는 길의 조건 | 문지기 = `LdrpInitializeProcess +0x19d7` (`pShimData ≠ 0`) **그리고** `+0x19dc` (`[pShimData+0x210] ≠ 0`). test.exe 는 `+0x210 = 0` 이라 `+0x19e3 JZ → +0x19f4`. `g_ShimsEnabled = 1` 은 `LdrpLoadShimEngine +0x148` **한 줄** | 맞음 (결과 같음) |
| Q127-c (이월) | `SbUpdateSwitchContextBasedOnDll` 이 `SwitchBackContext` 를 쓰나 | **쓴다.** `+0xd9 MOV [R14+0xc0],RDI` (ntdll 에서 모듈 항목 `+0xc0` 을 쓰는 줄은 이것뿐) | 확인 필요 (sast/116 그대로) |

---

## Q124 — run65 남은 것

### (a) `LdrpPolicyBits` → 3.8 로 옮길지 (근거만 옮김)

- sast/111 질문 2 가 이미 **"옮기자"** 로 답했다. 이번에 줄을 다시 셈:
  - 이 칸 (`0x1801cd7fc`) 을 **쓰는** 줄 5개: `LdrpInitializePolicy +0x7e` (`MOV …,0x29`) · `+0x26b` (`OR 4`) · `+0x305` (`OR 0x40`) · `+0x31d` (`OR 2`) · `+0x329` (`AND 0xfffffffe`). ntdll 전체에서 이 다섯뿐 (나머지 16줄은 전부 읽기).
  - 다섯 모두 한 길 뒤에 있다: `+0x42 MOV AL,[PEB+3]` · `+0x46 TEST AL,0x10` (패키지 앱) · `+0x48 JNZ +0x72` · `+0x72 CMP word [DllPath.Length],0` · `+0x75 JZ +0x4a` (비면 안 들어옴) · `+0x77` · **`+0x7e`**. 이 앞부분에서 `+0x7e` 너머로 뛰는 점프는 없다 (`+0x4c JNZ +0x6d`, `+0x51 JZ +0x58`, `+0x75 JZ +0x4a` 셋뿐).
  - test.exe: `PEB.BitField = 0x4` (sast/111) → 비트 4 꺼짐 → `+0x4a TEST AL,2` 도 꺼짐 → `+0x4e CMP word [RCX],0` 같음 → `+0x51 JZ +0x58` 로 그냥 돌아감. 값은 파일 `0x16` 그대로.
- 부르는 쪽: `LdrpInitializeProcess +0xfc5 / +0xfca` → `LdrpInitializePolicy`.
- **지금 우리**: 맞음 (공사 중 빈 함수 + 파일 값 `0x16`, sast/111).
- baggage 줄: **7.3 `≤P1a` 에서 빼서 3.8 의 "조건부 안 탐" 줄로 옮긴다** (위 쓰는 줄 5개 + 조건 한 줄).

### (b) `LdrpInitializeExecutionOptions +0x733` (`LdrpQueryIllegalCWDDevices`) — IFEO 키가 없을 때도 닿나

근거: `ms/disassemble/ntdll/LdrpInitializeExecutionOptions.asm`, `LdrpQueryIllegalCWDDevices.asm`, `_calls.tsv` (`+0x733 / +0x738`), `_refs/0x1d23f4.tsv`.

**1. 닿는다 — 키 없음 갈래도 같은 줄로 들어간다.**

| 자리 | 줄 | 뜻 |
|---|---|---|
| `+0xa2 MOV RAX,[RBX+0x20]` · **`+0xa6 TEST dword [RAX+8],0x4000`** · **`+0xad JNZ → +0x604`** | `ProcessParameters.Flags` 비트 14 (`0x4000`) = "IFEO 키 없음" 표시 (커널이 `0xC0000034` 를 보고 켬, run50 · sast/111). 켜져 있으면 IFEO 읽기 구간 (`+0xb3 ~ +0x603`) 을 통째로 건너뜀 | **test.exe 는 이쪽** (`Flags = 0x4001` `[관찰]` run44·run65) |
| `+0xc5 RtlpOpenImageFileOptionsKeyEx` (ret `+0xca`) · `+0xcf TEST EAX` · **`+0xd1 JS → +0x604`** | 표시는 없었지만 열기가 실패한 경우도 `+0x604` | 키 없음의 다른 입구 |
| (키가 열렸으면) `+0xde ~ +0x603` | IFEO 값들을 읽고 `+0x604` 로 내려옴 | 키 있음 |

- `+0x604 ~ +0x62a`: `LdrControlFlowGuardEnforced` (call `+0x604`, ret `+0x609`) → `RtlSetGlobalHeapFeatures` (call `+0x61e`, ret `+0x623`) → `+0x623 XOR ESI,ESI` (**`ESI` = 0 로 리셋**).
- `+0x62a JZ +0x696` · `+0x62c CMP [LdrpIsSecureProcess],SIL` / `+0x633 JNZ +0x696` · `+0x635 TEST byte [0x7ffe03a0],1` (KUSD `ImageFileExecutionOptions` 비트 0) / `+0x63d JZ +0x696` — 이 셋은 모두 `+0x696` 로 건너뜀 (검증기 쪽 HKCU 키 읽기를 거르는 문지기).
- `+0x696 TEST dword [RBX+0xbc],0x2000100` (PEB `NtGlobalFlag`) / `+0x6a0 JNZ +0x73d` · `+0x6a6 LdrpPayloadRestrictionMitigationsEnabled` (ret `+0x6ab`) / `+0x6ad JNZ +0x73d`: 켜졌으면 **`+0x73d LdrpInitializeApplicationVerifierPackage`** (call `+0x75e`, ret `+0x763`) 로 갔다가 `+0x76a JNS → +0x71c` 로 돌아옴. 아니면 `+0x6b3` 로 내려감.
- `+0x6b3 XOR R12D,R12D` · `+0x6b6 TEST R14,R14 / +0x6b9 JNZ +0x71c` (모던 앱 키) · `+0x6bb TEST RDI,RDI / +0x6be JNZ +0x71c` (**IFEO 키 핸들 `RDI`** — 키 없음이면 0, 시작값 `+0x56 MOV EDI,ECX`) · `+0x6c0 CMP byte [RBX+2],R12B / +0x6c4 JZ +0x71c` (`BeingDebugged == 0`) — **세 문 모두 `+0x71c` 로 합쳐진다.** (디버거가 붙어 있으면 `+0x6c6 ~ +0x715` 가 환경 변수를 정수로 바꿔 `NtGlobalFlag |= 0x70` 하는 길인데 test.exe 는 안 탐.)
- `+0x71c ~ +0x725`: `PEB+0x7c4` 비트 0 이면 `PEB+0x7c0 |= 1` (LeapSecondFlags).
- **`+0x72c TEST ESI,ESI` · `+0x72e JS +0x797`** — 이 문만 통과하면 `+0x730 MOV RCX,RDI` · **`+0x733 CALL LdrpQueryIllegalCWDDevices` (ret `+0x738`)** · `+0x738 JMP +0x7bd` (정리로 건너뜀).
- 안 닿는 경우 (**test.exe 는 해당 없음**, 그 길 모두 "음수 상태" 이거나 큰 페이지 이미지): `+0x3dd TEST EAX / +0x3df JS +0x797` (`ImageUsesLargePages` (`PEB+3` 비트 0) 일 때 `LdrpLargePageDllKeyHandle` 을 `NtOpenKey` (call `+0x3c6`, ret `+0x3cb`) 하다 `0xC0000034` 아닌 실패) · `+0x72e JS` 가 `ESI < 0` 일 때 (`+0x708 MOV ESI,EAX` = 디버그 힙 환경 변수 정수 변환 실패, `+0x766 MOV ESI,EAX` = 검증기 패키지 init 실패).
- 이 함수는 `LdrpInitializeProcess +0x531 / +0x536` 에서 불린다. `LdrpQueryIllegalCWDDevices` 의 부르는 곳은 이 `+0x733` 하나뿐 (`_calls.tsv`).

**2. 값은 어느 쪽이든 0 인가 — test.exe 에선 0, 일반론으론 아님.**

`LdrpQueryIllegalCWDDevices(RCX = IFEO 키 핸들)` (rva `0x57d54`):
- **`RCX = 0`** (키 없음, test.exe): `+0x13 TEST RCX,RCX` · `+0x16 JNZ +0x52` 안 탐 → `+0x18 MOVZX ECX,byte [0x7ffe02d5]` (KUSD `MitigationPolicies`) · `+0x20 SHR ECX,4` · `+0x23 AND ECX,3` (= `CurDirDevicesSkippedForDlls`) · `+0x26 CMP ECX,3 / +0x29 CMOVZ ECX,EDI(-1)` → `+0x2e JZ +0x91` (-1 이면 마스크 -1) · `+0x30 CMP 1 / +0x33 JZ +0x85` (1 이면 `0x2000`) · `+0x35 CMP 2` · `+0x38 MOV EAX,0x10` · `+0x3d CMOVNZ EAX,EBX(0)` · **`+0x40 MOV [LdrpIllegalCWDDevices],EAX`** — 0 이면 0 을 **명시적으로 쓴다** (파일 값도 0 이라 값은 안 바뀜).
- **`RCX ≠ 0`** (키 있음): `+0x52 ~ +0x6e` `RtlQueryImageFileKeyOption(키, "CWDIllegalInDLLSearch" (rva `0x17aa90`), 형식 4, &값, 크기 4)` (call `+0x6e`, ret `+0x73`).
  - 실패 (`+0x75 JS +0x18`) → 위 KUSD 길과 같음.
  - 성공: `+0x7b LEA EAX,[RCX+1]` · `+0x7e CMP EAX,3` · `+0x81 JBE +0x2c` (값이 -1·0·1·2 일 때만 씀) → -1 → `-1`, 0 → `0`, 1 → `0x2000`, 2 → `0x10`. 그 밖의 값은 `+0x83 JMP +0x18` 로 KUSD 길.
- KUSD `CurDirDevicesSkippedForDlls`: 사진 P1a `0y00` (sast/59 `[관찰]`). 커널이 부팅 때 `PspInitPhase2 +0x136 · +0x13f · +0x16c` 로 `PspCurDirDevicesSkippedForDlls` (파일 처음 값 0) 를 옮겨 쓴다 (sast/110 표 74). 그래서 **"0" 은 이 VM 의 정책 값 때문**이지, 코드가 0 을 보장하는 건 아니다. 사진 `LdrpIllegalCWDDevices` = `끝까지 0` (run44).
- → 결론: **test.exe (키 없음, 정책 0) 에서는 키가 있든 없든 (값 항목이 없으면) 0.** IFEO 에 `CWDIllegalInDLLSearch` 를 넣거나 `CurDirDevicesSkippedForDlls` 정책을 바꾼 컴퓨터에서만 0 이 아니다.

**지금 우리 (PE_emulator): 맞음.** `Src/ntdll/LdrInit.cpp:1621` 이 `LdrpQueryIllegalCWDDevices()` 를 "RCX = RDI = 0, `+0x738 JMP +0x7bd`" 로 부르는 줄과 같고, `:1477 ~ 1483` 이 위 `+0x13 ~ +0x40` 을 근거로 "KUSD `+0x2d5` = 0 → 0, 빈 함수와 결과가 같다" 고 적었다. 우리 `+0x6b9 / +0x6be` 주석 (`:1596`) 도 맞다. 키 있음 갈래 (IFEO 값) 는 test.exe 에 없어 공사 중으로 둬도 된다.

**baggage 줄**: `LdrpIllegalCWDDevices` 줄 (7.3 / 3.8 "마스크 0") 에 한 줄 보탬 — "`LdrpInitializeExecutionOptions +0x733 / +0x738` 는 키 없음 (`+0xad`) 이어도 탄다 (`RCX = 0`). 값 0 은 KUSD `CurDirDevicesSkippedForDlls = 0` 때문 (이 VM)" `[정적]` sast120.

### (c) exe TLS 블록 크기 — `0x11`B 의 근거 줄

근거: `LdrpAllocateTls.asm`, `LdrpAllocateTlsEntry.asm`, `LdrpInitializeTls.asm`, `LdrpHandleTlsData.asm`, `sample/bin/test.exe` (PE 바이트).

**1. test.exe 파일의 TLS 디렉터리** (데이터 디렉터리 9, rva `0x79500`, 크기 `0x28`, 파일 오프셋 `0x77d00`, `.rdata` 안, 바이트를 직접 읽음):

| 칸 | 바이트 | 값 |
|---|---|---|
| `StartAddressOfRawData` (`+0x0`) | `90 9f 07 40 01 00 00 00` | `0x140079f90` |
| `EndAddressOfRawData` (`+0x8`) | `91 9f 07 40 01 00 00 00` | `0x140079f91` |
| `AddressOfIndex` (`+0x10`) | `e0 2c 08 40 01 00 00 00` | `0x140082ce0` (`.data`) |
| `AddressOfCallBacks` (`+0x18`) | `80 e3 06 40 01 00 00 00` | `0x14006e380` (콜백 목록 `{0x1400012a0, 0}`) |
| `SizeOfZeroFill` (`+0x20`) | `00 00 00 00` | **0** |
| `Characteristics` (`+0x24`) | `00 00 10 00` | `0x00100000` → 정렬 코드 (`>>20 & 0xf`) = **1** (1바이트 정렬) |

- 원본 데이터 = `End − Start` = **1 바이트** (파일 오프셋 `0x78790`, 값 `00`). test.c 에는 `__declspec(thread)` 변수가 없어서 CRT 가 깔아 둔 1바이트 시작·끝 표시뿐이다.

**2. 크기를 셈하는 줄 — `LdrpAllocateTls` (스레드마다 한 번, 항목마다)**

| 줄 | 하는 일 |
|---|---|
| `+0x79 MOV R12D,0x10` | "지금까지의 정렬" 시작값 16 |
| `+0x98 MOV EDX,[R14+0x34]` · `+0x9c SHR EDX,0x14` · `+0x9f AND EDX,0xf` | 항목 안에 복사해 둔 디렉터리 `Characteristics` (`+0x34` = 항목 `+0x10` + `+0x24`) 에서 정렬 코드 꺼냄 → test.exe 1 |
| `+0xab LEA RDI,[R14+0x10]` · `+0xa2 LEA RCX,[R14+0x18]` · `+0xb4 MOV R15,[RCX]` · **`+0xb7 SUB R15,[RDI]`** | **`R15 = End − Start`** (= 1). `SizeOfZeroFill` (`항목 +0x30`) 은 이 함수의 어느 줄도 안 읽는다 (`[R14+0x30]` 줄 0) |
| `+0xba TEST [R14+0x34],0xf00000` · `+0xc2 LEA ECX,[RDX-1]` · `+0xc5 CMOVBE ECX,EDX` · `+0xc8 MOV R8D,1` · `+0xce SHL R8D,CL` | 정렬 = `1 << (코드-1)` (코드 0 이면 1). test.exe → `R8D = 1` |
| `+0xd1 CMP R8D,0x10` · `+0xd5 CMOVC R8D,R12D` · `+0xd9 MOV R12D,R8D` · `+0xdc LEA EBX,[R8-1]` | **정렬은 최소 `0x10`** (작으면 `R12D` = 16 로 올림). test.exe → `R8D = 0x10`, `EBX = 0xf` |
| **`+0xec LEA R8,[R15+1]`** · **`+0xf0 ADD R8,RBX`** | **받을 크기 = `(End − Start) + 1 + (정렬 − 1)` = `(End − Start) + 정렬`** = 1 + 0x10 = **`0x11`** |
| `+0xe0 MOV EDX,[NtdllBaseTag]` · `+0xe6 ADD EDX,0xc0000` · `+0xf3 MOV RCX,[LdrpTlsHeap]` · `+0xfa CALL RtlAllocateHeap` (ret `+0xff`) | 태그만 더한 플래그 — **0 채움 비트 (`0x8`) 가 없다** (대조: `LdrpAllocateSchedulerSharedData +0x54 OR EDX,8` 은 있음) |
| `+0x108 LEA RCX,[RAX+R12]` · `+0x10c NOT RBX` · `+0x10f AND RCX,RBX` · `+0x112 MOV [RCX-8],RAX` | 받은 주소 `RAX` 에서 정렬된 쪽으로 올림 (`RAX` 가 16 정렬이면 **`RAX + 0x10`**) + 바로 앞 8바이트에 원래 주소 숨김 |
| `+0x121 MOV [RSI+RAX*8],RCX` (`RAX` = 항목 `+0x40` 인덱스) · `+0x125 MOV R8,R15` · `+0x128 MOV RDX,[RDI]` · `+0x12b CALL memcpy` (ret `+0x130`) | 벡터 슬롯에 걸고 **원본 `End − Start` 바이트만** 복사 |

- 받은 블록 `0x11`B 중 앞 `0x10` 은 정렬 여유 + 숨긴 포인터 칸, 뒤 1바이트가 TLS 데이터. 딱 맞는다.
- 사진 정합 `[추측: 정합]`: run44 슬롯 0 = `0x25996f38440` = 블록 + `0x10` 이면 블록은 `…38430`, TLS 벡터 블록 (`0x58`B, `…383d0` 부터 `0x60`B 칸) 바로 다음 자리. 값이 어긋나지 않는다.
- `LdrpInitializeTls` 자체는 **바이트 크기를 안 센다.** 하는 일은 항목 수 `N` 세기 (`+0xf5 LdrpAllocateTlsEntry` (ret `+0xfa`) 를 항목마다) · 비트 수 `N + 8` (`+0x115 LEA ESI,[RDI+8]`) · `+0x15d LdrpAllocateTls` (ret `+0x162`) 부르기까지. 디렉터리를 항목으로 옮기는 줄은 `LdrpAllocateTlsEntry +0x54 ~ +0x6b` (`movups` 둘 + `movsd` = 정확히 `0x28`B 복사), `End < Start` 면 `+0x8f ~ +0x93 JC → +0x154` 에서 `0xc000007b`.
- 같은 식이 **DLL 쪽** `LdrpHandleTlsData +0x17a MOV R9,[RCX+0x18]` · `+0x17e SUB R9,[RCX+0x10]` · `+0x1db LEA R8,[R9+1]` · `+0x1df ADD R8,RDI` 에도 그대로 있다 (kernel32 · dll1 · dll2 용, test.exe 는 DLL TLS 디렉터리가 없어 안 쓰일 수 있음 — 이번엔 안 따짐).

→ 확정 `[정적]`: **exe TLS 블록 = `0x11` 바이트** (PE 바이트 + 위 줄). `SizeOfZeroFill` 이 0 이 아닌 이미지는 진짜 코드가 그만큼을 **받지도 0 으로 채우지도 않는다** (MS 링커는 보통 0 을 넣음 — test.exe 는 0).

**지금 우리 (PE_emulator): 맞음.** 우리는 진짜 `LdrpInitializeTls` → `LdrpAllocateTls` 를 Unicorn 안에서 돌리므로 (`Src/ntdll/LdrInit.cpp:4476 ~ 4500`) 크기가 따로 하드코딩된 데 없이 `0x11` 로 나온다. 확인 필요 1가지: VM 의 test.exe 가 `sample/bin/test.exe` 와 같은 바이트인지 (아래 "동적으로 확인할 것" 1).

**baggage 줄**: "exe TLS 블록" 줄의 `[추측: 결과서 값 — 셈 줄 없음]` → `[정적] sast120` 으로 바꾸고 경로에 `LdrpAllocateTls +0xb7 · +0xec · +0xf0 (크기 = End−Start + 정렬)` · `+0x108 ~ +0x12b` (정렬·복사) 를 적는다.

### (d) 스케줄러 공유 데이터 섹션 크기 — 어디서 정해지나

근거: `LdrpAllocateSchedulerSharedData.asm`, ntoskrnl `NtSetInformationProcess.asm`, `PspSchedulerSharedDataRegionCreate.asm` (둘 다 이미 있던 것).

- **ntdll 은 크기를 안 정한다.** `LdrpAllocateSchedulerSharedData +0x19 MOV R9D,8` · `+0x1f LEA R8,[RSP+0x30]` · `+0x24 OR RCX,-1` · `+0x28 LEA EDX,[R9+0x68]` (= `0x70`) · `+0x2c CALL NtSetInformationProcess` (ret `+0x31`): 인자 = (현재 프로세스, 클래스 `0x70`, **출력 핸들 칸 주소**, 길이 `8`). **이 `8` 은 핸들 한 칸의 길이**이지 섹션 크기가 아니다.
- **커널이 정한다 — 상수.** `NtSetInformationProcess +0xec4 CMP R13D,8` (길이가 정확히 8 이어야) → `+0xf17 CALL PspSchedulerSharedDataRegionCreate` (ret `+0xf1c`, `RDX = &[RSP+0x70]` 출력 핸들) → `+0x13ea MOV RAX,[RSP+0x70]` · `+0x13ef MOV [R15],RAX` (사용자 버퍼에 핸들).
- `PspSchedulerSharedDataRegionCreate` (rva `0x9325bc`) 안:
  - **`+0x127 MOV R15D,0x2000`** — 크기 `0x2000` 바이트 = **2쪽**.
  - `+0x13d MOV R8D,R15D` → `+0x158 CALL MmCreateSectionEx` (ret `+0x15d`) — 3번째 인자로 `0x2000` `[추측: 인자 이름 = 최대 크기]`.
  - `+0x16e MOV [RBP+0x50],R15` · `+0x172 LEA R8,[RBP+0x50]` → `+0x18e CALL MiMapViewInSystemSpace` (ret `+0x193`): 시스템 쪽 보기 크기도 `0x2000`.
  - `+0x1a9 MOV R9D,2` · `+0x20a SUB R9,1` · `+0x20e JNZ` — 쪽마다 한 칸 (스트라이드 `0x50`) 을 채우는 반복이 **2번**, 쪽 간격 `+0x1af LEA R11D,[RDI+1]` (= `0x1000`).
  - `+0x239 MOV [RSP+0x30],RAX` (`RAX = &[RBP+0x50]`) → `+0x24c CALL MmMapViewOfSection` (ret `+0x251`): **이 프로세스 쪽 보기** 크기도 같은 `0x2000`.
  - 이 칸들은 `ObCreateObjectEx` (`+0x9f`, 객체 본체 크기 `+0x7d MOV R14D,0xf8`) 로 만든 객체 안에 있다. **`0xf8` 은 객체 본체 크기지 섹션 크기가 아니다** (혼동 주의).
- 앞 문 둘: `+0x57 ~ +0x67 CMP RSI,[현재 스레드 프로세스]` · `JNZ +0x2f9` → `0xc00000bb` (자기 프로세스만 허용). `+0x4d TEST R8B,0x10` (`Feature_UserModeAutoBoost` 상태) 은 어느 쪽이든 `+0x613` 로 합쳐져 **문지기가 아니다** (꺼져 있으면 사용 보고만 하고 계속).
- → **사진 `[관찰]` "VAD 2쪽 매핑" (run44 P1a) 이 이 상수와 맞는다.**
- 섹션 안 슬롯을 나누는 법 (스레드마다 `TEB+0x1850` 이 가리키는 표) 은 `PspSchedulerSharedDataRegionSlotAllocate` 몫 — 이번 범위 밖 (sast/107 다음 후보 1, `.asm` 이미 있음).

**지금 우리 (PE_emulator): 맞음 (알고 줄임).** 우리 nt 는 클래스 `0x70` 에 기본 대답 (D32) 을 하고 출력 핸들 칸을 안 쓴다 (`LdrInit.cpp:4426 ~ 4440`) → 섹션도 매핑도 없고 `TEB+0x1850 = 0`. 크기를 우리가 정하는 곳이 지금 없다. 만들게 되는 날엔 `0x2000` (2쪽) 한 상수면 된다.

**baggage 줄**: "스케줄러 공유 데이터 섹션·매핑" 줄 (2346 줄 근방) 의 "크기 셈은 안 봄" → "크기 = 상수 `0x2000` (`PspSchedulerSharedDataRegionCreate +0x127`), `MmCreateSectionEx` (`+0x158`) · 시스템 보기 (`+0x18e`) · 이 프로세스 보기 (`+0x24c`) 셋이 같은 값 `[정적]` sast120".

---

## Q127 — run66 남은 것

### (a) `Flags.LoadNotificationsSent` (비트 3, `0x8`, 항목 `+0x68`) 를 켜는 줄 전수

근거: sast/103 방법 그대로 — 스크래치 `scan_flags.py` 를 **다시 돌림** (ntdll 코드 구역 전체 `objdump` 풀이, 항목 안 연결 고리 7종 기준으로 `Flags` 에 닿는 쓰기 전부) + `lea reg,[x+0x68]` 전수 + `KernelBase.dis`·`kernel32.dis` 에서 같은 검색.

- **답: `LdrpSendPostSnapNotifications +0xce OR dword [RBX+0x68],8` 하나뿐.** 줄 목록이 sast/103 표 1-E 와 한 줄도 안 어긋난다.
- 이 줄이 도는 조건: `LdrpNotifyLoadOfGraph +0x19 MOV [노드+0x38],7` (ret 전에 상태를 7 로) → `+0x20 CALL LdrpSendPostSnapNotifications` (ret `+0x25`). 안에서 `+0x7a MOV RDI,[R14+8]` 부터 노드의 모듈마다 `+0x8e LEA RBX,[RDI-0xa0]` → `+0xa3 SbUpdateSwitchContextBasedOnDll` (ret `+0xa8`) → (`g_pShimmedModuleList == 0` 이면) `+0xb6 LdrpCheckModule` (ret `+0xbb`) → `+0xc1 LdrpSendDllNotifications` (ret `+0xc6`) → **`+0xce`** → `+0xd8 LdrpLogDllState` (ret `+0xdd`). `+0xce` 앞에 **비트 3 을 보는 문지기는 없다.**
  - 건너뛰는 길은 하나: 검증기 (`+0x88 AND EBP,0x100` = `NtGlobalFlag & 0x100` · `+0x95 TEST EBP,EBP` · `+0x97 JNZ +0x12d`) 의 `AVrfDllLoadNotification` (call `+0x130`, ret `+0x135`) 이 음수를 돌려주면 `+0x140 JMP +0xe6` 로 목록을 빠져나감 (그 모듈부터 비트 안 켜짐). test.exe 는 `NtGlobalFlag = 0` (사진).
  - 노드는 한 번만 들어온다: `LdrpNotifyLoadOfGraph +0x4e CMP EAX,6` / `+0x51 JZ +0x61` (상태 6 만 재귀) · `+0x53 CMP EAX,7` / `+0x56 JL +0x83` (7 이상은 건너뜀). 그래서 **모듈 항목당 한 번**, ntdll 항목은 `State 9` 라 아예 안 들어옴 (사진 ntdll `0y0`).
- **다른 쓰기 명령 전수** (비트 3 을 새로 만들 수 있나):
  - 값이 명령에 박힌 `OR`/`BTS`/`MOV`: 비트 0·1·2·5·6·7·8·9·10·11(바이트 `+0x69`)·13·15·19·20·21·22·24·26·28 뿐 (sast/103 표). **8 (`0x8`) 은 `+0xce` 하나.**
  - 끄는 줄: `AND` 마스크 `0xfffffffb`(비트 2) · `0xffffffbf`(6) · `0xffffffdf`(5) · `AND byte [..+0x69],0xf7` (`LdrpLoadShimEngine +0x266`, 비트 **11**). **비트 3 을 끄는 줄 0.**
  - 읽고-고쳐-되쓰는 줄 (네 함수 8줄: `LdrpCompleteMapModule +0x26b · +0x27d`, `LdrpProcessMappedModule +0x183`, `LdrpInitializeProcess +0x13c4 · +0x13cf · +0x13e4 · +0x13f0`, `AvrfMiniLoadDll +0x880`) 은 **같은 항목의 옛 값 + 다른 비트** 라 새 비트를 안 만든다. 한 줄 `LdrpInitializeProcess +0x15b8 OR [RBX+0x68],ECX` 는 **다른 항목에서 복사** (AppVerifier 복제, `AvrfAppVerifierMode = 0` 이라 test.exe 는 안 탐 — sast/103).
  - 칸 주소를 들고 다니는 `lea reg,[x+0x68]`: 로더 안에서 `rsp` 아닌 것은 `LdrpProcessMappedModule +0x4a · +0x7c · +0x157 · +0x255` 와 `LdrpLoadDependentModuleInternal +0xb7c` (읽기만) — sast/103 과 같음.
  - **KernelBase · kernel32**: 같은 검색에서 `[reg+0x68]` 4바이트 쓰기가 KernelBase 14줄 · kernel32 6줄 나오지만 전부 다른 구조체다 (함수 이름: `FlsGetValue` · `BasepCopyFile*` · `BaseCopyStream` · `CreateProcessInternalW` 지역 블록 · `ReadSetting` · `PsspThunkWin32Nt_*` (그중 `or` 는 `PsspThunkWin32Nt_THREAD_ENTRY +0x8a` 한 줄, 프로세스 스냅샷) · `BaseDllCaptureIniFileParameters` · `WerpLaunchAeDebug` · `SdbpMatchList` …). 모듈 항목 `Flags` 를 쓰는 줄 없음.
  - 이 비트를 **읽는** `TEST/BT [reg+0x68],8·3` 꼴도 ntdll 에 0줄.
- **한계**: 이 풀이는 ntdll · KernelBase · kernel32 코드뿐이다 (apphelp · shimeng · 검증기 DLL · 다른 프로세스가 PEB 로더 목록을 직접 걸어 쓰는 건 범위 밖). `[reg+idx*N+0x68]` 꼴은 못 봄 (로더에서 못 봤음).
- → **"syscall 5번" 추정의 받침이 강해진다.** 알림 한 번 = 모듈 항목 하나 (`+0xce`) 이고, 다섯 모듈 (exe · KERNEL32 · KERNELBASE · dll1 · dll2) 이 `0y1`, ntdll `0y0` 인 사진 (`ms/logs/04_baggage-read_P1b.txt:3392 · 3602 · 3707 · 3812 · 3917` = 1, `:3497` = 0) 과 정확히 맞는다. 단 syscall 횟수 자체는 `LdrpCheckModule` → `CompatCachepLookupCdb` 안 조건 (sast/61) 이 따로 정하므로, 이 줄은 "모듈이 다섯" 의 받침일 뿐이다.

**지금 우리 (PE_emulator): 맞음.** 진짜 `LdrpSendPostSnapNotifications` 를 덫으로 돌려서 (`Src/ntdll/LdrInit.cpp:4817 ~ 4819`) `+0xce` 가 진짜 코드로 켜진다. `Src` 에서 `LoadNotificationsSent` 를 직접 쓰는 줄은 0 (grep).

**baggage 줄**: 3.5 `LoadNotificationsSent` 줄의 "이 비트를 켜는 다른 줄은 안 찾아봄" → "**전수: `+0xce` 하나, 끄는 줄 0** `[정적]` sast120 (sast/103 방법 재실행, KernelBase·kernel32 포함)". 해설 · run66 문서의 `[추측]` ("syscall 5번") 받침에 한 줄.

### (b) shim 엔진을 올리는 길의 조건 — sast/116 `[pShimData+0x210]` 와 이어서

근거: `LdrpInitializeProcess.asm` (`+0x19c8 ~ +0x19f4`, `+0x1ca7 ~ +0x1d0e`), `LdrpInitShimEngine.asm`, `LdrpLoadShimEngine.asm`, `LdrpGetShimEngineInterface.asm`, `LdrInitShimEngineDynamic.asm` (새로), `LdrpGetProcApphelpCheckModule.asm`, `_refs/g_ShimsEnabled.tsv`, `_map/LdrInitializeThunk_*.tsv` (깊이 5·6).

**한 줄: test.exe 는 호환 질의가 shim 을 안 골라 `[pShimData+0x210] == 0` 이라 `LdrpInitializeProcess +0x19e3 JZ → +0x19f4` 에서 `LdrpInitShimEngine` 을 부르지도 않는다.** (사진 P0 ~ P2 블록 `+0x210 = 0`, sast/116. 그 칸을 **누가 쓰나는 여전히 `[공백]`.**)

**문지기 (순서대로)**

| 자리 | 줄 | 조건 |
|---|---|---|
| `LdrpInitializeProcess +0x19cd MOV R13,[RSP+0x180]` (PEB `+0x2d8` `pShimData` 의 지역 사본, `+0x69f`) | `+0x19d7 TEST R13,R13` · `+0x19da JZ → +0x19f4` | `pShimData ≠ 0` |
| `+0x19dc CMP dword [R13+0x210],EAX(0)` | `+0x19e3 JZ → +0x19f4` | **`[pShimData+0x210] ≠ 0`** ← test.exe 는 여기서 건너뜀 |
| `+0x19e5 MOV [RDI+0x2e0],RAX(0)` (PEB `AppCompatInfo` := 0) · `+0x19ec MOV RCX,R13` · **`+0x19ef CALL LdrpInitShimEngine` (ret `+0x19f4`)** | | 반환값을 안 본다 (`+0x19f4` 가 곧바로 다음 일) |

**`LdrpInitShimEngine(pShimData)`** (rva `0x89444`) 안 — 새 문지기는 없고 **실패하면 조용히 끝**:
- 이름은 **`apphelp.dll`** (UTF-16 `0x1785e8`, 길이 `0x16`): `+0x9f LdrpBuildSystem32FileName` (ret `+0xa4`, 실패 `+0xa6 JS → +0x215` 로그 `0xe63`) → `+0xb7 LdrpInitializeDllPath(0, 0x4001)` (ret `+0xbc`) → `+0xcd LdrpLoadDll` (ret `+0xd2`, 실패 `+0xdf JS → +0x19c` 로그 `0xe76`).
- 성공하면: `+0xed OR [항목+0x68],R14D (0x100)` (비트 8 `ShimDll`) · `+0xf5 MOV [g_pShimEngineModule],[항목+0x30]` · `+0xfc LdrpPinModule` (ret `+0x101`) · `+0x104 LdrpDereferenceModule` (ret `+0x109`).
- **`+0x109 LdrpGetShimEngineInterface`** (ret `+0x10e`, 실패 `+0x110 JS → +0x203` 로그 `0xe84`).
- `+0x116 ~ +0x15d` 에서 `g_pfnSE_InitializeEngine` 을 KUSD 쿠키 (`[0x7ffe0330]`) 로 풀고 `+0x160` 간접 호출 (ret `+0x165`) — 인자: `RCX` = 지역 구조체 `{dword 0x2000000, 이름 목록 버퍼 포인터}`, `RDX` = exe 항목 `FullDllName` (`[LdrpImageEntry]+0x48`), `R8` = `pShimData`. 음수면 `+0x167 JS → +0x1c7` 정리로.
- 성공하면 **`+0x16e LdrpLoadShimEngine(목록 포인터)`** (ret `+0x173`). 엔진이 목록 포인터를 힙 블록으로 바꿔 놨으면 `+0x195 RtlFreeHeap` 로 풂.

**`LdrpGetShimEngineInterface()`** (rva `0x89b0c`) — 부르는 곳이 **셋**: `LdrpInitShimEngine +0x109`, `LdrpGetProcApphelpCheckModule +0x285` (ret `+0x28a`), `LdrInitShimEngineDynamic +0xe4` (ret `+0xe9`).
- `g_pShimEngineModule` (apphelp) 에서 **내보내기 12개**를 `LdrGetProcedureAddressForCaller` 로 찾는다 (call `+0x98 · +0xe7 · +0x193 · +0x1e6 · +0x239 · +0x28c · +0x2df · +0x332 · +0x385 · +0x3d8 · +0x42b · +0x47e`): `SE_InitializeEngine` · `SE_ShimDllLoaded` · `SE_InstallBeforeInit` · `SE_InstallAfterInit` · `SE_DllUnloaded` · `SE_LdrEntryRemoved` · `SE_DllLoaded` · `SE_ProcessDying` · `SE_LdrResolveDllName` · `SE_GetProcAddressForCaller` · `ApphelpCheckModule` · `ApphelpQueryModSettingsAlloc` (글은 `.rdata` `0x17c138 ~ 0x17c230` 에서 읽음).
- 하나라도 못 찾으면 (호출마다 뒤에 `JS → +0xf2`): 로그 `0xcd5` → **`+0x129 MOV [g_ShimsEnabled],0`** → `+0x130 LdrUnloadDll` (ret `+0x135`) → `+0x135 MOV [g_pShimEngineModule],0` → 실패 상태 반환.
- 다 찾으면: `+0x48f LdrProtectMrdata` (ret `+0x494`) 로 `.mrdata` 를 열고 12칸에 **쿠키로 감춘** 주소를 씀 (`+0x4a9 g_pfnSE_InitializeEngine` … `+0x559 g_pfnApphelpQueryModSettingsAllocProc`), `+0x560` 로 다시 닫음.

**`LdrpLoadShimEngine(이름 목록)`** (rva `0x8a07c`) — **`g_ShimsEnabled = 1` 은 여기서 딱 한 줄 (`+0x148 MOV byte [g_ShimsEnabled],1`).**
- `+0x61 LdrpInitializeDllPath` (ret `+0x66`) · `+0x7e MOV [g_ShimsLoading],1` (`R14B` = 1).
- `+0x95 CMP word [RSI],0` / `+0x99 JNZ → +0x18a`: 목록 (`MULTI_SZ`) 이 비어 있지 않으면 이름마다 `+0x1c9 LdrpLoadDll` (ret `+0x1ce`) → `+0x1d9 BTS [항목+0x68],8` (`ShimDll`) · `+0x1de LdrpPinModule` · 노드 상태 7 이면 `+0x1f0 LdrpInitializeNode` (ret `+0x1f5`, **실패하면 `+0x2b0 LdrpInitializationFailure` + `+0x2bb NtTerminateProcess` — 프로세스 죽임**) · `+0x202` 간접 호출 `SE_ShimDllLoaded` (ret `+0x207`). 이름 하나 로드 실패 → 로그 `0xd1d`, 반환값 `R14B = 0`.
- 목록 끝: `+0xcb` 간접 호출 `SE_InstallBeforeInit` (ret `+0xd0`) → `+0xf8 RtlEnterCriticalSection(LdrpDllNotificationLock)` (ret `+0xfd`) → `+0xfd CMP [g_ShimsEnabled],0` / `+0x10b JNZ → +0x25d` (이미 켜졌으면 모든 모듈의 `+0x69` 비트 11 `ShimEngineCalloutSent` 를 `+0x266` 으로 지우고 `+0x111` 로) → `+0x111 CMP [LdrInitState],2` · `+0x11f JGE` — 모듈 목록 (`PebLdr` 로드 순서) 을 돌며 `+0x12d LdrpSendShimEngineInitialNotifications(항목.DdagNode, SE_DllLoaded)` (ret `+0x132`; `LdrInitState < 2` 이면 첫 모듈 (exe) 은 건너뜀) → `+0x141 MOV [g_ShimsLoading],0` → **`+0x148 g_ShimsEnabled = 1`** → `+0x154 LdrpInitializeShimDllDependencies` (ret `+0x159`) → `AL = R14B` 반환.

**`g_ShimsEnabled` 쓰는 줄 전수** (`_refs/g_ShimsEnabled.tsv`): **1 로 켜는 줄 = `LdrpLoadShimEngine +0x148` 하나.** 0 으로 끄는 줄 둘 = `LdrpGetShimEngineInterface +0x129` (인터페이스 못 얻음) · `LdrpInitializeProcess +0x1ceb` (`+0x1ca7 CMP [g_ShimsEnabled]` / `+0x1cae JZ +0x1d07` 로 켜져 있을 때만 들어가 `+0x1cdf` `SE_InstallAfterInit` 간접 호출 (ret `+0x1ce4`) 이 `AL == 0` 을 돌려줬을 때 — 그때 `LdrUnloadDll(g_pShimEngineModule)` `+0x1cf9` 도). 나머지 10줄은 읽기.

**shim 엔진 입구는 셋 — test.exe 는 셋 다 안 탄다**

| 입구 | 부르는 곳 | `g_ShimsEnabled` 를 켜나 | test.exe |
|---|---|---|---|
| ① 프로세스 시작 | `LdrpInitializeProcess +0x19ef` → `LdrpInitShimEngine` | **켠다** (`+0x16e → LdrpLoadShimEngine +0x148`) | **안 탐** (`+0x19e3 JZ`, `[pShimData+0x210] = 0`) |
| ② 나중에 (내보내기) | `LdrInitShimEngineDynamic` (ntdll 내보내기 ordinal 143, rva `0x8b1b0`) `+0x67 LdrpLoadShimEngine` (ret `+0x6c`) · `+0xe4 LdrpGetShimEngineInterface` | **켠다** (같은 `LdrpLoadShimEngine`) | **안 탐.** ntdll 안에 이 함수를 부르는 줄 0 (`ntdll.dis` 에 `call 0x18008b1b0` 0줄), kernel32·KernelBase 에도 이름 0 → 부르는 쪽은 다른 DLL (apphelp 같은) `[추측]`. 문지기: `+0x29 TEST word [TEB+0x17ee],0x1000` / `+0x30 JZ → +0xa2` (꺼져 있으면 `LdrpDrainWorkQueue` call `+0xa7`, ret `+0xac`) · `+0x42 CMP [g_pShimEngineModule],0` (0 이면 인자 모듈로 채움 `+0xdd`) |
| ③ apphelp 캐시 대답 | `LdrpGetProcApphelpCheckModule +0x285` (`+0x27e MOV [g_pShimEngineModule],RCX`) | **안 켠다** (인터페이스만 얻음) | **안 탐.** `CompatCachepLookupCdb` 대답 비트 `0x10` 일 때만 (sast/61, run66) — 사진 `g_pShimmedModuleList = 0` |

- test.exe 에서 이 칸들은 **파일 값 0 그대로** (사진 `끝까지 0`): `g_ShimsEnabled` · `g_ShimsLoading` · `g_pShimEngineModule` · `g_pfnSE_*` 12개 (sast/61 · sast/116). `+0x1ca7 CMP [g_ShimsEnabled],R12B(0)` / `+0x1cae JZ → +0x1d07` 이라 `SE_InstallAfterInit` 도 안 부름 (sast/116 의 `+0x1cd4` 줄과 같음).

**지금 우리 (PE_emulator): 맞음 (결과 같음, 갈래만 다름).** 우리는 PEB `+0x2d8` 을 0 으로 둬서 `+0x19da JZ → +0x19f4` (`Src/ntdll/LdrInit.cpp:5766 ~ 5767`), 진짜 VM 은 블록이 있고 `+0x210 = 0` 이라 `+0x19e3 JZ → +0x19f4`. 둘 다 `LdrpInitShimEngine` 을 안 부르고 `AppCompatInfo` 를 0 그대로 둔다. `:6689 ~ 6705` 가 `g_ShimsEnabled = 0` → `+0x1cae JZ` 도 같게 적었다. **갈리는 건 `SbUpdateSwitchContextBasedOnDll` 하나** (아래 Q127-c).

**baggage 줄**: 3.8 `g_ShimsEnabled` · `g_pShimEngineModule` · `g_pfnSE_*` 줄과 3.9 shim 함수 칸의 "**지도 경로 모름**" → `②… → LdrpInitializeProcess (호출 +0x19ef) → LdrpInitShimEngine (지도 깊이 5; 호출 +0x109 → LdrpGetShimEngineInterface, +0x16e → LdrpLoadShimEngine (깊이 6) → +0x148 g_ShimsEnabled = 1)`, 조건 `[pShimData+0x210] ≠ 0` (test.exe 안 탐, 3.1 `pShimData` 줄과 같은 근거). 다른 입구 둘 (`LdrInitShimEngineDynamic`, `LdrpGetProcApphelpCheckModule +0x285`) 은 이름만.

### (c) `SbUpdateSwitchContextBasedOnDll` 이 `SwitchBackContext` 를 쓰나 (근거만 옮김)

- sast/109 (`+0xd9`) · sast/116 이 이미 답했다. 이번에 `.asm` 로 다시 확인:
  - **쓴다. `SbUpdateSwitchContextBasedOnDll +0xd9 MOV [R14+0xc0],RDI`** (`R14 = RCX` = 모듈 항목 `+0xf`, `+0xc0` = `SwitchBackContext` PDB). `RDI = [RSP+0x88] + 4` (`+0xd5 LEA RDI,[RDX+4]`) — `SbpDetermineDllContext(항목.DllBase, &…)` (call `+0xc1`, ret `+0xc6`) 가 돌려준 호환 표 칸의 `+4`.
  - 쓰는 조건: `+0x32 JZ` (`SbGetCurrentSwitchContext` = 0 이면 반환) 아님 · `+0x47 CMP [ctx+8],0` / `+0x4a JZ → +0x2ec` 아님 (ctx 켜짐 칸 ≠ 0) · `+0xac TEST R8D,R8D` / `+0xaf JNZ → +0x148` 아님 (`LdrpSendPostSnapNotifications +0x9d XOR R8D,R8D` 가 0 을 줌) · `+0xc6 TEST EAX` / `+0xc8 JZ → +0x131` 아님.
  - ntdll 전체에서 **모듈 항목 `+0xc0` 을 쓰는 줄은 이것뿐** (`[reg+0xc0]` 쓰기 훑기: `LdrpSnapModule +0xefa` 는 `RCX` 인자 = import 문맥이고 모듈 항목 아님; `LdrpMapCleanModuleView +0xa8` · `AvrfMiniLoadDll +0x8f8` 두 줄은 구조체 정체를 안 따라감 `[공백: 낮음]`).
- 사진: 다섯 모듈 `SwitchBackContext` 값이 있음 (sast/109 `0x7ffafb7d3014` 등), `SbDynamicContextDllCount` 번호 0 칸 개수 P1a 0 → P1b·P2 3 (sast/111) 이 이 줄이 돈 증거.
- **지금 우리 (PE_emulator): 확인 필요 (sast/116 그대로).** 우리는 `pShimData` 를 0 으로 둬서 `SbGetCurrentSwitchContext` 가 0 → `+0x32 JZ` 로 `+0xd9` 를 안 탄다 → 모듈 `+0xc0` = 0, `SbDynamicContextDllCount+4` = 0 (사진은 값이 있음). 읽는 쪽 (호환 표 · `LdrLoadDll` 포인터 3칸) 이 있을 때만 영향. `Src` 에 `SbUpdateSwitchContextBasedOnDll` · `SwitchBackContext` 줄 0 (grep).
- baggage 줄: 7.2 `P1a~P1b` "`SwitchBackContext` (test·k32·kb·dll1·dll2)" 의 "쓰나" 가 **닫힘 — `+0xd9` 하나** `[정적]` sast109 · sast120.

---

## 동적으로 확인할 것 (선택 — 정적으로 거의 닫힘)

dast 요청 필요: **없음** (모두 선택). 하려면 test.exe 일 때만 걸리게 조건을 붙인다.

1. **TLS 디렉터리가 파일과 같은지** (Q124-c 의 가정): exe 모듈 기준 `dq <test.exe 기준>+0x79500 L5` → `0x…79f90 0x…79f91 0x…82ce0 0x…6e380 0x0010000000000000` 꼴이 나오는지 (끝 칸은 `SizeOfZeroFill`=0 · `Characteristics`=0x100000 한 qword). 같으면 `0x11` 확정.
2. **TLS 블록 크기** (선택): `ntdll!LdrpAllocateTls+0xfa` (RtlAllocateHeap 부르기 직전) 에서 `r r8` = `0x11`. 반환 뒤 `dq poi(@$teb+0x58) L1` (슬롯 0) 과 `dq <슬롯 0>-8 L1` (숨긴 포인터 = 슬롯 0 − 0x10).
3. **키 없음에서 +0x733 에 닿고 `RCX = 0` 인지** (Q124-b): `ntdll!LdrpQueryIllegalCWDDevices` 입구 `r rcx` = 0, 그 뒤 `dd ntdll!LdrpIllegalCWDDevices L1` = 0. 겸사 `dd 7ffe02d5 L1` 비트 4~5 = 0.
4. **섹션 크기** (Q124-d, 선택): `nt!PspSchedulerSharedDataRegionCreate+0x158` (MmCreateSectionEx 부르기 직전) 에서 `r r8` = `0x2000` (조건: 프로세스 이름 test.exe). 사진 VAD 2쪽은 이미 있음.
5. **shim 엔진 안 타는지** (Q127-b): 멈춤점이 **안 걸리면** 맞음 — `ntdll!LdrpInitShimEngine` · `ntdll!LdrpLoadShimEngine` · `ntdll!LdrInitShimEngineDynamic`. 걸리면 `LdrpInitializeProcess +0x19dc` 에서 `dd poi(@rsp+0x180)+0x210 L1` 을 보면 이유가 나온다 (`@rsp+0x180` = `pShimData` 사본, `+0x19dc` 시점 `R13` 과 같음).
6. **`+0xce` 한 줄만 비트 3 을 켜는지** (Q127-a, 선택): 모듈 하나 (예: kernel32) 항목 `+0x68` 에 쓰기 멈춤점 (`ba w4 <항목>+0x68`) → 비트 3 이 켜지는 순간의 RIP 가 `LdrpSendPostSnapNotifications+0xce` 인지. (사진이 이미 "다섯 모듈 켜짐" 을 보여 주므로 급하지 않음.)

추천 멈춤점 (한 줄씩): `ntdll!LdrpInitializeExecutionOptions+0x733` (CWD, `+0xad` 갈래 확인) · `ntdll!LdrpAllocateTls+0xfa` (TLS 크기) · `nt!PspSchedulerSharedDataRegionCreate+0x158` (섹션 크기) · `ntdll!LdrpInitializeProcess+0x19dc` (`+0x210`) · `ntdll!LdrpSendPostSnapNotifications+0xce` (비트 3).

## 다음 요청 후보

1. (정적, 중간) **`[pShimData+0x210]` 를 누가 쓰나** — sast/116 `[공백]` 그대로. 후보: 부모 쪽 kernelbase `BasepGetAppCompatData` 에서 apphelp (`ApphelpCheckRunApp*` · `SE_*`) 로 이어지는 길과 `LdrInitShimEngineDynamic` 의 호출자 (apphelp 쪽 `.asm` 이 필요 — 새 분석 아님, `ms/decomp/apphelp` 가 있으면 읽기). shim 이 걸린 exe 로 바뀌는 날의 문턱 (PE_emulator 쪽은 "test.exe 한정" 으로 둬도 됨).
2. (정적, 중간) 커널 `PspSchedulerSharedDataRegionSlotAllocate` · `…SlotFree` 본문 — 섹션 `0x2000` 안 표 한 칸의 크기, 스레드마다 나누는 법 (`_KTHREAD +0x320` · `+0x464 SchedulerSharedOffset`). sast/107 후보 1 그대로. `.asm` 이미 있음 (`PspSchedulerSharedDataRegionSlotAllocate.asm`). PE_emulator 가 클래스 `0x39` 를 기본 대답에서 진짜 값으로 바꿀 때 필요.
3. (정적, 작음) `LdrpHandleTlsData` DLL 쪽 TLS (kernel32 · KernelBase · dll1 · dll2 중 TLS 디렉터리가 있는 것) 의 `Characteristics`·`End−Start` 를 파일 바이트로 계산해서 DLL 블록 크기를 baggage 에 `[정적]` 로 (식은 이번에 확정). **필요한지는 오케스트레이터 판단** — test.exe 길 밖일 수 있음.
4. (사용자 결정 대기) **Q124 (e)** 그림 2 에 `LdrpInitializeProcess` 새 callee 6개 넣기 — 이번에도 안 건드림.

## 판정

**분석성공.** Q124 (b)·(c)·(d), Q127 (a)·(b) 다섯 질문 모두 줄 단위 근거로 답함 (이월 둘 (Q124-a · Q127-c) 은 근거를 `.asm` 줄로 다시 확인해서 옮김). 핵심: ① `+0x733` 은 IFEO 키 없음 갈래 (`+0xad → +0x604`) 에서도 `+0x71c` 에서 합쳐져 `RCX = 0` 으로 닿고 test.exe 값은 0 (KUSD 정책 0 때문), ② exe TLS 블록 `0x11`B = `(End−Start=1) + 정렬 16`, `SizeOfZeroFill` 은 안 읽음, ③ 스케줄러 공유 데이터 섹션 크기 = 커널 상수 `0x2000` (`PspSchedulerSharedDataRegionCreate +0x127`), ④ `LoadNotificationsSent` 를 켜는 줄은 `+0xce` 하나, ⑤ shim 엔진 길의 문지기는 `[pShimData+0x210]` 하나 (`g_ShimsEnabled = 1` 은 `LdrpLoadShimEngine +0x148` 한 줄, 입구 셋). 정적으로 못 정한 것 = `[pShimData+0x210]` 를 쓰는 쪽 (`[공백]`), VM 의 test.exe 가 `sample/bin/test.exe` 와 같은 바이트인지 (가정), `LdrInitShimEngineDynamic` 의 실제 호출자 `[추측]`. PDB 일치, 스크립트 문제 없음.

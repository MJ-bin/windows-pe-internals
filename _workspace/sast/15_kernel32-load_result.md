# 15 kernel32-load — 정적 분석 결과

- 요청서: `_workspace/sast/15_kernel32-load.md`
- 모듈 / 바이너리: ntdll (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/ntdll.dll`). 이미 분석된 프로젝트 사용 (analyze 안 함). ntdll 은 커널이 아니라 GUID 기준 확인 대상은 아님.
- 뽑은 함수: 20개 (정규식 `^(LdrpInitializeKernel32Functions|LdrLoadDll|LdrpLoadDll)$`, depth 1) → `ms/decomp/ntdll/`, `ms/disassemble/ntdll/`
  - 본문을 읽은 것: 요청한 세 함수. `LdrpFindLoadedDllInternal`·`LdrpCodeAuthzInitialize` 는 호출 인자·갈림 확인용으로 앞쪽만 훑음 (요청서 범위 밖 본문은 안 읽음, 아래에 어디까지 봤는지 적음).
- 새로 한 일: `refs` 8개 → `ms/disassemble/ntdll/_refs/{LdrpKnownDllDirectoryHandle,LdrpKnownDllPath,LdrpSystem32,RtlpSystem32Dirs,LdrpHashTable,LdrpHeap,LdrpSystemDllBase,LdrpAppHeaders}.tsv` (바이트 수: 8·0x10·0x10·0x10·0x200·8·8·8). 원본 바이트로 문자열·구조체 확인 (스크래치 파이썬, 커밋 안 함). PDB 타입은 `llvm-pdbutil-14 dump -types` 로 확인.
- 이름 확인: `pdb_aliases.py --where ntdll.dll LdrLoadDll` → `LdrLoadDll rva=0x3cbf0` 그대로 (별칭 없음).
- 버전 확인 (알려진 사실 대조): `LdrpLoadDll +0xfb` → ret `+0x100` `LdrpLoadDllInternal` **일치**. run7 관찰 (`LdrpInitializeKernel32Functions` ret `+0xc0`/`+0x156` `LdrLoadDll`, `LdrLoadDll` ret `+0x170` `LdrpLoadDll`) 도 `_calls.tsv` 와 **모두 일치**.
- 스크립트 문제: 없음. 다만 `.asm` 의 전역 주석 `Wow64EnvironmentThunkTable+0x78/+0x88/+0x98` 은 실제로는 **ANSI_STRING 세 개** (함수 이름 문자열) 이다. 가장 가까운 PDB 이름이 붙은 것이라 이름이 오해를 부른다. 문서에 이 이름을 쓰지 않는다 (사양대로 동작, 고칠 것 없음).

## 한눈에 (flow.md 2.5 에 붙일 트리, 라벨은 ret 자리)

```
LdrpInitializeProcess +0x19a2  → LdrpInitializeKernel32Functions(RCX = 출력 칸)
  ├ +0x54  memset            (0x110 바이트 지역 버퍼 비움)
  ├ +0x86  LdrpAppendUnicodeStringToFilenameBuffer   (버퍼에 "KERNEL32.DLL" 담기)
  ├ +0xa4  LdrpFindLoadedDllInternal(버퍼, 0, &항목, &x, 플래그 0x20)   ← 이름만으로 이미 올라왔나?
  │     ├ 음수(못 찾음) → +0xc0 LdrLoadDll(0x4001, 0, "KERNEL32.DLL", &베이스)
  │     │                    └ 안에서 +0x170 LdrpLoadDll → +0x100 LdrpLoadDllInternal   [run7 관찰]
  │     │                    (결과 0xc0000135 "DLL 없음" 이면 아래 kernel32 쪽 둘을 건너뛰고 +0x13b 로)
  │     └ 0 이상(찾음) → LdrLoadDll 건너뜀, 베이스 = 항목+0x30
  │            (그리고 지금 프로세스 매개변수 Flags 의 0x20000000 비트가 켜졌으면 아래 kernel32 쪽 둘도 건너뜀)
  ├ +0xff  LdrGetProcedureAddressForCaller(kernel32, "BaseThreadInitThunk", &Kernel32ThreadInitThunkFunction)  ← 음수면 로그 후 끝
  ├ +0x130 LdrGetProcedureAddressForCaller(kernel32, "TermsrvGetWindowsDirectoryW", RCX 출력 칸)   ← 결과 안 봄
  ├ +0x135 LdrpSnapKernelBaseExtensions   ← 음수면 (로그 없이) 끝
  ├ +0x156 LdrLoadDll(0x4001, 0, "KERNELBASE.dll", &베이스)    ← 음수면 로그 후 끝. 뒤에 +0x170 LdrpLoadDll [run7 관찰]
  ├ +0x165 LdrpCodeAuthzInitialize          ← 안에서 조건부로 LdrLoadDll("ADVAPI32.DLL") 가능 (레지스터 값 따라)
  ├ +0x19b LdrGetProcedureAddressForCaller(kernelbase, "KernelbasePostInit", &[RSP+0x58])
  └ +0x1af 간접 호출 (CFG 거침) = [RSP+0x58] = KernelbasePostInit      ← 인자 따로 안 싣는다. 음수면 로그 후 끝
  (끝: EBX = 마지막 상태를 돌려줌. 호출자 `+0x19a2` 는 음수면 로그(줄 0x1d57) 후 에러 끝으로)
```

## 질문별 답 [정적]

### 1. `LdrpInitializeKernel32Functions` 순서

근거 파일: `ms/disassemble/ntdll/LdrpInitializeKernel32Functions.asm`, `ms/decomp/ntdll/LdrpInitializeKernel32Functions.c`. 호출 15곳 (`memset`, `Append`, `FindLoaded`, `LdrLoadDll` ×2, `GetProc` ×3, `Snap`, `CodeAuthz`, 간접 1, `cookie`, `LdrpLogInternal` ×3) 이 요청서와 같다. 이름 문자열은 `.asm` 의 전역 주소를 원본 바이트에서 읽었다.

시간 순서 (ret 자리):

| ret | 호출 | 하는 일 |
|---|---|---|
| `+0x54` | `memset(&[RSP+0x60], 0, 0x110)` | 지역 버퍼 비움 |
| `+0x86` | `LdrpAppendUnicodeStringToFilenameBuffer` | 버퍼 `[RSP+0x60]` (머리 `0x1000000`, 안에 담을 자리 `[RSP+0x70]`) 에 전역 `LdrpKernel32DllName` 을 담음 (`+0x5d LEA R14,[0x1801730b0]`) |
| `+0xa4` | `LdrpFindLoadedDllInternal` | 아래 1-a |
| `+0xc0` | `LdrLoadDll` | 아래 1-b (못 찾았을 때만) |
| `+0xff` | `LdrGetProcedureAddressForCaller` | 1-c: `BaseThreadInitThunk` |
| `+0x130` | `LdrGetProcedureAddressForCaller` | 1-c: `TermsrvGetWindowsDirectoryW` |
| `+0x135` | `LdrpSnapKernelBaseExtensions` | 1-e |
| `+0x156` | `LdrLoadDll` | 1-b (KERNELBASE) |
| `+0x165` | `LdrpCodeAuthzInitialize` | 1-e |
| `+0x19b` | `LdrGetProcedureAddressForCaller` | 1-c: `KernelbasePostInit` |
| `+0x1af` | 간접 (`cfg:stack[-0x140]`) | 1-f |
| `+0x1c7` | `__security_check_cookie` | 끝 |
| `+0x25c`, `+0x28c`, `+0x2d6` | `LdrpLogInternal` | 실패 로그 (메시지 6종이 이 세 자리를 나눠 씀, 1-h) |

**1-a. `+0x9f LdrpFindLoadedDllInternal`**
- 답: `LdrpKernel32DllName` ("KERNEL32.DLL", 유니코드, 길이 0x18) 이 **이미 모듈 목록에 있는지** 이름으로만 찾는다.
  - 인자: `RCX` = 방금 만든 이름 버퍼, `RDX` = 0, `R8` = `&[RSP+0x50]` (찾은 항목 출력), `R9` = `&[RSP+0x48]`, 다섯째(스택) = `0x20` (`+0x8b`).
  - 안쪽 앞부분 (`LdrpFindLoadedDllInternal.asm` `+0x5c~+0x76`): 플래그 `0x20` 이 켜져 있으면 곧바로 `+0x76 LdrpFindLoadedDllByName(이름, 0, 플래그, 항목 출력, …)` 만 부르고 그 결과를 돌려준다. 경로를 풀어 보거나 `LdrpSearchPath` 로 가는 갈래(`+0x16c LdrpResolveDllName`, `+0x226 LdrpSearchPath`)는 `0x20` 이 꺼져 있을 때의 쪽이다. 즉 이 호출은 **이름만으로 목록(해시 표 `LdrpHashTable`)을 뒤지는 확인**이다. 이 안쪽은 이 요청서 범위 밖이라 앞 0x100 바이트만 훑었다.
- **찾으면 `+0xbb LdrLoadDll` 을 건너뛰나: 그렇다.** `+0xa4 TEST EAX,EAX; +0xa6 JNS +0x1ff` (찾음 = 0 이상). 갈림:
  - `+0x1ff`: `[RSP+0x50]` 의 항목에서 `+0x30` (`LDR_DATA_TABLE_ENTRY.DllBase`) 을 `[RSP+0x40]` (베이스 칸) 에 저장 → `PEB+0x20` (`ProcessParameters`) 의 `+8` (`Flags`) 을 읽어 `0x20000000` 비트 확인.
  - 그 비트 **꺼짐** → `+0xd1` (kernel32 쪽 `GetProc` 둘과 `Snap` 으로) / **켜짐** → `+0x13b` (곧바로 KERNELBASE 로, kernel32 쪽 `GetProc` 둘과 `Snap` 은 건너뜀). 이 비트가 무슨 뜻인지는 PDB 로 이름을 못 찾았다 `[추측: 이미 kernel32 가 올라와 있고 매개변수가 어떤 특수 모드일 때의 예외 길]`.
- 못 찾음 (음수) → `+0xac` 쪽으로 가서 `LdrLoadDll`.

**1-b. 두 `LdrLoadDll` 이 넘기는 DLL 이름 (원본 바이트로 확인)**

| 호출 | 이름 전역 (rva) | 문자열 | 인자 |
|---|---|---|---|
| `+0xbb` (ret `+0xc0`) | `LdrpKernel32DllName` (`0x1730b0`) | **`KERNEL32.DLL`** (대문자, 유니코드 12글자) | `RCX=0x4001` (`+0xb6`), `RDX=0`, `R8=이름`, `R9=&[RSP+0x40]` |
| `+0x151` (ret `+0x156`) | `LdrpKernelbaseDllName` (`0x173140`) | **`KERNELBASE.dll`** (대소문자 섞임, 14글자) | 같음 (`+0x14c ECX=0x4001`, `+0x142 EDX=0`) |

- `RCX=0x4001`: 낮은 비트 `1` = LdrLoadDll 이 첫째 인자를 "경로가 아니라 플래그"로 읽는 표시 (2번 참고). 나머지 `0x4000` 은 공개 문서의 `LOAD_LIBRARY_SEARCH_SYSTEM32_NO_FORWARDER` 값과 같다 `[추측: 이름은 공개 문서 기억, 이 ntdll 안에서 이름 확인은 못 함]`.
- 첫째 호출의 결과 처리: `+0xc2 CMP EAX,0xc0000135 (STATUS_DLL_NOT_FOUND 값) ; JZ +0x13b` → kernel32 쪽을 건너뛰고 KERNELBASE 로. 음수면 `+0xcb JS +0x291` → 로그(줄 `0x877`, "Loading Windows subsystem DLL ... failed") 후 끝. 0 이상이면 `+0xd1` 로.
- 둘째 호출: 음수면 `+0x15a JS +0x22c` → 로그(줄 `0x8cf`) 후 끝.
- **순서 주의**: KERNELBASE 를 "올리는" 호출은 kernel32 쪽 `GetProc` 둘 **뒤**에 있다. run7 이 `LdrpCallInitRoutine` 을 KERNELBASE → KERNEL32 순으로 본 것은 첫째 `LdrLoadDll(KERNEL32.DLL)` 안에서 일어난 일일 것이다 (kernel32 가 kernelbase 를 import 하므로) `[추측]`. 그러면 둘째 `LdrLoadDll(KERNELBASE.dll)` 은 이미 올라온 것을 다시 찾는 호출일 것이다 `[추측]` (`LdrpLoadDllInternal` 안쪽은 Q11). run7 은 이 둘째 호출에서 `LdrpLoadDll` 까지만 관찰했다고 요청서에 있다.

**1-c. `LdrGetProcedureAddressForCaller` 세 곳 (이름·결과 칸)**

인자 (x64): `RCX`=DLL 베이스, `RDX`=ANSI_STRING 주소, `R8`=0 (번호 없음), `R9`=결과 칸, 다섯째(`[RSP+0x20]`)=0, 여섯째(`[RSP+0x28]`)=`[RBP+0x98]` = **이 함수의 반환 주소** (= `LdrpInitializeProcess +0x19a2`, `Caller`).

| ret | `RDX` (rva) | 함수 이름 (원본 바이트) | `RCX` | 결과를 넣는 칸 | 결과 검사 |
|---|---|---|---|---|---|
| `+0xff` | `0x173ba8` | **`BaseThreadInitThunk`** (19) | kernel32 베이스 `[RSP+0x40]` | 전역 `Kernel32ThreadInitThunkFunction` (rva `0x1d2760`, `+0xe4`) | `+0x101 TEST; +0x103 JS +0x2a1` → 로그(줄 `0x88e`) 후 끝 |
| `+0x130` | `0x173bc8` | **`TermsrvGetWindowsDirectoryW`** (27) | kernel32 베이스 | **호출자가 준 출력 칸 `*RCX`** (`+0x11c MOV R9,RDI`, `RDI` = 입구 `RCX`) | **안 봄** (바로 `Snap` 호출, `+0x130` 뒤에 `TEST` 없음) |
| `+0x19b` | `0x173bb8` | **`KernelbasePostInit`** (18) | kernelbase 베이스 (`[RSP+0x40]` 이 `+0x156` 호출로 다시 채워짐) | 지역 `[RSP+0x58]` (`+0x179 LEA R9`) | `+0x19d TEST; +0x19f JS +0x261` → 로그(줄 `0x8f1`) 후 끝 |

- `+0x6c MOV [RDI],RSI(=0)` 로 입구에서 출력 칸 `*RCX` 를 먼저 0 으로 만든다.
- 그러므로 요청 질문의 "첫째 인자(`RCX` = 호출자 `[RSP+0x188]`) 에 무엇을 써 주나": **kernel32 의 `TermsrvGetWindowsDirectoryW` 함수 주소** (실패하면 0 그대로). 이 칸을 `LdrpInitializeProcess +0x1b72 LdrpInitializePerUserWindowsDirectory` 에 넘긴다 (sast/14). 그 함수가 이 주소로 무엇을 하는지는 이번 범위 밖 (`RtlpSystem32Dirs +8` 을 쓰는 코드가 `LdrpInitializePerUserWindowsDirectory +0x164` 에 있다는 것만 `refs` 로 보임).

**1-d. `Kernel32ThreadInitThunkFunction` 을 채우는 `+0xe4`**
- `+0xe4 LEA R9,[0x1801d2760]` 는 **`+0xfa` 호출 (`BaseThreadInitThunk` 찾기) 의 결과 칸** 이다. 값은 `LdrGetProcedureAddressForCaller` 가 그 칸에 써 준다. 이름 문자열이 `BaseThreadInitThunk` 이므로 **넣는 값은 KERNEL32 의 `BaseThreadInitThunk` 함수 주소**로 읽는 게 맞다 (`BaseThreadInitThunk` 가 kernel32 내보내기이고 이름이 같다는 점은 문자열로 확인, 주소 값 자체는 동적). 내보내기가 다른 DLL 로 넘기는 전달 항목(forwarder)이면 그 대상 주소일 수 있다 `[추측]`.
- 읽는 곳은 `LdrpInitializeProcess +0x1b45`·`RtlUserThreadStart +0x7` (`refs`, sast/14 와 같음).

**1-e. `+0x130 LdrpSnapKernelBaseExtensions`, `+0x160 LdrpCodeAuthzInitialize` 의 갈림 조건**
- `LdrpSnapKernelBaseExtensions` (ret `+0x135`): **kernel32 쪽 `+0xd1` 길로 온 경우에만 탄다**. 건너뛰는 경우: ① `LdrLoadDll(KERNEL32.DLL)` 이 `0xc0000135` 를 돌려줌, ② 이미 올라와 있고 프로세스 매개변수 `Flags & 0x20000000` 켜짐. 결과가 음수면 **로그 없이** 바로 끝 (`+0x137 TEST; +0x139 JS +0x1b9` (함수 끝 정리), `EBX` 반환).
- `LdrpCodeAuthzInitialize` (ret `+0x165`): KERNELBASE `LdrLoadDll` 가 0 이상일 때 **항상** 부른다 (조건은 호출 앞에 `+0x15a JS` 하나뿐). 결과 갈림 (`+0x165~+0x170`, `+0x1e5`):
  - 0 이상, 또는 정확히 `0xc0000135` → 계속 (`KernelbasePostInit` 찾기로).
  - 그 밖의 음수 → 로그(줄 `0x8df`) 후 끝.
- **`LdrpCodeAuthzInitialize` 안 (훑은 만큼, 요청서 범위 밖)**: 앞쪽에서 ① 전역 `LdrpIsSecureProcess != 0` 이면 0 반환(`+0x20~+0x37`), ② 이미지 `Subsystem`(`[NT헤더+0x5c]`)이 2·3 이 아니면 0 반환(`+0x5f~+0x6b`), ③ 레지스트리 키 세 개를 `NtOpenKey`·`NtQueryValueKey` 로 열어 값에 따라 **`+0x141 LdrLoadDll(0, 0, "ADVAPI32.DLL", …)`** 후 `SaferiIsDllAllowed` 를 찾아 전역 `LdrpAdvapi32DllHandle`·`LdrpSaferIsDllAllowedRoutine` 에 저장한다 (`+0x19e`, `+0x1a8`). 레지스트리 값이 없으면(`ESI==0`) 이 길은 안 탄다. **즉 kernel32 준비 중 ADVAPI32 가 올라올 수도 있는 코드가 있다. 올라오는지는 레지스트리 값에 달려 동적** (아래 동적 항목). 이름 문자열 `ADVAPI32.DLL`(rva `0x1731b0`), `SaferiIsDllAllowed`(rva `0x1731d0`)는 원본 바이트로 확인.

**1-f. `+0x1aa` 간접 호출 (`cfg:stack[-0x140]`)**
- `+0x1a5 MOV RAX,[RSP+0x58]` → `+0x1aa CALL [__guard_dispatch_icall]` (CFG 검사 거침). `[RSP+0x58]` 는 **`+0x196` 호출 (`KernelbasePostInit` 찾기) 이 `R9=&[RSP+0x58]` 로 채운 칸**이다 (`+0x179 LEA R9,[RSP+0x58]`, 입구 `+0x38` 에서 0 으로 시작). 그러므로 대상 = **KERNELBASE 의 `KernelbasePostInit`** 로 읽는 게 맞다 (이름 문자열 확인, 주소는 동적).
- 부르기 직전에 인자 레지스터를 **따로 싣지 않는다** (`+0x1a5` 와 `+0x1aa` 사이에 `RCX/RDX/R8` 쓰기 없음). 인자 없는 호출로 보인다 `[추측: 함수 안을 안 봄]`. 음수면 로그(줄 `0x8fa`, "Calling KernelbasePostInit failed") 후 끝.

**1-g. 첫째 인자 (`RCX`) 에 쓰는 것**: 1-c 의 둘째 줄에서 이미 답. 입구에서 `*RCX=0` (`+0x6c`), 그 뒤 `+0x130` 호출이 `TermsrvGetWindowsDirectoryW` 의 주소를 씀.

**1-h. 실패로 끝나는 길**
- **로그를 남기고 끝: 6곳** (메시지·줄 번호는 `LdrpLogInternal` 에 넘기는 `EDX`, 파일 `minkernel\ldr\ldrinit.c`):

| 줄 번호 | 조건 | 메시지 요지 |
|---|---|---|
| `0x877` | `LdrLoadDll(KERNEL32.DLL)` 가 음수이고 `0xc0000135` 가 아님 | Windows 서브시스템 DLL 로드 실패 |
| `0x88e` | `BaseThreadInitThunk` 찾기 음수 | 프로시저 찾기 실패 |
| `0x8cf` | `LdrLoadDll(KERNELBASE.dll)` 음수 | 서브시스템 DLL 로드 실패 |
| `0x8df` | `LdrpCodeAuthzInitialize` 가 `0xc0000135` 아닌 음수 | CodeAuthz 초기화 실패 |
| `0x8f1` | `KernelbasePostInit` 찾기 음수 | KernelbasePostInit 찾기 실패 |
| `0x8fa` | `KernelbasePostInit` 호출이 음수 | KernelbasePostInit 호출 실패 |

- **로그 없이 끝: 1곳**: `LdrpSnapKernelBaseExtensions` 음수 (`+0x139 JS +0x1b9`).
- 어느 경우든 마지막 상태 `EBX` 를 그대로 돌려주고, 호출자 `LdrpInitializeProcess +0x19a2~+0x19a8` 이 음수면 로그(줄 `0x1d57`) 후 에러 끝으로 간다 (`+0x19ba JMP`, `LdrpInitializeProcess.asm`).
- **`0xc0000135` 는 실패가 아니다**: 두 군데 (`+0xc2` kernel32 적재, `+0x1e5` CodeAuthz) 에서 일부러 무시한다 (적재가 안 되는 프로세스 조건 `[추측]`).
- 로그 함수 `LdrpLogInternal` 은 호출 자리 3곳(`+0x25c`, `+0x28c`, `+0x2d6`)만 있고 6가지 메시지가 이 셋으로 모인다.

### 2. `LdrLoadDll` (rva `0x3cbf0`, 호출 11곳 = `LdrpLoadDll` 1, `RtlReleasePath` 1, `LdrpDereferenceModule` 1, `LdrpLogDllStateEx2` 1, 쿠키 1, `LdrpLogInternal` 6)

근거: `ms/disassemble/ntdll/LdrLoadDll.asm`, `LdrLoadDll.c` (흐름은 `.asm` 로 다시 확인함).

- 인자: `RCX`=검색 경로 **또는 플래그 (낮은 비트 1 이면 플래그)**, `RDX`=`DllCharacteristics` 포인터 (또는 0), `R8`=이름 `UNICODE_STRING*`, `R9`=결과(베이스) 칸.
- **`+0x16b LdrpLoadDll` 앞에서 바꾸는 것 (ret `+0x170`, run7 관찰과 일치)**:
  1. 입구에서 지역 구조체 `[RSP+0x40 ~ +0xbf]` 를 전부 0 으로 만든다 (`+0x24~+0x5a`). 이후 `[RSP+0x40]` = 경로 칸, `[RSP+0x58]` = 플래그, `[RSP+0x60]` = 이름 버퍼 포인터, `[RSP+0xbc]` = "경로를 해제해야 함" 바이트.
  2. `RDX != 0` 이면 `*RDX` 의 비트를 **다른 비트로 옮겨 `EBX`(플래그)** 를 만든다 (`+0x64~+0xa5`): 비트2→`0x8`, 비트1→`0x40`, 비트23→`0x80`, 비트12→`0x100`, 최상위비트→`0x400000`. `RDX==0` (kernel32 준비 때) 이면 `EBX=0`.
  3. 로그 두 번 (`+0xd2`, `+0x101`: "DLL name: %wZ").
  4. **이름 버퍼 포인터** `RBP = [R8+8]` 를 `[RSP+0x60]` 에 저장 (`+0x141`, `+0x152`).
  5. **`RCX` 의 낮은 비트가 1 (플래그 방식, kernel32 준비는 이쪽)**: 비트를 지운 값(`0x4000`)을 `[RSP+0x58]` 에 저장, 경로 칸 `[RSP+0x40]` 은 **0 그대로** (`+0x14f`, `+0x157`). 경로를 풀어 주는 일은 안쪽 몫이다.
     **`RCX` 가 0 이 아니고 비트가 0 (경로 포인터)**: 로그("DLL search path passed in externally: %ws"), `[RSP+0x40]` = 그 포인터 (`+0x285~+0x2d0`). **`RCX==0`** (`LdrpCodeAuthzInitialize` 의 ADVAPI32 호출): 아무것도 안 채움.
  6. 호출: `RCX`=이름(원래 `R8`), `RDX`=`&[RSP+0x40]`, `R8D`=`EBX`, `R9`=`&[RSP+0x30]` (항목 출력).
- **앞에서 바로 돌려보내는 갈래 (kernel32 준비는 안 탐)**:
  - `LdrpPolicyBits` (전역) 의 비트 `0x4` 가 꺼짐 + `(RCX & 0x401)==0x401` → `0xc000000d` 를 돌려주고 끝 (`+0x106~+0x244`). `0x4001 & 0x401 = 0x1` 이므로 해당 없음.
  - `EBX` 비트 `0x8` 가 켜졌는데 `LdrpPolicyBits` 비트 `0x8` 가 꺼짐 → 로그(줄 `0x55b`) 후 `0xc00001aa`.
  - TEB `SameTebFlags (+0x17ee)` 비트 `0x2000` (PDB `LoaderWorker`) 가 켜짐 → `0xc000071c`. 로더 일꾼 스레드에서는 못 부르게 막는 것 `[추측]`.
  - 이 셋 모두 `LdrpLoadDll` 을 안 부르고 `+0x1a3` 로그로 간다.
- **뒤에서 하는 일**:
  - `+0x170 CMP byte [RSP+0xbc],0 ; JZ +0x18e`: **그 바이트가 0 이 아니면** `+0x189 RtlReleasePath([RSP+0x40])` (`LdrpLoadDll` 이 경로를 새로 만들어 줬을 때 해제). `LdrpLoadDll` 자체는 이 바이트를 안 건드린다 (`.asm` 확인) → 켜는 곳은 `LdrpLoadDllInternal` 쪽 `[추측]` (Q11).
  - `+0x18e TEST EBX,EBX ; JS +0x1a3`: **성공(0 이상)일 때만** 결과 `*R9 = 항목[+0x30]` (DllBase, `+0x197~+0x19b`) 을 쓰고 `+0x19e LdrpDereferenceModule(RCX=항목)` 을 부른다. 실패면 둘 다 건너뜀. (`LdrpLoadDllInternal` 쪽에서 올려 둔 임시 참조를 내려놓는 것 `[추측: 이름과 호출 자리로 짐작]`)
  - 그다음 로그 두 번 ("Status: 0x%08lx", 줄 `0x57d`·`0x57e`) 후 상태를 돌려준다.
- **kernel32 준비 때와 `LoadLibraryExW` 때 타는 길이 같은가**:
  - **같은 함수 본문**이다 (`LdrLoadDll` 은 ntdll 내보내기, 호출자 4곳: `LdrpInitializeKernel32Functions`, `LdrpCodeAuthzInitialize`, `RtlLogMessageInEventLogger`, `SbpResolveBasedOnName` — `LoadLibraryExW` 는 KERNELBASE 에서 부르니 이 표(ntdll 안 호출자)에 없다).
  - 갈림은 **인자**로 결정된다. kernel32 준비는 `RCX=0x4001` (플래그 방식, 경로 0), `RDX=0` (`DllCharacteristics` 없음). `LoadLibraryExW` 가 `RCX`·`RDX` 에 무엇을 싣는지는 KERNELBASE 쪽 코드라 이 정적 분석에서는 모른다 → **Q31 동적 또는 kernelbase 정적** `[확인 필요]`. 위의 갈래들 (플래그 변환, 경로 포인터, 정책 비트) 중 어느 것을 타는지가 거기서 달라진다 `[추측]`.

### 3. `LdrpLoadDll` (rva `0x1f9c0`, 호출 9곳)

근거: `ms/disassemble/ntdll/LdrpLoadDll.asm`.

- 인자: `RCX`=이름 `UNICODE_STRING*` (`R15`), `RDX`=`LdrLoadDll` 의 경로 구조체 (`RDI`), `R8D`=플래그 (`[RSP+0x58]`), `R9`=항목 출력 (`RSI`).
- 순서 (ret 자리):
  1. `+0x50 memset([RSP+0x72], 0, 0xfe)`.
  2. **앞 ETW 검사** (`+0x50~+0x95`): 아래 ETW 설명.
  3. `+0xa0~+0xbd`: 지역 버퍼 `[RSP+0x60]` 준비 (머리 `0x1000000`, 안에 담을 자리 `[RSP+0x70]`), `+0xbd LdrpPreprocessDllName(RCX=이름, RDX=&버퍼, R8=0, R9=&플래그)` → ret `+0xc2`, 결과 `EAX` 를 `[RSP+0x50]` 에 저장. 플래그가 안에서 바뀔 수 있다 (`R9` 가 `[RSP+0x58]` 주소). 음수면 `LdrpLoadDllInternal` 건너뛰고 `+0x100` 으로 (`+0xc8 JS`).
  4. `+0xfb LdrpLoadDllInternal` (ret `+0x100`, run7 관찰·알려진 사실과 일치).
  5. `+0x10f` (ret `+0x114`): 버퍼 포인터 `[RSP+0x68]` 가 안에 담을 자리 `[RSP+0x70]` 와 **다르면** (= 이름이 커서 힙으로 옮겨짐) 해제 호출. **이 호출의 대상은 별칭 아홉 개가 한 주소를 쓴다** (`pdb_aliases.py --lookup ntdll.dll 0x3d630`: `RtlpFtFree`, `RtlpCtFreeMemory`, `RtlDeleteBoundaryDescriptor`, `RtlpFreeAtom`, `IdnaMemFree`, `RtlpFreeEnvironmentHashTable`, `RtlpFreeEnvBlock`, `NtdllpFreeStringRoutine`, `RtlpSysVolFree`). 요청서·`.asm` 의 `RtlDeleteBoundaryDescriptor` 는 이 중 Ghidra 가 고른 하나일 뿐이고 **하는 일은 "메모리 해제" 쪽으로 읽힌다** `[추측: 이름들로 짐작]`. 문서에 `RtlDeleteBoundaryDescriptor` 라고 쓰지 않는다.
  6. `+0x114~+0x126`: 버퍼 머리 다시 초기화.
  7. **뒤 ETW 검사** (`+0x12c~+0x25a`).
  8. `+0x15c MOV EAX,[RSP+0x50]` → 반환.
- **`LdrpLoadDllInternal` 에 넘기는 인자 (9개)** (`+0xca~+0xfb`):

| 칸 | 값 |
|---|---|
| `RCX` | `&[RSP+0x60]` = 전처리된 이름 버퍼 (`LdrpPreprocessDllName` 이 채운 것) |
| `RDX` | `RDI` = `LdrLoadDll` 의 경로 구조체 (`&[RSP+0x40]`, 그대로) |
| `R8D` | `[RSP+0x58]` = 플래그 (전처리가 바꿨을 수 있음) |
| `R9D` | **`4`** (`+0xe3`) — PDB 열거 `_LDR_DLL_LOAD_REASON` 의 `LoadReasonDynamicLoad = 4` 와 같은 값 `[추측: 이 인자 자리가 "적재 이유"라는 것은 이름이 아니라 값이 같다는 것만 확인]` |
| `[RSP+0x20]` | 0 |
| `[RSP+0x28]` | 0 |
| `[RSP+0x30]` | `RSI` = 항목 출력 칸 |
| `[RSP+0x38]` | `&[RSP+0x50]` = **상태 출력 칸** (아래) |
| `[RSP+0x40]` | 0 |

  - 이름: `LdrpPreprocessDllName` 결과가 **같은 칸 `[RSP+0x50]`** 에 먼저 들어가고, 그 칸의 주소를 `LdrpLoadDllInternal` 에 넘긴다. `+0xfb` 호출 뒤에 `EAX` 를 읽는 코드가 없다 (`+0x100` 에서 곧바로 `RCX` 를 덮어씀). 그래서 **`LdrpLoadDll` 의 반환값은 `LdrpLoadDllInternal` 의 반환값이 아니라 그 안이 `[RSP+0x50]` 칸에 쓴 상태** 로 읽는다 `[정적]` (칸에 쓰는지는 Q11).
  - 결과 칸 `[RSP+0x30]`(LdrLoadDll)/`RSI`: `LdrpLoadDllInternal` 이 채운다.
- **ETW·`RtlGetCurrentServiceSessionId` 는 언제** (`+0x50~+0x95`, `+0x12c~+0x156`, `+0x1c6~+0x262`): 사실상 "로더 추적 켜짐" 검사다 `[추측: 이름과 PDB 칸 이름으로 짐작]`.
  - 기준 바이트 `RBX = 0x7ffe0384` (`KUSER_SHARED_DATA+0x384`, PDB 상 `UserModeGlobalLogger` 배열 안). 단 `PEB+0x90` (PDB `SharedData`) 가 0 이 아니고 그 첫 dword 가 0 이 아니면 `[PEB+0x90]+0x22a` 로 바꿔 읽는다 (`+0x58~+0x6e`, `+0x181~+0x1a0`).
  - 그 바이트가 0 이 **아니고** `PEB+0x378` (PDB `TracingFlags`, 비트 `0x4` = `LibLoaderTracingEnabled`) 가 켜졌을 때만 `+0x1c6 RtlGetCurrentServiceSessionId` 를 부른다.
  - 서비스 세션 ID 가 0 이면 `0x7ffe0385`, 아니면 `[PEB+0x90]+0x22b` 의 바이트에서 `0x20` 비트를 본다. 켜졌으면 **`LdrpLogEtwEvent(ECX=0x14a8, …, 이름)`** (앞), 뒤에도 같은 검사로 **`ECX=0x14a9`** (뒤).
  - 즉 test.exe 는 로더 추적이 꺼져 있으면 두 검사 모두 `RtlGetCurrentServiceSessionId` 도 안 부르고 지나가는 쪽일 것 `[추측: 값은 동적]`.
  - 위 호출 개수 `RtlGetCurrentServiceSessionId` ×2, `LdrpLogEtwEvent` ×2 와 일치.

### 4. run18 이 남긴 연결점 (`refs` 8개)

근거: `ms/disassemble/ntdll/_refs/<이름>.tsv`.

**이번 세 함수 (`LdrpInitializeKernel32Functions`, `LdrLoadDll`, `LdrpLoadDll`) 가 직접 읽는 것은 여덟 개 중 하나도 없다** (읽는 곳 목록에 세 함수가 안 나옴). 안쪽 callee 가 읽는다.

| 전역 (rva, 크기) | 쓰는 곳 | 읽는 함수 (`refs`) | 이번 세 함수에서 닿는 길 `[정적, 지도·refs 이름 기준]` |
|---|---|---|---|
| `LdrpKnownDllDirectoryHandle` (`0x1e7120`, 8) | `LdrpInitializeProcess +0xdb1`(DATA), `+0xe00`(READ) | `LdrpFindKnownDll +0x98`, `LdrGetKnownDllSectionHandle +0x9f` | `LdrpFindKnownDll` 의 호출자는 `LdrpLoadKnownDll +0x5e` (지도). 그 위는 `LdrpLoadDllInternal` 아래일 것 `[추측]` → Q11 |
| `LdrpKnownDllPath` (`0x1d2770`, 0x10) | `LdrpInitializeProcess +0xe64`, `+0xe6b`, `+0xe72` | `LdrpCheckKnownDllFullPath +0x4/+0x79/+0xa3`, `LdrpFindKnownDll +0x16c/+0x186` | 호출자 `LdrpLoadKnownDll +0x32` (지도) → Q11 |
| `LdrpSystem32` (`0x1d2990`, 0x10) | `LdrpInitializeProcess +0x10a6`, `+0x10b2` | `LdrpIsModuleUnderSystem32 +0xa/+0x49` | 그 함수의 호출자 `LdrpCfgProcessLoadConfig +0x67e` (지도) → Q11·Q12 (CFG 설정을 읽는 쪽) |
| `RtlpSystem32Dirs` (`0x1d2330`, 0x10) | `LdrpInitializeProcess +0x109e` (그리고 `+8` 은 `LdrpInitializePerUserWindowsDirectory +0x164`) | `RtlpComputePath +0x367/+0x3a1/+0x3b8`, `RtlpAddSystemPath +0x15/+0x33/+0x20` | 지도엔 호출 선이 없음 (이 지도 뿌리 `LdrInitializeThunk` 아래로 안 닿음). 검색 경로 계산 쪽 (`LdrpSearchPath`·`RtlGetSearchPath` 계열) `[추측]` → Q11 |
| `LdrpHashTable` (`0x1d30a0`, 0x200) | `LdrpInitializeProcess +0x3c9` (비움, 이전 결과), 항목 끼움은 `LdrpInsertDataTableEntry +0x2b/+0x3d/+0x7a` | `LdrpFindLoadedDllByName(+LockHeld) +0x4e/+0x18c`, `LdrpFindOrPrepareLoadingModule +0x131/+0x146`, `LdrpLoadDependentModuleInternal +0xb3f/+0xb4f` | **`LdrpFindLoadedDllInternal` 이 `+0x76`/`+0x11f`/`+0x18c` 에서 `LdrpFindLoadedDllByName` 을 부른다** (`_calls.tsv`). 즉 이번 `+0x9f` 확인이 이 표를 뒤진다. `LdrpLoadDllInternal +0x1ed (call)` 은 `LdrpFindOrPrepareLoadingModule` 을 부른다 (지도) |
| `LdrpHeap` (`0x1d2860`, 8) | `LdrpInitializeProcess +0xb12` | 54곳. 이번 길에서: `LdrGetProcedureAddressForCaller +0x638/+0xa47`, `LdrpSearchPath +0x4b3/+0x51d`, `LdrpFindOrPrepareLoadingModule` ×4, `LdrpAllocateModuleEntry` ×3, `LdrpAllocatePlaceHolder`, `LdrpMapAndSnapDependency`, `LdrpSnapModule`, `LdrpFastpthReloadedDll +0x18b`, `LdrpDereferenceModule +0xbb` … | `kernel32 준비의 GetProc 세 곳` 과 `+0x1af` 앞뒤의 `LdrpDereferenceModule` (LdrLoadDll `+0x19e` 가 부름) 이 읽는다 |
| `LdrpSystemDllBase` (`0x1d2970`, 8) | `LdrpInitializeProcess +0x68c` | `LdrGetProcedureAddressForCaller +0x1ca`, `LdrUnloadDll +0x26`, `LdrAddRefDll +0x2b`, `LdrpFindLoadedDllByHandle +0x28`, `LdrGetDllFullName +0x28`, `LdrResolveDelayLoadedAPI +0x85`, … | `LdrGetProcedureAddressForCaller` (이번 세 곳이 부름) 이 `+0x1ca` 에서 읽는다 → 이번 `+0xff/+0x130/+0x19b` 가 거친다. 값을 무엇과 비교하는지는 이 요청서 밖 |
| `LdrpAppHeaders` (`0x1d28f8`, 8) | `LdrpInitializeProcess +0x36b` | `LdrpMinimalMapModule +0x2e3`, `LdrpProcessMachineMismatch +0x4a` | `LdrpMinimalMapModule` 의 호출자 `LdrpMapDllWithSectionHandle +0x49` (지도) → Q11 |

- `LdrpLoadDllInternal` 자신과 `LdrpPreprocessDllName` 은 여덟 개 어디에도 **직접** 안 나온다. 모두 한두 단계 아래에서 읽는다.
- 요청서 질문 4 의 "읽는 함수 목록만" 은 위 표로 충분하다고 본다. 54곳짜리 `LdrpHeap` 전체 목록은 `_refs/LdrpHeap.tsv`.

### 5. kernel32 가 이미 올라와 있을 수 있나

- 정적으로 말할 수 있는 만큼:
  1. **`+0x9f` 확인은 이름만으로 `LdrpHashTable` 을 뒤지는 것** (1-a: 플래그 `0x20`, `LdrpFindLoadedDllByName`).
  2. 이 표에 끼우는 코드 `LdrpInsertDataTableEntry` 의 호출자는 4곳 (지도): `LdrpInitializeNtdllDataTableEntry +0xf2` (ntdll 자신), `LdrpInitializeProcess +0x1450` (exe 항목, sast/14), **`+0x1625`** (`AvrfAppVerifierMode == 0` 이면 통째로 건너뛰는 둘째 묶음 안, sast/14), `LdrpMapDllWithSectionHandle +0x141` (DLL 을 매핑한 뒤).
  3. run18·run19 로 `+0x199d` 앞에는 `LdrpLoadDll`·`LdrpMapDllWithSectionHandle` 계열을 부르는 호출이 없다 (sast/13, sast/14). 즉 **`+0x199d` 에 닿을 때 표에 들어 있는 것은 ntdll, exe, (검증기 켜졌을 때) 검증기 DLL 뿐** `[정적]` (값은 동적).
  4. 따라서 test.exe 에서는 `+0x9f` 가 "못 찾음 (음수)" 을 돌려줄 것이고 `+0xbb LdrLoadDll` 을 탄다 `[추측: 목록에 kernel32 가 없다는 것의 정적 결론, 실제 반환값은 동적]`. run7 이 `+0xc0` 을 실제 관찰했으므로 **이 추측은 이미 동적 관찰과 맞다.** flow.md 2.5 의 `[추측]` "`LdrpInitializeProcess` 가 시작될 때 kernel32·kernelbase 는 아직 없다" 는 정적으로 **지지된다** (앞쪽·뒤쪽에 올리는 호출이 없고, `+0x199d` 전에 표에 넣는 곳이 ntdll·exe 뿐).
  5. "이미 있는" 갈래(`+0x1ff`)가 코드에 있는 이유는 **kernel32 가 이미 올라온 프로세스** (예: exe 가 kernel32 자신이거나 exe 가 아닌 곳에서 먼저 올림) 를 위한 것으로 보인다 `[추측]`. 그때는 `LdrLoadDll(KERNEL32.DLL)` 이 건너뛰어진다.
- flow.md 2.5 `[추측]` "exe 가 무엇을 import 하든 상관없이 ntdll 이 kernel32 를 스스로 불러온다": 조건은 `+0x199d` 쪽의 이미지 `Subsystem`==2 또는 3 (sast/14) 뿐이고 이 함수 안에 exe 의 import 표를 보는 코드는 없다 → **정적으로 지지된다** (이번 함수 본문에서 import 를 읽는 호출은 안 보임).

## 그림 2 `(정적)` 상자 후보 (2.5, 트리와 같은 순서)

1. **`LdrpInitializeKernel32Functions` → `LdrLoadDll("KERNEL32.DLL")`** : 이유 — 이름이 아직 목록에 없으니 (`LdrpFindLoadedDllInternal` 이름 검색) 가장 먼저 kernel32 를 올리는 호출 (`+0xc0`), run7 과 일치.
2. **`BaseThreadInitThunk` 찾기 → `Kernel32ThreadInitThunkFunction`** : 이유 — 새 스레드 시작과 프로세스 초기화가 나중에 읽을 전역을 채우는 지점 (`+0xff`).
3. **`LdrLoadDll("KERNELBASE.dll")`** : 이유 — 둘째 적재 호출 (`+0x156`), 이미 올라온 것을 다시 찾는지 동적 확인 대상.
4. **`KernelbasePostInit` 간접 호출** : 이유 — kernel32 준비의 마지막 일이고 KERNELBASE 쪽 초기화를 한 번 더 부름 (`+0x1af`).

(`TermsrvGetWindowsDirectoryW` 찾기 `+0x130` 과 `LdrpCodeAuthzInitialize` `+0x165` 는 트리에는 있고 상자까지는 안 만들어도 된다.)

## 동적분석으로 확인해야 할 것

번호는 plan.md 의 기존 항목에 이어서.

- **Q20·Q21·Q22 (kernel32·kernelbase 적재·`LdrpCallInitRoutine`) 와 겹침**: 첫 `LdrLoadDll(KERNEL32.DLL)` 안쪽 (`+0xc0` 안). 이 요청서 범위 밖이라 기존 번호에 맡긴다.
- **Q48 (`LdrpInitializeProcess` 뒤쪽 간접 호출) 에 더할 것**: `+0x1b59` 가 읽는 `Kernel32ThreadInitThunkFunction` 이 **`kernel32!BaseThreadInitThunk` 주소인지** (`ln @rax`). 값은 이 문서의 `+0xff` 호출 뒤에 칸이 채워진 뒤에 본다.
- **새로 제안 (Q10 동적 후보)**: 이 함수의 `+0xa4` 반환값이 "못 찾음 (음수)" 인지, `+0xc0` `EBX` 가 0 인지, 그리고 아래 값들.

추천 멈춤점 (모두 `ntdll+rva`, test.exe 일 때만 — 조건은 dast 가 붙임):

| 멈춤점 | rva (ret 자리) | 볼 것 |
|---|---|---|
| `LdrpInitializeKernel32Functions` ret `+0xa4` | `0xb80c8` | `eax` (음수=못 찾음) |
| ret `+0xc0` | `0xb80e4` | `eax`, `[rsp+0x40]` (kernel32 베이스) |
| ret `+0xff` | `0xb8123` | `eax`, `Kernel32ThreadInitThunkFunction` 값 (`ntdll+0x1d2760`) → `ln` 으로 이름 |
| ret `+0x130` | `0xb8154` | 호출자 출력 칸 `*[rsp+0x188 (호출자 기준)]`, 곧 이 함수 `RDI` 의 칸 → `ln` |
| ret `+0x135` | `0xb8159` | `eax` (Snap 결과) |
| ret `+0x156` | `0xb817a` | `eax`, 베이스가 kernel32 베이스 근처인지 (KERNELBASE) |
| ret `+0x165` | `0xb8189` | `eax` (CodeAuthz), 그 사이에 **`LdrLoadDll` 입구 `ntdll+0x3cbf0`** 이 `ADVAPI32.DLL` 로 불렸는지 (`r8` 의 UNICODE_STRING) |
| ret `+0x19b` 와 call `+0x1aa` | `0xb81bf`, call 자리 `0xb81ce` | `[rsp+0x58]` 와 `rax` → `ln` = `KERNELBASE!KernelbasePostInit` |
| `LdrLoadDll` ret `+0x170` | `0x3cd60` | `eax`, `[rsp+0xbc]` (경로 해제 바이트), `[rsp+0x40]` (경로 칸), `[rsp+0x58]` (`0x4000`) |
| `LdrpLoadDll` ret `+0x100` | `0x1fac0` | `[rsp+0x50]` (상태 칸) |

주의: `+0x1aa` 는 `CALL 0x180172020` (CFG 검사 thunk, 5바이트) 이라 ret 은 `+0x1af`. 진짜 대상은 호출 직전 `rax` (`+0x1a5 MOV RAX,[RSP+0x58]`) 에 있다.

추가 제안: `LdrLoadDll` 을 `r8` 의 이름 문자열로 가려내서 몇 번 불리는지 (KERNEL32, KERNELBASE, 혹시 ADVAPI32) 세는 한 번의 로그. 이 요청서 정적으로는 `LdrpCodeAuthzInitialize` 의 ADVAPI32 길이 열릴 가능성만 말할 수 있다.

## Q11 ~ Q13 에 넘길 연결점 (한 줄씩)

- **Q11**: `LdrpLoadDll +0xfb` 가 `LdrpLoadDllInternal` 에 넘기는 9개 인자는 위 표 그대로 (`RCX`=전처리 이름 버퍼, `RDX`=경로 구조체(`LdrLoadDll` 의 `[RSP+0x40]`), `R8D`=플래그, `R9D`=4, `[RSP+0x30]`=항목 칸, `[RSP+0x38]`=상태 칸). 지도의 `LdrpLoadDllInternal` 안 호출: `(call) +0xb3 LdrpFastpthReloadedDll`, `+0x1ed LdrpFindOrPrepareLoadingModule`, `+0x27f LdrpPrepareModuleForExecution` 등. 먼저 `LdrpFastpthReloadedDll`·`LdrpFindOrPrepareLoadingModule` 이 `LdrpHashTable`·`LdrpHeap` 을 읽는다는 것이 이번 `refs` 로 나옴.
- **Q11**: `LdrLoadDll` 이 만든 경로 구조체 (`[RSP+0x40]` 경로, `[RSP+0x58]` 플래그 0x4000, `[RSP+0x60]` 이름 포인터, `[RSP+0xbc]` 해제 바이트) 는 `LdrpLoadDllInternal`/`LdrpLoadDll` 에서 어디서 채워지고 쓰이나 → `LdrpInitializeDllPath` 의 로그 문자열이 `LdrLoadDll` 에 인라인된 것으로 보임 (로그 줄 `0x610`).
- **Q11·Q12**: 위 4번 표의 `LdrpKnownDllDirectoryHandle`·`LdrpKnownDllPath` 읽는 곳 `LdrpFindKnownDll`/`LdrpCheckKnownDllFullPath`/`LdrpLoadKnownDll`, `LdrpSystem32` 읽는 `LdrpIsModuleUnderSystem32` (CFG 쪽), `LdrpAppHeaders` 읽는 `LdrpMinimalMapModule`/`LdrpProcessMachineMismatch`.
- **Q13**: KERNELBASE·KERNEL32 시작 함수 이름 확인은 이번 정적에서 하지 않음. 다만 `LdrLoadDll(KERNEL32.DLL)` 이 부른 `LdrpCallInitRoutine` 두 번이 KERNELBASE → KERNEL32 순서 (run7) 이고, 이번 코드는 kernel32 쪽 `BaseThreadInitThunk`·`TermsrvGetWindowsDirectoryW`, kernelbase 쪽 `KernelbasePostInit` 세 이름을 찾는다는 것이 새로 알게 된 이름.
- **(추가) Q48 연결**: `Kernel32ThreadInitThunkFunction` 의 값은 `LdrpInitializeKernel32Functions +0xff` 뒤에 채워진다. 그 값을 `LdrpInitializeProcess +0x1b59` 가 `(1, 0, 0)` 으로 부른다.

## 판정

**분석성공.** 요청 질문 5개 모두 `.asm` 의 `+0x` 줄 근거와 함께 답했다 (DLL 이름 3개, 함수 이름 3개, 실패 7곳 포함). 버전 대조도 `_calls.tsv` 가 run7 값과 전부 일치. 갈래의 값 (찾음/못 찾음, ADVAPI32 적재 여부, 플래그 `0x20000000`, 로더 추적 비트) 은 코드에 쓰인 만큼만이고 동적으로 확인할 것으로 넘겼다.

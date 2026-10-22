# 2026-10-23 run20 — `LdrpInitializeKernel32Functions`: 이미 있나 찾기 → `LdrLoadDll("KERNEL32.DLL")` → `BaseThreadInitThunk` 찾기 → `LdrLoadDll("KERNELBASE.dll")` → `KernelbasePostInit` 부르기 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntdll (`ntdll.dll`, 26100.9457), pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (run17 ~ run19 와 같은 파일)
  - analyze 는 다시 안 했다. 이미 분석된 프로젝트에서 세 함수를 depth 1 로 뽑았다 (20개).
  - 이름 확인: `pdb_aliases.py --where ntdll.dll LdrLoadDll` → `LdrLoadDll rva=0x3cbf0` 그대로 (별칭 없음).
  - 버전 대조: **일치.** `_calls.tsv` 의 `LdrpInitializeKernel32Functions +0xbb → ret +0xc0 LdrLoadDll`, `+0x151 → ret +0x156 LdrLoadDll`, `LdrLoadDll +0x16b → ret +0x170 LdrpLoadDll`, `LdrpLoadDll +0xfb → ret +0x100 LdrpLoadDllInternal` 이 run7 콜스택 값과 모두 같다.
- 근거로 읽은 것
  - 본문: `LdrpInitializeKernel32Functions` (rva `0xb8024`, 748바이트), `LdrLoadDll` (rva `0x3cbf0`, 729바이트), `LdrpLoadDll` (rva `0x1f9c0`, 611바이트).
  - 갈림 확인용으로 앞쪽만 훑음: `LdrpFindLoadedDllInternal` (앞 0x100 바이트), `LdrpCodeAuthzInitialize` (앞쪽 갈림과 ADVAPI32 길).
  - DLL·함수 이름 문자열은 `.asm` 의 전역 주소를 원본 바이트에서 읽었다 (결과서).
  - 전역 변수 참조(`refs`) 8개 → `ms/disassemble/ntdll/_refs/{LdrpKnownDllDirectoryHandle,LdrpKnownDllPath,LdrpSystem32,RtlpSystem32Dirs,LdrpHashTable,LdrpHeap,LdrpSystemDllBase,LdrpAppHeaders}.tsv`

## 목적

- 대기열 **Q10**. flow.md 2.5 의 `LdrpInitializeKernel32Functions` 가 **무엇을 어떤 순서로** 하는지, 관찰된 두 `LdrLoadDll` 이 **각각 어느 DLL** 을 올리는지, `Kernel32ThreadInitThunkFunction` 칸에 **무엇을** 넣는지.
- 덤으로: `LdrLoadDll`·`LdrpLoadDll` 이 인자를 어떻게 바꿔 `LdrpLoadDllInternal` 로 넘기나, run18 이 준비한 전역 8개를 누가 읽나, "시작 때 kernel32 는 없다" `[추측]` 을 코드로 말할 수 있나.

## 방법

- 정적: `_workspace/sast/15_kernel32-load.md` — ntdll, `^(LdrpInitializeKernel32Functions|LdrLoadDll|LdrpLoadDll)$` depth 1 (+ 전역 refs 8개)
- 결과서: `_workspace/sast/15_kernel32-load_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntdll/{LdrpInitializeKernel32Functions,LdrLoadDll,LdrpLoadDll}.asm`
  - `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_refs/*.tsv`
- 오프셋 읽는 법 (run9 ~ 19 와 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**.
- 호출 수: 15 · 11 · 9곳 (`_calls.tsv`), 요청서와 같음.

## 결과

한 줄로: **kernel32 준비는 "주소록에 KERNEL32 가 있나 보고, 없으면 들여오고, 거기서 함수 주소 두 개를 적어 두고, KERNELBASE 도 들여와 마지막 인사(`KernelbasePostInit`)를 부르는 일"이다.**

- 비유: 새 집에 전화기(kernel32)를 들이고, 전화번호부에서 "스레드 시작 담당"(`BaseThreadInitThunk`) 번호를 적어 두는 것. 그 번호는 나중에 새 스레드가 시작할 때마다 쓴다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeKernel32Functions` | `+0x4f` | `+0x54` | `memset` (0x110 바이트 지역 버퍼) | [정적] |
| 〃 | `+0x81` | `+0x86` | `LdrpAppendUnicodeStringToFilenameBuffer` (버퍼에 `KERNEL32.DLL`) | [정적] |
| 〃 | `+0x9f` | `+0xa4` | `LdrpFindLoadedDllInternal` (이름만으로 이미 있나) | [정적] |
| 〃 | `+0xbb` | `+0xc0` | `LdrLoadDll` (`KERNEL32.DLL`, 못 찾았을 때만) | [정적] + [26100.9457 확인] run7 |
| 〃 | `+0xfa` | `+0xff` | `LdrGetProcedureAddressForCaller` (`BaseThreadInitThunk`) | [정적] |
| 〃 | `+0x12b` | `+0x130` | `LdrGetProcedureAddressForCaller` (`TermsrvGetWindowsDirectoryW`) | [정적] |
| 〃 | `+0x130` | `+0x135` | `LdrpSnapKernelBaseExtensions` | [정적] |
| 〃 | `+0x151` | `+0x156` | `LdrLoadDll` (`KERNELBASE.dll`) | [정적] + [26100.9457 확인] run7 |
| 〃 | `+0x160` | `+0x165` | `LdrpCodeAuthzInitialize` | [정적] |
| 〃 | `+0x196` | `+0x19b` | `LdrGetProcedureAddressForCaller` (`KernelbasePostInit`) | [정적] |
| 〃 | `+0x1aa` | `+0x1af` | 간접 (CFG 검사 thunk 거침, 대상 = `[RSP+0x58]`) | [정적] |
| `ntdll!LdrLoadDll` | `+0x16b` | `+0x170` | `LdrpLoadDll` | [정적] + [26100.9457 확인] run7 |
| 〃 | `+0x189` | `+0x18e` | `RtlReleasePath` (조건부) | [정적] |
| 〃 | `+0x19e` | `+0x1a3` | `LdrpDereferenceModule` (성공일 때만) | [정적] |
| `ntdll!LdrpLoadDll` | `+0xbd` | `+0xc2` | `LdrpPreprocessDllName` | [정적] |
| 〃 | `+0xfb` | `+0x100` | `LdrpLoadDllInternal` | [정적] + [26100.9457 확인] run7 |
| 〃 | `+0x10f` | `+0x114` | 메모리 해제 쪽 함수 (이름 버퍼가 힙으로 옮겨졌을 때만) | [정적] |

- 잡일: `LdrpInitializeKernel32Functions` 에 `__security_check_cookie` 1 + `LdrpLogInternal` 3, `LdrLoadDll` 에 쿠키 1 + `LdrpLogInternal` 6 + `LdrpLogDllStateEx2` 1, `LdrpLoadDll` 에 `memset` 1 + 쿠키 1 + `RtlGetCurrentServiceSessionId` 2 + `LdrpLogEtwEvent` 2.
- 이름 주의 (`_calls.tsv` 의 `alias:` 1곳): `LdrpLoadDll +0x10f` (ret `+0x114`) = 메모리 해제 쪽 함수 `[추측: 이름들로 짐작]`. 같은 주소(rva `0x3d630`) 이름 9개, 후보: `RtlpFtFree`, `RtlpCtFreeMemory`, `RtlDeleteBoundaryDescriptor`, `RtlpFreeAtom`, `IdnaMemFree`, `RtlpFreeEnvironmentHashTable`, `RtlpFreeEnvBlock`, `NtdllpFreeStringRoutine`, `RtlpSysVolFree`.
- `+0x1aa` 는 `CALL` 5바이트(CFG 검사 thunk)라 ret 이 `+0x1af` 다. 진짜 대상은 바로 앞 `+0x1a5 MOV RAX,[RSP+0x58]` 의 `rax`.

### 정적 — `LdrpInitializeKernel32Functions` 순서와 갈림

| 순서 | ret | 하는 일 | 갈림 | 표시 |
|---|---|---|---|---|
| 0 | (입구) `+0x6c` | 호출자가 준 출력 칸 `*RCX` 를 0 으로 | — | [정적] |
| 1 | `+0x86` | 지역 버퍼에 전역 `LdrpKernel32DllName` 을 담음 (`+0x5d`). 문자열 = **`KERNEL32.DLL`** (대문자, 유니코드 12글자) | — | [정적] |
| 2 | `+0xa4` | `LdrpFindLoadedDllInternal(버퍼, 0, &항목, &x, 0x20)` (`+0x8b` 다섯째 = `0x20`). 플래그 `0x20` 이면 안에서 `+0x76 LdrpFindLoadedDllByName` 만 부른다 → **이름만으로 모듈 목록(해시 표 `LdrpHashTable`)을 뒤지는 확인** | `+0xa6 JNS +0x1ff`: 찾음(0 이상) → `LdrLoadDll` 건너뜀 / 못 찾음 → 3 | [정적] |
| 2' | (찾음 길) `+0x1ff ~ +0x227` | 베이스 = 항목 `+0x30` (`DllBase`). 그리고 `PEB.ProcessParameters.Flags` 의 `0x20000000` 비트 확인 (`+0x21a`) | 꺼짐 → 4 로 (`+0xd1`) / 켜짐 → 7 로 (`+0x13b`, kernel32 쪽 둘과 6 을 건너뜀). 비트 이름은 PDB 로 못 찾음 | [정적] |
| 3 | **`+0xc0`** | **`LdrLoadDll(0x4001, 0, "KERNEL32.DLL", &베이스)`** (`+0xb6 ECX=0x4001`, 결과 `[RSP+0x40]`) | `+0xc2` 결과가 `0xc0000135` 면 → 7 로 (`+0x13b`, kernel32 쪽 건너뜀). 그 밖의 음수 → 로그 후 끝. 0 이상 → 4 | [정적] + [26100.9457 확인] run7 (자리) |
| 4 | `+0xff` | **`LdrGetProcedureAddressForCaller(kernel32, "BaseThreadInitThunk", 0, &Kernel32ThreadInitThunkFunction, 0, 호출자 주소)`** — 결과 칸 주소를 `+0xe4 LEA R9` 로 실음 | 음수 → 로그 후 끝 (`+0x103`) | [정적] |
| 5 | `+0x130` | `LdrGetProcedureAddressForCaller(kernel32, "TermsrvGetWindowsDirectoryW", …, 결과 = 호출자 출력 칸 *RCX)` (`+0x11c MOV R9,RDI`) | 결과를 **안 본다** | [정적] |
| 6 | `+0x135` | `LdrpSnapKernelBaseExtensions` | 음수 → **로그 없이** 끝 (`+0x139`) | [정적] |
| 7 | **`+0x156`** | **`LdrLoadDll(0x4001, 0, "KERNELBASE.dll", &베이스)`** (전역 `LdrpKernelbaseDllName`, 대소문자 섞인 14글자). 베이스 칸 `[RSP+0x40]` 을 다시 채움 | 음수 → 로그 후 끝 (`+0x15a`) | [정적] + [26100.9457 확인] run7 (자리) |
| 8 | `+0x165` | `LdrpCodeAuthzInitialize` — 7 이 성공하면 **항상** 부름 | 0 이상 또는 정확히 `0xc0000135` → 계속 (`+0x1e5`). 그 밖의 음수 → 로그 후 끝 | [정적] |
| 9 | `+0x19b` | `LdrGetProcedureAddressForCaller(kernelbase, "KernelbasePostInit", …, &[RSP+0x58])` (`+0x179`) | 음수 → 로그 후 끝 (`+0x19f`) | [정적] |
| 10 | `+0x1af` | **간접 호출: `[RSP+0x58]` = 9 가 찾은 `KernelbasePostInit` 주소.** 부르기 직전 인자 레지스터를 따로 싣지 않는다 | 음수 → 로그 후 끝 | [정적]. 인자 없는 함수 `[추측: 안을 안 봄]` |
| 끝 | `+0x1c7` | 마지막 상태(`EBX`) 반환. 호출자 `LdrpInitializeProcess +0x19a2` 는 음수면 로그 후 에러 끝 | — | [정적] |

- `LdrGetProcedureAddressForCaller` 세 곳의 공통 인자: `RCX` = DLL 베이스, `RDX` = 이름 ANSI 문자열, `R8` = 0 (번호 아님), `R9` = 결과 칸, 다섯째 = 0, 여섯째 = **이 함수의 돌아갈 주소** (`LdrpInitializeProcess +0x19a2`, 이름대로 "부른 곳"). [정적]
- **`RCX=0x4001`**: 낮은 비트 1 = `LdrLoadDll` 이 첫째 인자를 "경로가 아니라 플래그"로 읽는 표시 (아래 `LdrLoadDll` 표). 남은 `0x4000` 은 공개 문서의 `LOAD_LIBRARY_SEARCH_SYSTEM32_NO_FORWARDER` 값과 같다 `[추측: 이름은 공개 문서 기억]`. [정적]
- **`0xc0000135` (`STATUS_DLL_NOT_FOUND` 값) 는 일부러 실패로 안 친다**: 3 (kernel32 적재) 과 8 (CodeAuthz) 두 곳. kernel32 가 없는 프로세스 조건을 위한 길로 보인다 `[추측]`. [정적]
- **실패로 끝나는 길 7곳** [정적]:
  - 로그 남기고 끝 6곳 (로그 함수 호출 자리는 `+0x25c`·`+0x28c`·`+0x2d6` 셋을 나눠 씀, 파일 `minkernel\ldr\ldrinit.c`): KERNEL32 적재 실패(줄 `0x877`), `BaseThreadInitThunk` 찾기 실패(`0x88e`), KERNELBASE 적재 실패(`0x8cf`), CodeAuthz 실패(`0x8df`), `KernelbasePostInit` 찾기 실패(`0x8f1`), `KernelbasePostInit` 호출 실패(`0x8fa`).
  - 로그 없이 끝 1곳: `LdrpSnapKernelBaseExtensions` 음수.

### 정적 — `LdrpCodeAuthzInitialize` 안 (앞쪽만 훑음)

- 바로 0 을 돌려주는 경우: 전역 `LdrpIsSecureProcess != 0` (`+0x20 ~ +0x37`), 또는 이미지 `Subsystem` 이 2·3 이 아님 (`+0x5f ~ +0x6b`). [정적]
- 그 밖엔 **레지스트리 키 세 개**를 `NtOpenKey`·`NtQueryValueKey` 로 열어, **값에 따라** `+0x141 LdrLoadDll(0, 0, "ADVAPI32.DLL", …)` 을 부르고 `SaferiIsDllAllowed` 를 찾아 전역 `LdrpAdvapi32DllHandle`·`LdrpSaferIsDllAllowedRoutine` 에 넣는다 (`+0x19e`, `+0x1a8`). 레지스트리 값이 없으면(`ESI==0`) 이 길은 안 탄다. [정적]
- 즉 **kernel32 준비 중에 ADVAPI32 가 올라올 수도 있는 코드가 있다.** 실제로 올라오는지는 이 VM 의 레지스트리 값에 달렸다 → 동적 (Q49).

### 정적 — `LdrLoadDll` (rva `0x3cbf0`)

| 단계 | 자리 | 하는 일 | kernel32 준비 때 | 표시 |
|---|---|---|---|---|
| 인자 | 입구 | `RCX` = 검색 경로 **또는 플래그** (낮은 비트 1 이면 플래그), `RDX` = `DllCharacteristics` 포인터, `R8` = 이름, `R9` = 베이스 결과 칸 | `RCX=0x4001`, `RDX=0` | [정적] |
| 지역 구조체 | `+0x24 ~ +0x5a` | `[RSP+0x40 ~ +0xbf]` 를 0 으로. `+0x40` 경로 칸, `+0x58` 플래그, `+0x60` 이름 버퍼 포인터, `+0xbc` "경로 해제 필요" 바이트 | 그대로 | [정적] |
| 플래그 바꾸기 | `+0x64 ~ +0xa5` | `*RDX` 의 비트를 다른 비트로 옮겨 `EBX` 를 만듦 (비트2→`0x8`, 비트1→`0x40`, 비트23→`0x80`, 비트12→`0x100`, 최상위→`0x400000`) | `RDX=0` → `EBX=0` | [정적] |
| 앞에서 돌려보내기 3갈래 | `+0x106 ~ +0x244` 등 | ① 전역 `LdrpPolicyBits` 비트 `0x4` 꺼짐 + `(RCX & 0x401)==0x401` → `0xc000000d`, ② `EBX & 0x8` 인데 정책 비트 `0x8` 꺼짐 → `0xc00001aa`, ③ TEB `SameTebFlags` 비트 `0x2000` (PDB `LoaderWorker`) → `0xc000071c` | 셋 다 안 탐 (`0x4001 & 0x401 = 0x1`) | [정적]. ③ 은 로더 일꾼 스레드에서 못 부르게 막는 것 `[추측]` |
| 경로 칸 | `+0x14f`, `+0x157` / `+0x285 ~ +0x2d0` | 플래그 방식이면 비트 지운 값(`0x4000`)을 `+0x58` 에, 경로 칸은 **0 그대로**. 경로 포인터면 로그 후 경로 칸에. `RCX==0` 이면 아무것도 안 채움 | 플래그 방식, 경로 0 | [정적] |
| **`LdrpLoadDll`** | `+0x16b` / **`+0x170`** | `(이름, &[RSP+0x40], EBX, &[RSP+0x30])` | 탄다 | [정적] + [26100.9457 확인] run7 |
| 경로 해제 | `+0x189` / `+0x18e` | `[RSP+0xbc] != 0` 일 때만 `RtlReleasePath([RSP+0x40])`. `LdrpLoadDll` 은 이 바이트를 안 건드림 → 켜는 곳은 더 안쪽 `[추측]` (Q11) | 모름 | [정적] |
| 결과 | `+0x18e ~ +0x19e` / `+0x1a3` | 성공(0 이상)일 때만 `*R9 = 항목+0x30` (`DllBase`) 쓰고 `LdrpDereferenceModule(항목)`. 안쪽이 올려 둔 임시 참조를 내려놓는 것 `[추측: 이름]` | 성공이면 탐 | [정적] |

- **kernel32 준비 때와 `LoadLibraryExW` 때 같은 본문인가**: 같은 함수다. 갈림은 **인자**(`RCX`·`RDX`)로만 정해진다. `LoadLibraryExW` 가 무엇을 싣는지는 KERNELBASE 코드라 이번엔 모른다 → 동적 Q31 또는 kernelbase 정적. [정적] + `[확인 필요]`
- ntdll 안에서 `LdrLoadDll` 을 부르는 곳 4곳 (지도): `LdrpInitializeKernel32Functions`, `LdrpCodeAuthzInitialize`, `RtlLogMessageInEventLogger`, `SbpResolveBasedOnName`. [정적]

### 정적 — `LdrpLoadDll` (rva `0x1f9c0`)

- 순서: `+0x50 memset` → 앞 ETW 검사 → 이름 버퍼 준비 → **`+0xc2 LdrpPreprocessDllName(이름, &버퍼, 0, &플래그)`** (결과를 상태 칸 `[RSP+0x50]` 에, 음수면 다음 호출 건너뜀) → **`+0x100 LdrpLoadDllInternal`** → (이름이 힙으로 옮겨졌으면) `+0x114` 해제 → 뒤 ETW 검사 → `[RSP+0x50]` 반환. [정적]
- **`LdrpLoadDllInternal` 에 넘기는 인자 9개** (`+0xca ~ +0xfb`) [정적]:

| 칸 | 값 |
|---|---|
| `RCX` | 전처리된 이름 버퍼 (`&[RSP+0x60]`) |
| `RDX` | `LdrLoadDll` 의 경로 구조체 (`&[RSP+0x40]`, 그대로) |
| `R8D` | 플래그 (전처리가 바꿨을 수 있음) |
| `R9D` | `4` — PDB 열거 `_LDR_DLL_LOAD_REASON` 의 `LoadReasonDynamicLoad` 값과 같다 `[추측: 자리 뜻은 값이 같다는 것만]` |
| `[RSP+0x20]`, `[RSP+0x28]`, `[RSP+0x40]` | 0 |
| `[RSP+0x30]` | 항목 출력 칸 |
| `[RSP+0x38]` | **상태 출력 칸** `&[RSP+0x50]` |

- `+0x100` 뒤에 `EAX` 를 읽지 않는다. 그래서 **`LdrpLoadDll` 이 돌려주는 값은 `LdrpLoadDllInternal` 의 반환값이 아니라, 그 안이 상태 칸에 쓴 값**이다. [정적] (칸에 실제로 쓰는지는 Q11)
- ETW·`RtlGetCurrentServiceSessionId` 는 "로더 추적이 켜졌을 때만" [정적]: 기준 바이트 `KUSER_SHARED_DATA+0x384` (PEB `SharedData` 가 있으면 그쪽 `+0x22a`) 가 0 이 아니고 `PEB.TracingFlags`(+0x378) 비트 `0x4` (PDB `LibLoaderTracingEnabled`) 가 켜졌을 때만 세션 ID 를 묻고 `LdrpLogEtwEvent(0x14a8 앞 / 0x14a9 뒤)`. test.exe 는 추적이 꺼져 있으면 그냥 지나갈 것 `[추측: 값은 동적]`.

### 정적 — run18 이 남긴 전역 8개를 읽는 곳 (`refs`)

**이번 세 함수가 직접 읽는 것은 하나도 없다.** 모두 한두 단계 아래에서 읽는다. [정적]

| 전역 | 쓰는 곳 (run18) | 읽는 함수 (`refs`) | 이번 길과 닿는 곳 |
|---|---|---|---|
| `LdrpKnownDllDirectoryHandle` | `LdrpInitializeProcess +0xdb1`, `+0xe00` | `LdrpFindKnownDll +0x98`, `LdrGetKnownDllSectionHandle +0x9f` | `LdrpFindKnownDll` ← `LdrpLoadKnownDll +0x5e` (지도). 그 위는 `LdrpLoadDllInternal` 아래일 것 `[추측]` → Q11 |
| `LdrpKnownDllPath` | `+0xe64`, `+0xe6b`, `+0xe72` | `LdrpCheckKnownDllFullPath`, `LdrpFindKnownDll +0x16c/+0x186` | `LdrpLoadKnownDll +0x32` (지도) → Q11 |
| `LdrpSystem32` | `+0x10a6`, `+0x10b2` | `LdrpIsModuleUnderSystem32 +0xa/+0x49` | ← `LdrpCfgProcessLoadConfig +0x67e` (CFG 쪽) → Q11·Q12 |
| `RtlpSystem32Dirs` | `+0x109e` (`+8` 은 `LdrpInitializePerUserWindowsDirectory +0x164`) | `RtlpComputePath`, `RtlpAddSystemPath` | 지도에 호출 선 없음. 검색 경로 계산 쪽 `[추측]` → Q11 |
| `LdrpHashTable` | `+0x3c9` (비움), 끼우기 `LdrpInsertDataTableEntry` | `LdrpFindLoadedDllByName(+LockHeld)`, `LdrpFindOrPrepareLoadingModule`, `LdrpLoadDependentModuleInternal` | **이번 `+0xa4` 확인이 이 표를 뒤진다** (`LdrpFindLoadedDllInternal +0x76` → `LdrpFindLoadedDllByName`) |
| `LdrpHeap` | `+0xb12` | 54곳. 이번 길: `LdrGetProcedureAddressForCaller`, `LdrpSearchPath`, `LdrpFindOrPrepareLoadingModule`, `LdrpAllocateModuleEntry`, `LdrpDereferenceModule +0xbb` 등 | 세 `GetProc` 와 `LdrLoadDll +0x19e` 아래 |
| `LdrpSystemDllBase` | `+0x68c` | `LdrGetProcedureAddressForCaller +0x1ca` 외 여럿 | 이번 `GetProc` 세 곳이 거친다 |
| `LdrpAppHeaders` | `+0x36b` | `LdrpMinimalMapModule +0x2e3`, `LdrpProcessMachineMismatch +0x4a` | `LdrpMinimalMapModule` ← `LdrpMapDllWithSectionHandle +0x49` → Q11 |

### 정적 — "시작 때 kernel32 는 아직 없다" (flow.md 2.5 `[추측]`)

1. `+0xa4` 확인은 **이름만으로 `LdrpHashTable` 을 뒤지는 것**이다 (플래그 `0x20`). [정적]
2. 그 표에 항목을 끼우는 `LdrpInsertDataTableEntry` 를 부르는 곳은 4곳 (지도): ntdll 자신(`LdrpInitializeNtdllDataTableEntry +0xf2`), exe 항목(`LdrpInitializeProcess +0x1450`), AppVerifier 묶음 안(`+0x1625`, 검증기 꺼지면 통째로 안 탐), DLL 을 매핑한 뒤(`LdrpMapDllWithSectionHandle +0x141`). [정적]
3. run18·run19 로 `+0x199d` 앞엔 DLL 을 매핑하는 호출이 없다 (WOW64·.NET 길 빼고). 그래서 **`+0x199d` 에 닿을 때 표엔 ntdll·exe (와 검증기가 켜졌으면 그쪽) 뿐**이다. [정적] (실제 값은 동적)
4. 그러니 test.exe 에서 `+0xa4` 는 "못 찾음"이고 `+0xc0 LdrLoadDll` 을 탄다 `[추측: 정적 결론, 반환값은 동적 Q49]`. run7 이 `+0xc0` 을 실제로 봤으니 **관찰과도 맞다.**
5. "이미 있음" 갈래(`+0x1ff`)는 kernel32 가 먼저 올라온 프로세스를 위한 길로 보인다 `[추측]`.

- "exe import 와 상관없이 스스로 불러온다" `[추측]`: 이 함수 본문에 exe import 표를 읽는 호출이 없고, 이름은 고정 전역(`LdrpKernel32DllName`·`LdrpKernelbaseDllName`)이다. 부르는 조건은 run19 의 `Subsystem` 2·3 뿐. → **코드로 맞다.** [정적]

### 호출 관계

```
LdrpInitializeProcess +0x19a2   (Subsystem 2/3 일 때, run19)
 └─ LdrpInitializeKernel32Functions (0xb8024)       ← RCX = 출력 칸 (입구에서 0)
      ├─ (1) +0x86   LdrpAppendUnicodeStringToFilenameBuffer   "KERNEL32.DLL"
      ├─ (2) +0xa4   LdrpFindLoadedDllInternal (플래그 0x20)   이름만으로 이미 있나
      │      찾음 → +0x1ff (베이스 = 항목+0x30, Flags 0x20000000 켜졌으면 (7) 로)
      ├─ (3) +0xc0   LdrLoadDll(0x4001, 0, "KERNEL32.DLL")      못 찾았을 때만   [관찰] run7
      │     └─ +0x170  LdrpLoadDll
      │          ├─ +0xc2   LdrpPreprocessDllName
      │          └─ +0x100  LdrpLoadDllInternal(이름, 경로 구조체, 플래그, 4, …, 항목 칸, 상태 칸)   [관찰] run7
      │      (0xc0000135 이면 (4)~(6) 건너뛰고 (7) 로)
      ├─ (4) +0xff   LdrGetProcedureAddressForCaller  "BaseThreadInitThunk"  → 전역 Kernel32ThreadInitThunkFunction
      ├─ (5) +0x130  LdrGetProcedureAddressForCaller  "TermsrvGetWindowsDirectoryW" → 출력 칸 *RCX (결과 안 봄)
      ├─ (6) +0x135  LdrpSnapKernelBaseExtensions
      ├─ (7) +0x156  LdrLoadDll(0x4001, 0, "KERNELBASE.dll")                     [관찰] run7
      │     └─ +0x170  LdrpLoadDll
      ├─ (8) +0x165  LdrpCodeAuthzInitialize          레지스트리 값에 따라 안에서 LdrLoadDll("ADVAPI32.DLL")
      ├─ (9) +0x19b  LdrGetProcedureAddressForCaller  "KernelbasePostInit" → [RSP+0x58]
      └─ (10) +0x1af (간접, CFG) [RSP+0x58] = KernelbasePostInit
```

## 확인된 것

- **두 `LdrLoadDll` 의 DLL 이름**: 첫째(ret `+0xc0`) = `KERNEL32.DLL`, 둘째(ret `+0x156`) = `KERNELBASE.dll`. 둘 다 `RCX=0x4001`, `RDX=0`. [정적] 원본 바이트 (결과서 1-b) + 자리는 [관찰] run7
- **첫째 `LdrLoadDll` 은 "이름으로 이미 있나" 확인(`+0xa4`)이 못 찾았을 때만 탄다.** [정적] `+0xa6 JNS`
- **`Kernel32ThreadInitThunkFunction` 칸 = kernel32 에서 이름 `BaseThreadInitThunk` 로 찾은 주소.** `+0xe4` 는 칸 주소를 싣는 줄이고, 값은 `+0xfa` 호출(ret `+0xff`)이 써 준다. 읽는 곳은 `LdrpInitializeProcess +0x1b45`(읽기, 부르기는 `+0x1b59`)·`RtlUserThreadStart +0x7`. [정적] `refs`. 실제 주소 값(전달 항목이면 다른 곳일 수 있음 `[추측]`)은 동적 Q48.
- **호출자 출력 칸(`LdrpInitializeProcess` 의 `[RSP+0x188]`) = kernel32 의 `TermsrvGetWindowsDirectoryW` 주소** (실패면 0). 이 칸이 `+0x1b72 LdrpInitializePerUserWindowsDirectory` 로 간다 (run19). [정적]
- **마지막 일은 KERNELBASE 의 `KernelbasePostInit` 을 이름으로 찾아 간접 호출하는 것** (`+0x19b` → `+0x1af`). [정적]
- **KERNELBASE 를 이름으로 올리는 호출은 kernel32 쪽 두 `GetProc` 뒤에 있다.** run7 이 KERNELBASE·KERNEL32 시작 함수를 둘 다 첫째 `LdrLoadDll` 안에서 본 것과 합치면, kernelbase 는 첫째 호출 안에서 kernel32 의 import 로 따라 올라오고, 둘째 호출은 이미 있는 것을 다시 찾는 것일 것 `[추측]` (Q11·Q20).
- **`LdrLoadDll`·`LdrpLoadDll` 이 `LdrpLoadDllInternal` 에 넘기는 것**: 전처리 이름, `LdrLoadDll` 의 경로 구조체(kernel32 준비 땐 경로 0, 플래그 `0x4000`), 플래그, `4`, 항목 칸, 상태 칸. `LdrpLoadDll` 의 반환값은 상태 칸 값. [정적]
- **run18 전역 8개는 이번 세 함수가 직접 안 읽는다.** `LdrpHashTable` 만 이번 `+0xa4` 확인이 한 단계 아래에서 뒤진다. [정적] `refs`
- **flow.md 2.5 `[추측]` 두 개가 코드로 지지된다**: "exe import 와 상관없이 스스로 불러온다" (이름 고정, import 안 봄), "시작 때 kernel32 는 아직 없다" (`+0x199d` 전에 표에 들어가는 건 ntdll·exe 뿐). [정적]. 실행 값은 동적 Q49.

## 아직 확인 필요

- 동적 → **Q49** 이 함수 실행 값 (주소 = `ntdll+rva`, test.exe 만):
  - ret `+0xa4` (`ntdll+0xb80c8`) `r eax` — 음수(= kernel32 아직 없음)인가
  - ret `+0xc0` (`ntdll+0xb80e4`) `r eax`, `dq @rsp+0x40 L1` — kernel32 베이스
  - ret `+0x130` (`ntdll+0xb8154`) 출력 칸 값 `ln` — `TermsrvGetWindowsDirectoryW` 인가
  - ret `+0x135` (`ntdll+0xb8159`), ret `+0x156` (`ntdll+0xb817a`), ret `+0x165` (`ntdll+0xb8189`) `r eax`
  - call `+0x1aa` (`ntdll+0xb81ce`) `ln @rax` — `KERNELBASE!KernelbasePostInit` 인가
  - `LdrLoadDll` 입구 `ntdll+0x3cbf0` 에서 `dS @r8` 로 몇 번, 어떤 이름 (KERNEL32, KERNELBASE, **ADVAPI32 가 끼는지**)
  - `LdrLoadDll` ret `+0x170` (`ntdll+0x3cd60`) `[rsp+0xbc]`·`[rsp+0x40]`·`[rsp+0x58]`, `LdrpLoadDll` ret `+0x100` (`ntdll+0x1fac0`) `[rsp+0x50]`
- 동적 → **Q48** `Kernel32ThreadInitThunkFunction` 값: ret `+0xff` (`ntdll+0xb8123`) 뒤 `dq ntdll+0x1d2760 L1` → `ln`, 그리고 `LdrpInitializeProcess +0x1b59` `ln @rax`.
- 동적 → **Q20** 첫째 `LdrLoadDll(KERNEL32.DLL)` 안에서 kernelbase 가 올라오는 순간 (둘째 `LdrLoadDll(KERNELBASE.dll)` 은 다시 찾기인지).
- 정적 → **Q11** `LdrpLoadDllInternal` 의 인자 9개 (위 표), `[RSP+0xbc]` 경로 해제 바이트를 켜는 곳, 상태 칸에 쓰는지. 전역 읽는 함수: `LdrpFindKnownDll`/`LdrpLoadKnownDll`/`LdrpCheckKnownDllFullPath` (KnownDlls), `LdrpFindOrPrepareLoadingModule` (`LdrpHashTable`), `LdrpMinimalMapModule` (`LdrpAppHeaders`), `LdrpIsModuleUnderSystem32` (CFG 쪽, Q12 와 같이).
- 정적 → **Q13** KERNELBASE·KERNEL32 시작 함수 이름. 이번에 새로 나온 이름: kernel32 `BaseThreadInitThunk`·`TermsrvGetWindowsDirectoryW`, kernelbase `KernelbasePostInit`.
- 남은 `[추측]`: `PEB.ProcessParameters.Flags` `0x20000000` 비트의 이름·뜻, `0x4000` 플래그 이름, `R9D=4` 가 "적재 이유" 인지, `LdrpSnapKernelBaseExtensions`·`KernelbasePostInit` 이 하는 일, `LoadLibraryExW` 가 `LdrLoadDll` 에 싣는 인자 (Q31).

## 디버거 메모 / 도구 메모

- 도구 문제 없음 (결과서 "스크립트 문제: 없음").
- **`.asm` 의 전역 주석 이름이 오해를 부르는 곳**: `LdrpInitializeKernel32Functions` 의 이름 문자열 세 개(`BaseThreadInitThunk` 등)는 PDB 이름이 없는 ANSI 문자열이라, 주석엔 가장 가까운 다른 PDB 이름 + 오프셋이 붙는다. 그 주석 이름은 문서에 쓰지 않았다. 문자열은 원본 바이트로 읽은 것.
- `+0x1aa` (CFG 검사 thunk 거치는 `CALL`, 5바이트) 는 ret `+0x1af`. 대상은 호출 직전 `rax` 에서 본다.
- `alias:` 1곳 (`LdrpLoadDll +0x10f`) 은 별칭 규칙대로 하는 일로 적었다.

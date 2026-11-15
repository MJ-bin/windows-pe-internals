# 95 rpc-caller — 정적 분석 결과

- 요청서: `_workspace/sast/95_rpc-caller.md` (대기열 Q180c)
- 모듈 / 바이너리: rpcrt4 (`ms/bin/.../System32/rpcrt4.dll`), PDB `rpcrt4.pdb` guid_age = `0204A8A137C42B7DF1F0811A3A8277B21` (`ms/sym/rpcrt4.dll.pdb.info.txt`)
- analyze: **이번에 처음 함** (`fetch_pdb.py` → `pdb_aliases.py` → `run_ghidra.sh analyze rpcrt4.dll`, 약 1분, exit 0). 확인: `.gpr` + `ms/decomp/rpcrt4/_index.tsv` (3289 함수) + analyze 로그 `SetPdb:` 줄 (266행) + `SCRIPT ERROR` 0건.
- 지도: `ms/disassemble/rpcrt4/_map/UuidToStringA_*` (기준 20) 와 `PerformRpcInitialization_*` (기준 1000, 공용 도구로 안 거르고 펼침 — `PerformRpcInitialization` 은 caller 140곳이라 기준 20 에선 안 펼쳐짐).
- 뽑은 함수 (`ms/decomp/rpcrt4/`, `ms/disassemble/rpcrt4/`):
  - depth 1: `^(RpcpNormalizeSecurityDescriptor|InitializeDefaultSecurityDescriptor|UuidToStringA)$`
  - depth 0: `PerformRpcInitialization` `GenerateSectionName` `AllocateCellSection` `InitializeCellHeap` `InitializeEPMapperClient` `ReadPolicySettings` `InitializeRpcProtocolLrpc` `TransformLrpcEndpointToAlpcPortName` `AlpcConnect`
  - 문자열·함수 `refs`: `ms/disassemble/rpcrt4/_refs/` (`0xeb868.tsv` = `\RPC Control\`, `0xef3b8.tsv` = `\RPC Control\DSEC`, `0xe96e8.tsv` = `NtOpenDirectoryObject` 가져오기 칸, `0xe96e0.tsv` = `NtQuerySecurityObject` 가져오기 칸, `PerformRpcInitialization.tsv`, `RpcpNormalizeSecurityDescriptor.tsv`, `InitializeDefaultSecurityDescriptor.tsv`)
- **범위를 넓힌 곳 (읽기 전용 export, ntdll·kernelbase 는 이미 analyze 된 것)**:
  - ntdll `RtlGetAppContainerNamedObjectPath` (rpcrt4 가 부르는 것, 질문 1 의 `NtQueryInformationToken(-6,0x1d,4)` 가 여기 안에 있다), `RtlNewSecurityObjectEx` + `RtlpNewSecurityObject` (depth 1) + `RtlpGetDefaultsSubjectContext` + `RtlpGetDefaultTrustSubjectContext` (질문 2·4·5: rpcrt4 는 SD 를 직접 안 읽고 이 함수에 통째로 넘긴다).
  - kernelbase `OpenThreadToken` `OpenProcessToken` (얇은 껍데기, 질문 4·5).
  - `RtlpNewSecurityObject` (7338 B) 는 **앞쪽 + syscall 이 걸린 자리만** 봤다. 본문 전체는 안 따라감.
- 버전 확인: **rpcrt4 는 "알려진 사실" 표에 줄이 없어 직접 대조는 못 함.** 대신 (1) 같은 `ms/bin` 폴더의 ntdll `_calls.tsv` 가 알려진 사실과 일치 (`LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess`), (2) PDB 는 `fetch_pdb.py` 가 바이너리의 CodeView GUID 로 받은 것, (3) rpcrt4 `.asm` 의 `ret=` 와 `_calls.tsv` 의 `ret_off` 가 같고 call 직전 명령·점프 목적지 셈이 맞음. 일치.
- 스크립트 문제: 없음.
- 쓰기 범위: rpcrt4 · ntdll · kernelbase 의 export·map·refs 출력만. smss (sast/94) 쪽 파일·Ghidra 프로젝트는 안 건드림 (analyze 시작 때 Ghidra 프로세스 0개, 메모리 19 GB 여유 확인).

## 한눈에

- `\RPC Control` 을 `NtOpenDirectoryObject` 로 여는 rpcrt4 함수는 **`RpcpNormalizeSecurityDescriptor` 하나뿐** (가져오기 칸 `0xe96e8` 참조 1곳 `+0x12a`).
- 목적은 "RPC 기본 보안 설명자 (`gDefaultSecurityDescriptor`) 만들기": `\RPC Control` 디렉터리의 SD 를 **부모 SD** 로 빌려서 `RtlNewSecurityObjectEx` 로 새 SD 를 만든다. rpcrt4 는 그 SD 에서 소유자·ACE·라벨을 **직접 읽지 않는다** (`Rtl*GetOwner/GetAce/EqualSid` 호출 0).
- `NtQuerySecurityObject` 를 두 번 부르는 꼴 맞음 (크기 → 할당 → 다시). **첫 부름이 정확히 `0xC0000023` 일 때만** 다음으로 간다 (`+0x164 CMP EAX,0xc0000023` / `+0x169 JNZ`). 0 이든 다른 실패든 전부 `+0x2ed` → 반환값 `0xe`.
- `0xe` 는 이 사슬에서 **상수** 다 (NTSTATUS → Win32 변환 없음). `UuidToStringA` 는 `PerformRpcInitialization` 의 반환 EAX 를 안 건드리고 그대로 돌려준다.
- 에뮬레이터의 "status 0 + `len` 안 씀" 은 **길이를 보기도 전에** `+0x169 JNZ` 에서 실패로 간다. `len` 이 0 이어서 실패하는 게 아니다 (그건 그 다음 문제).
- `\RPC Control\DSEC` 는 이 초기화의 정상 길이 아니다 (공유 메모리 "셀 힙" 디버그용 `GenerateSectionName`, 정책 값으로 켰을 때만).

## 지도에서 본 것

`UuidToStringA` 뿌리 (깊이 0, caller 0곳) 지도 (`_map/UuidToStringA_*`, 기준 20): 직접 부르는 것 4개 — `PerformRpcInitialization` (`+0x31/+0x36`, **helper**: caller 140곳이라 안 펼침), `__security_check_cookie` (`+0x42/+0x47`), `ConvertToString` (`+0x65/+0x6a`), `W2AAttachHelper` (`+0x77/+0x7c`). `W2AAttachHelper` 아래는 `RtlUnicodeToMultiByteSize` / `AllocWrapper` / `RtlUnicodeToMultiByteN`. 이 지도에 **보안 검사는 없다** — 검사는 `PerformRpcInitialization` 쪽으로 넘어가야 나온다.

`PerformRpcInitialization` 을 뿌리로 다시 만든 지도 (`_map/PerformRpcInitialization_*`, 기준 1000, 71개 함수):

| 함수 | 깊이 (UuidToStringA 기준) | caller 수 | 비고 |
|---|---|---|---|
| `PerformRpcInitialization` | 1 | 140 | export 123개가 입구에서 부름 (질문 3) |
| `InitializeDefaultSecurityDescriptor` | 2 | 1 (`PerformRpcInitialization +0x2b3`) | |
| `RpcpNormalizeSecurityDescriptor` | 3 | 4 | 부르는 곳 4곳 (질문 1) |
| `RpcpCopySecurityDescriptor` | 4 | 8 | |

- 직접 호출 지도에 **`NtAlpc*` 와 `NtCreateSection` 은 없다** (`grep -ci 'alpc|CreateSection|NtCreate'` = 0). ALPC 는 `AlpcConnect` 쪽에만 있다 (질문 4).
- `NtOpenDirectoryObject` / `NtQuerySecurityObject` 는 rpcrt4 가 ntdll 에서 **직접 가져온다** (`__imp_` 칸 `0xe96e8` / `0xe96e0`). 그래서 지도에는 `external` 로 나온다.

## 질문별 답 [정적]

### 질문 1. 누가 여나 — 사슬

**`\RPC Control\` 문자열 xref** (`_refs/0xeb868.tsv`, UTF-16, 끝 `\` 포함 13글자, 11곳):

| 함수 | 줄 | 뭘 하나 |
|---|---|---|
| `RpcpNormalizeSecurityDescriptor` | `+0x63`, `+0xc8`, `+0x307` | **디렉터리 열기** (아래). 이름 만들 때 `wcsstr` 바늘 + `DuplicateString3` 가운데 조각 |
| `TransformLrpcEndpointToAlpcPortName` | `+0x10`, `+0x59`, `+0x95` | ALPC 포트 이름 만들기 (이름만) |
| `AlpcConnect` | `+0x8c`, `+0x9c`, `+0xf4`, `+0x472` | ALPC 연결 이름 (이름만) |
| `InquireBinding` | `+0x16e` | 같은 이름 꼴 (이름만) |

`NtOpenDirectoryObject` 가져오기 칸 참조는 **`RpcpNormalizeSecurityDescriptor +0x12a` 1곳** → 여는 함수는 이것 하나다. `NtQuerySecurityObject` 칸 참조도 같은 함수 `+0x158`, `+0x19c` 2곳뿐.

**사슬 표** (`.asm` 의 `CALL … ret=` 줄. 괄호는 call 자리/ret 자리):

| # | 함수 + 오프셋 | 하는 일 | syscall |
|---|---|---|---|
| 0 | `UuidToStringA +0x26/+0x2f` | `RpcHasBeenInitialized` (`0x180104104`) 가 0 이 아니면 `+0x5d` (정상 본문) 로 건너뜀 | - |
| 1 | `UuidToStringA +0x31/+0x36` → `PerformRpcInitialization` | 0 이 아니면 (실패) `+0x3a`~`+0x5b` 에서 EAX 그대로 반환 | - |
| 2 | `PerformRpcInitialization` 앞부분 (아래 "앞 단계") | 전역 락, 시스템 정보, 할당기, 서버 DLL 초기화 등 | 여러 개 (이미 지나감) |
| 3 | `PerformRpcInitialization +0x2b3/+0x2b8` → `InitializeDefaultSecurityDescriptor` | 기본 SD 를 **스택에서 만들어** 아래 4 로 넘김 | - |
| 4 | `InitializeDefaultSecurityDescriptor +0x13c/+0x141` → `RpcpNormalizeSecurityDescriptor(sd, 1, &gDefaultSecurityDescriptor)` | 인자: 만든 SD, `AutoInheritFlags=1` (`+0x134 MOV EDX,1`), 결과 칸 `0x180103e60` | - |
| 5 | `RpcpNormalizeSecurityDescriptor +0x88/+0x8f` → `wcsstr(L"", L"\RPC Control\")` | 바늘을 찾는 통은 **빈 문자열** (`+0x2b` RSI=`0x1800eb970` = `00 00`) → 항상 NULL | - |
| 6 | `+0xa8/+0xaf` → ntdll `RtlGetAppContainerNamedObjectPath(0,0,0,&us)` | 안에서 `+0xb6/+0xbb` **`NtQueryInformationToken(-6, 0x1d, &v, 4, &len)`** | **`NtQueryInformationToken`** |
| 7 | `+0xd2/+0xd7` → `DuplicateString3(앞, L"\RPC Control\", L"")` | 이름 = 앞 + `\RPC Control\` (일반 프로세스에서는 앞이 빈 글, 아래 갈림) | - |
| 8 | `+0xea/+0xf1` `RtlInitUnicodeString`, `+0x105 MOV EAX,0xfffe` / `+0x111 ADD word [RBP-0x39],AX` | **길이를 2바이트 깎음** → `\RPC Control` (Length 0x18) 로 열린다 (끝 `\` 뗌) | - |
| 9 | `+0x12a/+0x131` | **`NtOpenDirectoryObject(&h, 0x20001, &oa)`** (`+0x11c MOV EDX,0x20001`, OA: Length 0x30 `+0x10a`, RootDirectory 0 `+0xf6`, Attributes 0 `+0xfe`, SD·SQOS 0 `+0x125`) | **`NtOpenDirectoryObject`** |
| 10 | `+0x146`~`+0x158` / ret `+0x15f` | **`NtQuerySecurityObject(h, 0x17, NULL, 0, &len)`**, `len` 먼저 0 으로 지움 (`+0x146`) | **`NtQuerySecurityObject`** |
| 11 | `+0x16f`~`+0x172` / ret `+0x177` | `AllocWrapper(len)` (= `HeapAlloc(hRpcHeap, 0, len)`, `AllocWrapper +0x38`~`+0x44`) | - |
| 12 | `+0x183`~`+0x19c` / ret `+0x1a3` | **`NtQuerySecurityObject(h, 0x17, buf, len, &len)`** (같은 `len` 칸 재사용) | **`NtQuerySecurityObject`** |
| 13 | `+0x1b0/+0x1b7` `GetCurrentThread`, `+0x1cc/+0x1d3` `OpenThreadToken(thr, 8, TRUE, &tok)` | 스레드 토큰 열기 | **`NtOpenThreadToken`** (kernelbase `OpenThreadToken +0x4`) |
| 14 | 실패면 `+0x2ae/+0x2b5` `GetLastError`, `+0x2ba CMP EAX,0x3f0`, `+0x2c1/+0x2c8` `GetCurrentProcess`, `+0x2d9/+0x2e0` `OpenProcessToken(proc, 8, &tok)` | 스레드 토큰이 없을 때만 프로세스 토큰 | **`NtOpenProcessToken`** (kernelbase `OpenProcessToken +0x4`) |
| 15 | `+0x209/+0x210` → ntdll `RtlNewSecurityObjectEx(부모=버퍼, 만든이=기본 SD, &새SD, NULL, FALSE, 1, 토큰, 일반 매핑)` | 새 SD 만들기 (질문 4 표) | `NtQueryInformationToken` 여러 번 |
| 16 | `+0x224/+0x229` → `RpcpCopySecurityDescriptor(새SD, &gDefaultSecurityDescriptor)` | 새 SD 가 유효한지 보고 힙에 복사 | - |
| 17 | 마무리 `+0x22e/+0x233` `FreeWrapper(이름)`, `+0x23c/+0x243` `CloseHandle(h)`, `+0x250/+0x255` 버퍼 해제, `+0x25e/+0x265` `CloseHandle(tok)`, `+0x275/+0x27c` `RtlDeleteSecurityObject` | 정리 | **`NtClose`** ×2 |

- 일반 매핑 (`+0x1e0 LEA RAX,[RBP+0x17]` → `[RSP+0x38]`): `GenericRead 0x20001` `GenericWrite 0x20001` `GenericExecute 0x20000` `GenericAll 0x1f0001` (`+0x34` `+0x4c`~`+0x60` `+0x6a` 에서 채움).
- `RpcpNormalizeSecurityDescriptor` 를 부르는 곳은 **4곳**: `InitializeDefaultSecurityDescriptor +0x13c` (초기화), `SetAuthInformation +0x4f6`, `UpdateRpcInterfaceInformation +0x59`, `LRPC_FAST_BINDING_HANDLE +0x38a` (`refs`, Ghidra 이름 `LRPC_FAST_BINDING_HANDLE` 0x5b420, 1213 B — 생성자일 것 `[추측]`). 앞의 셋은 "바인딩에 인증 정보를 붙일 때 / 서버 인터페이스를 등록할 때" 같은 **초기화 밖** 길이다. 샘플 로그에서 `\RPC Control` 열기가 초기화 말고 다른 때에도 보이면 이쪽이다.

**앞 단계 (`PerformRpcInitialization +0x0`~`+0x2b3`, 3 에 닿기 전)**: 에뮬레이터는 이미 지나가는 자리지만 순서만 적는다 (이름·오프셋만).
`+0x21/+0x28` `RtlEnterCriticalSection(GlobalMutex)` → `+0x59/+0x60` `LoadLibraryExW(L"rpcrt4.dll")` (`0x1800ec490`, 실패하면 `+0x68 JZ` → `0xe`) → `+0x73/+0x78` `GetBasicSystemInfo` (안에 `NtQuerySystemInformation +0x40/+0x47`, `RtlGetNtProductType`) → `+0xf1/+0xf8` `GetTickCount` → `+0x103/+0x108` `InitializeRpcAllocator` → `+0x110/+0x115` `InitializeServerDLL` → 할당 `+0x120`, `+0x161` → `+0x18c/+0x193` `GetComputerNameExW` → `+0x299/+0x29e` `RpcpApplyIFEOConfigurations` (안에 `LdrQueryImageFileExecutionOptions`) → (PEB `+0xbc` 의 앱 검증기·페이지힙 비트가 켜져 있으면 `+0x2ae/+0x2b3` `InitializeRpcVerifier`) → `+0x2b3/+0x2b8` **3 으로**.

**`NtQueryInformationToken(-6, 0x1d, 4)` 와 갈림** (ntdll `RtlGetAppContainerNamedObjectPath.asm`; 호출 `Token=0`, `AppContainerSid=0`, `RelativePath=FALSE`):

- 부르는 줄: `RtlGetAppContainerNamedObjectPath +0xb6/+0xbb`. `+0x8c MOV RAX,-6` / `+0xa2 CMOVZ RDI,RAX` (토큰이 0 이면 `-6`), `+0x9f LEA EDX,[RSI+0x1d]` (클래스 `0x1d` = `TokenIsAppContainer` `[추측]` — 이름은 문서 지식, 숫자는 코드), `+0x93` 길이 4, 값 칸 `[RSP+0x30]` (`+0x1e` 에서 **0 으로 미리 지움**).
- 갈림 (**값 칸 `[RSP+0x30]` 과 status**):

| 결과 | 줄 | 이후 | rpcrt4 쪽에서 |
|---|---|---|---|
| status < 0 | `+0xbd JS` → `+0xd0` (에필로그) | 그 status 를 돌려줌 | `RpcpNormalize +0xb6 JS` → `+0xc5` : 이름 = `DuplicateString3(L"", L"\RPC Control\", L"")` = `\RPC Control\` |
| status ≥ 0, 값 = 0 | `+0xc1 CMP [RSP+0x30],ESI` / `+0xc5 JNZ` 안 감 → `+0xc7`~`+0xcc` 출력 `UNICODE_STRING` 16 바이트 0 으로 쓰고 `XOR EAX,EAX` | status 0, 문자열 비어 있음 | `+0xb8 MOV RCX,[RBP-0x21]` (Buffer) = 0 → `+0xbf JNZ` 안 감 → `+0xc5` 와 같은 길 (**`\RPC Control\`**) |
| status ≥ 0, 값 ≠ 0 (AppContainer) | `+0xc5 JNZ` → `+0xfa` → `+0x116/+0x11b` `RtlpGetTokenNamedObjectPath` | 샌드박스 개체 경로 | `+0xbf JNZ` → `+0x304`: 이름 = `DuplicateString3(그 경로, L"\RPC Control\", L"")` |

- 같은 호출이 **`InitializeCellHeap +0x29/+0x30`** 에도 있다 (아래 질문 4, 정책 값이 있을 때만).
- 지금 에뮬레이터 로그 (`NtQueryInformationToken(-6,0x1d,4)` 바로 뒤 `NtOpenDirectoryObject("\RPC Control")`) 는 위 앞 두 줄과 같은 길이다. **어느 쪽 대답이든 이름은 `\RPC Control`** 이라 이 호출의 대답은 통과·실패를 가르지 않는다 (값 칸을 안 써도 0 으로 미리 지워 둠).

**지금 우리 (PE_emulator)**: **맞음.** 로그 순서 (`UuidToStringA` 들어감 → … → `NtQueryInformationToken(-6,0x1d,4)` → `NtOpenDirectoryObject("\RPC Control", 0x20001)` → `NtQuerySecurityObject(h, 0x17, NULL, 0, &len)` → `NtClose`) 가 코드와 줄마다 같다. 권한 `0x20001` (`+0x11c`), 비트 `0x17` (`+0x154`/`+0x192`), (NULL, 0, `&len`) (`+0x14c` `+0x149` `+0x14f`) 모두 코드 상수. 이름의 끝 `\` 를 떼는 것 (`+0x111`) 도 로그의 `\RPC Control` 과 맞다.

### 질문 2. SD 에서 무엇을 보나

**답 1 — 두 번 부르는 꼴인가: 맞음.** `NtQuerySecurityObject` 가 `+0x158`/`+0x19c` 2곳. 첫째는 (NULL, 0), 둘째는 (`AllocWrapper(len)` 버퍼, `len`).

**답 2 — rpcrt4 는 SD 안을 안 읽는다.** `RpcpNormalizeSecurityDescriptor` 의 호출 목록 (`_calls.tsv`, `.c` 맨 위 `callees:`) 에 `RtlGetOwnerSecurityDescriptor` / `RtlGetDaclSecurityDescriptor` / `RtlGetSaclSecurityDescriptor` / `RtlGetAce` / `RtlEqualSid` / `RtlValidSid` **하나도 없다**. 받은 SD 는 `+0x1fc MOV RCX,RDI` 로 **그대로** `RtlNewSecurityObjectEx` 의 첫 인자 (부모 SD) 로 간다. 즉 "소유자 SID 비교 / DACL ACE 검사 / 라벨 ACE (수준·마스크) 비교" 는 **이 길에 없다**.

**검사 표** (rpcrt4 쪽에서 실제로 하는 검사 전부):

| 읽는 것 | 줄 | 비교 값 | 실패하면 |
|---|---|---|---|
| `NtOpenDirectoryObject` status | `+0x136 TEST EAX,EAX` / `+0x138 JS` | 음수 | `+0x2ed` (= `0x180038735`) `MOV EBX,0xe` → `+0x22b` 정리 → 반환 `0xe` |
| 첫 `NtQuerySecurityObject` status | `+0x164 CMP EAX,0xc0000023` / `+0x169 JNZ` | **정확히 `0xC0000023`** 만 통과 | `+0x2ed` (**0 이어도**) |
| `AllocWrapper(len)` 결과 | `+0x17a TEST RAX,RAX` / `+0x17d JZ` | NULL | `+0x2ed` |
| 둘째 `NtQuerySecurityObject` status | `+0x1a8 TEST EAX,EAX` / `+0x1aa JS` | 음수 | `+0x2ed` |
| `OpenThreadToken` 결과 | `+0x1d8` / `+0x1da JZ` → `+0x2ae` | 0 (FALSE) 이면 오류 코드 확인 | `+0x2b5` 뒤 `+0x2ba CMP EAX,0x3f0` / `+0x2bf JNZ` → `+0x2ed` (**`ERROR_NO_TOKEN` 만 프로세스 토큰으로 넘어감**) |
| `OpenProcessToken` 결과 | `+0x2e5 TEST EAX,EAX` / `+0x2e7 JNZ` → `+0x1e0` | 0 | 안 뛰고 떨어져서 `+0x2ed` |
| `RtlNewSecurityObjectEx` status | `+0x215 TEST EAX,EAX` / `+0x217 JS` | 음수 | `+0x2ed` |
| `DuplicateString3` 결과 (이름 만들기) | `+0xda TEST RSI,RSI` / `+0xdd JZ` → `+0x2a7` | NULL | `+0x2a7 MOV EBX,0xe` → `+0x2ac JMP +0x233` |
| `RpcpCopySecurityDescriptor` 결과 | `+0x229 MOV EBX,EAX` | 반환 그대로 | 그대로 돌려줌 (`RpcpCopySecurityDescriptor`: 유효하지 않으면 `+0xa1 MOV EAX,0x53a`, 할당 실패 `+0x104 MOV EAX,0xe`) |

- **"첫 부름 status 가 0 이면 어디로 가나" = `+0x169 JNZ` 로 `+0x2ed` (`0xe`).** 상위 길이값 `len` 은 이 비교 뒤에야 읽는다 (`+0x16f MOV ECX,[RBP-0x59]`).
- 첫 부름이 `0xC0000023` 이고 `len` 만 0 이면: `AllocWrapper(0)` (`HeapAlloc(…, 0, 0)`, 진짜 힙은 0 바이트도 포인터를 돌려주므로 `+0x17d` 통과 `[추측]`) → 둘째 부름이 길이 0 으로 나가 진짜 커널에서는 다시 `0xC0000023` → `+0x1aa JS` → `0xe`. 즉 **`len` 은 진짜 필요 길이** 여야 한다.
- rpcrt4 가 **스스로 만드는** SD (`InitializeDefaultSecurityDescriptor`, 이건 "부모" 가 아니라 "만든이" 쪽): `+0x23` 스택 SID `{Revision 1, SubAuthorityCount 1, Authority 0 0 0 0 0 5, SubAuthority[0]=7}` = `S-1-5-7` (익명 로그온) `[정적: 바이트 / 이름은 지식]`, `+0xe4/+0xeb` `AddAccessAllowedAce(acl, 2, 0x80000000, sid)` (`GENERIC_READ`), `+0xfc` 빈 SD, `+0x119` `SetSecurityDescriptorDacl(sd, TRUE, acl, FALSE)`. 소유자·그룹·SACL 없음. 결과의 소유자·그룹은 토큰에서 온다 (질문 4).
- **받은 부모 SD 를 ntdll 이 어떻게 읽나 (읽은 만큼)** — `RtlpNewSecurityObject` (`RtlNewSecurityObjectEx +0x59/+0x5e` 에서 부름): `RtlValidSecurityDescriptor` 같은 검증 함수를 **부르지 않고** (callee 목록에 없음), 부모 SD 의 `Control` (`+2`) 를 보고 자체 판단. `+0x35e MOVZX ECX,[R11+2]` / `+0x363 TEST CL,0x10` (`SE_SACL_PRESENT`) / `+0x366 JNZ +0xef8`, 거기서 `+0xef8 TEST CX,CX` / `+0xefb JNS` (자기상대형이 아니면 다른 칸 배치) / `+0xf01 MOV ECX,[R11+0xc]` (자기상대형 SACL 오프셋). DACL 쪽 읽기와 그 안 ACE 해석 (`RtlpInheritAcl2`) 은 안 따라감 `[공백]`.

**지금 우리 (PE_emulator)**: **틀림.** 에뮬레이터 대답 "status 0 + `len` 안 씀" 은 `+0x164 CMP EAX,0xc0000023` / `+0x169 JNZ` 에서 바로 `+0x2ed` → `0xe` 로 간다 (로그의 "`NtQuerySecurityObject` → `NtClose` → 반환, 그 사이 syscall 0" 과 코드가 정확히 같다: `+0x2ed` 에서 `EBX=0xe` 로 `+0x22b` 정리에 들어가 → 이름 해제 (`FreeWrapper`, 힙이라 syscall 없음) → `+0x233 TEST RCX` / `+0x23c CALL CloseHandle(h)` (= `NtClose` 하나) → 버퍼는 NULL 이라 해제 건너뜀 (`+0x248 TEST RDI` / `+0x24b JZ`) → 토큰 핸들 0 이라 건너뜀 (`+0x259`/`+0x25c`) → 새 SD 0 이라 `RtlDeleteSecurityObject` 건너뜀 (`+0x26a CMP [RBP-0x41],0` / `+0x26f JZ`) → `+0x2a5` 반환). 고칠 것: 첫 부름 = **`0xC0000023` + `len` = 필요 길이** (sast/93), 둘째 = 0 + 자기상대형 SD.

### 질문 3. 언제 `0xe` 로 가나

**답: `UuidToStringA` 는 `0xe` 를 만들지 않는다. 초기화 함수의 반환값을 그대로 돌려줄 뿐이고, `0xe` 는 `PerformRpcInitialization +0x425` 와 `RpcpNormalizeSecurityDescriptor +0x2ed` 가 상수로 정한다.**

- `UuidToStringA +0x36 TEST EAX,EAX` / `+0x38 JZ +0x5d`: 0 이면 정상 본문. 0 이 아니면 `+0x3a`~`+0x5b` (쿠키 검사 + 에필로그) 로 떨어지며 **EAX 를 안 건드림** → 호출한 쪽은 `PerformRpcInitialization` 값 그대로를 본다.
- `PerformRpcInitialization +0x425 MOV EBX,0xe` (`0x18005fba9`) 로 뛰어오는 곳 (전부 실패 모양, **각자 값을 버리고 한 값으로 뭉침**):

| 점프 | 조건 |
|---|---|
| `+0x68 JZ` | `LoadLibraryExW(L"rpcrt4.dll")` 가 NULL |
| `+0x7a JZ` | `GetBasicSystemInfo` 가 0 |
| `+0x10a JNZ` / `+0x117 JNZ` | `InitializeRpcAllocator` / `InitializeServerDLL` 가 0 이 아님 |
| `+0x12b JZ` → `+0x41e` | 락 할당 실패 (`+0x41e MOV [ThreadListMutex],RDI` 뒤 `+0x425`) |
| `+0x170 JZ` | 컴퓨터 이름 칸 할당 실패 |
| `+0x1c2 JGE`, `+0x201 JZ`, `+0x21f JNZ` | `GetComputerNameExW` 실패 갈래 (오류 8·2 외 등) |
| **`+0x2ba JNZ`** | **`InitializeDefaultSecurityDescriptor` 가 0 이 아니면 (어떤 값이든)** — 이 길 |

- `InitializeEPMapperClient` (`+0x2c7/+0x2c9`), `ReadPolicySettings` (`+0x2d6/+0x2d8`), `InitializeCellHeap` (`+0x362/+0x364`) 는 자기 값을 **그대로** (`EBX = EAX`) 돌려준다 (이들도 안에서는 `MOV EAX,0xe` 로 할당 실패를 표시함). 예외: `InitializeCellHeap` 가 `0x6e4` 를 돌려주면 `+0x34b CMP EAX,0x6e4` / `+0x352`~`+0x35a` 에서 디버그 정보 두 전역을 0 으로 끄고 성공 (`EBX=0`) 으로 친다.
- `InitializeDefaultSecurityDescriptor`: `RpcpNormalizeSecurityDescriptor` 의 반환을 `+0x141 JMP +0x9a` → `MOV EBX,EAX` → `+0xb1 MOV EAX,EBX` 로 그대로. 자기 실패 (`InitializeAcl` 등) 는 `GetLastError` (`+0x8e`), 할당 실패는 `+0x146`/`+0x150 MOV EBX,0xe`.
- `RpcpNormalizeSecurityDescriptor` 안에서 `0xe` 로 가는 곳은 표 (질문 2) 의 `+0x2ed` 와 `+0x2a7` 둘이다.
- **초기화 실패 status → Win32 오류 대응 줄은 없다.** 이 사슬 어디에도 `RtlNtStatusToDosError` 호출이 없다. NTSTATUS 를 받는 자리 (`+0x136`, `+0x164`, `+0x1a8`, `+0x215`) 는 부호·비교만 하고 값은 버린 채 `0xe`. 오직 `OpenThreadToken` 만 kernelbase 가 오류 코드를 바꾸어 (`OpenThreadToken +0x14/+0x16` → `BaseSetLastNTError`, 그 안 `+0x6/+0xd` `RtlNtStatusToDosError`) rpcrt4 `+0x2ae/+0x2b5 GetLastError` / `+0x2ba CMP EAX,0x3f0` 에서 비교한다.
- 값의 이름: `0xe` = 14 = `ERROR_OUTOFMEMORY` (= `RPC_S_OUT_OF_MEMORY`) `[지식, 코드엔 이름 없음]`. 정말 메모리 부족이라는 뜻이 아니라 "이유와 상관없이 초기화 실패" 의 단일 값이다.
- **`RpcHasBeenInitialized` 는 맨 끝 `+0x412 MOV [0x180104104],1` 에서만 켜진다.** 실패하면 안 켜지고, 같은 export 를 다시 부를 때마다 앞 단계부터 **전부 다시** 한다 (`+0xc CMP` / `+0x2d CMP` 두 번 확인 후 `+0x39` 부터 처음 길). 로그의 "한 샘플에서 100번 넘게 되풀이 (가장 많이 3137)" 와 맞는 코드다.

**지금 우리 (PE_emulator)**: **맞음 (`0xe` 의 출처).** `UuidToStringA` 가 `0xe` 를 돌려주는 건 코드대로다. 틀린 건 그 앞 `NtQuerySecurityObject` 대답 한 곳. 다른 export 도 같은 길: `PerformRpcInitialization` 을 입구에서 부르는 export 가 **123개** (`_refs/PerformRpcInitialization.tsv` 의 caller 이름과 export 이름 일치 기준) — 요청서의 `UuidToStringA` · `UuidFromStringA` (`+0xa9`) 말고도 `UuidToStringW` `UuidCreateSequential` (`+0xd6`) `RpcStringBindingComposeW` (`+0x115`) `RpcBindingFromStringBindingW` (`+0x196`) `RpcBindingSetAuthInfoExW` `RpcServerRegisterIf*` `RpcServerListen` `I_RpcGetDefaultSD` 등. advapi32 `LookupPrivilegeValue` / winhttp / ws2_32 / shell32 / combase 는 **안 봤다** — 이 export 들 중 하나를 부르면 같은 길이라는 것만 코드로 말할 수 있고, 각 모듈이 정확히 어느 export 를 부르는지는 `[공백]`.

### 질문 4. 통과하면 바로 다음 syscall

**답: ALPC 도 `\RPC Control\DSEC` 도 아니다. 같은 함수 안에서 토큰 열기와 토큰 정보 읽기가 먼저 나오고, 그다음 레지스트리 열기다.**

통과 (둘째 `NtQuerySecurityObject` 성공) 뒤 순서:

| 순서 | 어디 | syscall (이름·오프셋만) |
|---|---|---|
| ① | `RpcpNormalize +0x1cc/+0x1d3` → kernelbase `OpenThreadToken +0x4` | `NtOpenThreadToken(현재 스레드, 8, TRUE, &tok)` |
| ② | (실패하고 `GetLastError == 0x3f0` 일 때) `+0x2d9/+0x2e0` → kernelbase `OpenProcessToken +0x4` | `NtOpenProcessToken(현재 프로세스, 8, &tok)` |
| ③ | `+0x209/+0x210` → ntdll `RtlNewSecurityObjectEx +0x59/+0x5e` → `RtlpNewSecurityObject +0x189/+0x18e` | `NtQueryInformationToken(tok, 10, buf 0x38)` (`+0x16f LEA EDX,[R12+0xa]`, `+0x15b R9D=0x38`) → 결과의 `TokenType` (`[RBP+0x138]` = 칸 `+0x18`) 가 2 (가장) 이면 `+0x1a2 JZ` 로 다른 길 |
| ④ | `RtlpNewSecurityObject +0x234/+0x239` → `RtlpGetDefaultsSubjectContext` (`MOV EDX,0x80` `SETNZ DL` = 만든이 SD 의 `SE_SERVER_SECURITY`; 기본 SD 는 0) | `NtQueryInformationToken(tok, 4, 0x4c)` (call/ret `+0xbb/+0xc0`, 한 번에 — **크기 안 물음**) → `(tok, 0x19, 0x54)` (`+0x110/+0x115`) → `(tok, 5, NULL, 0)` 기대 `0xC0000023` (`+0x135/+0x13a`, `+0x13c CMP EAX,0xc0000023` / `+0x141 JNZ`) → `(tok, 5, buf, n)` (`+0x183/+0x188`) → `(tok, 6, NULL, 0)` 기대 `0xC0000023` (`+0x1a8/+0x1ad`, `+0x1af CMP` / `+0x1b4 JNZ`) → `(tok, 6, buf, n)` (`+0x1ee/+0x1f3`) |
| ⑤ | `+0x203/+0x208` → `RtlpGetDefaultTrustSubjectContext` | `NtQueryInformationToken(-4, 0x29, 0x4c)` (`+0x6f/+0x74`, `+0x56 MOV RCX,-4`, `+0x61 LEA EDX,[R9-0x23]`) → `(tok, 0x29, …)` (`+0xa2/+0xa7`) |
| ⑥ | `RpcpNormalize +0x23c/+0x243`, `+0x25e/+0x265` | `NtClose(h)`, `NtClose(tok)` |

- **그다음, `RpcpNormalizeSecurityDescriptor` 가 끝나고 `PerformRpcInitialization` 으로 돌아와** (`+0x2b8 TEST EAX,EAX` 통과) 부르는 것:
  - `+0x2c0/+0x2c5` `InitializeEPMapperClient` — 할당 + 임계 구역 초기화뿐, **syscall 없음**.
  - `+0x2cf/+0x2d4` `ReadPolicySettings` — **다음 syscall 은 레지스트리 열기**: `RegOpenKeyExW(HKLM, L"Software\Policies\Microsoft\Windows NT\Rpc", 0, 0x20019, &h)` (`ReadPolicySettings +0x44/+0x4b`; 키 문자열 `0x1800ecf20`). 결과: 0 → 값 읽기 (`RegGetValueW` 여러 번, 끝에 `RegCloseKey`), **오류 2 (없음)** → `+0x5f` `SetAutoPolicySettings` 호출 후 0 반환 (정상), **오류 5** → `+0x330` 같은 식 정상, 그 밖 → `+0x347` `MOV EAX,0xe`. 정확히 어느 `Nt*Key*` 가 나오는지는 kernelbase `RegOpenKeyExW` 안이라 안 따라감 `[공백]`.
  - 이어서 `+0x2fb/+0x302` `SbSelectProcedure` (`Microsoft_Windows_RPC_EventsEnableBits+0x8` 가 0 일 때만), `+0x36a/+0x371` `WinSqmIsOptedIn`, `+0x38d`/`+0x3a3`/`+0x3c2` `McGenEventRegister_EtwEventRegister`, `+0x3e8/+0x3ef` `EtwEventRegister`, `+0x407/+0x40c` `GetRpcIdleTimerWindow` (안 `RegGetValueW`), 끝 `+0x412` 로 초기화 완료. 이 뒤 ETW·레지스트리 syscall 의 정확한 이름은 안 따라감.
- **ALPC (`NtAlpcCreatePort` · `NtAlpcConnectPort*`)** 는 이 초기화 지도 71 함수 안에 없다. ALPC 는 `AlpcConnect` (`NtAlpcConnectPortEx` · `NtAlpcConnectPort`, 호출자 `Connect`·`ContinueAlpcConnectAfterSidNormalize`) 와 서버 쪽에서만 나오며, 초기화 `InitializeRpcProtocolLrpc` (`InitializeServerDLL +0x34/+0x39` 가 부름) 는 할당·임계 구역·`IntLogBase2` 뿐이다.
- **`\RPC Control\DSEC`** (`_refs/0xef3b8.tsv`): 쓰는 함수는 **`GenerateSectionName +0x25`** 하나 (`Ndr64UnmarshallHandle +0x293` 의 참조는 `0x1800ef3e0` 읽기로 다른 변수, 범위가 겹쳐 잡힌 것). `GenerateSectionName` 의 caller 는 `AllocateCellSection` (`+0xb6`, `+0xc7`), 그 caller 는 `AllocateCell`, `InitializeServerSideCellHeap`. `AllocateCellSection +0x13f/+0x146` 에서 **`NtCreateSection`** (이름 = `\RPC Control\DSEC` + 숫자들, `RtlIntegerToUnicodeString` 으로 붙임).
  - 이 길로 가려면 `PerformRpcInitialization +0x32e CMP g_fServerSideDebugInfoEnabled` / `+0x33a JNZ` 또는 `+0x33c CMP g_fClientSideDebugInfoEnabled` / `+0x342 JZ` → `+0x344/+0x349` `InitializeCellHeap`. 두 값은 `ReadPolicySettings` 가 정책 레지스트리 값으로 켠다 (`+0x351 MOV [g_fServerSideDebugInfoEnabled],EBX`). 정책 키가 없으면 (보통) 안 켜진다 `[추측: 기본값은 0 으로 초기화된 전역]`.
  - `InitializeCellHeap` 에서 `CellHeap` 생성자 (`+0x60/+0x65`) 가 `AllocateCell*` 까지 직접 닿는지는 지도 (직접 호출) 에 안 나온다 `[공백]`.

**지금 우리 (PE_emulator)**: **앞으로의 벽이다.** 첫 `NtQuerySecurityObject` 만 고치면 곧바로 ①~⑥ 이 나오므로, `NtOpenThreadToken` (STATUS_NO_TOKEN 이 정상) · `NtOpenProcessToken` · `NtQueryInformationToken` 클래스 `10 / 4 / 0x19 / 5 / 6 / 0x29` 와 레지스트리 `HKLM\Software\Policies\Microsoft\Windows NT\Rpc` 열기가 이미 구현돼 있어야 한다 — 이쪽 구현 상태는 `[공백]` (PE_emulator 쪽에서 확인). `\RPC Control\DSEC`·ALPC 는 이 문제의 다음 벽이 **아니다**.

### 질문 5. 최소 조건 표

에뮬레이터가 `UuidToStringA` 안 초기화 중 **`InitializeDefaultSecurityDescriptor` 를 통과** 시키려면 (줄은 위 표):

| # | syscall | 인자 (코드 상수) | 꼭 줘야 하는 것 | 틀리면 |
|---|---|---|---|---|
| 1 | `NtQueryInformationToken` | `(-6, 0x1d, &값, 4, &len)` (`RtlGetAppContainerNamedObjectPath +0xb6`) | 아무 status 나 가능. 값 칸을 안 써도 됨 (미리 0). **값 ≠ 0 (AppContainer) 만 피하기** | 값 ≠ 0 이면 이름이 `…\<샌드박스 경로>\RPC Control` 이 되어 `NtOpenDirectoryObject` 가 다른 이름으로 나감 |
| 2 | `NtOpenDirectoryObject` | `(&h, 0x20001, OA{Length 0x30, Root 0, Name=\RPC Control (Length 0x18), Attr 0})` | **status ≥ 0**, `h` = 쓸 수 있는 핸들 | 음수 → `0xe` (`+0x138`) |
| 3 | `NtQuerySecurityObject` | `(h, 0x17, NULL, 0, &len)` (`len` 은 0 으로 미리 지움) | **status = `0xC0000023` 정확히** + `*len` = **진짜 필요 길이** (sast/93: `0x14 + …`) | 다른 값 (0 포함) → `0xe` (`+0x169`) |
| 4 | `AllocWrapper` → `HeapAlloc` | 크기 = `*len` | 0 이 아닌 포인터 (크기 0 이라도) | NULL → `0xe` (`+0x17d`) |
| 5 | `NtQuerySecurityObject` | `(h, 0x17, buf, *len, &len)` | **status ≥ 0**, `buf` = **자기상대형 SD** (`Revision 1`, `Control` 에 `0x8000`, 오프셋이 `buf` 안, sast/93 질문 3 의 꼴) | 음수 → `0xe` (`+0x1aa`) |
| 6 | `NtOpenThreadToken` | `(현재 스레드, 8, TRUE, &tok)` | `STATUS_NO_TOKEN` (`0xC000007C`) → `GetLastError == 0x3f0` 이 되게 (ntdll `RtlNtStatusToDosError` 가 진짜면 자동) **또는** 성공 | 다른 실패 (예 `STATUS_ACCESS_DENIED`) → `+0x2bf JNZ` → `0xe` |
| 7 | `NtOpenProcessToken` | `(현재 프로세스, 8, &tok)` | **성공 + 핸들** | 실패 → `0xe` |
| 8 | `NtQueryInformationToken` | `(tok, 10, buf, 0x38, &len)` (`RtlpNewSecurityObject +0x189`) | **성공**, `TOKEN_STATISTICS.TokenType` (칸 `+0x18`) = **1** (primary) | 음수 → 실패, 2 → 다른 길 |
| 9 | `NtQueryInformationToken` | `(tok, 4, buf, 0x4c)` TokenOwner | 성공 (포인터 + SID 가 **0x4c 바이트 안**, 크기 질문 없음) | 음수 → 실패 |
| 10 | 〃 | `(tok, 0x19, buf, 0x54)` TokenIntegrityLevel | 성공 (0x54 안) | 〃 |
| 11 | 〃 | `(tok, 5, NULL, 0)` → **`0xC0000023` + 길이**, 다시 `(tok, 5, buf, n)` 성공 | TokenPrimaryGroup | status 가 `0xC0000023` 아니면 `+0x141 JNZ` → 실패 |
| 12 | 〃 | `(tok, 6, NULL, 0)` → **`0xC0000023` + 길이**, 다시 `(tok, 6, buf, n)` 성공 | TokenDefaultDacl | 〃 |
| 13 | 〃 | `(-4, 0x29, buf, 0x4c)` 와 `(tok, 0x29, buf, 0x4c)` | TokenProcessTrustLevel: 성공 (SID 칸 NULL 도 가능 `[정적: +0xb0/+0xb3 JNZ 안 감 경로 있음]`) | 음수 → 실패 |
| 14 | `NtClose` | `(h)`, `(tok)` | 아무 | - |

- **SD 안에서 꼭 맞아야 하는 값 (rpcrt4)**: 없음. rpcrt4 는 SD 를 안 읽는다 (질문 2).
- **SD 안에서 필요한 값 (ntdll)**: 읽은 만큼 — 구조가 자기상대형으로 옳을 것 (`Control` `0x8000`; SACL 이 없으면 `0x10` 을 꺼 두면 SACL 읽는 줄 `RtlpNewSecurityObject +0x363 TEST CL,0x10` 이 안 탄다). 부모의 상속 ACE 를 `RtlpInheritAcl2` 로 합치는 부분은 안 따라감 `[공백]`.
- **통과 조건 한 줄 요약**: (3) 만 정확히 `0xC0000023` + 길이, (5) 가 SD 를 채워 성공, 그리고 (6)~(13) 의 토큰 질문들이 다 성공.
- **따로 안 따라간 조건부 syscall** (`RtlpNewSecurityObject` 의 callee 에는 있음): `NtPrivilegeCheck` (LUID 8 = SeSecurityPrivilege 또는 0x20 = SeRestorePrivilege, 만든이가 SACL·소유자·그룹을 직접 줄 때 쪽으로 보임 `[추측]`), `NtDuplicateToken` + `NtAccessCheck` (`RtlpOwnerAcesPresent` 가 참일 때), `RtlAddMandatoryAce` (토큰 무결성 수준 낮을 때로 보임 `[추측]`, `+0x1498`). rpcrt4 의 기본 SD (DACL 하나, 소유자·그룹·SACL 없음) 로는 안 탈 것으로 보이지만 조건을 끝까지 안 봤다 `[공백]` → 동적 확인.

**지금 우리 (PE_emulator)**: 에뮬레이터는 1 과 2 는 이미 한다. 틀린 곳은 3 (0 + 길이 안 씀). **3 을 고치면 4~14 가 처음으로 나온다** — 그 구현이 있는지가 다음 벽. "`len` 이 0 이라 할당 0 → 실패" 가 아니라 **status 0 이라 `+0x169 JNZ` 에서 먼저 실패** 라는 점이 포인트다.

## 동적으로 확인할 것 (Q181 에 넘길 멈춤점)

정적으로 못 정한 것: 진짜 `\RPC Control` SD 바이트 (유저 쪽에서 가장 쉽게 뜨는 자리가 아래 2), 토큰 질문들의 진짜 대답, 조건부 syscall 이 타는지.

추천 멈춤점 (모두 rpcrt4 를 읽어 들인 **샘플 프로세스** (`UuidToStringA` 나 `RpcStringBindingComposeW` 를 부르는 프로그램) 에 걸 것. 커널 멈춤점은 프로세스 조건):

1. `rpcrt4!RpcpNormalizeSecurityDescriptor+0x12a` (`NtOpenDirectoryObject` 부르기 직전): `rcx` = `&h`, `rdx` = `0x20001`, `r8` = OA → `!ustr` 로 `[r8+0x10]` 이름이 `\RPC Control` (Length 0x18) 인지.
2. **`rpcrt4!RpcpNormalizeSecurityDescriptor+0x1a3`** (둘째 `NtQuerySecurityObject` 직후): `eax` = status (0), `rdi` = SD 버퍼, `dwo(rbp-0x59)` = 길이 → `db @rdi L?dwo(@rbp-0x59)` / `!sd @rdi`. **`\RPC Control` SD 바이트 (소유자·그룹·DACL·SACL 의 `0x11` 라벨 ACE 포함) 를 sast/93 의 커널 쪽 멈춤점 없이 뜬다.** 첫 부름 직후 `+0x15f` 에서 `eax == c0000023` 과 `dwo(@rbp-0x59)` (필요 길이) 도 같이.
3. `rpcrt4!RpcpNormalizeSecurityDescriptor+0x1d3` (`OpenThreadToken` 직후 `eax = 0`?) 와 `+0x2b5` (`GetLastError` 직후 `eax == 0x3f0`?): 스레드 토큰 없음 가정 확인.
4. `ntdll!RtlGetAppContainerNamedObjectPath+0xbb` (`NtQueryInformationToken` 직후): `eax`, `dwo(@rsp+0x30)` (`IsAppContainer` 값) 이 0 인지.
5. `ntdll!RtlpNewSecurityObject+0x18e` (`TokenStatistics` 직후): `eax`, `dwo(@rbp+0x138)` (`TokenType`). `ntdll!RtlpGetDefaultsSubjectContext+0xc0 / +0x115 / +0x13a / +0x188 / +0x1ad / +0x1f3`: 각 `eax` (특히 `+0x13a`, `+0x1ad` 는 `0xC0000023`) 과 쓰인 길이.
6. `ntdll!RtlpNewSecurityObject+0x461` / `+0x1121` / `+0x144a` / `+0x1498` (`RtlpInheritAcl2`, `NtDuplicateToken`, `NtPrivilegeCheck`, `RtlAddMandatoryAce` 직전): 조건부 syscall 이 이 입력에서 타는지 (`bp` 에 안 걸리면 안 탄 것).
7. `rpcrt4!RpcpNormalizeSecurityDescriptor+0x210` (`RtlNewSecurityObjectEx` 직후 `eax`, 새 SD 는 `[rbp-0x41]`) 와 `+0x229` (`RpcpCopySecurityDescriptor` 직후 `eax == 0`).
8. `rpcrt4!PerformRpcInitialization+0x2b8` (`eax == 0`?), `+0x2d4` (`ReadPolicySettings` 직후 `eax`, 키 없음이면 0), `+0x412` 에서 `RpcHasBeenInitialized` 가 1 로 바뀌는지 (`ba w4 rpcrt4+0x104104`).
9. (확인 한 줄) `rpcrt4!PerformRpcInitialization+0x344` (`InitializeCellHeap`) 가 **안 걸리는지**: 걸리면 `\RPC Control\DSEC` 와 `NtCreateSection` 이 정상 길에 있다는 뜻.

## 다음 요청 후보

1. **ntdll `RtlpNewSecurityObject` 조건부 갈래** (depth 0, 위 질문 5 마지막 항목): 기본 SD (DACL 하나) + 부모 SD 로는 `NtPrivilegeCheck`·`NtDuplicateToken`·`NtAccessCheck`·`RtlAddMandatoryAce` 가 타는지, 부모 SD 의 DACL·SACL (라벨) ACE 가 새 SD 에 어떻게 들어가는지 (`RtlpInheritAcl2`).
2. **초기화 뒷부분 syscall 목록** (kernelbase `RegOpenKeyExW` / `RegGetValueW` 가 부르는 `Nt*Key*`, `WinSqmIsOptedIn`, `EtwEventRegister` 가 부르는 `NtTraceControl` 등): `PerformRpcInitialization +0x2cf` 이후 `+0x412` 까지.
3. `RpcpNormalizeSecurityDescriptor` 다른 caller 셋 (`SetAuthInformation +0x4f6`, `UpdateRpcInterfaceInformation +0x59`, `LRPC_FAST_BINDING_HANDLE +0x38a`) 이 어느 export 에서 닿는지 (지도, depth 0): 샘플 로그에서 초기화 밖에서 `\RPC Control` 이 열리는 경우의 출처.
4. smss 쪽 (`\RPC Control` 만들 때 SD, sast/94) 결과가 나오면 거기서 정해지는 SD 와 이 문서의 "부모 SD" 를 맞춰 보기 (Q181 에서 2 번 멈춤점의 값과 대조).

## 판정

**분석성공.** 요청서의 질문 5개에 코드 근거 (`+0x` 줄) 로 답함. 정적으로 못 정한 것 (진짜 SD 바이트, 토큰 질문들의 진짜 대답, `RtlpNewSecurityObject` 조건부 갈래, kernelbase 레지스트리 syscall 이름) 은 위 "동적으로 확인할 것" 과 "다음 요청 후보" 로 넘겼다.

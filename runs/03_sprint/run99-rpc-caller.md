# 2026-11-15 run99 — rpcrt4 가 `\RPC Control` 을 열어 SD 를 묻는 까닭과 `0xe` 가 나오는 줄 (`UuidToStringA` → `PerformRpcInitialization` → `InitializeDefaultSecurityDescriptor` → `RpcpNormalizeSecurityDescriptor` → `NtOpenDirectoryObject` · `NtQuerySecurityObject` ×2 → `RtlNewSecurityObjectEx`) (Q180c = Q180 ③, PE_emulator 요청 34 의 3 — 정적만): **`\RPC Control` 을 여는 rpcrt4 함수는 `RpcpNormalizeSecurityDescriptor` 하나다 (`NtOpenDirectoryObject` 가져오기 칸 참조 1곳 `+0x12a`). 하는 일은 "RPC 기본 보안 설명자 (`gDefaultSecurityDescriptor`) 만들기" — `\RPC Control` 디렉터리의 SD 를 **부모 SD 로 빌려** ntdll `RtlNewSecurityObjectEx` 에 통째로 넘긴다. rpcrt4 는 그 SD 안 (소유자 · ACE · 라벨) 을 **직접 안 읽는다.** 첫 `NtQuerySecurityObject(h, 0x17, NULL, 0, &len)` 은 **정확히 `0xC0000023` 일 때만** 다음으로 간다 (`+0x164 CMP EAX,0xc0000023` · `+0x169 JNZ`) — 0 이든 다른 실패든 `+0x2ed MOV EBX,0xe`. `0xe` 는 이 사슬의 상수이고 (NTSTATUS → Win32 변환 없음), `UuidToStringA` 는 `PerformRpcInitialization` 의 반환값을 안 건드리고 그대로 돌려준다. 그래서 에뮬레이터의 "status 0 + `len` 안 씀" 은 길이를 보기도 전에 `+0x169` 에서 실패한다. 통과하면 다음 syscall 은 ALPC 도 `\RPC Control\DSEC` 도 아니고 토큰 열기 · 토큰 묻기 (`RtlpNewSecurityObject` 안), 그다음 레지스트리 열기 (`ReadPolicySettings`) 다.**

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### Q180c 결론 (run99)

- **`0xe` 의 출처가 코드로 섰다** `[정적]` — run97 에서 `[추측]` 으로 둔 "크기만 묻기 대답이 `UuidToStringA → 0xe` 의 가장 가까운 원인" 이 맞다. 첫 `NtQuerySecurityObject` 대답 (지금 status 0) 이 `RpcpNormalizeSecurityDescriptor +0x169 JNZ` 에서 `+0x2ed` (`0xe`) 로 간다. 로그의 "`NtQuerySecurityObject` → `NtClose` → 반환, 그 사이 syscall 0" 이 `+0x2ed → +0x22b` 정리 길 (`CloseHandle(h)` 하나만 타고 나머지 해제는 0 이라 건너뜀) 과 줄마다 같다.
- **`len` 이 0 이라서가 아니다** `[정적]` — `len` 은 `+0x169` 비교를 지난 뒤에야 읽는다 (`+0x16f MOV ECX,[RBP-0x59]`). status 하나만 `0xC0000023` 으로 바꿔도 `len` 이 진짜 필요 길이가 아니면 둘째 부름에서 다시 실패한다 (아래 최소 조건 3 · 5).
- **rpcrt4 쪽에서 SD 안의 값은 아무것도 맞출 필요 없다** `[정적]` — 받은 SD 는 `+0x1fc MOV RCX,RDI` 로 그대로 `RtlNewSecurityObjectEx` 의 부모 SD 가 된다. 꼴 (자기상대형) 만 맞으면 된다. ntdll 이 부모 SD 를 어떻게 합치는지 (`RtlpInheritAcl2`) 는 이번에 안 따라감 `[공백]`.
- **다음 벽은 토큰** `[정적]` — 첫 대답을 고치면 `NtOpenThreadToken` (없음이 정상) → `NtOpenProcessToken` → `NtQueryInformationToken` 클래스 `10` · `4` · `0x19` · `5` · `6` · `0x29` 가 처음으로 나온다 (아래 최소 조건 6 ~ 13). `\RPC Control\DSEC` · ALPC 는 이 문제의 다음 벽이 **아니다**.
- **한 번 실패하면 export 마다 처음부터 다시** `[정적]` — `RpcHasBeenInitialized` (`0x180104104`) 는 맨 끝 `PerformRpcInitialization +0x412` 에서만 1 이 된다. 실패하면 그대로 0 이라 다음 export 도 `+0x39` 부터 같은 길을 되풀이한다 (에뮬레이터 로그의 "한 샘플에서 100번 넘게 되풀이" 와 맞는 코드).
- **같은 길로 들어오는 export 가 123개** `[정적]` — `PerformRpcInitialization` caller 140곳 중 export 이름과 맞는 것 123 (`UuidToStringA` · `UuidFromStringA` `+0xa9` · `UuidCreateSequential` `+0xd6` · `RpcStringBindingComposeW` `+0x115` · `RpcBindingFromStringBindingW` `+0x196` · `RpcServerRegisterIf*` · `I_RpcGetDefaultSD` 등, 결과서). advapi32 · winhttp · ws2_32 · shell32 · combase 가 이 중 무엇을 부르는지는 안 봄 `[공백]`.

### 최소 조건 표 — `InitializeDefaultSecurityDescriptor` 를 통과시키려면 (결과서 질문 5 옮김, flow-writer 가 rpcrt4 · ntdll `.asm` 줄 다시 봄)

| # | syscall | 인자 (코드 상수) | 꼭 줘야 하는 것 | 틀리면 |
|---|---|---|---|---|
| 1 | `NtQueryInformationToken` | `(-6, 0x1d, &값, 4, &len)` — `RtlGetAppContainerNamedObjectPath +0xb6` | 아무 status 나 됨. 값 칸은 안 써도 됨 (`+0x1e` 에서 미리 0). **값 ≠ 0 만 피하기** | 값 ≠ 0 (AppContainer) 이면 이름이 `<샌드박스 경로>\RPC Control` 로 바뀜 |
| 2 | `NtOpenDirectoryObject` | `(&h, 0x20001, OA{Length 0x30, Root 0, Name = \RPC Control (Length 0x18), Attr 0, SD · QoS 0})` — `RpcpNormalize +0x12a` | status ≥ 0, 쓸 수 있는 핸들 | 음수 → `0xe` (`+0x138 JS`) |
| 3 | `NtQuerySecurityObject` | `(h, 0x17, NULL, 0, &len)`, `len` 미리 0 (`+0x146`) — `+0x158` | **status = `0xC0000023` 정확히** + `*len` = **진짜 필요 길이** (run97: `0x14` 이상) | 다른 값 (0 포함) → `0xe` (`+0x169 JNZ`) |
| 4 | (`AllocWrapper` → `HeapAlloc`, syscall 아님) | 크기 = `*len` | NULL 아닌 포인터 | NULL → `0xe` (`+0x17d`) |
| 5 | `NtQuerySecurityObject` | `(h, 0x17, buf, *len, &len)` — `+0x19c` | status ≥ 0, `buf` = 자기상대형 SD (`Revision 1` · `Control` 에 `0x8000`, run97 "에뮬레이터용 규칙 한 장") | 음수 → `0xe` (`+0x1aa JS`) |
| 6 | `NtOpenThreadToken` | `(현재 스레드, 8, TRUE, &tok)` — kernelbase `OpenThreadToken +0x4` (`RpcpNormalize +0x1cc`) | `STATUS_NO_TOKEN` → `GetLastError` = `0x3f0` 이 되게 (kernelbase `BaseSetLastNTError` → `RtlNtStatusToDosError`), 또는 성공 | 다른 실패 → `+0x2bf JNZ` → `0xe` |
| 7 | `NtOpenProcessToken` | `(현재 프로세스, 8, &tok)` — kernelbase `OpenProcessToken +0x4` (`RpcpNormalize +0x2d9`) | 성공 + 핸들 | 실패 → `0xe` (`+0x2e7` 안 뜀) |
| 8 | `NtQueryInformationToken` | `(tok, 10, buf, 0x38, &len)` — `RtlpNewSecurityObject +0x189` | 성공, `TokenType` (칸 `+0x18`) = 1 (primary) | 음수 → 실패, 2 → 다른 길 (`+0x1a2 JZ`) |
| 9 | 〃 | `(tok, 4, buf, 0x4c)` — `RtlpGetDefaultsSubjectContext +0xbb` | 성공 (크기 안 물음 — `0x4c` 안에 다 들어가야) | 음수 → 실패 |
| 10 | 〃 | `(tok, 0x19, buf, 0x54)` — `+0x110` | 성공 (`0x54` 안) | 〃 |
| 11 | 〃 | `(tok, 5, NULL, 0)` → `0xC0000023` + 길이 (`+0x135`, `+0x13c CMP` · `+0x141 JNZ`), 다시 `(tok, 5, buf, n)` (`+0x183`) | 둘 다 위처럼 | 첫 대답이 `0xC0000023` 아니면 실패 |
| 12 | 〃 | `(tok, 6, NULL, 0)` → `0xC0000023` + 길이 (`+0x1a8`, `+0x1af CMP` · `+0x1b4 JNZ`), 다시 `(tok, 6, buf, n)` (`+0x1ee`) | 둘 다 위처럼 | 〃 |
| 13 | 〃 | `(-4, 0x29, buf, 0x4c)` (`RtlpGetDefaultTrustSubjectContext +0x6f`, `+0x56 MOV RCX,-4`) · `(tok, 0x29, …)` (`+0xa2`) | 성공 | 음수 → 실패 |
| 14 | `NtClose` | `(h)` `+0x23c`, `(tok)` `+0x25e` | 아무 | — |

- 클래스 번호의 이름 (`0x1d` = `TokenIsAppContainer`, `10` = `TokenStatistics`, `4` = `TokenOwner`, `0x19` = `TokenIntegrityLevel`, `5` = `TokenPrimaryGroup`, `6` = `TokenDefaultDacl`, `0x29` = `TokenProcessTrustLevel`) 와 `-6` · `-4` 가 "현재 스레드 유효 토큰" · "현재 프로세스 토큰" 가짜 핸들이라는 건 `[추측: 공개 문서]` — 숫자는 코드.
- 따로 안 따라간 조건부 syscall (`RtlpNewSecurityObject` callee 에 있음): `NtDuplicateToken` · `NtAccessCheck` · `NtPrivilegeCheck` 두 곳, `RtlAddMandatoryAce`. rpcrt4 기본 SD (DACL 하나, 소유자 · 그룹 · SACL 없음) 로는 안 탈 것으로 보인다 `[추측: 결과서]` → 동적.

### 지금 PE_emulator 맞나 · 틀리나 (결과서 "지금 우리" 줄 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

| 질문 | 진짜 (26100.9457) `[정적]` | 지금 우리 | 판정 |
|---|---|---|---|
| 1 누가 여나 | `RpcpNormalizeSecurityDescriptor +0x12a` 하나, 사슬은 위 제목 | 로그 순서 (`NtQueryInformationToken(-6,0x1d,4)` → `NtOpenDirectoryObject("\RPC Control", 0x20001)` → `NtQuerySecurityObject(h,0x17,NULL,0,&len)` → `NtClose`) 가 코드와 같음 | **맞음** |
| 2 SD 에서 무엇을 보나 | 두 번 묻기. 첫 대답은 `0xC0000023` 만 통과. SD 안은 안 읽음 | status 0 + `len` 안 씀 | **틀림** — 최소 조건 3 · 5 |
| 3 언제 `0xe` | `RpcpNormalize +0x2ed` · `+0x2a7`, `PerformRpcInitialization +0x425` 의 상수. `UuidToStringA` 는 옮기기만 | `0xe` 반환 | **맞음** (코드대로) |
| 4 통과 뒤 다음 syscall | 토큰 열기 · 토큰 묻기 (조건 6 ~ 13) → `ReadPolicySettings` 레지스트리 | 아직 안 나옴 | **다음 벽** — 구현 상태는 PE_emulator 쪽 확인 `[공백]` |

### 원문 질문이 어디서 답해졌나

| 요청서 질문 | 답한 곳 (아래 절) | 한 줄 |
|---|---|---|
| 1 누가 여나 | "사슬" · "`NtQueryInformationToken(-6, 0x1d)` 갈림" | `RpcpNormalizeSecurityDescriptor` 하나, 이 토큰 질문은 이름 앞 부분만 가른다 |
| 2 SD 에서 무엇을 보나 | "rpcrt4 쪽 검사 전부" | 두 번 묻기 맞음, `0xC0000023` 만 통과, SD 안은 안 읽음 |
| 3 언제 `0xe` | "`0xe` 는 어디서 정하나" | 상수, 변환 없음, `UuidToStringA` 는 그대로 옮김 |
| 4 다음 syscall | "통과하면 다음" | 토큰 → 레지스트리. DSEC 는 정책으로 켤 때만, ALPC 는 초기화 밖 |
| 5 최소 조건 | 위 표 | 3 만 정확히 + 5 가 SD 채워 성공 + 6 ~ 13 성공 |

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 사진 대조 없음 — test.exe 는 rpcrt4 를 안 올린다: 초기화 끝 (P2) 모듈 6개 `test.exe` · `ntdll.dll` · `KERNEL32.DLL` · `KERNELBASE.dll` · `dll1.dll` · `dll2.dll` (`ms/logs/04_baggage-read_P2.txt:3398 · 3503 · 3608 · 3713 · 3818 · 3923`) `[관찰]` run44. `rpcrt4.dll` 은 같은 로그의 `\KnownDlls` 섹션 목록에만 있다 (`_P2.txt:1392` Section `rpcrt4.dll`) `[관찰]` — 올라올 준비는 돼 있지만 안 올라온 것.
- 정적: **rpcrt4 pdb `0204A8A137C42B7DF1F0811A3A8277B21`** (GUID+Age, `ms/sym/rpcrt4.dll.pdb.info.txt`). **이번에 처음 받고 처음 analyze** (함수 3289, `ms/decomp/rpcrt4/_index.tsv`). 바이너리 `System32\rpcrt4.dll` x64, 1,162,656B.
  - 버전 대조: rpcrt4 는 "알려진 사실" 표에 줄이 없어 간접으로 (결과서) — 같은 `ms/bin` 폴더 ntdll `_calls.tsv` 가 알려진 사실과 일치, PDB 는 바이너리 CodeView GUID 로 받은 것, rpcrt4 `.asm` 의 `ret=` 와 `_calls.tsv` 의 `ret_off` 가 같음.
- ntdll · kernelbase: 다시 analyze 안 함. 읽기 전용 export 만 (결과서) — ntdll `RtlGetAppContainerNamedObjectPath` (rva `0x4e2b0`) · `RtlNewSecurityObjectEx` (`0x510a0`) · `RtlpNewSecurityObject` (`0x51750`, 7338B — 앞쪽과 syscall 자리만) · `RtlpGetDefaultsSubjectContext` (`0x51110`) · `RtlpGetDefaultTrustSubjectContext` (`0x51580`), kernelbase `OpenThreadToken` (`0x22cc0`) · `OpenProcessToken` (`0x23f00`).
- 에뮬레이터 쪽 숫자 (`r04 2ed5ce549014 seq 289 → 312`, 383 샘플, 되풀이 3137 번, 요청서 배경) 는 **PE_emulator 쪽 관찰**이라 이 문서의 `[관찰]` 이 아니다.
- 이 길은 **test.exe 초기화 지도 밖**이다 (rpcrt4 를 안 올림). 이번에 읽은 ntdll 함수 다섯도 ntdll `_map/LdrInitializeThunk_*` 에 0 건 (flow-writer 검색).

## 목적

- PE_emulator 요청 34 의 3 (대기열 Q180 을 나눈 **Q180c**, 사용자 결정 2026-11-15). 에뮬레이터에서 `UuidToStringA` 가 `0xe` 를 돌려주는 짧은 길 — **누가 `\RPC Control` 을 열고, 받은 SD 에서 무엇을 보고, 언제 `0xe` 로 가나**, 통과하면 다음은 무엇인가, 에뮬레이터가 줘야 할 최소 조건.
- 범위 밖: RPC 전송 · 엔드포인트 · ALPC 본문, NDR, 다른 export 본문 (이름만). advapi32 · winhttp · shell32 · combase 쪽 모듈은 안 봄. 앞 바퀴 짝: run97 (커널 묻기 길, Q180a), run98 (smss 가 만드는 SD, Q180b). 커널이 SD 를 객체에 붙이는 계산 (`SeAssignSecurityEx2`) 은 Q180d · sast/96 → run100 몫.

## 방법

- 정적: `_workspace/sast/95_rpc-caller.md` — rpcrt4 (새 모듈)
  - `fetch_pdb.py rpcrt4.dll` → `pdb_aliases.py` → `run_ghidra.sh analyze rpcrt4.dll` (약 1분).
  - 지도: `_map/UuidToStringA_*` (기준 20 — `PerformRpcInitialization` 은 caller 140곳이라 안 펼쳐짐) · `_map/PerformRpcInitialization_*` (기준 1000, 71 함수).
  - export: depth 1 `RpcpNormalizeSecurityDescriptor` · `InitializeDefaultSecurityDescriptor` · `UuidToStringA`, depth 0 `PerformRpcInitialization` · `GenerateSectionName` · `AllocateCellSection` · `InitializeCellHeap` · `InitializeEPMapperClient` · `ReadPolicySettings` · `InitializeRpcProtocolLrpc` · `TransformLrpcEndpointToAlpcPortName` · `AlpcConnect`.
  - `refs` 7: `0xeb868` (`\RPC Control\` 글) · `0xef3b8` (`\RPC Control\DSEC`) · `0xe96e8` (`NtOpenDirectoryObject` 가져오기 칸) · `0xe96e0` (`NtQuerySecurityObject` 칸) · `PerformRpcInitialization` · `RpcpNormalizeSecurityDescriptor` · `InitializeDefaultSecurityDescriptor`.
  - 범위를 넓힌 곳: ntdll 다섯 · kernelbase 둘 (위 "환경").
- 결과서: `_workspace/sast/95_rpc-caller_result.md` (**분석성공** — 못 닫은 것: 진짜 `\RPC Control` SD 바이트, 토큰 질문의 진짜 대답, `RtlpNewSecurityObject` 조건부 갈래, kernelbase 레지스트리 syscall 이름).
- flow-writer 가 다시 본 것 (`.asm` · `_refs` · `_map` · `pdb_aliases.py`):
  - rpcrt4 `RpcpNormalizeSecurityDescriptor` 전부 (`+0x0 ~ +0x326`), `UuidToStringA` 전부 (`+0x0 ~ +0x7c`), `PerformRpcInitialization +0xc` · `+0x2d` · `+0x59` · `+0x68` · `+0x2b3 ~ +0x2d4` · `+0x2f4 ~ +0x31e` · `+0x32e` · `+0x33a` · `+0x344` · `+0x34b` · `+0x412` · `+0x425`, `InitializeDefaultSecurityDescriptor +0x23` · `+0xe4` · `+0xfc` · `+0x119` · `+0x134` · `+0x13c` · `+0x141`, `RpcpCopySecurityDescriptor +0xa1` · `+0x104`.
  - ntdll `RtlGetAppContainerNamedObjectPath +0x1e` · `+0x8c` · `+0x93` · `+0x9f` · `+0xa2` · `+0xb6` · `+0xbd` · `+0xc1` · `+0xc5` · `+0x116`, `RtlNewSecurityObjectEx +0x59`, `RtlpNewSecurityObject +0x15b` · `+0x16f` · `+0x189` · `+0x1a2` · `+0x234` · `+0x35e ~ +0x366` + 조건부 callee 자리 (`grep`), `RtlpGetDefaultsSubjectContext +0x7d ~ +0x1ee` · `+0x203`, `RtlpGetDefaultTrustSubjectContext +0x56` · `+0x61` · `+0x6f` · `+0xa2`.
  - kernelbase `OpenThreadToken +0x0 ~ +0x28`, `OpenProcessToken +0x0 ~ +0x19`.
  - `_refs/0xe96e8.tsv` (1줄) · `0xe96e0.tsv` (2줄) · `RpcpNormalizeSecurityDescriptor.tsv` (5줄), `_map/PerformRpcInitialization_summary.txt` · `_edges.tsv` 의 CFG 간접 줄.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/rpcrt4/_calls.tsv` · `.asm`, ntdll · kernelbase `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `rpcrt4!UuidToStringA` (rva `0x601e0`) | `+0x31` | `+0x36` | `PerformRpcInitialization` (rva `0x5f784`) | `+0x26 CMP [RpcHasBeenInitialized],0` · `+0x2f JNZ` — 이미 켜졌으면 건너뜀. 돌아와 0 이 아니면 `+0x3a ~ +0x5b` 에서 EAX 그대로 반환 | [정적] |
| `rpcrt4!PerformRpcInitialization` | `+0x59` | `+0x60` | `LoadLibraryExW(L"rpcrt4.dll")` | NULL 이면 `+0x68 JZ → +0x425` (`0xe`) — 앞 단계 하나 (나머지 결과서) | [정적] |
| 〃 | **`+0x2b3`** | **`+0x2b8`** | **`InitializeDefaultSecurityDescriptor`** (rva `0x99190`) | 0 이 아니면 `+0x2ba JNZ → +0x425` (`0xe`) | [정적] |
| 〃 | `+0x2c0` | `+0x2c5` | `InitializeEPMapperClient` | 통과 뒤. syscall 없음 (결과서) | [정적] |
| 〃 | `+0x2cf` | `+0x2d4` | `ReadPolicySettings` | 통과 뒤 다음 syscall — 레지스트리 열기 (결과서) | [정적] |
| 〃 | `+0x344` | `+0x349` | `InitializeCellHeap` | 디버그 정보 정책이 켜졌을 때만 (`+0x32e` · `+0x33a`) — `\RPC Control\DSEC` 쪽 | [정적] |
| `rpcrt4!InitializeDefaultSecurityDescriptor` | **`+0x13c`** | **`+0x141`** | **`RpcpNormalizeSecurityDescriptor`** (rva `0x38448`) | `(만든 SD, EDX = 1 (+0x134), &gDefaultSecurityDescriptor)`, 반환 그대로 (`+0x141 JMP`) | [정적] |
| `rpcrt4!RpcpNormalizeSecurityDescriptor` | `+0x88` | `+0x8f` | `wcsstr` | `(L"" (0x1800eb970), L"\RPC Control\")` → 늘 NULL | [정적] |
| 〃 | `+0xa8` | `+0xaf` | ntdll `RtlGetAppContainerNamedObjectPath` | `(0, 0, 0, &us)` | [정적] |
| 〃 | `+0xd2` | `+0xd7` | `DuplicateString3` | 이름 = 앞 + `\RPC Control\` (보통 앞이 빈 글) | [정적] |
| 〃 | **`+0x12a`** | **`+0x131`** | **`NtOpenDirectoryObject`** | `(&h, 0x20001 (+0x11c), OA)` — 이름 길이 2 깎음 (`+0x105` · `+0x111`) → `\RPC Control` | [정적] |
| 〃 | **`+0x158`** | **`+0x15f`** | **`NtQuerySecurityObject`** | `(h, 0x17, NULL, 0, &len)`. `+0x164 CMP EAX,0xc0000023` · `+0x169 JNZ → +0x2ed` | [정적] |
| 〃 | `+0x172` | `+0x177` | `AllocWrapper` | `(len)` — `+0x17d JZ → +0x2ed` | [정적] |
| 〃 | **`+0x19c`** | **`+0x1a3`** | **`NtQuerySecurityObject`** | `(h, 0x17, buf, len, &len)` — `+0x1aa JS → +0x2ed` | [정적] |
| 〃 | `+0x1cc` | `+0x1d3` | `OpenThreadToken` | `(GetCurrentThread(), 8, TRUE, &tok)` — 0 이면 `+0x1da JZ → +0x2ae` | [정적] |
| 〃 | `+0x2ae` | `+0x2b5` | `GetLastError` | `+0x2ba CMP EAX,0x3f0` · `+0x2bf JNZ → +0x2ed` | [정적] |
| 〃 | `+0x2d9` | `+0x2e0` | `OpenProcessToken` | `(GetCurrentProcess(), 8, &tok)` — 성공이면 `+0x2e7 JNZ → +0x1e0` | [정적] |
| 〃 | **`+0x209`** | **`+0x210`** | **ntdll `RtlNewSecurityObjectEx`** | `(부모 = buf (+0x1fc), 만든이 = 기본 SD, &새SD, NULL, FALSE, 1, tok, 일반 매핑)` — `+0x217 JS → +0x2ed` | [정적] |
| 〃 | `+0x224` | `+0x229` | `RpcpCopySecurityDescriptor` | `(새SD, &gDefaultSecurityDescriptor)` — 반환 그대로 | [정적] |
| 〃 | `+0x23c` · `+0x25e` | `+0x243` · `+0x265` | `CloseHandle` | `h` · `tok` (각각 0 이면 건너뜀) | [정적] |
| 〃 | `+0x275` | `+0x27c` | `RtlDeleteSecurityObject` | 새 SD 가 있을 때만 (`+0x26a`) | [정적] |
| `ntdll!RtlGetAppContainerNamedObjectPath` | `+0xb6` | `+0xbb` | `NtQueryInformationToken` | `(-6 (+0x8c · +0xa2), 0x1d (+0x9f), &값, 4 (+0x93), &len)` | [정적] |
| 〃 | `+0x116` | `+0x11b` | `RtlpGetTokenNamedObjectPath` | 값 ≠ 0 (AppContainer) 일 때만 | [정적] |
| `ntdll!RtlNewSecurityObjectEx` | `+0x59` | `+0x5e` | `RtlpNewSecurityObject` | | [정적] |
| `ntdll!RtlpNewSecurityObject` | `+0x189` | `+0x18e` | `NtQueryInformationToken` | `(tok, 10 (+0x16f), buf, 0x38 (+0x15b))` | [정적] |
| 〃 | `+0x234` | `+0x239` | `RtlpGetDefaultsSubjectContext` | | [정적] |
| `ntdll!RtlpGetDefaultsSubjectContext` | `+0xbb` · `+0x110` · `+0x135` · `+0x183` · `+0x1a8` · `+0x1ee` | `+0xc0` · `+0x115` · `+0x13a` · `+0x188` · `+0x1ad` · `+0x1f3` | `NtQueryInformationToken` ×6 | 클래스 4 · `0x19` · 5 (크기) · 5 · 6 (크기) · 6 | [정적] |
| 〃 | `+0x203` | `+0x208` | `RtlpGetDefaultTrustSubjectContext` | | [정적] |
| `ntdll!RtlpGetDefaultTrustSubjectContext` | `+0x6f` · `+0xa2` | `+0x74` · `+0xa7` | `NtQueryInformationToken` ×2 | `(-4, 0x29, …, 0x4c)` · `(tok, 0x29, …)` | [정적] |
| `kernelbase!OpenThreadToken` | `+0x4` | `+0xb` | `NtOpenThreadToken` | 실패면 `+0x16 BaseSetLastNTError` 뒤 0 반환 | [정적] |
| `kernelbase!OpenProcessToken` | `+0x4` | `+0xb` | `NtOpenProcessToken` | | [정적] |

- `NtOpenDirectoryObject` · `NtQuerySecurityObject` 는 rpcrt4 가 ntdll 에서 **직접 가져온다** (`__imp_` 칸 `0xe96e8` · `0xe96e0`). 두 칸을 읽는 줄은 rpcrt4 전체에서 위 3곳 (`+0x12a` · `+0x158` · `+0x19c`) 뿐이다 (`_refs`).
- `RpcpNormalizeSecurityDescriptor` 를 부르는 곳은 4곳 — `InitializeDefaultSecurityDescriptor +0x13c` (초기화), `SetAuthInformation +0x4f6`, `UpdateRpcInterfaceInformation +0x59`, `LRPC_FAST_BINDING_HANDLE +0x38a` (`_refs`). 뒤 셋은 초기화 밖 (바인딩에 인증을 붙일 때 · 서버 인터페이스 등록 등 `[추측: 이름]`) 이다. 에뮬레이터 로그에서 `\RPC Control` 열기가 초기화 말고 다른 때에도 보이면 이쪽이다.

### `NtQueryInformationToken(-6, 0x1d)` 갈림 — 이름 앞 부분만 가른다 (결과서 질문 1)

| 결과 | ntdll 쪽 | rpcrt4 쪽 이름 |
|---|---|---|
| status < 0 | `+0xbd JS` → 그 status 반환 | `+0xb6 JS → +0xc5` → `\RPC Control\` |
| status ≥ 0, 값 = 0 | `+0xc1 CMP` · `+0xc5 JNZ` 안 감 → 빈 문자열, status 0 | Buffer 0 (`+0xb8` · `+0xbf JNZ` 안 감) → `+0xc5` → `\RPC Control\` |
| status ≥ 0, 값 ≠ 0 | `+0xc5 JNZ` → `+0x116 RtlpGetTokenNamedObjectPath` | `+0xbf JNZ → +0x304` → `<경로>\RPC Control\` |

- 그래서 일반 프로세스에선 **어느 대답이든 이름은 `\RPC Control`** (끝 `\` 는 `+0x111` 에서 뗌). 이 질문의 대답은 통과 · 실패를 안 가른다. 값 칸은 ntdll 이 `+0x1e` 에서 미리 0 으로 지워 둔다.
- 같은 호출이 `InitializeCellHeap +0x29` 에도 있다 (결과서 — 정책 값이 있을 때만).

### rpcrt4 쪽 검사 전부 — 실패하면 어디로 (결과서 질문 2)

| 읽는 것 | 줄 | 통과 조건 | 실패하면 |
|---|---|---|---|
| `NtOpenDirectoryObject` status | `+0x136 TEST` · `+0x138 JS` | ≥ 0 | `+0x2ed` `0xe` |
| 첫 `NtQuerySecurityObject` status | `+0x164 CMP EAX,0xc0000023` · `+0x169 JNZ` | **정확히 `0xC0000023`** | `+0x2ed` (**0 이어도**) |
| `AllocWrapper(len)` | `+0x17a TEST` · `+0x17d JZ` | NULL 아님 | `+0x2ed` |
| 둘째 `NtQuerySecurityObject` status | `+0x1a8 TEST` · `+0x1aa JS` | ≥ 0 | `+0x2ed` |
| `OpenThreadToken` | `+0x1d8 TEST` · `+0x1da JZ` | 참 → `+0x1e0` | `+0x2ae GetLastError` → `+0x2ba CMP 0x3f0` 일 때만 프로세스 토큰, 아니면 `+0x2ed` |
| `OpenProcessToken` | `+0x2e5 TEST` · `+0x2e7 JNZ → +0x1e0` | 참 | 떨어져서 `+0x2ed` |
| `RtlNewSecurityObjectEx` status | `+0x215 TEST` · `+0x217 JS` | ≥ 0 | `+0x2ed` |
| `DuplicateString3` (이름) | `+0xda TEST` · `+0xdd JZ → +0x2a7` | NULL 아님 | `+0x2a7 MOV EBX,0xe` |
| `RpcpCopySecurityDescriptor` | `+0x229 MOV EBX,EAX` | 그대로 | 그 값 (`RpcpCopySecurityDescriptor +0xa1 0x53a` · `+0x104 0xe`) |

- **SD 안을 읽는 함수 호출이 없다** — `RpcpNormalizeSecurityDescriptor` callee 에 `RtlGetOwnerSecurityDescriptor` · `RtlGetDaclSecurityDescriptor` · `RtlGetSaclSecurityDescriptor` · `RtlGetAce` · `RtlEqualSid` · `RtlValidSid` 0 (결과서 callee 목록, flow-writer 가 `.asm` 전부 읽어 같음). 받은 버퍼는 `+0x1fc MOV RCX,RDI` 로 `RtlNewSecurityObjectEx` 첫 인자 (부모 SD).
- **rpcrt4 가 스스로 만드는 SD** (`InitializeDefaultSecurityDescriptor`, "만든이" 쪽): `S-1-5-7` SID (`+0x23` 바이트 `0x101` …, 이름 "익명 로그온" 은 `[추측: 지식]`) 에 `AddAccessAllowedAce(acl, 2, 0x80000000 = GENERIC_READ, sid)` (`+0xe4`) → `InitializeSecurityDescriptor` (`+0xfc`) → `SetSecurityDescriptorDacl(sd, TRUE, acl, FALSE)` (`+0x119`). 소유자 · 그룹 · SACL 없음 — 새 SD 의 소유자 · 그룹은 토큰에서 온다 (아래 클래스 4 · 5).
- 일반 매핑 (`RtlNewSecurityObjectEx` 마지막 인자, `+0x1e0`): Read `0x20001` · Write `0x20001` · Execute `0x20000` · All `0x1f0001` (`+0x34` · `+0x4c ~ +0x6a`).
- **ntdll 이 부모 SD 를 읽는 만큼** (`RtlpNewSecurityObject`): 검증 함수 안 부르고 `Control` (`+2`) 을 직접 본다 — `+0x35e MOVZX ECX,[R11+2]` · `+0x363 TEST CL,0x10` (`SE_SACL_PRESENT`) · `+0x366 JNZ`. 그 뒤 SACL · DACL 합치기 (`RtlpInheritAcl2`, 부르는 자리 `+0x45c` · `+0x61d` · `+0x956` · `+0xb4b`) 는 안 따라감 `[공백]`.

### `0xe` 는 어디서 정하나 (결과서 질문 3)

- **`UuidToStringA` 는 안 만든다** — `+0x36 TEST EAX,EAX` · `+0x38 JZ +0x5d`, 0 이 아니면 `+0x3a ~ +0x5b` (쿠키 검사 · 에필로그) 로 떨어지며 EAX 를 안 건드린다.
- **`0xe` 는 상수 세 곳**: `RpcpNormalizeSecurityDescriptor +0x2ed` (NT 함수 · 토큰 · 할당 실패 전부) · `+0x2a7` (이름 만들기 실패), `PerformRpcInitialization +0x425` (`+0x2ba JNZ` — `InitializeDefaultSecurityDescriptor` 가 0 이 아닌 **어떤 값이든**, 그리고 `LoadLibraryExW` · `GetBasicSystemInfo` · 할당 · 컴퓨터 이름 실패 등 앞 단계, 결과서 표). `InitializeDefaultSecurityDescriptor` 는 `RpcpNormalize` 반환을 그대로 옮긴다 (`+0x141 JMP`).
- **NTSTATUS → Win32 변환 없음** — 이 사슬에 `RtlNtStatusToDosError` 호출이 없다. NTSTATUS 를 받는 자리는 부호 · 비교만 하고 값은 버린다. 예외 하나: kernelbase `OpenThreadToken` 이 `+0x16 BaseSetLastNTError` 로 바꾼 오류 코드를 rpcrt4 `+0x2ba` 가 `0x3f0` 과 비교.
- `0xe` = 14 = `ERROR_OUTOFMEMORY` (= `RPC_S_OUT_OF_MEMORY`) 는 `[추측: 지식]` — 코드엔 이름이 없다. 여기선 "이유와 상관없이 초기화 실패" 한 값이다.
- **`RpcHasBeenInitialized` 는 `PerformRpcInitialization +0x412` 에서만 1** (`MOV dword [0x180104104],1`). 입구 `+0xc` · `+0x2d` 에서 두 번 확인 (락 앞뒤). 실패하면 0 그대로라 다음 export 가 다시 처음부터.

### 통과하면 다음 (결과서 질문 4)

| 순서 | 어디 | syscall |
|---|---|---|
| ① | `RpcpNormalize +0x1cc` → kernelbase `OpenThreadToken +0x4` | `NtOpenThreadToken(현재 스레드, 8, TRUE)` |
| ② | (없음 `0x3f0` 일 때) `+0x2d9` → `OpenProcessToken +0x4` | `NtOpenProcessToken(현재 프로세스, 8)` |
| ③ | `+0x209 RtlNewSecurityObjectEx +0x59` → `RtlpNewSecurityObject +0x189` | `NtQueryInformationToken(tok, 10, 0x38)` — `TokenType` (`[RBP+0x138]`) 2 면 `+0x1a2 JZ` 다른 길 |
| ④ | `RtlpNewSecurityObject +0x234` → `RtlpGetDefaultsSubjectContext` | 클래스 4 (`0x4c`, 한 번에) → `0x19` (`0x54`) → 5 (크기 → 다시) → 6 (크기 → 다시) |
| ⑤ | `RtlpGetDefaultsSubjectContext +0x203` → `RtlpGetDefaultTrustSubjectContext` | `(-4, 0x29, 0x4c)` → `(tok, 0x29, …)` |
| ⑥ | `RpcpNormalize +0x23c` · `+0x25e` | `NtClose(h)` · `NtClose(tok)` |
| ⑦ | `PerformRpcInitialization +0x2cf ReadPolicySettings` | `RegOpenKeyExW(HKLM, "Software\Policies\Microsoft\Windows NT\Rpc", 0, 0x20019)` (`ReadPolicySettings +0x44`, 결과서). 오류 2 · 5 는 정상으로 침, 그 밖은 `0xe`. 어느 `Nt*Key*` 인지는 kernelbase 안이라 안 따라감 `[공백]` |

- 그 뒤 `+0x2fb SbSelectProcedure` → (돌려준 주소가 0 이 아니면) `+0x313` CFG 간접 호출 (대상은 실행 때) → `WinSqmIsOptedIn` · ETW 등록 · `GetRpcIdleTimerWindow` → `+0x412` 완료 (결과서 + flow-writer `_map` 간접 줄). 이 뒤 syscall 이름은 안 따라감.
- **ALPC 는 이 초기화에 없다** — `_map/PerformRpcInitialization_*` 71 함수에 `NtAlpc*` · `NtCreateSection` 0 (결과서 `grep`). ALPC 는 `AlpcConnect` (`NtAlpcConnectPortEx` · `NtAlpcConnectPort`) 와 서버 쪽에서만 (이름만).
- **`\RPC Control\DSEC`** 는 `GenerateSectionName +0x25` 하나가 쓴다 → `AllocateCellSection +0x13f NtCreateSection` (이름 = `\RPC Control\DSEC` + 숫자). 이 길은 `PerformRpcInitialization +0x32e` (`g_fServerSideDebugInfoEnabled`) / `+0x33c` (`g_fClientSideDebugInfoEnabled`) 가 켜졌을 때만 `+0x344 InitializeCellHeap` 으로 간다. 두 값은 `ReadPolicySettings` 가 정책 레지스트리로 켠다 — 보통은 꺼짐 `[추측: 결과서, 전역 처음 값 0]`. `CellHeap` 생성자에서 `AllocateCell*` 까지 닿는지는 지도에 안 나옴 `[공백]`.

### 호출 관계

```
rpcrt4!UuidToStringA (0x601e0)                                                              [정적]
 │  +0x26  RpcHasBeenInitialized == 0 ?
 └─ +0x31  PerformRpcInitialization (0x5f784)              ← export 123개가 입구에서 부름
      │  +0x21 락 · +0x59 LoadLibraryExW("rpcrt4.dll") · +0x73 GetBasicSystemInfo · … · +0x299 IFEO
      ├─ +0x2b3  InitializeDefaultSecurityDescriptor (0x99190)       0 아니면 +0x2ba → +0x425 (0xe)
      │     │  +0xe4 AddAccessAllowedAce(S-1-5-7, GENERIC_READ) · +0x119 SetSecurityDescriptorDacl
      │     └─ +0x13c  RpcpNormalizeSecurityDescriptor (0x38448) (sd, 1, &gDefaultSecurityDescriptor)
      │           ├─ +0xa8   ntdll!RtlGetAppContainerNamedObjectPath(0,0,0,&us)
      │           │            └─ +0xb6  NtQueryInformationToken(-6, 0x1d, 4)      → 이름 앞 부분만
      │           ├─ +0xd2   DuplicateString3 → "\RPC Control\" · +0x111 길이 -2
      │           ├─ +0x12a  NtOpenDirectoryObject(&h, 0x20001, "\RPC Control")    음수 → +0x2ed
      │           ├─ +0x158  NtQuerySecurityObject(h, 0x17, NULL, 0, &len)
      │           │            +0x164 == 0xC0000023 ?  아니면 → +0x2ed (0xe)        ← 에뮬레이터가 멈춘 곳
      │           ├─ +0x172  AllocWrapper(len)
      │           ├─ +0x19c  NtQuerySecurityObject(h, 0x17, buf, len, &len)
      │           ├─ +0x1cc  OpenThreadToken(8) → NtOpenThreadToken
      │           │     (0x3f0 이면) +0x2d9 OpenProcessToken(8) → NtOpenProcessToken
      │           ├─ +0x209  ntdll!RtlNewSecurityObjectEx(부모 = buf, 만든이 = sd, …, tok)
      │           │            └─ +0x59  RtlpNewSecurityObject
      │           │                  ├─ +0x189  NtQueryInformationToken(tok, 10)
      │           │                  ├─ +0x234  RtlpGetDefaultsSubjectContext   (4 · 0x19 · 5 · 5 · 6 · 6)
      │           │                  │            └─ +0x203  RtlpGetDefaultTrustSubjectContext  (0x29 · 0x29)
      │           │                  └─ (조건부, 안 따라감) RtlpInheritAcl2 · NtDuplicateToken · NtAccessCheck · NtPrivilegeCheck · RtlAddMandatoryAce
      │           ├─ +0x224  RpcpCopySecurityDescriptor → gDefaultSecurityDescriptor
      │           └─ +0x22b  정리: FreeWrapper · +0x23c CloseHandle(h) · +0x25e CloseHandle(tok) · +0x275 RtlDeleteSecurityObject
      ├─ +0x2c0  InitializeEPMapperClient
      ├─ +0x2cf  ReadPolicySettings → RegOpenKeyExW(HKLM\Software\Policies\Microsoft\Windows NT\Rpc)
      ├─ +0x2fb  SbSelectProcedure → +0x313 CFG 간접 (대상 실행 때)
      ├─ (정책 켜짐일 때만) +0x344  InitializeCellHeap ⇢ … AllocateCellSection → GenerateSectionName ("\RPC Control\DSEC") → NtCreateSection
      ├─ … ETW 등록 · GetRpcIdleTimerWindow
      └─ +0x412  RpcHasBeenInitialized = 1                          (실패 길 +0x425 MOV EBX,0xe)

같은 디렉터리의 출처 (run98): smss!SmpConfigureObjectDirectories +0xf1 NtCreateDirectoryObject (Liberal SD)
커널 대답 규칙 (run97):       nt!NtQuerySecurityObject +0x1e2 *LengthNeeded · SeQuerySecurityDescriptorInfo +0x3a2 0xC0000023
```

## 확인된 것

- `[정적]` `\RPC Control` 을 `NtOpenDirectoryObject` 로 여는 rpcrt4 함수는 `RpcpNormalizeSecurityDescriptor` 하나 (`+0x12a`, 가져오기 칸 참조 1곳). 접근 `0x20001`, 이름은 `\RPC Control\` 에서 끝 `\` 를 뗀 `\RPC Control`.
- `[정적]` 초기화 사슬: `UuidToStringA +0x31` → `PerformRpcInitialization +0x2b3` → `InitializeDefaultSecurityDescriptor +0x13c` → `RpcpNormalizeSecurityDescriptor`. `RpcpNormalizeSecurityDescriptor` 의 다른 caller 셋은 초기화 밖.
- `[정적]` `NtQuerySecurityObject` 는 두 번 (`+0x158` 크기, `+0x19c` 버퍼). 첫 대답은 정확히 `0xC0000023` 만 통과, 0 포함 그 밖은 `0xe`.
- `[정적]` rpcrt4 는 SD 안을 안 읽는다 — 받은 SD 를 부모로 `RtlNewSecurityObjectEx` 에 넘겨 새 기본 SD 를 만들고, `RpcpCopySecurityDescriptor` 로 `gDefaultSecurityDescriptor` 에 복사한다.
- `[정적]` `0xe` 는 `RpcpNormalize +0x2ed` · `+0x2a7`, `PerformRpcInitialization +0x425` 의 상수. `UuidToStringA` 는 그대로 옮긴다. NTSTATUS → Win32 변환 없음.
- `[정적]` `NtQueryInformationToken(-6, 0x1d, 4)` 는 ntdll `RtlGetAppContainerNamedObjectPath +0xb6` 이 부른다. 값이 0 이 아닐 때만 이름이 바뀌고, status 는 통과 · 실패를 안 가른다.
- `[정적]` 통과 뒤 다음 syscall 은 `NtOpenThreadToken` → (없으면) `NtOpenProcessToken` → `NtQueryInformationToken` 여러 클래스 → `NtClose` ×2 → `ReadPolicySettings` 레지스트리. ALPC · `\RPC Control\DSEC` 는 이 초기화 정상 길에 없다.
- `[정적]` `RpcHasBeenInitialized` 는 성공 끝 (`+0x412`) 에서만 켜진다.
- `[관찰]` run44: test.exe 는 rpcrt4 를 안 올린다 (P2 모듈 6개). `rpcrt4.dll` 은 `\KnownDlls` 섹션으로만 있다 (`_P2.txt:1392`).
- 태그 바뀜: run97 "`0xC0000023` 대신 0 을 준 게 `UuidToStringA → 0xe` 의 가장 가까운 원인" `[추측]` → **`[정적]`** (run99, `RpcpNormalize +0x164` · `+0x169` · `+0x2ed`).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① (다르게) 결과서 질문 4 표 ⑤ 의 "`+0x203/+0x208` → `RtlpGetDefaultTrustSubjectContext`" 는 부르는 함수 이름이 빠져 있다. 부르는 쪽은 **`RtlpGetDefaultsSubjectContext +0x203`** 이다 (④ 와 같은 함수 안 — `RtlpGetDefaultsSubjectContext.asm` 135줄. `RtlpNewSecurityObject.asm` 에는 이 callee 가 없다).
- ② (다르게) 결과서 "동적으로 확인할 것" 6 의 `RtlpNewSecurityObject+0x461` · `+0x1121` · `+0x144a` 는 **ret 자리** (call 뒤) 이고 `+0x1498` 만 call 자리다. "부르기 직전" 에 걸려면 call 자리 `+0x45c` (`RtlpInheritAcl2`) · `+0x111c` (`NtDuplicateToken`) · `+0x1445` (`NtPrivilegeCheck`) · `+0x1498` (`RtlAddMandatoryAce`). 같은 callee 를 부르는 자리가 더 있다 — `RtlpInheritAcl2` `+0x61d` · `+0x956` · `+0xb4b`, `NtAccessCheck` `+0x116b`, `NtPrivilegeCheck` `+0x1b48`. "안 걸리면 안 탄 것" 으로 판정하려면 이 자리를 모두 (또는 callee 입구에 조건부로) 걸어야 한다.
- ③ (작게) 결과서 "`OpenThreadToken +0x14/+0x16` → `BaseSetLastNTError`" — call 은 `+0x16` (ret `+0x1b`), `+0x14` 는 `MOV ECX,EAX`.
- ④ (보탬) `PerformRpcInitialization +0x313` 은 **CFG 간접 호출** — `+0x2fb SbSelectProcedure(0xabababab, 0)` 가 돌려준 주소를 `+0x307` 에 저장하고 0 이 아니면 부른다 (`_map/PerformRpcInitialization_edges.tsv` `indirect-cfg`). 결과서는 `SbSelectProcedure` 이름만. 대상은 실행 때 → 동적.
- ⑤ (보탬) `RpcpNormalizeSecurityDescriptor` `refs` 에는 caller 4곳 말고 **DATA 1줄** (`0x106e48`, 함수 밖) 이 더 있다 — 예외 · 함수 범위 표 칸일 것 `[추측]`. caller 결론엔 영향 없음.
- ⑥ (보탬) `rpcrt4.dll` 은 test.exe 에 안 올라오지만 run44 `\KnownDlls` 섹션 목록에 있다 (`ms/logs/04_baggage-read_P2.txt:1392`) `[관찰]`.
- ⑦ (도구) `_calls.tsv` 의 `alias:PerformRpcInitialization+1 ; ALIASES(1): ?PerformRpcInitialization@@YAJXZ` 꼴은 같은 함수의 C++ 꾸밈 이름 하나다 — `pdb_aliases.py --lookup rpcrt4.dll` 로 `0x5f784` · `0x99190` · `0x38448` · `0x3877c` · `0x390f0` · `0x4fdc0` 모두 "별칭 무리 아님". 그래서 이 문서는 이름을 그대로 쓴다.
- 정정 없음 (이전 run 문서가 틀린 곳 없음). run97 의 `[추측]` 한 줄은 이번에 `[정적]` 으로 섰다 (위 "확인된 것" 끝).

## run98 에 보탠 것 (run98 해설서가 짚은 둘 — 이번 바퀴에 같이 봄)

- ① flow.md 2.4 run65 절 (run98) 줄의 "`KnownDllPath` 링크는 디렉터리를 새로 만들었을 때만 — 이미 있으면 `0x40000000` 으로 건너뜀, `.c`" 는 **`.asm` 으로도 맞다**: `smss!SmpOpenKnownDllsHandles +0x19d CMP EAX,0x40000000` · `+0x1a2 JZ → +0x219` (`+0x1fc NtCreateSymbolicLinkObject` 를 건너뛰어 핸들만 넘김). 그 뒤 `+0x1a4 TEST` · `+0x1a6 JNS → +0x1c2` (성공이면 링크 만들기), 음수면 `SmpLogFailure`. run98 문서 "결과서와 다르게 적은 곳 · 보탠 것" 에 ⑤ 한 줄로 보탰고, flow.md 줄의 근거를 `.c` → `.asm` 으로 고쳤다.
- ② flow.md 3장 run98 절 비유 "주인 칸은 … 시청 (커널) 이 채운다" 옆에 `[추측]` 을 붙였다 — run98 에선 "smss 가 비운 소유자 · 그룹을 커널이 채운다" 가 `[추측]` 이다 (Q180d · sast/96 → run100 에서 정적으로 닫힐 예정).

## 아직 확인 필요

**동적** (결과서 "동적으로 확인할 것" 1 ~ 9 + 보탬. **Q181 에 붙일 것**, VM 은 이번에 안 함). **test.exe 로는 못 본다** (rpcrt4 를 안 올림 `[관찰]` run44) — `UuidToStringA` 나 `RpcStringBindingComposeW` 를 부르는 샘플이 따로 필요하다. 커널 쪽 멈춤점은 프로세스 조건 필수.

- ① `rpcrt4!RpcpNormalizeSecurityDescriptor+0x12a` (`NtOpenDirectoryObject` 직전): `rdx` = `0x20001`, `!ustr` 로 `[r8+0x10]` 이름이 `\RPC Control` (Length `0x18`) 인지.
- ② `+0x15f` (첫 `NtQuerySecurityObject` 뒤): `eax == c0000023`, `dwo(@rbp-0x59)` = 필요 길이. **`+0x1a3`** (둘째 뒤): `eax` 0, `db @rdi L?dwo(@rbp-0x59)` · `!sd @rdi` — **진짜 `\RPC Control` SD 바이트를 유저 쪽에서 뜬다** (run98 Liberal 표 · run97 바이트 규칙과 대조).
- ③ `+0x1d3` (`OpenThreadToken` 뒤 `eax` 0 ?) · `+0x2b5` (`GetLastError` 뒤 `eax == 0x3f0` ?).
- ④ `ntdll!RtlGetAppContainerNamedObjectPath+0xbb`: `eax`, `dwo(@rsp+0x30)` 이 0 인지.
- ⑤ `ntdll!RtlpNewSecurityObject+0x18e` (`eax`, `dwo(@rbp+0x138)` = `TokenType`), `ntdll!RtlpGetDefaultsSubjectContext+0xc0` · `+0x115` · `+0x13a` · `+0x188` · `+0x1ad` · `+0x1f3` (각 `eax` — `+0x13a` · `+0x1ad` 는 `0xC0000023`), `RtlpGetDefaultTrustSubjectContext+0x74` · `+0xa7`.
- ⑥ 조건부 syscall 이 이 입력에서 타는지 — 위 "다르게 ②" 의 **call 자리 전부** (`RtlpNewSecurityObject+0x45c` · `+0x61d` · `+0x956` · `+0xb4b` · `+0x111c` · `+0x116b` · `+0x1445` · `+0x1498` · `+0x1b48`).
- ⑦ `rpcrt4!RpcpNormalizeSecurityDescriptor+0x210` (`RtlNewSecurityObjectEx` 뒤 `eax`, 새 SD `poi(@rbp-0x41)` → `!sd`) · `+0x229` (`eax == 0`).
- ⑧ `rpcrt4!PerformRpcInitialization+0x2b8` (`eax == 0`) · `+0x2d4` (`ReadPolicySettings` 뒤 `eax`) · `+0x412` 에서 `RpcHasBeenInitialized` 가 1 로 (`ba w4 rpcrt4+0x104104`).
- ⑨ `rpcrt4!PerformRpcInitialization+0x344` (`InitializeCellHeap`) 가 **안 걸리는지** — 걸리면 `\RPC Control\DSEC` · `NtCreateSection` 이 정상 길에 있다는 뜻.
- ⑩ (보탬) `+0x313` CFG 간접 호출의 대상 — `+0x307` 에 저장한 `SbSelectProcedure` 반환값 (`ln poi(rpcrt4+0x103e08)`).

**정적** (결과서 "다음 요청 후보" + 보탬, 대기열 번호는 오케스트레이터가 붙임):

- ① ntdll `RtlpNewSecurityObject` 조건부 갈래 — rpcrt4 기본 SD (DACL 하나) + 부모 SD 로 `NtPrivilegeCheck` · `NtDuplicateToken` · `NtAccessCheck` · `RtlAddMandatoryAce` 가 타는지, 부모 SD 의 DACL · SACL (라벨) ACE 가 새 SD 에 어떻게 들어가는지 (`RtlpInheritAcl2`). run98 Liberal 의 상속 전용 ACE 넷 (`0x0b`) 과 라벨 ACE 가 입력이 된다.
- ② 초기화 뒷부분 syscall 목록 — kernelbase `RegOpenKeyExW` · `RegGetValueW` 의 `Nt*Key*`, `WinSqmIsOptedIn`, `EtwEventRegister` 쪽 (`PerformRpcInitialization +0x2cf ~ +0x412`).
- ③ `RpcpNormalizeSecurityDescriptor` 다른 caller 셋 (`SetAuthInformation +0x4f6` · `UpdateRpcInterfaceInformation +0x59` · `LRPC_FAST_BINDING_HANDLE +0x38a`) 이 어느 export 에서 닿는지 (지도, depth 0).
- ④ advapi32 `LookupPrivilegeValueW/A` · winhttp `WinHttpOpen` · ws2_32 `gethostbyname` · shell32 `SHGetKnownFolderPath` · combase `CoInitializeSecurity` 가 rpcrt4 의 어느 export 를 부르는지 (요청서가 범위 밖으로 둔 것).
- ⑤ (= **Q180d**, sast/96 → run100 몫) 커널이 smss 의 Liberal SD 를 객체에 붙이는 꼴 — 위 동적 ② 의 기대값이 여기서 정해진다.

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 안 고침 — test.exe 는 rpcrt4 를 안 올림 `[관찰]` run44, 이번 사슬은 test.exe 초기화 지도에 없음):
  - 기준: rpcrt4.pdb 줄 새로, ntdll.pdb · kernelbase.pdb 줄에 run99 몫 (읽기 전용 export), 근거 로그 run99 줄 (사진 대조 없음 — 모듈 목록만 다시 확인).
  - 흐름도 앞 글: run99 문단 하나.
  - 2.4 run65 절 (run98) 줄: 링크 건너뛰기 근거 `.c` → `.asm` `+0x19d` · `+0x1a2` (run99 때 고침).
  - 3장 run97 절: (run99) 한 줄 — 이 묻기를 부르는 쪽과 첫 대답 규칙.
  - 3장 run98 절: 비유에 `[추측]`, (run99) 한 줄 — rpcrt4 는 이 SD 를 부모로만 쓴다.
  - 3장 새 절 **"RPC 런타임 초기화가 `\RPC Control` SD 를 빌려 기본 SD 를 만든다 — `rpcrt4!UuidToStringA` → … → `RpcpNormalizeSecurityDescriptor` (run99)"**.
  - 4장: run97 PE_emulator 줄 (run99 보탬 — `[추측]` → `[정적]`), run97 동적 줄 (run99 보탬 — (나) 를 유저 쪽 `+0x15f` · `+0x1a3` 로), run98 정적 줄 ⑤ (해결됨 → run99), run99 새 줄 셋 (동적 · 정적 · PE_emulator 와 다르게 적은 곳).
  - 5장: run 표 · 03_sprint 요청서 번호 run99 줄.
- 짐 지도: **고치지 않음** — test.exe 는 rpcrt4 를 안 올려서 이번 사슬이 놓는 것 (rpcrt4 전역 `gDefaultSecurityDescriptor` · `RpcHasBeenInitialized`, 힙 SD 사본) 은 test.exe 초기화 짐이 아니다.
- 이전 run 문서: run98 에 보탬 한 줄 (위 "run98 에 보탠 것" ①). 정정 없음.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점** `[추측]`: `PerformRpcInitialization +0x2ae InitializeRpcVerifier` 는 PEB `+0xbc` 의 검증기 · 페이지힙 비트가 켜졌을 때만 탄다 (결과서). 디버거 아래에서 이 비트 (`NtGlobalFlag`) 가 달라지면 갈래가 바뀔 수 있다 — 동적 때 `dt nt!_PEB @$peb NtGlobalFlag` 를 같이 찍자.
- 도구: 결과서 "스크립트 문제" 없음. `_calls.tsv` 의 `alias:…+1 ; ALIASES(1)` 이 C++ 꾸밈 이름 하나일 때도 `alias:` 로 찍혀 별칭 무리처럼 보인다 (위 보탬 ⑦) — 헷갈리면 `pdb_aliases.py --lookup` 으로 확인.
- **함정 후보** (→ windbg-kd-runbook pitfalls 에 추가 제안): 결과서의 "직전" 멈춤점 목록에 ret 자리 (`+0x461` 등) 가 섞여 있었다 — `.asm` 의 `CALL … ret=` 줄에서 **call 자리** 를 골라 쓰고, 같은 callee 를 부르는 자리가 여럿이면 모두 걸어야 "안 걸림 = 안 탐" 이 성립한다.

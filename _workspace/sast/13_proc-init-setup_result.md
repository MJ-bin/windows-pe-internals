# 13 proc-init-setup — 정적 분석 결과

- 요청서: `_workspace/sast/13_proc-init-setup.md` (Q9a, 02_sprint, 2026-10-22). 범위는 `ntdll!LdrpInitializeProcess +0x0 ~ +0x12dd` 만. 뒤쪽(`+0x12de ~ 끝`)은 안 읽음 (Q9b).
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), PDB guid_age = `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`).
- analyze 안 함 (이미 분석됨). 새로 한 일 (전부 몇 초):
  - `LdrpInitializeProcess` 는 다시 안 뽑음. 하네스 O1a·O1b 뒤에 뽑은 파일이다 (`.asm` 22:01:50 가 `ExportFunctions.java` 수정 22:01:22 보다 뒤, 전역 변수 주석·`seh:` 줄·읽지 않은 틈 목록 있음).
  - `refs`: `LdrpIsHotPatchingEnabled`, `UseWOW64`, `LdrpNtdllHotPatchContext`, `PebLdr`(0x58바이트), `0x1d2978`(`LdrpImageExpansionMitigation`) → `ms/disassemble/ntdll/_refs/`.
  - 범위를 **한 함수만** 넓힘: `LdrpInitializeExecutionOptions` depth 0 (질문 6 "`+0x587` 조건의 값이 어디서 오나" 때문. 그 함수 본문은 이 한 곳만 읽음).
- 이름 확인: `pdb_aliases.py --where ntdll.dll LdrpInitializeProcess` → rva 0x8c404, 이름 하나뿐 (겹침 없음). `_index.tsv` 에도 한 줄.
- 버전 확인: **일치**. `_calls.tsv` 의 `LdrpInitializeProcess +0x1bb6 → +0x1bbb LdrpInitializeGraphRecurse`, `_LdrpInitialize +0x167 → +0x16c LdrpInitializeProcess`.
- 호출 개수 주의: 요청서는 "앞쪽 95줄" 인데, `+0x12dd` 까지 세면 **94개** (`_calls.tsv`, 지도 `_edges.tsv`, `.asm` 의 CALL 셋 다 94). 95번째는 경계 `+0x12de` (`LdrpInitParallelLoadingSupport`) 로 보인다. 아래 표는 94개를 다 나눈 것.
- 판정: **분석성공**.

## 스크립트 문제

없음. 참고 하나 (고칠 필요 없음): `+0x7c4` 의 대상은 Ghidra 가 `RtlInterlockedPushListSList` (+별칭 1) 로 찍었는데, PDB 이름은 `InterlockedPushListSList` (0x164bd0). 별칭 규칙(내보내기 이름을 Ghidra 가 센다)대로다. 문서엔 "PDB `InterlockedPushListSList`" 로 쓴다.

## 질문별 답 [정적]

### 1. 입구 인자 RCX·RDX

- **RDX** (커널이 넘긴 값):
  - `+0x2d MOV RSI,RDX` (RSI 에 쥠) 와 `+0x30 MOV [RSP+0xd8],RDX` (스택 칸에 저장). 앞쪽에서 RSI 는 `+0x5bb` 에서 덮어쓰이기 전까지 쓰이고, `[RSP+0xd8]` 는 `+0x684`, `+0x1195` 에서 읽힌다 (뒤쪽 `+0x16fb` 에서도).
  - **앞쪽에서 처음 쓰는 곳 = `+0x3f9`** (`MOV RDX,RSI`) → `+0x400 RtlImageNtHeaderEx(3, RSI, 0, &로컬)` 로 이미지 기준 주소를 넘겨 헤더를 찾는다. 곧바로 `+0x415 RtlInsertInvertedFunctionTable(RSI, SizeOfImage)`, `+0x470 RtlRemoveInvertedFunctionTable(RSI)`.
  - `+0x68c` 에서 `[RSP+0xd8]` → 전역 `LdrpSystemDllBase`. `+0x1195` 에서 `[RSP+0xd8]` 가 `LdrpInitializeNtdllDataTableEntry` 의 첫째 인자(RCX).
  - 뜻: **RDX = ntdll 이미지 기준 주소** `[정적]` (`LdrpSystemDllBase` 에 저장되고 ntdll 모듈 항목을 만들 때 첫째 인자로 쓰이니까). 실제 값은 동적.
- **RCX** (로더 문맥): `+0x38 MOV [RSP+0xe0],RCX` 로 저장만 한다. **앞쪽 구간에서는 안 쓴다.** 처음 읽는 곳은 뒤쪽 `+0x17f4` (그다음 `+0x18ac`, `+0x18c3`) — Q9b 몫.
- 입구에서 같이 하는 일: 저장 레지스터 5개 push, 지역 변수 `0x410` 바이트, 보안 쿠키 넣기(`+0x1b~+0x25`), 지역 구조체 0 채우기, `memset` 2번 (`+0x105`, `+0x120`). `+0x13b R13 = GS:[0x30]` (TEB), `+0x14c RDI = [TEB+0x60]` (PEB) — 이후 RDI 가 PEB, R13 이 TEB 로 쓰인다 (R13 은 `+0x6c5` 에서 프로세스 매개변수로 바뀜).
- 근거: `ms/disassemble/ntdll/LdrpInitializeProcess.asm` 해당 `+0x..` 줄.

### 2. 앞쪽 구간의 큰 순서 (표)

호출 94개를 10묶음으로 나눔. "잡일" = `memset`, `RtlAppendUnicodeStringToString`, `RtlAppendUnicodeToString`, `LdrpLogInternal`, `RtlAllocateHeap`, `RtlFreeHeap`, `__security_check_cookie`. 호출 오프셋은 `+call / ret`. 묶음 경계는 호출 사이에서 자름.

| # | 범위 | 요약 | 대표 호출 (call / ret) | 호출 수 (잡일) |
|---|---|---|---|---|
| 1 | `+0x0 ~ +0x157` | 입구: 지역 변수 0 채우기, 안 쓰는 예외 필터 자리에 "빈 값" 암호화 저장 (`+0x12f RtlEncodePointer(0)` → `RtlpUnhandledExceptionFilter`), TEB·PEB 꺼내기, 첫 스레드 난수 준비 | `LdrInitializePerThreadRng` `+0x153 / +0x158` (인자 RCX = TEB) | 4 (2) |
| 2 | `+0x158 ~ +0x28d` | **PEB.Ldr 만들기**: ntdll 전역 `PebLdr` 를 채우고(Length 0x58, Initialized=1, 목록 머리 3개 자기 연결) PEB+0x18 에 연결(`+0x1c1`). 그다음 NLS 준비, CPU 가 2개 이상이면 스핀 횟수 3곳을 0x2800 으로, `RtlpFcProcessManager` 0 채우기, 비휘발 플러시 준비 | `LdrpInitializeNlsInfo` `+0x1c8 / +0x1cd` (RCX = PEB) | 6 (4) |
| 3 | `+0x292 ~ +0x4a1` | exe 경로 앞의 `\??\` 떼기(`+0x2c9~+0x32c`, 호출 없음), exe NT 헤더 찾기(`+0x35e` → 전역 `LdrpAppHeaders`), 보안 프로세스 검사(`+0x37f`), `LdrpHashTable` 32칸 비우기(`+0x3b8~+0x3d3`), ntdll 을 "역함수 표"에 등록, `.mrdata` 쓰기 허용 열고 닫기, 중요 thunk 잡기, CFG 확인 | `RtlInsertInvertedFunctionTable` `+0x415 / +0x41a` | 12 (1) |
| 4 | `+0x4a8 ~ +0x579` | 무결성 설정 비트 두 개 읽기(`LdrSystemDllInitBlock+0xa0`) 후 **실행 옵션(IFEO) 읽기** | `LdrpInitializeExecutionOptions` `+0x531 / +0x536` | 2 (1) |
| 5 | `+0x57a ~ +0x773` | 전역 켜고 끄기: 핫패치 끄기(`+0x587`), `UseWOW64` 정하기(`+0x5f2`), .NET(COR) 이미지 검사, 프로세스 매개변수 정규화, 로드 구성(LoadConfig)에서 `NtGlobalFlag`·타임아웃 반영, `LdrpDebugFlags` 정하기 | `RtlNormalizeProcessParams` `+0x6c0 / +0x6c5` | 5 (1) |
| 6 | `+0x774 ~ +0x9e4` | 임계 구역 디버그 정보 목록, **PEB 의 TLS 비트맵**(PEB+0x78/+0x238)과 FLS 자리 만들기, 스택 추적 DB 옵션(IFEO `StackTraceDatabaseSizeInMb`), **`FastPebLock` 임계 구역 만들기**(`+0x9cd`) 후 PEB+0x38 에 연결 | `RtlInitializeCriticalSectionEx` `+0x9cd / +0x9d2` | 8 (2) |
| 7 | `+0x9e5 ~ +0xb7d` | **프로세스 힙 만들기**: 힙 관리자, 힙 정보 설정, `LdrpInitializeProcessHeap`(결과가 PEB+0x30 `ProcessHeap`). 이어 서비스 기능 스위치 2개, 로더 스냅 버퍼, enclave 목록 락, 활성화 컨텍스트 스택, `LdrpHeap` = PEB 힙, 하이퍼바이저 공유 페이지 값(`NtQuerySystemInformation` 클래스 0xc5), 태그 힙 | `LdrpInitializeProcessHeap` `+0xa4b / +0xa50` | 13 (2) |
| 8 | `+0xb83 ~ +0xc41` | 스레드 풀 패키지 초기화(+ 스레드 풀 옵션 반영), ETW 공급자 등록 3곳, 힙 로깅, **환경 블록·매개변수 블록 준비** | `TpInitializePackage` `+0xb83 / +0xb88` (같은 묶음: `RtlpInitEnvironmentBlock` `+0xc2d / +0xc32`, `RtlpInitParameterBlock` `+0xc3a / +0xc3f`) | 11 (0) |
| 9 | `+0xc47 ~ +0x1168` | 디버그 페이지 힙 옵션(IFEO `DebugProcessHeapOnly`), **시스템 경로 문자열** 만들기(`<NtSystemRoot>\System32` → `LdrpSystem32`, `RtlpSystem32Dirs`), **`\KnownDlls` 디렉터리 열기 → `KnownDllPath` 링크 열기·읽기**(보호 프로세스 아니면, `+0xd57`), DLL 정책·대체 forwarder 경로 | `NtOpenDirectoryObject` `+0xdb8 / +0xdbd` (뒤이어 `NtOpenSymbolicLinkObject` `+0xe47`, `NtQuerySymbolicLinkObject` `+0xe90`, `LdrpInitializePolicy` `+0xfc5`) | 29 (17) |
| 10 | `+0x1169 ~ +0x12dd` | exe 폴더 종류 검사, exe 폴더(마지막 `\` 앞) 잘라 두기, **ntdll 모듈 항목 만들기**, (있으면) 핫패치 ntdll 항목, 히스토리 표, ntdll 항목을 `PebLdr` 초기화 순서 목록에 연결 | `LdrpInitializeNtdllDataTableEntry` `+0x119d / +0x11a2` | 4 (0) |
| 합 | | | | 94 (30) |

- 잡일 합 30 = `memset` 7, `RtlAppendUnicodeStringToString` 7, `LdrpLogInternal` 6 (전부 실패 때 찍는 로그 + 정상 1~2곳), `RtlAllocateHeap` 4, `RtlAppendUnicodeToString` 4, `RtlFreeHeap` 1, `__security_check_cookie` 1 (에필로그, 묶음 7 에 셈).
- 묶음 3 의 `LdrProtectMrdata` 3번 + 묶음 9 의 2번은 `.mrdata` 쓰기 보호 껐다 켜기 쌍이라 잡일로 안 셌다 (요청서 잡일 목록에 없음).
- 근거: `LdrpInitializeProcess.asm` 각 대표 호출 줄 (위 오프셋), `_calls.tsv` 의 `LdrpInitializeProcess` 줄 94개.
- 문자열로 확인한 것 (원본 바이트를 직접 읽음): `+0xdb8` 에 넘기는 이름은 `\KnownDlls` (`0x180180450`, 길이 0x14), `+0xe47` 의 링크 이름은 `KnownDllPath`, IFEO 값 이름은 `StackTraceDatabaseSizeInMb`(`+0x8f2`), `DebugProcessHeapOnly`(`+0xcae`), 보안 프로세스 로그 문구 `Secure processes must be large address aware`(`+0x3a9`).

### 3. 앞쪽 구간에서 DLL·이미지를 메모리에 올리나? → **아니오**

- `+0x0 ~ +0x12dd` 직접 호출 94개 중 `NtMapViewOfSection`, `NtCreateSection`, `LdrpMapDll*`, `LdrpMinimalMapModule`, `NtOpenSection` 은 **없다** (`_calls.tsv`).
- 비슷해 보이는 것들 (모두 올리기가 아님):
  - `+0xdb8 NtOpenDirectoryObject`, `+0xe47 NtOpenSymbolicLinkObject`, `+0xe90 NtQuerySymbolicLinkObject`, `+0xf04 NtClose` = KnownDlls 디렉터리·링크를 **열고 경로 문자열을 읽기만** 한다. KnownDlls 에서 섹션을 여는 호출은 없다.
  - `+0xb4d NtQuerySystemInformation(0xc5, …, 8)` 는 값 조회.
  - `+0x627 LdrpCorFixupImage` (.NET 보정, 안에 `NtProtectVirtualMemory`) 는 `PE32(0x10b)` 헤더 + `TEB.WowTebOffset <= 0` 일 때만 부른다 (`+0x602~+0x611`). x64 exe(`PE32+`, 0x20b)면 건너뜀.
- 지도(`_map/`) 한 단계 아래(깊이 6) 도 봤다 (앞쪽 callee 61개의 caller 줄): 섹션 만들기·올리기 호출 없음. `NtQueryVirtualMemory`(`LdrpInitializeNtdllDataTableEntry +0x1df`), `LdrpProcessMappedModule`(`+0x11f`) 이 있으나 "이미 올라온 ntdll 을 설명하는 항목" 쪽으로 보임 `[추측]`. 한계: 지도는 공용 도구를 안 펼쳐서 거기 안은 못 봄.
- 핫패치 ntdll 이 켜진 프로세스에선 **이 함수 앞**(`LdrpInitializeInternal +0xbd LdrpLoadPatchedNtdll`, sast/12)에서 이미 다른 이미지를 올렸을 수 있다. 그 결과가 아래 질문 4·6 의 `LdrpNtdllHotPatchContext`.
- 그래서 flow.md 2.5 `[추측]` 은 **앞쪽에서는 안 올린다** 까지 `[정적]` 으로 뒷받침. 올리는 곳은 뒤쪽(`+0x1aa4 LdrpMapAndSnapDependency` 등, Q9b·Q10).

### 4. 프로세스 모듈 목록 (PEB.Ldr) 과 `LdrpInitializeNtdllDataTableEntry` 두 번

- **PEB.Ldr 를 쓰는 줄 (앞쪽)**:
  - `+0x158 ~ +0x1b7`: 전역 `PebLdr`(rva 0x1d2880) 채우기. `Length=0x58`, `Initialized(+4)=1`, 세 목록 머리(`+0x10` 적재 순서, `+0x20` 메모리 순서, `+0x30` 초기화 순서) 모두 "자기를 가리키는 빈 목록", `+0x48` 종료 중=0, `+0x50`=0. 칸 이름은 PDB `_PEB_LDR_DATA` 로 맞춤 (`refs PebLdr`).
  - **`+0x1c1 MOV [RDI+0x18],&PebLdr`** : `PEB.Ldr`(PEB+0x18)에 연결. PEB 는 ntdll 안 전역을 가리키게 된다.
  - 이 시점엔 목록이 전부 비어 있다 (`+0x1c1` 시점).
- **초기화 순서 목록(`PebLdr+0x30`)에 ntdll 넣기 `+0x1283 ~ +0x12d7`**: ntdll 항목(`LdrpNtDllDataTableEntry`) 의 `+0x20` 을 목록 머리 바로 뒤에 끼운다 (끼우기 전에 `머리.Flink.Blink == 머리` 확인, 아니면 `+0x1e34 __fastfail(3)`). 두 번째 항목(`[RSP+0x160]`)이 있으면 그것도 끼움 (`+0x12b9~+0x12d7`). 적재 순서·메모리 순서 목록(`+0x10`, `+0x20`)에 넣는 줄은 이 구간에 **없다**.
  - 그 둘은 `LdrpInitializeNtdllDataTableEntry` 안에서 하는 것으로 보인다 (`refs PebLdr`: `+0x10`/`+0x20` 쓰기는 `LdrpInsertDataTableEntry` 뿐, 지도 `_edges.tsv`: `LdrpInitializeNtdllDataTableEntry +0xf2 → LdrpInsertDataTableEntry`) `[추측]` — 이 callee 는 본문을 안 읽음.
  - 지도로 본 `LdrpInitializeNtdllDataTableEntry` (633바이트) 의 callee: `memset`, `RtlImageNtHeaderEx`, **`LdrpAllocateModuleEntry`**(`+0x67`), `RtlAppendUnicodeStringToString`, `LdrpInsertDataTableEntry`, `LdrpLogDllState`, `LdrpInsertModuleToIndex`, `LdrpProcessMappedModule`, `NtQueryVirtualMemory`, `LdrpResolvePatchDllName`(`+0x1fe`), `LdrpRecordModuleDependency`. 이름으로 보면 ntdll 항목을 만들고 색인에 넣는다 `[추측]`.
- **두 번 부르는 이유** (인자가 다르다):
  - 첫째 `+0x119d`: **조건 없이** 부른다. RCX=ntdll 기준 주소(`[RSP+0xd8]`), RDX=`&LdrpNtDllDataTableEntry`(만든 항목을 돌려받는 전역), R8=0, R9=`&[RSP+0xc0]` (묶음 9 에서 만든 `<NtSystemRoot>\System32\` 문자열). 실패(음수)면 `+0x11a7 JS` 로 함수 종료.
  - 둘째 `+0x126a`: `+0x1250 RCX = [[LdrpNtdllHotPatchContext]]` 가 **0 이 아닐 때만** (`+0x1256 JZ` 로 건너뜀). RCX=그 값, RDX=`&[RSP+0x160]`(돌려받는 칸), R8=첫째로 만든 ntdll 항목, R9=0. 실패면 `+0x1271 JS` 로 함수 종료.
  - `LdrpNtdllHotPatchContext` 는 `LdrpInitializeInternal` 의 지역 칸을 가리키는 포인터 (sast/12). 첫 값은 `LdrpLoadPatchedNtdll` 가 채우는 것 `[추측]` (핫패치 켜졌을 때만). 그래서 **핫패치가 꺼진 평범한 프로세스는 한 번만 부르고, 켜진 프로세스는 "패치된 ntdll" 항목을 하나 더 만든다** `[추측]`. 어느 쪽인지는 값이 동적.
- **exe 의 모듈 항목**: 이 구간에서는 **안 만든다.** 뒤쪽 `+0x1359 LdrpAllocateModuleEntry` (RCX=`&[RSP+0x2b0]` = 방금 구성한 exe 경로 구조체) → `+0x1374 LdrpImageEntry` 에 저장 (`refs` 상 `LdrpImageEntry` 첫 쓰기). 앞쪽에서 exe 쪽은 PEB 의 `ImageBaseAddress`(`[RDI+0x10]`)로 헤더만 읽는다 (`+0x35e`, `+0x627`, `+0x662`, `+0x6ef`).
- 이 구간에서 exe 폴더를 `[RSP+0x130]` 에 따로 잘라 둔다 (`+0x11b0 ~ +0x1224`, 호출 없음). 뒤쪽 DLL 경로 만들 때 쓰일 것 `[추측]`.

### 5. 일찍 끝나는 길 (앞쪽 구간)

- **상태값을 돌려주고 끝나는 갈림길 19개** (전부 같은 에필로그 `+0xa7b ~ +0xaab` 로 감. 반환값은 대개 호출한 함수가 준 음수 상태, 일부는 고정값):
  1. `+0x37f~+0x3b3`: 보안 프로세스(`LdrpIsSecureProcess != 0`)인데 exe 헤더 `Characteristics`(`+0x16`) 의 `0x20`(큰 주소 인식) 이 없음 → 로그 후 `0xc00000bb`.
  2. `+0x3e0`: `LdrpHandleUserCopyAttributeList` 음수.
  3. `+0x53c`: `LdrpInitializeExecutionOptions` 음수 → 로그(`0x16f0`) 후 그 상태 반환.
  4. `+0x62e`: `LdrpCorFixupImage` 음수.
  5~6. `+0x9d4`: `FastPebLock` 만들기 실패, `+0x9f4`: `RtlInitializeHeapManager` 실패.
  7. `+0xa55`: `LdrpInitializeProcessHeap` 음수 → 로그(`0x186c`) 후 **`0xc0000017`**.
  8~10. `+0xb8a` `TpInitializePackage`, `+0xc34` `RtlpInitEnvironmentBlock`, `+0xc41` `RtlpInitParameterBlock` 음수.
  11. `+0xd10`: 힙 할당 실패(시스템 경로 버퍼) → `0xc0000017`.
  12~14. `+0xdcf` `NtOpenDirectoryObject`, `+0xe52` `NtOpenSymbolicLinkObject`, `+0xe9f` `NtQuerySymbolicLinkObject`(버퍼 부족 `0xc0000023` 은 재시도하고 다른 실패만) 음수 → 각각 로그(`0x1993`, `0x19aa`, `0x19cd`) 후 그 상태 반환.
  15~17. `+0xecd`, `+0xf71`, `+0x1052`: 힙 할당 실패 → `0xc0000017` (두 곳은 로그 `0x1a01`, `0x1a5e`).
  18~19. `+0x11a7`, `+0x1271`: `LdrpInitializeNtdllDataTableEntry` 음수.
- **강제 종료로 가는 갈림길 5개** (함수 안 끝 `+0x1e34`, `+0x1e3b`):
  - `+0x784`, `+0xb22` → `RtlRaiseStatus(0x80000002)`: 상수 주소(`RtlCriticalSectionDebugSList`, `EtwpFreeRegistrationList`)가 16바이트 정렬 맞는지 확인하는 코드라 평소엔 안 탈 것 `[추측]`.
  - `+0x1299`, `+0x12c6` → `__fastfail(3)`: `PebLdr+0x30` 목록 끼우기 전 연결 검사 실패.
  - `+0x1249 ~ +0x124e` → `__fastfail(0x45)`: `[LdrpNtdllHotPatchContext+8] < 0` 이고 `LdrpIsHotPatchingEnabled != 0` (질문 6).
- 뒤쪽으로 바로 뛰는 갈림길: `+0x12bc JZ → +0x12de` (둘째 항목 없으면 경계로 감) 하나뿐. 실패 길이 뒤쪽 본체를 거치는 것은 없다 (끝 조각 `+0x1e34~+0x1e46` 의 강제 종료 코드로 가는 점프만 있다).
- 호출한 쪽에서는 `_LdrpInitialize +0x172 JS` → `+0x1a7` 로그(`0xa78`) → 이후 `LdrpInitializationFailure` 쪽 (sast/12 몫).
- 대표 조건 2~3개: 1번(보안 프로세스 + 큰 주소 미지원), 3번(IFEO 읽기 실패), 7번(프로세스 힙 실패). test.exe 는 정상 흐름이라 이 길은 안 탈 것이다 `[추측]`.

### 6. sast/12 연결점 (전역 변수)

- **`+0x587` `LdrpIsHotPatchingEnabled` 쓰기**: `+0x57f CMP [LdrpImageExpansionMitigation],2` 가 같을 때만 `+0x587` 에서 0 을 쓴다 (R14B 가 0). 즉 **"ImageExpansionMitigation 값이 2 이면 핫패치를 끈다"**. 아니면 그대로 둔다.
  - `LdrpImageExpansionMitigation` (rva 0x1d2978) 은 `LdrpInitializeExecutionOptions +0x4d0` 에서 쓴다 (`refs 0x1d2978`: 쓰기 이 한 곳 + 읽기 `+0x57f` 한 곳). 값은 IFEO 값 `ImageExpansionMitigation`(DWORD) 을 `RtlQueryImageFileKeyOption` 으로 읽은 것 (`+0x4b4~+0x4d0`; 읽기 실패면 안 씀 = 기본 0).
  - 평소 값(`LdrpIsHotPatchingEnabled`)은 `LdrpInitializeHotPatching +0x55` 에서 `NtManageHotPatch` 결과로 정해진다 (이 함수 `+0x587` 이전, sast/12). `refs`: 읽기 `LdrpInitializeProcess +0x1240`, `LdrpInitializeInternal +0x98`, `LdrpMapAndSnapDependency +0xaa`, `LdrHotPatchNotify +0x14`.
  - IFEO 에 값이 없으면(기본) 이 줄은 안 탄다 `[추측]`.
- **`+0x5f2` `UseWOW64` 쓰기**: `+0x5e3 ECX = [TEB+0x180c]` (PDB `_TEB.WowTebOffset`, 오프셋 6156 = 0x180c 로 확인) → `+0x5ef TEST ECX,ECX; SETG AL` → **`UseWOW64 = (WowTebOffset > 0)`**. x64 exe 의 평범한 스레드면 WOW64 TEB 오프셋이 없으니 0 일 것 `[추측]` (값은 동적). 쓰는 곳은 이 함수 한 곳(`refs UseWOW64`), 읽는 곳은 앞쪽 6곳 (`+0x63b`, `+0x696`, `+0x6d5`, `+0x875`, `+0xbd0`, `+0xf0c`) + 뒤쪽 이 함수 3곳 (`+0x1424`, `+0x14d5`, `+0x1766`) + 다른 함수 (`_LdrpInitialize +0x88`, `+0x22d`, `LdrpInitializeProcessHeap +0x161` 등).
  - 0 이 아니면 달라지는 곳 (앞쪽): `+0x696` `pShimData` 안 읽음, `+0x6d5` LoadConfig 안 읽음, `+0x875` TLS 비트 19개 세움(`RtlSetBits`), `+0xbd0` `LdrpEnableUMGLTracingStateSync` 안 부름, `+0xf0c` 현재 폴더 문자열 처리를 건너뛰고 시스템 경로 문자열(`[RSP+0x120]`)을 그대로 복사.
- **`+0x1233` `LdrpNtdllHotPatchContext` 읽기**: 포인터를 읽어서 (a) `+0x123a [ctx+8] < 0` 이고 핫패치가 켜져 있으면 `__fastfail(0x45)`, (b) `+0x1250 [ctx+0]` 가 0 이 아니면 둘째 `LdrpInitializeNtdllDataTableEntry`(질문 4) 에 첫째 인자로 넘김.
  - `ctx` 는 `LdrpInitializeInternal +0xa7` 에서 자기 지역 칸 `[RSP+0x30]` (16바이트, 0 으로 비움)을 가리키게 해 둔다. `[ctx+8]` 에 들어가는 `[RSP+0x38]` 는 `LdrpLoadPatchedNtdll` 의 반환값 (`LdrpInitializeInternal +0xbd/+0xc2`) → `[ctx+8]` 는 "패치 ntdll 올리기 결과 상태" 로 보인다 `[추측]`.
  - 핫패치가 꺼졌으면 `LdrpLoadPatchedNtdll` 를 안 부르니 둘 다 0 → (a)(b) 모두 건너뜀.
  - 뒤쪽 `+0x18ef` 에서 한 번 더 읽음 (Q9b).
- **`+0x153` `LdrInitializePerThreadRng`**: 맞다 `[정적]`. `+0x153 CALL (RCX=R13=TEB)` ← 첫 스레드(프로세스 초기화 길). `_calls.tsv` 에 호출 지점이 두 곳뿐: `LdrpInitializeProcess +0x153` 과 `_LdrpInitialize +0x83` (그 앞 `+0x74 LdrpForkInProgress`, `+0x6a [R14+0x17ee] & 0x40` 검사가 있는 다른 갈래, sast/12). 이 함수 안에서는 **조건 없이** 부른다.

### 7. 디버거 때문에 달라지는 길 (앞쪽 구간)

- `PEB.BeingDebugged`(PEB+0x2): **앞쪽에서 안 읽는다.** 읽는 곳은 뒤쪽 `+0x17d2`, `+0x1b35` (Q9b).
- `KUSER_SHARED_DATA`(`0x7ffe....`): **앞쪽에서 안 읽는다.** 뒤쪽 `+0x188c`, `+0x1cb0` 에서 `0x7ffe0330` 읽음 (Q9b). 따라서 `KdDebuggerEnabled`(0x2d4) 를 앞쪽에서 읽는 줄은 없다.
- `NtGlobalFlag`(PEB+0xbc) 는 읽는다 (값은 PEB+0xbc, 비트 이름은 공개 문서 기준이라 `[추측]`):
  - `+0x58e` `& 0x2` (로더 스냅 보이기 `FLG_SHOW_LDR_SNAPS`) → `LdrpDebugFlags |= 1` (`+0x59c`). `+0x74b` 에서 LoadConfig 반영 **뒤에** 한 번 더 같은 검사 (`+0x754`).
  - `+0x70d`, `+0x71d`: LoadConfig(`LdrImageDirectoryEntryToLoadConfigEx`, 있을 때만) 의 GlobalFlagsClear/Set 을 PEB+0xbc 에 반영 → 즉 `+0x58e` 는 반영 전 값, `+0x74b` 는 반영 후 값.
  - `+0x8a3`, `+0x989` `& 0x1000` (사용자 스택 추적 DB `FLG_USER_STACK_TRACE_DB`) → IFEO `StackTraceDatabaseSizeInMb` 읽고 스택 추적 DB 만들기(`+0x979 RtlControlStackTraceDataBase`), `RtlpForceCSDebugInfoCreation` 켜기.
  - `+0xaf4` `& 0x800000` (예외 로그 `FLG_ENABLE_EXCEPTION_LOGGING`) → `+0xb00 RtlInitializeExceptionLog`.
  - 뒤쪽 `+0x16b7` `& 0x100` 도 있음 (Q9b).
- PEB+0x2c8 `AppCompatFlags` 비트 22 (`+0x99f ~ +0x9bb`) → `RtlpForceCSDebugInfoCreation`, `RtlpForceCSToUseEvents` 켜기. 디버거와 무관, 앱 호환성 설정.
- 힙: `+0xcae RtlQueryApplicationKeyOption("DebugProcessHeapOnly")` 와 `+0xcb7 RtlpDebugPageHeapTable+0x8` 로 디버그 페이지 힙을 켜는 갈림길이 있다. IFEO 값에 따름 (디버거 부착을 직접 검사하지 않음).
- IFEO `BreakOnInitializeProcessFailure` 를 `LdrpInitializeExecutionOptions +0x462` 에서 읽어 `g_LdrBreakOnLdrpInitializeProcessFailure` 에 둔다 (callee 안. 본문 일부만 봄). 평소엔 0.
- 커널 디버거만 붙었을 때 켜질지: PEB+0xbc 값은 커널이 PEB 만들 때 정해 넣은 것이라 (flow.md 의 MmCreatePeb 쪽) 이 구간 코드로는 알 수 없다 → **동적**. 커널 디버거만으로는 `NtGlobalFlag` 가 안 바뀔 것이다 `[추측]`.

## 하는 일로 본 그림 2 `(정적)` 상자 후보 (3 ~ 5개)

1. **PEB.Ldr 목록 머리 만들기** (`+0x158 ~ +0x1c1`, 전역 `PebLdr` 를 PEB+0x18 에 연결). 이유: PEB.Ldr 가 이 시점에 생긴다는 걸 못박는 줄(`+0x1c1`)이 있고, 뒤의 모듈 항목들이 여기에 달린다.
2. **`LdrpInitializeExecutionOptions`** (`+0x531 / +0x536`). 이유: 이미지 실행 옵션(IFEO) 읽기 — 핫패치·스택 DB 같은 갈림길이 여기서 정해진 값을 쓴다.
3. **`LdrpInitializeProcessHeap`** (`+0xa4b / +0xa50`). 이유: PEB.ProcessHeap(PEB+0x30) 가 만들어지는 단 한 곳. 이 뒤부터 이 함수가 힙을 쓴다.
4. **`\KnownDlls` 열기** (`NtOpenDirectoryObject +0xdb8 / +0xdbd`). 이유: 시스템 DLL 공유 목록 열기. 이후 DLL 로드 길(Q10)의 전제 (`LdrpKnownDllDirectoryHandle`, `LdrpKnownDllPath`).
5. **ntdll 모듈 항목 만들기** (`LdrpInitializeNtdllDataTableEntry +0x119d / +0x11a2`, 이어 `+0x12aa` 목록 연결). 이유: "ntdll 이 모듈 목록에 이 시점에 들어간다" 를 보이는 줄. 흐름도 모듈 목록 쪽 (그림 2 → Q9b 와 이어짐).

## 앞쪽에서 Q9b·Q10~Q16 에 넘길 연결점

- **Q9b**: `+0x12de LdrpInitParallelLoadingSupport` 가 경계. 이어서 `+0x1302 LdrpDrainWorkQueue`, `+0x1313 LdrpInitializeDllPath`, `+0x1359 LdrpAllocateModuleEntry` → `LdrpImageEntry`(exe 항목). RCX(`[RSP+0xe0]`) 처음 쓰는 곳 `+0x17f4`. `BeingDebugged` `+0x17d2`/`+0x1b35`, `KUSER +0x330` `+0x188c`/`+0x1cb0`, `LdrpNtdllHotPatchContext` 읽기 `+0x18ef`, `NtGlobalFlag & 0x100` `+0x16b7`.
- **Q10 (DLL 로드)**: 앞쪽에서 `LdrpKnownDllDirectoryHandle`(핸들), `LdrpKnownDllPath`(링크 값), `LdrpSystem32`, `RtlpSystem32Dirs`, `RtlpSystemDirs`, `LdrpHashTable`(32칸 비움 `+0x3b8`), `LdrpHeap`, `LdrpSystemDllBase`, `LdrpAppHeaders` 를 준비해 둔다. DLL 로드 코드(`LdrpLoadDllInternal` 등)가 이 전역들을 쓰는지 Q10 에서 `refs` 로 확인.
- **`LdrpInitializeNtdllDataTableEntry`** (633바이트, 지도 깊이 6): 적재/메모리 순서 목록 끼우기(`LdrpInsertDataTableEntry`), 색인(`LdrpInsertModuleToIndex`), 패치 DLL 이름(`LdrpResolvePatchDllName`) 쪽 — 한 번 읽을 가치가 있다 (둘째 호출의 정체 확인). 대기열에 추가 제안.
- **Q11~Q15 (모듈 목록·TLS)**: `PebLdr` 목록의 다른 사용처는 `refs PebLdr` (`ms/disassemble/ntdll/_refs/PebLdr.tsv`): `LdrpInitializeNode +0x2d4` 가 `+0x30` 목록에 끼움 (초기화 순서), `LdrpInsertDataTableEntry` 가 `+0x10`·`+0x20`, `LdrpInitializeTls +0x36` 가 `+0x10` 읽음.
- **Q16 (TLS)**: 이 구간에는 TLS 콜백 관련 호출이 없다. 단 `+0x774~+0x8a3` 에서 PEB 의 TLS 비트맵 칸(PEB+0x78 `TlsBitmap`, +0x80 비트 칸, +0x238/+0x240 확장)을 가리키게 설정해 둔다 — `LdrpInitializeTls` 가 쓰는 비트맵의 바탕.
- 핫패치 흐름 요약 (sast/12 와 이어짐): `LdrpInitializeInternal` 에서 핫패치 켬/끔 + 컨텍스트 준비 → 이 함수 `+0x587` 에서 IFEO 값으로 끌 수 있음 → `+0x1233 ~ +0x126a` 에서 컨텍스트 읽어 둘째 모듈 항목 만들기 → 뒤쪽 `+0x18ef` 에서 한 번 더 읽음.

## 동적분석으로 확인해야 할 것

(대기열 맨 뒤에 붙일 짧은 목록. 요청서 번호 매기기는 오케스트레이터가)

- 입구 `RCX`, `RDX` 값 (RDX 가 ntdll 기준 주소와 같은가).
- `+0x5f2` 뒤 `UseWOW64`, `+0x587` 줄을 탔는지 (IFEO `ImageExpansionMitigation`), `+0x1240` 시점 `LdrpIsHotPatchingEnabled`, `[ctx]`, `[ctx+8]` — 둘째 `LdrpInitializeNtdllDataTableEntry` 를 탔는지.
- `+0x12de` 직전에 `PebLdr` 목록 상태 (3개 목록에 ntdll 만 들어 있나, exe 는 아직 없나).
- `+0x58e` 시점 `PEB+0xbc` 값, `LdrpDebugFlags`. 커널 디버거만 붙었을 때 값이 달라지나.
- `\KnownDlls` 열기 성공 (`+0xdbd` 직후 `eax`), `KnownDllPath` 문자열 값, `LdrpSystem32` 문자열.
- 실패 길(질문 5)은 test.exe 에서 안 탈 것이다 — `+0xa7b` 에 걸어 반환값(`eax`) 한 번 확인하면 끝.
- 추천 멈춤점 (`ntdll+rva` = `0x8c404 + 오프셋`):
  - `ntdll+0x8c404` (입구, `rcx`/`rdx`)
  - `ntdll+0x8c9f6` (`+0x5f2`, `UseWOW64` 쓰기 직전: `eax`)
  - `ntdll+0x8c98b` (`+0x587`, 탔는지만)
  - `ntdll+0x8ce4f` (`+0xa4b`, `LdrpInitializeProcessHeap` 입구) / ret `ntdll+0x8ce54`
  - `ntdll+0x8d1bc` (`+0xdb8`, `\KnownDlls` 열기)
  - `ntdll+0x8d5a1` (`+0x119d`, 첫 ntdll 항목 만들기) / ret `ntdll+0x8d5a6`
  - `ntdll+0x8d637` (`+0x1233`, 핫패치 컨텍스트 읽기)
  - `ntdll+0x8d6e2` (`+0x12de`, 경계. 여기서 `PebLdr`(rva 0x1d2880) 목록 확인)
  - 조건: test.exe 일 때만 (프로세스 필터). 유저 쪽 ntdll 이라 새 프로세스마다 주소가 같을 수 있지만 ASLR 때문에 `lm` 으로 기준 확인.

## 판정

**분석성공.** 요청 7개 질문 모두 `[정적]` 근거(오프셋·줄)와 함께 답했다. 값에 달린 것(`UseWOW64`, 핫패치 켜짐, 둘째 항목, 디버거 영향)은 "코드에 이렇게 쓰여 있다"까지만이고 동적으로 확인할 것으로 넘겼다. 호출 수는 요청서 95 → 실제 94 (경계 `+0x12de` 포함 여부 차이). 결과물: `ms/disassemble/ntdll/LdrpInitializeProcess.asm`, `ms/disassemble/ntdll/_calls.tsv`, `ms/disassemble/ntdll/_refs/{LdrpIsHotPatchingEnabled,UseWOW64,LdrpNtdllHotPatchContext,PebLdr,0x1d2978}.tsv`, `ms/disassemble/ntdll/LdrpInitializeExecutionOptions.asm`.

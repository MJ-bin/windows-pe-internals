# 33 map-module — 정적 분석 결과

정정 (2026-10-24): 점프 목적지를 주소 끝자리 → 함수 오프셋으로 고침 (예: `+0x774` → `+0x1b4`). 동적 절 헤더 `ImageBase` 식의 `e_lfanew`(4바이트) 읽기도 `poi` → `dwo` 로 고침.

- 요청서: `_workspace/sast/33_map-module.md` (Q52, 02_sprint, 예정 run38)
- 판정: **분석성공** (질문 1~4 전부 답함. 몇 군데는 호출된 함수 안쪽이거나 실행 값이라 `[추측]`/동적으로 넘김)
- 날짜: 2026-10-24
- 모듈: ntdll 26100.9457. 이미 분석된 모듈이라 analyze 는 안 했다 (`.gpr`, `_index.tsv`, `SetPdb:` 줄 있음). 지도 `_map/` 와 `_nodes.tsv` 에서 위치·caller 수만 확인.
- 한 일:
  - `run_ghidra.sh export ntdll.dll '^(LdrpRelocateImage|LdrpProcessMachineMismatch)$' 0` (새로 2개).
  - **요청서 밖으로 하나 더**: `'^LdrpProtectAndRelocateImage$' 0` (1개). 이유: 요청서 2b 가 묻는 "보호 바꾸기·`LdrRelocateImageWithBias`" 는 `LdrpRelocateImage` 한 단계 아래(`LdrpProtectAndRelocateImage`)에 있다. 지도엔 깊이 5 로 있지만 `LdrpRelocateImage` 가 깊이 10 이라 안 펼쳐진 것. 이름·부르는 곳만 봤다.
  - `LdrpMinimalMapModule`·`LdrpCompleteMapModule` 은 `.asm`/`.c` 가 이미 있어서 다시 안 뽑았다. depth 1 callee 본문은 요청서대로 뽑지도 읽지도 않았다.
  - `run_ghidra.sh refs ntdll.dll` 4번: `LdrpLargePageDllKeyHandle`, `LdrpMapAndSnapWork`, `LdrpProtectAndRelocateImage`, `LdrpRelocateImage`, `LdrRelocateImageWithBias` (누가 쓰고 부르나).
- 스크립트 문제: 없음.
- 버전 대조: `_calls.tsv` 의 `LdrpLoadDll +0xfb / +0x100 → LdrpLoadDllInternal`, `LdrpInitializeProcess +0x1bb6 / +0x1bbb → LdrpInitializeGraphRecurse` (SKILL 알려진 사실과 **일치**). 새로 뽑은 4함수의 `.asm` `CALL ... ret=` 줄 수와 `_calls.tsv` 줄 수가 같음 (13 / 13 / 8 / 2). 요청서가 적은 오프셋(`+0x174`, `+0x36c`, `+0x31b` …)은 전부 **call 자리**와 일치.
- 이름 확인: 네 함수 모두 `_index.tsv` 에 PDB 이름 그대로 (rva `0xa220`/`0xa5c0`/`0xe3984`/`0x10af3c`, 크기 915/940/359/185). **별칭 하나**: `+0x257` callee 는 PDB 이름이 `RtlQueryImageFileKeyOption` 이고 Ghidra 가 `LdrQueryImageFileKeyOption`(내보내기 이름)으로 잡았다 (`pdb_aliases.py --where` 확인). 문서엔 PDB 이름을 쓴다.

## 파일 (레포 밖, 커밋 안 함)

- `ms/disassemble/ntdll/LdrpMinimalMapModule.asm`, `LdrpCompleteMapModule.asm` (이미 있던 것), 새로: `LdrpRelocateImage.asm`, `LdrpProcessMachineMismatch.asm`, `LdrpProtectAndRelocateImage.asm`
- `ms/decomp/ntdll/` 같은 이름 `.c` (`.asm` 으로 흐름을 확인했고 `.c` 는 보조)
- `ms/disassemble/ntdll/_calls.tsv` (새 3함수 줄이 덧붙음), `_refs/` 5개
- 칸 이름은 PDB 타입 (`llvm-pdbutil-14 dump -types ms/sym/ntdll.dll.pdb`, 스크래치 파일). **`_LDRP_LOAD_CONTEXT` 는 칸 정보 없음** — 오프셋 + 이번에 읽은 역할만. `_MEM_EXTENDED_PARAMETER`·`_MEM_ADDRESS_REQUIREMENTS`·`_IMAGE_COR20_HEADER` 도 PDB 에 없음 (모양이 맞아서 SDK 이름으로 적되 `[추측: 모양]`).

## 먼저 알아 둘 칸

PDB 로 확인한 `[정적]`:

- `_LDR_DATA_TABLE_ENTRY` (`E`): `+0x30 DllBase`, `+0x40 SizeOfImage`, `+0x48 FullDllName` (버퍼 `+0x50`), `+0x58 BaseDllName` (버퍼 `+0x60`), `+0x68 Flags`, `+0x108 BaseNameHashValue`.
  - `Flags` 비트: **2 `ImageDll`**, **22 `CorImage`**, **23 `DontRelocate`**, **24 `CorILOnly`**.
- `_PEB`: `+0x3` 비트 0 `ImageUsesLargePages`, `+0x10` `ImageBaseAddress`, `+0x90` `SharedData`, `+0x378` `TracingFlags` (비트 2 = `LibLoaderTracingEnabled`).
- `_TEB`: `NtTib +0x28 ArbitraryUserPointer`, `+0x30 Self` (`GS:[0x30]`).
- `_KUSER_SHARED_DATA +0x380 UserModeGlobalLogger` (ushort 16개). `+0x384`/`+0x385` = 둘째 칸의 아래/위 바이트. `_SILO_USER_SHARED_DATA +0x226 UserModeGlobalLogger` → 같은 칸이 `+0x22a`/`+0x22b`.
- `_IMAGE_NT_HEADERS64` (`NT`): `+0x16` FileHeader.`Characteristics`, `+0x30` `ImageBase`, `+0x48` `MajorSubsystemVersion`, `+0x50` `SizeOfImage`, `+0x54` `SizeOfHeaders`, `+0x5e` `DllCharacteristics`, `+0x84` `NumberOfRvaAndSizes`, `+0xf8` 디렉터리 14(.NET) 의 RVA (PE32 는 `+0x74`/`+0xe8`).

`_LDRP_LOAD_CONTEXT` (`ctx`) — 이번에 **확정/더한** 칸 (나머지는 sast/17·18·21 표):

| 칸 | 역할 | 근거 |
|---|---|---|
| `+0xa8` | **보기 크기(ViewSize) 받는 칸.** sast/21 에서 "맵 크기 `[추측]`" 이던 것이 확정. 맵 시스템 콜이 쓴다. 이어서 `RtlImageNtHeaderEx`·`LdrpRelocateImage` 가 읽음 | `MinimalMapModule +0xcd`·`+0xd4`·`+0x160` |
| `+0x20` 비트 `0x800` | 켜지면 맵 호출이 `Ex` 로 바뀜 + 주소 상한(`LdrpMaximumUserModeAddress`) 확장 인자 | `+0x11d`, `+0x30b~+0x327` |
| `+0x20` 비트 `0x800000` | 켜지면 `Ex` + 보호값 `PAGE_READONLY`(2) + 할당값에 `0x40000` + 확장 인자 유형 5 (값 `0x200`). `CompleteMapModule` 에선 .NET 헤더가 있으면 `0xc000007b` | `+0x132`, `+0x32c`, `Complete +0x109~+0x117` |
| `+0x20` 비트 `0x200000` | `LdrpProcessMachineMismatch` 가 켬 (.NET IL 전용인데 기계 종류 불일치). `CompleteMapModule` 이 읽고 `LdrpCorFixupImage` 를 부름 | `Mismatch +0x37`, `Complete +0x294` |
| `+0x20` 비트 `0x80` | 켜지면 DLL 헤더에 `FORCE_INTEGRITY`(`DllCharacteristics 0x80`) 가 있어야 함 `[추측: 서명 요구]` | `Complete +0xcc`, `+0x2c4` |
| `+0xb0` | `≠ 0` 이면 `0x40000003` 뒤 "재시도 검사" 건너뜀 (sast/21 은 뜻 모름 그대로) | `MinimalMap +0x288` |

상태 코드 (이 두 함수가 다루는 것):

| 값 | 이름 | 여기서 |
|---|---|---|
| `0x40000003` | `STATUS_IMAGE_NOT_AT_BASE` (요청서) | **유저 재배치를 일으키는 값.** 그대로 caller 로 올라감 |
| `0x40000036` | **이름 확인 못 함** (`STATUS_IMAGE_AT_DIFFERENT_BASE` 였던 기억 `[추측]`) | `0x40000003` 과 같은 갈래를 타지만 유저 재배치는 **안 함** (아래 2a) |
| `0x4000000e` | `STATUS_IMAGE_MACHINE_TYPE_MISMATCH` | `LdrpProcessMachineMismatch` 로 감 |
| `0xc0000018` | `STATUS_CONFLICTING_ADDRESSES` `[추측: SDK 이름 기억]` | kernel32 특수 갈래 (1c) |
| `0xc000022d` | `STATUS_RETRY` 로 기억 `[추측]` (sast/21 에선 이름 모름) | 재시도 검사가 참일 때 (1c) |
| `0xc000007b`, `0xc0000428` | `STATUS_INVALID_IMAGE_FORMAT`, `STATUS_INVALID_IMAGE_HASH` `[추측: SDK 기억]` | Mismatch 의 거절 응답, Complete 의 서명 검사 실패 |

## 질문별 답 [정적]

### 1. `LdrpMinimalMapModule` (rva `0xa220`, 915B)

**인자**: `RCX = ctx`, `RDX = 섹션 핸들` (입구 `+0x0` 에서 홈 자리에 저장). 입구에서 `RDI = E = ctx+0x38` (`+0x1b`), `RSI = ctx`.

#### 1a. 붙이는 길

- **기본 길은 `+0x174 NtMapViewOfSection` (ret `+0x179`) 이다. 예.** `+0x36c NtMapViewOfSectionEx` 는 **확장 인자가 하나라도 만들어졌을 때만** 탄다.
  - 조건 줄: `+0x11d TEST [ctx+0x20],0x800` → `JNZ +0x12c` (목적지 `+0x30b`: 주소 상한 확장 인자 1개 만듦, `ECX = 1`). `+0x132 TEST R8D` (`ctx+0x20 & 0x800000`) → `JNZ +0x135` (목적지 `+0x32c`: 유형 5 확장 인자 1개 더). `+0x13f TEST ECX,ECX` → `JNZ +0x141` (목적지 `+0x346`) = **Ex 길**. 둘 다 꺼지면 `ECX = 0` 이라 `+0x174` 로 간다.
  - 두 갈래는 `+0x179` 에서 합쳐진다 (Ex 는 `+0x371 JMP +0x179`). 그래서 **멈춤점 하나(`ntdll+0xa399`)로 둘 다 잡힌다.**
  - 두 비트(`0x800`, `0x800000`)를 **켜는 줄은 ntdll 의 뽑아 둔 함수들에서 못 찾았다** (`OR`/`BTS` 로 `+0x20` 에 쓰는 줄 검색 0건). test.exe 에서 Ex 를 탈지는 `ctx+0x20` 값을 읽어야 안다 → 동적. 비트 `0x800` 은 sast/21 의 `NtFileName +0x4a0`(WOW64 일 때만 의미) 로 보아 32비트 쪽 비트로 보인다 `[추측]`.
- **`NtMapViewOfSection` 인자 한 줄** (x64 호출 규약: 처음 넷은 `RCX/RDX/R8/R9`, 나머지는 스택 `[RSP+0x20]` 부터):

| 순서 | 이름 (SDK) | 값 | 줄 |
|---|---|---|---|
| 1 | SectionHandle | **`LdrpMinimalMapModule` 의 둘째 인자** (홈 `[RBP+0x6f]`). `ctx` 칸이 아니다. 부른 쪽은 `LdrpMapDllWithSectionHandle +0x43~+0x49` 에서 `RDX = 섹션 핸들` | `+0x147` |
| 2 | ProcessHandle | `-1` (자기 프로세스) | `+0x14f OR RDX,-1` |
| 3 | BaseAddress | **`&E.DllBase`** (`RDI+0x30`). 넣기·받기 겸용 칸. 처음엔 0 | `+0x13b`, `R8` |
| 4 | ZeroBits | 0 | `+0x165` |
| 5 | CommitSize | 0 | `+0x16e` |
| 6 | SectionOffset | `NULL` | `+0x168` |
| 7 | **ViewSize** | **`&ctx+0xa8`** (호출 전 `+0xd4 AND [R9],0` 으로 0 — "전체") | `+0xcd`, `+0x160` |
| 8 | InheritDisposition | `1` (`ViewShare`) | `+0x158` |
| 9 | AllocationType | **`0x800000`** 가 기본. `ctx+0x20 & 0x800000` 이면 `\| 0x40000` (`+0x103 BTS`). 큰 페이지 갈래(1d)가 성공하면 `0x20000000` | `+0x153` |
| 10 | Win32Protect | **`0x80`** 이 기본 (`PAGE_EXECUTE_WRITECOPY` 값). `ctx+0x20 & 0x800000` 이면 `2` (`PAGE_READONLY` 값) | `+0x14b`, 계산 `+0xf8~+0x10b` |

  - 숫자는 `.asm` 그대로. 이름 `MEM_DIFFERENT_IMAGE_BASE_OK`(`0x800000`)·`MEM_LARGE_PAGES`(`0x20000000`) 는 SDK 상수를 기억으로 맞춘 것 `[추측: 이름]`. 뜻이 맞다면 "헤더의 기준 주소가 아닌 곳에 붙여도 된다" 는 허락이라, 이 비트가 있어야 `0x40000003` 같은 경고 코드가 나올 수 있다 `[추측]`.
  - `Ex` 길 인자: SectionHandle 같음, ProcessHandle `-1`, BaseAddress `&E.DllBase`, SectionOffset `NULL`, ViewSize `&ctx+0xa8` (`[RSP+0x20]`), AllocationType 같은 값 (`[RSP+0x28]`), Win32Protect 같은 값 (`[RSP+0x30]`), 확장 인자 배열 `RBP-1` (`[RSP+0x38]`), 개수 `ECX` (`[RSP+0x40]`). 배열 칸: 유형 1 (주소 요구: 최하 0, **최상 `LdrpMaximumUserModeAddress`**, 정렬 0), 유형 5 (값 `0x200`) `[추측: SDK 모양]`.
- **곁다리 (디버거와 관계)**: 부르기 직전 `TEB.NtTib.ArbitraryUserPointer`(`TEB+0x28`) 를 `E.FullDllName.Buffer`(`E+0x50`) 로 바꾸고 (`+0xe2~+0xea`), 부른 뒤 원래 값으로 복구한다 (`+0x17b~+0x17f`). 맵 도중 DLL 이름을 커널에 알려주는 자리로 보인다 `[추측]`.

#### 1b. 항목의 `DllBase`·`SizeOfImage` 를 여기서 채우나?

- **`DllBase`: 이 함수가 값을 쓰는 줄은 없다. 시스템 콜이 직접 쓴다** `[정적]`. `BaseAddress` 인자가 `&E.DllBase` 라서 (`+0x13b LEA R8,[RDI+0x30]`) 맵이 끝나면 그 칸에 붙은 주소가 들어 있다. 이 함수가 `DllBase` 에 쓰는 줄은 **0 으로 되돌리는 `+0x2d9 AND [RDI+0x30],0`**(Unmap 뒤) 하나뿐.
- **보기 크기**: 같은 식으로 `ctx+0xa8` 에 시스템 콜이 쓴다 (위 표 7).
- **`SizeOfImage`(`E+0x40`): 여기서 안 채운다.** 쓰는 줄은 `LdrpInsertModuleToIndexLockHeld +0x2e` (NT 헤더 `+0x50` 값) 이고, `ctx+0x20 & 0x800000` 이 켜진 길은 `LdrpMapDllWithSectionHandle +0x222` 가 쓴다. 곧 **`SizeOfImage` 는 "맵 결과" 가 아니라 "파일 헤더 값"** 이다 (`LdrpInsertModuleToIndexLockHeld` 는 `+0x151` 에서 부름: sast/21).

#### 1c. 결과 코드 처리 — 돌려주는 값

`NtMapViewOfSection` 결과 `EBX` 에 대해 (`+0x190~+0x1aa` 가르기):

| 결과 | 갈래 | 돌려주는 값 |
|---|---|---|
| `0` (성공) | 그대로 `+0x1b0` | `0` |
| `0x40000003` 또는 `0x40000036` (`+0x192 SUB ECX,0x40000003 → JZ`, `+0x1a7 CMP ECX,0x28 → JZ`; 둘 다 `+0x288` 으로 점프) | **`ctx+0xb0 == 0` 이고 `LdrpMapAndSnapWork ≠ 0` 일 때만** `+0x2a9 LdrpCheckForRetryLoading(ctx, 1)`. 참(`AL ≠ 0`) → `EBX = 0xc000022d`. 거짓 → `+0x384`: kernel32·LAA exe 판정(`R14B`)이 참이면 `EBX = 0xc0000018`, 아니면 그대로. 위 조건이 아니면 검사 없이 그대로 `+0x1b0` | 보통 **받은 값 그대로** (`0x40000003`/`0x40000036`) |
| `0x4000000e` (`+0x19e SUB ECX,0xb` → `JZ`, 목적지 `+0x2fc`) | `+0x2ff LdrpProcessMachineMismatch(ctx)` → 결과가 `EBX` | 아래 "Mismatch" |
| 그 밖의 음수 | 바로 `+0x1b0` | 받은 값 |
| 그 밖의 양수 | 바로 `+0x1b0` | 받은 값 |

- 마지막 정리 (`+0x1b0~+0x1b7`, `+0x2c0~+0x2de`): **`E.DllBase ≠ 0` 이고 (`EBX < 0` 이거나 `EBX == 0x4000000e`)** 이면 **`+0x2d4 NtUnmapViewOfSection(-1, E.DllBase)` 하고 `E.DllBase = 0`**. 곧 성공 코드(경고 포함)는 맵을 **그대로 둔다**. 음수와 `0x4000000e` 는 맵을 지운다. 로그 두 줄 (`+0x1e6`, `+0x214` "Status: …") 뒤 `+0x219 MOV EAX,EBX` 로 반환.
- **caller 가 받는 값**: `LdrpMapDllWithSectionHandle +0x4e` 는 `0x4000000e` 나 음수면 바로 끝내고, 그 밖은 `EBP` 에 두었다가 `LdrpCompleteMapModule` 셋째 인자로 넘긴다 (sast/21). 그러니 `CompleteMapModule` 이 받는 값은 **`0`, `0x40000003`, `0x40000036` 또는 `Mismatch` 가 0 으로 바꾼 경우의 `0`, 그 밖의 양수** 중 하나다.
- **`+0x2ff LdrpProcessMachineMismatch` (rva `0x10af3c`, 185B, depth 0)**: 인자 `RCX = ctx`. 안에서 `E = ctx+0x38`.
  - `+0x27 RtlImageDirectoryEntryToData(E.DllBase, 1, 14, &크기)` 로 .NET 디렉터리(14번)를 찾는다. 있고 `Flags & 1` (IL 전용) 이면 **`ctx+0x20 \|= 0x200000` 하고 `0` 을 돌려줌** (`+0x37`, `+0x3c`). 곧 "기계 종류가 달라도 IL 전용 .NET 이면 받아준다".
  - 아니면 `LdrpAppHeaders` (exe 의 NT 헤더) 의 `MajorSubsystemVersion`(`+0x48`) 을 본다 (`+0x56`): **3 보다 크면 (`+0x5c`) `0x4000000e` 를 그대로 돌려준다** → 위 정리에서 맵을 지움. 3 이하면 `+0x90 NtRaiseHardError(0x4000000e, 매개변수 1개(=E.FullDllName), …, 응답 옵션 2)` 로 사용자에게 묻고 (`+0x95`): 성공이고 응답이 3 이면 `LdrInitState ≠ 3` 일 때 `LdrpFatalHardErrorCount++`, `0xc000007b` 돌려줌; 응답이 3 이 아니면 `E.Flags` 의 `ImageDll` 을 끄고 (`+0xb4`) `NtRaiseHardError` 결과(보통 0) 를 돌려줌.
  - test.exe(`MajorSubsystemVersion` 6, 아래 "샘플 헤더")는 **3 보다 큰 쪽** — 즉 만약 이 갈래를 타면 `0x4000000e` 가 그대로 올라가 `LdrpMapDllSearchPath` 의 "다시 찾기" (sast/21 10번) 로 이어진다. dll1·dll2·kernel32 는 모두 x64 라 이 갈래는 안 탈 것 `[정적 조건 + 헤더 값]`.

#### 1d. 큰 페이지 갈래 (`+0x257 RtlQueryImageFileKeyOption` — Ghidra 이름 `LdrQueryImageFileKeyOption`, `+0x278 RtlAcquirePrivilege`, `+0x37a RtlReleasePrivilege`)

- 조건 (`+0xbb`, `+0x22e~+0x238`): **`R14B == 0`** (kernel32 + LAA exe 가 아님) **그리고 전역 `LdrpLargePageDllKeyHandle ≠ 0`**.
- 하는 일: `+0x257` 로 그 핸들(레지스트리 키) 아래에서 **DLL 이름(`E.BaseDllName.Buffer`)과 같은 이름의 DWORD 값**(유형 4, 크기 4) 을 읽는다. 값이 0 이 아니면 `+0x278 RtlAcquirePrivilege(&LdrpLockMemoryPrivilege(값 4), 1, 0, &상태)`. 성공(`EAX ≥ 0`, `+0x27f CMOVNS`) 이면 **AllocationType 을 `0x20000000` 으로 바꾼다.** 맵을 부른 뒤 `AllocationType == 0x20000000` 이었으면 `+0x37a RtlReleasePrivilege(상태)`.
- `LdrpLargePageDllKeyHandle` 은 **`LdrpInitializeExecutionOptions +0x389` 가 `PEB.ImageUsesLargePages`(PEB+3 비트 0) 일 때만** `NtOpenKey` 로 연다 (`+0x3c6`; 키 이름은 `LargePageDLLs` — 원본 바이트에서 읽음). 없으면(`0xc0000034`) 0 으로 되돌림 (`+0x3d4`). `LdrpInitializeProcess +0x178d~+0x179e` 가 나중에 닫고 0 으로 쓴다. 쓰는 곳은 이 둘뿐 (`refs`).
- **test.exe 의 kernel32·dll1·dll2 에서 탈까?** 아니라고 본다 `[정적 조건 + 추측]`. exe 가 큰 페이지를 쓴다고 표시돼 있지 않으면 핸들이 0 이라 `+0x238 JZ` 로 건너뛴다 (sample 에 그런 설정 없음 `[소스]`). 동적으로 `LdrpLargePageDllKeyHandle` 값 하나만 읽으면 끝.
- **`R14B`(kernel32 + LAA exe)**: `+0x92 RtlEqualUnicodeString(E.BaseDllName, LdrpKernel32DllName="KERNEL32.DLL", 대소문자 무시)` 이 참이고 `LdrpAppHeaders+0x16 & 0x20` (exe `Characteristics` 의 `LARGE_ADDRESS_AWARE`) 이면 `R14B = 1`. **test.exe 헤더 `Characteristics` = `0x22`** 라 LAA 가 켜져 있다 (아래 "샘플 헤더"). 따라서 test.exe 에서 kernel32 를 올릴 때 `R14B = 1`: 큰 페이지 갈래는 건너뛰고, "재시도 검사 거짓일 때 `0xc0000018`" 갈래가 열려 있다. 그런데 그 갈래는 `LdrpMapAndSnapWork ≠ 0` 일 때만 닿고, kernel32 는 **병렬 켜기(`LdrpInitializeProcess +0x1a47`) 앞에서** 올라오므로 (run24 `LdrpMapAndSnapWork == 0`) kernel32 에선 안 탄다 `[정적: 코드 + run24 순서]`. 뜻(왜 kernel32 만 따로인가)은 모름.

### 2. `LdrpCompleteMapModule` (rva `0xa5c0`, 940B)

**인자**: `RCX = ctx`, `RDX = NT 헤더` (`R15`), `R8D = 맵 결과 코드` (`R12D`). 입구에서 `R14 = E = ctx+0x38`, `RDI = E.DllBase` (낮은 두 비트가 있으면 떼어 내고 `BL = !(비트 0)` — 데이터 파일로 맵된 경우를 구별하는 칸으로 보임 `[추측]`, 보통 `BL = 1`).

**재배치 담당인가: 예.** `LdrpRelocateImage` 를 부르는 코드는 **`+0x31b` 하나뿐**이다 (`refs LdrpRelocateImage` — 이 줄 말고는 함수 밖 `.pdata` 칸 하나뿐). 다만 **실제로 주소를 고치는 줄은 두 단계 아래**(`LdrpRelocateImage +0x14a` → `LdrpProtectAndRelocateImage +0x8e LdrRelocateImageWithBias`)다.

#### 2a. `+0x31b LdrpRelocateImage` 를 부르는 조건

위에서 아래로, **다 참이어야** 닿는다 (`.asm` 줄):

1. `NT.FileHeader.Characteristics & 0x2000` (DLL 비트) 가 켜져 있다 (`+0xbc~+0xc6`). 꺼졌으면 `E.Flags` 의 `ImageDll` 을 끄고 (`+0x2ba`) 끝 — exe 를 DLL 처럼 올린 경우의 정리.
2. `ctx+0x20 & 0x80` 이 꺼져 있거나, 켜졌다면 `NT.DllCharacteristics & 0x80` 이 켜져 있다 (`+0xcc`, `+0x2c4`). 아니면 로그 (`ldrmap.c` 줄 `0x413`) 하고 `0xc0000428` 로 끝 (`+0x2fa`, `+0x2ff`).
3. `E.Flags` 의 `CorILOnly`(비트 24) 가 꺼져 있다 (`+0xd7 TEST [R14+0x68],0x1000000` → `JNZ` 끝). (IL 전용 .NET 은 원래 재배치가 없다.)
4. **맵 결과가 `0x40000003` 또는 `0x40000036`** (`+0xe1~+0xef`). 아니면(`0` 포함) 끝.
5. **맵 결과가 정확히 `0x40000003`** (`+0x1b4 CMP R12D,0x40000003 → JZ`, 목적지 `+0x309`). 이때만 `+0x309~+0x31b` 에서 `LdrpRelocateImage(E.DllBase, ctx+0xa8 값, NT 헤더, &E.FullDllName)` 을 부른다. `0x40000036` 이면 `+0x1c1` 로 건너뛴다 (재배치 없이 ETW 두 번만 남음).
6. 부른 뒤: 결과가 음수면 `+0x338 LdrpLogError(상태, 0x1490, 0, &FullDllName)` 하고 그 상태를 돌려줌 (**재배치 실패 = 로드 실패**). 0 이상이면 `+0x1c1` 로 이어짐.

- **조건이 아닌 것**: `.reloc` 유무, `DYNAMIC_BASE`(`DllCharacteristics 0x40`) 는 이 함수가 안 본다. `.reloc` 과 `RELOCS_STRIPPED` 는 한 단계 아래 `LdrpRelocateImage` 가 본다 (2b).
- 곁가지: 맵 결과가 `0x40000003`/`0x40000036` 이고 위 1~3 을 통과하면, 재배치 앞뒤로 ETW 이벤트 `0x1490`(앞)·`0x1491`(뒤) 가 남는다 (2c). `0x40000036` 일 때도 이벤트는 남는다.

#### 2b. `LdrpRelocateImage` (rva `0xe3984`, 359B) — depth 0

- **인자**: `RCX = E.DllBase`, `RDX = ctx+0xa8 값(보기 크기)`, `R8 = NT 헤더`, `R9 = &E.FullDllName`. 반환 `NTSTATUS`. 로그 `ldrmap.c` 줄 `0x163`·`0x164`(들어가며), `0x18a`·`0x18b`(나가며).
- **하는 일** (줄):
  - `+0x7d TEST [R8+0x16],1` — NT `Characteristics` 의 **`RELOCS_STRIPPED`**. **꺼져 있으면** (`+0x95 RtlImageDirectoryEntryToData(DllBase, 1, 5(재배치 디렉터리), &크기)`): 디렉터리가 없거나 크기 0 이면 **아무것도 안 하고 `0` 반환** (`+0x9d`, `+0x115`). 있으면 다음으로.
  - **켜져 있으면** 위 검사를 건너뛰고 바로 다음으로 (`+0x81 JNZ +0x117`).
  - `+0x12b RtlImageDirectoryEntryToData(DllBase, 1, 14(.NET), &크기)`: 디렉터리가 없거나, 크기가 `0x48` 미만이거나, `Flags & 1`(IL 전용) 이 꺼져 있으면 → **재배치.** (IL 전용 .NET 이면 `0` 반환, 안 함.)
  - 재배치 = `+0x142 LdrpLogDllRelocationEtwEvent(R9=이름, 헤더 ImageBase(NT+0x30), 실제 DllBase, 보기 크기)` → `+0x14a LdrpProtectAndRelocateImage(DllBase)` → 그 결과 반환.
- **한 단계 아래** `LdrpProtectAndRelocateImage` (rva `0xe3af4`, 418B, depth 0 로 이름·호출만): `+0x4a NtQueryVirtualMemory(-1, DllBase, 클래스 4, …)` (이미지 첫 쪽 속성 조회로 보임 `[추측]`) → `+0x8e LdrRelocateImageWithBias(DllBase)` → `+0xbc`·`+0x16b LdrpSetProtection(DllBase, 0/1)` (**보호 바꾸기**: 갈래에 따라 재배치 앞/뒤 `[추측]`) + 로그 넷 + 예외 필터 `LdrpGenericExceptionFilter`. 본문(편차 계산·블록 처리)은 안 읽음.
- **재배치 줄(`LdrRelocateImageWithBias`)의 caller 는 `LdrpProtectAndRelocateImage +0x8e` 하나. 그 함수를 부르는 곳은 셋** (`refs`): `LdrpRelocateImage +0x14a`(DLL), **`LdrpInitializeProcess +0x1430`(exe)**, `LdrpLoadCustomNtdllWithSectionHandle +0x5a`(custom ntdll).

#### 2c. 나머지 갈래 한 줄씩

| 호출 | 조건 | 한 줄 |
|---|---|---|
| `+0x58 RtlImageNtHeaderEx(1, DllBase, 0, &NT)` | 항상 | 맵된 이미지의 NT 헤더 포인터를 얻음. 못 얻으면 .NET 검사를 건너뜀 |
| `+0x129 RtlAddressInSectionTable` | .NET 디렉터리 RVA 가 있고, `BL == 0`(데이터 파일로 맵) 이며 RVA ≥ `SizeOfHeaders` | RVA 를 파일 안 위치로 바꿈. 이미지로 맵된 보통 DLL 은 `DllBase + RVA` 로 바로 계산 (`+0xa9`)해서 안 탄다 |
| `+0x285 LdrpCorValidateImage(DllBase)` | .NET 디렉터리가 **있고** `ctx+0x20 & 0x800000` 꺼짐, 헤더 `Flags & 1`(IL 전용). 호출 전 `E.Flags` 에 `CorImage`(+0x26b)·`CorILOnly`(+0x279) 를 켬 | .NET 헤더 검증. 음수면 그 코드로 끝 |
| `+0x2a6 LdrpCorFixupImage(DllBase)` | 위에 더해 `ctx+0x20 & 0x200000` (Mismatch 가 켠 것) | 기계 종류 불일치인 IL 전용 이미지의 주소 고치기 `[추측: 이름]`. 음수면 끝 |
| `RtlGetCurrentServiceSessionId` ×4 (`+0x16b`, `+0x1c5`, `+0x204`, `+0x354`) | 재배치 앞뒤 ETW 켜짐 검사마다 | 결과 0 이면 전역 `KUSER_SHARED_DATA` (`0x7ffe0384`/`0x7ffe0385`), 아니면 `PEB.SharedData(+0x90)+0x22a/0x22b` 칸을 읽음 (= `UserModeGlobalLogger` 둘째 칸 아래/위 바이트, PDB). 서비스 세션별 사본 선택 |
| `LdrpLogEtwEvent` ×2 (`+0x39e` → 이벤트 `0x1490`, `+0x24f` → `0x1491`) | `PEB.TracingFlags` 비트 2(`LibLoaderTracingEnabled`) 켜짐, 위 첫째 바이트 `≠ 0`, 둘째 바이트 `& 0x20` | 재배치 **앞/뒤** 이벤트. 맵 결과가 `0x40000003`/`0x40000036` 일 때. `LdrpLogError` 는 재배치 실패 때만 |

- 그 밖: `+0x2fa LdrpLogInternal`(서명 실패 문구), `+0x338 LdrpLogError`.
- 이 함수의 반환은 거의 항상 `ESI`(= 0, 또는 .NET 검사/Fixup/재배치/서명의 오류 코드). 평범한 native DLL 은 .NET 디렉터리가 없어 `+0xaf` 로 곧장 가서 (`ECX = 0xc0000002` 는 "디렉터리 없음" 표시일 뿐 오류로 안 돌려줌) 1~5 검사만 하고 끝난다 `[정적: 경로 따라감]`.

### 3. 커널 재배치와의 관계

- `[정적]` **유저 재배치의 문은 `NtMapViewOfSection` 결과가 정확히 `0x40000003` 인 것 하나뿐이다** (2a 5번). `0` 이면 `LdrpRelocateImage` 를 부르지 않는다 (2a 4번에서 이미 끝).
- `[정적]` 같은 문이 `0x40000036` 은 **통과는 시키되 재배치는 안 시킨다.** 코드가 두 값을 구별한다는 뜻이다 (`MinimalMap +0x192`/`+0x1a7` 도, `Complete +0xe4`/`+0x1b4` 도).
- `[정적]` 커널이 `0` 을 돌려주나 `0x40000003` 을 돌려주나는 **ntdll 코드로는 정해지지 않는다.** 시스템 콜 안쪽(커널 `NtMapViewOfSection`)이라 run31 Q61·Q62 몫.
- `[추측]` (근거 약함) 커널이 섹션을 만들 때 이미 델타를 더해 둔다면 (run31 `MiRelocateImage`) ASLR 로 옮겨진 보통 DLL 은 `0` 또는 `0x40000036`("다른 주소지만 고쳐져 있음" 쪽) 일 수 있고, 그러면 **보통 DLL 은 유저 재배치를 안 한다.** 이 추측은 두 가지에 기댄다: ① `0x40000036` 이름 기억, ② 커널이 메모리 헤더의 `ImageBase` 도 새 값으로 고치는지 (exe 쪽 `LdrpInitializeProcess +0x1419 CMP [NT+0x30], PEB.ImageBaseAddress` 가 "헤더 ImageBase 와 실제 주소가 다르면 유저 재배치" 인 것으로 보아, 평소엔 같다고 기대하는 코드로 보임). 둘 다 정적으로 확인 못 함 → **동적으로 `NtMapViewOfSection` 반환 `eax` 를 kernel32·dll1·dll2 각각 보는 것** (아래).
- 힌트 (`[정적: 헤더 값]`): kernel32·ntdll·dll1·dll2 의 **헤더 `ImageBase` 가 전부 `0x180000000`**, test.exe 는 `0x140000000`, 다 `DYNAMIC_BASE`+`.reloc` 있음. 헤더 주소는 한 번에 한 DLL 만 쓸 수 있으니 이 중 적어도 몇은 어차피 다른 주소에 붙는다. 그래서 실제로 어떤 코드가 나오는지가 중요하다.

### 4. 이 둘이 exe 에도 불리나?

- **아니다 `[정적]`.** 지도 `_nodes.tsv` 의 `callers_total`: `LdrpMinimalMapModule` 1, `LdrpCompleteMapModule` 1, `LdrpRelocateImage` 1, `LdrpProcessMachineMismatch` 1. 각 caller 는 `LdrpMapDllWithSectionHandle`(`+0x49`, `+0x24a`) / `LdrpCompleteMapModule` / `LdrpMinimalMapModule(+0x2ff)` 뿐이다. exe 는 커널이 이미 붙여서 이 길로 안 온다.
- exe 쪽 재배치는 **다른 줄**이다: `LdrpInitializeProcess +0x1419 CMP [NT+0x30], PEB.ImageBaseAddress` (`PEB+0x10`) → 다르고, IL 전용이 아니고, `UseWOW64 == 0` 이면 `+0x1430 LdrpProtectAndRelocateImage(PEB.ImageBaseAddress)`. 곧 `LdrpRelocateImage` 와 `0x40000003` 문을 **건너뛴다.** exe 항목은 `+0x1450 LdrpInsertDataTableEntry` 로 명부에 들어간다.

## 구간 표

### `LdrpMinimalMapModule` (915B)

| 구간 | 하는 일 | 호출 |
|---|---|---|
| `+0x0 ~ +0x84` | 입구. `E = ctx+0x38`. 로그 둘 ("DLL name: %wZ") | `+0x50`, `+0x7f LdrpLogInternal` |
| `+0x84 ~ +0xbe`, `+0x2e3 ~ +0x2f7` | `R14B` 판정: `E.BaseDllName == "KERNEL32.DLL"` 이고 exe `Characteristics & 0x20` | `+0x92 RtlEqualUnicodeString` |
| `+0x22e ~ +0x283` | 큰 페이지 갈래 (`R14B == 0` 이고 핸들 `≠ 0`): IFEO 값 읽기, 권한 잡기, 할당값 `0x20000000` | `+0x257`, `+0x278` |
| `+0xc4 ~ +0x174`, `+0x30b ~ +0x346`, `+0x346 ~ +0x36c` | 인자 만들기: `ArbitraryUserPointer` 저장·바꿈, `ViewSize = 0`, 할당·보호 계산, 확장 인자 (`0x800`, `0x800000`) | 없음 |
| `+0x174` / `+0x36c` → `+0x179` | **시스템 콜**: `NtMapViewOfSection` (기본) / `NtMapViewOfSectionEx` (확장 인자 있을 때) → 포인터 복구 → 큰 페이지였으면 권한 풀기 | `+0x174`, `+0x36c`, `+0x37a RtlReleasePrivilege` |
| `+0x190 ~ +0x1b0`, `+0x288 ~ +0x2c0`, `+0x2fc`, `+0x384` | 결과 코드 가르기: 경고(`0x40000003`/`0x40000036`) → 재시도 검사, `0x4000000e` → Mismatch | `+0x2a9`, `+0x2ff` |
| `+0x1b0 ~ +0x22c`, `+0x2c0 ~ +0x2de` | 정리·반환: `DllBase ≠ 0` 이고 (음수 or `0x4000000e`) 면 Unmap, `DllBase = 0`. 로그 둘 | `+0x2d4 NtUnmapViewOfSection`, `+0x1e6`, `+0x214` |

### `LdrpCompleteMapModule` (940B)

| 구간 | 하는 일 | 호출 |
|---|---|---|
| `+0x0 ~ +0x58` | 입구. `E`, `DllBase`(낮은 비트 떼기), NT 헤더 얻기 | `+0x58 RtlImageNtHeaderEx` |
| `+0x6a ~ +0x162`, `+0x259`, `+0x34a` | .NET 디렉터리(14번) 찾기 — 없으면 그냥 지나감 (native DLL 은 여기서 `+0xbc` 로) | `+0x129 RtlAddressInSectionTable` (데이터 파일 맵만) |
| `+0x109 ~ +0x117`, `+0x263 ~ +0x2b5` | .NET 있을 때: `ctx&0x800000` 이면 `0xc000007b`; `E.Flags` 에 `CorImage`·`CorILOnly`; 검증·고치기 | `+0x285 LdrpCorValidateImage`, `+0x2a6 LdrpCorFixupImage` |
| `+0xbc ~ +0xf1`, `+0x2ba`, `+0x2c4 ~ +0x304` | 공통 검사: DLL 비트, 서명 요구(`ctx&0x80`), `CorILOnly` 면 끝, 맵 결과 `0x40000003`/`0x40000036` 인가 | `+0x2fa LdrpLogInternal` |
| `+0x167 ~ +0x1b4`, `+0x354 ~ +0x3ab` | **재배치 앞** ETW(`0x1490`) 켜짐 검사·남기기 | `+0x16b`, `+0x354 RtlGetCurrentServiceSessionId`, `+0x39e LdrpLogEtwEvent` |
| `+0x1b4`, `+0x309 ~ +0x33d` | **재배치**: 맵 결과 == `0x40000003` 일 때 `LdrpRelocateImage`; 음수면 에러 로그 후 끝 | `+0x31b LdrpRelocateImage`, `+0x338 LdrpLogError` |
| `+0x1c1 ~ +0x254` | **재배치 뒤** ETW(`0x1491`) | `+0x1c5`, `+0x204`, `+0x24f` |

## 대표 지점 (그림 2 상자 글 고칠 것)

1. **`jm1`/`jn1` — `LdrpMinimalMapModule`** (지금 "정적: 메모리에 붙음. 지도상 안에 +0x179 NtMapViewOfSection" → 고침):
   > **`+0x174 NtMapViewOfSection`** (기본; `ctx.flags` 의 `0x800`/`0x800000` 면 `+0x36c Ex`). 섹션 핸들 = 둘째 인자, 기준 주소 칸 = `&항목.DllBase`(0 으로 시작, 커널이 채움), 보기 크기 = `&ctx+0xa8`, 할당 `0x800000`·보호 `0x80`. 결과 `0x40000003` 은 그대로 위로 올림 `(정적)`
2. **`jm2`/`jn4` — `LdrpCompleteMapModule`** (지금 "재배치 후보 추측, Q52" → 고침):
   > **재배치 담당 (예)**: **맵 결과가 정확히 `0x40000003` 일 때만** `+0x31b LdrpRelocateImage` (DLL 이고 .NET 전용이 아닐 때). `0` 이면 안 함 `(정적)`. 실제 주소 고치기는 `LdrpRelocateImage` → `LdrpProtectAndRelocateImage +0x8e LdrRelocateImageWithBias` (두 단계 아래)
3. (선택) **`LdrpRelocateImage` 상자** 새로 두려면: "재배치 디렉터리 없으면(또는 `RELOCS_STRIPPED` 꺼진 채 크기 0) `0` 반환 / IL 전용이면 안 함 / 아니면 `LdrpProtectAndRelocateImage`" — 한 줄. flow.md 2.6 "재배치는 이 세 함수 안에 없다 — 후보 둘" 의 **둘째 후보 (`LdrpProcessMappedModule +0x24d LdrpIsExecutableRelocatedImage`) 는 이번에 안 봤다.** 재배치 실행 줄은 첫째 후보에서 찾아졌지만, 둘째가 "재배치된 이미지인가" 를 다시 묻는 줄인지는 sast/19·30 쪽 확인 필요.

## 동적으로 확인할 것 (dast 요청 필요)

Q20 (kernel32·kernelbase)·Q23 (dll1·dll2) 와 **같은 실행에서 볼 수 있다.** 이미 `LdrpMinimalMapModule` `+0x179` 에 `k` 를 건다고 한 Q20 의 멈춤점과 **같은 주소(`ntdll+0xa399`)** 라 거기서 `eax` 와 아래 값만 더 찍으면 된다. 멈춤은 test.exe 일 때만 (커널 멈춤점은 모든 프로세스에 걸림 → 조건은 runbook 몫).

| 확인 | 어디서 (`ntdll+rva`) | 무엇을 | 번호 |
|---|---|---|---|
| **맵 시스템 콜 결과** (kernel32·kernelbase·dll1·dll2 각각 `0` 인가 `0x40000003` 인가 `0x40000036` 인가) | `+0xa399` (= `LdrpMinimalMapModule +0x179`, `NtMapViewOfSection`/`Ex` 둘 다 여기로 합쳐짐) | `r eax`; 붙은 주소 `poi(@rdi+0x30)`; 보기 크기 `poi(@rsi+0xa8)`; 이름 `du poi(@rdi+0x50)`; 헤더 `ImageBase` `dq poi(@rdi+0x30)+dwo(poi(@rdi+0x30)+0x3c)+0x30 L1` (붙은 주소와 같은지 = 커널이 헤더도 고쳤는지) | 새 (Q20·Q23 에 붙임) |
| Ex 길을 타나 | `+0xa58c` (`NtMapViewOfSectionEx` call) | **안 서면** 기본 길만 탄 것. 서면 `dd @rsi+0x20 L1` (ctx 플래그) | 새 |
| 입구의 `ctx` 플래그·이름 | `+0xa220` | `dd @rcx+0x20 L1` (비트 `0x800`/`0x800000`), `du poi(poi(@rcx+0x38)+0x60)` | 새 |
| 재배치가 서나 | `+0xe3984` (`LdrpRelocateImage` 입구) 또는 호출 자리 `+0xa8db` | **안 서면 "보통 DLL 은 유저 재배치 안 함"** 확정. 서면 `r rcx`(붙은 주소), `du poi(@r9+8)` | 새 |
| 재배치 실제 줄 | `+0xe3b82` (`LdrRelocateImageWithBias` call) | 위가 서면 같이 | 새 |
| `CompleteMapModule` 이 받은 결과 | `+0xa5c0` 입구 | `r r8d` (위 `eax` 와 같아야 함) | 새 |
| 재시도 검사·Unmap·Mismatch 가 안 서야 정상 | `+0xa4c9`, `+0xa4f4`, `+0xa51f` | 안 서면 `0x40000003` 이 그대로 지나간 것. 서면 `r eax` | 새 |
| 값 읽기 (안 탈 갈래) | 전역 `ntdll+0x1d2780` (`LdrpLargePageDllKeyHandle`), `ntdll+0x1d2640` (`LdrpMapAndSnapWork`) | `dq ntdll+0x1d2780 L1` (0 예상), `dq ntdll+0x1d2640 L1` (kernel32 올릴 때 0, dll1 올릴 때 ≠0 인가) | 새 (값 읽기) |
| exe 쪽 재배치 안 하나 | `+0x8d81d` (`CMP`), 호출 자리 `+0x8d834` | `+0x8d834` 가 **안 서야** 정상 (헤더 ImageBase = `PEB.ImageBaseAddress`). 서면 커널이 헤더를 안 고친 것 | 새 (run31 D4·D5 와 같은 실행) |

- 추천 멈춤점 우선순위: ① `ntdll+0xa399` (eax, 한 곳으로 네 DLL 모두), ② `ntdll+0xe3984` (서나/안 서나), ③ `ntdll+0x8d834` (exe).
- 산수: 호출 자리 = 함수 rva + 호출 `+0x…`, ret 은 호출 +5. `+0xa399` = `0xa220 + 0x179` (ret 자리).

## 요청서와 달라진 점 / 알린 것

- 요청서 1d 의 "별칭 주의": PDB 이름은 `RtlQueryImageFileKeyOption`, Ghidra 이름은 `LdrQueryImageFileKeyOption`.
- sast/21 의 `ctx+0xa8` "맵 크기 `[추측]`" 은 **ViewSize 칸으로 확정** (`NtMapViewOfSection` 인자 7).
- 요청서에 없던 새 사실 둘: ① 재배치 줄의 부르는 곳이 셋 (DLL / exe / custom ntdll), exe 는 `LdrpRelocateImage` 를 안 거친다. ② `0x40000036` 은 `0x40000003` 과 같은 갈래를 타되 재배치만 빠진다.
- 하지 않은 것: `LdrpCheckForRetryLoading`·`LdrpCorValidateImage`·`LdrpCorFixupImage`·`LdrpLogEtwEvent` 본문, `LdrRelocateImageWithBias`·`LdrpSetProtection` 본문, `LdrpProcessMappedModule +0x24d` (요청서 범위 밖).
- `LdrpMinimalMapModule`·`LdrpCompleteMapModule` 의 depth 1 callee 는 다시 안 뽑았다 (요청서도 "본문 안 읽음").

## 샘플 헤더 (정적 근거, `sample/bin/*` 를 직접 읽음)

| 파일 | `Characteristics` | `ImageBase` | `DllCharacteristics` | `.reloc` | .NET | `MajorSubsystemVersion` |
|---|---|---|---|---|---|---|
| test.exe | `0x22` (LAA 켜짐, `RELOCS_STRIPPED` 꺼짐) | `0x140000000` | `0x8160` | 있음 (`0x810`) | 없음 | 6 |
| dll1.dll | `0x2022` | `0x180000000` | `0x160` (`FORCE_INTEGRITY` 없음) | 있음 (`0x800`) | 없음 | 6 |
| dll2.dll | `0x2022` | `0x180000000` | `0x160` | 있음 (`0x804`) | 없음 | 6 |
| kernel32.dll (`ms/bin`) | `0x2022` | `0x180000000` | `0x4160` | 있음 (`0x664`) | 없음 | 10 |

- 이 빌드 산출물이 타깃 VM 에서 실제로 도는 파일과 같다는 확인은 안 했다 (같다고 보고 `[소스]` 로 취급).
- 위 표로 본 test.exe 길 요약 `[정적 조건 + 헤더]`: native, DLL 비트 있음, `FORCE_INTEGRITY` 요구(`ctx&0x80`)는 평소 꺼짐 `[추측]`, `CorILOnly` 꺼짐 → 2a 1~3번 통과. 4~5번(맵 결과)만 실행에 달림.

## 판정

**분석성공.** 요청서 질문 1~4 에 모두 `.asm` 줄로 답했다. 정적으로 못 정한 것은 셋: ① `NtMapViewOfSection` 이 ASLR 로 옮겨진 DLL 에서 `0`/`0x40000003`/`0x40000036` 중 무엇을 돌려주나 (커널 안쪽), ② `ctx+0x20` 의 `0x800`·`0x800000` 이 test.exe 길에서 켜지나, ③ `0x40000036` 의 이름. 모두 위 동적 표(특히 `ntdll+0xa399` 의 `eax`)로 닫힌다.

# 31 snap-module — 정적 분석 결과

- 요청서: `_workspace/sast/31_snap-module.md` (Q50)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`). PDB `ntdll.pdb` guid+age `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`ntdll.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 셋 다 있음, 돌고 있는 Ghidra 프로세스 없음). 지도 `_map/` 도 이미 있음.
- 뽑은 함수:
  - `LdrpDoPostSnapWork` (rva `0xa4200`, 228B) 새로 뽑음, depth 0 → `ms/decomp/ntdll/LdrpDoPostSnapWork.c`, `ms/disassemble/ntdll/LdrpDoPostSnapWork.asm`, `_calls.tsv` 5줄. 로그 `ms/ghidra/ntdll.export.log`.
  - `LdrpSnapModule` (rva `0x3a8c0`, 4052B) 은 `.asm`(908줄)·`.c`(734줄)·`_calls.tsv`(36줄+SEH 필터 1) 가 이미 있어서 다시 안 뽑음. 요청서가 depth 1 로 적었지만 callee 는 "이름·인자·갈래 조건만" 이라 본문을 뽑지 않았다 (이미 파일이 있는 `LdrpFreeReplacedModule`·`LdrpReportError`·`LdrpHandleTlsData` 도 열지 않음).
- 버전 확인: **일치.** `_calls.tsv` 에서 `LdrpMapAndSnapDependency +0x4bd/+0x4c2` → `LdrpSnapModule`, `LdrpProcessWork +0x12c/+0x131` → `LdrpSnapModule`, `LdrpInitializeProcess +0x1bb6/+0x1bbb` → `LdrpInitializeGraphRecurse`. `LdrpSnapModule` 호출 줄 수(36 + `$filt$0` 1)도 요청서와 같다.
- 칸 이름은 PDB 타입 (`llvm-pdbutil-14 dump -types ms/sym/ntdll.dll.pdb`, 스크래치 파일, 커밋 안 함): `_LDR_DATA_TABLE_ENTRY` `+0x30 DllBase`, `+0x40 SizeOfImage`, `+0x48 FullDllName`(버퍼 `+0x50`), `+0x58 BaseDllName`, `+0x6e TlsIndex`, `+0x98 DdagNode`, `+0xb0 LoadContext`. `_LDR_DDAG_NODE +0x38 State`. `_LDR_DLL_LOAD_REASON`: 0 StaticDependency, **1 StaticForwarderDependency**, 2 DynamicForwarderDependency, 3 DelayloadDependency. `_IMAGE_NT_HEADERS64` 만 있고 **`_IMAGE_IMPORT_DESCRIPTOR`·`_IMAGE_EXPORT_DIRECTORY`·`_IMAGE_THUNK_DATA64` 는 PDB 에 없다** (PE 규격 이름으로 씀). **`_LDRP_LOAD_CONTEXT` 칸 정보 없음** (sast/17 그대로 오프셋만).
- 샘플 PE 계산 (스크립트로 `sample/bin/*.dll|exe` 의 import 표와 `ms/bin/.../kernel32.dll` 의 export 표를 직접 읽음, 스크래치 파일, 커밋 안 함): 아래 "test.exe 에서는" 에 씀.

## 스크립트 문제

없음.

## 한눈에

| 질문 | 한 줄 답 |
|---|---|
| 1 | **IAT 칸을 쓰는 줄은 딱 하나: `LdrpSnapModule +0x3e7` (`MOV [RCX],RSI`).** 이름·서수·forwarder·리다이렉트 모두 이 한 줄로 모인다. 이름 찾기(hint 먼저 + 이분 탐색)는 **본문 인라인**이다 (예). 서술자는 **모듈의 전부**를 돈다. |
| 2 | **예, forwarder 처리.** `strrchr(".")` 로 `DLL.함수` 를 가르고 `LdrpLoadDependentModuleInternal(…, 이유=1)` 로 대상 DLL 을 그 자리에서 올린다. 단 대상이 `NTDLL` 이면 올리지 않고 `LdrpNtDllDataTableEntry` 를 바로 쓴다. `RtlCharToInteger` 는 `DLL.#서수` 꼴. 부모 카운터와 엮인다 (`STATUS_PENDING` 갈래). **지연 import 가 아니다.** |
| 3 | **`State = 5` 는 `+0xb87` 한 곳, `LdrpDoPostSnapWork`(`+0xb61`) 가 성공한 뒤.** 실패하면 이 함수는 State 를 안 쓴다 (4 그대로). |
| 4 | `LdrpDoPostSnapWork` = ① IAT 보호 원래대로 (예, `+0x3e`) ② CFG 포인터 칸 변조 검사 ③ `TlsIndex == 0` 이면 `LdrpHandleTlsData` ④ CFG export suppression 이면 `LdrpUnsuppressAddressTakenIat`. |
| 5 | **아니오.** snap 은 부모 노드·부모 카운터를 쓰는 줄이 없다. 유일한 길은 forwarder 의 `LdrpLoadDependentModuleInternal` 안. snap 끝맺음은 caller 가 `*(ctx+0x28)` 에 실패 상태만 옮긴다. |
| 6 | 실패 코드는 이름 `0xc0000139`, 서수 `0xc0000138`. 나머지 작은 갈래는 아래. |

## 로드 문맥(`ctx`) 칸 — 이 함수가 읽고 쓰는 것

`LdrpSnapModule(ctx)` 의 `RCX` = `ctx` = `R15`. 비스택 메모리에 쓰는 줄은 `.asm` 에 **7줄**뿐이다 (`+0x105`, `+0x3e7`, `+0x462`, `+0xa72`, `+0xb87`, `+0xe03`, `+0xefa`) + `INC` 두 줄 (`+0x402`, `+0x471`).

| 칸 | 뜻 | 근거 |
|---|---|---|
| `+0x20` | 플래그. 비트 `0x2000000` = import 리다이렉션 켬 (sast/17 `MapAndSnap +0xd6` 이 켬) | 읽음 `+0x308` |
| `+0x38` | 이 문맥의 모듈 항목 (= snap 할 모듈, 지금부터 "importer") | `+0x4c` |
| `+0x58` | 자식(의존) 항목 배열, 서술자 번호로 색인 | 읽음 `+0xca`, 쓰기 `+0x105` (교체 때) |
| `+0x68` | import 서술자 개수 | `+0xbe` |
| `+0x80` | **지금 서술자 번호** (이어서 하기용) | 읽음 `+0xb7`, `INC` `+0x471` |
| `+0x84` | **그 서술자 안에서 이미 끝낸 칸 수** (이어서 하기용) | 읽음 `+0x291`, `INC` `+0x402`, 0 으로 `+0x462` |
| `+0x88` | import 서술자 배열 포인터 (**20바이트 = PE 규격 서술자**, `+0` OFT, `+0x10` FirstThunk) | `+0x113` |
| `+0xc0` | 끝에서 `NtUnmapViewOfSection(현재 프로세스, 이 값)` 하고 0 으로 비우는 뷰 주소 (정체 모름) | `+0xb93`, `+0xeee~+0xefa` |
| `+0x70/+0x78/+0x90` | IAT 시작/크기/원래 보호값. **snap 본문은 안 읽는다.** `LdrpDoPostSnapWork` 가 쓴다 | `.asm` 에 `R15+0x70` 등 없음 |
| `+0x98/+0xa0` | `LdrpPrepareImportAddressTableForSnap`(`.c`) 이 적음: `+0xa0` = 이 모듈 LoadConfig 의 `GuardCFCheckFunctionPointer` 칸 주소(`LoadConfig+0x70`), `+0x98` = 준비 때 그 칸의 값. `LdrpDoPostSnapWork` 가 비교 | Prepare `.c`, DoPostSnapWork `+0x49~+0x66` |
| `+0x6c`, `+0x50`, `+0x28` | snap 이 직접 안 만진다. forwarder 호출 때 callee 가 `+0x6c`·`+0x50` 을 만지고, 실패하면 `importer.LoadContext+0x28` 이 가리키는 곳에 `*ptr = 상태` (`+0xe03`) | 질문 2·5 |

## `LdrpSnapModule` 큰 구간 표

(오프셋은 `.asm` 의 `+0x…` 그대로. 코드는 forwarder·실패 갈래가 주소순으로 여기저기 흩어져 있어서 "주소 범위"가 겹쳐 보일 수 있다.)

| # | 범위 | 하는 일 | 대표 호출 |
|---|---|---|---|
| 1 | `+0x0 ~ +0xb1` | 입구. 쿠키, `R15=ctx`, `RDI=ctx+0x38`(importer 항목), DllBase·`&FullDllName` 꺼내기, 시작 로그, "교체 대기" 정리, 8칸 캐시(`[RSP+0x240]`) 0 으로 | `+0x79` `LdrpLogDllState(…,0x14a6)`, `+0x81` `LdrpHandlePendingModuleReplaced`, `+0x94` `memset` |
| 2 | `+0xb7 ~ +0x14c`, `+0x462 ~ +0x478` | **서술자 고리 머리·꼬리.** `ctx+0x80 < ctx+0x68` 이면 자식 항목(`ctx+0x58[번호]`) 꺼냄, 교체됐으면 바꿔치기, 서술자(20B)에서 OFT·FirstThunk RVA 읽기. 서술자 끝나면 `ctx+0x84=0`, `ctx+0x80++` 하고 `+0xb1` 로 | `+0xfc` `LdrpFreeReplacedModule` (조건부) |
| 3 | `+0x152 ~ +0x2af` | **자식(export 하는 쪽)의 export 디렉터리 찾기.** NT 헤더 → 데이터 디렉터리 0번 → 주소 계산, 최근 확인한 8곳 캐시, CFG 켜져 있으면 함수 표 등록 확인, export 표 5칸(함수·이름·서수 표, 이름 수, 끝 주소) 스택에 캐시 | `+0x189` `RtlImageNtHeaderEx`, `+0xcf7/+0xd2b` `RtlAddressInSectionTable`, `+0xab5` `RtlpxLookupFunctionTable` |
| 4 | `+0x2af ~ +0x430` | **thunk 고리.** `[OFT 칸]` 읽음 → 0 이면 서술자 끝. 서수(최상위 비트)·이름 갈림, hint 먼저 → 이분 탐색, 주소 계산, forwarder 판정, **`+0x3e7` IAT 쓰기**, 커서 +8, `ctx+0x84++`, `+0x2a7` 로 | `+0xd5d` `LdrpCheckRedirection` (조건부) |
| 5 | `+0x487 ~ +0x8a0`, `+0x93d~+0x95b`, `+0xb26~+0xb54`, `+0xc03~+0xc3c` 외 | **forwarder 처리.** `strrchr` 로 자르기, `#서수` 이면 `RtlCharToInteger`, `NTDLL` 이면 지름길, 아니면 UTF-16 버퍼에 담아 `LdrpLoadDependentModuleInternal`, 대상의 export 디렉터리 → 이름 이분 탐색 또는 서수, 대상 주소가 또 forwarder 이면 최대 32번 되돌아감 | `+0x4e5` `strrchr`, `+0x5a5` `LdrpAppendAnsiStringToFilenameBuffer`, `+0x5da` `LdrpLoadDependentModuleInternal`, `+0x94b` `RtlCharToInteger`, `+0x6b1` `RtlImageNtHeaderEx`, `+0x82a` `RtlpxLookupFunctionTable`, `+0x855` `RtlFreeHeap` |
| 6 | `+0x8a2 ~ +0xa4b` | 위 2·3·5 구간의 **PE32(`0x10b`) 갈래** (32비트 이미지 대비 복사본). 64비트 DLL 끼리는 안 탈 것 `[추측]` | `RtlAddressInSectionTable` |
| 7 | `+0xb59 ~ +0xbf0` | **끝맺음.** 서술자 다 돌았으면 `LdrpDoPostSnapWork` → 로그 → **`State = 5`** → `ctx+0xc0` 뷰 닫기 → 음수면 `LdrpLogError` → 쿠키 → `RET` | `+0xb61` `LdrpDoPostSnapWork`, `+0xb7b` `LdrpLogDllState(…,0x14a7)`, `+0xbbc` `LdrpLogError`, `+0xef5` `NtUnmapViewOfSection` |
| 8 | `+0xbf2 ~ +0xfe3` 중 나머지 | **실패·희귀 갈래.** export 없음 로그(`+0xf6d`), 못 찾음 처리 `+0xe0b~+0xe99` (`CompatCachepLookupCdb` ×2, `LdrpLogLoadFailureEtwEvent` ×2, `LdrpReportError`), 리다이렉트된 forwarder (`+0xebe`), 예외 착지 `+0xe9e` | `+0xe29/+0xe44` `CompatCachepLookupCdb`, `+0xe8f` `LdrpReportError`, `+0xf48/+0xf63` `LdrpLogLoadFailureEtwEvent` |

예외 필터: `LdrpSnapModule$filt$0` (`seh:+0x10` 에서 `LdrpGenericExceptionFilter`). 착지점 `+0xe9e~+0xeb6` ("읽지 않은 틈", 25바이트)는 원본 바이트를 읽었다: `R14D=0; EBX=1; R12D=[RSP+0x40]; R15=[RSP+0x128](=ctx); JMP +0xb93` → 예외가 나면 **끝맺음의 꼬리(`+0xb93`)로 가서** 지역 변수 `[RSP+0x40]` 의 상태로 끝난다 (그 변수는 thunk 마다 `0xc000007a` 로 시작해서 찾으면 0 이 된다 → 어느 시점 예외냐에 따라 다름 `[추측]`). 어느 구간이 `__try` 인지는 안 봤다 (`.pdata` 안 읽음).

## 질문별 답 [정적]

### 1. IAT 칸에 import 함수 주소를 적는 줄

- **답: `LdrpSnapModule +0x3e7` (`18003aca7`, `MOV qword ptr [RCX],RSI`) 한 줄뿐이다.** `[정적]`
  - `RCX` = `[RSP+0xa0]` = IAT 쓰기 커서 (`+0x3df` 에서 꺼냄). 서술자의 `FirstThunk` RVA + importer `DllBase` 로 시작 (`+0x129`~`+0x130`), `+ctx+0x84 * 8` 만큼 앞서 시작 (`+0x291~+0x29f`, 이어서 하기), 한 칸 쓸 때마다 `+0x3f6 ADD RCX,8`.
  - `RSI` = 쓸 값. 보통 `자식 DllBase + 함수 RVA` (`+0x3ac LEA RSI,[RBX+RCX]`). forwarder 를 풀었으면 그 최종 주소 (`+0x885 MOV RSI,RBX`), 리다이렉트됐으면 `LdrpCheckRedirection` 이 돌려준 주소 (`+0xd62 MOV RSI,RAX`).
  - 이 줄 앞의 조건은 `+0x3cc TEST R12D; JS` (찾기 실패면 이 줄을 건너뛰고 오류 처리). 쓰고 나면 `+0x402 INC [ctx+0x84]`, `+0x430 JMP +0x2a7`.
  - `.asm` 의 비스택 store 전수 조사: 이 줄 말고 IAT 로 갈 수 있는 쓰기 없음 (표는 위 "ctx 칸").
- **이름 import / 서수 import 별 (같은 `+0x3e7` 로 모이고, 찾는 줄만 다르다):**

  | 갈래 | 판정 | 찾는 줄 |
  |---|---|---|
  | 서수 | thunk 최상위 비트 (`+0x2ca SHR RDI,0x3f`, `+0x2e5 TEST DIL; JNZ +0x960`) | `+0x960~+0x96e`: 서수 = 하위 16비트, 색인 = 서수 − export `Base`(`+0x10`). `+0x396 CMP 색인, NumberOfFunctions(+0x14)`, 넘으면 못 찾음. `+0x39c LFENCE` 뒤 `+0x3a1` 에서 `AddressOfFunctions[색인]` |
  | 이름 | 비트 0 | `+0x2ee~+0x300` thunk = IMAGE_IMPORT_BY_NAME RVA → 힌트(2바이트) + 이름. `+0x316` 힌트 읽음 → **hint 먼저**, 아니면 이분 탐색 (1a). 이름이 서수가 되면 `+0x371` `AddressOfNameOrdinals`, `+0x3a1` `AddressOfFunctions[서수]` |

- **1a. 이름 → 주소는 어떤 함수로 하나? 답: 본문 안에서 직접 한다 (예). 간접 호출 아님.** `[정적]`
  - `_calls.tsv`/`.asm` 에 `indirect:` 호출이 **하나도 없다.** `LdrpGetProcedureAddress`·`LdrpNameToOrdinal` 은 **인라인**으로 들어와 있다: 로그 인자에 소스 파일 `minkernel\ldr\ldrsnap.c` 와 함수 이름이 박혀 있다 — `+0xb1c`·`+0xccb` `LdrpLogInternal(…, 0x8ee, "LdrpNameToOrdinal", …)` (이름 못 찾음 로그, 정상 길·forwarder 길 둘 다), `+0x756` (`0x346`) 와 `+0xb42` (`0x358`) `LdrpLogInternal(…, "LdrpGetProcedureAddress", …)` (forwarder 대상을 이름·서수로 찾기 시작 로그).
  - 정상 길의 순서 (`+0x316~+0x45c`):
    1. `+0x316` `R8D = 힌트`. `+0x321 CMP 힌트, NumberOfNames(ECX)`; **힌트 ≥ 이름 수면 가운데부터** (`+0x44c`: `(n-1)/2`).
    2. 힌트가 범위 안이면 `AddressOfNames[힌트]` 의 문자열과 **한 바이트씩 비교** (`+0x34d~+0x364`, 인라인 `strcmp`). 같으면 `+0x371` 로 서수 읽기.
    3. 다르면 비교 결과 부호로 범위를 줄인다: 양수 `+0x45c LEA R11D,[R8+1]` (low = mid+1), 음수 `+0x437 LEA R10D,[R8-1]` (high = mid−1), 새 mid = `(low+high)/2` (`+0x43b~+0x444`) → `+0x332 CMP R10D,R11D; JL` (low > high 면 못 찾음 → `+0xaed` 로그 → 상태 `0xc000007a`) → 다시 비교. 즉 **힌트 자리가 이분 탐색의 첫 한 걸음**이다.
  - 이름이 export 이름 표에 **정렬돼 있다고 믿고** 이분 탐색한다 (PE 규격: export 이름은 정렬).
- **1b. 무엇을 기준으로 도나? 답: 모듈의 모든 서술자를 돈다 (한 서술자만 아님).** `[정적]`
  - 바깥 고리: `ctx+0x80` (서술자 번호) < `ctx+0x68` (개수) 동안 (`+0xb7~+0xc2`). 서술자 = `ctx+0x88` 배열의 20바이트 항목 (`+0x113`). 자식 항목이 비어 있으면 (`+0x149 TEST RSI; JZ +0x471`) 그 서술자는 건너뜀 (sast/17: IAT 첫 칸이 0 인 서술자는 자식 칸이 0).
  - 안쪽 고리: OFT(`+0`) 가 가리키는 8바이트 칸을 하나씩, 값이 0 인 칸에서 끝 (`+0x2b3 TEST RAX; JZ +0x462`). OFT RVA 가 0 이거나 `SizeOfImage` 를 넘으면 FirstThunk 를 대신 읽는다 (`+0x138~+0x143`, `+0xf06`).
  - **`문맥+0x70/+0x78`(IAT 범위)는 이 함수가 읽지 않는다.** IAT 위치는 서술자의 FirstThunk 로 정한다. `+0x70/+0x78` 은 `LdrpDoPostSnapWork` 의 보호 복원용 (질문 4).
  - `ctx+0x80`/`ctx+0x84` 를 **ctx 에 저장해 두므로 중간에 멈췄다가 같은 자리에서 다시 시작할 수 있다** (질문 2 의 `STATUS_PENDING`).

### 2. `+0x4e5 strrchr` ~ `+0x5da LdrpLoadDependentModuleInternal` 갈래는 forwarder 인가

- **답: 예, forwarder 처리다.** `[정적]`
  - 들어가는 조건: `+0x3c0 CMP RSI,R8; JA +0x487` → `+0x487 CMP RSI,R9; JNC` — 계산한 주소가 **자식의 export 디렉터리 범위 안**(`R8` = 디렉터리 시작, `R9` = 시작 + 데이터 디렉터리 크기 `[RSP+0x58]`)이면 그 "주소"는 사실 `"다른DLL.함수"` 문자열 (PE 규격의 forwarder). 서수 import 든 이름 import 든 같다.
  - `+0x4e5 strrchr(문자열, '.')` 로 DLL 부분과 함수 부분을 가른다. 점이 없거나 DLL 부분이 `0xffff` 를 넘으면 `STATUS_INVALID_IMAGE_FORMAT`(`0xc000007b`).
- **`+0x94b RtlCharToInteger` = `DLL.#서수` 꼴 처리인가: 예.** `+0x518 CMP byte [점+1], '#'` (`0x23`) 이면 `+0x93d` 에서 `RtlCharToInteger(점+2, 0, &[RSP+0x84])`. 음수면 `0xc000007b`. 이름 포인터(`R13`)는 0 으로 두고 (`+0x958 MOV R13,R12`), 나중에 `+0xb26` 에서 "서수로 찾기" 갈래로 간다 (서수 0 이면 `0xc000000d`).
- **forwarder 대상 DLL 을 그 자리에서 올리나: 예. 단 `NTDLL` 은 예외.**
  - **`NTDLL` 지름길 (`+0x533~+0x555`):** DLL 부분 길이 5 이고 (`CMP SI,5`), 4바이트가 `ntdl` (`OR EAX,0x20202020; CMP …,0x6c64746e`), 5번째가 `l` (`OR AL,0x20; CMP AL,0x6c`) 이면 (**대소문자 무시**) 아무것도 올리지 않고 `R15 = LdrpNtDllDataTableEntry` (`+0x649`) 를 자식으로 쓴다.
  - 그 외: `+0x56e memset`, `+0x5a5 LdrpAppendAnsiStringToFilenameBuffer(&버퍼, &ANSI 이름)` 로 DLL 이름(점 앞)을 지역 `UNICODE_STRING`(글자 칸 `[RSP+0x140]`, 0xfe 바이트 — 모자라면 callee 가 힙으로 키움) 에 담고, **`+0x5da (call) / +0x5df (ret) LdrpLoadDependentModuleInternal`** 호출. 인자 (sast/18 번호):
    1. `&[RSP+0x130]` — 자식 이름 `UNICODE_STRING`
    2. `R14` = importer 항목의 `LoadContext`(`[[RSP+0x98]+0xb0]`) = `ctx`
    3. `RDI` = **forwarder 를 가진 쪽(수출하는 모듈) 항목** (`[RSP+0x60]`)
    4. `R9D` = **1** = `LoadReasonStaticForwarderDependency`
    5. `&[RSP+0x50]` — 자식 항목을 돌려받는 칸
    6. `&[RSP+0xc8]` — 간선 칸 보관 포인터 (끝에서 `+0x855 RtlFreeHeap(LdrpHeap,0,[RSP+0xc8])`, sast/18 과 같은 용법)
  - 올린 뒤 `+0x605` 는 지역 이름 버퍼가 힙으로 커졌을 때만 해제하는 껍데기다 (`RtlDeleteBoundaryDescriptor` 이름 믿지 말 것, 아래 6).
  - 이름 안 api-set (`api-ms-…`) 풀이는 `LdrpLoadDependentModuleInternal` 안 몫 (sast/18 질문 6).
  - 대상 항목의 export 를 읽는 줄은 `+0x674~+0x7d9` (같은 방식 반복: NT 헤더·디렉터리·`LdrpGetProcedureAddress` 로그·이름 이분 탐색(**힌트 없음**)·서수 → 주소). 서수 forwarder 는 `+0xb26~+0xb54` 에서 `서수 − Base` 로 색인.
  - 대상 주소가 **또 forwarder 이면** (`+0xc03~+0xc39`) 새 문자열로 `+0x4dd` 부터 다시 한다. 되돌아간 횟수 `[RSP+0x88]` 가 `0x20`(32)이 되면 `0xc000007b` (`+0xc22~+0xc25`, `+0xfd5`).
- **forwarder 대상의 의존 간선(부모 카운터)과 엮이나: 예.** `[정적]`
  - 간선을 쓰는 줄은 `LdrpLoadDependentModuleInternal +0xf69 / +0xf8b` 안이고, **부모 노드는 인자 2 의 항목**(sast/18: `[[arg2+0x38]+0x98]`), 즉 **import 하는 쪽(importer)** 이다. forwarder 를 가진 쪽(인자 3)은 활성화 컨텍스트·새 문맥의 `+0x30` 에만 쓰인다. → forwarder 대상은 **importer 의 자식 간선**으로 걸린다. 이유 코드만 `1`(StaticForwarder) 로 다르다.
  - `LdrpSnapModule` 이 불릴 때 `ctx+0x6c` 는 이미 0 이다 (sast/17: `MapAndSnap +0x4c2` 는 남은 자식 0 일 때만, `LdrpSignalModuleMapped +0x40~+0x59` 가 0 으로 만들고 일감을 넣음). sast/18 질문 5 의 **`ECX == 0` 갈래**가 여기서 일한다: 자식 `State < 2` 면 callee 가 `ctx+0x50 = 자식`, `ctx+0x6c = 1`, importer 노드 `State = 3`(`WaitingForDependencies`) 로 쓰고 **`0x103`(`STATUS_PENDING`)** 을 돌려준다. (sast/18 의 "`LdrpSnapModule +0x5df` 몫으로 보인다" `[추측]` → **맞다**.)
  - snap 은 `0x103` 을 받으면 (`+0x634~+0x641` 에서 `0x103` 은 정리 쪽으로, `+0x85f CMP R12D,0x103; JZ +0xeb7`) **`LdrpDoPostSnapWork` 도 `State = 5` 도 건너뛰고 반환값 0(성공)으로 나간다.** `ctx+0x80`·`ctx+0x84` 가 그대로라서 (펜딩이던 칸은 `+0x402 INC` 에 도달 못 했다) 나중에 다시 불리면 **같은 thunk 부터 다시** 한다. 다시 불리는 길: 자식이 `Mapped` 가 되면 `LdrpSignalModuleMapped` 가 importer 의 `ctx+0x6c` 를 줄여 0 이면 `State = 4` + `LdrpQueueWork` → `LdrpProcessWork +0x131` (sast/19). 이 연결은 줄로 확인한 것이고 실제 순서는 동적.
  - 자식이 이미 `State ≥ 2` 면 callee 는 아무것도 안 하고 0 을 돌려주니 펜딩 없이 그냥 이어간다.
  - 자식 올리기가 실패하면 (`+0x5ea JS +0x6b7`) `+0xe03 *(importer.LoadContext+0x28) = 상태` 를 쓰고 바로 오류 처리로 간다. 이때 최종 반환은 못 찾음 처리에 따라 `0xc0000139` 로 바뀔 수 있다 (질문 6).
- **flow.md 2.5 의 `[추측]` 정정 거리:** "`+0x5df` 갈래 = 지연 import 또는 forwarder" → **forwarder** 가 맞다. 이 함수는 지연 import 디렉터리(13번)를 읽지 않는다 (읽는 NT 헤더 칸이 `+0x14`, `+0x18`, `+0x54`, `+0x6`, `+0x74/+0x78/+0x7c`(PE32), `+0x84/+0x88/+0x8c`(PE32+) 뿐).

### 3. 노드 `State = 5` 를 쓰는 곳

- **답: `LdrpSnapModule +0xb87` (`18003b447`, `MOV dword ptr [RAX+0x38],0x5`) 한 곳.** `RAX = [RDI+0x98]` (importer 항목의 `DdagNode`), `+0x38` = `State`. `[정적]`
- **앞뒤 (서술자를 다 돈 뒤 `ctx+0x80 ≥ ctx+0x68` 에서):**
  1. `+0xb59 TEST R12D; JS +0xb8e` — 이전 상태가 음수면 PostSnap 도 State 도 건너뜀
  2. **`+0xb61 (call) / +0xb66 (ret) LdrpDoPostSnapWork(ctx)`**
  3. `+0xb69 TEST EAX; JS +0xb8e` — 음수면 State 안 씀
  4. `+0xb7b (call) / +0xb80 LdrpLogDllState(DllBase, &FullDllName, 0x14a7)` (끝 로그)
  5. **`+0xb87 State = 5`**
  6. `+0xb8e` 꼬리 (`ctx+0xc0` 뷰 닫기, 음수면 로그)
  → **`LdrpDoPostSnapWork` 뒤**이고, 끝 로그 뒤다.
- **실패 때 State: 이 함수는 쓰지 않는다** (snap 시작 때 4 그대로). 실패 상태는 반환값으로만 나가고 (`+0xbc1 MOV EAX,R12D`), 음수면 `+0xbbc LdrpLogError(상태, 0x19, 0, ctx)` 를 부른다 (asm 의 `EBX` 깃발이 0 일 때 `[추측: 조건 줄 EBX]`; `.c` 는 음수면 무조건으로 읽힘). 호출자가 `*(ctx+0x28)` 에 쓴다 (`MapAndSnap +0x4c2` 뒤, `ProcessWork +0x133~+0x13b`).
- 음수 State: `.asm` 모음에서 State 에 `-3`(`LdrModulesSnapError`) 를 쓰는 줄은 `LdrpCondenseGraphRecurse +0x14e` (자식이 −3 이면 부모에게 전파) 하나뿐이다. **처음 −3 을 쓰는 곳은 이번 범위에서 못 찾았다 `[공백]`.**
- 참고: `State = 5` 쓰는 곳은 `LdrpMapAndSnapDependency +0x5ae` (IAT 없는 모듈) 도 있다 (sast/17). snap 이 도는 모듈은 IAT 가 있는 모듈뿐이다.

### 4. `LdrpDoPostSnapWork` (rva `0xa4200`, 228B) 가 하는 일

근거: `ms/disassemble/ntdll/LdrpDoPostSnapWork.asm`, `ms/decomp/ntdll/LdrpDoPostSnapWork.c`. 인자 `RCX` = `ctx`, 반환 = 마지막 상태 (`0` 성공).

| 범위 | 하는 일 | 호출 |
|---|---|---|
| `+0x0 ~ +0x1c` | `RSI = ctx+0x38` (importer 항목), `RDI = ctx` | — |
| `+0x20 ~ +0x47` | `ctx+0x70` (IAT 시작) ≠ 0 이면 **`NtProtectVirtualMemory(-1, &ctx+0x70, &ctx+0x78, 새 보호 = [ctx+0x90], &임시)`**. 음수면 그 상태로 끝 (TLS·CFG 안 함) | `+0x3e (call) / +0x43 (ret)` |
| `+0x49 ~ +0x66` | `ctx+0xa0` 이 0 이 아니면 `*(ctx+0xa0)` 가 `ctx+0x98` 과 같은지 비교, **다르면 `INT 0x29` (ECX=`0x13`, 빠른 실패)** | — |
| `+0x84 ~ +0x99` | 항목 `TlsIndex(+0x6e) == 0` 이면 `LdrpHandleTlsData(항목)`; 음수면 그 상태로 끝. `TlsIndex ≠ 0` 이면 건너뜀 | `+0x8e (call) / +0x93 (ret)` |
| `+0x68 ~ +0x71`, `+0x9b ~ +0xe3` | `LdrControlFlowGuardEnforcedWithExportSuppression()` 이 0 이 아니면 `LdrpUnsuppressAddressTakenIat(DllBase, 0, 0)`. 음수면 로그 `LdrpDoPostSnapWork:Unable to unsuppress the export suppressed functions that are imported in the DLL …` (`ldrsnap.c` 줄 `0x24e`) | `+0x68`, `+0xa4`, `+0xde` |

- **`+0x3e NtProtectVirtualMemory` = IAT 보호를 원래대로 되돌림인가: 예.** `[정적]` `LdrpPrepareImportAddressTableForSnap` (`.c`) 이 `NtProtectVirtualMemory(-1, &지역복사, &지역크기, 4 (PAGE_READWRITE), ctx+0x90)` 로 쓰기 가능으로 바꾸고 원래 보호값을 `ctx+0x90` 에 적는다. 여기서는 `ctx+0x70`·`ctx+0x78` 을 직접 넘기고 새 보호값 = `[ctx+0x90]` 이므로 짝이다. (Prepare 는 쓰기 가능으로 바꾼 뒤 4KB 마다 `*p = *p` 로 한 번씩 건드린다 — 이번 범위 아님.) IAT 가 없는 모듈(`ctx+0x70 == 0`)은 건너뜀.
- **CFG 쪽 한 줄:** 프로세스가 "CFG 켜짐 + export suppression" 일 때만 이 DLL 이 import 하는 함수 중 export 억제된 것의 억제를 푼다 (`LdrpUnsuppressAddressTakenIat`, 이름·로그 문구에서 읽은 것 — 본문은 안 봄 `[추측]`). 켜짐 여부는 정적으로 못 정함 (프로세스 설정, 동적).
- **IAT 보호 복원 뒤 CFG 포인터 칸 검사 (`+0x49~+0x66`)**: Prepare 가 IAT 가 쓰기 가능한 동안 변조될 수 있는 모듈 LoadConfig 의 `GuardCFCheckFunctionPointer`(`LoadConfig+0x70`) 칸 값을 `ctx+0x98` 에 기억해 두고(`ctx+0xa0` = 칸 주소), 여기서 같은지 본다 (Prepare `.c` 에서 읽음, 모듈이 CFG 계측(`GuardFlags & 0x100`)일 때만 채워짐). `0x13` 이 SDK 의 어느 이름인지는 기억(`FAST_FAIL_MRDATA_MODIFIED`) `[추측]`.
- **`+0x8e LdrpHandleTlsData` — DLL 의 TLS 를 여기서 처리하나: 예 (조건은 `TlsIndex == 0` 하나).**
  - 이 함수 안에는 exe/DLL 구분이 **없다.** 조건은 항목 `TlsIndex(+0x6e) == 0` 뿐이다. `TlsIndex` 는 TLS 목록 항목이 만들어지면 `0xffff` 로 쓰인다 (sast/23: `LdrpHandleTlsData +0x3a8`, `LdrpInitializeTls +0x102`). 즉 "아직 TLS 항목을 안 만든 모듈"만 들어간다.
  - exe: TLS 항목은 `LdrpInitializeProcess +0x18cb LdrpInitializeTls` 가 만들고, 그 호출은 exe 의 `MapAndSnap`(`+0x1aa4`)보다 **앞 오프셋**이다. 그래서 exe 가 `PostSnapWork` 에 올 땐 `TlsIndex ≠ 0` 라 건너뛸 것 `[추측: 오프셋 순서, 실행 순서는 안 따라가 봄]`.
  - DLL (dll1·dll2, TLS 디렉터리 있음): `TlsIndex == 0` 이므로 여기서 `LdrpHandleTlsData` 로 들어간다 `[정적: 조건 줄]`. (TLS 디렉터리 없는 DLL 은 callee 안에서 아무것도 안 만들 것 — sast/23 `+0x88` 건너뜀.)

### 5. 부모와의 관계 — snap 이 끝난 뒤 부모 노드 카운터·State 를 건드리나

- **답: 아니오.** `[정적]`
  - `LdrpSnapModule` 의 비스택 store 7줄 (위 "ctx 칸") 에 부모 노드(`ctx+0x30` 의 `DdagNode`)·`ctx+0x6c` 를 쓰는 줄이 없다. `ctx+0x30` 은 읽지도 않는다 (`R15+0x30` 읽기는 `+0x674`, `+0x7ed` 두 곳인데 둘 다 `R15` 가 **자식 항목**일 때 `DllBase`).
  - State 쓰는 줄은 자기 노드 `+0xb87` 하나뿐 (질문 3). `LdrpDoPostSnapWork` 도 노드·카운터를 안 쓴다 (본문 위).
  - 영향이 가는 유일한 곳: forwarder 의 `LdrpLoadDependentModuleInternal` 호출 안에서 `ctx+0x6c`·`ctx+0x50`·importer `State=3` 이 써진다 (질문 2). 이건 snap **중간**의 일이고 snap **끝**의 일이 아니다.
- **snap 끝은 caller 가 마무리하나: caller 가 하는 일은 실패 상태 옮기기뿐이다.** `MapAndSnap +0x4c2` 뒤: 결과가 음수면 `*(ctx+0x28) = 상태`, 아니면 쿠키 검사 후 `RET` (sast/17). `LdrpProcessWork +0x131` 뒤: 음수면 `*(ctx+0x28) = ESI` (sast/19). 부모 카운터 줄이기는 snap 이 아니라 **자식이 `Mapped` 가 되는 순간**(`LdrpSignalModuleMapped`) 이다 → 부모는 자식이 `Snapped` 가 되기를 기다리지 않는다 `[정적: 줄 관계]`.

### 6. 작은 것

- **`+0x81 LdrpHandlePendingModuleReplaced(ctx)`**: 입구에서 **무조건** 부름 (조건은 callee 안, 안 읽음).
  **`+0xfc LdrpFreeReplacedModule`**: 자식 항목의 `LoadContext(+0xb0) ≠ 0` 이고 그 문맥 `+0x20` 비트 `0x80000` 이 꺼져 있고 문맥의 `+0x38` 항목이 자식 항목과 **다를 때** (= 올리는 사이 같은 DLL 이 다른 항목으로 교체됨, `LdrpLoadContextReplaceModule` 쪽, sast/17), `ctx+0x58[번호]` 를 바꿔 치고 (`+0xa72`·`+0x105`) 옛 항목을 정리한다. 정상은 `ctx+0x38 == 항목` 이라 안 탄다. test.exe 에서 평소 안 탈 것 `[추측]` (같은 DLL 이 병렬로 두 번 올라올 때만).
- **`+0xd5d LdrpCheckRedirection(importer 항목, 수출 항목, 이름)`** (이름 import 만, `ctx+0x20` 비트 `0x2000000`): 이 비트는 `MapAndSnap +0xd6` 이 `LdrpShouldModuleImportBeRedirected` 가 참일 때 켠다 (그 함수는 `LdrpRedirectionModule == 0` 이면 거짓, sast/17). 돌려준 주소가 `0xffffffffffbadd11` 이면 "리다이렉트 없음" → 평소 길, 아니면 그 주소를 IAT 에 쓴다 (`+0xd8f` 로그 `0xd98`, `+0xdba JMP +0x3df`). **`+0xecd`**: forwarder 를 푼 뒤 같은 검사 (importer `LoadContext+0x20` 같은 비트 + 이름 포인터 ≠ 0). **test.exe 에서는 안 탈 것** `[추측]` (`LdrpRedirectionModule` 값은 동적, sast/17 `dq ntdll+0x1d2600 L1`).
- **`+0xe29` / `+0xe44 CompatCachepLookupCdb(FullDllName 버퍼, 0x80)`**: **"못 찾음" 오류 처리에서만** (`+0xe0b`: 상태가 `0xc000007a` 이거나 `0xc0000135`). 앞이 importer, 뒤가 자식(수출 쪽) 의 경로 문자열. 둘 중 하나라도 0 이 아니면 `+0xf48`, `+0xf63` `LdrpLogLoadFailureEtwEvent` ×2 (ETW "LoadFailure", "LoadFailureOperational" 이름 상수) 를 부르고 `+0xe51` 로 이어진다. 어느 쪽이든 **상태 값은 안 바뀌고** 곧바로 `LdrpReportError` 로 간다 (로그만 다름). 정상 snap 에선 안 탄다 (`[정적]`: 못 찾음 처리 안).
- **import 를 못 찾을 때 실패 코드: 맞다.**
  - 이름 import: **`0xc0000139` (`STATUS_ENTRYPOINT_NOT_FOUND`)** (`+0xe5e`), 서수 import: **`0xc0000138` (`STATUS_ORDINAL_NOT_FOUND`)** (`+0xe76`, 판정은 `+0xe59 TEST DIL`). 둘 다 `+0xe8f LdrpReportError(&importer.FullDllName, 이름 포인터 또는 서수, 상태)` 를 거쳐 `EBX=1` 로 꼬리로 가고, 그 상태가 반환값이 된다.
  - 거기까지 가기 전의 "찾는 중" 상태 값은 `0xc000007a` (`STATUS_PROCEDURE_NOT_FOUND`) 로 thunk 마다 시작한다 (`+0x2bc`).
  - forwarder 대상 **DLL 을 못 올림**(`0xc0000135` 등)도 `+0xe0b` 에서 같은 `0xc0000139` 로 바뀌어 반환될 수 있다 (`LoadContext+0x28` 쪽에는 원래 상태가 남음, `+0xe03`).
  - 다른 실패 값: export 디렉터리 없음·읽기 실패 `0xc000007b` (로그 "does not contain an export table", `+0xf6d~+0xfa8`), forwarder 문자열 이상·너무 깊음 `0xc000007b`, 헤더 이상 `0xc000000d`, 서수 forwarder 의 서수가 0 `0xc000000d`.
- **`+0x605 RtlDeleteBoundaryDescriptor` (별칭 주의):** `pdb_aliases.py --lookup ntdll.dll 0x3d630` → 같은 주소 **9개 이름** (`RtlpFtFree`, `RtlpCtFreeMemory`, `RtlDeleteBoundaryDescriptor`, `RtlpFreeAtom`, `IdnaMemFree`, `RtlpFreeEnvironmentHashTable`, `RtlpFreeEnvBlock`, `NtdllpFreeStringRoutine`, `RtlpSysVolFree`). 원본 바이트(`llvm-objdump-14`): `PEB(gs:[0x60])` 에서 `[+0x30]`(= `ProcessHeap`) 을 꺼내 `RtlFreeHeap(ProcessHeap, 0, 포인터)` 로 `JMP` 하는 **7줄짜리 껍데기**. → 여기서는 "지역 이름 버퍼가 힙으로 커졌을 때 그 버퍼를 풀기" 한 줄 (`+0x600 CMP 포인터, 인라인 버퍼 [RSP+0x140]; JZ 건너뜀`). 문서엔 `RtlDeleteBoundaryDescriptor` 라고 쓰지 말고 "프로세스 힙 해제 껍데기 (`ntdll+0x3d630`)" 로.

## 큰 흐름 (정적, 순서는 코드 순서)

```
LdrpSnapModule(ctx)                                  ctx+0x38 = importer 항목
├─ (+0x79) 시작 로그 / (+0x81) 교체 대기 정리
├─ 서술자 고리  (ctx+0x80 < ctx+0x68)
│   ├─ 자식 항목 = ctx+0x58[번호]      (없으면 건너뜀)
│   ├─ 자식의 export 디렉터리 찾기      (+0x189, 8칸 캐시, CFG 켜짐이면 함수 표 확인)
│   └─ thunk 고리 (OFT 칸이 0 일 때까지)
│        ├─ 서수 → 색인 / 이름 → hint 먼저 + 이분 탐색 (인라인)
│        ├─ 주소 = 자식 DllBase + 함수 RVA
│        ├─ 주소가 export 디렉터리 안 = forwarder → 대상 DLL 올리기(NTDLL 지름길) → 다시 찾기 (최대 32번)
│        │      └─ 자식 State<2 면 STATUS_PENDING → snap 은 0 으로 일단 끝 (ctx+0x80/+0x84 보존)
│        ├─ 실패 → 0xc0000139 / 0xc0000138 (+ 로그·ETW)
│        └─ ★ +0x3e7  IAT 칸 = 주소      → ctx+0x84++
├─ (+0xb61) LdrpDoPostSnapWork(ctx)  : IAT 보호 복원 → CFG 칸 검사 → TLS(TlsIndex==0) → CFG unsuppress
├─ (+0xb7b) 끝 로그
├─ ★ +0xb87  노드 State = 5 (Snapped)
└─ (+0xef5) ctx+0xc0 뷰 닫기 / 음수면 (+0xbbc) LdrpLogError
```

## test.exe 에서는 (샘플 PE 계산, `[정적+샘플]`)

- `test.exe` : 서술자 2개 — `dll1.dll`(1개: `Dll1Hello`, 힌트 0), `KERNEL32.dll`(90개). IAT 쓰기 **91번**.
- `dll1.dll` : 서술자 2개 — `dll2.dll`(1개: `Dll2Hello`, 힌트 0), `KERNEL32.dll`(88개). **89번**. `dll2.dll`: `KERNEL32.dll` 88개. **88번**.
- **힌트**: `dll1!Dll1Hello`, `dll2!Dll2Hello` 는 힌트 0 이 export 이름 표 0번과 맞는다 (힌트로 바로 찾음). `KERNEL32` 쪽은 test.exe 90개·dll1 88개·dll2 88개 **전부 힌트가 틀린다** (이 빌드 `kernel32.dll` 의 이름 표 색인과 **전부 정확히 5 만큼** 어긋남 — 샘플을 만든 PC 의 import 라이브러리 기준 힌트라서). 힌트 자리의 이름은 import 이름보다 앞(작은) 이름이라, 첫 비교 결과가 "양수" → 범위가 `힌트+1 ~ 끝` 으로 줄어든다 → 전부 힌트 자리 비교 한 번 + 이분 탐색 (이름 1697개, 대략 11걸음). 힌트가 틀려도 동작에는 문제 없다 (코드가 이분 탐색으로 간다).
- **forwarder**: 세 모듈 모두 `KERNEL32` import 중 **10개가 `NTDLL.Rtl…` 로 가는 forwarder** (`InitializeSListHead`→`RtlInitializeSListHead`, `InterlockedPushEntrySList`·`InterlockedFlushSList`, `Enter/Leave/DeleteCriticalSection`, `EncodePointer`, `HeapAlloc`→`RtlAllocateHeap`, `HeapSize`, `HeapReAlloc`). 이들은 **`NTDLL` 지름길 (`+0x649`)** 을 타고, `+0x5da LdrpLoadDependentModuleInternal` 은 **안 탈 것이다** `[추측: 이 샘플의 import 만 계산]`. (대조: kernel32 자신(서술자 104개·import 1274곳)·kernelbase(4개·750곳)의 import 도 api-set 호스트를 KernelBase 로 가정해 셌더니 forwarder 대상은 NTDLL 뿐 — 근사.)
- 그래서 test.exe·dll1·dll2 의 snap 에서 `strrchr`(`+0x4e5`) 가 서는 횟수 = 위 forwarder import 수 (모듈당 10, 합 30; kernel32·kernelbase 의 snap 몫은 따로). `RtlCharToInteger`(`+0x94b`)·`STATUS_PENDING` 갈래는 안 탈 것 `[추측]`.

## 대표 지점 (그림 2 `LdrpSnapModule` 상자 아래용)

| # | 지점 | 오프셋 / `ntdll+rva` | 뜻 |
|---|---|---|---|
| 1 | **IAT 칸에 주소 쓰기** | `LdrpSnapModule +0x3e7` / `ntdll+0x3aca7` (`MOV [RCX],RSI`) | `RCX` = IAT 칸 주소, `RSI` = 쓸 주소 (`ln @rsi` 로 함수 이름). 이름·서수·forwarder 가 다 여기로 모인다 |
| 2 | **`LdrpDoPostSnapWork` 호출** | call `+0xb61` / `ntdll+0x3b421`, ret `+0xb66` / `ntdll+0x3b426` | 서술자를 다 돈 뒤. IAT 보호 복원·CFG 검사·TLS(TlsIndex==0)·CFG unsuppress |
| 3 | **노드 `State = 5`** | `LdrpSnapModule +0xb87` / `ntdll+0x3b447` | `PostSnapWork` 성공 + 끝 로그 뒤. `[RAX+0x38] = 5`, `RAX = DdagNode` |

## 동적으로 확인해야 할 것

(정적으로 못 정한 것: 실행 때 어느 갈래를 타는지, 런타임 값. 모두 test.exe 만 서게 조건이 필요하다 — kernel32·kernelbase·dll1·dll2 가 같은 함수를 지난다. 조건식은 dast-mentor 가.)

1. **IAT 쓰기 값**: `ntdll+0x3aca7` 에서 `rcx` (칸 주소), `rsi` (쓸 주소), `ln @rsi`. dll1 (또는 test.exe) 의 snap 중에 `Dll1Hello` 칸이 `dll1` 안 주소이고, forwarder 10개가 `ntdll!Rtl…` 인지. 한 모듈에서 수십~천 번 서니 개수 조건이 필요하다 (test.exe 91, dll1 89, dll2 88, kernel32 1274, kernelbase 750).
2. **snap 이 어느 스레드에서 어떤 순서로 도나** (Q23 dll 명부 순서 · Q47 일꾼 스레드와 **같은 실행에서 볼 수 있다**, 멈춤점만 더하면): `ntdll+0x3a8c0` (입구) 에서 `rcx` = ctx, 모듈 이름 = `poi(poi(@rcx+0x38)+0x50)` (FullDllName 버퍼), `@$tid`. Q47 의 `ntdll+0x2155d` (`MapAndSnap` 이 직접 부르는 자리) 와 `ntdll+0x21592` 와 이어서 보면, 어느 모듈 snap 이 main 스레드에서(부모 없음) / 일꾼에서 도는지 한 줄씩 나온다.
3. **forwarder 갈래**: `ntdll+0x3af09` (`NTDLL` 지름길, `MOV R15,[LdrpNtDllDataTableEntry]`) 횟수 — test.exe·dll1·dll2 에서 각 10번 기대. `ntdll+0x3ae9a` (`LdrpLoadDependentModuleInternal` call) 와 `ntdll+0x3b11f` (`CMP R12D,0x103`) 는 **0번 기대** (설 때만 이상 있음). 정적으로 단정 못 한 것: 실제 kernel32 export 가 이 빌드에서 정말 `NTDLL.` 로만 가는지 (PE 계산으로는 그렇다).
4. **`PostSnapWork` 갈래**: `ntdll+0xa4200` 입구 (모듈 이름 위와 같게), `ntdll+0xa423e` (`NtProtectVirtualMemory` call: `rdx` = `&ctx+0x70`, `r8` = `&ctx+0x78`, `r9d` = 복원할 보호값), `ntdll+0xa428e` (`LdrpHandleTlsData` call — **어느 모듈에서 서나**: dll1·dll2 만, exe 는 안 설 것), `ntdll+0xa426d` (`LdrControlFlowGuardEnforcedWithExportSuppression` 직후 `eax`) — CFG 쪽이 이 프로세스에서 켜져 있나.
5. **`State = 5` 순서**: `ntdll+0x3b447` 에서 모듈 이름 = `poi(@rdi+0x50)` (`rdi` = importer 항목, FullDllName 버퍼) 와 `@$tid` — 어느 모듈이 언제 `Snapped` 가 되나 (`PostSnapWork` 직후). `LdrpInitializeGraphRecurse` 쪽 읽기와 이어 볼 수 있음.
6. **안 타야 정상인 곳 (0번 기대)**: `ntdll+0x3b74f` (`LdrpReportError`), `ntdll+0x3b61d` (`LdrpCheckRedirection`), `ntdll+0x3a9bc` (`LdrpFreeReplacedModule`). 서면 이상이거나 이 가정이 틀린 것. 이 가정을 값으로 확인하려면 `dq ntdll+0x1d2600 L1` (`LdrpRedirectionModule`, sast/17 에서 이미 제안), `dq ntdll+0x1ea508 L1`(`LdrSystemDllInitBlock+0xb8`, CFG 비트맵 주소, run32), `db ntdll+0x1ea4ec L1` (`+0x9c`, 비트 0).
7. **dast 요청 필요**: 위 2·4·5 (대표 지점 3개 + 입구). 기존 동적 항목 Q23·Q47 과 같은 실행에서 멈춤점만 더하면 된다.

추천 멈춤점 최소 세트 (test.exe 만 서게): `ntdll+0x3a8c0` (snap 입구, 어느 모듈·어느 스레드), `ntdll+0x3aca7` (IAT 쓰기, dll1 snap 동안만, `ln @rsi`), `ntdll+0x3b447` (`State = 5`).

## 판정

**분석성공.** 요청서 6개 질문 모두 줄 단위 근거로 답함. `LdrpDoPostSnapWork` 새로 뽑음, 버전 대조 일치. 정적으로 못 정한 것은 위 "동적" 절 (갈래 횟수·스레드·CFG 켜짐 여부)과 `State = -3` 을 처음 쓰는 곳 `[공백]` 뿐.

# 2026-10-24 run36 — 주소 잇기 `LdrpSnapModule`: IAT 칸을 쓰는 줄은 **`+0x3e7` 하나**, 이름 찾기는 본문 안(hint 먼저 → 이분 탐색), `+0x4e5 ~ +0x5da` 갈래는 **forwarder**, 끝에 **`+0xb61 LdrpDoPostSnapWork` → `+0xb87 State = 5`** (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 정적: ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` (다시 analyze 안 함, flow.md 기준과 같은 분석본).
- 버전 대조 (결과서): `LdrpMapAndSnapDependency +0x4bd/+0x4c2` → `LdrpSnapModule`, `LdrpProcessWork +0x12c/+0x131` → `LdrpSnapModule`, `LdrpInitializeProcess +0x1bb6/+0x1bbb` → `LdrpInitializeGraphRecurse` 가 기준과 같다. `LdrpSnapModule` 호출 줄 수 36 + SEH 필터 1 도 요청서와 같다.
- 칸 이름·열거 값은 결과서가 PDB 타입(`llvm-pdbutil`)으로 읽었다: `_LDR_DATA_TABLE_ENTRY` (`+0x30 DllBase`, `+0x48 FullDllName`, `+0x6e TlsIndex`, `+0x98 DdagNode`, `+0xb0 LoadContext`), `_LDR_DDAG_NODE +0x38 State`, `_LDR_DLL_LOAD_REASON` (**1 = `StaticForwarderDependency`**).
  - PE 규격 구조체(`_IMAGE_IMPORT_DESCRIPTOR`·`_IMAGE_EXPORT_DIRECTORY`·`_IMAGE_THUNK_DATA64`)는 ntdll PDB 에 없다 → PE 규격 이름으로 쓴다.
  - `_LDRP_LOAD_CONTEXT` 는 칸 정보가 없다 → 오프셋 + 역할만 (run22 와 같음).
- **샘플 PE 계산** (결과서가 스크립트로 `sample/bin/` 의 exe·dll import 표와 `ms/bin/.../kernel32.dll` export 표를 직접 셈, 스크래치, 커밋 안 함). 코드 줄이 아니라 **파일 값을 센 것**이라 이 문서에선 `[정적+샘플]` 로 따로 적는다.

| 함수 | rva | 이번에 본 것 |
|---|---|---|
| `ntdll!LdrpSnapModule` | `0x3a8c0` (4052B) | 전체 (`.asm` 908줄, 호출 36줄 + `$filt$0`) — 이미 뽑혀 있던 파일 |
| `ntdll!LdrpDoPostSnapWork` | `0xa4200` (228B) | 전체 (이번에 새로 뽑음, 호출 5줄) |
| `ntdll!LdrpPrepareImportAddressTableForSnap` | — | `.c` 에서 `ctx+0x90`·`+0x98`·`+0xa0` 을 쓰는 줄만 (짝 확인용) |
| `ntdll!LdrpLoadDependentModuleInternal` | `0x21ab0` | 본문은 run23 그대로. 이번엔 인자·`0x103` 길만 |

## 목적

- Q50 (sast/18 판단·sast/19 출처): 주소 잇기 `LdrpSnapModule` 의 대표 지점.
  1. IAT 칸에 import 함수 주소를 적는 줄 (이름·서수별), 이름 → 주소 찾기를 무엇으로 하나, 서술자를 어디까지 도나.
  2. `+0x4e5 strrchr ~ +0x5da LdrpLoadDependentModuleInternal` 갈래가 forwarder 인가.
  3. 노드 `State = 5` 를 쓰는 곳.
  4. `LdrpDoPostSnapWork` 가 하는 일.
  5. snap 이 부모 노드·부모 카운터를 건드리나.
  6. 작은 갈래와 실패 코드.

## 방법

- 정적: `_workspace/sast/31_snap-module.md` — ntdll `^LdrpSnapModule$` (요청 depth 1, 실제는 있는 `.asm`·`.c` 그대로 + callee 이름·인자만), `^LdrpDoPostSnapWork$` depth 0
- 결과서: `_workspace/sast/31_snap-module_result.md` (**분석성공**)
- 이번에 `.asm` 과 다시 맞춰 본 줄 (flow-writer):
  - `LdrpSnapModule` `+0x79`, `+0x81`, `+0x94`, `+0xb7`, `+0xbe`, `+0xca`, `+0xfc`, `+0x105`, `+0x113`, `+0x129`, `+0x138`, `+0x149`, `+0x189`, `+0x291`, `+0x2a7`, `+0x2b3`, `+0x2bc`, `+0x2ca`, `+0x2e5`, `+0x316`, `+0x321`, `+0x332`, `+0x371`, `+0x396`, `+0x39c`, `+0x3a1`, `+0x3ac`, `+0x3c0`, `+0x3cc`, `+0x3df`, **`+0x3e7`**, `+0x3f6`, `+0x402`, `+0x430`, `+0x437`, `+0x44c`, `+0x45c`, `+0x462`, `+0x471`, `+0x487`, `+0x4e5`, `+0x518`, `+0x533`, `+0x56e`, `+0x5a5`, `+0x5da`, `+0x5ea`, `+0x600`, `+0x605`, `+0x634`, `+0x641`, `+0x649`, `+0x756`, `+0x855`, `+0x85f`, `+0x885`, `+0x93d`, `+0x94b`, `+0x958`, `+0x960`, `+0xa72`, `+0xb1c`, `+0xb42`, `+0xb59`, **`+0xb61`**, `+0xb69`, `+0xb7b`, **`+0xb87`**, `+0xb8e`, `+0xb93`, `+0xbbc`, `+0xc22`, `+0xd5d`, `+0xe03`, `+0xe0b`, `+0xe29`, `+0xe44`, `+0xe59`, `+0xe5e`, `+0xe76`, `+0xe8f`, `+0xeb7`, `+0xecd`, `+0xef5`, `+0xefa`, `+0xfd5`
  - `LdrpDoPostSnapWork` `+0x20`, `+0x3e`, `+0x49 ~ +0x66`, `+0x68`, `+0x84 ~ +0x99`, `+0xa4`, `+0xde`, `_calls.tsv` 5줄
  - 결과서와 다른 곳: 없음. 단 예외 착지점 `+0xe9e ~ +0xeb6` 은 `.asm` 에 없는 틈이라 결과서가 원본 바이트로 읽은 것을 그대로 옮겼다.

## 결과

비유 (이사 = 모듈 올리기, 전화번호부 = export 표, 단축번호 칸 = IAT):

- 새 집(모듈)에는 "자주 거는 곳" **단축번호 칸**(IAT)이 줄지어 있다. 처음엔 칸마다 "철수네 가게의 사과 담당" 같은 **이름 쪽지**만 붙어 있다.
- snap 은 쪽지를 하나씩 떼어, 그 가게의 **전화번호부**(export 표)에서 진짜 번호를 찾아 **칸에 적는** 일이다. 적는 손은 딱 하나(`+0x3e7`)다.
- 번호부를 찾을 땐 쪽지에 적힌 "아마 몇 쪽" (hint) 을 먼저 펴 보고, 틀리면 가운데부터 반씩 접어 찾는다 (이분 탐색).
- 가끔 번호부에 번호 대신 "**옆 가게 OO 에게 물어보세요**" 라고 적혀 있다 (forwarder). 그러면 그 옆 가게를 그 자리에서 불러오고 (단, 옆 가게가 관리실 `NTDLL` 이면 이미 있으니 부르지 않는다), 거기 번호부를 다시 찾는다.
- 칸을 다 채우면 단축번호 판을 다시 **잠그고** (`LdrpDoPostSnapWork`, 쓰기 금지로), TLS 자리를 마련한 뒤, 집 문패에 **"전화 다 이음(State 5)"** 도장을 찍는다 (`+0xb87`).

### 정적 — 호출 지점 (`.asm` CALL 줄, 라벨은 ret)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpMapAndSnapDependency` | `+0x4bd` | `+0x4c2` | `ntdll!LdrpSnapModule` (부모 없음·남은 자식 0) | [정적] (run22) |
| `ntdll!LdrpProcessWork` | `+0x12c` | `+0x131` | `ntdll!LdrpSnapModule` (노드 `State ≠ 0`) | [정적] (run24) |
| `ntdll!LdrpSnapModule` | `+0x79` | `+0x7e` | `ntdll!LdrpLogDllState(…, 0x14a6)` 시작 로그 | [정적] |
| `ntdll!LdrpSnapModule` | `+0x81` | `+0x86` | `ntdll!LdrpHandlePendingModuleReplaced(ctx)` — 무조건 | [정적] |
| `ntdll!LdrpSnapModule` | `+0xfc` | `+0x101` | `ntdll!LdrpFreeReplacedModule` — 자식 항목이 교체됐을 때만 | [정적] |
| `ntdll!LdrpSnapModule` | `+0x189` | `+0x18e` | `ntdll!RtlImageNtHeaderEx` — 자식(수출하는 쪽) NT 헤더 | [정적] |
| `ntdll!LdrpSnapModule` | `+0x4e5` | `+0x4ea` | `strrchr(문자열, '.')` — forwarder 자르기 | [정적] |
| `ntdll!LdrpSnapModule` | `+0x5a5` | `+0x5aa` | `ntdll!LdrpAppendAnsiStringToFilenameBuffer` — 대상 DLL 이름 | [정적] |
| `ntdll!LdrpSnapModule` | `+0x5da` | `+0x5df` | `ntdll!LdrpLoadDependentModuleInternal(…, 이유 1)` — forwarder 대상 올리기 (`NTDLL` 아닐 때) | [정적] |
| `ntdll!LdrpSnapModule` | `+0x605` | `+0x60a` | 프로세스 힙 해제 껍데기 (`ntdll+0x3d630`) — 이름 버퍼가 힙으로 커졌을 때만 | [정적] |
| `ntdll!LdrpSnapModule` | `+0x855` | `+0x85a` | `ntdll!RtlFreeHeap(LdrpHeap, 0, 간선 칸)` | [정적] |
| `ntdll!LdrpSnapModule` | `+0x94b` | `+0x950` | `ntdll!RtlCharToInteger` — `DLL.#서수` 꼴 | [정적] |
| `ntdll!LdrpSnapModule` | **`+0xb61`** | **`+0xb66`** | **`ntdll!LdrpDoPostSnapWork(ctx)`** — 서술자를 다 돈 뒤 | [정적] |
| `ntdll!LdrpSnapModule` | `+0xb7b` | `+0xb80` | `ntdll!LdrpLogDllState(…, 0x14a7)` 끝 로그 | [정적] |
| `ntdll!LdrpSnapModule` | `+0xbbc` | `+0xbc1` | `ntdll!LdrpLogError` — 상태 음수일 때 | [정적] |
| `ntdll!LdrpSnapModule` | `+0xd5d` / `+0xecd` | `+0xd62` / `+0xed2` | `ntdll!LdrpCheckRedirection` — 리다이렉션 비트가 켜졌을 때만 | [정적] |
| `ntdll!LdrpSnapModule` | `+0xe29` / `+0xe44` | `+0xe2e` / `+0xe49` | `ntdll!CompatCachepLookupCdb` — 못 찾음 오류 처리에서만 | [정적] |
| `ntdll!LdrpSnapModule` | `+0xe8f` | `+0xe94` | `ntdll!LdrpReportError` — 못 찾음 | [정적] |
| `ntdll!LdrpSnapModule` | `+0xef5` | `+0xefa` | `ntdll!NtUnmapViewOfSection(-1, ctx+0xc0)` | [정적] |
| `ntdll!LdrpDoPostSnapWork` | `+0x3e` | `+0x43` | `ntdll!NtProtectVirtualMemory` — IAT 보호 되돌리기 | [정적] |
| `ntdll!LdrpDoPostSnapWork` | `+0x8e` | `+0x93` | **`ntdll!LdrpHandleTlsData(항목)`** — `TlsIndex == 0` 일 때만 | [정적] |
| `ntdll!LdrpDoPostSnapWork` | `+0x68` | `+0x6d` | `ntdll!LdrControlFlowGuardEnforcedWithExportSuppression` | [정적] |
| `ntdll!LdrpDoPostSnapWork` | `+0xa4` | `+0xa9` | `ntdll!LdrpUnsuppressAddressTakenIat(DllBase, 0, 0)` — 위가 0 이 아닐 때만 | [정적] |

- `+0x605` 의 주소에는 같은 이름이 9개 있다 (Ghidra 표시 `RtlDeleteBoundaryDescriptor`). 후보: `RtlpFtFree`, `RtlpCtFreeMemory`, `RtlpFreeAtom`, `IdnaMemFree`, `RtlpFreeEnvironmentHashTable`, `RtlpFreeEnvBlock`, `NtdllpFreeStringRoutine`, `RtlpSysVolFree` 등. 원본 바이트로는 `PEB.ProcessHeap` 을 꺼내 `RtlFreeHeap` 으로 `JMP` 하는 7줄 껍데기다 (결과서). 이름은 믿지 말 것.
- `+0x94`·`+0x56e memset`, `+0x6b1 RtlImageNtHeaderEx`, `+0x82a`/`+0xab5 RtlpxLookupFunctionTable`, `+0xcf7`/`+0xd2b RtlAddressInSectionTable`, `+0xf48`/`+0xf63 LdrpLogLoadFailureEtwEvent`, 로그 `LdrpLogInternal`, `__security_check_cookie` 는 표에서 뺐다 (아래 구간 표에 자리만).
- **이름 → 주소 helper 호출이 없다.** `_calls.tsv`·`.asm` 에 간접 호출(`indirect:`)도 하나도 없다. `LdrpGetProcedureAddress`·`LdrpNameToOrdinal` 은 **인라인**이다: 로그 인자에 그 이름 문자열이 박혀 있다 (`+0xb1c` `"LdrpNameToOrdinal"`, `+0x756`·`+0xb42` `"LdrpGetProcedureAddress"`, 소스 `minkernel\ldr\ldrsnap.c`).

### 정적 — `LdrpSnapModule` 큰 구간

코드는 forwarder·실패 갈래가 주소순으로 흩어져 있어서 범위가 겹쳐 보일 수 있다 (결과서 표를 줄임).

| # | 범위 | 하는 일 | 대표 호출 |
|---|---|---|---|
| 1 | `+0x0 ~ +0xb1` | 입구. `R15 = ctx`, importer 항목(`ctx+0x38`)·`DllBase`·`FullDllName` 꺼냄, 시작 로그, 교체 대기 정리, 8칸 캐시 비우기 | `+0x79`, `+0x81`, `+0x94` |
| 2 | `+0xb7 ~ +0x14c`, `+0x462 ~ +0x478` | **서술자 고리** 머리·꼬리. `ctx+0x80 < ctx+0x68` 동안. 자식 항목 = `ctx+0x58[번호]`, 서술자 20바이트에서 OFT·FirstThunk | `+0xfc` (조건부) |
| 3 | `+0x152 ~ +0x2af` | 자식의 **export 디렉터리** 찾기 (NT 헤더 → 데이터 디렉터리 0번), 최근 8곳 캐시, CFG 켜졌으면 함수 표 확인 | `+0x189` |
| 4 | `+0x2af ~ +0x430` | **thunk 고리.** 서수·이름 갈림, hint → 이분 탐색, 주소 계산, forwarder 판정, **`+0x3e7` IAT 쓰기**, 커서 +8, `ctx+0x84++` | `+0xd5d` (조건부) |
| 5 | `+0x487 ~ +0x8a0` 외 | **forwarder.** 자르기 → `#서수` 면 `RtlCharToInteger` → `NTDLL` 지름길 또는 `LdrpLoadDependentModuleInternal` → 대상 export 에서 다시 찾기, 또 forwarder 면 최대 32번 | `+0x4e5`, `+0x5a5`, `+0x5da`, `+0x94b`, `+0x855` |
| 6 | `+0x8a2 ~ +0xa4b` | 2·3·5 의 **PE32(`0x10b`) 복사본**. 64비트끼리는 안 탈 것 `[추측]` | — |
| 7 | `+0xb59 ~ +0xbf0` | **끝맺음.** `LdrpDoPostSnapWork` → 끝 로그 → **`State = 5`** → `ctx+0xc0` 뷰 닫기 → 음수면 `LdrpLogError` | `+0xb61`, `+0xb7b`, `+0xbbc`, `+0xef5` |
| 8 | `+0xbf2 ~ +0xfe3` 나머지 | 실패·희귀 갈래: export 없음, 못 찾음 처리 `+0xe0b ~ +0xe99`, 리다이렉트된 forwarder, 예외 착지 `+0xe9e` | `+0xe29`/`+0xe44`, `+0xe8f` |

### 정적 — 하는 일 순서

```
LdrpSnapModule(ctx)                              ← MapAndSnap +0x4c2 / ProcessWork +0x131
 ├─ +0x7e   시작 로그 / +0x86 LdrpHandlePendingModuleReplaced
 ├─ 서술자 고리 (ctx+0x80 < ctx+0x68)              ← 모듈의 서술자 전부
 │   ├─ 자식 항목 = ctx+0x58[번호]   (0 이면 그 서술자 건너뜀, +0x149)
 │   ├─ +0x18e  자식 export 디렉터리 찾기
 │   └─ thunk 고리 (OFT 칸이 0 일 때까지, +0x2b3)
 │        ├─ 서수(최상위 비트) → 서수 − Base 로 색인
 │        ├─ 이름 → hint 자리 먼저 비교 → 다르면 이분 탐색 (인라인)
 │        ├─ 주소 = 자식 DllBase + 함수 RVA   (+0x3ac)
 │        ├─ 주소가 export 디렉터리 안 = forwarder (+0x3c0, +0x487)
 │        │     ├─ +0x4ea strrchr '.' → '#' 면 +0x950 RtlCharToInteger
 │        │     ├─ "NTDLL" (대소문자 무시) → +0x649 자식 = LdrpNtDllDataTableEntry   (올리지 않음)
 │        │     └─ 그 밖 → +0x5df LdrpLoadDependentModuleInternal(이름, ctx, 수출 항목, 1, ..)
 │        │           0x103 이면 → +0xeb7 반환 0 (PostSnap·State 5 건너뜀, ctx+0x80/+0x84 그대로)
 │        ├─ 못 찾음 → 0xc0000139 (이름) / 0xc0000138 (서수), +0xe94 LdrpReportError
 │        └─ ★ +0x3e7  [IAT 칸] = 주소   → +0x3f6 커서 +8, +0x402 ctx+0x84++
 ├─ ★ +0xb66 LdrpDoPostSnapWork(ctx)      IAT 보호 되돌림 → CFG 칸 검사 → TLS → CFG unsuppress
 ├─ +0xb80  끝 로그
 ├─ ★ +0xb87 노드 State = 5 (Snapped)
 └─ +0xb93 ~  ctx+0xc0 뷰 닫기 (+0xefa), 음수면 +0xbc1 LdrpLogError → RET (eax = 상태)
```

### 정적 — 질문 1: IAT 칸을 쓰는 줄 = **`+0x3e7` 하나**

- `+0x3e7 MOV qword ptr [RCX],RSI` (`18003aca7`). `.asm` 의 스택 밖 쓰기 7줄 + `INC` 2줄을 다 봐도 IAT 로 갈 수 있는 쓰기는 이것뿐이다 (결과서 전수 조사).
  - `RCX` = 쓰기 커서 (`+0x3df` 에서 `[RSP+0xa0]`). 서술자 `FirstThunk` + importer `DllBase` 로 시작하고 (`+0x129` 근처), 이어서 하기면 `ctx+0x84 × 8` 만큼 앞서 시작한다 (`+0x291`). 한 칸마다 `+0x3f6 ADD RCX,8`.
  - `RSI` = 쓸 값. 보통 `+0x3ac` 자식 `DllBase` + 함수 RVA. forwarder 를 풀었으면 `+0x885`, 리다이렉트면 `+0xd62` 에서 바뀐다.
  - 앞 조건 `+0x3cc JS` — 찾기가 실패면 이 줄을 건너뛰고 오류 처리.
- 이름·서수 둘 다 같은 줄로 모인다. 갈림은 thunk 최상위 비트 (`+0x2ca SHR RDI,0x3f`, `+0x2e5`).
  - 서수: `서수 − Base` 로 색인, `+0x396` 함수 수와 비교, `+0x39c LFENCE`, `+0x3a1` `AddressOfFunctions[색인]`.
  - 이름: `+0x316` hint 읽기 → `+0x321` hint 가 이름 수 이상이면 가운데(`+0x44c`)부터 → 한 바이트씩 비교 (인라인 `strcmp`) → 다르면 `+0x45c` (low = mid+1) / `+0x437` (high = mid−1) → `+0x332` low > high 면 못 찾음. **hint 자리가 이분 탐색의 첫 걸음**이다.
- **1a. 이름 → 주소 = 본문 안에서 직접 (예).** helper 호출도, 간접 호출도 없다 (위).
- **1b. 모듈의 서술자를 전부 돈다.** 바깥 `ctx+0x80` / `ctx+0x68`, 안쪽 OFT 칸이 0 일 때까지. OFT 가 0 이거나 이미지 밖이면 FirstThunk 를 대신 읽는다 (`+0x138`).
  - **`ctx+0x70/+0x78`(IAT 범위)은 이 함수가 안 읽는다.** IAT 자리는 서술자 FirstThunk 로 정한다. `+0x70/+0x78` 은 `LdrpDoPostSnapWork` 의 보호 되돌리기용이다.
  - `ctx+0x80`(서술자 번호)·`ctx+0x84`(그 안에서 끝낸 칸 수)를 문맥에 적어 두므로 **멈췄다가 같은 자리부터 다시 할 수 있다** (질문 2 의 `STATUS_PENDING`).

### 정적 — 질문 2: `+0x4e5 ~ +0x5da` = **forwarder (예)**, 지연 import 아님

- 들어가는 조건: 계산한 주소가 **자식 export 디렉터리 범위 안**이면 (`+0x3c0`, `+0x487`) 그 "주소" 는 `"다른DLL.함수"` 문자열이다 (PE 규격 forwarder). 이름·서수 import 둘 다 같다.
- `+0x4e5 strrchr(문자열, '.')` 로 자른다. 점이 없거나 DLL 부분이 너무 길면 `0xc000007b`.
- `DLL.#서수` 꼴: `+0x518 CMP byte [점+1],'#'` → `+0x93d ~ +0x94b RtlCharToInteger`. 이름 포인터는 0 으로 두고 (`+0x958`) 나중에 서수로 찾는다.
- **대상 DLL 이 `NTDLL` 이면 올리지 않는다** (`+0x533 ~ +0x555`, 길이 5 · `ntdll` 대소문자 무시) → `+0x649` 자식 = `LdrpNtDllDataTableEntry`.
- 그 밖이면 **그 자리에서 올린다**: `+0x5a5` 이름 버퍼 → **`+0x5da` (ret `+0x5df`) `LdrpLoadDependentModuleInternal`**. 인자 (run23 번호): 1 대상 이름, 2 `ctx` (importer 의 `LoadContext`), 3 **forwarder 를 가진 쪽(수출하는 모듈) 항목**, 4 **이유 1 = `LoadReasonStaticForwarderDependency`**, 5 결과 항목 칸, 6 간선 칸 (끝에 `+0x855 RtlFreeHeap`).
- 대상 export 를 다시 찾는 줄 `+0x674 ~ +0x7d9` (이름은 hint 없이 이분 탐색). 대상 주소가 **또 forwarder 면 다시** — 32번이면 `0xc000007b` (`+0xc22`, `+0xfd5`).
- **지연 import 가 아니다.** 이 함수가 읽는 NT 헤더 칸에 지연 import 디렉터리(13번)가 없다 (결과서).
- **부모 카운터와 엮인다 (예).** 간선은 callee `+0xf69/+0xf8b` 가 쓰고, 부모 노드 = 인자 2 의 항목 = **importer** 다. 그래서 forwarder 대상은 **importer 의 자식 간선**으로 걸리고 이유 코드만 1 이다.
  - snap 이 불릴 때 `ctx+0x6c` 는 이미 0 이다. 그래서 callee 의 "카운터 0" 갈래 (run23) 가 여기서 일한다: 대상 `State < 2` 면 `ctx+0x50 = 대상`, `ctx+0x6c = 1`, importer `State = 3`, 반환 **`0x103`**.
  - snap 은 `0x103` 을 받으면 (`+0x634`, `+0x85f CMP R12D,0x103`) **`LdrpDoPostSnapWork` 도 `State = 5` 도 건너뛰고 0 으로 나간다** (`+0xeb7 XOR EAX,EAX`). `ctx+0x80/+0x84` 가 그대로라서 다시 불리면 같은 thunk 부터 한다.
  - 다시 불리는 길: 대상이 Mapped 가 되면 `LdrpSignalModuleMapped` 가 importer `ctx+0x6c` 를 0 으로 → `State = 4` + `LdrpQueueWork` → `LdrpProcessWork +0x131` (run24). 줄 관계는 정적, 실제 순서는 동적.
  - 대상이 이미 `State ≥ 2` 면 callee 가 0 을 돌려줘 그냥 이어간다.
  - 대상 올리기가 실패하면 `+0x5ea JS` → `+0xe03 *(importer.LoadContext+0x28) = 상태` → 오류 처리.

### 정적 — 질문 3: `State = 5` = **`+0xb87` 한 곳, `LdrpDoPostSnapWork` 뒤**

- `+0xb87 MOV dword ptr [RAX+0x38],0x5` (`18003b447`), `RAX = importer DdagNode`.
- 순서: `+0xb59` 앞 상태 음수면 건너뜀 → **`+0xb61` PostSnap** → `+0xb69` 음수면 건너뜀 → `+0xb7b` 끝 로그 → **`+0xb87` State 5** → `+0xb8e` 꼬리.
- **실패하면 이 함수는 State 를 안 쓴다** (snap 시작 때의 4 그대로). 실패는 반환값(`+0xbc1`)으로만 나가고, caller 가 `*(ctx+0x28)` 에 옮긴다 (run22·run24).
- 다른 `State = 5`: `LdrpMapAndSnapDependency +0x5ae` (IAT 없는 모듈, run22). snap 을 도는 건 IAT 가 있는 모듈뿐이다.
- `State = -3` (`LdrModulesSnapError`) 를 **처음** 쓰는 곳은 못 찾았다 `[공백]`. `.asm` 모음에서 -3 을 쓰는 줄은 전파 `LdrpCondenseGraphRecurse +0x14e` 하나뿐 (결과서) → 정적 Q69.

### 정적 — 질문 4: `LdrpDoPostSnapWork` (rva `0xa4200`, 228B)

| 자리 | 하는 일 |
|---|---|
| `+0x20 ~ +0x47` | `ctx+0x70`(IAT 시작) ≠ 0 이면 **`+0x3e NtProtectVirtualMemory(-1, &ctx+0x70, &ctx+0x78, [ctx+0x90], &임시)`** = IAT 보호를 원래대로. 음수면 그대로 끝 |
| `+0x49 ~ +0x66` | `ctx+0xa0` ≠ 0 이면 `*(ctx+0xa0)` 를 `ctx+0x98` 과 비교, **다르면 `INT 0x29` (ECX = `0x13`, 빠른 실패)** |
| `+0x84 ~ +0x99` | 항목 **`TlsIndex(+0x6e) == 0` 이면 `+0x8e LdrpHandleTlsData(항목)`**, 음수면 그대로 끝 |
| `+0x68 ~ +0x71`, `+0x9b ~ +0xe3` | `LdrControlFlowGuardEnforcedWithExportSuppression()` ≠ 0 이면 `+0xa4 LdrpUnsuppressAddressTakenIat(DllBase, 0, 0)`, 실패면 로그 |

- 코드 흐름 순서는 **보호 되돌림 → CFG 칸 검사 → TLS → CFG unsuppress** 다 (`+0x5f`/`+0x53 JZ +0x84` 로 TLS 를 먼저 보고, `+0x89 JNZ`·`+0x97 JNS` 로 `+0x68` 에 간다).
- **보호 되돌림 = `LdrpPrepareImportAddressTableForSnap` 의 짝 (예).** Prepare 가 `PAGE_READWRITE`(4) 로 바꾸며 원래 값을 `ctx+0x90` 에 적고, 여기서 그 값으로 되돌린다 (결과서, Prepare `.c`).
- CFG 칸 검사: Prepare 가 IAT 를 열어 둔 동안 모듈 LoadConfig 의 `GuardCFCheckFunctionPointer` 칸(`ctx+0xa0` = 칸 주소, `ctx+0x98` = 그때 값)이 바뀌지 않았나 본다. 모듈이 CFG 계측일 때만 채워진다 (결과서). `0x13` 의 SDK 이름은 `[추측]`.
- CFG unsuppress: "CFG 켜짐 + export suppression" 프로세스일 때만 (이름·로그 문구로 읽은 뜻 `[추측]`, 켜짐 여부는 동적).
- **DLL 의 TLS 목록 항목은 여기서 만든다 (조건 `TlsIndex == 0` 하나).** 이 함수엔 exe/DLL 구분이 없다.
  - `TlsIndex` 는 TLS 항목이 만들어지면 `0xffff` 가 된다 (`LdrpHandleTlsData +0x3a8`, `LdrpInitializeTls +0x102`, run28). 그러니 "아직 TLS 항목을 안 만든 모듈" 만 들어간다.
  - exe: `LdrpInitializeTls` 호출(`LdrpInitializeProcess +0x18cb`)이 exe 의 `MapAndSnap`(`+0x1aa4`)보다 **앞 오프셋**이라, exe 가 여기 올 땐 `TlsIndex ≠ 0` 이라 건너뛸 것 `[추측: 오프셋 순서]`.
  - dll1·dll2 (TLS 디렉터리 있음 `[소스]`): `TlsIndex == 0` → 여기서 `LdrpHandleTlsData` `[정적: 조건 줄]`. TLS 디렉터리 없는 DLL 은 callee 안 `+0x88` 에서 건너뛴다 (run28).

### 정적 — 질문 5: snap 은 부모를 안 건드린다 — **아니오**

- 스택 밖 쓰기 7줄에 부모 노드(`ctx+0x30` 쪽)·`ctx+0x6c` 를 쓰는 줄이 없다. `ctx+0x30` 은 읽지도 않는다 (`R15+0x30` 읽기 `+0x674`·`+0x7ed` 는 그때 `R15` 가 자식 항목이라 `DllBase`).
- State 쓰기는 자기 노드 `+0xb87` 하나. `LdrpDoPostSnapWork` 도 노드·카운터를 안 쓴다.
- 영향이 가는 유일한 곳: forwarder 의 `+0x5da` callee 안 (`ctx+0x6c`·`ctx+0x50`·importer `State = 3`). 이건 snap **중간**의 일이다.
- caller 쪽 마무리는 실패 상태 옮기기뿐이다 (`*(ctx+0x28)`).
- 부모 카운터를 줄이는 건 snap 이 아니라 **자식이 Mapped 가 되는 순간**(`LdrpSignalModuleMapped`, run24) 이다 → **부모는 자식이 Snapped 가 되기를 기다리지 않는다** `[정적: 줄 관계]`.

### 정적 — 질문 6: 작은 갈래

| 갈래 | 조건 | test.exe 에서 |
|---|---|---|
| `+0x81 LdrpHandlePendingModuleReplaced` | 입구에서 무조건 (조건은 callee 안, 안 읽음) | 섬 |
| `+0xfc LdrpFreeReplacedModule` | 자식 항목 `LoadContext ≠ 0` + 그 문맥 `+0x20` 비트 `0x80000` 꺼짐 + 문맥 `+0x38` ≠ 자식 항목 (= 올리는 사이 같은 DLL 이 다른 항목으로 교체됨). `ctx+0x58[번호]` 를 바꿔 치고(`+0xa72`·`+0x105`) 옛 항목 정리 | 안 탈 것 `[추측]` |
| `+0xd5d LdrpCheckRedirection` (이름만) / `+0xecd` (forwarder 푼 뒤) | `ctx+0x20` 비트 `0x2000000` (`MapAndSnap +0xd6` 이 `LdrpRedirectionModule ≠ 0` 일 때 켬, run22). 돌려준 값 `0xffffffffffbadd11` = 리다이렉트 없음 | 안 탈 것 `[추측]` |
| `+0xe29`/`+0xe44 CompatCachepLookupCdb` | 못 찾음 오류 처리 안에서만 (`+0xe0b`: `0xc000007a` 또는 `0xc0000135`). 로그만 다르고 상태는 안 바뀜 | 안 탐 |
| 못 찾음 코드 | 이름 **`0xc0000139`** (`STATUS_ENTRYPOINT_NOT_FOUND`, `+0xe5e`) / 서수 **`0xc0000138`** (`STATUS_ORDINAL_NOT_FOUND`, `+0xe76`), 판정 `+0xe59`. 둘 다 `+0xe8f LdrpReportError`. thunk 마다 `0xc000007a` 로 시작 (`+0x2bc`) | — |

- 그 밖 실패: export 표 없음·forwarder 문자열 이상·너무 깊음 `0xc000007b`, 헤더 이상·서수 0 `0xc000000d`. forwarder 대상 DLL 을 못 올림(`0xc0000135` 등)도 `+0xe0b` 에서 `0xc0000139` 로 바뀌어 반환될 수 있다 (원래 값은 `LoadContext+0x28` 쪽에 남음, `+0xe03`).
- 예외: 필터 `LdrpSnapModule$filt$0` → `LdrpGenericExceptionFilter`. 착지점 `+0xe9e ~ +0xeb6` (원본 바이트, 결과서) 은 지역 상태 `[RSP+0x40]` 를 들고 꼬리 `+0xb93` 으로 간다. 어느 구간이 `__try` 인지는 안 봤다 (`.pdata` 안 읽음).

### 샘플 PE 계산 — test.exe 에서는 `[정적+샘플]`

| 모듈 | 서술자 (import 수) | IAT 쓰기 (`+0x3e7`) | forwarder |
|---|---|---|---|
| `test.exe` | `dll1.dll` (1: `Dll1Hello`, hint 0) · `KERNEL32.dll` (90) | 91번 | KERNEL32 쪽 10개 → 모두 `NTDLL.Rtl…` |
| `dll1.dll` | `dll2.dll` (1: `Dll2Hello`, hint 0) · `KERNEL32.dll` (88) | 89번 | 10개 → `NTDLL.Rtl…` |
| `dll2.dll` | `KERNEL32.dll` (88) | 88번 | 10개 → `NTDLL.Rtl…` |

- **hint**: `Dll1Hello`·`Dll2Hello` 는 hint 0 이 이름 표 0번과 맞아 첫 비교에서 찾는다. KERNEL32 쪽은 **전부 hint 가 틀린다** (이 빌드 kernel32 이름 표와 정확히 5 씩 어긋남 — 샘플을 만든 PC 의 import 라이브러리 기준이라서). 그래서 KERNEL32 import 는 전부 hint 한 번 + 이분 탐색 (이름 1697개, 대략 11걸음). 동작에는 문제가 없다 (코드가 이분 탐색으로 간다).
- **forwarder 10개**: `InitializeSListHead`, `InterlockedPushEntrySList`, `InterlockedFlushSList`, `EnterCriticalSection`, `LeaveCriticalSection`, `DeleteCriticalSection`, `EncodePointer`, `HeapAlloc`, `HeapSize`, `HeapReAlloc` → 모두 `NTDLL.Rtl…`. 그래서 **`+0x649` NTDLL 지름길**을 타고 `+0x5da LdrpLoadDependentModuleInternal` 은 안 탈 것이다 `[추측: 이 샘플 import 만 셈]`. `RtlCharToInteger`·`0x103` 갈래도 안 탈 것 `[추측]`.
- kernel32 자신(서술자 104·import 1274)·kernelbase(4·750)의 import 도 api-set 호스트를 KernelBase 로 가정해 세 보니 forwarder 대상은 NTDLL 뿐이었다 — 근사 `[정적+샘플, 가정 있음]`.
- dll2 의 import 는 **`KERNEL32.dll` 하나뿐**이다 (flow.md 2.6 의 "dll2 import 표를 안 봄" 칸이 채워짐).

### 대표 지점 (그림 2 `LdrpSnapModule` 상자 아래)

| # | 지점 | 오프셋 / `ntdll+rva` | 뜻 |
|---|---|---|---|
| 1 | IAT 칸에 주소 쓰기 | `LdrpSnapModule +0x3e7` / `ntdll+0x3aca7` | `rcx` = 칸 주소, `rsi` = 쓸 주소 (`ln @rsi`) |
| 2 | `LdrpDoPostSnapWork` 호출 | call `+0xb61` / `ntdll+0x3b421`, ret `+0xb66` / `ntdll+0x3b426` | 서술자 다 돈 뒤 |
| 3 | 노드 `State = 5` | `LdrpSnapModule +0xb87` / `ntdll+0x3b447` | PostSnap 성공 + 끝 로그 뒤 |

### 호출 관계

```
ntdll!LdrpMapAndSnapDependency(ctx) ── +0x4c2 ─┐       (run22, 부모 없음·남은 자식 0)
ntdll!LdrpProcessWork(ctx)          ── +0x131 ─┤       (run24, 노드 State ≠ 0)
                                               └─ ntdll!LdrpSnapModule(ctx)
                                                    ├─ (1) +0x86   LdrpHandlePendingModuleReplaced
                                                    ├─ (2) +0x18e  RtlImageNtHeaderEx            (서술자마다, 캐시에 없을 때)
                                                    ├─ (3) +0x4ea  strrchr                       (forwarder 일 때만)
                                                    ├─ (4) +0x5df  LdrpLoadDependentModuleInternal(…, 1)   (forwarder 대상이 NTDLL 아닐 때만)
                                                    │      +0x3e7  ★ IAT 칸 쓰기 (호출 아님)
                                                    ├─ (5) +0xb66  LdrpDoPostSnapWork(ctx)
                                                    │        ├─ (1) +0x43  NtProtectVirtualMemory     (ctx+0x70 ≠ 0)
                                                    │        ├─ (2) +0x93  LdrpHandleTlsData(항목)    (TlsIndex == 0)
                                                    │        ├─ (3) +0x6d  LdrControlFlowGuardEnforcedWithExportSuppression
                                                    │        └─ (4) +0xa9  LdrpUnsuppressAddressTakenIat   (위가 ≠ 0)
                                                    │      +0xb87  ★ State = 5 (호출 아님)
                                                    └─ (6) +0xefa  NtUnmapViewOfSection(-1, ctx+0xc0)
```

- 번호는 **코드 흐름 순서**다 (PostSnap 안 TLS 가 CFG 확인보다 먼저인 것도 점프를 따른 순서).

### 그림 반영 (flow.md 그림 2)

- `(1)~(2) 사이` 상자의 `LdrpSnapModule` 노드 넷 (`[test.exe]` 직접 `ja4`, `[test.exe 추정]` 일감 `jw5`, `[dll1 추정]` `jw9`, `[dll2 추정]` `jw11`) 글에 본문 요약을 붙였다: IAT 쓰기 `+0x3e7` 한 줄, forwarder (NTDLL 지름길), `+0xb87 State = 5`.
- 넷 아래에 **`LdrpDoPostSnapWork` (정적)** 노드를 하나씩 넣었다 (라벨 `+0xb66`). caller 노드가 다르니 함수 하나 = 노드 하나로 따로 그렸다 (R7).
- dll1·dll2 의 `LdrpDoPostSnapWork` 아래에만 **`LdrpHandleTlsData` (정적)** 노드를 넣었다 (라벨 `+0x93`, `TlsIndex == 0`). exe 쪽은 `TlsIndex ≠ 0` 이라 안 설 것 `[추측]` 이라 상자 글에만 적었다.
- `+0x5df LdrpLoadDependentModuleInternal` (forwarder 대상 올리기) 은 test.exe·dll1·dll2 에서 안 탈 것 `[추측: 샘플 PE]` 이라 그림에 안 넣고 글에만.
- `jc2` (dll2 `MapAndSnap`) 글 "dll2 의 import 는 안 봄" → "import = KERNEL32.dll 하나 (샘플 PE, run36)".

## 확인된 것

- (`[정적]`) **IAT 칸에 import 함수 주소를 쓰는 줄은 `LdrpSnapModule +0x3e7` 하나다.** 이름·서수·forwarder·리다이렉트가 모두 여기로 모인다.
- (`[정적]`) 이름 → 주소 찾기는 **본문 인라인** (hint 자리 먼저 → 이분 탐색). helper·간접 호출 없음.
- (`[정적]`) snap 은 **모듈의 서술자 전부**를 돈다. 진행 위치를 `ctx+0x80/+0x84` 에 적어 두어 중간에 멈췄다 다시 할 수 있다. IAT 범위 `ctx+0x70/+0x78` 은 snap 본문이 안 읽는다.
- (`[정적]`) `+0x4e5 ~ +0x5da` 갈래는 **forwarder** 다. 지연 import 가 아니다. 대상이 `NTDLL` 이면 올리지 않고 `LdrpNtDllDataTableEntry` 를 쓴다. 그 밖이면 `LdrpLoadDependentModuleInternal(…, 이유 1)` 로 그 자리에서 올리고, 간선은 **importer** 의 자식으로 걸린다.
- (`[정적]`) forwarder 대상이 아직 Mapped 전이면 `0x103` 을 받아 **PostSnap·State 5 없이 0 으로 일단 끝난다** → 나중에 snap 일감으로 같은 thunk 부터 다시. (sast/18 의 "`+0x5df` 는 snap 몫" `[추측]` 이 맞았다.)
- (`[정적]`) **노드 `State = 5` 는 `+0xb87` 한 곳**, `+0xb61 LdrpDoPostSnapWork` 가 성공하고 끝 로그를 남긴 뒤. 실패하면 snap 은 State 를 안 쓴다 (4 그대로).
- (`[정적]`) `LdrpDoPostSnapWork` = IAT 보호 되돌림 (`LdrpPrepareImportAddressTableForSnap` 의 짝) → CFG 포인터 칸 변조 검사 (다르면 빠른 실패) → **`TlsIndex == 0` 이면 `LdrpHandleTlsData`** → CFG export suppression 이면 unsuppress.
- (`[정적]`) **DLL 의 TLS 목록 항목을 만드는 `LdrpHandleTlsData` 의 caller 가 여기다** (`LdrpDoPostSnapWork +0x8e`, ret `+0x93`). run28 은 그 함수 안쪽만 봤다.
- (`[정적]`) snap 은 부모 노드·부모 카운터를 쓰지 않는다. 부모는 자식이 Snapped 가 되기를 기다리지 않는다 (카운터는 자식 Mapped 때 줄어듦).
- (`[정적]`) 못 찾음 코드: 이름 `0xc0000139`, 서수 `0xc0000138`.
- (`[정적+샘플]`) test.exe 91 · dll1 89 · dll2 88 칸. KERNEL32 hint 는 전부 틀려 이분 탐색, forwarder 10개씩은 모두 NTDLL 지름길. dll2 의 import 는 KERNEL32.dll 하나.

### flow.md 판정

| 예전 글 | 판정 | 근거 |
|---|---|---|
| "`LdrpSnapModule +0x5df` 갈래 = 지연 import 또는 forwarder" `[추측]` — 글자 그대로는 plan Q50·sast/31 요청서에 있고, flow.md 2.5 에는 지도 줄 "`LdrpSnapModule +0x5df` → `LdrpLoadDependentModuleInternal`" 만 있었다 | **forwarder** `[정적]`. 2.5 그 줄에 답을 붙였다 | 질문 2 |
| 2.6 "`0x103` 은 다른 caller(`LdrpSnapModule +0x5df`, …) 몫 `[추측]`" (sast/18) | **맞다** — snap 쪽은 `[정적]` (`LdrpLoadDependentModuleW +0xae` 쪽은 그대로) | 질문 2 |
| 2.6 일감 한 바퀴 "`LdrpSnapModule(P문맥)` (본문 Q50)", 남은 칸 "`LdrpSnapModule` 안 (IAT 쓰기, `State = 5`, Q50)" | **해결됨** | 질문 1·3 |
| 2.6 이야기 "dll2 의 import = ? 이미 올라온 것뿐이라면", "dll2 import 표를 안 봄" | **채움** `[정적+샘플]` — KERNEL32.dll 하나 | 샘플 PE |
| 4장 2.5·2.6 "IAT 에 함수 주소를 적는 곳 … `State 5` 로 쓰는 곳은 아직 모름 — 정적 Q50" | **해결됨** | 질문 1·3 |
| 2.7 "DLL 은 `LdrpHandleTlsData +0x10b`" (누가 부르는지 없음) | **더함** — caller `LdrpDoPostSnapWork +0x8e` | 질문 4 |

## 정정

- 없음. 이전 run 의 `[추측]` 둘(위 표 첫 두 줄)은 틀린 게 아니라 좁혀지거나 맞았다.

## 아직 확인 필요

동적 확인거리는 오케스트레이터가 대기열에 넣었다: 결과서 동적 2·5 (스레드·순서) → **Q47** 출처, 동적 1·3·4·6 → 새 **Q68**. 정적 공백 → 새 **Q69**.

- **Q47** (출처 더함) snap 이 어느 스레드·어떤 순서로 도나: `ntdll+0x3a8c0` (snap 입구) 에서 `poi(poi(@rcx+0x38)+0x50)` (FullDllName 버퍼)·`@$tid`, `ntdll+0x3b447` (`State = 5`) 에서 `poi(@rdi+0x50)`·`@$tid`. 기존 `ntdll+0x2155d` (`MapAndSnap` 직접 snap call) / `ntdll+0x21592` (`LdrpQueueWork` call) 와 같은 실행에서.
- **Q68** (새, 동적) snap 실행 값 (test.exe 만):
  - (a) IAT 쓰기 `ntdll+0x3aca7`: `r rcx`, `ln @rsi` — dll1 snap 동안 `Dll1Hello` 칸에 `dll1` 안 주소, forwarder 10개가 `ntdll!Rtl…` 인지. 한 모듈에서 수십~천 번 서니 개수 조건 필요 (test.exe 91 · dll1 89 · dll2 88 · kernel32 1274 · kernelbase 750).
  - (b) NTDLL 지름길 `ntdll+0x3af09` 횟수 — test.exe·dll1·dll2 에서 각 10번 기대.
  - (c) 0번 기대: `ntdll+0x3ae9a` (forwarder 대상 올리기), `ntdll+0x3b11f` (`0x103` 비교), `ntdll+0x3b74f` (`LdrpReportError`), `ntdll+0x3b61d` (`LdrpCheckRedirection`), `ntdll+0x3a9bc` (`LdrpFreeReplacedModule`).
  - (d) PostSnap: `ntdll+0xa423e` (`NtProtectVirtualMemory` call — `rdx` = `&ctx+0x70`, `r8` = `&ctx+0x78`, `r9d` = 되돌릴 보호값), **`ntdll+0xa428e` (`LdrpHandleTlsData` call) 가 어느 모듈에서 서나 — dll1·dll2 만, exe 는 안 섬 `[추측]`**, `ntdll+0xa426d` 에서 `r eax` (CFG export suppression 켜짐?).
  - (e) 값: `dq ntdll+0x1d2600 L1` (`LdrpRedirectionModule`), `dq ntdll+0x1ea508 L1` (`LdrSystemDllInitBlock+0xb8`, run32), `db ntdll+0x1ea4ec L1`.
- **Q69** (새, 정적, 작음) 노드 `State = -3`(`LdrModulesSnapError`) 를 **처음** 쓰는 곳, snap 실패 반환이 caller `*(ctx+0x28)` 뒤 어디서 State 로 바뀌나.
- 다음 바퀴 후보 (flow-writer, 대기열 판단은 오케스트레이터):
  - (Q60 단서) 결과서 샘플 계산으로는 test.exe 의 KERNEL32 import 90개 중 forwarder 는 위 10개뿐이고 `LoadLibraryExW` 는 그 목록에 없다 → kernel32 export `LoadLibraryExW` 는 forwarder 가 아닐 것 `[정적+샘플, 간접: 결과서가 이 이름을 따로 짚지는 않음]`. 그러면 run7 `[관찰]` 의 `KERNELBASE!LoadLibraryExW` 와 잇는 건 kernel32 쪽 짧은 스텁일 것 `[추측]` — Q60 에서 kernel32 export rva 와 그 몸통으로 확인.
  - 시동 때 일꾼이 snap 일감을 돌리면 `LdrpHandleTlsData` (`LdrpTlsLock` 독점, run28) 도 일꾼에서 돌 수 있다 `[정적: 길]` → run28 이 남긴 Q25 락 질문과 이어진다 (동적 Q25·Q47 같은 실행).

## 디버거 메모 / 도구 메모

- (다음 동적용) `ntdll+0x3aca7` 은 모듈마다 수십 번 이상 선다. kernel32·kernelbase snap 까지 합치면 2천 번이 넘는다 → 모듈 이름 조건 + 개수 제한이 필요하다 (dast-mentor 몫).
- (다음 동적용) `State = 5` 자리(`ntdll+0x3b447`) 에서 importer 항목은 **`rdi`** 다 (`rax` 는 DdagNode).
- (다음 동적용) forwarder 가 `0x103` 으로 미뤄지면 같은 모듈의 snap 입구(`ntdll+0x3a8c0`)가 **두 번** 설 수 있다. 두 번째는 `ctx+0x80`/`+0x84` 가 0 이 아니다.

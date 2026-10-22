# 16 load-dll-internal — 정적 분석 결과

- 요청서: `_workspace/sast/16_load-dll-internal.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). 이미 분석된 모듈이라 analyze 는 안 했다 (`.gpr`, `_index.tsv`, `SetPdb:` 줄 확인).
- 뽑은 함수: 정규식 `^(LdrpLoadDllInternal|LdrpPrepareModuleForExecution)$`, depth 1 → 29개 (`ms/decomp/ntdll/`, `ms/disassemble/ntdll/`). **callee 본문은 읽지 않았다** (이름·호출 자리만). 이름 사슬은 `_map/LdrInitializeThunk_edges.tsv` 에서 가장 짧은 길(BFS)로 찾았다.
- 새로 만든 것: `ms/disassemble/ntdll/_refs/LdrpMainThreadToken.tsv` (`refs`, 질문 1용). 형별·오프셋 확인은 `llvm-pdbutil-14 dump -types ms/sym/ntdll.dll.pdb` 로 PDB 타입을 직접 읽었다 (스크립트 변경 없음).
- 버전 확인: `LdrpLoadDll +0xfb → LdrpLoadDllInternal` (ret `+0x100`), `LdrpLoadDllInternal +0x27f → LdrpPrepareModuleForExecution` (ret `+0x284`), `LdrpPrepareModuleForExecution +0xea → LdrpInitializeGraphRecurse` (ret `+0xef`), `LdrpInitializeProcess +0x1bb6 → LdrpInitializeGraphRecurse` (ret `+0x1bbb`) 가 모두 `_calls.tsv`·`.asm` 과 run7·run8 관찰 값과 **일치**. 호출 줄 수도 요청서와 같다 (`LdrpLoadDllInternal` 33줄, `LdrpPrepareModuleForExecution` 8줄).

## 스크립트 문제

없음.

## 읽는 법 (오프셋·이름)

- 아래 `+0x..` 는 **ret 자리** (WinDbg 콜스택 기준). call 자리는 "(call)" 로 따로 적었다.
- 구조체 칸 이름은 PDB 타입으로 확인했다 `[정적]`:
  - `_LDR_DATA_TABLE_ENTRY`: `+0x30 DllBase`, `+0x68 Flags`(비트 5 = `ProcessStaticImport`), `+0x98 DdagNode`, `+0xb0 LoadContext`, `+0x10c LoadReason`, `+0x128 ActivePatchImageBase`, `+0x130 HotPatchState`.
  - `_LDR_DDAG_NODE`: `+0x10 ServiceTagList`, `+0x18 LoadCount`, `+0x38 State`.
  - `_LDR_DDAG_STATE`: 5 `Snapped`, 6 `Condensed`, 7 `ReadyToInit`, 8 `Initializing`, 9 `ReadyToRun` (4 `Snapping`, 2 `Mapped`, 1 `Mapping`).
  - `_LDR_DLL_LOAD_REASON`: 4 `DynamicLoad`, 9 `PatchImage`.
  - TEB `+0x17ee SameTebFlags` 의 비트 12 = `LoadOwner` (값 `0x1000`), `+0x1720 SubProcessTag`.
  - `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없다 (앞선 선언만). `LoadContext +0x20`, `+0xc8` 의 뜻은 **이름을 못 정한다.**
- 요청서의 인자 순번: `param_1`=RCX … `param_4`=R9D, `param_5`~`param_9` = 스택 `[RSP+0x20]`~`[RSP+0x40]` (호출 직전 기준).
- `LdrpLoadDllInternal` 의 반환형은 void. 결과는 **`param_7`(결과 항목 칸) 과 `param_8`(상태 칸)** 으로 나간다.

## 질문별 답 [정적]

### 1. `LdrpLoadDllInternal` 앞쪽 순서 (이유 = 4, `LdrLoadDll` 길 기준)

근거: `ms/disassemble/ntdll/LdrpLoadDllInternal.asm`, 구조 확인용 `ms/decomp/ntdll/LdrpLoadDllInternal.c`.

**순서 (시간 순)**

1. `+0x55`, `+0x84` `LdrpLogInternal` 2번 (이름 로그). 그다음 `*param_7 = 0`, 지역 항목 칸 `[RSP+0x40] = 0`.
2. `+0x9e CMP EDI,9` — 이유가 9(`PatchImage`)면 `+0xb3` 을 건너뛰고 `+0x15f` 로. 이유 4 에서는 안 뛴다.
3. `+0xb8 LdrpFastpthReloadedDll(이름, 플래그, 부모항목, *결과항목)` (call `+0xb3`).
   - 반환 `eax >= 0` 이거나 `eax == 0xc00004c0` 이면 **빠른 끝**: `*상태 = eax` 를 쓰고 `+0xe3` (마지막 로그 2개, `+0x113`, `+0x143`) 뒤 `RET` (`+0x152`). 이 길은 `+0x15f` 아래 전부(Drain, `FindOrPrepare`, `ProcessWork`, `+0x284` 의 `PrepareModuleForExecution`)를 **건너뛴다** (`+0xc2 JZ +0x154` → `+0x154 CMP EAX,0xc00004c0 JZ +0xc8`, `eax` 가 0 이상이면 `+0xc2` 에서 안 뛰고 바로 `+0xc8`).
   - 다른 음수면 느린 길 `+0x15f` 로 (`+0x154 CMP ... JNZ` 아래로 떨어짐).
   - 그러니까 "이미 올라온 DLL 을 빨리 돌려주는 길" 이 맞다고 **코드로는 읽힌다** `[추측]` (이름·인자·`LdrpFindLoadedDllByName`·`LdrpIncrementModuleLoadCount` 호출 이름은 지도에서만 봤고 본문은 안 읽음).
4. `+0x15f` TEB `SameTebFlags & 0x1000` (`LoadOwner`):
   - 이미 `LoadOwner` 면 `+0x420` 에서 `BL = 1` (지역 바이트 `[RSP+0xa8]` 에도 저장) 하고 `LdrpDrainWorkQueue(0)` 를 **건너뜀**.
   - 아니면 `BL = 0`, `+0x18a LdrpDrainWorkQueue(0)` (call `+0x185`, `ECX = 0`).
5. `+0x18d CMP EDI,9 JZ +0x42e` — 이유 9 면 패치 전용 갈래(질문 5 참조). 이유 4 는 계속.
6. `+0x1a0 LdrpThreadTokenSetMainThreadToken()` (call `+0x19b`, 인자 없음).
7. `+0x1a1~+0x1b5`: 부모 항목(`param_6`)이 있고(`R13 != 0`) `BL == 0` 이고 부모의 `DdagNode->LoadCount(+0x18) == 0` 이면 `+0x36a` 에서 `*상태 = 0xc0000135` 쓰고 `+0x210` 으로 직행 (`FindOrPrepare`·`ProcessWork` 건너뜀). `LdrLoadDll` 길은 `param_6 = 0` 이라 이 조건은 안 걸린다. (참고: 이 갈래는 뒤의 `+0x22f` 에서 지역 항목이 NULL 이라 결국 `*상태 = 0xc0000017` 로 덮인다.)
8. `+0x1c0 LdrpDetectDetour()` (call `+0x1bb`, 인자 없음).
9. `+0x1f2 LdrpFindOrPrepareLoadingModule(이름, 경로구조체, 플래그, 이유, param_5, &지역항목, 상태 칸)` (call `+0x1ed`). 7개 인자 (`[RSP+0x20]=param_5`, `[RSP+0x28]=&지역항목(RSP+0x40)`, `[RSP+0x30]=상태 칸`).
   - 반환 `0xc0000135` → `+0x20b` 에서 `LdrpProcessWork(지역항목->LoadContext(+0xb0), 1)` (call `+0x20b`, ret `+0x210`). 이때 `*상태` 는 안 쓴다.
   - 반환 `0xc000022d`, 또는 `0` 이상 → 아무것도 안 하고 `+0x210`.
   - 그 밖의 음수 → `+0x30b` 에서 `*상태 = eax`, 그다음 `+0x210`.
   - 역할 `[추측]`: 이름으로 이미 목록에 있는지 찾고, 없으면 새 항목(+ `LoadContext`)을 만들어 `0xc0000135` 로 "이제 실제로 올려라" 를 알린다 → `ProcessWork` 가 실제 올리기(경로 찾기·파일 열기·섹션 매핑, 질문 2). 이 해석은 지도의 callee 이름(`LdrpAllocateModuleEntry`, `LdrpLoadKnownDll`, `LdrpFindLoadedDllByName`)에 기댄 것이라 본문 확인 전 `[추측]`.
10. `+0x21a LdrpDrainWorkQueue(1)` (call `+0x215`, `ECX = 1`). `ProcessWork` 를 안 탄 길에서도 항상 탄다.
11. `+0x21a~+0x224` `LdrpMainThreadToken != 0` 이면 `+0x317 LdrpThreadTokenUnsetMainThreadToken()` (call `+0x312`).
12. `+0x22f` 지역 항목 NULL 이면 `+0x31c` `*상태 = 0xc0000017` (`STATUS_NO_MEMORY`) 후 정리로 (`+0x408`).
13. `+0x240 LdrpHandleReplacedModule(항목)` (call `+0x23b`, **항목이 NULL 이 아니면 무조건**). 결과를 `*결과항목` 에 쓴다.
    - 결과 ≠ 원래 항목이면 `+0x341 LdrpFreeReplacedModule()` (call `+0x33c`) 하고 새 항목으로 교체; 새 항목의 `LoadReason(+0x10c) == 9` 이고 이유 ≠ 9 이면 `*상태 = 0xc00004c0` (`+0x35f`).
14. `+0x262 LdrpCondenseGraph(항목->DdagNode)` (call `+0x25d`): 조건은 **`항목->LoadContext(+0xb0) != 0`**. 호출 뒤 `LoadContext +0xc8 = 0` (`+0x269`).
15. `+0x274 CMP [상태],0; JL` — **상태가 0 이상일 때만** `+0x284 LdrpPrepareModuleForExecution(항목, 상태 칸)` (call `+0x27f`). 음수면 `+0x2b1` (정리) 로 건너뛴다.

**`LdrInitState` 를 읽는 곳과 갈림**
- 이 함수 안에서는 **`+0x29b` 하나뿐** (`_refs/LdrInitState.tsv`). `CMP LdrInitState,0` 이 0 이면 `+0x337 LdrpPinModule(항목)` (call `+0x332`). 즉 **`LdrInitState == 0` 인 동안(= `LdrpInitializeProcess +0x1a4c` 에서 1 로 쓰기 전) 이 길로 올라온 모듈은 "고정(pin)" 된다.** 이 조건은 `Prepare` 와 `BuildForwarderLink` 가 모두 0 이상일 때만 닿는다.
- 같은 변수를 읽는 callee 이름(본문 안 읽음): `LdrpDynamicShimModule +0x12d`(`==3`), `LdrpMapDllNtFileName +0x20a`(`==3`), `LdrpAllocateModuleEntry +0xc2`(`==1`), `LdrpApplyPatchImage +0x62`(`==3`), `LdrpProcessMachineMismatch +0x9f`.
- 값 쓰는 곳: `LdrpInitializeProcess +0x1a4c`(=1), `+0x1b1e`(=2), `+0x17dd`(=3), `_LdrpInitialize +0x101`·`+0x205`.

**`LdrpDrainWorkQueue`·메인 스레드 토큰이 kernel32 준비 때(`LdrInitState == 0`, `LdrpEnableParallelLoading +0x1a42` 전)에도 타나**
- 이 함수 코드에는 **`LdrInitState` 조건이 없다** (읽는 곳이 `+0x29b` 뿐이므로). `+0x185`·`+0x215`(Drain) 와 `+0x19b`(Set) 는 `LoadOwner` 인지(`+0x16d`) 와 이유가 9 인지에만 갈린다. 그래서 `LdrLoadDll("KERNEL32.DLL")` 도 이 호출 자리들을 **지나가는 코드 모양** 이다.
- `+0x312 Unset` 은 **전역 `LdrpMainThreadToken` 이 0 이 아닐 때만** (`+0x21a`). 전역은 `LdrpThreadTokenSetMainThreadToken +0x29` 에서만 써지고 `...UnsetMainThreadToken +0x1f` 에서 0 으로 되돌려진다. 읽는 곳은 이 함수 `+0x21a`(+ `$fin$0 +0x3d`), `LdrpInitializeProcess +0x1ab4`, `LdrpWorkCallback`, `TppWorkpExecuteCallback` (`_refs/LdrpMainThreadToken.tsv`). **Set 이 이 전역을 실제로 쓰는지(조건이 있는지)는 callee 본문이라 정적으로 못 정함** → 동적.
- `LdrpInitializeProcess` 쪽도 같은 짝이다: `+0x1a89 Set` ... `+0x1aac Drain(1)` ... `+0x1ab4` 읽기 → `+0x1abd Unset`. 병렬 로딩을 켠 뒤(+0x1a42)에는 같은 함수가 다시 쓰인다.

**트리 (flow.md 2.5 `LdrLoadDll` 안쪽에 붙일 수 있게)**

```
LdrpLoadDllInternal  (이유=4 / 결과: *결과항목, *상태)
├ +0x55·+0x84      LdrpLogInternal ×2
├ +0xb8            LdrpFastpthReloadedDll   ── eax>=0 또는 0xc00004c0 → 로그 뒤 return (+0x284 건너뜀)
├ +0x18a           LdrpDrainWorkQueue(0)     (LoadOwner 가 아닐 때만)
├ +0x1a0           LdrpThreadTokenSetMainThreadToken
├ +0x1c0           LdrpDetectDetour
├ +0x1f2           LdrpFindOrPrepareLoadingModule ── 0xc0000135 일 때만
│  └ +0x210          LdrpProcessWork(LoadContext, 1)      ← 올리기 (질문 2)
├ +0x21a           LdrpDrainWorkQueue(1)
├ +0x317           LdrpThreadTokenUnsetMainThreadToken   (토큰 != 0 일 때)
├ +0x240           LdrpHandleReplacedModule   (바뀌었으면 +0x341 LdrpFreeReplacedModule)
├ +0x262           LdrpCondenseGraph          (LoadContext != 0)
├ +0x284           LdrpPrepareModuleForExecution   ── 상태 >= 0 일 때 (질문 4)
├ +0x295           LdrpBuildForwarderLink(부모항목, 항목)
├ +0x337           LdrpPinModule              (LdrInitState == 0 일 때만)
└ +0x2c8           LdrpFreeLoadContextOfNode(DdagNode, 상태 칸)  → 정리 (질문 5)
```

### 2. 올리기가 실제로 일어나는 곳 (지도 이름 사슬, 본문 안 읽음)

근거: `ms/disassemble/ntdll/_map/LdrInitializeThunk_edges.tsv` 의 caller→callee 줄 (표기는 ret 자리, 괄호는 최단 깊이).

**(가) KnownDll 길 — 메모리에 올리는 시스템 콜까지 (깊이 5)**
`LdrpLoadDllInternal +0x1f2 → LdrpFindOrPrepareLoadingModule +0x5e5 → LdrpLoadKnownDll +0xf1 → LdrpMapDllWithSectionHandle +0x4e → LdrpMinimalMapModule +0x179 → NtMapViewOfSection`
- `LdrpLoadKnownDll +0x63 → LdrpFindKnownDll +0xd9 → NtOpenSection` (깊이 4), `LdrpLoadKnownDll +0xfd → NtClose`. 즉 이 길은 **파일을 열지(`NtOpenFile`) 않고 `\KnownDlls` 섹션을 연다** (`NtCreateSection` 도 이 길에는 없다).
- **KnownDll 쪽이 이 길 위에 있다: 예** (`LdrpLoadKnownDll` 이 `LdrpFindOrPrepareLoadingModule +0x5e5` 안에서 불린다). 조건(어떤 DLL 이 KnownDll 인지)은 정적으로 못 정함.

**(나) 파일 경로 길 — 파일 열기·섹션 만들기·매핑 (깊이 4~6)**
- `LdrpLoadDllInternal +0x210 → LdrpProcessWork +0x6a → LdrpMapDllSearchPath +0x25c → LdrpMapDllNtFileName +0x137 → NtOpenFile` (깊이 4)
- 같은 길에서 `LdrpMapDllNtFileName +0x194 → NtCreateSection` (깊이 4)
- 이어서 `LdrpMapDllNtFileName +0x28c → LdrpMapDllWithSectionHandle +0x4e → LdrpMinimalMapModule +0x179 → NtMapViewOfSection` (깊이 6)
- `LdrpProcessWork` 안의 다른 갈래 이름: `+0x144 LdrpMapDllFullPath`, `+0x14e LdrpMapDllRetry`(→ `LdrpGetNtPathFromDosPath +0xf3 → NtQueryAttributesFile`), `+0x158 LdrpMapDllPatchImage`(패치용). `+0x131 LdrpSnapModule`.
- 즉 **실제 올리기(섹션 만들기·매핑)는 `LdrpFindOrPrepareLoadingModule`(KnownDll 갈래) 또는 `LdrpProcessWork`(파일 갈래) 아래 `LdrpMapDllWithSectionHandle` 에서 일어난다.** 지도에서 `LdrpMapDllWithSectionHandle` 의 caller 는 `LdrpLoadKnownDll`, `LdrpMapDllNtFileName`, `LdrpMapDllPatchImage` 셋이다.

**(다) 의존 DLL 을 처리하는 함수까지 (이름은 `LdrpLoadDependentModule*`)**
- 이름 확인: 지도에 `LdrpLoadDependentModuleInternal`, `LdrpLoadDependentModuleW`, `LdrpMapAndSnapDependency`, `LdrpSnapModule` 이 있다 (`LdrpLoadDependentModule` 단독 이름은 없다).
- 길 1 (깊이 4~5): `LdrpLoadDllInternal +0x1f2 → LdrpFindOrPrepareLoadingModule +0x5e5 → LdrpLoadKnownDll +0xf1 → LdrpMapDllWithSectionHandle +0x1c8 → LdrpMapAndSnapDependency` 안에서 `+0x34e LdrpLoadDependentModuleInternal` / `+0x4c2 LdrpSnapModule` / `+0x5d8 LdrpLoadDependentModuleW`(→ `+0xae LdrpLoadDependentModuleInternal`).
- 길 2 (깊이 3): `LdrpLoadDllInternal +0x210 → LdrpProcessWork +0x131 → LdrpSnapModule +0x5df → LdrpLoadDependentModuleInternal`.
- `LdrpLoadDependentModuleInternal` 안에는 `+0x1792 LdrpLoadKnownDll`, `+0x182c LdrpQueueWork`, `+0x1cae LdrpProcessWork`, `+0x115c LdrpAllocateModuleEntry`, `+0x16ee LdrpFindLoadedDllByName` 이 있다 → **의존 DLL 처리가 다시 같은 올리기 함수들로 돌아오는 재귀 모양**이다.

**flow.md 2.5 `[추측]` "kernel32 를 올리면 kernelbase 가 같은 `LdrLoadDll` 안에서 따라 올라온다" 에 대한 답: 예, 지도상 길이 있다.**
- kernel32 를 올리는 `LdrpLoadDllInternal` 의 호출 트리 **안쪽**에 위 길 1·2 (`LdrpMapAndSnapDependency`/`LdrpSnapModule` → `LdrpLoadDependentModuleInternal` → 다시 `LdrpLoadKnownDll`·`LdrpProcessWork`) 가 있어서, kernel32 의 import(kernelbase)를 **별도 `LdrLoadDll` 없이 같은 호출 안에서** 올릴 수 있는 코드 모양이다. `LdrLoadDll` 을 다시 부르는 길이 필요 없다.
- 단 `LdrpQueueWork`(`+0x182c`) 로 일을 큐에 넣고 `LdrpDrainWorkQueue` 가 돌리는 갈래도 이름에 보인다. 병렬 로딩 전(`LdrInitState == 0`) 에 이 일이 어디서 돌아가는지는 callee 본문이라 모른다 → Q47(일꾼)과 Q20(동적).
- 실제로 이 길로 갔는지는 동적 (Q20): `NtMapViewOfSection` 이 불릴 때 위 호출 사슬이 콜스택에 보이는지.

### 3. 둘째 `LdrLoadDll("KERNELBASE.dll")` 은 이 함수 안에서 어디로 끝나나

근거: 위 1번 `.asm` 줄 + `_map` 이름(`LdrpFastpthReloadedDll` 의 callee 이름: `LdrpFindLoadedDllByName +0x47`, `LdrpIncrementModuleLoadCount +0x77`, `LdrpDrainWorkQueue +0x11f` 등, 본문은 안 읽음).

정적으로 **두 길이 가능하고 어느 쪽인지는 못 정한다.**
- **(가) `+0xb8` 에서 끝 (빠른 끝).** `LdrpFastpthReloadedDll` 이 "이미 올라옴" 으로 `eax >= 0` 을 주면 `+0xc8` → `+0x143` → return. **`+0x284 PrepareModuleForExecution` 까지 안 간다.** 이름·callee 이름(`LdrpFindLoadedDllByName`, `LdrpIncrementModuleLoadCount`)으로 보면 이쪽이 이름에 어울린다 `[추측]`.
- **(나) 느린 길에서 `LdrpFindOrPrepareLoadingModule` 이 이미 있는 항목을 찾음.** `eax >= 0`(≠ `0xc0000135`) → `ProcessWork` 안 부름 → `+0x21a Drain(1)` → `+0x240 HandleReplacedModule` → (`LoadContext == 0` 이면 `+0x262 Condense` 안 탐) → **`+0x284` 까지 간다.** 그러나 `LdrpPrepareModuleForExecution` 안에서 노드 상태가 9(`ReadyToRun`) 면 `+0x40` 의 분기로 **바로 `return 0`** (질문 4) 하므로 `+0xef` 의 `LdrpInitializeGraphRecurse` 는 안 불리고 `LdrpCallInitRoutine` 도 안 불린다 (run7 둘째 호출에서 안 불린 것과 **둘 다 맞는다**).
- 이 갈림은 `LdrpFastpthReloadedDll` 의 반환값이다 → 동적: `ntdll+0x1fce8` 에서 `eax` (Q49 에 더함).
- 참고: (나)에서는 `+0x29b` 에서 `LdrInitState == 0` 이라 `+0x337 LdrpPinModule` 도 다시 불릴 수 있다 (이미 고정된 것을 또 고정하는지는 callee 본문).

### 4. `LdrpPrepareModuleForExecution` 의 순서 (호출 8곳)

근거: `ms/disassemble/ntdll/LdrpPrepareModuleForExecution.asm` (`RCX`=항목, `RDX`=상태 칸), `ms/decomp/ntdll/LdrpPrepareModuleForExecution.c`.

**순서**
1. `+0x20 ~ +0x2b`: 현재 스레드 ID (TEB `+0x48`) 가 전역 `LdrpDllNotificationLock +0x10` (= 임계 구역 `OwningThread` 칸 자리) 과 같으면 **바로 `return 0`** (`+0xfe`). `[추측]` 뜻: DLL 알림 콜백 안에서 다시 들어온 경우를 막는 것.
2. `+0x31` 항목의 `DdagNode->State(+0x38)` 로 갈림:
   - 5 (`Snapped`) → `+0x12f LdrpCondenseGraph(노드)` (call `+0x12a`) 한 뒤 `+0x50` 으로.
   - 6 (`Condensed`) → `+0x50` 로.
   - 7 (`ReadyToInit`) → `+0xbf` 로 (아래 6번, 알림·shim 건너뜀).
   - 8 (`Initializing`), 9 (`ReadyToRun`) → **바로 `return 0`**.
   - 그 밖 → `return 0xc00000e5`.
3. `+0x50 ~ +0x6a`: 항목 `Flags(+0x68)` 의 비트 5(`ProcessStaticImport`) 가 0 이고 TEB `SubProcessTag != 0` 이면 `+0x6f LdrpAddNodeServiceTag(노드)` (call `+0x6a`).
4. `+0x7b LdrpNotifyLoadOfGraph(노드)` (call `+0x76`). 반환 음수면 그 값으로 return (`+0x7f JS +0xfe`).
5. `+0x8d LdrpDynamicShimModule(노드)` (call `+0x88`). 음수면 `+0xbd LdrpLogInternal("Failed to load for appcompat reasons")` 하고 그 값으로 return.
6. `+0xbf`: **`항목->LoadContext(+0xb0) != 0` 이고 `LoadContext +0x20` 의 비트 0 이 0 일 때만** (`+0xbf ~ +0xcf`) 아래 7번으로. 아니면 지금 `EBX` 값(0 또는 shim 결과) 으로 return.
7. `+0xd6 LdrpAcquireLoaderLock()` (call `+0xd1`) → **`+0xef LdrpInitializeGraphRecurse(노드, 상태 칸, &지역바이트=0)`** (call `+0xea`) → `+0xfe LdrpReleaseLoaderLock(…, EDX=2, R8D=eax)` (call `+0xf9`) → `eax` 를 돌려줌.
   - 로드 락 해제 호출의 `EDX` 가 여기선 **2**, `LdrpInitializeProcess +0x1bc7`(call) 에선 **9** 로 다르다. `[추측]` 호출 자리 표시 값.

**`+0xea` 를 타지 않는 조건 (코드에 쓰인 것 전부)**
- (a) 락 소유 스레드 검사로 `+0x2b` 에서 return, (b) 노드 상태 8·9 (이미 시작함·끝남) 이거나 5~9 밖, (c) 알림·shim 이 음수, (d) `LoadContext == 0`, (e) `LoadContext +0x20` 비트 0 이 1.
- **두 caller 의 차이는 (e)** 로 설명된다. `LdrpInitializeProcess` 는 exe 항목을 만들 때 쓰는 로드 문맥(스택 `[RSP+0x2b0]`, `+0x1351 LEA RCX → LdrpAllocateModuleEntry`)의 `+0x20` 칸(`[RSP+0x2d0]`)에 **`+0x1a94 OR dword ptr [RSP+0x2d0], R12D`** (`R12D = 1`, `+0x1a8e`) 로 비트 0 을 **세운 뒤** `+0x1aa4 LdrpMapAndSnapDependency` 와 `+0x1aed LdrpPrepareModuleForExecution(LdrpImageEntry, …)` 를 부른다. 그래서 exe 로 부른 `Prepare` 는 `+0xcb JNZ +0xfe` 로 **그래프 켜기를 건너뛰고**, 켜기는 `LdrpInitializeProcess +0x1b91(락) → +0x1bbb LdrpInitializeGraphRecurse` 가 한꺼번에 한다 (run8 관찰과 맞음). 이 로드 문맥이 항목 `+0xb0` 칸에 들어간다는 것은 sast/14 연결 (`+0x1351` ↔ `LoadContext`) 이고, `+0x20` 비트 0 의 **이름은 모른다** `[추측]` ("프로세스 초기화 문맥: 켜기는 호출자가" 쯤).
- `LdrpLoadDllInternal` 쪽은 `LdrpFindOrPrepareLoadingModule` 이 만든 문맥을 쓰는데, 이 함수 안에서 그 비트를 세우는 줄은 없다 `[정적, 이 두 함수 안]`. 비트 0 의 값은 callee 가 정하므로 확인 전이다 → 동적. 그래도 run7 이 `+0xef` 에서 켜기를 **관찰**한 것과는 어긋나지 않는다 (비트가 0 이면 켠다).
- 같은 `Prepare` 가 새로 올라온 노드 상태(5·6)에서 `Condense`→통지→shim→켜기 순서로 돈다. `LdrpLoadDllInternal` 도 `+0x262` 에서 `LdrpCondenseGraph` 를 먼저 한 번 부르므로 5 인 노드는 보통 이미 6 일 것이다 `[추측]`.

**트리**

```
LdrpPrepareModuleForExecution(항목, 상태 칸)
├ (현재 스레드 == LdrpDllNotificationLock.OwningThread 이면 return 0)
├ State == 5 → +0x12f LdrpCondenseGraph      State 8·9 → return 0 / 6 → 다음 / 7 → 아래 6번
├ +0x6f  LdrpAddNodeServiceTag   (ProcessStaticImport 아님 && SubProcessTag != 0)
├ +0x7b  LdrpNotifyLoadOfGraph
├ +0x8d  LdrpDynamicShimModule   (실패 시 +0xbd 로그)
└ LoadContext != 0 && LoadContext+0x20 비트0 == 0 일 때만
   ├ +0xd6 LdrpAcquireLoaderLock
   ├ +0xef LdrpInitializeGraphRecurse(노드, 상태 칸, &0)   ← 그래프 켜기
   └ +0xfe LdrpReleaseLoaderLock
```

### 5. `LdrpLoadDllInternal` 뒤쪽 (`+0x284 ~ 끝`)

근거: 같은 `.asm`, 짧게.

- `+0x284` 결과를 `*상태` 에 쓰고, 음수면 `+0x2a8` (아래). 아니면 `+0x295 LdrpBuildForwarderLink(부모항목 R13, 항목)` (call `+0x290`) → `*상태` 에 쓰고 음수면 `+0x2a8`. 그다음 `+0x29b LdrInitState == 0` 이면 `+0x337 LdrpPinModule(항목)`.
- `+0x2a8` 이유 ≠ 9 면 `+0x2b1` 로.
- `+0x2c8 LdrpFreeLoadContextOfNode(항목->DdagNode, 상태 칸)` (call `+0x2c3`). 이 함수는 `LdrpInitializeProcess +0x1be7` 에서도 불린다 (exe 쪽 켜기 뒤) → 켜기 뒤에 로드 문맥을 치우는 같은 짝.
- 실패(`*상태 < 0`): 이유 ≠ 9 이면 `+0x2da` 에서 `*결과항목 = 0`, `+0x2eb LdrpDecrementModuleLoadCountEx(항목, 0)` (call `+0x2e6`), `+0x2f3 LdrpDereferenceModule(항목)` (call `+0x2ee`).
- 정리: `+0x408` `BL == 0` 이면 `+0x41b LdrpDropLastInProgressCount()` (call `+0x416`) → `+0xda` 에서 이유 9 면 `+0x514 LdrpDereferenceModule(param_5)` (call `+0x50f`) → `+0x113`·`+0x143` 마지막 로그 2개(`Status: 0x%08lx`) → `RET`.
- **이유 9 (PatchImage) 전용**, 이유 4 는 안 탄다: `+0x42e` `LdrpFindLoadedDllByHandle(param_9, &param_5, 0)` (call `+0x441`), `+0x4b6 LdrpQueryCurrentPatch` (call `+0x4b1`), `+0x37d~+0x3eb` `LdrpApplyPatchImage` (call `+0x3ac`), 해당 로그 (`+0x4fe`, `+0x3eb`), `+0x47e LdrpFreeUnicodeString` (call `+0x479`). 조건은 `HotPatchState(+0x130) == 4(FailedToPatch)` 이면 `*상태 = 0xc0000142`, `ActivePatchImageBase(+0x128) == DllBase(+0x30)` 이면 패치 안 부름.
- 예외 조각 `LdrpLoadDllInternal$fin$0` (rva `0x1683b0`, 예외 때 OS 가 부름): `CL != 0`(예외 중) 이면 `*상태 = 0xc0000005`, `LdrpLogError`, `LdrpDrainWorkQueue(1)`, 토큰 있으면 `LdrpThreadTokenUnsetMainThreadToken`, `BL == 0` 이면 `LdrpDropLastInProgressCount`. (본체 정리와 같은 순서의 "마무리 세트".)
- **결과를 caller 에 돌려주는 자리**: `*param_7`(결과 항목 칸): `+0x8e` 에서 0, `LdrpFastpthReloadedDll` 이 채울 수 있음 (인자 R9), `+0x240` 에서 `HandleReplacedModule` 결과, 실패 때 `+0x2da` 에서 0. `*param_8`(상태 칸): `+0xd0`(빠른 끝), `+0x30b`, `+0x31c`, `+0x35f`, `+0x372`, `+0x284`, `+0x295`, 마지막 로그에서 값을 읽어 찍음 (`+0x113`·`+0x13e`).
- 잡일 개수: `LdrpLogInternal` 6곳(본체), `LdrpFreeUnicodeString` 1곳, `$fin$0` 의 `LdrpLogError` 1곳.

### 6. 인자 대조 (`LdrpLoadDll +0xfb` → `LdrpLoadDllInternal`)

`LdrpLoadDll.asm +0xbd~+0xfb` 로 호출 직전 값을 다시 읽었다: `RCX = &[RSP+0x60]`(전처리 이름), `RDX = RDI`, `R8D = [RSP+0x58]`, `R9D = 4`, `[RSP+0x20] = 0`, `[RSP+0x28] = 0`, `[RSP+0x30] = RSI`(결과 항목 칸), `[RSP+0x38] = &[RSP+0x50]`(상태 칸), `[RSP+0x40] = 0`. 상태 칸은 `LdrpPreprocessDllName` 반환값(`+0xc2`)이 이미 들어 있는 곳이다 (`+0x274` 에서 0 이상이어야 `Prepare` 를 탄다).

| 인자 | 값 (`LdrLoadDll` 길) | 이 함수 안에서 어디로 가나 |
|---|---|---|
| `param_1` RCX (`RSI`) | 전처리 이름 | 로그 (`+0x55`,`+0x84`), `+0xb3 Fastpth` 1번째, `+0x1ed FindOrPrepare` 1번째, (이유 9 에서) `+0x479 FreeUnicodeString` |
| `param_2` RDX | 경로 구조체 (스택 `[RSP+0x98]` 에 저장) | `+0x1ed FindOrPrepare` 2번째 (`+0x1e2`). `Fastpth` 에는 안 간다 |
| `param_3` R8D (`R12D`) | 플래그 | `+0xb3 Fastpth` 2번째(`EDX`), `+0x1ed FindOrPrepare` 3번째 |
| `param_4` R9D (`EDI`) | 이유 = 4 | 이유 == 9 비교가 여러 곳(`+0x9e`,`+0xda`,`+0x18a`,`+0x2a8`,`+0x2d1`,`+0x356`), `+0x1ed FindOrPrepare` 4번째 (`+0x1dc`). `Fastpth`·`Prepare` 에는 안 간다 |
| `param_5` `[RSP+0xb0]` (`R14`) | 0 (패치용 기반 항목) | `+0x1ed FindOrPrepare` 5번째(`[RSP+0x20]`), 이유 9 일 때 `+0x50f LdrpDereferenceModule`, `+0x389` 비교 |
| `param_6` `[RSP+0xb8]` (`R13`) | 0 (부모 항목) | `+0xb3 Fastpth` 3번째(`R8`), `+0x1a1` 부모 `LoadCount` 검사, `+0x290 LdrpBuildForwarderLink` 1번째(`RCX`) |
| `param_7` `[RSP+0xc0]` (`R15`) | 결과 항목 칸 | `+0xb3 Fastpth` 4번째(`R9`), `+0x240` 에서 쓰기, `+0x2da` 에서 0 |
| `param_8` `[RSP+0xc8]` (`RBX`) | 상태 칸 | `+0x1ed FindOrPrepare` 7번째(`[RSP+0x30]`), `+0x27f Prepare` 2번째(`RDX`), `+0x2c3 FreeLoadContextOfNode` 2번째, 마지막 로그에서 읽음 |
| `param_9` `[RSP+0xd0]` | 0 | 이유 9 에서만 `+0x441 LdrpFindLoadedDllByHandle` 1번째 |

## 그림 2 `(정적)` 상자 후보 (2.5, 순서대로)

1. **`LdrpLoadDllInternal +0xb8 LdrpFastpthReloadedDll`** — 이유: 이미 올라온 DLL 이면 `+0x284` 까지 안 가고 끝내는 갈림 (둘째 `LdrLoadDll(KERNELBASE.dll)` 의 행선지, 질문 3).
2. **`LdrpLoadDllInternal +0x1f2 LdrpFindOrPrepareLoadingModule` → `+0x210 LdrpProcessWork`** — 이유: 새로 올려야 할 때만(`0xc0000135`) 실제 올리기(`NtOpenFile`·`NtCreateSection`·`NtMapViewOfSection`)가 이 아래에서 일어난다.
3. **`LdrpLoadDllInternal +0x284 LdrpPrepareModuleForExecution` → `+0xef LdrpInitializeGraphRecurse`** — 이유: run7 에서 kernel32 준비 때 의존 그래프 켜기를 본 자리. 켜기는 `LoadContext +0x20` 비트 0 으로 exe 쪽(`LdrpInitializeProcess +0x1bbb`)과 갈린다.
4. **`LdrpLoadDllInternal +0x337 LdrpPinModule`** — 이유: `LdrInitState == 0` 인 동안(kernel32 준비 포함)에만 타서 kernel32·kernelbase 를 고정하는 자리 (이 시기를 가르는 유일한 조건).

## 동적분석으로 확인해야 할 것

(번호는 `_workspace/plan.md` 의 기존 항목에 이어서. 모두 `ntdll+rva`, test.exe 일 때만 멈추게 — 조건은 dast 가 붙임.)

| 멈춤점 | `ntdll+rva` | 볼 것 | 번호 |
|---|---|---|---|
| `LdrpLoadDllInternal` ret `+0xb8` | `0x1fce8` | `eax` (Fastpth 결과: `>=0` 이거나 `0xc00004c0` 이면 `+0x284` 안 감). 첫째(`KERNEL32`)·둘째(`KERNELBASE`) 비교 | Q49 에 더함 |
| ret `+0x1a0` (Set 뒤) | `0x1fdd0` | `dq ntdll+0x1d2618 L1` (`LdrpMainThreadToken` 0 인지) — kernel32 준비(`LdrInitState` 0) 때 Set 이 값을 쓰나 | 새 (Q11 동적 후보) |
| ret `+0x1f2` | `0x1fe22` | `eax` (`0xc0000135` 인가), `[rsp+0x40]` 지역 항목 → `du poi(poi(@rsp+0x40)+0x60)` 이름 | 새 |
| ret `+0x284` | `0x1feb4` | `eax` (Prepare 결과), 둘째 호출에서 이 줄이 오는지 | Q49 |
| `LdrpPrepareModuleForExecution` 입구 | `0x86e4c` | `rcx`=항목: `dd poi(@rcx+0x98)+0x38 L1` (State), `dq @rcx+0xb0 L1` (LoadContext), `db poi(@rcx+0xb0)+0x20 L1` (비트 0). kernel32 때 비트 0 이 0 이고 exe 때 1 인지 | 새 (Q24 와 이어짐) |
| 같은 함수 call `+0xea` / ret `+0xef` | `0x86f36` / `0x86f3b` | 이 줄이 오는 호출이 어느 모듈인지(`du poi(@rcx+0x60)` 입구 값 저장 필요) | Q21·Q22·Q24 |
| ret `+0x29b` 직전 | `0x1fecb` | `dd ntdll+0x1d23c8 L1` (`LdrInitState`), 0 이면 `LdrpPinModule` 이 불리나 | 새 |
| `LdrpMinimalMapModule` ret `+0x179` (NtMapViewOfSection 호출 뒤) | rva 는 `_index.tsv` 로 | 콜스택에 질문 2 의 사슬(`LdrpFindOrPrepareLoadingModule +0x5e5` 또는 `LdrpProcessWork +0x6a` → …) 이 보이는지, KERNELBASE 가 KERNEL32 와 같은 `LdrLoadDll` 안인지 | Q20 |

- **dast 요청 필요**: 위 `새` 항목들(이미 대기 중인 Q20·Q21·Q22·Q24·Q49 에 흡수 가능). `LoadContext +0x20` 비트 0 이름과 `LdrpThreadTokenSetMainThreadToken` 이 전역을 쓰는 조건은 정적으로 못 정했다.

## Q12·Q15 에 넘길 연결점 (한 줄씩)

- **Q12 (그래프 만드는 함수)**: 후보 이름 — `LdrpAllocateModuleEntry`(노드·항목 만들기; caller `LdrpFindOrPrepareLoadingModule +0x47a`, `LdrpLoadDependentModuleInternal +0x115c`, `LdrpInitializeProcess +0x135e/+0x157c`), `LdrpMapAndSnapDependency`/`LdrpSnapModule`/`LdrpLoadDependentModuleInternal`(import 를 따라 의존 DLL 로 내려감), `LdrpCondenseGraph`→`LdrpCondenseGraphRecurse`→`LdrpMergeNodes`(고리 합치기). `LdrpRecordModuleDependency` 는 지도에서 `LdrpInitializeNtdllDataTableEntry +0x243` 만 부른다(ntdll 자신용). `LdrpDependencyExist` 는 `LdrpLoadContextReplaceModule +0x93`.
- **Q15 (`LdrpInitializeGraphRecurse` 본문)**: `Prepare +0xea` 와 `LdrpInitializeProcess +0x1bb6` 는 **같은 3 인자** (노드, 상태 칸, `&바이트=0`) 로 부른다. 어느 호출이 어떤 노드부터 시작하는지(kernel32 노드 vs exe 노드)와 `LdrpInitializeNode` 로 내려가는 길은 본문에서. 지도상 이 함수의 caller 는 `LdrpInitializeProcess` 3곳(`+0x16eb`,`+0x1941`,`+0x1bbb`), `LdrpPrepareModuleForExecution +0xef`, `LdrGetProcedureAddressForCaller +0x94a`, `AVrfInitializeVerifier`, `LdrpInitializeImportRedirection +0x150`, `LdrpInitializeShimDllDependencies +0xd2`.

## 판정

**분석성공.** 6개 질문 모두 `.asm` 줄과 지도 줄 근거로 답했다. 버전 대조(`LdrpLoadDll +0x100`, `LdrpInitializeProcess +0x1bbb`, `Prepare +0xef`)가 run7·run8 과 일치한다. `[추측]` 으로 남긴 것: `LdrpFastpthReloadedDll`·`FindOrPrepare` 의 역할(본문은 callee 라 안 읽음), `LoadContext +0x20` 비트 0 의 이름, 노드 상태 7 일 때 알림을 건너뛰는 이유. 실제로 어느 길을 탔는지(Fastpth 결과, 토큰 값, 비트 값)는 동적 확인으로 넘겼다.

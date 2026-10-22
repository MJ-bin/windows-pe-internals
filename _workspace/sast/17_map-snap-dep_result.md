# 17 map-snap-dep — 정적 분석 결과

- 요청서: `_workspace/sast/17_map-snap-dep.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). 이미 분석된 모듈이라 analyze 는 안 했다 (`.gpr`, `_index.tsv`, `SetPdb:` 줄 확인).
- 뽑은 함수: 정규식 `^(LdrpMapAndSnapDependency|LdrpAllocateModuleEntry)$`, depth 1 → 27개 (`ms/decomp/ntdll/`, `ms/disassemble/ntdll/`). **callee 본문(`LdrpLoadDependentModuleInternal`, `LdrpSnapModule`, `LdrpQueueWork`, `LdrpLoadDependentModuleW`)은 읽지 않았다.**
- 요청서 밖으로 넓힌 것 (이유 한 줄씩):
  - `LdrpMapDllWithSectionHandle` **depth 0** 으로 하나 더 뽑음 (`.asm` 만 읽음). 질문 6 에서 "누가 이 함수를 다시 부르나, 어떤 조건에서" 를 `+0x1c3` 앞뒤로 봐야 해서.
  - 이미 있던 `LdrpLoadDependentModuleInternal.asm`·`LdrpFindOrPrepareLoadingModule.asm`·`LdrpLoadDll.asm`·`LdrpLoadDllInternal.asm` 는 **호출 인자 줄과 로드 문맥 칸 쓰는 줄만** 봤다 (`+0x1157` 근처, `+0x10a3~+0x1157`, `+0x12df~+0x12f7`, `+0x1606~+0x1634`, `FindOrPrepare +0x3cb~+0x4a0`). 본문 순서는 안 읽었다 (Q12b 몫). 로드 문맥 `+0x30` 이 무엇인지 정하려면 이 줄들이 꼭 필요했다.
  - `LdrpPrepareImportAddressTableForSnap`, `LdrpGetImportDescriptorForSnap`, `LdrpShouldModuleImportBeRedirected`, `LdrpFindDllActivationContext`, `LdrpQueryCurrentPatch` 는 요청서 질문 1 이 직접 묻는 callee 라 `.c` 를 읽었다 (이 함수들의 안쪽 callee 는 안 봄).
- 새로 만든 것: 없음 (스크립트 변경 없음). 칸 이름·크기는 `llvm-pdbutil-14 dump -types ms/sym/ntdll.dll.pdb` (스크래치 파일, 커밋 안 함). `_calls.tsv` 는 export 로 줄이 덧붙었다.
- 버전 확인: `LdrpLoadDll +0xfb → LdrpLoadDllInternal` (ret `+0x100`), `LdrpInitializeProcess +0x1bb6 → LdrpInitializeGraphRecurse` (ret `+0x1bbb`) 가 `_calls.tsv` 와 **일치**. 호출 줄 수도 요청서와 같다 (`LdrpMapAndSnapDependency` 23줄, `LdrpAllocateModuleEntry` 5줄). 지도의 `LdrpInitializeProcess +0x1aa4/+0x1aa9`, `LdrpMapDllWithSectionHandle +0x1c3/+0x1c8` 도 일치.

## 스크립트 문제

없음.

## 읽는 법 (오프셋·이름)

- 아래 `+0x..` 는 **ret 자리** (WinDbg 콜스택 기준). call 자리는 "(call)". 메모리에 쓰는 줄(`MOV [..],..`)은 그 명령 자리.
- 이름은 PDB 타입으로 확인했다 `[정적]`:
  - `_LDR_DATA_TABLE_ENTRY` (크기 0x138): `+0x30 DllBase`, `+0x48 FullDllName`, `+0x58 BaseDllName`, `+0x68 Flags` (비트 2 `ImageDll`, 5 `ProcessStaticImport`, 24 `CorILOnly`, 26 `ChpeEmulatorImage`, 28 `Redirected`), `+0x6c ObsoleteLoadCount`, `+0x70 HashLinks`, `+0x88 EntryPointActivationContext`, `+0x90 Lock`, `+0x98 DdagNode`, `+0xa0 NodeModuleLink`, `+0xb0 LoadContext`, `+0xb8 ParentDllBase`, `+0x10c LoadReason`, `+0x110 ImplicitPathOptions`, `+0x114 ReferenceCount`, `+0x130 HotPatchState`.
  - `_LDR_DDAG_NODE` (크기 0x50): `+0x00 Modules`(목록 머리), `+0x10 ServiceTagList`, `+0x18 LoadCount`, `+0x20 LowestLink`, **`+0x28 Dependencies`**, **`+0x30 IncomingDependencies`** (둘 다 `_LDRP_CSLIST`, 칸 하나 `Tail`), `+0x38 State`, `+0x40 CondenseLink`, `+0x48 PreorderNumber`.
  - `_LDR_DDAG_STATE`: 0 PlaceHolder, 1 Mapping, 2 Mapped, 3 WaitingForDependencies, **4 Snapping**, **5 Snapped**, 6 Condensed, 7 ReadyToInit, 8 Initializing, 9 ReadyToRun.
  - `_LDR_DLL_LOAD_REASON`: 0 StaticDependency, 4 DynamicLoad, 9 PatchImage.
  - **`_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없다** (앞선 선언만). 아래 "로드 문맥 칸 표" 는 **오프셋만** 있고, 뜻은 코드에서 읽은 것이라 `[정적: 쓰고 읽는 줄]` 또는 `[추측]` 으로 구분했다.
- 인자: `param_1`=RCX … `param_4`=R9, 5번째부터 스택 `[RSP+0x20]`, `[RSP+0x28]` (호출 직전 기준).

### 로드 문맥(`ctx`) 칸 표 — 오프셋만, 이 요청서 범위에서 알게 된 것

| 칸 | 뜻 | 근거 |
|---|---|---|
| `+0x00` | 이름 `UNICODE_STRING` | exe 길 `LdrpInitializeProcess +0x133b` 가 16바이트를 쓰고, 같은 값이 exe 항목 `FullDllName(+0x48)` 에도 들어감(`+0x13b9`). 의존 DLL 길은 `LdrpLoadDependentModuleInternal +0x1121~+0x1150` 이 새 문맥에 이름을 복사 |
| `+0x10` | 경로 구조체 포인터 (`+0x18` 이 항목 `ImplicitPathOptions` 로 복사됨) | `LdrpAllocateModuleEntry +0x86~+0x8d` |
| `+0x20` | 플래그. `0x800000`·`0x2000000`·비트 0·`0x200`·`0x8000`·`4`·`0x40`·비트 27 를 코드가 읽거나 씀 | `MapAndSnap +0x33`, `+0xd6`; `Allocate +0x107~+0x129`; exe 초기값 `0x200` (`LdrpInitializeProcess +0x1328`) |
| `+0x28` | `NTSTATUS` 를 쓸 곳을 가리키는 포인터 | `MapAndSnap +0x6d`, `+0x417` (`*(ctx+0x28) = 상태`); `MapDllWithSectionHandle +0x1c8` 가 그걸 읽어 반환 |
| **`+0x30`** | **부모 항목 (이 DLL 을 import 한 쪽의 `_LDR_DATA_TABLE_ENTRY*`). exe·`LdrLoadDll` 길은 0** | 아래 질문 2 근거 |
| `+0x38` | **이 문맥의 모듈 항목** (`Allocate +0x96` 이 씀) | |
| `+0x50` | (`LdrpLoadDependentModuleInternal +0x1617` 이 자식 항목을 써 넣는 칸) | 이번엔 줄만 봄 `[추측]` |
| `+0x58` | **자식 항목 칸 배열** (import 서술자 수 × 8바이트, `MapAndSnap +0x17b` 가 만듦) | |
| `+0x60` | 핫패치 DLL 항목 결과 (`LdrpLoadDependentModuleW` 에 `ctx+0x60` 주소를 넘김) | `+0x5c2~+0x5d3` |
| `+0x68` | import 서술자 수 (끝 표시 아닌 것 전부) | `+0x19d` |
| `+0x6c` | **아직 안 끝난 자식 수 + 1** (아래 질문 4) | `+0x199`, `+0x46d~+0x474`; `LdrpLoadDependentModuleInternal +0x12df~+0x12f7` 이 줄임 |
| `+0x70` / `+0x78` | **IAT(import 주소 표) 시작 / 크기** | `LdrpPrepareImportAddressTableForSnap` (`.c`) |
| `+0x88` | import 서술자 배열 포인터 | `MapAndSnap +0x1a1` |
| `+0x90` | `NtProtectVirtualMemory` 가 돌려준 원래 보호값 | `Prepare...` (`.c`) |
| `+0xb8` | -1 로 시작 (`Internal +0x1113`, `FindOrPrepare +0x435`) | 뜻 모름 |

## 질문별 답 [정적]

### 1. `LdrpMapAndSnapDependency` 의 순서

근거: `ms/disassemble/ntdll/LdrpMapAndSnapDependency.asm`, 구조 확인용 `ms/decomp/ntdll/LdrpMapAndSnapDependency.c`.

**입구에서 꺼내는 것** (`RCX` = 로드 문맥 `ctx`, 이 함수는 반환값이 없고 결과는 `*(ctx+0x28)` 으로 나간다):
- `+0x2c` `R13 = ctx+0x38` (이 문맥의 모듈 항목 = "지금 snap 할 모듈"). 이후 `[R13+0x30]` 이 `DllBase` (import 서술자 읽을 때 RVA 에 더함).
- `+0x33` `ctx+0x20 & 0x800000`, `+0x51` `R13->LoadReason(+0x10c) == 9`(`PatchImage`).
- exe 길 인자 = `LdrpInitializeProcess [RSP+0x2b0]` (sast/14). 그 문맥은 `+0x105 memset(.., 0, 0xd0)` 로 0 으로 먼저 채워지고(`+0x133b~+0x1349` 가 이름·경로·`0x200`·상태 포인터만 채움), `+0x1359 LdrpAllocateModuleEntry` 가 `+0x38`(항목)을 채운다. 그래서 exe 문맥의 **`+0x30` 은 0** 이다.

**순서 (시간 순, 호출 23곳 + 호출이 아닌 중요한 줄)**

1. `+0x63 LdrpFindDllActivationContext(항목)` — **건너뛰는 조건**: `ctx+0x20 & 0x800000` 이 켜져 있거나 항목 `LoadReason == 9`. 아니면 부른다. 음수면 `*(ctx+0x28) = 상태` 쓰고 `+0x7e` 쿠키 검사 뒤 끝 (`+0x69~+0x94`). (본문: 매니페스트 찾기 프로버 루틴을 부르고 `항목+0x88` 에 활성화 컨텍스트를 넣음 `.c`)
2. `+0x9e LdrpPrepareImportAddressTableForSnap(ctx)` — 음수면 위와 같이 끝. `.c` 에서 읽은 일: `RtlImageDirectoryEntryToData(DllBase, 1, 0xc)` 로 **IAT 디렉터리(12번)** 를 `ctx+0x70/+0x78` 에 넣음. 없으면 import 디렉터리가 든 구역 전체를 IAT 로 잡음. `ctx+0x20 & 0x800000` 이 꺼져 있으면 `NtProtectVirtualMemory(-1, IAT, 크기, 4, &ctx+0x90)` 로 **IAT 를 쓰기 가능으로 바꾸고** 한 쪽(0x1000)마다 자기 값을 다시 써 본다(`*p = *p`). → "snap(주소 채우기) 전에 표를 쓸 수 있게 해 둔다" 가 코드에 쓰여 있다.
3. `+0xaa` `LdrpIsHotPatchingEnabled`(`ntdll+0x1d26f0`) 가 0 이면 건너뜀. 켜져 있고 `ctx+0x38 ≠ 0` 이면 **`+0x10e LdrpQueryCurrentPatch(DllBase, &패치있음바이트)`** (call `+0x109`). 음수면 실패 끝. 바이트(`DIL`)가 1 이면 "핫패치 DLL 이 따로 있다" 갈래가 켜진다 (`LdrpQueryCurrentPatch.c`: `NtManageHotPatch(8,..)` 결과가 `0xc0000023` 일 때만 1 `[정적]`; "이 DLL 에 패치가 걸려 있다" 라는 뜻은 `[추측]`).
4. `+0xb8` `ctx+0x70(IAT) == 0` 이고 `DIL == 0` 이면 **import 처리 전체를 건너뛰고 7번(끝맺음)** 으로 (`+0xbc JZ +0x630`).
5. 아니면 `+0xd2 LdrpShouldModuleImportBeRedirected(항목)` → 참이면 `ctx+0x20 |= 0x2000000` (`+0xd6`). 본문(`.c`): `LdrpRedirectionModule`(`ntdll+0x1d2600`)이 0 이거나 이 항목이면 거짓, 아니면 PEB `+3` 비트 `0x10` 에 따라 항목 `Flags` 비트 0, 또는 `LdrpRedirectionCalloutFunc` 호출(0 이면 참).
6. `+0xe5 LdrpGetImportDescriptorForSnap(ctx)` → `R14` = import 서술자 배열 (`RtlImageDirectoryEntryToData(DllBase, 1, 1)`). `.c`: exe 항목이고 전역 `DAT_1801ea4f8` 의 비트 44~45 가 1 일 때만 변조 검사(`LdrpCheckPagesForTampering`)와 깨끗한 보기 다시 맵(`LdrpMapCleanModuleView`)을 한다 `[정적: 조건 이름은 모름]`.
7. **세는 고리 `+0x122 ~ +0x14d`** (호출 없음): 서술자(0x14 바이트씩, `+0xc` 이름 RVA, `+0x10` FirstThunk)를 이름·FirstThunk 가 0 이 아닌 동안 돌며 `EBX++` (서술자 수), IAT 첫 칸 `[DllBase+FirstThunk]` 가 0 이 아니면 `R15D++`(실제 올릴 import 수). `R15D == 0` 이고 `DIL == 0` 이면 **8번 건너뛰고 끝맺음으로** (`+0x157 JZ +0x18f → +0x192 JZ +0x51d`).
8. `+0x17b RtlAllocateHeap(LdrpHeap, NtdllBaseTag+0x180000 | 8, EBX*8)` → `ctx+0x58` (자식 항목 칸 배열). 실패면 상태 `0xc0000017`(`STATUS_NO_MEMORY`) 로 끝. 이어 `ctx+0x6c = R15D+1` (핫패치면 +2), `ctx+0x68 = EBX`, `ctx+0x88 = 서술자`.
9. **import 하나마다 도는 고리 `+0x1d0 ~ +0x3a3`** (서술자 순서대로, 칸 번호 `R12D` 0 부터, **IAT 첫 칸이 0 이어서 건너뛴 서술자도 번호는 올라간다**):
   - 이름 RVA 또는 FirstThunk 가 0 이면 고리 끝 (`+0x1d6`, `+0x1e1 → +0x449`). IAT 첫 칸이 0 이면 이 바퀴 건너뛰고 다음 서술자 (`+0x1f0 JZ +0x435`).
   - `+0x213 strlen(DllBase + 이름RVA)`. `0xfffe` 보다 길면 상태 `0xc0000106`(`STATUS_NAME_TOO_LONG`) 로 고리 탈출.
   - `+0x258 memset`(지역 문자열 버퍼 0xfe 바이트). 이름을 **ANSI→UTF-16** 으로 바꾼다: 크기 계산(`GlobalRtlNlsState` 의 코드페이지가 `0xfde9`(UTF-8)면 `+0x3ce RtlUTF8ToUnicodeN`, 아니면 바이트 표 검사), 지역 버퍼(0x100)가 모자라면 `+0x541 RtlpCtAllocateMemory`/`+0x524 NtdllpReallocateStringRoutine`/`+0x567 RtlCopyMemory`, 변환은 `+0x315 RtlAnsiStringToUnicodeString`. (잡일: 이 묶음 호출 6곳.)
   - **`+0x34e LdrpLoadDependentModuleInternal(&이름, ctx, R13, 0, &ctx.58[번호], &[RSP+0x48])`** — 6개 인자 (표는 질문 2). **조건 없이 매 바퀴(IAT 첫 칸이 0 이 아닌 서술자) 직접 부른다.**
   - 음수면 `*칸 = 0`, `*(ctx+0x28) = 상태` 쓰고 (`+0x410~+0x417`) 문자열 정리 후 **고리 탈출**(`+0x389 JS +0x449`).
   - 성공이면 `+0x36e` (큰 버퍼였다면 해제 `RtlDeleteBoundaryDescriptor` 별칭 = 문자열 해제 함수 이름이 9개 겹침, sast/15) 하고 다음 서술자.
10. 고리 끝 (`+0x428`): `DIL ≠ 0` 이면 **`+0x5d8 LdrpLoadDependentModuleW(&임시, ctx, R13, ctx+0x60, &임시)`** — 핫패치 DLL 을 올리는 갈래. 음수면 `+0x604 LdrpLogEtwHotPatchStatus(...)` 로그만 하고 계속(결과 상태는 이미 덮어써진 채 `ESI`). **조건: `DIL`(위 3번) 하나뿐.**
11. `+0x451 RtlFreeHeap(LdrpHeap, 0, 임시)` — `[RSP+0x48]` 이 0 이 아니면 (`Internal` 이 6번째 인자로 돌려준 임시 버퍼 `[추측]`).
12. **`+0x461 / +0x47d` 락 짝** (`LdrpModuleDatatableLock` 독점): 상태가 0 이상일 때만, `ctx+0x6c` 를 1 줄이고(`+0x46d~+0x474`) 풀기. **이 락 안에서 하는 일은 그것 하나다** (`R15D` = 줄인 뒤 값). → `+0x6c` 는 "자식 수 + 1" 에서 **이 함수 자신의 몫 1** 을 놓는 것으로 읽힌다 `[추측: 값 이름만]`.
13. **끝맺음 `+0x47d` 뒤** (질문 4 에서 다시):
    - `ctx+0x70(IAT) == 0` → 항목 `DdagNode(+0x98)->State(+0x38) = 5`(`Snapped`) 만 쓰고 끝 (`+0x5ae`). `LdrpSnapModule` 안 부름.
    - 아니고 `R15D ≠ 0` (남은 자식이 있다) → 아무것도 안 하고 끝 (`+0x49a JNZ +0x564`).
    - 아니고 `R15D == 0` → `State = 4`(`Snapping`, `+0x4ac`) 쓰고,
      - `ctx+0x30 == 0` 이면 **`+0x4c2 LdrpSnapModule(ctx)`**,
      - `ctx+0x30 ≠ 0` 이면 **`+0x4f7 LdrpQueueWork(ctx)`**.
    - 결과 상태가 음수면 `*(ctx+0x28)` 에 쓰고 끝, 아니면 `+0x7e` 쿠키 검사 뒤 `RET`.
14. 쿠키 검사 `+0x7e` 한 곳, 잡일 합계: 문자열 변환 6, 힙 해제 1, 쿠키 1, 로그 1.

**짧은 트리** (`ctx` = 인자, `E` = `ctx+0x38` 항목, 괄호는 ret 자리)

```
LdrpMapAndSnapDependency(ctx)
├─ [flags&0x800000 또는 E.LoadReason==9 이면 건너뜀]  (+0x63) LdrpFindDllActivationContext(E)
├─ (+0x9e) LdrpPrepareImportAddressTableForSnap(ctx)      IAT 를 ctx+0x70 에, 쓰기 가능으로
├─ [LdrpIsHotPatchingEnabled 켜졌을 때만] (+0x10e) LdrpQueryCurrentPatch(E.DllBase, &DIL)
├─ [IAT==0 && !DIL] → (끝맺음 5) 으로 건너뜀
├─ (+0xd2) LdrpShouldModuleImportBeRedirected(E)  → 참이면 flags |= 0x2000000
├─ (+0xe5) LdrpGetImportDescriptorForSnap(ctx)  → R14
├─ 세는 고리 +0x122~+0x14d (EBX=서술자 수, R15D=IAT 첫 칸 ≠ 0 인 수)
├─ [R15D==0 && !DIL] → 끝맺음으로 건너뜀
├─ (+0x17b) RtlAllocateHeap → ctx+0x58 (칸 EBX 개);  ctx+0x6c = R15D+1
├─ ★ import 마다 도는 고리 +0x1d0~+0x3a3
│    ├─ 이름 변환 (strlen, memset, 필요하면 할당·복사, AnsiToUnicode)
│    ├─ (+0x34e) LdrpLoadDependentModuleInternal(이름, ctx, E, 0, &ctx.58[i], &임시)   ← 매 바퀴 무조건
│    └─ 실패면 *ctx.58[i]=0, *(ctx+0x28)=상태, 고리 탈출
├─ [DIL≠0] (+0x5d8) LdrpLoadDependentModuleW(.., ctx+0x60, ..)
├─ (+0x451) RtlFreeHeap(임시)
├─ (+0x461~+0x47d) 락 안에서 ctx+0x6c -= 1
└─ 끝맺음: IAT==0 → State=5
           R15D≠0 → 아무것도 안 함
           R15D==0 → State=4 → ctx+0x30==0 ? (+0x4c2) LdrpSnapModule(ctx) : (+0x4f7) LdrpQueueWork(ctx)
```

### 2. 그래프 간선("누가 누구에 기대나")을 적는 자리가 이 함수에 있나

- **답: 아니오** `[정적]`. `LdrpMapAndSnapDependency` 는 `DdagNode` 에 **딱 두 곳**만 쓴다 — `+0x38 State` (`+0x4ac` → 4, `+0x5ae` → 5). `Dependencies(+0x28)`·`IncomingDependencies(+0x30)`·`Modules(+0x00)` 에 쓰는 줄이 없다 (`.asm` 에서 `[R13+0x98]` 로 꺼낸 포인터에 쓰는 줄은 이 둘뿐). `LdrpAllocateModuleEntry` 도 이 두 칸에는 안 쓴다 (질문 5).
- **부모(기대는 쪽) 정보가 `LdrpLoadDependentModuleInternal` 로 가는 자리** `[정적: 이 함수의 호출 줄 `+0x33e~+0x349`]`:

| 인자 | 값 | 뜻 |
|---|---|---|
| 1 `RCX` | `&[RSP+0x70]` | **자식(import 되는 쪽) 이름** UTF-16 |
| 2 `RDX` | `ctx` (= `RBX`) | **부모(import 하는 쪽) 로드 문맥** — `+0x38` 부모 항목, `+0x58` 자식 칸 배열, `+0x6c` 남은 수 |
| 3 `R8` | `R13` (= `ctx+0x38`) | **부모 모듈 항목** (`LdrpLoadDependentModuleInternal` 안에서 새 자식 문맥의 `+0x30` 으로 복사됨, 아래) |
| 4 `R9D` | 0 | 올리는 이유 = `LoadReasonStaticDependency`(0). `Internal +0x1179` 가 새 항목 `LoadReason(+0x10c)` 에 그대로 씀 `[정적: 줄만 봄]` |
| 5 `[RSP+0x20]` | `&ctx.58[번호]` | **자식 항목을 돌려받는 칸** — 부모 문맥의 배열 |
| 6 `[RSP+0x28]` | `&[RSP+0x48]` | 임시 포인터 (끝에서 `RtlFreeHeap`) |

- **`ctx+0x30` = 부모 항목 이라는 근거 (정적)**:
  - `LdrpLoadDependentModuleInternal +0x1100~+0x1108` 에서 새 자식 문맥 `[RBX+0x30] = [RSP+0x130]` 이고 `[RSP+0x130]` 은 그 함수 입구에서 저장한 `R8` (세 번째 인자 = 부모 항목) `[Internal +0x30~+0x33]`.
  - `LdrpAllocateModuleEntry +0x9a~+0xbb`: `RSI = [ctx+0x30]` 이 0 이 아니면 `[RSI+0x88]`(= 항목 `EntryPointActivationContext`)을 꺼내 `RtlAddRefActivationContext` 하고 새 항목 `+0x88` 에 넣는다. → 부모의 활성화 컨텍스트를 물려받는다.
  - `LdrpMapDllWithSectionHandle +0x286~+0x28f`: `ctx+0x30 ≠ 0` 이면 `항목.ParentDllBase(+0xb8) = [ctx+0x30]->DllBase`. → **부모 쪽 `DllBase` 를 자식 항목에 적어 두는 줄이 `MapDllWithSectionHandle` 에 있다** (PDB 칸 이름 `ParentDllBase`).
  - `LdrpLoadDll +0xf6` 가 5번째 인자로 `0`(`XOR R12D` 한 값) 을 넘기고 `LdrpLoadDllInternal +0x1d7` → `FindOrPrepare` 새 문맥 `+0x30` 에 그대로 간다 → `LdrLoadDll` 길(kernel32·kernelbase 첫 로드)은 `+0x30 == 0`.
- **간선을 적는 줄은 이 두 함수에 없다** → Q12b 의 `LdrpLoadDependentModuleInternal` 안일 가능성 `[추측]`. 이번에 본 줄 단서:
  - `Internal +0x12df~+0x12f7`: 인자 2(부모 문맥)의 `+0x6c` 를 락 안에서 1 줄인다. 이미 0 이면 `+0x1606~+0x1634` 로 가서 부모 문맥 `+0x50 = 자식 항목`, `+0x6c = 1`, **부모 노드 `State = 3`(`WaitingForDependencies`)**, 반환 `0x103`(`STATUS_PENDING`).
  - `Internal +0x1164`: 새 항목을 인자 5(`&ctx.58[번호]`)에 저장.
  - 함수 이름에 "의존 관계를 더한다" 가 없다는 요청서 확인은 `_index.tsv` 로 다시 맞음 (`LdrpRecordModuleDependency`·`LdrpDependencyExist` 만).

### 3. 같이 올리기 vs 일감으로 넘기기

- **`LdrpLoadDependentModuleInternal` 과 `LdrpQueueWork` 는 같은 선택지가 아니다** `[정적]`. 둘이 "갈리는" 자리가 이 함수에 없다.
  - `LdrpLoadDependentModuleInternal`: import 서술자마다(IAT 첫 칸이 0 이 아닌 것) **언제나 직접** (`+0x34e`). 병렬 로딩 켜짐 여부를 읽는 줄이 이 함수 안에 **없다** (이 함수가 읽는 전역: `LdrpIsHotPatchingEnabled`, `NtdllBaseTag`, `LdrpHeap`, `GlobalRtlNlsState`, `LdrpModuleDatatableLock`, `LdrpImageEntry`, `__security_cookie` 뿐 — `LdrInitState`·`LdrpEnableParallelLoading` 결과는 안 읽는다).
  - `LdrpQueueWork`: **"자기 모듈 snap 을 일감으로 넘길까"** 에만 나온다 (`+0x4f7`). 조건은 질문 1 의 13번: **IAT 가 있고 + 남은 자식 수 `R15D == 0` + `ctx+0x30 ≠ 0`.** `ctx+0x30 == 0` 이면 같은 자리에서 `LdrpSnapModule` 을 직접 부른다 (`+0x4c2`).
- **kernel32 준비 때 vs exe import 때** (정적 조건까지):
  | 문맥 | `ctx+0x30` | 끝맺음에서 부르는 것 (남은 자식 0 일 때) |
  |---|---|---|
  | exe (`LdrpInitializeProcess +0x1aa4`) | 0 (memset 한 칸, `+0x105`) | `LdrpSnapModule` 직접 |
  | kernel32 (`LdrpLoadDll`→`FindOrPrepare` 문맥) | 0 (`LdrpLoadDll +0xf6`) | `LdrpSnapModule` 직접 |
  | kernel32 의 자식(kernelbase 등, `Internal` 이 만든 문맥) | kernel32 항목 | `LdrpQueueWork` |
  | exe 의 자식(dll1), 그 자식(dll2) | exe 항목 / dll1 항목 | `LdrpQueueWork` |
- **병렬 로딩 켜기(`LdrpInitializeProcess +0x1a42`) 앞뒤 차이는 이 함수 코드에는 없다.** 같은 코드가 kernel32 준비 때(켜기 앞 `+0x199d`)와 exe import 때(켜기 뒤 `+0x1aa4`)에 똑같이 돈다. 차이는 (a) 문맥 `+0x30` 값, (b) `LdrpQueueWork` 본문이 일감을 어디서 돌리나 — 이건 callee 본문이라 안 읽었다. 지도에서 `LdrpQueueWork +0x82` 가 `TpPostWork` 를 부른다는 것과 `LdrpProcessWork +0x131` 이 `LdrpSnapModule` 을 부른다는 것만 확인 (`_map` edges). → 병렬 로딩 전에 `QueueWork` 가 어떻게 되는지는 **Q47·Q20 (동적)**.
- `LdrpMapDllWithSectionHandle` 쪽도 병렬 여부를 읽지 않는다. (질문 6)

### 4. `LdrpSnapModule` 을 부르는 때

- **답: import 를 다 올린 뒤 한 번, 고리 밖** (`+0x4c2`). import 하나마다가 **아니다** `[정적]`.
- 조건 (전부): `ctx+0x70(IAT) ≠ 0`, 락 안에서 줄인 `ctx+0x6c` 값(`R15D`) == 0, `ctx+0x30 == 0`. 이 때 그 직전에 노드 `State = 4`(`Snapping`) 를 쓴다 (`+0x4ac`).
- `R15D ≠ 0` 이면 이 함수는 snap 도 일감 넣기도 안 하고 끝난다. 그 몫을 누가 이어받는지는 `LdrpLoadDependentModuleInternal +0x12df~+0x12f7` 이 부모 문맥 `+0x6c` 를 줄이는 것까지만 보였다 → "마지막으로 끝나는 자식 쪽 코드가 이어받나" 는 `[추측]`, Q12b.
- IAT 가 없으면 snap 대신 `State = 5` 를 바로 쓴다 (`+0x5ae`).
- 에러(자식이 음수)면 snap 도 일감도 없이 `*(ctx+0x28) = 상태`.

### 5. `LdrpAllocateModuleEntry`

근거: `ms/disassemble/ntdll/LdrpAllocateModuleEntry.asm` (이 함수 전체 0x19c 바이트).

- **답: 모듈 항목과 그래프 노드를 같이 만든다** `[정적]`.
  - `+0x2e` `RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 0x40000 | 8, 0x138)` → 항목 (`_LDR_DATA_TABLE_ENTRY` 크기 0x138 과 같다). 실패면 `0` 반환 (`+0x34 JZ`).
  - `+0x60` `RtlAllocateHeap(LdrpHeap, 같은 플래그, 0x50)` → 노드 (`_LDR_DDAG_NODE` 크기 0x50 과 같다). 실패면 `+0x15b RtlFreeHeap(항목)` 하고 `0` 반환 (`+0x14a`).
  - 플래그 `| 8` 은 0 으로 채워 달라는 비트로 읽힌다 `[추측: 공개 상수 이름]`. 이 함수가 안 쓰는 칸(노드 `State`, `Dependencies`, `IncomingDependencies` 등)은 0 이라는 결론이 이것에 기댄다.
- **처음 값**:
  | 대상 | 칸 | 값 |
  |---|---|---|
  | 항목 `+0x98 DdagNode` | | 새 노드 (`+0x73`, **ctx 가 NULL 이어도 먼저 씀**) |
  | 노드 `+0x00 Modules` | 목록 머리 | 항목 `+0xa0 NodeModuleLink` 하나만 든 원형 목록 (`+0xd4~+0xe5`) |
  | 항목 `+0x70 HashLinks` | | 자기 자신을 가리키는 빈 목록 (`+0xc9~+0xd1`) |
  | 항목 `+0x90 Lock` | | 0 |
  | 노드 `+0x18 LoadCount` | | **1** (`+0xf4`) |
  | 항목 `+0x114 ReferenceCount` | | **2** (`+0xfb`) |
  | 항목 `+0x6c ObsoleteLoadCount` | | **6** (word, `+0x130`) |
  | **노드 `+0x38 State`** | | **쓰지 않음 → 0 = `PlaceHolder`** `[정적: 쓰는 줄 없음; 0 은 위 `| 8` 에 기댄 추정]` |
  | 노드 `+0x28 Dependencies`, `+0x30 IncomingDependencies` | | **쓰는 줄 없음** (0 = 비어 있음) |
- **로드 문맥(`RCX`)이 NULL 이 아닐 때 (`+0x7a`)**:
  - `+0x7f` 항목 `+0xb0 LoadContext = ctx`, `+0x96` `ctx+0x38 = 항목` (서로 가리키기).
  - `+0x8d` 항목 `+0x110 ImplicitPathOptions = [[ctx+0x10]+0x18]`.
  - 플래그 옮기기 (`EBP = ctx+0x20`): 비트 2 → 항목 `Flags |= 0x10000000`(`Redirected`), **비트 6 이 꺼져 있으면 `Flags |= 4`(`ImageDll`)**, 비트 27 → `Flags |= 0x4000000`(`ChpeEmulatorImage`).
  - `+0xc2 LdrInitState == 1` 일 때만: 현재 스레드 ID(TEB `+0x48`)가 `ntdll+0x1cc920` 칸(`LdrpDllNotificationLock+0x10`)과 **다르면** `Flags |= 0x20`(`ProcessStaticImport`, `+0x194`). `LdrInitState` 가 1 이 아니면 안 한다.
- **활성화 컨텍스트**:
  - `+0xbb` (`RtlAddRefActivationContext`): **`ctx+0x30`(부모 항목) 이 있고 부모 `+0x88` 이 0 이 아닐 때** 부모 것을 가져와 `AddRef` 하고 새 항목 `+0x88` 에 넣음. 부모가 있는데 `+0x88` 이 0 이면 아무것도 안 하고 `LoadReason` 도 0(`StaticDependency`) 그대로.
  - `+0x16b` (`RtlGetActiveActivationContext(&항목+0x88)`): **`ctx+0x30 == 0` 일 때** (exe·`LdrLoadDll` 길) 현재 활성 컨텍스트를 가져오고 `항목+0x10c LoadReason = 4`(`DynamicLoad`) 를 씀 (`+0x16b`). → exe 항목의 `LoadReason` 은 이 함수가 4 로 시작시킨다. `LdrpInitializeProcess` 에는 `+0x10c` 를 쓰는 줄이 없어서(`grep` 0건) 그대로 남는 것으로 읽힌다 `[정적]`; 실제 값은 동적.
- **caller 6곳 정리** (인자 `RCX` = 로드 문맥, 반환 `RAX` = 항목):
  - `LdrpLoadDependentModuleInternal +0x1157`: 직전 `+0x10db` (ret) 의 `RtlAllocateHeap(.., 이름길이+0xd2)` 로 **새 문맥**을 만들고, `+0x10ec~+0x1108` 에 `[+0x20]`(플래그, 비트 15 켬), `[+0x10]`, `[+0x28]`, `[+0x30] = 부모 항목`, `[+0xb8] = -1`, 이름 복사까지 한 뒤 호출. 결과를 인자 5(`&부모ctx.58[번호]`)에 저장(`+0x1164`). → **"새 DLL 이면 문맥+항목+노드를 만든다" 가 맞게 읽힌다.** 이 갈래가 "이미 올라온 DLL 이 아닐 때" 만 타는지는 본문 순서를 안 읽어서 `[추측]` (Q12b).
  - `LdrpFindOrPrepareLoadingModule +0x475`: 같은 모양 (`+0x3f3` 힙, `+0x404~+0x42a` 칸 채우기). 결과를 `*결과항목` 에 쓰고 `+0x487` 에서 `항목.LoadReason(+0x10c) = 인자 4`(EDI), 값이 9 이면 `HotPatchState(+0x130) = 1`.
  - `LdrpInitializeProcess +0x1359` (exe, 위 문맥 `[RSP+0x2b0]`) 와 `+0x1577` (`[RSP+0xb0]`, run19 AppVerifier 묶음) — 문맥은 스택에 있다.
  - `LdrpInitializeNtdllDataTableEntry +0x67`, `LdrpAllocatePlaceHolder +0xd0` — 인자 줄은 안 봤다.

### 6. test.exe → dll1 → dll2 를 이 함수 기준으로

- **exe 에 대해 한 번 불린다** `[정적]`: `LdrpInitializeProcess +0x1aa4 → +0x1aa9` (이미 있던 사실).
- **dll1 은 exe 고리의 한 바퀴에서 `LdrpLoadDependentModuleInternal(dll1 이름, exeCtx, exe 항목, 0, ..)` 로 올라간다** (exe import 가 dll1 하나이고 소스상 dll1 만 import 라서 고리 서술자 1개 `[소스]`; 다른 import 가 몇 개인지는 test.exe 의 import 표를 안 봤다).
- **dll1 이 올라오는 중에 같은 함수가 dll1 에 대해 다시 불리는 코드 길이 있다** `[정적: 지도 호출 사슬]`:
  - `MapAndSnap +0x34e → LdrpLoadDependentModuleInternal → (+0x1cae) LdrpProcessWork → (+0x6a) LdrpMapDllSearchPath → (+0x25c) LdrpMapDllNtFileName → (+0x28c) LdrpMapDllWithSectionHandle → (+0x1c8) LdrpMapAndSnapDependency`. KnownDll 길(`Internal +0x1792 LdrpLoadKnownDll`)도 `LdrpLoadKnownDll +0xf1` 로 같은 `MapDllWithSectionHandle` 에 닿는다 (sast/16).
  - **`LdrpMapDllWithSectionHandle` 이 이 함수를 부르는 조건** (`.asm` 읽음): 맵을 끝내고 `LdrpInsertDataTableEntry`/`LdrpInsertModuleToIndexLockHeld` 로 목록에 넣은 뒤(`+0x146~+0x151`, 같은 모듈이 이미 있으면 `+0x3be LdrpLoadContextReplaceModule` 로 가서 이 길은 안 탐), `LdrpCompleteMapModule`(`+0x24f`)·`LdrpProcessMappedModule`(`+0x26c`) 다음, 항목 `Flags` 비트 24(`CorILOnly`)가 **꺼져** 있으면 `+0x1c3 LdrpMapAndSnapDependency(ctx)`. 켜져 있으면 `+0x2d0 LdrpCorProcessImports` 로 간다. 이미지로 안 올린 DLL(`ImageDll` 꺼짐 + `ctx+0x20&0x800000` 꺼짐)은 `State = 9` 를 쓰고 이 함수를 안 부른다 (`+0x387~+0x3ac`).
  - 그래서 **dll1 에 대해 `LdrpMapAndSnapDependency(dll1 ctx)` 가 다시 불린다** → 그 고리에서 `dll2` 를 올리고 → `dll2` 에 대해 또 한 번 (`dll2` 의 import 가 이미 올라온 DLL 뿐이면 그 고리는 짧게 끝난다 `[추측: dll2 import 표를 안 봄]`). **dll2 가 dll1 의 고리에서 나오는지, 아니면 `LdrpQueueWork` 일감으로 다른 스레드에서 도는지**는 `LdrpLoadDependentModuleInternal`/`LdrpProcessWork` 본문 몫.
- 이야기 그림 (정적으로 말할 수 있는 만큼, 모르는 칸 `?`):

```
exeCtx(+0x30=0, E_exe)
LdrpMapAndSnapDependency(exeCtx)                     [LdrpInitializeProcess +0x1aa4]
  IAT 쓰기 가능으로; import 서술자 읽기
  고리 한 바퀴 (import = dll1.dll)
    LdrpLoadDependentModuleInternal("dll1.dll", exeCtx, E_exe, 0, &exeCtx.58[i])
      └ 새 문맥 dll1Ctx(+0x30=E_exe) 만들고 LdrpAllocateModuleEntry → E_dll1 + 노드(State 0)
      └ ? 간선을 적는 줄 (Q12b)
      └ 올리기 ... LdrpMapDllWithSectionHandle (+0x28f: E_dll1.ParentDllBase = E_exe.DllBase)
            └ LdrpMapAndSnapDependency(dll1Ctx)    [+0x1c3]  ← 같은 함수가 dll1 에 대해
                 고리 (import = dll2.dll)
                   LdrpLoadDependentModuleInternal("dll2.dll", dll1Ctx, E_dll1, 0, &dll1Ctx.58[j])
                     └ dll2Ctx(+0x30=E_dll1), E_dll2 + 노드
                     └ ... LdrpMapDllWithSectionHandle
                           └ LdrpMapAndSnapDependency(dll2Ctx)      [+0x1c3]
                                 (dll2 의 import 처리; 남은 수 0 이면 State=4)
                                 dll2Ctx.+0x30 ≠ 0 → LdrpQueueWork(dll2Ctx)   [+0x4f7]
                 (dll1 의 남은 수 = ? 가 0 이면) State=4 → dll1Ctx.+0x30 ≠ 0 → LdrpQueueWork(dll1Ctx)
  (exe 의 남은 수 = ? 가 0 이면) State=4 → exeCtx.+0x30 == 0 → LdrpSnapModule(exeCtx)   [+0x4c2]
```

- `?` 로 남긴 것: 남은 수 `ctx+0x6c` 가 각 시점에 0 인지 (자식이 `Internal` 안에서 이미 끝났다고 줄이는지), 일감이 어느 스레드에서 도는지.
- run8 `[관찰]` ("`LdrpInitializeProcess +0x1bb6` 에서 멈췄을 때 dll1·dll2 가 이미 올라와 있었다. 켜는 순서 dll2 → dll1") 과 **어긋나지 않는다**: 이 고리가 `+0x1aa9` 전에 끝나고, 그 뒤 `+0x1ab1 LdrpDrainWorkQueue(1)` (run19) 이 일감을 기다린다.

## 그림 2 `(정적)` 상자 후보 (2.5·2.6, 순서대로)

1. **`ntdll!LdrpMapAndSnapDependency` — import 서술자마다 `LdrpLoadDependentModuleInternal`** (`+0x34e`, 조건 없음), 끝에 한 번 `LdrpSnapModule`(`+0x4c2`, 문맥 `+0x30` 이 0 일 때) 또는 `LdrpQueueWork`(`+0x4f7`). 이유: 2.6 "import DLL 이 올라오는 순간" 칸에 "바깥 고리는 여기" 를 정적으로 박는다. 병렬 로딩 켜기 앞/뒤에 같은 코드가 돈다는 점도 한 줄.
2. **`ntdll!LdrpAllocateModuleEntry` — 모듈 항목(0x138) + 노드(0x50) 한 쌍, 노드 `State` 0, `LoadCount` 1, 활성화 컨텍스트는 부모 것을 물려받음** (`+0x2e`, `+0x60`). 이유: 2.5 "올리는 순간" 의 그래프 노드가 태어나는 자리. `LdrpLoadDependentModuleInternal +0x115c`, `FindOrPrepare +0x47a` 가 부름.
3. **`ntdll!LdrpMapAndSnapDependency +0x4ac` — 노드 `State = 4`(Snapping), 직전 조건 "남은 자식 수 == 0"** (또는 IAT 없으면 `+0x5ae` `State = 5`). 이유: run21 의 노드 상태 표(5~9) 앞쪽 4·5 가 어디서 쓰이는지 박는 자리. `State` 3 은 `Internal +0x162d` 단서 `[추측]` 으로만.

## 동적분석으로 확인해야 할 것

모두 `test.exe` 일 때만 멈추는 조건을 붙인다 (기존 방식). 오프셋은 `ntdll+rva`.

- **`LdrpIsHotPatchingEnabled`(`ntdll+0x1d26f0`) 와 `LdrpRedirectionModule`(`ntdll+0x1d2600`) 값** — `db ntdll+0x1d26f0 L1`, `dq ntdll+0x1d2600 L1`. 핫패치 갈래(`LdrpQueryCurrentPatch`, `LdrpLoadDependentModuleW`)와 리다이렉션 비트(`ctx+0x20 |= 0x2000000`)를 test.exe 가 타는지. 둘 다 0 이면 안 탄다. (새 번호 없이 Q46 곁에 붙임 제안.)
- **Q23 (dll1·dll2 가 올라오는 순간)와 합침 제안** — 같은 `LdrpMapAndSnapDependency` 가 몇 번, 어떤 문맥으로 불리는지:
  - 입구 `ntdll+0x210a0`: `du poi(@rcx+0x38)+0x58` (BaseDllName), `dq @rcx+0x30 L1` (부모 항목: exe 때 0, dll1 때 exe 항목, dll2 때 dll1 항목인가), `k` (재귀가 쌓이나, 일꾼 스레드에서 오나 `~`).
  - 고리 안 호출 직전 `ntdll+0x213e9` (call): `du poi(@rcx+8)` (import 이름), `dq @r8 L1` (부모 항목). 돌아온 자리 `ntdll+0x213ee`: `r eax` (상태).
- **끝맺음 갈림 (질문 3·4)** — `ntdll+0x2155d` (`LdrpSnapModule` call 직전)와 `ntdll+0x21592` (`LdrpQueueWork` call 직전) 중 어느 쪽이 exe/dll1/dll2/kernel32/kernelbase 에서 맞나. 둘 다 안 맞고 지나가면(`R15D ≠ 0`) 그 모듈의 `ctx+0x6c` 를 줄이기 전 값으로 본다: 멈춤점 `ntdll+0x21506` (`+0x466`, 락 잡은 직후, `rbx` = ctx): `dd @rbx+0x6c L1`.
- **Q47 (일꾼 스레드)와 Q20 (kernel32 준비)** — 위 `ntdll+0x21592` 에서 `~` 와 `LdrInitState` 값. kernel32 준비 때(병렬 켜기 앞)에도 `QueueWork` 가 불리는지가 핵심 (kernelbase 가 `+0x30 ≠ 0` 이라 불릴 것 `[추측]`).
- **Allocate 결과** — `LdrpAllocateModuleEntry` 의 `RET` 자리 `ntdll+0x6fbf8`: `r rax` (항목), `dd @rax+0x10c L1` (LoadReason), `dd poi(@rax+0x98)+0x38 L1` (State, 0 인가), `dd poi(@rax+0x98)+0x18 L1` (LoadCount, 1 인가). exe 항목(`ntdll+0x1d2828`)의 `LoadReason` 이 4 인지.
- **IAT 보호 바꾸기** — `ntdll+0xa5840` 앞뒤(`LdrpPrepareImportAddressTableForSnap`)에서 `NtProtectVirtualMemory` 를 부르는지: Q 새로 안 만들고 기존 Q23 의 `lm`/`!address` 비교에 붙일 수 있다 (`!address @@(poi(@rcx+0x70))`).

## Q12b (`LdrpLoadDependentModuleInternal`) 에 넘길 질문

1. 인자 2·3(부모 문맥·부모 항목)로 **자식 노드 `IncomingDependencies(+0x30)` / 부모 노드 `Dependencies(+0x28)` 에 쓰는 줄**이 있나. 있으면 칸 이름(`_LDRP_CSLIST.Tail`)과 자리. 없으면 누가 쓰나 (`LdrpCondenseGraph`·`SnapModule` 쪽?).
2. `+0x12df~+0x12f7` 가 부모 문맥 `+0x6c` 를 줄이는 조건과, `+0x1606~+0x1634` 의 "`+0x6c == 0` 이면 부모 노드 `State = 3`, 반환 `0x103`" 이 뜻하는 것 (자식이 아직 끝나지 않아서 부모가 기다리는 표시인가).
3. **"이미 올라온 DLL" / KnownDll(`+0x1792`) / 새로 올리기(`+0x10a3`~`+0x1157` 새 문맥)** 갈림 조건과, 그때 인자 5(`&ctx.58[번호]`)에 무엇이 저장되는지 (새 DLL 일 때만 저장되나).
4. `+0x182c LdrpQueueWork`·`+0x1cae LdrpProcessWork` 를 부르는 조건. 병렬 로딩 켜짐 여부를 읽는가.
5. 인자 4(`R9D` = LoadReason)가 쓰이는 자리(`+0x1179`)와 6번째 인자 `[RSP+0x48]` 임시 포인터가 무엇을 돌려주는가.

## Q14 판단

- **일부.** `LdrpInitializeProcess +0x1aa4` 가 부르는 `LdrpMapAndSnapDependency` 의 안쪽 순서(IAT 준비 → 서술자 세기 → import 마다 `LdrpLoadDependentModuleInternal` → 끝맺음)와 dll2 가 "dll1 에 대해 같은 함수가 다시 불려서" 나오는 코드 길은 정적으로 읽혔다. 그러나 dll1·dll2 를 **실제로 메모리에 올리고 간선을 적는 일**은 `LdrpLoadDependentModuleInternal` 안이라 Q12b 를 거쳐야 답이 난다.

## 판정

**분석성공.** 요청서 질문 1~6 에 `.asm` 줄 근거로 답했다. 남은 `[추측]`: 간선을 적는 자리(Q12b), 남은 자식 수가 0 이 되는 시점, 일감이 도는 스레드, 핫패치·리다이렉션 갈래를 test.exe 가 타는지 (동적).

# 14 proc-init-modules — 정적 분석 요청

- 날짜: 2026-10-22
- 스프린트: 02_sprint (예정 run19)
- 대기열: Q9b (`_workspace/plan.md`) — Q9 를 둘로 나눈 뒤쪽. 앞쪽(`+0x0 ~ +0x12dd`)은 sast/13·run18 에서 끝
- 관련: flow.md 2.4 `#### ntdll!LdrpInitializeProcess` 절 (뒤쪽은 `[공백: 정적으로 아직 안 봄, Q9b]`), 2.5·2.6·2.7·2.9, 3장 ntdll 일꾼, 4장 공백 / 이전 요청 sast/13 (같은 함수 앞쪽), sast/12

## 범위 (꼭 지킬 것)

- 대상은 `ntdll!LdrpInitializeProcess` 하나, **`+0x12de ~ 끝`(+0x1e46)** 구간. 예외 정리 조각 `seh:LdrpInitializeProcess$fin$0` 도 포함.
- depth 1: callee 의 **이름과 자리**만. callee 본문은 읽지 않는다 (다음 항목들 몫: `LdrpInitializeKernel32Functions` = Q10, 올리기·그래프 = Q11·Q12·Q14, `LdrpInitializeGraphRecurse` = Q15, `LdrpCallTlsInitializers` = Q16). 이름·인자로 짐작한 역할은 `[추측]`.
- 잡일(`memset`, `LdrpLogInternal`, `LdrpLogDllState`, `NtClose`, `RtlFreeAnsiString` 등)은 묶음 안 개수만.
- 앞쪽(sast/13) 결과는 다시 하지 않는다. 앞쪽 값이 뒤쪽 갈림을 정하면 "앞쪽 `+0x..` 에서 정한 값" 으로만 가리킨다.

## 알고 싶은 것

1. **뒤쪽의 큰 순서**: 호출 75곳을 하는 일별 **묶음 5 ~ 10개**로 시간 순서대로. sast/13 질문 2 와 같은 표 양식 (묶음 | 범위 | 요약 | 대표 호출 call/ret | 호출 수(잡일)). 이미 관찰된 세 점이 어느 묶음에 드는지 표에 표시:
   - `+0x19a2` (ret) `LdrpInitializeKernel32Functions` — kernel32 준비 `[관찰]` run7
   - `+0x1bbb` (ret, call `+0x1bb6`) `LdrpInitializeGraphRecurse` — import DLL 초기화 `[관찰]` run7·run8
   - `+0x1c99` (ret) `LdrpCallTlsInitializers` — exe TLS 콜백 `[관찰]` run7·run8
2. **exe 모듈 항목**: `LdrpAllocateModuleEntry` 를 두 번 (`+0x1359`, `+0x1577`), `LdrpInsertDataTableEntry`·`LdrpInsertModuleToIndex`·`LdrpProcessMappedModule` 도 두 번씩 부른다 (`+0x1450/+0x14cd/+0x14e9`, `+0x1625/+0x166b/+0x1679`). 두 묶음이 각각 **무엇의 항목**인가 (exe / 그 밖 — 예: WOW64·패치·.NET), 갈림길 조건. test.exe(x64 보통 exe) 는 어느 쪽을 타나 `[정적]` 조건까지.
3. **`LdrpInitializeGraphRecurse` 세 자리**: `+0x16e6`, `+0x193c`, `+0x1bb6`. 각 자리의 갈림 조건과 넘기는 첫 인자(어느 모듈 항목인가). test.exe 길에서 타는 것은 `+0x1bb6` 하나뿐인가?
4. **올리기가 시작되는 자리** (예/아니오 + 이름): 이 함수가 직접 부르는 것 중, 다른 DLL 을 메모리에 올리는 일로 이어질 **후보 호출**은 어느 것인가. 지도에서 본 후보: `+0x199d LdrpInitializeKernel32Functions`, `+0x1aa4 LdrpMapAndSnapDependency`, `+0x17c1 LdrpLoadWow64`, `+0x1a7f LdrpCorProcessImports`, `+0x1aed LdrpPrepareModuleForExecution`. 각 후보의 갈림 조건, 그리고 `+0x1aa4` 의 첫 인자가 exe 항목(`LdrpImageEntry`)인가. (callee 본문은 안 본다 — "여기서 내려간다"까지만. flow.md 2.6 `[추측]` "올리는 일은 `+0x12de` 뒤 `+0x1bb6` 앞" 과 맞나)
5. **병렬 로딩 (일꾼 스레드 연결)**: `+0x12de LdrpInitParallelLoadingSupport`, `+0x1a42 LdrpEnableParallelLoading`, `LdrpDrainWorkQueue` 4곳(`+0x1302`, `+0x19c1`, `+0x1aac`, `+0x1b8c`), `LdrpThreadTokenSetMainThreadToken`/`Unset` (`+0x1a89`/`+0x1abd`). 순서와 갈림 조건만. 일꾼 스레드가 생길 수 있는 가장 이른 자리가 이 함수 안에 보이나 (flow.md 3장 `[추측]` "일꾼은 그 뒤에").
6. **간접 호출 4곳의 대상 후보와 조건**:
   - `+0x17fc` (`indirect:?`) — 바로 앞 `+0x17ed` 에서 `g_LdrpWow64LdrpInitialize` 를 읽는다 (sast/12). WOW64 일 때만인가. `RCX`(`[RSP+0xe0]`, 로더 문맥) 처음 쓰는 `+0x17f4` 가 이 호출의 인자인가
   - `+0x1b59` (`cfg:Kernel32ThreadInitThunkFunction`) — 무엇을 부르나 (값을 쓰는 곳 `refs`)
   - `+0x1cdf` (`cfg:?`), `+0x1d16` (`cfg:[RDI+0x230]`) — `RDI` 가 무엇이고 `+0x230` 칸 이름 (PDB 타입)
7. **디버거 때문에 달라지는 길**: `BeingDebugged`(PEB+0x2) `+0x17d2`·`+0x1b35` → `LdrpDoDebuggerBreak` `+0x17d8`·`+0x1b3b`, `KUSER_SHARED_DATA+0x330` `+0x188c`·`+0x1cb0`, `NtGlobalFlag & 0x100` `+0x16b7`, `AVrfInitializeVerifier` `+0x1713`. 각 자리에서 갈라지는 내용. (CLAUDE.md "디버거가 붙어서 달라지는 점은 따로 적기")
8. **상태 표시와 끝**: `LdrInitState` 쓰기 3곳(`+0x17dd`, `+0x1a4c`, `+0x1b1e`) 의 값, `+0x1c94` TLS 뒤에 하는 일(`+0x1cf9 LdrUnloadDll`, `+0x1d99 RtlIsProcessorFeaturePresent`, `+0x1def SymCryptDetectCpuFeaturesByCpuid`, `+0x1e40 RtlRaiseStatus`)과 정상 반환 자리. 뒤쪽의 실패 길 개수와 대표 2 ~ 3개 조건.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpInitializeProcess$` | 1 | `+0x12de ~ 끝` 호출 목록·묶음 (`ms/disassemble/ntdll/LdrpInitializeProcess.asm` 이미 있음, O1a·O1b 뒤 추출 — sast/13 확인) |
| ntdll | 전역 `Kernel32ThreadInitThunkFunction`, `LdrInitState`, `LdrpImageEntry`, `g_LdrpWow64LdrpInitialize` | - | 질문 2·4·6·8 — 누가 읽고 쓰나 (`refs`, 이미 있는 `_refs/` 먼저 확인) |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpInitializeProcess` rva `0x8c404`, 크기 7750(0x1e46), 깊이 4, caller 1곳(`_LdrpInitialize`). 뒤쪽 호출 75줄 (경계 `+0x12de` 포함, sast/13·run18 에서 셈).
- 간접 호출 4곳 모두 뒤쪽: `+0x17fc indirect`, `+0x1b59 cfg:Kernel32ThreadInitThunkFunction`, `+0x1cdf cfg:?`, `+0x1d16 cfg:[RDI+0x230]`.
- 뒤쪽 호출 순서 (지도 그대로, 잡일 일부 생략):
  - `+0x12de LdrpInitParallelLoadingSupport` → `+0x1302 LdrpDrainWorkQueue` → `+0x1313 LdrpInitializeDllPath`
  - `+0x1359 LdrpAllocateModuleEntry` → `+0x1430 LdrpProtectAndRelocateImage` → `+0x1450 LdrpInsertDataTableEntry` → `+0x14cd LdrpInsertModuleToIndex` → `+0x14e9 LdrpProcessMappedModule` → `+0x1517 RtlpInitCurrentDir`
  - `+0x1577 LdrpAllocateModuleEntry` → `+0x1625 LdrpInsertDataTableEntry` → `+0x164f RtlImageNtHeaderEx` → `+0x166b LdrpInsertModuleToIndex` → `+0x1679 LdrpProcessMappedModule`
  - `+0x16e6 LdrpInitializeGraphRecurse` → `+0x1713 AVrfInitializeVerifier` → `+0x1753 LdrpDropLastInProgressCount` → `+0x1761 RtlInitializeProcessorFeaturesBitMap` → `NtClose` ×3
  - `+0x17c1 LdrpLoadWow64` → `+0x17d8 LdrpDoDebuggerBreak` → `+0x17fc indirect` → `+0x1806 LdrpInitializeCfgScpHelpers` → `+0x1827 LdrpAllocateSchedulerSharedData` → `+0x1837 LdrpAcquireSchedulerSharedDataSlot`
  - `+0x184d LdrpCorInitialize` → `+0x1867 LdrpCorValidateImage` → `+0x18cb LdrpInitializeTls` → `+0x1916 LdrpLogEtwHotPatchStatus` → `+0x193c LdrpInitializeGraphRecurse` → `+0x195e LdrpInitializeImportRedirection`
  - `+0x199d LdrpInitializeKernel32Functions` → `+0x19c1 LdrpDrainWorkQueue` → `+0x19c8 SbObtainTraceHandle` → `+0x19ef LdrpInitShimEngine` → `+0x1a42 LdrpEnableParallelLoading` → `+0x1a7f LdrpCorProcessImports`
  - `+0x1a89 LdrpThreadTokenSetMainThreadToken` → `+0x1aa4 LdrpMapAndSnapDependency` → `+0x1aac LdrpDrainWorkQueue` → `+0x1abd LdrpThreadTokenUnsetMainThreadToken` → `+0x1aed LdrpPrepareModuleForExecution` → `+0x1b24 LdrpInitializePatchData` → `+0x1b3b LdrpDoDebuggerBreak` → `+0x1b40 LdrpDropLastInProgressCount`
  - `+0x1b59 cfg:Kernel32ThreadInitThunkFunction` → `+0x1b72 LdrpInitializePerUserWindowsDirectory` → `+0x1b8c LdrpDrainWorkQueue` → `+0x1b91 LdrpAcquireLoaderLock` → **`+0x1bb6 LdrpInitializeGraphRecurse`** → `+0x1bc7 LdrpReleaseLoaderLock` → `+0x1be2 LdrpFreeLoadContextOfNode` → `+0x1bfb LdrpReleaseDllPath` → `+0x1c25 LdrpInitializeSmtDelayedSleep`
  - `+0x1c85 RtlActivateActivationContextUnsafeFast` → **`+0x1c94 LdrpCallTlsInitializers`** → `+0x1ca2 RtlDeactivateActivationContextUnsafeFast` → `+0x1cdf cfg:?` → `+0x1cf9 LdrUnloadDll` → `+0x1d16 cfg:[RDI+0x230]` → `NtClose` ×2 → `+0x1d99 RtlIsProcessorFeaturePresent` → `+0x1def SymCryptDetectCpuFeaturesByCpuid` → `+0x1e40 RtlRaiseStatus`
  - `seh:LdrpInitializeProcess$fin$0+0x10 RtlDeactivateActivationContextUnsafeFast`
- 눈에 띄는 것: `LdrpInitializeGraphRecurse` 가 3곳, `LdrpInitializeTls` (`+0x18cb`) 가 kernel32 준비(`+0x199d`)보다 앞, `LdrpDrainWorkQueue` 가 4곳.

## 이미 아는 것

- `[관찰]` (run7·run8) 메인 스레드에서 `+0x19a2` → `+0x1bbb` → `+0x1c99` 순서. `+0x1bb6` 에서 멈췄을 때 이미 dll1·dll2 가 메모리에 있었다 (`lm m dll*`, run8). dll1·dll2 는 `LdrpLoadDll` 을 타지 않았다.
- `[관찰]` (run7) `+0x19a2` 안에서 `LdrLoadDll` 두 번 (`LdrpInitializeKernel32Functions +0xc0`, `+0x156`), 그 안에서 KERNELBASE·KERNEL32 시작 함수가 불림.
- `[관찰]` (run8) exe TLS 콜백은 그래프 순회가 아니라 `LdrpInitializeProcess` 가 `+0x1c99` 에서 직접 부른다.
- `[정적]` (sast/13·run18) 앞쪽은 아무것도 메모리에 올리지 않는다. 앞쪽 끝에서 `PebLdr` 목록에는 ntdll 항목뿐 `[정적]` (exe 항목은 뒤쪽 `+0x1359` 에서 `LdrpImageEntry` 로). 앞쪽은 `RCX` 를 `[RSP+0xe0]` 에 저장만, `RDX`(ntdll 시작 주소)는 `RSI`·`[RSP+0xd8]`. `UseWOW64` = `TEB.WowTebOffset > 0` (`+0x5f2`).
- `[정적]` (sast/12) `LdrpDoDebuggerBreak` 앞 `[RDI+0x2]` 바이트 검사 (`+0x17d2`, `+0x1b35`). `g_LdrpWow64LdrpInitialize` 는 `LdrpLoadWow64 +0xe8` 이 칸 주소를 넘겨 채우고, 여기 `+0x17ed` 가 읽는다. `LdrInitState` 쓰기 `+0x17dd`, `+0x1a4c`, `+0x1b1e`.
- `[추측]` (flow.md 2.5·2.6) kernel32·dll1·dll2 를 올리면서 의존 관계 그래프를 만들고, `LdrpInitializeGraphRecurse` 가 그 그래프 순서대로 켠다.
- 이름 주의: `LdrpInitializeProcess` 이름은 겹침 없음 (sast/13). 라벨은 **ret 자리**로 적는다 (run18 `+0x1bb6` → `+0x1bbb` 정정). call 자리를 쓸 땐 "(call)" 을 붙인다.

## 받고 싶은 것

- 질문별 답 + 근거 (`LdrpInitializeProcess.asm` 의 `+0x..` 줄). 태그 `[정적]` / `[추측]`.
- 질문 1 은 **표 하나** (sast/13 과 같은 칸). flow-writer 가 2.4 에 그대로 옮길 수 있게.
- 그림 2 에 넣을 `(정적)` 상자 후보 3 ~ 5개 (관찰된 `(6)`~`(8)` 은 이미 있음 — 그 사이·앞에 들어갈 것, 이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 추천 (`ntdll+rva` = `0x8c404 + 오프셋`, 짧게. 대기열 맨 뒤에 붙인다). 이미 대기 중인 Q14·Q22·Q24·Q44 와 겹치면 그 번호를 적어 준다.
- Q10 ~ Q16 에 넘길 연결점 한 줄씩.
- 스크립트 문제가 보이면 고치지 말고 "스크립트 문제" 절에.

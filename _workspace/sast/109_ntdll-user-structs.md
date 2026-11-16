# 109 ntdll-user-structs — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q78e1** — Q78e (ntdll 앞쪽, 힙 빼고) 를 그 자리에서 나눔 (2026-11-15): **Q78e1 = 유저 구조체 칸** (PEB·`PebLdr`·모듈 항목·노드) · Q78e2 = ntdll `.data`/`.mrdata` 전역 `≤P1a` + 묶음 7 힙 뒤 부분 + 조건부 힙 전역 8 + `EnvironmentVersion` +2. 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntdll.dll (PDB GUID 확인). 커널 쪽 (`MmCreatePeb` 등) 은 이미 있는 `.asm` 읽기만.
- 관련 (**같은 방식으로** — run52·53·54 와 sast/46·47·48 결과서의 네 가지 가름, 셈 기준: 명시 0 저장·입력값 쓰기·같은 DWORD 되쓰기만 "0 을 씀", 몸통 0 채움만으로는 안 셈):
  - `runs/03_sprint/baggage.md` **7.2** `_PEB`·`_PEB_LDR_DATA`·`_LDR_DATA_TABLE_ENTRY`·`_LDR_DDAG_NODE` 목록 (아래 범위에 옮김), 3.1·3.4·3.5·3.6 (아는 칸 — 다시 보지 않음), 7.5 실마리.
  - sast/48 결과서 (TEB 를 채울 때 — 같은 "범위 밖 길 (다른 DLL)" 묶기 방식), sast/54·57·58·59·61·63·65·84·85·86 결과서 (PEB·모듈 항목 칸을 이미 닫은 바퀴들 — 남은 칸은 그 바퀴들이 "안 봄" 으로 남긴 것).
  - sast/103 결과서 (모듈 항목 `Flags` 비트 `0x20` 전수 — objdump 전수 방법).
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/108) 을 돌린다 — **ntoskrnl Ghidra 프로젝트는 열지 않는다.** 이미 있는 ntdll `.asm`·`.c` 는 다시 뽑지 않는다. ntdll 전체 쓰기 전수는 스크래치 `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/` 의 ntdll objdump 풀이·도우미 재사용 (없으면 다시 만듦). 구조체 칸은 포인터를 얻는 방식이 여럿이라 오프셋 훑기 한계를 결과서에 한 줄로.

## 범위 — 모르는 칸 약 92 (7.2 그대로)

- **`_PEB`** (63): `P1~P1a` 1 — CrossProcessFlags* · `P1a~P1b` 1 — PatchLoaderData (Q78f 몫이지만 같은 구조체라 같이) · `끝까지 0` 61 — ReadImageFileExecOptions, BeingDebugged, Padding0, SubSystemData, AtlThunkSListPtr, IFEOKey, CrossProcessFlags.ProcessInJob · ProcessUsingVEH · ProcessUsingVCH · ProcessUsingFTH · ProcessPreviouslyThrottled · ProcessCurrentlyThrottled · ProcessImagesHotPatched · ReservedBits0, Padding1, KernelCallbackTable, UserSharedInfoPtr, SystemReserved, AtlThunkSListPtr32, TlsExpansionCounter, Padding2, AnsiCodePageData, OemCodePageData, UnicodeCaseTableData, GdiSharedHandleTable, ProcessStarterHelper, GdiDCAttributeList, Padding3, Padding4, GdiHandleBuffer, PostProcessInitRoutine, Padding5, AppCompatFlags, AppCompatFlagsUser, AppCompatInfo, SparePointers, ChpeV2ProcessInfo, AppModelFeatureState, SpareUlongs, UnusedNlsField, WerRegistrationData, WerShipAssertPtr, EcCodeBitMap, pImageHeaderHash, TracingFlags (+ HeapTracingEnabled · CritSecTracingEnabled · LibLoaderTracingEnabled · SpareTracingBits), Padding6, WaitOnAddressHashTable, TelemetryCoverageHeader, CloudFileFlags, CloudFileDiagFlags, PlaceholderCompatibilityMode, PlaceholderCompatibilityModeReserved, LeapSecondFlags (+ SixtySecondEnabled · Reserved), NtGlobalFlag2, Win32ClientProcInfo
- **`_PEB_LDR_DATA`** (2): `끝까지 0` — SsHandle, EntryInProgress
- **`_LDR_DATA_TABLE_ENTRY`** (24): `P1~P1a` 3 — FlagGroup, NodeModuleLink, LoadTime · `P1a~P1b` 1 — SwitchBackContext (Q78f 몫이지만 같이) · `끝까지 0` 20 — Flags.PackagedBinary, MarkedForRemoval, TelemetryEntryProcessed, ShimDll, VerifierProvider, LoadInProgress, EntryProcessed, AuxIatCopyPrivate, ReservedFlags3, ProcessAttachFailed, DontRelocate, ChpeImage, ChpeEmulatorImage, ReservedFlags5, Redirected, ReservedFlags6, CompatDatabaseProcessed, Lock, SigningLevel, ActivePatchImageBase
- **`_LDR_DDAG_NODE`** (4): `P1a~P1b` 1 — PreorderNumber (Q78f 몫이지만 같이) · `끝까지 0` 3 — ServiceTagList, LoadWhileUnloadingCount, CondenseLink
- `*` 칸은 처음 값을 쓰는 줄만. 뒤에 바꾸는 쪽은 이름만.
- PEB 의 `끝까지 0` 중 GDI·user32·WER·AppModel·Cloud Files·CHPE·트레이싱처럼 **다른 DLL 이 나중에 쓰는 칸** 은 "범위 밖 길 (다른 DLL)" 로 묶어서 이름만 (어느 DLL 인지 짐작이면 `[추측]`). 요청 28 (run85·86 user32·gdi32) 결과서에 쓰는 쪽이 나온 칸 (`KernelCallbackTable`·`GdiSharedHandleTable`·PEB `+0xf8` 등) 은 그 근거를 옮긴다.

## 알고 싶은 것

1. **`P1~P1a` 4칸 (PEB `CrossProcessFlags`, 모듈 항목 `FlagGroup`·`NodeModuleLink`·`LoadTime`) 쓰는 줄** — 함수+오프셋, 값의 출처. `NodeModuleLink`·`LoadTime` 은 `LdrpAllocateModuleEntry`·`LdrpInsertDataTableEntry`·`LdrpCreateNodeForModule` 류일 것 `[추측]`.
2. **`P1a~P1b` 3칸 (PEB `PatchLoaderData`, 모듈 항목 `SwitchBackContext`, 노드 `PreorderNumber`)** — 쓰는 줄 (run66 이 `SwitchBackContext` 를 `LdrpSendPostSnapNotifications +0xa3 SbUpdateSwitchContextBasedOnDll` 로 짚음 — 확인), `PreorderNumber` 는 그래프 걷기 (`LdrpInitializeGraphRecurse`·`LdrpBuildDependencyGraph` 류).
3. **`끝까지 0` 약 85칸 가르기** — 네 가지 (0 을 씀 / 조건부 안 탐 + 조건 / 아무도 안 씀 (초기화 길 안) / 범위 밖 길에서 씀 `[추측]` — ntdll 범위 밖 · 다른 DLL · nt). 조건부 갈래는 조건과 test.exe 가 왜 안 타는지 (`IFEOKey`·`ReadImageFileExecOptions` 는 IFEO 키 없음, `BeingDebugged` 는 디버거 없음 (run44 `BeingDebugged 0` `[관찰]`), `AppCompatFlags*` 는 shim 엔진, `NtGlobalFlag2` 는 sast/107 의 `MmCreatePeb` 복사 · IFEO `GlobalFlag2` 등).
4. **간접 호출** — 이 칸들을 쓰는 간접 호출이 있으면 대상을 좁히고, 못 정하면 동적 질문으로. 없으면 "0개".
5. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). ② 줄임은 baggage 3장 그대로. 공용 부품은 이름만.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntdll | `LdrpAllocateModuleEntry`·`LdrpInsertDataTableEntry`·`LdrpCreateNodeForModule`(이름 확인)·`LdrpInitializeGraphRecurse`·`LdrpSendPostSnapNotifications`·`LdrpInitializeProcess`·`LdrpInitializeExecutionOptions` (대부분 있음 — 읽기만) | 0 | 질문 1·2·3 |
| ntdll | 칸 쓰기 전수 (objdump) — PEB·모듈 항목·노드 오프셋 | — | 질문 1·2·3 |
| ntdll | (필요하면) 찾은 줄이 든 함수 중 `.asm` 없는 것 | 0 | `^이름$` |
| ntoskrnl | `MmCreatePeb` (있음 — 읽기만, 프로젝트 열지 않음) | — | 질문 3 — PEB 칸 중 커널이 0 을 쓰는 것 |

- 칸 이름·오프셋·비트는 `pdb_struct.py ntdll.dll _PEB` 등 (또는 sast/38 `_baggage/struct_*.tsv`).

## 받고 싶은 것

- 결과서 `_workspace/sast/109_ntdll-user-structs_result.md`. sast/46·47·48 과 같은 꼴:
  - 칸 약 92 전부의 가름 표 (구조체 · 구간 · 칸 · 가름 · 쓰는 줄 · 경로)
  - 셈 요약 (가름별 수)
  - baggage.md 3.1·3.4·3.5·3.6 에 넣을 줄 (3장 양식)
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"** (Q78e2 로 넘길 전역이 보이면 표시).

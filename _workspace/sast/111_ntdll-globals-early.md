# 111 ntdll-globals-early — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q78e2 + Q78f** (둘 다 ntdll 전역 — 연달아 있음, 오케스트레이터 기본값으로 한 요청·한 바퀴). Q78e2 = Q78e 의 전역 몫 (`≤P1a`), Q78f = KERNELBASE·KERNEL32 구간 (`P1a~P1b`) 의 남은 ntdll 전역 3 (run51·run68 등이 나머지를 이미 닫음 — Q78f 줄). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntdll.dll (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` **7.3** (ntdll 전역 — 아래 범위에 옮김), 3.8·3.9 (아는 전역 — 다시 보지 않음), 7.5 실마리, 7.24 (`LdrpPolicyBits` 를 옮길지), 3.2 (`EnvironmentVersion` — 나머지 +2).
  - sast/40 결과서 (run46, 프로세스 힙 구간 — "보고도 닫지 않은 것" 11: `RtlpLowFragHeapGlobalFlags`·`RtlpLowFragHeapRandomData*`·`RtlpDisableHeapLookaside`·`RtlpLargestLfhBlock`·`RtlpHeapFailureInfo`·`RtlpDebugPageHeapTable`·`RtlpSecMemListHead` (`≤P1a`), `RtlpHpHeapFeatures`·`RtlpHpAppCompatFlags`·`RtlpGlobalTagHeap`·`g_RegInfo` (`끝까지 0`) — 그중 "조건부 힙 전역 8개" 와 "묶음 7 힙 뒤 부분 (`+0xa50 ~ +0xb7d`)" 는 그 결과서 표현 그대로 찾을 것).
  - sast/57·59·61·62·63·64·65 결과서 (같은 `≤P1a` 목록을 줄여 온 바퀴들 — 같은 방법), sast/109 결과서 "Q78e2 로 넘길 전역" (`LdrpAuditIntegrityContinuity`·`LdrpEnforceIntegrityContinuity` 읽는 줄 `LdrpMapDllNtFileName +0x141`, `LdrpModuleDatatableLock`, `LdrpHeap`, `g_pShimEngineModule` — 쓰는 줄 `LdrpInitShimEngine +0xf5`·`LdrpGetProcApphelpCheckModule +0x27e`).
  - **Q80 과 겹침**: Q80 은 같은 `≤P1a` 전역을 "ntdll 파일의 처음 값 vs P1a 사진 값" 으로 가름. 이번에 **그 대조도 같이 해서** (파일 값 = 사진 값이면 "처음 값 그대로" 후보) 코드 근거와 함께 적는다 — Q80 을 닫을지는 Q80 차례에 오케스트레이터가 판단.
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/110) 을 돌린다 — ntoskrnl 쪽은 건드리지 않는다. 이미 있는 ntdll `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`. 전역 쓰기 전수는 `run_ghidra.sh refs ntdll.dll <전역>` (`_refs/`) 또는 스크래치 objdump 풀이 (`/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/` — sast/100~109 가 만든 것 재사용).
- 사진 값: `ms/logs/04_baggage-read_<K1 ~ P2>.txt` (ntdll `.data`·`.mrdata` 덤프 줄), 파일 값은 `ms/bin/.../ntdll.dll` 섹션 바이트.

## 범위

- **`.data` `≤P1a` 57** (7.3 그대로): _pctype, _pwctype, __initiallocinfo, __lconv_c, ?g_SbTableEntry_VerifyVersionInfoSwitch_RTL@@…, ?g_SbTableEntry_HeapPaddingAndLFHSubsegmentCommitSwitch_RTL@@…, RtlpSecMemListHead, RtlpLockedSectionList, RtlpDebugPageHeapTable, NormalizationListHead, TppCleanupGroupMemberpNoPoolList, LdrpUserDllDirectories, LdrpSnapsLock, LdrpSnapsLock_DEBUG, LdrpLoaderLock_DEBUG, LdrpLoaderLock*, LdrpDllNotificationList, RtlCriticalSectionList*, LdrpDllNotificationLock_DEBUG, LdrpDllNotificationLock, ?g_SbCachedModuleTable_RTL@@…, ?dazSupported@?1??_control87@@9@9, _matherr_flag, __mb_cur_max, ShowSnaps, g_sqmSessionHandle, g_SxsMaxDeadActivationContexts, Utf8TableInfo, PossibleDeadlockReportThreshold, RtlpPtrTree, RtlpLargestLfhBlock, __ft_g_token, TelemetryEventThrottle, RtlpHeapFailureInfo, RtlpDiskSpeedPolicy, RtlpUnloadEventTraceExSize, RtlpUnloadEventTraceExNumber, LdrpQueryProcessModuleInformationLoopDetectorCount, ModuleNamePlaceholderBuffer, LdrpPolicyBits, SbDynamicContextDllCount*, RtlpFlsContext*, RtlCriticalSectionDebugSList*, LdrpThunkSignature*, LdrpInitOnceLoadAsDataCrits*, RtlpDisableHeapLookaside, RtlpLowFragHeapGlobalFlags, RtlpTimeoutDisable, RtlpTimeout, RtlpEnvironmentHashTable, LdrpHotPatchInitCompleteEvent, RtlpLowFragHeapRandomData*, RtlpHypervisorSharedUserVa, RtlpStaticDebugInfo*, LdrpEnclaveList, LdrpEnclaveListLock, Feature_KVS_BugFixes_55830752__private_featureState
- **`.mrdata` `≤P1a` 2**: __guard_check_icall_fptr, NtUserPfn (run85 실마리: 파일 처음 값 `UninitUser32Proc` 그대로, test.exe 에선 아무도 안 씀 — 7.5)
- **`.mrdata` `K1 이전` 2**: LdrpVectorHandlerList, __security_cookie_complement
- **`.data` `P1a~P1b` 3 (Q78f)**: RtlpSearchPathMode, LdrpStatistics, LoadAsDataCrits
- **그 밖 (Q78e2 줄)**: 묶음 7 힙 뒤 부분 (`+0xa50 ~ +0xb7d`) · 조건부 힙 전역 8개 (run46 넘김) — sast/40 결과서에서 정확한 이름 목록을 가져와 위 목록과 겹치면 한 번만 · 매개변수 `EnvironmentVersion` 나머지 +2 (run45 — `RtlpInitEnvironmentBlock` 의 +1 뒤, P1a~P2 사이 두 번 더 오름 — 누가 환경을 바꾸나: `RtlSetEnvironmentVariable` 류 caller 이름 · 모듈).
- `*` 칸은 처음 값을 쓰는 줄만. 뒤에 바꾸는 쪽은 이름만.

## 알고 싶은 것

1. **전역마다 가름** — 쓰는 줄이 있으면 (아는 칸) 함수+오프셋·값의 출처·단계 (②), 없으면 네 가지 (0 을 씀 / 조건부 안 탐 + 조건 / 아무도 안 씀 / 범위 밖 길에서 씀 `[추측]`). 파일 값으로 이미 정해진 것 (CRT 표 `_pctype`·`__lconv_c`, 스위치 표 `g_SbTableEntry_*`, `LdrpPolicyBits` 파일 `0x16`) 은 **"파일 처음 값 그대로 (쓰는 줄 없음 / 조건부)"** 로 가름하고 파일 값·사진 값을 나란히.
2. **`LdrpPolicyBits`** — 7.24 질문 (3.8 로 옮길지 — 쓰는 줄이 패키지 갈래뿐) 에 답 하나.
3. **`EnvironmentVersion` +2** — 누가 (어느 모듈·함수) 언제 (사진 구간) 환경을 바꾸나.
4. **간접 호출** — 쓰는 줄이 간접 호출 대상 안이면 좁히고, 못 정하면 동적으로.
5. **줄기와 경로** — baggage 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거).

## 받고 싶은 것

- 결과서 `_workspace/sast/111_ntdll-globals-early_result.md`. sast/57·59 결과서 꼴 + 가름 표:
  - 전역 약 64 + 그 밖 전부의 가름 표 (구역 · 구간 · 이름 · 가름 · 쓰는 줄 · 파일 값 · 사진 값 · 경로)
  - 셈 요약, Q80 에 넘길 판단 (파일 값 대조로 닫히는 것 수)
  - baggage.md 3.8·3.9·3.2 에 넣을 줄 (3장 양식)
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"** (Q78g2 로 넘길 `끝까지 0` 전역이 보이면 표시).

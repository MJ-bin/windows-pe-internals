# 110 kusd-boot — 정적 분석 요청

- 날짜: 2026-11-16
- 스프린트: 03_sprint (**짐 지도 바퀴**)
- 대기열: **Q78g1** — Q78g (KUSD 와 남은 전역) 를 그 자리에서 나눔 (2026-11-16): **Q78g1 = `_KUSER_SHARED_DATA` 102칸 (커널)** · Q78g2 = ntdll `.00cfg` 4 + 앞 조각들이 못 닫은 ntdll 전역 (`끝까지 0` `.data` 215 · `.mrdata` 22 등). Q78f (ntdll) 가 대기열에서 앞이지만 ntdll 이 sast/109 에 쓰이는 중이라 이 몫을 먼저 계산 (문서는 대기열 차례대로). 사용자 결정 2026-11-15 "남은 정적 분석 전부". **정적만.**
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` **3.7** (KUSD — 아는 칸 1 `ProcessorFeatures`, run70 sast/65 의 방법: 커널 전체에서 `MmWriteableSharedUserData` 포인터 + disp 모양 · 절대 주소 `0xFFFFF780000000xx` 모양으로 쓰는 줄 찾기), **7.2 KUSD 목록** (아래 범위), 7.5·3.7 실마리 (run89 `ImageNumberLow/High`·`ComPlusPackage`·`Cookie`, run91 `RNGSeedVersion`), 단계 `Ⓑ` (부팅, run48 — `ⒷA`·`ⒷK`·`ⒷS` 줄임은 baggage "쓰는 말" 그대로).
  - sast/65 결과서 (같은 방법), sast/42 (부팅 준비 갈래), sast/60 (부팅 위상).
  - run44 사진 값: `ms/logs/04_baggage-read_supp_<K1 ~ P2>.txt` (KUSD 8장 — 값 대조용).
- 주의: 다른 decompiler 가 같은 시간에 **ntdll** (sast/109) 을 돌린다 — ntdll 쪽은 건드리지 않는다. 이미 있는 ntoskrnl `.asm`·`.c` 는 다시 뽑지 않고 필요한 것만 `^이름$`. 커널 전체 쓰기 전수는 스크래치 `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-windows-pe-internals/c114e664-2e38-4d75-9d4f-5d47663f1011/scratchpad/` 의 `nt.dis` (sast/104 가 만든 objdump 풀이) 재사용 (없으면 다시 만듦).

## 범위 — KUSD 모르는 칸 102 (7.2 그대로)

- `K1 이전` (61): TickCountMultiplier, InterruptTime*, SystemTime*, TimeZoneBias, ImageNumberLow, ImageNumberHigh, NtSystemRoot, LargePageMinimum, RNGSeedVersion, TimeZoneBiasStamp, NtBuildNumber, NtProductType, ProductTypeIsValid, NativeProcessorArchitecture, NtMajorVersion, Reserved1, Reserved3, BootId, SuiteMask, KdDebuggerEnabled, MitigationPolicies (+ NXSupportPolicy · SEHValidationPolicy), CyclesPerYield, ActiveConsoleId, ComPlusPackage, LastSystemRITEventTickCount, NumberOfPhysicalPages, VirtualizationFlags, SharedDataFlags (+ DbgElevationEnabled · DbgVirtEnabled · DbgInstallerDetectEnabled · DbgMultiSessionSku), TestRetInstruction, QpcFrequency, FullNumberOfPhysicalPages, TickCount*, TickCountQuad*, ReservedTickCountOverlay*, Cookie, ConsoleSessionForegroundProcessId, TimeUpdateLock*, BaselineSystemTimeQpc*, BaselineInterruptTimeQpc*, QpcSystemTimeIncrement, QpcInterruptTimeIncrement, QpcSystemTimeIncrementShift, QpcInterruptTimeIncrementShift, UnparkedProcessorCount, TelemetryCoverageRound, LangGenerationCount, QpcBias, ActiveProcessorCount, ActiveGroupCount, QpcData, QpcBypassEnabled, TimeZoneBiasEffectiveStart, TimeZoneBiasEffectiveEnd, XState, FeatureConfigurationChangeStamp
- `끝까지 0` (41): TickCountLowDeprecated, MaxStackTraceDepth, CryptoExponent, TimeZoneId, AitSamplingValue, AppCompatFlag, GlobalValidationRunlevel, Reserved0, NtMinorVersion, TimeSlip, AlternativeArchitecture, SystemExpirationDate, MitigationPolicies.CurDirDevicesSkippedForDlls, MitigationPolicies.Reserved, DismountCount, SafeBootMode, Reserved12, SharedDataFlags.DbgErrorPortPresent · DbgLkgEnabled · DbgDynProcessorEnabled · DbgConsoleBrokerEnabled · … (7.2 의 나머지 그대로 — 결과서에 전부 적기)
- `*` 칸 (시각·틱 — 계속 바뀜, run44 "소음") 은 **처음 값을 쓰는 줄 + 계속 고치는 쪽 이름** (`KeUpdateSystemTime`·`KiUpdateTime` 류) 만.

## 알고 싶은 것

1. **`K1 이전` 61칸 쓰는 줄** — 칸마다 함수+오프셋 (부팅 위상 `Ⓑ`: `KiInitializeKernel`·`InitBootProcessor`·`Phase1InitializationDiscard`·`ExpInitializeExecutive`·`MmInitSystem`·`KeInitializeTimer`·`HalpTimer*`·`ExpInitSystemPhase*`·`CmpInitializeBoot*` 류 — 이름은 찾은 줄에서), 값의 출처 (레지스트리 / 로더 블록 `LOADER_PARAMETER_BLOCK` 칸 / CPU / 상수 / 계산). 부팅 뒤 (프로세스 생성 앞) 에 다시 쓰는 쪽이 있으면 이름만 (`ActiveConsoleId`·`ConsoleSessionForegroundProcessId`·`TimeZoneBias*` 는 세션·시간대 서비스 쪽일 수 있음 — 커널 안 syscall 처리기 이름까지).
2. **`끝까지 0` 41칸 가르기** — 네 가지 (0 을 씀 / 조건부 안 탐 + 조건 / 아무도 안 씀 / 범위 밖 길에서 씀 `[추측]`). `SafeBootMode` (안전 모드일 때만), `KdDebuggerEnabled` 옆 칸들, `AlternativeArchitecture`, `SystemExpirationDate` (평가판), `TimeSlip` 등 조건.
3. **쓰는 길이 커널 밖** (예: `NtSystemRoot` 는 로더 블록에서 커널이 복사, `ActiveConsoleId` 는 csrss/winlogon 이 syscall 로) 이면 "놓는 법 = syscall, 쓴 주인 = nt, 부르는 쪽 = ○○" 로 (run87 처럼).
4. **간접 호출** — 쓰는 줄이 간접 호출 대상 안이면 좁히고, 못 정하면 동적으로.
5. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 `Ⓑ` / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). 공용 부품은 이름만.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | KUSD 쓰기 전수 — `MmWriteableSharedUserData`·`0xFFFFF78000000000` 기준 (objdump) | — | 질문 1·2 |
| ntoskrnl | 찾은 줄이 든 함수 중 `.asm` 없는 것 | 0 | `^이름$` |
| ntoskrnl | `KiInitializeKernel`·`InitBootProcessor` (있음 — 읽기만) | 0 | 질문 1 |

- 칸 이름·오프셋은 `pdb_struct.py ntdll.dll _KUSER_SHARED_DATA` (또는 sast/38 `_baggage/struct__KUSER_SHARED_DATA.tsv`).
- 훑기 한계 (포인터를 레지스터에 담아 쓰는 꼴 — run70 이 짚음) 를 결과서에 한 줄로.

## 받고 싶은 것

- 결과서 `_workspace/sast/110_kusd-boot_result.md`. sast/65 결과서와 같은 꼴 + sast/46~48 가름 표:
  - 칸 102 전부의 가름 표 (구간 · 칸 · 가름 · 쓰는 줄 · 값의 출처 · 경로)
  - 셈 요약, run44 사진 값과 맞춘 것
  - baggage.md 3.7 에 넣을 줄 (3장 양식)
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (에뮬레이터는 KUSD 를 덤프에서 떠 오는지 — 모르면 "확인 필요").
- 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"**.

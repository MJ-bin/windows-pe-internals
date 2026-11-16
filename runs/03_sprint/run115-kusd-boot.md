# 2026-11-16 (작성일) run115 — KUSD 102칸 가름 (Q78g1, 정적만): `_KUSER_SHARED_DATA` 모르는 칸 102 → **0**. `K1 이전` 61 = 쓰는 줄 모두 찾음 (nt 가 부팅 때 직접 44 · nt 가 쓰지만 부르는 쪽이 부팅 뒤 다른 모듈·프로세스 9 · 시각 `*` 8), `끝까지 0` 41 = 0 을 씀 8 · 조건부 안 탐 11 · 아무도 안 씀 17 · 범위 밖 5. 24H2 의 KUSD 쓰기 길은 `MmWriteableSharedUserData` 포인터 **하나**, `SystemCall` (`+0x308`) 은 조건 둘 (`KiSystemCallSelector == 1` · `HvlEnlightenments` 비트 19) 이 안 맞아 0 (run105 정적 ② 닫힘), `ActiveConsoleId` 류는 csrss 시스템 콜이 아니라 **win32k 드라이버가 nt 공개 함수** 를 부름 (요청서 짐작 정정) → **유저 구조체 모름 0**

(글의 오프셋은 call 자리 · 쓰는 줄 자리. "호출 관계" 나무의 callee 앞 오프셋도 call 자리 — 부팅 길이라 콜스택으로 본 적이 없어서 결과서 꼴 그대로 둔다. ret 는 아래 "정적 — 호출 지점" 표.)

> **지도 범위**: 이 바퀴의 쓰는 줄은 거의 다 **흐름도 범위 앞** (부팅 `Ⓑ`, 프로세스 생성 전) 이다. 그림 1 의 `SYSCALL` 갈래 꼬리표에 한 마디만 보탰고 새 노드는 없다. 글은 flow.md 기준 · 2.2 스텁 절 · 2.3 KUSD 절 (부팅 사슬 나무에 줄 보탬 + 새 소절) · 3장 bcryptprimitives 절 · 4장 · 5장, 짐 지도는 쓰는 말 · 1장 · 2장 · 2.1 · 3장 머리 · 3장 줄임 표 · **3.7** · 4장 짝 KUSD 줄 · 5장 `Ⓑ` · 7장 머리 · 7.1 · 7.2 · 7.5 · 7.29 · 7.37 · 새 7.46 · 8장을 고쳤다.

## 요약 표 (102칸 → ?)

| 구간 | 칸 | nt 가 부팅 때 직접 | nt 가 쓰고 부르는 쪽이 부팅 뒤 다른 모듈·프로세스 | 시각 `*` | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 | 범위 밖 길 `[추측]` | 모름 |
|---|---|---|---|---|---|---|---|---|---|
| `K1 이전` | 61 | 44 | 9 | 8 | — | — | — | — | 0 |
| `끝까지 0` | 41 | — | — | — | 8 | 11 | 17 | 5 | 0 |
| **합** | **102** | **44** | **9** | **8** | **8** | **11** | **17** | **5** | **0** |

- 짐 지도 셈 (run49 · sast/46 방식 — "0 을 씀" · "조건부 안 탐" 은 쓰는 줄이 있어 아는 칸):
  - `_KUSER_SHARED_DATA` 103칸 = 아는 칸 **81** (`ProcessorFeatures` 1 (run70) + `K1 이전` 61 + 0 을 씀 8 + 조건부 안 탐 11 — 모두 정확, 짐작 0) + 아무도 안 씀 **17** + 범위 밖 **5** + 모름 **0**.
  - 쓰는 줄은 모두 nt 안이라 2장 "nt 가 씀" 1 → **81**. 부르는 쪽이 win32k · cng · condrv · kdnet 같은 다른 모듈이어도 nt 로 센다 (run51 "syscall (다른 프로세스·드라이버가 부름)" · run47 "`nt (부르는 쪽 다른 드라이버)`" 규칙). "다른 DLL" 열은 그대로 0.
  - 구조체 21개 아는 칸 858 → **938** (정확 827 → **907** · 짐작 31) · 아무도 안 씀 185 → **202** · 범위 밖 262 → **267** · 모름 126 → **24** (남은 건 커널 `_EPROCESS.SharedCommitCharge` 1 + VAD 칸 안 읽음 23).
  - 아는 칸 중 `끝까지 0` (7.1): 362 → **381** (0 을 씀 8 + 조건부 11). 847 − 0 − 381 = 466 (아무도 안 씀 · 범위 밖 — run115 전 444 + 22).
  - 짐 지도 3.7 줄 5 → **36** (새 31 — 한 줄이 여러 칸을 묶음, 고친 줄 1).
- "직접 nt 44" 는 **부팅 때 첫 쓰는 줄이 nt 안** 이라는 뜻이다. 그중 몇 칸 (`NtBuildNumber` · `TimeZoneBiasStamp` · `LangGenerationCount` · `ComPlusPackage` · `KdDebuggerEnabled` · `SharedDataFlags`) 은 부팅 뒤에도 다시 쓰는 줄이 있다 (아래 표에 같이).

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntoskrnl pdb `ntkrnlmp.pdb` `C29EBFB06B78B3C020DCA66D99713F9E1`** (`ms/sym/ntoskrnl.exe.pdb.info.txt` — 기준과 같음, 결과서). 다시 analyze 안 함 (`.gpr` · `_index.tsv` · analyze 로그 `SetPdb:` 있음).
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31` (ret `+0xc36`) → `PspAllocateProcess` · `PspInsertThread +0x802` (ret `+0x807`) → `PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다. 쓰는 줄 131곳 (겹치는 칸에 여러 번 나와 대조는 144번) 을 새 `.asm` 줄과 하나씩 대조 — 144/144 일치 (objdump 오프셋 = Ghidra `.asm` 오프셋).
- 사진 값 대조: run44 덤프 읽기 보충 로그 `ms/logs/04_baggage-read_supp_<K1 ~ P2>.txt` 의 `==== 13s KUSD_KERNEL` 구획 (`dt nt!_KUSER_SHARED_DATA fffff78000000000`, `:182 ~ :285`). 8장 모두 같은 값 (시각 칸 8 빼고). 새 동적 실행은 없다.
- ntdll 은 이번에 안 건드렸다 (그때 sast/109 가 돌던 중 — 결과서). PE_emulator 줄을 쓰려고 있던 ntdll `.asm` 을 **읽기만** 했다.

## 목적

- 짐 지도 7.2 `_KUSER_SHARED_DATA` 모르는 칸 102 (`K1 이전` 61 · `끝까지 0` 41) 의 **쓰는 줄 · 값의 출처 · 지도 경로** (요청서 질문 1 · 5).
- `끝까지 0` 41 을 넷으로 가르기: 0 을 씀 / 조건부 안 탐 (+ 조건) / 아무도 안 씀 / 범위 밖 길 (질문 2).
- 쓰는 길이 커널 밖에서 시작하면 "놓는 법 · 쓴 주인 · 부르는 쪽" (질문 3), 간접 호출이면 좁히기 (질문 4).

## 방법

- 정적: `_workspace/sast/110_kusd-boot.md` — ntoskrnl, 모두 depth 0 `^이름$`.
  - 새로 뽑음 **61개** (한 번에): 쓰는 줄이 든 함수 44 (`InitBootProcessor` 처럼 이미 있던 것 빼고 — `RtlSetSystemGlobalData` · `RtlSetActiveConsoleId` · `KiSetupTimeIncrement` · `KiUpdateProcessorCount` · `IopStoreBootDriveLetter` · `PspNotifyThreadCreation` 등) + 값의 출처 · 조건 · 고치는 쪽을 읽으려고 17 (`KeInitializeClock` · `HalpTimerConfigureQpcBypass` · `KiParkCurrentProcessor` · `MiInitializeSharedUserData` 등). 로그 `SCRIPT ERROR` 0, `_calls.tsv` 9,193 → 10,163줄 (이전 줄 안 사라짐).
  - 읽기만: `InitBootProcessor` · `KiInitializeKernel` · `Phase1InitializationDiscard` · `PsBootPhaseComplete` · `PspInitPhase2` · `PspNotifyThreadCreation` · `MiInitSystem` · `KiInitializeNxSupportDiscard` · `KiSetFeatureBits` · `KiUpdateTime`.
  - 훑기 (결과서 스크래치, 커밋 안 함): 커널 코드 구역 17개 전부의 `objdump` 풀이 (`nt.dis`, sast/104 것 재사용) 에서 **KUSD 를 가리키는 레지스터** (`MmWriteableSharedUserData` 를 읽은 레지스터 · `movabs 0xfffff780000000xx` 를 담은 레지스터 — 복사 · `lea` · `add` 를 따라감) 로 만든 `[칸]` 이 **명령의 목적지** 인 줄 (`kscan.py`). 포인터를 메모리에 넣은 13곳 · 칸 주소를 인자로 넘긴 `call` 86곳은 손으로 가렸다. 다른 모듈 (win32kbase · win32kfull · condrv) 은 PDB 로 **IAT 호출 줄 → 함수 이름** 까지만 (`iatsites.py`), PDB 없는 cng · kdnet · kdcom 은 rva 만.
  - 한계 (결과서 "훑기 한계"): 레지스터 추적이라 분기 합치기 · 호출 뒤 휘발 레지스터를 안 따라가 **오탐 · 누락 가능**. 서버 실로 (컨테이너) 의 KUSD 사본 (`PspSiloInitializeUserSharedData`) 은 안 따라갔다. nt 밖 모듈 안에서 직접 KUSD 를 쓰는 일은 못 본다.
- 결과서: `_workspace/sast/110_kusd-boot_result.md` (**분석성공** — 스크립트 문제 없음. 읽을 때 주의: Ghidra 가 함수 한가운데를 떼어 놓은 `FUN_<주소>` 조각 8개가 있어 호출 그래프를 거꾸로 따라가면 엉뚱한 길이 나온다 — 결과서가 조각마다 주인을 따로 적음).
- flow-writer 가 다시 본 것:
  - ntoskrnl `.asm` 줄: `InitBootProcessor +0x16b` (`MOV R15D,0x1`) · `+0x384` · `+0x406` (`[RAX+0x28b],R15B`) · `+0x4a7` · `+0x4b5` · `+0x4c3` (`[RAX+0x36a],R15W`) · `+0x595` · `+0xc01` (`0x65f4`), `KiInitializeKernel +0x5a ~ +0x8e` (`SystemCall` 조건 — `+0x73 CMP [KiSystemCallSelector],ESI` · `+0x7b TEST [HvlEnlightenments],0x80000` · `+0x87 MOV RAX,[MmWriteableSharedUserData]` · `+0x8e MOV [RAX+0x308],ESI`) · `+0x591` · `+0x7ac` · `+0x7fe`, `MmInitSystem +0xb1` · `+0x144`, `KiUpdateProcessorCount +0x9b`, `PspNotifyThreadCreation +0xb4` (`CMPXCHG.LOCK [RCX+0x330]`), ntdll `RtlpAcquireSRWLockExclusiveContended +0xed` (`CMP word [0x7ffe036a],0x1`) · `+0xf6 JBE`.
  - `_calls.tsv`: 아래 표의 call · ret 줄. `MiInitNucleus` · `KeStartAllProcessors` · `EtwInitialize` · `EtwpInitialize` · `ExInitSystem` · `CmFcInitSystem2` · `HalpTimerInitSystem` · `NtInitializeRegistry` · `CmCompleteRegistryInitialization` 등은 `.asm` 이 없어 결과서 objdump 풀이 그대로 (`[정적: objdump]`).
  - 사진 줄: `ms/logs/04_baggage-read_supp_<K1 ~ P2>.txt` `:184` (`TickCountMultiplier 0xfa00000`) · `:190` (`NtSystemRoot "C:\WINDOWS"`) · `:200` (`NtBuildNumber 0x65f4`) · `:215` (`KdDebuggerEnabled 0x3`) · `:216` (`MitigationPolicies 0xa`) · `:222` (`ActiveConsoleId 1`) · `:230` (`SharedDataFlags 0x10e`) · `:246` (`SystemCall 0`) · `:254` (`Cookie 0xe8e5ccd6`) · `:264` (`UnparkedProcessorCount 2`) · `:273` (`ActiveProcessorCount 2`) · `:277` (`QpcBypassEnabled 0x83`) — 8장 모두 같음.

## 결과

비유 (KUSD = 동네 게시판):

- KUSD 는 **모든 가게 (프로세스) 가 같이 보는 게시판** 이다. 가게는 읽기만 하고, 쓰는 사람은 관청 (nt) 하나다.
- 이번에 게시판 칸 102개를 다 봤다. 거의 다 **동네가 처음 생길 때 (부팅)** 관청이 한 번 적었다. 몇 칸은 그 뒤 다른 관공서 (win32k · cng 같은 드라이버) 가 **관청 창구 (nt 공개 함수)** 에 와서 고쳐 달라고 한다.
- test.exe 가게가 문 여는 동안 (`NtCreateUserProcess` ~ `NtContinue`) 게시판을 새로 적는 사람은 없다 — 시계 칸만 계속 바뀐다.
- 관청이 게시판에 쓰는 **펜은 하나** (`MmWriteableSharedUserData`) 뿐이다. 앞 바퀴 (run70) 가 찾던 둘째 펜 (고정 주소 `0xFFFFF780…`) 은 읽기용이었다.

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!entry` (= `KiSystemStartup`) | `+0x1b5` | `+0x1ba` | `nt!KiInitializeBootStructures` | `ⒷS` (run70) | [정적] |
| `nt!entry` | `+0x1d1` | `+0x1d6` | `nt!KdInitSystem` (위상 0) | `KdDebuggerEnabled` (`+0x571`) | [정적] |
| `nt!entry` | `+0x1e8` | `+0x1ed` | `nt!KiInitializeXSaveConfiguration` | `ⒷX` — `XState` 840B (`+0x196 ~ +0x202`) | [정적] |
| `nt!entry` | `+0x27e` | `+0x283` | `nt!KiInitializeKernel` | `ⒷK` (run70) | [정적] |
| `nt!KiInitializeBootStructures` | `+0x2ad` | `+0x2b2` | `nt!MmInitSystem` (위상 −1) | KUSD 칸 안 씀 (run64 표와 같음) | [정적] |
| `nt!KiInitializeBootStructures` | `+0x370` | `+0x375` | `nt!KiInitializeNxSupportDiscard` | `MitigationPolicies` 바이트 (`+0x2d` ~ `+0x11e`) | [정적] |
| `nt!KiInitializeBootStructures` | `+0x382` | `+0x387` | `nt!KiSetFeatureBits` | `EnclaveFeatureMask` (`+0x83a` · `+0x859`, SGX 일 때만 — 안 탐) | [정적] |
| `nt!KiInitializeKernel` | `+0x7fe` | `+0x803` | `nt!InitBootProcessor` | `ⒷI` — 위상 0 본체 (17칸) | [정적] |
| `nt!InitBootProcessor` | `+0x2b0` | `+0x2b5` | `nt!KeInitializeClock` (위상 0) | → `+0x4e5` (ret `+0x4ea`) `KiSetupTimeIncrement` — QPC 칸 · Baseline 둘 | [정적] |
| `nt!InitBootProcessor` | `+0x4cb` | `+0x4d0` | `nt!ExInitSystem` (위상 0) | → `+0x1af ExpInitSystemPhase0` (`CyclesPerYield` 0xa · `SuiteMask`) | [정적] (`+0x1af` 은 objdump) |
| `nt!InitBootProcessor` | `+0x53a` | `+0x53f` | `nt!InitializeDynamicPartitioningPolicy` | `DbgDynProcessorEnabled` (`+0x94`, 조건부 안 탐) | [정적] |
| `nt!InitBootProcessor` | `+0x595` | `+0x59a` | `nt!MmInitSystem` (위상 0) | → `+0xb1` (ret `+0xb6`) `MiInitNucleus` · `+0x144` (ret `+0x149`) `MiInitSystem(0)` | [정적] |
| `nt!InitBootProcessor` | `+0x5f5` | `+0x5fa` | `nt!EtwInitialize` (위상 0) | → `EtwpInitialize` → `EtwpInitializeCoverage +0x1c` (`TelemetryCoverageRound` 1) | [정적] (안쪽 둘은 objdump) |
| `nt!InitBootProcessor` | `+0x805` | `+0x80a` | `nt!HvlPhase1Initialize` | → `+0xe5 VslpIumPhase0Initialize +0x187` (`EnclaveFeatureMask` 비트 8, 조건부 안 탐) | [정적] (`+0xe5` 은 objdump) |
| `nt!MmInitSystem` | `+0xb1` | `+0xb6` | `nt!MiInitNucleus` | `ⒷM` — `+0xe8 MiInitializeBootDefaults` (`SafeBootMode`) · `+0x1b8 MiMemoryLicense` (`NtProductType`) · `+0x44d MiProtectSharedUserPage` (쓰기 별칭 만듦) | [정적] (`MiInitNucleus` 안은 objdump) |
| `nt!MiInitSystem` | `+0x2cd` | `+0x2d2` | `nt!MiUpdateSharedUserDataNumberOfPages` | `NumberOfPhysicalPages` · `FullNumberOfPhysicalPages` | [정적] |
| `nt!Phase1Initialization` | `+0x1e` | `+0x23` | `nt!Phase1InitializationDiscard` | `ⒷP` — 위상 1 본체 | [정적] |
| `nt!Phase1Initialization` | `+0x36` | `+0x3b` | `nt!IoInitSystem` | → `+0x12 IoInitSystemPreDrivers` → `+0xf3b IopInitializeBootDrivers` → `+0x820 PnpBootDeviceWait` (콜백) → `IopMarkBootPartition +0x1ca IopStoreBootDriveLetter` (`NtSystemRoot`) | [정적] |
| `nt!Phase1Initialization` | `+0x61` | `+0x66` | `nt!Phase1InitializationIoReady` | → `+0x1ca FUN_140950fc4` (`SystemExpirationDate`) · `+0x1d5 PsInitSystem(2)` → `+0x3b PspInitPhase2` (`MitigationPolicies` SEH · CurDir) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0x365` | `+0x36a` | `nt!HalInitSystem` | → (표 호출 `[추정]`) `HalpTimerInitSystem` → `HalpTimerConfigureQpcBypass +0xb3 · +0xef RtlSetSystemGlobalData` (`QpcData` · `QpcBypassEnabled`) | [정적] (표 호출 쪽 `[추정]`) |
| `nt!Phase1InitializationDiscard` | `+0x698` | `+0x69d` | `nt!ExpWriteTimeZoneBias` | `TimeZoneBias` (`+0x35` · `+0x3a` · `+0x3f`) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0x743` | `+0x748` | `nt!KeSetSystemTime` | `SystemTime` 첫 값 — `+0x36 KeGenericCallDpcEx` → `KiSetSystemTimeDpc +0xd8 KiUpdateSystemTime` `[추정]` | [정적] (DPC 쪽 `[추정]`) |
| `nt!Phase1InitializationDiscard` | `+0x91f` | `+0x924` | `nt!KeStartAllProcessors` | → `+0x419 KiUpdateProcessorCount` (`+0x8e` · `+0x9b` · `+0xaf` — 프로세서 수 셋) | [정적] (`+0x419` 은 objdump) |
| `nt!Phase1InitializationDiscard` | `+0xaae` | `+0xab3` | `nt!ExInitSystem` (위상 1) | → `ExpInitSystemPhase1 +0x275` (`NtProductType`) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xc65` | `+0xc6a` | `nt!MmInitSystem` (위상 1) | → `+0xf0` (ret `+0xf5`) `MiInitSystem(1)` `+0x45e` (`LargePageMinimum` 0x200000) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xc91` | `+0xc96` | `nt!CmInitSystem1` | `+0x704` (`DbgLkgEnabled`, 조건부) · `+0x99a CmFcInitSystem2` → … `CmFcManagerStartRuntimePhase +0x245 RtlpFcWriteHighLowHigh` (`FeatureConfigurationChangeStamp`) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xf7e` | `+0xf83` | `nt!ExInitializeTimeRefresh` | `SystemExpirationDate` (`+0x44`, 0 을 씀) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xf8d` | `+0xf92` | `nt!ExInitializeUtcTimeZoneBias` | `TimeZoneBiasStamp` (`+0x196`) · `+0x19d ExpWriteTimeZoneBias` | [정적] |
| `nt!Phase1InitializationDiscard` | `+0xf9c` | `+0xfa1` | `nt!ExpRefreshTimeZoneInformation` | `+0x4e0 RtlSetSystemGlobalData` (`TimeZoneId`) · `+0x5fb ExpWriteTimeZoneBias` · `+0x61d ExpWriteTimeZoneBiasStartEnd` · `+0x5ec` · `+0x629` Stamp | [정적] |
| `nt!Phase1InitializationDiscard` | `+0x126c` | `+0x1271` | `nt!ExpWatchProductTypeInitialization` | `ProductTypeIsValid` · `NtProductType` (`+0x8c ExpGetNtProductTypeFromLicenseValue` 에 칸 주소 넘김) | [정적] |
| `nt!Phase1InitializationDiscard` | `+0x12fd` | `+0x1302` | `nt!SeCodeIntegrityInitializePolicy` | → … `SepSecureBootSetRegistryKey +0x14c` (`DbgSecureBootEnabled`, 조건부 안 탐) | [정적] (안쪽은 objdump) |
| `nt!PspUserThreadStartup` | `+0x9c` | `+0xa1` | `nt!PspNotifyThreadCreation` | `Cookie` (`+0xb4`, 0 일 때만 — test.exe 첫 스레드도 지나지만 안 씀) | [정적] |
| `nt!NtInitializeRegistry` | `+0xa7` | — | `nt!CmCompleteRegistryInitialization` | `+0x13f PsBootPhaseComplete` (`SharedDataFlags` 비트 1·2·3 `+0x278`) · `+0xc9` 콜백 `CmpFinishSystemHivesLoad` → `CmpLoadSystemVersionData +0x1e4` (`NtBuildNumber` 둘째) | [정적: objdump] |

- 다른 모듈이 nt 공개 함수를 부르는 줄 (IAT, 결과서 `iatsites.py` — 그 모듈 코드는 안 읽음): win32kbase `Win32kBaseDriverEntry +0xcd9` · `InitVideo +0x1d5` · `xxxRemoteConnect +0x837` · win32kfull `xxxRemoteReconnect +0xdb8` · `xxxRemoteSetDisconnectDisplayMode +0x16b` → `RtlSetActiveConsoleId`, win32kfull `EditionProcessForegroundPriorityChanged +0x6d` → `RtlSetConsoleSessionForegroundProcessId`, win32kbase `CitpLastInputUpdate +0x52` · cng.sys rva `+0x300b5` · `+0x61085` · condrv `CdCreateBroker +0x15e` · `CdDereferenceBroker +0xaa` · kdnet.dll (`+0x86f0` · `+0xb2c0` 근처) → `RtlSetSystemGlobalData`. `[정적]`

### 호출 관계

```
[부팅 위상 0 — BSP]
entry (= KiSystemStartup)
 ├─ +0x1b5  KiInitializeBootStructures                         ⒷS
 │    ├─ +0x2ad  MmInitSystem(−1)                               KUSD 칸 안 씀
 │    ├─ +0x370  KiInitializeNxSupportDiscard                   MitigationPolicies (NX) · (ProcessorFeatures 12, run70)
 │    └─ +0x382  KiSetFeatureBits                               EnclaveFeatureMask (SGX 일 때만 — 안 탐)
 ├─ +0x1d1  KdInitSystem(0)                                     KdDebuggerEnabled = 1
 ├─ +0x1e8  KiInitializeXSaveConfiguration                      ⒷX  XState 840B 통째 복사
 └─ +0x27e  KiInitializeKernel                                  ⒷK  +0x8e SystemCall (조건 둘 — 안 탐) · +0x591 TestRetInstruction · +0x7ac VirtualizationFlags
      └─ +0x7fe  InitBootProcessor                              ⒷI  +0xf3 BootId · +0x384 TickCountMultiplier · +0x397 CryptoExponent 0 · +0x3f9 GlobalValidationRunlevel
           │                                                         +0x406 (ProcessorFeatures 23, run70) · +0x4a7/+0x4b5/+0x4c3 프로세서 수 셋 = 1
           │                                                         +0x836/+0x847 Reserved1·3 · +0xbe2 ~ +0xc42 버전 · 아키텍처 · ImageNumber 둘 · LangGenerationCount 0
           ├─ +0x2b0  KeInitializeClock(0) ─ +0x4e5 KiSetupTimeIncrement   QPC 묶음 5 · Baseline 둘
           ├─ +0x4cb  ExInitSystem(0) ─ +0x1af ExpInitSystemPhase0          CyclesPerYield 0xa · SuiteMask
           ├─ +0x53a  InitializeDynamicPartitioningPolicy                   DbgDynProcessorEnabled (안 탐)
           ├─ +0x595  MmInitSystem(0)
           │    ├─ +0xb1   MiInitNucleus                         ⒷM  +0xe8 MiInitializeBootDefaults (SafeBootMode, 안 탐) · +0x1b8 MiMemoryLicense (NtProductType)
           │    │                                                     +0x44d MiProtectSharedUserPage — 쓰기 별칭을 새로 만들어 포인터를 바꿈 (칸은 안 씀)
           │    └─ +0x144  MiInitSystem(0)                            +0x2de LargePageMinimum 0 · +0x2cd MiUpdateSharedUserDataNumberOfPages
           ├─ +0x5f5  EtwInitialize(0) ─ EtwpInitialize ─ EtwpInitializeCoverage +0x1c   TelemetryCoverageRound 1
           └─ +0x805  HvlPhase1Initialize ─ +0xe5 VslpIumPhase0Initialize         EnclaveFeatureMask (VBS 안쪽 — 안 탐)

[부팅 위상 1 — 시스템 스레드]
Phase1Initialization
 ├─ +0x1e  Phase1InitializationDiscard                          ⒷP
 │    +0x365 HalInitSystem ⇢ (표 호출 [추정]) HalpTimerInitSystem   QpcBias · QpcData · QpcBypassEnabled
 │    +0x692 TimeZoneBiasStamp 0 · +0x698 ExpWriteTimeZoneBias · +0x743 KeSetSystemTime (SystemTime)
 │    +0x91f KeStartAllProcessors ─ +0x419 KiUpdateProcessorCount     프로세서 수 셋 +1
 │    +0xaae ExInitSystem(1) ─ ExpInitSystemPhase1 +0x275            NtProductType
 │    +0xc65 MmInitSystem(1) ─ +0xf0 MiInitSystem(1) +0x45e           LargePageMinimum 0x200000
 │    +0xc91 CmInitSystem1                                            DbgLkgEnabled (안 탐) · FeatureConfigurationChangeStamp
 │    +0xced / +0xd05 / +0xd1d                                        SharedDataFlags 비트 8 (켬) · 9 · 10 (안 탐)
 │    +0xf7e ExInitializeTimeRefresh · +0xf8d ExInitializeUtcTimeZoneBias · +0xf9c ExpRefreshTimeZoneInformation
 │    +0x126c ExpWatchProductTypeInitialization · +0x127a ComPlusPackage −1 · +0x12e0 CyclesPerYield · +0x12fd SeCodeIntegrityInitializePolicy
 ├─ +0x36  IoInitSystem ─ … IopInitializeBootDrivers +0x820 PnpBootDeviceWait ⇢ (콜백) IopMarkBootPartition +0x1ca IopStoreBootDriveLetter   NtSystemRoot
 └─ +0x61  Phase1InitializationIoReady
      ├─ +0x1ca  FUN_140950fc4 (조각)                           SystemExpirationDate
      └─ +0x1d5  PsInitSystem(2) ─ +0x3b PspInitPhase2              MitigationPolicies (SEH · CurDir)

[부팅 뒤 · 프로세스 생성 앞 — ⒷR]
NtInitializeRegistry (smss [추정]) ─ +0xa7 CmCompleteRegistryInitialization
 ├─ +0x13f  PsBootPhaseComplete +0x278                          SharedDataFlags 비트 1·2·3
 └─ +0xc9   (콜백) CmpFinishSystemHivesLoad ─ CmpLoadSystemVersionData +0x1e4   NtBuildNumber (둘째)
PspUserThreadStartup (첫 사용자 스레드 [추정: smss]) ─ +0x9c PspNotifyThreadCreation +0xb4   Cookie (0 일 때만)
win32kbase · win32kfull · cng · condrv · kdnet  ┄(IAT)▶  RtlSetActiveConsoleId · RtlSetConsoleSessionForegroundProcessId · RtlSetSystemGlobalData(ID)

[계속 — 시계 인터럽트]
(HAL 시계 인터럽트) ─ KeClockInterruptNotify +0x2ba KiUpdateTime      InterruptTime · SystemTime · TickCount · TimeUpdateLock · Baseline
```

### 1. KUSD 쓰는 길은 하나 — `MmWriteableSharedUserData` (결과서 "한눈에")

- `[정적]` 24H2 커널은 KUSD 를 전역 포인터 **`MmWriteableSharedUserData`** (nt+`0xfc5008`) 로만 쓴다. 이 포인터를 읽는 곳 전수 200곳 = 77 함수 (sast/65 `refs` 200줄과 같은 수 — 잘린 게 아님).
- `[정적]` 이 포인터의 PE 처음 값은 고정 별칭 `0xfffff78000000000` 이다. **`MiInitNucleus +0x44d → MiProtectSharedUserPage +0x1a4` 가 쓰기 가능한 별칭을 새로 만들어 포인터를 바꾼다.** 같은 함수가 기능 스위치 `Feature_KernelSharedUserDataAslr` 사용 보고를 부른다 — 이름으로 보아 KUSD 주소 무작위화 `[추정]`. 고정 별칭은 그 뒤 읽기 전용으로 보인다 `[추정]`.
- `[정적]` 고정 별칭 (`movabs 0xfffff780000000xx`, 1,294줄) 을 담은 레지스터로 **쓰는 줄은 0** (오탐 1줄은 `RtlGetSystemGlobalData` 의 읽기 조각 `FUN_1406f0d88 +0x305`). 그래서 run70 (sast/65) 이 훑던 둘째 모양 "절대 주소 `0xFFFFF780000002xx`" 는 **쓰기에선 빈 모양** 이었다 (읽기 · 잠금 주소 넘기기뿐).
- 쓰는 함수 54개 · 쓰는 줄 131곳 (`ProcessorFeatures` 28줄은 run70 몫이라 뺌). 가장 많이 쓰는 함수: `InitBootProcessor` 17칸 · `RtlSetSystemGlobalData` 10칸 (공개 함수 — 부르는 쪽이 따로) · `KiUpdateTime` 8칸 · `Phase1InitializationDiscard` 7칸 · `KiSetupTimeIncrement` 7칸.

### 2. `K1 이전` 61칸 — 값의 출처 (결과서 질문 1)

| 값의 출처 | 칸 |
|---|---|
| 상수 (코드에 박힘) | `ImageNumberLow` · `High` (`0x8664`) · `NtMajorVersion` (`0xa`) · `NtBuildNumber` 첫 값 (`0x65f4`) · `Reserved1` (`0x7ffeffff`) · `Reserved3` (`0x80000000`) · `TestRetInstruction` (`0xc3`) · `TelemetryCoverageRound` (1) · 프로세서 수 셋 첫 값 (BSP 1) · `TimeZoneBiasStamp` 첫 값 (0) · `CyclesPerYield` 첫 값 (`0xa`) · `LargePageMinimum` (0 → `0x200000`) |
| 로더 블록 `LOADER_PARAMETER_BLOCK` | `BootId` (확장 `+0xb54`) · `NtSystemRoot` 의 `\WINDOWS` (`NtBootPathName`) · `NXSupportPolicy` (`LoadOptions` 의 `NOEXECUTE=`) · (조건) `SafeBootMode` · `KiSystemCallSelector` (확장 `+0x84` 비트 8) |
| 레지스트리 | `SharedDataFlags` 비트 1·2·3 (`Policies\System` 의 `EnableLUA` · `EnableVirtualization` · `EnableInstallerDetection`, 값이 없으면 켬) · `NtProductType` (`MmRegistryState+0x2c` 글자) · `SuiteMask` (`CmSuiteBuffer`) · `ComPlusPackage` · `TimeZoneBias*` · `SEHValidationPolicy` (`PspSehValidationPolicy`, PE 처음 값 2) · `NtBuildNumber` 둘째 · `FeatureConfigurationChangeStamp` |
| CPU · 하이퍼바이저 | `XState` (CPUID 리프 0xd) · `VirtualizationFlags` (`KiVirtFlags`) · `QpcFrequency` · `QpcInterruptTimeIncrement*` (`RtlGenerateQpcToIncrementConstants`) · `QpcBias` · `QpcBypassEnabled` · `NativeProcessorArchitecture` (`KeProcessorArchitecture`) |
| 계산 | `TickCountMultiplier` (156250 × 2^24 / 10000) · `NumberOfPhysicalPages` · `FullNumberOfPhysicalPages` (`MiSystemPartition+0x4850`) · `CyclesPerYield` 둘째 (`ExpComputeCyclesPerYield`) · `Cookie` (QPC · `ExGenRandom` · `rdtsc` · PRCB 칸을 섞음) · `QpcSystemTime*` (Interrupt 쪽 복사) |
| 라이선스 | `NtProductType` (`ExpGetNtProductTypeFromLicenseValue` · `RtlGetProductInfo`) |
| 다른 모듈이 값을 정해 nt 공개 함수로 넘김 (`ⒷR`) | `RNGSeedVersion` (cng) · `ActiveConsoleId` · `ConsoleSessionForegroundProcessId` · `LastSystemRITEventTickCount` (win32kbase · win32kfull) · `KdDebuggerEnabled` 둘째 비트 (kdnet) |

- 칸마다 쓰는 줄 · 경로는 짐 지도 3.7 (결과서 표 A 1 ~ 61).
- 시각 `*` 8칸: 처음 값은 Baseline 둘 = `KiSetupTimeIncrement +0x135` · `+0x143` (위상 0 길에서만 닿음), 나머지 여섯 = 첫 시계 인터럽트 (`KeClockInterruptNotify +0x2ba → KiUpdateTime`) · `SystemTime` 은 `KeSetSystemTime` (`ⒷP +0x743`). 계속 고치는 쪽은 이름만 (`KiUpdateTimeAssist` · `KiUpdateSystemTime` · 디버거 얼림 · 깨움 `KeFreezeExecution` / `KeThawExecution` · 절전 복귀 `KeRebaselineSystemTime` / `KeRebaselineInterruptTime`).

### 3. `끝까지 0` 41칸 — 넷으로 가름 (결과서 질문 2)

| 가름 | 칸 | 한 줄 |
|---|---|---|
| **0 을 씀 (8)** | `CryptoExponent` · `NtMinorVersion` (명시 0, `InitBootProcessor +0x397` · `+0xbf3`) · `GlobalValidationRunlevel` (`+0x3f9`, 전역 복사) · `TimeZoneId` (`RtlSetSystemGlobalData` ID 6 — 이 VM 0 = UNKNOWN) · `SystemExpirationDate` (`ExInitializeTimeRefresh +0x44` · `FUN_140950fc4 +0x3ec`, 정식판 0) · `MitigationPolicies.CurDirDevicesSkippedForDlls` (`PspInitPhase2 +0x136` 비트 지움) · `MitigationPolicies.Reserved` (바이트 되쓰기에 실려 감) · `QpcReserved` (`QpcData` WORD 0) | 코드가 그 칸 (· 같은 바이트) 에 실제로 쓴다 |
| **조건부 안 탐 (11)** | `SafeBootMode` (`SAFEBOOT:`) · `DbgLkgEnabled` (`CmpLKGEnabled`) · `DbgDynProcessorEnabled` (동적 파티셔닝 라이선스) · `DbgSecureBootEnabled` (이 VM Secure Boot 꺼짐) · `DbgMultiUsersInSessionSku` · `DbgStateSeparationEnabled` · `SpareBits` (`Feature_ShadowAdmin`) · **`SystemCall`** (아래 5절) · `EnclaveFeatureMask` (SGX · VBS 안쪽) · `UserModeGlobalLogger` (ETW 세션) · `ImageFileExecutionOptions` (검증기) | 쓰는 줄이 부팅 길 (`ⒷS` ~ `ⒷP`) 안에 있는데 조건이 안 맞음 |
| **아무도 안 씀 (17)** | `TickCountLowDeprecated` · `MaxStackTraceDepth` · `AppCompatFlag` · `Reserved0` · `TimeSlip` · `AlternativeArchitecture` · `Reserved12` · `DataFlagsPad` · `Reserved2` · `SystemCallPad` · `TickCountPad` · `CookiePad` · `Reserved4` · `Reserved9` · `Spare` · `UserPointerAuthMask` (ARM64 용 `[추측]`) · `Reserved10` | 두 모양 · 포인터 저장 · 인자 넘김 어디에도 쓰는 줄 없음. 로더가 0 으로 채운 쪽이라 0 `[추정]` (`MiInitializeSharedUserData` 는 쪽을 안 지움) |
| **범위 밖 길 `[추측]` (5)** | `AitSamplingValue` (`NtSetSystemInformation +0x1030`) · `DismountCount` (`FsRtlDismountComplete +0xb` · `IopXxxControlFile`) · `DbgErrorPortPresent` (`DbgkRegisterErrorPort +0x29c`) · `DbgConsoleBrokerEnabled` (condrv `CdCreateBroker` → `RtlSetSystemGlobalData(14)`) · `InterruptTimeBias` (절전 복귀 `KiAdjustTimersAfterDripsExit +0x66`) | 부팅 길 안엔 쓰는 줄이 없고 시스템 콜 · 드라이버 · 절전 복귀에서만 |

- `SharedDataFlags` 비트 칸 12개는 **켜진 4 (1·2·3·8) + 끝까지 0 의 8** 로 갈렸고 비트마다 쓰는 함수가 다르다. 사진 `0x10e` = 비트 1·2·3·8 과 맞다.

### 4. 쓰는 길이 커널 밖에서 시작하는 칸 (결과서 질문 3) — 요청서 짐작 정정

- `[정적]` **`ActiveConsoleId` 는 csrss · winlogon 이 시스템 콜로 쓰는 게 아니다.** nt 공개 함수 `RtlSetActiveConsoleId` (`+0x18` 이 쓰는 줄) 를 **win32kbase.sys · win32kfull.sys (커널 드라이버)** 가 IAT 로 부른다 (nt 안엔 부르는 줄 0). 요청서의 "csrss/winlogon 이 syscall 로" 짐작이 틀렸다.
- 같은 꼴 (드라이버가 nt 공개 함수 호출 — syscall 아님): `ConsoleSessionForegroundProcessId` ← win32kfull · `LastSystemRITEventTickCount` ← win32kbase (입력 이벤트마다) · `RNGSeedVersion` ← cng.sys · `DbgConsoleBrokerEnabled` ← condrv · `KdDebuggerEnabled` 둘째 비트 ← kdnet.dll. 모두 쓰는 줄이 nt 안이라 **쓴 주인 = nt**.
- syscall (다른 프로세스가 부름): `LangGenerationCount` (`NtGetMUIRegistryInfo +0x374`) · `ComPlusPackage` (`NtSetSystemInformation +0x8e2`, 처음 묻는 쪽이 `ExpReadComPlusPackage +0x9e` 로 채움) · `SharedDataFlags` 비트 1·2·3 (`NtInitializeRegistry` — smss `[추정]`) · `TimeZoneBias*` (부팅은 직접, 이후 `NtSetSystemTime` 등).
- 직접 (첫 사용자 스레드 시작): `Cookie` — `PspNotifyThreadCreation +0xb4` 가 0 일 때만 `lock cmpxchg` 로 한 번. **test.exe 첫 스레드 (`P0`) 도 이 함수를 지나지만 이미 값이 있어 안 쓴다** `[정적]`. 처음 쓰는 스레드가 smss 주 스레드인지는 `[추정]`.
- HAL 은 nt 안에 있다 (hal.dll 은 코드 없는 껍데기: 구역 `.rdata` · `.data` · `.edata` · `.rsrc`, `0x6000`B — 결과서). 그래서 `QpcBias` · `QpcData` · `QpcBypassEnabled` 는 nt 가 자기 공개 함수 `RtlSetSystemGlobalData` 를 부르는 꼴.

### 5. `SystemCall` (`+0x308`) 쓰는 줄 — run105 정적 ② 닫힘

- `[정적]` 쓰는 줄 = **`KiInitializeKernel +0x8e`** (`MOV [RAX+0x308],ESI`, `RAX` = `MmWriteableSharedUserData`, `ESI` = 1). 조건 둘 (flow-writer `.asm` 으로도 봄):
  - `+0x73 CMP [KiSystemCallSelector],ESI` — `KiSystemCallSelector == 1`. 이 전역의 PE 처음 값은 0 이고, `KiInitializeBootStructures +0x2d1` 이 로더 확장 `+0x84` 비트 8 이 켜졌을 때만 1 로 만든다.
  - `+0x7b TEST [HvlEnlightenments],0x80000` — 비트 19.
  - 둘 다여야 `+0x8e` 에 닿는다. 아니면 `+0x79` · `+0x85` 가 `+0x9b` 로 건너뛴다.
- `[관찰]` run44 사진 8장 `SystemCall : 0` (`_supp_<K1 ~ P2>.txt:246`) → 이 VM 은 조건이 안 맞아 **조건부 안 탐**. ntdll 스텁 494곳은 bit 0 이 꺼져 `SYSCALL` 길 (run105 와 같음).
- 조건 전역 두 값 (`nt!KiSystemCallSelector` nt+`0x100815c` · `nt!HvlEnlightenments` nt+`0xfc6af8`) 은 사진에 없다 → 동적 1 (읽기만).

### 6. run70 `[추측: 1 그대로]` 확정 — `InitBootProcessor +0x406`

- `[정적]` `ProcessorFeatures[23]` (`FASTFAIL`, `+0x28b`) 을 쓰는 `+0x406 MOV [RAX+0x28b],R15B` 의 `R15B` 는 **늘 1** 이다. `r15d` 는 `+0x6d` 에서 1, 중간 갈래 (`+0x123`) 에서 바뀌어도 `+0x16b MOV R15D,0x1` 에서 다시 1 (결과서, `+0x16b` · `+0x406` 은 flow-writer `.asm` 으로도 봄). → 짐 지도 3.7 `[23]` 줄 `[추측]` → `[정적]`, flow.md 4장 run70 정적 ③ 닫힘.
- 덤 (run70 정적 ②): `KiInitializeXSaveConfiguration` 이 KUSD `+0x3d8` `XState` 를 쓰는 줄 = `+0x196 ~ +0x202` (지역 `_XSTATE_CONFIGURATION` 840B 통째 복사, 그 뒤 `+0x21e ~ +0x23c` 는 읽기). → flow.md 2.3 나무의 `[추측] — 본문 안 읽음` 이 `[정적]`.

### 7. 간접 호출 (결과서 질문 4)

- 좁힌 것: `IopMarkBootPartition` ← `IopInitializeBootDrivers +0x813` 이 주소를 `lea r8` 로 넘기고 `+0x820 PnpBootDeviceWait` (콜백) · `CmpFinishSystemHivesLoad` ← `CmCompleteRegistryInitialization +0xc9` (`lea r8`) · `KiSetSystemTimeDpc` ← `KeSetSystemTime +0x36 KeGenericCallDpcEx` `[추정]` · `HalpTimerInitSystem` ← `HalInitSystem` 안 표 호출 `[추정]` · `HalpTscFallback` ← `HalpTscFallbackToPlatformSource +0x54` (`lea rcx`) · `EtwpCoverageProvEnableCallback` ← `EtwpInitializeCoverage +0xb` (ETW 공급자 콜백 등록).
- 못 좁힌 것 (동적): `PnpBootDeviceWait` 가 콜백을 몇 번 · 어느 디스크에 부르나 (`NtSystemRoot` 를 두 번 쓸 수 있나), HAL 타이머 길이 어느 위상인가 (`QpcBias` 첫 값), 시스템 콜 (`NtSetSystemInformation` · `NtGetMUIRegistryInfo` · `NtInitializeRegistry` · `NtSetSystemTime` · `NtFsControlFile`) 을 누가 · 언제 부르나.

### 8. 사진 값과 맞춤 (결과서)

- **불일치 0.** 쓰는 줄이 있는 칸은 시각 `*` 8 빼고 8장 모두 같은 값 — "K1 ~ P2 사이엔 안 바뀐다" 는 run44 결론이 줄 단위로 닫혔다. 시각 8칸은 8장 모두 다르다.
- 계산으로 맞은 값: `TickCountMultiplier` `0xfa00000` (= 15.625 × 2^24) · `MitigationPolicies` `0xa` (= NX 2 (옵션 없음 = OptIn) + SEH 2 × 4) · `SharedDataFlags` `0x10e` · 프로세서 수 `2 · 2 · 1` (BSP 1 + AP 1) · 물리 쪽 `0x1ffe86` (8GB) · `TimeZoneBias` −9시간 (KST) · `TimeZoneBiasStamp` 4 (새로 고침 2번 × 2) · 버전 · 아키텍처 · `TestRetInstruction` · `NtProductType` · `LargePageMinimum` · `SafeBootMode` · `SystemCall` 0.
- 줄만 맞음 (값 계산 본문은 안 읽음): `CyclesPerYield` `0x22` · `Qpc*Increment` `0x8000000000000000` + 시프트 1 · `QpcBias` · `QpcBypassEnabled` `0x83` · `BootId` `0xf` · `SuiteMask` `0x110` · `Cookie` · `XState` `0xe7`.
- 사진은 부팅이 끝난 뒤 값이라 **부팅 중 순서** (`ⒷS` → `ⒷP`) 는 사진으로 못 맞춘다 (정적 순서만).

## 확인된 것

- `[정적]` KUSD 102칸 모두 쓰는 줄 (또는 "없음") 과 가름 (위 요약 표, 짐 지도 3.7 · 결과서 표 A). 쓰는 줄 131곳, `.asm` 대조 144/144.
- `[정적]` KUSD 를 쓰는 포인터는 `MmWriteableSharedUserData` 하나. 고정 별칭으로 쓰는 줄 0.
- `[정적]` `MmInitSystem` 위상 셋의 몫: `(−1)` = `KiInitializeBootStructures +0x2ad` (KUSD 안 씀), `(0)` = `InitBootProcessor +0x595` → `+0xb1 MiInitNucleus` (쓰기 별칭 · `SafeBootMode` · `NtProductType`) → `+0x144 MiInitSystem(0)` (물리 쪽 수 · `LargePageMinimum` 0), `(1)` = `Phase1InitializationDiscard +0xc65` → `+0xf0 MiInitSystem(1)` (`LargePageMinimum` `0x200000`). run64 의 갈래 표와 같다.
- `[정적]` `SystemCall` 쓰는 줄 `KiInitializeKernel +0x8e` 와 조건 둘. `[관찰]` 사진 0.
- `[정적]` `ActiveConsoleId` · `ConsoleSessionForegroundProcessId` · `LastSystemRITEventTickCount` · `RNGSeedVersion` · `DbgConsoleBrokerEnabled` 는 드라이버가 nt 공개 함수를 IAT 로 부른다.
- `[정적]` `InitBootProcessor +0x406` 의 `R15B` = 1 (run70 `[추측]` → 확정), `KiInitializeXSaveConfiguration +0x196 ~ +0x202` 가 `XState` 를 쓴다 (run70 정적 ② 닫힘).
- `[정적]` `UnparkedProcessorCount` (`+0x36a`) = BSP 1 (`InitBootProcessor +0x4c3`) → AP 를 올릴 때마다 +1 (`KiUpdateProcessorCount +0x9b`). ntdll 이 `RtlpAcquireSRWLockExclusiveContended +0xed` 에서 읽는다 (`≤ 1` 이면 `+0xf6 JBE` 로 스핀 건너뜀 — flow-writer `.asm`). `[관찰]` 사진 2.

## 결과서와 다르게 적은 곳 · 보탠 것

- **단계 줄임 `ⒷL` → 짐 지도에선 `ⒷR`**: 결과서가 제안한 `ⒷL` (부팅 뒤 · 프로세스 생성 앞) 은 짐 지도에 이미 있는 `ⒷL…` (run48 — `PsLocateSystemDlls → PspLocateSystemDll` 사슬) 과 겹친다. 그래서 **`ⒷR`** (부팅이 끝나고 돌아가는 동안) 로 바꿔 넣었다. 나머지 넷 (`ⒷI` · `ⒷM` · `ⒷX` · `ⒷP`) 은 겹치는 게 없어 그대로 — 짐 지도 3장 줄임 표에 `ⒷI…` 꼴로 (끝 `…` 는 짐 지도 줄임 표 관례).
- **`Ⓑ2` 의 뜻이 한 칸 다르다**: 결과서의 `Ⓑ2 +0xf1` 은 `PspInitPhase2` 안 줄이다. 짐 지도 3장 줄임 `Ⓑ2…` 는 한 단계 더 들어간 `PspInitializeSystemDlls` 까지라, 3.7 줄에선 `Phase1InitializationIoReady → PsInitSystem(2) (호출 +0x1d5) → PspInitPhase2 (호출 +0x3b) +0xf1` 처럼 풀어 썼다.
- **경로 꼴**: 결과서는 `함수 +0xN → 다음 함수` (오프셋 = 그 함수 안에서 다음을 부르는 줄) 로 썼다. 짐 지도 3장 관례 (`→ 다음 함수 (호출 +0xN)`, 오프셋은 마지막 함수 뒤만) 로 바꿔 옮겼다. 뜻은 같다.
- **run 번호**: 결과서 "3.7 에 넣을 줄" 머리의 `(해결됨 → run110, sast/110)` 은 sast 번호를 run 번호로 쓴 것이다 — 이 바퀴는 **run115**.
- **`MmInitSystem` 위상**: 앞 문서 (run57 정정 뒤 run64 표 · flow.md 2.1 · 짐 지도 `ⒷA`) 에 `MiInitNucleus` 위상을 틀리게 적은 곳은 못 찾았다. 결과서의 `ⒷM` 정의 (`InitBootProcessor +0x595 → MmInitSystem(0) +0xb1 → MiInitNucleus`) 가 run64 갈래 표 (위상 0 = `+0xa9 TEST R9D,R9D` → `+0xb1 MiInitNucleus`) 와 같다. 그래서 정정 줄은 안 넣고 "확인된 것" 에 한 줄로 남겼다.
- **flow.md 2.3 나무 한 줄**: `KiSetFeatureBits … (KUSD 안 씀)` 은 너무 넓었다 — `ProcessorFeatures` 는 안 쓰지만 `EnclaveFeatureMask` (`+0x36c`) 를 SGX 일 때 쓴다 (`+0x83a` · `+0x859`, 이 VM 은 안 탐). 아래 "정정".
- **셈** (오케스트레이터 지시 그대로): 아는 칸 `1 + 61 + 8 + 11 = 81`, 아무도 안 씀 17, 범위 밖 5. 결과서 셈과 같다.

## 정적과 동적이 다른 곳

- 없다. 102칸 모두 쓰는 줄 · 조건 · 사진 값이 맞는다.
- **디버거가 붙어서 달라지는 점**:
  - `KdDebuggerEnabled` = `0x3` 은 **KDNET 디버거가 붙어서 생긴 값**이다. nt 쓰는 줄 (`KdInitSystem(0) +0x571`) 은 1 만 쓰고 kdnet.dll 이 `RtlSetSystemGlobalData(10)` 로 비트를 더한다 (비트 뜻은 안 읽음). 디버거 없이 부팅하면 0 일 것 `[추측]` → 동적 4.
  - 시각 `*` 칸은 디버거가 멈출 때 `KeFreezeExecution` / `KeThawExecution` 도 고친다 — 이 사진들은 디버거 붙은 VM 이라 그 줄도 돌았을 수 있다 (사진은 원래 "소음" 칸).
  - 그 밖 `QpcBypassEnabled` `0x83` (이 VM 의 타이머 종류) · `NumberOfPhysicalPages` · `ActiveProcessorCount` (8GB · 2코어) 는 **VM 환경** 에 달린 값이다 (디버거와 무관).

## 정정

- **flow.md 2.3 "KUSD `ProcessorFeatures` 를 채우는 곳" 나무** (run70 이 그림): `(4) +0x387 KiSetFeatureBits … (KUSD 안 씀)` → "`ProcessorFeatures` 는 안 씀 · `EnclaveFeatureMask` 는 SGX 일 때만 (`+0x83a` · `+0x859`, 안 탐)". run70 문서는 `ProcessorFeatures` (`+0x274`) 만 말했으니 틀리지 않았다 — 맨 위 정정 줄은 안 넣는다.
- **요청서 짐작** (run 문서 아님): `ActiveConsoleId` 는 csrss · winlogon 시스템 콜이 아니라 win32k 드라이버의 nt 공개 함수 호출 (위 4절).

## 이전 run 에서 닫은 것

- run105 정적 ② (flow.md 4장 · 짐 지도 7.37 · plan Q192 ②) "KUSD `+0x308` 을 쓰는 커널 줄 — 부팅 쪽" → `KiInitializeKernel +0x8e`, 조건부 안 탐 (5절).
- run70 정적 ② (`KiInitializeXSaveConfiguration` 이 `XState` 쓰는 줄) · ③ (`InitBootProcessor +0x16b` `R15D` 가 `+0x406` 까지 1 인가) → 닫힘 (6절). 짐 지도 7.29 ② ③ · 7.5 `XState` 실마리.
- run89 실마리 (짐 지도 7.2 · 3.7 머리) `ImageNumberLow` · `High` 쓰는 쪽 → `InitBootProcessor +0xc2c` · `+0xc37` (상수 `0x8664`), `ComPlusPackage` → `Phase1InitializationDiscard +0x127a` (−1) · `ExpReadComPlusPackage +0x9e` · `NtSetSystemInformation +0x8e2`, `Cookie` → `PspNotifyThreadCreation +0xb4`. flow.md 4장 run89 정적 ① 의 "`ExpReadComPlusPackage` (KUSD `ComPlusPackage` 를 쓰나)" → 쓴다 (`+0x9e`, 처음 묻을 때).
- run91 실마리 `RNGSeedVersion` 쓰는 쪽 → `RtlSetSystemGlobalData +0x14f` ← cng.sys (IAT).
- run83 실마리 `SharedDataFlags.DbgConsoleBrokerEnabled` 쓰는 쪽 → condrv `CdCreateBroker +0x15e` → `RtlSetSystemGlobalData(14)` (범위 밖 — test.exe 콘솔은 이미 있음).
- 짐 지도 3.7 사진 줄 "값은 부팅 때 정해진 것 `[추측]`" → `[정적]` (부팅 `Ⓑ` 52칸 · 부팅 뒤 `ⒷR` 9칸 — 둘 다 test.exe 프로세스 생성 앞).

## 아직 확인 필요

### 동적 (결과서 "동적으로 확인할 것" 1 ~ 5 — 결과서 판단 "1 · 2 읽기만, 급하지 않음", 대기열 번호는 오케스트레이터가 붙임)

- **1** "조건부 안 탐" 11칸의 조건 전역 값 — 부팅이 끝난 VM 에서 `dd` 한 번씩 (읽기만): `nt!KiSystemCallSelector` (nt+`0x100815c`) · `nt!HvlEnlightenments` (`0xfc6af8`, 비트 19 `0x80000`) · `nt!KeDynamicPartitioningSupported` (`0xfc6ae4`) · `nt!CmpLKGEnabled` (`0xfd8108`) · `nt!RtlpMultiUsersInSessionSupported` (`0xfc37d0`) · `nt!CmStateSeparationEnabled` (`0xeefb28`) · `nt!ViImageExecutionOptions` (`0xffdaac`) · `nt!InitSafeBootMode` (`0xfc3804`) · `nt!g_SecureBootPolicyBlobHeader` (`0xe67d90`). 모두 0 이면 사진 0 과 맞는다.
- **2** "0 을 씀" 입력값: `nt!CmGlobalValidationRunlevel` (`0x10086f4`) · `nt!ExpNtExpirationDate` (`0xfd9f20`, 8B) · `nt!ExpSystemSetupInProgress` (`0xef0070`, 1B) · `nt!PspCurDirDevicesSkippedForDlls` (`0xfc5308`) · `nt!PspSehValidationPolicy` (`0xfc5310`, PE 처음 값 2) · `nt!InitTickRolloverDelay` (`0x100869c`).
- 3 (선택, 낮음 — 부팅 중 멈춤점) 부팅 때 순서 · 몇 번 쓰나: `IopStoreBootDriveLetter +0x136` (`PnpBootDeviceWait` 콜백 횟수) · `PspNotifyThreadCreation +0xb4` (누가 처음 `Cookie` 를 쓰나) · `ExpReadComPlusPackage +0x9e` (누가 처음 묻나) · `HalpTimerConfigureQpcBypass +0xb3` (어느 위상).
- 4 (선택) 디버거 없이 부팅한 VM 에서 `KdDebuggerEnabled` 가 0 인지 한 번 대조.
- 5 (선택) 레지스트리 `HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System` 의 `EnableLUA` · `EnableVirtualization` · `EnableInstallerDetection` — 사진 `0x10e` 와 맞는지.

### 정적 (결과서 "다음 요청 후보" + flow-writer)

- ① (작음) HAL 타이머 길 닫기 — `HalInitSystem → HalpTimerInitSystem` 간접 사슬 · `KeInitializeClock` 위상 1 갈래 (`QpcBias` · `QpcData` · `QpcBypassEnabled` 가 어느 위상에 쓰이나, 지금 `[추정]`).
- ② (작음) 값 계산식 본문 — 에뮬레이터 상수를 CPU · 환경에서 셈하려면: `ExpComputeCyclesPerYield` · `RtlGenerateQpcToIncrementConstants` · `ExGetSuiteMask` · `ExpGetNtProductTypeFromLicenseValue` · `ExpInitSystemPhase1 +0x23c ~ +0x275`.
- ③ cng.sys · kdnet.dll · kdcom.dll PDB 받아서 (`fetch_pdb.py`) IAT 호출 줄에 이름 붙이기 — `RNGSeedVersion` 올리는 때 · `KdDebuggerEnabled` 둘째 비트 뜻.
- ④ (작음) `PspUserThreadStartup → PspNotifyThreadCreation` 앞쪽 — `Cookie` 를 처음 쓰는 스레드가 정말 smss 인지 (`[추정]`).
- ⑤ (flow-writer) `MiProtectSharedUserPage` 는 `.asm` 이 없다 (결과서 objdump). 고정 별칭이 정말 읽기 전용이 되는지 (`[추정]`) 는 그 함수 본문 몫.
- (Q78g2 — ntdll `.00cfg` · 남은 전역 — 는 이미 대기열에 있음, 이 바퀴와 겹침 없음.)

### PE_emulator 쪽 (결과서 "PE_emulator 쪽 한 줄씩" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- 현황 (결과서가 읽음): `Src/nt/AddressSpace.cpp` `_WriteKusdBootValues` (`:462`) 가 **덤프에서 떠 오지 않고** 아홉 칸만 상수 · 계산으로 쓴다 — `NtSystemRoot` · `NtProductType` · `ProductTypeIsValid` · `FullNumberOfPhysicalPages` · `ActiveProcessorCount` · `TickCountMultiplier` · `QpcFrequency` · `CyclesPerYield` · `ProcessorFeatures` — 거기에 시계 셋 (`+0x8` · `+0x14` · `+0x320`). 나머지는 0. 코드 주석 "진짜 쓴 자리 모름 (… Q78g)" 은 이번 결과로 낡았다.
- `K1 이전` 61칸: **부분 맞음** — 위 아홉 + 시계 셋은 값이 맞다. 나머지 줄 있는 칸 (`NtBuildNumber` · `NtMajorVersion` · `SharedDataFlags` · `MitigationPolicies` · `Cookie` · `XState` · `BootId` · `SuiteMask` · `NumberOfPhysicalPages` …) 은 0. ntdll 이 test.exe 초기화에서 어느 칸을 읽는지는 **확인 필요**. 읽는 쪽이 알려진 것: `Cookie` (.NET 갈래만) · `XState` (예외 길에서만) · `UserModeGlobalLogger` (0 맞음).
- **`UnparkedProcessorCount` (`+0x36a`) — 틀림 가능 (확인 필요)**: 에뮬레이터 0, 진짜 2. ntdll `RtlpAcquireSRWLockExclusiveContended +0xed CMP word [0x7ffe036a],1` → `+0xf6 JBE` 가 **≤ 1 이면 스핀을 건너뛴다** — 길이 갈린다 (`RtlFreeHeap` · `RtlpLowFragHeapAllocFromContext` 등 5함수가 읽음, 결과서). 같은 묶음 `ActiveProcessorCount` 는 2 로 맞다. 2 로 써도 되는지는 에뮬레이터 결정.
- `끝까지 0` 41칸: **맞음** — 에뮬레이터는 안 쓰고 0. **`SystemCall` (`+0x308`) 은 0 이어야 맞다** (조건 둘이 안 맞음) — 1 로 쓰면 ntdll 스텁 494곳이 `INT 0x2e` 길로 간다.
- 쓰는 길이 커널 밖인 칸 (`ActiveConsoleId` 등): **맞음** — test.exe 만들기 길에서 쓰는 코드가 안 닿는다 → 입력 값으로 두면 됨. 지금은 0. `ActiveConsoleId` 를 읽는 쪽 (`WTSGetActiveConsoleSessionId` 류) 이 test.exe 길에 있는지는 확인 안 함 `[확인 필요]`.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp pdb 줄에 run115, 근거 로그 run115 줄), 그림 1 `SYSCALL` 갈래 꼬리표 한 마디, 2.2 스텁 절 (`SystemCall` 쓰는 줄), 2.3 머리 run 목록 · KUSD 절 나무 (`KiInitializeXSaveConfiguration` `[정적]` · `KiSetFeatureBits` 정정 · `InitBootProcessor +0x406` 확정) + 새 소절 "KUSD 나머지 칸 — 부팅 때", 3장 bcryptprimitives 절 `RNGSeedVersion` 쓰는 쪽, 4장 (run105 ② · run70 ② ③ · run89 ① 일부 해결 표시 + run115 새 줄 넷), 5장 근거 run 표 · 요청서 번호 표.
- 짐 지도 (`baggage.md`): 기준 run115 줄 · 판 줄, 쓰는 말 단계 (`ⒷR`), 1장 KUSD 줄 + 메모, 2장 KUSD 줄 · 유저 합 · 합 문장 + run115 메모, 2.1 아래 (유저 모름 102 → 0, 아는 칸과 맞춰 보기), 3장 머리 run115 줄 · 줄임 표 (`ⒷI…` · `ⒷM…` · `ⒷX…` · `ⒷP…` · `ⒷR`), **3.7 머리 · 새 줄 31 · 고친 줄 1** (`ProcessorFeatures [23]`), 4장 짝 KUSD 줄 (커널 별칭 `[공백]` → 고정 별칭 + 쓰기 별칭), 5장 `Ⓑ` 2번 · 새 9번 (순서 줄), 7장 머리 · 7.1 · 7.2 KUSD (`(해결됨 → run115)`) · 7.5 `XState` · 7.29 ② ③ · 7.37 ② · 새 7.46, 8장 두 표.
- 이전 run 문서 맨 위 정정 한 줄: 없음.

## 디버거 메모 / 도구 메모

- 결과서 제안 (하네스, 직접 안 고침): ① `refs <바이너리> <전역>` 의 200줄 상한에 걸렸는지 파일 끝에 한 줄로 찍기 (이번엔 objdump 전수와 같은 200이라 안 잘렸지만, 파일만 보고는 모름) ② "전역 포인터 한 개로 쓰는 칸 전수" (레지스터 따라가며 `[포인터 + 칸]` 목적지 모으기 · IAT 호출 줄 → 이름 · 직접 호출 표 위로 사슬) 를 `ghidra-headless-export` 도구 하나로 — `MmRegistryState` · `PspHostSiloGlobals` 같은 질문이 또 나온다.
- flow-writer 메모: Ghidra 가 함수 가운데를 떼어 낸 `FUN_` 조각 (이번 8개) 은 호출 그래프에서 "다른 함수가 부른다" 로 보여 **사슬을 거꾸로 따라가면 엉뚱한 길** 이 나온다 (예 `FUN_1406ee0d6` 의 caller 가 `MiObtainedPageIsGood`). run111 메모 (다른 함수의 조각을 가까운 `FUN_` + 거리로 보여 줌) 와 같은 꼴 — windbg-kd-runbook 이 아니라 ghidra 쪽 함정이라 제안만.

# 2026-11-16 (작성일) run116 — ntdll 전역 `끝까지 0` 나머지 + `.00cfg` (Q78g2, 정적만): 241칸 가름 — **0 을 씀 3 · 조건부 안 탐 99 · 잠금 칸 10 (run114 셋 포함)** 은 3.8 · 3.9 · 3.10 으로 (모름 268 → **157**), 아무도 안 씀 44 · 범위 밖 80 은 7.3 에 표시만. **짐 지도 C (Q78) 정적 조각 끝** — 남은 "아직 모름" 은 구조체 24 (모두 커널) · 전역 157 (**모두 가름 끝**, 셈 열 결정 대기)

(글의 오프셋은 call 자리 · 쓰는 줄 자리. "호출 관계" 나무의 callee 앞 오프셋은 ret 자리 (콜스택 표시) — 둘 다 아래 "정적 — 호출 지점" 표.)

> **지도 범위**: 그림은 안 고쳤다 (run112 ~ run115 와 같은 까닭). 새 단계 · 새 caller 가 없고, 이미 있는 함수 (`LdrpInitializeExecutionOptions` · `LdrpInitializeKernel32Functions` · 뒤9 `+0x1c25`) 의 **안쪽** 한두 걸음만 늘었다. 글은 flow.md 기준 · 2.4 · 4장 · 5장, 짐 지도는 1장 · 2장 · 2.1 · 3.8 · 3.9 · 3.10 · 5장 ② · 6장 · 7장 머리 · 7.1 · 7.3 · 7.5 · 7.45 · 새 7.47 · 8장을 고쳤다.

## 요약 표 (241칸 → ?)

셈 기준은 오케스트레이터가 정했다 (run114 와 같음 + run114 가 넘긴 둘):

- **0 을 씀** · **조건부 안 탐** → 3.8 · 3.9 · 3.10 줄로 (run60 · run66 · run114 선례).
- **아무도 안 씀** · **범위 밖 길** → 7.3 에 두고 `(run116 가름: …)` 표시만. 가름 열을 새로 둘지는 **사용자 결정 대기** (7.45 ② · 7.47 ②).
- **run114 넘김 ①**: `RtlFailedCriticalDebugAllocations` → 3.8 (결과서 "0 을 씀 3" 안에 있다 — 확인).
- **run114 넘김 ②**: 잠금 칸 — 결과서의 잠금 7 + run114 가 7.3 에 둔 잠금 셋 (`LdrpLoaderLock` · `LdrpDllNotificationLock` · `LdrpModuleDatatableLock`) 을 run60 (`LdrpInvertedFunctionTableSRWLock`) · run69 (`TppPoolpListLock`) 처럼 3.8 "잡고 푸는 락 — ② 에서 쓰고 처음 값으로 돌아옴" 으로. **10칸 모두 잡고 푸는 줄이 초기화 길 (지도) 안에 있어서** "아무도 안 씀" 으로 남긴 잠금 칸은 없다 (아래 2절).
- `__@@_PchSym_…` 7칸은 그대로 세고 7.3 에 메모만 (전역 수에서 뺄지 사용자 결정).

| 구역 | 칸 | 0 을 씀 | 조건부 안 탐 | 잠금 칸 (잡고 풂) | 아무도 안 씀 | 범위 밖 `[추측]` | 이미 3장 (sast/111 닫음 7 · run105 1) | → 3장 (새) | 7.3 에 남음 |
|---|---|---|---|---|---|---|---|---|---|
| `.data` `끝까지 0` | 215 | 3 | 76 (+ `AvrfpLoaderEntry` 1 은 이미 3.8) | 7 + 1 (`LdrpModuleDatatableLock`) | 43 | 77 | 8 | **87** | **120** |
| `.mrdata` `끝까지 0` | 22 | 0 | 18 | 0 | 1 | 3 | 0 | **18** | **4** |
| `.00cfg` `못 읽음` | 4 | 0 | 4 | 0 | 0 | 0 | 0 | **4** | **0** |
| (run114 넘김) `.data ≤P1a` 잠금 둘 | 2 | — | — | 2 (`LdrpLoaderLock` · `LdrpDllNotificationLock`) | — | — | — | **2** | 0 |
| **합** | **243** | **3** | **98 (+1)** | **10** | **44** | **80** | **8** | **111** | **124** |

- 결과서 셈 (241) 은 위 앞 세 줄이다: 0 을 씀 3 · 조건부 안 탐 99 (`AvrfpLoaderEntry` 포함) · 아무도 안 씀 51 (쓰는 코드 없음 44 + 잠금 7) · 범위 밖 80 · sast/111 이 닫음 8. 합 241.
- 결과서의 `.data` 215 는 run114 전 7.3 이름 그대로다. 그중 **8 은 이미 3장에 있다** — run114 가 옮긴 7 (sast/111 이 닫은 8 중 `LdrpModuleDatatableLock` 빼고) + run105 의 `AvrfpLoaderEntry` (결과서 표 195번 "조건부 안 탐" — 이미 3.8 짐작 줄). 그래서 지금 7.3 `.data` `끝까지 0` 은 207 = 3 + 76 + 7 + 1 + 43 + 77.
- 그래서 짐 지도 숫자 (2장):
  - ntdll `.data` 쓴 자리 아는 것 200 → **289** (정확 197 → **286** · 짐작 3) · 모름 238 → **149** · ntdll 197 → **286** · nt 6 그대로.
  - ntdll `.mrdata` 아는 것 24 → **42** (정확 23 → **42** · 짐작 1 → **0** — `g_LdrpWow64LdrpInitialize` `[추측]` → `[정적]`, 아래 6절) · 모름 26 → **8** · ntdll 22 → **40**.
  - ntdll `.00cfg` 아는 것 1 → **5** (정확 0 · 짐작 1 → **5** — 결과서 판단 "사진 8장 읽기 불가라 짐작") · 모름 4 → **0** · ntdll 0 → **4**.
  - ntdll 전역 3구역 합: 아는 것 225 → **336** · 모름 268 → **157** · ntdll 219 → **330**.
  - 7.3 사진 구간별 모름: `K1 이전` 2 · `P1a~P1b` 1 · `≤P1a` 32 → **30** · `끝까지 0` 229 → **124** (`.data` 207 → 120 · `.mrdata` 22 → 4) · 못 읽음 4 → **0**.
  - 아는 전역 중 끝까지 0 (7.1): 67 → **172** (= 296 − 124).
  - "0 그대로 (사진)" 열 (`.data` 265* · `.mrdata` 31*) 은 **그대로** (사진 분류 — run66 선례).

## 환경

- 26100.9457, **정적만** (VM 실행 없음).
- 정적: **ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`** (`ms/sym/ntdll.dll.pdb.info.txt` — sast/111 과 같음), ntoskrnl pdb 정보 파일 `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음). **ntoskrnl Ghidra 프로젝트는 안 열었다** (그때 sast/112 가 도는 중).
- 버전 대조 (결과서): `LdrInitializeThunk +0xe → LdrpInitialize` · `LdrpInitialize +0x3c ⇒ LdrpInitializeInternal` · `LdrpInitializeInternal +0x5a → _LdrpInitialize` · `_LdrpInitialize +0x16c → LdrpInitializeProcess` · `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse` · `LdrpInitializeNode +0x16b → LdrpCallTlsInitializers` · `+0x19c → LdrpCallInitRoutine` · `LdrpLoadDll +0x100 → LdrpLoadDllInternal` 가 `_calls.tsv` 와 같다. 본문 `함수 +0x…` 508쌍을 결과서 스크립트가 objdump 풀이 명령 시작과 맞춤 (불일치 0).
- 사진 값 대조: 결과서가 run44 덤프 읽기 로그 셋 (`ms/logs/04_baggage-read_{P1a,P1b,P2}.txt`) 에서 **이름 칸 첫 `0x20` 바이트가 `.data` · `.mrdata` 237칸 모두 0** 임을 다시 봤다 (7.3 `끝까지 0` 라벨 정정 없음). `.00cfg` 는 8장 모두 `????`. 새 동적 실행은 없다.
- **sast/113 결과만 넣었다.** sast/112 · 114 ~ 120 은 안 넣었다.

## 목적

- **Q78g2** (Q78g 의 나머지): 짐 지도 7.3 의 ntdll `.data` `끝까지 0` 215 · `.mrdata` `끝까지 0` 22 · `.00cfg` `못 읽음` 4 — Q78 짐 지도 C 의 **마지막 정적 조각**.
- run114 가 넘긴 둘: `RtlFailedCriticalDebugAllocations` 옮기기 · 잠금 칸 셈 맞추기.

## 방법

- 정적: `_workspace/sast/113_ntdll-globals-zero.md` — ntdll, depth 0 `^이름$`.
  - 새로 뽑음 15개 (한 번): `ExecuteHotpatchTestRuntimeFunction` · `LdrpPayloadRestrictionMitigationsEnabled` · `LdrpInitializeCriticalSectionExceptionGlobalMitigation` · `LdrpInitializeSmtDelayedSleepQueryRegistry` · `RtlpLoadNlsData` · `LdrpGetShimEngineInterface` · `LdrpBuildImportRedirection` · `RtlpMoveActCtxToFreeList` · `IsFeatureEnabled` · `ApiSetpResolveHost` · `ApiSetpIsFeatureEnabled` · `RtlpAreLegacyDosDeviceNamesEnabled` · `RtlpInitializeStackTraceDatabase` · `EtwpInsertGuidEntry` · `LdrpGetFileSizeFromLoadAsDataTable`. `_calls.tsv` 4,328 → 4,408 줄 (줄어든 줄 0).
  - 읽기만: `LdrpInitializeProcess` · `_LdrpInitialize` · `LdrpInitializeExecutionOptions` · `LdrpInitializePolicy` · `LdrpCfgProcessLoadConfig` · `LdrpCfgCheckRoutineCallback` · `LdrpCfgDispatchRoutineCallback` · `LdrAddLoadAsDataTable` · `LdrpLoadWow64` · `RtlSetHeapInformation` · `LdrpMapDllNtFileName` · `wil_details_*` · `LdrpInitializeSmtDelayedSleep` 등.
  - 훑기 (결과서 스크래치, 커밋 안 함): ntdll 전체 objdump 풀이의 RIP 상대 목적지로 이름 칸에 가는 명령을 쓰기 · 읽기 · `lea` 로 가름 + 지도 깊이 (`m0 ~ m10` / `out`). **한계: 레지스터에 담은 포인터로 쓰는 줄은 안 잡힘** — WOW64 표 · WIL 기술자 · `EtwpInsertGuidEntry` · `LdrpHandleTlsData` 는 `.asm` 을 읽어 보탬. `out` 중 지도 안 호출자가 있는 17개는 3단까지 거슬러 다시 봄.
- 결과서: `_workspace/sast/113_ntdll-globals-zero_result.md` (**분석성공** — 스크립트 문제 없음, 제안 셋).
- flow-writer 가 다시 본 것:
  - ntdll `.asm` 줄: `ExecuteHotpatchTestRuntimeFunction +0x0 ~ +0x21` (`+0xb JNZ` 목적지 `0x18013463f` = `+0x17`) · `LdrpInitializeCriticalSectionExceptionGlobalMitigation +0x1b` · `+0x22` · `+0x44` · `+0xac` · `+0xb5` · `+0xc3` · `+0xc8` · `+0xca` · `LdrpInitializeProcess +0x693` · `+0x76b` · `+0x76d` · `+0x773` · 잠금 칸 lea `_LdrpInitialize +0xf3` · `LdrpSendPostSnapNotifications +0x30` · `LdrpFindExistingModule +0x21` · `LdrpComputeLazyDllPath +0x19` · `LdrpReleaseDllPath +0x2f` · `RtlpComputeDllPath +0x52` · `RtlpAddDebugInfoToCriticalSection +0xbe` · `SbUpdateSwitchContextBasedOnDll +0x1ea`.
  - `.asm` 이 없어 결과서 objdump 풀이만 있는 줄 (`[정적: objdump]`): `LdrpGetFromMUIMemCache +0xce` (`MuiCacheSWRLock`) · `RtlpCallSecureMemoryCallbacks +0x2a` (`RtlpSecMemLock`) · `LdrpFindLoadedDllByName +0x17a` (`LdrpModuleDatatableLock`).
  - `_calls.tsv`: 아래 표의 call · ret 줄.

## 결과

비유 (새 가게 = test.exe 프로세스, ntdll `.data` = 가게 뒤 창고 선반):

- 이번엔 **끝까지 빈 칸** 241개를 다 열어 봤다. 빈 칸이라도 사정이 다 다르다.
- 3칸은 직원이 문 열 때 **"0" 이라고 또박또박 적은** 칸이다 (원래 비어 있어서 겉보기는 같다).
- 99칸은 **"특별한 가게일 때만"** 채우는 칸이다 — 앱 검사기 · shim · 32비트 · 매니페스트 · CFG 강제 같은 조건이 test.exe 엔 없다.
- 10칸은 **자물쇠** 칸이다. 문 여는 길에 잠갔다가 다시 풀어서, 사진엔 처음 모양 그대로 찍힌다.
- 124칸은 이 가게 문 여는 길에선 **아무도 안 만지거나** (44), **다른 날 (종료 · 오류 · 공개 API) 에만** 쓰는 칸 (80) 이다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x199d` | `+0x19a2` | `LdrpInitializeKernel32Functions` | 뒤5 kernel32 준비 (run7 관찰) | [정적] |
| `ntdll!LdrpInitializeKernel32Functions` | `+0xfa` · `+0x12b` | `+0xff` · `+0x130` | `LdrGetProcedureAddressForCaller` | 함수 주소 찾기 (이 아래 맨 앞에서 핫패치 시험 함수) | [정적] |
| `ntdll!LdrGetProcedureAddressForCaller` | `+0x53` | `+0x58` | `ExecuteHotpatchTestRuntimeFunction` | **늘** — 안 `+0x17` 이 `RuntimeGlobal` 에 0. 바로 뒤 `+0x58 GetHotpatchTestRuntimeFunctionState` 가 그 칸을 읽음 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x531` | `+0x536` | `LdrpInitializeExecutionOptions` | 묶음 4 IFEO 읽기 | [정적] (run18 그대로) |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x9d` | `+0xa2` | `LdrpInitializeCriticalSectionExceptionGlobalMitigation` | **늘** — IFEO 건너뛰기 (`+0xa6` · `+0xad`) 앞 | [정적] |
| `ntdll!LdrpInitializeCriticalSectionExceptionGlobalMitigation` | `+0x4b` · `+0x7d` · `+0x89` | `+0x50` · `+0x82` · `+0x8e` | `NtOpenKey` · `NtQueryValueKey` · `NtClose` | `Session Manager` 키 · 값 `RaiseExceptionOnPossibleDeadlock` — 보안 프로세스가 아닐 때 (`+0x1b CMP [LdrpIsSecureProcess],0` · `+0x22 JNZ → +0xac`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1c25` | `+0x1c2a` | `LdrpInitializeSmtDelayedSleep` | 뒤9 SMT 지연 잠 (늘) | [정적] (run19 그대로) |
| `ntdll!LdrpInitializeSmtDelayedSleep` | `+0x1b` · `+0x2e` | `+0x20` · `+0x33` | `NtOpenKey` · `NtClose` | 키 열기 · 닫기 | [정적] |
| `ntdll!LdrpInitializeSmtDelayedSleep` | `+0x4c` · `+0x64` · `+0x7c` · `+0x94` · `+0xac` | `+0x51` · `+0x69` · `+0x81` · `+0x99` · `+0xb1` | `LdrpInitializeSmtDelayedSleepQueryRegistry` | DWORD 다섯 — 값이 있을 때만 `+0x75` 가 `SmtDelayedConfiguration` 칸에 씀 | [정적] |
| `ntdll!LdrpInitializeSmtDelayedSleepQueryRegistry` | `+0x40` | `+0x45` | `NtQueryValueKey` | | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1713` | `+0x1718` | `AVrfInitializeVerifier` | AppVerifier 만 (`+0x1568` · `+0x156f JE → +0x1753`) | [정적] |
| `ntdll!LdrpInitializeApplicationVerifierPackage` | `+0xf2` | `+0xf7` | `AVrfInitializeVerifier` | 위와 같은 묶음의 다른 자리 (3.9 `AvrfAppVerifierMode` 줄) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x17c1` | `+0x17c6` | `LdrpLoadWow64` | WOW64 만 (`+0x1766` · `+0x176d JE → +0x1806`) | [정적] |
| `ntdll!LdrpLoadWow64` | `+0xf1` | `+0xf6` | `LdrGetProcedureAddress` | `.rdata` `0x173830` 표 6항목 {이름, 칸 주소} 를 돌며 칸을 채움 (`+0xd7 CMP EDI,6`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x195e` | `+0x1963` | `LdrpInitializeImportRedirection` | → `+0xf1` `LdrpBuildImportRedirection` — `RedirectionDllName` 이 비면 `+0x56 JNE` 로 일 안 함 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x19ef` | `+0x19f4` | `LdrpInitShimEngine` | `PEB.pShimData` 0 이면 안 부름 (`+0x19da` · `+0x19e3 JE → +0x19f4`) → `+0x109 LdrpGetShimEngineInterface` | [정적] |
| `ntdll!ApiSetpResolveHost` | `+0x204` | `+0x209` | `ApiSetpIsFeatureEnabled` | ApiSet 항목에 기능 번호가 붙었을 때만 → `+0x4 IsFeatureEnabled` | [정적] |
| `ntdll!RtlpGetProcessCodepagesForLocale` | `+0x28` | `+0x2d` | `RtlpLoadNlsData` | 매니페스트 `activeCodePage` 가 있을 때만 (sast/52) | [정적] |
| `ntdll!RtlReleaseActivationContext` | `+0xaa` | (꼬리 JMP) | `RtlpMoveActCtxToFreeList` | 보존 스위치 `g_SxsKeepActivationContextsAlive` 가 켜질 때만 (`+0x92 CMP` · `+0x9c JNE`) | [정적] |
| `ntdll!LdrpProcessDetachNode` | `+0x58` | `+0x5d` | `LdrpRecordUnloadEvent` | DLL 을 실제로 내릴 때만 | [정적] |
| `ntdll!LdrResSearchResource` | `+0x727` | `+0x72c` | `LdrAddLoadAsDataTable` | 파일 경로 자원 갈래 — **test.exe 는 `+0x37` 에 안 들어감** (run114 반영 그대로) | [정적 + 사진] |
| `ntdll!LdrpCfgProcessLoadConfig` | `+0x70c` | `+0x711` | 간접 (`indirect:cfg`) — 콜백 셋 | CFG 강제일 때만 — `+0x3a9 TEST R12D` · `+0x3ac JZ → +0x454` 로 test.exe 는 반환 | [정적] |
| `ntdll!RtlControlStackTraceDataBase` | `+0x25` | `+0x2a` | `RtlpInitializeStackTraceDatabase` | 사용자 스택 추적 DB (`NtGlobalFlag 0x1000`) 일 때만 | [정적] |

### 호출 관계

```
[test.exe 첫 스레드 — ② ntdll]
_LdrpInitialize  +0xf3 lea LdrpLoaderLock (잡고 풂 — P1b 잡힌 사진)
 └─ +0x16c LdrpInitializeProcess
    +0x693 R12D = 0 … +0x773 RtlFailedCriticalDebugAllocations = 0   (무조건)
    ├─ +0x536  LdrpInitializeExecutionOptions
    │          └─ +0xa2 LdrpInitializeCriticalSectionExceptionGlobalMitigation
    │                   +0x44 RtlpRaiseExceptionOnPossibleDeadlock = 0
    │                   ├─ +0x50 NtOpenKey(Session Manager)
    │                   ├─ +0x82 NtQueryValueKey(RaiseExceptionOnPossibleDeadlock)   (값 > 0 이면 +0xac 1 — 안 탐)
    │                   └─ +0x8e NtClose
    │          +0xa6 / +0xad  IFEO 통째 건너뜀 (Flags 0x4001) — IFEO 쪽 칸 넷 안 탐
    ├─ +0x1718 AVrfInitializeVerifier      (AppVerifier 만 — 안 탐, 칸 15)
    ├─ +0x17c6 LdrpLoadWow64               (WOW64 만 — 안 탐, .mrdata 칸 5 + g_LdrpWow64LdrpInitialize)
    ├─ +0x1963 LdrpInitializeImportRedirection   (RedirectionDllName "" → 일 안 함, 칸 4)
    ├─ +0x19a2 LdrpInitializeKernel32Functions
    │          └─ +0xff / +0x130 LdrGetProcedureAddressForCaller
    │                   └─ +0x58 ExecuteHotpatchTestRuntimeFunction   +0x17 RuntimeGlobal = 0 (늘)
    ├─ +0x19f4 LdrpInitShimEngine          (pShimData 0 → 안 부름, .mrdata 칸 9)
    ├─ … LdrpSendPostSnapNotifications +0x30 lea LdrpDllNotificationLock (잡고 풂)
    ├─ … LdrpFindExistingModule +0x21 lea LdrpModuleDatatableLock (잡고 풂)
    ├─ … LdrpSearchPath → LdrpComputeLazyDllPath +0x19 LdrpPathLock · RtlpGetCachedPath → RtlpComputeDllPath +0x52 LdrpDllDirectoryLock (잡고 풂)
    └─ +0x1c2a LdrpInitializeSmtDelayedSleep
               ├─ +0x20 NtOpenKey  · +0x33 NtClose
               └─ +0x51 … +0xb1 LdrpInitializeSmtDelayedSleepQueryRegistry ×5 ─ +0x45 NtQueryValueKey
                        (+0x75 SmtDelayedConfiguration — 값이 있을 때만, 이 VM 은 없음)
```

### 1. 0 을 씀 3 — 결과서 1.1

| 이름 (rva, 크기) | 쓰는 줄 | 왜 늘 타나 | 사진 |
|---|---|---|---|
| `RuntimeGlobal` (`0x1cddb8`, 4B) | `ExecuteHotpatchTestRuntimeFunction +0x17 MOV [칸],0` | `+0x0 LEA RAX,[칸]` · `+0x7 CMP RAX,0x20` · `+0xb JNZ → +0x17` — 칸 주소는 0x20 이 아니라 늘 뛴다. `+0xd` 의 `0x14` 는 죽은 줄 (핫패치 시험이 본문을 바꿀 때만) | `끝까지 0` |
| `RtlFailedCriticalDebugAllocations` (`0x1d272c`, 4B) | `LdrpInitializeProcess +0x773 MOV [칸],R12D` (`+0x693 XOR R12D,R12D`) | `+0x76b JGE → +0x773` 이든 아니든 같은 줄. `+0x693 ~ +0x773` 로 끼어드는 점프 없음 (결과서가 목적지 전부 셈). 키우는 줄 `RtlpAddDebugInfoToCriticalSection +0x2bd LOCK INC` 는 디버그 정보 할당 실패 때만 | `끝까지 0` |
| `RtlpRaiseExceptionOnPossibleDeadlock` (`0x1d4009`, 1B) | `LdrpInitializeCriticalSectionExceptionGlobalMitigation +0x44 MOV BYTE [칸],0` (1 로 켜는 줄 `+0xac`) | 부르는 곳 `LdrpInitializeExecutionOptions +0x9d` 가 IFEO 건너뛰기 앞이라 늘. 보안 프로세스면 바로 1 (`+0x22 JNZ → +0xac`), 아니면 0 → 키 열고 값 읽기 → 종류 4 · 길이 4 · 값 > 0 (`+0xb5` · `+0xbc` · `+0xc3` · `+0xc8 JBE`) 이면 `+0xca → +0xac` 1. test.exe 는 `LdrpIsSecureProcess` 0 (`0x1d28e0` P1a · P1b · P2 `[관찰]`) · 값 없음 → 0 | `끝까지 0` |

- `RtlFailedCriticalDebugAllocations` 는 **run114 넘김 ①** — 오케스트레이터 셈대로 이번에 3.8 로 옮겼다 (7.5 실마리 줄 `(해결됨 → run116)`).
- sast/111 이 "init 줄기 직접 줄이라 0 을 씀 후보" 로 넘긴 다섯 중 이것 하나만 0 을 쓴다. 나머지 넷은 조건부 안 탐 (아래 "결과서와 다르게 적은 곳" 이 아니라 결과서 자체의 정정 — 3절 표).

### 2. 잠금 칸 10 — 잡고 풀어 처음 값으로 돌아옴 (run114 넘김 ②)

판단 기준 (한 줄): **잡고 푸는 줄 (lea 로 잠금 함수에 주소를 넘기거나 SRW 빠른 길 `LOCK CMPXCHG` · `LOCK BTS`) 이 지도 안 함수에 있으면 3.8**, 없으면 "아무도 안 씀". 10칸 모두 지도 안에 줄이 있어 모두 3.8 로 갔다.

| 이름 (rva) | 구간 (7.3) | 잡고 푸는 줄 (대표) | 그 길을 test.exe 가 타나 | 근거 |
|---|---|---|---|---|
| `LdrpLoaderLock` (`0x1cc898`, 임계구역) | `≤P1a*` | `_LdrpInitialize +0xf3 LEA` (②) · 공용 `RtlEnterCriticalSection` · `RtlLeaveCriticalSection` 이 `+0x8 ~ +0x10` 만 바꿈 | 탐 — P1b 사진이 **잡힌 상태** (`LockCount −2` · `RecursionCount 1` · `OwningThread 0x26ec`), P2 풀림 `[관찰]` run44 | `[정적]` sast111 · `.asm` |
| `LdrpDllNotificationLock` (`0x1cc910`, 임계구역) | `≤P1a` | `LdrpSendPostSnapNotifications +0x30 · +0xeb` 등 lea 14곳 | 탐 (모듈 알림 길 — run66) · 사진 3장 풀린 상태 | `[정적]` sast111 · `.asm` |
| `LdrpModuleDatatableLock` (`0x1d2458`, SRW 8B) | `끝까지 0` | `LdrpFindExistingModule +0x21 LEA` · `LdrpFindLoadedDllByName +0x17a` 외 SRW 빠른 길 수십 곳 `[추정]` | 탐 (모듈 찾기) · 사진 3장 0 (풀림) | `[정적]` sast111 · `.asm` (`+0x17a` 는 `[정적: objdump]`) |
| `RtlCriticalSectionLock` (`0x1d2450`) | `끝까지 0` | `RtlpAddDebugInfoToCriticalSection +0xbe LEA` (m6) · `RtlDeleteCriticalSection +0x84 · +0xba` (m7) | 탐 — 같은 함수가 `RtlCriticalSectionList` 를 씀 (run114, 사진 P1a 항목 둘) | `[정적]` sast113 · `.asm` |
| `RtlpCachedPathLock` (`0x1d2320`) | `끝까지 0` | `LdrpReleaseDllPath +0x2f LEA` · `+0x55 LOCK BTS` (m5) · `LdrpResolveProcedureAddress +0x283 · +0x298` · `RtlReleasePath +0x12 · +0x28` (m7) | 탐 (DLL 경로 풀기) — 정확한 첫 줄은 동적 | `[정적]` sast113 · `.asm` |
| `LdrpPathLock` (`0x1d2318`) | `끝까지 0` | `LdrpComputeLazyDllPath +0x19 · +0xbf LEA` (m8) | 탐 — run114 가 같은 사슬 아래 `0x1d2358` P1b `0xb0` 을 봄 (`LdrpSearchPath +0x36d → LdrpComputeLazyDllPath`) | `[정적]` sast113 · `.asm` |
| `LdrpDllDirectoryLock` (`0x1d2388`) | `끝까지 0` | `RtlpComputeDllPath +0x52 · +0x7f · +0xaf · +0xce LEA` (깊이 11, `RtlpGetCachedPath +0xbe · +0xe9` 가 부름) | 탐 — 같은 사슬 (`RtlpGetCachedPath → RtlpComputeDllPath → RtlpLookupCurDirSetting`, run114). `LdrSetDllDirectory` · `LdrAddDllDirectory` 쪽 `LOCK CMPXCHG` 는 안 탐 | `[정적]` sast113 · `.asm` |
| `SbpContextLock` (`0x1d25e0`) | `끝까지 0` | `SbUpdateSwitchContextBasedOnDll +0x1ea · +0x247 · +0x2a4 LEA` (m7) · `SbpUpdateCache +0x62 · +0x75` | 함수는 탐 (`SbDynamicContextDllCount+0x4` P1b 3, run114). 이 잠금 줄이 같은 갈래인지는 안 풂 `[추정]` | `[정적]` sast113 · `.asm` |
| `MuiCacheSWRLock` (`0x1ce420`) | `끝까지 0` | `LdrpGetFromMUIMemCache +0xce LOCK CMPXCHG` (m10) · lea `LdrpSetAlternateResourceModuleHandle +0x58 · +0x45b` · `LdrUnloadAlternateResourceModuleEx +0x2c · +0x1d1` | 줄은 지도 안 (`LdrResSearchResource +0x30b → LdrLoadAlternateResourceModuleEx +0x119`), 그 갈래를 탔는지는 모름 — 조건부일 수 있음 `[추정]` | `[정적: objdump]` sast113 |
| `RtlpSecMemLock` (`0x1ce6f8`) | `끝까지 0` | lea `RtlpCallSecureMemoryCallbacks +0x2a · +0x69 · +0x8f · +0xf9` (m8) · 공개 API `RtlRegister/DeregisterSecureMemoryCacheCallback` | 줄은 지도 안 (보안 메모리 해제 콜백 길), 탔는지는 모름 — 조건부일 수 있음 `[추정]` | `[정적: objdump]` sast113 |

- 처음 값은 10칸 모두 파일 그대로 (임계구역 둘은 파일 값, 나머지 여덟은 0). 사진 P2 에서 모두 처음 값.
- 마지막 둘은 "잡고 푸는 줄이 길 안" 이라 셈 기준대로 옮겼지만 **탔다는 근거는 없다** — 근거 칸에 `조건부일 수 있음` 을 달았다 (아래 "아직 확인 필요" 결정 ③).

### 3. 조건부 안 탐 99 — test.exe 가 왜 안 타나 (결과서 표 · 공통 조건 ⓐ ~ ⓙ)

| 묶음 | 칸 | 구역 | 대표 쓰는 줄 | 조건 — test.exe |
|---|---|---|---|---|
| AppVerifier ⓐ | `AVrfpHandleTraces` · `AVrfpDebug` · `AVrfpVerifierDllsString` · `AVrfpEnabledSystemWide` · `AVrfpVerifierProvidersList` · `AVrfpVerifierFlags` · `AVrfpVerifierStopMessageFunction` · `RtlpCriticalSectionVerifier` (8) | `.data` | `AVrfInitializeVerifier +0x36e · +0x391` (lea 출력) · `+0x30f` · `+0x353` · `+0x6d` · `+0x303 · +0x340` · `AVrfpVerifierStopInitialize +0x93` · `AVrfpEnableVerifierOptions +0x8b` | `AvrfAppVerifierMode` 0 → `LdrpInitializeProcess +0x1568 CMP · +0x156f JE → +0x1753` |
| AppVerifier (다른 문) | `AVrfpVerifierLock` · `AVrfpCurrentUserKeyPath` · `LdrpShouldCreateStackTraceDb` (3) | `.data` | `AVrfDllLoadNotification +0x44 · +0x5f` (lea) · `AVrfpFormatCurrentUserKeyPath +0x9` · `LdrpInitializeApplicationVerifierPackage +0xd8 · +0x143` | `LdrpSendPostSnapNotifications +0x88 AND EBP,0x100` · `+0x97 JNE` (`NtGlobalFlag` 0) / `LdrpInitializeExecutionOptions +0x635 TEST [KUSD+0x3a0],1` · `+0x63d JE` / `+0x696 TEST [PEB+0xbc],0x2000100` · `+0x6a0 JNZ` · `+0x6a6 LdrpPayloadRestrictionMitigationsEnabled` |
| AppVerifier `.mrdata` ⓐ | `AvrfpAPILookupCallbacksEnabled` · `AVrfpEnabled` · `RtlGuardAllowSuppressedCalls` · `AvrfpAPILookupCallbackRoutine` (4) | `.mrdata` | `AVrfInitializeVerifier +0x1d3 ~ +0x1ea` · `LdrpInitializeExecutionOptions +0x37d` (`RtlGuardAllowSuppressedCalls` = 1, IFEO 쪽) | ⓐ · IFEO 건너뜀 (`+0xa6` · `+0xad`) |
| shim 엔진 콜아웃 ⓑ | `g_pfnSE_GetProcAddressForCaller` · `g_pfnApphelpQueryModSettingsAllocProc` · `g_pfnSE_DllUnloaded` · `g_pfnSE_ProcessDying` · `g_pfnSE_ShimDllLoaded` · `g_pfnSE_InitializeEngine` · `g_pfnSE_LdrResolveDllName` · `g_pfnSE_InstallBeforeInit` · `g_pfnSE_LdrEntryRemoved` (9) | `.mrdata` | `LdrpGetShimEngineInterface +0x4a9 ~ +0x559` | `PEB.pShimData` 0 → `+0x19da` / `+0x19e3 JE → +0x19f4` (sast/14 · 61) |
| WOW64 ⓒ | `Wow64ApcRoutine` · `Wow64PrepareForException` · `g_LdrpWow64PrepareForDebuggerAttach` · `g_LdrpWow64SuspendLocalProcess` · `g_LdrpWow64SuspendLocalThread` (5) | `.mrdata` | `LdrpLoadWow64 +0xe8 ~ +0xf1` (표 구동 — 칸 주소가 `.rdata` `0x173830` 표에만) | `UseWOW64` 0 → `+0x1766` · `+0x176d JE → +0x1806` |
| WIL ApiSet 기능 갈래 ⓔ | `Feature_ProjectTessera_CreateProcessInSandbox` · `Feature_DriverSetup_ReofferFingerprinting_v2` · `Feature_LearningModeTraceService` 의 `__private_reporting` 3 + `__private_featureState` 3 · 필요 기능 `Feature_TestLoc03` · `Feature_Tessera_202605Retail` 의 `__private_featureState` 2 (8) | `.data` | `…Reevaluate +0xa0` · `wil_details_FeatureReporting_*` (기술자 `+0x10`) | `ApiSetpResolveHost +0x1ed ~ +0x202` — ApiSet 항목에 기능 번호 `0x3a132a5` · `0x3a52a68` · `0x3b76c1d` 가 붙었을 때만 `+0x204` |
| WIL 패키지 앱 갈래 | `Feature_PackagedComElevationSupport_v2` · 필요 기능 `Feature_UxAccOptimization` · `Feature_Standalone_Future` 의 `__private_featureState` (3) | `.data` | `LdrpInitializePolicy +0xf4 · +0x17b · +0x1ea · +0x259 · +0x2d1` → `…Reevaluate +0xa0` | `LdrpInitializePolicy +0x46 TEST AL,0x10` · `+0x48 JNZ` — 패키지 앱만 (test.exe `BitField 0x4` `[관찰]`) |
| WIL DOS 장치 이름 | `Feature_TreatDosDevicePathsAsNormalFiles` 의 `__private_reporting` · `__private_featureState` (2) | `.data` | `RtlpAreLegacyDosDeviceNamesEnabled +0x11e · +0x12d · +0x136` | `RtlpIsDosDeviceName_Ustr +0x536 ~ +0x53c` 뒤 `+0x542` — 옛 DOS 장치 이름 비교 갈래 `[추정]` |
| SxS 보존 | `g_SxsCurrentDeadActivationContexts` · `g_SxsFreeActivationContexts` · `g_SxsLiveActivationContexts` (3) | `.data` | `RtlpMoveActCtxToFreeList +0x71 ~ +0xbd` · `RtlpEnsureLiveDeadListsInitialized` · `RtlpPlaceActivationContextOnLiveList +0x3e` | `RtlReleaseActivationContext +0x92 CMP g_SxsKeepActivationContextsAlive` · `+0x9c JNE` — 스위치 칸을 쓰는 코드 없음 |
| IFEO | `RtlpShutdownProcessFlags` · `LdrpUseImpersonatedDeviceMap` · `RtlpModernAppKey` (3) | `.data` | `LdrpInitializeExecutionOptions +0x1a2` (lea) · `+0x279` · `RtlOpenModernAppOptionsKey +0x77` | IFEO 통째 건너뜀 `+0xa6 TEST [PP+8],0x4000` · `+0xad JNZ → +0x604` (`Flags 0x4001` `[관찰]`) |
| 언로드 기록 ⓕ | `RtlpUnloadEventTraceEx` · `LdrpUnloadIndex` · `LdrpUnloadIndexEx` · `RtlpUnloadEventTrace` (4) | `.data` | `LdrpRecordUnloadEvent +0x22 · +0x47 · +0xb6 · +0x119 · +0x1f2` | DLL 을 실제로 내릴 때만 (`LdrpProcessDetachNode +0x58`) — init 길의 부르는 곳은 오류 · 옵션 갈래 |
| import 재지정 ⓓ | `LdrpRedirectionTree` · `LdrpRedirectionByFunctionCalloutFunc` · `LdrpRedirectionModule` · `LdrpRedirectionCalloutFunc` (4) | `.data` | `LdrpInitializeImportRedirection +0x18f` · `LdrpBuildImportRedirection +0x3d · +0x57` (lea) · `LdrpAddRedirectedFunction +0x148 · +0x249` | `+0x53 CMP [RedirectionDllName.Length]` · `+0x56 JNE → +0x80` — 비면 반환 (test.exe `""` `[관찰]` P2:3252) |
| 경로 | `RtlpSearchPath` · `RtlpExeSearchPath` · `RtlpDllSearchPathWithOptions` · `CentennialIssueTracker_InitializedState` (4) | `.data` | `RtlpSignalSystemDirsModification +0x27 · +0x36` · `LdrSetDllDirectory +0x21d` · `LdrAddDllDirectory +0x1e8` · `LdrpLogRelativePathWithAlteredSearchError +0x49 · +0x64` | `TermsrvGetWindowsDirectoryW` 길이 0 (`LdrpInitializePerUserWindowsDirectory +0x4c`, run114) · `DllPath ""` (`LdrpInitializePolicy +0x4e` · `+0x51 JE`) · 상대 경로 오류 보고 갈래 `[추정]` |
| LoadAsData 표 ⓖ | `LoadAsDataTableBlockCount` · `LoadAsDataTable` · `LoadAsDataTableCount` (3) | `.data` | `LdrAddLoadAsDataTable +0x142 · +0x172 · +0x179` | `LdrResSearchResource +0x727` 파일 경로 자원 갈래 — `+0x37` 로 들어오면 반드시 써지는데 사진 0 → 안 탐 |
| MUI 대체 모듈 ⓗ | `AlternateResourceModules` · `AlternateResourceModuleCount` · `AltResMemBlockCount` (3) | `.data` | `LdrpSetAlternateResourceModuleHandle +0x2ac ~ +0x365` | `.mui` 대체 모듈을 처음 올릴 때만 (`LdrResSearchResource +0x30b`) |
| MUI 임계구역 · 언어 키 | `InitRegistryInfoCritSect` · `RegistryInfoCritSect` · `gCustomCultureRegKey` · `gLocaleDataRegKey` (4) | `.data` | `RtlpInitMuiCriticalSection +0x1a · +0x3d · +0x50` · `RtlpGetCustomCultureRegKey +0x42` · `RtlpGetLocaleDataKey +0x7c` | MUI 자원 구성 · 언어 대체 목록 · 언어 이름 갈래 (`LdrpGetRcConfig +0xbe` · `LdrIsResItemExist +0x97` · `LdrResFallbackLangList +0x4a2` · `LdrResSearchResource +0x3de`) — 사진 0 = 안 탐 |
| NLS | `gLocaleTables` · `pTblPtrs` (2) | `.data` | `RtlpLoadNlsData +0x6e ~ +0xd1` | 매니페스트 `activeCodePage` 가 있을 때만 (`LdrpFusionManifestCodePages +0x4e` 음수 → `+0x57 ~ +0x5a` 0 반환, sast/52) |
| 힙 디버그 · 추적 · 태그 | `RtlpHpStackTrackingContext` · `RtlpStackTraceDatabase` · `RtlpHeapInvalidBreakPoint` · `RtlpHpTagInitVar` · `RtlpHpTagContext` · `TrkContext` (6) | `.data` | `RtlpHpStackTraceEnable +0x32 ~ +0x82` · `RtlpInitializeStackTraceDatabase +0x2d` · `RtlpBreakPointHeap +0xf · +0x17` · `RtlSetHeapInformation +0x96 · +0xa4` (lea) · `RtlpHpTagContextAllocateTag +0x16a` · `RtlpHeapTrkSyncWithDiagnoser +0x5a` | `NtGlobalFlag` 0 (`+0x8a3 TEST [PEB+0xbc],0x1000` · `RtlpCreateHeap +0x34e TEST EDI,0x61000000`) · `RtlSetHeapInformation` 클래스 4 만 (init 호출은 1 · 7) · 힙 추적이 켜진 힙만 `[추정]` |
| 스레드 풀 | `TppPoolpSerializedPoolLock` · `TppPoolpSerializedPool` · `TppCleanupGroupMemberpNoPoolListLock` (3) | `.data` | lea `TppCleanupGroupMemberInitialize +0x249 · +0x28f · +0x296` | `+0x146 JNE → +0x28f` 직렬화 풀 갈래 `[추정]` · 풀 없는 멤버 갈래 (`+0x17d JE → +0x249`, run114 `TppCleanupGroupMemberpNoPoolList` 와 같음) |
| 오류 보고 | `LdrpCODScenarioTriggered` · `LdrpCODScenarioLock` · `LdrpFatalHardErrorCount` (3) | `.data` | `LdrpCheckComponentOnDemandEtwEvent +0xaa · +0x11f · +0x160` · `LdrpMapDllNtFileName +0x213` · `LdrpReportError +0x26a` | DLL 열기 접근 거부 (`0xc0000022`) · 하드 에러 · DLL 못 찾음 갈래 |
| 복제 자식 | `LdrpForkActiveLock` · `LdrpForkConditionVariable` (2) | `.data` | `_LdrpInitialize +0x336 · +0x33d` | `+0x62 CMP [PEB+0],BL` · `+0x64 JNE → +0x284` — `InheritedAddressSpace ≠ 0` (`RtlCloneUserProcess` 자식) 만. test.exe 0 `[관찰]` |
| TLS | `LdrpDelayedTlsReclaimTable` · `LdrpPotentialTlsLeaks` (2) | `.data` | `LdrpQueueDeferredTlsData +0x1a` (lea) · `LdrpHandleTlsData +0x5a9` | `LdrpHandleTlsData +0x428 JNE` · `+0x46c JNE` — 지연 반환 · 이미 쓰이는 슬롯 갈래 `[추정]` |
| 임계구역 | `PossibleDeadlockReportCounter` · `RtlpForceCSToUseEvents` (2) | `.data` | `RtlpPossibleDeadlock +0x84` · `LdrpInitializeProcess +0x9bb` (1) | 오래 기다릴 때만 (`RtlpTimeoutDisable` 1) `[추정]` · `PEB.AppCompatFlags` 비트 22 (`+0x9ae BT` · `+0x9b3 JAE`, test.exe 0 `[관찰]`) |
| 그 밖 1씩 | `RtlpProtectedPoliciesSRWLock` · `EtwpGuidEntryTable` · `LibLoaderTelemetryInitRunOnce` · `SmtDelayedConfiguration` (4) | `.data` | lea `RtlUnwindEx +0xaed` · `EtwpInsertGuidEntry +0x2e` (lea `R15` 뒤 트리 넣기) · lea `LdrpLogEtwHotPatchStatus +0x65` · `LdrpInitializeSmtDelayedSleepQueryRegistry +0x75` | 예외 풀기가 돌 때만 `[추정]` · TraceLogging 특성 등록 때만 `[추정]` · `LdrpInitializeProcess +0x18f6 CMP [R9+0xc],DL` · `+0x18fa JE` (핫패치 상태 바이트 0) · `Session Manager` 의 `SmtDelay…` DWORD 다섯이 있을 때만 (이 VM 없음) |
| CFG `.00cfg` ⓙ | `__guard_xfg_check_icall_fptr` · `__guard_xfg_dispatch_icall_fptr` · `__guard_xfg_table_dispatch_icall_fptr` · `__castguard_check_failure_os_handled_fptr` (4) | `.00cfg` | 콜백 `LdrpCfgCheckRoutineCallback +0x25 · +0x49` · `LdrpCfgDispatchRoutineCallback +0x25 · +0x49` · `LdrpCgRoutineCallback` (← `LdrpCfgProcessLoadConfig +0x70c` 간접) | `LdrpCfgProcessLoadConfig +0x3a9 TEST R12D` · `+0x3ac JZ → +0x454` (`LdrControlFlowGuardEnforced` 0 — `LdrSystemDllInitBlock +0xb8` 0). CastGuard 칸은 `+0x17c BT R15D,0x18` · `+0x181 JC` (`GuardFlags` 비트 24 꺼짐) |

- 합: `.data` 76 (AppVerifier 11 · WIL 13 · SxS 3 · IFEO 3 · 언로드 4 · 재지정 4 · 경로 4 · LoadAsData 3 · MUI 7 (대체 모듈 3 · 임계구역 · 언어 키 4) · NLS 2 · 힙 6 · 스레드 풀 3 · 오류 보고 3 · 복제 자식 2 · TLS 2 · 임계구역 2 · 그 밖 4) · `.mrdata` 18 · `.00cfg` 4 = **98**. 결과서 99 는 + `AvrfpLoaderEntry` (이미 3.8, run105).
- 값은 모두 파일 처음 값 = 0 = 사진 (`.00cfg` 는 파일 값이 0 이 아닌 셋 포함 — 5절).
- `[추정]` 이 붙은 11칸 (결과서 "다음 요청 후보") 은 **쓰는 줄은 `[정적]`, 조건 점프 줄까지는 안 풂** — 값 (0) 은 닫혔다.

### 4. 7.3 에 남는 124 (run116 가름 표시만)

- **아무도 안 씀 44** (이름 칸에 쓰는 명령이 ntdll 에 없음, 값 = 파일 = 0):
  - `.data` 43: WIL 보고 칸 12 (직접 묻는 함수가 없음 · 보고 종류 0) · WIL 상수 기능 상태 칸 5 (`wil_details_AreDependenciesEnabled +0x20 ~ +0x2a` — 기술자 `+0x26` = 1 이면 평가 안 함) · WIL 훅 포인터 3 · SxS 디버그 스위치 3 · 힙 디버그 스위치 5 · `RtlpHpNullGUID` · `NlsEmptyLeadByteInfoTable` · `RtlpCsVerifyDoNotBreak` · `HotPatchSpareGlobals` · 그 밖 4 (`RtlpFreezeTimeBias` · `g_dwLastErrorToBreakOn` · `LdrpFailureData` · `g_LdrBreakOnLdrpInitializeProcessFailure`) · **`__@@_PchSym_…` 7** (코드 참조 0 — 변수가 아니라 컴파일러 PCH 심볼 표시일 가능성 `[추정]`. 전역 수에서 뺄지 사용자 결정).
  - `.mrdata` 1: `LdrpUnalignedUserCallTargetBitMap`.
- **범위 밖 길에서 씀 `[추측]` 80** (쓰는 함수가 지도 밖이고 지도 안 호출자도 없음):
  - `.data` 77: 힙 누수 검출 13 (`RtlDetectHeapLeaks` 류) · 미예상 코드 길 텔레메트리 8 (`UcOnUnexpectedCodePath` 류) · 텔레메트리 6 · 프로퍼티 저장소 · 포인터 매핑 5 · `RtlSetIoCompletionCallback` 5 · 잠긴 메모리 4 · ETW 로거 4 · 기능 토글 `__ft_*` 4 · WIL 밖 기능 4 (`RtlCrc32` · MUI 언어 목록) · 힙 디버그 3 · 그 밖 3 (`RtlRcuAllocate` 등) · 2개씩 (WER · 능력 검사 · VEH · AppVerifier 정지 · 기본 풀 설정) · 1개씩 (디버거 원격 중단 · 디스크 속도 · 예외 기록 · 로더 락 횟수 · 모듈 구역 잠금 · `gMUICacheType` · 정규화 · 패키지 경로).
  - `.mrdata` 3: `RtlpProtectedPolicies*` (`RtlSetProtectedPolicy +0x143 · +0x165 · +0x14a`).
- run114 가 남긴 33 (아무도 안 씀 19 · 범위 밖 14 — 잠금 셋이 빠짐) 과 합치면 **7.3 모름 157 = 아무도 안 씀 63 · 범위 밖 94** — 모두 가름이 끝났다 (아래 "짐 지도 C 끝").

### 5. `.00cfg` 4칸 — 결과서 질문 2

| 칸 (rva) | 로드 구성 필드 | 파일 값 (재배치 뒤) | 쓰는 줄 | P2 까지 값 (짐작) |
|---|---|---|---|---|
| `__guard_xfg_check_icall_fptr` (`0x1e6008`) | `+0x118` | `ntdll+0xfac60` (`_guard_check_icall_nop`, `ret 0`) | `LdrpCfgCheckRoutineCallback +0x25 · +0x49` | 파일 값 그대로 |
| `__guard_xfg_dispatch_icall_fptr` (`0x1e6010`) | `+0x120` | `ntdll+0x1664b0` (`_guard_xfg_dispatch_icall_nop` — `JMP [0x1e6000]`) | `LdrpCfgDispatchRoutineCallback +0x25 · +0x49` | 파일 값 그대로 |
| `__guard_xfg_table_dispatch_icall_fptr` (`0x1e6018`) | `+0x128` | `ntdll+0x1664b0` | 위와 같음 | 파일 값 그대로 |
| `__castguard_check_failure_os_handled_fptr` (`0x1e6020`) | `+0x130` | 0 (`.reloc` 없음) | `LdrpCgRoutineCallback` (칸에 `0x18015ed50` 또는 0) | 0 그대로 |

- 이 네 칸을 **읽는** ntdll 코드는 없다 (코드 참조 0 — 로드 구성 필드와 `.reloc` 만). 옆 `0x1e6000` (`__guard_dispatch_icall_fptr`, 3.10 첫 줄) 만 CFG 간접 호출마다 읽힌다 — 결과서는 이 칸도 `LdrpCfgProcessLoadConfig` 가 모으는 다섯 (`+0x70` · `+0x78` · `+0x118` · `+0x120` · `+0x128`) 중 하나로 적었다 (3.10 첫 줄에 메모만, 셈은 그대로).
- 사진이 못 읽은 까닭은 정적으로 못 정함 `[공백]` — 동적 1.

### 6. WIL 기능 칸을 가른 규칙 · WOW64 표 — 결과서 1.2 · 1.3

- 기술자 (`.rdata` `0x1b7cd0 ~ 0x1b80f0`, 항목 `0x38`B 19개): `+0x8` 상태 칸 주소 · `+0x10` 보고 칸 주소 · `+0x25` · `+0x26` · `+0x27` 바이트 · `+0x28` 필요 기능 목록.
- 필요 기능의 `+0x25` 또는 `+0x26` 이 0 이 아니면 **평가 없이 `+0x27` 만 읽는다** (상수 기능) — 그 기능의 상태 칸은 안 써짐. 둘 다 0 이면 `…Reevaluate +0xa0` 이 상태 칸을 쓴다. 사진 `ImplVal` 0 · `KVS` `0x40147` (run114) 이 이 규칙과 맞는다 `[정적 + 사진]`.
- 보고 칸은 `wil_details_IsEnabledFallback +0x43 TEST ESI,ESI` · `+0x45 JNZ` — 보고 종류가 0 이 아닐 때만 `+0x72 ReportUsageToService` 가 쓴다.
- **WOW64 표**: `LdrpLoadWow64 +0xcd LEA R15` = `.rdata` `0x173830` 표, 항목 `0x10`B 6개 `{이름 포인터, 칸 주소}` (`Wow64LdrpInitialize` → `0x1e70c8` 외 다섯). `+0xd7 CMP EDI,6` 고리가 `+0xf1 LdrGetProcedureAddress(…, R9 = 칸 주소)` 로 채운다. → 3.9 `g_LdrpWow64LdrpInitialize` 줄 `[추측]` → **`[정적]`** (짐작 → 정확).

### 7. 간접 호출 — 결과서 질문 3

- 칸을 쓰는 줄이 간접 호출 대상 안인 곳은 `.00cfg` 4칸 하나 — `LdrpCfgProcessLoadConfig +0x70c` (콜백 셋), 안 탐.
- `LdrpLoadWow64 +0xf1` 는 직접 호출 (표 구동일 뿐). `…Reevaluate +0xe0 · +0x106` 는 구독 포인터 둘 (0 — 위 WIL 훅 포인터 "아무도 안 씀").

## 확인된 것

- `[정적]` 0 을 쓰는 줄 셋 (1절) — `.asm` 줄과 점프 목적지 (flow-writer 다시 셈).
- `[정적]` `LdrpInitializeExecutionOptions +0x9d → LdrpInitializeCriticalSectionExceptionGlobalMitigation` (+ `NtOpenKey` · `NtQueryValueKey` · `NtClose`) — IFEO 건너뛰기 앞이라 test.exe 에서도 **늘** 부른다. 시스템 콜 셋이 이 길에 있다 (키가 열리면).
- `[정적]` 잠금 칸 10 의 잡고 푸는 줄 (2절). `[관찰]` run44 `LdrpLoaderLock` P1b 잡힌 상태.
- `[정적]` 조건부 안 탐 98 (+1) 의 쓰는 줄과 조건 (3절). 사진 0 이 "안 탐" 과 맞는다.
- `[정적]` WOW64 6칸은 `.rdata` 표 구동 — `g_LdrpWow64LdrpInitialize` 짐작 → 정확.
- `[정적]` WIL 상수 기능 규칙 (`wil_details_AreDependenciesEnabled +0x20 ~ +0x2a`).
- `[정적 + 사진]` `LdrAddLoadAsDataTable +0x37` 은 test.exe 에서 안 탐 (표 칸 셋 0) — run114 가 이미 3.8 `LdrpInitOnceLoadAsDataCrits` · `LoadAsDataCrits` 경로 칸과 flow.md D2 에 반영함 (확인만).

## 결과서와 다르게 적은 곳 · 보탠 것

- **`.data` 셈**: 결과서 "조건부 안 탐 `.data` 77" 에는 `AvrfpLoaderEntry` (run105 가 이미 3.8 짐작 줄) 가 들어 있다. 새 줄은 76. 결과서 7.3 숫자 (B 안 203 등) 는 run114 전 305 기준이라 안 썼다 — 이 문서 숫자는 지금 268 에서 셈.
- **잠금 칸** (run114 넘김 ②): 결과서는 "아무도 안 씀 · 잠금 칸 7" 로 셌다. 셈 기준대로 3.8 로 옮겼고, run114 의 셋도 같이 옮겼다 (2절).
- **정확 / 짐작**: 새 `.data` 89 · `.mrdata` 18 은 모두 **정확**으로 셌다 — 쓰는 줄 · 주소 넘김 줄 · 잡고 푸는 줄이 칸 단위로 `[정적]` 이다 (run114 `RtlpDisableHeapLookaside` (IFEO 출력 칸 주소 넘김) 를 정확으로 센 선례). `.00cfg` 4 는 결과서 판단대로 **짐작** (값이 사진 없이 추정). 주소 넘김만 있는 칸 (lea 출력 칸 — `AVrfpHandleTraces` · `AVrfpDebug` · `RtlpShutdownProcessFlags` · `LdrpRedirection*CalloutFunc` 둘 · `RtlpHpTagInitVar` 등) 을 run105 `AvrfpLoaderEntry` 처럼 짐작으로 볼지는 셈 결정 (아래 ④).
- **`LdrpLoaderLock` 구간**: 7.3 라벨은 `≤P1a*` (`*` = 뒤에 바뀜 — P1b 잡힘) 그대로 둔다.

## 정적과 동적이 다른 곳

- 없다. 241칸 모두 파일 값 · 쓰는 줄 · 사진 값이 맞는다 (`.00cfg` 는 사진이 없음).
- **디버거가 붙어서 달라지는 점**: 없음 `[추측]` — AppVerifier · 힙 디버그 갈래는 `NtGlobalFlag` 로만 켜지고 KDNET 은 그 값을 안 바꾼다. 사용자 모드 디버거로 띄우면 힙 디버그 깃발이 켜져 `RtlpHeapInvalidBreakPoint` 등 힙 디버그 칸 길이 열릴 수 있다 `[추측]`.

## 정정

- **이전 run 문서에 틀린 곳은 없다.** run114 는 sast/111 의 "init 줄기 직접 줄" 다섯을 "조건부 안 탐 / 0 을 씀 **후보**" 로만 적었다.
- **결과서 (sast/111) 정정 — 결과서 쪽**: "0 을 씀 후보" 다섯 중 넷 (`RtlpForceCSToUseEvents` · `LdrpForkActiveLock` · `LdrpForkConditionVariable` · `LdrpRedirectionModule`) 은 **조건부 안 탐** (3절 표). 짐 지도 7.45 ① 줄에 `(해결됨 → run116)` 과 함께 적었다.
- 결과서 스스로 고친 첫 판 하나 (`RuntimeGlobal` — 처음에 "죽은 저장" 으로 읽음 → 늘 0 을 씀) 는 결과서 안 일이라 정정 대상이 아니다.

## 이전 run 에서 닫은 것

- run114 정적 ① (짐 지도 7.45 · flow.md 4장) **Q78g2** → 이번 바퀴 (241칸 가름).
- run114 정적 ③ 잠금 칸 셈 일관성 → 오케스트레이터 결정 (3.8 로), 10칸 옮김.
- run114 실마리 (짐 지도 7.5) `RtlFailedCriticalDebugAllocations` → 3.8 (0 을 씀).
- run17 짐작 (3.9) `g_LdrpWow64LdrpInitialize` `[추측]` → `[정적]` (표 구동).
- run111 · run114 가 남긴 7.3 `.mrdata` `끝까지 0` 22 · `.00cfg` 4 → 모두 가름.

## 짐 지도 C (Q78) 정적 조각 끝 — 남은 "아직 모름"

- **구조체 (7.2) 24칸** — 모두 커널: `_EPROCESS.SharedCommitCharge` 1 (`K1 이전`, 줄 후보만 — 7.5) + VAD 칸 안 읽음 23 (`_MMVAD` 9 · `_MMVAD_SHORT` 14). 이 바퀴와 무관 (run115 그대로). 결과서가 적은 "301" 은 옛 숫자다.
- **전역 (7.3) 157칸** — **모두 가름이 끝났다** (아무도 안 씀 63 · 범위 밖 94, `.data` 149 · `.mrdata` 8). 진짜로 "쓴 자리를 모르는" 전역은 **0**. 가름 열을 두면 7.3 은 0 이 된다 (사용자 결정 ②).
  - 그중 `__@@_PchSym_…` 7 은 변수가 아닐 수 있다 — 빼면 전역 438 → 431, 모름 157 → 150.
- `[공백]` 으로 남는 것: `.00cfg` 4칸 P2 값 (사진 읽기 불가 — 동적 1) · `[추정]` 11칸의 정확한 조건 점프 (값은 닫힘) · `LdrpInitMuiCrits` 첫 부름 줄 (후보 셋 — run114 D2).

## 아직 확인 필요

### 동적 (결과서 "동적으로 확인할 것" 1 ~ 5 — 급하지 않음, Q104 VM 단계에 얹을 것. 대기열 번호는 오케스트레이터가 붙임, 멈춤점은 test.exe 조건)

- **1** `.00cfg` 읽기 `[공백]`: `.process /p <test.exe>` 뒤 `.pagein ntdll+0x1e6000` → `dq ntdll+0x1e6000 L5` (기대 `ntdll+0x166480 · 0xfac60 · 0x1664b0 · 0x1664b0 · 0`). 안 되면 `!pte ntdll+0x1e6000`. 사진 8장이 모두 `????` 인 까닭.
- **2** (읽기만) `NtOpenKey` 반환 둘: `ntdll!LdrpInitializeSmtDelayedSleep+0x20` · `ntdll!LdrpInitializeCriticalSectionExceptionGlobalMitigation+0x50` 에서 `r eax` — 0 (열림) 이면 syscall 이 더 난다 (SMT: `NtQueryValueKey` 5 + `NtClose` 1 / 교착: `NtQueryValueKey` 1 + `NtClose` 1). PE_emulator syscall 시간표와 맞추는 용.
- **3** `ntdll!LdrpInitMuiCrits` 입구 (rva `0x479f0`) 첫 도착 `k` — run114 D2 와 같음 (후보 셋).
- 4 (선택) `ntdll!ApiSetpIsFeatureEnabled` 입구 (rva `0x1382c8`) 가 init 동안 몇 번 · `ntdll!IsFeatureEnabled` 의 `ecx` — 0 번이면 ApiSet 기능 갈래 "안 탐" 증명.
- 5 (선택, 읽기만) `db poi(ntdll+0x1d2908)+0xc L1` (P1b) = 0 — `LibLoaderTelemetryInitRunOnce` 가 0 인 까닭의 한 고리.

### 정적 (결과서 "다음 요청 후보" + flow-writer)

- ① (작음) `[추정]` 11칸의 조건 점프 줄 — `LdrpGetDllPath +0x1c7 ~ +0x1f0` · `RtlpIsDosDeviceName_Ustr +0x4f2 ~ +0x542` · `LdrpHandleTlsData +0x425 ~ +0x46c` · `TppCleanupGroupMemberInitialize +0x12b ~ +0x146` · `EtwpUpdateEnableInfoAndCallback +0x282` 앞 · `RtlpHpStackTraceConfig +0x30 ~ +0x61`. 값은 닫혀 급하지 않음.
- ② **셈 결정 (사용자)**: 전역에도 "아무도 안 씀 · 범위 밖" 가름 열을 둘지 — 두면 7.3 모름 157 → **0**.
- ③ **셈 결정 (사용자)**: `__@@_PchSym_…` 7칸을 전역 수에서 뺄지 (빼면 438 → 431).
- ④ **셈 일관성 (오케스트레이터)**: 주소 넘김만 있는 칸 — 이번엔 정확 (run114 선례), run105 `AvrfpLoaderEntry` 는 짐작. 한쪽으로 맞출지. 잠금 칸 둘 (`MuiCacheSWRLock` · `RtlpSecMemLock`) 은 "줄은 길 안 · 탔는지 모름" 인데 3.8 로 셈 — 그대로 둘지.
- ⑤ (작음) 3.10 `__guard_dispatch_icall_fptr` (`0x1e6000`) 의 쓴 주인 — 결과서가 `LdrpCfgProcessLoadConfig` 다섯 칸 중 하나로 적음 (CFG 강제 때 콜백). 짐작 줄 주인 "모름" → ntdll 로 고칠지.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

- **틀림 — 빈 덫 `LdrpInitializeCriticalSectionExceptionGlobalMitigation`** (`Src/ntdll/LdrInit.cpp:1445`): 진짜는 `RtlpRaiseExceptionOnPossibleDeadlock` 에 0 을 쓰고 (`+0x44`) `NtOpenKey("…\\Session Manager")` (`+0x4b`) → `NtQueryValueKey("RaiseExceptionOnPossibleDeadlock")` (`+0x7d`) → `NtClose` (`+0x89`) 를 부른다. 칸이 0 으로 시작하면 값은 같고, `0xcc` 무늬로 채워 시험하면 다르다. syscall 셋이 빠져 있다 (동적 2 로 실제 반환 확인 뒤).
- **맞음**: `LdrpInitializeProcess` 몸통이 진짜 코드라 직접 줄 (`RtlFailedCriticalDebugAllocations` 기대 0 — `Test/LdrInitTest.cpp:1646`) 이 같다. 안 타는 조건의 입력 — `NtGlobalFlag` 0 (`Src/nt/Process.cpp:236`) · `InheritedAddressSpace` (`:304`) · `UseWOW64` 0 (`LdrInitTest.cpp:1642`) · `g_ShimsEnabled` 0 (`:9975`) · `SmtDelayedConfiguration` 0 (`:9686`) · 시스템 DLL 블록 `+0xb8` 0 (`LdrInit.cpp:1460` 주석).
- **다름 (값은 같음)**: SMT — PE_emulator 는 `NtOpenKey` 를 "없음" 으로 두어 `+0x2c` 에서 일찍 끝 → 칸은 같지만 syscall 수가 다르다 (진짜는 키가 열리면 `NtQueryValueKey` 5 · `NtClose` 1 `[추정]`).
- **확인 필요**: PEB `AppCompatFlags` (`+0x2c8`) · `pShimData` · KUSD `+0x3a0` · `RedirectionDllName` 을 우리가 0 · 빈 값으로 두는지, 활성화 문맥 (`activeCodePage` 없음) 응답, `LdrpCfgProcessLoadConfig` 가 진짜 몸통인지, `.00cfg` 칸에 `.reloc` 가 적용되는지 (`0x1e6000` 은 간접 호출마다 읽혀 틀리면 바로 죽으니 돌고 있다면 맞음).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntkrnlmp · ntdll pdb 줄에 run116 · run 메모 한 줄 — 새 로그 없음이라 근거 로그 줄은 그대로), 2.4 머리 run 목록 · 묶음 4 `[정적]` 한 줄 (교착 완화 함수 + syscall 셋) · 뒤5 · 뒤9 한 줄 (`RuntimeGlobal` · SMT), 4장 run114 ① · ③ `(해결됨 → run116)` · ② 숫자 보탬 + 새 줄 넷 (해결됨 · 동적 1 ~ 5 · 정적 ① ~ ⑤ · PE_emulator), 5장 두 표. 그림은 안 고침.
- 짐 지도 (`baggage.md`): 기준 run116 줄 · 판 줄 · 1장 (`.data` · `.mrdata` · `.00cfg` 줄 + run116 메모) · 2장 표 넷 (`.data` · `.mrdata` · `.00cfg` · 전역 합) · 2장 아래 전역 줄 · **2장 맨 끝 "짐 지도 C 끝" 한 줄** · 2.1 아래 전역 모름 나눔 · 3장 머리 run116 줄 · **3.8 머리 run116 줄 · 새 줄 44 (89칸 — 0 을 씀 3 · 잠금 7줄 10칸 · 조건부 34줄 76칸)** · **3.9 머리 · 새 줄 3 (18칸) + 고친 줄 1 (`g_LdrpWow64LdrpInitialize`)** · **3.10 머리 · 새 줄 1 (4칸) + 첫 줄 메모** · 5장 ② · 6장 새 줄 1 (잠금 잡고 풀기) · 7장 머리 · 7.1 · 7.3 (머리 · 가름 표시 · 취소선 111 · 해결됨 줄) · 7.5 `RtlFailedCriticalDebugAllocations` 해결됨 · 7.45 ① · ③ 해결됨 · 새 7.47 · 8장.
- 이전 run 문서 맨 위 정정 한 줄: 없음.

## 디버거 메모 / 도구 메모

- 결과서 제안 (하네스, 직접 안 고침): ① `globals.tsv` 크기가 "다음 이름까지 거리" 라 이웃 칸이 섞임 — 코드 접근 폭 열 (sast/111 제안 2 다시) ② `refs` 가 **레지스터 간접 · 표 구동 쓰기** 를 못 봄 — `.rdata` 포인터 표가 칸 주소를 들고 있으면 "표 구동 쓰기 후보" 로 보여 주기 ③ `_nodes.tsv` 에서 `out` 이 공개 API 인지 깊이 10 한계인지 가르는 열.
- 짐 지도 메모 (flow-writer): 잠금 칸은 이번 바퀴로 "잡고 푸는 줄이 길 안이면 3.8" 하나로 맞췄다 (run60 · run69 · run116). 다음부터 결과서가 "쓰는 줄 없음 (잠금 칸)" 으로 적어도 같은 규칙으로 센다 — 오케스트레이터 확인 거리 (정적 ④).

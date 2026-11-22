# 2026-11-22 (작성일, 정적 분석 2026-11-16) run130 — run65 · 66 남은 작은 정적 (Q124 + Q127, 정적만, 짐 지도 바퀴): `+0x733 LdrpQueryIllegalCWDDevices` 는 IFEO 키가 없어도 닿고 (`RCX = 0`), exe TLS 블록 `0x11`B = `(End − Start = 1) + 정렬 0x10` (`SizeOfZeroFill` 은 안 읽음), 스케줄러 공유 데이터 섹션 = 커널 상수 `0x2000` (2쪽), `LoadNotificationsSent` 를 켜는 줄은 `+0xce` 하나, shim 엔진 길의 문지기는 `[pShimData+0x210]` 하나 — 덤으로 짐 지도 7.6.12 커널 줄을 run125 로 채움

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 이미 있는 상자 **안쪽** 줄만 늘었다 — 2.4 `LdrpInitializeProcess` 묶음 1 (`LdrpInitializeExecutionOptions`) · 뒤4 (스케줄러 · TLS), 2.5 올린 뒤 알림 (`LdrpSendPostSnapNotifications`) · shim 엔진 게이트 (`+0x19dc`). 새 단계 · 새 caller 가 없어 **그림은 안 바꿨다.**
>
> **사용자 결정 (2026-11-22, plan.md 결정 기록)**: 그림 2 에 `LdrpInitializeProcess` 새 callee 상자 (Q124 (e) — 정책 · 현재 디렉터리 · CPU 기능 · 스케줄러 둘 · TLS 준비) 와 `LdrpEnableUMGLTracingStateSync` 상자 (Q119 (e)) 는 **안 넣는다** (`(n)` 번호 밀림 피함). 커널 일꾼 공장 노드 (Q228 (가)) 도 지금은 안 넣는다 (Q226 동적 뒤 다시). 짐 지도 7.6.12 커널 줄 (Q228 (나)) 은 **이번에 채운다** — 아래 "덤" 절.

## 요약

| 대기열 | 물음 | 답 | 표시 |
|---|---|---|---|
| Q124 (a) | `LdrpPolicyBits` 를 짐 지도 7.3 → 3.8 로 | **이미 run114 가 옮겼다** (3.8 조건부 안 탐 줄, 7.3 취소선). 쓰는 줄 5개 (`LdrpInitializePolicy +0x7e · +0x26b · +0x305 · +0x31d · +0x329`) 전부 "패키지 앱 + `DllPath` 비지 않음" 갈래 — 이번에 다시 셈, 같음. **2장 숫자 그대로** | [정적] |
| Q124 (b) | `+0x733 LdrpQueryIllegalCWDDevices` 가 IFEO 키 없을 때도 닿나 | **닿는다.** 키 없음 표시 (`+0xad JNZ → +0x604`) 도 `+0x71c` 에서 일반 길과 합쳐져 `+0x72e JS` 만 넘으면 `+0x733`. 인자 `RCX` = 키 핸들 = **0**. 값 0 은 **이 VM 의 KUSD 정책 (`CurDirDevicesSkippedForDlls = 0`) 때문**이지 코드가 0 을 보장하는 건 아니다 | [정적] |
| Q124 (c) | exe TLS 블록 `0x11`B 의 근거 줄 | **`LdrpAllocateTls +0xb7 SUB R15,[RDI]` (End − Start = 1) · `+0xd1 ~ +0xd5` (정렬은 최소 `0x10`) · `+0xec LEA R8,[R15+1]` · `+0xf0 ADD R8,RBX` → `1 + 0x10 = 0x11`.** `SizeOfZeroFill` 은 어느 줄도 안 읽는다. 0 채움 할당 플래그도 없다 | [정적] (+ PE 바이트) |
| Q124 (d) | 스케줄러 공유 데이터 섹션 크기 (사진 2쪽) | **커널 상수 `0x2000`** — `PspSchedulerSharedDataRegionCreate +0x127 MOV R15D,0x2000`. 섹션 · 시스템 보기 · 이 프로세스 보기 셋이 같은 값. ntdll 의 `8` 은 출력 핸들 칸 길이 | [정적] |
| Q124 (e) | 그림 2 에 새 callee 상자 | **사용자 결정: 안 넣음** (2026-11-22) | 결정 |
| Q127 (a) | `Flags.LoadNotificationsSent` 를 켜는 다른 줄 | **`LdrpSendPostSnapNotifications +0xce` 하나뿐**, 끄는 줄 0 (ntdll · KernelBase · kernel32 전수). "syscall 5번" `[추측]` 의 받침이 "모듈이 다섯" 쪽에서 닫혔다 (syscall 수 자체는 여전히 `[추측]`) | [정적] |
| Q127 (b) | shim 엔진 올리는 길의 조건 | 문지기는 `LdrpInitializeProcess +0x19d7` (`pShimData ≠ 0`) **그리고** `+0x19dc` (`[pShimData+0x210] ≠ 0`) 둘뿐. 안쪽 셋 (`LdrpInitShimEngine` · `LdrpGetShimEngineInterface` · `LdrpLoadShimEngine`) 엔 새 문지기가 없고 실패하면 조용히 끝. `g_ShimsEnabled = 1` 은 `LdrpLoadShimEngine +0x148` 한 줄. 입구는 셋 — test.exe 는 셋 다 안 탐 | [정적] |
| Q127 (c) | `SbUpdateSwitchContextBasedOnDll` 이 `SwitchBackContext` 를 쓰나 | **쓴다** — `+0xd9` (run113 그대로). ntdll 에서 모듈 항목 `+0xc0` 을 쓰는 줄은 이것뿐 | [정적] |
| Q228 (나) | 짐 지도 7.6.12 커널 줄 | run125 칸으로 채움 (아래 "덤") | [정적] (run125) |

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든여덟째 바퀴. plan **Q124 + Q127** (run65 · run66 이 남긴 작은 정적). 정적은 2026-11-16 (사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례), 문서는 사용자 결정 2026-11-22 "run130 = sast/120 문서 먼저". **짐 지도 바퀴.** 덤으로 Q228 (나) (사용자 결정 2026-11-22).
- 정적: ntdll `C3093720177DA6851519DCFFE8DF53FE1` · ntoskrnl (`ntkrnlmp.pdb`) `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음, 결과서 대조).
- 버전 대조 (결과서): `LdrpInitializeProcess +0x1bb6/+0x1bbb` · `NtCreateUserProcess +0xc31/+0xc36` · `_LdrpInitialize +0x167/+0x16c` · `LdrpInitializeNode +0x166/+0x16b` · `+0x197/+0x19c` — 알려진 값과 같음.
- **다시 analyze 안 함.** 새로 뽑은 함수 1개 (`^LdrInitShimEngineDynamic$` depth 0, `_calls.tsv` +9줄). ntoskrnl 은 있던 `.asm` 둘 (`PspSchedulerSharedDataRegionCreate` · `NtSetInformationProcess`) 읽기만. PE 바이트는 `sample/bin/test.exe` (VM 파일과 같다는 가정 — sast/59 와 같음).

## 목적

- run65 (sast/59) · run66 (sast/61) 가 남긴 작은 정적을 한 바퀴로 닫는다 (요청서 `_workspace/sast/120_run65-66-leftovers.md`):
  - Q124: `LdrpPolicyBits` 자리 · `+0x733` 이 닿나 · TLS 블록 크기 줄 · 스케줄러 섹션 크기 출처.
  - Q127: `LoadNotificationsSent` 켜는 줄 전수 · shim 엔진 길 조건 · `SwitchBackContext` 쓰는 줄.
- 덤: run125 (일꾼 공장) 결과로 짐 지도 7.6.12 의 커널 칸 `[공백]` 을 채운다.

## 방법

- 정적: `_workspace/sast/120_run65-66-leftovers.md` — ntdll (+ ntoskrnl 읽기만).
- 결과서: `_workspace/sast/120_run65-66-leftovers_result.md` (**분석성공**, 스크립트 문제 없음, 동적 필수 없음).
- 결과서 도구: 스크래치 `ntdll.dis` · `KernelBase.dis` · `kernel32.dis` (objdump), sast/103 의 `scan_flags.py` 다시 돌림, 작은 도구 `s120/rda.py` · `s120/scan_flags_any.py` — 레포 밖, 결과 파일 안 남김.
- flow-writer 가 `.asm` 으로 다시 본 줄 (결과서와 모두 같음):
  - `LdrpInitializeExecutionOptions +0xad · +0x604 · +0x6b9 · +0x6be · +0x6c4 · +0x71c · +0x72c · +0x72e · +0x730 · +0x733 · +0x738`, `LdrpQueryIllegalCWDDevices +0x13 · +0x16 · +0x40 · +0x6e`
  - `LdrpAllocateTls +0x79 · +0x98 · +0xab · +0xb4 · +0xb7 · +0xc5 · +0xd1 · +0xd5 · +0xdc · +0xe0 · +0xe6 · +0xec · +0xf0 · +0xfa · +0x108 · +0x112 · +0x121 · +0x125 · +0x12b`
  - `LdrpAllocateSchedulerSharedData +0x19 · +0x28 · +0x2c · +0x54`, nt `NtSetInformationProcess +0xec4 · +0xf17 · +0x13ea · +0x13ef`, `PspSchedulerSharedDataRegionCreate +0x127 · +0x13d · +0x158 · +0x18e · +0x1a9 · +0x24c`
  - `LdrpSendPostSnapNotifications +0xce`
  - `LdrpInitializeProcess +0x19c8 ~ +0x19f4 · +0x1ca7 · +0x1cae · +0x1cdf · +0x1ceb`, `LdrpInitShimEngine +0x9f · +0xcd · +0xed · +0xf5 · +0x109 · +0x160 · +0x16e`, `LdrpGetShimEngineInterface +0x15 · +0x129 · +0x135 · +0x4a9 · +0x4d9 · +0x4e9`, `LdrpLoadShimEngine +0x4c · +0x5e · +0x7e · +0xfd · +0x141 · +0x148`, `LdrInitShimEngineDynamic +0x29 · +0x30 · +0x42 · +0x67 · +0xa7 · +0xdd · +0xe4`, `LdrpGetProcApphelpCheckModule +0x27e · +0x285`
- PE 바이트: flow-writer 가 `sample/bin/test.exe` TLS 디렉터리 (데이터 디렉터리 9, rva `0x79500`, 크기 `0x28`, 파일 오프셋 `0x77d00`) 를 다시 읽음 — 결과서 값과 같음.
- 덤 (Q228 (나)): `runs/03_sprint/run125-worker-factory.md` · `_workspace/sast/122_worker-factory_result.md` 를 근거로만 옮김 (새 분석 없음).

## 결과

비유 (가게 개업, 앞 run 들 이어서):

- 가게 문을 열기 전 점검표 칸 몇 개가 "아마 이럴 것" 으로 남아 있었다. 이번엔 그 칸마다 **"점검표 몇 번째 줄에 그렇게 쓰여 있다"** 를 찾아 붙였다.
- 예를 들어 TLS 블록 `0x11` 바이트는 "그쯤이겠지" 가 아니라 "물건 1 바이트 + 줄 맞춤 여유 16 바이트" 라는 셈이 코드에 그대로 있었다.
- 호환성 고침 (shim) 엔진은 가게 문 앞 **문지기 둘** (블록이 있나 · 블록 `+0x210` 이 0 이 아닌가) 만 통과하면 들어온다. test.exe 는 둘째 문지기에서 돌아간다.

### 정적 — 호출 지점 (`ms/disassemble/<모듈>/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x531` | `+0x536` | `ntdll!LdrpInitializeExecutionOptions` | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0xc5` | `+0xca` | `ntdll!RtlpOpenImageFileOptionsKeyEx` (키 없음 표시 `+0xad` 면 안 옴) | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x604` | `+0x609` | `ntdll!LdrControlFlowGuardEnforced` (키 있음 · 없음 합류 첫 줄) | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x61e` | `+0x623` | `ntdll!RtlSetGlobalHeapFeatures` | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x6a6` | `+0x6ab` | `ntdll!LdrpPayloadRestrictionMitigationsEnabled` | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x75e` | `+0x763` | `ntdll!LdrpInitializeApplicationVerifierPackage` (`NtGlobalFlag & 0x2000100` 또는 위 함수 참일 때만) | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x733` | `+0x738` | **`ntdll!LdrpQueryIllegalCWDDevices`** (`RCX` = IFEO 키 핸들, test.exe 0) | [정적] |
| `ntdll!LdrpQueryIllegalCWDDevices` | `+0x6e` | `+0x73` | IFEO 키 값 읽기 `CWDIllegalInDLLSearch` (`RCX ≠ 0` 일 때만. 같은 주소 이름 2개, 후보: `LdrQueryImageFileKeyOption` · `RtlQueryImageFileKeyOption`) | [정적] |
| `ntdll!LdrpInitializeTls` | `+0xf5` | `+0xfa` | `ntdll!LdrpAllocateTlsEntry` (디렉터리 `0x28`B 를 항목으로) | [정적] |
| `ntdll!LdrpInitializeTls` | `+0x15d` | `+0x162` | `ntdll!LdrpAllocateTls` | [정적] |
| `ntdll!LdrpAllocateTls` | `+0xfa` | `+0xff` | `ntdll!RtlAllocateHeap` (`LdrpTlsHeap`, 태그만, 크기 `0x11`) | [정적] |
| `ntdll!LdrpAllocateTls` | `+0x12b` | `+0x130` | 메모리 복사 (`End − Start` 바이트. 같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`) | [정적] |
| `ntdll!LdrpAllocateSchedulerSharedData` | `+0x2c` | `+0x31` | `ntdll!NtSetInformationProcess` (`-1`, `0x70`, &핸들, `8`) | [정적] |
| `nt!NtSetInformationProcess` | `+0xf17` | `+0xf1c` | `nt!PspSchedulerSharedDataRegionCreate` | [정적] |
| `nt!PspSchedulerSharedDataRegionCreate` | `+0x158` | `+0x15d` | `nt!MmCreateSectionEx` (`R8D = 0x2000`) | [정적] |
| `nt!PspSchedulerSharedDataRegionCreate` | `+0x18e` | `+0x193` | `nt!MiMapViewInSystemSpace` (보기 크기 `0x2000`) | [정적] |
| `nt!PspSchedulerSharedDataRegionCreate` | `+0x24c` | `+0x251` | `nt!MmMapViewOfSection` (이 프로세스, `0x2000`) | [정적] |
| `nt!PspSchedulerSharedDataRegionCreate` | `+0x276` | `+0x27b` | `nt!ObInsertObjectEx` (핸들) | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xa3` | `+0xa8` | `ntdll!SbUpdateSwitchContextBasedOnDll` (안 `+0xd9` 이 모듈 `+0xc0`) | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xc1` · `+0xd8` | `+0xc6` · `+0xdd` | `LdrpSendDllNotifications` · `LdrpLogDllState` (사이 `+0xce` 가 `Flags` 비트 3) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x19ef` | `+0x19f4` | **`ntdll!LdrpInitShimEngine`** (`+0x19da` · `+0x19e3` 두 문지기 뒤) | [정적] |
| `ntdll!LdrpInitShimEngine` | `+0xcd` | `+0xd2` | `ntdll!LdrpLoadDll` (`apphelp.dll`) | [정적] |
| `ntdll!LdrpInitShimEngine` | `+0x109` | `+0x10e` | `ntdll!LdrpGetShimEngineInterface` | [정적] |
| `ntdll!LdrpInitShimEngine` | `+0x160` | `+0x165` | 간접 (CFG) = 푼 `g_pfnSE_InitializeEngine` | [정적] |
| `ntdll!LdrpInitShimEngine` | `+0x16e` | `+0x173` | `ntdll!LdrpLoadShimEngine` | [정적] |
| `ntdll!LdrpGetShimEngineInterface` | `+0x130` | `+0x135` | `ntdll!LdrUnloadDll` (내보내기 12개 중 하나라도 못 찾을 때) | [정적] |
| `ntdll!LdrpGetShimEngineInterface` | `+0x48f` · `+0x560` | `+0x494` · `+0x565` | `ntdll!LdrProtectMrdata` (열기 · 닫기) | [정적] |
| `ntdll!LdrpLoadShimEngine` | `+0x12d` | `+0x132` | `ntdll!LdrpSendShimEngineInitialNotifications` | [정적] |
| `ntdll!LdrpLoadShimEngine` | `+0x1f0` · `+0x2b0` · `+0x2bb` | `+0x1f5` · `+0x2b5` · `+0x2c0` | `LdrpInitializeNode` · 실패면 `LdrpInitializationFailure` · `NtTerminateProcess` | [정적] |
| `ntdll!LdrInitShimEngineDynamic` (새로 뽑음) | `+0x67` · `+0xe4` | `+0x6c` · `+0xe9` | `LdrpLoadShimEngine` · `LdrpGetShimEngineInterface` | [정적] |
| `ntdll!LdrpGetProcApphelpCheckModule` | `+0x285` | `+0x28a` | `ntdll!LdrpGetShimEngineInterface` | [정적] |

### Q124 — run65 남은 것

**(a) `LdrpPolicyBits` 자리** — 근거만 옮김.

- 이 칸 (`0x1cd7fc`) 을 **쓰는** 줄은 ntdll 전체에 다섯: `LdrpInitializePolicy +0x7e` (`= 0x29`) · `+0x26b` (`|= 4`) · `+0x305` (`|= 0x40`) · `+0x31d` (`|= 2`) · `+0x329` (`&= ~1`). 나머지 16줄은 읽기.
- 다섯 모두 `+0x46 TEST [PEB+3],0x10` (패키지 앱) · `+0x72 CMP word [DllPath.Length],0` 뒤에만 있다. 앞부분에서 `+0x7e` 너머로 뛰는 점프는 없다.
- test.exe `BitField 0x4` (`[관찰]` run44) → `+0x51 JZ +0x58` 로 그냥 돌아감 → 파일 값 `0x16` 그대로.
- **짐 지도 셈**: run114 가 이미 3.8 "조건부 안 탐" 줄로 옮기고 7.3 에서 취소선으로 뺐다 (짐 지도 3.8 `LdrpPolicyBits` 줄 · 7.24 ①). 그래서 **이번엔 다시 세지 않는다** — run123 이 Q113 (c) 에서 "run114 가 이미 셌다" 로 판단한 것과 같다. **2장 숫자 그대로.**

**(b) `+0x733` 이 IFEO 키 없을 때도 닿나** — 닿는다.

```
LdrpInitializeExecutionOptions
 │  +0xa6 TEST [ProcessParameters.Flags],0x4000      "IFEO 키 없음" 표시 (test.exe 0x4001 [관찰] run44)
 │  +0xad JNZ → +0x604                               ← test.exe 는 여기서 IFEO 읽기 구간 (+0xb3 ~ +0x603) 을 통째로 건너뜀
 │  (+0xca 키 열기 실패 +0xd1 JS → +0x604 도 같은 곳)
 ├─ +0x609  LdrControlFlowGuardEnforced
 ├─ +0x623  RtlSetGlobalHeapFeatures              +0x623 XOR ESI,ESI (상태 0 으로)
 │  +0x62a ~ +0x63d  검증기 HKCU 키 읽기 문지기 셋 → +0x696
 │  +0x696 NtGlobalFlag & 0x2000100 · +0x6a6 PayloadRestriction → (켜지면 +0x763 검증기 패키지, 아니면 +0x6b3)
 │  +0x6b9 (모던 앱 키) · +0x6be (IFEO 키 핸들 RDI — 키 없음이면 0) · +0x6c4 (BeingDebugged == 0) → 셋 다 +0x71c 로 합침
 │  +0x71c ~ +0x725  PEB+0x7c4 비트 0 이면 PEB+0x7c0 |= 1
 │  +0x72c TEST ESI · +0x72e JS → +0x797          (ESI < 0 일 때만 건너뜀 — test.exe 는 0)
 │  +0x730 MOV RCX,RDI                             RCX = 0 (키 없음)
 └─ +0x738  LdrpQueryIllegalCWDDevices             그 뒤 +0x738 JMP +0x7bd (정리)
              +0x13 TEST RCX · +0x16 JNZ 안 탐 → +0x18 KUSD 0x7ffe02d5 비트 4~5 → +0x40 [LdrpIllegalCWDDevices] = 0
```

- 안 닿는 경우는 상태가 음수일 때뿐이다 (`+0x3df JS` = 큰 페이지 이미지의 키 열기 실패, `+0x72e JS` = 디버그 힙 환경 변수 변환 실패 · 검증기 패키지 실패). test.exe 는 해당 없음 `[정적 + 관찰 값]`.
- 값: `RCX = 0` 이면 KUSD `CurDirDevicesSkippedForDlls` (`+0x18 ~ +0x26`) 로 `0 → 0` · `1 → 0x2000` · `2 → 0x10` · `3 → −1`. `+0x40` 이 0 을 **명시적으로 쓴다** (파일 값도 0 이라 결과는 같음). `RCX ≠ 0` 이면 먼저 IFEO 값 `CWDIllegalInDLLSearch` (`+0x6e`), 없거나 −1 ~ 2 밖이면 KUSD 길.
- 사진: KUSD 비트 4~5 = `0y00` (`[관찰]` run44 P1a, sast/59), `LdrpIllegalCWDDevices` `끝까지 0*` (run44). 부팅 때 커널이 `PspInitPhase2 +0x136 · +0x13f · +0x16c` 로 이 정책을 KUSD 에 옮긴다 (sast/110, run115).
- → **"0" 은 이 VM 의 정책 값 때문이다.** IFEO 에 `CWDIllegalInDLLSearch` 를 넣었거나 정책을 바꾼 컴퓨터에선 0 이 아니고, 그러면 `LdrpInitializeProcess +0x1183 LdrpCheckAppDirType` 이 탄다 (run65).

**(c) exe TLS 블록 크기 `0x11`B** — 셈 줄이 있다.

test.exe TLS 디렉터리 (PE 바이트, 결과서 + flow-writer 다시 읽음):

| 칸 | 값 |
|---|---|
| `StartAddressOfRawData` · `EndAddressOfRawData` | `0x140079f90` · `0x140079f91` → 원본 **1 바이트** |
| `AddressOfIndex` · `AddressOfCallBacks` | `0x140082ce0` · `0x14006e380` |
| `SizeOfZeroFill` | **0** |
| `Characteristics` | `0x00100000` → 정렬 코드 1 (1 바이트 정렬) |

`LdrpAllocateTls` (항목마다, 스레드마다):

| 줄 | 하는 일 |
|---|---|
| `+0x79 MOV R12D,0x10` | 정렬 시작값 16 |
| `+0x98 ~ +0x9f` | 항목 `+0x34` (= 디렉터리 `Characteristics`) 에서 정렬 코드 → 1 |
| `+0xb4 MOV R15,[RCX]` · **`+0xb7 SUB R15,[RDI]`** | `R15 = End − Start` = 1. 항목 `+0x30` (`SizeOfZeroFill`) 을 읽는 줄은 **없다** |
| `+0xc5 ~ +0xce` | 정렬 = `1 << (코드 − 1)` → 1 |
| `+0xd1 CMP R8D,0x10` · `+0xd5 CMOVC` · `+0xdc LEA EBX,[R8-1]` | **정렬은 최소 `0x10`** → `EBX = 0xf` |
| **`+0xec LEA R8,[R15+1]` · `+0xf0 ADD R8,RBX`** | 크기 = `(End − Start) + 정렬` = **`0x11`** |
| `+0xe0 ~ +0xe6` → `+0xfa RtlAllocateHeap` | 플래그 = `NtdllBaseTag + 0xc0000` 뿐 — **0 채움 비트 (`8`) 없음** |
| `+0x108 ~ +0x112` | 받은 주소를 16 정렬로 올리고 바로 앞 8 바이트에 원래 주소 숨김 |
| `+0x121` · `+0x12b` | 벡터 칸에 걸고, **원본 `End − Start` 바이트만** 복사 |

- 블록 `0x11`B = 앞 `0x10` (정렬 여유 + 숨긴 주소) + 뒤 1B 데이터.
- 사진 맞춤 `[추측: 계산]`: run44 칸 0 = `0x25996f38440` 이 블록 + `0x10` 이면 블록은 `…38430` — 벡터 블록 (`…383d0`, `0x58`B) 바로 다음 자리. 어긋나지 않는다.
- `LdrpInitializeTls` 는 바이트 크기를 안 센다 (항목 수 · 비트 수만). DLL 쪽 `LdrpHandleTlsData +0x17a ~ +0x1df` 도 같은 식 (결과서 — test.exe 길에서 쓰이는지는 이번에 안 따짐).
- → `SizeOfZeroFill` 이 0 이 아닌 이미지라도 진짜 코드는 그만큼 받지도 0 으로 채우지도 않는다 `[정적]`. test.exe 는 0 이라 상관없다.

**(d) 스케줄러 공유 데이터 섹션 크기** — 커널이 정하는 상수.

- ntdll: `LdrpAllocateSchedulerSharedData +0x19 MOV R9D,8` · `+0x28 LEA EDX,[R9+0x68]` (`0x70`) · `+0x2c` 호출 — `8` 은 **출력 핸들 칸 길이**다. 크기를 넘기지 않는다.
- 커널: `NtSetInformationProcess +0xec4 CMP R13D,8` (정확히 8) → `+0xf17 PspSchedulerSharedDataRegionCreate` → `+0x13ea ~ +0x13ef` 사용자 버퍼에 핸들.
- `PspSchedulerSharedDataRegionCreate`: **`+0x127 MOV R15D,0x2000`** → `+0x13d` → `+0x158 MmCreateSectionEx` · `+0x18e MiMapViewInSystemSpace` · `+0x24c MmMapViewOfSection` 셋 모두 `0x2000`. `+0x1a9 MOV R9D,2` — 쪽마다 한 칸을 채우는 반복 2번.
- 헷갈리지 말 것: `+0x7d MOV R14D,0xf8` 은 객체 본체 크기 (`ObCreateObjectEx` `+0x9f`) 이지 섹션 크기가 아니다 (결과서).
- 사진 `[관찰]` run44 P1a "VAD `25996cb0` 2쪽 매핑" (짐 지도 7.6.5) 과 맞는다.

### Q127 — run66 남은 것

**(a) `Flags.LoadNotificationsSent` (항목 `+0x68` 비트 3) 를 켜는 줄** — `+0xce` 하나.

- 결과서가 sast/103 방법 (`scan_flags.py`) 을 다시 돌렸다: ntdll 에서 이 비트를 만드는 쓰기는 **`LdrpSendPostSnapNotifications +0xce OR dword [RBX+0x68],8` 하나**. 비트 3 을 끄는 `AND` 는 0줄. 읽고-고쳐-되쓰는 줄 (`LdrpCompleteMapModule` · `LdrpProcessMappedModule` · `LdrpInitializeProcess +0x13c4 ~ +0x13f0` · `AvrfMiniLoadDll`) 은 같은 항목의 옛 값이라 새 비트를 안 만든다. `LdrpInitializeProcess +0x15b8` 은 다른 항목에서 복사하는 검증기 길 (test.exe 안 탐, sast/103).
- KernelBase · kernel32 에도 모듈 항목 `Flags` 를 쓰는 줄은 없다 (`[reg+0x68]` 쓰기는 모두 다른 구조체).
- 이 줄 앞엔 비트 3 을 보는 문지기가 없다. 건너뛰는 길은 검증기 (`NtGlobalFlag & 0x100`, test.exe 0 — 사진) 뿐. 노드는 `State 6 → 7` 한 번만 들어오고 ntdll 노드는 `State 9` 라 안 들어온다.
- → 사진 (`ms/logs/04_baggage-read_P1b.txt:3392 · 3602 · 3707 · 3812 · 3917` = 1, `:3497` = 0) 의 "다섯 모듈 켜짐 · ntdll 꺼짐" 이 **알림 다섯 번** 과 맞는다 `[정적 + 관찰]`. 단 syscall (`NtApphelpCacheControl`) 수는 `LdrpCheckModule` 안 조건이 따로 정하므로 "syscall 5번" 은 여전히 `[추측]` (받침: 모듈이 다섯).
- 한계 (결과서): ntdll · KernelBase · kernel32 코드만. apphelp · 검증기 DLL 은 범위 밖, `[reg+idx*N+0x68]` 꼴은 못 봄.

**(b) shim 엔진을 올리는 길의 조건** — 문지기 둘, 입구 셋.

```
LdrpInitializeProcess
 │  +0x19cd MOV R13,[RSP+0x180]                   PEB +0x2d8 pShimData 의 지역 사본
 │  +0x19d7 TEST R13 · +0x19da JZ → +0x19f4       문지기 1: pShimData ≠ 0
 │  +0x19dc CMP [R13+0x210],0 · +0x19e3 JZ → +0x19f4   문지기 2: 블록 +0x210 ≠ 0   ← test.exe 는 여기서 건너뜀
 │  +0x19e5 PEB+0x2e0 AppCompatInfo := 0
 └─ +0x19f4  LdrpInitShimEngine(pShimData)        반환값 안 봄
              ├─ +0xd2   LdrpLoadDll("apphelp.dll")     실패 → 로그 0xe76, 끝
              │          +0xed 항목 Flags |= 0x100 (ShimDll) · +0xf5 g_pShimEngineModule
              ├─ +0x10e  LdrpGetShimEngineInterface     내보내기 12개 → g_pfnSE_* (쿠키로 감춤). 못 찾으면 +0x129 g_ShimsEnabled = 0 · +0x135 LdrUnloadDll
              ├─ +0x165  간접 = g_pfnSE_InitializeEngine  음수면 정리로
              └─ +0x173  LdrpLoadShimEngine(이름 목록)
                         +0x7e g_ShimsLoading = 1 → (목록 이름마다 LdrpLoadDll · LdrpInitializeNode · SE_ShimDllLoaded)
                         → SE_InstallBeforeInit → 모듈마다 LdrpSendShimEngineInitialNotifications
                         → +0x141 g_ShimsLoading = 0 → +0x148 g_ShimsEnabled = 1
```

- 문지기는 위 **둘뿐**이다. 안쪽 셋에는 새 문지기가 없고, 실패하면 로그를 남기고 조용히 끝난다 (`LdrpLoadShimEngine` 안 shim DLL 의 `LdrpInitializeNode` 실패만 `NtTerminateProcess` — 프로세스를 죽임).
- `g_ShimsEnabled` 쓰는 줄 전수 (`_refs/g_ShimsEnabled.tsv`): **1 = `LdrpLoadShimEngine +0x148` 하나.** 0 = `LdrpGetShimEngineInterface +0x129` (flow-writer `.asm`: `+0x15 XOR EDI,EDI` 뒤 `RDI` 를 바꾸는 줄 없음 → 값 0) · `LdrpInitializeProcess +0x1ceb` (켜졌을 때 `SE_InstallAfterInit` 가 0 을 돌려주면, run66).
- `g_ShimsLoading` 값 (flow-writer `.asm`): `+0x7e` = `R14B` = **1** (`+0x5e MOV R14B,1`), `+0x141` = `R13B` = **0** (`+0x4c XOR R13D,R13D`, 그 뒤 `R13` 쓰기 없음). 짐 지도의 `[추측]` 두 값이 `[정적]` 이 됐다.

shim 엔진 입구 셋 — test.exe 는 셋 다 안 탄다:

| 입구 | 부르는 곳 | `g_ShimsEnabled` 를 켜나 | test.exe |
|---|---|---|---|
| ① 프로세스 시작 | `LdrpInitializeProcess +0x19ef` → `LdrpInitShimEngine` | 켠다 (`+0x16e → LdrpLoadShimEngine +0x148`) | **안 탐** — 블록 `+0x210 = 0` (`[덤프 오프라인]` run118) → `+0x19e3 JZ` |
| ② 나중에 (내보내기) | `LdrInitShimEngineDynamic` (rva `0x8b1b0`) `+0x67` → `LdrpLoadShimEngine` · `+0xe4` → `LdrpGetShimEngineInterface` | 켠다 | **안 탐** — ntdll · kernel32 · KernelBase 안에 부르는 줄 0. 부르는 쪽은 다른 DLL (apphelp 같은) `[추측]` |
| ③ apphelp 캐시 대답 | `LdrpGetProcApphelpCheckModule +0x285` (`+0x27e g_pShimEngineModule`) | 안 켠다 (인터페이스만) | **안 탐** — `g_pShimmedModuleList = 0` (`[관찰]` run44, run66) |

- 블록 `+0x210` 을 **누가 쓰나** 는 여전히 `[공백]` (sast/116 그대로 — 부모 쪽 kernelbase · apphelp 길 후보).

**(c) `SbUpdateSwitchContextBasedOnDll` 이 `SwitchBackContext` 를 쓰나** — 근거만 옮김.

- **쓴다**: `+0xd9 MOV [R14+0xc0],RDI` (`RDI` = `SbpDetermineDllContext` (call `+0xc1`, ret `+0xc6`) 가 준 표 칸 `+4`). run113 · run118 그대로.
- 결과서가 덧붙인 전수: ntdll 에서 **모듈 항목 `+0xc0` 을 쓰는 줄은 이것뿐**. `LdrpSnapModule +0xefa` 는 import 문맥 (모듈 항목 아님). `LdrpMapCleanModuleView +0xa8` · `AvrfMiniLoadDll +0x8f8` 은 구조체 정체를 안 따라감 `[공백: 낮음]`.

### 호출 관계 (이번에 닫힌 길만)

```
LdrpInitializeProcess
 ├─ +0x536   LdrpInitializeExecutionOptions
 │            └─ +0x738  LdrpQueryIllegalCWDDevices (RCX = 0)       +0x40 LdrpIllegalCWDDevices = 0
 ├─ +0xfca   LdrpInitializePolicy                                  LdrpPolicyBits 쓰는 다섯 줄 — 패키지 갈래, 안 탐
 ├─ +0x182c  LdrpAllocateSchedulerSharedData
 │            └─ +0x31   NtSetInformationProcess(0x70) ==SYSCALL==> nt!NtSetInformationProcess
 │                                                                   └─ +0xf1c PspSchedulerSharedDataRegionCreate (0x2000)
 ├─ +0x18d0  LdrpInitializeTls
 │            └─ +0x162  LdrpAllocateTls                           크기 (End−Start) + 0x10 = 0x11
 ├─ +0x19f4  LdrpInitShimEngine                                    (+0x19e3 에서 건너뜀 — test.exe 안 탐)
 └─ (알림)   LdrpNotifyLoadOfGraph → +0x25 LdrpSendPostSnapNotifications
                                      ├─ +0xa8 SbUpdateSwitchContextBasedOnDll   +0xd9 모듈 +0xc0
                                      └─ +0xce Flags |= 8 (LoadNotificationsSent) — 이 비트를 켜는 유일한 줄
```

## 덤: run125 로 짐 지도 채움 (Q228 (나))

사용자 결정 2026-11-22 ⑤ 대로, 짐 지도 7.6.12 의 `(커널) 로더 풀 일꾼 공장 — "풀어 줄 수"` 줄 (`[추측: 커널 본문 안 봄]` · 커널 칸 `[공백]` (Q134 (b))) 을 run125 결과로 채웠다. 새 분석은 없다 — run125 문서 · sast/122 결과서에 있는 칸만 옮겼다.

- 로더 풀 첫 넣기 `TppWorkPost +0x4f3` 의 `NtReleaseWorkerFactoryWorker` 한 번이 커널에서 바꾸는 칸 (run125 `[정적]`, 칸 이름은 PDB 에 없어 `[추측: 이름]`):
  - 공장 `+0x10` 이 가리키는 덩어리 `P` (`0x28`B): **`P+0x18` 넣은 일감 수 0 → 1** (`+0xfb`) · **`P+0x20` "패킷이 포트에 있음" 0 → 1** (`+0x10c`).
  - 완료 포트: 네 칸이 0 인 "풀어주기 패킷" 하나 — 기다리는 일꾼이 없으니 줄에 붙고 `SignalState` 0 → 1 (`+0x2e0 ~ +0x42f`).
  - 그 자리에서 `+0x201 ExpWorkerFactoryCheckCreate` — 새 공장 첫 넣기는 조건 일곱이 모두 참 → **`f+0x190` (만드는 중) ++** → `+0x19b ExpWorkerFactoryCreateThread` → **`f+0x188` (Ready 전) ++** → `RtlpCreateUserThreadEx` → `ZwCreateThreadEx` (= `NtCreateThreadEx` 길) 로 **일꾼 스레드 하나** (`CreateFlags 0x11` → `LoaderWorker` — 로더 풀 bit 11, run69 · run125) 를 멈춘 채 만들고 `+0x153 ZwResumeThread` 로 같은 호출 안에서 푼다.
  - 새 일꾼이 `NtWorkerFactoryWorkerReady` 를 부르면 `+0x190` · `+0x188` 이 내려가고 `+0x180` · `+0x184` 가 오른다 — 이건 **일꾼 스레드** 몫 (`② 일꾼`).
- 짐 지도에서 고친 곳: 7.6.12 커널 한 줄 → 네 줄 (P 칸 · 포트 · 만드는 수 · 일꾼 스레드), 4장 짝 `(run72) 로더 풀 +0x38 WorkerFactory` 줄의 커널 쪽 `[공백]` → `[정적]`, 7.6.9 커널 줄 "`+0x198` bit 11 뜻 `[공백]`" → `LoaderWorker`, 7.28 ② · 7.31 ④ `(해결됨)` · 7.28 **5** `(일부 해결)` (PDB 에 타입 없음 → `dq` 로 칸 오프셋, 값은 동적 그대로). 모두 체크리스트 밖 (7.6) · 짝 표라 **2장 숫자에 안 든다.**
- 사용자가 말한 "3장 run72 커널 칸" 은 짐 지도 **4장 (두 공간 짝)** 의 `(run72)` 줄이다 — 짐 지도 3장엔 run72 커널 칸이 없다 (run125 문서 "결정할 것" 의 "3장" 은 4장을 가리킨 것).
- 사진 맞춤 `[추측]` (run125 8 그대로): run44 `ActiveThreads` P1a 1 → P1b 3 (일꾼 2) 은 "첫 넣기 1명 + 연쇄 1명" 과 어긋나지 않는다. 어느 풀 · 어느 계기인지는 동적 (Q226).

## 확인된 것

- `[정적]` `LdrpInitializeExecutionOptions +0x733` 은 IFEO 키 없음 갈래 (`+0xad → +0x604`) 에서도 `+0x71c` 에서 합쳐져 `RCX = 0` 으로 닿는다. 건너뛰는 건 상태가 음수일 때뿐.
- `[정적]` `LdrpIllegalCWDDevices` 0 은 KUSD `CurDirDevicesSkippedForDlls = 0` (`[관찰]` run44) 때문. `+0x40` 이 0 을 명시적으로 쓴다.
- `[정적]` exe TLS 블록 = `(End − Start) + max(정렬, 0x10)` = `0x11` (`LdrpAllocateTls +0xb7 · +0xd1 ~ +0xd5 · +0xec · +0xf0`), 0 채움 없음, `SizeOfZeroFill` 안 읽음. PE 바이트 `End − Start = 1`, `SizeOfZeroFill = 0`.
- `[정적]` 스케줄러 공유 데이터 섹션 = `PspSchedulerSharedDataRegionCreate +0x127` 상수 `0x2000` — `[관찰]` run44 VAD 2쪽과 맞음.
- `[정적]` `LoadNotificationsSent` 를 켜는 줄 = `LdrpSendPostSnapNotifications +0xce` 하나 (끄는 줄 0) — `[관찰]` 사진 다섯 모듈 1 · ntdll 0 과 맞음.
- `[정적]` shim 엔진 길 문지기 = `+0x19d7` · `+0x19dc` 둘. `g_ShimsEnabled = 1` 은 `LdrpLoadShimEngine +0x148` 하나. 입구 셋. `g_ShimsLoading` 1 / 0 · `+0x129` 0 은 레지스터 출처까지 `[정적]`.
- `[정적]` 모듈 항목 `+0xc0` 을 쓰는 줄은 `SbUpdateSwitchContextBasedOnDll +0xd9` 하나.

## 정적과 동적이 다른 곳

- 없음. 사진 값 (KUSD `0y00` · `LdrpIllegalCWDDevices` 0 · TLS 칸 0 주소 · VAD 2쪽 · 모듈 비트 3 · `g_Shims*` 0) 이 모두 정적 길과 맞는다.

## 정정

이전 문서 맨 위에 한 줄씩 넣었다 (결과서 파일은 안 고침):

- **run65** — 닫힘 + 작은 정정: "`+0x733` 이 닿는지는 안 봤다 (IFEO 키가 없어 IFEO 를 건너뛴다)" → **닿는다** (`RCX = 0`). "마스크는 어느 쪽이든 0" → test.exe 에선 0 이지만 이유는 **KUSD 정책 0** (코드가 보장하지 않음). TLS 블록 `0x11`B `[추측]` → `[정적]`. 스케줄러 섹션 크기 → 커널 상수 `0x2000`. 짐 지도 7.6.5 의 "0 채움 (0B)" 은 틀린 말 — 0 채움 단계 자체가 없다 (짐 지도만 고침, run65 문서엔 그 말이 없음).
- **run66** — 닫힘: "`+0xce` 말고 켜는 줄은 안 봤다 → syscall 5번 `[추측]`" → 켜는 줄 `+0xce` 하나 (끄는 줄 0) — "모듈 다섯" 은 `[정적]`, syscall 수는 `[추측]` 그대로. shim 엔진 길 조건 → 문지기 둘 · 입구 셋.
- **run125** — 닫힘: "짐 지도 7.6.12 커널 줄 · 3장 (run72) 줄을 채울지 — 다음 짐 지도 바퀴" → run130 에서 채움 (짐 지도 7.6.12 · 4장 짝). 그림 2 노드 (가) 는 사용자 결정으로 지금은 안 넣음.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 질문 | 맞나 | 한 줄 |
|---|---|---|
| Q124 (a) `LdrpPolicyBits` | **맞음** | 공사 중 빈 함수 + 파일 값 `0x16` |
| Q124 (b) `+0x733` | **맞음** | 결과서가 본 `Src/ntdll/LdrInit.cpp` 줄 (`:1621` · `:1477 ~ 1483` · `:1596`) 이 "`RCX = RDI = 0`, KUSD `+0x2d5` = 0 → 0" 과 같음. IFEO 값 갈래는 test.exe 에 없음 |
| Q124 (c) TLS 블록 | **맞음** | 진짜 `LdrpInitializeTls` → `LdrpAllocateTls` 를 그대로 돌려 `0x11` 이 저절로 나옴. VM 파일이 `sample/bin/test.exe` 와 같은지만 확인 필요 |
| Q124 (d) 섹션 크기 | **맞음 (알고 줄임)** | 클래스 `0x70` 기본 대답, 섹션 · 매핑 없음, `TEB+0x1850 = 0`. 만들 땐 `0x2000` 한 상수 |
| Q127 (a) 비트 3 | **맞음** | 진짜 `LdrpSendPostSnapNotifications` 로 `+0xce` 가 켬 |
| Q127 (b) shim 길 | **맞음 (결과 같음, 갈래만 다름)** | 우리는 `PEB+0x2d8 = 0` 이라 `+0x19da JZ`, 진짜는 블록이 있고 `+0x210 = 0` 이라 `+0x19e3 JZ`. 둘 다 `LdrpInitShimEngine` 안 부름 |
| Q127 (c) `SwitchBackContext` | **확인 필요** (sast/116 그대로) | 블록 0 이라 `SbGetCurrentSwitchContext` 0 → `+0xd9` 안 탐 → 모듈 `+0xc0` 0 (사진은 값 있음) |

## 아직 확인 필요

동적 (모두 **선택** — 결과서 "dast 필요 없음". 대기열 번호는 오케스트레이터가 붙임, 멈춤점은 test.exe 조건):

- ① VM 의 test.exe TLS 디렉터리가 파일과 같은지 — exe 기준 `+0x79500` 에서 `dq … L5` (`0x…79f90 · 0x…79f91 · 0x…82ce0 · 0x…6e380 · 0x0010000000000000`).
- ② `ntdll!LdrpAllocateTls+0xfa` 에서 `r r8` = `0x11`, 돌아온 뒤 칸 0 − 8 의 숨긴 주소 = 칸 0 − `0x10`.
- ③ `ntdll!LdrpQueryIllegalCWDDevices` 입구 `r rcx` = 0 · 그 뒤 `dd ntdll!LdrpIllegalCWDDevices L1` = 0 · `db 0x7ffe02d5 L1` 비트 4~5 = 0.
- ④ `nt!PspSchedulerSharedDataRegionCreate+0x158` 에서 `r r8` = `0x2000` (프로세스 이름 조건).
- ⑤ `ntdll!LdrpInitShimEngine` · `ntdll!LdrpLoadShimEngine` · `ntdll!LdrInitShimEngineDynamic` 에 **안 서면** 맞음. 서면 `LdrpInitializeProcess+0x19dc` 에서 `dd poi(@rsp+0x180)+0x210 L1`.
- ⑥ 모듈 하나 (예: kernel32) 항목 `+0x68` 에 `ba w4` — 비트 3 이 켜지는 RIP = `LdrpSendPostSnapNotifications+0xce` 인지.
- (덤) Q226 그대로 — run44 일꾼 2개가 어느 풀 · 어느 계기로 생겼나 (`nt!ExpWorkerFactoryCreateThread` 입구 `k` · hit 수).

정적 (결과서 "다음 요청 후보"):

- ① (중간) 블록 `[pShimData+0x210]` 를 누가 쓰나 — sast/116 `[공백]` 그대로 (부모 쪽 kernelbase `BasepGetAppCompatData` → apphelp 길, `LdrInitShimEngineDynamic` 의 진짜 호출자). shim 이 걸린 exe 로 바뀌는 날의 문턱.
- ② (중간) 커널 `PspSchedulerSharedDataRegionSlotAllocate` · `…SlotFree` 본문 — 섹션 `0x2000` 안 한 칸 크기 · 스레드마다 나누는 법 (sast/107 후보 1 그대로, `.asm` 있음). PE_emulator 가 클래스 `0x39` 를 진짜 값으로 바꿀 때.
- ③ (작음, 필요한지는 오케스트레이터 판단) DLL 쪽 TLS 블록 크기 — kernel32 · KernelBase · dll1 · dll2 중 TLS 디렉터리가 있는 것의 `Characteristics` · `End − Start` 를 PE 바이트로 계산 (식은 이번에 확정).
- ④ (낮음, flow-writer 보탬) `LdrpMapCleanModuleView +0xa8` · `AvrfMiniLoadDll +0x8f8` 의 `[reg+0xc0]` 쓰기가 모듈 항목인지 (결과서 `[공백: 낮음]`).

## 바꾼 곳

- flow.md: 기준 (pdb 줄 둘 · 근거 로그 줄 run130 · PE 바이트 줄), 그림 메모 (run62 · run65 "넣을지는 사용자와 정한다" → 사용자 결정 · 새 run130 메모 — 그림 자체는 그대로), 2.4 제목 · run65 절 (정책 · CWD 마스크 · 스케줄러 크기 · TLS 크기 줄), 2.5 제목 · run66 알림 절 (비트 3 전수 · shim 게이트 · `+0xc0` 전수), 3장 run125 절 (결정 둘 표시), 4장 (run62 · run65 · run66 · run125 줄 `(해결됨)` · 새 run130 줄 둘), 5장 (근거 run · 요청서 번호).
- 짐 지도: 기준 · 판 줄, 2장 메모 (숫자 그대로 · 셈 판단), 3.5 `LoadNotificationsSent` · `SwitchBackContext`, 3.8 `g_ShimsEnabled` 둘 (`+0x148` · `+0x129`) · `g_ShimsLoading` 둘 · `LdrpIllegalCWDDevices` (3장 고친 줄 10 — 3.5 2 · 3.8 5 · 3.9 3, 새 줄 · 칸 없음), 3.9 `g_pfnApphelpCheckModuleProc` · `g_pfnSE_InstallAfterInit` · `g_pfnSE_DllLoaded`, 4장 짝 (run72 줄), 7.6.5 (스케줄러 섹션 · TLS 블록), 7.6.9 커널 줄, 7.6.12 (커널 줄 네 줄 · 짐 수), 7.24 · 7.25 · 7.28 · 7.31 `(해결됨)` 표시, 새 7.54, 8장.

## 도구 메모

- 디버거를 안 썼다. 새 함정 없음.
- 결과서는 사진 줄 번호를 앞 run 문서에서 그대로 옮겼다 (비트 3 `_P1b.txt:3392 …`) — flow-writer 가 run66 · 짐 지도 3.5 줄과 같은지 맞춤 (같음).
- 디버거가 붙어서 달라지는 점 (정적): `LdrpInitializeExecutionOptions +0x6c0 CMP [PEB+2]` (`BeingDebugged`) 가 0 이 아니면 `+0x6c6 ~ +0x715` 가 환경 변수를 읽어 `NtGlobalFlag |= 0x70` 하는 길을 탄다 (결과서). 커널 디버거만 붙은 run44 는 `BeingDebugged` 0 이라 안 탄다 `[추측: 유저 디버거를 붙이면 탐]`. 그 길에서 변환이 실패하면 `ESI < 0` → `+0x72e JS` 로 `+0x733` 을 건너뛸 수 있다.

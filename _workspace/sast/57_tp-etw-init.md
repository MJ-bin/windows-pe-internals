# 57 tp-etw-init — 정적 분석 요청

- 날짜: 2026-11-03
- 스프린트: 03_sprint (짐 지도 바퀴)
- 대기열: **Q118** (PE_emulator 요청 10, 사용자 결정 2026-11-03: 다음으로, Q104 VM 단계 앞. 급함 정도 보통 — PE_emulator 54 바퀴가 도는 동안 끝나면 55 바퀴가 씀)
- 관련: run45 (sast/39 work-list — 일 54 "스레드 풀 기본 준비와 ETW 등록", P1~P1a), run51 (sast/45 — `EtwEventRegister`·`LdrpEnableUMGLTracingStateSync` 의 커널 쪽 끝 syscall), run18 (sast/13 — `TpInitializePackage`·`RtlInitializeExceptionLog` 이름만), baggage 3.8 ntdll `.data` 전역
- 원문: `../PE_emulator/_workspace/00_planner/54_loader-heap-state.md` "Q2" 절 (읽기만, 고치지 않음)

## 왜 묻나

- PE_emulator 는 진짜 nt·ntdll 과 같은 이름·호출관계로 에뮬레이터를 짓는다. 54 바퀴에서 `LdrpInitializeProcess +0xaa9 ~ +0xb7d` 를, 다음 55 바퀴에서 `+0xb83 ~ +0xc2d` 를 C++ 로 쓴다.
- 이 구간 함수 대부분이 `ms/disassemble/ntdll/` 에 본문 `.asm` 이 없다 (`_index.tsv` 에 이름·크기만). 본문이 없으면 PE_emulator 는 "공사 중" 빈 함수로만 남긴다.
- PE_emulator 에는 "호출 약속" 덫이 있다 — 함수가 syscall 을 몇 번 부르는지 미리 약속하고 실행에서 맞춰 본다. 그래서 **syscall 이름·횟수·조건**이 꼭 필요하다.

## 알고 싶은 것

**모든 함수에 같은 네 가지를 묻는다:**
- (1) 무엇을 쓰나 — ntdll 전역 (이름·rva·크기), TEB/PEB 칸, 인자로 받은 구조체 칸 (오프셋·크기·쓰는 값)
- (2) syscall 을 부르나 — 부르면 `NtXxx` 이름, 몇 번, 어떤 조건에서. 직접 부르지 않고 하위 함수 안에서 부르면 그 사슬 (하위 함수 + 오프셋 → … → syscall)
- (3) 부르는 하위 함수 — call/ret 오프셋 둘 다
- (4) test.exe 길에서 타는 갈래 — 조건과 근거 (사진 값·run 문서 값이 있으면 같이). 정적으로 못 정하면 "동적 필요"

1. **`TpInitializePackage`** (rva `0xdc484`, 193B, `LdrpInitializeProcess +0xb83/+0xb88`) — 실패 (`+0xb8a JS`) 때 어디로 가나도 같이
2. **`TpSetDefaultPoolCpuSets`** (rva `0x121c38`, 77B, `+0xba2/+0xba7`) — `+0xb90 ~ +0xb99` 조건 (매개변수 블록 `+0x438` 이 0 이면 안 부름). test.exe 에서 `+0x438` 값이 0 인가 (사진·run32 기록으로)
3. **`+0xba7 ~ +0xbd0` 블록** — `+0x43c` 가 0 아니면 `TppPoolpGlobalPoolLock` 잡고 `TppPoolpGlobalPoolMaxThreadsOverride` 에 씀. test.exe 에서 타나 (`+0x43c` 값)
4. **`LdrpEnableUMGLTracingStateSync`** (rva `0x95794`, 234B, `+0xbe2/+0xbe7`, `.asm` 있음) — PE_emulator 원문 목록엔 없지만 같은 구간이라 같이 본다 (오케스트레이터 기본값). 부르는 조건 `UseWOW64 == 0` 이고 `LdrpIsSecureProcess == 0` (`+0xbd0 ~ +0xbe0`). run51 의 사슬 `… → RtlpWnfRegisterTpNotification +0x7c NtSetWnfProcessNotificationEvent` 을 줄기 끝까지 다시 확인하고 (1)~(4) 로 정리
5. **`TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation`** (rva `0x171708`, 155B, `+0xbee/+0xbf3`, 인자 `RCX = 0x1801cc6f0` — Ghidra 주석은 `NormalizationListHead+0x48` 인데 이름을 믿지 말 것. 이 주소의 진짜 정체 (PDB 이름이 있는 TraceLogging 공급자 구조체인가) 를 찾아 줄 것)
6. **`EtwEventRegister`** (rva `0x71450`, 72B) — 이 구간 직접 호출 2번의 인자를 따로:
   - `+0xc06/+0xc0b`: `RCX = PrivateLoggerNotificationGuid` (`0x18017b4d0`), `RDX = 0`, `R8 = 0`, `R9 = g_hPrivLoggerNotificationProvider` (`0x1801d33c0`)
   - `+0xc23/+0xc28`: `RCX = UserDiagnosticGuid` (`0x180190830`), `RDX = 0x180147e40` (콜백 — 이름?), `R8 = 0`, `R9 = g_hUserDiagnosticProvider` (`0x1801d3d98`)
   - GUID 두 개의 값 (16바이트 그대로), 콜백 함수 이름, 핸들 출력 자리에 무엇을 쓰나
   - **`EtwEventRegister` 가 이 구간에서 모두 몇 번 불리나** — 직접 2 + `TraceLoggingRegisterEx…` 안 (`+0x55`) + `RtlInitializeHeapLogging` 안 (`+0x1e`, 그리고 `+0x3e` 의 `TraceLoggingRegisterEx…` 안). 그중 **syscall 까지 가는 것은 어느 것인가** — run51 은 `EtwEventRegister +0x1e → EtwNotificationRegister +0x15c → EtwpRegisterProvider +0xaf → EtwpRegisterTpNotificationOnce +0x91 NtTraceControl(0x1b)` 로 적었다. "Once" 이름대로 첫 번째만 syscall 을 부르나, 첫 번째는 어느 호출인가 (이 구간보다 앞에서 이미 불렸으면 그것도)
7. **`RtlInitializeHeapLogging`** (rva `0x70420`, 68B, `+0xc28/+0xc2d`)
8. **54 바퀴에서 공사 중으로 남기는 셋** (이 구간보다 앞, `+0xaa9 ~ +0xb7d`):
   - (a) `Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_IsEnabledNoReportingNoInline` (rva `0x11f8e4`, 36B, call `+0xaad/+0xab2`) → 꼬리 `…IsEnabledFallback` (rva `0x11fc1c`). 결과가 `RtlpSrwLockAllowImplicitUpgrade` 로 가는 줄. 무엇을 읽어 참/거짓을 정하나 (기능 상태 전역·KUSD 칸 등). PE_emulator 는 지금 "켜짐" 고정 (run44 사진에서 `RtlpSrwLockAllowImplicitUpgrade` 는 끝까지 0, baggage 7장 "끝까지 0" 목록) — 이 고정이 맞나
   - (b) `Feature_Servicing_LoaderSnapsBuffer__private_IsEnabledDeviceUsageNoInline` (rva `0x109dac`, 39B, call `+0xac0/+0xac5`) → 꼬리 `…IsEnabledFallback` (rva `0x10e7b8`). 참이면 `LdrpInitializeSnapsBuffer` (rva `0xfb520`, 86B, `+0xac9/+0xace`, `.asm` 있음). test.exe 에서 참인가. PE_emulator 는 스냅 버퍼를 건너뜀 (`LdrpLogInternal +0x85 ~ +0x8c` 가 NULL 이면 건너뜀) — 건너뛰어도 되나
   - (c) `RtlInitializeExceptionLog` (rva `0x122248`, 73B, call `+0xb00/+0xb05`) — 부르는 조건 (플래그 `0x800000` — 어느 칸의 비트인가), test.exe 에서 타나
9. **매개변수 블록 `+0x430 ~ +0x43c`** — 아래 "이미 아는 것" 의 PDB 답을 asm 과 맞춰 보기만 한다 (크기·읽는 줄)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^TpInitializePackage$` | 1 | 1 (하위 `TppQueryMaximumGroupCount` 는 줄기 위면 끝까지) |
| ntdll | `^TpSetDefaultPoolCpuSets$` | 0 | 2 |
| ntdll | `^LdrpEnableUMGLTracingStateSync$` | 줄기 끝까지 | 4 (`.asm` 있음) |
| ntdll | `^TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation$` | 1 | 5 (하위 `EtwEventSetInformation`) |
| ntdll | `^EtwEventRegister$`, `^EtwNotificationRegister$`, `^EtwpRegisterProvider$`, `^EtwpRegisterTpNotificationOnce$` | 줄기 끝까지 | 6 |
| ntdll | `^RtlInitializeHeapLogging$` | 1 | 7 |
| ntdll | `^Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_IsEnabled` , `^Feature_Servicing_LoaderSnapsBuffer__private_IsEnabled` | 1 | 8(a)(b) — 꼬리 `Fallback` 포함, 공용 기능 상태 함수가 있으면 한 번만 보고 이름으로 |
| ntdll | `^LdrpInitializeSnapsBuffer$` | 1 | 8(b) (`.asm` 있음) |
| ntdll | `^RtlInitializeExceptionLog$` | 0 | 8(c) |
| ntdll | (refs) `RtlpSrwLockAllowImplicitUpgrade`, `TppPoolpGlobalPoolMaxThreadsOverride` (`0x1801d22a8`), `g_hPrivLoggerNotificationProvider` (`0x1801d33c0`), `g_hUserDiagnosticProvider` (`0x1801d3d98`), `0x1801cc6f0` | — | 쓰는 곳 전부 |

- 공용 부품 (`RtlAcquireSRWLockExclusive`·`RtlReleaseSRWLockExclusive`·`memset`·`RtlCopyMemory`·`RtlAllocateHeap`·`RtlCreateTagHeap`·`__security_check_cookie`·`RtlRestoreLastWin32Error`) 은 본문을 다시 보지 않고 이름으로 가리킨다. 단 syscall 을 부를 수 있으면 (예: SRW 락 경합 길) 그 조건만 한 줄로.
- 이름이 `_index.tsv` 에 없으면 바로 "없다" 하지 말고 `pdb_aliases.py --where ntdll.dll <이름>` 으로 한 번 더 찾는다.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- 대상 10개 모두 깊이 5, 상태 expanded. caller 수: `TraceLoggingRegisterEx…` 10, `EtwEventRegister` 7, 나머지 1
- 직접 호출 (지도 edges):
  - `TpInitializePackage`: `+0x14/+0x19 memset` · `+0x36/+0x3b RtlCreateTagHeap` · `+0x61/+0x66 NtQuerySystemInformation` (alias — 같은 주소 이름이 하나 더, 이름을 믿지 말 것) · `+0x80/+0x85 TppQueryMaximumGroupCount`
  - `TpSetDefaultPoolCpuSets`: `+0x16/+0x1b RtlAcquireSRWLockExclusive` · `+0x32/+0x37 RtlCopyMemory` (alias 3개) · `+0x48` 꼬리 `RtlReleaseSRWLockExclusive`
  - `TraceLoggingRegisterEx…`: `+0x55/+0x5a EtwEventRegister` · `+0x6e/+0x73 EtwEventSetInformation` · `+0x7d/+0x82 __security_check_cookie`
  - `EtwEventRegister`: `+0x1e/+0x23 EtwNotificationRegister` · `+0x2b/+0x30 RtlRestoreLastWin32Error` (alias 1개)
  - `RtlInitializeHeapLogging`: `+0x1e/+0x23 EtwEventRegister` · `+0x3e/+0x43 TraceLoggingRegisterEx…`
  - `RtlInitializeExceptionLog`: `+0x19/+0x1e RtlAllocateHeap` · `+0x35/+0x3a memset`
  - `Feature_Servicing_DisallowSharedLockImplicitUpgrade…NoInline`: `+0x20` 꼬리 `…Fallback`
  - `Feature_Servicing_LoaderSnapsBuffer…DeviceUsageNoInline`: `+0x1e` 꼬리 `…Fallback`

## 이미 아는 것

- `[정적]` `LdrpInitializeProcess` 구간 asm (`ms/disassemble/ntdll/LdrpInitializeProcess.asm`, 오케스트레이터가 읽음):
  - `+0xb7d` `NtdllBaseTag` 에 EAX (앞 `RtlCreateTagHeap` 결과) 저장 → `+0xb83 CALL TpInitializePackage` → `+0xb8a JS` (실패면 함수 시작 기준 `0x18008ce7f − 0x18008c404 = +0xa7b` 로 — 결과서에서 다시 확인)
  - `+0xb90 MOV EDX,[R13+0x438]` → 0 이면 건너뜀 → `+0xb9b MOV RCX,[R13+0x430]` → `+0xba2 CALL TpSetDefaultPoolCpuSets` (R13 = 매개변수 블록 `[추측]` — 결과서에서 확인)
  - `+0xba7 MOV EBX,[R13+0x43c]` → 0 이면 `+0xbd0` 으로 → 아니면 `TppPoolpGlobalPoolLock` 잡고 `+0xbbe` `TppPoolpGlobalPoolMaxThreadsOverride` 에 EBX → 풂
  - `+0xbd0 CMP UseWOW64, R15D` / `+0xbd9 CMP LdrpIsSecureProcess, R15B` → 둘 다 같으면 `+0xbe2 CALL LdrpEnableUMGLTracingStateSync` (R15 = 0 `[추측]`)
  - `+0xbe7 LEA RCX,[0x1801cc6f0]` → `+0xbee CALL TraceLoggingRegisterEx…`
  - `+0xc06`, `+0xc23` `EtwEventRegister` 인자는 위 질문 6 대로
  - `+0xc28 CALL RtlInitializeHeapLogging` → `+0xc2d CALL RtlpInitEnvironmentBlock` (이번 범위 밖)
- `[정적]` PDB (`pdb_struct.py ntdll.dll _RTL_USER_PROCESS_PARAMETERS`, 크기 `0x450`): `+0x430 DefaultThreadpoolCpuSetMasks` (`unsigned __int64*`, 8B) · `+0x438 DefaultThreadpoolCpuSetMaskCount` (`unsigned long`, 4B) · `+0x43c DefaultThreadpoolThreadMaximum` (`unsigned long`, 4B) · (다음 칸 `+0x440 HeapMemoryTypeMask`)
- `[정적]` baggage 3.2: `+0x430` 은 `PspCopyAndFixupParameters` 가 새 주소 기준으로 Fixup (run32), 사진 `K1 이전` → `P1~P1a` 에 바뀜
- `[정적]` run51: `LdrpEnableUMGLTracingStateSync → … → RtlpWnfRegisterTpNotification +0x7c NtSetWnfProcessNotificationEvent`, `EtwEventRegister +0x1e → EtwNotificationRegister +0x15c → EtwpRegisterProvider +0xaf → EtwpRegisterTpNotificationOnce +0x91 NtTraceControl(0x1b)` — 커널 쪽 `EPROCESS.EtwDataSource`·`WnfContext` 를 만드는 syscall (baggage 3.11)
- `[관찰]` run44 사진: `RtlpSrwLockAllowImplicitUpgrade` 끝까지 0 (baggage 7장). `TppPoolpGlobalPoolMaxThreadsOverride`·`g_hUserDiagnosticProvider` 는 baggage 에 이름만 있음 — 사진 값이 `ms/logs/04_baggage-read_P1.txt`·`_P1a.txt` 에 있으면 P1 → P1a 변화로 (4) 를 뒷받침할 것

## 이번에 안 하는 것

- `RtlpInitEnvironmentBlock` (`+0xc2d`) 부터 뒤 — 다음 구간
- 커널 쪽 `NtTraceControl`·`NtSetWnfProcessNotificationEvent` 본문 (run51 몫)
- 공용 부품 본문 (위 "대상" 아래 목록)
- ETW 이벤트 쓰기 (`EtwEventWrite*`) 쪽 — 등록만

## 받고 싶은 것

- 함수별 (1)~(4) 답 + 근거 `+0x` 줄 (call/ret 둘 다, 점프 목적지는 "목적지 − 함수 시작")
- **syscall 표**: 함수 / 사슬 / `NtXxx` / 횟수 / 조건 / test.exe 에서 타나 — 이 구간 (`+0xb83 ~ +0xc2d`) 전체 합계 한 줄, 54 공사 중 셋 (`+0xaad ~ +0xb05`) 따로 한 줄
- PE_emulator 에 줄 한 줄 결론: 함수마다 "정적으로 닫힘 / 동적 필요", 54 의 고정값 (`RtlpSrwLockAllowImplicitUpgrade` "켜짐", 스냅 버퍼 건너뜀) 이 맞는지
- 동적으로만 아는 값 (Q104 VM 단계에 얹을 수 있게 — 멈춤점 `ntdll+rva` 와 찍을 것)

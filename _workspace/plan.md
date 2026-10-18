# 분석 진행표

- 마지막 갱신: 2026-10-19 (sast/02 스크립트 시험 성공 뒤)
- 다음 항목: Q1
- 근거: `runs/01_sprint/flow.md` 그림 1·2, 2장, 4장·4.1 표

## 대기열 (위에서부터 처리, 새 항목은 맨 아래에)

| # | 항목 | 단계 | 정적/동적 | 들어온 곳 | 상태 |
|---|---|---|---|---|---|
| Q1 | 2.1 `nt!NtCreateUserProcess` callee (depth 1) | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 대기 |
| Q2 | 2.1 `nt!PspAllocateProcess`, `nt!MmCreatePeb` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 대기 |
| Q3 | 2.1 `nt!PspAllocateThread`, `nt!PspInsertProcess` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 대기 |
| Q4 | 2.1 `nt!PspInsertThread`, `nt!PspCallProcessNotifyRoutines` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 대기 |
| Q5 | 2.2 첫 스레드를 풀어 주는 함수 (nt 쪽 이름, `_index.tsv`로 확인) + 부모 쪽에서 그걸 부르는 곳 (`KERNELBASE!CreateProcessInternalW` 등, 이름 확인 필요) | 1 | 정적 (ntoskrnl, kernelbase) | 처음 (4장 2.2) | 대기 |
| Q6 | 2.3 `nt!KiStartUserThread`, `nt!PspUserThreadStartup` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 2) | 대기 |
| Q7 | 2.3 유저 첫 위치가 `ntdll!LdrInitializeThunk`로 정해지는 곳, `RtlUserThreadStart`와의 관계 | 1 | 정적 (ntoskrnl) | 처음 (4장 2.3) | 대기 |
| Q8 | 2.4 `ntdll!LdrInitializeThunk`, `LdrpInitializeInternal`, `LdrpInitialize` callee | 2 | 정적 (ntdll) | 처음 (그림 2); sast/02 결과 (`+0x16c` 의 caller 이름이 `_LdrpInitialize`, `LdrpInitialize` 는 꼬리호출(JMP)로 `LdrpInitializeInternal` 에 넘김 → flow.md 2.4 이름 정정 필요) | 대기 |
| Q9 | 2.4 `ntdll!LdrpInitializeProcess` callee (크면 그 자리에서 Q9a/Q9b로 나눔) | 2 | 정적 (ntdll) | 처음 (그림 2) | 대기 |
| Q10 | 2.5 `LdrpInitializeKernel32Functions`, `LdrLoadDll`, `LdrpLoadDll` callee | 2 | 정적 (ntdll) | 처음 (그림 2) | 대기 |
| Q11 | 2.5 `LdrpLoadDllInternal` (`+0x284` 앞 = 올리기 쪽), `LdrpPrepareModuleForExecution` callee | 2 | 정적 (ntdll) | 처음 (그림 2, 4장 2.5) | 대기 |
| Q12 | 2.5·2.6 올리면서 의존 관계 그래프를 만드는 함수 (Q11 결과에서 이어감) | 2 | 정적 (ntdll) | 처음 (4장 2.5·2.6) | 대기 |
| Q13 | 2.5 KERNELBASE·KERNEL32 시작 함수 이름 (PE 진입점 → PDB 이름) | 2 | 정적 (kernelbase, kernel32) | 처음 (4장 2.5) | 대기 |
| Q14 | 2.6 dll1·dll2를 올리는 쪽 (`LdrpInitializeProcess+0x1bb6` 앞 호출 중 import 처리) | 2 | 정적 (ntdll) | 처음 (4장 2.6) | 대기 |
| Q15 | 2.7 `LdrpInitializeGraphRecurse`, `LdrpInitializeNode` callee | 2 | 정적 (ntdll) | 처음 (그림 2) | 대기 |
| Q16 | 2.7 `LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `ImageTlsCallbackCaller` callee | 2 | 정적 (ntdll) | 처음 (그림 2, 4장 2.7) | 대기 |
| Q17 | 2.10 로더 끝 → 진입점: `LdrInitializeThunk` 뒤 `NtContinue`, `RtlUserThreadStart` callee | 2 | 정적 (ntdll) | 처음 (그림 2 PH4) | 대기 |
| Q18 | 2.10 `KERNEL32!BaseThreadInitThunk` callee | 2 | 정적 (kernel32) | 처음 (그림 2) | 대기 |
| Q19 | 2.2 첫 스레드가 실제로 풀리는 시점 (부모 스레드와의 순서) | 3 | 동적 | 처음 (4장 2.2) | 대기 |
| Q20 | 2.5 kernel32·kernelbase가 메모리에 올라오는 순간 (`NtMapViewOfSection` 계열 + `k`) | 3 | 동적 | 처음 (4장 2.5) | 대기 |
| Q21 | 2.5 `LdrpCallInitRoutine` → KERNELBASE·KERNEL32 시작 함수 실제 진입 + 콜스택 | 3 | 동적 | 처음 (4.1 (1)) | 대기 |
| Q22 | 2.5 `LdrpInitializeNode` [KERNELBASE]/[KERNEL32] 대응 (긴 `k`) | 3 | 동적 | 처음 (4.1 (1)) | 대기 |
| Q23 | 2.6 dll1·dll2가 메모리에 올라오는 순간 | 3 | 동적 | 처음 (4장 2.6) | 대기 |
| Q24 | 2.7 `LdrpInitializeGraphRecurse` 재귀 단계 ↔ 모듈 대응 (`k 8` 이상) | 3 | 동적 | 처음 (4.1 (2)) | 대기 |
| Q25 | 2.7 `LdrpCallInitRoutine` → `ImageTlsCallbackCaller` (`k`), 그 안쪽 | 3 | 동적 | 처음 (4.1 (2)(3), 4장 2.7) | 대기 |
| Q26 | 2.8 `ImageTlsCallbackCaller` → `Dll2Tls`/`Dll1Tls` (`k`) | 3 | 동적 | 처음 (4.1 (2), 4장 2.8) | 대기 |
| Q27 | 2.8 `LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain` (`k`) | 3 | 동적 | 처음 (4.1 (2)) | 대기 |
| Q28 | 2.9 `ImageTlsCallbackCaller` → `test!ExeTls` (`k`) | 3 | 동적 | 처음 (4.1 (3)) | 대기 |
| Q29 | 2.10 `LdrInitializeThunk` → `NtContinue` → `RtlUserThreadStart` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝) | 대기 |
| Q30 | 2.10 `RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝) | 대기 |
| Q31 | 2.10 `mainCRTStartup` → `LoadLibraryExW` ×3 → `main` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝, 4장 2.10) | 대기 |

상태: `대기` / `진행 중` / `끝 (runN)` / `합침 → Qn` / `보류 (사용자 결정, 날짜)`

- 단계 3 항목은 그때까지 정적으로 채워지면 `합침 → Qn` 또는 끝으로 바꾼다 (이미 정적으로 답이 나온 것은 동적으로 다시 보지 않는다).
- `nt!KiSystemServiceCopyEnd`(시스템 콜 입구)는 callee가 간접 호출뿐이라 단계 1 항목에서 뺐다.
- test.exe 바이너리는 서버에 없어서 `mainCRTStartup` 안쪽은 정적 항목이 없다 (Q31 동적).

## 결정 기록
- 2026-10-19: 처음 대기열 Q1~Q31 사용자 확인 ("확인")
- 2026-10-19: Q1 전에 스크립트 시험을 먼저 한다 (사용자 요청). sast/01_script-check = 대기열 항목 아님. ntoskrnl analyze는 뒤에서, ntdll로 알려진 오프셋 대조. 스크립트 문제는 고치지 않고 보고만 (`.claude/`는 사용자가 고침)
- 2026-10-19: sast/01 실패 (JDK 25 + Ghidra 11.3.2 OSGi 로더). 사용자가 JDK 21 설치·`launch.properties` 지정. 사용자 허락으로 ntoskrnl 작업 종료, PDB 없는 Ghidra 프로젝트 2개 삭제 → sast/02_script-check 로 재시도
- 2026-10-19: sast/02 실패 (JDK 25 때 만든 Ghidra 스크립트 캐시 `~/.config/ghidra/ghidra_11.3.2_PUBLIC/osgi/compiled-bundles/d5ff6acd` 재사용). 사용자 허락으로 ntoskrnl 작업 종료·PDB 없는 프로젝트 삭제·캐시 삭제 → 같은 요청서 sast/02로 한 번 더 (사용자 결정)
- 2026-10-19: sast/02 재실행 성공. ntdll 알려진 오프셋 8/8 일치 (전부 ret_off). ntoskrnl analyze 뒤에서 도는 중 (PID 847212, PDB 붙음)

# 분석 진행표

- 마지막 갱신: 2026-10-21 (run12 뒤)
- 다음 항목: Q5
- 근거: `runs/01_sprint/flow.md` 그림 1·2, 2장, 4장·4.1 표

## 대기열 (위에서부터 처리, 새 항목은 맨 아래에)

| # | 항목 | 단계 | 정적/동적 | 들어온 곳 | 상태 |
|---|---|---|---|---|---|
| Q1 | 2.1 `nt!NtCreateUserProcess` callee (depth 1) | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 끝 (run9) |
| Q2 | 2.1 `nt!PspAllocateProcess`, `nt!MmCreatePeb` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 끝 (run10) |
| Q3 | 2.1 `nt!PspAllocateThread`, `nt!PspInsertProcess` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1) | 끝 (run11) |
| Q4 | 2.1 `nt!PspInsertThread`, `nt!PspCallProcessNotifyRoutines` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 1); sast/06 결과 (`PspAllocateThread` 가 잡은 스레드 락을 풀지 않음 → `PspInsertThread` 에서 푸는지) | 끝 (run12) |
| Q5 | 2.2 첫 스레드를 풀어 주는 함수 (nt 쪽 이름, `_index.tsv`로 확인) + 부모 쪽에서 그걸 부르는 곳 (`KERNELBASE!CreateProcessInternalW` 등, 이름 확인 필요) | 1 | 정적 (ntoskrnl, kernelbase) | 처음 (4장 2.2) | 대기 |
| Q6 | 2.3 `nt!KiStartUserThread`, `nt!PspUserThreadStartup` callee | 1 | 정적 (ntoskrnl) | 처음 (그림 2); ntoskrnl analyze (2026-10-19): `_index.tsv` 에 `KiStartUserThread` 없음, `KxStartUserThread` 있음 (PDB엔 `KiStartUserThread`·`KiStartUserThreadReturn` 이름 있음) → 이름 대응 확인 | 대기 |
| Q7 | 2.3 유저 첫 위치가 `ntdll!LdrInitializeThunk`로 정해지는 곳, `RtlUserThreadStart`와의 관계 | 1 | 정적 (ntoskrnl) | 처음 (4장 2.3); sast/03 결과 (`PspCreateUserContext` 91바이트 — `NtCreateUserProcess +0xd22` 에서 부름, 첫 스레드 유저 시작 문맥 후보); sast/06 결과 (`PspAllocateThread` 는 CONTEXT.Rip→StartAddress, Rcx→Win32StartAddress 로 옮겨 적기만 함, `PspUserThreadStartup` 주소는 `+0x681` 에서 `KeInitThread` 로 → CONTEXT 를 누가 채우나) | 대기 |
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
| Q19 | 2.2 첫 스레드가 실제로 풀리는 시점 (부모 스레드와의 순서) | 3 | 동적 | 처음 (4장 2.2); sast/03 동적 1·2 (`NtCreateUserProcess` 입구 ThreadFlags bit0, `PspInsertThread` 의 `KeSuspendThread` `+0x2e5`·`KeReadyThread` `+0x5a6` 탔는지); run12 (멈춘 첫 스레드도 `KeReadyThread` 를 거침 → 실제로 서는 시점·풀리는 시점, `KeSuspendThread` 안쪽) | 대기 |
| Q20 | 2.5 kernel32·kernelbase가 메모리에 올라오는 순간 (`NtMapViewOfSection` 계열 + `k`) | 3 | 동적 | 처음 (4장 2.5) | 대기 |
| Q21 | 2.5 `LdrpCallInitRoutine` → KERNELBASE·KERNEL32 시작 함수 실제 진입 + 콜스택 | 3 | 동적 | 처음 (4.1 (1)) | 대기 |
| Q22 | 2.5 `LdrpInitializeNode` [KERNELBASE]/[KERNEL32] 대응 (긴 `k`) | 3 | 동적 | 처음 (4.1 (1)) | 대기 |
| Q23 | 2.6 dll1·dll2가 메모리에 올라오는 순간 | 3 | 동적 | 처음 (4장 2.6) | 대기 |
| Q24 | 2.7 `LdrpInitializeGraphRecurse` 재귀 단계 ↔ 모듈 대응 (`k 8` 이상) | 3 | 동적 | 처음 (4.1 (2)) | 대기 |
| Q25 | 2.7 `LdrpCallInitRoutine` → `ImageTlsCallbackCaller` (`k`), 그 안쪽 | 3 | 동적 | 처음 (4.1 (2)(3), 4장 2.7); sast/05 지도 (`ImageTlsCallbackCaller` 는 호출 참조 0, 데이터 참조뿐 — `LdrpCallTlsInitializers+0x18f` 에서 주소만 꺼냄 → 정적 호출 지도로는 못 닿음) | 대기 |
| Q26 | 2.8 `ImageTlsCallbackCaller` → `Dll2Tls`/`Dll1Tls` (`k`) | 3 | 동적 | 처음 (4.1 (2), 4장 2.8) | 대기 |
| Q27 | 2.8 `LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain` (`k`) | 3 | 동적 | 처음 (4.1 (2)) | 대기 |
| Q28 | 2.9 `ImageTlsCallbackCaller` → `test!ExeTls` (`k`) | 3 | 동적 | 처음 (4.1 (3)) | 대기 |
| Q29 | 2.10 `LdrInitializeThunk` → `NtContinue` → `RtlUserThreadStart` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝); sast/05 지도 (`RtlUserThreadStart` 는 CALL/JMP 참조 0 — `NtContinue` 의 SYSCALL 에서 정적 길이 끊김) | 대기 |
| Q30 | 2.10 `RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝) | 대기 |
| Q31 | 2.10 `mainCRTStartup` → `LoadLibraryExW` ×3 → `main` (`k`) | 3 | 동적 | 처음 (4.1 로더 끝, 4장 2.10) | 대기 |
| Q32 | 2.2 `nt!PspMapThreadCreationFlags` 본문 (ThreadFlags → 최종 플래그 bit0 가 그대로 가나) | 1 | 정적 (ntoskrnl) | sast/03 결과 (정적 후보) | 대기 |
| Q33 | 2.1 `nt!MmCreateSpecialImageSection` callee (exe 이미지 섹션, `MiCreateImageFileMap` 과의 관계) | 1 | 정적 (ntoskrnl) | sast/03 결과 (정적 후보, points.md [확인 필요]) | 대기 |
| Q34 | 2.1 `NtCreateUserProcess` 실행 길: 간접 호출 2곳(`+0x5b2`, `+0x11bb`) 대상, `PspCreateUserContext` 길 vs `PspGetContextThreadInternal` 길, 이미지 섹션 재시도, 반환값 | 3 | 동적 | sast/03 동적 3~6, run9 flow.md 4.1 | 대기 |
| Q35 | 2.1 `PspAllocateProcess` 실행 길: `MmInitializeProcessAddressSpace` `+0x2095`(보통) vs `+0x2113`(복제) 중 탄 쪽, 간접 호출 2곳(`+0x121a` @0xf04d28, `+0x17c4` @0xf04d20) 대상, `PspWow64PickBestNtdll` `+0xffb` 타는지, `+0x2314`·`+0x239c` 에서 새 PEB `+0x10`·`+0x20` 값 | 3 | 동적 | sast/04 동적 확인 | 대기 |
| Q36 | 2.1 `nt!PspCopyAndFixupParameters`(PEB `+0x20` 프로세스 매개변수를 누가 쓰나), `nt!PspPrepareSystemDllInitBlock`(ntdll 로 넘기는 시작 정보) | 1 | 정적 (ntoskrnl) | sast/04 결과 (다음 정적 후보) | 대기 |
| Q37 | 지도 크기 재기 (실험): 정적 호출 관계를 뿌리에서 끝까지 따라가 깊이별 함수 수를 재고, 끊기는 노드에 끊긴 이유를 적는다. 뿌리 = 커널 `nt!NtCreateUserProcess`, 유저 `ntdll!LdrInitializeThunk` | - | 정적 (ntoskrnl, ntdll) | 사용자 제안 (2026-10-21) | 끝 (run 없음, 도구 실험 — 결과서 sast/05) — 사용자 결정으로 Q3 앞에서 처리 |
| Q38 | 2.1 `PspAllocateThread`·`PspInsertProcess` 실행 길: 입구 11번째 인자(`poi(@rsp+0x58)`) 가 0인지(보통 길), `+0x6f4` 에서 `KeInitThread` 시작 정보(`dqs @rdx`), 먼 덩어리(`nt+0xb0d315`) 를 WinDbg 가 무슨 이름으로 보이나(`ln`), `PspInsertProcess+0x115` 에서 test.exe 가 목록에 보이나, `CONTEXT.Rip/Rcx` ↔ `_ETHREAD.StartAddress/Win32StartAddress` 비교, `PspInsertProcess+0x1e3` 앞뒤 핸들 표, `PspSetupUserStack`·`MmCreateTeb` 반환값과 TEB 주소 | 3 | 동적 | sast/06 동적 확인, run11 4.1; 묶음 M 시험 (`PspSetupUserShadowStack` `+0xb65` 를 타는지 = 이 VM 에서 CET 섀도 스택이 켜졌나) | 대기 |
| Q39 | 2.1 `PspInsertThread`·`PspCallProcessNotifyRoutines` 실행 길: 입구 flags(`poi(@r9)`) bit0·`0x20`, `+0x2e0` 멈춤 여부, `+0x5dd`/`+0x5a1` 전후 스레드 상태, 프로세스 락이 호출자에서 들어오나, 알림 간접 호출 4곳(`+0x14d`/`+0x31c`/`+0xbe`/`+0x1cf`) 의 `ln @rax` 와 `PspNotifyEnableMask`·목록·BAM/DAM 값, `PsTerminateProcess`·잡 길을 안 타는지, 둘째 스레드 때 `+0x802` 가 안 걸리는지, 알림 문턱 값(`프로세스+0x5fc` bit0, `+0x640`), `+0x499` 에서 `poi(@rax)` | 3 | 동적 | sast/07 동적 확인, flow.md 4.1 (run12) (Q38 과 연달아 있어 한 run 으로 묶을 수 있음) | 대기 |

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
- 2026-10-19: 사용자 결정 — 하네스는 오케스트레이터가 사용자 검토 뒤 고치고, 묶음마다 하네스 전용 커밋으로 관리 (되돌리기 = `git revert`)
- 2026-10-19: 하네스 묶음 A (수정 규칙) 커밋 `e3ac6ba`, 묶음 C (ghidra-headless-export 알려진 사실 표·reference 예시·JDK 캐시 항목 정정) 커밋 `933fd83`. 사용자 검토 완료
- 2026-10-19: 하네스 묶음 B·D-1·D-2·E 사용자 OK. D-2 (analyze 건너뛰기 조건 강화, 실패면 원인 보고 후 사용자 결정) 커밋 `40df1ae`, E (check_commit.sh 한글 첫 줄 검사) 커밋 `b1fd76f`
- 2026-10-19: B (ExportFunctions 꼬리호출 `tail`·`INT` 제외), D-1 (run_ghidra.sh SCRIPT ERROR면 실패 종료)는 ntoskrnl analyze 끝난 뒤 고침 (도는 중인 스크립트). Q1은 B 뒤에 시작 (사용자 결정 "(나)")
- 2026-10-19: ntoskrnl analyze 끝 (01:02 ~ 01:54, 함수 34982개, `FUN_` 4625개, SetPdb 성공, SCRIPT ERROR 0)
- 2026-10-19: 하네스 묶음 B (ExportFunctions 꼬리호출 `tail`·`INT` → `TRAP`) 커밋 `ef63cc2`, D-1 (run_ghidra.sh SCRIPT ERROR → exit 4) 커밋 `11f2a2d`. ntdll 재export 시험 통과. Q1 시작 (sast/03)
- 2026-10-19: Q1 끝 (run9, sast/03). 새 할 일 — 사용자: "제안대로" → Q32·Q33·Q34 맨 아래 추가, `PspCreateUserContext` 는 Q7 에, ThreadFlags·KeSuspendThread 동적 확인은 Q19 에 출처만 추가
- 2026-10-19: 사용자 "F 먼저" → 하네스 묶음 F (sast/03 스크립트 문제 1~3) 사용자 검토 후 커밋: F-1 CFG `indirect:cfg:` `a7a7c92`, F-2 ICF `alias:` (Ordinal_/$thunk$/Nt·Zw 제외) `41bdb35`, F-3 조각 `frag:` `41b7428`. ntoskrnl·ntdll 재export 시험 통과. 그다음 Q2 시작 (sast/04)
- 2026-10-19: Q2 끝 (run10, sast/04). 새 할 일 Q35(동적)·Q36(정적) 맨 아래 추가 (기존 항목과 겹침 없음). 그림 1 정적 상자 8개 (요청 5~7 보다 1개 많음, MmMapViewOfSectionEx 두 번 = 상자 둘이라 그대로 둠)
- 2026-10-19: 사용자 "너무 빠르다" → 속도 규칙·해설 담당·별칭 표 제안. 사용자: 중복 이름은 추천대로, 해설에서 "스스로 확인 질문"은 뺌, 나머지 동의
- 2026-10-21: 하네스 묶음 H (바퀴 사이 멈춤, 주제 하나씩) `e568e3d`, I (explainer·run-explainer, runN-키워드-해설.md) `a09703d`, J (pdb_aliases.py 별칭 표, P19) `bdedf48`. ms/sym/*.aliases.tsv 생성 (커밋 안 함). 다음: run9·run10 해설서 채워 넣기, Q3 는 사용자 "다음" 뒤
- 2026-10-21: 사용자 "바탕 개념 절은 너무 기초적" → 하네스 묶음 K (해설서 양식에서 바탕 개념 절 제거, 채팅 설명 순서도) `a0f4164`. run9·run10 해설은 그대로
- 2026-10-21: 사용자 질문 "정적 전체 지도를 먼저 뽑는 게 빠를까?" → 비교 설명 후 사용자 결정: Q3 전에 지도 크기 재기 실험 (Q37). 뿌리 nt!NtCreateUserProcess / ntdll!LdrInitializeThunk, 끊기는 노드엔 이유. 도구는 하네스 밖 실험용 스크립트 (쓸 만하면 그때 하네스 검토)
- 2026-10-21: Q37 끝 (sast/05). 결론 — 정적 전체 지도는 커널 끝까지 11688개라 읽을 목록으론 못 씀, 흐름도 함수는 커널 깊이 5·ntdll 깊이 7 안쪽, 공용 도구(caller≥20)로 잘린 흐름도 함수 없음. 사용자: "지도는 색인, 분석은 지금처럼" → 하네스 묶음 L (CallGraphMap.java + run_ghidra.sh map) 검토 OK, 시험 통과
- 하네스 후보 (나중에 검토): CallGraphMap/ExportFunctions 의 `cfgSource()` 가 외부 주소 참조를 `@0xfffffffe…` 로 찍음 (커널 2곳, ntdll 7곳)
- 2026-10-21: 하네스 묶음 L 커밋 `6159550` (CallGraphMap.java, run_ghidra.sh map, 지도 = 색인)
- 2026-10-21: Q3 끝 (run11, sast/06). Q38(동적) 맨 아래 추가, Q4·Q7 에 출처 추가. sast/06 이 찾은 스크립트 구멍(함수에 안 속한 코드 덩어리 안 호출이 _calls.tsv·지도에서 빠짐) 은 바퀴 끝난 뒤 사용자에게 따로 제안
- 2026-10-21: 사용자 승인 → 하네스 묶음 M (주인 없는 코드 덩어리를 본래 함수 몫으로 읽기, ExportFunctions·CallGraphMap). 재추출 결과 이전 바퀴 숫자 변화 없음 (NtCreateUserProcess 83, PspAllocateProcess 144 등 그대로), PspAllocateThread 만 37→49줄 (run11 에 이미 표로 있음) → run9·10·11 정정 필요 없음. 지도 커널 기준20 5694→5757, 흐름도 함수 41개 변화 없음. .pdata 대조 720/720
- 2026-10-21: 하네스 묶음 M 커밋 `cb41881`
- 2026-10-21: Q4 끝 (run12, sast/07). 그림 1 의 nt 함수(Q1~Q4) 끝. 단계 1 남은 처음 항목: Q5·Q6·Q7. Q39(동적) 맨 아래 추가. flow.md 2.2 "+0x598 에서 다른 길" 정정 (실패 길도 +0x59e 로 돌아와 KeReadyThread)

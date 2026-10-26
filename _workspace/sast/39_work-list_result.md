# 39 work-list — 정적 분석 결과 (일 목록)

- 요청서: `_workspace/sast/39_work-list.md` (03_sprint 짐 지도 단계 4, 대기열 **Q81**. 목록 만들기와 분류만 한다)
- 모듈 / 바이너리 / PDB
  - ntoskrnl: `ms/bin/.../System32/ntoskrnl.exe`, pdb guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
  - ntdll: `ms/bin/.../System32/ntdll.dll`, pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1` (기준과 같음)
  - 두 프로젝트 모두 이미 analyze 끝남 (`.gpr` + `_index.tsv` + `SetPdb:` 줄). **다시 analyze 안 했다.** 도는 Ghidra 프로세스 없음.
- 읽은 근거: `runs/03_sprint/flow.md` (그림 1·2, 2.1~2.10, 3장, 4장 — run9~run44 의 합본), `runs/03_sprint/baggage.md`, `runs/03_sprint/run44-baggage-snap.md`, `_workspace/sast/38_baggage-checklist_result.md`, `_workspace/sast/03_create-user-process_result.md`, `runs/02_sprint/run10-allocate-process.md`, `_workspace/plan.md` (대기열 Q1~Q81), 그리고 callee 표 `ms/disassemble/{ntoskrnl,ntdll}/_baggage/callees.tsv` (15 함수, 637줄 + 머리 2줄 = 639줄).
  - run 문서(run3~run44)·결과서(sast/01~38)를 줄 단위로 다 다시 읽지는 않았다. 그 내용은 flow.md·baggage.md 가 이미 합쳐 둔 것을 믿고 썼다 (두 문서가 각 문단 끝에 run 번호를 달아 두었다). 개별 run 문서를 직접 본 곳은 run10(묶음 표)·sast/03(NtCreateUserProcess 83줄 표) 뿐이다.
- **Ghidra 를 새로 돌렸다 (읽기만, analyze 아님).** 분류가 안 되던 함수(callee 표 `doc=none` 이거나 본문이 없던 것)만 depth 0 으로 2번, 합계 14개, 각 4초 안팎:
  - 1차 6개: `LdrpInitializeNlsInfo` `RtlpInitParameterBlock` `RtlpInitEnvironmentBlock` `LdrpInitializeCfgScpHelpers` `LdrpInitializeImportRedirection` `LdrInitializeMrdata` — 이름만으로는 (가)/(나)/(다) 를 못 가르던 것.
  - 2차 8개: `RtlpInitCodePageTables` `RtlpQueryNlsSystemCodePages` `RtlpInitUppercaseTables` `SbObtainTraceHandle` `LdrpInitializeSmtDelayedSleep` `RtlpInitializeThreadActivationContextStack` `LdrpInitializeSnapsBuffer` `LdrpCaptureCriticalThunks` — 1차에서 나온 NLS 길과 `doc=none` ntdll 이름을 마저 가르려고.
  - 명령: `bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh export ntdll.dll '^(…)$' 0` ×2. 결과는 `ms/decomp/ntdll/<함수>.c`·`ms/disassemble/ntdll/<함수>.asm`, `_calls.tsv` 는 1,559줄 → 1,625줄 (줄어든 줄 없음). 디컴파일 코드는 결과서에 안 붙였다.
- 버전 확인: `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines`, `LdrInitializeThunk +0xe → LdrpInitialize`, `LdrpInitializeInternal +0x5a → _LdrpInitialize`, `_LdrpInitialize +0x16c → LdrpInitializeProcess`, `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse` — `_calls.tsv`·`callees.tsv` 에서 **6/6 일치**.
- 스크립트 문제: **없음.** 스크립트(`.claude/`)는 안 고쳤다.
- 만든 파일 (커밋 안 함, `ms/` 는 `.gitignore`): `ms/disassemble/ntoskrnl/_baggage/callees_by_work.tsv`, `ms/disassemble/ntdll/_baggage/callees_by_work.tsv` (callee 637줄 각각의 일 번호), 임시 스크립트 `ms/ghidra/_tmp_sast39/`.

## 한눈에

- **일 86개.** 분류: (가) 짐 지도 칸으로 덮임 **69** (그중 (나) 도 겸하는 것 45) · (나) 체크리스트 밖 짐을 남김 **48** (가와 겸 45 + (나) 만 3) · (다) 흐름도에만 있음 **14**.
  - 즉 **일 86개 중 48개가 체크리스트 밖에도 짐을 남긴다.** 사용자의 짐작("이미지 페이지 안 IAT·재배치 값, 다른 DLL 의 섹션·파일 객체, KERNELBASE·KERNEL32 전역, csrss 쪽 기록") 이 **맞다.** 거기에 더해 체크리스트에 칸 표가 없는 커널 이웃 객체, 힙 안 블록, 일꾼 스레드 2개의 객체 일체, 핸들 표 항목, 부모 cmd.exe 쪽 기록이 나왔다 (3장 C1~C12).
- **callee 표 637줄 → 일 번호**: 일에 들어간 줄 433, 공용 부품 146, 실패·정리 길 49, 조각 점프(호출 아님) 9, **어느 쪽에도 못 넣은 줄 0**. `doc=none` 57개도 전부 어느 일에 들어가거나 공용 부품이다. callee 표 때문에 **새로 세운 일은 3개**(53·54·61, 전부 ntdll 의 `doc=none` 이름)뿐이고 나머지 줄은 문서에 있던 일에 들어갔다. 거꾸로 **표에 한 줄도 안 나오는 일이 19개** (부모 쪽, csrss, DLL 올리기 공통 부품 …) — 이것들은 문서에서 찾았다 (5장).
- **대기 항목과 이음**: 대기 중인 Q 항목 41개(Q78~Q81 제외) 가 모두 어느 일에 걸린다 (Q30·Q31·Q60 은 "진입점 뒤" 경계). 거꾸로 **일 14개는 구체적인 대기 항목이 없다** (Q78·Q79·Q80 만 걸리거나 아무것도 없음) — 그중 "새 할 일" 은 4장.
- **이번 바퀴에 본문을 읽어 닫은 빈 곳 (baggage.md 7장) 6건**: ① `PEB.ProcessParameters` 를 힙으로 옮기는 주인 = `RtlpInitParameterBlock` (+ `RtlpInitEnvironmentBlock`), ② `LdrInitializeMrdata` 가 쓰는 전역 3개 (`.data` 1 · `.mrdata` 2), ③ `LdrpInitializeCfgScpHelpers` 가 쓰는 `.mrdata` 칸 2개, ④ `PEB.ActiveCodePage`·`OemCodePage` 쓴 자리 + NLS 섹션 길, ⑤ import 리다이렉션은 test.exe 에서 안 탐, ⑥ 3.1 의 `ProcessParameters` 줄에 쓴 주인 하나 더 (6장).
- **제안 (정하는 건 사용자)**: (나) 는 Q78 의 방식(칸 → 쓴 자리)으로는 대부분 안 보인다. Q78 을 넓히기보다 **옆 항목 4~5개** 를 두는 쪽을 권한다 — 3장 끝 "제안". 가장 싸고 효과가 큰 것은 **기존 덤프 8개로 일꾼 스레드 2개 객체·VAD→파일 사슬·핸들 생긴 구간을 읽는 것** (Q79 에 붙임).

## 읽는 법

- **일 번호**: 시간 순서다 (부모 입구 → 커널 `NtCreateUserProcess` 안 → 부모 마무리 → 첫 스레드 커널 쪽 → ntdll). 다음 바퀴에서 "일 29" 처럼 부른다. 같은 번호를 3장(나 표)·4장·5장에서도 쓴다.
- **한 일 = 한 덩어리 작업.** DLL 하나를 올리는 일은 kernel32·kernelbase·dll1·dll2 가 **같은 부품**(일 70 ~ 79)을 거치므로 부품별로 한 번만 센다 (모듈마다 따로 안 셈).
- **분류**: **가** = 결과가 sast/38 체크리스트의 구조체 21종 칸이나 ntdll 전역(`.data`·`.mrdata`·`.00cfg`) 에 남는다. **나** = 결과가 체크리스트에 없는 곳에 남는다. **다** = 짐을 안 남기거나 잠깐 쓰고 지운다 (검사·락·조회·알림·조건부로 안 타는 길). 둘 다면 "가·나" — 가: 뒤에 칸, 나: 뒤에 어디.
- **구간**: baggage.md 의 사진 구간 (`K1 이전` `K1~K2` … `P1b~P2`). "사진 전" = 부모 쪽이라 사진 지점이 없음. `—` = 조건 갈래.
- **사슬**: baggage.md 약어를 쓴다. `①…` = 부모 cmd.exe 스레드의 `NtCreateUserProcess`, `②…` = `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess`. `+0x…` 는 **돌아올 자리(ret)** 다 (WinDbg 콜스택과 같은 기준), `→` 부름, `⇒` 꼬리 JMP, `┄▶` 다른 프로세스로 넘어감. 오프셋은 `_calls.tsv`·`callees.tsv` 의 ret_off.
- **태그**: `[관찰]` 디버거·사진으로 본 것, `[정적]` 코드에 쓰여 있는 것, `[정적: 이름]`·`[추측: 이름]` 함수 이름만 보고 짐작한 것 (본문 안 읽음), `[정적+샘플]` sample PE 를 센 것, `[공백]` 아직 아무것도 안 봄.
- **대기 항목 열**: 그 일의 실행 확인·남은 정적이 걸린 plan.md 대기열 번호. `(끝)` 은 이미 끝난 항목 (참고용). `새 할 일`·`없음` 은 4장.
- 비유 하나: 일 목록은 이삿날 **작업 지시서 전체**다. 짐 지도(baggage.md)는 그중 "어느 서랍에 뭘 넣었나" 만 적은 표였다. 이번에 서랍 밖에 놓인 짐(바닥 상자·옆집 창고·이웃 집 서랍)이 얼마나 되는지 센 것이다.

## 1. 일 목록 (질문 1·2·3)

일 86개. 열: 번호 · 일 · 공간 · 단계(flow.md 절 · baggage 단계) · run44 구간 · 대표 함수 사슬(지도 기준) · 근거 · 태그 · 분류 · 기존 대기 항목.

### A. 부모 쪽 입구와 커널 `NtCreateUserProcess` 안 (부모 cmd.exe 스레드)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 | 근거 | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|---|
| **1** | 부모 입구 — `CreateProcessW` 껍데기 둘 → `CreateProcessInternalW` 앞부분 (`ThreadFlags` 늘 1, `dwCreationFlags` 에 4 없으면 `ProcessFlags` bit 9, 시스템 콜 인자 만들기. 매개변수 블록 만들기는 `BasepCreateProcessParameters` `[추측]`) | 다른 프로세스 (부모 유저) | 2.2 · ① 앞 | 사진 전 | `cmd!ExecPgm +0x59c → kernel32!CreateProcessW +0x54 → KERNELBASE!CreateProcessW +0x66 → CreateProcessInternalW +0x30ee → ntdll!NtCreateUserProcess` | run14·run34·sast/09·sast/29 | `[정적]` (cmd 쪽은 import 표만, 실행은 `[공백]`) | **다** — 시스템 콜 인자만 만든다. 매개변수 블록 원본은 부모 유저 메모리에 있고, 일 6 이 커널 사본을, 일 20 이 새 프로세스 복사본을 만든다 | Q19·Q41(끝)·Q66 |
| **2** | 시스템 콜 입구 · 인자 검사 · 만들기 정보·속성 목록 풀기 · 부모 핸들·silo·권한 확인 · 메모리 파티션 문맥 | 커널 | 2.1 · ① | K1 이전 | `①… +0x29a PspBuildCreateProcessContext · +0x316 PspCaptureCreateInfo · +0x364 ObpReferenceObjectByHandleWithTag · +0x38c~+0x3c2 silo 확인 · +0x4e0 SeSinglePrivilegeCheck · +0x5e2 PspGetMemoryPartitionContext` (진입: ntdll 스텁 `0xd1` SYSCALL → `KiSystemServiceCopyEnd`) | run9·run34·sast/03 표 #1~#24 | `[관찰]` 호출 자리 / `[정적]` 안쪽 | **다** — 커널 풀의 임시 구조 (만들기 준비물). 끝에서 `PspDeleteCreateProcessContext` 로 치움 `[추측: 이름]` | Q34 |
| **3** | exe 파일 열기 (확장 파라미터 만들기 → 열기 → 실패하면 한 번 더 → 객체 참조) | 커널 | 2.1 · ① | K1 이전 | `①… +0x5ff PspCreateUserProcessEcp · +0x634 FsRtlpPrepareExtraCreateParametersForCreate · +0x6cb IopCreateFile (재시도 +0x740 IoCreateFileEx) · +0x791 ObReferenceObjectByHandle` | run9·sast/03 #23~#28 | `[정적]` | **가·나** — 가: `_FILE_OBJECT` (모름 30). 나: 열려 있는 동안의 파일 핸들 항목 (부모 쪽 또는 커널 핸들 표 `[추측]`) | Q34·Q78 |
| **4** | exe 이미지 섹션 만들기 — 헤더 읽기·검사, 컨트롤 영역·세그먼트·서브섹션·프로토 PTE, 진입점 `TransferAddress` 계산 (같은 파일 서랍이 있으면 공유 길) | 커널 | 2.1 · ① | K1 이전 | `①… +0x93c MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection →(갈림) MiCreateNewSection → MiCreateImageFileMap → MiVerifyImageHeader · MiBuildImageControlArea; → MiSectionCreated; → MiRelocateImage` (공유 길 `+0x1d5 MiShareExistingControlArea`) | run31·sast/26 | `[정적]` (결과 칸은 K1 `[관찰]`) | **가·나** — 가: `_SECTION`·`_CONTROL_AREA`·`_SEGMENT`·`_SECTION_IMAGE_INFORMATION` (아는 17줄). 나: `_SUBSECTION`·`_MSUBSECTION`·`_SECTION_OBJECT_POINTERS`·프로토 PTE·헤더 페이지 (baggage 7.6 에 이름만) | Q61·Q62·Q34·Q78 |
| **5** | 서명 수준·보호 프로세스 요구 검사, 그리고 "수준이 다르면 섹션을 닫고 다시 만들기" 고리 | 커널 | 2.1 · ① | K1 이전 | `①… +0x995 PspGetProcessProtectionRequirementsFromImage · +0x9dd SeQuerySigningPolicy · +0xa0d SeCompareSigningLevels · +0xa23 ObCloseHandle(재시도) · +0xa69·+0xbab PspValidateCreateProcessProtection · +0xa9d PspCheckForInvalidAccessByProtection · +0xe94 PsTestProtectedProcessIncompatibility` | run9·run31·sast/03 #38~#44·#62 | `[정적]` | **다** — 검사·판정이다. 나온 값은 일 8 이 프로세스 객체 칸에 담을 것 `[추측: 이름]` | Q34 |
| **6** | 프로세스 매개변수 커널 사본 만들기 (부모가 준 블록을 커널 풀에 새로 복사, 입력 마스크 `0xf4010e11` 검사) | 커널 (부모 유저 메모리를 읽음) | 2.1 · ① | K1 이전 | `①… +0xab8 PspGetProcessParameterOverrides · +0xacb PspCaptureProcessParameters → PspCaptureUserProcessParameters` | run32·sast/27 | `[정적]` | **다** — 임시 풀 사본. 새 프로세스로 복사된 뒤엔 안 쓴다. 값의 쓰임은 일 20 | Q63(a)(e)·Q78 |
| **7** | 새 프로세스 토큰 정하기·붙이기 — 부모 토큰 참조·복제(가장 끄고 켜기), 권한·세션 확인, 변조 방지 정책 | 커널 | 2.1 · ① | K1 이전 | `①… +0x41c PspReferenceTokenForNewProcess · +0x804 SeDuplicateTokenWithPredictedClaims · +0x8e2 SeDuplicateTokenAndAddOriginClaim → PspAllocateProcess +0x147f SeIsTokenAssignableToProcess · +0x1927 PspInitializeProcessSecurity · +0x264f SeConstructAntiTamperPolicy · +0x26b1 SeSetTokenTamperProtection` | sast/04·sast/38 callee 표 (이름만) | `[추측: 이름]` | **가** — `_TOKEN` (모름 55)·`_EPROCESS.Token`. 사진: 토큰 8장이 같은 주소지만 부모 토큰과 같은지는 안 읽음 | Q78 |
| **8** | 프로세스 객체 만들기 + exe 이미지 정보 복사 (파티션·섹션 참조, 런다운 보호, 머신 종류, 에너지 문맥) | 커널 | 2.1 · ① | K1 이전 | `①… +0xc36 PspAllocateProcess +0x539 ObCreateObjectEx · +0x835 PsReferencePartitionSafe · +0x991 MmGetSectionInformation · +0xa8c PspSelectMachineForProcess · +0x75c PoEnergyContextInitialize` | run10·sast/04 | `[관찰]` 호출 자리 / `[정적]` | **가·나** — 가: `_EPROCESS` (아는 22, 모름 241). 나: 객체 헤더 `_OBJECT_HEADER` (프로세스 객체 앞 0x38바이트 — 칸 표 없음) | Q35·Q78 |
| **9** | 실행 옵션(IFEO) 읽기 — 커널 쪽 (성능 옵션·노드 옵션) | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0xa61 RtlpOpenImageFileOptionsKeyEx · +0xb16~+0xe94 RtlQueryImageFileKeyOption ×8 · +0xcd2 PspReadIFEONodeOptions · +0xe59 PspReadIFEOPerfOptions · +0x19e0 PspApplyIFEOPerfOptions` | run10 묶음 3·sast/38 callee 표 (이름만) | `[추측: 이름]` | **가** — `_EPROCESS`·`_KPROCESS` 의 옵션 칸 (어느 칸인지 모름). 레지스트리 열기·닫기는 짐이 아님 | Q78 |
| **10** | 세션·쿼터·프로세서 그룹(NUMA)·친화도·CPU Set·우선순위·quantum 정하기 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x14d0 PsGetProcessSessionIdEx · +0x1524 PspAttachSession · +0x1594 PspAssignProcessQuotaBlock · +0x12b8 PspSelectNodeForProcess · +0x1a05 KeSetCpuSetsProcess · +0x1a40 PspSetProcessPriorityClass · +0x1a64 PspComputeQuantumAndPriority · +0x2364 PspSetProcessAffinitySafe` | run10 묶음 5·6·7 (이름만) | `[추측: 이름]` | **가·나** — 가: `_EPROCESS`·`_KPROCESS`. 나: 쿼터 블록 (부모와 공유 `[추측]`), 세션 공간 참조 수 | Q78 |
| **11** | 주소 공간 껍데기 — 최상위 페이지 표(PML4), 커널 쪽 매핑 잇기, 세션에 넣기, 커밋 청구 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x163c MmCreateProcessAddressSpace → MiAllocateTopLevelPage · MiCreateNewProcessTopLevelMappings · PsJoinSession` (+ `+0x1692 MmInitializeHandBuiltProcess`, `+0x19b1 MmInitializeHandBuiltProcess2`) | run10 | `[정적]` | **가·나** — 가: `_KPROCESS.DirectoryTableBase` (모름). 나: 최상위 페이지 표 내용·PFN 항목 | Q78 |
| **12** | 커널 프로세스 구조와 핸들 표 만들기 (`KeInitializeProcess`, `ObInitProcess` — 물려줄 핸들이 있으면 같이) | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x171e KeInitializeProcess · +0x1991 ObInitProcess` | run10·sast/04 (호출 자리만, 본문 안 뽑음) | `[추측: 이름]` | **가·나** — 가: `_KPROCESS` (아는 0)·`_HANDLE_TABLE` (아는 0). 나: 핸들 표 항목들 (일 21 와 같은 곳) | Q78 |
| **13** | 완화(mitigation) 정책 정하기 — IFEO·부모에서 물려받기·강화·CET 허용 여부·앱 모델 정책 → 24바이트 완화 옵션 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x1a90 PspReadIFEOMitigationOptions · +0x1aef PspInheritMitigationOptions · +0x1c87 PspHardenMitigationOptions · +0x1c8c KeIsUserCetAllowed · +0x1eb8 PspApplyMitigationOptions · +0x1fb8 AppModelPolicy_GetPolicy_Internal · +0x200d KeInitializeProcessUserCetLogging` | run10 묶음 10·run32 | `[정적]` | **가** — `_EPROCESS` 완화 칸 (`Flags3` 비트 7개는 아는, 나머지 모름). 24바이트는 일 22 이 ntdll 블록 `+0xa0` 에 복사 | Q35·Q63·Q78 |
| **14** | 전체 이미지 이름 만들기 · 생성 알림 (`SmProcessCreateNotification`) · 세션 리더 확인 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x1ff8 SmProcessCreateNotification · +0x202e PspIsSessionLeaderProcess · +0x206a PspInitializeFullProcessImageName` | run10 묶음 11 | `[추측: 이름]` | **가·나** — 가: `_EPROCESS` 이름 칸. 나: 이미지 이름 문자열 풀 블록 | Q78 |
| **15** | 주소 공간 장부 — 작업 집합, 유저 주소 범위 계산, 메모리 구역 장부(VAD) 처음 8개 만들어 넣기 (KUSD 두 페이지 등), 예약 매핑 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x2095 MmInitializeProcessAddressSpace → +0xed MiInitializeWorkingSetList · +0x13b MiComputeProcessUserVa · +0x161 MiAllocateProcessVads · +0x1a8 MiInsertProcessVads;  +0x223e PspSetupReservedUserMappings` | run10·run44 (VAD 8개) | `[관찰]`+`[정적]` | **가·나** — 가: `_MMVAD`·`_MMVAD_SHORT` (칸 안 읽음 23). 나: 작업 집합 구조 (`Vm` 안) | Q35·Q78·Q79 |
| **16** | exe 이미지를 프로세스 메모리에 붙이기 (+ 다른 주소면 진입점 보정, CFG 준비 후보) | 커널 | 2.1 · ① | K1 이전 | `①… MmInitializeProcessAddressSpace +0x1ce MiMapProcessExecutable +0xb8 MmMapViewOfSectionEx → +0xc5 SectionBaseAddress` (+ `+0xe3 MiCfgInitializeProcess` `[정적: 지도]`, 보정 `PspAllocateProcess +0x20b3~+0x20ce`) | run10·run16·run31·run38 | `[정적]` (붙은 주소는 `[관찰]`) | **가·나** — 가: `_EPROCESS.SectionBaseAddress`·exe `_MMVAD`. 나: **test.exe 이미지 페이지** (프로토 PTE 로 이어짐 — IAT·재배치 값이 놓이는 곳, 일 59·77) | Q35·Q61·Q62(f) |
| **17** | ntdll 이미지를 프로세스 메모리에 붙이기 (부팅 때 정한 주소·공용 섹션) | 커널 | 2.1 · ① | K1 이전 | `MiMapProcessExecutable +0xdc PsMapSystemDlls → +0xc0 PspMapSystemDll +0x11d MmMapViewOfSectionEx` (+ `PspAllocateProcess +0xffb PspWow64PickBestNtdll` 조건) | run10·run16 | `[정적]` | **가·나** — 가: ntdll `_MMVAD`·(읽기) `PsNtdllExports`. 나: ntdll 이미지 페이지 (`.data`·`.mrdata` 처음 값 = 파일 값) — K1 ~ P1 에서 읽기 불가였던 161칸의 이유 `[추측]` | Q35·Q80 |
| **18** | PEB 만들기 — PEB 자리, exe 헤더에서 서브시스템·버전, 프로세서 수, 세션 번호 (+ PEB 칸 더 쓰는 후보 `KeCopyXfdMaskToPeb`·`PspUpdatePebForAffinityChange`) | 커널 (새 프로세스에 붙어서 유저 칸에 씀) | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x2314 MmCreatePeb → +0xc7 KiStackAttachProcess · +0x14b MiCreatePebOrTeb · +0x2ce RtlImageNtHeader · +0x93 KeQueryGroupMaskProcess`;  `PspSetupUserProcessAddressSpace +0x227 KeCopyXfdMaskToPeb · +0x234 PspUpdatePebForAffinityChange` | run10·run43·run44 | `[관찰]`+`[정적]` | **가** — `_PEB` (아는 31, 모름 93) | Q35·Q78 |
| **19** | 읽기 전용 뷰 셋 올리기 — leap second 데이터, API set 지도, silo 공유 데이터 | 커널 | 2.1 · ① | K1 이전 | `MmCreatePeb +0x132 MmMapViewOfSection`;  `PspSetupUserProcessAddressSpace +0x255 MmMapApiSetView · +0x263 PspMapSiloSharedDataView` | run10·run32·run44 | `[정적]` (뷰 주소는 `[관찰]`) | **가·나** — 가: `_PEB.LeapSecondData`·`ApiSetMap`. 나: 뷰가 가리키는 구역 (읽기 전용 VAD, 내용은 시스템 섹션). `ApiSetMap` 을 쓴 줄은 `[공백]` | Q78·Q20 |
| **20** | 매개변수 블록을 새 프로세스로 복사 + 안쪽 포인터 12개 고치기 + `PEB+0x20` 쓰기 (붙기 전에 커널 사본의 `Flags`·`LoaderThreads` 손봄) | 커널 (새 프로세스에 붙어서 유저 칸에 씀) | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x239c PspSetupUserProcessAddressSpace +0x19c ZwAllocateVirtualMemory · +0x1b9 PspCopyAndFixupParameters (+0x52b / +0x347)` | run32·sast/27 | `[정적]` | **가·나** — 가: `_RTL_USER_PROCESS_PARAMETERS`(아는 40)·`_PEB.ProcessParameters`. 나: 환경 블록 구역 (`Environment` 가 가리킴, 칸이 아닌 구역) | Q35·Q63·Q79 |
| **21** | 콘솔·표준 입출력 핸들을 부모에서 새 프로세스로 복제 (`ObDuplicateObject`) | 커널 | 2.1 · ① | K1 이전 | `PspCopyAndFixupParameters +0xbc / +0x130 ObDuplicateObject;  PspSetupUserProcessAddressSpace +0x92 PspGetStandardHandleList` | run32·run44 (핸들 4) | `[정적]`+`[관찰]` | **가·나** — 가: 매개변수 `ConsoleHandle`·`Standard*`. 나: 새 프로세스 핸들 표 항목 4개 (`_HANDLE_TABLE_ENTRY`, K1 핸들 4)·공유 파일 객체 참조 수 | Q63(c)·Q78 |
| **22** | 시스템 DLL 시작 정보 블록 채우기 — ntdll `LdrSystemDllInitBlock` (난수·완화 옵션·CFG 비트맵 주소·SCP 표) | 커널 (새 프로세스 ntdll `.mrdata` 에 씀) | 2.1 · ① | K1 이전 | `PspSetupUserProcessAddressSpace +0x1e6 PspPrepareSystemDllInitBlock` | run32·run43·run44 | `[정적]` (값은 K1 `[관찰]`) | **가·나** — 가: ntdll `.mrdata` `LdrSystemDllInitBlock` (타입이 PDB 에 없음 → 오프셋 표). 나: `+0xb8` 이 가리키는 CFG 비트맵 구역 | Q35·Q63(b)(d)·Q71(b) |
| **23** | 프로세스 ID 얻기(`PspCidTable`)·프로세스 락 잡기·이미지 파일 강한 참조·`KeSecureProcess`·생성 시각 | 커널 | 2.1 · ① | K1 이전 | `PspAllocateProcess +0x26e9 PspLockProcessExclusive · +0x2703 ExCreateHandleEx · +0x274a MmGetSectionStrongImageReference · +0x27a0 KeSecureProcess · +0x27c9 KeQuerySystemTimePrecise` | run10·sast/04 | `[정적]` | **가·나** — 가: `_EPROCESS` (UniqueProcessId·CreateTime·ProcessLock — 아는). 나: `PspCidTable` 안 PID 항목 | Q78 |
| **24** | 첫 스레드의 시작 문맥 만들기 (`CONTEXT`: Rip=`RtlUserThreadStart`, Rcx=진입점, Rdx=PEB) + `ThreadFlags` 변환 | 커널 | 2.1 · ① | K1 이전 (지역 값) | `①… +0xce0 RtlInitializeExtendedContext2 · +0xd22 PspCreateUserContext · +0xdd3 PspMapThreadCreationFlags` | run9·run16 | `[정적]` | **다** — 커널 스택 위 지역 CONTEXT. 값은 일 28 이 트랩 프레임에 옮긴다 | Q34·Q38 |
| **25** | 스레드 객체 만들기·스레드 ID 주기·시작 주소 옮겨 적기 | 커널 | 2.1 · ① | K1~K2 | `①… +0xe5c PspAllocateThread +0x2e7 ObCreateObjectEx · +0x51c ExfAcquirePushLockExclusiveEx(스레드 락) · +0x532 PsAssignThreadId · +0x640~+0x655 StartAddress·Win32StartAddress` | run11·sast/06 | `[관찰]`+`[정적]` | **가·나** — 가: `_ETHREAD` (아는 5, 모름 144). 나: `PspCidTable` 안 TID 항목 | Q38·Q78 |
| **26** | 유저 스택 만들기 (VAD + 스택 정보) · (CET 면 그림자 스택) | 커널 (새 프로세스에 붙어서) | 2.1 · ① | K1~K2 | `PspAllocateThread +0x5ef PspSetupUserStack` (조건 `+0xb65 PspSetupUserShadowStack`) | run11 | `[정적]` (스택 VAD 는 K2 `[관찰]`) | **가·나** — 가: 스택 `_MMVAD`. 나: 유저 스택 구역 + `InitialTeb` (타입이 PDB 에 없음) — 일 42 가 맡긴 문맥을 둘 곳 | Q38 |
| **27** | TEB 만들기 (대입 약 47줄) | 커널 (새 프로세스에 붙어서 유저 칸에 씀) | 2.1 · ① | K1~K2 | `PspAllocateThread +0x631 MmCreateTeb +0x7f KiStackAttachProcess · +0xac MiAllocateFromSubAllocatedRegion · (대입 ~47줄) · +0x29e KiUnstackDetachProcess` | run11·run43·sast/38 | `[정적]` (칸별은 `[공백]`) | **가** — `_TEB` (아는 4, 모름 133; K1~K2 에 9칸) | Q38·Q78 |
| **28** | 스레드 커널 쪽 초기화 — 커널 스택·`KernelStack`·트랩 프레임 처음 문맥·APC·이벤트·시작 정보 | 커널 | 2.1 · ① | K1~K2 | `PspAllocateThread +0x6f9 KeInitThread +0x152 KeInitializeApc · +0x163 KeInitializeEvent · +0x301 KiInitializeContextThread → KxContextToKframes` | run11·run13·run15·run16 | `[정적]` (칸 값 K2 `[관찰]`) | **가·나** — 가: `_KTHREAD`·`_KTRAP_FRAME`. 나: 커널 스택 페이지 (첫 스택 맨 위 `KiStartUserThread`, `_KEXCEPTION_FRAME` — PDB 에 있으나 표 없음) | Q38·Q42·Q78 |
| **29** | 프로세스를 시스템 목록에 넣기 — 감사·시스템 콜 제공자 상속, 목록 잠금 안 `ActiveProcessLinks`·`SequenceNumber` 직접 쓰기, 잡 상속·디버그 포트 복사(조건), 객체 등록 | 커널 | 2.1 · ① | K2~K3 | `①… +0xfc2 PspInsertProcess +0x78 SeAuditProcessCreation · +0xa6~+0x115 목록 잠금 · +0x143 DbgkCopyProcessDebugPort · +0x1e3 ObInsertObjectEx` | run11·run44 | `[관찰]`+`[정적]` | **가·나** — 가: `_EPROCESS` 목록 칸·`_HANDLE_TABLE.UniqueProcessId` (K2~K3, 쓴 줄 모름). 나: 전역 `PsActiveProcessHead` 와 이웃 프로세스의 Flink, 객체 헤더 | Q38·Q78 |
| **30** | 스레드 넣기 (첫 스레드 길) — `KeStartThread`(스케줄링 준비), 스레드 수·첫 스레드 표시, 스레드 목록 달기, 프로세스 락 풀기, 객체 등록, 스레드 락 풀기 | 커널 | 2.1 · ① | K2~K3 | `①… +0x101a PspInsertThread +0x5e2 KeStartThread · +0x266 목록 달기 · +0x2a2 락 풀기 · +0x3ea ObInsertObjectEx · +0x490 ThreadInserted · +0x499 락 풀기` | run12·run44 | `[관찰]`+`[정적]` | **가·나** — 가: `_EPROCESS`·`_ETHREAD`·`_KTHREAD` (K2~K3 에 우선순위 등 11칸). 나: 스레드 객체 헤더 | Q39·Q78 |
| **31** | 첫 스레드 멈춰 두기 — `SuspendCount` +1, `SuspendEvent` 0, 멈춤 APC 걸기 | 커널 | 2.1·2.2 · ① | K2~K3 | `PspInsertThread +0x2e5 KeSuspendThread → +0x6b KiSuspendThread → +0x88 KiInsertQueueApc · +0x1fb KiSignalThreadForApc` | run13·run44 | `[관찰]`+`[정적]` | **가** — `_KTHREAD` (SuspendCount·SuspendEvent·SchedulerApc·ApcState — 아는 줄 다수) | Q19·Q39 |
| **32** | 알림 콜백 — 프로세스 알림 4곳 (등록 목록 64칸·BAM·DAM), 스레드 알림, ETW | 커널 (받는 쪽은 드라이버·ETW) | 2.1 · ① | K3~P0 | `PspInsertThread +0x807 PspCallProcessNotifyRoutines (간접 ×4) · +0x4fa PspCallThreadNotifyRoutines · +0x4dc EtwTraceThread · +0x7d3 EtwTraceProcess;  PspInitializeThunkContext +0x11b~ 알림 루프` | run12·run16 | `[관찰]` 호출 자리 / `[정적]` | **다** — 받는 쪽 일이다. 거부하면 `PsTerminateProcess`. 받는 쪽 드라이버가 남기는 짐은 범위 밖 | Q39 |
| **33** | 부모용 핸들 만들기·출력 칸 쓰기·정리 — 프로세스 핸들, 스레드 핸들, 만들기 정보 결과(6), 임시 구조 해제 | 커널 (부모 핸들 표·부모 유저 메모리) | 2.1 · ① | K3~P0 | `①… +0x10a7 PspCreateObjectHandle (스레드: PspInsertThread +0x525) · +0x1129 PspUpdateCreateInfo · +0x11d5 PspDeleteCreateProcessContext · +0x11e6 FsRtlFreeExtraCreateParameterList` | run9·run12 | `[정적]` | **나** — 부모 cmd 의 핸들 표 항목 2개와 부모 유저 쪽 출력 칸 (핸들 값, 만들기 정보 출력 — PEB 주소 등 `[추측]`) | 없음 |
| **34** | 스레드를 CPU 대기 줄에 세우기 (`KeReadyThread` — 멈춘 스레드도) | 커널 | 2.2 · ① | K3~P0 | `PspInsertThread +0x5a6 KeReadyThread` | run9·run12·run13 | `[정적]` | **가·나** — 가: `_KTHREAD.State`·`Priority`. 나: CPU 준비 큐 (KPRCB 쪽 `[추측]`) | Q19·Q42 |
| **35** | 커널 쪽 조건 갈래 묶음 — 복제(fork) 길, WOW64, Pico·본뜨기 스레드, 보안 스레드, 잡 배정, 최소 프로세스, 실패 정리. test.exe 는 안 탄 걸로 보임 | 커널 | 2.1 · ① | — | `PspAllocateProcess +0x2113 (복제) · +0xffb PspWow64PickBestNtdll;  MmCreatePeb +0x680 MiInitializeWowPeb;  PspAllocateThread 먼 덩어리(+0x2ca…)·+0x9eb PspWow64SetupUserStack · +0xb7d PspWow64InitThread;  PspInsertThread +0x8ef PspAssignProcessToJobList · +0x93d PspCreateSecureThread;  PspUserThreadStartup +0xca·+0xe3·+0xf5` | run9~run16·run44 | `[정적]` ("안 탔다" 는 일부만 `[관찰]`: TEB 새로 만듦, 핸들 4) | **다** — 짐을 남기지 않거나 test.exe 가 안 타는 길 | Q35·Q38·Q39 |

### B. 부모 마무리 (K3 → P0 사이, 부모·csrss)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 | 근거 | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|---|
| **36** | 부모 쪽 사이 검사·후처리 (조건부) — 이미지 검사 12곳, 상승 검사, 잡 연결, 패키지 앱 후처리 | 다른 프로세스 (부모 유저) | 2.2 | K3~P0 | `CreateProcessInternalW +0x3f91 BasepCheckWebBladeHashes · +0x3fd1 BasepIsProcessAllowed · +0x40c4 BasepCheckWinSaferRestrictions · +0x47c1 BaseCheckElevation · +0x55f1 AssignProcessToJobList · +0x59cd BasepPostSuccessAppXExtension` | run14 | `[정적]` | **다** — 조건부 검사. 결과로 남는 짐 없음 | Q19 |
| **37** | 부모가 앱 호환 데이터를 자식 메모리에 써 넣기 (자식 PEB `pShimData` 후보 `[추측]`) — 활성화 컨텍스트 데이터는 csrss 의 SxS 가 쓸 수도 있음 | 다른 프로세스 (부모 유저 → 자식 유저) | 2.2 | K3~P0 | `CreateProcessInternalW +0x5755 NtAllocateVirtualMemory · +0x5795·+0x57d7·+0x5831 NtWriteVirtualMemory · +0x597d BasepInitAppCompatData` | run14·run44 | `[정적: 이름]`+`[관찰]` (구간) | **가·나** — 가: `_PEB.pShimData`·`SystemDefaultActivationContextData` (baggage 7.8 의 "다른 DLL" 첫 후보). 나: 자식 안 VAD 2개와 그 내용 (shim 데이터 블록, 활성화 컨텍스트 데이터 매핑) | Q78 (7.8) — 단 부모 KERNELBASE 쪽이라 새 할 일 |
| **38** | csrss 에 "프로세스 만들었다" 알림 — 부모가 `CsrClientCallServer`(0x1001d) → csrss 가 새 프로세스·스레드 핸들을 직접 열고 자기 목록에 등록 | 다른 프로세스 (csrss) + 커널 (csrss 의 핸들) | 2.2 | K3~P0 | `CreateProcessInternalW +0x4672 CsrClientCallServer ┄▶ (csrss) csrsrv!CsrApiRequestThread +0x4e6 → basesrv!BaseSrvCreateProcess2 → InternalBaseSrvCreateProcess +0x17c CsrCreateProcess → CsrpCreateProcess +0x50c CsrInsertProcess` | run14·run34·sast/29 | `[정적]` (실행은 `[공백]`) | **나** — csrss 쪽 프로세스·스레드 기록 (csrsrv 전역/힙), csrss 핸들 표 항목 2, 자식 객체 참조 수. 요청서 범위상 "이름·한 줄까지만" | Q19(낮음)·Q66(b) |
| **39** | 부모가 첫 스레드 풀어 주기 — `NtResumeThread` → `PsMultiResumeThread`(`ProcessFirstResume` 켬) → `KeResumeThread`(`SuspendCount` −1) → `KiResumeThread`(`SuspendEvent` 1, 깨우기) | 커널 (부모 스레드) | 2.2 · ① | K3~P0 | `CreateProcessInternalW +0x5bd3 NtResumeThread(SYSCALL 0x52) → PsMultiResumeThread +0x4e → KeResumeThread +0x68 → KiResumeThread +0x20 · +0xab KiTryUnwaitThread` | run13·run14·run33·run44 | `[정적]` (구간 `[관찰]`) | **가** — `_EPROCESS.Flags3.ProcessFirstResume`·`_KTHREAD.SuspendCount`·`SuspendEvent` (아는 줄) | Q19·Q64 |

### C. test.exe 첫 스레드 커널 쪽 (⓪)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 | 근거 | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|---|
| **40** | 멈춘 채 첫 CPU 에 올라 → IRQL 내림 → 멈춤 APC 를 처리하며 잠들었다 깨기 (사진 P0 에서 `SuspendCount` 는 이미 0) | 커널 | 2.2·2.3 · ① | K3~P0 | `SwapContext 끝 RET → KiStartUserThread → PspUserThreadStartup +0x22 → KiApcInterrupt +0x3ab → KiDeliverApc +0x220 → KiSchedulerApc +0xf7 → KeWaitForSingleObject(SuspendEvent)` | run13·run15·run33 | `[정적]` (실제로 잤는지 `[공백]`) | **가** — `_KTHREAD` (`SuspendSchedulerApcWait` 은 썼다 되돌려 끝까지 0) | Q42·Q19 |
| **41** | 스레드 시작 코드 잡일 — 주 토큰 교환 막기, 세션 로케일 → TEB, 스케줄링 속성 → TEB, 스레드 만들기 알림 | 커널 | 2.3 · ① | P0~P1 | `KiStartUserThread → PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange · +0x78 PsGetSessionLocaleId · +0x99 KeUpdateTebSchedulingPropertiesCurrentThread · +0xa1 PspNotifyThreadCreation` | run15·run44 | `[정적]`+`[관찰]` | **가** — `_TEB` (CurrentLocale 아는; IdealProcessor·PrimaryGroupAffinity 모름)·`_EPROCESS` (CreateReported·ImageNotifyDone·ImagePathHash·PrimaryTokenFrozen — P0~P1, 쓴 줄 모름 `[추측]`) | Q78·Q42 |
| **42** | 원래 시작 문맥을 유저 스택에 맡기고, 트랩 프레임을 `LdrInitializeThunk` 로 바꿔 끼우기 | 커널 (유저 스택에 씀) | 2.3 · ① | P0~P1 | `PspUserThreadStartup +0xb3 PspInitializeThunkContext +0x16d PspGetContextThreadInternal · +0x285 RtlCopyContext · +0x29a KePopulateContinuationContext · +0x2db PspSetContextThreadInternal → PspSetContext +0x68` | run16·run42·run44 | `[정적]`+`[관찰]` | **가·나** — 가: `_KTRAP_FRAME` (Rip·Rsp·Rcx·Rdx — 아는)·`_KTHREAD.SpecialApcDisable`. 나: 유저 스택 위 맡긴 `_CONTEXT` (0x4d0, P1 에서 읽었음 `[관찰]` — 칸 표가 없음) | Q42·Q29·Q57·Q75 |
| **43** | 커널 → 유저 첫 번째 `IRETQ` | 커널 → 유저 | 2.3 · ① | P0~P1 | `KxStartUserThread 끝 조각 +0x9c IRETQ` 또는 `KiKernelExit +0xa5` (`KiKvaShadow` 로 갈림) | run15 | `[정적]` | **다** — 전환일 뿐 짐 없음 | Q42 |

### D. ntdll 입구와 `LdrpInitializeProcess` 앞쪽 (P1 → P1a)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 | 근거 | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|---|
| **44** | ntdll 입구 잡일 — 보안 쿠키, 보안 프로세스 표시, 특수 프레임 표시, CPU 기능 표 (끝쪽에서 `RtlInitializeProcessorFeaturesBitMap`·`SymCryptDetectCpuFeaturesByCpuid` 로 한 번 더) | 유저 | 2.4 · ② | P1~P1a (쿠키) / P1b~P2 (CPU 기능 끝쪽) | `LdrInitializeThunk +0xe → LdrpInitialize +0x41 InitSecurityCookie · +0x27 InitSpecialMachineFrames · +0x2c __cpu_features_init;  ②… +0x1766 RtlInitializeProcessorFeaturesBitMap · +0x1d9e RtlIsProcessorFeaturePresent · +0x1df4 SymCryptDetectCpuFeaturesByCpuid` | run17·run19·run44 | `[정적]`+`[관찰]` | **가** — ntdll `.data`·`.mrdata` 전역 (`__security_cookie`·`SecurityCookieInitialized`·`SpecialMachineFramesInitialized`·`LdrpIsSecureProcess` 아는; `x86_cpu_enable_*`·`g_SymCrypt*` 모름) | Q43·Q80·Q78 |
| **45** | 핫패치 준비 — 처음 온 스레드 판정, 이벤트 만들기, 핫패치 켜짐 확인(`NtManageHotPatch`), 켜졌으면 패치 ntdll 올리기, 준비 끝 표시 | 유저 | 2.4 · ② | P1~P1a | `LdrpInitialize ⇒ LdrpInitializeInternal +0x3b CMPXCHG · +0x93 NtCreateEvent · +0x98 LdrpInitializeHotPatching · +0xc2 LdrpLoadPatchedNtdll · +0xdf LdrpInitializationComplete` | run17·sast/12 | `[정적]` (사진: `LdrpHotPatchInitialized` P1a 에 2) | **가·나** — 가: `.data` (`LdrpHotPatchInitialized`·`LdrpIsHotPatchingEnabled`·`LdrpNtdllHotPatchContext` — 아는). 나: 이벤트 핸들 표 항목 1개 `[추측: 이름 모름]` | Q43·Q67(c) |
| **46** | 첫 스레드 판정 · 로더 완료 이벤트 · 로더 락·`ProcessInitializing`·`LdrInitState` · `.mrdata` 준비 (`LdrInitializeMrdata`) | 유저 | 2.4 · ② | P1~P1a | `LdrpInitializeInternal +0x5a _LdrpInitialize +0x3a CMPXCHG · +0xea NtCreateEvent(LdrpInitCompleteEvent) · +0x14a LdrInitializeMrdata` | run17·sast/12; 이번 바퀴 `LdrInitializeMrdata.c` 직접 | `[정적]`+`[관찰]` | **가·나** — 가: PEB `LoaderLock`·`ProcessInitializing`·TEB `RanProcessInit`·전역 (아는 다수) + **전역 3칸: `.data` `LdrpMrdataLock`, `.mrdata` `LdrpAllocationGranularity`·`LdrpMaximumUserModeAddress` (이번에 본문 읽어 쓴 자리 닫음 `[정적]`)**. 나: 이벤트 핸들 항목 (`0xc` `[관찰]`) | Q43·Q78 (`LdrInitializeMrdata` 본문) |
| **47** | `PebLdr` 빈 목록 3개·`PEB.Ldr`·난수·NLS(코드 페이지) 정보 — 레지스트리에서 코드 페이지 번호 읽기, NLS 파일 섹션 얻기(`NtGetNlsSectionPtr` ×3) | 유저 + 커널 (syscall) | 2.4 · ② | P1~P1a | `②… +0x158 LdrInitializePerThreadRng · +0x1c1 PEB.Ldr · +0x1cd LdrpInitializeNlsInfo → RtlpQueryNlsSystemCodePages(NtOpenKey·NtQueryValueKey) · RtlpInitCodePageTables(NtGetNlsSectionPtr ×2) · RtlpInitUppercaseTables(NtGetNlsSectionPtr)` | run18; 이번 바퀴 `LdrpInitializeNlsInfo.c`·`_calls.tsv` 직접 | `[정적]` (VAD 11→21 안의 NLS 파일 이름은 run44 `[관찰]`) | **가·나** — 가: `_PEB_LDR_DATA`(아는 7)·`_PEB.Ldr`·**`ActiveCodePage`·`OemCodePage`·`UseCaseMapping` (쓰는 줄 확인 `[정적]`)**·ntdll NLS 전역. 나: NLS 섹션 매핑 (`C_949.NLS`·`l_intl.nls` 읽기 전용 VAD, 커널 `NtGetNlsSectionPtr` 몫 `[추측]`) | 새 할 일 |
| **48** | exe NT 헤더 잡기(`LdrpAppHeaders`) · 보안 프로세스 검사 · 모듈 해시 표 비우기 · ntdll 예외 표를 역함수 표에 등록 · `.mrdata` 열고 닫기 · CFG 강제 여부 · 중요 thunk 기록 | 유저 | 2.4 · ② | P1~P1a | `②… +0x363 RtlImageNtHeaderEx · +0x41a RtlInsertInvertedFunctionTable · +0x446 LdrProtectMrdata · +0x486 LdrpCaptureCriticalThunks · +0x492 LdrControlFlowGuardEnforcedWithExportSuppression · +0x3de LdrpHandleUserCopyAttributeList · +0x292 RtlpInitializeNonVolatileFlush` | run18·sast/13 | `[정적]` | **가** — `.data`·`.mrdata` 전역 (`LdrpAppHeaders`·`LdrpHashTable` 아는; `LdrpInvertedFunctionTables`·`LdrpDetourExist` 등은 모름·`≤P1a`) | Q44·Q67(a)·Q80 |
| **49** | 실행 옵션(IFEO) 읽기 — 유저 쪽 (레지스트리 열고 읽고 닫기, `LoaderThreads` 덮기) | 유저 | 2.4 · ② | P1~P1a | `②… +0x536 LdrpInitializeExecutionOptions;  +0x8f7 LdrQueryImageFileExecutionOptions;  +0x1785·+0x179e·+0x17b4 NtClose` | run18·sast/13 | `[정적]` | **가** — `.data` `LdrpLargePageDllKeyHandle`·`LdrpImageExpansionMitigation` (끝까지 0)·매개변수 `LoaderThreads`. 레지스트리 핸들은 열고 닫음. (앞서 `LdrSystemDllInitBlock+0xa0` 의 완화 비트 둘을 **읽는다** — 쓴 쪽은 일 22) | Q44 |
| **50** | 전역 켜고 끄기 — 핫패치 끄기, `UseWOW64`, .NET 검사, 매개변수 정규화(`RtlNormalizeProcessParams`), LoadConfig 읽기, `LdrpDebugFlags` | 유저 | 2.4 · ② | P1~P1a | `②… +0x587·+0x5f2 · +0x62c LdrpCorFixupImage · +0x6c5 RtlNormalizeProcessParams · +0x6f4 LdrImageDirectoryEntryToLoadConfigEx` | run18·sast/13 | `[정적]` | **가** — `.data` (`UseWOW64`·`LdrpIsHotPatchingEnabled` 아는, `LdrpDebugFlags` 짐작). 정규화가 블록에 하는 일은 모름 | Q44·Q63(d) |
| **51** | PEB 의 TLS 비트맵 4칸 · FLS 자리 · 스택 추적 DB 옵션 · `FastPebLock` 만들기 | 유저 | 2.4 · ② | P1~P1a | `②… +0x88c·+0x8a3 RtlSetBits · +0x7c9 RtlInterlockedPushListSList · +0x97e RtlControlStackTraceDataBase · +0x9d2 RtlInitializeCriticalSectionEx` | run18 | `[정적]` | **가** — `_PEB` (TlsBitmap 계열·FastPebLock — 아는)·`.data FastPebLock` | Q44·Q74(c) |
| **52** | 프로세스 힙 만들기 (힙 관리자·힙 본체·태그 힙). 이후 일들이 이 힙에 블록을 놓는다 | 유저 (+ 커널 VAD) | 2.4 · ② | P1~P1a (힙 `25996f30000` `[관찰]`) | `②… +0x9f2 RtlInitializeHeapManager · +0xa50 LdrpInitializeProcessHeap(→ NtAllocateVirtualMemory) · +0xa39 RtlSetHeapInformation · +0xb7d RtlCreateTagHeap` | run18·run44 | `[정적: 호출 자리]`+`[관찰]` | **가·나** — 가: `_PEB` (ProcessHeap·NumberOfHeaps·ProcessHeaps)·`.data LdrpHeap`. 나: 힙 구조 자체 (`_HEAP` 헤더·세그먼트·LFH) | Q44 (힙 만들기 반환값 — 동적만)·새 할 일 (정적) |
| **53** | 힙 안 작은 로더 구조 — 로더 스냅 버퍼, 활성화 컨텍스트 스택(TEB 가 가리킴), 예외 로그, 시스템 정보 조회 | 유저 | 2.4 · ② | P1~P1a | `②… +0xace LdrpInitializeSnapsBuffer(RtlAllocateHeap) · +0xb05 RtlInitializeExceptionLog · +0xb12 RtlpInitializeThreadActivationContextStack · +0xb52 NtQuerySystemInformation` | run18; `_calls.tsv` 직접 | `[정적: 이름·호출]` | **가·나** — 가: `_TEB.ActivationContextStackPointer` (P1~P1a). 나: 힙 안 블록들 (스냅 버퍼·예외 로그·활성화 컨텍스트 스택 구조) | 새 할 일 |
| **54** | 스레드 풀 기본 준비와 ETW 등록 — 풀 패키지, 풀 CPU Set, ETW 등록 3곳, 힙 로깅 | 유저 (+ 커널 ETW 등록) | 2.4 · ② | P1~P1a | `②… +0xb88 TpInitializePackage · +0xba7 TpSetDefaultPoolCpuSets · +0xbe7 LdrpEnableUMGLTracingStateSync · +0xbf3 TraceLoggingRegisterEx… · +0xc0b·+0xc28 EtwEventRegister · +0xc2d RtlInitializeHeapLogging` | run18 | `[추측: 이름]` | **가·나** — 가: `.data` 전역 (REGHANDLE·풀 패키지 `[추측]`). 나: 힙 안 등록 블록 | 새 할 일 (Q78 의 `.data` 343개에 섞여 있음) |
| **55** | **환경 블록·매개변수 블록을 힙으로 옮기기** — 환경 복사(`EnvironmentVersion` +1·`Environment`·`EnvironmentSize`), 매개변수 블록 힙 새 블록으로 복사·포인터 9개 새 자리 기준으로·`PEB+0x20` 갈아끼움·옛 영역 `NtFreeVirtualMemory` | 유저 (+ 커널 VAD 삭제) | 2.4 · ② | P1~P1a | `②… +0xc32 RtlpInitEnvironmentBlock · +0xc3f RtlpInitParameterBlock (RtlAllocateHeap → RtlCopyMemory → 포인터 고침 → PEB+0x20 → NtFreeVirtualMemory(MEM_RELEASE))` | run44 `[관찰]`; **이번 바퀴 `RtlpInitParameterBlock.c`·`RtlpInitEnvironmentBlock.c` 직접 `[정적]`** | `[정적]`+`[관찰]` | **가·나** — 가: `_PEB.ProcessParameters`·`_RTL_USER_PROCESS_PARAMETERS` (포인터 칸·`Environment`·`EnvironmentSize`·`EnvironmentVersion`). 나: 힙 안 새 블록·환경 사본, 비워진 옛 영역 (VAD 사라짐 `[관찰]`) | Q78 (7.8 첫째 "누가 옮기나" — 이번에 닫음)·Q63(d) |
| **56** | `\KnownDlls` 열기·경로 읽기, 시스템 폴더·DLL 검색 목록 기본 문자열, DLL 정책 | 유저 (+ 커널 객체 열기) | 2.4 · ② | P1~P1a | `②… +0xdbd NtOpenDirectoryObject · +0xe4c NtOpenSymbolicLinkObject · +0xe95 NtQuerySymbolicLinkObject · +0xfca LdrpInitializePolicy · +0xfe9 LdrpIncludeAlternateForwarders · +0x1161 RtlpSystemDirs · +0x1183 LdrpCheckAppDirType` | run18·run39 | `[정적]`+`[관찰]` (핸들 `0x4c`) | **가·나** — 가: `.mrdata`·`.data` 전역 (`LdrpKnownDllDirectoryHandle` 짐작·`LdrpKnownDllPath`·`LdrpSystem32`·`RtlpSystem32Dirs`·`RtlpSystemDirs` — 아는). 나: 핸들 표 항목 (Directory `0x4c` `[관찰]`) | Q44·Q71(a)·Q80 |
| **57** | ntdll 모듈 항목 만들고 명부에 올리기 (명부·해시·색인·`State` 9·초기화 순서 목록 머리) + (핫패치면) 패치 항목 | 유저 | 2.4 · ② | P1~P1a | `②… +0x11a2 LdrpInitializeNtdllDataTableEntry → LdrpAllocateModuleEntry · LdrpInsertDataTableEntry · LdrpInsertModuleToIndex · LdrpProcessMappedModule;  +0x127c RtlInitializeHistoryTable` | run35·sast/30 | `[정적]` (항목 위치 힙 `[관찰]`) | **가** — `_LDR_DATA_TABLE_ENTRY`·`_LDR_DDAG_NODE`·`_PEB_LDR_DATA` (아는 다수) | Q44·Q45(끝)·Q67 |
| **58** | 병렬 로딩 지원 준비 (이벤트 2·락·큐·재시도 큐) · DLL 검색 경로 빈 구조 · 현재 폴더 | 유저 (+ 커널 이벤트) | 2.4·2.6 · ② | P1~P1a | `②… +0x12e3 LdrpInitParallelLoadingSupport · +0x1307 LdrpDrainWorkQueue · +0x1318 LdrpInitializeDllPath · +0x151c RtlpInitCurrentDir` | run19·run24·run39 | `[정적]` | **가·나** — 가: `.data` 전역 (아는 다수). 나: 이벤트 핸들 표 항목 2개 (`0x50`·`0x54` `[관찰]`), 현재 폴더 핸들 `[추측]` | Q44·Q46 |
| **59** | exe 모듈 항목 만들고 명부 맨 앞에 올리기 (`LdrpImageEntry`) + (헤더 `ImageBase` ≠ `PEB.ImageBaseAddress` 일 때만) exe 직접 재배치 | 유저 | 2.4 · ② | P1~P1a | `②… +0x135e LdrpAllocateModuleEntry → +0x1455 LdrpInsertDataTableEntry → +0x14d2 LdrpInsertModuleToIndex → +0x14ee LdrpProcessMappedModule;  (조건) +0x1435 LdrpProtectAndRelocateImage` | run19·run22·run35·run38 | `[정적]` | **가·나** — 가: 항목·노드 (아는 다수). 나 (조건): 재배치를 타면 test.exe 이미지 페이지 안 고쳐진 값 `[추측: 커널이 먼저 고쳐 놔서 안 탈 것]` | Q44·Q46·Q61·Q67(b) |
| **60** | 스케줄러 공유 데이터 슬롯 얻기 — 커널에서 슬롯 표를 받아 TEB `+0x1850` 에 걸기 | 유저 + 커널 (syscall) | 2.7 · ② | P1~P1a | `②… +0x182c LdrpAllocateSchedulerSharedData · +0x183c LdrpAcquireSchedulerSharedDataSlot → NtSetInformationThread(0x39) ⇒ 커널 PspSchedulerSharedDataRegionSlotAllocate` | run41·run44 | `[정적]`+`[관찰]` | **가·나** — 가: `_TEB.SchedulerSharedDataSlot`·`_KTHREAD.SchedulerSharedDataObject`. 나: 커널이 내준 영역 (옛 매개변수 자리에 2쪽 매핑 `[관찰]`, 슬롯 표 8칸) | Q74(c)(d)·Q78 |
| **61** | CFG 호출 검사 도우미 연결 — CFG 켜져 있으면 `.mrdata` 의 함수 포인터 둘에 `LdrpValidateUserCallTarget`·`LdrpDispatchUserCallTarget` 넣기 | 유저 | 2.4 · ② | P1~P1a | `②… +0x180b LdrpInitializeCfgScpHelpers (LdrProtectMrdata 열고 닫기)` | 이번 바퀴 `LdrpInitializeCfgScpHelpers.c` 직접 | `[정적]` | **가** — `.mrdata` `LdrpGuardCheckIcallNoESFptr`·`LdrpGuardDispatchIcallNoESFptr` (baggage 7.3: `LdrpGuardDispatchIcallNoESFptr` 는 `≤P1a`, `LdrpGuardCheckIcallNoESFptr` 는 `K1 이전` — 둘 다 쓴 자리 닫음) | 없음 (이번에 닫음) |
| **62** | exe TLS 준비 — TLS 디렉터리 읽기, 목록 항목, exe `TlsIndex`, TLS 벡터 | 유저 | 2.7 · ② | P1~P1a | `②… +0x18d0 LdrpInitializeTls → LdrpAllocateTlsEntry · LdrpAllocateTls` | run19·run28 | `[정적]` | **가·나** — 가: `.data LdrpTlsList`·exe 항목 `TlsIndex`·TEB `ThreadLocalStoragePointer`. 나: 힙 안 TLS 항목·벡터·초기 데이터 복사본 | Q16(끝)·Q25 |
| **63** | import 리다이렉션 준비 — 매개변수 `RedirectionDllName` 이 비어 있지 않을 때만 DLL 올리고 표 만듦 (sample 은 비어 있어 안 탐) | 유저 | 2.4 · ② | P1~P1a | `②… +0x1963 LdrpInitializeImportRedirection (조건: +0xd0 LdrpLoadDll · +0xf1 LdrpBuildImportRedirection)` | 이번 바퀴 `LdrpInitializeImportRedirection.c` 직접; run44 (`RedirectionDllName` 끝까지 0) | `[정적]`+`[관찰]` | **다** — 조건이 안 맞는다 (`+0x410` 문자열 길이 0 `[관찰]`) | 없음 (이번에 닫음) |
| **64** | ntdll 쪽 조건 갈래 묶음 — AppVerifier, WOW64, .NET, shim 엔진, 핫패치 데이터, 디버거 멈춤, 복제 프로세스 길. test.exe 는 안 탄 걸로 보임 | 유저 | 2.4 · ② | — | `②… +0x157c~+0x1718 (AppVerifier) · +0x17c6 LdrpLoadWow64 · +0x1852 LdrpCorInitialize · +0x19f4 LdrpInitShimEngine · +0x1b29 LdrpInitializePatchData · +0x1b40 LdrpDoDebuggerBreak;  _LdrpInitialize +0x354·+0x284` | run17~run19·run44 (갈림 값) | `[정적]` + 안 탐 `[관찰]` (`UseWOW64`·`AvrfAppVerifierMode`·`g_pfnSE_InstallAfterInit` 0) | **다** — 짐을 남기지 않거나 test.exe 가 안 타는 길 | Q46·Q48 |

### E. kernel32·kernelbase 준비 (P1a → P1b 앞부분)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 | 근거 | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|---|
| **65** | kernel32·kernelbase 올리기 — `Subsystem` 2/3 이면 ntdll 이 직접 `LdrLoadDll("KERNEL32.DLL")` (kernelbase 는 kernel32 import 로 따라 옴 `[추측]`), 이어서 `LdrLoadDll("KERNELBASE.dll")` | 유저 + 커널 (syscall) | 2.5 · ② | P1a~P1b | `②… +0x19a2 LdrpInitializeKernel32Functions +0xa4 LdrpFindLoadedDllInternal · +0xc0 LdrLoadDll → LdrpLoadDll +0x100 → LdrpLoadDllInternal (안쪽은 일 70~79) · +0x156 LdrLoadDll` | run19·run20·run21·sast/14~16 | `[관찰]` 호출 자리 / `[정적]` | **가·나** — 호출 틀이다. 남기는 짐은 안쪽 일 70~79 의 것 (모듈 항목·노드 등 가, 섹션·VAD·IAT 등 나) | Q20·Q49·Q10(끝)·Q11(끝) |
| **66** | kernel32·kernelbase 에서 이름으로 함수 주소 찾기·부르기 — `BaseThreadInitThunk`(→ `Kernel32ThreadInitThunkFunction`), `TermsrvGetWindowsDirectoryW`, `KernelbasePostInit`(간접 호출), `LdrpSnapKernelBaseExtensions`, `LdrpCodeAuthzInitialize` | 유저 | 2.5 · ② | P1a~P1b | `LdrpInitializeKernel32Functions +0xff·+0x130·+0x19b LdrGetProcedureAddressForCaller · +0x135 LdrpSnapKernelBaseExtensions · +0x165 LdrpCodeAuthzInitialize · +0x1af (간접) KernelbasePostInit` | run20·run25 | `[정적]`+`[관찰]` (`Kernel32ThreadInitThunkFunction` 값) | **가·나** — 가: `.data Kernel32ThreadInitThunkFunction` (= KERNEL32+`0x2cd70` `[관찰]`)·`LdrpAdvapi32DllHandle`·`LdrpSaferIsDllAllowedRoutine` (끝까지 0). 나: `KernelbasePostInit` 이 쓰는 KERNELBASE 전역 (긴 경로 설정 `[추측: 이름]`) | Q48·Q49·Q59 |
| **67** | KERNELBASE·KERNEL32 시작 함수가 하는 일 — csrss 연결(`CsrClientConnectToServer`), 콘솔 연결(`ConsoleInitialize`), NLS·메모리 관리자 준비, 태그 힙, 스레드 풀 시작 함수 등록, 보호 정책 | 유저 (+ 다른 프로세스 csrss 연결) | 2.5 · ② | P1a~P1b | `LdrpInitializeNode → LdrpCallInitRoutine → LdrpCallInitRoutineInternal → KERNELBASE!KernelBaseDllInitialize → _KernelBaseBaseDllInitialize +0x2d9 CsrClientConnectToServer · +0x526 ConsoleInitialize;  KERNEL32!BaseDllInitialize → _BaseDllInitialize` | run25 (이름·호출 자리만, 본문 안 읽음); run44 (구간) | `[정적: 이름]`+`[관찰]` (구간) | **가·나** — 가: PEB `ReadOnlySharedMemoryBase`·`ReadOnlyStaticServerData`·`CSDVersion`·`CsrServerReadOnlySharedMemoryBase`, ntdll `Csr*` 전역 8, 매개변수 `ConsoleHandle`, `_EPROCESS.OwnerProcessId` (쓴 주인 모두 `[추측]`, baggage 7.8). 나: KERNELBASE·KERNEL32 자기 전역, CSR 포트·콘솔 핸들 (`0x74`·`0x60`)과 공유 메모리 매핑, csrss 쪽 연결 기록 | Q21·Q22·Q78(7.8) — 안쪽은 새 할 일 |

### F. 병렬 로딩과 exe import (P1a → P1b)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 | 근거 | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|---|
| **68** | 병렬 로딩 켜기와 일꾼 스레드 둘 — 풀·일감 개체 만들기(`LoaderThreads` 0 → 스레드 4 → 최대 3), 일꾼 스레드가 커널에서 만들어져 `LdrInitializeThunk` 로 들어옴 | 유저 + 커널 | 2.4·3장 · ② | P1a~P1b | `②… +0x1a47 LdrpEnableParallelLoading → TpAllocPoolInternal · TpAllocWork (→ 커널 NtCreateThreadEx → PspCreateThread → PspAllocateThread … PspInsertThread;  일꾼: PspUserThreadStartup → PspInitializeThunkContext → LdrInitializeThunk → LdrpInitializeInternal → _LdrpInitialize +0xa7 LdrpInitializeThread → … → TppWorkerThread)` | run5·run19·run24·run44 | `[관찰]` (스레드 3개)+`[정적]` | **가·나** — 가: `.data LdrpThreadPool`·`LdrpMapAndSnapWork`·`LdrInitState`. 나: **일꾼 스레드 2개의 `_ETHREAD`·`_KTHREAD`·`_TEB`·유저/커널 스택·트랩 프레임·맡긴 `_CONTEXT`**, 풀 구조(힙) — 체크리스트는 메인 스레드 하나만 봤음 | Q47·Q79 |
| **69** | exe 의 import 를 올리고 연결하는 바깥 고리 — import 표 준비(IAT 쓰기 가능), 서술자마다 DLL 하나 맡기기, 다 끝나면 snap. 자식 DLL 은 같은 함수가 다시 불림 | 유저 | 2.6 · ② | P1a~P1b | `②… +0x1a8e LdrpThreadTokenSetMainThreadToken · +0x1aa9 LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal · LdrpSnapModule · LdrpQueueWork · +0x1ab1 LdrpDrainWorkQueue(1)` | run22·run23·run24 | `[정적]` | **다** — 호출 틀이다 (짐은 안쪽 일 70~79) | Q14(끝)·Q23 |

### G. DLL 하나를 올리는 공통 부품 (kernel32·kernelbase: `LdrLoadDll` 길 / dll1·dll2: import 길)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 | 근거 | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|---|
| **70** | 이름 다듬기·이미 있나 찾기 — ApiSet 풀기, 경로 전처리, 해시 표 조회, 빠른 끝 | 유저 | 2.5·2.6 · ② | P1a~P1b | `LdrpLoadDll +0xc2 LdrpPreprocessDllName;  LdrpLoadDllInternal +0xb8 LdrpFastpthReloadedDll · +0x1f2 LdrpFindOrPrepareLoadingModule;  LdrpLoadDependentModuleInternal +0x2ff ApiSetpResolveHost` | run20~run23 | `[정적]` | **다** — 조회다 (읽는 쪽: `PEB.ApiSetMap` 뷰, 일 19) | Q20·Q23 |
| **71** | DLL 파일 찾기 (파일 길) — 검색 경로 목록 짓기, 후보 파일 있나 확인 | 유저 + 커널 (파일 조회) | 2.6 · ② | P1a~P1b | `LdrpProcessWork +0x6a LdrpMapDllSearchPath +0x131 LdrpSearchPath → LdrpComputeLazyDllPath → RtlpComputePath;  LdrpResolveDllName → NtQueryAttributesFile` | run26·run39 | `[정적]` | **가·나** — 가: `.data RtlpDllSearchPath` (짐작, P1a~P1b `[관찰]`). 나: 힙 안 경로 목록 블록 (문자열)·`SafeDllSearchMode` 레지스트리 읽기 | Q53(끝)·Q71·Q23 |
| **72** | DLL 섹션 얻기 — `\KnownDlls` 섹션 열기, 또는 파일 열기 + `NtCreateSection(SEC_IMAGE)` | 유저 호출 → 커널 객체 | 2.5·2.6 · ② | P1a~P1b | KnownDll: `LdrpFindOrPrepareLoadingModule +0x5e5 LdrpLoadKnownDll → LdrpFindKnownDll +0xd9 NtOpenSection`;  파일: `LdrpMapDllSearchPath +0x25c LdrpMapDllNtFileName +0x137 NtOpenFile · +0x194 NtCreateSection · +0x2a0·+0x2a9 NtClose` | run21·run26 | `[정적]` (어느 길을 탔는지 `[공백]`) | **나** — dll1·dll2 (파일 길 `[추측]`) 의 섹션·컨트롤 영역·세그먼트·서브섹션·파일 객체 + 핸들 표 항목 (열고 닫음). kernel32·kernelbase 가 KnownDll 이면 전역 섹션을 열 뿐 새 컨트롤 영역 없음 `[추측]`. 체크리스트는 exe 것만 읽음 (sast/38) | Q20·Q23·Q62(f)·Q79 |
| **73** | DLL 메모리에 붙이기 (+ 맵 결과가 `0x40000003` 일 때만 재배치) | 유저 호출 → 커널 | 2.6 · ② | P1a~P1b | `LdrpMapDllWithSectionHandle +0x4e LdrpMinimalMapModule +0x174 NtMapViewOfSection (기준 주소 칸 = 항목 DllBase);  LdrpCompleteMapModule +0x31b LdrpRelocateImage` | run26·run38 | `[정적]` (붙은 주소는 `[관찰]`) | **가·나** — 가: `_LDR_DATA_TABLE_ENTRY.DllBase` (커널이 채움)·`_MMVAD`. 나: DLL 이미지 페이지 (재배치된 값·`.data` 초기값), 페이지 표 | Q20·Q23·Q52(끝)·Q62(f)·Q70 |
| **74** | 새 모듈 항목·그래프 노드 만들고 명부·해시·색인에 올리고 마무리 | 유저 | 2.6 · ② | P1a~P1b | `LdrpMapDllWithSectionHandle +0x146 LdrpInsertDataTableEntry · +0x151 LdrpInsertModuleToIndexLockHeld · +0x26c LdrpProcessMappedModule;  LdrpFindOrPrepareLoadingModule +0x47a / LdrpLoadDependentModuleInternal +0x115c LdrpAllocateModuleEntry` | run22·run26·run35 | `[정적]`+`[관찰]` | **가** — `_LDR_DATA_TABLE_ENTRY`·`_LDR_DDAG_NODE`·`_PEB_LDR_DATA`·색인 전역 (아는 다수). 모듈마다 역함수 표 등록 (`LdrpInvertedFunctionTables`) 은 모름 | Q72·Q54(끝)·Q80 |
| **75** | 의존 간선 쓰기 + 부모 카운터 (그래프 만들기) | 유저 | 2.6 · ② | P1a~P1b | `LdrpLoadDependentModuleInternal +0xf69 · +0xf8b (간선 칸 0x20B 를 부모 노드·자식 노드 두 목록에) · +0x12df 부모 카운터` | run23 | `[정적]` | **가·나** — 가: `_LDR_DDAG_NODE.Dependencies`·`IncomingDependencies` (아는). 나: 간선 칸 자체 (타입이 PDB 에 없음, 힙) | Q72(b) |
| **76** | 일감 넣기·꺼내기·일꾼 — 큐, 책임자, 부모 카운터 줄이기, 메인 스레드 토큰 가장 | 유저 (메인 + 일꾼) | 2.6 · ② | P1a~P1b | `LdrpLoadDependentModuleInternal +0x182c LdrpQueueWork;  LdrpDrainWorkQueue → LdrpProcessWork;  LdrpSignalModuleMapped;  (일꾼) TppWorkpExecuteCallback → LdrpWorkCallback` | run24 | `[정적]` (어느 스레드인지 `[공백]`) | **가·나** — 가: `.data` 일감 전역 (아는 다수)·TEB `LoadOwner`. 나: 로드 문맥 (힙, 타입이 PDB 에 없음) | Q47 |
| **77** | IAT 연결(snap) — import 서술자마다 상대 DLL export 에서 주소 찾아 IAT 칸에 쓰기, forwarder, 끝맺음(IAT 보호 되돌리기) | 유저 (+ 커널 보호 변경) | 2.6 · ② | P1a~P1b | `LdrpProcessWork +0x131 / LdrpMapAndSnapDependency +0x4c2 LdrpSnapModule +0x3e7 (IAT 칸 쓰기) · +0x5df (forwarder) · +0xb66 LdrpDoPostSnapWork` | run36·sast/31 | `[정적+샘플]` | **가·나** — 가: `_LDR_DDAG_NODE.State` (아는). **나: 이미지 페이지 안 IAT 칸** — test.exe 91 (`dll1` 1 + KERNEL32 90), dll1 89, dll2 88 `[정적+샘플]`; kernel32 서술자 104·import 약 1,274, kernelbase 4·약 750 `[정적+샘플: 근사]` | Q50(끝)·Q68·Q69 |
| **78** | DLL 의 TLS 목록 항목 만들기 | 유저 | 2.7 · ② | P1a~P1b | `LdrpDoPostSnapWork +0x93 LdrpHandleTlsData → LdrpAllocateTlsEntry · LdrpAllocateTls` | run28·run36 | `[정적]` | **가·나** — 가: `.data LdrpTlsList`·항목 `TlsIndex`. 나: 힙 안 TLS 항목·벡터 | Q16(끝)·Q68 |
| **79** | 그래프 정리·실행 준비 — 고리 합치기, 알림, 로드 문맥·경로 해제 | 유저 | 2.7 · ② | P1a~P1b | `LdrpPrepareModuleForExecution +0x12f LdrpCondenseGraph → LdrpCondenseGraphRecurse → LdrpMergeNodes · +0x7b LdrpNotifyLoadOfGraph;  ②… +0x1be7 LdrpFreeLoadContextOfNode · +0x1c00 LdrpReleaseDllPath` | run21·run24·run27 | `[정적]` | **가** — `_LDR_DDAG_NODE` (LowestLink·State 6·7 — 아는). 풀어 준 로드 문맥은 힙 블록 free | Q15(끝)·Q24 |

### H. 켜기와 은퇴 (P1b → P2)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 | 근거 | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|---|
| **80** | 켜기 직전 준비 — 사용자 Windows 폴더(검색 목록 다시 씀), `Kernel32ThreadInitThunkFunction(1,0,0)` 간접 호출, 로더 락, 첫 스레드 문맥 주소 걸기 | 유저 | 2.4 · ② | P1a~P1b | `②… +0x1b83 LdrpProcessInitContextRecord · +0x1b5e (간접) Kernel32ThreadInitThunkFunction · +0x1b77 LdrpInitializePerUserWindowsDirectory · +0x1b96 LdrpAcquireLoaderLock` | run19·run39 | `[정적]` | **가** — `.data` (`LdrpProcessInitContextRecord`·`RtlpSystemDirs` — 아는). `BaseThreadInitThunk(1,0,0)` 이 KERNEL32 전역에 쓰는지는 모름 | Q46·Q48·Q59·Q71(a) |
| **81** | 그래프 켜기 — 자식 먼저, 모듈마다 `State` 8 → 9, 초기화 순서 목록 꼬리에 끼우기 | 유저 | 2.7 · ② | P1b~P2 | `②… +0x1bbb LdrpInitializeGraphRecurse → +0x6a LdrpInitializeNode` | run27 | `[관찰]` 호출 자리 / `[정적]` | **가** — 초기화 순서 목록·`ProcessAttachCalled`·노드 `State`·`LdrpCurrentDllInitializer` (아는 다수) | Q22·Q24·Q54(끝) |
| **82** | TLS 콜백 부르기 — `LdrpCallTlsInitializers` → `ImageTlsCallbackCaller` → `Dll2Tls`·`Dll1Tls` (락 소유 칸 표 쓰기 포함) | 유저 (사용자 코드 호출) | 2.7·2.8 · ② | P1b~P2 | `LdrpInitializeNode +0x16b LdrpCallTlsInitializers → ImageTlsCallbackCaller → Dll2Tls / Dll1Tls` | run28·run41 | `[관찰]` 호출 자리 / `[정적]` | **가** — `.data LdrpTlsLock`·TEB 락 소유 칸 (썼다 되돌림 → 끝까지 0). 콜백 안에서 사용자 코드가 남기는 짐은 범위 밖 | Q25·Q26·Q56(끝)·Q74 |
| **83** | 진입점 부르기 — `LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain` (kernel32 준비 때는 KERNELBASE·KERNEL32 시작 함수 → 일 67) | 유저 (사용자 코드 호출) | 2.7·2.8 · ② | P1b~P2 | `LdrpInitializeNode +0x19c LdrpCallInitRoutine → +0x93 LdrpCallInitRoutineInternal → (dllN) _DllMainCRTStartup ⇒ dllmain_dispatch +0x91 → DllMain` | run27·run28·run40 | `[관찰]` 호출 자리 / `[정적]` | **가** — 항목 `ProcessAttachCalled` (아는). `DllMain` 이 남기는 짐은 사용자 코드 (범위 밖) | Q21·Q27·Q55(끝)·Q73 |
| **84** | exe TLS 콜백 부르기 (활성화 컨텍스트 켜고 끄기 포함) | 유저 (사용자 코드 호출) | 2.9 · ② | P1b~P2 | `②… +0x1c8a RtlActivateActivationContextUnsafeFast · +0x1c99 LdrpCallTlsInitializers · +0x1ca7 RtlDeactivateActivationContextUnsafeFast` | run19·run28 | `[관찰]` 호출 자리 / `[정적]` | **다** — 일 82 와 같은 부품. 콜백이 남기는 짐은 사용자 코드 | Q28 |
| **85** | 로더 끝 잡일과 `_LdrpInitialize` 마무리 — shim 후처리(간접), `PostProcessInitRoutine`(간접), SMT 지연 잠 설정, 핸들 닫기, 완료 신호·`LdrInitState` 3·`ProcessInitializing` 끔 | 유저 | 2.4·2.10 · ② | P1b~P2 | `②… +0x1ce4 (간접) g_pfnSE_InstallAfterInit · +0x1d1b (간접) PEB.PostProcessInitRoutine · +0x1c2a LdrpInitializeSmtDelayedSleep · +0x1d2d·+0x1d3c NtClose;  _LdrpInitialize +0x1a1 LdrpTouchThreadStack · +0x252 LdrpInitializationComplete · +0xbe NtTestAlert` | run17·run19·run44 | `[정적]`+`[관찰]` (값) | **가** — `LdrInitState`·`LdrpProcessInitialized`·`LdrpHotPatchInitialized`·PEB `ProcessInitializing` (아는) + SMT 지연 잠 전역 (레지스트리 읽기, 모름) | Q48 |
| **86** | 은퇴 — 맡긴 문맥으로 `NtContinue`: 유저 APC 확인, 트랩 프레임 덮기, `IRETQ` → `RtlUserThreadStart` | 유저 → 커널 → 유저 | 2.10 · ② 끝 | P2 (입구) 이후 — 사진 없음 | `LdrInitializeThunk +0x18 NtContinue(SYSCALL 0x43) → nt!NtContinue ⇒ NtContinueEx → KiContinueEx +0xc9 KeTestAlertThread → KiContinuePreviousModeUser +0x422 KeContextToKframes → IRETQ → RtlUserThreadStart` | run29·run42·run44 (P2 입구) | `[관찰]` 입구 / `[정적]` 안쪽 | **가** — `_KTRAP_FRAME` (② 끝 4칸)·`_KTHREAD.ApcState`. 범위 경계: 이 뒤 `RtlUserThreadStart` → `BaseThreadInitThunk` → 진입점은 경계 (Q30·Q31) | Q29·Q57·Q75 (경계 뒤: Q30·Q31·Q60) |

### 1.1 요청서 예시 → 일 번호 (빠진 예시가 없나 확인용)

| 요청서가 든 예 | 일 번호 |
|---|---|
| exe 이미지 섹션 만들기 | 4 |
| 주소 공간 만들기 | 11 15 |
| ntdll·시스템 DLL 매핑 | 16 17 |
| PEB 만들기 | 18 19 |
| 매개변수 복사·고치기 | 20 21 |
| 시스템 DLL 시작 정보 블록 | 22 |
| 스레드 객체·ID·TEB·유저 스택 | 25 26 27 28 |
| 출발 문맥 만들기·바꿔 끼우기 | 24 42 |
| 프로세스·스레드 넣기 (핸들 표·목록) | 29 30 33 |
| 알림 콜백 | 32 |
| 부모의 csrss 알림 | 38 |
| 스레드 풀기 (멈춰 두기 포함) | 31 39 |
| 로더 쪽 힙 만들기 | 52 |
| ntdll 항목 | 57 |
| 실행 옵션 | 9 49 |
| kernel32·kernelbase 올리기·초기화 | 65 66 67 |
| import DLL 찾기·올리기 (경로 검색·섹션·매핑) | 69 70 71 72 73 |
| 재배치 (4: 섹션 만들 때 진입점 델타, 16: 진입점 보정, 59: exe 직접(조건), 73: DLL(조건)) | 4 16 59 73 |
| IAT 연결 (snap) | 77 |
| 의존 그래프 | 75 79 81 |
| 일꾼 스레드 | 68 76 |
| TLS 준비·콜백 | 62 78 82 84 |
| DllMain 부르기 | 83 |
| 매개변수 블록 힙으로 옮기기 (run44) | 55 |
| NtContinue | 86 |

- 예시에 없었는데 근거 문서에서 더한 일: 파일 열기 3, 서명 검사 5, 토큰 7, 세션·쿼터 10, 완화 정책 13, NLS 47, 핫패치 45, CFG 도우미 61, 스케줄러 슬롯 60, 스레드 풀·ETW 54, `\KnownDlls` 56, 병렬 로딩 지원 58, 조건 갈래 35·64, 부모 사이 검사 36, 부모 앱 호환 데이터 37.

- 위 표에서 **범위 밖**으로 둔 것: 종료(`LdrShutdownProcess`)·복제(`RtlCloneUserProcess`·`LdrpCompleteProcessCloning` …)·DLL 폴더 API(`LdrSetDllDirectory`) 길 — baggage.md 3장이 단계 `—` 로 이미 가려 둔 줄들이다. 진입점 뒤(`RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup` → CRT → `main`) 도 경계라 일로 안 셌다 (Q30·Q31·Q60).
- 일 86 (`test!Worker` 스레드) 같은 `main` 뒤 일은 범위 밖이다. 단 일꾼 스레드 **둘**은 `P1a~P1b` 에 생기는 초기화의 일부라 일 68 에 넣었다.

## 2. 분류 요약과 "Q78 이 볼 것" 길잡이 (질문 2)

| 분류 | 일 수 | 일 번호 |
|---|---|---|
| 가 만 | 24 | 7 9 13 18 27 31 39 40 41 44 48 49 50 51 57 61 74 79 80 81 82 83 85 86 |
| 가·나 | 45 | 3 4 8 10 11 12 14 15 16 17 19 20 21 22 23 25 26 28 29 30 34 37 42 45 46 47 52 53 54 55 56 58 59 60 62 65 66 67 68 71 73 75 76 77 78 |
| 나 만 | 3 | 33 38 72 |
| 다 | 14 | 1 2 5 6 24 32 35 36 43 63 64 69 70 84 |

### 2.1 (가) 의 칸을 쓰는 일 — 구조체·전역별 (Q78 이 "일 단위" 로 갈 때의 길잡이)

Q78 은 "칸 → 쓴 자리" 로 가지만, 같은 구조체를 여러 일이 나눠 쓴다. 아래 표는 **구조체마다 그 칸에 쓰는 일**을 모은 것이다. `[정적]`/`[추측]` 은 3장 표의 태그와 같다. 괄호 숫자는 직접 칸 수 (sast/38).

| 구조체·구역 | 쓰는 일 (번호) |
|---|---|
| `_EPROCESS` (263) | 7 8 9 10 13 14 16 23 29 30 39 41 67 |
| `_KPROCESS` (65) | 10 11 12 |
| `_ETHREAD` (149) | 25 30 |
| `_KTHREAD` (237) | 28 30 31 34 39 40 42 60 86 |
| `_KTRAP_FRAME` (66) | 28 42 86 |
| `_HANDLE_TABLE` (17) — 핸들을 새로 만드는 일(일 45·46·56·58·67 등)도 `HandleCount` 류 칸을 바꾼다 `[추측]` | 12 21 29 |
| `_MMVAD`·`_MMVAD_SHORT` (23) | 15 16 17 19 26 37 47 52 55 60 73 |
| `_TOKEN` (55) | 7 |
| `_SECTION`·`_CONTROL_AREA`·`_SEGMENT`·`_SECTION_IMAGE_INFORMATION` (63) — DLL 쪽 같은 타입은 일 72 (체크리스트 밖) | 4 |
| `_FILE_OBJECT` (30) — 일 23 는 `ImageFilePointer` 후보 | 3 23 |
| `_PEB` (124) | 18 19 20 37 46 47 51 52 55 67 85 |
| `_RTL_USER_PROCESS_PARAMETERS` (40) | 20 21 49 55 67 |
| `_TEB` (137) | 27 41 46 53 60 62 76 82 |
| `_PEB_LDR_DATA` (9) | 47 57 59 74 81 |
| `_LDR_DATA_TABLE_ENTRY` (65) | 57 59 73 74 78 81 83 |
| `_LDR_DDAG_NODE` (10) | 57 59 74 75 77 79 81 |
| `_KUSER_SHARED_DATA` (103) — 쓰는 일 없음 (읽기만. 일 15 가 VAD 만 만듦). 값은 부팅 때 정해진 것 `[추측]` | — |
| ntdll `.data`·`.mrdata`·`.00cfg` (493) — 일 22 는 `.mrdata` 의 `LdrSystemDllInitBlock` | 22 44 45 46 48 49 50 51 52 54 56 57 58 61 62 66 68 71 76 78 80 81 82 85 |

- 가장 많은 일이 걸리는 곳: ntdll 전역 (24개 일), `_EPROCESS` (13개), `_PEB` (11개), `_MMVAD` 계열 (11개), `_KTHREAD` (9개). `_TOKEN` (일 7) 과 `_SECTION` 계열 (일 4) 은 일이 하나라 Q78 이 한 번에 끝낼 수 있고, `_KUSER_SHARED_DATA` 는 쓰는 일이 없다.
- 칸 이름이 "모름" 인 곳이 많은 구조체(`_EPROCESS` 241 · `_KTHREAD` 227 · `_TEB` 133 · ntdll 전역 391)는 일 번호가 곧 **읽을 함수 묶음**이다. 예: `_EPROCESS` 모르는 칸은 일 8~14 의 `PspAllocateProcess` 안 callee (이름만 아는 것 40개 — 5장 표) 를 읽으면 대부분 닿는다.

## 3. (나) 체크리스트 밖에 남는 짐 (질문 2 (나))

(나) 를 포함한 일은 48개다. 아래는 **일이 아니라 "남는 곳"** 으로 묶었다 (같은 곳에 여러 일이 쓰므로). 각 줄: 어디에 남나 · 관련 일 · **기존 덤프(dast/03 의 8개, `ms/dump/03_baggage-snap/<P>/`) 로 읽힐지** `[추측]` · PDB 로 칸 표를 만들 수 있나 `[정적]` · 제안.

| 번호 | 어디에 남나 | 관련 일 | 덤프로 읽힐지 `[추측]` | 칸 표 (PDB) | 제안 |
|---|---|---|---|---|---|
| **C1** | **커널 이웃 객체** (체크리스트에 칸 표가 없는 타입): `_SUBSECTION`·`_MSUBSECTION`·`_SECTION_OBJECT_POINTERS`·`_OBJECT_HEADER`·`_HANDLE_TABLE_ENTRY`·`_MI_SECTION_IMAGE_INFORMATION`·`_KEXCEPTION_FRAME`·`_MMSUPPORT_FULL` | 3 4 8 12 21 15 28 29 30 | 읽힘 `[추측]`: 전부 커널 풀이라 K1~P2 어느 덤프에도 있다. `_CONTROL_AREA.Segment`·`Subsection` 을 따라가면 닿는다. | **있음** (이번에 PDB 타입 레코드로 확인 `[정적]`): nt PDB 에 정의가 있다 (`_OBJECT_HEADER` 0x38, `_SUBSECTION` 0x38, `_MSUBSECTION` 0x98, `_SECTION_OBJECT_POINTERS` 0x18, `_HANDLE_TABLE_ENTRY` 0x10, `_MI_SECTION_IMAGE_INFORMATION` 0x50, `_KEXCEPTION_FRAME` 0x140, `_MMSUPPORT_FULL` 0x140). run43 의 표 만들기를 그대로 쓸 수 있다. | **B** 체크리스트 확장 (새 항목) → 그 표로 **A** 읽기 |
| **C2** | **PDB 에 정의가 없는 타입**: `_LDRP_LOAD_CONTEXT`·`_EPROCESS_QUOTA_BLOCK` (정방향 선언만), 의존 간선 칸 0x20B, `InitialTeb`, `LdrSystemDllInitBlock` | 10 75 76 26 22 | 읽힘 `[추측]`: 메모리엔 있으나 칸 이름을 모른다. 오프셋으로만 적을 수 있다. | **없음** (이번에 확인 `[정적]`: 두 PDB 모두 정방향 선언만) | **Q78 안에서** 코드에서 오프셋을 읽어 적는다 (이미 run23·run32 가 그렇게 함) |
| **C3** | **커널 전역 목록·표 안의 항목**: `PspCidTable` 의 PID·TID 항목, `PsActiveProcessHead` 와 이웃 프로세스의 Flink, CPU 준비 큐(KPRCB), 쿼터 블록·세션 참조 수 | 23 25 29 34 10 | 읽힘 `[추측]`: `nt!PspCidTable`·`nt!PsActiveProcessHead` 는 전역 이름으로 닿는다. K1 / K2 / K3 비교로 "언제 들어갔나" 가 가려진다 (예: K2 의 `ActiveProcessLinks` = `[0 - 0]` 은 이미 관찰). | 있음 (`_HANDLE_TABLE`·`_HANDLE_TABLE_ENTRY`·`_KPRCB` 0xcf00 모두 nt PDB 에 정의) | **A** 덤프 보충 읽기 (Q79 에 붙임) |
| **C4** | **페이지 표·PFN** (주소 공간 아래층): 최상위 페이지 표(PML4), 이미지가 붙을 때 이어지는 프로토 PTE | 11 15 16 17 73 | 읽힘 `[추측]`: DirBase `192c2e000` 를 물리 주소로 읽을 수 있다 (dast/02 `!vtop`·물리 주소 읽기 `[관찰]`). 값이 많고 하는 일이 "페이지를 연결" 이라 칸 단위 의미가 약하다. | 있음 (`_MMPTE` 8B·`_MMPFN` 0x30) | **사용자 결정**: 에뮬레이터가 페이지 표를 직접 흉내 낼지에 달렸다. 지금은 A 의 맨 뒤 (낮음) |
| **C5** | **이미지 페이지 안 값**: test.exe·dll1·dll2·kernel32·kernelbase 의 IAT 칸 (test.exe 91 · dll1 89 · dll2 88 · kernel32 약 1,274 · kernelbase 약 750), 다른 주소에 붙어서 고쳐진 값(재배치), `.data` 처음 값 | 16 17 59 73 77 | 읽힘 `[추측]`: P1b·P2 에선 IAT 페이지가 이미 쓰여서(복사본 페이지) 메모리에 있다. 그 전 지점은 파일 매핑 상태라 `????` 일 수 있다 (ntdll `.data` 가 K1~P1 에서 읽기 불가였던 것과 같은 모양 `[관찰]`). **기대값은 PE 파일로 정적 계산**된다: 값 = 대상 DLL 베이스 + export RVA (sample PE 는 `sample/bin`, 시스템 DLL 은 `ms/bin`). | 해당 없음 (칸 이름이 아니라 import 표 순서) | **C** 새 정적 (기대값 계산) + **A** 읽기로 대조 |
| **C6** | **다른 모듈의 섹션·컨트롤 영역·파일 객체·VAD 노드** (dll1·dll2·kernel32·kernelbase). 체크리스트는 exe 것만 읽음 | 72 73 15 | 읽힘 `[추측]`: P1b·P2 의 `!vad` 목록은 있다 (VAD 32개 `[관찰]`). 노드 `dt` 에서 `Subsection → ControlArea → FilePointer` 로 따라가면 닿는다. Q79 가 "VAD 노드마다 `dt`" 를 이미 담고 있다. | 있음 (`_MMVAD`·`_CONTROL_AREA`·`_SEGMENT`·`_FILE_OBJECT` 표 이미 있음) | **A** Q79 를 "모듈 5개의 VAD → 컨트롤 영역 → 파일" 까지로 넓힘 |
| **C7** | **프로세스 힙 안 블록**: 힙 구조 자체(`_HEAP`), 새 매개변수·환경 블록, 로더 스냅 버퍼, 활성화 컨텍스트 스택, 경로 목록 캐시, TLS 항목·벡터, 간선 칸, 로드 문맥, 스레드 풀 구조, ETW 등록 블록 | 52 53 54 55 62 71 75 76 78 | 읽힘 `[추측]`: P1a~P2 에 힙 영역 `25996f30000` 부터 있다 (프로세스 힙 `[관찰]`). 어떤 블록이 누구 것인지는 힙 걷기(`!heap`)가 필요한데 **오프라인 덤프에서 되는지는 안 해 봤다**. | 일부 있음 (ntdll PDB 에 `_HEAP` 0x2c8·`_HEAP_ENTRY`·`_HEAP_SEGMENT`·`_HEAP_LOCAL_DATA`·`_ACTIVATION_CONTEXT_STACK` 0x28·`_TP_POOL` 0x1d8 정의 있음 `[정적]`) | **D** 작은 오프라인 시험 (`!heap` 되나) → 되면 **A**, 안 되면 **C** (함수별로 "어떤 크기 블록을 놓나") |
| **C8** | **칸이 아닌 유저 구역**: 맡긴 `_CONTEXT`(0x4d0), 유저 스택(메인 + 일꾼 2), 읽기 전용 뷰(leap second·API set 지도), NLS 매핑 2개, 스케줄러 공유 데이터 2쪽, shim 데이터·활성화 컨텍스트 데이터 VAD 2개, 환경 블록 구역 | 42 26 19 20 47 60 37 68 | 읽힘 `[추측]`: 주소는 이미 알고 있다 (`_CONTEXT` `9b2fcffa40`, API set `25996cd0000`, shim `25996d10000` …). 맡긴 `_CONTEXT` 는 P1·P2 에서 읽었다 `[관찰]`. 나머지는 내용을 안 읽었다. leap second 내용은 8장 모두 읽기 불가 `[관찰]`. | 있음 (`_CONTEXT` 0x4d0 — 표 안 만듦; `_NLS_DATA_BLOCK`·`_CPTABLEINFO` 있음) | **A** 덤프 보충 읽기 (주소 있는 것부터) |
| **C9** | **다른 모듈의 전역**: KERNELBASE `.data` (공개 데이터 이름 **3,125**, 구역 0xad18)·KERNEL32 `.data` (**197**, 0x16b8) — 시작 함수와 `KernelbasePostInit` 이 쓴다 | 67 66 | 읽힘 `[추측]`: P1b·P2 에서 두 DLL 의 `.data` 구역은 복사본 페이지로 메모리에 있다. P1a 에선 아직 없다(두 DLL 이 P1a 뒤에 올라옴) → "처음 값 구간" 은 `P1a~P1b` 하나로만 나온다. | 이름 목록은 PDB 공개 이름으로 가능 (ntdll 전역 때처럼 `gen_globals` 방식). 크기는 "다음 이름까지 거리" 짐작 `[추측]` | **C** 새 정적 (KERNELBASE·KERNEL32 전역 목록 + 시작 함수 본문) → **A** 읽기 |
| **C10** | **일꾼 스레드 2개의 객체 일체**: `_ETHREAD`·`_KTHREAD`·`_TEB`·`_KTRAP_FRAME`·유저/커널 스택·맡긴 `_CONTEXT`·TLS 벡터 (메인 스레드와 같은 타입) | 68 | 읽힘: P1b·P2 에서 `ActiveThreads` 3 `[관찰]` — 스레드 목록에서 나머지 둘의 주소를 얻는다. 칸 표는 **메인 스레드용을 그대로 재사용**한다 (같은 PDB 타입). | 있음 (이미 표 있음) | **A** 덤프 보충 읽기 (가장 싸고 효과 큼 — 일꾼 TEB 의 `SameTebFlags` 로 `LoaderWorker`·`SkipLoaderInit` 도 확인) |
| **C11** | **핸들 표 항목과 그 뒤의 객체**: 이벤트 3 (`0xc`·`0x50`·`0x54`), `\KnownDlls` 디렉터리(`0x4c`), 콘솔 `\Reference`→`\Connect` (`4`→`0x60`), CSR ALPC 포트(`0x74`), 현재 폴더 등 (핸들 4 → 26 → 44 → 46) | 12 21 45 46 56 58 67 47 | 읽힘: `!handle` 목록은 8장 모두 있다 `[관찰]` (dast/04). 핸들이 **생긴 구간** 은 K1~P2 목록 비교로 가려진다. 핸들 22개 (P1→P1a) · 18개 (P1a→P1b) · 2개 (P1b→P2) 가 어느 일 것인지는 이름·종류로 짝지을 수 있다 `[추측]`. | 있음 (`_HANDLE_TABLE_ENTRY`) | **A** 덤프 보충 읽기 — 핸들 번호별 "생긴 구간·종류·이름" 표 (일 번호 붙이기) |
| **C12** | **다른 프로세스**: 부모 cmd.exe (핸들 표 항목 2·출력 칸), csrss (CSR 프로세스·스레드 기록, csrss 핸들 2, 연결 기록) | 33 37 38 67 | 부모: 읽힘 `[추측]` — 부모 EPROCESS `ffff828598add080` 를 이미 안다 `[관찰]`. csrss: `.process /r /p <csrss>` 로 문맥을 바꿔야 한다 `[추측]` (덤프는 물리 메모리 전체라 csrss 유저 페이지도 있을 수 있다. 페이지아웃된 건 못 읽음). K3 vs P0 비교. | csrsrv·basesrv PDB 있음 (`ms/sym`) — 공개 데이터 이름 csrsrv `.data` 42 · basesrv 55 | 부모: **A**. csrss: **사용자 결정** (요청서가 "이름·한 줄까지만" 으로 정함) — 넓히면 새 항목 |

읽는 법: **A** = 기존 덤프로 새 명령만 더 치면 된다 (Q79 에 붙임) · **B** = 칸 표를 먼저 만든다 (정적) · **C** = 코드부터 읽어 목록을 만든다 (정적) · **D** = 작은 시험·새 사진이 필요하다 (동적).

### 3.1 PDB 타입 정의가 있나 (이번에 확인 `[정적]`)

`ms/ghidra/_tmp_sast38/nt_types.txt`·`ntdll_types.txt` (run43 이 만든 `llvm-pdbutil dump -types` 결과) 에서 정방향 선언이 아닌 정의 레코드를 찾았다.

- **정의 있음 (칸 표 가능)**: nt PDB — `_OBJECT_HEADER` 0x38 · `_SUBSECTION` 0x38 · `_MSUBSECTION` 0x98 · `_SECTION_OBJECT_POINTERS` 0x18 · `_HANDLE_TABLE_ENTRY` 0x10 · `_MI_SECTION_IMAGE_INFORMATION` 0x50 · `_KEXCEPTION_FRAME` 0x140 · `_MMSUPPORT_FULL` 0x140 · `_MMPTE` 8 · `_MMPFN` 0x30 · `_OBJECT_DIRECTORY` 0x158 · `_ETW_REG_ENTRY` 0x78 · `_NLS_DATA_BLOCK` 0x18 · `_CONTEXT` 0x4d0 · `_PS_NTDLL_EXPORTS` 0xa8. ntdll PDB — `_HEAP` 0x2c8 · `_HEAP_ENTRY` 0x10 · `_HEAP_SEGMENT` 0x70 · `_HEAP_LOCAL_DATA` 0x30 · `_ACTIVATION_CONTEXT_STACK` 0x28 · `_RTL_ACTIVATION_CONTEXT_STACK_FRAME` 0x18 · `_TP_POOL` 0x1d8 · `_CPTABLEINFO` 0x40 · `_ACTIVATION_CONTEXT_DATA` 0x20 · `_CURDIR`.
- **정의 없음 (정방향 선언만)**: `_LDRP_LOAD_CONTEXT`, `_EPROCESS_QUOTA_BLOCK` (run43 의 "PDB 에 없는 타입" 목록 `InitialTeb`·간선 칸·`LdrSystemDllInitBlock` 에 이 둘이 더해진다).

### 3.2 제안 (정하는 건 사용자)

- **제안 1 — Q79 (덤프 보충 읽기) 에 붙임. 새 정적 작업 없이 바로 가능한 것.** C3 (`PspCidTable`·`PsActiveProcessHead`), C6 (VAD → 컨트롤 영역 → 파일 5모듈), C8 (주소 아는 구역 내용), C10 (일꾼 스레드 둘), C11 (`!handle` 구간 표), C12 부모 쪽, 그리고 이미 담긴 `PsDefaultLoaderThreads`. 덤프 8개는 그대로고 디버거 VM 에서 오프라인 명령만 더 치면 된다.
- **제안 2 — 새 항목 "체크리스트 확장" (정적, 작음~중간).** C1 의 8개 이웃 타입 + 필요하면 `_HEAP` 계열·`_ACTIVATION_CONTEXT_STACK`·`_TP_POOL`·`_CONTEXT` 의 칸 표를 run43 방식으로 만든다. 이번 바퀴에서 이 타입들이 PDB 에 **정의가 있음**을 확인했으므로 표 만들기는 가능하다. Q78 안에 넣을 수도 있지만 Q78 은 "쓴 자리 찾기" 라 성격이 달라서 따로 두는 쪽을 권한다.
- **제안 3 — 새 항목 "KERNELBASE·KERNEL32 시작 함수 + 전역" (정적, 중간).** C9. `_KernelBaseBaseDllInitialize`·`_BaseDllInitialize` 안 `CsrClientConnectToServer`·`ConsoleInitialize` 등 본문(run25 는 이름만), 두 DLL 의 `.data` 전역 목록. baggage.md 7.8 의 `Csr*` 8개·PEB CSR 칸 4개·`ConsoleHandle`·`OwnerProcessId` 의 "쓴 주인" 이 이 일에서 나온다. Q78 의 "구간 안 함수만" 은 nt·ntdll 로 한정돼서 이 함수들은 **Q78 로는 안 닿는다**.
- **제안 4 — 새 항목 "IAT 기대값 계산" (정적, 작음) → A 와 짝.** C5. PE 파일 둘만 있으면 계산되고, P1b·P2 에서 읽은 값과 맞으면 "snap 이 쓴 값" 을 칸 이름 없이도 확인한다.
- **제안 5 — 작은 오프라인 시험 "`!heap` 이 되나".** C7. 한 번 해 보고 되면 힙 안 블록(일 52·53·54·55·62)은 A 로, 안 되면 제안 6 으로.
- **제안 6 — 힙 안 블록 정적 목록 (정적, 중간).** 일 52·53·54 은 아직 callee 본문을 거의 안 읽었다. 함수 10개 안팎.
- **제안 7 — 사용자 결정: csrss 쪽 기록 (C12).** 요청서가 이름까지만으로 정했으므로 넓히려면 사용자가 정한다. 넓히면 csrsrv·basesrv 쪽 정적 + 덤프(K3 vs P0 csrss 문맥) 한 항목.
- **Q78 범위에 대하여.** (나) 는 대부분 Q78 의 방식(칸 → 쓴 자리)으로는 안 보인다. 칸 표가 없거나(C1·C9), 다른 프로세스·다른 모듈이거나(C9·C12), 구역이라 칸이 아니거나(C7·C8) 하기 때문이다. 그래서 Q78 을 넓히기보다 위 제안 1~4 를 **옆 항목**으로 두고, Q78 은 "체크리스트 21종 + ntdll 전역" 에 그대로 두는 쪽을 권한다. **정하는 건 사용자.**


## 4. 기존 대기 항목과 잇기, 새 할 일 (질문 3)

### 4.1 대기 항목 → 걸린 일

일마다 걸린 대기 항목은 1장 표 마지막 열에 있다. 아래는 거꾸로 **plan.md 대기열에서 상태가 `대기`/`진행 중` 인 항목** 마다 어느 일에 걸리는지다.

| 항목 | 내용 (줄임) | 걸린 일 |
|---|---|---|
| Q19 | 2.2 첫 스레드가 실제로 풀리는 시점 (부모 스레드와의 순서) | 1 31 34 36 38 39 40 |
| Q20 | 2.5 kernel32·kernelbase가 메모리에 올라오는 순간 (`NtMapViewOfSection` 계열 + `k`) | 19 65 70 72 73 |
| Q21 | 2.5 `LdrpCallInitRoutine` → KERNELBASE·KERNEL32 시작 함수 실제 진입 + 콜스택 | 67 83 |
| Q22 | 2.5 `LdrpInitializeNode` [KERNELBASE]/[KERNEL32] 대응 (긴 `k`) | 67 81 |
| Q23 | 2.6 dll1·dll2가 메모리에 올라오는 순간 | 69 70 71 72 73 |
| Q24 | 2.7 `LdrpInitializeGraphRecurse` 재귀 단계 ↔ 모듈 대응 (`k 8` 이상) | 79 81 |
| Q25 | 2.7 `LdrpCallInitRoutine` → `ImageTlsCallbackCaller` (`k`), 그 안쪽 | 62 82 |
| Q26 | 2.8 `ImageTlsCallbackCaller` → `Dll2Tls`/`Dll1Tls` (`k`) | 82 |
| Q27 | 2.8 `LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain` (`k`) | 83 |
| Q28 | 2.9 `ImageTlsCallbackCaller` → `test!ExeTls` (`k`) | 84 |
| Q29 | 2.10 `LdrInitializeThunk` → `NtContinue` → `RtlUserThreadStart` (`k`) | 42 86 |
| Q30 | 2.10 `RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup` (… | 86 |
| Q31 | 2.10 `mainCRTStartup` → `LoadLibraryExW` ×3 → `main` (`k`) **(2026-10-… | 86 |
| Q34 | 2.1 `NtCreateUserProcess` 실행 길: 간접 호출 2곳(`+0x5b2`, `+0x11bb`) 대상, `Psp… | 2 3 4 5 24 |
| Q35 | 2.1 `PspAllocateProcess` 실행 길: `MmInitializeProcessAddressSpace` `+0x2… | 8 13 15 16 17 18 20 22 35 |
| Q38 | 2.1 `PspAllocateThread`·`PspInsertProcess` 실행 길: 입구 11번째 인자(`poi(@rsp+… | 24 25 26 27 28 29 35 |
| Q39 | 2.1 `PspInsertThread`·`PspCallProcessNotifyRoutines` 실행 길: 입구 flags(`p… | 30 31 32 35 |
| Q42 | 2.3 커널 → 유저 전환 실제 길: `KiKvaShadow`·`KiIrqlFlags` 값(유저로 가는 `IRETQ` 가 `K… | 28 34 40 41 42 43 |
| Q43 | 2.4 로더 입구 실행 값 (주소로 멈춤 — 이름 겹침): `ln ntdll+0x8c0f8`·`x ntdll!*LdrpInit… | 44 45 46 |
| Q44 | 2.4 `LdrpInitializeProcess` 앞쪽 실행 값 (주소로 멈춤, `ntdll+0x8c404+오프셋`, test… | 48 49 50 51 52 56 57 58 59 |
| Q46 | 2.4 `LdrpInitializeProcess` 뒤쪽 실행 값 (주소로 멈춤, `ntdll+0x8c404+오프셋`, test… | 58 59 64 80 |
| Q47 | 2.4·3장 ntdll 일꾼 스레드가 처음 생기는 시점: `LdrpInitializeProcess +0x1a42 LdrpEna… | 68 76 |
| Q48 | 2.4·2.9 `LdrpInitializeProcess` 뒤쪽 간접 호출 대상: `+0x1b59` `Kernel32Thread… | 64 66 80 85 |
| Q49 | 2.5 `LdrpInitializeKernel32Functions` 실행 값 (`ntdll+rva`, test.exe 만): … | 65 66 |
| Q57 | 2.10 `NtContinue` 길의 CET·유저 APC 실제 값: 스레드 `+0x74` bit20 `CetUserShadow… | 42 86 |
| Q59 | 2.10 kernel32 `BasepInitializeTermsrvFpns` 안쪽 (`BaseThreadInitThunk` 첫… | 66 80 |
| Q60 | 2.10 `KERNEL32!LoadLibraryExW` → `KERNELBASE!LoadLibraryExW` 연결 (작음): … | 86 |
| Q61 | 2.1 이미지 섹션 만들기 실제 길 + `TransferAddress` 값: `nt+0x96f4d0` `MiCreateNewS… | 4 16 59 |
| Q62 | 2.1 이미지 섹션 길 작은 정적 묶음 (대표 지점 목표엔 덜 중요): (a) 안 읽은 함수에도 `TransferAddress… | 4 16 72 73 |
| Q63 | 2.1 매개변수·시작 정보 블록 작은 정적 묶음 (대표 지점 목표엔 덜 중요): (a) `PspCaptureUserProces… | 6 13 20 21 22 50 55 |
| Q64 | 2.2 `PspProcessClose` (프로세스 객체 CloseProcedure, `ProcessFirstResume` 를 … | 39 |
| Q66 | 2.2 부모 쪽 작은 정적 둘: `0x7ffe0308` 칸 이름 (`_KUSER_SHARED_DATA.SystemCall` `… | 1 38 |
| Q67 | 2.4 ntdll 항목 주변 작은 정적 셋 (대표 지점 목표엔 덜 중요): (a) `RtlInsertInvertedFuncti… | 45 48 57 59 |
| Q68 | 2.5 `LdrpSnapModule` 실행 값 (test.exe 만, Q23·Q47 과 같은 실행이면 묶기): IAT 쓰기 `… | 77 78 |
| Q69 | 2.5 snap 실패 길 작은 정적 (대표 지점 목표엔 덜 중요): 노드 `State = -3`(`LdrModulesSnapE… | 77 |
| Q70 | 2.6 매핑 갈래 작은 정적 (대표 지점 목표엔 덜 중요): 로드 문맥 `ctx+0x20` 비트 `0x800`·`0x80000… | 73 |
| Q71 | 2.4·2.6 검색 경로 주변 작은 정적 (대표 지점 목표엔 덜 중요): (a) `LdrpInitializePerUserWin… | 22 56 71 80 |
| Q72 | 2.5·2.7 Q54 남은 전수 확인 (작음, 대표 지점 목표엔 덜 중요): (a) ntdll 안에서 항목 `Flags(+0x… | 74 75 |
| Q73 | 2.8 sample dll CRT 남은 것 (작음, CRT 한 상자 원칙이라 대표 지점 목표엔 덜 중요): (a) `FlsGe… | 83 |
| Q74 | 2.7 TLS 락 소유 칸 표 남은 것 (작음, 대표 지점 목표엔 덜 중요): (a) 칸 bit 0 을 켜는 곳, (b) `R… | 51 60 82 |
| Q75 | 2.10 `NtContinue` 길 CET 남은 정적 (작음, 대표 지점 목표엔 덜 중요): (a) 시동 문맥(`0x10005… | 42 86 |
| Q78 | **짐 지도 C — 쓴 자리·줄기** (정적): Q76·Q77 의 칸마다 쓴 자리(직접 / 주소 넘김 / 붙이기 / sysca… | (칸 → 쓴 자리: 2.1 의 구조체 색인) |
| Q79 | 덤프 보충 읽기 (오프라인, VM 은 디버거만): VAD 노드마다 `dt` (`_MMVAD_SHORT`/`_MMVAD`, 지금… | 15 16 20 68 72 73 + (나) 제안 1 |
| Q80 | ntdll 전역 `≤P1a` 좁히기 (정적): K1~P1 덤프에선 test.exe 의 사본 페이지가 없어 읽기 불가였던 칸 —… | 17 44 48 56 74 |
| Q81 | **일 목록** (정적 정리, flow.md·run 문서 바탕): 초기화 동안 (프로세스 생성 → `NtContinue`) n… | (이 항목) |

- 일에 안 걸린 대기 항목: 없음.
- Q30·Q31·Q60 은 "진입점 뒤(경계)" 항목이라 일 86 의 뒤에 둔다 (일로 안 센다).

### 4.2 구체적인 대기 항목이 없는 일 (Q78·Q79·Q80 만 걸리거나 아무것도 안 걸림)

| 일 | 내용 | 걸린 것 | 분류 |
|---|---|---|---|
| 7 | 새 프로세스 토큰 정하기·붙이기 | Q78 | 가 |
| 9 | 실행 옵션(IFEO) 읽기 | Q78 | 가 |
| 10 | 세션·쿼터·프로세서 그룹(NUMA)·친화도·CPU Set·우선순위·quantum 정하기 | Q78 | 가·나 |
| 11 | 주소 공간 껍데기 | Q78 | 가·나 |
| 12 | 커널 프로세스 구조와 핸들 표 만들기 (`KeInitializeProcess`, `ObInitProcess` | Q78 | 가·나 |
| 14 | 전체 이미지 이름 만들기 · 생성 알림 (`SmProcessCreateNotification`) · 세션 리… | Q78 | 가·나 |
| 23 | 프로세스 ID 얻기(`PspCidTable`)·프로세스 락 잡기·이미지 파일 강한 참조·`KeSecurePr… | Q78 | 가·나 |
| 33 | 부모용 핸들 만들기·출력 칸 쓰기·정리 | 없음 | 나 |
| 37 | 부모가 앱 호환 데이터를 자식 메모리에 써 넣기 (자식 PEB `pShimData` 후보 `[추측]`) | Q78 | 가·나 |
| 47 | `PebLdr` 빈 목록 3개·`PEB.Ldr`·난수·NLS(코드 페이지) 정보 | 없음 | 가·나 |
| 53 | 힙 안 작은 로더 구조 | 없음 | 가·나 |
| 54 | 스레드 풀 기본 준비와 ETW 등록 | Q78 | 가·나 |
| 61 | CFG 호출 검사 도우미 연결 | 없음 | 가 |
| 63 | import 리다이렉션 준비 | 없음 | 다 |

### 4.3 새 할 일 (어느 대기 항목에도 안 걸리는 일)

두 종류를 모았다. (a) 위 4.2 중에서 Q78 의 방식으로도 안 닿는 것. Q78 은 "체크리스트 21종 + ntdll 전역" 의 칸을 **nt·ntdll 안**에서 찾는 항목이다. (b) 대기 항목이 걸려 있어도 그 항목이 **실행 확인(동적)** 뿐이라 안쪽 본문·읽기를 아무도 안 맡은 것 (표의 1·3·4·7·8 번 등).

| # | 새 할 일 | 관련 일 | 종류 | 크기 짐작 |
|---|---|---|---|---|
| 1 | KERNELBASE·KERNEL32 시작 함수 본문 (`_KernelBaseBaseDllInitialize`·`_BaseDllInitialize` 안 `CsrClientConnectToServer`·`ConsoleInitialize`·`BaseNlsDllInitialize` …) + 두 DLL 의 `.data` 전역 목록 (공개 이름 3,125 / 197) | 67 66 | 정적 (kernelbase·kernel32 는 Ghidra 프로젝트 이미 있음) | 중간 |
| 2 | 부모 KERNELBASE 가 자식 PEB·메모리에 쓰는 것 (`BasepInitAppCompatData`·`BasepGetAppCompatData`·`NtWriteVirtualMemory` ×3) — baggage 7.8 "다른 DLL 첫 후보" | 37 | 정적 (kernelbase) + 동적 한 번 (아래 동적 1) | 작음 |
| 3 | csrss 쪽 기록 (csrsrv `CsrInsertProcess`·`CSR_PROCESS` 구조, basesrv `BaseSrvCreateProcess2`) — 범위를 넓힐지 사용자 결정 | 38 67 | 정적 (csrsrv·basesrv 프로젝트 이미 있음) + 덤프 | 중간 (결정 먼저) |
| 4 | 프로세스 힙 만들기와 힙 안 작은 구조들 (`RtlInitializeHeapManager`·`LdrpInitializeProcessHeap`·`LdrpInitializeSnapsBuffer`·`RtlpInitializeThreadActivationContextStack`·`RtlInitializeExceptionLog`) + 스레드 풀 패키지·ETW 등록 3곳 | 52 53 54 | 정적 (ntdll) | 함수 10개 안팎 |
| 5 | NLS 길의 커널 쪽 (`nt!NtGetNlsSectionPtr` 가 섹션을 프로세스에 붙이는 길) — VAD `C_949.NLS`·`l_intl.nls` 의 주인 | 47 | 정적 (ntoskrnl) | 작음 |
| 6 | 부모용 핸들·출력 칸 (`PspCreateObjectHandle`·`PspUpdateCreateInfo` 가 부모 쪽에 쓰는 것) | 33 | 정적 (ntoskrnl) + 덤프 (제안 1) | 작음 |
| 7 | 일꾼 스레드 2개의 객체 읽기 (칸 표 재사용) | 68 | 덤프 (Q79 에 붙임) | 작음 |
| 8 | 다른 DLL 의 섹션·컨트롤 영역·파일 객체 사슬 읽기 | 72 73 | 덤프 (Q79 확장) | 작음 |
| 9 | 체크리스트 확장 — 이웃 타입 칸 표 (3.1 의 정의 있음 목록) | 4 12 21 28 29 42 52 | 정적 (PDB, run43 방식) | 작음~중간 |
| 10 | IAT 기대값 계산 (PE 만으로) | 77 16 | 정적 (스크립트) | 작음 |
| 11 | 핸들 번호별 "생긴 구간·종류·이름" 표 | 12 21 45 46 56 58 67 47 | 덤프 (`!handle` 8장 비교) | 작음 |

- 이번 바퀴에 **이미 닫은 것** (새 할 일에서 뺌): 일 61, 일 63, 일 55 의 "누가 옮기나", 일 46 의 전역 3칸, 일 47 의 PEB 코드 페이지 칸 2개 (6장).

## 5. 빠짐 점검 — sast/38 callee 표 639줄 → 일 번호 (질문 4)

표: `ms/disassemble/{ntoskrnl,ntdll}/_baggage/callees.tsv` (15 함수, 데이터 637줄). 각 줄을 **호출한 함수 안의 자리(ret 오프셋)와 callee 이름**으로 일에 넣었다. 줄마다의 결과는 `callees_by_work.tsv` (같은 폴더, 열: 원래 열 + `work_no` `work_key` `class`).

### 5.1 함수별 요약

| 함수 | 줄 | 일에 든 줄 | 공용 부품 | 실패·정리 길 | 조각 점프 | 간접 호출 | 걸린 일 번호 |
|---|---|---|---|---|---|---|---|
| `NtCreateUserProcess` | 83 | 54 | 19 | 10 | 0 | 2 | 2 3 4 5 6 7 8 24 25 29 30 33 |
| `PspAllocateProcess` | 144 | 115 | 26 | 3 | 0 | 2 | 7 8 9 10 11 12 13 14 15 16 18 19 20 22 23 33 35 |
| `MmCreatePeb` | 19 | 9 | 9 | 0 | 1 | 0 | 18 19 35 |
| `PspSetupUserProcessAddressSpace` | 20 | 15 | 5 | 0 | 0 | 0 | 18 19 20 21 22 35 |
| `PspAllocateThread` | 49 | 23 | 11 | 10 | 5 | 0 | 25 26 27 28 35 |
| `MmCreateTeb` | 5 | 1 | 4 | 0 | 0 | 0 | 27 |
| `PspInsertProcess` | 21 | 14 | 5 | 2 | 0 | 0 | 29 |
| `PspInsertThread` | 62 | 24 | 17 | 18 | 3 | 0 | 30 31 32 33 34 35 |
| `PspUserThreadStartup` | 9 | 9 | 0 | 0 | 0 | 0 | 35 40 41 42 |
| `PspInitializeThunkContext` | 20 | 15 | 5 | 0 | 0 | 1 | 32 42 |
| `LdrInitializeThunk` | 3 | 3 | 0 | 0 | 0 | 0 | 44 86 |
| `LdrpInitialize` | 4 | 4 | 0 | 0 | 0 | 0 | 44 |
| `LdrpInitializeInternal` | 6 | 6 | 0 | 0 | 0 | 0 | 45 46 68 |
| `_LdrpInitialize` | 23 | 15 | 3 | 5 | 0 | 1 | 46 64 68 85 |
| `LdrpInitializeProcess` | 169 | 126 | 42 | 1 | 0 | 4 | 44 47 48 49 50 51 52 53 54 55 56 57 58 59 60 61 62 63 64 65 68 69 76 79 80 81 84 85 |
| **합계** | **637** | **433** | **146** | **49** | **9** | **10** | |

- 줄 수는 `callees_summary.tsv` 의 `rows` 와 15개 함수 모두 같다 (assert 로 확인). 조각 점프 9줄은 "자기 조각(`FUN_…`)으로 가는 JMP" 라 호출이 아니다 (sast/38 의 세는 법).
- 간접 호출 10줄 (nt 5 · ntdll 5) 도 일에 넣었다 (대상은 동적으로만): `NtCreateUserProcess +0x5b7`→일 2 · `+0x11c0`→일 33, `PspAllocateProcess +0x121a`·`+0x17c4`→일 35, `PspInitializeThunkContext +0x360`→일 32, `_LdrpInitialize +0x9c`→일 68, `LdrpInitializeProcess +0x17fe`→일 64 · `+0x1b5e`→일 80 · `+0x1ce4`→일 85 · `+0x1d1b`→일 85.

### 5.2 일별 줄 수 — 표에 한 줄도 안 나오는 일

| 번호 | 줄 | 번호 | 줄 | 번호 | 줄 | 번호 | 줄 |
|---|---|---|---|---|---|---|---|
| 1 | 0 | 2 | 13 | 3 | 7 | 4 | 1 |
| 5 | 9 | 6 | 2 | 7 | 22 | 8 | 12 |
| 9 | 16 | 10 | 25 | 11 | 3 | 12 | 2 |
| 13 | 13 | 14 | 3 | 15 | 3 | 16 | 1 |
| 17 | 0 | 18 | 13 | 19 | 5 | 20 | 9 |
| 21 | 1 | 22 | 6 | 23 | 7 | 24 | 5 |
| 25 | 13 | 26 | 3 | 27 | 2 | 28 | 1 |
| 29 | 15 | 30 | 12 | 31 | 1 | 32 | 7 |
| 33 | 8 | 34 | 1 | 35 | 20 | 36 | 0 |
| 37 | 0 | 38 | 0 | 39 | 0 | 40 | 1 |
| 41 | 4 | 42 | 13 | 43 | 0 | 44 | 8 |
| 45 | 4 | 46 | 4 | 47 | 2 | 48 | 10 |
| 49 | 5 | 50 | 3 | 51 | 5 | 52 | 4 |
| 53 | 5 | 54 | 7 | 55 | 2 | 56 | 12 |
| 57 | 3 | 58 | 4 | 59 | 6 | 60 | 2 |
| 61 | 1 | 62 | 1 | 63 | 1 | 64 | 22 |
| 65 | 1 | 66 | 0 | 67 | 0 | 68 | 10 |
| 69 | 1 | 70 | 0 | 71 | 0 | 72 | 0 |
| 73 | 0 | 74 | 0 | 75 | 0 | 76 | 8 |
| 77 | 0 | 78 | 0 | 79 | 3 | 80 | 4 |
| 81 | 1 | 82 | 0 | 83 | 0 | 84 | 4 |
| 85 | 9 | 86 | 2 |  |  |  |  |

- **줄이 0 인 일 19개**: 1 17 36 37 38 39 43 66 67 70 71 72 73 74 75 77 78 82 83. 15개 함수의 **1단계 callee 에 안 나오는 것**이다. 이유별로: 부모 쪽·csrss (1 36 37 38 39), 커널 두 단계 아래 (17), 첫 스레드의 커널→유저 전환 (43), kernel32 준비 안쪽 (66 67), DLL 올리기 공통 부품 (70 71 72 73 74 75 77 78), TLS 콜백·진입점 부르기 (82 83). 이 일들은 **callee 표가 아니라 문서(flow.md)에서** 찾았다. 그래서 callee 표만 믿고 일을 세면 이만큼 빠진다.

### 5.3 `doc=none` 57개 (문서 어디에도 없던 callee) → 일

`callees_summary.tsv` 의 `doc_none` (nt 43 + ntdll 14). 역할은 **이름으로 본 짐작 `[추측: 이름]`** 이다 (이번에 본문을 읽은 것은 표시). 일 열이 "공용" 이면 일에 안 넣은 공용 부품.

| 함수 | callee | ret | 일 | 짐작 역할 |
|---|---|---|---|---|
| `PspAllocateProcess` | `PoEnergyContextInitialize` | +0x75c | 8 | 에너지 추정 문맥 초기화 |
| `PspAllocateProcess` | `PsReferencePartitionSafe` | +0x835 | 8 | 메모리 파티션 참조 잡기 |
| `PspAllocateProcess` | `ExAcquireRundownProtection` | +0x8dd | 8 | 런다운 보호 잡기 (객체가 치워지는 중이면 못 잡게) |
| `PspAllocateProcess` | `ExReleaseRundownProtection` | +0x906 | 8 | 런다운 보호 풀기 |
| `PspAllocateProcess` | `ZwOpenKey` | +0xc42,+0xe40 | 9 | IFEO 레지스트리 키 열기 |
| `PspAllocateProcess` | `PspReadIFEONodeOptions` | +0xcd2 | 9 | IFEO 의 NUMA 노드 옵션 읽기 |
| `PspAllocateProcess` | `PspReadIFEOPerfOptions` | +0xe59 | 9 | IFEO 의 성능 옵션 읽기 |
| `PspAllocateProcess` | `PspIsSessionLeaderProcess` | +0x10eb,+0x202e | 7 (+0x10eb) / 14 (+0x202e) | 세션 리더인지 판정 (권한·이름 판단에 쓰임) |
| `PspAllocateProcess` | `Feature_OutsideInProtection__private_IsEnabledDeviceUsageNoInline` | +0x11eb,+0x1e3d,+0x261a | 공용 | 기능 스위치(WIL 플래그) 검사 |
| `PspAllocateProcess` | `SeFreeAntiTamperPolicy` | +0x1201 | 7 | 변조 방지 정책 해제 (정리) |
| `PspAllocateProcess` | `KeSelectGroupFromNode` | +0x12c8,+0x132b | 10 | 노드에서 프로세서 그룹 고르기 |
| `PspAllocateProcess` | `KeQueryActiveGroupCount` | +0x12e1 | 10 | 활성 프로세서 그룹 수 조회 |
| `PspAllocateProcess` | `KeAddProcessorAffinityEx` | +0x131c | 10 | 친화도 집합에 프로세서 더하기 |
| `PspAllocateProcess` | `KeSelectNodeForAffinity` | +0x13a2 | 10 | 친화도에 맞는 NUMA 노드 고르기 |
| `PspAllocateProcess` | `KeQueryAffinityProcess` | +0x13cc | 10 | 프로세스 친화도 조회 |
| `PspAllocateProcess` | `SeIsTokenAssignableToProcess` | +0x147f | 7 | 이 토큰을 이 프로세스에 줄 수 있나 검사 |
| `PspAllocateProcess` | `SeQuerySessionIdToken` | +0x14b9 | 7 | 토큰의 세션 번호 조회 |
| `PspAllocateProcess` | `PsGetSessionSchedulingGroupByProcess` | +0x16e7 | 10 | 세션의 스케줄링 그룹 조회 |
| `PspAllocateProcess` | `Feature_DecoupleAppContainerAndIntegrityLevel__private_IsEnabledDeviceUsageNoInline` | +0x186f | 공용 | 기능 스위치 검사 |
| `PspAllocateProcess` | `MmInitializeHandBuiltProcess2` | +0x19b1 | 11 | "손으로 만든" 프로세스 주소 공간 마무리 (보통 길인지 모름) |
| `PspAllocateProcess` | `PspApplyIFEOPerfOptions` | +0x19e0 | 9 | IFEO 성능 옵션 적용 |
| `PspAllocateProcess` | `KeSetCpuSetsProcess` | +0x1a05 | 10 | CPU Set 지정 |
| `PspAllocateProcess` | `KeSetExpectedConcurrencyCountProcess` | +0x1a1e | 10 | 예상 동시 실행 수 지정 |
| `PspAllocateProcess` | `PspSetProcessPriorityClass` | +0x1a40 | 10 | 우선순위 클래스 지정 |
| `PspAllocateProcess` | `PspComputeQuantumAndPriority` | +0x1a64 | 10 | quantum·우선순위 계산 |
| `PspAllocateProcess` | `PspReadIFEOMitigationOptions` | +0x1a90 | 13 | IFEO 완화 옵션 읽기 |
| `PspAllocateProcess` | `PspInheritMitigationOptions` | +0x1aef,+0x1be5 | 13 | 부모 완화 옵션 물려받기 |
| `PspAllocateProcess` | `PspReadIFEOMitigationAuditOptions` | +0x1aff | 13 | IFEO 완화 감사 옵션 읽기 |
| `PspAllocateProcess` | `PspInheritMitigationAuditOptions` | +0x1b5e,+0x1c56 | 13 | 부모 완화 감사 옵션 물려받기 |
| `PspAllocateProcess` | `PspHardenMitigationOptions` | +0x1c87 | 13 | 완화 옵션 강화 |
| `PspAllocateProcess` | `KeIsUserCetAllowed` | +0x1c8c | 13 | 유저 CET 허용 여부 |
| `PspAllocateProcess` | `KeIsCoreIsolationMitigationPolicyEnforceable` | +0x1e22 | 13 | 코어 격리 완화 정책 적용 가능 여부 |
| `PspAllocateProcess` | `PspReadIFEOAntiTamperOptions` | +0x1e6d | 13 | IFEO 변조 방지 옵션 읽기 |
| `PspAllocateProcess` | `AppModelPolicy_GetPolicy_Internal` | +0x1fb8 | 13 | 앱 모델 정책 조회 |
| `PspAllocateProcess` | `KeInitializeProcessUserCetLogging` | +0x200d | 13 | 유저 CET 로깅 초기화 |
| `PspAllocateProcess` | `SeConstructAntiTamperPolicy` | +0x264f | 7 | 변조 방지 정책 만들기 |
| `PspAllocateProcess` | `SeSetTokenTamperProtection` | +0x26b1 | 7 | 토큰에 변조 방지 설정 |
| `PspAllocateProcess` | `ObFastDereferenceObject` | +0x26cc,+0x26d6 | 공용 | 빠른 참조 해제 (공용) |
| `PspAllocateProcess` | `MmGetSectionStrongImageReference` | +0x274a | 23 | 이미지 파일 강한 참조 얻기 (`ImageFilePointer` 쪽 `[추측]`) |
| `PspAllocateProcess` | `KiQueryUnbiasedInterruptTime` | +0x2804 | 23 | 시간 읽기 (생성 시각 계열) |
| `PspSetupUserProcessAddressSpace` | `PspCalculateUserAttributeListSize` | +0x135 | 20 | 유저 속성 목록 크기 계산 (새 프로세스 안 자리 크기) |
| `MmCreateTeb` | `MiAllocateFromSubAllocatedRegion` | +0xac | 27 | TEB 자리를 잘게 나눈 구역에서 떼어 주기 |
| `PspInitializeThunkContext` | `PspGetBaseTrapFrame` | +0x3a0,+0x3ad | 42 | 기본 트랩 프레임 찾기 (오류 길) |
| `LdrpInitializeProcess` | `RtlpInitializeNonVolatileFlush` | +0x292 | 48 | 비휘발 메모리 flush 준비 |
| `LdrpInitializeProcess` | `LdrpCaptureCriticalThunks` | +0x486 | 48 | 중요 thunk 바이트 기록 (detour 감지 — `LdrpDetourExist`) |
| `LdrpInitializeProcess` | `RtlSetHeapInformation` | +0xa39 | 52 | 힙 옵션 지정 (프로세스 힙) |
| `LdrpInitializeProcess` | `Feature_Servicing_DisallowSharedLockImplicitUpgrade__private_IsEnabledNoReportingNoInline` | +0xab2 | 공용 | 기능 스위치 검사 |
| `LdrpInitializeProcess` | `Feature_Servicing_LoaderSnapsBuffer__private_IsEnabledDeviceUsageNoInline` | +0xac5 | 공용 | 기능 스위치 검사 |
| `LdrpInitializeProcess` | `LdrpInitializeSnapsBuffer` | +0xace | 53 | 로더 스냅 로그 버퍼 (힙 블록 하나, 본문 `_calls.tsv` 로 확인) |
| `LdrpInitializeProcess` | `RtlpInitializeThreadActivationContextStack` | +0xb12 | 53 | TEB 활성화 컨텍스트 스택 포인터 걸기 (호출 없는 짧은 함수) |
| `LdrpInitializeProcess` | `TpSetDefaultPoolCpuSets` | +0xba7 | 54 | 스레드 풀 기본 CPU Set |
| `LdrpInitializeProcess` | `EtwEventRegister` | +0xc0b,+0xc28 | 54 | ETW 등록 |
| `LdrpInitializeProcess` | `RtlInitializeHeapLogging` | +0xc2d | 54 | 힙 로깅 초기화 |
| `LdrpInitializeProcess` | `RtlInitializeHistoryTable` | +0x127c | 57 | 예외 풀기 기록 표 초기화 |
| `LdrpInitializeProcess` | `LdrpInitializeCfgScpHelpers` | +0x180b | 61 | CFG 호출 검사 도우미 연결 (**이번에 본문 읽음**) |
| `LdrpInitializeProcess` | `SbObtainTraceHandle` | +0x19cd | 64 | shim·샌드박스 추적(ETW 알림) 핸들 얻기 |
| `LdrpInitializeProcess` | `LdrpInitializeSmtDelayedSleep` | +0x1c2a | 85 | SMT 지연 잠 설정 (레지스트리 읽기, 본문 `_calls.tsv` 로 확인) |

- 서로 다른 (함수, callee) 쌍 **57개** — `callees_summary.tsv` 의 `doc_none` 합 57과 같다. 이 중 **일을 새로 세우게 한 것**: `LdrpInitializeCfgScpHelpers`(일 61), `LdrpInitializeSnapsBuffer`·`RtlpInitializeThreadActivationContextStack`(일 53), `EtwEventRegister`·`RtlInitializeHeapLogging`(일 54), `PspSelectNodeForProcess` 주변 그룹 계산(일 10). 커널의 `PspReadIFEO*`·`PspInherit*Mitigation*` 이름은 새로 나왔지만 일(일 9·13)은 run10 의 묶음에 이미 있던 것이다. 나머지도 이미 있던 일에 들어갔다.

### 5.4 어느 일에도 안 넣은 줄 — 공용 부품과 실패·정리 길

**전부 "새 일" 이 아니라 공용 부품 또는 정리 길**이다 (새 일 후보 0). 공용 부품 146줄 / 정리·실패 길 49줄 / 조각 점프 9줄.

**공용 부품** — 이름 · 줄 수 · 하는 일 (이름으로 본 것 `[추측: 이름]`). baggage.md 6장 "공용 부품" 이 "아직 없음" 이므로 이 목록이 그 후보 첫 판이다.

| callee | 줄 | 어느 함수에서 | 역할 |
|---|---|---|---|
| `memset` | 25 | LdrpInitializeProcess×7, NtCreateUserProcess×5, PspAllocateProcess×5, PspAllocateThread×3 외 3곳 | 버퍼 0 채우기 |
| `LdrpLogInternal` | 10 | LdrpInitializeProcess×8, _LdrpInitialize×2 | 로더 디버그 로그 |
| `KiUnstackDetachProcess` | 9 | MmCreatePeb×3, PspAllocateProcess×2, MmCreateTeb×2, PspSetupUserProcessAddressSpace 외 1곳 | 붙은 주소 공간에서 떼기 |
| `__security_check_cookie` | 8 | NtCreateUserProcess, PspAllocateProcess, MmCreatePeb, PspSetupUserProcessAddressSpace 외 4곳 | 스택 쿠키 확인(함수 끝) |
| `ObfDereferenceObject` | 7 | NtCreateUserProcess×3, PspInsertThread×2, PspAllocateProcess, PspInsertProcess | 객체 참조 내림 |
| `KiStackAttachProcess` | 7 | PspAllocateProcess×2, PspAllocateThread×2, MmCreatePeb, PspSetupUserProcessAddressSpace 외 1곳 | 다른 프로세스 주소 공간에 붙기 |
| `RtlAppendUnicodeStringToString` | 7 | LdrpInitializeProcess×7 | 문자열 이어 붙이기 |
| `KeAbPostRelease` | 6 | PspInsertThread×6 | 락 풀고 정리 |
| `ExAllocatePool2` | 5 | PspAllocateProcess×3, PspAllocateThread, PspInsertThread | 풀 블록 할당 |
| `SeDeleteAccessState` | 4 | PspInsertThread×2, NtCreateUserProcess, PspInsertProcess | 접근 상태 정리 |
| `RtlAllocateHeap` | 4 | LdrpInitializeProcess×4 | 힙 할당 |
| `RtlAppendUnicodeToString` | 4 | LdrpInitializeProcess×4 | 문자열 이어 붙이기 |
| `ExRaiseDatatypeMisalignment` | 3 | NtCreateUserProcess, MmCreatePeb, PspAllocateThread | 정렬 오류 예외(오류 길) |
| `ObfReferenceObjectWithTag` | 3 | NtCreateUserProcess, PspAllocateProcess, PspInsertProcess | 객체 참조 올림(태그) |
| `ObfDereferenceObjectWithTag` | 3 | NtCreateUserProcess, PspAllocateProcess, PspInsertProcess | 객체 참조 내림(태그) |
| `KeAbPreAcquire` | 3 | PspInsertThread×2, PspAllocateThread | 락 잡기 준비(주소 공간 추적) |
| `ExfAcquirePushLockExclusiveEx` | 3 | PspInsertThread×2, PspAllocateThread | 푸시락 독점으로 잡기 |
| `__chkstk` | 2 | NtCreateUserProcess, PspInitializeThunkContext | 스택 확장 검사 |
| `ObfReferenceObject` | 2 | PspAllocateProcess×2 | 객체 참조 올림 |
| `ObFastDereferenceObject` | 2 | PspAllocateProcess×2 | 빠른 참조 해제 |
| `RtlImageDirectoryEntryToData` | 2 | MmCreatePeb, LdrpInitializeProcess | PE 디렉터리 항목 찾기 |
| `RtlImageNtHeaderEx` | 2 | LdrpInitializeProcess×2 | PE NT 헤더 찾기 |
| `ObpReferenceObjectByHandleWithTag` | 1 | NtCreateUserProcess | 핸들 → 객체 참조(태그) |
| `KiLeaveCriticalRegionUnsafe` | 1 | NtCreateUserProcess | 임계 구역 나감 |
| `ExFreePoolWithTag` | 1 | PspAllocateProcess | 풀 블록 해제 |
| `RtlCopyMemory` | 1 | PspAllocateProcess | 메모리 복사 |
| `RtlImageNtHeader` | 1 | MmCreatePeb | PE NT 헤더 찾기 |
| `ObReferenceObjectByHandle` | 1 | PspInsertProcess | 핸들 → 객체 참조 |
| `ObReferenceObjectExWithTag` | 1 | PspInsertThread | 객체 참조 여러 개 올림(태그) |
| `LdrpLogDllState` | 1 | _LdrpInitialize | 로더 DLL 상태 로그 |
| `RtlEncodePointer` | 1 | LdrpInitializeProcess | 포인터 인코딩 |
| `RtlAcquireSRWLockExclusive` | 1 | LdrpInitializeProcess | SRW 락 독점 잡기 |
| `RtlReleaseSRWLockExclusive` | 1 | LdrpInitializeProcess | SRW 락 풀기 |
| `RtlInitUnicodeStringEx` | 1 | LdrpInitializeProcess | 문자열 구조 초기화 |
| `RtlFreeHeap` | 1 | LdrpInitializeProcess | 힙 해제 |
| `RtlFreeAnsiString` | 1 | LdrpInitializeProcess | 문자열 해제 |
| `Feature_*` · `wil_details_*` (8종) | 11 | PspAllocateProcess×4, NtCreateUserProcess×3, PspSetupUserProcessAddressSpace×2, LdrpInitializeProcess×2 | 기능 스위치(WIL feature flag) 검사·사용 기록 — 이름 목록은 `callees_by_work.tsv` |

**실패·정리 길** — 함수별로 이름만 (성공 길에서 안 타는 호출).

| 함수 | 줄 | 이름 |
|---|---|---|
| `NtCreateUserProcess` | 10 | `ObCloseHandle`×3, `PspRundownSingleProcess`×3, `PspUnlockProcessExclusive`×2, `PsGetProcessServerSilo`, `PsTerminateProcess` |
| `PspAllocateProcess` | 3 | `PspUnlockProcessExclusive`×2, `PspRundownSingleProcess` |
| `PspAllocateThread` | 10 | `ObfDereferenceObjectWithTag`×2, `ExfTryToWakePushLock`×2, `KeAbPostRelease`×2, `MmDeleteTeb`, `PspDeleteUserStack`, `ExFreePoolWithTag`, `ObfDereferenceObject` |
| `PspInsertProcess` | 2 | `PspUnlockProcessExclusive`×2 |
| `PspInsertThread` | 18 | `ExfTryToWakePushLock`×6, `KeForceResumeThread`×3, `ExFreePoolWithTag`×2, `PspDeleteUserStack`×2, `KeUninitThread`, `MmDeleteTeb`, `ObCloseHandle`, `KeRaiseUserException`, `KeRequestTerminationThread` |
| `_LdrpInitialize` | 5 | `LdrpInitializationComplete`, `LdrpInitializationFailure`, `NtTerminateProcess`, `RtlRaiseStatus`, `LdrpInitializeProcessWrapperFilter` |
| `LdrpInitializeProcess` | 1 | `RtlRaiseStatus` |

**조각 점프 9줄** (호출 아님): `MmCreatePeb` 1 · `PspAllocateThread` 5 · `PspInsertThread` 3. 조각 안에서 부르는 호출은 위 정리 길·조건 갈래(일 35)로 셌다.

### 5.5 이 점검이 말해 주는 것

1. callee 표의 **모든 줄이 어느 일 또는 공용 부품·정리 길로 들어간다** (못 넣은 줄 0). 표 때문에 새로 세운 일은 53·54·61 셋뿐이다.
2. 그러나 callee 표는 **1단계만** 본다. 일 19개는 표에 0줄이다. "일을 빠짐없이 센다" 는 목표엔 callee 표(1단계)만으론 부족하고, 이번처럼 flow.md 의 트리를 같이 봐야 한다.
3. 일 안에서도 **"이름만 아는" 일** 이 많다 (태그 `[추측: 이름]`): 일 7 ~ 14 (PspAllocateProcess 안 묶음) 과 일 53 ~ 54. 이 일들은 "무엇을 놓나" 가 아직 비어 있어서 (가) 로 분류했어도 칸은 모른다 — Q78 의 몫이다.

## 6. 이번 바퀴에 본문을 읽어 닫은 것 (`[정적]`)

이번 바퀴에서 Ghidra 로 **본문을 읽고** 닫은 것이다 (`[정적]`, 코드는 안 붙임). 전부 baggage.md 의 빈 곳 (7장) 이다.

1. **`PEB.ProcessParameters` 를 옮기는 주인 (7.8 첫째, run44 가 `[공백]` 으로 남김) = `RtlpInitParameterBlock`** (일 55, `LdrpInitializeProcess +0xc3f` 가 부름).
   - 하는 일 순서: `+0x36 RtlAllocateHeap(PEB.ProcessHeap, 0, 블록.Length)` → `+0x52 RtlCopyMemory` → 안쪽 포인터 9개를 새 블록 기준으로 고침 (칸 `+0x40 +0x58 +0x68 +0x78 +0xb8 +0xc8 +0xd8 +0xe8 +0x418` = `CurrentDirectory`·`DllPath`·`ImagePathName`·`CommandLine`·`WindowTitle`·`DesktopInfo`·`ShellInfo`·`RuntimeData`·`RedirectionDllName` 의 `Buffer`, 각각 0 이 아닐 때만) → `PEB+0x20` 에 새 주소 → `+0x12b NtFreeVirtualMemory(-1, &옛, &0, MEM_RELEASE)`.
   - 사진과 맞는다: 옛 영역 VAD 가 사라졌고, 바뀐 포인터 칸은 `CurrentDirectory`·`ImagePathName`·`CommandLine`·`WindowTitle`·`DesktopInfo`·`ShellInfo` 이며 `DllPath`·`RuntimeData`·`RedirectionDllName` 은 0 이라 그대로 `[관찰]` run44.
   - `Environment` (`+0x80`) 는 여기서 안 고친다. 바로 앞 `+0xc32 RtlpInitEnvironmentBlock` 이 환경을 **별도 힙 블록**으로 복사하고 `Environment`·`EnvironmentSize`(`+0x3f0`) 를 쓰고 `EnvironmentVersion`(`+0x3f8`) 을 **+1** 한다. run44 의 `EnvironmentVersion` 0→3 중 +1 은 여기고 나머지 +2 는 아직 모른다 (`RtlpSetupEnvironmentHashTable` 쪽일 수 있음 `[추측]`).
2. **`LdrInitializeMrdata` 후보 (7.5) — 일부 닫힘.** `LdrInitializeMrdata` (109B) 는 `memset` 한 뒤 `LdrpMrdataLock = 0` (`.data` rva `0x1d4de8`) 을 쓰고, `NtQuerySystemInformation(0)` 이 성공하면 `LdrpAllocationGranularity`·`LdrpMaximumUserModeAddress` (`.mrdata` `0x1e7038`·`0x1e7030`, 둘 다 7.3 의 `≤P1a`) 를 쓴다. **`LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` 는 여기서 안 쓴다** — 그 4개 후보는 그대로 모름.
3. **CFG 도우미 (7.3: `LdrpGuardDispatchIcallNoESFptr` 는 `≤P1a`, `LdrpGuardCheckIcallNoESFptr` 는 `K1 이전`) 쓴 자리 닫힘.** `LdrpInitializeCfgScpHelpers` (일 61) 는 CFG 강제가 켜져 있고 ntdll 헤더·LoadConfig 플래그가 맞으면 `LdrProtectMrdata(0)` 으로 `.mrdata` 를 연 뒤 `LdrpGuardCheckIcallNoESFptr = LdrpValidateUserCallTarget`, `LdrpGuardDispatchIcallNoESFptr = LdrpDispatchUserCallTarget` 을 쓰고 `LdrProtectMrdata(1)` 로 닫는다.
4. **NLS 정보 (일 47).** `LdrpInitializeNlsInfo(PEB)` 가 `PEB.ActiveCodePage`(`+0x34c`)·`OemCodePage`(`+0x34e`) 를 쓴다 (둘 다 baggage 의 "모름" 칸). 값은 매니페스트(`LdrpFusionManifestCodePages`) → 없으면 레지스트리(`RtlpQueryNlsSystemCodePages`: `NtOpenKey`·`NtQueryValueKey`) → 그것도 실패면 `0xfde9`(65001, UTF-8) 이고, 매개변수 `Flags` 비트 27 이 켜져 있으면 곧장 `0xfde9`. `UseCaseMapping`(`+0x350`) 은 **읽기만** 한다. 이어서 `RtlpInitCodePageTables`(`NtGetNlsSectionPtr` ×2)·`RtlpInitUppercaseTables`(`NtGetNlsSectionPtr`) 가 커널에서 NLS 섹션 주소를 받는다 — run44 의 VAD `C_949.NLS`·`l_intl.nls` 가 이것 `[추측]` (커널 쪽 본문은 안 읽음).
5. **import 리다이렉션 (일 63) 은 안 탄다 (닫힘).** `LdrpInitializeImportRedirection` 은 매개변수 `RedirectionDllName`(`+0x410`) 의 길이가 0 이 아닐 때만 `LdrpLoadDll` 로 DLL 을 올리고 표를 만든다. 사진에서 이 칸은 끝까지 0 `[관찰]` 이므로 test.exe 에선 아무것도 안 한다.
6. **`ProcessParameters` 이동 주인이 정해져서** baggage 의 "쓴 주인" 이 `nt` 에서 `ntdll` 로 바뀌는 줄이 있다: `PEB.ProcessParameters` (`+0x20`) 는 ① `PspCopyAndFixupParameters` (nt) 와 ② `RtlpInitParameterBlock` (ntdll) **둘이** 쓴다 (3.1 은 ① 만 적혀 있음).


## 동적분석으로 확인해야 할 것 (대기열 맨 뒤로)

정적으로 결론 못 낸 것과, 이번 목록에서 새로 생긴 것. **Q 번호는 정하는 사람(오케스트레이터)이 붙인다.**

1. **부모가 자식 PEB 에 쓰는 순간** (일 37·38): K3→P0 사이에 자식 PEB `pShimData`(`25996d10000`)·`SystemDefaultActivationContextData`(`25996d00000`) 를 누가 쓰나. 부모 cmd.exe 에서 `NtWriteVirtualMemory` 입구(대상 프로세스 = test.exe)에서 `k`, 또는 자식 PEB 칸 쓰기 감시. Q19 와 같은 실행에서 할 수 있다.
2. **P1a~P1b 안을 쪼개기** (일 67): `Csr*` 8개·PEB CSR 칸 4개·`ConsoleHandle`(4→`0x60`)·`OwnerProcessId`(`0x19a8`→`0x729`) 가 바뀌는 자리. `_KernelBaseBaseDllInitialize` 입구/출구, `CsrClientConnectToServer` 뒤, `ConsoleInitialize` 뒤에 사진 지점을 더하거나 칸 쓰기 감시(`ba w`)를 건다.
3. **K1 이전 쪼개기** (일 8~23): "K1 이전" 에 모르는 칸이 218개 (유저 68 · 커널 147 · 전역 3, baggage 2.1) 있고 사진으로는 순서가 안 가려진다. `PspAllocateProcess` 안 `+0x163c`(주소 공간 직후), `+0x2095`(exe·ntdll 붙은 직후), `+0x2314`(PEB 직후), `+0x239c`(매개변수 복사 직후), `+0x2703`(PID 직후) 에 추가 지점을 두면 일 8~23 의 순서가 갈린다.
4. **오프라인 덤프에서 `!heap` 이 되나** (일 52): 작은 시험 한 번. 3장 C7·제안 5.
5. 기존 Q 와 겹치는 것 (새로 안 만듦): 일 40 멈춤 APC 로 실제로 잠들었나 (Q42·Q19), 일 77 IAT 쓰기 값 (Q68), 일 76 일꾼이 어느 스레드인지 (Q47), 일 55 `RtlpInitParameterBlock` 값 대조는 선택 — 입구 `ntdll+0xf66b0` 한 번 (낮음).

## 문서 반영 제안 (flow-writer, run45)

요청서 지시대로 템플릿 바깥에 새 장을 만들지 않는 방향이다. 하네스 변경 제안은 없다.

- **run45 문서**: 1장 일 목록 표 (+ 1.1 예시 대응표), 2장 분류 요약, 5장 빠짐 점검 요약 (숫자만: 637줄 → 일 433 / 공용 146 / 정리 49 / 조각 9, `doc=none` 57 → 일 또는 공용, 줄 0 인 일 19개). 일 번호는 이 결과서의 번호를 그대로 쓴다.
- **baggage.md 7장 "빈 곳"**: (나) 는 3장 C1~C12 요약 한 덩어리로 **7.6 "칸 표가 아직 없는 짐"** 에 합친다 (7.6 의 "PDB 에 있는 이웃 타입 / 없는 타입" 두 줄은 3.1 의 확인 결과로 고친다: `_LDRP_LOAD_CONTEXT`·`_EPROCESS_QUOTA_BLOCK` 이 "없는" 쪽에 더해지고, 8개 이웃 타입은 "있는" 쪽으로 확정). 6장의 닫은 6건은 **7.3·7.5·7.8** 해당 줄에 반영하고, 3.1 `_PEB` 의 `+0x20 ProcessParameters` 줄에 쓴 주인 하나 (`RtlpInitParameterBlock`, ntdll) 를, 3.2 의 `Environment`·`EnvironmentSize`·`EnvironmentVersion` 줄에 `RtlpInitEnvironmentBlock` 을 더하며, 3.8~3.9 에 `.data`/`.mrdata` 전역 5개의 쓴 자리 (일 46·61) 를 더하고, 3.1 의 `ActiveCodePage`·`OemCodePage` 를 "모름" 에서 "아는" 칸으로 옮긴다 (일 47).
- **flow.md**: 2.4 `LdrpInitializeProcess` 표의 묶음 8 (`+0xc32 RtlpInitEnvironmentBlock`, `+0xc3f RtlpInitParameterBlock`) 에 "매개변수 블록을 힙으로 옮김" 한 줄과, 4장의 "`ProcessParameters` 를 누가 옮기나" 해결 표시. 2.4 앞쪽 묶음 2 의 `LdrpInitializeNlsInfo`, `_LdrpInitialize` 절의 `LdrInitializeMrdata`, 뒤쪽 `+0x1963 LdrpInitializeImportRedirection` (조건 `RedirectionDllName`), `+0x180b LdrpInitializeCfgScpHelpers` 한 줄씩.
- **plan.md** (오케스트레이터): Q81 끝 (sast/39), 4.3 의 새 할 일 11개 중 사용자가 고른 것을 맨 뒤에 새 항목으로, Q79 에 제안 1 의 읽기 묶음 붙이기, Q78 에서 "`ProcessParameters` 를 P1→P1a 사이에 힙으로 옮기는 줄"·"`LdrInitializeMrdata` 본문" 두 덩어리를 빼기.

## 판정

**분석성공.** 요청서 네 질문 모두 답했다: 일 86개 표(질문 1), 분류 (가) 69 / (나) 48 / (다) 14 — 가·나가 겹치는 45개 때문에 합이 86보다 크다 (질문 2), 대기 항목 이음과 새 할 일 (질문 3), callee 637줄 전부 일 또는 공용 부품·정리 길로 (질문 4, 못 넣은 줄 0). Ghidra 는 분류가 안 되던 14개 함수에만 depth 0 으로 읽기만 했다 (analyze 안 함).

한계 (쓸 때 알아야 할 것):

- flow.md·baggage.md 를 합본으로 믿고 썼다. run 문서·결과서를 줄 단위로 다시 읽은 건 아니다.
- **"일" 의 경계는 내가 정한 것이다.** 특히 DLL 올리기를 모듈별이 아니라 부품별로 센 것, `PspAllocateProcess` 의 14묶음을 일 8~23 로 다시 자른 것은 판단이다. 다른 자름도 가능하다.
- (나) 의 "덤프로 읽힐지" 는 모두 `[추측]` 이다. 읽어 본 것이 아니다. ntdll `.data` 가 K1~P1 에서 읽기 불가였던 것(run44)처럼 **그 시점에 페이지가 없으면 `????`** 일 수 있다.
- 태그 `[추측: 이름]` 인 일(커널 `PspAllocateProcess` 안 묶음, 힙·ETW 등)은 "무엇을 놓나" 를 아직 모른다. (가)/(나) 분류도 이름에 의존한다.

## 근거 (쓴 명령)

- `python3 .claude/skills/pe-flow-cycle/scripts/next_index.py latest sast` → `_workspace/sast/39_work-list.md`
- `bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh export ntdll.dll '^(LdrpInitializeNlsInfo|RtlpInitParameterBlock|RtlpInitEnvironmentBlock|LdrpInitializeCfgScpHelpers|LdrpInitializeImportRedirection|LdrInitializeMrdata)$' 0` (3.6초)
- 같은 스크립트, `'^(RtlpInitCodePageTables|RtlpQueryNlsSystemCodePages|RtlpInitUppercaseTables|SbObtainTraceHandle|LdrpInitializeSmtDelayedSleep|RtlpInitializeThreadActivationContextStack|LdrpInitializeSnapsBuffer|LdrpCaptureCriticalThunks)$' 0`
- `llvm-pdbutil-14 dump -publics ms/sym/{KernelBase,kernel32,csrsrv,basesrv}.dll.pdb` + `ms/ghidra/_tmp_sast38/pubs.py`·`pesec.py` (구역별 공개 데이터 이름 수: KERNELBASE `.data` 3,125 · KERNEL32 197 · csrsrv 42 · basesrv 55)
- PDB 타입 정의 확인: `ms/ghidra/_tmp_sast38/nt_types.txt`·`ntdll_types.txt` 를 정방향 선언 제외하고 파싱 (3.1)
- 일 목록·callee 매핑·이 문서 생성: 임시 스크립트 `ms/ghidra/_tmp_sast39/` (`works_a.py` `works_b.py` `mapcallee.py` `extra.py` `texts.py` `gen.py`). 다시 돌리면 같은 문서·TSV 가 나온다.
- 읽은 `.c`: `ms/decomp/ntdll/{RtlpInitParameterBlock,RtlpInitEnvironmentBlock,LdrInitializeMrdata,LdrpInitializeCfgScpHelpers,LdrpInitializeNlsInfo,LdrpInitializeImportRedirection}.c`. `_calls.tsv` 로 본 것: `RtlpInitCodePageTables` `RtlpQueryNlsSystemCodePages` `RtlpInitUppercaseTables` `SbObtainTraceHandle` `LdrpInitializeSmtDelayedSleep` `LdrpInitializeSnapsBuffer` `LdrpCaptureCriticalThunks`.


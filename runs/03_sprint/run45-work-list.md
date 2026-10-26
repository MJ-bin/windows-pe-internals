# 2026-10-26 run45 — 일 목록: 초기화 동안 nt·ntdll 이 하는 일 86개 (짐 지도 칸 69 · 체크리스트 밖 짐 48 · 흐름도에만 14), sast/38 callee 637줄 모두 일·공용 부품·정리 길로 나눔 (못 넣은 줄 0), 체크리스트 밖 짐 "남는 곳" C1~C12. 본문을 읽어 빈 곳 6건을 닫음 — `ProcessParameters` 를 힙으로 옮기는 주인 = `RtlpInitParameterBlock` (`LdrpInitializeProcess +0xc3f`)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 03_sprint 셋째 바퀴 (짐 지도 바퀴, plan **Q81**, pe-flow-cycle 단계 4).
- 정적: ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E`, ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE` (flow.md 기준과 같음). 두 프로젝트 다 다시 analyze 안 함.
- 버전 대조 (결과서): `NtCreateUserProcess +0xc36 → PspAllocateProcess`, `PspInsertThread +0x807 → PspCallProcessNotifyRoutines`, `LdrInitializeThunk +0xe → LdrpInitialize`, `LdrpInitializeInternal +0x5a → _LdrpInitialize`, `_LdrpInitialize +0x16c → LdrpInitializeProcess`, `LdrpInitializeProcess +0x1bbb → LdrpInitializeGraphRecurse` — `_calls.tsv`·`callees.tsv` 에서 6/6 같음.
- 이 문서를 쓰며 다시 맞춘 것:
  - 아래 "호출 지점" 표의 줄을 `ms/disassemble/ntdll/_calls.tsv` 와 `.asm` 5개 (`RtlpInitParameterBlock`·`RtlpInitEnvironmentBlock`·`LdrInitializeMrdata`·`LdrpInitializeCfgScpHelpers`·`LdrpInitializeNlsInfo`) 로 다시 봤다. 결과서와 같다.
  - 닫은 칸의 값을 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_<P>.txt`, dast/04) 에서 찾아 맞춰 봤다 (아래 6장 "사진과 맞춰 보기"). 새 동적 실행은 없다.

## 목적

- 짐 지도 (`baggage.md`) 는 "어느 칸에 무엇이 들어가나" 를 센다. 그런데 사용자가 물은 둘째 질문은 더 넓다 — **초기화 동안 nt·ntdll 이 무슨 일을 해 주나** (PE 로드, DLL 로드, 그 밖의 일).
- 그래서 이번엔 **일을 빠짐없이 세고 나눈다** (요청서 질문 넷):
  1. 일 목록 (프로세스 생성 → `NtContinue` 은퇴)
  2. 일마다 분류: (가) 짐 지도 칸으로 덮임 / (나) 체크리스트 밖 짐을 남김 / (다) 흐름도에만 있음
  3. 기존 대기 항목과 잇기, 어디에도 안 걸리는 "새 할 일"
  4. 빠짐 점검: sast/38 callee 표 637줄이 모두 어느 일에 들어가나
- 일마다 줄기를 끝까지 파는 건 이번 범위 밖 (Q78·뒤 항목). "구현할지" 로 거르지 않는다.

## 방법

- 정적: `_workspace/sast/39_work-list.md` — 문서 (flow.md 그림·2장·4장, baggage.md, run44, sast/38·sast/03 결과서, run10) 를 합쳐 일 목록을 만들고, sast/38 callee 표 637줄을 줄마다 일 번호에 넣었다.
- 분류가 안 되던 ntdll 함수 14개만 Ghidra depth 0 으로 읽기만 했다 (analyze 아님, 각 4초 안팎):
  - 1차 6개: `LdrpInitializeNlsInfo` `RtlpInitParameterBlock` `RtlpInitEnvironmentBlock` `LdrpInitializeCfgScpHelpers` `LdrpInitializeImportRedirection` `LdrInitializeMrdata`
  - 2차 8개: `RtlpInitCodePageTables` `RtlpQueryNlsSystemCodePages` `RtlpInitUppercaseTables` `SbObtainTraceHandle` `LdrpInitializeSmtDelayedSleep` `RtlpInitializeThreadActivationContextStack` `LdrpInitializeSnapsBuffer` `LdrpCaptureCriticalThunks`
  - `ms/disassemble/ntdll/_calls.tsv` 1,559줄 → 1,625줄 (줄어든 줄 없음).
- PDB 타입 정의 확인: run43 이 만든 `llvm-pdbutil dump -types` 결과에서 정방향 선언이 아닌 정의 레코드를 찾음. KERNELBASE·KERNEL32·csrsrv·basesrv 는 `-publics` 로 구역별 공개 데이터 이름 수만 셈.
- 결과서: `_workspace/sast/39_work-list_result.md` (**분석성공**)
- 줄별 표 (커밋 안 함): `ms/disassemble/{ntoskrnl,ntdll}/_baggage/callees_by_work.tsv` — callee 637줄 각각의 일 번호·분류. 임시 스크립트 `ms/ghidra/_tmp_sast39/`.
- 문서: 이 run 문서 (일 목록 표), `baggage.md` 7장 (체크리스트 밖 짐 C1~C12, 닫은 6건), `flow.md` 2.4·4장.

## 결과

비유 (이삿날 작업 지시서):

- 짐 지도는 "어느 서랍에 뭘 넣었나" 만 적은 표였다.
- 이번엔 **작업 지시서 전체** (일 86개) 를 쓰고, 서랍 밖에 놓인 짐 (바닥 상자·옆집 창고·이웃 집 서랍) 이 얼마나 되는지 셌다. 86개 중 48개가 서랍 밖에도 짐을 남긴다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`, 이번에 본문을 읽은 함수 둘레)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!_LdrpInitialize` | `+0x145` | `+0x14a` | `ntdll!LdrInitializeMrdata` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1c8` | `+0x1cd` | `ntdll!LdrpInitializeNlsInfo` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xc2d` | `+0xc32` | `ntdll!RtlpInitEnvironmentBlock` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xc3a` | `+0xc3f` | `ntdll!RtlpInitParameterBlock` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1806` | `+0x180b` | `ntdll!LdrpInitializeCfgScpHelpers` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x195e` | `+0x1963` | `ntdll!LdrpInitializeImportRedirection` | [정적] |
| `ntdll!LdrInitializeMrdata` | `+0x39` | `+0x3e` | 시스템 정보 조회 (같은 주소 이름 2개: `NtQuerySystemInformation`·`RtlGetNativeSystemInformation`) | [정적] |
| `ntdll!LdrpInitializeNlsInfo` | `+0x2a` | `+0x2f` | `ntdll!LdrpFusionManifestCodePages` | [정적] |
| `ntdll!LdrpInitializeNlsInfo` | `+0x3d` | `+0x42` | `ntdll!RtlpQueryNlsSystemCodePages` | [정적] |
| `ntdll!LdrpInitializeNlsInfo` | `+0x5c` | `+0x61` | `ntdll!RtlpInitCodePageTables` | [정적] |
| `ntdll!LdrpInitializeNlsInfo` | `+0x6d` | (꼬리 JMP) | `ntdll!RtlpInitUppercaseTables` | [정적] |
| `ntdll!RtlpInitEnvironmentBlock` | `+0x62` | `+0x67` | 메모리 복사 함수 (같은 주소 이름 4개, 후보: `RtlCopyMemory`·`RtlMoveMemory`·`memcpy`·`memmove`) | [정적] |
| `ntdll!RtlpInitEnvironmentBlock` | `+0x9b` | `+0xa0` | `ntdll!RtlpSetupEnvironmentHashTable` | [정적] |
| `ntdll!RtlpInitParameterBlock` | `+0x36` | `+0x3b` | `ntdll!RtlAllocateHeap` | [정적] |
| `ntdll!RtlpInitParameterBlock` | `+0x52` | `+0x57` | 메모리 복사 함수 (위와 같은 주소) | [정적] |
| `ntdll!RtlpInitParameterBlock` | `+0x12b` | `+0x130` | `ntdll!NtFreeVirtualMemory` | [정적] |
| `ntdll!LdrpInitializeCfgScpHelpers` | `+0x5a` · `+0x80` | `+0x5f` · `+0x85` | `ntdll!LdrProtectMrdata` (열기 · 닫기) | [정적] |
| `ntdll!LdrpInitializeImportRedirection` | `+0xd0` | `+0xd5` | `ntdll!LdrpLoadDll` (조건부) | [정적] |
| `ntdll!LdrpInitializeImportRedirection` | `+0xf1` | `+0xf6` | `ntdll!LdrpBuildImportRedirection` (조건부) | [정적] |

### 호출 관계 (이번에 본문을 읽은 곳만)

```
_LdrpInitialize
 ├─ +0x14a  LdrInitializeMrdata          LdrpMrdataLock = 0 (+0x23) → 시스템 정보 조회 (+0x3e)
 │                                       → 성공이면 LdrpAllocationGranularity (+0x46) · LdrpMaximumUserModeAddress (+0x52)
 └─ +0x16c  LdrpInitializeProcess
      ├─ +0x1cd  LdrpInitializeNlsInfo   매개변수 Flags 비트 27 → 곧장 0xfde9 (+0x77)
      │    ├─ +0x2f  LdrpFusionManifestCodePages   (매니페스트)
      │    ├─ +0x42  RtlpQueryNlsSystemCodePages   (없으면 레지스트리)
      │    │   PEB.ActiveCodePage (+0x4e) · OemCodePage (+0x55)
      │    ├─ +0x61  RtlpInitCodePageTables         (NtGetNlsSectionPtr ×2)
      │    └─ ⇒      RtlpInitUppercaseTables        (NtGetNlsSectionPtr)
      ├─ +0xc32  RtlpInitEnvironmentBlock 옛 매개변수 블록에: 환경 힙 복사 → EnvironmentVersion +1 (+0x86)
      │                                   · Environment (+0x8d) · EnvironmentSize (+0x94) → +0xa0 RtlpSetupEnvironmentHashTable
      ├─ +0xc3f  RtlpInitParameterBlock   +0x3b RtlAllocateHeap(PEB.ProcessHeap, Length) → +0x57 통째 복사
      │                                   → 포인터 9개 새 자리 기준 (+0x6b ~ +0x106) → PEB+0x20 = 새 블록 (+0x10d)
      │                                   → +0x130 NtFreeVirtualMemory(옛 블록, MEM_RELEASE)
      ├─ +0x180b LdrpInitializeCfgScpHelpers  (CFG 강제 + 헤더·LoadConfig 플래그) → LdrProtectMrdata(0)
      │                                   → LdrpGuardCheckIcallNoESFptr (+0x6b) · LdrpGuardDispatchIcallNoESFptr (+0x79) → LdrProtectMrdata(1)
      └─ +0x1963 LdrpInitializeImportRedirection  (RedirectionDllName 길이 ≠ 0 일 때만) → +0xd5 LdrpLoadDll · +0xf6 LdrpBuildImportRedirection
```

### 1. 일 목록 (질문 1·2·3)

- 일 86개. **번호는 결과서 번호 그대로** 다 (다음 바퀴에서 "일 55" 처럼 부른다). 시간 순서다: 부모 입구 → 커널 `NtCreateUserProcess` 안 → 부모 마무리 → 첫 스레드 커널 쪽 → ntdll.
- 열은 결과서 표에서 **근거 열을 빼고**, 분류 열은 갈래 (가/나/다) 만 남겼다. 근거 (run·결과서 줄) 와 분류 칸의 "어느 칸·어디에 남나" 는 결과서 1장 같은 번호 줄에 있다.
- 읽는 법 (결과서와 같음):
  - 한 일 = 한 덩어리 작업. DLL 하나를 올리는 일은 kernel32·kernelbase·dll1·dll2 가 **같은 부품** (일 70 ~ 79) 을 거치므로 부품별로 한 번만 셌다.
  - 사슬 머리 `①…` = 부모 cmd.exe 스레드의 `NtCreateUserProcess`, `②…` = `LdrInitializeThunk → LdrpInitialize ⇒ LdrpInitializeInternal → _LdrpInitialize → LdrpInitializeProcess` (baggage.md 약어). `+0x…` 는 돌아올 자리 (ret), `→` 부름, `⇒` 꼬리 JMP, `┄▶` 다른 프로세스로 넘어감.
  - 구간 = 짐 지도 사진 구간 (run44). "사진 전" = 부모 쪽, `—` = 조건 갈래.
  - 태그: `[관찰]` 디버거·사진, `[정적]` 코드, `[정적: 이름]`·`[추측: 이름]` 함수 이름만 보고 짐작 (본문 안 읽음), `[정적+샘플]` sample PE 를 센 것.
  - 분류: **가** = 체크리스트 (구조체 21종 + ntdll 전역) 칸에 남음 · **나** = 체크리스트 밖에 남음 · **다** = 짐을 안 남기거나 잠깐 쓰고 지움. "가·나" = 둘 다.
  - 기존 대기: plan.md 대기열 번호. `(끝)` = 이미 끝난 항목.
- **"일" 의 경계는 결과서가 정한 것이다** (결과서 "한계"). 특히 DLL 올리기를 부품별로 센 것, `PspAllocateProcess` 의 14묶음 (run10) 을 일 8 ~ 23 으로 다시 자른 것은 판단이다.

#### A. 부모 쪽 입구와 커널 `NtCreateUserProcess` 안 (부모 cmd.exe 스레드)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 (지도 기준) | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|
| **1** | 부모 입구 — `CreateProcessW` 껍데기 둘 → `CreateProcessInternalW` 앞부분 (`ThreadFlags` 늘 1, `dwCreationFlags` 에 4 없으면 `ProcessFlags` bit 9, 시스템 콜 인자 만들기. 매개변수 블록 만들기는 `BasepCreateProcessParameters` `[추측]`) | 다른 프로세스 (부모 유저) | 2.2 · ① 앞 | 사진 전 | `cmd!ExecPgm +0x59c → kernel32!CreateProcessW +0x54 → KERNELBASE!CreateProcessW +0x66 → CreateProcessInternalW +0x30ee → ntdll!NtCreateUserProcess` | `[정적]` (cmd 쪽은 import 표만, 실행은 `[공백]`) | 다 | Q19·Q41(끝)·Q66 |
| **2** | 시스템 콜 입구 · 인자 검사 · 만들기 정보·속성 목록 풀기 · 부모 핸들·silo·권한 확인 · 메모리 파티션 문맥 | 커널 | 2.1 · ① | K1 이전 | `①… +0x29a PspBuildCreateProcessContext · +0x316 PspCaptureCreateInfo · +0x364 ObpReferenceObjectByHandleWithTag · +0x38c~+0x3c2 silo 확인 · +0x4e0 SeSinglePrivilegeCheck · +0x5e2 PspGetMemoryPartitionContext` (진입: ntdll 스텁 `0xd1` SYSCALL → `KiSystemServiceCopyEnd`) | `[관찰]` 호출 자리 / `[정적]` 안쪽 | 다 | Q34 |
| **3** | exe 파일 열기 (확장 파라미터 만들기 → 열기 → 실패하면 한 번 더 → 객체 참조) | 커널 | 2.1 · ① | K1 이전 | `①… +0x5ff PspCreateUserProcessEcp · +0x634 FsRtlpPrepareExtraCreateParametersForCreate · +0x6cb IopCreateFile (재시도 +0x740 IoCreateFileEx) · +0x791 ObReferenceObjectByHandle` | `[정적]` | 가·나 | Q34·Q78 |
| **4** | exe 이미지 섹션 만들기 — 헤더 읽기·검사, 컨트롤 영역·세그먼트·서브섹션·프로토 PTE, 진입점 `TransferAddress` 계산 (같은 파일 서랍이 있으면 공유 길) | 커널 | 2.1 · ① | K1 이전 | `①… +0x93c MmCreateSpecialImageSection → MiCreateSection → MiCreateImageOrDataSection →(갈림) MiCreateNewSection → MiCreateImageFileMap → MiVerifyImageHeader · MiBuildImageControlArea; → MiSectionCreated; → MiRelocateImage` (공유 길 `+0x1d5 MiShareExistingControlArea`) | `[정적]` (결과 칸은 K1 `[관찰]`) | 가·나 | Q61·Q62·Q34·Q78 |
| **5** | 서명 수준·보호 프로세스 요구 검사, 그리고 "수준이 다르면 섹션을 닫고 다시 만들기" 고리 | 커널 | 2.1 · ① | K1 이전 | `①… +0x995 PspGetProcessProtectionRequirementsFromImage · +0x9dd SeQuerySigningPolicy · +0xa0d SeCompareSigningLevels · +0xa23 ObCloseHandle(재시도) · +0xa69·+0xbab PspValidateCreateProcessProtection · +0xa9d PspCheckForInvalidAccessByProtection · +0xe94 PsTestProtectedProcessIncompatibility` | `[정적]` | 다 | Q34 |
| **6** | 프로세스 매개변수 커널 사본 만들기 (부모가 준 블록을 커널 풀에 새로 복사, 입력 마스크 `0xf4010e11` 검사) | 커널 (부모 유저 메모리를 읽음) | 2.1 · ① | K1 이전 | `①… +0xab8 PspGetProcessParameterOverrides · +0xacb PspCaptureProcessParameters → PspCaptureUserProcessParameters` | `[정적]` | 다 | Q63(a)(e)·Q78 |
| **7** | 새 프로세스 토큰 정하기·붙이기 — 부모 토큰 참조·복제(가장 끄고 켜기), 권한·세션 확인, 변조 방지 정책 | 커널 | 2.1 · ① | K1 이전 | `①… +0x41c PspReferenceTokenForNewProcess · +0x804 SeDuplicateTokenWithPredictedClaims · +0x8e2 SeDuplicateTokenAndAddOriginClaim → PspAllocateProcess +0x147f SeIsTokenAssignableToProcess · +0x1927 PspInitializeProcessSecurity · +0x264f SeConstructAntiTamperPolicy · +0x26b1 SeSetTokenTamperProtection` | `[추측: 이름]` | 가 | Q78 |
| **8** | 프로세스 객체 만들기 + exe 이미지 정보 복사 (파티션·섹션 참조, 런다운 보호, 머신 종류, 에너지 문맥) | 커널 | 2.1 · ① | K1 이전 | `①… +0xc36 PspAllocateProcess +0x539 ObCreateObjectEx · +0x835 PsReferencePartitionSafe · +0x991 MmGetSectionInformation · +0xa8c PspSelectMachineForProcess · +0x75c PoEnergyContextInitialize` | `[관찰]` 호출 자리 / `[정적]` | 가·나 | Q35·Q78 |
| **9** | 실행 옵션(IFEO) 읽기 — 커널 쪽 (성능 옵션·노드 옵션) | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0xa61 RtlpOpenImageFileOptionsKeyEx · +0xb16~+0xe94 RtlQueryImageFileKeyOption ×8 · +0xcd2 PspReadIFEONodeOptions · +0xe59 PspReadIFEOPerfOptions · +0x19e0 PspApplyIFEOPerfOptions` | `[추측: 이름]` | 가 | Q78 |
| **10** | 세션·쿼터·프로세서 그룹(NUMA)·친화도·CPU Set·우선순위·quantum 정하기 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x14d0 PsGetProcessSessionIdEx · +0x1524 PspAttachSession · +0x1594 PspAssignProcessQuotaBlock · +0x12b8 PspSelectNodeForProcess · +0x1a05 KeSetCpuSetsProcess · +0x1a40 PspSetProcessPriorityClass · +0x1a64 PspComputeQuantumAndPriority · +0x2364 PspSetProcessAffinitySafe` | `[추측: 이름]` | 가·나 | Q78 |
| **11** | 주소 공간 껍데기 — 최상위 페이지 표(PML4), 커널 쪽 매핑 잇기, 세션에 넣기, 커밋 청구 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x163c MmCreateProcessAddressSpace → MiAllocateTopLevelPage · MiCreateNewProcessTopLevelMappings · PsJoinSession` (+ `+0x1692 MmInitializeHandBuiltProcess`, `+0x19b1 MmInitializeHandBuiltProcess2`) | `[정적]` | 가·나 | Q78 |
| **12** | 커널 프로세스 구조와 핸들 표 만들기 (`KeInitializeProcess`, `ObInitProcess` — 물려줄 핸들이 있으면 같이) | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x171e KeInitializeProcess · +0x1991 ObInitProcess` | `[추측: 이름]` | 가·나 | Q78 |
| **13** | 완화(mitigation) 정책 정하기 — IFEO·부모에서 물려받기·강화·CET 허용 여부·앱 모델 정책 → 24바이트 완화 옵션 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x1a90 PspReadIFEOMitigationOptions · +0x1aef PspInheritMitigationOptions · +0x1c87 PspHardenMitigationOptions · +0x1c8c KeIsUserCetAllowed · +0x1eb8 PspApplyMitigationOptions · +0x1fb8 AppModelPolicy_GetPolicy_Internal · +0x200d KeInitializeProcessUserCetLogging` | `[정적]` | 가 | Q35·Q63·Q78 |
| **14** | 전체 이미지 이름 만들기 · 생성 알림 (`SmProcessCreateNotification`) · 세션 리더 확인 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x1ff8 SmProcessCreateNotification · +0x202e PspIsSessionLeaderProcess · +0x206a PspInitializeFullProcessImageName` | `[추측: 이름]` | 가·나 | Q78 |
| **15** | 주소 공간 장부 — 작업 집합, 유저 주소 범위 계산, 메모리 구역 장부(VAD) 처음 8개 만들어 넣기 (KUSD 두 페이지 등), 예약 매핑 | 커널 | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x2095 MmInitializeProcessAddressSpace → +0xed MiInitializeWorkingSetList · +0x13b MiComputeProcessUserVa · +0x161 MiAllocateProcessVads · +0x1a8 MiInsertProcessVads;  +0x223e PspSetupReservedUserMappings` | `[관찰]`+`[정적]` | 가·나 | Q35·Q78·Q79 |
| **16** | exe 이미지를 프로세스 메모리에 붙이기 (+ 다른 주소면 진입점 보정, CFG 준비 후보) | 커널 | 2.1 · ① | K1 이전 | `①… MmInitializeProcessAddressSpace +0x1ce MiMapProcessExecutable +0xb8 MmMapViewOfSectionEx → +0xc5 SectionBaseAddress` (+ `+0xe3 MiCfgInitializeProcess` `[정적: 지도]`, 보정 `PspAllocateProcess +0x20b3~+0x20ce`) | `[정적]` (붙은 주소는 `[관찰]`) | 가·나 | Q35·Q61·Q62(f) |
| **17** | ntdll 이미지를 프로세스 메모리에 붙이기 (부팅 때 정한 주소·공용 섹션) | 커널 | 2.1 · ① | K1 이전 | `MiMapProcessExecutable +0xdc PsMapSystemDlls → +0xc0 PspMapSystemDll +0x11d MmMapViewOfSectionEx` (+ `PspAllocateProcess +0xffb PspWow64PickBestNtdll` 조건) | `[정적]` | 가·나 | Q35·Q80 |
| **18** | PEB 만들기 — PEB 자리, exe 헤더에서 서브시스템·버전, 프로세서 수, 세션 번호 (+ PEB 칸 더 쓰는 후보 `KeCopyXfdMaskToPeb`·`PspUpdatePebForAffinityChange`) | 커널 (새 프로세스에 붙어서 유저 칸에 씀) | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x2314 MmCreatePeb → +0xc7 KiStackAttachProcess · +0x14b MiCreatePebOrTeb · +0x2ce RtlImageNtHeader · +0x93 KeQueryGroupMaskProcess`;  `PspSetupUserProcessAddressSpace +0x227 KeCopyXfdMaskToPeb · +0x234 PspUpdatePebForAffinityChange` | `[관찰]`+`[정적]` | 가 | Q35·Q78 |
| **19** | 읽기 전용 뷰 셋 올리기 — leap second 데이터, API set 지도, silo 공유 데이터 | 커널 | 2.1 · ① | K1 이전 | `MmCreatePeb +0x132 MmMapViewOfSection`;  `PspSetupUserProcessAddressSpace +0x255 MmMapApiSetView · +0x263 PspMapSiloSharedDataView` | `[정적]` (뷰 주소는 `[관찰]`) | 가·나 | Q78·Q20 |
| **20** | 매개변수 블록을 새 프로세스로 복사 + 안쪽 포인터 12개 고치기 + `PEB+0x20` 쓰기 (붙기 전에 커널 사본의 `Flags`·`LoaderThreads` 손봄) | 커널 (새 프로세스에 붙어서 유저 칸에 씀) | 2.1 · ① | K1 이전 | `①… PspAllocateProcess +0x239c PspSetupUserProcessAddressSpace +0x19c ZwAllocateVirtualMemory · +0x1b9 PspCopyAndFixupParameters (+0x52b / +0x347)` | `[정적]` | 가·나 | Q35·Q63·Q79 |
| **21** | 콘솔·표준 입출력 핸들을 부모에서 새 프로세스로 복제 (`ObDuplicateObject`) | 커널 | 2.1 · ① | K1 이전 | `PspCopyAndFixupParameters +0xbc / +0x130 ObDuplicateObject;  PspSetupUserProcessAddressSpace +0x92 PspGetStandardHandleList` | `[정적]`+`[관찰]` | 가·나 | Q63(c)·Q78 |
| **22** | 시스템 DLL 시작 정보 블록 채우기 — ntdll `LdrSystemDllInitBlock` (난수·완화 옵션·CFG 비트맵 주소·SCP 표) | 커널 (새 프로세스 ntdll `.mrdata` 에 씀) | 2.1 · ① | K1 이전 | `PspSetupUserProcessAddressSpace +0x1e6 PspPrepareSystemDllInitBlock` | `[정적]` (값은 K1 `[관찰]`) | 가·나 | Q35·Q63(b)(d)·Q71(b) |
| **23** | 프로세스 ID 얻기(`PspCidTable`)·프로세스 락 잡기·이미지 파일 강한 참조·`KeSecureProcess`·생성 시각 | 커널 | 2.1 · ① | K1 이전 | `PspAllocateProcess +0x26e9 PspLockProcessExclusive · +0x2703 ExCreateHandleEx · +0x274a MmGetSectionStrongImageReference · +0x27a0 KeSecureProcess · +0x27c9 KeQuerySystemTimePrecise` | `[정적]` | 가·나 | Q78 |
| **24** | 첫 스레드의 시작 문맥 만들기 (`CONTEXT`: Rip=`RtlUserThreadStart`, Rcx=진입점, Rdx=PEB) + `ThreadFlags` 변환 | 커널 | 2.1 · ① | K1 이전 (지역 값) | `①… +0xce0 RtlInitializeExtendedContext2 · +0xd22 PspCreateUserContext · +0xdd3 PspMapThreadCreationFlags` | `[정적]` | 다 | Q34·Q38 |
| **25** | 스레드 객체 만들기·스레드 ID 주기·시작 주소 옮겨 적기 | 커널 | 2.1 · ① | K1~K2 | `①… +0xe5c PspAllocateThread +0x2e7 ObCreateObjectEx · +0x51c ExfAcquirePushLockExclusiveEx(스레드 락) · +0x532 PsAssignThreadId · +0x640~+0x655 StartAddress·Win32StartAddress` | `[관찰]`+`[정적]` | 가·나 | Q38·Q78 |
| **26** | 유저 스택 만들기 (VAD + 스택 정보) · (CET 면 그림자 스택) | 커널 (새 프로세스에 붙어서) | 2.1 · ① | K1~K2 | `PspAllocateThread +0x5ef PspSetupUserStack` (조건 `+0xb65 PspSetupUserShadowStack`) | `[정적]` (스택 VAD 는 K2 `[관찰]`) | 가·나 | Q38 |
| **27** | TEB 만들기 (대입 약 47줄) | 커널 (새 프로세스에 붙어서 유저 칸에 씀) | 2.1 · ① | K1~K2 | `PspAllocateThread +0x631 MmCreateTeb +0x7f KiStackAttachProcess · +0xac MiAllocateFromSubAllocatedRegion · (대입 ~47줄) · +0x29e KiUnstackDetachProcess` | `[정적]` (칸별은 `[공백]`) | 가 | Q38·Q78 |
| **28** | 스레드 커널 쪽 초기화 — 커널 스택·`KernelStack`·트랩 프레임 처음 문맥·APC·이벤트·시작 정보 | 커널 | 2.1 · ① | K1~K2 | `PspAllocateThread +0x6f9 KeInitThread +0x152 KeInitializeApc · +0x163 KeInitializeEvent · +0x301 KiInitializeContextThread → KxContextToKframes` | `[정적]` (칸 값 K2 `[관찰]`) | 가·나 | Q38·Q42·Q78 |
| **29** | 프로세스를 시스템 목록에 넣기 — 감사·시스템 콜 제공자 상속, 목록 잠금 안 `ActiveProcessLinks`·`SequenceNumber` 직접 쓰기, 잡 상속·디버그 포트 복사(조건), 객체 등록 | 커널 | 2.1 · ① | K2~K3 | `①… +0xfc2 PspInsertProcess +0x78 SeAuditProcessCreation · +0xa6~+0x115 목록 잠금 · +0x143 DbgkCopyProcessDebugPort · +0x1e3 ObInsertObjectEx` | `[관찰]`+`[정적]` | 가·나 | Q38·Q78 |
| **30** | 스레드 넣기 (첫 스레드 길) — `KeStartThread`(스케줄링 준비), 스레드 수·첫 스레드 표시, 스레드 목록 달기, 프로세스 락 풀기, 객체 등록, 스레드 락 풀기 | 커널 | 2.1 · ① | K2~K3 | `①… +0x101a PspInsertThread +0x5e2 KeStartThread · +0x266 목록 달기 · +0x2a2 락 풀기 · +0x3ea ObInsertObjectEx · +0x490 ThreadInserted · +0x499 락 풀기` | `[관찰]`+`[정적]` | 가·나 | Q39·Q78 |
| **31** | 첫 스레드 멈춰 두기 — `SuspendCount` +1, `SuspendEvent` 0, 멈춤 APC 걸기 | 커널 | 2.1·2.2 · ① | K2~K3 | `PspInsertThread +0x2e5 KeSuspendThread → +0x6b KiSuspendThread → +0x88 KiInsertQueueApc · +0x1fb KiSignalThreadForApc` | `[관찰]`+`[정적]` | 가 | Q19·Q39 |
| **32** | 알림 콜백 — 프로세스 알림 4곳 (등록 목록 64칸·BAM·DAM), 스레드 알림, ETW | 커널 (받는 쪽은 드라이버·ETW) | 2.1 · ① | K3~P0 | `PspInsertThread +0x807 PspCallProcessNotifyRoutines (간접 ×4) · +0x4fa PspCallThreadNotifyRoutines · +0x4dc EtwTraceThread · +0x7d3 EtwTraceProcess;  PspInitializeThunkContext +0x11b~ 알림 루프` | `[관찰]` 호출 자리 / `[정적]` | 다 | Q39 |
| **33** | 부모용 핸들 만들기·출력 칸 쓰기·정리 — 프로세스 핸들, 스레드 핸들, 만들기 정보 결과(6), 임시 구조 해제 | 커널 (부모 핸들 표·부모 유저 메모리) | 2.1 · ① | K3~P0 | `①… +0x10a7 PspCreateObjectHandle (스레드: PspInsertThread +0x525) · +0x1129 PspUpdateCreateInfo · +0x11d5 PspDeleteCreateProcessContext · +0x11e6 FsRtlFreeExtraCreateParameterList` | `[정적]` | 나 | 없음 |
| **34** | 스레드를 CPU 대기 줄에 세우기 (`KeReadyThread` — 멈춘 스레드도) | 커널 | 2.2 · ① | K3~P0 | `PspInsertThread +0x5a6 KeReadyThread` | `[정적]` | 가·나 | Q19·Q42 |
| **35** | 커널 쪽 조건 갈래 묶음 — 복제(fork) 길, WOW64, Pico·본뜨기 스레드, 보안 스레드, 잡 배정, 최소 프로세스, 실패 정리. test.exe 는 안 탄 걸로 보임 | 커널 | 2.1 · ① | — | `PspAllocateProcess +0x2113 (복제) · +0xffb PspWow64PickBestNtdll;  MmCreatePeb +0x680 MiInitializeWowPeb;  PspAllocateThread 먼 덩어리(+0x2ca…)·+0x9eb PspWow64SetupUserStack · +0xb7d PspWow64InitThread;  PspInsertThread +0x8ef PspAssignProcessToJobList · +0x93d PspCreateSecureThread;  PspUserThreadStartup +0xca·+0xe3·+0xf5` | `[정적]` ("안 탔다" 는 일부만 `[관찰]`: TEB 새로 만듦, 핸들 4) | 다 | Q35·Q38·Q39 |

#### B. 부모 마무리 (K3 → P0 사이, 부모·csrss)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 (지도 기준) | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|
| **36** | 부모 쪽 사이 검사·후처리 (조건부) — 이미지 검사 12곳, 상승 검사, 잡 연결, 패키지 앱 후처리 | 다른 프로세스 (부모 유저) | 2.2 | K3~P0 | `CreateProcessInternalW +0x3f91 BasepCheckWebBladeHashes · +0x3fd1 BasepIsProcessAllowed · +0x40c4 BasepCheckWinSaferRestrictions · +0x47c1 BaseCheckElevation · +0x55f1 AssignProcessToJobList · +0x59cd BasepPostSuccessAppXExtension` | `[정적]` | 다 | Q19 |
| **37** | 부모가 앱 호환 데이터를 자식 메모리에 써 넣기 (자식 PEB `pShimData` 후보 `[추측]`) — 활성화 컨텍스트 데이터는 csrss 의 SxS 가 쓸 수도 있음 | 다른 프로세스 (부모 유저 → 자식 유저) | 2.2 | K3~P0 | `CreateProcessInternalW +0x5755 NtAllocateVirtualMemory · +0x5795·+0x57d7·+0x5831 NtWriteVirtualMemory · +0x597d BasepInitAppCompatData` | `[정적: 이름]`+`[관찰]` (구간) | 가·나 | Q78 (7.8) — 단 부모 KERNELBASE 쪽이라 새 할 일 |
| **38** | csrss 에 "프로세스 만들었다" 알림 — 부모가 `CsrClientCallServer`(0x1001d) → csrss 가 새 프로세스·스레드 핸들을 직접 열고 자기 목록에 등록 | 다른 프로세스 (csrss) + 커널 (csrss 의 핸들) | 2.2 | K3~P0 | `CreateProcessInternalW +0x4672 CsrClientCallServer ┄▶ (csrss) csrsrv!CsrApiRequestThread +0x4e6 → basesrv!BaseSrvCreateProcess2 → InternalBaseSrvCreateProcess +0x17c CsrCreateProcess → CsrpCreateProcess +0x50c CsrInsertProcess` | `[정적]` (실행은 `[공백]`) | 나 | Q19(낮음)·Q66(b) |
| **39** | 부모가 첫 스레드 풀어 주기 — `NtResumeThread` → `PsMultiResumeThread`(`ProcessFirstResume` 켬) → `KeResumeThread`(`SuspendCount` −1) → `KiResumeThread`(`SuspendEvent` 1, 깨우기) | 커널 (부모 스레드) | 2.2 · ① | K3~P0 | `CreateProcessInternalW +0x5bd3 NtResumeThread(SYSCALL 0x52) → PsMultiResumeThread +0x4e → KeResumeThread +0x68 → KiResumeThread +0x20 · +0xab KiTryUnwaitThread` | `[정적]` (구간 `[관찰]`) | 가 | Q19·Q64 |

#### C. test.exe 첫 스레드 커널 쪽 (⓪)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 (지도 기준) | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|
| **40** | 멈춘 채 첫 CPU 에 올라 → IRQL 내림 → 멈춤 APC 를 처리하며 잠들었다 깨기 (사진 P0 에서 `SuspendCount` 는 이미 0) | 커널 | 2.2·2.3 · ① | K3~P0 | `SwapContext 끝 RET → KiStartUserThread → PspUserThreadStartup +0x22 → KiApcInterrupt +0x3ab → KiDeliverApc +0x220 → KiSchedulerApc +0xf7 → KeWaitForSingleObject(SuspendEvent)` | `[정적]` (실제로 잤는지 `[공백]`) | 가 | Q42·Q19 |
| **41** | 스레드 시작 코드 잡일 — 주 토큰 교환 막기, 세션 로케일 → TEB, 스케줄링 속성 → TEB, 스레드 만들기 알림 | 커널 | 2.3 · ① | P0~P1 | `KiStartUserThread → PspUserThreadStartup +0x3c PspDisablePrimaryTokenExchange · +0x78 PsGetSessionLocaleId · +0x99 KeUpdateTebSchedulingPropertiesCurrentThread · +0xa1 PspNotifyThreadCreation` | `[정적]`+`[관찰]` | 가 | Q78·Q42 |
| **42** | 원래 시작 문맥을 유저 스택에 맡기고, 트랩 프레임을 `LdrInitializeThunk` 로 바꿔 끼우기 | 커널 (유저 스택에 씀) | 2.3 · ① | P0~P1 | `PspUserThreadStartup +0xb3 PspInitializeThunkContext +0x16d PspGetContextThreadInternal · +0x285 RtlCopyContext · +0x29a KePopulateContinuationContext · +0x2db PspSetContextThreadInternal → PspSetContext +0x68` | `[정적]`+`[관찰]` | 가·나 | Q42·Q29·Q57·Q75 |
| **43** | 커널 → 유저 첫 번째 `IRETQ` | 커널 → 유저 | 2.3 · ① | P0~P1 | `KxStartUserThread 끝 조각 +0x9c IRETQ` 또는 `KiKernelExit +0xa5` (`KiKvaShadow` 로 갈림) | `[정적]` | 다 | Q42 |

#### D. ntdll 입구와 `LdrpInitializeProcess` 앞쪽 (P1 → P1a)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 (지도 기준) | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|
| **44** | ntdll 입구 잡일 — 보안 쿠키, 보안 프로세스 표시, 특수 프레임 표시, CPU 기능 표 (끝쪽에서 `RtlInitializeProcessorFeaturesBitMap`·`SymCryptDetectCpuFeaturesByCpuid` 로 한 번 더) | 유저 | 2.4 · ② | P1~P1a (쿠키) / P1b~P2 (CPU 기능 끝쪽) | `LdrInitializeThunk +0xe → LdrpInitialize +0x41 InitSecurityCookie · +0x27 InitSpecialMachineFrames · +0x2c __cpu_features_init;  ②… +0x1766 RtlInitializeProcessorFeaturesBitMap · +0x1d9e RtlIsProcessorFeaturePresent · +0x1df4 SymCryptDetectCpuFeaturesByCpuid` | `[정적]`+`[관찰]` | 가 | Q43·Q80·Q78 |
| **45** | 핫패치 준비 — 처음 온 스레드 판정, 이벤트 만들기, 핫패치 켜짐 확인(`NtManageHotPatch`), 켜졌으면 패치 ntdll 올리기, 준비 끝 표시 | 유저 | 2.4 · ② | P1~P1a | `LdrpInitialize ⇒ LdrpInitializeInternal +0x3b CMPXCHG · +0x93 NtCreateEvent · +0x98 LdrpInitializeHotPatching · +0xc2 LdrpLoadPatchedNtdll · +0xdf LdrpInitializationComplete` | `[정적]` (사진: `LdrpHotPatchInitialized` P1a 에 2) | 가·나 | Q43·Q67(c) |
| **46** | 첫 스레드 판정 · 로더 완료 이벤트 · 로더 락·`ProcessInitializing`·`LdrInitState` · `.mrdata` 준비 (`LdrInitializeMrdata`) | 유저 | 2.4 · ② | P1~P1a | `LdrpInitializeInternal +0x5a _LdrpInitialize +0x3a CMPXCHG · +0xea NtCreateEvent(LdrpInitCompleteEvent) · +0x14a LdrInitializeMrdata` | `[정적]`+`[관찰]` | 가·나 | Q43·Q78 (`LdrInitializeMrdata` 본문) |
| **47** | `PebLdr` 빈 목록 3개·`PEB.Ldr`·난수·NLS(코드 페이지) 정보 — 레지스트리에서 코드 페이지 번호 읽기, NLS 파일 섹션 얻기(`NtGetNlsSectionPtr` ×3) | 유저 + 커널 (syscall) | 2.4 · ② | P1~P1a | `②… +0x158 LdrInitializePerThreadRng · +0x1c1 PEB.Ldr · +0x1cd LdrpInitializeNlsInfo → RtlpQueryNlsSystemCodePages(NtOpenKey·NtQueryValueKey) · RtlpInitCodePageTables(NtGetNlsSectionPtr ×2) · RtlpInitUppercaseTables(NtGetNlsSectionPtr)` | `[정적]` (VAD 11→21 안의 NLS 파일 이름은 run44 `[관찰]`) | 가·나 | 새 할 일 |
| **48** | exe NT 헤더 잡기(`LdrpAppHeaders`) · 보안 프로세스 검사 · 모듈 해시 표 비우기 · ntdll 예외 표를 역함수 표에 등록 · `.mrdata` 열고 닫기 · CFG 강제 여부 · 중요 thunk 기록 | 유저 | 2.4 · ② | P1~P1a | `②… +0x363 RtlImageNtHeaderEx · +0x41a RtlInsertInvertedFunctionTable · +0x446 LdrProtectMrdata · +0x486 LdrpCaptureCriticalThunks · +0x492 LdrControlFlowGuardEnforcedWithExportSuppression · +0x3de LdrpHandleUserCopyAttributeList · +0x292 RtlpInitializeNonVolatileFlush` | `[정적]` | 가 | Q44·Q67(a)·Q80 |
| **49** | 실행 옵션(IFEO) 읽기 — 유저 쪽 (레지스트리 열고 읽고 닫기, `LoaderThreads` 덮기) | 유저 | 2.4 · ② | P1~P1a | `②… +0x536 LdrpInitializeExecutionOptions;  +0x8f7 LdrQueryImageFileExecutionOptions;  +0x1785·+0x179e·+0x17b4 NtClose` | `[정적]` | 가 | Q44 |
| **50** | 전역 켜고 끄기 — 핫패치 끄기, `UseWOW64`, .NET 검사, 매개변수 정규화(`RtlNormalizeProcessParams`), LoadConfig 읽기, `LdrpDebugFlags` | 유저 | 2.4 · ② | P1~P1a | `②… +0x587·+0x5f2 · +0x62c LdrpCorFixupImage · +0x6c5 RtlNormalizeProcessParams · +0x6f4 LdrImageDirectoryEntryToLoadConfigEx` | `[정적]` | 가 | Q44·Q63(d) |
| **51** | PEB 의 TLS 비트맵 4칸 · FLS 자리 · 스택 추적 DB 옵션 · `FastPebLock` 만들기 | 유저 | 2.4 · ② | P1~P1a | `②… +0x88c·+0x8a3 RtlSetBits · +0x7c9 RtlInterlockedPushListSList · +0x97e RtlControlStackTraceDataBase · +0x9d2 RtlInitializeCriticalSectionEx` | `[정적]` | 가 | Q44·Q74(c) |
| **52** | 프로세스 힙 만들기 (힙 관리자·힙 본체·태그 힙). 이후 일들이 이 힙에 블록을 놓는다 | 유저 (+ 커널 VAD) | 2.4 · ② | P1~P1a (힙 `25996f30000` `[관찰]`) | `②… +0x9f2 RtlInitializeHeapManager · +0xa50 LdrpInitializeProcessHeap(→ NtAllocateVirtualMemory) · +0xa39 RtlSetHeapInformation · +0xb7d RtlCreateTagHeap` | `[정적: 호출 자리]`+`[관찰]` | 가·나 | Q44 (힙 만들기 반환값 — 동적만)·새 할 일 (정적) |
| **53** | 힙 안 작은 로더 구조 — 로더 스냅 버퍼, 활성화 컨텍스트 스택(TEB 가 가리킴), 예외 로그, 시스템 정보 조회 | 유저 | 2.4 · ② | P1~P1a | `②… +0xace LdrpInitializeSnapsBuffer(RtlAllocateHeap) · +0xb05 RtlInitializeExceptionLog · +0xb12 RtlpInitializeThreadActivationContextStack · +0xb52 NtQuerySystemInformation` | `[정적: 이름·호출]` | 가·나 | 새 할 일 |
| **54** | 스레드 풀 기본 준비와 ETW 등록 — 풀 패키지, 풀 CPU Set, ETW 등록 3곳, 힙 로깅 | 유저 (+ 커널 ETW 등록) | 2.4 · ② | P1~P1a | `②… +0xb88 TpInitializePackage · +0xba7 TpSetDefaultPoolCpuSets · +0xbe7 LdrpEnableUMGLTracingStateSync · +0xbf3 TraceLoggingRegisterEx… · +0xc0b·+0xc28 EtwEventRegister · +0xc2d RtlInitializeHeapLogging` | `[추측: 이름]` | 가·나 | 새 할 일 (Q78 의 `.data` 343개에 섞여 있음) |
| **55** | **환경 블록·매개변수 블록을 힙으로 옮기기** — 환경 복사(`EnvironmentVersion` +1·`Environment`·`EnvironmentSize`), 매개변수 블록 힙 새 블록으로 복사·포인터 9개 새 자리 기준으로·`PEB+0x20` 갈아끼움·옛 영역 `NtFreeVirtualMemory` | 유저 (+ 커널 VAD 삭제) | 2.4 · ② | P1~P1a | `②… +0xc32 RtlpInitEnvironmentBlock · +0xc3f RtlpInitParameterBlock (RtlAllocateHeap → RtlCopyMemory → 포인터 고침 → PEB+0x20 → NtFreeVirtualMemory(MEM_RELEASE))` | `[정적]`+`[관찰]` | 가·나 | Q78 (7.8 첫째 "누가 옮기나" — 이번에 닫음)·Q63(d) |
| **56** | `\KnownDlls` 열기·경로 읽기, 시스템 폴더·DLL 검색 목록 기본 문자열, DLL 정책 | 유저 (+ 커널 객체 열기) | 2.4 · ② | P1~P1a | `②… +0xdbd NtOpenDirectoryObject · +0xe4c NtOpenSymbolicLinkObject · +0xe95 NtQuerySymbolicLinkObject · +0xfca LdrpInitializePolicy · +0xfe9 LdrpIncludeAlternateForwarders · +0x1161 RtlpSystemDirs · +0x1183 LdrpCheckAppDirType` | `[정적]`+`[관찰]` (핸들 `0x4c`) | 가·나 | Q44·Q71(a)·Q80 |
| **57** | ntdll 모듈 항목 만들고 명부에 올리기 (명부·해시·색인·`State` 9·초기화 순서 목록 머리) + (핫패치면) 패치 항목 | 유저 | 2.4 · ② | P1~P1a | `②… +0x11a2 LdrpInitializeNtdllDataTableEntry → LdrpAllocateModuleEntry · LdrpInsertDataTableEntry · LdrpInsertModuleToIndex · LdrpProcessMappedModule;  +0x127c RtlInitializeHistoryTable` | `[정적]` (항목 위치 힙 `[관찰]`) | 가 | Q44·Q45(끝)·Q67 |
| **58** | 병렬 로딩 지원 준비 (이벤트 2·락·큐·재시도 큐) · DLL 검색 경로 빈 구조 · 현재 폴더 | 유저 (+ 커널 이벤트) | 2.4·2.6 · ② | P1~P1a | `②… +0x12e3 LdrpInitParallelLoadingSupport · +0x1307 LdrpDrainWorkQueue · +0x1318 LdrpInitializeDllPath · +0x151c RtlpInitCurrentDir` | `[정적]` | 가·나 | Q44·Q46 |
| **59** | exe 모듈 항목 만들고 명부 맨 앞에 올리기 (`LdrpImageEntry`) + (헤더 `ImageBase` ≠ `PEB.ImageBaseAddress` 일 때만) exe 직접 재배치 | 유저 | 2.4 · ② | P1~P1a | `②… +0x135e LdrpAllocateModuleEntry → +0x1455 LdrpInsertDataTableEntry → +0x14d2 LdrpInsertModuleToIndex → +0x14ee LdrpProcessMappedModule;  (조건) +0x1435 LdrpProtectAndRelocateImage` | `[정적]` | 가·나 | Q44·Q46·Q61·Q67(b) |
| **60** | 스케줄러 공유 데이터 슬롯 얻기 — 커널에서 슬롯 표를 받아 TEB `+0x1850` 에 걸기 | 유저 + 커널 (syscall) | 2.7 · ② | P1~P1a | `②… +0x182c LdrpAllocateSchedulerSharedData · +0x183c LdrpAcquireSchedulerSharedDataSlot → NtSetInformationThread(0x39) ⇒ 커널 PspSchedulerSharedDataRegionSlotAllocate` | `[정적]`+`[관찰]` | 가·나 | Q74(c)(d)·Q78 |
| **61** | CFG 호출 검사 도우미 연결 — CFG 켜져 있으면 `.mrdata` 의 함수 포인터 둘에 `LdrpValidateUserCallTarget`·`LdrpDispatchUserCallTarget` 넣기 | 유저 | 2.4 · ② | P1~P1a | `②… +0x180b LdrpInitializeCfgScpHelpers (LdrProtectMrdata 열고 닫기)` | `[정적]` | 가 | 없음 (이번에 닫음) |
| **62** | exe TLS 준비 — TLS 디렉터리 읽기, 목록 항목, exe `TlsIndex`, TLS 벡터 | 유저 | 2.7 · ② | P1~P1a | `②… +0x18d0 LdrpInitializeTls → LdrpAllocateTlsEntry · LdrpAllocateTls` | `[정적]` | 가·나 | Q16(끝)·Q25 |
| **63** | import 리다이렉션 준비 — 매개변수 `RedirectionDllName` 이 비어 있지 않을 때만 DLL 올리고 표 만듦 (sample 은 비어 있어 안 탐) | 유저 | 2.4 · ② | P1~P1a | `②… +0x1963 LdrpInitializeImportRedirection (조건: +0xd0 LdrpLoadDll · +0xf1 LdrpBuildImportRedirection)` | `[정적]`+`[관찰]` | 다 | 없음 (이번에 닫음) |
| **64** | ntdll 쪽 조건 갈래 묶음 — AppVerifier, WOW64, .NET, shim 엔진, 핫패치 데이터, 디버거 멈춤, 복제 프로세스 길. test.exe 는 안 탄 걸로 보임 | 유저 | 2.4 · ② | — | `②… +0x157c~+0x1718 (AppVerifier) · +0x17c6 LdrpLoadWow64 · +0x1852 LdrpCorInitialize · +0x19f4 LdrpInitShimEngine · +0x1b29 LdrpInitializePatchData · +0x1b40 LdrpDoDebuggerBreak;  _LdrpInitialize +0x354·+0x284` | `[정적]` + 안 탐 `[관찰]` (`UseWOW64`·`AvrfAppVerifierMode`·`g_pfnSE_InstallAfterInit` 0) | 다 | Q46·Q48 |

#### E. kernel32·kernelbase 준비 (P1a → P1b 앞부분)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 (지도 기준) | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|
| **65** | kernel32·kernelbase 올리기 — `Subsystem` 2/3 이면 ntdll 이 직접 `LdrLoadDll("KERNEL32.DLL")` (kernelbase 는 kernel32 import 로 따라 옴 `[추측]`), 이어서 `LdrLoadDll("KERNELBASE.dll")` | 유저 + 커널 (syscall) | 2.5 · ② | P1a~P1b | `②… +0x19a2 LdrpInitializeKernel32Functions +0xa4 LdrpFindLoadedDllInternal · +0xc0 LdrLoadDll → LdrpLoadDll +0x100 → LdrpLoadDllInternal (안쪽은 일 70~79) · +0x156 LdrLoadDll` | `[관찰]` 호출 자리 / `[정적]` | 가·나 | Q20·Q49·Q10(끝)·Q11(끝) |
| **66** | kernel32·kernelbase 에서 이름으로 함수 주소 찾기·부르기 — `BaseThreadInitThunk`(→ `Kernel32ThreadInitThunkFunction`), `TermsrvGetWindowsDirectoryW`, `KernelbasePostInit`(간접 호출), `LdrpSnapKernelBaseExtensions`, `LdrpCodeAuthzInitialize` | 유저 | 2.5 · ② | P1a~P1b | `LdrpInitializeKernel32Functions +0xff·+0x130·+0x19b LdrGetProcedureAddressForCaller · +0x135 LdrpSnapKernelBaseExtensions · +0x165 LdrpCodeAuthzInitialize · +0x1af (간접) KernelbasePostInit` | `[정적]`+`[관찰]` (`Kernel32ThreadInitThunkFunction` 값) | 가·나 | Q48·Q49·Q59 |
| **67** | KERNELBASE·KERNEL32 시작 함수가 하는 일 — csrss 연결(`CsrClientConnectToServer`), 콘솔 연결(`ConsoleInitialize`), NLS·메모리 관리자 준비, 태그 힙, 스레드 풀 시작 함수 등록, 보호 정책 | 유저 (+ 다른 프로세스 csrss 연결) | 2.5 · ② | P1a~P1b | `LdrpInitializeNode → LdrpCallInitRoutine → LdrpCallInitRoutineInternal → KERNELBASE!KernelBaseDllInitialize → _KernelBaseBaseDllInitialize +0x2d9 CsrClientConnectToServer · +0x526 ConsoleInitialize;  KERNEL32!BaseDllInitialize → _BaseDllInitialize` | `[정적: 이름]`+`[관찰]` (구간) | 가·나 | Q21·Q22·Q78(7.8) — 안쪽은 새 할 일 |

#### F. 병렬 로딩과 exe import (P1a → P1b)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 (지도 기준) | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|
| **68** | 병렬 로딩 켜기와 일꾼 스레드 둘 — 풀·일감 개체 만들기(`LoaderThreads` 0 → 스레드 4 → 최대 3), 일꾼 스레드가 커널에서 만들어져 `LdrInitializeThunk` 로 들어옴 | 유저 + 커널 | 2.4·3장 · ② | P1a~P1b | `②… +0x1a47 LdrpEnableParallelLoading → TpAllocPoolInternal · TpAllocWork (→ 커널 NtCreateThreadEx → PspCreateThread → PspAllocateThread … PspInsertThread;  일꾼: PspUserThreadStartup → PspInitializeThunkContext → LdrInitializeThunk → LdrpInitializeInternal → _LdrpInitialize +0xa7 LdrpInitializeThread → … → TppWorkerThread)` | `[관찰]` (스레드 3개)+`[정적]` | 가·나 | Q47·Q79 |
| **69** | exe 의 import 를 올리고 연결하는 바깥 고리 — import 표 준비(IAT 쓰기 가능), 서술자마다 DLL 하나 맡기기, 다 끝나면 snap. 자식 DLL 은 같은 함수가 다시 불림 | 유저 | 2.6 · ② | P1a~P1b | `②… +0x1a8e LdrpThreadTokenSetMainThreadToken · +0x1aa9 LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal · LdrpSnapModule · LdrpQueueWork · +0x1ab1 LdrpDrainWorkQueue(1)` | `[정적]` | 다 | Q14(끝)·Q23 |

#### G. DLL 하나를 올리는 공통 부품 (kernel32·kernelbase: `LdrLoadDll` 길 / dll1·dll2: import 길)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 (지도 기준) | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|
| **70** | 이름 다듬기·이미 있나 찾기 — ApiSet 풀기, 경로 전처리, 해시 표 조회, 빠른 끝 | 유저 | 2.5·2.6 · ② | P1a~P1b | `LdrpLoadDll +0xc2 LdrpPreprocessDllName;  LdrpLoadDllInternal +0xb8 LdrpFastpthReloadedDll · +0x1f2 LdrpFindOrPrepareLoadingModule;  LdrpLoadDependentModuleInternal +0x2ff ApiSetpResolveHost` | `[정적]` | 다 | Q20·Q23 |
| **71** | DLL 파일 찾기 (파일 길) — 검색 경로 목록 짓기, 후보 파일 있나 확인 | 유저 + 커널 (파일 조회) | 2.6 · ② | P1a~P1b | `LdrpProcessWork +0x6a LdrpMapDllSearchPath +0x131 LdrpSearchPath → LdrpComputeLazyDllPath → RtlpComputePath;  LdrpResolveDllName → NtQueryAttributesFile` | `[정적]` | 가·나 | Q53(끝)·Q71·Q23 |
| **72** | DLL 섹션 얻기 — `\KnownDlls` 섹션 열기, 또는 파일 열기 + `NtCreateSection(SEC_IMAGE)` | 유저 호출 → 커널 객체 | 2.5·2.6 · ② | P1a~P1b | KnownDll: `LdrpFindOrPrepareLoadingModule +0x5e5 LdrpLoadKnownDll → LdrpFindKnownDll +0xd9 NtOpenSection`;  파일: `LdrpMapDllSearchPath +0x25c LdrpMapDllNtFileName +0x137 NtOpenFile · +0x194 NtCreateSection · +0x2a0·+0x2a9 NtClose` | `[정적]` (어느 길을 탔는지 `[공백]`) | 나 | Q20·Q23·Q62(f)·Q79 |
| **73** | DLL 메모리에 붙이기 (+ 맵 결과가 `0x40000003` 일 때만 재배치) | 유저 호출 → 커널 | 2.6 · ② | P1a~P1b | `LdrpMapDllWithSectionHandle +0x4e LdrpMinimalMapModule +0x174 NtMapViewOfSection (기준 주소 칸 = 항목 DllBase);  LdrpCompleteMapModule +0x31b LdrpRelocateImage` | `[정적]` (붙은 주소는 `[관찰]`) | 가·나 | Q20·Q23·Q52(끝)·Q62(f)·Q70 |
| **74** | 새 모듈 항목·그래프 노드 만들고 명부·해시·색인에 올리고 마무리 | 유저 | 2.6 · ② | P1a~P1b | `LdrpMapDllWithSectionHandle +0x146 LdrpInsertDataTableEntry · +0x151 LdrpInsertModuleToIndexLockHeld · +0x26c LdrpProcessMappedModule;  LdrpFindOrPrepareLoadingModule +0x47a / LdrpLoadDependentModuleInternal +0x115c LdrpAllocateModuleEntry` | `[정적]`+`[관찰]` | 가 | Q72·Q54(끝)·Q80 |
| **75** | 의존 간선 쓰기 + 부모 카운터 (그래프 만들기) | 유저 | 2.6 · ② | P1a~P1b | `LdrpLoadDependentModuleInternal +0xf69 · +0xf8b (간선 칸 0x20B 를 부모 노드·자식 노드 두 목록에) · +0x12df 부모 카운터` | `[정적]` | 가·나 | Q72(b) |
| **76** | 일감 넣기·꺼내기·일꾼 — 큐, 책임자, 부모 카운터 줄이기, 메인 스레드 토큰 가장 | 유저 (메인 + 일꾼) | 2.6 · ② | P1a~P1b | `LdrpLoadDependentModuleInternal +0x182c LdrpQueueWork;  LdrpDrainWorkQueue → LdrpProcessWork;  LdrpSignalModuleMapped;  (일꾼) TppWorkpExecuteCallback → LdrpWorkCallback` | `[정적]` (어느 스레드인지 `[공백]`) | 가·나 | Q47 |
| **77** | IAT 연결(snap) — import 서술자마다 상대 DLL export 에서 주소 찾아 IAT 칸에 쓰기, forwarder, 끝맺음(IAT 보호 되돌리기) | 유저 (+ 커널 보호 변경) | 2.6 · ② | P1a~P1b | `LdrpProcessWork +0x131 / LdrpMapAndSnapDependency +0x4c2 LdrpSnapModule +0x3e7 (IAT 칸 쓰기) · +0x5df (forwarder) · +0xb66 LdrpDoPostSnapWork` | `[정적+샘플]` | 가·나 | Q50(끝)·Q68·Q69 |
| **78** | DLL 의 TLS 목록 항목 만들기 | 유저 | 2.7 · ② | P1a~P1b | `LdrpDoPostSnapWork +0x93 LdrpHandleTlsData → LdrpAllocateTlsEntry · LdrpAllocateTls` | `[정적]` | 가·나 | Q16(끝)·Q68 |
| **79** | 그래프 정리·실행 준비 — 고리 합치기, 알림, 로드 문맥·경로 해제 | 유저 | 2.7 · ② | P1a~P1b | `LdrpPrepareModuleForExecution +0x12f LdrpCondenseGraph → LdrpCondenseGraphRecurse → LdrpMergeNodes · +0x7b LdrpNotifyLoadOfGraph;  ②… +0x1be7 LdrpFreeLoadContextOfNode · +0x1c00 LdrpReleaseDllPath` | `[정적]` | 가 | Q15(끝)·Q24 |

#### H. 켜기와 은퇴 (P1b → P2)

| 번호 | 일 | 공간 | 단계 | 구간 | 대표 함수 사슬 (지도 기준) | 태그 | 분류 | 기존 대기 |
|---|---|---|---|---|---|---|---|---|
| **80** | 켜기 직전 준비 — 사용자 Windows 폴더(검색 목록 다시 씀), `Kernel32ThreadInitThunkFunction(1,0,0)` 간접 호출, 로더 락, 첫 스레드 문맥 주소 걸기 | 유저 | 2.4 · ② | P1a~P1b | `②… +0x1b83 LdrpProcessInitContextRecord · +0x1b5e (간접) Kernel32ThreadInitThunkFunction · +0x1b77 LdrpInitializePerUserWindowsDirectory · +0x1b96 LdrpAcquireLoaderLock` | `[정적]` | 가 | Q46·Q48·Q59·Q71(a) |
| **81** | 그래프 켜기 — 자식 먼저, 모듈마다 `State` 8 → 9, 초기화 순서 목록 꼬리에 끼우기 | 유저 | 2.7 · ② | P1b~P2 | `②… +0x1bbb LdrpInitializeGraphRecurse → +0x6a LdrpInitializeNode` | `[관찰]` 호출 자리 / `[정적]` | 가 | Q22·Q24·Q54(끝) |
| **82** | TLS 콜백 부르기 — `LdrpCallTlsInitializers` → `ImageTlsCallbackCaller` → `Dll2Tls`·`Dll1Tls` (락 소유 칸 표 쓰기 포함) | 유저 (사용자 코드 호출) | 2.7·2.8 · ② | P1b~P2 | `LdrpInitializeNode +0x16b LdrpCallTlsInitializers → ImageTlsCallbackCaller → Dll2Tls / Dll1Tls` | `[관찰]` 호출 자리 / `[정적]` | 가 | Q25·Q26·Q56(끝)·Q74 |
| **83** | 진입점 부르기 — `LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain` (kernel32 준비 때는 KERNELBASE·KERNEL32 시작 함수 → 일 67) | 유저 (사용자 코드 호출) | 2.7·2.8 · ② | P1b~P2 | `LdrpInitializeNode +0x19c LdrpCallInitRoutine → +0x93 LdrpCallInitRoutineInternal → (dllN) _DllMainCRTStartup ⇒ dllmain_dispatch +0x91 → DllMain` | `[관찰]` 호출 자리 / `[정적]` | 가 | Q21·Q27·Q55(끝)·Q73 |
| **84** | exe TLS 콜백 부르기 (활성화 컨텍스트 켜고 끄기 포함) | 유저 (사용자 코드 호출) | 2.9 · ② | P1b~P2 | `②… +0x1c8a RtlActivateActivationContextUnsafeFast · +0x1c99 LdrpCallTlsInitializers · +0x1ca7 RtlDeactivateActivationContextUnsafeFast` | `[관찰]` 호출 자리 / `[정적]` | 다 | Q28 |
| **85** | 로더 끝 잡일과 `_LdrpInitialize` 마무리 — shim 후처리(간접), `PostProcessInitRoutine`(간접), SMT 지연 잠 설정, 핸들 닫기, 완료 신호·`LdrInitState` 3·`ProcessInitializing` 끔 | 유저 | 2.4·2.10 · ② | P1b~P2 | `②… +0x1ce4 (간접) g_pfnSE_InstallAfterInit · +0x1d1b (간접) PEB.PostProcessInitRoutine · +0x1c2a LdrpInitializeSmtDelayedSleep · +0x1d2d·+0x1d3c NtClose;  _LdrpInitialize +0x1a1 LdrpTouchThreadStack · +0x252 LdrpInitializationComplete · +0xbe NtTestAlert` | `[정적]`+`[관찰]` (값) | 가 | Q48 |
| **86** | 은퇴 — 맡긴 문맥으로 `NtContinue`: 유저 APC 확인, 트랩 프레임 덮기, `IRETQ` → `RtlUserThreadStart` | 유저 → 커널 → 유저 | 2.10 · ② 끝 | P2 (입구) 이후 — 사진 없음 | `LdrInitializeThunk +0x18 NtContinue(SYSCALL 0x43) → nt!NtContinue ⇒ NtContinueEx → KiContinueEx +0xc9 KeTestAlertThread → KiContinuePreviousModeUser +0x422 KeContextToKframes → IRETQ → RtlUserThreadStart` | `[관찰]` 입구 / `[정적]` 안쪽 | 가 | Q29·Q57·Q75 (경계 뒤: Q30·Q31·Q60) |

#### 1.1 요청서 예시 → 일 번호 (빠진 예시가 없나 확인용)

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
| 재배치 (4 섹션 만들 때 진입점 델타 · 16 진입점 보정 · 59 exe 직접 (조건) · 73 DLL (조건)) | 4 16 59 73 |
| IAT 연결 (snap) | 77 |
| 의존 그래프 | 75 79 81 |
| 일꾼 스레드 | 68 76 |
| TLS 준비·콜백 | 62 78 82 84 |
| DllMain 부르기 | 83 |
| 매개변수 블록 힙으로 옮기기 (run44) | 55 |
| NtContinue | 86 |

- 예시엔 없었는데 근거 문서에서 더한 일: 파일 열기 3 · 서명 검사 5 · 토큰 7 · 세션·쿼터 10 · 완화 정책 13 · 핫패치 45 · NLS 47 · 스레드 풀·ETW 54 · `\KnownDlls` 56 · 병렬 로딩 지원 58 · 스케줄러 슬롯 60 · CFG 도우미 61 · 조건 갈래 35·64 · 부모 사이 검사 36 · 부모 앱 호환 데이터 37.
- 범위 밖으로 둔 것: 종료 (`LdrShutdownProcess`)·복제 (`RtlCloneUserProcess` …)·DLL 폴더 API 길 (baggage.md 3장 단계 `—` 줄), 진입점 뒤 (`RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup` → `main`, Q30·Q31·Q60). 일꾼 스레드 둘은 `P1a~P1b` 에 생기는 초기화 일부라 일 68 에 넣었다.

### 2. 분류 요약 (질문 2)

| 분류 | 일 수 | 일 번호 |
|---|---|---|
| 가 만 | 24 | 7 9 13 18 27 31 39 40 41 44 48 49 50 51 57 61 74 79 80 81 82 83 85 86 |
| 가·나 | 45 | 3 4 8 10 11 12 14 15 16 17 19 20 21 22 23 25 26 28 29 30 34 37 42 45 46 47 52 53 54 55 56 58 59 60 62 65 66 67 68 71 73 75 76 77 78 |
| 나 만 | 3 | 33 38 72 |
| 다 | 14 | 1 2 5 6 24 32 35 36 43 63 64 69 70 84 |

- (가) 를 포함한 일 69 · (나) 를 포함한 일 48 · (다) 14. 가·나가 겹치는 45개 때문에 합이 86 보다 크다.
- 사용자의 짐작 "이미지 페이지 안 IAT·재배치 값, 다른 DLL 의 섹션·파일 객체, KERNELBASE·KERNEL32 전역, csrss 쪽 기록" 은 **맞았다** (결과서). 거기에 커널 이웃 객체, 힙 안 블록, 일꾼 스레드 2개의 객체 일체, 핸들 표 항목, 부모 cmd.exe 쪽 기록이 더 나왔다 (아래 3).

#### 2.1 (가) 의 칸을 쓰는 일 — 구조체·전역별 (Q78 이 "일 단위" 로 갈 때 길잡이)

| 구조체·구역 (직접 칸 수) | 쓰는 일 (번호) |
|---|---|
| `_EPROCESS` (263) | 7 8 9 10 13 14 16 23 29 30 39 41 67 |
| `_KPROCESS` (65) | 10 11 12 |
| `_ETHREAD` (149) | 25 30 |
| `_KTHREAD` (237) | 28 30 31 34 39 40 42 60 86 |
| `_KTRAP_FRAME` (66) | 28 42 86 |
| `_HANDLE_TABLE` (17) | 12 21 29 (핸들을 새로 만드는 일 45·46·56·58·67 등도 `HandleCount` 류 칸을 바꿀 것 `[추측]`) |
| `_MMVAD`·`_MMVAD_SHORT` (23) | 15 16 17 19 26 37 47 52 55 60 73 |
| `_TOKEN` (55) | 7 |
| `_SECTION`·`_CONTROL_AREA`·`_SEGMENT`·`_SECTION_IMAGE_INFORMATION` (63) | 4 (DLL 쪽 같은 타입은 일 72 — 체크리스트 밖) |
| `_FILE_OBJECT` (30) | 3 23 |
| `_PEB` (124) | 18 19 20 37 46 47 51 52 55 67 85 |
| `_RTL_USER_PROCESS_PARAMETERS` (40) | 20 21 49 55 67 |
| `_TEB` (137) | 27 41 46 53 60 62 76 82 |
| `_PEB_LDR_DATA` (9) | 47 57 59 74 81 |
| `_LDR_DATA_TABLE_ENTRY` (65) | 57 59 73 74 78 81 83 |
| `_LDR_DDAG_NODE` (10) | 57 59 74 75 77 79 81 |
| `_KUSER_SHARED_DATA` (103) | — (쓰는 일 없음. 일 15 가 VAD 만 만듦. 값은 부팅 때 `[추측]`) |
| ntdll `.data`·`.mrdata`·`.00cfg` (493) | 22 44 45 46 48 49 50 51 52 54 56 57 58 61 62 66 68 71 76 78 80 81 82 85 |

- 가장 많은 일이 걸리는 곳: ntdll 전역 (24개 일) · `_EPROCESS` (13) · `_PEB` (11) · `_MMVAD` 계열 (11) · `_KTHREAD` (9). `_TOKEN` (일 7) 과 `_SECTION` 계열 (일 4) 은 일이 하나라 Q78 이 한 번에 끝낼 수 있다.
- 칸 이름이 "모름" 인 곳이 많은 구조체는 일 번호가 곧 **읽을 함수 묶음**이다. 예: `_EPROCESS` 모르는 칸은 일 8 ~ 14 의 `PspAllocateProcess` 안 callee 를 읽으면 대부분 닿는다 (결과서).

### 3. (나) 체크리스트 밖 짐 — "남는 곳" C1 ~ C12 와 사용자 결정

결과서 3장은 (나) 를 **일이 아니라 남는 곳**으로 묶었다 (같은 곳에 여러 일이 쓴다). "덤프로 읽힐지" 는 모두 결과서의 `[추측]` 이다 — 읽어 본 것이 아니다. 오른쪽 끝 열은 사용자 결정 (2026-10-26, plan.md 결정 기록).

| 번호 | 어디에 남나 | 관련 일 | 기존 덤프로 `[추측]` | PDB 칸 표 `[정적]` | 사용자 결정 |
|---|---|---|---|---|---|
| C1 | 커널 이웃 객체: `_SUBSECTION`·`_MSUBSECTION`·`_SECTION_OBJECT_POINTERS`·`_OBJECT_HEADER`·`_HANDLE_TABLE_ENTRY`·`_MI_SECTION_IMAGE_INFORMATION`·`_KEXCEPTION_FRAME`·`_MMSUPPORT_FULL` | 3 4 8 12 15 21 28 29 30 | 읽힘 (커널 풀) | **있음** (8개 모두 정의 레코드) | 기록만 (baggage 7.6) |
| C2 | PDB 에 정의가 없는 타입: `_LDRP_LOAD_CONTEXT`·`_EPROCESS_QUOTA_BLOCK` (정방향 선언만), 간선 칸 0x20B, `InitialTeb`, `LdrSystemDllInitBlock` | 10 22 26 75 76 | 읽힘, 칸 이름은 모름 (오프셋으로만) | **없음** | 기록만 |
| C3 | 커널 전역 목록·표 안 항목: `PspCidTable` PID·TID 항목, `PsActiveProcessHead` 와 이웃 Flink, CPU 준비 큐, 쿼터 블록·세션 참조 수 | 10 23 25 29 34 | 읽힘 (전역 이름으로) | 있음 | **Q79** 에 붙임 |
| C4 | 페이지 표·PFN (PML4, 프로토 PTE) | 11 15 16 17 73 | 읽힘 (DirBase 물리 주소) | 있음 (`_MMPTE`·`_MMPFN`) | 기록만 |
| C5 | 이미지 페이지 안 값: IAT 칸 (test.exe 91 · dll1 89 · dll2 88 · kernel32 약 1,274 · kernelbase 약 750), 재배치된 값, `.data` 처음 값 | 16 17 59 73 77 | P1b·P2 는 읽힘, 그 전은 `????` 일 수 있음. 기대값은 PE 파일로 계산 | 해당 없음 (import 표 순서) | **새 Q82** (IAT 기대값 대조, 정적) |
| C6 | 다른 모듈 (dll1·dll2·kernel32·kernelbase) 의 섹션·컨트롤 영역·파일 객체·VAD 노드 | 15 72 73 | 읽힘 (VAD → `Subsection` → `ControlArea` → `FilePointer`) | 있음 (표 이미 있음) | **Q79** 를 넓힘 |
| C7 | 프로세스 힙 안 블록: `_HEAP`, 새 매개변수·환경 블록, 스냅 버퍼, 활성화 컨텍스트 스택, 경로 캐시, TLS 항목·벡터, 간선 칸, 로드 문맥, 스레드 풀, ETW 등록 | 52 53 54 55 62 71 75 76 78 | 메모리엔 있음. 오프라인 `!heap` 이 되는지 모름 | 일부 있음 (`_HEAP` 0x2c8 등) | 기록만 (+ 동적 시험 **Q86**) |
| C8 | 칸이 아닌 유저 구역: 맡긴 `_CONTEXT`, 유저 스택 (메인 + 일꾼 2), API set·leap second 뷰, NLS 매핑 2개, 스케줄러 공유 데이터 2쪽, shim·활성화 컨텍스트 데이터 VAD 2개, 환경 블록 구역 | 19 20 26 37 42 47 60 68 | 주소는 앎. `_CONTEXT` 는 이미 읽음 `[관찰]`, 나머지 내용 안 읽음 | 있음 (`_CONTEXT` 0x4d0 등) | **Q79** 에 붙임 |
| C9 | 다른 모듈의 전역: KERNELBASE `.data` (공개 데이터 이름 3,125)·KERNEL32 `.data` (197) | 66 67 | P1b·P2 에서 읽힘, P1a 엔 아직 없음 | 공개 이름 목록은 가능, 크기는 짐작 | 기록만 (관련 동적 **Q84**) |
| C10 | 일꾼 스레드 2개의 객체 일체 (`_ETHREAD`·`_KTHREAD`·`_TEB`·트랩 프레임·스택·맡긴 `_CONTEXT`·TLS 벡터) | 68 | 읽힘 (P1b·P2 `ActiveThreads` 3) — 메인 스레드 칸 표 재사용 | 있음 | **Q79** 에 붙임 |
| C11 | 핸들 표 항목과 그 뒤 객체 (이벤트 3, `\KnownDlls`, 콘솔, CSR 포트, 현재 폴더 … — 핸들 4 → 26 → 44 → 46) | 12 21 45 46 47 56 58 67 | 읽힘 (`!handle` 8장 모두 있음) | 있음 | **Q79** 에 붙임 (번호별 생긴 구간 표) |
| C12 | 다른 프로세스: 부모 cmd.exe (핸들 표 항목 2·출력 칸) / csrss (CSR 프로세스·스레드 기록, csrss 핸들 2, 연결 기록) | 33 37 38 67 | 부모: 읽힘. csrss: 문맥 바꿔야 함, 페이지아웃이면 못 읽음 | csrsrv·basesrv PDB 있음 | 부모 → **Q79** / csrss → 기록만 |

- "기록만" 은 baggage.md 7.6 에 줄마다 "구현 범위 정할 때 같이 (사용자 결정 2026-10-26)" 를 붙여 남겼다.
- **Q78 범위는 그대로** (체크리스트 21종 + ntdll 전역, 사용자 결정). 결과서 이유: (나) 는 대부분 Q78 의 방식 (칸 → 쓴 자리) 으로는 안 보인다 — 칸 표가 없거나 (C1·C9), 다른 프로세스·다른 모듈이거나 (C9·C12), 구역이라 칸이 아니어서 (C7·C8).

### 4. 대기 항목과 잇기 (질문 3)

- plan.md 대기열에서 `대기`·`진행 중` 인 항목 41개 (Q78 ~ Q81 제외) 가 **모두 어느 일에 걸린다.** Q30·Q31·Q60 은 "진입점 뒤 (경계)" 라 일 86 뒤에 둔다. 항목마다 걸린 일은 결과서 4.1 표.
- 거꾸로 **구체적인 대기 항목이 없는 일 14개** (Q78·Q79·Q80 만 걸리거나 아무것도 없음): 7 9 10 11 12 14 23 33 37 47 53 54 61 63. 그중 61·63 은 이번에 닫음 (6장).
- 결과서 4.3 "새 할 일" 11개가 어디로 갔나 (사용자 결정 2026-10-26):

| # | 새 할 일 (결과서 4.3) | 관련 일 | 어디로 |
|---|---|---|---|
| 1 | KERNELBASE·KERNEL32 시작 함수 본문 + 두 DLL `.data` 전역 목록 | 66 67 | 기록만 (C9) — 쓰는 순간은 동적 Q84 |
| 2 | 부모 KERNELBASE 가 자식 PEB·메모리에 쓰는 것 | 37 | 동적 **Q83**. 정적 부분 (`BasepInitAppCompatData` 등 본문) 은 따로 정한 곳 없음 |
| 3 | csrss 쪽 기록 | 38 67 | 기록만 (C12 csrss) |
| 4 | 프로세스 힙과 힙 안 작은 구조들 + 스레드 풀·ETW 등록 | 52 53 54 | 기록만 (C7) — 오프라인 시험 Q86 |
| 5 | NLS 길의 커널 쪽 (`nt!NtGetNlsSectionPtr`) | 47 | **정한 곳 없음** (다음 바퀴 후보) |
| 6 | 부모용 핸들·출력 칸 (`PspCreateObjectHandle`·`PspUpdateCreateInfo`) | 33 | 덤프 쪽은 Q79 (C12 부모). 정적 쪽은 **정한 곳 없음** |
| 7 | 일꾼 스레드 2개 객체 읽기 | 68 | Q79 (C10) |
| 8 | 다른 DLL 의 섹션·컨트롤 영역·파일 객체 사슬 읽기 | 72 73 | Q79 (C6) |
| 9 | 체크리스트 확장 — 이웃 타입 칸 표 | 4 12 21 28 29 42 52 | 기록만 (C1) |
| 10 | IAT 기대값 계산 | 16 77 | **Q82** |
| 11 | 핸들 번호별 "생긴 구간·종류·이름" 표 | 12 21 45 46 47 56 58 67 | Q79 (C11) |

- 새 대기 항목이 나온 곳:
  - **Q82** IAT 기대값 대조 (정적) ← 결과서 3장 C5 · 제안 4 (새 할 일 10)
  - **Q83** 부모가 자식 PEB 에 쓰는 순간 (동적) ← 결과서 "동적으로 확인" 1 (일 37·38)
  - **Q84** P1a~P1b 쪼개기 (동적) ← "동적으로 확인" 2 (일 67 — `Csr*` 8개·PEB CSR 칸 4개·`ConsoleHandle`·`OwnerProcessId`)
  - **Q85** K1 이전 쪼개기 (동적) ← "동적으로 확인" 3 (일 8 ~ 23, `PspAllocateProcess` `+0x163c`·`+0x2095`·`+0x2314`·`+0x239c`·`+0x2703`)
  - **Q86** 오프라인 `!heap` 시험 (동적) ← "동적으로 확인" 4 (일 52, C7·제안 5)
  - **Q79** 넓힘 ← 결과서 3.2 제안 1 (C3·C6·C8·C10·C11·C12 부모)
- Q78 에서 빠진 두 덩어리 (이번에 닫음): `ProcessParameters` 를 P1→P1a 사이에 힙으로 옮기는 줄, `LdrInitializeMrdata` 본문 (plan.md Q78 줄에 이미 적힘).

### 5. 빠짐 점검 — sast/38 callee 표 637줄 → 일 (질문 4)

| 함수 | 줄 | 일에 든 줄 | 공용 부품 | 실패·정리 길 | 조각 점프 | 간접 호출 |
|---|---|---|---|---|---|---|
| nt 10 함수 (`NtCreateUserProcess` ~ `PspInitializeThunkContext`) | 432 | 279 | 101 | 43 | 9 | 5 |
| ntdll 5 함수 (`LdrInitializeThunk` ~ `LdrpInitializeProcess`) | 205 | 154 | 45 | 6 | 0 | 5 |
| **합계 (15 함수)** | **637** | **433** | **146** | **49** | **9** | **10** |

- **못 넣은 줄 0.** 637줄이 모두 어느 일 또는 공용 부품·정리 길에 들어간다. 줄 수는 `callees_summary.tsv` 와 15 함수 모두 같다 (결과서가 assert 로 확인).
- 조각 점프 9줄은 자기 조각 (`FUN_…`) 으로 가는 JMP 라 호출이 아니다. 간접 호출 10줄 (nt 5 · ntdll 5) 도 일에 넣었다 (대상은 동적으로만).
- `doc=none` (문서 어디에도 없던 callee) **57개** (nt 43 + ntdll 14) 도 전부 어느 일에 들어가거나 공용 부품이다. callee 표 때문에 **새로 세운 일은 3개** (53·54·61, 모두 ntdll `doc=none` 이름).
- 거꾸로 **callee 표에 0줄인 일 19개**: 1 17 36 37 38 39 43 66 67 70 71 72 73 74 75 77 78 82 83 — 부모 쪽·csrss, 커널 두 단계 아래, 커널→유저 전환, kernel32 준비 안쪽, DLL 올리기 공통 부품, TLS 콜백·진입점 부르기. callee 표 (1단계) 만 믿고 일을 세면 이만큼 빠진다 → flow.md 트리를 같이 봐야 한다.
- "이름만 아는" 일이 많다 (태그 `[추측: 이름]`): 일 7 ~ 14 (`PspAllocateProcess` 안 묶음), 53 ~ 54. (가) 로 나눴어도 칸은 모른다 — Q78 몫.
- 공용 부품 146줄 (`memset` 25 · `LdrpLogInternal` 10 · `KiUnstackDetachProcess` 9 · `__security_check_cookie` 8 · `ObfDereferenceObject` 7 · `KiStackAttachProcess` 7 · `RtlAppendUnicodeStringToString` 7 · 기능 스위치 `Feature_*`·`wil_details_*` 11 …) 은 결과서 5.4 표 — baggage.md 6장 "공용 부품" 후보 첫 판.

### 6. 본문을 읽어 닫은 빈 곳 6건 (`[정적]`, 결과서 6장) — 사진과 맞춰 보기

1. **`PEB.ProcessParameters` 를 옮기는 주인 = `RtlpInitParameterBlock`** (일 55, `LdrpInitializeProcess +0xc3f`). run44 가 `[공백]` 으로 남긴 것.
   - 순서: `RtlAllocateHeap(PEB.ProcessHeap, 0, 블록.Length)` (ret `+0x3b`) → 통째 복사 (ret `+0x57`) → 안쪽 포인터 9개를 새 블록 기준으로 (`+0x40 +0x58 +0x68 +0x78 +0xb8 +0xc8 +0xd8 +0xe8 +0x418` = `CurrentDirectory`·`DllPath`·`ImagePathName`·`CommandLine`·`WindowTitle`·`DesktopInfo`·`ShellInfo`·`RuntimeData`·`RedirectionDllName` 의 `Buffer`, 0 이 아닐 때만, `+0x6b ~ +0x106`) → `PEB+0x20` = 새 블록 (`+0x10d`) → `NtFreeVirtualMemory(-1, 옛 블록, 0, MEM_RELEASE)` (ret `+0x130`).
   - 사진과 맞음 `[관찰]` run44: 옛 private 영역 VAD 가 P1→P1a 에 사라짐. 바뀐 포인터 칸 `CurrentDirectory`·`ImagePathName`·`CommandLine`·`WindowTitle`·`DesktopInfo`·`ShellInfo`, 0 이라 그대로인 `DllPath`·`RuntimeData`·`RedirectionDllName`.
   - **`Environment` (`+0x80`) 는 여기서 안 고친다.** 바로 앞 `+0xc32 RtlpInitEnvironmentBlock` 이 **옛 블록** (`PEB.ProcessParameters`) 에서 환경을 별도 힙 블록으로 복사하고 `EnvironmentVersion` (`+0x3f8`) 을 +1 (`+0x86`), `Environment` (`+0x8d`)·`EnvironmentSize` (`+0x3f0`, `+0x94`) 를 쓴다. 그 뒤 통째 복사로 새 블록에 넘어간다.
   - run44 의 `EnvironmentVersion` 0 → 3 중 **+1 만 여기**다. 나머지 +2 는 모른다 (`+0xa0 RtlpSetupEnvironmentHashTable` 쪽일 수 있음 `[추측]`).
2. **`LdrInitializeMrdata` (109바이트) — 일부 닫힘.** `memset` 뒤 `.data` `LdrpMrdataLock` (rva `0x1d4de8`) = 0 (`+0x23`), 시스템 정보 조회 (클래스 0) 가 성공하면 `.mrdata` `LdrpAllocationGranularity` (`0x1e7038`, `+0x46`)·`LdrpMaximumUserModeAddress` (`0x1e7030`, `+0x52`).
   - 사진과 맞음 `[관찰]` (dast/04 P1a 17b): `LdrpMaximumUserModeAddress` = `0x7ffffffeffff`, `LdrpAllocationGranularity` = `0x10000`. `LdrpMrdataLock` 은 끝까지 0* (0 을 쓰는 줄과 맞음).
   - `LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpMrdataUnprotected` 는 **이 함수가 안 쓴다** → 그대로 모름 (baggage 7.5).
3. **CFG 도우미 쓴 자리.** `LdrpInitializeCfgScpHelpers` (일 61, `+0x180b`) 는 CFG 강제가 켜져 있고 ntdll 헤더·LoadConfig 플래그가 맞으면 `LdrProtectMrdata(0)` 으로 `.mrdata` 를 열고 `LdrpGuardCheckIcallNoESFptr` = `LdrpValidateUserCallTarget` (`+0x6b`), `LdrpGuardDispatchIcallNoESFptr` = `LdrpDispatchUserCallTarget` (`+0x79`) 을 쓴 뒤 `LdrProtectMrdata(1)` 로 닫는다.
   - **사진 값은 이 두 함수가 아니다** `[관찰]` (dast/04 17b): `LdrpGuardCheckIcallNoESFptr` = ntdll+`0xfac60` (8장 모두 같음 — 아무것도 안 하는 3바이트 함수, 같은 주소 이름 26개, 후보: `_guard_check_icall_nop`·`RtlInitNlsTables` 등), `LdrpGuardDispatchIcallNoESFptr` = ntdll+`0x166480` (`_guard_dispatch_icall`, P1a ~ P2). `LdrpValidateUserCallTarget` 은 rva `0x1244e0`, `LdrpDispatchUserCallTarget` 은 `0x124580` (`_index.tsv`). → test.exe 에선 쓰는 갈래의 조건이 안 맞았다 `[추측]` (아래 "정적과 동적이 다른 곳").
4. **NLS (일 47).** `LdrpInitializeNlsInfo(PEB)` 가 `PEB.ActiveCodePage` (`+0x34c`, `+0x4e`)·`OemCodePage` (`+0x34e`, `+0x55`) 를 쓴다. 값은 매니페스트 (`LdrpFusionManifestCodePages`) → 없으면 레지스트리 (`RtlpQueryNlsSystemCodePages`: `NtOpenKey`·`NtQueryValueKey`) → 그것도 실패면 `0xfde9` (65001). 매개변수 `Flags` 비트 27 이 켜져 있으면 곧장 `0xfde9` (`+0x77`, 두 칸 한 번에). `UseCaseMapping` (`+0x350`) 은 **읽기만**. 이어서 `RtlpInitCodePageTables` (`NtGetNlsSectionPtr` ×2)·`RtlpInitUppercaseTables` (`NtGetNlsSectionPtr`, 꼬리 JMP) 가 커널에서 NLS 섹션을 받는다.
   - 사진 `[관찰]` (dast/04 구획 10 PEB, 8장 모두): `ActiveCodePage`·`OemCodePage` = `0x3b5` (949), `UseCaseMapping` = `0x6a5` — **K1 부터 이미 같은 값**이고 안 바뀐다. 곧 ntdll 은 같은 값을 다시 쓴 것이고, K1 전에 처음 쓴 쪽은 모른다 `[공백]` (baggage 7.5). run44 의 VAD `C_949.NLS`·`l_intl.nls` 가 이 섹션들일 것 `[추측]` (커널 쪽 본문은 안 읽음).
5. **import 리다이렉션 (일 63) 은 test.exe 에서 안 탄다.** `LdrpInitializeImportRedirection` (`+0x1963`) 은 매개변수 `RedirectionDllName` (`+0x410`) 길이가 0 이 아닐 때만 `LdrpLoadDll` (ret `+0xd5`) 로 DLL 을 올리고 `LdrpBuildImportRedirection` (ret `+0xf6`) 으로 표를 만든다. 사진에서 그 칸은 끝까지 0 `[관찰]` run44. flow.md 2.4·2.5 의 "올리는지 모름" 이 닫혔다.
6. **`PEB.ProcessParameters` (`+0x20`) 를 쓰는 주인은 둘이다**: ① nt `PspCopyAndFixupParameters` (`+0x52b`·`+0x347`, run32) → ② ntdll `RtlpInitParameterBlock` (`LdrpInitializeProcess +0xc3f`, 함수 안 `+0x10d`). baggage 3.1 에 ② 줄을 더했다.

## 확인된 것

- `[정적]` 초기화 동안 nt·ntdll 이 하는 일은 86개로 셀 수 있고, sast/38 callee 표 637줄은 모두 그중 어느 일·공용 부품·정리 길에 들어간다 (결과서 1·5장).
- `[정적]` 86개 중 48개가 짐 지도 체크리스트 (구조체 21종 + ntdll 전역) **밖에도** 짐을 남긴다. 남는 곳은 C1 ~ C12 열두 곳으로 묶인다.
- `[정적]` PDB 타입 정의 확인 (결과서 3.1):
  - 정의 있음 — nt: `_OBJECT_HEADER` 0x38 · `_SUBSECTION` 0x38 · `_MSUBSECTION` 0x98 · `_SECTION_OBJECT_POINTERS` 0x18 · `_HANDLE_TABLE_ENTRY` 0x10 · `_MI_SECTION_IMAGE_INFORMATION` 0x50 · `_KEXCEPTION_FRAME` 0x140 · `_MMSUPPORT_FULL` 0x140 · `_MMPTE` 8 · `_MMPFN` 0x30 · `_OBJECT_DIRECTORY` 0x158 · `_ETW_REG_ENTRY` 0x78 · `_NLS_DATA_BLOCK` 0x18 · `_CONTEXT` 0x4d0 · `_PS_NTDLL_EXPORTS` 0xa8 (+ `_KPRCB` 0xcf00). ntdll: `_HEAP` 0x2c8 · `_HEAP_ENTRY` 0x10 · `_HEAP_SEGMENT` 0x70 · `_HEAP_LOCAL_DATA` 0x30 · `_ACTIVATION_CONTEXT_STACK` 0x28 · `_RTL_ACTIVATION_CONTEXT_STACK_FRAME` 0x18 · `_TP_POOL` 0x1d8 · `_CPTABLEINFO` 0x40 · `_ACTIVATION_CONTEXT_DATA` 0x20 · `_CURDIR`.
  - 정의 없음 (정방향 선언만): `_LDRP_LOAD_CONTEXT`, `_EPROCESS_QUOTA_BLOCK`.
- `[정적]` + `[관찰]` `PEB.ProcessParameters` 를 P1→P1a 사이에 힙으로 옮기는 주인은 `LdrpInitializeProcess +0xc3f RtlpInitParameterBlock`, 환경 블록은 그 앞 `+0xc32 RtlpInitEnvironmentBlock`. 코드 순서와 run44 사진 (VAD 사라짐, 바뀐 포인터 6칸, 0 인 3칸) 이 맞는다.
- `[정적]` + `[관찰]` `LdrInitializeMrdata` 가 쓰는 전역 3개와 P1a 값 (`0x7ffffffeffff`·`0x10000`·0).
- `[정적]` + `[관찰]` `ActiveCodePage`·`OemCodePage` 를 ntdll (`LdrpInitializeNlsInfo`) 이 ② 에서 쓰지만, 값 `0x3b5` 는 K1 부터 이미 있었다 (같은 값을 다시 씀).
- `[정적]` + `[관찰]` import 리다이렉션 길은 test.exe 에서 안 탄다 (`RedirectionDllName` 0).
- 짐 지도 반영: baggage.md 3장에 줄 8개 (PEB 2 · 매개변수 3 · `.data` 1 · `.mrdata` 2) — 모르던 칸 7개가 아는 칸이 됨 (PEB `ActiveCodePage`·`OemCodePage`, `.data` `LdrpMrdataLock`, `.mrdata` `LdrpMaximumUserModeAddress`·`LdrpAllocationGranularity`·`LdrpGuardCheckIcallNoESFptr`·`LdrpGuardDispatchIcallNoESFptr`). 구조체 아는 칸 181 → 183, 전역 54 → 59.

## 정적과 동적이 다른 곳

- **CFG 도우미 두 칸** (`.mrdata` `LdrpGuardCheckIcallNoESFptr`·`LdrpGuardDispatchIcallNoESFptr`):
  - 정적 (이번 바퀴): `LdrpInitializeCfgScpHelpers` 가 쓰는 갈래를 타면 값은 `LdrpValidateUserCallTarget` (rva `0x1244e0`)·`LdrpDispatchUserCallTarget` (`0x124580`).
  - 사진 (run44 덤프 로그를 이번에 다시 읽음): ntdll+`0xfac60` (아무것도 안 하는 3바이트 함수, K1 부터 그대로)·ntdll+`0x166480` (`_guard_dispatch_icall`, `≤P1a`).
  - 둘 중 하나가 틀렸다기보다 **test.exe 에선 쓰는 갈래의 조건 (CFG 강제 여부 `LdrControlFlowGuardEnforced`, ntdll 헤더·LoadConfig 플래그) 이 안 맞은 것** `[추측]`. 그러면 두 값의 주인은 파일 처음 값 또는 다른 쓰는 곳이다 — `LdrpGuardCheckIcallNoESFptr` 는 `.mrdata` 끝 페이지라 K1 부터 읽혔고 (파일 값 `[추측]`), `LdrpGuardDispatchIcallNoESFptr` 는 앞 페이지라 `≤P1a` (Q80 의 "파일 처음 값과 P1a 값 비교" 로 가려짐). flow.md 4장·baggage 7.5 에 넣었다.

## 정정

- **run44 "161" 두 뜻** (run44 explainer 지적, plan 2026-10-25):
  - run44 문서 요약·baggage 2.1 의 "**전역 못 좁힌 것 161**" = ntdll 전역 493 중 `≤P1a` 156 (`.data` 143 · `.mrdata` 13) + `.00cfg` 읽기 불가 5.
  - baggage 7.1·7.8·flow.md 4장의 "`≤P1a` 161" = `≤P1a` 전역 156 + `PebLdr` 5칸 (`_PEB_LDR_DATA`).
  - 같은 숫자가 두 묶음을 가리켜서 헷갈렸다. 이제 **"161" 은 앞의 뜻 (전역 못 좁힌 것) 에만 쓰고**, 뒤의 것은 "`≤P1a` 전역 156 + `PebLdr` 5칸" 으로 풀어 쓴다. run44 문서·baggage.md·flow.md 를 그렇게 고쳤다 (run44 맨 위 정정 안내).
- **run44 트랩 프레임이 덮이는 시점**: run44 문서 "칸마다 처음 값 구간 — 요약" 아래 줄의 "P1a 부터 … 덮인다" 는 틀렸다. P1 트랩 프레임 `Rip` 이 이미 멈춘 주소 + 1 (`7ffafb741421`) 이므로 **P1 부터** `int3` 트랩 프레임이고, P1a 부터는 시스템 콜 트랩 프레임도 같은 자리에 덮인다 (run44 "디버거 영향"·baggage "쓰는 말" 과 같은 말). run44 문서 그 줄을 정정 표시와 함께 고쳤다.
- run44 "아직 확인 필요" 의 "`ProcessParameters` 를 누가 옮기나 — `+0x6c5 RtlNormalizeProcessParams` 근처 `[추측]`" 은 자리가 달랐다: 옮기는 건 `+0xc3f RtlpInitParameterBlock` 이다 (위 6장 1). `RtlNormalizeProcessParams` 가 하는 일은 그대로 모른다.

## 아직 확인 필요

- **`EnvironmentVersion` 나머지 +2** (0 → 3 중 +1 만 `RtlpInitEnvironmentBlock +0x86`): `RtlpSetupEnvironmentHashTable` (`RtlpInitEnvironmentBlock +0xa0`) 안쪽 후보 `[추측]` — 정적 Q78.
- **`ActiveCodePage`·`OemCodePage` (`0x3b5`) 를 K1 전에 처음 쓴 쪽**: `MmCreatePeb` 후보 `[추측]` (같은 `K1 이전` PEB 칸들이 거기서 나옴) — 정적 Q78 / 순서는 동적 Q85.
- **CFG 도우미 갈래가 test.exe 에서 왜 안 탔나** — `LdrpInitializeCfgScpHelpers` 의 조건 (`LdrControlFlowGuardEnforced` 결과, 헤더·LoadConfig 플래그) 의 실제 값. 그리고 `LdrpGuardDispatchIcallNoESFptr` 의 `≤P1a` 값이 파일 처음 값인지 → 정적 Q80 (파일 값 비교) 에 함께.
- **일 목록에서 갈 곳이 안 정해진 것** (사용자 결정 2026-10-26 에 없음): 결과서 4.3 #5 NLS 길의 커널 쪽 (`nt!NtGetNlsSectionPtr` — VAD `C_949.NLS`·`l_intl.nls` 의 주인, 일 47), #6 의 정적 쪽 (`PspCreateObjectHandle`·`PspUpdateCreateInfo` 가 부모 쪽에 쓰는 것, 일 33), #2 의 정적 쪽 (`BasepInitAppCompatData` 등 본문, 일 37).
- `LdrpMrdataBase`·`LdrpMrdataSize`·`LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected`·`LdrpMrdataUnprotected` 쓴 자리 — `LdrInitializeMrdata` 는 아님. 정적 Q78.
- `LdrpInitializeProcess +0x6c5 RtlNormalizeProcessParams` 가 블록에 하는 일 — 여전히 모름 (Q63(d)·Q78).

## 도구 메모

- Ghidra 는 분류가 안 되던 14개만 depth 0 으로 읽기만 했다 (결과서). 하네스 스크립트는 안 고쳤다.
- 이번 문서의 일 목록 표는 결과서 1장 표에서 근거 열을 빼고 분류를 갈래만 남긴 것이다 (flow-writer 가 결과서에서 그대로 뽑음, 내용은 안 바꿈).
- 닫은 칸의 사진 값은 run44 덤프 읽기 로그 (`ms/logs/04_baggage-read_<P>.txt`) 의 구획 10 (PEB `dt`) 과 17b (`.mrdata` raw `dq`) 에서 찾았다. 새 덤프 읽기는 없다.
- windbg-kd-runbook pitfalls 에 더할 새 함정은 없다.

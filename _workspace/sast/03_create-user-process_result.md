# 03 create-user-process — 정적 분석 결과

- 요청서: `_workspace/sast/03_create-user-process.md`
- 날짜: 2026-10-19
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb=`ntkrnlmp.pdb`, guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` (기준값과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- 분석: **다시 analyze 안 함.** 이미 있던 것만 썼다 (`ms/ghidra/ntoskrnl.gpr`, `ms/decomp/ntoskrnl/_index.tsv` 34982개, `ms/ghidra/ntoskrnl.analyze.log` 179줄 `SetPdb:`; 도는 Ghidra 프로세스·잠금 파일 없음).
- 뽑은 함수: 2개 (정규식 `^(NtCreateUserProcess|PspInsertThread)$`, depth 0, export 3.9초, 종료코드 0, 로그에 SCRIPT ERROR 0건)
  - `ms/decomp/ntoskrnl/NtCreateUserProcess.c`, `PspInsertThread.c`
  - `ms/disassemble/ntoskrnl/NtCreateUserProcess.asm`, `PspInsertThread.asm`, `_calls.tsv`
  - 보조: `ms/disassemble/ntoskrnl/_NtCreateUserProcess_sorted.txt` = `_calls.tsv` 에서 NtCreateUserProcess 줄만 **오프셋 숫자 순**으로 다시 정렬한 것 (`_calls.tsv` 는 글자 순 정렬이라 읽기 불편해서 만들었다. 요청서 밖 함수는 안 뽑았다).
  - export 로그: `ms/ghidra/ntoskrnl.export.sast03.stdout`, `ms/ghidra/ntoskrnl.export.log`
- **버전 확인: 일치 (2/2, 요청서 관찰 4개까지 하면 6/6).**
  - `NtCreateUserProcess` `+0xc36` → `PspAllocateProcess` 일치 (call `+0xc31`, ret `+0xc36`)
  - `PspInsertThread` `+0x807` → `PspCallProcessNotifyRoutines` 일치 (call `+0x802`, ret `+0x807`)
- 판정: **분석성공** (아래 "판정"). 정적으로 못 정한 것은 "동적으로 확인할 것"에 모아 뒀다.

## 스크립트 문제 (맨 위)

**막히는 문제는 없다.** 스크립트는 한 글자도 안 고쳤다. 아래 3건은 "돌긴 도는데 읽는 사람이 오해하기 쉬운 것"이다. 안 고쳐도 이번 결과는 믿어도 된다 (아래 각각 asm 으로 직접 확인).

1. **CFG 간접 호출이 `indirect:` 가 아니라 `_guard_dispatch_icall` 이라는 평범한 이름으로 나온다.**
   - 증상: `_calls.tsv` 에 `NtCreateUserProcess +0x5b2 +0x5b7 _guard_dispatch_icall`, `+0x11bb +0x11c0 _guard_dispatch_icall`. `indirect:` 줄은 0개.
   - 실제: 두 곳 모두 앞에서 전역 포인터를 읽어 `TEST RAX,RAX; JZ` 로 0 검사한 뒤 `CALL _guard_dispatch_icall` 로 그 포인터 함수를 부른다 (asm `+0x59f..+0x5b2`, `+0x11af..+0x11bb`). 진짜 대상은 RAX 안의 값이라 동적으로만 알 수 있다.
   - 위치: `.claude/skills/ghidra-headless-export/scripts/ExportFunctions.java` `calleeName()` (43~65줄). ntoskrnl 의 CFG 검사는 `CALL rel32` 로 스텁 함수를 부르는 모양이라 "직접 호출" 로 보인다 (ntdll 은 `CALL [__guard_dispatch_icall_fptr]` 모양이라 `indirect:` 로 나옴).
   - 제안: callee 이름이 `_guard_dispatch_icall` / `guard_dispatch_icall` 이면 `indirect:cfg(RAX)` 같이 따로 표시.
2. **같은 주소에 PDB 이름이 여럿이면 하나만 보여 줘서 엉뚱한 이름이 나온다.**
   - 증상: `NtCreateUserProcess +0x3cb +0x3d0 ?GetStoragePropertyPost@SC_DEVICE@@MEAAJW4_STORAGE_PROPERTY_ID@@PEAU_STORAGE_DESCRIPTOR_HEADER@@@Z`.
   - 실제: 대상 `140490960` 은 3바이트(`33 c0 c3` = `xor eax,eax; ret`)짜리다. 링커가 같은 코드를 합친 것(ICF)이라 한 주소에 PDB 이름이 여러 개 붙었고 Ghidra 가 그중 하나(SC_DEVICE 쪽)를 골랐다고 보인다 [추측: 별칭 목록은 안 뽑아 봄]. NtCreateUserProcess 와 저장소 장치 클래스는 관련 없다.
   - 위치: `ExportFunctions.java` 50~51줄·58~62줄 (`getPrimarySymbol`).
   - 제안: 같은 주소의 다른 심볼도 `(= 별칭1, 별칭2)` 로 덧붙이거나, 함수 크기가 아주 작을 땐 `.asm` 에 `; ALIAS` 표시.
3. **이름 없는 함수 조각으로 가는 꼬리 점프가 나온다.**
   - 증상: `PspInsertThread +0xa33 tail FUN_140b0d692` (`_calls.tsv`), `PspInsertThread.asm` `+0xa33 JMP 0x140b0d692 ; TAILJMP`.
   - 실제: `+0xa1e` 의 `JNZ`, `+0xa54`·`+0xa6c`·`+0xa72` 도 같은 바깥 구역(`0x140b0d692`, `0x140b0d6e4`, `0x140b0d6e7`)으로 간다. 함수 중간에서 갈라져 나간 **PspInsertThread 의 조각**(에러 길 마무리)으로 보인다 [추측]. 이 조각 안의 호출(`KeForceResumeThread`, `ObCloseHandle`, `KeRaiseUserException`, `KeRequestTerminationThread` 등)은 `PspInsertThread` 의 `_calls.tsv` 줄에 **안 들어간다** (`.c` 에는 보인다).
   - 이번 질문엔 영향 없음 (NtCreateUserProcess 는 조각 점프가 0개, `TAILJMP`·`TRAP` 도 0개). PspInsertThread 를 깊이 볼 Q4 에서 문제가 된다.
   - 제안: 꼬리 점프 대상이 `FUN_` 이름이고 호출자 바로 근처(몇 KB 안)면 그 조각을 같이 뽑거나 `; FRAGMENT` 로 표시.

## 질문별 답 [정적]

### 1. NtCreateUserProcess 가 직접 부르는 함수 (call 순서)

- 답: **직접 호출 줄 83개.** 전부 `CALL rel32` 이고 `tail` 0개, `indirect:` 0개, `TRAP` 0개. 단 2개(`#21 +0x5b2`, `#78 +0x11bb`)는 `_guard_dispatch_icall` 경유 **간접 호출** (스크립트 문제 1번). 전체 83줄은 아래 질문 2의 구간 표에 오프셋 순으로 다 들어 있다.
  - 함수는 `+0x0` ~ `+0x1220`(마지막 `RET`), 호출자 목록은 비어 있음 (`NtCreateUserProcess.c` 머리말 `callers:` 가 빔 = 시스템 콜 입구가 간접으로 부르는 모양과 맞음).
- 01_sprint 관찰 4개와 정적 비교:

| 관찰 (ret_off) | 정적 call_off | 정적 ret_off | callee | 일치? |
|---|---|---|---|---|
| `+0xc36` | `+0xc31` | `+0xc36` | `PspAllocateProcess` | **예** |
| `+0xe5c` | `+0xe57` | `+0xe5c` | `PspAllocateThread` | **예** |
| `+0xfc2` | `+0xfbd` | `+0xfc2` | `PspInsertProcess` | **예** |
| `+0x101a` | `+0x1015` | `+0x101a` | `PspInsertThread` | **예** |

  - 이 네 함수는 각각 **한 번씩만** 불린다 (`_calls.tsv` 에 같은 callee 줄이 하나뿐).
  - 오프셋 순서 = 관찰 순서 (`+0xc36` < `+0xe5c` < `+0xfc2` < `+0x101a`).
- 근거: `ms/disassemble/ntoskrnl/NtCreateUserProcess.asm` 의 `+0xc31`, `+0xe57`, `+0xfbd`, `+0x1015` 줄 (`; CALL ret=… -> 이름`), `ms/disassemble/ntoskrnl/_calls.tsv`.
- 읽을 때 주의:
  - **오프셋 순서 ≠ 실행 순서.** 에러 길·공통 마무리(`+0x11a3` 이후)가 코드 뒤쪽에 모여 있고, 서명 검사 재시도는 루프다. 그래도 위 관찰 4개는 오프셋 순서와 실행 순서가 같았다.
  - 인자 이름(ProcessFlags, ThreadFlags, AttributeList 등)은 NtCreateUserProcess 의 **알려진 시그니처**(11개 인자)에서 가져온 것이다. 이 빌드의 PDB 로 형식 정보를 확인한 건 아니다 [추측]. 디컴파일 `param_7` = ProcessFlags, `param_8` = ThreadFlags, `param_9` = ProcessParameters, `param_10` = CreateInfo, `param_11` = AttributeList 로 읽었다.

### 2. 관찰 4개를 경계로 구간별 나누기

| 구간 | 범위 (call_off) | 호출 수 | 대표 지점 (★) | 잡일 |
|---|---|---|---|---|
| (가) | `+0xc36` 앞 (`+0xd2` ~ `+0xba6`) | 47 | 3개 | 44 |
| (나) | `+0xc36` ~ `+0xe5c` 사이 (`+0xc85` ~ `+0xdce`) | 10 | 2개 | 8 |
| (다) | `+0xe5c` ~ `+0xfc2` 사이 (`+0xe6a` ~ `+0xee5`) | 5 | 1개 | 4 |
| (라) | `+0xfc2` ~ `+0x101a` 사이 | **0** | - | 0 |
| (마) | `+0x101a` 뒤 (`+0x1020` ~ `+0x1208`) | 17 | 2개 | 15 |
| 경계 4개 | 관찰 지점 | 4 | | |
| 합계 | | **83** (= 47+10+5+0+17+4) | | |

(★ 는 흐름도에 넣어 볼 만한 대표 후보. 이름 옆 "메모"는 우리 말 요약이고, 디컴파일 코드는 붙이지 않았다.)

#### (가) `+0xc36` 앞 — "재료 모으기": 인자 검사, 파일 열기, 이미지 섹션 만들기, 토큰·권한·서명 검사

- 대표 후보 (이유):
  - ★ `PspBuildCreateProcessContext` (`+0x295`): 11번째 인자(속성 목록)를 읽어 프로세스 만들기 컨텍스트로 풀어 둔다.
  - ★ `IopCreateFile` (`+0x6c6`): 실행 파일을 연다 (접근권 `0x100020` = 실행+동기). 앞에 `PspCreateUserProcessEcp`(`+0x5fa`) 로 확장 파라미터를 만들고, 실패 때 `IoCreateFileEx`(`+0x73b`) 로 한 번 더 시도하는 코드가 있다.
  - ★ `MmCreateSpecialImageSection` (`+0x937`): 연 파일로 **이미지 섹션**을 만든다. 서명 수준이 안 맞으면 섹션을 닫고 다시 만드는 루프 안에 있다 (`+0xa1e` ObCloseHandle, `+0xa2a` ObfDereferenceObject).
- 잡일 44개: memset 4, silo(컨테이너) 확인 7, 토큰·권한·서명·보호 검사, 객체 참조·해제, 에러 처리 등.

| # | call_off | ret_off | callee | 메모 |
|---|---|---|---|---|
| 1 | +0xd2 | +0xd7 | `memset` |  |
| 2 | +0x117 | +0x11c | `memset` |  |
| 3 | +0x1ab | +0x1b0 | `memset` |  |
| 4 | +0x241 | +0x246 | `ExRaiseDatatypeMisalignment` |  |
| 5 | +0x27b | +0x280 | `memset` |  |
| 6 | +0x295 | +0x29a | `PspBuildCreateProcessContext` | ★ 유저가 준 속성 목록(11번째 인자)을 내부 컨텍스트로 풀어 둠 |
| 7 | +0x311 | +0x316 | `PspCaptureCreateInfo` | 유저가 준 CreateInfo(10번째 인자)를 커널로 복사 |
| 8 | +0x35f | +0x364 | `ObpReferenceObjectByHandleWithTag` |  |
| 9 | +0x387 | +0x38c | `PsGetProcessServerSilo` |  |
| 10 | +0x392 | +0x397 | `PsIsHostSilo` |  |
| 11 | +0x3b2 | +0x3b7 | `PsGetEffectiveServerSilo` |  |
| 12 | +0x3bd | +0x3c2 | `PsIsHostSilo` |  |
| 13 | +0x3cb | +0x3d0 | `?GetStoragePropertyPost@SC_DEVICE@@MEAAJ…` | 3바이트 `xor eax,eax; ret` (같은 코드가 합쳐져 PDB 별칭 이름이 SC_DEVICE 로 나옴. 뜻은 "0 반환") |
| 14 | +0x417 | +0x41c | `PspReferenceTokenForNewProcess` | 새 프로세스에 줄 토큰을 잡음 |
| 15 | +0x442 | +0x447 | `SeQueryServerSiloToken` |  |
| 16 | +0x456 | +0x45b | `PspIsSiloInSilo` |  |
| 17 | +0x4c0 | +0x4c5 | `ZwSystemDebugControl` |  |
| 18 | +0x4db | +0x4e0 | `SeSinglePrivilegeCheck` |  |
| 19 | +0x524 | +0x529 | `SeQuerySigningPolicy` |  |
| 20 | +0x543 | +0x548 | `SeCompareSigningLevels` |  |
| 21 | +0x5b2 | +0x5b7 | `_guard_dispatch_icall` | 간접 호출 (CFG 경유). 전역 포인터 nt+0xfd83d0 이 0 아니면 부름 |
| 22 | +0x5dd | +0x5e2 | `PspGetMemoryPartitionContext` |  |
| 23 | +0x5fa | +0x5ff | `PspCreateUserProcessEcp` |  |
| 24 | +0x62f | +0x634 | `FsRtlpPrepareExtraCreateParametersForCreate` |  |
| 25 | +0x6c6 | +0x6cb | `IopCreateFile` | ★ 실행 파일(exe)을 열기 (접근권 0x100020 = 실행+동기) |
| 26 | +0x73b | +0x740 | `IoCreateFileEx` |  |
| 27 | +0x75a | +0x75f | `PspUpdateCreateInfo` |  |
| 28 | +0x78c | +0x791 | `ObReferenceObjectByHandle` |  |
| 29 | +0x7c3 | +0x7c8 | `Feature_TrustedLaunchHosts__private_IsEn…` |  |
| 30 | +0x7ff | +0x804 | `SeDuplicateTokenWithPredictedClaims` |  |
| 31 | +0x855 | +0x85a | `PsDisableImpersonation` |  |
| 32 | +0x867 | +0x86c | `SeSinglePrivilegeCheck` |  |
| 33 | +0x889 | +0x88e | `PsRestoreImpersonation` |  |
| 34 | +0x8a8 | +0x8ad | `PsRestoreImpersonation` |  |
| 35 | +0x8dd | +0x8e2 | `SeDuplicateTokenAndAddOriginClaim` |  |
| 36 | +0x937 | +0x93c | `MmCreateSpecialImageSection` | ★ 연 파일로 이미지 섹션을 만듦 (섹션 핸들 = 지역변수, 뒤에서 객체로 바꿈) |
| 37 | +0x96d | +0x972 | `ObReferenceObjectByHandle` |  |
| 38 | +0x990 | +0x995 | `PspGetProcessProtectionRequirementsFromImage` |  |
| 39 | +0x9d8 | +0x9dd | `SeQuerySigningPolicy` |  |
| 40 | +0xa08 | +0xa0d | `SeCompareSigningLevels` |  |
| 41 | +0xa1e | +0xa23 | `ObCloseHandle` |  |
| 42 | +0xa2a | +0xa2f | `ObfDereferenceObject` |  |
| 43 | +0xa64 | +0xa69 | `PspValidateCreateProcessProtection` |  |
| 44 | +0xa98 | +0xa9d | `PspCheckForInvalidAccessByProtection` |  |
| 45 | +0xab3 | +0xab8 | `PspGetProcessParameterOverrides` |  |
| 46 | +0xac6 | +0xacb | `PspCaptureProcessParameters` | 프로세스 파라미터(9번째 인자)를 커널로 복사 |
| 47 | +0xba6 | +0xbab | `PspValidateCreateProcessProtection` |  |

#### (나) `+0xc36` ~ `+0xe5c` — "프로세스는 만들었다. 첫 스레드 준비"

(경계 두 줄 포함. 사이 10개가 이 구간.)

- 대표 후보 (이유):
  - ★ `PspCreateUserContext` (`+0xd1d`): 첫 스레드가 유저에서 시작할 문맥(CONTEXT)을 만든다. 이미지 섹션 참조가 있으면 이 길 (`+0xce0` `CMP` 후 `JZ`), 없으면 `PspGetContextThreadInternal`(`+0xd39`) 길. 어느 쪽을 타는지는 런타임 값이다.
  - ★ `PspMapThreadCreationFlags` (`+0xdd3`): 유저가 준 ThreadFlags 를 커널 내부 플래그로 바꾼다. 이 값이 (마)의 `PspInsertThread` 로 넘어가 "멈춰 둘지"를 가른다 (질문 2 아래 "멈춘 채? 풀어 줌?" 참고).
- 잡일 8개: `RtlGetExtendedContextLength2`, `__chkstk`, `memset`, `RtlInitializeExtendedContext2`, `PspGetContextThreadInternal`(위 반대쪽 길), `PspUnlockProcessExclusive`/`PspRundownSingleProcess` (`+0xd4b`, `+0xd56` = 문맥 가져오기 실패 때만), `ObfReferenceObjectWithTag` 등.

| # | call_off | ret_off | callee | 메모 |
|---|---|---|---|---|
| 48 | +0xc31 | +0xc36 | `PspAllocateProcess` | **관찰 일치** (`+0xc36`) |
| 49 | +0xc85 | +0xc8a | `RtlGetExtendedContextLength2` |  |
| 50 | +0xcb2 | +0xcb7 | `__chkstk` |  |
| 51 | +0xcc7 | +0xccc | `memset` |  |
| 52 | +0xcdb | +0xce0 | `RtlInitializeExtendedContext2` |  |
| 53 | +0xd1d | +0xd22 | `PspCreateUserContext` | ★ 첫 스레드의 시작 문맥(CONTEXT) 만들기. 섹션 참조가 있는 쪽 길 |
| 54 | +0xd39 | +0xd3e | `PspGetContextThreadInternal` | 섹션 참조가 없는 쪽 길 (부모 스레드 문맥 복사). 일반 exe 실행에선 앞의 PspCreateUserContext 쪽이 맞아 보임 [추측] |
| 55 | +0xd4b | +0xd50 | `PspUnlockProcessExclusive` |  |
| 56 | +0xd56 | +0xd5b | `PspRundownSingleProcess` |  |
| 57 | +0xd84 | +0xd89 | `ObfReferenceObjectWithTag` |  |
| 58 | +0xdce | +0xdd3 | `PspMapThreadCreationFlags` | ★ 유저의 ThreadFlags(8번째 인자)를 커널 내부 플래그로 바꿈. 이 값이 PspInsertThread 의 "멈춤" 갈림길로 감 |
| 59 | +0xe57 | +0xe5c | `PspAllocateThread` | **관찰 일치** (`+0xe5c`) |

#### (다) `+0xe5c` ~ `+0xfc2` — "스레드는 만들었다. 끼워 넣기 직전"

(경계 두 줄 포함. 사이 5개가 이 구간.)

- 대표 후보: ★ `PsTestProtectedProcessIncompatibility` (`+0xe8f`) 하나. 부모·자식 보호 프로세스가 안 맞는지 본다 (이름과 인자로 본 역할 [추측 섞임]).
- 잡일 4개: `PspUnlockProcessExclusive`/`PspRundownSingleProcess` (`+0xe6a`, `+0xe75` = `PspAllocateThread` 가 음수를 돌려줬을 때만), `wil_details_*` 2개 (`+0xed0`, `+0xee5` = 기능 플래그 사용 기록).
- 이 구간엔 흐름도에 넣을 만한 호출이 사실상 없다. 대신 `PspAllocateThread` 반환 뒤 `PspInsertProcess` 에 넘기는 인자(토큰/silo/파티션)를 준비하는 코드가 호출 없이 이어진다.

| # | call_off | ret_off | callee | 메모 |
|---|---|---|---|---|
| 59 | +0xe57 | +0xe5c | `PspAllocateThread` | **관찰 일치** (`+0xe5c`) |
| 60 | +0xe6a | +0xe6f | `PspUnlockProcessExclusive` |  |
| 61 | +0xe75 | +0xe7a | `PspRundownSingleProcess` |  |
| 62 | +0xe8f | +0xe94 | `PsTestProtectedProcessIncompatibility` | ★ 보호 프로세스끼리 안 맞는지 검사 |
| 63 | +0xed0 | +0xed5 | `wil_details_FeatureReporting_ReportUsageToService` |  |
| 64 | +0xee5 | +0xeea | `wil_details_FeatureStateCache_TryEnableDeviceUsageFastPath` |  |
| 65 | +0xfbd | +0xfc2 | `PspInsertProcess` | **관찰 일치** (`+0xfc2`) |

#### (라) `+0xfc2` ~ `+0x101a` — **호출이 0개**

- `PspInsertProcess` 반환(`+0xfc2`)에서 `EAX` 를 `EBX` 에 저장하고, 인자만 채운 뒤 바로 `PspInsertThread` 를 부른다 (`+0x1015`). 사이에 분기나 호출이 없다.
- 특이점 [정적]: `PspInsertProcess` 가 음수를 돌려줘도 `PspInsertThread` 를 **부르는 코드가 먼저**다. 음수 검사(`TEST EBX,EBX; JNS`)는 `PspInsertThread` 가 돌아온 뒤 `+0x1025` 에서 한다 (asm `+0xfc2` ~ `+0x1027`). 실패하면 그때 `PspRundownSingleProcess`(`+0x102f`) 로 정리한다.

| # | call_off | ret_off | callee | 메모 |
|---|---|---|---|---|
| 65 | +0xfbd | +0xfc2 | `PspInsertProcess` | **관찰 일치** (`+0xfc2`) |
| 66 | +0x1015 | +0x101a | `PspInsertThread` | **관찰 일치** (`+0x101a`) |

#### (마) `+0x101a` 뒤 — "부모 쪽 마무리"

- 대표 후보 (이유):
  - ★ `PspCreateObjectHandle` (`+0x10a2`): 성공 길에서 **프로세스 핸들**을 만든다. 바로 뒤에 CreateInfo 결과를 복사하고 핸들 값을 유저의 `ProcessHandle` 출력 칸에 쓴다 (asm `+0x10b1` ~ `+0x10f6`). (스레드 핸들은 `PspInsertThread` 안의 `PspCreateObjectHandle`(`+0x520`)이 만든다.)
  - ★ `PspUpdateCreateInfo` (`+0x1124`): 성공 길의 끝. 상태값 6 (`MOV ECX,0x6` at `+0x111f`)을 CreateInfo 에 기록한다. 실패 길은 같은 함수를 한 자리(`+0x75a`)에서 값 1 (파일 열기 실패) 또는 값 2 (섹션 만들기 실패)로 부른다 (디컴파일 `uVar19 = 1`/`2` → 공통 호출).
- 잡일 15개: `KiLeaveCriticalRegionUnsafe`, `PspRundownSingleProcess`, `PsGetProcessServerSilo`, `SeDeleteAccessState`, `PsTerminateProcess`(실패 길만), `ObfDereference*` 3, `ObCloseHandle` 2(실패 길), `_guard_dispatch_icall`, `PspDeleteMemoryPartitionContext`, `PspDeleteCreateProcessContext`, `FsRtlFreeExtraCreateParameterList`, `__security_check_cookie`.
- 호출 17개는 `#67` ~ `#83` 이다. 아래 표는 읽기 쉽게 경계인 `#66 PspInsertThread` 줄부터 보여 주지만 17개엔 안 센다.

| # | call_off | ret_off | callee | 메모 |
|---|---|---|---|---|
| 66 | +0x1015 | +0x101a | `PspInsertThread` | **관찰 일치** (`+0x101a`) |
| 67 | +0x1020 | +0x1025 | `KiLeaveCriticalRegionUnsafe` |  |
| 68 | +0x102f | +0x1034 | `PspRundownSingleProcess` |  |
| 69 | +0x1043 | +0x1048 | `PsGetProcessServerSilo` |  |
| 70 | +0x1060 | +0x1065 | `SeDeleteAccessState` |  |
| 71 | +0x106f | +0x1074 | `PsTerminateProcess` | 실패 길에서만 (PspInsertThread 가 음수를 돌려줬을 때) |
| 72 | +0x1078 | +0x107d | `ObfDereferenceObject` |  |
| 73 | +0x1086 | +0x108b | `ObfDereferenceObjectWithTag` |  |
| 74 | +0x10a2 | +0x10a7 | `PspCreateObjectHandle` | ★ 성공 길: 프로세스 핸들 만들기 |
| 75 | +0x1124 | +0x1129 | `PspUpdateCreateInfo` | ★ 성공 길: PS_CREATE_INFO 에 상태값 6 기록 (ECX=6). 6 = "성공" 으로 알려진 값 [추측: 이 빌드 PDB 로 이름 확인 안 함] |
| 76 | +0x1159 | +0x115e | `ObCloseHandle` |  |
| 77 | +0x1191 | +0x1196 | `ObCloseHandle` |  |
| 78 | +0x11bb | +0x11c0 | `_guard_dispatch_icall` | 간접 호출 (CFG 경유). 전역 포인터 nt+0xf04d30 이 0 아니면 부름 (공통 마무리 길) |
| 79 | +0x11c4 | +0x11c9 | `PspDeleteMemoryPartitionContext` |  |
| 80 | +0x11d0 | +0x11d5 | `PspDeleteCreateProcessContext` |  |
| 81 | +0x11e1 | +0x11e6 | `FsRtlFreeExtraCreateParameterList` |  |
| 82 | +0x11ee | +0x11f3 | `ObfDereferenceObject` |  |
| 83 | +0x1208 | +0x120d | `__security_check_cookie` |  |

#### 첫 스레드를 **멈춘 채로 두는지 / 풀어 주는지** (flow.md 2.2 `[공백]`)

- 답: **NtCreateUserProcess 코드 안에는 첫 스레드를 풀어 주는 호출이 없다.** 멈춤 여부는 **런타임 값**(ThreadFlags 의 bit0)이라 정적으로는 못 정한다.
- 근거 (줄마다):
  1. `+0x101a` 뒤 호출 17개 중 `Resume`·`Ready`·`Start` 계열 이름이 없다 (위 (마) 표).
  2. 입구에서 `ThreadFlags & 0xfffffffe` 가 0 이 아니면 바로 `0xc000000d` 로 돌아간다 (asm `+0x1bf` `TEST [8번째 인자],0xfffffffe` → `+0x1ce` `JZ` → `+0x11f9` `MOV EAX,0xc000000d`). 7번째 인자(ProcessFlags)도 `0xff317838` 비트가 있으면 같은 곳으로 간다. 즉 **ThreadFlags 에서 허용되는 비트는 bit0 하나뿐**이다.
  3. `PspMapThreadCreationFlags` (`+0xdd3`)가 ThreadFlags → 내부 플래그(지역변수 `local_788`, 포인터가 `PspInsertThread` 4번째 인자로 감)로 바꾼다. 이 함수 본문은 이번에 안 뽑았다(80바이트로 작다). bit0 가 그대로 bit0 로 가는지는 **확인 못 함**.
  4. `PspInsertThread` 안 (이번에 같이 뽑은 파일 `PspInsertThread.asm`):
     - 플래그 포인터의 bit0 가 켜져 있으면 `KeSuspendThread` 호출 (`+0x2d4` `MOV EAX,[R12]; TEST R13B,AL` → `+0x2e0` CALL). 꺼져 있으면 이 호출을 건너뜀 (`+0x2db` `JZ` → `+0x329`). 플래그 포인터는 4번째 인자(`R9`)이고 `+0x2a2` 에서 `R12` 로 다시 불러온다.
     - `KeSuspendThread` 직후 스레드의 어떤 비트(`[스레드+0x5a0]` bit0, 이름 모름)가 켜져 있으면 `KeForceResumeThread` 로 바로 푼다 (`+0x319` ~ `+0x323` → `+0x9fc`). 에러 때 쓰는 길로 보인다 [추측].
     - 함수 끝 공통 출구에서 `KeReadyThread` 를 부른다 (`+0x5a1`, ret `+0x5a6`). 멈춤 여부와 상관없이 끝에서 불린다.
     - `KeStartThread` 는 `+0x5dd`.
  5. 그러므로 코드가 보여 주는 것은 "ThreadFlags bit0 → (매핑) → `KeSuspendThread` → 마지막에 `KeReadyThread`" 까지다. `KeSuspendThread` 안에서 실제로 어떻게 멈추는지, 그 뒤 스레드가 CPU 를 타는지는 정적으로 못 정한다 (이번에 안 열어 봄).
  6. 풀어 주는 호출은 NtCreateUserProcess 밖(부모 쪽 유저 코드가 NtResumeThread 를 부르는지)이라 `plan.md` Q5(`KERNELBASE!CreateProcessInternalW`)에서 찾을 일이다. 이건 이번 파일로는 알 수 없다.

### 3. points.md `[확인 필요]` 이름 4개가 직접 부르는 목록에 있는가

| 이름 | `_index.tsv` | NtCreateUserProcess 의 직접 호출? |
|---|---|---|
| `MiCreateImageFileMap` | 있음 (`14096ff50`, 2916바이트) | **아니오** |
| `MmInitializeProcessAddressSpace` | 있음 (`14090a498`, 1237바이트) | **아니오** |
| `PspMapSystemDll` | 있음 (`140911bd8`, 480바이트) | **아니오** |
| `MmCreateTeb` | 있음 (`1409318c4`, 811바이트) | **아니오** |

- 근거: 83줄 callee 목록(위 표)에 네 이름이 없다. `_calls.tsv` 에서 네 이름 grep 결과 0줄.
- 따라서 **"Q2 이후에서 찾을 것"** 이다. depth 를 늘려 쫓지 않았다.
- 참고만: 직접 호출 목록에 `MmCreateSpecialImageSection`(371바이트, `+0x937`)이 있고 `MiCreateImageFileMap` 과 이름이 비슷하다. 둘이 이어지는지는 **모른다** [추측]. 가까운 후보는 `PspAllocateProcess`(10186바이트)·`PspAllocateThread`(2850바이트)·`PspInsertProcess`(846바이트) 쪽인데, 어느 함수 안에 있는지는 본문을 안 봐서 모른다.

### 4. 옛 방식 `nt!PspCreateProcess` 를 NtCreateUserProcess 가 부르는 코드가 있는가

- 답: **아니오.** `_calls.tsv`, `.asm`, `.c` (머리말 `callees:` 포함) 어디에도 `PspCreateProcess` 가 없다 (grep 0건).
- `PspCreateProcess` 는 `_index.tsv` 에 함수로 있다 (`1409ffe0c`, 1497바이트). 정적으로 보면 NtCreateUserProcess 는 `PspAllocateProcess` → ... → `PspInsertProcess` 길을 쓰고 있다. 01_sprint 의 "PspCreateProcess 는 불리지 않았다" 관찰과 모순 없음.
- 한계: 이건 **직접** 호출만 본 것이다. 다른 함수 안에서 간접으로 부르는지는 이번 범위 밖.

### 5. 간접 호출·꼬리호출

- NtCreateUserProcess: `tail` 0개, `indirect:` 표기 0개, `TRAP` 0개. 단 **실제 간접 호출 2개**가 `_guard_dispatch_icall` 이름으로 섞여 있다 (스크립트 문제 1번).
  - `+0x5b2` (ret `+0x5b7`): 전역 포인터 `nt+0xfd83d0` (Ghidra 이름 `DAT_140fd83d0`) 이 0 이 아니면 부른다. 인자는 `RCX` = 새 프로세스용 토큰(`[RBP+0x240]` 쪽) [추측]. 반환이 음수면 에러 마무리로 간다. 서명/토큰 관련 콜백으로 보이나 이름 모름 [추측].
  - `+0x11bb` (ret `+0x11c0`): 전역 포인터 `nt+0xf04d30` (`DAT_140f04d30`), 공통 마무리 길에서 불린다. `[RBP+0x258]`(파일 객체 참조, 열었다면 0 아님)이 0 아닐 때만.
- PspInsertThread (버전 확인으로 같이 뽑음): `+0xa33 tail FUN_140b0d692` 1개 (스크립트 문제 3번), `+0x703 INT 0x29` 는 `TRAP` (표에서 빠짐. 리스트 손상 검사 같은 `__fastfail` 로 보이나 [추측]).
- 위 둘 다 **"동적으로 확인할 것"** 으로 넘긴다.

## 참고: PspInsertThread 에서 본 것 (버전 확인용, 범위 밖이라 간단히)

- 호출 줄 52개. 호출자 목록: `NtCreateUserProcess PspCreateThread PspCreatePicoThread` (`PspInsertThread.c` 머리말).
- `+0x802` → `PspCallProcessNotifyRoutines`: 앞에서 `EtwTraceProcess`(`+0x7ce`)·`PoEnergyContextStart`(`+0x7df`)와 함께 한 덩어리(`+0x4b4` `TEST DIL,2` 로 들어가는 곳)에 있다. 이 덩어리는 로컬 비트 `0x2` 가 켜져 있어야 들어가고, 그 비트는 프로세스의 스레드 개수 필드(`[프로세스+0x380]`)가 1이 될 때 켜지는 모양이다 (`+0x78a`~`+0x792` `OR EDI,2`) → "첫 스레드일 때만" 로 읽힌다 [추측]. 01_sprint 에서 `+0x807` 이 관찰된 것과 맞다.
- 그 바로 뒤 `+0x4f5` `PspCallThreadNotifyRoutines`, `+0x520` `PspCreateObjectHandle`(스레드 핸들), `+0x8ea` `PspAssignProcessToJobList`.

## 동적분석으로 확인해야 할 것

1. **ThreadFlags 실제 값** (bit0 켜졌나): 첫 스레드가 멈춘 채로 나오는지의 출발점. 정적으로 못 정함.
2. **`PspInsertThread` 에서 `KeSuspendThread`(`+0x2e0`) 를 탔나**, 그 뒤 `KeReadyThread`(`+0x5a1`) 가 불리고, 그때 스레드 상태가 어떤가. (질문 2 "멈춘 채? 풀어 줌?" 의 동적 쪽)
3. `+0xd1d` `PspCreateUserContext` 길과 `+0xd39` `PspGetContextThreadInternal` 길 중 어느 쪽을 타나. (정적으로는 둘 다 있음; 일반 exe 실행이면 앞쪽일 거라 [추측])
4. 간접 호출 2곳의 실제 대상: `+0x5b2` (포인터 `nt+0xfd83d0`) 가 0 이 아닌지, 대상 함수 이름. `+0x11bb` (포인터 `nt+0xf04d30`) 도 같이.
5. 서명 수준이 안 맞아 `MmCreateSpecialImageSection` 이 한 번 더 불리는지(루프 `+0xa08` ~ `+0xa2a` 후 재시도). test.exe 에선 아마 한 번일 것 [추측].
6. 성공 길 끝(`+0x1124` `PspUpdateCreateInfo`, ECX=6) 도달과 반환 `RAX`(NTSTATUS).

### 추천 멈춤점 (dast-mentor 용 후보, 대기열 맨 아래에 붙일 후보)

대부분 **함수 입구**, 값 보는 건 1~2개. 커널 멈춤점은 모든 프로세스에 걸리므로 부모(= test.exe 를 띄우는 쪽) 조건을 붙여야 한다.

| 후보 | 어디 | 볼 값 | 이유 |
|---|---|---|---|
| A | `nt!NtCreateUserProcess` 입구 | 8번째 인자 `ThreadFlags` = 입구에서 `poi(@rsp+0x40)` (스택 인자 5번째가 `+0x28`) | 동적 확인 1번. asm `+0x1bf` 가 같은 자리를 `[RBP+0x7d8]` 로 읽는 것을 프롤로그로 계산해 맞춰 봄 (`RBP`=입구`RSP`-0x798). 디버거로는 아직 확인 안 함 |
| B | `nt!PspInsertThread` 입구 (NtCreateUserProcess `+0x101a` 에서 돌아올 때) | 4번째 인자 `R9` 가 가리키는 값의 bit0 = `poi(@r9) & 1` | 동적 확인 1·2번. 변환된 최종 플래그 |
| C | `nt!KeSuspendThread` 입구 (또는 `nt!PspInsertThread+0x2e0`) | 호출 여부 | 동적 확인 2번 |
| D | `nt!KeReadyThread` 입구 (호출자 `PspInsertThread+0x5a6` 로 필터) | 호출 시점과 그때 콜스택 | 첫 스레드가 스케줄될 수 있게 되는 순간 후보 |
| E | `nt!NtCreateUserProcess+0x1124` (또는 `nt!PspUpdateCreateInfo` 입구, `ECX==6`) | `ECX`=6 | 성공 길 끝 확인 |

(주의: 입구 인자 위치와 `R9` 사용은 x64 호출 규약에서 계산한 것이고, 이 빌드의 디버거 출력으로 확인한 값이 아니다.)

### 대기열에 붙일 정적 후보 (Q2~Q4 는 이미 있음)

- **작고 싸다**: `PspMapThreadCreationFlags` (80바이트) 본문 1개 — bit0 매핑을 정적으로 마저 확인. 동적 확인 1번을 줄여 준다.
- `PspCreateUserContext` (91바이트) — 첫 스레드의 유저 시작 위치(`Rip`)를 정하는 곳이라 Q7(`LdrInitializeThunk` 로 정해지는 곳)과 이어질 수 있음 [추측].
- `MmCreateSpecialImageSection` (371바이트) callee — `MiCreateImageFileMap` 과의 관계 확인 (points.md `[확인 필요]`).
- Q2 `PspAllocateProcess` 는 10186바이트로 크다. depth 1 이면 파일이 많아질 수 있으니 depth 0 + `_calls.tsv` 로 먼저 보는 게 낫다 [추천].

## 판정

**분석성공.** PDB(guid 기준값 일치)가 붙은 기존 프로젝트로 export 가 바로 돌았고(종료코드 0, SCRIPT ERROR 0), 알려진 사실 2개와 요청서의 관찰 4개가 모두 오프셋까지 일치했다. 질문 1~5 모두 답했고, 단 "첫 스레드를 멈춘 채 두는지"는 **ThreadFlags 의 런타임 값 + PspMapThreadCreationFlags 본문**이 없어 **부분 답**(NtCreateUserProcess 쪽에 풀어 주는 호출은 없다 / 멈춤은 `PspInsertThread +0x2e0` 갈림길)이다.

## dast 요청 필요 / 이름 확인

- dast 요청 필요: 위 "동적분석으로 확인해야 할 것" 1·2번 (추천 멈춤점 A·B·C·D).
- 요청서의 함수 이름 확인 필요: 없음 (요청서 이름 2개 모두 `_index.tsv` 에 정확히 1개씩).

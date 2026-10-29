# 44 eprocess-create — 정적 분석 결과 (Q78c2a)

- 요청서: `_workspace/sast/44_eprocess-create.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`, 기준과 같음).
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 다 있음, Ghidra 프로세스 없음).
- **새로 뽑은 함수 27개, 전부 depth 0**, 두 번에 나눠 뽑았다 (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`). `_calls.tsv` 는 두 번 다 `comm -23` 0줄 (줄이 안 사라짐), 3,598 → 3,744줄.
  - 1차 11개 (요청서 표 그대로): `PspApplyMitigationOptions`, `PspInheritMitigationOptions`, `PspHardenMitigationOptions`, `PspReadIFEOMitigationOptions`, `PspAssignProcessQuotaBlock`, `PspInheritQuota`, `PspInitializeFullProcessImageName`, `PspAttachSession`, `PspSelectMachineForProcess`, `PoEnergyContextInitialize`, `PspUpdateCreateInfo`.
  - 2차 16개 (**요청서 표에 없던 것 — 칸을 실제로 쓰는 줄이 있어서 넣음**): `MiCreateNewProcessTopLevelMappings` (run49 에선 objdump 만이었음, 이제 `.asm` 있음), `PsJoinSession` (**`Session`·`SessionProcessLinks`·`ProcessInSession`**), `PsChargeProcessQuota`, `PspChargeQuota` (**쿼터 사용량 칸**), `MiInsertVad`, `MiInsertProcessVads`, `MiInsertVadCharges` (**VAD·가상 크기 칸**), `MiComputeProcessUserVa` (**`HighestUserAddress`**), `MiInitializeProcessBottomUpEntropy`, `PspSetProcessShortName` (**`ImageFileName`**), `PspReadIFEONodeOptions`, `PspApplyIFEOPerfOptions`, `PspInheritSyscallProvider`, `DbgkCopyProcessDebugPort`, `PsReserveSessionMembership`, `PspSelectNodeForProcess`. 뽑기만 하고 안쪽을 꼼꼼히 안 읽은 것: `MiInsertVadCharges`, `PspSelectNodeForProcess`, `PspUpdateCreateInfo`, `PspAssignProcessQuotaBlock` (칸 쓰기 줄만 `[reg+칸]` 훑어 봄).
  - 이미 `.c` 가 있어서 **읽기만 한 것** (다시 안 뽑음): `PspAllocateProcess`, `PspInsertProcess`, `MmCreateProcessAddressSpace`, `MmInitializeProcessAddressSpace`, `PspSetupUserProcessAddressSpace`, `PspComputeQuantumAndPriority`, `PspInitializeProcessSecurity`, `ObInitProcess`, `PspSetProcessPriorityClass`, `MmCreatePeb`, `NtCreateUserProcess`, `PspCopyAndFixupParameters`.
- **Ghidra 로 안 뽑고 `objdump -d` / PE 직접 읽기로만 본 곳** (함수+오프셋으로만 적는다, `.asm` 없음): `PspReadOptionsMapFromIFEO` (`+0x1f~+0x2b`), `PspReadIFEOAntiTamperOptions`·`PspReadIFEOMitigationAuditOptions`·`PspReadIFEOPerfOptions` (앞부분), `PsChargeProcessPagedPoolQuota`·`PsChargeProcessNonPagedPoolQuota`, `ObCreateObjectEx +0x148`, `ObpAllocateObject +0x191 · +0x197`, `ExAllocatePoolWithTag +0x48 · +0x9f`, 값 읽기(IFEO 값 이름 문자열, `sample/bin/test.exe` 의 `DllCharacteristics`). 그리고 **160칸 전체 훑기** (아래 "판단 기준").
- **이름 확인 (지난 바퀴에 할당 함수 이름이 문서 사이에서 어긋났던 것 포함)**: 이 결과서에 적은 함수 이름 전부를 `ms/decomp/ntoskrnl/_index.tsv` 로 한 번 더 확인했다 — 모두 있음 (rva 는 필요한 곳에 적음). 전역 변수 이름 (`SeCiCallbacks`, `PspSystemPartition`, `PspSystemMitigationOptions`, `PsNextSecurityDomain`, `MmRegistryState` …) 은 `_index.tsv` 가 함수 목록이라 `pdb_aliases.py --where` 로 PDB 이름 확인함 (rva 나옴). 별칭 하나: `PAP +0x7e4` 의 대상 `0x489730` 은 PDB 이름 둘 (`KeQueryHeteroCpuPolicyThread`, `MmGetDefaultPagePriority`) — 여기선 뜻이 맞는 `MmGetDefaultPagePriority` 로 쓴다 (`pdb_aliases.py --lookup` 확인).
  - **할당 함수 이름 정리** (어긋났던 부분): `PspAllocateProcess +0x534 ObCreateObjectEx` → (`ObCreateObjectEx +0x148`) `ObpAllocateObject` → (`ObpAllocateObject +0x197`) **`ExAllocatePoolWithTag`** (rva `0xb80010`, 얇은 겉포장) → (`ExAllocatePoolWithTag +0x9f`) **`ExAllocatePool2`** (rva `0xb800f0`, 실제 할당). 둘 다 맞고 층이 다르다. 0 채움 비트는 `ObpAllocateObject +0x191 BTS ECX,0xa` 가 켜고 `ExAllocatePoolWithTag +0x48 BT EBX,0xa; CMOVB` 가 "초기화 안 함" 을 안 붙이는 쪽으로 가른다 `[정적, objdump]`. 게다가 `PspAllocateProcess +0x55f memset(…, 0, R12D)` 가 한 번 더 몸통 + 확장 영역을 0 으로 민다 (겹치는 안전장치). `PspAllocateProcess` 자기 안의 다른 풀 할당은 `ExAllocatePool2` 직접 (`+0xd75` 'PsCs'(허용 CPU 집합), `+0xfc4` 'WowP'(WoW64 칸 0x10B), `+0x213a` 'SePa').
- 버전 확인: **일치.** `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess`. `PspAllocateProcess +0x1719` (ret `+0x171e`) → `KeInitializeProcess`. 그리고 **사진 값과 코드가 칸마다 맞는다**: `Flags` K1 `0x10050c00` (비트 10·11 `AddressSpaceInitialized`, 16 `ProcessInSession`, 18 `HasAddressSpace`, 28 `DefaultIoPriority`=2) 가 아래 줄 4개와 정확히 같다. `MitigationFlags` K1 `0x20` = `sample/bin/test.exe` 의 `DllCharacteristics` `0x8160` 에 HIGH_ENTROPY_VA (0x20) 는 있고 CFG (0x4000) 는 없는 것과 맞다. `ImageFileName` K1 `"test.exe"`, `Machine` `0x8664`, `HighestUserAddress` `0x7fffffff0000`. 바이너리와 타깃 VM 이 같다고 볼 수 있다.

## 스크립트 문제

없다. (도구 평가의 제안 하나는 맨 아래.)

## 판단 기준 (한 줄)

**만들기 길** = `NtCreateUserProcess` → `PspAllocateProcess` 본문 + EPROCESS 포인터를 넘겨받는 그 callee 줄기 (주소 공간·세션·쿼터·이미지 이름·완화 옵션·보안·핸들 표·VAD) + `PspInsertProcess`; 이 함수들의 `.asm` 에서 칸 오프셋으로 쓰는 줄이 있으면 → 값 0 을 쓰거나 같은 DWORD·QWORD 를 읽어 그대로 되쓰는 줄은 **"0 을 씀"**, 켜는 줄이 있는데 test.exe 에서 조건이 안 맞으면 **"조건부 안 탐"**; 만들기 길 안에 줄이 없으면 커널 전체 `objdump` 훑기 (`[reg+칸오프셋]` 쓰기, 오프셋 숫자 일치라 **오탐 있음** — KTHREAD·PEB·`ERESOURCE` 가 같은 숫자를 씀) 로 이름 있는 EPROCESS 쪽 함수가 보이면 **"범위 밖 길에서 씀 `[추측]`"**, 그것도 안 보이면 **"아무도 안 씀 (만들기 길 안)"**.

## 요약 표 (212칸 → ?)

| | 칸 | 아는 칸 (nt) | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (만들기 길 안) | 범위 밖 길에서 씀 `[추측]` | 여전히 모름 |
|---|---|---|---|---|---|---|---|
| `K1 이전` | 52 | **50** | — | — | — | 1 (`NumberOfPrivatePages`) | 1 (`SharedCommitCharge`) |
| `끝까지 0` | 160 | — | **50** | **30** | **28** | **52** | 0 |
| **합** | **212** | **50** | **50** | **30** | **28** | **53** | **1** |

- `K1 이전` 아는 칸 50 = 쓰는 줄을 찾고 사진 값과 맞춰 봄. `*` 칸 (사진에서 뒤에 또 바뀜) 은 **처음 값만** 닫음 — 뒤에 바꾸는 쪽은 이름만.
- `0 을 씀` 50 = 명시 0 저장 11 + 입력값 0 을 씀 4 (`ServerSilo`·`Protection`·`SignatureLevel`·`SectionSignatureLevel`) + 같은 DWORD 되쓰기 35 (`Flags2` 19 비트 `+0x819`, `Flags` 16 비트 `+0x7fe`). run49 방식으로 합치면 baggage 2장 "아는 칸" 에 `50 + 50 + 30 = 130` 이 들어가고 (줄이 있는 칸), "아무도 안 씀" 은 `28`, "범위 밖 길" 53 은 flow-writer 가 정한다 (run47·run49 와 같은 방식).
- 조건부 안 탐 30 중 `Job`·`JobLinks`·`CommitChargeJob` 3칸은 **호출 줄 (`PspInsertProcess +0xd4`) 과 조건만 앎, 칸을 실제로 쓰는 줄은 callee (`PspImplicitAssignProcessToJob` 아래) 안이라 안 읽음** `[추측]`.

## 한눈에

- **`K1 이전` 52칸 중 50칸은 `PspAllocateProcess` 한 함수와 그 callee 몇 개가 쓴다.** 객체를 받고 (`+0x534`) 0 으로 밀고 (`+0x55f`) 목록 머리를 자기 연결하는 묶음 (`+0x564~+0x611`) 뒤, 부모 값을 복사하는 칸들 (`Flags`·`Flags2` 의 우선순위 비트, `InheritedFromUniqueProcessId`, `DefaultHardErrorProcessing`, `QuotaBlock`, `SecurityDomain`) 과 입력으로 받는 칸 (`SectionObject`, `ImageFilePointer`, `Machine`) 과 주소 공간을 만들며 쓰는 칸 (`Vm`, `VadRoot`, `HighestUserAddress`, `MmProcessLinks`, `CommitCharge` …) 이 나온다.
- **`Session` 은 부모가 아니라 "지금 도는 프로세스 (호출한 쪽)" 의 세션이다.** `PsReserveSessionMembership` 이 현재 스레드 (`GS:[0x188]`) 의 프로세스 `+0x2e8` 을 읽고 참조를 올려서 `PsJoinSession` 에 넘긴다 `[정적]`. 부모 핸들이 따로 있어도 같다.
- **IFEO 키가 없으면 (test.exe) 커널은 IFEO 값을 하나도 안 읽는다.** `RtlpOpenImageFileOptionsKeyEx` (`PAP +0xa5c`) 가 `0xC0000034` 를 돌려주면 `CreateInfo+0x10 |= 0x40`, 키 핸들 칸 (`+0xc8`) = 0 → 이후 모든 IFEO 값 읽기를 건너뛰고 (`PAP +0xad0 JZ → +0xfa5`), `PspReadOptionsMapFromIFEO` 도 `+0xc8` 이 0 이면 `0xC000000D` 로 바로 돌아간다. 이 `0x40` 은 `PspCopyAndFixupParameters +0x74~+0x82` 가 새 프로세스 매개변수 `Flags |= 0x4000` 으로 옮기고, 그래서 ntdll `LdrpInitializeExecutionOptions` 가 IFEO 를 건너뛴다 (run46 의 `Flags=0x4001` 과 같은 줄기).
- **끝까지 0 160칸**: 50 은 만들기 길이 0 을 쓴다, 30 은 켜는 줄이 있는데 test.exe 가 안 탄다 (복제 길·WoW64·작업·디버그 포트·IFEO 값·세션 만들기·CPU 집합 …), 28 은 아무도 안 쓴다, 52 는 종료·작업·타이머·페이지 오류 같은 범위 밖 길에서 쓴다 `[추측]`.
- **정적으로 못 닫은 것**: `SharedCommitCharge` K1 값 (`0x2b`) 을 만든 줄, `NumberOfPrivatePages` 처음 값 (페이지 오류 `[추측]`), `MitigationFlags` `0x20` 이 두 후보 줄 중 어느 쪽인지 (값이 같음), `SecurityDomain` 이 부모 값 복사인지 (cmd 값 대조 필요), 간접 호출 `SeCiCallbacks+0xc0/+0xc8` 의 대상 (Q35).

## 질문별 답 [정적]

경로 약어 (`(호출 +0x..)` = 부르는 자리, ret 아님):

- `①` = `NtCreateUserProcess`
- `①P…` = `① → PspAllocateProcess (호출 +0xc31)` (이후 `PAP` 라고도 씀)
- `①C…` = `①P… → MmCreateProcessAddressSpace (호출 +0x1637)`
- `①M…` = `①P… → MmInitializeProcessAddressSpace (호출 +0x2090)`
- `①I…` = `① → PspInsertProcess (호출 +0xfbd)`

`PspAllocateProcess` 인자 (`+0x...` 는 `.asm` 로 센 자리): 1 `RCX` 부모 · 7 섹션 (`NtCreateUserProcess +0xc0c`) · 8 토큰 (`+0xc00`) · 9 `ProcessFlags` · 11 속성 모음 `CreateInfo` (`[RBP+0x1a0]`) · 12 "토큰 핸들 속성 있음" (`+0xbbd CMP [RBP+0x238]; SETNZ`) · 13 파티션 속성 · 15 출력 (새 프로세스 포인터 `+0x2829`). `puVar37` (지역 플래그 `EBX`) 의 비트 `4` 는 **"부모를 복제 (섹션 없음)"** 지 "부모가 있음" 이 아니다 (`PAP +0x2ec~+0x317`: 섹션 `param_7` 이 0 일 때만 켜짐). test.exe 는 섹션이 있어서 이 플래그가 0 이다.

### 1. `K1 이전` 52칸 쓰는 줄

**답: 52칸 중 50칸은 쓰는 줄을 찾았고 (아래 표), 사진 K1 값과 칸마다 맞춰 봤다. 1칸 (`NumberOfPrivatePages`) 은 만들기 길에 쓰는 줄이 없고 페이지 오류 처리가 올리는 것으로 보인다 `[추측]`. 1칸 (`SharedCommitCharge`) 은 줄 후보만 있고 K1 값을 만든 줄은 못 정함.**

#### 1.1 칸마다 (baggage 3.11 양식, 바로 옮길 수 있게)

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| `+0x1f0` `Flags2` · `.DefaultPagePriority` (12:3) | ① | 부모 `Flags2` 비트 12~14 (페이지 우선순위) 를 읽어 **같은 DWORD 를 되씀** (다른 비트는 읽은 값 0). 부모가 없으면 `MmGetDefaultPagePriority` 반환값 | 직접 | nt | `①P… +0x7be (읽기) · +0x819 (쓰기)`; 부모 없음 `+0x7e4` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`0x5000` → 5) |
| `+0x1f4` `Flags` · `.DefaultIoPriority` (27:3) | ① | 부모 `Flags` 비트 27~29 복사, 부모 없으면 `2`. 같은 DWORD 되쓰기 | 직접 | nt | `①P… +0x7b0~+0x7bb (읽기) · +0x7fe (쓰기)`; 부모 없음 `+0x7df` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (비트 28 → 2) |
| `+0x1f4` `Flags` · `.AddressSpaceInitialized` (10:2) | ① | 비트 11 켜기 (최상위 페이지 표 만든 직후) → 비트 10 켜기 (작업 집합·VAD 준비 끝) | 직접 (`lock or`) | nt | 비트 11: `①C… → MiCreateNewProcessTopLevelMappings (호출 +0x203) +0x72` (`lock or 0x800`); 비트 10: `①M… +0x117` (`lock or 0x400`) | `[정적]` sast44 · 정확 · 사진 `K1 이전` (3) |
| `+0x1f4` `Flags` · `.ProcessInSession` (16) | ① | 세션 목록에 달면서 켬 | 직접 (`lock or`) | nt | `①C… → PsJoinSession (호출 +0x21a) +0x21` (`lock or 0x10000`) | `[정적]` sast44 · 정확 · 사진 `K1 이전` |
| `+0x1f4` `Flags` · `.HasAddressSpace` (18) | ① | 주소 공간이 생긴 뒤 켬 (`MmCreateProcessAddressSpace` 성공 후) | 직접 (`lock or`) | nt | `①P… +0x169c` (`lock or 0x40000`) | `[정적]` sast44 · 정확 · 사진 `K1 이전` |
| `+0x200` `ProcessQuotaUsage*` (16B) · `+0x210` `ProcessQuotaPeak*` (16B) | ① | 쿼터 종류별 8바이트 칸에 **더함** (사용량) 과 최대값 갱신 (Peak). 처음 사진 값 `0x4d8` · `0x2998` (Peak 도 같음). 어느 종류 번호가 어느 칸인지는 안 맞춤 | 직접 (공용 부품 `PspChargeQuota` 가 `lock xadd` · `cmpxchg`) | nt | `PspChargeQuota +0x95 (사용량) · +0xeb (Peak)`; 부르는 쪽 `①C… +0x19b PsChargeProcessQuota (+0x20)`, 핸들 표 `ObInitProcess → … → ExpAllocateHandleTable +0x4f → PsChargeProcessPagedPoolQuota (+0x23)`, VAD `①M… → MiInsertProcessVads (호출 +0x49) → MiInsertVadCharges (+0x74 · +0x153)` | `[정적]` sast44 (`PspChargeQuota` 는 `.asm`, 두 `PsCharge…Quota` 는 objdump) · 정확 (칸) / 순서는 동적 · 사진 `K1 이전` → 바뀜 (`*`) |
| `+0x220` `PeakVirtualSize*` · `+0x228` `VirtualSize*` | ① | VAD 를 달 때마다 `VirtualSize += 크기`, `Peak` 가 더 작으면 올림. 처음 값 `0x545000` | 직접 | nt | `①M… → MiInsertProcessVads (호출 +0x1a3) → MiInsertVad (호출 +0x5d) +0x1b0 (VirtualSize) · +0x1c7 (Peak)` | `[정적]` sast44 · 정확 · 사진 `K1 이전` → 바뀜 (`*`) |
| `+0x230` `SessionProcessLinks` | ① | 세션의 프로세스 목록 꼬리에 달기 (목록 잠금 안, 호출 없이 포인터 4개) | 직접 | nt | `①C… → PsJoinSession +0x41 ~ +0x65` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`…9537e870`, `Session +0x10` 과 맞음) |
| `+0x288` `NumberOfPrivatePages*` | — | 만들기 길 안에 쓰는 줄 **없음**. K1 값 `0x20` 은 페이지 오류 (`MiCompletePrivateZeroFault +0xc4a · +0x1144` 의 `lock add/inc [+0x288]`) 가 올린 것으로 보인다 `[추측]` (`MmInitializeHandBuiltProcess +0x100` 은 손수 만든 프로세스 갈래라 안 탐) | — | 범위 밖 (페이지 오류) | 지도 밖 (`MiCompletePrivateZeroFault` 는 지도에 없음) | `[추측]` sast44 · 짐작 · 사진 `K1 이전` → 바뀜 (`*`) |
| `+0x2a8` `SectionObject` | ① | 7번째 인자의 섹션 객체 (참조 +1 한 뒤) 저장. 단 `0x1000` 플래그면 되돌려 0 (안 탐) | 직접 | nt | `①P… +0x95f` (참조 `+0x89e ObfReferenceObject`) | `[정적]` sast41 · sast44 · 정확 · 사진 `K1 이전` |
| `+0x2d0` `InheritedFromUniqueProcessId` | ① | 부모의 `UniqueProcessId` (`+0x1d0`) 복사, 부모 없으면 0 그대로 | 직접 | nt | `①P… +0x7ce (읽기) · +0x7d6 (쓰기)` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`0x19a8` = 부모 cmd) |
| `+0x2e8` `Session` | ① | **지금 도는 프로세스 (호출한 쪽) 의 세션** 포인터 (참조 +1 받아서) | 직접 | nt | `①C… → PsReserveSessionMembership (호출 +0x1da) +0x23 · +0x3e` (읽고 참조 올림) → `①C… → PsJoinSession (호출 +0x21a) +0x17` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`…9537e860`). 갈래: `PspAttachSession (호출 +0x151f)` 는 아래 2장 (안 탐) |
| `+0x2f8` `QuotaBlock` | ① | **부모 (토큰 핸들 속성이 없을 때) 의 쿼터 블록을 같이 가리킴**, 블록의 참조 두 칸 (`+0x200`·`+0x204`) 을 `lock inc` | 직접 | nt | `①P… → PspInheritQuota (호출 +0x15d7) +0xc · +0x13 · +0x1a` | `[정적]` sast44 · 정확 · 사진 `K1 이전`. 갈래: `PspAssignProcessQuotaBlock (호출 +0x158f)` — 12번째 인자 (토큰 핸들 속성) ≠ 0 일 때만 (`+0x1574 CMP [RSP+0x8c8]`) → 안 탐 |
| `+0x330` `ImageFilePointer` | ① | `CreateInfo+0xb8` 의 이미지 파일 객체 (참조 +1 'PsCr') | 직접 | nt | `①P… +0xa2b (참조) · +0xa37 (저장)` | `[정적]` sast41 · sast44 · 정확 · 사진 `K1 이전` |
| `+0x338` `ImageFileName` (15B) | ① | 이미지 파일 이름 마지막 조각을 **한 글자씩 (유니코드 → 1바이트)** 15바이트까지 복사 + 널 | 직접 (반복문) | nt | `①P… → PspInitializeFullProcessImageName (호출 +0x2065) → PspSetProcessShortName (호출 +0x77) +0x48 · +0x52` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`"test.exe"`). 조건 `Flags3.Minimal` (`+0x4d`) 면 건너뜀 |
| `+0x347` `PriorityClass` | ① | 먼저 `2` (보통), 이어서 부모 클래스가 `1` 또는 `5` 면 부모 값 복사 (`(c-1) & 0xfb == 0`) | 직접 | nt | `①P… +0x1931 (상수 2) · +0x194f (복사)`. 그 뒤 `PspSetProcessPriorityClass (호출 +0x1a3b) +0x72` 는 속성 모음 비트 8 일 때만 (안 탐) | `[정적]` sast44 · 정확 (**3.11 "K1 값 2 를 처음 넣는 줄은 못 찾음" 닫힘**) · 사진 `K1 이전` |
| `+0x350` `SeAuditProcessCreationInfo` | ① | 이미지 전체 경로 (`ObQueryNameStringMode` 결과, 풀 'PsIn') 객체 이름 구조체 포인터 | 직접 | nt | `①P… → PspInitializeFullProcessImageName +0x115` (복제 길은 `①P… +0x213f` 'SePa' — 안 탐) | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`…9ab2a460`) |
| `+0x368` `HighestUserAddress` | ① | 기본 `0x7fffffff0000`, WoW64 x86 이면 `0xffff0000`/`0x7fff0000` | 직접 | nt | `①M… → MiComputeProcessUserVa (호출 +0x136) +0xa8` | `[정적]` sast44 · 정확 · 사진 `K1 이전` |
| `+0x388` `DefaultHardErrorProcessing` | ① | 부모 값 복사 (속성 모음 비트 9 가 있으면 `CreateInfo+0x13c`, 부모 없으면 `5`) | 직접 | nt | `①P… +0x78a (읽기) · +0x799 (쓰기)` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (1) |
| `+0x3d8` `CommitCharge*` · `+0x3e0` `CommitChargePeak*` | ① | **처음 값 `4`** (주소 공간 만들 때 최상위 페이지 표 하나치), 뒤에 `MiChargeProcessCommitment` 가 더함·Peak 갱신 | 직접 | nt | `①C… +0x1ed` (`MOV [+0x3d8],4`); 더하기 `MiChargeProcessCommitment +0x11 · +0x4d` (Peak `+0x5d`) | `[정적]` sast44 · 정확 (처음 값) · 사진 `K1 이전` (`0x58`) → 바뀜 |
| `+0x400` `Vm` (`_MMSUPPORT_FULL`, 320B)* | ① | 안쪽은 범위 밖 (이름만). 시작: `Instance` 앞부분 난수 (`RtlRandomEx`), 작업 집합 최소·최대 (`+0x70`·`+0x78`), `VmWorkingSetList` (`Vm+0x10`=`+0x410`) = 풀 블록 0x580B 포인터 ('MiP2') | 직접 | nt | `①C… +0xdd · +0x12d · +0x131 · +0x15e · +0x213`; `①M… → MiInitializeWorkingSetList (호출 +0xe8)` | `[정적]` sast44 · 정확 (시작만) · 사진 `K1 이전` → 바뀜 |
| `+0x540` `MmProcessLinks` | ① | 전역 프로세스 목록 (`0x140e2db98`, PDB 이름 안 찾음) 꼬리에 달기 (스핀락 안) | 직접 | nt | `①C… → MiCreateNewProcessTopLevelMappings (호출 +0x203) +0x3d ~ +0x65` | `[정적]` sast44 · 정확 (**3.11 "+0x540 목록 이름 모름" 닫힘**) · 사진 `K1 이전` |
| `+0x554` `ExitStatus` | ① | `0x103` (`STATUS_PENDING`, 아직 안 끝남) | 직접 | nt | `①P… +0x7a0` | `[정적]` sast44 · 정확 · 사진 `K1 이전` |
| `+0x558` `VadRoot*` · `+0x560` `VadHint*` · `+0x568` `VadCount*` | ① | `VadRoot` = 0, 뒤에 VAD 를 달 때마다 `VadCount++`, `VadHint` = 방금 단 VAD, 트리에 삽입 | 직접 | nt | `①M… +0xc3` (VadRoot=0); `①M… → MiInsertProcessVads (호출 +0x1a3) → MiInsertVad (호출 +0x5d) +0x9d (VadCount) · +0xbc (VadHint) · +0x12e (RtlAvlInsertNodeEx)` | `[정적]` sast44 · 정확 · 사진 `K1 이전` → 바뀜 (`*`) |
| `+0x580` `AlpcContext*` (32B) | ① | 0 으로 만들고 목록 머리 (`+0x588`) 자기 연결 | 직접 | nt | `①P… +0x567 · +0x56f · +0x577 · +0x585 ~ +0x58c` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (처음 칸 0) → 바뀜 (`*`) |
| `+0x620` `SvmProcessDeviceListHead` | ① | 자기 연결 | 직접 | nt | `①P… +0x5a8 ~ +0x5b3` | `[정적]` sast44 · 정확 · 사진 `K1 이전` |
| `+0x638` `DiskCounters` · `+0x668` `EnergyContext` · `+0x7f0` `NetworkCounters` | ① | 몸통 뒤 **확장 영역** 안을 가리키는 포인터 (`PsDisableDiskCounters`==0 이면 디스크 칸 0x28B, `PoEnergyEstimationEnabled` 면 에너지 칸 0x1e8B). 에너지는 이어서 초기화 호출 | 직접 | nt | `①P… +0x728 (DiskCounters) · +0x739 (NetworkCounters) · +0x750 (EnergyContext) → PoEnergyContextInitialize (호출 +0x757)` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`…b8c0`·`…b8f8`·`…b8e8`, 몸통 끝 `+0x840` 이후) |
| `+0x680` `CreateInterruptTime` · `+0x688` `CreateUnbiasedInterruptTime` · `+0x698` `LastAppStateUpdateTime` | ① | 공유 데이터 `0xfffff78000000008` (InterruptTime) 복사 / `KiQueryUnbiasedInterruptTime` 반환 / `+0x680` 복사. 세 값 K1 이 같음 (`0x31c4e3efc`) | 직접 | nt | `①P… +0x27f5 (Create) · +0x280c (Unbiased ← 호출 +0x27ff) · +0x281a (LastAppStateUpdateTime)` | `[정적]` sast44 · 정확 · 사진 `K1 이전` |
| `+0x6a8` `SharedCommitCharge*` | ? | **줄 후보만**: `MiInsertSharedCommitNode +0x33e` (`lock add [+0x6a8]`) / `MiRemoveSharedCommitNode +0x173` — 부르는 쪽 `MiAllocateChildVads +0x249` (복제), `MmLinkJobProcess +0x195` (작업), `MiSectionOpen +0x18` (← `ObpIncrementHandleCountEx +0x77e`, 지도 깊이 5). 어느 줄이 K1 `0x2b` 를 만들었는지 못 정함 | ? | nt `[추측]` | `?` | `[공백]` 줄 후보 · 사진 `K1 이전` → 바뀜 (`*`) |
| `+0x6b8` `SharedCommitLinks*` (16B) | ① | 자기 연결로 시작 (`+0x6b0` 락은 0 으로 씀). 뒤에 노드가 달리면 바뀜 (K1 은 이미 다른 주소) | 직접 | nt | `①C… +0xe0 ~ +0xeb` | `[정적]` sast44 · 정확 (처음 값) · 사진 `K1 이전` → 바뀜 |
| `+0x6ec` `Machine` | ① | 기본 `0x8664` (`+0x24f`), `PspSelectMachineForProcess` 가 정한 값으로 덮음, 복제 길은 부모 값 | 직접 | nt | `①P… +0x1043`; 값 `PspSelectMachineForProcess (호출 +0xa87)` 출력 `[RSP+0xd0]` (`+0xaa2`) | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`0x8664`) |
| `+0x6ee` `MmSlabIdentity` | ① | `0xff` (슬랩 식별자 없음) | 직접 | nt | `①C… +0x84` (`MiCreateSlabIdentity` 갈래 `+0x8d` 는 인자 비트 4 일 때만 — 안 탐) | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`0xff`) |
| `+0x710` `VirtualTimerListHead` | ① | 자기 연결 | 직접 | nt | `①P… +0x5b6 ~ +0x5c1` | `[정적]` sast44 · 정확 · 사진 `K1 이전` |
| `+0x750` `MitigationFlags` (+`Values`) | ① | `0x20` (`HighEntropyASLREnabled`). 켜는 줄 둘: ⓐ 이미지 헤더 `DllCharacteristics` 비트 5 (HIGH_ENTROPY_VA) → 인자 비트 1 → `PspApplyMitigationOptions +0xd1`; ⓑ `MiInitializeProcessBottomUpEntropy +0x19d` (전역 `0x140fc6ac8` 의 `0x30` 비트가 꺼져 있을 때). test.exe `DllCharacteristics` `0x8160` 이라 ⓐ 는 확실히 켠다 | 직접 (`lock or`) | nt | ⓐ `①P… → PspApplyMitigationOptions (호출 +0x1eb3) +0xd1`; ⓑ `①M… → MiInitializeProcessBottomUpEntropy (호출 +0x181) +0x19d` | `[정적]` sast44 · 정확 (ⓐ) / ⓑ 조건은 동적 · 사진 `K1 이전` (`0x20`) |
| `+0x754` `MitigationFlags2` (+`Values`) | ① | `0x40000000` (비트 30 `CetDynamicApisOutOfProcOnly`). 켜는 줄: `PspApplyMitigationOptions +0x5a5` (`R14D=0x40000000` 은 `+0x3da`, 사이에 `R14` 안 바뀜) — 조건 = 합친 옵션 셋째 칸 비트 16~17 가 1. 그 값의 뿌리 (`PspSystemMitigationOptions` 기본값 `[추측]`) 는 동적 | 직접 (`lock or`) | nt | `①P… → PspInheritMitigationOptions (호출 +0x1aea) → PspApplyMitigationOptions (호출 +0x1eb3) +0x5a5` | `[정적]` sast44 · 정확 (줄) / 값의 뿌리 `[추측]` · 사진 `K1 이전` |
| `+0x758` `PartitionObject` | ① | 전역 시스템 파티션 (`PspSystemPartition`) 포인터 (참조 `+0x28` `XADD` 로 +1). 13번째 인자 (파티션 속성) 가 있으면 `PsReferencePartitionSafe (호출 +0x830)` | 직접 | nt | `①P… +0x843 ~ +0x855 (참조) · +0x88a (저장)` | `[정적]` sast44 · 정확 · 사진 `K1 이전` (`…936b5f40`) |
| `+0x760` `SecurityDomain` · `+0x768` `ParentSecurityDomain` | ① | **기본은 부모의 `SecurityDomain` 복사**, 둘에 같은 값. 새 번호 (`PsNextSecurityDomain` `XADD` +1) 는 ⓐ 작업 breakaway 플래그 ⓑ 부모 `MitigationFlags` 부호 비트 (`IsolateSecurityDomain`) ⓒ 새 프로세스 `Flags3.VmProcessorHost` 이면서 부모 값이 0 일 때 | 직접 | nt | `①P… → PspInitializeProcessSecurity (호출 +0x1922) +0x183 (부모 값) / +0x18f (새 번호) · +0x1a0 · +0x1a7` | `[정적]` sast44 · 정확 (줄·조건) / 부모 값과 같은지는 동적 · 사진 `K1 이전` (`0x100000040` 둘 다) |
| `+0x7a4` `PageCombineSequence` | ① | `1` | 직접 | nt | `①C… +0x73` (`MiSetProcessPartitionId` 호출 +0x6e 바로 뒤) | `[정적]` sast44 · 정확 · 사진 `K1 이전` (1) |

- 이 표는 `K1 이전` 52칸을 모두 센다 (`Flags2` 2 + `Flags` 5 + 쿼터 2 + 가상 크기 2 + `SessionProcessLinks` 1 + `NumberOfPrivatePages` 1 + 한 줄짜리 12 + `CommitCharge` 2 + `Vm` 1 + `MmProcessLinks` 1 + `ExitStatus` 1 + VAD 3 + `AlpcContext` 1 + `SvmProcessDeviceListHead` 1 + 포인터 3 + 시각 3 + `SharedCommit*` 2 + `Machine`·`MmSlabIdentity`·`VirtualTimerListHead` 3 + 완화 4 + `PartitionObject` 1 + 보안 도메인 2 + `PageCombineSequence` 1 = 52).
- 값 대조: 사진 8장 (`ms/logs/04_baggage-read_<K1~P2>.txt` `01c EPROCESS_RAW`) 을 칸마다 `scratchpad/fields.py` 로 뽑아 비교. K1 이전 52칸 중 **여덟 장 내내 안 바뀐 칸**: `SessionProcessLinks`, `SectionObject`, `InheritedFromUniqueProcessId`, `Session`, `QuotaBlock`, `ImageFilePointer`, `ImageFileName`, `PriorityClass`, `SeAuditProcessCreationInfo`, `HighestUserAddress`, `DefaultHardErrorProcessing`, `MmProcessLinks`, `ExitStatus`, 시각 3, `Machine`, `MmSlabIdentity`, `VirtualTimerListHead`, 완화 4, `PartitionObject`, 보안 도메인 2, `PageCombineSequence`, 에너지·디스크·네트워크 포인터 3. 바뀌는 칸 (`*`): `Flags`·`Flags2` (P0, P1a 에 비트 추가 — Q78c2b), 쿼터 2, 가상 크기 2, `NumberOfPrivatePages`, `CommitCharge*`, `Vm`, VAD 3, `SharedCommit*`.

#### 1.2 3.11 "(run49) Q78c2 에서 볼 것" 메모 확인

| 메모 줄 | 확인 결과 |
|---|---|
| `SecurityDomain`·`ParentSecurityDomain` `PspInitializeProcessSecurity +0x1a0 · +0x1a7`, 지움 `PspDisablePrimaryTokenExchange +0x94 · +0xa2` | 줄 맞음. **조건이 구체화됨**: 기본은 부모 값 복사, 새 번호는 ⓐⓑⓒ (위 표). 지우는 쪽은 만들기 길 밖 (스레드 시작) |
| `PriorityClass` `PspSetProcessPriorityClass +0x72` + "K1 값 2 를 처음 넣는 줄 못 찾음" | `+0x72` 는 맞고 (안 탐), **처음 넣는 줄 = `PspAllocateProcess +0x1931 / +0x194f`** — 닫힘 |
| `Flags.AddressSpaceInitialized` 비트 11 `MiCreateNewProcessTopLevelMappings +0x72` (`.asm` 없음) | 맞음, 이제 `.asm` 있음. **비트 10 은 다른 함수**: `MmInitializeProcessAddressSpace +0x117` |
| `+0x540` 목록·`+0x3d8`·`+0x410` (이름 모름) | **`+0x540` = `MmProcessLinks`, `+0x3d8` = `CommitCharge` (처음 값 4), `+0x410` = `Vm.Instance.VmWorkingSetList`** (`Vm+0x10`, 풀 블록 포인터). 줄은 위 표 |
| `PspAllocateProcess +0x564~+0x5fc` 초기화 묶음 | 칸 이름 맞춤 — 아래 2장 "0 을 씀" 표 |
| 몸통 크기 `+0x450~+0x4f7` | 확인: 몸통 `0x840` (+`PsDisableDiskCounters`==0 이면 0x28 디스크 칸, `PoEnergyEstimationEnabled` 면 0x1e8 에너지 칸, 그룹 여럿이면 그룹당 0x10) 뒤에 `KeSizeOfKProcessExtension` 만큼 붙임 (run49 1.2). 디스크·에너지·네트워크 칸 포인터가 이 확장 영역 안을 가리킴 (위 표) |

### 2. 끝까지 0 160칸 가르기

**답: 0 을 씀 50 · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 길에서 씀 52 (합 160). 판단 기준은 위 한 줄.**

#### 2.1 0 을 씀 (50)

| 묶음 | 칸 | 쓰는 줄 | 비고 |
|---|---|---|---|
| 명시 0 저장 (11) | `RundownProtect` | `①P… +0x58c` | |
| | `AddressCreationLock` · `PageTableCommitmentLock` | `①M… +0xb5 · +0xbc` | `MmInitializeProcessAddressSpace` |
| | `SharedCommitLock` | `①C… +0xee` | `AND [..],0` |
| | `EnclaveLock` · `DynamicEHContinuationTargetsTree` · `DynamicEHContinuationTargetsLock` · `DynamicEnforcedCetCompatibleRanges` (16B) · `EnableOptionalXStateFeaturesLock` · `PpmPolicyLock` | `①P… +0x5e7 · +0x5f5 · +0x5ee · +0x603 · +0x5fc · +0x60a · +0x611` | `memset` 직후 묶음 (3.11 메모) |
| | `DebugPort` | `①I… → DbgkCopyProcessDebugPort (호출 +0x13e) +0x27` | 부모 것을 복사하는 줄 (`+0x2c89e1` 조각) 은 아래 2.2 |
| 입력값 0 을 씀 (4) | `Protection` · `SignatureLevel` · `SectionSignatureLevel` | `①P… +0x5c4 · +0x5d2 · +0x5e0` | 값 = 인자 4·5·6 (`NtCreateUserProcess +0xb5d~+0xb71` 이 부모 프로세스의 값을 읽고 속성 검증 `PspValidateCreateProcessProtection (호출 +0xba6)` 을 거친 것) — cmd 가 보호 프로세스가 아니라 0 `[관찰 간접]` |
| | `ServerSilo` | `①P… +0x97f` (값 = `CreateInfo+0x1a8` 서버 실로 속성) | 속성 없어서 0 |
| 같은 DWORD 되쓰기 (35) | `Flags2` 19비트: `JobNotReallyActive` `AccountingFolded` `NewProcessReported` `ExitProcessReported` `ReportCommitChanges` `LastReportMemory` `NeedsHandleRundown` `RefTraceEnabled` `PicoCreated` `EmptyJobEvaluated` `ProcessVerifierTarget` `RestrictSetThreadContext` `AffinityPermanent` `AffinityUpdateEnable` `Flags2Available1` `FatalAccessTerminationRequested` `DisableSystemAllowedCpuSet` `Flags2Available2` `InPrivate` | `①P… +0x819` (읽고 우선순위 비트만 바꿔 되씀) | 기준: 같은 DWORD 를 쓰는 줄이 있으면 0 을 쓴 것으로 봄. 범위 밖에서 이 비트를 켜는 곳 `[추측]`: 작업 (`PspRemoveProcessFromJobChain`·`PspNotifyEmptyJobsInJobChain`), 피코 (`PspCreatePicoProcess +0x1c6`), 종료 (`PspProcessRundownWorker`), 메모리 한도 (`PsReportProcessMemoryLimitViolation`) |
| | `Flags` 16비트: `ProcessExiting` `ManageExecutableMemoryWrites` `VmDeleted` `OutswapEnabled` `Outswapped` `FailFastOnCommitFail` `SetTimerResolution` `BreakOnTermination` `DeprioritizeViews` `WriteWatch` `Reserved` `PdeUpdateNeeded` `VdmAllowed` `ProcessRundown` `ProcessSelfDelete` `SetTimerResolutionLink` | `①P… +0x7fe` | 실패 정리 길 `PspRundownSingleProcess` (`PAP +0x3727`) 도 `Flags` 를 `cmpxchg` 함 (안 탐) |

#### 2.2 조건부 안 탐 (30) — test.exe 가 왜 안 타나

| 칸 | 켜는 줄 | 조건 | test.exe 가 왜 안 탐 |
|---|---|---|---|
| `Flags2.ForceWakeCharge` | `①P… +0xd0d` (지역 비트 `0x40`) → `+0x1404 OR` | IFEO `ForceWakeCharge` 값 ≠ 0 | IFEO 키 없음 (3장) |
| `Flags2.CrossSessionCreate` | `①P… +0x156b` | `PspAttachSession` 갈래 (`+0x14f9~+0x1524`): 토큰 핸들 속성이 있는데 토큰 세션 ≠ 지금 세션, 또는 복제 길에서 세션이 다를 때 | 토큰 핸들 속성 없음 (`NtCreateUserProcess +0xbbd`), 복제 아님. 사진 `Session` 부모와 같음 |
| `Flags2.PropagateNode` · `ExplicitAffinity` | 부모 `Flags2` 비트를 지역 `[RSP+0xb4]` 로 (`+0x3b7~+0x3cc`, `+0x127f~+0x128b`) → `+0x1404 OR`; `PspReadIFEONodeOptions +0x90`; 둘을 지우는 줄 `+0x232d` | 부모에 비트 있음 / IFEO `NodeOptions` | 사진 8장 모두 0 (부모 cmd 에 없음 `[관찰 간접]`), IFEO 없음 |
| `Flags2.EnableReadVmLogging` | `①P… +0x2199` | 복제 길에서 부모에 비트 | 복제 아님 |
| `Flags.NoDebugInherit` | `①I… +0x322` | `DbgkCopyProcessDebugPort` 출력 바이트 ≠ 0 이고 `PspInsertProcess` 인자 6 비트 1 | 부모에 디버그 포트 없음 (아래 `DebugPort`) |
| `Flags.OverrideAddressSpace` | `①P… +0x13f6 OR [RSP+0xb0]` ← `+0xac7` (`PspSelectMachineForProcess` 출력 3번째 바이트 ≠ 0) · `+0x950` (복제 길 부모 비트) | 위 | 사진 `Flags` 비트 17 = 0 |
| `Flags.ProcessDelete` | `①P… +0xf93` (`[RSP+0xb0] |= 8`) → `+0x13f6` | 섹션 없는 복제 길 (`param_11 == 0` 또는 플래그 2) 이면서 `& 4` | 섹션이 있음. (비트 이름은 PDB 그대로 — 뜻이 어색해서 이 줄이 진짜 이 칸인지 `[추측]`) |
| `Flags.Wow64VaSpace4Gb` | `①P… +0x1015` · `MiComputeProcessUserVa +0xf0` | WoW64 x86 (`Machine` = `0x14c`) 과 32비트 주소 공간 | `Machine` = `0x8664` |
| `Flags.VmTopDown` | `MiComputeProcessUserVa +0x165` | 전역 `MmRegistryState+0x20` (`0x140fc5210`) ≠ 0 이고 최고 주소 > `0x80000000` | 사진 `Flags` 비트 21 = 0 → 전역이 0 `[관찰 간접]` (값 자체는 동적/Q79) |
| `ForkInProgress` | `MiCloneVads +0x172 · +0x57c` | `MiCloneProcessAddressSpace` (`①M… +0x3ee`) = 복제 길 | 섹션이 있어서 복제 아님 (Q35 의 `+0x2095` 쪽) |
| `Job` · `JobLinks` · `CommitChargeJob` | `①I… +0xd4 PspImplicitAssignProcessToJob` 아래 (`PspEstablishJobHierarchy`·`MmAssignProcessToJob` 등, **안은 안 읽음** `[추측]`) | 부모의 `Job` (`+0x2a0`) ≠ 0 (`PspInsertProcess +0x7d~+0x87`) | 사진 8장 모두 `Job` = 0 (sast/43 5.1) |
| `WoW64Process` | `①P… +0xfcc` (풀 'WowP') | 지역 플래그 0x40: 부모에 WoW64 (`+0x92b`) 또는 `PspSelectMachineForProcess` 출력 바이트 2 ≠ 0 (`+0xaa9`) | `Machine` = `0x8664`, 사진 `WoW64Process` 0 |
| `LockedPagesList` | `MiInitializeLockedPagesTracking +0x31` | `MmTrackLockedPages` (`0x140fc6bfc`) 비트 0 켜짐, 비트 28 꺼짐 (`①M… +0xca~+0x45e`) | 사진 0 → 꺼짐 (값은 동적/Q79) |
| `LargePrivateVadCount` | `MiInsertVad +0x23b` | 큰 페이지 VAD 일 때 | 처음 VAD 들은 큰 페이지 아님 |
| `AllowedCpuSets` · `DefaultCpuSets` · `…Indirect` (4) | 확장 칸 포인터 `①P… +0x694 · +0x6a7` (`Flags3.IndirectCpuSets` 와 함께 `+0x685`) ; 직접 칸은 `KeSetCpuSetsProcess (호출 +0x1a00)` | 프로세서 그룹 수 > 1 (`KeQueryMaximumGroupCount`, `+0x4a0`) / IFEO `AllowedCpuSets` | 그룹 1개 (`Flags3.IndirectCpuSets` 사진 0), IFEO 없음 |
| `Win32KFilterSet` | `①P… +0x1f11` | 속성 모음 `[+0x1b0]` 포인터 ≠ 0 (Win32k 필터 속성). 같은 구조체 비트가 `MitigationFlags` `0x4000`·`0x8000` (`+0x1edb · +0x1ef8`) 도 켬 | 속성 없음 (사진 `MitigationFlags` 0x20 뿐) |
| `DisabledComponentFlags` | `①P… +0x1f2b` | 속성 모음 `[+0x1b8]` 포인터 ≠ 0 | 속성 없음 |
| `SyscallProvider` · `SyscallProviderProcessLinks` · `SyscallProviderDispatchContext` | `①I… → PspInheritSyscallProvider (호출 +0x8f)` → `PspAttachProcessToSyscallProvider` | 부모의 `SyscallProvider` (`+0x7b8`) ≠ 0 (`PspInheritSyscallProvider +0x18~+0x2a`) | 부모 cmd 에 없음 `[추측]` (child 사진 0) |
| `MitigationFlags3` (+`Values`) | `PspApplyMitigationOptions +0x931 · +0xb9a · +0xea7` | 합친 옵션의 해당 비트 (IFEO·시스템 기본·부모 상속) | 사진 0 → 옵션 꺼짐 |
| `Flags4.ImageExpansionDisable` | `①P… +0x660` (ProcessFlags 비트 19) · `+0xea2 · +0xecd` | ProcessFlags 비트 19 / IFEO `ImageExpansionMitigation` = 2 | 둘 다 아님 |
| `Flags4.SessionFirstProcess` | `①M… → PsSessionCreate (호출 +0x39a) → PspMarkSessionReferenceProcess +0x38` | ProcessFlags 비트 7 = "세션 새로 만들기" (`+0x392 TEST AL,AL; JNS`) | smss 만 켬 |

#### 2.3 아무도 안 씀 — 만들기 길 안 (28)

`ExceptionPortValue.ExceptionPortState`, `MmReserved`, `RotateInProgress`, `CloneRoot`, `MmReserved2`, `Win32WindowStation`, `Spare1`, `PageDirectoryPte`, `SecurityPort`, `WorkingSetWatch`, `CommitChargeLimit`, `VadPhysicalPagesLimit`, `SvmProcessLock`, `SvmLock`, `LastFreezeInterruptTime`, `EnclaveTable`, `EnclaveNumber`, `HighPriorityFaultsAllowed`, `DxgProcess`, `Spare0`, `ProcessTimerDelay`, `VirtualTimerListLock`, `SupervisorDeviceAsid`, `SupervisorSvmData`, `FreezeWorkLinks`, `Flags3.SubsystemProcess`, `Flags3.VmProcessorHostTransition`, `Flags3.EnableProcessImpersonationLogging`. 근거: `PspAllocateProcess` 와 위 callee `.asm` 의 `[reg+칸]` 쓰기 전수에 없고, 전체 훑기에서 이름 있는 EPROCESS 쪽 쓰기가 안 보임 (또는 오탐뿐: 예 `SecurityPort +0x348` 은 세션 전역·PEB 칸, `WorkingSetWatch +0x2c0` 은 `MmCreatePeb` 의 PEB 칸). `DxgProcess` 는 `dxgkrnl.sys` 가 쓸 칸 `[추측]`.

#### 2.4 범위 밖 길에서 씀 `[추측]` (52)

이름 있는 쓰는 함수가 만들기 길 밖에 있는 칸 (**전체 훑기, 오프셋 숫자 일치라 틀릴 수 있음**):

| 칸 | 쓰는 곳 (범위 밖) |
|---|---|
| `Flags3.Crashed` · `.HangCount` · `.GhostCount` · `.PrefilterException` | `PsSetProcessFaultInformation`, `DbgkForwardException` |
| `Flags3.JobVadsAreTracked` · `.VadTrackingDisabled` | `MmLinkJobProcess +0x12f`, 이름 없는 조각 `FUN_140b18863 +0x19/+0x21` |
| `Flags3.ReplacingPageRoot` · `.RelinquishedCommit` | `MiStealPage +0x140b`, `MiReAcquireOutSwappedProcessCommit +0xd9` |
| `Flags3.HighGraphicsPriority` | `NtSetInformationProcess` (짐작) |
| `Flags3.CommitFailLogged` · `.ReserveFailLogged` | `MiLogCommitRequestFailed`, `MiLogReserveVaFailed` |
| `Flags3.AllImagesAtBasePristineBase` | `VmCreateMemoryProcess +0x17a` |
| `Flags3.ForegroundExternal` · `.ForegroundSystem` | `PspSetProcessForegroundBackgroundRequest` |
| `Flags3.HighMemoryPriority` | `SmProcessStoreMemoryPriorityRequest +0x105` |
| `Flags3.SecurityDomainChanged` | `PspCombineSecurityDomains +0x273`, `PspOneDirectionSecurityDomainCombine +0x1bc` |
| `Flags3.AltSyscall` · `Flags3.TimerResolutionIgnore` | `PspSyscallProviderOptIn +0x6b`, `ExUpdateTimerResolutionPolicy` |
| `LastThreadExitStatus` · `ExitTime` · `ThreadListLock` | `PspExitThread`, `PspExitLastThread`, `PspInsertThread` (푸시락 잡았다 놓음) |
| `VadPhysicalPages` | `MiCleanProcessPhysicalPages`, `MiAllocateUserPhysicalPages` |
| `TimerResolutionLink` · `…StackRecord` · `RequestedTimerResolution` · `SmallestTimerResolution` · `KTimerSets` · `KTimer2Sets` · `ThreadTimerSets` | `NtSetTimerResolution`, `ExCleanTimerResolutionRequest` 와 타이머·스케줄러 카운터 |
| `InvertedFunctionTableLock` | `RtlInsertDynamicUserFunctionTable` |
| `DeviceAsid` · `SvmData` | `ExpShareAddressSpaceWithDevice` |
| `PicoContext` · `VmContext` | `PsCreateMinimalProcess`/피코 쪽, `VmpProcessContextSetup` |
| `TotalUnbiasedFrozenTime` | `PsThawMultiProcess` |
| `.LastAppStateUptime` · `.LastAppState` | `NtSetInformationProcess` 쪽 `[추측]` |
| `DiskIoAttribution` | `IoSetDiskIoAttributionOnProcess` |
| `Read/Write/OtherOperationCount` · `Read/Write/OtherTransferCount` (6) | I/O 완료 `IopUpdateIrpTransferCount`, `KeFoldProcessStatisticsThread` |
| `CoverageSamplerContext` · `MmHotPatchContext` | `EtwpCovSampProcessEnsureContext`, `MiGetProcessHotPatchContext` |
| `PathRedirectionHashes` | `PspRundownSingleProcess` (실패 정리 길이기도 함 — `PAP +0x3727`), `RtlSetProcessPlaceholderCompatibilityMode` |
| `SyscallUsage` · `…Values` | `EtwTiLogSyscallUsage` |
| `Execution` · `ThreadIndexTable` · `Flags4.MinimalTerminate` | `PspChangeProcessExecutionState`, `PspQueryThreadIndexInformation`, `PsTerminateMinimalProcess` |

#### 2.5 test.exe 길에서 조건부로 안 타는 큰 갈래 (한 번에)

| 갈래 | 가르는 곳 | test.exe |
|---|---|---|
| 작업 (Job) | `PspInsertProcess +0x7d` 부모 `Job` | 안 탐 (사진 `Job`=0) |
| 디버그 포트 | `DbgkCopyProcessDebugPort` 가 부모 `+0x308` 복사 / 디버그 객체 핸들 (`PspInsertProcess +0x2f3`) | 안 탐 (사진 `DebugPort`=0) |
| WoW64 | `Machine==0x14c` · 지역 플래그 0x40 | 안 탐 (`Machine`=`0x8664`) |
| 보호 프로세스 | `Protection`·`SignatureLevel`·`SectionSignatureLevel` 입력 | 값 0 을 씀 (cmd 가 보호 프로세스 아님 `[관찰 간접]`) |
| 서버 실로 (`ServerSilo`) | `CreateInfo+0x1a8` | 0 을 씀 |
| 피코 | `PspCreatePicoProcess` — 다른 시스템 콜 | `NtCreateUserProcess` 길 아님 |
| 서브시스템 프로세스 (`Flags3.SubsystemProcess`) | 만들기 길에 켜는 줄 없음 | 아무도 안 씀 |
| 복제 (`ForkInProgress`, `puVar37&4`) | 섹션 없음 | 안 탐 |
| 세션 만들기 | ProcessFlags 비트 7 | 안 탐 (smss 만) |
| 세션 다름 → `PspAttachSession` | 토큰 핸들 속성 / 복제 | 안 탐 |

### 3. 갈림길과 입력 — IFEO 와 간접 호출

#### 3.1 IFEO 읽기 — 어느 칸에 들어가나

- 열기: `PAP +0xa5c RtlpOpenImageFileOptionsKeyEx` (이미지 이름 `CreateInfo+0xf0` → 키 핸들 `CreateInfo+0xc8`). **실패 `0xC0000034` 이면 `CreateInfo+0x10 |= 0x40` (`+0xa6c`), `+0xc8` = 0 (`+0xa71`).** 그 뒤 `+0xad0 TEST RCX,RCX; JZ → +0xfa5` 로 아래 IFEO 값 읽기 구간을 통째로 건너뛴다.
- 값 이름 (PE 에서 직접 읽은 문자열) 과 들어가는 곳:

| IFEO 값 | 읽는 줄 | 효과 | 들어가는 칸 |
|---|---|---|---|
| `Debugger` (REG_SZ, 비어있지 않음) | `PAP +0xb11` | 만들기 포기 — `PspUpdateCreateInfo(5)` 후 실패 정리 (`+0xb72`~) | 칸 없음 |
| `AppExecutionAliasRedirect` = 1 | `+0xb5f` | 같은 포기 | 칸 없음 |
| `UseLargePages` ≠ 0 (+ 하위 키 `PspLargePageDLLKeyName` 아래 `ntdll.dll`) | `+0xbab`, `+0xc75` | 지역 플래그 `0x10` · `0x20` → 주소 공간·이미지 붙이기 쪽 인자 (`[RSP+0x8b0]`) | EPROCESS 칸 아님 |
| `NodeOptions` | `PspReadIFEONodeOptions` (호출 `+0xccd`, 호출 조건 `[RSP+0x140]==0`) | 노드 번호 → `KeInitializeProcess` 5번째 인자 (노드); 상위 바이트 ≠ 0 이면 `Flags2.PropagateNode` (`PspReadIFEONodeOptions +0x90`) | KPROCESS `IdealGlobalNode`, `Flags2.PropagateNode` |
| `ForceWakeCharge` ≠ 0 | `+0xd3b` | 지역 `0x40` → `+0x1404` | `Flags2.ForceWakeCharge` |
| `AllowedCpuSets` (이진) | `+0xd34`~`+0xdb9` (풀 'PsCs') | 길이 ≤ 0x100, 8의 배수 → `KeSetCpuSetsProcess (호출 +0x1a00)` | 허용 CPU 집합 칸 |
| `PerfOptions` 하위 키: `IoPriority` · `PagePriority` · (셋째 값) | `PspReadIFEOPerfOptions` (호출 `+0xe54`) → `PspApplyIFEOPerfOptions` (호출 `+0x19db`) | `Flags` 27:3 (`+0x5a` cmpxchg), `Flags2` 12:3 (`+0x91` cmpxchg, 기본보다 낮을 때만), `PspSetProcessPriorityClass` | `Flags.DefaultIoPriority`, `Flags2.DefaultPagePriority`, `PriorityClass` |
| `ImageExpansionMitigation` = 2 | `+0xe8f` | `Flags4 |= 4` (`+0xea2`) | `Flags4.ImageExpansionDisable` |
| `MitigationOptions` | `PspReadIFEOMitigationOptions` (호출 `+0x1a8b`) → `PspReadOptionsMapFromIFEO` | 옵션 24바이트 → 시스템 기본·부모와 합침 (`PspInheritMitigationOptions` 호출 `+0x1aea`·`+0x1be0`, `PspHardenMitigationOptions` `+0x1c82`) → `PspApplyMitigationOptions (호출 +0x1eb3)` | `MitigationFlags`·`2`·`3`, `SignatureLevel`·`SectionSignatureLevel` (조건부 `+0xc35 · +0xc41 · +0xb1d · +0xc4d`) |
| `MitigationAuditOptions` | `PspReadIFEOMitigationAuditOptions` (호출 `+0x1afa`) | 감사 옵션 → `PspApplyMitigationOptions` 인자 | `MitigationFlags*` 감사 비트 |
| `AntiTamperOptions` | `PspReadIFEOAntiTamperOptions` (호출 `+0x1e68`), 조건 `Feature_OutsideInProtection` + 속성 비트 36 | `SeConstructAntiTamperPolicy (호출 +0x264a)` → 토큰 | EPROCESS 칸 아님 (`_TOKEN` `AntiTamper*`, sast/43) |

- **test.exe 는 IFEO 키가 없을 때의 길.** `PspReadOptionsMapFromIFEO` 는 `CreateInfo+0xc8` 이 0 이면 `0xC000000D` 로 바로 돌아간다 (`+0x1f~+0x2b`) — 그래서 `Mitigation*`·`Audit` 읽기도 값 없음, 나머지 값 읽기는 `+0xfa5` 로 건너뜀. 칸 값은 부모·기본 (`PspSystemMitigationOptions`) 에서만 온다. **키 없음 표시 `CreateInfo+0x10` 비트 `0x40` 은 `PspSetupUserProcessAddressSpace` → `PspCopyAndFixupParameters +0x74~+0x82` 가 프로세스 매개변수 `Flags |= 0x4000` 으로 옮기고** (조건: `CreateInfo+0x11` 비트 0 이 꺼져 있을 때), 이것이 run46 의 사진 `Flags=0x4001` 의 `0x4000` 이며 ntdll `LdrpInitializeExecutionOptions` 가 IFEO 를 건너뛰는 이유다. → **run46 ntdll 쪽과 같은 결과** (이 exe 는 IFEO 키가 없다). 단 run46 이 본 것처럼 ntdll 힙 관리자는 IFEO **바탕 키** 를 따로 열어 `GCInterval` 을 읽는다 (커널의 이 읽기와 별개). `CreateInfo` 인자 연결 (`PspCopyAndFixupParameters` 의 `param_3` = `CreateInfo`) 은 `.asm` 에서 짐작 `[정적 + 추측]`.
  - 한 줄 더: 이미지 헤더 때문에 켜진 `MitigationFlags=0x20` (HIGH_ENTROPY_VA) 은 IFEO 가 아니라 **PE `DllCharacteristics`** (`CreateInfo+0x66`) → `PspApplyMitigationOptions` 인자 비트 → 줄 `+0xd1`.

#### 3.2 간접 호출 두 곳

| 호출 | 읽는 변수 | 대상 | 쓰는 칸 |
|---|---|---|---|
| `PAP +0x1215 cfg:@0xf04d28` (ret `+0x121a`) | `SeCiCallbacks+0xc8` (`PAP +0x1201 MOV RAX,[0x140f04d28]`) | CI 가 등록한 콜백 `[추측]` (`_refs/SeCiCallbacks.tsv`: 이 칸 읽기는 이 줄뿐, 쓰는 줄은 표 통째 복사라 안 보임) | **EPROCESS 칸 안 씀.** 인자 `RCX=&[RSP+0x5a0]` (0x218B 지역 버퍼) — 실패·정리 블록 (`+0x1189~+0x121c`) 안, 버퍼를 비우는 호출로 보임 `[추측]` |
| `PAP +0x17bf cfg:@0xf04d20` (ret `+0x17c4`) | `SeCiCallbacks+0xc0` (`PAP +0x17a7`) | 같음 `[추측]` | **EPROCESS 칸 안 씀.** 인자 `RCX=CreateInfo+0xb8` (이미지 파일 객체), `RDX=&[RSP+0x5a0]`; 결과 버퍼 (0x218B) 가 `PspInitializeProcessSecurity` 의 인자로 넘어가 토큰 쪽 입력이 됨 |

- 둘 다 대상은 정적으로 못 좁힘 → **Q35 의 "간접 호출 2곳" 그대로** (plan Q35). 이 바퀴에서 새로 아는 것: 칸을 직접 쓰지 않는다는 점과 인자.

### 4. 줄기와 경로

- **줄기** (칸 값을 정하는 함수까지 끝까지): `PspAllocateProcess` 본문, `MmCreateProcessAddressSpace`, `MiCreateNewProcessTopLevelMappings`, `PsJoinSession`, `PsReserveSessionMembership`, `MmInitializeProcessAddressSpace`, `MiComputeProcessUserVa`, `MiInsertProcessVads` → `MiInsertVad`, `MiInitializeProcessBottomUpEntropy`, `PspInheritQuota`, `PspInitializeFullProcessImageName` → `PspSetProcessShortName`, `PspInitializeProcessSecurity` (보안 도메인 줄), `PspApplyMitigationOptions` (켜는 줄), `PspSelectMachineForProcess` (인자만), `PspInsertProcess` (+ `DbgkCopyProcessDebugPort`).
- **줄기 밖 (이름만)**: `memset`, `ExAllocatePool2`/`ExAllocatePoolWithTag`, `ObCreateObjectEx`/`ObpAllocateObject`, `ObRetagReferences`, 락·참조 (`ExAcquireSpinLockExclusive`, `PspLockProcessListExclusive`, `ObfReferenceObject*`), `PspChargeQuota` 안쪽·`PspExpandQuota`, `MiChargeProcessCommitment` 안쪽, 그룹·친화도 도우미 (`KeSelectGroupFromNode`, `KeAddProcessorAffinityEx`, `KiCopyAffinityEx`, `KeQueryAffinityProcess`, `KeSelectNodeForAffinity`), `PspAttachSession`·`PspAssignProcessQuotaBlock` 안쪽 (안 탐), `MiInsertVadCharges`·`PspSelectNodeForProcess` 안쪽, `PspImplicitAssignProcessToJob` 아래, `PspUpdateCreateInfo` 안쪽, `MiCloneProcessAddressSpace` 아래.
- 칸마다 경로·근거는 위 1.1 표 (3장 양식). 0 칸 묶음은 2.1~2.4.

## 동적분석으로 확인해야 할 것

"사진" = 이미 있는 덤프 (`ms/logs/04_baggage-read_*.txt`, `ms/dump/03_baggage-snap/`) 로 답할 수 있는지. 번호는 이 조각 안 번호 (E1~E9).

| # | 질문 | 추천 멈춤점 / 찍을 것 | 사진으로? |
|---|---|---|---|
| E1 | 자식 값이 부모 (cmd) 값 복사가 맞나: `SecurityDomain`·`ParentSecurityDomain`, `Flags` 비트 27:3, `Flags2` 비트 12:3, `DefaultHardErrorProcessing`, `PriorityClass`, `QuotaBlock`, `Session`, `Job`, `DebugPort`, `SyscallProvider`, `Protection`/`SignatureLevel` | 부모 cmd EPROCESS (`!process 19a8 0`, sast/43 D1 과 같은 방법) 에 `dt nt!_EPROCESS` 후 자식 K1 값과 대조 | **가능성 있음** (K1 덤프에 부모 EPROCESS 가 있으면 새 요청 없이 Q79 읽기에 추가) |
| E2 | 커널 전역 값: `PspSystemPartition` (`PartitionObject` 대조), `PspSystemMitigationOptions` (24B — `MitigationFlags2` 비트 30 의 뿌리), `nt+0xfc6ac8` 바이트 (`& 0x30`), `MmRegistryState+0x20` (`nt+0xfc5210`, `VmTopDown` 조건), `MmTrackLockedPages` (`nt+0xfc6bfc`) | `dq`/`dd`/`db` 한 번씩 | **가능** (전역은 덤프에 있음) → Q79 |
| E3 | IFEO 키 없음의 직접 증거: `PAP +0xa61` 직후 `eax = 0xC0000034` 이고 `CreateInfo+0x10` 비트 `0x40` 이 켜졌나 | `nt!PspAllocateProcess+0xa61` (rva `0xa02fc9`) 에서 `r eax`, `db @r15+0x10 L1` (r15 = `CreateInfo`). 조건: 현재 프로세스 이미지 = cmd.exe (새 프로세스는 아직 이름 없음) | 불가 (인자·상태 사라짐). 결과는 사진 P1 `Flags=0x4001` 로 간접 확인됨 |
| E4 | 간접 호출 `SeCiCallbacks+0xc0/+0xc8` 대상과 시점 | Q35 그대로 | 불가 |
| E5 | `SharedCommitCharge` K1 `0x2b` 를 올린 줄 | `nt!MiInsertSharedCommitNode+0x33e` 에서 `k` (조건: `rcx` 가 새 EPROCESS 일 때만) | 불가. 낮은 우선순위 |
| E6 | `NumberOfPrivatePages` K1 `0x20` 이 페이지 오류로 오른 것 | 새 프로세스 만든 직후 `ba w8 <eproc>+0x288` 콜스택 | 불가. 낮은 우선순위 |
| E7 | `MitigationFlags=0x20` 을 실제로 켠 줄: `PspApplyMitigationOptions +0xd1` 인가 `MiInitializeProcessBottomUpEntropy +0x19d` 인가 | 두 줄에 걸고 먼저 오는 쪽 확인 | 불가 (값이 같음). 낮은 우선순위 |
| E8 | 쿼터 두 칸 (`0x4d8`·`0x2998`) 의 종류 번호 ↔ 칸 대응과 어느 호출이 만들었나 | `nt!PspChargeQuota+0x95` 입구에서 `r8d`(종류)·`r9`(양) 기록 | 불가. 낮은 우선순위 |
| E9 | `Job`·`DebugPort` 부모 값이 정말 0 인가 (안 탐 확정) | E1 에 포함 | **가능성 있음** (E1 과 같은 덤프) |

- 급한 것은 없다. E1·E2·E9 는 사진 (Q79 에 읽기 추가) 으로 닫히고, 나머지는 닫지 않아도 칸 표는 완성된다.
- 디버거가 붙어서 달라지는 점: 없음 (모든 근거가 정적 + 이미 찍은 사진).

## Q78c2b 로 넘길 것

- 이 바퀴에서 **우연히 본** 뒤 구간 줄:
  - `Flags.ProcessInserted` (`K2~K3`, 비트 26) ← **`PspInsertProcess +0x202`** (`lock or 0x4000000`, `①I…`). 사진 `K2~K3` 와 맞음 → 닫을 수 있음.
  - `ActiveThreadsHighWatermark` (`K2~K3`) ← `PspInsertThread +0x678`; `Flags4.ThreadWasActive` ← `+0x67f` (`BTS.LOCK`); `Flags2.PrimaryTokenFrozen` (`P0~P1`) 을 켜는 줄이 둘 — `PspDisablePrimaryTokenExchange +0x22` (기존) 와 **`PspInsertThread +0x77b`** (`lock bts [+0x1f0],0xf`, 조건은 안 읽음).
  - `ExceptionPortData` (`K3~P0`): `PspAllocateProcess` 에 직접 쓰는 줄 없음 (`+0x240` 은 모두 스택). `Flags2.EnableWriteVmLogging`, `Flags3.Enable…Logging` 비트, `WakeChannel`·`WakeInfo`, `Flags.CreateReported`·`LaunchPrefetched`·`ImageNotifyDone`: 만들기 길에서 못 찾음 → Q78c2b 가 `NtSetInformationProcess`·프로세스 알림 쪽을 볼 것.
- 이미 아는 것 다시 안 봄: `BamPpmPolicy`·`PpmPolicyLock` (sast/43 `PspSetProcessPpmPolicy +0x70~+0xa5`), `OwnerProcessId` `P1a~P1b`, `Win32Process`.

## 정정 · 새로 알게 된 것 (flow-writer 용)

1. baggage 3.11 "(run49) Q78c2 에서 볼 것" 의 `PriorityClass` "처음 넣는 줄 못 찾음" → 닫힘 (`PspAllocateProcess +0x1931 · +0x194f`). `+0x540` = `MmProcessLinks`, `+0x3d8` = `CommitCharge`, `+0x410` = `Vm.Instance.VmWorkingSetList` (이름 모름 → 닫힘).
2. `Session` (`+0x2e8`) = **호출한 쪽 프로세스** 의 세션 (`PsReserveSessionMembership`). `InheritedFromUniqueProcessId` 와 `Flags`·`Flags2` 우선순위 비트는 **부모** 에서 옴. 두 개가 다르다는 점을 3.11 줄에 쓸 것.
3. `AddressSpaceInitialized` 의 두 비트가 **서로 다른 함수** (11: `MiCreateNewProcessTopLevelMappings +0x72`, 10: `MmInitializeProcessAddressSpace +0x117`).
4. `SecurityDomain` 기본은 **부모 값 복사**; 새 번호는 예외 갈래 (3.11 메모의 "부모 값 또는 +1" 을 조건과 함께 고침).
5. 객체 할당 이름 어긋남 정리: `ObpAllocateObject` 가 부르는 건 `ExAllocatePoolWithTag` (겉포장), 그 안에서 `ExAllocatePool2` 를 부른다. `PspAllocateProcess` 의 직접 할당은 `ExAllocatePool2`.
6. IFEO 키 없음 → `CreateInfo+0x10` 비트 `0x40` → 프로세스 매개변수 `Flags |= 0x4000` (`PspCopyAndFixupParameters +0x74~+0x82`). baggage `ProcessParameters.Flags` 쓰는 곳 (run45 `RtlpInitParameterBlock` 등) 에 이 커널 줄을 추가하면 run46 의 `0x4001` 이 끝까지 이어짐.
7. 지역 플래그 비트 `4` = "복제 (섹션 없음)". run49 가 이 비트를 "부모 있음" 으로 읽은 곳이 있으면 고칠 것 (sast/43 1.1 의 `[RSP+0xf8]=4` 는 **다른 변수** — `EBX & 0x10002 == 0` 이고 부모가 있을 때 — 라 그대로 맞음).
8. 새 칸 표 없는 짐 후보 (7.6 에 올릴지는 사용자와): 확장 영역 안 `_PROCESS_DISK_COUNTERS` (0x28B), `_PO_PROCESS_ENERGY_CONTEXT` (0x1e8B), `_PROCESS_NETWORK_COUNTERS` (`PDB` 에 있음, 크기는 안 봄), 풀 블록 `_MMWSL_INSTANCE` (0x580B, 'MiP2'), 쿼터 블록 `_EPROCESS_QUOTA_BLOCK` (PDB 정의 없음, 부모와 공유), 세션 구조 `_PSP_SESSION_SPACE`.

## 도구 평가 · 하네스 제안 (스크립트는 안 고쳤다)

- **고칠 버그는 없다.** `run_ghidra.sh export` 두 번, `pdb_struct.py`, `pdb_aliases.py`, `refs` 모두 문제없이 돌았다.
- **제안 (sast/43 제안 1 을 다시, 이번엔 더 구체적으로)**: "이 구조체의 **모든 칸** 에 대해 누가 쓰나" 가 필요했다. 이번에도 `objdump -d` 한 번 (1초, 106MB) + `_index.tsv` 로 함수 이름을 붙이는 임시 스크립트 (`scratchpad` 에만 있음: `ntscan.py` 함수 경계 · `hits.py` 칸 오프셋 쓰기 · `zeroscan.py` 160칸 전체 · `pewide.py`) 를 짰다. 제안: `scripts/field_writers.py <바이너리> --struct _EPROCESS [--cells K1.tsv]` — `pdb_struct.py` 의 직접 칸 오프셋 전체에 대해 `[reg+오프셋]` 쓰기 명령 (mov/or/and/bts/btr/xadd/cmpxchg, `lock` 접두사 포함) 을 `칸 / 함수+오프셋 / 명령 / 지도 깊이` TSV 로 뽑기. 오탐 (KTHREAD·PEB·ERESOURCE 가 같은 숫자 사용 — 이번에 `KeWaitForMutexObject`·`MmCreatePeb`·`ExReleaseResourceLite` 가 걸림) 이 많으니 "후보 줄" 로만 쓰고 `.asm` 로 확인하는 약속과 함께, 그리고 **레지스터가 어디서 왔는지** (예: `MOV R14,RCX` 가 첫 인자인지) 를 같이 보여 주면 오탐을 많이 줄일 수 있다.
- 겹침: `pdb_struct.py` 의 앞 바퀴 제안 (`--expand`) 은 이번에도 `Flags`·`Flags2`·`Flags3`·`MitigationFlags*` 비트에 필요했다 — sast/38 `struct__EPROCESS.tsv` 로 대신 봤다.

## 판정

**분석성공.** 요청서 질문 1~4 모두 답했다. 212칸 → 아는 칸 (nt) 50 · 0 을 씀 50 · 조건부 안 탐 30 · 아무도 안 씀 28 · 범위 밖 길 53 · 여전히 모름 1. 버전은 사진 값과 칸마다 일치. 정적으로 못 닫은 것: `SharedCommitCharge` K1 값을 만든 줄, `NumberOfPrivatePages` 처음 값 (`[추측]`), `MitigationFlags` `0x20` 두 후보 줄 중 어느 쪽, `MitigationFlags2` 비트 30 의 뿌리, `SecurityDomain` 이 부모와 같은 값인지, 간접 호출 `SeCiCallbacks+0xc0/+0xc8` 대상 (Q35), `Job` 등 callee 안쪽, 그리고 "범위 밖 길" 52칸·"아무도 안 씀" 28칸의 근거가 전체 훑기 (오탐 가능) 라는 점.

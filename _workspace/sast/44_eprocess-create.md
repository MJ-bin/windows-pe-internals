# 44 eprocess-create — 정적 분석 요청

- 날짜: 2026-10-30
- 스프린트: 03_sprint (짐 지도 바퀴, 예정 run50)
- 대기열: **Q78c2a** — Q78c2 (`_EPROCESS` 모르는 칸 241) 를 그 자리에서 나눈 첫 조각 (오케스트레이터 기본값 2026-10-30: 시간 순서 — Q78c2a = 만들 때 (`K1 이전` 52) + 끝까지 0 (160) 을 **만들기 길 안에서** 가름, Q78c2b = 그 뒤 구간 (`K2~K3` 3 · `K3~P0` 11 · `P0~P1` 9 · `P1~P1a` 5 · `P1a~P1b` 1 = 29) + `OwnerProcessId` `P1a~P1b` 바뀜 + 옛 Q65 몫)
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` 3.11 `_EPROCESS` (아는 칸 22, **"(run49) Q78c2 에서 볼 것"** 메모 — sast/43 이 지나가며 본 쓰는 줄, 그대로 확인해서 옮김), 7.2 `_EPROCESS` 모르는 칸 이름 (구간별)
  - `runs/02_sprint/run10-allocate-process.md` (sast/04 — `PspAllocateProcess` 본문·callee), `run47-section-chain.md` (sast/41 넘김: `SectionObject`·`ImageFilePointer`), `run49-kprocess-token-htable.md` (sast/43 넘김, 3.11 메모와 같음)
  - 이전 요청 sast/43 (Q78c1 양식 — 같은 방식으로)

## 범위

- `_EPROCESS` 모르는 칸 중 **212칸**:
  - `K1 이전` 52 (7.2 그대로): Flags2*, Flags2.DefaultPagePriority, Flags*, Flags.AddressSpaceInitialized, Flags.ProcessInSession, Flags.HasAddressSpace, Flags.DefaultIoPriority, ProcessQuotaUsage*, ProcessQuotaPeak*, PeakVirtualSize*, VirtualSize*, SessionProcessLinks, NumberOfPrivatePages*, SectionObject, InheritedFromUniqueProcessId, Session, QuotaBlock, ImageFilePointer, ImageFileName, PriorityClass, SeAuditProcessCreationInfo, HighestUserAddress, DefaultHardErrorProcessing, CommitCharge*, CommitChargePeak*, Vm*, MmProcessLinks, ExitStatus, VadRoot*, VadHint*, VadCount*, AlpcContext*, SvmProcessDeviceListHead, DiskCounters, EnergyContext, CreateInterruptTime, CreateUnbiasedInterruptTime, LastAppStateUpdateTime, SharedCommitCharge*, SharedCommitLinks*, Machine, MmSlabIdentity, VirtualTimerListHead, MitigationFlags, MitigationFlagsValues, MitigationFlags2, MitigationFlags2Values, PartitionObject, SecurityDomain, ParentSecurityDomain, PageCombineSequence, NetworkCounters
  - `끝까지 0` 160 (7.2 그대로 — 대부분 `Flags`·`Flags2`·`Flags3` 비트, 카운터, 종료·작업(Job)·디버그 칸)
- **끝까지 0 160칸은 만들기 길 (`NtCreateUserProcess → PspAllocateProcess` 와 그 callee, `PspInsertProcess` 까지) 안에서만 본다**: 쓰는 줄이 있으면 (0 을 씀 / 조건부 안 탐) 줄과 조건, 없으면 "아무도 안 씀 (만들기 길 안)" — run47·run49 와 같은 세 가지. 종료·디버그·작업 같은 범위 밖 길에서 쓰는 칸은 이름만 ("범위 밖 길에서 씀 `[추측]`").
- 몸통은 `PspAllocateProcess +0x534 ObCreateObjectEx` (0 채움 — run49 에서 `ObpAllocateObject +0x191` → `ExAllocatePoolWithTag +0x48` 로 확인됨) 뒤 `+0x55f memset` 등. 0 채움 근거는 그대로 써도 된다.
- 칸 이름·오프셋은 `pdb_struct.py ntoskrnl.exe _EPROCESS` (이름 없는 공용체 안쪽 — `Flags`·`Flags2`·`Flags3`·`MitigationFlags*` 비트 — 는 sast/38 `ms/disassemble/ntoskrnl/_baggage/struct__*.tsv` 로).
- **범위 밖**: 뒤 구간 29칸 (Q78c2b), `_KPROCESS` 등 앞 조각에서 닫은 구조체, `Vm` (`_MMSUPPORT_FULL`)·`VadRoot` 안쪽 내용 (칸 표가 없는 짐 — 이름만), 주소 공간 만들기의 페이지 표 내용.

## 알고 싶은 것

1. **`K1 이전` 52칸 쓰는 줄** — 칸마다 함수+오프셋, 값의 출처 (부모 / 인자 / 섹션 정보 / IFEO / 계산 / 상수).
   - 3.11 "(run49) Q78c2 에서 볼 것" 메모의 줄 (`SecurityDomain`·`ParentSecurityDomain` `PspInitializeProcessSecurity +0x1a0 · +0x1a7`, `PriorityClass` `PspSetProcessPriorityClass +0x72` 와 K1 값 2 를 처음 넣는 줄, `Flags.AddressSpaceInitialized` bit 11 `MiCreateNewProcessTopLevelMappings +0x72`, `PspAllocateProcess +0x564 ~ +0x5fc` 초기화 묶음) 을 확인해서 옮길 수 있게.
   - sast/41 넘김: `SectionObject`·`ImageFilePointer` 쓰기 줄.
   - `InheritedFromUniqueProcessId` (부모 PID), `Session`·`SessionProcessLinks` (`PspAttachSession`), `QuotaBlock`·`ProcessQuota*` (`PspAssignProcessQuotaBlock`·`PspInheritQuota`), `ImageFileName`·`SeAuditProcessCreationInfo` (`PspInitializeFullProcessImageName`), `Mitigation*` (`PspReadIFEOMitigationOptions`·`PspInheritMitigationOptions`·`PspHardenMitigationOptions`·`PspApplyMitigationOptions`), `Machine` (`PspSelectMachineForProcess`), `EnergyContext` (`PoEnergyContextInitialize`), `CreateInterruptTime`·`CreateUnbiasedInterruptTime` (`KiQueryUnbiasedInterruptTime +0x27ff` 근처), `Vm`·`VadRoot`·`VadHint`·`VadCount`·`CommitCharge*`·`VirtualSize`·`HighestUserAddress`·`MmProcessLinks`·`MmSlabIdentity`·`PageCombineSequence` (`MmCreateProcessAddressSpace`·`MmInitializeProcessAddressSpace`·`PspSetupUserProcessAddressSpace`), `PartitionObject` (`PsReferencePartitionSafe +0x830`), `ExitStatus`·`DefaultHardErrorProcessing`·`AlpcContext`·`DiskCounters`·`NetworkCounters`·`VirtualTimerListHead`·`SvmProcessDeviceListHead`·`LastAppStateUpdateTime` (초기화 묶음 후보).
   - `*` 칸 (사진에서 뒤에 또 바뀜) 은 처음 값을 쓰는 줄만. 뒤에 바꾸는 쪽은 이름만 (Q78c2b 또는 범위 밖 길).
2. **끝까지 0 160칸 가르기** — 만들기 길 안에서 세 가지 (0 을 씀 / 조건부 안 탐 + 조건 / 아무도 안 씀). 비트 칸은 같은 바이트·DWORD 를 쓰는 줄 (예: `Flags` 를 통째로 쓰는 줄) 이 있으면 그 줄이 0 을 쓴 것으로 본다 — 판단 기준을 결과서에 한 줄로.
   - test.exe 길에서 조건부로 안 타는 갈래 (작업(Job), 디버그 포트, WOW64 (`WoW64Process`), 보호 프로세스 (`Protection`·`SignatureLevel`·`SectionSignatureLevel`), 실로 (`ServerSilo`), 피코, 서브시스템 프로세스 등) 는 조건과 test.exe 가 왜 안 타는지 (사진·인자·IFEO 값).
3. **갈림길과 입력** — IFEO 읽기 (`RtlpOpenImageFileOptionsKeyEx +0xa5c`, `RtlQueryImageFileKeyOption` 여럿, `PspReadIFEONodeOptions`·`PspReadIFEOPerfOptions`·`PspReadIFEOMitigationOptions`·`PspReadIFEOAntiTamperOptions`) 가 어느 칸에 들어가나, test.exe 는 IFEO 키가 없을 때의 길 (run46 ntdll 쪽과 같은 결과인지). 간접 호출 `+0x1215 cfg:@0xf04d28`·`+0x17bf cfg:@0xf04d20` 이 쓰는 칸 (대상은 정적으로 좁히고 못 정하면 동적 — Q35 와 같은 질문이면 Q35 로).
4. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). 경로는 `NtCreateUserProcess` 부터 지도 기준. 줄기 위는 끝까지, 줄기 밖·공용 부품 (`memset`, `ExAllocatePool*`, 락, 참조 수, `RtlCopyMemory`, 그룹·친화도 도우미) 은 이름만.

## 대상

rva 는 `ms/decomp/ntoskrnl/_index.tsv` 기준 (2026-10-30 확인). **이미 `.c` 가 있는 함수는 다시 뽑지 않는다** (뽑기 전에 있는지 확인).

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntoskrnl | `PspAllocateProcess` | (sast/04, `.c`/`.asm` 있음) | 0 | 본문 직접 쓰기 — 읽기만 |
| ntoskrnl | `MmCreateProcessAddressSpace` · `MmInitializeProcessAddressSpace` · `PspSetupUserProcessAddressSpace` · `PspComputeQuantumAndPriority` | `.c` 있음 | 0 | 읽기만 |
| ntoskrnl | `PspApplyMitigationOptions` `0xa6e1ac` (3763B) · `PspInheritMitigationOptions` `0xa25784` · `PspHardenMitigationOptions` `0xa7c9d0` · `PspReadIFEOMitigationOptions` `0xa3590c` | | 0 | `Mitigation*` 칸 |
| ntoskrnl | `PspAssignProcessQuotaBlock` `0xa4d458` · `PspInheritQuota` `0xa733a4` | | 0 | `QuotaBlock`·쿼터 칸 |
| ntoskrnl | `PspInitializeFullProcessImageName` `0xa49d98` | | 0 | `ImageFileName`·`SeAuditProcessCreationInfo` |
| ntoskrnl | `PspAttachSession` `0x787c70` · `PspSelectMachineForProcess` `0xa2bb9c` · `PoEnergyContextInitialize` `0xa72904` · `PspUpdateCreateInfo` `0xa48da4` | | 0 | `Session`·`Machine`·`EnergyContext` |
| ntoskrnl | (필요하면) `PspInsertProcess` (`.c` 있는지 확인), 쓰는 줄이 있는 다른 callee | `_index.tsv` / `pdb_aliases.py --where` | 0 | 1·2번 답에 따라 |

- 위 목록이 다 필요하지 않으면 줄인다. 큰 함수 (`PspApplyMitigationOptions`) 는 `_EPROCESS` 칸 쓰기 줄만 찾는다.
- objdump 로만 본 근거는 sast/43 처럼 함수+오프셋으로 적고 "`.asm` 없음" 표시.

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*`)

- `PspAllocateProcess` 직접 호출 (메모리·속성 관련 일부): `+0x534 ObCreateObjectEx`, `+0x757 PoEnergyContextInitialize`, `+0x830 PsReferencePartitionSafe`, `+0xa06 PspUpdateCreateInfo`, `+0xa5c RtlpOpenImageFileOptionsKeyEx`, `+0xa87 PspSelectMachineForProcess`, `+0xb11 ~ +0xe8f RtlQueryImageFileKeyOption` 여럿·`PspReadIFEONodeOptions +0xccd`·`PspReadIFEOPerfOptions +0xe54`, `+0xff6 PspWow64PickBestNtdll`, `+0x1215 cfg:@0xf04d28`, `+0x151f PspAttachSession`, `+0x158f PspAssignProcessQuotaBlock`, `+0x15d7/+0x1681 PspInheritQuota`, `+0x1637 MmCreateProcessAddressSpace`, `+0x168d MmInitializeHandBuiltProcess`, `+0x17bf cfg:@0xf04d20`, `+0x19ac MmInitializeHandBuiltProcess2`, `+0x19db PspApplyIFEOPerfOptions`, `+0x1a5f PspComputeQuantumAndPriority`, `+0x1a8b ~ +0x1c82` 완화 옵션 읽기·물려받기·굳히기, `+0x1e68 PspReadIFEOAntiTamperOptions`, `+0x1eb3 PspApplyMitigationOptions`, `+0x1fb3 AppModelPolicy_GetPolicy_Internal`, `+0x1ff3 SmProcessCreateNotification`, `+0x2065 PspInitializeFullProcessImageName`, `+0x2090/+0x210e MmInitializeProcessAddressSpace`, `+0x2239 PspSetupReservedUserMappings`, `+0x230f MmCreatePeb`, `+0x2397 PspSetupUserProcessAddressSpace`, `+0x27ff KiQueryUnbiasedInterruptTime`.

## 이미 아는 것

- `[정적]` baggage 3.11 아는 칸 22 (run10·run12·run43 등, `UniqueProcessId` `+0x27e4` 등) — 다시 보지 않는다.
- `[정적]` run49: 몸통 0 채움 (`ObpAllocateObject +0x191` → `ExAllocatePoolWithTag +0x48`), `Token` `PspInitializeProcessSecurity +0x110`, `ObjectTable` `ObInitProcess +0x53`.
- `[관찰]` run44: `K1 이전` 52칸 값은 K1 덤프에 있음 (값 대조에 써도 된다 — `ms/logs/04_baggage-read_K1.txt` 등).

## 받고 싶은 것 (결과서 `_workspace/sast/44_eprocess-create_result.md`)

- 맨 앞 요약 표: "212칸 → 아는 칸 (nt) / 0 을 씀 / 조건부 안 탐 / 아무도 안 씀 (만들기 길 안) / 범위 밖 길에서 씀 / 여전히 모름" 숫자, `K1 이전`·끝까지 0 따로.
- 질문 1~4 답 + 근거 (`.asm` 의 +0x 줄). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 칸별 "쓰는 곳" 표 (baggage.md 3.11 양식, 바로 옮길 수 있게). 160칸 쪽은 묶어서 (예: "같은 DWORD `Flags` 를 `+0x5xx` 에서 통째로 씀") 줄 수를 줄여도 된다.
- 동적 질문 목록 (번호, 추천 멈춤점, 찍을 것, 사진 덤프로 답할 수 있는지 — 덤프면 Q79 로).
- Q78c2b 로 넘길 것.
- 코드는 길게 붙이지 않고 우리 말로.

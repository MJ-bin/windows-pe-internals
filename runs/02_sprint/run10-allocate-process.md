# 2026-10-19 run10 — `PspAllocateProcess` 안: 주소 공간 → exe·ntdll 올리기 → PEB (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntoskrnl (`ntoskrnl.exe`, 26100.9457), pdb `ntkrnlmp.pdb` GUID `C29EBFB06B78B3C020DCA66D99713F9E1`
  - run3(관찰)·run9(정적)와 같은 커널이다.
- 뽑은 함수 8개 (모두 depth 0):
  - 요청서 대상: `PspAllocateProcess`, `MmCreatePeb`, `MmInitializeProcessAddressSpace`, `PspMapSystemDll`
  - 질문 3이 짚은 것: `MmCreateProcessAddressSpace`
  - exe·ntdll 올리는 곳을 찾으려고 더 뽑음: `MiMapProcessExecutable`, `PsMapSystemDlls`, `PspSetupUserProcessAddressSpace`

## 목적

- `PspAllocateProcess` 가 직접 부르는 함수를 보고, 하는 일 단위로 묶는다 (대기열 Q2).
- 그 안에서 아래 4가지가 일어나는지, 어떤 순서인지 본다.
  - (가) 새 프로세스의 주소 공간(메모리 공간) 만들기
  - (나) exe 를 새 프로세스 메모리에 올리기
  - (다) ntdll 을 새 프로세스 메모리에 올리기
  - (라) PEB 만들기
- run3 관찰 `PspAllocateProcess+0x2314` → `MmCreatePeb` 가 코드에서도 같은지 맞춰 본다.
- points.md `[확인 필요]` 이름 `MmInitializeProcessAddressSpace`, `PspMapSystemDll` 을 누가 부르는지 찾는다.

## 방법

- 정적: `_workspace/sast/04_allocate-process.md` — ntoskrnl, `^(PspAllocateProcess|MmCreatePeb|MmInitializeProcessAddressSpace|PspMapSystemDll)$` depth 0 (+ 위 4개 더)
- 결과서: `_workspace/sast/04_allocate-process_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨):
  - `ms/disassemble/ntoskrnl/_calls.tsv`, `_PspAllocateProcess_sorted.txt` (숫자 순서로 다시 정렬한 목록)
  - `ms/disassemble/ntoskrnl/` 의 `PspAllocateProcess.asm`, `MmCreatePeb.asm`, `MmCreateProcessAddressSpace.asm`, `MmInitializeProcessAddressSpace.asm`, `MiMapProcessExecutable.asm`, `PsMapSystemDlls.asm`, `PspMapSystemDll.asm`, `PspSetupUserProcessAddressSpace.asm`
  - `ms/decomp/ntoskrnl/` 의 같은 이름 `.c` (머리말 `callers:` 줄)
- 오프셋 읽는 법 (run9 와 같음)
  - `call` = call 명령이 있는 자리
  - `ret` = call 다음 명령 자리 = **WinDbg 콜스택에 찍히는 오프셋**. flow.md 그림 라벨은 이걸 쓴다.

## 결과

### 정적 — 큰 줄기 (exe·ntdll 이 올라가는 사슬)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!PspAllocateProcess` | `+0x1637` | `+0x163c` | `nt!MmCreateProcessAddressSpace` | [정적] |
| `nt!PspAllocateProcess` | `+0x2090` | `+0x2095` | `nt!MmInitializeProcessAddressSpace` | [정적] (보통 길) |
| `nt!PspAllocateProcess` | `+0x210e` | `+0x2113` | `nt!MmInitializeProcessAddressSpace` | [정적] (복제 길) |
| `nt!MmInitializeProcessAddressSpace` | `+0x1c9` | `+0x1ce` | `nt!MiMapProcessExecutable` | [정적] |
| `nt!MiMapProcessExecutable` | `+0xb3` | `+0xb8` | `nt!MmMapViewOfSectionEx` (exe) | [정적] |
| `nt!MiMapProcessExecutable` | `+0xd7` | `+0xdc` | `nt!PsMapSystemDlls` | [정적] |
| `nt!PsMapSystemDlls` | `+0xbb` | `+0xc0` | `nt!PspMapSystemDll` | [정적] |
| `nt!PspMapSystemDll` | `+0x118` | `+0x11d` | `nt!MmMapViewOfSectionEx` (ntdll) | [정적] |
| `nt!PspAllocateProcess` | `+0x230f` | `+0x2314` | `nt!MmCreatePeb` | [26100.9457 확인] + [정적] |
| `nt!PspAllocateProcess` | `+0x2397` | `+0x239c` | `nt!PspSetupUserProcessAddressSpace` | [정적] |

### 정적 — `PspAllocateProcess` 직접 호출, 하는 일 단위로

- 직접 호출 **144줄** (서로 다른 이름 107개). 함수 포인터를 거치는 **간접 호출 2곳**(`+0x121a`, `+0x17c4`).
- 이 함수를 부르는 곳 (머리말 `callers:`): `PspCreateProcess`, `NtCreateUserProcess`, `PsCreateMinimalProcess`.
- 주소 순서 = 실행 순서로 보인다 (결과서가 디컴파일 흐름과 맞춰 봄). 예외는 실패 정리 길과 복제 갈래.
- 숫자는 ret (괄호 안 call).

| # | 하는 일 | 대표 호출 | 개수 |
|---|---|---|---|
| 1 | 새 프로세스 객체 만들기 | `ObCreateObjectEx` `+0x539` (`+0x534`) | 9 |
| 2 | 파티션·섹션 참조 잡기 | (대표 없음) | 7 |
| 3 | exe 이미지 정보 읽기, IFEO 레지스트리, CPU 종류 | `MmGetSectionInformation` `+0x991`, `RtlpOpenImageFileOptionsKeyEx` `+0xa61`, `PspSelectMachineForProcess` `+0xa8c` | 23 |
| 4 | 토큰·권한 확인 | `SeQueryInformationToken` `+0x1065` | 5 |
| 5 | NUMA 노드·프로세서 그룹 정하기 | `PspSelectNodeForProcess` `+0x12b8` | 9 |
| 6 | 세션 검사, 다른 세션 붙기 | `PspAttachSession` `+0x1524` | 7 |
| 7 | 쿼터 연결 | `PspAssignProcessQuotaBlock` `+0x1594` | 3 |
| 8 | **(가) 주소 공간 만들기** | **`MmCreateProcessAddressSpace` `+0x163c`** | 4 |
| 9 | 커널 프로세스 구조, 보안, 핸들 표 | `KeInitializeProcess` `+0x171e`, `PspInitializeProcessSecurity` `+0x1927`, `ObInitProcess` `+0x1991` | 12 |
| 10 | 완화(mitigation) 정책 | `PspApplyMitigationOptions` `+0x1eb8` | 12 |
| 11 | 생성 알림, 전체 이미지 이름 | `SmProcessCreateNotification` `+0x1ff8`, `PspInitializeFullProcessImageName` `+0x206a` | 6 |
| 12 | **(나)(다) exe·ntdll 올리기** | **`MmInitializeProcessAddressSpace` `+0x2095`** | 7 |
| 13 | 유저 쪽 준비 + **(라) PEB** | `PspSetupReservedUserMappings` `+0x223e`, **`MmCreatePeb` `+0x2314`**, `PspSetupUserProcessAddressSpace` `+0x239c` | 5 |
| 14 | 마무리: ID 핸들, 생성 시각 | `ExCreateHandleEx` `+0x2703`, `KeQuerySystemTimePrecise` `+0x27c9` | 15 |
| — | 실패 정리·출구 | — | 7 |
| — | 복제 갈래 (부모 PEB 복사 등) | — | 13 |

### 정적 — 사슬 안쪽 함수들

`MmCreateProcessAddressSpace` (가):

- `MiAllocateTopLevelPage` `+0x1fd` → `MiCreateNewProcessTopLevelMappings` `+0x208` → `PsJoinSession` `+0x21f`. 커밋 요금(`MiChargeCommit` `+0xb1`)도 여기서.
- caller 는 `PspAllocateProcess` 하나뿐 (머리말).

`MmInitializeProcessAddressSpace` (ret 순서):

- `KiStackAttachProcess` `+0xb5` → `MiInitializeWorkingSetList` `+0xed` → `MiComputeProcessUserVa` `+0x13b` → `MiAllocateProcessVads` `+0x161` → `MiInsertProcessVads` `+0x1a8` → **`MiMapProcessExecutable` `+0x1ce`** → `KiUnstackDetachProcess` `+0x1e5`
- caller: `MiInitializeBootProcess`, `PspAllocateProcess` (머리말).

`MiMapProcessExecutable`:

- `+0xb8` `MmMapViewOfSectionEx` → `+0xc5` 결과를 새 프로세스 `+0x2b0` 에 저장 → `+0xdc` `PsMapSystemDlls`
- caller 는 `MmInitializeProcessAddressSpace` 하나뿐.

`PsMapSystemDlls` → `PspMapSystemDll`:

- `PsMapSystemDlls` 는 `PspSystemDlls` 표를 최대 6칸 돈다. 0번 칸은 늘 `PspMapSystemDll` 을 부르고, 나머지 칸엔 WOW64 조건이 붙는다.
- `PspMapSystemDll` 은 `PspReferenceSystemDll` `+0x40` 으로 섹션을 잡고 `+0x11d` `MmMapViewOfSectionEx` 로 올린다.
- `PspMapSystemDll` 의 caller 는 4곳: `PsMapSystemDlls`, `PspInitPhase3`, `PsLocateSystemDlls`, `PspLocateSystemDll`. 새 프로세스 길에서는 `PsMapSystemDlls`.

`MmCreatePeb` (직접 호출 19줄, 주요한 것만):

- `KiStackAttachProcess` `+0xc7` (새 프로세스 안으로 들어감)
- `MmMapViewOfSection` `+0x132` — leap second 데이터 섹션을 새 프로세스에 올림
- `MiCreatePebOrTeb` `+0x14b` — PEB 자리 만들기로 보임 [추측]
- `RtlImageNtHeader` `+0x2ce`, `RtlImageDirectoryEntryToData` `+0x311` — exe 헤더 읽기
- `MiInitializeWowPeb` `+0x680` — 32비트 조건일 때만
- `KiUnstackDetachProcess` `+0x5b5` 등

`PspSetupUserProcessAddressSpace` (ret 순서, 주요한 것만):

- `KiStackAttachProcess` `+0xfa` → `ZwAllocateVirtualMemory` `+0x19c` → `PspCopyAndFixupParameters` `+0x1b9` → `PspPrepareSystemDllInitBlock` `+0x1e6` → `MmMapApiSetView` `+0x255` → `PspMapSiloSharedDataView` `+0x263` → `KiUnstackDetachProcess` `+0x26f`

### 호출 관계

```
PspAllocateProcess                                   (번호 = 코드 순서)
 ├─ (1) +0x163c  MmCreateProcessAddressSpace          [정적] (가) 주소 공간 껍데기
 ├─ (2) +0x2095  MmInitializeProcessAddressSpace      [정적] 보통 길 (+0x2113 은 복제 길)
 │                └─ +0x1ce  MiMapProcessExecutable   [정적]
 │                     ├─ (1) +0xb8  MmMapViewOfSectionEx   [정적] (나) exe 올리기
 │                     └─ (2) +0xdc  PsMapSystemDlls        [정적]
 │                          └─ +0xc0  PspMapSystemDll       [정적]
 │                               └─ +0x11d  MmMapViewOfSectionEx  [정적] (다) ntdll 올리기
 ├─ (3) +0x2314  MmCreatePeb                          [관찰] run3 + [정적] (라) PEB
 └─ (4) +0x239c  PspSetupUserProcessAddressSpace      [정적]
```

### 정적 — `MmCreatePeb` 가 PEB 에 쓰는 것

- 오프셋은 디스어셈블에서 본 그대로 [정적]. **이름은 일반적으로 알려진 PEB 배치에 맞춰 본 것이라 [추측].**

| PEB 오프셋 [정적] | 무엇을 쓰나 [정적] | 이름 [추측] |
|---|---|---|
| `+0x0`, `+0x3`, `+0x8` | `PspAllocateProcess` 가 넘긴 16바이트 견본 복사 (`+0x8` 은 -1) | InheritedAddressSpace / BitField / Mutant |
| `+0x10` | 새 프로세스 `+0x2b0` 값 = `MiMapProcessExecutable` 이 exe 를 올린 뒤 저장한 주소 | ImageBaseAddress |
| `+0xb8` ~ `+0xe0` | 프로세서 개수, 전역 플래그, 커널 전역값 몇 개 | NumberOfProcessors, NtGlobalFlag 등 |
| `+0x118` ~ `+0x124` | 버전 숫자 (10, 0, 빌드 번호, CSD, 2). exe 헤더 값이 있으면 덮어씀 | OS 버전 칸들 |
| `+0x128` ~ `+0x130` | exe NT 헤더에서 읽은 서브시스템 값 | ImageSubsystem 등 |
| `+0x2c0` | 세션 번호 | SessionId |
| `+0x7b8` | leap second 뷰 주소 | LeapSecondData |

- **`PEB+0x18`, `+0x20`, `+0x30` 은 쓰는 줄이 없다.** [정적]
- PEB 주소는 새 프로세스 `+0x2e0` 에 돌려준다 (호출부 `+0x22f2` 에서 그 자리를 인자로 줌). [정적]

## 확인된 것

- **run3 관찰과 코드가 맞는다.** [관찰] run3 + [정적] run10
  - `PspAllocateProcess` call `+0x230f` / ret `+0x2314` → `MmCreatePeb`. 오프셋까지 같다.
  - `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess` 도 다시 맞음 (run9 와 같은 값).
- **(가)(나)(다)(라) 모두 `PspAllocateProcess` 안에서 일어나고, 순서도 (가) → (나) → (다) → (라).** [정적]
  - 오프셋: `+0x163c` < `+0x2095` < `+0x2314`
  - (나)·(다)는 따로 있는 호출이 아니다. **`MmInitializeProcessAddressSpace` 한 번 안에서 exe 먼저, ntdll 다음** (`MiMapProcessExecutable` `+0xb8` → `+0xdc`).
- **exe 를 올릴 때 쓰는 섹션 = run9 의 이미지 섹션.** [정적]
  - `NtCreateUserProcess` 가 `MmCreateSpecialImageSection`(ret `+0x93c`) 결과를 7번째 인자로 `PspAllocateProcess` 에 넘긴다 (`+0xc0c`).
  - `PspAllocateProcess` 가 그걸 `MmInitializeProcessAddressSpace` 의 세 번째 인자로 넘긴다 (`+0x2080` ~ `+0x2090`).
- **커널 쪽에서 exe·ntdll 을 `MmMapViewOfSectionEx` 로 올리는 코드가 있다.** [정적]
  - exe: `MiMapProcessExecutable +0xb8`
  - ntdll: `PspMapSystemDll +0x11d` (`PsMapSystemDlls +0xc0` 에서 부름)
- **points.md 이름 두 개의 caller** [정적]
  - `MmInitializeProcessAddressSpace` ← `PspAllocateProcess` 가 **직접** 부른다 (`+0x2095`, `+0x2113` 두 곳).
  - `PspMapSystemDll` ← `PspAllocateProcess` 가 직접 부르지 **않는다.** 새 프로세스 길의 caller 는 `PsMapSystemDlls`.
- **`MmInitializeProcessAddressSpace` 를 부르는 곳이 두 곳이고, 같은 플래그(bit `0x4`, 복제)로 갈린다.** [정적]
  - `+0x2095`: 보통 길 (부모 없음, 섹션 있음)
  - `+0x2113`: 복제 길 (부모 있음, 섹션 없음)
  - `MmCreatePeb` 도 그 플래그가 꺼졌을 때만 부른다 (`+0x22e9 TEST BL,0x4`).
  - 그래서 run3 에서 `+0x2314` 가 찍혔다면 그 실행은 `+0x2095` 쪽이었을 것이다. [추측] (같은 변수라는 가정, 동적으로 확인할 것)
- **`MmCreatePeb` 는 PEB `+0x20` 을 쓰지 않는다.** [정적]
  - ntdll `LdrpInitializeProcess` 는 PEB `+0x20` 을 읽는다 (sast/02 때 뽑은 `.c`).
  - 그럼 누가 쓰나? `PspSetupUserProcessAddressSpace` 안 `PspCopyAndFixupParameters`(`+0x1b9`)가 후보다. 본문은 안 봤다. [추측]

## 아직 확인 필요

- 동적으로 볼 것 (커널 멈춤점은 부모 cmd.exe 로 `/p` 필터, run3 방식). 아래 오프셋은 모두 ret 라서 `k` 콜스택과 바로 비교된다.
  1. `MmInitializeProcessAddressSpace` 를 `+0x2095` / `+0x2113` 중 어디서 탔는지
  2. PEB 값: `PspAllocateProcess+0x2314` 에서 `poi(새 EPROCESS+0x2e0)` 의 `+0x10`, `+0xb8`, `+0xbc`, `+0x118~`, 그리고 `+0x20` (아직 0 이어야 함). `+0x239c` 뒤에 `+0x20` 이 채워졌는지
  3. 간접 호출 2곳 대상: `+0x1215`(ret `+0x121a`) / `+0x17bf`(ret `+0x17c4`) 에서 `[nt+0xf04d28]`, `[nt+0xf04d20]` 값을 `ln`
  4. `MiMapProcessExecutable+0xdc` 에서 새 프로세스의 ntdll 위치가 나중 `LdrInitializeThunk` 주소와 맞는지
  5. 갈림길을 탔는지: `PspWow64PickBestNtdll` `+0xffb` (64비트면 안 타야 함), `KeSecureProcess` `+0x27a0`, `PspSetProcessAffinitySafe` `+0x2364`
  - 추천 멈춤점: `PspAllocateProcess+0x163c`, `+0x2095`, `MiMapProcessExecutable+0xb8`, `+0xdc`, `PspAllocateProcess+0x2314`, `+0x239c`
  - `PspMapSystemDll` 입구는 부팅 때 System 프로세스에도 걸린다 (caller 에 `PspInitPhase3`). 새 프로세스만 보려면 `PsMapSystemDlls` 입구나 `MiMapProcessExecutable+0xdc` 가 낫다.
- 정적으로 더 볼 것
  - `PspCopyAndFixupParameters`: PEB `+0x20` 을 채우는 곳인지
  - `PspPrepareSystemDllInitBlock`: ntdll 로 넘기는 시작 정보 묶음인지 [추측]
  - `MmCreateSpecialImageSection` callee (대기열 Q33): `MiCreateImageFileMap` 과 이어지는지
- "커널이 올려 주는 건 exe 와 ntdll **뿐**" 은 아직 [추측]
  - exe·ntdll 말고도 이 단계에 새 프로세스에 붙는 뷰가 코드에 보인다: leap second 섹션(`MmCreatePeb +0x132`), ApiSet 뷰(`PspSetupUserProcessAddressSpace +0x255`), silo 공유 데이터 뷰(`+0x263`). 이것들이 "DLL" 은 아니지만, 다른 매핑이 없다는 건 확인하지 않았다.

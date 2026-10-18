# 04 allocate-process — 정적 분석 결과

- 요청서: `_workspace/sast/04_allocate-process.md` (Q2)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), pdb `ntkrnlmp.pdb` guid_age=`C29EBFB06B78B3C020DCA66D99713F9E1` (기준 GUID와 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`). analyze 는 다시 안 했다 (`.gpr`·`_index.tsv`·`SetPdb:` 줄 모두 있음). export 3번만.
- 뽑은 함수 (전부 depth 0, 종료코드 0, SCRIPT ERROR 없음) → `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`
  1. 요청서 대상 + 질문 3이 짚어 달라던 것: `^(PspAllocateProcess|MmCreatePeb|MmInitializeProcessAddressSpace|PspMapSystemDll|MmCreateProcessAddressSpace)$` → 5개
  2. 질문 2 (나)(다)를 답하려고 **요청서 밖으로 조금 넓힘** (모두 작은 함수, depth 0): `^(PsMapSystemDlls|PspSetupUserProcessAddressSpace)$` → 2개, `^MiMapProcessExecutable$` → 1개. 이유: `PspAllocateProcess` 의 직접 호출에는 exe·ntdll 을 올린다는 이름이 없어서, `MmInitializeProcessAddressSpace` 와 `PspMapSystemDll` 의 `callers:` 를 거슬러 올라가야 답이 났다.
- `_calls.tsv`: export 전 줄이 하나도 안 사라졌다 (전·후 `diff` 로 확인). 지금 PspAllocateProcess 144줄, MmCreatePeb 19줄 등 쌓여 있다.
- 버전 확인 (알려진 사실 대조): 일치
  - `NtCreateUserProcess +0xc31` (ret `+0xc36`) → `PspAllocateProcess` ✓ (표의 `+0xc36`)
  - `PspAllocateProcess +0x230f` (ret `+0x2314`) → `MmCreatePeb` ✓ (01_sprint/run3 `[관찰]` `+0x2314`)
  - 두 값 다 `ret_off` 와 같다. 스크립트 오프셋 계산과 바이너리를 같이 믿어도 된다.

## 스크립트 문제 (경미, 결과는 믿어도 됨)

고치지 않았다. 제안만.

1. `_calls.tsv` 가 글자 순서로 정렬된다 (`sort -u`). `+0x1060` 이 `+0x126` 보다 앞에 나온다. 요청서의 "call 순서"를 보려면 숫자로 다시 정렬해야 했다. 제안: 정렬 키를 call_off 숫자값으로 (또는 `.asm` 순서 그대로). `frag:` 줄은 맨 뒤.
2. `ret_off=tail` 줄이 **진짜 꼬리호출이 아닌** 경우가 있다. `MmCreatePeb +0x6b4 tail FUN_140b1c2b0`, `MiMapProcessExecutable +0x137 tail FUN_140b18902`. 둘 다 본체에서 떨어진 **조각으로 가는 `JMP`** 이고, 조각이 끝나면 본체로 되돌아온다 (아래 5번에서 확인). reference.md 에 frag 설명은 있지만 `tail` 라벨만 보면 꼬리호출로 읽힌다. 제안: 대상이 `FUN_` 이고 `.asm` 에 `; --- 조각` 블록이 붙으면 라벨을 `tail` 대신 `jmp-frag` 같은 별도 이름으로.
3. 위 2번 때문에 `ms/decomp/ntoskrnl/MmCreatePeb.c` 의 한 곳 (`FUN_140b1c2b0(); return;`) 은 **실제와 다르다.** 실제로는 조각이 끝나고 `+0x433` 으로 돌아와 PEB 채우기를 계속한다. 이 결과서는 `.asm` 로 확인한 흐름만 쓴다. 제안: 조각이 본체로 돌아오는 함수는 `.c` 머리말에 `// frag-rejoin: FUN_xxx -> +0x...` 한 줄.
4. `PspAllocateProcess.c` 에 `*(undefined8 *)(puVar31 + -8) = 0x140a02e0b;` 같은 줄이 많다. Ghidra 가 call 을 "복귀 주소를 스택에 쓰기"로 그린 것이다. 숫자는 `함수 시작(0x140a02568) + ret_off` 와 같다 (예: `0x140a03ba4` = `+0x163c`). 읽는 데 도움은 되지만 그대로 믿으면 오해한다는 점만 적어 둔다.

## 질문별 답 [정적]

### 1. `PspAllocateProcess` 가 직접 부르는 함수, `+0x2314` → `MmCreatePeb` 일치?

- 답: **예, 같다.** call 명령 `+0x230f`, 돌아올 자리 `ret_off=+0x2314`, 대상 `MmCreatePeb`.
- 직접 호출 144줄, 서로 다른 이름 107개. 표기별 개수: `indirect:cfg:` 2, `alias:` 8, `tail` 0, `frag:` 0, `TRAP`(INT 0x29) 1.
- 전체 목록(call 순서)은 `ms/disassemble/ntoskrnl/_PspAllocateProcess_sorted.txt` (번호, call_off, ret_off, 대상 144줄. `_calls.tsv` 의 `PspAllocateProcess` 줄을 숫자 순서로 다시 정렬한 것, sast/03 의 `_NtCreateUserProcess_sorted.txt` 와 같은 모양). 묶어서 읽는 표는 질문 2.
- 이 함수를 부르는 곳: `PspCreateProcess`, `NtCreateUserProcess`, `PsCreateMinimalProcess` (`PspAllocateProcess.c` 머리말 `callers:`).
- 근거: `PspAllocateProcess.asm` `+0x230f CALL ... ; CALL ret=+0x2314 -> MmCreatePeb`.

### 2. 호출을 하는 일 단위로 묶기

주소 순서 = 실행 순서다 (디컴파일 흐름과 맞춰 봄). 예외 2곳: 맨 아래 "실패 정리·출구"와 "갈래" 줄. 표의 숫자는 `ret_off` (괄호는 call_off).

| # | 하는 일 (개수) | 대표 호출 (ret_off, call_off) | 디컴파일로 본 역할 | 나머지 잡일 |
|---|---|---|---|---|
| 1 | 새 프로세스 객체 만들기 (9) | `ObCreateObjectEx` `+0x539` (`+0x534`) | 프로세스 객체를 만든다. 크기는 앞에서 계산 (`KeSizeOfKProcessExtension` `+0x4ef` 등). 돌려받은 메모리를 `memset` 하고 리스트 머리를 초기화. | 8 (memset 4, PoEnergyEstimationEnabled, KeQueryMaximumGroupCount, KeSizeOfKProcessExtension, ObRetagReferences) |
| 2 | 파티션·섹션 참조 잡기 (7) | (없음) | 부모의 파티션·섹션 객체에 참조를 건다. | 7 |
| 3 | exe 이미지 정보 읽기, IFEO, CPU 종류 정하기 (23) | `MmGetSectionInformation` `+0x991` (`+0x98c`); `RtlpOpenImageFileOptionsKeyEx` `+0xa61` (`+0xa5c`); `PspSelectMachineForProcess` `+0xa8c` (`+0xa87`) | 이미지 섹션(NtCreateUserProcess 가 만든 것)에서 정보를 읽는다. exe 이름의 IFEO 레지스트리 키를 열어 `Debugger`, `UseLargePages`, `AllowedCpuSets`, `ImageExpansionMitigation` 등을 읽는다 (`Debugger` 값이 있으면 `PspUpdateCreateInfo` 에 코드 5 를 주고 실패 정리 길로 간다 — 유저 쪽이 디버거로 다시 만들게 하려는 것으로 보임 `[추측]`). 이미지에 맞는 CPU 종류를 고른다. 32비트 조건이면 `PspWow64PickBestNtdll` `+0xffb` (`+0xff6`) 로 쓸 ntdll 종류도 고른다. | 20 |
| 4 | 토큰·권한 확인 (5) | `SeQueryInformationToken` `+0x1065` (`+0x1060`) | 호출자 토큰 정보를 읽고, 필요하면 권한(TCB 등)을 잠깐 얻는다. | 4 |
| 5 | CPU 노드·그룹·친화도 정하기 (9) | `PspSelectNodeForProcess` `+0x12b8` (`+0x12b3`) | 새 프로세스를 어느 NUMA 노드·프로세서 그룹에 둘지 정한다. | 8 |
| 6 | 세션·토큰 검사, 다른 세션 붙기 (7) | `PspAttachSession` `+0x1524` (`+0x151f`) | 토큰의 세션이 호출자와 다르면 그 세션에 붙는다. | 6 (alias 3개 포함) |
| 7 | 쿼터 연결 (3) | `PspAssignProcessQuotaBlock` `+0x1594` (`+0x158f`) | 쿼터 블록을 붙이거나 부모 것을 물려받는다 (`PspInheritQuota`). | 2 |
| 8 | **(가) 주소 공간 만들기 (4)** | **`MmCreateProcessAddressSpace` `+0x163c` (`+0x1637`)** | 새 프로세스의 맨 위 페이지 표와 커널 쪽 매핑을 만든다 (그 함수 안 호출: `MiAllocateTopLevelPage` `+0x1fd`, `MiCreateNewProcessTopLevelMappings` `+0x208`, `PsJoinSession` `+0x21f`, 커밋 요금 `MiChargeCommit`). 이 함수의 caller 는 `PspAllocateProcess` 하나뿐 (머리말). | 3 (`PspDetachSession` `+0x166e`; 부모가 없을 때 갈래 `PspInheritQuota` `+0x1686`, `MmInitializeHandBuiltProcess` `+0x1692`) |
| 9 | 커널 프로세스 구조 초기화, 보안, 핸들 표, 우선순위 (12) | `KeInitializeProcess` `+0x171e` (`+0x1719`); `PspInitializeProcessSecurity` `+0x1927` (`+0x1922`); `ObInitProcess` `+0x1991` (`+0x198c`) | 스케줄링 쪽 프로세스 구조를 초기화. 토큰을 새 프로세스에 붙임. 핸들 표를 준비하고 부모 핸들을 물려줄지 정함 (이름과 인자(부모, 새 프로세스)로 읽은 것, 본문은 안 봄 `[추측]`). | 9 (간접 호출 `+0x17c4` 포함) |
| 10 | 완화(mitigation) 정책 정하기 (12) | `PspApplyMitigationOptions` `+0x1eb8` (`+0x1eb3`) | IFEO·부모·시스템 기본값에서 읽은 완화 옵션을 합쳐 새 프로세스에 적용한다. | 11 |
| 11 | 앱 정책·알림·이미지 이름 (6) | `PspInitializeFullProcessImageName` `+0x206a` (`+0x2065`); `SmProcessCreateNotification` `+0x1ff8` (`+0x1ff3`) | 전체 이미지 경로 이름을 만든다. 프로세스 생성 알림 (`Sm`). | 4 |
| 12 | **(나) exe 올리기 + (다) ntdll 올리기 (7)** | **`MmInitializeProcessAddressSpace` `+0x2095` (`+0x2090`)** | 질문 2 아래 "(나)(다)" 참고. 인자: 새 프로세스, 부모=0, 섹션 객체, 플래그. | 6 (복제 갈래 `+0x2113`, MiSectionControlArea, ExAllocatePool2, memcpy 계열, ObfDereferenceObject, RtlReleasePrivilege) |
| 13 | 유저 쪽 준비 + **(라) PEB 만들기 (5)** | **`MmCreatePeb` `+0x2314` (`+0x230f`)**; `PspSetupUserProcessAddressSpace` `+0x239c` (`+0x2397`); `PspSetupReservedUserMappings` `+0x223e` (`+0x2239`) | PEB 를 만든다 (질문 4). 그 뒤 `PspSetupUserProcessAddressSpace` 가 새 프로세스에 붙어서 매개변수 공간 할당·복사, ApiSet 뷰, silo 공유 데이터 뷰, `PspPrepareSystemDllInitBlock` 을 한다. | 2 (`PsQueryProcessAttributes`, `PspSetProcessAffinitySafe` `+0x2364`) |
| 14 | 마무리: ID 핸들, 토큰 보호, 생성 시각 (15) | `ExCreateHandleEx` `+0x2703` (`+0x26fe`); `KeQuerySystemTimePrecise` `+0x27c9` (`+0x27c4`) | `PspCidTable` 에 새 프로세스의 핸들을 만들어 새 프로세스 `+0x1d0` 에 저장 `[정적]` (이게 프로세스 ID 인지는 `[추측]`). 현재 시각을 새 프로세스 `+0x1f8` 에 저장 (생성 시각 `[추측]`). | 13 (`KeSecureProcess` `+0x27a0` 는 플래그 0x10000 일 때만) |
| — | (예외) 실패 정리·출구 (7) | — | 중간에 실패하면 `LAB_...` 로 점프해서 `PspRundownSingleProcess` `+0x11bf` 등으로 객체를 치운다. 끝에서 간접 호출 `+0x121a` 와 `__security_check_cookie` `+0x284d`. | 7 |
| — | (예외) 갈래: 부모 PEB 를 복사하는 길·다른 후처리 (13) | — | 플래그 `4`(복제)일 때 `MmCreatePeb` 대신 부모 PEB 포인터를 복사하고 `+0x23d7 ~ +0x260d` 의 attach/ApiSet/silo/ntdll-init-block 호출을 직접 한다. test.exe 처럼 섹션이 있는 보통 길에서는 13번 줄의 `PspSetupUserProcessAddressSpace` 가 이 일을 대신한다 (코드 조건으로 읽은 것, 실제로 탄 길은 동적). | 13 |

합계 9+7+23+5+9+7+3+4+12+12+6+7+5+15+7+13 = 144 (표의 14번 줄과 예외 두 줄 포함). 줄 순서는 위 표 기준.

**4가지 일 (가)(나)(다)(라) 일어나나?**

| 일 | 이 함수 안에서 일어나나? | 어느 호출 (ret_off) |
|---|---|---|
| (가) 주소 공간 만들기 | **예** | `MmCreatePeb` 앞, `MmCreateProcessAddressSpace` `+0x163c` |
| (나) exe 올리기 | **예, 한 단계 안쪽** | `MmInitializeProcessAddressSpace` `+0x2095` 안에서 `MiMapProcessExecutable` (ret `+0x1ce`) → `MmMapViewOfSectionEx` (ret `+0xb8`). 이 직접 호출 이름이 `PspAllocateProcess` 에는 없다. |
| (다) ntdll 올리기 | **예, 두 단계 안쪽** | 같은 `MmInitializeProcessAddressSpace` `+0x2095` → `MiMapProcessExecutable` `+0xdc` → `PsMapSystemDlls` → `PspMapSystemDll` (ret `+0xc0`) → `MmMapViewOfSectionEx` (ret `+0x11d`) |
| (라) PEB 만들기 | **예** | `MmCreatePeb` `+0x2314` |

- **순서는 (가) → (나) → (다) → (라) 가 맞다.** `+0x163c` < `+0x2095` < `+0x2314`. (나)와 (다)는 따로 있는 호출이 아니라 **`MmInitializeProcessAddressSpace` 한 번 안에서 exe 먼저, ntdll 다음** 이다 (`MiMapProcessExecutable.asm`: exe 매핑 `+0xb3`(ret `+0xb8`) → 결과를 새 프로세스 `+0x2b0` 에 저장 `+0xc5` → `PsMapSystemDlls` `+0xd7`(ret `+0xdc`)).
- (나)의 섹션 출처: `NtCreateUserProcess` 가 `MmCreateSpecialImageSection` (ret `+0x93c`) 의 결과를 `[RBP+0x260]` → `[RBP+0x270]` (`+0x978`) 으로 옮기고, `PspAllocateProcess` 호출 (`+0xc31`) 때 7번째 인자 (`[RSP+0x30] = [RBP+0x270]`, `+0xc0c`) 로 넘긴다. `PspAllocateProcess` 는 이 인자를 `[RSP+0x108]` (`+0x54`) 에 두었다가 `MmInitializeProcessAddressSpace` 의 세 번째 인자(R8)로 넘긴다 (`+0x2080`~`+0x2090`). 즉 **sast/03 의 이미지 섹션이 바로 (나)에서 쓰인다.**
- `MmInitializeProcessAddressSpace` 안 순서 (ret): `KiStackAttachProcess` `+0xb5` → `MiInitializeWorkingSetList` `+0xed` → `MiComputeProcessUserVa` `+0x13b` → `MiAllocateProcessVads` `+0x161` → `MiInsertProcessVads` `+0x1a8` → `MiMapProcessExecutable` `+0x1ce` → `KiUnstackDetachProcess` `+0x1e5`. 유저 쪽 주소 배치 장부(작업 집합, VAD)를 만든 뒤 exe 와 ntdll 을 올린다. 그래서 (가) = 껍데기(최상위 페이지 표), 그 위 장부와 올리기는 `MmInitializeProcessAddressSpace` 몫이다.
- ntdll 올리기 반복문: `PsMapSystemDlls` 는 `PspSystemDlls` 표를 최대 6칸 돈다. 0번 칸은 무조건 `PspMapSystemDll` 을 부르고, 나머지 칸은 WOW64 조건이 붙는다 (코드 조건으로 읽은 것). 64비트 test.exe 면 0번 칸 하나일 것 `[추측]`. 이 칸에 든 섹션은 `PspMapSystemDll` 이 `PspReferenceSystemDll` (ret `+0x40`) 로 참조해 `MmMapViewOfSectionEx` 에 넘긴다. 섹션 자체를 만드는 쪽은 `PsLocateSystemDlls`/`PspLocateSystemDll`/`PspInitPhase3` 라는 이름만 보인다 (본문은 안 봄, 부팅 때 준비하는 걸로 `[추측]`).
- 틀리기 쉬운 점: `MmInitializeProcessAddressSpace` 호출이 두 곳이다. `+0x2095` (보통 길: 부모=0, 섹션 있음) 와 `+0x2113` (복제 길: 부모 있음, 섹션=0). 이 둘은 같은 플래그 `4` 로 갈린다. `MmCreatePeb` 도 같은 플래그가 꺼져 있을 때만 부르므로 (`+0x22e9 TEST BL,0x4`), 01_sprint 에서 `+0x2314` 가 관찰되었다면 **그 실행은 `+0x2095` 쪽을 탔다** (같은 변수라는 가정 하의 정적 추론, 확인은 동적).

### 3. `MmInitializeProcessAddressSpace`, `PspMapSystemDll` 을 누가 부르나

| 함수 | `.c` 머리말 `callers:` | `PspAllocateProcess` 가 직접 부르나 |
|---|---|---|
| `MmInitializeProcessAddressSpace` (1237바이트) | `MiInitializeBootProcess`, `PspAllocateProcess` | **예.** `+0x2090` (ret `+0x2095`) 와 `+0x210e` (ret `+0x2113`) 두 곳. |
| `PspMapSystemDll` (480바이트) | `PsMapSystemDlls`, `PspInitPhase3`, `PsLocateSystemDlls`, `PspLocateSystemDll` | **아니오.** 새 프로세스 길의 실제 caller 는 `PsMapSystemDlls` (`+0xbb`, ret `+0xc0`). |

- 그 위 caller 사슬 (모두 머리말 `callers:` 와 `_calls.tsv` 로 확인): `PspAllocateProcess` `+0x2095` → `MmInitializeProcessAddressSpace` `+0x1ce` → `MiMapProcessExecutable` `+0xdc` → `PsMapSystemDlls` `+0xc0` → `PspMapSystemDll`.
  - `PsMapSystemDlls` 의 caller 는 `MiMapProcessExecutable` 하나뿐, `MiMapProcessExecutable` 의 caller 도 `MmInitializeProcessAddressSpace` 하나뿐이다 (머리말 기준. Ghidra 의 `callers:` 는 CALL 참조만 센다. 함수 포인터로 부르는 길은 안 보인다).
- `MmCreateProcessAddressSpace` (692바이트): **있다.** (가) 후보가 맞다. caller 는 `PspAllocateProcess` 뿐이고 `+0x1637` (ret `+0x163c`) 에서 부른다 (질문 2의 8번 줄).
- `points.md` 이름 `PspMapSystemDll` 을 대표 지점으로 쓰려면 "직접 caller 는 `PsMapSystemDlls`" 임을 같이 적어야 한다.

### 4. `MmCreatePeb` 직접 호출, PEB 에 채우는 것

직접 호출 19줄 (call 순서, ret_off):

- 준비: `memset` `+0x7d`, `KeQueryGroupMaskProcess` `+0x93`, `KeFindFirstSetRightGroupMask` `+0xac`, `KiStackAttachProcess` `+0xc7` (새 프로세스 안으로 들어감)
- 새 프로세스에 메모리 만들기: `MmMapViewOfSection` `+0x132` (`ExLeapSecondDataSectionPointer` 섹션을 새 프로세스에 매핑), `MiCreatePebOrTeb` `+0x14b` (PEB 가 놓일 메모리를 만드는 호출, 인자 0, 이름과 출력 자리로 읽은 것 `[추측]`)
- 값 모으기: `PsGetCurrentServerSiloGlobals` `+0x15a`, `PsGetProcessSessionId` `+0x292` (alias, 아래 5번), `RtlImageNtHeader` `+0x2ce`, `RtlImageDirectoryEntryToData` `+0x311` (인자 `10`)
- 이상 처리: `ExRaiseDatatypeMisalignment` `+0x373`, `KeQueryGroupAffinity` `+0x62e` 와 조각 안 한 곳 (아래 5번)
- WOW64: `MiInitializeWowPeb` `+0x680` (새 프로세스의 `+0x310` 이 0 이 아닐 때만)
- 마무리: `KiUnstackDetachProcess` `+0x5b5`, `+0x657`, `+0x699`, `__security_check_cookie` `+0x5d2`
- 이 안에 `indirect:` 는 없다. `tail` 1, `frag:` 1, `alias:` 1.

PEB 에 쓰는 것 (Ghidra 가 보여 준 오프셋 그대로 `[정적]`; 괄호 안 이름은 내 기억의 PEB 배치와 맞춰 본 것이라 `[추측]`):

| PEB 오프셋 | 무엇을 쓰나 (코드에서 읽은 것) | 이름 `[추측]` |
|---|---|---|
| `+0x0`, `+0x3`, `+0x8` | `PspAllocateProcess` 가 16바이트 견본을 만들어 넘긴다 (`+0x8` 은 `-1`, `+0x3` 은 프로세스 플래그에서 만든 비트). 그대로 복사. | InheritedAddressSpace / BitField / Mutant |
| `+0x10` | 새 프로세스 `+0x2b0` 값. `MiMapProcessExecutable` `+0xc5` 가 exe 를 올린 직후 저장한 그 주소. | ImageBaseAddress |
| `+0xb8`, `+0xbc`, `+0xc0`, `+0xc8`, `+0xd0`, `+0xd8`, `+0xe0` | `KeNumberProcessors`, `NtGlobalFlag`, 커널 전역 변수 몇 개 | NumberOfProcessors, NtGlobalFlag, 힙·타임아웃 기본값 |
| `+0x118`, `+0x11c`, `+0x120`, `+0x122`, `+0x124` | 버전 숫자: 10, 0, silo 의 빌드 번호, `CmNtCSDVersion`, 2. exe NT 헤더의 `+0x4c` 값이 0 이 아니면 이 값들을 그걸로 덮어씀. | OSMajor/Minor/Build/CSD/PlatformId |
| `+0x128`, `+0x12c`, `+0x130` | exe NT 헤더의 `+0x5c`, `+0x48`, `+0x4a` | ImageSubsystem, 서브시스템 버전 |
| `+0x2c0` | 세션 번호 (`PsGetProcessSessionId`) | SessionId |
| `+0x318`, `+0x34c`, `+0x34e`, `+0x350` | 전역/silo 데이터 | (코드 페이지 쪽 값으로 보임) |
| `+0x7b8`, `+0x7c0`, `+0x7c4` | 매핑한 leap second 뷰 주소, 0, `NtGlobalFlag2` | LeapSecondData / Flags / NtGlobalFlag2 |

- **`MmCreatePeb` 는 `PEB+0x18`, `+0x20`, `+0x30` 을 쓰지 않는다** (쓰는 줄이 없음).
- 출력 두 개: 새 프로세스 `+0x2e0` 에 PEB 주소 (호출부 `+0x230f` 에서 R8 = `새 프로세스 + 0x2e0`), 네 번째 인자에 exe 로드 구성 디렉터리 `+0x40` 값(친화도 마스크로 보임)을 쓴다. 그 값이 0 이 아니면 `PspAllocateProcess` 가 `PspSetProcessAffinitySafe` `+0x2364` 를 부른다.
- 반대쪽(ntdll)이 나중에 읽는 것 `[정적]` — `ms/decomp/ntdll/LdrpInitializeProcess.c` (sast/02 때 뽑음):
  - PEB `+0x10`: exe NT 헤더를 찾고(`RtlImageNtHeaderEx`), 로드 구성을 읽는 데 쓴다.
  - PEB `+0xb8`: 1 보다 크면 스핀 횟수를 `0x2800` 으로 정한다.
  - PEB `+0xbc`: 읽은 뒤 exe 로드 구성의 GlobalFlagsClear/Set 으로 고쳐 쓴다.
  - PEB `+0xc0`: `RtlpTimeout` 에 저장.
  - PEB `+0x3`: 비트 몇 개를 검사.
  - PEB `+0x20`: 읽는다 (`RtlNormalizeProcessParams` 에 넘김). 그런데 **`MmCreatePeb` 는 이걸 안 쓴다.**
  - ntdll 이 PEB 에 직접 쓰는 곳: `+0x18`, `+0x30`, `+0x38` 등.
- 그럼 `+0x20` (프로세스 매개변수) 은 커널이 누가 채우나: 직접 증거는 아직 없다. `PspSetupUserProcessAddressSpace` (`+0x239c`) 안에서 새 프로세스에 붙은 채로 `ZwAllocateVirtualMemory` (ret `+0x19c`) 후 `PspCopyAndFixupParameters` (ret `+0x1b9`) 를 부르는 건 보인다 `[정적]`. 이 함수가 PEB `+0x20` 에 연결하는지는 **본문을 안 봐서 `[확인 필요]`.** (다음 정적 요청 후보)

### 5. 간접 호출·꼬리호출·별칭·조각

| 함수 | 줄 | 처리 |
|---|---|---|
| `PspAllocateProcess` | `indirect:cfg:@0xf04d28` `+0x1215` (ret `+0x121a`) | CFG 검사 함수(`+0x1406b77f0`)를 거친 간접 호출. 함수 끝 출구에서 `[0x140f04d28]` 이 0 이 아니면 부름. 인자는 스택 버퍼(`+0x5a0`) 하나. 진짜 대상은 동적. |
| 〃 | `indirect:cfg:@0xf04d20` `+0x17bf` (ret `+0x17c4`) | 같은 방식. `[0x140f04d20]` 이 0 이 아니고 만들기 정보에서 얻은 포인터(`+0xb8`)가 있을 때, 인자 둘(그 포인터, 스택 버퍼 `+0x5a0`)로 부름. 위 줄과 한 쌍(채우기 → 정리)으로 보임 `[추측]`. 진짜 대상은 동적 (`nt+0xf04d20`, `nt+0xf04d28` 에 든 값). |
| 〃 | `alias:KeQueryHeteroCpuPolicyThread+1` `+0x7e4` | **이름이 틀렸을 가능성이 크다.** `.asm` 의 `ALIASES(1)` 는 `MmGetDefaultPagePriority`. 부모가 없을 때 기본값을 받아 3비트 칸(새 프로세스 `+0x1f0` 의 12~14비트)에 넣고, 부모가 있으면 부모 `+0x1f0` 의 같은 3비트를 복사하는 모양이라 `MmGetDefaultPagePriority` 가 맞아 보인다 `[추측]`. 대표 지점으로 안 쓴다. |
| 〃 | `alias:ObfReferenceObject+1` `+0x89e`, `+0x8f9` | 별칭은 `PsReferenceSiloContext`. 코드 모양이 섹션 객체에 참조를 거는 것이라 `ObfReferenceObject` 로 읽는다. 대표 지점 아님. |
| 〃 | `alias:KiCopyAffinityEx+1` `+0x134d`, `alias:PsGetProcessSessionIdEx+1` `+0x14cb`, `+0x153a`, `+0x1550`, `alias:RtlCopyMemory+3` `+0x2167` | 별칭이 이름만 다른 같은 뜻 (mangled 이름, `PsGetSessionIdEx`, `memcpy`/`RtlMoveMemory`/`memmove`). 문제 없음. 대표 지점 아님. |
| 〃 | `TRAP` (`INT 0x29`) `+0x868` | 시스템 파티션의 참조 카운트를 올린 결과가 2 보다 작으면(0 에서 올린 경우) `__fastfail` (인자 `0xe`). 함수 호출 아님. |
| `MmCreatePeb` | `tail FUN_140b1c2b0` `+0x6b4`, `frag:FUN_140b1c2b0+0x18` (ret `frag+0x1d`) → `KeQueryGroupAffinity` | **진짜 조각이다.** 조각은 본체와 같은 레지스터(RDI, RBX, R13)와 같은 스택 자리(`[RSP+0x58]`, `[RSP+0x68]`, `[RSP+0x130]` 등)를 쓰고, 끝에서 `JMP 0x1409313c7` (= `MmCreatePeb +0x433`) 로 본체에 돌아온다. 하는 일: exe 헤더에 "단일 프로세서 전용"(`+0x16` 의 `0x4000` 비트) 표시가 있으면 `+0x6a5` 에서 전역 카운터를 올리고 CPU 하나를 골라 친화도 마스크를 만든다. 조각 안 `KeQueryGroupAffinity` 는 그 CPU 가 그룹에 속하는지 확인. |
| 〃 | `alias:PsGetProcessSessionId+1` `+0x292` | 별칭은 `PsGetSessionId`. 같은 뜻. |
| `MiMapProcessExecutable` (참고) | `tail FUN_140b18902` `+0x137`, `frag:...+0x8` → `MI_READ_PTE_LOCK_FREE` | 이쪽도 진짜 조각이다 (RBP/R14/RBX 같은 틀, 끝에서 `JMP 0x14090ac1a` = 본체 `+0xd2` 로 돌아와 `PsMapSystemDlls` 호출로 이어짐). |
| `MmInitializeProcessAddressSpace` | `alias:RtlCopyMemory+3` `+0x232`, `+0x32b` | `memcpy` 계열, 문제 없음. |
| 나머지 (`PspMapSystemDll`, `MmCreateProcessAddressSpace`, `PsMapSystemDlls`, `PspSetupUserProcessAddressSpace`) | 없음 | |

## 동적분석으로 확인해야 할 것

dast 요청 필요:

1. **`MmInitializeProcessAddressSpace` 를 어느 호출이 탔나.** `+0x2095` (보통 길) 가 맞는지. cmd.exe 가 test.exe 를 만드는 한 번에서 `nt!PspAllocateProcess+0x2095` 와 `+0x2113` 중 어디가 맞는지 보면 된다.
2. **PEB 안 값을 눈으로.** `nt!PspAllocateProcess+0x2314` 에서 새 프로세스 PEB (`poi(새 EPROCESS+0x2e0)`) 의 `+0x10` (exe 주소), `+0xb8`, `+0xbc`, `+0x118~` 와 `+0x20` (아직 0 이어야 함) 을 읽는다. 그리고 `+0x239c` 이후에 `+0x20` 이 채워졌는지.
3. **간접 호출 두 곳의 대상.** `+0x17bf` 와 `+0x1215` 에서 `[nt+0xf04d20]`, `[nt+0xf04d28]` 값 (`ln` 으로 이름).
4. **ntdll 실제 주소.** `MiMapProcessExecutable +0xdc` 에서 `PsMapSystemDlls` 가 끝난 직후 새 프로세스의 ntdll 위치가 기대한 값(이후 `LdrInitializeThunk` 주소와 맞는지)인지.
5. 갈림길: 32비트(WOW64) 조건 `PspWow64PickBestNtdll` `+0xffb` 를 test.exe 가 타는지 (64비트면 안 타야 함). `KeSecureProcess` `+0x27a0` 와 `PspSetProcessAffinitySafe` `+0x2364` 도 탔는지.

추천 멈춤점 (조건: 부모 cmd.exe 스레드일 때만, run3 때와 같게):

- `nt!PspAllocateProcess+0x163c` — (가) 끝, 새 프로세스의 주소 공간 껍데기가 막 생긴 시점
- `nt!PspAllocateProcess+0x2095` — (나)(다) 끝
- `nt!MiMapProcessExecutable+0xb8` 와 `+0xdc` — exe 가 먼저 올라간 시점과 ntdll 이 올라간 시점을 나눠서 본다
- `nt!PspAllocateProcess+0x2314` — (라) 끝 (이미 run3 에서 관찰한 지점)
- `nt!PspAllocateProcess+0x239c` — `PspSetupUserProcessAddressSpace` 끝 (매개변수·ApiSet·silo 뷰가 붙은 시점)
- 참고: 위 오프셋은 모두 `ret_off` 이므로 WinDbg 콜스택의 `caller+0x..` 와 바로 비교하면 된다. `PspMapSystemDll` 은 입구에 걸면 부팅 때 System 프로세스에도 걸린다 (caller 에 `PspInitPhase3` 가 있음). 새 프로세스만 보려면 `PsMapSystemDlls` 입구나 `MiMapProcessExecutable +0xdc` 가 낫다.

다음 정적 요청 후보 (이번엔 안 뽑음): `PspCopyAndFixupParameters` (PEB `+0x20` 연결), `PspPrepareSystemDllInitBlock` (ntdll 로 넘기는 시작 정보 블록), `MmCreateSpecialImageSection` (이미 대기열 Q33).

## 판정

**분석성공.** 질문 1~5 모두 `.asm`/`_calls.tsv` 줄로 답했다. 알려진 사실 2건 일치, PDB 기준 GUID 일치. 요청서 밖으로 3개 함수를 더 뽑은 점(위 "뽑은 함수" 2번)과 스크립트 표기 불편 4건(맨 위)은 있지만 결과를 믿는 데는 영향 없다. `[정적]` 만 썼고, 실행 때 이 길로 갔다는 말은 하지 않았다 (질문 2의 "01_sprint 관찰과 맞다"는 추론으로 표시).

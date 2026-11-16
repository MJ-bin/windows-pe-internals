# 112 parent-handle-nls — 정적 분석 결과 (Q88 + Q89)

- 요청서: `_workspace/sast/112_parent-handle-nls.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), 커널 PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준값과 같음 (`ms/sym/ntoskrnl.exe.pdb.info.txt`).
- 분석: `.gpr` + `_index.tsv` + `SetPdb:` 줄이 다 있어서 **다시 안 걸었다.** ntdll 쪽은 건드리지 않았다 (읽기만: `ms/disassemble/ntdll/_calls.tsv` 에서 부르는 자리 세 줄 대조).
- 새로 뽑은 것 (로그 `ms/ghidra/ntoskrnl.export.sast112{a,b}.stdout`, `SCRIPT ERROR` 0):
  - a: `^(PspCreateObjectHandle|PspPropagateHandle)$` depth 1 → 5개: `PspCreateObjectHandle` · `PspPropagateHandle` · `ObOpenObjectByPointer` · `SepAdtPrivilegeObjectAuditAlarm` · `ObDuplicateObject`.
  - b: `^MiMapViewOfDataSection$` depth 0 → 1개. **요청서 범위 밖으로 한 개 넓힘** — 이유: Q89 "뷰를 붙이는 줄" 의 VAD 를 누가 만드는지 (`MiMapViewOfSection` 은 `MiMapViewOfDataSection` 으로 넘기기만 해서) 이름 한 단계 아래를 보려고. 본문은 칸 단위로는 안 읽음 (이름·호출 자리만).
  - 이미 있던 것 (안 뽑음): `NtCreateUserProcess` · `PspUpdateCreateInfo` · `NtGetNlsSectionPtr` · `RtlpInitNlsSectionName` · `MiMapViewOfSection` · `PspInsertThread` · `PspInsertProcess` · `PspAllocateThread` · `PspCaptureCreateInfo` · `PspBuildCreateProcessContext` · `PspSetupUserProcessAddressSpace` · `PspLocateInPEManifest` · `ObpCreateHandle` · `ObInsertObjectEx`.
- **버전 확인: 일치.** `_calls.tsv` `NtCreateUserProcess` call `+0xc31` / ret **`+0xc36`** → `PspAllocateProcess` · `PspInsertThread` call `+0x802` / ret **`+0x807`** → `PspCallProcessNotifyRoutines` (스킬 "알려진 사실" 둘 다 맞음).
- 읽는 법: `+0x..` 는 `.asm` 줄의 명령 오프셋. 부르기는 `call 자리 / ret 자리`. 조각 (`FUN_…`) 안 줄은 `.asm` 이 달아 둔 "(= 함수 +0x..)" 값으로 썼다.

## 스크립트 문제

없음. (`PspUpdateCreateInfo` 는 조각 셋 + "주인 없는 덩어리" 다섯이 붙은 함수라 `.asm` 아래쪽까지 봐야 한다. 읽는 데 지장은 없었다.)

## 먼저 알아 둘 것 — 요청서·옛 결과서 정정 둘

1. **요청서 Q89 "타입 11 = 코드 페이지 · 12 = 대문자 표" 에서 12 는 틀렸다.** 대문자 표는 **14 (`0xe`)** 이고, 12 (`0xc`) 는 정규화 표 (`\NLS\NlsSectionNORM…`) 다. 근거: `RtlpInitNlsSectionName.asm` `+0xd SUB ECX,0xb`(→ `0xb`) / `+0x10 JZ +0x88` · `+0x12 SUB ECX,1` / `+0x15 JZ` (→ `0xc`) · `+0x1b CMP ECX,2` / `+0x1e JNZ` 실패 (→ `0xe` 만 남음), `NtGetNlsSectionPtr.asm` `+0x152 CMP ESI,0xd` / `+0x155 JZ` (`0xd` 거름). sast/52 표와 같다.
2. **sast/52 질문 1 ③ 의 "`MiMapViewOfSection` (`+0x2e1` call)" 은 오타.** 진짜는 call **`+0x2e6`** / ret `+0x2eb` (`+0x2e1` 은 그 앞 `MOV RCX,[RSP+0x48]`). baggage 7.6.3 표의 "호출 `+0x2e6`" 이 맞다.

---

# Q88 — 부모 (cmd) 쪽에 쓰는 것

## 한눈에 (1장)

`NtCreateUserProcess` 끝 무렵 순서 (`.asm` 줄):

| 순서 | 자리 (`NtCreateUserProcess +`) | 하는 일 |
|---|---|---|
| ① | `+0xfbd`/`+0xfc2` `PspInsertProcess` | 새 프로세스를 이름 공간·장부에 **넣기만** (핸들 안 만듦). 접근 상태 `ACCESS_STATE` 를 여기서 처음 채움 |
| ② | `+0x1015`/`+0x101a` `PspInsertThread` | 스레드도 넣기 + **스레드 핸들을 만들어 부모 핸들 표에 넣고 부모 `ThreadHandle` 칸에 씀** (안쪽 `+0x520` `PspCreateObjectHandle`, `+0x573` 쓰기) |
| ③ | `+0x1043` `PsGetProcessServerSilo` → `+0x1048 CMP RAX,[RBP+0x348]` / `+0x104f JZ +0x1090` | 새 프로세스 서버 silo 가 기대값과 같을 때만 계속. 다르면 `ESI = 0xc000022d` (`+0x1051`) 로 빠짐 |
| ④ | `+0x10a2`/`+0x10a7` `PspCreateObjectHandle` | **프로세스 핸들** 을 부모 핸들 표에 만들기. 음수면 `+0x10ab JS +0x1160` 로 정리 |
| ⑤ | `+0x10b1 ~ +0x10e4` | `PS_ATTRIBUTE` "이미지 정보" 출력 칸 (0x40 바이트) 이 있으면 복사 `[범위 밖 — 아래 7.6.13 표에 한 줄]` |
| ⑥ | `+0x10e8 ~ +0x10f6` | **부모 `ProcessHandle` 칸 (8 바이트) 에 프로세스 핸들 값 쓰기** (`MOV RAX,[RBP+0x588]` / `MOV RCX,[RBP+0x88]` / `+0x10f6 MOV [RCX],RAX`) |
| ⑦ | `+0x1124`/`+0x1129` `PspUpdateCreateInfo(6, 만들기 문맥, 새 프로세스)` | **부모의 `PS_CREATE_INFO` 출력 칸 채우기** (`+0x111f MOV ECX,6`) |

- 스레드 핸들이 **프로세스 핸들보다 먼저** 만들어진다 (② → ④). `[정적]`
- ④·⑦ 이 실패하면 이미 만든 핸들을 닫는다: ⑦ 실패 `+0x112d JNS` 안 탐 → `+0x1133 ~ +0x115e` 프로세스 핸들 `ObCloseHandle`, 이어서 `+0x116b ~ +0x1196` 스레드 핸들 `ObCloseHandle` (닫을 때 모드: 핸들 속성 `RBP+0x580`/`+0x710` 에 `0x200` 이 있거나 `[R13+0x5fc] & 0x1000` — 현재 프로세스가 PDB `_EPROCESS.Flags3` 비트 12 `SystemProcess` — 이면 커널 모드 0, 아니면 유저 모드 1). 그 뒤 `+0x1060 SeDeleteAccessState` · `+0x106f PsTerminateProcess` (음수일 때).
- ①~③ 이 실패하면 ④ 와 ⑦ 에 안 온다 (`+0x1027 JNS`/`+0x103d JS`) → 부모 `PS_CREATE_INFO` 의 `State` 는 처음 값 (0) 그대로 `[정적 추론]`.

## 질문 1-1. `PspCreateObjectHandle` (rva `0x84509c`, 175 B)

`.c`: `ms/decomp/ntoskrnl/PspCreateObjectHandle.c`, `.asm`: `ms/disassemble/ntoskrnl/PspCreateObjectHandle.asm`. 원형 (읽은 것): `NTSTATUS PspCreateObjectHandle(PVOID 객체 RCX, 핸들블록* RDX, POBJECT_TYPE 형식 R8)`.

**핵심: 접근 마스크는 이 함수의 인자가 아니다.** 인자 `RDX` 는 **0x190 바이트 "핸들 만들기 블록"** 이다 (NtCreateUserProcess 안 지역 변수 둘: 프로세스 용 `RBP+0x400`, 스레드 용 `RBP+0x590`).

| 블록 칸 | 크기 | 뜻 (PDB `_ACCESS_STATE`·`_AUX_ACCESS_DATA` 와 맞춤) |
|---|---|---|
| `+0x00 ~ +0x9f` | 0xa0 | **`_ACCESS_STATE`** (`RemainingDesiredAccess` `+0x10` · `PreviouslyGrantedAccess` `+0x14` · `OriginalDesiredAccess` `+0x18` · `AuxData` `+0x48` …) |
| `+0xa0 ~ +0x17f` | 0xe0 | **`_AUX_ACCESS_DATA`** (`PrivilegesUsed` `+0x0` · `GenericMapping` `+0x8` …) |
| `+0x180` | 4 | **핸들 속성** (`HandleAttributes`) |
| `+0x184` | 1 | **접근 모드** (`PreviousMode`: 0 커널 / 1 유저) |
| `+0x188` | 8 | **핸들 값 출력** (`ObOpenObjectByPointer` 가 씀 — 커널 스택 칸, 부모 유저 메모리 아님) |

  - 0x190 = `memset(…,0,0x190)` — `NtCreateUserProcess +0xc1 MOV EBX,0x190` / `+0xd2` (프로세스 블록) · `+0x110 ~ +0x117` (스레드 블록). 두 칸 이름 (`+0x180` 등) 은 PDB 에 없다 (`pdb_struct.py --list` 로 확인: 이 크기의 이름 있는 타입 없음) `[정적 — 이름 없음]`. 앞 0x180 이 `_ACCESS_STATE` 0xa0 + `_AUX_ACCESS_DATA` 0xe0 인 것은 `PspInsertProcess +0x199 LEA R9,[RSI+0xa0]` (둘째 구조체 주소) 와 PDB 크기가 맞아서.

**`PspCreateObjectHandle` 이 하는 일** (`.asm`):
1. `+0x38`/`+0x3d` **`ObOpenObjectByPointer`**(객체, `HandleAttributes`=`[블록+0x180]` (`+0x32`), `PassedAccessState`=블록 (`+0x2f MOV R8,RDX`), `DesiredAccess`=**0** (`+0x28 XOR R9D,R9D`), 형식=인자 R8 (`+0x2b`), `AccessMode`=`[블록+0x184]` (`+0x10`·`+0x24`), 핸들 출력=`&[블록+0x188]` (`+0x16`·`+0x1d`)). `DesiredAccess` 가 0 인 것은 접근 상태가 이미 들어 있어서 — `ObOpenObjectByPointer.c` 가 `PassedAccessState` 가 NULL 일 때만 `DesiredAccess` 로 새 접근 상태를 만든다 (`SepCreateAccessStateFromSubjectContext`). `[정적]`
2. 성공하고 (`+0x3f TEST EAX / +0x41 JS +0x9d 빠짐`), 쓴 권한이 있고 (`+0x43 MOV RCX,[RBX+0x48]` 접근 상태 `AuxData` → `+0x47 MOV RDX,[RCX]` `PrivilegesUsed` 가 NULL 아님 `+0x4a/+0x4d`, `+0x4f CMP dword [RDX],0` / `+0x52 JBE` 권한 개수 > 0), **유저 모드 호출** (`+0x54 CMP byte [RBX+0x184],0` / `+0x5b JZ`) 이면 `+0x98`/`+0x9d` **`SepAdtPrivilegeObjectAuditAlarm`** (권한 쓴 감사). 값을 바꾸지 않는다 — 감사 기록용.
3. 돌려주는 값 = `ObOpenObjectByPointer` 의 상태 그대로 (`+0xa2 MOV EAX,EDI`).

**핸들이 부모 표에 들어가는 길** `[정적]`: `ObOpenObjectByPointer +0x1ba`/`+0x1bf` `ObpCreateHandle` (공용 부품 — 이름만). 핸들 표는 **현재 프로세스 = 부모 cmd** 의 것이다 (아직 새 프로세스로 `KeStackAttachProcess` 하지 않은 시스템 콜 안, `GS:[0x188]+0xb8` 가 부모). 속성에 `OBJ_KERNEL_HANDLE (0x200)` 가 없으니 (아래) 커널 핸들 표가 아니라 **부모의 유저 핸들 표**.

**접근 마스크 (`[정적]`, 공용 부품 한 번만 봄)**
- 줄기: `NtCreateUserProcess` 의 `ProcessDesiredAccess`(`[RBP+0x54]`, 인자 R8D)·`ThreadDesiredAccess` (`[RBP+0x50]`, R9D) → 아래 둘째 줄 → 접근 상태.
  - 보호 프로세스 호환 검사 `+0xe8f`/`+0xe94 PsTestProtectedProcessIncompatibility` 가 **0 이면** (`+0xe96 JZ 0x140a022af` = 함수 시작 + `0xf6f`) `ESI=[RBP+0x50]`, `R8D=[RBP+0x54]` **그대로**. **1 이면** 인자에 `MAXIMUM_ALLOWED` (비트 25, `+0xef5 BT R8D,0x19`) 가 있을 때 표 `RtlProtectedAccess2` 쪽 (`0x140b4a890`, 칸 12 B, `+4` 프로세스 · `+8` 스레드) 에서 `~값 & 0x1fffff` 를 더하고 비트 25 를 끈다 (`+0xf08 ~ +0xf16`, 스레드 `+0xf44 ~ +0xf51`). 그 뒤 **비트 0 (=1) 도 켠다** (`+0xf2c OR R8D,R14D`, `R14D=1` `+0x320`) — 조건 `[RBP+0x228]` (부모 속성으로 받은 프로세스) 가 NULL 이거나 현재 프로세스 (`R13`) 와 같을 때. 이 비트 0 이 무슨 권한인지는 이름만 짐작 (`PROCESS_TERMINATE` = 1) `[추측]`.
  - test.exe (보호 프로세스 아님) 가 어느 쪽인지는 런타임 값 — 동적 항목 D4.
- 접근 상태 채우기: 프로세스 `PspInsertProcess +0x1a5` / 스레드 `PspInsertThread +0x3aa` **`SeCreateAccessStateEx`** (인자: `DesiredAccess`=`[RBP+0x50]`(R13D) / `[RSP+0xf0]`, 일반 매핑 = 형식 개체 `+0x4c` (`PsProcessType`/`PsThreadType` + `0x4c` = `TypeInfo.GenericMapping`)). 이어 `PspInsertProcess +0x1de` / `PspInsertThread +0x3e5` **`ObInsertObjectEx`** (핸들 출력 인자 0 — 핸들 안 만듦).
- 핸들 항목에 들어가는 값: `ObpCreateHandle +0x3cc ~ +0x431` — `RemainingDesiredAccess` 의 `MAXIMUM_ALLOWED` 를 `GENERIC_ALL` 로 바꿔 (`+0x3cf BT EAX,0x19` / `+0x3d5 BTR` / `+0x3d9 BTS EAX,0x1c`) 형식 일반 매핑 (`[형식+0x4c ~ +0x58]`) 으로 풀고, `([+0x10] | [+0x14]) & (형식 ValidAccessMask [+0x5c] | 0x1000000)` 을 `PreviouslyGrantedAccess` 에 씀 (`+0x41b ~ +0x431`). 갈래 조건은 안 봄 `[추측: 핸들 열기 길]`. 곧 **핸들의 접근 = 부모가 준 요청 (보호 프로세스면 줄인 것) 을 형식 유효 마스크로 자른 값** — 최종 숫자는 동적 D2.

**속성 (`[정적]`)**: 블록 `+0x180` = 부모가 준 `ProcessObjectAttributes.Attributes` / `ThreadObjectAttributes.Attributes` 의 **`& 0x1df2`** (유저 호출) 또는 `& 0x11ff2` (커널 호출). 개체 속성이 NULL 이면 0.
  - 프로세스: `NtCreateUserProcess +0x251 MOV EAX,[R13+0x18]` · `+0x261 AND EAX,0x1df2` (유저, `+0x258 JNZ`) / `+0x25a AND EAX,0x11ff2` (커널) · `+0x266 MOV [RBP+0x580],EAX`.
  - 스레드: `PspAllocateThread +0x203 MOV EAX,[R9+0x18]` · 조각 `+0x2ca883 AND EAX,0x1df2` (유저, `+0x1fa CMP DL,R14B` / `+0x1fd JZ`) / `+0x207 AND EAX,0x11ff2` (커널) · `+0x20c MOV [R11+0x180],EAX` (`R11` = 열두째 인자 = 스레드 블록 `[RSP+0x278]`, `+0xb7`). 개체 속성 NULL 이면 `+0x1cd MOV [R11+0x180],EDI(0)`.
  - `0x1df2` 의 켜진 비트: `0x2` 상속 · `0x10` 영구 · `0x20` 독점 · `0x40` 대소문자 무시 · `0x80` 있으면 열기 · `0x100` 링크 열기 · `0x400` 접근 검사 강제 · `0x800` · `0x1000`. **`0x200` (커널 핸들) 은 없다** → 유저 호출이면 부모 유저 핸들. (이름은 공개 헤더 값, PDB 아님 `[추측-이름]`.)
  - 접근 모드 `+0x184` = `PreviousMode`: 프로세스 `NtCreateUserProcess +0x1eb MOV [RBP+0x584],R12B`, 스레드 `PspAllocateThread +0x1db MOV [R11+0x184],DL` (`DL` = `[RSP+0x230]` = 셋째 인자 `R8B` 의 집, 호출 `NtCreateUserProcess +0xe46~+0xe49 MOV R8B,BL`, `BL=[RBP+0xd]` = `PreviousMode`).

**부모 쪽 출력 인자 (`ProcessHandle`·`ThreadHandle`) 에 쓰는 줄 `[정적]`**:

| 출력 | 값 | 쓰는 줄 | 미리 확인 |
|---|---|---|---|
| `ProcessHandle` (인자 1, 부모 유저 8 B) | `[RBP+0x588]` = 프로세스 블록 `+0x188` | `NtCreateUserProcess +0x10e8 MOV RAX,[RBP+0x588]` / `+0x10ef MOV RCX,[RBP+0x88]` / **`+0x10f6 MOV [RCX],RAX`** | 입구 `+0x201 ~ +0x216` — 유저 호출이면 "읽고 같은 값 다시 쓰기" 로 쓰기 가능한지 건드림 (한계 `0x7fffffff0000` 으로 누름) |
| `ThreadHandle` (인자 2, 부모 유저 8 B) | `[스레드 블록+0x188]` | **`PspInsertThread +0x573 MOV [RAX],RCX`** (`+0x564 MOV RCX,[RBX+0x188]` / `+0x56b MOV RAX,[RSP+0x118]` = 열째 인자 `[RBP+0x80]`) | 입구 `+0x219 ~ +0x22e` 같은 방식 |

- 스레드 쪽은 예외 필터 `PspInsertThread$filt$1` (`CMP byte [RCX+0x184],1` / `SETZ`) — **유저 호출일 때만** 예외를 삼킨다. 쓰기가 실패하면 (`+0x55e JS +0xa4a`) 방금 만든 스레드 핸들을 닫는다 (`+0xa4a ~ +0xa72` → `ObCloseHandle`).
- `NtCreateUserProcess` 에는 예외 필터 `$filt$0` (34 B) 가 하나 있다. 프로세스 핸들 쓰기 (`+0x10f6`) 가 그 `try` 안인지는 예외 표를 안 봐서 `[공백]`.

## 질문 1-2. `PspUpdateCreateInfo` (rva `0xa48da4`, 386 B + 조각)

원형: `NTSTATUS PspUpdateCreateInfo(ULONG 상태 ECX, 만들기 문맥* RDX, EPROCESS* R8)`. `RDX` = 0x260 바이트 문맥 (`NtCreateUserProcess +0x26e/+0x274 memset(RBP+0x1a0, 0, 0x260)`, `PspBuildCreateProcessContext` 가 채움). `R8` = 새 프로세스 (실패 호출은 0).

**부모 `PS_CREATE_INFO` 어디 있나 `[정적]`**: 문맥 `+0x30` = **부모 유저 메모리의 `PS_CREATE_INFO` 주소 그 자체** (커널 사본 아님). `PspCaptureCreateInfo` 끝 `*(문맥+0x30) = 사용자 포인터` (`.c` 마지막 줄) 가 심고, 입구에서 크기 `0x58` (`*param_2 == 0x58`), 상태 `+8 == 0` 을 검사하며 첫 바이트와 `+0x57` 바이트를 "읽고 같은 값 다시 쓰기" 로 건드려 쓸 수 있는지 확인한다 (유저 호출). `PspUpdateCreateInfo` 는 `RBX = [RDX+0x30]` (`+0x1d`) 로 이 칸에 **곧장** 쓴다 (`PspUpdateCreateInfo` 안에는 `try` 가 없다 — 바깥 `NtCreateUserProcess` 의 `$filt$0` 가 덮는지는 `[공백]`).

**PDB 에 `_PS_CREATE_INFO` 가 없다** (`pdb_struct.py --list ntoskrnl.exe '^_?PS_'` 에 없음 — `_PS_*` 중 비슷한 것 없음). 아래 칸 **이름은 공개 헤더 (ntpsapi.h 꼴) 기억에서 붙인 것 `[추측-이름]`**이고, 오프셋·크기·값 출처는 `.asm` 근거 `[정적]`. 상태 번호의 의미 (1 파일 열기 실패 · 2 섹션 만들기 실패 · 3 실행 형식 불량 · 4 머신 불일치 · 5 이름 불량 · 6 성공) 도 같은 이유로 이름은 `[추측-이름]`, 분기 모양은 `[정적]`.

**호출 자리와 조건**

| 호출 | 상태 (`ECX`) | 문맥 / 프로세스 | 조건 |
|---|---|---|---|
| `NtCreateUserProcess +0x75a`/`+0x75f` | **1** (`+0x74d MOV ECX,R14D`, `R14D=1` `+0x320`) 또는 **2** (`+0xaff MOV ECX,2` → `+0xb04 JMP +0x750`) | `RBP+0x1a0` / 0 | 실행 파일 열기 실패 (`+0x6cf JNS` 안 탐 → `+0x744 JNS` 안 탐 → 1) · 섹션 만들기 실패 (`+0x940 JS +0xaf8` → 2) |
| `PspAllocateProcess +0xa06`/`+0xa0b` | **3** (`+0x9fb MOV ECX,3`) | 문맥 `R15` / 0 | 부모가 준 "금지 이미지 특성" (문맥 `+0x12` 워드) 과 이미지 특성 (`+0x64`) 이 겹침 (`+0x9ef TEST word [R15+0x12],AX` / `+0x9f4 JZ`) → `EDI = 0xc000007b` |
| **`NtCreateUserProcess +0x1124`/`+0x1129`** | **6** (`+0x111f MOV ECX,6`) | `RBP+0x1a0` / `[RBP+0x18]` 새 프로세스 | `PspInsertProcess` 성공 · `PspInsertThread` 성공 · 서버 silo 같음 · `PspCreateObjectHandle` (프로세스) 성공 (`+0x1110 TEST ESI / +0x1112 JS`). **상태 4·5 는 이 파일 안에서 부르는 자리를 못 찾음** (다른 호출 자리 `[공백]`) |

**상태별로 쓰는 칸 (`.asm` 줄)** — 공통 문: 입구 `+0x4b JZ +0x189`: **문맥 `+0x10` 비트 1 (`TEST byte [RDX+0x10],2`) 이 꺼져 있으면 `State` (`+0x8`) 만 쓴다.** 이 비트는 `PspCaptureCreateInfo` 가 부모 `PS_CREATE_INFO +0x10` 의 **비트 0 (`WriteOutputOnExit` 로 읽힘 `[추측-이름]`)** 을 옮긴 것 (`.c`: `((char)param_2[2]*2 ^ bVar2) & 2 ^ bVar2` → 비트 1). 부모 쪽이 이걸 켜 주는지는 nt 쪽에선 안 보임 → D1.

| 상태 | 쓰는 칸 | 줄 |
|---|---|---|
| 1 · 4 · 그 밖 | `State` 만 | `+0x6f JNZ +0x189` → 끝 `+0x18d ~ +0x191` |
| 2 | `+0x10` ← 문맥 `+0xb0` 의 핸들 (파일 핸들) 을 **부모 표로 옮긴 값** | `+0x240 ~ +0x263` (`+0x24f` `PspPropagateHandle`, ret `+0x254`) |
| 3 | `+0x10` (워드) ← 문맥 `+0x66` 워드 | `+0x21c ~ +0x220` |
| 5 | `+0x10` ← 문맥 `+0xc8` 핸들을 부모 표로 옮긴 값 | `+0x1dc ~ +0x203` (`+0x1eb` `PspPropagateHandle`) |
| **6 (성공, test.exe 의 부모 쪽)** | 아래 표 | `+0x75 ~ +0x175` + 끝 `+0x191` |

**성공 (상태 6) 의 쓰기 — 부모 `PS_CREATE_INFO` (0x58 B)**

| 부모 칸 | 이름 `[추측-이름]` | 크기 | 값 | 출처 | 줄 (`PspUpdateCreateInfo +`) |
|---|---|---|---|---|---|
| `+0x10` 비트 0 | `OutputFlags.ProtectedProcess` | 1 bit | 새 프로세스 `EPROCESS.Protection.Type` (`+0x5fa`, PDB `_PS_PROTECTION` `Type` 0:3) ≠ 0 | 새 프로세스 `[R13+0x5fa] & 7` | `+0xc3 ~ +0xce` (`SETA`) |
| `+0x10` 비트 4 | `…ProtectedProcessLight` | 1 bit | `Protection.Type == 1` | 같음 | `+0xd1 ~ +0xe9` (`CMP AL,1 / SETNZ / DEC / AND 0x10`) |
| `+0x10` 비트 1 | `…AddressSpaceOverride` | 1 bit | `EPROCESS.Flags` (`+0x1f4`) 비트 17 `OverrideAddressSpace` (`[R13+0x1f6] & 2`, PDB) | 새 프로세스 | `+0xe8 ~ +0xf4` |
| `+0x10` 비트 2 | `…DevOverrideEnabled` | 1 bit | 커널 전역 `PspGlobalFlags` (`0x140fd9bac`) 비트 0 | 전역 | `+0xf7 ~ +0x104` |
| `+0x10` 비트 3 | `…ManifestDetected` | 1 bit | 문맥 `+0x10` 비트 5 (`[RSI+0x10]>>2 & 8`) | `PspLocateInPEManifest +0x88 OR byte [RBX+0x10],0x20` (매니페스트 찾았을 때) | `+0x107 ~ +0x112` |
| `+0x10` 나머지 | — | — | 0 (`+0xbf AND dword [RBX+0x10],0` 로 4 바이트를 먼저 0 으로) | — | `+0xbf` |
| `+0x18` | `FileHandle` | 8 | 부모 표로 옮긴 파일 핸들 값 (아니면 0) | 문맥 `+0xb0` (`NtCreateUserProcess +0x6bf LEA RCX,[RBP+0x250]` → `IopCreateFile`) 를 `PspPropagateHandle` | `+0x8a`(call)/`+0x8f` · 쓰기 `+0x133 MOV [RBX+0x18],RAX` |
| `+0x20` | `SectionHandle` | 8 | 부모 표로 옮긴 섹션 핸들 값 (아니면 0) | 문맥 `+0xc0` (`+0x930 LEA RCX,[RBP+0x260]` → `MmCreateSpecialImageSection`) 를 `PspPropagateHandle` | `+0xa8`(call)/`+0xad` · 쓰기 `+0x13c` |
| `+0x28` | `UserProcessParametersNative` | 8 | **새 프로세스** 가상 주소 공간 안 매개변수 사본 주소 | 문맥 `+0xe0` ← `PspSetupUserProcessAddressSpace +0x170 LEA RDX,[RSI+0xe0]` / `+0x197 ZwAllocateVirtualMemory` | `+0x140 ~ +0x147` |
| `+0x30` | `UserProcessParametersWow64` | 4 | 문맥 `+0xe8` (64 비트 프로세스는 0 — 문맥을 0 으로 `memset`, 쓰는 자리 못 봄) | 문맥 | `+0x14b ~ +0x151` |
| `+0x34` | `CurrentParameterFlags` | 4 | 문맥 `+0xd8` (부모 블록의 **커널 사본**, run104) 가 가리키는 구조체 `+0x8` = `RTL_USER_PROCESS_PARAMETERS.Flags` | 문맥 | `+0x154 ~ +0x15e` |
| `+0x38` | `PebAddressNative` | 8 | `EPROCESS.Peb` (`+0x2e0`) = 새 프로세스 PEB 주소 | `PspAllocateProcess +0x22f2 LEA R12,[R14+0x2e0]` 를 `+0x230f MmCreatePeb` 의 출력 인자로 넘김 | `+0x161 ~ +0x168` |
| `+0x40` | `PebAddressWow64` | 4 | 문맥 `+0xec` (64 비트는 0) | 문맥 | `+0x16c ~ +0x172` |
| `+0x48` | `ManifestAddress` | 8 | 문맥 `+0x130` — **`ManifestDetected` 가 켜졌을 때만** | `PspLocateInPEManifest +0x2f LEA RAX,[RBX+0x130]` (→ `LdrResSearchResource`) | `+0x11a ~ +0x121` (`+0x115 TEST CL,8`) |
| `+0x50` | `ManifestSize` | 4 | 문맥 `+0x138` — 같은 조건 | `PspLocateInPEManifest +0x8c MOV [RBX+0x138],EAX` | `+0x125 ~ +0x12b` |
| **`+0x08`** | **`State`** | 4 | **6 — 맨 마지막에 씀** | 입력 `ECX` (`+0x5 MOV [RSP+0x8],ECX` → 끝 조각) | `+0x191 MOV [RBX+8],EAX` (`+0x189 TEST EDI,EDI / +0x18b JS` 로 실패면 건너뜀) |

- 파일 핸들·섹션 핸들 두 칸은 **`ctx+8` (문맥 `+0x8`) 의 비트 5** 가 켜져 있을 때만 (`+0x75 TEST byte [RSI+8],0x20` / `+0x79 JZ +0xbf`). 문맥 `+0x8` 은 `PspBuildCreateProcessContext` 가 속성 목록에서 본 **속성 번호별 비트** (`*(param_4+8) |= 1 << (번호 & 0x3f)`, `.c` 줄 175~176) 라서 **비트 5 = 속성 `0x20005` (이미지 이름, `.c` 줄 321 `local_1d0 == 0x20005`)** 이 있을 때. 곧 **부모가 이미지 이름 속성을 주면** 파일·섹션 핸들 둘이 부모 표에 더 생긴다. 이름 속성을 주는지는 KERNELBASE 쪽이라 이 정적 범위 밖 — D1.
- **`PspPropagateHandle` (rva `0xa49048`, 한 함수)** (`.asm`):
  - 호출 모드가 **커널 (`CL == 0`)** 이면 핸들을 그대로 넘기고 원본 칸을 0 으로 (`+0x48 MOV [R8],R10` / `+0x4b MOV [RDX],RAX(0)`).
  - **유저 호출** 이면 `+0x3d`/`+0x42` **`ObDuplicateObject`**(원본 프로세스 = 현재, 원본 핸들 = `[RDX]`, 대상 프로세스 = 현재, 출력 `R9`=`R8`, 접근 0 (`+0x39 MOV [RSP+0x20],EAX`), 속성 0 (`+0x2f`), 옵션 `2` (`+0x24 MOV [RSP+0x30],2` = 같은 접근 복사), 모드 0 (`+0x1d`)). 곧 **원본은 그대로 두고 부모 (현재) 핸들 표에 새 유저 핸들을 하나 더** 만든다 (옵션에 "원본 닫기" `1` 없음). 원본이 커널 핸들인 이유: 파일을 열 때 객체 속성이 `0x640`/`0x240` (`NtCreateUserProcess +0x56e MOV EAX,0x640` / `+0x573 MOV ECX,0x240` / `+0x578 CMP R12B,R14B` / `+0x57b CMOVNZ`, `+0x57e MOV [RBP+0xd0],EAX`) = `0x200` (커널 핸들) 포함 `[정적]` → 원본은 커널 핸들 표의 핸들일 것 `[추측]`. 원본 커널 핸들을 누가 닫는지는 이번에 안 봄 (`+0x11d0 PspDeleteCreateProcessContext` 일 것 `[추측]`).
  - 둘째 복제가 실패하면 첫째를 닫는다 (`+0xb1 JS +0x1d1` → `+0x1bc ~ +0x298` 의 `ObCloseHandle` 반복, `R15D=2` 번).
- 반환: 평소 0. 음수는 `PspPropagateHandle` 복제 실패뿐 (`+0x1a7 XOR EAX,EAX`).

## 질문 1-3. 부르는 자리·조건 요약

- `PspCreateObjectHandle`: **`NtCreateUserProcess +0x10a2`** (프로세스), **`PspInsertThread +0x520`** (스레드, `PspInsertThread` 는 `NtCreateUserProcess +0x1015`). 그 밖 호출자 (`PspCreateProcess +0x533`, `PsCreateMinimalProcess`) 는 test.exe 길 아님.
- `PspUpdateCreateInfo`: 위 표. 성공 길 한 자리 (`+0x1124`).
- 조건: 위 "한눈에" ①~③ + 부모 `ProcessHandle`·`ThreadHandle` 칸이 쓸 수 있을 것 (입구 검사).

## 질문 2. 짐 지도 관점 — 부모 쪽 짐 목록 (baggage 7.6 "체크리스트 밖", 새 7.6.13 제안)

**이 짐은 test.exe 의 짐이 아니라 부모 (cmd) 의 짐이다.** 공간은 둘: **부모 핸들 표 (커널)** 과 **부모 유저 메모리**. 단계 `①`. 쓴 주인 = nt. 칸 이름은 PDB 에 있는 것만 칸 단위, 없는 것 (`PS_CREATE_INFO` · 핸들 만들기 블록) 은 오프셋으로. 줄임 (3장 줄임표에 더하자고 제안):

| 줄임 | 사슬 |
|---|---|
| `①OP…` | `①… → PspCreateObjectHandle (호출 +0x10a2) → ObOpenObjectByPointer (호출 +0x38) → ObpCreateHandle (호출 +0x1ba)` — 프로세스 핸들 |
| `①OT…` | `①… → PspInsertThread (호출 +0x1015) → PspCreateObjectHandle (호출 +0x520) → ObOpenObjectByPointer (호출 +0x38) → ObpCreateHandle (호출 +0x1ba)` — 스레드 핸들 |
| `①UC…` | `①… → PspUpdateCreateInfo (호출 +0x1124)` — `①UC… +0x..` 는 `PspUpdateCreateInfo` 안 줄 |

**(가) 부모 핸들 표 — 커널 공간 (부모 `_HANDLE_TABLE`, 3.16 과 같은 구조체)**

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| 새 핸들 항목 ① — **스레드 핸들** (`_HANDLE_TABLE_ENTRY`, 0x10 B) | ① | 새 `_ETHREAD` 참조 · 접근 = 부모가 준 `ThreadDesiredAccess` (보호 프로세스면 줄임) 를 `PsThreadType` 일반 매핑·유효 마스크로 자른 값 · 속성 = 부모 `ThreadObjectAttributes.Attributes & 0x1df2` · 모드 유저 (1) | 직접 (공용 부품 `ObpCreateHandle`) | nt | `①OT… +0x3d` (`ObOpenObjectByPointer`) | `[정적]` sast112 · 칸 단위 값은 `[공백]` (`_HANDLE_TABLE_ENTRY` 칸 이름·핸들 값은 동적 D2) |
| 새 핸들 항목 ② — **프로세스 핸들** | ① | 새 `_EPROCESS` 참조 · 접근 = `ProcessDesiredAccess` 를 `PsProcessType` 으로 · 속성 = `ProcessObjectAttributes.Attributes & 0x1df2` · 유저 모드 | 직접 | nt | `①OP… +0x3d` | 같음 · 스레드 핸들 **뒤** (순서 `[정적]`) |
| 새 핸들 항목 ③ — **파일 핸들 사본** | ① | exe 파일 핸들을 부모 표에 한 개 더 (같은 접근, 속성 0, 유저 핸들) — **이름 속성 `0x20005` 가 있고 `WriteOutputOnExit` 가 켜졌을 때만** | `ObDuplicateObject` (옵션 2) | nt | `①UC… +0x8a → PspPropagateHandle (호출 +0x8a) → ObDuplicateObject (호출 +0x3d)` | `[정적]` sast112 · 조건 값 `[공백]` D1 |
| 새 핸들 항목 ④ — **섹션 핸들 사본** | ① | 새 exe 섹션 핸들 한 개 더 (같음) | 같음 | nt | `①UC… → PspPropagateHandle (호출 +0xa8) → ObDuplicateObject (호출 +0x3d)` | 같음 |
| 부수 칸 — 새 프로세스·스레드 객체 `_OBJECT_HEADER` 의 핸들 수·참조 수, 부모 `_HANDLE_TABLE` 의 `HandleCount`·`FreeLists`·`TableCode`(커질 때) | ① | +1 (두 객체), 표 쪽 칸 값 | 직접 (공용 부품) | nt | `①OP/OT… → ObpCreateHandle → ObpIncrementHandleCountEx (호출 +0x50b) · ExpAllocateHandleTableEntrySlow (호출 +0x17bb, 표 커질 때)` | `[정적]` 이름만 (칸 단위 안 봄) · 3.16 의 `ObpCreateHandle`·`ExCreateHandleEx` 줄과 같은 공용 부품 |

**(나) 부모 유저 메모리 (cmd 의 가상 주소 공간)**

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 | 근거 |
|---|---|---|---|---|---|---|
| `ProcessHandle` 출력 칸 (8 B, 부모 변수) | ① | 프로세스 핸들 값 | 직접 | nt | `①… +0x10f6 MOV [RCX],RAX` | `[정적]` sast112 |
| `ThreadHandle` 출력 칸 (8 B) | ① | 스레드 핸들 값 (프로세스 핸들보다 **먼저** 쓰임) | 직접 | nt | `①OT… → PspInsertThread +0x573 MOV [RAX],RCX` | `[정적]` sast112 |
| `PS_CREATE_INFO` `+0x10` `OutputFlags` (4 B) | ① | 비트 0 ProtectedProcess · 1 AddressSpaceOverride · 2 DevOverrideEnabled · 3 ManifestDetected · 4 ProtectedProcessLight (`[추측-이름]`) — 안 켜진 비트는 0 | 직접 | nt | `①UC… +0xbf ~ +0x112` | `[정적]` sast112 · 조건 = `State` 6 + `WriteOutputOnExit` |
| `PS_CREATE_INFO` `+0x18` `FileHandle` (8 B) · `+0x20` `SectionHandle` (8 B) | ① | (가) ③④ 의 핸들 값 (아니면 0) | 직접 | nt | `①UC… +0x133 · +0x13c` | `[정적]` sast112 |
| `PS_CREATE_INFO` `+0x28` `UserProcessParametersNative` (8 B) · `+0x30` (4 B) · `+0x34` `CurrentParameterFlags` (4 B) | ① | 새 프로세스 안 매개변수 사본 주소 · (Wow64 쪽 0) · 부모 블록 사본 `Flags` | 직접 | nt | `①UC… +0x140 · +0x14b · +0x154` | `[정적]` sast112 · 같은 주소를 새 프로세스 PEB `ProcessParameters` (3.1) 가 가짐 — 값이 같은지는 `[공백]` |
| `PS_CREATE_INFO` `+0x38` `PebAddressNative` (8 B) · `+0x40` (4 B) | ① | `EPROCESS.Peb` · (Wow64 0) | 직접 | nt | `①UC… +0x161 · +0x16c` | `[정적]` sast112 |
| `PS_CREATE_INFO` `+0x48` `ManifestAddress` (8 B) · `+0x50` `ManifestSize` (4 B) | ① | 새 프로세스 exe 의 매니페스트 리소스 주소·크기 (찾았을 때만) | 직접 | nt | `①UC… +0x11a · +0x125` | `[정적]` sast112 |
| `PS_CREATE_INFO` `+0x08` `State` (4 B) | ① | **6** — 다른 칸을 다 쓴 **뒤** 마지막에 | 직접 | nt | `①UC… +0x191` | `[정적]` sast112 |
| `PS_ATTRIBUTE` 이미지 정보 출력 칸 (0x40 B) `[범위 밖 — 이름만]` | ① | 새 exe `SECTION_IMAGE_INFORMATION` 사본 (`NtCreateUserProcess +0x10bd ~ +0x10e4` 가 문맥 `+0x38 ~ +0x77` 을 네 번 16 B 복사) — 포인터 = 문맥 `+0x28` (`PspBuildCreateProcessContext.c` 줄 231, 속성 `6`, 크기 `0x40`) | 직접 | nt | `①… +0x10bd ~ +0x10e4` | `[정적]` sast112 · 칸 단위는 3.23 (커널 쪽 같은 구조체) 참고 |
| `PS_ATTRIBUTE` `ClientId` (0x10 B) · `TebAddress` (8 B) 출력 칸 · 각 속성 `ReturnLength` `[범위 밖 — 쓰는 줄 못 찾음]` | ① | 새 프로세스·스레드 PID·TID, 새 TEB 주소 — 문맥 `+0x18`·`+0x20` 가 포인터 (`.c` 줄 251·286). `ReturnLength` 는 목록을 읽을 때 (`PspBuildCreateProcessContext`) `0x40`·`0x10`·`8` 을 이미 씀 (`.c` 줄 246 `*local_290 = 0x40` · 281 `= 0x10` · 그 뒤 `= 8` — `.c` 만 봄, `.asm` 오프셋 대조 안 함) | 직접 | nt | `[공백]` (`PspAllocateThread` 안 `[추측]`) | `[정적]` 포인터 자리만 · 쓰는 줄 `[공백]` → 다음 요청 후보 |

- 이 목록은 7.6 의 "체크리스트 밖" 에 새 절 (제안 번호 7.6.13) 로. **2장 숫자에는 안 센다.** 부모 쪽이라 test.exe 의 `_PEB`·`_EPROCESS` 칸 값에는 영향이 없다 — 단 `PS_CREATE_INFO` `+0x28`·`+0x38` 값은 새 프로세스 쪽 `_PEB.ProcessParameters` (`+0x20`)·`EPROCESS.Peb` 와 같은 주소여야 하는 짝이다 (3.1·3.11 참고).
- 핸들 값 (예: `0x…`) 은 사진 (run44) 에서 부모 cmd 표를 안 읽어서 `[공백]` (사진은 test.exe 프로세스 문맥만).

## Q88 — 지금 우리 (PE_emulator) 맞나·틀리나

- **첫 프로세스 길 (`Src/nt/Process.cpp` `PspCreateObjectHandle`·`PspUpdateCreateInfo` 빈 함수, "안 함"): 맞음.** 부모가 없으니 쓸 칸이 없다. 그 주석의 줄 (`+0x10b1 ~ +0x10f6`·"성공 길 값 6"·"실패 길 `+0x75f` 값 1·2") 도 이번 `.asm` 과 같다 (6 이 성공 분기 한 곳으로 가는 것 `[정적]` 확인, **"6=성공" 의 이름만 `[추측-이름]` 로 남음**).
- **게스트가 `NtCreateUserProcess` 를 부르는 길 (`ext/syscall/syscall_layer_cpp/src/process/nt_create_user_process.cpp`) — 부분 일치, 다른 점 셋 (읽기만 함, 고치지 않음):**
  1. 속성 마스크 `& 0x1df2` — **맞음** (`+0x261`·`+0x2ca883`).
  2. `MAXIMUM_ALLOWED` → 모든 권한 — **방향 맞음** (`ObpCreateHandle +0x3d9` 가 `GENERIC_ALL` 로 바꿔 일반 매핑으로 풂), 단 진짜는 형식 일반 매핑 그대로 (프로세스 `0x1fffff` 로 가정은 `확인 필요`: `PsProcessType+0x4c` 값).
  3. **다름 (a) 핸들 순서**: 에뮬레이터는 프로세스 핸들 → 스레드 핸들 (코드 `handles_.allocate` 두 줄). 진짜는 **스레드 먼저** (`PspInsertThread +0x520` → `NtCreateUserProcess +0x10a2`). 핸들 번호가 중요하면 바꿔야 함 — 실제 번호는 표의 빈 칸 규칙에도 좌우되니 동적 D2 로 확인 필요.
  4. **다름 (b)** `PS_CREATE_INFO` 성공 때: 에뮬레이터는 `Size`·`State=6`·`PebAddressNative` 만 쓰고 나머지 0. 진짜는 `OutputFlags` (`+0x10`) · `UserProcessParametersNative` (`+0x28`) · `CurrentParameterFlags` (`+0x34`) 도 쓰고, 이미지 이름 속성이 있으면 `FileHandle`·`SectionHandle` (+ 부모 표에 핸들 둘) · 매니페스트가 있으면 `ManifestAddress/Size`. 부모 코드가 이 칸들을 읽는지는 KERNELBASE 쪽이라 `확인 필요`.
  5. `Size` (`+0x0`) 는 진짜가 안 쓴다 (입구 검사만). 에뮬레이터가 0x58 을 쓰는 것은 부모가 이미 0x58 을 넣은 값이라 같은 결과.
- 짐 지도 줄 (질문 2): 에뮬레이터 쪽 해당 없음 (부모 cmd 가 없음).

---

# Q89 — `NtGetNlsSectionPtr` 커널 쪽 (일 47)

**sast/52 (질문 1) 가 이미 답한 것은 근거만 옮긴다.** 이번에 다시 읽어 확인한 줄은 ✔, 새로 본 것은 ★.

## 질문 3-1. 이름·갈래 (인자별)

원형 (`.asm` 입구): `NtGetNlsSectionPtr(종류 ECX→ESI, 데이터 EDX→R13D, 섹션객체출력 R8→RDI, 뷰주소출력 R9→R14, 뷰크기출력 [스택]→R15)` — 시스템 콜 `0x102`, 인자 5개 (`_workspace/sast/51_syscall-args_table.tsv`). 이름은 쓰임으로 붙인 `[추측-이름]` ✔.

| 종류 | 섹션 이름 (`\NLS\` 밑) | 만들기 갈래 파일 | test.exe 가 타나 | 근거 ✔ |
|---|---|---|---|---|
| **`0xb` (11) 코드 페이지** | `\NLS\NlsSectionCP<둘째 인자 10진>` (예 `…CP949`) | `\SystemRoot\System32\c_<번호 %.3d>.nls` | **예** — ANSI (`RtlpInitCodePageTables +0x46`), ANSI≠OEM 이면 OEM (`+0x6c`) 도 | `RtlpInitNlsSectionName +0xd/+0x10`, `+0x88 ~ +0x9a` · `RtlpInitNlsFileName +0x192 ~ +0x1a9` |
| **`0xe` (14) 대문자 표** | `\NLS\NlsSectionCASE` (고정, 둘째 인자 안 씀) | `\SystemRoot\System32\l_intl.nls` | **예** — `PEB.UseCaseMapping ≠ 0` 일 때 (`RtlpInitUppercaseTables +0x3b`) | `RtlpInitNlsSectionName +0x24 ~ +0x55` · `RtlpInitNlsFileName +0x1b1 ~ +0x1c5` |
| `0xc` (12) 정규화 표 | `\NLS\NlsSectionNORM%.8x` (둘째 인자 16진) | 레지스트리 `…\Control\Nls\Normalization` 값에서 | 아니오 (ntdll 이 이 종류로 안 부름) | `RtlpInitNlsSectionName +0xbb` · `RtlpInitNlsFileName +0x7f ~ +0x158` |
| 그 밖 (`0xd` 포함) | — | — | — | `RtlpInitNlsSectionName` → `0xc00000ef`, `NtGetNlsSectionPtr +0x152 CMP ESI,0xd` / `+0x155 JZ +0x505` (`0xc0000001`), `+0x144 LEA EAX,[RSI-0xb]` / `+0x147 TEST EAX,0xfffffffc` / `+0x14c JNZ +0x505` |

**ntdll 쪽 짝 (읽기만 — `ms/disassemble/ntdll/_calls.tsv`)**: `LdrpInitializeNlsInfo +0x5c`/`+0x61` → `RtlpInitCodePageTables` → `+0x46`/`+0x4b` · `+0x6c`/`+0x71` `NtGetNlsSectionPtr`; `LdrpInitializeNlsInfo +0x6d` (꼬리 JMP) → `RtlpInitUppercaseTables +0x3b`/`+0x40` `NtGetNlsSectionPtr`. 인자 (ntdll 인자 값은 sast/52): 종류 `0xb`·`0xe`, 둘째 = 코드 페이지 번호 / 0, **셋째 (섹션 객체 출력) 늘 NULL**, 넷째 = `GlobalRtlNlsState` 의 `+0x80`·`+0x88`·로컬, **다섯째 (뷰 크기 출력) 늘 NULL**. ✔
- test.exe: ACP = OCP = 949 (`0x3b5`), `UseCaseMapping` = `0x6a5` `[관찰]` → 실행되는 호출 2번 (`0xb` 한 번 + `0xe` 한 번). ✔ (정적으로 갈래, 값은 `[관찰]`.)

## 질문 3-2. 이 커널 함수 안 길 (유저 호출, `PreviousMode != 0`) `[정적]`

| 단계 | 줄 (`NtGetNlsSectionPtr.asm`) | 하는 일 |
|---|---|---|
| 인자 거름 | `+0x6f TEST R9,R9` / `+0x72 JZ +0x4e8` → `+0x4e8 TEST RDI,RDI` / `+0x4f1 → 0xc000000d` (넷째·셋째 둘 다 NULL) · `+0xda TEST RDI,RDI` / `+0xdd JNZ +0x4fb → 0xc00000f1` (유저가 셋째를 주면) · `+0x88 ~ +0xd3` 세 출력 칸 쓰기 확인 ✔ | |
| 이름 만들기 | `+0xfd`/`+0x102` `RtlpInitNlsSectionName` (음수면 `+0x104 JS` 로 끝) ✔ | |
| 열기 | `+0x11d MOV [RSP+0xa0],0x2d0` (속성: 영구 `0x10` · 대소문자 무시 `0x40` · 있으면 열기 `0x80` · 커널 핸들 `0x200`) · `+0x163 MOV EDX,4` (접근 = 읽기 매핑) · **`+0x16d`/`+0x172` `ZwOpenSection`** ✔ | 핸들은 **커널 핸들** — 곧 부모/test.exe **프로세스 핸들 표를 안 건드림** |
| 성공 | `+0x1af`/`+0x1b4` `ObReferenceObjectByHandle`(접근 `0xf001f`, 모드 `R9D=0` 커널, 형식 `MmSectionObjectType`) → `+0x1bb`/`+0x1c0` `ZwClose` ✔ ★ (모드 0) | 섹션 객체 포인터만 `[RSP+0x48]` 에 남김 |
| 실패 → 만들기 | `+0x176 JS +0x370` → `+0x3ac`/`+0x3b1` `RtlpInitNlsFileName` → `+0x41d`/`+0x422` `ZwOpenFile` → `+0x458`/`+0x45d` `ZwCreateSection` (읽기 전용 `2`, SEC_COMMIT, **같은 이름·영구**) → `+0x464`/`+0x469` `ZwClose` → 위 "성공" 으로 합류 (`+0x469 JMP +0x17f`) ✔ | 이 갈래는 **부팅 때** (`ExInitializeNls` 가 `\NLS` 를 막 만든 뒤) 첫 섹션을 만든다. test.exe 때는 이미 있어서 안 탄다 `[추측]` — 동적 D7 |
| 뷰 붙이기 | 아래 3-3 | |
| 뒷정리 | `+0x2f9 TEST RDI,RDI` / `+0x2fc JNZ` — **유저 호출은 셋째가 늘 NULL → `+0x303 ObfDereferenceObject`** (섹션 참조 놓음, call `+0x303`/ret `+0x308`) ✔ | 남는 참조는 뷰 (VAD) 쪽 |
| 출력 | `+0x316 MOV [R14],RAX` (넷째 ← 뷰 주소 `[RSP+0x50]`), `+0x31e MOV [R15],R12` (다섯째 ← 뷰 크기 `[RSP+0x128]`, `R15 != 0` 일 때 `+0x319 TEST/+0x31c JZ`) ✔ | ntdll 은 다섯째가 NULL 이라 크기를 못 받음 |

## 질문 3-3. 뷰를 붙이는 줄 — 주소·크기·보호 `[정적]`

**`NtGetNlsSectionPtr +0x2e6`/`+0x2eb` `MiMapViewOfSection`** (공개 API `MmMapViewOfSection` 이 **아님**). 인자 구조체 `[RSP+0x110]` (0x80 B, `+0x1ff ~ +0x20d memset`) 와 스택 칸:

| 뷰 칸 | 값 | 줄 |
|---|---|---|
| 섹션 | 방금 얻은 섹션 (`[RSP+0x48]`) | `+0x2e1` |
| 프로세스 | **현재 프로세스** (`GS:[0x188]+0xb8`) — 부모 아님, test.exe 의 `ntdll` 이 부르는 중이라 test.exe 자기 것 | `+0x212 ~ +0x22a` |
| **시작 주소 (입출력)** | **입력 0** (`[RSP+0x50]`, `+0x65 MOV [RSP+0x50],RBX(0)` 뒤 안 바뀜) → **커널이 고름**. 유저가 주소를 정할 길 없음 (넷째는 출력 전용: 입력을 안 읽음, `+0xa6/+0xa9` 은 쓰기 가능 확인뿐) | `+0x2d4 LEA R8,[RSP+0x50]` |
| 주소 고르기 | 정렬 `0x10000` (64 KB) · 위쪽 한계 `min(EPROCESS.HighestUserAddress − 1, 0x7ffffffeffff)` (`EPROCESS +0x368`) · 방향 플래그 없음 | `+0x2ab MOV [RSP+0x120],0x10000` · `+0x288 ~ +0x2a3` |
| **크기** | **0 = 섹션 전체** (`+0x27d MOV [RSP+0x128],R13`, `R13=0` ← `+0x17c XOR R13D,R13D`). 돌아올 때 같은 칸에 실제 뷰 크기가 채워짐 (`+0x2f1 MOV R12,[RSP+0x128]`) | `+0x27d` |
| **보호** | **`2` (PAGE_READONLY)** | `+0x25b MOV [RSP+0x13c],2` |
| 할당 종류 | `0x400000` | `+0x250 MOV [RSP+0x138],0x400000` |
| 상속 | `1` | `+0x2bf MOV [RSP+0x28],1` (마지막 칸도 `1`, `+0x2b7` — 뜻 안 정함) |
| 호출자 모드 | `PreviousMode` 를 구조체에 넣음 (`+0x149`) | `+0x232 ~ +0x241` |

- **커널 호출자** (`PreviousMode == 0`, 부팅 때 `ExInitializeNls` 길) 는 위 대신 **시스템 공간 뷰**: `+0x1da JZ +0x46e` → `+0x480 MiSectionControlArea` · `+0x48b MiCheckPurgeAndUpMapCount` · `+0x4be MiInsertInSystemSpace` ✔. (test.exe 는 해당 없음.)

**`MiMapViewOfSection` 안 (공용 부품, 이름·갈래만)** ★: `+0x299 MOV EAX,[R14+0x38]` (컨트롤 영역 `u` = `_MMSECTION_FLAGS`) → `+0x29d BT EAX,0xa` (`PhysicalMemory`, `+0x2a1 JC`) / `+0x2a7 TEST AL,0x20` (`Image`, `+0x2a9 JNZ 0x1408b5a43` = 함수 시작 + `0x413` 조각 → `MiMapViewOfImageSection`; `+0x2a1 JC` = `+0x400` → `+0x409 MiMapViewOfPhysicalSection`) 둘 다 꺼져 있으면 → **`+0x2d9`/`+0x2de` `MiMapViewOfDataSection`**. NLS 섹션은 일반 파일 데이터 섹션 (`ZwCreateSection` SEC_COMMIT 파일 기반) 이라 이 갈래 `[정적 추정 — 섹션 플래그 값은 안 봄]`.

**`MiMapViewOfDataSection` 안 (이름만)** ★ (`ms/decomp/ntoskrnl/MiMapViewOfDataSection.c`, rva `0x907090`): `+0x1b3 MOV EDX,0x88` · `+0x1b8 MOV R8D,0x20646156` (태그 `Vad `) · `+0x1d2`/`+0x1d7` `MiAllocatePool` → **`_MMVAD` (긴 꼴, 0x88 B) 한 개를 풀에서 받는다** (PDB `_MMVAD` 크기 0x88 과 같음). 이어 `+0x2f6`/`+0x2fb` `MiSelectUserAddress` (주소 정함) · `+0x33c` `MiUpdateVadStartVpn` · `+0x3c6` `MiWriteVadFlags2` · `+0x45c`/`+0x461` `MiInsertVad` (VAD 트리에 넣기) · `+0x76d` `MiSetVadFlags`. VAD 칸 값은 본문을 안 읽어 `[공백]` (baggage 3.17 에 칸 단위 줄이 아직 없음 — 다음 요청 후보).

**`\NLS\…` 객체를 부팅 때 누가 만드나 (이름만, sast/52 ✔)**: `Phase1InitializationIoReady +0x27` → `ExInitializeNls` (`+0x28 ZwCreateDirectoryObject` = `\NLS` 디렉터리) → `+0x91` 커널 사본 `RtlpInitCodePageTables` (→ `+0x69`·`+0x91` `NtGetNlsSectionPtr`) · `+0x96` 커널 사본 `RtlpInitUppercaseTables` (→ `+0x2b` `NtGetNlsSectionPtr`) → 위 "실패 → 만들기" 갈래. (`_calls.tsv` 로 call 자리 다시 확인 ✔.)

## 질문 3 요약 — test.exe 가 타는 갈래

ntdll 이 부르는 `0xb` (1~2 번) · `0xe` (1 번) → 이 함수 안에서 `ZwOpenSection` 성공 (`\NLS` 섹션이 부팅 때 이미 있음) → `MiMapViewOfSection` → `MiMapViewOfDataSection` 으로 **읽기 전용 · 섹션 전체 · 64 KB 정렬 · 커널이 주소 선택** 뷰 → 넷째 출력 칸 (`GlobalRtlNlsState +0x80`/`+0x88`, 로컬) 에 뷰 주소. **VAD 한 개 = 호출 한 번** (호출마다 `MiAllocatePool` 로 새 `_MMVAD`). `[관찰]` run44 P1a 에서 `C_949.NLS`·`l_intl.nls` 한 개씩 — 호출 2번 (ACP = OCP, `UseCaseMapping ≠ 0`) 과 맞음. 이후 같은 이름이 한 개씩 더 나오는 것은 ntdll 의 이 두 호출이 아니다 (이 함수에 `MiMapViewOfSection` 부르는 자리는 `+0x2e6` 하나, 호출자는 위 두 사본뿐 `.c` `callers:` 줄) — 다른 호출자 (KERNELBASE 쪽, 이번 범위 밖) `[공백]`.

## 질문 4. 짐 칸 줄 (3장 양식) — baggage 3.17 `_MMVAD` 아래 "노드 단위 줄" 제안

3.17·3.18 은 지금 **칸 단위로 쓴 자리를 아는 칸이 0** 이라 칸 줄을 못 만든다. **노드 단위** (VAD 노드 한 개 = 한 줄) 로 다음 세 줄을 3.17 에 더하는 것을 제안 (2장 숫자는 그대로 — 칸을 아는 칸으로 안 바꿈):

| 칸 | 단계 | 무엇을 (값·모양) | 놓는 법 | 쓴 주인 | 경로 (지도 기준) | 근거 |
|---|---|---|---|---|---|---|
| **VAD 노드 한 개 (노드 단위, 칸 단위 아님)** — NLS 코드 페이지 뷰 (`C_949.NLS` 등) | ② | `_MMVAD` (0x88 B, 풀 태그 `Vad `) 새 노드 — 읽기 전용 파일 뷰 · 섹션 전체 (`[관찰]` 0x31쪽) · 64 KB 정렬 · 주소는 커널이 고름 (`MiSelectUserAddress`) | **syscall** (`NtGetNlsSectionPtr`, ntdll 이 부름) → 커널이 뷰를 붙임 | nt | `②… → LdrpInitializeNlsInfo (호출 +0x1c8) → RtlpInitCodePageTables (호출 +0x5c) → NtGetNlsSectionPtr (호출 +0x46 · +0x6c, syscall 0x102) → nt!NtGetNlsSectionPtr → MiMapViewOfSection (호출 +0x2e6) → MiMapViewOfDataSection (호출 +0x2d9) → MiAllocatePool (호출 +0x1d2) · MiSelectUserAddress (호출 +0x2f6) · MiInsertVad (호출 +0x45c)` | `[정적]` sast112 · 칸 값 `[공백]` · `[관찰]` run44 P1a VAD `25996d20 ~ 25996d50` (`04_baggage-read_P1a.txt:1539`) |
| **VAD 노드 한 개** — NLS 대문자 표 뷰 (`l_intl.nls`) | ② | 같은 모양, 뷰 3쪽 | **syscall** (`NtGetNlsSectionPtr`) | nt | `②… → LdrpInitializeNlsInfo (호출 +0x1c8) ⇒ RtlpInitUppercaseTables (꼬리 +0x6d) → NtGetNlsSectionPtr (호출 +0x3b) → … (위와 같은 안쪽)` | `[정적]` sast112 · `[관찰]` P1a `25996d60 ~ 25996d62` (`:1540`) |
| **VAD 노드 한 개** — 둘째 `C_949.NLS`·`l_intl.nls` (P1b 이후) | ② | 주인 `[공백]` — 위 두 호출은 아님 (VAD 한 개 = 호출 한 번) | `[공백]` | `[공백]` | `[공백]` | `[관찰]` run44 P1b·P2 · 7.18 (1) |

- 부르는 쪽 = **ntdll** (`LdrpInitializeNlsInfo` → `RtlpInitCodePageTables` / `RtlpInitUppercaseTables`), 쓴 주인 = **nt**, 놓는 법 = **syscall** (`NtGetNlsSectionPtr`, 번호 `0x102`, 인자 5). ✔ 요청서 문장과 같다.
- baggage 7.6.3 의 NLS 줄 ("코드 페이지 표 뷰 `C_949.NLS` (유저 구역)" · "대문자 표 뷰 `l_intl.nls`") 은 이미 맞다. 더할 것: **경로 끝 `→ MiMapViewOfDataSection (호출 +0x2d9) → MiAllocatePool (호출 +0x1d2)`** 와 "핸들을 프로세스 표에 안 만듦 (커널 핸들 열기·닫기)" 한 줄, "호출 +0x2e6" 로 통일 (sast/52 오타 정정).

## Q89 — 지금 우리 (PE_emulator) 맞나·틀리나

(읽기만: `ext/syscall/stub-work/decisions/opt_startup_syscalls_model.md` "PE_emulator 브랜치 덧붙임 (152)" 표 · `Test/LdrInitTest.cpp`.)

- **종류별 이름·파일 (`0xb`·`0xe`·`0xc`·그 밖 `0xc00000ef`) — 맞음.** `0xe` = 대문자 표 (12 아님).
- **뷰 줄 `+0x2e6` · 크기 0 `+0x27d` · 보호 2 `+0x25b` · 64 KB `+0x2ab` · 넷째 `+0x316` · 다섯째 `+0x31e` — 맞음** (이번 `.asm` 로 다시 확인).
- **핸들을 안 만든다 — 맞음** (진짜도 커널 핸들을 열고 `+0x1bb ZwClose`, 프로세스 핸들 표는 그대로).
- **부를 때마다 새 뷰 — 맞음** (호출마다 `MiAllocatePool` 로 새 VAD).
- **"뷰 크기 = 쪽 올림" 가정 (TODO(VM)) — 확인 필요.** ntdll 은 다섯째를 NULL 로 불러서 이 칸 값이 test.exe 의 ntdll 길에선 안 쓰인다. `[관찰]` 뷰 길이 0x31쪽·3쪽은 쪽 올림과 모순 없지만 파일 크기를 못 봐서 판정 못 함 (동적 D6).
- **주소 선택 (`next_base_` 규칙)**: 진짜는 64 KB 정렬 · 위쪽 한계 `HighestUserAddress−1` 안에서 **커널이 빈 곳을 고름** (`MiSelectUserAddress`). 에뮬레이터가 부르는 순서·간격이 진짜와 같은지는 `확인 필요` (`[관찰]` ApiSet 뷰 `25996cd0` 다음 `25996d20` 이 C_949).
- `kernelbase NlsProcessInitialize` 의 `NtGetNlsSectionPtr` 호출 (에뮬레이터가 이미 덫을 둠, `LdrInitTest.cpp:7283`) 은 이번 정적 범위 밖 — 진짜 쪽 확인은 새 요청 필요.

---

# 동적으로 확인할 것 (dast 요청 필요)

멈춤점은 **프로세스 조건** 필요 (부모 `cmd.exe` 가 `NtCreateUserProcess` 를 부를 때, 이어서 test.exe).

- **D1. 부모 `PS_CREATE_INFO` 가 실제로 어떻게 채워지나.** `nt!PspUpdateCreateInfo` 입구 (`ecx == 6` 일 때): `poi(rdx+0x30)` 에서 0x58 바이트 (`dq`) — 입구 값과 `NtCreateUserProcess+0x1129` (ret) 직후 값 둘을 찍어 비교. 같이: `rdx+0x8` (문맥 속성 비트 — 비트 5 = 이름 속성), `rdx+0x10` (비트 1 `WriteOutputOnExit`, 비트 4 `DetectManifest`, 비트 5 `ManifestDetected`), 부모가 준 입구 `PS_CREATE_INFO +0x10` (`InitFlags`). → "파일·섹션 핸들 사본이 정말 생기나", "OutputFlags 값" 확정.
- **D2. 부모 핸들 표의 두 핸들 값·접근.** `nt!PspCreateObjectHandle` 입구와 출구 (`PspInsertThread+0x525` · `NtCreateUserProcess+0x10a7`): `rdx+0x10/+0x14/+0x18` (`RemainingDesiredAccess` · `PreviouslyGrantedAccess` · `OriginalDesiredAccess`), `rdx+0x180/+0x184`, 출구 `rdx+0x188` (핸들 값). 그 값으로 부모 cmd 문맥에서 `!handle <값> f` (`GrantedAccess`·속성) — 스레드가 프로세스보다 먼저 만들어지는지(핸들 번호), 접근 값, 속성이 `0x1df2` 걸러진 값인지.
- **D3. `nt!PspPropagateHandle` 입구**: `cl` (모드 1), `[rdx]` (원본 핸들 값 — 음수 비트 = 커널 핸들인지), 끝 `[r8]` (부모 새 핸들). 원본 커널 핸들을 누가 닫는지 (`PspDeleteCreateProcessContext +0x11d0` 로 짐작).
- **D4. `PsTestProtectedProcessIncompatibility` 반환 (`NtCreateUserProcess+0xe94` 직후 `al`)** = test.exe 때 0 인지 (0 이면 부모 인자 그대로 `ProcessDesiredAccess`·`ThreadDesiredAccess`). 같이 입구 `r8d`·`r9d` (부모가 `MAXIMUM_ALLOWED` 를 쓰는지).
- **D5. 부모 `ProcessHandle`·`ThreadHandle` 칸이 정말 그 자리에 쓰이는지**: `NtCreateUserProcess+0x10f6` / `PspInsertThread+0x573` 직후 두 칸 값 (같은 값이 `PspCreateObjectHandle` 출구 `rdx+0x188` 와 같은지).
- **D6. `NtGetNlsSectionPtr+0x2eb`** (`MiMapViewOfSection` 돌아온 뒤): `[rsp+0x50]` 뷰 주소 · `[rsp+0x128]` 뷰 크기 (= 쪽 올림인지; PE_emulator TODO), 콜스택 (`RtlpInitCodePageTables+0x4b` · `RtlpInitUppercaseTables+0x40` 외 다른 호출자가 있는지). sast/52 동적 1 과 한 번에.
- **D7. `NtGetNlsSectionPtr+0x176 JS` (열기 실패) 가 test.exe 때 도는지** (정적으로는 안 돎, `[추측]`) — sast/52 동적 1 과 같음.

추천 멈춤점 (요약): `nt!PspUpdateCreateInfo` (ecx==6) · `nt!PspCreateObjectHandle` · `nt!PspPropagateHandle` · `nt!NtGetNlsSectionPtr` (+ `+0x2eb`).

# 다음 요청 후보 (정적)

1. **`PspAllocateThread` 의 속성 출력 쓰기** — `PS_ATTRIBUTE` `ClientId` (문맥 `+0x18`) · `TebAddress` (`+0x20`) 출력 칸을 쓰는 줄·값 출처 (이번 7.6.13 표 마지막 줄 `[공백]`). 작고 안전.
2. **`PspDeleteCreateProcessContext` (`NtCreateUserProcess +0x11d0`)** — 문맥의 커널 핸들 (파일 `+0xb0` · 섹션 `+0xc0` · IFEO `+0xc8`) 을 닫는지·언제 닫는지 (D3 와 짝).
3. **`MiMapViewOfDataSection` 본문 칸 단위** — VAD `_MMVAD` 칸 (`StartingVpn`·`EndingVpn`·`u.VadFlags`·보호·`Subsection` 등) 쓰는 줄 → baggage 3.17 (지금 칸 단위 0줄) 채우기. NLS·ApiSet·ntdll·exe 뷰 모두에 공용이라 이득이 큼.
4. **KERNELBASE `NlsProcessInitialize` 의 `NtGetNlsSectionPtr` 부르는 자리** — 둘째 `C_949.NLS`·`l_intl.nls` 뷰 (7.18 (1)) 의 주인. PE_emulator 가 이미 덫을 둔 자리와 같은지.
5. (낮음) `PspInsertProcess`/`PspInsertThread` 의 접근 상태 주체 (현재 프로세스 대 새 프로세스 토큰) — 핸들 접근 값 D2 로 갈리면.

# 판정

**분석성공.** Q88 (질문 1·2) 와 Q89 (질문 3·4) 모두 줄 근거와 함께 답했다. 정적으로 못 정한 것: (a) 부모가 이름 속성·`WriteOutputOnExit` 를 켜는지 (KERNELBASE 쪽, D1), (b) 핸들의 최종 접근 숫자·핸들 번호 (D2), (c) 보호 프로세스 호환 검사 값 (D4), (d) 둘째 `C_949.NLS`·`l_intl.nls` 뷰 주인, (e) `PS_ATTRIBUTE` `ClientId`·`TebAddress` 쓰는 줄. 동적분석 요청 필요: D1 ~ D7.

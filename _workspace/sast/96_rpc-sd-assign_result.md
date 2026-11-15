# 96 rpc-sd-assign — 정적 분석 결과

- 요청서: `_workspace/sast/96_rpc-sd-assign.md` (대기열 Q180d)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), PDB `ntkrnlmp.pdb` guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- analyze: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음). 읽기 전용 export 와 refs 만 돌림 (export 한 번에 ~8초, 메모리 걱정 없었음: `free` available 14 GB 유지).
- 뽑은 함수 (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`):
  - depth 1: `^(SeAssignSecurityEx2|SeAssignSecurityEx|SeAssignSecurity|RtlpNewSecurityObject)$` → 31개 (callee 포함)
  - depth 0 (요청서 대상): `RtlpInheritAcl2` `ObpCreateDirectoryObject` `NtCreateDirectoryObject` `NtCreateDirectoryObjectEx` `ObpAssignSecurity` `ObInsertObjectEx` `SeDefaultObjectMethod` `ObInitSystem` → 8개. (Directory 형 초기화 줄은 `ObInitSystem +0x2a0 ~ +0x2e4`; 이름은 `ObpCreateDirectoryObject`·`ObInitSystem`)
  - **범위를 넓힌 곳** (이유 포함, 전부 depth 0):
    - `ObpCreateHandle` `ObpInsertOrLocateNamedObject` `ObAssignSecurity` `SeComputeAutoInheritByObjectTypeEx`: 질문 1 — 이름 있는 객체는 `ObInsertObjectEx` 가 `ObpAssignSecurity` 를 **직접 안 부르고** 이 둘을 거쳐서 (요청서 사슬과 다름, 아래 질문 1).
    - `RtlpCopyAces` `RtlpGenerateInheritedAce` `RtlpCopyEffectiveAce` (+ `ObpGetObjectSecurity` `ObLogSecurityDescriptor` `RtlMapGenericMask` 도 같이 뽑힘): 질문 3 — generic 바꾸기가 `RtlpInheritAcl2` 안이 아니라 `RtlpCopyAces` 에 있음 ("generic 바꾸기 함수는 줄기 안" 규칙).
    - `ObpCaptureObjectCreateInformation` `SeCaptureSecurityDescriptor`: 넘긴 SD 가 `RtlpNewSecurityObject` 에 닿기 전에 어떻게 잡히나 (짧게).
  - `refs`: `ObpObjectSecurityInheritance` (`ms/disassemble/ntoskrnl/_refs/ObpObjectSecurityInheritance.tsv`)
  - 값 읽기: `ms/bin/.../ntoskrnl.exe` 파일에서 `ObpDirectoryMapping` (rva `0xc7fe70`) · `SepTokenMapping` (`0xb496d0`) 16 바이트, 레지스트리 표 문자열 (`-I` python, 읽기만).
  - 구조체: `pdb_struct.py ntoskrnl.exe _TOKEN _OBJECT_TYPE _OBJECT_TYPE_INITIALIZER` (PDB 로 `+0xd8 SeMandatoryLabelMask`, `+0xdc SeTrustConstraintMask`, `TypeInfo +0x3 bit1 SeTrustConstraintMaskPresent`, 토큰 `+0x90 DefaultOwnerIndex` `+0x98 UserAndGroups` `+0xa8 PrimaryGroup` `+0xb8 DefaultDacl` `+0xd0 IntegrityLevelIndex` 확인)
- 버전 확인: **일치.** `_calls.tsv` 에 `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 그대로.
- 스크립트 문제: 없음.
- 쓰기 범위: ntoskrnl 쪽 파일만. rpcrt4 (sast/95) 쪽 파일·Ghidra 프로젝트·`ntdll` 프로젝트는 안 건드림 (돌고 있던 `ntdll.export` 는 다른 프로젝트).
- 표기: `+0x..` 는 함수 시작 기준. 부르기는 `call/ret`, 점프 목적지는 "목적지 − 함수 시작".

## 한눈에 (먼저 읽기)

| 물음 | 짧은 답 |
|---|---|
| 사슬 | `NtCreateDirectoryObject → ObpCreateDirectoryObject → ObInsertObjectEx → ObpCreateHandle → ObpInsertOrLocateNamedObject → ObpAssignSecurity → SeAssignSecurityEx2 → RtlpNewSecurityObject`, 돌아와서 `SeDefaultObjectMethod`(EDX=3) 가 SD 를 캐시에 넣고 헤더에 건다. 요청서의 `ObInsertObjectEx → ObpAssignSecurity` 직행은 **아님** (그 직행 `SeAssignSecurityEx2` 는 이름 없는 객체 = 토큰 등 길). |
| 소유자·그룹 | SD 에 없으면 **만든 쪽 토큰** 에서: 소유자 = `Token.UserAndGroups[DefaultOwnerIndex].Sid` (`+0x98`, `+0x90`), 그룹 = `Token.PrimaryGroup` (`+0xa8`). 값은 VM (`[추측]` SYSTEM 토큰이면 `S-1-5-32-544` / `S-1-5-18`). |
| DACL | 명시 DACL 이 있고 자동 상속 꺼짐 (Ob 가 켜는 조건 없음) → **부모 ACE 안 섞임**, ACE 순서·개수 그대로. generic 은 **INHERIT_ONLY(0x08) 가 아닌 ACE 만** Directory `GenericMapping` `{0x20003, 0x2000c, 0x20003, 0xf000f}` 로 바꾸고 `& 0xf000f`. 상속 전용 ACE 는 지우지 않고 **마스크도 그대로**. `AclSize` 는 다시 계산: `0xb8 → 0x9c` (8 + ACE 크기 합 148). |
| SACL / 라벨 | 라벨 ACE (`0x11`, 마스크 1, `S-1-16-4096`, 플래그 0) 는 **바이트 그대로**. `AclSize 0x20 → 0x1c`. 라벨만 있는 SACL 은 `SeSecurityPrivilege` 안 묻는다. 대신 **`Control` 에 `SE_SACL_AUTO_INHERITED 0x800` 이 켜진다** (라벨 갈래가 자동 상속 모드로 도니까). |
| `Control` | `0x8814` = `SELF_RELATIVE` · `SACL_AUTO_INHERITED` · `SACL_PRESENT` · `DACL_PRESENT`. `OWNER/GROUP_DEFAULTED` 안 켜짐. |
| 저장 모양 | self-relative, 순서 **머리 `0x14` → SACL → DACL → 소유자 → 그룹** (묻기 출력 순서와 다름!). 오프셋 칸: Sacl `0x14`, Dacl `0x30`, Owner `0xcc`, Group `0xdc` (소유자 16 B 가정). 총 `0xe8` 바이트 (소유자 16 B + 그룹 12 B 가정). |
| 토큰 객체 | **라벨 ACE `0x11` 이 붙는다** (조건 없이): 토큰 형의 `SeMandatoryLabelMask = 1` (`SepTokenInitialization`) → 마스크 `1` (NO_WRITE_UP), SID = **만든 쪽 토큰의 무결성 SID**. DACL = 만든 쪽 토큰의 `DefaultDacl` 을 Token `GenericMapping` 으로 바꾼 것. |

## 질문별 답 [정적]

### 질문 1. 사슬

**답**: 이름 있는 객체 (`\RPC Control`) 는 아래 사슬. 요청서의 가정과 다른 점은 ★.

| 단계 | call / ret | 근거 |
|---|---|---|
| `NtCreateDirectoryObject` → `ObpCreateDirectoryObject` | `+0xc/+0x11` (22 B 껍데기. `NtCreateDirectoryObjectEx` 도 `+0xc/+0x11`) | `_calls.tsv` |
| `ObpCreateDirectoryObject` → `ObCreateObjectEx` | `+0xe0/+0xe5` | 이 안에서 `ObCreateObjectEx +0xde/+0xe3 ObpCaptureObjectCreateInformation` → `+0x260/+0x265 SeCaptureSecurityDescriptor` 로 SD 를 잡음 (아래 "잡기") |
| `ObpCreateDirectoryObject` → `ObInsertObjectEx` | `+0x18a/+0x18f` (AccessState `RDX`=0 이라 `ObInsertObjectEx` 가 스스로 만듦) | `ObpCreateDirectoryObject.asm` `+0x185 XOR EDX,EDX`, `+0x18a CALL` |
| ★ `ObInsertObjectEx` → `ObpCreateHandle` | `+0x443/+0x448` (이름 없고 SD 도 필요 없을 때만 빠른 길 `+0x156/+0x15b`) | 형 `+0x42 & 8` (SecurityRequired) 가 켜져 있고 (`+0xde TEST byte [RSI+0x42],8` / `+0xef JNZ +0x1c0`) 이름이 있으면 (`+0xd1 LEA R12,...` 이름 정보, `+0x279 TEST R12,R12` / `+0x27c JNZ +0x3ea`) **SD 계산 블록 (`+0x28c ~ +0x3e6`) 을 건너뜀**. 이 블록 안에 `+0x325/+0x32a SeAssignSecurityEx2` 가 있으니 = 이름 없는 객체 길. |
| ★ `ObpCreateHandle` → `ObpInsertOrLocateNamedObject` | `+0x13bd/+0x13c2` (진입 `+0x526 JNZ → +0x139a`; `R13` 의 뜻은 안 풀었음) | `ObpCreateHandle.asm` |
| ★ `ObpInsertOrLocateNamedObject` → `ObpAssignSecurity` | `+0x1fe/+0x203`. 앞서 `+0x117/+0x11c ObpLookupObjectName`, 부모 SD 는 `+0x175/+0x17a ObpGetObjectSecurity` | `ObpInsertOrLocateNamedObject.asm` |
| `ObpAssignSecurity` → `SeAssignSecurityEx2` | `+0xd9/+0xde` (`+0x1ce/+0x1d3` 의 두 번째 호출은 6번째 인자 ≠ 0 일 때 (`+0x87 CMP byte [RSP+0xc8],R13B; +0x8f JNZ → +0x19e`) 먼저 도는 보조 호출 — 명시 SD 가 없는데 자동 상속 플래그가 있을 때 `ObpInsertOrLocateNamedObject +0x1c4 JZ → +0x412 ~ +0x41d` 가 1 로 만듦; 우리는 0) | `ObpAssignSecurity.asm` |
| `SeAssignSecurityEx2` → `RtlpNewSecurityObject` | `+0x65/+0x6a` (먼저 `+0x19 CMP [RAX],8 / +0x1c JNZ → 0xc000000d`: 확장 인자 구조체 첫 칸이 8 이어야 함) | `SeAssignSecurityEx2.asm` |
| `ObpAssignSecurity` → `SeDefaultObjectMethod` | `+0x14a/+0x14f` (EDX=3). 형 `+0x98` 이 `SeDefaultObjectMethod` 인지 `+0x118 MOV RAX,[RBX+0x98]` / `+0x123 CMP RAX,RDX` / `+0x144 JNZ → +0x1e3` (CFG 간접) | 같은 파일 |

- **`SeAssignSecurityEx` 와 `SeAssignSecurity` 는 껍데기**: `SeAssignSecurityEx +0x3a/+0x3f → SeAssignSecurityEx2` (인자 하나 밀어 넣음), `SeAssignSecurity` 는 `RtlpNewSecurityObject` 로 넘김 (`+0x44/+0x49`; 부르는 쪽은 레지스트리 `CmpCreateChild` 따위). 우리 사슬에선 안 쓰임.
- `SeAssignSecurityEx2` 인자 (asm 으로 푼 것): 부모 SD, 명시 SD, 출력 SD 포인터, 객체 형 GUID 포인터 (우리는 NULL), `IsDirectoryObject`, `AutoInheritFlags`, 확장 인자 (첫 칸 8), `SubjectContext`, `GenericMapping`. `ObpAssignSecurity` 가 넘기는 값: `SubjectContext = AccessState+0x20` (`+0x83 LEA R14,[RSI+0x20]`), `GenericMapping = 형 +0x4c` (`+0x61 LEA RBP,[RBX+0x4c]`), `IsDirectoryObject = (형 == ObpDirectoryObjectType)` (`+0x95 CMP RBX,[ObpDirectoryObjectType]` / `+0x9c JZ → +0x1dc MOV AL,1`).
- **부모 SD**: `ObpInsertOrLocateNamedObject` 가 부모 디렉터리 (이름 정보 `[R14]`) 가 있으면 가져온다: `+0x145 TEST RCX,RCX`, `+0x14a MOV EAX,[RCX+0x150]` / `+0x150 TEST AL,8` / `+0x152 JNZ → +0x44f` (디렉터리 플래그 8 = NtCreateDirectoryObjectEx 의 `Flags & 1` 로 켜짐, `ObpCreateDirectoryObject +0x145 OR EDX,8`), `+0x158 CMP [ObpObjectSecurityInheritance],EDI` / `+0x15e JNZ → +0x44f` (`+0x44f MOV [RBP-0x78],3` = 자동 상속 켬). **둘 다 아니면** 플래그 0. 부모 = 루트 `\` (`ObInitSystem +0x557/+0x55c` 가 `NtCreateDirectoryObject` 로 만듦, SD 는 `ObpAuditBaseDirectories`·`ObpAuditBaseObjects` 가 0 이면 `SePublicDefaultUnrestrictedSd` — `+0x427 ~ +0x43e JZ → +0x521`).
- `nt!ObpObjectSecurityInheritance` (rva `0xe66e08`): `.data` 초기값 0 (파일에 없음 = 0). 읽는 곳은 `ObpInsertOrLocateNamedObject +0x158`, `+0x1a6` 둘뿐. 값을 채우는 건 **레지스트리** `HKLM\...\Session Manager\Kernel` 의 `ObObjectSecurityInheritance` (INIT 구역 표 rva `0xc86fe0` 안 문자열 `Session Manager\Kernel` / `ObObjectSecurityInheritance` → 대상 `0x140e66e08`). 기본 설치엔 없다고 보면 0 `[추측: 값은 VM]`.
- **자동 상속 플래그 (AutoInheritFlags)** 는 `ObpAssignSecurity +0x6d ~ +0x74` 에서 `EDI = SeComputeAutoInheritByObjectTypeEx 출력 | 호출자 인자 (0 또는 3)`.
  - `SeComputeAutoInheritByObjectTypeEx` (`+0x3f MOV EDX,[RCX+0xd8]` = 형 `SeMandatoryLabelMask`): Directory 형은 **0** (초기화 구조체 길이가 `0x78` 이라 `+0xd8` 이 안 채워짐: `ObInitSystem +0x219 MOV EDI,0x78` / `+0x22f MOV [RBP],DI`; `ObCreateObjectTypeEx +0x45c ~ +0x46d` 는 길이 ≥ `0x80` 일 때만 `+0xd8`·`+0xdc` 를 채움). 명시 SD 에 DACL·SACL 이 다 있으면 (`+0xfd TEST byte [RCX],4`, `+0x107 TEST byte [RCX],0x10 / JNZ → +0x87`) 자동 상속 비트 (1·2) 도 안 켜짐 → **0**.
  - 그래서 우리 경우 `AutoInheritFlags = 0`.
- **Assign 갈래 (`SeDefaultObjectMethod` EDX=3)**: 분기 `+0x8c TEST EDX,EDX` (0 쓰기) / `+0x94 SUB EDX,1; +0x97 JZ → +0x7d9` (1 묻기) / `+0x1c CMP EDX,2` (삭제) / `+0x9d CMP EDX,2; +0xa0 JNZ → +0xa00 KeBugCheckEx` → 나머지 3 = `+0xa6` 부터. 하는 일 (`ObLogSecurityDescriptor` 를 **안 거치고 인라인**):
  1. 크기 ESI = `0x14` + 소유자 `(0xb+4n)&~3` + 그룹 같은 식 + DACL `(AclSize+3)&~3` + SACL 같은 식 (`+0xaf ~ +0x161`, self-relative 는 `+0xb8 TEST CX,CX; JNS` 가 안 갈리는 쪽).
  2. SD 바이트를 `xor/rol` 로 해시 (`+0x172 ~ +0x18e`), 캐시 `ObsSecurityDescriptorCache` (`+0x199 LEA RAX,[0x140fd0ec0]`, 해시 하위 1바이트로 통 고름) 에서 같은 해시·같은 크기·`memcmp` 같은 항목을 찾음 (`+0x213 ~ +0x33e`).
  3. 있으면 참조만 늘림 (`+0x340 XADD [RDI+8],R12`), 없으면 `ExAllocatePool2` (`+0x277`, 태그 `ObSc` = `0x6353624f`) 로 `항목 헤더 0x20 + SD 복사본` 을 만듦 (`+0x29f RtlCopyMemory`: SD 바이트 그대로 ESI 개).
  4. `RtlpNewSecurityObject` 가 만든 임시 SD 를 `ExFreePoolWithTag` (`+0x395`), 객체 헤더 `[객체-8]` 에 `항목+0x20 | 0xf` (EX_FAST_REF) 를 씀 (`+0x3a3 LEA RAX,[R13+0xf]` / `+0x3a7 MOV [R15-8],RAX`).
  - 결론: **저장된 SD = `RtlpNewSecurityObject` 출력 바이트 그대로** (사본). 같은 바이트의 SD (예: `\RPC Control` 과 `\Windows` 의 처음 SD) 는 캐시 항목 하나를 같이 씀.
- **잡기** (`SeCaptureSecurityDescriptor`, 이름만): 넘긴 SD 는 항상 **self-relative 사본** 이 된다 (`.c` 에 `*(ushort *)(사본+2) |= 0x8000`, 순서 머리 `0x14` → SACL → DACL → 소유자 → 그룹, ACL 의 `AclSize` 는 4 맞춤 올림 — smss 값 `0xb8`·`0x20` 은 이미 4 의 배수라 그대로). `Control` 의 나머지 비트는 그대로 (smss 쪽 `0x14` → 사본 `0x8014`). `RtlpNewSecurityObject` 는 절대/상대 둘 다 읽으므로 입력 모양은 결과에 영향 없음.

**지금 우리 (PE_emulator)**: 객체 만들 때 SD 를 붙이는 길이 아예 없음 (`namespace.cpp:21` 에서 `\RPC Control` 에 SD 안 채움, 요청서). **틀림 → 모자람**: "새 객체 SD 계산" 함수 하나 (부모 SD·명시 SD·만든 토큰·`GenericMapping`·`IsDirectory`) 를 두고 객체 만들 때 부르면 됨. 형별 갈래는 필요 없음 (Directory·Token 둘 다 같은 `SeDefaultObjectMethod`).

### 질문 2. 소유자·그룹

**답**: smss SD 는 소유자·그룹이 없으므로 (`Control` 에 `0x1`·`0x2` 도 없음) 커널이 **만든 쪽 토큰** 에서 채운다.

- 어느 토큰: `SubjectContext` (`RDI`) 에서 **ClientToken (`[RDI]`, impersonation 중일 때) 이 있으면 그것, 없으면 PrimaryToken (`[RDI+0x10]`)** (`+0x1c5 MOV R8,[RDI]` / `+0x1ce JNZ → +0x2136` (ClientToken 있는 갈래; 거기서 `+0x2136 CMP [RDI+8],R12D` 로 `ImpersonationLevel == 1` 이면 서버 플래그를 켬) / `+0x1d4 MOV R8,[RDI+0x10]`). smss 는 impersonation 안 하므로 smss 프로세스 토큰 (SYSTEM) `[추측]`.
- 칸 (토큰 안): `+0x1dc MOV RAX,[R8+0x98]` (`UserAndGroups`), `+0x1e3 MOV ECX,[R8+0x90]` (`DefaultOwnerIndex`), `+0x1f1 ADD RCX,RCX` / `+0x202 MOV R13,[RAX+RCX*8]` (= `UserAndGroups[DefaultOwnerIndex].Sid`, 16 B 칸 × index), 그룹 `+0x1ea MOV RBX,[R8+0xa8]` (`PrimaryGroup`), 기본 DACL `+0x1f4 MOV RSI,[R8+0xb8]`, 무결성 SID `+0x21d CALL SepLocateTokenIntegrity` (토큰 `+0xd0` 번째 `UserAndGroups` 칸, 없으면 `SepDefaultMandatorySid`).
  - 이 SID 들은 `ExAllocatePool2(0x100, …, 'SeSd')` (`+0x2ad`) 한 덩어리에 복사 (소유자 `+0x2d2`, 무결성 `+0x2ff`, 그룹 `+0x349`, "서버용" 소유자 `+0x374`·그룹 `+0x3ae`, 기본 DACL `+0x3d7`).
- 고르기: 명시 소유자 없음 (사본은 항상 self-relative: `+0x430 MOVZX EDX,[R11+2]; +0x438 JS → +0x44d MOV EAX,[R11+4]` 오프셋 0 → 없음; 절대 모양이면 `+0x43a MOV RAX,[R11+8]`) → `+0x459 TEST CL,0x20` (`SEF_DEFAULT_OWNER_FROM_PARENT`) 이 **꺼져 있으면** (Ob 는 0x20·0x40 을 안 줌) → `+0x462 TEST R8B,R8B; +0x465 MOV RAX,R9; +0x468 CMOVNZ RAX,R15` = **토큰 소유자 사본**, 서버 보안 플래그 (명시 SD `Control & 0x80`, 또는 `ImpersonationLevel==1`) 가 켜지면 **PrimaryToken 쪽 소유자**. 그룹도 같은 식 (`+0x48f TEST CL,0x40`, `+0x498 MOV RBX,R14`, `+0x49e CMOVNZ RBX,R13` = `PrimaryGroup` 사본). 못 찾으면 소유자 `0xC000007C`(`+0xc02/+0xc06`), 그룹 `0xC000005B` (`+0xcd8/+0xcdc`).
- 명시 소유자가 있으면 `SepValidOwnerSubjectContext` 로 검사 (`+0x1988`; Ob 는 `SEF_AVOID_OWNER_CHECK 0x10` 도 `param_5 & 1` 일 때만 — 이름 있는 객체 길엔 안 줌) → smss 는 소유자 안 줘서 해당 없음.
- 결과 SID: 소유자 = 토큰 `DefaultOwnerIndex` 가 가리키는 그룹 SID, 그룹 = 토큰 `PrimaryGroup`. **어느 SID 인지는 VM** (`[추측]` SYSTEM 토큰: 소유자 `S-1-5-32-544` Administrators, 그룹 `S-1-5-18` SYSTEM — 파일·키에서 "SYSTEM 이 만든 것의 소유자는 Administrators" 로 흔히 보이는 모양). 질문 7 바이트 표엔 이렇게 가정하고 `[VM]` 로 표시.
- `SE_OWNER_DEFAULTED`·`SE_GROUP_DEFAULTED` 는 **안 켜진다** (결과 `Control` 을 만드는 줄은 `+0x58e ~ +0x5ac`, `+0xfc8 ~ +0xfdf`, `+0x1102 ~ +0x1119` 뿐이고 `0x1`·`0x2` 를 켜는 줄 없음).

**지금 우리**: 객체 SD 구조에 소유자·그룹 칸은 있다 (요청서 `handles.hpp:14`) → 칸은 맞음. **채우는 규칙이 없음** → 만든 쪽 토큰의 `DefaultOwner` / `PrimaryGroup` 으로 채우기 (smss 의 SYSTEM 토큰이면 위 추측 값). 모자람.

### 질문 3. DACL 가공 (명시 SD 를 줬을 때)

DACL 은 `RtlpInheritAcl2` (모드 1, `RtlpNewSecurityObject +0x1668/+0x166d`) 가 만든다. 호출 인자 중 자동 상속 = `AutoInheritFlags & 1` = **0** (`+0x1054 ~ +0x105e`: `R15D = R9D & 1`, `+0x1663 MOV [RSP+0x20],R15B`).

**3-가. generic → 구체 권한**: **한다. 어느 ACE 에: INHERIT_ONLY(0x08) 가 아닌 ACE 만, 형이 `0..0xa`·`0xd`·`0xe` (허용·거부·감사·경보·콜백 따위) 인 것만.**
- 근거 `RtlpCopyAces.asm` (자동 상속 꺼짐 = `param_5 == 0` 의 일반 복사 갈래: `+0x101 CMP byte [RBP+0x50],0; +0x105 JNZ → +0x25f` 이 안 갈림): `+0x13e CALL RtlCopyMemory` (ACE 그대로 복사) → `+0x146 CMP AL,0xb; +0x148 JNC → +0x4df` (형 ≥ 0xb 는 `0xd`·`0xe` 만 변환 갈래로 되돌아옴) → **`+0x14e TEST byte [RBX+1],8; +0x152 JNZ → +0x1a8`** (상속 전용이면 변환 건너뜀) → `+0x154 MOV EAX,[RBX+4]` (마스크) → `+0x15b TEST EAX,EAX; JNS` (비트 31, GENERIC_READ `0x80000000` 이면 `+0x15f OR EAX,[map+0]`), `+0x161 BT EAX,0x1e` (GENERIC_WRITE → `+0x167 OR … [map+4]`), `+0x16a BT EAX,0x1d` (GENERIC_EXECUTE → `[map+8]`), `+0x173 BT EAX,0x1c` (GENERIC_ALL → `[map+0xc]`), `+0x17c AND EAX,0xfffffff` (generic 4 비트 지움), 이어서 `+0x190 MOV R8D,0x673; BT R8D,ECX` (허용·거부 형 0·1·4·5·6·9·10) 이면 `+0x1a0 MOV ECX,[map+0xc]; +0x1a3 AND ECX,EAX` = **`& GenericAll`** (`+0x1a5` 저장); 그 밖의 형 (감사·경보 등) 은 `+0x19a JNC → +0x4fa MOV ECX,[RDX+0xc]; +0x4fd BTS ECX,0x18` 로 `GenericAll | 0x1000000` 와 & (`ACCESS_SYSTEM_SECURITY` 허용).
- `RtlpNewSecurityObject` 가 마지막에 SD 로 복사할 때도 같은 변환 루프가 한 번 더 있지만 (`+0x13e2 ~ +0x1480`, SACL `+0x12c4 ~ +0x1335`), **`RtlpInheritAcl2` 가 새 ACL 을 만들어 줬으면 (`[RBP-0x80] != 0`) 건너뜀** (`+0x13b3 TEST R13B,R13B; JNZ → +0x945`). 우리 DACL 은 `RtlpInheritAcl2` 가 만들었으니 변환 한 번.
- Directory `GenericMapping` (**숫자 넷**): `ObpDirectoryMapping` (rva `0xc7fe70`, INIT 구역) = **`{GenericRead 0x20003, GenericWrite 0x2000c, GenericExecute 0x20003, GenericAll 0xf000f}`**. `ObInitSystem +0x2a0 MOVUPS XMM0,[0x140c7fe70]` → `+0x2df MOVDQU [RBP+0xc],XMM0` (초기화 구조체 `+0xc`) → `ObCreateObjectTypeEx` 가 형 객체 `+0x4c` 로 복사 (`+0x42e LEA RCX,[RBX+0x40]`, `+0x44b/+0x452` 복사) — `ObpAssignSecurity +0x61 LEA RBP,[RBX+0x4c]` 로 읽음. (같은 방식으로 토큰: `SepTokenMapping` rva `0xb496d0` = `{0x2001a, 0x201e0, 0x20005, 0xf01ff}`.)
- ACE 별 결과 (smss Liberal DACL):

| # | SID | 플래그 | 넘긴 마스크 | 상속 전용? | 저장되는 마스크 |
|---|---|---|---|---|---|
| 0 | `S-1-1-0` | 0 | `0xE0000000` | 아니오 | `0x2000f` (`0x20003 \| 0x2000c \| 0x20003`, `& 0xf000f`) |
| 1 | `S-1-5-12` | 0 | `0xE0000000` | 아니오 | `0x2000f` |
| 2 | `S-1-5-32-544` | 0 | `0x10000000` | 아니오 | `0xf000f` (`GenericAll`) |
| 3 | `S-1-1-0` | `0x0b` | `0xE0000000` | **예 (0x08)** | **`0xE0000000` 그대로** |
| 4 | `S-1-5-12` | `0x0b` | `0xE0000000` | 예 | `0xE0000000` 그대로 |
| 5 | `S-1-5-32-544` | `0x0b` | `0x10000000` | 예 | `0x10000000` 그대로 |
| 6 | `S-1-3-0` | `0x0b` | `0x10000000` | 예 | `0x10000000` 그대로 |

**3-나. 상속 전용 ACE**: **지우지도 않고 마스크도 안 바꾼다.** 자동 상속 꺼진 명시 DACL 은 **모든 ACE 를 순서대로 복사** (`RtlpInheritAcl2 +0x208 TEST DIL,DIL; +0x20b JZ → +0x2f4` 로 자동 상속 꺼진 갈래 → `+0x2f4 XOR R9B,R9B; +0x2f7 XOR AL,AL; +0x2f9 JMP → +0x222` → `+0x238 MOV R8D,R13D` (= 2, 전부 복사), 5번째 인자 `AL = 0`; `RtlpCopyAces +0xf7 CMP R15D,2; +0xfb JNZ` 가 안 갈림). `AceFlags` 도 그대로 (`RtlpCopyAces +0x1a8 ~ +0x1b0 AND byte [RBX+1],~R8B`, 지울 비트 `R8B = param_4 = 0`).
- (참고, 우리와 상관없는 갈래) 자동 상속이 켜지면 (`AutoInheritFlags & 1`: 부모 디렉터리 플래그 8 또는 `ObpObjectSecurityInheritance != 0`, 또는 `SeComputeAutoInheritByObjectTypeEx` 가 "명시 DACL 없고 부모가 `DACL_AUTO_INHERITED`" 로 켬) 복사 모드가 1 로 바뀌어 **`INHERITED_ACE 0x10` 가 붙은 ACE 는 버리고** (`RtlpCopyAces +0xf7 ~ +0x101` `~(flags>>4)&1`) 부모 ACE 를 `RtlpGenerateInheritedAce` 로 새로 만들어 섞는다. 이름 있는 객체 + 기본 설정에선 안 탐.

**3-다. `AclSize` 다시 계산**: **한다.** `0xb8 → 0x9c (156)`. `RtlpInheritAcl2 +0x1b1 ADD SI,8; +0x1b5 ADD SI,R12W; +0x1bb MOV [RBX+2],SI` = `8 + 상속으로 만든 바이트 (0) + 명시 ACE 복사 바이트 (7 개 = 20+20+24+20+20+24+20 = 148)`. `AceCount` 7 (복사할 때마다 `RtlpCopyAces +0x1b4 INC word [RDI+4]`), `AclRevision` = `max(2, 명시 ACL 리비전)` = 2 (`RtlpInheritAcl2 +0x1f3 ~ +0x1ff`), `Sbz1`·`Sbz2` 0. 출력 버퍼는 처음 `200` B (`RtlpNewSecurityObject +0x149e MOV EAX,0xc8`) 이고 모자라면 필요한 만큼으로 한 번 더 (`0xC0000023` 재시도, 최대 2 번) — 156 ≤ 200 이라 한 번에 됨. 마지막 SD 로 복사할 때 `(AclSize+3)&~3` 만큼 자리 (`+0x11e8 ~ +0x1217`), 156 은 4 의 배수라 빈틈 없음.

**3-라. 부모 (루트 `\`) 의 ACE 가 섞이나**: **아니오.** `RtlpInheritAcl2` 가 부모를 쓰는 조건 (`+0x119 TEST DIL,DIL` 자동 상속, `+0x11e TEST BPL,4` 명시 DACL 있음, `+0x128 TEST R14D,R14D` `DACL_DEFAULTED`, `+0x131 TEST DIL,DIL; +0x134 JZ → +0x48f`): **자동 상속 꺼짐 + 명시 DACL 있음 + `DEFAULTED` 아님** → 부모 건너뜀 (`+0x48f`). 명시 DACL 이 `SE_DACL_DEFAULTED` 로 오거나 DACL 이 아예 없으면 (`NULL`) 그때서야 부모에서 (`+0x513` 이하, `RtlpGenerateInheritedAce +0x441`).
- 부모 SD 가 있어도 **한 가지 일은 한다** (결과 바이트엔 영향 없음): 명시 SD·SubjectContext·부모 SD 가 다 있고 `SEF_AVOID_OWNER_RESTRICTION 0x1000` 이 없으면 (`+0x1128 BT R9D,0xc`, `+0x112f CMP [RBP-0x48],0`, `+0x1135 TEST R14,R14`, `+0x113a TEST R10,R10; +0x113d JNZ → +0x17d3`) `RtlpNewSecurityObject` 를 **한 번 더 (부모만으로, 재귀 `+0x181e/+0x1823`)** 돌려서 부모에서 물려받을 DACL 에 `OWNER RIGHTS` ACE 가 있는지 (`RtlpOwnerAcesPresent +0x1846/+0x184b`) 본다. 있으면 만든 쪽이 그 SD 에서 `WRITE_DAC 0x40000` 을 받는지 `SeAccessCheck` (`+0x1b22`) — 못 받으면 생성 실패. 루트 DACL 에 `OWNER RIGHTS` 가 없으면 아무 일 없음 `[루트 DACL 내용은 VM]`.

**지금 우리**: generic 변환 없음 (요청서 `nt_security_object.cpp`/`handles.hpp`) → **틀림**. 규칙: "INHERIT_ONLY 아닌 ACE 의 마스크를 `GenericMapping` 으로 바꾸고 `& GenericAll`, 상속 전용 ACE 는 마스크·플래그 그대로, `AclSize = 8 + ACE 크기 합`" 한 줄.

### 질문 4. SACL / 라벨 가공

**4-가. 명시 라벨 ACE (`0x11`, 마스크 1, `S-1-16-4096`)**: **그대로 둔다.** 길:
1. `RtlpNewSecurityObject +0x781 JZ → +0xbd3 ~ +0xbed` (형 마스크가 0 일 때): 명시 SACL 에서 `RtlFindAceByType(0x11)` → ACE 의 플래그·마스크·SID 를 꺼냄. **플래그가 `INHERIT_ONLY` 뿐 (`== 8`) 이거나 `INHERITED 0x10` 이 있으면 무시** (`C: local_290 == 8 || & 0x10`). 우리 플래그 0 → 사용. `& 8` 이 있고 만든 쪽 무결성 RID 가 `0x2000` 미만이면 실패 (`+0x18bb ~ +0x18d0 RtlSubAuthoritySid; CMP [RAX],0x2000`).
2. 검증 후 지역 ACL (헤더 `0x800002` = 리비전 2·크기 `0x80`) 에 라벨 ACE 를 다시 만듦 (`+0xdb0 ~ +0xee1`): SID 권한이 `{0,0,0,0,0,0x10}` 아니면 (`+0xe0c ~ +0xe26`), 플래그에 `0xffffffe0` 비트 있으면 (`+0xe2c`), **마스크에 `0xfffffff8` 비트 있으면** (`+0xe38`) → 전부 `0xC000000D`. 우리 값 (플래그 0, 마스크 1) 은 통과.
3. `RtlpInheritAcl2` **모드 3** (`+0x155e/+0x1563`; 인자 자동 상속 = **1 상수** `+0x1559 MOV byte [RSP+0x20],0x1`, 플래그 4 — 플래그는 `+0xf01 TEST EAX,0x700; JZ → +0x16d5`, `+0x16d5 TEST R14,R14; JNZ → +0x16e3 ~ +0x16fc` 에서 `R15D = (명시 Control>>1 & 0x18 \| Control & 0x2800)>>1` = `0x8014` 이면 4, `+0xf1e TEST R15D,R15D; JNZ → +0x149e` 로 이 호출 블록에 들어옴, 출력 버퍼 `+0x14c3 ExAllocatePool2(200, 태그 SeAc)`): 명시 라벨 ACL 을 복사. 복사는 `RtlpCopyAces` 의 "효과적 ACE" 갈래 → `RtlpCopyEffectiveAce` (형 > 0xe 는 **통째 복사** 후 `AceFlags &= 0xe0`, `INHERITED 0x10` 은 안 붙임 — `param_2 = 0`) → 우리 플래그 0 이라 변화 없음. 마스크·SID 불변.
4. `RtlpCombineAcls` (`+0xf73/+0xf78`): 감사 ACE (`2·3·7·8·0xd~0x10`) → 라벨 (`0x11`) → 신뢰 라벨 (`0x14`) → 접근 필터 (`0x15`) → 속성 (`0x12`) → 범위 (`0x13`) 순서로 한 ACL 에 모음. 우리는 라벨 하나뿐 → **SACL = `{rev 2, AclSize 0x1c, AceCount 1}` + 라벨 ACE 20 B**. 새 ACL 은 `ExAllocatePool2` (태그 `SeSd`) 로 `(8 + ACE 합 + 3) & ~3` = 28.
- 부모의 SACL 은 라벨 호출 (모드 3) 에서 `RtlpInheritAcl2 +0x119 ~` 부모 갈래로 들어가도 "출력에 이미 `0x11` 이 있으면 `break`" (`+0x5ab RtlFindAceByType`) 라 섞이지 않는다. 모드 2 (비-라벨 SACL) 는 우리 SACL 에 감사 ACE 가 없어서 `0x8000000b` (상속할 것 없음) 로 끝나고 부모 SACL 도 안 섞임 (`+0x16b MOV EAX,0x8000000b`).

**4-나. 만드는 쪽에 `SeSecurityPrivilege` 가 없으면**: **SACL 을 버리지 않고 실패** (`0xC0000061 STATUS_PRIVILEGE_NOT_HELD`). 그리고 **라벨만 든 SACL 은 이 검사 자체를 안 한다**:
- 게이트 `RtlpNewSecurityObject +0x116d CMP BL,1; JNZ` (BL = 이전 모드가 유저, 커널 호출이면 통째로 건너뜀: `+0x12f ~ +0x13f` 에서 `GS:[0x188]+0x232` 를 읽어 `[RBP-0x7e]` 에 저장, `SEF_FORCE_USER_MODE 0x2000` 이 있으면 항상 1 로 둠 `+0x129 BT ECX,0xd`) → `+0x1172 CMP byte [RBP-0x7f],0; JNZ → +0x1e70` (명시 SACL 에 **비-라벨 ACE 가 있었다는 표시** — `RtlpInheritAcl2` 가 모드 2 에서 `+0x2ea/+0x336 MOV byte [R13],1` 로 켬; 우리는 안 켜짐) → `+0x1e70 TEST R9B,8` (`SEF_AVOID_PRIVILEGE_CHECK`) → `+0x1e7a TEST AL,AL` (AL = 종류 마스크 판정, `+0x1151 ~ +0x116b`: 종류 마스크 `RtlpCombineAcls` 출력이 0 이거나 `0x1b0` (라벨 `0x10` · 속성 `0x20` · 신뢰 `0x80` · 필터 `0x100`) 밖 비트 (**감사 `0x8`·범위 `0x40`**) 가 있으면 1) → `+0x1ec3 SePrivilegeCheck(SeSecurityPrivilege)` + `+0x1edb SePrivilegedServiceAuditAlarm` → 실패 `+0x1eed MOV EBX,0xc0000061`.
- 우리 SACL = 라벨 하나 → 비-라벨 표시 꺼짐 → **`SeSecurityPrivilege` 안 묻는다** (smss 는 물론 그 권한도 있음).
- 라벨 **지배 검사**: `AutoInheritFlags & 8` (`AVOID_PRIVILEGE_CHECK`) 가 없으면 (`+0xfea TEST R9B,8`) 만든 쪽 무결성 SID (`[RBP+0x10]`) 가 라벨 SID 를 지배하는지 `RtlSidDominates` (`+0x1022/+0x1027`; `RtlSidDominates.c`: 둘 다 `S-1-16-x` 이고 **마지막 하위 권한 숫자가 토큰 ≥ 라벨**). 못 지배하면 `[RBP+0x2c]=1` (`+0x1031 ~ +0x1043`) → `+0x1182 CMP byte [RBP+0x2c],0; JNZ → +0x1c57` → `+0x1c99 SePrivilegeCheck(SeRelabelPrivilege)` → 실패 `0xC0000061` (`+0x1cc3`). smss (SYSTEM = `S-1-16-16384` 가정) 는 `S-1-16-4096` (낮음) 을 지배 → 통과.

**4-다. 명시 라벨이 없을 때 기본 라벨 ACE**: **조건부로 붙인다** (`RtlpNewSecurityObject` 라벨 갈래, C `uVar14`·`puVar24`; `+0xc98 ~ +0xdd9` 와 `+0x18bb ~ +0x1914`). 순서대로:

| 조건 (명시 라벨 없음) | 결과 |
|---|---|
| 형의 `SeMandatoryLabelMask != 0` (→ `AutoInheritFlags` 의 `0x100`·`0x200`·`0x400` = NO_WRITE_UP·NO_READ_UP·NO_EXECUTE_UP, `SeComputeAutoInheritByObjectTypeEx +0x58 ~ +0x5e`) | 라벨 ACE `{플래그 0, 마스크 = 그 비트, SID = 만든 쪽 토큰의 무결성 SID}`. **이 경우 명시 SACL 에서 라벨을 찾지도 않는다** (`+0x75f ~ +0x781` 이 `AutoInheritFlags` 에서 마스크 `EBX` 를 만들고 `+0x781 JZ → +0xbd3` 으로 **마스크 0 일 때만** `+0xbd3 ~ +0xbed RtlFindAceByType(명시 SACL, 0x11)` 로 감) — 그 대신 `SeComputeAutoInheritByObjectTypeEx +0xe2 ~ +0x130` 가 명시 SACL 에 라벨이 있으면 **그 ACE 마스크에 형 마스크를 OR** (`OR [R10+4],EDX`) 하고 자동 상속 비트를 0 으로 지워 줌. |
| 형 마스크 0 이고, 만든 쪽 무결성 SID 의 하위 권한이 **`< 0x2000` (Medium 미만)** | 라벨 ACE `{플래그 0, **마스크 1 (NO_WRITE_UP)**, SID = 만든 쪽 무결성 SID}` (`+0x18bb ~ +0x18d0` RtlSubAuthoritySid `CMP [RAX],0x2000; JC → +0x2194 MOV EBX,1` → `puVar24 = 1`) |
| 그 밖 (Medium 이상, 형 마스크 0) | **안 붙임.** (부모 SACL 에 상속 가능한 라벨이 있으면 `RtlpInheritAcl2` 모드 3 이 부모 쪽에서 만들 수 있음) |

- Directory 형: `SeMandatoryLabelMask = 0` → 위 두 번째 (낮은 무결성 만든이) 만 해당.
- `SeTrustConstraintMask` (신뢰 라벨 `0x14` 자동 추가) 는 `SEF_AI_USE_EXTRA_PARAMS 0x800` 이 있을 때만 (`+0x1cd0 ~ +0x1d5c`, C `param_7 >> 0xb & 1`). Ob 는 이 비트를 안 줌 → **안 탐.**

**지금 우리**: 객체 SD 에 SACL·라벨 칸이 없음 (요청서 `handles.hpp:14`) → **틀림**. 칸 추가 + "명시 라벨 그대로 / 없으면 위 표".

### 질문 5. `Control` 결과 비트

**답**: 결과 `Control` = `0x8000 (SELF_RELATIVE)` | SACL 비트 | DACL 비트. 우리 smss SD → **`0x8814`**.

| 비트 | 켜지나 | 근거 |
|---|---|---|
| `SE_SELF_RELATIVE 0x8000` | 항상 | `+0x58e MOV EAX,0x8000 / +0x593 MOV ECX,0x8800 / +0x598 CMOVZ ECX,EAX` (SACL 자동 상속 요청 (`AutoInheritFlags & 2`) 이 없으면 0x8000) → `+0x5ac MOV [RBP-0x28],ECX` |
| `SE_DACL_PRESENT 0x4` | 명시·부모·기본 DACL 중 하나라도 있으면 | `+0x110f OR EAX,0x4` / `+0x1119 OR R13D,EAX` (기본 DACL 쓸 때는 `+0x10d6 OR R13D,4`) |
| `SE_DACL_AUTO_INHERITED 0x400` | `AutoInheritFlags & 1` 일 때만 (`+0x10aa TEST R9B,1; +0x10b0 BTS R13D,0xa`), 또는 `RtlpInheritAcl2` 출력 플래그 (`& 0x1408`, `+0x1106 AND EAX,0x1408`) | 우리 DACL 은 자동 상속 꺼짐 → **안 켜짐** |
| `SE_DACL_PROTECTED 0x1000` / `DACL_DEFAULTED 0x8` | 명시 SD 에 있으면 (`& 0x1408` 로 옮김) | 우리는 없음 |
| `SE_SACL_PRESENT 0x10` | SACL 이 만들어지면 | `+0xfd3 OR EAX,0x4 / +0xfd6 ADD EAX,EAX` (= `0x10`) / `+0xfdc OR R13D,EAX` |
| **`SE_SACL_AUTO_INHERITED 0x800`** | **라벨 갈래 (모드 3) 가 성공하면 켜짐** | 라벨 호출은 자동 상속 인자가 **항상 1** (`+0x1559`), `RtlpInheritAcl2 +0xd3 ~ +0xe9` (`NEG AL; SBB ECX,ECX; AND ECX,0x400; MOV [RAX],ECX`) 가 출력 플래그 `0x400` 을 내고 `+0x1599 MOV EDI,[RBP-0x40]` 로 읽어 `+0xfc8 ~ +0xfdc` 에서 `((flags&8 \| 4)*2 \| flags&0x1400)*2 = 0x810` → `0x10 \| 0x800`. 라벨이 아예 없으면 (`+0xf34 MOV EDI,EDX` = 0, `0x8000000b` 로 돌아온 갈래) 안 켜짐 |
| `SE_SACL_PROTECTED 0x2000` / `SACL_DEFAULTED 0x20` | 명시 SD 에 있으면 | 우리는 없음 |
| `SE_OWNER_DEFAULTED 0x1`·`SE_GROUP_DEFAULTED 0x2` | **안 켜짐** (토큰에서 채워도) | 위 질문 2 |
| `SE_DACL_UNTRUSTED 0x40`·`SE_SERVER_SECURITY 0x80`·`*_AUTO_INHERIT_REQ`·`RM_CONTROL_VALID` | 안 옮김 (명시 `Control` 에서 읽기만, 결과엔 없음) | `+0x14b ~ +0x16e` 읽기만 |

- 근거 줄: 조립 `+0x1250 MOV byte [R14],1` (리비전) / `+0x124b OR word [R14+2],R13W` (`Control`).
- **정정 후보 (바로 동적 확인)**: `SE_SACL_AUTO_INHERITED` 가 정말 켜지는지는 위 코드로는 켜진다. 만약 VM 에서 `0x8014` 가 나오면 이 줄을 의심 (`+0x1559` 인자 or `+0x1599` 읽기).

**지금 우리**: 에뮬레이터가 SD 를 어떤 `Control` 로 만드는지 요청서에 없음 → 모름. **`0x8814` 로 만들면 맞다 (`[정적]`, VM 대조 1순위).**

### 질문 6. 토큰 객체 쪽

**답**: **붙는다 — 조건 없이 (형 규칙).** 토큰 형은 `SeMandatoryLabelMask = 1`.

- 형 초기화: `SepTokenInitialization` 이 초기화 구조체 길이를 `0x80` 으로 둠 (`+0x29 MOV EBX,0x80` / `+0x4e MOV [RBP-0x29],BX`, 구조체 시작 = `RBP-0x29`), 형 플래그 `+0x43 OR byte [RBP-0x27],0xe` = `UnnamedObjectsOnly | UseDefaultObject | SecurityRequired`, 그리고 구조체 `+0x78` 칸 (= `RBP+0x4f`) 에 **`+0x8a MOV dword [RBP+0x4f],EBX` (`+0x59 MOV EBX,0x1` 이라 값 1)** → `ObCreateObjectTypeEx +0x461 ~ +0x464` (길이 ≥ `0x80` 일 때만) 가 형 `+0xd8` 로 복사 → **1** (Directory 는 길이 `0x78` 이라 0).
- 이름 없는 객체 길: `ObInsertObjectEx +0x282 TEST byte [RSI+0x42],8` (SecurityRequired) → `+0x2c8 SeComputeAutoInheritByObjectTypeEx` → 토큰은 `EDI = 0x100` (`NO_WRITE_UP`) → `+0x2d8 ~ +0x2dc OR ECX,R13D` (`R13D = (param_5&1)<<4`) → `+0x325/+0x32a SeAssignSecurityEx2(부모 NULL, 명시 SD, …, IsDirectory 0 (`+0x2f0 XOR R8D,R8D` → `+0x31b MOV [RSP+0x20],R8B`), AutoInherit, 확장 `[RBP-0x78]` (첫 칸 8, `+0x2c1`), SubjectContext `[RDI+0x20]`, GenericMapping `형+0x4c`)` → `+0x38c/+0x391 SeDefaultObjectMethod(EDX=3)` (형 `+0x98` 이 `SeDefaultObjectMethod` 일 때, 아니면 `+0x7e3` 간접).
- 만들기 자리: `SepDuplicateToken +0x158/+0x15d ObCreateObjectEx` → `NtDuplicateToken +0x1f8/+0x1fd ObInsertObjectEx`, `SepCreateTokenEx +0x5dc/+0x5e1 ObCreateObject` → `+0xbd7·+0xc3a ObInsertObject` (sast/93 에서 확인한 자리).
- 명시 SD 가 보통 없음 (`NtDuplicateToken` 이 `ObjectAttributes` 에 SD 없을 때) → `RtlpNewSecurityObject` 가 **빈 SD** (`+0x621 ~ +0x637`: `LEA R11,[RBP+0xa0]; MOV byte [RBP+0xa0],1`) 를 명시 SD 로 씀. 결과:
  - 소유자·그룹: 토큰 (질문 2).
  - **DACL**: 명시도 부모도 없음 → 만든 쪽 토큰의 **기본 DACL** 사본 (`+0x10c6 MOV RAX,[RBP+0x70]; TEST RAX,RAX; JZ; … OR R13D,4`; 토큰 `+0xb8`). 최종 복사 때 generic 변환 (토큰 `GenericMapping`) 도 한 번 (`+0x13b3 TEST R13B,R13B` 가 0 이므로 `+0x13e2 ~ +0x1480` 루프를 탐). 기본 DACL 이 NULL 이면 DACL 없음 (`Dacl` 오프셋 0, `DACL_PRESENT` 꺼짐).
  - **SACL = 라벨 ACE `0x11` 하나**: `AutoInheritFlags & 0x700 = 0x100` → 명시 라벨 찾기 건너뜀 → `puVar24 = 1`, SID = **만든 쪽 토큰의 무결성 SID** (`+0x1dc ~` 에서 복사한 `[RBP+0x10]`; 토큰에 무결성이 없으면 `SepDefaultMandatorySid`), 플래그 0, 마스크 **1**. `Control` 은 `0x8814` (DACL 있을 때) 로 질문 5 와 같은 식.
- 라벨의 SID 값 = **토큰을 만든 순간 (`NtDuplicateToken` 이면 부르는 쪽 유효 토큰, `NtCreateToken` 이면 `lsass` 의 SYSTEM 토큰)** 의 무결성 → 값은 VM (`[VM]`). `NtDuplicateToken` 은 SD 가 없었을 때 `SepAppendAceToTokenObjectAcl (+0x21e)` · `SepSetProcessTrustLabelAceForToken (+0x338)` 로 **더** 덧붙이는 ACE 가 있음 (sast/93, 이번엔 안 읽음) → 토큰 SD 의 최종 모양은 이 둘까지 읽어야 닫힘 (다음 요청 후보 1).
- sast/93 질문 6 의 `[공백]` ("`0x11` 이 붙나") 은 **닫힘: 붙는다** (조건: 형 마스크 1, 명시 라벨이 있으면 그 마스크에 `| 1`).

**지금 우리**: 토큰 SD 에 라벨 없음 (샘플 53 개가 `GetSecurityInfo(토큰, 6, 0x10)` 에서 라벨 바이트를 못 받음, 요청서) → **틀림.** 규칙: "토큰 객체 SD 에 라벨 `{0x11, 플래그 0, 마스크 1, SID = 만든 쪽 무결성 SID}`", 값은 Q181 확인.

### 질문 7. 바이트 다시 만들기 (`\RPC Control`)

입력 = sast/94 Liberal SD (명시 SD: 소유자 없음 · 그룹 없음 · `Control 0x14`) + 만든 이 = smss SYSTEM 토큰 + 형 = Directory + 부모 = 루트 `\` + 자동 상속 0.

**객체에 붙는 SD (순서 = 머리 → SACL → DACL → 소유자 → 그룹)**

| 칸 | 값 | 근거 |
|---|---|---|
| 머리 `Revision` / `Sbz1` | `1` / `0` | `+0x1250 MOV byte [R14],1`, 머리 0 으로 지움 `+0x1243` |
| `Control` | **`0x8814`** | 질문 5 |
| `Sacl` 오프셋 (+0xc) | `0x14` | `+0x123c LEA RBX,[R14+0x14]` 첫 자리, `+0x136f MOV [RAX+0xc],R14D` |
| `Dacl` 오프셋 (+0x10) | `0x30` | `+0x962 MOV [R14+0x10],EAX` (SACL 28 B 뒤) |
| `Owner` 오프셋 (+4) | `0xcc` (소유자 16 B 가정 아니어도 DACL 끝 = `0xcc`) | `+0x985 MOV [R14+4],EAX` |
| `Group` 오프셋 (+8) | `0xcc + 소유자 크기` (= `0xdc`) | `+0x9a3 MOV [R14+8],EBX` |
| SACL (28 B) | 리비전 2 · `AclSize 0x1c` · `AceCount 1` · `Sbz2 0` + ACE `{0x11, 플래그 0, 크기 0x14, 마스크 1, S-1-16-4096}` | 질문 4 |
| DACL (156 B) | 리비전 2 · `AclSize 0x9c` · `AceCount 7` + 아래 ACE 7 개 | 질문 3 |
| 소유자 | `[VM]` (`[추측]` `S-1-5-32-544`, 16 B) | 질문 2 |
| 그룹 | `[VM]` (`[추측]` `S-1-5-18`, 12 B) | 질문 2 |
| 총 길이 | `0xcc + 소유자 크기 + 그룹 크기` = **`0xe8` (232)** (가정 값) | 캐시 크기 ESI (`SeDefaultObjectMethod +0xc5 ~ +0x161`) 와 같은 식 |

DACL ACE (순서 그대로): `0: 허용 S-1-1-0 플래그 0 마스크 0x2000f` · `1: S-1-5-12 0 0x2000f` · `2: S-1-5-32-544 0 0xf000f` · `3: S-1-1-0 0x0b 0xE0000000` · `4: S-1-5-12 0x0b 0xE0000000` · `5: S-1-5-32-544 0x0b 0x10000000` · `6: S-1-3-0 0x0b 0x10000000`.

**저장된 SD 바이트 (소유자 `S-1-5-32-544`, 그룹 `S-1-5-18` 가정 — `[VM]` 칸은 `0xcc ~ 0xe7`)**

```
0000  01 00 14 88 cc 00 00 00 dc 00 00 00 14 00 00 00
0010  30 00 00 00 02 00 1c 00 01 00 00 00 11 00 14 00
0020  01 00 00 00 01 01 00 00 00 00 00 10 00 10 00 00
0030  02 00 9c 00 07 00 00 00 00 00 14 00 0f 00 02 00
0040  01 01 00 00 00 00 00 01 00 00 00 00 00 00 14 00
0050  0f 00 02 00 01 01 00 00 00 00 00 05 0c 00 00 00
0060  00 00 18 00 0f 00 0f 00 01 02 00 00 00 00 00 05
0070  20 00 00 00 20 02 00 00 00 0b 14 00 00 00 00 e0
0080  01 01 00 00 00 00 00 01 00 00 00 00 00 0b 14 00
0090  00 00 00 e0 01 01 00 00 00 00 00 05 0c 00 00 00
00a0  00 0b 18 00 00 00 00 10 01 02 00 00 00 00 00 05
00b0  20 00 00 00 20 02 00 00 00 0b 14 00 00 00 00 10
00c0  01 01 00 00 00 00 00 03 00 00 00 00 [01 02 00 00
00d0  00 00 00 05 20 00 00 00 20 02 00 00][01 01 00 00
00e0  00 00 00 05 12 00 00 00]
```
(`[ ]` = `[VM]` 칸: 첫 묶음 = 소유자 16 B (`0xcc~0xdb`), 둘째 묶음 = 그룹 12 B (`0xdc~0xe7`). 길이가 다르면 `Group` 오프셋·총 길이 따라 움직임.)

**`NtQuerySecurityObject(h, 0x17, …)` 대답** (sast/93 규칙: 머리 `0x14` → Owner → Group → DACL → SACL, 오프셋 칸 Owner `+4` Group `+8` Sacl `+0xc` Dacl `+0x10`, `Control = 0x8000 | (원본 & 0x1) | (& 0x2) | (& 0x140c) | (& 0x2830)`; 마스크 `0x17 & 0x1f8 = 0x10` → `RtlpFilterSacl` 이 형 `0x11` 만 담음). 필요 길이 `0x14 + 16 + 12 + 156 + 28` = **`0xe8` (232)** (소유자·그룹 크기는 `[VM]`), `Control` **`0x8814`**:

```
0000  01 00 14 88 14 00 00 00 24 00 00 00 cc 00 00 00
0010  30 00 00 00 [01 02 00 00 00 00 00 05 20 00 00 00
0020  20 02 00 00][01 01 00 00 00 00 00 05 12 00 00 00]
0030  02 00 9c 00 07 00 00 00 00 00 14 00 0f 00 02 00
0040  01 01 00 00 00 00 00 01 00 00 00 00 00 00 14 00
0050  0f 00 02 00 01 01 00 00 00 00 00 05 0c 00 00 00
0060  00 00 18 00 0f 00 0f 00 01 02 00 00 00 00 00 05
0070  20 00 00 00 20 02 00 00 00 0b 14 00 00 00 00 e0
0080  01 01 00 00 00 00 00 01 00 00 00 00 00 0b 14 00
0090  00 00 00 e0 01 01 00 00 00 00 00 05 0c 00 00 00
00a0  00 0b 18 00 00 00 00 10 01 02 00 00 00 00 00 05
00b0  20 00 00 00 20 02 00 00 00 0b 14 00 00 00 00 10
00c0  01 01 00 00 00 00 00 03 00 00 00 00 02 00 1c 00
00d0  01 00 00 00 11 00 14 00 01 00 00 00 01 01 00 00
00e0  00 00 00 10 00 10 00 00
```
(`[ ]` = `[VM]` (소유자 `0x14~0x23`, 그룹 `0x24~0x2f`). 오프셋: Owner `0x14`, Group `0x14 + 소유자`, Dacl `0x30` (= 위 가정), Sacl `0xcc`.) 첫 부름 (버퍼 NULL·길이 0) 은 `0xC0000023` + `*LengthNeeded = 0xe8` (sast/93 질문 4).

**`NtQuerySecurityObject(h, 0x10, …)` 대답** (LABEL 만): Owner·Group·DACL 안 담음. 필요 길이 `0x14 + 28` = **`0x30` (48)**, `Control` **`0x8810`** (`0x8000 | SACL 비트 0x810`), `Sacl` 오프셋 `0x14`, 나머지 오프셋 0:

```
0000  01 00 10 88 00 00 00 00 00 00 00 00 14 00 00 00
0010  00 00 00 00 02 00 1c 00 01 00 00 00 11 00 14 00
0020  01 00 00 00 01 01 00 00 00 00 00 10 00 10 00 00
```
(이 대답엔 `[VM]` 칸 없음 — 소유자·그룹을 안 묻기 때문. 라벨은 smss 가 준 값 그대로.)

- 지름길 확인: 위 세 바이트 덩어리는 `ms/` 에서 본 값 (`SIDs` smss `.asm` 줄 `+0xd1e ~ +0xe6f`, GenericMapping 파일 바이트) 으로 만든 표 + 규칙이며 `[정적]`. 직접 만든 계산은 `scratchpad` 의 작은 파이썬 (repo 밖, 커밋 안 함).

**지금 우리**: `\RPC Control` 에 SD 를 안 채움 → **틀림.** 위 표를 객체 SD 로 넣고 (SACL 칸 추가, `Control 0x8814`), 묻기는 sast/93 규칙 (순서 Owner → Group → DACL → SACL) 으로 만들면 `0x17` 은 위 `0xe8` 바이트, `0x10` 은 `0x30` 바이트.

## 에뮬레이터용 규칙 한 장 (질문 1~6)

1. 객체 만들 때 (이름 있든 없든) `AssignSecurity(부모SD, 명시SD, 만든토큰, GenericMapping, IsDirectory)` 를 한 번 부른다. 결과를 `self-relative` 로 객체에 건다. 같은 바이트면 공유해도 됨 (캐시).
2. 명시 소유자 없으면 토큰 `DefaultOwner` (`UserAndGroups[DefaultOwnerIndex]`), 명시 그룹 없으면 토큰 `PrimaryGroup`. `*_DEFAULTED` 안 켬.
3. DACL: 명시 DACL 있고 자동 상속 꺼짐이면 **명시 ACE 만**, 순서대로, 상속 전용 (`0x08`) 아닌 것만 generic → `GenericMapping` (`& GenericAll`), `AclSize = 8 + ΣACE`, `Control |= 0x4`. 명시 DACL 이 없으면 부모 DACL 에서 상속 가능한 ACE 를 만들고 (`RtlpGenerateInheritedAce` — 이번엔 안 푼 곳), 만들 게 없으면 (`0x8000000b`) 토큰 `DefaultDacl` (코드 줄 `+0x10c6 ~ +0x10d6`).
4. SACL: 라벨 ACE (`0x11`) 명시 → 그대로 (플래그 `INHERIT_ONLY` 뿐/`INHERITED` 면 무시, 마스크 `& ~7` 있으면 `0xC000000D`, 만든 쪽 무결성이 라벨을 못 지배하면 `SeRelabelPrivilege` 필요). 감사 ACE 가 있으면 `SeSecurityPrivilege` 필요 (없으면 `0xC0000061`). 라벨 없고 형 마스크 ≠ 0 이면 `{형 마스크, 만든 쪽 무결성 SID}`, 형 마스크 0 이고 만든 쪽 무결성 < Medium (`0x2000`) 이면 `{마스크 1, 만든 쪽 무결성 SID}`.
5. 라벨 갈래가 돌면 `Control |= SACL_PRESENT | SACL_AUTO_INHERITED (0x810)`. 최종 `Control = 0x8000 | DACL 비트 | SACL 비트`.
6. 토큰 형: 형 마스크 1 → 토큰 SD 에 항상 `{0x11, 0, 마스크 1, 만든 쪽 무결성 SID}` + 기본 DACL.
7. 저장 순서 머리 → SACL → DACL → 소유자 → 그룹 (묻기 출력은 머리 → 소유자 → 그룹 → DACL → SACL).

## 동적으로 확인할 것 (Q181 에 넘길 칸)

정적으로 못 정한 것: 소유자·그룹 SID 값, 루트 `\` 의 SD, 토큰 라벨 SID, `ObObjectSecurityInheritance` 레지스트리 값, `Control` 의 `0x800` 비트 (코드로는 켜지지만 대조).

**dast 요청 필요**: 아래를 Q181 (`\RPC Control`·토큰 SD 바이트) 에 묶기.

1. **`\RPC Control` 의 실제 SD (부팅 직후, 사후 읽기로 충분)**: `!object \RPC Control` → 몸통 주소 `B` → `dq B-8 L1` → 값 `& 0xfffffffffffffff0` = SD 포인터 `S` → `db S L0xe8` (그리고 `!sd S 1`). 위 "저장된 SD 바이트" 표와 칸별 대조:
   - `Control` `0x8814` 인가 (`0x8014` 이면 질문 5 의 `+0x1559`·`+0x1599` 의심).
   - DACL `AclSize` `0x9c`, SACL `AclSize` `0x1c`, ACE 0~2 마스크 `0x2000f`·`0x2000f`·`0xf000f`, ACE 3~6 마스크 `0xE0000000`·`0xE0000000`·`0x10000000`·`0x10000000` 이 그대로인가.
   - 소유자·그룹 SID (`[VM]` 칸), 오프셋 `Sacl 0x14 / Dacl 0x30 / Owner 0xcc / Group 0xcc+소유자`.
2. **같은 SD 로 만든 `\Windows`**: 같은 처음 바이트인가 (캐시 항목 공유 확인: `ObsSecurityDescriptorCache`). csrss 가 DACL 을 고친 뒤 값은 다름 (sast/94).
3. **형 객체 칸**: `dd poi(nt!ObpDirectoryObjectType)+0x4c L4` (= `20003 2000c 20003 f000f`), `dd poi(nt!ObpDirectoryObjectType)+0xd8 L2` (= `0 0`), `dd poi(nt!SeTokenObjectType)+0xd8 L2` (= `1 0`), `dd poi(nt!SeTokenObjectType)+0x4c L4` (= `2001a 201e0 20005 f01ff`), `dd nt!ObpObjectSecurityInheritance L1` (= `0`), `dd poi(nt!ObpRootDirectoryObject)+0x150 L1` (루트 `\` 디렉터리 `Flags`, 비트 8 = 0 이어야 부모 SD 를 가져옴 — `ObpInsertOrLocateNamedObject +0x14a/+0x150`).
4. **smss 토큰**: smss 의 토큰 객체 `T` → `dt nt!_TOKEN T DefaultOwnerIndex PrimaryGroup UserAndGroups IntegrityLevelIndex DefaultDacl` → 소유자·그룹 SID (`!sid`). 질문 2 의 `[VM]` 값.
5. **토큰 객체 SD**: `NtOpenProcessToken(자기, 0xa0008)` 로 연 토큰의 SD 바이트: 라벨 ACE `{0x11, 플래그 0, 마스크 1, SID}` 의 SID (만든 쪽 무결성), DACL 이 토큰 `DefaultDacl` 과 같은지, `Control` `0x8814`.
6. **(선택, 부팅 중)** 만들 때를 잡으려면 KDNET 초기 중단 뒤: `nt!ObpAssignSecurity+0xde` (`SeAssignSecurityEx2` 직후, `eax` = status, `poi(@rsp+0x58)` = 새 SD 임시 포인터) 에서 `db poi(@rsp+0x58) L0xe8`, 그리고 `nt!RtlpNewSecurityObject+0x1225` (`rdx` = 마지막 SD 할당 크기 = 가정과 같으면 `0xe8`). 조건은 `r12` (몸통) 가 `\RPC Control` 인지 `!object @r12` — 어렵다 → 1 번 사후 읽기로 대신 권장.

## 다음 요청 후보

1. `NtDuplicateToken` 의 SD 덧붙임: `SepAppendAceToTokenObjectAcl` (`+0x21e/+0x223`, 인자 `8`·`SeAliasAdminsSid`) · `SepSetProcessTrustLabelAceForToken` (`+0x338/+0x33d`) — 토큰 SD 에 **더** 붙는 ACE 의 형·마스크·SID (이번엔 이름만). 토큰 SD 바이트를 닫으려면 필요 (낮음~중간).
2. `NtOpenDirectoryObject("\RPC Control", 0x20001)` 접근 검사 (`ObOpenObjectByNameEx` → `ObpLookupObjectName`·`ObpCheckObjectReference`·`SeAccessCheck` 이름만) — 이 SD 로 `0x20001` 이 허용되는 조건. 미리 보기 `[추측]`: ACE 0 (Everyone `0x2000f`) 가 `0x20001` 를 덮어서 허용, 라벨 NO_WRITE_UP 은 읽기를 안 막음 → 에뮬레이터가 열기를 허용해야 함 (sast/93 "다음 요청 후보 2" 와 같음).
3. (낮음) 명시 DACL **없는** 새 객체의 부모 상속 세부 (`RtlpGenerateInheritedAce` · 루트 `\` SD `SePublicDefaultUnrestrictedSd` 내용: `SepInitializationPhase0/1`) — 에뮬레이터가 `\BaseNamedObjects` 같은 다른 디렉터리·뮤텍스를 만들 때 필요. 이번 요청서는 명시 SD 만 다뤘음.

## 판정

**분석성공** — 질문 1 (사슬 오프셋 + 요청서와 다른 점), 2 (토큰 칸), 3 (generic 변환: 비-INHERIT_ONLY 만 · 상속 전용 ACE 불변 · `AclSize` 재계산 · 부모 안 섞임 · Directory `GenericMapping` 숫자 넷), 4 (라벨 그대로 · 권한 조건 · 기본 라벨 조건), 5 (`Control 0x8814`, `SACL_AUTO_INHERITED` 가 켜지는 줄), 6 (토큰 라벨 붙음), 7 (저장·`0x17`·`0x10` 바이트) 전부 `.asm` 줄로 닫았다. 값이 VM 몫인 것은 소유자·그룹·토큰 라벨 SID 와 루트 SD 뿐이고 `[VM]` 로 표시했다. 확신이 낮은 한 곳: `Control` 의 `SE_SACL_AUTO_INHERITED 0x800` (코드 줄은 명확하지만 실제 SD 로 대조 필요, Q181 1번).

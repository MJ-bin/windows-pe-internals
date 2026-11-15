# 93 rpc-sd-query — 정적 분석 결과

- 요청서: `_workspace/sast/93_rpc-sd-query.md` (대기열 Q180a)
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`), PDB `ntkrnlmp.pdb` guid_age = `C29EBFB06B78B3C020DCA66D99713F9E1` (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`)
- analyze: **다시 안 함** (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 다 있음). 읽기 전용 export 와 map 만 돌림.
- 뽑은 함수 (`ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`):
  - depth 1: `^(NtQuerySecurityObject|ObQuerySecurityDescriptorInfo|SeQuerySecurityDescriptorInfo|SeDefaultObjectMethod)$` → 31개 (callee 포함)
  - depth 0: `SeQuerySecurityAccessMask` `ObpGetObjectSecurity` `SeQueryMandatoryLabel` `ObCreateObjectTypeEx` `ObCreateObjectType` `ObInitSystem` `SepInitializationPhase0/1` `SepTokenInitialization` `ObpCreateDefaultObjectTypeSD`
  - 질문 6 용 depth 0: `NtDuplicateToken` `SepDuplicateToken` `NtCreateToken` `NtCreateTokenEx` `SepCreateToken` `SepCreateTokenEx` `ObpAssignSecurity` `SeAssignSecurityEx` `SeAssignSecurity` `SepAppendAceToTokenObjectAcl` `SepSetProcessTrustLabelAceForToken` `RtlAddProcessTrustLabelAce`
  - 지도: `ms/disassemble/ntoskrnl/_map/NtQuerySecurityObject_{edges,nodes,summary}` (깊이 4, 기준 20)
  - 범위를 넓힌 곳: `ObpReferenceObjectByHandleWithTag` (질문 1 의 "권한 모자라면 status" 가 여기서 정해져서 읽음, 이미 있던 `.asm`) 와 `RtlpFilterSacl` (질문 2 핵심, `SeQuerySecurityDescriptorInfo` 의 callee 라서 depth 1 에 들어옴).
- 버전 확인: 일치. `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 에 그대로 있음 (알려진 사실 표와 같음).
- 스크립트 문제: 없음.
- 쓰기 범위: ntoskrnl 쪽 파일만. smss (sast/94) 쪽 파일·Ghidra 프로젝트는 안 건드림.

## 지도에서 본 것

`NtQuerySecurityObject` 뿌리 (깊이 0, caller 1곳) 지도 기준 직접 부르는 것: `ExRaiseAccessViolation` (+0xa8/+0xad), `ExRaiseDatatypeMisalignment` (+0xae/+0xb3), `ObReferenceObjectByHandle` (+0x145/+0x14a), `SeDefaultObjectMethod` (+0x1d4/+0x1d9), `ObfDereferenceObject` (+0x1f9/+0x1fe), `IopGetSetSecurityObject` (+0x238/+0x23d), `CmpSecurityMethod` (+0x258/+0x25d), 그리고 CFG 간접 호출 하나 (+0x265/+0x26a, `indirect-cfg`).

- 깊이 1: `SeDefaultObjectMethod` → 깊이 2: `SeQuerySecurityDescriptorInfo` → 깊이 3: `RtlpFilterSacl`.
- 이 지도에 **없는** 것: `ObQuerySecurityDescriptorInfo` · `ObpGetObjectSecurity` · `SeQuerySecurityAccessMask` · `SeQueryMandatoryLabel`. 직접 호출로는 닿지 않는다는 뜻이다 (간접 호출 한 곳 +0x265 만 빼고, 그건 아래 질문 5).
- 권한 계산 (`SeQuerySecurityAccessMask` 와 같은 규칙) 은 `NtQuerySecurityObject` 본문에 **인라인**으로 들어 있다.

## 질문별 답 [정적]

### 질문 1. 비트 → 필요한 핸들 권한

**답 (표)**: `NtQuerySecurityObject` 가 `SecurityInformation` 으로 `DesiredAccess` 를 만든 뒤 `ObReferenceObjectByHandle(핸들, DesiredAccess, ObjectType=NULL, 이전모드, ...)` 에 넘긴다. 비트마다 요구하는 권한은 아래. 여러 비트가 켜지면 OR.

| SecurityInformation 비트 | 이름 | 필요한 핸들 권한 | 근거 `NtQuerySecurityObject.asm` |
|---|---|---|---|
| `0x1` | OWNER | `READ_CONTROL` (0x20000) | `+0xd4` BTS EDX,0x11 / `+0xd8` TEST R8B,0x17 / `+0xdc` CMOVZ |
| `0x2` | GROUP | `READ_CONTROL` | 같은 줄 (0x17 에 들어 있음) |
| `0x4` | DACL | `READ_CONTROL` | 같은 줄 |
| `0x8` | SACL | `ACCESS_SYSTEM_SECURITY` (0x1000000) 만. `READ_CONTROL` 은 요구 안 함 | `+0x118` BTS EDX,0x18 / `+0x11c` TEST [RSP+0x88],0x8 / `+0x124` CMOVZ |
| `0x10` | **LABEL** | **`READ_CONTROL`** | `+0xd8` TEST R8B,**0x17** (0x10 이 이 마스크 안에 있음) |
| `0x20` | ATTRIBUTE | `READ_CONTROL` | `+0xe1`~`+0xe9` (TEST R8B,0x20) |
| `0x40` | SCOPE | `READ_CONTROL` | `+0xee`~`+0xf6` (TEST R8B,0x40) |
| `0x80` | PROCESS_TRUST_LABEL | `READ_CONTROL` | `+0xfb`~`+0x103` (TEST R8B,0x80) |
| `0x100` | ACCESS_FILTER | `READ_CONTROL` | `+0x106` BT R8D,8 / `+0x10b` JC → `+0x279` BTS ECX,0x11 |
| `0x10000` | BACKUP | `READ_CONTROL` + `ACCESS_SYSTEM_SECURITY` (0x1020000) | `+0xc7` BT R8D,0x10 / `+0xcc` JC → `+0x26f` MOV ECX,0x1020000 → `+0xd2` 로 돌아옴 |
| 그 밖의 비트 | | 요구 없음 (무시) | 위 테스트에 안 걸림 |

- 이 계산은 `SeQuerySecurityAccessMask` (`+0x0`~`+0x5a`) 와 줄마다 같은 규칙이다. 그 함수의 caller 는 `IoCheckFunctionAccess` 하나뿐이고 `NtQuerySecurityObject` 는 부르지 않는다 (복사본이 인라인). 즉 "길에 있나" = **없음, 같은 규칙의 복사본이 본문에 있음**.
- **`0x20001` 핸들로 `0x17` 이 통과하나 = 예.** `0x17` → DesiredAccess = `0x20000` (READ_CONTROL). 핸들이 가진 권한 `0x20001` 에 그 비트가 있다. LABEL (`0x10`) 은 `READ_CONTROL` 만으로 된다.
  - 확인 줄: `ObpReferenceObjectByHandleWithTag.asm` `+0x50f` MOV EAX,R14D (핸들이 가진 권한) / `+0x512` NOT EAX / `+0x514` TEST EBP,EAX (DesiredAccess & ~가진권한) / `+0x516` JNZ → `+0x693`. 이 비교는 이전 모드가 유저일 때만 한다 (`+0x392` CMP byte [RSP+0xc8],BPL / `+0x3a1` JNZ → `+0x50f`).
  - 래퍼 `ObReferenceObjectByHandle.asm` (`+0x29` CALL) 는 `DesiredAccess` (EDX) 를 그대로 둔다.
- 권한 모자라면: **`STATUS_ACCESS_DENIED` `0xC0000022`** (`ObpReferenceObjectByHandleWithTag +0x693` MOV EBP,0xc0000022). 이때 `NtQuerySecurityObject` 는 `+0x14c` JS → `+0x200` 으로 바로 돌아가서 `*LengthNeeded` 를 쓰지 않는다.
- 잘못된 핸들은 `0xC0000008` (`+0x575`). `ObjectType` 은 NULL 이라 형 검사는 없다 (`+0x1e5` TEST RCX,RCX → `+0x38b`). 커널 모드 호출이면 권한 비교 자체를 건너뛴다.
- 토큰 핸들 `0xa0008` (= `READ_CONTROL | WRITE_OWNER | TOKEN_QUERY`) 로 `0x10` 도 같은 이유로 통과 (`0x20000` 이 들어 있음).

**지금 우리 (PE_emulator)**: LABEL 비트를 모른다고 했는데, 규칙은 "O/G/D/LABEL 은 `READ_CONTROL`" 한 줄에 같이 들어간다. 표만 넣으면 맞는다. SACL 만 `ACCESS_SYSTEM_SECURITY` 로 따로 (READ_CONTROL 아님). → **모자람, 표로 채우면 됨**.

### 질문 2. LABEL `0x10` 만 있고 SACL `0x8` 이 없을 때 — 어느 ACE 를 담나

**답**: `SeQuerySecurityDescriptorInfo` 가 SACL 을 담을 때 `RtlpFilterSacl(객체 SACL, 출력, 크기, 마스크)` 를 부른다 (`+0x320/+0x325` 복사, `+0x383/+0x388` 크기만). 마스크 = `SecurityInformation & 0x1f8`. ACE 형마다 켜져 있어야 하는 마스크 비트:

| ACE 형 | 담으려면 마스크에 | 근거 `RtlpFilterSacl.asm` |
|---|---|---|
| `0x11` SYSTEM_MANDATORY_LABEL | `0x10` (LABEL) | 크기 `+0x49`/`+0x4c` CMP EAX,0x11 → `+0x52` TEST BPL,0x10. 복사 `+0xb3`/`+0xb6` JZ → `+0x114` TEST BPL,0x10 |
| `0x12` RESOURCE_ATTRIBUTE | `0x20` | 점프표 (크기 표 `0x850b48`, 복사 표 `0x850b98`) 에서 `+0x17c` TEST BPL,0x20 / `+0x185` |
| `0x13` SCOPED_POLICY_ID | `0x40` | `+0x18c` TEST BPL,0x40 / `+0x1b8` |
| `0x14` PROCESS_TRUST_LABEL | `0x80` | `+0x13b` TEST BPL,BPL (부호 = 0x80) / `+0x168` |
| `0x15` ACCESS_FILTER | `0x100` | `+0x19b` BT EBP,8 / `+0x1bf` |
| 그 밖의 모두 (`0x2` `0x3` `0x7` `0x8` `0xd`~`0x10` 감사·알람 형, `0x0`/`0x1`/`0x4`~`0x6`/`0x9`~`0xc` 허용·거부 형, 15 넘는 번호) | `0x8` (SACL) | `+0x10c` TEST R9B,BPL (R9B=8) / `+0x1aa` / 복사 표 → `+0xc1` TEST BPL,8 |

- 그래서 **마스크가 `0x10` 뿐이면 담기는 건 형 `0x11` ACE 뿐이다.** `0x12`~`0x15` 는 안 간다 (각자 `0x20`~`0x100` 이 필요). 감사 ACE 도 안 간다 (`0x8` 이 필요).
- 반대로 `0x8` 만 있으면 라벨 ACE 는 안 간다. 둘 다 있으면 `0x11` 포함 전부.
- ACE 는 **객체 SACL 안 차례 그대로** 한 번에 하나씩 통째로 (`AceSize` 바이트) 복사된다 (`+0xa6`~`+0xfb` 루프, 복사 `+0xd2/+0xd7`).
- 담을 때 SACL 머리 다시 만들기 (`RtlpFilterSacl.asm`):
  - `+0x97` MOV RAX,[RSI] / `+0x9a` MOV [R14],RAX: 객체 SACL 의 앞 8 바이트를 통째로 복사 (`AclRevision` · `Sbz1` · `Sbz2` 는 객체 것 그대로).
  - `+0x9d` MOV [R14+2],R9D (R9D=8): 바이트 2~5 를 `08 00 00 00` 으로 덮음 → `AclSize = 8`, `AceCount = 0`.
  - ACE 하나 담을 때마다 `+0xd7` `AceCount` +1, `+0xe0` `AclSize` += 그 ACE 의 `AceSize`.
  - 결과: `AclSize = 8 + Σ(담은 ACE 크기)`, `AceCount = 담은 개수`. 자리는 `(8 + Σ + 3) & ~3` (`+0x6e`/`+0x72`), 크기 계산은 먼저 한 번 돌고 (`+0x3e`~`+0x6c`) 모자라면 필요한 크기만 `*R8` 에 쓰고 돌아온다 (`+0x75` CMP [R8],EAX / `+0x7a` MOV [R8],EAX).
- **라벨 ACE 가 없을 때**:
  - (a) 객체에 SACL 이 없을 때 (`Control` 에 `SE_SACL_PRESENT 0x10` 이 꺼져 있거나 SACL 오프셋이 0): SACL 포인터가 0 이 되어 (`SeQuerySecurityDescriptorInfo +0xc9`~`+0xd1`, `+0x3d1`~`+0x3d9`) 복사를 건너뛴다 (`+0x302` TEST RAX,RAX / `+0x305` JZ → `+0x46a`, 크기 쪽 `+0x370` TEST R11,R11 / `+0x373` JZ). → **`Sacl` 오프셋 = 0**, 출력 `Control` 의 `SE_SACL_PRESENT` 는 **객체 `Control` 에 있던 대로** (SACL 이 아예 없으면 꺼짐).
  - (b) SACL 은 있는데 `0x11` 이 없을 때: 그래도 `RtlpFilterSacl` 이 돈다. 담을 ACE 0개라서 **8 바이트짜리 빈 SACL** (`AclSize=8`, `AceCount=0`, 나머지 머리는 객체 것) 이 붙고 `Sacl` 오프셋은 0 이 아니다. `SE_SACL_PRESENT` 켜짐 (객체 `Control` 에서 옴).
  - (c) 객체 `Control` 에 `SE_SACL_PRESENT` 는 있는데 SACL 오프셋이 0 인 경우 (NULL SACL): 오프셋 0 + `SE_SACL_PRESENT` 켜짐.
- 객체 SD 가 `NULL` 이면 (아래 질문 3 의 첫 갈래): 아무 구성요소도 안 담김.

**지금 우리**: 객체 SD 에 SACL 칸이 없으니 (a) 와 같은 결과 (오프셋 0, `SE_SACL_PRESENT` 꺼짐) 가 나온다. **진짜 객체에 SACL 이 없다면 지금 구조로도 바이트가 맞을 수 있다.** 진짜 객체 SD 에 라벨 ACE 가 있는지는 VM 몫 (아래 동적 확인 1).

### 질문 3. 나오는 바이트 꼴 (바이트 만들기 규칙)

`SeQuerySecurityDescriptorInfo` (`ms/disassemble/ntoskrnl/SeQuerySecurityDescriptorInfo.asm`). 입력: `SecurityInformation`, 객체 SD, 받은 길이. 순서대로:

0. **객체 SD 가 없을 때 (NULL)**: `+0x3c`/`+0x3f`/`+0x42` JZ → `+0x430`. 필요 길이 `0x14` 를 `*길이` 에 쓰고 (`+0x435`), 받은 길이가 `0x14` 보다 작으면 `0xC0000023` (`+0x43c`), 아니면 20 바이트 (`Revision=1`, `Control=0x8000`, 나머지 0) 를 쓰고 성공 (`+0x443`~`+0x45e`, 성공 `+0x446` XOR EAX,EAX).
1. **`BACKUP (0x10000)` 이면 마스크 넓히기**: `+0x10b` BT R9D,0x10 / `+0x110` JC → `+0x3ef`. `SecurityInformation |= 0x1ff`, 그런데 원래 `0x80` 이 없었으면 `0x80` 을 다시 끄고 (`+0x460` BTR R9D,7), 원래 `0x100` 이 없었으면 `0x100` 도 끈다 (`+0x405` BTR R9D,8). 결과 = `0x7f` (O G D S LABEL ATTR SCOPE) (+원래 켠 것).
2. **구성요소 찾기** (객체 SD 가 self-relative 이면 `base+오프셋`, absolute 이면 포인터 칸): Owner = 오프셋 0 이면 없음. Group 같음. DACL = 객체 `Control` 에 `0x4` 가 있을 때만 (`+0x9a`), SACL = 객체 `Control` 에 `0x10` 이 있을 때만 (`+0xc0`). 없으면 포인터 0.
3. **크기 모으기** (`needed` 시작 `0x14`, `+0xee`):
   - Owner (비트 `0x1` + 있음): `8 + 4*SubAuthorityCount` (`+0x121`~`+0x130`).
   - Group (`0x2` + 있음): 같은 식 (`+0x142`~`+0x153`).
   - DACL (`0x4` + 객체 `Control` `0x4` + 있음): `(AclSize + 3) & ~3` (`+0x178`~`+0x186`).
   - SACL (`0x1f8` 중 아무거나 + 객체 `Control` `0x10` + 있음): `RtlpFilterSacl` 의 필요 크기 `(8 + 담을 ACE 합 + 3) & ~3` (`+0x199`/`+0x19b` JNZ → `+0x370`~`+0x388`). 담을 ACE 가 없어도 8.
4. `*길이 = needed` (`+0x1a1`/`+0x1a9`), `needed > 받은 길이` 면 `STATUS_BUFFER_TOO_SMALL` (`+0x1ab`/`+0x1af` JA → `+0x3a2` MOV EAX,0xc0000023). 이 갈래는 출력 버퍼에 아무것도 안 쓴다 (머리 지우기 `+0x1b5` 가 이 비교 뒤).
5. **머리 20 바이트** (`+0x1b5`~`+0x1cc`): 16+4 바이트 0 으로 지우고 `Revision = 1` (객체 값을 안 옮기고 상수), `Sbz1 = 0`, `Control = 0x8000` (`SE_SELF_RELATIVE`, 새로 켜는 유일한 비트).
6. **본문 시작** = `(버퍼 + 0x17) & ~3` (`+0x1d0`/`+0x1d4`) = 4 바이트 맞춤 위로 `+0x14`. 유저 버퍼는 입구에서 4 바이트 맞춤이 이미 보장됨 (질문 4).
7. **구성요소 차례: Owner → Group → DACL → SACL** (`Sacl` 이 `Dacl` 앞이 아니다). 각각 `memcpy` 뒤 머리 칸에 `(본문 위치 − 버퍼 시작)` 오프셋을 쓰고, 위치를 4 바이트 올림해서 이동:
   | 차례 | 조건 | 복사 길이 | 오프셋 칸 | `Control` 에 옮기는 비트 |
   |---|---|---|---|---|
   | Owner | 비트 `0x1` + 있음 (`+0x1e0`~`+0x1eb`) | SID 길이 (`8+4n`, 이미 4의 배수) `+0x1fa` | `+4` (`+0x203`) | 객체 `Control & 0x1` (`SE_OWNER_DEFAULTED`) (`+0x206`~`+0x212`) |
   | Group | 비트 `0x2` + 있음 (`+0x236`~`+0x23d`) | 같은 식 `+0x24a` | `+8` (`+0x253`) | `& 0x2` (`SE_GROUP_DEFAULTED`) (`+0x256`~`+0x262`) |
   | DACL | 비트 `0x4` 가 켜지면 `Control` 비트 먼저 (`+0x28c`~`+0x29f`), 객체에 DACL 있고 `SE_DACL_PRESENT` 일 때만 복사 (`+0x2a3`~`+0x2ac`) | `AclSize` 바이트 (`+0x2ae` MOVZX R8D,[RBX+2], `+0x2b9`), 위치는 `(AclSize+3)&~3` 만큼 이동 (`+0x2b5`~`+0x2cb`) | `+0x10` (`+0x2c2`) | `& 0x140c` (`DACL_PRESENT 0x4` · `DACL_DEFAULTED 0x8` · `DACL_AUTO_INHERITED 0x400` · `DACL_PROTECTED 0x1000`) |
   | SACL | `SecurityInformation & 0x1f8` 가 0 아님 (`+0x2d8` TEST [R8],0x1f8) 이면 `Control` 비트 먼저 (`+0x2e5`~`+0x2ee`), 객체 `Control 0x10` + SACL 있을 때만 복사 (`+0x2f3`~`+0x305`) | `RtlpFilterSacl` 이 쓴 만큼 (`+0x30b`~`+0x325`, `+0x32c` 위치 += [RSP+0x24]) | `+0xc` (`+0x329`) | `& 0x2830` (`SACL_PRESENT 0x10` · `SACL_DEFAULTED 0x20` · `SACL_AUTO_INHERITED 0x800` · `SACL_PROTECTED 0x2000`) |
8. 성공 `0` (`+0x46e` XOR EAX,EAX).

- **옮기지 않는 `Control` 비트**: 위 마스크 밖 전부 (`SE_DACL_UNTRUSTED` · `SE_SERVER_SECURITY` · `*_AUTO_INHERIT_REQ` · `SE_RM_CONTROL_VALID` 등은 안 간다).
- **안 물은 구성요소**: 오프셋 칸은 머리를 0 으로 지운 그대로라서 **0**. 해당 `Control` 비트도 안 켜진다.
- **구성요소 사이 빈틈** (올림으로 생기는 바이트) 은 안 지운다 (`memset` 없음). Owner·Group 은 4 의 배수라 틈이 안 생기고, DACL·SACL 은 `AclSize` 가 4 의 배수가 아닐 때만 생긴다 (보통 안 생김).
- 객체 SD 가 absolute 형이어도 출력은 항상 self-relative.

**지금 우리**: 소유자·그룹·DACL 만 있는 객체로 `0x17` 을 만들면 `Owner → Group → DACL`, `Sacl = 0`, `Control = 0x8000 | (원본 & 0x1) | (원본 & 0x2) | (원본 & 0x140c)` 로 만들어야 한다. 차례·맞춤을 이 표대로 하면 맞는다 (지금 우리 코드가 어떤 차례인지는 이쪽에서 모름 → 확인할 것: 차례가 Owner·Group·DACL 인지).

### 질문 4. 크기만 묻기 — status · `*LengthNeeded`

**세 경우 표** (`NtQuerySecurityObject` → `SeDefaultObjectMethod` → `SeQuerySecurityDescriptorInfo`, 객체 SD 는 있다고 가정):

| 경우 | status | `*LengthNeeded` | 버퍼 |
|---|---|---|---|
| 크기만 (버퍼 NULL · 길이 0) | **`0xC0000023` `STATUS_BUFFER_TOO_SMALL`** (`SeQuerySecurityDescriptorInfo +0x3a2`) | **쓴다** = 필요 길이 (`+0x1e2`) | 안 씀. 길이 0 이면 입구에서 버퍼를 검사도 안 한다 |
| 작음 (0 < 길이 < 필요) | `0xC0000023` | 쓴다 = 필요 길이 | 안 씀 (입구 검사 때 페이지마다 읽고-다시 쓰기만, 값은 안 바뀜) |
| 충분 | `0` | **쓴다** = 필요 길이 (= 만든 바이트 수) | 채움 |

- **`*LengthNeeded` 를 쓰는 자리**: `NtQuerySecurityObject +0x1db` MOV EAX,[RSP+0x98] / **`+0x1e2` MOV [RDI],EAX**. 이건 형의 `SecurityProcedure` 가 **돌아온 뒤** 에 하고, status 가 실패 (`0xC0000023` 포함) 여도 쓴다. 이 `+0x1db`~`+0x1e6` 이 두 번째 `__try` 범위 (`+0x1e6` 이 `__except` 착지점, ObfDereferenceObject 부르고 예외 코드를 status 로 돌려줌).
- 그 값은 안쪽 함수가 먼저 `*(지역 길이)` 에 쓴다: `SeQuerySecurityDescriptorInfo +0x1a1` MOV [RAX],EDI (비교 `+0x1ab` 보다 앞이라 성공·실패 둘 다). 객체 SD 가 NULL 이면 `+0x435` (`0x14`).
- **`*LengthNeeded` 를 안 쓰는 경우**: `ObReferenceObjectByHandle` 가 실패했을 때 (`+0x14c` JS → `+0x200`): 잘못된 핸들 `0xC0000008`, 권한 모자람 `0xC0000022`.
- **유저 포인터 검사 순서** (`NtQuerySecurityObject.asm`; 이전 모드가 유저일 때만, `+0x24`/`+0x2d` 에서 `GS:[0x188]+0x232` 를 읽고 `+0x35`/`+0x38` JZ → `+0x212` 로 건너뜀):
  1. `+0x3e`~`+0x5c`: **`LengthNeeded` 포인터** 를 `min(포인터, 0x7fffffff0000)` 로 눌러서 4바이트 **읽고-다시 쓰기**. 정렬 검사 없음. NULL 이면 주소 0 을 건드려서 접근 위반.
  2. `+0x5e`/`+0x62`/`+0x65`: **길이가 0 이면 버퍼 검사를 통째로 건너뜀** (JZ → `+0xb4`). 그래서 (NULL, 0) 이 통과한다.
  3. 길이가 0 이 아니면: `+0x6a`/`+0x6d` 버퍼가 4 바이트 맞춤이 아니면 `ExRaiseDatatypeMisalignment` (`+0xae`/`+0xb3`), `+0x73`~`+0x7e` 끝 주소가 넘치거나 `0x7fffffff0000` 이상이면 `ExRaiseAccessViolation` (`+0xa8`/`+0xad`), 아니면 `+0x90`~`+0xa4` 4KB 페이지마다 1바이트 읽고-다시 쓰기.
  - 이 검사 전체가 첫 번째 `__try` (`+0x3e`~`+0xb7`, 착지점 `+0xb7`, 거기서 바로 `+0x200` 끝맺음으로 감). 착지점에서 EAX 를 따로 안 채우므로 돌려주는 값 = 예외 코드 `[추측]` (`__C_specific_handler` 가 풀 때 EAX 로 넘기는 값; 정적으로 못 확인, 동적 확인 4).
- 길이 0 · 작은 길이로 불러도 (기본 함수를 쓰는 Directory·Token 에서는) **status 가 0 으로 나오는 길은 없다**: 필요 길이는 항상 ≥ `0x14` (`+0xee`) 이고 비교는 `JA` 라서, 받은 길이가 0 이면 반드시 `0xC0000023`.

**지금 우리**: "성공 0 + 길이 칸 안 씀" 은 **틀림.** 진짜는 `0xC0000023` 이고 `*LengthNeeded` 를 쓴다 (≥ `0x14`). 에뮬레이터가 이렇게 못 해서 부른 쪽이 길이를 못 얻어 실패했다 (`UuidToStringA` → `0xe`). 고칠 곳: 크기만/작음 둘 다 `0xC0000023` + 필요 길이 쓰기, 충분일 때만 0 + 바이트.

### 질문 5. 형마다 `SecurityProcedure` — Directory 와 Token

**갈림길 (`NtQuerySecurityObject`)**: 핸들 → 객체 → 헤더 쿠키로 형 객체 찾기 (`+0x152`~`+0x17d`, `ObHeaderCookie`·`ObTypeIndexTable`) → **형 객체 `+0x98` = `SecurityProcedure`** (`+0x181` MOV R10,[RAX+0x98]) 을 읽고 주소를 비교한다.
- `== SeDefaultObjectMethod` (`+0x194` LEA, `+0x1c2` CMP, `+0x1c5` JNZ) → 직접 호출 `+0x1d4/+0x1d9`.
- `== IopGetSetSecurityObject` (`+0x21f`~`+0x229`) → `+0x238/+0x23d` (파일·장치).
- `== CmpSecurityMethod` (`+0x23f`~`+0x246`) → `+0x258/+0x25d` (레지스트리 키).
- 그 밖 → CFG 간접 호출 `+0x265/+0x26a` (`indirect-cfg`, 대상은 실행 때. 예: `WmipSecurityMethod`).
- 네 갈래 모두 같은 인자 모양 (`EDX = 1` = 질문 연산 `+0x1bd`, `SecurityInformation` 포인터, 버퍼, 길이 포인터).

**`SeDefaultObjectMethod` 안** (`.asm` 앞쪽 연산 분기): `EDX==2` 삭제 (`+0x1c`/`+0x1f`), `0` 쓰기 (`+0x8c`/`+0x8e`), **`1` 질문 → `+0x7d9`**, `3` 새로 달기, 그 밖은 `KeBugCheckEx` (`+0xa00`~`+0xa17`). 질문 갈래: `+0x7dd/+0x7e2` `ObpReferenceSecurityDescriptor` (헤더 `-0x30` 의 SD 를 빌림) → `+0x7fd/+0x802` **`SeQuerySecurityDescriptorInfo` 를 직접 부름** → `+0x809`~`+0x831` 빌린 참조 되돌리기 (또는 `+0x9f6/+0x9fb` `ObDereferenceSecurityDescriptor`). **`ObQuerySecurityDescriptorInfo` 를 거치지 않는다.** 쓰기·삭제·새로 달기 갈래는 이름만 (`RtlpSetSecurityObject` `+0x53f/+0x544`, `ObLogSecurityDescriptor` `+0x55b/+0x560`, `PspChargeQuota` `+0x63a/+0x63f`, `PsReturnSharedPoolQuota` `+0x66f/+0x674`).

**나머지 함수가 길 밖인 이유** (`.c` 맨 위 caller 줄):
- `ObQuerySecurityDescriptorInfo` (`+0x22` 참조 빌리기 → `+0x3d/+0x42` `SeQuerySecurityDescriptorInfo` → `+0x87/+0x8c` 되돌리기, 얇은 포장): caller = `WmipSecurityMethod` 와 이름 없는 `FUN_140b2a1cc`. 즉 자기 보안 함수를 따로 가진 형들이 쓰는 도우미. Directory·Token 은 안 쓴다.
- `ObpGetObjectSecurity` (caller `ObpCheckTraverseAccess` · `ObpCheckObjectReference` · `ObpInsertOrLocateNamedObject` · `ObCheckCreateObjectAccess` · `MiAllowImageMap` 등 접근 검사 쪽): **같은 비교를 따로 한다** (`+0x4d` LEA / `+0x59` CMP [RBP+0x98],RAX / `+0x60` JNZ). 같으면 형의 함수를 안 부르고 헤더의 SD 를 직접 읽고 (`+0x62`~`+0x88`), 다르면 `SecurityInformation = 0x1bf` (`+0xdc`) 로 형의 질문 함수를 부른다 (`CmpSecurityMethod` `+0x15b`, `IopGetSetSecurityObject` `+0x1fa`, 그 밖 `+0x290`/`+0x2ad` 간접). 유저의 `NtQuerySecurityObject` 길이 아니다.

**형 → `SecurityProcedure` 표**:

| 형 | 형 만드는 자리 | 초기화 구조체의 `+0x58` (`SecurityProcedure`) | 형 객체 `+0x98` 최종값 |
|---|---|---|---|
| **Directory** (`ObpDirectoryObjectType`) | `ObInitSystem +0x2e4`/`+0x2e9` `ObCreateObjectType` | **안 채움 (0).** 구조체를 `+0x223` `memset(0, 0x78)` 로 지운 뒤 `+0x40` (`ObpCloseDirectoryObject`) · `+0x48` (`ObpDeleteDirectoryObject`) · `+0x1c` 허용마스크 · `+0x2c` 만 채움 (`+0x2a9`~`+0x2df`) | **`SeDefaultObjectMethod`** |
| **Token** (`SeTokenObjectType`) | `SepTokenInitialization +0x8d`/`+0x92` `ObCreateObjectType` (`SepInitializationPhase0 +0x18`/`+0x1d` 에서 부름) | **안 채움 (0).** `memset(0, 0x80)` (`+0x37`) 뒤 `+2` `+4` `+8` `+0xc` `+0x1c` `+0x24` `+0x48` (`SepTokenDeleteMethod`) `+0x78` 만 채움 (`+0x43`~`+0x8a`) | **`SeDefaultObjectMethod`** |

- 기본값 채우는 자리: `ObCreateObjectTypeEx`(`ObCreateObjectType` 는 `+0xc` 에서 이걸 부르는 22바이트 얇은 껍데기) `+0x42e`~`+0x452` 초기화 구조체를 형 객체 `+0x40` 으로 복사 → `+0x4af` CMP [RDI+0x58],0 / `+0x4b3` JNZ → `+0x4b5` LEA RAX,[SeDefaultObjectMethod] / **`+0x4bc` MOV [RBX+0x98],RAX**. 넘긴 값이 0 이면 기본 함수를 넣는 규칙.
- **Directory 와 Token 은 같은 길을 탄다 = 예** (둘 다 `SeDefaultObjectMethod`). 그래서 에뮬레이터는 형별로 갈라 만들 필요가 없다. 위는 "형 만들 때 값" 까지이고, 만든 뒤 다른 곳에서 `+0x98` 을 덮어쓰는지는 안 봤다 → 동적 확인 2.

**지금 우리**: 형별 분기는 필요 없음, 한 구현이면 맞다 (맞음). 단 "다른 형" 은 파일·키가 따로 있다는 것만 알아 두면 된다.

### 질문 6. 토큰 객체 SD 를 붙이는 자리 (이름·줄만)

- **만드는 자리**: 복제 = `SepDuplicateToken +0x137` (`SeTokenObjectType` 읽기) → `+0x158/+0x15d` `ObCreateObjectEx`. 새로 만들기 = `SepCreateTokenEx +0x5b1` → `+0x5dc/+0x5e1` `ObCreateObject`.
- **SD 를 주나**: 둘 다 `ObjectAttributes` 의 SD 를 먼저 확인한다 (`NtDuplicateToken +0xb2/+0xb7`, `SepCreateTokenEx +0x118/+0x11d` `SeCaptureObjectAttributeSecurityDescriptorPresent`). 넣는 건 `ObInsertObjectEx` (`NtDuplicateToken +0x1f8/+0x1fd`) / `ObInsertObject` (`SepCreateTokenEx +0xbd7`·`+0xc3a`). `ObInsertObjectEx` 가 그 안에서 **`SeAssignSecurityEx2` 로 SD 를 계산** (`+0x325/+0x32a`, 디렉터리 형이면 `+0x2df`/`+0x2ea` 로 플래그만 다르게 감) 해서 형 함수에 새로 달기 (`SeDefaultObjectMethod` `+0x38c/+0x391`, 형 함수가 기본이 아니면 `+0x7e3` 간접) 로 건넨다. 즉 **형 기본 SD 가 아니라 만드는 쪽 SD (+ 만든 사람 토큰으로 계산)**. `ObpCreateDefaultObjectTypeSD` (`.c` 뽑음) 는 형 객체 자신의 SD 용이라 토큰 객체와 상관없음.
- **라벨 ACE 가 붙을 여지**:
  - `NtDuplicateToken` 은 `ObjectAttributes` 에 SD 가 **없었을 때** (`+0x3a` MOV [RSP+0x43],AL, 검사 `+0x203` CMP) 넣은 다음 `SepAppendAceToTokenObjectAcl(토큰, 8, SeAliasAdminsSid)` (`+0x21e/+0x223`, 토큰 객체 SD 에 ACE 하나 덧붙임. DACL 인지는 안 읽음 `[추측]`) → `SepSetProcessTrustLabelAceForToken` (`+0x338/+0x33d`) 를 부른다.
  - 뒤의 함수는 `RtlAddProcessTrustLabelAce` (`SepSetProcessTrustLabelAceForToken +0x140/+0x145`) 로 SACL 을 만들어 `ObSetSecurityObjectByPointer` (`+0x196/+0x19b`) 로 건다. 그 함수는 ACE 형 **`0x14`** 만 받는다 (`RtlAddProcessTrustLabelAce +0x3c` CMP [RSP+0x60],0x14). 즉 **붙는 건 `0x14` (PROCESS_TRUST_LABEL), `0x11` 아님.**
  - `0x11` 이 붙는지는 `SeAssignSecurityEx2` / `RtlpNewSecurityObject` 안일 텐데 이번에 안 읽었다 `[공백]`. 값은 VM 몫.
  - 마스크 `0x10` 만으로는 `0x14` 가 안 간다 (질문 2), 그러니 이 SACL 은 `0x17` 질문에는 안 나오고 `0x8`/`0x80`/`0x10000` 에서 나온다.

## 에뮬레이터용 규칙 요약 (질문 2·3·4 한 장)

1. 입구 (유저): `LengthNeeded` 포인터 읽고-쓰기 검사 → 길이≠0 일 때만 버퍼 4바이트 맞춤·범위 검사.
2. 권한: 질문 1 표로 `DesiredAccess` 계산 → 핸들 권한에 없으면 `0xC0000022` (그리고 `*LengthNeeded` 안 씀).
3. 객체 SD 없음 → 20 바이트 `{01 00 00 80, 0 ×16}`, 필요 길이 `0x14`.
4. `needed = 0x14 + Owner(요청 시) + Group(요청 시) + DACL (`(AclSize+3)&~3`) + SACL ((8+선택 ACE 합+3)&~3)`.
5. `needed > 받은 길이` → `0xC0000023` (버퍼 안 건드림). 어느 쪽이든 `*LengthNeeded = needed` (핸들 성공 뒤).
6. 채우기: 머리 `Revision 1`, `Control` 시작값 `0x8000`; 차례 Owner → Group → DACL → SACL; 칸 오프셋 `+4 +8 +0x10 +0xc`; `Control` 에 `0x1`/`0x2`/`0x140c`/`0x2830` 를 요청된 구성요소에 맞춰 객체 값에서 옮김.
7. SACL 고르기: 마스크 `0x1f8` 와 ACE 형 표 (질문 2). 마스크 `0x10` → 형 `0x11` 만. 머리 `AclRevision`/`Sbz1`/`Sbz2` 는 객체 것, `AclSize = 8+Σ`, `AceCount = 개수`.

## 동적으로 확인할 것 (Q181 에 넘길 멈춤점)

정적으로 못 정한 것: 진짜 객체의 SD 바이트, 형 객체 `+0x98` 실제 값, 예외 코드 반환, 간접 호출 대상.

1. **SD 바이트 뜨기** (`\RPC Control` 디렉터리 객체, test.exe 프로세스 토큰 객체): 객체 헤더 SD 칸 = 객체 주소 `-8` 의 값에서 아래 4비트를 뗀 포인터. `!sd (poi(<객체>-8) & 0xfffffffffffffff0)`. 볼 것: `Control` 에 `SE_SACL_PRESENT` 가 있나, SACL 안에 `0x11` ACE 가 있나 (있으면 `AceFlags` · `Mask` · SID 의 `S-1-16-…`), 소유자·그룹·DACL 크기. 이게 질문 2 (a)/(b) 중 어느 쪽인지 정한다.
   - 추천 멈춤점: `nt!SeQuerySecurityDescriptorInfo+0x3f` (이 줄 직후 `rax` = 객체 SD 포인터, `+0x3c` MOV RAX,[R9] 다음), 조건 = test.exe 프로세스.
2. **`SecurityProcedure` 실제 값**: `dq poi(nt!ObpDirectoryObjectType)+0x98 L1`, `dq poi(nt!SeTokenObjectType)+0x98 L1`, `x nt!SeDefaultObjectMethod` 와 같은지 (같으면 질문 5 확정).
3. **진짜 응답 대조**: `nt!NtQuerySecurityObject+0x1d9` (`SeDefaultObjectMethod` 에서 돌아온 직후, `eax` = status), `+0x1e2` (`*LengthNeeded` 쓰기 직전, `eax` = 필요 길이, `rdi` = 유저 포인터). `0xC0000023` 과 길이가 우리 공식 `0x14 + …` 과 같은가.
4. **충분한 버퍼일 때 만든 바이트**: `nt!SeQuerySecurityDescriptorInfo+0x46e` (성공 끝, 이때 `rsi` = 출력 버퍼, `rdi` = 쓰고 난 끝 위치) 에서 `db @rsi L?(@rdi-@rsi)`. 차례 (Owner → Group → DACL → SACL) 와 `Control` 이 표와 맞나.
5. **라벨 고르기**: `nt!RtlpFilterSacl` 입구 (`rcx` = SACL, `r9d` = 마스크; `0x17` 질문이면 `0x10`) 와 `+0x97` (복사 시작). 돌아온 뒤 `SeQuerySecurityDescriptorInfo+0x325` 에서 `[RSP+0x24]` = SACL 자리 크기.
6. **예외 코드 반환** (정적 `[추측]`): 유저 쪽 `LengthNeeded` 를 일부러 잘못된 포인터로 불러서 status 가 `0xC0000005` 인지 (입구 `+0x3e`~`+0x5c` 근처).
7. **권한 실패**: `nt!ObpReferenceObjectByHandleWithTag+0x516` (JNZ 앞, `ebp` = DesiredAccess, `eax` = ~가진권한) — `0x20001` 핸들에 `0x17` 이 통과하는지 눈으로.

## 다음 요청 후보

- `SeAssignSecurityEx2` / `RtlpNewSecurityObject` (depth 1): 새 객체 SD 에 `0x11` 라벨 ACE 를 기본으로 붙이는지 (질문 6 의 `[공백]`). smss 가 `\RPC Control` 을 만들 때 SD 를 직접 주는지는 sast/94 결과에 달림.
- `NtOpenDirectoryObject` 로 `\RPC Control` 을 `0x20001` 로 열 때 접근 검사 (`ObpCheckObjectReference`·`ObpGetObjectSecurity` 쪽, `SeAccessCheck` 는 이름만): 에뮬레이터가 핸들에 `0x20001` 을 주는 조건.
- (낮음) `NtSetSecurityObject` 쓰기 길 (이번 범위 밖).

## 판정

**분석성공.** 여섯 질문 모두 `.asm` 줄 근거로 답했다. 정적으로 못 정한 것은 위 "동적으로 확인할 것" 1~7 (객체 SD 값, 형 `+0x98` 실제 값, 예외 코드 반환 값 등).

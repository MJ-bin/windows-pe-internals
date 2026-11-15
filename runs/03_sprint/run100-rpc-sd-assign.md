# 2026-11-15 run100 — 넘긴 SD 가 새 객체에 붙을 때 커널이 바꾸는 것 (`NtCreateDirectoryObject` → `ObpCreateDirectoryObject` → `ObInsertObjectEx` → `ObpCreateHandle` → `ObpInsertOrLocateNamedObject` → `ObpAssignSecurity` → `SeAssignSecurityEx2` → `RtlpNewSecurityObject` → `RtlpInheritAcl2` → `RtlpCopyAces`, 저장은 `SeDefaultObjectMethod` (EDX=3)) (Q180d = Q180 의 마지막 조각, PE_emulator 요청 34 의 1 · 2 를 "바이트 규칙" 으로 잇기 — 정적만): **이름 있는 객체 (`\RPC Control`) 는 `ObInsertObjectEx` 가 SD 를 직접 계산하지 않는다 — `+0x443 ObpCreateHandle` → `+0x13bd ObpInsertOrLocateNamedObject` → `+0x1fe ObpAssignSecurity` → `+0xd9 SeAssignSecurityEx2` → `+0x65 RtlpNewSecurityObject` 를 거친다 (요청서가 가정한 `ObInsertObjectEx → ObpAssignSecurity` 직행은 없고, `ObInsertObjectEx +0x325 SeAssignSecurityEx2` 는 이름 없는 객체 (토큰 등) 길). smss 가 비운 소유자 · 그룹은 **만든 쪽 토큰**에서 채운다 — 소유자 = `UserAndGroups[DefaultOwnerIndex]` (토큰 `+0x98` · `+0x90`), 그룹 = `PrimaryGroup` (`+0xa8`), `*_DEFAULTED` 비트는 안 켠다 (SID 값은 VM). DACL 은 ACE 순서 · 개수 그대로 두고, **상속 전용 (`0x08`) 이 아닌 ACE 셋만** generic 을 Directory `GenericMapping {0x20003, 0x2000c, 0x20003, 0xf000f}` 로 바꿔 `& 0xf000f` → `0x2000f` · `0x2000f` · `0xf000f`. 상속 전용 넷은 마스크 · 플래그 그대로. `AclSize` 는 다시 잰다 (`0xb8 → 0x9c`, SACL `0x20 → 0x1c`). 라벨 ACE 는 바이트 그대로. 부모 (루트 `\`) 의 ACE 는 안 섞인다 (자동 상속 꺼짐). `Control` = **`0x8814`** (`SE_SACL_AUTO_INHERITED 0x800` 이 라벨 갈래에서 켜진다 — 이 바퀴에서 확신이 가장 낮은 한 곳). 저장 순서는 머리 → SACL → DACL → 소유자 → 그룹 (묻기 출력 순서와 다름), 총 `0xe8` 바이트 (소유자 16B · 그룹 12B 가정). 그래서 `NtQuerySecurityObject(0x17)` 대답은 `0xe8` 바이트, `0x10` 대답은 `0x30` 바이트. 토큰 객체는 형의 `SeMandatoryLabelMask = 1` 이라 **라벨 `0x11` (마스크 1, SID = 만든 쪽 무결성) 이 조건 없이 붙는다** — run97 의 `[공백]` 이 닫혔다.**

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### Q180d 결론 (run100)

- **객체를 만들 때 SD 를 한 번 계산해 붙인다** `[정적]` — 이름이 있든 없든 결국 `SeAssignSecurityEx2 → RtlpNewSecurityObject` 하나다. 입력은 다섯: 부모 SD · 넘긴 (명시) SD · 만든 쪽 토큰 · 형 `GenericMapping` · `IsDirectoryObject`. 형마다 따로 만들 것은 없다 (Directory · Token 둘 다 `SeDefaultObjectMethod`, run97).
- **smss 의 Liberal SD 는 "그대로" 저장되지 않는다** `[정적]` — 바뀌는 것 다섯: 소유자 · 그룹이 생김, ACE 0 ~ 2 의 마스크가 구체 권한이 됨, `AclSize` 두 개가 줄어듦, `Control` 에 `0x8000` · `0x800` 이 더해짐, 저장 순서가 바뀜 (SACL 이 맨 앞). 바뀌지 않는 것: ACE 개수 · 순서 · 플래그, 상속 전용 ACE 넷의 마스크, 라벨 ACE 전체.
- **`0x17` 대답 = 아래 `0xe8` 바이트, 첫 크기 묻기 = `0xC0000023` + `*LengthNeeded = 0xe8`** `[정적]` — run97 묻기 규칙 + 이 바퀴 저장 표. 소유자 · 그룹 28 바이트만 `[VM]` (길이가 다르면 `Group` · `Dacl` · `Sacl` 오프셋과 총 길이가 따라 움직임).
- **토큰 객체 SD 에도 라벨이 있다** `[정적]` — `{0x11, 플래그 0, 마스크 1, 만든 쪽 무결성 SID}` + DACL = 만든 쪽 토큰 `DefaultDacl` 을 Token `GenericMapping` 으로 바꾼 것. `NtDuplicateToken` 이 뒤에 더 붙이는 ACE (`SepAppendAceToTokenObjectAcl` · `0x14`) 는 이번에 안 읽음.

### `\RPC Control` 객체에 붙는 SD (결과서 질문 7 — 저장 순서)

입력 = run98 Liberal SD (소유자 · 그룹 없음, `Control 0x14`) · 만든 이 = smss 토큰 · 형 = Directory · 부모 = 루트 `\` · 자동 상속 0.

| 자리 | 칸 | 값 | 근거 |
|---|---|---|---|
| `+0x0` | `Revision` · `Sbz1` | `1` · `0` | `RtlpNewSecurityObject +0x1250 MOV byte [R14],1` |
| `+0x2` | `Control` | **`0x8814`** | 아래 질문 5 |
| `+0x4` | `Owner` 오프셋 | `0xcc` | `+0x985` |
| `+0x8` | `Group` 오프셋 | `0xcc + 소유자 크기` (가정 `0xdc`) | `+0x9a3` |
| `+0xc` | `Sacl` 오프셋 | `0x14` | `+0x123c` · `+0x136f` |
| `+0x10` | `Dacl` 오프셋 | `0x30` | `+0x962` |
| `0x14 ~ 0x2f` | SACL 28B | rev 2 · `AclSize 0x1c` · `AceCount 1` + 라벨 ACE `{0x11, 0, 0x14, 마스크 1, S-1-16-4096}` | 질문 4 |
| `0x30 ~ 0xcb` | DACL 156B | rev 2 · `AclSize 0x9c` · `AceCount 7` + 아래 ACE 7개 | 질문 3 |
| `0xcc ~` | 소유자 | `[VM]` (`[추측]` `S-1-5-32-544`, 16B) | 질문 2 |
| 그 뒤 | 그룹 | `[VM]` (`[추측]` `S-1-5-18`, 12B) | 질문 2 |
| — | 총 길이 | `0xcc + 소유자 + 그룹` (가정 **`0xe8`**) | `SeDefaultObjectMethod +0xaf ~ +0x161` 크기 식과 같음 |

| # | SID | 플래그 | smss 가 넘긴 마스크 | 상속 전용? | 저장되는 마스크 |
|---|---|---|---|---|---|
| 0 | `S-1-1-0` | 0 | `0xE0000000` | 아니오 | **`0x2000f`** |
| 1 | `S-1-5-12` | 0 | `0xE0000000` | 아니오 | **`0x2000f`** |
| 2 | `S-1-5-32-544` | 0 | `0x10000000` | 아니오 | **`0xf000f`** |
| 3 | `S-1-1-0` | `0x0b` | `0xE0000000` | 예 | `0xE0000000` 그대로 |
| 4 | `S-1-5-12` | `0x0b` | `0xE0000000` | 예 | `0xE0000000` 그대로 |
| 5 | `S-1-5-32-544` | `0x0b` | `0x10000000` | 예 | `0x10000000` 그대로 |
| 6 | `S-1-3-0` | `0x0b` | `0x10000000` | 예 | `0x10000000` 그대로 |

- 저장 바이트 전체 (hex 16줄) 는 결과서 질문 7 "저장된 SD 바이트". 표 값은 `ms/` 에서 읽은 값 (smss `.asm` SID 줄, `ObpDirectoryMapping` 파일 바이트) + 규칙으로 만든 것이라 `[정적]`.
- 같은 바이트의 SD 는 캐시 항목 하나를 같이 쓴다 (`SeDefaultObjectMethod` EDX=3, 아래). `\Windows` 가 처음 받는 SD 도 같은 smss SD 객체라 처음엔 같은 항목일 것 `[추측: 같은 입력 · 같은 만든 이면]`.

### 에뮬레이터가 돌려줄 대답 바이트 (결과서 질문 7 옮김)

**`NtQuerySecurityObject(h, 0x17, …)`** — run97 규칙 (머리 → Owner → Group → DACL → SACL, LABEL 만 물어서 SACL 은 형 `0x11` 만). 길이 `0xe8`, `Control 0x8814`, 오프셋 Owner `0x14` · Group `0x24` · Dacl `0x30` · Sacl `0xcc`. `[ ]` 칸 = `[VM]` (소유자 `0x14~0x23`, 그룹 `0x24~0x2f`, 가정 값 `S-1-5-32-544` · `S-1-5-18`).

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

- 첫 부름 (버퍼 NULL · 길이 0) = `0xC0000023` + `*LengthNeeded = 0xe8` (run97 · run99 최소 조건 3).

**`NtQuerySecurityObject(h, 0x10, …)`** (LABEL 만) — 길이 `0x30`, `Control 0x8810`, `Sacl` 오프셋 `0x14`, 나머지 오프셋 0. `[VM]` 칸 없음.

```
0000  01 00 10 88 00 00 00 00 00 00 00 00 14 00 00 00
0010  00 00 00 00 02 00 1c 00 01 00 00 00 11 00 14 00
0020  01 00 00 00 01 01 00 00 00 00 00 10 00 10 00 00
```

- flow-writer 가 칸 합을 다시 셈: DACL ACE `20+20+24+20+20+24+20 = 148` + 머리 8 = `0x9c`, SACL `8 + 20 = 0x1c`, `0x14 + 16 + 12 + 0x9c + 0x1c = 0xe8`, `0x14 + 0x1c = 0x30`. 결과서와 같음.

### 에뮬레이터용 규칙 한 장 (결과서 "에뮬레이터용 규칙 한 장" 옮김, 질문 1 ~ 6)

1. 객체를 만들 때 (이름 있든 없든) `AssignSecurity(부모SD, 명시SD, 만든토큰, GenericMapping, IsDirectory)` 를 한 번 부르고, 결과를 자기상대형으로 객체에 건다. 같은 바이트면 같이 써도 된다 (캐시).
2. 명시 소유자가 없으면 토큰 `UserAndGroups[DefaultOwnerIndex]`, 명시 그룹이 없으면 토큰 `PrimaryGroup`. `*_DEFAULTED` 는 안 켠다.
3. DACL: 명시 DACL 이 있고 자동 상속이 꺼졌으면 **명시 ACE 만** 순서대로. 상속 전용 (`0x08`) 이 아닌 ACE 만 generic → `GenericMapping` 후 `& GenericAll` (감사 · 경보 형은 `GenericAll | 0x1000000`). `AclSize = 8 + ACE 크기 합`, `Control |= 0x4`. 명시 DACL 이 없으면 부모에서 상속 가능한 ACE 를 만들고 (이번엔 안 푼 길), 만들 게 없으면 토큰 `DefaultDacl`.
4. SACL: 명시 라벨 (`0x11`) 은 그대로 (플래그가 `INHERIT_ONLY` 뿐이거나 `INHERITED` 면 무시, 마스크에 `& ~7` 비트 · 플래그에 `& ~0x1f` 비트 · SID 권한 ≠ 16 이면 `0xC000000D`, 만든 쪽 무결성이 라벨을 못 지배하면 `SeRelabelPrivilege` 필요). 감사 ACE 가 있으면 `SeSecurityPrivilege` 필요 (없으면 `0xC0000061`). 라벨이 없고 형 마스크 ≠ 0 이면 `{형 마스크, 만든 쪽 무결성 SID}`, 형 마스크 0 이고 만든 쪽 무결성 < Medium (`0x2000`) 이면 `{마스크 1, 만든 쪽 무결성 SID}`, 그 밖엔 안 붙임.
5. 라벨 갈래가 돌면 `Control |= 0x810` (`SACL_PRESENT | SACL_AUTO_INHERITED`). 최종 `Control = 0x8000 | DACL 비트 | SACL 비트`.
6. 토큰 형: 형 마스크 1 → 토큰 SD 에 늘 `{0x11, 0, 마스크 1, 만든 쪽 무결성 SID}` + 기본 DACL.
7. 저장 순서 머리 → SACL → DACL → 소유자 → 그룹. 묻기 출력은 머리 → 소유자 → 그룹 → DACL → SACL (run97).

### 지금 PE_emulator 맞나 · 틀리나 (결과서 "지금 우리" 줄 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

| 질문 | 진짜 (26100.9457) `[정적]` | 지금 우리 (결과서가 원문 요청에서 옮긴 것) | 판정 |
|---|---|---|---|
| 1 SD 붙이는 길 | 객체 만들 때 한 번 계산 (위 규칙 1) | 객체 만들 때 SD 를 붙이는 길이 없음 (`namespace.cpp:21`) | **모자람** |
| 2 소유자 · 그룹 | 만든 쪽 토큰에서, `DEFAULTED` 안 켬 | 칸은 있음 (`handles.hpp:14`), 채우는 규칙 없음 | **모자람** |
| 3 DACL | 비-상속전용 ACE 만 generic 변환, `AclSize` 다시 잼 | generic 변환 없음 | **틀림** |
| 4 SACL · 라벨 | 명시 라벨 그대로 · 없으면 규칙 4 표 | 객체 SD 에 SACL · 라벨 칸 없음 | **틀림** |
| 5 `Control` | `0x8814` | (원문에 없음) | **모름** — `0x8814` 로 만들면 맞음 (VM 대조 1순위) |
| 6 토큰 라벨 | 늘 붙음 | 토큰 SD 에 라벨 없음 (샘플 53개가 `GetSecurityInfo(토큰, 6, 0x10)` 에서 못 받음 — PE_emulator 쪽 관찰) | **틀림** |
| 7 대답 바이트 | `0x17` → `0xe8` B, `0x10` → `0x30` B | `\RPC Control` 에 SD 안 채움 | **틀림** — 위 바이트 |

### 요청서 질문이 어디서 답해졌나

| 요청서 질문 (sast/96) | 답한 곳 (아래 절) | 한 줄 |
|---|---|---|
| 1 사슬 | "사슬 — 이름 있는 객체는 한 칸 더 돈다" | `ObpCreateHandle` · `ObpInsertOrLocateNamedObject` 를 거침. `SeDefaultObjectMethod` Assign 갈래 (EDX=3) 가 끝에 낌 |
| 2 소유자 · 그룹 | "소유자 · 그룹" | 만든 쪽 토큰 칸 `+0x90` · `+0x98` · `+0xa8`, 값은 VM |
| 3 DACL | "DACL" | 비-상속전용만 변환, 상속 전용 그대로, `AclSize` `0x9c`, 부모 안 섞임, 매핑 숫자 넷 |
| 4 SACL · 라벨 | "SACL · 라벨" | 라벨 그대로, 라벨만이면 `SeSecurityPrivilege` 안 물음, 기본 라벨 조건 표 |
| 5 `Control` | "`Control`" | `0x8814` |
| 6 토큰 | "토큰 객체" | 라벨 `0x11` 늘 붙음 |
| 7 바이트 | 위 두 절 | 저장 `0xe8`, `0x17` `0xe8`, `0x10` `0x30` |

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 사진 대조 없음 — 객체 헤더 SD · 형 객체 칸은 run44 사진에 칸이 없고, `\RPC Control` 은 부팅 때 smss 가 만든 다른 프로세스의 일이다 (run98). 사진도 다시 안 읽었다.
- 정적: **ntoskrnl pdb (`ntkrnlmp.pdb`) `C29EBFB06B78B3C020DCA66D99713F9E1`** (기준과 같음, `ms/sym/ntoskrnl.exe.pdb.info.txt`). 다시 analyze 안 함 — 읽기 전용 export 와 `refs` 만 (결과서).
  - 버전 대조 (결과서): `_calls.tsv` 의 `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 그대로.
  - 구조체 칸은 `pdb_struct.py ntoskrnl.exe _TOKEN _OBJECT_TYPE _OBJECT_TYPE_INITIALIZER` (결과서): 토큰 `+0x90 DefaultOwnerIndex` · `+0x98 UserAndGroups` · `+0xa8 PrimaryGroup` · `+0xb8 DefaultDacl` · `+0xd0 IntegrityLevelIndex`, 형 `+0xd8 SeMandatoryLabelMask` · `+0xdc SeTrustConstraintMask`.
  - 파일 값 (결과서, PE 바이트): `ObpDirectoryMapping` (rva `0xc7fe70`) · `SepTokenMapping` (`0xb496d0`) 16 바이트씩, `ObpObjectSecurityInheritance` (rva `0xe66e08`) `.data` 초기값 0.
- 다른 decompiler 가 같은 시간에 ntoskrnl (sast/101) · ntdll (sast/102) 을 돌리는 중이다. 그 결과는 이 문서에 안 넣었다.
- 에뮬레이터 쪽 숫자 (샘플 53개, `namespace.cpp:21` · `handles.hpp:14`, 원문 요청) 는 **PE_emulator 쪽 관찰**이라 이 문서의 `[관찰]` 이 아니다.

## 목적

- PE_emulator 요청 34 (원문 `../PE_emulator/_workspace/05_tuning_planner/264_internals-rpc-security.md`) 의 마지막 조각 **Q180d**. run97 (묻기 출력 규칙) 과 run98 (smss 가 넘기는 Liberal SD) 사이의 빈칸 — **넘긴 SD 가 객체에 붙을 때 무엇이 바뀌나** (소유자 · 그룹 채우기, generic → 구체 권한, 상속 전용 ACE, `AclSize`, SACL · 라벨, `Control`) 를 정해서 `\RPC Control` 의 대답 바이트를 다시 만든다. 같은 함수로 토큰 객체 라벨 (run97 `[공백]`) 도 닫는다.
- 범위 밖 (요청서): 접근 검사 (`SeAccessCheck`), 감사, `NtSetSecurityObject` 길 — 이름만. `RtlpNewSecurityObject` 의 다른 갈래 (자식 상속 계산 전부, 객체 형 GUID ACE, 조건부 ACE, 리소스 속성) 는 갈림 줄만.
- 앞 바퀴 짝: run97 (Q180a 커널 묻기), run98 (Q180b smss SD), run99 (Q180c rpcrt4).

## 방법

- 정적: `_workspace/sast/96_rpc-sd-assign.md` — ntoskrnl (다시 analyze 안 함)
  - depth 1: `^(SeAssignSecurityEx2|SeAssignSecurityEx|SeAssignSecurity|RtlpNewSecurityObject)$` → 31개 (callee 포함).
  - depth 0 (요청서 대상): `RtlpInheritAcl2` · `ObpCreateDirectoryObject` · `NtCreateDirectoryObject` · `NtCreateDirectoryObjectEx` · `ObpAssignSecurity` · `ObInsertObjectEx` · `SeDefaultObjectMethod` · `ObInitSystem`.
  - 범위를 넓힌 곳 (depth 0, 결과서): `ObpCreateHandle` · `ObpInsertOrLocateNamedObject` · `ObAssignSecurity` · `SeComputeAutoInheritByObjectTypeEx` (이름 있는 객체가 다른 길이라), `RtlpCopyAces` · `RtlpGenerateInheritedAce` · `RtlpCopyEffectiveAce` (+ `ObpGetObjectSecurity` · `ObLogSecurityDescriptor` · `RtlMapGenericMask`) (generic 바꾸기가 `RtlpCopyAces` 안이라), `ObpCaptureObjectCreateInformation` · `SeCaptureSecurityDescriptor` (넘긴 SD 를 잡는 곳).
  - `refs`: `ObpObjectSecurityInheritance` (`ms/disassemble/ntoskrnl/_refs/ObpObjectSecurityInheritance.tsv`).
- 결과서: `_workspace/sast/96_rpc-sd-assign_result.md` (**분석성공** — 값이 VM 몫인 것: 소유자 · 그룹 · 토큰 라벨 SID, 루트 `\` SD, `ObObjectSecurityInheritance` 레지스트리 값. 확신이 낮은 한 곳: `Control` 의 `0x800`).
- flow-writer 가 다시 본 것 (`.asm` · `_calls.tsv` · `_map`):
  - 사슬: `ObpCreateDirectoryObject +0x145` · `+0x185` · `+0x18a`, `ObInsertObjectEx +0xde` · `+0xef` · `+0x156` · `+0x279` · `+0x27c` · `+0x282` · `+0x2c8` · `+0x2f0` · `+0x31b` · `+0x325` · `+0x38c` · `+0x443`, `ObpCreateHandle +0x526` · `+0x13bd`, `ObpInsertOrLocateNamedObject +0x117` · `+0x14a` · `+0x150` · `+0x158` · `+0x175` · `+0x1fe` · `+0x44f`, `ObpAssignSecurity +0x4c` · `+0x61` · `+0x83` · `+0x95` · `+0xd9` · `+0x14a` · `+0x1ce`, `SeAssignSecurityEx2 +0x19` · `+0x65`.
  - 계산: `RtlpNewSecurityObject +0x1c5` · `+0x1dc` · `+0x1e3` · `+0x1ea` · `+0x202` · `+0x21d` · `+0x2ad` · `+0x459` · `+0x48f` · `+0x58e` · `+0x598` · `+0x621` · `+0x781` · `+0xbd3` · `+0xe38` · `+0xf73` · `+0xfdc` · `+0xfea` · `+0x1022` · `+0x10c6` · `+0x10d6` · `+0x110f` · `+0x1128` · `+0x116d` · `+0x1172` · `+0x1182` · `+0x124b` · `+0x1250` · `+0x13b3` · `+0x149e` · `+0x1559` · `+0x155e` · `+0x1599` · `+0x1668` · `+0x181e` · `+0x1846` · `+0x1b22` · `+0x1c99` · `+0x1ec3` · `+0x1eed`, `RtlpInheritAcl2 +0xd3` · `+0x134` · `+0x16b` · `+0x1b1` · `+0x1b5` · `+0x1bb` · `+0x208` · `+0x5ab`, `RtlpCopyAces +0x13e` · `+0x14e` · `+0x152` · `+0x15b` · `+0x161` · `+0x16a` · `+0x173` · `+0x17c` · `+0x190` · `+0x1a3` · `+0x1b4` · `+0x2fe`, `SeComputeAutoInheritByObjectTypeEx +0x3f` · `+0x130`.
  - 형 · 저장: `ObInitSystem +0x219` · `+0x2a0` · `+0x2df`, `ObCreateObjectTypeEx +0x45c` · `+0x461` · `+0x464`, `SepTokenInitialization +0x29` · `+0x43` · `+0x4e` · `+0x59` · `+0x8a` · `+0x8d`, `SeDefaultObjectMethod +0x277` · `+0x29f` · `+0x395` · `+0x3a3` · `+0x3a7`.
  - `_map/NtCreateUserProcess_edges.tsv` · `_nodes.tsv` 에서 `ObInsertObjectEx` 의 caller 자리 (아래 "test.exe 길에 있나").
  - 결과서 값과 다른 줄 없음 (다시 본 줄 모두 일치).

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtCreateDirectoryObject` | `+0xc` | `+0x11` | `ObpCreateDirectoryObject` | 22B 껍데기 (`NtCreateDirectoryObjectEx` 도 `+0xc/+0x11`) | [정적] |
| `nt!ObpCreateDirectoryObject` | `+0xe0` | `+0xe5` | `ObCreateObjectEx` | 그 안 `+0xde ObpCaptureObjectCreateInformation` → `+0x260 SeCaptureSecurityDescriptor` 가 넘긴 SD 를 자기상대형 사본으로 잡음 | [정적] |
| 〃 | `+0x18a` | `+0x18f` | `ObInsertObjectEx` | `+0x185 XOR EDX,EDX` (AccessState 0 → `ObInsertObjectEx` 가 만듦) | [정적] |
| `nt!ObInsertObjectEx` (rva `0x87da20`) | **`+0x443`** | **`+0x448`** | **`ObpCreateHandle`** | 형 `+0x42 & 8` (SecurityRequired, `+0xde` · `+0xef`) + 이름 있음 (`+0x279` · `+0x27c JNZ → +0x3ea`) → SD 블록 `+0x28c ~ +0x3e6` 을 건너뜀. 이름도 SD 도 필요 없으면 빠른 길 `+0x156/+0x15b` | [정적] |
| 〃 (이름 없는 객체) | `+0x2c8` | `+0x2cd` | `SeComputeAutoInheritByObjectTypeEx` | `+0x282 TEST byte [RSI+0x42],8` 뒤 | [정적] |
| 〃 (이름 없는 객체) | `+0x325` | `+0x32a` | `SeAssignSecurityEx2` | 부모 NULL, `IsDirectory` 0 (`+0x2f0` · `+0x31b`) — 토큰 길 | [정적] |
| 〃 (이름 없는 객체) | `+0x38c` | `+0x391` | `SeDefaultObjectMethod` | EDX=3 (형 `+0x98` 이 기본 함수일 때, 아니면 `+0x7e3` 간접) | [정적] |
| `nt!ObpCreateHandle` | **`+0x13bd`** | **`+0x13c2`** | **`ObpInsertOrLocateNamedObject`** | 진입 `+0x526 JNZ → +0x139a` (`R13` 뜻은 결과서가 안 풂) | [정적] |
| `nt!ObpInsertOrLocateNamedObject` | `+0x117` | `+0x11c` | `ObpLookupObjectName` | | [정적] |
| 〃 | `+0x175` | `+0x17a` | `ObpGetObjectSecurity` | 부모 디렉터리 SD | [정적] |
| 〃 | **`+0x1fe`** | **`+0x203`** | **`ObpAssignSecurity`** (rva `0x87e330`) | 자동 상속 인자: 부모 플래그 `+0x150` 이 `8` 이거나 (`+0x14a`), `ObpObjectSecurityInheritance != 0` (`+0x158`) 이면 `+0x44f` 에서 3. 우리는 0 | [정적] |
| `nt!ObpAssignSecurity` | `+0x4c` | `+0x51` | `SeComputeAutoInheritByObjectTypeEx` | 형 `+0xd8` (`SeMandatoryLabelMask`) · 명시 SD 를 보고 자동 상속 비트를 냄 — Directory 는 0 (보탬: call 자리) | [정적] |
| 〃 | **`+0xd9`** | **`+0xde`** | **`SeAssignSecurityEx2`** (rva `0x43d980`) | `SubjectContext = AccessState+0x20` (`+0x83`), `GenericMapping = 형+0x4c` (`+0x61`), `IsDirectory = (형 == ObpDirectoryObjectType)` (`+0x95`) | [정적] |
| 〃 | `+0x1ce` | `+0x1d3` | `SeAssignSecurityEx2` | 6번째 인자 ≠ 0 일 때 먼저 도는 보조 호출 — 우리는 0 | [정적] |
| 〃 | **`+0x14a`** | **`+0x14f`** | **`SeDefaultObjectMethod`** | EDX=3 (새로 달기), 형 `+0x98` 비교 (`+0x118 ~ +0x144`, 아니면 `+0x1e3` 간접) | [정적] |
| `nt!SeAssignSecurityEx2` | **`+0x65`** | **`+0x6a`** | **`RtlpNewSecurityObject`** (rva `0x8cea00`, 8746B) | 앞서 `+0x19 CMP [RAX],8` (확장 인자 첫 칸 8, 아니면 `0xC000000D`) | [정적] |
| `nt!SeAssignSecurityEx` | `+0x3a` | `+0x3f` | `SeAssignSecurityEx2` | 껍데기 — 우리 사슬엔 안 쓰임 | [정적] |
| `nt!SeAssignSecurity` | `+0x44` | `+0x49` | `RtlpNewSecurityObject` | 껍데기 (레지스트리 `CmpCreateChild` 쪽, 결과서) | [정적] |
| `nt!RtlpNewSecurityObject` | `+0x21d` | `+0x222` | `SepLocateTokenIntegrity` | 만든 쪽 무결성 SID (없으면 `SepDefaultMandatorySid`) | [정적] |
| 〃 | `+0x2ad` | `+0x2b2` | `ExAllocatePool2` | `0x100` B, 태그 `SeSd` — 토큰 SID · 기본 DACL 사본 한 덩어리 | [정적] |
| 〃 | **`+0x155e`** | **`+0x1563`** | **`RtlpInheritAcl2`** | **모드 3 (라벨)**, 자동 상속 인자 **상수 1** (`+0x1559`) | [정적] |
| 〃 | `+0xf73` | `+0xf78` | `RtlpCombineAcls` | SACL 조각을 형 순서로 모음 | [정적] |
| 〃 | `+0x1022` | `+0x1027` | `RtlSidDominates` | 만든 쪽 무결성 ≥ 라벨 ? (`+0xfea TEST R9B,8` 이 없을 때) | [정적] |
| 〃 | **`+0x1668`** | **`+0x166d`** | **`RtlpInheritAcl2`** | **모드 1 (DACL)**, 자동 상속 = `AutoInheritFlags & 1` = 0 (`+0x1663`) | [정적] |
| 〃 | `+0x181e` | `+0x1823` | `RtlpNewSecurityObject` (재귀) | 부모만으로 한 번 더 — `OWNER RIGHTS` 검사용 (`+0x1128` 등 조건) | [정적] |
| 〃 | `+0x1846` | `+0x184b` | `RtlpOwnerAcesPresent` | 있으면 `+0x1b22 SeAccessCheck` (`WRITE_DAC`) | [정적] |
| 〃 | `+0x1c99` | `+0x1c9e` | `SePrivilegeCheck` | `SeRelabelPrivilege` (라벨 못 지배할 때) | [정적] |
| 〃 | `+0x1ec3` | `+0x1ec8` | `SePrivilegeCheck` | `SeSecurityPrivilege` (비-라벨 SACL 이 있을 때) → 실패 `+0x1eed 0xC0000061` | [정적] |
| 〃 | `+0x1225` | `+0x122a` | `ExAllocatePool2` | 마지막 SD 버퍼 (크기 = 총 길이) | [정적] |
| `nt!RtlpInheritAcl2` | `+0x290` | `+0x295` | `RtlpCopyAces` | 명시 ACE 복사 (모드 2 = 전부) | [정적] |
| 〃 | `+0x441` | `+0x446` | `RtlpGenerateInheritedAce` | 부모 ACE 상속 — 우리 DACL 길엔 안 탐 | [정적] |
| 〃 | `+0x5ab` | `+0x5b0` | `RtlFindAceByType` | 출력에 `0x11` 이 이미 있으면 부모 라벨 안 섞음 | [정적] |
| `nt!RtlpCopyAces` | `+0x13e` | `+0x143` | 복사 함수 (alias, rva `0x6c3640`) | ACE 를 그대로 복사한 뒤 `+0x146 ~ +0x1a5` 에서 마스크 변환 (아래). 같은 주소 이름 4개, 후보: `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove` (run97 ③ 과 같은 주소) | [정적] |
| 〃 | `+0x2fe` | `+0x303` | `RtlpCopyEffectiveAce` | "효과적 ACE" 갈래 (라벨 복사) — 형 > `0xe` 는 통째 복사 후 `AceFlags &= 0xe0` | [정적] |
| `nt!SeDefaultObjectMethod` | `+0x277` | `+0x27c` | `ExAllocatePool2` | 캐시 항목 (태그 `ObSc`) — 같은 SD 가 없을 때만 | [정적] |
| 〃 | `+0x29f` | `+0x2a4` | 복사 함수 (같은 alias) | SD 바이트 그대로 사본 | [정적] |
| 〃 | `+0x395` | `+0x39a` | `ExFreePoolWithTag` | `RtlpNewSecurityObject` 가 만든 임시 SD 해제 | [정적] |
| `nt!ObInitSystem` | `+0x557` | `+0x55c` | `NtCreateDirectoryObject` | 루트 `\` (부모). SD 는 감사 전역 둘이 0 이면 `SePublicDefaultUnrestrictedSd` (`+0x427 ~ +0x43e`) | [정적] |
| `nt!SepTokenInitialization` | `+0x8d` | `+0x92` | `ObCreateObjectType` | 토큰 형 (구조체 길이 `0x80`, `+0x78` 칸 = 1) | [정적] |

### 사슬 — 이름 있는 객체는 한 칸 더 돈다 (결과서 질문 1)

- `ObInsertObjectEx` 안의 SD 계산 블록 (`+0x28c ~ +0x3e6`, `+0x325 SeAssignSecurityEx2`) 은 **이름 없는 객체만** 탄다. 이름이 있으면 `+0x27c JNZ → +0x3ea` 로 건너뛰고 `+0x443 ObpCreateHandle` 로 간다.
- 이름 있는 객체는 `ObpCreateHandle +0x13bd` → `ObpInsertOrLocateNamedObject` 가 이름을 찾은 뒤 (`+0x117`) 부모 디렉터리 SD 를 가져와 (`+0x175`) `+0x1fe ObpAssignSecurity` 를 부른다.
- **부모 SD 는 루트 `\`** — `ObInitSystem +0x557` 이 만든 디렉터리. 부모가 있으면 늘 가져오고 (`+0x145` 이후), 부모 플래그 8 (`NtCreateDirectoryObjectEx` 의 `Flags & 1` → `ObpCreateDirectoryObject +0x145 OR EDX,8`) 이거나 전역 `ObpObjectSecurityInheritance` 가 0 이 아니면 자동 상속 3 을 켠다. 이 전역의 파일 초기값은 0, 읽는 곳 2곳 (`+0x158` · `+0x1a6`), 레지스트리 `Session Manager\Kernel` 의 `ObObjectSecurityInheritance` 로 채워짐 (결과서 INIT 표) — 기본 설치엔 없다고 보면 0 `[추측: 값은 VM]`.
- **자동 상속 플래그 = 0** — `ObpAssignSecurity +0x6d ~ +0x74` 에서 `SeComputeAutoInheritByObjectTypeEx` 출력 | 호출자 인자. Directory 형은 `SeMandatoryLabelMask` 가 0 이고 (초기화 구조체 길이 `0x78` — `ObInitSystem +0x219`, `ObCreateObjectTypeEx +0x45c ~ +0x464` 는 길이 ≥ `0x80` 일 때만 `+0xd8` 을 채움), 명시 SD 에 DACL · SACL 이 다 있어 자동 상속 비트도 안 켜진다.
- **넘긴 SD 를 잡는 곳**: `SeCaptureSecurityDescriptor` 가 늘 자기상대형 사본을 만든다 (smss `Control 0x14` → 사본 `0x8014`, `AclSize` 는 4 맞춤 올림 — `0xb8` · `0x20` 은 이미 4 의 배수). `RtlpNewSecurityObject` 는 두 꼴을 다 읽어서 입력 꼴은 결과에 영향 없다 (결과서).

### 소유자 · 그룹 (결과서 질문 2)

- **어느 토큰**: `SubjectContext` 의 ClientToken (`[RDI]`, 남 흉내 중일 때) 이 있으면 그것, 없으면 PrimaryToken (`[RDI+0x10]`) — `RtlpNewSecurityObject +0x1c5 MOV R8,[RDI]` · `+0x1ce JNZ` · `+0x1d4`. smss 는 흉내를 안 내므로 smss 프로세스 토큰 `[추측]`.
- **칸**: `+0x1dc` `UserAndGroups` (`+0x98`), `+0x1e3` `DefaultOwnerIndex` (`+0x90`), `+0x202` = `UserAndGroups[DefaultOwnerIndex].Sid` (칸 16B × index), `+0x1ea` `PrimaryGroup` (`+0xa8`), 기본 DACL `+0x1f4` (`+0xb8`), 무결성 `+0x21d SepLocateTokenIntegrity` (`+0xd0`). 이 SID 들은 `+0x2ad ExAllocatePool2(0x100, 'SeSd')` 한 덩어리에 복사된다.
- **고르기**: 명시 소유자가 없고 `SEF_DEFAULT_OWNER_FROM_PARENT 0x20` 이 꺼져 있으면 (`+0x459`, Ob 는 안 줌) 토큰 소유자. 그룹도 같은 식 (`+0x48f TEST CL,0x40`). 못 찾으면 `0xC000007C` (소유자) · `0xC000005B` (그룹).
- **`SE_OWNER_DEFAULTED 0x1` · `SE_GROUP_DEFAULTED 0x2` 는 안 켜진다** — `Control` 을 만드는 줄 (`+0x58e ~ +0x5ac`, `+0xfc8 ~ +0xfdf`, `+0x1102 ~ +0x1119`) 에 그 비트가 없다.
- **SID 값은 VM** — smss 가 SYSTEM 토큰이면 소유자 `S-1-5-32-544` · 그룹 `S-1-5-18` 일 것 `[추측: 결과서, SYSTEM 이 만든 것의 흔한 모양]`.

### DACL (결과서 질문 3)

- DACL 은 `RtlpInheritAcl2` **모드 1** (`+0x1668`) 이 만든다. 자동 상속 꺼짐 → **명시 ACE 를 순서대로 모두 복사** (`RtlpInheritAcl2 +0x208 JZ → +0x2f4` → 모드 2 = 전부, `+0x290 RtlpCopyAces`). 부모를 쓰는 조건 (자동 상속 · 명시 DACL 없음 · `DACL_DEFAULTED`) 이 하나도 없어 **루트 `\` 의 ACE 는 안 섞인다** (`+0x134 JZ → +0x48f`).
- **generic 바꾸기는 `RtlpCopyAces` 본문** — 복사 (`+0x13e`) 뒤 형 `0..0xa` · `0xd` · `0xe` 이고 **`+0x14e TEST byte [RBX+1],8` (상속 전용) 이 아니면** 마스크 비트 31 · 30 · 29 · 28 에 `GenericMapping` 네 칸을 OR (`+0x15b` · `+0x161` · `+0x16a` · `+0x173`), generic 비트 지움 (`+0x17c AND EAX,0xfffffff`), 허용 · 거부 형 (`+0x190` 비트표 `0x673`) 이면 `& GenericAll` (`+0x1a3`), 감사 · 경보 형이면 `& (GenericAll | 0x1000000)`.
- `RtlpNewSecurityObject` 마지막 복사에도 같은 변환 루프가 있지만 (`+0x13e2 ~ +0x1480`), `RtlpInheritAcl2` 가 새 ACL 을 만들었으면 건너뛴다 (`+0x13b3 TEST R13B,R13B`) — 변환은 한 번.
- **Directory `GenericMapping`** = `ObpDirectoryMapping` (rva `0xc7fe70`) `{0x20003, 0x2000c, 0x20003, 0xf000f}` → `ObInitSystem +0x2a0` · `+0x2df` 로 초기화 구조체 `+0xc` → 형 객체 `+0x4c` → `ObpAssignSecurity +0x61`. (토큰 `SepTokenMapping` `{0x2001a, 0x201e0, 0x20005, 0xf01ff}`.)
- **`AclSize` 다시 잼** — `RtlpInheritAcl2 +0x1b1 ADD SI,8` · `+0x1b5 ADD SI,R12W` · `+0x1bb MOV [RBX+2],SI` = `8 + 148 = 0x9c`. `AceCount` 는 복사할 때마다 하나씩 (`RtlpCopyAces +0x1b4`) = 7, 리비전 `max(2, 명시)` = 2. 출력 버퍼는 처음 200B (`+0x149e`) 라 한 번에 된다.
- ACE 별 결과는 위 "`\RPC Control` 객체에 붙는 SD" 표.
- (우리와 상관없는 갈래) 자동 상속이 켜지면 `INHERITED_ACE 0x10` 이 붙은 명시 ACE 는 버리고 부모 ACE 를 `RtlpGenerateInheritedAce` 로 새로 만들어 섞는다 (결과서).
- **`OWNER RIGHTS` 검사** — 명시 SD · 만든 이 · 부모가 다 있고 `0x1000` 이 없으면 (`+0x1128` 등) `+0x181e` 재귀로 부모만의 SD 를 만들어 `+0x1846 RtlpOwnerAcesPresent` 를 본다. 있으면 `+0x1b22 SeAccessCheck` 로 `WRITE_DAC` 을 묻고 못 받으면 실패. 결과 바이트엔 영향 없음. 루트 DACL 에 `OWNER RIGHTS` 가 있는지는 `[VM]`.

### SACL · 라벨 (결과서 질문 4)

- **명시 라벨은 바이트 그대로** — 형 마스크가 0 일 때만 (`+0x781 JZ → +0xbd3`) 명시 SACL 에서 `0x11` 을 찾는다. 플래그가 `INHERIT_ONLY` 뿐 (`== 8`) 이거나 `INHERITED 0x10` 이 있으면 무시. 다시 만들 때 검사 (`+0xdb0 ~ +0xee1`): SID 권한 `{0,0,0,0,0,0x10}` 아님 · 플래그 `& 0xffffffe0` · 마스크 `& 0xfffffff8` (`+0xe38`) 이면 `0xC000000D`. 우리 값 (플래그 0, 마스크 1) 통과.
- 그 뒤 `RtlpInheritAcl2` **모드 3** (`+0x155e`, 자동 상속 = 상수 1 `+0x1559`) 이 라벨 ACL 을 복사하고 (`RtlpCopyAces` → `RtlpCopyEffectiveAce`, 플래그 0 이라 변화 없음), `+0xf73 RtlpCombineAcls` 가 형 순서로 모은다 → SACL `{rev 2, AclSize 0x1c, AceCount 1}` + 라벨 20B. 부모 SACL 은 "출력에 이미 `0x11` 있음" (`RtlpInheritAcl2 +0x5ab`) 이라 안 섞인다. 비-라벨 SACL (모드 2) 은 감사 ACE 가 없어서 `0x8000000b` (`+0x16b`) 로 끝난다.
- **라벨만 든 SACL 은 `SeSecurityPrivilege` 를 안 묻는다** — `+0x116d CMP BL,1` (이전 모드 유저) → `+0x1172 CMP byte [RBP-0x7f],0` (비-라벨 ACE 표시, 우리는 안 켜짐) → 권한 검사 `+0x1ec3` 로 안 감. 감사 ACE 가 있으면 `SeSecurityPrivilege` 없을 때 `0xC0000061` (`+0x1eed`) — SACL 을 버리지 않고 실패.
- **지배 검사** — `+0xfea TEST R9B,8` 가 없으면 `+0x1022 RtlSidDominates` (마지막 하위 권한 숫자: 토큰 ≥ 라벨). 못 지배하면 `+0x1182` → `+0x1c99 SePrivilegeCheck(SeRelabelPrivilege)`. smss (`S-1-16-16384` 가정) 는 `S-1-16-4096` 을 지배 `[추측: smss 무결성 값]`.
- **명시 라벨이 없을 때** (결과서 표):

| 조건 | 결과 |
|---|---|
| 형 `SeMandatoryLabelMask ≠ 0` (→ `AutoInheritFlags` `0x100` · `0x200` · `0x400`) | `{플래그 0, 마스크 = 그 비트, SID = 만든 쪽 무결성}`. 이때는 명시 라벨을 찾지도 않고, 대신 `SeComputeAutoInheritByObjectTypeEx +0x130 OR [R10+4],EDX` 가 명시 라벨 마스크에 형 마스크를 OR |
| 형 마스크 0, 만든 쪽 무결성 하위 권한 `< 0x2000` (Medium 미만) | `{플래그 0, 마스크 1, SID = 만든 쪽 무결성}` (`+0x18bb ~ +0x18d0`, `+0x2194 MOV EBX,1`) |
| 그 밖 | 안 붙임 (부모 SACL 에 상속 가능한 라벨이 있으면 모드 3 이 만들 수 있음) |

- Directory 는 형 마스크 0. `SeTrustConstraintMask` (신뢰 라벨 `0x14` 자동) 는 `SEF_AI_USE_EXTRA_PARAMS 0x800` 이 있을 때만 — Ob 는 안 줌.

### `Control` (결과서 질문 5)

| 비트 | 켜지나 (`\RPC Control`) | 근거 |
|---|---|---|
| `SE_SELF_RELATIVE 0x8000` | 켜짐 (늘) | `+0x58e MOV EAX,0x8000` · `+0x598 CMOVZ` |
| `SE_DACL_PRESENT 0x4` | 켜짐 | `+0x110f OR EAX,0x4` |
| `SE_SACL_PRESENT 0x10` | 켜짐 | `+0xfd3 ~ +0xfdc` |
| **`SE_SACL_AUTO_INHERITED 0x800`** | **켜짐** | 라벨 호출의 자동 상속 = 상수 1 (`+0x1559`) → `RtlpInheritAcl2 +0xd3 ~ +0xe9` 출력 플래그 `0x400` → `+0x1599` 읽기 → `+0xfc8 ~ +0xfdc` 에서 `0x810` |
| `SE_DACL_AUTO_INHERITED 0x400` | 안 켜짐 (DACL 자동 상속 꺼짐) | `+0x10aa TEST R9B,1` |
| `*_PROTECTED` · `*_DEFAULTED` (DACL · SACL) | 명시 SD 에 있으면 옮김 — 우리는 없음 | `& 0x1408` (`+0x1106`) |
| `SE_OWNER_DEFAULTED` · `SE_GROUP_DEFAULTED` | 안 켜짐 | 질문 2 |
| `SE_DACL_UNTRUSTED` · `SE_SERVER_SECURITY` · `*_AUTO_INHERIT_REQ` · `RM_CONTROL_VALID` | 안 옮김 (읽기만) | `+0x14b ~ +0x16e` |

- 조립: `+0x124b OR word [R14+2],R13W` · `+0x1250 MOV byte [R14],1`. 결과 **`0x8814`**.
- **확신이 낮은 한 곳** (결과서): `0x800`. 코드 줄은 분명하지만 VM 에서 `0x8014` 가 나오면 `+0x1559` 인자나 `+0x1599` 읽기를 다시 본다.

### 토큰 객체 (결과서 질문 6)

- **토큰 형 `SeMandatoryLabelMask = 1`** — `SepTokenInitialization` 이 초기화 구조체 길이를 `0x80` 으로 두고 (`+0x29` · `+0x4e`), 형 플래그 `+0x43 OR byte [RBP-0x27],0xe` (`UnnamedObjectsOnly | UseDefaultObject | SecurityRequired`), 구조체 `+0x78` 칸에 `+0x8a MOV [RBP+0x4f],EBX` (`+0x59 MOV EBX,1`) → `ObCreateObjectTypeEx +0x461` · `+0x464` 가 형 `+0xd8` 로 옮김.
- **길**: 토큰은 이름 없는 객체라 `ObInsertObjectEx` 의 SD 블록을 탄다 — `+0x282` → `+0x2c8 SeComputeAutoInheritByObjectTypeEx` (토큰은 `0x100` = NO_WRITE_UP) → `+0x325 SeAssignSecurityEx2` → `+0x38c SeDefaultObjectMethod (EDX=3)`. 만들기 자리는 run97 그대로 (`SepDuplicateToken +0x158 ObCreateObjectEx` → `NtDuplicateToken +0x1f8 ObInsertObjectEx`, `SepCreateTokenEx +0x5dc ObCreateObject` → `+0xbd7` · `+0xc3a ObInsertObject`).
- **명시 SD 가 없을 때 결과** — `RtlpNewSecurityObject` 가 빈 SD (`+0x621` · `+0x637`) 를 명시 SD 로 쓴다:
  - 소유자 · 그룹: 토큰 (질문 2).
  - DACL: 만든 쪽 토큰 `DefaultDacl` 사본 (`+0x10c6` · `+0x10d6 OR R13D,4`) + Token `GenericMapping` 변환 한 번 (`+0x13b3` 이 0 → `+0x13e2 ~ +0x1480` 루프). 기본 DACL 이 NULL 이면 DACL 없음.
  - **SACL = 라벨 `0x11` 하나** `{플래그 0, 마스크 1, SID = 만든 쪽 무결성}` (토큰에 무결성이 없으면 `SepDefaultMandatorySid`). `Control` 은 DACL 이 있으면 `0x8814`.
- **명시 SD 에 라벨이 있으면** 그 마스크에 `| 1` (`SeComputeAutoInheritByObjectTypeEx +0x130`).
- 라벨 SID 값 = 토큰을 만든 순간의 만든 쪽 무결성 → `[VM]`. `NtDuplicateToken` 이 SD 없이 불렸을 때 뒤에 붙이는 것 (`+0x21e SepAppendAceToTokenObjectAcl` · `+0x338 SepSetProcessTrustLabelAceForToken` → `0x14`, run97) 은 이번에 안 읽음 → 토큰 SD 의 최종 모양은 그 둘까지 읽어야 닫힌다.

### 저장 — `SeDefaultObjectMethod` 의 새로 달기 갈래 (EDX=3, 결과서 질문 1)

- 갈래: `+0x8c` (0 쓰기) · `+0x94 SUB EDX,1; JZ → +0x7d9` (1 묻기, run97) · `+0x1c` (2 삭제) · `+0x9d CMP EDX,2; +0xa0 JNZ → +0xa00 KeBugCheckEx` → 3 = `+0xa6` 부터.
- 하는 일 (`ObLogSecurityDescriptor` 를 안 거치고 인라인):
  1. 크기 = `0x14` + 소유자 · 그룹 · DACL · SACL 각각 4 맞춤 (`+0xaf ~ +0x161`).
  2. SD 바이트 해시 (`+0x172 ~ +0x18e`) → 캐시 `ObsSecurityDescriptorCache` 에서 같은 해시 · 크기 · 바이트를 찾음 (`+0x213 ~ +0x33e`).
  3. 있으면 참조만 늘림 (`+0x340`), 없으면 `+0x277 ExAllocatePool2` (태그 `ObSc`) 로 `머리 0x20 + SD 사본` (`+0x29f` 복사).
  4. 임시 SD 해제 (`+0x395`), 객체 헤더 `[몸통-8]` 에 `항목+0x20 | 0xf` (`+0x3a3` · `+0x3a7`).
- 그래서 **저장된 SD = `RtlpNewSecurityObject` 출력 바이트 그대로** (사본). run97 동적 ① 의 "헤더 SD 칸 = 몸통 `-8` 값 `& ~0xf`" 가 이 줄로 정적으로도 선다.

### test.exe 길에 있나

- **`\RPC Control` 사슬은 test.exe 초기화 밖** — 부팅 때 smss 가 부른다 (run98). test.exe 초기화 지도 (프로세스 생성 → 은퇴) 앞 · 다른 프로세스.
- **같은 `ObInsertObjectEx` 는 test.exe 만들기에도 있다** `[정적: 지도]` — `_map/NtCreateUserProcess_edges.tsv` 에 caller `PspInsertProcess +0x1de` (깊이 2) · `PspInsertThread +0x3e5` (2) · `SeSubProcessToken +0x80e` (4) 등. 지도에서 `ObInsertObjectEx` 는 caller 63곳이라 **안 펼친 공용 도구** (`_nodes.tsv` `helper`) 다. 흐름도엔 이미 `nt!ObInsertObjectEx` 노드 하나 (`PspInsertProcess` 의 "객체 등록") 가 있다.
- **test.exe 토큰** (`SeSubProcessToken +0x80e ObInsertObjectEx`) 은 토큰이라 이름 없는 길 → 형 규칙상 ObInsertObjectEx 를 지나면 라벨 `0x11` (마스크 `1` 이상) 이 있다 `[정적: 형 규칙]` — 단 `SeSubProcessToken` 이 넘기는 SD 와 뒤 `+0x847 SepAppendAceToTokenObjectAcl` · `+0x85b SepFinalizeTokenAcls` 가 무엇을 바꾸나는 안 봤다 `[공백]`. 만든 쪽 = 부모 cmd 스레드라 라벨 SID 는 cmd 의 무결성일 것 `[추측]`.
- 프로세스 · 스레드 객체 (`PspInsertProcess` · `PspInsertThread`) 가 SD 블록을 타는지 (형 `SecurityRequired`) 는 안 봤다 `[공백]`.
- 그래서 **흐름도 그림은 안 고친다** — 새 함수는 이미 있는 `ObInsertObjectEx` 노드 안쪽이고 (대표 지점 규칙), `\RPC Control` 사슬은 지도 밖.

### 호출 관계

```
nt!NtCreateDirectoryObject +0xc → ObpCreateDirectoryObject                                     [정적]
 ├─ +0xe0  ObCreateObjectEx → +0xde ObpCaptureObjectCreateInformation → +0x260 SeCaptureSecurityDescriptor
 │                                            (넘긴 SD → 자기상대형 사본, Control 0x14 → 0x8014)
 └─ +0x18a ObInsertObjectEx (AccessState 0)
      │  +0xde 형+0x42 & 8 (SecurityRequired) · +0x279 이름 있음 → SD 블록 (+0x28c ~ +0x3e6) 건너뜀
      │     (이름 없는 객체 = 토큰 등: +0x2c8 SeComputeAutoInherit… → +0x325 SeAssignSecurityEx2 → +0x38c SeDefaultObjectMethod)
      └─ +0x443 ObpCreateHandle
           └─ +0x13bd ObpInsertOrLocateNamedObject
                ├─ +0x117 ObpLookupObjectName · +0x175 ObpGetObjectSecurity (부모 = 루트 \)
                │    +0x14a 부모 플래그 8 ? · +0x158 ObpObjectSecurityInheritance ? → 자동 상속 3 (우리 0)
                └─ +0x1fe ObpAssignSecurity
                     ├─ +0x4c  SeComputeAutoInheritByObjectTypeEx   형+0xd8 = 0 → 0
                     ├─ +0xd9  SeAssignSecurityEx2 (부모, 명시, …, IsDirectory 1, 0, 확장, SubjectContext, 형+0x4c)
                     │    └─ +0x65 RtlpNewSecurityObject
                     │         ├─ +0x1c5 ~ +0x21d  토큰: 소유자 [+0x98][+0x90] · 그룹 +0xa8 · DACL +0xb8 · 무결성
                     │         ├─ +0x155e RtlpInheritAcl2 (모드 3 라벨, 자동 상속 1) → RtlpCopyAces → RtlpCopyEffectiveAce
                     │         ├─ +0xf73  RtlpCombineAcls → SACL 0x1c
                     │         ├─ +0x1022 RtlSidDominates (만든 쪽 ≥ 라벨)
                     │         ├─ +0x1668 RtlpInheritAcl2 (모드 1 DACL, 자동 상속 0)
                     │         │    └─ +0x290 RtlpCopyAces   +0x14e 상속 전용 아니면 generic → 매핑 · & GenericAll
                     │         │         AclSize 0xb8 → 0x9c (RtlpInheritAcl2 +0x1bb)
                     │         ├─ (+0x181e 재귀 · +0x1846 RtlpOwnerAcesPresent · +0x1b22 SeAccessCheck — OWNER RIGHTS 있을 때)
                     │         └─ +0x1225 ExAllocatePool2 → 조립 머리 → SACL → DACL → 소유자 → 그룹, Control 0x8814
                     └─ +0x14a SeDefaultObjectMethod (EDX=3)
                          캐시 ObsSecurityDescriptorCache 찾기 / +0x277 새 항목 · +0x29f 사본
                          +0x3a7 헤더 [몸통-8] = 항목+0x20 | 0xf

형 만들 때 (부팅):
  ObInitSystem +0x2a0 ObpDirectoryMapping {0x20003, 0x2000c, 0x20003, 0xf000f} · 구조체 길이 0x78 → 형+0xd8 = 0
  SepTokenInitialization +0x8a 구조체+0x78 = 1 · 길이 0x80 → ObCreateObjectTypeEx +0x464 형+0xd8 = 1
```

## 확인된 것

- `[정적]` 이름 있는 객체의 SD 는 `ObInsertObjectEx +0x443 ObpCreateHandle → +0x13bd ObpInsertOrLocateNamedObject → +0x1fe ObpAssignSecurity → +0xd9 SeAssignSecurityEx2 → +0x65 RtlpNewSecurityObject` 에서 계산된다. `ObInsertObjectEx +0x325` 는 이름 없는 객체 길.
- `[정적]` 계산된 SD 는 `SeDefaultObjectMethod` (EDX=3, `ObpAssignSecurity +0x14a`) 가 캐시에 사본으로 넣고 헤더 `[몸통-8]` 에 `| 0xf` 로 건다 (`+0x3a7`). 바이트는 그대로.
- `[정적]` 명시 SD 에 소유자 · 그룹이 없으면 만든 쪽 토큰의 `UserAndGroups[DefaultOwnerIndex]` · `PrimaryGroup` (`RtlpNewSecurityObject +0x1dc` · `+0x1e3` · `+0x1ea` · `+0x202`). `*_DEFAULTED` 안 켬.
- `[정적]` generic 바꾸기는 `RtlpCopyAces +0x146 ~ +0x1a5`, 상속 전용 (`+0x14e`) ACE 는 건너뜀. Directory 매핑 `{0x20003, 0x2000c, 0x20003, 0xf000f}` (`ObpDirectoryMapping`).
- `[정적]` 자동 상속 꺼진 명시 DACL 은 ACE 를 모두 순서대로 복사, 부모 ACE 안 섞음, `AclSize = 8 + 합` (`RtlpInheritAcl2 +0x1bb`).
- `[정적]` 명시 라벨 ACE 는 그대로 SACL 에 남고, 라벨만 있는 SACL 은 `SeSecurityPrivilege` 를 안 묻는다. 라벨 갈래가 돌면 `Control` 에 `0x810`.
- `[정적]` `\RPC Control` 저장 SD: `Control 0x8814`, SACL `0x1c` · DACL `0x9c`, 순서 머리 → SACL → DACL → 소유자 → 그룹, 총 `0xcc + 소유자 + 그룹`.
- `[정적]` 토큰 형 `SeMandatoryLabelMask = 1` (`SepTokenInitialization +0x8a`), Directory 형은 0 (`ObInitSystem +0x219` 구조체 길이 `0x78`). 그래서 새 토큰 객체 SD 엔 늘 라벨 `0x11` 마스크 1.
- `[정적: 지도]` test.exe 만들기의 `PspInsertProcess +0x1de` · `PspInsertThread +0x3e5` · `SeSubProcessToken +0x80e` 도 같은 `ObInsertObjectEx` 를 부른다. 지도에선 공용 도구라 안 펼침.
- 태그 바뀜 (flow.md): 3장 run98 절 "주인 칸은 시청 (커널) 이 채운다" `[추측]` → **`[정적]`** (run100, `RtlpNewSecurityObject +0x1dc ~ +0x202` · `+0x1ea`, SID 값은 `[VM]`). 3장 run97 · run98 절의 "소유자 · 그룹은 커널이 채운다 `[추측]`" 두 줄과 run98 절 `[추측]` 문단 (커널이 소유자 · 그룹 · GENERIC · `AclSize` 를 바꿀 수 있다) 도 같은 근거로 `[정적]`. run97 질문 6 `[공백]` ("토큰 객체 SD 에 `0x11` 이 붙나") → **`[정적]` 붙는다**.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① (보탬) `ObpAssignSecurity` 가 `SeComputeAutoInheritByObjectTypeEx` 를 부르는 자리 — `+0x4c/+0x51` (`.asm`). 결과서는 출력을 합치는 줄 (`+0x6d ~ +0x74`) 만 적음.
- ② (보탬) `RtlpNewSecurityObject` 가 `RtlpInheritAcl2` 를 부르는 자리는 **넷**이다 — `+0x85b` · `+0xaef` · `+0x155e` · `+0x1668` (`_calls.tsv`). 결과서는 모드 3 (`+0x155e`) · 모드 1 (`+0x1668`) 만 자리를 적고, 모드 2 (비-라벨 SACL, `0x8000000b` 로 끝남) 는 자리 없이 썼다. 앞 두 자리 중 어느 것이 모드 2 인지는 이번에 안 가렸다 (작은 `[공백]` — 결과 바이트엔 영향 없음).
- ③ (보탬) `RtlpCopyAces +0x13e` · `SeDefaultObjectMethod +0x29f` 의 복사 함수는 alias — rva `0x6c3640`, run97 ③ 과 같은 주소 (같은 주소 이름 4개, 후보 `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`). 하는 일은 복사.
- ④ (보탬) test.exe 길 — `_map/NtCreateUserProcess_*` 에서 `ObInsertObjectEx` caller 자리 셋과 "공용 도구라 안 펼침" (위 "test.exe 길에 있나"). 결과서엔 없음. test.exe 토큰에 라벨이 있을 것은 형 규칙에서 나온 것이고 `SeSubProcessToken` 인자는 안 봄.
- ⑤ (보탬, `[추측]`) run99 표의 rpcrt4 `RtlNewSecurityObjectEx` 6번째 인자 `1` 이 자동 상속 플래그 (`SEF_DACL_AUTO_INHERIT`) 라면 `[추측: 공개 원형]`, 커널 쪽 같은 이름 함수의 규칙으로는 자동 상속이 켜져 부모 (`\RPC Control`) 의 상속 가능한 ACE (`0x0b` 넷) 가 rpcrt4 기본 SD 에 섞이는 갈래다. ntdll `RtlpNewSecurityObject` 는 다른 바이너리라 이 문서 근거로는 말할 수 없다 → Q186 ① 의 입력 힌트.
- 정정 없음 (이전 run 문서가 틀린 곳 없음). 요청서의 가정 (`ObInsertObjectEx → ObpAssignSecurity` 직행) 은 run 문서에 적힌 적이 없어 정정 대상이 아니다. run97 의 "`ObInsertObjectEx +0x325 SeAssignSecurityEx2` → `+0x38c SeDefaultObjectMethod`" (토큰) 는 이 바퀴와 같다.

## 이전 run 에서 닫은 것 (오케스트레이터 요청)

- run97 문서 "토큰 객체 SD" 절 `0x11` `[공백]` 줄과 "아직 확인 필요" 정적 ① 줄 — 끝에 **(해결됨 → run100, Q180d)** 를 붙였다 (원래 글은 둠).
- flow.md 3장 run98 절 비유 · 본문, 3장 run97 절 두 줄, 4장 run97 · run98 · run99 정적 줄의 Q180d 항목 — 아래 "flow.md 에 반영한 것".

## 아직 확인 필요

**동적** (결과서 "동적으로 확인할 것" 1 ~ 6. **Q181 에 붙일 것**, VM 은 이번에 안 함). `\RPC Control` 은 전역 객체라 **test.exe 없이 부팅 뒤 덤프로** 볼 수 있다 `[추측]` (run97 · run98 과 같은 판단).

- ① **`\RPC Control` 실제 SD**: `!object \RPC Control` → 몸통 `B` → `dq B-8 L1` → `& 0xfffffffffffffff0` = `S` → `db S L0xe8` · `!sd S 1`. 위 저장 표와 칸별 대조 — **`Control` `0x8814` 인가** (`0x8014` 면 `RtlpNewSecurityObject +0x1559` · `+0x1599` 의심), DACL `AclSize 0x9c` · SACL `0x1c`, ACE 0 ~ 2 마스크 `0x2000f` · `0x2000f` · `0xf000f`, ACE 3 ~ 6 그대로, 오프셋 `Sacl 0x14` · `Dacl 0x30` · `Owner 0xcc`, 소유자 · 그룹 SID.
- ② **`\Windows` 처음 SD** — 같은 캐시 항목인가 (`ObsSecurityDescriptorCache`). csrss 가 DACL 을 고친 뒤 값은 다름 (run98).
- ③ **형 객체 칸**: `dd poi(nt!ObpDirectoryObjectType)+0x4c L4` (= `20003 2000c 20003 f000f`) · `+0xd8 L2` (= `0 0`), `dd poi(nt!SeTokenObjectType)+0x4c L4` (= `2001a 201e0 20005 f01ff`) · `+0xd8 L2` (= `1 0`), `dd nt!ObpObjectSecurityInheritance L1` (= 0), `dd poi(nt!ObpRootDirectoryObject)+0x150 L1` (루트 `Flags`, 비트 8 = 0 이어야).
- ④ **smss 토큰**: `dt nt!_TOKEN <T> DefaultOwnerIndex PrimaryGroup UserAndGroups IntegrityLevelIndex DefaultDacl` → `!sid` — 소유자 · 그룹 `[VM]` 값.
- ⑤ **토큰 객체 SD**: `NtOpenProcessToken(자기, 0xa0008)` 로 연 토큰의 SD — 라벨 `{0x11, 0, 1, SID}` 의 SID, DACL 이 `DefaultDacl` 과 같은가, `Control`. test.exe 토큰이면 run97 동적 ⑧ (`SeSubProcessToken+0x813` · `+0x84c` · `+0x860` 뒤 `!sd`) 과 같이 보면 `ObInsertObjectEx` 직후 라벨이 있는지 · 뒤 두 함수가 무엇을 더하나가 한 번에 갈린다 (보탬).
- ⑥ (선택, 부팅 중 — 어렵다) `nt!ObpAssignSecurity+0xde` (`SeAssignSecurityEx2` 직후 `eax`, `db poi(@rsp+0x58) L0xe8`) · `nt!RtlpNewSecurityObject+0x1225` (`rdx` = 마지막 SD 크기). 조건 = 몸통 `r12` 가 `\RPC Control`. ① 의 사후 읽기로 대신 권장 (결과서).

**정적** (결과서 "다음 요청 후보" + 보탬, 대기열 번호는 오케스트레이터가 붙임):

- ① `NtDuplicateToken` 의 SD 덧붙임 — `SepAppendAceToTokenObjectAcl` (`+0x21e/+0x223`, 인자 `8` · `SeAliasAdminsSid`) · `SepSetProcessTrustLabelAceForToken` (`+0x338/+0x33d`) 본문. 토큰 SD 바이트를 닫으려면 필요 (낮음 ~ 중간). test.exe 토큰 쪽 `SeSubProcessToken +0x847` · `+0x85b` (run97 정적 ③) 와 같이 보면 좋다.
- ② `NtOpenDirectoryObject("\RPC Control", 0x20001)` 접근 검사 (`ObOpenObjectByNameEx` → `ObpLookupObjectName` · `ObpCheckObjectReference` · `SeAccessCheck` 이름만) — 이 SD 로 `0x20001` 이 허용되는 조건 (= run97 정적 ② 와 같은 질문). 미리 보기 `[추측: 결과서]`: ACE 0 (Everyone `0x2000f`) 이 `0x20001` 을 덮고, 라벨 NO_WRITE_UP 은 읽기를 안 막는다.
- ③ (낮음) 명시 DACL **없는** 새 객체의 부모 상속 (`RtlpGenerateInheritedAce`, 루트 `\` SD `SePublicDefaultUnrestrictedSd` 내용 `SepInitializationPhase0/1`) — 에뮬레이터가 다른 디렉터리 · 뮤텍스를 만들 때.
- ④ (보탬, 작음) `RtlpNewSecurityObject +0x85b` · `+0xaef` 중 어느 것이 모드 2 (비-라벨 SACL) 인가 (위 보탬 ②).
- ⑤ (보탬, 작음) 프로세스 · 스레드 형의 `SecurityRequired` (형 `+0x42 & 8`) — test.exe 프로세스 · 스레드 객체가 `ObInsertObjectEx` SD 블록을 타는가 (`PsProcessType` · `PsThreadType` 초기화 줄). 짐 지도 후보 (객체 헤더 SD) 와 짝.
- ⑥ (= Q186 ① 에 넘길 힌트) ntdll `RtlpNewSecurityObject` · `RtlpInheritAcl2` 가 커널 쪽과 같은 규칙인가 — 같다면 rpcrt4 기본 SD 에 Liberal 의 상속 ACE 가 섞이는 갈래 (보탬 ⑤ `[추측]`).

**짐 지도 후보** (이번엔 baggage.md 안 고침 — 사용자 결정 대기): 토큰 · 프로세스 · 스레드 객체 헤더 SD (run97 정적 ③ · run98 짐 지도 후보와 같은 묶음). 이 바퀴로 "토큰 SD 에 무엇이 들어가나" 의 앞 절반 (`ObInsertObjectEx` 까지: 소유자 · 그룹 · 기본 DACL · 라벨 `0x11`) 은 규칙이 섰다. 공간 = 커널 (객체 헤더 · 캐시 풀 `ObSc`), 놓는 법 = nt 엔진 (syscall 안쪽), 경로 = `SeSubProcessToken +0x80e ObInsertObjectEx → +0x325 SeAssignSecurityEx2 → RtlpNewSecurityObject` · `+0x38c SeDefaultObjectMethod`.

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 안 고침 — 위 "test.exe 길에 있나"):
  - 기준: ntkrnlmp.pdb 줄에 run100 몫 (읽기 전용 export), 근거 로그 run100 줄 (사진 안 씀).
  - 흐름도 앞 글: run100 문단 하나.
  - 2.1 run49 토큰 줄 (run97) 아래: (run100) 한 줄 — 토큰 형 규칙으로 `ObInsertObjectEx` 직후 라벨 `0x11`, 뒤 두 함수는 `[공백]`.
  - 3장 run97 절: 토큰 객체 줄 `[공백]` → (해결됨 → run100), run98 줄의 `[추측]` → `[정적]` (run100).
  - 3장 run98 절: 비유 `[추측]` → `[정적]` (run100), Liberal 줄 "커널이 채울 것 `[추측]`" → `[정적]`, `[추측]` 문단 → `[정적]` (run100) 으로 고쳐 씀.
  - 3장 새 절 **"넘긴 SD 가 새 객체에 붙는 순간 — `nt!ObInsertObjectEx` → … → `SeAssignSecurityEx2` → `RtlpNewSecurityObject` (run100)"** 과 **"PE_emulator 요청 34 정적 답 한 장 (run97 ~ run100)"**.
  - 4장: 2.1 `ObInsertObjectEx` 줄에 (run100) 보탬, run97 동적 · PE_emulator 줄 · run98 동적 · PE_emulator · 짐 지도 후보 줄 · run99 동적 줄에 (run100) 보탬, run97 정적 ① · run98 정적 ① · run99 정적 ⑤ (해결됨 → run100), run100 새 줄 셋 (동적 · 정적 + 짐 지도 후보 · PE_emulator 와 보탠 것).
  - 5장: run 표 · 03_sprint 요청서 번호 run100 줄.
- 짐 지도: **고치지 않음** — 객체 헤더 SD 는 지금 짐 칸이 아니다 (run97 · run98 과 같은 판단, 사용자 결정 대기). 후보만 4장.
- 이전 run 문서: run97 에 (해결됨 → run100, Q180d) 두 줄. 정정 없음.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점** `[추측]`: 이번에 읽은 줄 안에 디버거 칸을 읽는 줄은 없다. 부팅 중 멈춤 (동적 ⑥) 은 smss 시간을 바꿀 수 있어 사후 읽기 (①) 가 낫다 (run98 과 같음).
- 도구: 결과서 "스크립트 문제" 없음. 지도 `_map/NtCreateUserProcess_*` 는 `ObInsertObjectEx` 를 공용 도구로 멈춰서 (caller 63) 안쪽 SD 사슬이 안 보인다 — 이번처럼 따로 export 해야 한다 (불편한 점, 오류 아님).
- **함정 후보** (→ windbg-kd-runbook pitfalls 에 추가 제안): 객체 SD 를 볼 때 이름 있는 객체 (`\RPC Control`) 와 이름 없는 객체 (토큰) 는 **계산 자리가 다르다** — 이름 있는 쪽은 `nt!ObpAssignSecurity+0xd9`, 이름 없는 쪽은 `nt!ObInsertObjectEx+0x325`. 한 자리에만 걸면 다른 쪽은 "안 걸림" 이 "안 탐" 이 아니다.

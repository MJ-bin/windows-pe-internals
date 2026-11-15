# 2026-11-15 run97 — 커널이 "보안 정보 묻기" 에 답하는 법 (`NtQuerySecurityObject` → `SeDefaultObjectMethod` → `SeQuerySecurityDescriptorInfo` → `RtlpFilterSacl`) (Q180a = Q180 ①, PE_emulator 요청 34 의 1 — 정적만): **비트 → 권한은 `NtQuerySecurityObject` 본문 안에 인라인으로 있다 — OWNER · GROUP · DACL · LABEL (`0x10`) · ATTRIBUTE · SCOPE · PROCESS_TRUST_LABEL · ACCESS_FILTER 는 `READ_CONTROL`, SACL (`0x8`) 은 `ACCESS_SYSTEM_SECURITY` 만, BACKUP 은 둘 다. 그래서 `0x20001` 핸들로 `0x17` 은 통과한다. 크기만 묻기 (버퍼 NULL · 길이 0) 는 `0xC0000023` 이고 `*LengthNeeded` 에 필요 길이 (늘 `0x14` 이상) 를 쓴다 — 실패여도 쓴다 (`+0x1e2`, 형 함수가 돌아온 뒤). 안 쓰는 건 핸들 단계 실패 (`0xC0000008` · `0xC0000022`) 뿐. 바이트는 머리 20B (`Revision 1` · `Control 0x8000` + 객체에서 옮긴 비트) 뒤 Owner → Group → DACL → SACL 차례로 4바이트씩 맞춰 붙는다. LABEL 만 물으면 객체 SACL 에서 형 `0x11` ACE 만 골라 새 SACL 머리 (`AclSize = 8 + 합` · `AceCount`) 를 만든다. 객체에 SACL 이 없으면 `Sacl` 오프셋 0. Directory · Token 형은 둘 다 형을 만들 때 칸을 0 으로 넘겨 `SeDefaultObjectMethod` 를 받는다 — 같은 길. `ObQuerySecurityDescriptorInfo` · `ObpGetObjectSecurity` · `SeQuerySecurityAccessMask` · `SeQueryMandatoryLabel` 은 이 길 밖이다. `NtDuplicateToken` 이 토큰 객체에 붙이는 SACL ACE 는 형 `0x14` (PROCESS_TRUST_LABEL) 이고 `0x11` 이 아니다.** `NtQuerySecurityObject` (`+0xd4 ~ +0x124` 권한 계산 → `+0x145 ObReferenceObjectByHandle` → `+0x181` 형 객체 `+0x98` 읽기 → `+0x1c2` 비교 → `+0x1d4 SeDefaultObjectMethod` (`EDX = 1`) → `+0x7fd SeQuerySecurityDescriptorInfo` (`+0x1a9 *길이` · `+0x1af JA → 0xC0000023` · `+0x1b5` 머리 · `+0x1fa` / `+0x24a` / `+0x2b9` 복사 · `+0x320 RtlpFilterSacl`) → `+0x1e2 *LengthNeeded`)

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### Q180a 결론 (run97)

- **크기만 묻기는 지금 틀렸다** `[정적]` — 진짜는 `0xC0000023` (`STATUS_BUFFER_TOO_SMALL`) + `*LengthNeeded` = 필요 길이다. 에뮬레이터가 "성공 0 + 길이 칸 안 씀" 으로 넘겨서 부른 쪽 (rpcrt4) 이 버퍼 크기를 못 얻었다. 이게 `UuidToStringA → 0xe` 의 가장 가까운 원인 후보다 `[추측: 결과서]`.
- **LABEL 권한은 `READ_CONTROL` 하나로 된다** `[정적]` — `0x17` → 요구 권한 `0x20000`. `0x20001` 디렉터리 핸들도, `0xa0008` 토큰 핸들도 통과한다. SACL (`0x8`) 만 `ACCESS_SYSTEM_SECURITY` 로 따로다.
- **바이트 만들기 규칙은 아래 "에뮬레이터용 규칙 한 장"** — 객체 SD (소유자 · 그룹 · DACL · SACL) 만 있으면 `0x17` · `0x10` 대답을 그대로 만들 수 있다.
- **형마다 따로 만들 필요 없다** `[정적]` — Directory · Token 둘 다 `SeDefaultObjectMethod`. 다른 함수를 쓰는 형은 파일 · 장치 (`IopGetSetSecurityObject`) · 레지스트리 키 (`CmpSecurityMethod`) · 그 밖 (간접, 예 `WmipSecurityMethod`).
- **남은 값은 VM 몫** — 진짜 `\RPC Control` · 토큰 객체 SD 에 SACL / 라벨 ACE 가 있나. 없으면 지금 객체 SD 구조 (라벨 칸 없음) 로도 바이트가 맞는다 (아래 질문 2 (a)). → Q181.

### 지금 PE_emulator 맞나 · 틀리나 (결과서 "지금 우리" 줄 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

| 갈래 | 진짜 (26100.9457) `[정적]` | 지금 우리 | 판정 |
|---|---|---|---|
| 비트 → 권한 | O · G · D · LABEL · `0x20` · `0x40` · `0x80` · `0x100` = `READ_CONTROL`, SACL = `ACCESS_SYSTEM_SECURITY`, BACKUP = 둘 다, 모자라면 `0xC0000022` | LABEL 비트 모름 | 모자람 — 표로 채우면 맞음 |
| 크기만 · 작음 | `0xC0000023` + `*LengthNeeded` = 필요 길이 (≥ `0x14`) | 성공 0 + 길이 칸 안 씀 | **틀림** |
| 충분 | 0 + `*LengthNeeded` = 만든 바이트 수 + 버퍼 채움 | (둘째 부름 0 줄 — 못 감) | 규칙대로 만들면 맞음 |
| 바이트 꼴 | 머리 20B → Owner → Group → DACL → SACL, 4B 맞춤, `Control` = `0x8000` + 옮긴 비트 | 차례 모름 (이쪽에서 코드 안 봄) | 확인할 것: 차례가 Owner · Group · DACL 인가 |
| LABEL 고르기 | SACL 에서 형 `0x11` 만, 새 SACL 머리 | 객체 SD 에 SACL 칸 없음 | 진짜 객체에 SACL 이 없으면 결과 같음 (오프셋 0 · `SE_SACL_PRESENT` 꺼짐) — VM 확인 |
| 형별 함수 | Directory · Token = `SeDefaultObjectMethod` | 한 구현 | 맞음 |

### 에뮬레이터용 규칙 한 장 (결과서 "에뮬레이터용 규칙 요약" + flow-writer 줄 확인)

1. **입구 (이전 모드 유저일 때)**: `LengthNeeded` 포인터를 4바이트 읽고-다시 쓰기 (`+0x3e ~ +0x5c`, 정렬 검사 없음, NULL 이면 접근 위반). 길이가 0 이면 버퍼 검사 통째로 건너뜀 (`+0x65 JZ → +0xb4`). 0 이 아니면 버퍼 4바이트 맞춤 (`+0x6a`) · 범위 (`+0x73 ~ +0x7e`) · 페이지마다 읽고-다시 쓰기.
2. **권한**: 질문 1 표로 `DesiredAccess` → 핸들 권한에 없으면 `0xC0000022` (`*LengthNeeded` 안 씀). 잘못된 핸들 `0xC0000008` (안 씀).
3. **객체 SD 가 NULL**: 필요 길이 `0x14`, 20바이트 `{01 00 00 80, 0 × 16}`.
4. **필요 길이** = `0x14` + Owner (`0x1` 물음 + 있음: `8 + 4 × SubAuthorityCount`) + Group (같은 식) + DACL (`0x4` 물음 + 객체 `Control 0x4` + 있음: `(AclSize + 3) & ~3`) + SACL (`0x1f8` 중 아무 비트 + 객체 `Control 0x10` + 있음: `(8 + 고른 ACE 합 + 3) & ~3`).
5. **`*길이 = 필요 길이` 먼저 쓰고** (`SeQuerySecurityDescriptorInfo +0x1a9`), `필요 > 받은 길이` 면 `0xC0000023` (버퍼 안 건드림). `NtQuerySecurityObject +0x1e2` 가 그 값을 유저 `*LengthNeeded` 로 옮긴다 — 성공 · 실패 모두.
6. **채우기**: 머리 0 으로 지움 → `Revision = 1` · `Control = 0x8000`. 본문 시작 = `(버퍼 + 0x17) & ~3`. 차례 Owner (`+4` 칸, `Control |= 원본 & 0x1`) → Group (`+8`, `& 0x2`) → DACL (`+0x10`, `& 0x140c`) → SACL (`+0xc`, `& 0x2830`). 각 칸 = 그 구성요소 시작 − 버퍼 시작. 안 물은 구성요소는 오프셋 0 · 비트 안 켬.
7. **SACL 고르기** (마스크 = `SecurityInformation & 0x1f8`): 형 `0x11` ← `0x10` · `0x12` ← `0x20` · `0x13` ← `0x40` · `0x14` ← `0x80` · `0x15` ← `0x100` · 나머지 전부 ← `0x8`. 객체 SACL 안 차례대로 통째 복사. 머리 = 객체 SACL 앞 8B 복사 뒤 `AclSize = 8` · `AceCount = 0` 으로 덮고 ACE 마다 더함.

### 원문 질문이 어디서 답해졌나

| 요청서 질문 | 답한 곳 (아래 절) | 한 줄 |
|---|---|---|
| 1 (가) 비트 → 권한 | "비트 → 권한" | 표, `0x20001` · `0x17` = 예, 모자라면 `0xC0000022` |
| 2 (나) LABEL 만 | "SACL 고르기" | 형 `0x11` 만, 머리 다시 만들기, 없을 때 셋 (a) (b) (c) |
| 3 (다) 바이트 꼴 | "바이트 만들기" | 차례 Owner → Group → DACL → SACL, 4B 맞춤, `Control` 마스크 |
| 4 (라) 크기만 | "크기만 묻기" | `0xC0000023`, `*LengthNeeded` 씀 (`+0x1e2`), 입구 검사 순서 |
| 5 (마) 형 함수 | "형 → `SecurityProcedure`" | Directory · Token 둘 다 `SeDefaultObjectMethod` |
| 6 토큰 SD | "토큰 객체 SD" | 만드는 쪽 SD + `SeAssignSecurityEx2`, `NtDuplicateToken` 은 `0x14` 를 붙임 |

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 사진 대조 없음, 사진도 다시 안 읽었다 (객체 SD · 형 객체는 run44 사진에 칸이 없다).
- 정적: **ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1`** (기준과 같음). **다시 analyze 안 함** (`.gpr` · `_index.tsv` · `SetPdb:` 줄 있음). 읽기 전용 export 와 map 만 (결과서).
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess`, `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines` 가 `_calls.tsv` 에 그대로.
- 에뮬레이터 로그 값 (`0x17` · `0x20001` · `0xa0008` · 383 샘플 등, 요청서 배경) 은 **PE_emulator 쪽 관찰**이라 이 문서의 `[관찰]` 이 아니다.
- 이 길은 **test.exe 초기화 지도 밖**이다 (아래 "test.exe 길에 있나").

## 목적

- PE_emulator 요청 34 의 1 (대기열 Q180 을 나눈 **Q180a**, 사용자 결정 2026-11-15). 에뮬레이터 rpcrt4 가 `\RPC Control` 디렉터리와 자기 토큰에 `NtQuerySecurityObject` 를 크기만 물을 때, 진짜 커널은 **어떤 권한을 요구하고, 어떤 status · 길이 · 바이트로 답하나**. 특히 LABEL (`0x10`) 비트.
- 범위 밖: `NtSetSecurityObject` (쓰기 길), `SeAccessCheck`, 감사 (`SeAuditing*`), smss 의 `\RPC Control` 만들기 (Q180b · sast/94), rpcrt4 (Q180c · sast/95), 새 객체 SD 계산 `SeAssignSecurityEx2` 본문 (Q180d · sast/96).

## 방법

- 정적: `_workspace/sast/93_rpc-sd-query.md` — ntoskrnl
  - depth 1: `^(NtQuerySecurityObject|ObQuerySecurityDescriptorInfo|SeQuerySecurityDescriptorInfo|SeDefaultObjectMethod)$` (31개, callee 포함 — `RtlpFilterSacl` 여기서).
  - depth 0: `SeQuerySecurityAccessMask` · `ObpGetObjectSecurity` · `SeQueryMandatoryLabel` · `ObCreateObjectTypeEx` · `ObCreateObjectType` · `ObInitSystem` · `SepInitializationPhase0/1` · `SepTokenInitialization` · `ObpCreateDefaultObjectTypeSD`, 질문 6 용 `NtDuplicateToken` · `SepDuplicateToken` · `NtCreateToken` · `NtCreateTokenEx` · `SepCreateToken` · `SepCreateTokenEx` · `ObpAssignSecurity` · `SeAssignSecurityEx` · `SeAssignSecurity` · `SepAppendAceToTokenObjectAcl` · `SepSetProcessTrustLabelAceForToken` · `RtlAddProcessTrustLabelAce`.
  - 읽기만 (이미 있던 `.asm`): `ObpReferenceObjectByHandleWithTag` · `ObReferenceObjectByHandle`.
  - 지도: `ms/disassemble/ntoskrnl/_map/NtQuerySecurityObject_{edges,nodes,summary}` (깊이 4, 기준 20, 함수 273개).
- 결과서: `_workspace/sast/93_rpc-sd-query_result.md` (**분석성공** — 못 닫은 것: 진짜 객체 SD 값, 형 객체 `+0x98` 실행 값, 입구 예외 때 돌려주는 값, 간접 호출 대상).
- flow-writer 가 다시 본 것 (`ms/disassemble/ntoskrnl/*.asm`, `_calls.tsv`, `_map/`):
  - `NtQuerySecurityObject +0x24` · `+0x2d` · `+0x35` · `+0x38` · `+0x3e ~ +0x5c` · `+0x5e` · `+0x65` · `+0x6a` · `+0x6d` · `+0x73` · `+0x7e` · `+0xb4` · `+0xd4` · `+0xd8` · `+0xdc` · `+0x118` · `+0x11c` · `+0x124` · `+0x14c` · `+0x181` · `+0x194` · `+0x1bd` · `+0x1c2` · `+0x1c5` · `+0x1d4` · `+0x1db` · `+0x1e2` · `+0x200` · `+0x212` · `+0x265` · `+0x26f`, `.c` 맨 위 caller 줄.
  - `SeQuerySecurityDescriptorInfo +0x3c ~ +0x42` · `+0xee` · `+0x10b` · `+0x110` · `+0x199 ~ +0x1d4` · `+0x1d8 ~ +0x338` (전부) · `+0x370 ~ +0x3a7` · `+0x3ef ~ +0x40d` · `+0x430 ~ +0x46e`.
  - `RtlpFilterSacl +0x97` · `+0x9a` · `+0x9d` · `+0xd7` · `+0xe0` · `+0x114`.
  - `SeDefaultObjectMethod +0x1c` · `+0x1f` · `+0x8c` · `+0x8e` · `+0x7d9` · `+0x7dd` · `+0x7fd` · `+0x809` · `+0x831` · `+0x9f6` · `+0xa00`.
  - `ObCreateObjectType +0xc` · `ObCreateObjectTypeEx +0x4af ~ +0x4bc` · `ObInitSystem +0x223 ~ +0x2e4` (Directory 구조체 칸 쓰기 전부) · `SepInitializationPhase0 +0x18` · `SepTokenInitialization +0x37 ~ +0x8d` (Token 구조체 칸 쓰기 전부).
  - `ObpReferenceObjectByHandleWithTag +0x514` · `+0x516` · `+0x693`, `ObpGetObjectSecurity +0x59` · `+0xdc`, `NtDuplicateToken +0xb2` · `+0x1f8` · `+0x21e` · `+0x338`, `SepSetProcessTrustLabelAceForToken +0x140` · `+0x196`, `RtlAddProcessTrustLabelAce +0x3c`, `SepDuplicateToken +0x158`, `SepCreateTokenEx` · `ObInsertObjectEx` 줄 (`_calls.tsv`).
  - `SeSubProcessToken` · `PspInitializeProcessSecurity` 의 `_calls.tsv` 줄 (보탬 — test.exe 토큰 길, 본문 안 읽음).
  - test.exe 길에 있나: ntdll `_map/LdrInitializeThunk_*`, kernelbase `_map/CreateProcessInternalW_*` · `_map/UnhandledExceptionFilter_*`, ntoskrnl `_map/NtCreateUserProcess_*` 에서 `QuerySecurity` 검색. ntdll · kernelbase · kernel32 `.asm` 전체에서 `QuerySecurityObject` 검색 (0 건).

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntoskrnl/_calls.tsv` · `.asm` · `_map/NtQuerySecurityObject_edges.tsv`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `nt!NtQuerySecurityObject` | `+0xa8` · `+0xae` | `+0xad` · `+0xb3` | `ExRaiseAccessViolation` · `ExRaiseDatatypeMisalignment` | 입구 버퍼 검사 실패 (첫 `__try` 안) | [정적] |
| 〃 | **`+0x145`** | **`+0x14a`** | **`ObReferenceObjectByHandle`** | `(핸들, DesiredAccess, ObjectType = NULL, 이전 모드, …)`. `+0x14c JS → +0x200` (실패면 `*LengthNeeded` 안 씀) | [정적] |
| 〃 | **`+0x1d4`** | **`+0x1d9`** | **`SeDefaultObjectMethod`** | 형 객체 `+0x98` (`+0x181`) == `SeDefaultObjectMethod` (`+0x194` LEA · `+0x1c2` CMP · `+0x1c5` JNZ). `EDX = 1` (질문, `+0x1bd`) | [정적] |
| 〃 | `+0x238` | `+0x23d` | `IopGetSetSecurityObject` | `+0x98` == 이 함수 (파일 · 장치) | [정적] |
| 〃 | `+0x258` | `+0x25d` | `CmpSecurityMethod` | `+0x98` == 이 함수 (레지스트리 키) | [정적] |
| 〃 | `+0x265` | `+0x26a` | 간접 (CFG) | 그 밖 형 (대상은 실행 때) | [정적] |
| 〃 | `+0x1f9` | `+0x1fe` | `ObfDereferenceObject` | 끝맺음 | [정적] |
| `nt!ObReferenceObjectByHandle` | `+0x29` | — | `ObpReferenceObjectByHandleWithTag` | `DesiredAccess` (EDX) 그대로 (결과서) | [정적] |
| `nt!SeDefaultObjectMethod` | `+0x7dd` | `+0x7e2` | `ObpReferenceSecurityDescriptor` | 질문 갈래 (`+0x7d9 ADD RCX,-0x30` = 객체 헤더) | [정적] |
| 〃 | **`+0x7fd`** | **`+0x802`** | **`SeQuerySecurityDescriptorInfo`** | 직접 — `ObQuerySecurityDescriptorInfo` 를 안 거침 | [정적] |
| 〃 | `+0x9f6` | `+0x9fb` | `ObDereferenceSecurityDescriptor` | 빌린 참조 되돌리기 (또는 `+0x809 ~ +0x831` 직접) | [정적] |
| `nt!SeQuerySecurityDescriptorInfo` | `+0x1fa` · `+0x24a` · `+0x2b9` | `+0x1ff` · `+0x24f` · `+0x2be` | 복사 함수 | Owner · Group · DACL 복사. 같은 주소 (rva `0x6c3640`) 이름 4개 (Ghidra 표시 `RtlCopyMemory` · `RtlMoveMemory` · `memcpy` · `memmove`, PDB `--lookup` 은 `memcpy` · `memmove` 2개) | [정적] |
| 〃 | **`+0x320`** | **`+0x325`** | **`RtlpFilterSacl`** | 채우기 — `(객체 SACL, 쓸 자리, &크기, 마스크 = SecurityInformation & 0x1f8)` (`+0x30b ~ +0x31d`) | [정적] |
| 〃 | `+0x383` | `+0x388` | `RtlpFilterSacl` | 크기만 계산 — `RDX = 0` (`+0x37e`) | [정적] |
| `nt!ObQuerySecurityDescriptorInfo` | `+0x3d` | `+0x42` | `SeQuerySecurityDescriptorInfo` | **길 밖** — caller `WmipSecurityMethod` · `FUN_140b2a1cc` | [정적] |
| `nt!ObInitSystem` | `+0x2e4` | `+0x2e9` | `ObCreateObjectType` | Directory 형 (`R9 = ObpDirectoryObjectType`, `+0x299`) | [정적] |
| `nt!SepInitializationPhase0` | `+0x18` | `+0x1d` | `SepTokenInitialization` | | [정적] |
| `nt!SepTokenInitialization` | `+0x8d` | `+0x92` | `ObCreateObjectType` | Token 형 (`R9 = SeTokenObjectType`, `+0x52`) | [정적] |
| `nt!ObCreateObjectType` | `+0xc` | `+0x11` | `ObCreateObjectTypeEx` | 22바이트 껍데기 | [정적] |
| `nt!NtDuplicateToken` | `+0xb2` | `+0xb7` | `SeCaptureObjectAttributeSecurityDescriptorPresent` | `ObjectAttributes` 에 SD 있나 | [정적] |
| 〃 | `+0x1f8` | `+0x1fd` | `ObInsertObjectEx` | 넣기 (그 안 `+0x325 SeAssignSecurityEx2` → `+0x38c SeDefaultObjectMethod` 새로 달기) | [정적] |
| 〃 | `+0x21e` | `+0x223` | `SepAppendAceToTokenObjectAcl` | SD 를 안 받았을 때, `(토큰, 8, SeAliasAdminsSid)` | [정적] |
| 〃 | `+0x338` | `+0x33d` | `SepSetProcessTrustLabelAceForToken` | → `+0x140 RtlAddProcessTrustLabelAce` (형 `0x14` 만, `+0x3c`) → `+0x196 ObSetSecurityObjectByPointer` | [정적] |
| `nt!SepDuplicateToken` | `+0x158` | `+0x15d` | `ObCreateObjectEx` | 토큰 객체 몸통 | [정적] |
| `nt!SepCreateTokenEx` | `+0x5dc` · `+0xbd7` · `+0xc3a` | `+0x5e1` · `+0xbdc` · `+0xc3f` | `ObCreateObject` · `ObInsertObject` × 2 | 새 토큰 | [정적] |
| `nt!SeSubProcessToken` | `+0x80e` · `+0x847` · `+0x85b` | `+0x813` · `+0x84c` · `+0x860` | `ObInsertObjectEx` · `SepAppendAceToTokenObjectAcl` · `SepFinalizeTokenAcls` | **보탬** — test.exe 토큰을 만드는 길 (run49 `PspInitializeProcessSecurity +0xf7`). 본문 안 읽음 | [정적: `_calls.tsv`] |

- `NtQuerySecurityObject` 의 커널 안 caller 는 `RtlpSysVolCheckOwnerAndSecurity` 하나 (`.c` 맨 위 줄). 유저는 syscall 로 들어온다.
- 지도 기준 직접 부르는 것: 위 `NtQuerySecurityObject` 줄 일곱 + CFG 간접 하나. 지도에 **없는** 것: `ObQuerySecurityDescriptorInfo` · `ObpGetObjectSecurity` · `SeQuerySecurityAccessMask` · `SeQueryMandatoryLabel`.

### 비트 → 권한 (`NtQuerySecurityObject +0xc7 ~ +0x124`, 결과서 질문 1)

`SecurityInformation` 으로 `DesiredAccess` 를 만들어 `+0x145 ObReferenceObjectByHandle` 에 넘긴다. 켜진 비트들의 OR.

| 비트 | 이름 | 필요한 핸들 권한 | 줄 |
|---|---|---|---|
| `0x1` · `0x2` · `0x4` · **`0x10`** | OWNER · GROUP · DACL · **LABEL** | `READ_CONTROL` (`0x20000`) | `+0xd4 BTS EDX,0x11` · `+0xd8 TEST R8B,0x17` · `+0xdc CMOVZ` |
| `0x8` | SACL | `ACCESS_SYSTEM_SECURITY` (`0x1000000`) **만** | `+0x118 BTS EDX,0x18` · `+0x11c TEST [RSP+0x88],0x8` · `+0x124 CMOVZ` |
| `0x20` · `0x40` · `0x80` | ATTRIBUTE · SCOPE · PROCESS_TRUST_LABEL | `READ_CONTROL` | `+0xe1 ~ +0x103` (결과서) |
| `0x100` | ACCESS_FILTER | `READ_CONTROL` | `+0x106 BT R8D,8` → `+0x279` (결과서) |
| `0x10000` | BACKUP | `READ_CONTROL` + `ACCESS_SYSTEM_SECURITY` | `+0xc7` → **`+0x26f MOV ECX,0x1020000`** |
| 그 밖 | | 요구 없음 | |

- 같은 규칙이 `SeQuerySecurityAccessMask` (`+0x0 ~ +0x5a`) 에도 있지만 그 함수의 caller 는 `IoCheckFunctionAccess` 뿐이다 (`.c` 맨 위 줄). **이 길에선 복사본이 본문에 인라인.**
- **`0x20001` 핸들 + `0x17` = 통과** — 요구 `0x20000` 이 핸들 권한 안에 있다. 비교 = `ObpReferenceObjectByHandleWithTag +0x514 TEST EBP,EAX` (요구 & ~가진 권한) · `+0x516 JNZ → +0x693 MOV EBP,0xc0000022`. 이 비교는 이전 모드가 유저일 때만 (결과서 `+0x392` · `+0x3a1`).
- 토큰 핸들 `0xa0008` + `0x10` 도 같은 까닭으로 통과.
- `ObjectType = NULL` 이라 형 검사 없음. 잘못된 핸들 = `0xC0000008` (결과서 `+0x575`).

### 크기만 묻기 — status · `*LengthNeeded` (결과서 질문 4)

| 경우 | status | `*LengthNeeded` | 버퍼 |
|---|---|---|---|
| 크기만 (NULL · 0) | **`0xC0000023`** (`SeQuerySecurityDescriptorInfo +0x3a2`) | **씀** = 필요 길이 | 안 씀. 입구 버퍼 검사도 건너뜀 |
| 작음 (0 < 길이 < 필요) | `0xC0000023` | 씀 | 안 씀 (입구에서 페이지마다 읽고-다시 쓰기만) |
| 충분 | `0` (`+0x46e`) | 씀 = 만든 바이트 수 | 채움 |
| 핸들 실패 | `0xC0000008` / `0xC0000022` | **안 씀** (`+0x14c JS → +0x200`) | 안 씀 |

- 쓰는 순서: 안쪽 `SeQuerySecurityDescriptorInfo +0x1a1 MOV RAX,[RSP+0xd0]` · **`+0x1a9 MOV [RAX],EDI`** (지역 길이 칸) → `+0x1ab CMP` · `+0x1af JA → +0x3a2`. 바깥 `NtQuerySecurityObject +0x1db MOV EAX,[RSP+0x98]` · **`+0x1e2 MOV [RDI],EAX`** (유저 포인터, `RDI` = `+0x4b`). 형 함수가 돌아온 뒤 status 와 상관없이 쓴다. 이 줄은 둘째 `__try` 범위 (결과서: 착지점 `+0x1e6`).
- 객체 SD 가 NULL 이면 `+0x435` 가 `0x14` 를 쓴다.
- 필요 길이는 늘 `0x14` 이상 (`+0xee MOV EDI,0x14`) 이고 비교가 `JA` 라 **길이 0 으로 성공하는 길은 없다** (기본 함수 형).
- **입구 검사 순서** (`+0x24` `GS:[0x188]` · `+0x2d` `[+0x232]` 이전 모드 → `+0x35` · `+0x38 JZ +0x212` 커널이면 건너뜀):
  1. `+0x3e ~ +0x5c` `LengthNeeded` 를 `min(포인터, 0x7fffffff0000)` 로 눌러 4바이트 읽고-다시 쓰기. 정렬 검사 없음.
  2. `+0x5e ~ +0x65` 길이 0 이면 `JZ → +0xb4` (버퍼 검사 통째로 건너뜀).
  3. `+0x6a TEST BL,3` · `+0x6d JNZ` → `ExRaiseDatatypeMisalignment`, `+0x73 ~ +0x7e` 넘침 · 범위 → `ExRaiseAccessViolation`, 아니면 페이지마다 읽고-다시 쓰기 (결과서 `+0x90 ~ +0xa4`).
  - 이 검사가 첫 `__try` (착지점 `+0xb7` → `+0x200`). 돌려주는 값이 예외 코드인지는 `[추측]` (결과서 — `__C_specific_handler` 가 EAX 로 넘기는 값, 동적 ⑥).

### 바이트 만들기 (`SeQuerySecurityDescriptorInfo`, 결과서 질문 3)

0. **객체 SD NULL** (`+0x3c MOV RAX,[R9]` · `+0x3f TEST` · `+0x42 JZ → +0x430`): `*길이 = 0x14` (`+0x435`), 작으면 `0xC0000023` (`+0x43c`), 아니면 `+0x448 ~ +0x45a` 20바이트 (`Revision 1` · `Control 0x8000`) · 성공.
1. **BACKUP 넓히기** (`+0x10b BT R9D,0x10` · `+0x110 JC → +0x3ef OR R9D,0x1ff`): 원래 `0x80` · `0x100` 이 없었으면 다시 끔 (`+0x460` · `+0x405`). 넓힌 값을 `[R8]` (부른 쪽의 `SecurityInformation` 칸) 에 되쓴다 (`+0x3f6` · `+0x40a` · `+0x465`).
2. **구성요소 찾기**: self-relative 면 `base + 오프셋`, absolute 면 포인터. DACL 은 객체 `Control 0x4`, SACL 은 `0x10` 일 때만 (결과서 `+0x9a` · `+0xc0`).
3. **크기** (`+0xee` 에서 `0x14` 로 시작): Owner · Group `8 + 4n`, DACL `(AclSize+3)&~3`, SACL `+0x199 TEST AL,0x10` · `+0x19b JNZ → +0x370 ~ +0x388` (`RtlpFilterSacl` 크기만, 담을 ACE 0 이어도 8).
4. **`*길이` 쓰기 · 비교** (`+0x1a9` · `+0x1ab` · `+0x1af JA → +0x3a2`). 출력 버퍼는 이 뒤부터만 건드린다.
5. **머리** `+0x1b5 ~ +0x1cc`: 16+4 바이트 0 → `+0x1c0 MOV byte [RSI],1` (Revision) → `+0x1c3 MOV ECX,0x8000` · `+0x1c8 OR` · `+0x1cc` (`Control`). 객체 `Revision` 을 안 옮긴다.
6. **본문 시작** `+0x1d0 LEA RDI,[RSI+0x17]` · `+0x1d4 AND RDI,-4`.
7. **차례**:

| 차례 | 물음 조건 | 복사 | 오프셋 칸 | `Control` 에 옮김 | 다음 자리 |
|---|---|---|---|---|---|
| Owner | `+0x1e0 TEST AL,1` + 있음 (`+0x1e8`) | `+0x1fa` (SID 길이) | `+0x203 [RSI+4]` | `+0x20a AND CX,0x1` | `+0x216 ~ +0x223` 4B 올림 |
| Group | `+0x236 TEST AL,2` + 있음 | `+0x24a` | `+0x253 [RSI+8]` | `+0x25a AND CX,0x2` | `+0x266 ~ +0x270` |
| DACL | `+0x28c TEST AL,4` → 비트 먼저 (`+0x294 0x140c` · `+0x29f`), 복사는 객체 `Control 0x4` (`+0x2a3`) + 있음 (`+0x2a9`) | `+0x2b9` (`AclSize` 바이트, `+0x2ae`) | `+0x2c2 [RSI+0x10]` | `& 0x140c` | `+0x2c5` · `+0x2c8` (`R15D` = 올린 크기) |
| SACL | `+0x2d8 TEST [R8],0x1f8` → 비트 먼저 (`+0x2e5 0x2830` · `+0x2ee`), 복사는 객체 `Control 0x10` (`+0x2f3`) + 있음 (`+0x302`) | `+0x320 RtlpFilterSacl` | `+0x329 [RSI+0xc]` | `& 0x2830` | `+0x32c ~ +0x330` |

- `0x140c` = `DACL_PRESENT 0x4` · `DACL_DEFAULTED 0x8` · `DACL_AUTO_INHERITED 0x400` · `DACL_PROTECTED 0x1000`. `0x2830` = `SACL_PRESENT 0x10` · `SACL_DEFAULTED 0x20` · `SACL_AUTO_INHERITED 0x800` · `SACL_PROTECTED 0x2000` (비트 이름은 `[추측: 공개 헤더]`, 값은 코드).
- **DACL · SACL 의 `Control` 비트는 물음 비트만 보고 켠다** — 객체에 그 비트가 있으면 복사를 안 해도 옮겨진다 (`+0x290 ~ +0x29f` 가 `+0x2a3` 검사보다 앞). 결과서 질문 2 (c) "NULL DACL/SACL = 오프셋 0 + PRESENT 켜짐" 이 이 꼴이다.
- 안 물은 구성요소: 오프셋 0 · 비트 없음. 구성요소 사이 올림 빈틈은 안 지운다 (`memset` 없음, 결과서).
- 객체 SD 가 absolute 여도 출력은 늘 self-relative.

### SACL 고르기 — LABEL 만 물을 때 (`RtlpFilterSacl`, 결과서 질문 2)

| ACE 형 | 담으려면 마스크에 | 줄 (결과서) |
|---|---|---|
| `0x11` SYSTEM_MANDATORY_LABEL | `0x10` | 크기 `+0x49` · `+0x52`, 복사 `+0x114 TEST BPL,0x10` |
| `0x12` RESOURCE_ATTRIBUTE | `0x20` | `+0x17c` · `+0x185` |
| `0x13` SCOPED_POLICY_ID | `0x40` | `+0x18c` · `+0x1b8` |
| `0x14` PROCESS_TRUST_LABEL | `0x80` | `+0x13b` · `+0x168` |
| `0x15` ACCESS_FILTER | `0x100` | `+0x19b` · `+0x1bf` |
| 그 밖 모두 (감사 · 알람 · 허용 · 거부 · 15 넘는 번호) | `0x8` | `+0x10c` · `+0x1aa` · `+0xc1` |

- **`0x17` (마스크 `0x10`) → 형 `0x11` 만 담긴다.** `0x8` 만이면 라벨은 안 간다.
- 머리: `+0x97` · `+0x9a` 객체 SACL 앞 8B 통째 복사 (`AclRevision` · `Sbz1` · `Sbz2` 는 객체 것) → **`+0x9d MOV [R14+2],R9D` (`R9D = 8`)** = `AclSize 8` · `AceCount 0`. ACE 마다 `+0xd7 INC [R14+4]` · `+0xe0 ADD [R14+2],AX`.
- 차례는 객체 SACL 안 차례 그대로, ACE 통째 (`AceSize`).
- **라벨 ACE 가 없을 때**:
  - (a) 객체 SACL 없음 (`Control 0x10` 꺼짐 또는 오프셋 0): 복사 건너뜀 (`+0x305 JZ`, 크기 쪽 `+0x373 JZ`) → `Sacl` 오프셋 0, `SE_SACL_PRESENT` 는 객체 것 그대로 (없으면 꺼짐).
  - (b) SACL 은 있는데 `0x11` 없음: **8바이트 빈 SACL** 이 붙고 오프셋 ≠ 0, `SE_SACL_PRESENT` 켜짐.
  - (c) `SE_SACL_PRESENT` 켜짐 + 오프셋 0 (NULL SACL): 오프셋 0 + `SE_SACL_PRESENT` 켜짐.

### 형 → `SecurityProcedure` (결과서 질문 5)

- **갈림길은 `NtQuerySecurityObject` 하나**: 헤더 쿠키로 형 찾기 (결과서 `+0x152 ~ +0x17d`, `ObHeaderCookie` · `ObTypeIndexTable`) → `+0x181 MOV R10,[RAX+0x98]` (형 객체 `+0x98` = `SecurityProcedure`) → 주소 비교 셋 (위 호출 표).
- `SeDefaultObjectMethod` 의 연산 갈래: `EDX == 2` 삭제 (`+0x1c` · `+0x1f`), `0` 쓰기 (`+0x8c` · `+0x8e`), **`1` 질문 → `+0x7d9`**, `3` 새로 달기, 그 밖 `KeBugCheckEx` (`+0xa00 ~`).
- **형을 만들 때 `+0x58` (`SecurityProcedure`) 를 0 으로 두면 기본 함수**: `ObCreateObjectTypeEx +0x4af CMP [RDI+0x58],R14` (0) · `+0x4b3 JNZ` · `+0x4b5 LEA RAX,[SeDefaultObjectMethod]` · **`+0x4bc MOV [RBX+0x98],RAX`**.

| 형 | 만드는 자리 | 초기화 구조체 `+0x58` | 형 객체 `+0x98` |
|---|---|---|---|
| Directory (`ObpDirectoryObjectType`) | `ObInitSystem +0x2e4` | 0 — `+0x223 memset` 뒤 `+0x0` · `+0x8` (바로 앞 형 형 때 쓴 것, `+0x22f` · `+0x237`) 과 `+0x2` · `+0xc` · `+0x1c` · `+0x24` · `+0x2c` · `+0x40` (`ObpCloseDirectoryObject`) · `+0x48` (`ObpDeleteDirectoryObject`) 만 씀 (`+0x28a ~ +0x2df`) | **`SeDefaultObjectMethod`** |
| Token (`SeTokenObjectType`) | `SepTokenInitialization +0x8d` (`SepInitializationPhase0 +0x18`) | 0 — `+0x37 memset` 뒤 `+0x2` · `+0x4` · `+0x8` · `+0xc` · `+0x1c` · `+0x24` · `+0x48` · `+0x78` 만 씀 (`+0x43 ~ +0x8a`, 칸 = `RBP` 기준 −`0x29`) | **`SeDefaultObjectMethod`** |

- 이건 "형을 만들 때 값" 까지다. 만든 뒤 `+0x98` 을 다른 곳에서 덮는지는 안 봤다 → 동적 ②.
- **길 밖인 셋**:
  - `ObQuerySecurityDescriptorInfo` (참조 빌리기 → `SeQuerySecurityDescriptorInfo` → 되돌리기) — caller `WmipSecurityMethod` · `FUN_140b2a1cc`. 자기 함수를 가진 형이 쓰는 도우미.
  - `ObpGetObjectSecurity` — 접근 검사 쪽 (`ObpCheckTraverseAccess` · `ObpCheckObjectReference` · `ObCheckCreateObjectAccess` 등) 이 쓴다. 같은 비교를 따로 하고 (`+0x59 CMP [RBP+0x98],RAX`), 기본 함수면 헤더 SD 를 직접 읽는다. 아니면 `SecurityInformation = 0x1bf` (`+0xdc`) 로 형 함수를 부른다.
  - `SeQuerySecurityAccessMask` (caller `IoCheckFunctionAccess`) · `SeQueryMandatoryLabel` — 이 지도에 없음.

### 토큰 객체 SD (이름 · 줄만, 결과서 질문 6)

- **만들기**: 복제 `SepDuplicateToken +0x158 ObCreateObjectEx`, 새로 `SepCreateTokenEx +0x5dc ObCreateObject`.
- **SD 는 만드는 쪽 것 + 계산**: `ObjectAttributes` 의 SD 를 먼저 보고 (`NtDuplicateToken +0xb2`, `SepCreateTokenEx +0x118`), 넣을 때 `ObInsertObjectEx +0x325 SeAssignSecurityEx2` 가 계산해 `+0x38c SeDefaultObjectMethod` (새로 달기) 로 건넨다. 형 기본 SD 가 아니다. `ObpCreateDefaultObjectTypeSD` 는 형 객체 자신의 SD 용.
- **라벨 여지**: `NtDuplicateToken` 은 SD 를 안 받았을 때 넣은 뒤 `+0x21e SepAppendAceToTokenObjectAcl(토큰, 8, SeAliasAdminsSid)` → `+0x338 SepSetProcessTrustLabelAceForToken` → `RtlAddProcessTrustLabelAce` (**형 `0x14` 만**, `+0x3c CMP [RSP+0x60],0x14`) → `ObSetSecurityObjectByPointer`. 그래서 붙는 건 `0x14` 이고 **`0x17` 질문에는 안 나온다** (마스크 `0x10` 은 `0x14` 를 안 고름).
- `0x11` 라벨을 붙이는지는 `SeAssignSecurityEx2` / `RtlpNewSecurityObject` 안 — `[공백]` (= Q180d, sast/96 진행 중).
- (보탬) **test.exe 토큰은 `NtDuplicateToken` 길이 아니다** — run49 의 `PspInitializeProcessSecurity +0xf7 SeSubProcessToken → +0x43c SepDuplicateToken`. `SeSubProcessToken` 안에도 `+0x80e ObInsertObjectEx` → `+0x847 SepAppendAceToTokenObjectAcl` → `+0x85b SepFinalizeTokenAcls` 가 있다 (`_calls.tsv`, 본문 안 읽음). 이 길이 토큰 객체 SD 에 무엇을 붙이나는 `[공백]`.

### test.exe 길에 있나

- **test.exe 초기화 지도 안에는 `NtQuerySecurityObject` 를 부르는 줄이 없다** `[정적: 지도]`:
  - ntdll `_map/LdrInitializeThunk_*` 0 건 (비슷한 이름 `NtQuerySecurityAttributesToken` 만 — `RtlQueryPackageClaims` 등, 다른 syscall). ntdll · kernelbase · kernel32 `.asm` 에도 0 건.
  - kernelbase 는 부모 쪽 `CreateProcessInternalW` 지도의 **AppContainer · LowBox 갈래에만**: `+0x24e9 BasepCreateAppContainer` · `+0x266d BasepCreateLowBox` → `BasepCreateAppContainerObjectDirectories +0x3dd` · `+0x44a` (import). test.exe 는 일반 프로세스라 이 갈래를 안 탈 것 `[추측]` (cmd 가 AppContainer 속성을 안 넘김 — 확인한 줄 없음).
  - (`UnhandledExceptionFilter` 지도에도 같은 함수가 깊이 5 에 보이는데, 처리 안 된 예외 길이라 초기화 밖.)
- 커널 안에서는 `SeQuerySecurityDescriptorInfo` 가 `NtCreateUserProcess` 지도 깊이 6 ~ 7 에 보인다 (`CmpSecurityMethod` · `IopGetSetSecurityObject` · `SeDefaultObjectMethod` · `ObQuerySecurityDescriptorInfo` 경유, `_map/NtCreateUserProcess_edges.tsv`). 실제로 그 길을 타는지는 모른다 `[공백]`. 이름 있는 객체를 열 때의 접근 검사 (`ObpCheckTraverseAccess` → `ObpGetObjectSecurity`) 는 test.exe 초기화에도 있을 법하지만 `[추측]` — 기본 함수 형이면 헤더 SD 를 직접 읽어 `SeQuerySecurityDescriptorInfo` 를 안 탄다 (위).
- 그래서 **흐름도 그림은 안 고친다.** 줄은 flow.md 3장 새 절에만.

### 호출 관계

```
nt!NtQuerySecurityObject (0x850d60) (Handle, SecurityInformation, SD 버퍼, Length, *LengthNeeded)          [정적]
 │  +0x24/+0x2d 이전 모드 — 유저면:
 │    +0x3e ~ +0x5c  *LengthNeeded 읽고-다시 쓰기 · +0x65 길이 0 → 버퍼 검사 건너뜀
 │    +0x6a 맞춤 → ExRaiseDatatypeMisalignment (+0xae) · 범위 → ExRaiseAccessViolation (+0xa8)
 │  +0xc7 ~ +0x124  DesiredAccess ← 비트 (O G D LABEL … = READ_CONTROL · SACL = ACCESS_SYSTEM_SECURITY)
 ├─ (1) +0x145  ObReferenceObjectByHandle(h, DesiredAccess, NULL, …)
 │        └─ ObpReferenceObjectByHandleWithTag +0x514/+0x516 → 0xC0000022      실패 → +0x200 (LengthNeeded 안 씀)
 │  +0x181  R10 = 형 객체 +0x98 (SecurityProcedure)
 ├─ (2) +0x1d4  SeDefaultObjectMethod (EDX=1 질문)      == SeDefaultObjectMethod 일 때 (Directory · Token)
 │        ├─ +0x7dd  ObpReferenceSecurityDescriptor (헤더 −0x30 의 SD 빌림)
 │        ├─ +0x7fd  SeQuerySecurityDescriptorInfo (0x8504f0)
 │        │        │  +0x42 SD NULL → +0x430 (0x14 · 20B)
 │        │        │  +0x110 BACKUP → +0x3ef 마스크 넓히기
 │        │        │  크기 모으기 (+0xee 0x14 …) — SACL 은 +0x383 RtlpFilterSacl(…, NULL) 크기만
 │        │        │  +0x1a9 *길이 = 필요 · +0x1af JA → +0x3a2 0xC0000023
 │        │        │  +0x1b5 머리 (Revision 1 · Control 0x8000)
 │        │        ├─ +0x1fa  복사 Owner → [+4]
 │        │        ├─ +0x24a  복사 Group → [+8]
 │        │        ├─ +0x2b9  복사 DACL  → [+0x10]
 │        │        └─ +0x320  RtlpFilterSacl(SACL, 자리, &크기, 마스크 & 0x1f8) → [+0xc]
 │        │                   +0x9d 머리 AclSize 8 · AceCount 0 → 고른 ACE 마다 더함 (0x11 ← 0x10 …)
 │        └─ +0x809 ~ +0x831 / +0x9f6  빌린 참조 되돌리기
 ├─ (2') +0x238 IopGetSetSecurityObject · +0x258 CmpSecurityMethod · +0x265 CFG 간접 (그 밖 형)
 │  +0x1e2  *LengthNeeded = 필요 길이      (성공 · 0xC0000023 모두)
 └─ +0x1f9  ObfDereferenceObject

길 밖: ObQuerySecurityDescriptorInfo (WmipSecurityMethod 쪽) · ObpGetObjectSecurity (접근 검사 쪽)
       · SeQuerySecurityAccessMask (IoCheckFunctionAccess) · SeQueryMandatoryLabel

형 만들 때 (부팅):
nt!ObInitSystem +0x2e4 ──┐
nt!SepInitializationPhase0 +0x18 → SepTokenInitialization +0x8d ──┤
                         └→ ObCreateObjectType +0xc → ObCreateObjectTypeEx
                                +0x4af [초기화+0x58] == 0 ? → +0x4bc 형+0x98 = SeDefaultObjectMethod
```

## 확인된 것

- `[정적]` 비트 → 권한 규칙은 `NtQuerySecurityObject +0xc7 ~ +0x124` 에 인라인. LABEL `0x10` = `READ_CONTROL` (`+0xd8 TEST R8B,0x17`). SACL = `ACCESS_SYSTEM_SECURITY` 만. `0x20001` 핸들로 `0x17` 통과.
- `[정적]` 권한 모자람 = `0xC0000022` (`ObpReferenceObjectByHandleWithTag +0x693`), 그때 `*LengthNeeded` 안 씀.
- `[정적]` 크기만 · 작음 = `0xC0000023` (`SeQuerySecurityDescriptorInfo +0x3a2`), `*LengthNeeded` 는 씀 (`+0x1a9` → `NtQuerySecurityObject +0x1e2`). 필요 길이 ≥ `0x14`.
- `[정적]` 출력 = 머리 20B (`Revision 1` · `Control 0x8000 | 옮긴 비트`) + Owner → Group → DACL → SACL, 4B 맞춤, 칸 `+4` · `+8` · `+0x10` · `+0xc`.
- `[정적]` LABEL 만 = SACL 에서 형 `0x11` 만 (`RtlpFilterSacl +0x114`), 새 머리 `AclSize = 8 + 합` · `AceCount = 개수` (`+0x9d` · `+0xd7` · `+0xe0`).
- `[정적]` Directory · Token 형 `SecurityProcedure` = `SeDefaultObjectMethod` (형 만들 때 0 → `ObCreateObjectTypeEx +0x4bc`).
- `[정적]` `SeDefaultObjectMethod` 질문 갈래는 `SeQuerySecurityDescriptorInfo` 를 직접 부른다 (`+0x7fd`). `ObQuerySecurityDescriptorInfo` · `ObpGetObjectSecurity` · `SeQuerySecurityAccessMask` · `SeQueryMandatoryLabel` 은 길 밖.
- `[정적]` `NtDuplicateToken` 이 SD 없이 불리면 붙이는 SACL ACE = 형 `0x14` (`RtlAddProcessTrustLabelAce +0x3c`).
- `[정적: 지도]` test.exe 초기화 지도에 `NtQuerySecurityObject` 를 부르는 줄 없음.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① (자리 바로 적음) 결과서 질문 4 "`SeQuerySecurityDescriptorInfo +0x1a1 MOV [RAX],EDI`" — `+0x1a1` 은 `MOV RAX,[RSP+0xd0]` (포인터 읽기), 쓰는 줄은 **`+0x1a9 MOV [RAX],EDI`** 다. 질문 3 의 "`+0x1a1`/`+0x1a9`" 표기는 맞다. 결론 같음 (비교 `+0x1ab` 보다 앞).
- ② (자리 바로 적음) 결과서 질문 4 표 "`*LengthNeeded` 쓴다 (`+0x1e2`)" 의 `+0x1e2` 는 `NtQuerySecurityObject` 쪽이다 (`SeQuerySecurityDescriptorInfo +0x1e2` 는 Owner 갈래 `JZ`). 표 머리에 함수 이름이 없어 헷갈릴 수 있어 이 문서는 함수 이름을 붙였다.
- ③ (보탬) Owner · Group · DACL 복사 함수는 alias 다 — rva `0x6c3640`, Ghidra 가 고른 이름 `RtlCopyMemory` (같은 주소 이름 4개, PDB `--lookup` 은 `memcpy` · `memmove`). 결과서는 `memcpy` 로 적음. 하는 일은 같다 (복사).
- ④ (보탬) **DACL · SACL `Control` 비트는 물음 비트만 보고 먼저 켠다** — `+0x290 ~ +0x29f` (DACL), `+0x2e5 ~ +0x2ee` (SACL) 가 복사 조건 검사 (`+0x2a3` · `+0x2f3`) 보다 앞. 결과서 질문 3 표 "비트 `0x4` 가 켜지면 `Control` 비트 먼저" 와 같은 말, 질문 2 (c) 의 근거 줄.
- ⑤ (보탬) BACKUP 넓히기는 넓힌 마스크를 부른 쪽 `SecurityInformation` 칸 (`[R8]`) 에 되쓴다 (`+0x3f6` · `+0x40a` · `+0x465`). 유저 메모리는 아니다 (`NtQuerySecurityObject` 가 넘긴 칸) `[추측: 칸 출처는 안 따라감]`.
- ⑥ (보탬) Directory 형 초기화 구조체는 바로 앞 `ObpTypeObjectType` (`+0x276`) 을 만들 때 쓴 구조체를 그대로 다시 쓴다 (`+0x223 memset` 한 번). 두 번 다 `+0x58` 에는 안 쓴다 (`+0x22f ~ +0x2df`).
- ⑦ (보탬) test.exe 토큰 길 `SeSubProcessToken +0x80e ObInsertObjectEx` · `+0x847 SepAppendAceToTokenObjectAcl` · `+0x85b SepFinalizeTokenAcls` (`_calls.tsv`). 결과서 질문 6 은 `NtDuplicateToken` · `SepCreateTokenEx` 만 봤다.
- ⑧ (보탬) test.exe 길에 있나 — 지도 검색 (위 절). 결과서엔 없음.

## 아직 확인 필요

**동적** (결과서 "동적으로 확인할 것" 1 ~ 7 + 보탬. **Q181 에 붙일 것**, VM 은 이번에 안 함):

- `\RPC Control` 은 전역 객체라 **test.exe 가 없어도 볼 수 있다** `[추측]` — `!object \RPC Control` 로 객체 주소를 얻으면 된다. 토큰 객체는 test.exe 의 `EPROCESS.Token` 으로 볼 수 있다 (rpcrt4 는 test.exe 가 안 올림 — run44 모듈 6개 `[관찰]`). 그래서 ① ② 는 **Q104 VM 단계에 붙일 수 있고**, ③ ④ ⑤ ⑦ 은 rpcrt4 를 쓰는 다른 프로세스 (또는 일부러 `NtQuerySecurityObject` 를 부르는 샘플) 가 필요하다.
- ① **SD 바이트 뜨기** (질문 2 (a) / (b) 가르기): `\RPC Control` 디렉터리 객체 · test.exe 토큰 객체. 헤더 SD 칸 = 객체 `-8` 의 값에서 아래 4비트 뗀 포인터 — `!sd (poi(<객체>-8) & 0xfffffffffffffff0)` (결과서). 볼 것: `Control` 의 `SE_SACL_PRESENT`, SACL 안 `0x11` ACE (있으면 `AceFlags` · `Mask` · `S-1-16-…`), `0x14` ACE, 소유자 · 그룹 · DACL 크기. 멈춤점을 쓴다면 `nt!SeQuerySecurityDescriptorInfo+0x3f` (`rax` = 객체 SD), 조건 = 그 프로세스.
- ② **형 `+0x98` 실행 값**: `dq poi(nt!ObpDirectoryObjectType)+0x98 L1` · `dq poi(nt!SeTokenObjectType)+0x98 L1` 이 `x nt!SeDefaultObjectMethod` 와 같은가 (질문 5 확정). 멈출 필요 없음.
- ③ **진짜 응답 대조**: `nt!NtQuerySecurityObject+0x1d9` (`eax` = status) · `+0x1e2` (`eax` = 필요 길이, `rdi` = 유저 포인터). `0xC0000023` 과 길이가 공식 `0x14 + …` 과 같은가.
- ④ **충분한 버퍼일 때 바이트**: `nt!SeQuerySecurityDescriptorInfo+0x46e` 에서 `db @rsi L?(@rdi-@rsi)` — 차례 · `Control` 이 표와 맞나.
- ⑤ **라벨 고르기**: `nt!RtlpFilterSacl` 입구 (`rcx` = SACL, `r9d` = 마스크 — `0x17` 이면 `0x10`) · `+0x97`. 돌아온 뒤 `SeQuerySecurityDescriptorInfo+0x325` 에서 `dd @rsp+0x24 L1`.
- ⑥ **입구 예외 때 status** (`[추측]` 예외 코드): 잘못된 `LengthNeeded` 포인터로 불러 `0xC0000005` 인가 (`+0x3e ~ +0x5c`).
- ⑦ **권한 비교**: `nt!ObpReferenceObjectByHandleWithTag+0x516` (`ebp` = 요구, `eax` = ~가진 권한) — `0x20001` 에 `0x17` 이 통과하나. 이 함수는 모든 핸들 참조에 걸리니 조건 필수.
- ⑧ (보탬) test.exe 토큰 객체 SD 를 붙이는 줄 — `nt!SeSubProcessToken+0x813` (`ObInsertObjectEx` 반환) · `+0x84c` · `+0x860` 뒤에 ① 의 `!sd` 를 다시 찍어, 어느 단계가 무엇을 더하나. `SeSubProcessToken` 은 `NtCreateUserProcess` 안 (부모 cmd 스레드) 이라 test.exe 이름 조건은 run44 방식으로.

**정적** (결과서 "다음 요청 후보" + 보탬):

- ① (= **Q180d**, sast/96 진행 중) `SeAssignSecurityEx2` / `RtlpNewSecurityObject` — 새 객체 SD 에 `0x11` 라벨 ACE 를 기본으로 붙이나 (질문 6 `[공백]`).
- ② `NtOpenDirectoryObject("\RPC Control", 0x20001)` 의 접근 검사 (`ObpCheckObjectReference` · `ObpGetObjectSecurity` 쪽, `SeAccessCheck` 는 이름만) — 에뮬레이터가 핸들에 `0x20001` 을 줄 조건.
- ③ (보탬) `SeSubProcessToken +0x80e ~ +0x85b` (`ObInsertObjectEx` · `SepAppendAceToTokenObjectAcl` · `SepFinalizeTokenAcls`) — test.exe 토큰 객체 SD 에 무엇을 붙이나. ① 과 같이 보면 좋다. 짐 지도 후보: 토큰 · 프로세스 객체 헤더 SD (지금 baggage.md 에 칸 없음).
- ④ (보탬, 작음) `+0x98` 을 형을 만든 뒤 덮는 줄이 있나 — ntoskrnl 에서 `[reg+0x98]` 쓰기 + 형 객체 전역 읽기 훑기. 동적 ② 가 더 빠르다.
- ⑤ (낮음) `NtSetSecurityObject` 쓰기 길 (이번 범위 밖).

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 안 고침 — test.exe 초기화 지도에 이 syscall 이 없다, 위 "test.exe 길에 있나"):
  - 기준: ntkrnlmp pdb 줄에 run97 몫, 근거 로그 run97 줄 (사진 안 씀).
  - 흐름도 앞 글: run97 문단 하나.
  - 2.1 run49 토큰 줄 아래: (run97) `SeSubProcessToken` 의 SD 붙이기 줄 (`_calls.tsv`, 본문 안 읽음).
  - 3장 새 절 **"보안 정보 묻기 — `nt!NtQuerySecurityObject` → `SeDefaultObjectMethod` → `SeQuerySecurityDescriptorInfo` → `RtlpFilterSacl` (run97)"**.
  - 4장: run97 새 줄 셋 (동적 · 정적 · PE_emulator 와 다르게 적은 곳).
  - 5장: run 표 · 03_sprint 요청서 번호 run97 줄.
- 짐 지도: **고치지 않음** — 객체 헤더 SD 는 지금 짐 칸이 아니고, 이번 결과서는 test.exe 객체의 SD 값을 말하지 않는다. 후보만 4장 정적 ③ 에 적음.
- 이전 run 문서: 정정 없음.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점** `[추측]`: 이번에 읽은 줄 안에 디버거 칸을 읽는 줄은 없다. 단 권한 비교 · 입구 검사는 **이전 모드가 유저일 때만** 한다 — 디버거에서 `!sd` 로 읽는 건 이 길을 안 탄다 (값만 같다).
- **함정 후보** (→ windbg-kd-runbook pitfalls 에 추가 제안): 객체 헤더 SD 칸 (객체 `-8`) 은 아래 4비트가 참조 수 꼴이라 그대로 `!sd` 에 넣으면 틀린 주소가 된다 — `& ~0xf` 로 떼야 한다 (결과서 동적 1). `ObpReferenceObjectByHandleWithTag` · `SeQuerySecurityDescriptorInfo` 는 모든 프로세스 · 모든 핸들에 걸리므로 조건 없이 멈추면 VM 이 사실상 멈춘다.
- 도구 (결과서 "스크립트 문제"): 없음.

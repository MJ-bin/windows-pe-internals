# 93 rpc-sd-query — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q180a** (= Q180 ①, PE_emulator 요청 34 의 1). 사용자 결정 2026-11-15: Q176a · Q178a 다음 둘째로 이 요청. Q180 을 그 자리에서 Q180a (① ntoskrnl) · Q180b (② smss, sast/94 와 나란히) · Q180c (③ rpcrt4, 낮음) 로 나눔. **정적만** — VM 몫 (SD 바이트 뜨기) 은 Q181 로 따로 있음, 결과서엔 "동적으로 확인할 것" 으로만.
- 빌드: 26100.9457. ntoskrnl analyze 됨 (PDB 붙음). `_index.tsv`: `NtQuerySecurityObject` rva `0x850d60` (618 B) · `ObQuerySecurityDescriptorInfo` `0x8502e8` (141 B) · `SeQuerySecurityDescriptorInfo` `0x8504f0` (1153 B) · `SeDefaultObjectMethod` `0x87e530` (2655 B) · `ObpGetObjectSecurity` `0x879e00` (718 B) · `SeQuerySecurityAccessMask` `0xa0e3d8` (91 B) · `SeQueryMandatoryLabel` `0x97ac98` (153 B). 앞의 다섯은 `.c`·`.asm` 아직 없음.
- 원문: `../PE_emulator/_workspace/05_tuning_planner/264_internals-rpc-security.md` (읽어도 됨 — 아래에 필요한 것은 옮김).

## 배경 (PE_emulator 쪽)

- `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]` rpcrt4 가 RPC 런타임을 켤 때 (`UuidToStringA`·`WinHttpOpen`·`LookupPrivilegeValueW` 등 안) 다음을 한다. 383 샘플 · 29528 줄, 모두 같은 꼴:
  - `NtQueryInformationToken(-6, class 0x1d, 4 바이트)` → `NtOpenDirectoryObject("\RPC Control", 0x20001)` (`READ_CONTROL | DIRECTORY_QUERY`) → **`NtQuerySecurityObject(h, 0x17, NULL, 0, &len)`** (OWNER `0x1` · GROUP `0x2` · DACL `0x4` · LABEL `0x10`, 크기만 묻기) → `NtClose(h)`. 버퍼를 준 둘째 부름은 0 줄 — 에뮬레이터가 "성공 0 + 길이 칸 안 씀" 으로 넘겨서 부른 쪽이 바로 실패 (`UuidToStringA` → `0xe`).
  - 따로 토큰: `NtOpenProcessToken(자기, 0xa0008)` → `NtQuerySecurityObject(h, 0x10, NULL, 0, &len)` 한 번 → `advapi32!GetSecurityInfo` 0 (53 샘플).
- 에뮬레이터가 모르는 것: LABEL 비트를 커널이 어떻게 답하나 (권한 · 고르는 ACE · 바이트 꼴 · 크기만 묻기 status).

## 알고 싶은 것

1. **(가) 비트 → 필요한 핸들 권한 표**: `NtQuerySecurityObject` (또는 `SeQuerySecurityAccessMask`) 가 `SecurityInformation` 비트마다 요구하는 권한. 적어도 `0x1` · `0x2` · `0x4` · `0x8` · `0x10`, 그 밖에 보이는 비트 (`0x20` ATTRIBUTE · `0x40` SCOPE · `0x10000` BACKUP 등) 도 표로. **`0x20001` 핸들로 `0x17` 이 통과하나 = LABEL 이 `READ_CONTROL` 만으로 되나** (예/아니오 + 줄). 권한 모자라면 status.
2. **(나) LABEL `0x10` 만 있고 SACL `0x8` 이 없을 때**: `SeQuerySecurityDescriptorInfo` 가 객체 SACL 에서 어느 ACE 를 골라 담나 (ACE 형 번호 — `SYSTEM_MANDATORY_LABEL_ACE_TYPE` `0x11` 뿐인가, `0x12`~`0x14` 같은 다른 형도 같이 가나). 담을 때 SACL 머리 (`AclRevision` · `Sbz1` · `AclSize` · `AceCount` · `Sbz2`) 를 어떻게 다시 만드나. **라벨 ACE 가 없으면** (객체 SACL 없음 / SACL 은 있는데 라벨 없음) `Control` 의 `SE_SACL_PRESENT` 와 `Sacl` 오프셋은 무엇인가 (0 인가, 빈 SACL 인가).
3. **(다) 나오는 바이트 꼴**: self-relative 머리 `0x14` 뒤 구성요소 순서 (Owner · Group · Sacl · Dacl 중 어떤 차례로 붙나), 맞춤 (4 바이트?), `Control` 에 그대로 옮기는 비트와 새로 켜는 비트 (`SE_SELF_RELATIVE` `0x8000` 등). 안 물은 구성요소의 오프셋은 0 인가.
4. **(라) 크기만 묻기** (버퍼 `NULL` · 길이 `0`): status (`STATUS_BUFFER_TOO_SMALL` `0xc0000023` 인가), **`*LengthNeeded` 를 쓰는지와 쓰는 자리** (함수 + 오프셋). 버퍼가 작을 때 버퍼에 쓰는 것이 있나. 성공일 때도 `*LengthNeeded` 를 쓰나. 사용자 포인터 검사 (`ProbeForWrite` 류) 순서.
5. **(마) 형마다 `SecurityProcedure`**: `ObpGetObjectSecurity` / `ObQuerySecurityDescriptorInfo` 를 타는 길과 형의 `SecurityProcedure` 를 부르는 길이 어디서 갈리나. **Directory 형과 Token 형이 같은 길을 타나** — 각 형의 `SecurityProcedure` 가 `SeDefaultObjectMethod` 인가 (형 초기화 자리 `ObCreateObjectType` 의 인자 줄, 또는 형 객체 칸 오프셋으로). 다르면 Token 쪽 함수 이름만.
6. (작게, 이름만) 토큰 객체를 만들 때 **객체 SD 를 붙이는 자리** — `SepDuplicateToken`·`SepCreateToken` 류가 `ObInsertObject` 에 SD 를 주나, 형 기본 SD 인가. 라벨 ACE 가 붙을 여지가 코드에 있나 (줄만, 값은 VM 몫).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtQuerySecurityObject$` | 1 | 질문 1·4·5 입구 |
| ntoskrnl | `^SeQuerySecurityAccessMask$` | 0 | 질문 1 — 비트 → 권한 (길에 있는지 먼저) |
| ntoskrnl | `^ObQuerySecurityDescriptorInfo$` | 1 | 질문 2·3·4 |
| ntoskrnl | `^SeQuerySecurityDescriptorInfo$` | 1 | 질문 2·3·4 핵심 |
| ntoskrnl | `^SeDefaultObjectMethod$` | 1 | 질문 5 — QuerySecurityDescriptor 갈래만 (Set·Assign·Delete 갈래는 이름만) |
| ntoskrnl | `^ObpGetObjectSecurity$` | 0 | 질문 5 |
| ntoskrnl | `^SeQueryMandatoryLabel$` | 0 | 질문 2 — 길에 있는지 먼저, 없으면 "길 밖" 한 줄 |
| ntoskrnl | `^ObCreateObjectType$` 를 Directory·Token 형으로 부르는 자리 (`ObpCreateDirectoryObjectType`·`SepTokenInitialization` 류 — `_index.tsv`/`pdb_aliases.py` 로 이름 확인) | 0 | 질문 5 — `SecurityProcedure` 칸 값만 |

- 범위: 커널이 **묻기에 어떻게 답하나** 만. `NtSetSecurityObject` (쓰기 길), 접근 검사 (`SeAccessCheck`), 감사 (`SeAuditing*`) 는 이름만.
- smss 의 `\RPC Control` 만들기는 **sast/94 (Q180b)** 몫 — 여기선 안 본다. rpcrt4 는 Q180c.
- 공용 부품 (`memcpy`, `RtlLengthSid`, `ExAllocatePool*`, 락, `ProbeForWrite`) 은 이름만. 단 질문 4 의 쓰기 순서에 닿는 `ProbeForWrite`·`__try` 줄은 오프셋만 적는다.
- 함수가 깊으면 질문 2·3·4 줄기만 끝까지, 나머지는 이름.

## 지도에서 본 것

- ntoskrnl `_map/` 에 이 길의 지도 없음. 필요하면 `run_ghidra.sh map` 으로 `NtQuerySecurityObject` 진입 지도를 먼저 만들고 깊이·caller·직접 호출을 결과서에 옮긴다.

## 이미 아는 것

- `[정적]` (에뮬레이터 쪽 syscall 카드, MS 문서 수준): O / G / D 는 `READ_CONTROL`, SACL 은 `ACCESS_SYSTEM_SECURITY`, 버퍼가 작으면 `STATUS_BUFFER_TOO_SMALL`. LABEL 은 카드에 없음. 조건은 코드로 볼 것.
- `[정적]` run49: 토큰 안 칸 `IntegrityLevelIndex` · `MandatoryPolicy` — 이건 토큰 **안** 값이고, 여기서 묻는 건 토큰 **객체의 SD** 라벨 (다를 수 있음).
- 이 저장소엔 `RPC Control` 0 건 (`runs/`, `ms/decomp/ntoskrnl`).

## 받고 싶은 것

- 결과서 `_workspace/sast/93_rpc-sd-query_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: 비트 → 권한 표 + `0x20001`·`0x17` 예/아니오.
- 질문 2·3: **바이트 만들기 규칙** — 에뮬레이터가 객체 SD (소유자 · 그룹 · DACL · SACL) 에서 `0x17`·`0x10` 대답 바이트를 만들 수 있게 순서대로 (머리 → 구성요소 순서 → 맞춤 → `Control` 비트 → 라벨 고르기 → 없을 때).
- 질문 4: status · `*LengthNeeded` 표 (크기만 / 작음 / 충분 세 경우).
- 질문 5: 형 → `SecurityProcedure` 표 (Directory · Token).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (지금 우리 = "성공 0 + 길이 칸 안 씀", LABEL 비트 모름, 객체 SD 는 소유자·그룹·DACL 뿐 — 라벨 칸 없음).
- 끝에 **"동적으로 확인할 것"** (Q181 에 넘길 멈춤점) 과 **"다음 요청 후보"**.

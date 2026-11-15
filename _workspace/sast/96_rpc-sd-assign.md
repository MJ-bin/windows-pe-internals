# 96 rpc-sd-assign — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q180d** (= Q180 의 마지막 조각, PE_emulator 요청 34 의 1·2 를 "바이트 규칙" 으로 잇는 몫). sast/93 결과서 "다음 요청 후보" 1 과 sast/94 결과서 "다음 요청 후보" 1 이 같은 함수를 가리켜서, Q180 안에서 그 자리에 나눔 (2026-11-15). **정적만.**
- 빌드: 26100.9457. ntoskrnl analyze 됨 (PDB 붙음). `_index.tsv`: `SeAssignSecurityEx2` rva `0x43d980` (133 B) · `SeAssignSecurityEx` `0xa48960` (68 B) · `SeAssignSecurity` `0x8cd230` (142 B) · `RtlpNewSecurityObject` `0x8cea00` (8746 B) · `RtlpInheritAcl2` `0x8ce350` (1694 B) · `ObpAssignSecurity` `0x87e330` (492 B) · `ObInsertObjectEx` `0x87da20` (2289 B).
- 주의: 다른 decompiler 가 같은 시간에 **rpcrt4** (sast/95) 를 돌릴 수 있다 — rpcrt4 쪽 파일·Ghidra 프로젝트는 건드리지 않는다.

## 왜

- sast/94 (`_workspace/sast/94_rpc-dir-create_result.md` 1-5 ~ 1-7): smss 가 `\RPC Control` 을 `NtCreateDirectoryObject` (`SmpConfigureObjectDirectories +0xf1`, 속성 `0xd0`, 접근 `0xf000f`) 로 만들며 넘기는 **Liberal SD** = 소유자·그룹 없음, DACL 허용 ACE 7개 (`0xE0000000` generic 마스크 · 상속 플래그 `0x0b` 섞임, `AclSize 0xb8`), SACL 라벨 ACE 1개 (`0x11`, 마스크 1, `S-1-16-4096`, `AclSize 0x20`).
- sast/93 (`_workspace/sast/93_rpc-sd-query_result.md`): 묻기는 **객체에 붙은 SD** 를 `SeQuerySecurityDescriptorInfo` 로 잘라 줌. 하지만 **넘긴 SD 가 객체에 붙을 때 무엇이 바뀌는지** (소유자·그룹 채우기, generic → 구체 권한, 상속 전용 ACE, `AclSize` 줄이기, `Control` 비트) 는 아무도 안 봄 → 에뮬레이터가 대답 바이트를 만들 수 없다.
- 같은 함수가 토큰 객체 SD (sast/93 질문 6 `[공백]`: 라벨 ACE `0x11` 이 붙나) 도 정한다.

## 알고 싶은 것

1. **사슬**: `NtCreateDirectoryObject` → `ObpCreateDirectoryObject` → `ObInsertObjectEx` → (`ObpAssignSecurity` →) `SeAssignSecurityEx2` → `RtlpNewSecurityObject` 가 맞나 (오프셋). Directory 형 `SecurityProcedure` = `SeDefaultObjectMethod` (sast/93) 의 Assign 갈래가 끼나.
2. **소유자·그룹**: 넘긴 SD 에 없을 때 어디서 채우나 (토큰 `+?` 의 `DefaultOwner` / `PrimaryGroup` 칸 — 만든 쪽 토큰 = smss 의 SYSTEM 토큰 `[추측]`). 결과 SID 를 적을 수 있으면 (`S-1-5-32-544` 인가 `S-1-5-18` 인가 — 토큰 칸 이름까지만, 값은 VM 몫).
3. **DACL 가공 (명시 SD 를 줬을 때)**: 
   - generic 비트 (`0xE0000000`, `0x10000000`) 를 형의 `GenericMapping` 으로 바꾸나 — **어느 ACE 에** (상속 전용 `INHERIT_ONLY 8` 이 붙은 ACE 도 바꾸나, 그대로 두나).
   - 상속 전용 ACE 를 지우나 · 그대로 두나. `AclSize` 를 다시 계산하나 (`0xb8` 그대로인가).
   - Directory 형 `GenericMapping` 값 (형 초기화 줄 또는 `ObpDirectoryObjectType` 형 객체 칸 — 숫자 넷).
   - 부모 (루트 `\` 디렉터리) 에서 상속받는 ACE 가 섞이나 (명시 DACL 이 있을 때).
4. **SACL / 라벨 가공**: 명시 SACL 의 라벨 ACE (`0x11`, 마스크 1, `S-1-16-4096`) 를 그대로 두나. 만드는 쪽에 `SeSecurityPrivilege` 가 없으면 SACL 을 버리나, 라벨만은 따로 다루나 (`SE_SACL_PRESENT`·`Sacl` 결과). 명시 라벨이 **없을 때** 기본 라벨 ACE 를 붙이나 (토큰 무결성으로) — 붙이면 조건 (중간 이상이면 안 붙임 등).
5. **`Control` 결과 비트**: `SE_DACL_PRESENT` · `SE_SACL_PRESENT` · `SE_DACL_AUTO_INHERITED` · `SE_*_DEFAULTED` · `SE_SELF_RELATIVE` 중 붙는 것.
6. **토큰 객체 쪽** (sast/93 질문 6 이어서): `SepDuplicateToken +0x158` / `SepCreateTokenEx +0x5dc` → `ObInsertObjectEx +0x325 SeAssignSecurityEx2` 에서 토큰 객체 SD 에 라벨 ACE `0x11` 이 붙는 조건 (위 4 의 규칙으로 답이 나오면 한 줄).
7. **바이트 다시 만들기 (가장 중요)**: 1~5 로 sast/94 Liberal SD 를 넣으면 객체에 붙는 SD 가 어떻게 되는지 순서대로 (소유자 → 그룹 → DACL ACE 표 (마스크 바뀐 값) → SACL → `Control`). 값이 실행 때만 정해지는 칸 (토큰 SID) 은 `[VM]` 표시.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^SeAssignSecurityEx2$` | 1 | 질문 1 입구 |
| ntoskrnl | `^RtlpNewSecurityObject$` | 1 | 질문 2·3·4·5 핵심 (8746 B — 줄기만: 명시 DACL·SACL 이 있고 부모가 디렉터리일 때, 소유자·그룹 기본값, generic 바꾸기, 라벨) |
| ntoskrnl | `^RtlpInheritAcl2$` | 0 | 질문 3 — 명시 DACL 일 때 타나만 |
| ntoskrnl | `^ObpAssignSecurity$`, `^ObInsertObjectEx$` | 0 | 질문 1 — 부르는 자리·인자 (부모 SD, 형 `GenericMapping`, `IsDirectoryObject`) |
| ntoskrnl | `^SeDefaultObjectMethod$` | 0 | sast/93 에서 뽑음 — Assign 갈래 자리만 |
| ntoskrnl | `^ObpCreateDirectoryObject$`, Directory 형 초기화 (`ObInitSystem`·`ObpCreateDirectoryObjectType` 류 — 이름 확인) | 0 | 질문 3 — `GenericMapping` 값 |

- 범위: **새 객체에 SD 가 붙는 순간** 까지. 접근 검사 (`SeAccessCheck`), 감사, `NtSetSecurityObject` 길은 이름만.
- `RtlpNewSecurityObject` 가 크면 질문 7 줄기 (명시 SD + 디렉터리 부모) 만 끝까지, 다른 갈래 (자식 상속 계산 전부, 객체 형 GUID ACE, 조건부 ACE, 리소스 속성) 는 갈림 줄만.
- 공용 부품 (`RtlLengthSid`, `RtlCopySid`, 할당, `RtlpApplyAclToObject` 같은 이름이 generic 바꾸기면 그건 본문까지) — generic 바꾸기 함수는 줄기 안이다.

## 지도에서 본 것

- ntoskrnl `_map/NtQuerySecurityObject_*` 은 sast/93 이 만듦. 이 사슬 지도는 없음 → 필요하면 `run_ghidra.sh map` (진입 `SeAssignSecurityEx2`) 먼저.

## 이미 아는 것

- `[정적]` sast/94: Liberal SD 내용 (위 "왜"), Liberal 몸통 `smss+0x32680`, 같은 SD 객체를 `\Windows` 와 같이 씀.
- `[정적]` sast/93: Directory·Token 형 `SecurityProcedure` = `SeDefaultObjectMethod`; 묻기 출력 바이트 규칙 (머리 `0x14` → Owner → Group → DACL → SACL, 4 바이트 맞춤, `Control` 옮기는 비트); LABEL 만 물으면 `RtlpFilterSacl` 이 형 `0x11` 만 담음.
- `[정적]` sast/93: 토큰 SD — `NtDuplicateToken` 은 SD 가 없을 때만 `SepAppendAceToTokenObjectAcl` (`+0x21e`) · `SepSetProcessTrustLabelAceForToken` (`+0x338`, 형 `0x14`).

## 받고 싶은 것

- 결과서 `_workspace/sast/96_rpc-sd-assign_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 7: **`\RPC Control` 객체 SD 다시 만들기 표** + 그걸 sast/93 규칙으로 `0x17` 을 물었을 때 돌아올 **바이트 (hex)**·길이 (토큰 SID 칸은 자리만). 같은 식으로 `0x10` (LABEL 만) 대답.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄.
- 끝에 **"동적으로 확인할 것"** (Q181 에 넘길 — 실제 SD 와 대조할 칸) 과 **"다음 요청 후보"**.

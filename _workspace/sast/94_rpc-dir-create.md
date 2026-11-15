# 94 rpc-dir-create — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q180b** (= Q180 ②, PE_emulator 요청 34 의 2-가 · 2-나). Q180 을 Q180a (ntoskrnl 묻기 길, sast/93 — 지금 나란히 돎) · Q180b (이것) · Q180c (rpcrt4, 낮음) 로 나눔. **정적만** — 부팅 뒤 실제 SD 바이트는 Q181 (VM) 몫, 결과서엔 "동적으로 확인할 것" 으로만.
- 빌드: 26100.9457. **smss.exe 는 아직 analyze 안 됨** (PDB 없음) → `fetch_pdb.py smss.exe` 로 PDB 받고 analyze 부터. 바이너리: `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/smss.exe` (228,824 B).
- 원문: `../PE_emulator/_workspace/05_tuning_planner/264_internals-rpc-security.md` (읽어도 됨).
- 주의: 다른 decompiler 가 같은 시간에 **ntoskrnl** (sast/93) 을 돌린다 — ntoskrnl 쪽 파일·Ghidra 프로젝트는 건드리지 않는다. 이 요청에서 ntoskrnl 을 봐야 할 것 (질문 3) 은 `_index.tsv`·`_calls.tsv` 읽기만, 새 export 는 하지 않는다.

## 배경 (PE_emulator 쪽)

- 에뮬레이터는 `\RPC Control` 디렉터리 객체의 SD 를 모른다 (소유자·그룹·DACL·라벨 전부). rpcrt4 가 이 객체의 보안 정보 (`0x17` = OWNER · GROUP · DACL · LABEL) 를 묻고, 대답이 없어서 실패한다 (383 샘플).
- `[수집 — 에뮬레이터 쪽 레지스트리 덤프]` `HKLM\SYSTEM\ControlSet001\Control\Session Manager` 에 `ObjectDirectories` (REG_MULTI_SZ — `\Windows` · `\RPC Control`) · `ProtectionMode` (REG_DWORD 1). smss.exe 안에 이 네 글이 나란히 있음 (바이트 검색만 — 실마리, 근거 아님).

## 알고 싶은 것

1. **(2-가) 만드는 함수와 SD**: smss 가 `ObjectDirectories` 이름마다 디렉터리를 만드는 함수 (레지스트리 읽기 → 이름마다 `NtCreateDirectoryObject`/`…Ex` 부르는 자리, 오프셋). 그때 넘기는 `OBJECT_ATTRIBUTES.SecurityDescriptor` 를 만드는 함수와 **`ProtectionMode` 1 갈래** 의 SD 내용:
   - 소유자 · 그룹이 SD 에 있나 (없으면 만든 프로세스 토큰에서 옴 — 그렇다고 적기)
   - DACL ACE 마다: 형 (허용/거부) · SID (`S-1-5-18` · `S-1-5-32-544` · `S-1-1-0` · `S-1-5-12` · 그 밖) · 마스크 · 플래그 (상속 등) · 순서
   - SACL / 라벨 ACE 가 있나 (`SYSTEM_MANDATORY_LABEL_ACE` 수준·마스크)
   - `ProtectionMode` 0 갈래와 다르면 차이만 한 줄
   - `\RPC Control` 만 따로 다루는 줄이 있나 (이름 비교), 아니면 모든 이름에 같은 SD 인가
2. **(2-나) 만든 뒤 고치는 곳**: smss 안에서 이 디렉터리에 `NtSetSecurityObject` 를 부르는 곳이 있나. smss 밖 (부팅 중 다른 모듈 — csrss/`csrsrv`·`basesrv`, rpcss 등) 은 **이 저장소에 이미 있는 `_index.tsv`·`_calls.tsv`·문자열로 보이는 만큼만** (`RPC Control` 문자열 검색, `NtSetSecurityObject` caller). 새 모듈 analyze 는 하지 않는다 — 후보 이름만 적는다.
3. (작게) 커널이 `\RPC Control` 을 미리 만드나 — ntoskrnl `_index.tsv`·문자열에 `RPC Control` 이 있나 (읽기만). 있으면 함수 이름만 적고 "다음 요청 후보" 로.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| smss | `ObjectDirectories` 문자열을 쓰는 함수 (문자열 xref 로 찾기) | 1 | 질문 1 — 레지스트리 읽기 → 디렉터리 만들기 |
| smss | `NtCreateDirectoryObject`·`NtCreateDirectoryObjectEx` 를 부르는 함수 (caller) | 1 | 질문 1 |
| smss | `ProtectionMode` 문자열을 쓰는 함수 · SD 만드는 함수 (`RtlCreateSecurityDescriptor`·`RtlAddAccessAllowedAce*`·`RtlAddMandatoryAce`·`RtlSetDaclSecurityDescriptor`·`RtlSetSaclSecurityDescriptor` caller) | 1 | 질문 1 — ACE 내용 |
| smss | `NtSetSecurityObject` caller | 0 | 질문 2 |

- 이름은 PDB 를 붙인 뒤 `_index.tsv` 와 `pdb_aliases.py --where smss.exe <이름>` 으로 확인 (`SmpCreateDynamicEnvironmentVariables`·`SmpInitializeObjectDirectories`·`SmpCreateSecurityDescriptors` 같은 이름은 `[추측]` — 그대로 쓰지 말 것).
- SID 는 바이트로 만들어지면 (`RtlInitializeSid` + `RtlSubAuthoritySid`, 또는 상수 블롭) 값을 풀어서 `S-1-…` 꼴로 적는다.
- 범위: `\RPC Control` 의 처음 SD 까지. smss 의 다른 일 (페이징 파일, 세션, KnownDlls 등) 은 이름만. 단 KnownDlls·`\Windows` 와 SD 를 함께 만드는 함수면 그 SD 들도 표에 같이 둔다 (짐 지도에 쓸모).
- 공용 부품 (`RtlAllocateHeap`, 문자열 함수, 레지스트리 읽기 껍데기) 은 이름만.

## 지도에서 본 것

- smss 지도 없음 (analyze 전). analyze 뒤 `run_ghidra.sh map` 으로 질문 1 함수의 깊이·caller·직접 호출을 결과서에 옮긴다.

## 이미 아는 것

- 이 저장소엔 `RPC Control` 0 건 (`runs/`, `ms/decomp/ntoskrnl`).
- `[추측]` 원문: smss 가 만든다. 부팅 뒤 지금 값은 정적으로 다 못 닫을 수 있음.

## 받고 싶은 것

- 결과서 `_workspace/sast/94_rpc-dir-create_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: **SD 표** — 구성요소 (소유자 · 그룹 · DACL · SACL) 마다 값 출처, DACL ACE 표 (순서 · 형 · SID · 마스크 · 플래그), 라벨 ACE 있나. 가능하면 **self-relative 바이트를 다시 만들 수 있는 수준** (ACE 순서·크기).
- 질문 2·3: 예/아니오 + 줄 (또는 후보 이름).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (지금 우리 = `\RPC Control` 객체에 SD 를 안 채움).
- 끝에 **"동적으로 확인할 것"** (Q181 에 넘길 것 — 부팅 뒤 실제 SD, 고유 SID 섞인 ACE) 과 **"다음 요청 후보"**.

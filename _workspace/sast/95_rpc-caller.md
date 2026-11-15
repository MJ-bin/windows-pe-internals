# 95 rpc-caller — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q180c** (= Q180 ③, PE_emulator 요청 34 의 3, 원문 급함 낮음). Q180a (sast/93, ntoskrnl 묻기 길) 는 끝, Q180b (sast/94, smss) 는 나란히 도는 중. **정적만.**
- 빌드: 26100.9457. **rpcrt4.dll 은 아직 analyze 안 됨** (PDB 없음) → `fetch_pdb.py rpcrt4.dll` 로 PDB 받고 analyze 부터. 바이너리: `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/rpcrt4.dll` (1,162,656 B). UTF-16 문자열 `\RPC Control\` · `\RPC Control\DSEC` 가 있음 (`strings -el` 로 봄 — 실마리).
- 원문: `../PE_emulator/_workspace/05_tuning_planner/264_internals-rpc-security.md` (읽어도 됨).
- 주의: 다른 decompiler 가 같은 시간에 **smss.exe** (sast/94) 를 돌린다 — smss 쪽 파일·Ghidra 프로젝트는 건드리지 않는다.

## 배경 (PE_emulator 쪽)

- `[관찰 — 에뮬레이터 로그, 진짜 Windows 아님]` 가장 짧은 꼴 (`r04 2ed5ce549014 seq 289 → 312`): `rpcrt4!UuidToStringA` 들어감 → … → `NtQueryInformationToken(-6, class 0x1d, 4 바이트)` → `NtOpenDirectoryObject("\RPC Control", 0x20001)` → `NtQuerySecurityObject(h, 0x17, NULL, 0, &len)` → `NtClose(h)` → `UuidToStringA` 가 **`0xe`** 를 돌려줌 (`NtClose` 와 반환 사이 syscall 0). 에뮬레이터 대답은 "status 0 + `len` 안 씀" 이었다.
- 같은 꼴이 `WinHttpOpen` · `LookupPrivilegeValueW/A` · `UuidFromStringA` · `gethostbyname` · `SHGetKnownFolderPath` · `CoInitializeSecurity` 안에서도 남 (383 샘플).
- `[정적]` sast/93 (`_workspace/sast/93_rpc-sd-query_result.md`): 진짜 커널은 크기만 묻기에 **`0xC0000023` + `*LengthNeeded` = 필요 길이** 를 돌려줌 (`NtQuerySecurityObject +0x1db/+0x1e2`). `0x20001` 핸들로 `0x17` 통과. 바이트 꼴은 그 결과서 질문 3.

## 알고 싶은 것

1. **누가 여나**: `\RPC Control` 을 `NtOpenDirectoryObject` 로 여는 rpcrt4 함수 (문자열 xref → caller 사슬). `UuidToStringA` 에서 거기까지 닿는 사슬 (RPC 런타임 초기화 — `PerformRpcInitialization` 류 `[추측]`, 이름은 `_index.tsv`·`pdb_aliases.py --where rpcrt4.dll` 로 확인). 앞의 `NtQueryInformationToken(-6, 0x1d, 4)` (클래스 `0x1d` = `TokenIsAppContainer` `[추측]`) 을 부르는 줄과 그 결과로 갈리는 갈림.
2. **SD 에서 무엇을 보나**: `NtQuerySecurityObject(h, 0x17, …)` 를 두 번 부르는 꼴인가 (크기 → 할당 → 다시). 첫 부름 status 를 어떻게 보나 — `0xC0000023` 만 다음으로 가나, 0 이면 어디로 가나 (에뮬레이터 대답 = 0 + `len` 안 씀 → 이 길이 실패인지). 받은 SD 에서 무엇을 읽나: 소유자 SID 비교 / DACL ACE 검사 / 라벨 ACE (수준 · 마스크) — 함수 이름 (`RtlGetOwnerSecurityDescriptor`·`RtlGetSaclSecurityDescriptor`·`RtlGetAce`·`RtlEqualSid` 등) 과 비교 상수.
3. **언제 `0xe` 로 가나**: `UuidToStringA` 가 `0xe` (`ERROR_OUTOFMEMORY` `[추측]`) 를 돌려주는 줄 — 초기화 실패를 그대로 옮기나, 따로 정하나. 초기화 실패 status → Win32 오류 대응 줄.
4. **통과하면 바로 다음 syscall**: 위 검사를 통과한 뒤 RPC 런타임 초기화가 부르는 다음 syscall (`\RPC Control\DSEC` 쪽인가, ALPC 포트 만들기인가 — 이름과 오프셋만, 본문 안 따라감).
5. **최소 조건 표**: 에뮬레이터가 이 길을 통과시키려면 줘야 하는 것 (syscall 별 status · 출력 칸 · SD 안에서 꼭 맞아야 하는 값).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| rpcrt4 | `\RPC Control\` 문자열을 쓰는 함수 (xref) | 1 | 질문 1·2 |
| rpcrt4 | 그 함수의 caller 사슬 → `UuidToStringA` 까지 | 0 | 질문 1 — 이름·오프셋만 (지도로) |
| rpcrt4 | `^UuidToStringA$` | 1 | 질문 3 |
| rpcrt4 | `NtQuerySecurityObject` · `NtQueryInformationToken` caller (위 사슬 위에 있는 것만) | 1 | 질문 1·2 |
| rpcrt4 | `\RPC Control\DSEC` 문자열을 쓰는 함수 | 0 | 질문 4 — 이름·갈림만 |

- 범위: `\RPC Control` 보안 검사와 그 결과 갈림까지. RPC 전송·엔드포인트·ALPC 본문, NDR, 다른 export 는 이름만.
- `WinHttpOpen`·`LookupPrivilegeValueW`·`SHGetKnownFolderPath`·`CoInitializeSecurity` 쪽 모듈 (winhttp·advapi32·shell32·combase) 은 **안 본다** — 같은 rpcrt4 초기화로 들어온다고 보이는지만 한 줄 (export caller 로 보이는 만큼).
- 공용 부품 (힙, 락, 문자열, WPP/ETW) 은 이름만.

## 지도에서 본 것

- rpcrt4 지도 없음 (analyze 전). analyze 뒤 `run_ghidra.sh map` (진입 `UuidToStringA`) 으로 깊이·caller·직접 호출을 결과서에 옮긴다.

## 이미 아는 것

- `[정적]` sast/93: 커널 대답 규칙 (위 배경).
- `[관찰 — 에뮬레이터]` 버퍼를 준 둘째 부름은 0 줄 — 첫 부름 뒤 바로 `NtClose`.

## 받고 싶은 것

- 결과서 `_workspace/sast/95_rpc-caller_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: 사슬 표 (함수+오프셋 · 하는 일 · syscall).
- 질문 2: SD 검사 표 (읽는 칸 · 비교 값 · 실패하면 가는 곳).
- 질문 5: **최소 조건 표**.
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄. 특히 "status 0 + `len` 안 씀" 이 왜 실패로 가는지 (예: `len` 이 0 이라 할당 0 → 실패) 를 코드로.
- 끝에 **"동적으로 확인할 것"** (Q181 에 넘길 멈춤점) 과 **"다음 요청 후보"**.

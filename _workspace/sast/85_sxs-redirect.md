# 85 sxs-redirect — 정적 분석 요청

- 날짜: 2026-11-13
- 스프린트: 03_sprint
- 대기열: **Q151b** (PE_emulator 요청 29 의 2·3·4 — 로더가 활성화 문맥으로 DLL 이름을 리다이렉트하는 길). 사용자 결정 2026-11-12: 요청 26 ~ 30 맨 앞, 29 는 4순위, **첫 답은 지도 수준**. 정적만 — VM 몫은 Q104 출처로.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request29_sxs_activation_context.md` (읽어도 됨).
- 짝 바퀴: sast/84 sxs-create (Q151a — 만드는 쪽). **결과서 `_workspace/sast/84_sxs-create_result.md` 가 있으면 먼저 읽고** "Q151b 로 넘길 것" 을 따른다.
- 겹침: 대기열 Q90 (ntdll 이 PEB `pShimData`·`SystemDefaultActivationContextData` 를 **읽는** 곳). 이번에 닫히면 결과서에 "Q90 닫힘 (문맥 쪽)" 을 적는다.
- 빌드: 26100.9457 (ntdll — analyze 됨).
- 관련: run23 (`LdrpLoadDependentModuleInternal +0x1426` — 의존 DLL 이름 리다이렉트 자리 `[정적]`), run65·run67 (KnownDlls·`LdrSetDllManifestProber`·`BasepProbeForDllManifest` — 탐침이 자원 없는 모듈에 주는 상태값은 Q130 (a)), PE_emulator 흉내: 문맥 없음 → KnownDll `COMCTL32` = System32 v5.82.

## 알고 싶은 것

1. **리다이렉트 길**: `LdrpLoadDependentModuleInternal +0x1426` 아래 → `RtlDosApplyFileIsolationRedirection_Ustr` (rva `0x3eb80`) → `RtlFindActivationContextSectionString` (rva `0x41680`) → `RtlpFindNextActivationContextSection` (`0x41f60`) — 부르는 순서 (오프셋 + ret). **KnownDlls 검사와 리다이렉트 중 무엇이 먼저인가** (comctl32 는 KnownDll — 리다이렉트가 이기려면?).
2. **어느 문맥을 보나**: 스레드 활성화 문맥 스택 (TEB `ActivationContextStackPointer`) → 프로세스 기본 (`PEB.ActivationContextData`) → 시스템 기본 (`PEB.SystemDefaultActivationContextData`) 순서인가 — 줄. 셋 다 없으면 (PE_emulator 지금) 결과는 "리다이렉트 없음" (`STATUS_SXS_KEY_NOT_FOUND` 류) 인가.
3. **`ACTIVATION_CONTEXT_DATA` 최소 칸 표**: 로더가 DLL 이름을 찾을 때 읽는 칸만 — 머리 (Magic `'xtcA'`?·HeaderSize·FormatVersion·TotalSize·DefaultTocOffset·…), 목차 (TOC) 항목 (섹션 id `ACTIVATION_CONTEXT_SECTION_DLL_REDIRECTION` = 2?), 문자열 섹션 머리 (`ACTIVATION_CONTEXT_STRING_SECTION_HEADER` — Magic·해시 알고리즘·항목 수·항목 배열 오프셋), 항목 (키 문자열·해시·값 오프셋), DLL 리다이렉트 값 (경로 조각). **(오프셋 · 크기 · 뜻 · 읽는 줄)**. PDB 에 타입이 있으면 이름으로, 없으면 `[추측: 이름]`.
4. **comctl32 v6 로 가는 최소 조건**: 블롭에 무엇이 있으면 `comctl32.dll` 이 `C:\Windows\WinSxS\<어셈블리 폴더>\comctl32.dll` 로 바뀌나 — 경로를 만드는 줄 (어셈블리 루트 + 조각), 그 파일을 여는 syscall.
5. **시스템 기본 문맥만 있을 때** (매니페스트 없는 exe) 리다이렉트 결과 — test.exe 처럼 기본 문맥만 있으면 comctl32 는 v5.82 로 가나 v6 로 가나 (`[추측]` 이면 그렇다고).
6. **nt 엔진이 흉내 낼 최소 블롭** — 위 표에서 "comctl32 → v6 한 줄만 리다이렉트" 하는 가장 작은 블롭의 모양 (칸과 값). 해시 함수 (`RtlHashUnicodeString` 알고리즘 번호) 도.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpLoadDependentModuleInternal$` | 0 | 질문 1 — 이미 있음, `+0x1426` 앞뒤와 KnownDlls 갈림만 |
| ntdll | `^(RtlDosApplyFileIsolationRedirection_Ustr|RtlFindActivationContextSectionString|RtlpFindNextActivationContextSection)$` | 1 | 질문 1·2·3 |
| ntdll | 문자열 섹션을 찾는 안쪽 (`RtlpFindUnicodeStringInSection` 류, 이름은 `_index.tsv` 로) | 0 | 질문 3·6 |
| ntdll | `^RtlGetActiveActivationContext$` | 0 | 질문 2 |

- 지도: `run_ghidra.sh map ntdll.dll RtlDosApplyFileIsolationRedirection_Ustr`.
- 활성화 문맥 스택 API (`ActivateActCtx`) 전부·COM·창 클래스 섹션은 이름만.

## 이미 아는 것

- `[정적]` run23: `LdrpLoadDependentModuleInternal +0x1426` 이 의존 DLL 이름 리다이렉트 자리.
- `[관찰]` run45: test.exe PEB `SystemDefaultActivationContextData` = `25996d00000` (매핑, K3 ~ P0).
- `[관찰 — PE_emulator 흉내]` 문맥 없음 → KnownDll `COMCTL32` = System32 v5.82.
- sast/84 결과 (있으면): 블롭을 만드는 쪽·넣는 법.

## 받고 싶은 것

- 결과서 `_workspace/sast/85_sxs-redirect_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- **질문 3 의 데이터 모양 표** (오프셋 · 크기 · 뜻 · 읽는 줄) 와 **질문 6 의 최소 블롭**.
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.
- 끝에 "다음 요청 후보".
- 실행 때만 정해지는 것은 "동적으로 확인할 것" — Q104 에 붙인다 (예: test.exe 의 기본 문맥 블롭을 덤프에서 떠 와 표와 대조 — run44 사진 덤프 오프라인으로 되면 그렇다고).

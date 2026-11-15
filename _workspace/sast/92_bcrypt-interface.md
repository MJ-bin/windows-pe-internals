# 92 bcrypt-interface — 정적 분석 요청

- 날짜: 2026-11-15
- 스프린트: 03_sprint
- 대기열: **Q178a** (= Q178 ①, run94 남은 정적). 사용자 결정 2026-11-15: **Q176 ② · Q178 ① 먼저** (Q104 VM 단계 앞 멈춤 자리에서 끼움). Q178 ② ③ 은 Q178b 로 남김. **정적만** — VM 몫은 결과서 "동적으로 확인할 것" 으로만 적는다.
- 빌드: 26100.9457. bcrypt.dll 은 sast/88 에서 analyze·지도 됨 (PDB 붙음). bcryptprimitives.dll 은 sast/87 에서 analyze 됨. `_index.tsv` (bcrypt): `_LoadImage` rva `0x2c28` (421 B) · `ValidateFunctionTable` `0xff60` (1244 B) · `_LoadInterface` `0x10810` (2222 B) · `VsmLoadInterface` `0x199b0` (957 B).
- 관련: **sast/90 결과서** `_workspace/sast/90_bcrypt-resolve_result.md` 4-a 최소 조건 표 5·6 행 (222~223줄), 254~255줄 (`dwInterface` 번호), 271줄 (`Get…Interface` 이름 후보), "다음 요청 후보" 1 (308줄) · run 문서 `runs/03_sprint/run94-bcrypt-resolve.md` · sast/87 결과서 (bcryptprimitives `DllMain`).

## 배경 (PE_emulator 쪽)

- run94 로 `BCryptResolveProviders` 를 통과하는 최소 KsecDD 답이 나왔다 (공급자 1개 · `dwInterface 1..8`). 그다음 bcrypt 는 공급자 DLL (`bcryptprimitives.dll` `[추측]`) 을 올리고, 그 DLL 의 `Get…Interface` 를 `GetProcAddress` 로 찾아 함수 표를 받고, 표를 검사한 뒤 간접 호출로 연다. 에뮬레이터가 다음에 막힐 벽 후보가 여기다.

## 알고 싶은 것

1. **`dwInterface` 번호 ↔ 내보내기 이름**: `_LoadInterface` (`+0x130 GetProcAddress`) 가 번호마다 어떤 이름 문자열을 고르나. 1 ~ 8 전부 (예: `GetCipherInterface`·`GetHashInterface`·`GetAsymmetricEncryptionInterface`·`GetSecretAgreementInterface`·`GetSignatureInterface`·`GetRngInterface`·`GetKeyDerivationInterface` …). 표 (번호 · 이름 · 문자열 rva · 고르는 줄).
2. **`Get…Interface` 를 어떻게 부르나**: 인자 (공급자 이름 · 알고리즘 이름 · 받을 표 포인터 · 플래그 — 칸 순서), 반환 status 를 어떻게 보나 (음수면 어디로), 받은 표 포인터를 어디에 두나 (핸들 칸 오프셋).
3. **`ValidateFunctionTable` 이 무엇을 검사하나**: 번호마다 표의 머리 (버전 · 크기 칸) 와 함수 포인터 칸 중 **NULL 이면 안 되는 칸**. 실패 status 표 (status · 줄 · 조건). 검사 순서 (`_LoadInterface` 안 몇 번째 호출 — sast/90 의 `+0x70b` 가 어느 함수 안인지 확인).
4. **`BCryptOpenAlgorithmProvider +0x851 / +0x883` 간접 호출**: 함수 표의 몇 번째 칸 (오프셋) 을 어떤 인자로 부르나 (열기 함수 = `OpenAlgorithmProvider` 류로 보임 `[추측]`), 반환을 어떻게 보나. CFG 검사 (`__guard_dispatch_icall`) 를 지나는지.
5. **`_LoadImage` 와 이미 올라간 DLL**: 공급자 이미지 이름 → `LoadLibraryExW` 류 (플래그) 인가, 이미 올라간 DLL 이면 다시 안 올리나 (캐시 · 전역 목록 이름만). `VsmLoadInterface` 갈래는 언제 타나 (조건 줄만).
6. **최소 조건 표 (가장 중요)**: run94 4-a 표의 5·6 행을 채운다 — `BCryptOpenAlgorithmProvider` 가 성공 핸들을 돌려주려면 공급자 DLL 쪽이 갖춰야 하는 것 (내보내기 이름 · 표 머리 · NULL 이면 안 되는 칸 · 열기 함수가 돌려줘야 할 status). 짝으로 **bcryptprimitives 가 실제로 그 이름들을 내보내나** (PE export 표로 — 이름 목록만, 본문 아님).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| bcrypt | `^_LoadInterface$` | 1 | 질문 1·2·3 |
| bcrypt | `^ValidateFunctionTable$` | 1 | 질문 3 |
| bcrypt | `^_LoadImage$` | 1 | 질문 5 |
| bcrypt | `^BCryptOpenAlgorithmProvider$` | 0 | `+0x518 LoadProviderEx` 뒤 ~ `+0x851`·`+0x883` 앞뒤만 (질문 4·6). sast/90 에서 앞쪽은 읽음 |
| bcrypt | `^LoadProviderEx$` | 1 | `_LoadImage`·`_LoadInterface` 로 가는 사슬 (지도로 먼저 확인) |
| bcrypt | `^VsmLoadInterface$` | 0 | 질문 5 — 갈림 조건만 |
| bcryptprimitives | PE export 표 | — | 질문 6 — 내보내기 이름 목록 (Ghidra 또는 `objdump`/`pefile`), 본문 아님 |

- 범위: bcrypt 가 공급자 DLL 에서 **무엇을 찾고 무엇을 검사하나** 만.
- 공급자 DLL 안 (`bcryptprimitives` 의 `Get…Interface` 본문, 표 안 함수들) 은 **이번에 안 따라간다** — 이름·rva 만.
- Q178 ② (`BCryptEnumProviders`·`BCryptEnumAlgorithms`), ③ (알고리즘별 interface 선택) 은 **이번에 안 한다** (Q178b). 단 질문 1 표가 ③ 의 일부를 덮으면 그대로 적는다.
- 커널 `ksecdd.sys`, ETW/WPP (`WPP_SF_*`, `McGenEventWrite_*`, `_tlg*`), `_LogBCryptOpenProviderFailure` 는 이름만.
- 공용 부품 (`memcpy`, `BCryptAlloc`/`BCryptFree`, 문자열 함수, 락) 은 이름만.

## 지도에서 본 것 (`ms/disassemble/bcrypt/_map/entry_*`)

- `BCryptOpenAlgorithmProvider` rva `0x9410`, 깊이 7, caller 3, expanded. `+0x259 BCryptResolveProvidersForBcoapCached`, `+0x518 LoadProviderEx`, `+0x3cf _LogBCryptOpenProviderFailure`. 간접 호출 `+0x851`·`+0x883` (sast/90 4-a 6행).
- `_LoadInterface`·`ValidateFunctionTable`·`_LoadImage` 의 깊이·caller 는 아직 안 봄 → `entry_nodes.tsv`·`entry_edges.tsv` 에서 확인하고 결과서에 옮긴다.

## 이미 아는 것

- `[정적]` sast/90: `dwInterface` 는 `1..8` 이어야 함 (`BCryptOpenAlgorithmProvider +0x3ac ~ +0x3b7`, 밖이면 `0xc0000008`). 값은 핸들 `+0x24` 에 들어감 (`+0x498`). **6 = 난수** (`OpenSystemPreferredAlgorithmProvider +0xeb`), **2 = hash** (`+0xaa1`).
- `[정적]` sast/90: `_LoadInterface +0x130 GetProcAddress` 한 번. 이름 후보 문자열은 bcrypt 안에 있음 (271줄), 번호 ↔ 이름 짝은 `[공백]`.
- `[추측]` 공급자 DLL = `bcryptprimitives.dll` (sast/90 4-b 의 `pszImage`).

## 받고 싶은 것

- 결과서 `_workspace/sast/92_bcrypt-interface_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄), `[정적]`.
- 질문 1: 번호 ↔ 이름 표.
- 질문 3: 번호별 표 검사 표 (머리 칸 · 꼭 있어야 할 칸 · 실패 status).
- 질문 6: **최소 조건 표** — sast/90 4-a 표와 같은 양식으로 5·6 행 (그리고 그 뒤 행이 있으면 더). 짝으로 bcryptprimitives 내보내기 이름 목록 (질문 1 이름이 다 있나 O/X).
- 질문마다 **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄 (지금 우리 = run94 최소 답까지 통과, 그 뒤 진짜 DLL 을 올려 `GetProcAddress` 하는지는 에뮬레이터 로더에 달림 `[추측]`).
- 끝에 **"동적으로 확인할 것"** (추천 멈춤점, C++ 꾸민 이름 주의 — `x bcrypt!*이름*`) 과 **"다음 요청 후보"**.

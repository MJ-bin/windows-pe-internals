# 2026-11-15 run96 — bcrypt 가 공급자 DLL 을 올린 뒤 (`LoadProviderEx` → `_LoadImage` · `_LoadInterface` → `GetProcAddress(Get…Interface)` → 표 복사 → `ValidateFunctionTable` → 표 `+8` 열기) (Q178a = Q178 ①, run94 남은 정적 — 한 바퀴로 끝): **`dwInterface` `1..8` 은 내보내기 이름 8개와 짝이 정해져 있다 (`_LoadInterface +0x4e` 점프 표 rva `0x110cc`: 1 `GetCipherInterface` · 2 `GetHashInterface` · 3 `GetAsymmetricEncryptionInterface` · 4 `GetSecretAgreementInterface` · 5 `GetSignatureInterface` · 6 `GetRngInterface` · 7 `GetKeyDerivationInterface` · 8 `GetKeyEncapsulationInterface`), 진짜 `bcryptprimitives.dll` 은 8개를 다 내보낸다 (PE 내보내기 표 11개). bcrypt 는 그 함수를 CFG 간접 호출로 `(pszProvider, pszFunction, &표, 0)` (번호 6 만 `(pszProvider, &표, 0)`) 로 부르고, 음수면 그대로 위로, 아니면 표 첫 2바이트 ≥ 1 만 보고 (0 이면 `0xc00000bb`) 성공 0 을 돌려준다. 받은 표는 핸들 `+0x30` 으로 번호별 크기 (`0x30 ~ 0x70`) 만큼 복사하고 (`LoadProviderEx +0x247 memcpy`), `BCryptOpenAlgorithmProvider +0x70b ValidateFunctionTable` 이 번호별로 함수 칸 (`+8` 부터) 이 NULL 이 아닌지만 본다 (하나라도 NULL 이면 `0xc0000008`). 열기는 표 `+8` 을 `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)` 로 부른다 (`+0x851`, status ≥ 0 이면 성공). 표 머리 · NULL 칸 · 열기 status 는 진짜 DLL 코드가 정한다 — 에뮬레이터가 값을 만들 칸은 없고, 막힐 곳 후보는 `GetProcAddress` · CFG 간접 호출 · DLL 안 코드다. run94 정정 하나: KsecDD 답의 `pszProvider` · `pszFunction` 은 `Get…Interface` 의 첫째 · 둘째 인자로 간다 (로그용 아님), 핸들에 복사되는 이름은 호출자 `pszAlgId` 다.** `BCryptOpenAlgorithmProvider +0x518 LoadProviderEx` (`+0x9e ~ +0xd1` 목록 `g_pLoadedProviderList` 찾기 → 없으면 `+0x121 _LoadImage` (`+0x9d LoadLibraryExW(경로, 0, 0)`) → `+0xee _LoadInterface` (`+0x130 GetProcAddress` → `+0x1dc … +0x3dc` CFG 간접 `Get…Interface` → `+0x5d9` 머리 검사) → `+0x247 memcpy` (핸들 `+0x30`)) → `+0x70b ValidateFunctionTable` → `+0x851` 표 `+8` 열기 (→ `0xc000a001` + hash + 플래그 `0x8` 이면 `+0x883` 다시) → 번호 2 · 3 `+0xb19` / `+0xbcd` 표 `+0x10` (`GetProperty`) → `+0xcc7` 표 `+0x18` → `+0xce7` 핸들 돌려줌

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## PE_emulator 쪽에서 쓸 것

### Q178a 결론 (run96)

- **이름 문제는 닫혔다** `[정적]` — 번호 ↔ 이름은 bcrypt 안 문자열 (rva `0x20010 ~ 0x200d0`) 이 정하고, 진짜 `bcryptprimitives.dll` 이 8개를 다 내보낸다 (PE 내보내기 표, flow-writer 가 `objdump -p` 로 다시 셈). 에뮬레이터가 이름을 만들 일은 없다.
- **표 · 검사 · 열기는 진짜 DLL 몫** `[정적]` — `Get…Interface` 가 주는 표의 머리 (첫 2바이트 ≥ 1), NULL 이면 안 되는 칸, 열기 함수의 status 는 모두 `bcryptprimitives` 안 코드가 정한다. KsecDD 답 (run94) 이 정하는 건 "번호 · 공급자 이름 · 알고리즘 이름 · 이미지 이름 · 속성" 뿐.
- **그래서 `0xc0000008` · `0xc00000bb` 가 에뮬레이터에서 보이면** 원인은 KsecDD 답이 아니라 "DLL 이 진짜처럼 안 돌았다" (재배치 · IAT · CFG · 메모리) 쪽일 가능성이 높다 `[추측: 결과서]`.
  - `0xc0000008` = 복사된 표에 NULL 칸 (`ValidateFunctionTable`), 또는 번호가 `1..8` 밖 (run94 `+0x3d4`).
  - `0xc00000bb` = 표 첫 2바이트 0 (`_LoadInterface +0x5de` 등).
  - `0xc0000139` = `GetProcAddress` 가 NULL (`_LoadInterface +0x144`).
- **같은 DLL 은 한 번만 올린다** `[정적]` — `LoadProviderEx` 의 목록 `g_pLoadedProviderList` (rva `0x25b80`) 에 이름이 있으면 `_LoadImage` (= `LoadLibraryExW`) 를 건너뛴다. 예: AES 한 번 · 난수 한 번이면 `LoadLibraryExW` 는 한 번.
- **CFG 간접 호출을 지난다** `[정적]` — `Get…Interface` · 열기 · `GetProperty` · `SetProperty` · `Close` · `BCryptGenRandom +0x75` 모두 `CALL 0x18001d010` → `jmp 0x18001c060` → `jmp [0x18001ed98]` (`__guard_dispatch_icall_fptr`). 파일 속 값은 `0x18001c090` (`jmp rax`, 검사 없음). 실행 때 ntdll 이 이 칸을 바꿔 쓰는지는 `[공백]` (동적).
- **`pszProvider` 를 NULL (`-1`) 로 주는 run94 최소 답** — bcrypt 쪽은 그대로 통과하지만, 그 NULL 이 진짜 `Get…Interface` 첫 인자로 간다. 진짜 DLL 이 NULL 을 받아 주는지는 `[공백]` (DLL 본문 안 읽음). 안전한 쪽은 `L"Microsoft Primitive Provider"` 를 넣는 것 `[추측: 결과서]`.

### 지금 PE_emulator 맞나 · 틀리나 (결과서 "지금 우리" 줄 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

| 갈래 | 진짜 (26100.9457) `[정적]` | 지금 우리 (run94 최소 답까지 통과 전제) |
|---|---|---|
| 번호 ↔ 이름 | 1..8 = 이름 8개 (bcrypt 안 문자열) | **맞음** — AES 1 · hash 2 · RNG 6 다 `1..8` 안, 진짜 DLL 에 이름 있음 |
| `GetProcAddress` | 진짜 DLL 내보내기 표에서 찾음 | 에뮬레이터 로더 · `GetProcAddress` 가 진짜처럼 돌면 O `[추측]` |
| `Get…Interface` 인자 | KsecDD 답의 `pszProvider` (첫째) · `pszFunction` (둘째) | `pszProvider = -1` (NULL) 이면 진짜 DLL 이 받는지 `[공백]` |
| 표 머리 · NULL 칸 | 진짜 DLL 이 채움 | 우리가 만들 칸 없음. 재배치 · IAT 를 안 하면 칸이 0 → `0xc0000008` |
| 열기 (표 `+8`) | `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)`, status ≥ 0 | 진짜 코드. 이름은 호출자 것 (`L"AES"`) |
| CFG | `__guard_dispatch_icall_fptr` 경유 | 칸 값이 파일 값 (`jmp rax`) 이면 그냥 점프. ntdll 이 바꿔 썼다면 그 검사가 대상을 받아 줘야 함 `[공백]` |
| `LoadLibraryExW` 횟수 | 이름당 한 번 (목록 hit) | 에뮬레이터 로그에 같은 DLL 이 두 번 올라가면 이상 |

### 원문 질문이 어디서 답해졌나

| 요청서 질문 | 답한 곳 | 한 줄 |
|---|---|---|
| 1 번호 ↔ 이름 | "번호 ↔ 이름 표" | 1..8 전부 + `0x10001` · `0x10002` · `0x10004` (이 길에선 안 닿음) |
| 2 `Get…Interface` 부르는 법 | "`Get…Interface` 부르는 모양" · "표를 어디에 두나" | 인자 4개 (번호 6 은 3개), 음수 그대로, 표는 핸들 `+0x30` 으로 복사 |
| 3 `ValidateFunctionTable` | "NULL 이면 안 되는 칸 표" · "표 머리 검사" | 머리는 `_LoadInterface` 가, NULL 칸은 이 함수가. 자리는 `BCryptOpenAlgorithmProvider +0x70b` (`_LoadInterface` 안 아님) |
| 4 `+0x851` / `+0x883` | "열기 · 재시도" · "CFG" | 표 `+8`, 재시도 조건 셋, CFG 경유 |
| 5 `_LoadImage` · 이미 올라간 DLL · Vsm | "`_LoadImage` 와 이미 올라간 DLL" | `LoadLibraryExW(경로, 0, 0)`, 목록 hit 면 안 부름, Vsm 은 `g_TrustedEnvironment & 1 == 0` 일 때만 |
| 6 최소 조건 표 | 아래 "최소 조건 표" | run94 4-a 5 · 6 행 채움 |

### 최소 조건 표 — run94 4-a 의 5 · 6 행을 채움 (결과서 질문 6)

전제: run94 4-a 1 ~ 4 행을 맞췄고 (공급자 한 칸 + 이미지 이름 `bcryptprimitives.dll`), `LoadLibraryExW` 가 진짜 DLL 을 올려 attach 를 통과시켰다 (run91).

| # | syscall (또는 읽는 칸) | 입력 | 기대 답 | 안 맞으면 |
|---|---|---|---|---|
| 4 | `LoadLibraryExW` (run94 와 같음) | `시스템 폴더\bcryptprimitives.dll`, 플래그 0 | 성공 + attach 통과 | `_LoadImage +0x18c` `0xc0000135` / `+0x176` `0xc000007b` |
| **5** | **`GetProcAddress`** (`_LoadInterface +0x130`) | 위 `HMODULE` + 번호별 이름 (AES 1 → `GetCipherInterface`, hash 2 → `GetHashInterface`, RNG 6 → `GetRngInterface`) | 0 아닌 주소 (진짜 DLL 에 8개 다 있음) | NULL → `+0x144` **`0xc0000139`** → `LoadProviderEx +0x10f` 로그 → `+0x514 _FreeImage` (`FreeLibrary`) → 다음 공급자 / 실패 |
| **5-b** | `Get…Interface` (CFG 간접, `+0x1dc` 등) | 1 ~ 5 · 7 · 8: `(pszProvider, pszFunction, &표, 0)` · 6: `(pszProvider, &표, 0)` | status ≥ 0 **그리고 표 포인터 ≠ NULL**, 표 첫 2바이트 ≥ 1 | 음수 → 그대로 위로. 표 포인터 NULL → 크래시 (`+0x5d1` 바로 읽음). 머리 0 → **`0xc00000bb`** |
| **5-c** | KsecDD 답의 `pszProvider` · `pszFunction` (run94 4-b `0x28` · `0x20`) | 진짜 `Get…Interface` 가 받음 | 진짜 DLL 이 받아 줄 값 | **`[공백]`** — DLL 본문 안 읽음 |
| **5-d** | 표 복사 (`LoadProviderEx +0x247 memcpy`) | 핸들 `+0x30` ← 표 (번호별 `0x30 ~ 0x70` 바이트) | 표가 그 크기만큼 읽힘 | 읽기 오류 |
| **6** | **`ValidateFunctionTable`** (`BCryptOpenAlgorithmProvider +0x70b`) | 복사된 표 | 번호별 NULL 금지 칸이 다 채워짐 | **`0xc0000008`** → 다음 공급자 / 실패 |
| **6-b** | 열기 (표 `+8`, CFG, `+0x851`) | `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)` | status ≥ 0, `핸들[+0x18]` 채움 | 음수 → 다음 공급자 / 실패. `0xc000a001` + 번호 2 + 플래그 `0x8` 만 → `0x8` 빼고 `+0x883` 다시 |
| **6-c** | `GetProperty` (표 `+0x10`, 번호 2 `L"HashBlockLength"` `+0xb19` · 번호 3 `L"PaddingSchemes"` `+0xbcd`) | 4바이트 출력 | status ≥ 0 | 음수 → `+0xc31` `Close` 부르고 실패 (다음 공급자 안 감) |
| 6-d | `SetProperty` (표 `+0x18`, `+0xcc7`) | KsecDD 답 속성 `cProperties` 개 | 반환 안 봄 (run94 최소 답은 0개) | — |
| **7** | 성공 | `*phAlgorithm = 핸들` (`+0xce7`), 반환 0 (`+0xd19`) | | |

- 번호 6 (난수) 이 가장 단순: 표 `0x30`, 검사 5칸, `GetProperty` 없음 (`+0xaa1 CMP EAX,2` · `+0xb57 CMP EAX,3` 둘 다 아님). 그다음 `BCryptGenRandom +0x75` 가 **핸들 `+0x58` (= 표 복사 `+0x28`)** 을 `(핸들[+0x18], 버퍼, 크기, 플래그)` 로 부른다 (`+0x6b MOV RAX,[RAX+0x58]`) — 이게 다음 벽 후보.

## 환경

- 26100.9457, **정적만** (VM 실행 없음). test.exe 는 bcrypt 를 안 올린다 (run92 · run94 줄 그대로 — 초기화 끝 모듈 6개 · `\KnownDlls` 목록에 없음 `[관찰]` run44). 사진 대조 없음, 사진도 다시 안 읽었다.
- 정적: **bcrypt pdb `5F1077ED8D3ABC83AD7D378F94EA41901`** (run92 · run94 와 같음). **다시 analyze 안 함** (`.gpr` · `_index.tsv` · `SetPdb:` 줄 있음). `export` 2번, `SCRIPT ERROR` 0줄 (결과서).
- bcryptprimitives (pdb `E63663F36C3ABFBF74EACD102DC13DBB1`, run91) 는 **PE 내보내기 표만** (결과서 임시 파이썬 + `objdump -p`, 레포 밖). 본문 · `Get…Interface` `.asm` 없음. rva · 크기는 `ms/decomp/bcryptprimitives/_index.tsv` 와 같다 (flow-writer 대조).
- PDB 에 이 구조체들 (함수 표 · 핸들) 정의가 없다 — 칸 이름 (`OpenAlgorithmProvider` 등) 은 공개 헤더 기억 `[추측]`, 오프셋 · 크기 · 검사는 코드 `[정적]`.
- 버전 대조 (결과서): bcrypt 는 "알려진 사실" 표에 없어 요청서의 `_index.tsv` 값 (`_LoadImage 0x2c28`/421 · `ValidateFunctionTable 0xff60`/1244 · `_LoadInterface 0x10810`/2222 · `VsmLoadInterface 0x199b0`/957) 과 sast/90 오프셋 (`+0x518` · `+0x70b` · `+0x851` · `+0x883` · `_LoadInterface +0x130`) 이 그대로 나와서 맞춤.

## 목적

- run94 "아직 확인 필요" 정적 1 (대기열 Q178 을 나눈 **Q178a**, 사용자 결정 2026-11-15 "Q176 ② · Q178 ① 먼저", Q104 VM 단계 앞 멈춤 자리에서 정적 먼저). run94 로 `BCryptResolveProviders` 를 통과하는 최소 KsecDD 답이 나왔으니, 그 뒤 bcrypt 가 공급자 DLL 에서 **무엇을 찾고 무엇을 검사하나** — 에뮬레이터의 다음 벽 후보.
- 범위 밖: `bcryptprimitives` 의 `Get…Interface` 본문 · 표 안 함수들, Q178 ② ③ (= Q178b), 커널 `ksecdd.sys`, ETW/WPP, `_LogBCryptOpenProviderFailure`, 공용 부품 (`memcpy` · `BCryptAlloc` · 문자열 · 락).

## 방법

- 정적: `_workspace/sast/92_bcrypt-interface.md` — bcrypt `_LoadInterface` · `ValidateFunctionTable` · `_LoadImage` · `LoadProviderEx` · `VsmLoadInterface` · `BCryptOpenAlgorithmProvider` (모두 depth 0 으로 뽑음, 결과서). 덧붙임 `_FreeImage` · `UnloadProvider` (`.asm` 만) · `_GetImagePath` (다시 뽑음). bcryptprimitives PE 내보내기 표.
- 결과서: `_workspace/sast/92_bcrypt-interface_result.md` (**분석성공** — 못 닫은 것: 진짜 `Get…Interface` 가 `pszProvider` 를 어떻게 쓰나 (다음 요청 1) · 알고리즘 ↔ 번호 전체 표 (커널 몫) · `__guard_dispatch_icall_fptr` 의 실행 때 값 (동적)).
- flow-writer 가 다시 본 것 (`ms/disassemble/bcrypt/*.asm`, `_calls.tsv`, `_map/entry_*`):
  - `_LoadInterface +0x28` · `+0x38 ~ +0x4e` · `+0xc6` · `+0x12d ~ +0x144` · `+0x188 ~ +0x1e7` · `+0x338 ~ +0x345` · `+0x464` · `+0x573` · `+0x58b` · `+0x5a2` · `+0x5c0` · `+0x5d1 ~ +0x619` · CMP 줄 일곱 (`+0x64b` · `+0x671` · `+0x69a` · `+0x6cf` · `+0x704` · `+0x739` · `+0x76e`) · `+0x7e0`, `_calls.tsv` 의 이 함수 줄 전부 (간접 11 · `GetProcAddress` 1).
  - `LoadProviderEx +0x37` · `+0x9e ~ +0x12a` · `+0x16a` · `+0x230 ~ +0x24c` · `+0x3f0 ~ +0x403` · `+0x44e` · `+0x514`, `_calls.tsv` 이 함수 줄 전부.
  - `BCryptOpenAlgorithmProvider +0x56` · `+0x5f` · `+0x66` · `+0x1e2 ~ +0x1f4` · `+0x201 ~ +0x22c` · `+0x3b7` · `+0x495 ~ +0x4bb` (`+0x49c MOV R8,R12`) · `+0x4e9` · `+0x508` · `+0x518` · `+0x539 ~ +0x53c` · `+0x66e ~ +0x682` · `+0x6fe ~ +0x715` · `+0x83a ~ +0x892` · `+0x9e2 ~ +0x9e7` · `+0xaa1` · `+0xb57` · `+0xce7` · `+0xd19`, `_calls.tsv` 이 함수 줄 전부.
  - `ValidateFunctionTable +0xd` · `+0x1a` · `+0x2b` · `+0x82 ~ +0x88` · 실패 줄 일곱 (`+0x117` · `+0x1be` · `+0x254` · `+0x2e4` · `+0x350` · `+0x3da` · `+0x466`) · `+0x48e`.
  - `_LoadImage +0x2c` · `+0x7b` · `+0x9d` · `+0x140` · `+0x16a` · `+0x176` · `+0x18c`, `_FreeImage +0x12`, `VsmLoadInterface +0x45 ~ +0x5f`, `BCryptGenRandom +0x6b` · `+0x75`.
  - 지도 `entry_nodes.tsv` · `entry_edges.tsv` 의 다섯 함수 줄 (깊이 · caller · `VsmOpenAlgorithmProvider +0x16d → VsmLoadInterface`).
  - PE 바이트 (`objdump`): bcrypt `0x18001d010` (`jmp 0x18001c060`) · `0x18001c060` (`jmp [0x18001ed98]`) · `0x18001ed90 ~ 0x18001ed9f` (`0x18001bea0` · `0x18001c090`) · `DllCharacteristics 0x41e0` (`GUARD_CF`). bcryptprimitives 내보내기 표 11줄 (번호 1 ~ 11, rva).

## 결과

### 정적 — 호출 지점 (`ms/disassemble/bcrypt/_calls.tsv` · `.asm` · `_map/entry_edges.tsv`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `bcrypt!BCryptOpenAlgorithmProvider` | `+0x1f4` | `+0x1f9` | `VsmOpenAlgorithmProvider` | `+0x1e2 TEST AL,1` · `+0x1e4 JNZ +0x201` 안 탈 때 (`g_TrustedEnvironment & 1 == 0`) | [정적] |
| 〃 | `+0x4bb` | `+0x4c2` | `wcsncpy_s` | 핸들 알고리즘 이름 칸 ← **호출자 `pszAlgId`** (`+0x49c MOV R8,R12`, `R12` = `+0x5f MOV R12,RDX`), 최대 `0x104` 글자 (`+0x4a6`) | [정적] |
| 〃 | **`+0x518`** | **`+0x51d`** | **`LoadProviderEx`** | 5번째 인자 = 핸들 `+0x30` (`+0x4e9 LEA R9,[R15+0x30]` · `+0x508`) | [정적] |
| 〃 | **`+0x70b`** | **`+0x710`** | **`ValidateFunctionTable`** | `+0x539 TEST R15D` · `+0x53c JNS +0x682` 뒤, 핸들 `+0x30` 에서 `0x188` B 를 지역 `[RBP-0x10]` 로 복사 (`+0x682 ~ +0x6e3`) 한 것을 줌 (`+0x6fe`) | [정적] |
| 〃 | **`+0x851`** | **`+0x856`** | 간접 (CFG) — 표 `+8` (열기) | `+0x715 JNS +0x83a` 뒤. `RCX = &핸들[+0x18]` (`+0x84d`) · `RDX = R12` 호출자 `pszAlgId` (`+0x83e`) · `R8D = [RSP+0x44]` 호출자 플래그 (`+0x845`) · `RAX = [RBP-0x28]` (`+0x841` · `+0x84a`) | [정적] |
| 〃 | `+0x883` | `+0x888` | 간접 (CFG) — 표 `+8` 다시 | `+0x859` `0xc000a001` · `+0x860` 번호 2 · `+0x86c ~ +0x873` `(플래그 & 0x68) == 8` → `+0x875` `& ~8`. 뒤 `+0x888 OR [핸들+8],8` | [정적] |
| 〃 | `+0x9f4` | `+0x9f9` | `UnloadProvider` | 열기 음수 · `[RSP+0x40] == 1` (`+0x9e2` · `+0x9e7 JNZ +0xc36`) — 다음 공급자 앞 (본문 안 읽음) | [정적] |
| 〃 | `+0xb19` · `+0xbcd` | `+0xb1e` · `+0xbd2` | 간접 (CFG) — 표 `+0x10` | 번호 2 (`+0xaa1`) `L"HashBlockLength"` · 번호 3 (`+0xb57`) `L"PaddingSchemes"` | [정적] |
| 〃 | `+0xc31` | `+0xc36` | 간접 (CFG) — 표 `+0x20` | `GetProperty` 음수 뒤 뒤처리 | [정적] |
| 〃 | `+0xcc7` | `+0xccc` | 간접 (CFG) — 표 `+0x18` | 속성 수만큼, 반환 안 봄 | [정적] |
| `LoadProviderEx` | `+0xba` | `+0xc1` | `_wcsicmp` | 목록 `g_pLoadedProviderList` (`+0x9e`) 노드 이름 ↔ 이미지 이름 (`+0xb0` · `+0xb7`) | [정적] |
| 〃 | **`+0x121`** | **`+0x126`** | **`_LoadImage`** | 목록에 없을 때 (`+0xae` · `+0xd6 JZ +0x119`). 성공이면 `+0x12a JZ +0xe1` → `_LoadInterface` 로 | [정적] |
| 〃 | **`+0xee`** | **`+0xf3`** | **`_LoadInterface`** | `(HMODULE [RSP+0x40], 공급자 칸 R15, —, &표 [RSP+0x48])`. 목록 hit 면 `+0xd8` 노드 `+8` HMODULE 로 바로 | [정적] |
| 〃 | `+0x10f` | `+0x114` | `DebugTraceError` | `_LoadInterface` 0 아님 (`+0xf7 JZ` 안 탐) → 실패 길 | [정적] |
| 〃 | **`+0x247`** | **`+0x24c`** | `memcpy` | `(핸들+0x30 [RSP+0xa0], 표 [RSP+0x48], 번호별 크기)` (`+0x237 ~ +0x244`, 크기 `+0x16a ~ +0x230`) | [정적] |
| 〃 | `+0x366` · `+0x3e2` | `+0x36d` · `+0x3e7` | `RtlAllocateHeap` · `memcpy` | 새 노드 (`0x20` + 이름), `+0x403` 목록 머리에 | [정적] |
| 〃 | `+0x514` | `+0x519` | `_FreeImage` | 실패 길, 새로 올린 DLL 이면 (`_FreeImage +0x12 FreeLibrary`, `g_hInstance` · NULL 이면 안 함) | [정적] |
| `_LoadImage` | `+0x45` · `+0x59` | `+0x4c` · `+0x5e` | `RtlInitUnicodeString` · `_GetImagePath` | 이름 = 공급자 칸 `+0x28` → `+0` (`+0x2c`), 경로 = 시스템 폴더 + `\` + 이름 (run94) | [정적] |
| 〃 | `+0x7b` | `+0x82` | `RtlCompareUnicodeString` | 이름이 `bcrypt.dll` 이면 (대소문자 무시) `+0x140 g_hInstance` | [정적] |
| 〃 | **`+0x9d`** | **`+0xa4`** | **`LoadLibraryExW`** | `(경로, NULL, 0)` | [정적] |
| 〃 | `+0x16a` | `+0x171` | `GetLastError` | NULL 이면 — `0x7e` → `0xc0000135` (`+0x18c`), 그 밖 `0xc000007b` (`+0x176`) | [정적] |
| `_LoadInterface` | **`+0x130`** | **`+0x137`** | **`GetProcAddress`** | `RCX = R8` (`+0x12d`, = 들어온 `HMODULE` — `+0x28 MOV R8,RCX`) · `RDX` = 번호별 이름 (아래 표) | [정적] |
| 〃 | `+0x1dc` · `+0x225` · `+0x26e` · `+0x2b7` · `+0x300` · **`+0x345`** · `+0x38e` · `+0x3d7` | `+0x1e1` · `+0x22a` · `+0x273` · `+0x2bc` · `+0x305` · **`+0x34a`** · `+0x393` · `+0x3dc` | 간접 (CFG) — `Get…Interface` 1 ~ 8 | `+0x1ba` 점프 표 rva `0x110ec`. 번호 6 이 `+0x345` | [정적] |
| 〃 | `+0x41c` · `+0x4ac` · `+0x4ee` | `+0x421` · `+0x4b1` · `+0x4f3` | 간접 (CFG) — `0x10001` · `0x10004` · `0x10002` | 이 길엔 안 닿음 (`+0x3b7` 이 `1..8` 만) | [정적] |
| `VsmOpenAlgorithmProvider` | `+0x16d` | `+0x172` | `VsmLoadInterface` | 지도 `entry_edges.tsv` 깊이 9. 이 함수의 `.asm` 은 안 뽑음 | [정적] |
| 〃 | `+0x208` | `+0x20d` | `ValidateFunctionTable` | 지도 (caller 둘 중 하나) | [정적] |
| `VsmLoadInterface` | `+0x54` · `+0x5f` | `+0x59` · `+0x64` | `VsmGetLinkedInterfaceProvider` · `VsmGetInterfaceProviderFromLibrary` | `+0x50 TEST AL,0x18` (enclave `8` · `0x10` → 앞, 그 밖 → 뒤) — 이름만 | [정적] |
| `BCryptGenRandom` | `+0x75` | `+0x7a` | 간접 (CFG) — 핸들 `+0x58` (= 표 `+0x28`) | `+0x6b MOV RAX,[RAX+0x58]` | [정적] |

- 모든 "간접 (CFG)" 는 `CALL 0x18001d010` (5B 중개) 이다. `_calls.tsv` 에는 `indirect:cfg:?` 로 찍힌다.
- `ValidateFunctionTable` 의 callee 는 `WPP_SF_sDsd` (`+0x4d3`) 하나 — 잎에 가깝다 (`_calls.tsv`).

### 번호 ↔ 이름 표 (`_LoadInterface +0x19 ~ +0x12d`, 결과서 질문 1)

번호가 `1..8` 이면 `+0x38 DEC EAX` · `+0x3a CMP EAX,7` · `+0x43 MOV ECX,[R12+RAX*4+0x110cc]` · `+0x4e JMP RCX` → 번호별 `LEA RDX,[이름]` → 모두 `+0x12d MOV RCX,R8` → `+0x130 GetProcAddress`.

| `dwInterface` | 이름 (bcrypt `.rdata`, ANSI) | 문자열 rva | 고르는 줄 | bcryptprimitives 내보내기 (번호 · rva · 크기) |
|---|---|---|---|---|
| 1 | `GetCipherInterface` | `0x20010` | `+0x54` | **O** (2 · `0xad50` · 128) |
| 2 | `GetHashInterface` | `0x20028` | `+0x60` | **O** (3 · `0xacb0` · 140) |
| 3 | `GetAsymmetricEncryptionInterface` | `0x20040` | `+0x6c` | **O** (1 · `0x48800` · 133) |
| 4 | `GetSecretAgreementInterface` | `0x20068` | `+0x78` | **O** (7 · `0x46210` · 207) |
| 5 | `GetSignatureInterface` | `0x20088` | `+0x84` | **O** (8 · `0x4c6a0` · 283) |
| 6 | `GetRngInterface` | `0x200a0` | `+0x90` | **O** (6 · `0x48b90` · 13) |
| 7 | `GetKeyDerivationInterface` | `0x200b0` | `+0x9c` | **O** (4 · `0x48c00` · 228) |
| 8 | `GetKeyEncapsulationInterface` | `0x200d0` | `+0xa8` | **O** (5 · `0x5f290` · 126) |
| `0x10001` | `GetKeyStorageInterface` | `0x200f0` | `+0xb1` | X — 이 길엔 안 닿음 |
| `0x10002` | `GetSChannelInterface` | `0x20128` | `+0x126` | X — 〃 |
| `0x10004` | `GetKeyProtectionInterface` | `0x20108` | `+0x11d` | X — 〃 |
| 그 밖 | — | — | `+0xc6 MOV EBX,0xc00000bb` | — |

- 문자열 · 점프 표 8칸은 결과서가 PE 바이트로 읽음. 내보내기 표는 flow-writer 가 `objdump -p` 로 다시 셈 — 11개 (위 8 + `MSCryptConvertRsaPrivateBlobToFullRsaBlob` 9 · `ProcessPrng` 10 · `ProcessPrngGuid` 11), 이름 있음, rva 가 모두 `.text` 안 (포워더 아님).
- 이 표는 **"번호 → 이름"** 쪽만이다. **"알고리즘 이름 → 번호"** 는 커널이 KsecDD 답 `+0x18` 에 써 주는 값이라 이번엔 `[공백]` (Q178b ③ · Q173b ③).

### `Get…Interface` 부르는 모양 (`_LoadInterface`, 결과서 질문 2)

- 함수 모양: `_LoadInterface(HMODULE /*RCX*/, 공급자 칸* /*RDX → R15*/, (안 씀), 표포인터* /*R9 → R14*/)`. 3번째는 `+0x28 MOV R8,RCX` 로 덮여 안 쓴다.
- ① 이름 찾기 (위 표) → `+0x13c MOV R10,RAX` · `+0x13f TEST` · `+0x142 JNZ`. **NULL 이면 `+0x144` `0xc0000139`** 로 끝.
- ② 번호로 한 번 더 갈려 (`+0x188 MOV EAX,[R15]` … `+0x1ba` 점프 표 rva `0x110ec`) 부른다:

| 번호 | 인자 (칸 순서) | 근거 (번호 1 · 6) |
|---|---|---|
| 1 ~ 5 · 7 · 8 | `RCX = [R15+0x10]` (공급자 이름) · `RDX = [R15+8]` (알고리즘 이름 `pszFunction`) · `R8 = R14` (표 포인터를 받을 칸) · `R9 = 0` (플래그) | `+0x1cb` · `+0x1cf` · `+0x1d2` · `+0x1d6` · `+0x1d9 MOV RAX,R10` · `+0x1dc` |
| **6 Rng** | `RCX = [R15+0x10]` · `RDX = R14` · `R8 = 0` (인자 3개, 알고리즘 이름 없음) | `+0x338` · `+0x345` |

- `[R15+8]` · `[R15+0x10]` 은 run94 "출력 칸 표" 의 공급자 칸 `+0x08` (이름) · `+0x10` (공급자 이름) — 출력 시작 기준으로는 run94 4-b 의 `0x20` · `0x28`. 칸 이름 (`pszProviderName` · `pszAlgorithmName` · `ppFunctionTable` · `dwFlags`) 은 `[추측: 공개 헤더]`.
- **status 보기**: `+0x1e5 TEST EAX` · `+0x1e7 JNS +0x58b` (번호마다 같은 꼴). 음수면 WPP 로그 뒤 `+0x573 MOV EAX,EBX` 로 **그대로** 반환 → `LoadProviderEx +0xf7 JZ` 안 탐 → `+0x10f DebugTraceError` → 실패 길 (`+0x514 _FreeImage` · 출력 0).
- 0 이상이면 `+0x58b` → `+0x5c0` 점프 표 rva `0x1110c` → **머리 검사** (아래) → 통과하면 `+0x5a2 XOR EBP,EBP` — **성공 반환은 늘 0** (공급자가 양수를 돌려줘도).
- **표 포인터가 NULL 이면 크래시** `[정적: 읽기]` — `+0x5d1 MOV RAX,[R14]` 뒤 바로 `+0x5d9 CMP CX,[RAX]`. 부르기 전에 `[R14]` (= `LoadProviderEx` 지역 `[RSP+0x48]`) 를 0 으로 만들지 않는다 (결과서).

### 표 머리 검사 (`_LoadInterface`, 공급자가 준 원본 표)

- 번호마다 같은 꼴: `MOV RAX,[R14]` · `MOV ECX,1` · **`CMP CX,word ptr [RAX]`** · `JBE` (통과). 곧 **표 첫 2바이트 ≥ 1**. 0 이면 `MOV EBP,0xc00000bb`.
- CMP 줄: 번호 1 `+0x5d9` · 2 `+0x64b` · 3 `+0x671` · 4 `+0x69a` · 5 `+0x6cf` · 6 `+0x704` · 7 `+0x739` · 8 `+0x76e`. 실패 줄 (번호 1) `+0x5de`.
- 두 번째 `USHORT` · 크기 칸은 안 본다. 첫 2바이트 = 공개 헤더의 `Version.MajorVersion` 으로 보임 `[추측]`.

### 표를 어디에 두나 — 핸들 `+0x30` 으로 복사 (`LoadProviderEx`)

- `_LoadInterface` 는 표 포인터를 `LoadProviderEx` 지역 `[RSP+0x48]` 에만 돌려준다 (`+0xe6 LEA R9,[RSP+0x48]`).
- `+0x237 MOV RDX,[RSP+0x48]` · `+0x23c MOV RCX,[RSP+0xa0]` (= 5번째 인자 = 핸들 `+0x30`) · `+0x244 MOV R8D,EAX` · **`+0x247 memcpy`**.
- 크기 (`+0x16a ~ +0x230`, 점프 표 rva `0x8d9c`, 결과서): 1 → `0x60` · 2 → `0x60` · 3 → `0x70` · 4 → `0x68` · 5 → `0x60` · 6 → `0x30` · 7 → `0x58` · 8 → `0x60` (`0x10001` → `0x100` · `0x10004` → `0x28` · `0x10002` → `0x188`).
- **원본 표는 복사 뒤 안 쓴다.** 표는 DLL 정적 데이터여도 되고, 복사 크기만큼 읽히기만 하면 된다.
- 이후 쓰는 쪽은 `핸들 + 0x30 + 표 오프셋` — 예: `BCryptGenRandom +0x6b MOV RAX,[RAX+0x58]` = 표 `+0x28`.

### NULL 이면 안 되는 칸 (`ValidateFunctionTable`, 복사된 표 — 결과서 3-b)

모양: `ValidateFunctionTable(번호 /*ECX*/, 표 복사본* /*RDX*/, 출력1 /*R8*/, 출력2 /*R9*/, 출력3, 출력4)`. 번호 `1..8` 밖이면 `+0xd JA +0x48e` → `0xc0000008`. 안이면 `+0x1a` 점프 표 rva `0x10444`. **머리 (버전 · 크기) 는 안 본다.**

| 번호 | 이름 | 복사 크기 | NULL 이면 안 되는 칸 (`+8` 부터 8B 씩) | 실패 줄 (`MOV EBX,0xc0000008`) |
|---|---|---|---|---|
| 1 | Cipher | `0x60` | `+8 ~ +0x58` 11칸 전부 | `+0x88` |
| 2 | Hash | `0x60` | `+8 ~ +0x48` 9칸 (`+0x50` · `+0x58` 은 안 봄) | `+0x117` |
| 3 | AsymEnc | `0x70` | `+8 ~ +0x68` 13칸 전부 | `+0x1be` |
| 4 | SecretAgreement | `0x68` | `+8 ~ +0x60` 12칸 전부 | `+0x254` |
| 5 | Signature | `0x60` | `+8 ~ +0x58` 11칸 전부 | `+0x2e4` |
| 6 | Rng | `0x30` | `+8 ~ +0x28` 5칸 전부 | `+0x350` |
| 7 | KeyDerivation | `0x58` | `+8 ~ +0x50` 10칸 전부 | `+0x3da` |
| 8 | KeyEncapsulation | `0x60` | `+8 ~ +0x58` 11칸 전부 | `+0x466` |

- 복사 크기의 마지막 칸까지 검사한다 (번호 2 만 마지막 둘 뺌).
- 꺼내 주는 칸 (결과서): `[출력1] ← 표+8` · `[출력2] ← 표+0x20` · `[출력3] ← 표+0x18` · `[출력4] ← 표+0x10` (**번호 2 · 3 만**; 나머지는 `BCryptOpenAlgorithmProvider +0x66` 초기값 0 그대로).

### 표 칸 뜻 — 부르는 모양은 `[정적]`, 이름은 `[추측: 공개 헤더]`

| 표 칸 | 이름 (추측) | 부르는 자리 | 인자 (레지스터로 확인) |
|---|---|---|---|
| `+0` | 머리 (Version) | `_LoadInterface +0x5d9` 등 | 첫 2바이트 ≥ 1 |
| `+8` | `OpenAlgorithmProvider` | `BCryptOpenAlgorithmProvider +0x851` · `+0x883` | `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)` |
| `+0x10` | `GetProperty` | `+0xb19` (번호 2) · `+0xbcd` (번호 3) | `(핸들[+0x18], 속성 이름, 출력, 4, &길이, 0)` (결과서) |
| `+0x18` | `SetProperty` | `+0xcc7` | `(핸들[+0x18], 이름, 값, 크기, 0)` (결과서) |
| `+0x20` | `CloseAlgorithmProvider` | `+0xc31` | `(핸들[+0x18], 0)` (결과서) |
| `+0x28 ~` | 알고리즘별 (번호 6: 난수 만들기) | `BCryptGenRandom +0x75` 등 | 이번 범위 밖 |

### 열기 · 재시도 (`BCryptOpenAlgorithmProvider +0x83a ~ +0x892`)

- 열기 = **표 `+8`** 을 `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)` 로. KsecDD 답의 `pszFunction` 이 아니라 **호출자 이름**이 간다.
- 반환 `+0x856 MOV R15D,EAX` → `+0x88f TEST R15D` · `+0x892 JNS +0xa86` — **status ≥ 0 이면 성공**.
- 재시도 (`+0x883`) 조건 셋 다: ① `== 0xc000a001` (`+0x859`) ② 번호 2 (`+0x860 CMP [RSP+0x78],2` · `+0x86a JNZ`) ③ `(플래그 & 0x68) == 8` (`+0x86c ~ +0x873`). 그때 `+0x875 AND R8D,0xfffffff7` 로 `0x8` 을 빼고 다시, 뒤 `+0x888 OR [핸들+8],8`. `0x8` = `BCRYPT_ALG_HANDLE_HMAC_FLAG` `[추측: 이름]`.

### CFG — 간접 호출이 지나는 칸

- `CALL 0x18001d010` → `jmp 0x18001c060` → `jmp [0x18001ed98]` (flow-writer `objdump` 확인). `0x18001ed98` = `__guard_dispatch_icall_fptr` (로드 구성 `GuardCFDispatchFunctionPointer`, 결과서).
- **파일 속 값** `[정적: PE 바이트]`: `[0x18001ed98] = 0x18001c090` (`jmp rax`, 검사 없음) · `[0x18001ed90] = 0x18001bea0` (`ret`). bcrypt `DllCharacteristics 0x41e0` 에 `GUARD_CF` 있음.
- 실행 때 이 칸을 ntdll 이 바꿔 쓰는지 · 무엇으로 바꾸는지는 `[공백]` (동적 ①). 바꿔 쓰면 대상 (`bcryptprimitives` 안 함수) 이 CFG 비트맵에 "유효" 로 올라 있어야 한다 `[추측]`. 틀리면 `__fastfail` (`int 0x29`) `[추측]`.

### `_LoadImage` 와 이미 올라간 DLL (결과서 질문 5)

- `_LoadImage`: 이름 = 공급자 칸 `+0x28` → `+0` (`+0x2c`), 경로 = `GetSystemDirectoryW() + "\" + 이름` (`+0x59 _GetImagePath`, run94). **`+0x9d LoadLibraryExW(경로, NULL, 0)`** — 검색 경로 플래그 없음.
- 이름이 `bcrypt.dll` 이면 (`+0x7b RtlCompareUnicodeString`, 대소문자 무시, 비교 대상은 경로가 아닌 이름) `+0x140 g_hInstance` 를 그대로 쓰고 `LoadLibraryExW` 안 부름.
- **두 번 안 올리는 장치는 `LoadProviderEx` 안**: 목록 `g_pLoadedProviderList` (rva `0x25b80`, 락 `g_csLoaderLock` rva `0x25ba0` — 결과서) 를 `+0x9e ~ +0xd1` 에서 `_wcsicmp` 로 돈다. hit 면 `+0xd8 MOV RAX,[RBX+8]` (노드의 `HMODULE`) → `_LoadImage` 건너뛰고 `+0xee _LoadInterface`, 참조 수 `[노드+0x10]` +1 (`+0x255 ~`). miss 면 `+0x121 _LoadImage` → 성공이면 `+0x12a JZ +0xe1` → `+0xee`. 새 노드: `+0` 이름 · `+8 HMODULE` · `+0x10` 참조 수 · `+0x18` 다음 (`+0x403` 목록 머리에).
- 이 목록은 **bcrypt 가 올린 것만** 안다. 다른 길로 이미 올라간 `bcryptprimitives.dll` 이면 `LoadLibraryExW` 가 불리고, 같은 `HMODULE` 을 돌려주는 건 ntdll 로더 몫 `[추측]`.
- `_FreeImage` (실패 때만): `g_hInstance` · NULL 이면 안 함, 아니면 `+0x12 FreeLibrary`.
- **Vsm 갈래**: `BCryptOpenAlgorithmProvider +0x1e2 TEST AL,1` · `+0x1e4 JNZ +0x201` 을 **안 탈 때** (`g_TrustedEnvironment` 비트 0 이 0) `+0x1f4 VsmOpenAlgorithmProvider` → `+0x16d VsmLoadInterface` (지도). 일반 프로세스는 `1` 이라 안 탄다 — run92 의 NtQIP `0x56` 조건 (`1` · `0x10` · `0x11` 이 아닌 값) 을 지키면 그대로. 값 `4` (trustlet) · `8` / `0x10` (enclave) 와 `VsmLoadInterface +0x50 TEST AL,0x18` 갈림은 결과서 줄 — 본문 안 따라감.

### 핸들 칸 (이번에 보인 것, `0x3c8` 바이트 — 결과서 + flow-writer `.asm`)

| 칸 | 값 · 뜻 | 쓰는 줄 |
|---|---|---|
| `+0x00` | `0x3c8` | `+0x49f` |
| `+0x04` | `0x55555551` | `+0x4ab` |
| `+0x08` | 플래그 (HMAC 재시도 뒤 `\|= 8`) | `+0x888` |
| `+0x0c` | HashBlockLength (번호 2) | (결과서) |
| `+0x10` | `1` | `+0x4b3` |
| `+0x18` | 공급자 쪽 핸들 (열기가 써 줌) | `+0x84d` (주소 넘김) |
| `+0x24` | `dwInterface` | `+0x498` (run94) |
| `+0x28` | 올린 공급자 노드 (`LoadProviderEx` 4번째 인자) | `LoadProviderEx +0x44e` |
| `+0x30 ~` | 표 복사 (`0x30 ~ 0x70`) | `LoadProviderEx +0x247` |
| `+0x1b8` | PaddingSchemes (번호 3) | (결과서) |
| `+0x1bc` | 알고리즘 이름 = 호출자 `pszAlgId` (최대 `0x104` 글자) | `+0x4bb wcsncpy_s` |

### 실패 status 표 (이 사슬 — 결과서 3-c 줄임)

| status | 자리 | 조건 |
|---|---|---|
| `0xc0000008` | `ValidateFunctionTable +0x88` · `+0x117` · `+0x1be` · `+0x254` · `+0x2e4` · `+0x350` · `+0x3da` · `+0x466` | 번호별 NULL 금지 칸 |
| `0xc0000008` | `ValidateFunctionTable +0x48e` | 번호 `1..8` 밖 (이 길에선 run94 `+0x3b7` 이 먼저 막음) |
| `0xc00000bb` | `_LoadInterface +0x5de` 등 · `+0xc6` · `+0x464` · `+0x7e0` | 표 첫 2바이트 0 · 번호가 정해진 11개 밖 |
| `0xc0000139` | `_LoadInterface +0x144` | `GetProcAddress` NULL |
| `0xc0000135` / `0xc000007b` | `_LoadImage +0x18c` / `+0x176` | `LoadLibraryExW` 실패 (`GetLastError == 0x7e` 면 앞) |
| 공급자가 준 음수 | `_LoadInterface +0x1e7` 등 | `Get…Interface` 음수 |
| `0xc00000e5` | `LoadProviderEx +0x37` | `g_fLoaderInitialized == 0` (run94) |
| `0xc000000d` | `LoadProviderEx +0x1d3` · `+0x4d9` | 번호 갈림 밖 · 새 노드 이름 NULL / `0x800` 글자 안에 NUL 없음 (결과서) |
| `0xc0000017` / `0xc000009a` / `0xc0000095` | `LoadProviderEx +0x37a` / `+0x261` / `+0x457` | 노드 할당 실패 / 참조 수 넘침 / 길이 넘침 (결과서) |

### 검사 순서와 "다음 공급자로 가나" (`BCryptOpenAlgorithmProvider`)

1. `+0x518` `LoadProviderEx` → (목록 miss 면 `_LoadImage`) → `_LoadInterface` (이름 → `Get…Interface` → 머리 검사) → `memcpy` (핸들 `+0x30`).
2. `+0x539 TEST R15D` · `+0x53c JNS +0x682` → 핸들 `+0x30` 에서 `0x188` B 를 지역으로 복사.
3. **`+0x70b` `ValidateFunctionTable`**.
4. `+0x713 TEST` · `+0x715 JNS +0x83a` → **`+0x851` 열기**.
5. `+0x892 JNS +0xa86` → 번호 2 · 3 이면 `GetProperty` → 속성마다 `SetProperty` → `+0xce7 *phAlgorithm = 핸들` → `+0xd19 XOR R15D,R15D` (0).

- **다음 공급자로 넘어가는 건 "시스템 기본" 모드일 때만**: `+0x201 CMP [g_fLoadCNGDone]` 0 아님 + `pszImplementation` NULL (`+0x21f TEST RCX` · `+0x222 JNZ` 안 탐) → **`+0x224 MOV byte [RSP+0x40],1`**.
  - `LoadProviderEx` 음수 · `ValidateFunctionTable` 음수 → `+0x66e CMP byte [RSP+0x40],1` · `+0x673 JNZ +0xc36` (아니면 핸들 free · 실패), 맞으면 `+0x67d JMP +0xa12` → 다음 공급자.
  - 열기 음수 → `+0x9e2` · `+0x9e7 JNZ +0xc36`, 맞으면 `+0x9f4 UnloadProvider` 뒤 다음 공급자.
  - `GetProperty` 음수 → 다음 공급자 **없이** `+0xc31` Close → `+0xc36` 실패.
- run94 최소 답은 공급자 하나라 "다음" 이 없다 — 어느 단계든 실패하면 그 status 로 끝.

### 호출 관계

```
bcrypt!BCryptOpenAlgorithmProvider (0x9410)  (phAlgorithm, pszAlgId=R12, pszImplementation, dwFlags)         [정적]
 │  +0x1e4 JNZ (g_TrustedEnvironment & 1) — 아니면 +0x1f4 VsmOpenAlgorithmProvider → +0x16d VsmLoadInterface (이름만)
 │  +0x224 [RSP+0x40] = 1  (CNG 끝 + 공급자 NULL → 실패 때 다음 공급자)
 │  … run94: +0x259 BcoapCached → BCryptResolveProviders (KsecDD 답) · +0x3b7 dwInterface ∈ 1..8
 │  +0x498 핸들+0x24 ← 번호 · +0x4bb wcsncpy_s 핸들+0x1bc ← 호출자 pszAlgId
 ├─ (1) +0x518  LoadProviderEx (0x8840) (공급자 칸, …, 노드*, 핸들+0x30)
 │        │  +0x37 g_fLoaderInitialized 0 → 0xc00000e5
 │        │  +0x9e ~ +0xd1  g_pLoadedProviderList (0x25b80) 에서 _wcsicmp(이미지 이름)  → hit: +0xd8 HMODULE
 │        ├─ (1) +0x121  _LoadImage (0x2c28)                                        miss 일 때만
 │        │        ├─ +0x59  _GetImagePath → 시스템 폴더 + "\" + 이름
 │        │        ├─ +0x7b  RtlCompareUnicodeString — "bcrypt.dll" 이면 +0x140 g_hInstance
 │        │        └─ +0x9d  LoadLibraryExW(경로, NULL, 0)   실패 → 0xc0000135 / 0xc000007b
 │        │     +0x12a JZ +0xe1 ↓
 │        ├─ (2) +0xee   _LoadInterface (0x10810) (HMODULE, 공급자 칸, —, &표)
 │        │        │  +0x4e  점프 표 0x110cc — 번호 → 이름 (1 GetCipherInterface … 8 GetKeyEncapsulationInterface)
 │        │        ├─ (1) +0x130  GetProcAddress(HMODULE, 이름)      NULL → +0x144 0xc0000139
 │        │        ├─ (2) +0x1dc … +0x3d7  CFG 간접 Get…Interface(pszProvider, pszFunction, &표, 0)
 │        │        │        └─ 번호 6 만 +0x345  (pszProvider, &표, 0)
 │        │        │     +0x1e7 JNS — 음수면 그대로 반환
 │        │        └─ +0x5d9 …  표 첫 2바이트 ≥ 1 ?  아니면 0xc00000bb  → 통과면 0
 │        │     +0xf7 JZ — 아니면 +0x10f DebugTraceError → +0x514 _FreeImage (FreeLibrary)
 │        ├─ (3) +0x247  memcpy(핸들+0x30, 표, 번호별 0x30 ~ 0x70)
 │        └─ (4) +0x366 RtlAllocateHeap · +0x403 목록 머리에 새 노드        miss 일 때
 │  +0x53c JNS → +0x682 핸들+0x30 → 지역 0x188 B
 ├─ (2) +0x70b  ValidateFunctionTable (0xff60) (번호, 표 복사본, &표+8, &표+0x20, &표+0x18, &표+0x10)
 │        +0x1a 점프 표 0x10444 — 번호별 함수 칸 NULL? → 0xc0000008
 │  +0x715 JNS — 음수면 (+0x66e) 다음 공급자 / 실패
 ├─ (3) +0x851  CFG 간접 표+8 (&핸들[+0x18], pszAlgId, dwFlags)        status ≥ 0 이면 성공
 │        └─ +0x883  0xc000a001 · 번호 2 · 플래그 0x8 → 0x8 빼고 다시 · +0x888 핸들+8 |= 8
 │  +0x892 JNS — 음수면 (+0x9e2) +0x9f4 UnloadProvider 뒤 다음 공급자 / 실패
 ├─ (4) +0xb19 / +0xbcd  CFG 간접 표+0x10 GetProperty("HashBlockLength" / "PaddingSchemes")   번호 2 / 3
 │        음수 → +0xc31 CFG 간접 표+0x20 Close → 실패
 ├─ (5) +0xcc7  CFG 간접 표+0x18 SetProperty × 속성 수                 반환 안 봄
 └─ +0xce7  *phAlgorithm = 핸들 · +0xd19 0

모든 "CFG 간접" = CALL 0x18001d010 → jmp 0x18001c060 → jmp [0x18001ed98] (__guard_dispatch_icall_fptr, 파일 값 = jmp rax)

bcrypt!BCryptGenRandom (번호 6 핸들)                                                                      [정적]
 └─ +0x75  CFG 간접 [핸들+0x58] = 표+0x28 (핸들[+0x18], 버퍼, 크기, 플래그)          (이번 범위 밖 — 다음 벽 후보)
```

## 확인된 것

- `[정적]` `dwInterface` `1..8` ↔ 내보내기 이름 8개 (`_LoadInterface +0x4e` 점프 표 rva `0x110cc`, 고르는 줄 `+0x54 ~ +0xa8`). 그 밖 `0x10001` · `0x10002` · `0x10004` 는 이 길에 안 닿는다.
- `[정적: PE 표]` bcryptprimitives 는 8개를 다 내보낸다 (11개 중, 포워더 없음). run94 의 `[공백]` "이름 ↔ 번호" · "번호 1 과 이름 짝" 이 닫힘.
- `[정적]` `Get…Interface` 인자 = `(pszProvider, pszFunction, &표, 0)` (번호 6 은 `(pszProvider, &표, 0)`), CFG 간접. 음수 그대로, 0 이상이면 표 첫 2바이트 ≥ 1 만 보고 0 반환.
- `[정적]` 표는 핸들 `+0x30` 으로 번호별 크기만큼 복사 (`LoadProviderEx +0x247`). 원본은 그 뒤 안 씀.
- `[정적]` `ValidateFunctionTable` 은 `BCryptOpenAlgorithmProvider +0x70b` 에서 불리고 (`_LoadInterface` 안 아님), 머리는 안 보고 번호별 함수 칸 NULL 만 본다 → `0xc0000008`.
- `[정적]` 열기 = 표 `+8` `(&핸들[+0x18], 호출자 pszAlgId, 호출자 플래그)` (`+0x851`), status ≥ 0 이면 성공. HMAC 플래그 재시도 `+0x883` 조건 셋.
- `[정적]` `LoadLibraryExW(경로, NULL, 0)` 는 목록 `g_pLoadedProviderList` 에 이름이 없을 때만 — 이름당 한 번.
- `[정적: PE 바이트]` 간접 호출은 모두 `__guard_dispatch_icall_fptr` 경유, 파일 값 `jmp rax`.

## 결과서와 다르게 적은 곳 · 보탠 것

- ① (자리 바로 적음) 결과서 "실패했을 때 다음 공급자로 가나" 의 "`+0x216 ~ +0x229 MOV byte [RSP+0x40],1`" — 그 줄은 **`+0x224`** 하나다 (`+0x216` 은 `MOV RCX,[RSP+0x70]`, `+0x229` 는 `LEA EDI,[RCX+2]`). 조건은 `+0x21f TEST RCX` · `+0x222 JNZ` (공급자 인자 NULL). 결론 같음.
- ② (보탬) `LoadProviderEx` 의 miss 길이 `_LoadInterface` 로 가는 줄 — `+0x126 MOV EDI,EAX` · `+0x128 TEST` · **`+0x12a JZ +0xe1`** (= `0x180008921`) → `+0xee`. 결과서는 "(없으면 `_LoadImage` →) `_LoadInterface`" 로만 적음.
- ③ (보탬) 결과서 "근거 파일" 의 "`_LoadInterface.asm` (전체: `+0x0 ~ +0x589` …)" — 함수는 `+0x8b6` 까지다 (2222 B, `.asm` 492줄). 머리 검사 `+0x5d1 ~ +0x7cb` 와 WPP 줄 `+0x81a` · `+0x8aa` 는 그 뒤. 결과서 본문은 이 줄들을 읽었으니 범위 표기만 다르다.
- ④ (보탬) 결과서 "핸들 칸" 의 `+0x00 = 0x3c8` · `+0x04 = 0x55555551` · `+0x10 = 1` 쓰는 줄 = `+0x49f` · `+0x4ab` · `+0x4b3` (flow-writer `.asm`).
- ⑤ (보탬, `[추측]`) **test.exe 같은 일반 프로세스에선 CFG 칸이 파일 값 그대로일 가능성** — run63 은 test.exe 에서 `LdrControlFlowGuardEnforced` 가 0 (`LdrSystemDllInitBlock +0xb8` CFG 비트맵 주소 0, 짐 지도 사진) 이라 `LdrpCfgProcessLoadConfig` 의 CFG 갈래를 안 탄다고 적었다. 이 갈래가 DLL 의 `__guard_dispatch_icall_fptr` 를 바꿔 쓰는 곳이라면 (`[추측]` — 쓰는 줄은 안 봄), bcrypt 의 간접 호출은 `jmp rax` 그대로다. 같은 꼴의 파일 값 (`_guard_dispatch_icall_nop` = `jmp rax`) 은 ntdll 칸 (run28, flow.md 2.7 `LdrpCallInitRoutineInternal` 절 — 실행 값은 동적 Q27) · test.exe 칸 (run30, 2.10 "test.exe 안 CFG") 에도 적혀 있다. 동적 ① 로 확인.
- ⑥ (보탬) 지도 (`entry_edges.tsv`) 에서 `VsmOpenAlgorithmProvider +0x16d` → `VsmLoadInterface` 를 확인했다. `_calls.tsv` 의 `VsmLoadInterface +0x16d` (간접) 는 **같은 숫자인 다른 줄** (VsmLoadInterface 안) 이니 헷갈리지 말 것.

## 정정

- **run94 (결과서 sast/90 4-b) 두 칸 설명이 틀렸다** — run94 문서 맨 위에 한 줄 넣음.
  - run94 "가장 작은 출력 한 장" 표 `0x60` 줄의 "`BCryptOpenAlgorithmProvider +0x4bb` 가 핸들 안에 복사" — 틀림. `+0x4bb wcsncpy_s` 의 원본은 `+0x49c MOV R8,R12` = **호출자 `pszAlgId`** (`+0x5f MOV R12,RDX`) 다. KsecDD 답의 `pszFunction` (`0x20` → `0x60` 문자열) 은 **`Get…Interface` 둘째 인자**로 간다 (`_LoadInterface +0x1cb MOV RDX,[R15+8]`).
  - run94 표 `0x28` 줄 `pszProvider` `-1` (sast/90 결과서는 "로그에만 씀") — 로그용이 아니다. **`Get…Interface` 첫 인자**로 간다 (`+0x1d2 MOV RCX,[R15+0x10]`, 번호 6 은 `+0x338`). `-1` 이면 NULL 이 진짜 DLL 에 간다. bcrypt 쪽 검사는 그대로 통과하므로 run94 최소 답이 `BCryptResolveProviders` 를 통과한다는 결론은 그대로. 진짜 DLL 이 NULL 을 받아 주는지는 `[공백]`.
  - run94 의 "`1 = 암호` 는 `[추측]`" → 번호 1 ↔ `GetCipherInterface` `[정적]` (이름 짝). 정정이라기보다 표시를 올림.
- run94 해설서 (`run94-bcrypt-resolve-해설.md` 529줄) 에도 같은 `0x60` 줄이 있다 — 해설서는 flow-writer 가 안 고침 (explainer 몫).

## 아직 확인 필요

**동적** (결과서 "동적분석으로 확인해야 할 것". 이번 묶음은 VM 안 함, 대기열 번호는 오케스트레이터가 붙임). **test.exe 로는 못 본다** (bcrypt 안 올림 `[관찰]` run44, sample 에 암호 API 없음 `[소스]`) → Q104 에 안 붙임. **Q179 (run94 동적) · Q174 와 같은 프로세스에서 묶을 후보.**

- ① (제일 중요) CFG 칸 실행 값 — `dq bcrypt!__guard_dispatch_icall_fptr L1` 이 `bcrypt+0x1c090` (파일 값) 인가, ntdll 안 함수인가 (`ln`). 후자면 그 함수가 `bcryptprimitives` 안 대상을 받아 주나.
- ② 진짜 KsecDD 답의 `pszProvider` · `pszFunction` (Q179 의 `BCryptResolveProviders+0x5f2` 한 곳에서 같이 나옴) — 특히 `pszProvider` 가 NULL 인가 `Microsoft Primitive Provider` 인가.
- ③ `Get…Interface` 가 실제로 주는 표 — 머리 첫 2바이트 · 칸 값 (NULL 없나).
- ④ 열기 함수가 받는 `pszAlgId` · 플래그 · 반환 status, 번호 2 · 3 의 `GetProperty` 반환.
- ⑤ `bcryptprimitives.dll` 이 bcrypt 열기 전에 이미 올라가 있나 (`lm m bcryptprimitives`), `_LoadImage +0xa4` 가 이름당 한 번만 걸리나 (목록 hit).
- 추천 멈춤점 (그 프로세스 조건. 심볼에 `_` 붙은 이름이 있으니 `x bcrypt!*LoadInterface*` 로 먼저 확인):
  - `bcrypt!_LoadInterface+0x130` (call 직전): `lm a @rcx` · `da @rdx` (고른 이름). `+0x137`: `ln @rax` (0 이면 `0xc0000139` 길). 비교용 `x bcryptprimitives!Get*Interface`.
  - `bcrypt!LoadProviderEx+0xf3` (`_LoadInterface` 반환): `eax` · `dq poi(@rsp+0x48) L0xe` (표 머리 · 칸).
  - `bcrypt!BCryptOpenAlgorithmProvider+0x70b`: `ecx` (번호) · `dq @rdx L0xe`. `+0x710`: `eax`.
  - `bcrypt!BCryptOpenAlgorithmProvider+0x851`: `ln @rax` · `du @rdx` · `r r8d`. `+0x856`: `eax`.
  - `bcrypt!BCryptOpenAlgorithmProvider+0xb19` / `+0xbcd` (번호 2 · 3): `du @rdx`. `+0xb1e` / `+0xbd2`: `eax`.
  - `bcrypt!BCryptOpenAlgorithmProvider+0xce7`: `dq @rax L0x79` (핸들 전체).

**정적** (결과서 "다음 요청 후보" + 보탬, 대기열 번호는 오케스트레이터가 붙임):

- ① (이 다음 벽) bcryptprimitives `GetCipherInterface` (`0xad50`) · `GetHashInterface` (`0xacb0`) · `GetRngInterface` (`0x48b90`) 본문과 그 표의 `+8` (열기) · `+0x10` (`GetProperty`) — `pszProvider` NULL 이어도 되나, `pszAlgId` 이름 검사, 표 머리 값, 열기가 쓰는 전역 · API. bcryptprimitives 는 run91 에서 analyze 됨.
- ② 난수 표 `+0x28` (`BCryptGenRandom +0x75` 가 부르는 함수) 과 `Close` 표 `+0x20` — test.exe 같은 프로그램이 난수를 받는 마지막 줄까지.
- ③ (= Q178b 그대로) `BCryptEnumProviders` · `BCryptEnumAlgorithms` 의 `ppBuffer` NULL 길, 알고리즘 ↔ `dwInterface` 전체 표 (`RSA` 같은 이름의 번호 선택 — 커널 `ksecdd.sys` 몫, Q173b ③ 과 겹침). 이번 표로 "번호 → 이름" 쪽은 덮였으니 남은 건 "알고리즘 → 번호" 만.
- ④ (보탬, 후순위) DLL 의 `__guard_dispatch_icall_fptr` 를 ntdll 이 바꿔 쓰는 줄 — run63 `LdrpCfgProcessLoadConfig` 의 CFG 갈래 (일반 프로세스에선 안 탐 `[정적]` run63) 가 그 자리인지. 동적 ① 이 더 빠를 수 있음.
- (그대로) Q173b ① ④ (`GetModuleHandleExW` · `LdrAddRefDll`, `NtClose` × 5, `NtTraceControl` × 4).

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 안 고침 — test.exe 는 bcrypt 를 안 올림, run92 · run94 와 같은 까닭):
  - 기준: bcrypt pdb 줄 · bcryptprimitives pdb 줄에 run96 몫, 근거 로그 run96 줄 (사진 안 씀).
  - 흐름도 앞 글: run96 문단 하나.
  - 3장 "bcrypt 시작 함수" 절: 제목에 run96 사슬, run94 하위 절 끝 "정적 →" 줄에 (해결됨 → run96), 새 하위 절 **"공급자 DLL 을 올린 뒤 — 이름 찾기 · 표 받기 · 표 검사 · 열기 (run96)"**.
  - 4장: run94 정적 줄 ① (해결됨 → run96), ④ 의 "번호 1 과 이름 짝" (해결됨 → run96), run94 PE_emulator 줄 `1 = 암호 [추측]` 에 (run96) 표시 · run94 정정 안내, run96 새 줄 셋.
  - 5장: run 표 · 03_sprint 요청서 번호 run96 줄.
- 짐 지도: **고치지 않음** (오케스트레이터 지시 "바꿀 사실이 있을 때만" — test.exe 는 bcrypt 를 안 올려 새 짐 칸 · 고칠 칸 없음, run94 와 같은 판단).
- run94 문서: 맨 위 정정 안내 한 줄.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음.
- **디버거가 붙어서 달라지는 점** `[추측]`: 이번에 읽은 줄 안에 디버거 칸을 읽는 줄은 없다. 단 CFG 칸 값은 프로세스 완화 정책 · 로더에 달렸으니, 동적 ① 은 디버거가 붙은 그 프로세스 값이라고 적어 둘 것.
- **함정 후보** (→ windbg-kd-runbook pitfalls 에 추가 제안): CFG 간접 호출 (`CALL bcrypt+0x1d010`) 은 중개가 `JMP` 뿐이라 **콜스택에 bcrypt 쪽 중개 프레임이 안 남는다** — `bcryptprimitives!GetCipherInterface` 에서 `k` 를 찍으면 바로 위가 `bcrypt!_LoadInterface+0x1e1` 로 보일 것 (run28 의 ntdll CFG 중개 — flow.md 2.7 "중개는 `JMP` 뿐이라 콜스택에 따로 안 보임" — 와 같은 꼴 `[추측]`). 멈춤점은 `_LoadInterface +0x1dc` (call 직전, `rax` = 대상) 에 거는 게 확실하다.
- 도구 (결과서 "스크립트 문제"): 없음.

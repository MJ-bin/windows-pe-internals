# 2026-11-13 run88 — SxS 활성화 문맥을 로더가 읽는 쪽: `LdrpLoadDependentModuleInternal +0x1426` → `RtlDosApplyFileIsolationRedirection_Ustr` → `sxsisol_SearchActCtxForDllName` → `RtlFindActivationContextSectionString` (Q151b, PE_emulator 요청 29 둘째 · 마지막 조각): **DLL 이름 리다이렉트가 KnownDll 보다 먼저다. 리다이렉트가 전체 경로를 주면 `LdrpLoadKnownDll` 이 "폴더가 System32 가 아님" 으로 거절하고 디스크에서 올린다. 찾는 차례는 스레드 활성화 문맥 → PEB `+0x2f8` (프로세스 기본) → `+0x308` (시스템 기본). 블롭 (`ACTIVATION_CONTEXT_DATA`) 은 위치를 전부 32비트 오프셋으로 적어서 어느 주소에 붙여도 읽히고, 안에는 조립 폴더 이름과 파일 이름 (키) 만 있다 — 루트 `C:\WINDOWS\WinSxS\` 는 ntdll 이 런타임에 붙인다. 매니페스트 없는 exe 도 시스템 기본 문맥 때문에 `comctl32.dll` 은 System32 KnownDll 이 아니라 WinSxS 5.82 폴더 경로로 간다 (파일 바이트는 System32 것과 같음). comctl32 v6 로 보내는 가장 작은 블롭은 `0x1dc`B (전체 경로 한 줄, 아래 "최소 블롭 B"). 요청 29 끝 (run87 + run88). Q90 문맥 쪽 닫힘.** ntdll `LdrpLoadDependentModuleInternal +0x1426/+0x142b RtlDosApplyFileIsolationRedirection_Ustr` (LoadLibrary 길은 `LdrpLoadDll +0xbd LdrpPreprocessDllName +0x622`) → `+0x484 sxsisol_SearchActCtxForDllName` → `+0xc9 RtlFindActivationContextSectionString(3, 0, 2, 이름, 결과)` → `+0xed RtlpFindNextActivationContextSection` (`+0x105` TEB `+0x2c8` · `+0x119` PEB `+0x2f8` · `+0x131` PEB `+0x308`) · `+0x20e RtlpFindUnicodeStringInSection` (해시 `IMUL 0x1003f`) → (찾으면) `sxsisol +0x215 RtlGetAssemblyStorageRoot` → … `RtlpProbeAssemblyStorageRootForAssembly +0x26c NtOpenFile` (WinSxS 폴더) → 돌아와 `LdrpLoadDependentModuleInternal +0x15af BTS ESI,9` → `+0x178d LdrpLoadKnownDll` (`+0x23 BT EAX,9` → `+0x32 LdrpCheckKnownDllFullPath` → `0xc0000135`) → `+0x1ca9 LdrpProcessWork` → `LdrpMapDllFullPath`

## PE_emulator 쪽에서 쓸 것

### 요청 29 전체 결론 (run87 + run88)

- **블롭은 누가 넣나** (run87 `[정적]`): 부모도 ntdll 도 아니고 **csrss** 다. 자식 PEB 에 주소 둘을 쓴다.
  - `PEB+0x308` `SystemDefaultActivationContextData` ← 시스템 기본 문맥. **매니페스트 없는 exe 에도 늘.** test.exe 사진 P0 `25996d00000` `[관찰]` run44.
  - `PEB+0x2f8` `ActivationContextData` ← 앱 문맥. **exe 매니페스트가 있을 때만.** test.exe 는 8장 모두 (null) `[관찰]` run44.
  - 둘 다 읽기 전용 공유 보기 (`NtMapViewOfSection` 보호 2) 로 붙는다.
- **블롭은 누가 읽나** (run88 `[정적]`): **ntdll 로더**가 DLL 을 올릴 때마다 읽는다. 이름을 받아 다듬는 자리 둘 — import 는 `LdrpLoadDependentModuleInternal +0x1426`, `LoadLibrary` 는 `LdrpPreprocessDllName +0x622`.
  - 찾는 차례: ① 스레드 활성화 문맥 스택 맨 위 (TEB `+0x2c8`) → ② `PEB+0x2f8` → ③ `PEB+0x308`. 앞에서 못 찾으면 다음으로.
  - 셋 다 없으면 바로 "리다이렉트 없음" (`0xC0150008`) 이고, 로더는 그냥 이름으로 간다.
- **리다이렉트가 KnownDll 을 이긴다** (run88 `[정적]`): 리다이렉트가 먼저 돌고, 전체 경로가 나오면 적재 문맥 플래그 비트 9 (`0x200`) 가 켜진다. 그러면 `LdrpLoadKnownDll` 은 `\KnownDlls` 를 안 열고 "경로의 폴더가 `LdrpKnownDllPath` (System32) 와 같나" 만 본다. WinSxS 폴더는 다르니 `0xc0000135` → 디스크 파일 길 (`NtOpenFile` · `NtCreateSection`).
- **엔진이 할 일** (판단, 결과서 질문 6 + run87 질문 5):
  - 블롭을 만드는 코드 (매니페스트 해석 · 정책 · 캐시) 는 흉내 안 낸다. **미리 만든 바이트 덩어리를 읽기 전용으로 붙이고 PEB 칸에 주소만 쓴다.** 블롭 안 위치는 전부 오프셋이라 어느 주소든 된다 (64KB 정렬은 윈도우가 그렇게 붙인다는 것뿐, 로더는 안 따짐).
  - comctl32 v6 가 필요하면: 아래 **최소 블롭 B** (`0x1dc`B) 를 붙이고 `PEB+0x2f8` 에 주소. 그다음 단계로 **A** (윈도우 방식, 키만). 둘 다 바뀌는 PEB 칸은 `+0x2f8` 하나다.
  - 시스템 기본 (`+0x308`) 까지 맞추려면: 진짜 시스템 기본 블롭 (`0x3084`B, 해시표 12원소) 을 떠 와 그대로 붙이는 게 가장 싸다 (손으로 만들기보다 복사). 떠 올 곳은 아래 "동적" (Q104 3).
  - **TEB `+0x2c8` 은 널이면 안 된다.** 로더가 널 검사 없이 따라간다 (`RtlFindActivationContextSectionString +0x141 ~ +0x148`). 빈 스택 (`ActiveFrame` 0) 을 가리키면 된다 — 진짜에선 TEB `+0x290` (`_ActivationStack`, 짐 지도 3.3).
  - 리다이렉트 → KnownDll 차례를 지킨다. "KnownDll 먼저" 로 짜 두었다면 블롭을 넣어도 v6 로 안 간다.
  - 리다이렉트된 경로의 파일이 엔진 파일 시스템에 있어야 한다. v6 파일 = `ms/bin/…/WinSxS/amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201/comctl32.dll` (SHA-256 `17de1830…`, flow-writer 다시 셈).
- **지금 PE_emulator 판정**:
  - "문맥 없음 → 리다이렉트 없음" 이라는 **결과 자체는 맞다** — 셋 다 없을 때 진짜 ntdll 도 같은 `0xC0150008` 를 낸다 (질문 2).
  - 그러나 **"문맥 없음 → System32 KnownDll `COMCTL32` (v5.82)" 는 틀림 (작게).** 실제 윈도우는 매니페스트 없는 exe 에도 시스템 기본 문맥이 있고, 거기 `comctl32.dll` 키가 있어서 `C:\WINDOWS\WinSxS\amd64_microsoft.windows.common-controls_6595b64144ccf1df_5.82.26100.8328_none_87eede957a2ccddd\comctl32.dll` 로 간다 (질문 5).
    - 파일 바이트는 System32 것과 같다 (이 레포 사본 SHA-256 둘 다 `67e04b3a…`, flow-writer 다시 셈). 그래서 **서수 #345 · #381 이 없는 문제는 그대로**다 — v5.82 는 v5.82 다.
    - 다른 것은 모듈 이름 · 경로 (`FullDllName`) 와 연 syscall (KnownDll `NtOpenSection` 대신 폴더 · 파일 `NtOpenFile` · `NtCreateSection`).
    - `sample/` 의 test.exe 는 comctl32 를 안 쓰므로 GUI 샘플 · notepad 부터 영향.
  - notepad 처럼 매니페스트로 v6 를 쓰는 exe 는 **앱 블롭 (`+0x2f8`) 이 없으면 v6 로 갈 길이 없다.** 그 블롭을 넣어야 원문 "서수 없음 `0xc0000138`" 벽이 풀린다 `[추측: 서수 #345 · #381 이 v6 에만 있다는 원문 관찰에 기댐]`.
- **확인거리** (결과서 "다음 요청 후보" 4 — PE_emulator 쪽, 우리 대기열 아님):
  - TEB `+0x2c8` 이 널 아닌 빈 스택을 가리키는지 (지금 엔진 TEB 값).
  - `PEB+0x2f8` 에 앱 블롭을 넣을 때 `ProcessParameters.Flags` 비트 13 (`0x2000`, 매니페스트 있음 — 커널 `PspLocateInPEManifest` 가 켬, 짐 지도 3.2) 도 같이 켜는지. 진짜에선 이 비트가 csrss 의 "앱 문맥을 만들까" 를 정한다 (run87). ntdll 쪽은 `PEB+0x2f8 ≠ 0` 이면 exe 자기 매니페스트를 다시 안 찾는다 (`LdrpFindDllActivationContext +0x16c`, run67).

### 원문 질문이 어디서 답해졌나

| 원문 요청 29 | 답한 곳 |
|---|---|
| 1 프로세스 기본 활성화 문맥을 누가 · 언제 만드나 (부모 → csrss → 자식 PEB, 섹션 매핑 · 주소, 매니페스트 있을 때 · 없을 때) | **run87** (sast/84) — csrss sxssrv 가 만들어 붙이고 PEB `+0x308` (늘) · `+0x2f8` (매니페스트 있을 때) 에 주소를 씀 |
| 2 로더가 DLL 이름을 리다이렉트하는 길 · 읽는 데이터 모양 (`ACTIVATION_CONTEXT_DATA` 머리 · 문자열 구역 최소 칸) | **run88** — 아래 "질문마다" 1 · 2 와 "`ACTIVATION_CONTEXT_DATA` 최소 칸 표" |
| 3 (가) 매니페스트 해석이 유저에서인가 csrss 에서인가 | **run87** — csrss (sxssrv 가 sxs.dll `SxsGenerateActivationContext` 를 부름) |
| 3 (나) comctl32 v6 최소 조건 · WinSxS 경로 규칙 | **run88** — 블롭엔 조립 폴더 이름 + 파일 이름 (키) 만, 루트는 ntdll 이 `RtlGetNtSystemRoot()` + `\WinSxS\` + 폴더 + `\` 로 조립. 최소 조건 A · B (아래). csrss 쪽 이름 → 폴더 이름 함수는 run87 |
| 4 시스템 기본 문맥만 있을 때 리다이렉트 결과 | **run88** — `comctl32.dll` → WinSxS 5.82 폴더 (KnownDll 아님). 시스템 기본 블롭 안 조립 6개 |
| (배경) nt 엔진이 직접 만들어 넣을 수 있나 · 최소 모양 | 판단 **run87** (블롭 떠 와 붙이고 주소만), 최소 모양 **run88** (블롭 B `0x1dc`B · A `0x21c`B) |
| (지금 우리) 문맥 없음 → System32 v5.82 → 서수 없음 `0xc0000138` | **run88** — 결과 "리다이렉트 없음" 은 맞지만 경로가 다름 (위 판정). 서수 문제는 v6 앱 블롭이 있어야 풀림 |

### 질문마다 (결과서 sast/85 질문 1 ~ 6, 근거 줄은 오케스트레이터 · flow-writer 가 `.asm` 으로 다시 봄)

| 질문 (요청서 번호) | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1 리다이렉트 길 · KnownDll 과 무엇이 먼저 | **리다이렉트가 먼저**. import 길 `+0x1426` (리다이렉트) → `+0x178d` (`LdrpLoadKnownDll`), LoadLibrary 길 `LdrpLoadDll +0xbd` (`LdrpPreprocessDllName +0x622` 리다이렉트) → `+0xfb … LdrpFindOrPrepareLoadingModule +0x5e0` (`LdrpLoadKnownDll`). 경로가 나오면 비트 9 → `LdrpCheckKnownDllFullPath` 가 폴더 비교 → System32 아니면 `0xc0000135` → 디스크 | 문맥 없을 때 결과는 맞음. **"KnownDll 먼저" 로 짰다면 틀림** | `LdrpLoadDependentModuleInternal +0x1426` · `+0x15af BTS ESI,9` · `+0x178d` (오케스트레이터 · flow-writer), `LdrpLoadKnownDll +0x23 BT EAX,9` · `+0x32` · `+0x3e JZ → +0x113 MOV EAX,0xc0000135` (오케스트레이터 · flow-writer), `LdrpProcessWork +0x5b BT EAX,9` → `+0x13f LdrpMapDllFullPath` |
| 2 어느 문맥을 보나 | ① TEB `+0x2c8` 스택 맨 위 틀의 문맥 → ② `PEB+0x2f8` → ③ `PEB+0x308`. 어디서 찾았는지는 결과 칸 `ulFlags` (`+0x44`) 0 · 1 · 2. **셋 다 없으면** `0xC0150001` → `sxsisol` 이 `0xC0150008` 로 바꿈 = 리다이렉트 없음 | 지금 (셋 다 없음) 결과는 맞음. 단 TEB `+0x2c8` 널이면 읽기 오류 | `RtlFindActivationContextSectionString +0x30` · `+0x134` · `+0x141 ~ +0x151`, `RtlpFindNextActivationContextSection +0x105` · `+0x119` · `+0x131` · `+0x192` (오케스트레이터 · flow-writer), `sxsisol_SearchActCtxForDllName +0xd8` · `+0xdf` |
| 3 `ACTIVATION_CONTEXT_DATA` 최소 칸 | 아래 표 A ~ F (+ A 방식에만 G). 모든 위치 칸이 32비트 오프셋 (블롭 시작 또는 구역 시작 기준), 8바이트 절대 주소를 따라가는 곳 없음 | 전부 새 정보 | 표의 "읽는 줄" 열 |
| 4 comctl32 v6 최소 조건 | DLL 구역 (id 2) 에 키 `comctl32.dll` 한 줄 + 값. 값은 (A) 키만 — 플래그 2, 폴더는 조립 명부에서 (윈도우 방식) 또는 (B) 전체 경로 — 플래그 1, 조각 1개. **"블롭 안에 전체 경로냐 키만이냐" 닫힘 → 윈도우는 키만** | 없음 (틀림) — A 나 B 를 넣어야 v6 폴더로 감 | `sxsisol +0x1cb TEST AL,2` → `+0x215 RtlGetAssemblyStorageRoot`, `+0x337 TEST AL,1`, sxs `SxspDllRedirectionStringSectionGenerationCallback` (쓰는 쪽) |
| 5 시스템 기본 문맥만 있을 때 | **v5.82 로 가되 WinSxS 5.82 폴더 경로로** (KnownDll 거절). 시스템 기본 블롭엔 v6 항목이 없다 — v6 는 exe 매니페스트가 있어 앱 블롭이 생길 때만 | **틀림 (작게)** — 경로 · syscall 이 다름, 파일 바이트는 같음 | 사진 덤프의 시스템 기본 블롭 `[관찰]` (사진 덤프 오프라인 읽기, 물리 연속 가정 — test.exe `25996d00000` 이 그 블롭인지는 `[확인 필요]`) + 질문 1 · 3 의 줄 |
| 6 엔진이 흉내 낼 최소 블롭 · 해시 | 블롭 B (`0x1dc`B, 전체 경로) 먼저, 다음 A (`0x21c`B, 키만). 해시 알고리즘 1 (X65599) | 새 설계 | `min_A.bin` · `min_B.bin` (커밋 안 됨 — 아래 표로 다시 만들 수 있게 적음) |

### `ACTIVATION_CONTEXT_DATA` 최소 칸 표 — 로더가 DLL 이름을 찾을 때 읽는 칸만

- 이름: PDB 에 있는 건 `_ACTIVATION_CONTEXT_DATA` (표 A) 하나뿐. 나머지는 공개 이름 `[추측: 이름]`.
- "실제" 열 = 사진 덤프에서 읽은 값 `[관찰]` (위 질문 5 줄과 같은 단서 — **오프라인 · 물리 연속 가정, test.exe `25996d00000` 이 그 블롭인지는 `[확인 필요]`**). 앱 블롭 = 물리 `0xd4c17000` (comctl32 v6 를 쓰는 다른 GUI 프로세스 것), 시스템 기본 = 물리 `0x1e4892000`. 사진 이름은 아래 "사진 덤프 대조".
- 단 **시스템 기본 블롭의 머리 `0x20`B 는 test.exe `25996d00000` 에서 직접 읽은 값과 같다** `[관찰]` run44 (`ms/logs/04_baggage-read_P1b.txt:4469 ~ 4470` · `_P2.txt:4481 ~ 4482` — `Magic 0x78746341` · `HeaderSize 0x20` · `FormatVersion 1` · `TotalSize 0x3084` · `DefaultTocOffset 0xdc` · `ExtendedTocOffset 0` · `AssemblyRosterOffset 0x20` · `Flags 0`, flow-writer). 나머지 `0x3064`B 는 `[확인 필요]`.
- "B 값" 열 = 아래 최소 블롭 B 의 값.

**A. 블롭 머리** (블롭 `+0`, `0x20`B, PDB `_ACTIVATION_CONTEXT_DATA`)

| 오프셋 | 크기 | 이름 (PDB) | 뜻 | 읽는 줄 | 실제 (앱 / 시스템 기본) | B 값 |
|---|---|---|---|---|---|---|
| `+0x0` | 4 | `Magic` | `0x78746341` (메모리 `41 63 74 78` = "Actx") | 이 길에선 안 읽음 (만들 때 검사 `RtlpValidateActivationContextData`) | `0x78746341` | `0x78746341` |
| `+0x4` | 4 | `HeaderSize` | `0x20` 이상 | `RtlpLocateActivationContextSection +0x3e` | `0x20` | `0x20` |
| `+0x8` | 4 | `FormatVersion` | 1 | 만들 때 검사만 | 1 | 1 |
| `+0xc` | 4 | `TotalSize` | 블롭 전체 크기. 모든 오프셋의 경계 검사에 씀 | `Locate +0x2a` | `0x2050` / `0x3084` | `0x1dc` |
| `+0x10` | 4 | `DefaultTocOffset` | 목차 머리 위치 (블롭 기준, 4의 배수, 0 아님) | `Locate +0x58` | `0x94` / `0xdc` | `0x4c` |
| `+0x14` | 4 | `ExtendedTocOffset` | GUID 로 찾는 구역용. DLL 이름 찾기에선 안 씀 | `Locate +0x1b4` (GUID 줄 때만) | 0 | 0 |
| `+0x18` | 4 | `AssemblyRosterOffset` | 조립 명부 머리 위치 (블롭 기준) | `RtlpResolveAssemblyStorageMapEntry +0x14a`, `RtlpGetActivationContextDataStorageMapAndRosterHeader +0xe1` · `+0x22f` (A 방식만) | `0x20` | `0x20` |
| `+0x1c` | 4 | `Flags` | 이 길에선 안 읽음 | — | 0 | 0 |

**B. 목차 (TOC)** — 머리는 블롭 `+DefaultTocOffset`. 이름 `[추측: ACTIVATION_CONTEXT_DATA_TOC_HEADER / _ENTRY]`

| 오프셋 | 크기 | 이름 | 뜻 | 읽는 줄 | 실제 | B 값 |
|---|---|---|---|---|---|---|
| 머리 `+0x0` | 4 | `HeaderSize` | `0x10` 이상 (만들 때 검사) | validate | `0x10` | `0x10` |
| 머리 `+0x4` | 4 | `EntryCount` | 항목 개수 | `Locate +0x6f` | 앱 9, 시스템 9 | 1 |
| 머리 `+0x8` | 4 | `FirstEntryOffset` | 항목 배열 위치 (**블롭** 기준, 4의 배수) | `Locate +0x82` | 앱 `0xa4` | `0x5c` |
| 머리 `+0xc` | 4 | `Flags` | 비트 0 = 빽빽 (id 가 연속 — `id − 첫 id` 로 바로, `+0x10d`), 비트 1 = id 순 정렬 (이분 탐색 `+0xf4` · `+0x140 bsearch`). 둘 다 0 이면 처음부터 훑음 | `Locate +0xed` | 2 | 2 |
| 항목 `+0x0` | 4 | `SectionId` | **DLL 리다이렉트 = 2** (`sxsisol +0x6a MOV R8D,2`). 참고 (덤프 목차에서 본 번호, 이름은 공개 이름 `[추측]`): 1 조립 정보 · 3 창 클래스 · 4 COM 서버 · 5 COM 인터페이스 · 6 타입 라이브러리 · 7 ProgID · 9 CLR · 10 앱 설정 · 11 호환성 · 12 WinRT | `Locate` 비교 | 2 | 2 |
| 항목 `+0x4` | 4 | `Offset` | 구역 위치 (**블롭** 기준, 0 아님, `< TotalSize`) | `Locate +0x151` | 앱 `0xb1c` | `0x6c` |
| 항목 `+0x8` | 4 | `Length` | 구역 길이 (`Offset + Length ≤ TotalSize`) | `Locate +0x15e` | 앱 `0xc4` | `0x170` |
| 항목 `+0xc` | 4 | `Format` | 이 길에선 안 읽음 | — | 1 | 1 |
| (항목 크기 `0x10`) | | | | | | |

**C. 문자열 구역 머리** (구역 `+0`, `0x2c`B) — `RtlFindActivationContextSectionString +0x1e3 CMP [R15],0x64487353` 이 Magic 을, 구역 길이 `≥ 0x2c` 를 본다. 칸 읽기는 `RtlpFindUnicodeStringInSection`. 이름 `[추측: ACTIVATION_CONTEXT_STRING_SECTION_HEADER]`

| 오프셋 | 크기 | 이름 | 뜻 | 읽는 줄 | 실제 (앱 / 시스템 기본) | B 값 |
|---|---|---|---|---|---|---|
| `+0x0` | 4 | `Magic` | `0x64487353` ("SsHd") | `+0x37` | 같음 | `0x64487353` |
| `+0x4` | 4 | `HeaderSize` | `0x2c`. 이 길 (플래그 3) 에선 안 읽음 | — | `0x2c` | `0x2c` |
| `+0x8` | 4 | `FormatVersion` | **1 이어야 해시표를 씀** | `+0x122 CMP [RDI+8],1` | 1 | 1 |
| `+0xc` | 4 | `DataFormatVersion` | 결과 칸 `ulDataFormatVersion` 으로 복사. `sxsisol` 은 **1 이어야** 진행 | 복사 `+0x2d8`, `sxsisol +0x172 CMP [RSP+0x64],1` | 1 | 1 |
| `+0x10` | 4 | `Flags` | **비트 0 = 대소문자 무시** (해시 전에 대문자로, 비교 5번째 인자), **비트 1 = 원소가 PseudoKey 순 정렬** (이분 탐색 `+0x135` → `+0x188 bsearch`) | `+0x24`, `+0x135` | 앱 3, 시스템 1 | 3 |
| `+0x14` | 4 | `ElementCount` | 원소 개수. **0 이면 못 찾음** (`0xC0150008`) | `+0x6b` | 앱 2, 시스템 12 | 1 |
| `+0x18` | 4 | `ElementListOffset` | 원소 배열 위치 (**구역** 기준) | `+0x148`, `+0x333` | `0x2c` | `0x2c` |
| `+0x1c` | 4 | `HashAlgorithm` | **1 = 곱 65599 (`0x1003f`) 해시** (`+0x100 IMUL R10D,R10D,0x1003f`). 0 도 같은 계산. **`0xFFFFFFFF` = 해시 안 씀** (`+0x88 CMP EAX,-1`, 이름 비교만). 그 밖 숫자는 "지원 안 함" 오류 | `+0x75 ~ +0x8b` | 1 | 1 |
| `+0x20` | 4 | `SearchStructureOffset` | 해시표 위치 (**구역** 기준). 0 이면 해시표 없음 → 정렬 / 선형 | `+0x395` | 앱 0, 시스템 `0x464` | 0 |
| `+0x24` | 4 | `UserDataOffset` | 구역 공용 자료 (결과 `lpSectionGlobalData`). 0 = 없음 | `FillOut` 인자 | 0 | 0 |
| `+0x28` | 4 | `UserDataSize` | 위 크기 | 〃 | 0 | 0 |

**D. 문자열 구역 원소** (`0x18`B, 구역 `+ElementListOffset`). 이름 `[추측: ACTIVATION_CONTEXT_STRING_SECTION_ENTRY]`

| 오프셋 | 크기 | 이름 | 뜻 | 읽는 줄 | 실제 (`comctl32.dll`, 앱) | B 값 |
|---|---|---|---|---|---|---|
| `+0x0` | 4 | `PseudoKey` | 키 해시 (아래 "해시") | `+0x2c0` · `+0x31f` · `+0x353` · `+0x3f5` 비교 | `0xf1c4bc4f` | `0xf1c4bc4f` |
| `+0x4` | 4 | `KeyOffset` | 키 문자열 위치 (**구역** 기준, UTF-16, 끝 NUL 없음) | `+0x28c MOV R8D,[RBP+4]` | `0x5c` | `0x44` |
| `+0x8` | 4 | `KeyLength` | 키 **바이트** 수 (16비트만 씀) | `+0x290 MOVZX R9D,word [RBP+8]` | `0x18` | `0x18` |
| `+0xc` | 4 | `Offset` | 값 위치 (**구역** 기준). **0 이면 결과를 안 채움** | `+0x2c9 CMP [RBP+0xc],0` | `0x78` | `0x5c` |
| `+0x10` | 4 | `Length` | 값 길이. DLL 은 **`0x14` 이상** | `+0x2f5` (결과 쓰기), `sxsisol +0x163 CMP [RSP+0x70],0x14` · `+0x16c JC` | `0x14` | `0x114` |
| `+0x14` | 4 | `AssemblyRosterIndex` | 이 DLL 이 속한 조립 (명부 번호). **0 은 못 씀** (자리 채움) | `+0x4a7` → 결과 `+0x40` → `RtlGetAssemblyStorageRoot` 인자 | 2 | 1 (B 에선 안 읽음) |

**E. 해시표** (`SearchStructureOffset ≠ 0` 일 때만 — 시스템 기본 블롭이 이 모양. 최소 블롭은 안 씀)

| 위치 (구역 기준) | 크기 | 뜻 | 읽는 줄 |
|---|---|---|---|
| `+SearchStructureOffset+0` | 4 | 칸 (bucket) 개수 N (실제 3) | `+0x395 MOV EDX,[RDI+0x20]` 뒤 나머지 연산 |
| `+SearchStructureOffset+4` | 4 | 칸 배열 위치 (실제 `0x46c` = 바로 뒤) | 〃 |
| 칸 i (8B) | 4 + 4 | `{체인 길이, 체인 위치}`, `i = PseudoKey % N` | 〃 |
| 체인 | 4 × 길이 | **원소의 구역 안 오프셋** (원소 번호 아님) | 〃 |

**F. DLL 리다이렉트 값** (원소 `Offset` 이 가리키는 곳). 이름 `[추측: ACTIVATION_CONTEXT_DATA_DLL_REDIRECTION / _PATH_SEGMENT]`. 읽는 곳은 전부 `sxsisol_SearchActCtxForDllName`

| 오프셋 (값 기준) | 크기 | 이름 | 뜻 | 읽는 줄 | B 값 |
|---|---|---|---|---|---|
| `+0x0` | 4 | `Size` | 값 머리 크기 (`0x14` 이상). 이 길에선 값을 안 읽음 | — | `0x14` |
| `+0x4` | 4 | `Flags` | **비트 0 (1)** = 조각이 파일 이름까지 포함 → 이름을 안 붙임 (`+0x337`). **비트 1 (2)** = 조립 폴더 루트를 앞에 붙임 (`+0x1cb` → `+0x215 RtlGetAssemblyStorageRoot`). **비트 2 (4)** = 환경변수 펼침 (`+0x3d9` → `+0x3f9`) — 비트 1 과 같이 켜면 거부 (`+0x1d7` → `+0x5c8 MOV EBX,0xc0150003`). **비트 3 (8)** = 이름은 마지막 `\` 뒤 조각만 (`+0x33f`) | 위 | **1** |
| `+0x8` | 4 | `TotalPathLength` | 조각 길이 합 (바이트). 이 길에선 안 읽음 | — | `0xf8` |
| `+0xc` | 4 | `PathSegmentCount` | 조각 개수 (`< 0x20000000`) | `+0x193` | 1 |
| `+0x10` | 4 | `PathSegmentOffset` | 조각 배열 위치 (**구역** 기준 — 결과 칸 `lpSectionBase` 가 구역 시작이라서) | `+0x186`, 경계 `+0x1ba` | `0x70` |
| 조각 `+0x0` | 4 | `Length` | 바이트 (16비트만 씀) | 조각 읽는 반복 | `0xf8` |
| 조각 `+0x4` | 4 | `Offset` | 문자열 위치 (**구역** 기준), UTF-16, NUL 없음 | 〃 | `0x78` |

- 경로를 만드는 차례 (`sxsisol_SearchActCtxForDllName`): ① 비트 1 이면 저장소 루트 문자열 먼저 (`+0x2f0` 복사) → ② 조각들 (`+0x3b5`) → ③ 비트 0 이 꺼져 있으면 **물어본 이름 그대로** (비트 3 이면 마지막 조각만, `+0x506`) → ④ 비트 2 면 환경변수 펼침 (`+0x3f9`) → ⑤ "리다이렉트 됨" 표시. 복사는 셋 다 `memcpy` (같은 주소 이름 2개 — `memcpy` · `memmove`, `pdb_aliases.py --lookup ntdll.dll 0x166500`).
- 붙는 이름은 물어본 철자 그대로다 (`COMCTL32.dll` 로 물으면 `…\COMCTL32.dll`). 키 비교는 대소문자 무시지만 결과에 키의 철자를 안 쓴다 (결과서 미니 해석기).

**G. 조립 명부 · 조립 정보** — 값 플래그 비트 1 (A 방식) 일 때만 읽는다. 이름 `[추측]`

| 위치 | 크기 | 이름 | 뜻 | 읽는 줄 | 실제 |
|---|---|---|---|---|---|
| 명부 머리 `+0x0` | 4 | `HeaderSize` | `0x14` 이상 (만들 때 검사) | validate | `0x14` |
| `+0x4` | 4 | `HashAlgorithm` | 1 | — | 1 |
| `+0x8` | 4 | `EntryCount` | **명부 번호 < EntryCount** | `RtlGetAssemblyStorageRoot +0xba` | 앱 4, 시스템 7 |
| `+0xc` | 4 | `FirstEntryOffset` | 명부 항목 배열 (**블롭** 기준) | `RtlpResolveAssemblyStorageMapEntry +0x151` | `0x34` |
| `+0x10` | 4 | `AssemblyInformationSectionOffset` | 조립 정보 안 문자열 오프셋의 **기준점** (블롭 기준) | `ResolveEntry +0x15d` | 앱 `0x134` |
| 명부 항목 (`0x18`B) `+0x0` | 4 | `Flags` | 0번은 1 (자리 채움), 루트 조립 2, 나머지 0 | — | |
| `+0x4` · `+0x8` · `+0xc` | 4 × 3 | `PseudoKey` · `NameOffset` · `NameLength` | 조립 이름 (이 길에선 안 읽음) | — | |
| `+0x10` | 4 | `AssemblyInformationOffset` | 조립 정보 위치 (**블롭** 기준) | `ResolveEntry +0x158` | 앱[2] `0x3bc` |
| `+0x14` | 4 | `AssemblyInformationLength` | 이 길에선 안 읽음 | — | |
| 조립 정보 `+0x4` | 4 | `Flags` | **비트 4 (`0x10`) 꺼짐** = 보통 WinSxS 조립. 켜짐 = 매니페스트 파일이 있는 폴더가 곧 루트 (`ResolveEntry +0x178`) | 〃 | 조립[2] 0, exe 매니페스트 조립[1] `0x11` |
| `+0x50` | 4 | `AssemblyDirectoryNameLength` | 폴더 이름 **바이트** 수 (`< 0xffff`) | `ResolveEntry +0x167` · `+0x2a1` | `0xba` |
| `+0x54` | 4 | `AssemblyDirectoryNameOffset` | 폴더 이름 위치 (명부 머리 `+0x10` 기준점 기준) | `ResolveEntry +0x2b3` | 앱[2] `0x508` |
| (조립 정보 크기 `0x6c`. 나머지 칸은 이 길에서 안 읽음) | | | | | |

- 저장소 루트 문자열은 **블롭에 없다.** ntdll 이 런타임에 `RtlGetNtSystemRoot()` (= KUSD `0x7ffe0030` 의 `C:\WINDOWS`) + `\WinSxS\` + 폴더 이름 + `\` 로 짓는다 (`RtlpAssemblyStorageMapResolutionDefaultCallback +0x1df` · `+0x240`, `RtlpProbeAssemblyStorageRootForAssembly +0x11a` · `+0x141`).
- **엔진이 미리 넣을 필요 없는 것**: PEB `+0x300` `ProcessAssemblyStorageMap` · `+0x310` `SystemAssemblyStorageMap` — 처음 루트를 구할 때 ntdll 이 프로세스 힙에 만들어 `+0x292 CMPXCHG.LOCK` 으로 넣는다 (아래 "확인된 것", 짐 지도 3.1).
- 결과 칸 `ACTCTX_SECTION_KEYED_DATA` (`0x70`B) 는 블롭이 아니라 함수 사이 약속이다 — `+0x28 lpSectionBase` 가 **구역 시작** (블롭 시작 아님), `+0x40` 명부 번호, `+0x44 ulFlags` (0 스레드 · 1 프로세스 기본 · 2 시스템 기본), `+0x38 hActCtx` (기본 문맥이면 0). 표 전체는 결과서 3-H.

### comctl32 v6 최소 조건 — A (키만) / B (전체 경로)

공통 (둘 다):

- `PEB+0x2f8` (또는 `+0x308`) = 블롭 주소, 블롭이 읽힘. TEB `+0x2c8` 이 널 아님.
- 리다이렉트를 건너뛰는 경우가 아님: 보안 프로세스 아님 (`LdrpIsSecureProcess` 0 — `LdrpLoadDependentModuleInternal +0x13e7`), 이름이 API set 이 아님 (API set 이름으로 바뀐 길은 `ProcessParameters.Flags & 0x1000` (`.local`) 이 꺼져 있으면 건너뜀 — `+0x62f` · `+0x638` · `+0x661 ~ +0x664`), `.local` 이 먼저 찾지 않음 (`RtlDosApplyFileIsolationRedirection_Ustr +0x443` · `+0x455`).
- 블롭 안: 목차에 id 2 구역, 구역 `DataFormatVersion` 1, 키 `comctl32.dll` (구역 Flags 비트 0 이면 대소문자 무시), 값 길이 `≥ 0x14`.
- 그 뒤: 경로 폴더 ≠ System32 → KnownDll 거절 → `LdrpProcessWork` → `LdrpMapDllFullPath` → `LdrpMapDllNtFileName +0x132 NtOpenFile` · `+0x18f NtCreateSection` → 붙이기. 엔진 파일 시스템에 그 파일이 있어야 한다.

| | (A) 키만 — 윈도우가 실제로 쓰는 방식 | (B) 전체 경로 — 로더가 읽을 수 있는 방식 |
|---|---|---|
| 값 `Flags` | 2 (루트 붙임), 조각 0개 | 1 (조각이 파일 이름까지), 조각 1개 |
| 폴더 이름은 어디서 | 조립 명부 → 조립 정보 `+0x54` (`…6.0.26100.9278_none_3e0d1ba8e3303201`) | 조각 문자열 안에 통째로 |
| 루트 `C:\WINDOWS\WinSxS\` | ntdll 이 런타임에 붙임 | 블롭에 박힘 `[추측: 엔진의 시스템 루트가 C:\WINDOWS 가 아니면 B 문자열도 바꿔야]` |
| 런타임에 더 타는 것 | 저장소 맵 (힙 할당 2번, PEB `+0x300` 또는 `+0x310`), `NtOpenKey` (`…\SideBySide\AssemblyStorageRoots` — 없어도 됨 `0xC0000034`), **폴더 `NtOpenFile`** (접근 `0x100020`, 공유 3, 옵션 `0x21`) — 폴더가 없으면 후보를 돌다 오류 | 없음 (명부 · 저장소 맵 · 레지스트리 · 폴더 열기 안 탐) |
| 한계 | — | 명부를 읽는 다른 API (`QueryActCtx` 류) 엔 못 씀. 폴더가 정말 있는지는 안 보고 파일 열기에서 바로 터짐 |
| 윈도우 근거 | 사진 덤프 앱 블롭 (`comctl32.dll` → 플래그 2 · 조각 0 · 명부[2] Common-Controls 6.0.26100.9278) `[관찰]` + sxs `SxspDllRedirectionStringSectionGenerationCallback` (`loadFrom` 없으면 `플래그 \|= 2`, 조각 0) `[정적]` | 윈도우는 매니페스트에 `loadFrom=` 이 있을 때 이 모양 (`CDllRedir::ContributorCallback`) `[정적]` |

### 해시 (알고리즘 1)

- 구역 머리 `HashAlgorithm = 1` (`HASH_STRING_ALGORITHM_X65599` `[추측: 공개 이름]`). 식: 키를 **먼저 대문자로** 바꾸고, 글자마다 `h = h × 0x1003f + 글자` (32비트에서 넘침 버림). `a ~ z` 는 `−0x20`, `0xC0` 이상 글자는 ntdll NLS 대문자표 (결과서) — ASCII 키는 단순.
  - `comctl32.dll` → `COMCTL32.DLL` → **`0xf1c4bc4f`** (오케스트레이터 · flow-writer 파이썬으로 다시 셈 — 실제 블롭 `PseudoKey` 와 같음 `[관찰]`).
  - `comctl32.dll.mui` → `0xbbf34ea2` (실제 블롭과 같음 `[관찰]`).
  - 우리 말 코드 (MS 코드 아님): `h = 0; for c in key.upper(): h = (h * 0x1003f + ord(c)) & 0xffffffff`.
- 구역 Flags 비트 0 이 꺼진 (대소문자 구분) 구역이면 대문자 변환 없이 같은 식.
- **`HashAlgorithm = 0xFFFFFFFF` 면 해시를 안 쓴다** — `PseudoKey` 를 안 보고 문자열만 비교 (`RtlpFindUnicodeStringInSection +0x88 CMP EAX,-1`). 엔진이 해시 계산을 피하고 싶으면 가능. 단 윈도우가 만든 블롭과는 다르다.

### 최소 블롭 B — 표만 보고 다시 만들기 (총 `0x1dc`B)

- 블롭 파일 `ms/disassemble/ntdll/_sast85/min_B.bin` 은 `ms/` 라 커밋 안 된다. 아래 표가 그 파일과 **바이트 단위로 같다** (flow-writer 가 작은 파서로 칸마다 대조).
- 숫자 칸은 모두 4바이트 little-endian. 문자열은 UTF-16LE, 끝 NUL 없음. 비는 바이트 없음 (마지막 칸이 정확히 `0x1dc` 에서 끝남).
- "기준" 열: 그 값이 오프셋이면 어디서부터 세는지 (괄호 = 블롭 안 절대 위치).

| 블롭 오프셋 | 크기 | 칸 | 값 | 기준 (가리키는 곳) |
|---|---|---|---|---|
| `0x00` | 4 | 머리 `Magic` | `0x78746341` | |
| `0x04` | 4 | 머리 `HeaderSize` | `0x20` | |
| `0x08` | 4 | 머리 `FormatVersion` | 1 | |
| `0x0c` | 4 | 머리 `TotalSize` | `0x1dc` | = 파일 크기 |
| `0x10` | 4 | 머리 `DefaultTocOffset` | `0x4c` | 블롭 (→ `0x4c`) |
| `0x14` | 4 | 머리 `ExtendedTocOffset` | 0 | |
| `0x18` | 4 | 머리 `AssemblyRosterOffset` | `0x20` | 블롭 (→ `0x20`) |
| `0x1c` | 4 | 머리 `Flags` | 0 | |
| `0x20` | 4 | 명부 `HeaderSize` | `0x14` | |
| `0x24` | 4 | 명부 `HashAlgorithm` | 1 | |
| `0x28` | 4 | 명부 `EntryCount` | 1 | |
| `0x2c` | 4 | 명부 `FirstEntryOffset` | `0x34` | 블롭 (→ `0x34`) |
| `0x30` | 4 | 명부 `AssemblyInformationSectionOffset` | `0x4c` | 블롭 (안 읽음) |
| `0x34` | `0x18` | 명부[0] | `Flags` 1, 나머지 5칸 0 | 자리 채움 (번호 0 은 못 씀) |
| `0x4c` | 4 | 목차 `HeaderSize` | `0x10` | |
| `0x50` | 4 | 목차 `EntryCount` | 1 | |
| `0x54` | 4 | 목차 `FirstEntryOffset` | `0x5c` | 블롭 (→ `0x5c`) |
| `0x58` | 4 | 목차 `Flags` | 2 | |
| `0x5c` | 4 | 목차[0] `SectionId` | **2** (DLL 리다이렉트) | |
| `0x60` | 4 | 목차[0] `Offset` | `0x6c` | 블롭 (→ `0x6c` = 구역 시작) |
| `0x64` | 4 | 목차[0] `Length` | `0x170` | `0x6c + 0x170 = 0x1dc` |
| `0x68` | 4 | 목차[0] `Format` | 1 | |
| `0x6c` | 4 | 구역 `Magic` | `0x64487353` | |
| `0x70` | 4 | 구역 `HeaderSize` | `0x2c` | |
| `0x74` | 4 | 구역 `FormatVersion` | 1 | |
| `0x78` | 4 | 구역 `DataFormatVersion` | 1 | |
| `0x7c` | 4 | 구역 `Flags` | **3** (대소문자 무시 + 정렬) | |
| `0x80` | 4 | 구역 `ElementCount` | 1 | |
| `0x84` | 4 | 구역 `ElementListOffset` | `0x2c` | 구역 (→ `0x98`) |
| `0x88` | 4 | 구역 `HashAlgorithm` | 1 | |
| `0x8c` | 4 | 구역 `SearchStructureOffset` | 0 | 해시표 없음 |
| `0x90` | 4 | 구역 `UserDataOffset` | 0 | |
| `0x94` | 4 | 구역 `UserDataSize` | 0 | |
| `0x98` | 4 | 원소[0] `PseudoKey` | **`0xf1c4bc4f`** | |
| `0x9c` | 4 | 원소[0] `KeyOffset` | `0x44` | 구역 (→ `0xb0`) |
| `0xa0` | 4 | 원소[0] `KeyLength` | `0x18` | 바이트 (12글자) |
| `0xa4` | 4 | 원소[0] `Offset` | `0x5c` | 구역 (→ `0xc8`) |
| `0xa8` | 4 | 원소[0] `Length` | **`0x114`** | `0xc8 ~ 0x1dc` (값 + 조각 + 문자열) |
| `0xac` | 4 | 원소[0] `AssemblyRosterIndex` | 1 | 안 읽음 (0 은 못 씀) |
| `0xb0` | `0x18` | 키 | `comctl32.dll` | |
| `0xc8` | 4 | 값 `Size` | `0x14` | |
| `0xcc` | 4 | 값 `Flags` | **1** | |
| `0xd0` | 4 | 값 `TotalPathLength` | `0xf8` | |
| `0xd4` | 4 | 값 `PathSegmentCount` | 1 | |
| `0xd8` | 4 | 값 `PathSegmentOffset` | `0x70` | 구역 (→ `0xdc`) |
| `0xdc` | 4 | 조각[0] `Length` | `0xf8` | 바이트 (124글자) |
| `0xe0` | 4 | 조각[0] `Offset` | `0x78` | 구역 (→ `0xe4`) |
| `0xe4` | `0xf8` | 경로 문자열 | `C:\WINDOWS\WinSxS\amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201\comctl32.dll` | 끝 = `0x1dc` |

- 결과 (결과서 미니 해석기 — 진짜 ntdll 로 돌린 건 아님 `[확인 필요]`): `comctl32.dll` (대소문자 아무거나) → 위 경로 문자열 그대로. `user32.dll` → `0xC0150008` (리다이렉트 없음).
- 결과서가 두 최소 블롭이 `RtlpValidateActivationContextData` 의 만들 때 검사 규칙도 통과한다고 확인 `[정적]`.

**최소 블롭 A — 키만 (윈도우 방식, 총 `0x21c`B)** — 결과서 표 그대로, 차이만 flow-writer 가 파일로 대조:

| 오프셋 | 크기 | 칸 | 값 |
|---|---|---|---|
| `0x00` | `0x20` | 블롭 머리 | B 와 같되 `TotalSize 0x21c`, `DefaultTocOffset 0x64` |
| `0x20` | `0x14` | 명부 머리 | `HeaderSize 0x14`, `HashAlgorithm 1`, `EntryCount` **2**, `FirstEntryOffset 0x34`, `AssemblyInformationSectionOffset 0x84` |
| `0x34` | `0x18` | 명부[0] | `Flags` 1, 나머지 0 |
| `0x4c` | `0x18` | 명부[1] | `Flags` 0, `PseudoKey` 0, `NameOffset` 0, `NameLength` 0, `AssemblyInformationOffset 0x84`, `AssemblyInformationLength` **`0x128`** (결과서 표는 `0x126` — 파일은 `0x128` = `0x6c + 0xbc`, 이 길에선 안 읽음) |
| `0x64` | `0x10` | 목차 머리 | `HeaderSize 0x10`, `EntryCount 1`, `FirstEntryOffset 0x74`, `Flags 2` |
| `0x74` | `0x10` | 목차[0] | `SectionId 2`, `Offset 0x1ac`, `Length 0x70`, `Format 1` |
| `0x84` | `0x6c` | 조립 정보 | `Size 0x6c`, `Flags` **0**, `+0x50` 폴더 이름 길이 `0xba`, `+0x54` 폴더 이름 위치 `0x6c` (기준점 `0x84` → `0xf0`), 나머지 0 |
| `0xf0` | `0xbc` | 폴더 이름 | `amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201` (`0xba`B) + 패딩 2 |
| `0x1ac` | `0x2c` | 문자열 구역 머리 | B 와 같음 |
| `0x1d8` | `0x18` | 원소[0] | `PseudoKey 0xf1c4bc4f`, `KeyOffset 0x44`, `KeyLength 0x18`, `Offset 0x5c`, `Length 0x14`, `AssemblyRosterIndex` **1** |
| `0x1f0` | `0x18` | 키 | `comctl32.dll` |
| `0x208` | `0x14` | 값 | `Size 0x14`, `Flags` **2**, 나머지 0 |

### 시스템 기본 문맥만 있을 때 (test.exe 같은 매니페스트 없는 exe)

- 사진 덤프의 **진짜 시스템 기본 블롭** (AMD64, `0x3084`B, 이름표 "System Default Context") `[관찰]`:
  - 조립 6개: `Microsoft.Windows.SystemCompatible 6.0.26100.1` (루트) · `IsolationAutomation 1.0.26100.5074` (`sxsoa.dll`) · `GdiPlus 1.1.26100.9444` (`GdiPlus.dll`) · **`Common-Controls 5.82.26100.8328` (`comctl32.dll`)** · `IsolationAutomation.ProxyStub` (`sxsoaps.dll` / `.tlb`) · `Common-Controls.Resources 5.82.26100.8117 ko-KR` (`comctl32.dll.mui`). 명부 7칸 (0번 자리 채움 포함).
  - DLL 구역: 원소 12 = DLL 6 × (이름 키 + `C:\WINDOWS\system32\<이름>` 키). 이름 키 값은 플래그 2, 전체 경로 키 값은 플래그 **`0xa`** (2 + 8, 조각 0). 구역 Flags 1 (해시표), 칸 3개.
  - 그래서 `comctl32.dll` (또는 `C:\WINDOWS\system32\comctl32.dll`) → `C:\WINDOWS\WinSxS\amd64_microsoft.windows.common-controls_6595b64144ccf1df_5.82.26100.8328_none_87eede957a2ccddd\comctl32.dll` (결과서 미니 해석기, 덤프 블롭 5개). `kernel32.dll` → 리다이렉트 없음.
  - **v6 항목은 시스템 기본에 없다.** v6 는 exe 매니페스트가 `Common-Controls 6.0.0.0` 을 요구해서 앱 블롭이 생길 때만 (정책으로 `6.0.26100.9278` 선택 — 고르는 코드 길은 Q164 ②).
- test.exe 에서는 (`[추측: 위 블롭이 test.exe 블롭과 같다는 가정 — 머리 0x20B 만 확인]`):
  - 리다이렉트 검색은 **탄다** — PEB `+0x308` 이 있으니까. kernel32 를 올리는 `LdrLoadDll` 길 (`LdrpPreprocessDllName +0x622`) 과, 각 모듈 import 중 API set 이 아닌 이름마다 (`LdrpLoadDependentModuleInternal +0x1426`).
  - test.exe 의 모듈 이름 (`kernel32.dll` · `kernelbase.dll` · `dll1.dll` · `dll2.dll` …) 은 12키에 없다 → 모두 `0xC0150008` → 이름 그대로 (지금 흐름도와 같은 길).
  - 그래서 저장소 맵 · 레지스트리 · 폴더 열기는 안 탄다 → PEB `+0x300` · `+0x310` 은 0 그대로 — 사진 8장 모두 (null) `[관찰]` run44 (`04_baggage-read_P0.txt:1639 · 1641`, `_P2.txt:2516 · 2518`) 과 맞다.
  - 사진의 `25996d00000` 머리는 P0 · P1 · P1a 에서 `????` (이 프로세스에 페이지가 아직 안 붙음), P1b · P2 에서 보인다 `[관찰]` (`_P1a.txt:3351`, `_P1b.txt:4469`). 처음 읽는 때가 `P1a~P1b` (kernel32 를 올리는 첫 리다이렉트 검색) 라는 것과 맞는다 `[추측: 공유 섹션 페이지는 처음 건드릴 때 붙음]`.

### Q90 닫힘 (문맥 쪽)

- 대기열 Q90: "ntdll 이 PEB `pShimData` · `SystemDefaultActivationContextData` 를 **읽는** 곳".
- 답 `[정적]` (결과서 — 내보낸 ntdll `.asm` 662개 안에서 찾은 범위, ntdll 전체 전수는 아님):
  - **`PEB+0x308` 읽는 곳**: `RtlFindActivationContextSectionString +0x134` (셋 다 없나 보는 맨 앞 검사) · `RtlpFindNextActivationContextSection +0x131` · `+0x192` (다음 문맥 고르기) · `RtlpGetActivationContextDataStorageMapAndRosterHeader +0x215 ~ +0x21c` (저장소 맵, 플래그 비트 1) · `RtlpFindActivationContextSection_FillOutReturnedData +0x1d7` (`MOV ECX,0x308`).
  - **`PEB+0x2f8` 읽는 곳**: 같은 넷의 짝 줄 — `RtlFindActivationContextSectionString +0x30` · `RtlpFindNextActivationContextSection +0x119` · `RtlpGetActivationContextDataStorageMapAndRosterHeader +0xc7 ~ +0xce` · `FillOutReturnedData +0xc7` (`MOV ECX,0x2f8`, flow-writer) — 그리고 `LdrpFindDllActivationContext +0x16c` (exe 자기 매니페스트를 다시 안 찾으려는 검사, run67).
  - 읽어서 하는 일: DLL 이름 리다이렉트 (위 전부). 문맥 쪽은 여기서 닫힘.
- **`pShimData` (`+0x2d8`) 읽는 쪽은 이번 범위가 아니다** — 후보 이름만 `[공백]`: `LdrpInitializeProcess +0x69f`, `LdrpCheckComponentOnDemandEtwEvent +0x6c`, `RtlpHpOptIntoSegmentHeap +0x184`, `SbSelectProcedure +0x5f` · `+0x8f` · `+0x140` · `+0x199`, `SbObtainTraceHandle +0x22` (줄에서 `GS:[0x60]` 로 PEB 를 읽는 것만 확인, 용도는 안 읽음). **Q90 은 이쪽만 남긴다.**

## 환경

- 26100.9457, 정적만 (VM 실행 없음). 결과서가 사진 덤프 (`ms/dump/`) 를 서버에서 오프라인으로 읽었다 (아래 "사진 덤프 대조").
- 정적 (다시 analyze 안 함, 전부 `-readOnly`):
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` — 22개 depth 0 새로 뽑음 + 있던 `.asm` 재사용, 새 지도 `ms/disassemble/ntdll/_map/RtlDosApplyFileIsolationRedirection_Ustr_{edges,nodes}.tsv` · `_summary.txt` (깊이 6, 106함수).
  - sxs pdb `76674E9722F9EFB085A6BE05B23E9B371` (run87 에서 analyze) — 읽기 전용 한 번: DLL 리다이렉트 구역을 **쓰는** 쪽 4개 + depth 1 = 16개 (`SxspDllRedirectionContributorCallback` · `SxspDllRedirectionStringSectionGenerationCallback` · `CDllRedir::ContributorCallback` 등).
- 버전 대조 (결과서): `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal` (`_calls.tsv`), run23 `LdrpLoadDependentModuleInternal +0x1426/+0x142b → RtlDosApplyFileIsolationRedirection_Ustr` 그대로. 사진 블롭 안 조립 폴더 이름 (`…6.0.26100.9278_none_3e0d1ba8e3303201` · `…5.82.26100.8328_none_87eede957a2ccddd`) 이 이 빌드의 `ms/bin/…/WinSxS/` 사본에 있음 (flow-writer 도 `ls` 로 봄 — 그 밖에 `.1591` 폴더도 있어 버전 고르기가 의미 있음).

## 목적

- PE_emulator 요청 29 (SxS 활성화 문맥) 의 둘째 · 마지막 조각 (Q151b) — **로더가 읽는 쪽**: 리다이렉트 길 (KnownDll 과의 차례), 어느 문맥을 보나, `ACTIVATION_CONTEXT_DATA` 최소 칸 표, comctl32 v6 최소 조건, 시스템 기본만 있을 때, 엔진이 흉내 낼 최소 블롭 · 해시. 첫 답은 지도 수준 (사용자 결정 2026-11-12).
- 짝 run87 (Q151a, 만드는 쪽) 이 "run88 에서 닫힘 / 다룸" 으로 넘긴 자리 (시스템 기본 문맥 실제 내용 · 키만이냐 전체 경로냐 · 폴더 여는 쪽 · 6.0 버전 묶기 · 블롭 덤프) 를 채운다.
- 겹치는 대기열 Q90 (ntdll 이 PEB 문맥 칸을 읽는 곳) 을 문맥 쪽만 닫는다.

## 방법

- 정적: `_workspace/sast/85_sxs-redirect.md` — ntdll `^LdrpLoadDependentModuleInternal$` (있는 `.asm`, `+0x1426` 앞뒤와 KnownDll 갈림), `^(RtlDosApplyFileIsolationRedirection_Ustr|RtlFindActivationContextSectionString|RtlpFindNextActivationContextSection)$` depth 1, 구역 안쪽 (`RtlpFindUnicodeStringInSection` 등), `RtlGetActiveActivationContext`. 지도 `run_ghidra.sh map ntdll.dll RtlDosApplyFileIsolationRedirection_Ustr`.
- 결과서: `_workspace/sast/85_sxs-redirect_result.md` (**분석성공 — 지도 수준 + 데이터 모양 검증**).
  - 결과서가 요청서 범위를 넓힌 곳 둘: ① sxs 의 DLL 구역 **쓰는** 쪽 (윈도우가 키만 쓰나 전체 경로를 쓰나), ② **사진 덤프 오프라인 읽기** — VMware 메모리 사진에서 `Actx` 머리를 찾아 실제 블롭으로 표를 검증.
  - 임시 도구 · 최소 블롭: `ms/disassemble/ntdll/_sast85/` (커밋 안 됨) — `scan_actx.py` (머리 찾기) · `dumpblob.py` (풀어 보기) · `miniloader.py` (로더 읽는 코드를 파이썬으로 옮긴 미니 해석기 — 진짜 ntdll 아님) · `buildmin.py` → `min_A.bin` · `min_B.bin`.
- 오케스트레이터가 `.asm` 으로 다시 맞춘 줄: `LdrpLoadDependentModuleInternal +0x1426 RtlDosApplyFileIsolationRedirection_Ustr` → `+0x178d LdrpLoadKnownDll` (리다이렉트가 먼저), `LdrpLoadKnownDll +0x23 BT 9` · `+0x32 LdrpCheckKnownDllFullPath`, `RtlpFindNextActivationContextSection +0x105` TEB `+0x2c8` · `+0x119` PEB `+0x2f8` · `+0x131` PEB `+0x308`, 해시 65599 곱 `COMCTL32.DLL` = `0xf1c4bc4f` (파이썬으로 다시 셈).
- flow-writer 도 다시 봤다:
  - `.asm` 줄: `LdrpLoadDependentModuleInternal +0x264` · `+0x62f ~ +0x664` · `+0x94a` · `+0x965` · `+0x10d6` · `+0x10ec` · `+0x13e7` · `+0x1426 ~ +0x148e` · `+0x14f2 ~ +0x1506` · `+0x15aa ~ +0x15bc` · `+0x178d` · `+0x1827` · `+0x1ca9`, `LdrpLoadKnownDll +0x0 ~ +0x63` · `+0x113`, `LdrpCheckKnownDllFullPath +0x0 ~ +0x94`, `RtlDosApplyFileIsolationRedirection_Ustr` call 줄 · `+0x443` · `+0x455`, `sxsisol_SearchActCtxForDllName` call 줄 · `+0x33` · `+0x6a` · `+0xd8 ~ +0xdf` · `+0x163 ~ +0x1d7` · `+0x337` · `+0x33f` · `+0x3d9` · `+0x5c8`, `RtlFindActivationContextSectionString` 줄 열 개, `RtlpFindNextActivationContextSection` 다섯, `RtlpFindUnicodeStringInSection` 열여덟, `RtlpLocateActivationContextSection` 열둘, `RtlGetAssemblyStorageRoot` 셋, **`RtlpGetActivationContextDataStorageMapAndRosterHeader +0x0 ~ +0x2fd`** (PEB `+0x300` · `+0x310` 쓰는 줄), `RtlpResolveAssemblyStorageMapEntry` call 줄, `RtlpAssemblyStorageMapResolutionDefaultCallback` · `RtlpProbeAssemblyStorageRootForAssembly` call 줄, `RtlpFindActivationContextSection_FillOutReturnedData +0xc7` · `+0x1d7`, `LdrpFindDllActivationContext +0x16c`, **`LdrpAllocateModuleEntry +0x0 ~ +0x198`** · **`RtlGetActiveActivationContext` 전부** (모듈 항목 `+0x88` 쓰는 줄), `LdrpLoadDll +0xbd` · `+0xfb`, `LdrpPreprocessDllName +0x622`, `LdrpFindOrPrepareLoadingModule +0x5e0`, `LdrpProcessWork +0x5b` · `+0x13f`.
  - 이름: `pdb_aliases.py --lookup ntdll.dll 0x166500` (복사 함수 — 같은 주소 이름 2개).
  - 최소 블롭: `min_A.bin` · `min_B.bin` 을 작은 파이썬 파서 (scratchpad, 레포 밖) 로 칸마다 풀어 결과서 표와 대조 (B 전부 같음, A 는 한 칸 다름 — 아래). 해시 · 경로 길이 (124글자 = `0xf8`B) 다시 셈.
  - 파일: `ms/bin/…/WinSxS/` 의 common-controls 폴더 목록 · comctl32 사본 셋의 SHA-256 앞 8자리.
  - 사진 값 (run44 덤프 읽기 로그): PEB `+0x300` · `+0x310` 8장, `PEB+0x308` 이 가리키는 `dq` 4칸 8장 (아래 "사진 덤프 대조"). 새 동적 실행은 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpLoadDependentModuleInternal` | **`+0x1426`** | **`+0x142b`** | **`RtlDosApplyFileIsolationRedirection_Ustr`** (rva `0x3eb80`) | 인자 `ecx=1`, `rdx`=이름, `r8`=`".DLL"`, `[rsp+0x20]`=결과 문자열. 건너뜀: 보안 프로세스 (`+0x13e7 CMP [LdrpIsSecureProcess],0`), API set 으로 바뀐 이름이고 `.local` 꺼짐 (`+0x62f TEST [RAX+8],0x1000` · `+0x638 XOR R15B,R15B` → `+0x661 TEST R15B` · `+0x664 JNZ`; R15B 처음 1 `+0x264`) | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0x148e` | `+0x1493` | `RtlGetFullPathName_Ustr` | 리다이렉트 성공 뒤 경로 다듬기. 실패 상태가 `0xc0150008` 면 오류로 안 적음 (`+0x14f2 CMP R14D,0xc0150008` · `+0x14f9 JZ`), 다른 값은 `+0x1502 MOV [RSP+0x78],EBX` | [정적] (flow-writer) |
| 〃 | `+0x15aa` | `+0x15af` | `LdrpLogInternal` ("… resolved by SxS" / "API set") | 바로 뒤 **`+0x15af BTS ESI,9`** (전체 경로) · `+0x15bc OR ESI,4` (SxS 로 얻음) | [정적] (flow-writer) |
| 〃 | **`+0x178d`** | **`+0x1792`** | `LdrpLoadKnownDll` | 리다이렉트 **뒤** | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0x1ca9` | `+0x1cae` | `LdrpProcessWork` | KnownDll 아님 + 경로 직접 (flags `0x400`) | [정적] (run23) |
| 〃 | `+0x1827` | `+0x182c` | `LdrpQueueWork` | 그 밖 (일감) | [정적] (run23) |
| `ntdll!LdrpLoadKnownDll` | **`+0x32`** | **`+0x37`** | `LdrpCheckKnownDllFullPath` (rva `0x9ac0`) | 문맥 `+0x20` 비트 9 켜졌을 때만 (`+0x9 MOV EAX,[RCX+0x20]` · `+0x23 BT EAX,9` · `+0x27 JNC`). 경로의 폴더 길이 · 글자를 `LdrpKnownDllPath` 와 비교, 다르면 `AL=0` → `+0x3e JZ` → `+0x113 MOV EAX,0xc0000135` | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0x5e` | `+0x63` | `LdrpFindKnownDll` | 비트 9 꺼짐 (이름만) 또는 폴더가 System32 | [정적] (run65) |
| `ntdll!LdrpProcessWork` | `+0x13f` | `+0x144` | `LdrpMapDllFullPath` | `+0x5b BT EAX,9` 켜짐 (전체 경로) | [정적] (flow-writer) |
| `ntdll!LdrpLoadDll` | `+0xbd` | `+0xc2` | `LdrpPreprocessDllName` | LoadLibrary 길 이름 다듬기 | [정적] |
| `ntdll!LdrpPreprocessDllName` | `+0x622` | `+0x627` | `RtlDosApplyFileIsolationRedirection_Ustr` | import 길과 같은 모양 (1, 이름, `".DLL"`, …) | [정적] (flow-writer) |
| `ntdll!LdrpFindOrPrepareLoadingModule` | `+0x5e0` | `+0x5e5` | `LdrpLoadKnownDll` | LoadLibrary 길도 리다이렉트 (`LdrpLoadDll +0xbd`) 가 먼저 | [정적] (flow-writer) |
| `ntdll!RtlDosApplyFileIsolationRedirection_Ustr` | `+0x1dd` | `+0x1e2` | `RtlFindCharInUnicodeString` | `.` 찾기. 없으면 `+0x273/+0x299` 에서 `.DLL` 붙임 | [정적] |
| 〃 | `+0x361` | `+0x366` | `RtlGetFullPathName_UstrEx` | 이름이 드라이브 · 슬래시 모양일 때만 | [정적] |
| 〃 | `+0x6cc` | `+0x6d1` | `sxsisol_RespectDotLocal` | `+0x443 TEST [ProcessParameters+8],0x1000` (`.local`) 켜졌을 때만 | [정적] (flow-writer) |
| 〃 | **`+0x484`** | **`+0x489`** | **`sxsisol_SearchActCtxForDllName`** (rva `0x40f80`) | `.local` 이 못 찾았을 때 (`+0x455 TEST R15B,1`) | [정적] (flow-writer) |
| 〃 | `+0x603` | `+0x608` | `sxsisol_FreeUnicodeStringBufferAroundUnicodeStrings_Success` | 결과를 호출자 문자열에 확정 | [정적] |
| `ntdll!sxsisol_SearchActCtxForDllName` | **`+0xc9`** | **`+0xce`** | **`RtlFindActivationContextSectionString`** (rva `0x41680`) | 인자 (3, 0, **2** — `+0x6a MOV R8D,2`, 이름, 결과 칸 `cbSize 0x70` — `+0x33`). 돌아와 `+0xd8 CMP EAX,0xc0150001` → `+0xdf LEA EBX,[RAX+7]` = `0xc0150008` | [정적] (flow-writer) |
| 〃 | `+0x215` | `+0x21a` | `RtlGetAssemblyStorageRoot` (rva `0xb6148`) | 값 `Flags` 비트 1 (`+0x1cb TEST AL,2`), 비트 2 와 같이면 `+0x1d7` → 거부 | [정적] (flow-writer) |
| 〃 | `+0x3f9` | `+0x3fe` | `sxsisol_ExpandEnvironmentStrings_UEx` | 비트 2 (`+0x3d9`) | [정적] |
| 〃 | `+0x2f0` · `+0x3b5` · `+0x506` | `+0x2f5` · `+0x3ba` · `+0x50b` | `memcpy` (같은 주소 이름 2개 `memcpy` · `memmove`) | 루트 · 조각 · 이름 복사 | [정적] (flow-writer) |
| `ntdll!RtlFindActivationContextSectionString` | `+0xed` | `+0xf2` | `RtlpFindNextActivationContextSection` (rva `0x41f60`) | 맨 앞 `+0x30` (PEB `+0x2f8`) · `+0x134` (PEB `+0x308`) · `+0x141 ~ +0x148` (TEB `+0x2c8` → `[RAX]`, 널 검사 없음) 셋 다 비면 `+0x151 MOV EAX,0xc0150001` 로 끝 | [정적] (flow-writer) |
| 〃 | `+0x20e` | `+0x213` | `RtlpFindUnicodeStringInSection` (rva `0x419e0`) | 그 전 `+0x1e3 CMP [R15],0x64487353` (구역 Magic) | [정적] (flow-writer) |
| 〃 | `+0x255` | `+0x25a` | `RtlpFindNextActivationContextSection` | 못 찾으면 (`0xC0150001`) 다음 문맥 | [정적] |
| 〃 | `+0x2e0` | `+0x2e5` | `RtlpFindActivationContextSection_FillOutReturnedData` (rva `0x6fe88`) | 결과 칸 채우기 (`ulFlags` = 반복자 `& 3`) | [정적] |
| `ntdll!RtlpFindNextActivationContextSection` | `+0x95` | `+0x9a` | `RtlpLocateActivationContextSection` (rva `0x42120`) | 고른 문맥의 목차에서 id 2 찾기. 고르기: `+0x105` TEB `+0x2c8` · `+0x119` PEB `+0x2f8` · `+0x131` PEB `+0x308` · `+0x192` (스레드 틀 값이 특수 표식 −4 일 때 PEB `+0x308`) | [정적] (오케스트레이터 · flow-writer) |
| `ntdll!RtlpLocateActivationContextSection` | `+0x140` | `+0x145` | `bsearch` | 목차 Flags 비트 1 | [정적] |
| `ntdll!RtlpFindUnicodeStringInSection` | `+0x188` | `+0x18d` | `bsearch` | 구역 Flags 비트 1 (`+0x135`) | [정적] (flow-writer) |
| `ntdll!RtlGetAssemblyStorageRoot` | `+0x8a` | `+0x8f` | `RtlpGetActivationContextDataStorageMapAndRosterHeader` | 저장소 맵이 비었으면 만듦 (아래) | [정적] (flow-writer) |
| 〃 | `+0xd7` | `+0xdc` | `RtlpResolveAssemblyStorageMapEntry` (rva `0xb6848`) | 명부 번호 `< EntryCount` (`+0xba`) | [정적] (flow-writer) |
| `ntdll!RtlpGetActivationContextDataStorageMapAndRosterHeader` | `+0x266` | `+0x26b` | `RtlAllocateHeap` (`PEB.ProcessHeap` `+0x262`, 0, `EntryCount × 8 + 0x10` `+0x258`) | 맵 칸이 0 일 때 | [정적] (flow-writer) |
| 〃 | `+0x281` | `+0x286` | `RtlpInitializeAssemblyStorageMap` | 0 채움 → **`+0x292 CMPXCHG.LOCK [R14],R13`** — R14 = PEB `+0x300` (`+0xd1`, 플래그 비트 1 꺼짐) / PEB `+0x310` (`+0x21f`, 비트 1 켜짐 `+0xb5`). 졌으면 `+0x29c` 되돌리기 · `+0x2b3 RtlFreeHeap` | [정적] (flow-writer) |
| `ntdll!RtlpResolveAssemblyStorageMapEntry` | `+0x259` · `+0x2ee` · `+0x355` | `+0x25e` · `+0x2f3` · `+0x35a` | `RtlpAssemblyStorageMapResolutionDefaultCallback` | 루트 후보 묻기 | [정적] (flow-writer) |
| 〃 | `+0x3ad` · `+0x543` | `+0x3b2` · `+0x548` | `RtlpProbeAssemblyStorageRootForAssembly` | 후보 루트 + 폴더 열어 보기 | [정적] (flow-writer) |
| 〃 | `+0x229` · `+0x560` | `+0x22e` · `+0x565` | `RtlpInsertAssemblyStorageMapEntry` | 맵에 항목 (루트 문자열 · 폴더 핸들) | [정적] (flow-writer) |
| `ntdll!RtlpAssemblyStorageMapResolutionDefaultCallback` | `+0x169` | `+0x16e` | `NtOpenKey` (`\Registry\Machine\Software\Microsoft\Windows\CurrentVersion\SideBySide\AssemblyStorageRoots`, 접근 8) | 없어도 됨 (`0xC0000034` · `0xC0000189`) | [정적] |
| 〃 | `+0xb9` | `+0xbe` | `NtEnumerateKey` | 하위 키 후보 돌기 | [정적] |
| 〃 | `+0x1df` | `+0x1e4` | `RtlGetNtSystemRoot` | `C:\WINDOWS` → `+0x240` `\WinSxS\` 복사 | [정적] |
| `ntdll!RtlpProbeAssemblyStorageRootForAssembly` | **`+0x26c`** | **`+0x271`** | `NtOpenFile` | 조립 폴더 열기 (접근 `0x100020`, 공유 3, 옵션 `0x21`). 앞 `+0x11a` · `+0x141` 폴더 이름 붙이기 | [정적] |
| `ntdll!LdrpAllocateModuleEntry` | `+0x166` | `+0x16b` | `RtlGetActiveActivationContext` (rva `0x6ef00`) | 문맥 `+0x30` (부모) 이 0 일 때 (`+0x9e TEST RSI` · `+0xa1 JZ +0x15f`), `RCX = 항목 + 0x88` (`+0x15f`). 부르는 곳은 이 하나 | [정적] (flow-writer) |

### 호출 관계

```
[의존 DLL 하나 — import]  LdrpLoadDependentModuleInternal
 ├─ (API set 이름이면) +0x2ff ApiSetpResolveHost    — 이때 .local 꺼짐이면 아래 리다이렉트 건너뜀
 ├─ +0x142b RtlDosApplyFileIsolationRedirection_Ustr           ← 보안 프로세스면 건너뜀 (+0x13e7)
 │     ├─ +0x1e2  RtlFindCharInUnicodeString     ('.' 없으면 ".DLL")
 │     ├─ +0x366  RtlGetFullPathName_UstrEx      (경로 모양일 때만)
 │     ├─ +0x6d1  sxsisol_RespectDotLocal        (.local 있을 때만, 찾으면 문맥은 안 봄)
 │     ├─ +0x489  sxsisol_SearchActCtxForDllName
 │     │     ├─ +0xce  RtlFindActivationContextSectionString(3, 0, 2, 이름, 결과)
 │     │     │     ├─ (맨 앞) PEB+0x2f8 · PEB+0x308 · TEB+0x2c8→ActiveFrame 셋 다 비면 0xc0150001
 │     │     │     ├─ +0xf2   RtlpFindNextActivationContextSection   ① 스레드 → ② PEB+0x2f8 → ③ PEB+0x308
 │     │     │     │     └─ +0x9a  RtlpLocateActivationContextSection   목차에서 id 2
 │     │     │     ├─ +0x213  RtlpFindUnicodeStringInSection       해시 (0x1003f 곱) · 이분 탐색 · 해시표
 │     │     │     ├─ +0x25a  RtlpFindNextActivationContextSection (못 찾으면 다음 문맥)
 │     │     │     └─ +0x2e5  RtlpFindActivationContextSection_FillOutReturnedData
 │     │     │   0xc0150001 → 0xc0150008 (+0xdf)  = "리다이렉트 없음"
 │     │     ├─ +0x21a  RtlGetAssemblyStorageRoot                  (값 Flags 비트 1 — A 방식만)
 │     │     │     ├─ +0x8f  RtlpGetActivationContextDataStorageMapAndRosterHeader
 │     │     │     │          (처음만) +0x26b RtlAllocateHeap → +0x286 RtlpInitializeAssemblyStorageMap
 │     │     │     │          → +0x292 CMPXCHG.LOCK  PEB+0x300 / +0x310
 │     │     │     └─ +0xdc  RtlpResolveAssemblyStorageMapEntry
 │     │     │              ├─ +0x25e … RtlpAssemblyStorageMapResolutionDefaultCallback
 │     │     │              │        ├─ +0x16e NtOpenKey (AssemblyStorageRoots)   ├─ +0xbe NtEnumerateKey
 │     │     │              │        └─ +0x1e4 RtlGetNtSystemRoot → "\WinSxS\"
 │     │     │              ├─ +0x3b2 … RtlpProbeAssemblyStorageRootForAssembly → +0x271 NtOpenFile (폴더)
 │     │     │              └─ +0x22e … RtlpInsertAssemblyStorageMapEntry
 │     │     ├─ +0x2f5 / +0x3ba / +0x50b  memcpy   루트 · 조각 · 물어본 이름
 │     │     └─ +0x3fe  sxsisol_ExpandEnvironmentStrings_UEx       (값 Flags 비트 2)
 │     └─ +0x608  sxsisol_FreeUnicodeStringBufferAroundUnicodeStrings_Success
 │   돌아와 +0x142b MOV R14D,EAX → 성공이면 +0x1493 RtlGetFullPathName_Ustr
 │        → +0x15af LdrpLogInternal 뒤 BTS ESI,9 · OR ESI,4
 ├─ +0x16ee LdrpFindLoadedDllByName   (이미 있나)
 ├─ +0x115c LdrpAllocateModuleEntry   (새 항목. 부모 없을 때만 +0x16b RtlGetActiveActivationContext → 항목 +0x88)
 ├─ +0x1792 LdrpLoadKnownDll
 │     ├─ (비트 9) +0x37 LdrpCheckKnownDllFullPath   폴더 ≠ LdrpKnownDllPath → 0xc0000135
 │     └─ (이름만) +0x63 LdrpFindKnownDll → NtOpenSection(\KnownDlls\<이름>)
 └─ (0xc0000135) +0x1cae LdrpProcessWork → (비트 9) +0x144 LdrpMapDllFullPath
                                         → LdrpMapDllNtFileName  NtOpenFile · NtCreateSection
     또는 +0x182c LdrpQueueWork

[LoadLibrary]  LdrpLoadDll
 ├─ +0xc2  LdrpPreprocessDllName → +0x627 RtlDosApplyFileIsolationRedirection_Ustr   (같은 모양)
 └─ +0x100 LdrpLoadDllInternal → … LdrpFindOrPrepareLoadingModule → +0x5e5 LdrpLoadKnownDll
```

### 사진 덤프 대조 (오프라인)

- 결과서가 사진 둘을 서버에서 직접 읽었다 (WinDbg 아님, 파이썬 — 물리 메모리 파일에서 `41 63 74 78 20 00 00 00 01 00 00 00` 찾기):
  - **run44 시험 사진 dast/02** — `ms/dump/02_snapshot-test/BOB Windows 11 24H2-Snapshot5.vmem` (8GB). test.exe 를 `LdrInitializeThunk` 입구에 세운 순간이지만, **짐 사진 8장과 다른 test.exe 실행이다** (Cid `0a58`, PEB `f392a35000` — `ms/logs/02_snapshot-test_result.md`). 후보 약 200개: DLL 안 임베디드 매니페스트류 110개, **comctl32 v6 를 쓰는 앱 블롭** 약 50개 (다른 GUI 프로세스 것), **시스템 기본 블롭 (AMD64, `0x3084`B, 명부 7칸)** 몇 개.
  - **run44 짐 사진 P0** — `ms/dump/03_baggage-snap/P0/memory.dmp`. 시스템 기본 블롭 사본 3개, 같은 결과.
- 한계 (결과서): **물리 연속 가정** — 한 블롭이 여러 쪽이면 사본에 따라 뒤가 안 이어진다 (`0x1e4892000` · `0x1e260c000` 은 끝까지 맞고 `0xf1f3d000` 은 2쪽째부터 다름). 가상 ↔ 물리 변환은 안 했다 → **test.exe `25996d00000` 이 그 블롭인지는 `[확인 필요]`**.
- flow-writer 가 보탠 것 — run44 덤프 읽기 로그의 test.exe `25996d00000` `dq` 4칸 (`[관찰]`):

| 사진 | 줄 | 값 |
|---|---|---|
| K1 · K2 · K3 | `04_baggage-read_K1.txt:2952` · `_K2.txt:2952` · `_K3.txt:2957` | PEB `+0x308` = 0 (아직 없음) |
| P0 · P1 · P1a | `_P0.txt:2959 ~ 2961` · `_P1.txt:2958 ~ 2960` · `_P1a.txt:3350 ~ 3352` | 주소 `25996d00000` 은 있으나 내용 `????` |
| P1b · P2 | `_P1b.txt:4469 ~ 4470`, `_P2.txt:4481 ~ 4482` | `00000020'78746341 00003084'00000001` · `00000000'000000dc 00000000'00000020` = Magic · HeaderSize `0x20` · FormatVersion 1 · **TotalSize `0x3084`** · **DefaultTocOffset `0xdc`** · ExtendedToc 0 · **AssemblyRosterOffset `0x20`** · Flags 0 |

  - → 결과서의 시스템 기본 블롭 머리 (`0x3084` · `0xdc` · `0x20`) 와 **같다**. 머리만 본 것이라 몸통 (`0x3064`B) 은 그대로 `[확인 필요]` (Q104 3).
  - PEB `+0x300` `ProcessAssemblyStorageMap` · `+0x310` `SystemAssemblyStorageMap` 은 8장 모두 (null) (`_K1.txt:1631 · 1633` … `_P2.txt:2516 · 2518`) `[관찰]`.
- 미니 해석기 (결과서): `RtlpLocateActivationContextSection` · `RtlpFindUnicodeStringInSection` · `sxsisol_SearchActCtxForDllName` 의 줄을 옮긴 파이썬. 실제 블롭 5개에 물려 코드로 예상한 값과 모두 맞았다 — 앱 블롭 `0xd4c17000`: `COMCTL32.dll` → `…6.0.26100.9278…\COMCTL32.dll`, `comctl32.dll.mui` → ko-KR 폴더, `user32.dll` → 없음. 시스템 기본 셋 + P0 셋: `comctl32.dll` · `C:\WINDOWS\system32\comctl32.dll` → `…5.82.26100.8328…\comctl32.dll`, `gdiplus.dll` → GdiPlus 1.1.26100.9444 폴더, `kernel32.dll` → 없음. **진짜 ntdll 이 같은 값을 낸다는 증명은 아니다** (코드를 읽고 다시 쓴 것).

## 확인된 것

- `[정적]` 의존 DLL 이름 리다이렉트는 `LdrpLoadDependentModuleInternal +0x1426/+0x142b RtlDosApplyFileIsolationRedirection_Ustr` 이고, `+0x178d/+0x1792 LdrpLoadKnownDll` 보다 **먼저** 돈다. LoadLibrary 길도 `LdrpLoadDll +0xbd` (`LdrpPreprocessDllName +0x622`) 가 `+0xfb` (→ `LdrpFindOrPrepareLoadingModule +0x5e0 LdrpLoadKnownDll`) 보다 먼저다.
- `[정적]` 리다이렉트가 경로를 주면 `+0x15af BTS ESI,9`, `LdrpLoadKnownDll +0x23 BT EAX,9` 가 켜진 비트를 보고 `+0x32 LdrpCheckKnownDllFullPath` 로 폴더를 `LdrpKnownDllPath` 와 비교, 다르면 `+0x113` 에서 `0xc0000135`. 같은 비트를 `LdrpProcessWork +0x5b` 가 보고 `+0x13f LdrpMapDllFullPath` 로 간다. (ESI 가 새 문맥 `+0x20` 으로 가는 길 — `+0x94a MOV [RSP+0x98],ESI` · `+0x10ec MOV [RAX+0x20],R15D` 사이 — 은 끝까지 안 따라감 `[추측]`, 결과서.)
- `[정적]` 찾는 차례는 TEB `+0x2c8` 맨 위 틀 → PEB `+0x2f8` → PEB `+0x308` (`RtlpFindNextActivationContextSection +0x105` · `+0x119` · `+0x131`). 셋 다 비면 `RtlFindActivationContextSectionString +0x151` `0xc0150001` → `sxsisol +0xdf` `0xc0150008`.
- `[정적]` TEB `+0x2c8` 은 널 검사 없이 따라간다 (`RtlFindActivationContextSectionString +0x141 MOV RAX,[RCX+0x2c8]` · `+0x148 CMP [RAX],R15`).
- `[정적]` 블롭 안 위치 칸은 전부 32비트 오프셋이다 (블롭 · 구역 기준). 읽는 길에 8바이트 절대 주소가 없다 (결과서 표 3, 미니 해석기).
- `[정적]` PEB `+0x300` · `+0x310` 을 쓰는 줄은 ntdll `RtlpGetActivationContextDataStorageMapAndRosterHeader +0x292 CMPXCHG.LOCK` 하나다 (R14 = `+0xd1 LEA R14,[RDX+0x300]` / `+0x21f LEA R14,[RDX+0x310]`). 값은 `+0x266 RtlAllocateHeap(PEB.ProcessHeap, 0, EntryCount × 8 + 0x10)`. `RtlGetAssemblyStorageRoot +0x8a` 를 처음 부를 때만 — 리다이렉트 값 플래그 비트 1 일 때만 부른다 (`sxsisol +0x1cb` · `+0x215`). 사진 8장 (null) `[관찰]` = test.exe 는 안 탐.
- `[정적]` 모듈 항목 `+0x88` (`EntryPointActivationContext`) 을 쓰는 줄: `LdrpAllocateModuleEntry +0xbb MOV [RBX+0x88],RSI` (부모 `+0x88` ≠ 0 일 때 그 값, `+0xa7 ~ +0xb1`), 부모가 없으면 `+0x15f LEA RCX,[RBX+0x88]` → `+0x166 RtlGetActiveActivationContext` 가 `+0x49` 에서 0 을 쓰고 스레드 맨 위 틀이 있으면 (`+0x61 ~ +0x67`) `+0x86` 에서 틀의 문맥을 쓴다 (AddRef `+0x7d`). 사진 `LoadReason` exe 4 · k32 4 (`+0x16b MOV [RBX+0x10c],4` 와 같은 길) `[관찰]` run44 → exe · kernel32 는 이 길로 0 을 받았다 `[추측: TEB 활성화 스택 ActiveFrame 0 — 짐 지도 3.3]`. 사진 8장 모두 0 (짐 지도 7.2 끝까지 0) 과 맞다.
- `[관찰]` test.exe `PEB+0x308` 블롭 머리 `0x20`B 가 사진 덤프의 시스템 기본 블롭 머리와 같다 (위 표).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **태그**: 결과서의 `[덤프 오프라인]` 은 CLAUDE.md 태그 목록에 없다 → 이 문서는 `[관찰]` 로 쓰고 처음 한 번 "(사진 덤프 오프라인 읽기, 물리 연속 가정 — test.exe `25996d00000` 이 그 블롭인지는 `[확인 필요]`)" 를 붙였다 (오케스트레이터 지시).
- ② **사진 이름**: 결과서 "`P0/memory.dmp` (run44/45 사진)" → **run44 짐 사진 P0** (dast/03, `ms/dump/03_baggage-snap/P0/memory.dmp`) — run45 는 사진을 안 찍었다. 결과서 "`02_snapshot-test/…Snapshot5.vmem`" → **run44 시험 사진 dast/02** — 짐 사진 8장 (`25996d00000` 의 test.exe) 과 **다른 실행** (Cid `0a58`). 거기서 찾은 앱 블롭은 test.exe 것이 아니다.
- ③ (보탬) **test.exe 블롭 머리 `[관찰]`** — run44 덤프 읽기 로그 P1b · P2 의 `dq` 가 이미 머리 `0x20`B 를 담고 있었다 (위 표). 결과서 "동적 3" 의 머리 부분은 이걸로 닫힘, 몸통만 남음.
- ④ (보탬) **PEB `+0x300` · `+0x310` 쓰는 줄 `+0x292`** — 결과서는 "`LOCK` 비교교환으로 PEB 칸에 넣는다" 까지. 줄 · 두 칸 가르는 갈래 (`+0xb5 TEST CL,2` → `+0x215`) 는 flow-writer `.asm`. 짐 지도 3.1 두 칸을 닫았다.
- ⑤ (보탬) **모듈 항목 `+0x88` 쓰는 줄** — 결과서 질문 2-5 "`[모듈 항목+0x88]` 에 현재 스레드 맨 위 틀의 문맥을 복사" 를 `.asm` 으로 따라가 `+0x49` (0 쓰기) · `+0x86` · `LdrpAllocateModuleEntry +0xbb` 를 붙였다. 짐 지도 3.5 한 칸을 닫았다.
- ⑥ **`sxsisol` 길이 검사**: 결과서 "`+0x16c CMP [RSP+0x70],0x14; JC`" → CMP 는 **`+0x163`**, `+0x16c` 는 `JC`.
- ⑦ **min_A.bin 명부[1] `AssemblyInformationLength`**: 결과서 표 `0x126` → 파일은 **`0x128`** (`0x6c` 조립 정보 + `0xbc` 폴더 이름 · 패딩). 이 길에선 안 읽는 칸이라 결과는 같다.
- ⑧ **복사 함수**: `_calls.tsv` 표시 `alias:RtlCopyMemory+3` → 같은 주소 이름 **2개** `memcpy` · `memmove` (`pdb_aliases.py --lookup ntdll.dll 0x166500`). 문서엔 `memcpy` 로.
- ⑨ (보탬) **저장소 맵 푸는 길 call 자리** — 결과서는 함수 이름만. `RtlpResolveAssemblyStorageMapEntry` 가 `+0x259` · `+0x2ee` · `+0x355` (Callback) · `+0x3ad` · `+0x543` (Probe) · `+0x229` · `+0x560` (Insert) 에서 부른다.
- ⑩ (보탬) **Q90 `PEB+0x2f8` 줄** — 결과서 "위 4곳" 중 `FillOutReturnedData` 짝 줄은 `+0xc7 MOV ECX,0x2f8`.
- ⑪ (보탬) 최소 블롭 B 를 파일과 칸마다 대조 (전부 같음), 해시 · 경로 길이 다시 셈, comctl32 사본 셋 SHA-256 앞 8자리 (`67e04b3a` System32 = 5.82 `.8328` 폴더, `17de1830` 6.0 `.9278`).
- ⑫ (보탬) 사진 `????` → 보임 (P1a → P1b) — 첫 읽기가 `P1a~P1b` 라는 짐작 `[추측]`.

## 정정

- **짐 지도 7.1 셈 (예전부터 묵음)**: "0 그대로 중 모르는 칸 구조체 136" 은 run59 뒤 안 고쳐졌다. run62 (`TppWorkerpListLock`) · run66 (`ShimEngineCalloutSent`) · run70 (`MinimumStackCommit`) · run87 (`ActivationContextData`) 이 닫은 끝까지 0 4칸을 안 뺐다 → run87 뒤 **132** (2.1 유저 `끝까지 0` 모름과 같음), run88 뒤 **129**. 같은 절의 "아는 칸 중 끝까지 0 295" 도 같은 넷을 안 더했다 → run87 뒤 299, run88 뒤 **302**. 짐 지도 7.1 을 고쳐 썼다. 이전 run 문서 잘못이 아니라 짐 지도 장부 셈이라 run 문서 맨 위 정정 줄은 안 넣는다.

## 아직 확인 필요

동적 — 결과서 "동적으로 확인할 것" 번호 그대로.

- **test.exe 로 되는 것 → 대기열 Q104**:
  - **3** 시스템 기본 블롭 대조: 머리 `0x20`B 는 run44 로그로 이미 같다 (위 ③). 남은 건 몸통 — test.exe `PEB+0x308` (`25996d00000`) 을 `.writemem` 으로 `0x3084`B 떠서 사진 블롭 · 결과서 표와 대조. P0 `memory.dmp` 를 가상 → 물리 변환해 오프라인으로 읽어도 됨 (변환 도구는 아직 없음).
  - **4 중 test.exe 몫**: test.exe 의 dll 로드 (kernel32 `LdrLoadDll` · 각 import) 에서도 리다이렉트 검색은 탄다. 하지만 키가 없어 저장소 맵 · 레지스트리 · 폴더 열기 셋 (`RtlpAssemblyStorageMapResolutionDefaultCallback+0x16e` · `RtlpProbeAssemblyStorageRootForAssembly+0x271` · `RtlpGetActivationContextDataStorageMapAndRosterHeader+0x26b`) 은 **안 서야 맞다** `[추측]`. 안 서는지와, 검색 결과 (`ntdll!RtlDosApplyFileIsolationRedirection_Ustr` 입구 `rdx` 이름 · `+0x489` 의 `eax` = `0xc0150008`, `LdrpLoadDependentModuleInternal+0x142b` 의 `eax`) 를 본다. (PEB `+0x300` · `+0x310` 값은 사진으로 이미 0 `[관찰]`.)
  - **6** PEB 쓰기 순간 → Q83 (sast/84 와 같음).
- **comctl32 를 쓰는 프로세스 · 작은 exe 가 필요한 것 → 대기열 Q162** (test.exe 는 comctl32 를 안 씀 `[소스]`):
  - **1** notepad 류 (v6) 에서 `LdrpLoadDependentModuleInternal+0x142b` 직후 결과 문자열 (`[rsp+0x140]`) 과 `eax` — 정적 `…6.0.26100.9278…\comctl32.dll` 과 맞나. 멈춤점 `ntdll!RtlDosApplyFileIsolationRedirection_Ustr` (rva `0x3eb80`) 입구 · `+0x489`.
  - **2** KnownDll 거절: 같은 로드에서 `LdrpLoadKnownDll` (rva `0x9970`) 이 `0xc0000135`, `LdrpCheckKnownDllFullPath` (`0x9ac0`) `al=0`. 매니페스트 없는 작은 exe 에서 `LoadLibrary("comctl32.dll")` → **WinSxS 5.82 폴더 경로** 가 나오나 (질문 5 의 핵심).
  - **4 나머지** (A 방식 흉내용 실제 값): `+0x16e` `NtOpenKey` 상태, `+0x271` `NtOpenFile` 상태 · 경로, 처음 부를 때 PEB `+0x300` / `+0x310` 이 채워지는 순간 (`+0x26b`).
  - **5** 최소 블롭을 진짜 ntdll 에 먹이기: 작은 exe 의 `PEB+0x2f8` 에 `min_B.bin` 을 쓰고 `LoadLibrary("comctl32.dll")` → 모듈 목록 `FullDllName`. (dast 가 정함, 사용자 결정 몫.)
  - 판단 메모: 4 를 test.exe 몫 · 나머지로 나눈 건, test.exe 에선 이 셋이 안 서는 게 기대값이라 "안 서는지" 만 test.exe 로 볼 수 있어서다.

정적 — 다음 바퀴 후보:

- 결과서 후보 2 (sxs 조립 명부 · 조립 정보 칸 쓰는 줄 — 폴더 이름 `+0x54` · 플래그 `0x10` 이 정해지는 곳) = **Q164 ①**, 후보 3 (정책 6.0.0.0 → `.9278`) = **Q164 ②**. 새 번호 없음.
- **Q165** (새로):
  - ① `LdrpLoadDependentModuleInternal` 의 ESI → 새 적재 문맥 `+0x20` 길 (`+0x94a` `[RSP+0x98]` → R15D → `+0x10ec`) 끝까지 — 결과서 `[추측]`.
  - ② `LdrpLoadDependentModuleInternal +0x71 ~ +0x1d3` 의 부모 항목 문맥 틀 올리기 — 부모 `+0x88` 이 0 일 때 틀을 안 올리나 · 빈 틀을 올리나 (리다이렉트 ① "스레드 문맥" 에 무엇이 보이나). flow.md 2.6 `[추측]` "SxS 리다이렉트가 부모의 매니페스트 기준일 것".
  - ③ `_ACTIVATION_CONTEXT` 개체 칸 (`+0x18` = 블롭 말고는 `[공백]`), 스레드 틀 특수 값 `-3` · `-4` 이름 `[추측]`, 상태 값 이름 (`0xC0150001` · `0xC0150003` · `0xC0150008`, `[추측: 이름]`).
  - ④ Q90 문맥 쪽 전수: ntdll 전체 (`objdump` 꼴 검색) 에서 PEB `+0x2f8` · `+0x308` 을 읽는 줄 — 이번 목록은 내보낸 `.asm` 662개 안.
- **Q90 은 `pShimData` (`+0x2d8`) 읽는 쪽만 남김** — 위 후보 이름 (`LdrpInitializeProcess +0x69f` 등) 의 본문.
- PE_emulator 쪽 확인거리 (결과서 후보 4) 는 맨 위 "PE_emulator 쪽에서 쓸 것".

## flow.md · 짐 지도에 반영한 것

- flow.md:
  - 기준: ntdll · sxs pdb 줄에 run88 몫, 근거 로그 run88 줄 (사진 덤프 오프라인 · run44 로그 `dq` 4칸 · PEB `+0x300` · `+0x310`).
  - 그림 2: `LdrpLoadDependentModuleInternal` [dll1 추정] · [dll2 추정] 아래에 **`RtlDosApplyFileIsolationRedirection_Ustr`** 상자 둘 (`(정적)` 점선, `(2) +0x142b`) — 번호 (1) ~ (6) 으로 다시 매김. 읽는 법에 run88 문단.
  - 2.2 csrss 절 끝 줄 (→ run88), 2.5 KnownDll 본문 줄 (비트 9 · 폴더 비교), 2.6 머리 · `LdrpLoadDependentModuleInternal` 트리 · 새 절 "SxS 리다이렉트".
  - 4장: run23 "이름 · 뜻 못 정한 것" 줄 일부 해결, run87 동적 · 정적 줄 해결 표시, run88 새 줄 셋. 4.1 그림 2 `LdrpLoadDependentModuleInternal` 줄 (번호 · 새 상자).
  - 5장: run 표 · 03_sprint 요청서 번호 run88 줄.
- 짐 지도 (짐 지도 바퀴 — **모르던 칸 3 닫음**): 3.1 PEB 2줄 (`+0x300` · `+0x310`, ntdll, 조건부 안 탐) · `+0x2f8` · `+0x308` 줄에 읽는 쪽 한 마디, 3.3 TEB `ActivationContextStackPointer` 읽는 쪽 한 마디, 3.5 모듈 항목 3줄 (`EntryPointActivationContext`), 3장 줄임 `②R…`, 2장 PEB ntdll 22 → 24 · 모름 65 → 63, 모듈 항목 ntdll 38 → 39 · 모름 27 → 26, 유저 합 · 구조체 합, 2.1 유저 `끝까지 0` 모름 132 → 129, 4장 짝 1, 5장 ② 1, 6장 부품 1, 7 머리 · 7.1 (셈 정정) · 7.2 · 7.5, 8장 두 표, 기준 run88 줄 · 판 목록 (마흔넷째판).
- run87 문서: "run88 에서 닫힘 / 다룸" 으로 넘긴 자리에 "→ run88" 결과 한 줄씩.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음. 새 함정 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 디버거 여부를 보는 줄은 없다 `[정적: 읽은 줄 안에서]`. `LdrpLogInternal` (`+0x15aa`) 은 로더 로그 (켜졌을 때만 남김) 이지 디버거 검사가 아니다.
- 도구 (결과서): 사진 덤프를 서버에서 바로 읽을 수 있었다 — `vmem` · `memory.dmp` 에서 바이트 꼴 찾기 + 물리 연속 가정. **가상 ↔ 물리 변환 도구가 아직 없어서** "어느 프로세스의 블롭인가" 는 못 가린다. 후보: 하네스 밖 임시 스크립트로 PEB `+0x308` 가상 주소를 test.exe DirBase 로 변환 (오케스트레이터가 정함).
- 도구 (flow-writer): run44 덤프 읽기 로그에 **블롭 머리 `dq` 가 이미 있었다** — 다음에 비슷한 대조가 필요하면 `04_baggage-read_<P>.txt` 의 `PEB+0x…` `.catch` 구획부터 본다.

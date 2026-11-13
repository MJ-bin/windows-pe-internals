# 2026-11-13 run87 — SxS 활성화 문맥을 만드는 쪽: 부모 `CreateProcessInternalW` → csrss basesrv · sxssrv · sxs → 자식 PEB (Q151a, PE_emulator 요청 29 첫 조각): **자식 PEB 의 문맥 칸 둘 — `+0x308` `SystemDefaultActivationContextData` · `+0x2f8` `ActivationContextData` — 을 쓰는 건 부모가 아니라 csrss 다. 부모 (kernelbase `CreateProcessInternalW`) 는 kernel32 `BasepConstructSxsCreateProcessMessage` (지연 import) 로 SxS 칸 `0x1c8`B 를 채워 프로세스 등록 메시지 (`0x1001d`, `0x218`B) 에 실어 보낼 뿐이고, 보통 길에선 매니페스트 파일도 자원도 안 연다 (SxS Flags `0x40` = "csrss 가 찾아라"). csrss 는 basesrv `InternalBaseSrvCreateProcess` 에서 sxssrv `BaseSrvSxsCreateProcess` 를 불러 ① 시스템 기본 문맥 섹션 (csrss 가 구조마다 한 번 만들어 돌려씀) 을 자식에 읽기 전용으로 붙이고 `PEB+0x308` 에 주소를 쓴다 — 매니페스트 없는 exe 에도 늘. ② 매니페스트가 있을 때만 sxs.dll `SxsGenerateActivationContext` 로 앱 블롭을 만들어 붙이고 `PEB+0x2f8` 에 쓴다. 부모가 자식 PEB 에 직접 쓰는 칸은 앱 호환 `pShimData` (`+0x2d8`) 하나다 (Q87 닫힘 — `BasepInitAppCompatData` 는 kernel32 에 있고 PEB 를 안 쓴다). PE_emulator 엔진은 블롭을 미리 떠 와 읽기 전용으로 붙이고 PEB 칸에 주소만 쓰면 된다 — 매니페스트 해석은 흉내 안 내도 된다.** kernelbase `CreateProcessInternalW +0x4458 BasepConstructSxsCreateProcessMessage` (kernel32) · `+0x463b BasepCsrCaptureSxsMessage` · `+0x466b CsrClientCallServer(0x1001d)` ┄▶ csrss basesrv `BaseSrvCreateProcess2 +0x216 InternalBaseSrvCreateProcess +0xfb` (CFG 간접, `sxsFunctions+0x8`) → sxssrv `BaseSrvSxsCreateProcess +0x8d BaseSrvSxsDoSystemDefaultActivationContext` (`+0x1ab NtMapViewOfSection` · `+0x211 NtWriteVirtualMemory` → PEB `+0x308`) · `+0x4c InternalSxsCreateProcess +0x66d BaseSrvSxsCreateActivationContextFromStructEx` (`+0x8d6` 간접 = sxs `SxsGenerateActivationContext` · `+0xadb NtMapViewOfSection` · `+0xb09 NtWriteVirtualMemory` → PEB `+0x2f8`) · 부모 `+0x57d0 NtWriteVirtualMemory` → PEB `+0x2d8`

## PE_emulator 쪽에서 쓸 것

### 요청 29 첫 조각 결론 (run87)

- **자식에 실제로 닿는 것은 포인터 셋 + 메모리 덩어리 셋이다** `[정적]`.
  - `PEB+0x308` `SystemDefaultActivationContextData` ← 시스템 기본 문맥 블롭 주소. **csrss 가 쓴다. 매니페스트 없는 exe 에도 늘** (보호 프로세스류만 빼고).
  - `PEB+0x2f8` `ActivationContextData` ← 앱 문맥 블롭 주소. **csrss 가 쓴다. exe 매니페스트가 있을 때만.**
  - `PEB+0x2d8` `pShimData` ← 앱 호환 데이터 영역 주소. **부모가 쓴다** (`BasepGetAppCompatData` 가 값을 줬을 때만). SxS 와는 다른 이야기.
  - 셋 다 같은 구간 (부모 `NtCreateUserProcess` 뒤 ~ `NtResumeThread` 앞, 사진 `K3~P0`) 에 들어간다. 자식 첫 스레드는 그때 멈춰 있다.
- **엔진이 할 일은 "붙이고 주소 쓰기" 뿐이다** `[정적 판단 — 결과서 질문 5]`.
  - 아래 "누가 무엇을 자식에 넣나" 표의 3 ~ 5 · 10 ~ 12 를 nt 엔진이 대신하면 된다.
  - 6 ~ 9 (매니페스트 찾기 · 해석 · 캐시) 는 csrss 안에서만 오간다. 블롭을 미리 떠 오면 필요 없다.
  - 붙이는 모양: **읽기 전용 공유 보기** (`NtMapViewOfSection` 공유 1 · `0x400000` · 보호 2). test.exe 사진 VAD 가 `Mapped READONLY Pagefile section` 4쪽, 주소 `25996d00000` (64KB 정렬) 이다 `[관찰]` run44 (`ms/logs/04_baggage-read_P0.txt:1380`).
- **지금 PE_emulator 는 빠져 있다 (틀림)**: test.exe 같은 매니페스트 없는 exe 도 진짜 윈도우에선 `PEB+0x308` 이 채워져 있다 (`[관찰]` run44 P0 `25996d00000`).
- **부모 쪽 흉내는 필요 없다.** 부모 · csrss 가 없으니 메시지를 만들 일이 없다.
- **확인거리 (결과서 다음 요청 후보 4 — PE_emulator 쪽, 우리 대기열 아님)**: PE_emulator 의 `NtCreateUserProcess` 흉내가
  - 만들기 정보 (`PS_CREATE_INFO`) 출력에 exe 안 매니페스트 주소 · 크기를 주는지,
  - `ProcessParameters.Flags` 비트 13 (`0x2000`, 커널 `PspLocateInPEManifest +0x99` 가 켬 — 짐 지도 3.2) 을 켜는지.
  - 진짜에선 이 둘이 부모 → csrss 로 넘어가 "앱 문맥을 만들까" 를 정한다 (`InternalSxsCreateProcess +0x405 TEST [R15],0x2000`). 엔진이 앱 블롭을 넣을지 고를 때 같은 기준을 쓰면 된다 `[추측: 엔진 쪽]`.
- 블롭 **안쪽 모양** (머리 · 목차 · 구역), 어느 주소에 붙여도 읽히는지, 로더가 세 문맥을 어떤 차례로 보는지 → **run88 (Q151b, sast/85)**.

### 원문 질문이 어디서 답해졌나

| 원문 요청 29 | 답한 곳 |
|---|---|
| 1 프로세스 기본 활성화 문맥을 누가 · 언제 만드나 (부모 → csrss → 자식 PEB, 섹션 매핑 · 주소, 매니페스트 있을 때 · 없을 때) | **run87** (sast/84 질문 1 · 2 · 3) |
| 2 로더가 DLL 이름을 리다이렉트하는 길 · 읽는 데이터 모양 (`ACTIVATION_CONTEXT_DATA` 머리 · 문자열 구역 최소 칸) | **run88** (Q151b, sast/85) |
| 3 (가) 매니페스트 해석이 유저 (sxs.dll 등) 에서인가 csrss 에서인가 | **run87**: **csrss** — sxssrv 가 sxs.dll 을 불러와 `SxsGenerateActivationContext` 를 부른다 (sast/84 질문 2). 자식 프로세스 안이 아니다. 부모 kernel32 는 느린 길에서 매니페스트 스트림 (자원 · 파일) 만 연다 |
| 3 (나) comctl32 v6 최소 조건 · WinSxS 경로 규칙 | csrss (sxs) 쪽 경로 만드는 함수 이름만 **run87** (sast/84 질문 4 일부). 블롭 안에 무엇이 들어가나 · 폴더를 여는 쪽 · 최소 조건은 **run88** (sast/85) |
| 4 시스템 기본 문맥만 있을 때 리다이렉트 결과 | **run88** (sast/85). "시스템 기본 문맥은 늘 만든다" 는 run87 (sast/84 질문 3) |
| (배경) nt 엔진이 직접 만들어 넣을 수 있나 · 최소 모양 | 판단 · 이유 **run87** (sast/84 질문 5). 최소 블롭 모양은 **run88** (sast/85) |

### 질문마다 (결과서 sast/84 그대로, 근거 줄은 오케스트레이터 · flow-writer 가 `.asm` 으로 다시 봄)

| 질문 (요청서 번호) | 답 (한 줄) | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|
| 1 부모 쪽 | 부모는 exe 자원을 직접 안 뒤진다 — 매니페스트 (`RT_MANIFEST` 24, ID 1) 는 **커널** `PspLocateInPEManifest` 가 찾아 만들기 정보로 주소 · 크기를 돌려준다 (sast/54). 메시지 만드는 함수 = **kernel32 `BasepConstructSxsCreateProcessMessage`** (rva `0x26070`, 1877B — kernelbase 가 **지연 import** 로 부름). 그 뒤 `BasepCsrCaptureSxsMessage` (kernelbase rva `0xfadc0`) 가 문자열을 캡처하고 `CsrClientCallServer(메시지, 캡처, 0x1001d, 0x218)`. **매니페스트가 없어도 메시지는 그대로 보낸다** (프로세스 등록 알림이라 늘) — 없는지 알아보는 쪽은 csrss | 부모 흉내는 필요 없음. 커널 몫 (비트 13 · 주소 · 크기) 은 위 확인거리 | `CreateProcessInternalW +0x3f51 ~ +0x3f72` · `+0x4361/+0x4366` · `+0x4458/+0x445f` · `+0x463b/+0x4640` · `+0x464f ~ +0x4672` (오케스트레이터 · flow-writer) |
| 2 csrss 쪽 — 누가 무엇을 자식에 넣나 | **csrss 가 넣는다.** 블롭 만드는 함수 = sxs `SxsGenerateActivationContext` (rva `0x5a2b0`, 내보내기). 넣는 법은 **섹션을 자식에 붙이기 + 자식 PEB 에 주소 쓰기** 둘 다 (아래 표) | 빠져 있음 (틀림). 3 ~ 5 · 10 ~ 12 를 엔진이 대신 | `BaseSrvSxsDoSystemDefaultActivationContext +0x1ab` · `+0x1fa ADD RDX,0x308` · `+0x211` (오케스트레이터 · flow-writer), `FromStructEx +0xadb` · `+0xb09` (오케스트레이터 · flow-writer), `InternalSxsCreateProcess +0x60c LEA RAX,[R13+0x2f8]` (flow-writer) |
| 3 시스템 기본 문맥 | **매니페스트 없는 exe 에도 늘 만든다** (부모가 SxS 를 통째로 건너뛰는 보호 프로세스류만 빼고). `BaseSrvSxsCreateProcess` 가 **먼저** 시스템 기본을, 그다음 앱 문맥을 부른다. 시스템 기본 섹션은 csrss 전역 표 `SxsSystemDefaultActivationContexts` (5칸, 구조마다) 에 **한 번 만들고 돌려쓴다**. 만드는 재료는 코드가 짓는 합성 요청 하나 (`Microsoft.Windows.SystemCompatible,version="6.0.0.0",…` · 이름표 `"System Default Context"` · 루트 `<Windows>\WinSxS`). 실제 내용 (어떤 조립이 들었나) 은 결과서 `[공백]` → **run88 (sast/85) 에서 닫힘** | 빠져 있음. test.exe 도 `+0x308` 이 채워져야 맞다 | `BaseSrvSxsCreateProcess +0x1a` · `+0x2f` · `+0x8d` · `+0x94` · `+0x96` (flow-writer), `DoSystemDefault +0x13e` · `+0x5ef` (flow-writer) |
| 4 WinSxS 경로 규칙 | (일부만) csrss (sxs) 쪽에 어셈블리 이름 → 폴더 이름 함수 다섯 (`SxspGenerateKeyform` · `SxspGenerateTextualIdentity` · `SxspGenerateSxsPath` · `SxspGenerateManifestPathForProbing` · `SxspGenerateManifestPathOnAssemblyIdentity`) 과 문자열 `\WinSxS\` · `manifests` · `.manifest` 가 있다 — 의존 어셈블리를 찾을 때 csrss 가 `WinSxS\manifests\<키폼>.manifest` 로 간다. **블롭 안에 전체 경로냐 키만이냐** 는 결과서 `[공백]` → **run88 (sast/85) 에서 닫힘**. 폴더를 실제로 여는 쪽 (`[추측: 로더 = ntdll]`) · "6.0.0.0 요청이 어느 6.0.26100.x 로 묶이나" 는 **run88 (sast/85) 에서 다룸** | 블롭 모양을 알기 전엔 못 정함 → run88 | `ms/decomp/sxs/_index.tsv`, sxs.dll UTF-16 문자열 (결과서) |
| 5 nt 엔진이 직접 만든다면 | **미리 만든 블롭 (시스템 기본 1 + 앱 1) 을 덤프에서 떠 와 붙이고 `PEB+0x308` · `+0x2f8` 에 주소만 쓰는 게 가장 싸다.** 해석 · 정책 · 캐시는 흉내 안 내도 된다. 한계: 앱 블롭은 exe 마다, 시스템 기본은 구조마다 다르고, 같은 빌드 · 같은 WinSxS 에서만 맞는다 | 맞는 방향 (요청서의 "덤프에서 떠 와 매핑" 과 같음) | 아래 표 3 ~ 5 · 10 ~ 12 |

### 누가 무엇을 자식에 넣나 (결과서 질문 2 표, 차례는 코드 순서)

| 차례 | 프로세스 | 함수 + 자리 (call/ret) | syscall | 자식에 생기는 것 |
|---|---|---|---|---|
| 1 | 커널 (부모의 `NtCreateUserProcess` 안) | `CreateProcessInternalW +0x30e7/+0x30ee` → 커널 `PspLocateInPEManifest` (sast/54) | (같은 호출) | 자식 PEB, `ProcessParameters.Flags` 비트 13 (매니페스트 자원을 찾았을 때), 만들기 정보에 매니페스트 주소 · 크기 |
| 2 | 부모 | `CreateProcessInternalW +0x466b/+0x4672` | `CsrClientCallServer` → ALPC | 아직 없음 (자식 PEB 주소 · 구조 번호 · SxS 칸을 csrss 로 보냄) |
| 3 | csrss | `BaseSrvSxsCreateProcess +0x8d/+0x92` → `BaseSrvSxsDoSystemDefaultActivationContext +0x1ab/+0x1b2` | `NtMapViewOfSection` (csrss 의 시스템 기본 섹션 → 자식) | VAD 하나: 시스템 기본 블롭 (읽기 전용) |
| 4 | csrss | 같은 함수 `+0x211/+0x218` | `NtWriteVirtualMemory` (보통 8B) | **`PEB+0x308` `SystemDefaultActivationContextData`** ← 붙인 주소 |
| 5 | csrss (WOW64 일 때) | 같은 함수 `+0x279/+0x280` | `NtWriteVirtualMemory` (4B) | `PEB32+0x200` |
| 6 | csrss | `InternalSxsCreateProcess +0x2d6/+0x2dd` | `NtDuplicateObject` (부모의 exe 파일 핸들 → csrss) | 없음 (csrss 쪽 핸들, Flags `0x40` 갈래) |
| 7 | csrss | `InternalSxsCreateProcess +0x330/+0x337` | `NtReadVirtualMemory` (자식 `PEB+0x10` `ImageBaseAddress`) | 없음 (읽기만) |
| 8 | csrss | `+0x3f6/+0x3fb BaseSrvSxsProbeManifestAndPolicyAndDotLocal` | `NtApphelpCacheControl` (`g_CompatCacheServiceApi`) · 자식 메모리의 자원 · `.Manifest` 파일 · `.local` | 없음 (읽기만. 부모 토큰을 흉내 내고 파일에 접근) |
| 9 | csrss | `+0x66d/+0x672` → `BaseSrvSxsCreateActivationContextFromStructEx` → 간접 `+0x8d6/+0x8db` = sxs `SxsGenerateActivationContext` | (sxs 안: `SxspBuildActCtxData +0x16c CreateFileMappingW` · `+0x1a4 MapViewOfFile`) | 없음 (csrss 안에 블롭 섹션. 같은 키면 캐시 재사용 — `GetCacheKey +0x414` · `LookupEntry +0x468` · `InsertEntry +0xa37`) |
| 10 | csrss | `FromStructEx +0xadb/+0xae2` | `NtMapViewOfSection` (블롭 섹션 → 자식) | VAD: **앱 블롭** (매니페스트가 있을 때만) |
| 11 | csrss | `FromStructEx +0xb09/+0xb10` | `NtWriteVirtualMemory` (8B) | **`PEB+0x2f8` `ActivationContextData`** ← 붙인 주소 |
| 12 | csrss (WOW64) | `FromStructEx +0xb54/+0xb5b` | `NtWriteVirtualMemory` (4B) | `PEB32+0x1f8` |
| 13 | csrss | `InternalBaseSrvCreateProcess +0x175/+0x17c` | `CsrCreateProcess` (run34) | 없음 (csrss 쪽 프로세스 등록) |
| 14 | **부모** (SxS 아님) | `CreateProcessInternalW +0x574e/+0x5755` · `+0x578e/+0x5795` · **`+0x57d0/+0x57d7`** · `+0x582a/+0x5831` | `NtAllocateVirtualMemory` · `NtWriteVirtualMemory` ×3 | 앱 호환 데이터 private 영역 + **`PEB+0x2d8` `pShimData`** (8B) + `PEB32+0x1e8` (4B, WOW64) |

- 차례 14 는 2 **뒤**다 (`+0x57d0 > +0x466b`). csrss 일 (3 ~ 13) 이 14 보다 먼저인 건 "부모가 `CsrClientCallServer` 에서 답을 기다린다" `[추측: 일반 지식, flow.md 2.2 Q66]` 에 기댄다.
- 사진 (run44): K3 에선 PEB `pShimData` · `SystemDefaultActivationContextData` 둘 다 (null) (`ms/logs/04_baggage-read_K3.txt:1633 · 1638`), P0 에선 `25996d10000` · `25996d00000` (`04_baggage-read_P0.txt:1635 · 1640`), `ActivationContextData` 는 8장 모두 (null) (`_P0.txt:1638`, `_P2.txt:2515`) `[관찰]` — 위 표 (test.exe 는 매니페스트 없음 → 4 는 있고 11 은 없음) 와 맞는다.

### SxS 메시지 — 부모가 보내는 것 (결과서 질문 1)

메시지 머리 `0x40`B 뒤 데이터 `0x218`B (`CreateProcessInternalW` 안 지역 `[RSP+0xbe0]` 가 머리). csrss 쪽 오프셋은 머리 기준이라 아래 값 + `0x40`.

| 데이터 기준 | 뜻 | csrss 쪽 (`BaseSrvSxsCreateProcess`) |
|---|---|---|
| `+0x0` · `+0x8` · `+0x10` | ProcessHandle · ThreadHandle · ClientId | — |
| `+0x20` | CreationFlags (`[RSP+0x108] & ~3`) | `+0x60` — **비트 31 이 켜져 있으면 SxS 전체 건너뜀** (`+0x1a CMP [R8+0x60],0` · `+0x2f JGE`) |
| `+0x38` (`0x1c8`B) | **SxS 칸** (아래) | `+0x78` |
| `+0x200` · `+0x208` | 자식 PEB 주소 (native · WOW64) | `+0x240` · `+0x248` |
| `+0x210` (word) | 프로세서 구조 번호 | `+0x250` — 0 → `0xa`, 5 → `0xd`, 나머지 그대로 (`+0x6d ~ +0x9f`) |

SxS 칸 (`+0x38` 부터, 쓰는 줄 · 읽는 줄로 확인한 것만):

| SxS 기준 | 뜻 | 쓰는 줄 (kernel32 `BasepConstructSxsCreateProcessMessage`) | csrss 가 읽는 곳 |
|---|---|---|---|
| `+0x0` Flags | `0x1` 매니페스트 스트림 · `0x2` 정책 (`.Config`) 스트림 · **`0x20` SxS 건너뜀** (`+0x37f`) · **`0x40` "매니페스트는 csrss 가 찾아라"** (`+0x622`) · 그 밖 `0x10` · `0x80` · `0x200` · `0x400` · `0x800` | 위 | `InternalSxsCreateProcess` 맨 위: `(Flags & 0x20) == 0 && (Flags & 0x41) != 0` 일 때만 앱 문맥 |
| `+0x4` | 자식 `ProcessParameters.Flags` 사본 — 비트 13 `0x2000` 매니페스트 자원 찾음 · `0x1000` `.local` | `+0x6f6` | 메시지 `+0x7c` — `InternalSxsCreateProcess +0x405 TEST [R15],0x2000` |
| `+0x8` | exe 파일 핸들 (`0x40` 갈래) | `+0x625` | `NtDuplicateObject +0x2d6` |
| `+0x10` · `+0x20` | exe 경로 문자열 둘 | `+0x630` · `+0x60a` | `BaseSrvSxsValidateMessageStrings +0x26f` |
| `+0x50` · `+0x58` | **자식 안 매니페스트 주소 · 크기 (커널이 준 값)** | `+0x616` · `+0x61d` | Probe 인자 |
| `+0x88` | 언어 대체 목록 | `+0x649/+0x64e` | 문자열 검증 |
| `+0xa8` | 빈 "어셈블리 이름 자리" (`-` 59개로 채움, csrss 가 답을 씀) | `+0x691` · `+0x69f` | `CsrValidateMessageBuffer +0x800` |

- **두 갈래 (부모 kernel32)**:
  - **빠른 길 (보통)**: `PreferExternalManifest` 레지스트리 값 0 · 인자 7 꺼짐 · 이동식 매체 아님 · 안전 부팅 아님 (`+0x3b7 CMP byte [0x7ffe02ec]` = KUSD `SafeBootMode`) 이면 (또는 자식이 보호 프로세스면) `+0x65a` 로 → Flags `0x40` 과 경로 · 파일 핸들 · 커널이 준 주소 · 크기만 채우고 끝. **부모는 매니페스트 파일도 자원도 안 연다.**
  - **느린 길**: 하나라도 어긋나면 `+0x5ba BasepSxsCreateProcessCsrMessage` → `BasepSxsCreateStreams` — `PreferExternalManifest` 0 이면 자원 먼저 (`LdrResSearchResource` 타입 24) 없으면 `<exe>.Manifest` 파일 (`NtOpenFile` · `NtCreateSection`), 1 이면 순서 반대. `.Config` 도 같은 파일 함수로.
- **건너뛰는 갈래 (부모 kernelbase)**: `NtCreateUserProcess` 출력 Flags 비트 0 켜짐 · 비트 4 꺼짐 (`+0x4328 ~ +0x4336`, 이름은 보호 프로세스 · 경량 보호 `[추측]`) 이면 메시지 함수를 안 부르고, `+0x4623` 에서 ThreadHandle 칸 비트 1 을 켠다 → csrss `InternalBaseSrvCreateProcess +0xca ~ +0xcc` 가 SxS 호출 자체를 건너뜀 → **그 프로세스는 시스템 기본 문맥도 못 받는다**.

## 환경

- 26100.9457, 정적만 (VM 실행 없음)
- 정적 (이번에 처음 analyze — 결과서):
  - sxssrv pdb `8FAA75EF96847C62E731DC9A759FE2C21` (`ms/sym/sxssrv.dll.pdb.info.txt`), 함수 99개
  - sxs pdb `76674E9722F9EFB085A6BE05B23E9B371` (`ms/sym/sxs.dll.pdb.info.txt`), 함수 901개
- 읽기만 (다시 analyze 안 함): kernelbase pdb `72CCE506944DE0267C172AD64287F9BD1` (run14 의 `CreateProcessInternalW.asm` 재사용 + `BasepCsrCaptureSxsMessage` 새로) · kernel32 pdb `AE9E9D8F7AC1467A645320FDC23235D01` (13개 지정 + depth 1 = 35개 새로) · basesrv pdb `4B0951C4FAD4CCBDDFDAA284D1ED2C2E1` (sast/29 의 `.asm` 재사용).
- 버전 대조 (결과서): `CreateProcessInternalW +0x466b/+0x4672 → CsrClientCallServer` (sast/09), `+0x5795/+0x57d7/+0x5831 NtWriteVirtualMemory` (run14), `BaseSrvCreateProcess2 +0x216/+0x21b → InternalBaseSrvCreateProcess` (sast/29) 모두 그대로. sxssrv `ServerDllInitialization +0xdf/+0xe6` 이 `functions` 표를 `BaseSrvRegisterSxS` 로 넘기고, 표 `+0x8` 칸 = `BaseSrvSxsCreateProcess` (rva `0x1cd0`) 를 결과서가 `pefile` 로 표 (rva `0x90f0`) 를 읽어 맞춤.

## 목적

- PE_emulator 요청 29 (SxS 활성화 문맥) 의 첫 조각 (Q151a) — **만드는 쪽**: 부모 kernelbase `CreateProcessInternalW` → csrss (basesrv · sxssrv · sxs) → 자식 PEB 에 문맥 데이터가 들어가는 길. 원문 1 · 3 (일부), 매니페스트 있을 때 · 없을 때. **첫 답은 지도 수준** (사용자 결정 2026-11-12).
- 겹치는 대기열 Q87 (부모 `BasepInitAppCompatData` 본문 — 부모가 자식 PEB 를 쓰나) 을 같이 닫는다.
- 짝 Q151b (로더가 읽는 쪽, sast/85) 는 다음 바퀴 run88.

## 방법

- 정적: `_workspace/sast/84_sxs-create.md` — kernelbase (`CreateProcessInternalW` 의 SxS 갈래만) · kernel32 · basesrv · **sxssrv · sxs (새 analyze, PDB 붙음)**
  - 뽑음: kernelbase `BasepCsrCaptureSxsMessage` 1, kernel32 13 + depth 1 = 35 (`BasepConstructSxsCreateProcessMessage` · `BasepSxsCreateProcessCsrMessage` · `BasepSxsCreateStreams` · `BasepSxsCreateFileStreamEx` · `BasepSxsCreateResourceStream` · `BasepGetPreferExternalManifestConfig` · `BasepInitAppCompatData` 등), sxssrv 21 + depth 1 = 44, sxs `SxsGenerateActivationContext` depth 1 + 경로 함수 5 depth 0.
  - 출력 `ms/decomp/{kernel32,sxssrv,sxs}/`, `ms/disassemble/{kernel32,sxssrv,sxs,kernelbase}/` (+ `_calls.tsv`), 로그 `ms/ghidra/*.sast84*.stdout`. 다른 decompiler 가 쓰는 ntdll · ucrtbase · condrv · user32 · win32u · gdi32 프로젝트는 안 열었다.
  - 결과서가 `pefile` 로 읽은 것: kernelbase 지연 import 표 (`DelayLoad_BasepConstructSxsCreateProcessMessage` rva `0x12a3c4`, API 세트 `ext-ms-win-kernelbase-processthread-l1-1-0`), sxssrv `functions` 표 (rva `0x90f0`) · 구조 표 (rva `0xc038`).
- 결과서: `_workspace/sast/84_sxs-create_result.md` (**분석성공 — 지도 수준**. 질문 1 · 2 · 3 은 함수 · 오프셋 · syscall 까지, 5 는 판단 + 이유, **4 는 일부** — 블롭 안 폴더 칸 · 로더 쪽 해석은 Q151b 로 넘김. Q87 닫힘)
- 오케스트레이터가 `.asm` 으로 다시 맞춘 줄: kernelbase `CreateProcessInternalW +0x4458 BasepConstructSxsCreateProcessMessage` (kernel32, 지연 import) · `+0x466b CsrClientCallServer` · `+0x57d0` / `+0x582a NtWriteVirtualMemory` (`pShimData`), sxssrv `BaseSrvSxsDoSystemDefaultActivationContext +0x1fa ADD RDX,0x308`, `BaseSrvSxsCreateActivationContextFromStructEx +0xb09 NtWriteVirtualMemory`.
- flow-writer 도 다시 봤다:
  - `.asm` 줄: kernelbase `CreateProcessInternalW +0x3f51 ~ +0x3f72` · `+0x4328 ~ +0x4366` · `+0x4440 ~ +0x4477` · `+0x4604 ~ +0x4677` · `+0x56b3 ~ +0x5839` · `+0x5976`, kernel32 `BasepInitAppCompatData` (전부, 123B), sxssrv `BaseSrvSxsCreateProcess` (전부) · `BaseSrvSxsDoSystemDefaultActivationContext +0x0 ~ +0x2ad` · `+0x433` · `+0x523` · `+0x5ef` · `InternalSxsCreateProcess` call 줄 · `+0x405 ~ +0x40c` · `+0x60c` · `+0x8c9` · `FromStructEx +0x414 ~ +0xb64` call 줄, basesrv `InternalBaseSrvCreateProcess +0xc2 ~ +0x1d9`, sxs `SxsGenerateActivationContext` call 줄 · `SxspBuildActCtxData +0x16c` · `+0x1a4`.
  - 이름: `_index.tsv` (kernelbase · kernel32 — 함수가 어느 모듈에 있나), `pdb_aliases.py --lookup` 로 이번에 이름을 쓴 함수 (kernel32 `0x26070` · `0x78e40` · `0x26ae4` · `0x258a8`, kernelbase `0xfadc0`, sxssrv `0x1cd0` · `0x1440` · `0x1d80` · `0x3170` · `0x2800`, sxs `0x5a2b0` · `0x1d1c0` · `0x2f950` · `0x4b060`) 가 별칭 무리가 아님을 확인. kernelbase `0x12a02c` (아래 표 "있나" 검사) 만 **같은 주소 이름 21개**.
  - PE 표: `objdump -p KernelBase.dll` 로 `NtWriteVirtualMemory` · `NtAllocateVirtualMemory` · `CsrClientCallServer` 가 `ntdll.dll` 가져오기임을 확인 (지연 import 표는 이 `objdump` 가 안 보여 결과서 값 그대로).
  - 사진 값 (run44 덤프 읽기 로그): PEB `+0x2d8` · `+0x2f8` · `+0x308` 과 VAD 두 줄 (아래 "보탠 것" ③). 새 동적 실행은 없다.

## 결과

### 정적 — 호출 지점 (`ms/disassemble/{kernelbase,kernel32,basesrv,sxssrv,sxs}/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `KERNELBASE!CreateProcessInternalW` | `+0x30e7` | `+0x30ee` | `ntdll!NtCreateUserProcess` | 커널이 매니페스트를 찾고 (`PspLocateInPEManifest`, sast/54) PEB 주소를 돌려줌 (run14) | [정적] |
| 〃 | `+0x4361` | `+0x4366` | "그 함수가 있나" 검사 (같은 주소 이름 21개 `Is<이름>Present` — 후보 `IsBasepConstructSxsCreateProcessMessagePresent` 등, `pdb_aliases.py --lookup KernelBase.dll 0x12a02c`) | 0 이면 `+0x4368 JZ` → `+0x4466` (메시지 함수 안 부름) | [정적] |
| 〃 | **`+0x4458`** | **`+0x445f`** | **`KERNEL32!BasepConstructSxsCreateProcessMessage`** (지연 import 칸 `[0x1803c66d8]`) | 출력 Flags 비트 0 켜짐 · 비트 4 꺼짐이면 `+0x4336 JZ` → `+0x4477` 로 안 탐 | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0x463b` | `+0x4640` | `KERNELBASE!BasepCsrCaptureSxsMessage` (rva `0xfadc0`) | 음수면 `+0x4649 JS` (실패 길) | [정적] |
| 〃 | **`+0x466b`** | **`+0x4672`** | `ntdll!CsrClientCallServer` | `R8D = 0x1001d` (`+0x4655`), `R9D = 0x218` (`+0x464f`), 메시지 `[RSP+0xbe0]` | [정적] (run14 · 오케스트레이터) |
| 〃 | `+0x56bb` | `+0x56c2` | `KERNEL32!BasepGetAppCompatData` (지연 import) | 결과 `[RSP+0x2a0]` 가 0 이면 `+0x56d2 JZ` → 아래 넷 안 탐 | [정적] |
| 〃 | `+0x574e` | `+0x5755` | `ntdll!NtAllocateVirtualMemory` | 자식에 영역 (`+0x572b` `0x1000` MEM_COMMIT, 크기 `[RSP+0x1f8]`) | [정적] |
| 〃 | `+0x578e` | `+0x5795` | `ntdll!NtWriteVirtualMemory` | 그 영역에 앱 호환 데이터 복사 | [정적] |
| 〃 | **`+0x57d0`** | **`+0x57d7`** | `ntdll!NtWriteVirtualMemory` | **자식 PEB `+0x2d8` (`pShimData`) 8B** — `+0x57a9 MOV RDX,[RSP+0x1a0]` (자식 PEB) · `+0x57b1 ADD RDX,0x2d8`, 값 `[RSP+0x3a8]` = 위 영역 주소 | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0x582a` | `+0x5831` | `ntdll!NtWriteVirtualMemory` | WOW64 PEB `+0x1e8` 4B (`+0x57f4` `[RSP+0x590]` 0 이면 안 탐) | [정적] |
| 〃 | `+0x5976` | `+0x597d` | `KERNEL32!BasepInitAppCompatData` (지연 import) | PEB 안 씀 (아래) | [정적] |
| `KERNEL32!BasepConstructSxsCreateProcessMessage` | `+0x5ba` | `+0x5bf` | `BasepSxsCreateProcessCsrMessage` (rva `0x26ae4`) | 느린 길만 | [정적] |
| `KERNEL32!BasepSxsCreateProcessCsrMessage` | `+0x1ac` | `+0x1b1` | `BasepSxsCreateStreams` (rva `0x258a8`) | `.Manifest` · `.Config` 스트림 | [정적] |
| `KERNEL32!BasepInitAppCompatData` | `+0x5c` | `+0x61` | (CFG 간접) `g_CompatCacheServiceApi(0xa, 구조체)` | `+0x2f memset(0x188)` 뒤 한 번. 이 함수 안 호출은 이 둘뿐 | [정적] (flow-writer 전부 읽음) |
| `basesrv!BaseSrvCreateProcess2` | `+0x216` | `+0x21b` | `InternalBaseSrvCreateProcess` | (sast/29 · run34) | [정적] |
| `basesrv!InternalBaseSrvCreateProcess` | **`+0xfb`** | **`+0x100`** | (CFG 간접) `sxsFunctions[+0x8]` = **`sxssrv!BaseSrvSxsCreateProcess`** | `+0xca` 첫 인자 0 · `+0xce` 부모 CSR_PROCESS `[+0x5c]` 비트 `0x2000` 꺼짐 · `+0xd7` 표 있음 · `+0xe3` 메시지 있음. `+0xf7 MOV RAX,[RAX+0x8]` 이 표 칸 읽기. **음수면 `+0x102 JS` → `+0x1bc` (아래 `CsrCreateProcess` 도 건너뛰고 끝)** | [정적] (flow-writer) |
| 〃 | `+0x175` | `+0x17c` | `csrsrv!CsrCreateProcess` | (run34) | [정적] |
| `sxssrv!BaseSrvSxsCreateProcess` | **`+0x8d`** | **`+0x92`** | `BaseSrvSxsDoSystemDefaultActivationContext` (rva `0x1440`) | 메시지 `+0x60` 비트 31 꺼짐 (`+0x2f JGE +0x6d`). 음수면 `+0x94 JS` → 앱 문맥 안 부르고 그 값 반환 | [정적] (flow-writer) |
| 〃 | `+0x4c` | `+0x51` | `InternalSxsCreateProcess` (rva `0x1d80`) | 시스템 기본 성공 뒤 `+0x96 JMP +0x38` 으로 옴 | [정적] (flow-writer) |
| `sxssrv!BaseSrvSxsDoSystemDefaultActivationContext` | `+0x5ef` | `+0x5f4` | `BaseSrvSxsCreateActivationContextFromStructEx` (처음 만들 때만) | 표 칸이 비었을 때 (`+0x13e CMP [R15],0` · `+0x142 JZ +0x2b2`) | [정적] |
| 〃 | **`+0x1ab`** | **`+0x1b2`** | `NtMapViewOfSection` | 섹션 `[R15]` → 자식 (`R13`). `+0x172` 보호 `R14D = 2`, `+0x177` `0x400000`, `+0x17f` 공유 1 | [정적] (flow-writer) |
| 〃 | **`+0x211`** | **`+0x218`** | `NtWriteVirtualMemory` | **자식 PEB `+0x308`** (`+0x1f2 MOV RDX,[RSP+0xc0]` = 메시지 `+0x240` · `+0x1fa ADD RDX,0x308`). 크기는 구조 번호로 (보탠 것 ①) | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0x279` | `+0x280` | `NtWriteVirtualMemory` | WOW64 PEB `+0x200` (`+0x260 ADD RDX,0x200`) — WOW64 PEB 주소가 0 아니고 구조 `0xa` 일 때 | [정적] (flow-writer) |
| `sxssrv!InternalSxsCreateProcess` | `+0x2d6` | `+0x2dd` | `NtDuplicateObject` | Flags `0x40` 갈래 | [정적] |
| 〃 | `+0x330` | `+0x337` | `NtReadVirtualMemory` | 자식 `PEB+0x10` | [정적] |
| 〃 | `+0x3f6` | `+0x3fb` | `BaseSrvSxsProbeManifestAndPolicyAndDotLocal` (rva `0x2800`) | 뒤 `+0x405 TEST [R15],0x2000` · `+0x40c JZ +0x8b2` — 못 찾으면 앱 문맥 건너뜀 | [정적] (flow-writer) |
| 〃 | `+0x66d` | `+0x672` | `BaseSrvSxsCreateActivationContextFromStructEx` (rva `0x3170`) | 대상 칸 = `+0x60c LEA RAX,[R13+0x2f8]` (WOW64 는 `+0x8c9 ADD RAX,0x1f8`) | [정적] (flow-writer) |
| `sxssrv!BaseSrvSxsCreateActivationContextFromStructEx` | `+0x414` · `+0x468` | `+0x419` · `+0x46d` | `BaseSrvSxsGetCacheKey` · `BaseSrvActivationContextCacheLookupEntry` | 캐시 | [정적] |
| 〃 | `+0x830` | `+0x835` | `BaseSrvSxsGetActivationContextGenerationFunction` | sxs.dll 올리기 · 내보내기 얻기 | [정적] |
| 〃 | `+0x8d6` | `+0x8db` | (CFG 간접, 스택 칸) = `sxs!SxsGenerateActivationContext` (결과서 — 위 함수가 얻은 주소) | 블롭 만들기 | [정적] |
| 〃 | `+0xa37` | `+0xa3c` | `BaseSrvActivationContextCacheInsertEntry` | | [정적] |
| 〃 | **`+0xadb`** | **`+0xae2`** | `NtMapViewOfSection` | 블롭 섹션 → 자식, `+0xa98` 보호 2 · `+0xaa5` `0x400000` · `+0xab0` 공유 1. 대상 칸 `[RDI+0xb8]` 이 0 이면 `+0xa8a JZ` 로 안 탐 | [정적] (flow-writer) |
| 〃 | **`+0xb09`** | **`+0xb10`** | `NtWriteVirtualMemory` | **`[RDI+0xb8]` = 자식 PEB `+0x2f8`**, `R9D = 8` (`+0xaf8`) | [정적] (오케스트레이터 · flow-writer) |
| 〃 | `+0xb54` | `+0xb5b` | `NtWriteVirtualMemory` | WOW64 `[RDI+0xc0]`, 크기 구조 `0xa`·`0xd` 면 4 (`+0xb2b ~ +0xb3f`) | [정적] (flow-writer) |
| `sxs!SxsGenerateActivationContext` | `+0x3c2` · `+0x5bc` · `+0x61d` | `+0x3c7` · `+0x5c1` · `+0x622` | `SxspInitActCtxGenCtx` · `SxspAddRootManifestToActCtxGenCtx` · `SxspBuildActCtxData` (rva `0x1d1c0`) | `_calls.tsv` 표시는 `alias:…+1` — 별칭 하나는 C++ 꾸민 이름이라 같은 함수 (`pdb_aliases.py` 무리 아님) | [정적] |
| `sxs!SxspBuildActCtxData` | `+0x16c` · `+0x1a4` | `+0x173` · `+0x1ab` | `CreateFileMappingW` · `MapViewOfFile` | 블롭을 담을 익명 섹션 (csrss 안) | [정적] |

### 호출 관계

```
(부모 cmd.exe 스레드)  KERNELBASE!CreateProcessInternalW
 ├─ +0x30ee  ntdll!NtCreateUserProcess            커널이 매니페스트 찾음 (PspLocateInPEManifest, sast/54)
 │      … 검사 (kernel32 지연 import 들)
 ├─ +0x4366  "있나" 검사 (같은 주소 이름 21개)      0 → +0x4466
 ├─ +0x445f  KERNEL32!BasepConstructSxsCreateProcessMessage   SxS 칸 0x1c8B   (보호 프로세스 갈래면 안 탐)
 │            └─ (느린 길만) +0x5bf BasepSxsCreateProcessCsrMessage → +0x1b1 BasepSxsCreateStreams
 ├─ +0x4640  BasepCsrCaptureSxsMessage            문자열 캡처
 ├─ +0x4672  ntdll!CsrClientCallServer(0x1001d, 0x218)
 │      ┄┄▶ (csrss 요청 스레드)  basesrv!BaseSrvCreateProcess2
 │              └─ +0x21b  InternalBaseSrvCreateProcess
 │                   ├─ (1) +0x100  (CFG 간접) sxsFunctions+0x8 = sxssrv!BaseSrvSxsCreateProcess      음수면 +0x102 JS 로 끝
 │                   │        ├─ (1) +0x92  BaseSrvSxsDoSystemDefaultActivationContext   (메시지 +0x60 비트 31 꺼짐)
 │                   │        │        ├─ (처음만) +0x5f4  BaseSrvSxsCreateActivationContextFromStructEx(-1, -1, …)
 │                   │        │        ├─ +0x1b2  NtMapViewOfSection   시스템 기본 섹션 → 자식 (읽기 전용)
 │                   │        │        └─ +0x218  NtWriteVirtualMemory  → 자식 PEB +0x308
 │                   │        └─ (2) +0x51  InternalSxsCreateProcess                    (시스템 기본 성공 뒤)
 │                   │                 ├─ +0x3fb  BaseSrvSxsProbeManifestAndPolicyAndDotLocal   못 찾으면 +0x40c → 끝
 │                   │                 └─ +0x672  BaseSrvSxsCreateActivationContextFromStructEx
 │                   │                          ├─ +0x8db  (간접) sxs!SxsGenerateActivationContext → SxspBuildActCtxData
 │                   │                          ├─ +0xae2  NtMapViewOfSection   앱 블롭 → 자식 (읽기 전용)
 │                   │                          └─ +0xb10  NtWriteVirtualMemory  → 자식 PEB +0x2f8
 │                   └─ (2) +0x17c  csrsrv!CsrCreateProcess     (run34 — 그 안 CsrSetExceptionPort, run51)
 │      … 상승 · 잡 (조건부)
 ├─ +0x56c2  KERNEL32!BasepGetAppCompatData       0 이면 아래 넷 안 탐
 ├─ +0x5755  ntdll!NtAllocateVirtualMemory        자식에 private 영역
 ├─ +0x5795  ntdll!NtWriteVirtualMemory           영역에 앱 호환 데이터
 ├─ +0x57d7  ntdll!NtWriteVirtualMemory           → 자식 PEB +0x2d8 (pShimData)
 ├─ +0x5831  ntdll!NtWriteVirtualMemory           → WOW64 PEB +0x1e8 (WOW64 일 때)
 ├─ +0x597d  KERNEL32!BasepInitAppCompatData      g_CompatCacheServiceApi(0xa, …) 뿐, PEB 안 씀
 └─ +0x5bd3  ntdll!NtResumeThread                 (run14)
```

## 확인된 것

- `[정적]` 자식 PEB `+0x308` (`SystemDefaultActivationContextData`) 을 쓰는 줄은 csrss 안 `sxssrv!BaseSrvSxsDoSystemDefaultActivationContext +0x211/+0x218 NtWriteVirtualMemory` 다 (`+0x1fa ADD RDX,0x308`). 값은 같은 함수 `+0x1ab/+0x1b2 NtMapViewOfSection` 으로 자식에 붙인 시스템 기본 섹션 주소다.
- `[정적]` 자식 PEB `+0x2f8` (`ActivationContextData`) 을 쓰는 줄은 csrss 안 `sxssrv!BaseSrvSxsCreateActivationContextFromStructEx +0xb09/+0xb10` 이다. 대상 칸 주소는 `InternalSxsCreateProcess +0x60c LEA RAX,[R13+0x2f8]` 에서 오고, 그 길은 매니페스트를 찾았을 때만 탄다 (`+0x405 TEST [R15],0x2000` · `+0x40c JZ`).
- `[정적]` 부모가 자식 PEB 에 쓰는 칸은 `+0x2d8` (`pShimData`) 하나다: `KERNELBASE!CreateProcessInternalW +0x57d0/+0x57d7`. `+0x2f8` · `+0x308` 을 쓰는 줄은 부모 쪽에 없다.
- `[정적]` `BasepInitAppCompatData` · `BasepConstructSxsCreateProcessMessage` 는 **kernel32** 함수다 (kernel32 `_index.tsv` 에 본문, kernelbase `_index.tsv` 엔 `DelayLoad_<이름>` 스텁만). kernelbase 는 지연 import 로 부른다.
- `[정적]` `BaseSrvSxsCreateProcess` 는 시스템 기본 문맥을 **먼저**, 앱 문맥을 **나중에** 부르고, 시스템 기본이 실패하면 앱 문맥은 안 부른다 (`+0x8d` → `+0x94 JS` / `+0x96 JMP` → `+0x4c`).
- `[정적]` basesrv `InternalBaseSrvCreateProcess` 는 SxS 간접 호출 (`+0xfb`) 을 `CsrCreateProcess` (`+0x175`) 보다 먼저 부른다. SxS 가 음수면 `CsrCreateProcess` 까지 건너뛰고 끝난다 (`+0x102 JS +0x1bc`).
- `[관찰]` run44 사진: K3 → P0 사이에 PEB `pShimData` (null → `25996d10000`) · `SystemDefaultActivationContextData` (null → `25996d00000`), `ActivationContextData` 는 8장 모두 (null). P0 VAD 두 줄 = 위 두 값 (보탠 것 ③).

## 결과서와 다르게 적은 곳 · 보탠 것

- ① **`+0x211` 쓰기 크기는 구조 번호로 갈린다** (flow-writer `.asm`): `+0x1e0 CMP EBX,0xa` · `+0x29b CMP EBX,0xd` 가 맞으면 `+0x1e9 MOV R9,RSI` (섹션이 이미 있던 길에선 `+0x148` 의 4), 아니면 `+0x2a4` 에서 8. 결과서 표 4 의 "8바이트" 는 보통 (AMD64) 길이다. 구조 `0xa` · `0xd` 의 뜻 (x86 · ARM32 on x64) 은 결과서 `[추측: PROCESSOR_ARCHITECTURE 값]` 그대로.
- ② **SxS 가 실패하면 csrss 등록도 없다** (flow-writer `.asm`): `InternalBaseSrvCreateProcess +0x100 TEST EAX,EAX` · `+0x102 JS +0x1bc` 는 `CsrCreateProcess` (`+0x175`) · `UserNotifyProcessCreate` (`+0x1b7`) 를 건너뛰고 함수 끝 (`+0x1bc`) 으로 간다. 부모 쪽이 이 실패를 어떻게 받나 (프로세스 만들기 실패로 끝나나) 는 안 봤다 `[추측]`. PE_emulator 엔 csrss 가 없어 상관없다.
- ③ **사진 VAD 가 붙이는 모양과 맞는다** (flow-writer, run44 덤프 읽기 로그): P0 `25996d00 – 25996d03` **Mapped READONLY "Pagefile section, shared commit 0x4"** (`ms/logs/04_baggage-read_P0.txt:1380`) = PEB `+0x308` 값 (`:1640`) — 결과서가 `[추측]` 으로 둔 보호 2 (읽기 전용) · 익명 (pagefile) 섹션 (`CreateFileMappingW`) 과 맞다. 크기 4쪽 (`0x4000`B). `25996d10 – 25996d11` **Private READWRITE** 2쪽 (`:1381`) = `pShimData` (`:1635`) — 부모 `NtAllocateVirtualMemory` (`+0x572b` MEM_COMMIT) 와 맞다. K3 에선 둘 다 (null) (`04_baggage-read_K3.txt:1633 · 1638`). → 결과서 "동적 2" 의 test.exe 몫 (크기 · 보호 · 유형) 은 사진으로 이미 `[관찰]`.
- ④ **이름 정정을 넓힘 — `CreateProcessInternalW` 가 부르는 `Base*` · `Basep*` 대부분이 kernel32** (flow-writer `_index.tsv` · `pdb_aliases.py`): kernelbase `_index.tsv` 엔 `DelayLoad_<이름>` 스텁만, 본문은 kernel32 `_index.tsv` 에 있는 것 — `BasepCheckWebBladeHashes` (`0x388d0`) · `BasepIsProcessAllowed` (`0x380b0`) · `BasepCheckWinSaferRestrictions` (`0x32200`) · `BaseCheckElevation` (`0x1e970`) · `BaseMarkProcessTokenForInstallers` (`0x6cd00`) · `BasepGetAppCompatData` (`0x2efe0`) · `BaseElevationPostProcessing` (`0x3ae70`) · `BasepPostSuccessAppXExtension` (`0x43e50`) + 결과서 셋 (`BasepConstructSxsCreateProcessMessage` `0x26070` · `BasepInitAppCompatData` `0x78e40` · `BasepReleaseSxsCreateProcessUtilityStruct`). run14 · flow.md 2.2 의 "있나 검사 (같은 주소 이름 21개)" 가 바로 이 지연 import 들의 `Is<이름>Present` 21개다 (`pdb_aliases.py --lookup KernelBase.dll 0x12a02c`).
- ⑤ **결과서 `[공백]` "`+0x2e` 칸 이름" 은 ntoskrnl PDB 로 붙는다** (flow-writer): 결과서 짐작대로 `[RSP+0x870]` 이 `SECTION_IMAGE_INFORMATION` 시작이면 (`+0x20` = `[RSP+0x890]` 서브시스템 비교와 맞음), `[RSP+0x89e]` = `+0x2e` = ntoskrnl PDB `_SECTION_IMAGE_INFORMATION.DllCharacteristics` (짐 지도 3.23 — 결과서는 `pdb_struct.py` 가 이 구조체를 못 찾았다고 적음). 비트 9 (`0x200`) 이름 `IMAGE_DLLCHARACTERISTICS_NO_ISOLATION` 은 공개 상수 이름 `[추측]`. test.exe `DllCharacteristics = 0x8160` (짐 지도 3.23, 사진 K1) 은 비트 9 가 꺼져 있어 Flags `0x20` (SxS 건너뜀) 갈래를 안 탄다.
- ⑥ `InternalBaseSrvCreateProcess` SxS 간접 호출의 **call 은 `+0xfb`** 다 (결과서 지도 "`+0xf7/+0xfb` (ret `+0x100`)" 의 `+0xf7` 은 `MOV RAX,[RAX+0x8]` 표 칸 읽기). flow.md 2.2 의 ret `+0x100` 과 같다.
- ⑦ 결과서 순서 메모 "run45 사진에서 시스템 기본 매핑이 앞, `pShimData` 영역이 뒤" 는 **주소 크기 순서**라 쓴 순서의 근거로는 약하다 `[추측]`. 순서는 코드 (`+0x466b` < `+0x57d0`) 와 "부모가 csrss 답을 기다린다" `[추측]` 로만 선다 (Q83 동적).
- `_calls.tsv` 의 `alias:` 줄 중 이름을 쓴 것은 sxs `SxspInitActCtxGenCtx` · `SxspAddRootManifestToActCtxGenCtx` · `SxspBuildActCtxData` 셋 — 별칭이 C++ 꾸민 이름 하나뿐이고 `pdb_aliases.py` 로 무리가 아님을 확인했다. "있나" 검사 (`0x12a02c`) 는 이름 대신 하는 일로 적었다.

## Q87 닫힘

- 질문 (대기열 Q87): "부모 `BasepInitAppCompatData` 본문 — 부모가 자식 PEB `pShimData` · `SystemDefaultActivationContextData` 를 쓰나?"
- 답 `[정적]`:
  - **`BasepInitAppCompatData` 는 kernel32 에 있다** (rva `0x78e40`, 123B). kernelbase `CreateProcessInternalW +0x5976/+0x597d` 가 지연 import 로 부른다. 하는 일: `0x188`B 구조체를 0 으로 채우고 (`+0x2f memset`) 인자 셋을 넣어 (`+0x45 ~ +0x55`) **`g_CompatCacheServiceApi(0xa, 구조체)` 한 번** (`+0x5c`). **PEB 를 쓰지 않는다.**
  - **`pShimData`**: 부모가 쓴다 — `CreateProcessInternalW +0x57d0/+0x57d7` (PEB `+0x2d8`, 8B) · `+0x582a/+0x5831` (WOW64 PEB `+0x1e8`, 4B). 값은 `+0x574e/+0x5755` 로 자식에 받은 영역, 내용은 `+0x578e/+0x5795` 가 `BasepGetAppCompatData` 결과를 복사.
  - **`SystemDefaultActivationContextData` · `ActivationContextData`**: 부모는 안 쓴다. **csrss 만 쓴다** (위 표 4 · 5 · 11 · 12).
- 그래서 Q83 (동적) 의 "누가 쓰나" 는 답이 나왔고, 남은 건 "실제 순간 · 값 확인" 이다 (아래).

## 정정

- **run45 1장 일 37** (`runs/03_sprint/run45-work-list.md:151`): 사슬에 `+0x597d BasepInitAppCompatData` 를 "자식 메모리에 써 넣기" 와 같이 넣었다 → `BasepInitAppCompatData` 는 **kernel32** 함수이고 자식 PEB 를 안 쓴다 (`g_CompatCacheServiceApi(0xa, …)` 하나). 같은 줄의 짐작 "자식 PEB `pShimData` 후보 `[추측]`" 과 "활성화 컨텍스트 데이터는 csrss 의 SxS 가 쓸 수도" 는 **맞았다** (`+0x57d0` · sxssrv). run45 문서 맨 위에 정정 한 줄을 넣었다.
- **이름 위치**: 요청서 sast/84 · 대기열 Q87 · 짐 지도 7.6 이 쓴 "KERNELBASE `BasepInitAppCompatData`" 와 `BasepConstructSxsCreateProcessMessage` 의 모듈은 **kernel32** 다 (kernelbase 는 지연 import 로 부를 뿐). 짐 지도 7.6 은 고쳐 썼다. 요청서는 고치지 않는다 (결과서가 정정). plan.md Q87 행 이름은 오케스트레이터 몫.
- flow.md 2.2 의 `[추측]` "앱 호환 데이터 쓰기 중 하나가 자식 PEB `+0x2d8` 에 주소를 쓴다" (run14) 는 틀린 게 아니라 **`[정적]` 으로 올림** (`+0x57d0/+0x57d7`). run14 문서 (02_sprint) 는 안 고친다.

## flow.md · 짐 지도에 반영한 것

- flow.md:
  - 기준: sxssrv · sxs pdb 줄 새로, kernelbase · kernel32 · basesrv pdb 줄에 run87 몫, 근거 로그 run87 줄.
  - 그림 1: KERNELBASE `CreateProcessInternalW` 가 부르는 대표 지점에 **`KERNEL32!BasepConstructSxsCreateProcessMessage` (`+0x445f`) · `ntdll!NtWriteVirtualMemory` (`+0x57d7`, `pShimData`)** 두 상자를 더함 (`(정적)` 점선, 번호 (1) ~ (5) 로 다시 매김). 노란 메모 (csrss) 에 sxssrv 의 PEB `+0x308` · `+0x2f8` 쓰기.
  - 2.2: 전체 순서 트리 · `CreateProcessInternalW` 절 (② SxS 메시지 · ③ `pShimData` `[추측]` → `[정적]` · kernel32 지연 import) · run44 사진 줄 (쓴 쪽 닫힘) · csrss 절 (트리에 sxssrv · sxs 사슬, 새 글머리).
  - 4장: run44 (2.2 짐 지도) 줄 · run45 일 목록 줄에 해결 표시, run87 새 줄 셋 (동적 Q104 · Q83 · Q162 / 정적 Q164 / PE_emulator · 보탠 것 · 정정). 4.1: 그림 1 KERNELBASE 줄 (번호 (1) ~ (5), 새 상자 둘의 근거 · 확인할 것) · 노란 메모 (csrss) 줄 (sxssrv 사슬).
  - 5장: run 표 · 03_sprint 요청서 번호 run87 줄.
- 짐 지도 (짐 지도 바퀴 — **새 짐 칸 3**): 3.1 PEB 에 **`pShimData` (`+0x2d8`) · `ActivationContextData` (`+0x2f8`) · `SystemDefaultActivationContextData` (`+0x308`) 세 줄** (쓴 주인 nt — 놓는 법 `syscall (다른 프로세스가 부름)`: 부모 KERNELBASE · csrss sxssrv), 3장 줄임 표 `①B…` · `①X…` 새로, 2장 PEB nt 38 → 41 · 모름 68 → 65 (유저 합 · 구조체 합 · 모르는 칸 셈), 1장 PEB 줄, 4장 짝 2줄 (문맥 뷰 ↔ csrss 섹션 · `pShimData` 영역 ↔ VAD), 5장 ① `K3~P0` 줄, 7.2 PEB (`K3~P0` 2 → 0, `끝까지 0` 64 → 63), 7.6 이름 정정, 7.8 부모 쪽 칸 해결 표시, 8장 두 표, 기준 run87 줄 · 판 목록 (마흔셋째판).

## 아직 확인 필요

동적 — 결과서 "동적으로 확인할 것" 번호 그대로.

- **test.exe 로 볼 수 있는 것 (1 ~ 5 · 7) → 대기열 Q104**, 그중 3 은 **Q83**:
  - 1 블롭 덤프: 자식 `PEB+0x308` (test.exe 는 이것만) 이 가리키는 곳의 `Actx` 머리 · 전체 크기만큼. → run88 (sast/85) 이 사진 덤프를 **오프라인으로** 읽어 다룸 — 라이브 대조는 남음.
  - 2 `!address` / `!vad` 로 두 보기의 크기 · 보호 · 유형. → test.exe 몫은 run44 사진 VAD 로 이미 `[관찰]` (위 보탠 것 ③). 남은 건 `NtMapViewOfSection` 인자 (`0x400000`, 공유 1, 보호 2) 를 그 줄에서 직접 보는 것.
  - **3 PEB 쓰기 순간 → Q83** (Q83 의 질문 "누가 쓰나" 는 이번 정적으로 답이 나왔다 — 부모 `+0x57d0` · csrss sxssrv. **남은 건 실제 순간 · 값 확인**): csrss 문맥에서 `sxssrv!BaseSrvSxsDoSystemDefaultActivationContext+0x211` (rva `0x1651`) 의 `rdx` = 자식 `PEB+0x308`, 부모 문맥에서 `KERNELBASE!CreateProcessInternalW+0x57d0` 의 `rdx` = 자식 `PEB+0x2d8`, 둘의 앞뒤. (`FromStructEx+0xb09` (rva `0x3c79`) 는 test.exe 에선 안 서야 맞다.)
  - 4 빠른 길인지: 부모 `CreateProcessInternalW+0x445f` 직후 `dd @rsp+0xc58 L2` → SxS Flags `0x40`, 메시지 `+0x7c` 비트 13.
  - 5 csrss 가 SxS 를 부르는 조건 (`InternalBaseSrvCreateProcess +0xca ~ +0xee`): 첫 인자 (`esi`) · 부모 CSR_PROCESS `[+0x5c]` 비트 `0x2000`, test.exe 때 `+0xfb` 가 타나.
  - 7 test.exe `PEB+0x2f8` 이 0 인지 → 값은 run44 사진 8장 (null) 로 이미 `[관찰]`. 남은 건 `InternalSxsCreateProcess +0x40c JZ` 로 빠졌는지 (Probe 결과).
  - 추천 멈춤점 (결과서): csrss 안 `sxssrv!BaseSrvSxsCreateProcess` (rva `0x1cd0`) 입구 — `r8` = 메시지, `[r8+0x78]` SxS Flags, `[r8+0x7c]` 매개변수 Flags, `[r8+0x240]` PEB, `[r8+0x60]` CreationFlags. csrss 문맥이 필요하다.
- **test.exe 아닌 프로세스 (6) → 대기열 Q162** (notepad 류 · GUI 샘플 행): comctl32 6.0 이 어느 버전 폴더로 묶이나. run88 (sast/85) 오프라인 덤프로 `.9278` 이 나왔다 — **남은 건 라이브 확인뿐**.

정적 — 다음 바퀴 후보, 대기열 **Q164**:

- ① sxs 블롭 **쓰는 쪽** — `SxspBuildActCtxData` · `SxspAddRootManifestToActCtxGenCtx` 가 채우는 조립 명부 · 조립 정보 · 문자열 영역 칸 (결과서 후보 2). run88 (sast/85) 이 다룬 것 (로더가 읽는 칸 · DLL 구역 쓰는 쪽 · 블롭 안 폴더 칸) 은 빼고 남은 것만.
- ② "6.0.0.0 요청 → 6.0.26100.x" 를 고르는 **코드 길** (`SxspParseApplicationPolicy` · 정책 폴더). 고른 값 자체는 run88 (sast/85) 이 다룸.
- ③ 작은 `[공백]` 넷: 부모 CSR_PROCESS `[+0x5c]` 비트 `0x2000` 의 뜻 (`InternalBaseSrvCreateProcess +0x108 ~ +0x10c` 이 `CsrCreateProcess` 에 넘기는 값), 메시지 데이터 `+0x24` (`[RSP+0xc44]`, VDM 쪽 `[추측]`), CreationFlags 비트 31 (SxS 건너뜀) 을 켜는 쪽 (`CreateProcessInternalW` 엔 없음), SxS 가 실패했을 때 부모 `CreateProcessInternalW` 가 받는 값 · 처리 (위 보탠 것 ②).
- (결과서 후보 1 = Q151b 는 run88. 후보 4 = PE_emulator 쪽 확인거리라 맨 위 "PE_emulator 쪽에서 쓸 것" 에 둠.)

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음. 새 함정 없음.
- **디버거가 붙어서 달라지는 점**: 이번 줄기에서 디버거 여부를 보는 줄은 부모 `CreateProcessInternalW` 의 디버그 플래그 갈래 (`+0x81a DbgUiConnectToDbg`, run14) 뿐이고 SxS · 앱 호환 길과는 무관하다 `[정적: 읽은 줄 안에서]`.
- 도구 (결과서): 중간에 사용량 한도로 한 번 멈췄다 이어서 끝냄 (`.gpr` · `_index.tsv` · `SetPdb:` 다시 확인, 다시 analyze 안 함). `pdb_struct.py` 가 `_SECTION_IMAGE_INFORMATION` 을 못 찾음 — ntdll PDB 기준이라 그렇고, ntoskrnl PDB 엔 있다 (위 보탠 것 ⑤, 짐 지도 3.23). kernelbase 가 kernel32 를 **지연 import** 로 부르는 함수는 `_edges.tsv` 에 `external` 로만 찍힌다 — 본문을 보려면 kernel32 프로젝트를 열어야 한다.
- 도구 (flow-writer): 지연 import 로 부르는 함수가 어느 모듈인지는 kernelbase `_index.tsv` 의 `DelayLoad_<이름>` 스텁과 kernel32 `_index.tsv` 를 같이 보면 바로 갈린다.

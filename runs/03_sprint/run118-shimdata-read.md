# 2026-11-16 (작성일) run118 — ntdll 이 PEB `pShimData` 를 **읽는** 쪽 (Q90 남은 몫), 정적 + 사진 덤프 오프라인 읽기: PEB 에서 읽는 줄 **9곳 · 함수 6**, test.exe 에서 갈래를 **타는 것은 `SbUpdateSwitchContextBasedOnDll` 하나** (모듈 `SwitchBackContext` · `SbDynamicContextDllCount +4`), 블록 (`0x11c0`B, 0 아닌 바이트 74) 은 P0 ~ P2 **한 바이트도 안 바뀜**, sast/40 D1 (`+0x11aa` · `+0x11b2`) 은 **둘 다 0 → NT 힙** 으로 닫힘

(글의 오프셋은 call 자리 · 읽는/쓰는 줄 자리. "호출 관계" 나무의 callee 앞 오프셋은 ret 자리 (콜스택 표시) — 둘 다 아래 "정적 — 호출 지점" 표.)

> **지도 범위**: 그림은 안 바꿨다. 이번에 본 읽는 줄은 모두 이미 있는 대표 지점 (`LdrpInitializeProcess` · `RtlInitializeHeapManager` 상자 `p2h` · 2.5 알림 줄 `SbUpdateSwitchContextBasedOnDll`) **안쪽** 이고, 새 대표 지점 · 새 호출 관계는 없다. 글 (2.2 · 2.4 · 2.5 · 2.6) 과 짐 지도에만 넣었다.

> **새 태그 `[덤프 오프라인]`**: VMware 사진 (run44, dast/03) 을 바꾼 `memory.dmp` 를 **서버에서 스크립트로 읽은 바이트**. 디버거로 그 순간을 멈춰 본 `[관찰]` 과 다르다 (가상 → 물리 번역을 우리 스크립트가 했다). flow.md 표시 규칙 · 짐 지도 쓰는 말에 한 줄씩 정의했다. run88 은 같은 종류를 오케스트레이터 지시로 `[관찰]` 로 썼는데, 그 판은 그대로 둔다 (아래 "정정" 참고).

## 요약

- **읽는 곳 전수** `[정적]` — ntdll `.text` 의 `[reg+0x2d8]` 28줄 중 PEB 에서 온 것은 **9줄 · 함수 6** (나머지 19줄은 스택 · 다른 구조체):
  1. `LdrpInitializeProcess +0x69f` — 지역 칸 `[RSP+0x180]` 에 복사만 (WOW64 면 건너뜀). 그 복사본을 `+0x19cd` 에서 다시 꺼내 **`+0x19dc` 블록 `+0x210`** 을 본다 (0 이 아니면 shim 엔진).
  2. `SbObtainTraceHandle +0x22` — SwitchBack 문맥 (ctx = 블록 `+0x7e0`) 의 ETW 켜짐 칸 (`[ctx+0xc]`).
  3. `SbGetCurrentSwitchContext +0x9` — ctx 주소를 돌려주는 작은 함수 (블록 0 이거나 `[ctx+0x30]` 0 이면 0).
  4. `SbSelectProcedure +0x5f` · `+0x8f` · `+0x140` · `+0x199` — 같은 ctx 로 OS 버전별 함수 고르기.
  5. `RtlpHpOptIntoSegmentHeap +0x184` — 블록 `+0x11b2` 비트 `0x4` · `+0x11aa` 비트 `0x40` (세그먼트 힙).
  6. `LdrpCheckComponentOnDemandEtwEvent +0x6c` — DLL 열기가 접근 거부일 때만 (블록 `+0x1178` 에 이벤트 핸들을 잠깐 씀).
- **test.exe 에서 갈리는 것** (`[정적]` 갈래 + `[덤프 오프라인]` 블록 값):
  - 블록 `+0x210` = 0 → shim 엔진 (`LdrpInitShimEngine`) 안 부름.
  - `[ctx+0xc]` = 0 → ETW 등록 안 함.
  - `+0x11b2` = 0 → 세그먼트 힙 조건 ④ 건너뜀 → 결국 **NT 힙** (run46 사진과 같음). **sast/40 D1 · plan Q91 의 D1 닫힘.**
  - COD 길 · `SbSelectProcedure` 는 부르는 길 자체를 안 탐.
  - **타는 것은 하나**: 모듈마다 `SbUpdateSwitchContextBasedOnDll` 이 ctx 를 보고 모듈 `+0xc0` (`SwitchBackContext`) 와 `SbDynamicContextDllCount +4` 를 쓴다 (run113 · run114 가 본 칸). ctx 를 **바꾸는** 갈래는 안 탄다 — test.exe 문맥이 이미 가장 낮은 칸 (표 번호 0, 6.0).
- **블록 꼴**: 부모 kernel32 `BasepGetAppCompatData` 가 `0x11c0`B 를 만들고 (머리표 `0xac0dedab`), `SbPrepareSwitchContext` → `SbpCreateSwitchContext` 가 ctx 를 채운다. 부모 kernelbase `CreateProcessInternalW` 가 자식에 2쪽 받아 복사 (run87). ntdll 이 읽는 칸 중 **쓰는 줄을 못 찾은 4칸** (`+0x210` · `+0x11a8` · `+0x11aa` · `+0x11b2`) 은 `[공백]` — 모두 test.exe 에서 0.
- **PE_emulator**: `pShimData` 를 0 으로 두면 ① · ② · ⑤ · ⑥ 은 같은 결과, **`SbUpdateSwitchContextBasedOnDll` 만 다름** (`+0x32` 에서 끝 → 모듈 `+0xc0` · 개수 칸 0). 지금 에뮬레이터가 두 칸을 어떻게 두는지 **확인 필요**.
- 짐 지도: **2장 숫자 그대로.** 3.1 (`pShimData` 읽는 쪽 · `AppCompatInfo` 조건 `[추측]` → `[정적]`), 3.5 · 3.8 (SwitchBack 칸 게이트), 새 **7.6.15** (블록 칸 표, 체크리스트 밖), 7.9 D1 닫힘, 새 7.49.

## 환경

- 26100.9457, **정적 + 사진 덤프 오프라인 읽기** (VM 실행 없음).
- 정적: **ntdll pdb guid_age `C3093720177DA6851519DCFFE8DF53FE1`** (기준과 같음). kernel32 · kernelbase 는 이미 있던 프로젝트에서 `.asm` 만.
- 버전 대조 (결과서): `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` · `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal` 일치.
- 사진: run44 (dast/03) 짐 사진 `ms/dump/03_baggage-snap/{P0,P1,P1a,P1b,P2}/memory.dmp` (커밋 안 함). test.exe `DirectoryTableBase` `0x192c2e000` (`ms/logs/04_baggage-read_P0.txt:333`) — 이번 부팅 기준.
- **sast/116 결과만 넣었다.** sast/114 · 115 · 117 ~ 120 은 안 넣었다 (다음 바퀴 몫).

## 목적

- **Q90 남은 몫**: ntdll 이 PEB `pShimData` (`+0x2d8`) 를 **읽는** 곳 전수, 블록 안 어느 칸을 보고 무엇을 하나 (문맥 쪽 `+0x2f8` · `+0x308` 은 run88 로 닫힘).
- 블록 칸 표 (ntdll 이 읽는 칸 + 부모가 쓰는 줄) 와 test.exe 블록 값으로 갈래별 결과. 짐 지도 3.1 · 7.6 · 7.9 로.

## 방법

- 정적: `_workspace/sast/116_shimdata-read.md` — ntdll · kernel32 (kernelbase 읽기만).
  - 새로 뽑음 (depth 0): ntdll `^(SbGetCurrentSwitchContext|LdrpIsCODServiceEnabled)$` · `^SbpDetermineDllContext$` (3개), kernel32 `^(BasepGetAppCompatData|SbPrepareSwitchContext|SbpCreateSwitchContext|SbpIsTraceEnabled)$` (4개 — 요청서는 "읽기만" 이었는데 블록을 만드는 함수 `.asm` 이 없어서 대조용으로 넓힘).
  - 읽기만 (있던 것): `LdrpInitializeProcess` · `SbObtainTraceHandle` · `SbSelectProcedure` · `SbUpdateSwitchContextBasedOnDll` · `SbpUpdateCache` · `RtlpHpOptIntoSegmentHeap` · `RtlInitializeHeapManager` · `LdrpCheckComponentOnDemandEtwEvent` · `LdrpMapDllNtFileName` · `LdrpInitShimEngine`, kernelbase `CreateProcessInternalW` · `CreateFileInternal`.
  - 읽기 전수: 스크래치 `objdump` (ntdll `.text` 전체) 에서 `+0x2d8]` 꼴 28줄 → 앞줄로 PEB 에서 온 것만 거름 → 함수 이름은 `_index.tsv`.
- 사진 오프라인 읽기 (결과서 방법 — 아래 "4."): PAGEDU64 물리 구역 + 4단 페이지 표 번역으로 블록 `0x25996d10000` 2쪽 (`0x2000`B) 을 다섯 장에서 읽음.
- 결과서: `_workspace/sast/116_shimdata-read_result.md` (**분석성공** — 스크립트 문제 없음).
- flow-writer 가 다시 맞춘 것: 아래 "정적 — 호출 지점" 표를 `_calls.tsv` 로, 읽는 줄 · 갈래 줄을 `.asm` 으로 (`LdrpInitializeProcess +0x5d9 · +0x5db · +0x696 · +0x69d · +0x69f · +0x6a6 · +0x19cd ~ +0x19f4 · +0x1ca7 · +0x1cae · +0x1cd4`, `RtlpHpOptIntoSegmentHeap +0x66 · +0x184 ~ +0x1b5`, `SbGetCurrentSwitchContext` 전부, `SbObtainTraceHandle +0xf · +0x22 · +0x2e · +0x37 · +0x44 · +0x47 · +0x49 · +0x5c · +0xad`, `SbUpdateSwitchContextBasedOnDll +0x32 · +0x47 · +0x4a · +0xb5 · +0xd5 · +0xd9 · +0xfb · +0x10f · +0x12c · +0x1c6 ~ +0x1f6 · +0x202 · +0x2a0 · +0x2ba`, `SbSelectProcedure +0x5f · +0x8f · +0x140 · +0x199`, `LdrpCheckComponentOnDemandEtwEvent` 전부, kernel32 `BasepGetAppCompatData +0x31 · +0xb2 · +0xc0 · +0xce · +0xdc · +0x159 · +0x182`, `SbpCreateSwitchContext +0x1a2 · +0x1a9 · +0x1b4 · +0x1c1`). 덤프 블록 파일 (스크래치 `s116/blk_*.bin`) 다섯 장 md5 가 같은지와 칸 값도 다시 셈.
- 앞 바퀴 짝: run87 · run88 (SxS — 부모가 `pShimData` 를 쓰는 쪽 · 문맥 쪽 Q90), run46 (sast/40 D1), run113 (`SwitchBackContext` `+0xc0`), run114 (`SbDynamicContextDllCount +4`).

## 결과

비유 (`pShimData` 블록 = 본점이 새 가게에 넣어 준 **"호환 메모 봉투"**):

- 본점 (부모 cmd) 이 가게를 열기 전에 봉투를 하나 넣어 준다. 봉투 안엔 "이 가게는 어느 시절 규칙으로 돌아가나" 같은 메모가 칸마다 있다.
- 새 가게 직원 (ntdll) 은 봉투를 **여섯 군데서** 열어 본다. 그런데 test.exe 봉투는 거의 비어 있다 (8,192 바이트 중 74 바이트만 글씨).
- 그래서 "특별 규칙 엔진 켜기 (shim)", "기록 남기기 (ETW)", "새 창고 방식 (세그먼트 힙)" 은 모두 "해당 없음" 으로 지나간다.
- 딱 하나, 짐 (DLL) 이 들어올 때마다 "이 짐은 몇 년식 규칙인가" 를 짐 꼬리표에 붙이고 그 연식 칸에 개수를 센다. 봉투가 아예 없으면 이 꼬리표 붙이기를 통째로 건너뛴다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`, kernel32 · kernelbase 는 각 `_calls.tsv`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x9ed` | `+0x9f2` | `RtlInitializeHeapManager` | 늘 (run46) | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x28` | `+0x2d` | `RtlpHpOptIntoSegmentHeap` | 늘. 안에서 `+0x184` `pShimData` 읽기 | [정적] |
| `ntdll!RtlInitializeHeapManager` | `+0x56` | `+0x5b` | `SbSelectProcedure` | `+0x2f JZ +0x6c` — 앞 반환 0 이면 건너뜀 (test.exe 안 탐) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x19c8` | `+0x19cd` | `SbObtainTraceHandle` | 늘 (인자 0) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x19ef` | `+0x19f4` | `LdrpInitShimEngine` | 복사본 ≠ 0 · 블록 `+0x210` ≠ 0 일 때만 (`+0x19da` · `+0x19e3 JZ +0x19f4`) — test.exe 안 탐 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1aed` | `+0x1af2` | `LdrpPrepareModuleForExecution` | → `+0x76/+0x7b LdrpNotifyLoadOfGraph` → `+0x20/+0x25 LdrpSendPostSnapNotifications` | [정적] (run66) |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xa3` | `+0xa8` | `SbUpdateSwitchContextBasedOnDll` | 모듈마다 늘 (run66 · run113) | [정적] |
| `ntdll!SbUpdateSwitchContextBasedOnDll` | `+0x14` | `+0x19` | `SbGetCurrentSwitchContext` | ctx 받기. `+0x32 JZ +0x39` — 0 이면 끝 | [정적] |
| `ntdll!SbUpdateSwitchContextBasedOnDll` | `+0xc1` | `+0xc6` | `SbpDetermineDllContext` | `[ctx+8]` ≠ 0 (`+0x47` · `+0x4a`) 일 때. 뒤 `+0xd9` 모듈 `+0xc0` | [정적] |
| `ntdll!SbUpdateSwitchContextBasedOnDll` | `+0x205` · `+0x297` | `+0x20a` · `+0x29c` | `SbObtainTraceHandle` | ctx 바꾸는 갈래만 (`+0x1ea` 잠금 뒤) — test.exe 안 탐 | [정적] |
| `ntdll!SbpDetermineDllContext` | `+0x5f` · `+0xc3` | `+0x64` · `+0xc8` | `RtlImageNtHeaderEx` · `SbpRetrieveCompatibilityManifest` | DLL PE 머리 · 호환 매니페스트 | [정적] (새로) |
| `ntdll!SbObtainTraceHandle` | `+0x83` · `+0xec` | `+0x88` · `+0xf1` | `EtwNotificationRegister` · `SbpTraceContextUpdate` | `[ctx+0xc]` ≠ 0 일 때만 — test.exe 안 탐 | [정적] |
| `ntdll!LdrpMapDllNtFileName` | `+0x4d7` | `+0x4dc` | `LdrpCheckComponentOnDemandEtwEvent` | `+0x4c0 CMP EAX,0xc0000022` (접근 거부) · `R13D` 0 일 때만 | [정적] |
| `ntdll!LdrpCheckComponentOnDemandEtwEvent` | `+0x56` | `+0x5b` | `CompatCachepLookupCdb` | 0 이면 `+0x5d JZ` 끝 | [정적] |
| `ntdll!LdrpCheckComponentOnDemandEtwEvent` | `+0x7c` | `+0x81` | `LdrpIsCODServiceEnabled` | `pShimData` ≠ 0 (`+0x73` · `+0x76`) 일 때 | [정적] (새로) |
| `ntdll!LdrpCheckComponentOnDemandEtwEvent` | `+0x9d` · `+0x126` · `+0x141` · `+0x14f` | `+0xa2` · `+0x12b` · `+0x146` · `+0x154` | `NtCreateEvent` · `EtwEventWriteNoRegistration` · `NtWaitForSingleObject` (10초) · `NtClose` | COD 길 (test.exe 안 탐으로 보임) | [정적] |
| `kernelbase!CreateProcessInternalW` | `+0x56bb` | `+0x56c2` | `kernel32!BasepGetAppCompatData` | 부모 쪽 블록 만들기 | [정적] (run87) |
| `kernelbase!CreateProcessInternalW` | `+0x574e` · `+0x578e` · `+0x57d0` | `+0x5755` · `+0x5795` · `+0x57d7` | `NtAllocateVirtualMemory` · `NtWriteVirtualMemory` ×2 | 자식에 2쪽 받기 · 블록 복사 · PEB `+0x2d8` | [정적] (run87) |
| `kernel32!BasepGetAppCompatData` | `+0x8f` | `+0x96` | `RtlAllocateHeap` | 블록 `0x11c0`B (`+0x31`) | [정적] (새로) |
| `kernel32!BasepGetAppCompatData` | `+0x261` | `+0x266` | `SbPrepareSwitchContext` | → `+0xc0/+0xc5 SbpCreateSwitchContext` (ctx 채움, `+0x193/+0x198 SbpIsTraceEnabled`) | [정적] (새로) |

### 호출 관계

```
(부모 cmd) kernelbase!CreateProcessInternalW
 ├─ +0x56c2  kernel32!BasepGetAppCompatData            블록 0x11c0B (+0x208 크기 · +0x20c 0xac0dedab · +0x214 Machine · +0xb38 부모 PID · +0xb3c 부모 경로 · +0x1144)
 │            └─ +0x266  SbPrepareSwitchContext
 │                        └─ +0xc5  SbpCreateSwitchContext    ctx = 블록 +0x7e0 (+0x1a2 · +0x1a9 · +0x1b4 · +0x1c1 …)
 ├─ +0x5755  NtAllocateVirtualMemory (자식) → +0x5795 NtWriteVirtualMemory (블록) → +0x57d7 NtWriteVirtualMemory (PEB +0x2d8)   (run87)

(test.exe) ntdll!LdrpInitializeProcess
 │  +0x69f   PEB +0x2d8 읽기 → +0x6a6 [RSP+0x180] 에 복사 (WOW64 면 +0x69d 에서 건너뜀, 미리 +0x5db 에서 0)
 ├─ +0x9f2   RtlInitializeHeapManager
 │            ├─ +0x2d  RtlpHpOptIntoSegmentHeap      +0x184 PEB +0x2d8 · +0x190 [+0x11b2]&4 · +0x199 [+0x11aa]&0x40   → test.exe +0x197 에서 ⑤ 로 → 0 (NT 힙)
 │            └─ +0x5b  SbSelectProcedure             +0x2f JZ 로 안 탐
 ├─ +0x19cd  SbObtainTraceHandle                      +0x22 PEB +0x2d8 → ctx · +0x44 [ctx+0xc] = 0 → +0x49 0 반환
 │  +0x19cd ~ +0x19e3  복사본 → [+0x210] = 0 → +0x19f4 로 (shim 안 켬, +0x19e5 AppCompatInfo 쓰기 안 탐)
 ├─ +0x19f4  LdrpInitShimEngine                       (안 탐)
 └─ +0x1af2  LdrpPrepareModuleForExecution
              └─ +0x7b  LdrpNotifyLoadOfGraph
                         └─ +0x25  LdrpSendPostSnapNotifications   (모듈마다)
                                    └─ +0xa8  SbUpdateSwitchContextBasedOnDll
                                               ├─ +0x19  SbGetCurrentSwitchContext   +0x9 PEB +0x2d8 → ctx (+0x1d [ctx+0x30] ≠ 0)
                                               │  +0x47 [ctx+8] = 1 → 계속
                                               ├─ +0xc6  SbpDetermineDllContext (DllBase) → +0xd9 모듈 +0xc0 ← 표 칸 +4 · +0xfb SbDynamicContextDllCount+4 +1
                                               │  +0x1c6 ~ +0x1cb  DLL 칸 번호 ≥ 지금 칸 번호 → 안 바꿈 (test.exe: 지금 칸 = 번호 0)
                                               └─ (+0x1ea 잠금 · +0x1f6 · +0x202 · +0x20a · +0x29c · +0x2a0 — ctx 바꾸는 갈래, 안 탐)

ntdll!LdrpMapDllNtFileName
 └─ +0x4dc  LdrpCheckComponentOnDemandEtwEvent       접근 거부 (0xc0000022) 때만. +0x6c PEB +0x2d8 · +0xce [+0x1178] ← 이벤트 · +0x154 [+0x1178] = 0
```

### 1. 읽는 곳 전수 — 결과서 질문 1 `[정적]`

| # | 읽는 줄 | 읽은 블록으로 하는 일 | 사진 구간 | test.exe |
|---|---|---|---|---|
| 1 | `LdrpInitializeProcess +0x69f` (`RDI` = PEB, 앞 `+0x696 CMP UseWOW64` · `+0x69d JNZ +0x6ae`) | 지역 `[RSP+0x180]` 에 복사만 (`+0x6a6`, 미리 `+0x5d9` · `+0x5db` 0) | P1 ~ P1a | 탐. 복사 값 `0x25996d10000` |
| 2 | 같은 함수 `+0x19cd MOV R13,[RSP+0x180]` → `+0x19d7 TEST` · `+0x19da JZ` → **`+0x19dc CMP [R13+0x210],0`** · `+0x19e3 JZ +0x19f4` | `+0x210` ≠ 0 이면 `+0x19e5` PEB `AppCompatInfo` (`+0x2e0`) = 0 · `+0x19ef LdrpInitShimEngine(블록)`. 또 `+0x1cd4 MOV RDX,R13` 가 `g_pfnSE_InstallAfterInit` 인자 (`+0x1ca7 CMP g_ShimsEnabled` · `+0x1cae JZ +0x1d07` — 켜졌을 때만) | P1a ~ P1b · `+0x1cd4` 는 P1b ~ P2 | `+0x210` = 0 → 안 탐 |
| 3 | `SbObtainTraceHandle +0x22` (`+0xf GS:[0x60]`) → `+0x2e ADD RBX,0x7e0` · `+0x37 CMP [RBX+0x30]` | `[ctx+0xc]` (`+0x44` · `+0x47 JNZ +0x5c`) ≠ 0 이면 `MS_Windows_AeSwitchBack_Provider` 를 `EtwNotificationRegister` 로 등록 → 핸들 `[ctx+0x10]` (`+0xad LOCK CMPXCHG`) · `SbpTraceContextUpdate`. 아니면 `+0x49` 0 | P1a ~ P1b | `[ctx+0xc]` = 0 → 0 반환 (쓰기 · 시스템 콜 0) |
| 4 | `SbGetCurrentSwitchContext +0x9` (`+0x0 GS:[0x60]`) · `+0x15 ADD RAX,0x7e0` · `+0x1d CMP [RAX+0x30],0` | ctx 주소 돌려줌 (없으면 `+0x25` 0). 부르는 곳: `SbUpdateSwitchContextBasedOnDll +0x14` · `SbpUpdateCache +0x3e` · (`LdrShutdownProcess +0x1d6` — 종료, 범위 밖) | P1a ~ P1b (모듈마다) | ctx 있음 → **탄다** (아래) |
| 5 | `SbSelectProcedure +0x5f` · `+0x8f` · `+0x140` · `+0x199` (모두 `GS:[0x60]` → `+0x2d8`) | `[ctx+0x30]` 없으면 `RtlGetVersion` + `SbSupportedOsList` 로 고름, 있으면 `[ctx+0]` 를 캐시와 견줘 다르면 `SbpUpdateCache` | 초기화 범위 안 부르는 곳은 `RtlInitializeHeapManager +0x56` 하나 | **안 탐** (`+0x2f JZ`) |
| 6 | `RtlpHpOptIntoSegmentHeap +0x184` (`+0x66 MOV R14,GS:[0x60]`) · `+0x18b TEST` · `+0x18e JZ +0x1aa` | `+0x190 TEST [+0x11b2],4` · `+0x197 JZ +0x1aa` → 있으면 `+0x199 TEST [+0x11aa],0x40` · `+0x1a0 JZ +0x281` (0 반환, NT 힙) / 켜져 있으면 `+0x1a6` · `+0x1b5` 1 반환 (세그먼트 힙). 비트 4 없으면 `+0x1aa RtlGetSuiteMask` (sast/40 ⑤ ~ ⑨) 로 | P1 ~ P1a | `+0x11b2` = 0 → `+0x197` 에서 ⑤ 로 → NT 힙 |
| 7 | `LdrpCheckComponentOnDemandEtwEvent +0x6c` (`+0x63 GS:[0x60]`) · `+0x73 TEST` · `+0x76 JZ` | 블록 `+0x1178` 에 이벤트 핸들을 **씀** (`+0xce`), ETW (`UserLoaderGuid` · `ComponentOnDemand`) 쓰고 10초 기다림 (`-0x5f5e100`), `+0x154` 에서 `+0x1178` = 0 | `LdrpMapDllNtFileName +0x4d7` 에서만 | 안 탐으로 보임 (접근 거부 없음 `[추측]` — 동적) |

- 범위 밖 읽는 곳 (이름만): kernelbase `CreateFileInternal +0x61f` (`[RCX+0x2d8]`) · `+0x2f5 SbSelectProcedure(…, g_SbModuleTable_KernelBase, …)` — 초기화 동안 불리는지는 모름 (동적). 부모 `CreateProcessInternalW +0x56f4` 는 PEB 가 아니라 자기 로컬 블록 `+0x11a8` (ARM64 exe 일 때만). shim 엔진 본문 (apphelp `SE_*`) 은 블록 포인터를 통째로 받는다 (`LdrpInitShimEngine +0x13f MOV R8,RDI` · `+0x160` CFG 호출) — 이름만.
- PEB 아닌 `+0x2d8` 19줄 (결과서 목록 그대로): 스택 `[rsp+0x2d8]` 8줄 (`LdrpInitializeProcess +0x1349 · +0x1a67 · +0x1ade · +0x1ba7 · +0x1bcc` 등) · 다른 구조체 11줄 (활성화 문맥 스택 프레임 · 스택 추적 DB · Xpress 풀기 · 스냅샷 복사).

**탄다 — `SbUpdateSwitchContextBasedOnDll` 안 (모듈마다)** `[정적]`:

- `+0x14` ctx 받기 → `+0x32 JZ +0x39` (ctx 0 → 0 반환. **`pShimData` 0 이면 여기서 끝**).
- `+0x47 CMP [ctx+8],0` · `+0x4a JZ +0x2ec` (켜짐 0 → 1 반환). test.exe `[ctx+8]` = 1 → 계속.
- `+0xc1 SbpDetermineDllContext(DllBase)` (DLL PE 머리 OS 버전 + `SbpRetrieveCompatibilityManifest`) → 돌려준 표 칸 → `+0xd5 LEA RDI,[RDX+4]` · **`+0xd9 MOV [R14+0xc0],RDI`** (모듈 `SwitchBackContext`) → **`+0xfb INC.LOCK SbDynamicContextDllCount+4 + 번호×8`**.
- ctx 바꾸는 갈래: 표 칸 번호 4 (10.0) 면 `+0x10f ~ +0x126` 에서 `+0x12c` (1 반환). 아니면 `+0x1c6 MOV EAX,[R15]` · `+0x1c9 CMP [RDX],EAX` · `+0x1cb JGE +0x12c` — DLL 칸 번호가 지금 칸 번호 **이상이면 안 바꿈**. 바꾸면 `+0x1e0 CMP RCX,[RBP+0x28]` · `+0x1e4 JC` 뒤 `+0x1ea SbpContextLock` · `+0x1f6 INC [ctx]` · `+0x202` 새 GUID → `[ctx+0x40]` · `SbObtainTraceHandle` · `+0x2a0 INC [ctx]`.
- test.exe: 지금 칸 = 번호 0 (6.0, 가장 낮음) → 어떤 DLL 도 더 낮을 수 없어 바꾸는 갈래 안 탐. 블록 바이트 P0 ~ P2 같음 `[덤프 오프라인]` 과 맞음.
- `SbSupportedOsList` (ntdll rva `0x173010`, `0x20`B × 5 — 파일 값 `[정적]`, 결과서): 0 = 6.0 `{e2011457-1546-43c5-a5fe-008deee3d3f0}` · 1 = 6.1 · 2 = 6.2 · 3 = 6.3 · 4 = 10.0 `{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}`.
- 이 갈래가 돈 흔적: run113 사진 모듈 `+0xc0` (P1a null → P1b exe `0x7ffafb7d3014`) · run114 사진 `SbDynamicContextDllCount` 번호 0 칸 개수 (P1a 0 → P1b · P2 3) `[관찰]` run44.

### 2. 블록 칸 표 — 결과서 질문 2

- 블록 = `0x11c0`B (VAD 2쪽 `25996d10 ~ 25996d11` Private READWRITE, `_P0.txt:1381`). ctx = 블록 `+0x7e0`.
- 사진 열은 `[덤프 오프라인]` (P0 = P1 = P1a = P1b = P2 — 다섯 장 md5 같음). 2쪽 `0x2000`B 중 0 아닌 바이트 **74개** — 표 밖 칸은 전부 0.

| 블록 오프셋 (ctx 기준) | 크기 | ntdll 읽는 줄 | 뜻 | 부모가 쓰는 줄 (kernel32) | 사진 `[덤프 오프라인]` |
|---|---|---|---|---|---|
| `+0x208` | 4 | — | 블록 크기 `[추측]` | `BasepGetAppCompatData +0x31` (`0x11c0`) · `+0xb2` | `0x11c0` |
| `+0x20c` | 4 | — | 머리표 | `+0xc0` | `0xac0dedab` |
| **`+0x210`** | 4 | **`LdrpInitializeProcess +0x19dc`** | **shim 엔진 켤지** | 못 찾음 **`[공백]`** (apphelp 질의 결과 쪽 `[추측]`) | `0` |
| `+0x214` | 2 | — | 이미지 `Machine` `[추측]` | `+0xce` | `0x8664` |
| `+0x218 ~ +0x3df` | `0x1c8` | — | 호출자가 준 질의 결과 복사 | `+0x3be ~ +0x420` | 전부 `0` |
| **`+0x7e0`** (ctx `+0x0`) | 8 | `SbSelectProcedure +0xb5` · `SbpUpdateCache +0x72` | 바뀜 번호 (ntdll 이 올림 — 바꾸는 갈래만) | `SbpCreateSwitchContext +0x1a2` | `1` |
| **`+0x7e8`** (`+0x8`) | 4 | **`SbUpdateSwitchContextBasedOnDll +0x47`** | 켜짐 | `+0x1a9` | `1` |
| **`+0x7ec`** (`+0xc`) | 4 | **`SbObtainTraceHandle +0x44`** · `SbSelectProcedure +0x15b · +0x1b4` | ETW 켜짐 | `+0x1b4` (`SbpIsTraceEnabled` 반환) | `0` |
| `+0x7f0` (`+0x10`) | 8 | `SbObtainTraceHandle +0x5c` · `SbSelectProcedure +0x21a` | ETW 알림 핸들 (ntdll 이 `LOCK CMPXCHG` 로 씀 — ETW 갈래만) | `+0x1c1` (0) | `0` |
| `+0x7f8` (`+0x18`) | 8 | `SbSelectProcedure +0x1f9` (`SbpUpdateCache` 인자) | 아래 덩어리 시작 (버전 수 `[추측]`) | `+0x169 ~ +0x17c` · `+0x1c9 ~` (`0x340`B 복사) | `0x0006000000000000` |
| `+0x800` (`+0x20`) | 4 | — | 부모 인자 | `+0x18b` | `3` |
| **`+0x808`** (`+0x28`) | 8 | **`SbUpdateSwitchContextBasedOnDll +0x1e0`** | DLL 버전과 견주는 기준 | `+0xd1 ~ +0xeb` (조건부) | `0` |
| **`+0x810`** (`+0x30`) | 16 | **`SbGetCurrentSwitchContext +0x1d`** · `SbObtainTraceHandle +0x37` · `SbSelectProcedure +0x7c · +0xab` · `SbpUpdateCache +0x206` | 처음 정한 호환 문맥 GUID (첫 dword ≠ 0 = "문맥 있음") | `+0xfd` · `+0x10a` | `{e2011457-1546-43c5-a5fe-008deee3d3f0}` = `SbSupportedOsList` 번호 0 (6.0) |
| **`+0x820`** (`+0x40`) | 16 | `SbUpdateSwitchContextBasedOnDll +0x55 ~ +0x82` · `SbpUpdateCache +0x6e` (읽기) · `+0x202` (ntdll 이 씀 — 바꾸는 갈래만) | 지금 문맥 GUID | 같은 줄 | 위와 같은 GUID |
| `+0x830` (`+0x50`) | 4 | — | 부모 인자 | `+0xe0` | `1` |
| **`+0x834`** (`+0x54`) | 4 | **`SbpUpdateCache +0xb3`** | 아래 GUID 표 개수 | 안 씀 (0) | `0` |
| `+0x838 ~` (`+0x58 + 0x10·i`) | 16 × n | `SbpUpdateCache +0xbf ~ +0xdb` | 앱이 지원한다고 알린 OS GUID 표 | 안 씀 | 비어 있음 |
| `+0xb38` | 4 | — | 부모 PID (`GS:[0x40]`) | `BasepGetAppCompatData +0x159` | `0x19a8` = test.exe `InheritedFromUniqueProcessId` (`_P0.txt:154` `[관찰]`) |
| `+0xb3c ~` | UTF-16 | — | 부모 이미지 경로 | `+0x182 ADD RCX,0xb3c` · `+0x18e wcsncpy_s` | `C:\WINDOWS\system32\cmd.exe` |
| `+0x1144` | 4 | — | 이미지 파일 크기 `[추측]` (`SECTION_IMAGE_INFORMATION +0x38`) | `+0xdc` | `0x86a00` = 551,424 = `sample/bin/test.exe` 파일 크기 `[소스]` |
| `+0x1148 ~ +0x1160` | 4 × 7 | — | 체크섬 · 플래그 `[추측]` | 결과서 `+0xe9` 등 7줄 | 전부 `0` |
| **`+0x1178`** | 8 | ntdll 이 **씀**: `LdrpCheckComponentOnDemandEtwEvent +0xce` · `+0x154` (0) | 임시 이벤트 핸들 (COD 길만) | — | `0` |
| `+0x11a8` 비트 7 | 1 | — (kernelbase `CreateProcessInternalW +0x56f4 ~ +0x56fd` 가 ARM64 exe 일 때 봄) | | 못 찾음 **`[공백]`** | `0` |
| **`+0x11aa`** 비트 `0x40` | 1 | **`RtlpHpOptIntoSegmentHeap +0x199`** | 세그먼트 힙 켜라 | 못 찾음 **`[공백]`** | `0` |
| **`+0x11b2`** 비트 `0x4` | 1 | **`RtlpHpOptIntoSegmentHeap +0x190`** | 위 값이 정해졌나 | 못 찾음 **`[공백]`** | `0` |

- 부모 쪽 대조 한 줄 `[정적]`: kernelbase `CreateProcessInternalW +0x56bb BasepGetAppCompatData` → (블록 `[RSP+0x2a0]`, 크기 `[RSP+0x1f8]`) → `+0x574e NtAllocateVirtualMemory` (자식) → `+0x578e NtWriteVirtualMemory` (블록) → `+0x57d0` PEB `+0x2d8` (run87). `0x11c0` 를 쪽으로 올리면 2쪽 — VAD 크기와 맞음.
- 쓰는 줄 못 찾은 4칸 (`+0x210` · `+0x11a8` · `+0x11aa` · `+0x11b2`) 은 apphelp 호환 질의 (SDB) 결과일 것 `[추측]` — shim 이 걸린 exe 에서만 켜진다. 다음 요청 후보 1.

### 3. test.exe 에서 갈리는 것 — 결과서 질문 3

| 읽기 갈래 | 블록 값 `[덤프 오프라인]` | 결과 (test.exe) | 안 탄 길 |
|---|---|---|---|
| 1 `+0x69f` 복사 | PEB `+0x2d8` = `0x25996d10000` (`_P0.txt:1635` `[관찰]`) | 복사 | — |
| 2 `+0x19dc` | `+0x210` = 0 | `+0x19e3 JZ` — `LdrpInitShimEngine` 안 부름 · PEB `AppCompatInfo` 0 그대로 (`_P0.txt:1636` `(null)` `[관찰]`) · `g_ShimsEnabled` 0 → `SE_InstallAfterInit` 안 부름 | shim 엔진 전체 (`LdrpInitShimEngine` · `LdrpLoadShimEngine` · `LdrpGetShimEngineInterface` · `SE_*`) |
| 3 `SbObtainTraceHandle` | `[ctx+0x30]` ≠ 0, `[ctx+0xc]` = 0 | 0 반환 | ETW 등록 |
| 4 `SbUpdateSwitchContextBasedOnDll` | `[ctx+8]` = 1, `[ctx+0x28]` = 0, ctx GUID = 번호 0 | **탄다** — 모듈마다 `+0xc0` · 개수 +1. 바꾸는 갈래 안 탐 | 문맥 바꾸기 (`SbpContextLock` · `INC [ctx]` · GUID 쓰기) |
| 5 `SbSelectProcedure` | — | 안 부름 (6 이 0) | 전부 |
| 6 `RtlpHpOptIntoSegmentHeap` | `+0x11b2` = 0 · `+0x11aa` = 0 | `+0x197` 에서 ⑤ 로 → 끝까지 가서 0 = **NT 힙** (사진 힙 `+0x10` = `0xffeeffee`, run46 `[관찰]`) | 세그먼트 힙 길 |
| 7 COD | `+0x1178` = 0 | 안 탐으로 보임 | 이벤트 · ETW · 10초 기다림 |

### 4. 사진 오프라인 읽기 — 방법 · 한계 (결과서 그대로 요약)

- 파일: `ms/dump/03_baggage-snap/<태그>/memory.dmp` — VMware 사진을 `vmss2core` 로 바꾼 **PAGEDU64 풀 덤프**. 머리 `+0x88` 의 물리 구역 2개 (`0 ~ 0xc0000000`, `0x100000000 ~ +0x140000000`), 자료는 `+0x2000` 부터.
- 번역: test.exe `DirectoryTableBase` `0x192c2e000` 으로 4단 페이지 표 걷기.
- 검증: 번역한 PEB `+0x10` (`0x7ff61a370000`) · `+0x2d8` (`0x25996d10000`) · `+0x308` (`0x25996d00000`) · `+0x2e0` · `+0x2f8` (0) 이 `04_baggage-read_P0.txt` · `_P1a.txt` 디버거 줄과 같다.
- 결과: 블록 2쪽 모두 물리 메모리에 있었고, **P0 · P1 · P1a · P1b · P2 바이트 같음** (P2 는 덤프 머리의 기본 `DirectoryTableBase` 가 다른 프로세스 것이라 직접 줌). K3 는 PEB `+0x2d8` null (`_K3.txt:1633`) 이라 블록이 아직 없음.
- 한계: 한 번의 실행 (run44) 값. 사진 사이에 블록을 썼다가 되돌렸다면 못 본다 (정적으로 ntdll 이 블록에 쓰는 줄은 `+0x7f0` (ETW 갈래) · `+0x7e0` · `+0x820` (바꾸는 갈래) · `+0x1178` (COD) 뿐이고 test.exe 에서 모두 안 탐).

## 확인된 것

- `[정적]` ntdll 이 PEB `pShimData` 를 PEB 로 읽는 줄은 **9곳 · 함수 6** (위 1. 표 1 · 3 ~ 7행). 2행은 1행 복사본을 다시 읽는 줄.
- `[정적]` `LdrpInitShimEngine` (`+0x19ef`) 을 부르는 게이트는 **블록 `+0x210` ≠ 0** (+ `pShimData` ≠ 0 · WOW64 아님). run66 정적 ② "shim 엔진 올리는 조건" 의 첫 단추.
- `[정적]` PEB `AppCompatInfo` 0 쓰기 (`+0x19e5`) 의 `R13` = `+0x69f` 의 `pShimData` 복사본 (`[RSP+0x180]`) — run113 의 `[추측]` "시작 때 모은 앱 호환 블록" 이 맞다.
- `[정적]` + `[덤프 오프라인]` 세그먼트 힙 조건 ④ (`pShimData` 두 바이트) 는 test.exe 에서 **둘 다 0** → `+0x197` 에서 ⑤ 로 → NT 힙. **sast/40 D1 · Q91 D1 닫힘.**
- `[정적]` + `[덤프 오프라인]` test.exe 에서 블록을 보고 갈래를 **타는 것은 `SbUpdateSwitchContextBasedOnDll` 하나**. 그 결과 칸 (모듈 `+0xc0` · `SbDynamicContextDllCount +4`) 은 run113 · run114 `[관찰]` 과 맞음.
- `[덤프 오프라인]` 블록 바이트는 P0 ~ P2 **안 바뀜** → 초기화 동안 ntdll 이 블록에 쓴 바이트 없음 (정적 쓰기 줄이 모두 안 타는 갈래라는 것과 맞음).
- `[덤프 오프라인]` ctx GUID `{e2011457-…}` = `SbSupportedOsList` 번호 0 (6.0) — 매니페스트 없는 exe 의 기본 칸 `[추측: 뜻]`. run113 모듈 `+0xc0` exe 값 (번호 0 칸 `+4`) 과 같은 칸.
- `[덤프 오프라인]` + `[관찰]` 블록 `+0xb38` `0x19a8` = test.exe 부모 PID (`_P0.txt:154`), `+0xb3c` = 부모 이미지 경로, `+0x1144` = `sample/bin/test.exe` 크기 `[소스]`.

## 결과서와 다르게 적은 곳 · 보탠 것

- **오프셋 넷 (`LdrpCheckComponentOnDemandEtwEvent`)** — `.asm` · `_calls.tsv` 로 고침: `CompatCachepLookupCdb` call 은 `+0x52` 가 아니라 **`+0x56`** (ret `+0x5b`), 블록 `+0x1178` 쓰기는 `+0xca` 가 아니라 **`+0xce`**, `EtwEventWriteNoRegistration` call 은 `+0x122` 가 아니라 **`+0x126`**, `NtWaitForSingleObject` call 은 `+0x13d` 가 아니라 **`+0x141`** (ret `+0x146` 은 같음). 뜻은 그대로. `NtCreateEvent` (`+0x9d`) · `NtClose` (`+0x14f`) 는 flow-writer 가 보탬.
- **`+0x1144` = test.exe 파일 크기**: 결과서는 `[추측]` 이름에 값만 맞췄고, flow-writer 가 `sample/bin/test.exe` 크기 551,424 B 와 맞춰 봤다 (`[소스]`, 칸 이름은 그대로 `[추측]`).
- **덤프 블록 다시 셈**: 스크래치 `s116/blk_{P0,P1,P1a,P1b,P2}.bin` md5 다섯 장 같음 · 0 아닌 바이트 74 · 표의 칸 값 (크기 · 머리표 · `Machine` · ctx · GUID 둘 · PID · 경로 · 파일 크기 · `+0x11a8 ~ +0x11b3` 모두 0) 을 flow-writer 가 다시 읽어 결과서와 같음을 봤다 (새 분석 아님 — 결과서가 뽑은 파일을 다시 셈).
- **태그**: 결과서 `[덤프 오프라인]` 을 그대로 썼다 (오케스트레이터 지시 — run88 때 `[관찰]` 로 바꾼 것과 다름).

## 정적과 동적이 다른 곳

- 없다 (새 동적 없음). 정적 갈래 ("블록에 쓰는 줄은 test.exe 에서 안 탐") 와 사진 다섯 장 블록 바이트 같음이 맞고, "탄다" 고 한 `SbUpdateSwitchContextBasedOnDll` 은 run113 · run114 사진 칸 값과 맞는다.
- **디버거가 붙어서 달라지는 점**: 모름 — 블록 값은 부모 kernel32 가 정한다. 부모 쪽 갈래에 디버거 깃발을 보는 줄이 있는지는 이번에 안 봤다 `[공백]`.

## 정정

- 이전 run 이 틀린 곳은 없다. 이전 run 문서 맨 위 정정 한 줄: 없음.
- 태그 올림 (틀린 게 아니라 근거가 생김): 짐 지도 3.1 `AppCompatInfo` 줄의 "`R13` = 시작 때 모은 앱 호환 블록 `[추측]`" → `[정적]` (`+0x69f` · `+0x6a6` · `+0x19cd`). flow.md 2.4 힙 절 "넷째 (`pShimData` 바이트) 만 못 봤다 → 동적 D1" → `[덤프 오프라인]` 0 · 0.
- run88 의 사진 덤프 오프라인 값 (`[관찰]` + "test.exe 블롭인지 `[확인 필요]`") 은 그 판 그대로 둔다. 이번 판부터 같은 종류는 `[덤프 오프라인]` 이고, run88 쪽은 가상 → 물리 번역 없이 물리 연속을 가정했다는 점이 다르다.

## 이전 run 에서 닫은 것

- **run88 "Q90 은 `pShimData` 읽는 쪽만 남김"** (flow.md 4장 run88 정적 줄, 2.6 Q90 줄) → 이번에 닫힘. **Q90 끝.**
- **run46 D1** (sast/40 · plan Q91 의 D1, 짐 지도 7.9) → `[덤프 오프라인]` 으로 닫힘 (라이브 확인은 선택).
- run66 정적 ② (shim 엔진 올리는 조건) → 첫 게이트 (`+0x210`) 만 닫힘. `LdrpLoadShimEngine` · `LdrpGetShimEngineInterface` 안 조건은 그대로.
- run113 정적 ⑤ (`SbpDetermineDllContext` 본문) → `.asm` 은 이번에 뽑았고 부르는 둘 (`RtlImageNtHeaderEx` · `SbpRetrieveCompatibilityManifest`) 까지만. 칸 고르기 규칙은 다음 요청 후보 2 로 남음.
- run113 PE_emulator "`SwitchBackContext` 확인 필요" → 갈리는 조건이 **`pShimData` 블록 하나** 라는 게 닫힘 (에뮬레이터 쪽은 여전히 확인 필요).

## 아직 확인 필요

### 동적 (결과서 "동적분석으로 확인해야 할 것" — 대기열 번호는 오케스트레이터가 붙임, 멈춤점은 test.exe 조건. 모두 선택 · 낮음)

- (Q91 D1 과 겹침, 선택) `ntdll!RtlpHpOptIntoSegmentHeap+0x190` — `r rax` · `db @rax+0x11a8 L0x10`. 기대 `0` (덤프로 이미 0). 라이브 일치 확인용.
- **안 걸려야 맞는 멈춤점**: `LdrpMapDllNtFileName+0x4d7` (COD) · `LdrpInitializeProcess+0x19ef` (`LdrpInitShimEngine`) · `SbObtainTraceHandle+0x5c` (ETW 갈래) · `SbUpdateSwitchContextBasedOnDll+0x1ea` (바꾸는 갈래) · `RtlInitializeHeapManager+0x56` (`SbSelectProcedure`).
- **걸려야 맞는 것**: `SbUpdateSwitchContextBasedOnDll+0xd9` (모듈마다 `[R14+0xc0]` 값이 run113 사진과 같은지).
- kernelbase `CreateFileInternal+0x2f5` (`SbSelectProcedure`) 가 초기화 범위 안에서 불리는지.
- shim 이 걸린 exe 에서만 켜지는 갈래 (`+0x210` ≠ 0 · `+0x11aa`/`+0x11b2` 켜짐 · ETW 켜짐) 는 test.exe 로 못 봄 — 다른 샘플 필요 (범위 밖).

### 정적 (결과서 "다음 요청 후보")

- 1 (범위 넓음) 블록 `+0x210` · `+0x11a8` · `+0x11aa` · `+0x11b2` 를 **누가 켜나** — 부모 `BasepGetAppCompatData` 안엔 없음. 후보 apphelp (`ApphelpQueryCompat…` 류) · kernel32 `BasepCheckAppCompat` · `BasepInitializeApphelpGlobals` · `SbpGetShimData` (`SbpCreateSwitchContext +0x2b2`).
- 2 (작음) `SbpDetermineDllContext` 가 부르는 `SbpRetrieveCompatibilityManifest` · `SbpResolveBasedOnName` — DLL 마다 어느 칸을 읽어 `SbSupportedOsList` 칸을 고르나 (PE_emulator 가 `SwitchBackContext` 를 채우려면).
- 4 (도구) 사진 `memory.dmp` 오프라인 읽기 도구를 레포에 둘지 — 아래 "도구 메모".

### PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 결과서가 본 줄만 옮김. 고칠지는 사용자 결정)

- 지금: 블록을 안 만들고 `PEB+0x2d8 = 0` (결과서: `Src/ntdll/LdrInit.cpp:5763`, 단위 시험 `Test/LdrInitTest.cpp:2824` 는 일부러 안 열린 주소로 `RtlpHpOptIntoSegmentHeap +0x190` 읽기 실패를 시험).
- 갈래 1 · 2 · 3 · 6 · 7: 0 이든 사진 블록이든 같은 길 → **맞음**. 5 는 어느 쪽이든 안 탐.
- 갈래 4 (`SbUpdateSwitchContextBasedOnDll`): 0 이면 `+0x32` 에서 끝 → 모듈 `+0xc0` 0 · `SbDynamicContextDllCount +4` 0. 진짜는 모듈마다 표 칸 주소 · 번호 0 칸 개수 0 → 3. 결과서가 에뮬레이터 `Src` 에서 이 함수 줄을 못 찾음 (grep 0) → **확인 필요**: 두 칸을 지금 어떻게 두나.
- 맞추려면 (결정은 사용자): `PEB+0x2d8` 에 최소 블록 (ctx `+0x7e0 ~ +0x838` 의 위 값) + `SbUpdateSwitchContextBasedOnDll` 줄. 다른 칸은 0 이어도 읽기 결과가 같다.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntdll · kernel32 · kernelbase pdb 줄에 run118, 근거 로그 run118 한 줄), 표시 규칙 표 `[덤프 오프라인]` 새 줄, 2.2 머리 · `CreateProcessInternalW` 절 (블록 꼴 한 줄), 2.4 머리 · `LdrpInitializeProcess` 뒤쪽 (`+0x69f` 복사 · `+0x19dc` 게이트) · 힙 절 (D1 닫힘 · 조건 ④ 줄), 2.5 머리 · 알림 절 (`SbUpdateSwitchContextBasedOnDll` 게이트), 2.6 머리 · Q90 줄 (닫힘), 4장 run46 D1 · run66 ② · run88 Q90 · run113 ⑤ 줄 고침 + 새 줄 셋 (run118 동적 · 정적 · PE_emulator), 4.1 `p2h` 줄 한 마디, 5장 두 표. **그림 안 바꿈.**
- 짐 지도 (`baggage.md`): 기준 run118 줄 · 판 줄, 쓰는 말 태그 `[덤프 오프라인]`, 1장 run118 메모, 2장 아래 (run118) 한 줄 (**숫자 그대로**), 3장 머리 run118 줄, 3.1 `pShimData` · `AppCompatInfo` 줄, 3.5 `SwitchBackContext` 줄, 3.8 `SbDynamicContextDllCount +4` 줄, 4장 `pShimData` 영역 짝 줄, 7.6 C8 한 마디 · **새 7.6.15 (블록 칸 표)**, 7.9 D1 닫힘, 새 7.49, 8장.

## 디버거 메모 / 도구 메모

- **도구 (레포 밖)**: 결과서의 덤프 읽기 도구 `scratchpad/s116/vread.py` (PAGEDU64 물리 구역 + 4단 페이지 표 번역) 와 블록 파일 `blk_*.bin` 은 스크래치에만 있다 (커밋 안 됨, 스크래치는 지워질 수 있음). **레포에 둘지는 사용자 결정** (run88 · sast/85 도 같은 말).
- 함정 제안 (windbg-kd-runbook pitfalls 또는 덤프 읽기 도구 메모에): **P2 덤프는 머리의 기본 `DirectoryTableBase` 가 test.exe 가 아니라 다른 프로세스 것** — 오프라인 번역 때 test.exe 값 (`0x192c2e000`, `04_baggage-read_P0.txt:333`) 을 직접 줘야 한다 (결과서).
- 결과서: 스크립트 문제 없음. `.asm` 의 `JZ` 목적지는 주소로 나오니 `+오프셋` 은 함수 입구 주소를 빼서 셈 (예 `RtlpHpOptIntoSegmentHeap +0x1a0 JZ` → `+0x281`).

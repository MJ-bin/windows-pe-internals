# 116 shimdata-read — 정적 분석 결과

- 요청서: `_workspace/sast/116_shimdata-read.md` (Q90 의 남은 몫 — ntdll 이 PEB `pShimData` (`+0x2d8`) 를 **읽는** 곳)
- 모듈 / 바이너리: ntdll (`ms/bin/…/System32/ntdll.dll`, pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1`), 부모 쪽 대조는 kernel32 · kernelbase (이미 있던 `.asm` + 아래 4개)
- 새로 뽑은 함수 (depth 0, 있던 `.asm`·`.c` 는 다시 안 뽑음):
  - ntdll 3개 (`^(SbGetCurrentSwitchContext|LdrpIsCODServiceEnabled)$`, `^SbpDetermineDllContext$`) → `ms/disassemble/ntdll/`, `ms/decomp/ntdll/`
  - kernel32 4개 (`^(BasepGetAppCompatData|SbPrepareSwitchContext|SbpCreateSwitchContext|SbpIsTraceEnabled)$`) → `ms/disassemble/kernel32/`, `ms/decomp/kernel32/`. 요청서엔 "읽기만" 이었지만 부모가 블록을 **만드는** 함수 `.asm` 이 없어서 이 4개만 depth 0 으로 뽑았다 (질문 2 의 대조용).
  - ntoskrnl 은 안 건드렸다. 새 ntdll·kernel32 함수의 호출 줄이 각 `_calls.tsv` 에 덧붙었다 (ntdll 에 `LdrpIsCODServiceEnabled` 2줄 등).
- 읽기 전수: 스크래치 objdump (`…/scratchpad/ntdll.dis`, ntdll `.text` 전체) 에서 `+0x2d8]` 꼴 전수 → 함수 이름은 `_index.tsv` 로 붙임.
- 사진 값: `ms/dump/03_baggage-snap/{P0,P1,P1a,P1b,P2}/memory.dmp` 를 **오프라인으로** 읽음 (아래 "사진 오프라인 읽기"). `ms/logs/04_baggage-read_*` 에는 블록 바이트 덤프가 없어서 (PEB 줄과 VAD 줄뿐) 직접 읽었다. 표시는 `[덤프 오프라인]` (디버거로 본 `[관찰]` 아님).
- 버전 확인: `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 일치, `LdrpLoadDll +0xfb/+0x100 → LdrpLoadDllInternal` 일치. 오프라인으로 번역한 PEB `+0x10` (`0x7ff61a370000`)·`+0x2d8` (`0x25996d10000`)·`+0x308` (`0x25996d00000`) 이 `04_baggage-read_P0.txt` · `_P1a.txt` 줄과 같음 → 스크립트와 바이너리 둘 다 믿는다.
- 스크립트 문제: 없음.

## 한눈에

- ntdll 안에서 PEB `+0x2d8` 를 PEB 로 읽는 줄은 **9줄, 함수 6개** 뿐이다 (아래 질문 1 표 7행 중 2번은 같은 함수가 복사해 둔 지역 칸을 다시 읽는 줄). 나머지 `+0x2d8]` 19줄은 스택·다른 구조체라 PEB 가 아니다.
- test.exe 의 블록은 **2쪽 (0x2000) 중 0이 아닌 바이트가 74개** 뿐이다. ntdll 이 읽는 칸은 대부분 0 이라 **shim 엔진·세그먼트 힙·ETW 갈래는 전부 안 탄다.**
- 딱 하나 **탄다**: 모듈마다 부르는 `SbUpdateSwitchContextBasedOnDll` 이 블록 안 SwitchBack 칸 (`+0x7e0`) 을 보고 모듈 항목 `+0xc0` 에 값을 놓는다. 사진 `SbDynamicContextDllCount` 번호 0 칸의 개수가 P1a 0 → P1b·P2 3 (sast/111) 으로 늘어난 것이 이게 돈 증거다.
- **PE_emulator 가 `pShimData` 를 0 으로 두면 이 하나만 달라진다** (모듈 `+0xc0`·개수 칸이 0). 나머지 읽기는 0 이어도 결과가 같다.
- sast/40 D1 (`+0x11aa`·`+0x11b2` 두 바이트) 을 **덤프로 읽어 닫았다**: 둘 다 `0` → ④ 조건이 안 걸리고 ⑤ 로 넘어가 NT 힙. Q91 의 D1 은 오프라인으로 답이 났다 (라이브 확인은 선택).

## 질문별 답 [정적]

### 질문 1. `pShimData` 를 읽는 곳 전수 (초기화 범위)

찾는 법: ntdll `.text` objdump 에서 `[reg+0x2d8]` 꼴 전부 (28줄) → 앞줄에서 PEB (`GS:[0x60]`·`[RDI+..]` 등) 에서 온 것만 거름. 거른 19줄은 맨 아래 "PEB 아닌 `+0x2d8`".

| # | 함수 `+오프셋` (읽는 줄) | 읽은 값으로 하는 일 | 불리는 길 · 사진 구간 | test.exe |
|---|---|---|---|---|
| 1 | `LdrpInitializeProcess +0x69f` `MOV RAX,[RDI+0x2d8]` (RDI = PEB). 앞에 `+0x696 CMP [UseWOW64]` · `+0x69d JNZ +0x6ae` (WOW64 면 건너뜀) | 값을 지역 칸 `[RSP+0x180]` 에 **복사만** (`+0x6a6`). 이 칸은 `+0x5d9 XOR EAX,EAX` · `+0x5db` 에서 미리 0 | `LdrpInitializeProcess` 본문 · **P1~P1a** | 탐 (x64 exe). 복사 값 = `0x25996d10000` |
| 2 | 같은 함수가 그 지역 칸을 다시 씀: `+0x19cd MOV R13,[RSP+0x180]` → `+0x19d7 TEST` · `+0x19da JZ +0x19f4` → **`+0x19dc CMP [R13+0x210],0`** · `+0x19e3 JZ +0x19f4` | 블록 `+0x210` 이 0 이 아니면: `+0x19e5` PEB `AppCompatInfo` (`+0x2e0`) 에 0 을 쓰고 `+0x19ef/+0x19f4 LdrpInitShimEngine(블록)` (apphelp 를 올림). 또 `+0x1cd4 MOV RDX,R13` 로 `g_pfnSE_InstallAfterInit(…, 블록)` 인자 (`+0x1ca7 CMP [g_ShimsEnabled],R12B` · `+0x1cae JZ +0x1d07` — 켜져 있을 때만) | **P1a~P1b** (`+0x19cd`) · `+0x1cd4` 는 **P1b~P2** | `+0x210` = 0 → `+0x19e3` 에서 건너뜀. `LdrpInitShimEngine`·`SE_InstallAfterInit` 안 탐 |
| 3 | `SbObtainTraceHandle +0x22` `MOV RBX,[RAX+0x2d8]` (`+0xf` 가 `GS:[0x60]`). 이어서 `+0x2e ADD RBX,0x7e0` → 문맥(ctx) 주소, `+0x37 CMP [RBX+0x30],0` | ctx 안 **ETW 켜짐 칸** `[ctx+0xc]` (`+0x44 CMP`·`+0x47 JNZ +0x5c`) 이 0 이 아니면 `MS_Windows_AeSwitchBack_Provider` 를 `EtwNotificationRegister` (`+0x83/+0x88`) 로 등록하고 핸들을 `[ctx+0x10]` 에 `LOCK CMPXCHG` (`+0xad`), `SbpTraceContextUpdate` (`+0xec/+0xf1`). 아니면 `+0x49` 에서 0 반환 | `LdrpInitializeProcess +0x19c8/+0x19cd` (RCX = 0) · `SbUpdateSwitchContextBasedOnDll +0x205/+0x20a`, `+0x297/+0x29c` · **P1a~P1b** | 블록 있음 · `[ctx+0x30]` ≠ 0 · `[ctx+0xc]` = 0 → **0 반환, 쓰기·시스템 콜 0** |
| 4 | `SbGetCurrentSwitchContext +0x9` `MOV RAX,[RAX+0x2d8]` (`+0x0` 가 `GS:[0x60]`). `+0x15 ADD RAX,0x7e0`, `+0x1d CMP [RAX+0x30],0`, `+0x23 RET` / 없으면 `+0x25` 0 | **ctx 주소를 돌려주는 작은 함수.** 블록 0 이거나 `[ctx+0x30]` 첫 dword 가 0 이면 0 | 부르는 곳: `SbUpdateSwitchContextBasedOnDll +0x14/+0x19`, `SbpUpdateCache +0x3e`, (`LdrShutdownProcess +0x1d6` — 종료라 범위 밖). `SbUpdateSwitchContextBasedOnDll` ← `LdrpSendPostSnapNotifications +0xa3/+0xa8` ← `LdrpNotifyLoadOfGraph +0x20/+0x25` ← `LdrpPrepareModuleForExecution +0x76/+0x7b` ← `LdrpInitializeProcess +0x1aed/+0x1af2` · **P1a~P1b** (모듈마다) | ctx 있음 → 아래 "탄다" 설명 |
| 5 | `SbSelectProcedure +0x5f`, `+0x8f`, `+0x140`, `+0x199` (모두 `GS:[0x60]` → `[RAX+0x2d8]`) | 같은 ctx 를 보고: `[ctx+0x30]` 없으면 `+0x545` 길 (`RtlGetVersion` + `SbSupportedOsList` 표로 OS 맞는 함수 고름), 있으면 `[ctx+0]` 를 캐시와 견줘 (`+0xb5 ~ +0xbc`) 다르면 `SbpUpdateCache` (`+0x203/+0x208`), 같으면 캐시 항목에서 함수 고름. ETW 켜짐이면 `AeSbCallEvent`/`AeSbImplEvent` 를 씀 (`+0x3d7`, `+0x52a`) | 초기화 범위 안에서 부르는 곳은 **`RtlInitializeHeapManager +0x56/+0x5b`** 하나 (`+0x2f JZ +0x6c` 가 앞 함수 `RtlpHpOptIntoSegmentHeap` 반환 0 이면 건너뜀). 그 밖 `SwitchedRtlGetVersion +0x6c`, `SbExecuteProcedure +0x4` (내보낸 함수, ntdll 안 부르는 줄 없음) | **안 탐** (아래 6 이 0 반환 = NT 힙) |
| 6 | `RtlpHpOptIntoSegmentHeap +0x184` `MOV RAX,[R14+0x2d8]` (`+0x66 MOV R14,GS:[0x60]`). `+0x18b TEST` · `+0x18e JZ +0x1aa` | `+0x190 TEST byte [RAX+0x11b2],0x4` · `+0x197 JZ +0x1aa`: **비트 4 가 있으면** `+0x199 TEST byte [RAX+0x11aa],0x40` · `+0x1a0 JZ +0x281` — 비트 `0x40` 없으면 0 반환 (NT 힙, 뒤 조건 안 봄), 있으면 `+0x1a6 [RDX]=1` · `+0x1b5` 1 반환 (세그먼트 힙). 비트 4 가 없으면 `+0x1aa RtlGetSuiteMask` 쪽 (sast/40 ⑤ ~ ⑨) 로 넘어감 | `RtlInitializeHeapManager +0x28/+0x2d` ← `LdrpInitializeProcess +0x9ed/+0x9f2` · **P1~P1a** | 블록 있음 · `+0x11b2` = 0 → **`+0x197` 에서 ⑤ 로 넘어감** · 결과 NT 힙 (sast/40 사진과 같음) |
| 7 | `LdrpCheckComponentOnDemandEtwEvent +0x6c` `MOV RBX,[RAX+0x2d8]` (`+0x63 GS:[0x60]`). 앞에 `+0x52 CompatCachepLookupCdb(이름, 0x100)` 가 0 이면 `+0x5d JZ +0x146`, 뒤에 `+0x73 TEST` · `+0x76 JZ +0x146` · `+0x7c LdrpIsCODServiceEnabled` (레지스트리 `…\AppCompatFlags\Compatibility Assistant\` 키 열어 봄) | 블록 `+0x1178` 에 이벤트 핸들을 **씀** (`+0xca`), `ETW` 이벤트 (`UserLoaderGuid`·`ComponentOnDemand`, `+0x122/+0x12b`) 를 쓰고 `NtWaitForSingleObject` (`+0x13d/+0x146`, 상대 시간 `-0x5f5e100` = 10초), `+0x154` 에서 `+0x1178` 을 0 으로 되돌림 | **`LdrpMapDllNtFileName +0x4d7/+0x4dc`** 에서만. 그 앞 `+0x4c0 CMP EAX,0xc0000022` (접근 거부) · `+0x4cb TEST R13D` · `+0x4ce JNZ` (`R13D` 가 0 일 때만) 일 때만 | **안 탐으로 보임** (DLL 열기가 접근 거부로 안 끝남 — 동적으로 확인) |

**탄다 (4번 → `SbUpdateSwitchContextBasedOnDll`) 안에서 일어나는 일:**
- `+0x14` 로 ctx 를 받는다. `+0x32 JZ +0x39` 은 ctx 0 이면 바로 0 반환 (`pShimData` 가 0 이면 여기서 끝).
- `+0x47 CMP [ctx+8],0` · `+0x4a JZ +0x2ec`: ctx 의 "켜짐" 칸이 0 이면 아무것도 안 하고 1 반환. 사진 값 1 → 계속.
- `+0xb5 ~ +0xc1` `SbpDetermineDllContext(DllBase)` (이 DLL 의 PE 머리 OS 버전 + 호환 매니페스트 `SbpRetrieveCompatibilityManifest` 로 "어느 OS 용으로 만든 DLL 인가" 를 `SbSupportedOsList` 표 칸으로 돌려줌) → `+0xd9 MOV [R14+0xc0],RDI` 모듈 항목 `SwitchBackContext` 에 표 칸 `+4` 주소를 놓음 (sast/109 가 이미 정리) → `+0xfb INC.LOCK SbDynamicContextDllCount+4` 칸 개수 +1.
- ctx 를 **바꾸는 갈래**: DLL 의 표 칸이 번호 4 (10.0) 면 `+0x10f ~ +0x126` 에서 바로 `+0x12c` (1 반환). 아니면 `+0x1c6 MOV EAX,[R15]` · `+0x1c9 CMP [RDX],EAX` · `+0x1cb JGE +0x12c` — DLL 칸 번호가 **지금 문맥 칸 번호 이상이면 안 바꿈**. 바꾸는 쪽은 `+0x1d1 ~ +0x1e4` 버전 견주기 (`[ctx+0x28]`) 뒤 `+0x1ea` 잠금 `SbpContextLock` → `+0x1f6 INC [ctx]` → `+0x202` 새 GUID 를 `[ctx+0x40]` 에 씀 → `SbObtainTraceHandle` → `+0x2a0 INC [ctx]`. **test.exe 는 지금 문맥이 이미 표 번호 0 (6.0, 가장 낮은 칸)** 이라 어떤 DLL 도 더 낮을 수 없어서 이 갈래가 안 탄다 (사진 P0~P2 블록 바이트가 안 바뀜 — 아래). `SbSupportedOsList` (ntdll `rva 0x173010`, 칸 `0x20` 바이트 × 5 — 파일 값 `[정적]`): 번호 0 = 6.0 `{e2011457-1546-43c5-a5fe-008deee3d3f0}`, 1 = 6.1, 2 = 6.2, 3 = 6.3, 4 = 10.0 `{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}`.

**범위 밖 읽는 곳 (이름만, 안 따라감):** kernelbase `CreateFileInternal +0x61f` (`[RCX+0x2d8]`) 와 `+0x2f5 SbSelectProcedure(0xabababab, …, g_SbModuleTable_KernelBase, …)`. 부모 쪽 kernelbase `CreateProcessInternalW +0x56f4` 는 PEB 가 아니라 자기 로컬 블록 `+0x11a8` 을 읽는다 (ARM64 exe 일 때만). shim 엔진 본문 (apphelp `SE_InitializeEngine` 등) 은 이름만: 블록 포인터를 통째로 받는다 (`LdrpInitShimEngine +0x13f MOV R8,RDI`, `+0x160` CFG 호출).

**PEB 아닌 `+0x2d8` 19줄** (같은 objdump, 앞줄 확인): 스택 `[rsp+0x2d8]` — `LdrpResCompareResourceNames +0x4b`, `LdrpLogEtwEvent +0x3c`, `LdrpLoadDependentModuleInternal +0x23a4`, `LdrpInitializeProcess +0x1349 · +0x1a67 · +0x1ade · +0x1ba7 · +0x1bcc`, `_output_s +0x831` · 다른 구조체 `[rcx/rdx/rdi/r14/rbp + …]` — `RtlpAllocateActivationContextStackFrame +0x20e` (쓰기), `RtlStdInitializeStackDatabase +0x187`, `RtlpStdLogCapturedStackTrace +0x5d · +0xd7 · +0xe2`, `RtlStdReleaseStackTrace +0xe6`, `Xp10ReadAndDecodeHuffmanTables +0x218 · +0x72c`, `PsspDuplicateSnapshotLocalToRemote +0x36a · +0x3b3`. 숫자 `0x2d8` 가 `]` 로 안 끝나는 줄은 `LdrpInitializeProcess +0xed` (`[r11-0x2d8]`) · `LdrpFindLoadedDll +0xc · +0x1b8` (`sub/add rsp`) 뿐.

- 근거: `ms/disassemble/ntdll/` `LdrpInitializeProcess.asm`, `SbObtainTraceHandle.asm`, `SbGetCurrentSwitchContext.asm` (새로), `SbSelectProcedure.asm`, `SbUpdateSwitchContextBasedOnDll.asm`, `SbpUpdateCache.asm`, `RtlpHpOptIntoSegmentHeap.asm`, `RtlInitializeHeapManager.asm`, `LdrpCheckComponentOnDemandEtwEvent.asm`, `LdrpMapDllNtFileName.asm +0x4b5 ~ +0x4e4`, `LdrpIsCODServiceEnabled.asm` (새로), `LdrpInitShimEngine.asm`, `SbpDetermineDllContext.asm` (새로).
- **지금 PE_emulator**: 읽기 갈래 7행 중 **1·2·3·6 은 0 으로 둬도 결과가 같다** (복사 0 → `+0x19da JZ`, `SbObtainTraceHandle` 0 반환, 6 은 `+0x18e JZ` 로 같은 ⑤ 로). 에뮬레이터 소스 주석 (`Src/ntdll/LdrInit.cpp:5763 ~ 5766`) 도 이렇게 줄였다. **4 (`SbUpdateSwitchContextBasedOnDll`) 만 갈린다**: 0 이면 `+0x32` 에서 끝이라 모듈 `+0xc0` 이 0 · `SbDynamicContextDllCount +4` 가 0 인데, 사진은 모듈마다 표 칸 주소 (sast/109) · 번호 0 칸 개수 3 (sast/111). 에뮬레이터 `Src` 에 `SbUpdateSwitchContextBasedOnDll` 줄이 안 보여서 (grep 0) **확인 필요**: 지금 이 두 칸을 어떻게 두는지.

### 질문 2. 블록 꼴 (ntdll 이 읽는 칸 + 부모가 쓰는 줄 + 사진)

`ctx` = 블록 `+0x7e0` (SwitchBack 문맥). 사진 값은 `[덤프 오프라인]` (P0 = P1 = P1a = P1b = P2, 블록 바이트 같음). 블록 전체 0x2000 중 0이 아닌 바이트 74개 — 아래 표 밖은 전부 0.

| 오프셋 (ctx 기준) | 크기 | ntdll 읽는 줄 | 뜻 | 부모가 쓰는 줄 | 사진 |
|---|---|---|---|---|---|
| `+0x208` | 4 | (ntdll 안 읽음) | 블록 크기 `[추측]` — 부모가 `RtlAllocateHeap` 크기와 `NtAllocateVirtualMemory` 크기 (`[RSP+0x1f8]`) 로 같은 값을 씀 | kernel32 `BasepGetAppCompatData +0x31` (지역 `0x11c0`), `+0x8f` 할당, `+0xb2` 쓰기 | `0x11c0` (VAD 2쪽 안에 들어감) |
| `+0x20c` | 4 | (안 읽음) | 머리표 | `BasepGetAppCompatData +0xc0` | `0xac0dedab` |
| **`+0x210`** | 4 | **`LdrpInitializeProcess +0x19dc`** | **shim 엔진을 켤지** (0 아니면 `LdrpInitShimEngine`) | 못 찾음 `[공백]` (`BasepGetAppCompatData` 안 쓰기 없음. apphelp 질의 결과 쪽 `[추측]`) | `0` |
| `+0x214` | 2 | (안 읽음) | 이미지 `Machine` `[추측]` | `+0xce` | `0x8664` |
| `+0x218 ~ +0x3df` | `0x1c8` | (안 읽음) | 호출자가 준 `0x1c8` 크기 질의 결과 복사 (`[R13]==0x1c8` 일 때) | `+0x3be ~ +0x420` | 전부 `0` |
| **`+0x7e0`** (ctx `+0x0`) | 8 | `SbSelectProcedure +0xb5` · `SbpUpdateCache +0x72` | **바뀜 번호** (캐시와 견줌). ntdll 이 올림 (`SbUpdateSwitchContextBasedOnDll +0x1f6 · +0x2a0`) | `kernel32!SbpCreateSwitchContext +0x1a2` (=1) | `1` |
| **`+0x7e8`** (ctx `+0x8`) | 4 | **`SbUpdateSwitchContextBasedOnDll +0x47`** | **켜짐** (0 이면 모듈 문맥 갱신 통째로 건너뜀) | `+0x1a9` (=1) | `1` |
| **`+0x7ec`** (ctx `+0xc`) | 4 | **`SbObtainTraceHandle +0x44`** · `SbSelectProcedure +0x15b · +0x1b4` | **ETW 켜짐** (`MS_Windows_AeSwitchBack_Provider`) | `+0x1b4` = `SbpIsTraceEnabled` 반환 (`+0x193` 호출) | `0` |
| `+0x7f0` (ctx `+0x10`) | 8 | `SbObtainTraceHandle +0x5c`, `SbSelectProcedure +0x21a` | ETW 알림 핸들 칸 (ntdll 이 `LOCK CMPXCHG` 로 **씀**: `SbObtainTraceHandle +0xad`, `SbSelectProcedure +0x2a4 · +0x3f9`) | `+0x1c1` (0) | `0` |
| `+0x7f8` (ctx `+0x18`) | 8 | `SbSelectProcedure +0x1f9` 가 `ctx+0x18` 을 `SbpUpdateCache` 에 넘김 | 아래 덩어리의 시작 (맨 앞 8B 는 고른 OS 의 버전 수 `[추측]`) | `+0x169 ~ +0x17c` 에서 만들어 `+0x1c9 ~` 로 `0x340` 바이트 복사 | `0x0006000000000000` |
| `+0x800` (ctx `+0x20`) | 4 | (안 읽음) | 부모 인자 `R9D` | `+0x18b` | `3` |
| **`+0x808`** (ctx `+0x28`) | 8 | **`SbUpdateSwitchContextBasedOnDll +0x1e0`** (`CMP RCX,[RBP+0x28]`) | DLL 버전 수와 견주는 기준 (더 작으면 갱신 건너뜀) | `+0xd1 ~ +0xeb` (`0x60002` 를 조건부로) | `0` (→ 아무 DLL 도 안 걸림) |
| **`+0x810`** (ctx `+0x30`) | 16 | **`SbGetCurrentSwitchContext +0x1d`** · `SbObtainTraceHandle +0x37` · `SbSelectProcedure +0x7c · +0xab` (첫 dword 가 0 이 아니면 "문맥 있음"), `SbpUpdateCache +0x206` | 처음 정한 호환 문맥 GUID | `+0xfd`, `+0x10a` (표 칸 GUID 복사) | `{e2011457-1546-43c5-a5fe-008deee3d3f0}` = `SbSupportedOsList` 번호 0 (6.0, 매니페스트 없는 exe 의 기본 칸) |
| **`+0x820`** (ctx `+0x40`) | 16 | `SbUpdateSwitchContextBasedOnDll +0x55 ~ +0x82`, `SbpUpdateCache +0x6e` (읽기) · `SbUpdateSwitchContextBasedOnDll +0x202` (**ntdll 이 씀**, 갱신 갈래만) | **지금 문맥 GUID** | 같은 줄 | 위와 같은 GUID |
| `+0x830` (ctx `+0x50`) | 4 | (안 읽음) | 부모 인자 `R8D` (값 1) | `+0xe0` | `1` |
| **`+0x834`** (ctx `+0x54`) | 4 | **`SbpUpdateCache +0xb3`** (`CMP EAX,[R8+0x3c]`, R8 = ctx+0x18) | 아래 GUID 표 개수 | (안 씀 — 0 으로 둠) | `0` |
| `+0x838 ~` (ctx `+0x58 + 0x10·i`) | 16 × n | `SbpUpdateCache +0xbf ~ +0xdb` | 앱이 지원한다고 알린 OS GUID 표 | (안 씀) | 비어 있음 |
| `+0xb38` | 4 | (안 읽음) | 부모 PID (`GS:[0x40]`) | `BasepGetAppCompatData +0x159` | `0x19a8` = 부모 (`InheritedFromUniqueProcessId`, `_P0.txt:154`) |
| `+0xb3c ~` | UTF-16 | (안 읽음) | 부모 이미지 경로 | `+0x182 ADD RCX,0xb3c` · `+0x18e wcsncpy_s` | `C:\WINDOWS\system32\cmd.exe` |
| `+0x1144` | 4 | (안 읽음) | 이미지 파일 크기 `[추측]` (`SECTION_IMAGE_INFORMATION +0x38`) | `+0xdc` | `0x86a00` = 551424 = test.exe 파일 크기 |
| `+0x1148 ~ +0x1160` | 4 × 7 | (안 읽음) | 체크섬·플래그들 `[추측]` | `+0xe9 · +0xfe · +0x114 · +0x129 · +0x138 · +0x146`, `+0x31a (+0x114c)` | 전부 `0` |
| **`+0x1178`** | 8 | ntdll 이 **씀**: `LdrpCheckComponentOnDemandEtwEvent +0xca`, 0 으로 `+0x154` | 임시 이벤트 핸들 (COD 길에서만) | — | `0` |
| `+0x11a8` bit 7 | 1 | (ntdll 안 읽음) | kernelbase `CreateProcessInternalW +0x56f4 ~ +0x56fd` 가 ARM64 exe 일 때 봄 | 쓰는 줄 못 찾음 `[공백]` | `0` |
| **`+0x11aa`** bit `0x40` | 1 | **`RtlpHpOptIntoSegmentHeap +0x199`** | **세그먼트 힙 켜라** | 못 찾음 `[공백]` | `0` |
| **`+0x11b2`** bit `0x4` | 1 | **`RtlpHpOptIntoSegmentHeap +0x190`** | 위 값이 "정해졌나" (비트 4 없으면 `+0x11aa` 안 봄) | 못 찾음 `[공백]` | `0` |
| `+0x11c0` | — | — | 블록 끝 | — | — |

- 한 줄 대조 (부모가 쓰는 꼴): kernelbase `CreateProcessInternalW` 가 `BasepGetAppCompatData` (`+0x56bb`, ret `+0x56c2`) 로 `[RSP+0x2a0]` 에 이 블록 (크기 `[RSP+0x1f8]`) 을 받고 자식에 `NtAllocateVirtualMemory` (`+0x574e/+0x5755`) → `NtWriteVirtualMemory` (`+0x578e/+0x5795`) → PEB `+0x2d8` (`+0x57d0/+0x57d7`). 사진의 VAD `25996d10 ~ 25996d11` (2쪽, Private READWRITE, `_P0.txt:1381`) 이 `0x11c0` 을 올려 2쪽으로 받은 것과 맞다. 부모 안 `BasepGetAppCompatData` 가 `+0x261/+0x266` 에서 `SbPrepareSwitchContext` → `+0xc0/+0xc5` `SbpCreateSwitchContext` 를 불러 ctx 를 채운다.
- 쓰는 줄이 이 정적 범위에서 **안 보이는 칸** `[공백]`: `+0x210`, `+0x11a8`, `+0x11aa`, `+0x11b2`. 이 칸들은 apphelp 의 호환 질의 (SDB) 결과일 것 `[추측]` — shim 이 걸린 exe 에서만 켜진다. test.exe 는 전부 0.
- **지금 PE_emulator**: 블록을 안 만들고 `PEB+0x2d8 = 0` 으로 둠 (`Src/ntdll/LdrInit.cpp:5763`, 단위 시험 `Test/LdrInitTest.cpp:2824` 는 일부러 안 열린 주소를 넣어 `RtlpHpOptIntoSegmentHeap +0x190` 읽기 실패를 시험). 위 표의 ntdll 이 읽는 칸이 전부 0 이라 **0 으로 둬도 읽기 쪽 결과는 같다.** 사진과 맞추려면 `+0x7e0 ~ +0x838` (ctx) 만 채우면 된다 — 지금 갈리는 것은 질문 1 의 4 (`SbUpdateSwitchContextBasedOnDll`) 하나라서.

### 질문 3. test.exe 에서 무엇이 갈리나 (블록 있음 — 사진 값)

| 읽기 갈래 | 사진 값 | 결과 (test.exe) | 안 탄 길 |
|---|---|---|---|
| 1 `LdrpInitializeProcess +0x69f` | PEB `+0x2d8` = `0x25996d10000` | 지역 칸에 복사 | — |
| 2 `+0x19dc` `[+0x210]` | `0` | `+0x19e3 JZ +0x19f4` — `LdrpInitShimEngine` 안 부름 (apphelp 안 올림), PEB `AppCompatInfo` 0 그대로 (`_P0.txt:1636`·`_P2.txt:2513` `(null)`), `g_ShimsEnabled` 0 → `+0x1cae JZ +0x1d07` 로 `SE_InstallAfterInit` 안 부름 | shim 엔진 전체 (`LdrpInitShimEngine`·`LdrpLoadShimEngine`·`LdrpGetShimEngineInterface`·`SE_*`) |
| 3 `SbObtainTraceHandle` | `[ctx+0x30]` ≠ 0, `[ctx+0xc]` = 0 | 0 반환 (`+0x44/+0x47` 에서 `+0x49`) | ETW 등록 (`EtwNotificationRegister`·`SbpTraceContextUpdate`·`CMPXCHG`) |
| 4 `SbUpdateSwitchContextBasedOnDll` | `[ctx+8]` = 1, `[ctx+0x28]` = 0, (exe → 번호 0, k32 → 번호 4, sast/109), ctx GUID = `SbSupportedOsList` 번호 0 (6.0) | **탄다.** 모듈마다 `SbpDetermineDllContext` → `[모듈 +0xc0]` 에 표 칸 주소, `SbDynamicContextDllCount +4` 개수 +1 (번호 0 칸 개수 P1a 0 → P1b·P2 3, sast/111). 갱신 갈래 (`+0x1ea` 잠금) 는 안 탐 — 블록 바이트 P0~P2 같음 | 문맥 갱신 (`SbpContextLock`, `INC [ctx]`, GUID 쓰기) |
| 5 `SbSelectProcedure` | — | 안 부름 (6 이 0 반환 → `RtlInitializeHeapManager +0x2f JZ +0x6c`). `g_RegInfo+8` (`0x1ce698`) 0 그대로 (sast/40 사진) | `SbSelectProcedure` 전부, `+0x67` CFG 호출 |
| 6 `RtlpHpOptIntoSegmentHeap` | `+0x11b2` = `0`, `+0x11aa` = `0` | `+0x197 JZ +0x1aa` — ④ 안 걸리고 `RtlGetSuiteMask` (⑤) 로. 끝까지 가서 0 반환 = **NT 힙** (사진 힙 `+0x10` = `0xffeeffee`, sast/40) | 세그먼트 힙 길 (`+0x1a6`, `RtlpHpHeapCreate` 등) |
| 7 COD 길 | `+0x1178` = 0 | 안 탐 (`LdrpMapDllNtFileName +0x4c0` 접근 거부 때만) | 이벤트 만들기·ETW 쓰기·10초 기다림 |

- 방법·사진 구간은 아래 "사진 오프라인 읽기".
- **지금 PE_emulator**: 2·3·6·7 은 0 이든 사진 블록이든 같은 길 (**맞음**). 4 는 0 이면 모듈 `+0xc0`·개수 칸이 안 놓임 (**틀림 (작게)** — sast/109 "확인 필요" 에 이어, 읽는 쪽 조건이 이 블록 하나였다는 것이 이번에 닫힘). 5 는 어느 쪽이든 안 탐.

### 질문 4. 짐 지도에 보탤 줄 (제안, 문서는 안 고침)

- **baggage 3.1 `+0x2d8 pShimData` 줄** ("읽는 쪽" 칸에 보탬): `[정적]` sast116 — ntdll 은 PEB 에서 읽는 줄 9곳 (함수 6): `LdrpInitializeProcess +0x69f` (지역 복사) · `SbObtainTraceHandle +0x22` · `SbGetCurrentSwitchContext +0x9` · `SbSelectProcedure +0x5f/+0x8f/+0x140/+0x199` · `RtlpHpOptIntoSegmentHeap +0x184` · `LdrpCheckComponentOnDemandEtwEvent +0x6c`. 읽은 블록 안 칸: `+0x210` · `+0x7e0 ~ +0x838` (SwitchBack 문맥) · `+0x11aa` · `+0x11b2` · (쓰기 `+0x1178`). test.exe 에서 탄 것: **`SbUpdateSwitchContextBasedOnDll` 뿐**. 사진 `[덤프 오프라인]`: 블록 P0 ~ P2 바이트 같음.
- **baggage 3.1 `+0x2e0 AppCompatInfo`**: 쓰는 줄 `LdrpInitializeProcess +0x19e5` (블록 `+0x210` ≠ 0 일 때만, test.exe 안 탐 — `끝까지 0`).
- **baggage 7.6 `pShimData` 영역 줄** ("(run87) PEB `pShimData` 가 가리키는 앱 호환 데이터 영역") 에 칸 표 한 줄: 위 질문 2 표 요약 — 크기 `0x11c0` (VAD 2쪽), 비제로 74B (`+0x208` 크기 · `+0x20c` `0xac0dedab` · `+0x214` `0x8664` · ctx `+0x7e0 ~ +0x838` · `+0xb38` 부모 PID · `+0xb3c` 부모 경로 · `+0x1144` 파일 크기), ntdll 이 **블록에 쓰는 칸은 test.exe 에서 없음** (갱신 갈래 안 탐, COD 안 탐, ETW 안 탐), 부모 쓰는 줄 `BasepGetAppCompatData` (kernel32, rva `0x2efe0`) · `SbpCreateSwitchContext` (`0x2f65c`) · 7.8 쓰기 순서 그대로.
- **baggage 7.9 D1** (`+0x11aa` · `+0x11b2`): "정적으로 못 닫음 (Q91)" → **`[덤프 오프라인]` 둘 다 `0`** 로 닫음 (라이브 확인 선택).
- 칸 수·구간 숫자는 안 바뀐다 (블록 안 칸은 `_PEB` 가 아니라 목록 밖 데이터 영역 줄).

## 사진 오프라인 읽기 (방법·한계)

- 파일: `ms/dump/03_baggage-snap/<태그>/memory.dmp` — VMware 사진을 `vmss2core` 로 바꾼 **PAGEDU64 풀 덤프**. 머리 `+0x88` 의 물리 구역 2개 (`0 ~ 0xc0000000`, `0x100000000 ~ +0x140000000`), 자료는 `+0x2000` 부터 이어 붙임.
- 번역: test.exe 의 `DirectoryTableBase` = `0x192c2e000` (`04_baggage-read_P0.txt:333`) 로 4단 페이지 표 걷기. 검증: PEB `+0x10` = `0x7ff61a370000`, `+0x2d8` = `0x25996d10000`, `+0x308` = `0x25996d00000`, `+0x2e0`·`+0x2f8` = 0 — 로그 줄과 같다. 블록 2쪽 (`0x25996d10000 ~ 0x25996d11fff`) 모두 물리 메모리에 있었음.
- 결과: **P0 · P1 · P1a · P1b = P2 블록 바이트 같음** (md5 같음, P2 는 머리의 기본값이 다른 프로세스 것이라 `DirectoryTableBase` 를 직접 줌). 즉 ntdll 초기화 동안 ntdll 이 블록에 쓴 바이트 없음. K3 는 PEB `+0x2d8` 이 null (`_K3.txt:1633`) 이라 블록이 아직 없음.
- 한계: `[덤프 오프라인]` — 디버거로 본 `[관찰]` 이 아니다. 사진은 한 번의 실행 (run44) 값이다. P1 ~ P1b 사이에 블록을 쓰고 지웠다면 못 본다 (정적으로는 쓰기 줄이 `+0x1178` 하나뿐이고 안 탐).
- 스크래치 (커밋 안 함): `…/scratchpad/s116/vread.py` (읽기 도구), `…/scratchpad/s116/blk_P0.bin` (블록 `0x2000` 바이트, MS 코드 아님). **레포에 읽기 도구가 아직 없다** (sast/85 도 같은 말) — 둘지는 오케스트레이터가 정한다.

## 동적분석으로 확인해야 할 것

- (Q91 **D1 과 겹침**) `ntdll!RtlpHpOptIntoSegmentHeap +0x190` — `r rax` (블록 주소) · `db @rax+0x11a8 L0x10`. 기대 `0` (오프라인으로 이미 `0`) — 라이브 일치 확인용, 선택.
- 안 탄다고 한 것들이 정말 안 타는지 (멈춤점이 **안 걸리면** 맞음): `LdrpMapDllNtFileName +0x4d7` (COD, 접근 거부 때만), `LdrpInitializeProcess +0x19ef` (`LdrpInitShimEngine`), `SbObtainTraceHandle +0x5c` (ETW 갈래), `SbUpdateSwitchContextBasedOnDll +0x1ea` (갱신 갈래 잠금), `RtlInitializeHeapManager +0x56` (`SbSelectProcedure`).
- 탄다고 한 것: `SbUpdateSwitchContextBasedOnDll +0xd9` (ret 자리에서 `[R14+0xc0]` 값 — sast/109 사진과 같은지, 모듈마다).
- kernelbase `CreateFileInternal +0x2f5` 가 초기화 범위 안에서 불리는지 (불리면 `SbSelectProcedure` 가 ctx 를 읽음) — 정적으로는 부르는 길만 있고 불리는지는 모름.
- shim 이 걸린 exe 에서만 켜지는 갈래 (`+0x210` ≠ 0, `+0x11aa`·`+0x11b2` 켜짐, ETW 켜짐) 는 test.exe 로 못 봄 — 필요하면 다른 샘플 (요청서 범위 밖).
- 추천 멈춤점 정리: `RtlpHpOptIntoSegmentHeap +0x190` (D1), `LdrpInitializeProcess +0x19dc` (`+0x210`), `SbUpdateSwitchContextBasedOnDll +0x47` (`[ctx+8]`) · `+0xd9`.

## 다음 요청 후보

1. **정적 (apphelp 쓰는 쪽, 범위 넓음)**: 블록 `+0x210` · `+0x11a8` · `+0x11aa` · `+0x11b2` 를 **누가 켜나** — 부모 `BasepGetAppCompatData` 안에서는 못 찾았다. apphelp (`ApphelpQueryCompat…` 류) · kernel32 `BasepCheckAppCompat` · `BasepInitializeApphelpGlobals` · `SbpGetShimData` 가 후보. 요청서에서 apphelp 본문은 이름만이라 이번엔 안 봄.
2. **정적 (작게)**: `SbpDetermineDllContext` 가 부르는 `SbpRetrieveCompatibilityManifest` 와 `SbpResolveBasedOnName` — DLL 마다 어떤 칸을 읽어 `SbSupportedOsList` 칸을 고르나 (PE_emulator 가 `SwitchBackContext` 를 채우려면).
3. **PE_emulator 쪽 결정 (요청서 범위 밖)**: 모듈 `+0xc0` · `SbDynamicContextDllCount +4` 를 채울지. 채우려면 `PEB+0x2d8` 에 최소 블록 (`+0x7e0 ~ +0x838` 의 위 값) 을 두고 `SbUpdateSwitchContextBasedOnDll` 줄이 필요.
4. **도구**: 사진 `memory.dmp` 오프라인 읽기 도구를 레포에 둘지 (오케스트레이터).
5. **dast (선택)**: 위 "동적분석으로 확인해야 할 것" 의 라이브 일치 (Q91 D1 과 함께 한 번에).

## 판정

**분석성공.** 질문 1 은 ntdll 안 PEB `+0x2d8` 읽기 전수 (9줄·함수 6) 와 갈래 · 사진 구간, 2 는 ntdll 이 읽는 칸 표 + 부모가 쓰는 줄 대조, 3 은 사진 블록 값 (`[덤프 오프라인]`) 으로 갈래별 결과, 4 는 baggage 줄 제안. 쓰는 줄을 못 찾은 칸 4개 (`+0x210`, `+0x11a8`, `+0x11aa`, `+0x11b2`) 는 `[공백]` 으로 남김. 스크립트 문제 없음.

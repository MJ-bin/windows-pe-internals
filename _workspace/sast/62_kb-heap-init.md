# 62 kb-heap-init — 정적 분석 요청

- 날짜: 2026-11-06
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run67
- 대기열: **Q128** (PE_emulator 요청 16 의 급한 셋 + 4번. 사용자 결정 2026-11-06: Q128 → Q129 → Q104 VM 단계)
- 관련: flow.md 3158줄 쯤 (`_KernelBaseBaseDllInitialize` ret 대표: `+0x1b2 KernelBaseDllInitializeMemoryManager` → `+0x1d6 RtlCreateTagHeap` → `+0x2d9 CsrClientConnectToServer` …), 3162줄 쯤 (kernel32 `_BaseDllInitialize` ret: `+0x73 RtlImageNtHeader` → `+0x99 RtlSetThreadPoolStartFunc` → `+0xba LdrSetDllManifestProber` …), run15·run20 (kernel32 올리기), run46 (프로세스 힙), Q84 (P1a ~ P1b 쪼개기 — `_KernelBaseBaseDllInitialize`)
- 원문 (읽기만): `../PE_emulator/_workspace/00_planner/103_kernelbase-globals.md` (A/B/C 표), PE_emulator 105·112

## 무슨 일이 있었나

- PE_emulator 는 kernelbase·kernel32 DllMain 을 진짜로 돌리지 않고 모방한다 (직접 쓰기 + 진짜 ntdll 함수 덫). 본문 asm 이 없는 자리는 공사 중으로 두었고, 그 때문에 셋이 막혔다:
  - (a) **가장 급함 (112)**: 은퇴 뒤 test.exe 가 `OutputDebugStringA → CreateDBWinMutex → GlobalAlloc → RtlAllocateHeap(BaseHeap = 0)` 에서 멈춤. `KernelBaseDllInitializeMemoryManager` 가 비어 있어 `BaseHeap` 이 0
  - (b) `RtlUnicodeStringToAnsiString` 이 없으면 `BaseAnsiCommandLine` 이 비어 `GetCommandLineA` 가 빈 값을 줄 수 있음 `[추측]`
  - (c) `LdrSetDllManifestProber` 가 공사 중이라 `LdrpManifestProberRoutine` 이 0 → `LdrpFindDllActivationContext` 가 건너뜀 → exe import (dll1·dll2) 길이 진짜와 갈릴 수 있음 `[추측]`

## 알고 싶은 것

1. **kernelbase `KernelBaseDllInitializeMemoryManager`** (rva `0xf8344`, 155B, 부르는 곳 `_KernelBaseBaseDllInitialize +0x1ad/+0x1b2`)
   - 인자, 쓰는 전역 전부 (`BaseHeap`·`KernelBaseGlobalData+0x40 ~ +0x57`·그 밖 — 이름·크기는 PDB 로), **값의 출처** (PEB `ProcessHeap`? 다른 PEB 칸? 새 힙?), 하위 호출·syscall
   - PE_emulator 에 줄 한 줄: "`BaseHeap` = 무엇" (예: `PEB.ProcessHeap` 그대로)
2. **`KernelBaseGlobalData+0x40 ~ +0x57` 을 쓰는 곳 전부** (kernelbase `_refs` — `KernelBaseGlobalData` rva 는 `.asm` 주석 `0x1803aeec0` 기준 `+0x40` = `0x1803aef00`) — 함수+오프셋+쓰는 값. 1 이 아닌 곳이 있으면 이름만
3. **ntdll `RtlUnicodeStringToAnsiString`** (rva `0x4b9f0`, 807B) — 부르는 곳 `_KernelBaseBaseDllInitialize +0x490/+0x497` (`RCX = &BaseAnsiCommandLine`, `RDX = &BaseUnicodeCommandLine` (`+0x474`), `R8B = 1` (할당)) 에서 타는 갈래만 자세히:
   - 길이 셈 (`RtlUnicodeStringToAnsiSize` 류·NLS 코드 페이지 표 — run57 `NtGetNlsSectionPtr`·ACP 표와 이어지나), 할당 (`R8B = 1` 이면 어느 힙·크기 — `RtlAllocateStringRoutine`?), 변환 함수 (`RtlUnicodeToMultiByteN` 등 — 이름만, 줄기면 한 단계), 실패 상태값
   - syscall 이 있나
   - 쓰는 것: 출력 `ANSI_STRING` 세 칸 (`Length`·`MaximumLength`·`Buffer`) 의 값 꼴 (test.exe 명령줄 기준 — ASCII 만이면 길이 = 유니코드 길이/2?)
4. **ntdll `LdrSetDllManifestProber`** (rva `0x10c7e0`, 22B) — 인자 셋을 어디에 쓰나. kernel32 `_BaseDllInitialize +0xb3/+0xba` 가 넘기는 값 (`RCX = kernel32!BasepProbeForDllManifest` rva `0x23f90`, `RDX = kernel32!BasepRegenerateActCtxWithLanguage` rva `0x61520`, `R8 = __imp_ReleaseActCtx`) → `LdrpManifestProberRoutine` (rva `0x1d23c0`) 와 나머지 둘의 전역 이름. 같은 함수의 둘째 호출 `_BaseDllInitialize +0x3af/+0x3b6` 은 무엇을 넘기나 (해제 길?)
   - (b) **시간 순서**: kernel32 `_BaseDllInitialize` (DllMain) 가 도는 때가 dll1·dll2 의 `LdrpMapAndSnapDependency +0x5e/+0x63 LdrpFindDllActivationContext` 보다 **앞인가 뒤인가** (flow.md 그림 2·2.5·2.6·2.8 기준 — kernel32 초기화 루틴이 `LdrpInitializeGraphRecurse` (`LdrpInitializeProcess +0x1bb6`) 에서 도는지, 그 전 `LdrpInitializeKernel32Functions` 쪽에서 도는지). 뒤라면 test.exe 의 dll1·dll2 import 때 진짜도 `LdrpManifestProberRoutine = 0` 이라 PE_emulator 의 0 이 맞다
   - (c) `LdrpFindDllActivationContext` (rva `0x96000`, 495B) — `+0x6` 의 0 검사, 0 아닐 때 `+0x7e/+0x83` 간접(CFG) 호출 `LdrpManifestProberRoutine` 인자·반환을 어떻게 쓰나 (짧게, 갈래 조건과 쓰는 칸)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernelbase | `^KernelBaseDllInitializeMemoryManager$` | 1 | 1 |
| kernelbase | (refs) `KernelBaseGlobalData` `+0x40 ~ +0x57`, `BaseHeap` | — | 1·2 |
| ntdll | `^RtlUnicodeStringToAnsiString$` | 1 | 3 (줄기 위 하위는 지도로 찾아 그것만) |
| ntdll | `^LdrSetDllManifestProber$`, `^LdrpFindDllActivationContext$` | 1 | 4 |
| ntdll | (refs) `LdrpManifestProberRoutine` `0x1d23c0` (와 4 에서 나오는 나머지 두 전역) | — | 4 |
| kernelbase / kernel32 | `_KernelBaseBaseDllInitialize.asm`, `_BaseDllInitialize.asm` (있음 — 다시 뽑지 않음) | — | 부르는 자리 |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where <모듈 파일 이름> <이름>` (kernelbase 는 `kernelbase.dll`). DLL 진입점은 Ghidra 가 `entry` 로 잡는다.
- 공용 부품 (`RtlAllocateHeap`·`memcpy`·락) 은 이름만.

## 지도·`.asm` 에서 본 것 (오케스트레이터)

- `_KernelBaseBaseDllInitialize` (kernelbase rva `0x4e6c0`, 1835B): `+0x1a6 MOV byte [BasepIsSecureProcess],1` · `+0x1ad CALL KernelBaseDllInitializeMemoryManager ret=+0x1b2` · `+0x1db MOV [KernelBaseGlobalData],EAX` · `+0x474 LEA RDX,BaseUnicodeCommandLine` · `+0x47b MOV R8B,1` · `+0x47e LEA RCX,BaseAnsiCommandLine` · `+0x485 MOVUPS XMM0,[RAX+0x70]` (`RAX = [RSI+0x20]` — `ProcessParameters`?, `+0x70` = `CommandLine`) · `+0x489` `BaseUnicodeCommandLine` 에 복사 · `+0x490 CALL [__imp_RtlUnicodeStringToAnsiString] ret=+0x497`
  - `KernelBaseGlobalData` = kernelbase `0x1803aeec0` (`.asm` 주석). `BaseAnsiCommandLine` = `0x1803af050`, `BaseUnicodeCommandLine` = `0x1803af040`
- kernel32 `_BaseDllInitialize`: `+0x8b LEA RCX,BaseCreateThreadPoolThread` (`+0x99 RtlSetThreadPoolStartFunc` 인자) · `+0x9e MOV R8,[__imp_ReleaseActCtx]` · `+0xa5 LEA RDX,BasepRegenerateActCtxWithLanguage` · `+0xac LEA RCX,BasepProbeForDllManifest` · `+0xb3 CALL [__imp_LdrSetDllManifestProber] ret=+0xba` · 둘째 `+0x3af/+0x3b6`
- ntdll 지도: `LdrpMapAndSnapDependency +0x5e/+0x63 LdrpFindDllActivationContext` · `LdrpFindDllActivationContext +0x7e/+0x83` 간접(CFG) `cfg:LdrpManifestProberRoutine` · `+0x158 LdrResFindResourceDirectory` · `+0x162 RtlRunOnceExecuteOnce` · `+0x188 RtlReleaseActivationContext`
- `_index.tsv`: `KernelBaseDllInitializeMemoryManager` `0xf8344` 155B · `RtlUnicodeStringToAnsiString` `0x4b9f0` 807B · `LdrSetDllManifestProber` `0x10c7e0` 22B · `LdrpFindDllActivationContext` `0x96000` 495B · kernel32 `BasepProbeForDllManifest` `0x23f90` 275B · `BasepRegenerateActCtxWithLanguage` `0x61520` 562B

## 이미 아는 것

- `[정적]` flow.md 3158·3162줄: 두 DllMain 의 ret 대표 줄 (위)
- `[관찰]` run44 사진: `BaseHeap`·`KernelBaseGlobalData`·`BaseAnsiCommandLine`·`LdrpManifestProberRoutine` 의 P1a·P1b·P2 값이 있으면 같이 대조해 줘 (`ms/logs/04_baggage-read_P*.txt` — 없으면 "사진에 없음")
- PE_emulator 원문: `BaseHeap = 0` 이라 `RtlAllocateHeap` 에서 멈춤

## 이번에 안 하는 것

- Q129 (요청 16 의 나머지 — `ConsoleInitialize`·`CsrClientConnectToServer`·`BaseNlsDllInitialize` 등)
- `RtlUnicodeToMultiByteN` 등 변환 본문 깊이 (이름·줄기 한 단계만)
- `BasepProbeForDllManifest` 본문 (kernel32 — 4(b) 가 "앞" 일 때만 이름·syscall 한 줄)

## 받고 싶은 것

- 질문별 답 (예/아니오·이름) + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋. 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- PE_emulator 가 그대로 쓸 **작은 표**: 함수 × (인자 · 쓰는 칸 · 값 출처 · 하위 호출 · syscall · 반환)
- 1번·4(b) 한 줄 결론 (PE_emulator 가 바로 쓸 수 있게)
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 동적 몫은 Q104 VM 단계에 얹을 멈춤점·읽을 칸으로
- 짐 지도 반영거리 (kernelbase·ntdll 전역 줄)

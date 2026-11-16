# 2026-11-16 (작성일) run108 — 모듈 항목 `ProcessStaticImport` 와 간선 칸 비트 0 전수 (Q72, 정적만, 짐 지도 바퀴): **`Flags (+0x68)` 비트 5 (`0x20`) 를 켜는 줄은 ntdll 전체에서 `LdrpAllocateModuleEntry +0x194` 하나**, 끄는 줄도 하나 (`LdrpFreeReplacedModule +0x15`, 바꿔치기된 항목을 버릴 때). 조건 칸 `ntdll+0x1cc920` = **`LdrpDllNotificationLock.OwningThread`** (알림 락 주인 스레드 ID). kernel32·kernelbase 가 0 인 이유 = **`LdrpInitializeKernel32Functions` (`LdrpInitializeProcess +0x199d`) 가 `LdrInitState ← 1` (`+0x1a4c`) 보다 먼저 올리기 때문** — 사진 6 모듈 값과 전부 맞음. **간선 칸 `+0x18` 비트 0 을 켜는 곳은 넷** (`LdrpRecordModuleDependency +0x11a` · `LdrpFastpthReloadedDll +0x1a5` · `LdrpBuildForwarderLink +0x11c` · `LdrpLoadDependentModuleInternal +0x17d9` — 마지막은 **탈 수 없는 길로 보임** `[정적 추정]`), 지우는 곳 둘 (**`LdrpLoadDependentModuleInternal +0xede`** · `LdrpMergeNodes +0xe3`). test.exe 정적 import 간선은 **비트 0 = 0** `[정적]`. `LdrpRecordModuleDependency` caller 는 정말 하나 (`LdrpInitializeNtdllDataTableEntry +0x23e`, 핫패치 갈래만).

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표와 호출 관계 트리.)

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 사진 덤프 읽기 로그를 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`** (기준과 같음). 다시 analyze 안 함, export 한 번 (새 함수 7개, exit 0 — 결과서).
  - 버전 대조 (결과서 + flow-writer `.asm`): `LdrpInitializeProcess +0x1bb6 / ret +0x1bbb → LdrpInitializeGraphRecurse` 그대로. `LdrpAllocateModuleEntry` 부르는 곳 6곳이 `objdump` 전수와 `_calls.tsv` 에서 줄마다 같다. `LdrpDllNotificationLock` 40바이트 파일 값이 사진 P1a · P1b · P2 와 같다 → 바이너리가 타깃 VM 것과 같다고 본다.
- 커널은 안 건드렸다 (ntoskrnl pdb 가 기준과 같은지만 결과서가 확인).
- "ntdll 전체에서 그 줄 찾기" 는 sast/100 이 만든 스크래치 `objdump -d` 풀이 (`ntdll.dis`, 421,500줄, 커밋 안 함) 를 다시 썼다 (결과서, run106 · run107 과 같은 방법).
- **sast/105 ~ 119 결과는 이 문서에 안 넣었다** (다음 바퀴들 몫).

## 목적

- **Q72** (Q54 를 2026-10-24 사용자 결정으로 문서로 닫으며 남긴 전수 확인, run27 · run40):
  - (a) 항목 `Flags (+0x68)` 에 `0x20` (`ProcessStaticImport`) 을 켜는 곳이 `LdrpAllocateModuleEntry +0x194` 하나뿐인가. 조건 칸 `ntdll+0x1cc920` 의 뜻.
  - (b) 간선 칸 `+0x18` 비트 0 을 켜는 곳 · 지우는 곳 · 읽는 곳 전부. 같은 `+0x18` 을 쓰는 다른 칸과 어떻게 가르나.
  - (c) `LdrpRecordModuleDependency` caller 가 정말 `LdrpInitializeNtdllDataTableEntry +0x23e` 하나인가 (직접 + 간접).
- 같이 (문서만): **Q123** — run57 호출 관계 트리 `ExInitializeNls` 오프셋을 트리 규칙 (ret 자리) 대로.
- 앞 바퀴 짝: run22 (`LdrpAllocateModuleEntry +0x194` 첫 발견, sast/17), run23 (`LdrpLoadDependentModuleInternal` 간선 칸, sast/18), run27 (`LdrpInitializeGraphRecurse` 가 비트 0 간선을 건너뜀, sast/22), run40 (Q54 를 문서로 닫은 줄), run41 (간선 비트 이야기가 이어진 바퀴 — 오케스트레이터 지정).

## 방법

- 정적: `_workspace/sast/103_static-import-flags.md` — ntdll
  - 이미 있던 `.asm` · `.c` 를 그대로 읽음 (다시 안 뽑음): `LdrpAllocateModuleEntry`, `LdrpRecordModuleDependency`, `LdrpInitializeGraphRecurse`, `LdrpLoadDependentModuleInternal`, `LdrpInitializeNtdllDataTableEntry`, `LdrpInitializeProcess`, `LdrpInitializeKernel32Functions`.
  - 새로 뽑음 (depth 0, 오프셋 대조용): `^(LdrpFastpthReloadedDll|LdrpBuildForwarderLink|LdrpMergeNodes|LdrpLoadContextReplaceModule|LdrpSignalModuleMapped|LdrpInitializeShimDllDependencies|LdrpFreeReplacedModule)$` (7개). `_calls.tsv` 4059 → 4064 줄.
  - 새 `_refs` 하나: `ms/disassemble/ntdll/_refs/LdrpDllNotificationLock.tsv` (20줄).
  - 전수 검색: `objdump` 풀이에서 (1) `[reg+0x68]` 와 항목 안 고리 6종 기준 같은 칸 쓰기 전부 (2) `lea reg,[x+0x68]` (3) 항목 통째 복사 (크기 `0x138`) (4) 바이트 패턴 교차 (`OR imm` / `AND imm` ModRM `disp8=0x68`). 간선은 (1) `[reg+0x18]` 비트 연산 전부 (2) `mov` 뒤 8줄 안 비트 연산 (3) 되살리기 관용구 (4) 크기 `0x20` + `NtdllBaseTag` 힙 할당 (5) 로더 함수의 `[reg+0x18]` 접근 하나씩 배정 (6) 들어오는 고리 쪽 `[reg+0x8]` · 마스크 `-2` / `-8`. caller 는 `call` / `jmp` / `jcc` / 주소 상수 / 데이터 바이트 (8바이트 VA · 4바이트 RVA) 전 구역.
- 결과서: `_workspace/sast/103_static-import-flags_result.md` (**분석성공** — 정적 추정으로 남은 것: `+0x17d9` 죽은 길, `LdrpMergeNodes` 호출 조건).
- flow-writer 가 다시 본 것 (`.asm` 줄):
  - `LdrpAllocateModuleEntry` (`+0xc2` · `+0x105` · `+0x17a` · `+0x183` · `+0x187` · `+0x18e` · `+0x194` · `+0x198`), `LdrpFreeReplacedModule` (`+0x15` · `+0x1c` · `+0x2b`)
  - `LdrpFastpthReloadedDll` (`+0x92` · `+0x95` · `+0x198` · `+0x1a5` · `+0x1dd` · `+0x1e0` · `+0x1f6`), `LdrpBuildForwarderLink` (`+0x17` · `+0x1a` · `+0x10b` · `+0x11c` · `+0x154` · `+0x157` · `+0x16d`)
  - `LdrpLoadDependentModuleInternal` (`+0x929` · `+0xed5` · `+0xed8` · `+0xedb` · `+0xede` · `+0xf33` · `+0xf4a` · `+0xf4d` · `+0xf8f` · `+0xf96` · `+0xfa5` · `+0x179d` · `+0x17bd` · `+0x17d6` · `+0x17d9` · `+0x17dc`)
  - `LdrpMergeNodes` (`+0xe3` · `+0xf6` · `+0x1b4` · `+0x25c`), `LdrpCondenseGraphRecurse +0xd3`, `LdrpInitializeGraphRecurse` (`+0x26` · `+0x3a` · `+0x3e`), `LdrpInitializeShimDllDependencies` (`+0xa6` · `+0xaa` · `+0xcd`), `LdrpLoadContextReplaceModule` (`+0x75` · `+0x7c` · `+0x80` · `+0xe7`), `LdrpSignalModuleMapped` (`+0x2c` · `+0x30` · `+0x52` · `+0x59`)
  - `LdrpInitializeNtdllDataTableEntry` (`+0x67` · `+0x140` · `+0x143` · `+0x22c` · `+0x23e`), `LdrpInitializeKernel32Functions` (`+0xbb` · `+0x151`), `LdrpInitializeProcess` (`+0x119d` · `+0x1253` · `+0x1256` · `+0x126a` · `+0x1359` · `+0x1577` · `+0x199d` · `+0x1a4c` · `+0x1aa4` · `+0x1b1e` · `+0x1bb6`), `LdrpLoadDllInternal` (`+0xb3` · `+0x290`), `LdrpLoadDll` (`+0x71` · `+0xf1` · `+0x1f8` · `+0x243`)
  - `_calls.tsv` (ntdll): 아래 표의 줄 전부. `alias:` 줄은 이번 문서에 없다.
  - 사진 로그: `ms/logs/04_baggage-read_P1a.txt` · `_P1b.txt` · `_P2.txt` (아래 "사진 로그 대조").

## 결과

비유 (모듈 항목 = 손님 명찰, `ProcessStaticImport` = 명찰의 "초대 손님" 도장, 간선 = 두 손님을 잇는 끈, 비트 0 = 끈에 붙인 "따라가지 마" 꼬리표):

- "초대 손님" 도장은 **한 사람 (`LdrpAllocateModuleEntry`) 만 찍는다.** 그것도 "본 행사 손님 맞이 시간 (`LdrInitState == 1`)" 에, "안내 방송실 (알림 락) 안에서 부른 손님" 이 아닐 때만.
- kernel32 · kernelbase 는 **본 행사 전에 먼저 와 있던 사람**이라 도장이 없다. dll1 · dll2 는 본 행사 시간에 와서 도장이 있다.
- 끈의 꼬리표는 넷이 붙일 수 있고 둘이 뗀다. test.exe 의 import 끈은 만들 때 일부러 꼬리표를 떼서 **모두 따라가는 끈**이다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x119d` | `+0x11a2` | `LdrpInitializeNtdllDataTableEntry` (첫째, `R8 = 0`) | ntdll 항목 (`LdrInitState` 0) | [정적] (run35 그대로) |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x67` | `+0x6c` | `LdrpAllocateModuleEntry` | ntdll 항목 — 상태 0 이라 `0x20` 안 켬 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x126a` | `+0x126f` | `LdrpInitializeNtdllDataTableEntry` (둘째, `R8 = [LdrpNtDllDataTableEntry]`) | `+0x1253 TEST RCX,RCX` · `+0x1256 JZ +0x127b` — 핫패치 문맥 첫 8바이트 ≠ 0 일 때만 | [정적] (run105 그대로) |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | `+0x23e` | `+0x243` | `LdrpRecordModuleDependency` (**유일한 caller**) | `+0x140 TEST RBP,RBP` · `+0x143 JNZ +0x22c` — 셋째 인자 ≠ 0 (둘째 호출) 일 때만. `+0x22c RCX = [LdrpNtDllDataTableEntry]` (원래 ntdll) · `RDX` = 새 항목 · `R8 = 0` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1359` | `+0x135e` | `LdrpAllocateModuleEntry` | exe 항목 (상태 0) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1577` | `+0x157c` | `LdrpAllocateModuleEntry` | AppVerifier 복제 항목 (`+0x1568 AvrfAppVerifierMode ≠ 0` 일 때만, 상태 0) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x199d` | `+0x19a2` | `LdrpInitializeKernel32Functions` | **`+0x1a4c` (상태 ← 1) 보다 앞** | [정적] (run20 그대로) |
| `ntdll!LdrpInitializeKernel32Functions` | `+0xbb` / `+0x151` | `+0xc0` / `+0x156` | `LdrLoadDll` (kernel32 / kernelbase 이름) | 상태 0 때 — kernelbase 는 kernel32 import 로 같이 올라옴 | [정적] (run20 그대로) |
| `ntdll!LdrpInitializeProcess` | `+0x1aa4` | `+0x1aa9` | `LdrpMapAndSnapDependency` | exe import (dll1 · dll2) — 상태 1 구간 (`+0x1a4c` ~ `+0x1b1e`) | [정적] (run19 그대로) |
| `ntdll!LdrpLoadDependentModuleInternal` | `+0x1157` | `+0x115c` | `LdrpAllocateModuleEntry` | DLL 항목 (상태 1 구간이면 1) | [정적] |
| `ntdll!LdrpLoadDependentModuleInternal` | `+0x929` | `+0x92e` | `RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 0x280000, 0x20)` | 정적 import 간선 칸 ("미리 만든 상자") | [정적] (run23 그대로) |
| `ntdll!LdrpLoadDependentModuleInternal` | `+0x17bd` | `+0x17c2` | `RtlAllocateHeap(…, 태그 0x240000, 0x20)` | `+0xf4d JZ` (RCX == 0) 로만 들어옴 — **죽은 길로 보임** `[정적 추정]` | [정적] |
| `ntdll!LdrpLoadDllInternal` | `+0xb3` | `+0xb8` | `LdrpFastpthReloadedDll` | 부모 인자 = 이 함수 6번째 인자 | [정적] (run21 그대로) |
| `ntdll!LdrpLoadDllInternal` | `+0x290` | `+0x295` | `LdrpBuildForwarderLink` | 같은 부모 인자 | [정적] (run21 그대로) |
| `ntdll!LdrpFastpthReloadedDll` | `+0x198` | `+0x19d` | `RtlAllocateHeap(…, 0x20)` | 부모 ≠ NULL · 부모 `LoadCount ≠ 0` · 자식 ≠ 부모 · 이미 같은 간선 없음 | [정적] |
| `ntdll!LdrpBuildForwarderLink` | `+0x10b` | `+0x110` | `RtlAllocateHeap(…, 0x20)` | 부모 ≠ NULL (`+0x17` · `+0x1a JE +0xc5`) · 같은 조건 | [정적] |
| `ntdll!LdrpRecordModuleDependency` | `+0x10d` | `+0x112` | `RtlAllocateHeap(…, 0x20)` | 호출자가 상자 (R8) 를 안 줬을 때 (`+0x77` · `+0x7a JZ +0xf4`) | [정적] |
| `ntdll!LdrpCondenseGraphRecurse` | `+0xd3` | `+0xd8` | `LdrpMergeNodes` (유일한 caller) | 고리 (순환 의존) 를 합칠 때 | [정적] (run24 그대로) |
| `ntdll!LdrpInitializeShimDllDependencies` | `+0xcd` | `+0xd2` | `LdrpInitializeGraphRecurse` | `ShimDll` 항목의 `Dependencies` 를 돌며 (비트 0 간선은 `+0xa6` 에서 건너뜀) | [정적] |
| `ntdll!LdrpLoadContextReplaceModule` | `+0xe7` | `+0xec` | `RtlFreeHeap` (뗀 간선 상자) | | [정적] |
| `ntdll!LdrpSignalModuleMapped` | `+0x59` | `+0x5e` | `LdrpQueueWork` | 부모 노드 카운터 0 → `+0x52 State := 4` 뒤 | [정적] (run24 그대로) |
| `ntdll!LdrpFreeReplacedModule` | `+0x2b` (꼬리 JMP) | — | `LdrpDereferenceModule` | `+0x15` 비트 5 끔 → `+0x1c ReferenceCount := 1` 뒤 | [정적] |
| `ntdll!LdrpLoadDllInternal` · `LdrpLoadDependentModuleInternal` · `LdrpSnapModule` · `LdrpFreeLoadContext` | `+0x33c` · `+0x133c` · `+0xfc` · `+0x9d` | `+0x341` · `+0x1341` · `+0x101` · `+0xa2` | `LdrpFreeReplacedModule` | 같은 DLL 이 다른 항목으로 바꿔치기됐을 때 (`LdrpHandlePendingModuleReplaced +0x27` 도 — 결과서 `objdump`, `.asm` 없음) | [정적] |

### (a) `ProcessStaticImport` (`Flags` 비트 5) — 켜는 줄 하나, 끄는 줄 하나 (결과서 1)

- **켜는 줄 (ntdll 전체 유일)** `[정적]`: `LdrpAllocateModuleEntry +0x194 OR dword [RBX+0x68],0x20`.
  - 길: `+0xc2 CMP [LdrInitState],1` → `+0x105 JZ +0x17a` → `+0x17a MOV RAX,GS:[0x30]` (TEB) → `+0x183 MOV RCX,[RAX+0x48]` (TEB `ClientId.UniqueThread`) → `+0x187 CMP [ntdll+0x1cc920],RCX` → `+0x18e JZ +0x107` (같으면 건너뜀) → 다르면 `+0x194` → `+0x198 JMP +0x107`.
  - 곧 **`LdrInitState == 1` 이고 지금 스레드가 알림 락 주인이 아닐 때.** (2026-10-24 결정문 괄호 · flow.md 2.6 의 말과 같다.)
- **끄는 줄 (유일)** `[정적]`: `LdrpFreeReplacedModule +0x15 AND dword [RBX+0x68],0xffffffdf`. 조건 없음. 바꿔치기돼 버려질 항목을 풀기 직전 (`+0x1c ReferenceCount := 1` → `+0x2b` 꼬리 JMP `LdrpDereferenceModule`). 부르는 곳 다섯 (위 표 끝 줄).
- **비트 5 와 상관없는 `Flags` 쓰기 전부** 는 결과서 1-E 표 (비트 0 · 1 · 2 · 3 · 6 · 7 · 8 · 9 · 10 · 13 · 15 · 19 · 20 · 21 · 22 · 24 · 26 · 28). AppVerifier 복제 `LdrpInitializeProcess +0x15b8 OR [RBX+0x68],ECX` 는 원본 `Flags` 를 옮기는 줄이라 원본에 `0x20` 이 없으면 안 옮긴다 (test.exe 는 `AvrfAppVerifierMode` 0 이라 이 갈래 자체를 안 탐 — 짐 지도 3.9).
- **조건 칸 `ntdll+0x1cc920` = `LdrpDllNotificationLock` (rva `0x1cc910`) `+0x10` = `RTL_CRITICAL_SECTION.OwningThread`** `[정적]`:
  - `pdb_aliases.py --where ntdll.dll 0x1cc920` → 공개 심볼에 없음. 가까운 앞 이름 `LdrpDllNotificationLock` (`0x1cc910`), 다음 이름 `LdrpTlsList` (`0x1cc938`) 라 덩치 `0x28` = `RTL_CRITICAL_SECTION` 크기 (결과서). 그 앞 `0x1cc8e0` 에 `LdrpDllNotificationLock_DEBUG`.
  - 칸 주소를 직접 쓰는 줄은 없다 (`_refs/LdrpDllNotificationLock.tsv` 20줄 — `mov [rip+..]` 꼴 0). 쓰는 건 `RtlEnterCriticalSection` / `RtlLeaveCriticalSection` 이 포인터로 한다. 락을 잡는 쪽: `LdrpSendDllNotifications` · `LdrpSendPostSnapNotifications` · `LdrRegisterDllNotification` · `LdrUnregisterDllNotification` · `LdrpUnloadNode` · `LdrpDynamicShimModule` · `LdrpLoadShimEngine` (결과서 1-B, 짝 오프셋은 결과서).
  - **"TEB `+0x48` 과 같나" 를 읽는 줄 5**: `LdrpAllocateModuleEntry +0x187`, `LdrpMapDllWithSectionHandle +0x232` (같으면 `LdrpCompleteMapModule` 건너뜀), `LdrpPrepareModuleForExecution +0x24` (같으면 바로 돌려줌 — flow.md 2.7 "1." 과 같은 줄), `LdrGetProcedureAddressForCaller +0x18e` · `+0x925` (결과서).
  - 뜻 `[추측]`: `LdrpSendDllNotifications` 가 락을 쥔 채 알림 콜백을 부르니 "주인 == 지금 스레드" = **알림 콜백 안에서 부른 로드**. 그 경우 도장을 안 찍는다.
- **`LdrpAllocateModuleEntry` 부르는 곳 6 (`objdump` 전수 = `_calls.tsv`)** 과 그때 `LdrInitState` (위 표): ntdll · exe · AppVerifier 복제는 상태 0, DLL 둘 (`LdrpLoadDependentModuleInternal +0x1157`, `LdrpFindOrPrepareLoadingModule +0x475`) 과 자리표시 (`LdrpAllocatePlaceHolder +0xd0`) 는 그 순간 상태대로. `.rdata` 에 `0x6fab0` 이 4번 나오지만 전부 풀기(unwind) 사슬 칸이라 함수 포인터가 아니다 (결과서).
- **`LdrInitState` 쓰는 줄** (`_refs/LdrInitState.tsv`, 결과서): `_LdrpInitialize +0x101` (0), **`LdrpInitializeProcess +0x1a4c` (1)**, `+0x1b1e` (2), `+0x17dd` · `_LdrpInitialize +0x205` (3). 상태 1 = **`+0x1a4c` ~ `+0x1b1e`**, 그 안에 exe import 올리기 `+0x1aa4`.
- **test.exe 에서 누가 켜지나** `[정적]` = `[관찰]`:
  - exe · ntdll — 상태 0 때 만들어짐 → 꺼짐.
  - **kernel32 · kernelbase — 꺼짐.** `LdrpInitializeKernel32Functions` (`+0x199d`) 가 `+0xbb LdrLoadDll` (kernel32), `+0x151 LdrLoadDll` (kernelbase 이름) 로 **`+0x1a4c` 보다 앞에서** 올린다 (kernelbase 는 kernel32 import 로 같이). 그때 상태 0.
  - dll1 · dll2 — `+0x1aa4 LdrpMapAndSnapDependency` (상태 1) 에서 처음 만들어짐 → **켜짐**. 알림 락 주인은 0 이라 (사진) 건너뛰는 갈래가 설 자리가 없다.

### (b) 간선 칸 `+0x18` 비트 0 — 켬 4 · 지움 2 · 읽음 (결과서 2)

- **간선 상자** `[정적]`: 크기 `0x20`, `RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 태그, 0x20)` 로 만드는 곳 **5** (태그 `0x240000` 4곳 — `LdrpRecordModuleDependency +0x10d` · `LdrpLoadDependentModuleInternal +0x17bd` · `LdrpFastpthReloadedDll +0x198` · `LdrpBuildForwarderLink +0x10b`, 태그 `0x280000` 1곳 — `LdrpLoadDependentModuleInternal +0x929`). **`HEAP_ZERO_MEMORY` 가 없다** (쓰레기 값으로 시작).
  - 칸 배치 (run23 과 같음): `+0x0` 출발 노드 `Dependencies` 고리 · `+0x8` 도착 노드 · `+0x10` 도착 노드 `IncomingDependencies` 고리 · `+0x18` 출발 노드 포인터 (**하위 비트에 표지**). PDB 에 이름 없음.
- **켜는 곳 (비트 0 := 1, 전수 4)**:

| 함수 + 오프셋 | 명령 | 조건 | test.exe |
|---|---|---|---|
| `LdrpRecordModuleDependency +0x11a` | `OR dword [RAX+0x18],1` (새 상자) | 호출자가 상자를 안 줬을 때 | 핫패치 갈래만 — 아래 (c) |
| `LdrpFastpthReloadedDll +0x1a5` | `OR dword [RAX+0x18],1` | 부모 ≠ NULL (`+0x92` · `+0x95 JE`) · 부모 `LoadCount ≠ 0` · 자식 ≠ 부모 · "자식 `State 9` 이고 (`LoadCount == -1` 또는 `0x20`)" 아님 · 같은 간선 없음 | 부모 인자 = `LdrpLoadDllInternal` 6번째 인자. **`LdrpLoadDll` 이 0 을 넣는다** (`+0x71 XOR R12D,R12D` → `+0xf1` / `+0x1f8` / `+0x243 MOV [RSP+0x28],R12`) → `LdrLoadDll` 길 (kernel32 준비 · `LoadLibraryExW`) 에선 안 탐 `[정적]` |
| `LdrpBuildForwarderLink +0x11c` | `OR dword [RAX+0x18],1` | 부모 ≠ NULL (`+0x17` · `+0x1a JE +0xc5`) · 같은 꼴 조건 | 같은 부모 인자 → `LdrLoadDll` 길에선 안 탐. 부모가 있는 길 = `LdrpLoadForwardedDll +0x2e1` · `LdrpLoadPatchImage +0xbf` (결과서) — test.exe 에서 포워더 길을 타는지는 **안 따라감** |
| `LdrpLoadDependentModuleInternal +0x17d9` | `+0x17d6 MOV EAX,[RAX+0x18]` · `OR EAX,1` · `+0x17dc MOV [RCX+0x18],EAX` | 들어오는 길 = `+0xf4d JZ +0x179d` (RCX == 0) 하나. **RCX 는 `+0xed5` 에서 읽어 `+0xed8` 로 이미 쓰인 값이라 0 일 수 없다** → 죽은 길로 보임 | `[정적 추정]` — 동적 D2 |

- **지우는 곳 (비트 0 := 0)**:
  - **`LdrpLoadDependentModuleInternal +0xede`** (`+0xed5 MOV RCX,[R8]` · `+0xed8 MOV EAX,[RCX+0x18]` · `+0xedb AND EAX,0xfffffffe` · `+0xede MOV [RCX+0x18],EAX`) — 6번째 인자가 가리키는 칸의 상자 (`+0x929` 로 만든 것) 의 비트 0 만 지움. 조건 없음. 쓰레기 값으로 시작하니 일부러 지우는 것 `[추측: 뜻]`.
  - **`LdrpMergeNodes +0xe3`** `MOV qword [RAX+0x18],RBX` — 합쳐져 없어질 노드의 간선 `+0x18` 을 합친 노드로 덮어씀 → 하위 비트 전부 0. caller 는 `LdrpCondenseGraphRecurse +0xd3` 하나 (순환 의존일 때). test.exe 는 고리가 없다고 보임 `[정적: 코드 모양]` (run24).
- **되살리는 관용구 4곳 (순효과 없음)**: 포인터를 통째로 쓰고 옛 비트 0 을 되살린다 — `LdrpRecordModuleDependency +0xb8 ~ +0xce`, `LdrpLoadDependentModuleInternal +0xf8f ~ +0xfa5`, `LdrpFastpthReloadedDll +0x1dd ~ +0x1f6`, `LdrpBuildForwarderLink +0x154 ~ +0x16d`.
- **읽는 곳**:
  - 비트 0 을 보고 건너뜀: **`LdrpInitializeGraphRecurse +0x3a`** (`TEST byte [RBX+0x18],1` · `+0x3e JNZ`, run27 그대로) 와 **`LdrpInitializeShimDllDependencies +0xa6`** (`+0xaa JNZ`, 새로).
  - 포인터로 읽음 (하위 3비트 가림 `AND r,-8`): `LdrpLoadContextReplaceModule +0x7c` / `+0x80` (간선 떼고 `+0xe7 RtlFreeHeap`), `LdrpSignalModuleMapped +0x2c` / `+0x30` (출발 노드 카운터 1 줄이고 0 이면 `State := 4` · `LdrpQueueWork`).
  - 가리지 않고 비교 · 옮김: `LdrpMergeNodes +0x1b4` / `+0x25c` (`+0xe3` 이 이미 덮어서 표지 없음).
- **같은 `+0x18` 인데 간선이 아닌 칸** (결과서 2-F): 노드 `LoadCount` (포인터가 항목 `+0x98 DdagNode` 에서 옴 — `OR [노드+0x18],-1` 이 "비트 0 켬" 처럼 보이지만 다른 칸), 내보내기 디렉터리 `NumberOfNames`, 로드 문맥 `+0x18`, 스택 홈 칸 · 스레드풀 등. 가르는 근거: (1) `0x20` 상자 + `NtdllBaseTag` 태그 할당에서 왔나 (2) 노드 `+0x28` / `+0x30` 고리를 돌아 얻었나 (3) `+0x18` 에 노드 포인터를 쓰고 비트 0 을 만지는 모양인가.
- **test.exe 정적 import 간선은 비트 0 = 0** `[정적]`: `LdrpLoadDependentModuleInternal` 이 `+0x929` 로 상자를 만들고 → `+0xede` 에서 비트 0 을 지우고 → `+0xf96 ~ +0xfa5` 에서 잇는다 (되살릴 옛 비트 0). 그래서 `LdrpInitializeGraphRecurse` 가 전부 따라간다. 초기화 동안 비트 0 간선이 생길 길은 핫패치 (`LdrpRecordModuleDependency`) · 포워더로 다른 DLL 을 올리는 길 (`LdrpLoadForwardedDll`) · 부모가 있는 `LdrpLoadDllInternal` 호출뿐.

### (c) `LdrpRecordModuleDependency` caller — 하나뿐 (결과서 3)

| 꼴 | 결과 | 근거 |
|---|---|---|
| 직접 `CALL` | **1곳** — `LdrpInitializeNtdllDataTableEntry +0x23e` (ret `+0x243`) | `objdump` 전수, `_calls.tsv` 한 줄, 지도 `_map/LdrInitializeThunk_edges.tsv` 한 줄 (결과서) |
| 꼬리 `JMP` · 조건 점프 | 0 | 같은 검색 |
| 코드가 주소를 꺼냄 (`LEA` / `MOV`) | 0 | |
| 데이터가 주소를 들고 있음 | 0 (`.pdata` `RUNTIME_FUNCTION` 시작 한 줄뿐) | 전 구역 8바이트 VA · 4바이트 RVA, CFG 함수 표 포함 |

- 그 호출은 **둘째 `LdrpInitializeNtdllDataTableEntry` (핫패치 ntdll 둘째 항목) 에서만** 간다 (위 표). test.exe: 둘째 호출 조건 칸 (핫패치 문맥 첫 8바이트) 은 핫패치가 꺼져 0 그대로라고 run105 가 정리했다 (`LdrpIsHotPatchingEnabled` 0 `[관찰]` run44). → **test.exe 에선 `LdrpRecordModuleDependency` 가 안 불릴 것** `[정적+추측]` (문맥 칸 값 자체는 동적 D3 = Q44 와 같은 값).

### 사진 로그 대조 (run44, 새로 돌린 것 아님 — flow-writer)

| 정적 | 사진 값 | 맞나 |
|---|---|---|
| (a) exe · ntdll 0 (상태 0 때 만듦) | P1a `ProcessStaticImport : 0y0` 둘 (`ms/logs/04_baggage-read_P1a.txt:2696` · `2801`) | **맞음** |
| (a) exe · ntdll · KERNEL32 · KERNELBASE 0, dll1 · dll2 1 | P1b `0y0` · `0y0` · `0y0` · `0y0` · **`0y1`** · **`0y1`** (`_P1b.txt:3394` · `3499` · `3604` · `3709` · `3814` · `3919`, 이름은 같은 로그 `BaseDllName` `:3386` · `3491` · `3596` · `3701` · `3806` · `3911`) | **맞음** (6/6) |
| (a) 같은 값이 초기화 끝까지 | P2 같은 순서 (`_P2.txt:3406` · `3511` · `3616` · `3721` · `3826` · `3931`, 이름 `:3398 ~ 3923`) | **맞음** — P1b → P2 사이 값 안 바뀜 |
| (a) 알림 락 주인 0, 알림 목록 빔 | `ntdll+0x1cc910` (`7ffa`fb82c910`) 40B: `DebugInfo` = `…c8e0` (= `LdrpDllNotificationLock_DEBUG` rva `0x1cc8e0`), `LockCount` `0xffffffff` (−1), `RecursionCount` 0, **`OwningThread` 0**, `LockSemaphore` 0, `SpinCount` `0x4000000`. `ntdll+0x1cc8c0` (`LdrpDllNotificationList`) = 자기 자신 둘 (`_P1a.txt:3590` · `3592` · `3595 ~ 3597`, `_P1b.txt:4708` · `4710` · `4713 ~ 4715`, `_P2.txt:4720` · `4722` · `4725 ~ 4727`) | **맞음** — 세 장 같음. 파일 값과도 같다 (결과서, `DebugInfo` 는 같은 rva) |
| (b) 정적 import 간선 비트 0 = 0 | 간선 상자 `+0x18` 은 사진 로그에 칸이 없다 (노드 `Dependencies` 꼬리 포인터만 — 짐 지도 3.6) | **못 봄** — 동적 D1 (이미 있는 덤프로 가능) |

### 호출 관계

```
(ProcessStaticImport — 상태와 시간 순서)
LdrpInitializeProcess
 ├─ +0x11a2  LdrpInitializeNtdllDataTableEntry(R8 = 0)
 │            └─ +0x6c   LdrpAllocateModuleEntry          ntdll 항목 — 상태 0 → 0x20 안 켬
 ├─ +0x126f  LdrpInitializeNtdllDataTableEntry(R8 = 원래 ntdll)   +0x1256 핫패치 문맥 0 이면 안 탐 (test.exe 안 탐)
 │            │  +0x143  RBP ≠ 0 → +0x22c
 │            └─ +0x243  LdrpRecordModuleDependency        ← 유일한 caller. +0x11a 간선 비트 0 켬
 ├─ +0x135e  LdrpAllocateModuleEntry                       exe 항목 — 상태 0
 │  (+0x157c LdrpAllocateModuleEntry                      AppVerifier 복제 — AvrfAppVerifierMode ≠ 0 일 때만)
 ├─ +0x19a2  LdrpInitializeKernel32Functions               ← 상태 0 인 채로
 │            ├─ +0xc0   LdrLoadDll (kernel32)   → … → LdrpAllocateModuleEntry   0x20 안 켬
 │            └─ +0x156  LdrLoadDll (kernelbase) → …                             0x20 안 켬
 │  +0x1a4c  LdrInitState ← 1
 ├─ +0x1aa9  LdrpMapAndSnapDependency → LdrpLoadDependentModuleInternal (dll1 · dll2)
 │            └─ +0x115c LdrpAllocateModuleEntry
 │                        +0xc2 상태 == 1 → +0x17a TEB ClientId.UniqueThread
 │                        +0x187 == LdrpDllNotificationLock.OwningThread ? → 같으면 건너뜀
 │                        +0x194 Flags |= 0x20                                  ← dll1 · dll2
 │  +0x1b1e  LdrInitState ← 2
 └─ +0x1bbb  LdrpInitializeGraphRecurse                   +0x3a 간선 비트 0 켜진 칸 건너뜀

(간선 +0x18 비트 0 — test.exe 정적 import 간선)
LdrpLoadDependentModuleInternal
 ├─ +0x92e   RtlAllocateHeap(0x20, 태그 0x280000)          쓰레기 값으로 시작
 │  +0xed5 ~ +0xede  비트 0 지움                            ← 조건 없음
 │  +0xf4d  RCX == 0 → +0x179d   (RCX 는 +0xed8 에서 이미 쓰임 → 안 탐, 정적 추정)
 │  │        └─ +0x17c2  RtlAllocateHeap(0x20, 태그 0x240000) → +0x17d9 비트 0 켬
 │  +0xf8f ~ +0xfa5  +0x18 = 출발 노드 (옛 비트 0 되살림 — 0 이라 그대로)
 └─ (간선을 Dependencies / IncomingDependencies 꼬리에 — +0xf69 / +0xf8b, run23)

(간선 비트 0 을 켜는 다른 둘 — 부모 인자가 NULL 이 아닐 때만)
LdrpLoadDll  +0x71 R12 = 0 → +0xf1 / +0x1f8 / +0x243 [RSP+0x28] (6번째 인자) = 0
 └─ +0x100  LdrpLoadDllInternal(…, 부모 = 0)
             ├─ +0xb8   LdrpFastpthReloadedDll      +0x95 부모 NULL → 끝 (+0x19d 할당 · +0x1a5 켬 안 탐)
             └─ +0x295  LdrpBuildForwarderLink      +0x1a 부모 NULL → 끝 (+0x110 할당 · +0x11c 켬 안 탐)
 (부모가 있는 길: LdrpLoadForwardedDll +0x2e1 · LdrpLoadPatchImage +0xbf → LdrpLoadDllInternal — 결과서)

(간선 비트 0 을 지우는 다른 하나 — 고리 합칠 때만)
LdrpCondenseGraphRecurse
 └─ +0xd8   LdrpMergeNodes      +0xe3 [간선+0x18] = 합친 노드 (하위 비트 0)

(비트 0 을 보고 건너뛰는 둘)
LdrpInitializeGraphRecurse        +0x26 Dependencies → +0x3a TEST [간선+0x18],1 → +0x3e JNZ 건너뜀
LdrpInitializeShimDllDependencies +0xa6 TEST [간선+0x18],1 → +0xaa JNZ 건너뜀
 └─ +0xd2   LdrpInitializeGraphRecurse

(ProcessStaticImport 를 끄는 하나 — 바꿔치기된 항목)
{LdrpLoadDllInternal +0x341 · LdrpLoadDependentModuleInternal +0x1341 · LdrpSnapModule +0x101 · LdrpFreeLoadContext +0xa2}
 └─ LdrpFreeReplacedModule   +0x15 Flags &= ~0x20 → +0x1c ReferenceCount = 1 → +0x2b (꼬리 JMP) LdrpDereferenceModule
```

## 확인된 것

- `[정적]` **`ProcessStaticImport` 를 켜는 줄은 ntdll 전체에서 `LdrpAllocateModuleEntry +0x194` 하나**, 조건 = `LdrInitState == 1` 이고 TEB `ClientId.UniqueThread` ≠ `LdrpDllNotificationLock.OwningThread` (`ntdll+0x1cc920`). 끄는 줄은 `LdrpFreeReplacedModule +0x15` 하나 (바꿔치기된 항목).
- `[정적+관찰]` **kernel32 · kernelbase 가 0 인 이유 = 상태 1 이 되기 전에 올라옴** (`LdrpInitializeProcess +0x199d` → `LdrpInitializeKernel32Functions +0xbb` / `+0x151` `LdrLoadDll`, 상태 ← 1 은 `+0x1a4c`). 사진 6 모듈 (exe · ntdll · k32 · kb 0, dll1 · dll2 1) 이 P1b · P2 에서 정적 추론과 전부 같다.
- `[정적+관찰]` `LdrpDllNotificationLock` 은 test.exe 초기화 동안 주인 0, 알림 목록 빔 (사진 세 장) → 건너뛰는 갈래는 설 자리가 없다.
- `[정적]` **간선 `+0x18` 비트 0 을 켜는 곳 4** (`LdrpRecordModuleDependency +0x11a` · `LdrpFastpthReloadedDll +0x1a5` · `LdrpBuildForwarderLink +0x11c` · `LdrpLoadDependentModuleInternal +0x17d9`), **지우는 곳 2** (`LdrpLoadDependentModuleInternal +0xede` · `LdrpMergeNodes +0xe3`), 비트 0 을 보고 건너뛰는 곳 2 (`LdrpInitializeGraphRecurse +0x3a` · `LdrpInitializeShimDllDependencies +0xa6`), 포인터로 읽는 곳 3 함수.
- `[정적]` **test.exe 정적 import 간선은 비트 0 = 0** (`+0x929` 상자 → `+0xede` 지움). `LdrLoadDll` 길 (`LdrpLoadDll` 이 부모 0) 에선 `LdrpFastpthReloadedDll` · `LdrpBuildForwarderLink` 가 간선을 안 만든다.
- `[정적]` **`LdrpRecordModuleDependency` caller 는 `LdrpInitializeNtdllDataTableEntry +0x23e` 하나** (직접 1 · 꼬리 0 · 주소 상수 0 · 데이터 0), 그 호출은 셋째 인자 ≠ 0 (핫패치 둘째 항목) 일 때만.

## 결과서와 다르게 적은 곳 · 보탠 것

- **문서 줄 번호**: 결과서 "다음 요청 후보 N1 (c)" 의 `baggage.md 565 줄` 은 지금 **586 줄** (3.5 `ProcessStaticImport` 줄) 이다 (뒤 바퀴들이 줄을 더해서). 내용 기준으로 고쳤다.
- **사진 줄 보탬**: P2 의 6 모듈 줄 (`_P2.txt:3406 ~ 3931`) 과 알림 락 · 목록의 세 장 줄 번호를 flow-writer 가 다시 읽어 붙였다 (결과서는 P1b 줄 번호만).
- **"run41 (간선 비트)"**: 오케스트레이터가 앞 바퀴 짝으로 준 run41 문서에는 이번 줄들 (`+0x17d9` · `+0xede`) 이 없다 (`grep`). 옛 목록이 적힌 곳은 run23 · run27 · run40 문서와 flow.md 2.6 · 2.7 · 4장 2.7 줄이다 → 그 셋에 정정 줄을 넣었다.
- 결과서의 나머지 오프셋은 위 "방법" 의 `.asm` 줄로 다시 맞췄고 한 줄도 안 어긋났다.

## 정정

- **간선 비트 0 의 옛 설명** (run23 `runs/02_sprint/run23-load-dependent.md` "간선 한 칸" 줄 · run27 `runs/02_sprint/run27-init-graph.md` "이 비트를 켜는 곳" 줄 · run40 `runs/02_sprint/run40-dll-crt-entry.md` Q54 줄 · flow.md 2.6 간선 절 · 4장 2.7 줄): "켜는 곳 = `LdrpRecordModuleDependency +0x11a` · `LdrpLoadDependentModuleInternal +0x17d9` (**대비용 칸** `+0x17c2`), 지우는 곳 = `+0xede` (보통 칸)" →
  - 켜는 곳은 **넷** — `LdrpFastpthReloadedDll +0x1a5` · `LdrpBuildForwarderLink +0x11c` 가 빠져 있었다.
  - `+0x17d9` (`+0x17c2` 상자) 는 **탈 수 없는 길로 보인다** `[정적 추정]` (`+0xf4d` 의 RCX 는 `+0xed8` 에서 이미 쓰인 값). 그래서 "비트 0 = 대비용 칸 표시" 라는 뜻 (run23 `[추측]`) 은 근거를 잃었다. 뜻은 `[공백]` 으로 돌린다 — 지금 아는 건 "켜진 간선은 그래프 켜기가 따라가지 않는다" 뿐.
  - `+0xede` 의 함수 이름 = **`LdrpLoadDependentModuleInternal`**. 지우는 곳이 하나 더 있다 — **`LdrpMergeNodes +0xe3`** (덮어쓰기).
  - test.exe 실행 흐름 · 지도 결과는 안 바뀐다 (정적 import 간선 비트 0 = 0, "GR 이 건너뛸 간선 없음" 은 그대로 — 근거가 `[추측]` 에서 `[정적]` 으로).
  - run23 · run27 · run40 문서 맨 위에 정정 한 줄씩 넣었다.
- **Q123 — run57 호출 관계 트리** (`runs/03_sprint/run57-nls-apiset.md`): `└─ +0x27  ExInitializeNls` → **`+0x2c`** (트리는 ret 자리 — `Phase1InitializationIoReady.asm` `+0x27 CALL … ; CALL ret=+0x2c -> ExInitializeNls`). 그 줄만 고쳤다. 같은 문서 "정적 — 호출 지점" 표의 그 줄 (call `+0x27`, ret `—`) 은 그대로 두었다 (ret 칸이 비어 있을 뿐 틀린 값은 아님).

## 이전 run 에서 닫은 것

- **Q72** (Q54 를 문서로 닫으며 남긴 전수 확인) — (a) · (b) · (c) 모두 답. 남은 것: `+0x17d9` 죽은 길 · 포워더 길 간선 (동적 D2 · 정적 N2).
- flow.md 2.8 "dll1 · dll2 항목이 그 조건에서 만들어졌는지는 실행 값 (Q27)" — 사진 P1b · P2 `ProcessStaticImport` 1 `[관찰]` 로 닫힘 (진입점 넷째 인자 실제 값 자체는 Q27 그대로).
- **Q123** (run57 트리 오프셋) — 고침.

## 아직 확인 필요

### 동적 (결과서 "동적으로 확인할 것" — 사진으로 닫힌 것은 뺌. 대기열 번호는 오케스트레이터가 붙임)

1. **D1 (간선 비트, 사진으로 가능)**: 노드 `Dependencies` (`+0x28`) / `IncomingDependencies` (`+0x30`) 꼬리 포인터는 사진 P1b · P2 에 이미 있다 (짐 지도 3.6). 고리를 따라 간선 상자마다 `dq <상자> L4` 로 `+0x18` 하위 비트를 본다. 기대 `[정적]`: 모두 0. **이미 있는 덤프 (`ms/dump/03_baggage-snap/` P1b · P2) 를 오프라인으로 읽으면 새 실행 없이 닫힌다.**
2. **D2 (죽은 길 · 안 타는 길)**: 한 실행에 멈춤점 넷 (모두 test.exe 조건) — `ntdll!LdrpLoadDependentModuleInternal+0x179d`, `ntdll!LdrpFastpthReloadedDll+0x1a5`, `ntdll!LdrpBuildForwarderLink+0x11c`, `ntdll!LdrpMergeNodes+0xe3`. 기대: 하나도 안 선다. (`LoadLibraryExW` ×3 구간까지 포함하면 진입점 뒤도 같이 본다.)
3. **D3 (= Q44 와 같은 값)**: `ntdll!LdrpInitializeProcess+0x126a` 가 안 서면 `LdrpRecordModuleDependency` 는 안 불림. 새 항목 필요 없음.
4. (선택) **D4**: `ntdll!LdrpAllocateModuleEntry+0x194` 가 정확히 2번 (dll1 · dll2) — 사진이 이미 같은 결과라 급하지 않음.

### 정적 (결과서 "다음 요청 후보")

1. **N2 (작음)**: `LdrpLoadForwardedDll` (`+0x2e1`) 이 `LdrpLoadDllInternal` 을 부를 때 부모 인자가 무엇인가, test.exe 의 포워더 (kernel32 → ntdll 등) 가 `LdrpSnapModule` 쪽에서 **다른 DLL 을 새로 올리는 길** (= `LdrpBuildForwarderLink` 간선이 생기는 길) 을 타는가. 지금 "test.exe 비트 0 간선 0개" 는 이 길 하나가 `[정적 추정]`.
2. **N3 (중요도 낮음)**: 알림 락 주인일 때 건너뛰는 갈래의 뜻 (`LdrpPrepareModuleForExecution +0x2b → +0xfe`, `LdrpMapDllWithSectionHandle +0x239 → +0x357`, `LdrGetProcedureAddressForCaller +0x195` / `+0x92c`). test.exe 는 알림 등록이 없어 안 탐.
3. (보탬) 비트 0 의 **뜻** `[공백]` — 켜는 셋 (`LdrpRecordModuleDependency` · `LdrpFastpthReloadedDll` · `LdrpBuildForwarderLink`) 이 공통으로 "부모 쪽이 따로 있는 간선" 이라는 짐작 `[추측]` 을 굳힐 근거 (예: 켜진 간선을 지울 때 누가 `LoadCount` 를 되돌리나).
4. (보탬, 짐 지도) `LdrpDllNotificationLock` (7.3 `≤P1a` 모름) — 칸 주소를 직접 쓰는 줄이 없고 (`_refs`), 파일 값 = 사진 값 세 장. 3.8 로 옮길지 (2장 숫자가 바뀜) 는 오케스트레이터 결정.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

- (a) **맞음** — `Src/ntdll/LdrInit.cpp` 가 진짜 `LdrpAllocateModuleEntry` 를 부르고 (3961 ~ 3972 줄), `LdrInitState ← 1` 을 진짜 `+0x1a4c` 자리에서 쓰고 (5653 줄), DLL 은 진짜 `LdrpMapAndSnapDependency` (5304 줄~). `Src` 에 `0x20` 을 직접 쓰는 줄 없음. TEB `+0x48` 은 `Src/nt/Process.cpp:1410` 이 진짜 tid. **확인 필요 하나**: kernel32 · kernelbase 올리기가 `LdrInitState ← 1` 보다 앞인가 (헤더 주석상 같은 순서, 줄 대조는 안 함).
- (b) **맞음** — 간선을 만드는 쪽 · 읽는 쪽 모두 진짜 코드 (`LdrInit.cpp` 5304 줄~ · 6429 줄~), `Src` 에 간선 `+0x18` 쓰기 없음. **확인 필요**: `LoadLibraryExW` ×3 (상태 3) 길도 진짜 로더로 돌리는가.
- (c) **맞음** — 첫 `LdrpInitializeNtdllDataTableEntry` (`R8 = 0`) 만 부르고 (`LdrInit.cpp:3635`~), 둘째 갈래는 "문맥 0 이라 안 탄다" 로 줄임 (7087 ~ 7091 줄).

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntdll pdb 줄 · 근거 로그 (run108)), 2.4 (절 머리 · `LdrpInitializeNtdllDataTableEntry` 트리 `+0x243` 줄), 2.5 (절 머리 · `LdrpLoadDllInternal` `[a]` · `[g]` 줄 — 부모 인자 0 이면 간선 안 만듦), 2.6 (절 머리 · 간선 절 **정정** · `LdrpAllocateModuleEntry` `0x20` 줄 — 전수 · 조건 칸 · kernel32 순서), 2.7 (절 머리 · GR 절 비트 0 줄), 2.8 (넷째 인자 줄 — `[추측]` → `[정적+관찰]`), 4장 (2.7 run27 줄 해결됨 + 정정 · run108 새 줄 셋 — 동적 · 정적 · PE_emulator), 5장 (run108 · sast/103).
- baggage.md: 기준 (run108 줄 · 쉰여덟째판), 3.5 `ProcessStaticImport` 줄 (켜는 줄 전수 · 끄는 줄 · k32 · kb 0 의 이유 · 사진 줄), 3.6 머리 메모 1 (간선 상자 · 비트 0 전수) + `Dependencies` · `IncomingDependencies` 줄 근거 한 마디, 새 7.39. **칸 수 · 줄 수 · 2장 숫자 그대로** (고친 줄 · 메모만).
- 이전 run 문서: run23 · run27 · run40 맨 위 정정 한 줄, run57 트리 한 줄 (Q123).
- 그림: 바뀐 호출 관계 없음 (새 함수들은 대표 지점 밖) → 노드 · 화살표 그대로.

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 중 디버거 칸을 읽는 것은 없다 `[정적: 읽은 줄 안에서]`. 알림 락 주인 비교는 TEB 스레드 ID 만 본다.
- pitfalls 제안 없음. 도구는 run106 · run107 과 같은 `objdump` 풀이 재사용.

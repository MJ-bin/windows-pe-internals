# 2026-10-23 run25 — KERNELBASE·KERNEL32 시작 함수 = PE 진입점 = PDB `KernelBaseDllInitialize`(`0x5060`)·`BaseDllInitialize`(`0x2c5d0`), 정확히 같음. kernel32 는 `KERNELBASE.dll` 을 이름 그대로 import, ntdll 이 찾는 세 export 는 forwarder 아님 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: 두 모듈 모두 `ms/bin/.../System32/` 의 26100.9457.
  - kernel32 (`kernel32.dll`, 이번에 처음): pdb GUID+Age `AE9E9D8F7AC1467A645320FDC23235D01` (바이너리 CodeView 로 받은 것). analyze 29초, 함수 2633개, `SetPdb:` 있음. 별칭 표 `ms/sym/kernel32.dll.aliases.tsv` (31곳).
  - KernelBase (`KernelBase.dll`): run14 와 같은 analyze 를 다시 썼다. pdb `72CCE506944DE0267C172AD64287F9BD1` (flow.md 기준과 같음). 별칭 표 `ms/sym/KernelBase.dll.aliases.tsv`.
  - 버전 대조 (결과서): 이 두 모듈은 스킬 표에 "알려진 사실" 줄이 없다. 대신 (1) PDB 를 바이너리 CodeView 로 받았고 (2) PE 진입점 rva 에 PDB 가 정확히 `BaseDllInitialize`/`KernelBaseDllInitialize` 를 두었고 (3) kernel32 `entry.asm` 의 `CALL +0x1d` → ret `+0x22` 를 원본 바이트로 맞춰 봤다. 셋 다 맞음.
- **Ghidra 는 두 진입점 함수를 PDB 이름 대신 `entry` 로 잡는다.** 그래서 파일은 `ms/disassemble/kernel32/entry.asm`, `ms/disassemble/kernelbase/entry.asm` 이다. 이 문서에서는 PDB 이름을 쓴다 (`pdb_aliases.py --where` 가 `_index.tsv 이름: entry` 라고 알려 줌).
- 근거로 읽은 것
  - kernel32: `BaseDllInitialize`(=`entry`, rva `0x2c5d0`, 55바이트), `_BaseDllInitialize` (`0x2c610`, 954바이트), `_DllMainCRTStartupForGS2` (`0x56898`, 24바이트), `_Kernel32LegacyLib_BaseDllInitialize` (`0x2ca24`, depth 0 따로), `BaseThreadInitThunk` (`0x2cd70`, 76바이트), `BasepInitializeTermsrvFpns` (`0x2cdc4`, 이름만).
  - KernelBase: `KernelBaseDllInitialize`(=`entry`, `0x5060`, 358바이트), `KernelBaseBaseDllInitialize` (`0x51d0`, 125바이트), `_KernelBaseBaseDllInitialize` (`0x4e6c0`, depth 0 따로), `KernelbasePostInit` (`0xf4780`, 93바이트).
  - PE 표 (`pefile`, 결과서): 두 DLL 의 `AddressOfEntryPoint`, import·delay import, export (forwarder 여부).

## 목적

- run8 `[확인 필요]` 4·5번 줄: `LdrpCallInitRoutine` 의 `rcx` 가 `KERNELBASE!KernelBaseDllInitialize`·`KERNEL32!BaseDllInitialize` "근처 (정확히 일치 아님)" 였다. 두 DLL 의 **진짜 진입점이 무슨 이름인지** 코드·PE 헤더로 본다.
- 두 시작 함수가 이유 1(프로세스 시작)일 때 하는 일 (대표만), KERNEL32 시작 함수가 KERNELBASE 를 부르나.
- kernel32 의 import 이름 (api-set 인가, `KERNELBASE.dll` 직접인가) → flow.md 2.5 `[추측]` "kernel32 를 들이면 kernelbase 가 따라 들어온다" 의 재료.
- ntdll 이 이름으로 찾는 세 함수(run20)가 export 에 있나, forwarder 인가.

## 방법

- 정적: `_workspace/sast/20_dll-entry.md` — kernel32 `^(entry|BaseThreadInitThunk)$` depth 1 (6개) + `^_Kernel32LegacyLib_BaseDllInitialize$` depth 0, KernelBase `^(entry|KernelbasePostInit)$` depth 1 (14개) + `^_KernelBaseBaseDllInitialize$` depth 0. 지도(`map`)는 안 만듦.
- 결과서: `_workspace/sast/20_dll-entry_result.md` (**분석성공**)
- 범위 넓힘 (결과서): "이유 1 갈래 대표 호출" 과 "KERNEL32 → KERNELBASE 호출" 은 진입점 한 단계 아래 함수에서 일어나서, `_Kernel32LegacyLib_BaseDllInitialize` 와 `_KernelBaseBaseDllInitialize` 를 depth 0 으로 하나씩 더 뽑았다.

## 결과

### 정적 — 진입점 rva 와 PDB 이름

| 모듈 | PE `AddressOfEntryPoint` | 그 주소의 PDB 이름 | 같은 주소 다른 이름 | run8 이 적은 이름의 rva | 차이 |
|---|---|---|---|---|---|
| kernel32 | `0x2c5d0` | `BaseDllInitialize` | 없음 | `BaseDllInitialize` = `0x2c5d0` | **0** |
| KernelBase | `0x5060` | `KernelBaseDllInitialize` | 없음 | `KernelBaseDllInitialize` = `0x5060` | **0** |

- imagebase 둘 다 `0x180000000`.
- 근거: PE 헤더, `pdb_aliases.py --where kernel32.dll BaseDllInitialize` → `rva=0x2c5d0 [function]`, `--where KernelBase.dll KernelBaseDllInitialize` → `rva=0x5060 [function]` (둘 다 `_index.tsv 이름: entry`). `--lookup KernelBase.dll 0x5060` → "별칭 무리 아님".
- 이름이 비슷한 **다른** 함수 (헷갈리기 쉬움):
  - kernel32: 진입점 `BaseDllInitialize`(`0x2c5d0`) 바로 뒤에 밑줄 붙은 `_BaseDllInitialize`(`0x2c610`, 진입점 `+0x40`)가 있다. 실제 일은 이쪽이 한다.
  - KernelBase: 진입점 `KernelBaseDllInitialize`(`0x5060`) 와 이름이 거의 같은 `KernelBaseBaseDllInitialize`(`0x51d0`, 진입점 `+0x170`), `_KernelBaseBaseDllInitialize`(`0x4e6c0`) 는 진입점이 부르는 아래 함수다.

### 정적 — 호출 지점 (`ms/disassemble/kernel32/_calls.tsv`, `ms/disassemble/kernelbase/_calls.tsv`)

진입점과 그 바로 아래 (이유 1 갈래 대표만. 전부는 결과서).

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `KERNEL32!BaseDllInitialize` (`entry`) | `+0x31` | `+0x36` | `KERNEL32!_DllMainCRTStartupForGS2` (이유 == 1 일 때만, `+0x16 JZ`) | [정적] |
| `KERNEL32!BaseDllInitialize` (`entry`) | `+0x1d` | `+0x22` | `KERNEL32!_BaseDllInitialize` (모든 이유, 꼬리호출 아님) | [정적] |
| `KERNEL32!_DllMainCRTStartupForGS2` | `+0x9` | `+0xe` | `__security_init_cookie` (이유 == 1 일 때만) | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x6c` | `+0x73` | ntdll `RtlImageNtHeader` | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x92` | `+0x99` | ntdll `RtlSetThreadPoolStartFunc` | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0xb3` | `+0xba` | ntdll `LdrSetDllManifestProber` | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0xc2` | `+0xc7` | 메모리 관리 준비 함수 (rva `0x2c9d8`, 같은 주소 이름 2개, 후보: `BaseDllInitializeMemoryManager2`, `BaseDllInitializeMemoryManager`) | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0xe9` | `+0xf0` | ntdll `RtlSetProtectedPolicy` | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x1f9` | `+0x200` | ntdll `RtlGetSuiteMask` | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x210` | `+0x217` | ntdll `LdrDisableThreadCalloutsForDll` | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x21f` | `+0x224` | `KERNEL32!_Kernel32LegacyLib_BaseDllInitialize` | [정적] |
| `KERNEL32!_BaseDllInitialize` | `+0x380` | `+0x387` | **`KERNELBASE!GetRegistryExtensionFlags`** (import, `RtlGetSuiteMask & 0x10` 이고 `+0x364 InitializeRegTermsrvFpns` 가 통과할 때만) | [정적] |
| `KERNEL32!_Kernel32LegacyLib_BaseDllInitialize` | `+0x19` | `+0x20` | **`KERNELBASE!KernelBaseGetGlobalData`** (import, 이유 1 갈래) | [정적] |
| `KERNELBASE!KernelBaseDllInitialize` (`entry`) | `+0x8` | `+0xd` | `KERNELBASE!KernelBaseBaseDllInitialize` (맨 먼저, 모든 이유) | [정적] |
| `KERNELBASE!KernelBaseDllInitialize` (`entry`) | `+0xba` | `+0xbf` | `KERNELBASE!WilInitialize` | [정적] |
| `KERNELBASE!KernelBaseDllInitialize` (`entry`) | `+0xc4` | `+0xc9` | `KERNELBASE!TipInitialize` | [정적] |
| `KERNELBASE!KernelBaseDllInitialize` (`entry`) | `+0xce` | `+0xd3` | `KERNELBASE!FeatureTuningInitialize` | [정적] |
| `KERNELBASE!KernelBaseDllInitialize` (`entry`) | `+0xd3` | `+0xd8` | 음수를 돌려주면 실패로 끝내는 함수 (rva `0x5ff0`, 같은 주소 이름 2개, 후보: `Initialize`, `?Initialize@Globals@ARI@@SAJXZ`) | [정적] |
| `KERNELBASE!KernelBaseDllInitialize` (`entry`) | `+0xe5` | `+0xea` | `KERNELBASE!AppContainerRegisterInit` | [정적] |
| `KERNELBASE!KernelBaseBaseDllInitialize` | `+0x28` | `+0x2d` | `KERNELBASE!_DllMainCRTStartupForGS2` (`0x1290e8`, 모든 이유) | [정적] |
| `KERNELBASE!KernelBaseBaseDllInitialize` | `+0x44` | (꼬리 `JMP`) | `KERNELBASE!_KernelBaseBaseDllInitialize` | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x1ad` | `+0x1b2` | `KERNELBASE!KernelBaseDllInitializeMemoryManager` | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x1cf` | `+0x1d6` | ntdll `RtlCreateTagHeap` | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x2d2` | `+0x2d9` | ntdll `CsrClientConnectToServer` | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x4e1` | `+0x4e6` | `KERNELBASE!BaseNlsDllInitialize` | [정적] |
| `KERNELBASE!_KernelBaseBaseDllInitialize` | `+0x521` | `+0x526` | `KERNELBASE!ConsoleInitialize` | [정적] |
| `KERNEL32!BaseThreadInitThunk` | `+0x12` | `+0x17` | 간접 호출 (CFG 스텁 `0x180087010` 거침, 대상 = `RDX`, 인자 `RCX = R8`). 첫 인자 `ECX == 0` 일 때 | [정적] |
| `KERNEL32!BaseThreadInitThunk` | `+0x19` | `+0x20` | ntdll `RtlExitUserThread` (같은 갈래) | [정적] |
| `KERNEL32!BaseThreadInitThunk` | `+0x26` | `+0x2d` | ntdll `RtlGetSuiteMask` (`ECX ≠ 0` 일 때, `+0xa JNZ`) | [정적] |
| `KERNEL32!BaseThreadInitThunk` | `+0x36` | `+0x3b` | `KERNEL32!BasepInitializeTermsrvFpns` (`AL & 0x10` 일 때) | [정적] |

- 이유(두 번째 인자)는 둘 다 `EDX` 에서 읽는다: kernel32 `entry +0xa MOV EDI,EDX`, KernelBase `entry +0x6 MOV EBX,EDX`.
- 이유 1 갈림: kernel32 `entry +0x14 CMP` / `+0x16 JZ`, `_BaseDllInitialize +0x5f CMP EAX,1`. KernelBase `entry +0x4f CMP EBX,1` → `+0xa3 CMP AL,1` 뒤 `+0xba` 부터, `_KernelBaseBaseDllInitialize +0x60 JZ` → `+0x195`.
- KernelBase 의 이유 1 갈래 맨 끝: `_KernelBaseBaseDllInitialize +0x16a` 전역 `BasepInitializationStage` 에 값을 쓴다. `entry` 는 `+0xd3` 함수가 음수면 `+0x160` 에서 `AL = 0` (실패) 로 끝난다.
- 전역 값만 쓰는 곳 (kernel32 이유 1): `+0x189` `BaseStaticServerData`, `+0x194` `BaseCSDVersion`, `+0x1ab` `BaseWindowsSys32x86Directory`, `+0x1e4` `BasepAppCertDllsList`.

### 정적 — kernel32 import 표 (PE import directory 그대로)

| 묶음 | 개수 | 예 |
|---|---|---|
| `api-ms-win-…` 가상 이름 | 102개 (함수 815개) | `api-ms-win-core-processthreads-l1-1-0.dll`(37), `api-ms-win-core-file-l1-1-0.dll`(73), `api-ms-win-core-libraryloader-l1-2-0.dll`(24) |
| 진짜 이름 | 2개 | **`ntdll.dll`** (366개), **`KERNELBASE.dll`** (93개) |
| delay import (처음 쓸 때 올림) | 3개 | `ext-ms-win-oobe-query-l1-1-0.dll`, `ext-ms-win-packagevirtualizationcontext-l1-1-0.dll`, `RPCRT4.dll` |

- 일반 import 는 모두 104개. `ext-ms-…` 는 일반 import 에 없다. 바운드 import 없음.
- 참고 (KernelBase): 일반 import 는 `ntdll.dll`(743) + `api-ms-win-eventing-provider-l1-1-0.dll`(5) + `api-ms-win-core-apiquery-l1-1-0`/`l1-1-2`(1, 1) 뿐. 나머지는 delay import.

### 정적 — ntdll 이 이름으로 찾는 세 함수 (export 표)

| 이름 | 모듈 | ordinal | rva | forwarder? | PDB 이름 |
|---|---|---|---|---|---|
| `BaseThreadInitThunk` | kernel32 | 75 | `0x2cd70` | **아니오** | `BaseThreadInitThunk` (76바이트) |
| `TermsrvGetWindowsDirectoryW` | kernel32 | 1511 | `0x35f10` | **아니오** | `TermsrvGetWindowsDirectoryW` (18바이트) |
| `KernelbasePostInit` | KernelBase | 1062 | `0xf4780` | **아니오** | `KernelbasePostInit` (93바이트) |

- 셋 다 export 디렉터리 범위 밖의 코드 주소를 가리킨다 (kernel32 export dir `0xa65f0` 크기 `0xed6c`, KernelBase `0x391ab0` 크기 `0x1006c`). 같은 주소에 다른 PDB 이름 없음.
- 참고: kernel32 export 1697개 중 211개, KernelBase 2037개 중 104개는 forwarder 다. 이 셋은 아니다.
- `TermsrvGetWindowsDirectoryW` 몸통 (18바이트, 원본 바이트): 전역 칸 하나에서 함수 포인터를 읽어 0 이 아니면 CFG 스텁을 거쳐 그리로 점프, 0 이면 그냥 `ret`.
- `KernelbasePostInit` 이 부르는 것 (이름만): `+0x18` `BasepGetLongPathAwareRegistryState`, `+0x53` `BasepIsProcessLongPathAwareByManifest`.

### 호출 관계

```
LdrpCallInitRoutine (ntdll, run8 관찰 자리. 들어가는 건 아직 ?)
 ├─ ? → KERNELBASE!KernelBaseDllInitialize   (rva 0x5060 = PE 진입점, Ghidra 이름 entry)
 │       ├─ (1) +0xd   KernelBaseBaseDllInitialize          모든 이유
 │       │      ├─ (1) +0x2d  _DllMainCRTStartupForGS2       (이유 1 이면 __security_init_cookie)
 │       │      └─ (2) +0x44  꼬리 JMP → _KernelBaseBaseDllInitialize
 │       │                    이유 1: +0x1b2 KernelBaseDllInitializeMemoryManager → +0x1d6 RtlCreateTagHeap
 │       │                            → +0x2d9 CsrClientConnectToServer → … +0x4e6 BaseNlsDllInitialize
 │       │                            → +0x526 ConsoleInitialize …
 │       └─ (2) 이유 1:  +0xbf WilInitialize → +0xc9 TipInitialize → +0xd3 FeatureTuningInitialize
 │                        → +0xd8 (0x5ff0, 이름 2개) → +0xea AppContainerRegisterInit …
 └─ ? → KERNEL32!BaseDllInitialize            (rva 0x2c5d0 = PE 진입점, Ghidra 이름 entry)
         ├─ (1) +0x36  _DllMainCRTStartupForGS2             이유 1 일 때만
         └─ (2) +0x22  _BaseDllInitialize                    모든 이유
                 이유 1: +0x73 RtlImageNtHeader → +0x99 RtlSetThreadPoolStartFunc → +0xba LdrSetDllManifestProber
                         → +0xc7 (0x2c9d8, 이름 2개) → +0xf0 RtlSetProtectedPolicy → +0x200 RtlGetSuiteMask
                         → +0x217 LdrDisableThreadCalloutsForDll
                         → +0x224 _Kernel32LegacyLib_BaseDllInitialize
                                   └─ +0x20 KERNELBASE!KernelBaseGetGlobalData   (import)
                         (조건부) +0x387 KERNELBASE!GetRegistryExtensionFlags     (import, 터미널 서비스 갈래)

KERNEL32!BaseThreadInitThunk (rva 0x2cd70, export, forwarder 아님)
 ├─ ECX == 0:  +0x17 간접 호출 RDX(R8) (CFG 스텁) → +0x20 RtlExitUserThread
 └─ ECX != 0:  +0x2d RtlGetSuiteMask → (AL & 0x10) +0x3b BasepInitializeTermsrvFpns
```

## 확인된 것

- `[정적]` **두 DLL 의 PE 진입점 = PDB 이름 `KernelBaseDllInitialize`(`0x5060`)·`BaseDllInitialize`(`0x2c5d0`). rva 가 정확히 같고, 같은 주소에 다른 이름은 없다.** run8 이 `ln` 으로 본 두 이름이 바로 진입점의 이름이다.
- `[정적]` 두 진입점은 CRT 식 `_DllMainCRTStartup` → `DllMain` 모양이 **아니다**. CRT 조각 `_DllMainCRTStartupForGS2`(이유 1 일 때 보안 쿠키만 준비, 24바이트)를 부르고 바로 전용 초기화 함수로 간다. dll1·dll2(run8: `_DllMainCRTStartup` → `DllMain`)와 다르다.
  - kernel32: `BaseDllInitialize` → (이유 1) `_DllMainCRTStartupForGS2` → `_BaseDllInitialize`.
  - KernelBase: `KernelBaseDllInitialize` → `KernelBaseBaseDllInitialize` → `_DllMainCRTStartupForGS2` → 꼬리 `JMP` `_KernelBaseBaseDllInitialize`.
- `[정적]` **KERNEL32 시작 함수 아래에 KERNELBASE 함수를 부르는 코드가 있다**: `_Kernel32LegacyLib_BaseDllInitialize +0x19` `KernelBaseGetGlobalData` (이유 1 갈래), `_BaseDllInitialize +0x380` `GetRegistryExtensionFlags` (조건부). 둘 다 kernel32 import 표의 `KERNELBASE.dll` 항목이다. 진입점 자신은 KERNELBASE 를 직접 부르지 않는다.
- `[정적]` KERNELBASE 시작 함수(이유 1)는 아래에서 **csrss 연결 `CsrClientConnectToServer`** 를 부른다 (`_KernelBaseBaseDllInitialize +0x2d2`). 순서·조건은 점프를 읽은 것이고 실행 때 그대로인지는 동적.
- `[정적]` **kernel32 는 `KERNELBASE.dll` 을 이름 그대로 import 한다** (93개 함수). api-set 이름 102개와는 따로다. 그러니 "kernel32 를 들이면 kernelbase 가 따라 들어온다" 는 ApiSet 번역 없이도 import 표만으로 성립한다. api-set 102개가 실행 때 무엇으로 바뀌는지는 ApiSet 맵을 봐야 안다.
- `[정적]` **ntdll 이 이름으로 찾는 세 함수는 모두 export 에 있고 forwarder 가 아니다**: kernel32 `BaseThreadInitThunk` `0x2cd70`, `TermsrvGetWindowsDirectoryW` `0x35f10`, KernelBase `KernelbasePostInit` `0xf4780`. run20 의 `[추측]` "forwarder 면 다른 곳" 은 이 빌드에선 해당 없음.
- `[정적]` `BaseThreadInitThunk` ret `+0x20` 은 `+0x19 RtlExitUserThread` 호출의 돌아올 자리다. 01_sprint 가 Worker 스레드 종료 콜스택에서 본 `BaseThreadInitThunk+0x20` `[관찰]` 과 오프셋이 맞는다 (첫 인자 0 갈래: 시작 함수를 부르고 돌아온 뒤 스레드 끝).
- `[정적]` `BaseThreadInitThunk` 의 첫 인자가 0 이 아니면 스레드 시작 함수를 부르지 않고 터미널 서비스 쪽 준비(`BasepInitializeTermsrvFpns`, `AL & 0x10` 일 때)만 한다. `LdrpInitializeProcess +0x1b5e` 의 `(1,0,0)` 호출(run19)이 이 갈래에 해당할 것이다 `[추측: 칸 값이 BaseThreadInitThunk 라면. 값은 Q48]`.

## 정적과 동적이 다른 곳

- run8 `[관찰]`: `ln @rcx` 가 두 이름 "**근처** (정확히 일치 아님)".
- run25 `[정적]`: 진입점 rva 가 두 이름과 **정확히 같다**.
- 정적으로는 "진입점이 다른 이름의 작은 함수다 / 이름 없는 thunk 다" 같은 설명이 **안 맞는다**. 그 밖의 이유는 정적으로 못 정한다. run8 의 원본 로그(`run8-dllcheck.log`)는 레포에 없어서 `ln` 출력 그대로를 다시 볼 수 없다.
  - `[추측]` `ln` 은 앞뒤 가까운 심볼을 같이 보여 준다. kernel32 쪽이면 `BaseDllInitialize` 다음 줄에 `_BaseDllInitialize`(`+0x40`)가 같이 떴을 것이다. 이 모양을 "근처" 로 적었을 수 있다.
  - `[추측]` 또는 그때 `rcx` 가 진입점이 아닌 다른 값이었을 수 있다.
- run8 문서는 관찰 기록이라 그대로 둔다. 어느 쪽인지는 Q21 에서 `? @rcx - kernelbase` / `- kernel32` 로 다시 본다 (예측 `0x5060` / `0x2c5d0`).

## 아직 확인 필요

- 실제로 진입점 주소로 들어가나, `ln` 이 정확히 보이나 → 동적 **Q21**: `ntdll!LdrpCallInitRoutine` 입구 (조건 `r8 == 1`, test.exe) 에서 `ln @rcx`, `? @rcx - kernelbase`, `? @rcx - kernel32`. 이어서 `kernelbase+0x5060`·`kernel32+0x2c5d0` 에서 `k`.
  - 예상 콜스택 `[추측]`: `KERNEL32!BaseDllInitialize+0x22` → `_BaseDllInitialize` (일반 `CALL` 이라 프레임이 남음). KernelBase 쪽은 `KernelBaseBaseDllInitialize` 가 꼬리 `JMP` 라서 `_KernelBaseBaseDllInitialize` 가 `KernelBaseDllInitialize+0xd` 바로 아래에 보일 것.
- `LdrpInitializeNode` [KERNELBASE]/[KERNEL32] 대응 → 동적 **Q22** (같은 `ln` 으로).
- `Kernel32ThreadInitThunkFunction` 칸 값 → 동적 **Q48**: `ln @rax` 예측 `KERNEL32!BaseThreadInitThunk` (`kernel32+0x2cd70`).
- `TermsrvGetWindowsDirectoryW`·`KernelbasePostInit` 찾은 값 → 동적 **Q49**: 예측 `kernel32+0x35f10`, `kernelbase+0xf4780`.
- `BaseThreadInitThunk` 안 간접 호출 대상, `BasepInitializeTermsrvFpns` 안쪽 → 정적 **Q18** (단서: 위 호출 관계).
- 정적으로 못 정한 값: `_BaseDllInitialize +0x364 ~ +0x380` 갈래를 test.exe 가 타나 (`RtlGetSuiteMask` 값), api-set 102개가 실제로 어느 DLL 이 되나 (ApiSet 맵, Q20 근처).
- kernelbase 를 **처음** 들이는 import 바퀴가 이름 그대로 `KERNELBASE.dll` 항목인지, 그보다 앞의 api-set 항목이 KERNELBASE 로 풀린 것인지 → 정적 **Q51** (kernel32 import 서술자 순서, `pefile`). 이번 결과서엔 순서가 없다. api-set 이 실제로 무엇으로 풀리나는 동적 Q20.

## 도구 메모

- Ghidra 는 PE 진입점 함수를 `entry` 로 잡는다. PDB 이름으로 정규식을 걸면 0개 → `pdb_aliases.py --where <바이너리> <PDB이름>` 으로 `_index.tsv` 이름을 먼저 본다.
- `pdb_aliases.py --lookup` 은 이름이 하나뿐인 주소엔 "별칭 무리 아님" 만 말한다. 이름 확인은 `--where`.

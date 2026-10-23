# 20 dll-entry — 정적 분석 결과

- 요청서: `_workspace/sast/20_dll-entry.md`
- 모듈 / 바이너리: kernel32 (처음 분석), KernelBase (이미 analyze 된 것 재사용, 다시 안 돌림). 둘 다 `ms/bin/.../System32/` 의 26100.9457.
  - kernel32 PDB: `ms/sym/kernel32.dll.pdb`, guid+age = `AE9E9D8F7AC1467A645320FDC23235D01` (바이너리의 CodeView 에서 읽어 받은 것이라 빌드와 항상 맞음. 요청서의 "맞는지 확인" 은 통과). 별칭 표 `ms/sym/kernel32.dll.aliases.tsv` 만듦 (31곳).
  - kernel32 analyze: 29초, 함수 2633개, `SetPdb:` 줄 있음, `ms/decomp/kernel32/_index.tsv` 있음. 
- 뽑은 함수 (모두 `ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`):
  - kernel32: `^(entry|BaseThreadInitThunk)$` depth 1 (6개) + `^_Kernel32LegacyLib_BaseDllInitialize$` depth 0 (1개, 아래 "범위 넓힌 이유")
  - KernelBase: `^(entry|KernelbasePostInit)$` depth 1 (14개) + `^_KernelBaseBaseDllInitialize$` depth 0 (1개)
  - **Ghidra 는 두 DLL 의 진입점 함수를 `entry` 라는 이름으로 잡는다.** PDB 이름(`BaseDllInitialize`, `KernelBaseDllInitialize`)으로 정규식을 걸면 0개가 나온다 → `^entry$` 로 뽑았다. 파일은 `kernel32/entry.{c,asm}`, `kernelbase/entry.{c,asm}`.
- PE 표 읽기: `pefile` (파이썬) 로 두 DLL 의 `AddressOfEntryPoint`, import/delay-import, export 표를 읽었다 (스크립트: 스크래치패드의 일회용, 레포 밖).
- 버전 확인: 이 두 모듈은 스킬 표에 "알려진 사실" 줄이 없다. 대신 (1) PDB 가 바이너리 CodeView guid 로 받은 것이고, (2) PE 진입점 rva 에 PDB 가 정확히 `BaseDllInitialize` / `KernelBaseDllInitialize` 를 두었고, (3) `entry.asm` 의 `CALL +0x1d` → ret `+0x22` 를 `objdump` 바이트(`18002c5ed` 5바이트 → `18002c5f2`)로 직접 맞춰 보았다. 셋 다 맞음. 판정에 영향 없음.

## 스크립트 문제

- 없음 (실패 없음). 참고만:
  - `_index.tsv` 와 `.asm` 머리에는 두 진입점이 PDB 이름이 아니라 `entry` 로 나온다 (Ghidra 가 PE 진입점 라벨을 먼저 붙임). 지금은 `pdb_aliases.py --where <바이너리> <PDB이름>` 이 `_index.tsv 이름: entry` 라고 알려 줘서 길을 찾을 수 있었다. 문서에는 PDB 이름을 쓰고 Ghidra 이름 `entry` 를 한 번 밝히면 된다.
  - `pdb_aliases.py --lookup` 은 이름이 하나뿐인 주소엔 "별칭 무리 아님" 만 말한다 (진입점 이름은 `--where` 로 확인).

## 질문별 답 [정적]

### 1. PE 진입점 → PDB 이름

| 모듈 | PE `AddressOfEntryPoint` | 그 주소 PDB 이름 (같은 주소에 다른 PDB 이름) | 요청서가 말한 `XxxDllInitialize` rva | 차이 |
|---|---|---|---|---|
| kernel32 | `0x2c5d0` | `BaseDllInitialize` (하나뿐) | `BaseDllInitialize` = `0x2c5d0` | **0 (정확히 같음)** |
| KernelBase | `0x5060` | `KernelBaseDllInitialize` (하나뿐) | `KernelBaseDllInitialize` = `0x5060` | **0 (정확히 같음)** |

- 답 (예/아니오): **예. 두 진입점 모두 요청서의 두 이름과 rva 가 정확히 같다.** 별칭도 없다 (같은 주소에 PDB 이름 하나).
- 근거: PE 헤더 `AddressOfEntryPoint` (kernel32 `0x2c5d0`, KernelBase `0x5060`; imagebase 둘 다 `0x180000000`), PDB 공개 심볼 목록 (`near.py` 로 그 주소 앞뒤 0x60~0xa0 를 뽑음), `pdb_aliases.py --where kernel32.dll BaseDllInitialize` → `rva=0x2c5d0 [function]`, `--where KernelBase.dll KernelBaseDllInitialize` → `rva=0x5060 [function]`.
- 이름이 비슷하지만 **다른** 함수 (헷갈리기 쉬움):
  - kernel32: `BaseDllInitialize` (`0x2c5d0`, 55바이트) 가 진입점이고, 바로 뒤 `_BaseDllInitialize` (밑줄, `0x2c610`, 954바이트, 진입점보다 `+0x40`) 가 실제 일을 하는 함수다. 진입점이 `+0x1d` 에서 `_BaseDllInitialize` 를 부른다. `ms/disassemble/kernel32/entry.asm`.
  - KernelBase: `KernelBaseDllInitialize` (`0x5060`, 358바이트) 가 진입점. 이름이 거의 같은 `KernelBaseBaseDllInitialize` (`0x51d0`, 125바이트, 진입점 `+0x170`) 와 `_KernelBaseBaseDllInitialize` (`0x4e6c0`) 는 진입점이 부르는 아래 함수들이다. `ms/disassemble/kernelbase/entry.asm`.
- WinDbg `ln` 이 "근처" 로 보인 이유: 정적으로는 **진입점이 `XxxDllInitialize` 와 정확히 같으니 "다른 이름의 작은 함수/이름 없는 thunk 가 진입점" 이라는 설명은 안 맞는다.** 이유를 정적으로 정할 수는 없고 `[추측]` 만 가능하다.
  - `[추측]` run8 이 `ln` 출력 맨 앞 이름(= 정확히 같은 심볼)을 "근처" 로 적었을 수 있다. `ln` 은 앞뒤 가까운 심볼 둘을 보여 주는 명령이라 `BaseDllInitialize` 다음 줄에 `_BaseDllInitialize` (`+0x40`) 가 같이 뜬다.
  - `[추측]` 또는 그때 `rcx` 가 진입점이 아니라 다른 값이었을 수 있다. → 동적 확인 거리 (아래).

### 2. 이유 1(프로세스 시작)일 때 하는 일 (depth 1)

두 번째 인자 = 이유(reason) 는 두 진입점 모두 `EDX` 에서 읽는다 (kernel32 `entry +0xa` `MOV EDI,EDX`, KernelBase `entry +0x6` `MOV EBX,EDX`).

**KERNEL32 `BaseDllInitialize` (진입점)** — `ms/disassemble/kernel32/entry.asm`
- 호출 목록 (`_calls.tsv`): `+0x1d`(ret `+0x22`) `_BaseDllInitialize`, `+0x31`(ret `+0x36`) `_DllMainCRTStartupForGS2`.
- 이유 == 1 이면 `+0x16 JZ` 로 `+0x31` 에 가서 `_DllMainCRTStartupForGS2` 를 먼저 부르고(`+0x36` 에서 `JMP +0x18`), 그 뒤 모든 이유에서 `_BaseDllInitialize(rcx, edx)` 를 부른다 (`+0x1d`, 꼬리호출 아님 → 콜스택에 `BaseDllInitialize+0x22` 프레임이 보일 것 `[추측]`).
- `_DllMainCRTStartupForGS2` (`0x56898`, 24바이트): 이유 == 1 일 때만 `__security_init_cookie` 를 부르고 `1` 을 돌려준다 (`+0x4` `CMP EDX,1`, `+0x9` CALL). CRT 쿠키 초기화만 한다.
- `_BaseDllInitialize` (`0x2c610`) 가 이유 1 일 때 타는 갈래 (`+0x5f CMP EAX,1` → `+0x68` 이어짐) 의 대표 호출:
  1. `+0x6c`(ret `+0x73`) ntdll `RtlImageNtHeader` — PEB ImageBaseAddress(`[RBX+0x10]`) 의 PE 헤더를 얻음.
  2. `+0x92`(ret `+0x99`) ntdll `RtlSetThreadPoolStartFunc`
  3. `+0xb3`(ret `+0xba`) ntdll `LdrSetDllManifestProber`
  4. `+0xc2`(ret `+0xc7`) `BaseDllInitializeMemoryManager2` (PDB 별칭: `BaseDllInitializeMemoryManager` 도 같은 주소 `0x2c9d8`)
  5. `+0xe9`(ret `+0xf0`) ntdll `RtlSetProtectedPolicy`
  6. `+0x1f9`(ret `+0x200`) ntdll `RtlGetSuiteMask` → `AL & 0x10` 이면 `+0x364 InitializeRegTermsrvFpns` 와 `+0x380 GetRegistryExtensionFlags`(KERNELBASE import, 아래).
  7. `+0x210`(ret `+0x217`) ntdll `LdrDisableThreadCalloutsForDll(hDll)`
  8. `+0x21f`(ret `+0x224`) `_Kernel32LegacyLib_BaseDllInitialize` (`0x2ca24`)
  - 그 밖에 이유 1 에서 값만 쓰는 곳: `+0x189` `BaseStaticServerData`, `+0x194` `BaseCSDVersion`, `+0x1ab` `BaseWindowsSys32x86Directory`, `+0x1e4~+0x1f2` `BasepAppCertDllsList` (전역 변수 채움). 조건부: `+0x32e LdrQueryImageFileExecutionOptions` → `+0x346 SetSearchPathMode`.
- **KERNEL32 시작 함수가 KERNELBASE 쪽 함수를 부르나: 예 (부르는 코드가 있다).** 둘 다 **kernel32 의 import 표에서 `KERNELBASE.dll` 항목**이다.
  - `_BaseDllInitialize +0x380`(ret `+0x387`): `CALL [0x18008a5b0]` = KERNELBASE `GetRegistryExtensionFlags` (조건: `RtlGetSuiteMask & 0x10` 이고 `InitializeRegTermsrvFpns` 성공, `+0x364`~`+0x37c` 조건 통과 시. 터미널 서비스 갈래라 test.exe 에선 안 탈 수 있음 `[추측]`).
  - `_Kernel32LegacyLib_BaseDllInitialize +0x19`(ret `+0x20`, 이유 1 갈래) 와 `+0xae`(ret `+0xb5`): `CALL [0x18008a5b8]` = KERNELBASE `KernelBaseGetGlobalData` (이유 0/1 모두 부르는 줄이 있음. 이유 1 은 `+0x19`, 반환 칸에 0 을 씀). 이건 진입점에서 depth 2 라 **원본 바이트(objdump) + `_calls.tsv` 로 본 것**이다 (이 함수만 depth 0 으로 따로 뽑음: `ms/disassemble/kernel32/_Kernel32LegacyLib_BaseDllInitialize.asm`).
  - 나머지 호출은 ntdll import 이거나 kernel32 안 함수. 진입점 자체(`entry`)는 KERNELBASE 를 직접 부르지 않는다.

**KernelBase `KernelBaseDllInitialize` (진입점)** — `ms/disassemble/kernelbase/entry.asm`
- `+0x8`(ret `+0xd`) `KernelBaseBaseDllInitialize(rcx, edx, r8)` 를 **맨 먼저, 이유와 상관없이** 부른다. 그 뒤 `KernelBaseGlobalData` 의 값을 `SystemRangeStart`, `BaseHeap`, `BaseHeapHandleTablePtr` 로 복사 (`+0xd~+0x3f`).
- 이유 1 이면 (`+0x4f CMP EBX,1` → `+0xa3`, `AL`==1 일 때): 대표 호출
  1. `+0xba`(ret `+0xbf`) `WilInitialize(1)`
  2. `+0xc4`(ret `+0xc9`) `TipInitialize(1)`
  3. `+0xce`(ret `+0xd3`) `FeatureTuningInitialize(1)`
  4. `+0xd3`(ret `+0xd8`) `Initialize` (PDB 이름 `?Initialize@Globals@ARI@@SAJXZ`, 별칭 한 개) — 반환 음수면 `+0x160` 에서 실패(`AL=0`) 로 끝.
  5. `+0xe5`(ret `+0xea`) `AppContainerRegisterInit(1)`, `+0xf1`(ret `+0xf6`) `TraceLoggingRegisterEx_EventRegister_EventSetInformation`
- `KernelBaseBaseDllInitialize` (`0x51d0`) 내용: 이유 1 이면 `RtlInitializeCriticalSectionAndSpinCount(_CrtLock_Exit, 0xfa0)` (`+0x55`), 이유 0 이고 `r8==0` 이면 `RtlDeleteCriticalSection` (`+0x6f`). 그리고 **모든 이유에서** `+0x28` `_DllMainCRTStartupForGS2`(ret `+0x2d`, `0x1290e8`, 24바이트) → `+0x44` 에서 `JMP` 로 **`_KernelBaseBaseDllInitialize` (`0x4e6c0`) 꼬리호출**. (`_calls.tsv`: `+0x44 tail`.)
- 진짜 초기화는 `_KernelBaseBaseDllInitialize` (이유 1 은 `+0x60 JZ` → `+0x195`). 이유 1 대표 호출 (`ms/disassemble/kernelbase/_KernelBaseBaseDllInitialize.asm`):
  1. `+0x199` `BaseDllHandle` 에 DllBase 저장
  2. `+0x1ad`(ret `+0x1b2`) `KernelBaseDllInitializeMemoryManager` (`0xf8344`)
  3. `+0x1cf`(ret `+0x1d6`) ntdll `RtlCreateTagHeap`
  4. `+0x2d2`(ret `+0x2d9`) `CsrClientConnectToServer` — csrss 연결
  5. `+0x4ab` `RtlSetUnhandledExceptionFilter`, `+0x4e1` `BaseNlsDllInitialize`, `+0x501` `RtlInitializeCriticalSection`, `+0x521` `ConsoleInitialize`, `+0x160` `TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation`, 마지막 `+0x16a` `BasepInitializationStage = 0x2bc`, `+0x171`~`+0x189` `g_Initialized` 올림.
  - 이 부분은 depth 2 아래라 갈림길을 줄 단위로 따라가 대표만 적었다. 순서/조건은 `.asm` 의 점프를 읽은 것이고 실행 때 그대로인지는 동적.

### 3. kernel32 의 import 표 (PE import directory 그대로)

- 일반 import: **DLL 이름 104개**. 그중:
  - `api-ms-win-…` 가상 이름 **102개** (import 한 함수 합계 815개). 예: `api-ms-win-core-rtlsupport-l1-1-0.dll`, `api-ms-win-core-processthreads-l1-1-0.dll`(37), `api-ms-win-core-file-l1-1-0.dll`(73), `api-ms-win-core-libraryloader-l1-2-0.dll`(24), `api-ms-win-core-console-l3-2-0.dll`(32). 같은 가족의 버전 다른 이름이 여러 개 (예: `processthreads-l1-1-0/1/2/3`).
  - 진짜 이름 둘: **`ntdll.dll` (366개 함수)**, **`KERNELBASE.dll` (93개 함수)**. → **KERNELBASE.dll 을 이름 그대로 직접 import 한다: 예.**
  - `ext-ms-…` 는 일반 import 에 없음.
- delay import (처음 쓸 때 로드, 시작 때는 안 올라옴): `ext-ms-win-oobe-query-l1-1-0.dll`(1), `ext-ms-win-packagevirtualizationcontext-l1-1-0.dll`(7), `RPCRT4.dll`(10).
- 바운드 import 없음.
- 근거: PE import directory (`pefile`). 참고로 KernelBase 의 일반 import 는 `ntdll.dll`(743) + `api-ms-win-eventing-provider-l1-1-0.dll`(5) + `api-ms-win-core-apiquery-l1-1-0/1-2.dll`(1,1) 뿐이고, 나머지(`ext-ms-win-…` 다수, `WINTRUST.dll`, `OLEAUT32.dll` 등)는 delay import.
- 이것이 run20·run23 `[추측]` 에 주는 것 `[정적]`: "kernel32 를 들이면 KERNELBASE 가 따라 들어온다" 는 **api-set 번역 없이도** 성립한다. kernel32 가 `KERNELBASE.dll` 을 진짜 이름으로 import 하기 때문이다. 다만 102개 `api-ms-win-…` 이름이 무엇으로 바뀌는지(대부분 KERNELBASE 일 것 `[추측]`, 일부는 다른 DLL 일 수 있음)는 실행 때 ApiSet 맵을 봐야 안다.

### 4. ntdll 이 이름으로 찾는 세 함수

| 이름 | 모듈 | export 표에 있나 | ordinal(번호) | rva | 전달(forwarder)? | PDB 이름 |
|---|---|---|---|---|---|---|
| `BaseThreadInitThunk` | kernel32 | 있음 | 75 | `0x2cd70` | **아니오** (코드 주소) | `BaseThreadInitThunk` (76바이트) |
| `TermsrvGetWindowsDirectoryW` | kernel32 | 있음 | 1511 | `0x35f10` | **아니오** | `TermsrvGetWindowsDirectoryW` (18바이트) |
| `KernelbasePostInit` | KernelBase | 있음 | 1062 | `0xf4780` | **아니오** | `KernelbasePostInit` (93바이트) |

- 셋 다 export 표 항목이 가리키는 rva 가 export 디렉터리 범위 밖의 코드 주소다 (kernel32 export dir `0xa65f0` 크기 `0xed6c`, KernelBase `0x391ab0` 크기 `0x1006c`; `pefile` 이 `forwarder=None`). 참고: kernel32 export 1697개 중 211개, KernelBase 2037개 중 104개는 forwarder.
- **run20 의 남은 `[추측]` "BaseThreadInitThunk 가 forwarder 인지" → 아니다 `[정적]`.** 이름으로 찾으면 kernel32 안 `0x2cd70` 코드가 바로 나온다.
- 같은 주소에 다른 PDB 이름 없음 (별칭 무리 아님).
- `TermsrvGetWindowsDirectoryW` 몸통: 전역 `[0x1800c1d10]` 에서 함수 포인터를 읽어 0 이 아니면 CFG 스텁(`0x180087010`)을 거쳐 그 포인터로 점프, 0 이면 `ret` (18바이트, 원본 바이트 objdump). 값은 터미널 서비스 쪽에서 채워 주는 포인터라 `[추측]`, 실제 값은 동적.
- `BaseThreadInitThunk` 호출 목록 (depth 1, `ms/disassemble/kernel32/BaseThreadInitThunk.asm`, 인자: `ECX`=첫 인자, `RDX`=시작 주소, `R8`=파라미터):
  - 첫 인자 0 이면: `+0x12`(ret `+0x17`) `indirect:cfg` — `RAX=RDX`(시작 주소) 를 `RCX=R8` 로 간접 호출 (CFG 스텁 `0x180087010` 경유; 대상은 동적). 그 뒤 `+0x19`(ret `+0x20`) ntdll `RtlExitUserThread(eax)`.
  - 첫 인자 0 아니면(`+0xa JNZ` → `+0x26`): `+0x26`(ret `+0x2d`) ntdll `RtlGetSuiteMask`, `AL & 0x10` 이면 `+0x36`(ret `+0x3b`) `BasepInitializeTermsrvFpns` (`0x2cdc4`).
  - `BasepInitializeTermsrvFpns` 안 호출(이름만): `IsTerminalServerCompatible`, `IsSystemLUID`, `IsTSAppCompatEnabled`, `LoadLibraryExW`, `GetProcAddress`.

### 5. DllMain 모양

- 두 진입점 모두 **CRT 식 `_DllMainCRTStartup` 을 거치지 않는다.** 대신 CRT 의 작은 조각 `_DllMainCRTStartupForGS2` (24바이트, 이유 == 1 일 때 `__security_init_cookie` 만 부르는 함수, 각 DLL 에 하나씩) 만 부르고, 이어서 `XxxDllInitialize` 류 전용 함수로 간다:
  - kernel32: `BaseDllInitialize`(진입점) → (이유 1) `_DllMainCRTStartupForGS2` → `_BaseDllInitialize`.
  - KernelBase: `KernelBaseDllInitialize`(진입점) → `KernelBaseBaseDllInitialize` → `_DllMainCRTStartupForGS2` → (꼬리호출) `_KernelBaseBaseDllInitialize`.
- 진입점 `entry.asm` 안에 `DllMain` 이라는 이름의 함수는 없다. (dll1·dll2 는 `_DllMainCRTStartup` → `DllMain` 으로 run8 에서 정확히 일치했던 것과 다름.)

## 그림 2 `d1`·`d2` 상자 글을 이렇게 바꾸면 된다

- `d1`: "KERNELBASE 시작 함수<br/>KernelBaseDllInitialize 근처" → **"KERNELBASE 시작 함수<br/>KernelBaseDllInitialize (PE 진입점 rva 0x5060, 정확히 같음)"** — 안에서 `KernelBaseBaseDllInitialize` → `_KernelBaseBaseDllInitialize` 로 이어짐. (`[정적]`, 실행 때 정말 이 주소로 들어가는지는 Q21 에서 확인 전이면 "근처" 표현을 `[확인 필요]` 로 남겨도 됨)
- `d2`: "KERNEL32 시작 함수<br/>BaseDllInitialize 근처" → **"KERNEL32 시작 함수<br/>BaseDllInitialize (PE 진입점 rva 0x2c5d0, 정확히 같음)"** — 안에서 `_BaseDllInitialize`(`0x2c610`) 를 부르고, 그 아래에서 KERNELBASE 의 `KernelBaseGetGlobalData` 등을 부르는 코드가 있다.

## 동적분석으로 확인해야 할 것

- dast 요청 필요: 정적으로는 **"실제로 진입점 주소로 들어갔나, `ln` 이 정확히 보이나"** 를 못 정한다. 새 번호는 만들지 않고 기존 대기 항목에 얹으면 된다:
  - **Q21** (`LdrpCallInitRoutine` → KERNELBASE·KERNEL32 시작 함수 실제 진입 + 콜스택): 이번 값으로 확인 거리가 구체적으로 생김.
    - `LdrpCallInitRoutine` 입구에서 `rcx`(진입 함수) 의 `ln @rcx` 와 `? @rcx - kernel32`(또는 `- kernelbase`) 가 각각 **`0x2c5d0` / `0x5060`** 이면 정확히 같다 (정적 예측). 이유(`r8`)가 1 일 때.
    - 콜스택에 `KERNEL32!BaseDllInitialize+0x22 → _BaseDllInitialize` 와 `KERNELBASE!KernelBaseDllInitialize+0xd → KernelBaseBaseDllInitialize` 가 보이는지 (둘 다 일반 `CALL` 이라 프레임이 남을 것 `[추측]`). `KernelBaseBaseDllInitialize` 가 꼬리호출(`+0x44`)로 `_KernelBaseBaseDllInitialize` 를 부르므로 그 아래에서 콜스택에는 `KernelBaseBaseDllInitialize` 프레임이 안 보이고 `_KernelBaseBaseDllInitialize` 가 `KernelBaseDllInitialize+0xd` 아래에 바로 붙어 보일 것 `[추측]`.
  - **Q22** (`LdrpInitializeNode` [KERNELBASE]/[KERNEL32] 대응): 위 `ln` 결과와 함께.
  - **Q48** (`LdrpInitializeProcess +0x1b59 Kernel32ThreadInitThunkFunction` 간접 호출): 이번 결과로 `ln @rax` 예측 = `KERNEL32!BaseThreadInitThunk` (export rva `0x2cd70`, forwarder 아님).
  - **Q49** (`LdrpInitializeKernel32Functions` 실행 값): `GetProc(kernel32, "TermsrvGetWindowsDirectoryW")` 결과 `ln` 예측 = `KERNEL32!TermsrvGetWindowsDirectoryW` (`0x35f10`), `KernelbasePostInit` 예측 = `KERNELBASE!KernelbasePostInit` (`0xf4780`).
- 추천 멈춤점: `ntdll!LdrpCallInitRoutine` 입구 (조건: `r8 == 1` 이고 test.exe 일 때), 이어서 `kernel32+0x2c5d0` / `kernelbase+0x5060` (모듈이 올라온 뒤).
- 정적으로 못 정한 것: `_BaseDllInitialize` 안 `+0x364`~`+0x380` (`GetRegistryExtensionFlags`) 갈래를 test.exe 가 타는지 (`RtlGetSuiteMask` 값이 런타임), `BaseThreadInitThunk +0x12` 의 간접 호출 대상, api-set 102개 이름이 실제로 어느 DLL 이 되는지.

## Q18 (`BaseThreadInitThunk` callee) 에 넘길 단서 한 줄

`BaseThreadInitThunk(ECX, RDX=시작주소, R8=파라미터)`: `ECX==0` 이면 `+0x12` 에서 CFG 스텁(`0x180087010`) 거쳐 `RDX(R8)` 간접 호출 후 `+0x19` `RtlExitUserThread`; 아니면 `+0x26` `RtlGetSuiteMask`(ntdll) → `&0x10` 이면 `+0x36` `BasepInitializeTermsrvFpns`. (forwarder 아님, rva `0x2cd70`.)

## 범위 넓힌 이유

- 요청서는 depth 1 만 말했지만, 이유 1 갈래의 "대표 호출 3~5개" 와 "KERNEL32 → KERNELBASE 호출" 질문은 진입점이 아닌 한 단계 아래 함수에서 일이 일어난다. 그래서 `_Kernel32LegacyLib_BaseDllInitialize` 와 `_KernelBaseBaseDllInitialize` 만 depth 0 으로 딱 하나씩 더 뽑았다 (`_BaseDllInitialize`·`KernelBaseBaseDllInitialize` 는 depth 1 에 이미 포함). 지도(`map`)는 만들지 않았다 (kernel32 에 뿌리 지정이 없고 호출 몇 개 안이라 불필요).

## 판정

**분석성공.** PDB·analyze·depth 1 추출 완료, 질문 5개 모두 답했고, 진입점 = `BaseDllInitialize`/`KernelBaseDllInitialize` 정확히 같음, 세 export 는 forwarder 아님, kernel32 가 KERNELBASE.dll 을 직접 import 함이 확인됨. 실행 때의 갈림길·간접 호출·`ln` 출력은 동적 확인 거리로 남김.

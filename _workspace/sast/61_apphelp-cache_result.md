# 61 apphelp-cache — 정적 분석 결과 (Q125)

- 요청서: `_workspace/sast/61_apphelp-cache.md`
- 모듈 / 바이너리
  - ntdll (`ms/bin/.../System32/ntdll.dll`) — 이미 분석돼 있어서 다시 안 걸었다.
  - ntoskrnl (`.../System32/ntoskrnl.exe`) — 이미 분석돼 있음. PDB guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준값과 같음.
  - **ahcache.sys (`.../System32/drivers/ahcache.sys`) — 이번에 새로 분석했다 (범위를 넓힌 이유는 바로 아래).** PDB guid+age `D54D2D17350903291E74D1E13B85E1831`, `SetPdb:` 줄 있음, `_index.tsv` 669줄.
- 버전 확인: 일치
  - ntdll `LdrpInitializeProcess` call `+0x1bb6` / ret `+0x1bbb` → `LdrpInitializeGraphRecurse` (`_calls.tsv`)
  - ntoskrnl `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess`
  - ntdll `LdrpInitializeNode` ret `+0x16b` / `+0x19c`
  - 커널 PDB GUID 도 같음.
- 뽑은 것 (`ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`)
  - ntdll: `LdrpSendPostSnapNotifications`, `LdrpCheckModule`, `CompatCachepLookupCdb`, `NtApphelpCacheControl` (depth 1 → 18개).
  - ntoskrnl: `NtApphelpCacheControl` (depth 0).
  - ahcache: `AhcDriverDispatchDeviceControl`, `AhcDispatch`, `AhcApiLookupCdb` (depth 1 → 20개), 거기에 `AhcInitialize`, `DriverEntry`, `AhcCdbCreate`, `AhcpAVLTableCompare`, `AhcCdbReadRegistry`, `AhcpCdbEntryLoad` (depth 0).
  - refs: ntdll `g_pShimmedModuleList`, `g_pShimmedModuleListLength`, `g_ShimsEnabled`, `g_ShimsLoading`, `g_pfnApphelpCheckModuleProc`, `g_pfnSE_DllLoaded`. ahcache `AhcEnabled`, `AhcCdbHandle`. (`ms/disassemble/<모듈>/_refs/`)

## 범위를 넓힌 이유 (먼저 읽을 것)

- **커널 `NtApphelpCacheControl` 은 혼자 대답하지 않는다.** 클래스 `0x6` 은 `\Device\ahcache` (ahcache.sys 드라이버) 에 IOCTL 을 던지고, 그 드라이버가 돌려준 상태를 그대로 돌려준다.
  - 그래서 "캐시가 비었을 때 상태값" (질문 4) 은 ntoskrnl 만으로는 못 답한다 (`Ahc*` 하위 호출이 ntoskrnl 에는 `AhcCacheQueryHwId` 하나뿐이고, 그건 클래스 9 전용).
  - 요청서가 "지도로 찾아 그것만" 이라고 했는데, 지도 (ntoskrnl `_map`) 에는 이 길이 없다. 대신 `NtApphelpCacheControl.asm` 의 `IoGetDeviceObjectPointer`·`IoBuildDeviceIoControlRequest`·`IofCallDriver` 와 문자열 `\Device\ahcache` 로 찾았다.
  - ahcache.sys 는 `ms/bin` 에 이미 있었고 작아서 (376 KB) analyze 몇 분이었다. 클래스 `0x6` 갈래 (`AhcDriverDispatchDeviceControl` → `AhcDispatch` → `AhcApiLookupCdb`) 만 뽑았다.
- 도구 메모 (스크립트 문제 아님): ahcache analyze 로그에 `WindowsResourceReference` 스크립트 로딩 경고 (`osgi.ee=UNKNOWN`) 가 한 번 나왔다. 분석은 끝까지 갔고 (`Post-analysis succeeded`, `SetPdb:` 있음, `_index.tsv` 생김) 결과에 영향 없어 보인다.

## 한 줄 결론 (PE_emulator 용, 질문 6)

- **기본 대답 (성공, 출력 안 씀) 은 안전하다.** ntdll 이 부르기 *전에* 출력을 직접 0 으로 비운다.
  - `CompatCachepLookupCdb +0x29/+0x2e` 가 0x188 바이트 버퍼 전체를 `memset(0)`. 출력 DWORD (스택 `RSP+0x1d0`) 는 `+0x33` 에서 따로 0 으로 쓴다.
  - 돌아온 값이 0 이상이면 `([출력 DWORD] & 마스크) != 0` 인지만 본다 (`+0xbd/+0xc4`). 안 쓰면 0 → "해당 없음" 으로 끝. 음수여도 똑같이 0 으로 끝 (`+0xbb`).
  - 진짜 (shim 걸릴 일 없는 모듈) 도 같다: ahcache 가 **못 찾으면 `STATUS_SUCCESS` + 출력 0** 을 준다. 그래서 "성공 + 출력 0" 이 가장 진짜에 가깝다.
- 스택 쓰레기를 읽는 길은 없다 (위 두 줄). **shim 갈래로 가는 열쇠는 "출력 DWORD 의 비트 `0x10` 이 켜지는 것" 하나뿐이다.**
  - 켜지면 `LdrpCheckModule` 이 `g_pShimmedModuleList` 를 채우고, 바로 뒤 `LdrpPrepareModuleForExecution +0x88/+0x8d LdrpDynamicShimModule` 이 그 목록을 보고 apphelp 를 올려 부른다 (질문 2 끝). 기본 대답은 이 비트를 절대 안 켜므로 그 길은 안 열린다.
  - 그러니 PE_emulator 는 출력 DWORD 에 **비트 `0x10` 을 켜는 값을 쓰지 말 것.**
- `STATUS_NOT_FOUND` (`0xC0000225`) 를 줘도 결과는 같다 (ntdll 이 음수는 0 으로 뭉갠다). 그래도 진짜는 보통 `0` 이므로 `0` 을 권한다.
- 줄 때 쓸 약속 한 줄: `NtApphelpCacheControl(ECX=Service, RDX=Data)`: `Service==6` 이면 `STATUS_SUCCESS`, 가능하면 `*(DWORD*)*(QWORD*)(Data+0x178) = 0` (안 써도 됨).

## 질문별 답 [정적]

### 1. `LdrpSendPostSnapNotifications` (rva `0xc240`, 321B)

- **인자**: `RCX` = **DDAG 노드** (`_LDR_DDAG_NODE*`), 모듈 항목이 아니다.
  - `+0x2a MOV R14,RCX`, 모듈 목록은 노드 `+0x0 Modules` 리스트 (`+0x7a MOV RDI,[R14+8]`, 한 칸씩 `[RDI+8]` 로 이동, `+0x7e/+0x81` 와 `+0xe1/+0xe4` 에서 `R14` 로 돌아오면 끝).
  - 모듈 항목 = 리스트 칸 − `0xa0` (`+0x8e LEA RBX,[RDI-0xa0]`, `_LDR_DATA_TABLE_ENTRY.NodeModuleLink` = `+0xa0`, `pdb_struct`).
  - 반환 `EAX = R12D` (`+0xfc`): 보통 0, 앱 검증기 갈래가 실패 상태를 주면 그 값. 노드 쪽은 `LdrpNotifyLoadOfGraph +0x25/+0x27` 에서 음수면 `State` 를 6 으로 되돌린다.
- **들어갈 때 한 번**: `+0x4a/+0x4f RtlEnterCriticalSection(LdrpDllNotificationLock)` (끝에서 `+0xf2/+0xf7` 로 푼다). 아래 loop 전체 (syscall 포함) 가 이 락 안이다.
- **`+0xb6/+0xbb LdrpCheckModule` 을 부르는 조건**: 노드 칸·PEB 비트·`ShimData` 가 **아니다.** 오직 하나:
  - 함수 들어갈 때 `g_pShimmedModuleList` 가 0 이면 부른다. (`+0x23 CMP [g_pShimmedModuleList],0`, `+0x46 CMOVNZ R15D,1` → `+0xad TEST R15,R15`, `+0xb0 JNZ` 목적지 `+0xbb` 로 **건너뜀**).
  - R15 는 들어갈 때 한 번만 정해진다 (loop 도중에 목록이 생겨도 이번 호출의 나머지 모듈은 그대로 부른다).
  - 인자: `RCX = [RBX+0x50]` = 모듈 항목 `FullDllName.Buffer` (전체 경로 문자열 포인터).
  - `g_pfnApphelpCheckModuleProc`·`ShimData` 는 이 함수에도 `LdrpCheckModule` 에도 안 나온다 (refs: `g_pfnApphelpCheckModuleProc` 읽는 곳 = `LdrpGetProcApphelpCheckModule +0x7c/+0x2a1`, 쓰는 곳 = `LdrpGetShimEngineInterface +0x552` 뿐).
  - test.exe: `g_pShimmedModuleList` 처음 값 0 (`.data` 의 파일에 없는 0 채움 구간: `.data` 가상 크기 `0x8f58` > 파일 크기 `0x2000`, rva `0x1d23d8` 은 뒤쪽. 쓰는 곳은 `LdrpCheckModule +0xb0`, `LdrpDynamicShimModule +0x61` 뿐) → 건너뛰는 갈래는 **정상 길에선 안 탄다.** 모든 모듈이 `LdrpCheckModule` 을 부른다.
- 나머지 호출 조건 (한 줄씩):

| 호출 (call/ret) | 조건 | test.exe 에서 |
|---|---|---|
| `+0xa3/+0xa8 SbUpdateSwitchContextBasedOnDll(모듈, 0)` | 조건 없이 부름 (앱 검증기 갈래를 거쳐도 `+0x9d` 로 와서 부름). 안에서 `SbGetCurrentSwitchContext` 가 0 이면 바로 끝 (`SbUpdateSwitchContextBasedOnDll +0x2f/+0x32`) | 부름. 안의 실제 일은 안 봄 (요청서: 이름만) |
| `+0xc1/+0xc6 LdrpSendDllNotifications(모듈, 1)` | 조건 없이 부름. 안에서 `LdrpDllNotificationList` 가 비었으면 락만 풀고 끝 (`LdrpSendDllNotifications +0x59/+0x5c`) | 부름. 등록된 알림이 없으면 바로 끝 (`LdrRegisterDllNotification` 호출자가 없으면) |
| `+0xd8/+0xdd LdrpLogDllState` | 조건 없음. 그 앞 `+0xce` 에서 항목 `Flags |= 8` (`LoadNotificationsSent`) | 부름 |
| `+0x122/+0x127` 간접(CFG) = `g_pfnSE_DllLoaded` 를 푼 값 | `g_ShimsEnabled != 0` (`+0x4f/+0x56`, 아니면 `+0x7a` 로 건너뛰어 RSI=0) **그리고** 푼 값 `!= 0` (`+0xa8/+0xab`, 목적지 `+0x113`) **그리고** `g_ShimsLoading == 0` (`+0x113/+0x11a`, 아니면 `+0xad`). 부르고 나면 `+0x127` 에서 항목 `+0x69` 비트 3 (= `ShimEngineCalloutSent`, Flags 비트 11) 를 켬 | 안 탄다. `g_ShimsEnabled` 에 상수 1 을 쓰는 곳은 `LdrpLoadShimEngine +0x148` 뿐이고 (다른 쓰기 `LdrpInitializeProcess +0x1ceb`·`LdrpGetShimEngineInterface +0x129` 는 레지스터 값이라 값은 확인 안 함), 그건 `LdrpInitializeProcess +0x19ef LdrpInitShimEngine` 쪽 (본문은 이번에 안 봄). run59 사진에서 `g_pfnSE_*` 도 0 `[관찰]` |
| `+0x130/+0x135 AVrfDllLoadNotification` | `PEB.NtGlobalFlag & 0x100` (`+0x17 MOV RAX,GS:[0x60]` → `+0x40 MOV EBP,[RAX+0xbc]`, `+0x88 AND EBP,0x100`, `+0x97 JNZ` 목적지 `+0x12d`). 안에서도 같은 비트를 한 번 더 본다 (`AVrfDllLoadNotification +0x20`). 음수를 돌려주면 loop 를 끝내고 그 값으로 반환 (`+0x13a JNS` → 아니면 `+0x140 JMP` 목적지 `+0xe6`) | 기본 test.exe 는 비트 꺼짐이 보통 → 안 탄다. 값은 동적으로 확인 (`dd @$peb+0xbc L1`) |

- **정적으로 닫힘.** 동적 몫: `PEB.NtGlobalFlag` 실제 값 한 번.

### 2. `LdrpCheckModule` (rva `0xcf50`, 339B)

- **인자**: `RCX` = 전체 경로 문자열 (`+0x1d MOV RDI,RCX`). 호출 때 `EDX = 0x10` (`+0x18`) 를 `CompatCachepLookupCdb` 에 마스크로 넘김.
- `+0x20/+0x25 CompatCachepLookupCdb` 앞에 조건 **없다.** 시작 정리 몇 줄 (`+0x18`~`+0x1d`) 뒤 바로 부른다.
- 반환을 읽는 법: `+0x28 TEST EAX,EAX; +0x2a JNZ` 목적지 `+0x46`.
  - **0 이면** 아무것도 안 하고 돌아간다 (`+0x2c ~ +0x44`). 할당·복사·해제 없음.
  - **0 이 아니면** (= 이 모듈이 DB 에서 비트 `0x10` 이 켜진 모듈) 이름을 "shim 대상 모듈 목록" 에 이어 붙인다:
    - `+0x46~+0x55` 이름 길이 세기. `+0x57 CMP [g_pShimmedModuleList],0; +0x5e JNZ` 목적지 `+0xef` (목록이 이미 있으면 새 크기 = 옛 길이 + 1 + 이름 길이).
    - `+0x7e/+0x83 RtlAllocateHeap(PEB.ProcessHeap, 8=HEAP_ZERO_MEMORY, 크기)`. 실패 (`+0x89 JZ` 목적지 `+0xc3`) 면 `+0xe5/+0xea LdrpLogInternal` 로 로그만.
    - 목록이 처음이면 `+0xab/+0xb0 RtlCopyMemory` 로 이름 복사 후 `+0xb0 [g_pShimmedModuleList]=새 블록`, `+0xb7 [g_pShimmedModuleListLength]=길이` (둘 다 ntdll 전역 `.data`, rva `0x1d23d8`·`0x1d2940`).
    - 이미 있으면 `+0x112/+0x117` 옛 내용 복사, `+0x12f/+0x134` 새 이름 복사, `+0x14a/+0x14f RtlFreeHeap(옛 목록)`, 다시 `+0xb0` 로 합류.
  - 만드는 것은 **NUL 로 이어 붙인 전체 경로 문자열 목록 하나** (힙) 와 길이 전역 하나. 노드 칸·항목 칸은 안 건드린다.
- **목록이 비지 않으면 생기는 일 (shim 길의 입구)**: `LdrpPrepareModuleForExecution +0x88/+0x8d` 가 `LdrpNotifyLoadOfGraph` 바로 뒤에 `LdrpDynamicShimModule(노드)` 를 부른다 (이름만 봄, 본문은 범위 밖).
  - 목록이 **0 이면 아무 일 없이 끝**: `LdrpDynamicShimModule +0x18` 이 상태 DWORD (ntdll rva `0x1cd7cc`, 파일에 든 처음 값 `1`) 를 읽고, `+0x2f CMP [g_pShimmedModuleList],0; +0x36 JNZ` (안 뜀) → `+0x38 CMP ESI,1; +0x3b JNZ` (안 뜀) → `+0x3d CMP …,0; +0x44 JZ` 목적지 `+0x6f` → `+0x74 MOV EAX,EBX` (`EBX=0`) 로 반환. 이 DWORD 를 쓰는 곳은 `LdrpDynamicShimModule +0x7b`·`+0x96` 둘뿐 (refs).
  - 목록이 **0 이 아니면** `+0x36 JNZ` 목적지 `+0x91` → `+0x96` DWORD 를 0 으로 쓰고 → `+0x9d/+0xa2 LdrpGetProcApphelpCheckModule` (apphelp 를 올리는 쪽, `g_pfnApphelpCheckModuleProc`) → 목록의 이름마다 간접 호출 `+0xdf/+0xe4` (`indirect:cfg:stack[+0x10]`) → 끝나면 목록을 비운다 (`+0x5c/+0x61 RtlFreeHeap`, `+0x61`·`+0x68` 에서 전역 둘을 0 으로). 요청서가 걱정한 "엉뚱한 shim 갈래" 의 입구가 이것이다.
- 이 목록을 읽는 곳은 `LdrpSendPostSnapNotifications +0x23` (위 건너뛰기 조건), `LdrpCheckModule` 자신, `LdrpDynamicShimModule +0x2f/+0x3d/+0x51/+0xac` (refs). shim 엔진을 올리는 호출 (`LdrpLoadShimEngine`) 은 `LdrpCheckModule` 안에 없다 (callee 일곱 개 모두 위에 나옴).
- **정적으로 닫힘.** (DB 에서 실제 어떤 모듈이 `0x10` 인지는 질문 4 의 동적 몫)

### 3. `CompatCachepLookupCdb` (rva `0xc144`, 244B)

- **인자**: `RCX` = 이름 (UTF-16, 경로 전체여도 됨), `EDX` = 마스크. 돌려주는 값: 0 또는 1.
  - `+0x17 MOV ESI,EDX` (마스크), `+0x19 MOV RDI,RCX`.
- **이름 다듬기**: `+0x3d/+0x42 wcsrchr(이름, '\\')` — 마지막 `\` 가 있으면 그 다음 글자부터 (`+0x4e ADD RAX,2`), 없으면 (`+0x48 JZ` 목적지 `+0xe2`) 이름 그대로 (`+0xe2/+0xe5` → `+0x51`). 즉 **파일 이름만** 보낸다.
  - 이름 포인터가 0 이면 `wcslen` 건너뜀 (`+0x67 JZ` 목적지 `+0x8f`).
  - `+0x6c/+0x71 wcslen`; 길이가 `0x7ffe` 글자보다 길면 (`+0x77 JA` 목적지 `+0xea`) **syscall 안 부르고 0 반환**.
  - 문자열 길이 `Length = 길이×2` (`+0x79~+0x7c`), `MaximumLength = Length+2` (`+0x84`).
- **출력 버퍼를 syscall 전에 비우나: 예 (U1 걱정 닫힘).**
  - `+0x1e LEA RCX,[RSP+0x20]`, `+0x23 MOV R8D,0x188`, `+0x29/+0x2e memset(버퍼, 0, 0x188)` — 첫 일이다.
  - 그 뒤 `+0x2e XOR EBX,EBX`, `+0x33 MOV [RSP+0x1d0],EBX` — **출력 DWORD (0x188 블록 밖, 버퍼 시작 + 0x1b0) 도 0 으로 씀.**
  - 이 뒤로 이 DWORD 에 쓰는 코드는 이 함수 안에 없다 (쓰는 쪽은 커널/드라이버뿐).
- **버퍼 꼴** (시작 = `RSP+0x20`, 크기 `0x188`):

| 칸 (버퍼 시작 기준) | 크기 | 뜻 | 근거 줄 |
|---|---|---|---|
| `+0x000 ~ +0xf7` | 0xf8 | 전부 0 | `+0x29` memset |
| `+0x0f8` | 2 | `UNICODE_STRING.Length` = 이름 길이×2 | `+0x7c MOV [RSP+0x118],AX` |
| `+0x0fa` | 2 | `MaximumLength` = Length+2 | `+0x87 MOV [RSP+0x11a],AX` |
| `+0x100` | 8 | `Buffer` = 파일 이름 시작 (호출자 문자열 안을 가리킴, 복사 안 함) | `+0x5c MOV [RSP+0x120],RAX` |
| `+0x178` | 8 | 출력 DWORD 를 가리키는 포인터 (= `RSP+0x1d0`, 버퍼 밖) | `+0xa7 MOV [RSP+0x198],RAX` |
| `+0x180` | 4 | 출력 크기 = 4 | `+0x97 MOV [RSP+0x1a0],4` |
| 나머지 | — | 0 | memset |

  (그 밖의 칸은 **쓰는 곳 없음**. 커널 쪽 `AhcApiLookupCdb` 도 이 칸들만 읽는다. 아래 질문 4.)
- **`+0xb4/+0xb9 NtApphelpCacheControl(0x6, 버퍼)`**: `+0xaf MOV ECX,6`, `+0xa2 LEA RDX,[RSP+0x20]`. 인자 **2개** (클래스, 버퍼).
- **호출 전 건너뛰는 갈래**: 위 "길이 > 0x7ffe → 0 반환" 하나뿐. 나머지는 다 부른다.
- **돌아온 뒤** (`ret +0xb9`):
  - `+0xb9 TEST EAX,EAX; +0xbb JS` 목적지 `+0xc6` → 음수면 **0 반환.**
  - `+0xbd TEST [RSP+0x1d0],ESI; +0xc4 JNZ` 목적지 `+0xee` → `(출력 DWORD & 마스크) != 0` 이면 **1 반환** (`+0xee MOV EBX,1`), 아니면 0 (`+0xc6 MOV EAX,EBX`, EBX=0).
  - 반환은 0 / 1 둘뿐. 상태값을 호출자에게 전하지 않는다.
- **호출자 다섯의 차이** (이름 · 마스크):

| 호출자 (call/ret) | 이름 인자 | 마스크 `EDX` | 비고 |
|---|---|---|---|
| `LdrpCheckModule +0x20/+0x25` | 모듈 `FullDllName.Buffer` | `0x10` | 이번 길 |
| `LdrpSnapModule +0xe29/+0xe2e` | `[항목+0x50]` (`RSP+0x98` 쪽 항목) | `0x80` (`+0xe18`) | 오류 `0xC000007A` 처리 안 (`+0xe0b CMP R12D,0xc000007a`) — run36 과 같음 |
| `LdrpSnapModule +0xe44/+0xe49` | `[항목+0x50]` (`RSP+0x60` 쪽 항목) | `0x80` (`+0xe36`) | 같은 오류 처리 안 |
| `LdrpLogDeprecatedDllEtwEvent +0x5c/+0x61` | 인자 `UNICODE_STRING` 의 `Buffer` (NUL 끝인지 검사 후) | `0x4` (`+0x54`) | ETW 기록 |
| `LdrpCheckComponentOnDemandEtwEvent +0x56/+0x5b` | `[RCX+8]` (`UNICODE_STRING.Buffer`) | `0x100` (`+0x33`) | ETW 기록 |

- **정적으로 닫힘.**

### 4. ntoskrnl `NtApphelpCacheControl` (rva `0x9f46c0`, 437B) — 클래스 `0x6`

**syscall 표**

| 항목 | 값 | 근거 |
|---|---|---|
| ntdll 스텁 | `NtApphelpCacheControl` rva `0x161730`, 24B | `ms/disassemble/ntdll/NtApphelpCacheControl.asm` |
| syscall 번호 | **`0x4c`** (`+0x3 MOV EAX,0x4c`). `+0x10 JNZ` 목적지 `+0x15` 가 `INT 0x2e` 갈래, `+0x12` 가 `SYSCALL` | sast/51 표 `0x4c` 와 같음 |
| 인자 수 | **2** — `ECX` = 클래스, `RDX` = 서비스 데이터 포인터. 스택 인자 0 | 커널 `+0x26 MOV RSI,RDX`, `+0x35 CMP ECX,0xc`. sast/51 표의 `total_args 4` 는 스택 인자 0 인 syscall 의 자리 수 (레지스터 4칸 상한) 일 뿐, 실제 2개 |
| 클래스 범위 | `ECX >= 0xc` 면 `STATUS_INVALID_PARAMETER` (`+0x35 CMP; +0x38 JGE` 목적지 `+0x165` → `+0x165 MOV EBX,0xc000000d`) | |
| 클래스 `0x6` 의 길 | `+0x5c MOV R15D,0xc0000001` → `+0x62 CMP ECX,5; +0x65 JG` 목적지 `+0x190` → `+0x190 SUB ECX,6; +0x193 JZ` 목적지 `+0x79` → `+0x79 MOV EBX,0xc0000225` (기본값) | |
| 장치 얻기 | `g_AhcDeviceObject` (`+0x7e CMP [0x140eeff40],0`) 가 0 이면 `+0x114 RtlInitUnicodeStringEx(L"\\Device\\ahcache")` (`+0x11f/+0x124`) → `+0x139/+0x13e IoGetDeviceObjectPointer(…, 0xC0000000)` → `+0x148 CMPXCHG.LOCK` 로 전역에 저장 (진 쪽은 `+0x15b/+0x160 ObfDereferenceObject`). **둘 중 하나가 실패하면 `+0x126 JS` / `+0x140 JS` 가 `+0xf9` 로 가서 기본값 (`0xC0000225`) 을 그대로 반환** | |
| IOCTL 만들기 | `+0xcf/+0xd4 IoBuildDeviceIoControlRequest(코드, g_AhcDeviceObject, 입력=RDX 그대로, 입력 길이=0x188, 출력=NULL, 출력 길이=0, InternalDeviceIoControl=FALSE, 이벤트, IoStatusBlock)`. 코드 = `클래스×4 \| 0x220003` (`+0x42 LEA EDI,[RCX*4]`, `+0x4d OR EDI,0x220003`) = 클래스 6 은 **`0x22001B`**, 방식 `METHOD_NEITHER` (아래 3 비트 = 3). **입력 길이는 항상 `0x188` 로 고정** (`+0xab MOV R9D,0x188`) | 사용자 크기를 안 검사한다. 사용자 포인터를 그대로 넘기고 드라이버가 읽는다 |
| 보내기 | `+0xe0/+0xe5 ExGetPreviousMode` → IRP `+0x40` (RequestorMode) 에 저장 → `+0xf2/+0xf7 IofCallDriver`. 돌아온 값이 곧 반환값 (`+0xf7 MOV EBX,EAX`, `+0x112 RET`) | 커널은 상태를 스스로 정하지 않는다 |
| IRP 만들기 실패 | `+0xda JZ` 목적지 `+0x1b7` → `EBX = R15D = 0xC0000001` (조각 `FUN_1409f486f`) | |
| 이벤트 | 반드시 `KeInitializeEvent` 를 `+0x96/+0x9b` 에서 부름 (동기 IRP) | |

**ahcache.sys 쪽 (클래스 6)**

- `AhcDriverDispatchDeviceControl` (rva `0x392d0`): IOCTL 코드의 `(코드 >> 2) & 0xff` 가 클래스 (`+0x23 SHR EAX,2; +0x26 MOVZX R9D,AL`). `>= 0xc` 면 `0xC000000D` (`+0x2a CMP; +0x2e JNC` 목적지 `+0x61`). 입력 포인터 = IRP 스택의 `Type3InputBuffer` (`+0x1f MOV RDX,[RCX+0x20]`), 요청자 모드 = IRP `+0x40` (`+0x30`). `+0x38/+0x3d AhcDispatch` 부른 뒤 상태를 IRP 에 쓰고 (`+0x41`) `+0x47/+0x4e IofCompleteRequest`.
- `AhcDispatch` (rva `0x39360`, `R9D`=클래스, `RDX`=입력, `R8B`=요청자 모드):
  - 사용자 모드 (`+0x2d TEST R8B,R8B` 가 0 이 아님) 면 4바이트 정렬 검사 (`+0x32 TEST DL,3; +0x35 JNZ` 목적지 `+0xb4` → `ExRaiseDatatypeMisalignment`), `RDX+0x188 <= MmUserProbeAddress` 검사 (`+0x37~+0x50`), 그다음 **`0x188` 바이트를 스택 지역 사본 (`RSP+0x40`) 으로 복사** (`+0x52~+0xcb`). 이후는 사본만 쓴다.
  - 클래스 6: `+0x176 CMP R9D,6; +0x17a JNZ` 목적지 `+0x20e` (6 이 아니면 점프표). 6 이면 `+0x18c` ETW 이벤트 (`CompatCdbQueryStart`) 후 `+0x1ae CMP [AhcEnabled],0; +0x1b5 JZ` 목적지 `+0x121` → **`EBX = 0xC0000225` 로 반환.** 아니면 `+0x1bb~+0x1d3 AhcApiLookupCdb(사본+0xf8 = 이름, 사본+0x178 = 출력 포인터, 사본+0x180 = 출력 크기)` (call `+0x1d3`, ret `+0x1d8`) 결과를 반환 (`+0x1d8 MOV EBX,EAX`).
  - `AhcEnabled` 는 `AhcInitialize +0x23c` 에서 **1 로 쓴다** (앞 단계들이 성공한 뒤). `DriverEntry +0x192/+0x197` 이 `AhcInitialize` 를 부른다 (`DriverEntry` 가 `\Device\ahcache` 를 만든다: `+0x114 IoCreateDevice`). 실패하면 정리하고 `AhcEnabled` 를 안 켠다 (`AhcLockCreate`·`AhcCacheCreate`·`AhcCdbCreate`·`AhcSdbQueryCreate`·`CitmInitialize` 반환을 봄. `AhcCacheReadRegistry` 가 실패하면 캐시를 비우고 계속 가고 (`+0x157 TEST; +0x159 JNS`, 아니면 `+0x17f/+0x184 AhcCacheClear`), `AhcCdbReadRegistry` 는 반환을 안 본다). 끌 때는 `AhcDriverUnload +0x7c` 가 0.
- `AhcApiLookupCdb` (rva `0x39850`, `RDX`=이름 `UNICODE_STRING*`(사본 안), `R8`=출력 DWORD 포인터, `R9D`=출력 크기):

| 순서 | 일 | 근거 | 실패 상태 |
|---|---|---|---|
| 1 | 출력 포인터가 NULL 이거나 출력 크기 ≠ 4 면 거절 | `+0x3f TEST R13,R13; +0x42 JZ` · `+0x48 CMP R9D,4; +0x4c JNZ` 목적지 `+0x34c` | `0xC000000D` |
| 2 | 이름 `Buffer`·`Length` 검사 (사용자 범위 검사 `+0x71~+0x87`; `Buffer==0` · `Length==0` · 첫 글자 NUL 이면 거절) | `+0x90 JZ`·`+0x9a JZ`·`+0xa5 JZ` 목적지 `+0x196` | `0xC000000D` |
| 3 | `Length+2` 가 16비트를 넘으면 거절 | `+0xb9 CMP AX,R14W; +0xbd JC` 목적지 `+0x152` | `0xC0000095` |
| 4 | 풀에서 이름 복사본 만들기 (`ExAllocatePool2`, 태그 `'APpt'`) | `+0xe3/+0xe8`, `+0x114/+0x119 memmove` | 실패 `0xC0000017` (`+0x105 JZ` 목적지 `+0x161`) |
| 5 | `AhcCdbHandle` 의 락을 공유로 잡기 (`KeEnterCriticalRegion` + `ExAcquireSharedStarveExclusive`) | `+0x1e4/+0x1eb`, `+0x1f5/+0x1fc`; 락 포인터가 0 이면 `+0x1de JZ` 목적지 `+0x378` (내부 `0xC000000D`), 락 실패면 `+0x203 JZ` 목적지 `+0x382` (내부 `0xC0000022`) | **둘 다 로그만 남기고 `+0x20f TEST EDI,EDI; +0x211 JS` 목적지 `+0x3b9` → `+0x3db JMP` 목적지 `+0x273` 의 마무리로 합류 → 출력 0, 반환 `STATUS_SUCCESS`** (`+0x2dc XOR EDI,EDI`) |
| 6 | **AVL 표에서 이름 찾기**: `RtlLookupElementGenericTableAvl(AhcCdbHandle+8 의 표, 키)`. 키 = 이름 `UNICODE_STRING` 이 원소의 `+0x18` 에 오는 꼴이고, 비교는 `RtlCompareUnicodeString(…, 대소문자 무시)` (`AhcpAVLTableCompare +0x12`) | `+0x24d/+0x254` | — |
| 7a | **찾았으면** 원소 참조수를 올리고 (`+0x33d INC.LOCK [RAX+0x10]`) 원소의 `+0x28` 이 가리키는 DWORD 를 읽어 (`+0x341`, `+0x332 MOV EBX,[R15]`) 그 값이 출력 | | 성공 |
| 7b | **못 찾았으면**: 내부 상태 `0xC0000225` (`+0x262`) 를 `EDI` 에 두지만, 아래 마무리에서 `ESI = 0` 이 되고 (`+0x292 XOR ESI,ESI; +0x294 TEST EDI,EDI; +0x296 CMOVNS ESI,EBX` — 음수라 안 바뀜) | | |
| 8 | 마무리: 참조수 내림 (`+0x278 DEC.LOCK`), 락 풀기 (`+0x285/+0x28c`), `ProbeForWrite(출력 포인터, 4, 4)` (`+0x2a4/+0x2ab`), `*출력 = ESI` (`+0x2b0`), 그리고 **`+0x2b4 JMP` 목적지 `+0x2dc` 의 `XOR EDI,EDI` 로 상태를 0 으로 바꿔 반환** (복사본은 0 으로 지우고 풀 반납 `+0x2f6/+0x2fb`, `+0x303/+0x308`) | | **못 찾아도 `STATUS_SUCCESS`, 출력 0** |

- 그러니까 **클래스 6 의 입력 칸**: `+0xf8` 이름 `UNICODE_STRING` (Length·MaximumLength·Buffer@`+0x100`), `+0x178` 출력 포인터, `+0x180` 출력 크기 (4 여야 함). 나머지는 안 읽는다. 출력: 포인터가 가리키는 DWORD 하나 (= 그 이름의 DB 항목 플래그 묶음).

**실패 상태값 표 (클래스 6)**

| 상태 | 값 | 어디서 | ntdll 에서 결과 |
|---|---|---|---|
| `STATUS_SUCCESS` | `0` | 찾음 / **못 찾음** (출력 0) | 출력 DWORD 로 갈림 |
| `STATUS_NOT_FOUND` | `0xC0000225` | `AhcEnabled == 0` (`AhcDispatch +0x1b5` → `+0x121`); `\Device\ahcache` 를 못 열 때 커널 기본값 (`NtApphelpCacheControl +0x79`) | 음수 → 0 반환 |
| `STATUS_UNSUCCESSFUL` | `0xC0000001` | IRP 못 만듦 (`NtApphelpCacheControl +0x1b7`) | 음수 → 0 |
| `STATUS_INVALID_PARAMETER` | `0xC000000D` | 클래스 ≥ 12, 출력 포인터/크기 이상 (`+0x34c`), 이름 비었음 (`+0x196`) | 음수 → 0 |
| `STATUS_NO_MEMORY` | `0xC0000017` | 풀 할당 실패 (`+0x161`) | 음수 → 0 |
| (`0xC0000022`, 락 포인터 0 의 `0xC000000D`) | — | 락 단계 오류는 내부 값일 뿐, 위 5번 줄처럼 **반환은 `STATUS_SUCCESS`** | 출력 0 |
| `STATUS_INTEGER_OVERFLOW` | `0xC0000095` | 이름 길이 계산이 16비트 넘음 | 음수 → 0 |
| 접근 위반·정렬 예외 | 예외 코드 | 사용자 포인터 검사 / `ProbeForWrite` 실패 (`AhcApiLookupCdb` 의 `+0x2b6~+0x2db` 는 점프로 안 닿는 예외 착지점 `[추측]`) | 음수 → 0 |

**클래스 표 (클래스 6 밖은 번호·핸들러 이름만)**

- PDB 에 열거 (`_APPHELPCACHESERVICECLASS` 등) 는 **없다** (`pdb_struct.py --list` 로 ntoskrnl·ntdll·ahcache 에서 `apphelp|ahc|shim|compatcache` 0건). 아래 이름은 `AhcDispatch` 점프표 (rva `0x19eec`, 11칸 × 4바이트) 와 `Ahc*` 핸들러 이름에서 읽은 것이다.
- 점프표는 `클래스 − 1` 로 찾는다 (`+0x20e DEC R9D; +0x211 CMP R9D,0xa`). 클래스 0 과 6 은 점프표 앞에서 따로 처리하고, 표의 6번 칸 (`+0x41b`)·9번 칸은 `0xC000000D` 로 가는 기본 갈래다.

| 클래스 | 핸들러 (`AhcDispatch` call/ret) | `AhcEnabled==0` 일 때 | 커널 기본값 (장치를 못 열 때) |
|---|---|---|---|
| 0 | `AhcApiLookup` (`+0x202/+0x207`) | `0xC0000225` | `0xC0000225` |
| 1 | `AhcApiRemove` (`+0x354/+0x359`) | `0` | `0` |
| 2 | `AhcApiUpdate` (`+0x2c5/+0x2ca`) | `0` | `0` |
| 3 | `AhcApiClear` (`+0x374/+0x379`) | (검사 없음) | `0` |
| 4 | `AhcApiSnapStatistics` (`+0x3aa/+0x3af`) | `0xC0000001` | `0xC0000001` |
| 5 | `AhcApiSnapCache` (`+0x3e2/+0x3e7`) | `0xC0000001` | `0xC0000001` |
| **6** | **`AhcApiLookupCdb` (`+0x1d3/+0x1d8`)** | **`0xC0000225`** | **`0xC0000225`** |
| 7 | `AhcCdbRefresh` (`+0x405/+0x40a`) | `0` | `0` |
| 8 | `AhcApiMapQuirks` (`+0x306/+0x30b`) | `0xC0000001` (`AhcCdbEnabled==0` 도 같음, 커널 모드 호출은 `0xC0000002`) | `0xC0000001` |
| 9 | 드라이버를 안 거치고 커널 안 `AhcCacheQueryHwId` (`NtApphelpCacheControl +0x16f/+0x174`) | — | — |
| 10 | `AhcApiInitProcessData` (`+0x32a/+0x32f`) | `0` | `0` |
| 11 | `AhcApiLookupAndWriteToProcess` (`+0x278/+0x27d`) | `0xC0000225` | `0xC0000001` |
| ≥ 12 | — | — | `0xC000000D` |

  (공개 헤더의 이름 — Lookup / Remove / Update / Clear / SnapStatistics / SnapCache / LookupCdb / RefreshCdb / MapQuirks / HwIdQuery / InitProcessData / LookupAndWriteToProcess — 은 핸들러 이름과 맞아 보이지만 PDB 근거가 아니라 `[추측]`.)

**"캐시가 부팅 때 채워지나" — 정적으로 어디까지**

- 채우는 코드는 있다: `AhcInitialize +0x1c8 AhcCdbCreate` → `+0x1fd AhcCdbReadRegistry` (레지스트리 `CacheMainSdb` 항목 — `AhcCdbReadRegistry.c`) → `AhcSdbQueryCreate`. `AhcCdbRefresh` (클래스 7) 도 있다. 표 원소는 `AhcpCdbEntryLoad` 쪽 (`AhcpCdbLoadDb`/`AhcpCdbLoadFromPdb`) 에서 만든다.
- 그러나 **표에 `kernel32.dll`·`kernelbase.dll`·test.exe 이름이 들어 있는지, 들어 있다면 플래그 `0x10` 이 켜졌는지는 정적으로 안 정해진다** (sysmain.sdb 와 레지스트리 내용에 달림). → **동적 필요.** 다만 위의 못 찾음 = 성공 + 출력 0, 그리고 ntdll 의 마스크 `0x10` 이 "shim 목록에 올릴 모듈" 이라 일반 시스템 DLL 에 켜질 일은 드물다는 점에서 `[추측]`: 보통 0.

### 5. 노드마다 몇 번

- **노드마다 한 번** (모듈 이름마다가 아니라 DDAG 노드마다) `[정적]`.
  - `LdrpPrepareModuleForExecution` 이 노드 `State` (`+0x38`) 를 읽는다: `+0x31 MOV RCX,[RCX+0x98]` (`DdagNode`), `+0x38 MOV R8D,[RCX+0x38]`, `+0x3c SUB R8D,5; +0x40 JZ` 목적지 `+0x12a` — 5 (Snapped) 면 먼저 `+0x12a/+0x12f LdrpCondenseGraph` 로 6 이 되고 `+0x12f JMP` 목적지 `+0x50` 으로 합류. 6 이면 (`+0x46 SUB R8D,1; +0x4a JNZ` 가 안 뛰고 `+0x50` 으로) `+0x76/+0x7b LdrpNotifyLoadOfGraph(노드)`. 7 은 `+0x111 SUB; +0x115 JZ` 목적지 `+0xbf` 로 알림 없이 가고, 8·9 는 `+0xfe` 로 끝나고 (`+0x117~+0x121`), 그 밖은 `0xC00000E5` (`+0x123`).
  - `LdrpNotifyLoadOfGraph`: 노드의 `Dependencies` 목록 (`+0xa MOV RSI,[RCX+0x28]`) 이 비면 `+0x19` 에서 `State = 7` 을 쓰고 `+0x20/+0x25 LdrpSendPostSnapNotifications(자기 노드)`. 있으면 목록을 돌며 (`+0x44~+0x5d`) 의존 노드의 `State` (`[의존노드+0x38]`) 가 **6 이면 재귀** `+0x61/+0x66`, 7 이상이면 건너뜀 (`+0x53 CMP EAX,7; +0x56 JL` 목적지 `+0x83` 은 7 미만 — 7 이상은 아래로 `+0x58`), 6 도 7 도 아닌 값은 오류 (`+0x83~+0x90`: `-4` 면 `0xC0000142`, 아니면 `0xC00000E5`) 로 끝냄. 그다음 자기 몫은 `+0x73 JNS` 목적지 `+0x16` 부터.
  - 그러니 한 노드의 `State` 는 6 → 7 로 **한 번만** 바뀌고 (`+0x19`), 두 번 알림이 가지 않는다. 의존 노드가 먼저 (깊이 우선, 뒤에서 앞으로) 끝난다.
  - 한 노드 안의 모듈 수만큼 `LdrpCheckModule` → syscall (보통 노드당 모듈 1개).
- 부르는 쪽 둘: `LdrpInitializeProcess +0x1aed/+0x1af2` (exe 노드 한 번; exe 의 정적 import 그래프 전체가 이 한 번의 재귀로 처리됨) 와 `LdrpLoadDllInternal +0x27f/+0x284` (`LdrLoadDll` 마다).
- test.exe 부팅의 추정 (노드 집합 자체는 이 요청서 범위 밖이라 `[추측]`): exe · dll1 · dll2 · KERNEL32 · KERNELBASE = **syscall 5번**. ntdll 노드는 `State` 가 처음부터 9 (run43 기록) 라 `+0x56 JL` 를 지나 건너뛴다 → **0번**. CRT 의 `LoadLibraryExW` 등 나중에 올리는 DLL 은 DLL 마다 한 번 더.
- 정적으로는 규칙까지만 닫힘. **실제 횟수는 동적** (`ntdll!CompatCachepLookupCdb+0xb9` 히트 수).

### 6. 한 줄 결론

위 "한 줄 결론" 절 참고. 요약: 기본 대답 (`STATUS_SUCCESS`, 출력 안 씀) 은 안전하다. 진짜 상태값을 줄 필요 없다. (`STATUS_NOT_FOUND` 류를 줘도 결과는 같지만 진짜는 `0` 이 보통.)

## 질문별 닫힘 / 동적 필요

| 질문 | 상태 | 남은 것 |
|---|---|---|
| 1 | 정적으로 닫힘 | `PEB.NtGlobalFlag` 값 한 번 확인 (앱 검증기 갈래) |
| 2 | 정적으로 닫힘 | — |
| 3 | 정적으로 닫힘 | — |
| 4 | 갈래·상태값은 닫힘 / **DB 내용은 동적 필요** | `AhcEnabled` 실제 값, 표에 실제로 있는 이름·플래그 |
| 5 | 규칙은 닫힘 / **횟수는 동적 필요** | 히트 수 |
| 6 | 정적으로 닫힘 | — |

## 동적분석으로 확인해야 할 것 (Q104 VM 단계에 얹을 것)

타깃 VM 이라 ahcache 가 실제로 로드돼 있는지부터 본다.

- `lm m ahcache` , `dd ahcache!AhcEnabled L1` (1 이어야 `AhcApiLookupCdb` 까지 간다), `dq ahcache!AhcCdbHandle L1`.
- 추천 멈춤점 (test.exe 프로세스일 때만):
  - `ntdll!CompatCachepLookupCdb` 입구 — `du @rcx` (전체 경로), `r edx` (마스크), 히트 수 = 모듈 수.
  - `ntdll!CompatCachepLookupCdb+0xb9` — `r eax` (돌아온 상태), `dd @rsp+1d0 L1` (출력 DWORD; 이 자리에서 RSP 는 함수 안 값이라 `RSP+0x1d0` 가 맞다), 이어서 반환값이 1 인 모듈이 있는지.
  - `ahcache!AhcApiLookupCdb+0x254` — `r rax` (0 이면 못 찾음), 찾았으면 `dd poi(@rax+0x28) L1` (플래그). 이어서 `+0x2b0` 에서 `r esi` (출력으로 쓰는 값).
  - `ntdll!g_pShimmedModuleList` (`x ntdll!g_pShimmedModuleList`, `dq`) — 부팅 끝에서도 0 인지.
  - `dd @$peb+0xbc L1` — `NtGlobalFlag` (앱 검증기 비트 `0x100`).
  - 노드 `State`: 첫 `ntdll!LdrpNotifyLoadOfGraph+0x19` 히트 때 `dd @rbx+0x38 L1` 로 7 이 쓰이는 노드 수 세기 (kernel32/kernelbase/dll1/dll2/exe).

## 스크립트 문제

- 없음. (analyze 로그의 `WindowsResourceReference` 스크립트 로딩 경고는 결과에 영향 없어 보임. ahcache 에서 한 번 나옴.)

## 판정

**분석성공.** 질문 1~3, 6 은 정적으로 닫혔고 (U1 걱정 해소: ntdll 이 출력을 syscall 전에 직접 0 으로 비운다), 질문 4 의 갈래와 상태값표도 닫혔다. 단 클래스 6 의 대답은 ntoskrnl 이 아니라 ahcache.sys 에서 정해져서 새 모듈 하나를 analyze 했다. 남은 동적 몫은 DB 에 실제로 들어 있는 이름·플래그, `AhcEnabled` 실제 값, 노드별 히트 횟수.

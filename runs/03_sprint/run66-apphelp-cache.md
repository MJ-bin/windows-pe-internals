# 2026-11-05 run66 — 모듈을 올린 뒤 알림 길의 apphelp 캐시 묻기 (Q125, PE_emulator 요청 13 의 8번): `LdrpNotifyLoadOfGraph` → `LdrpSendPostSnapNotifications` → `LdrpCheckModule` → `CompatCachepLookupCdb` → **`NtApphelpCacheControl(6, 버퍼)`** (syscall `0x4c`) → 커널은 `\Device\ahcache` 에 IOCTL `0x22001B` 를 던지고 **ahcache.sys** 가 대답한다. **ntdll 이 부르기 전에 출력을 0 으로 비우므로 "성공 + 출력 안 씀" 기본 대답은 안전하다**

> **닫힘 (run130, sast/120 — Q127)**: 아래 "이 비트 (`LoadNotificationsSent`) 를 켜는 곳이 `+0xce` 하나뿐인지는 안 봤다" → **`LdrpSendPostSnapNotifications +0xce` 하나뿐, 끄는 줄 0** (ntdll · KernelBase · kernel32 전수). "모듈 다섯" 은 `[정적]`, syscall 5번은 `LdrpCheckModule` 안 조건이 따로라 `[추측]` 그대로 (받침 더 강함). "shim 엔진 올리는 쪽의 조건" → 문지기는 `LdrpInitializeProcess +0x19d7` · `+0x19dc` (`[pShimData+0x210] ≠ 0`) 둘뿐, `g_ShimsEnabled = 1` 은 `LdrpLoadShimEngine +0x148` 한 줄, 입구 셋 (`+0x19ef` · `LdrInitShimEngineDynamic` · `LdrpGetProcApphelpCheckModule +0x285`) 모두 test.exe 안 탐. 자세히는 `run130-run65-66-leftovers.md`.


## PE_emulator 쪽에서 쓸 것

### 한 줄 결론

- **`NtApphelpCacheControl` 은 `STATUS_SUCCESS` (0) 를 돌려주고 출력은 안 써도 된다.** 출력 DWORD 에 비트 `0x10` 을 켜는 값만 쓰지 말 것.
- 근거 (모두 `[정적]`, `ms/disassemble/ntdll/CompatCachepLookupCdb.asm`):
  - `+0x1e LEA RCX,[RSP+0x20]` · `+0x23 MOV R8D,0x188` · `+0x29` (ret `+0x2e`) `memset` — **부르기 전에 0x188 바이트 버퍼를 통째로 0 으로** 비운다. 함수의 첫 일이다.
  - `+0x33 MOV [RSP+0x1d0],EBX` (`EBX = 0`, `+0x2e XOR EBX,EBX`) — 버퍼 밖에 있는 **출력 DWORD 도 따로 0 으로** 쓴다. 이 함수 안에 이 칸을 쓰는 줄은 이것뿐이다.
  - 돌아온 뒤: `+0xb9 TEST EAX,EAX` · `+0xbb JS` (→ `+0xc6`) — 음수면 0 반환. 아니면 `+0xbd TEST [RSP+0x1d0],ESI` · `+0xc4 JNZ` (→ `+0xee`) — `(출력 & 마스크) != 0` 일 때만 1 반환.
  - 그러니 **스택 쓰레기를 읽는 길은 없다** (요청서 걱정 U1 닫힘). 상태값이 성공이든 실패든, 출력을 안 쓰면 반환은 0 이다.
- 진짜 커널·드라이버도 shim 대상이 아닌 모듈엔 **"성공 + 출력 0"** 을 준다 (`AhcApiLookupCdb` 의 못 찾음 갈래가 상태를 0 으로 바꿈 — 아래). 그래서 0 이 가장 진짜에 가깝다. `STATUS_NOT_FOUND` (`0xC0000225`) 를 줘도 ntdll 쪽 결과는 같다.
- 약속에 넣을 꼴: `NtApphelpCacheControl(ECX = 6, RDX = 버퍼)` — 인자 **2개**, 스택 인자 0. 원하면 `*(DWORD*)*(QWORD*)(버퍼+0x178) = 0` (안 써도 됨).

### syscall 표

| 항목 | 값 | 근거 |
|---|---|---|
| ntdll 스텁 | `NtApphelpCacheControl` (rva `0x161730`, 24B) | `+0x3 MOV EAX,0x4c` · `+0x12 SYSCALL` (`+0x8 TEST [0x7ffe0308],1` · `+0x10 JNZ` → `+0x15 INT 0x2e` 갈래) |
| 번호 | **`0x4c`** | 위 줄. sast/51 표와 같음 |
| 인자 | `ECX` = 서비스 번호 (클래스) `6`, `RDX` = 버퍼 `RSP+0x20` | `CompatCachepLookupCdb +0xaf MOV ECX,6` · `+0xa2 LEA RDX,[RSP+0x20]`, 커널 `NtApphelpCacheControl +0x26 MOV RSI,RDX` · `+0x35 CMP ECX,0xc` |
| 부르는 자리 | `CompatCachepLookupCdb +0xb4` (ret `+0xb9`) | |
| 한 번에 몇 번 | 모듈 하나에 한 번 (DDAG 노드마다 알림 한 번, 노드 안 모듈마다) | 아래 "몇 번" |
| test.exe 초기화에서 | **5번** `[추측]` (exe · dll1 · dll2 · KERNEL32 · KERNELBASE, ntdll 은 0번) — 사진의 모듈 항목 비트로 강하게 받침 | 아래 "사진과 맞춰 본 것" |

### 버퍼 꼴 (클래스 6, 시작 = `CompatCachepLookupCdb` 의 `RSP+0x20`, 크기 `0x188`)

| 칸 (버퍼 기준) | 크기 | 뜻 | 쓰는 줄 |
|---|---|---|---|
| `+0x000 ~ +0x0f7` | `0xf8` | 0 | `+0x29` `memset` |
| `+0x0f8` | 2 | `UNICODE_STRING.Length` = 파일 이름 글자 수 × 2 | `+0x7c` |
| `+0x0fa` | 2 | `MaximumLength` = `Length + 2` | `+0x87` |
| `+0x100` | 8 | `Buffer` = 파일 이름 시작 (마지막 `\` 다음 — 호출자 문자열 안을 가리킴, 복사 안 함) | `+0x5c` |
| `+0x178` | 8 | 출력 DWORD 주소 (= `RSP+0x1d0`, 버퍼 밖) | `+0xa7` |
| `+0x180` | 4 | 출력 크기 = 4 | `+0x97` |
| 나머지 | — | 0 | `memset` |

- 이름은 `+0x3d` (ret `+0x42`) `wcsrchr(이름, '\\')` 로 **파일 이름만** 떼어 보낸다. `\` 가 없으면 이름 그대로 (`+0x48 JZ` → `+0xe2`).
- 이름이 `0x7ffe` 글자보다 길면 (`+0x77 JA` → `+0xea`) **syscall 없이 0 반환**. 부르기 전 건너뛰는 갈래는 이것 하나다.

### 커널·드라이버가 주는 상태값 (클래스 6)

| 상태 | 값 | 언제 | ntdll 결과 |
|---|---|---|---|
| `STATUS_SUCCESS` | `0` | 찾음 (출력 = DB 플래그) / **못 찾음 (출력 0)** / 락 단계 내부 오류 (출력 0) | 출력 DWORD 로 갈림 |
| `STATUS_NOT_FOUND` | `0xC0000225` | ahcache `AhcEnabled == 0` / 커널이 `\Device\ahcache` 를 못 열 때 (커널 기본값) | 음수 → 0 |
| `STATUS_UNSUCCESSFUL` | `0xC0000001` | IRP 못 만듦 | 음수 → 0 |
| `STATUS_INVALID_PARAMETER` | `0xC000000D` | 클래스 ≥ `0xc`, 출력 포인터 0·크기 ≠ 4, 이름 빔 | 음수 → 0 |
| `STATUS_NO_MEMORY` | `0xC0000017` | 풀 할당 실패 | 음수 → 0 |
| `STATUS_INTEGER_OVERFLOW` | `0xC0000095` | `Length + 2` 가 16비트 넘음 | 음수 → 0 |

- 줄 근거는 결과서 `_workspace/sast/61_apphelp-cache_result.md` "실패 상태값 표". 클래스 0 ~ 11 핸들러 이름 표도 결과서에 있다 (PDB 열거 없음, 이름은 `AhcDispatch` 점프표 + `Ahc*` 함수 이름).

오프셋 읽는 법 (run60 ~ run65 와 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값.

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE1`, ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1`, **ahcache.sys pdb GUID `D54D2D17350903291E74D1E13B85E1831`** (이번에 처음 analyze — `SetPdb:` 줄 있음, `_index.tsv` 669줄, `_calls.tsv` 243줄)
- 값 대조: run44 사진의 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 를 다시 읽었다 (flow-writer). 새 동적 실행은 없다.

## 목적

- PE_emulator 가 kernel32 를 올리다 `NtApphelpCacheControl(0x6, …)` 에서 멈췄다 (약속 밖 syscall). 근거 asm 이 internals 에 없었다.
- 알고 싶은 것: 이 syscall 을 부르는 조건, 버퍼 꼴, 출력을 미리 비우나 (U1), 커널이 주는 상태값, 노드마다 몇 번, 그리고 PE_emulator 가 그대로 쓸 한 줄 결론.

## 방법

- 정적: `_workspace/sast/61_apphelp-cache.md` — ntdll `LdrpSendPostSnapNotifications`·`LdrpCheckModule`·`CompatCachepLookupCdb` depth 1 (→ 18개), 스텁 `NtApphelpCacheControl`, ntoskrnl `NtApphelpCacheControl` depth 0.
  - **범위를 넓힘**: 커널 함수가 `\Device\ahcache` 에 IOCTL 을 던지고 그 대답을 그대로 돌려줘서, `ms/bin` 에 있던 **ahcache.sys 를 새로 analyze** 했다 (376 KB). 클래스 6 길 `AhcDriverDispatchDeviceControl` → `AhcDispatch` → `AhcApiLookupCdb` depth 1 (→ 20개) + `AhcInitialize`·`DriverEntry`·`AhcCdbCreate`·`AhcpAVLTableCompare`·`AhcCdbReadRegistry`·`AhcpCdbEntryLoad` depth 0.
  - `_refs`: ntdll `g_pShimmedModuleList`·`g_pShimmedModuleListLength`·`g_ShimsEnabled`·`g_ShimsLoading`·`g_pfnApphelpCheckModuleProc`·`g_pfnSE_DllLoaded` (+ `0x1cd7cc`), ahcache `AhcEnabled`·`AhcCdbHandle`.
  - ntdll·ntoskrnl 은 다시 analyze 안 함. ahcache analyze 로그의 `WindowsResourceReference` 스크립트 경고는 결과에 영향 없어 보임 (결과서).
- 결과서: `_workspace/sast/61_apphelp-cache_result.md` (**분석성공**).
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `CompatCachepLookupCdb +0x1e` · `+0x23` · `+0x29/+0x2e` · `+0x33` · `+0xa2` · `+0xaf` · `+0xb4/+0xb9` · `+0xbb` · `+0xbd`, `LdrpSendPostSnapNotifications +0xad` · `+0xb0` · `+0xb2` · `+0xb6/+0xbb`.
- flow-writer 가 `.asm` 으로 한 번 더 본 줄:
  - `LdrpSendPostSnapNotifications` 전체 (321B — 아래 "결과서와 같음/다름").
  - `LdrpCheckModule +0x18` · `+0x1d` · `+0x20/+0x25` · `+0x28` · `+0x2a` · `+0x57` · `+0x5e` (→ `+0xef`) · `+0x7e/+0x83` · `+0x95` (→ `+0x101`) · `+0xab/+0xb0` · `+0xb0` · `+0xb7` · `+0x112/+0x117` · `+0x12f/+0x134` · `+0x14a/+0x14f`.
  - `CompatCachepLookupCdb` 전체 (244B).
  - `LdrpNotifyLoadOfGraph +0xa` · `+0x14` · `+0x19` · `+0x20/+0x25` · `+0x29` · `+0x47` · `+0x4e` · `+0x51` · `+0x53` · `+0x56` (→ `+0x83`) · `+0x61/+0x66` · `+0x73` (→ `+0x16`) · `+0x83 ~ +0x90`.
  - `LdrpPrepareModuleForExecution +0x31` · `+0x38` · `+0x3c` · `+0x40` · `+0x46` · `+0x4a` · `+0x76/+0x7b` · `+0x88/+0x8d` · `+0x111` · `+0x115` (→ `+0xbf`) · `+0x12a/+0x12f` · `+0x12f` (→ `+0x50`).
  - `LdrpDynamicShimModule +0x18` · `+0x2b ~ +0x44` · `+0x5c/+0x61` · `+0x61` · `+0x68` · `+0x6f ~ +0x7b` · `+0x96` · `+0x9d/+0xa2`.
  - `LdrpInitializeProcess +0x1ca7` · `+0x1cae` · `+0x1cdf/+0x1ce4` · `+0x1ce4` · `+0x1ce9` · `+0x1ceb` (`g_ShimsEnabled` 셋째 쓰기 — 아래).
  - 스텁 `NtApphelpCacheControl` 전체 (24B).
  - ntoskrnl `NtApphelpCacheControl +0x26` · `+0x35` · `+0x38` · `+0x42` · `+0x4d` · `+0x5c` · `+0x62` · `+0x65` · `+0x79` · `+0x7e` · `+0xab` · `+0xcf/+0xd4` · `+0xf2/+0xf7` · `+0x112` · `+0x114` · `+0x11f/+0x124` · `+0x139/+0x13e` · `+0x148` · `+0x165` · `+0x16f/+0x174` · `+0x190` · `+0x193` (→ `+0x79`).
  - ahcache `DriverEntry +0xd3` · `+0xda` · `+0x114/+0x11b` · `+0x192/+0x197`, `AhcInitialize +0x152` · `+0x1c8/+0x1cd` · `+0x1f7` · `+0x1fd/+0x202` · `+0x21f ~ +0x23c`, `AhcDriverDispatchDeviceControl +0x1f` · `+0x23` · `+0x38/+0x3d` · `+0x47/+0x4e`, `AhcDispatch +0x121` · `+0x176` · `+0x17a` · `+0x1ae` · `+0x1b5` · `+0x1d3/+0x1d8`, `AhcApiLookupCdb +0x3f` · `+0x48` · `+0x4c` · `+0x24d/+0x254` · `+0x262` · `+0x292` · `+0x296` · `+0x2a4/+0x2ab` · `+0x2b0` · `+0x2b4` · `+0x2dc`.
  - PDB 타입: ntoskrnl `_DRIVER_OBJECT` (`MajorFunction` = `+0x70`, `pdb_struct.py`).
- 로그: 없음 (정적만). 값 대조만 run44 덤프 읽기 로그.

## 결과

### 정적 — 호출 지점 (`_calls.tsv`, 길 순서)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpPrepareModuleForExecution` | `+0x76` | `+0x7b` | `ntdll!LdrpNotifyLoadOfGraph` | 노드 `State` 5 (`+0x12f` 그래프 정리 뒤) 또는 6. 7 이면 `+0x115` → `+0xbf` 로 건너뜀, 8·9 는 끝 | [정적] |
| `ntdll!LdrpPrepareModuleForExecution` | `+0x88` | `+0x8d` | `ntdll!LdrpDynamicShimModule` | 위 호출이 0 이상일 때 | [정적] |
| `ntdll!LdrpNotifyLoadOfGraph` | `+0x61` | `+0x66` | `ntdll!LdrpNotifyLoadOfGraph` (재귀) | 의존 노드 `State == 6` | [정적] |
| `ntdll!LdrpNotifyLoadOfGraph` | `+0x20` | `+0x25` | `ntdll!LdrpSendPostSnapNotifications` | 의존 목록이 비었거나 모두 7 이상 — 바로 앞 `+0x19` 에서 자기 `State = 7` | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0x4a` | `+0x4f` | `ntdll!RtlEnterCriticalSection(LdrpDllNotificationLock)` | 늘 (끝 `+0xf2/+0xf7` 에서 풂) | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xa3` | `+0xa8` | `ntdll!SbUpdateSwitchContextBasedOnDll` | 늘 (모듈마다) | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0x122` | `+0x127` | 간접 (CFG) = 푼 `g_pfnSE_DllLoaded` | `g_ShimsEnabled != 0` · 푼 값 `!= 0` · `g_ShimsLoading == 0` — test.exe 안 탐 | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xb6` | `+0xbb` | `ntdll!LdrpCheckModule` | 들어갈 때 `g_pShimmedModuleList == 0` | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xc1` | `+0xc6` | `ntdll!LdrpSendDllNotifications(모듈, 1)` | 늘 | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0xd8` | `+0xdd` | `ntdll!LdrpLogDllState` | 늘 (앞 `+0xce` 에서 항목 `Flags \|= 8`) | [정적] |
| `ntdll!LdrpSendPostSnapNotifications` | `+0x130` | `+0x135` | `ntdll!AVrfDllLoadNotification` | `PEB.NtGlobalFlag & 0x100` — test.exe 안 탐 | [정적] |
| `ntdll!LdrpCheckModule` | `+0x20` | `+0x25` | `ntdll!CompatCachepLookupCdb(전체 경로, 0x10)` | 늘 (앞 조건 없음) | [정적] |
| `ntdll!LdrpCheckModule` | `+0x7e` | `+0x83` | `ntdll!RtlAllocateHeap` | 위 반환이 1 일 때만 | [정적] |
| `ntdll!CompatCachepLookupCdb` | `+0x29` | `+0x2e` | `ntdll!memset(버퍼, 0, 0x188)` | 늘 — 첫 일 | [정적] |
| `ntdll!CompatCachepLookupCdb` | `+0x3d` | `+0x42` | `ntdll!wcsrchr(이름, '\\')` | 늘 | [정적] |
| `ntdll!CompatCachepLookupCdb` | `+0x6c` | `+0x71` | `ntdll!wcslen` | 이름 포인터 ≠ 0 | [정적] |
| `ntdll!CompatCachepLookupCdb` | `+0xb4` | `+0xb9` | `ntdll!NtApphelpCacheControl` (스텁 → syscall `0x4c`) | 이름 ≤ `0x7ffe` 글자 | [정적] |
| `nt!NtApphelpCacheControl` | `+0x139` | `+0x13e` | `nt!IoGetDeviceObjectPointer(\Device\ahcache, 0xC0000000)` | 전역 `g_AhcDeviceObject` 가 0 일 때만 (`+0x7e`) | [정적] |
| `nt!NtApphelpCacheControl` | `+0xcf` | `+0xd4` | `nt!IoBuildDeviceIoControlRequest(0x22001B, 장치, RDX, 0x188, NULL, 0, FALSE, 이벤트, IoStatus)` | 장치를 얻었을 때 | [정적] |
| `nt!NtApphelpCacheControl` | `+0xf2` | `+0xf7` | `nt!IofCallDriver` | IRP 를 만들었을 때. 반환값 = syscall 반환 | [정적] |
| (IRP 배달) | — | — | `ahcache!AhcDriverDispatchDeviceControl` | 드라이버 객체 `MajorFunction[14]` 칸 (`DriverEntry +0xda`) — `IofCallDriver` 안 간접 호출이라 `?` | [정적: 칸] · [추측: 배달] |
| `ahcache!AhcDriverDispatchDeviceControl` | `+0x38` | `+0x3d` | `ahcache!AhcDispatch` | 클래스 `(코드 >> 2) & 0xff` < `0xc` | [정적] |
| `ahcache!AhcDispatch` | `+0x1d3` | `+0x1d8` | `ahcache!AhcApiLookupCdb` | 클래스 6 이고 `AhcEnabled != 0` (`+0x1ae`·`+0x1b5`, 아니면 `+0x121` → `0xC0000225`) | [정적] |

### 호출 관계

```
LdrpPrepareModuleForExecution                                   (노드 하나, State 5·6)
 ├─ (1) +0x12f  LdrpCondenseGraph                                State 5 일 때만 → 6
 ├─ (2) +0x7b   LdrpNotifyLoadOfGraph (노드)
 │               ├─ (1) +0x66  LdrpNotifyLoadOfGraph (의존 노드, State 6 일 때만 — 재귀)
 │               └─ (2) +0x25  LdrpSendPostSnapNotifications (노드)        ← +0x19 State = 7 바로 뒤
 │                              │   락 LdrpDllNotificationLock 안, 노드의 모듈마다:
 │                              ├─ +0xa8   SbUpdateSwitchContextBasedOnDll
 │                              ├─ +0x127  간접 g_pfnSE_DllLoaded            (shim 엔진 있을 때만 — test.exe 안 탐)
 │                              ├─ +0xbb   LdrpCheckModule (FullDllName.Buffer)    g_pShimmedModuleList == 0 일 때
 │                              │           └─ +0x25  CompatCachepLookupCdb (이름, 0x10)
 │                              │                      ├─ +0x2e  memset (버퍼 0x188 → 0) · +0x33 출력 DWORD = 0
 │                              │                      ├─ +0x42  wcsrchr · +0x71 wcslen
 │                              │                      └─ +0xb9  NtApphelpCacheControl (6, 버퍼)  ── SYSCALL 0x4c ──┐
 │                              ├─ +0xc6   LdrpSendDllNotifications (모듈, 1)                                         │
 │                              ├─ +0xdd   LdrpLogDllState              (앞 +0xce Flags |= 8 LoadNotificationsSent) │
 │                              └─ +0x135  AVrfDllLoadNotification       (NtGlobalFlag & 0x100 — test.exe 안 탐)  │
 └─ (3) +0x8d   LdrpDynamicShimModule (노드)   g_pShimmedModuleList == 0 이면 바로 끝                                 │
                                                                                                                       │
nt!NtApphelpCacheControl  ◄────────────────────────────────────────────────────────────────────────────────────────────┘
 ├─ (조건) +0x13e  IoGetDeviceObjectPointer (\Device\ahcache)    g_AhcDeviceObject 가 0 일 때만 → +0x148 저장
 ├─ +0xd4   IoBuildDeviceIoControlRequest (IOCTL 0x22001B, 입력 = 유저 버퍼 그대로, 길이 0x188)
 └─ +0xf7   IofCallDriver ── ? ──► ahcache!AhcDriverDispatchDeviceControl
                                    └─ +0x3d  AhcDispatch (클래스 6)
                                               └─ +0x1d8  AhcApiLookupCdb (이름, 출력 포인터, 4)   AhcEnabled != 0 일 때
```

- 라벨은 ret 자리. 번호 (1)~(3) 은 코드 흐름 순서.
- `LdrpSendPostSnapNotifications` 안 모듈마다 순서: `SbUpdate…` → (shim 갈래) → `LdrpCheckModule` → `LdrpSendDllNotifications` → `Flags |= 8` → `LdrpLogDllState`. 앱 검증기 비트가 켜졌으면 `SbUpdate…` 앞에 `AVrfDllLoadNotification` (`+0x97 JNZ` → `+0x12d`, 끝나면 `+0x13a JNS` → `+0x9d`).

### 질문별 요약 (근거 줄은 결과서 해당 절)

| # | 질문 | 답 | 닫힘 |
|---|---|---|---|
| 1 | `LdrpSendPostSnapNotifications` 의 `LdrpCheckModule` 조건 | 인자 = DDAG **노드** (`+0x2a MOV R14,RCX`, 모듈은 노드 `Modules` 목록 − `0xa0`). 조건은 **들어갈 때 `g_pShimmedModuleList == 0`** 하나 (`+0x23` · `+0x46 CMOVNZ R15D` · `+0xad` · `+0xb0 JNZ` → `+0xbb`). 노드 칸·PEB 비트·`ShimData`·`g_pfnApphelpCheckModuleProc` 는 안 본다 | 정적으로 닫힘 |
| 2 | `LdrpCheckModule` | 인자 = 전체 경로. 앞 조건 없이 `CompatCachepLookupCdb(경로, 0x10)`. 반환 0 이면 아무것도 안 함. 1 이면 경로를 NUL 로 이은 목록 `g_pShimmedModuleList` (힙) 에 붙이고 길이 `g_pShimmedModuleListLength` 를 씀 (`+0xb0`·`+0xb7`) → 바로 뒤 `LdrpDynamicShimModule` 이 apphelp 를 올리는 shim 길 입구 | 정적으로 닫힘 |
| 3 | `CompatCachepLookupCdb` | 위 "PE_emulator 쪽에서 쓸 것". **출력을 syscall 전에 0 으로 비운다.** 반환은 0 / 1 뿐 (상태값을 안 넘김). 호출자 다섯의 마스크: `LdrpCheckModule` `0x10` · `LdrpSnapModule` ×2 `0x80` (못 찾음 오류 처리 안, run36) · `LdrpLogDeprecatedDllEtwEvent` `0x4` · `LdrpCheckComponentOnDemandEtwEvent` `0x100` | 정적으로 닫힘 |
| 4 | 커널 `NtApphelpCacheControl` 클래스 6 | 커널은 스스로 대답 안 함 — `\Device\ahcache` 에 IOCTL `0x22001B` (`클래스 × 4 \| 0x220003`, METHOD_NEITHER) 를 던지고 드라이버 상태를 그대로 반환. 입력 길이는 늘 `0x188` (사용자 크기 검사 없음). ahcache: 사용자 모드면 정렬·범위 검사 후 `0x188` 바이트를 스택 사본으로 복사, `AhcEnabled` 확인, `AhcApiLookupCdb` 가 AVL 표 (`AhcCdbHandle`) 에서 대소문자 무시로 이름을 찾음. **못 찾으면 출력 0 + `STATUS_SUCCESS`** (`+0x262` 내부 `0xC0000225` → `+0x292 ~ +0x296` 출력 0 → `+0x2b0` 쓰기 → `+0x2b4 JMP` → `+0x2dc XOR EDI,EDI`) | 갈래·상태값은 닫힘 / **DB 내용은 동적** |
| 5 | 노드마다 몇 번 | 노드마다 알림 한 번 (`State` 6 → 7 은 `+0x19` 한 번뿐), 노드 안 모듈마다 syscall 한 번. 부르는 쪽 둘: `LdrpInitializeProcess +0x1af2` (exe 그래프 한 번) · `LdrpLoadDllInternal +0x284` (`LdrLoadDll` 마다). ntdll 노드는 `State 9` 라 건너뜀 | 규칙은 닫힘 / 횟수는 동적 (사진이 받침 — 아래) |
| 6 | 한 줄 결론 | 기본 대답 안전. 위 | 정적으로 닫힘 |

## 확인된 것

- `[정적]` 요청서 걱정 U1 (스택 쓰레기 출력) 은 **일어나지 않는다**: `CompatCachepLookupCdb +0x29/+0x2e memset(0x188)` + `+0x33` 출력 DWORD 0.
- `[정적]` shim 길로 가는 열쇠는 **출력 DWORD 의 비트 `0x10`** 하나다 (`LdrpCheckModule +0x18 MOV EDX,0x10` → `CompatCachepLookupCdb +0xbd`). 켜지면 `g_pShimmedModuleList` 가 생기고 `LdrpPrepareModuleForExecution +0x8d LdrpDynamicShimModule` 이 `+0x36 JNZ` → `+0x9d/+0xa2 LdrpGetProcApphelpCheckModule` 로 apphelp 를 올린다 (본문 범위 밖).
- `[정적]` 클래스 6 대답은 ntoskrnl 이 아니라 **ahcache.sys** 가 정한다. ntoskrnl 안 `Ahc*` 는 `AhcCacheQueryHwId` 하나뿐이고 클래스 9 전용이다 (`+0x16f/+0x174`).
- `[정적]` `AhcEnabled` 는 `AhcInitialize +0x23c` 가 `EDI` 로 쓴다. `EDI` 는 `+0x152 MOV EDI,1` 뒤 이 함수 안에서 다시 안 바뀐다 (`grep`, flow-writer) → 1. 앞 단계 (`AhcLockCreate`·`AhcCacheCreate`·`AhcCdbCreate`·`AhcSdbQueryCreate`·`CitmInitialize`) 가 성공해야 닿는다. `DriverEntry +0x192/+0x197` 이 `AhcInitialize` 를 부른다 (부팅 때 `[추측]` — 드라이버가 언제 올라오는지는 안 봄).
- `[정적]` 드라이버 객체 `MajorFunction[14]` (`_DRIVER_OBJECT +0x70 + 14×8 = +0xe0`) 에 `AhcDriverDispatchDeviceControl` (rva `0x392d0`) 을 넣는 줄: `DriverEntry +0xd3 LEA RAX` · `+0xda MOV [RDI+0xe0],RAX` (flow-writer). 14 가 `IRP_MJ_DEVICE_CONTROL` 이라는 이름은 `[추측]` (PDB 열거를 안 봄).
- `[정적]` `LdrpDynamicShimModule` 은 목록이 0 인 정상 길에서도 상태 DWORD (ntdll rva `0x1cd7cc`) 에 **읽은 값을 그대로 다시 쓴다** (`+0x18` 읽기 → `+0x7b MOV [0x1cd7cc],ESI`). 0 은 목록이 있을 때 `+0x96` 에서만.
- `[정적]` `g_ShimsEnabled` 의 셋째 쓰기 `LdrpInitializeProcess +0x1ceb` 는 **0 을 쓴다** (`+0x1ce4 XOR R15D,R15D`), 조건: `g_ShimsEnabled != 0` (`+0x1ca7` · `+0x1cae JZ`) 이고 `g_pfnSE_InstallAfterInit` 간접 호출 (`+0x1cdf/+0x1ce4`) 이 `AL == 0` 을 돌려줄 때 (`+0x1ce7` · `+0x1ce9 JNZ`). test.exe 는 `g_ShimsEnabled == 0` 이라 안 탐.

## 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

- **`LdrpSendPostSnapNotifications` 의 모듈 몸통을 탄 모듈 = exe · KERNEL32 · KERNELBASE · dll1 · dll2, ntdll 은 아님.** 이 함수는 모듈마다 `+0xce OR [RBX+0x68],8` (`Flags.LoadNotificationsSent`, PDB 비트 3) 을 켠다. P1b 사진:
  - test.exe `0y1` (`ms/logs/04_baggage-read_P1b.txt:3392`) · ntdll `0y0` (`:3497`) · KERNEL32 `0y1` (`:3602`) · KERNELBASE `0y1` (`:3707`) · dll1 `0y1` (`:3812`) · dll2 `0y1` (`:3917`).
  - 정적 추정 "노드 5개, ntdll 0번" 과 맞는다. 단 이 비트를 켜는 곳이 `+0xce` 하나뿐인지는 안 봤다 (구조체 비트 `_refs` 없음) → syscall 5번은 여전히 `[추측]` (강한 받침).
- **`Flags.ShimEngineCalloutSent` (비트 11, `+0x127 OR [RBX+0x69],8`) 는 여섯 모듈 모두 `0y0`** (`_P1b.txt:3400` · `3505` · `3610` · `3715` · `3820` · `3925`) — shim 간접 호출 `+0x122` 를 안 탔다는 정적 결론과 맞다.
- **`g_pShimmedModuleList` (`ntdll+0x1d23d8`) 8바이트 = 0** — P1a (`_P1a.txt:5047`, `` 00007ffa`fb8323d0 `` 줄 둘째 qword) · P1b (`_P1b.txt:6165`) · P2 (`_P2.txt:6177`). 그러니 이 구간의 모든 `LdrpSendPostSnapNotifications` 에서 `R15 = 0` → `LdrpCheckModule` 을 불렀다 `[정적 + 관찰 값]`.
  - 짐 지도 7.3 이 이 이름을 `≤P1a` 로 둔 건 짐작 크기 16B 안 `+0x8` (`0x1d23e0`, run-once, run62) 값 `0x25996f31ed2` (`:5048`) 때문이다 — 7.5 의 `[추측]` 이 맞았다.
- `g_ShimsEnabled` (`0x1d23cc`)·`g_ShimsLoading` (`0x1d23cf`) = 0 — 같은 줄 앞 qword 묶음 `0x1d23c8` = `` 00010000`00000000 `` (P1a) / `…00000002` (P1b) / `…00000003` (P2) 에서 두 바이트 모두 0 (`_P1a.txt:5046` · `_P1b.txt:6164` · `_P2.txt:6176`). 바뀌는 바이트는 `0x1d23c8` (다른 칸) 이다.
- `0x1cd7cc` (`LdrpDynamicShimModule` 상태 DWORD) = **1** — P1a·P1b·P2 모두 (`_P1a.txt:3830` · `_P1b.txt:4948` · `_P2.txt:4960`, `0x1cd7c8` qword `` 00000001`00019000 `` 의 위 4바이트). 파일 처음 값 1 · `+0x7b` 가 같은 값을 다시 씀과 맞다.
- **`PEB.NtGlobalFlag` = 0** — P1a (`_P1a.txt:1770`) · P1b (`_P1b.txt:2468`) · P2 (`_P2.txt:2480`). 그러니 `AVrfDllLoadNotification` 갈래 (`& 0x100`) 는 안 탔다. 결과서 동적 몫 "`dd @$peb+0xbc`" 는 사진으로 이미 답했다.
- 노드 `State` (짐 지도 3.6 사진): exe P1b 7 → P2 9, dll1·dll2 P1b 7 → P2 9, ntdll·k32·kb 9 — `LdrpNotifyLoadOfGraph +0x19` 가 7 을 쓴다는 정적과 맞다.

## 결과서와 같음 / 다르게 적은 곳 · 보탠 것

- **(오케스트레이터 질문) `LdrpSendPostSnapNotifications +0xa8 TEST RSI,RSI` · `+0xab JNZ` (→ `+0x113`) 는 `LdrpCheckModule` 을 건너뛰지 않는다 — 결과서 "조건은 `g_pShimmedModuleList == 0` 하나뿐" 이 맞다.**
  - RSI 의 출처: `+0x3d MOV ESI,R12D` (`R12D = 0`, `+0x20 XOR R12D,R12D`) 로 0 에서 시작. `+0x4f CMP [g_ShimsEnabled],SIL` · `+0x56 JZ` (→ `+0x7a`) 를 지나면 (켜졌을 때만) `+0x64 MOV RSI,[g_pfnSE_DllLoaded]` → `+0x58 [0x7ffe0330]` (KUSD 쿠키) 로 `+0x74 ROR` · `+0x77 XOR` = **포인터 풀기**. 즉 RSI = 풀린 shim 엔진 `SE_DllLoaded` 주소 (shim 엔진이 없으면 0).
  - `+0xab` 의 목적지 `+0x113` 에서 두 갈래 모두 `+0xad` 로 **돌아온다**: `g_ShimsLoading != 0` 이면 `+0x11a JNZ 0x18000c2ed` (= `+0xad`), 아니면 `+0x122` 간접 호출 → `+0x127` 비트 → `+0x12b JMP 0x18000c2ed` (= `+0xad`). `+0xad TEST R15,R15` 가 `LdrpCheckModule` 조건이다.
  - 그러니 RSI 갈래는 "shim 콜아웃을 `LdrpCheckModule` **앞에** 끼워 넣는" 갈래일 뿐, 건너뛰기가 아니다. 결과서 표의 `+0x122` 줄 ("아니면 `+0xad`") 과도 맞다.
- (보탬) `LdrpDynamicShimModule` 정상 길 끝 `+0x7b` 가 상태 DWORD 에 같은 값 (1) 을 다시 쓴다. 결과서는 "`+0x74 MOV EAX,EBX` 로 반환" 까지만 적었다 (뜻은 같음 — 값이 안 바뀜).
- (보탬) `g_ShimsEnabled` 셋째 쓰기 `LdrpInitializeProcess +0x1ceb` 값 = 0 과 조건 (결과서는 "레지스터 값이라 값은 확인 안 함").
- (보탬) `AhcEnabled` 에 쓰는 `EDI` = 1 의 출처 줄 `AhcInitialize +0x152` (결과서는 "1 로 쓴다" 만).
- (보탬) `IofCallDriver` → `AhcDriverDispatchDeviceControl` 연결의 칸: `DriverEntry +0xda` (`MajorFunction[14]`).
- (보탬) 사진 대조 절 전체 (결과서는 사진을 안 봄). 결과서 동적 몫 중 `g_pShimmedModuleList` · `NtGlobalFlag` 두 줄은 사진으로 답했다 (P1a·P1b·P2 세 순간 기준).

## 정정

- 없음. (짐 지도 7.5 의 `g_pShimmedModuleList` `[추측]` 은 맞아서 `(해결됨)` 으로만 남김.)

## 아직 확인 필요

동적 몫은 모두 **Q104 VM 단계**에 얹는다. 멈춤점은 test.exe 일 때만 걸리게 조건을 붙인다 (커널 쪽 `ahcache!` 멈춤점은 모든 프로세스에 걸린다 — 실행 안내 때 조건 필수).

1. **ahcache 가 올라와 있고 켜졌나**: `lm m ahcache` · `dd ahcache!AhcEnabled L1` (1 이어야 `AhcApiLookupCdb` 까지 감) · `dq ahcache!AhcCdbHandle L1`. 덤: `dq nt!g_AhcDeviceObject L1` (0 이 아니면 커널이 이미 장치를 잡아 둠).
2. `ntdll!CompatCachepLookupCdb` 입구 — `du @rcx` (전체 경로) · `r edx` (마스크 `0x10`) · **히트 수 = 모듈 수** (정적 + 사진 추정 5).
3. `ntdll!CompatCachepLookupCdb+0xb9` — `r eax` (돌아온 상태, 추정 0) · `dd @rsp+1d0 L1` (출력 DWORD, 추정 0). 반환 1 인 모듈이 있는지.
4. `ahcache!AhcApiLookupCdb+0x254` — `r rax` (0 = 못 찾음), 찾았으면 `dd poi(@rax+0x28) L1` (플래그). 이어서 `+0x2b0` 에서 `r esi` (출력으로 쓰는 값).
5. (사진으로 답함, 선택) `dq ntdll!g_pShimmedModuleList L1` 을 은퇴 직전에 — 사진 P2 는 0. 그 뒤 (P2 ~ `NtContinue`) 만 남음.
6. (사진으로 답함, 선택) `dd @$peb+0xbc L1` — 사진 P1a·P1b·P2 모두 0.
7. 노드 `State`: `ntdll!LdrpNotifyLoadOfGraph+0x19` 히트마다 `dt ntdll!_LDR_DATA_TABLE_ENTRY poi(@rbx)-0xa0 BaseDllName` (노드 `Modules` 첫 칸 − `0xa0`) 로 이름 세기 — kernel32 길 2 (KERNELBASE → KERNEL32) · exe 길 3 (dll2 → dll1 → exe) 추정.

- 다음 바퀴 후보 (정적):
  - `Flags.LoadNotificationsSent` (비트 3) 를 켜는 다른 줄이 있나 — ntdll 전체에서 `OR dword ptr [reg+0x68],0x8` 꼴 검색 (있으면 5번 추정의 받침이 약해짐).
  - shim 엔진을 올리는 쪽 `LdrpInitShimEngine` (`LdrpInitializeProcess +0x19ef`) → `LdrpLoadShimEngine` → `LdrpGetShimEngineInterface` 의 조건 (`g_ShimsEnabled` 상수 1 쓰기 `LdrpLoadShimEngine +0x148`) — test.exe 에서 안 탄다는 것은 사진 (`g_pfnSE_*` 끝까지 0) 으로만 앎.
  - `SbUpdateSwitchContextBasedOnDll` 안 (`SbGetCurrentSwitchContext` 가 0 이 아닐 때 하는 일) — 사진의 `SwitchBackContext` (짐 지도 7.2 `P1a~P1b`, test·k32·kb·dll1·dll2) 를 이 함수가 쓰는지.

## 도구 메모

- 새 함정 없음.
- 새 모듈 analyze: ahcache.sys (커널 드라이버). 앞으로 `ahcache!` 이름은 `ms/disassemble/ahcache/_index.tsv` 로 확인한다.
- 같은 qword 줄에 여러 전역이 들어 있는 사진 (`dq` 줄) 은 바이트 순서 (little-endian) 로 칸을 나눠 읽었다: 줄 둘째 qword `` 00010000`00000000 `` = 바이트 `0x1d23c8 … 0x1d23cf` 가 `00 00 00 00 00 00 01 00`.

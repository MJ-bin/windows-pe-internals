# 63 dll-init-rest — 정적 분석 요청

- 날짜: 2026-11-06
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run68
- 대기열: **Q129** (PE_emulator 요청 16 의 나머지 — 2·3·6 정적·8·9. 사용자 결정 2026-11-06: Q104 VM 단계 앞)
- 관련: run67 (sast/62 — `KernelBaseDllInitializeMemoryManager`·`RtlUnicodeStringToAnsiString`·`LdrSetDllManifestProber`, kernel32 `BaseDllInitializeMemoryManager2`), run61 (`RtlRandom` = `RtlRandomEx` 본문), run62 (ETW 등록 꼴), run34 (csrss 쪽 `CsrClientConnectToServer` 상대편 — `basesrv`/`csrsrv`), run57 (NLS), Q84 (P1a ~ P1b 쪼개기 — 동적, 이번 몫 아님)
- 원문 (읽기만): `../PE_emulator/_workspace/00_planner/103_kernelbase-globals.md` (A/B/C 표), PE_emulator 103·105

## 무엇을 위해

- PE_emulator 는 kernelbase·kernel32 DllMain 을 진짜로 돌리지 않고 모방한다 (직접 쓰기 + 진짜 ntdll 함수 덫). 아래 함수들의 본문 asm 이 internals 에 없어 공사 중으로 두었다. 함수마다 **인자 · 쓰는 칸 (전역·구조체 칸, 이름은 PDB) · 값 출처 · 하위 호출 · syscall (이름·클래스·인자) · 반환** 이 필요하다.
- **test.exe 기본 길에서 타는 갈래만 자세히**, 나머지는 조건·이름만. 함수가 많다 (약 18) — 묶음 A 를 먼저.

## 알고 싶은 것

### 묶음 A — kernelbase DllMain (`_KernelBaseBaseDllInitialize`, kernelbase rva `0x4e6c0`) 안, 시간 순서

부르는 자리 (ret, 오케스트레이터가 `.asm` 에서 봄): `+0x165 TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation` · `+0x189 DisableRegCachingForConstrainedImpersonation` · (`+0x1b2` MemoryManager — run67) · `+0x1d6 RtlCreateTagHeap` · `+0x241 NtQuerySystemInformation` · `+0x25a`·`+0x278 RtlRandomEx` · `+0x2d9 CsrClientConnectToServer` · (`+0x497` — run67) · `+0x4b2 RtlSetUnhandledExceptionFilter` · `+0x4c0 RtlEncodePointer` · `+0x4e6 BaseNlsDllInitialize` · `+0x508 RtlInitializeCriticalSection` · `+0x526 ConsoleInitialize`

1. **`TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation`** (kernelbase rva `0xf70ac`, 182B) — 어느 공급자를 등록하나 (GUID·전역 이름), syscall (`EtwEventRegister` → run62 꼴과 같나 — 같으면 가리키기만)
2. **`DisableRegCachingForConstrainedImpersonation`** (rva `0x4e64c`, 418B) — 무엇을 읽어 무엇을 쓰나, syscall (레지스트리?), test.exe 에서 타는 갈래
3. **`+0x241 NtQuerySystemInformation`** 의 클래스·버퍼 — 이것이 kernelbase `SysInfo` 를 채우는 자리인가 (질문 9 와 이어짐)
4. **`CsrClientConnectToServer`** (ntdll rva `0xdb7a0`, 489B, 부르는 곳 `+0x2d4/+0x2d9` 쯤) — 인자 (서버 이름·서버 DLL 번호·연결 정보 버퍼·크기), 하위 호출·syscall (`NtSecureConnectPort`/`NtAlpcConnectPort` 등 — 처음 연결이면 `CsrpConnectToServer` 류 줄기 끝까지), 쓰는 전역 (`CsrPortHandle`·`CsrServerApiRoutine`·`CsrProcessId` 등)·PEB 칸 (`+0x88`·`+0x98`·`+0x380` — 원문 5번), 돌아온 연결 정보를 kernelbase 가 어디에 쓰나 (`BaseStaticServerData`?). **값은 csrss 가 주므로 동적 (Q84)** — 이번엔 꼴과 쓰는 자리만
5. **`RtlSetUnhandledExceptionFilter`** (ntdll rva `0xe95f0`, 21B) — 쓰는 전역 (인코딩해서 넣나), 넘긴 값 (`+0x4ab` 쯤 인자)
6. **`BaseNlsDllInitialize`** (kernelbase rva `0xea9b0`, 122B) — 쓰는 전역, 하위 호출, syscall (NLS — run57 과 이어지면 가리키기만)
7. **`RtlInitializeCriticalSection`** (ntdll rva `0xd65c0`, 335B) · **`RtlInitializeCriticalSectionAndSpinCount`** (`0xd4900`, 378B) — `+0x508` 이 초기화하는 락 (전역 이름), `_RTL_CRITICAL_SECTION` 칸에 쓰는 값 (특히 `DebugInfo` — 디버그 정보 블록을 어디서 받나: 정적 풀 `RtlpStaticDebugInfo`? 힙?, `SpinCount`), 전역 목록 (`RtlCriticalSectionList`?) 에 끼우나, syscall
8. **`ConsoleInitialize`** (kernelbase rva `0xbeb60`, 585B) — test.exe (콘솔, Subsystem 3, 부모 cmd) 에서 타는 갈래: 무엇을 읽어 (`ProcessParameters.ConsoleHandle`·`ConsoleFlags`·`StandardInput/Output/Error`) 무엇을 쓰나 (전역), syscall (`NtDeviceIoControlFile` 등 condrv 연결?). 값이 부모·condrv 에서 오면 "동적 필요"
- 덤: `RtlCreateTagHeap` (`+0x1d6`, run67 — 반환 0 이유만 다시 쓸 것 없음), `RtlRandomEx` (run61 본문 — 가리키기만, 두 호출이 쓰는 전역 이름만), `RtlEncodePointer` (run59 — 가리키기만)

### 묶음 B — kernelbase `entry` 와 kernel32 DllMain

9. **`_SYSTEM_BASIC_INFORMATION` (클래스 0) 칸 모양**: ntoskrnl `ExpGetSystemBasicInformation` (rva `0x457484`, 264B) 본문 — 출력 칸 오프셋·크기·뜻·값 출처 (`MmNumberOfPhysicalPages`·`KeMaximumIncrement`·페이지 크기·할당 단위·최소/최대 유저 주소·`KeActiveProcessors`·프로세서 수 등 — 칸 이름은 PDB 타입이 없으면 공개 이름 `[추측]` 과 함께), 크기 검사·실패 값. 정적으로 정해지는 값 (상수) 과 부팅 때 정해지는 값 (동적) 을 가름. kernelbase 에서 `SysInfo` (전역 — `--where` 로 못 찾음, `.asm` 주석·`_refs` 로 찾을 것) 를 채우는 곳과 읽는 함수들 (이름·읽는 칸). PE_emulator nt 대답은 `+0x18`·`+0x28`·`+0x38` 만 씀 — 나머지 칸 중 kernelbase·kernel32 가 실제로 읽는 칸 표
10. kernel32 **`_BaseDllInitialize`** (rva는 `.asm` 머리) 안 — `+0x6c/+0x73 RtlImageNtHeader` (ntdll rva `0xddaf0`, 43B) · `+0x92/+0x99 RtlSetThreadPoolStartFunc` (PDB rva `0x10bc80` — `_index.tsv` 이름 `CsrIdentifyAlertableThread`, 같은 주소 이름 21개 = ICF 로 합쳐진 빈 함수일 수 있음 — 본문이 정말 "아무것도 안 함" 인지) · `+0xe9/+0xf0 RtlSetProtectedPolicy` (ntdll `0xf9d80`, 740B) · `+0x210/+0x217 LdrDisableThreadCalloutsForDll` (ntdll `0xed1f0`, 81B — kernel32 노드 칸 비트?) · `+0x364/+0x369 InitializeRegTermsrvFpns` (kernel32 `0x2cbc0`, 415B) · `+0x380/+0x387 GetRegistryExtensionFlags` (kernelbase `0x13a540`, 366B) — 함수마다 쓰는 칸·syscall·test.exe 에서 타나
11. kernel32 **`_Kernel32LegacyLib_BaseDllInitialize`** (`.asm` 있음) — `+0x19/+0x20`·`+0xae/+0xb5 KernelBaseGetGlobalData` (kernelbase `0xe25e0`, 8B — `&KernelBaseGlobalData` 를 돌려주나) · `+0x9c/+0xa3 NtQuerySystemInformation` 클래스·버퍼 · `+0xed/+0xf2 BaseDllReadWriteIniFile` 조건 — 쓰는 kernel32 전역
12. kernelbase **`_DllMainCRTStartupForGS2`** (rva `0x1290e8`, 24B) — 무엇을 하나 (보안 쿠키 초기화 → run59 쿠키 사슬과 이어지나), 누가 부르나

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernelbase | `^TraceLoggingRegisterEx_EtwEventRegister_EtwEventSetInformation$`, `^DisableRegCachingForConstrainedImpersonation$`, `^BaseNlsDllInitialize$`, `^ConsoleInitialize$`, `^KernelBaseGetGlobalData$`, `^GetRegistryExtensionFlags$`, `^_DllMainCRTStartupForGS2$` | 1 | 1·2·6·8·10·11·12 |
| ntdll | `^CsrClientConnectToServer$` (+ 연결 줄기 하위는 지도로 찾아 그것만) | 1 | 4 |
| ntdll | `^RtlSetUnhandledExceptionFilter$`, `^RtlInitializeCriticalSection$`, `^RtlInitializeCriticalSectionAndSpinCount$` | 1 | 5·7 |
| ntdll | `^RtlImageNtHeader$`, `^CsrIdentifyAlertableThread$` (= `RtlSetThreadPoolStartFunc`), `^RtlSetProtectedPolicy$`, `^LdrDisableThreadCalloutsForDll$` | 1 | 10 |
| kernel32 | `^InitializeRegTermsrvFpns$` | 1 | 10 |
| ntoskrnl | `^ExpGetSystemBasicInformation$` | 1 | 9 |
| kernelbase | (refs) `SysInfo` 쪽 전역, `+0x241` 버퍼 | — | 3·9 |
| kernelbase / kernel32 | `_KernelBaseBaseDllInitialize.asm`·`entry.asm`·`_BaseDllInitialize.asm`·`_Kernel32LegacyLib_BaseDllInitialize.asm` (있음 — 다시 뽑지 않음) | — | 부르는 자리 |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where <모듈 파일 이름> <이름>`. **kernelbase 는 `KernelBase.dll` (대소문자 그대로 — 소문자면 "표 없음", run67 도구 메모)**. kernelbase DllMain `KernelBaseDllInitialize` 는 Ghidra 이름 `entry` (rva `0x5060`).
- 공용 부품 (`RtlAllocateHeap`·`RtlFreeHeap`·락·`memset`·`memcpy`·`RtlInitUnicodeString`·`StringCbPrintfW`) 은 이름만.

## 이미 아는 것

- `[정적]` run67: `KernelBaseDllInitializeMemoryManager`, `RtlUnicodeStringToAnsiString`, `LdrSetDllManifestProber`, kernel32 `BaseDllInitializeMemoryManager2` 본문, `RtlCreateTagHeap` 반환 0 (NtGlobalFlag 에 `0x800` 없음)
- `[정적]` run61: `RtlRandom` (= `RtlRandomEx`) 본문·`RtlpRandomExInit`
- `[정적]` run59: `RtlEncodePointer`·쿠키 사슬
- `[관찰]` run44 사진: P1a ~ P1b 사이에 `Csr*` 8개·PEB CSR 칸 4개·`ConsoleHandle` `4 → 0x60` 이 바뀜 (Q84). kernelbase·kernel32 `.data` 는 사진에 없음 (run67)
- PE_emulator nt 대답 `FillSystemBasicInformation` 은 `+0x18`·`+0x28`·`+0x38` 세 칸만 쓰고 나머지 0

## 이번에 안 하는 것

- Q84 (csrss 연결 결과 값 — 동적), Q104 (`SysInfo` 사진)
- csrss 쪽 (`basesrv`·`csrsrv`) 본문 — run34 가리키기만
- `ConsoleInitialize` 아래 condrv 커널 쪽 본문

## 받고 싶은 것

- 질문별 답 (예/아니오·이름) + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋. 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- **syscall 표**: 이름 · 클래스 · 부르는 자리 (함수+call/ret) · 조건 · test.exe 기본 길에서 횟수
- PE_emulator 가 그대로 쓸 **작은 표**: 함수 × (인자 · 쓰는 칸 · 값 출처 · 하위 호출 · syscall · 반환)
- `_SYSTEM_BASIC_INFORMATION` 칸 표 (오프셋·크기·뜻·값 출처·정적 값 또는 "동적")
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 동적 몫은 Q104 VM 단계 (또는 csrss 몫이면 Q84) 에 얹을 멈춤점·읽을 칸으로
- 짐 지도 반영거리 (kernelbase·kernel32·ntdll 전역 줄)

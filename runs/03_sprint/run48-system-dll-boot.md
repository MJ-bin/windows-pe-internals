# 2026-10-29 run48 — 부팅 준비 갈래 (Q93): ntdll 섹션·기본 주소·`PsNtdllExports` 는 **부팅 때 한 번** 만들어진다. `IoInitSystemPreDrivers +0x10f5` → `PsLocateSystemDlls` → (칸마다) `PspLocateSystemDll` → `PspMapSystemDll` (인자4 = 1) 이 `PspSystemDlls` 칸을 채우고, 뒤에 `PspInitPhase2 → PspInitializeSystemDlls +0x6b` 가 `PsNtdllExports` 를 채운다 (정적)

> **정정 (run81, sast/78)**: 아래 "① 에서 읽는 자리" 표 `PsNtdllExports` 줄의 "`PspInitializeThunkContext +0x1a1 · +0x37f` (`+0x8` → 새 스레드 `Rip`)" 중 **`+0x37f` 는 새 스레드 `Rip` 가 아니다.** 실패 갈래 (`+0x2fc JS +0x37b`) 에서 같은 `+0x8` (`LdrInitializeThunk`) 을 예외 레코드 `ExceptionAddress` (`+0x386 [RBP+0x40]`) 로 읽는 줄이다 (그 뒤 `+0x3c3 KiDispatchException`, 첫 기회 0 → 프로세스 종료). 새 스레드 `Rip` 는 `+0x1a1` 하나 (`runs/03_sprint/run81-exception-kernel.md` "정정").

## 부팅 준비 호출 사슬 (함수 / call / ret / 하는 일 / 실패하면)

- 결과서 맨 앞 표 바탕. 오프셋은 함수 시작에서. `ret` 이 WinDbg 콜스택 `caller+0x..` 와 같은 값. 모두 `_calls.tsv`·`.asm` 기준 `[정적]`.
- 이 사슬은 **프로세스 생성 전 (부팅)** 이다. 흐름도 범위 (프로세스 생성 → 진입점) 앞이라 그림에는 안 넣는다.
- 번호 = 결과서 번호. "사이트 N" = `KeBugCheckEx(0x6b, 상태, N, …)` 의 둘째 인자 (아래 "버그체크 표").

| 함수 (caller → callee) | call | ret | 하는 일 | 실패하면 |
|---|---|---|---|---|
| 앞. `InitBootProcessor` → `PsInitSystem(0)` → `PspInitPhase0` | `+0xbad` | `+0xbb2` | (이름만) 프로세스 쪽 0단계. 이 구간보다 앞 | — |
| 앞. `Phase1InitializationDiscard` → `PsInitSystem(1)` → `PspInitPhase1` | `+0xc24` | `+0xc29` | (이름만) 인자 1 은 `EBX` (`+0xa8f` 에서 1) `[추측]` | — |
| 1. `Phase1Initialization` → `IoInitSystem` | `+0x36` | `+0x3b` | 부팅 `Phase1` 스레드가 I/O 초기화로 (`Discard` 는 `+0x1e` 에서 먼저) | 실패값이면 `+0x55 KeBugCheckEx(0x69, IopInitFailCode, …)` |
| 2. `IoInitSystem` → `IoInitSystemPreDrivers` | `+0x12` | `+0x17` | 드라이버 올리기 전 준비 (4821바이트) | — |
| 3. `IoInitSystemPreDrivers` → **`PsLocateSystemDlls`** | **`+0x10f5`** | **`+0x10fa`** | 시스템 DLL 찾기 시작. 부르기 직전 `NtGlobalFlag \|= 0x40000` (`+0x10e3 ~ +0x10ef`), 돌아와서 되돌림 (`+0x10fa`) | 늘 0 을 돌려줘서 `IopInitFailCode=7` 갈래 (`+0x1114`) 는 못 탄다 |
| 4. `PsLocateSystemDlls` → `ExVerifySuite(6)` | `+0x14` | `+0x19` | 6번 제품군 + `PsEmbeddedNTMask` 비트0 이면 전체 건너뜀 (`+0x1d ~ +0x25` → `+0x145`) | — |
| 5. `PsLocateSystemDlls` → `VslGetNestedPageProtectionFlags(0)` | `+0x2d` | `+0x32` | 비트 `4` 만 남김 (`+0x3b`) | — |
| 6. `PsLocateSystemDlls` → **`PspLocateSystemDll`** | **`+0x9e`** | **`+0xa3`** | 표 6칸 고리. 칸이 NULL 이 아니고 플래그 `0x20` 이 꺼져 있으면 한 번. `EDX` = (칸 플래그 `0x10` ? 5의 비트4 : 0) | 음수 + 칸 플래그 비트0 (필수) → `+0xcf/+0xd4 KeBugCheckEx(0x6b, 상태, 2, 칸번호, 0)`. 필수 아닌 칸은 그냥 다음 칸 |
| 6'. `PsLocateSystemDlls` → `PspMapSystemDll` (별칭 칸) | `+0x84` | **`+0x89`** | 플래그 `0x20` 켜진 칸만: 0번 칸 `+0x38` 복사 (`+0x6c/+0x70`) 뒤 현재 프로세스에 올림 (인자3·4 = 0·1). **이 빌드 표엔 그런 칸이 없다** | 음수면 그 칸을 NULL (`+0x8d`), 부팅은 계속 |
| 7a. `PspLocateSystemDll` → `ZwSystemDebugControl` | `+0x79` | `+0x7e` | 명령 `0x26` (커널 디버거에서 파일 끌어오기), 입력 = 경로 16바이트. `NtGlobalFlag & 0x40000` 일 때만 → 3번의 임시 비트 때문에 **늘 켜짐** | 반환값 안 봄 |
| 7b. → `ZwOpenFile` | `+0xc0` | `+0xc5` | 칸 `+0x10` 경로로 DLL 파일 열기 | 음수면 그대로 돌려줌 (6 에서 판단) |
| 7c. → `MmCreateSpecialImageSection` | `+0xef` | `+0xf4` | 이미지 섹션 만들기 — exe 와 **같은 함수·같은 안쪽 사슬**, 인자만 다름 | 음수 → `+0x10b/+0x110` 사이트 3 |
| 7d. → `ObCloseHandle` (파일 핸들) | `+0x117` | `+0x11c` | 파일 핸들 닫기 | — |
| 7e. → `ObReferenceObjectByHandle` | `+0x141` | `+0x146` | 섹션 핸들 → 섹션 객체 (`MmSectionObjectType`, 접근 `0xf001f`, 커널 모드) | 음수 → `+0x15d/+0x162` 사이트 4 |
| 7f. → `ObCloseHandle` (섹션 핸들) | `+0x169` | `+0x16e` | 섹션 핸들 닫기 (객체 참조는 남김) | — |
| 7g. → `MmGetSectionInformation(섹션, 2, …)` | `+0x17e` | `+0x183` | 재배치량 → **칸 `+0x38`** (`+0x1aa`) | 음수 → `+0x19a/+0x19f` 사이트 8 |
| 7h. → `ObInitializeFastReference` | `+0x1ae` | `+0x1b3` | 섹션 객체를 칸 `+0x0` 이 가리키는 빠른 참조 자리에 넣음 (참조 +15), 락 칸 `+0x8` = 0 (`+0x1b6`) | — |
| 7i. → `MmGetSectionInformation(섹션, 4, …)` | `+0x1cd` | `+0x1d2` | (칸 플래그 `0x21` 중 하나일 때만) 이미지 정보 + 부가 비트 | 7j·7k 와 같이 사이트 9 |
| 7j. → `MiSectionControlArea` | `+0x1e5` | `+0x1ea` | 섹션 → 컨트롤 영역 | — |
| 7k. → `MiDoesImageContainFunctionOverrideFixups` | `+0x1f5` | `+0x1fa` | 컨트롤 영역의 로드 구성에 "함수 덮어쓰기 고침" 이 있나 (확인만) | 없음/비트 모자람 → `+0x211/+0x216` 사이트 9 |
| 7l. → **`PspMapSystemDll`** (부팅 갈래) | **`+0x233`** | **`+0x238`** | 현재 프로세스에 올리고 칸 `+0x20`·`+0xc`·`+0x28` 채움 (인자3·4 = 0·**1**) | 음수 → `+0x24f/+0x254` 사이트 5 |
| 8a. `PspMapSystemDll` → `PspReferenceSystemDll` | `+0x3b` | `+0x40` | 칸 `+0x0` 빠른 참조로 섹션을 하나 잡음 | NULL → `0xc0000001` |
| 8b. → `MmMapViewOfSectionEx` | `+0x118` | `+0x11d` | 섹션을 프로세스에 올림 | 상태 그대로 |
| 8c. → `ObFastDereferenceObject` | `+0x12d` | `+0x132` | 잡은 참조 돌려줌 (태그 `PsSd`) | — |
| 8d. → `RtlImageNtHeader` | `+0x1ae` | `+0x1b3` | (부팅 갈래만) 올린 뷰의 NT 헤더 → 칸 쓰기 | — |
| 9. `Phase1Initialization` → `Phase1InitializationIoReady` | `+0x61` | `+0x66` | I/O 끝난 뒤 나머지 (3 이후) | — |
| 10. `Phase1InitializationIoReady` → `PsInitSystem(2)` → `PspInitPhase2` | `+0x1d5` / `PsInitSystem +0x3b` | `+0x1da` / `+0x40` | 프로세스 쪽 2단계 | 거짓이면 IoReady 실패 갈래 (`+0x1dc`) |
| 10a. `PspInitPhase2` → **`PspInitializeSystemDlls`** | `+0x172` | `+0x177` | 칸마다 이름 표를 보고 ntdll 함수 주소를 찾아 **`PsNtdllExports`** 등에 씀 | 사이트 6 |
| 10b. `PspInitializeSystemDlls` → `PsQuerySystemDllInfo` | `+0x2d` | `+0x32` | 칸 `+0x20 ≠ 0` 인 칸만 (아니면 건너뜀) | — |
| 10c. → `PspLookupEntryPoint` (→ `RtlFindExportedRoutineByName` `+0x1b/+0x20`) | `+0x56` | `+0x5b` | 이름 → 주소 | 음수 → `+0xa0/+0xa5 KeBugCheckEx(0x6b, 상태, 6, 0, 0)` |
| 10d. `PspInitializeSystemDlls` 쓰는 줄 | `+0x6b` | — | `MOV [RCX],RAX` — `PsNtdllExports` 칸에 씀 | — |
| 10e. → `PspWow64GetSharedInformation` (칸 1 일 때) | `+0x77` | `+0x7c` | `PsWowX86SharedInformation +0x30` ← WOW 칸 `+0x20` (`+0x80`) | — |
| 11. `Phase1InitializationIoReady` → `MmInitSystemDll` | `+0x1e2` | `+0x1e7` | `MiState.Processes.SystemDllBase` ← 0번 칸 `+0x20` (쓰는 줄 `MmInitSystemDll +0x22`) | — |
| 12. `Phase1InitializationIoReady` → `PsInitSystem(3)` → `PspInitPhase3` | `+0x20a` / `PsInitSystem +0x34` | `+0x20f` / `+0x39` | **`VslVsmEnabled` 일 때만**: `PsSecureSystemProcess` 에 0번·4번 칸 올림 (`+0x6b/+0x70`, `+0xa0/+0xa5`, 인자3·4 = 0·0) 뒤 `VslpEnterIumSecureMode` (`+0xea`) | — |

비유 (아파트 관리실):

- 부팅 때 관리실이 **공용 열쇠 꾸러미** (ntdll 섹션) 를 딱 한 번 만든다. 꾸러미 번호표 (`PspSystemDlls` 칸) 에 "어느 칸에 걸었나" (기본 주소 `+0x20`) 를 적어 둔다.
- 그 꾸러미로 **자주 쓰는 문 위치 목록** (`PsNtdllExports` — `LdrInitializeThunk` 등 19개) 도 미리 적어 둔다.
- 새 입주자 (새 프로세스) 가 오면 관리실은 꾸러미를 **같은 칸에 걸어 주기만** 하고, 칸이 다르면 거절한다 (`PspMapSystemDll +0x172`).

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 03_sprint 여섯째 바퀴 (짐 지도 바퀴, plan **Q93** — 사용자 요청 2026-10-29, Q78c 앞에 끼움).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (flow.md 기준과 같음).
- 버전 대조 (결과서): `NtCreateUserProcess` ret `+0xc36` → `PspAllocateProcess`, `PspInsertThread` ret `+0x807` → `PspCallProcessNotifyRoutines` 가 `_calls.tsv` 와 같다.
- 이 문서를 쓰며 다시 맞춘 것: 요약 표 call/ret 전부를 `ms/disassemble/ntoskrnl/_calls.tsv` 로, 쓰는 줄 넷 (`IoInitSystemPreDrivers +0x10e3 ~ +0x10fa` `NtGlobalFlag`, `PspMapSystemDll +0x207054 ~ +0x20705e` (rva `0xb18c2c ~ 0xb18c36`), `PspInitializeSystemDlls +0x6b`, `MmInitSystemDll +0x22` `[0x140e2db80]`) 을 `.asm` 으로 한 번 더 봤다.

## 목적

- 짐 지도의 **"입력" 짐 출처**를 닫는다. 프로세스 생성 (①) 때 커널이 읽기만 하는 것 — `PspSystemDlls` 표, ntdll 섹션, 기본 주소 (`+0x20`), `PsNtdllExports` — 은 누가 언제 만들었나.
- 요청서 질문 넷: (1) 부르는 자리와 순서, (2) `PspLocateSystemDll` 줄마다, (3) `PspMapSystemDll` 부팅 갈래와 `+0x20`·`+0x28` 값 출처·`PsNtdllExports` 채우는 줄, (4) 쓰는 상태와 프로세스 생성 때 읽는 자리 짝.
- 사용자 짐작 `[추측]` "ntdll 섹션·기본 주소·`PsNtdllExports` 는 부팅 때 준비되고 프로세스마다는 붙이기만" 이 맞나.
- 범위 밖: 부팅 전체 순서 (이름만), `PspInitPhase3` VBS 갈래 본문, WOW64 ntdll 매핑 내용 (도는지만).

## 방법

- 정적: `_workspace/sast/42_system-dll-boot.md` — ntoskrnl. 이미 있던 `PspLocateSystemDll`·`PspMapSystemDll`·`PspInitializeSystemDlls` 는 읽기만. 새로 depth 0: 요청서 대상 3개 (`PsLocateSystemDlls`·`PspReferenceSystemDll`·`IoInitSystemPreDrivers`) + 길 따라 더한 것 (부팅 앞뒤 순서 10개, `PspLocateSystemDll` 줄 뜻 4개, `+0x7e` 정체 3개, 다른 쓰는 곳 확인 3개). 목록은 결과서 머리.
  - 새 지도 `ms/disassemble/ntoskrnl/_map/PsLocateSystemDlls_*` (깊이 3, 43개). 새 `_refs` 10개 (`PspSystemDlls`·`PsNtdllExports`·`PspNativeSystemDll` 등).
  - 칸 처음 값 (`PspSystemDlls` 표·칸·경로 문자열·이름 표 `NtdllExportInformation`) 은 결과서가 PE 파일 바이트를 직접 읽었다 (스크래치 임시 파이썬). PDB 공개 이름으로 주소를 이름에 맞춤.
  - `_PS_SYSTEM_DLL_INFO` 는 PDB 에 이름만 있고 칸 정의가 없다 (`pdb_struct.py` "없음") → 칸은 오프셋으로만 쓴다.
- 결과서: `_workspace/sast/42_system-dll-boot_result.md` (**분석성공**). 동적 없음.
- 값 대조 (이 문서에서 더함): run44 K1 덤프 읽기 로그 `ms/logs/04_baggage-read_K1.txt` 1531 ~ 1542줄 (`dq nt!PsNtdllExports L0x15`), 51 ~ 54줄 (`lm m ntdll`). 새 동적 실행은 없다.
- 문서: 이 run 문서, `baggage.md` (쓰는 말 단계 `Ⓑ`, 새 절 7.6.2, 1·2·3.8·3.15·4·5·6·7.11·8장), `flow.md` 2.1 `PsMapSystemDlls` 절 메모·2.3 끝 `[공백]` 해결·4장·5장·기준. 그림은 안 바꿈.

## 결과

### 호출 관계 (부팅, 코드 순서)

```
Phase1Initialization                                          부팅 Phase1 스레드 (System 프로세스 소속으로 보임 [추측], D3)
 ├─ (1) +0x23   Phase1InitializationDiscard
 │        └─ +0xc29  PsInitSystem(1) → PspInitPhase1            (이름만)
 ├─ (2) +0x3b   IoInitSystem
 │        └─ +0x17  IoInitSystemPreDrivers
 │                │  +0x10e3 ~ +0x10ef  NtGlobalFlag |= 0x40000 (임시)
 │                ├─ +0x10fa  PsLocateSystemDlls
 │                │        ├─ +0x19  ExVerifySuite(6)                   EmbeddedNT 면 전부 건너뜀
 │                │        ├─ +0x32  VslGetNestedPageProtectionFlags(0)
 │                │        │   ── 표 6칸 고리 ──
 │                │        ├─ +0x89  PspMapSystemDll(…, 0, 1)            별칭 칸 (플래그 0x20) 만 — 이 빌드엔 없음
 │                │        ├─ +0xa3  PspLocateSystemDll                  [0] ntdll · [1] SysWOW64 ntdll · [4] vertdll
 │                │        │        ├─ +0x7e   ZwSystemDebugControl(0x26)       NtGlobalFlag 0x40000 → 늘 부름
 │                │        │        ├─ +0xc5   ZwOpenFile                       칸 +0x10 경로
 │                │        │        ├─ +0xf4   MmCreateSpecialImageSection      exe 와 같은 사슬, 인자 다름
 │                │        │        ├─ +0x11c  ObCloseHandle (파일)
 │                │        │        ├─ +0x146  ObReferenceObjectByHandle        섹션 객체 (돌려주지 않음)
 │                │        │        ├─ +0x16e  ObCloseHandle (섹션)
 │                │        │        ├─ +0x183  MmGetSectionInformation(2)       → 칸 +0x38 (+0x1aa)
 │                │        │        ├─ +0x1b3  ObInitializeFastReference        → *칸[+0x0] (+0x1b6 락 0)
 │                │        │        ├─ +0x1d2  MmGetSectionInformation(4)       ┐
 │                │        │        ├─ +0x1ea  MiSectionControlArea             ├ 필수 칸만, 확인만 (안 씀)
 │                │        │        ├─ +0x1fa  MiDoesImageContainFunctionOverrideFixups ┘
 │                │        │        └─ +0x238  PspMapSystemDll(현재 프로세스, 칸, 0, 1)
 │                │        │                 ├─ +0x40   PspReferenceSystemDll
 │                │        │                 ├─ +0x11d  MmMapViewOfSectionEx
 │                │        │                 ├─ +0x132  ObFastDereferenceObject
 │                │        │                 └─ +0x1b3  RtlImageNtHeader
 │                │        │                    +0x207054 · +0x20705b · +0x20705e   칸 +0x20 · +0xc · +0x28
 │                │        └─ +0xd4  KeBugCheckEx(0x6b, 상태, 2, 칸번호, 0)    필수 칸 실패
 │                │  +0x10fa  NtGlobalFlag 되돌림
 │                └─ (+0x113e IopReassignSystemRoot — 이 뒤)
 └─ (3) +0x66   Phase1InitializationIoReady
          ├─ +0x1da  PsInitSystem(2) → +0x40 PspInitPhase2
          │                 └─ +0x177  PspInitializeSystemDlls
          │                          ├─ +0x32  PsQuerySystemDllInfo       칸 +0x20 = 0 이면 건너뜀
          │                          ├─ +0x5b  PspLookupEntryPoint
          │                          │        └─ +0x20  RtlFindExportedRoutineByName
          │                          │  +0x6b  PsNtdllExports 칸에 씀
          │                          └─ +0x7c  PspWow64GetSharedInformation   (WOW 칸) → +0x80
          ├─ +0x1e7  MmInitSystemDll          +0x22  MiState.Processes.SystemDllBase
          └─ +0x20f  PsInitSystem(3) → +0x39 PspInitPhase3   VslVsmEnabled 일 때만 (보안 System 프로세스)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 줄은 그 함수 안 쓰는 줄.
- `PspMapSystemDll +0x207054` 등은 함수 본문에서 **멀리 떨어진 조각** (rva `0xb18c28 ~ 0xb18c3a`). WinDbg 가 `PspMapSystemDll+0x207…` 로 보일지 다른 이름으로 보일지는 확인 전 `[추측]` (rva 는 확실).

### `PspSystemDlls` 표와 칸 (질문 1·4)

- 표 = 포인터 6칸 (rva `0xfd8130`). 파일에 박힌 처음 값 `[정적]` (PE 바이트):

| 표 칸 | 가리키는 것 | 경로 (`+0x10`) | 짧은 이름 (`+0x30`) | 플래그 (`+0x8`) | 머신 (`+0xa`) |
|---|---|---|---|---|---|
| `[0]` | `PspNativeSystemDllData` (rva `0xfd8980`) | `\SystemRoot\System32\ntdll.dll` | `ntdll.dll` | `0x0013` | `0x8664` |
| `[1]` | `PspWowX86SystemDllData` (`0xfd8940`) | `\SystemRoot\SysWOW64\ntdll.dll` | `ntdll32.dll` | `0x0008` | `0x014c` |
| `[4]` | `PspVsmEnclaveRuntimeDllData` (`0xfd89c0`) | `\SystemRoot\System32\vertdll.dll` | `vertdll.dll` | `0x0012` | `0x8664` |
| `[2]` `[3]` `[5]` | 0 | — | — | — | — |

- 칸 하나 = 0x40바이트 (PDB 정의 없음 → 오프셋):
  - `+0x0` 섹션 빠른 참조 자리 포인터 (`PspNativeSystemDll` 등 16바이트 자리를 가리킴, 처음 0) · `+0x8` 플래그 · `+0xa` 머신 · `+0xc` `SizeOfImage` · `+0x10` 경로 `UNICODE_STRING` (버퍼 `+0x18`) · `+0x20` 기본 주소 · `+0x28` 뷰 시작 · `+0x30` 짧은 이름 · `+0x38` 재배치량.
  - 파일 값으로 오는 칸: `+0x0`·`+0x8`·`+0xa`·`+0x10`·`+0x30`. 부팅 때 nt 가 쓰는 칸: `+0xc`·`+0x20`·`+0x28`·`+0x38` (+ `+0x0` 이 가리키는 자리).
- 그래서 이 빌드에서 `PspLocateSystemDll` 을 **세 번** (ntdll · WOW ntdll · vertdll) 부르는 코드 길이 있다 `[정적]` (표 처음 값 기준). 부팅 때는 프로세스 종류 조건이 없다 — 칸이 있으면 다 찾는다. 프로세스 종류 조건은 `PsMapSystemDlls` 쪽에 있다 (아래 짝 표).
- 표에 쓰는 줄은 `PsLocateSystemDlls +0x8d` (별칭 칸 실패 → NULL) 하나뿐 (`_refs/PspSystemDlls.tsv` 쓰기 1줄) → `[2][3][5]` 는 이 빌드에서 안 바뀐다 `[정적]`. 레지스터+오프셋 쓰기는 `_refs` 가 못 잡으니 값은 D1.
- 칸 플래그 비트 (이 구간 코드가 읽는 것, 동작으로만 `[추측]` 이름 없이): `0x1` 필수 (실패하면 버그체크, 7i~7k 확인 대상) · `0x8` 올릴 주소 상한 풀기 (WOW) · `0x10` VSL 비트4 를 섹션 만들기에 넘김 · `0x20` 별칭 칸 · `0x40` 보안 프로세스에게도 올릴 칸 (`PsMapSystemDlls`) · `0x2` 이 구간은 안 읽음.
- `ExVerifySuite(6)` + `PsEmbeddedNTMask` 비트0 이면 고리 전체를 건너뛴다 (EmbeddedNT 제품 `[추측]`). 일반 Win11 Edu 는 아닐 것.

### 섹션 만들기 — exe 와 같은 길, 다른 인자 (질문 2)

- `PspLocateSystemDll +0xef` 의 `MmCreateSpecialImageSection` 은 exe (`NtCreateUserProcess +0x937`, run31·run47) 와 **같은 함수 → 같은 안쪽 사슬** (`MiCreateSection → MiCreateImageOrDataSection → MiCreateNewSection → MiCreateImageFileMap …`) `[정적]`.

| 인자 | exe (`NtCreateUserProcess +0x937`) | 시스템 DLL (`PspLocateSystemDll +0xef`) |
|---|---|---|
| 섹션 객체 이름 | (run31) | 없음 (`+0xd8` 에서 `ObjectName` NULL) |
| 토큰 (3번째) | 프로세스 쪽 토큰 | NULL (`R8D=0`) |
| 서명 수준 (4번째) | 부모 쪽에서 구한 값 | `0x0c` (`+0xe4 MOV R9B,0xc`) |
| 플래그 비트0 | 늘 켜짐 → 할당 속성 `SEC_IMAGE 0x01000000` | 꺼짐 → 할당 속성 `0x02400000` (`MmCreateSpecialImageSection +0x43 ~ +0x56`) |
| 플래그 비트2 (`4`) | — | 칸 `0x10` + VSL 비트4 일 때 → `MiCreateSection` 플래그 `8` |
| 플래그 비트1 (`2`) | — | 켜짐 (`+0xd5 OR ESI,2`). `MmCreateSpecialImageSection` 은 안 읽는 비트 |

- 할당 속성 `0x02400000` 은 `MiInitializeCreateSectionPacket +0x346 ~ +0x35a` 에서 **결국 `SEC_IMAGE` + 패킷 플래그 `0x10` + 서명 수준 칸 (패킷 `+0x18`) `0x0c`** 로 바뀐다 `[정적]`. 이름 (`SEC_PROTECTED_IMAGE`·`SEC_NO_CHANGE`·`SE_SIGNING_LEVEL_WINDOWS`) 은 SDK 기억 `[추측]`. 서명 검사 본체는 안 따라감 `[공백]`.
- 상태 `0xc0000054` 면 `KeDelayExecutionThread` 로 쉬고 다시 (`+0xdd ~ +0xea`).
- 섹션 핸들과 파일 핸들은 **둘 다 닫는다**. 섹션 객체 참조만 남고 (`ObReferenceObjectByHandle` 참조를 돌려주는 줄이 없음), 그 참조가 칸 `+0x0` 의 빠른 참조 자리로 들어간다 (`+0x1b3`, 참조 +15 덤). → 섹션 객체는 영구히 산다 `[정적]`.
- 7i ~ 7k (필수 칸 = 0번 ntdll) 는 **확인만** 한다. 결과를 전역·표 칸에 안 쓴다. 뜻: "필수 시스템 DLL 은 함수 덮어쓰기 (DVRT 쪽) 정보가 있는 이미지여야 한다" `[추측]`.

### `PspMapSystemDll` 의 네 caller (질문 3)

- 시그니처는 `.asm` 으로 셌다: `(프로세스, 칸, 인자3, 인자4)` = (`RCX`, `RDX`, `R8D`, `R9D`). (`PsMapSystemDlls.c` 는 인자 하나만 그린다 — `.c` 함정)

| 부르는 자리 (call / ret) | 프로세스 | 인자3 | 인자4 |
|---|---|---|---|
| `PspLocateSystemDll +0x233 / +0x238` (부팅) | 현재 스레드의 프로세스 (`GS:[0x188]` → `+0xb8`) | 0 | **1** |
| `PsLocateSystemDlls +0x84 / +0x89` (부팅, 별칭 칸만) | 〃 | 0 | **1** |
| `PsMapSystemDlls +0xbb / +0xc0` (프로세스마다) | 새 프로세스 | `PsMapSystemDlls` 둘째 인자 (`MiMapProcessExecutable +0xd7` 에서 0) | **0** |
| `PspInitPhase3 +0x6b` · `+0xa0` (부팅, `VslVsmEnabled` 일 때만) | `PsSecureSystemProcess` | 0 | **0** |

- **인자4 가 하는 일** `[정적]`:
  1. 인자4 ≠ 0 이면 올릴 때 할당 종류에 비트 `0x40000` 을 더한다 (`+0xbb BTS ECX,0x12`). MM 안 뜻은 안 따라감 `[공백]`.
  2. 올린 뒤 (`+0x169`) **인자4 ≠ 0 → 칸을 채움** (`+0x1a5` 로, 돌려줄 값 0), **인자4 = 0 → 칸 `+0x20` 이 올라간 주소와 같은지 비교** (`+0x16e ~ +0x176`), 다르면 `0xc0000001` (`+0x178`).
  3. `STATUS_IMAGE_NOT_AT_BASE` (`0x40000003`) 는 `R14 == PsInitialSystemProcess` 일 때만 성공 취급 (`+0x132 ~ +0x19c`), 다른 프로세스면 `0xc0000018`. 인자4 와는 상관없다.
- 올리는 호출 `+0x118 MmMapViewOfSectionEx`: 섹션 통째, 페이지 보호 `2`, 확장 인자 2개 (주소 요구 · 종류 5 값 `0x20`) — **`MiMapProcessExecutable` 과 같은 값**. 별칭 칸이면 셋째 `ImageMachine`.

### 기본 주소 `+0x20` 은 어디서 오나 (질문 3)

- 쓰는 줄 (부팅 갈래, 떨어진 조각): `+0x1a5` 올라간 주소 → `+0x1b3 RtlImageNtHeader` → `Magic` 이 PE32 (`0x10b`, WOW ntdll) 면 `[RAX+0x34]`, 아니면 `[RAX+0x30]` (`ImageBase`) → **`+0x207054 MOV [RBX+0x20],RDX`** → `+0x20705b MOV [RBX+0xc],EAX` (`SizeOfImage`) → `+0x20705e MOV [RBX+0x28],R14` (뷰 시작) `[정적]` (이 문서에서 `.asm` 121 ~ 125줄 다시 봄).
- 즉 `+0x20` = **System 프로세스에 올린 뷰의 헤더 `ImageBase` 값**이다. 파일 속 `0x180000000` 을 그대로 읽는 줄이 아니다.
- 새 기본 주소를 고르는 건 그보다 앞, **섹션 만들 때 MM** (`MiCreateNewSection +0x3da → MiRelocateImage → MiSelectImageBase`, `ExGenRandom`) 이다. 부팅 때 한 번 고르고, 모든 프로세스가 같은 섹션을 올리니 같은 주소를 받는다 `[정적]`.
  - 둘을 잇는 근거: 프로세스마다 `+0x172` 가 "`+0x20` == 올라간 주소" 를 요구 `[정적]` + run44 `[관찰]` 첫 트랩 프레임 `Rdx` = ntdll 실제 베이스 `7ffafb660000` (파일 값 아님), `Rdx` 는 `PspSystemDlls[0]+0x20` 에서 옴 (run16 `[정적]`).
  - 빈 곳: 뷰 헤더 `ImageBase` 칸이 새 주소로 **바뀌는 MM 줄**은 안 따라갔다 `[공백]` → D5.
- `+0x28` 과 `+0x20` 이 다를 수 있는 이유가 코드에 있다: `PspLookupEntryPoint` 가 `+0x28` 뷰에서 이름을 찾고 `(+0x20) − (+0x28)` 을 더해 (`+0x2f ~ +0x3a`) 사용자 쪽 주소로 바꾼다. 같다면 더하는 값은 0. 실제로 같은지는 D1.
- System 프로세스에 정말 올리나: **올리는 코드가 있다** (프로세스 인자 = 현재 스레드의 프로세스, 이 갈래에 `MmUnmapViewOfSection` 없음). `Phase1Initialization` 스레드가 System 프로세스 소속이라는 건 코드가 가정하는 것 (`+0x195` 비교) `[추측]` → D3.

### `PsNtdllExports` 를 채우는 줄 (질문 3)

- **`PspInitializeSystemDlls +0x6b` (`MOV [RCX],RAX`)** 하나 (`_refs/PsNtdllExports.tsv` 쓰기 1줄) `[정적]`. `PspInitPhase2 +0x172` (ret `+0x177`) 안 — 부팅 `PsLocateSystemDlls` 가 끝난 **뒤** (IoReady `+0x1d5`).
- 이름 표 `NtdllExportInformation` (rva `0xc7ff90`, 칸마다 `{표, 개수}`): `[0]` ntdll **19개**, `[1]` WOW **8개**, `[2~5]` 0개 (vertdll 도 0 → 이 구간에서 vertdll 함수 주소는 안 찾음). run16 의 "`NtdllExportInformation` 0번 = {`NtdllExports`, 19개}" 와 같다.
- `[0]` → `PsNtdllExports` (`_PS_NTDLL_EXPORTS` 0xa8, PDB 21칸) 중 19칸. **이름 표에 없는 칸 둘 `+0x38 KiUserCallbackDispatcherReturn`·`+0x98 KiUserEmulationDispatcher` 는 이 구간이 안 채운다** `[정적]`.
  - `[관찰]` run44 K1 로그 (`ms/logs/04_baggage-read_K1.txt` 1531 ~ 1542줄 `dq nt!PsNtdllExports L0x15`): 21칸 중 **`+0x38`·`+0x98` 두 칸만 0**, 나머지 19칸은 `00007ffa…` 주소. 정적 계산과 칸마다 맞는다 (이 문서에서 대조).
- `[1]` → `PsWowX86SharedInformation` (rva `0xfda900`) 8칸, 그리고 `+0x30` 은 `+0x77 PspWow64GetSharedInformation` 뒤 `+0x80` 에서 WOW 칸 `+0x20` 을 복사.
- 칸 `+0x20 = 0` 인 칸 (못 찾은 칸) 은 `PsQuerySystemDllInfo` 가 NULL 을 줘서 건너뛴다 (`+0x38 JZ`). 그래서 **`PsLocateSystemDlls` 가 먼저 끝나야** 의미가 있고, 코드 순서도 그렇다 (`IoInitSystem` → `Phase1InitializationIoReady`).

### 버그체크 표 (모두 `KeBugCheckEx(0x6b, …)`, 넷째 인자 0 — 실패하면 부팅이 멈춘다)

| 어디 (call / ret) | 사이트 | 1번째 인자 | 조건 |
|---|---|---|---|
| `PsLocateSystemDlls +0xcf / +0xd4` | 2 (3번째 = 칸 번호) | `PspLocateSystemDll` 음수 (실질 `ZwOpenFile` 실패) | 칸 플래그 비트0 (필수) 칸만 — 이 빌드엔 0번 ntdll 만 |
| `PspLocateSystemDll +0x10b / +0x110` | 3 | `MmCreateSpecialImageSection` 음수 | 모든 칸 (연 뒤에는 필수 아니어도 멈춤) |
| `+0x15d / +0x162` | 4 | `ObReferenceObjectByHandle` 음수 | 〃 |
| `+0x19a / +0x19f` | 8 | `MmGetSectionInformation` (클래스 2) 음수 | 〃 |
| `+0x211 / +0x216` | 9 | 클래스 4 호출 상태 | 필수 칸: 클래스 4 실패 / 부가 비트 `0x200` 꺼짐 / 컨트롤 영역 `+0x38` 비트 `0x20` 꺼짐 / 함수 덮어쓰기 고침 없음 |
| `+0x24f / +0x254` | 5 | `PspMapSystemDll` 음수 | 모든 칸 |
| `PspInitializeSystemDlls +0xa0 / +0xa5` | 6 | `PspLookupEntryPoint` 음수 (`0xc000007a` = 이름 없음) | 이름 표의 모든 이름 |

- 필수 아닌 칸 (WOW ntdll·vertdll) 은 **파일이 없으면 조용히 빠진다** (칸 `+0x20` 이 0 으로 남음). 하지만 일단 연 뒤에는 어느 칸이든 섹션·참조·올리기 실패가 부팅을 멈춘다 `[정적]`.

### 프로세스 생성 때 이 짐을 읽는 자리 (질문 4, 짝)

| 부팅 때 쓴 것 | 프로세스 생성 (①) 때 읽는 자리 |
|---|---|
| 섹션 객체 (칸 `+0x0` 빠른 참조) | `MiMapProcessExecutable +0xdc → PsMapSystemDlls +0xc0 → PspMapSystemDll +0x40 PspReferenceSystemDll` → `+0x11d MmMapViewOfSectionEx` (인자4=0). 올릴 칸: 0번 늘 · WOW 프로세스 (`EPROCESS +0x310` ≠ 0) 면 `PsWow64GetProcessNtdllType` 번호 칸 · 보안 프로세스 (`+0x170` 비트0) 면 플래그 `0x40` 칸 (`PsMapSystemDlls +0x7c ~ +0xa4`, `+0x12a`). `PsMapSystemDlls` 는 `MiMapProcessExecutable` 플래그 `0x10` 이 꺼졌을 때만 (`+0xce ~ +0xd0`) |
| 칸 `+0x20` (기본 주소) | `PspMapSystemDll +0x172` (비교), `PspInitializeThunkContext +0x196 · +0x19d` (새 스레드 `Rdx`, run16·run18), 그 밖 `PspGetScpCfgFunctions`·`PsShutdownSystem`·`PsQuerySystemDllInfo`·`MiRelocateImage +0x756 · +0x760` |
| 칸 `+0xc` (`SizeOfImage`) | `MiRelocateImage +0x75d` |
| 칸 `+0x38` (재배치량) | `PspPrepareSystemDllInitBlock +0xa5/+0xac` (→ init 블록 `+0x10`), `+0xcb ~ +0xd6` (WOW → `+0x8`) (이름 `SystemDllNativeRelocation`·`SystemDllWowRelocation` 은 run32) |
| 칸 `+0x8` 플래그 (파일 값) | `PsMapSystemDlls +0x12a`, `PspMapSystemDll +0x68` |
| `PsNtdllExports` | `PspInitializeThunkContext +0x1a1 · +0x37f` (`+0x8` → 새 스레드 `Rip`), `PspPrepareSystemDllInitBlock +0x214` (`+0x0`), `NtCreateUserProcess +0xd10` (`+0x10` `RtlUserThreadStart`) 등 — 읽기 약 40줄 (`_refs/PsNtdllExports.tsv`) |
| `PsWowX86SharedInformation` | WOW 쪽 (안 봄). `PspPrepareSystemDllInitBlock +0xc3 ~ +0xef` 가 복사하는 것으로 읽힘 `[추측]` |
| `MiState.Processes.SystemDllBase` | `MmGetSectionInformation` (그 밖 안 봄) |

- 사용자 짐작 판정: **맞다** `[정적]`. 세 가지 (섹션·기본 주소·`PsNtdllExports`) 모두 부팅 때 한 번 쓰고, 프로세스마다는 **읽기·비교·올리기**뿐이다. 다만 프로세스마다 읽은 값으로 새로 만드는 것 (init 블록, 새 스레드 문맥) 은 있다 — run16·run32 가 다룬 ① 짐.

## 확인된 것

- **시스템 DLL 준비는 부팅 `Phase1` 에서 한 번이다.** `Phase1Initialization +0x3b IoInitSystem → +0x17 IoInitSystemPreDrivers → +0x10fa PsLocateSystemDlls → +0xa3 PspLocateSystemDll → +0x238 PspMapSystemDll`. `PsLocateSystemDlls` caller 는 `IoInitSystemPreDrivers +0x10f5` 하나 (`_refs`: 코드 호출 1줄 + `.pdata` 1줄). `[정적]`
- **부팅 안 순서**: `Phase1Initialization` 이 `Discard (+0x1e)` → `IoInitSystem (+0x36)` → `Phase1InitializationIoReady (+0x61)` 순으로 부르므로 `PsLocateSystemDlls` → `PspInitPhase2` (`PspInitializeSystemDlls`, IoReady `+0x1d5`) → `MmInitSystemDll` (`+0x1e2`) → `PspInitPhase3` (`+0x20a`). run16 이 비워 둔 "앞뒤 순서" 가 닫혔다. `[정적]`
- `PsLocateSystemDlls` 는 `IopInitializeBootDrivers` (`+0xf3b`) **뒤**, `IopReassignSystemRoot` (`+0x1139`) **앞**에서 불린다. `\SystemRoot` 링크가 이미 있어야 한다. `[정적]`
- **칸 `+0x20`·`+0xc`·`+0x28` 을 쓰는 줄은 `PspMapSystemDll` 떨어진 조각 rva `0xb18c2c`·`0xb18c33`·`0xb18c36`** (인자4 ≠ 0 갈래). `+0x38` 은 `PspLocateSystemDll +0x1aa`. `[정적]`
- **`PsNtdllExports` 는 `PspInitializeSystemDlls +0x6b` 하나가 19칸을 쓴다.** 안 쓰는 두 칸 (`+0x38`·`+0x98`) 이 run44 사진에서도 0. `[정적]` + `[관찰]` run44 K1
- **ntdll 섹션은 exe 와 같은 `MmCreateSpecialImageSection` 사슬로 만든다** — 토큰 NULL·서명 수준 `0x0c`·보호 이미지 패킷 플래그 `0x10` 이 다르다. `[정적]`
- **`MmInitSystemDll +0x22`** 가 `MiState.Processes.SystemDllBase` (rva `0xe2db80` = `MiState+0x1000`) 에 0번 칸 `+0x20` 을 복사한다. `[정적]` (PDB 이름 `_MI_PROCESS_STATE +0x0`)

## 디버거가 붙어서 달라지는 점

- **`PspLocateSystemDll +0x79 ZwSystemDebugControl` (명령 `0x26`) 은 커널 디버깅을 켠 부팅에서 더 깊이 간다** `[정적]`.
  - `IoInitSystemPreDrivers` 가 `NtGlobalFlag` 비트 `0x40000` 을 `PsLocateSystemDlls` 동안만 켜 줘서 (`+0x10ef`, `+0x10fa` 되돌림), 이 호출은 칸마다 **늘** 탄다.
  - 명령 `0x26` 은 `NtSystemDebugControl` 에서 디버그 권한 검사를 면제 (`+0xc8 CMP EBX,0x26`) 받고, 입력 `0x10`·출력 `0` 이면 `ExpKdPullRemoteFileForUser` (조각 `FUN_140b3e0e7 +0x1f8`, ret `+0x1fd`) 로 간다.
  - **커널 디버깅을 안 켠 부팅** (`KdPitchDebugger` 켜짐, `KdLocalDebugEnabled` 꺼짐): `NtSystemDebugControl` 앞에서 `0xc0000354` 로 끝. 남는 것 없음.
  - **커널 디버깅을 켠 부팅** (우리 타깃 VM — KDNET): 경로를 소문자로 만들어 SHA-256 해시 → "이미 끌어온 파일 표" `ExpSysDbgPulledFileTable` 에 없으면 `KdPullRemoteFile` 을 부른다. 표가 새로 생길 수 있다 (풀 `0x4010` 바이트). 호스트가 실제로 파일을 넘겨주는지는 연결 상태에 달림 `[공백]`.
  - `PspLocateSystemDll` 은 반환값을 안 본다 (`+0x7e` 다음 `+0x81` 로 그냥 이어짐). 그래서 **보통 부팅과 디버깅 부팅의 차이는 `ExpSysDbgPulledFileTable` 같은 커널 디버거 쪽 상태뿐**이고, 칸 값에는 영향이 없을 것 `[추측]` (`KdPullRemoteFile` 이 성공해 다른 파일을 열게 되는 길이 있는지는 안 봄).
  - 이름 "명령 0x26 = 원격 파일 끌어오기" 는 함수 이름과 분기 사슬로 읽은 것 `[정적]`. 번호 이름 표는 안 봄.
  - 짐 지도에는 `ExpSysDbgPulledFileTable` 을 "디버거 붙은 부팅만" 줄로 따로 둔다 (baggage 7.6.2). 에뮬레이터는 디버거가 없는 부팅을 흉내 내면 이 짐이 없다.

## 정정

- **run16 (157줄) · flow.md 2.1 `PsMapSystemDlls` 절 — "칸 `+0x20`·`+0x28` 은 `PspLocateSystemDll +0x238` / `PsLocateSystemDlls +0x89` 에서 채운다"** 는 반만 맞다.
  - `PsLocateSystemDlls +0x89` 는 **별칭 칸 (플래그 `0x20`) 전용** 갈래의 ret 자리다. 이 빌드 표 처음 값에는 그런 칸이 없어 **안 탄다** `[정적]`. 이 빌드에서 칸을 채우는 길은 `PspLocateSystemDll +0x238` 하나다.
  - 같은 줄들이 `+0xc` (`SizeOfImage`) 도 같이 채운다.
  - `IoInitSystemPreDrivers +0x10f5` 는 call 자리다. WinDbg 콜스택에 보일 값은 ret `+0x10fa`.
  - run16 맨 위에 정정 한 줄을 넣었다. flow.md 는 이 스프린트 문서라 바로 고쳤다.
- **run10 (182줄) — "`PspMapSystemDll` 입구는 부팅 때 System 프로세스에도 걸린다 (caller 에 `PspInitPhase3`)"** 의 이유가 틀렸다.
  - 부팅 때 System 프로세스 (현재 프로세스) 에 올리는 caller 는 `PspLocateSystemDll +0x233` (와 안 타는 별칭 갈래 `PsLocateSystemDlls +0x84`) 이다.
  - `PspInitPhase3` 는 `VslVsmEnabled` 일 때만, **`PsSecureSystemProcess`** 에 올린다 `[정적]`.
  - 결론 ("새 프로세스만 보려면 `PsMapSystemDlls` 쪽이 낫다") 은 그대로다. 그리고 이 부팅 호출들은 모두 로그인 전이라 보통 KDNET 세션에서는 이미 지나간 뒤다 `[추측]` (시점으로 짐작).
  - run10 맨 위에 정정 한 줄을 넣었다.
- 보탬 (틀린 말은 아님): flow.md 2.1 의 `[추측]` 두 줄 ("다른 caller 3곳 = 부팅 때 ntdll 섹션 준비 쪽", "섹션은 부팅 때 한 번, 프로세스마다 붙이기만") → `[정적]` run48. `PsMapSystemDlls` 나머지 칸 조건에 보안 프로세스 (`+0x170` 비트0 + 칸 플래그 `0x40`) 를 더했다.

## 아직 확인 필요

- 동적 (결과서 번호 그대로). **D1·D2 는 plan Q42 에 출처로 합침**, **D3 ~ D8 (부팅 뒤 값만)·D9 ~ D12 (부팅 디버깅, 낮은 우선순위) 는 새 Q94** (오케스트레이터). 멈춤점·조건은 dast-mentor 몫.
  - **A. 부팅 뒤 보통 KDNET 세션 / 덤프에서 값만** (`dq`·`dt`·`lm`):
    - **D1** `dq nt!PspSystemDlls L6` — `[0][1][4]` 만 0 이 아닌가. 칸마다 `dq <칸> L8`: `+0x8` = `0x86640013` / `0x014c0008` / `0x86640012`, `+0xc` = `0x267000` / `0x1bf000` / `0x36000` 기대 (PE `SizeOfImage`), `+0x20`·`+0x28`·`+0x38`. `+0x20` = `+0x28` 인가, `+0x20` = 사용자 프로세스 `lm m ntdll` 인가, `+0x20 − 0x180000000` = `+0x38` 인가. → **Q42**
      - 실마리: run44 `lm m ntdll` 크기 `0x267000` (K1 로그 54줄) = 기대 `+0xc` 값. 칸 자체는 안 읽었다.
      - 결과서 추천: `nt!PspMapSystemDll+0x172` (프로세스 생성 때 비교 줄) 에서 `dq @rbx L8` 과 `rax` 를 같이 보면 test.exe 한 번에 "칸 `+0x20` == 올라간 주소" 를 본다 (test.exe 조건).
    - **D2** `dq nt!PsNtdllExports L15` — `+0x38`·`+0x98` 이 0 인가 → **이미 run44 K1 로그 1531 ~ 1542줄에 있다 `[관찰]`** (둘 다 0). 남은 것: `dq nt!PsWowX86SharedInformation L9` (`+0x30` = WOW ntdll 기본 주소). → **Q42**
    - **D3** System 프로세스 (PID 4) 에 ntdll 뷰가 있나 (`!process 4 0` → `.process /p /r` → `lm m ntdll` 또는 `!vad`), 시작 = 칸 `+0x28` 인가. SysWOW64 ntdll·vertdll 뷰도 있나. → Q94
    - **D4** `dq nt!MiState+0x1000 L1` = `poi(nt!PspSystemDlls)+0x20` 인가. → Q94
    - **D5** 뷰 헤더 `ImageBase` 가 새 주소로 바뀌었나 — D3 문맥에서 `+0x28` 위치 `!dh`. 정적으로 못 닫은 MM 줄 확인. → Q94
    - **D6** `dd nt!NtGlobalFlag` (비트 `0x40000` 꺼짐 = 되돌림), `dq nt!ExpSysDbgPulledFileTable L1` (≠ 0 이면 명령 `0x26` 이 표까지 감) — **디버거가 붙어서 달라지는 점** 확인. 표가 0 이어도 두 경우를 못 가린다 `[공백]`. → Q94
    - **D7** D1 에서 `[1]`·`[4]` 의 `+0x20`·`+0x28` 이 채워졌으면 그 칸도 찾아 올려진 것. → Q94
    - **D8** `dd nt!VslVsmEnabled L1` — 0 이면 `PspInitPhase3` 갈래 안 탐. → Q94
  - **B. 부팅 디버깅 필요** (낮은 우선순위, Q94 — `Phase1` 이라 로그인 뒤 `bp` 는 이미 지나감. VM 을 다시 켜며 첫 접속에서 멈춘 뒤 `bp`):
    - **D9** `bp nt!PspLocateSystemDll` — `rcx` 가 `PspNativeSystemDllData` → `PspWowX86SystemDllData` → `PspVsmEnclaveRuntimeDllData` 순인가, `edx` 0/4, 반환 `eax`. `bp nt!PspMapSystemDll` 의 `r9d` = 1, `rcx` = `PsInitialSystemProcess` 인가.
    - **D10** `PspMapSystemDll +0x11d` 의 `eax` 가 `0` 인가 `0x40000003` 인가.
    - **D11** `PspLocateSystemDll +0x7e` 의 `eax`, `IoInitSystemPreDrivers +0x10ef` 직후 `NtGlobalFlag` 비트 `0x40000`.
    - **D12** `PspLocateSystemDll +0xef` 인자 (`r8d` 0, `r9b` `0xc`, `[rsp+0x28]` 2 또는 6) 와 패킷 `+0x18` = `0xc`.
- 정적 (결과서 4장 `[공백]` 9건 — 대부분 MM 안 뜻):
  - 뷰 헤더 `ImageBase` 를 고치는 MM 줄 (D5), `MmMapViewOfSectionEx` 할당 종류 `0x40000`·`0x20000000` 뜻, `MiGetControlAreaLoadConfig` 칸 이름 (사이트 9 조건), 서명 검사 본체 (`SeGetImageRequiredSigningLevel`), `PsInitSystem(1)` 인자, `PspInitPhase3` VBS 본문, `PsWowX86SharedInformation` 읽는 자리.
  - **`\SystemRoot` 링크를 만드는 곳** (결과서 공백 2): `_calls.tsv` 에 `Phase1InitializationDiscard +0xc31 CreateSystemRootLink` (ret `+0xc36`) 줄이 있다. 이름으로 보면 후보 `[추측]` (본문 안 읽음). `Discard` 는 `IoInitSystem` 보다 먼저라 순서는 맞다 `[정적]`. → 다음 바퀴 후보 (작은 정적).

## 도구 메모

- 결과서 제안 둘 (하네스라 오케스트레이터가 사용자에게):
  - PE 파일의 초기화된 데이터를 읽는 도구 (`pe_data.py <바이너리> <이름|0xRVA> <바이트수>`, PDB 공개 이름으로 포인터를 이름에 맞춤). 이번에 표 처음 값을 임시 파이썬으로 읽었다. 짐 지도 "입력 (파일 값)" 칸마다 필요.
  - `pdb_struct.py --list` 가 "이름만 있고 칸 정의가 없는 타입" (`_PS_SYSTEM_DLL_INFO`) 을 따로 표시.
- `.c` 함정 (결과서 5장): `PsMapSystemDlls.c` 는 `PspMapSystemDll` 인자를 하나만 그린다 (`.asm` 은 넷). `MiInitializeCreateSectionPacket.c` 는 `0x02000000` 갈래를 `FUN_140b23a4f` 호출로 그려 흐름이 틀어진다. 인자·갈래는 `.asm` 으로 셀 것.
- 결과서 알림: `MiInitializeCreateSectionPacket.c`·`MiSectionControlArea.c` 를 확인 없이 다시 뽑아 덮어썼다 (같은 방식이라 내용 같을 것). `pdb_aliases.py --lookup` 은 `ms/sym/ntoskrnl.aliases.tsv` 가 없어 이번엔 못 돌렸다 (떨어진 조각 rva `0xb18c28` 의 다른 이름 확인 못 함).

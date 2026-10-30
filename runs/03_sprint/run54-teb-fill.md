# 2026-10-30 run54 — TEB 를 채울 때 (Q78d3): `_TEB` 모르는 칸 133 → **여전히 모름 0**. 커널이 틀을 만들고 (`MmCreateTeb` 64비트 갈래 13줄 · `PspAllocateThread +0x793` `SameTebFlags` 16비트 · 첫 스레드 시작 때 `KiUpdateTebSchedulingPropertiesCurrentThread` 두 줄), ntdll 이 채운다 (TLS 벡터·난수 씨앗·활성화 문맥 스택·`LoadOwner`·`LastStatusValue`·`FlsData`). 끝까지 0 112칸은 0 을 씀 12 · 조건부 안 탐 13 · 아무도 안 씀 28 · 범위 밖 길 59 `[추측]` (ntdll 19 · nt 1 · 다른 DLL 39) 으로 갈림. **TEB 의 0 은 `memset` 이 아니라 새로 커밋된 페이지** (정적)

## 요약 표 (모르는 칸 133 → ?)

- 결과서 맨 앞 표 그대로. 분류 뜻은 run50·run52·run53 과 같다.
- **셈 방식**: run50·run52·run53 그대로 — **명시 0 저장·입력값 쓰기·같은 DWORD (여기선 16비트) 되쓰기만** "0 을 씀".
  - 이번엔 **몸통 `memset` 이 없다** (`MmCreateTeb`·`MiAllocateFromSubAllocatedRegion` 둘 다 안 부름, `_calls.tsv`). TEB 페이지는 서브할당 구역을 새로 커밋한 **요구 시 0 페이지** 라 처음부터 0 이다 — `memset` 부재는 `[정적]`, 커밋 방식은 `[추측]` (사진 K2 에서 0 확인). 그래서 run53 처럼 "요청서 문구대로 셌을 때의 대안 숫자" 는 **없다**.
- **범위 밖 길은 이번에 처음 셋으로 나눈다** (유저 구조체라 쓰는 쪽이 커널만이 아니다): ntdll (지도 밖 ntdll 함수) / nt (커널 함수) / 다른 DLL (KERNELBASE·GDI·OpenGL 등 — 이름으로 짐작한 것 포함).

| 사진 구간 | 칸 | 아는 칸 nt | 아는 칸 ntdll | 아는 칸 다른 DLL | 0 을 씀 | 조건부 안 탐 | 아무도 안 씀 (길 안) | 범위 밖 ntdll | 범위 밖 nt | 범위 밖 다른 DLL | 모름 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `K1~K2` | 8 | **8** | — | — | — | — | — | — | — | — | 0 |
| `P0~P1` | 5 | **5** | — | — | — | — | — | — | — | — | 0 |
| `P1~P1a` | 5 | — | **5** | — | — | — | — | — | — | — | 0 |
| `P1a~P1b` | 1 | — | **1** | — | — | — | — | — | — | — | 0 |
| `P1b~P2` | 2 | — | **2** | — | — | — | — | — | — | — | 0 |
| `끝까지 0` | 112 | — | — | — | **12** (nt 11 · ntdll 1) | **13** (nt 8 · ntdll 5) | **28** | **19** | **1** | **39** | 0 |
| **합** | **133** | **13** | **8** | **0** | **12** | **13** | **28** | **19** | **1** | **39** | **0** |

- `P1b~P2` 의 `LastStatusValue`·`FlsData` 는 **쓰는 줄이 ntdll** (`RtlNtStatusToDosError +0x1d`, `RtlFlsSetValue +0x104`) 이고 부르는 쪽 (KERNELBASE·CRT) 은 `[공백]` → 쓴 주인 ntdll 로 셈.
- "범위 밖 길" 59 와 "아무도 안 씀" 28 은 **ntdll·커널·KERNELBASE·KERNEL32 전체를 TEB 칸 오프셋으로 훑은 결과**가 근거다 (`objdump -d`, `.asm` 없음). 한계:
  - TEB 를 `gs:[0x30]` 로 얻은 함수는 레지스터를 따라가며 모았다 (분기·레이블 합치기 있음, 호출 뒤 휘발 레지스터는 버림).
  - TEB 를 **인자로 받는** 함수 (`RtlpInitializeThreadActivationContextStack`(RCX), `RtlpFlsSetValue`(슬롯 포인터)) 와 **인덱스 레지스터 쓰기** (`TlsSlots[i]`) 는 숫자 일치 후보를 눈으로 가렸다 → 오탐·누락 가능.
  - 커널은 `[x+0xf0]` (`_KTHREAD.Teb`) 에서 읽은 레지스터만 따라갔다.
  - "다른 DLL" 39 중 쓰는 줄을 본 건 KERNELBASE 4칸뿐이다. 나머지 35칸은 칸 이름으로 짐작한 DLL 이다 `[추측]`.

짐 지도 (`baggage.md` 2장) 로 옮길 때 — run52·run53 과 같은 규칙:

- "아는 칸" 21 + "0 을 씀" 12 + "조건부 안 탐" 13 = **46** → 쓴 주인 열 ("nt 가 씀" / "ntdll 이 씀")
- "아무도 안 씀" **28** → 2장 표 아래 줄 (유저 구조체에서 처음)
- "범위 밖 길에서만 씀 `[추측]`" **59** → 모름에서 빼고 따로 한 줄 (유저 구조체에서 처음, ntdll 19 · nt 1 · 다른 DLL 39)
- 그리고 이미 아는 칸 **`NtTib` 에 커널 쓰는 줄을 보탬** (아래 "정정") → `NtTib` 은 nt·ntdll 양쪽

| | nt 가 씀 | ntdll 이 씀 | 다른 DLL | 아무도 안 씀 | 범위 밖 길에서만 씀 `[추측]` | 아직 모름 | 아는 칸 = 정확 + 짐작 |
|---|---|---|---|---|---|---|---|
| `_TEB` (137칸) | 1 → **34** | 3 → **17** | 0 → **0** | 0 → **28** | 0 → **59** | 133 → **0** | 4 + 0 → **50 + 0** |

- nt 34 = `CurrentLocale` 1 + `NtTib` 1 (정정) + 아는 칸 13 + 0 을 씀 11 + 조건부 8. ntdll 17 = 옛 3 + 아는 칸 8 + 0 을 씀 1 (`HeapWalkContext`) + 조건부 5. 둘이 겹치는 칸 1 (`NtTib`) → 34 + 17 − 1 = **50** = 아는 칸.
- `SameTebFlags*` 16비트 통째 칸은 nt 로만 셌다. ntdll 이 뒤에 켜고 끄는 비트는 비트 칸 (`RanProcessInit`·`LoadOwner`) 으로 센다.
- 새 아는 칸 46 은 모두 정확. 짐작 0.
- 짐 지도 전체: 구조체 21개 직접 칸 1,456 = 아는 칸 656 → **702** (정확 601 → **647** · 짐작 55) + 아무도 안 씀 132 → **160** + 범위 밖 길에서만 씀 198 → **257** + 모름 470 → **337**.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 03_sprint 열두째 바퀴 (짐 지도 바퀴, plan **Q78d3** — Q78d 첫 스레드의 셋째 조각).
- 정적: ntoskrnl (`ntkrnlmp.pdb`) GUID `C29EBFB06B78B3C020DCA66D99713F9E1`, ntdll (`ntdll.pdb`) GUID `C3093720177DA6851519DCFFE8DF53FE1` (flow.md 기준과 같음, 결과서 **PDB 일치** — 사진 로그 `04_baggage-read_K2.txt`·`_P2.txt` 의 `lm` 줄과도 같음). 이미 analyze 된 프로젝트에서 뽑기만 했다.
- 버전 대조 (결과서): `_calls.tsv` 알려진 호출 지점 (`NtCreateUserProcess +0xc36`·`PspInsertThread +0x807`·`LdrInitializeThunk +0xe`·`LdrpInitializeInternal +0x5a`·`_LdrpInitialize +0x16c`·`LdrpInitializeProcess +0x1bbb`·`LdrpInitializeNode +0x16b/+0x19c`) 이 모두 맞고, **사진 K2 ~ P2 의 TEB 값이 칸마다 코드와 맞는다** (아래 표).
- 오케스트레이터가 `.asm`·PDB 로 확인한 것: `PspAllocateThread +0x793 MOV word [RBX+0x17ee]` (`SameTebFlags`), `MmCreateTeb +0xc1 ~ +0x13a` 대입 묶음, `MmCreateTeb +0x175 · +0x17e` = `[RCX+0x6b8]`·`[RCX+0x6b4]` (`_TEB32` 쪽), `KiUpdateTebSchedulingPropertiesCurrentThread +0x136` (`IdealProcessorValue` +0x1744) · `+0x141` (`PrimaryGroupAffinity` +0x1860), ntdll `LdrpAllocateTls +0x1e2`, `RtlFlsSetValue +0x104` (`FlsData` +0x17c8), `RtlNtStatusToDosError +0x1d` (`LastStatusValue` +0x1250).
- 이 문서를 쓰며 다시 맞춘 것 (flow-writer, `.asm`·`_calls.tsv`): `MmCreateTeb` `+0xc1 ~ +0x13a` 13줄 (`+0x126`·`+0x12e` 가 `INITIAL_TEB +0x10`·`+0x18` 을 읽어 `+0x12a`·`+0x132` 에 씀), `PspAllocateThread +0x78d` (`KiStackAttachProcess`, ret `+0x792`), `NtCreateUserProcess +0x1bf`·`+0x1ce`·`+0xdfd`, `PspUserThreadStartup +0x94` → `KeUpdateTebSchedulingPropertiesCurrentThread +0x15` → `KiUpdateTebSchedulingPropertiesCurrentThread`, `LdrpInitializeProcess` 의 `+0x153`·`+0xb0d`·`+0x18cb` 와 `LdrpDrainWorkQueue` 4곳·`LdrpDropLastInProgressCount` 3곳, `LdrpInitializeTls +0x15d`, `RtlNtStatusToDosError +0xf`.
- `.asm` 이 없는 곳 (결과서가 `objdump -d` 로만 봄): 모듈 전체 TEB 쓰기 훑기, "범위 밖 길" 표의 함수 대부분 (`Rtl*Thread*`·`Tpp*`·`Etw*`·`RtlpFlsSetValue +0xf4` 등), KERNELBASE 쪽 이름 전부. 이 문서에선 결과서의 함수+오프셋 그대로 쓴다.

## 목적

- 짐 지도 Q78 의 여덟째 조각. `_TEB` (baggage 3.3) 모르는 칸 133 전부를 닫는다.
  - `K1~K2` 8 — 커널 `MmCreateTeb`·`PspAllocateThread` 가 쓰는 줄과 값의 출처.
  - `P0~P2` 13 — 첫 스레드 시작 (커널) 과 ntdll 초기화 길에서 쓰는 줄.
  - 끝까지 0 112 — 네 가지로 가르기 (범위 밖 길은 ntdll / nt / 다른 DLL 로).
- 갈림길과 간접 호출.
- 범위 밖: 일꾼 스레드 2개의 TEB (C10, Q79), `TlsSlots`·`TlsExpansionSlots` 가 가리키는 안쪽, `GdiTebBatch` 같은 큰 배열 안쪽, `_TEB32` (WOW64).

## 방법

- 정적: `_workspace/sast/48_teb-fill.md` — ntoskrnl·ntdll. 새로 depth 0 **25개** (커널 9 · ntdll 16), 나머지는 이미 있어 읽기만. `_calls.tsv` 커널 4,307 → 4,332줄, ntdll 2,188 → 2,299줄 (줄이 안 사라짐).
  - 커널 새로 뽑은 것: `KeUpdateTebSchedulingPropertiesCurrentThread`·`KiUpdateTebSchedulingPropertiesCurrentThread`·`KiUpdateTebApc` (`P0~P1` 다섯 칸), `RtlCreateUserStack` (`DeallocationStack`·스택 경계의 출처), `MiAllocateFromSubAllocatedRegion` (TEB 페이지가 왜 0 인가), `PspWriteTebImpersonationInfo`·`KeRaiseUserException` (범위 밖 근거), `KiQueueTebUpdateApc`·`MmDeleteTeb` (뽑기만 — Q78d4·종료 쪽 자료).
  - ntdll 새로 뽑은 것: `LdrInitializePerThreadRng`·`RtlInitializeTRng`·`LdrpGenRandom` (`RngState`), `RtlNtStatusToDosError`·`RtlSetLastWin32ErrorAndNtStatusFromNtStatus`·`RtlRestoreLastWin32Error` (`LastStatusValue`·`LastErrorValue`), `RtlFlsSetValue` (`FlsData`), 힙 LFH·VS 넷 (`HeapData`), `RtlpCheckRelativeDrive` (`HardErrorMode`), `RtlTlsAlloc` (`TlsSlots`), `RtlpHpEnvFlsCleanup` (`HeapFlsData`), 둘은 뽑기만.
  - 칸 이름·오프셋은 `pdb_struct.py ntdll.dll _TEB` (+ `_TEB32`), 비트 칸은 `ms/disassemble/ntdll/_baggage/struct__TEB.tsv`. 커널 코드는 TEB 를 타입 없이 오프셋으로 쓰므로 오프셋으로 짝지었다.
  - 값 대조: run44 덤프 읽기 로그 (K2 ~ P2 일곱 장 — K1 엔 TEB 가 없음). 새 동적 실행은 없다.
- 결과서: `_workspace/sast/48_teb-fill_result.md` (**분석성공**, PDB 일치 둘 다)
- 문서: 이 run 문서, `baggage.md` (머리, 쓰는 말, 1장, 2장·2.1, 3장 줄임·3.3 다시 씀·3.13 머리, 5장 ①·②, 6장, 7장 머리·7.1·7.2·7.5, 새 7.17, 8장), `flow.md` (기준·2.1 `PspAllocateThread`·`MmCreateTeb` 절·2.3 `PspUserThreadStartup` 절·2.4 `LdrpInitializeProcess` 트리·4장·5장). 그림 (mermaid) 은 안 바꿨다.
- **새 줄임 둘** (짐 지도 3장 줄임 표): `①TB…` = `①A… → MmCreateTeb (호출 +0x62c)`, `①TS…` = `①T… → KeUpdateTebSchedulingPropertiesCurrentThread (호출 +0x94) → KiUpdateTebSchedulingPropertiesCurrentThread (호출 +0x15)`.
  - 기존 줄임과 글자가 겹치지 않는다 (`①T…`·`①Tok…`·`①TD…` 와 다름, flow-writer 가 `runs/` 전체에서 찾아 봄). 단 **`①TB…` 는 `①T…` 아래가 아니라 `①A…` 아래**다 (`①TD…` 가 `①Tok…` 아래인 것처럼 글자만 T 로 시작). `①TS…` 는 `①T…` 아래가 맞다.

## 결과

비유 (새 직원 책상 차리기, 이어서):

- TEB = 새 직원의 **개인 서랍**. 본사 (커널) 가 서랍을 새로 들여놓는다. 새 서랍이라 처음부터 텅 비어 있다 (지우개질이 필요 없음).
- 본사가 서랍 앞에 이름표 몇 장을 붙인다: 사번 (`ClientId`), 회사 안내판 위치 (`ProcessEnvironmentBlock`), 책상 높이 (스택 경계), "첫 직원" 표시 (`InitialThread`).
- 첫 출근 날 본사가 "앉을 자리" (이상적 프로세서·그룹 친화도) 를 한 번 더 적는다.
- 그 뒤 직원 본인 (ntdll) 이 서랍 칸막이 (TLS 벡터·활성화 문맥 스택)·주사위 (난수 씨앗) 를 넣는다.
- 서랍 칸 대부분은 GDI·OpenGL·소켓처럼 **나중에 다른 부서가 쓸 칸**이라 초기화 동안엔 비어 있다.

### 정적 — 호출 지점 (`_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0xe57` | `+0xe5c` | `nt!PspAllocateThread` | [정적] |
| `nt!PspAllocateThread` | `+0x5ea` | `+0x5ef` | `PspSetupUserStack` (→ `RtlCreateUserStack` 호출 `+0x89`, `INITIAL_TEB` 채움) | [정적] |
| `nt!PspAllocateThread` | `+0x62c` | `+0x631` | `nt!MmCreateTeb` | [정적] |
| `nt!MmCreateTeb` | `+0x7a` | `+0x7f` | `KiStackAttachProcess` (새 프로세스에 붙기) | [정적] |
| `nt!MmCreateTeb` | `+0xa7` | `+0xac` | `MiAllocateFromSubAllocatedRegion` (TEB 자리, `memset` 없음) | [정적] |
| `nt!PspAllocateThread` | `+0x78d` | `+0x792` | `KiStackAttachProcess` (→ `+0x793` `SameTebFlags`) | [정적] |
| `nt!PspUserThreadStartup` | `+0x94` | `+0x99` | `KeUpdateTebSchedulingPropertiesCurrentThread` | [정적] |
| `nt!KeUpdateTebSchedulingPropertiesCurrentThread` | `+0x15` | `+0x1a` | `KiUpdateTebSchedulingPropertiesCurrentThread` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x153` | `+0x158` | `LdrInitializePerThreadRng` (⇒ `RtlInitializeTRng`, 꼬리 JMP `+0x54`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0xb0d` | `+0xb12` | `RtlpInitializeThreadActivationContextStack` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x18cb` | `+0x18d0` | `LdrpInitializeTls` (→ `LdrpAllocateTls` 호출 `+0x15d`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1302` · `+0x19c1` · `+0x1aac` · `+0x1b8c` | `+0x1307` · `+0x19c6` · `+0x1ab1` · `+0x1b91` | `LdrpDrainWorkQueue` (`LoadOwner` 켬 `+0xd3`) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1753` · `+0x1b40` · `+0x1be7` | `+0x1758` · `+0x1b45` · `+0x1bec` | `LdrpDropLastInProgressCount` (`LoadOwner` 끔 `+0x12`) | [정적] |

### 호출 관계 (대표 지점)

```
(부모 cmd 스레드, 커널)
NtCreateUserProcess
 └─ +0xe5c  PspAllocateThread                                                         [정적]
         │  +0x54a ~ +0x5a5  만들기 플래그 → SameTebFlags 값 (첫 스레드는 늘 0x400)
         ├─ +0x5ef  PspSetupUserStack → RtlCreateUserStack   INITIAL_TEB (StackBase·StackLimit·AllocatedStackBase)
         ├─ +0x631  MmCreateTeb
         │       ├─ +0x7f   KiStackAttachProcess
         │       ├─ +0xac   MiAllocateFromSubAllocatedRegion   새 커밋 페이지 (memset 없음)
         │       │  +0xc1 ~ +0x13a  64비트 갈래 13줄: NtTib 넷·ClientId·TxFsContext·RealClientId·
         │       │                   StaticUnicodeString 머리·ProcessEnvironmentBlock·DeallocationStack
         │       └─ +0x29e  KiUnstackDetachProcess
         ├─ +0x792  KiStackAttachProcess
         │  +0x793  SameTebFlags (16비트 통째)
         └─ …
(test.exe 첫 스레드, 커널)
KiStartUserThread → PspUserThreadStartup
         │  +0x78  CurrentLocale (run15)
         └─ +0x99  KeUpdateTebSchedulingPropertiesCurrentThread
                 └─ +0x1a  KiUpdateTebSchedulingPropertiesCurrentThread
                          +0x136 이상적 프로세서 4B · +0x141 PrimaryGroupAffinity 16B
(test.exe 첫 스레드, ntdll)
LdrpInitializeProcess
 ├─ +0x158  LdrInitializePerThreadRng ⇒ RtlInitializeTRng   RngState 앞 16B
 ├─ +0xb12  RtlpInitializeThreadActivationContextStack   _ActivationStack · ActivationContextStackPointer
 ├─ +0x18d0 LdrpInitializeTls → +0x162 LdrpAllocateTls   ThreadLocalStoragePointer
 ├─ +0x1b91 LdrpDrainWorkQueue   LoadOwner 켬 (마지막 켬)
 └─ +0x1bec LdrpDropLastInProgressCount   LoadOwner 끔
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 쓰는 줄.
- `LdrpDrainWorkQueue`·`LdrpDropLastInProgressCount` 는 다른 자리에서도 불린다 (위 표). 트리엔 사진과 맞는 짝만 적었다 — 그 짝이 맞는지는 `[추측]` (D6).
- 지도 밖이라 트리에 없는 것: `RtlNtStatusToDosError` (`LastStatusValue`, 호출자 `[공백]`), `RtlFlsSetValue` (`FlsData`, 사용자 코드 → KERNELBASE → ntdll `[추측]`), 힙 할당기 (`HeapData`·`RngState` 갱신).

### TEB 의 0 은 어디서 오나

- `MmCreateTeb` 도 `MiAllocateFromSubAllocatedRegion` 도 `memset` 을 부르지 않는다 (`_calls.tsv`: 앞쪽 `KiStackAttachProcess`·`MiAllocateFromSubAllocatedRegion`·`KiUnstackDetachProcess`, 뒤쪽 `RtlFindClearBitsAndSetEx`·`MiCommitExistingVad` 등) `[정적]`.
- TEB 페이지는 서브할당 구역을 새로 커밋한 페이지라 처음부터 0 이다 `[추측]` (요구 시 0 페이지). 사진 K2 의 112칸 0 과 맞는다.
- 크기: `0x1878` 을 페이지로 올려 `0x2000` (`MmCreateTeb +0x85 ~ +0xa2`).

### `K1~K2` 8칸 (질문 1)

- **`MmCreateTeb` 의 메모리 쓰기 줄은 모두 50줄** (run11 "약 47줄" 과 같은 묶음). 갈래 셋:
  - 64비트 갈래 (EDI = 4) **13줄** `+0xc1 ~ +0x13a` — test.exe 가 탄다.
  - WOW64 x86 갈래 (EDI = 5, TEB 뒤 `+0x2000` 에 `_TEB32`) 19줄 `+0x14a ~ +0x1db`.
  - EDI = 6 갈래 17줄 `+0x1e9 ~ +0x27f` (둘째 TEB 쪽).
  - 나머지 1줄.
  - 가르는 곳: `EPROCESS.WoW64Process` (+0x310) = 0 → `R13 = 0` → EDI = 4 (`+0x58 ~ +0x6a`).
- 64비트 갈래 13줄이 쓰는 칸 (칸마다는 짐 지도 3.3):

| 줄 | 칸 | 값 |
|---|---|---|
| `+0xc1` | `NtTib.Version` (+0x20) | `0x1e00` (R8D) |
| `+0xc5` | `NtTib.Self` (+0x30) | TEB 주소 |
| `+0xcd` · `+0xd4` | `ClientId` (+0x48 · +0x40) | `_ETHREAD.Cid` 사본 (`[R14+8]`·`[R14]`) |
| `+0xde` | `TxFsContext` (+0x2e8) | `0xfffe` (`+0xd8`) |
| `+0xe9` · `+0xf3` | `RealClientId` (+0x7e0 · +0x7d8) | `ClientId` 와 같은 값 |
| `+0x100` | `StaticUnicodeString.MaximumLength` (+0x125a) | `0x20a` (`+0xfa`) |
| `+0x10f` | `StaticUnicodeString.Buffer` (+0x1260) | TEB + `0x1268` (`+0x108 LEA`) |
| `+0x11d` | `ProcessEnvironmentBlock` (+0x60) | `EPROCESS.Peb` (`+0x116`) |
| `+0x12a` | `NtTib.StackBase` (+0x8) | `INITIAL_TEB +0x10` (`+0x126`) |
| `+0x132` | `NtTib.StackLimit` (+0x10) | `INITIAL_TEB +0x18` (`+0x12e`) |
| `+0x13a` | `DeallocationStack` (+0x1478) | `INITIAL_TEB +0x20` `AllocatedStackBase` (`+0x136`) |

- `StaticUnicodeString.Length` 는 안 쓴다 (0). `NtTib.ExceptionList` (+0x0) 도 64비트 갈래는 안 쓴다 (WOW64 갈래만 `+0x1d8`).
- `INITIAL_TEB` 는 `RtlCreateUserStack` 이 채운다 (`①A… → PspSetupUserStack (호출 +0x5ea) → RtlCreateUserStack (호출 +0x89) +0x128`). 사진: `StackBase` `0x9b2fd00000`, `DeallocationStack` `0x9b2fc00000` (예약 1MB).
- **`MmCreateTeb +0x175 · +0x17e` 는 `_TEB32.RealClientId`** 다 (`pdb_struct.py ntdll.dll _TEB32`: `+0x6b4 RealClientId`). `RCX = TEB + 0x2000` (32비트 TEB) 이고 WOW64 x86 갈래 안 줄 → **64비트 TEB 칸이 아니고 test.exe 는 안 탄다**. sast/47 이 "`_ETHREAD` 칸이 아니다" 라고 한 줄의 답.
- **`SameTebFlags`·`InitialThread`** — `PspAllocateThread +0x793` (`MOV word [RBX+0x17ee],R12W`) 한 줄. 프로세스에 붙은 채 (`+0x78d KiStackAttachProcess`) 16비트를 통째로 쓴다. 값이 0 이 아닐 때만 (`+0x66f TEST R12W`).
  - 값 계산 `+0x54a ~ +0x5a5`: 만들기 플래그 `0x40` → `0x400` (`InitialThread`, 비트 10) · `0x2` → `0x8` (`SkipThreadAttach`) · `0x80` → `0x2000` (`LoaderWorker`) · `0x100` → `0x4000` (`SkipLoaderInit`).
  - **첫 스레드에선 늘 `0x400` 뿐이다** `[정적]`: `NtCreateUserProcess +0x1bf` (`TEST [RBP+0x7d8],0xfffffffe`) → `+0x1ce JZ` 가 `ThreadFlags` 에 비트 0 말고 다른 비트가 있으면 거절 (`0xc000000d`). `+0xdfd OR [RBP+0x10],0x60` 이 `0x40` 을 항상 더한다. 사진 K2 `0x400`.
- 복제 갈래 (`PspAllocateThread +0x5ab ~ +0x5b3`, 사용자 스택 인자 = 0) 도 `ClientId`·`RealClientId` 를 쓰지만 (`+0x2cab6d`·`+0x2cab79`) 만들어 둔 TEB 를 다시 쓰는 길이고 test.exe 는 안 탄다.
- 칸 수 대조 (결과서): 7.2 `K1~K2 (8)` 이름 여덟 개 — 빠진 이름 0 · 더 들어간 이름 0.
- (메모) 결과서 "한눈에" 셋째 줄의 "8칸 중 7칸은 `MmCreateTeb` …, 나머지 둘 (`SameTebFlags`·`InitialThread`) 은 `+0x793`" 은 합이 9 라 표현이 틀렸다. 요청서 목록으로 세면 `MmCreateTeb` 몫은 **6칸** (`ClientId`·`ProcessEnvironmentBlock`·`TxFsContext`·`RealClientId`·`StaticUnicodeString`·`DeallocationStack`) + `+0x793` 몫 2칸 = 8 (결과서 1번 표와 이 문서는 그대로 맞음, 결과서는 안 고침).

### `P0~P2` 13칸 (질문 2)

- **`P0~P1` 5칸 = 커널, 첫 스레드 시작 때.** `PspUserThreadStartup +0x94 → KeUpdateTebSchedulingPropertiesCurrentThread → (호출 +0x15) KiUpdateTebSchedulingPropertiesCurrentThread`:
  - `+0x136` (`MOV [RCX+0x1744],EAX`) — 이상적 프로세서 4바이트 한 번. 이 4B 가 `CurrentIdealProcessor` = `IdealProcessorValue` 이고, 그 안 바이트가 `ReservedPad0·1` (그룹 2B) · `ReservedPad2` (번호) · `IdealProcessor` (번호 사본). 값: `_KTHREAD.UserIdealProcessor` (+0xc4) → 전역 `KiGlobalState` 표 → 그룹 = 값 >> 6, 번호 = 값 & 0x3f. 사진 P1 `0x01010000` = 그룹 0, 번호 1.
  - `+0x141` (`MOVDQU [RCX+0x1860],XMM0`) — `PrimaryGroupAffinity` 16바이트. `Mask` = `UserAffinity` 비트맵의 주 그룹 8B, `Group` = `UserAffinityPrimaryGroup`. 뒤 `Reserved` 6B 는 이 함수가 안 쓴 스택 자리를 그대로 옮긴다 `[정적]` (사진은 0, 에뮬레이터는 0). 사진 P1 `0x3`.
  - 같은 함수를 `KiUpdateTebApc` (`UpdateTebApc` 의 `NormalRoutine`, run53) 도 부른다 — 친화도가 바뀔 때 다시 쓰는 길. 이 길에선 APC 가 아직 안 걸린다 (Q78d4).
- **`P1~P2` 8칸 = ntdll.**
  - `ThreadLocalStoragePointer` — `LdrpAllocateTls +0x1e2` (`LdrpInitializeTls` 호출 `+0x15d`). `LdrpTlsBitmap ≠ 0` 일 때만 (`+0x43`). test.exe 는 exe TLS 가 있다. 사진 `0x25996f383e0`.
  - `RngState` 앞 16B — `LdrInitializePerThreadRng` 가 `LdrSystemDllInitBlock`·TEB 주소·KUSD 틱·스택 주소를 섞어 씨앗을 만들고 (`+0x4 ~ +0x40`) `RtlInitializeTRng(TEB+0x260, 씨앗)` 으로 꼬리 점프 (`+0x54`). splitmix64 로 8B 둘 (`+0x6`·`+0x54`). 뒤 16B 는 아무도 안 쓴다. 그 뒤 힙 할당기가 갱신한다 (`RtlAllocateHeap +0x229 · +0x234` 등, 이름만).
    - **커널 `_ETHREAD.RngState` (run53, 3.13) 와 이름만 같은 다른 칸이다.**
  - `_ActivationStack` (40B) · `ActivationContextStackPointer` — `RtlpInitializeThreadActivationContextStack` (`RCX` = TEB) `+0x15 ~ +0x47` 이 스택 한 개를 TEB 안에 만들고 `+0x52` 에서 그 주소 (TEB + `0x290`) 를 포인터 칸에. 이미 포인터가 있으면 건너뜀 (`+0x5`).
  - `HeapData` — 한 DWORD 가 아니라 **바이트 넷**을 힙 LFH·VS 함수가 따로 쓴다 (줄 후보 여섯 곳). 어느 줄이 먼저인지는 `[공백]` (D5).
  - `SameTebFlags.LoadOwner` — 켬 `LdrpDrainWorkQueue +0xd3` (run24) / 끔 `LdrpDropLastInProgressCount +0x12`. 사진 P1a 0 · P1b 1 · P2 0 이라 짝은 `+0x1b8c` 켬 → `+0x1be7` 끔 으로 보임 `[추측]` (D6).
  - `LastStatusValue` — `RtlNtStatusToDosError` 가 **입구에서 무조건** 받은 상태를 TEB 에 저장 (`+0xf` `GS:[0x30]` → `+0x1d`). 사진 P2 `0xc0000034`. 누가 불렀나는 `[공백]` (D2).
  - `FlsData` — 처음 `RtlFlsSetValue` 가 불리면 (`TEB+0x17c8 == 0`) 0x58B 블록을 만들어 0 으로 채우고 `+0x104` 에서 TEB 에 놓는다. 사진 P2 `0x25996f3c160` (프로세스 힙 안). 부르는 쪽은 KERNELBASE `FlsSetValue` (사용자 CRT 에서) 로 보임 `[추측]`, 정확한 호출자 `[공백]` (D3). **지도 밖**.
- `*` 칸 (`RngState`·`HeapData`·`LoadOwner`·`SameTebFlags`) 은 처음 값 줄만 닫았다. 뒤에 바꾸는 쪽은 이름만.
- 칸 수 대조 (결과서): 7.2 `P0~P1 (5)` · `P1~P1a (5)` · `P1a~P1b (1)` · `P1b~P2 (2)` 이름 열세 개 — 빠진 이름 0.

### 끝까지 0 — 112칸 (질문 3)

- flow-writer 도 7.2 `끝까지 0` 112 이름과 결과서 3.1 ~ 3.4 를 스크립트로 짝지었다 — 빠진 이름 0 · 더 들어간 이름 0 · 겹침 0.
- **0 을 씀 12**:
  - `SameTebFlags` 의 0 비트 9개 (`SafeThunkCall`·`InDebugPrint`·`HasFiberData`·`WerInShipAssertCode`·`SuppressDebugMsg`·`DisableUserStackWalk`·`RtlExceptionAttached`·`SessionAware`·`SkipFileAPIBrokering`) — `①A… +0x793` 16비트 통째 쓰기의 0 비트 ("입력값 쓰기", run52 DWORD 되쓰기 기준과 같음).
  - `ReservedPad0`·`ReservedPad1` — `①TS… +0x136` 4B 의 그룹 2바이트 = 0.
  - `HeapWalkContext` — `RtlAllocateHeap +0x6a` 가 힙 주소를 넣었다가 `+0x3d2` 에서 0 으로 되돌림 (**쓰고 되돌림**). 사진은 늘 0.
- **`SameTebFlags` 비트를 가른 규칙**: 16비트 모두 `+0x793` 통째 쓰기로 0 이 되지만, **켜는 줄이 초기화 길 안에 따로 있는 비트** (`SkipThreadAttach`·`LoaderWorker`·`SkipLoaderInit` ← `+0x56a`·`+0x587`·`+0x599`, `ClonedThread` ← 복제 갈래 `+0x2cabac ~ +0x2cabb2`) 는 "조건부 안 탐", **켜는 줄이 없는 9비트** 는 "0 을 씀" 으로 셌다 — run52 `MiscFlags` 가름 (되쓰기 `+0xfd` 로 0 이어도 켜는 줄이 따로 있는 `SecureThread`·`SystemThread` 등은 조건부) 과 **같은 기준**. 다른 점은 쓰는 꼴뿐: run52 는 옛 DWORD 를 읽어 몇 비트만 바꿔 되쓰기, 여기는 만들기 플래그로 새로 계산한 16비트를 통째 쓰기 (읽기 없음), 그리고 셋의 켜는 줄은 그 같은 `+0x793` 값을 만드는 계산 줄이다.
- **조건부 안 탐 13** — test.exe 가 왜 안 타는지:

| 칸 | 켜는 줄 | test.exe 가 왜 안 탐 |
|---|---|---|
| `SkipThreadAttach` · `LoaderWorker` · `SkipLoaderInit` | `①A… +0x56a` · `+0x587` · `+0x599` | **코드로 닫힘**: `NtCreateUserProcess +0x1bf` 가 `ThreadFlags` 비트 0 만 허용. KERNELBASE `CreateProcessInternalW +0x6cb` 상수 1. 로더 일꾼 (`NtCreateThreadEx` 길, Q79) 에서만 켜짐 |
| `ClonedThread` · `IsImpersonating` · `MuiImpersonation` | `①A… +0x2cabac ~ +0x2cabb2` · `+0x2cab81` · `+0x2cab87` | 복제 갈래 — 사용자 스택 인자 ≠ 0 → 일반 길 |
| `WowTebOffset` | `①TB… +0x1ca` · `+0x271` | `WoW64Process = 0` (x64 exe) |
| `ExtendedFeatureDisableMask` | `KeCopyXfdMaskToTeb +0x96` ← `KeInitThread +0x49a` | `KeFeatureBits` 비트 `0x37` 등 — **탔는지 / 탔는데 값 0 인지 `[공백]`** (D1, run52 D1 과 같은 읽기) |
| `LastErrorValue` | ntdll 힙 오류 경로들, `RtlRestoreLastWin32Error +0x1c` | 힙 실패 없음 · 값이 다를 때만 씀 |
| `HardErrorMode` | `RtlpCheckRelativeDrive +0x15f` · `+0x1ba` | 드라이브 상대 경로일 때만 — 탔어도 되돌려 0 (D4) |
| `HeapFlsData` | `RtlpFlsSetValue +0xf4` (`.asm` 없음) ← 힙 LFH 스레드 데이터 | 사진 P2 까지 0 → 안 탐 `[관찰 간접]` |
| `TlsSlots` · `TlsExpansionSlots` | `RtlTlsAlloc +0x168` · `+0x1ef` | 초기화 길에서 `RtlTlsAlloc` 이 불리나 `[공백]` (D4) |

- **아무도 안 씀 28**: `EnvironmentPointer`, `CountOfOwnedCriticalSections`, `FpSoftwareStatusRegister`, `SystemReserved1`, `PlaceholderHydrationAlwaysExplicit`, `PlaceholderReserved`, `Padding0 ~ Padding8` (9), `InstrumentationCallbackSp`·`PreviousPc`·`PreviousSp`·`Disabled`, `UnalignedLoadStoreExceptions`, `TlsLinks`, `Vdm`, `Instrumentation`, `ChpeV2CpuAreaInfo`, `Unused`, `CrossTebFlags`, `CrossTebFlags.SpareCrossTebBits`, `EffectiveContainerId`. 값 0 은 새 페이지에서 온다.
  - `Instrumentation*` 5칸은 OS 가 읽기만 하고 사용자가 설치한 계측 콜백이 쓰는 칸으로 보인다 `[추측]`. `Vdm`·`FpSoftwareStatusRegister` 등은 옛 칸, `ChpeV2CpuAreaInfo` 는 ARM64 전용 `[추측]`.
  - `TlsLinks`·`CountOfOwnedCriticalSections`·`Instrumentation`·`EffectiveContainerId` 는 쓸 코드가 있을 법한데 네 모듈에서 못 찾았다 `[공백]` (훑기 한계 안).
- **범위 밖 길에서 씀 `[추측]` 59** (결과서 3.4 표, 함수+오프셋은 거기):

| 쓰는 쪽 | 칸 |
|---|---|
| **ntdll (19)** — 지도 밖 ntdll 함수 | `PlaceholderCompatibilityMode`, `ProxiedProcessId`, `WorkingOnBehalfTicket`, `DbgSsReserved`, `ActivityId`, `SubProcessTag`, `WaitingOnLoaderLock`, `ThreadPoolData`, `MuiGeneration`, `CurrentTransactionHandle`, `ActiveFrame`, `PreferredLanguages`, `UserPrefLanguages`, `MergedPrefLanguages`, `LockCount`, `ResourceRetValue`, `LastSleepCounter`, `SpinCallCount`, `Rcu` |
| **nt (1)** | `ExceptionCode` ← `KeRaiseUserException +0x63` (잘못된 핸들 예외, `.asm` 있음) |
| **다른 DLL (39)** | GDI 6 (`GdiTebBatch` 등), user32 3, win32kbase 1 (`Win32ThreadInfo`), opengl32 8, ws2_32 1, rpcrt4 2, ole32/combase 4, advapi32/sechost 2, CSR 1, WOW64 1 (`WOW32Reserved`), **KERNELBASE 4 (쓰는 줄 확인)**: `StaticUnicodeBuffer`·`NlsCache`·`GuaranteedStackBytes`·`SavedPriorityState`, 이름만 6 (`ReservedForPerf`·`ReservedForCodeCoverage`·`ReservedForWdf`·`ReservedForCrt`·`ReservedForDebuggerInstrumentation`·`pShimData`) |

- 한 칸을 길 안 분류 (0 을 씀 / 조건부) 와 범위 밖이 같이 쓰면 길 안 분류로 셌다 (예: `InDebugPrint` ← `vDbgPrintExWithPrefixInternalHelper`, `HasFiberData` ← KERNELBASE `ConvertThreadToFiber`, `IsImpersonating` ← 커널 `PspWriteTebImpersonationInfo`). 길 안 줄이 없으면 ntdll > nt > 다른 DLL 순으로 하나에만 셌다.

### test.exe 가 탄 길 (갈림길 다섯)

| 갈림길 | 가르는 곳 | test.exe |
|---|---|---|
| WOW64 | `EPROCESS.WoW64Process` (`MmCreateTeb +0x58 ~ +0x6a`, `PspAllocateThread +0x79b`) | 0 → 64비트 갈래 13줄 |
| 첫 스레드 플래그 | `NtCreateUserProcess +0x1bf` (`ThreadFlags` 비트 0 만) · `+0xdfd` (`0x60`) | `SameTebFlags = 0x400` |
| 복제 | 사용자 스택 인자 = 0 (`PspAllocateThread +0x5ab ~ +0x5b3`) | 인자 ≠ 0 → 일반 길 |
| TLS | `LdrpTlsBitmap ≠ 0` (`LdrpAllocateTls +0x43`) | exe TLS 있음 → 벡터 만듦 |
| 활성화 문맥 스택 | TEB 포인터가 이미 있나 (`RtlpInitializeThreadActivationContextStack +0x5`) | 없음 → 만듦 |

- 이 값들은 사진 값으로 맞춰 본 것 `[관찰 간접]`.

### 간접 호출 (질문 4)

- **이 길에서 TEB 칸을 쓰는 간접 호출은 0개** `[정적]`. `.asm` 36개 (커널 14 · ntdll 22) 에서 `CALL reg`·`CALL [mem]`·`JMP reg`·`cfg:` 를 찾아 커널 0개, ntdll 1개: `_LdrpInitialize +0x9a CALL RAX` (`g_LdrpWow64LdrpInitialize`, `UseWOW64 ≠ 0` 갈래 — x64 test.exe 는 안 탄다). → Q35 에 넣을 것 없음.

## 확인된 것

- **`K1~K2` 8칸 모두 커널** — `MmCreateTeb` 64비트 갈래 13줄 + `PspAllocateThread +0x793` `[정적]` · 사진 K2 값과 칸마다 맞음 (짐 지도 3.3)
- **`MmCreateTeb +0x175 · +0x17e` = `_TEB32.RealClientId`** (WOW64 갈래, 안 탐) `[정적]` (오케스트레이터 `.asm` 확인)
- **첫 스레드 `SameTebFlags` = `0x400` 뿐** — 코드로 닫힘 (`NtCreateUserProcess +0x1bf`·`+0xdfd`) `[정적]`
- **`P0~P1` 5칸 = `KiUpdateTebSchedulingPropertiesCurrentThread +0x136 · +0x141`** `[정적]` (오케스트레이터 `.asm` 확인). flow.md 2.3 의 `[추측]` 이 맞았다
- **`P1~P2` 8칸 = ntdll** — `LdrpAllocateTls +0x1e2`, `LdrInitializePerThreadRng ⇒ RtlInitializeTRng`, `RtlpInitializeThreadActivationContextStack`, 힙 LFH·VS, `LdrpDrainWorkQueue +0xd3`, `RtlNtStatusToDosError +0x1d`, `RtlFlsSetValue +0x104` `[정적]`
- **TEB 몸통 `memset` 없음** — 0 은 새 페이지 `[정적]` 부재 + `[추측]` 커밋 방식
- **TEB 칸을 쓰는 간접 호출 없음** `[정적]`
- 끝까지 0 112칸은 0 을 씀 12 · 조건부 안 탐 13 · 아무도 안 씀 28 · 범위 밖 길 59 `[추측]` (ntdll 19 · nt 1 · 다른 DLL 39) 로 갈렸다. 여전히 모름 0

## 정정

- **baggage 3.3 `NtTib` 줄에 커널 쓰는 줄이 빠져 있었다.** `NtTib` 안쪽 `StackBase` (+0x8) · `StackLimit` (+0x10) · `Version` (+0x20 = `0x1e00`) · `Self` (+0x30 = TEB 주소) 를 `MmCreateTeb` 가 쓴다 (`+0x12a · +0x132 · +0xc1 · +0xc5`). 사진 K2 에서 `NtTib` 이 이미 0 이 아닌 것도 이 줄 때문이다. 지금까지 줄은 ntdll `ArbitraryUserPointer` (+0x28) 만 적었다 → 짐 지도 3.3 에 ① nt 줄 하나를 보태고, `NtTib` 은 nt·ntdll 양쪽에 센다 (그래서 `_TEB` "nt 가 씀" 이 +1). 칸 수·아는 칸 수는 그대로 (이미 아는 칸).
- **baggage 3.3 머리 메모 (run52·run53)** 는 `.asm` 으로 맞다. 표로 옮기고 `(해결됨 → run54)` 로 남겼다. 보탬은 위 "첫 스레드는 늘 `0x400`" 과 "`+0x175 · +0x17e` 는 `_TEB32`".
- **flow.md 2.1 `MmCreateTeb` 의 "약 47줄"** 은 결과서가 센 **50줄** (갈래 셋) 과 같은 묶음이다. run11·run43 문장은 "약" 이라 틀리지 않아 이전 run 문서는 안 고쳤다. flow.md 해당 줄에 정확한 수를 보탰다.
- 이전 run 문서 맨 위 정정 안내: **없음** (`NtTib` 줄은 결과서 4번 대조 (run43) 가 옮긴 줄이고, 그 줄 자체는 맞다 — 빠진 줄을 보탠 것).

## 아직 확인 필요

- 동적 (결과서 번호 그대로, D1 ~ D6 — run52·run53 의 D 번호와 다른 질문).
  - **D1** (덤프, **Q79**) `ExtendedFeatureDisableMask` (TEB `+0x1848`) 갈래를 탔나 — `KeFeatureBits` 비트 `0x37` (`dq nt+0xfc6b70 L1`) 과 KUSD `+0x3d8` 비트 18 (`dd 0xfffff780000003d8 L1`). run52 D1 과 **같은 읽기** — 한 번에 `_KTHREAD`·`_TEB` 두 칸이 같이 닫힌다.
  - **D2** (멈춤, **새 대기열 항목** — 오케스트레이터가 plan 에 붙임) TEB `LastStatusValue` = `0xc0000034` 를 쓴 호출자 (P1b ~ P2). `bp ntdll!RtlNtStatusToDosError ".if (@ecx == 0xc0000034) { k; dd @$teb+0x1250 L1 } .else { gc }"`. 조건: 지금 프로세스 test.exe, P1b (`LdrpInitializeProcess +0x1bb6`) 뒤. DLL 시작 코드인가, KERNELBASE `BaseSetLastNTError` 인가. 덤프로는 불가 (콜스택).
  - **D3** (멈춤, 같은 새 항목) TEB `FlsData` 를 처음 놓는 `RtlFlsSetValue` 의 호출자. `bp ntdll!RtlFlsSetValue ".if (poi(@$teb+0x17c8)==0) { k } .else { gc }"`. 조건 test.exe, P1b 뒤. 사용자 CRT → KERNELBASE `FlsSetValue` → ntdll 인가. 덤프로는 불가.
  - **D4** (**Q100** 낮음) 초기화 길에서 `RtlTlsAlloc`·`RtlpHpEnvTlsAlloc` 이 불리나 — `NtContinue` 전까지 hit 수.
  - **D5** (**Q100** 낮음) `HeapData` 바이트를 처음 쓰는 줄 — `ba w1 @$teb+0x17b2` / `+0x17b3` 를 `LdrpInitializeProcess` 입구부터, `r rip`.
  - **D6** (**Q100** 낮음) `LoadOwner` 켬/끔 짝이 `+0x1b8c` ↔ `+0x1be7` 인가 — `ba w2 @$teb+0x17ee` 를 `+0x199d` 부터, `k` 에 `LdrpInitializeProcess+0x1b91` · `+0x1bec`.
- 정적 (넘길 것 — 짐 지도 절 머리 메모):
  - **Q78d4** (3.3·3.13 머리): `KiQueueTebUpdateApc` (rva `0x3bc0f4`) 호출자 `KiSetIdealProcessorThread +0x10e` · `KiSetAffinityThread +0xf1` → `UpdateTebApc` 가 걸리면 `KiUpdateTebApc → KiUpdateTebSchedulingPropertiesCurrentThread` 로 `P0~P1` 다섯 칸을 첫 스레드 시작 뒤에 다시 쓴다 (큐에 넣는 조건·시점). `KeStartThread` 쪽에서 `_KTHREAD.UserAffinity`·`UserAffinityPrimaryGroup`·`UserIdealProcessor` 가 최종으로 정해지는 줄 → `P0~P1` 입력의 마지막 출처. `MmDeleteTeb` (rva `0x976370`) 는 뽑아 두기만 (종료 쪽, Q80 이후).
  - (작은 것) "범위 밖 길" 59·"아무도 안 씀" 28 의 쓰는 함수를 `.asm` 로 한 줄씩 확인. "다른 DLL" 35칸은 이름 짐작이라 DLL 을 뽑아 봐야 한다.
- 칸 표가 없는 짐 후보: 없음 (이번 결과서는 새 후보를 안 냄).

## 그림

- flow.md 그림 (mermaid) 은 안 바꿨다. 새 단계·새 caller 가 없다. `KeUpdateTebSchedulingPropertiesCurrentThread` 는 이미 그림 2 nt 의 노드다 — 그 안 callee (`KiUpdateTebSchedulingPropertiesCurrentThread`) 는 2.3 글 트리에 `·` 줄로만 넣었다 ("그림엔 없음"). 2.4 트리에도 `RtlpInitializeThreadActivationContextStack` 을 `·` 줄로 넣었다.

## 도구 메모

- 결과서 "도구 평가" — 하네스라 오케스트레이터가 사용자에게:
  - **제안 1 (sast/43·44·46·47 이어서)**: `scripts/field_writers.py <바이너리> --struct _TEB` — (a) 분기·레이블 합치기가 있는 레지스터 추적 (선형 훑기는 `pop r14; ret` 뒤 레이블에서 `gs:[0x30]` 를 잃어 `_LdrpInitialize +0xea` 같은 줄을 놓침), (b) 인자로 받은 구조체 포인터 따라가기, (c) `.asm` 이 있으면 그 `+0x` 로 바꿔 쓰기.
  - 제안 2: `pdb_struct.py` 의 겹치는 칸 (`CurrentIdealProcessor`·`IdealProcessorValue`·`ReservedPad0~2`·`IdealProcessor`) 을 "같은 바이트 묶음" 으로 보여 주는 옵션 (sast/47 제안 2 와 같음).
  - 제안 3 (사소): `ExportFunctions` 가 이미 `.asm` 이 있는 함수는 건너뛴다고 알려 주기.
- Ghidra 시작 때 `GhidraMCP/Module.manifest` 경고 한 줄 (사용자 설정 확장 모듈) — 결과 영향 없음.
- 디버거가 붙어서 달라지는 점: 없음 (근거가 정적 + 이미 찍은 사진).

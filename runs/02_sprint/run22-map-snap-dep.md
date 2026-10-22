# 2026-10-23 run22 — `LdrpMapAndSnapDependency`: import 마다 `LdrpLoadDependentModuleInternal`, 끝에 한 번 snap 또는 일감 넘기기. `LdrpAllocateModuleEntry` 는 항목과 그래프 노드를 한 쌍으로 만든다 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntdll (`ntdll.dll`, 26100.9457), pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (run17 ~ run21 과 같은 파일)
  - analyze 는 다시 안 했다. 이미 분석된 프로젝트에서 두 함수를 depth 1 로 뽑았다 (27개).
  - 버전 대조: **일치.** `_calls.tsv` 의 `LdrpLoadDll +0xfb → ret +0x100 LdrpLoadDllInternal`, `LdrpInitializeProcess +0x1bb6 → ret +0x1bbb LdrpInitializeGraphRecurse` 가 run7·run8 콜스택 값과 같다. 지도의 `LdrpInitializeProcess +0x1aa4 → ret +0x1aa9`, `LdrpMapDllWithSectionHandle +0x1c3 → ret +0x1c8` 도 같다.
- 근거로 읽은 것
  - 본문: `LdrpMapAndSnapDependency` (rva `0x210a0`, 1544바이트, 직접 호출 23줄), `LdrpAllocateModuleEntry` (rva `0x6fab0`, 412바이트, 직접 호출 5줄).
  - 확인용 (`.asm` 만): `LdrpMapDllWithSectionHandle` (depth 0, "누가 이 함수를 다시 부르나" 를 보려고).
  - 줄만 본 것 (본문 순서는 Q12b 몫): `LdrpLoadDependentModuleInternal` `+0x10a3 ~ +0x1164`·`+0x12df ~ +0x12f7`·`+0x1606 ~ +0x1634`, `LdrpFindOrPrepareLoadingModule` `+0x3cb ~ +0x4a0`, `LdrpLoadDll`·`LdrpLoadDllInternal` 의 인자 줄. 로드 문맥 `+0x30` 칸이 무엇인지 정하려고.
  - `.c` 만 읽은 callee (안쪽 callee 는 안 봄): `LdrpPrepareImportAddressTableForSnap`, `LdrpGetImportDescriptorForSnap`, `LdrpShouldModuleImportBeRedirected`, `LdrpFindDllActivationContext`, `LdrpQueryCurrentPatch`.
  - 구조체 칸 이름은 PDB 타입(`llvm-pdbutil`)으로: `_LDR_DATA_TABLE_ENTRY`(크기 0x138), `_LDR_DDAG_NODE`(크기 0x50), `_LDR_DDAG_STATE`, `_LDR_DLL_LOAD_REASON`.
  - **`_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없다** (앞선 선언만). 그래서 로드 문맥 칸은 아래에서 **오프셋 + 역할** 로만 쓴다. 칸 이름은 붙이지 않는다.

## 목적

- 대기열 **Q12a**. flow.md 2.6 `[공백]` "import DLL 이 메모리에 올라오는 순간" 의 바깥 고리를 코드로 읽는다.
  - exe 의 import 를 어떤 순서로 도나, 하나마다 무엇을 부르나.
  - "누가 누구에 기대나"(그래프 간선)를 적는 자리가 여기인가.
  - 같이 올리기 vs 일감으로 넘기기가 어디서 갈리나.
  - 그래프 노드는 어디서 태어나나 (`LdrpAllocateModuleEntry`).
- 덤으로: 2.5 kernelbase 가 kernel32 와 **같은 호출 안에서** 따라 올라오는 길을 이 함수 기준으로 다시 본다.

## 방법

- 정적: `_workspace/sast/17_map-snap-dep.md` — ntdll, `^(LdrpMapAndSnapDependency|LdrpAllocateModuleEntry)$` depth 1 (+ `LdrpMapDllWithSectionHandle` depth 0)
- 결과서: `_workspace/sast/17_map-snap-dep_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntdll/{LdrpMapAndSnapDependency,LdrpAllocateModuleEntry,LdrpMapDllWithSectionHandle}.asm`
  - `ms/disassemble/ntdll/_calls.tsv`
- 오프셋 읽는 법 (run9 ~ 21 과 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**. 메모리에 쓰는 줄(`MOV [..],..`)은 그 명령 자리.
  - 결과서는 몇 곳(`LdrpMapDllWithSectionHandle +0x1c3`·`+0x2d0`·`+0x3be`, `LdrpLoadDependentModuleInternal +0x1157` 등)을 call 자리로 적었다. 이 문서는 `_calls.tsv` 로 ret 자리를 맞춰 적는다 (아래 표).

## 결과

한 줄로: **`LdrpMapAndSnapDependency` 는 "한 모듈의 import 표를 읽어, 기대는 DLL 하나하나를 `LdrpLoadDependentModuleInternal` 에 맡기고, 맡긴 게 다 끝났으면 그 모듈의 주소 채우기(snap)를 한 번 한다" 는 함수다.** snap 을 직접 할지 일감으로 넘길지는 "이 모듈을 import 한 부모가 있나" 하나로 갈린다.

- 비유: 이삿짐 목록표 확인. 목록(import 표)을 한 줄씩 읽으며 "이 짐 가져와" 를 심부름꾼(`LdrpLoadDependentModuleInternal`)에게 시킨다. 다 왔으면 마지막에 한 번 전기·수도를 잇는다(snap). 내가 맨 위 집(exe)이면 직접 잇고, 누군가의 짐으로 딸려 온 집이면 "이어 달라" 는 쪽지를 일감 상자에 넣는다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

`LdrpMapAndSnapDependency` (23줄). 잡일(이름 문자열 8곳, 쿠키 1곳)은 아래 따로.

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpMapAndSnapDependency` | `+0x5e` | `+0x63` | `LdrpFindDllActivationContext(항목)` — 문맥 `+0x20 & 0x800000` 이 꺼져 있고 항목 `LoadReason ≠ 9` 일 때 | [정적] |
| 〃 | `+0x99` | `+0x9e` | `LdrpPrepareImportAddressTableForSnap(문맥)` — IAT 를 문맥 `+0x70/+0x78` 에, 쓰기 가능으로 | [정적] |
| 〃 | `+0x109` | `+0x10e` | `LdrpQueryCurrentPatch(DllBase, &바이트)` — 전역 `LdrpIsHotPatchingEnabled` 가 켜졌을 때만 | [정적] |
| 〃 | `+0xcd` | `+0xd2` | `LdrpShouldModuleImportBeRedirected(항목)` — 참이면 문맥 `+0x20 \|= 0x2000000` | [정적] |
| 〃 | `+0xe0` | `+0xe5` | `LdrpGetImportDescriptorForSnap(문맥)` — import 서술자 배열 | [정적] |
| 〃 | `+0x176` | `+0x17b` | `RtlAllocateHeap(LdrpHeap, .., 서술자 수 × 8)` → 문맥 `+0x58` (자식 항목 칸 배열) | [정적] |
| 〃 | `+0x349` | `+0x34e` | **`LdrpLoadDependentModuleInternal`** (6 인자) — import 마다, 조건 없음 | [정적] |
| 〃 | `+0x5d3` | `+0x5d8` | `LdrpLoadDependentModuleW` — 핫패치 바이트가 1 일 때만 (고리 밖) | [정적] |
| 〃 | `+0x5ff` | `+0x604` | `LdrpLogEtwHotPatchStatus` — 위 호출이 실패했을 때 로그 | [정적] |
| 〃 | `+0x44c` | `+0x451` | `RtlFreeHeap(LdrpHeap, 0, 임시)` — 임시 포인터가 0 이 아닐 때 | [정적] |
| 〃 | `+0x45c` / `+0x478` | `+0x461` / `+0x47d` | `RtlAcquireSRWLockExclusive` / `RtlReleaseSRWLockExclusive` (`LdrpModuleDatatableLock`) | [정적] |
| 〃 | `+0x4bd` | `+0x4c2` | **`LdrpSnapModule(문맥)`** — 남은 자식 0 이고 문맥 `+0x30 == 0` | [정적] |
| 〃 | `+0x4f2` | `+0x4f7` | **`LdrpQueueWork(문맥)`** — 남은 자식 0 이고 문맥 `+0x30 ≠ 0` | [정적] |
| 〃 | `+0x79` | `+0x7e` | `__security_check_cookie` (끝나는 길 하나로 모임) | [정적] |

- 이름 문자열 잡일 8곳 (import 이름 ANSI → UTF-16): ret `+0x213 strlen`, `+0x258 memset`, `+0x315 RtlAnsiStringToUnicodeString`, `+0x3ce RtlUTF8ToUnicodeN`, `+0x524 NtdllpReallocateStringRoutine`, `+0x541`, `+0x567`, `+0x36e`.
  - `+0x541` = 문자열 버퍼 잡는 함수. 같은 주소 이름: `RtlpCtAllocateMemory`·`RtlpAllocateAtom`·`NtdllpAllocateStringRoutine`.
  - `+0x567` = 메모리 복사. 같은 주소(rva `0x166500`) PDB 이름 2개: `memcpy`·`memmove` (`pdb_aliases.py --lookup ntdll.dll 0x166500`). `_calls.tsv` 의 Ghidra 표기 `RtlCopyMemory`·`RtlMoveMemory` 는 PDB 목록에 없다.
  - `+0x36e` = 문자열 버퍼 놓는 함수. 같은 주소(rva `0x3d630`, run20 의 `LdrpLoadDll +0x114` 와 같은 곳) 이름 9개, 후보: `RtlpFreeEnvBlock`·`RtlpFreeAtom`·`IdnaMemFree`·`NtdllpFreeStringRoutine`·`RtlpCtFreeMemory` 등.

`LdrpAllocateModuleEntry` (5줄)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpAllocateModuleEntry` | `+0x29` | `+0x2e` | `RtlAllocateHeap(LdrpHeap, .., 0x138)` → **모듈 항목** (`_LDR_DATA_TABLE_ENTRY` 크기와 같음) | [정적] |
| 〃 | `+0x5b` | `+0x60` | `RtlAllocateHeap(LdrpHeap, .., 0x50)` → **그래프 노드** (`_LDR_DDAG_NODE` 크기와 같음) | [정적] |
| 〃 | `+0xb6` | `+0xbb` | `RtlAddRefActivationContext` — 문맥 `+0x30`(부모)이 있고 부모 `+0x88` 이 0 이 아닐 때 | [정적] |
| 〃 | `+0x166` | `+0x16b` | `RtlGetActiveActivationContext(&항목+0x88)` — 문맥 `+0x30 == 0` 일 때 | [정적] |
| 〃 | `+0x156` | `+0x15b` | `RtlFreeHeap(항목)` — 노드 할당 실패 때 | [정적] |

이 함수를 부르는 곳 (caller 6곳, `_calls.tsv` ret 자리)

| caller | call | ret | 무엇의 항목 | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x1359` | `+0x135e` | exe (문맥 = 스택 `[RSP+0x2b0]`, run19) | [정적] |
| 〃 | `+0x1577` | `+0x157c` | AppVerifier 복제 (run19) | [정적] |
| `ntdll!LdrpLoadDependentModuleInternal` | `+0x1157` | `+0x115c` | 의존 DLL (힙에 새 문맥을 만든 뒤) | [정적] |
| `ntdll!LdrpFindOrPrepareLoadingModule` | `+0x475` | `+0x47a` | `LdrLoadDll` 길 DLL (kernel32 등) | [정적] |
| `ntdll!LdrpInitializeNtdllDataTableEntry` | — | `+0x67` | ntdll (인자 줄은 안 봄) | [정적] |
| `ntdll!LdrpAllocatePlaceHolder` | — | `+0xd0` | (인자 줄은 안 봄) | [정적] |

이 함수를 다시 부르는 곳 (`LdrpMapDllWithSectionHandle`, depth 0 `.asm`)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x1c3` | `+0x1c8` | `LdrpMapAndSnapDependency(문맥)` — 항목 `Flags` 비트 24(`CorILOnly`)가 꺼져 있을 때 | [정적] |
| 〃 | `+0x2d0` | `+0x2d5` | `LdrpCorProcessImports` — `CorILOnly` 가 켜져 있을 때 (위 대신) | [정적] |
| 〃 | `+0x3be` | `+0x3c3` | `LdrpLoadContextReplaceModule` — 같은 모듈이 이미 목록에 있을 때 (위 둘 안 탐) | [정적] |

### 정적 — 로드 문맥 칸 (오프셋 + 역할만, 칸 이름 없음)

PDB 에 칸 정보가 없다. 역할은 코드가 쓰고 읽는 줄에서 읽은 것이다. 줄만 보고 짐작한 칸은 `[추측]`.

| 칸 | 역할 | 근거 | 표시 |
|---|---|---|---|
| `+0x00` | 모듈 이름 (`UNICODE_STRING`) | exe: `LdrpInitializeProcess +0x133b`. 의존 DLL: `LdrpLoadDependentModuleInternal +0x1121 ~ +0x1150` 이 새 문맥에 복사 | [정적] |
| `+0x10` | 경로 구조체 포인터 (그 `+0x18` 이 항목 `ImplicitPathOptions` 로 감) | `LdrpAllocateModuleEntry +0x86 ~ +0x8d` | [정적] |
| `+0x20` | 플래그 (`0x800000`·`0x2000000`·비트 0·`0x200`·`0x8000`·`4`·`0x40`·비트 27 을 읽거나 씀). exe 처음 값 `0x200` | `MapAndSnap +0x33`·`+0xd6`, `Allocate +0x107 ~ +0x129`, `LdrpInitializeProcess +0x1328` | [정적] (비트 뜻은 `[추측]`) |
| `+0x28` | 결과 상태(`NTSTATUS`)를 써 넣을 곳의 포인터 | `MapAndSnap +0x6d`·`+0x417`, `LdrpMapDllWithSectionHandle +0x1c8` 이 읽어 돌려줌 | [정적] |
| **`+0x30`** | **부모 항목** = 이 DLL 을 import 한 쪽의 `_LDR_DATA_TABLE_ENTRY*`. exe 길·`LdrLoadDll` 길은 0 | 아래 "부모 정보" | [정적] |
| `+0x38` | 이 문맥의 모듈 항목 | `Allocate +0x96` 이 씀 | [정적] |
| `+0x50` | 자식 항목을 써 넣는 칸 | `LdrpLoadDependentModuleInternal +0x1617` (줄만) | [추측] |
| `+0x58` | 자식 항목 칸 배열 (서술자 수 × 8바이트) | `MapAndSnap +0x17b` | [정적] |
| `+0x60` | 핫패치 DLL 항목을 돌려받는 칸 | `MapAndSnap +0x5c2 ~ +0x5d3` | [정적] |
| `+0x68` | import 서술자 수 | `MapAndSnap +0x19d` | [정적] |
| `+0x6c` | **아직 안 끝난 자식 수 + 1** (아래 끝맺음) | `MapAndSnap +0x199`·`+0x46d ~ +0x474`, `LdrpLoadDependentModuleInternal +0x12df ~ +0x12f7` 이 줄임 | [정적] (값 이름은 `[추측]`) |
| `+0x70` / `+0x78` | IAT(import 주소 표) 시작 / 크기 | `LdrpPrepareImportAddressTableForSnap` (`.c`) | [정적] |
| `+0x88` | import 서술자 배열 포인터 | `MapAndSnap +0x1a1` | [정적] |
| `+0x90` | `NtProtectVirtualMemory` 가 돌려준 원래 보호값 | `LdrpPrepareImportAddressTableForSnap` (`.c`) | [정적] |
| `+0xb8` | -1 로 시작 (`LdrpLoadDependentModuleInternal +0x1113`, `LdrpFindOrPrepareLoadingModule +0x435`) | 뜻 모름 | [확인 필요] |

- 항목 쪽 짝: 항목 `+0xb0 LoadContext` = 문맥 (`Allocate +0x7f`), 문맥 `+0x38` = 항목 (`Allocate +0x96`). **서로 가리킨다.** → run19·run21 에서 `[추측]` 으로 둔 "로드 문맥 ↔ exe 항목" 관계가 이것이다 `[정적]`.

### 정적 — `LdrpMapAndSnapDependency` 순서

입구 인자는 로드 문맥 하나(`RCX`). 반환값이 없고 결과는 문맥 `+0x28` 이 가리키는 칸으로 나간다. `+0x2c` 에서 `R13` = 문맥 `+0x38`(이번에 snap 할 모듈 항목).

```
LdrpMapAndSnapDependency(문맥)                         E = 문맥+0x38 (모듈 항목)
├─ +0x63   LdrpFindDllActivationContext(E)              [문맥+0x20 & 0x800000 꺼짐, E.LoadReason ≠ 9 일 때]
├─ +0x9e   LdrpPrepareImportAddressTableForSnap(문맥)   IAT → 문맥+0x70/+0x78, 쓰기 가능으로 바꿈
├─ +0x10e  LdrpQueryCurrentPatch(E.DllBase, &바이트)    [LdrpIsHotPatchingEnabled 켜졌을 때만]
├─ [IAT == 0 이고 바이트 == 0] → 끝맺음으로 건너뜀
├─ +0xd2   LdrpShouldModuleImportBeRedirected(E)        참이면 문맥+0x20 |= 0x2000000
├─ +0xe5   LdrpGetImportDescriptorForSnap(문맥)         import 서술자 배열
├─ 세는 고리 +0x122 ~ +0x14d (호출 없음)                 서술자 수, 실제로 올릴 수(IAT 첫 칸 ≠ 0)
├─ [올릴 수 == 0 이고 바이트 == 0] → 끝맺음으로 건너뜀
├─ +0x17b  RtlAllocateHeap → 문맥+0x58 (자식 칸 배열)   문맥+0x6c = 올릴 수 + 1
├─ ★ import 마다 도는 고리 +0x1d0 ~ +0x3a3 (서술자 순서)
│    ├─ 이름 문자열 만들기 (잡일)
│    ├─ +0x34e  LdrpLoadDependentModuleInternal(이름, 문맥, E, 0, &문맥.58[i], &임시)   ← 매 바퀴, 조건 없음
│    └─ 실패면 칸 = 0, 상태 기록, 고리 탈출
├─ +0x5d8  LdrpLoadDependentModuleW(.., 문맥+0x60, ..) [바이트 ≠ 0 일 때만]
├─ +0x451  RtlFreeHeap(임시)
├─ +0x461 ~ +0x47d  락 안에서 문맥+0x6c 를 1 줄임 (이 함수 자기 몫)
└─ 끝맺음
     IAT == 0           → 노드 State = 5 (Snapped, +0x5ae) 만 쓰고 끝
     남은 수 ≠ 0         → 아무것도 안 하고 끝 (+0x49a)
     남은 수 == 0        → 노드 State = 4 (Snapping, +0x4ac) →
                            문맥+0x30 == 0 ? +0x4c2 LdrpSnapModule(문맥) : +0x4f7 LdrpQueueWork(문맥)
```

- **import 고리** `+0x1d0 ~ +0x3a3` `[정적]`: 서술자(0x14 바이트씩)를 차례로 돈다. IAT 첫 칸이 0 인 서술자는 건너뛰지만 칸 번호는 올라간다. 이름이 `0xfffe` 바이트보다 길면 `STATUS_NAME_TOO_LONG`(`0xc0000106`) 으로 고리를 나간다.
- **`LdrpLoadDependentModuleInternal` 은 갈림 없이 매 바퀴 직접 부른다** `[정적]`. 병렬 로딩이 켜졌는지 읽는 줄이 이 함수에 없다. 이 함수가 읽는 전역은 `LdrpIsHotPatchingEnabled`, `NtdllBaseTag`, `LdrpHeap`, `GlobalRtlNlsState`, `LdrpModuleDatatableLock`, `LdrpImageEntry`, `__security_cookie` 뿐이다.
- **snap 은 고리 밖에서 한 번** `[정적]`. import 하나마다가 아니다.
- **IAT 를 먼저 쓰기 가능으로 바꾼다** `[정적, .c]`: `LdrpPrepareImportAddressTableForSnap` 이 IAT 디렉터리(12번)를 찾고, 문맥 `+0x20 & 0x800000` 이 꺼져 있으면 `NtProtectVirtualMemory(.., 4, ..)` 로 바꾼다. 4 = 읽기·쓰기 페이지 `[추측: 공개 상수]`.
- **핫패치·리다이렉션 갈래** `[정적]`: 둘 다 전역 값으로 갈린다. `LdrpIsHotPatchingEnabled`(`ntdll+0x1d26f0`) 가 0 이면 `+0x10e`·`+0x5d8` 을 안 탄다. `LdrpRedirectionModule`(`ntdll+0x1d2600`) 이 0 이면 `+0xd2` 가 거짓. test.exe 가 이 갈래를 안 탈 것은 `[추측]` (값은 동적 Q46).
- **락 짝 안에서 하는 일은 `+0x6c` 1 줄이기 하나뿐** `[정적]`. "자식 수 + 1" 의 +1 을 이 함수가 놓는 것으로 읽힌다 `[추측: 값 이름]`.

### 정적 — 그래프 간선("누가 누구에 기대나")을 적는 자리

- **이 두 함수 안에는 없다** `[정적]`.
  - `LdrpMapAndSnapDependency` 가 노드에 쓰는 줄은 `State`(`+0x38`) 두 곳뿐 (`+0x4ac` → 4, `+0x5ae` → 5). `Dependencies`(`+0x28`)·`IncomingDependencies`(`+0x30`)·`Modules`(`+0x00`) 에 쓰는 줄이 없다.
  - `LdrpAllocateModuleEntry` 도 `Dependencies`·`IncomingDependencies` 에 쓰는 줄이 없다.
  - → 간선을 적는 곳은 `LdrpLoadDependentModuleInternal` 안일 것 `[추측]` (Q12b).
- **부모 정보가 넘어가는 자리** `[정적: +0x33e ~ +0x349]` — `+0x34e LdrpLoadDependentModuleInternal` 의 인자:

| 인자 | 값 | 역할 |
|---|---|---|
| 1 `RCX` | 지역 문자열 | 자식(import 되는 쪽) 이름, UTF-16 |
| 2 `RDX` | 문맥 | **부모(import 하는 쪽)의 로드 문맥** |
| 3 `R8` | `R13` = 문맥 `+0x38` | **부모 모듈 항목** |
| 4 `R9D` | 0 | 올리는 이유 = `LoadReasonStaticDependency`(0). `Internal +0x1179` 가 새 항목 `LoadReason` 에 씀 (줄만) |
| 5 `[RSP+0x20]` | `&문맥.58[i]` | 자식 항목을 돌려받는 칸 |
| 6 `[RSP+0x28]` | `&[RSP+0x48]` | 임시 포인터 (끝에 `+0x451` 에서 놓음) `[추측]` |

- **문맥 `+0x30` = 부모 항목** 이라는 근거 `[정적]`:
  - `LdrpLoadDependentModuleInternal +0x1100 ~ +0x1108`: 새 자식 문맥 `+0x30` 에 세 번째 인자(부모 항목)를 넣는다.
  - `LdrpAllocateModuleEntry +0x9a ~ +0xbb`: 문맥 `+0x30` 이 있으면 그 항목의 `EntryPointActivationContext`(`+0x88`)를 물려받는다.
  - `LdrpMapDllWithSectionHandle +0x286 ~ +0x28f`: 문맥 `+0x30` 이 있으면 자식 항목 `ParentDllBase`(`+0xb8`, PDB 이름) = 부모 `DllBase`.
  - `LdrpLoadDll +0xf6` 은 이 칸으로 갈 값에 0 을 넣는다 → `LdrLoadDll` 길(kernel32·kernelbase 첫 로드 요청)은 0. exe 문맥은 `LdrpInitializeProcess` 앞쪽 `memset(.., 0, 0xd0)` (call `+0x105`, ret `+0x10a`) 으로 0 이 되고, 뒤쪽 `+0x133b ~ +0x1349` 는 이름·경로·`0x200`·상태 포인터만 채운다 (결과서).

### 정적 — 같이 올리기 vs 일감으로 넘기기

- **`LdrpLoadDependentModuleInternal` 과 `LdrpQueueWork` 는 같은 선택지의 두 갈래가 아니다** `[정적]`.
  - 자식 DLL 을 맡기는 건 늘 `+0x34e` 직접 호출이다.
  - `+0x4f7 LdrpQueueWork` 는 "**이 모듈 자신의 snap** 을 일감으로 넘길까" 에만 나온다.
- 문맥별로 끝맺음에서 무엇을 부르나 (남은 자식 수 0 일 때, 정적 조건까지):

| 문맥 | `+0x30` | 끝맺음 |
|---|---|---|
| exe (`LdrpInitializeProcess +0x1aa4` → ret `+0x1aa9`) | 0 | `+0x4c2 LdrpSnapModule` 직접 |
| kernel32 (`LdrLoadDll` → `LdrpFindOrPrepareLoadingModule` 문맥) | 0 | `+0x4c2 LdrpSnapModule` 직접 |
| kernel32 의 자식 (kernelbase 등, `Internal` 이 만든 문맥) | kernel32 항목 | `+0x4f7 LdrpQueueWork` |
| exe 의 자식 dll1, 그 자식 dll2 | exe 항목 / dll1 항목 | `+0x4f7 LdrpQueueWork` |

- **병렬 로딩 켜기(`LdrpInitializeProcess +0x1a42`) 앞뒤 차이는 이 함수 코드에 없다** `[정적]`. kernel32 준비 때(켜기 앞)와 exe import 때(켜기 뒤)에 같은 코드가 돈다. 다른 건 문맥 `+0x30` 값, 그리고 `LdrpQueueWork` 본문이 일감을 어디서 돌리나 (본문 안 읽음 → Q47·Q20).
  - 지도: `LdrpQueueWork +0x82` → `TpPostWork`, `LdrpProcessWork +0x131` → `LdrpSnapModule` (이름·호출 자리만).
- `LdrpMapDllWithSectionHandle` 쪽도 병렬 여부를 읽지 않는다 `[정적]`.

### 정적 — `LdrpAllocateModuleEntry`: 항목 + 노드 한 쌍

- **모듈 항목(0x138)과 그래프 노드(0x50)를 같이 만든다** `[정적]`. 노드 할당이 실패하면 항목도 놓고 0 을 돌려준다.
- 처음 값:

| 대상 | 칸 (PDB) | 값 | 표시 |
|---|---|---|---|
| 항목 | `+0x98 DdagNode` | 새 노드 (`+0x73`, 문맥이 NULL 이어도) | [정적] |
| 노드 | `+0x00 Modules` | 항목 `+0xa0 NodeModuleLink` 하나만 든 원형 목록 | [정적] |
| 항목 | `+0x70 HashLinks` | 빈 목록 (자기 자신) | [정적] |
| 노드 | `+0x18 LoadCount` | 1 (`+0xf4`) | [정적] |
| 항목 | `+0x114 ReferenceCount` | 2 (`+0xfb`) | [정적] |
| 항목 | `+0x6c ObsoleteLoadCount` | 6 (`+0x130`) | [정적] |
| 노드 | `+0x38 State` | **쓰는 줄 없음** → 0 = `PlaceHolder` 일 것 | [정적: 쓰는 줄 없음] / 0 은 `[추측]` |
| 노드 | `+0x28 Dependencies`, `+0x30 IncomingDependencies` | **쓰는 줄 없음** → 비어 있을 것 | [정적: 쓰는 줄 없음] / 비어 있음은 `[추측]` |

  - 두 할당 모두 힙 플래그에 `| 8` 이 붙는다. 이걸 "0 으로 채워 달라" 비트로 읽은 것이 `[추측: 공개 상수 이름]` 이다. 위 State·Dependencies "0" 은 **이 추측에 기댄다.**
- 문맥이 NULL 이 아닐 때 (`+0x7a`) `[정적]`:
  - 항목 `+0xb0 LoadContext` = 문맥, 문맥 `+0x38` = 항목 (서로 가리키기).
  - 항목 `+0x110 ImplicitPathOptions` = 문맥 `+0x10` 이 가리키는 구조체의 `+0x18`.
  - 문맥 `+0x20` 비트를 항목 `Flags` 로 옮긴다: 비트 2 → `Redirected`, 비트 6 이 꺼져 있으면 `ImageDll`, 비트 27 → `ChpeEmulatorImage`.
  - `LdrInitState == 1` 일 때만 (`+0xc2`): 현재 스레드가 `ntdll+0x1cc920` 칸(`LdrpDllNotificationLock+0x10`)의 스레드와 **다르면** `Flags |= 0x20`(`ProcessStaticImport`, `+0x194`).
- 활성화 컨텍스트 `[정적]`:
  - 부모가 있고(`+0x30 ≠ 0`) 부모 `+0x88` 이 0 이 아니면 부모 것을 `+0xbb RtlAddRefActivationContext` 로 물려받는다.
  - 부모가 없으면(`+0x30 == 0`, exe·`LdrLoadDll` 길) `+0x16b RtlGetActiveActivationContext` 로 지금 것을 가져오고 **항목 `LoadReason`(`+0x10c`) = 4**(`DynamicLoad`).
  - `LdrpInitializeProcess` 에는 `+0x10c` 를 쓰는 줄이 없다 (`grep` 0건) → exe 항목 `LoadReason` 은 4 로 남는 것으로 읽힌다 `[정적]`. 실제 값은 동적.
- caller 쪽: `LdrpLoadDependentModuleInternal +0x115c` 는 바로 앞 `+0x10db` 에서 힙에 새 문맥을 만들고, 플래그(비트 15 켬)·경로·상태 포인터·**`+0x30` = 부모 항목**·`+0xb8` = -1·이름을 채운 뒤 부른다. 돌려받은 항목은 인자 5(`&부모문맥.58[i]`)에 넣는다 (`+0x1164`).
  - → "**새 DLL 을 만나면 문맥 + 항목 + 노드를 만든다**" 가 코드 모양으로 맞다 `[정적]`. 이 갈래가 "아직 안 올라온 DLL 일 때만" 인지는 본문 순서를 안 봐서 `[추측]` (Q12b).
  - `LdrpFindOrPrepareLoadingModule +0x47a` 도 같은 모양. 돌려받은 뒤 `+0x487` 에서 `LoadReason` = 넷째 인자, 9 면 `HotPatchState` = 1.

### 정적 — test.exe → dll1 → dll2 이야기

정적으로 말할 수 있는 만큼. 모르는 칸은 `?`. 화살표 사이에 다른 함수가 더 있을 수 있다 (`LdrpLoadDependentModuleInternal` 본문은 안 읽음).

```
exe문맥 (+0x30 = 0, +0x38 = exe 항목)
LdrpMapAndSnapDependency(exe문맥)                                  ← LdrpInitializeProcess +0x1aa9
  IAT 쓰기 가능 · import 서술자 읽기
  고리 한 바퀴 (import = dll1.dll [소스])
    +0x34e LdrpLoadDependentModuleInternal("dll1.dll", exe문맥, exe 항목, 0, &exe문맥.58[i])
      ├ 새 dll1문맥 (+0x30 = exe 항목) → +0x115c LdrpAllocateModuleEntry → dll1 항목 + 노드 (State 0 ?)
      ├ ? 간선을 적는 줄 (Q12b)
      └ ? … LdrpMapDllWithSectionHandle   (+0x28f: dll1.ParentDllBase = exe.DllBase)
            └ +0x1c8 LdrpMapAndSnapDependency(dll1문맥)                ← 같은 함수가 dll1 에 대해
                 고리 한 바퀴 (import = dll2.dll [소스])
                   +0x34e LdrpLoadDependentModuleInternal("dll2.dll", dll1문맥, dll1 항목, 0, ..)
                     ├ 새 dll2문맥 (+0x30 = dll1 항목), dll2 항목 + 노드
                     └ ? … LdrpMapDllWithSectionHandle
                           └ +0x1c8 LdrpMapAndSnapDependency(dll2문맥)
                                 (dll2 의 import ?)  남은 수 0 이면 State = 4
                                 +0x30 ≠ 0 → +0x4f7 LdrpQueueWork(dll2문맥)
                 남은 수 = ?  0 이면 State = 4 → +0x30 ≠ 0 → +0x4f7 LdrpQueueWork(dll1문맥)
  남은 수 = ?  0 이면 State = 4 → +0x30 == 0 → +0x4c2 LdrpSnapModule(exe문맥)
```

- `?` 로 남긴 것: `LdrpLoadDependentModuleInternal` 이 어느 길(이미 있음 / KnownDll / 새로 올리기)로 `LdrpMapDllWithSectionHandle` 에 닿나, 각 시점의 남은 수(`+0x6c`)가 0 인가, 일감이 어느 스레드에서 도나, dll2 의 import 가 무엇인가 (dll2 import 표를 안 봤다).
- 지도 이름 사슬 (run21 과 같음, 라벨 ret): 파일 길 `LdrpLoadDependentModuleInternal +0x1cae LdrpProcessWork` → `+0x6a LdrpMapDllSearchPath` → `+0x25c LdrpMapDllNtFileName` → `+0x28c LdrpMapDllWithSectionHandle` → `+0x1c8 LdrpMapAndSnapDependency`. KnownDll 길 `Internal +0x1792 LdrpLoadKnownDll` → `+0xf1 LdrpMapDllWithSectionHandle`.
- run8 `[관찰]` 과 어긋나지 않는다: "`+0x1bb6` 에서 dll1·dll2 가 이미 올라와 있었다". 이 고리는 `+0x1aa9` 전에 끝나고, 그 뒤 `+0x1ab1 LdrpDrainWorkQueue(1)` 이 일감을 기다린다 (run19).

### 정적 — kernelbase 가 같은 `LdrLoadDll` 안에서 따라 올라오는 길 (2.5)

- run21 은 "따라 올라올 길이 지도에 있다" 까지였다. 이번엔 이 함수 기준으로 한 칸 더 말할 수 있다 `[정적]`:
  - kernel32 문맥(`+0x30 = 0`)으로 `LdrpMapDllWithSectionHandle +0x1c8 LdrpMapAndSnapDependency` → 고리에서 kernel32 의 import 마다 `+0x34e LdrpLoadDependentModuleInternal(이름, kernel32문맥, kernel32 항목, 0, ..)`.
  - kernelbase 가 kernel32 의 import 이면 이 고리의 한 바퀴다 `[추측: kernel32 import 표를 안 봄]`. kernelbase 문맥은 `+0x30` = kernel32 항목이라, 끝맺음은 `+0x4f7 LdrpQueueWork`.
  - kernel32 자신은 `+0x30 = 0` 이라 남은 수 0 이면 `+0x4c2 LdrpSnapModule` 직접.
- 그래서 **kernel32 준비 때(병렬 로딩 켜기 앞)에도 `LdrpQueueWork` 를 타는 코드 길이 있다** `[정적: 코드 모양]`. 그때 일감이 어디서 도는지는 모른다 (Q47·Q20).

### 호출 관계

```
LdrpInitializeProcess
 └─ (9) +0x1aa9  LdrpMapAndSnapDependency [test.exe]                          [정적] run19·run22
      ├─ (1) +0x9e   LdrpPrepareImportAddressTableForSnap                        [정적]
      ├─ (2) +0xe5   LdrpGetImportDescriptorForSnap                              [정적]
      ├─ (3) +0x34e  LdrpLoadDependentModuleInternal [dll1 추정]  import 마다   [정적]
      │      ├─ +0x115c  LdrpAllocateModuleEntry [dll1 추정]  항목 + 노드         [정적]
      │      └─ ?  LdrpMapDllWithSectionHandle [dll1 추정]                        [추측]
      │            └─ +0x1c8  LdrpMapAndSnapDependency [dll1 추정]               [정적]
      │                 ├─ (1) +0x34e  LdrpLoadDependentModuleInternal [dll2 추정]
      │                 │      └─ ?  LdrpMapDllWithSectionHandle [dll2 추정]
      │                 │            └─ +0x1c8  LdrpMapAndSnapDependency [dll2 추정]
      │                 │                 └─ +0x4f7  LdrpQueueWork [dll2 추정]   부모 있음 · 남은 수 0
      │                 └─ (2) +0x4f7  LdrpQueueWork [dll1 추정]                  부모 있음 · 남은 수 0
      └─ (4) +0x4c2  LdrpSnapModule [test.exe]   부모 없음 · 남은 수 0          [정적]
```

- 이름 옆 `[dll1 추정]` 등은 "그때 맡은 모듈" 이 소스 의존 관계로 짐작한 것이라는 뜻이다. 코드는 같은 함수가 모듈마다 다시 불리는 모양까지만 말한다.

## 확인된 것

- (`[정적]`) `LdrpMapAndSnapDependency` 는 import 서술자마다 `+0x34e LdrpLoadDependentModuleInternal` 을 **조건 없이 직접** 부른다. 고리는 `+0x1d0 ~ +0x3a3` 하나 (앞의 `+0x122 ~ +0x14d` 는 세기만).
- (`[정적]`) snap(`+0x4c2 LdrpSnapModule`)은 고리 **밖에서 한 번**. 조건: IAT 있음, 락 안에서 줄인 남은 자식 수 0, 문맥 `+0x30 == 0`. 그 직전 노드 `State = 4`(`Snapping`). 문맥 `+0x30 ≠ 0` 이면 같은 자리에서 `+0x4f7 LdrpQueueWork`. IAT 가 없으면 `State = 5`(`Snapped`) 만.
- (`[정적]`) 로드 문맥 `+0x30` = 부모 항목. exe 길·`LdrLoadDll` 길은 0, `LdrpLoadDependentModuleInternal` 이 만든 자식 문맥은 부모 항목.
- (`[정적]`) 로드 문맥 `+0x38` ↔ 항목 `+0xb0 LoadContext` 가 서로 가리킨다 (`LdrpAllocateModuleEntry +0x7f`·`+0x96`).
- (`[정적]`) 그래프 간선(`Dependencies`·`IncomingDependencies`)을 적는 줄은 이 두 함수에 **없다**.
- (`[정적]`) `LdrpAllocateModuleEntry` 는 항목(0x138) + 노드(0x50) 를 같이 만든다. 노드 `LoadCount` 1, 항목 `ReferenceCount` 2. 노드 `State` 를 쓰는 줄은 없다.
- (`[정적]`) `LdrpMapDllWithSectionHandle` 은 매핑하고 목록에 넣은 뒤, `CorILOnly` 가 꺼진 모듈이면 `+0x1c8 LdrpMapAndSnapDependency` 를 부른다. 그래서 **올라온 DLL 마다 같은 함수가 다시 돈다** (dll1 → dll2 로 내려가는 코드 모양).
- (`[정적]`) 병렬 로딩 켜짐 여부는 이 함수·`LdrpMapDllWithSectionHandle` 어디서도 읽지 않는다.

## 아직 확인 필요

- **간선을 적는 자리, "이미 올라옴 / KnownDll / 새로 올리기" 갈림, 인자 5 칸에 무엇이 들어가나** — `LdrpLoadDependentModuleInternal` 본문 (정적 **Q12b**). 이어 볼 줄: `+0x12df ~ +0x12f7`(부모 `+0x6c` 줄이기), `+0x1606 ~ +0x1634`(부모 노드 `State = 3`, 반환 `0x103`), `+0x182c LdrpQueueWork`·`+0x1cae LdrpProcessWork` 조건, `+0x1179`(`LoadReason`).
- **남은 자식 수가 0 이 되는 때, 0 이 아니면 snap 을 누가 이어받나** — 정적 Q12b, 동적 Q23. 멈춤: `ntdll+0x21506`(`+0x466`, 락 잡은 직후) `dd @rbx+0x6c L1`.
- **dll1·dll2 에 대해 이 함수가 정말 다시 불리나, 문맥 `+0x30` 값** — 동적 **Q23**. 입구 `ntdll+0x210a0` 에서 `du poi(@rcx+0x38)+0x58`(BaseDllName), `dq @rcx+0x30 L1`, `k`. 고리 안 `ntdll+0x213e9`(call `+0x349`) 에서 `du poi(@rcx+8)`.
- **끝맺음 갈림이 모듈마다 어느 쪽인가, 일감이 어느 스레드에서 도나** — 동적 **Q47** (+ kernel32 준비 때는 Q20). `ntdll+0x2155d`(`LdrpSnapModule` call) / `ntdll+0x21592`(`LdrpQueueWork` call) 에서 `~`, `dd ntdll+0x1d23c8 L1`(`LdrInitState`).
- **핫패치·리다이렉션 전역 값, exe 항목 `LoadReason`** — 동적 **Q46**. `db ntdll+0x1d26f0 L1`, `dq ntdll+0x1d2600 L1`. `LdrpAllocateModuleEntry` `RET` `ntdll+0x6fbf8` 에서 `r rax`, `dd @rax+0x10c L1`, `dd poi(@rax+0x98)+0x38 L1`(State 0 인가), `dd poi(@rax+0x98)+0x18 L1`.
- 이름·뜻 못 정한 것: 로드 문맥 칸 이름 전부 (PDB 에 없음), `+0xb8`(-1) 의 뜻, `+0x20` 비트들의 뜻, `LdrpGetImportDescriptorForSnap` 이 보는 전역(`DAT_1801ea4f8` 비트 44~45, 변조 검사 갈래)의 이름, `LdrpQueryCurrentPatch` 결과 바이트의 뜻, 힙 플래그 `| 8`.
- Q14 ("dll1·dll2 를 올리는 쪽") 는 **일부**: 바깥 고리 순서와 "dll1 에 대해 같은 함수가 다시 불린다" 는 코드 모양까지. 실제로 올리고 간선을 적는 일은 Q12b 뒤에 다시 본다.

## 디버거 메모 / 도구 메모

- 디버거는 안 썼다.
- 결과서는 몇몇 caller 오프셋을 call 자리로 적었다 (`LdrpMapDllWithSectionHandle +0x1c3`, `LdrpLoadDependentModuleInternal +0x1157`, `LdrpFindOrPrepareLoadingModule +0x475`, `LdrpInitializeProcess +0x1359`). 이 문서와 flow.md 는 `_calls.tsv` 로 ret(`+0x1c8`, `+0x115c`, `+0x47a`, `+0x135e`)를 맞춰 적었다. 결과서 "읽는 법" 은 ret 자리라고 했으니, 다음 결과서는 둘을 같이 적어 달라고 decompiler 에 제안.
- 별칭 확인함: `pdb_aliases.py --lookup ntdll.dll <0xRVA>` (표는 `ms/sym/ntdll.dll.aliases.tsv`, 모듈 인자는 `ntdll.dll`). `0x3d630` 9개·`0x21a90` 3개는 문서에 쓴 것과 같음. `0x166500` 은 PDB 이름이 `memcpy`·`memmove` 2개뿐이라 그 줄을 고쳤다 (`.asm` 의 `RtlCopyMemory`·`RtlMoveMemory` 는 Ghidra 표기).

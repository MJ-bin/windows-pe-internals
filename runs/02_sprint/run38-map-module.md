# 2026-10-24 run38 — DLL 을 메모리에 붙이는 `LdrpMinimalMapModule` (`+0x174 NtMapViewOfSection`) 과 재배치 담당 `LdrpCompleteMapModule` (`+0x31b LdrpRelocateImage`, 맵 결과가 정확히 `0x40000003` 일 때만) (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 결과서 날짜.
- 정적: ntdll 26100.9457, pdb GUID `C3093720177DA6851519DCFFE8DF53FE1` (기준 절과 같음). 이미 analyze 된 프로젝트라 다시 analyze 하지 않았다.
- 버전 대조 (결과서): `_calls.tsv` 의 `LdrpLoadDll +0xfb / +0x100 → LdrpLoadDllInternal`, `LdrpInitializeProcess +0x1bb6 / +0x1bbb → LdrpInitializeGraphRecurse` 가 알려진 사실과 **일치.** 새로 뽑은 함수의 `.asm` `CALL … ret=` 줄 수와 `_calls.tsv` 줄 수도 같다 (13 / 13 / 8 / 2).
- 이름 (결과서, `_index.tsv` + `pdb_aliases.py --where`): 네 함수 모두 PDB 이름 그대로.
  - **별칭 하나**: `LdrpMinimalMapModule +0x257` 의 callee 는 Ghidra 가 내보내기 이름 `LdrQueryImageFileKeyOption` 으로 잡았다 (`_calls.tsv` 에 `alias:` 로 나옴). PDB 이름은 `RtlQueryImageFileKeyOption`. 이 문서는 PDB 이름을 쓴다.
- 표시:
  - 코드 줄은 `[정적]`.
  - **sample 빌드 파일** (`sample/bin/test.exe`·`dll1.dll`·`dll2.dll`) 의 PE 헤더 값은 `[정적+샘플]`.
  - **MS 바이너리** (`kernel32.dll`) 의 PE 헤더 값은 `[정적: PE 표]`.
- 칸 이름은 PDB 타입 (`llvm-pdbutil`, 결과서). `_LDRP_LOAD_CONTEXT` 는 PDB 에 칸 정보가 없어서 오프셋 + 이번에 읽은 역할만 적는다.

## 목적

- Q52 (sast/21 "다음 정적 후보 — 가치 높음", flow.md 2.6 `[추측]` "재배치 후보 둘"):
  1. `LdrpMinimalMapModule` 이 **어떤 시스템 콜로** DLL 을 메모리에 붙이나. 인자, 항목의 `DllBase`·`SizeOfImage` 를 누가 채우나, 결과 코드를 어떻게 다루나.
  2. `LdrpCompleteMapModule` 이 **재배치(기준 주소가 바뀌었을 때 주소 고치기) 담당인가.** 부르는 조건은.
  3. 커널이 이미지 섹션을 만들 때 하는 재배치(run31 `MiRelocateImage`) 와 어떤 관계인가.
  4. 이 둘이 exe 에도 불리나.

## 방법

- 정적: `_workspace/sast/33_map-module.md` — ntdll
  - `^LdrpMinimalMapModule$`·`^LdrpCompleteMapModule$` depth 1 (`.asm`/`.c` 가 이미 있어 다시 안 뽑음, depth 1 callee 본문은 안 읽음)
  - `^LdrpRelocateImage$`·`^LdrpProcessMachineMismatch$` depth 0 (새로 뽑음. 지도에서 깊이 10 이라 안 펼쳐졌던 것)
  - 요청서 밖 하나 더: `^LdrpProtectAndRelocateImage$` depth 0. 요청서 2b 가 물은 "보호 바꾸기·`LdrRelocateImageWithBias`" 가 `LdrpRelocateImage` 한 단계 아래에 있어서다. 이름·호출 자리만 봤다.
  - `refs` 5개: `LdrpLargePageDllKeyHandle`, `LdrpMapAndSnapWork`, `LdrpProtectAndRelocateImage`, `LdrpRelocateImage`, `LdrRelocateImageWithBias` (누가 부르고 쓰나).
- 결과서: `_workspace/sast/33_map-module_result.md` (**분석성공**)
- 샘플 헤더: 결과서가 `sample/bin/*` 를 직접 읽음. 타깃 VM 에서 실제로 돈 파일과 같은지는 확인 안 함.

## 결과

비유 (이사):

- `LdrpMinimalMapModule` = **짐 꾸러미를 집 안에 들여놓는 손.** 관리실(커널)에 "이 꾸러미(섹션)를 우리 집에 펼쳐 줘" 하고 부탁한다 (`NtMapViewOfSection`). 어느 방에 펼쳤는지는 관리실이 **집 장부의 `DllBase` 칸에 직접** 적어 준다.
- 관리실이 "원래 자리(헤더의 `ImageBase`) 가 아닌 방에 펼쳤어요" 라고 답하면 (`0x40000003`), 짐 안의 **주소 쪽지를 새 방 번호로 고쳐야** 한다. 그게 재배치다.
- 그 고치기를 맡는 게 `LdrpCompleteMapModule` → `LdrpRelocateImage`. 관리실이 "펼쳤어요, 문제없음"(`0`) 이라고 답하면 **안 고친다.**

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x49` | `+0x4e` | `ntdll!LdrpMinimalMapModule` | [정적] (run26) |
| `ntdll!LdrpMinimalMapModule` | `+0x92` | `+0x97` | `ntdll!RtlEqualUnicodeString` (이름이 `KERNEL32.DLL` 인가) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x174` | **`+0x179`** | **`ntdll!NtMapViewOfSection`** (기본 길) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x257` | `+0x25c` | `ntdll!RtlQueryImageFileKeyOption` (큰 페이지 갈래. Ghidra 이름 `LdrQueryImageFileKeyOption`) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x278` | `+0x27d` | `ntdll!RtlAcquirePrivilege` (큰 페이지 갈래) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x2a9` | `+0x2ae` | `ntdll!LdrpCheckForRetryLoading` (경고 코드 + 일꾼 개체 있을 때) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x2d4` | `+0x2d9` | `ntdll!NtUnmapViewOfSection` (음수·`0x4000000e` 일 때) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x2ff` | `+0x304` | `ntdll!LdrpProcessMachineMismatch` (`0x4000000e` 일 때) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x36c` | `+0x371` | `ntdll!NtMapViewOfSectionEx` (확장 인자 있을 때, `+0x371 JMP` 로 `+0x179` 에 합침) | [정적] |
| `ntdll!LdrpMinimalMapModule` | `+0x37a` | `+0x37f` | `ntdll!RtlReleasePrivilege` (큰 페이지였을 때) | [정적] |
| `ntdll!LdrpMapDllWithSectionHandle` | `+0x24a` | `+0x24f` | `ntdll!LdrpCompleteMapModule` | [정적] (run26) |
| `ntdll!LdrpCompleteMapModule` | `+0x58` | `+0x5d` | `ntdll!RtlImageNtHeaderEx` | [정적] |
| `ntdll!LdrpCompleteMapModule` | `+0x285` | `+0x28a` | `ntdll!LdrpCorValidateImage` (.NET 만) | [정적] |
| `ntdll!LdrpCompleteMapModule` | `+0x2a6` | `+0x2ab` | `ntdll!LdrpCorFixupImage` (.NET + 기계 불일치 만) | [정적] |
| `ntdll!LdrpCompleteMapModule` | `+0x31b` | **`+0x320`** | **`ntdll!LdrpRelocateImage`** (맵 결과 == `0x40000003` 만) | [정적] |
| `ntdll!LdrpCompleteMapModule` | `+0x338` | `+0x33d` | `ntdll!LdrpLogError` (재배치 실패 때) | [정적] |
| `ntdll!LdrpRelocateImage` | `+0x95` | `+0x9a` | `ntdll!RtlImageDirectoryEntryToData` (재배치 디렉터리 5번) | [정적] |
| `ntdll!LdrpRelocateImage` | `+0x12b` | `+0x130` | `ntdll!RtlImageDirectoryEntryToData` (.NET 디렉터리 14번) | [정적] |
| `ntdll!LdrpRelocateImage` | `+0x142` | `+0x147` | `ntdll!LdrpLogDllRelocationEtwEvent` | [정적] |
| `ntdll!LdrpRelocateImage` | `+0x14a` | **`+0x14f`** | **`ntdll!LdrpProtectAndRelocateImage`** | [정적] |
| `ntdll!LdrpProtectAndRelocateImage` | `+0x8e` | **`+0x93`** | **`ntdll!LdrRelocateImageWithBias`** (실제 주소 고치기) | [정적] |
| `ntdll!LdrpProtectAndRelocateImage` | `+0xbc` / `+0x16b` | `+0xc1` / `+0x170` | `ntdll!LdrpSetProtection` (보호 바꾸기 둘) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1430` | `+0x1435` | `ntdll!LdrpProtectAndRelocateImage` (**exe** 재배치) | [정적] (run19 이름, 이번 refs) |
| `ntdll!LdrpLoadCustomNtdllWithSectionHandle` | `+0x5a` | — (`_calls.tsv` 에 없음, `refs` 줄만) | `ntdll!LdrpProtectAndRelocateImage` (custom ntdll) | [정적] (refs) |

- 그 밖 직접 호출 (로그 `LdrpLogInternal` ×4·×1, ETW `LdrpLogEtwEvent` ×2, `RtlGetCurrentServiceSessionId` ×4, `RtlAddressInSectionTable`, `NtQueryVirtualMemory`, `NtRaiseHardError` 등) 은 아래 "구간" 표와 결과서.
- `LdrRelocateImageWithBias` 를 부르는 곳은 `LdrpProtectAndRelocateImage +0x8e` **하나**, `LdrpProtectAndRelocateImage` 를 부르는 곳은 **셋** (DLL / exe / custom ntdll), `LdrpRelocateImage` 를 부르는 곳은 `LdrpCompleteMapModule +0x31b` **하나** (`refs`, 나머지는 함수 밖 `.pdata` 칸).

### 정적 — `LdrpMinimalMapModule` (rva `0xa220`, 915바이트) 구간

인자: `RCX` = 로드 문맥 `ctx`, `RDX` = 섹션 핸들. 입구에서 `RDI` = 항목 `E` (`ctx+0x38` 칸이 가리키는 `_LDR_DATA_TABLE_ENTRY`, `+0x1b`), `RSI` = `ctx`.

| 구간 | 하는 일 | 호출 |
|---|---|---|
| `+0x0 ~ +0x84` | 입구, 로그 둘 | `+0x50`, `+0x7f LdrpLogInternal` |
| `+0x84 ~ +0xbe` | **kernel32 + LAA exe 판정** (`R14B`): `E.BaseDllName` 이 `KERNEL32.DLL`(대소문자 무시) 이고 exe 헤더 `Characteristics & 0x20` | `+0x92 RtlEqualUnicodeString` |
| `+0x22e ~ +0x283` | **큰 페이지 갈래**: `R14B == 0` 이고 전역 `LdrpLargePageDllKeyHandle ≠ 0` 일 때만 (`+0xbb`, `+0x238`) | `+0x257`, `+0x278` |
| `+0xc4 ~ +0x174`, `+0x30b ~ +0x36c` | 인자 만들기: `ViewSize` 칸 0, 할당·보호 값, `TEB.ArbitraryUserPointer` 바꿔 두기, 확장 인자 | 없음 |
| `+0x174` / `+0x36c` → `+0x179` | **시스템 콜** (기본 `NtMapViewOfSection`, 확장 인자 있으면 `Ex`) → 포인터 되돌림 → 큰 페이지였으면 권한 풀기 | `+0x174`, `+0x36c`, `+0x37a` |
| `+0x190 ~ +0x1b0`, `+0x288 ~ +0x2c0`, `+0x2fc`, `+0x384` | **결과 코드 가르기** | `+0x2a9`, `+0x2ff` |
| `+0x1b0 ~ +0x22c`, `+0x2c0 ~ +0x2de` | 정리: 음수·`0x4000000e` 면 뷰를 지우고 `DllBase = 0`. 로그 둘. `+0x219 MOV EAX,EBX` 로 반환 | `+0x2d4`, `+0x1e6`, `+0x214` |

### 정적 — `LdrpCompleteMapModule` (rva `0xa5c0`, 940바이트) 구간

인자: `RCX` = `ctx`, `RDX` = NT 헤더 (`R15`), `R8D` = **맵 결과 코드** (`R12D`). `R14` = `E`, `RDI` = `E.DllBase` (낮은 두 비트를 떼어 냄).

| 구간 | 하는 일 | 호출 |
|---|---|---|
| `+0x0 ~ +0x58` | 입구, NT 헤더 얻기 | `+0x58 RtlImageNtHeaderEx` |
| `+0x6a ~ +0x162` | .NET 디렉터리(14번) 찾기. 보통 native DLL 은 없어서 그냥 지나감 | `+0x129 RtlAddressInSectionTable` (데이터 파일 맵만) |
| `+0x109 ~ +0x117`, `+0x263 ~ +0x2b5` | .NET 있을 때만: 검증·고치기, `E.Flags` 에 `CorImage`·`CorILOnly` | `+0x285`, `+0x2a6` |
| `+0xbc ~ +0xf1`, `+0x2ba`, `+0x2c4 ~ +0x304` | **공통 검사**: DLL 비트, 서명 요구, `CorILOnly` 면 끝, 맵 결과가 `0x40000003`/`0x40000036` 인가 | `+0x2fa LdrpLogInternal` |
| `+0x167 ~ +0x1b4`, `+0x354 ~ +0x3ab` | 재배치 **앞** ETW (`0x1490`) | `+0x16b`·`+0x354 RtlGetCurrentServiceSessionId`, `+0x39e LdrpLogEtwEvent` |
| `+0x1b4`, `+0x309 ~ +0x33d` | **재배치**: 맵 결과 == `0x40000003` 일 때만 | **`+0x31b LdrpRelocateImage`**, `+0x338 LdrpLogError` |
| `+0x1c1 ~ +0x254` | 재배치 **뒤** ETW (`0x1491`) | `+0x1c5`·`+0x204`, `+0x24f` |

### 호출 관계

```
LdrpMapDllWithSectionHandle (dll1·dll2·kernel32·kernelbase 공통 — run21·run26)
 ├─ (1) +0x4e  LdrpMinimalMapModule(ctx, 섹션)
 │      ├─ +0x97   RtlEqualUnicodeString      이름 == KERNEL32.DLL 인가 (+ exe LAA → R14B)
 │      ├─ [큰 페이지 갈래: R14B == 0 · LdrpLargePageDllKeyHandle ≠ 0]  +0x25c RtlQueryImageFileKeyOption → +0x27d RtlAcquirePrivilege
 │      ├─ +0x179  NtMapViewOfSection(섹션, -1, &E.DllBase, 0, 0, NULL, &ctx+0xa8, 1, 0x800000, 0x80)   ← 기본
 │      │   (또는 +0x371 NtMapViewOfSectionEx — ctx+0x20 비트 0x800 · 0x800000 일 때, +0x179 로 합침)
 │      ├─ 0x40000003 · 0x40000036 → [ctx+0xb0 == 0 · LdrpMapAndSnapWork ≠ 0 일 때만] +0x2ae LdrpCheckForRetryLoading
 │      ├─ 0x4000000e → +0x304 LdrpProcessMachineMismatch
 │      └─ 음수 · 0x4000000e → +0x2d9 NtUnmapViewOfSection, DllBase = 0
 │      반환 = 맵 결과 (보통 받은 값 그대로)
 ├─ (2) +0x146 LdrpInsertDataTableEntry · (3) +0x151 색인      (run26)
 ├─ (4) +0x24f LdrpCompleteMapModule(ctx, NT, 맵 결과)
 │      └─ [DLL 비트 · 서명 조건 · CorILOnly 아님 · 맵 결과 == 0x40000003]
 │         +0x320 LdrpRelocateImage(DllBase, ViewSize, NT, &FullDllName)
 │              ├─ RELOCS_STRIPPED 꺼짐 + 재배치 디렉터리 없음/크기 0 → 0 반환 (안 고침)
 │              ├─ IL 전용 .NET → 0 반환 (안 고침)
 │              └─ +0x14f LdrpProtectAndRelocateImage(DllBase)
 │                   ├─ +0x4f  NtQueryVirtualMemory
 │                   ├─ +0x93  LdrRelocateImageWithBias        ★ 실제 주소 고치기
 │                   └─ +0xc1 / +0x170 LdrpSetProtection       보호 바꾸기
 ├─ (5) +0x26c LdrpProcessMappedModule                         (run24·run26)
 └─ (6) +0x1c8 LdrpMapAndSnapDependency                        (run22·run26)

LdrpInitializeProcess (exe — 다른 길)
 └─ +0x1419 CMP [NT+0x30] (헤더 ImageBase), PEB.ImageBaseAddress → 다르고 · IL 전용 아님 · UseWOW64 == 0 이면
    +0x1435 LdrpProtectAndRelocateImage(PEB.ImageBaseAddress)    ← LdrpRelocateImage·0x40000003 문 안 거침
```

## 확인된 것

### 1. 붙이기 = `LdrpMinimalMapModule +0x174 NtMapViewOfSection` (ret `+0x179`) `[정적]`

- **기본 길은 `NtMapViewOfSection` 이다.** `NtMapViewOfSectionEx` (`+0x36c`) 는 확장 인자가 하나라도 만들어졌을 때만 탄다.
  - 조건: `+0x11d TEST [ctx+0x20],0x800` (켜지면 주소 상한 `LdrpMaximumUserModeAddress` 확장 인자, `+0x30b ~ +0x327`), `+0x132` (`ctx+0x20 & 0x800000` 이면 유형 5 확장 인자, `+0x32c`). `+0x13f TEST ECX,ECX` → `+0x141 JNZ` 가 `Ex` 길 (`+0x346`). 둘 다 꺼지면 `+0x174`.
  - 두 길은 **`+0x179` 에서 합친다** (`+0x371 JMP`). 그래서 멈춤점 하나 `ntdll+0xa399` (= `0xa220 + 0x179`) 로 둘 다 잡힌다.
  - 두 비트를 **켜는 줄은 못 찾았다** (뽑아 둔 ntdll 함수에서 `ctx+0x20` 에 `OR`/`BTS` 하는 줄 0건). test.exe 길에서 켜지는지는 실행 값 → 동적 (Q20·Q23), 켜는 줄은 정적 Q70.
- **인자** (x64: 처음 넷 `RCX/RDX/R8/R9`, 나머지 스택):

| 순서 | 이름 (SDK) | 값 | 줄 |
|---|---|---|---|
| 1 | SectionHandle | 이 함수의 **둘째 인자** (`ctx` 칸이 아님) | `+0x147` |
| 2 | ProcessHandle | `-1` (자기 프로세스) | `+0x14f` |
| 3 | BaseAddress | **`&E.DllBase`** (`E+0x30`, 처음엔 0) | `+0x13b LEA R8,[RDI+0x30]` |
| 4·5·6 | ZeroBits · CommitSize · SectionOffset | 0 · 0 · `NULL` | `+0x165`, `+0x16e`, `+0x168` |
| 7 | **ViewSize** | **`&ctx+0xa8`** (호출 전 0 = "전체") | `+0xcd`, `+0xd4`, `+0x160` |
| 8 | InheritDisposition | `1` | `+0x158` |
| 9 | AllocationType | 기본 **`0x800000`**. `ctx+0x20 & 0x800000` 이면 `\| 0x40000` (`+0x103`). 큰 페이지 갈래가 성공하면 `0x20000000` | `+0x153` |
| 10 | Win32Protect | 기본 **`0x80`**. `ctx+0x20 & 0x800000` 이면 `2` | `+0x14b`, 계산 `+0xf8 ~ +0x10b` |

  - 상수 이름 `MEM_DIFFERENT_IMAGE_BASE_OK`(`0x800000`)·`MEM_LARGE_PAGES`(`0x20000000`)·`PAGE_EXECUTE_WRITECOPY`(`0x80`)·`PAGE_READONLY`(`2`)·`ViewShare`(`1`) 은 SDK 기억으로 맞춘 것 `[추측: 이름]`. `0x800000` 이 "헤더 기준 주소가 아닌 곳에 붙여도 된다" 는 뜻이라면, 이 비트가 있어야 `0x40000003` 같은 경고 코드가 나온다 `[추측]`.
- 곁다리: 부르기 직전 `TEB.NtTib.ArbitraryUserPointer` (`TEB+0x28`) 를 `E.FullDllName.Buffer` 로 바꾸고 (`+0xe2 ~ +0xea`), 부른 뒤 되돌린다 (`+0x17b ~ +0x17f`) `[정적]`. 맵 도중 DLL 이름을 커널(또는 디버거)에 알리는 자리로 보인다 `[추측]`.

### 2. `DllBase`·`SizeOfImage`·`ctx+0xa8` 는 누가 채우나 `[정적]`

- **`DllBase` 는 시스템 콜이 직접 쓴다.** BaseAddress 인자가 `&E.DllBase` 라서다. 이 함수가 그 칸에 쓰는 줄은 지울 때 `+0x2d9 AND [RDI+0x30],0` 하나뿐.
- **`ctx+0xa8` = ViewSize 칸** (인자 7). sast/21·run26 의 "맵 크기 `[추측]`" 이 **확정**. 이어서 `LdrpCompleteMapModule +0x309` 가 읽어 `LdrpRelocateImage` 둘째 인자로 넘긴다.
- **`SizeOfImage` (`E+0x40`) 는 여기서 안 채운다.** 쓰는 줄은 `LdrpInsertModuleToIndexLockHeld +0x2e` (NT 헤더 값) 와, `ctx+0x20 & 0x800000` 길의 `LdrpMapDllWithSectionHandle +0x222`. 곧 맵 결과가 아니라 **헤더 값**이다.

### 3. 결과 코드 `[정적]`

`NtMapViewOfSection` 결과 (`EBX`) 를 `+0x190 ~ +0x1aa` 에서 가른다.

| 결과 | 갈래 | 돌려주는 값 |
|---|---|---|
| `0` | 그대로 | `0` |
| `0x40000003` (`+0x192`) · `0x40000036` (`+0x1a7 CMP ECX,0x28`) | `+0x288`: **`ctx+0xb0 == 0` 이고 `LdrpMapAndSnapWork ≠ 0`** 일 때만 `+0x2a9 LdrpCheckForRetryLoading(ctx, 1)`. 참이면 `0xc000022d`. 거짓이면 `+0x384`: `R14B` 면 `0xc0000018`. 조건이 아니면 검사 없이 그대로 | 보통 **받은 값 그대로** |
| `0x4000000e` (`+0x19e`) | `+0x2fc` → `+0x2ff LdrpProcessMachineMismatch(ctx)` | 그 결과 |
| 그 밖 | 그대로 | 받은 값 |

- 정리 (`+0x1b0`, `+0x2c0 ~ +0x2de`): **`DllBase ≠ 0` 이고 (음수 또는 `0x4000000e`)** 이면 `+0x2d4 NtUnmapViewOfSection` 하고 `DllBase = 0`. 성공·경고 코드는 뷰를 그대로 둔다.
- caller (`LdrpMapDllWithSectionHandle +0x4e`) 는 음수·`0x4000000e` 면 끝내고, 나머지는 `EBP` 에 두었다가 `LdrpCompleteMapModule` 셋째 인자로 넘긴다 (run26). 그래서 `CompleteMap` 이 받는 값은 `0`, `0x40000003`, `0x40000036`, 그 밖의 양수 중 하나다.
- `0x40000036` 의 이름은 **못 정했다.** `STATUS_IMAGE_AT_DIFFERENT_BASE` 로 기억 `[추측]` → 정적 Q70. `0xc000022d`(`STATUS_RETRY`)·`0xc0000018`(`STATUS_CONFLICTING_ADDRESSES`)·`0xc000007b`·`0xc0000428` 이름도 SDK 기억 `[추측]`.
- **`LdrpProcessMachineMismatch`** (rva `0x10af3c`, 185바이트, depth 0):
  - `+0x27 RtlImageDirectoryEntryToData` 로 .NET 디렉터리(14번) 를 찾아, IL 전용(`Flags & 1`) 이면 `+0x37 ctx+0x20 |= 0x200000` 하고 `0` 반환 (`+0x3c`). "기계 종류가 달라도 IL 전용 .NET 이면 받아 준다".
  - 아니면 exe 헤더(`LdrpAppHeaders`) `MajorSubsystemVersion` (`+0x56`) 이 3 보다 크면 `0x4000000e` 그대로 (`+0x5c`) → 위 정리에서 뷰를 지움. 3 이하면 `+0x90 NtRaiseHardError` 로 사용자에게 묻는다.
  - test.exe 는 `MajorSubsystemVersion` 6 `[정적+샘플]`, dll1·dll2·kernel32 는 모두 x64 라 이 갈래는 안 탈 것 `[정적 조건 + 헤더 값]`.

### 4. 큰 페이지 갈래와 kernel32 갈래 — test.exe 에선 안 탈 것

- 큰 페이지 갈래 (`+0x257`, `+0x278`, `+0x37a`) `[정적]`:
  - `LdrpLargePageDllKeyHandle` 은 `LdrpInitializeExecutionOptions +0x389` 가 `PEB.ImageUsesLargePages` (PEB+3 비트 0) 일 때만 `+0x3c6 NtOpenKey` 로 연다 (키 이름 `LargePageDLLs`, 원본 바이트). 없으면 `+0x3d4` 에서 0. `LdrpInitializeProcess +0x178d ~ +0x179e` 가 닫고 0 으로 쓴다. 쓰는 곳은 이 둘뿐 (`refs`).
  - 핸들이 있으면 DLL 이름과 같은 DWORD 값을 읽고, 0 이 아니면 권한을 잡아 할당 값을 `0x20000000` 으로 바꾼다.
  - sample 에 큰 페이지 설정이 없다 `[소스]` → 핸들 0, `+0x238 JZ` 로 건너뛸 것 `[추측]` (동적: `dq ntdll+0x1d2780 L1`).
- kernel32 갈래 (`R14B`):
  - test.exe 헤더 `Characteristics` = `0x22` (LAA 켜짐) `[정적+샘플]` → kernel32 를 올릴 때 `R14B = 1` `[정적 조건 + 헤더 값]`. 그때 큰 페이지 갈래는 건너뛰고, "재시도 검사 거짓 → `0xc0000018`" 갈래가 열린다.
  - 그 갈래는 `LdrpMapAndSnapWork ≠ 0` 일 때만 닿는다. kernel32 는 병렬 켜기 (`LdrpInitializeProcess (8) +0x1a47`) 앞에서 올라오고 그때 그 값은 0 (run24) → **kernel32 에선 안 탄다** `[정적: 코드 + run24 순서]`. kernel32 만 따로 다루는 이유는 모른다.

### 5. 재배치 담당 = `LdrpCompleteMapModule` — **예** `[정적]`

- `LdrpRelocateImage` 를 부르는 코드는 **`+0x31b` 하나뿐**이다 (`refs`).
- 부르는 조건 (다 참이어야 함):
  1. NT `Characteristics & 0x2000` (DLL 비트) (`+0xbc ~ +0xc6`). 꺼졌으면 `+0x2ba` 에서 `E.Flags` 의 `ImageDll` 을 끄고 끝.
  2. `ctx+0x20 & 0x80` 이 꺼져 있거나, 켜졌다면 `DllCharacteristics & 0x80` (`FORCE_INTEGRITY`) 가 있다 (`+0xcc`, `+0x2c4`). 아니면 `+0x2fa` 로그 → `+0x2ff` `0xc0000428`.
  3. `E.Flags` 의 `CorILOnly` (비트 24) 꺼짐 (`+0xd7`).
  4. 맵 결과가 `0x40000003` 또는 `0x40000036` (`+0xe4 SUB ECX,0x40000003`, `+0xec CMP ECX,0x33`). 아니면 (`0` 포함) 끝.
  5. **맵 결과가 정확히 `0x40000003`** (`+0x1b4 CMP R12D,0x40000003`). 이때만 `+0x309 ~ +0x31b` 에서 `LdrpRelocateImage(E.DllBase, ctx+0xa8 값, NT, &E.FullDllName)`. `0x40000036` 이면 `+0x1c1` 로 건너뛴다 (ETW 만).
  6. 결과가 음수면 `+0x338 LdrpLogError` 하고 그 값을 돌려준다 → **재배치 실패 = 로드 실패.**
- **조건이 아닌 것**: `.reloc` 유무, `DYNAMIC_BASE` (`DllCharacteristics 0x40`) 는 이 함수가 안 본다.
- 재배치 앞뒤 ETW 이벤트 `0x1490`·`0x1491` (`+0x39e`·`+0x24f LdrpLogEtwEvent`) 는 `PEB.TracingFlags` 비트 2 와 공유 데이터 `UserModeGlobalLogger` 칸이 켜졌을 때만. `RtlGetCurrentServiceSessionId` ×4 는 그 칸을 `KUSER_SHARED_DATA` 에서 읽을지 silo 사본에서 읽을지 고르는 것.
- 평범한 native DLL 은 .NET 디렉터리가 없어 `+0xaf` 로 곧장 가서 1~5 검사만 한다 `[정적: 경로 따라감]`.

### 6. `LdrpRelocateImage` (rva `0xe3984`, 359바이트, depth 0) `[정적]`

- 인자: `RCX` = `DllBase`, `RDX` = ViewSize, `R8` = NT 헤더, `R9` = `&FullDllName`.
- `+0x7d TEST [R8+0x16],1` (`RELOCS_STRIPPED`):
  - 꺼져 있으면 `+0x95` 로 재배치 디렉터리(5번) 를 찾고, 없거나 크기 0 이면 **아무것도 안 하고 0 반환**.
  - 켜져 있으면 그 검사를 건너뛴다 (`+0x81 JNZ`).
- `+0x12b` 로 .NET 디렉터리(14번) 를 보고, IL 전용이면 0 반환 (안 고침).
- 아니면 `+0x142 LdrpLogDllRelocationEtwEvent` → **`+0x14a LdrpProtectAndRelocateImage(DllBase)`** → 그 결과 반환.
- 한 단계 아래 `LdrpProtectAndRelocateImage` (rva `0xe3af4`, 418바이트, 이름·호출만): `+0x4a NtQueryVirtualMemory` → **`+0x8e LdrRelocateImageWithBias(DllBase)`** → `+0xbc`·`+0x16b LdrpSetProtection` (보호 바꾸기 — 앞/뒤 어느 쪽인지는 `[추측]`) + 예외 필터 `LdrpGenericExceptionFilter`. 본문 (편차 계산·블록 처리) 은 안 읽었다.

### 7. 커널 재배치와의 관계

- `[정적]` **유저 쪽 DLL 재배치의 문은 맵 결과가 정확히 `0x40000003` 인 것 하나뿐.** `0` 이면 `LdrpRelocateImage` 를 안 부른다.
- `[정적]` `0x40000036` 은 문을 통과하지만 재배치는 안 시킨다. 코드가 두 값을 구별한다 (`MinimalMap +0x192`/`+0x1a7`, `Complete +0xe4`/`+0x1b4`).
- `[정적]` 커널이 `0` / `0x40000003` / `0x40000036` 중 무엇을 돌려주나는 **ntdll 코드로 안 정해진다** (시스템 콜 안쪽).
- `[추측]` (근거 약함) 커널이 섹션을 만들 때 이미 델타를 더해 둔다면 (run31 `MiRelocateImage`), ASLR 로 옮겨진 보통 DLL 은 `0` 이나 `0x40000036` 을 받고 **유저 재배치를 안 할** 것이다. 이 짐작은 ① `0x40000036` 이름 기억, ② 커널이 메모리 속 헤더 `ImageBase` 도 새 값으로 고치는지 에 기댄다. 둘 다 정적으로 확인 못 함 → 동적.
- 헤더 값:
  - `[정적+샘플]` dll1·dll2 헤더 `ImageBase` = `0x180000000`, test.exe = `0x140000000`. 셋 다 `DYNAMIC_BASE` (`DllCharacteristics` 에 `0x40`) + `.reloc` 있음, .NET 없음.
  - `[정적: PE 표]` kernel32 헤더 `ImageBase` 도 `0x180000000`, `DllCharacteristics` `0x4160`, `.reloc` 있음, `MajorSubsystemVersion` 10.
  - `[관찰]` (run6·run8, 01_sprint) 실제로 붙은 주소: dll1 `0x7ffd814e0000` / `0x7ffd4f190000`, dll2 `0x7ffd80e70000` / `0x7ffd4ea20000`, test `0x7ff70ee10000`. **헤더 주소와 다르다.** (run6~8 은 디버거 VM 빌드라 sample/bin 과 같은 파일인지는 모른다. 헤더 `ImageBase` 는 같은 링크 기본값일 것 `[추측]`)
  - 그러니 "다른 주소에 붙었다" 는 거의 틀림없고, 남은 질문은 **그때 커널이 `0x40000003` 을 돌려줘서 ntdll 이 고쳤나, 커널이 다 고쳐 두고 `0`/`0x40000036` 을 돌려줬나** 하나다.

### 8. exe 에는 안 불린다 — exe 재배치는 다른 줄 `[정적]`

- `_nodes.tsv` `callers_total`: `LdrpMinimalMapModule` 1, `LdrpCompleteMapModule` 1, `LdrpRelocateImage` 1, `LdrpProcessMachineMismatch` 1. 모두 DLL 매핑 사슬 안이다. exe 는 커널이 이미 붙여서 (run10 `MiMapProcessExecutable`) 이 길로 안 온다.
- exe 재배치: `LdrpInitializeProcess +0x1419 CMP [NT+0x30], PEB.ImageBaseAddress` → 다르고, IL 전용이 아니고, `+0x1424` `UseWOW64 == 0` 이면 **`+0x1430` (ret `+0x1435`) `LdrpProtectAndRelocateImage(PEB.ImageBaseAddress)`**. `LdrpRelocateImage` 와 `0x40000003` 문을 건너뛴다. run19 의 "헤더 `ImageBase` 가 `PEB.ImageBaseAddress` 와 다를 때만" 과 같은 줄이고, 이번에 callee 이름과 그 아래 (`+0x8e LdrRelocateImageWithBias`) 가 이어졌다.
- 그 뒤 exe 항목은 `+0x1450` (ret `+0x1455`) `LdrpInsertDataTableEntry` 로 명부에 들어간다 (run19·run35).

## 정정

- 이전 run 은 틀린 곳이 없다. run26 의 `[추측]` "재배치 후보 둘 (`LdrpCompleteMapModule` / `LdrpProcessMappedModule +0x24d LdrpIsExecutableRelocatedImage`)" 중 **첫째가 맞았다.** 둘째 후보 (`LdrpIsExecutableRelocatedImage`) 가 "재배치된 이미지인가" 를 다시 묻는 줄인지는 이번에 안 봤다 (공백으로 남김).
- 결과서 읽을 때 고친 것 (결과서도 같은 날 decompiler 가 정정함 — 맨 위 정정 줄): 결과서 몇 군데의 점프 목적지 `+0x…` 가 **함수 오프셋이 아니라 주소 끝 세 자리**다. `.asm` 으로 다시 맞춰 이 문서엔 함수 오프셋으로 적었다.
  - `LdrpCompleteMapModule`: `+0x774` → **`+0x1b4`** (`CMP R12D,0x40000003`), `+0x8c9` → `+0x309`, `+0x781` → `+0x1c1`, `+0x727` → `+0x167`, `+0x914 ~ +0x96b` → `+0x354 ~ +0x3ab`, `+0x66f` → `+0xaf`.
  - `LdrpMinimalMapModule`: `+0x4a8` → `+0x288`, `+0x51c` → `+0x2fc`, `+0x52b ~ +0x547` → `+0x30b ~ +0x327`, `+0x54c` → `+0x32c`, `+0x566` → `+0x346`, `+0x5a4` → `+0x384`.
  - 결과서의 call 자리·ret 자리 (`+0x174`, `+0x31b`, `+0x14a`, `+0x8e` 등) 와 동적 표의 `ntdll+rva` 는 맞다.

## 아직 확인 필요

- **동적 Q20** (kernel32·kernelbase) · **Q23** (dll1·dll2) — 같은 실행에서 (오케스트레이터가 plan.md 에 sast/33 출처로 붙임):
  - `ntdll+0xa399` (`LdrpMinimalMapModule +0x179`, `NtMapViewOfSection`/`Ex` 둘 다 여기): `r eax` (`0` / `0x40000003` / `0x40000036`), 붙은 주소 `poi(@rdi+0x30)`, 보기 크기 `poi(@rsi+0xa8)`, 이름 `du poi(@rdi+0x50)`, 메모리 헤더 `ImageBase` `dq poi(@rdi+0x30)+dwo(poi(@rdi+0x30)+0x3c)+0x30 L1` (붙은 주소와 같으면 커널이 헤더도 고친 것).
    - `e_lfanew` 는 4바이트라 `dwo(…+0x3c)` 로 읽는다. 결과서도 처음 `poi(…+0x3c)` 였다가 같은 날 `dwo` 로 정정됐다 (맨 위 정정 줄).
  - `ntdll+0xe3984` (`LdrpRelocateImage` 입구, 호출 자리 `ntdll+0xa8db`): **안 서면 "보통 DLL 은 유저 재배치 안 함" 확정.** 서면 `r rcx`, `du poi(@r9+8)`, 그리고 `ntdll+0xe3b82` (`LdrRelocateImageWithBias` call).
  - 안 서야 정상: `ntdll+0xa58c` (`Ex` call), `ntdll+0xa4c9` (재시도 검사), `ntdll+0xa4f4` (Unmap), `ntdll+0xa51f` (Mismatch).
  - `ntdll+0xa5c0` (`CompleteMap` 입구) `r r8d` = 위 `eax` 와 같은가.
  - 값: `dq ntdll+0x1d2780 L1` (`LdrpLargePageDllKeyHandle`, 0 기대), `dq ntdll+0x1d2640 L1` (`LdrpMapAndSnapWork`, kernel32 때 0, dll1 때 ≠ 0 인가).
  - exe: `ntdll+0x8d834` (`LdrpInitializeProcess +0x1430` call) 이 **안 서야** 헤더 `ImageBase` 가 이미 `PEB.ImageBaseAddress` 와 같은 것 (커널이 헤더를 고침). run31 Q61 과 같은 실행이면 좋다.
- **정적 Q70** (새, 작은 정적): `ctx+0x20` 비트 `0x800`·`0x800000` (→ `Ex` 길) 을 켜는 줄, `0x40000036` 의 이름 (PDB/헤더로).
- 번호 없이 남긴 것: `LdrpProcessMappedModule +0x24d LdrpIsExecutableRelocatedImage` 가 무엇을 묻나 (run26 둘째 후보), `LdrpCheckForRetryLoading`·`LdrpCorValidateImage`·`LdrpCorFixupImage`·`LdrRelocateImageWithBias`·`LdrpSetProtection` 본문, `LdrpCompleteMapModule` 입구 `BL` (낮은 비트 = 데이터 파일 맵 표시 `[추측]`), `ctx+0x20` 비트 `0x80` (서명 요구 `[추측]`).

## 디버거 메모 / 도구 메모

- 디버거 안 씀.
- 도구: 결과서 "스크립트 문제 없음".
- 결과서 읽기 메모: `.asm` 의 `JZ 0x18000a774` 같은 목적지를 `+0x774` 로 옮겨 적은 곳이 있었다 (위 정정). decompiler 쪽에 "점프 목적지는 주소에서 함수 시작을 빼서 적기" 를 알려 두면 좋겠다 (하네스 제안, 오케스트레이터 판단).
- 멈춤점 메모 (다음 dast 용, 제안): `ntdll+0xa399` 는 DLL 마다 한 번씩 서므로 (kernel32·kernelbase·dll1·dll2 + `LoadLibraryExW` 셋) 횟수 제한 없이 `g` 로 이어도 된다. 대신 test.exe 조건은 꼭 (유저 주소라 다른 프로세스 ntdll 에도 걸림).

# 85 sxs-redirect — 정적 분석 결과

- 요청서: `_workspace/sast/85_sxs-redirect.md` (Q151b, 첫 답은 지도 수준). 짝 결과서 `84_sxs-create_result.md` 의 "다음 요청 후보 1 (Q151b)" 를 따랐다.
- 빌드: 26100.9457. **버전 확인: 일치** (아래 "버전 확인").
- 스크립트 문제: 없음. (참고: `ms/ghidra/ntdll.export.log` 는 다른 decompiler 와 같이 쓰는 로그라 줄이 섞일 수 있다. 내 출력은 `*.sast85a.stdout` 따로 남김.)
- 모듈 상태:
  - **ntdll**: 이미 분석됨 (`.gpr` + `_index.tsv` + `SetPdb:` 줄 있음) → 다시 analyze 안 함. 열기 전에 `pgrep -fa 'ghidra.app.util.headless'` 로 확인 → 그때 돌던 건 user32 · win32u 프로젝트뿐 (ntdll 잠금 없음). 전부 `-readOnly`.
  - **sxs**: 이미 분석됨 (sast/84). 질문 4 의 `[공백]` 을 닫으려고 읽기 전용으로 한 번만 열었다 (DLL 리다이렉트 구역을 **만드는** 쪽 함수 4개 + depth 1).
  - **gdi32 · gdi32full · mscoree 프로젝트는 열지 않았다.**
- 새로 뽑은 것:
  - ntdll 22개 (depth 0): `RtlDosApplyFileIsolationRedirection_Ustr` · `sxsisol_SearchActCtxForDllName` · `RtlFindActivationContextSectionString` · `RtlpFindNextActivationContextSection` · `RtlpFindUnicodeStringInSection` · `RtlpFindActivationContextSection_FillOutReturnedData` · `RtlGetAssemblyStorageRoot` · `RtlpGetActivationContextDataStorageMapAndRosterHeader` · `RtlpResolveAssemblyStorageMapEntry` · `RtlpAssemblyStorageMapResolutionDefaultCallback` · `RtlpProbeAssemblyStorageRootForAssembly` · `RtlpGetAssemblyStorageMapRootLocation` · `RtlpInitializeAssemblyStorageMap` · `RtlpInsertAssemblyStorageMapEntry` · `sxsisol_RespectDotLocal` · `sxsisol_ExpandEnvironmentStrings_UEx` · `RtlpCrackActivationContextStringSectionHeader` · `RtlpCompareActivationContextStringSectionEntryByPseudoKey` · `RtlpValidateActivationContextData` · `RtlComputePrivatizedDllName_U` · `LdrIsModuleSxsRedirected` · `RtlpLocateActivationContextSectionForQuery`.
  - 이미 있던 `.asm` 재사용: `LdrpLoadDependentModuleInternal` · `LdrpFindDllActivationContext` · `RtlpLocateActivationContextSection` · `RtlGetActiveActivationContext` · `LdrpLoadKnownDll` · `LdrpCheckKnownDllFullPath` · `LdrpFindKnownDll` · `LdrpProcessWork` · `LdrpMapDllNtFileName` · `LdrpPreprocessDllName` 등.
  - sxs 16개 (지정 4개 + depth 1): `SxspDllRedirectionContributorCallback` · `SxspDllRedirectionStringSectionGenerationCallback` · `_DLL_REDIRECTION_ENTRY` · `CompareStringSectionEntries` + 그들이 부르는 것 (`ContributorCallback` = `CDllRedir::ContributorCallback`, `CGenericStringBuffer*`, `memcpy`, `operator_delete`, `FusionpTraceCallEntry/Exit` …).
  - 지도: `ms/disassemble/ntdll/_map/RtlDosApplyFileIsolationRedirection_Ustr_{edges,nodes}.tsv`, `_summary.txt` (깊이 6, 106함수).
  - 로그: `ms/ghidra/ntdll.map.sast85a.stdout`, `ntdll.export.sast85a.stdout`, `sxs.export.sast85a.stdout`.
- **요청서 범위를 넓힌 곳 두 가지 (이유)**:
  1. `sxs` 의 DLL 구역 쓰는 쪽 → "블롭 안에 전체 경로냐 키만이냐" 를 로더 쪽 코드만으로는 "둘 다 읽을 수 있다" 까지만 말할 수 있어서, 윈도우가 실제로 어느 쪽으로 쓰는지 보려고.
  2. **사진 덤프 오프라인 읽기** (`ms/dump/…` 의 VMware 메모리 사진). 요청서 "동적으로 확인할 것" 에 "run44 사진 덤프 오프라인으로 되면 그렇다고" 라고 되어 있어서 해 봤다 → **된다.** 실제 블롭 모양으로 표를 검증했다 (아래 "사진 덤프 대조").
- 임시 도구는 `ms/disassemble/ntdll/_sast85/` (커밋 안 함, README.txt 있음): 블롭 풀어보기 · 'Actx' 찾기 · **미니 해석기** (아래) · 최소 블롭 파일 `min_A.bin` / `min_B.bin`.

## 버전 확인

- 02 대조표: `LdrpLoadDll +0xfb/ret +0x100 → LdrpLoadDllInternal` 일치 (`_calls.tsv`).
- run23 `LdrpLoadDependentModuleInternal +0x1426` 이 `RtlDosApplyFileIsolationRedirection_Ustr` 를 부르는 자리 — 일치 (call `+0x1426`, ret `+0x142b`).
- 사진 덤프 값이 이 빌드의 WinSxS 사본과 맞음: 블롭 안 어셈블리 폴더 이름 `…common-controls…_6.0.26100.9278_none_3e0d1ba8e3303201` 와 `…_5.82.26100.8328_none_87eede957a2ccddd` 가 `ms/bin/…/WinSxS/` 에 그대로 있음.

## 한 장 지도 (누가 → 누구, 오프셋은 call / ret)

```
[의존 DLL 하나를 적재: 정적 import]  LdrpLoadDependentModuleInternal
  (API set 이름이면 먼저 호스트 이름으로 바꿈 +0x2fa/+0x2ff ApiSetpResolveHost)
  +0x1426/+0x142b  RtlDosApplyFileIsolationRedirection_Ustr   (rva 0x3eb80)   인자: ecx=1, rdx=이름, r8=".DLL", [rsp+0x20]=결과 문자열
      +0x1dd/+0x1e2  RtlFindCharInUnicodeString  ('.' 찾기. 없으면 +0x273/+0x299 에서 ".DLL" 을 붙임)
      +0x361/+0x366  RtlGetFullPathName_UstrEx   (이름이 드라이브/슬래시 모양일 때만)
      +0x443 TEST [ProcessParameters+8],0x1000 → +0x6cc/+0x6d1 sxsisol_RespectDotLocal   (.local 이 있을 때만)
      +0x484/+0x489  sxsisol_SearchActCtxForDllName            (rva 0x40f80)   ← 활성화 문맥 뒤지기 (.local 이 찾았으면 건너뜀)
          +0xc9/+0xce    RtlFindActivationContextSectionString(3, 0, 2, 이름, 결과)   (rva 0x41680)
              +0x30/+0x134/+0x141  PEB+0x2f8 · PEB+0x308 · TEB+0x2c8→ActiveFrame 셋 다 비었는지 먼저 본다
              +0xed/+0xf2    RtlpFindNextActivationContextSection        (rva 0x41f60)   "다음에 볼 문맥" 고르기
                  +0x95/+0x9a    RtlpLocateActivationContextSection       (rva 0x42120)   그 문맥의 목차에서 구역 id 2 찾기
              +0x20e/+0x213  RtlpFindUnicodeStringInSection              (rva 0x419e0)   구역 안에서 이름(키) 찾기
              +0x255/+0x25a  (못 찾으면) RtlpFindNext… 다시 → 다음 문맥
              +0x2e0/+0x2e5  RtlpFindActivationContextSection_FillOutReturnedData (rva 0x6fe88)   결과 칸 채우기
          +0x215/+0x21a  RtlGetAssemblyStorageRoot                 (rva 0xb6148)   ← 항목 플래그 비트 1 일 때만 (조립 폴더 루트 얻기)
              +0x8a/+0x8f   RtlpGetActivationContextDataStorageMapAndRosterHeader (PEB+0x300/+0x310 저장소 맵을 처음 부를 때 만든다)
              +0xd7/+0xdc   RtlpResolveAssemblyStorageMapEntry        (rva 0xb6848)   WinSxS\<폴더> 열어 보기 (NtOpenFile)
          +0x3f9/+0x3fe  sxsisol_ExpandEnvironmentStrings_UEx      (항목 플래그 비트 2 일 때만)
      +0x603/+0x608  sxsisol_FreeUnicodeStringBufferAroundUnicodeStrings_Success  (결과를 호출자 문자열에 확정)
  ← 돌아와서 +0x142b MOV R14D,EAX → 성공이면 +0x148e/+0x1493 RtlGetFullPathName_Ustr (경로 다듬기)
     +0x69a CMP [RSP+0x360],0 ≠ 0 → +0x15aa LdrpLogInternal("…resolved by SxS") 와 +0x15af BTS ESI,9 · +0x15bc OR ESI,4
  …→ +0x16e9/+0x16ee LdrpFindLoadedDllByName (이미 올라왔나)
  …→ +0x1157/+0x115c LdrpAllocateModuleEntry
  …→ +0x178d/+0x1792 LdrpLoadKnownDll        ← KnownDll 검사는 여기, 리다이렉트 **뒤**
  …→ (KnownDll 아님 = 0xc0000135) +0x1ca9/+0x1cae LdrpProcessWork  또는  +0x1827/+0x182c LdrpQueueWork
        LdrpProcessWork +0x5b BT EAX,9 → +0x13f/+0x144 LdrpMapDllFullPath → LdrpMapDllNtFileName +0x132 NtOpenFile / +0x18f NtCreateSection

[LoadLibrary 쪽]  LdrpLoadDll +0xbd/+0xc2 LdrpPreprocessDllName → +0x622/+0x627 RtlDosApplyFileIsolationRedirection_Ustr (같은 모양: 1, 이름, ".DLL", 0, 동적 문자열…)
                  → +0xfb/+0x100 LdrpLoadDllInternal → +0x1ed/+0x1f2 LdrpFindOrPrepareLoadingModule → +0x5e0/+0x5e5 LdrpLoadKnownDll
```

## 질문별 답 [정적]

### 1. 리다이렉트 길 — KnownDlls 와 무엇이 먼저인가

- 답:
  1. **리다이렉트가 먼저, KnownDll 은 그 결과를 받아 본다.** 위 지도처럼 import 쪽은 `LdrpLoadDependentModuleInternal` `+0x1426` (리다이렉트) → `+0x178d` (`LdrpLoadKnownDll`), LoadLibrary 쪽은 `LdrpLoadDll +0xbd` (`LdrpPreprocessDllName` 안 `+0x622`) → `+0x1ed` (`LdrpFindOrPrepareLoadingModule` 안 `+0x5e0` 이 `LdrpLoadKnownDll`). 두 길 다 같다.
  2. **리다이렉트가 이기는 이유**: 리다이렉트가 성공해서 경로가 나오면 적재 문맥의 플래그 비트 9 (`0x200`, "이름이 아니라 전체 경로") 가 켜진다 (`LdrpLoadDependentModuleInternal +0x15af BTS ESI,9`; SxS 로 얻었으면 비트 2 도 `+0x15bc OR ESI,4`. 이 ESI 가 새 적재 문맥 `+0x20` 으로 간다고 본다 — `+0x10ec MOV [RAX+0x20],R15D` 가 문맥 할당(`+0x10d6`) 직후 쓰는 줄이고, R15D 가 `[RSP+0x98]`(`+0x94a` 에서 ESI 보관)에서 오는 길은 끝까지 추적하지 않았다 `[추측]`. 확실한 것은 `LdrpProcessWork +0x5b` 와 `LdrpLoadKnownDll +0x23` 이 **같은 문맥 `+0x20` 의 비트 9** 를 읽는다는 것, 그리고 그 문맥 `+0x0` 문자열이 `[RSP+0x360]` 버퍼(`+0x965 LEA R14,[RSP+0x360]`, 리다이렉트 뒤엔 전체 경로)라는 것). `LdrpLoadKnownDll` 은 비트 9 가 켜져 있으면 `+0x23 BT EAX,9` → `+0x32/+0x37 LdrpCheckKnownDllFullPath` 를 불러서 **경로의 폴더가 `LdrpKnownDllPath` (System32) 와 글자 하나까지 같은가** 를 본다. WinSxS 폴더는 System32 가 아니므로 거절 (`0xc0000135` 를 돌려줌) → 호출쪽은 KnownDll 을 포기하고 디스크에서 올린다 (`+0x1ca9 LdrpProcessWork` → 비트 9 라서 `LdrpMapDllFullPath`; 아니면 `+0x1827 LdrpQueueWork`).
  3. 리다이렉트가 **없으면** (경로가 안 나옴 → 비트 9 꺼짐) `LdrpLoadKnownDll` 이 이름만으로 `LdrpFindKnownDll` (`+0x5e`) → `NtOpenSection(\KnownDlls\<이름>)` (`LdrpFindKnownDll +0xd4`) 을 한다. 이게 PE_emulator 의 지금 흉내 길이다.
  4. **리다이렉트 안 하는 경우 (호출 자체를 건너뜀)**: `LdrpLoadDependentModuleInternal +0x13e7 CMP [LdrpIsSecureProcess],0; JNZ` (보안 프로세스) — `+0x661 TEST R15B,R15B; +0x664 JNZ +0x13e7`. R15B 는 처음 1 (`+0x264`), 이름이 API set 이라 호스트 이름으로 바뀐 길(`+0x2fa ApiSetpResolveHost`; 로그 문자열이 "API set" / "SxS" 로 갈린다 `+0x15aa`)에서는 `PEB.ProcessParameters.Flags & 0x1000` (`+0x62f TEST [RAX+8],0x1000`, 비트 이름은 `.local` 있음 — sast/84 와 같은 비트) 가 꺼져 있으면 0 으로 지워진다 (`+0x638 XOR R15B,R15B`).
  5. `RtlDosApplyFileIsolationRedirection_Ustr` 안 순서: 확장자 붙이기 → (경로 모양이면) 전체 경로 → **`.local` 먼저** (`+0x443` → `+0x6cc`; 찾으면 문맥은 안 봄 `+0x455 TEST R15B,1`) → 활성화 문맥 (`+0x484`) → 마무리 (`+0x603`).
  6. **결과 값**: 문맥이 없거나 키가 없으면 `0xC0150008` (`STATUS_SXS_KEY_NOT_FOUND` — 이름은 코드 상수와 맞춰 `[추측: 이름]`). 호출쪽 `+0x14f2 CMP R14D,0xc0150008; JZ` 는 이 값을 오류로 기록하지 않는다 (다른 오류는 `+0x1502 MOV [RSP+0x78],EBX` 로 기록).
- 근거: `ms/disassemble/ntdll/LdrpLoadDependentModuleInternal.asm` `+0x264`, `+0x61e`~`+0x664`, `+0x1426`~`+0x14f2`, `+0x15aa`~`+0x15bf`, `+0x178d`, `+0x1ca9`; `LdrpLoadKnownDll.asm` `+0x23`~`+0x5e`; `LdrpCheckKnownDllFullPath.asm`(전체); `LdrpFindKnownDll.asm` `+0xd4`; `LdrpProcessWork.asm` `+0x51`~`+0x65`, `+0x13f`; `RtlDosApplyFileIsolationRedirection_Ustr.asm` `+0x420`~`+0x484`, `+0x6be`~`+0x6d5`, `+0x603`.
- 지금 PE_emulator: **문맥이 없을 때의 결과는 맞다** (아래 질문 2 — 같은 `0xC0150008`). 다만 **순서를 지금처럼 "KnownDll 먼저" 로 구현했다면 틀림.** 리다이렉트 → (경로 폴더 ≠ System32 면) KnownDll 거절 순서여야 문맥을 넣었을 때 WinSxS 로 간다.

### 2. 어느 문맥을 보나

- 답:
  1. **순서는 요청서 추측이 맞다**: ① 스레드 활성화 문맥 스택의 맨 위 (TEB `+0x2c8` `ActivationContextStackPointer` → `ActiveFrame` → 그 틀의 `ActivationContext`) → ② 프로세스 기본 (`PEB+0x2f8`) → ③ 시스템 기본 (`PEB+0x308`). `RtlpFindNextActivationContextSection` 이 상태(반복자 `+0x14`: 0→1→2→3)로 하나씩 돌려준다: `+0x105` TEB+0x2c8, `+0x119` PEB+0x2f8, `+0x131` PEB+0x308, `+0x192` (스레드 틀의 값이 특수 표식 `-4` 일 때 PEB+0x308). 스레드 틀의 값이 `0xfffffffffffffffd` (`-3`) 면 "비어 있는 문맥" 표식 (`"Actx "`) 이라 찾을 게 없다 (이름은 `[추측]`). 못 찾으면 (`0xC0150001`) 다음 문맥으로 넘어간다.
  2. 어느 문맥에서 찾았는지는 결과 칸 `ulFlags` (`+0x44`) 에 남는다: 스레드 문맥 0, **프로세스 기본 1**, **시스템 기본 2** (`RtlpFindNextActivationContextSection` 반복자 `+0x18` 을 `FillOutReturnedData` 가 `& 3` 해서 씀). `hActCtx` (`+0x38`) 는 스레드 문맥일 때만 값이 있고 기본 문맥은 0.
  3. **셋 다 없으면**: `RtlFindActivationContextSectionString` 맨 앞 (`+0x30 CMP [PEB+0x2f8],0; +0x134 CMP [PEB+0x308],0; +0x141 MOV RAX,[TEB+0x2c8]; +0x148 CMP [RAX],0; +0x151 MOV EAX,0xc0150001`) 에서 `0xC0150001` (`STATUS_SXS_SECTION_NOT_FOUND` `[추측: 이름]`) 로 바로 돌아간다. 그러면 `sxsisol_SearchActCtxForDllName +0xd8 CMP EAX,0xc0150001; +0xdf LEA EBX,[RAX+0x7]` 가 **`0xC0150008` (KEY_NOT_FOUND) 로 바꿔** 돌려준다 (그냥 두면 `RtlDosApply… +0x802` 에 있는 `RtlAssert "Status != STATUS_SXS_SECTION_NOT_FOUND"` 에 걸림). → "리다이렉트 없음" 이 맞다. 문맥은 있는데 거기에 DLL 구역 (id 2) 이 없는 경우도 같은 결과 (`RtlpLocateActivationContextSection` 이 못 찾음 → 반복자 끝 → `0xC0150001` → 위와 같이 변환).
  4. **TEB `+0x2c8` 은 비어 있으면 안 된다**: `+0x141`~`+0x148` 이 포인터를 따라가기만 하고 널 검사를 안 한다 (널이면 읽기 오류). 보통 `TEB+0x290` (`_ActivationStack`, 0x28바이트, PDB) 을 가리키고 그 `ActiveFrame` (`+0x0`) 이 0.
  5. **`RtlGetActiveActivationContext`** (rva `0x6ef00`) 는 이 길에 **없다.** 부르는 곳은 `LdrpAllocateModuleEntry +0x166/+0x16b` 하나뿐 (`[모듈 항목+0x88]` 에 현재 스레드 맨 위 틀의 문맥을 복사 + AddRef). 스레드 틀만 보고 PEB 기본 문맥은 안 본다. 틀 검사는 `Flags & 0x70 == 0x20` 과 쿠키 두 칸 (`+0x18 == ~Previous`, `+0x20 == ~ActivationContext`) 이며 틀 구조는 PDB `_RTL_ACTIVATION_CONTEXT_STACK_FRAME` (Previous `+0`, ActivationContext `+8`, Flags `+0x10`) + 쿠키 확장 `+0x18`·`+0x20`.
  6. `ActivationContext` 개체 (`_ACTIVATION_CONTEXT`, PDB 에 정의 없음) 에서 읽는 칸: `+0x18` = 그 문맥의 `ACTIVATION_CONTEXT_DATA*` (`RtlpFindNextActivationContextSection` 이 `*(pcVar6+0x18)`, 저장소 맵 함수가 `+0x18`·`+0x70`). 구조 이름·나머지 칸은 `[공백]`.
- 근거: `RtlFindActivationContextSectionString.asm` `+0x30`~`+0x151`; `RtlpFindNextActivationContextSection.asm` `+0x105`~`+0x192` (+ `.c` 의 상태 흐름); `sxsisol_SearchActCtxForDllName.asm` `+0xd8`~`+0xdf`; `RtlpFindActivationContextSection_FillOutReturnedData.asm` (`ulFlags` 쓰는 줄); `RtlGetActiveActivationContext.asm`; `LdrpAllocateModuleEntry.asm +0x166`; PDB (`pdb_struct.py ntdll.dll _PEB _TEB _ACTIVATION_CONTEXT_STACK _RTL_ACTIVATION_CONTEXT_STACK_FRAME`).
- 지금 PE_emulator: **지금(셋 다 없음)의 결과는 맞다.** 하지만 실제 윈도우 프로세스는 **시스템 기본 문맥이 늘 있다** (sast/84 질문 3, 아래 질문 5) → 문맥을 넣는 순간 위 ①②③ 순서 · 결과 칸 · `ulFlags` 가 필요하다.

### 3. `ACTIVATION_CONTEXT_DATA` 최소 칸 표 (오프셋 · 크기 · 뜻 · 읽는 줄)

읽는 방법: **모든 위치 칸이 32비트 오프셋** (블롭 시작 또는 구역 시작 기준) 이다. 읽는 길에서 8바이트 절대 주소를 따라가는 곳이 없다 → 어느 주소에 매핑해도 읽힌다. 이름은 PDB 에는 `_ACTIVATION_CONTEXT_DATA` 만 있고 (아래 표 A), 나머지는 공개 이름 `[추측: 이름]`. "실제" 열은 사진 덤프에서 읽은 값(앱 블롭 = 물리 `0xd4c17000`, 시스템 기본 = `0x1e4892000`; 아래 "사진 덤프 대조").

**A. 블롭 머리** (오프셋 0, 0x20 바이트, PDB `_ACTIVATION_CONTEXT_DATA`)

| 오프셋 | 크기 | 이름 (PDB) | 뜻 | 읽는 줄 | 실제 |
|---|---|---|---|---|---|
| +0x0 | 4 | Magic | `0x78746341` (메모리에서 `41 63 74 78` = "Actx") | 이 길에서는 안 읽음. 만들 때 검사 `RtlpValidateActivationContextData` | `0x78746341` |
| +0x4 | 4 | HeaderSize | `0x20` 이상이어야 함 | `RtlpLocateActivationContextSection +0x3e` | `0x20` |
| +0x8 | 4 | FormatVersion | 1 | 만들 때 검사만 | 1 |
| +0xc | 4 | TotalSize | 블롭 전체 크기. **모든 오프셋의 경계 검사에 씀** | `Locate +0x2a` | 앱 `0x2050`, 시스템 기본 `0x3084` |
| +0x10 | 4 | DefaultTocOffset | 목차 머리 위치 (4의 배수, 0 아님) | `Locate +0x58` | 앱 `0x94`, 시스템 `0xdc` |
| +0x14 | 4 | ExtendedTocOffset | GUID 로 찾는 구역용. DLL 이름 찾기에서는 안 씀 | `Locate +0x1b4` (GUID 줄 때만) | 0 |
| +0x18 | 4 | AssemblyRosterOffset | 조립 명부 머리 위치 | `ResolveEntry +0x14a`, `GetStorageMap… +0xe1`/`+0x22f` | `0x20` |
| +0x1c | 4 | Flags | 이 길에서는 안 읽음 | — | 0 |

**B. 목차 (TOC)** — 머리는 블롭 `+DefaultTocOffset`

| 오프셋 | 크기 | 이름 `[추측: ACTIVATION_CONTEXT_DATA_TOC_HEADER/_ENTRY]` | 뜻 | 읽는 줄 | 실제 |
|---|---|---|---|---|---|
| 머리 +0x0 | 4 | HeaderSize | `0x10` 이상 (만들 때 검사) | validate | `0x10` |
| 머리 +0x4 | 4 | EntryCount | 항목 개수 | `Locate +0x6f` | 앱 9, 시스템 9 |
| 머리 +0x8 | 4 | FirstEntryOffset | 항목 배열 위치 (**블롭** 기준, 4의 배수) | `Locate +0x82` | 앱 `0xa4` |
| 머리 +0xc | 4 | Flags | 비트 0 = 빽빽 (id 가 연속, `id − 첫 id` 로 바로 찾음 `+0x10d`), 비트 1 = id 순 정렬 (이분 탐색 `+0xf4`, `+0x140 bsearch`). 둘 다 0 이면 처음부터 훑음 | `Locate +0xed` | 2 |
| 항목 +0x0 | 4 | SectionId | **DLL 리다이렉트 = 2** (`sxsisol` 이 `R8D=2` 로 부름 `+0x6a`). 참고: 1 조립 정보, 3 창 클래스, 4 COM 서버, 5 COM 인터페이스, 6 타입 라이브러리, 7 ProgID, 9 CLR, 10 앱 설정, 11 호환성, 12 WinRT (번호는 덤프 목차에서 본 값, 이름은 공개 이름 기준 `[추측]`) | `Locate` 비교 | 2 |
| 항목 +0x4 | 4 | Offset | 구역 위치 (**블롭** 기준, 0 아님, `Offset < TotalSize`) | `Locate +0x151` | 앱 `0xb1c` |
| 항목 +0x8 | 4 | Length | 구역 길이 (`Offset + Length ≤ TotalSize`) | `Locate +0x15e` | 앱 `0xc4` |
| 항목 +0xc | 4 | Format | 이 길에서는 안 읽음 | — | 1 |
| (항목 크기 0x10) | | | | | |

**C. 문자열 구역 머리** (구역 시작, 0x2c 바이트) — `RtlFindActivationContextSectionString +0x1e3 CMP [R15],0x64487353` 이 Magic 을, 구역 길이 `≥ 0x2c` 를 검사한다 (`.c` 85행). 칸 읽기는 `RtlpFindUnicodeStringInSection`

| 오프셋 | 크기 | 이름 `[추측: ACTIVATION_CONTEXT_STRING_SECTION_HEADER]` | 뜻 | 읽는 줄 | 앱 / 시스템 기본 |
|---|---|---|---|---|---|
| +0x0 | 4 | Magic | `0x64487353` ("SsHd") | `Find…InSection +0x37` | 같음 |
| +0x4 | 4 | HeaderSize | `0x2c`. 이 길(플래그 3)에서는 안 읽음. 조립 메타데이터를 묻는 길(플래그 비트 2)에서만 `FillOut… ` 가 `Length ≥ HeaderSize ≥ 0x2c` 검사 | — | `0x2c` |
| +0x8 | 4 | FormatVersion | **1 이어야 해시표를 씀** | `+0x122 CMP [RDI+8],1` | 1 |
| +0xc | 4 | DataFormatVersion | 결과 칸 `ulDataFormatVersion` 으로 복사. `sxsisol` 은 **1 이어야** 진행 (`+0x172 CMP [RSP+0x64],1`) | 복사 `Find…InSection +0x2d8`/`+0x2df` | 1 |
| +0x10 | 4 | Flags | **비트 0 = 대소문자 무시** (해시 계산 `+0x100`, 문자열 비교 5번째 인자 `[RSP+0x20]=R13B`), **비트 1 = 원소가 PseudoKey 순으로 정렬됨** (이분 탐색 `+0x135 TEST [RDI+0x10],2` → `+0x188 bsearch`) | `+0x24`, `+0x135` | 앱 3 (정렬), 시스템 1 (해시표) |
| +0x14 | 4 | ElementCount | 원소 개수. **0 이면 못 찾음** (`+0x6b CMP [RCX+0x14],0` → `0xC0150008`) | `+0x6b` | 앱 2, 시스템 12 |
| +0x18 | 4 | ElementListOffset | 원소 배열 위치 (**구역** 기준) | `+0x148` (이분 탐색 길), `+0x333` (선형 길) | `0x2c` |
| +0x1c | 4 | HashAlgorithm | **1 = 곱 `65599` (0x1003f) 해시** (`+0x100 IMUL R10D,R10D,0x1003f`). 0 도 같은 계산. **`0xFFFFFFFF` = 해시 안 씀** (`+0x88 CMP EAX,-1`; 이름 비교만 함). 그 외 숫자는 "지원 안 함" 오류 | `+0x75`~`+0x8b` | 1 |
| +0x20 | 4 | SearchStructureOffset | 해시표 위치 (**구역** 기준). 0 이면 해시표 없음 → 정렬/선형 | `+0x395` | 앱 0, 시스템 `0x464` |
| +0x24 | 4 | UserDataOffset | 구역 전체 공용 자료 (결과 `lpSectionGlobalData`). 0 이면 없음 | `FillOut` 인자 | 0 |
| +0x28 | 4 | UserDataSize | 위 크기 | 〃 | 0 |

**D. 문자열 구역 원소 (0x18 바이트, 구역 `+ElementListOffset`)** — `[추측: ACTIVATION_CONTEXT_STRING_SECTION_ENTRY]`

| 오프셋 | 크기 | 이름 | 뜻 | 읽는 줄 | 실제 (`comctl32.dll`) |
|---|---|---|---|---|---|
| +0x0 | 4 | PseudoKey | 키의 해시 (HashAlgorithm=1 이고 Flags 비트 0 이면 **대문자로 바꾼 키** `COMCTL32.DLL` 의 `h = h×0x1003f + 글자`, 32비트) | `CMP [RBP],EAX` 같은 비교 `+0x2c0`, `+0x31f`, `+0x353`, `+0x3f5` | **`0xf1c4bc4f`** (직접 계산해서 일치 확인) |
| +0x4 | 4 | KeyOffset | 키 문자열 위치 (**구역** 기준, UTF-16, 끝 NUL 없음) | `+0x28c MOV R8D,[RBP+4]` | 앱 `0x5c` |
| +0x8 | 4 | KeyLength | 키 **바이트** 수 (`comctl32.dll` = 12글자 = `0x18`) | `+0x290 MOVZX R9D,word ptr [RBP+8]` (16비트만 씀) | `0x18` |
| +0xc | 4 | Offset | 값(항목) 위치 (**구역** 기준). **0 이면 결과를 안 채움** (`+0x2c9 CMP [RBP+0xc],0; JZ`) | `+0x2e7`~`+0x2ed` (lpData 쓰기) | 앱 `0x78` |
| +0x10 | 4 | Length | 값 길이. DLL 은 **0x14 이상이어야** (`sxsisol +0x16c CMP [RSP+0x70],0x14; JC`) | `+0x2f2`/`+0x2f5` (ulLength 쓰기) | `0x14` |
| +0x14 | 4 | AssemblyRosterIndex | 이 DLL 이 속한 조립 (명부 번호). **0 은 못 씀** (자리 채움) | 결과 `+0x40` (`Find…InSection +0x4a7`/`+0x4aa`) → `RtlGetAssemblyStorageRoot` 인자 | 앱 2 |

**E. 해시표 (SearchStructureOffset ≠ 0 일 때만)** — 시스템 기본 블롭이 이 모양 (앱 블롭은 원소 둘뿐이라 정렬+이분 탐색)

| 위치 (구역 기준) | 크기 | 뜻 | 읽는 줄 |
|---|---|---|---|
| `+SearchStructureOffset+0` | 4 | 칸(bucket) 개수 N (실제 3) | `+0x395 MOV EDX,[RDI+0x20]` 후 `나머지 연산` |
| `+SearchStructureOffset+4` | 4 | 칸 배열 위치 (실제 `0x46c` = 바로 뒤) | 〃 |
| 칸 i (8바이트) | 4+4 | `{체인 길이, 체인 위치}`. `i = PseudoKey % N` | 〃 |
| 체인 | 4×길이 | **원소의 구역 안 오프셋** (원소 번호 아님) 배열 | 〃 |

**F. DLL 리다이렉트 값 (원소 `Offset` 가 가리키는 곳)** — `[추측: ACTIVATION_CONTEXT_DATA_DLL_REDIRECTION / _PATH_SEGMENT]`. 읽는 곳은 전부 `sxsisol_SearchActCtxForDllName`.

| 오프셋 (항목 기준) | 크기 | 이름 | 뜻 | 읽는 줄 |
|---|---|---|---|---|
| +0x0 | 4 | Size | 항목 크기 (`0x14` 이상). 값은 이 길에서 안 읽음 | — |
| +0x4 | 4 | Flags | **비트 0 (1)** = "경로 조각이 파일 이름까지 포함" → 이름을 안 붙임 (`+0x337 TEST AL,1`). **비트 1 (2)** = "조립 폴더 루트를 앞에 붙임" (`+0x1cb TEST AL,2` → `+0x215 RtlGetAssemblyStorageRoot`). **비트 2 (4)** = 환경변수 펼침 (`+0x3d9 TEST [R15+4],4` → `+0x3f9`); 비트 1 과 같이 켜지면 거부 (`+0x1d7 TEST AL,4; JNZ +0x58e` → `+0x5c3 DbgPrintEx` + `+0x5c8 MOV EBX,0xc0150003`). **비트 3 (8)** = 이름은 마지막 `\` 뒤 조각만 붙임 (`+0x33f TEST AL,8` → `+0x466 RtlFindCharInUnicodeString`) | 위 |
| +0x8 | 4 | TotalPathLength | 조각 길이 합 (바이트). 이 길에서는 안 읽음 (`sxsisol` 이 직접 합산) | — |
| +0xc | 4 | PathSegmentCount | 조각 개수 (`< 0x20000000`) | `+0x193 MOV EDX,[R15+0xc]` |
| +0x10 | 4 | PathSegmentOffset | 조각 배열 위치 (**구역** 기준 — `lpSectionBase` 가 구역 시작이라서; `+0x186 MOV R8D,[R15+0x10]`, 경계 `+0x1ba`) | 〃 |
| 조각 +0x0 | 4 | Length | 바이트 (16비트만 씀) | 조각 읽는 반복 안 |
| 조각 +0x4 | 4 | Offset | 문자열 위치 (**구역** 기준), UTF-16, NUL 없음 | 〃 |

경로를 만드는 차례 (`sxsisol_SearchActCtxForDllName`): ① 플래그 비트 1 이면 **저장소 루트 문자열** 먼저 (`+0x2f0` 복사) → ② 조각들을 차례로 (`+0x3b5`) → ③ 비트 0 이 꺼져 있으면 **물어본 이름** 그대로 (비트 3 이면 마지막 조각만) (`+0x506`) → ④ 비트 2 면 환경변수 펼침 (`+0x3f9`) 하고 결과를 되복사 (`+0x697`) → ⑤ `*param_4 |= 2` ("리다이렉트 됨" 표시, `RtlDosApply…` 의 `param_7`). **붙는 이름은 물어본 철자 그대로** (`COMCTL32.dll` 로 물으면 `…\COMCTL32.dll`) — 키 비교는 대소문자 무시지만 결과에 키의 철자를 쓰지 않는다 (미니 해석기로 확인).

**G. 조립 명부 · 조립 정보** — 비트 1 (조립 폴더 루트) 일 때만 읽는다

| 위치 | 크기 | 이름 `[추측]` | 뜻 | 읽는 줄 | 실제 |
|---|---|---|---|---|---|
| 명부 머리 +0x0 | 4 | HeaderSize | `0x14` 이상 (만들 때 검사) | validate | `0x14` |
| +0x4 | 4 | HashAlgorithm | 1 | — | 1 |
| +0x8 | 4 | EntryCount | **명부 번호 < EntryCount** 여야 (`RtlGetAssemblyStorageRoot +0xba`) | 〃 | 앱 4, 시스템 7 |
| +0xc | 4 | FirstEntryOffset | 명부 항목 배열 (**블롭** 기준) | `ResolveEntry +0x151` | `0x34` |
| +0x10 | 4 | AssemblyInformationSectionOffset | 조립 정보 안 문자열 오프셋의 **기준점** | `ResolveEntry +0x15d` | 앱 `0x134` |
| 명부 항목 (0x18) +0x0 | 4 | Flags | 0번은 1(자리 채움), 루트 조립은 2, 나머지 0 | — | |
| +0x4 / +0x8 / +0xc | 4×3 | PseudoKey · NameOffset · NameLength | 조립 이름 해시·이름 (이 길에서는 안 읽음) | — | |
| +0x10 | 4 | AssemblyInformationOffset | 조립 정보 위치 (**블롭** 기준) | `ResolveEntry +0x158` | 앱[2] `0x3bc` |
| +0x14 | 4 | AssemblyInformationLength | 〃 길이 (이 길에서는 안 읽음) | — | |
| 조립 정보 +0x0 | 4 | Size | `0x6c` (이 길에서는 안 읽음) | — | |
| +0x4 | 4 | Flags | **비트 4 (`0x10`) 꺼짐** = 보통 WinSxS 조립. 켜짐 = 매니페스트 파일이 있는 폴더가 곧 루트 (`ResolveEntry +0x178 TEST [R8+4],0x10`; 켜졌을 때 `+0x18c` 가 `+0x18` 의 ManifestPathOffset 을 읽어 `wcsrchr('\\')` 로 폴더만 자름) | 〃 | 조립[2] 0, exe 매니페스트 조립[1] `0x11` |
| +0x50 | 4 | AssemblyDirectoryNameLength | 폴더 이름 **바이트** 수 (< `0xffff`) | `ResolveEntry +0x167`, `+0x2a1` | `0xba` |
| +0x54 | 4 | AssemblyDirectoryNameOffset | 폴더 이름 위치 (**조립 정보 구역 기준점**=명부 머리 `+0x10` 기준) | `ResolveEntry +0x2b3` | 앱[2] `0x508` |
| (조립 정보 크기 0x6c; 나머지 칸 — EncodedAssemblyIdentity `+0x8/+0xc`, ManifestPath `+0x10`~`+0x18`, 마지막 쓴 시각 `+0x1c`(8B), Policy… , 언어 — 은 이 길에서 안 읽음) | | | | | |

저장소 루트 문자열 (런타임에 ntdll 이 조립, 블롭에는 없음): **`RtlGetNtSystemRoot()` (= `SharedUserData` `0x7ffe0030` 의 `C:\WINDOWS`) + `\WinSxS\` + 조립 폴더 이름 + `\`** — `RtlpAssemblyStorageMapResolutionDefaultCallback +0x1df`~`+0x240` (후보 1번 = `\WinSxS\`), `RtlpProbeAssemblyStorageRootForAssembly` 가 폴더를 이어 붙이고 `NtOpenFile` 로 열려 보고 뒤에 `\` 를 붙임 (`+0x26c`).

**H. 결과 칸 `ACTCTX_SECTION_KEYED_DATA` (0x70 바이트, 로더가 넘겨 받는 모양) — 블롭이 아니라 함수 사이 약속**

| 오프셋 | 뜻 | 채우는 줄 |
|---|---|---|
| +0x0 | `cbSize` = 0x70 (`sxsisol +0x33`) | 호출자 |
| +0x4 | `ulDataFormatVersion` ← 구역 머리 `+0xc` | `Find…InSection` |
| +0x8 / +0x10 | `lpData` ← 구역 시작 + 원소.Offset / `ulLength` ← 원소.Length | 〃 (`+0x2ed`, `+0x2f5`) |
| +0x18 / +0x20 | `lpSectionGlobalData` / `ulSectionGlobalDataLength` ← 머리 `UserData…` | `FillOut` |
| **+0x28** | **`lpSectionBase` ← 구역 시작 (블롭 시작이 아니다!)** | `FillOut` (`param_2+0x28 = param_5`) |
| +0x30 | `ulSectionTotalLength` ← 목차 항목 Length | `FillOut` |
| +0x38 | `hActCtx` (플래그 비트 0 일 때 AddRef, 기본 문맥이면 0) | `FillOut` |
| +0x40 | `ulAssemblyRosterIndex` ← 원소 `+0x14` | `Find…InSection` |
| +0x44 | `ulFlags` = 1 프로세스 기본 / 2 시스템 기본 / 0 스레드 | `FillOut` |

**I. 런타임에 ntdll 이 알아서 만드는 것 (블롭에 안 넣는다)** — 질문 84-5 (c) 닫힘

- `PEB+0x300` (`ProcessAssemblyStorageMap`) · `PEB+0x310` (`SystemAssemblyStorageMap`): 처음 `RtlGetAssemblyStorageRoot` 를 부를 때 `RtlpGetActivationContextDataStorageMapAndRosterHeader +0x266` 이 `RtlAllocateHeap(PEB.ProcessHeap, 0, EntryCount×8+0x10)` 로 만들고 `+0x281 RtlpInitializeAssemblyStorageMap` 으로 0 채움, `LOCK` 비교교환으로 PEB 칸에 넣는다. 모양: `{+0 Flags, +4 AssemblyCount, +8 배열 포인터(=맵+0x10)}`, 배열 칸 = 항목 포인터 (`{+8 UNICODE_STRING 루트, +0x18 폴더 핸들}`, `RtlpInsertAssemblyStorageMapEntry`). **에뮬레이터가 미리 넣을 필요 없다.**

- 근거: 위 표의 줄들 + `ms/disassemble/ntdll/RtlpLocateActivationContextSection.asm`, `RtlpFindUnicodeStringInSection.asm`, `RtlpFindActivationContextSection_FillOutReturnedData.asm`, `sxsisol_SearchActCtxForDllName.asm`, `RtlGetAssemblyStorageRoot.asm`, `RtlpGetActivationContextDataStorageMapAndRosterHeader.asm`, `RtlpResolveAssemblyStorageMapEntry.asm`, `RtlpProbeAssemblyStorageRootForAssembly.asm`, `RtlpAssemblyStorageMapResolutionDefaultCallback.asm`; 쓰는 쪽 `ms/decomp/sxs/SxspDllRedirectionStringSectionGenerationCallback.c` (항목 `0x14`, 조각 8바이트, 문자열; 플래그 계산), `ContributorCallback.c`(= `CDllRedir::ContributorCallback`: 파일 이름 키, `loadFrom` 이 있으면 `this[0x230]`·`[0x231]`, `%windir%\system32\` 이름쌍은 `[0x232]`).
- 지금 PE_emulator: **전부 새 정보 (비어 있음).**

### 4. comctl32 v6 으로 가는 최소 조건

- 답:
  1. **블롭에 필요한 것**: DLL 구역 (id 2) 의 문자열 구역에 키 `comctl32.dll` 하나 + 값 하나 (표 F). 값을 쓰는 두 방법:
     - **(A) 키만 — 윈도우가 실제로 쓰는 방식 `[정적 + 덤프]`**: 값 = 플래그 **2**, 조각 0개. 폴더는 **조립 명부 항목의 폴더 이름** (`…common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201`) 에서 온다. 실제 앱 블롭이 이 모양이다 (`comctl32.dll` → 플래그 2, 조각 0, 명부[2] = Common-Controls 6.0.26100.9278). `sxs` 쪽 근거: `SxspDllRedirectionStringSectionGenerationCallback` 이 `loadFrom` 이 없으면(`*(this+0x20)==0`) `플래그 |= 2`, `PathSegmentCount = 0` 으로 쓴다.
     - **(B) 전체 경로 — 로더가 읽을 수 있는 방식 `[정적]`**: 값 = 플래그 **1**, 조각 1개 = `C:\WINDOWS\WinSxS\<폴더>\comctl32.dll`. 윈도우는 매니페스트에 `loadFrom=` 이 있는 DLL 에 이 모양으로 쓴다 (`ContributorCallback`: `loadFrom` 문자열 → 조각, 끝에 `\` 가 없으면 비트 0).
  2. **"블롭 안에 전체 경로를 넣나 키만 넣나" 는 닫힘 `[정적]`**: 윈도우가 만든 블롭은 **키만** 넣는다 — 블롭에 들어 있는 건 **조립 폴더 이름**(상대) 과 **파일 이름**(키) 뿐이고, `C:\WINDOWS\WinSxS\` 루트는 블롭에 **없다** (ntdll 이 `RtlGetNtSystemRoot() + \WinSxS\` 로 조립). 전체 경로가 블롭에 들어가는 건 `loadFrom` 이 있을 때와 시스템 기본의 `C:\WINDOWS\system32\<이름>` 키 쌍(질문 5)의 *키* 쪽뿐이다 (값은 여전히 플래그 `0xa`·조각 0).
  3. **경로를 만드는 줄** (A 방식): 어셈블리 루트 = `RtlpAssemblyStorageMapResolutionDefaultCallback +0x1df` (`RtlGetNtSystemRoot`) + `+0x240` (`\WinSxS\` 상수 복사) → 폴더 이름 붙이기 `RtlpProbeAssemblyStorageRootForAssembly +0x11a/+0x141` (복사 두 번) → 끝에 `\` → `sxsisol_SearchActCtxForDllName +0x2f0` (루트 복사) → `+0x506` (파일 이름 복사). (B 방식은 `+0x3b5` 조각 복사만.)
  4. **그 파일을 여는 syscall**:
     - 경로 푸는 동안 (A 방식만): `NtOpenKey` (`\Registry\Machine\Software\Microsoft\Windows\CurrentVersion\SideBySide\AssemblyStorageRoots`, 접근 8, 대소문자 무시 — `RtlpAssemblyStorageMapResolutionDefaultCallback +0x169/+0x16e`. 없어도(`0xC0000034`, `0xC0000189`) 괜찮음, 성공이면 `NtClose` 가 정리 때 따라옴) → **`NtOpenFile` 로 조립 폴더 열어 보기** (`RtlpProbeAssemblyStorageRootForAssembly +0x26c/+0x271`, 접근 `0x100020` = SYNCHRONIZE+TRAVERSE, 공유 3, 옵션 `0x21` = 폴더 전용+동기). 이 폴더가 **없으면** 후보를 하나씩 돌고 (레지스트리 하위 키 `NtEnumerateKey` `Callback +0xb9`) 다 실패하면 오류. 폴더 핸들은 저장소 맵에 남는다.
     - DLL 파일 자체: `LdrpProcessWork +0x13f` → `LdrpMapDllFullPath +0xf0` → `LdrpMapDllNtFileName +0x132/+0x137 NtOpenFile` → `+0x18f/+0x194 NtCreateSection` → `+0x287/+0x28c LdrpMapDllWithSectionHandle` (→ 섹션 매핑).
  5. **비교해야 할 칸 정리**: PEB `+0x2f8` = 블롭 주소 (프로세스 기본), TEB `+0x2c8` = 널 아닌 빈 스택, 그리고 위 구역. 시스템 기본 `+0x308` 은 없어도 된다 (스레드 → 프로세스 순서로 찾으니).
- 근거: 위 줄들 + 사진 덤프에서 읽은 앱 블롭 (`comctl32.dll`: 원소 `PseudoKey 0xf1c4bc4f`, 값 `Size 0x14 / Flags 2 / 조각 0`, 명부[2] 폴더 이름) + `sxs` 쓰는 쪽.
- 지금 PE_emulator: **없다 (틀림)** — 문맥이 없으니 System32 KnownDll 로 간다. 위 (A)/(B) 중 하나를 넣어야 v6 폴더로 간다. 파일은 `ms/bin/…/WinSxS/amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201/comctl32.dll` (v6, SHA-256 `17de1830…`) 이 사본에 있다.

### 5. 시스템 기본 문맥만 있을 때 (매니페스트 없는 exe)

- 답: **v5.82 로 간다 — 단, System32 KnownDll 이 아니라 WinSxS 5.82 폴더 경로로.** 근거는 사진 덤프의 **진짜 시스템 기본 블롭**이다:
  - 시스템 기본 블롭 (AMD64, 총 `0x3084` 바이트, 명부 7칸): 조립 6개 — `Microsoft.Windows.SystemCompatible 6.0.26100.1`(루트·이름표 "System Default Context"), `IsolationAutomation 1.0.26100.5074`(`sxsoa.dll`), `GdiPlus 1.1.26100.9444`(`GdiPlus.dll`), **`Common-Controls 5.82.26100.8328`(`comctl32.dll`)**, `IsolationAutomation.ProxyStub`(`sxsoaps.dll/.tlb`), `Common-Controls.Resources 5.82.26100.8117 ko-KR`(`comctl32.dll.mui`).
  - DLL 구역 (id 2): 원소 12개 = 위 DLL 6개 × (이름 키 + `C:\WINDOWS\system32\<이름>` 키). 이름 키 값은 플래그 2, 전체 경로 키 값은 플래그 **`0xa`** (2 + 8: "System32 DLL 을 WinSxS 로 돌려라" 쪽, 조각 0). 구역 머리: Flags 1 (해시표), HashAlgorithm 1, 칸 3개.
  - 그래서 `comctl32.dll` (또는 `C:\WINDOWS\system32\comctl32.dll`) 을 물으면 결과 = **`C:\WINDOWS\WinSxS\amd64_microsoft.windows.common-controls_6595b64144ccf1df_5.82.26100.8328_none_87eede957a2ccddd\comctl32.dll`**. (미니 해석기로 두 번 확인; 아래.) 폴더가 System32 가 아니므로 질문 1 의 KnownDll 이 거절되어 **디스크 경로로 올라간다.** 이 폴더의 `comctl32.dll` 은 `System32\comctl32.dll` 과 **바이트가 같다** (이 레포 사본 SHA-256 둘 다 `67e04b3a…`) — 다만 모듈 이름·경로(FullDllName)와 연 syscall (조립 폴더 `NtOpenFile`, 파일 `NtOpenFile`/`NtCreateSection`) 은 다르다.
  - **시스템 기본에는 v6 항목이 없다.** v6 은 exe 매니페스트가 `Common-Controls 6.0.0.0` 을 요구해서 csrss 가 앱 블롭(`PEB+0x2f8`)을 만들 때만 생긴다 (정책으로 6.0.26100.9278 선택).
  - sast/84 질문 3 의 `[공백]` ("시스템 기본 문맥의 실제 내용") 도 위로 닫힘: 조립 6개 (WinSxS 의 `SystemCompatible` 하나가 아니다).
- 근거: 사진 덤프(아래), 코드 줄은 질문 1·3.
- 지금 PE_emulator: **틀림 (작게).** "문맥 없음 → KnownDll COMCTL32 = System32 v5.82" 는 실제 윈도우(test.exe 같은 exe 도 `PEB+0x308` 이 있고 거기에 `comctl32.dll` 이 있음)와 **파일 내용은 같고 경로가 다르다.** 단 `sample/` 의 test.exe 는 comctl32 를 import 하지 않으므로 GUI 샘플부터 영향. `[덤프 오프라인 확인]` — 실제 test.exe `PEB+0x308` 이 정확히 이 블롭인지는 라이브 확인 필요.

### 6. nt 엔진이 흉내 낼 최소 블롭

- 판단: 문맥을 만드는 코드를 흉내 낼 필요는 없다. **미리 만든 바이트 덩어리를 읽기 전용으로 매핑하고 `PEB+0x2f8` 에 주소만 쓰면 된다** (모든 위치가 오프셋이라 어느 주소든 됨, 64KB 정렬 `25996d00000` 는 윈도우가 그렇게 매핑한다는 것뿐 로더는 안 따짐). 두 가지를 만들어 `ms/disassemble/ntdll/_sast85/min_A.bin`, `min_B.bin` 으로 두었다. **둘 다** (1) `RtlpValidateActivationContextData` 의 만들 때 검사 규칙을 통과하고 (2) 위 미니 해석기(= 로더 코드를 옮겨 쓴 것)가 `comctl32.dll` → v6 WinSxS 경로를 돌려주고 `user32.dll` 은 `KEY_NOT_FOUND` 를 돌려준다. **진짜 ntdll 로 돌려 본 건 아니다 → 동적으로 확인.**
- **해시**: `RtlHashUnicodeString` 알고리즘 번호는 **1 (`HASH_STRING_ALGORITHM_X65599`)** — 구역 머리 `HashAlgorithm = 1`, `h = h × 0x1003f + 글자` (32비트), 글자는 **먼저 대문자로** (`a`~`z` 는 −0x20, 0xC0 이상은 ntdll NLS 대문자표 `DAT_1801cd038`; ASCII 키는 단순). `comctl32.dll` → `COMCTL32.DLL` → **`0xf1c4bc4f`**. `comctl32.dll.mui` → `0xbbf34ea2` (실제 블롭과 일치). 대소문자 구분 구역(Flags 비트 0 꺼짐)이면 대문자 변환 없이 같은 식.
  - **해시를 아예 안 쓰는 길**: `HashAlgorithm = 0xFFFFFFFF` 로 두면 `PseudoKey` 를 안 보고 문자열만 비교한다 (코드 `+0x88 CMP EAX,-1`). 엔진이 해시 계산을 안 하고 싶으면 가능 — 단 윈도우가 만든 블롭과는 다르다.

**최소 블롭 A — 키만 (윈도우 방식, 총 0x21c 바이트)**

| 오프셋 | 크기 | 칸 | 값 |
|---|---|---|---|
| 0x00 | 0x20 | 블롭 머리 | Magic `0x78746341`, HeaderSize `0x20`, FormatVersion 1, TotalSize `0x21c`, DefaultTocOffset `0x64`, ExtendedTocOffset 0, AssemblyRosterOffset `0x20`, Flags 0 |
| 0x20 | 0x14 | 명부 머리 | HeaderSize `0x14`, HashAlgorithm 1, EntryCount **2**, FirstEntryOffset `0x34`, AssemblyInformationSectionOffset `0x84` |
| 0x34 | 0x18 | 명부[0] | Flags 1, 나머지 0 (자리 채움; 번호 0 은 못 씀) |
| 0x4c | 0x18 | 명부[1] | Flags 0, PseudoKey 0, NameOffset 0, NameLength 0, AssemblyInformationOffset `0x84`, AssemblyInformationLength `0x126` |
| 0x64 | 0x10 | 목차 머리 | HeaderSize `0x10`, EntryCount 1, FirstEntryOffset `0x74`, Flags 2 |
| 0x74 | 0x10 | 목차[0] | SectionId **2**, Offset `0x1ac`, Length `0x70`, Format 1 |
| 0x84 | 0x6c | 조립 정보 | Size `0x6c`, Flags **0**, `+0x50` DirNameLength `0xba`, `+0x54` DirNameOffset `0x6c`, 나머지 0 |
| 0xf0 | 0xbc | 폴더 이름 | UTF-16 `amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201` (0xba 바이트) + 패딩 2 |
| 0x1ac | 0x2c | 문자열 구역 머리 | Magic `0x64487353`, HeaderSize `0x2c`, FormatVersion 1, DataFormatVersion 1, Flags **3**, ElementCount 1, ElementListOffset `0x2c`, HashAlgorithm 1, SearchStructureOffset 0, UserDataOffset 0, UserDataSize 0 |
| 0x1d8 | 0x18 | 원소[0] | PseudoKey `0xf1c4bc4f`, KeyOffset `0x44`, KeyLength `0x18`, Offset `0x5c`, Length `0x14`, AssemblyRosterIndex **1** |
| 0x1f0 | 0x18 | 키 | UTF-16 `comctl32.dll` |
| 0x208 | 0x14 | DLL 값 | Size `0x14`, Flags **2**, TotalPathLength 0, PathSegmentCount 0, PathSegmentOffset 0 |

→ 결과 `C:\WINDOWS\WinSxS\amd64_…_6.0.26100.9278_none_3e0d1ba8e3303201\comctl32.dll`. 필요한 런타임: `NtOpenKey`(레지스트리 키) · 폴더 `NtOpenFile` 이 응답돼야 함 (질문 4), `PEB.ProcessHeap` 에서 힙 할당 2번(저장소 맵·항목), 저장소 맵 `PEB+0x300` 은 ntdll 이 채움.

**최소 블롭 B — 전체 경로 (총 0x1dc 바이트, 더 작고 시스템 호출 적음)**

| 오프셋 | 크기 | 칸 | 값 |
|---|---|---|---|
| 0x00 | 0x20 | 블롭 머리 | A 와 같되 TotalSize `0x1dc`, DefaultTocOffset `0x4c` |
| 0x20 | 0x14 | 명부 머리 | HeaderSize `0x14`, HashAlgorithm 1, EntryCount **1**, FirstEntryOffset `0x34`, AssemblyInformationSectionOffset `0x4c` (안 읽음) |
| 0x34 | 0x18 | 명부[0] | Flags 1 (자리 채움) |
| 0x4c | 0x10 | 목차 머리 | HeaderSize `0x10`, EntryCount 1, FirstEntryOffset `0x5c`, Flags 2 |
| 0x5c | 0x10 | 목차[0] | SectionId **2**, Offset `0x6c`, Length `0x170`, Format 1 |
| 0x6c | 0x2c | 문자열 구역 머리 | A 와 같음 |
| 0x98 | 0x18 | 원소[0] | PseudoKey `0xf1c4bc4f`, KeyOffset `0x44`, KeyLength `0x18`, Offset `0x5c`, Length **`0x114`**, AssemblyRosterIndex 1 (안 읽음) |
| 0xb0 | 0x18 | 키 | UTF-16 `comctl32.dll` |
| 0xc8 | 0x14 | DLL 값 | Size `0x14`, Flags **1**, TotalPathLength `0xf8`, PathSegmentCount 1, PathSegmentOffset `0x70` (구역 기준) |
| 0xdc | 8 | 경로 조각 | Length `0xf8`, Offset `0x78` (구역 기준) |
| 0xe4 | 0xf8 | 문자열 | UTF-16 `C:\WINDOWS\WinSxS\amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201\comctl32.dll` |

→ 결과 같은 경로. 명부·저장소 맵·레지스트리·폴더 열기 **전부 안 탄다**. 대신 (1) 블롭이 "경로 모양 상수"가 되어 `QueryActCtx` 류 (명부를 읽는 다른 API) 에는 못 쓴다, (2) 폴더가 실제로 있는지 확인은 안 하고 곧장 파일 열기에서 터진다. `[정적]` 로더 길에서는 둘 다 같은 최종 파일을 연다.

- **추천**: 첫 구현은 **B**, 다음에 **A** 로 바꾸기 — 둘 다 같은 `PEB+0x2f8` 한 칸만 바뀐다. 시스템 기본 (`PEB+0x308`) 까지 흉내 내려면 덤프에서 뜬 실제 `0x3084` 블롭 (위 표 5) 을 그대로 매핑하는 게 가장 싸다 (구역 12원소 해시표라 손으로 만들기보다 복사).
- 근거: 질문 3 의 표 + `min_A.bin`·`min_B.bin` + `buildmin.py`.
- 지금 PE_emulator: **새 설계 (비어 있음).**

## 사진 덤프 대조 (오프라인, 라이브 아님)

- 방법: `ms/dump/02_snapshot-test/BOB Windows 11 24H2-Snapshot5.vmem` (VMware 물리 메모리 8GB) 에서 12바이트 `41 63 74 78 20 00 00 00 01 00 00 00` ("Actx", HeaderSize 0x20, FormatVersion 1) 를 찾음 → 후보 약 200개 (총 크기 `0x2d4`~`0x9518`). 목차 구성별로 묶으면: `[1,4,5,6,9,11]` 110개 (DLL 안 임베디드 매니페스트 등 작은 것들), `[1,2,3,4,5,6,9,11]`·`[…,10,11]` 약 50개 (**comctl32 v6 를 쓰는 앱 블롭**), 총 `0x3084` + 명부 7칸 + `[1,2,3,4,5,6,7,9,11]` 몇 개 (**시스템 기본 블롭 AMD64**, 이름표 "System Default Context"). `P0/memory.dmp` (run44/45 사진) 에도 시스템 기본 블롭이 3개 있고 같은 결과가 나옴.
- 한계: 물리 연속을 가정하고 읽었다 (한 블롭이 여러 페이지면 복사본에 따라 뒤쪽이 이어지지 않는다 — `0x1e4892000`·`0x1e260c000` 은 끝까지 맞고 `0xf1f3d000` 은 2쪽째부터 다른 데이터). 어느 프로세스가 이 블롭을 쓰는지(가상 주소 ↔ 물리 주소)는 안 봤다 → **test.exe 의 `25996d00000` 이 이 블롭이라는 건 아직 `[확인 필요]`.** 이 판에서는 `[덤프 오프라인]` 로 쓴다 (`[관찰]` 아님).
- **미니 해석기**: `RtlpLocateActivationContextSection`·`RtlpFindUnicodeStringInSection`·`sxsisol_SearchActCtxForDllName` 의 줄을 파이썬으로 옮긴 것 (`_sast85/miniloader.py`). 실제 블롭 5개에 물려서 얻은 값과 코드에서 예상한 값이 모두 맞았다:
  - 앱 블롭 `0xd4c17000`: `COMCTL32.dll` → `C:\WINDOWS\WinSxS\…6.0.26100.9278_none_3e0d1ba8e3303201\COMCTL32.dll`, `comctl32.dll.mui` → ko-KR 폴더, `user32.dll` → KEY_NOT_FOUND.
  - 시스템 기본 `0x1e4892000`·`0x1e260c000`·P0 3개: `comctl32.dll`, `C:\WINDOWS\system32\comctl32.dll` → `…5.82.26100.8328_none_87eede957a2ccddd\comctl32.dll`; `gdiplus.dll` → GdiPlus 1.1.26100.9444 폴더; `kernel32.dll` → KEY_NOT_FOUND.
  - 직접 계산한 해시가 블롭에 적힌 `PseudoKey` 와 일치 (`0xf1c4bc4f`, `0xbbf34ea2`).
  - 이게 "진짜 ntdll 이 같은 결과를 낸다" 의 증명은 아니다 (우리가 코드를 읽고 다시 쓴 것).

## 동적으로 확인할 것 (Q104 에 붙임)

1. **리다이렉트 결과 문자열**: notepad 류(comctl32 v6)에서 `LdrpLoadDependentModuleInternal +0x142b` (ret) 직후 `[rsp+0x140]` (결과 UNICODE_STRING) 와 `eax` — 정적으로 낸 `…6.0.26100.9278…\comctl32.dll` 와 맞는지. 추천 멈춤점: `ntdll!RtlDosApplyFileIsolationRedirection_Ustr` (rva `0x3eb80`) 입구 (`rdx`=이름) 와 `ntdll!sxsisol_SearchActCtxForDllName +0x489` (ret, `eax`).
2. **KnownDll 거절**: 같은 로드에서 `LdrpLoadKnownDll` (rva `0x9970`) 이 `0xc0000135` 를 돌려주는지, `LdrpCheckKnownDllFullPath` (`0x9ac0`) 결과 `al=0` 인지. 매니페스트 없는 exe 에서 `LoadLibrary("comctl32.dll")` 해서 **WinSxS 5.82 폴더 경로가 나오는지** (질문 5 의 핵심; sample/test.exe 는 comctl32 를 안 쓰므로 실제 확인용 작은 exe 나 notepad 아닌 cmd 류 필요).
3. **시스템 기본 블롭 대조**: test.exe 가 P0 에 있을 때 `PEB+0x308` 의 값(`25996d00000`)이 가리키는 블롭을 `!db`/`.writemem` 으로 뜬 것 ↔ 위 `0x3084` 사진 블롭. P0 `memory.dmp` 를 오프라인으로 가상 주소 변환해도 됨 (이 레포에 변환 도구는 아직 없음).
4. **저장소 맵 · 레지스트리 · 폴더 열기** (A 방식 흉내용): `RtlpAssemblyStorageMapResolutionDefaultCallback +0x16e` (ret, `NtOpenKey` 상태), `RtlpProbeAssemblyStorageRootForAssembly +0x271` (ret, `NtOpenFile` 상태·경로), 처음 부를 때 `PEB+0x300` 값이 0 → 채워지는 순간 (`RtlpGetActivationContextDataStorageMapAndRosterHeader +0x26b`).
5. **최소 블롭을 진짜 ntdll 에 먹여 보기**: 타깃 VM 에서 간단한 exe 의 `PEB+0x2f8` 을 비워 두고 `min_B.bin` 을 `NtAllocateVirtualMemory`+쓰기 후 주소를 `PEB+0x2f8` 에 쓰고 `LoadLibrary("comctl32.dll")` → 모듈 목록의 `FullDllName` 확인. (정적 요청서 범위 밖 — dast 가 정함.)
6. **PEB 쓰기 순간** (Q83) 은 sast/84 와 같다.

## Q90 닫힘 (문맥 쪽)

- 질문: ntdll 이 PEB `SystemDefaultActivationContextData` (`+0x308`) 를 **읽는 곳**.
- 답 `[정적]` (내보낸 `.asm` 662개 안에서 찾은 범위. ntdll 전체 전수는 아님):
  - `RtlFindActivationContextSectionString +0x134` (셋 다 없는지 보는 맨 앞 검사)
  - `RtlpFindNextActivationContextSection +0x131`, `+0x192` (다음 문맥 고르기)
  - `RtlpGetActivationContextDataStorageMapAndRosterHeader +0x215`/`+0x21f` (`+0x308`/`+0x310` 주소 계산, 플래그 비트 1)
  - `RtlpFindActivationContextSection_FillOutReturnedData +0x1d7` (`MOV ECX,0x308`, 어셈블리 정보 구역 위치)
  - 같은 방식으로 `PEB+0x2f8`: 위 4곳 + `LdrpFindDllActivationContext +0x16c` (`CMP [PEB+0x2f8],0` — **exe 자신** 의 매니페스트를 다시 안 찾으려는 검사).
  - `pShimData` (`+0x2d8`) 읽는 곳은 이번 범위가 아님: 후보만 — `LdrpInitializeProcess +0x69f`, `LdrpCheckComponentOnDemandEtwEvent +0x6c`, `RtlpHpOptIntoSegmentHeap +0x184`, `SbSelectProcedure +0x5f/+0x8f/+0x140/+0x199`, `SbObtainTraceHandle +0x22` (줄에서 `GS:[0x60]` 로 PEB 를 읽는 것만 확인, 용도는 안 읽음 `[공백]`).
- 따라서 Q90 의 **문맥 쪽은 닫힘**, `pShimData` 쪽은 후보만 남김.

## 다음 요청 후보

1. **dast** (Q104): 위 "동적으로 확인할 것" 1~3 — 특히 2 (매니페스트 없는 exe 에서 comctl32 경로) 와 3 (test.exe `PEB+0x308` 블롭 대조). 이게 질문 5·6 의 마지막 확인.
2. **정적 (sxs 쓰는 쪽 2차)**: `SxspBuildActCtxData` 이후 **조립 명부·조립 정보 칸을 채우는 줄** — 조립 폴더 이름(`+0x54`)·플래그 `0x10` 이 어떻게 정해지나. 이번엔 로더가 읽는 칸과 실제 블롭으로 모양을 굳혔고, 쓰는 줄은 DLL 구역만 읽었다.
3. **정적 (정책)**: "6.0.0.0 요청 → 6.0.26100.9278 선택" 은 `SxspParseApplicationPolicy` · 정책 폴더 (sast/84 이름). 앱 블롭 `[2]` 이 `9278` 로 정해지는 과정. (PE_emulator 는 덤프 값을 쓰면 되므로 우선순위 낮음.)
4. **PE_emulator 쪽 확인 (요청서 범위 밖)**: `TEB+0x2c8` 가 빈 스택을 가리키는지, `PEB+0x2f8`/`+0x308` 매핑 후 `ProcessParameters.Flags` 비트 `0x2000` (앱 매니페스트 있음) 을 같이 쓰는지.

## 판정

**분석성공 (지도 수준 + 데이터 모양 검증).** 질문 1·2 는 함수·오프셋·ret 까지, 3 은 PDB 이름이 있는 머리와 코드·실제 블롭으로 검증한 칸 표, 4 는 `[공백]` 이었던 "전체 경로냐 키만이냐" 를 **닫음 (키만)**, 5 는 실제 시스템 기본 블롭으로 "WinSxS 5.82 로 간다" 까지 (`[덤프 오프라인]`, 라이브 확인 필요), 6 은 최소 블롭 A/B 파일과 해시 알고리즘 번호 (1). Q90 문맥 쪽 닫힘.

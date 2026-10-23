# 18 load-dependent — 정적 분석 결과

- 요청서: `_workspace/sast/18_load-dependent.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`), pdb guid_age=`C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). 이미 분석된 모듈이라 analyze 는 안 했다 (`.gpr`, `_index.tsv`, `SetPdb:` 줄 확인).
- 뽑은 함수: `LdrpLoadDependentModuleInternal` 하나 (+ 예외 조각 `$fin$0`). `.asm`·`.c`·`_calls.tsv` 75줄은 이미 있어서 **다시 뽑지 않았다** (sast/17 에서 export 해 둔 것). callee 본문은 하나도 안 읽었다.
- 버전 확인: `LdrpMapAndSnapDependency +0x349 (call) / +0x34e (ret) → LdrpLoadDependentModuleInternal`, `LdrpLoadDll +0xfb / +0x100 → LdrpLoadDllInternal` 이 `_calls.tsv` 와 **일치**. 이 함수 호출 줄 수도 요청서와 같다 (본체 74 + `$fin$0` 1 = 75).
- 새로 만든 것: 없음 (스크립트 변경 없음). 칸 이름·크기는 PDB 타입 (`llvm-pdbutil-14 dump -types`, 스크래치 파일, 커밋 안 함).

## 스크립트 문제

없음.

## 읽는 법

- 오프셋은 **`+0xCALL (call) / +0xRET (ret)`** 둘 다. 라벨로 쓸 땐 ret. 메모리에 쓰는 줄은 그 명령 자리 하나.
- 인자 이름 (이 문서에서만): 1 = 자식 이름 UNICODE_STRING*, 2 = 부모 로드 문맥 `ctx`, 3 = 부모 항목, 4 = LoadReason, 5 = 자식 항목을 돌려받는 칸 `&ctx.58[번호]`, 6 = 임시 포인터의 주소 `&[RSP+0x48]` (caller 쪽).
- 이 함수 안에서 인자가 있는 곳: `[RSP+0xa8]`=1, `[RSP+0xf8]`=2, `[RSP+0x130]`=3, `[RSP+0x84]`=4, `[RSP+0xb0]`=5, `[RSP+0xe0]`=6. 이름을 다듬어 담는 지역 버퍼 = `UNICODE_STRING` 이 `[RSP+0x360]`, 글자 칸이 `[RSP+0x370]` (0xfe 바이트, 모자라면 힙으로 키움).
- PDB 로 확인한 칸 (`[정적]`): `_LDR_DATA_TABLE_ENTRY` `+0x48 FullDllName`, `+0x58 BaseDllName`, `+0x68 Flags`, `+0x6e TlsIndex`, `+0x88 EntryPointActivationContext`, `+0x98 DdagNode`, `+0xa0 NodeModuleLink`, `+0xb0 LoadContext`, `+0x108 BaseNameHashValue`, `+0x10c LoadReason`, `+0x114 ReferenceCount`, `+0x130 HotPatchState`. `_LDR_DDAG_NODE` `+0x18 LoadCount`, `+0x28 Dependencies`, `+0x30 IncomingDependencies`, `+0x38 State`. `_LDRP_CSLIST` 는 칸이 `Tail` 하나 (`_SINGLE_LIST_ENTRY*`). `_LDR_DDAG_STATE`: **음수(-1 Unloading ~ -5 Merged)는 실패·내려가는 중**, 0 PlaceHolder, 1 Mapping, 2 Mapped, 3 WaitingForDependencies, 4 Snapping, 5 Snapped … 9 ReadyToRun.
- **간선 한 칸의 구조체 이름은 PDB 에 없다.** 오프셋+역할로만 쓴다 (이름 안 지어냄).
- 로드 문맥(`ctx`) 칸은 sast/17 표 그대로 (PDB 에 칸 정보 없음): `+0x10` 경로 구조체, `+0x20` 플래그, `+0x28` 상태를 쓸 곳, `+0x30` 부모 항목, `+0x38` 이 문맥의 항목, `+0x50` (이번에 뜻이 잡힘: "기다리는 자식 항목"), `+0x6c` 남은 자식 수 (+1).
- 이 함수가 **지역 변수 `flags` (`ESI`, `[RSP+0x5c]`)** 로 들고 다니는 비트 (부모 `ctx+0x20 & 0x1800800` 에서 시작 → 자식 문맥 `+0x20` 으로 감). 뜻은 쓰고 읽는 줄에서 읽은 것이라 이름은 `[추측]`:
  | 비트 | 켜는 자리 | 읽는 자리 | 뜻 `[추측]` |
  |---|---|---|---|
  | 0x4 | `+0x15bc` (SxS 로 바뀐 이름일 때만) | `LdrpAllocateModuleEntry` (sast/17: 비트 2 → 항목 `Redirected`) | 매니페스트(SxS)가 이름을 바꿈 |
  | 0x20 | `+0x6fa` (이름에 `\`·`/` 가 없을 때) | `+0x99f` | 맨 이름 (경로 없음) |
  | 0x200 | `+0x15af` 뒤 (`+0x15af BTS ESI,9`), `+0x16af` | `+0x9a9` | 경로(전체 이름)를 알고 있음 |
  | 0x400 | `+0x16af` (`LdrpGetFullPath` 성공) | `+0xe1b`, `+0x1812` | 경로를 직접 줬음 → 일감으로 안 넘기고 바로 올림 |
  | 0x8000 | `+0x10e7` (새 자식 문맥 만들 때) | — | 자식 문맥 표시 (뜻 모름) |
  | 0x800000 | 부모에서 물려받음 | `+0x1ec` 앞뒤, `+0x8f8` | enclave 갈래 |
  | 0x4000000 | `+0x1c9d` (바로 올리기 직전, 자식 문맥에) | — | 뜻 모름 |

## 질문별 답 [정적]

### 1. 큰 순서

근거: `ms/disassemble/ntdll/LdrpLoadDependentModuleInternal.asm` 전체, 구조 확인용 `ms/decomp/ntdll/LdrpLoadDependentModuleInternal.c`. **함수 안에 `LdrpPreprocessDllName`(이름 다듬기)와 `LdrpFindOrPrepareLoadingModule`(찾기·새 항목) 모양의 코드가 끼워져 들어가 있다** (`LdrpLogInternal` 문자열이 `LdrpPreprocessDllName` / `LdrpFindOrPrepareLoadingModule` 로 나옴: `+0x8c1`, `+0x15aa`, `+0xde8`). 코드 주소 순서는 뒤죽박죽이라 아래는 **실행 순서**로 다시 정리한 것.

**갈림길 (어느 길로 끝나나)**

```
LdrpLoadDependentModuleInternal(이름, 부모ctx, 부모항목, 이유, &결과항목, &임시블록)
│
├─ [항상] 부모 항목의 활성화 컨텍스트를 이 스레드 스택에 올림 (+0x71~+0x1d3, 호출 없음)
│        (꼭대기 프레임 Flags&0x70 ≠ 0x20 이면 +0x23f0 RtlRaiseException 0xc0150014)
├─ 이유 == 9 (PatchImage) ─────────────┐ 이름 다듬기 건너뜀
├─ ctx.flags & 0x800000 (enclave) ────┤ 이름 다듬기 건너뜀
├─ 아니면 이름 다듬기 ─ (a) ApiSet 풀이  (b) SxS 리다이렉트  (c) 경로·확장자
│        └ 어느 단계든 음수 → "LdrpPreprocessDllName ... failed" 로그 → 맨 끝으로 (간선 없음)
│
├─ [enclave] +0x1be2 LdrpFindOrPrepareEnclaveModule → 맨 끝으로 (간선 없음)
├─ *arg6 가 비어 있으면 0x20바이트 칸 할당 (+0x929), *arg5 = 0
│
├─ 찾기 (이미 올라와 있나)
│    ├ 이유 == 9      : 안 찾음, 자리표시 이름, 상태 0xc0000135 ─────────────┐
│    ├ flags 0x20     : +0x16e9 LdrpFindLoadedDllByName (맨 이름) ──┐        │
│    ├ flags 0x200    : 인라인 해시표 훑기 (+0xb2d~, 전체 이름)  ──┤        │
│    └ 둘 다 없음     : 안 찾음, 상태 0xc0000135 ─────────────────────────────┤
│                                                               │            │
├─ ① 찾았다 (상태 ≠ 0xc0000135) ◀──────────────────────────────┘            │
│       노드 State < 0 → "순환/실패" 로그, 상태 0xc00000e5, *arg5=0 → 맨 끝으로  │
│       아니면 +0x150e LdrpIncrementModuleLoadCount ─────────────┐            │
│                                                                 │            │
├─ ③ 못 찾았다 (0xc0000135) ◀─────────────────────────────────────────────────┘
│       새 문맥 할당 (+0x10d6) → +0x1157 LdrpAllocateModuleEntry → *arg5 = 새 항목
│       항목.LoadReason = arg4 (+0x1179)
│       ├ 이유 == 9 : 상태 0xc0000135 (KnownDll 안 부름)
│       └ 그 외    : ② +0x178d LdrpLoadKnownDll(새 문맥) ──▶ 상태 (0 이면 KnownDll 로 올림 끝, 0xc0000135 이면 KnownDll 아님)
│       └ flags 0x400 이고 상태 == 0xc0000135 : +0x1ca9 LdrpProcessWork(문맥, 1) 바로 올림 → 상태 = 문맥의 상태
│                                                               │
├─ [공통 꼬리: *arg5 ≠ 0 이면 항상, 상태가 음수여도] ◀──────────┘
│       LdrpModuleDatatableLock 독점 (인라인 +0xe94 / 막히면 +0x176d)
│       항목 교체 확인 (+0x15d8) → 간선 쓰기 (+0xf2c~+0xfbb)
│       상태 == 0xc0000135 이고 flags 0x400 아님 → +0x1827 LdrpQueueWork(항목.LoadContext)   ★락 안
│       상태 ≥ 0 이면 부모 ctx.+0x6c 줄이기 / 기다림 표시 (+0x12df~, +0x1606~)
│       참조(+0x114) 하나 내려놓기 (+0x1310) → 마지막 참조였으면 항목 정리 (+0x187d~+0x1f70)
│       락 풀기 (+0x132a)
└─ 활성화 컨텍스트 내림 (+0x2352), 지역 버퍼 정리, 반환 (+0x2395 RET, eax = 상태)
```

**묶음 표** (실행 순서, 호출 75곳)

| 묶음 | 범위 | 요약 | 대표 호출 call / ret | 호출 수 (잡일) |
|---|---|---|---|---|
| 1 입구 | `+0x0 ~ +0x1ec` | 인자 저장, 지역 문자열 버퍼 초기화, 부모 활성화 컨텍스트를 스레드 스택에 올림, ctx.flags 에서 `0x1800800` 비트만 뽑음 | `+0x8a / +0x8f memset` · 조건부 `+0x23f0 / +0x23f5 RtlRaiseException` | 2 (memset 1, 예외 1) |
| 2 ApiSet 풀이 | `+0x1ec ~ +0x66a` (+흩어진 조각 `+0x1f86~+0x22e0`) | 이름이 `api-ms-win-…` 같은 가상 이름이면 진짜 DLL 이름을 얻고 `NtSystemRoot\System32\<진짜이름>` 전체 경로를 지역 버퍼에 만듦. 실행 로그(ETW)·`LdrpLogDllState` | `+0x2fa / +0x2ff ApiSetpResolveHost` · `+0x22b0 / +0x22b5 ApiSetpSearchForApiSet` · `+0x22db / +0x22e0 ApiSetpSearchForApiSetHost` · `+0x3bf / +0x3c4 wcslen` | 10 (잡일 7: 로그 `RtlGetCurrentServiceSessionId` 3, `LdrpLogEtwEvent` 2, `LdrpLogDllState` 1, `wcslen` 1) |
| 3 이름 완성 | `+0x664 ~ +0x8ca` (+흩어진 조각 `+0x1426~+0x1ccd`, `+0x192b~+0x1e54`) | 매니페스트(SxS) 리다이렉트 시도 → 이름에 경로가 있는지 보고 `flags` 0x20 / 0x200 / 0x400 / 0x4 를 정함 → 확장자 없으면 `.dll` 붙임, 끝 점 떼기. 실패하면 로그 | `+0x1426 / +0x142b RtlDosApplyFileIsolationRedirection_Ustr` · `+0x148e / +0x1493 RtlGetFullPathName_Ustr` · `+0x167c / +0x1681 RtlDetermineDosPathNameType_Ustr` · `+0x1695 / +0x169a LdrpGetFullPath` · `+0x1669 / +0x166e`, `+0x1cc8 / +0x1ccd LdrpAppendUnicodeStringToFilenameBuffer` | 26 (잡일 20: 버퍼 복사 `RtlCopyMemory` 7, 버퍼 키우기 `RtlpCtAllocateMemory` 5·`NtdllpReallocateStringRoutine` 4, 문자열 해제 `RtlDeleteBoundaryDescriptor` 별칭 2, `LdrpLogInternal` 2) |
| 4 갈림길 준비 | `+0x8f2 ~ +0x9a9` | enclave 면 `LdrpFindOrPrepareEnclaveModule`. 아니면 `*arg6` 에 간선 칸(0x20바이트) 할당, `*arg5=0`, 부모 문맥에서 `+0x28`(상태 칸)·`+0x10`(경로 구조체) 꺼냄. 이유 9 면 자리표시 이름 길이 계산 | `+0x929 / +0x92e RtlAllocateHeap` · `+0x1be2 / +0x1be7 LdrpFindOrPrepareEnclaveModule` · `+0x19cd / +0x19d2 wcslen` | 3 (잡일 1: `wcslen`) |
| 5 ① 이미 올라와 있나 | `+0x9a9 ~ +0xdff`, `+0x150b` | 맨 이름이면 `LdrpFindLoadedDllByName`, 경로 있으면 해시표(32칸, `LdrpHashTable`)를 `RtlAcquireSRWLockShared` 아래서 훑어 전체 이름 비교. 찾으면 `ReferenceCount++`(`+0xce7`), 실패 상태면 순환 로그, 아니면 로드 수 +1 | `+0x16e9 / +0x16ee LdrpFindLoadedDllByName` · `+0xb2d / +0xb32 RtlAcquireSRWLockShared` · `+0xd1a / +0xd1f RtlReleaseSRWLockShared` · `+0x150e / +0x1513 LdrpIncrementModuleLoadCount` · `+0xdfa / +0xdff LdrpDereferenceModule` | 9 (잡일 4: 로그) |
| 6 ③ 새로 만들기 | `+0x10a3 ~ +0x11f5` | 새 로드 문맥(이름 길이 + 0xd2) 할당·채움 → 항목+노드 한 쌍 만들기 → `*arg5` 에 저장 → `LoadReason` 기록 | `+0x10d6 / +0x10db RtlAllocateHeap` · `+0x1157 / +0x115c LdrpAllocateModuleEntry` · 실패 `+0x1800 / +0x1805 RtlFreeHeap` | 6 (잡일 3: `RtlCopyMemory` 1, 로그 2) |
| 7 ② KnownDll · 바로 올리기 | `+0x1777 ~ +0x1798`, `+0x1c7f ~ +0x1cb8` | 새 항목마다 KnownDll 시도 (이유 9 는 건너뜀). `flags` 0x400 이고 아직 못 찾았으면 `LdrpProcessWork(문맥,1)` 바로 | `+0x178d / +0x1792 LdrpLoadKnownDll` · `+0x1ca9 / +0x1cae LdrpProcessWork` | 2 |
| 8 공통 꼬리: 락·간선·일감·카운터 | `+0xe0e ~ +0x1345` | 락 독점 → 항목 교체 확인 → **간선 쓰기** → 일감 넣기 → 부모 `+0x6c` 줄이기/기다림 → 참조 -1 → 락 풀기 | `+0x176d / +0x1772 RtlpAcquireSRWLockExclusiveContended` · `+0x17bd / +0x17c2 RtlAllocateHeap` (대비용 간선 칸) · `+0x1827 / +0x182c LdrpQueueWork` · `+0x132a / +0x132f RtlReleaseSRWLockExclusive` · `+0x133c / +0x1341 LdrpFreeReplacedModule` | 5 |
| 9 마지막 참조 정리 | `+0x1319 → +0x187a ~ +0x1f70` | 참조가 이 함수 몫 하나뿐이었을 때만: 목록에서 빼고 `LdrpUnmapModule`·TLS·활성화 컨텍스트·이름·항목 해제, 노드가 비면 `LdrpDestroyNode` | `+0x187d / +0x1882 RtlAcquireSRWLockExclusive` · `+0x1f03 / +0x1f08 RtlReleaseSRWLockExclusive` · `+0x1f12 / +0x1f17 LdrpUnmapModule` · `+0x1f55 / +0x1f5a LdrpDestroyNode` · `+0x1f64 / +0x1f69 LdrpReleaseTlsEntry` · `+0x1f6b / +0x1f70 RtlReleaseActivationContext` · `+0x1f33 / +0x1f38 LdrpFreeUnicodeString` · `+0x1f44 / +0x1f49 RtlFreeHeap` | 8 |
| 10 끝·예외 | `+0x234a ~ +0x2395`, `$fin$0` | 활성화 컨텍스트 내림, 지역 버퍼가 힙이면 해제, 쿠키 검사, 반환. `$fin$0` 은 예외 때 활성화 컨텍스트만 내림 | `+0x2352 / +0x2357 RtlDeactivateActivationContextUnsafeFast` · `+0x236c / +0x2371` (문자열 해제) · `+0x237e / +0x2383 __security_check_cookie` · `$fin$0 +0x10 / +0x15` | 4 |
| 합 | | | | **75** (본체 74 + `$fin$0` 1) |

- 호출 안 하는 줄 중 중요한 것 (인라인): 활성화 컨텍스트 올리기 (`+0x71~+0x1d3`), 락 독점 빠른 길 (`+0xe94 BTS.LOCK`), **간선 쓰기 (`+0xf2c~+0xfbb`)**, 부모 카운터 (`+0x12df~`), `ReferenceCount` 내리기 (`+0x1310 XADD.LOCK`), 해시표 훑기 (`+0xa..~+0xcc6`).
- 락 빠른 길 앞 `+0xe47~+0xe91`: TEB `+0x1850` 의 8칸 표에서 빈 칸을 찾아 락 주소를 적는 덩어리 (SRW 락 소유 추적으로 보임 `[추측]`). 잡일.

### 2. 간선(의존 관계)을 적는 줄

- **답: 예. 이 함수 안에 있다** (호출 아님, 인라인). 락(`LdrpModuleDatatableLock` 독점) 안, 새 문맥·항목을 만든 뒤, `LdrpQueueWork` 앞이다. `[정적]`
- **어느 노드끼리**:
  - 부모 노드 `R14` = `[[arg2 + 0x38] + 0x98]` (**인자 3 이 아니라 인자 2 의 `+0x38` 항목의 `DdagNode`**, `+0xeec~+0xeff`). 인자 3 은 활성화 컨텍스트·ApiSet 호스트 고르기·새 문맥 `+0x30` 에만 쓰인다.
  - 자식 노드 `RBX` = `[*arg5 + 0x98]` (`+0xea1`, `+0xef8`). 찾은 항목이든 새 항목이든 같음. (`+0x15d8` 에서 문맥의 `+0x38` 항목이 `*arg5` 와 다르면 문맥 쪽 항목으로 바꿈: "올리는 사이 같은 DLL 이 다른 항목으로 교체된 경우" `[추측]`.)
- **쓰는 줄** (`_LDRP_CSLIST.Tail` 에 끼워 넣기, 꼬리가 0 이면 자기 자신을 가리키게):
  | 줄 | 하는 일 |
  |---|---|
  | `+0xf63`, `+0xf66`, **`+0xf69 MOV [R14+0x28],RCX`** | 간선 칸 `RCX` 를 **부모 노드 `Dependencies`(+0x28)** 목록 꼬리에 끼움 (비어 있으면 `+0x17e4` 에서 `[RCX]=RCX`) |
  | `+0xf6d MOV [RCX+0x8],RBX` | 칸 `+0x8` = **자식 노드** |
  | `+0xf82`, `+0xf88`, **`+0xf8b MOV [RBX+0x30],RDX`** (`RDX = 칸+0x10`) | 칸 `+0x10` 을 **자식 노드 `IncomingDependencies`(+0x30)** 목록 꼬리에 끼움 (비어 있으면 `+0x17ec`) |
  | `+0xf96 MOV [RCX+0x18],R14` (+`+0xf9c~+0xfa5`) | 칸 `+0x18` = **부모 노드** (맨 아래 비트 0 은 원래 값 유지) |
- **간선 한 칸 = 0x20 바이트**, 구조 (이름 없음): `+0x00` 부모 `Dependencies` 목록 고리 / `+0x08` 자식 노드 / `+0x10` 자식 `IncomingDependencies` 목록 고리 / `+0x18` 부모 노드 (비트 0 = "대비용 `+0x17bd` 로 할당한 칸" 표시로 보임 `[추측]`).
- **칸은 어디서 오나** (요청서가 물은 `RtlAllocateHeap` 세 자리):
  - **`+0x929 (call) / +0x92e (ret)`** — 크기 `0x20`, 태그 `NtdllBaseTag + 0x280000`. `*arg6`(= caller 의 `[RSP+0x48]`)이 0 일 때만. 이 칸이 보통 쓰인다 (`+0xed5 RCX=[arg6]`, `+0xede` 에서 비트 0 지움). caller `LdrpMapAndSnapDependency +0x451` 이 안 쓰인 칸을 `RtlFreeHeap` 하는 것과 맞다 (sast/17 `[추측]` → **확인**).
  - **`+0x17bd (call) / +0x17c2 (ret)`** — 크기 `0x20`, 태그 `+0x240000`. `*arg6` 이 0 이어서 `RCX == 0` 일 때만 쓰는 대비용 (`+0xf4a JZ +0x179d`). 비트 0 을 켜 둠 (`+0x17d9`). 실패하면 `+0x1c64` 에서 `0xc0000017` 로 가는데 이 줄은 주소 0 에 쓰는 모양이라 거의 안 오는 길로 보임 `[추측]`.
  - **`+0x10d6 (call) / +0x10db (ret)`** — 간선 칸이 **아님**. 새 **로드 문맥**(이름 길이 + 0xd2 바이트).
- **칸을 쓴 뒤**: `*arg6 = 0` (`+0x12ab`, `RCX` 가 0). 간선을 안 썼으면 칸이 `*arg6` 에 그대로 남아 caller 가 해제.
- **간선을 안 쓰는 경우** (`+0xf06`, `+0xf0f~+0xf26`, `+0xf2c~+0xf48`) → `+0x1294` 로 가서 자식 노드 `LoadCount` 를 1 줄임 (`LoadCount` 가 2 이상이고 `-1` 아닐 때만, `+0x1297~+0x1525`):
  1. 부모 노드 == 자식 노드 (자기 자신)
  2. 자식 노드 `State == 9` (ReadyToRun) 이고 (`LoadCount == -1` 이거나 자식 항목 `Flags` 비트 5(`ProcessStaticImport`) 가 켜짐)
  3. 부모 `Dependencies` 목록을 한 바퀴 돌아 이미 같은 자식이 있음 (`+0xf2c~+0xf48`)
  → 해석 `[추측]`: `LoadCount` 는 "들어오는 간선 수"를 대신하는 값이라서, 새 간선이 안 생기면 찾을 때 올린 값(`+0x1513` 의 `LdrpIncrementModuleLoadCount`)을 되돌린다.
- **상태가 음수여도 간선은 쓴다**: 꼬리에 들어가는 조건은 `*arg5 ≠ 0` 뿐이다 (`+0xe0e~+0xe15`). 상태 검사는 `+0xe1b` 의 `0xc0000135`(바로 올리기) 하나.
- **노드 `State` 를 쓰는 곳**: 이 함수는 `+0x162d` 에서 부모 노드 `State = 3` 하나만 쓴다 (질문 5). `Modules`(+0x00) 에는 안 씀.

### 3. 세 갈림

근거: `.asm` 의 `+0x8f2~+0x9b0`, `+0x10a3~+0x11f9`, `+0x1777~+0x1798`, `+0xd92~+0xdff`, `+0x1812~+0x183a`.

**중요한 정정: ② KnownDll 과 ③ 새로 올리기는 "둘 중 하나"가 아니다.** ③ 이 먼저고 ② 는 ③ 안에서 불린다 `[정적]`:

1. **① 이미 올라와 있음**: 이름이 준비된 뒤 `*arg5 = 0` 으로 비우고(`+0x98b`) 찾는다.
   - 이유(`arg4`) == 9 : 안 찾음 → ③ 으로 (`+0x995 JZ +0x19b6`: 자리표시 이름 `ModuleNamePlaceholderBuffer`, 상태 `0xc0000135`).
   - `flags & 0x20` (맨 이름) : **`+0x16e9 (call) / +0x16ee (ret) LdrpFindLoadedDllByName(지역 이름, 0, flags, arg5, &모듈상태)`**. 찾은 항목은 callee 가 `*arg5` 에 쓴다 (인자 4 = `R9 = arg5`, 본문은 안 읽음).
   - 아니고 `flags & 0x200` (전체 이름 있음) : 인라인 해시 훑기 `+0xb2d / +0xb32` 락(공유) → 이름의 마지막 조각을 대문자로 바꿔 해시 (`hash = hash*0x1003f + 글자`) → `LdrpHashTable[hash & 0x1f]` → 항목 `+0x108` 해시 같고 `FullDllName` 이 같으면 찾음 → **`+0xce7` `ReferenceCount++`, `+0xcf6 MOV [arg5],RSI`**(찾은 항목을 인자 5 에 저장) → 락 풀기 `+0xd1a / +0xd1f`.
   - 둘 다 아님 (경로가 있는데 `RtlDetermineDosPathNameType_Ustr` 가 5 로 돌려준 모양): 찾지 않고 상태 `0xc0000135` (`+0x9ad JNC +0x1a1b`).
   - 결과가 `0xc0000135` 이면 ③ (`+0xd92 CMP EBX,0xc0000135 → +0x10a3`). **아니면 ① 확정**:
     - 모듈 상태가 **음수**면 (`+0xdb3 JNS` 안 탐): 로그 "Found circular dependent DLL … failed to load previously" (`+0xded`), 반환 상태 `0xc00000e5`, `LdrpDereferenceModule(*arg5)` (`+0xdfa / +0xdff`), `*arg5 = 0` → 간선 없이 맨 끝.
     - 아니면 **`+0x150e / +0x1513 LdrpIncrementModuleLoadCount(항목)`**, 상태 0.
   - ① 일 때도 **공통 꼬리로 가서 간선은 생긴다** (위 질문 2의 세 가지 안 쓰는 경우만 빼고). 일감 넣기(`LdrpQueueWork`)는 상태가 0 이라 안 탄다.
2. **③ 새로 올리기** (`+0x10a3`): `*arg5 = 0` → `+0x10d6 / +0x10db RtlAllocateHeap(LdrpHeap, 태그+0x40000 | 8, 이름길이+0xd2)` (실패 `+0x10e1 JZ +0x1f72` → 상태 `0xc0000017` 쪽으로) → 새 문맥 칸 채우기:
   `+0x10ec [+0x20] = flags | 0x8000`, `+0x10f8 [+0x10] = 부모 ctx+0x10`, `+0x10fc [+0x28] = 부모 ctx+0x28`, `+0x1108 [+0x30] = 인자 3 (부모 항목)`, `+0x1113 [+0xb8] = -1`, `+0x1121 [+0x8] = +0xd0` 에 이름 `UNICODE_STRING`, `+0x1140 / +0x1145 RtlCopyMemory` 로 이름 복사 → **`+0x1157 / +0x115c LdrpAllocateModuleEntry(새 문맥)`** → **`+0x1164 MOV [arg5],RAX`** (새 항목을 인자 5 에 저장; 0 이면 `+0x1800 RtlFreeHeap` 하고 `0xc0000017`).
   → `+0x1179 MOV [RAX+0x10c],R14D` (**항목 `LoadReason` = 인자 4**), 값이 9 면 `+0x118a [+0x130 HotPatchState] = 1`.
3. **② KnownDll** (`+0x11e6 CMP R14D,9`): 이유 ≠ 9 이면 **새 항목 `LoadContext`(`+0xb0`) 로 `+0x178d / +0x1792 LdrpLoadKnownDll`**. 이유 == 9 면 안 부르고 상태 `0xc0000135` (`+0x11f0`).
   - 어떤 DLL 이 KnownDll 인지 판단은 callee 몫 (이 함수는 조건 없이 부름; 이름이 맨 이름인지 따위를 안 봄).
   - 돌려받은 상태가 **0** 이면 callee 안에서 올림이 끝난 것 (sast/16: `LdrpLoadKnownDll → LdrpMapDllWithSectionHandle → LdrpMapAndSnapDependency` 재귀) → 일감 안 넣음. **`0xc0000135`** 이면 KnownDll 아님 → 질문 4 의 일감/바로.
- **인자 5 에 저장하는 값 정리**: ① 해시 길: 찾은 항목 (`+0xcf6`), ① 맨 이름 길: callee(`LdrpFindLoadedDllByName`)가 씀 `[추측: 인자로 넘김만 확인]`, ③: 새 항목 (`+0x1164`), 교체됐으면 문맥 쪽 항목 (`+0x1345`), 실패·순환: 0.

### 4. 일감 vs 바로

- **`+0x1827 (call) / +0x182c (ret) LdrpQueueWork(항목.LoadContext)`** — 조건 (모두): `*arg5 ≠ 0`, 락 독점 잡은 뒤, 상태 == `0xc0000135` (`+0x12b2`), 그리고 `flags & 0x400` == 0 (`+0x1812 TEST [RSP+0x5c],0x400`). 부른 뒤 상태를 0 으로 만든다 (`+0x182c`). **★ `LdrpModuleDatatableLock` 독점을 잡은 채로 부른다** (`+0xe94` 잡음 ~ `+0x132a` 풀음 사이) `[정적]`.
  - 상태가 `0xc0000135` 인 길 = "새로 만들고 ② KnownDll 도 아니었다", 또는 이유 9 (PatchImage). ① 이미 올라와 있는 DLL 은 상태가 0 이라 안 탄다.
  - 상태 `0xc000022d` 는 0 으로 바꿔서 그냥 지나간다 (`+0x12be`; 이름은 `STATUS_RETRY` 로 알려진 값 `[추측]`).
- **`+0x1ca9 (call) / +0x1cae (ret) LdrpProcessWork(항목.LoadContext, 1)`** — 조건: `flags & 0x400` (**경로를 직접 줬을 때**, `LdrpGetFullPath` 성공 `+0x16af`) 이고 상태 == `0xc0000135`. 부르기 전에 문맥 `+0x20` 비트 26 을 켠다 (`+0x1c9d`). 돌아와서 **상태 = `*(문맥+0x28)`** (`+0x1cae~+0x1cb4`). **락을 잡기 전**에 부른다 (꼬리의 `+0xe94` 보다 앞).
- **병렬 로딩 켜짐 여부를 읽나: 아니오** `[정적]`. 이 함수가 읽는 전역은 `__security_cookie`, `NtdllBaseTag`, `LdrpHeap`, `LdrpModuleDatatableLock`, `LdrpHashTable`, `GlobalRtlNlsState(+0xa8)`, `LdrpIsSecureProcess`, `LdrpDefaultExtension`, `LdrpIllegalCWDDevices`, `SlashSystem32SlashString`, `ModuleNamePlaceholderBuffer` 뿐 (`.asm` 주석). `LdrInitState`·`LdrpEnableParallelLoading` 결과는 없다. TEB/PEB 에서 읽는 것: `GS:[0x30]+0x1850`(락 추적 표), `GS:[0x60]` 의 `+0x20 ProcessParameters`, `+0x68 ApiSetMap`, `+0x90`·`+0x378`(추적 켜짐 비트, ETW 로그용 `[추측]`), TEB `+0x2c8` (활성화 컨텍스트 스택).
- **kernel32 준비 때와 exe import 때 다르게 가나**: 이 함수 코드는 **똑같이** 돈다. 갈라지는 건 ① 부모 문맥 `+0x20` 의 `0x800`·`0x800000`·`0x1000000` 비트가 자식으로 물려가는 것 (그 비트를 켜는 자리는 이 요청 밖), ② `LdrpQueueWork` 본문 (Q47). 달라지는 점 하나: `LdrpQueueWork` 가 돌아온 **뒤의** 자식 `State` 를 `+0x12df~` 에서 읽는다 (질문 5) — 일감이 그 자리에서 다 올려 주면 `State ≥ 2`, 미뤄 두면 `State < 2` 라서 부모 카운터가 달라진다 → 병렬 켜기 앞/뒤 차이가 여기에 나타날 수 있다 `[추측]`. 동적으로 확인 (Q47).
- `[추측]` kernel32 의 자식 kernelbase 가 KnownDll 이면 ② 에서 다 올라와서 이 함수의 `LdrpQueueWork` 는 안 타고, 끝맺음(`LdrpMapAndSnapDependency +0x4f7`)의 `LdrpQueueWork` 만 탈 것이다 (Q20).

### 5. 남은 자식 수와 기다림

근거: `+0x12ce~+0x12fa`, `+0x1606~+0x163d`, `+0x1700~+0x1711`. 이 블록은 **상태가 0 이상 (또는 0xc000022d·`0xc0000135`→일감으로 0 이 된 것)일 때만** 돈다 (`+0x12ce MOV EDI,1` 이 켜는 조건; 음수 상태면 건너뜀).

- 읽는 칸: 부모 문맥(`arg2`) `+0x6c` = `ECX`, 자식 노드 `State` (`[[*arg5+0x98]+0x38]`).
- **`ECX ≠ 0` 일 때 (보통, `LdrpMapAndSnapDependency` 고리에서)**: `+0x12ea` — `*arg6 ≠ 0` (간선 칸이 남음 = 새 간선이 안 생김) **이거나** 자식 `State ≥ 2` (`Mapped` 이상, `+0x1707`) 이면 **`+0x12f4 / +0x12f7 [ctx+0x6c] = ECX - 1`**. 새 간선이 생겼는데 자식 `State < 2` 면 **줄이지 않는다** (`+0x1711 JMP +0x12fa`).
  → 뜻 `[추측]`: 부모는 "아직 안 끝난 자식 수 + 1" 을 들고 있다 (sast/17). 이미 끝난 자식·새 간선이 필요 없는 자식은 바로 뺀다. **새로 이어 붙인 자식이 아직 안 올라왔으면 부모 몫으로 남겨 둔다** → 그 자식이 다 올라온 뒤 부모 쪽 카운터를 줄이는 코드는 **이 함수에 없다** (누가 이어받는지는 callee 쪽: `LdrpProcessWork`·`LdrpSnapModule` 방향, Q12c 후보).
- **`ECX == 0` 일 때 (`+0x12e4 JZ +0x1606`)**: 자식 `State < 2` 면 (`+0x160d~+0x1611`):
  - `+0x1617 [ctx+0x50] = 자식 항목` (기다리는 자식)
  - `+0x161b [ctx+0x6c] = 1`
  - **`+0x162d MOV [부모 노드+0x38],3`** (`[[ctx+0x38]+0x98]` 의 `State` = 3 `WaitingForDependencies`)
  - **`+0x1634 MOV EBX,0x103`** → 반환 상태 `0x103` (`STATUS_PENDING`). `State ≥ 2` 면 아무것도 안 하고 지나감.
  → 뜻: "부모 카운터가 이미 0 인데 아직 안 끝난 자식이 새로 생겼다 → 그 자식 하나를 기다리는 걸로 되돌리고 부모를 기다림 상태로 두고 0x103 을 돌려준다" `[정적: 줄, 뜻은 추측]`.
- **`LdrpMapAndSnapDependency` 고리에서는 `ECX == 0` 이 안 나온다**: 카운터가 `자식 수 + 1` 로 시작하고 그 `+1` 은 고리를 다 돈 뒤 끝맺음(`MapAndSnap +0x47d`)에서만 줄이기 때문에 고리 도중엔 항상 ≥ 1. 그래서 `0x103` 은 `ctx+0x6c` 가 0 일 수 있는 다른 caller (`LdrpSnapModule +0x5df`, `LdrpLoadDependentModuleW +0xae`) 몫으로 보인다 `[추측]`.
- **caller 가 `0x103` 을 받는 법** (`LdrpMapAndSnapDependency.asm +0x34e~+0x352`): `MOV ESI,EAX; TEST EAX,EAX; JS +0x4ad` — **음수일 때만** 실패로 보고 `0x103` 은 성공처럼 지나간다 (칸 저장·정리 후 다음 import). 즉 `MapAndSnap` 은 `0x103` 을 따로 구분하지 않는다 `[정적]`.

### 6. ApiSet

- **답: 예. 가상 이름을 진짜 DLL 이름으로 바꿔 주는 자리가 이 함수 안이다** (`LdrpPreprocessDllName` 이 끼워져 있음). `[정적]`
  - **`+0x2fa (call) / +0x2ff (ret) ApiSetpResolveHost`**: 인자 `RCX = PEB+0x68 ApiSetMap`, `RDX = 이름 글자`, `R8 = 글자 수`, `R9 = 1`, 스택으로 부모 `BaseDllName`(`arg3+0x58`), `&[RSP+0x50]`(= api-set 이었나 바이트), `&[RSP+0x108]`(= 진짜 이름 `UNICODE_STRING`). 음수면 import 실패. 이 길은 `ApiSetMap` 첫 바이트가 7 이거나 (버전 6 이면서 `+0x10 > 8`, `+0x1c` 바이트 == 7, `+0x1e & 4 == 0`) 일 때만 (`+0x2ba~`, `+0x1f86~+0x2087`). 그 외 `ApiSetMap` 모양이면 아래 옛 길.
  - 옛 길 (맵 모양이 위와 다를 때): 이름 첫 8바이트가 `api-` 또는 `ext-` (대문자 마스크 비교, `+0x1fbc~+0x1fdc`) 이면 **마지막 `-` 뒤를 떼고** (`+0x20d1~+0x212f`) **`+0x22b0 (call) / +0x22b5 (ret) ApiSetpSearchForApiSet`** → 부모 `BaseDllName` 이 있고 후보가 2개 이상이면 **`+0x22db (call) / +0x22e0 (ret) ApiSetpSearchForApiSetHost`** (부모 이름에 따라 호스트 고름) → 후보 칸에서 호스트 이름(`+0xc` 오프셋, `+0x10` 길이)을 `[RSP+0x108]` 에 적음.
  - 어느 길이든 결과(`[RSP+0x50] ≠ 0`)면: 길이 0 이면 상태 `0xc0000481` 로 실패 (`+0x2077`), 아니면 지역 버퍼에 **`SharedUserData NtSystemRoot` (`0x7ffe0030`, 서버 사일로면 `PEB+0x90` 쪽) + `"\System32\"` (`SlashSystem32SlashString`) + 진짜 이름** 을 이어 붙인다 (`+0x3ca~+0x5ff`, `wcslen` `+0x3bf`). → **전체 경로가 만들어진다.** 로그 "DLL … was redirected to … by API set" (`+0x15aa`, 문자열은 `LdrpLogInternal` 인자), `flags |= 0x200` (SxS 가 아니라서 0x4 는 안 붙음).
- **바뀐 이름으로 ①②③ 을 다시 타나: 예.** 이름이 바뀌어도 같은 아래 줄을 지난다. 달라지는 점: `flags` 에 0x20(맨 이름)이 **안 켜져서** `LdrpFindLoadedDllByName` 이 아니라 **인라인 해시 훑기 + 전체 이름 비교** (`+0xb2d~`)로 간다. 못 찾으면 ③ (새 문맥 이름 = 전체 경로) → ② KnownDll 시도.
- 이 길에서 SxS 리다이렉트는 `PEB.ProcessParameters(+0x20)` 의 `Flags(+8) & 0x1000` 이 켜졌을 때만 시도 (`+0x62f~+0x638`; 안 켜지면 `R15B=0` → `+0x664` 안 탐). 일반 이름(api-set 아님)은 항상 시도 (`LdrpIsSecureProcess == 0` 일 때, `+0x13e7`).
- 실행 때 `[추측]`: kernel32 의 import 가 `api-ms-win-…` 이면 이 자리에서 `KERNELBASE.dll` 전체 경로로 바뀐다 (run20·21 의 "kernel32 를 들이면 kernelbase 가 따라 들어온다" 와 이어짐). kernel32 의 import 목록은 이번 범위 밖이라 "이 함수가 바꿔 준다" 까지만.
- test.exe 의 `dll1.dll`·`dll2.dll`: `[소스]` 일반 이름 (`sample/` 에 `dll1.c`·`dll2.c`). api-set 이름이 아니라서 이 갈래는 **"api-set 아님" 쪽** 으로 지나갈 것이다 `[추측]` (`ApiSetpResolveHost` 가 0 이상을 돌려주고 `[RSP+0x50] == 0`).

### 7. 인자와 결과

- **인자 4 (`R9D` = LoadReason)**: `+0x25 R12D=R9D`, `+0x28 [RSP+0x84]=R9D`.
  - `+0x1ec CMP R12D,9` : 9 면 이름 다듬기 전체 건너뜀 (→ `+0x8f2`).
  - `+0x995 CMP R12D,9` : 9 면 찾기 건너뜀, 자리표시 이름 (`+0x19b6`).
  - **`+0x1179 MOV [RAX+0x10c],R14D`** : 새 항목 `LoadReason(+0x10c)` = 인자 4. `R14D` 는 `+0x1171` 에서 `[RSP+0x84]` 를 다시 읽은 값.
  - `+0x1180 CMP R14D,9` 면 `+0x118a [항목+0x130 HotPatchState] = 1`.
  - `+0x11e6 CMP R14D,9` : 9 면 KnownDll 안 부름 (상태 `0xc0000135`, `+0x11f0`).
  - 값: `LdrpMapAndSnapDependency` 가 0 (`StaticDependency`) 을 넘긴다 (sast/17). 9 는 핫패치 갈래 (`LdrpLoadDependentModuleW` 가 부를 때 `[추측]`).
  - **① 찾은 항목에는 `LoadReason` 을 쓰지 않는다** (새 항목에만).
- **6번째 인자 (`[RSP+0x48]`) = 간선 칸 보관 포인터의 주소** `[정적]`: 이 함수가 처음 비어 있으면 `0x20` 바이트 칸을 할당해 넣고 (`+0x929`), **간선을 새로 쓰면 비우고(0)** 안 쓰면 그대로 둔다 (`+0x12ab`). caller 가 고리 끝에 남은 칸을 `RtlFreeHeap` (sast/17 `MapAndSnap +0x451`). 고리 도는 동안 같은 임시 변수를 계속 쓰니 import 마다 새로 할당될 수도 재사용될 수도 있다.
- **반환값 `[정적]`** (`+0x2371 MOV EAX,EBX`, `RET +0x2395`):
  | 값 | 언제 |
  |---|---|
  | 0 | 찾아서 로드 수만 올림 (①), KnownDll 로 올림 끝(②), 일감 넣고 돌아옴(상태 0 으로 바꿈) |
  | `0x103` | 부모 `ctx+0x6c == 0` 이고 자식 `State < 2` (`+0x1634`) |
  | `0xc0000135` | `flags 0x400` 로 바로 올렸는데 못 찾음 (일감이 안 되므로 상태가 그대로 남음) |
  | `0xc0000017` | 힙 할당 실패 (`+0x935`, `+0x116b`, `+0x17d0`, 버퍼 키우기) |
  | `0xc0000106` | 이름·경로가 너무 길다 (0xfffe 넘음) |
  | `0xc0000481` | api-set 으로 풀었는데 진짜 이름 길이가 0 |
  | `0xc00000e5` | 이미 있는 항목의 상태가 음수 ("failed to load previously") |
  | 이름 다듬기 실패값 | `RtlDosApplyFileIsolationRedirection_Ustr`(`0xc0150008` 은 오류 아님, `+0x14f2`), `LdrpGetFullPath` 등 |
  | enclave 갈래 | `LdrpFindOrPrepareEnclaveModule` 반환값 그대로 (`+0x1be9`) |
  | ProcessWork/KnownDll 반환 | 그대로 |

### 8. 실패 정리

- **`+0x1f03 ~ +0x1f70` 은 "실패했을 때"가 아니라 "이 함수가 들고 있던 마지막 참조를 내려놓았을 때"** 타는 길이다 `[정적: 조건 줄 / 뜻은 추측]`:
  - 조건 줄: `+0x12fa~+0x1310` — 자식 노드 `LoadCount ≠ -1` 이고 항목 `Flags` 비트 5 (`ProcessStaticImport`) 가 꺼져 있으면 **`XADD.LOCK [항목+0x114], -1`** (`ReferenceCount` 내림). 돌려받은 옛 값이 **1** 이면 (`+0x1319 CMP R15D,1 → JZ +0x187a`) → 마지막 참조.
  - `+0x187d / +0x1882 RtlAcquireSRWLockExclusive` → 항목 `NodeModuleLink(+0xa0)` 이웃 확인 (깨졌으면 `+0x18a4 INT 0x29` = `__fastfail(3)`) → 목록에서 뺌.
  - `+0x1f03 / +0x1f08 RtlReleaseSRWLockExclusive`. 그다음:
    - `+0x1f08 CMP word [항목+0x6e]` (`TlsIndex`) ≠ 0 이면 `+0x1f64 / +0x1f69 LdrpReleaseTlsEntry(항목, 0)`
    - **`+0x1f12 / +0x1f17 LdrpUnmapModule(항목)`**
    - 항목 `+0x88` (활성화 컨텍스트) 가 0·-1 이 아니면 **`+0x1f6b / +0x1f70 RtlReleaseActivationContext`**
    - 항목 `FullDllName.Buffer(+0x50)` ≠ 0 이면 `+0x1f33 / +0x1f38 LdrpFreeUnicodeString(항목+0x48)`
    - `+0x1f44 / +0x1f49 RtlFreeHeap(항목)`
    - 노드 `Modules` 가 이 항목 하나뿐이었으면 (`+0x1f49 CMP RDI,RBX`) **`+0x1f55 / +0x1f5a LdrpDestroyNode(노드)`**
  - 참조 쌍: 새 항목은 `ReferenceCount = 2` 로 시작 (sast/17), ① 은 `+0xce7` 에서 +1. 이 함수가 끝에 −1 을 하니 **정상으로 올라온 DLL 은 옛 값이 2 이상이라 이 정리 길을 안 탄다.** 로드가 실패해서 다른 쪽이 먼저 놓았을 때만 마지막 참조가 된다 `[추측]`.
  - **test.exe 길에서 안 탈 것인가: 안 탈 것으로 읽힌다** `[추측]`. 확인은 `+0x1319` 앞 한 번 (동적).
- **`+0x23f0 (call) / +0x23f5 (ret) RtlRaiseException`**: 맨 앞(`+0x19d~+0x1a0`)에서 이 스레드의 활성화 컨텍스트 스택 **꼭대기 프레임이 있고** (`TEB+0x2c8 → [..] ≠ 0`) 그 프레임 `Flags(+0x10) & 0x70 ≠ 0x20` 일 때 (`+0x2397~+0x239e`). 예외 코드 `0xc0150014`, 매개변수 4개 (`+0x23a4`). 호출이 끝나면 `+0x23f5 JMP +0x1c8e` 로 돌아와 계속 진행. 이름은 SxS 활성화 스택 이상으로 보임 `[추측]`. **test.exe 길에서는 꼭대기 프레임이 없거나 정상 프레임이라 안 탈 것** `[추측]`.
- `$fin$0` : 예외 때 운영체제가 부른다. `RtlDeactivateActivationContextUnsafeFast(프레임)` 하나 (`+0x10 / +0x15`). 호출로 안 닿는다.

## 활성화 컨텍스트 (요청 밖이지만 함수 전체에 걸친 것)

- 함수 전체가 **부모 항목의 `EntryPointActivationContext(+0x88)`** 를 켠 상태에서 돈다 (`+0xf1 RDX=[RSI+0x88]`, 프레임을 `[RSP+0x260]` 에 만들어 TEB `+0x2c8` 가 가리키는 스택의 꼭대기에 연결 `+0x1d3~+0x2412`). 이미 같은 컨텍스트가 꼭대기면 연결 생략. 그 덕에 `RtlDosApplyFileIsolationRedirection_Ustr` (SxS 리다이렉트)가 **부모의 매니페스트 기준**으로 이름을 바꾼다 `[추측]`.
- 내려놓는 곳: 정상 끝 `+0x2352 / +0x2357`, 예외 때 `$fin$0`.

## test.exe → dll1 → dll2 이야기 그림의 `?` 칸 (run22 그림 기준)

채워지는 것:

```
LdrpLoadDependentModuleInternal("dll1.dll", exeCtx, E_exe, 0, &exeCtx.58[i], &임시)       ← MapAndSnap +0x34e
  부모(E_exe) 활성화 컨텍스트 올림
  이름 다듬기: api-set 아님 → SxS 시도 (보통 "없음" 0xc0150008) → 맨 이름(0x20) → ".dll" 확인     [추측: 실제 값은 동적]
  *arg6 에 0x20바이트 칸 할당 (+0x929)
  LdrpFindLoadedDllByName → 0xc0000135 (아직 없음)                                          [추측]
  새 문맥(+0x30 = E_exe) + LdrpAllocateModuleEntry → E_dll1 + 노드(State 0), *arg5 = E_dll1, LoadReason 0
  LdrpLoadKnownDll(dll1Ctx) → 0xc0000135 (KnownDll 아님)                                    [추측]
  락 독점
    ★ 간선: E_exe 노드.Dependencies ← 칸 → dll1 노드.IncomingDependencies  (+0xf69, +0xf8b)   ← 이전 그림의 "? 간선"
    LdrpQueueWork(dll1Ctx)  (+0x1827, 락 안)                                                  ← 이전 그림의 "올리기 …"
    exeCtx.+0x6c : dll1 State < 2 면 그대로 (줄이지 않음), ≥ 2 면 −1                           ← 이전 그림의 "남은 수 = ?"
  참조 −1, 락 풀기, 반환 0
```

- `?` 로 아직 남는 것: `LdrpQueueWork` 가 돌아왔을 때 dll1 `State` 가 얼마인지 (= `LdrpQueueWork` 가 그 자리에서 올려 주는지, 일꾼 스레드로 미루는지); `dll1Ctx` 로 `LdrpMapAndSnapDependency` 가 다시 불리는 스레드; 부모 `exeCtx.+0x6c` 를 나중에 줄이고 `LdrpSnapModule(exeCtx)` 를 부르게 하는 쪽 (이 함수에 없음). run22 의 "exe 의 남은 수 = ?" 는 **"dll1 이 안 올라왔으면 2 (= 자식 1 + 1) → 끝맺음에서 1 → R15D≠0 이라 snap 안 함"** 까지 정적으로 읽힌다 (`MapAndSnap +0x47d~+0x49a`).
- run8 `[관찰]` (dll1·dll2 는 `+0x1bb6` 전에 올라와 있었고 켜는 순서 dll2 → dll1) 과 **어긋나지 않는다**: 일감을 넣는 건 여기서, 기다려 비우는 건 `LdrpInitializeProcess +0x1ab1 LdrpDrainWorkQueue(1)` (run19).

## 그림 2 `(정적)` 상자 후보 (2.5·2.6)

1. **`ntdll!LdrpLoadDependentModuleInternal` — import 이름 하나 처리: 이름 다듬기(`+0x2ff` ApiSet / `+0x142b` SxS / `+0x169a` 경로) → 이미 있나(`+0x16ee`, 해시) → 없으면 새 항목 (`+0x115c`)**. 이유: 2.6 "import DLL 이 올라오는 순간" 칸에 "한 import 가 어떤 갈래로 가는지" 를 정적으로 박는다. api-set 이 진짜 이름으로 바뀌는 자리(run20·21 `[추측]`)도 여기.
2. **`ntdll!LdrpLoadDependentModuleInternal +0xf69 / +0xf8b` — 간선 쓰기: 부모 노드 `Dependencies(+0x28)` 와 자식 노드 `IncomingDependencies(+0x30)` 에 0x20바이트 칸을 끼움** (칸은 `+0x92e` 에서 할당). 이유: "누가 누구에 기대나" 가 적히는 자리. 2.5·2.6 그래프 줄의 `?` 해소.
3. **`ntdll!LdrpLoadKnownDll`(`+0x1792`) → 못 찾으면 `ntdll!LdrpQueueWork`(`+0x182c`, 락 안)** — KnownDll 이면 그 자리에서 올림, 아니면 일감으로 넘김. 이유: sast/16 의 "KnownDll 길 / 파일 길" 이 이 함수의 어디서 갈리는지 한 줄.
4. **`ntdll!LdrpLoadDependentModuleInternal +0x12f7 / +0x1634` — 부모 문맥 `+0x6c` 줄이기, 0 이면 부모 노드 `State = 3` + 반환 `0x103`**. 이유: 노드 상태 표(run21)의 3 이 쓰이는 자리 (4·5 는 sast/17 `MapAndSnap`).

## 동적분석으로 확인해야 할 것

모두 `test.exe` 일 때만 멈추는 조건 (기존 방식). 오프셋은 `ntdll+rva` (이 함수 시작 `ntdll+0x21ab0`, rva = 함수 시작 + 오프셋).

| 무엇을 | 멈춤점 (`ntdll+…`) | 보는 것 | 기존 번호 |
|---|---|---|---|
| import 하나가 이 함수에 들어오는 순간 | 입구 `+0x21ab0` | `du poi(@rcx+8)` 이름, `dq @rdx+0x30 L1` (부모 `ctx.+0x30`), `dq @rdx+0x38 L1`, `dd @rdx+0x6c L1`, `r r9d` (LoadReason), `dd @rdx+0x20 L1` (ctx 플래그) | **Q23** 에 붙임 (`ntdll+0x213e9` 와 짝) |
| api-set 이름이었나 / 진짜 이름 | `ApiSetpResolveHost` ret `+0x21daf` | `r eax`, `db @rsp+0x50 L1` (api-set 여부), `dS @rsp+0x108` (진짜 이름), `dd poi(@$peb+0x68) L1` (맵 버전) | **Q20** (kernel32 import), **Q23** (dll1: api-set 이 아님을 확인) |
| 맨 이름 찾기 결과 | ret `+0x2319e` (`FindLoaded`) | `r eax` (0 이냐 `c0000135` 냐), `dq poi(@rsp+0xb0) L1` (arg5 칸) | **Q23** (dll1·dll2 처음엔 `c0000135`) |
| 새 항목 | ret `+0x22c0c` (`LdrpAllocateModuleEntry`) | `dq @rax+0x98 L1` 노드, `dd poi(@rax+0x98)+0x38 L1` (State 0), `dd @rax+0x10c L1` (아직 안 씀) → 다음 `+0x22c29` 뒤 `LoadReason` | sast/17 이 정한 `ntdll+0x6fbf8` 와 짝, **Q23** |
| KnownDll 결과 | ret `+0x23242` | `r eax`: kernelbase 등은 0, dll1·dll2 는 `c0000135` 인가 | **Q20** |
| 일감 넣기 | call `+0x232d7` | `dq ntdll!LdrpModuleDatatableLock L1` (비트 0 = 독점 잡힘), `~`, `kb` (일꾼 스레드 / 같은 스레드); ret `+0x232dc` 에서 `dd poi(@rsi+0x98)+0x38 L1` (자식 State: 그 자리에서 올렸나) | **Q47** (`LdrpQueueWork`) · **Q20** (kernel32 준비) |
| 간선 쓰기 | `+0x22a58` (두 목록에 다 끼운 직후) | `dq @rcx L4` (+0x8 자식 노드, +0x18 부모 노드), `dq @r14+0x28 L1`, `dq @rbx+0x30 L1` | 새 항목 (Q23 곁에 붙임 제안) |
| 부모 카운터 | `+0x22d8f` | `dd @rdx+0x6c L1` (`ecx`), 자식 `State` `dd poi(@rsi+0x98)+0x38 L1`, `*arg6` `dq poi(@rsp+0xe0) L1` | **Q23**·**Q47** |
| 기다림 `0x103` 길을 안 타나 | `+0x230c7` | 안 걸리면 정적 읽기 확인 (`test.exe` 에선 안 걸릴 것) | Q23 곁 |
| 마지막 참조 정리를 안 타나 | call `+0x2332d` | 안 걸릴 것 | Q23 곁 |
| 바로 올리기 / SxS | `+0x23759` (ProcessWork call), `+0x22ed6` (SxS 리다이렉트 call) | 안 걸리거나 `eax=c0150008` | Q23 곁 |
| 반환 | `RET +0x23e45` | `r eax` | Q23 |

- `LdrpIsHotPatchingEnabled`·`LdrpRedirectionModule` 은 이 함수가 아니라 sast/17 몫 (Q46 에 이미 출처 있음). 이번에 새 번호는 안 만들고 위 표대로 기존 Q20·Q23·Q47 에 붙이는 걸 제안.
- 새로 확인할 만한 것 하나: **`LdrpQueueWork` 를 부르는 동안 `LdrpModuleDatatableLock` 이 독점으로 잡혀 있다**는 것이 맞는가 (call 직전 `dq ntdll!LdrpModuleDatatableLock L1` 비트 0). 맞으면 `LdrpQueueWork` 본문은 이 락을 다시 안 잡는 모양일 것 (Q47 에서 `k` 로 확인).

## 판단

- **Q12c (`LdrpCondenseGraph` → `LdrpCondenseGraphRecurse` → `LdrpMergeNodes`, `LdrpSnapModule`) 를 따로 한 바퀴로 볼 가치**: **있다.** 이유 한 줄: 부모의 남은 자식 수(`+0x6c`)를 "다 올라온 뒤" 줄이고 부모 snap 으로 이어 주는 코드는 이 함수에도 `MapAndSnap` 에도 없어서 **callee `LdrpQueueWork`·`LdrpProcessWork` 쪽 + `LdrpSnapModule`** 이 이어받을 것으로 보이고, 간선은 이미 이 함수에서 쓰이니 그 간선을 읽는 쪽(Condense)과 짝으로 보기 좋다. 순서는 (1) `LdrpQueueWork`·`LdrpProcessWork` 본문 → (2) Condense 쪽이 자연스럽다 `[추측]`.
- **Q14 ("dll1·dll2 를 올리는 쪽")**: **일부.** 이 함수로 답이 나는 것: import 이름 처리 순서, 새 항목·노드 만들기, 간선 쓰기, "KnownDll 아니면 `LdrpQueueWork` 로 넘긴다" 까지. 안 나는 것: dll1·dll2 를 **실제로 메모리에 맵하는** 코드(일감을 돌리는 `LdrpProcessWork` 이하, callee)와 일감이 어느 스레드에서 도는지 — 정적으로 `LdrpProcessWork +0x6a → … LdrpMapDllWithSectionHandle` 길은 알지만(sast/16·17) 이번 요청 밖.

## 판정

**분석성공.** 질문 1~8 에 `.asm` 줄 근거로 답했다. 남은 `[추측]`: 플래그 비트 이름(0x4·0x20·0x200·0x400), `ApiSetMap` 맵 버전 갈래, 간선 칸 비트 0 의 뜻, test.exe 가 정리 길·예외 길·기다림 길을 안 탄다는 것(동적), `LdrpQueueWork`/`LdrpProcessWork` 본문이 하는 일(범위 밖).

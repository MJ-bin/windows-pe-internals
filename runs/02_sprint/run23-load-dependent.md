# 2026-10-23 run23 — `LdrpLoadDependentModuleInternal`: import 이름 하나를 다듬고 → 이미 있나 보고 → 없으면 새 항목 + KnownDll 시도 → 락 안에서 간선(`+0xf69`/`+0xf8b`)을 쓰고 일감(`+0x182c LdrpQueueWork`)으로 넘긴다 (정적)

> **정정 (run24)**: "일감 vs 바로" 절의 `[추측]` "`LdrpQueueWork` 가 그 자리에서 다 올렸으면 `+0x12df` 에서 자식 `State ≥ 2` → 병렬 켜기 앞뒤 차이가 여기 나타날 수 있다" 는 틀렸다. `LdrpQueueWork` 는 큐에 넣기만 하고, 자식 `State = 2` 는 같은 `LdrpModuleDatatableLock` 안(`LdrpProcessMappedModule +0x1c2`)에서만 써지므로 새로 일감에 넣은 자식은 `+0x12df` 에서 늘 `State < 2` 다. 부모 카운터의 그 몫은 나중에 `LdrpSignalModuleMapped +0x45` 가 줄인다 → run24.

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 날짜는 작성일.
- 정적: ntdll (`ntdll.dll`, 26100.9457), pdb GUID+Age `C3093720177DA6851519DCFFE8DF53FE1` (run17 ~ run22 와 같은 파일)
  - analyze 도, export 도 다시 안 했다. `.asm`·`.c`·`_calls.tsv` 75줄은 sast/17 때 뽑아 둔 것 그대로.
  - 버전 대조: **일치.** `_calls.tsv` 의 `LdrpMapAndSnapDependency +0x349 → ret +0x34e LdrpLoadDependentModuleInternal`, `LdrpLoadDll +0xfb → ret +0x100 LdrpLoadDllInternal` 이 run22·run7 값과 같다. 이 함수 호출 줄 수도 요청서와 같다 (본체 74 + 예외 조각 `$fin$0` 1 = 75).
- 근거로 읽은 것
  - 본문: `LdrpLoadDependentModuleInternal` (rva `0x21ab0`, 9250바이트, 직접 호출 75줄). callee 본문은 하나도 안 읽었다 (`LdrpLoadKnownDll`, `LdrpQueueWork`, `LdrpProcessWork`, `LdrpFindLoadedDllByName`, `ApiSetp*` 등은 이름·인자·결과만).
  - 구조체 칸 이름은 PDB 타입(`llvm-pdbutil`)으로: `_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`(`+0x18 LoadCount`, `+0x28 Dependencies`, `+0x30 IncomingDependencies`, `+0x38 State`), `_LDRP_CSLIST`(칸 `Tail` 하나), `_LDR_DDAG_STATE`(음수 = 실패·내려가는 중, 0 PlaceHolder, 1 Mapping, 2 Mapped, 3 WaitingForDependencies, 4 Snapping, 5 Snapped … 9 ReadyToRun).
  - **간선 한 칸의 구조체 이름은 PDB 에 없다.** 이 문서는 오프셋 + 역할로만 쓴다. 로드 문맥(`_LDRP_LOAD_CONTEXT`)도 run22 처럼 칸 정보가 없어 오프셋 + 역할만.

## 목적

- 대기열 **Q12b**. run22 가 넘긴 질문 다섯 개를 이 함수 본문으로 푼다.
  - 그래프 **간선**("누가 누구에 기대나")을 적는 줄이 여기 있나.
  - "이미 올라와 있음 / KnownDll / 새로 올리기" 가 어떻게 갈리나, 인자 5 칸에 무엇이 들어가나.
  - `LdrpQueueWork`(일감) 와 `LdrpProcessWork`(바로) 를 부르는 조건.
  - 부모 문맥 `+0x6c`(남은 자식 수) 를 줄이는 조건, `State = 3`·반환 `0x103` 의 뜻.
  - 인자 4(`LoadReason`)·인자 6(임시 포인터) 이 하는 일, 반환값.
- 덤으로: 2.5 의 "kernel32 의 import 가 `api-ms-win-…` 같은 가상 이름이면 진짜 이름(KERNELBASE)으로 바뀐다" `[추측]` 의 자리.

## 방법

- 정적: `_workspace/sast/18_load-dependent.md` — ntdll, `^LdrpLoadDependentModuleInternal$` depth 1 (예외 조각 `$fin$0` 포함)
- 결과서: `_workspace/sast/18_load-dependent_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/ntdll/LdrpLoadDependentModuleInternal.asm`
  - `ms/disassemble/ntdll/_calls.tsv`
  - 구조 확인용 `ms/decomp/ntdll/LdrpLoadDependentModuleInternal.c`
- 오프셋 읽는 법 (run9 ~ 22 와 같음): `call` = call 명령 자리, `ret` = 그 다음 명령 = **WinDbg 콜스택에 찍히는 오프셋**. 이번 결과서는 두 자리를 다 적어 줬다. 이 문서도 표에는 둘 다, 글·그림 라벨에는 ret 를 쓴다. 메모리에 쓰는 줄(`MOV [..],..`)은 그 명령 자리 하나.
- 함수 안에 이름 다듬기(`LdrpPreprocessDllName`) 와 찾기·새 항목(`LdrpFindOrPrepareLoadingModule`) 모양의 코드가 **끼워져 들어가 있다** (로그 문자열이 그 두 이름으로 나옴: `+0x8c1`, `+0x15aa`, `+0xde8` 의 `LdrpLogInternal` 인자). 코드 주소 순서가 뒤죽박죽이라 아래는 **실행 순서**로 다시 맞춘 것이다 (결과서 정리).

## 결과

한 줄로: **`LdrpLoadDependentModuleInternal` 은 "import 이름 하나를 받아, 진짜 이름으로 다듬고, 이미 올라와 있으면 그걸 쓰고, 없으면 새 항목을 만들어 KnownDll 로 올려 보고, 그래도 안 되면 일감으로 넘긴다. 어느 쪽이든 끝에 락을 잡고 부모 → 자식 간선을 한 칸 적는다" 는 함수다.**

- 비유: 이삿짐 목록의 한 줄을 받은 심부름꾼. 먼저 별명으로 적힌 짐 이름을 진짜 이름으로 바꾸고(ApiSet), 창고에 이미 있나 본다. 있으면 "하나 더 쓴다" 표시만 한다. 없으면 새 짐표를 만들고, 공용 창고(KnownDll)에 있으면 거기서 바로 꺼내 온다. 공용 창고에도 없으면 "가져와 주세요" 쪽지를 일감 상자에 넣는다. 마지막엔 늘 장부에 "이 집은 이 짐에 기댄다" 한 줄을 적는다(간선).

### 정적 — 갈림길 (어느 길로 끝나나)

```
LdrpLoadDependentModuleInternal(이름, 부모문맥, 부모항목, 이유, &결과항목, &임시칸)
│
├─ [항상] 부모 항목의 활성화 컨텍스트를 이 스레드에 올림 (+0x71 ~ +0x1d3, 호출 없음)
│        (꼭대기 프레임 Flags & 0x70 ≠ 0x20 이면 +0x23f5 RtlRaiseException 0xc0150014)
├─ 이유 == 9 (PatchImage) 또는 문맥 flags 0x800000 (enclave) → 이름 다듬기 건너뜀
├─ 아니면 이름 다듬기: (a) ApiSet 풀이  (b) SxS 리다이렉트  (c) 경로·확장자
│        └ 어느 단계든 음수 → 로그 → 맨 끝 (간선 없음)
├─ [enclave] +0x1be7 LdrpFindOrPrepareEnclaveModule → 맨 끝 (간선 없음)
├─ *임시칸 이 비었으면 0x20바이트 간선 칸 할당 (+0x92e), *결과항목 = 0
│
├─ 찾기
│    ├ 이유 == 9        : 안 찾음 → 상태 0xc0000135
│    ├ flags 0x20 (맨 이름) : +0x16ee LdrpFindLoadedDllByName
│    ├ flags 0x200 (전체 이름) : 해시표 훑기 (락 공유 +0xb32 ~ +0xd1f)
│    └ 둘 다 아님      : 안 찾음 → 상태 0xc0000135
│
├─ ① 찾았다
│     노드 State < 0 → "순환 / 앞서 실패" 로그, 상태 0xc00000e5, 결과항목 = 0 → 맨 끝
│     아니면 +0x1513 LdrpIncrementModuleLoadCount, 상태 0 ─────────────────┐
│                                                                          │
├─ ③ 못 찾았다 (0xc0000135)                                                 │
│     새 로드 문맥 할당 (+0x10db) → +0x115c LdrpAllocateModuleEntry          │
│     → *결과항목 = 새 항목 (+0x1164), 항목 LoadReason = 이유 (+0x1179)     │
│     ├ ② 이유 ≠ 9 : +0x1792 LdrpLoadKnownDll  (0 = 그 안에서 올림 끝 / 0xc0000135 = KnownDll 아님)
│     └ flags 0x400 (경로를 직접 줌) 이고 아직 0xc0000135 : +0x1cae LdrpProcessWork(문맥, 1)
│                                                                          │
├─ [공통 꼬리: *결과항목 ≠ 0 이면 늘, 상태가 음수여도] ◀────────────────────┘
│     LdrpModuleDatatableLock 독점 (+0xe94 빠른 길 / 막히면 +0x1772)
│     항목 교체 확인 (+0x15d8) → ★ 간선 쓰기 (+0xf2c ~ +0xfbb)
│     상태 == 0xc0000135 이고 flags 0x400 아님 → +0x182c LdrpQueueWork(항목.LoadContext)   ★ 락 안
│     상태 ≥ 0 이면 부모 문맥 +0x6c 줄이기 / 기다림 표시 (+0x12df ~, +0x1606 ~)
│     참조(+0x114) 하나 내려놓기 (+0x1310) → 마지막 참조였으면 항목 정리 (+0x187a ~ +0x1f70)
│     락 풀기 (+0x132f)
└─ 활성화 컨텍스트 내림 (+0x2357), 지역 버퍼 정리, 반환 (+0x2395 RET, eax = 상태)
```

- 위 트리는 `[정적]` (결과서 질문 1 트리를 ret 라벨로 옮김). 실행 때 test.exe 가 어느 갈래를 탔는지는 모른다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv`)

묶음별 대표 (실행 순서). 잡일은 표 아래 개수만.

| 묶음 | caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|---|
| 1 입구 | `ntdll!LdrpLoadDependentModuleInternal` | `+0x23f0` | `+0x23f5` | `RtlRaiseException(0xc0150014)` — 활성화 컨텍스트 스택 꼭대기 프레임이 이상할 때만 | [정적] |
| 2 ApiSet | 〃 | `+0x2fa` | `+0x2ff` | `ApiSetpResolveHost(PEB.ApiSetMap, 이름, 글자 수, 1, 부모 BaseDllName, &api-set이었나, &진짜이름)` — 맵 모양이 새 쪽일 때 | [정적] |
| 〃 | 〃 | `+0x22b0` | `+0x22b5` | `ApiSetpSearchForApiSet` — 맵 모양이 옛 쪽이고 이름이 `api-`/`ext-` 로 시작할 때 | [정적] |
| 〃 | 〃 | `+0x22db` | `+0x22e0` | `ApiSetpSearchForApiSetHost` — 위에서 후보가 2개 이상이고 부모 이름이 있을 때 | [정적] |
| 3 이름 완성 | 〃 | `+0x1426` | `+0x142b` | `RtlDosApplyFileIsolationRedirection_Ustr` — 매니페스트(SxS) 리다이렉트 | [정적] |
| 〃 | 〃 | `+0x167c` | `+0x1681` | `RtlDetermineDosPathNameType_Ustr` — 이름에 경로가 있나 | [정적] |
| 〃 | 〃 | `+0x1695` | `+0x169a` | `LdrpGetFullPath` — 성공하면 flags `0x600` (`+0x16af`) | [정적] |
| 〃 | 〃 | `+0x148e` | `+0x1493` | `RtlGetFullPathName_Ustr` | [정적] |
| 4 갈림길 준비 | 〃 | `+0x929` | `+0x92e` | `RtlAllocateHeap(LdrpHeap, .., 0x20)` → `*인자6` — **간선 칸**, `*인자6 == 0` 일 때만 | [정적] |
| 〃 | 〃 | `+0x1be2` | `+0x1be7` | `LdrpFindOrPrepareEnclaveModule` — enclave 갈래만 | [정적] |
| 5 ① 이미 있나 | 〃 | `+0x16e9` | `+0x16ee` | `LdrpFindLoadedDllByName(이름, 0, flags, 인자5, &모듈상태)` — 맨 이름(flags `0x20`) | [정적] |
| 〃 | 〃 | `+0xb2d` / `+0xd1a` | `+0xb32` / `+0xd1f` | `RtlAcquireSRWLockShared` / `RtlReleaseSRWLockShared` — 전체 이름(flags `0x200`)일 때 해시표 훑기 짝 | [정적] |
| 〃 | 〃 | `+0x150e` | `+0x1513` | `LdrpIncrementModuleLoadCount(찾은 항목)` — 찾았고 상태가 음수 아닐 때 | [정적] |
| 〃 | 〃 | `+0xdfa` | `+0xdff` | `LdrpDereferenceModule` — 찾았는데 상태 음수 ("앞서 실패") | [정적] |
| 6 ③ 새로 | 〃 | `+0x10d6` | `+0x10db` | `RtlAllocateHeap(LdrpHeap, .., 이름 길이 + 0xd2)` — **새 로드 문맥** (간선 칸 아님) | [정적] |
| 〃 | 〃 | `+0x1157` | `+0x115c` | **`LdrpAllocateModuleEntry(새 문맥)`** → 결과를 인자 5 에 (`+0x1164`) | [정적] |
| 〃 | 〃 | `+0x1800` | `+0x1805` | `RtlFreeHeap(새 문맥)` — 위가 0 을 돌려줬을 때 | [정적] |
| 7 ② KnownDll · 바로 | 〃 | `+0x178d` | `+0x1792` | **`LdrpLoadKnownDll(새 항목.LoadContext)`** — 이유 ≠ 9 이면 새 항목마다 | [정적] |
| 〃 | 〃 | `+0x1ca9` | `+0x1cae` | `LdrpProcessWork(문맥, 1)` — flags `0x400` 이고 아직 `0xc0000135` 일 때만, **락 잡기 전** | [정적] |
| 8 공통 꼬리 | 〃 | `+0x176d` | `+0x1772` | `RtlpAcquireSRWLockExclusiveContended` — 락 독점 빠른 길(`+0xe94 BTS.LOCK`)이 막혔을 때 | [정적] |
| 〃 | 〃 | `+0x17bd` | `+0x17c2` | `RtlAllocateHeap(.., 0x20)` — **대비용 간선 칸**, `*인자6` 이 비어 `RCX == 0` 일 때만 | [정적] |
| 〃 | 〃 | `+0x1827` | `+0x182c` | **`LdrpQueueWork(항목.LoadContext)`** — 상태 `0xc0000135` 이고 flags `0x400` 아님, **락 독점 안** | [정적] |
| 〃 | 〃 | `+0x132a` | `+0x132f` | `RtlReleaseSRWLockExclusive` (`LdrpModuleDatatableLock`) | [정적] |
| 〃 | 〃 | `+0x133c` | `+0x1341` | `LdrpFreeReplacedModule` — 항목이 교체됐을 때 | [정적] |
| 9 마지막 참조 정리 | 〃 | `+0x187d` / `+0x1f03` | `+0x1882` / `+0x1f08` | `RtlAcquireSRWLockExclusive` / `RtlReleaseSRWLockExclusive` — 목록에서 빼기 | [정적] |
| 〃 | 〃 | `+0x1f12` | `+0x1f17` | `LdrpUnmapModule(항목)` | [정적] |
| 〃 | 〃 | `+0x1f64` | `+0x1f69` | `LdrpReleaseTlsEntry(항목, 0)` — `TlsIndex ≠ 0` 일 때 | [정적] |
| 〃 | 〃 | `+0x1f6b` | `+0x1f70` | `RtlReleaseActivationContext` — 항목 `+0x88` 이 0·-1 아닐 때 | [정적] |
| 〃 | 〃 | `+0x1f33` / `+0x1f44` | `+0x1f38` / `+0x1f49` | `LdrpFreeUnicodeString(FullDllName)` / `RtlFreeHeap(항목)` | [정적] |
| 〃 | 〃 | `+0x1f55` | `+0x1f5a` | `LdrpDestroyNode(노드)` — 노드에 이 항목 하나뿐이었을 때 | [정적] |
| 10 끝 | 〃 | `+0x2352` | `+0x2357` | `RtlDeactivateActivationContextUnsafeFast` — 활성화 컨텍스트 내림 | [정적] |
| 〃 | 〃 | `+0x237e` | `+0x2383` | `__security_check_cookie` | [정적] |
| 예외 | `$fin$0` | `+0x10` | `+0x15` | `RtlDeactivateActivationContextUnsafeFast` — 예외 때만 (호출로는 안 닿음) | [정적] |

- 묶음 2 의 로그 잡일: `+0x21a8 LdrpLogDllState`, `LdrpLogEtwEvent` 4곳, `RtlGetCurrentServiceSessionId` 6곳, `LdrpLogInternal` 3곳 (`+0x8c6`, `+0x15af`, `+0xded`), `wcslen` 2곳 (`+0x3c4`, `+0x19d2`), `LdrpAppendUnicodeStringToFilenameBuffer` 2곳 (`+0x166e`, `+0x1ccd`), `memset` 1곳 (`+0x8f`).
- 이름 버퍼 잡일 (Ghidra 가 고른 이름이 PDB 에 같은 주소 여러 개라 하는 일로 적는다):
  - 메모리 복사 8곳 (`+0x494`·`+0x5e9`·`+0x78d`·`+0x1145`·`+0x1d8e`·`+0x1dd0`·`+0x1e12`·`+0x1e54`). 같은 주소(rva `0x166500`) PDB 이름 2개: `memcpy`·`memmove`.
  - 버퍼 잡기 5곳 (`+0x1bfa`·`+0x1d56`·`+0x1d98`·`+0x1dda`·`+0x1e1c`). 같은 주소(rva `0x21a90`) 이름 3개: `NtdllpAllocateStringRoutine`·`RtlpCtAllocateMemory`·`RtlpAllocateAtom`.
  - 버퍼 놓기 3곳 (`+0x14e3`·`+0x1c20`·`+0x2371`). 같은 주소(rva `0x3d630`, run20·run22 와 같은 곳) 이름 9개, 후보: `NtdllpFreeStringRoutine`·`RtlpCtFreeMemory`·`RtlpFreeEnvBlock` 등.
  - 버퍼 키우기 `NtdllpReallocateStringRoutine` 4곳 (`+0x1930`·`+0x1a8f`·`+0x1b10`·`+0x1b8c`).

### 정적 — 간선(의존 관계)을 적는 줄

- **답: 이 함수 안에 있다** `[정적]`. 호출이 아니라 함수 안에서 직접 쓴다. 자리는 공통 꼬리, 락(`LdrpModuleDatatableLock` 독점) 안, 새 항목을 만든 뒤, `LdrpQueueWork` 앞.
- **어느 노드끼리**:
  - 부모 노드 = **인자 2(부모 문맥) `+0x38` 항목의 `DdagNode`** (`+0xeec ~ +0xeff`). 인자 3(부모 항목)이 아니다. 인자 3 은 활성화 컨텍스트, ApiSet 호스트 고르기, 새 문맥 `+0x30` 에만 쓰인다.
  - 자식 노드 = `*인자5` (찾은 항목이든 새 항목이든) 의 `DdagNode` (`+0xea1`, `+0xef8`).
- **쓰는 줄** (`_LDRP_CSLIST.Tail` 꼬리에 끼우기. 목록이 비어 있으면 칸이 자기 자신을 가리키게):

| 줄 | 하는 일 | 표시 |
|---|---|---|
| **`+0xf69`** `MOV [부모노드+0x28],칸` (앞 `+0xf63`·`+0xf66`) | 칸을 **부모 노드 `Dependencies`(+0x28)** 꼬리에 끼움 (비었으면 `+0x17e4`) | [정적] |
| `+0xf6d` `MOV [칸+0x8],자식노드` | 칸 `+0x08` = 자식 노드 | [정적] |
| **`+0xf8b`** `MOV [자식노드+0x30],칸+0x10` (앞 `+0xf82`·`+0xf88`) | 칸 `+0x10` 을 **자식 노드 `IncomingDependencies`(+0x30)** 꼬리에 끼움 (비었으면 `+0x17ec`) | [정적] |
| `+0xf96` `MOV [칸+0x18],부모노드` (+ `+0xf9c ~ +0xfa5`) | 칸 `+0x18` = 부모 노드 (맨 아래 비트 0 은 원래 값 유지) | [정적] |

- **간선 한 칸 = 0x20 바이트** (구조체 이름 없음): `+0x00` 부모 `Dependencies` 목록 고리 · `+0x08` 자식 노드 · `+0x10` 자식 `IncomingDependencies` 목록 고리 · `+0x18` 부모 노드. 비트 0 은 "대비용 `+0x17c2` 로 잡은 칸" 표시로 보인다 `[추측]`.
  - 한 칸이 두 목록에 동시에 걸린다. 부모 쪽에서 보면 "내가 기대는 자식", 자식 쪽에서 보면 "나에게 기대는 부모".
- **칸은 어디서 오나**:
  - 보통은 **`+0x92e`** (크기 `0x20`, `*인자6` 이 0 일 때). caller `LdrpMapAndSnapDependency +0x451 RtlFreeHeap(임시)` 가 안 쓰인 칸을 놓는 것과 맞다 → run22 의 "임시 포인터" `[추측]` 이 **간선 칸 보관 포인터**로 정해졌다 `[정적]`.
  - `+0x17c2` 는 대비용 (`+0xf4a` 에서 `RCX == 0` 일 때). 실패하면 `0xc0000017`. 이 길은 거의 안 오는 모양 `[추측]`.
  - `+0x10db` 는 간선 칸이 **아니다** (새 로드 문맥).
- 칸을 쓴 뒤 `*인자6 = 0` (`+0x12ab`). 안 썼으면 칸이 남아 caller 가 놓는다.
- **간선을 안 쓰는 경우** (`+0xf06`, `+0xf0f ~ +0xf26`, `+0xf2c ~ +0xf48`) `[정적]`:
  1. 부모 노드 == 자식 노드
  2. 자식 `State == 9`(ReadyToRun) 이고 (`LoadCount == -1` 이거나 자식 항목 `Flags` 의 `ProcessStaticImport` 비트가 켜짐)
  3. 부모 `Dependencies` 를 한 바퀴 돌아 이미 같은 자식이 있음
  - 이때는 `+0x1294` 로 가서 자식 노드 `LoadCount` 를 1 줄인다 (2 이상이고 -1 아닐 때). 해석 `[추측]`: `LoadCount` 가 "들어오는 간선 수" 를 대신해서, 새 간선이 안 생기면 찾을 때 올린 값(`+0x1513`)을 되돌린다.
- **상태가 음수여도 간선은 쓴다**: 꼬리로 들어가는 조건은 `*인자5 ≠ 0` 하나 (`+0xe0e ~ +0xe15`).
- 이 함수가 노드 `State` 에 쓰는 곳은 `+0x162d`(부모 노드 `State = 3`) 하나. `Modules` 에는 안 쓴다.

### 정적 — 세 갈림 (① 이미 있음 / ③ 새로 / ② KnownDll)

- **② KnownDll 은 ③ 새로 올리기 안에서 불린다** `[정적]`. 셋이 나란한 갈래가 아니다. 순서는 ① 찾기 → 못 찾으면 ③ 새 항목 → 그 새 항목으로 ② KnownDll 시도.
- **① 이미 올라와 있음**:
  - 이유 == 9 면 안 찾는다 (`+0x995`, 자리표시 이름 `ModuleNamePlaceholderBuffer`, 상태 `0xc0000135`).
  - 맨 이름(flags `0x20`, 이름에 `\`·`/` 가 없을 때 `+0x6fa`) → `+0x16ee LdrpFindLoadedDllByName`. 찾은 항목은 callee 가 인자 5 칸에 쓴다 (인자로 넘기는 것만 봄).
  - 전체 이름(flags `0x200`) → 함수 안에서 해시표를 훑는다: 이름 마지막 조각을 대문자로 바꿔 해시 → `LdrpHashTable[hash & 0x1f]` → 항목 `+0x108` 해시와 `FullDllName` 비교 → 찾으면 `+0xce7 ReferenceCount++`, `+0xcf6` 인자 5 칸에 저장.
  - 찾았는데 그 모듈 상태가 **음수** → 로그 "Found circular dependent DLL … failed to load previously" (`+0xded`), 상태 `0xc00000e5`, 인자 5 = 0 → 간선 없이 끝.
  - 아니면 `+0x1513 LdrpIncrementModuleLoadCount`, 상태 0. **간선은 생긴다** (위 세 예외만 빼고). 상태가 0 이라 `LdrpQueueWork` 는 안 탄다.
- **③ 새로 올리기** (`+0x10a3`): 인자 5 = 0 → `+0x10db` 새 로드 문맥 → 칸 채우기 → `+0x115c LdrpAllocateModuleEntry` → **`+0x1164` 인자 5 = 새 항목** → **`+0x1179` 항목 `LoadReason` = 인자 4**. 9 면 `+0x118a` `HotPatchState = 1`.
  - 새 문맥에 채우는 칸: `+0x20` = flags `| 0x8000`, `+0x10` = 부모 문맥 `+0x10`(경로 구조체), `+0x28` = 부모 문맥 `+0x28`(상태를 쓸 곳), **`+0x30` = 인자 3(부모 항목)**, `+0xb8` = -1, `+0x00` 이름 (`+0xd0` 에 복사) `[정적]`.
  - → run22 의 "이 갈래가 '아직 안 올라온 DLL 일 때만' 인지는 `[추측]`" 이 **정해졌다**: 찾기가 `0xc0000135`(못 찾음) 일 때만 탄다 `[정적]` (`+0xd92 CMP → +0x10a3`).
- **② KnownDll** (`+0x11e6`): 이유 ≠ 9 이면 새 항목 `LoadContext`(`+0xb0`) 로 **`+0x1792 LdrpLoadKnownDll`**. 어떤 이름이 KnownDll 인지는 callee 가 정한다 (이 함수는 조건 없이 부른다).
  - 결과 0 → callee 안에서 올림이 끝난 것 (지도: `LdrpLoadKnownDll +0xf1 → LdrpMapDllWithSectionHandle`) → 일감 안 넣음.
  - 결과 `0xc0000135` → KnownDll 아님 → 아래 일감 / 바로.
- **인자 5 칸에 들어가는 것**: ① 해시 길 = 찾은 항목 (`+0xcf6`), ① 맨 이름 길 = callee 가 씀 (넘기는 것만 봄), ③ = 새 항목 (`+0x1164`), 교체됐으면 문맥 쪽 항목 (`+0x1341`, `LdrpFreeReplacedModule` 바로 뒤. 결과서의 `+0x1345` 는 그다음 JMP 줄), 순환·실패 = 0.

### 정적 — 일감 vs 바로

| 호출 | 조건 (모두) | 락 | 뒤에 | 표시 |
|---|---|---|---|---|
| `+0x1827` (call) / **`+0x182c`** (ret) `LdrpQueueWork(항목.LoadContext)` | 인자 5 ≠ 0, 상태 == `0xc0000135` (`+0x12b2`), flags `0x400` 꺼짐 (`+0x1812`) | **`LdrpModuleDatatableLock` 독점을 잡은 채** (`+0xe94` ~ `+0x132f` 사이) | 상태 = 0 | [정적] |
| `+0x1ca9` (call) / **`+0x1cae`** (ret) `LdrpProcessWork(항목.LoadContext, 1)` | flags `0x400` (경로를 직접 줌, `LdrpGetFullPath` 성공) 이고 상태 == `0xc0000135` | 락 잡기 **전** | 문맥 `+0x20` 비트 26 켬(`+0x1c9d`), 상태 = `*(문맥+0x28)` | [정적] |

- 상태가 `0xc0000135` 인 길 = "새로 만들었고 KnownDll 도 아니었다" 또는 이유 9. **이미 올라와 있던 DLL 은 상태 0 이라 일감에 안 들어간다.**
- 상태 `0xc000022d` 는 0 으로 바꿔서 그냥 지나간다 (`+0x12be`). 이 값 이름은 `STATUS_RETRY` 로 알려져 있다 `[추측]`.
- **병렬 로딩이 켜졌는지 읽는 줄이 없다** `[정적]`. 읽는 전역은 `__security_cookie`, `NtdllBaseTag`, `LdrpHeap`, `LdrpModuleDatatableLock`, `LdrpHashTable`, `GlobalRtlNlsState`, `LdrpIsSecureProcess`, `LdrpDefaultExtension`, `LdrpIllegalCWDDevices`, `SlashSystem32SlashString`, `ModuleNamePlaceholderBuffer` 뿐. `LdrInitState`·`LdrpEnableParallelLoading` 결과는 없다.
- 그래서 kernel32 준비 때(병렬 켜기 앞)와 exe import 때(켜기 뒤)에 **이 함수 코드는 똑같이 돈다** `[정적]`. 다를 수 있는 건 두 가지:
  - 부모 문맥 `+0x20` 에서 물려받는 비트 (`0x800`·`0x800000`·`0x1000000`). 켜는 곳은 이 함수 밖.
  - `LdrpQueueWork` 본문 (Q12c·Q47). `LdrpQueueWork` 가 돌아온 **뒤** 자식 `State` 를 `+0x12df ~` 에서 읽으니, 그 자리에서 다 올렸으면 `State ≥ 2`, 미뤘으면 `< 2` → 부모 카운터가 달라진다. 병렬 켜기 앞뒤 차이가 여기 나타날 수 있다 `[추측]`.

### 정적 — 남은 자식 수와 기다림

근거 `+0x12ce ~ +0x12fa`, `+0x1606 ~ +0x163d`, `+0x1700 ~ +0x1711`. 상태가 0 이상 (또는 `0xc000022d`·일감으로 0 이 된 것) 일 때만 돈다.

- 부모 문맥 `+0x6c` 가 **0 이 아닐 때** (보통, `LdrpMapAndSnapDependency` 고리에서):
  - `*인자6 ≠ 0` (간선 칸이 남음 = 새 간선이 안 생김) **이거나** 자식 `State ≥ 2`(Mapped 이상) → **`+0x12f7` `+0x6c` = 값 − 1** `[정적]`.
  - 새 간선이 생겼는데 자식 `State < 2` 면 **줄이지 않는다** (`+0x1711 → +0x12fa`) `[정적]`.
  - 뜻 `[추측]`: "이미 끝난 자식, 새 간선이 필요 없는 자식은 바로 빼고, 새로 이어 붙였는데 아직 안 올라온 자식은 부모 몫으로 남긴다". 그 자식이 다 올라온 뒤 부모 카운터를 줄이는 코드는 **이 함수에 없다** (callee 쪽 → Q12c).
- 부모 문맥 `+0x6c` 가 **0 일 때** (`+0x12e4 → +0x1606`), 자식 `State < 2` 면:
  - `+0x1617` 문맥 `+0x50` = 자식 항목, `+0x161b` 문맥 `+0x6c` = 1, **`+0x162d` 부모 노드 `State = 3`**(WaitingForDependencies), **`+0x1634` 반환 상태 `0x103`** (`STATUS_PENDING` 값) `[정적: 줄]`.
  - 뜻 `[추측]`: "카운터가 이미 0 인데 안 끝난 자식이 새로 생겼다 → 그 하나를 기다리는 걸로 되돌리고 부모를 기다림 상태로 둔다".
- **`LdrpMapAndSnapDependency` 고리에서는 `+0x6c == 0` 이 안 나온다** `[정적: 카운터 줄]`: 카운터가 "자식 수 + 1" 로 시작하고 그 +1 은 고리 뒤 끝맺음(`MapAndSnap +0x47d`)에서만 놓는다. 그래서 `0x103` 은 다른 caller(`LdrpSnapModule +0x5df`, `LdrpLoadDependentModuleW +0xae`) 몫으로 보인다 `[추측]`.
- caller 가 `0x103` 을 받는 법 (`LdrpMapAndSnapDependency +0x34e ~ +0x352`): **음수일 때만** 실패로 본다. `0x103` 은 성공처럼 지나간다 `[정적]`.

### 정적 — ApiSet (가상 이름 → 진짜 이름)

- **가상 이름을 진짜 DLL 이름으로 바꾸는 자리가 이 함수 안이다** `[정적]`.
  - 새 맵 모양 (`PEB.ApiSetMap` 첫 바이트 7, 또는 버전 6 이고 `+0x10 > 8`·`+0x1c == 7`·`+0x1e & 4 == 0`): `+0x2ff ApiSetpResolveHost`. 부모 `BaseDllName` 도 넘긴다 (부모마다 다른 호스트를 고를 수 있게 `[추측]`).
  - 옛 맵 모양: 이름 첫 8바이트가 `api-`/`ext-` 면 마지막 `-` 뒤를 떼고 `+0x22b5 ApiSetpSearchForApiSet` → 후보가 둘 이상이고 부모 이름이 있으면 `+0x22e0 ApiSetpSearchForApiSetHost`.
  - api-set 이었으면: 진짜 이름 길이 0 이면 `0xc0000481` 로 실패. 아니면 **`NtSystemRoot` + `\System32\` + 진짜 이름** 전체 경로를 지역 버퍼에 만든다 (`+0x3ca ~ +0x5ff`). 로그 "… was redirected to … by API set" (`+0x15af`), flags `|= 0x200`.
- **바뀐 이름도 같은 ①②③ 을 탄다** `[정적]`. 다른 점: flags 에 `0x20`(맨 이름) 이 안 켜져서 `LdrpFindLoadedDllByName` 대신 **해시표 훑기 + 전체 이름 비교** 로 간다. 못 찾으면 ③ → ② KnownDll.
- SxS 리다이렉트는 api-set 길에서는 `ProcessParameters.Flags & 0x1000` 일 때만, 일반 이름은 `LdrpIsSecureProcess == 0` 이면 늘 시도 (`+0x13e7`).
- 2.5 와 이어짐 `[추측]`: kernel32 의 import 가 `api-ms-win-…` 라면 여기서 `KERNELBASE.dll` 전체 경로로 바뀐다. kernel32 의 import 목록은 이번에 안 봤다. 그러니 "이 함수가 바꿔 주는 자리가 있다" 까지만 `[정적]`.
- test.exe 의 `dll1.dll`·`dll2.dll` 은 일반 이름 `[소스]` → "api-set 아님" 쪽으로 지나갈 것 `[추측]`.

### 정적 — 인자와 결과

- **인자 4 (`LoadReason`)**: `+0x1ec`·`+0x995`·`+0x11e6` 에서 9(`PatchImage`) 와 비교 (이름 다듬기·찾기·KnownDll 건너뜀). **`+0x1179` 새 항목 `LoadReason(+0x10c)` = 인자 4**, 9 면 `+0x118a` `HotPatchState = 1`. ① 찾은 항목에는 안 쓴다. `LdrpMapAndSnapDependency` 는 0 을 넘긴다 (run22).
- **인자 6 = 간선 칸 보관 포인터의 주소** (위 간선 절).
- **반환값** (`+0x2371 MOV EAX,EBX`, `+0x2395 RET`):

| 값 | 언제 | 표시 |
|---|---|---|
| 0 | ① 찾아서 로드 수만 올림, ② KnownDll 로 올림 끝, 일감 넣고 돌아옴 | [정적] |
| `0x103` | 부모 `+0x6c == 0` 이고 자식 `State < 2` (`+0x1634`) | [정적] |
| `0xc0000135` | flags `0x400` 로 바로 올렸는데 못 찾음 | [정적] |
| `0xc0000017` | 힙 할당 실패 | [정적] |
| `0xc0000106` | 이름·경로가 `0xfffe` 넘음 | [정적] |
| `0xc0000481` | api-set 으로 풀었는데 진짜 이름 길이 0 | [정적] |
| `0xc00000e5` | 이미 있는 항목의 상태가 음수 | [정적] |
| 그 밖 | 이름 다듬기 callee 실패값 (`0xc0150008` 은 오류 아님 `+0x14f2`), enclave·ProcessWork·KnownDll 결과 그대로 | [정적] |

### 정적 — `+0x1f03 ~` 은 "마지막 참조 정리"

- **실패 정리가 아니다.** 이 함수가 들고 있던 참조를 내려놓았는데 그게 마지막이었을 때만 탄다 `[정적: 조건 줄]`.
  - `+0x1310 XADD.LOCK [항목+0x114], -1` (`ReferenceCount` 내림. 자식 노드 `LoadCount ≠ -1` 이고 `ProcessStaticImport` 꺼졌을 때). 옛 값이 1 이면 (`+0x1319`) → `+0x187a`.
  - 목록에서 빼고 (`NodeModuleLink` 이웃이 깨졌으면 `+0x18a4 INT 0x29`), `LdrpReleaseTlsEntry` → `LdrpUnmapModule` → `RtlReleaseActivationContext` → 이름·항목 놓기 → 노드가 비면 `LdrpDestroyNode`.
- 새 항목은 `ReferenceCount = 2` 로 시작 (run22), ① 은 `+0xce7` 에서 +1. 이 함수는 −1 만 하니 **정상으로 올라온 DLL 은 이 길을 안 탄다** `[추측]`. 다른 쪽이 먼저 놓았을 때만.
- `+0x23f5 RtlRaiseException(0xc0150014)` 는 활성화 컨텍스트 스택 꼭대기 프레임의 `Flags & 0x70 ≠ 0x20` 일 때. 끝나면 `+0x23f5 JMP` 로 `+0x1de` 에 돌아와 계속 간다 (결과서의 `+0x1c8e` 는 점프 대상 절대 주소 `0x180021c8e` 의 끝자리를 적은 것. 오프셋으로는 `+0x1de`). test.exe 길에선 안 탈 것 `[추측]`.

### 정적 — 활성화 컨텍스트

- 함수 전체가 **부모 항목의 `EntryPointActivationContext`(+0x88)** 를 켠 채 돈다 (`+0xf1`, 프레임 `[RSP+0x260]`, TEB `+0x2c8`). 이미 같은 것이 꼭대기면 생략. 그래서 SxS 리다이렉트가 **부모의 매니페스트 기준** 으로 이름을 바꿀 것 `[추측]`.
- 내림: 정상 `+0x2357`, 예외 `$fin$0`.

### 정적 — test.exe → dll1 → dll2 이야기 (run22 그림의 `?` 칸 채우기)

```
LdrpLoadDependentModuleInternal("dll1.dll", exe문맥, exe 항목, 0, &exe문맥.58[i], &임시)   ← MapAndSnap +0x34e
  부모(exe 항목) 활성화 컨텍스트 올림
  이름 다듬기: api-set 아님 → SxS 시도 → 맨 이름(0x20) → ".dll" 확인          [정적: 길] / 실제 값 [추측]
  *임시 에 0x20바이트 간선 칸 (+0x92e)
  +0x16ee LdrpFindLoadedDllByName → 0xc0000135 (아직 없음)                       [추측]
  새 dll1문맥 (+0x30 = exe 항목) → +0x115c LdrpAllocateModuleEntry
     → dll1 항목 + 노드, *인자5 = dll1 항목, LoadReason 0 (+0x1179)            [정적]
  +0x1792 LdrpLoadKnownDll(dll1문맥) → 0xc0000135 (KnownDll 아님)                [추측]
  락 독점
    ★ 간선: exe 노드.Dependencies ← 칸 → dll1 노드.IncomingDependencies (+0xf69, +0xf8b)   ← run22 "? 간선"
    +0x182c LdrpQueueWork(dll1문맥)  (락 안)                                      ← run22 "? … 올리기"
    exe문맥 +0x6c : dll1 State < 2 면 그대로, ≥ 2 면 −1                            ← run22 "남은 수 = ?"
  참조 −1, 락 풀기, 반환 0
```

- 아직 `?` 인 것:
  - `LdrpQueueWork` 가 돌아왔을 때 dll1 `State` (그 자리에서 올려 주나, 일꾼으로 미루나) → Q12c (본문), Q47 (실행).
  - dll1문맥으로 `LdrpMapDllWithSectionHandle` → `LdrpMapAndSnapDependency` 를 **누가, 어느 스레드에서** 부르나 → Q12c·Q47.
  - 나중에 exe문맥 `+0x6c` 를 줄이고 exe snap 으로 잇는 쪽 (이 함수에도 `MapAndSnap` 에도 없다) → Q12c.
- 정적으로 한 칸 더 읽히는 것: run22 의 "exe 의 남은 수 = ?" 는, dll1 이 `LdrpQueueWork` 뒤에도 `State < 2` 라면 **2 (자식 1 + 1) → 끝맺음에서 1 → 0 이 아니라 snap 안 함** 이다 (`MapAndSnap +0x47d ~ +0x49a`). `State ≥ 2` 라면 1 → 끝맺음에서 0 → `+0x4c2 LdrpSnapModule`. 어느 쪽인지는 `[추측]` (Q47).
- run8 `[관찰]` ("`+0x1bb6` 에서 dll1·dll2 가 이미 올라와 있었고 켜는 순서 dll2 → dll1") 과 **어긋나지 않는다**: 일감을 넣는 건 여기, 일감이 끝나길 기다리는 건 `LdrpInitializeProcess +0x1ab1 LdrpDrainWorkQueue(1)` (run19).

### 호출 관계

```
LdrpMapAndSnapDependency [test.exe]
 └─ (3) +0x34e  LdrpLoadDependentModuleInternal [dll1 추정]           [정적] run22·run23
      ├─ (1) +0x2ff   ApiSetpResolveHost          맵 모양이 새 쪽일 때 (옛 쪽은 +0x22b5 / +0x22e0)   [정적]
      ├─ (2) +0x16ee  LdrpFindLoadedDllByName     맨 이름일 때 (전체 이름은 해시 훑기)               [정적]
      ├─ (3) +0x115c  LdrpAllocateModuleEntry     못 찾았을 때만 (새 문맥 +0x10db 뒤)                [정적]
      ├─ (4) +0x1792  LdrpLoadKnownDll            새 항목마다 (이유 ≠ 9)                             [정적]
      │      [바로 올리기 +0x1cae LdrpProcessWork 는 경로를 직접 준 이름만 — dll1 은 안 탈 것 [추측]]
      │      ── 락 독점 → 간선 +0xf69 / +0xf8b (호출 아님) ──
      ├─ (5) +0x182c  LdrpQueueWork               상태 0xc0000135 · flags 0x400 아님 · 락 안         [정적]
      │      └─ ?  LdrpMapDllWithSectionHandle [dll1 추정]   누가·어느 스레드에서인지 Q12c·Q47      [추측]
      │            └─ +0x1c8  LdrpMapAndSnapDependency [dll1 추정]                                  [정적] run22
      └─ (끝) 부모 +0x6c 줄이기 +0x12f7 / 기다림 +0x162d·+0x1634 (호출 아님)
```

## 확인된 것

- (`[정적]`) 그래프 **간선은 `LdrpLoadDependentModuleInternal` 안에서 쓴다.** `+0xf69` 부모 노드 `Dependencies`(+0x28), `+0xf8b` 자식 노드 `IncomingDependencies`(+0x30). 한 칸 0x20바이트(`+0x92e` 할당)가 두 목록에 같이 걸린다. 부모 노드는 인자 2(부모 문맥) `+0x38` 항목의 노드다.
- (`[정적]`) 간선은 **`LdrpModuleDatatableLock` 독점** 안에서 쓴다 (`+0xe94` 잡음 ~ `+0x132f` 풀음). 같은 락 안에서 `+0x182c LdrpQueueWork` 를 부른다.
- (`[정적]`) 갈림 순서 = ① 찾기 → 못 찾으면 ③ 새 문맥·항목(`+0x115c`) → 그 항목으로 ② `+0x1792 LdrpLoadKnownDll`. ② 는 ③ 안이다.
- (`[정적]`) ① 이미 올라와 있으면 `+0x1513 LdrpIncrementModuleLoadCount` 만 하고 **간선은 생긴다** (자기 자신·이미 같은 간선·`State 9` 예외 셋 빼고). 일감은 안 넣는다.
- (`[정적]`) `LdrpQueueWork`(`+0x182c`) 조건 = 새 항목이 KnownDll 도 아님(상태 `0xc0000135`) + 경로를 직접 준 이름 아님(flags `0x400` 꺼짐). `LdrpProcessWork`(`+0x1cae`) 는 경로를 직접 준 이름일 때만, 락 잡기 전.
- (`[정적]`) 병렬 로딩 켜짐 여부를 읽지 않는다. kernel32 준비 때와 exe import 때 같은 코드.
- (`[정적]`) 부모 문맥 `+0x6c` 는 "새 간선이 안 생겼거나 자식이 `State ≥ 2`" 일 때만 1 줄인다. 카운터가 0 인데 안 끝난 자식이 생기면 부모 노드 `State = 3`, 반환 `0x103`.
- (`[정적]`) `LdrpMapAndSnapDependency +0x34e` 쪽은 음수만 실패로 본다 (`0x103` 은 성공처럼).
- (`[정적]`) api-set 이름을 진짜 이름 + `System32` 전체 경로로 바꾸는 자리가 이 함수 안 (`+0x2ff`, 옛 맵 `+0x22b5`·`+0x22e0`). 바뀐 이름도 같은 ①②③ 을 탄다.
- (`[정적]`) 새 항목 `LoadReason` = 인자 4 (`+0x1179`). 인자 6 = 간선 칸 보관 포인터 (run22 `[추측]` "임시 포인터" 를 정함).
- (`[정적]`) `+0x1f03 ~ +0x1f70` 은 실패 정리가 아니라 **마지막 참조 정리** 길.

## 정정

- run22 가 "아직 확인 필요" 로 둔 **"이미 올라옴 / KnownDll / 새로 올리기" 세 갈래**는 나란한 세 형제가 아니다. KnownDll 은 "새로 올리기" 안에서 새 항목을 만든 **뒤** 시도한다. run22 는 이걸 질문으로만 적었고 사실로 쓰지 않아서, run22 문서 맨 위 정정 안내는 넣지 않았다. flow.md 2.6 의 같은 틀(`?` 칸 설명)은 이번에 고쳤다.
- run21·run22 가 "파일 길" 이라고 적은 지도 이름 사슬 `LdrpLoadDependentModuleInternal +0x1cae LdrpProcessWork → …` 는 사슬 자체는 맞다. 다만 이 호출은 **경로를 직접 준 이름(flags `0x400`)일 때만** 탄다 `[정적]`. dll1·dll2 처럼 맨 이름은 KnownDll 이 아니면 `+0x182c LdrpQueueWork` 로 간다. flow.md 2.6 의 그 문장에 조건을 덧붙였다.

## 아직 확인 필요

- **일감 뒤 실제로 누가 올리나** — `LdrpQueueWork`·`LdrpProcessWork`·`LdrpDrainWorkQueue` 본문, 자식이 다 올라온 뒤 부모 `+0x6c` 를 줄이고 snap 으로 잇는 코드, 그래프 정리(`LdrpCondenseGraph` 계열) → 정적 **Q12c**.
- **주소 연결 `LdrpSnapModule`** (이 함수를 `+0x5df` 에서 부르는 갈래, `0x103` 을 받는 caller 후보) → 정적 **Q50**.
- **dll1·dll2 가 실제로 어느 갈래를 탔나** — 동적 **Q23**. 멈춤 (`ntdll+rva`, 이 함수 시작 `ntdll+0x21ab0`):
  - 입구 `ntdll+0x21ab0`: `du poi(@rcx+8)`(이름), `dq @rdx+0x30 L1`, `dq @rdx+0x38 L1`, `dd @rdx+0x6c L1`, `r r9d`, `dd @rdx+0x20 L1`.
  - `FindLoaded` ret `ntdll+0x2319e`: `r eax` (`c0000135` 인가). 새 항목 ret `ntdll+0x22c0c`: `dd poi(@rax+0x98)+0x38 L1`.
  - 간선 직후 `ntdll+0x22a58`: `dq @rcx L4`, `dq @r14+0x28 L1`, `dq @rbx+0x30 L1`. 부모 카운터 `ntdll+0x22d8f`.
  - 안 서야 할 곳: `0x103` 길 `ntdll+0x230c7`, 마지막 참조 `ntdll+0x2332d`, 바로 올리기 `ntdll+0x23759`. 반환 `ntdll+0x23e45` 에서 `r eax`.
- **kernel32 import 가 api-set 인가, kernelbase 가 KnownDll 로 올라오나, 락** — 동적 **Q20**. `ApiSetpResolveHost` ret `ntdll+0x21daf` 에서 `r eax`·`db @rsp+0x50 L1`·`dS @rsp+0x108`. KnownDll ret `ntdll+0x23242` 에서 `r eax`.
- **`LdrpQueueWork` 가 어느 스레드에서 도나, 정말 락을 잡은 채인가** — 동적 **Q47**. call `ntdll+0x232d7` 에서 `dq ntdll!LdrpModuleDatatableLock L1`(비트 0), `~`, `kb`. ret `ntdll+0x232dc` 에서 자식 `State`.
- 이름·뜻 못 정한 것: flags 비트 `0x4`·`0x20`·`0x200`·`0x400`·`0x8000`·`0x800000`·`0x4000000` 의 이름, 간선 칸 비트 0 의 뜻, 간선 칸 구조체 이름 (PDB 에 없음), `ApiSetMap` 버전 갈래, `0xc000022d`·`0xc0150014` 이름, TEB `+0x1850` 8칸 표 (SRW 락 소유 추적 `[추측]`), 문맥 `+0xb8`(-1).
- Q14 ("dll1·dll2 를 올리는 쪽") 는 **아직 일부**: 이름 처리·새 항목·간선·"KnownDll 아니면 일감" 까지 `[정적]`. 실제로 메모리에 맵하는 쪽(일감을 돌리는 `LdrpProcessWork` 이하)과 스레드는 Q12c·Q47 뒤.

## 디버거 메모 / 도구 메모

- 디버거는 안 썼다.
- 결과서 한 곳 고쳐 읽음: `RtlRaiseException` 뒤 돌아가는 자리 "`+0x1c8e`" 는 `.asm` 의 `+0x23f5 JMP 0x180021c8e` 의 절대 주소 끝자리다. 오프셋은 `+0x1de` (`.asm` 81줄). 이 문서·flow.md 는 `+0x1de` 로 적었다.
- 같은 식으로 한 곳 더: 교체된 항목을 인자 5 칸에 쓰는 줄은 `+0x1341 MOV [R13],RSI` 다 (결과서 `+0x1345` 는 그다음 `JMP`).
- 결과서가 call/ret 를 둘 다 적어 줘서 이번엔 `_calls.tsv` 로 따로 맞출 일이 거의 없었다 (run22 에서 decompiler 에 한 제안이 반영됨).
- 별칭 확인함: `pdb_aliases.py --lookup ntdll.dll 0x166500` (2개), `0x21a90` (3개), `0x3d630` (9개). 문서엔 하는 일로 적었다.

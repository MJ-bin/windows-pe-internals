# 30 ntdll-entry — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 02_sprint (예정 run35)
- 관련: 대기열 Q45 (단계 3 정적 묶음, 출처 sast/13·run18 "새 정적 후보"), flow.md 2.4 `LdrpInitializeProcess` 묶음 10, 4장 공백 "`LdrpInitializeNtdllDataTableEntry` 본문 (Q45)"
- 이전 요청: sast/13 (proc-init-setup), sast/14 (exe 항목 `+0x1450`), sast/21 (`LdrpMapDllWithSectionHandle +0x141`)

## 알고 싶은 것

1. `ntdll!LdrpInitializeNtdllDataTableEntry` (rva `0x7015c`, 633B) 가 하는 일을 **순서대로** 대표 지점 수준으로 (호출 오프셋 + 한 줄 뜻). 특히:
   - 1a. 항목은 새로 할당하나(`+0x67 LdrpAllocateModuleEntry`), 아니면 둘째 인자 `&LdrpNtDllDataTableEntry` 같은 전역 칸을 그대로 채우나? 둘째 인자에 무엇이 저장되나?
   - 1b. `FullDllName`·`BaseDllName` 은 어떻게 만드나 (`+0xcb RtlAppendUnicodeStringToString` — 넷째 인자 `<NtSystemRoot>\System32\` 뒤에 `ntdll.dll` 을 붙이나?)
2. **PEB.Ldr 적재 순서(`+0x10`)·메모리 순서(`+0x20`) 목록에 넣는 곳은 `+0xf2 LdrpInsertDataTableEntry` 인가? (예/아니오)**
   - `LdrpInsertDataTableEntry` (rva `0xb380`, 274B) 본문도 depth 0 으로 읽어서: 정말 `PebLdr+0x10`·`+0x20` 두 목록 **꼬리**에 끼우나, 해시 표(`LdrpHashTable`, `+0x1d` `LdrpHashUnicodeString`)에도 끼우나. sast/13 은 `refs PebLdr` 로만 짐작했다 (`[추측]`).
3. **색인**: `+0x111 LdrpInsertModuleToIndex` 는 무슨 색인에 넣나 (주소 기준 트리 `LdrpModuleBaseAddressIndex` 등 — 이름은 PDB/전역 이름으로 확인). 같은 이름의 `...LockHeld` 와의 관계 한 줄.
4. `+0x11f LdrpProcessMappedModule` 를 ntdll 항목에 부르는 이유 한 줄 (인자, DDAG `State` 를 몇으로 만드나 — sast/14·sast/18 에서 exe·DLL 은 이 뒤 State 2/9).
5. **둘째 호출(`LdrpInitializeProcess +0x126a`, 핫패치 컨텍스트가 있을 때만)의 정체**: 함수 뒤쪽 `+0x1ab ~ +0x23e` (`NtQueryVirtualMemory`, `LdrpResolvePatchDllName`, `LdrpFreeUnicodeString`, `LdrpRecordModuleDependency`) 갈래는 어떤 인자/조건일 때 타나? 첫째 호출(보통 ntdll)과 무엇이 다른가 — "패치된 ntdll" 이미지의 항목을 만들고 원래 ntdll 과 의존 간선을 잇는 것인가? (예/아니오 + 근거 줄)
6. 보통 프로세스(test.exe, 핫패치 꺼짐)에서 이 함수가 끝난 뒤 PEB.Ldr 목록에 ntdll 하나만 있다는 run18 결론 (`[추측]` 부분 포함) 과 맞나.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpInitializeNtdllDataTableEntry$` | 1 | 본체 — 질문 1~6 |
| ntdll | `^LdrpInsertDataTableEntry$` | 0 | 질문 2 — 목록 두 개·해시 표 끼우는 줄 확인 (이미 `.asm` 있음, 다시 뽑지 않아도 됨) |
| ntdll | `^LdrpInsertModuleToIndex$` | 0 | 질문 3 — 이름만 필요하면 depth 0 훑기로 충분 |

- depth 1 callee 중 `LdrpResolvePatchDllName`, `LdrpRecordModuleDependency` 는 **이름과 인자만** (본문 깊이 읽지 않음). `LdrpLogDllState`·`LdrpLogInternal`·`memset`·`__security_check_cookie` 는 건너뜀.
- 범위 밖: `LdrpProcessMappedModule` 본문 (sast/18·21 에서 일부 봄), 핫패치 자체(`LdrpLoadPatchedNtdll`, run17).

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpInitializeNtdllDataTableEntry` rva `0x7015c`, 633B, 깊이 5, caller 1개 함수 (`LdrpInitializeProcess` `+0x119d/+0x11a2`, `+0x126a/+0x126f` 두 곳), expanded.
- 직접 호출 15줄: `+0x46 memset`, `+0x60 RtlImageNtHeaderEx`, `+0x67 LdrpAllocateModuleEntry`, `+0xcb RtlAppendUnicodeStringToString`, `+0xf2 LdrpInsertDataTableEntry`, `+0x104 LdrpLogDllState`, `+0x111 LdrpInsertModuleToIndex`, `+0x11f LdrpProcessMappedModule`, `+0x13b LdrpLogDllState`, `+0x16c __security_check_cookie`, `+0x1ab LdrpLogInternal`, `+0x1df NtQueryVirtualMemory`, `+0x1fe LdrpResolvePatchDllName`, `+0x216 LdrpFreeUnicodeString`, `+0x23e LdrpRecordModuleDependency`.
  - `+0x16c` cookie 검사 뒤에 있는 `+0x1ab ~ +0x23e` 는 함수 끝 뒤 갈래(cold 블록)일 수 있음 `[추측]` — 어디서 점프해 오는지 확인.
- `LdrpInsertDataTableEntry` rva `0xb380`, 274B, caller 4곳 (`LdrpInitializeNtdllDataTableEntry +0xf2`, `LdrpInitializeProcess +0x1450`·`+0x1625`, `LdrpMapDllWithSectionHandle +0x141`), 직접 호출 `+0x1d LdrpHashUnicodeString` 하나.

## 이미 아는 것

- `[정적]` (sast/13) 첫째 호출 인자: RCX = ntdll 베이스(`[RSP+0xd8]`, `LdrpInitializeProcess` 입구 RDX), RDX = `&LdrpNtDllDataTableEntry`, R9 = `<NtSystemRoot>\System32\` 문자열. 둘째 호출(`+0x126a`)은 `LdrpNtdllHotPatchContext` 의 `[ctx]` 가 있을 때만, 결과는 `[RSP+0x160]` → 초기화 순서 목록에 둘째로 끼움 (`+0x12b9~+0x12d7`).
- `[정적]` (sast/13) `LdrpInitializeProcess +0x1283~+0x12d7` 이 초기화 순서 목록(`PebLdr+0x30`)에 ntdll 항목 `+0x20` 을 끼움. 적재·메모리 순서 목록에 넣는 줄은 그 구간에 없음.
- `[정적]` (sast/16 표) `LdrpHashTable` 끼움은 `LdrpInsertDataTableEntry +0x2b/+0x3d/+0x7a`.
- `[추측]` (sast/13·run18) 적재·메모리 순서 목록에 넣는 건 `LdrpInsertDataTableEntry` (refs PebLdr 로만 봄). 둘째 호출은 "패치된 ntdll" 항목.
- `[정적]` (run17) `LdrpNtdllHotPatchContext` 칸은 핫패치가 켜졌을 때 `LdrpLoadPatchedNtdll` 만 채움 — 보통 프로세스는 0.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x` 줄). PDB 구조체(`_LDR_DATA_TABLE_ENTRY`, `_PEB_LDR_DATA`) 칸 이름은 PDB 타입으로 확인해서 적기.
- 대표 지점으로 쓸 만한 곳 1~3개 (그림 2 `(0) 프로세스 준비` 상자에 넣을 것).
- 동적으로 확인할 멈춤점 추천 (함수+오프셋 또는 `ntdll+rva`) — 따로 동적 항목을 만들지 않고 Q44 출처로 붙일 수 있게, Q44 와 같은 기준점(test.exe, `LdrpInitializeProcess` 앞쪽)에서 볼 수 있는 것 위주.

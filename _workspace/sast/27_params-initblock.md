# 27 params-initblock — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run32)
- 대기열: Q36 (`_workspace/plan.md`) — 2.1 `nt!PspCopyAndFixupParameters` (PEB `+0x20` 프로세스 매개변수를 누가 쓰나), `nt!PspPrepareSystemDllInitBlock` (ntdll 로 넘기는 시작 정보). 단계 3 "작은 정적부터 몰아서" (사용자 결정 2026-10-23).
- 관련: flow.md 2.1 `nt!PspSetupUserProcessAddressSpace` 절 `[추측]` 두 줄, `MmCreatePeb` 절 "PEB `+0x18`, `+0x20`, `+0x30` 은 쓰지 않는다 … 누가 채우는지 모른다", 4장 2.1·2.4 줄 ("PEB `+0x20` 은 `MmCreatePeb` 가 안 씀 → 누가 쓰나", "`PspPrepareSystemDllInitBlock` 안쪽") / 이전 요청 sast/04 (run10), sast/09 (run14, 부모 `BasepCreateProcessParameters` → `NtCreateUserProcess` 9번째 인자), sast/13 (run18, ntdll 이 `LdrSystemDllInitBlock+0xa0` 을 읽음), sast/23 (run28, `LdrSystemDllInitBlock+0xb8` CFG 비트맵)

## 범위

- **대표 지점 수준** (CLAUDE.md). 함수마다 하는 일을 3 ~ 6 묶음으로, 질문에 걸린 줄만 줄 단위로.
- 대상 둘 (depth 1, 지도 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*` 에 있음):
  - `PspCopyAndFixupParameters` (rva `0xae57e4`, 1340B, 깊이 3, caller `PspSetupUserProcessAddressSpace +0x1b4` (ret `+0x1b9`), 호출 9: `ObDuplicateObject` ×2, `Feature_CreateProcessSegmentHeapEnablement…`, `RtlCopyToUser`, `RtlWriteULong64ToUser` ×2, `PspCopyUserAttributes`, `RtlCopyMemory`)
  - `PspPrepareSystemDllInitBlock` (rva `0x9395d0`, 812B, 깊이 2, caller 3: `PspAllocateProcess +0x25dc`·`+0x25fc`, `PspSetupUserProcessAddressSpace +0x1e6`; 호출 5: `PsWow64GetProcessNtdllType`, `PspWow64GetSharedInformation` ×2, `ExGenRandom`, `PspGetScpCfgFunctions`)
- 부르는 쪽 줄만 더 봐도 된다: `PspSetupUserProcessAddressSpace` 의 `+0x19c ZwAllocateVirtualMemory` ~ `+0x1e6` 구간 (인자 싣는 줄), `PspAllocateProcess +0x25d7`·`+0x25f7` 앞뒤 (어느 갈래인가).
- callee 본문은 읽지 않는다. 오프셋은 call·ret 둘 다, 점프 목적지는 `절대 주소 − 함수 시작`. 구조체 칸은 PDB 타입으로 (`_RTL_USER_PROCESS_PARAMETERS`, `_PEB`, `_PS_SYSTEM_DLL_INIT_BLOCK` 같은 것이 있으면).

## 알고 싶은 것

1. **PEB `+0x20` (`ProcessParameters`) 를 누가 쓰나**
   - `PspCopyAndFixupParameters` 가 새 PEB `+0x20` 에 쓰는가 (예/아니오, 줄). 아니면 `PspSetupUserProcessAddressSpace` 쪽인가.
   - 매개변수 원본은 어디서 오나 — 부모가 `NtCreateUserProcess` 9번째 인자로 넘긴 `_RTL_USER_PROCESS_PARAMETERS` (run14 `BasepCreateProcessParameters` `[추측]`) 를 새 프로세스 메모리(`+0x19c ZwAllocateVirtualMemory` 로 잡은 곳?)로 **복사**하나.
   - "Fixup" 이 무엇인가 — 안쪽 포인터(명령줄·이미지 경로·환경 블록 등 `UNICODE_STRING.Buffer`)를 새 주소 기준으로 고치나, 오프셋(정규화되지 않은 꼴)으로 두나. `Flags` 의 정규화 비트(`RTL_USER_PROC_PARAMS_NORMALIZED` 류) 를 켜나. 대표만.
   - 핸들 두 번 복제(`ObDuplicateObject` ret `+0xbc`·`+0x130`) 는 무엇인가 — 표준 입출력 핸들? (이름과 한 줄)
2. **`PspPrepareSystemDllInitBlock` 이 만드는 것**
   - 어디에 쓰나 — 커널 안 전역 표인가, **새 프로세스 안 ntdll 의 `LdrSystemDllInitBlock`** 인가 (주소를 어디서 얻나). 이름으로 답.
   - 채우는 칸 대표 (PDB 타입 이름이 있으면 그 칸 이름): 버전/크기, Wow64 정보, `ExGenRandom` 으로 만드는 값 (쿠키?), `PspGetScpCfgFunctions` (CFG 관련 함수 주소?), 그리고 run18·run28 이 ntdll 쪽에서 읽는 **`+0xa0`(무결성 설정 비트)·`+0xb8`(CFG 비트맵)** 을 여기서 채우나.
   - caller 셋의 차이: `PspAllocateProcess +0x25dc`·`+0x25fc` 와 `PspSetupUserProcessAddressSpace +0x1e6` — test.exe (보통 64비트, 복제 아님) 는 어느 자리를 타나 (정적 조건).
3. **순서**: `PspSetupUserProcessAddressSpace` 안에서 할당 → 매개변수 복사 → 시작 정보 블록 → ApiSet 뷰 순서 (run10) 가 맞나, 사이에 PEB `+0x20` 을 쓰는 줄이 어디 끼나.
4. **ntdll 과 이어 붙이기**: 이번에 채운 칸을 ntdll 이 읽는 첫 자리 (이미 아는 것: run18 `LdrpInitializeProcess` 쪽 `LdrSystemDllInitBlock+0xa0`, run28 `LdrpDispatchUserCallTarget` 의 `+0xb8`, ntdll 이 PEB `+0x20` 을 읽는 곳) 와 짝을 지어 표 하나 — 커널 쓰는 줄 ↔ ntdll 읽는 줄 (ntdll 쪽은 이미 문서에 있는 것만, 새로 읽지 않음).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^PspCopyAndFixupParameters$` | 1 | 질문 1·3 |
| ntoskrnl | `^PspPrepareSystemDllInitBlock$` | 1 | 질문 2·3·4 |
| ntoskrnl | `^PspSetupUserProcessAddressSpace$` | 0 | 질문 1·3 (이미 있으면 다시 안 뽑아도 됨, `+0x19c ~ +0x1e6` 줄만) |

## 지도에서 본 것

- 위 "범위" 의 호출 목록과 caller (지도 `_edges.tsv`).
- `PspSetupUserProcessAddressSpace` 는 `PspAllocateProcess +0x239c` 에서 (run10).

## 이미 아는 것

- `[정적]` (run10) `MmCreatePeb` 는 PEB `+0x18`, `+0x20`, `+0x30` 을 쓰지 않는다.
- `[정적]` (run10) `PspSetupUserProcessAddressSpace`: 새 프로세스 안으로 들어가(`+0xfa`) `ZwAllocateVirtualMemory` `+0x19c` → `PspCopyAndFixupParameters` `+0x1b9` → `PspPrepareSystemDllInitBlock` `+0x1e6` → `MmMapApiSetView` `+0x255` → `PspMapSiloSharedDataView` `+0x263`.
- `[추측]` (run14) 부모 `KERNELBASE!CreateProcessInternalW +0x2de4 BasepCreateProcessParameters` 가 `NtCreateUserProcess` 9번째 인자(매개변수)를 만든다.
- `[정적]` (run18) ntdll `LdrpInitializeProcess` 가 `LdrSystemDllInitBlock+0xa0` (무결성 설정 비트) 를 읽는다. (run28) `LdrpDispatchUserCallTarget` 가 `LdrSystemDllInitBlock+0xb8` (CFG 비트맵) 을 본다.
- `[정적]` (run17·run18 등) ntdll 이 `PEB.ProcessParameters` 의 `Flags`, `LoaderThreads`(+0x40c) 등을 읽는다 (flow.md 2.4·2.5).

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, call·ret 둘 다). 태그 `[정적]` / `[추측]`. 라벨은 ret.
- 질문 4 의 "커널 쓰는 줄 ↔ ntdll 읽는 줄" 표.
- flow.md 2.1 `[추측]` 두 줄과 4장 2.1·2.4 두 줄 판정.
- 그림 1 에 넣을 `(정적)` 상자 후보 0 ~ 2개 (대표 지점만 — 필요 없으면 0).
- 동적으로 확인할 것 + 멈춤점 (`nt+rva`, 짧게). 이미 대기 중인 Q35 (`PspAllocateProcess` 실행 길, 새 PEB `+0x10`·`+0x20` 값) 와 겹치면 그 번호를.
- 스크립트 문제는 "스크립트 문제" 절에.

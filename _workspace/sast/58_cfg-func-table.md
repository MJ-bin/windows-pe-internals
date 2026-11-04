# 58 cfg-func-table — 정적 분석 요청

- 날짜: 2026-11-04
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run63
- 대기열: **Q120a** (PE_emulator 요청 11 의 질문 1 ~ 6. 7번 묶음은 Q120b — 이번에 안 함)
- 관련: run35 (`LdrpInitializeNtdllDataTableEntry`, sast 결과 `+0x145 LdrpCfgProcessLoadConfig`), run51 (sast/45 — `RtlpInsertOrRemoveScpCfgFunctionTable +0xc4 → RtlAddGrowableFunctionTable +0x14f NtSetInformationProcess(0x35)` → nt `RtlInsertDynamicUserFunctionTable +0x20c` → `EPROCESS.InvertedFunctionTable`, 탄 여부 `[공백]` F7), run60 (sast/55 — `RtlpxLookupFunctionTable`, `LdrProtectMrdata`, `LdrEnsureMrdataHeapExists`, `LdrpMrdataHeap` 끝까지 0), baggage 3.11
- 원문 (읽기만): `../PE_emulator/_workspace/orchestrator/64_ntdll-entry.md`, `../PE_emulator/_workspace/02_reviewer/64_ntdll-entry.md`, `../PE_emulator/_workspace/00_planner/65_ntdll-init-order.md` (R1 ~ R3)

## 무슨 일이 있었나

- PE_emulator 가 진짜 `LdrpInitializeNtdllDataTableEntry` (`LdrpInitializeProcess +0x1183 ~ +0x11a7`, call `+0x119d`) 를 Unicorn 안에서 돌렸다. 덫 안 syscall 은 **`NtQueryVirtualMemory(-1, 0x180000000, 0xe, &out, 0x18, &ret)` 하나**뿐이었다.
  - 우리 nt 엔 클래스 `0xe` 몸통이 없어 "성공, 출력 칸 안 씀" 을 줬다. 그래서 게스트가 `RtlAddGrowableFunctionTable` → `NtSetInformationProcess(-1, 0x35)` 까지 안 갔다. `RtlpDynamicFunctionTable` 은 빈 목록이다.
  - `LdrpCfgProcessLoadConfig` 의 `NtProtectVirtualMemory` (지도 3자리) 도 안 나왔다. PE_emulator 가 잰 ntdll 노드 `Flags` = `0xa2c4`.
- PE_emulator 는 클래스 `0xe` 출력 모양·값을 지어낼 수 없어 이 길을 "정정 대기" 로 뒀다. **이번 바퀴의 핵심은 질문 1·2** (출력 모양과 ntdll 에 줄 값, 그 값으로 갈래가 어떻게 갈리나).

## 알고 싶은 것

1. **`RtlpInsertOrRemoveScpCfgFunctionTable` (ntdll rva `0xa7830`, 220B) 본문**
   - 인자 꼴 (넣기/빼기 고르는 인자, 모듈 노드?)
   - `+0x58/+0x5d NtQueryVirtualMemory` 인자 다섯·여섯 (핸들, 주소 = 이미지 시작?, 클래스 `0xe`, 출력 버퍼 크기 `0x18`, 반환 길이)
   - 출력의 **어느 칸을 어떻게 읽어** `+0xc4/+0xc9 RtlAddGrowableFunctionTable` 갈래, `+0xd4/+0xd9 RtlDeleteFunctionTable` 갈래, 아무것도 안 하는 갈래를 고르나 (조건 줄 + 점프 목적지)
   - `RtlAddGrowableFunctionTable` 에 넘기는 인자 넷·여섯 (함수 표 주소, 개수, 최대 개수, 범위 시작·끝 — 값이 클래스 `0xe` 출력 칸에서 오나)
   - 상태값 실패 (`NtQueryVirtualMemory` 가 실패를 돌려줄 때) 갈래
2. **ntoskrnl `NtQueryVirtualMemory` 클래스 `0xe` 길**
   - `NtQueryVirtualMemory` (rva `0x89c8d0`, 42B) → `MmQueryVirtualMemory` (rva `0x89c910`, 4021B) 에서 클래스 `0xe` 갈래만 → `MiQueryImageExtensionInformation` (rva `0x48062c`, 541B) 인가 (다른 함수면 그 이름)
   - 클래스 `0xe` 이름 (`MEMORY_INFORMATION_CLASS` 의 PDB 열거 이름이 있으면), 들어오는 크기 검사 (`0x18` 이 아니면?)
   - 출력 `0x18`B 칸 표: 오프셋 · 크기 · 칸 이름 (PDB 타입이 있으면) · 뜻 · 값의 출처 (이미지 섹션/제어 영역의 어느 칸, 이미지 헤더의 어느 디렉터리)
   - **test.exe 프로세스에서 ntdll 이미지 (`0x180000000` = 파일 기준 시작 주소, 실제는 매핑 주소) 에 줄 값**: 정적으로 정해지면 값 (ntdll 파일 헤더에서 계산 — 예: 어떤 디렉터리가 있으면 그 RVA·크기), 실행 때만 정해지면 꼴만 + "동적 필요"
   - 이미지가 아닌 주소·디렉터리가 없을 때 돌려주는 상태값
3. **`LdrpCfgProcessLoadConfig` (ntdll rva `0xa4a70`, 1813B) 본문 — ntdll 노드로 불릴 때 타는 갈래 중심**
   - 인자 꼴, 처음 갈림 조건 (`LdrControlFlowGuardEnforced` `+0x187` 결과, 로드 구성 디렉터리 유무, `GuardFlags`)
   - syscall: `NtProtectVirtualMemory` `+0x16a/+0x16f` · `+0x4be/+0x4c3` · `+0x535/+0x53a` 세 자리 각각의 조건과 인자 (주소, 크기, 새 보호). ntdll 노드일 때 타나 (정적으로 정해지는 만큼)
   - **ntdll 노드 `Flags` (`_LDR_DATA_TABLE_ENTRY.Flags`) 의 `0x8000` 비트를 켜는 곳** — 이 함수 안인가, 아니면 어디 (함수+오프셋). 비트 이름 (PDB 비트필드 이름이 있으면). PE_emulator 실측 `0xa2c4` 의 비트들 중 이 함수가 켜는 것
   - 쓰는 전역 (`LdrpGuard*` 등 CFG 도우미 칸 — run45 의 `LdrpGuardCheckIcallNoESFptr`·`LdrpGuardDispatchIcallNoESFptr` 와 이어지면 같이)
   - `+0x70c/+0x711` 간접 호출: 대상 후보 (정적으로 좁혀지면)
   - 1813B 로 크다 — ntdll 노드 갈래만 따라가고, 나머지 갈래는 조건·이름만
4. **`RtlAddGrowableFunctionTable` (ntdll rva `0xa72e0`, 873B) 본문**
   - 인자 꼴과 반환 (표 핸들을 어디에 돌려주나)
   - 처음 갈림: `+0x40 LdrEnsureMrdataHeapExists` 를 부르는 조건 (CFG 켜짐? `LdrControlFlowGuardEnforced` `+0x4e`)
   - `+0x117 NtQuerySystemTime` · `+0x14f/+0x154 NtSetInformationProcess(-1, 0x35, …)` 앞뒤 — 인자 (16B 버퍼 내용) 과 조건 (이 syscall 을 늘 내나, 조건부인가)
   - 쓰는 전역: `RtlpDynamicFunctionTable` (목록 머리), 트리 (`RtlAvlInsertNodeEx` `+0x1a9`·`+0x1eb` 의 트리 머리 전역 이름), 락, `LdrpMrdataHeap` 쪽 (할당 `+0xfb RtlAllocateHeap` 의 힙이 무엇인가), 새 항목 (`_DYNAMIC_FUNCTION_TABLE` 이면) 의 칸 표
   - 하위 `LdrEnsureMrdataHeapExists` (rva 는 `_index.tsv`) — 줄기 끝까지: syscall (`+0x61 NtAllocateVirtualMemory`, `+0x14c NtFreeVirtualMemory`), `RtlpCreateHeap` 인자 (힙 플래그·크기), 쓰는 전역 (`LdrpMrdataHeap` `+0xef`·`LdrpMrdataHeapUnprotected` `+0xe8` — run60). `RtlpCreateHeap` 본문은 이름만 (run46 몫)
   - **ntdll 노드 길에서 이 함수까지 오면 syscall 이 몇 번·어떤 순서로 나오나** (표)
5. **`RtlInitializeHistoryTable` (rva `0xf20bc`, 185B, 호출 `LdrpInitializeProcess +0x1277/+0x127c`) · `RtlpFunctionAddressTableEntry` (rva `0xf217c`, 86B) · `RtlLookupFunctionEntry` (rva `0x13010`, 863B, `.asm` 있음) · `RtlpLookupDynamicFunctionEntry` (rva `0xe3680`, 660B) 본문**
   - 함수마다: 인자, 쓰는 전역 (이름·크기), 하위 호출, syscall
   - `RtlInitializeHistoryTable`: `+0x17 LdrProtectMrdata` … `+0xb4` 꼬리 `LdrProtectMrdata` 사이에 무엇을 채우나 (히스토리 표 전역 이름·칸), `+0x50/+0x55 RtlLookupFunctionEntry` 를 몇 번·무엇으로 부르나
   - `RtlLookupFunctionEntry` → `+0x48/+0x4d RtlpxLookupFunctionTable` → (`LdrInitState` 0 이면) 클래스 6 `NtQueryVirtualMemory` (run60) — `LdrpInitializeProcess +0x1277` 시점의 `LdrInitState` 값으로 어느 길인가 (정적으로 정해지면)
   - `+0x352/+0x357 RtlpLookupDynamicFunctionEntry` 로 가는 조건 (함수 표에서 못 찾았을 때?), 그 안 `+0x133` 간접 호출 (콜백) 과 `RtlpDynamicFunctionTable` 을 어떻게 걷나 — 질문 4 의 목록이 비어 있으면 무엇을 돌려주나
6. **`RtlpxLookupFunctionTable +0x2d5 ~ +0x303`** (`.asm` 있음, run60 `+0x2d0/+0x2d5 NtQueryVirtualMemory`) — 클래스 6 출력의 **어느 칸 (오프셋) 을 읽어** 무엇을 정하나. 클래스 6 이름·출력 크기, 그 칸 뜻 (커널 쪽 클래스 6 갈래 함수 이름만 — 본문은 필요하면 질문 2 와 같은 꼴로 짧게)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^RtlpInsertOrRemoveScpCfgFunctionTable$` | 1 | 1 |
| ntoskrnl | `^NtQueryVirtualMemory$`, `^MmQueryVirtualMemory$`, `^MiQueryImageExtensionInformation$` | 0 | 2 (`MmQueryVirtualMemory` 는 클래스 `0xe`·6 갈래만 읽음) |
| ntdll | `^LdrpCfgProcessLoadConfig$` | 1 | 3 |
| ntdll | `^RtlAddGrowableFunctionTable$`, `^LdrEnsureMrdataHeapExists$` | 1 | 4 |
| ntdll | `^RtlInitializeHistoryTable$`, `^RtlpFunctionAddressTableEntry$`, `^RtlpLookupDynamicFunctionEntry$` | 1 | 5 |
| ntdll | `^RtlLookupFunctionEntry$`, `^RtlpxLookupFunctionTable$`, `^LdrpProcessMappedModule$`, `^LdrpInitializeNtdllDataTableEntry$` | 0 | 5·6, 길 확인 (`.asm` 이미 있음 — 다시 뽑지 않아도 됨) |
| ntdll | (refs) `RtlpDynamicFunctionTable` 쪽 전역 (질문 4 에서 이름이 나오면) | — | 4·5 쓰는 곳 |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where ntdll.dll <이름>` (ntoskrnl 은 `ntoskrnl.exe`).
- 공용 부품 (`RtlAcquireSRWLock*`, `RtlAllocateHeap`, `RtlFreeHeap`, `RtlProtectHeap`, `RtlAvlInsertNodeEx`, `memset`, `qsort`, `__security_check_cookie`) 은 이름만.
- `LdrControlFlowGuardEnforced` 본문은 Q120b·Q80 몫 — 이번엔 **반환값이 무엇으로 정해지나 한 줄**만 (읽는 전역 이름).

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- 길: `LdrpInitializeProcess +0x119d/+0x11a2` → `LdrpInitializeNtdllDataTableEntry +0x11f/+0x124` → `LdrpProcessMappedModule` → `+0x145/+0x14a LdrpCfgProcessLoadConfig` · `+0x19e/+0x1a3 RtlpInsertOrRemoveScpCfgFunctionTable`
  - `LdrpProcessMappedModule` 의 다른 caller: `LdrpInitializeProcess +0x14e9`·`+0x1679`, `LdrpMapDllWithSectionHandle +0x267`. `LdrpInitializeNtdllDataTableEntry` 는 `+0x126a` 에서도 불림
  - `LdrpInitializeNtdllDataTableEntry` 안에 `+0x1df/+0x1e4 NtQueryVirtualMemory` 도 있음 — PE_emulator 가 본 한 번이 이 자리가 아니라 `RtlpInsertOrRemoveScpCfgFunctionTable +0x58` 인지 돌아올 자리로 확인해 줄 것 (PE_emulator 원문에 돌아올 자리가 있으면 그것과 대조)
- 깊이·caller: `RtlpInsertOrRemoveScpCfgFunctionTable` 6/2 · `LdrpCfgProcessLoadConfig` 6/2 (다른 caller `AvrfMiniLoadDll +0x869`) · `RtlAddGrowableFunctionTable` 7/1 · `RtlInitializeHistoryTable` 5/1 · `RtlpFunctionAddressTableEntry` 6/1 · `RtlLookupFunctionEntry` 3/8 · `RtlpLookupDynamicFunctionEntry` 4/1
- `RtlpInsertOrRemoveScpCfgFunctionTable` callee: `+0x58 NtQueryVirtualMemory` · `+0x71 __security_check_cookie` · `+0xc4 RtlAddGrowableFunctionTable` · `+0xd4 RtlDeleteFunctionTable`
- `LdrpCfgProcessLoadConfig` callee: `+0x61 memset` · `+0xec RtlImageDirectoryEntryToData` · `+0x144 LdrpMakePermanentImageCommit` · `+0x16a NtProtectVirtualMemory` · `+0x187 LdrControlFlowGuardEnforced` · `+0x39b qsort` · `+0x4be NtProtectVirtualMemory` · `+0x4ee LdrpCfgCheckRoutineCallback` · `+0x535 NtProtectVirtualMemory` · `+0x57a LdrpCfgDispatchRoutineCallback` · `+0x67e LdrpIsModuleUnderSystem32` · `+0x70c` 간접(CFG)
- `RtlAddGrowableFunctionTable` callee: `+0x40 LdrEnsureMrdataHeapExists` · `+0x4e`·`+0xdf`·`+0x230`·`+0x24e`·`+0x2a7`·`+0x2d6 LdrControlFlowGuardEnforced` · `+0xab`·`+0x313`·`+0x349 RtlProtectHeap` · `+0xfb RtlAllocateHeap` · `+0x117 NtQuerySystemTime` · `+0x14f NtSetInformationProcess` · `+0x160`·`+0x29f LdrProtectMrdata` · `+0x1a9`·`+0x1eb RtlAvlInsertNodeEx` · `+0x249 RtlFreeHeap` · 락 여럿
- `LdrEnsureMrdataHeapExists` callee: `+0x61 NtAllocateVirtualMemory` · `+0x8c RtlpCreateHeap` · `+0xa5 RtlAllocateHeap` · `+0xbd`·`+0x11e RtlProtectHeap` · `+0xc4`·`+0x100`·`+0x114 LdrProtectMrdata` · `+0x133 RtlDestroyHeap` · `+0x14c NtFreeVirtualMemory`
- `RtlInitializeHistoryTable` callee: `+0x17 LdrProtectMrdata` · `+0x29 RtlpFunctionAddressTableEntry` · `+0x50 RtlLookupFunctionEntry` · `+0xb4` 꼬리 `LdrProtectMrdata`
- `RtlLookupFunctionEntry` callee: `+0x48 RtlpxLookupFunctionTable` · `+0x352 RtlpLookupDynamicFunctionEntry`. `RtlpLookupDynamicFunctionEntry`: 공유 락 · `+0x133` 간접(CFG)
- ntoskrnl `_index.tsv`: `NtQueryVirtualMemory` `0x89c8d0` 42B · `MmQueryVirtualMemory` `0x89c910` 4021B · `MiQueryImageExtensionInformation` `0x48062c` 541B (이 함수가 클래스 `0xe` 몫인지는 짐작 — 확인할 것). `.asm` 은 셋 다 아직 없음

## 이미 아는 것

- `[정적]` run51: `RtlAddGrowableFunctionTable +0x14f` 에서 `EDX = 0x35` 로 `NtSetInformationProcess` → nt `RtlInsertDynamicUserFunctionTable +0x20c` 가 `EPROCESS.InvertedFunctionTable` 을 만듦. test.exe 에서 탔는지 `[공백]` (F7)
- `[관찰]` run44 사진: `EPROCESS.InvertedFunctionTable` 이 P1 ~ P1a 사이에 값이 생김 (baggage 3.11). `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` 는 끝까지 0 (run60)
- `[정적]` run60: `RtlpxLookupFunctionTable` 은 `LdrInitState == 3` 이면 빠른 길, 아니면 `+0x2d0/+0x2d5` 클래스 6 `NtQueryVirtualMemory`
- `[관찰]` PE_emulator (진짜 ntdll 코드를 Unicorn 에서 실행 — 26100.9457 VM 관찰 아님): 덫 안 syscall `NtQueryVirtualMemory(-1, 0x180000000, 0xe, &out, 0x18, &ret)` 하나, ntdll 노드 `Flags 0xa2c4`
- `[추측]` 사진의 `InvertedFunctionTable` 은 있는데 `LdrpMrdataHeap` 은 0 → `RtlAddGrowableFunctionTable` 이 `LdrEnsureMrdataHeapExists` 를 안 타는 갈래로 갔거나, `InvertedFunctionTable` 을 다른 길이 썼다. 이번 질문 4 로 가를 수 있으면 갈라 줄 것

## 이번에 안 하는 것

- Q120b 묶음 (TLS, `LdrpAllocateSchedulerSharedData`, `RtlInitializeProcessorFeaturesBitMap`, KnownDlls 등)
- `LdrControlFlowGuardEnforced` 본문 (반환값 출처 한 줄만), `RtlpCreateHeap`·`RtlDeleteFunctionTable` 본문 (이름만)
- 커널 `RtlInsertDynamicUserFunctionTable` (run51 에서 봄)
- 예외 처리 길 (`RtlUnwindEx` 등) 의 `RtlLookupFunctionEntry` caller 들 — 이름만

## 받고 싶은 것

- 질문별 답 (예/아니오 또는 이름) + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다 (`+0x58/+0x5d`), 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋 (`reference.md` "오프셋 적는 법")
- **syscall 표**: 이름 · 클래스 · 부르는 자리 (함수+call/ret) · 조건 · ntdll 노드 길에서 횟수
- **클래스 `0xe` 출력 `0x18`B 칸 표** (PE_emulator 가 nt 몸통을 지을 때 그대로 씀) + ntdll 이미지에 줄 값 (정적으로 정해지는 것만, 나머지는 꼴)
- PE_emulator 가 그대로 쓸 **작은 표**: 함수 × (인자 · 쓰는 칸 · 하위 호출 · syscall · 반환)
- 질문마다 **"정적으로 닫힘 / 동적 필요"** 를 갈라 적기. 동적 몫은 Q104 VM 단계 (F7 = Q79 출처 포함) 에 얹을 수 있게 멈춤점·읽을 전역으로
- 짐 지도 반영거리: baggage 3.11 `InvertedFunctionTable`, 3.9 `.mrdata`·`LdrpMrdataHeap`, `RtlpDynamicFunctionTable` 쪽 전역

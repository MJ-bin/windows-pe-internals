# 2026-11-04 run63 — ntdll 항목 마무리 안의 CFG·ScpCfg 함수 표 (Q120a, PE_emulator 요청 11 의 1 ~ 6): `RtlpInsertOrRemoveScpCfgFunctionTable` 의 `NtQueryVirtualMemory` 클래스 `0xe` 는 **이미지 뒤 "확장 쪽" 의 RVA·크기** 를 돌려준다. 진짜 VM 은 **확장 있음 갈래**였다 (ntdll 노드 `Flags 0x20a2c4` 의 비트 21 `[관찰]`) → `RtlAddGrowableFunctionTable` → `NtSetInformationProcess(0x35)` 까지 갔다 = F7 거의 닫힘. ntdll 에 줄 값 `+0x8 = 0x267000`, `+0x10 = 0x1000` (정적). `LdrEnsureMrdataHeapExists` 는 **늘 불리고 안에서 바로 0** (test.exe CFG 꺼짐) — run60 `[추측]` 확정. `Flags` 비트 `0x8000` (`ProtectDelayLoad`) = `LdrpCfgProcessLoadConfig +0xc5`

## PE_emulator 쪽에서 쓸 것

**결론 넷.**

- **클래스 `0xe` 의 몸통 = 커널 `MiQueryImageExtensionInformation`.** 출력 `0x18`B 는 입력 겸용이다. ntdll 이미지에 줄 값 (정적): 상태 `0`, 돌아온 길이 `0x18`, `+0x0` 0 (입력 그대로), `+0x4` 0, **`+0x8 = 0x267000`**, **`+0x10 = 0x1000`**. 칸 표는 아래 1절.
- **"출력 칸 안 씀" (지금 PE_emulator) 은 다른 갈래다.** `+0x10 = 0` 이면 ntdll 이 `0x117` 로 끝내고, caller 가 `Flags` 비트 21 을 안 켠다 → `0xa2c4`. 진짜 VM 은 `0x20a2c4` (`04_baggage-read_P1a.txt:2795`·`:2817`) `[관찰]`.
- **확장 쪽 메모리 (`ntdll + 0x267000`, `0x1000`B) 안을 ntdll 이 읽는다**: `+0x14` 의 dword (= `0x2a4`) 가 0 이면 역시 `0x117`. 내용 = ntdll 구역 `SCPCFGNP` (`0x16e000`, `0x2b0`B) 를 복사 + 나머지 `0xcc` (test.exe 는 "Nop" 형 `[정적 추정]`, 결과서 2-5). 함수 표 = `확장 + 0x2a4`, 개수 1, 범위 `[확장, 확장 + 0x1000)`.
- **syscall 약속 (ntdll 노드 길, `LdrpInitializeProcess +0x119d` ~ `+0x127c`) = 6번** (2절 표). `LdrpCfgProcessLoadConfig` 의 `NtProtectVirtualMemory` 세 자리 (지도 "3자리") 는 **ntdll 노드에서 하나도 안 난다**. 대신 `.mrdata` 열고 닫기가 넷 (Add 둘 + History 둘).

오프셋 읽는 법 (run60 ~ run62 와 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값.

### 1. 클래스 `0xe` 출력 칸 표 `[정적]` (결과서 2-3 그대로)

| 오프셋 | 크기 | 칸 이름 | 뜻 | 값의 출처 |
|---|---|---|---|---|
| `+0x00` | 4 | `ExtensionType` `[추측: 외부 이름]` — PDB 열거 `_MI_IMAGE_VIEW_EXTENSION_TYPE` | 입력. ntdll 은 0 (= `MiImageViewExtensionScpCfg`) | 입력을 그대로 되돌려 씀 |
| `+0x04` | 4 | `Flags` `[추측: 이름]` | 예약. **0 이어야** 함 (아니면 `0xc000000d`) | 같이 되돌려 씀 |
| `+0x08` | 8 | `ExtensionImageBaseRva` `[추측: 이름]` | 확장 시작 RVA. 없으면 0 | VAD 시작 + `Segment.TotalNumberOfPtes << 12` + 표 `DAT[종류]` − VAD 시작 (`MiGetImageExtensionBaseAddress`, `MiQueryImageExtensionInformation +0x1a1 ~ +0x1a8`). `DAT[0]` 은 늘 0 |
| `+0x10` | 8 | `ExtensionSize` `[추측: 이름]` | 확장 크기. 없으면 0 | 상수 **`0x1000`** (`+0x1ad`) |

- 들어오는 검사: 길이 < `0x18` → `0xc0000004` (`MmQueryVirtualMemory +0x1c8`·`+0x1cc`), 주소 0 또는 `0x7ffffffeffff` 초과 → `0xc000000d`, `+0x0` 이 1 → `0xc00000bb`, 2 이상 → `0xc000000d`, 돌아올 길이 포인터 NULL 허용 (`MiQueryImageExtensionInformation +0x1ef`).
- 돌려주는 갈래 (`MiQueryImageExtensionInformation`):

| 조건 (줄) | 결과 |
|---|---|
| 주소를 덮는 VAD 가 없음 (`+0x91/+0x96 MiObtainReferencedVadEx` → `+0x9c JZ`) | `0xc00000a0` (끝나는 중이면 `0xc000010a`) |
| 이미지 VAD 가 아님 (`+0xa2 ~ +0xab`) | `0xc0000141` |
| VAD `+0x40` 비트 5 (`_MMVAD_FLAGS2.ImageMappingExtended`) 꺼짐 (`+0xdd ~ +0xe2 JZ +0x1c9`) | **성공 0, `+0x8 = +0x10 = 0`** |
| 확장 쪽 PTE 가 0 (`+0x18a`·`+0x190 JZ +0x1b6`) | **성공 0, 둘 다 0** |
| 모두 통과 | **성공 0, `+0x8` = 확장 RVA, `+0x10 = 0x1000`** |

- ntdll 쪽 갈래 (`RtlpInsertOrRemoveScpCfgFunctionTable`): `0xc00000bb` → `0x117` · 그 밖 음수 → 그대로 반환 (모듈 적재 실패) · `+0x10 == 0` → `0x117` · `[확장 + 0x14] == 0` → `0x117` · 나머지 → 넣기 (`RtlAddGrowableFunctionTable`).
  - caller `LdrpProcessMappedModule` 은 `0x117` 을 성공으로 넘기되 비트 21 만 안 켠다 (`+0x1a7 JNZ +0x203` → `+0x20c JNS +0x1ad`). `0xc0000141` 은 `+0x26e ~ +0x28b` 에서 `LdrpIsImageAddress` 로 다시 본다.

### 2. syscall 표 — ntdll 노드 길 `[정적]` (결과서 "syscall 표", 사슬은 call/ret, 하나 고침)

test.exe 의 ntdll 노드, **확장 있음 (진짜 VM 갈래)**.

| # | 함수 / 사슬 | NtXxx | 횟수 | 조건 | test.exe |
|---|---|---|---|---|---|
| 1 | `LdrpInitializeProcess +0x119d/+0x11a2` → `LdrpInitializeNtdllDataTableEntry +0x11f/+0x124` → `LdrpProcessMappedModule +0x19e/+0x1a3` → `RtlpInsertOrRemoveScpCfgFunctionTable +0x58/+0x5d` | `NtQueryVirtualMemory` (클래스 `0xe`, 길이 `0x18`) | 1 | 노드 비트 21 꺼짐 · 인자 비트 23 꺼짐 (`LdrpProcessMappedModule +0x185 ~ +0x196`) | 탄다 |
| 2 | 〃 `+0xc4/+0xc9` → `RtlAddGrowableFunctionTable +0x14f/+0x154` | `NtSetInformationProcess` (클래스 `0x35`, 길이 `0x10`, `{새 항목 주소, 0}`) | 1 | #1 이 `+0x10 ≠ 0`·`[확장+0x14] ≠ 0` · 개수 ≤ 최대 · 범위 시작 < 끝 · 항목 할당 성공 | 탄다 (비트 21 `[관찰]`) |
| 3 | 〃 `RtlAddGrowableFunctionTable +0x160/+0x165` → `LdrProtectMrdata(0)` → `LdrpChangeMrdataProtection(4)` | `NtProtectVirtualMemory` (`.mrdata`, `PAGE_READWRITE`) | 1 | #2 성공 · 카운터 `LdrpMrdataUnprotected` 0 → 1 | 탄다 |
| 4 | 〃 `+0x29f/+0x2a4` → `LdrProtectMrdata(1)` → `LdrpChangeMrdataProtection(2)` | `NtProtectVirtualMemory` (`PAGE_READONLY`) | 1 | 카운터 1 → 0 | 탄다 |
| 5 | `LdrpInitializeProcess +0x1277/+0x127c` → `RtlInitializeHistoryTable +0x17/+0x1c` → `LdrProtectMrdata(0)` | `NtProtectVirtualMemory` (4) | 1 | 카운터 0 → 1 | 탄다 |
| 6 | 〃 `RtlInitializeHistoryTable +0xb4` (꼬리 `JMP`, 돌아올 자리 = `LdrpInitializeProcess +0x127c`) → `LdrProtectMrdata(1)` | `NtProtectVirtualMemory` (2) | 1 | 1 → 0 | 탄다 |

- 순서: #1 → #2 → #3 → #4 → (`+0x11a2 ~ +0x1277` 는 이 요청 범위 밖) → #5 → #6.
- **안 나오는 것** (test.exe): `LdrpCfgProcessLoadConfig` 의 `NtProtectVirtualMemory` 셋 (`+0x16a`·`+0x4be`·`+0x535`) · `LdrEnsureMrdataHeapExists` 의 `NtAllocateVirtualMemory`·`NtFreeVirtualMemory` · `NtQuerySystemTime` (실제로는 `RtlQuerySystemTime` = KUSD `0x7ffe0014` 읽기) · `RtlInitializeHistoryTable` 여섯 조회의 클래스 6 (`RtlLookupFunctionEntry` 의 칸 0 빠른 길).
- **확장 없음 (PE_emulator 지금 대답)**: #1 하나만 나고 `0x117` 로 끝 — 요청서가 본 "덫 안 syscall 하나" 와 같은 갈래. 비트 21 안 켜짐, `RtlpDynamicFunctionTable` 빈 목록, `EPROCESS.InvertedFunctionTable` 도 이 길로는 안 생김.
- **PE_emulator R4 (`LdrpMrdataUnprotected` 를 1 로 둠, 진짜 0)**: 진짜는 #3 ~ #6 이 각각 syscall. 1 이면 #3·#5 는 2 로 오르기만 해서 syscall 이 안 난다 (run60 카운터 규칙).

### 3. 함수마다 "정적으로 닫힘 / 동적 필요" (결과서 표 요약)

| 질문 | 함수 | 닫힘 | 남은 것 (동적) |
|---|---|---|---|
| 1 | `RtlpInsertOrRemoveScpCfgFunctionTable` (`0xa7830`, 220B) | 인자·갈래·여섯 인자 출처·실패 갈래·`0x117` | 실제 버퍼 값 (`+0x5d`) |
| 2 | 커널 `NtQueryVirtualMemory` → `MmQueryVirtualMemory` → `MiQueryImageExtensionInformation` (`0x48062c`, 541B) | 길·검사·칸 표·값 식·쪽 내용·형 0 | 값 직접, 쪽 형 (`+0x40` 바이트), `DAT[3]`, ntdll 이 왜 `ImageExpansionCompatible` 인가 |
| 3 | `LdrpCfgProcessLoadConfig` (`0xa4a70`, 1813B) | 갈래·`0x8000` 줄·NtProtect 세 자리 (ntdll 안 탐) | `LdrSystemDllInitBlock+0xb8 = 0` 직접 (사진으로는 0) |
| 4 | `RtlAddGrowableFunctionTable` (`0xa72e0`, 873B) · `LdrEnsureMrdataHeapExists` (`0xa716c`, 360B) | `Ensure` 늘 부름·항목 칸·목록·나무·`0x35` 버퍼·syscall 순서 | 목록·나무에 실제 항목 셋 (ntdll·kernel32·kernelbase) 인지 |
| 5 | `RtlInitializeHistoryTable` (`0xf20bc`) · `RtlpFunctionAddressTableEntry` (`0xf217c`) · `RtlLookupFunctionEntry` (`0x13010`) · `RtlpLookupDynamicFunctionEntry` (`0xe3680`) | 6개 함수·빠른 길·`LdrInitState` 0·동적 조회 조건·빈 나무 NULL | `RtlpUnwindHistoryTable` 최종 값, 칸 0 값 |
| 6 | `RtlpxLookupFunctionTable +0x2d5 ~ +0x303` (클래스 6) | 읽는 칸·커널 채우기 | 실제 값 |

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음). 값 대조는 run44 덤프 읽기 로그 `ms/logs/04_baggage-read_P1a.txt` (59 · 1551 · 2690 · 2712 · 2795 · 2817 · 2846줄) 를 결과서가 다시 읽은 것이고, flow-writer 가 그 일곱 줄을 한 번 더 읽어 맞췄다. 날짜는 결과서 날짜.
- 03_sprint 스물한째 바퀴. plan **Q120a** — PE_emulator 요청 11 의 질문 1 ~ 6 (7번 묶음은 Q120b, 이번에 안 함). **짐 지도 바퀴** — `baggage.md` 에 새 쓴 자리 (아래 "짐 지도 반영").
- 정적: ntdll (`ntdll.pdb`) `C3093720177DA6851519DCFFE8DF53FE1` — VM 사진 59줄 `lm` 경로와 같다. ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` — flow.md 기준과 같다 (결과서).
- 버전 대조 (결과서, `_calls.tsv`): `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeNode +0x16b/+0x19c`, `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` — 일치. 그리고 ntdll 노드 `Flags` 사진값 `0x20a2c4` 의 비트 하나하나가 이번 정적 줄과 맞는다 (아래 "확인된 것").

## 목적

- PE_emulator 가 진짜 `LdrpInitializeNtdllDataTableEntry` 를 Unicorn 안에서 돌렸더니 덫 안 syscall 이 `NtQueryVirtualMemory(-1, 0x180000000, 0xe, &out, 0x18, &ret)` 하나뿐이었다. nt 쪽에 클래스 `0xe` 몸통이 없어 "성공, 출력 칸 안 씀" 을 줬고, 그래서 `RtlAddGrowableFunctionTable` → `NtSetInformationProcess(0x35)` 까지 안 갔다. ntdll 노드 `Flags` = `0xa2c4`.
- 알고 싶은 것 (요청서 1 ~ 6): 클래스 `0xe` 출력 모양과 ntdll 에 줄 값 (핵심), 그 값으로 갈래가 어떻게 갈리나, `LdrpCfgProcessLoadConfig` 의 ntdll 갈래와 `Flags` `0x8000`, `RtlAddGrowableFunctionTable`·`LdrEnsureMrdataHeapExists` 줄기, 히스토리 표·함수 항목 조회 넷, 클래스 6 의 칸.
- 짐 지도: baggage 3.11 `InvertedFunctionTable` (F7), 3.9 `.mrdata`·`LdrpMrdataHeap`, `RtlpDynamicFunctionTable` 쪽 전역.
- 범위 밖 (요청서): Q120b 묶음, `LdrControlFlowGuardEnforced` 본문 (반환값 출처 한 줄만), `RtlpCreateHeap`·`RtlDeleteFunctionTable` 본문, 커널 `RtlInsertDynamicUserFunctionTable` (run51), 예외 처리 길의 `RtlLookupFunctionEntry` caller.

## 방법

- 정적: `_workspace/sast/58_cfg-func-table.md` — ntdll 대상 10개 (`RtlpInsertOrRemoveScpCfgFunctionTable`·`LdrpCfgProcessLoadConfig`·`RtlAddGrowableFunctionTable`·`LdrEnsureMrdataHeapExists`·`RtlInitializeHistoryTable`·`RtlpFunctionAddressTableEntry`·`RtlpLookupDynamicFunctionEntry`·`LdrControlFlowGuardEnforced`·`LdrProtectMrdata`·`RtlDeleteFunctionTable`) + 길 따라 11개 (`LdrpCfgCheckRoutineCallback`·`LdrpCfgDispatchRoutineCallback`·`LdrpCgRoutineCallback`·`LdrpMakePermanentImageCommit`·`LdrpIsModuleUnderSystem32`·`LdrpChangeMrdataProtection`·`NtQuerySystemTime`·`RtlQuerySystemTime`·`NtSetInformationProcess`·`NtQueryVirtualMemory`·`LdrpUnmapModule`), ntoskrnl 대상 3개 (`NtQueryVirtualMemory`·`MmQueryVirtualMemory`·`MiQueryImageExtensionInformation`) + 값의 출처를 따라 12개 (`MiGetImageExtensionBaseAddress`·`MmInitializeImageViewExtension`·`MmInitializeImageViewExtensionCfg`·`MmGetScpCfgFunctionOffset`·`MiMapImageScpCfgPages`·`MiMapAllImageScpPages`·`MiMapViewOfImageSection`·`PsGetScpCfgPageTypeForProcess`·`PsInitializeScpCfgPages`·`PspFinalizeScpCfgPage`·`MiObtainReferencedVadEx`·`MiDoesImageContainFunctionOverrideFixups`). 새 `_refs` (ntoskrnl) 4개.
  - 이미 analyze 된 프로젝트 — **다시 analyze 안 함**, export 만 depth 0. 이미 있던 `.asm` (`RtlLookupFunctionEntry`·`RtlpxLookupFunctionTable`·`LdrpProcessMappedModule`·`LdrpInitializeNtdllDataTableEntry`·`LdrpInitializeProcess`·`_LdrpInitialize`) 은 읽기만.
  - 파일 값 (ntdll 구역 `SCPCFG*`·내보내기 `RtlpScpCfgNtdllExports`·로드 구성·PE 디렉터리) 은 결과서가 PE 바이트를 직접 읽었다.
- 결과서: `_workspace/sast/58_cfg-func-table_result.md` (**분석성공**, PDB 일치, 1 ~ 6 모두 답).
- 오케스트레이터가 `.asm` 으로 확인한 줄: `RtlpInsertOrRemoveScpCfgFunctionTable +0x58/+0x5d`·`+0x8e JZ +0x64`·`+0x9d JZ +0x64`·`+0xa6 JZ +0xd1`·`+0xc4/+0xc9`, `RtlAddGrowableFunctionTable +0x40/+0x45`·`+0xee JZ +0x20a`·`+0x14f/+0x154`, `LdrpCfgProcessLoadConfig +0xb5`·`+0xbf JZ`·`+0xc5 OR [RSI+0x68],0x8000`, P1a 로그 2795줄.
- 이 문서를 쓰며 flow-writer 가 다시 맞춘 것 (`.asm`): `RtlpInsertOrRemoveScpCfgFunctionTable +0x31`·`+0x43`·`+0x5d ~ +0x64`·`+0x86 ~ +0x9f`·`+0xac`·`+0xcb ~ +0xdb`, `RtlAddGrowableFunctionTable +0x24 ~ +0x55`·`+0xdf ~ +0x117`·`+0x13a ~ +0x16c`·`+0x1a9`·`+0x1eb`·`+0x1f0 ~ +0x201`·`+0x20a ~ +0x217`·`+0x27d ~ +0x2ae`, `LdrEnsureMrdataHeapExists +0xa ~ +0x2d`·`+0x61`·`+0x8c`, `LdrpCfgProcessLoadConfig +0x77 ~ +0xec`·`+0x16a`·`+0x17c ~ +0x18e`·`+0x454`·`+0x67e ~ +0x685`, `LdrpProcessMappedModule +0x13a ~ +0x1ad`·`+0x203`·`+0x20e`, `LdrpUnmapModule +0x1b ~ +0x35`, `RtlInitializeHistoryTable +0x0 ~ +0xb4` 전부, `_LdrpInitialize +0x10d ~ +0x13e`, ntoskrnl `MmQueryVirtualMemory +0x16f ~ +0x18b`·`+0x1c8 ~ +0x1cc`·`+0x274764/+0x274769`, `MiQueryImageExtensionInformation +0x91 ~ +0xab`·`+0xdd ~ +0xe2`·`+0x18a ~ +0x1ad`·`+0x1ef ~ +0x1f4`. 결과서와 다른 곳은 아래 "결과서와 다르게 적은 곳".
- 문서: 이 run 문서, `run60-func-table-mrdata.md` · `run51-eprocess-later.md` (맨 위 정정 한 줄), `flow.md` (기준, 그림 2, 2.4 run51 절·`LdrpInitializeNtdllDataTableEntry` 절·run60 절 + 새 절, 4장, 4.1, 5장), `baggage.md` (아래).

## 결과

비유 (가게 문 열기):

- 관리실 (ntdll) 이 자기 카드를 명부에 꽂고 나서, **본사 (커널) 에 "우리 건물 뒤에 붙은 창고가 있나요?"** 하고 묻는다 (클래스 `0xe`).
- 본사가 "네, 건물 끝 바로 뒤 (`+0x267000`) 에 한 칸 (`0x1000`) 있어요" 하면, 관리실은 그 창고 안 안내판 (`+0x14`) 을 읽어 **비상구 지도 (함수 표)** 를 본사 장부에 등록한다 (`NtSetInformationProcess 0x35`). 그리고 자기 카드에 "창고 지도 등록함" 도장 (비트 21) 을 찍는다.
- PE_emulator 본사는 "창고 있나요?" 에 대답 칸을 비워 줬다. 그러면 관리실은 "창고 없음" 으로 알고 등록도 도장도 건너뛴다. 진짜 VM 카드에는 도장이 찍혀 있었다.

### 정적 — 호출 지점 (`ms/disassemble/{ntdll,ntoskrnl}/*.asm` `CALL … ret=`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `ntdll!LdrpProcessMappedModule` | `+0x145` | `+0x14a` | `ntdll!LdrpCfgProcessLoadConfig` (`R8` = 로드 구성 포인터 `+0x13a`). 성공이면 `+0x154 OR [RBX+0x68],EBP` (`EBP = 0x2000`, `+0xc2`) | [정적] |
| `ntdll!LdrpProcessMappedModule` | `+0x19e` | `+0x1a3` | `ntdll!RtlpInsertOrRemoveScpCfgFunctionTable` (`+0x198 MOV R8B,1` 넣기). 조건 `+0x185 BT EDX,0x15` · `+0x18c BT R13D,0x17` · `+0x196 JZ`. 0 이면 `+0x1a9 BTS [RSI],0x15` | [정적] |
| `ntdll!LdrpUnmapModule` | `+0x30` | `+0x35` | `ntdll!RtlpInsertOrRemoveScpCfgFunctionTable` (`+0x2d XOR R8D,R8D` 빼기, `+0x20 TEST [RBX+0x68],0x200000` · `+0x27 JZ` — 비트 21 켜진 모듈만) | [정적] |
| `ntdll!RtlpInsertOrRemoveScpCfgFunctionTable` | `+0x58` | `+0x5d` | `NtQueryVirtualMemory(-1, 이미지 시작, 0xe, RSP+0x40, 0x18, RSP+0x30)` | [정적] |
| `ntdll!RtlpInsertOrRemoveScpCfgFunctionTable` | `+0xc4` · `+0xd4` | `+0xc9` · `+0xd9` | `RtlAddGrowableFunctionTable` (넣기) · `RtlDeleteFunctionTable` (빼기, 본문 이름만) | [정적] |
| `ntdll!LdrpCfgProcessLoadConfig` | `+0x67e` · `+0xec` · `+0x187` | `+0x683` · `+0xf1` · `+0x18c` | `LdrpIsModuleUnderSystem32` (ntdll 은 거짓 — `DependentLoadFlags 0x800` `[관찰]`) · `RtlImageDirectoryEntryToData(…, 0xd)` (ntdll 은 지연 가져오기 디렉터리 없음) · `LdrControlFlowGuardEnforced` (test.exe 0) | [정적] |
| `ntdll!LdrpCfgProcessLoadConfig` | `+0x16a` · `+0x4be` · `+0x535` | `+0x16f` · `+0x4c3` · `+0x53a` | `NtProtectVirtualMemory` 셋 — **ntdll 노드에선 안 탐** | [정적] |
| `ntdll!RtlAddGrowableFunctionTable` | `+0x40` | `+0x45` | `LdrEnsureMrdataHeapExists` — 인자 검사 (`+0x24 CMP R8D,R9D` · `+0x27 JA +0xd8` · `+0x3a JNC +0xd8`) 뒤 **조건 없이** | [정적] |
| `ntdll!LdrEnsureMrdataHeapExists` | `+0xa` | `+0xf` | `LdrControlFlowGuardEnforced` → `+0x11 JZ +0x1f` → `+0x1f XOR EAX,EAX` (test.exe 는 여기서 끝) | [정적] |
| `ntdll!RtlAddGrowableFunctionTable` | `+0x4e` · `+0xdf` · `+0x2a7` | `+0x53` · `+0xe4` · `+0x2ac` | `LdrControlFlowGuardEnforced` (`+0x55 JZ +0xdf` · `+0xee JZ +0x20a` · `+0x2ae JZ +0xbc`) | [정적] |
| `ntdll!RtlAddGrowableFunctionTable` | `+0xfb` · `+0x117` | `+0x100` · `+0x11c` | `RtlAllocateHeap(PEB.ProcessHeap, 0, 0x88)` (힙 = `+0x20a ~ +0x213`) · `NtQuerySystemTime` (= `RtlQuerySystemTime`, syscall 아님) | [정적] |
| `ntdll!RtlAddGrowableFunctionTable` | `+0x14f` | `+0x154` | `NtSetInformationProcess(-1, 0x35, RSP+0x20, 0x10)` (`+0x146 LEA EDX,[R9+0x25]`) | [정적] |
| `ntdll!RtlAddGrowableFunctionTable` | `+0x160` · `+0x16c` · `+0x1a9` · `+0x1eb` · `+0x295` · `+0x29f` | `+0x165` · `+0x171` · `+0x1ae` · `+0x1f0` · `+0x29a` · `+0x2a4` | `LdrProtectMrdata(0)` · `RtlAcquireSRWLockExclusive(RtlpDynamicFunctionTableLock)` · `RtlAvlInsertNodeEx` ×2 · `RtlReleaseSRWLockExclusive` · `LdrProtectMrdata(1)` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1277` | `+0x127c` | `ntdll!RtlInitializeHistoryTable` (인자 없음) | [정적] |
| `ntdll!RtlInitializeHistoryTable` | `+0x17` · `+0x29` · `+0x50` | `+0x1c` · `+0x2e` · `+0x55` | `LdrProtectMrdata(0)` · `RtlpFunctionAddressTableEntry(i)` (7번) · `RtlLookupFunctionEntry(pc, &시작, NULL)` (6번) | [정적] |
| `ntdll!RtlInitializeHistoryTable` | `+0xb4` (꼬리 `JMP`) | — | `LdrProtectMrdata(1)` | [정적] |
| `ntoskrnl!NtQueryVirtualMemory` | `+0x20` | `+0x25` | `MmQueryVirtualMemory` | [정적] |
| `ntoskrnl!MmQueryVirtualMemory` | `+0x274764` | `+0x274769` | `MiQueryImageExtensionInformation` (`+0x16f CMP R14D,0xe` · `+0x18b JMP RAX` 표 15칸, 몸통은 "떨어진 범위") | [정적] |

### 질문별 답 (결과서 요약)

1. **`RtlpInsertOrRemoveScpCfgFunctionTable`** — 인자 `RCX` = 이미지 시작 (`+0x39`·`+0x3c`), `R8B` = 1 넣기 / 0 빼기 (`+0x31`), 모듈 노드는 안 받는다. 출력 칸을 읽는 줄: `+0x86 MOV R8,[RSP+0x50]` (칸 `+0x10`) · `+0x8e JZ +0x64` → `+0x90 MOV RAX,[RSP+0x48]` (칸 `+0x8`) · `+0x95 ADD RAX,RBX` · `+0x98 MOV ECX,[RAX+0x14]` · `+0x9d JZ +0x64` → `+0x9f LEA RDX,[RAX+RCX]` (함수 표) · `+0xa6 JZ +0xd1` (빼기). `RtlAddGrowableFunctionTable` 여섯 인자 = 핸들 칸 `RSP+0x38` (버려짐) · 함수 표 · 개수 1 · 최대 1 · 범위 시작 `이미지 + 칸+0x8` · 범위 끝 `시작 + 칸+0x10`. 반환 0 / `0x117` / 음수. `[정적]`
   - PE_emulator 가 본 한 번은 이 자리 (`+0x58`, 돌아올 자리 `+0x5d`) 다. `LdrpInitializeNtdllDataTableEntry +0x1df/+0x1e4` 의 `NtQueryVirtualMemory` 는 클래스 2, `R9 == 0` (패치 이름) 일 때만이라 첫 호출에선 안 탄다.
2. **클래스 `0xe` 길** — 위 1절. 클래스 이름은 PDB 에 `MEMORY_INFORMATION_CLASS` 가 없어 못 정한다 (`MemoryImageExtensionInformation` 은 `[추측: 외부 이름]`). **ntdll 에 줄 값**: `+0x8 = 0x267000` = ntdll `SizeOfImage` (쪽 `0x267` = `TotalNumberOfPtes`) + `DAT[0] = 0`, `+0x10 = 0x1000`. 확장이 붙는 조건 = `MiMapViewOfImageSection +0x172 ~ +0x1fa` (부팅 전역 `[0xe2d8ec] ≠ 0` · 이미지 `ImageExpansionCompatible` · 뷰가 세그먼트 전체 · 프로세스 쪽 플래그 둘 등). `[정적]`
3. **`LdrpCfgProcessLoadConfig`** — 인자 `RCX` 노드 · `RDX` NT 헤더 · `R8` 로드 구성 (없으면 0). ntdll 노드 갈래: 로드 구성 있음 (`+0x77`) · 크기 ≥ `0x94` (`+0x80`) → `DependentLoadFlags` 를 마스크로 걸러 노드 `+0x118` 에 (`+0x8c ~ +0xa7`) → `LdrpIsModuleUnderSystem32` 거짓 → **`+0xb5 TEST [RDI+0x90],0x1000` · `+0xbf JZ +0x16f` · `+0xc5 OR [RSI+0x68],0x8000`** (`GuardFlags = 0x10417500`, 비트 `0x1000` 켜짐) → 지연 가져오기 디렉터리 없음 → `+0x187 LdrControlFlowGuardEnforced` 0 → `+0x584` → `qsort` (0개) → `+0x454` 반환 0. **syscall 없음.** `0x8000` 의 PDB 비트 이름 = `ProtectDelayLoad` (비트 15). `+0x70c` 간접 호출 후보 = `LdrpCgRoutineCallback` 하나 `[정적 좁힘]` (CastGuard, test.exe 안 탐). `[정적]`
4. **`RtlAddGrowableFunctionTable`** — `+0x40` `LdrEnsureMrdataHeapExists` 는 **늘 부른다**. test.exe 는 CFG 강제가 아니라 그 안에서 바로 0. 항목 힙 = `PEB.ProcessHeap` (`+0xee JZ +0x20a`). 새 항목 = PDB `_DYNAMIC_FUNCTION_TABLE` `0x88`B (칸은 아래 "짐 지도 반영" 의 7.6.4). `Type` = 3 (`RF_KERNEL_DYNAMIC`, `+0x13a`). `NtSetInformationProcess(0x35)` 의 16B = `{새 항목 주소, 바이트 0}` (`+0x141`·`+0x14a`), **조건 없이 늘** (항목을 얻은 뒤). 성공이면 `.mrdata` 열고 → 락 → 나무 둘 (`RtlpDynamicFunctionTableTreeMin/Max`) → 목록 끝 (`RtlpDynamicFunctionTable`, `+0x1f0 ~ +0x201` 끝 검사 `INT 0x29` 코드 3) → 락 풀고 → 닫고 → 핸들 `*RCX` (`+0x2a4`). 실패면 (`+0x158 JS +0x230`) 항목을 풀고 상태 반환. `[정적]`
5. **히스토리 표·조회** — `RtlInitializeHistoryTable` 은 `RtlpFunctionAddressTableEntry` 의 함수 6개 (`RtlUnwindEx`·`__C_specific_handler`·`RtlpExecuteHandlerForException`·`RtlDispatchException`·`RtlRaiseException`·`KiUserExceptionDispatcher`) 를 `RtlLookupFunctionEntry` 로 찾아 **`RtlpUnwindHistoryTable`** (`.mrdata` `0x1e7350`, `0xd8`B) 에 넣는다 — 개수 6 (`+0x9f`), `GlobalHint` = 4 (`RtlRaiseException`, `+0x3f`), 아래·위 한계, 칸마다 이미지 시작·함수 항목. 여섯 함수가 다 ntdll 안이라 `RtlLookupFunctionEntry` 는 **`LdrpInvertedFunctionTables` 칸 0 빠른 길** (`+0x21`·`+0x37`·`+0x3a JNC +0x1bf`) 을 타고 `RtlpxLookupFunctionTable`·클래스 6 을 안 거친다. `RtlpLookupDynamicFunctionEntry` 는 `RtlpxLookupFunctionTable` 이 못 찾았을 때만 (`+0x53 JZ +0x34c`) — 나무 넷만 걷고 (목록은 안 걷음), 비었으면 NULL. `LdrInitState` 는 `+0x1277` 시점 0. `[정적]`
6. **클래스 6** — `RtlpxLookupFunctionTable +0x2d0` `NtQueryVirtualMemory(-1, pc, 6, RSP+0x30, 0x18, NULL)`. ntdll 이 읽는 칸: `+0x00` 이미지 시작 (0 이거나 `pc < 시작` 이면 못 찾음) · `+0x08` 이미지 크기 (낮은 dword) · `+0x10` 플래그 (비트 0·1 켜지면 못 찾음). 커널 몸통은 `MmQueryVirtualMemory` 안 (`+0x274a66 ~`, 별도 함수 없음). 플래그 비트 6 = `ImageMappingExtended`. 클래스 6 을 타는 곳은 `LdrpInitializeProcess +0x43a` (run60) 뿐 — 이 바퀴 범위에서는 안 탐. `[정적]`

### 호출 관계

```
LdrpInitializeProcess
 ├─ +0x11a2 LdrpInitializeNtdllDataTableEntry                       (run35)
 │        └─ +0x124 LdrpProcessMappedModule [ntdll]
 │                 ├─ +0x14a LdrpCfgProcessLoadConfig               +0xa7 노드 DependentLoadFlags · +0xc5 Flags |= 0x8000 (ProtectDelayLoad)
 │                 │        ├─ +0x683 LdrpIsModuleUnderSystem32     ntdll 거짓
 │                 │        ├─ (+0xf1 RtlImageDirectoryEntryToData — 지연 가져오기 없음 → +0x16f NtProtect 안 탐)
 │                 │        └─ +0x18c LdrControlFlowGuardEnforced   0 → +0x584 → +0x454 반환 0
 │                 │  +0x154 Flags |= 0x2000 (LoadConfigProcessed)
 │                 ├─ +0x1a3 RtlpInsertOrRemoveScpCfgFunctionTable(ntdll, 1)
 │                 │        ├─ +0x5d NtQueryVirtualMemory(-1, ntdll, 0xe, 0x18)   → nt MmQueryVirtualMemory +0x274769 MiQueryImageExtensionInformation
 │                 │        │                                                       = {0, 0, 0x267000, 0x1000} (확장 있음)
 │                 │        │  +0x98 [ntdll+0x267000+0x14] = 0x2a4 ≠ 0
 │                 │        └─ +0xc9 RtlAddGrowableFunctionTable(&h, 확장+0x2a4, 1, 1, 확장, 확장+0x1000)
 │                 │                 ├─ +0x45 LdrEnsureMrdataHeapExists → +0xf LdrControlFlowGuardEnforced 0 → +0x1f 0 반환
 │                 │                 ├─ +0x100 RtlAllocateHeap(PEB.ProcessHeap, 0, 0x88)       새 항목
 │                 │                 ├─ +0x11c NtQuerySystemTime (= KUSD 읽기)                 항목 +0x18
 │                 │                 ├─ +0x154 NtSetInformationProcess(-1, 0x35, {항목, 0})   → nt RtlInsertDynamicUserFunctionTable +0x20c  EPROCESS.InvertedFunctionTable (run51)
 │                 │                 ├─ +0x165 LdrProtectMrdata(0)  → NtProtectVirtualMemory(4)
 │                 │                 │  락 → +0x1ae · +0x1f0 RtlAvlInsertNodeEx (TreeMin · TreeMax) → +0x27d ~ +0x28e 목록 끝 → 락 풀기
 │                 │                 └─ +0x2a4 LdrProtectMrdata(1)  → NtProtectVirtualMemory(2)   · +0x2a4 *h = 항목
 │                 │  +0x1a9 Flags 비트 21 (ScpInExceptionTable)  ← 반환 0 일 때만
 │                 └─ …
 │  …
 └─ +0x127c RtlInitializeHistoryTable
          ├─ +0x1c LdrProtectMrdata(0)  → NtProtectVirtualMemory(4)
          ├─ +0x2e RtlpFunctionAddressTableEntry(i)  ×7 (마지막은 0)
          ├─ +0x55 RtlLookupFunctionEntry(pc, &시작, NULL)  ×6   ← 칸 0 빠른 길, syscall 없음
          │  +0x3f GlobalHint = 4 · +0x69 · +0x6e 칸 · +0x7c · +0x8c 한계 · +0x9f 개수 6   (RtlpUnwindHistoryTable)
          └─ ═▶ +0xb4 JMP LdrProtectMrdata(1)  → NtProtectVirtualMemory(2)
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). `═▶` 는 꼬리 `JMP` (콜스택에 안 보임). 호출 기호 없는 `+0x..` 줄은 그 함수 안 줄.
- 콜스택 기대 (#2 `NtSetInformationProcess` 에서 `k`): `NtSetInformationProcess` ← `RtlAddGrowableFunctionTable+0x154` ← `RtlpInsertOrRemoveScpCfgFunctionTable+0xc9` ← `LdrpProcessMappedModule+0x1a3` ← `LdrpInitializeNtdllDataTableEntry+0x124` ← `LdrpInitializeProcess+0x11a2` ← (콜스택 표시) `LdrpInitialize+0x16c`. `[정적]`

### 사진 대조 (run44 P1a 덤프 읽기 로그 — 결과서가 읽고 flow-writer 가 줄을 다시 봄)

| 칸 | 사진 | 뜻 |
|---|---|---|
| ntdll 노드 `Flags` (`+0x68`) | `0x20a2c4`, `ScpInExceptionTable : 0y1` (`04_baggage-read_P1a.txt:2795`·`:2817`) | 비트 21 = `RtlpInsertOrRemove…` 가 **정확히 0** 을 돌려줬다 → 확장 있음 갈래 + `RtlAddGrowableFunctionTable` 성공 + `NtSetInformationProcess(0x35)` 성공 |
| test.exe 노드 `Flags` | `0x22c4`, `ScpInExceptionTable : 0y0` (`:2690`·`:2712`) | exe 는 확장 없음 (`0x117`) — 비트 21 안 켜짐, `0x8000` 도 없음 (`GuardFlags` 쪽) |
| ntdll 노드 `DependentLoadFlags` (`+0x118`) | `0x800` (`:2846`) | `LdrpCfgProcessLoadConfig +0xa7` 마스크 값 그대로 — `LdrpIsModuleUnderSystem32` 갈래의 비트 8·9 안 켜짐 |
| ntdll VAD | `7ffafb660 ~ 7ffafb8cb` (`:1551`, `0x26c` 쪽) | `lm` 이미지 끝 `7ffa'fb8c7000` (`:59`, `0x267` 쪽) 보다 **5쪽 더 김** = 확장 영역 (ScpCfg 1 + HPAT 4 `[추측: DAT[3] = 0x5000]`) |
| `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` | 끝까지 0 (run60) | `LdrEnsureMrdataHeapExists +0x11 JZ +0x1f` (CFG 꺼짐) 와 맞음 |
| `RtlpDynamicFunctionTable*`·`TreeMin*`·`TreeMax*` | `≤P1a` 값, 뒤에 바뀜 `*` (run59) | `RtlAddGrowableFunctionTable` 이 항목을 달았다 — 쓰는 줄이 이번에 나옴 |
| `RtlpUnwindHistoryTable` | `≤P1a` (baggage 7.3) | `RtlInitializeHistoryTable +0x9f` 등 — 쓰는 줄이 이번에 나옴 |
| `EPROCESS.InvertedFunctionTable` | `P1~P1a` (run51) | #2 가 만든 것 — 비트 21 과 같이 맞음 |

## 확인된 것

- **`LdrpProcessMappedModule +0x19e` (ret `+0x1a3`) → `RtlpInsertOrRemoveScpCfgFunctionTable` 은 클래스 `0xe` 출력의 `+0x10` (크기) 과 확장 쪽 `+0x14` 둘이 0 이 아닐 때만 `RtlAddGrowableFunctionTable` 을 부르고, 0 을 돌려줄 때만 caller 가 비트 21 을 켠다.** `[정적]` (`+0x86 ~ +0x9d`, `LdrpProcessMappedModule +0x1a7`·`+0x1a9`)
- **진짜 VM 의 ntdll 노드는 비트 21 이 켜져 있다** `[관찰]` run44 (`04_baggage-read_P1a.txt:2795`·`:2817`). 그러므로 이 프로세스에서 #1 이 확장 있음을 돌려줬고 #2 `NtSetInformationProcess(0x35)` 가 성공했다 (`RtlAddGrowableFunctionTable +0xbc MOV EAX,EDI` — 실패면 `+0x230` 으로 빠져 음수 반환 → `+0x1a9` 안 돎). `[정적 + 관찰]`
  - → **F7 (`InvertedFunctionTable` 이 이 길로 왔나) 은 정적으로 거의 닫힘.** 남은 것은 풀 블록 내용을 직접 보는 것뿐 (아래 "아직 확인 필요" 3).
  - kernel32·kernelbase 노드도 비트 21 이 켜져 있다 (baggage 7.2 `Flags.ScpInExceptionTable(ntdll·k32·kb)`) → 같은 길을 세 번 탄 것으로 보인다 `[추측: 개수]`.
- **ntdll 노드 `Flags` `0xa2c4` 의 비트마다 쓴 자리** (PDB 비트 이름):

| 비트 | 켜는 곳 | 근거 |
|---|---|---|
| `0x4` `ImageDll` | `LdrpAllocateModuleEntry +0x11a` (run35), `LdrpInitializeNtdllDataTableEntry +0x93 OR [RBX+0x68],0x204` 가 한 번 더 | `[정적]` |
| `0x200` `InExceptionTable` | `LdrpInitializeNtdllDataTableEntry +0x93` (같은 `OR`) | `[정적]` run35 |
| `0x40` `InLegacyLists` · `0x80` `InIndexes` | `LdrpInsertDataTableEntry +0x5e` · `LdrpInsertModuleToIndexLockHeld +0xec` (run35 — 결과서는 `[추측]` 으로 둠, 아래 "결과서와 다르게") | `[정적]` run35 |
| `0x2000` `LoadConfigProcessed` | `LdrpProcessMappedModule +0x154 OR [RBX+0x68],EBP` (`EBP = 0x2000` `+0xc2`) — `LdrpCfgProcessLoadConfig` 성공 직후 | `[정적]` |
| **`0x8000` `ProtectDelayLoad`** | **`LdrpCfgProcessLoadConfig +0xc5`** (`GuardFlags & 0x1000`) | `[정적]` |
| `0x200000` `ScpInExceptionTable` (VM 만) | `LdrpProcessMappedModule +0x1a9 BTS [RSI],0x15` | `[정적]` + `[관찰]` |

  - 합 `0x4 + 0x40 + 0x80 + 0x200 + 0x2000 + 0x8000 = 0xa2c4` (PE_emulator), `+ 0x200000 = 0x20a2c4` (VM). `[정적 + 관찰]`
- **`RtlAddGrowableFunctionTable +0x40` 은 인자 검사 (`+0x27`/`+0x3a`) 뒤 늘 `LdrEnsureMrdataHeapExists` 를 부르고, CFG 확인은 그 안 (`+0xa/+0xf` · `+0x11 JZ +0x1f`).** test.exe 는 `LdrSystemDllInitBlock+0xb8 = 0` 이라 `LdrControlFlowGuardEnforced` 가 0 → `LdrpMrdataHeap` 은 끝까지 0, 항목은 프로세스 힙. `[정적]` + 사진 `[관찰]` (run60·baggage 3.9).
- **`LdrpCfgProcessLoadConfig` 의 `NtProtectVirtualMemory` 세 자리는 ntdll 노드에서 안 탄다** — `+0x16a` 는 지연 가져오기 디렉터리가 있어야 하고 (ntdll PE 디렉터리 0·2·3·4·5·6·10 만), `+0x4be`·`+0x535` 는 CFG 강제여야 한다. `[정적]` + 파일 값.
- **`RtlInitializeHistoryTable` 는 `.mrdata` `RtlpUnwindHistoryTable` (`0x1e7350`) 을 채운다** (개수 6 · `GlobalHint` 4 · 한계 둘 · 칸 6). `.mrdata` 를 열고 닫아 `NtProtectVirtualMemory` 2번. `[정적]` (`+0x3f`·`+0x69`·`+0x6e`·`+0x7c`·`+0x8c`·`+0x9f`)
- **`RtlpLookupDynamicFunctionEntry` 는 연결 목록을 안 걷고 나무 넷만 본다.** `_LdrpInitialize +0x10d ~ +0x13e` 가 비워 둔 직후엔 NULL. `[정적]`

## 결과서와 다르게 적은 곳

- **`RtlAddGrowableFunctionTable` 의 첫 `LdrProtectMrdata(0)` 자리** — 결과서는 `+0x15e/+0x165` 로 적었다 (질문 4·syscall 표 #3). `+0x15e` 는 `XOR ECX,ECX` (인자 0) 이고 CALL 은 **`+0x160` / ret `+0x165`** 다 (`.asm`, 요청서 지도 `+0x160` 과 같음). 뜻은 같다. 이 문서·flow.md·baggage 는 `+0x160/+0x165` 로 적었다.
- **`Flags` 비트 `0x40` `InLegacyLists`·`0x80` `InIndexes`** — 결과서 질문 3 표는 "`LdrpInsertDataTableEntry` · `LdrpInsertModuleToIndex` 안으로 `[추측: 본문 안 읽음]`" 이라 적었다. run35 가 이미 `[정적]` 으로 쓴 줄을 봤다: `LdrpInsertDataTableEntry +0x5e` · `LdrpInsertModuleToIndexLockHeld +0xec` (flow.md 2.4, baggage 3.5). 이 문서는 `[정적]` run35 로 적었다.
- **`ImageDll` (`0x4`)** — 결과서는 `LdrpInitializeNtdllDataTableEntry +0x93` 만 적었다. run35 는 `LdrpAllocateModuleEntry +0x11a` 가 먼저 켜고 `+0x93` 은 같은 비트를 다시 켠다고 봤다 (새로 켜는 건 `0x200` 하나). 둘 다 적었다.
- 결과서 숫자·판정은 고치지 않았다.

## 정정

- **run60 — `LdrpMrdataHeap`·`LdrpMrdataHeapUnprotected` 의 "② 조건부" 뜻.** run60 은 "test.exe 에선 쓰기까지 안 간 것 `[추측]`", "`LdrEnsureMrdataHeapExists +0xa` 의 CFG 확인도 그 갈래일 것 `[추측]`" 이라 적었다. 두 짐작은 **맞았다** → `[정적]`. 그런데 그 뒤 이 길을 "`RtlAddGrowableFunctionTable` 이 `LdrEnsureMrdataHeapExists` 를 안 타는 갈래" 로 읽은 것 (sast/58 요청서 "이미 아는 것", flow.md 2.4 run51 절 "안 간 것") 은 **틀렸다**: `+0x40` (ret `+0x45`) 은 인자 검사 `+0x27`/`+0x3a` 뒤 **늘** 부르고, 갈림은 그 안 `+0x11 JZ +0x1f` 다. 사진의 `LdrpMrdataHeap = 0` 과 `EPROCESS.InvertedFunctionTable ≠ 0` 은 이 한 갈래로 같이 설명된다 ("다른 길이 `InvertedFunctionTable` 을 썼다" 는 가설은 필요 없음).
  - run60 문서 맨 위에 한 줄. flow.md 2.4 run51 절 트리 줄·4장 run60 "결과서와 다르게" 줄, 짐 지도 3.9 `LdrpMrdataHeap` 줄·2장 run60 메모.
- **run51 — F7 "`InvertedFunctionTable` 이 클래스 `0x35` 길로 왔나 `[공백]`"** → **정적으로 거의 닫힘** (비트 21 `[관찰]` 근거, 위 "확인된 것"). 남은 것은 풀 블록 내용 (개수 3 인가) 직접 확인뿐.
  - run51 문서 맨 위에 한 줄. flow.md 2.4 run51 절·4장 Q79 줄, 짐 지도 3.11 `InvertedFunctionTable` 줄·7.4·7.14 F7.

## 짐 지도 반영 (`baggage.md`)

- 3.5 `_LDR_DATA_TABLE_ENTRY` 에 **4줄** (근거 칸 `sast58`, 줄 끝 **"Q120a 로 닫힘"**): `Flags.LoadConfigProcessed` (`LdrpProcessMappedModule +0x154`) · `Flags.ProtectDelayLoad` (`LdrpCfgProcessLoadConfig +0xc5`) · `Flags.ScpInExceptionTable` (`LdrpProcessMappedModule +0x1a9`) · `DependentLoadFlags` (`LdrpCfgProcessLoadConfig +0xa7`). 모듈 항목 모르는 칸 34 → **30**, ntdll 31 → 35.
- 3.8 `.data` `RtlpDynamicFunctionTableLock` **둘째 줄** (Add 가 독점으로 잡고 풂). 이미 아는 칸이라 숫자는 그대로.
- 3.9 `.mrdata` 에 **3줄**: `RtlpUnwindHistoryTable` (새로 — 모름 29 → **28**, 아는 것 21 → 22) · `RtlpDynamicFunctionTable` 둘째 줄 (목록 끝에 달기) · `TreeMin`·`TreeMax` 둘째 줄 (AVL 넣기). 고친 줄: `LdrpMrdataHeap`·`HeapUnprotected` (정정 — 조건은 Ensure 안), `LdrpMrdataUnprotected` (Add·History 열고 닫기 두 쌍 덧붙임).
- 3.11 `InvertedFunctionTable` 줄 ntdll 사슬에 클래스 `0xe` 앞 단계, F7 거의 닫힘.
- 3.17 `_MMVAD` 머리에 ntdll VAD 확장 메모 (칸 단위 근거는 아직 없음 — 숫자 그대로).
- 4장 짝 2줄 (동적 함수표 항목 ↔ `InvertedFunctionTable` · 확장 쪽 ↔ VAD 확장·커널 ScpCfg 쪽), 5장 ② 4 의 ntdll 항목 부분, 6장 부품 3줄 (`LdrControlFlowGuardEnforced` · `RtlAddGrowableFunctionTable` → `LdrEnsureMrdataHeapExists` · `RtlLookupFunctionEntry`), 7.2·7.3 목록, 7.4·7.14 F7, 새 **7.6.4** (`_DYNAMIC_FUNCTION_TABLE` 칸 표 + 확장 쪽 + 커널 전역), 새 **7.23** (동적 — Q104 VM 단계).

## 아직 확인 필요

- 동적 (결과서 "동적분석으로 확인해야 할 것" 1 ~ 6 번호 그대로 — 대기열 **Q104 VM 단계** 몫, 오케스트레이터가 plan 에 얹음). 주소는 ntdll 이미지 시작 + rva, test.exe 프로세스 조건 (`/p <test EPROCESS>`). 모두 "이미 정적 + 사진으로 맞음" 이라 급하지 않다:
  - **1** `ntdll!RtlpInsertOrRemoveScpCfgFunctionTable+0x5d` (`ntdll+0xa788d`, `NtQueryVirtualMemory` 에서 돌아온 자리): `r eax` (기대 0) · `dq @rsp+0x40 L3` (기대 `0, 0x267000, 0x1000`) · `dq @rsp+0x30 L1` (기대 `0x18`) · `r rbx` (이미지 시작). 이어서 `db @rbx+0x267000+0x40 L3` (형 0 이면 `48 ff e0`) · `dd @rbx+0x267000+0x14 L1` (기대 `0x2a4`) · `dd @rbx+0x267000+0x2a4 L3` (기대 `0, 0x280, 0x298`).
  - **2** `nt!MiQueryImageExtensionInformation` 입구와 `+0x1dc` (복사 직전): `dq @rsp+0x28 L3` · `r ebx`. 같이 `dd nt+0xe2d8e0 L8` (기대 `[0] = 0`, `[2] = 0x1000`, `[3] = 0x5000?`) · `dd nt+0xe375ec L1` (기대 `0x4000?`). 조건: 현재 프로세스가 test.exe. (`MmQueryVirtualMemory +0x274764` 콜스택 표시는 `[추측]` 이라 입구로 잡음 — 결과서 "스크립트 문제")
  - **3** `ntdll!RtlAddGrowableFunctionTable+0x154` (`ntdll+0xa7434`, `r edi` = 0) 와 `+0x2a4` (`ntdll+0xa7584`, 핸들): `dq ntdll+1e7060 L2` (목록 머리) · `dq ntdll+1e7070 L2` (나무 둘) · `dq ntdll+1e7010 L1` (`LdrpMrdataHeap`, 기대 0) · `dq ntdll+1e7020 L1`. 그 뒤 `dt nt!_EPROCESS <test> InvertedFunctionTable` → `dq <값> L6` (개수 3 인가 — **F7 마저 닫기**, Q79 출처).
  - **4** `ntdll!RtlInitializeHistoryTable+0x9f` (`ntdll+0xf215b`): `dq ntdll+1e7350 L1b` (개수 6 · `GlobalHint` 4). 같은 사슬에서 `ntdll!RtlpxLookupFunctionTable+0x2d5` 가 `LdrpInitializeProcess +0x127c` 까지 이 호출자 아래에선 **안 걸려야** 한다 (빠른 길 확인).
  - **5** 클래스 6 직접: `ntdll!RtlpxLookupFunctionTable+0x2d5` 첫 히트 (`LdrpInitializeProcess +0x43f` 자리): `dq @rsp+0x30 L3` (기대 `ntdll 시작, 0x267000, 플래그 (비트 0·1 꺼짐)`). run60 동적 2 와 같은 자리.
  - **6** `dt nt!_LDR_DATA_TABLE_ENTRY <ntdll 노드> Flags` = `0x20a2c4` (사진으로 이미 맞음 — 3 과 같이).
- 정적 (결과서 "못 닫은 것" + flow-writer, 대기열 번호는 오케스트레이터가 붙임):
  - ntdll 이미지가 왜 `ImageExpansionCompatible` (`_MI_EXTRA_IMAGE_INFORMATION +0xc` 비트 6) 인가, 작업집합 쪽 `[[EPROCESS+0x410]+0x425] & 4` 가 언제 켜지나 (`MiMapProcessExecutable → MiMapAllImageScpPages +0x1c` 의 호출 조건). VM 결과가 이미 "확장 있음" 이라 ntdll 값에는 영향 없음. test.exe·kernel32 등 **다른 이미지에 줄 값** 이 필요해지면 그때.
  - `DAT[3]` (`nt+0xe2d8ec`) 값 `0x5000` 과 `[0xe375ec] = 0x4000` 은 `[추측]` — `IoInitSystemPreDrivers +0x1123` 의 `R12D` 를 안 따라감. 동적 2 로 닫힘.
  - `LdrpIsModuleUnderSystem32` 가 ntdll 노드에서 왜 거짓인가 (`LdrpSystem32` 값·길이 비교). 사진 `DependentLoadFlags 0x800` 으로 "거짓" 은 `[관찰]`.
  - Q120b 묶음 (TLS, `LdrpAllocateSchedulerSharedData`, `RtlInitializeProcessorFeaturesBitMap`, KnownDlls 등) — 요청서 범위 밖.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (정적 바퀴). 결과서가 남긴 참고 하나: `MmQueryVirtualMemory` 의 클래스 `0xe`·6 몸통이 **같은 함수의 떨어진 범위** (`+0x274348 ~`) 에 있어, WinDbg 가 이 자리를 `nt!MmQueryVirtualMemory+0x274764` 로 보여 줄지 모른다 `[추측]`. 멈춤점은 떨어진 범위 안이 아니라 `MiQueryImageExtensionInformation` 입구로 잡는 게 안전하다. → windbg-kd-runbook pitfalls 후보 (동적 2 를 돌린 뒤 실제로 어떻게 보이는지 확인되면 제안).

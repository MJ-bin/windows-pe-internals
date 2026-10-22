# 13 proc-init-setup — 정적 분석 요청

- 날짜: 2026-10-22
- 스프린트: 02_sprint (예정 run18)
- 대기열: Q9a (`_workspace/plan.md`) — Q9 를 둘로 나눈 앞쪽. 뒤쪽(`+0x12de ~ 끝`)은 Q9b 로 다음 바퀴
- 관련: flow.md 2.4 `#### ntdll!LdrpInitializeProcess` 절 (지금은 `[관찰]` 3줄 + `[추측]` 1줄뿐), 4장 2.4 줄 / 이전 요청 sast/12 (`_LdrpInitialize +0x167` 이 이 함수를 부름)

## 범위 (꼭 지킬 것)

- 대상은 `ntdll!LdrpInitializeProcess` 하나, **`+0x0 ~ +0x12dd`** 구간만. `+0x12de`(`LdrpInitParallelLoadingSupport`) 부터 끝까지는 이번에 **보지 않는다** (Q9b).
  - 단, 앞쪽 구간에서 뒤쪽으로 바로 뛰는 갈림길(실패 길 등)이 있으면 "어디로 뛰는지"만 적는다.
- depth 1: 이 함수가 부르는 것의 **이름과 자리**만. callee 본문은 읽지 않는다. 이름·인자로 짐작한 역할은 `[추측]` 으로.
- `memset`·`RtlAppendUnicode*`·`LdrpLogInternal`·`RtlAllocateHeap`/`RtlFreeHeap` 같은 잡일 호출은 하나씩 풀지 말고 "어느 묶음 안의 잡일"로 묶어 개수만.

## 알고 싶은 것

1. **입구 인자**: 첫째 인자 `RCX`(로더가 넘긴 문맥)와 둘째 인자 `RDX`(커널이 넘긴 값, run17 `[정적]`)를 어느 레지스터·스택 칸에 저장하고, 앞쪽 구간에서 **처음 쓰는 곳**은 어디인가 (`+0x..` 와 그 줄이 하는 일). 앞쪽 구간에서 안 쓰면 "안 씀"이라고 쓴다.
2. **앞쪽 구간의 큰 순서**: 호출 95곳을 하는 일별 **묶음 5 ~ 10개**로 나누어 시간 순서대로 적는다. 묶음마다:
   - 오프셋 범위, 한 줄 요약 (예: "NLS 준비", "실행 옵션(IFEO) 읽기", "프로세스 힙 만들기", "시스템 경로·KnownDlls 열기", "ntdll 모듈 항목 만들기")
   - **대표 호출 1개** (흐름도 상자 후보) — 이름 + `+0x..`(call) / ret 오프셋
   - 묶음 안 잡일 호출 개수
3. **이 구간에서 DLL 이나 이미지를 메모리에 올리는 호출이 있나?** (예/아니오). `NtMapViewOfSection`·`NtCreateSection`·`LdrpMapDll*`·`LdrpMinimalMapModule` 같은 것. 있으면 자리와 무엇을 올리는지. (flow.md 2.5 `[추측]` "`LdrpInitializeProcess` 가 시작될 때는 test.exe 와 ntdll 만 올라와 있다"와 관련. 이번엔 "앞쪽 구간에서는 안 올린다"까지만 답할 수 있으면 된다)
4. **프로세스 모듈 목록(PEB.Ldr)**: `LdrpInitializeNtdllDataTableEntry` 를 두 번 부른다 (`+0x119d`, `+0x126a`). 두 번 부르는 이유(갈림길 조건)와, 그 앞뒤에서 `PEB->Ldr` 이나 모듈 목록 머리를 쓰는 줄이 있는지. exe 의 모듈 항목은 이 구간에서 만드나, 뒤쪽(`+0x1359 LdrpAllocateModuleEntry`)인가?
5. **일찍 끝나는 길**: 앞쪽 구간에서 함수가 실패로 끝나는(에필로그·`RtlRaiseStatus` 로 가는) 갈림길이 몇 개인가. 대표 2 ~ 3개의 조건만.
6. **sast/12 가 넘긴 연결점** (전역 변수, `_refs/` 나 `run_ghidra.sh refs` 로 확인):
   - `+0x587` 에서 `LdrpIsHotPatchingEnabled` 를 쓰는 조건 (무슨 값으로 정하나)
   - `+0x5f2` 에서 `UseWOW64` 를 쓰는 조건 (test.exe 같은 x64 exe 면 0 인가)
   - `+0x1233` 에서 `LdrpNtdllHotPatchContext` 를 읽어 무엇에 쓰나
   - `+0x153` `LdrInitializePerThreadRng` — 첫 스레드는 여기서 부른다는 sast/12 결과가 맞나 (한 줄 확인)
7. **디버거 때문에 달라지는 길**: 앞쪽 구간에 `PEB.BeingDebugged`(PEB `+0x2`) 나 `NtGlobalFlag`(PEB `+0xbc`), `KUSER_SHARED_DATA.KdDebuggerEnabled` 를 읽고 갈라지는 곳이 있나. 있으면 자리와 갈라지는 내용 (예: 디버그 힙, 로더 스냅). 커널 디버거만 붙었을 때 켜질지는 `[추측]` 으로.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpInitializeProcess$` | 1 | `+0x0 ~ +0x12dd` 호출 목록·묶음·입구 인자 (이미 `ms/disassemble/ntdll/LdrpInitializeProcess.asm` 있음 — 하네스 O1a·O1b 뒤 다시 뽑았는지 확인하고, 아니면 다시 뽑기) |
| ntdll | 전역 `LdrpIsHotPatchingEnabled`, `UseWOW64`, `LdrpNtdllHotPatchContext` | - | 질문 6 — 누가 읽고 쓰나 (`refs`) |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpInitializeProcess` rva `0x8c404`, 크기 7750(0x1e46), **깊이 4**, caller 1곳(`_LdrpInitialize`), expanded.
- 직접 호출 줄 169개 (`call` 165, `indirect` 1, `indirect-cfg` 3), 서로 다른 callee 113개. 간접 호출 4곳은 모두 뒤쪽(`+0x17fc`, `+0x1b59`, `+0x1cdf`, `+0x1d16`) — 이번 범위 밖.
- 앞쪽 `+0x0 ~ +0x12dd` = 95줄, 뒤쪽 = 74줄.
- 앞쪽에서 눈에 띄는 것 (지도 그대로, 역할은 아직 모름):
  - `+0x153 LdrInitializePerThreadRng`, `+0x1c8 LdrpInitializeNlsInfo`
  - `+0x35e`/`+0x400 RtlImageNtHeaderEx`, `+0x415 RtlInsertInvertedFunctionTable`, `+0x441`·`+0x47c`·`+0x488` `LdrProtectMrdata`, `+0x481 LdrpCaptureCriticalThunks`
  - `+0x531 LdrpInitializeExecutionOptions`, `+0x6c0 RtlNormalizeProcessParams`, `+0x6ef LdrImageDirectoryEntryToLoadConfigEx`, `+0x8f2 LdrQueryImageFileExecutionOptions`
  - `+0x9ed RtlInitializeHeapManager`, `+0xa4b LdrpInitializeProcessHeap`
  - `+0xb83 TpInitializePackage`, `+0xc06`/`+0xc23 EtwEventRegister`
  - `+0xc2d RtlpInitEnvironmentBlock`, `+0xc3a RtlpInitParameterBlock`
  - `+0xdb8 NtOpenDirectoryObject`, `+0xe47 NtOpenSymbolicLinkObject`, `+0xe90 NtQuerySymbolicLinkObject` (KnownDlls 쪽 `[추측]`)
  - `+0xfc5 LdrpInitializePolicy`, `+0xfe4 LdrpIncludeAlternateForwarders`, `+0x117e LdrpCheckAppDirType`
  - `+0x119d`/`+0x126a LdrpInitializeNtdllDataTableEntry`, `+0x1277 RtlInitializeHistoryTable`
- 뒤쪽 첫 호출 = `+0x12de LdrpInitParallelLoadingSupport` (경계). 그 뒤에 `+0x1359 LdrpAllocateModuleEntry`, `+0x199d LdrpInitializeKernel32Functions`, `+0x1aa4 LdrpMapAndSnapDependency`, `+0x1bb6 LdrpInitializeGraphRecurse`, `+0x1c94 LdrpCallTlsInitializers` 가 있다 (Q9b 몫, 이번엔 안 봄).

## 이미 아는 것

- `[관찰]` 메인 스레드에서 한 번 불린다. 뒤쪽에서 `+0x19a2`(kernel32 준비) → `+0x1bb6`(import DLL 초기화) → `+0x1c99`(exe TLS) 순서 (run7·run8, flow.md 2.4). 모두 뒤쪽이라 이번 범위 밖.
- `[정적]` (sast/12·run17) `_LdrpInitialize +0x167` 에서 `LdrpInitializeProcess(RCX = R12(로더 문맥), RDX = [RSP+0x88](커널이 넘긴 RDX))` 로 부른다. 첫 스레드 길만. 이 호출은 예외 보호 구역 안 (필터 `_LdrpInitialize$filt$0` → `LdrpInitializeProcessWrapperFilter`).
- `[정적]` (sast/12) `LdrInitializePerThreadRng` 는 첫 스레드에선 `LdrpInitializeProcess +0x153` 에서 부른다.
- `[정적]` (sast/12) 이 함수가 쓰는 전역: `LdrpIsHotPatchingEnabled` 쓰기 `+0x587`, `UseWOW64` 쓰기 `+0x5f2`, `LdrpNtdllHotPatchContext` 읽기 `+0x1233` (그리고 뒤쪽 `+0x18ef`).
- `[추측]` (flow.md 2.4) "프로세스 하나당 한 번만 하는 유저 쪽 준비의 총괄 함수".
- 이름 주의: `LdrpInitialize` 이름이 0xe1448 과 0x8bf8c(`_LdrpInitialize`) 에 걸친다 (run17). 이번 함수 이름은 겹침이 없는지 `pdb_aliases.py --where ntdll LdrpInitializeProcess` 로 한 번 확인.

## 받고 싶은 것

- 질문별 답 + 근거 (`LdrpInitializeProcess.asm` 의 `+0x..` 줄). 태그 `[정적]` / `[추측]`.
- 질문 2 는 **표 하나**로 (묶음 | 범위 | 요약 | 대표 호출 call/ret | 잡일 수). flow-writer 가 그대로 2.4 에 옮길 수 있게.
- 그림 2 에 넣을 `(정적)` 상자 후보 3 ~ 5개 (묶음 대표 중에서, 이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 추천 (`ntdll+rva` 형식. 대기열 맨 뒤에 붙일 거라 짧게).
- 앞쪽 구간 안에서 Q9b·Q10~Q16 에 넘길 연결점이 보이면 한 줄씩.
- 스크립트 문제가 보이면 고치지 말고 "스크립트 문제" 절에 적기 (하네스는 오케스트레이터가 사용자 검토 뒤 고친다).

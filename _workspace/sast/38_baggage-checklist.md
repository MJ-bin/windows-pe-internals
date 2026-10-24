# 38 baggage-checklist — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 03_sprint (짐 지도 첫 바퀴, pe-flow-cycle 단계 4)
- 관련: `_workspace/plan.md` Q76, CLAUDE.md "지금 목표 (03_sprint~)", `.claude/skills/flow-doc/baggage-template.md`

## 배경 (이번 바퀴가 하는 일)

- 짐 지도 = 초기화 동안(프로세스 생성 `NtCreateUserProcess` → 은퇴 `NtContinue`) nt·ntdll이 두 공간에 세팅하는 것을 빠짐없이 센 목록.
  - 유저 공간 = 게스트 가상 주소 공간 (PEB, TEB, 매개변수, 이미지, ntdll 전역, 로더 구조 …)
  - 커널 공간 = 커널 객체·장부 (프로세스·스레드 객체, 핸들 표, 메모리 구역 장부, 토큰, 섹션 …)
- 이번 바퀴(A)는 **체크리스트만** 만든다: PDB 칸 목록 + ntdll 전역 목록 + 초기화 함수들의 callee 목록, 그리고 이미 문서에 있는 "누가 씀"을 대조해 표시.
- 칸마다 실제로 쓰는 줄을 새로 찾는 것(Q78), 상태 사진(Q77)은 다음 바퀴. 이번엔 하지 않는다.

## 알고 싶은 것

1. **구조체 칸 목록** (PDB 타입) — 구조체마다 크기, 직접 칸 수, 칸마다 (오프셋, 이름, 타입, 크기). 비트 칸이 든 칸(Flags 류)은 비트 이름·위치도. 이름 없는 union/struct 는 안쪽을 펴서 적는다.
   - 커널 공간 (`ntoskrnl.exe.pdb`): `_EPROCESS`, `_KPROCESS`, `_ETHREAD`, `_KTHREAD`, `_KTRAP_FRAME`, 핸들 표, 메모리 구역 장부(VAD), 토큰, 섹션·컨트롤 영역·세그먼트·`_SECTION_IMAGE_INFORMATION`, 파일 객체.
     - 핸들 표·VAD·토큰·섹션 쪽은 정확한 타입 이름을 PDB에서 찾아 쓴다 (예상 이름: `_HANDLE_TABLE`, `_MMVAD`/`_MMVAD_SHORT`, `_TOKEN`, `_SECTION`/`_CONTROL_AREA`/`_SEGMENT`, `_FILE_OBJECT`). 없거나 다르면 가까운 이름과 함께 그렇다고 적는다.
   - 유저 공간: `_PEB`, `_TEB`, `_RTL_USER_PROCESS_PARAMETERS`, `_PEB_LDR_DATA`, `_LDR_DATA_TABLE_ENTRY`, `_LDR_DDAG_NODE`, `_KUSER_SHARED_DATA`.
     - 어느 PDB(ntdll / ntoskrnl)에서 뽑았는지 적는다. 둘 다 있으면 크기가 같은지 본다.
     - `LdrSystemDllInitBlock` 의 타입이 PDB에 있는지 다시 확인한다 (run32 는 "PDB 에 이 타입이 없다" 고 했다). 없으면 없다고 적는다.
2. **ntdll 전역 변수 목록** (`ntdll.dll.pdb` 공개 이름 중 데이터) — 이름, 섹션, rva. 크기는 PDB에 있으면 그 값, 없으면 다음 이름까지의 거리로 짐작하고 `[추측]` 표시.
   - 오케스트레이터가 센 수: `.data` 438, `.mrdata` 50 (`llvm-pdbutil-14 dump -publics`, 함수 아닌 것). 맞는지 확인하고, `.00cfg` 등 다른 쓰기 가능한 섹션도 있으면 따로 센다.
3. **초기화 함수들의 callee 목록** (지도 `ms/disassemble/<모듈>/_calls.tsv` 기준) — 함수마다 직접 부르는 함수 전부 (call 줄 수, 서로 다른 callee 수, 간접 호출 줄).
   - ntoskrnl: `NtCreateUserProcess`, `PspAllocateProcess`, `MmCreatePeb`, `PspSetupUserProcessAddressSpace`, `PspAllocateThread`, `MmCreateTeb`, `PspInsertProcess`, `PspInsertThread`, `PspUserThreadStartup`, `PspInitializeThunkContext`
   - ntdll: `LdrInitializeThunk`, `LdrpInitialize`, `LdrpInitializeInternal`, `_LdrpInitialize`, `LdrpInitializeProcess`
   - `MmCreateTeb` 는 장부에 줄이 하나도 없다 (본문을 뽑은 적 없음). 이번에 export 해서 callee 목록을 채운다.
4. **이미 아는 "누가 씀" 대조** — 1·2번 목록의 칸·전역마다, 이미 있는 근거에서 쓰는 자리를 찾아 표시한다.
   - 볼 곳: `runs/01_sprint/*.md`, `runs/02_sprint/*.md` (해설서 `*-해설.md` 는 빼고), `_workspace/sast/*_result.md`, `ms/disassemble/*/_refs/*.tsv` 의 WRITE·DATA 줄.
   - 칸마다: 쓰는 함수+오프셋, 근거(run 번호 또는 결과서), 확실도(오프셋·이름이 문서와 정확히 맞음 / 짐작). 못 찾으면 "모름".
   - 3번 callee 마다: flow.md 에 나옴 / run·결과서에 이름만 / 어디에도 없음.

## 대상

| 모듈 | 무엇 | 이유 |
|---|---|---|
| ntoskrnl | PDB 타입 (1번 커널 쪽), `_calls.tsv` (3번) | 커널 공간 체크리스트 |
| ntoskrnl | `^MmCreateTeb$` (export, depth 1) | 장부에 없는 함수 |
| ntdll | PDB 타입 (1번 유저 쪽), 공개 이름 (2번), `_calls.tsv` (3번) | 유저 공간 체크리스트 |

## 지도에서 본 것

- 오케스트레이터가 센 callee 수 (`_calls.tsv`, alias 접두사 떼고): `NtCreateUserProcess` 61, `PspAllocateProcess` 105, `MmCreatePeb` 16, `PspSetupUserProcessAddressSpace` 18, `PspAllocateThread` 40, `PspInsertProcess` 20, `PspInsertThread` 42, `PspUserThreadStartup` 9, `LdrpInitialize` 4, `LdrpInitializeInternal` 6, `_LdrpInitialize` 20, `LdrpInitializeProcess` 109, `MmCreateTeb` 0 (장부 없음).
- 오케스트레이터가 센 직접 칸 수 (`ntoskrnl.exe.pdb`): `_EPROCESS` 263 (0x840), `_KPROCESS` 65 (0x1c8), `_ETHREAD` 149 (0x798), `_KTHREAD` 237 (0x4c0), `_PEB` 124 (0x7d8), `_TEB` 137 (0x1878).

## 이미 아는 것

- `[정적]` run10 `MmCreatePeb` 가 PEB 자리를 만들고 exe 헤더 값을 채움, PEB 주소는 프로세스 객체 `+0x2e0`
- `[정적]` run32 `PspCopyAndFixupParameters` PEB `+0x20`, `PspPrepareSystemDllInitBlock` 이 `LdrSystemDllInitBlock` (`+0xa0`, `+0xb8` 등)
- `[정적]` run15 `PspUserThreadStartup` 이 TEB `+0x108` (`CurrentLocale`)
- `[정적]` run18 `LdrpInitializeProcess +0x1c1` PEB.Ldr, run18 PEB TLS 비트맵 칸 (`+0x78`·`+0x80`·`+0x238`·`+0x240`), run19 `LdrpImageEntry`
- `[정적]` run20 `Kernel32ThreadInitThunkFunction` 은 직접 쓰기 없음, `LdrpInitializeKernel32Functions +0xe4` 주소 넘김
- 이 밖의 것은 4번에서 문서·결과서·`_refs` 로 찾는다.

## 만들 파일

- 체크리스트 원본(표 파일)은 `ms/disassemble/<모듈>/_baggage/` 에 둔다 (커밋 안 되는 곳, `_calls.tsv` 와 같은 대접). 예: `ntoskrnl/_baggage/struct__EPROCESS.tsv`, `ntdll/_baggage/globals.tsv`, `ntdll/_baggage/callees.tsv`. 열 이름은 첫 줄에.
- 결과서 `_workspace/sast/38_baggage-checklist_result.md` 에는 요약만 쓴다: 구조체별 크기·칸 수·"이미 아는 쓴 자리" 수, 전역 섹션별 수와 이미 아는 수, 함수별 callee 수와 "어디에도 없음" 수, 그리고 표 파일 경로와 열 설명.
- 임시 스크립트는 `ms/ghidra/_tmp_sast38/` 에 둔다. `.claude/` 는 고치지 않는다 (필요한 스크립트 문제는 결과서에 "스크립트 문제"로).

## 범위 밖 (이번에 하지 않음)

- 칸마다 실제로 쓰는 줄을 새로 찾기 (Q78), 상태 사진 (Q77), callee 를 "짐을 놓나 / 잡일 / 안 타는 갈래"로 나누기 (Q78).
- KERNELBASE·KERNEL32 안쪽.
- 어디까지 구현할지 판단.

## 받고 싶은 것

- 질문별 답 + 근거 (쓴 명령, 파일 경로)
- 표 파일 경로와 열 설명
- Q77(사진)에서 떠야 할 것 추천: 구조체별 덤프 대상, ntdll 쓰기 가능한 섹션의 rva·크기, 그 밖에 사진에 꼭 넣을 것 (예: KUSD·API set 지도 주소를 어디서 얻나)
- 새로 생긴 공백 (지도에 없는 함수, PDB 에 없는 타입 등)

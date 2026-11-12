# 77 thread-leftovers — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q142** (run74 남은 정적 ①~⑤ — 스레드 만들기 꼬리). 사용자 결정 2026-11-12 "vm 제외하고 Q142 까지" — 이번 묶음의 마지막. 정적만.
- 관련: sast/69 thread-create·sast/70 thread-stack (run74 — 결과서 `_workspace/sast/69_thread-create_result.md`, `_workspace/sast/70_thread-stack_result.md` 를 먼저 읽기)
- 빌드: 26100.9457 (ntoskrnl·kernelbase·ntdll). PDB GUID 가 다르면 멈추고 보고.
- 모듈이 셋이다. ntoskrnl Ghidra 프로젝트가 다른 작업으로 잠겨 있으면 (`pgrep -fa 'ghidra.app.util.headless'`) kernelbase·ntdll 쪽부터 한다.

## 배경

- run74 에서 둘째 스레드 (`CreateThread`) 를 커널 쪽에서 닫았고, 작은 꼬리 다섯이 남았다. ①이 닫히면 대기열 Q42 의 동적 몫 "`CreateThread` 가 커널에 정말 (0, 0) 을 넘기나" 가 정적으로 닫힌다.
- test.exe [소스] `sample/test.c:34`: `CreateThread(NULL, 0, Worker, NULL, 0, NULL)`.

## 알고 싶은 것

1. **① `KERNELBASE!CreateRemoteThreadEx`** (rva `0xca3f0`, 2126B — `kernel32!CreateThread` 가 여기로 오는지도 한 줄, 이름만):
   - (a) `NtCreateThreadEx` 를 부르는 오프셋 (ret).
   - (b) 넘기는 `CreateFlags` — `dwCreationFlags` (0) → `THREAD_CREATE_FLAGS_*` 바꾸는 줄. 0 이면 0 인가.
   - (c) 스택 인자 (9·10번째 `StackSize`·`MaximumStackSize`) — `dwStackSize = 0` 일 때 각각 무엇을 넘기나, `STACK_SIZE_PARAM_IS_A_RESERVATION` (`0x10000`) 갈래. **커널에 (0, 0) 이 가나** (예/아니오, 줄).
   - (d) `AttributeList` — NULL 인가, 아니면 어떤 속성을 넣나 (`CLIENT_ID`·`TEB_ADDRESS` 등, 항목 수).
   - (e) 부른 뒤 하는 일 (CSR 알림·활성화 문맥·`ResumeThread`) — 이름과 오프셋만.
2. **② `ZwSetInformationProcess` 클래스 `0x29`** (run74: `RtlCreateUserStack +0x104` 가 유저 스택 예약에 씀 `[추측]`): 커널 `NtSetInformationProcess` (rva `0x9329b0`, 19144B — **클래스 `0x29` 갈래만**) 가 부르는 함수, 그 함수가 정말 예약 (`MEM_RESERVE`) 을 하나, 정렬 (`0x10000`?), 정보 칸 `+0` 노드·`+8` 크기·`+0x10`/`+0x18` ZeroBits·출력 주소 해석. PDB 의 `PROCESSINFOCLASS` 값 `0x29` 이름.
3. **③ `PspBuildCreateProcessContext +0x1876 ~ +0x1882`** 의 `ctx+0x10` 비트 0 을 **누가 읽나** — `NtCreateThreadEx`·`PspCreateThread`·`PspAllocateThread`·`PspInsertThread` (이미 있음) 안에서 그 칸을 읽는 줄. 못 찾으면 "훑기 한계" 로 그렇다고만.
4. **④ `PspAllocateThread +0x8c7`** 노드 갈래 (`KeGetProcessorNodeNumber` 뒤) — 조건과 하는 일 (노드 ≠ 0 일 때). 이미 있는 `.asm` 에서 그 근처만.
5. **⑤ ntdll `RtlCreateUserStack`** (rva `0xf5130`, 709B) — 커널 `RtlCreateUserStack` (sast/70) 과 같은 꼴인가: 크기 0 → exe 헤더 (헤더는 PEB `ImageBaseAddress` 로 찾나), 다듬기, 예약·커밋·가드 3쪽. 다른 점만 표로. 누가 부르나 (callers 이름만 — 유저 쪽에서 스택을 직접 만드는 길이 있나).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernelbase | `^CreateRemoteThreadEx$` (rva `0xca3f0`, 2126B) | 1 | 질문 1 |
| ntoskrnl | `^NtSetInformationProcess$` (19144B) | 0 | 질문 2 — 클래스 `0x29` 갈래만 (전체 풀지 말 것) + 그 갈래가 부르는 함수 depth 1 |
| ntoskrnl | `^(PspBuildCreateProcessContext|NtCreateThreadEx|PspCreateThread|PspAllocateThread|PspInsertThread)$` | 0 | 질문 3·4 — 이미 있음, 해당 칸·줄만 |
| ntdll | `^RtlCreateUserStack$` (rva `0xf5130`, 709B) | 0 | 질문 5 |

- 줄기 밖은 이름만.

## 이미 아는 것

- `[정적]` sast/69 (run74): `NtCreateThreadEx +0x154 MOV byte [R14],0` (StackInfo 첫 바이트 늘 0), `PspSetupUserStack +0x89 → RtlCreateUserStack` 에 `RCX=[StackInfo+0x10]` 커밋·`RDX=[+0x18]` 예약, 속성 검사 `NtCreateThreadEx +0xde PspBuildCreateProcessContext` (`AttributeList` ≠ NULL 일 때만), 스레드 쪽 속성 다섯 (`0x10003`·`0x10004`·`0x3000c`·`0x3000e`·`0x3001e`).
- `[정적]` sast/70 (run74): 커널 `RtlCreateUserStack` — 0 인 쪽만 exe 헤더 (`+0x22d`·`+0x244`, 헤더는 `EPROCESS.SectionBaseAddress` `+0x2b0`), 다듬기 (커밋 0 → `0x4000`, 커밋 ≥ 예약 → 1MB 올림, 페이지·64KB 올림, `PEB+0x318`), `+0x104 ZwSetInformationProcess(0x29, 길이 0x28)`, `+0x170` 커밋, `+0x1c6` 가드 3쪽 (`0x104`).
- `[소스]` `sample/test.c:34` `CreateThread(NULL, 0, Worker, NULL, 0, NULL)`.

## 받고 싶은 것

- 결과서 `_workspace/sast/77_thread-leftovers_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`.
- 질문 1 끝에 한 줄: **"test.exe `CreateThread` → 커널 `NtCreateThreadEx` 인자 (CreateFlags, StackSize, MaximumStackSize, AttributeList)"** — Q42 동적 몫이 닫히는지.
- 정적으로 못 닫는 것은 "동적으로 확인할 것" — Q42·Q104 에 붙인다 (이번 묶음엔 VM 안 함).

# 03 create-user-process — 정적 분석 요청

- 날짜: 2026-10-19
- 스프린트: 02_sprint
- 대기열: **Q1** — 2.1 `nt!NtCreateUserProcess` callee (depth 1) (`_workspace/plan.md`)
- 관련: `runs/01_sprint/flow.md` 그림 1, 2.1, 2.2 `[공백]` / 이전 요청 sast/02 (ntdll 스크립트 시험 성공)

## 알고 싶은 것

1. `NtCreateUserProcess` 가 **직접 부르는 함수** 목록을 call 순서(오프셋 순)대로. 각 줄에 call_off / ret_off / callee.
   - 그중 01_sprint 관찰 4개가 정적으로도 같은 오프셋인가? (예/아니오, 줄마다)
     - `+0xc36` `PspAllocateProcess`, `+0xe5c` `PspAllocateThread`, `+0xfc2` `PspInsertProcess`, `+0x101a` `PspInsertThread`
2. 위 목록을 관찰 4개를 경계로 **구간별로** 나눠 주기: (가) `+0xc36` 앞, (나) `+0xc36`~`+0xe5c`, (다) `+0xe5c`~`+0xfc2`, (라) `+0xfc2`~`+0x101a`, (마) `+0x101a` 뒤 (반환까지).
   - 구간마다, 흐름도에 대표 지점으로 넣을 만한 호출을 1~3개 골라 이름과 이유(디컴파일 코드로 본 역할, 우리 말 한 줄)를 적는다. 나머지는 "잡일"(검사·잠금·해제 등)로 묶어 개수만.
   - 특히 (마)는 flow.md 2.2 `[공백]`(부모 쪽 마무리 ~ 첫 스레드 출발)과 관련 있다. `NtCreateUserProcess` 가 첫 스레드를 **멈춘 채로 두는지 / 풀어 주는지**가 코드에 보이면 근거 줄과 함께 적는다 (보이지 않으면 "정적으로 못 정함").
3. points.md 에 `[확인 필요]` 로 적어 둔 이름이 `NtCreateUserProcess` 가 **직접** 부르는 목록에 있는가? (예/아니오)
   - `MiCreateImageFileMap`, `MmInitializeProcessAddressSpace`, `PspMapSystemDll`, `MmCreateTeb`
   - 오케스트레이터가 `_index.tsv` 로 확인함: 네 개 모두 이 빌드에 함수로 **있다**. 직접 안 부르면 "Q2 이후에서 찾을 것"으로만 적는다 (depth를 늘려 쫓지 않는다).
4. `nt!PspCreateProcess`(옛 방식, 01_sprint에서 불리지 않음 관찰)를 `NtCreateUserProcess` 가 부르는 코드가 있는가? (예/아니오)
5. 간접 호출(`indirect:`)이나 꼬리호출(`tail`)이 있으면 목록에 그대로 두고 "동적으로 확인할 것"에 넘긴다.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^(NtCreateUserProcess\|PspInsertThread)$` | 0 | NtCreateUserProcess = Q1 본체 (callee 목록은 `_calls.tsv` 로 충분, callee 본문은 안 뽑음). PspInsertThread = ntoskrnl 첫 사용이라 알려진 사실 대조용 (`+0x807`) |

- **depth 0.** 대기열의 "callee (depth 1)" 은 "직접 부르는 함수 목록"이란 뜻이다. callee 본문은 Q2 이후 항목에서 뽑는다.
- ntoskrnl analyze 는 끝나 있다 (`ms/ghidra/ntoskrnl.gpr`, `ms/decomp/ntoskrnl/_index.tsv` 34982개, 로그 179줄 `SetPdb:`). **다시 analyze 하지 않는다.**
- 스크립트는 2026-10-19 묶음 B·D-1 반영본이다: 꼬리호출은 `ret_off=tail`, `INT` 는 표에서 빠지고 `.asm` 에 `; TRAP`, `run_ghidra.sh` 종료코드 4 = 로그에 SCRIPT ERROR.
- `ms/disassemble/ntoskrnl/_calls.tsv` 는 아직 없다 (처음 export).

## 이미 아는 것

- `[관찰]` (01_sprint run3) 부모 cmd.exe 스레드에서 차례로: `NtCreateUserProcess` `+0xc36` → `PspAllocateProcess`, `+0xe5c` → `PspAllocateThread`, `+0xfc2` → `PspInsertProcess`, `+0x101a` → `PspInsertThread`. `PspInsertThread` `+0x807` → `PspCallProcessNotifyRoutines`. 이 값들은 WinDbg 콜스택의 돌아올 자리(= `ret_off`) 다.
- `[관찰]` `nt!KiSystemServiceCopyEnd+0x28` 이 `NtCreateUserProcess` 를 부름 (시스템 콜 입구, 간접 호출이라 이번 범위 밖. `_index.tsv` 에 함수로 없음).
- `[관찰]` 옛 방식 `nt!PspCreateProcess` 는 불리지 않았다.
- 커널 PDB GUID `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준값 (오케스트레이터가 확인).

## 받고 싶은 것

- 질문 1~5 답 + 근거 (`ms/disassemble/ntoskrnl/NtCreateUserProcess.asm` 의 `+0x` 줄, `.c` 요약)
- 버전 확인 줄: ntoskrnl 알려진 사실 2개 (`NtCreateUserProcess +0xc36`, `PspInsertThread +0x807`) 일치 여부
- 동적으로 확인할 것 + 추천 멈춤점 (있으면). 대기열 맨 아래에 붙일 후보로 쓴다.
- 결과서: `_workspace/sast/03_create-user-process_result.md`

# 70 thread-stack — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q141a** 이어서 (PE_emulator 요청 25 ㄱ-2 "스택 크기 0 → exe 헤더"). 사용자 결정 2026-11-12 "이번 바퀴에 하나 더" — sast/69 와 합쳐 run74 하나로 문서화한다.
- 관련: sast/69 thread-create 결과서 질문 2(d) 와 "후속" 절
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경

- sast/69 에서 둘째 스레드 (`NtCreateThreadEx`) 의 유저 스택은 커널이 잡는다고 닫았다: `PspAllocateThread +0x5ea` → `PspSetupUserStack +0x89` (ret `+0x8e`) → 커널 안 **`RtlCreateUserStack`** (rva `0x938db0`, 738B).
  - 넘기는 인자: `RCX = [StackInfo+0x10]`, `RDX = [StackInfo+0x18]`, `R8 = [StackInfo+0x8]`, `R9 = 0x1000 (| 노드 << 56)`, 출력 InitialTeb = `[RSP+0x28]`.
- 읽은 넷 (`NtCreateThreadEx`, `PspCreateThread`, `PspAllocateThread`, `PspSetupUserStack`) 에는 "크기 0 → exe 헤더 값" 줄이 없었다. 남은 후보는 `RtlCreateUserStack` 본문 하나다.
- 첫 스레드는 `NtCreateUserProcess +0xd92 ~ +0xdbc` 가 이미지 정보로 StackInfo 를 미리 채운다 (reserve 최소 `0x40000`). 이번엔 둘째 스레드 길만 본다.
- PE_emulator 의 지금: "스택 크기 0 → exe 헤더 (`SizeOfStackReserve`·`SizeOfStackCommit`)" `[추측]`. 맞는지·틀리는지 한 줄로.

## 알고 싶은 것

1. `RtlCreateUserStack` 의 첫째 (`RCX`) 와 둘째 (`RDX`) 인자는 각각 커밋 크기·최대 (reserve) 크기인가? (sast/69 표기대로인지 확인)
2. **둘 중 하나가 0 이면 다른 값으로 바꾸는 줄이 있나** (예/아니오, 오프셋)?
   - 있으면 어디서 값을 가져오나: exe 헤더 (`SizeOfStackReserve` `+0x48`·`SizeOfStackCommit` `+0x50` of `IMAGE_OPTIONAL_HEADER64` `[추측]`) / PEB 칸 / 프로세스 객체 칸 / 상수.
   - exe 헤더라면 헤더는 어떻게 찾나 (PEB `ImageBaseAddress`? 섹션 정보?) — 줄로.
3. 크기를 다듬는 규칙: 커밋 ≥ reserve 일 때, 페이지·`0x10000` (할당 단위) 올림, 최소값 — 각각 오프셋과 상수.
4. `+0x202` `RtlImageNtHeader` 는 무엇에 쓰나 — 질문 2 의 헤더 읽기인가, 할당 (`+0x170`·`+0x1c6`) **뒤**라 다른 용도인가?
5. 할당 꼴: `+0x170` / `+0x1c6` `ZwAllocateVirtualMemory` 두 번 = reserve 한 번 + 커밋 한 번인가? 가드 페이지를 놓는 줄 (보호값 `PAGE_GUARD`), `+0x104` `ZwSetInformationProcess` 의 정보 클래스 번호와 이유, 실패 때 `+0x28d` `ZwFreeVirtualMemory`.
6. 출력 InitialTeb (`+0x10` StackBase, `+0x18` StackLimit, `+0x20` AllocationBase — sast/69) 를 채우는 줄과 계산식.
7. 넷째 인자 (`R9 = 0x1000 | 노드 << 56`) 와 셋째 인자 (`R8` = ZeroBits `[추측]`) 의 쓰임 — 짧게.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^RtlCreateUserStack$` (rva `0x938db0`, 738B) | 0 | 질문 1 ~ 7 — 아직 `.asm`·`.c` 없으면 새로 뽑기 |

- callee (`ZwAllocateVirtualMemory`, `ZwSetInformationProcess`, `RtlImageNtHeader`, `ZwFreeVirtualMemory`) 본문은 **안 본다.** 이름·인자만.
- 유저 쪽 (`KERNELBASE!CreateRemoteThreadEx`, `ntdll!RtlCreateUserStack`) 은 이번에도 안 본다. 단, 커널 함수 본문이 ntdll 같은 이름 함수와 모양이 같아 보이면 "같아 보임 `[추측]`" 한 줄만.

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/NtCreateThreadEx_*`)

- `RtlCreateUserStack` 깊이 4, caller 1 (`PspSetupUserStack +0x89`), expanded.
- 직접 호출 5줄: `+0x104` `ZwSetInformationProcess`, `+0x170` `ZwAllocateVirtualMemory`, `+0x1c6` `ZwAllocateVirtualMemory`, `+0x202` `RtlImageNtHeader`, `+0x28d` `ZwFreeVirtualMemory`.
- `RtlImageNtHeader` 가 할당 둘 **뒤**에 있다 → 질문 4.

## 이미 아는 것

- `[정적]` sast/69: `PspSetupUserStack +0x28 TEST [R9],1` — StackInfo 첫 바이트 bit0 ("이미 있음") 은 `NtCreateThreadEx +0x154 MOV byte [R14],0` 이 늘 0 → 늘 새로 만든다.
- `[정적]` sast/69: `CONTEXT.Rsp` = `PspSetupUserStack +0xdc`, `StackBase − ((ExGenRandom(1) & 0x7f) << 4) − 0x28` (랜덤 끄기 비트 `EPROCESS +0x750` 비트 6).
- `[정적]` sast/69: `MmCreateTeb +0x12a ~ +0x13a` 가 InitialTeb `+0x10/+0x18/+0x20` 을 TEB `+8/+0x10/+0x1478` 로 복사.
- `[정적]` sast/69: 첫 스레드 StackInfo = `NtCreateUserProcess +0xd92 ~ +0xdbc` (이미지 정보 `+0x1e8` MaximumStackSize, `+0x1f0` CommittedStackSize, reserve 최소 `0x40000`).

## 받고 싶은 것

- 결과서 `_workspace/sast/70_thread-stack_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret 오프셋).
- 마지막에 **"PE_emulator ㄱ-2 최종 답"** 한 문단: 둘째 스레드에서 `NtCreateThreadEx` 가 크기 0 을 받으면 실제 reserve·commit 이 무엇이 되나, 그 근거 줄. 첫 스레드와 같은 값인지 다른 값인지.
- 정적으로 못 닫는 것은 "동적으로 확인할 것" (멈춤점 `nt!RtlCreateUserStack` 입구·출구 추천) — 대기열 Q42 에 붙인다.

# 2026-11-13 run90 — 둘째 스레드 만들기 꼬리 (Q142, run74 남은 정적 ①~⑤): 유저 쪽 `kernel32!CreateThread +0x36` → `KERNELBASE!CreateRemoteThreadEx +0x2e2` → `NtCreateThreadEx` 는 **스택 (0, 0) · 속성 2개 (`CLIENT_ID` · `TEB_ADDRESS`) · CreateFlags 0 (조건 셋이 다 거짓일 때, 아니면 1)** 을 넘긴다. 커널 클래스 `0x29` 는 `NtSetInformationProcess +0x485` → `+0x50e MmAllocateUserStack` → `+0x12f MiAllocateVirtualMemory` → `+0x334 MiReserveUserMemory` 로 **예약 (`MEM_RESERVE`) 만** 한다 (시작 64KB 정렬). 노드 갈래 `PspAllocateThread +0x8c7` 은 이상적 프로세서 속성이 있을 때만 (값 = 노드 + 1), ntdll `RtlCreateUserStack` 은 커널 것과 같은 꼴 (부르는 곳은 kernelbase `CreateFiberEx` 하나). `ctx+0x10` 비트 0 을 읽는 곳은 못 찾음

## PE_emulator 쪽에서 쓸 것

- **한 줄**: test.exe 의 `CreateThread(NULL, 0, Worker, NULL, 0, NULL)` (`[소스]` `sample/test.c:34`) 은 커널에 이렇게 넘긴다 `[정적]`:
  - `NtCreateThreadEx(&핸들, 0x1fffff, NULL, -1, Worker, NULL, CreateFlags, 0, 0, 0, 속성 목록 2칸)`.
  - 조건이 붙은 건 `CreateFlags` 하나뿐이다. 보통 0, 조건 셋 중 하나라도 참이면 1.
  - 스택 크기 (0, 0) → 커널이 exe 헤더로 채움 → Worker 스택 = 예약 `0x100000` · 커밋 `0x1000` · 가드 `0x3000`. **첫 스레드와 같은 값**이다 (run74 식, `PEB.MinimumStackCommit` 0 일 때).
- run74 "지금 우리" 가정이 이번 답으로 어떻게 되나:

| 항목 | run74 때 | run90 답 | 지금 우리 (PE_emulator) | 근거 줄 |
|---|---|---|---|---|
| 스택 크기 (0, 0) → exe 헤더 | 커널 쪽은 맞음 `[정적]`. 유저가 정말 0 을 넘기나는 `[추측]` | **예 — 유저도 (0, 0)**. `dwStackSize = 0` 이면 `STACK_SIZE_PARAM_IS_A_RESERVATION` 비트와 상관없이 9·10번째 둘 다 0. 8번째 ZeroBits 도 늘 0 | **맞음** (이제 유저 쪽까지 닫힘) | `CreateRemoteThreadEx +0x280 ~ +0x291` (`CMOVNZ` 둘) · `+0x2a2` · `+0x2a7` · `+0x2ac` |
| 속성은 `CLIENT_ID` · `TEB_ADDRESS` 둘 | 커널은 다섯을 받으니 "둘만" 은 좁다 | `CreateThread` 길은 **늘 딱 둘**. `0x10003` (Size `0x10`, 값 = ClientId 받는 칸 16B) · `0x10004` (Size `8`, 값 = 새 TEB 주소 받는 칸). `TotalLength 0x48`, ReturnLength 없음. 나머지 셋 (그룹 친화도 · 이상적 프로세서 · XState) 은 `CreateThread` 길엔 안 나온다. 나올 수 있는 건 `lpAttributeList` 를 주는 호출자 (`CreateRemoteThreadEx` 를 직접 — `BasepConvertWin32AttributeList` 가 항목을 더 넣음, 본문 안 봄) 나 `NtCreateThreadEx` 를 직접 부르는 다른 코드 `[추측]` | test.exe 엔 **맞음**. 다섯을 다 받는 건 다른 exe 몫 (구현 범위는 사용자 결정 — run74 와 같음) | `+0x140 ~ +0x1bd`, `+0x1af JNZ` → `+0x59b` |
| `CreateFlags` | 가정 없음 | 바꾸는 줄은 `CREATE_SUSPENDED (0x4) → 0x1` 하나. **단 조건 셋 중 하나라도 참이면 `dwCreationFlags` 와 상관없이 1** — 멈춘 채 만들고, kernelbase 가 새 TEB 를 손본 뒤 `NtResumeThread` 로 직접 깨운다. test.exe: (가) 0 `[정적+샘플]` · (나) 0 `[관찰: 은퇴까지]` · (다) `[확인 필요]` (아래 ①) | 엔진은 **0 을 받는 게 보통**. 1 이 와도 새 TEB `SameTebFlags` 는 똑같이 커널이 안 쓴다 (아래 "보탠 것" ③). 1 이면 `NtResumeThread` syscall 이 하나 더 온다 | `+0x22e` · `+0x237` · `+0x245` · `+0x25b` · `+0x261` · `+0x26c` · `+0x274` · `+0x319 ~ +0x341` |
| 클래스 `0x29` 가 예약인가 | `[추측]` (본문 안 봄) | **예약 (`MEM_RESERVE`) 만 한다. 커밋은 안 한다** `[정적]`. 시작 주소 64KB 정렬, 크기는 4KB 올림 (`M` 은 이미 64KB 로 옴), 상한 `min(HighestUserAddress − 1, 0x7ffffffeffff)`, ZeroBits 지킴, 노드 ≤ `KeNumberNodes`. 시작 주소를 정보 칸 `+0x20` 에 돌려줌 | "현재 프로세스 유저 공간에 `[A, A+M)` 을 `MEM_RESERVE` 로 잡고 `A` 를 `+0x20` 에" 로 구현 (결과서 판단 그대로). 커밋·가드는 `RtlCreateUserStack` 이 `ZwAllocateVirtualMemory` 로 따로 (run74) | `NtSetInformationProcess +0x485 ~ +0x528`, `MmAllocateUserStack +0xcb` · `+0xd8` · `+0x12f`, `MiAllocateVirtualMemory +0x125 ~ +0x12f` · `+0x334` |
| 노드 (`R9` 위 8비트) | "보통 0" `[추측]` | 이상적 프로세서 속성 (`0x3000e`) 이 있을 때만 **노드 + 1** (1부터, 0 = 선호 없음). test.exe 는 그 속성이 없어 **0** `[정적]` | `R9 = 0x1000` 그대로 | `PspAllocateThread +0x18b` · `+0x1b9` · `+0x8bc ~ +0x8c7` |
| ntdll `RtlCreateUserStack` | 같은 꼴일 것 `[추측]` | 같은 꼴 (다른 점 넷 — 아래 ⑤ 표). 부르는 곳은 kernelbase `CreateFiberEx` 하나 — 둘째 스레드 길엔 **안 쓰인다** | 지금은 필요 없음. 파이버를 지원할 때만 | ntdll `RtlCreateUserStack.asm`, kernelbase `_refs/0x29ba60.tsv` |
| `ctx+0x10` 비트 0 | 읽는 곳 안 따라감 | 요청한 네 함수와 이웃 셋 안엔 **읽는 줄이 없다** (훑기 한계 — 넓게 훑기는 Q168 ②) | 지금은 흉내 안 해도 될 것 `[추측: 읽는 곳을 못 찾았을 뿐]` | `PspBuildCreateProcessContext +0x1876 ~ +0x1882` |

### 클래스 `0x29` 정보 칸 (길이 `0x28`)

`NtSetInformationProcess +0x485 ~ +0x528` 갈래가 읽는 꼴 `[정적]`. 칸 이름은 공개 헤더 꼴 `{PreferredNode, Reserved[3], ReserveSize, ZeroBits, StackBase}` 에 맞춘 `[추측: 이름]` (PDB 에 `PROCESSINFOCLASS` 도 이 구조체도 없다).

| 칸 | 뜻 | 검사 (어기면) | 줄 |
|---|---|---|---|
| `+0x0` (4B) | 노드 | `> 0x40` 이면 `0xC000000D` | `+0x4bf ~ +0x4c6` |
| `+0x4 ~ +0xf` | 비워 둔 칸 | 모두 0 이어야 (`0xC000000D`) | `+0x4cc ~ +0x4d8` |
| `+0x10` (8B) | 예약 크기 `M` | 0 이면 `0xC000000D` | `+0x4de` · `+0x4e2 ~ +0x4e8` |
| `+0x18` (8B) | ZeroBits | (0 이 아니면 아래에서 `MiValidateZeroBits`) | `+0x4fe ~ +0x50a` |
| `+0x20` (8B) | 출력 = 예약 시작 주소 `A` | 부르기 전에 0 으로 | `+0x4f6`, 복사 `+0xff1` · `+0xff5` |

- 그 밖 검사: 핸들은 **-1 (현재 프로세스) 만** (`+0x49f`), 길이는 `0x28` (`+0x4ac`) — `0x18` 이면 옛 꼴 `{+0 크기, +8 ZeroBits, +0x10 출력}` (노드 0, `+0xe0b`), 그 밖은 `0xC0000004`. 권한 검사는 없다.
- 유저 모드에서 부르면 (ntdll 쪽, 파이버 길) 칸이 8바이트 정렬이고 `칸 + 길이 ≤ 0x7fffffff0000` 이어야 하고 (`+0x429 ~ +0x44c`), 칸을 지역으로 복사해 읽은 뒤 출력만 사용자 칸에 다시 쓴다 (`+0x116d ~ +0x11a0`, `+0xff1`). 커널 `RtlCreateUserStack +0x104` 는 커널 모드라 이 복사·검사를 건너뛴다.

### PE_emulator 확인거리 (우리 대기열 아님)

- `RtlQueryInformationActivationContext` (진짜 ntdll 코드) 가 **실패하면 `CreateRemoteThreadEx` 는 스레드를 안 만들고 끝난다** (`+0x228 JS` → `+0x352` 실패 기록 `DbgPrint` → `+0x368 JMP +0x4d4`) `[정적]` (flow-writer `.asm`). 성공해서 돌려준 핸들이 0 이 아니고 그 플래그 비트 0 이 꺼져 있으면 CreateFlags 1 길 (`+0x245 ~ +0x24d`, `+0x4aa ~ +0x4b8`). 엔진에서 메인 스레드가 `CreateThread` 를 부를 때 이 함수가 무엇을 돌려주는지 보면 된다.
- 엔진의 `NtSetInformationProcess(0x29)` 가 커널 모드 호출 (커널 `RtlCreateUserStack`) 과 유저 모드 호출 (ntdll `RtlCreateUserStack`, 파이버) 을 둘 다 받는지 — 위 정보 칸 표와 검사 그대로.

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 사진 값은 run44 덤프 읽기 로그를 다시 읽었다 (flow-writer).
- 정적 (결과서 — 셋 다 기준 GUID 와 같음, 다시 analyze 안 함):
  - ntoskrnl pdb `C29EBFB06B78B3C020DCA66D99713F9E1` — depth 0 셋 새로 (`MmAllocateUserStack` · `MiAllocateVirtualMemoryPrepare` · `MiAllocateVirtualMemory`), 있던 `.asm` 읽기 (`NtSetInformationProcess` 클래스 `0x29` 갈래 · `PspBuildCreateProcessContext` · `NtCreateThreadEx` · `PspCreateThread` · `PspAllocateThread` · `PspInsertThread` · `PspDeleteCreateProcessContext` · `PspCallProcessNotifyRoutines`).
  - kernelbase pdb `72CCE506944DE0267C172AD64287F9BD1` — `CreateRemoteThreadEx` depth 1 (10개) · `CreateThread` depth 0 새로, refs 넷 (`RtlCreateUserStack` · `0x268d80` · `0x29ba60` · `KernelBaseGlobalData`).
  - ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1` — `RtlCreateUserStack` depth 0 새로, refs 하나.
  - kernel32 — `CreateThread` depth 0 새로 (pdb `AE9E9D8F7AC1467A645320FDC23235D01`, `ms/sym/kernel32.dll.pdb.info.txt` — flow.md 기준과 같음).
- 버전 대조 (결과서): ntoskrnl `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807 → PspCallProcessNotifyRoutines`, ntdll `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` · `_LdrpInitialize +0x167/+0x16c → LdrpInitializeProcess` 일치. 요청서의 rva·크기 셋 (`CreateRemoteThreadEx` `0xca3f0` 2126B · `NtSetInformationProcess` `0x9329b0` 19144B · ntdll `RtlCreateUserStack` `0xf5130` 709B) 도 맞음.

## 목적

- run74 (Q141a) 가 남긴 정적 꼬리 다섯 (Q142): ① 유저 `CreateRemoteThreadEx` 가 `NtCreateThreadEx` 에 주는 `CreateFlags` · 스택 인자 · `AttributeList` ② 커널 클래스 `0x29` 갈래가 정말 예약하나 ③ `ctx+0x10` 비트 0 을 누가 읽나 ④ `PspAllocateThread +0x8c7` 노드 갈래 조건 ⑤ ntdll `RtlCreateUserStack` 이 커널 것과 같은 꼴인가.
- ① 이 닫히면 대기열 Q42 의 동적 몫 "`CreateThread` 가 커널에 정말 (0, 0) 을 넘기나" 가 정적으로 닫힌다.

## 방법

- 정적: `_workspace/sast/77_thread-leftovers.md` — 위 "환경" 의 함수들. 지도는 새로 안 만들고 `_calls.tsv` · `refs` · `.asm` 으로 길을 찾음.
  - 점프 표 (`NtSetInformationProcess +0x25b ~ +0x284`) 와 PE import 표는 결과서가 읽기 전용 임시 스크립트로 풀었다 (세션 임시 폴더, 레포 밖) — 아래 "도구 메모".
- 결과서: `_workspace/sast/77_thread-leftovers_result.md` (**분석성공**, 커밋 `681bce0`).
- flow-writer 가 `.asm` 으로 다시 본 줄:
  - kernelbase `CreateRemoteThreadEx +0x118` · `+0x120` · `+0x127` · `+0x140` · `+0x14c` · `+0x170` · `+0x17c` · `+0x1a0` · `+0x1b5` · `+0x1b9` · `+0x1bd` · `+0x1c5` · `+0x1c8` · `+0x1d6 ~ +0x211` (`RtlQueryInformationActivationContext` 인자) · `+0x218 ~ +0x235` · `+0x237` · `+0x243` · `+0x245` · `+0x24d` · `+0x25b` · `+0x25f` · `+0x261` · `+0x26c` · `+0x274` · `+0x279` · `+0x280 ~ +0x2b1` · `+0x2e2` · `+0x2f7` · `+0x2ff` · `+0x307` · `+0x319` · `+0x321` · `+0x341 ~ +0x368` · `+0x36d ~ +0x38c` · `+0x3a5` · `+0x3b9` · `+0x3c8` · `+0x3e6` · `+0x41b` · `+0x432` · `+0x448` · `+0x46c` · `+0x473 ~ +0x4d3` · `+0x4e1` · `+0x506` · `+0x662` · `+0x6a7 ~ +0x832` 의 call 줄, `_calls.tsv` 의 이 함수 줄 전부.
  - kernel32 · kernelbase `CreateThread +0x11` · `+0x1c` · `+0x32` · `+0x36`, `_calls.tsv` 두 줄.
  - ntoskrnl `NtSetInformationProcess +0x25b` · `+0x284` · `+0x359` · `+0x35c` · `+0x485` · `+0x49f ~ +0x4e2` · `+0x4fe` · `+0x50e` · `+0x513` · `+0x51d` · `+0x520` · `+0xe0b` · `+0xff1` · `+0xff5` · `+0x116d` · `+0x119c`, `MmAllocateUserStack +0x9a` · `+0xcb` · `+0xd8` · `+0x12f` · `+0x134` · `+0x157 ~ +0x163`, `MiAllocateVirtualMemory +0x11c` · `+0x125` · `+0x12c` · `+0x12f` · `+0x31e` · `+0x334`, `MiAllocateVirtualMemoryPrepare +0x10d` · `+0x118` · `+0x1bf` · `+0x374` · `+0x490` · `+0x495` · `+0x4ba` · `+0x4c1` · `+0x517` · `+0x528` · `+0x560 ~ +0x567`, `PspAllocateThread +0x13b` · `+0x165 ~ +0x1b9` · `+0x245` · `+0x54a ~ +0x5a0` (`R12W` 셈) · `+0x5c6` · `+0x5d2` · `+0x66f` · `+0x673` · `+0x886` · `+0x88f` · `+0x894` · `+0x8bc ~ +0x8c7` · `+0xa03` · `+0xa06` (`[RBX+0x10]` 줄 0개도), `PspBuildCreateProcessContext +0x186f ~ +0x1882`, `PspDeleteCreateProcessContext +0x7d` · `+0x81`.
  - ntdll `RtlCreateUserStack +0x4d` · `+0x5e ~ +0x6a` · `+0x74` · `+0x85` · `+0x88` · `+0x9b` · `+0xa3` · `+0xb8` · `+0xc8` · `+0xdf` · `+0x113 ~ +0x123` · `+0x159` · `+0x164` · `+0x16f` · `+0x1db` · `+0x227` · `+0x248` · `+0x24b` · `+0x277`, `_calls.tsv` 다섯 줄. kernelbase `_refs/0x29ba60.tsv` (`CreateFiberEx+0xce`).
  - ntdll `CsrClientConnectToServer` 전부 (`+0x0 ~ +0x1e5`) — 아래 "보탠 것" ① (`KernelBaseGlobalData+4` 의 뜻).
  - `sample/bin/test.exe` PE 머리 (scratchpad 임시 스크립트, `python3 -I`): `Subsystem` = 3 (NT 헤더 `+0x5c`), import `KERNEL32.dll!CreateThread`.
  - 사진 값: run44 덤프 읽기 로그 (아래 "사진 대조").

## 결과

### 정적 — 호출 지점 (`ms/disassemble/<모듈>/_calls.tsv`)

| caller | call | ret | callee | 조건·뜻 | 표시 |
|---|---|---|---|---|---|
| `test!main` | — | — | `kernel32!CreateThread` | `CreateThread(NULL, 0, Worker, NULL, 0, NULL)`, import 는 `KERNEL32.dll` | [소스] `test.c:34` + PE import 표 |
| `kernel32!CreateThread` (rva `0x315c0`, 71B) | `+0x36` | `+0x3d` | `CreateRemoteThreadEx` | IAT `[0x18008ba50]` (`api-ms-win-core-processthreads-l1-1-0` — KERNELBASE 로 풀림은 `[추측]`). `hProcess = -1` (`+0x32`), `dwCreationFlags & 0x10004` (`+0x11`), `lpAttributeList = NULL` (`+0x1c`) | [정적] |
| `KERNELBASE!CreateThread` (rva `0x18b3d0`, 64B) | `+0x36` | `+0x3b` | `KERNELBASE!CreateRemoteThreadEx` | 직접. 몸은 kernel32 것과 같음 | [정적] |
| `KERNELBASE!CreateRemoteThreadEx` | `+0x211` | `+0x218` | `ntdll!RtlQueryInformationActivationContext` | 현재 프로세스 (`+0x1d9 CMP R12,-1`) 일 때 | [정적] |
| `KERNELBASE!CreateRemoteThreadEx` | **`+0x2e2`** | **`+0x2e9`** | **`ntdll!NtCreateThreadEx`** | IAT `[0x18029b990]`, 인자 11개 (아래 ① 표) | [정적] |
| `KERNELBASE!CreateRemoteThreadEx` | `+0x341` | `+0x348` | `ntdll!NtResumeThread` | 멈춘 채 만들었고 (`[RSP+0xf0] ≠ 0`) `CREATE_SUSPENDED` 가 없을 때만 | [정적] |
| `KERNELBASE!CreateRemoteThreadEx` | `+0x3b9` · `+0x3c8` · `+0x3e6` | `+0x3be` · `+0x3cd` · `+0x3eb` | `GetModuleHandleA("csrsrv")` · `GetProcAddress("CsrCreateRemoteThread")` · 간접 (CFG) | `KernelBaseGlobalData+4 ≠ 0` (서버 프로세스) 일 때만 (`+0x3a5`) | [정적] |
| `KERNELBASE!CreateRemoteThreadEx` | `+0x41b` · `+0x46c` | `+0x422` · `+0x473` | `RtlAllocateActivationContextStack` · `RtlActivateActivationContextEx` | 멈춘 채 + 활성화 문맥이 있고 NO_INHERIT 아닐 때. 새 TEB `+0x2c8` 에 저장 (`+0x448`) | [정적] |
| `KERNELBASE!CreateRemoteThreadEx` | `+0x4e1` | `+0x4e8` | `RtlReleaseActivationContext` | 활성화 문맥 핸들이 있을 때 (끝길) | [정적] |
| `KERNELBASE!CreateRemoteThreadEx` | `+0x662` | `+0x667` | `BasepConvertWin32AttributeList` | `lpAttributeList ≠ NULL` 일 때만 — `CreateThread` 는 아님 | [정적] |
| `nt!NtSetInformationProcess` | **`+0x50e`** | **`+0x513`** | **`nt!MmAllocateUserStack`** | 클래스 `0x29` 갈래 (`+0x485`, 점프 표로 이 클래스만 옴) | [정적] |
| `nt!MmAllocateUserStack` (rva `0x904d94`, 536B) | `+0xd8` | `+0xdd` | `nt!MiAllocateVirtualMemoryPrepare` | 할당 종류 `0x2000` (`MEM_RESERVE`) 하나 (`+0xcb`) | [정적] |
| `nt!MmAllocateUserStack` | `+0x12f` | `+0x134` | `nt!MiAllocateVirtualMemory` | 실행 | [정적] |
| `nt!MiAllocateVirtualMemoryPrepare` | `+0x374` | `+0x379` | `nt!MiValidateZeroBits` | ZeroBits ≠ 0 일 때 | [정적] |
| `nt!MiAllocateVirtualMemory` | **`+0x334`** | **`+0x339`** | **`nt!MiReserveUserMemory`** | `MEM_RESERVE` 비트 (`+0x12c TEST` · `+0x12f JNZ → +0x31e`). 본문 안 읽음 | [정적] |
| `nt!PspAllocateThread` | `+0x8bc` | `+0x8c1` | `nt!KeGetProcessorNodeNumber` | 이상적 프로세서 속성이 있을 때만 (`+0x1b9 JNZ → +0x886`) | [정적] |
| `ntdll!RtlCreateUserStack` (rva `0xf5130`, 709B) | `+0xa3` · `+0x16f` · `+0x1db` · `+0x227` · `+0x277` | `+0xa8` · `+0x174` · `+0x1e0` · `+0x22c` · `+0x27c` | `RtlImageNtHeaderEx` · `NtSetInformationProcess(0x29)` · `NtAllocateVirtualMemory` (커밋) · 같은 것 (가드) · `NtFreeVirtualMemory` | 커널 것과 같은 순서 (아래 ⑤) | [정적] |
| `KERNELBASE!CreateFiberEx` | `+0xce` | `+0xd5` | `ntdll!RtlCreateUserStack` | IAT `[0x18029ba60]` — ntdll 쪽의 유일한 caller | [정적] |

### ① `CreateRemoteThreadEx` 가 `NtCreateThreadEx` 에 주는 것

`CreateRemoteThreadEx` 안 `RSP` 는 진입 때보다 `0x708` 작다 (저장 7 + `SUB RSP,0x6d0`). 인자 이름은 공개 헤더 순서에 asm 자리를 맞춘 `[추측: 이름]`, 자리·상수는 `[정적]`.

| `NtCreateThreadEx` 인자 | test.exe 값 | 줄 | 닫힘 |
|---|---|---|---|
| 1 ThreadHandle | `&[RSP+0x100]` | `+0x2da` | 닫힘 |
| 2 DesiredAccess | `0x1fffff` | `+0x2d5` | 닫힘 |
| 3 ObjectAttributes | NULL (`lpThreadAttributes == NULL`, `+0x136`) | `+0x2d2` | 닫힘 |
| 4 ProcessHandle | -1 | `+0x2cf` (`CreateThread +0x32`) | 닫힘 |
| 5 · 6 StartRoutine · Argument | `Worker` · NULL | `+0x2c2 ~ +0x2ca` · `+0x2b5 ~ +0x2bd` | 닫힘 |
| 7 CreateFlags | **0** (조건 셋이 다 거짓일 때) / **1** (하나라도 참) | `+0x2b1` ← `+0x261` / `+0x274` | 조건 값만 동적 |
| 8 ZeroBits | 0 | `+0x2ac` | 닫힘 |
| 9 StackSize | **0** | `+0x2a7` | 닫힘 |
| 10 MaximumStackSize | **0** | `+0x2a2` | 닫힘 |
| 11 AttributeList | `&[RSP+0x210]`, `TotalLength 0x48`, 항목 2 | `+0x295` | 닫힘 |

- **거르기**: `dwCreationFlags` 에 `0x4` · `0x10000` 말고 다른 비트가 있으면 `0xC000000D` (`+0x120 TEST R14D,0xfffefffb` · `+0x127 JNZ`).
- **스택 인자**: `+0x1c8` 에서 `ESI` = RESERVATION 비트, `+0x280 ~ +0x291` 에서 비트가 없으면 (9, 10) = (`dwStackSize`, 0), 있으면 (0, `dwStackSize`). `dwStackSize = 0` 이면 어느 쪽이든 **(0, 0)**. 커널 `NtCreateThreadEx +0x15f/+0x16a` 는 그대로 복사하고 `RtlCreateUserStack` 이 0 인 쪽을 헤더로 바꾼다 (run74).
- **속성 목록** (`lpAttributeList == NULL` 일 때 — `CreateThread` 는 늘): `+0x140` `0x10003` · `+0x14c` Size `0x10` · 값 = ClientId 칸, `+0x170` `0x10004` · `+0x17c` Size `8` · 값 = 새 TEB 주소 칸, 둘 다 ReturnLength 0. `+0x1a0 ~ +0x1bd` `TotalLength = 2 × 0x20 + 8 = 0x48`. 커널 검사 (run74: `≥ 0x28`, `(len − 8) & 0x1f == 0`, Size, THREAD 비트) 를 통과한다.
- **CreateFlags 조건 셋** (`+0x22e ~ +0x24d`, 하나라도 참이면 `+0x26c` 표시 → `+0x274 ECX = 1`):
  - (가) `KernelBaseGlobalData+0x4` (kernelbase rva `0x3aeec4`) ≠ 0 — **서버 프로세스 표시** (뜻은 아래 "보탠 것" ①). test.exe **0** `[정적+샘플]`.
  - (나) TEB `+0x1720` (`SubProcessTag`) ≠ 0. test.exe 메인 스레드는 은퇴까지 0 `[관찰]` (아래 "사진 대조"). `main` 에서 `CreateThread` 까지 이 칸을 쓰는 코드가 있는지는 안 봤다 `[추측: 0 그대로]`.
  - (다) 지금 활성화 문맥 핸들 (`[RSP+0x118]`) ≠ 0 이고 그 플래그 비트 0 (NO_INHERIT 쯤 `[추측: 이름]`) 이 꺼짐. 은퇴 때 메인 스레드 활성화 문맥 스택은 비어 있다 `[관찰]`. 그래도 `RtlQueryInformationActivationContext` 가 무엇을 돌려주는지는 정적으로 못 정함 → 동적.
- **부른 뒤 (test.exe 길, 세 조건이 다 거짓)**: `+0x307 JNZ` 안 탐 → `lpThreadId` NULL 이라 `+0x30c JZ` → `+0x321 JZ +0x4d4` → 핸들 있으면 `RtlReleaseActivationContext` (`+0x4e1`) → 반환 = 스레드 핸들 (`+0x506`). **`NtResumeThread` · CSR 알림 · 활성화 없음.**
- **멈춘 채 길 (조건 참)**: `+0x36d ~ +0x38c` 메인 TEB `SubProcessTag` 를 새 TEB 에 복사, 활성화 문맥 스택을 새로 만들어 새 TEB `+0x2c8` 에 넣고 활성화, 서버 프로세스면 `csrsrv!CsrCreateRemoteThread` 간접 호출, `CREATE_SUSPENDED` 가 없으면 `+0x341 NtResumeThread`.

→ **Q42 동적 몫 "`CreateThread` 가 커널에 정말 (0, 0) 을 넘기나" 는 정적으로 닫혔다 (예).** 남은 건 CreateFlags 의 조건 (다) 뿐이다.

### ② 클래스 `0x29` — 정말 예약하나

- **갈래 찾기**: `+0x25b LEA EAX,[RDX-1]` ~ `+0x284 JMP RDX` 가 바이트 표 (rva `0x935914`) → 4바이트 표 (rva `0x935800`) 로 간다. 결과서가 PE 에서 풀어 **클래스 `0x29` → `+0x485`** (이 목적지로 오는 건 `0x29` 하나). 유저 모드 호출이면 앞 사다리 `+0x359 CMP EBX,0x29` → `+0x429` 에서 8바이트 정렬·범위 검사를 먼저 한다.
- **갈래 본문** (`+0x485 ~ +0x528`): 지역 40B 0 → 핸들 -1 · 길이 `0x28` 검사 → (유저 모드면 칸 복사) → 노드 · 비운 칸 · 크기 검사 → `+0x50e MmAllocateUserStack(출력 칸, ZeroBits, &크기, 노드, 파티션 0)` → 실패면 그대로 반환, 유저 모드면 출력을 사용자 칸에 복사 (`+0xff1/+0xff5`). 위 "정보 칸" 표.
- **예약만 한다** `[정적]`:
  - `MmAllocateUserStack +0xcb MOV dword [RSP+0x20],0x2000` — `MiAllocateVirtualMemoryPrepare` 의 할당 종류가 **`0x2000` (`MEM_RESERVE`) 하나**. `0x1000` (`MEM_COMMIT`) 비트 없음. 보호 `4` (`PAGE_READWRITE`, 파티션 0 일 때), 프로세스 -1 (`+0x9a`), 원하는 시작 0.
  - `MiAllocateVirtualMemory +0x125` 가 할당 종류를 읽고 `+0x12c TEST R13D(=0x2000),EAX` · `+0x12f JNZ → +0x31e` → **`+0x334 MiReserveUserMemory`** (ret `+0x339`). 커밋 쪽으로는 안 간다. `MiReserveUserMemory` 본문은 안 읽음 (이름만).
  - 성공하면 `MmAllocateUserStack +0x157 ~ +0x15c` 시작 주소 → 출력 칸, `+0x15f ~ +0x163` 실제 크기 → 크기 칸.
- **정렬**: `MiAllocateVirtualMemoryPrepare +0x10d MOV R8D,0x10000` → `+0x1bf` 칸 `+0x10` 에 정렬값 `0x10000` (시작 주소 0 · `MEM_RESERVE` 길). 크기는 `+0x4ba ~ +0x4c1` 4KB 올림, 시작 주소는 `+0x490 ~ +0x495` 64KB 내림. 크기를 64KB 로 올리는 줄은 여기 없다 — `RtlCreateUserStack` 이 이미 올려서 온다 (run74 다듬기 ④).
- **상한**: ZeroBits 0 이면 `min(_EPROCESS.HighestUserAddress (+0x368) − 1, 0x7ffffffeffff)` (`+0x517 ~ +0x528`). 0 이 아니면 `+0x374 MiValidateZeroBits` 뒤 `−1 >> ZeroBits`.
- **노드**: `+0x560 ~ +0x567` `KeNumberNodes` 보다 크면 `0xC000000D`. 시작 주소 고르기에 어떻게 쓰이는지는 `MiReserveUserMemory` 안이라 안 봄.
- **이름**: PDB (ntoskrnl · ntdll · kernelbase · kernel32) 에 `PROCESSINFOCLASS` 열거형도 `ProcessThreadStackAllocation` 글자도 없다 (결과서, `llvm-pdbutil` ENUM 4377개). 공개 헤더 이름은 `[추측: 이름]` 그대로.

### ③ `ctx+0x10` 비트 0 을 누가 읽나

- 쓰는 줄 (다시 확인): `PspBuildCreateProcessContext +0x186f CMP [RSP+0x90],ESI` · `+0x1876 SETNZ DL` (셋째 인자 ≠ 0 = 스레드 모드) · `+0x1879 ~ +0x1882` 비트 0 만 바꿔 저장. 이 함수 안 `[R12+0x10]` 은 이 두 줄뿐.
- **읽는 줄을 못 찾았다 (훑기 한계)** `[정적: 훑은 범위 안에서]`:

| 함수 | `ctx` 를 어떻게 쓰나 | `ctx+0x10` 읽는 줄 |
|---|---|---|
| `NtCreateThreadEx` | 주소만 넘김, 직접 읽는 칸은 `+0x18` 하나 (`+0x2a8`) | 없음 |
| `PspCreateThread` | NULL 검사 (`+0x15f`) 뒤 `PspAllocateThread` 4번째 · `PspInsertThread` 7번째로 넘기기만 | 없음 |
| `PspAllocateThread` | `+8` · `+0x14` · `+0x140` · `+0x148` · `+0x1e0` 만. `[RBX+0x10]` 은 파일 전체에 0줄 (flow-writer 다시 셈) | 없음 |
| `PspInsertThread` | `+0x20` · `+8` · `+0x14` · `+0x170`, 첫 스레드면 `+0x7f7` 에서 `PspCallProcessNotifyRoutines` 로 넘김 | 없음 |
| (이웃) `PspDeleteCreateProcessContext` | `+0x7d TEST byte [RBX+0x10],0x4` | **비트 2** 를 읽음 (비트 0 아님) |
| (이웃) `NtCreateUserProcess` | 비트 3 켜기 `+0xaa1`, 비트 4 읽기 `+0xb4d`, 비트 2 끄기 `+0xad1` · `+0xb55` | 비트 0 안 만짐 |
| (이웃) `PspCallProcessNotifyRoutines` | `+0x40` · `+0xb8` · `+8` · `+0xf0` · `+0xd8` · `+0x1a0` | 없음 |

- `ctx` 를 받는 더 아래 callee (`KeInitThread` · `KeStartThread` · `MmCreateTeb` 등) 는 안 읽었다. 넘기는 값이 `ctx` 전체가 아니라 `+0x140` · `+0x1e0` 같은 부분 포인터라 이 비트는 안 보일 것 `[추측]` (결과서).

### ④ `PspAllocateThread +0x8c7` 노드 갈래

- **"노드 ≠ 0" 갈래가 아니라 "이상적 프로세서 속성 (`0x3000e`) 이 있을 때" 타는 곳이다** `[정적]`.
- 들어가는 길: `+0x165 TEST RBX,RBX` (`ctx` NULL 이면 통째로 건너뜀) → `+0x170 MOV R8,[RBX+8]` (받은 속성 번호 비트) → `+0x174 BT R8,0xc` (그룹 친화도 `0x3000c`) / `+0x17f MOV ESI,0x4000` · `+0x18b TEST RSI,R8` (이상적 프로세서 `0x3000e`) → `+0x18e JNZ → +0xa03 MOV EAX,[RBX+0x14]` (프로세서 번호) · `+0xa06` 표 `KiProcessorBlock` 에서 그 프로세서의 `PRCB` → `+0x1b6 TEST RCX,RCX` · **`+0x1b9 JNZ → +0x886`**.
- 갈래 안: 친화도 속성이 없으면 (`+0x886 TEST RDX,RDX`) 친화도 비트를 지어 넣고 (`+0x88f BTS R8,0xc` · `+0x894`) 그 프로세서 그룹 전체를 친화도로 채움 (`+0x898 ~ +0x8b5`, 결과서). 친화도 속성도 있으면 그룹이 같고 마스크가 겹쳐야 통과 (`+0xa5d ~ +0xa87`, 아니면 `0xC0000030`).
- 합류점: `+0x8bc KeGetProcessorNodeNumber(PRCB)` (ret `+0x8c1`) → `+0x8c1 MOVZX EDX,AX` · `+0x8c4 ADD EDX,R14D` (`R14D = 1` — `+0x13b LEA R14D,[RDI+1]`, `+0x245 MOV R14D,EDI` 는 이 갈래 뒤) → **`+0x8c7 MOV [RSP+0x5c],EDX` = 노드 + 1**.
- 값이 가는 곳: `+0x5c6 MOV EBX,[RSP+0x5c]` · `+0x5d2 MOV [RSP+0x20],EBX` → `PspSetupUserStack` 5번째 → `R9 = 0x1000 | (값 << 56)` (run74) → `RtlCreateUserStack` 정보 칸 `+0` → `MmAllocateUserStack` `R9D` → `MiAllocateVirtualMemoryPrepare +0x560 ~ +0x567`. **값은 1부터, 0 = 선호 없음.**
- test.exe: 속성 둘 (①) 에 이상적 프로세서가 없다 → `RCX = 0` → 이 갈래 안 탐 → `[RSP+0x5c] = 0` (`+0x11d`) → `R9 = 0x1000`. run74 의 "보통 0 `[추측]`" 이 닫힘.

### ⑤ ntdll `RtlCreateUserStack` — 커널 것과 같은 꼴인가

**같은 꼴이다.** 0 인 쪽만 헤더로 (`CMOVZ`), 다듬기 ①~⑤, `NtSetInformationProcess(-1, 0x29, 칸, 0x28)` 예약, 커밋 `4`, 가드 `0x104` 3쪽, `INITIAL_TEB` 채우기, 실패 때 `MEM_RELEASE` — 순서·상수가 같다. 다른 점만:

| 항목 | 커널 `RtlCreateUserStack` (run74) | ntdll `RtlCreateUserStack` |
|---|---|---|
| exe 헤더 찾기 | 붙은 프로세스의 `_EPROCESS.SectionBaseAddress` → `RtlImageNtHeader` | **PEB `ImageBaseAddress`** (`+0x74` `GS:[0x60]` · `+0x9b [R13+0x10]`) → `RtlImageNtHeaderEx(1, …)` (`+0xa3`) |
| PE32 갈래 | 있음 (4바이트 칸) | **없음** — 8바이트 칸 `+0x68` · `+0x60` 만 (`+0xb8` · `+0xc8 CMOVZ`) |
| 5번째 인자 | 안 읽음 | **할당 단위 `g`** — 0 이거나 페이지보다 작으면 `0xC000000D` (`+0x5e ~ +0x6a`), 예약 올림을 `g` 로 (`+0x113 ~ +0x120`). 커널은 `0x10000` 고정 |
| 입력 검사 | 노드 `> 0x40` 만 | 노드 + **페이지 크기 0 거절** (`+0x4d`) + `g` 검사 둘 |
| 어느 PEB | 대상 프로세스 `EPROCESS.Peb` | **지금** 프로세스 (`GS:[0x60]`) — `MinimumStackCommit` `+0x123 [R13+0x318]` |
| 프로세스에 붙기 | 부른 쪽이 `KiStackAttachProcess` | 없음 (자기 프로세스, 핸들 -1) |
| 부르는 것 | `Zw*` (커널 안) | **`Nt*` (syscall)**: `+0x16f NtSetInformationProcess` · `+0x1db` · `+0x227 NtAllocateVirtualMemory` · `+0x277 NtFreeVirtualMemory` |

- **누가 부르나**: ntdll 안엔 부르는 코드가 없다 (`refs` = 데이터 참조 셋 — 내보내기 표 둘 · CFG 표 하나). kernelbase 는 **`CreateFiberEx +0xce`** (ret `+0xd5`, IAT `[0x18029ba60]`) 하나. System32 의 `.dll` · `.exe` 중 이 글자가 있는 건 `KernelBase.dll` · `ntdll.dll` 둘뿐 (결과서 `grep -l -a`).
- 그래서 **유저 쪽에서 스택을 직접 만드는 길 = 파이버 (`CreateFiberEx`)**. `CreateThread` 길은 크기 둘만 넘기고 커널 `RtlCreateUserStack` 이 만든다. `RtlCreateUserThread` 도 이 함수를 안 부른다.

### 호출 관계

```
test!main                                                       [소스] test.c:34
 └─ kernel32!CreateThread                                       (IAT KERNEL32.dll)
      └─ +0x3d  CreateRemoteThreadEx  (api-ms-win-core-processthreads → KERNELBASE [추측])
           │  +0x120  dwCreationFlags & ~0x10004 ≠ 0 → 0xC000000D
           │  +0x140 ~ +0x1bd  속성 2칸 (0x10003 · 0x10004), TotalLength 0x48
           ├─ (1) +0x218  ntdll!RtlQueryInformationActivationContext     실패면 +0x228 JS → 스레드 안 만들고 끝
           │  +0x22e · +0x237 · +0x245  조건 셋 → 하나라도 참이면 +0x274 CreateFlags = 1
           │  +0x280 ~ +0x291  StackSize · MaximumStackSize = (0, 0)  (dwStackSize = 0)
           ├─ (2) +0x2e9  ntdll!NtCreateThreadEx  ── syscall ──▶  nt!NtCreateThreadEx (run74)
           │                                                            └─ … PspAllocateThread
           │                                                                 │  +0x1b9  이상적 프로세서 속성 있으면 → +0x886
           │                                                                 │     └─ +0x8c1 KeGetProcessorNodeNumber → +0x8c7 노드+1
           │                                                                 └─ +0x5ef PspSetupUserStack → +0x8e RtlCreateUserStack
           │                                                                      └─ +0x109 ZwSetInformationProcess(0x29)
           │                                                                           ⇢ NtSetInformationProcess +0x485 갈래
           │                                                                              └─ +0x513 MmAllocateUserStack
           │                                                                                   ├─ (1) +0xdd  MiAllocateVirtualMemoryPrepare  (0x2000, 정렬 0x10000)
           │                                                                                   └─ (2) +0x134 MiAllocateVirtualMemory
           │                                                                                        └─ +0x339 MiReserveUserMemory  (본문 안 봄)
           ├─ (3) +0x348  ntdll!NtResumeThread                   멈춘 채 만든 경우만
           └─ (4) +0x4e8  ntdll!RtlReleaseActivationContext      핸들 있을 때

KERNELBASE!CreateFiberEx
 └─ +0xd5  ntdll!RtlCreateUserStack                              (ntdll 쪽 유일 caller)
      ├─ (1) +0xa8   RtlImageNtHeaderEx                          PEB ImageBaseAddress
      ├─ (2) +0x174  NtSetInformationProcess(-1, 0x29, 칸, 0x28)
      ├─ (3) +0x1e0  NtAllocateVirtualMemory                     커밋
      ├─ (4) +0x22c  NtAllocateVirtualMemory                     가드 3쪽
      └─ (실패) +0x27c  NtFreeVirtualMemory
```

- `ZwSetInformationProcess` → `NtSetInformationProcess` 사이 (`⇢`) 는 커널 안 시스템 콜 길이라 줄을 안 봤다 `[추측: Zw 스텁 → 서비스 표]`. 이전 모드가 커널이라 `NtSetInformationProcess` 가 칸 복사·검사를 건너뛴다는 건 `[정적]` (`+0x4b6 TEST DIL,DIL`).

### 사진 대조 (run44 덤프 읽기 로그, 새 실행 없음 — flow-writer)

- 둘째 스레드는 `main` 뒤라 사진 범위 밖이다. 같은 함수를 쓰는 첫 스레드와 메인 스레드 칸만 봤다.
- `[관찰]` 첫 스레드 예약 시작 `DeallocationStack` = `0x9b2fc00000` (`ms/logs/04_baggage-read_K2.txt:1856`) — 아래 16비트가 0, **64KB 정렬**이다. `MiAllocateVirtualMemoryPrepare` 정렬 `0x10000` 과 어긋나지 않는다 (한 번 본 값이라 "늘 그렇다" 는 아님).
- `[관찰]` 메인 스레드 TEB `SubProcessTag` (`+0x1720`) = (null) — K2 (`_K2.txt:1866`) · P2 (`_P2.txt:2750`). 조건 (나) 는 은퇴까지 거짓.
- `[관찰]` 메인 스레드 활성화 문맥 스택 (TEB `+0x2c8` → `0x9b2fa67290`) 첫 8B (`ActiveFrame`) = 0, 틀 목록 빈 리스트 (`_P2.txt:4567 ~ 4569`). 은퇴 때 올린 틀이 없다. 조건 (다) 의 실제 값은 이것만으로는 못 정함 (`RtlQueryInformationActivationContext` 가 틀이 없을 때 무엇을 돌려주는지 안 봄).

## 확인된 것

- `[정적]` 유저 쪽 사슬: `kernel32!CreateThread +0x36` (ret `+0x3d`) → `KERNELBASE!CreateRemoteThreadEx` → `+0x2e2` (ret `+0x2e9`) `ntdll!NtCreateThreadEx`. run11 · run74 의 `[추측]` "유저 `CreateThread` 가 `NtCreateThreadEx` 로 온다" 가 코드로 맞다 (kernel32 IAT → KERNELBASE 풀이만 `[추측]`).
- `[정적]` test.exe 는 커널에 스택 (0, 0) · ZeroBits 0 · 속성 2개 (`TotalLength 0x48`) · 핸들 -1 · 접근 `0x1fffff` · `ObjectAttributes` NULL 을 넘긴다. CreateFlags 는 조건 셋이 다 거짓이면 0, 아니면 1.
- `[정적]` `kernel32!CreateThread` · `KERNELBASE!CreateThread` 는 `dwCreationFlags` 에서 `0x4` · `0x10000` 만 남기고, `lpAttributeList` 는 늘 NULL.
- `[정적]` 클래스 `0x29` = `NtSetInformationProcess +0x485` → `MmAllocateUserStack` → `MiAllocateVirtualMemory` → `MiReserveUserMemory`. 할당 종류는 `MEM_RESERVE` 하나, 시작 64KB 정렬. 정보 칸은 `+0` 노드 · `+0x10` 크기 · `+0x18` ZeroBits · `+0x20` 출력 (run74 와 같음).
- `[정적]` `PspAllocateThread +0x8c7` 의 값은 이상적 프로세서 속성이 있을 때 노드 + 1, 없으면 0. test.exe 는 0.
- `[정적]` ntdll `RtlCreateUserStack` 은 커널 것과 같은 계산을 하고 `Nt*` syscall 로 부른다. 부르는 곳은 kernelbase `CreateFiberEx` 하나.
- `[정적: 훑은 범위 안에서]` `ctx+0x10` 비트 0 을 읽는 줄은 `NtCreateThreadEx` · `PspCreateThread` · `PspAllocateThread` · `PspInsertThread` · `PspDeleteCreateProcessContext` · `NtCreateUserProcess` · `PspCallProcessNotifyRoutines` 에 없다.
- `[관찰]` + `[정적]` 첫 스레드 예약 시작 `0x9b2fc00000` 은 64KB 정렬 (위 "사진 대조").

## 결과서와 다르게 적은 곳 · 보탠 것

- ① (보탬, 결과서 `[추측]` → `[정적]`) **`KernelBaseGlobalData+0x4` 의 뜻 = "이 프로세스가 csrss 같은 서버 프로세스인가"**. 결과서는 "`CsrClientConnectToServer` 5번째 인자 (서버끼리 호출 표시) — 서버가 아니면 0 일 것 `[추측]`" 으로 뒀는데, 앞 바퀴 둘이 이미 답했다:
  - run68 (sast/63 질문 4): kernelbase 가 5번째 인자로 지역 `&[RSP+0x30]` 을 넘기고 (`_KernelBaseBaseDllInitialize +0x2c4`, 앞서 `+0x29` 에서 0), 돌아와서 `+0x326 CMP` · `+0x337 SETNZ` 로 이 칸에 옮긴다 `[정적]`.
  - run85 (sast/82 질문 1(a)): ntdll `CsrClientConnectToServer` 가 그 칸을 **클라이언트 길이면 0 (`+0xc7 MOV byte [RCX],0`), 서버 길이면 1 (`+0x1cb MOV byte [RAX],R15B`, `R15 = 1`)** 로 쓴다 `[정적]`. 서버 길은 `CsrClientProcess == 0` — exe `Subsystem` (NT 헤더 `+0x5c`) 이 1 (NATIVE) 일 때 (`+0x81 CMP` · `+0x86 JZ` → `+0x1df` → `+0x97 JZ +0x137`) 또는 이미 `CsrServerApiRoutine` 이 선 csrss 안 (`+0x125`). flow-writer 가 `CsrClientConnectToServer.asm` 으로 다시 봄.
  - test.exe `Subsystem` = 3 (`sample/bin/test.exe` 직접 읽음, run68 결과서와 같음) → 클라이언트 길 → **0** `[정적+샘플]`. 그래서 조건 (가) 는 test.exe 에서 거짓이 정적으로 닫힌다.
  - 이름 (`CalledFromServer` · `ServerToServerCall`) 은 공개 헤더 맞춤이라 `[추측: 이름]` 그대로. 짐 지도 7.6.8 `KernelBaseGlobalData +0x4` 줄은 이미 `[정적]` 이었고, 이번에 읽는 곳 (`CreateRemoteThreadEx +0x22e` · `+0x3a5`) 을 붙였다.
- ② (정정 — 요청서) 요청서의 "정보 칸 `+8` 크기" 는 틀렸다. **크기는 `+0x10`** (`NtSetInformationProcess +0x4de ADD R15,0x10` · `+0x4e2`, ntdll `+0x14f [RSP+0x68]` = 칸 `+0x10`). run74 문서 · flow.md · 짐 지도는 처음부터 `+0x10` 이라 고칠 곳이 없다 (요청서만의 오기).
- ③ (보탬) **CreateFlags 가 1 이어도 새 TEB `SameTebFlags` 는 커널이 안 쓴다** `[정적]`. `PspAllocateThread +0x54a ~ +0x5a0` 이 `R12W` 를 만들기 플래그의 `0x40` · `0x2` · `0x80` · `0x100` 비트로만 만든다 — `0x1` (멈춤) 은 안 들어간다. 그래서 run74 의 "`+0x66f/+0x673` 안 탐 → `+0x793` 건너뜀" 은 CreateFlags 0 · 1 둘 다 맞다.
- ④ (보탬) `RtlQueryInformationActivationContext` 가 실패하면 (`+0x228 JS`) `NtCreateThreadEx` 를 안 부른다 (`+0x352 DbgPrint` → `+0x368 JMP +0x4d4`). 결과서 (e) 표엔 없던 길. 함수 안 `DbgPrint` 셋 (`+0x363` · `+0x4a3` · `+0x4ce`) 은 모두 실패 기록 — 앞이 `+0x228 JS` · `+0x483 JS` · `+0x432 JS`.
- ⑤ (보탬) `Subsystem` · import 표: test.exe 는 `CreateThread` 를 `KERNEL32.dll` 에서 가져온다 (PE import 표) — 그래서 유저 사슬 첫 칸이 `kernel32!CreateThread` 다.
- ⑥ (보탬, 사진) 위 "사진 대조" 셋 — 결과서엔 없음.
- 결과서 "요청서와 다르게 한 것" (범위 밖으로 `MiAllocateVirtualMemoryPrepare` · `MiAllocateVirtualMemory` · `CreateThread` 둘 · refs 를 더 뽑음) 은 그대로 받아 썼다.

## 아직 확인 필요

**동적 — 대기열 Q42** (run74 동적 1 ~ 6 이 있는 행, 같은 실행. 대상은 test.exe 둘째 스레드 하나. 이번 묶음은 VM 안 함). 공통 조건: 현재 프로세스 = test.exe, 그리고 StartRoutine = `test!Worker` 로 둘째 스레드만.

1. **CreateFlags 가 0 인가 1 인가** — 남은 건 조건 (다) 뿐 ((가) 는 0 `[정적+샘플]`, (나) 는 은퇴까지 0 `[관찰]`). `KERNELBASE!CreateRemoteThreadEx+0x218` (ret) 에서 `r eax` · `dq @rsp+0x118 L2` (핸들 · 플래그), `+0x22e` 에서 `db kernelbase+0x3aeec4 L1` · `dq @$teb+0x1720 L1` (둘 다 0 기대).
2. `NtCreateThreadEx` 에 가는 인자 전부: `KERNELBASE!CreateRemoteThreadEx+0x2e2` 에서 `r ecx` (CreateFlags) · `dq @rsp+0x20 L7` (인자 5 ~ 11) · `dd poi(@rsp+0x50) L1` (`0x48`) · `dq poi(@rsp+0x50)+8 L8` (항목 둘). `nt!NtCreateThreadEx+0x70` 에서 `r13d` · `rdi` 와 맞나.
3. `MmAllocateUserStack` 결과 모양: `nt!NtSetInformationProcess+0x485` (서면 `ebx == 0x29`) `r10` · `r13d` · `dq @r15 L5`, `nt!MmAllocateUserStack+0xd8` `dd @rsp+0x20 L1` (`0x2000`) · `dd @rsp+0x28 L1` (4), `+0x134` `dq @r15 L1` (`A` — 64KB 정렬인가) · `dq @r14 L1` (크기 `M`) + `!address`.
4. `nt!PspAllocateThread+0x886` 은 **안 서야** 맞다 (이상적 프로세서 속성 없음). 서면 `rcx` (`PRCB`) · `dq @rbx+8 L1`.

- run74 동적 중 정적으로 닫힌 몫: 3 의 "유저가 (0, 0) 을 넘겼나" (예), 5 의 "예약인가" (예 — `A` 모양만 남음), 6 의 "`AttributeList` 가 NULL 인가 · 항목 수" (NULL 아님, 2). 멈춤점 자체는 값 대조용으로 그대로 둔다.

**정적 — 대기열 Q168** (오케스트레이터 결정 — ① 은 선택, ② 는 넓게 훑기):

- ① `MiReserveUserMemory` 본문 — 시작 주소를 어떻게 고르나 (아래에서 위 / 위에서 아래, 64KB 칸 고르기), 노드 선호를 어떻게 쓰나. 동적 3 이 이상하게 나올 때만.
- ② (질문 ③ 의 남은 `[공백]`) `ctx+0x10` 비트 0 을 읽는 곳 — 이번 훑기 범위 밖을 넓게: `KeInitThread` · `KeStartThread` · `MmCreateTeb` 등 `ctx` 를 받는 callee, 또는 ntoskrnl 전체에서 `[reg+0x10]` 바이트 비트 0 을 시험하는 줄.
- (`CsrClientConnectToServer` 5번째 인자는 위 "보탠 것" ① 로 닫혀서 Q168 에 안 넣음.)

## flow.md · 짐 지도에 반영한 것

- flow.md (그림은 안 고침 — 둘째 스레드는 `main` 뒤라 지도 범위 밖, run74 와 같음):
  - 기준: ntkrnlmp · kernelbase · kernel32 · ntdll pdb 줄에 run90 몫, 근거 로그 run90 줄.
  - 흐름도 앞 글: run90 문단 하나 (그림 안 고침).
  - 2.1: 머리 run 목록, `PspBuildCreateProcessContext` 절 (`ctx+0x10` 비트), `PspAllocateThread` 절 (노드 갈래), `PspSetupUserStack → RtlCreateUserStack` 절 4 (예약 `[추측]` → `[정적]`).
  - 2.5: 머리 run 목록, csrss 연결 절에 `KernelBaseGlobalData +0x4` 의 뜻 · 읽는 곳.
  - 3장: Worker 길 `[추측]` 두 곳에 유저 사슬 `[정적]`, "둘째 스레드를 만드는 커널 길" 절 머리 · 나무 첫 줄 · 스택 줄 · 동적 줄, 새 하위 절 "유저 쪽 — `CreateThread` → `CreateRemoteThreadEx`".
  - 4장: run74 동적 줄 · 정적 줄에 run90 결과, run90 새 줄 셋 (동적 Q42 · 정적 Q168 · PE_emulator 쪽 · 다르게 적은 곳).
  - 5장: run 표 · 03_sprint 요청서 번호 run90 줄.
- 짐 지도 (짐 지도 바퀴 — **새 짐 칸 없음, 칸 수 · 2장 숫자 그대로**. 주제가 둘째 스레드라 은퇴 뒤, 범위 밖):
  - 3.3 `DeallocationStack` ① 줄: `A` 출처 `[추측: 예약]` → `[정적]` (`MmAllocateUserStack` → `MiReserveUserMemory`, 64KB 정렬), 근거 칸 `sast77`.
  - 3.1 `MinimumStackCommit` ① 줄: 읽는 곳에 ntdll `RtlCreateUserStack +0x123` (파이버 길) 더함.
  - 7.6 C8 유저 스택 줄: 예약 확정 · 둘째 스레드 (0, 0) 한 마디.
  - 7.6.8 `KernelBaseGlobalData +0x4` 줄: 뜻 (서버 프로세스 표시) · 1 이 되는 길 · 읽는 곳.
  - 기준 run90 줄 · 판 목록 (마흔여섯째판), 8장 두 표.

## 디버거 메모 / 도구 메모

- 정적만이라 실행 메모 없음. 새 함정 없음.
- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 안에서 디버거 여부를 보는 줄은 없다 `[정적: 읽은 줄 안에서]`. (`CreateThread` 는 `NtCreateThreadEx` CreateFlags `0x4` (디버거에서 숨김) 를 안 켠다.)
- 도구 (결과서 "스크립트 문제" 1): `.asm` 에 **점프 표 목적지가 안 나온다** — `NtSetInformationProcess +0x25b ~ +0x284` 의 클래스 → 목적지를 결과서가 임시 스크립트로 PE 에서 풀었다. 제안 "`.asm` 머리에 점프 표 → 목적지 줄" 은 하네스라 오케스트레이터 · 사용자 검토 몫.
- 도구 (결과서 "스크립트 문제" 2): `refs <바이너리> <이름>` 이 **import 이름에는 엉뚱한 칸** (`.rdata` 이름 표 쪽 `0x268d80`) 을 돌려준다 — 진짜 IAT 칸 (`0x29ba60`) 은 PE import 표로 따로 찾았다. 제안 "import 이름이면 IAT 칸을 찾아 주기" 도 하네스 몫.

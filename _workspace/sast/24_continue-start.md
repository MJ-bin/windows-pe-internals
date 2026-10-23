# 24 continue-start — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run29)
- 대기열: Q17 (`_workspace/plan.md`) — 2.10 로더 끝 → 진입점: `LdrInitializeThunk` 뒤 `NtContinue`, `RtlUserThreadStart` callee
- 관련: flow.md 그림 2 `PH4` ("로더 끝 → 진입점", `q1` `ntdll!NtContinue` → `r1` `ntdll!RtlUserThreadStart` 화살표 "커널 NtContinue 안은 공백 (추측)"), 2.4 `LdrInitializeThunk` 절, 2.10 전체, 4.1 / 이전 요청 sast/11 (run16, `LdrInitializeThunk +0x18 NtContinue(맡긴 문맥, TRUE)`, `PspInitializeThunkContext`), sast/14·15 (run19·run20, `Kernel32ThreadInitThunkFunction` 칸), sast/20 (run25, `BaseThreadInitThunk` 본문)

## 범위

- 대상 (depth 1):
  - ntoskrnl `NtContinue` (rva `0x6adde0`, **5바이트** — 아마 `NtContinueEx` 로 넘기는 조각, 줄만 보면 됨)
  - ntoskrnl `NtContinueEx` (rva `0x6addf0`, 710바이트)
  - ntoskrnl `KiContinueEx` (rva `0x4241b0`, 512바이트)
  - ntoskrnl `KiContinuePreviousModeUser` (rva `0x2bc750`, 1226바이트)
  - ntdll `RtlUserThreadStart` (rva `0xacac0`, 91바이트)
  - (작음) ntdll `NtContinue` (rva `0x161610`, 24바이트) — SYSCALL 번호만
- 위 ntoskrnl 넷은 `NtCreateUserProcess` 지도 밖이다 (지도엔 `KxContextToKframes` 깊이 4, `KiVerifyContextIpForUserCet` 깊이 5 만 있음). 이름으로 직접 뽑으면 된다. 길 찾기가 필요하면 `run_ghidra.sh map` 을 `NtContinueEx` 뿌리로 돌려도 좋다 (그러면 이유를 결과서에).
- callee 본문은 읽지 않는다. 예외:
  - `KeContextToKframes`·`KxContextToKframes` 는 이미 뽑혀 있다 (run11·run16). 새로 읽지 말고, 필요하면 **"CONTEXT 의 `Rip`·`Rcx`·`Rsp` 가 트랩 프레임으로 가는가"** 를 run16 결과로 답한다.
  - 시스템 호출 출구(`KiSystemCall64`, `KiSystemServiceExit`, `KiServiceExit*` 등)는 **본문을 읽지 않는다.** 대상 함수 안에서 "출구가 트랩 프레임을 전부 되살리게 만드는 표시"(스레드 칸·플래그·특별한 반환 경로)가 보이면 그 칸 이름과 줄만 적는다. 출구 쪽 이름이 필요하면 `pdb_aliases.py --where ntoskrnl <이름>` 으로.
- 오프셋은 call·ret 둘 다. 점프 목적지는 `절대 주소 − 함수 시작` 으로 환산하고, 다 쓴 뒤 `.asm` 과 다시 대조.
- `__except`/`__finally` 착지점 안의 호출도 놓치지 않게 (unwind 범위 표로 확인).
- 구조체 칸은 PDB 타입으로 (`_CONTEXT`, `_KTRAP_FRAME`, `_KTHREAD`, `_ETHREAD`, `_KCONTINUE_ARGUMENT` 같은 것이 있으면). PDB 에 없는 칸은 오프셋+역할로.

## 알고 싶은 것

1. **커널 입구: `nt!NtContinue` → `NtContinueEx`**
   - `NtContinue`(5바이트)는 `NtContinueEx` 로 그대로 넘기나 (예/아니오). 둘째 인자(유저가 준 `TRUE` = TestAlert)가 `NtContinueEx` 에서 어떤 모양이 되나 (BOOLEAN 그대로 / 구조체 / 플래그 비트).
   - `NtContinueEx` 는 이전 모드가 **유저**일 때 어느 함수로 가나 (`KiContinuePreviousModeUser` 인가, `KiContinueEx` 인가, 둘 다인가). 갈림 줄과 조건.
2. **문맥 → 트랩 프레임**
   - 유저가 준 `CONTEXT`(로더가 유저 스택에 맡겨 둔 것, `Rip = RtlUserThreadStart`, `Rcx` = exe 진입점 — run16) 를 **어느 함수, 어느 줄**에서 트랩 프레임에 옮겨 적나. run16 이 이름만 본 후보: `KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0` → `KeContextToKframes` `[추측]`. 맞나, 둘 중 어느 쪽이 유저 길인가.
   - 옮기기 전 검사: `CONTEXT` 를 커널로 복사·검사(Probe)하는 곳, `ContextFlags` 로 바꾸는 범위를 줄이나, `Rip` 검사(`KiVerifyContextIpForUserCet` 등, CET 일 때만?) 가 있나. 각 검사 실패 시 가는 곳.
   - CET: run16 `PspInitializeThunkContext +0x295 KePopulateContinuationContext(원래 Rip)` 가 그림자 스택에 넣어 둔 값을 이 길에서 **쓰나 / 맞춰 보나** (예/아니오, 줄).
3. **TestAlert (`TRUE`)**
   - 둘째 인자가 참일 때 하는 일 — 경보 검사(`KeTestAlertThread` 류) 를 부르나, 유저 APC 를 배달하게 만드나. 함수 이름과 줄.
   - test.exe 시동 길에서 이때 배달될 유저 APC 가 있을지는 `[추측]` 으로만 (정적으로 정해지지 않으면 "동적").
4. **유저로 돌아가는 법**
   - `NtContinueEx` 는 성공하면 **보통 돌아가는 것처럼 반환하나**, 아니면 특별한 출구로 바로 빠지나. 시스템 호출의 보통 출구는 휘발성 레지스터(`rcx` 등)를 다 되살리지 않는데, 여기서는 `Rcx` = exe 진입점이 살아서 유저로 가야 한다 — 그걸 보장하는 표시·경로가 대상 함수 안에 보이나 (칸 이름, 줄). 안 보이면 "출구 쪽 (범위 밖)" 이라고만.
   - 실패하면 무엇을 돌려주나. 그때 ntdll `LdrInitializeThunk +0x1a` (ret `+0x1f`) `RtlRaiseStatus` 로 간다 (run16).
5. **`ntdll!RtlUserThreadStart` (91바이트)**
   - 받는 인자: `rcx` = 시작 주소(exe 진입점), `rdx` = ? (run16: 처음 문맥의 `Rdx` = PEB). 맞나.
   - `+0x7` 에서 전역 `Kernel32ThreadInitThunkFunction` 을 읽은 뒤 (run19 `refs`): 값이 있으면 그걸 **어떤 인자로** 부르나 — run25 `BaseThreadInitThunk(ECX, RDX = 시작 주소, R8 = 파라미터)` 와 맞게 `(0, rcx, rdx)` 인가. 호출 방식 (직접 / CFG 간접), call·ret 줄. **ret 이 `+0x2c` 인가** (01_sprint Worker 종료 콜스택 `RtlUserThreadStart+0x2c` `[관찰]`).
   - 값이 0 이면 가는 다른 갈래 (시작 주소를 직접 부르나, 다른 함수를 부르나). 이름과 줄.
   - 예외 처리(`__except` 필터 — 처리 안 된 예외 필터 이름), 돌아오면 무엇을 하나 (돌아오지 않는 설계인가).
6. **ntdll `NtContinue` 스텁**: SYSCALL 번호 (`MOV EAX, imm`). 그 번호가 ntoskrnl 서비스 표의 `NtContinue` 칸인지는 쉽게 확인되면만 (아니면 생략).
7. **그림·관찰과 맞추기**
   - 그림 2 `PH4` 의 `q1 ntdll!NtContinue ==> r1 ntdll!RtlUserThreadStart` "커널 NtContinue 안은 공백 (추측)" 을 코드 사슬로 채우면: `LdrInitializeThunk +0x18` → (SYSCALL) `nt!NtContinue` → `NtContinueEx +?` → `Ki...+?` → (트랩 프레임 `Rip` = `RtlUserThreadStart`, `Rcx` = 진입점) → (출구) → `RtlUserThreadStart` → `+?` `Kernel32ThreadInitThunkFunction` (= `BaseThreadInitThunk`, run20·run25) → `BaseThreadInitThunk +0x12` (ret `+0x17`) → 진입점(`test!mainCRTStartup`). 빈칸 `?` 을 채워 한 줄로.
   - `main` 에서 `k` 를 찍었을 때 예상 콜스택 (아래부터): `RtlUserThreadStart+?` → `BaseThreadInitThunk+0x17` → `mainCRTStartup+?` → `main`. CFG 중개(`_guard_dispatch_icall` 류)가 JMP 뿐이라 프레임에 안 남는지도 같이 (run28 결과서와 같은 식).
   - flow.md 2.10 `[추측]` 두 줄 각각 맞음 / 틀림 / 여전히 동적:
     - "로더 일이 끝나면 `ntdll!NtContinue` 로 실행이 `ntdll!RtlUserThreadStart` 로 넘어간다"
     - "`RtlUserThreadStart` → `KERNEL32!BaseThreadInitThunk` → `test!mainCRTStartup` 순서로 불린다" (`BaseThreadInitThunk` → `mainCRTStartup` 칸은 Q18 몫이니 `RtlUserThreadStart` → `BaseThreadInitThunk` 까지만)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtContinue$` | 1 | 질문 1 (5바이트) |
| ntoskrnl | `^NtContinueEx$` | 1 | 질문 1·2·3·4 |
| ntoskrnl | `^KiContinueEx$` | 1 | 질문 2·4 |
| ntoskrnl | `^KiContinuePreviousModeUser$` | 1 | 질문 2·3·4 |
| ntdll | `^RtlUserThreadStart$` | 1 | 질문 5 |
| ntdll | `^NtContinue$` | 0 | 질문 6 (SYSCALL 번호만) |

## 지도에서 본 것

- ntdll (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`):
  - `NtContinue` rva `0x161610`, 24바이트, 깊이 1, caller 2, 상태 `syscall` ("함수 안에 SYSCALL: 커널로 넘어감") — 정적 길이 여기서 끊긴다.
  - `RtlUserThreadStart` 는 지도에 없다 (CALL/JMP 참조 0 — sast/05). `_index.tsv` 엔 rva `0xacac0`, 91바이트.
  - `LdrInitializeThunk` rva `0xe1420`, 32바이트, 깊이 0.
- ntoskrnl (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*`): 대상 넷 모두 지도 밖. 지도 안 관련 함수: `KxContextToKframes` (깊이 4, caller `KiInitializeContextThread +0x1ac`), `KiVerifyContextIpForUserCet` (깊이 5, caller `KeVerifyContextRecord +0xe7`, `KeVerifyContextIpForUserCet +0x6e`).
- `_index.tsv` 에서 이름 확인 (ntoskrnl): `NtContinue` 5B, `NtContinueEx` 710B, `KiContinueEx` 512B, `KiContinuePreviousModeUser` 1226B, `KeContextToKframes` 236B, `KxContextToKframes` 1381B, `KiVerifyContextIpForUserCet` 451B, `KiRestoreSetContextState` 611B, `ZwContinue` 30B. ntdll: `NtContinueEx` 24B 도 있음 (이번 길엔 `NtContinue` 를 부름 — run16).

## 이미 아는 것

- `[정적]` (run16) `LdrInitializeThunk`: `+0x13` (ret **`+0x18`**) `NtContinue(RCX = 맡긴 문맥, DL = 1)`. 돌아오면 `+0x1a` (ret `+0x1f`) `RtlRaiseStatus`.
- `[정적]` (run16) 맡긴 문맥 = 첫 스레드의 처음 문맥: `Rip = RtlUserThreadStart` (`PspCreateUserContext +0x28`, `PsNtdllExports+0x10`), `Rcx` = exe 진입점 (`TransferAddress`), `Rdx` = PEB. `PspInitializeThunkContext` 가 이걸 유저 스택에 맡기고 트랩 프레임 `Rip` 를 `LdrInitializeThunk` 로 바꿔 끼움. `+0x295 KePopulateContinuationContext(원래 Rip)` — CET 일 때만 일함, 목적 `[추측]` "NtContinue 로 돌아갈 때 검사 맞추기".
- `[정적]` (run16, 원본 바이트 훑기만) `KeContextToKframes` 를 부르는 곳에 `KiContinuePreviousModeUser +0x422`, `KiContinueEx +0x1e0` 이 있다. 본문은 안 읽음 `[추측]`.
- `[정적]` (run19·run20) `RtlUserThreadStart +0x7` 이 전역 `Kernel32ThreadInitThunkFunction` 을 읽는다 (`refs`). 칸은 `LdrpInitializeKernel32Functions` ret `+0xff` `LdrGetProcedureAddressForCaller(kernel32, "BaseThreadInitThunk")` 결과.
- `[정적]` (run25) `KERNEL32!BaseThreadInitThunk` rva `0x2cd70`, forwarder 아님. `ECX == 0` 이면 `+0x12` (ret `+0x17`) CFG 거쳐 `RDX` 를 `RCX = R8` 로 간접 호출 → `+0x19` (ret `+0x20`) `RtlExitUserThread`.
- `[관찰]` (01_sprint) Worker 스레드 종료 콜스택에 `RtlUserThreadStart+0x2c` → `BaseThreadInitThunk+0x20`. 메인 스레드에선 직접 안 봄.
- `[관찰]` 메인 스레드 Win32 시작 주소 = `test!mainCRTStartup`.
- `[추측]` (flow.md 2.10) 커널 `NtContinue` 가 맡긴 문맥의 `Rip` 로 실제로 보낸다 / `RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup`.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, call·ret 둘 다). 태그 `[정적]` / `[추측]`. 라벨은 ret.
- `LdrInitializeThunk +0x18` 에서 `RtlUserThreadStart` → `BaseThreadInitThunk` 까지 **이어진 사슬 하나** (커널·유저 경계 표시), 단계마다 `Rip`·`Rcx`(·`Rdx`) 가 어떻게 옮겨 가는지 짧게.
- flow.md 2.10 `[추측]` 두 줄 판정, 그림 2 `PH4` 의 "공백 (추측)" 화살표를 무엇으로 바꿀지.
- 그림 2 `(정적)` 상자 후보 2 ~ 4개 (이유 한 줄씩). 커널 쪽 상자를 그림 2 에 넣을지 (그림 1 은 nt, 그림 2 는 ntdll 이다 — 경계에 어떻게 둘지 제안).
- 동적으로 확인할 것 + 멈춤점 (`nt!함수+off` / `ntdll+rva`, 짧게). 이미 대기 중인 **Q29**(`NtContinue` → `RtlUserThreadStart` `k`), **Q30**(`RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup`), **Q42**(커널 → 유저 전환), **Q48**(`Kernel32ThreadInitThunkFunction` 값) 과 겹치면 그 번호를.
- 스크립트 문제는 "스크립트 문제" 절에.

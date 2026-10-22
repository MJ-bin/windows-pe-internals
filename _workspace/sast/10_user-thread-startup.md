# 10 user-thread-startup — 정적 분석 요청

- 날짜: 2026-10-22
- 스프린트: 02_sprint
- 대기열: **Q6** — 2.3 `nt!KiStartUserThread`, `nt!PspUserThreadStartup` callee (`_workspace/plan.md`)
- 관련: `runs/02_sprint/flow.md` 2.3 (`[관찰]` run4·run5 콜스택 + `[공백]` 커널 → 유저 전환), run11 (`PspAllocateThread +0x681` 이 `PspUserThreadStartup` 주소를 `KeInitThread` 로), run13 (멈춤 APC, `PspUserThreadStartup +0x22` `[추측]`)

## 이름·지도에서 본 것 (오케스트레이터가 `pdb_aliases.py --where` 로 확인)

- `KiStartUserThread` = RVA `0x6afbd0`, PDB 종류 **함수 아님(none)**, `_index.tsv` 에 없음. → Ghidra 함수 **`KxStartUserThread`**(RVA `0x6afb70`) 안 `+0x60` 자리에 붙은 이름표다.
- `KiStartUserThreadReturn` = RVA `0x6afb60`, 함수 아님, `_index.tsv` 에 없음. `KxStartUserThread` 바로 **앞 16바이트**.
- `[관찰]` (run4) 콜스택: `nt!PspUserThreadStartup` ← `nt!KiStartUserThread+0x28` ← `nt!KiStartUserThreadReturn` ← (유저 쪽 자리) `ntdll!RtlUserThreadStart`.
  - 그러니 `KiStartUserThread+0x28` = `KxStartUserThread+0x88` 일 것이다 `[추측: 계산]`.
- `PspUserThreadStartup`(0x910de0) 장부 9줄 (이미 sast/08 때 뽑힘): `+0x22 KiLowerIrqlProcessIrqlFlags`, `+0x3c PspDisablePrimaryTokenExchange`, `+0x78 PsGetSessionLocaleId`, `+0x99 KeUpdateTebSchedulingPropertiesCurrentThread`, `+0xa1 PspNotifyThreadCreation`, **`+0xb3 PspInitializeThunkContext`**, `+0xca DbgkCreateMinimalThread`, `+0xe3 KeWaitForSingleObject`(장부 표시 `alias:KeWaitForMutexObject+1` — 같은 함수 공개 이름 둘, run13), `+0xf5 PspTerminateThreadByPointer`.
- 지도: `KxStartUserThread`·`PspUserThreadStartup` 은 `NtCreateUserProcess` 지도에 없다 (호출로 안 닿음 — 새 스레드 출발은 호출이 아님, sast/05).

## 알고 싶은 것

1. **이름표 확인**: `KxStartUserThread` 의 `+0x60`(`KiStartUserThread`), `+0x88` 에 무슨 명령이 있나. `+0x88` 이 run4 의 `KiStartUserThread+0x28`(돌아올 자리)과 같은가 — 즉 그 바로 앞(`+0x8x`)이 **시작 루틴(=`PspUserThreadStartup`)을 부르는 간접 호출**인가? (예/아니오 + 명령 줄). 시작 루틴 주소를 어디서 꺼내나 (run11/run13: `KeInitThread` 가 시작 정보 묶음으로 넘긴 값, `KiInitializeContextThread` 가 첫 스택에 쌓음 `[추측]`).
2. **`KiStartUserThreadReturn`(0x6afb60) 자리**: 무슨 명령인가. 콜스택에 이 이름이 보인 이유(시작 루틴이 돌아올 주소로 스택에 쌓여 있던 값인지).
3. **커널 → 유저 전환 (2.3 `[공백]`)**: `PspUserThreadStartup` 이 돌아온 뒤 `KxStartUserThread`/`KiStartUserThreadReturn` 이후 어떤 길로 유저 모드에 들어가나 — 유저로 넘어가는 **명령**(예: `IRETQ`/`SYSRETQ` 계열)과 그 함수·자리. 유저의 첫 RIP 를 **어디서 꺼내 쓰나** (트랩 프레임 같은 저장 묶음의 어느 칸) — 그 칸을 **누가 채우나**는 Q7(`PspInitializeThunkContext`) 몫이니 이름만 짚어라.
   - `KxStartUserThread` 와 그 뒤에 이어지는 코드가 Ghidra 함수가 아니면 `objdump` 등 원본 바이트로 읽고 출처를 밝혀라 (sast/08 처럼).
4. **`PspUserThreadStartup` 흐름**: 위 9줄의 실제 흐름 순서(점프 따라)와 하는 일 단위. 특히
   - `+0x22` IRQL 내리기 직후 멈춤 APC 가 배달될 수 있다는 run13 `[추측]` 을 코드로 더 굳힐 수 있나.
   - `+0xb3 PspInitializeThunkContext` 는 **첫 스레드일 때만** 부르나, 모든 스레드에 부르나 (조건 줄). 본문은 Q7 에서 본다 — 이번엔 부르는 조건과 인자만.
   - `+0xca DbgkCreateMinimalThread`, `+0xe3` 기다림, `+0xf5` 종료는 어떤 조건일 때인가 (보통 test.exe 첫 스레드가 타는 길 한 줄).
5. 표기 규칙(tail/frag, indirect-cfg, alias, 떨어진 범위, `cfg:stack[…]`)은 reference.md 대로. `.c` 는 "`.c` 읽을 때 주의"대로 — 흐름은 `.asm`.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^(KxStartUserThread\|PspUserThreadStartup)$` | 0 | Q6 본체 |

- depth 0. 유저로 넘어가는 길이 다른 함수(예: 트랩 프레임 복원·시스템 서비스 출구 계열)로 이어지면, 이름을 `--where`·지도로 찾고 그 함수만 넓혀 뽑는다 (넓힌 이유를 결과서에).
- analyze 다시 하지 않는다.

## 이미 아는 것

- `[관찰]` (run4·run5) test.exe 모든 새 스레드가 `PspUserThreadStartup` 에서 출발, 콜스택 모양 같음. 메인 스레드는 커널 다음 유저 쪽에서 처음 `ntdll!LdrInitializeThunk` 에 멈췄고, 그때 콜스택은 자기 혼자였다. 커널 콜스택의 유저 쪽 자리엔 `ntdll!RtlUserThreadStart` 가 보였다.
- `[정적]` (run11) `PspAllocateThread +0x681` 이 `PspUserThreadStartup` 주소를 `KeInitThread`(+0x6f9) 에 넘김. CONTEXT.Rip → StartAddress, Rcx → Win32StartAddress.
- `[정적]` (run13) `KeInitThread +0x301` → `KiInitializeContextThread`. 멈춘 첫 스레드는 `KiDeliverApc +0x220 → KiSchedulerApc +0xf7` 에서 기다림. 배달 시점은 `PspUserThreadStartup +0x22` 직후 `[추측]`.

## 받고 싶은 것

- 질문 1~5 답 + 근거 (`.asm` 의 `+0x` 줄 또는 원본 바이트 주소). 대표 지점은 ret_off, call_off 는 괄호.
- 동적으로 확인할 것 + 추천 멈춤점.
- 결과서: `_workspace/sast/10_user-thread-startup_result.md`

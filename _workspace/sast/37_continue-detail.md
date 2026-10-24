# 37 continue-detail — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 02_sprint
- 대기열: Q58 (단계 3, 정적 묶음 — 대표 지점 목표엔 덜 중요한 "작은 정적")
- 관련: flow.md 2.10 (`NtContinue` 길), run29 "아직 확인 필요" 정적 Q58 / 이전 요청 sast/24 (continue-start, run29), sast/36 (run41 — 넷째 질문 답)

## 알고 싶은 것

작은 질문 셋. 각각 **값 하나, 함수 이름 하나, 또는 예/아니오** 로.

1. **맡긴 문맥의 `ContextFlags` 가 정말 `0x10001b` 인가.**
   - sast/24: `PspInitializeThunkContext +0x8d ADD R12D,0x10001b` (CET 스레드면 `+0x40` 더함, `+0x6c~+0x76`) 로 정한 값을 `+0xf0` 에서 `RtlInitializeExtendedContext2` 에 넘김. "그 함수가 값을 그대로 쓴다" 는 `[추측]`.
   - 커널 `RtlInitializeExtendedContext2` 가 받은 `ContextFlags` 를 그대로 `CONTEXT.ContextFlags` 에 쓰나, 비트를 더하거나 깎나 (예: XSTATE 비트 `0x40`, `CONTEXT_EX` 관련 비트). 예/아니오 + 쓰는 줄 `+0x`.
   - 그 뒤 `PspInitializeThunkContext` 안에서 `ContextFlags` 를 다시 바꾸는 줄이 있나.
   - 최종 값: CET 아닐 때 / CET 일 때 각각 한 값으로.
2. **`KiVerifyContextIpForUserCet` 가 한 칸 올린 새 그림자 스택 포인터를 누가 유저 SSP(MSR `0x6a7`) 로 쓰나.**
   - sast/24: 성공하면 `+0x128 ADD RAX,8` 로 올린 포인터를 `[R15]` 에 씀. 호출자 `KiContinuePreviousModeUser` (`+0x399`, 그 칸 = `[RBP+0x8]` 로 보임) 는 `+0x39e` 뒤에 그 칸을 안 읽음 → `[공백]`.
   - 찾을 것: 그 칸(또는 같은 값)을 `WRMSR 0x6a7` 하거나 트랩 프레임/스레드 칸에 저장하는 함수 + 오프셋. 후보 `[추측]`: `KiContinuePreviousModeUser` 안의 다른 갈래, `KeContextToKframes`, `NtContinueEx` 출구 쪽 (`KiRestoreSetContextState` `+0x19c`, 커널 그림자 스택 처리 `+0x1e2~+0x20f`), `KiKernelExit`.
   - 아무도 안 쓰면 "안 씀 — 그러면 유저 SSP 는 `RtlUserThreadStart` 칸을 가리킨 채로 유저로 돌아간다" 같은 결론 한 줄 (가능하면 근거와 함께, 아니면 `[추측]`).
3. **`KiInitiateUserApc` 본문 — 배달할 유저 APC 가 있을 때 원래 문맥을 어디에 보관하나.**
   - 208바이트, 짧다. 무엇을 부르나 (`KiDeliverApc` 등), 원래 트랩 프레임 문맥을 유저 스택에 복사하는지 한 줄.
   - test.exe 시동 때 이게 불리는지는 정적으로 못 정함 (동적 Q57 몫) — 본문 요약만.

## 넷째 질문은 뺐음 (run41 로 답함)

- Q58 원래 넷째: `RtlUserThreadStart` 의 `RtlLeaveCriticalSection` 특례 갈래(`+0x13~+0x30`)가 왜 있나.
- run41 (sast/36 질문 3): 같은 꼴(`LEA 함수 / CMP / JZ / 직접 CALL`)이 ntdll 에 23곳 넘게 있음 → 컴파일러가 간접 호출을 "자주 오는 대상 먼저 비교해서 직접 부르기" 로 바꾼 꼴 `[추측]`. 정적으로 더 좁히기 어렵고, 실제로 안 타는지는 동적 Q30 에 붙여 둠. 이번엔 안 본다.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^RtlInitializeExtendedContext2$` | 0 | 질문 1 (rva `0x2b85c0`, 545바이트, `.c` 있음) |
| ntoskrnl | `^PspInitializeThunkContext$` | 0 | 질문 1 (rva `0x9106e8`, 957바이트, 이미 있음 — `+0x6c~+0xf0` 와 그 뒤 `ContextFlags` 쓰기만) |
| ntoskrnl | `^KiVerifyContextIpForUserCet$` | 0 | 질문 2 (rva `0x411b78`, 451바이트, 이미 있음 — `[R15]` 가 무엇인지) |
| ntoskrnl | `^KiContinuePreviousModeUser$` | 0 | 질문 2 (rva `0x2bc750`, 1226바이트, 이미 있음 — `+0x399` 앞 `R9`/칸 준비, `+0x39e` 뒤) |
| ntoskrnl | `^NtContinueEx$` | 0 | 질문 2 (rva `0x6addf0`, 710바이트, 이미 있음 — 출구 쪽 `0x6a7` 쓰기) |
| ntoskrnl | `^KiInitiateUserApc$` | 1 | 질문 3 (rva `0x6b3260`, 208바이트, `.c` 있음) |

- 질문 2 의 "쓰는 곳" 이 위 함수 밖이면 이름·오프셋만 찾고 본문은 안 읽어도 된다 (`WRMSR` 검색이나 `_calls.tsv`).
- `KeContextToKframes`, `KiRestoreSetContextState`, `KiKernelExit` 은 질문 2 에 필요할 때만 depth 0 으로.

## 지도에서 본 것

- ntoskrnl 지도는 `ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*` 하나뿐 — `NtContinue` 길 함수는 그 지도 밖. rva·크기는 `ms/decomp/ntoskrnl/_index.tsv` 에서.
- 참고: ntdll `RtlInitializeExtendedContext2` 도 있음 (rva `0xcc6f0`, 527바이트) — 이번 대상 아님 (맡긴 문맥은 커널이 만듦).

## 이미 아는 것

- `[정적]` (sast/24, run29) `PspInitializeThunkContext +0x8d` `ADD R12D,0x10001b`, CET 면 `+0x40`; `+0xf0` `RtlInitializeExtendedContext2`; `+0x295` 쯤 유저 그림자 스택에 `RtlUserThreadStart` 한 칸을 올림 (`KePopulateContinuationContext` 와 같은 꼴: `RDMSR 0x6a7` → `−8` → `WRUSSQ` → `WRMSR 0x6a7`).
- `[정적]` (sast/24) `KiVerifyContextIpForUserCet`: `RDMSR 0x6a7` (`+0x13a~+0x145`) → 칸 값과 `CONTEXT.Rip` 비교 (`+0xfc~+0x101`) → 같으면 `+0x128 ADD RAX,8` 해서 `[R15]` 에 쓰고 성공. `Rip == RtlUserThreadStart` 면 `ContinueType` 0→1 특례 (`+0x9d`·`+0xa6`).
- `[정적]` (sast/24) `KiContinuePreviousModeUser +0x399` 는 CET 스레드(`_KTHREAD +0x74` bit20) + 프로세스 `+0x754` bit17 일 때만 부름. `+0x39e` 뒤 `[RBP+0x8]` 안 읽음.
- `[정적]` (sast/24) `NtContinueEx +0x148 TEST byte [thread+0xc2],3` → `+0x15b` `KiInitiateUserApc` (ret `+0x160`). "원래 문맥은 APC 틀에 보관" 은 `[추측: 본문 안 읽음]`.
- `[정적]` (sast/36, run41) `RtlUserThreadStart` 특례 = 컴파일러 꼴 `[추측]` (위 "넷째 질문은 뺐음").

## 범위 밖

- CET 전체 동작, 그림자 스택 만들기·크기.
- test.exe 시동 때 CET·APC 가 실제로 켜졌나 / 불리나 — 동적 Q57.
- `KiDeliverApc`·유저 APC 디스패처(`KiUserApcDispatcher`) 본문 — 이름만.
- 출구 `KiKernelExit` 의 `IRETQ` 동작 (run15 에서 봄) — 질문 2 에 필요한 `0x6a7` 쓰기만.

## 받고 싶은 것

- 질문별 답 + 근거 (asm `+0x` 줄, 점프 목적지는 "주소 − 함수 시작")
- 질문 1: 최종 `ContextFlags` 값 표 (CET 아님 / CET)
- 동적으로 확인할 멈춤점 추천 (있으면 — Q57·Q29 에 붙일 수 있게, 함수+오프셋과 `nt + rva`)
- 새로 생긴 정적 공백 (있으면)

# 11 thunk-context — 정적 분석 요청

- 날짜: 2026-10-22
- 스프린트: 02_sprint
- 대기열: **Q7** — 2.3 유저 첫 위치가 `ntdll!LdrInitializeThunk` 로 정해지는 곳, `RtlUserThreadStart` 와의 관계 (`_workspace/plan.md`). 단계 1 의 마지막 항목.
- 관련: `runs/02_sprint/flow.md` 2.3·2.4·2.10, run9 (`PspCreateUserContext`), run11 (CONTEXT.Rip → StartAddress, Rcx → Win32StartAddress), run15 (유저 첫 RIP = `_KTRAP_FRAME.Rip`(+0x168), 트랩 프레임 = InitialStack−0x190, `PspUserThreadStartup +0xb3 PspInitializeThunkContext` 는 모든 스레드)

## 이름·지도에서 본 것 (`pdb_aliases.py --where`, 지도)

- 모두 `_index.tsv` 에 있음: `PspInitializeThunkContext` 0x9106e8, `PspCreateUserContext` 0xa2e790 (91바이트), `KxContextToKframes` 0x3e9fc0 (1381바이트), `KeContextToKframes`, `PspSetContextThreadInternal`, `PspGetContextThreadInternal`, `KiInitializeUserApc`.
- PDB 공개 이름에 `PspSystemDlls`, `PspInitializeSystemDlls`, `PspLocateSystemDll`, `PspPrepareSystemDllInitBlock`, `PspNativeSystemDll`, `PspNativeSystemDllData` 가 있다 — 커널이 ntdll 안 함수 주소(`LdrInitializeThunk`, `RtlUserThreadStart` 등)를 기억해 두는 곳일 수 있다 `[추측]`.
- 지도: `PspCreateUserContext` 깊이 1, caller 1곳, 장부에 직접 호출 줄 없음 (작은 함수). `KxContextToKframes` 깊이 4, caller 2곳.

## 알고 싶은 것

1. **트랩 프레임 `Rip` 칸에 `LdrInitializeThunk` 를 넣는 곳**: `PspInitializeThunkContext` 안에서 `_KTRAP_FRAME.Rip`(+0x168) 에 쓰는 줄과 그 값의 출처 (어느 전역 변수·구조체 칸에서 꺼내나 — 이름 있으면 이름). 같이 바꾸는 칸(`Rsp`, `Rcx`, `Rdx` 등)과 그 값의 뜻 (코드로 보이는 만큼).
2. **그 전의 값**: 트랩 프레임 `Rip` 은 처음에 무엇이었나 —
   - `PspCreateUserContext`(run9: `NtCreateUserProcess +0xd22`) 가 CONTEXT 의 `Rip`·`Rcx`·`Rsp` 에 무엇을 넣나 (`RtlUserThreadStart` 주소? 시작 주소?). 그 값의 출처.
   - 그 CONTEXT 가 `KxContextToKframes` 로 트랩 프레임에 처음 옮겨지는 길 (run15 `[추측]`) 이 맞나. 깊이 파지 말고 "누가 어느 칸을" 수준.
3. **`RtlUserThreadStart` 와의 관계** (flow.md 2.3·2.10 `[추측]`):
   - 원래 CONTEXT(Rip = `RtlUserThreadStart`? Rcx = test!mainCRTStartup?) 를 `PspInitializeThunkContext` 가 **유저 스택에 저장해 두고**, 트랩 프레임은 `LdrInitializeThunk` 로 바꾸는가? 그렇다면 `LdrInitializeThunk` 가 그 저장본을 어떻게 받나 (Rcx 로 주소를 넘기나).
   - 01_sprint `[관찰]`: 커널 콜스택의 유저 쪽 자리에 `ntdll!RtlUserThreadStart` 가 보였고, 첫 유저 멈춤은 `LdrInitializeThunk` 였다. 이 둘이 코드로 어떻게 맞아떨어지는지 한 문단.
4. **커널이 `LdrInitializeThunk` 주소를 아는 방법**: `PspSystemDlls` 같은 표에 부팅 때 채워 두는 것인지 — 채우는 함수(`PspInitializeSystemDlls`/`PspLocateSystemDll` 등)와 "LdrInitializeThunk" 이름 문자열을 쓰는지 (있으면 그 자리). 깊이 파지 말고 "어디서 오는지"만.
5. 표기 규칙(reference.md)대로. `.c` 는 "`.c` 읽을 때 주의"대로 — 칸 오프셋·흐름은 `.asm` 과 PDB 타입(`llvm-pdbutil-14 dump -types`).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^(PspInitializeThunkContext\|PspCreateUserContext\|KxContextToKframes)$` | 0 | Q7 본체 |

- depth 0. 질문 4 때문에 `PspInitializeSystemDlls`/`PspLocateSystemDll` 등을 봐야 하면 지도·`--where` 로 찾아 그것만 넓히고 이유를 쓴다.
- analyze 다시 하지 않는다.

## 이미 아는 것

- `[관찰]` (run4·run7) 첫 유저 멈춤은 `ntdll!LdrInitializeThunk`, 그때 유저 콜스택은 혼자. 커널 콜스택 유저 자리엔 `ntdll!RtlUserThreadStart`.
- `[관찰]` (run5) 메인 스레드 Win32 Start Address = `test!mainCRTStartup`.
- `[정적]` (run11) `PspAllocateThread`: CONTEXT.Rip → `_ETHREAD.StartAddress`, CONTEXT.Rcx → `Win32StartAddress`.
- `[정적]` (run15) 유저로는 `IRETQ`, 첫 RIP 는 `_KTRAP_FRAME.Rip`(+0x168), `PspInitializeThunkContext` 는 모든 스레드에 불림 (Terminated=0·Minimal=0).
- `[추측]` (flow.md 2.10, PELdr 기록) 로더가 끝나면 `ntdll!NtContinue` 로 `RtlUserThreadStart` 로 넘어간다.

## 받고 싶은 것

- 질문 1~5 답 + 근거 (`.asm` 의 `+0x` 줄, PDB 타입 칸). 대표 지점은 ret_off, call_off 는 괄호.
- 동적으로 확인할 것 + 추천 멈춤점 (Q42 와 같이 볼 것은 표시).
- 결과서: `_workspace/sast/11_thunk-context_result.md`

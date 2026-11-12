# 69 thread-create — 정적 분석 요청

- 날짜: 2026-11-12
- 스프린트: 03_sprint
- 대기열: **Q141a** (PE_emulator 요청 25 의 ㄱ — 스레드 만들기). 사용자 결정 2026-11-12 "맨 앞, 바로 시작"
- 관련: run11 `PspAllocateThread` (sast/06), run12 `PspInsertThread`, run9 `NtCreateUserProcess` (`PspBuildCreateProcessContext +0x29a`, `PspCreateUserContext +0xd22`), run16 `PspCreateUserContext`·`PspInitializeThunkContext`, run54 `SameTebFlags` (`PspAllocateThread +0x54a ~ +0x5a5`, `+0x793`)
- 빌드: 26100.9457 (ntoskrnl). PDB GUID 가 다르면 멈추고 보고.

## 배경 (대화에서 정한 것)

- PE_emulator (`../PE_emulator`) 가 test.exe 의 **둘째 스레드** (`sample` 의 `CreateThread` 1개) 를 처음부터 끝까지 돌렸다. 근거 없이 `[추측]` 으로 둔 자리를 우리 쪽에서 닫아 달라는 요청 (PE_emulator 189 ~ 191).
- 이번 바퀴는 **커널 쪽 "스레드 만들기"만** 본다. 넘기기·기다림·끝내기·남의 스레드 문맥은 Q141b ~ e 로 따로 한다 — 이번엔 보지 않는다.
- 첫 스레드 (`NtCreateUserProcess`) 길은 이미 run9 ~ run16·run54 에 있다. 이번 질문의 핵심은 **`NtCreateThreadEx` 길이 첫 스레드 길과 어디서 같고 어디서 다른가**.
- "지금 우리" = PE_emulator 가 지금 하는 것. 질문마다 맞는지·틀리는지 한 줄로 답해 주면 된다.

## 알고 싶은 것

1. **부르는 순서**: `NtCreateThreadEx` 가 부르는 함수를 순서대로 (CALL 오프셋 + ret 오프셋). 그중 `PspCreateThread` 는 어느 오프셋인가?
   - 이어서 `PspCreateThread` 가 부르는 함수를 순서대로. `PspAllocateThread`·`PspInsertThread` 를 부르나, 몇 번째인가?
2. **새 스레드 첫 문맥**:
   - (a) 유저가 준 시작 주소·인자가 `CONTEXT` 의 어느 레지스터로 가나 (`Rcx`·`Rdx` 인가)? 그 값을 쓰는 줄은 어느 함수 몇 오프셋인가?
   - (b) 이 길에서 `PspCreateUserContext` 를 부르나 (예/아니오, 오프셋)? 안 부르면 문맥을 만드는 함수 이름은?
   - (c) `CONTEXT.Rip` 는 무엇이 되나 — 유저 시작 주소 그대로인가, `ntdll!RtlUserThreadStart` 같은 고정 주소인가? (run11: `Rip`→`StartAddress`, `Rcx`→`Win32StartAddress` 는 이미 앎)
   - (d) 스택: 유저 스택을 누가 잡나 (커널이 잡나, 유저가 미리 잡아서 넘기나 — `NtCreateThreadEx` 인자 `StackSize`·`MaximumStackSize` 를 커널이 쓰나)? **크기 0 일 때 exe 헤더 (`SizeOfStackReserve`·`SizeOfStackCommit`) 값을 쓰는 줄이 커널에 있나** (예/아니오, 있으면 오프셋)?
   - 지금 우리: 첫 스레드 사슬 (`MmCreateTeb`·`RtlCreateUserStack`) 재사용, 스택 크기 0 → exe 헤더 `[추측]`.
3. **`CreateFlags` (`THREAD_CREATE_FLAGS_*`)**:
   - (a) `NtCreateThreadEx` 다섯째쯤 인자 `CreateFlags` 가 어느 비트 검사를 받나 (허용 마스크, 거절 상태값과 오프셋)?
   - (b) 그 비트가 `PspCreateThread`/`PspAllocateThread` 의 어떤 플래그로 바뀌나 (비트 대응표). run54 의 `PspAllocateThread +0x54a ~ +0x5a5` (만들기 플래그 `0x40`→`0x400`, `0x2`→`0x8`, `0x80`→`0x2000`, `0x100`→`0x4000`) 로 이어지는 사슬을 완성해 주면 된다.
   - (c) 둘째 스레드 (`CreateThread`, 보통 `CreateFlags = 0`) 의 새 TEB `SameTebFlags` 값은 정적으로 정해지나? 정해지면 값 (첫 스레드는 늘 `0x400` — run54).
   - (d) `CREATE_SUSPENDED` (`THREAD_CREATE_FLAGS_CREATE_SUSPENDED` = 1 `[추측]`) 는 어디서 멈춰 두나 (함수·오프셋). 안 켜면 언제 실행 가능 상태가 되나?
4. **속성 목록 (`PS_ATTRIBUTE_LIST`) 검사**:
   - (a) 누가 검사하나 (`NtCreateThreadEx` 안, `PspBuildCreateProcessContext`, 그 밖)? 순서 (스레드를 만들기 **전**인가 **후**인가).
   - (b) `TotalLength`·항목 `Size` 규칙 — 어떤 크기면 거절하나, 상태값.
   - (c) 모르는 속성 번호가 오면 — 거절인가 무시인가?
   - (d) 출력 속성 (`PS_ATTRIBUTE_CLIENT_ID`·`PS_ATTRIBUTE_TEB_ADDRESS`) 의 출력 포인터가 나쁠 때 (쓰기 실패) — 그 시점에 스레드가 **이미 만들어졌나**? 실패를 돌려주나, 스레드는 남나?
   - 지금 우리: `CLIENT_ID`·`TEB_ADDRESS` 둘만 쓰고 그 밖은 멈춤 (semantics 카드 `NtCreateThreadEx` 의 "미확인 범위").
5. **`PspBuildCreateProcessContext`**: 스레드 쪽 (`NtCreateThreadEx`) 에서 부르나 — 예/아니오, CALL 오프셋. 부르면 스레드 쪽에서 넘기는 둘째·셋째 인자 (`NtCreateUserProcess +0x295` 와 다른 값인가) 와, 스레드 쪽에서 실제로 탈 수 있는 속성 갈래만 짧게.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^NtCreateThreadEx$` (rva `0x840610`, 772B) | 1 | 질문 1·3·4·5 — 새로 뽑기 (아직 `.asm`·`.c` 없음) |
| ntoskrnl | `^PspCreateThread$` (rva `0x842274`, 1160B) | 1 | 질문 1·2·3 — 새로 뽑기 (아직 `.asm`·`.c` 없음) |
| ntoskrnl | `^PspBuildCreateProcessContext$` (rva `0x840920`, 6374B) | 0 | 질문 4·5 — 이미 있음. **스레드 쪽 인자로 타는 갈래만** 읽기 (전체를 풀지 말 것) |
| ntoskrnl | `^PspAllocateThread$` | 0 | 질문 2·3 — 이미 있음 (run11·run54). 만들기 플래그·문맥·스택 줄만 다시 읽기 |
| ntoskrnl | `^PspCreateUserContext$` | 0 | 질문 2(b) — 이미 있음 (91B). 스레드 길에서 불리는지만 |

- 질문 2(d) 의 스택을 잡는 쪽이 커널 안 다른 함수 (예: `PspSetupUserStack`·`MmCreateTeb` 쪽) 로 이어지면 depth 1 로 그 함수 하나까지만 더 본다. 그보다 깊으면 이름만 적고 멈춘다.
- 유저 쪽 (`KERNELBASE!CreateRemoteThreadEx`·`ntdll!RtlCreateUserStack`) 은 **이번엔 안 본다**. 커널 코드가 "유저가 준 스택을 쓴다" 로 끝나면 그렇다고만 적는다.

## 지도에서 본 것 (`ms/disassemble/ntoskrnl/_map/`)

- `_map/` 에는 `NtCreateUserProcess`·`PsLocateSystemDlls` 뿌리 지도만 있다. **`NtCreateThreadEx` 뿌리 지도는 없다** → 먼저 `run_ghidra.sh map` 으로 `NtCreateThreadEx` 뿌리 지도를 만들고 시작해 달라.
- `NtCreateUserProcess` 지도: `PspBuildCreateProcessContext` 깊이 1, caller 3곳, expanded.
- `ms/decomp/ntoskrnl/PspBuildCreateProcessContext.c` 머리: `callers: NtCreateThreadEx PsCreateMinimalProcess NtCreateUserProcess` → 질문 5 는 "부른다" 일 가능성이 높다 `[추측]`. asm 오프셋으로 확인해 달라.
- `_index.tsv` 에 대상 이름 모두 있음 (오케스트레이터 확인 2026-11-12).

## 이미 아는 것

- `[정적]` run11: `PspAllocateThread +0x640 ~ +0x647` `_ETHREAD.StartAddress` ← `CONTEXT.Rip`, `+0x64e ~ +0x655` `Win32StartAddress` ← `CONTEXT.Rcx`.
- `[정적]` run54: `PspAllocateThread +0x54a ~ +0x5a5` 만들기 플래그 → `SameTebFlags` 비트 (`0x40`→`0x400` InitialThread, `0x2`→`0x8` SkipThreadAttach, `0x80`→`0x2000` LoaderWorker, `0x100`→`0x4000` SkipLoaderInit), `+0x793` 통째 쓰기. 첫 스레드는 `NtCreateUserProcess +0x1bf`·`+0xdfd` 때문에 늘 `0x400`.
- `[정적]` run9: `NtCreateUserProcess +0x295` (ret `+0x29a`) `PspBuildCreateProcessContext`, `+0xd1d` (ret `+0xd22`) `PspCreateUserContext`.
- `[정적]` run12: `PspInsertThread` caller = `NtCreateUserProcess`, `PspCreateThread`, `PspCreatePicoThread`.
- `[관찰]` 01_sprint: 첫 스레드 Win32 Start Address = `test!mainCRTStartup`.
- `[추측]` (PE_emulator) 둘째 스레드도 첫 스레드 사슬 (`MmCreateTeb`·`RtlCreateUserStack`) 로 만들고, 스택 크기 0 이면 exe 헤더 값.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x` 줄, ret 오프셋 같이). 표시 태그 `[정적]`.
- 질문마다 "지금 우리 (PE_emulator) 가 맞나·틀리나" 한 줄.
- 정적으로 못 닫는 것은 "동적으로 확인할 것" 으로 따로 — 멈춤점 추천 (함수+오프셋, test.exe 둘째 스레드만 걸리는 조건). 이 몫은 대기열 Q42 에 붙인다 (새 TEB `SameTebFlags`·첫 문맥 실제 값).
- 새로 본 함수 이름은 `_index.tsv` 로 확인한 것만 쓴다 (없으면 `pdb_aliases.py --where ntoskrnl.exe <이름>`).

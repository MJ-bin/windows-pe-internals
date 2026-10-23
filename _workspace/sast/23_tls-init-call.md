# 23 tls-init-call — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run28)
- 대기열: Q16 (`_workspace/plan.md`) — 2.7 `LdrpCallTlsInitializers`, `LdrpCallInitRoutine`, `ImageTlsCallbackCaller` callee
- 관련: flow.md 그림 2 (`n1c`·`n2c`·`x3` `ImageTlsCallbackCaller` 상자와 그 앞 `?`), 2.7 `ntdll!LdrpCallTlsInitializers`·`ntdll!LdrpCallInitRoutine` 절 (`[추측]` 3줄), 2.8, 2.9 / 이전 요청 sast/22 (run27, `LdrpInitializeNode` 가 싣는 인자), sast/14 (run19, exe TLS `LdrpInitializeProcess +0x1c94`), sast/05 (지도에 `ImageTlsCallbackCaller` 가 없는 이유 — DATA 참조뿐, `LdrpCallTlsInitializers +0x18f` 에서 주소를 꺼냄)

## 범위

- 대상 네 함수, depth 1:
  - `LdrpCallTlsInitializers` (963바이트, 호출 7)
  - `LdrpCallInitRoutine` (667바이트, 호출 ~11)
  - `LdrpCallInitRoutineInternal` (47바이트, `+0x1d` CFG 간접 호출 하나)
  - `ImageTlsCallbackCaller` (rva `0xe83e0`, 55바이트 — 지도에 없음. `_index.tsv` 엔 있음. 이름 그대로 뽑기)
- callee 본문은 읽지 않는다. 로그·ETW(`LdrpLogInternal`, `LdrpLogError`, `LdrpLogEtwEvent`, `NtTraceEvent`, `RtlGetCurrentServiceSessionId`, `memset`, 쿠키)는 개수와 켜지는 조건만.
- 오프셋은 call·ret 둘 다. 점프 목적지는 `절대 주소 − 함수 시작` 으로 환산 (sast/22 에서 절대 주소 끝자리를 옮기는 실수가 있었다). 다 쓴 뒤 `.asm` 과 다시 대조.
- `__except`/`__finally` 착지점 안의 호출도 놓치지 않게 (sast/22: `_calls.tsv` 가 `__except` 착지점 안 호출을 빠뜨린 적 있음). unwind 범위 표로 확인.
- 구조체 칸은 PDB 타입으로 (`_LDR_DATA_TABLE_ENTRY`, `_IMAGE_TLS_DIRECTORY64`, `_TEB` 등). PDB 에 없는 칸은 오프셋+역할로.

## 알고 싶은 것

1. **`LdrpCallTlsInitializers` (인자: `ECX` = 이유, `RDX` = 모듈 항목 포인터 — run27)**
   - TLS 콜백 목록을 어디서 꺼내나 (PE TLS 디렉터리 `AddressOfCallBacks` 를 직접 찾나, 항목/전역에 미리 둔 칸을 읽나). 꺼내는 오프셋.
   - 콜백 배열을 **도는 고리**가 있나 — 콜백마다 `+0x196 LdrpCallInitRoutine` (ret `+0x19b`) 을 한 번씩 부르나. 0 을 만나면 멈추나.
   - `+0x196` 에 싣는 네 인자: `rcx` = `ImageTlsCallbackCaller` 주소 (`+0x18f` 에서 꺼냄, run8 `[관찰]`), `rdx`·`r8`·`r9` 각각 무엇인가 (DllBase? 이유? **실제 콜백 주소**?). 진짜 콜백 주소가 어느 칸으로 가는지 — 이름으로 답.
   - 콜백을 **하나도 안 부르고 돌아가는 조건** (run8: kernel32 길에서 이 함수는 불렸지만 콜백 호출이 없었다 `[관찰]` — 콜백 배열이 비었나 / 디렉터리가 없나 / 다른 조건인가).
   - `+0x113 RtlpAcquireSRWLockSharedContended` — 무슨 락인가 (전역 이름), 짝 풀기는 어디인가 (인라인?).
   - `+0x1e8 NtSetInformationThread`, `+0x1fe RtlRaiseStatus`, `+0x386 NtAlertThreadByThreadIdEx` 를 타는 조건. test.exe 시동 길에서 안 탈 것인지 정적 조건까지.
   - SEH 필터 (`$filt$0` → `RtlReportException`) 범위: 콜백 하나가 예외를 던지면 잡고 다음 콜백으로 가나, 끝내나.
   - 이 함수가 **TLS 데이터(스레드별 TLS 칸)를 마련하는 일**도 하나, 콜백 부르기만 하나 (이전 추측: 마련은 `LdrpInitializeTls` 쪽).
2. **`LdrpCallInitRoutine` (인자: `rcx` = 부를 함수, `rdx`, `r8`, `r9` — run27: Node 에서 진입점·DllBase·1·CONTEXT 또는 0)**
   - `+0x8e LdrpCallInitRoutineInternal` (ret `+0x93`) 에 넘기는 네 칸 — 받은 인자를 **그대로** 넘기나, 순서를 바꾸나.
   - 돌려주는 값: `LdrpCallInitRoutineInternal` 의 결과(`AL`)를 그대로 돌려주나, 가공하나.
   - 로그·ETW 가 켜지는 조건 (전역 플래그 이름). 오류 로그 `+0x12f LdrpLogError` 조건 (FALSE 를 받았을 때?).
   - `__finally` (`$fin$0`) 에서 하는 일 — 예외가 나도 무엇을 남기나.
   - 이 함수 안에서 스택을 바꾸거나 다른 스레드로 넘기는 일은 없나 (예/아니오).
3. **`LdrpCallInitRoutineInternal` (47바이트)**
   - `+0x1d` CFG 간접 호출 직전에 레지스터를 **한 칸씩 미나** — 즉 대상 함수가 받는 것이 `(rcx, rdx, r8) = (받은 rdx, r8, r9)` 인가. **예/아니오와 줄 단위 근거.**
   - 그렇다면 DllMain 은 `(hinstDLL = DllBase, fdwReason = 1, lpReserved = CONTEXT 포인터 또는 0)` 을 받는다 — run27 `[추측]` 이 맞나.
   - CFG 검사 방식 (`__guard_dispatch_icall` 류, 검사 실패 시 어디로).
4. **`ImageTlsCallbackCaller` (55바이트)**
   - 받는 인자와 하는 일: `LdrpCallInitRoutineInternal` 이 한 칸 밀어 넘긴 인자 중 하나(질문 1 의 "진짜 콜백 주소")를 꺼내 **진짜 콜백을 부르나**. 그때 콜백이 받는 세 인자 (`DllHandle`, `Reason`, `Reserved`) 각각 무엇인가.
   - 왜 중간 함수를 끼우나 — 코드에 보이는 이유만 (반환값을 TRUE 로 바꾸나, CFG 다른 검사, 스택 맞추기 등). 모르면 `[추측]`.
   - 결과를 무엇으로 돌려주나 (TLS 콜백은 `void` 라 늘 TRUE?).
5. **세 자리 비교** (부르는 쪽 줄만 보면 됨, callee 본문은 위에서):
   - Node 길 (`LdrpInitializeNode +0x166`, run27), exe 길 (`LdrpInitializeProcess +0x1c94`, ret `+0x1c99`, run19), 새 스레드 길 (`LdrpInitializeThread +0x28c`·`+0x3bb`) 에서 `LdrpCallTlsInitializers` 에 넘기는 `ECX`(이유)·`RDX` 가 각각 무엇인가 — 표 하나.
6. **그림·관찰과 맞추기**:
   - 그림 2 의 `LdrpCallTlsInitializers` → `?` → `ImageTlsCallbackCaller` → `Dll2Tls`/`Dll1Tls`/`ExeTls` 사슬을 코드로 채우면 정확히 어떤 함수 순서인가 (예: `LdrpCallTlsInitializers +0x19b` → `LdrpCallInitRoutine +0x93` → `LdrpCallInitRoutineInternal +0x22` → `ImageTlsCallbackCaller +?` → 콜백).
   - 진입점 길도 같은 식으로: `LdrpInitializeNode +0x19c` → `LdrpCallInitRoutine +0x93` → `LdrpCallInitRoutineInternal +0x22` → `_DllMainCRTStartup`.
   - run8 `[관찰]` "`r8=1` 이 바로 뒤 `dll2!DllMain` 의 `rdx=1`" 과 맞나.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpCallTlsInitializers$` | 1 | 질문 1·5 |
| ntdll | `^LdrpCallInitRoutine$` | 1 | 질문 2 |
| ntdll | `^LdrpCallInitRoutineInternal$` | 1 | 질문 3 |
| ntdll | `^ImageTlsCallbackCaller$` | 1 | 질문 4 (지도 밖 — 이름으로 직접) |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`, 오프셋은 call / ret)

- `LdrpCallTlsInitializers` rva `0x10e50`, 963바이트, 깊이 5, caller 6곳: `LdrpInitializeProcess +0x1c94`, `LdrpInitializeNode +0x166`, `LdrpInitializeThread +0x28c`·`+0x3bb`, `LdrpProcessDetachNode +0x130`.
  - 호출 7: `+0x113 RtlpAcquireSRWLockSharedContended`, `+0x180 LdrpLogInternal`, **`+0x196 LdrpCallInitRoutine`**, `+0x1e8 NtSetInformationThread`, `+0x1fe RtlRaiseStatus`, `+0x386 NtAlertThreadByThreadIdEx`, SEH `$filt$0+0x2b RtlReportException`.
  - `+0x18f` 에서 `ImageTlsCallbackCaller` 주소를 꺼냄 (DATA 참조, sast/05).
- `LdrpCallInitRoutine` rva `0x10ba0`, 667바이트, 깊이 5, caller 8곳: `LdrpInitializeNode +0x197`, `LdrpCallTlsInitializers +0x196`, `LdrpInitializeThread +0x2a0`, `LdrpProcessDetachNode +0x143`, `AVrfpLoadAndInitializeProvider +0x1b0`, `AvrfMiniLoadDll +0x988` 등.
  - 호출: **`+0x8e LdrpCallInitRoutineInternal`**, `+0xfa __security_check_cookie`, `+0x12f LdrpLogError`, `+0x1b5 memset`, `+0x1e0`·`+0x230 RtlGetCurrentServiceSessionId`, `+0x213 NtTraceEvent`, `+0x27c LdrpLogEtwEvent`, SEH `$fin$0` 안 `RtlGetCurrentServiceSessionId` 2·`LdrpLogEtwEvent`·`LdrpLogError`.
- `LdrpCallInitRoutineInternal` rva `0x11eab8`, 47바이트, 깊이 6, caller 1 (`LdrpCallInitRoutine +0x8e`). 호출 1: `+0x1d` (ret `+0x22`) CFG 간접 호출 (대상은 실행 때).
- `ImageTlsCallbackCaller` rva `0xe83e0`, 55바이트 — 지도에 없음 (호출 참조 0, DATA 참조 3개 중 하나가 `LdrpCallTlsInitializers +0x18f`).

## 이미 아는 것

- `[관찰]` (run8) `LdrpCallTlsInitializers +0x19b` 에서 `LdrpCallInitRoutine` 을 부를 때 `rcx` = `ntdll!ImageTlsCallbackCaller` (정확히 일치). dll 진입 때 `rcx` = `dll2!_DllMainCRTStartup`, `r8` = 1, 바로 뒤 `dll2!DllMain` 의 `rdx` = 1. kernel32 길에서는 `LdrpCallTlsInitializers` 가 한 번 불렸지만 콜백 호출이 없었다.
- `[관찰]` (run7·run8) 모듈마다 TLS 먼저(`+0x16b`), 시작 함수 나중(`+0x19c`). 순서 dll2 → dll1 → exe TLS.
- `[정적]` (sast/22·run27) `LdrpInitializeNode`:
  - `+0x166 LdrpCallTlsInitializers(ECX=1, RDX=모듈 항목 포인터)` — 조건 `TlsIndex(+0x6e) ≠ 0` 뿐, 반환값 버림.
  - `+0x197 LdrpCallInitRoutine(rcx=EntryPoint, rdx=DllBase, r8=1, r9=Flags.ProcessStaticImport ? LdrpProcessInitContextRecord 값 : 0)` — 조건 `EntryPoint ≠ 0`, 결과 `AL`. FALSE 면 `0xc0000142`.
  - 두 호출 모두 활성화 문맥 틀 안, `__except` 안, `LdrpCurrentDllInitializer = 그 항목` 인 채로.
- `[정적]` (sast/14·run19) exe TLS: `LdrpInitializeProcess +0x1c94` (ret `+0x1c99`), exe 항목 `TlsIndex ≠ 0` 일 때만, 활성화 문맥 틀 `+0x1c85 ~ +0x1ca2`.
- `[추측]` (flow.md 2.7) `ImageTlsCallbackCaller` 는 실제 TLS 콜백을 대신 불러 주는 중간 함수. `LdrpCallInitRoutine` 은 ntdll 이 유저 코드를 부르는 공용 창구. 한 칸씩 밀어 `DllMain(DllBase, 1, r9)` 로 넘긴다면 정적 import DLL 의 `lpReserved` = CONTEXT 포인터.
- `[소스]` `sample/`: exe·dll1·dll2 모두 TLS 콜백이 있다 (`ExeTls`, `Dll1Tls`, `Dll2Tls`).

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, call·ret 둘 다). 태그 `[정적]` / `[추측]`. 라벨은 ret.
- TLS 길·진입점 길을 **각각 이어진 사슬 하나**로 (부르는 쪽 → `LdrpCallInitRoutine` → `LdrpCallInitRoutineInternal` → (`ImageTlsCallbackCaller` →) 유저 코드), 단계마다 레지스터가 어떻게 옮겨 가는지 짧게.
- flow.md 2.7 의 `[추측]` 3줄 (`ImageTlsCallbackCaller` 역할, 공용 창구, 한 칸 밀기) 각각 맞음 / 틀림 / 여전히 동적.
- 그림 2 `(정적)` 상자 후보 2 ~ 4개 (이유 한 줄씩). 특히 `LdrpCallInitRoutineInternal` 을 그림에 넣을 가치가 있는지.
- 동적으로 확인할 것 + 멈춤점 (`ntdll+rva`, 짧게). 이미 대기 중인 Q25·Q26·Q27·Q28 과 겹치면 그 번호를.
- 스크립트 문제는 "스크립트 문제" 절에.

# 25 exe-crt-start — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run30)
- 대기열: Q18 (`_workspace/plan.md`) — 2.10 `KERNEL32!BaseThreadInitThunk` callee. **사용자 결정 (2026-10-23)**: kernel32 쪽은 run25·run29 로 이미 답했으니, 남은 "`BaseThreadInitThunk +0x12` 간접 호출 대상 → `main`" 을 **test.exe 정적**으로 본다.
- 관련: flow.md 그림 2 `m1` `test!mainCRTStartup` "(추측: 스레드 시작 주소)" 상자, `m2` `KERNELBASE!LoadLibraryExW ×3`, 그 앞뒤 `?`. 2.10 "`RtlUserThreadStart` 뒤 → `mainCRTStartup` → `main`" 절. 4장 2.10 줄, 4.1 / 이전 요청 sast/20 (run25, `BaseThreadInitThunk` 본문), sast/24 (run29, `RtlUserThreadStart`, CFG 중개는 JMP 뿐)

## 바이너리 (새로 올라옴 — MS 바이너리 아님)

- 위치: `sample/bin/` (`.gitignore` 대상). 사용자가 VM 에서 실제로 돌린 빌드를 그대로 올렸다.
  - `test.exe` (551424바이트), `dll1.dll`, `dll2.dll`, PDB `test.pdb`·`dll1.pdb`·`dll2.pdb` (같은 폴더, 로컬 파일 — msdl 에서 받는 것 아님)
- 빌드: [`sample/build.bat`](../../sample/build.bat) — `cl /Z7 /Od /MT`, `link /DEBUG /INCREMENTAL:NO`. **`/MT` 라서 CRT 코드가 test.exe 안에 들어 있다.**
- 오케스트레이터가 확인한 것 (pefile):
  - test.exe 진입점 rva `0x1d40` — flow.md 2.3 `[관찰]` "Win32 Start Address = `test!mainCRTStartup` (RVA `0x1D40`)" 과 같다 → **같은 빌드로 본다.** 그래도 Ghidra 에서 `0x1d40` 의 PDB 이름이 `mainCRTStartup` 인지 한 번 더 확인할 것.
  - test.exe PDB 서명 `C66E7084C5CA4874B1677F6F1C36A0511` age 1 (CodeView), `test.pdb` 와 맞는지 Ghidra 가 붙일 때 확인.
- `run_ghidra.sh` 는 `ms/bin` 아래에서 바이너리를, `ms/sym/<바이너리이름>.pdb` 에서 PDB 를 찾는다. **하네스 스크립트는 고치지 말고**, `ms/bin`·`ms/sym` 아래(둘 다 `.gitignore`)에 복사하거나 링크해서 돌린다. 어떻게 했는지, 불편했던 점은 "스크립트 문제" 절에.
- 이번 요청은 **test.exe 만** 분석한다. dll1·dll2 는 Q55 몫이니 손대지 않는다 (analyze 를 미리 걸어 두는 건 괜찮음 — 하면 결과서에 적기).

## 범위

- test.exe, 진입점 `mainCRTStartup` 에서 `main` 까지의 **사슬**만. depth 는 길을 따라가는 만큼 (CRT 안 함수 이름이 버전마다 달라서, 사슬을 찾는 데 필요한 만큼 내려간다). 사슬 위 함수 수는 대략 3 ~ 6개 예상.
  - 예상 모양 `[추측]`: `mainCRTStartup` → `__scrt_common_main` → `__scrt_common_main_seh` → (CRT 초기화들) → `invoke_main` → `main`. 실제 이름은 `_index.tsv`·PDB 로.
- 사슬 위 함수의 **다른 callee** (CRT 초기화: `_initterm`, `__scrt_initialize_crt`, 힙·로캘·`atexit` 등)는 **이름과 호출 자리만**. 본문은 읽지 않는다.
  - 예외: **`LoadLibraryExW` 를 부르는 함수**는 찾아야 한다 (질문 3). 그 함수 하나는 본문을 읽어도 된다.
- `main` 본문, `Worker`, `ExeTls`, `Log` 는 범위 밖 (소스가 있다). `main` 이 돌아온 뒤 (`exit`/`ExitProcess`) 는 이름과 호출 자리만.
- 오프셋은 call·ret 둘 다. 점프 목적지는 `절대 주소 − 함수 시작` 으로 환산하고 `.asm` 과 다시 대조.

## 알고 싶은 것

1. **진입점 이름**: rva `0x1d40` 의 PDB 이름이 `mainCRTStartup` 인가 (예/아니오). 그 함수가 받는 인자 (`RtlUserThreadStart`·`BaseThreadInitThunk` 를 거쳐 `rcx` = PEB 가 올 것 — run25·run29 `[정적]`) 를 쓰나, 버리나.
2. **`mainCRTStartup` → `main` 사슬**: 각 단계 함수 이름, call·ret 오프셋. `main` 을 부르는 줄 (함수+오프셋, ret) — `main` 에서 `k` 를 찍었을 때 바로 아래 프레임이 될 자리.
3. **`LoadLibraryExW` ×3 (run7 `[관찰]`)**:
   - run7: `ExeTls` 다음, `main` 전에 test.exe 안 `0x7ff70ee67723` (돌아올 자리로 보임) 에서 `KERNELBASE!LoadLibraryExW` 가 세 번 불렸다. 그때 test.exe 베이스는 기록에 없다.
   - test.exe 안에서 `LoadLibraryExW` (import `KERNEL32!LoadLibraryExW` 또는 `api-ms-…`) 를 부르는 자리를 전부 찾고, 그중 **rva 끝 네 자리가 `…7723` 이 되는 ret** 이 있는가 (베이스는 64KB 정렬이라 아래 16비트 `0x7723` 가 rva 아래 16비트와 같아야 한다). 있으면 그 함수 이름과 rva.
   - 그 함수는 무엇을 부르려고 `LoadLibraryExW` 를 쓰나 (DLL 이름 문자열 — 예: api-set 이름들). 왜 세 번인가 (목록을 돌며 하나씩? 실패하면 다음?). 문자열은 원본 바이트로.
   - 이 함수가 2번 사슬의 어디에서 불리나 (`main` 전 CRT 초기화의 어느 단계인가).
4. **`BaseThreadInitThunk` 와 이어 붙이기**: `KERNEL32!BaseThreadInitThunk +0x12` (ret `+0x17`) 가 CFG 중개(JMP 뿐 — run29)를 거쳐 부르는 대상이 `mainCRTStartup` 이라면, `main` 에서 `k` 예상 콜스택 (아래부터): `RtlUserThreadStart+0x2c` → `BaseThreadInitThunk+0x17` → `mainCRTStartup+?` → … → `main`. `?` 와 … 를 채워서.
5. **test.exe 안 CFG**: `/guard:cf` 를 안 켰다 (build.bat 에 없음). 그래도 test.exe 에 CFG 표(`GuardCFFunctionTable`)·`__guard_dispatch_icall_fptr` 가 있나 (예/아니오). 없다면 kernel32 쪽 CFG 검사가 test.exe 진입점을 어떻게 통과시키나는 `[추측]` 으로만 (동적 Q30).
6. **TLS 콜백 자리 (작은 확인)**: `_tls_used` (`IMAGE_TLS_DIRECTORY64`) 의 `AddressOfCallBacks` 배열에 `ExeTls` 하나만 있나, 0 으로 끝나나. CRT 가 자기 TLS 콜백(`__dyn_tls_init` 류)을 더 넣었나 — run28 `LdrpCallTlsInitializers` 는 배열을 0 까지 돈다.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| test.exe | rva `0x1d40` (`^mainCRTStartup$`) | 사슬 따라 | 질문 1·2·4 |
| test.exe | `^main$` | 0 | 질문 2 (caller 자리만, 본문 범위 밖) |
| test.exe | `LoadLibraryExW` import 를 부르는 함수 (이름은 찾아서) | 1 | 질문 3 |
| test.exe | `_tls_used`, `__guard_*` (데이터) | - | 질문 5·6 |

## 지도에서 본 것

- test.exe 는 지도가 없다 (처음 분석). `map` 은 필요하면 `mainCRTStartup` 뿌리로 돌려도 좋다 (CRT 는 작아서 깊이 제한 안에 들어올 것 `[추측]`).

## 이미 아는 것

- `[관찰]` (run4·run5, flow.md 2.3) 메인 스레드 Win32 시작 주소 = `test!mainCRTStartup` (RVA `0x1D40`, `dumpbin /headers`).
- `[관찰]` (run7) `ExeTls` 다음, `main` 전에 test.exe 안 `0x7ff70ee67723` 에서 `KERNELBASE!LoadLibraryExW` 3번 (각각 `LdrpLoadDll`). 그다음 `test!main`.
- `[정적]` (run25) `KERNEL32!BaseThreadInitThunk(ECX = 0, RDX = 시작 주소, R8 = 파라미터)`: `+0x12` (ret `+0x17`) CFG 스텁 거쳐 `RDX` 를 `RCX = R8` 로 간접 호출 → `+0x19` (ret `+0x20`) `RtlExitUserThread`.
- `[정적]` (run29) `RtlUserThreadStart +0x27` (ret `+0x2c`) 가 `Kernel32ThreadInitThunkFunction(0, 진입점, PEB)`. CFG 중개는 `JMP` 뿐이라 프레임이 안 남는다.
- `[소스]` `sample/test.c`: `main` 이 `Dll1Hello()` → `CreateThread(Worker)` → `WaitForSingleObject` → `return 0`. `ExeTls` 는 `.CRT$XLB` 에 둔 TLS 콜백. `/MT` 빌드.
- `[추측]` (flow.md 2.10) `mainCRTStartup` 은 CRT 준비(`LoadLibraryExW` 3번 포함)를 하고 `main` 을 부른다.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄, call·ret 둘 다). 태그 `[정적]` / `[추측]`. 라벨은 ret.
- `BaseThreadInitThunk +0x17` 에서 `main` 까지 **이어진 사슬 하나** (함수+ret, 그 사이 `LoadLibraryExW` 를 부르는 곳이 어디 끼는지).
- flow.md 2.10 `[추측]` 판정: "`mainCRTStartup` 이 CRT 준비(`LoadLibraryExW` 3번 포함)를 하고 `main` 을 부른다", "그 코드(`…67723`)가 test.exe 의 어느 함수인지".
- 그림 2 `m1`·`m2` 와 그 사이 `?` 를 무엇으로 바꿀지 — `(정적)` 상자 후보 2 ~ 4개 (대표 지점만, CRT 함수를 다 넣지 말 것).
- 동적으로 확인할 것 + 멈춤점 (`test+rva`, 짧게). 이미 대기 중인 **Q30**(`RtlUserThreadStart` → `BaseThreadInitThunk` → `mainCRTStartup` `k`), **Q31**(`mainCRTStartup` → `LoadLibraryExW` ×3 → `main` `k`) 과 겹치면 그 번호를.
- 스크립트 문제 (MS 바이너리가 아닌 파일을 넣을 때 불편했던 점 포함)는 "스크립트 문제" 절에.

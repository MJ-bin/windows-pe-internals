# 35 dll-crt-entry — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 02_sprint
- 대기열: Q55 (단계 3, 정적 묶음)
- 관련: flow.md 2.8 (`LdrpCallInitRoutine` → `_DllMainCRTStartup` → `DllMain`), 4장 "dll1·dll2 시작 함수 안 `LoadLibraryExW` ×3" 줄 / 이전 요청 sast/23 (tls-init-call, run28), sast/25 (exe-crt-start, run30)

## 이번 바퀴의 특이점

- **대상이 MS 바이너리가 아니라 우리 sample DLL 이다.** `sample/bin/dll1.dll`, `sample/bin/dll2.dll` (PDB 는 같은 폴더 `dll1.pdb`, `dll2.pdb`). MS 심볼 서버에서 PDB 를 받지 않는다.
- 아직 Ghidra 에 안 들어간 모듈이다 (`ms/decomp/` 에 `dll1`·`dll2` 없음). test.exe 를 넣을 때(sast/25)와 같은 방식으로 넣고, 결과는 `ms/decomp/dll1/`, `ms/decomp/dll2/` (그리고 `ms/disassemble/` 같은 이름) 에 둔다. 빌드 번호 대조(PDB GUID 26100.9457) 는 해당 없음 — 대신 dll 과 pdb 가 짝인지만 확인.
- **CRT 한 상자 원칙 (2026-10-23 결정)**: 진입점 뒤 CRT 는 지도에서 점선 상자 하나로만 둔다. 이번엔 **함수 이름과 인자 전달만** 보고, CRT 초기화 안쪽(`__acrt_*` 표 등)은 따라가지 않는다.

## 알고 싶은 것

1. **dll1·dll2 의 PE 진입점 이름은 `_DllMainCRTStartup` 인가?** (예/아니오 + PDB 이름)
   - pefile 로 읽은 `AddressOfEntryPoint`: dll1 `0x1d60`, dll2 `0x1d50` (둘 다 ImageBase `0x180000000`, SizeOfImage `0x8c000`) — 오케스트레이터가 확인.
2. **진입점 → `DllMain` 까지 함수 사슬과, 셋째 인자 `lpReserved`(진입 때 `R8`) 가 `DllMain` 의 `R8` 까지 바뀌지 않고 가나?** (예/아니오)
   - 사슬 예상: `_DllMainCRTStartup` → `dllmain_dispatch` → `DllMain` `[추측: MSVC CRT 일반 모양]`. 실제 이름·call 자리(`+0x` call/ret)로.
   - `R8` 을 어디에 저장했다가 다시 싣는지 (스택 칸·레지스터) 줄 단위로. 중간에 값을 바꾸거나 0 으로 바꾸는 갈래가 있으면 그 조건.
   - `fdwReason == 1` (`DLL_PROCESS_ATTACH`) 일 때 `DllMain` **앞에서** 부르는 CRT 함수 이름 목록 (depth 1, 이름만. 예: `dllmain_crt_process_attach`, `__scrt_initialize_crt` …). `DllMain` 이 CRT 준비 **뒤에** 불린다는 run7 `[확인 필요]` ("DLL 시작 함수가 CRT 준비를 먼저 하고, 그다음 DllMain") 가 맞나.
   - `lpReserved` 를 CRT 가 **읽어서** 무언가를 정하는 줄이 있나 (예: 프로세스 분리 때 `reserved != NULL` 이면 정리 건너뜀). 있으면 이유값 몇에서인지.
3. **run7 의 `LoadLibraryExW` ×3 돌아올 주소 둘은 dll 안 어느 함수인가?**
   - `[관찰]` run7 표 7번: `KERNELBASE!LoadLibraryExW` ← `0x7ffd80e2f5fb` (모듈 미확인), 9번: ← `0x7ffd80ebf60b` (run6 의 dll2 범위 `0x7ffd80e70000~0x80efc000` 안).
   - 64KB 정렬이므로 rva 의 아래 16비트는 각각 `0xf5fb`, `0xf60b` 로 정해진다. SizeOfImage `0x8c000` 안에서 rva 후보 = `0x0f5fb, 0x1f5fb, …, 0x8f5fb` / `0x0f60b, …, 0x8f60b`.
   - 이 후보 중 **바로 앞이 `CALL [IAT LoadLibraryExW]` 인 ret 자리** 가 dll1·dll2 각각 몇 개인가, 그 함수 이름 + 오프셋. 기대: test.exe 와 같은 UCRT `try_get_function_slow +0x73` (sast/25 — test.exe 는 rva `0x57723`) `[추측]`.
   - dll2 는 run6 base 그대로면 rva `0x4f60b`. 그 자리가 맞는지 먼저 보고, 아니면 후보 전체에서.
   - `0x7ffd80e2f5fb` 가 dll1 인지 dll2 인지 이 방법으로 갈리면 적어 준다 (run7 "모듈 미확인" 풀기).
4. dll1 과 dll2 의 진입점 쪽 구조가 같은가 (같은 CRT 함수, 오프셋만 다름) — 한 줄.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| dll1 (`sample/bin/dll1.dll` + `dll1.pdb`) | `^_DllMainCRTStartup$` | 1 | 질문 1·2 |
| dll1 | `^dllmain_dispatch$` | 1 | 질문 2 (이름은 `_index.tsv` 로 확인) |
| dll1 | `^DllMain$` | 0 | 질문 2 (`R8` 을 받는 쪽) |
| dll1 | `^try_get_function_slow$` | 0 | 질문 3 |
| dll2 (`sample/bin/dll2.dll` + `dll2.pdb`) | 위와 같은 넷 | 같음 | 질문 3·4 |

- 질문 3 의 ret 후보가 `try_get_function_slow` 가 아니면, 그 주소를 품은 함수 이름만 `_index.tsv` 로 찾고 본문은 안 읽어도 된다.

## 이미 아는 것

- `[정적]` (sast/23, run28) `LdrpCallInitRoutine` → `LdrpCallInitRoutineInternal` 이 한 칸 밀어 진입점을 `(RCX=DllBase, EDX=1, R8=r9 원본)` 으로 부른다. `r9 원본` = 항목 `Flags.ProcessStaticImport` 면 `LdrpProcessInitContextRecord` 의 값(첫 스레드 CONTEXT 포인터), 아니면 0. 즉 정적 import DLL 인 dll1·dll2 의 진입점은 `lpReserved = CONTEXT 포인터` 를 받을 것.
- `[정적]` (Q54 정리, sast/17) `ProcessStaticImport` 는 `LdrpAllocateModuleEntry +0x194` 가 켬 (`LdrInitState == 1` 이고 현재 스레드가 `LdrpDllNotificationLock` 주인이 아닐 때).
- `[관찰]` (run6) `dll1!DllMain` = `0x7ffd814e12e0`, `dll2!DllMain` = `0x7ffd80e712e0` → 둘 다 rva `0x12e0`. (run8) `r8=1` 이 바로 뒤 `dll2!DllMain` 의 `rdx=1`.
- `[정적]` (sast/25, run30) test.exe 의 `LoadLibraryExW` ×3 = UCRT `try_get_function_slow +0x6d`(call)/`+0x73`(ret), `LoadLibraryExW(이름, NULL, 0x800)`.
- `[정적]` (sast/20) kernel32·KernelBase 진입점은 `_DllMainCRTStartup` 을 거치지 않음 (MS DLL 은 `_DllMainCRTStartupForGS2` 만). dll1·dll2 는 run8 에서 `_DllMainCRTStartup` → `DllMain` 으로 맞았음 `[관찰]`.
- `[소스]` `sample/build.bat`: `/MT` (정적 CRT), `/Od`, `/Z7`, `/LD`. `sample/dll1.c` `DllMain(HINSTANCE, DWORD, LPVOID reserved)` 는 `reserved` 를 쓰지 않음.

## 범위 밖

- CRT 초기화 안쪽 (`__acrt_execute_initializers` 표, 멀티바이트 등) — 한 상자 원칙.
- TLS 콜백 `Dll1Tls`/`Dll2Tls` (Q26 동적), `Dll1Hello`/`Dll2Hello` export.
- 프로세스 분리(이유 0)·스레드 이유(2·3) 의 CRT 길 — 질문 2 의 "`lpReserved` 를 읽는 줄" 에 걸리면 한 줄만.

## 받고 싶은 것

- 질문별 답 + 근거 (asm `+0x` 줄, 점프 목적지는 "주소 − 함수 시작")
- 진입점 → `DllMain` 사슬 한 줄 그림 (call/ret 오프셋, `R8` 이 어디를 거치는지)
- run7 주소 둘의 정체 (모듈·함수·오프셋), 갈리지 않으면 왜 안 갈리는지
- 동적으로 확인할 멈춤점 추천 (`dll1+rva`·`dll2+rva` 꼴) — Q27·Q31 에 붙일 것
- 새로 생긴 정적 공백 (있으면)

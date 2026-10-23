# 20 dll-entry — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run25)
- 대기열: Q13 (`_workspace/plan.md`)
- 관련: flow.md 2.5 "KERNELBASE · KERNEL32 시작 함수" 절 (`[관찰]` "근처", `[추측]` 실제로 들어갔나), 그림 2 `d1`·`d2` 상자, 2.10 (`BaseThreadInitThunk`) / 이전 run8 (`[확인 필요]` 4·5번 줄), sast/15·run20 (ntdll 이 이름으로 찾는 함수 셋), sast/18·run23 (ApiSet)

## 범위

- 모듈 둘: **KernelBase.dll** (이미 analyze 됨, PDB 있음), **kernel32.dll** (아직 안 됨 — PDB 받기·analyze 부터. 844KB 라 오래 안 걸릴 것).
  - 경로: `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/kernel32.dll`, `.../KernelBase.dll` (대소문자 주의)
  - kernel32 PDB GUID 가 26100.9457 과 맞는지 (`.pdb.info.txt`) 확인. 다르면 멈추고 보고.
- 시작 함수 두 개는 depth 1 (이름·자리만, callee 본문 안 읽음). 그 밖은 PE 헤더·import·export 표 읽기.
- 오프셋은 call·ret 둘 다. 점프 목적지는 `절대 주소 − 함수 시작` 으로.

## 알고 싶은 것

1. **PE 진입점 → PDB 이름**: 두 DLL 의 PE `AddressOfEntryPoint` rva 와, 그 주소의 PDB 이름 (`_index.tsv`, `pdb_aliases.py --lookup <바이너리> <0xRVA>` — 바이너리 인자는 파일 이름 그대로, 예 `KernelBase.dll`). run8 은 `rcx` 가 `KERNELBASE!KernelBaseDllInitialize` / `KERNEL32!BaseDllInitialize` **근처**였고 "정확히 일치 아님" 이었다. 그래서:
   - 진입점 주소가 그 두 이름과 **정확히 같은가** (예/아니오).
   - 아니면 진입점에 있는 함수 이름은 무엇이고, `KernelBaseDllInitialize`·`BaseDllInitialize` 와 rva 차이는 얼마인가. 같은 주소에 이름이 여럿인가 (별칭).
   - WinDbg `ln` 이 "근처" 로 보일 수 있는 이유를 정적으로 말할 수 있는 만큼 (예: 진입점이 다른 이름의 작은 함수, 이름 없는 thunk) — `[추측]` 이면 그렇게.
2. **시작 함수가 이유 1(프로세스 시작)일 때 하는 일** (depth 1): 두 진입 함수의 호출 목록과, 두 번째 인자(이유) 가 1 일 때 타는 갈래의 대표 호출 3 ~ 5개. 특히 KERNEL32 시작 함수가 KERNELBASE 쪽 함수를 부르는가.
3. **kernel32 의 import 표**: kernel32.dll 이 import 하는 DLL 이름 목록 (PE import directory 그대로). 그중 `api-ms-win-…` 같은 가상 이름이 몇 개이고, `KERNELBASE.dll` 을 직접 이름으로 import 하나. → run20·run23 의 `[추측]` "kernel32 를 들이면 api-set 이름이 KERNELBASE 로 바뀌어 따라 들어온다" 의 재료. (ApiSet 이 실제로 무엇으로 바뀌는지는 실행 때 맵 — 이번엔 "import 이름이 무엇인가" 까지)
4. **ntdll 이 이름으로 찾는 세 함수** (sast/15·run20): kernel32 export `BaseThreadInitThunk`·`TermsrvGetWindowsDirectoryW`, KernelBase export `KernelbasePostInit`.
   - 셋이 export 표에 있나, 각 rva 와 PDB 이름.
   - **전달(forwarder) 항목인가** (export 가 다른 DLL 이름 문자열을 가리키나). run20 의 남은 `[추측]` "`BaseThreadInitThunk` 가 forwarder 인지".
   - `BaseThreadInitThunk` 의 호출 목록 (depth 1, 이름만) — 2.10 `BaseThreadInitThunk` 쪽 Q18 로 넘길 짧은 단서.
5. **DllMain 모양**: 두 시작 함수가 CRT 식 `_DllMainCRTStartup` 을 거치나, 바로 `XxxDllInitialize` 인가 (run8: dll1·dll2 는 `_DllMainCRTStartup` → `DllMain`).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernel32 | (PDB 받기 + analyze + export·map) 진입점 함수, `^BaseDllInitialize$`, `^BaseThreadInitThunk$` | 1 | 질문 1·2·4·5 |
| kernelbase | 진입점 함수, `^KernelBaseDllInitialize$`, `^KernelbasePostInit$` | 1 | 질문 1·2·4·5 |
| kernel32 | PE import directory, export 표 (forwarder 여부) | - | 질문 3·4 |

## 이미 아는 것

- `[관찰]` (run8) `LdrpInitializeNode +0x19c` 의 `LdrpCallInitRoutine` 이 두 번: `ln @rcx` = `KERNELBASE!KernelBaseDllInitialize` 근처 → `KERNEL32!BaseDllInitialize` 근처, `r8` 둘 다 1. "정확히 일치 아님" `[확인 필요]`. dll1·dll2 는 `_DllMainCRTStartup` 과 정확히 일치.
- `[정적]` (sast/15·run20) ntdll `LdrpInitializeKernel32Functions` 가 `LdrLoadDll("KERNEL32.DLL")` → `GetProc(kernel32, "BaseThreadInitThunk")` → 칸 `Kernel32ThreadInitThunkFunction`, `GetProc(kernel32, "TermsrvGetWindowsDirectoryW")`, `LdrLoadDll("KERNELBASE.dll")`, `GetProc(kernelbase, "KernelbasePostInit")` → 간접 호출.
- `[정적]` (sast/18·run23) ntdll `LdrpLoadDependentModuleInternal +0x2ff ApiSetpResolveHost` 가 api-set 이름을 진짜 이름으로 바꾼다.
- `[추측]` (flow.md 2.5) kernel32 를 들이면 kernelbase 가 따라 들어오고, 그래서 KERNELBASE 가 먼저 켜진다.
- KernelBase analyze 는 끝나 있다 (함수 6448, SetPdb OK, plan 결정 기록 2026-10-21). PDB: `ms/sym/KernelBase.dll.pdb`, 별칭 표 `ms/sym/KernelBase.dll.aliases.tsv`.

## 받고 싶은 것

- 질문별 답 + 근거 (PE 헤더 값, `.asm` 의 `+0x..` 줄, export·import 표). 태그 `[정적]` / `[추측]`.
- 두 DLL 의 **"진입점 rva | 그 주소 PDB 이름 | `XxxDllInitialize` rva | 차이"** 표 하나.
- kernel32 import DLL 이름 목록 (짧게 — 길면 개수 + api-set 이름 몇 개 예시 + KERNELBASE 직접 여부).
- 그림 2 `d1`·`d2` 상자 글을 어떻게 바꾸면 되는지 한 줄씩.
- 동적으로 확인할 것 + 멈춤점 (이미 대기 중인 Q21·Q22·Q48·Q49 와 겹치면 그 번호를).
- Q18 (`BaseThreadInitThunk` callee) 에 넘길 단서 한 줄.
- 스크립트 문제는 "스크립트 문제" 절에.

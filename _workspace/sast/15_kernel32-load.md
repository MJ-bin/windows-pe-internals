# 15 kernel32-load — 정적 분석 요청

- 날짜: 2026-10-23
- 스프린트: 02_sprint (예정 run20)
- 대기열: Q10 (`_workspace/plan.md`)
- 관련: flow.md 2.5 (`ntdll` → `KERNELBASE`/`KERNEL32` 준비, run7 관찰 트리), 4장 2.5 공백 / 이전 요청 sast/14 (`LdrpInitializeProcess +0x199d` 에서 이 함수를 부름), sast/13 (앞쪽이 준비한 전역)

## 범위

- 대상 세 함수, depth 1: `LdrpInitializeKernel32Functions`, `LdrLoadDll`, `LdrpLoadDll`.
- callee 본문은 읽지 않는다. 특히 `LdrpLoadDllInternal` 안쪽(올리기·켜기)은 Q11·Q12 몫이다. 이름·인자로 짐작한 역할은 `[추측]`.
- 잡일(`memset`, `LdrpLogInternal`, `LdrpLogEtwEvent`, `__security_check_cookie`, `LdrpLogDllStateEx2`)은 개수만.

## 알고 싶은 것

1. **`LdrpInitializeKernel32Functions` 의 순서**: 호출 15곳을 시간 순서로 정리한다. 특히:
   - `+0x9f LdrpFindLoadedDllInternal` 은 무엇을 찾나 (이미 올라와 있는지 확인?). 찾으면 `+0xbb LdrLoadDll` 을 건너뛰나?
   - 두 `LdrLoadDll` (`+0xbb`/ret `+0xc0`, `+0x151`/ret `+0x156`) 은 **각각 어느 DLL 이름**을 넘기나 (원본 바이트로 문자열 확인). run7 은 둘 다 관찰했지만 이름은 모른다.
   - `LdrGetProcedureAddressForCaller` 3곳(`+0xfa`, `+0x12b`, `+0x196`)이 찾는 **함수 이름**과 결과를 어느 전역에 넣나.
   - `Kernel32ThreadInitThunkFunction` 칸을 채우는 `+0xe4`(sast/14 `refs`) 는 어느 호출의 결과인가. 넣는 값이 `KERNEL32!BaseThreadInitThunk` 주소인가 (이름 문자열로).
   - `+0x130 LdrpSnapKernelBaseExtensions`, `+0x160 LdrpCodeAuthzInitialize` 의 갈림 조건.
   - `+0x1aa` 간접 호출(`cfg:stack[-0x140]`): 스택 칸에 무엇을 넣었나 (`+0x196` 의 결과인가), 그 함수 이름.
   - 첫째 인자(`RCX` = 호출자의 `[RSP+0x188]`, sast/14) 에 무엇을 써 주나.
   - 실패로 끝나는 길: 개수, 대표 조건 2 ~ 3개.
2. **`LdrLoadDll`**: `+0x16b LdrpLoadDll` (ret `+0x170`, run7 관찰과 맞나) 앞에서 인자를 어떻게 바꾸나 (검색 경로, 플래그). 뒤에서 `RtlReleasePath`·`LdrpDereferenceModule` 을 부르는 조건. 이 함수가 caller 4곳인데, kernel32 준비 때 타는 길과 `LoadLibraryExW`(test.exe `main` 앞, Q31) 때 타는 길이 같은가 (정적으로 말할 수 있는 만큼).
3. **`LdrpLoadDll`**: `+0xbd LdrpPreprocessDllName` → `+0xfb LdrpLoadDllInternal` (ret `+0x100`, run7 관찰과 맞나). `LdrpLoadDllInternal` 에 넘기는 인자 (이름, 경로, 플래그, 결과 칸). `RtlDeleteBoundaryDescriptor`·`RtlGetCurrentServiceSessionId`·ETW 는 언제.
4. **run18 이 남긴 연결점 (refs)**: 앞쪽이 준비한 전역 `LdrpKnownDllDirectoryHandle`, `LdrpKnownDllPath`, `LdrpSystem32`, `RtlpSystem32Dirs`, `LdrpHashTable`, `LdrpHeap`, `LdrpSystemDllBase`, `LdrpAppHeaders` 를 **읽는 함수 목록**만 (`run_ghidra.sh refs`). 그중 이번 세 함수가 직접 읽는 것이 있나? 없으면 "어느 함수가 읽는지"만 표로 (Q11·Q12 에서 쓸 단서). 본문은 안 본다.
5. **kernel32 이 이미 올라와 있을 수 있나**: flow.md 2.5 `[추측]` "`LdrpInitializeProcess` 가 시작될 때 kernel32·kernelbase 는 아직 없다". 1번 `LdrpFindLoadedDllInternal` 갈림과 run18·run19 결과(앞쪽·뒤쪽에서 `+0x199d` 전에 올리는 호출이 없음)를 합쳐서, 정적으로 말할 수 있는 만큼.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpInitializeKernel32Functions$` | 1 | 질문 1·5 |
| ntdll | `^LdrLoadDll$` | 1 | 질문 2 |
| ntdll | `^LdrpLoadDll$` | 1 | 질문 3 |
| ntdll | 전역 `Kernel32ThreadInitThunkFunction` (이미 `_refs/` 있음), 질문 4 의 전역 8개 | - | `refs` |

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpInitializeKernel32Functions` rva `0xb8024`, 748바이트, 깊이 5, caller 1 (`LdrpInitializeProcess +0x199d`). 직접 호출 15줄: `+0x4f memset`, `+0x81 LdrpAppendUnicodeStringToFilenameBuffer`, `+0x9f LdrpFindLoadedDllInternal`, **`+0xbb LdrLoadDll`**, `+0xfa`·`+0x12b LdrGetProcedureAddressForCaller`, `+0x130 LdrpSnapKernelBaseExtensions`, **`+0x151 LdrLoadDll`**, `+0x160 LdrpCodeAuthzInitialize`, `+0x196 LdrGetProcedureAddressForCaller`, `+0x1aa cfg:stack[-0x140]` (간접), `+0x1c2 __security_check_cookie`, `LdrpLogInternal` ×3.
- `LdrLoadDll` rva `0x3cbf0`, 729바이트, 깊이 6, caller 4. 직접 호출 11줄: **`+0x16b LdrpLoadDll`**, `+0x189 RtlReleasePath`, `+0x19e LdrpDereferenceModule`, `+0x2d0 LdrpLogDllStateEx2`, 나머지 로그·쿠키.
- `LdrpLoadDll` rva `0x1f9c0`, 611바이트, 깊이 6, caller 8. 직접 호출 9줄: `+0x4b memset`, `+0xbd LdrpPreprocessDllName`, **`+0xfb LdrpLoadDllInternal`**, `+0x10f RtlDeleteBoundaryDescriptor`, `RtlGetCurrentServiceSessionId` ×2, `LdrpLogEtwEvent` ×2, 쿠키.

## 이미 아는 것

- `[관찰]` (run7) `LdrpInitializeProcess +0x19a2` → `LdrpInitializeKernel32Functions` → `+0xc0 LdrLoadDll` → `+0x170 LdrpLoadDll` → `+0x100 LdrpLoadDllInternal` → `+0x284 LdrpPrepareModuleForExecution` → … → `LdrpCallInitRoutine` 두 번 (KERNELBASE, KERNEL32 시작 함수 근처 `rcx`, `r8`=1). 그 뒤 `+0x156 LdrLoadDll` → `+0x170 LdrpLoadDll`.
- `[관찰]` (run7·run8) import 로 붙는 dll1·dll2 는 `LdrpLoadDll` 을 타지 않았다. `LdrpLoadDll` 은 kernel32 준비와 `LoadLibraryExW` 때만 찍혔다.
- `[정적]` (sast/14·run19) `+0x199d` 는 `LdrInitState`=0 일 때 부르고, 갈림 조건은 이미지 `Subsystem` 이 2 또는 3 인 것뿐이다. 첫 인자 = 호출자의 `[RSP+0x188]` (출력 칸, 나중에 `+0x1b72 LdrpInitializePerUserWindowsDirectory` 에 넘김). `Kernel32ThreadInitThunkFunction` 칸은 이 함수 `+0xe4` 가 채우고 `LdrpInitializeProcess +0x1b59` 와 `RtlUserThreadStart +0x7` 이 읽는다.
- `[정적]` (sast/13·run18) `LdrpInitializeProcess` 앞쪽은 아무것도 메모리에 올리지 않는다. 뒤쪽도 `+0x199d` 전에 네이티브 x64 길에서 올리는 호출은 없다 (sast/14).
- `[추측]` (flow.md 2.5) exe 가 무엇을 import 하든 상관없이 ntdll 이 kernel32 를 스스로 불러온다.
- 이름 주의: `LdrLoadDll` 은 export 이름. `--where` 로 PDB 이름이 같은지 한 번 확인.

## 받고 싶은 것

- 질문별 답 + 근거 (`.asm` 의 `+0x..` 줄). 태그 `[정적]` / `[추측]`. 라벨은 ret 자리 (call 자리를 쓸 땐 "(call)").
- `LdrpInitializeKernel32Functions` 순서를 **짧은 트리 하나**로 (flow.md 2.5 트리에 그대로 붙일 수 있게).
- 그림 2 `(정적)` 상자 후보 2 ~ 4개 (이유 한 줄씩).
- 동적으로 확인할 것 + 멈춤점 (`ntdll+rva`, 짧게). 이미 대기 중인 Q20·Q21·Q22·Q48 과 겹치면 그 번호를.
- Q11 ~ Q13 에 넘길 연결점 한 줄씩 (특히 `LdrpLoadDllInternal` 에 넘기는 인자, 질문 4 의 전역을 읽는 함수).
- 스크립트 문제는 "스크립트 문제" 절에.

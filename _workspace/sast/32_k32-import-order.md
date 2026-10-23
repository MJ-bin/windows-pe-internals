# 32 k32-import-order — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 02_sprint (예정 run37)
- 관련: 대기열 Q51 (단계 3 정적 묶음, 출처 run25 flow-writer 제안), flow.md 2.5 "kernel32 가 kernelbase 를 import 한다" 절·4장 2.5 `[공백]` (run25 "kernelbase 를 처음 들이는 import 바퀴")
- 이전 요청: sast/20 (run25, kernel32 import 표 세기), sast/29 (run34, `apisetschema.dll` 표 읽기), sast/17 (run22, `LdrpMapAndSnapDependency` 서술자 고리)

## 알고 싶은 것

이번엔 **PE 표만** 본다 (Ghidra 디컴파일 필요 없음, pefile 등으로 `ms/bin` 의 파일을 직접 읽기).

1. kernel32.dll 일반 import 서술자 104개의 **파일 속 순서** (0번부터). 표로: 번호, DLL 이름, 함수 수, 그 이름이 풀리는 진짜 DLL (api-set 이면 `apisetschema.dll` 로 — **가져오는 쪽이 kernel32 일 때의 예외 host** 를 반영. run34 에서 `processthreads` 가 kernel32 importer 면 kernelbase 로 풀린 것과 같은 규칙).
   - 104줄 전부 결과서에 붙이지 말고, 앞쪽 몇 줄 + "kernelbase 로 풀리는 첫 항목" 앞뒤 + 진짜 이름 둘(`ntdll.dll`, `KERNELBASE.dll`) 자리만.
2. **kernelbase 를 처음 들이는 서술자는 몇 번인가, 그 이름은 이름 그대로 `KERNELBASE.dll` 인가, 앞쪽 api-set 항목인가?** (번호 + 이름으로 답)
3. 서술자 중 **kernelbase·ntdll 말고 다른 DLL** 로 풀리는 것이 있나? (있으면 이름과 번호 — 그 DLL 도 kernel32 를 들일 때 같이 올라와야 함. 빈 host(풀 곳 없음) 로 풀리는 항목이 있으면 그것도)
4. IAT 첫 칸이 0 인 서술자 (sast/17 의 세는 고리 `+0x122~+0x14d` 가 "실제로 올릴 수" 에서 빼는 것) 가 있나? 있으면 번호.
5. 같은 확인을 **kernelbase.dll** 에도 짧게: kernelbase 의 일반 import 서술자 순서에서 첫 항목과, ntdll 말고 다른 DLL 로 풀리는 항목이 있나 (있으면 이름). kernelbase 가 다른 DLL 을 더 끌어오는지 보려는 것.

## 대상

| 모듈 | 무엇 | depth | 이유 |
|---|---|---|---|
| kernel32.dll | PE import 디렉터리 (일반), delay import 는 개수만 | - | 질문 1~4 |
| kernelbase.dll | PE import 디렉터리 (일반) | - | 질문 5 |
| apisetschema.dll | `.apiset` 섹션 (ApiSet 맵 v6, host 예외 목록 포함) | - | api-set 이름 → host |

- 파일은 `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` (경로에 띄어쓰기).
- 임시 스크립트는 세션 임시 폴더에. 레포에 남길 산출물은 결과서뿐.
- 범위 밖: ntdll 의 ApiSet 풀기 코드(`ApiSetpResolveHost`) 본문, delay import 의 실제 로드 시점, test.exe·dll 쪽 import (run36 에서 셈).

## 지도에서 본 것

- 이번은 PE 표 확인이라 지도 대상 함수가 없다. 코드 쪽 근거는 이미 있음: `LdrpMapAndSnapDependency` 의 import 고리 `+0x1d0 ~ +0x3a3` 은 **서술자 순서**로 돌고, 매 바퀴 `+0x34e LdrpLoadDependentModuleInternal` (조건 없음) `[정적]` (run22). api-set 이름은 그 안에서 `+0x2ff ApiSetpResolveHost` (옛 맵: `+0x22b5`/`+0x22e0`) 로 진짜 이름이 된다 `[정적]` (run23).

## 이미 아는 것

- `[정적]` (run25, sast/20) kernel32 일반 import 104개 = `api-ms-win-…` 102개 + `ntdll.dll`(366 함수) + `KERNELBASE.dll`(93 함수). delay import: `ext-ms-win-oobe-query-l1-1-0.dll`, `ext-ms-win-packagevirtualizationcontext-l1-1-0.dll`, `RPCRT4.dll`. 바운드 import 없음.
- `[정적]` (run34, sast/29) `apisetschema.dll` 표로 `api-ms-win-core-processthreads-l1-1-0.dll` = 기본 `kernel32.dll`, 가져오는 쪽이 kernel32 면 `kernelbase.dll`.
- `[추측]` (run25) kernelbase 를 처음 들이는 건 api-set 항목일 수도, `KERNELBASE.dll` 항목일 수도 있다 — 순서를 안 봄.

## 받고 싶은 것

- 질문별 답 (번호·이름). 근거는 "파일 오프셋/RVA 로 읽은 표" (`[정적+샘플]` 이 아니라 MS PE 표라서 `[정적: PE 표]` 처럼 적어 줘).
- 이 결과로 flow.md 2.5 kernel32 → kernelbase 줄을 어떻게 고치면 되는지 한두 줄.
- 동적으로 확인할 것이 있으면 (예: kernel32 snap 중 첫 `LdrpLoadDependentModuleInternal` 의 이름 `dS`) — 기존 동적 항목 Q20·Q49 와 같은 실행에서 볼 수 있는지.

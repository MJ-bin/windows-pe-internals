# 34 dll-search-path — 정적 분석 요청

- 날짜: 2026-10-24
- 스프린트: 02_sprint
- 대기열: Q53 (단계 3, 정적 묶음)
- 관련: flow.md 2.6 "검색 경로" 줄 (`exe 폴더를 넣는 줄은 이 세 함수에 없다 → LdrpInitializeDllPath·LdrpSearchPath 몫 (정적 Q53)`), 그림 2 `jf0` 상자 "exe 폴더 자리는 안쪽 Q53" / 이전 요청 sast/21 (map-dll-file, run26)

## 알고 싶은 것

1. **`LdrpInitializeProcess +0x1313 LdrpInitializeDllPath(0, 0, &경로)` 는 폴더 목록을 그 자리에서 만드나, 아니면 "나중에 만들 것" 표시만 해 두나?**
   - 함수가 167바이트이고 직접 호출이 로그뿐이라 "나중에 만듦(lazy)" 일 것 `[추측]`.
   - 경로 구조체의 어느 칸에 무엇을 쓰나 (칸 오프셋 + 우리 말 뜻). 인자 1·2 가 0 일 때 타는 갈래.
2. **실제 폴더 목록(문자열)은 어디서 만들어지나?** 지도 후보: `LdrpSearchPath +0x36d LdrpComputeLazyDllPath` → `+0x62 LdrpGetDllPath` → `+0xa8 RtlpGetCachedPath`. 이 사슬이 맞나 (예/아니오), 맞으면 각 함수가 하는 일 한 줄씩.
3. **exe 폴더는 어떤 값에서 오나?** 예: `LdrpImageEntry` 의 `FullDllName`, PEB `ProcessParameters` 의 `ImagePathName`, `RtlGetExePath`, 그 밖. 읽는 줄 (함수 + 오프셋)과, 이름에서 마지막 `\` 뒤를 잘라 폴더로 만드는 줄.
4. **기본 목록의 순서** (인자 0,0 일 때): exe 폴더 / System32 / System / Windows / 현재 폴더 / `PATH` 중 무엇이 어떤 순서로 붙나.
   - 코드에 문자열·전역(`LdrpSystem32` 등)으로 보이면 그대로, 환경 변수·레지스트리에서 오면 "어디서 읽는다"까지만.
   - `RtlpLookupSafeCurDirList` (SearchPath `+0x4f7`) 와 "현재 폴더 위치(SafeDllSearchMode)" 가 이 목록에 관여하나.
5. **`LdrpSearchPath` 가 목록을 어떻게 도나**: `;` 로 나눠 한 칸씩 → 폴더 + 이름 붙이기(`+0x225 RtlAppendUnicodeStringToString`) → 파일 있는지 확인(`+0x244 LdrpResolveDllName` 인가, 다른 함수인가) → **처음 찾은 것에서 멈추나** (예/아니오). 못 찾으면 돌려주는 값 (`0xc0000135` 기대, sast/21).
   - 앞에서 검색을 건너뛰는 갈래 (이름에 `\` 가 있음 등) 가 있으면 조건만 한 줄.
6. **dll1·dll2 에 적용하면**: 둘 다 부모(test.exe·dll1)에 `DependentLoadFlags` 가 없다고 보면 (sast/21: 그러면 `ctx+0x10` 경로), dll1·dll2 는 목록의 **몇 번째 칸(exe 폴더?)** 에서 찾힐 것인가 `[정적]`/`[추측]` 구분해서. dll2 의 로드 문맥(부모 dll1)도 exe 문맥의 `+0x10` 을 그대로 물려받나, 아니면 다른 경로를 쓰나.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpInitializeDllPath$` | 1 | 질문 1 (+0x1313 에서 부르는 그것) |
| ntdll | `^LdrpSearchPath$` | 1 | 질문 5·6 (목록 도는 고리) |
| ntdll | `^LdrpComputeLazyDllPath$` | 1 | 질문 2 (나중에 만들기) |
| ntdll | `^LdrpGetDllPath$` | 1 | 질문 2·3·4 (실제 목록 만들기 후보) |
| ntdll | `^RtlpGetCachedPath$` | 0 | 질문 3·4 (exe 폴더·고정 폴더 후보) |

- `LdrpInitializeDllPath.asm`, `LdrpSearchPath.asm` 는 이미 `ms/disassemble/ntdll/` 에 있다 (sast/21 depth 1 때). 다시 뽑을지는 decompiler 판단.
- `LdrpResolveDllName` (880바이트) 은 질문 5 에 꼭 필요할 때만 본문을 읽는다.
- 읽을 함수가 너무 많아지면 1·2·3 번을 먼저 답하고 나머지는 "새 공백" 으로 남긴다.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpInitializeDllPath` rva `0x899a0`, 167바이트, 깊이 5, caller 9곳 (expanded).
  - 직접 호출: `+0x88 LdrpLogInternal`, `+0xa3` 꼬리호출 `LdrpLogDllStateEx2` — 그 밖에 없음.
  - caller: `LdrpInitializeProcess +0x1313`, `LdrpMapDllSearchPath +0x330`, `LdrpLoadWow64 +0x8e`, `LdrpInitializeImportRedirection +0xb8`, `LdrpInitShimEngine +0xb7`, `LdrpLoadShimEngine +0x61`, `AVrfpLoadAndInitializeProvider +0xd6`.
- `LdrpSearchPath` rva `0x8eb0`, 1463바이트, 깊이 7, caller 3곳: `LdrpMapDllSearchPath +0x12c`, `LdrpFindLoadedDllInternal +0x226`, `LdrpFindLoadedDll +0x2f5`.
  - 직접 호출 중 눈에 띄는 것: `+0x172 LdrpAllocateUnicodeString`, `+0x225 RtlAppendUnicodeStringToString`, `+0x244 LdrpResolveDllName`, `+0x36d LdrpComputeLazyDllPath`, `+0x3a0 LdrpSaveLocationsSearched`, `+0x404`·`+0x5a3 RtlCompareUnicodeStrings`, `+0x4f7 RtlpLookupSafeCurDirList`, `+0x55a RtlCopyUnicodeString`. (`+0x29a RtlDeleteBoundaryDescriptor` 는 같은 주소 이름 10개 — 이름 믿지 말 것)
- `LdrpComputeLazyDllPath` rva `0xb9870`, 295바이트, caller 1곳 (`LdrpSearchPath +0x36d`). 직접 호출: SRW 락 잡기/풀기, `+0x62 LdrpGetDllPath`, 로그.
- `LdrpGetDllPath` rva `0xb9f30`, 635바이트, caller 2곳. 직접 호출: `+0xa8 RtlpGetCachedPath`, `+0x1ad`·`+0x1d3 RtlDetermineDosPathNameType_U`, `+0x1f9 LdrpLogRelativePathWithAlteredSearchError`.
- `RtlpLookupSafeCurDirList` rva `0x115674`: `NtOpenKey`·`NtQueryValueKey`·`RtlQueryEnvironmentVariable_U`·`NtEnumerateValueKey` — 레지스트리·환경 변수에서 목록을 읽는 모양.
- `_index.tsv` 에 비슷한 이름: `LdrGetDllPath` (0xb9f00, 34바이트), `RtlGetSearchPath` (0xb8680, 65바이트), `RtlGetExePath` (0x108a90, 76바이트) — 관련 있으면 언급만.

## 이미 아는 것

- `[정적]` (sast/21, run26) `LdrpMapDllSearchPath` 의 검색 경로 = 로드 문맥 `+0x10`. 뿌리는 `LdrpInitializeProcess +0x1313 LdrpInitializeDllPath(0,0,&경로)` → `+0x1320` 에서 exe 문맥 `+0x10` 에 넣음. 부모 `DependentLoadFlags(+0x118)` 가 마스크 `0x7b00`(또는 `0x7f00`) 와 겹치면 `+0x330/+0x335 LdrpInitializeDllPath(부모 FullDllName.Buffer, 플래그|1, &새경로)` 로 새로 만듦.
- `[정적]` (sast/21) `LdrpSearchPath` 호출 인자: `RCX=ctx`, `RDX=경로 구조체`, `R8B=(ctx.flags>>3)&1`, `R9=&B`, 스택 `&A`(NT 경로), `&D`(기본 이름), `&C`(전체 이름), `&바이트`, `&dword`. 못 찾으면 음수 → `NtFileName` 안 부르고 오류. `0x4000000e`(기계 종류 불일치) 면 `LdrpSearchPath` 부터 다시 (두 번째 못 찾으면 `0xc000007b`).
- `[정적]` (run20) kernel32 준비 때 `LdrLoadDll` 은 검색 경로 칸을 0 으로 넘김 — 경로는 더 안쪽 몫.
- `[소스]` `sample/build.bat`: dll2·dll1·test.exe 를 모두 `bin\` 한 폴더에 만든다. 링크 옵션에 `/DEPENDENTLOADFLAG` 없음.
- `[추측]` dll1·dll2 는 KnownDll 이 아니라 exe 폴더에서 `LdrpSearchPath` 로 찾음 (flow.md 2.6, 그림 2 `jf0`·`jg0` "[dll1/dll2 추정]").

## 범위 밖 (보지 않을 것)

- WOW64·shim·Application Verifier·import 리다이렉션 쪽 caller (`LdrpLoadWow64`, `LdrpInitShimEngine`, `LdrpLoadShimEngine`, `AVrfpLoadAndInitializeProvider`, `LdrpInitializeImportRedirection`) 의 경로 — test.exe 에서 안 탐 `[추측]`.
- ETW·로그 함수 (`LdrpLog*`, `LdrpSaveLocationsSearched`, `EtwEventWrite*`) 안쪽.
- `LdrpFindLoadedDll*` 쪽에서 `LdrpSearchPath` 를 부르는 갈래 (이미 올라온 모듈 찾기) — 있다는 것만 한 줄.
- `SetDllDirectory`·`AddDllDirectory` 등 실행 중에 목록을 바꾸는 API (test.exe 는 안 씀 `[소스]`).

## 받고 싶은 것

- 질문별 답 + 근거 (asm 파일의 `+0x` 줄, 점프 목적지는 "주소 − 함수 시작"으로)
- 경로 구조체 칸 표 (오프셋 / 누가 쓰나 / 뜻)
- 기본 목록 순서를 한 줄 그림으로 (예: `exe 폴더 ; System32 ; …`) — 코드에서 본 것과 추측을 구분
- 동적으로 확인할 멈춤점 추천 (함수 + 오프셋, `ntdll+rva` 도 같이): 예) dll1 찾을 때 실제 목록 문자열, 몇 번째 칸에서 찾았나
- 새로 생긴 정적 공백 (있으면)

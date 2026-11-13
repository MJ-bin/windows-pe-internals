# 86 dotnet-start — 정적 분석 요청

- 날짜: 2026-11-13
- 스프린트: 03_sprint
- 대기열: **Q152** (PE_emulator 요청 30 — .NET exe 시작 길). 사용자 결정 2026-11-12: 요청 26 ~ 30 맨 앞, 30 은 5순위 (마지막), **첫 답은 "무엇이 필요한가" 지도**. 할 만한지 판단용. 정적만 — VM 몫은 Q104 출처로.
- 원문: `/tmp/claude-1000/-home-mjbin-prj-bob-teamprj-PE-emulator/d6bd3369-b666-4505-9231-47b12d2704dd/scratchpad/internals_requests/request30_dotnet_startup.md` (읽어도 됨).
- 빌드: 26100.9457. ntdll 은 analyze 됨. **mscoree.dll 은 아직 analyze 안 됨** → analyze (PDB 포함). **`mscoreei.dll`·`clr.dll`·`Microsoft.NET` 폴더는 우리 덤프 (`ms/bin`) 에도 없다** (PE_emulator 덤프와 같음) — 그쪽은 mscoree 가 부르는 이름·경로·레지스트리까지만.
- 관련: ntdll `LdrpCorValidateImage` (rva `0x10e4b4`, 37B)·`LdrpCorFixupImage` (`0x109374`, 388B)·`LdrpCorProcessImports` (`0xb7e0`, 97B) 가 `_index.tsv` 에 있음 (오케스트레이터 확인). run20 ~ run30 대의 로더 본문 (`LdrpInitializeProcess`·`LdrpMapAndSnapDependency` 등).

## 배경

- 샘플 중 CLR 헤더 (COM descriptor, 데이터 디렉터리 14) 가 있는 x64 exe 가 310 개 (2%). PE_emulator 는 .NET exe 를 돌려 본 적 없고, 로더 흉내에 CLR 헤더 갈래가 없음 `[추측]`.
- 경계: 로더 일은 은퇴 전 ntdll 엔진이 꼭 필요한 것만 흉내, 그 밖은 진짜 코드, syscall 은 nt 엔진·팀원 몸.

## 알고 싶은 것

1. **ntdll 로더의 CLR 헤더 갈래**: CLR 헤더를 보는 줄 (`RtlImageDirectoryEntryToData(…, 14)`), IL-only 검사 (`COMIMAGE_FLAGS_ILONLY`), `LdrpCorValidateImage`·`LdrpCorFixupImage`·`LdrpCorProcessImports` 를 누가·언제 부르나 (오프셋 + ret), **진입점을 `mscoree!_CorExeMain` 으로 바꾸는 자리** (exe 의 `AddressOfEntryPoint` 를 무시하나, 시작 문맥 `Rcx` 를 바꾸나 — run7·run29 의 첫 스레드 시작 문맥과 이어서), **mscoree.dll 을 언제·어떻게 올리나** (exe 의 import (`mscoree!_CorExeMain` 하나) 로? 로더가 따로 `LdrLoadDll`?). 64비트 IL-only exe 와 혼합 (C++/CLI) exe 의 갈림.
2. **`mscoree!_CorExeMain` → 런타임 고르기 첫 단계**: 읽는 레지스트리 키 (`HKLM\SOFTWARE\Microsoft\.NETFramework` — `InstallRoot` 등), 파일 (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\mscoreei.dll`·`clr.dll`), 부르는 API·syscall (`LoadLibraryExW`·`RegOpenKeyExW` → `NtOpenKey` 등) — 목록. mscoreei 가 없으면 어디서 무슨 오류로 끝나나.
3. **`clr.dll` 시작까지 큰 줄기**: mscoreei → clr (`_CorExeMain` 다음) 에 필요한 DLL·레지스트리·파일 목록만 (깊이 들어가지 않음, 우리 덤프에 없으니 이름·경로 수준).
4. **.NET Core / .NET 5+ 자체 포함 exe 와 Framework exe 를 머리에서 가르는 법**: .NET Core 앱 호스트 (apphost — 보통 네이티브 exe, CLR 헤더 없음 + `hostfxr.dll`) 와 Framework exe (CLR 헤더 + `mscoree` import) 의 PE 머리 차이 — 데이터 디렉터리 14·import·자원으로. 단일 파일 번들 표식도 이름만.
5. **"덤프에 더할 파일" 목록**과 **"PE_emulator 가 할 만한가" 한 줄 판단** (근거와 함께).

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^(LdrpCorValidateImage|LdrpCorFixupImage|LdrpCorProcessImports)$` + 부르는 곳 (`refs`/callers) | 1 | 질문 1 |
| ntdll | 진입점을 바꾸는 자리 (`LdrpInitializeProcess` 등 이미 있는 `.asm` 에서 CLR 갈래만) | 0 | 질문 1 |
| mscoree | `_CorExeMain`·`CorExeMain` 류와 런타임 고르기 첫 함수 | 1 | 질문 2 — 레지스트리·파일·`LoadLibrary` 이름만 |

- JIT·GC·CLR 안쪽·관리 코드 실행은 넣지 않는다.
- 질문 4 는 PE 형식 (데이터 디렉터리·import) 으로 답할 수 있으면 코드 없이 — 근거는 로더가 그 칸을 읽는 줄.

## 이미 아는 것

- `_index.tsv` (ntdll): `LdrpCorValidateImage` `0x10e4b4` (37B), `LdrpCorFixupImage` `0x109374` (388B), `LdrpCorProcessImports` `0xb7e0` (97B), `RtlImageDirectoryEntryToData` `0xa5ec0`.
- 덤프: `System32/mscoree.dll` 있음, `mscoreei.dll`·`Microsoft.NET` 없음 (오케스트레이터 확인).

## 받고 싶은 것

- 결과서 `_workspace/sast/86_dotnet-start_result.md`. 질문별 답 + 근거 (`.asm` `+0x` 줄, ret), `[정적]`. mscoree PDB GUID.
- 질문 1 은 차례 표 (로더 단계 · 함수+오프셋 · 하는 일).
- 끝에 **"덤프에 더할 파일" 목록**, **"PE_emulator 가 할 만한가" 한 줄**, "다음 요청 후보".
- 질문마다 "지금 우리 (PE_emulator) 맞나·틀리나" 한 줄.

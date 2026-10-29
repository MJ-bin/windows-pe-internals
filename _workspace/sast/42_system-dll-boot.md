# 42 system-dll-boot — 정적 분석 요청

- 날짜: 2026-10-29
- 스프린트: 03_sprint (예정 run48)
- 대기열: **Q93** — 사용자 요청 2026-10-29 "다음 분석" (Q78c 앞에 끼움, 결정 기록 참고)
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련:
  - `runs/02_sprint/run10-allocate-process.md` (`PsMapSystemDlls` → `PspMapSystemDll`, caller 4곳: `PsMapSystemDlls`·`PspInitPhase3`·`PsLocateSystemDlls`·`PspLocateSystemDll`)
  - `runs/02_sprint/run16-thunk-context.md` 157~159줄 (정보 칸 `+0x20`·`+0x28` 은 부팅 때 `PspMapSystemDll` (인자 1 갈래) 이 채움, `PspLocateSystemDll +0x238`·`PsLocateSystemDlls +0x89` 에서; `PsLocateSystemDlls` ← `IoInitSystemPreDrivers +0x10f5`; 부팅 안 순서는 `[공백]`)
  - `runs/03_sprint/baggage.md` 789줄 근처 (ntdll 시작 주소 `LdrpSystemDllBase` = `PspSystemDlls[0]` `+0x20`, `PspMapSystemDll +0x172`), 짐 지도 "입력" (부팅 때 이미 있는 것)
  - run9·run31·run47 (exe 섹션 사슬 `MmCreateSpecialImageSection` 아래 — 비교 대상)
  - plan Q42 (`poi(PspSystemDlls)+0x20` vs `lm m ntdll`, `dq nt!PsNtdllExports L3` — 동적)

## 범위

- 부팅 때 시스템 DLL (ntdll 등) 을 찾아 섹션을 만들고 정보를 채우는 갈래. 짐 지도로 보면 프로세스 생성 때 "입력" 으로 쓰는 커널 쪽 짐 (`PspSystemDlls` 표, ntdll 섹션, 기본 주소, `PsNtdllExports`) 의 **출처**를 닫는 것.
- 범위 밖: 부팅 전체 순서 (`PspInitPhase2` 등과의 앞뒤는 이름만), `PspInitPhase3` 갈래 본문 (이름만, 필요하면 한 줄 요약), WOW64 ntdll 의 매핑 내용 (도는지 여부만).

## 디컴파일 내보내기

- `PsLocateSystemDlls` (`0x77e070`, 230바이트) — `.c` 없음, 뽑는다.
- 이미 있는 `PspLocateSystemDll.c` (`0x77e3b0`)·`PspMapSystemDll.c` (`0x911bd8`)·`PspInitializeSystemDlls.c` (`0xc43c40`) 는 다시 뽑지 않고 읽기만 한다 (`.asm` 이 없으면 asm 만 뽑아도 된다).
- 길을 따라 꼭 필요한 것만 더한다. 후보: `PspReferenceSystemDll` (`0x911da8`), `IoInitSystemPreDrivers` (`0xc28bcc`, 4821바이트 — `+0x10f5` 앞뒤만), `PspSystemDlls` 를 채우는 쪽, `PsNtdllExports` 를 채우는 쪽 (`run_ghidra.sh refs ntoskrnl.exe PsNtdllExports` / `PspSystemDlls` 로 찾기). 더한 것은 이유를 적는다.
- 구조체 칸은 `pdb_struct.py ntoskrnl.exe <타입>` 으로 (`PspSystemDlls` 칸 타입이 PDB 에 있으면 — 예: `_PS_SYSTEM_DLL*` 류 이름은 `--list ntoskrnl.exe SYSTEM_DLL` 로 찾기).

## 알고 싶은 것

1. **부르는 자리와 순서**
   - 부팅에서 누가 `PsLocateSystemDlls` 를 부르나 — run16 이 적은 `IoInitSystemPreDrivers +0x10f5` 가 맞나 (예/아니오, call/ret 오프셋). 다른 caller 가 있나 (`_refs`).
   - `PsLocateSystemDlls` → `PspLocateSystemDll` 이 몇 번, 어떤 인자로 불리나 (ntdll 하나인지, WOW64 쪽 ntdll 등 다른 시스템 DLL 도 도는지, 조건).
   - `PsLocateSystemDlls +0x89` 자리 (run16: `PspMapSystemDll` 을 여기서도 부름) 는 무엇인가.
2. **`PspLocateSystemDll` 이 하는 일** (줄마다 call/ret 오프셋)
   - 파일 경로를 어디서 가져오나 (`PspSystemDlls` 표의 어느 칸), `+0xc5 ZwOpenFile` 인자 (접근·공유·옵션).
   - `+0xf4 MmCreateSpecialImageSection` 인자. exe 섹션 사슬 (run31·run47, `NtCreateUserProcess +0x93c`) 과 같은 길인가, 다른 플래그·인자 (토큰·서명 수준 등) 가 있나.
   - `+0x7e ZwSystemDebugControl` 은 무엇을 하나 (조건부인가, 클래스 번호).
   - `+0x146 ObReferenceObjectByHandle`, `+0x183`·`+0x1d2 MmGetSectionInformation` 두 번, `+0x1ea MiSectionControlArea`, `+0x1fa MiDoesImageContainFunctionOverrideFixups` — 결과를 어디에 쓰나 (전역·표 칸).
   - 각 `KeBugCheckEx` 의 조건과 버그체크 코드·인자 (실패하면 부팅이 멈춘다는 뜻이면 그렇게).
   - 핸들을 닫는 자리 (`ObCloseHandle` 둘) — 섹션 객체 참조만 남기고 핸들은 닫는지.
3. **`PspMapSystemDll` 부팅 갈래**
   - `PspLocateSystemDll +0x238` 에서 부를 때의 인자와 갈래 — 프로세스마다 (`PsMapSystemDlls +0xc0`) 부를 때와 무엇이 다른가 (run16 "인자 1 갈래").
   - `PspSystemDlls[0]+0x20` (기본 주소) 을 쓰는 줄과 값의 출처 — 헤더 `ImageBase` 인가, 따로 고른 주소 (무작위 등) 인가. 부팅 때 System 프로세스에 실제로 매핑하나. 정적으로 닫히면 닫고, 아니면 동적 질문 (Q42 와 이어짐).
   - `+0x28` 칸도 같이.
   - `PsNtdllExports` (`LdrInitializeThunk`·`RtlUserThreadStart` 등 주소) 를 채우는 줄이 이 구간에 있나. 있으면 어느 함수의 어느 줄, 이름 → 주소를 어떻게 찾나 (export 표 읽기 함수 이름).
4. **쓰는 상태**
   - `PspSystemDlls` 표 칸 (PDB 에 타입이 있으면 칸 이름으로, 없으면 오프셋만), 섹션 객체 참조, 그 밖에 이 구간이 처음 쓰는 커널 전역.
   - 결과서에 baggage.md 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거) 으로 적는다. 단계는 "부팅 (⓪ 이전)" 처럼 새로 표시해도 된다 — 짐 지도 기존 단계 표시와 어떻게 이을지 의견.
   - 프로세스 생성 때 이 짐을 **읽는** 자리 (이미 아는 것: `PsMapSystemDlls`, `PspPrepareSystemDllInitBlock` 등) 와 짝을 지어 준다.

## 대상

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntoskrnl | `PsLocateSystemDlls` | `0x77e070` | 1 | 새로 뽑기 |
| ntoskrnl | `PspLocateSystemDll` | `0x77e3b0` | 1 | 이미 `.c` 있음 — 읽기 |
| ntoskrnl | `PspMapSystemDll` | `0x911bd8` | 1 | 이미 `.c` 있음 — 읽기, 부팅 갈래 |
| ntoskrnl | `PspInitializeSystemDlls` | `0xc43c40` | 0 | 이미 `.c` 있음 — 표 처음 값 |
| ntoskrnl | `PspReferenceSystemDll` | `0x911da8` | 0 | 필요하면 |
| ntoskrnl | `IoInitSystemPreDrivers` | `0xc28bcc` | 0 | `+0x10f5` 앞뒤만 |

## 지도에서 본 것

- 지금 커널 지도 (`ms/disassemble/ntoskrnl/_map/NtCreateUserProcess_*`) 는 뿌리가 `NtCreateUserProcess` 라 부팅 갈래가 없다. 필요하면 `run_ghidra.sh map ntoskrnl.exe PsLocateSystemDlls` 로 작은 지도를 따로 만든다 (기존 지도는 덮지 않게 — 파일 이름이 뿌리별이라 따로 생김).
- `_calls.tsv` (사용자가 본 것): `PspLocateSystemDll` `+0x7e ZwSystemDebugControl`, `+0xc5 ZwOpenFile` → `+0xf4 MmCreateSpecialImageSection` → `+0x146 ObReferenceObjectByHandle` → `+0x183`·`+0x1d2 MmGetSectionInformation` → `+0x1ea MiSectionControlArea` → `+0x1fa MiDoesImageContainFunctionOverrideFixups` → `+0x238 PspMapSystemDll`, 사이사이 `KeBugCheckEx`, `ObCloseHandle` 둘.

## 이미 아는 것

- `[정적]` run10: `PsMapSystemDlls` 는 `PspSystemDlls` 표를 최대 6칸 돈다. 0번 칸은 늘 `PspMapSystemDll`, 나머지는 WOW64 조건. `PspMapSystemDll` 은 `PspReferenceSystemDll +0x40` 으로 섹션을 잡고 `+0x11d MmMapViewOfSectionEx` 로 올린다.
- `[정적]` run16: 정보 칸 `+0x20`·`+0x28` 은 부팅 때 `PspMapSystemDll` (인자 1 갈래) 이 채운다. 프로세스마다는 읽기만.
- `[정적]` baggage: `LdrpSystemDllBase` = `PspSystemDlls[0]` `+0x20`, `PspMapSystemDll +0x172`.
- `[추측]` (사용자) ntdll 섹션·기본 주소·`PsNtdllExports` 는 부팅 때 준비되고 프로세스마다는 붙이기만 한다 — 맞는지 정적으로 확인.

## 받고 싶은 것 (결과서 `_workspace/sast/42_system-dll-boot_result.md`)

- **맨 앞 요약 표**: "부팅 준비 호출 사슬 (함수 / call / ret / 하는 일 / 실패하면)".
- 질문 1~4 답 + 근거 (`.asm` 의 +0x 줄). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 쓰는 상태 표 (baggage 양식) 와 프로세스 생성 때 읽는 자리 짝.
- 동적 질문 목록 (번호, 추천 멈춤점 — 부팅 때라 일반 KDNET 세션으로는 못 설 수 있음: 그러면 "부팅 디버깅 필요 / 사진·`dq` 로 값만 확인 가능" 을 구분).
- 코드는 길게 붙이지 않고 우리 말로.

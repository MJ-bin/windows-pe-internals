# 134 winsta-name — 정적 분석 요청

- 날짜: 2026-11-23
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **Q250** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/324_internals-winsta-name.md` — 원문을 먼저 읽을 것. 튜닝 `324_tune14.md` T1, 샘플 489 · 병목 120 · `CoInitializeSecurity` `0x8007000e` 63). 사용자 결정 2026-11-23 "아직 분석 전인 4 개 요청 순차적으로" 의 넷째. **정적만.**
- **먼저 읽는 것**: `_workspace/sast/128_user32-regclass_result.md` (combase 를 처음 연 바퀴 — combase PDB GUID `DA1DC0C4F35CEE58F672CBB20460D11D1`, `RegisterOleWndClass` 의 `GetLastError` → HRESULT 바꾸기 꼴).
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `user32.dll` · `combase.dll` — **이미 analyze 됨**. 다시 analyze 하지 말고 `^이름$` export 만.
    - user32 (`_index.tsv`): `GetUserObjectInformationW` rva `0x72820` 6 B (썽크로 보임) · `GetUserObjectInformationA` rva `0x51fa0` 274 B.
    - combase (`_index.tsv`): `GetThreadWinstaDesktop` rva `0x82330` 909 B · `ResolveProcessDesktopNoUser` `0x82a04` 248 B · `SetExplicitWinstaDesktopString` `0x175114` 188 B.
  - `win32u.dll` — 번호만 이미 있음 (`ms/disassemble/win32u/_syscalls.tsv`: `NtUserGetObjectInformation` `0x1065` · `NtUserGetProcessWindowStation` `0x101f` · `NtUserGetThreadDesktop` `0x107b`).
  - (질문 3, 되면) `win32kfull.sys` (4,390,912 B) · `win32kbase.sys` (3,420,160 B) — **아직 analyze 안 됨**, 같은 System32 폴더에 있음. PDB `fetch_pdb.py`. 둘 다 크다 → 1·2 를 결과서에 먼저 쓴 뒤, `NtUserGetObjectInformation` 이 어느 쪽에 있는지 export 표로 먼저 가려서 **그 하나만** analyze. 메모리가 모자라거나 오래 걸리면 `[공백]` + "다음 요청으로".
- 주의: 같은 시간에 다른 decompiler 둘이 **ntoskrnl** (sast/131) 과 **advapi32·sechost·sspicli·wininet (+ 필요하면 rpcrt4)** (sast/133) 을 돈다 — 그쪽은 건드리지 않는다. 새 analyze 전에 `free -g` 를 본다.

## 왜 (요청한 쪽 말 요약)

- 에뮬레이터 로그 (진짜 Windows 관찰 아님, r14): `NtUserGetProcessWindowStation → 0x20000` → `NtUserGetThreadDesktop → 0x20400` → `NtUserGetObjectInformation(0x20000, 2, buf, 0x40)` 기본 대답 0 (버퍼 안 씀) → 끝 글 "Exception.ToString()이 실패" → 종료.
- 부르는 꼴 넷: `(0x20000, 2, buf, 0x40)` 479 샘플 · `(0x20000, 1, buf, 0xc)` 353 · `(0x20000, 2, buf, 0)` 8 · `(0x20400, 2, buf, 0x208)` 1. 다섯째 인자 (`lpnLengthNeeded`) 는 로그에 안 보임.
- 그 뒤 COM 실패 값이 `0x8007000e` (63) / `0x8007007a` (5) 로 섞여 나옴 → "묵은 `GetLastError` 를 HRESULT 로 바꿈" 으로 보임 `[추측]`.
- 창 스테이션 핸들이 0 이던 판 (r13) 에서는 이름을 안 묻고 바로 `\RPC Control\epmapper` 연결로 감.
- `[관찰]` run44 — 프로세스 매개변수 `DesktopInfo` 글이 `Winsta0\Default`.

## 알고 싶은 것

1. **user32 `GetUserObjectInformationW` / `A`**:
   - `W` 가 정말 `win32u!NtUserGetObjectInformation` 으로 바로 가는 썽크인가 — 예/아니오 (6 B 의 점프 대상).
   - `A` 가 `NtUserGetObjectInformation` 을 부르는 인자 차림 (다섯 인자의 차례 · 버퍼 · 길이 단위) 과 돌아온 뒤 읽는 것: 종류 2 (이름) · 3 (형식) 의 글이 UTF-16 · 널 포함인지 (A 판이 바꾸는 줄에서 보이는 만큼), `lpnLengthNeeded` 가 바이트인지, 반환 0 / 0 아님을 어떻게 쓰는지, 실패 때 `LastError` 를 user32 가 정하나 (커널이 TEB 에 쓴다고 보고 안 쓰나).
2. **combase `GetThreadWinstaDesktop`** (+ 부르는 쪽 `ResolveProcessDesktopNoUser`, 글을 받는 쪽 `SetExplicitWinstaDesktopString` 은 이어지면):
   - 부르는 차례: `GetProcessWindowStation` → `GetThreadDesktop` → `GetUserObjectInformationW` 몇 번 · 어느 핸들 · 어느 종류 · 버퍼 크기 (`0x40`·`0xc`·`0`·`0x208` 이 각각 어느 줄인지).
   - 버퍼가 모자랄 때 길 (길이 묻기 → 다시 부르기인가).
   - 실패 때 돌려주는 HRESULT — 고정값인가 `GetLastError` 변환인가 (`0x8007000e`·`0x8007007a` 가 나올 수 있는 줄).
   - 성공하면 만드는 글 꼴 (`<창 스테이션>\<데스크톱>` 인가) 과 그 글이 어디로 가나 (RPCSS 연결 인자인지 — 함수 이름까지만).
   - 핸들이 0 이면 이름을 안 묻고 건너뛰는 줄이 있나 (r13 로그와 맞나), 부르는 쪽이 실패를 받으면 RPC 연결을 건너뛰나 / 멈추나.
3. **win32k 쪽 (되면)** — `NtUserGetObjectInformation` 의 종류 1 · 2 갈래:
   - 종류 1 (`USEROBJECTFLAGS`, 12 B) 세 칸 뜻과 대화형 창 스테이션의 값 (`WSF_VISIBLE` 류 깃발).
   - 종류 2 가 쓰는 이름 글 (널 포함 여부) 과 `lpnLengthNeeded` 단위, 버퍼가 작을 때 반환 · `LastError` (`ERROR_INSUFFICIENT_BUFFER` 122 = `0x7a` 인가).
   - 못 하면 `[공백]` 으로 두고 1·2 만.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| user32 | `GetUserObjectInformationW` · `GetUserObjectInformationA` | 1 | 질문 1 |
| combase | `GetThreadWinstaDesktop` · `ResolveProcessDesktopNoUser` · (이어지면) `SetExplicitWinstaDesktopString` | 1 | 질문 2 |
| win32kfull 또는 win32kbase | `NtUserGetObjectInformation` (+ 바로 아래 도우미 하나) | 0~1 | 질문 3 (되면) |

- 범위: 묻기·출력 꼴·실패 값·글 만들기까지. RPCSS 연결 본문 (epmapper — run129·run133 에 있음)·창 스테이션 객체 본문·보안 검사 본문은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/134_winsta-name_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1) `syscall · 인자 차례 · 뜻 · 입력/출력 · 단위 · 줄` · (2) `차례 · 부르는 것 · 핸들 · 종류 · 버퍼 크기 · 실패 때 · 돌려주는 값 · 줄` · (3) 종류 1 · 2 출력 칸 표.
  4. PDB GUID 확인 줄 (export 한 모듈 — 이미 쓰는 프로젝트와 같은지, 새로 analyze 한 win32k 는 GUID).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall` 갈래 gui 의 T1 — 새 몸 `src/gui/nt_user_get_object_information.cpp` (종류 2 · 1 의 출력 · 길이 · 반환) · `nt_user_get_process_window_station.cpp` · `nt_user_get_thread_desktop.cpp` 의 `TODO(VM)` 줄. stub 이 돌려줄 값 (반환 · 쓸 출력 칸 · 길이) 을 줄로. 이름 글의 대소문자 (`WinSta0` / `Winsta0`) 는 정적으로 안 정해지면 `[공백]` (run44 `[관찰]` 값만 가리킴).
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** (VM 에서 `GetUserObjectInformationW` 한 번 — 멈춤점 `caller+0x…`) 과 **"다음 요청 후보"** (다음 벽 RPCSS 연결 — 2 의 답이 그쪽이면).

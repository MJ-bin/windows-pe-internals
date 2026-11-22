# 128 user32-regclass — 정적 분석 요청

- 날짜: 2026-11-22
- 스프린트: 03_sprint (짐 지도 바퀴 아님 — PE_emulator 요청, test.exe 초기화 범위 밖)
- 대기열: **새 Q240** (PE_emulator 요청 `../PE_emulator/_workspace/05_tuning_planner/298_internals-user32-regclass.md` — "COM 초기화 (STA) 가 창 클래스를 등록할 때 user32·combase 가 커널 답에서 무엇을 읽나", 튜닝 `298_tune10.md` U4a, 샘플 135). 사용자 결정 2026-11-22 "이 세 요청 먼저 가장 우선". **정적만.** (run86 "다음 요청 후보 4" 의 클래스 등록 몫만)
- 대상 빌드: Windows 11 24H2 26100.9457 x64. `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`:
  - `user32.dll` — 이미 analyze 됨 (`ms/disassemble/user32/` 에 초기화 함수들). 다시 analyze 하지 말고 `^이름$` export 만.
  - `combase.dll` (3,716,184 B) — **아직 analyze 안 됨**. PDB 는 `ms/sym/combase.dll.pdb` 에 있음 (GUID 확인). 필요한 함수만 export.
  - `win32u.dll` — 번호만 이미 있음 (`ms/disassemble/win32u/_syscalls.tsv`: `NtUserRegisterClassExWOW` `0x10a2` · `NtUserUnregisterClass` `0x10ad` · `NtUserRegisterWindowMessage` `0x1032` · `NtUserCreateWindowEx` `0x106f`).
  - 커널 (win32kfull) 쪽 본문은 이번 범위 아님 — 이름만.
- 이어 쓰는 것: run86 (`runs/03_sprint/run86-*.md`, `NtGdiInit2` 임시 stub 이 호출 쪽 읽는 자리로 맞춘 선례), run85 (`gSharedInfo`/`SHAREDINFO`).
- 주의: 같은 시간에 다른 decompiler 가 **npfs·kernelbase·ntoskrnl** (sast/129) 을 돈다 — 그쪽은 건드리지 않는다. combase analyze 는 서버 메모리를 보고 (지금 여유 약 31 GB) 한 번에 하나.

## 왜 (요청한 쪽 말 요약)

- 에뮬레이터 로그 (진짜 Windows 관찰 아님): `NtUserRegisterWindowMessage` (기본 대답, seq 9941) → `NtUserRegisterClassExWOW(0x7fbff150, 0x7fbff0f0, 0x7fbff0d0, 0x7fbff110, …)` → `NtUserUnregisterClass(0x7fbff200, 0x7ffffcf20000, 0x7fbff228)` → `NtUserRegisterClassExWOW(같은 인자)` (셋 다 기본 대답 = 0, seq 10019 ~ 10021) → .NET `OutOfMemoryException` (10063) → 하드 에러 (10123) → 종료 `0xe0434352`. **135 / 135 같은 꼴.** 로그는 인자 앞 넷만 찍음.
- `NtUserUnregisterClass` 둘째 인자 `0x7ffffcf20000` = combase 올라간 주소 → 부르는 쪽은 combase `[추측]`.
- 요청한 쪽 목적: 임시 stub 이 **값 하나만 돌려줘도 되나**, 출력 인자·공유 메모리까지 써 줘야 하나를 가름. 진짜 클래스 번호 값은 필요 없음.

## 알고 싶은 것

1. **user32 등록 길** — `RegisterClassW` / `RegisterClassExW` (A 판은 이름만) → … → `win32u!NtUserRegisterClassExWOW`:
   - 넘기는 인자 개수·뜻 (어느 것이 입력 구조체 / 이름 `UNICODE_STRING`·`LARGE_STRING` 류 / 메뉴 이름 / 출력). 사이 도우미 이름 (`RegisterClassExWOWW` 류 — `_index`·`pdb_aliases.py` 로 확인).
   - 돌아온 값 (ATOM, 16비트) 으로 하는 일: 0 일 때 오류값 (`SetLastError` 누가·무슨 값 — 커널이 TEB `LastErrorValue` 를 쓴다고 보고 안 쓰는지), 0 아닐 때.
   - **성공 뒤 user32 가 더 읽는 것** — 출력 인자 / `gSharedInfo`·`SHAREDINFO` 칸 / 클래스 캐시 (`gpsi` 류) — 있나 없나, 있으면 칸·줄.
   - → 한 줄 답: "stub 은 0 아닌 ATOM 하나만 돌려주면 되나" 예/아니오.
2. **user32 지움 길** — `UnregisterClassW` → `NtUserUnregisterClass`: 셋째 인자 (출력) 모양 (크기·칸) · 참을 돌려받은 뒤 그 출력으로 하는 일 (메뉴 이름 풀기 등 — stub 이 무엇을 써 줘야 안전한가, 0 으로 두면 무엇을 하나) · 거짓일 때 (오류값 · 부른 쪽에 돌려주는 값).
3. **combase 쪽** — STA 초기화 (`CoInitializeEx` / `OleInitialize` 아래) 에서 창 클래스를 등록하는 함수 (이름 후보: `OXIDEntry`·`CChannelHandle`·`GetOrCreateSTAWindow`·`RegisterWindowClass`·`InitMainThreadWnd` 류 — PDB 이름으로 확인, 지어내지 말 것):
   - "등록 → 지움 → 등록" 차례가 코드에 맞나 (첫 등록 실패 → 지우고 다시 — 그 조건 줄).
   - 둘째 등록도 실패하면 돌려주는 HRESULT (`E_OUTOFMEMORY` `0x8007000e` 인가) — CLR 이 메모리 부족으로 읽는 값인지 이어지는 줄.
   - **성공하면 바로 다음에 부르는 user32 함수와 인자** (`CreateWindowExW` — 클래스 · 부모 `HWND_MESSAGE` (= -3) 인가 · 스타일) 와 그 실패가 치명인가.
   - 앞의 `RegisterWindowMessageW` 가 0 이면 치명인가 (부르는 줄과 0 견주기).
   - 클래스 이름 문자열 (`.rdata` 그대로, 예 `OleMainThreadWndClass`) 이 있으면.

## 대상

| 모듈 | 함수 | depth | 이유 |
|---|---|---|---|
| user32 | `RegisterClassW`·`RegisterClassExW` → `NtUserRegisterClassExWOW` 사이 도우미 | 1 | 질문 1 |
| user32 | `UnregisterClassW` (→ `NtUserUnregisterClass`) | 1 | 질문 2 |
| combase | STA 창 클래스 등록 함수 · 그것을 부르는 STA 초기화 한 단계 · 등록 뒤 창 만들기 줄 | 0~1 | 질문 3 |

- 범위: 등록·지움·창 만들기 부르는 줄까지. 창 만들기 (`CreateWindowExW`) 본문·메시지 고리·win32kfull 본문은 이름만.

## 받고 싶은 것

- 결과서 `_workspace/sast/128_user32-regclass_result.md`.
  1. **asm 을 남겨 줌**: `ms/disassemble/<모듈>/<함수>.asm`. 근거는 `[정적] ms/disassemble/<모듈>/<함수>.asm +오프셋` 꼴.
  2. 맨 위 **"먼저 읽는 한 줄 답"** (질문마다 한 줄).
  3. 표: (1·2) `syscall · 인자 차례 · 뜻 · 입력/출력 · 돌아온 뒤 읽는 칸 · 줄` · (3) `차례 · 부르는 것 · 실패 때 · 돌려주는 값 · 줄`.
  4. PDB GUID 확인 줄 (combase).
- **"지금 우리 (PE_emulator) 맞나·틀리나"** 한 줄씩 — 쓰는 곳: `ext/syscall` 갈래 gui `syscall_layer_cpp/src/gui/` (새 임시 stub, `nt_gdi_init2.cpp` 곁) · `src/layer.cpp:740` 곁 · `include/syscall_layer/layer.hpp` · `tests/user32_stub.cpp`. stub 이 돌려줄 값 (ATOM 0 아님 · TRUE) 과 써 줄 출력 칸을 줄로.
- 정적으로 못 닫은 줄은 `[추측]`·`[공백]`. 끝에 **"동적으로 확인할 것"** 과 **"다음 요청 후보"** (창 만들기 `NtUserCreateWindowEx` 가 읽는 칸 — 3 의 답이 메시지 전용 창이면).

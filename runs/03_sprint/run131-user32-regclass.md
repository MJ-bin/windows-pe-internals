# 2026-11-22 (작성일, 정적 분석 2026-11-22) run131 — 창 클래스 등록 (Q240, 정적만): combase STA 초기화는 `RegisterClassW("OleMainThreadWndClass")` → (0 이면) `UnregisterClassW` → `RegisterClassW` 다시 → `CreateWindowExW(…, HWND_MESSAGE)` 차례이고, user32 는 `NtUserRegisterClassExWOW` 의 **돌려준 값 16비트 (AX) 하나만** 본다. 둘째 등록도 0 이고 `GetLastError() == 0` 이면 combase 가 **`0x8007000e` (E_OUTOFMEMORY)** 를 만들어 `CoInitializeEx` 가 그대로 돌려준다. `NtUserUnregisterClass` 셋째 인자 (24 B) 는 user32 가 미리 0 으로 채우는 출력이고, 돌려준 값이 정확히 1 일 때만 그 앞 두 칸을 힙 해제한다

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 **PE_emulator 요청** ("COM 초기화 (STA) 가 창 클래스를 등록할 때 user32 · combase 가 커널 답에서 무엇을 읽나", 원 요청 `../PE_emulator/_workspace/05_tuning_planner/298_internals-user32-regclass.md`, 튜닝 `298_tune10.md` U4a, 샘플 135) 이고 test.exe 초기화 (프로세스 생성 → `NtContinue` 은퇴) **밖**이다. test.exe 는 user32 · combase 를 안 올린다 — 초기화 끝 (P2) 모듈 6개 (test.exe · ntdll · KERNEL32 · KERNELBASE · dll1 · dll2) 에 없음 `[관찰]` (`ms/logs/04_baggage-read_P2.txt:3398 ~ 3923`, run85 · run129 와 같은 줄), `sample/*.c` 에 COM · 창 API 가 없다 `[소스]` (`sample/log.h:3` "user32(wsprintf)도 안 쓴다"). 그래서 **그림은 안 바꿨다.** flow.md 는 기준 (PDB 줄) · 3장 run85 절 한 줄 · 3장 끝 새 절 하나 · 4장 (run86 줄 ④ 일부 닫힘 · Q240 닫힘 줄 · run131 줄 셋) · 5장만 고쳤고, 짐 지도 (`baggage.md`) 는 건드리지 않았다.

> **대기열**: **Q240** (사용자 결정 2026-11-22 "이 세 요청 먼저 가장 우선"). 결과서 판정 **분석성공** — 요청서 물음 1 · 2 · 3 모두 줄 근거로 답함. 덧붙임으로 원 요청의 물음 4 (`NtUserGetAtomName`) 는 닫고, 물음 5 (`NtUserCreateWindowEx`) 는 사슬 · 진입 줄만 (본격은 다음 요청).
> - 이번에 일부 닫는 앞 run 몫: **run86** "다음 요청 후보 4" (= 4장 run86 정적 ④, 대기열 Q163 ④ "창 만들기") 의 **클래스 등록 몫만**. `CreateWindowExW` · 메시지 루프가 읽는 `gSharedInfo` 칸은 그대로 남음.

## 요약

- **등록 길** `[정적]`: combase `RegisterOleWndClass +0x61` → (지연 가져오기) user32 `RegisterClassW +0x6a` → `RegisterClassExWOWW +0x236` → win32u `NtUserRegisterClassExWOW` (`0x10a2`, 인자 **7개**). combase 는 `RegisterClassExW` 가 아니라 **`RegisterClassW`** 를 부른다 (둘 다 같은 도우미 · 같은 syscall).
- **돌아온 뒤 user32 가 읽는 것** `[정적]`: **AX 만** (`RegisterClassExWOWW +0x242 MOVZX EDI,AX`, `+0x265 TEST`). 출력 인자 없음, `gSharedInfo` · `gpsi` · 클래스 캐시를 이 길에서 안 읽는다. 0 이어도 user32 는 `SetLastError` 를 안 쓴다 (커널이 쓴 TEB 값 그대로).
- **지움 길** `[정적]`: `UnregisterClassW +0xd0` → `NtUserUnregisterClass` (`0x10ad`, 인자 3개). 셋째 = **출력 24 B** (`CLSMENUNAME` 모양), user32 가 부르기 전에 0 으로 채운다. 돌려준 값이 **정확히 1** 이면 앞 두 칸이 힙 포인터일 때만 `RtlFreeHeap`. 그 밖엔 아무것도 안 하고, 커널 값을 그대로 돌려준다.
- **combase 차례** `[정적]`: `CoInitializeEx` → `_CoInitializeEx +0x34f` → `ThreadFirstInitialize` → (프로세스 첫 클래식 STA 일 때만) `+0x304 RegisterOleWndClass` → 등록 0 이면 `UnregisterClassW` (결과 안 봄) → 다시 `RegisterClassW` → 성공이고 `gdwMainThreadId == 0` 이면 `+0x2be InitMainThreadWnd` → `CreateWindowExW(0, ATOM, L"OleMainThreadWndName", 0x88000000, CW_USEDEFAULT ×4, HWND_MESSAGE (-3), 0, g_hinst, 0)`.
- **실패 값** `[정적]`: 둘째 등록 0 → `GetLastError()` 가 0 이면 **`0x8007000e`**, 양수면 `0x80070000 | (e & 0xffff)`, 음수면 그대로 → `ThreadFirstInitialize` → `CoInitializeEx` 반환. 창 만들기 실패도 같은 규칙.
- **`RegisterWindowMessageW`** `[정적]`: combase STA 길에 **없다**. user32 에서 `RegisterWindowMessageW` 는 `RegisterClipboardFormatW` 와 같은 함수 (rva `0x4ce60`). combase 가 부르는 곳은 `CLIPFORMAT_UserUnmarshal(64)` 둘뿐. 요청 쪽 로그 seq 9941 의 호출자는 `[공백]`.
- **`NtUserGetAtomName`** `[정적]`: 클래스가 아톰일 때 `ClassNameToVersion +0x226` 이 부름, 인자 2개, 반환 = **글자 수**. 0 이면 `NtUserCreateWindowEx` 를 안 부르고 NULL.

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든아홉째 바퀴. plan **Q240**. **짐 지도 바퀴 아님.**
- 정적: **처음 analyze** 는 combase 하나 (MAXMEM 10G, 약 2분 반 — 다른 decompiler 의 JVM 하나와 같이 돌아 한 번에 하나). user32 · win32u 는 다시 analyze 안 하고 `^이름$` export 만.

| 모듈 | 바이너리 (`System32\`) | PDB guid+age | 함수 수 |
|---|---|---|---|
| combase | `combase.dll` 3,716,184 B | `DA1DC0C4F35CEE58F672CBB20460D11D1` | 16,607 (`FUN_` 632) |
| user32 | `user32.dll` (run85 때 analyze) | `CDB947D1B96B87977915DE04CF54279C1` | — |
| win32u | `win32u.dll` (run85 때 analyze) | `6295787D0B7E537E98F77F31F4426D1C1` | — |

- combase PDB GUID 는 `llvm-pdbutil dump --summary` 의 `{DA1DC0C4-F35C-EE58-F672-CBB20460D11D}` 와 같음. PDB 안 Age 4 · 심볼 서버 키는 바이너리 Age 1 (run129 winnsi 와 같은 모양). `ms/ghidra/combase.analyze.log` 에 `SetPdb:` 줄 있음. 별칭 표 `ms/sym/combase.dll.aliases.tsv` (별칭 무리 1,118곳).
- combase 는 창 함수를 **지연 가져오기** 로 쓴다 (`ext-ms-win-rtcore-ntuser-window-ext-l1-1-0` → `apisetschema.dll` 에서 `…-l1-1-1 → user32.dll`). 첫 호출 때 `DelayLoad_RegisterClassW` (rva `0x1792a9`) → 공용 꼬리 `__tailMerge_ext_ms_win_rtcore_ntuser_window_ext_l1_1_0_dll` (rva `0x1790c2`). 그 안의 로딩 코드는 안 읽음 `[공백]`.
- 버전 대조 (결과서): combase 는 "알려진 사실" 표에 없어서, 요청 쪽 에뮬레이터 로그의 **스택 주소 여섯 개** 를 `.asm` 의 스택 칸 셈과 맞췄다 — 전부 일치 (등록 인자 넷: WNDCLASSEXW 복사 `RBP-0x40` · 이름 USTR `RSP+0x60` · 버전 이름 USTR `RSP+0x40` · `CLSMENUNAME` `RBP-0x80`, 지움 인자 1 · 3 차이 `0x28`). 에뮬레이터가 진짜 user32 코드를 돌린 것이라 "오프셋 셈 · 바이너리를 믿어도 된다" 는 근거이지, 진짜 Windows `[관찰]` 은 아니다.

## 목적

- PE_emulator 에서 샘플 135 개가 같은 꼴로 죽는다 (요청 쪽 에뮬레이터 로그 — 진짜 Windows 관찰 아님): `NtUserRegisterWindowMessage` (기본 대답, seq 9941) → `NtUserRegisterClassExWOW` → `NtUserUnregisterClass(…, 0x7ffffcf20000 = combase, …)` → `NtUserRegisterClassExWOW` (셋 다 기본 대답 0) → .NET `OutOfMemoryException` → 하드 에러 → 종료 `0xe0434352`. 요청서 `_workspace/sast/128_user32-regclass.md`:
  1. user32 등록 길 — 인자 · 돌아온 값으로 하는 일 · 성공 뒤 더 읽는 칸 → "stub 은 0 아닌 ATOM 하나만 돌려주면 되나".
  2. user32 지움 길 — 셋째 인자 (출력) 모양 · 참 / 거짓일 때 하는 일.
  3. combase — "등록 → 지움 → 등록" 조건, 둘째 실패 HRESULT, 성공 뒤 `CreateWindowExW` 인자 · 치명 여부, `RegisterWindowMessageW` 0 이 치명인가, 클래스 이름.
- 쓰는 곳: PE_emulator 임시 stub (`NtUserRegisterClassExWOW` · `NtUserUnregisterClass`, run86 `NtGdiInit2` stub 꼴) 이 **값 하나만 돌려줘도 되나, 출력 인자 · 공유 메모리까지 써 줘야 하나**.

## 방법

- 정적 sast/128:
  - combase 처음 analyze → `export` depth 0 11개 (`RegisterOleWndClass` · `UnRegisterOleWndClass` · `InitMainThreadWnd` · `UninitMainThreadWnd` · `GetOrCreateSTAWindow` · `OleMainThreadWndProc` · `ThreadWndProc` · `ThreadFirstInitialize` · `CoInitializeEx` · `_CoInitializeEx` · `IsCreateWindowExWPresent`) + `DelayLoad_RegisterClassW`.
  - user32 `^이름$` export 17개 (`RegisterClassW` · `RegisterClassExW` · `RegisterClassA` · `RegisterClassExA` · `RegisterClassExWOWW` · `RegisterClassExWOWA` · `UnregisterClassW` · `UnregisterClassA` · `VersionRegisterClass` + depth 1 `InitClsMenuNameW` · `ClassNameToVersion` · `RegisterDefaultClass` · `RtlGetExpWinVer` …) + depth 0 `RegisterClipboardFormatW` · `CreateWindowExW` · `CreateWindowInternal` · `VerNtUserCreateWindowEx`.
  - win32u 스텁 넷 (depth 0). `refs`: combase 가져오기 칸 `0x3635c8` (`RegisterClassW`) · `0x3635d0` (`UnregisterClassW`) · `0x363630` (`CreateWindowExW`) · `0x363540` (`RegisterClipboardFormatW`), user32 `0x4ce60` · `0xb5eb8`.
  - 일회용 읽기 (파일로 안 남김): `apisetschema.dll` `.apiset`, combase `.rdata` UTF-16 문자열.
- 결과서: `_workspace/sast/128_user32-regclass_result.md` (**분석성공**, 스크립트 문제 없음 — Ghidra `.c` 가 틀어 보인 곳 넷은 `.asm` 으로 읽음).
- flow-writer 가 다시 본 것:
  - `_calls.tsv` call · ret 짝 — user32 (`RegisterClassW` · `RegisterClassExW` · `RegisterClassExWOWW` · `RegisterDefaultClass` · `UnregisterClassW` · `ClassNameToVersion` · `RegisterClipboardFormatW` · `CreateWindowExW` · `CreateWindowInternal` · `VerNtUserCreateWindowEx`), combase (`CoInitializeEx` · `_CoInitializeEx` · `ThreadFirstInitialize` · `RegisterOleWndClass` · `UnRegisterOleWndClass` · `InitMainThreadWnd` · `IsCreateWindowExWPresent`) — **결과서와 모두 같음**.
  - `.asm` 줄: `RegisterClassW +0x22` · `+0x39` · `+0x54`, `RegisterClassExWOWW +0x9b` · `+0xa4` · `+0x153 ~ +0x170` · `+0x215 ~ +0x231` · `+0x242` · `+0x265` · `+0x268` · `+0x326`, `RegisterDefaultClass +0x23 ~ +0x76` · `+0x21e ~ +0x231`, `UnregisterClassW +0xc8` · `+0xdc ~ +0xf0` · `+0x13c`, `ClassNameToVersion +0x98` · `+0x9f` · `+0x21b ~ +0x239`, `VerNtUserCreateWindowEx +0x17c ~ +0x1e1` · `+0x26f`, combase `RegisterOleWndClass +0x6d ~ +0x7a` · `+0x1397e8` · `+0x1397fa ~ +0x139810`, `InitMainThreadWnd +0x1b` · `+0x21` · `+0x38` · `+0x66`, `ThreadFirstInitialize +0x278 ~ +0x30f`, `CoInitializeEx +0x33` · `+0x37` · `+0xb2`, `UnRegisterOleWndClass +0x22`. 결과서와 다른 곳 → 아래 "flow-writer 대조".
  - `pdb_aliases.py --lookup`: combase `0x179074` = **별칭 무리 3개** (`IsCreateWindowExWPresent` · `IsIsGUIThreadPresent` · `IsInSendMessageExPresent`) → 글에서는 "API 세트가 있나 묻는 함수" 로 적는다. combase `0x12e52c` (`RegisterOleWndClass`) · user32 `0x1c99c` (`RegisterClassExWOWW`) · `0x4ce60` 은 별칭 무리 아님. `_calls.tsv` 의 다른 `alias:X+1` 은 같은 함수의 C++ 장식 이름 하나 (`.asm` `ALIASES(1)`) 라 PDB 이름을 그대로 썼다. `0x4ce60` 의 두 이름 (`RegisterWindowMessageW` · `RegisterClipboardFormatW`) 은 PDB 별칭이 아니라 **내보내기 이름 둘** (서수 2245 · 2270, 결과서).
- 앞 바퀴 짝: run85 (sast/82 — user32 처음 analyze, `gSharedInfo` · `gpsi` · `ClientThreadSetup`), run86 (sast/83 — gdi32 · gdi32full, `NtGdiInit2` stub 이 호출 쪽 읽는 자리로 맞춘 선례).

## 결과

비유:

- 창 클래스 등록은 **동사무소에 "우리 가게 이름" 을 올리는 것** 이다. 동사무소 (커널) 는 번호표 (ATOM) 한 장만 돌려준다. user32 는 번호표에 숫자가 있나만 보고, 다른 서류는 안 받는다.
- combase 는 번호표를 못 받으면 "혹시 예전 이름이 남았나" 하고 **지우고 한 번 더** 신청한다. 또 못 받으면 "왜 안 됐어요?" (`GetLastError`) 를 묻는데, 대답이 없으면 (0) **"메모리가 모자랐나 보다"** 로 적는다 `[추측: 비유]`.
- 지울 때 동사무소가 "돌려줄 서류 봉투" (24 B) 에 뭘 넣어 주면 user32 는 그걸 버리러 간다. **빈 봉투** 면 그냥 지나간다 — 그래서 아무것도 안 넣는 게 안전하다 `[추측: 비유]`.

### 정적 — 호출 지점 (`ms/disassemble/{combase,user32}/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `combase!CoInitializeEx` (rva `0x309e0`) | `+0x44` | `+0x49` | `_CoInitializeEx` | [정적] |
| 〃 | `+0xa9` | `+0xae` | API 세트가 있나 묻는 함수 (rva `0x179074` — 같은 주소 이름 3개, 후보: `IsCreateWindowExWPresent` · `IsIsGUIThreadPresent` · `IsInSendMessageExPresent`) — STA 일 때 (`+0x33 TEST DIL,0xa`) | [정적] |
| 위 함수 (`0x179074`) | `+0x25` | `+0x2a` | `ApiSetQueryApiSetPresence(L"ext-ms-win-rtcore-ntuser-window-ext-l1-1-0")` | [정적] |
| `combase!_CoInitializeEx` | `+0x34f` | `+0x354` | `ThreadFirstInitialize` (rva `0x31518`) | [정적] |
| `combase!ThreadFirstInitialize` | `+0x22b` | `+0x232` | `HeapAlloc(g_hHeap, 0, 0x1b0)` (STA 갈래) | [정적] |
| 〃 | **`+0x304`** | **`+0x309`** | `RegisterOleWndClass` (`g_cClassicSTAInits` 가 1 이 됐을 때만) | [정적] |
| 〃 | **`+0x2be`** | **`+0x2c3`** | `InitMainThreadWnd` (`gdwMainThreadId == 0` 일 때만) | [정적] |
| 〃 | `+0x2d2` | `+0x2d7` | `UnRegisterOleWndClass` (창 만들기 실패 뒤 `g_cClassicSTAInits` 가 0 이 되면) | [정적] |
| `combase!RegisterOleWndClass` (rva `0x12e52c`) | **`+0x61`** | **`+0x68`** | `RegisterClassW` (가져오기 칸 `0x1803635c8`) — ① | [정적] |
| 〃 | `+0x1397be` | `+0x1397c5` | `UnregisterClassW(L"OleMainThreadWndClass", g_hinst)` — ② | [정적] |
| 〃 | `+0x1397cf` | `+0x1397d6` | `RegisterClassW` — ③ | [정적] |
| 〃 | `+0x1397ee` | `+0x1397f5` | `GetLastError` (③ 실패 때) | [정적] |
| `combase!UnRegisterOleWndClass` (rva `0x13796c`) | `+0x12` | `+0x19` | `UnregisterClassW` | [정적] |
| `combase!InitMainThreadWnd` (rva `0x12f250`) | **`+0x50`** | **`+0x57`** | `CreateWindowExW` — ④ | [정적] |
| 〃 | `+0x68` · `+0x85` | `+0x6f` · `+0x8c` | `GetCurrentThreadId` (성공) · `GetLastError` (실패) | [정적] |
| `user32!RegisterClassW` | `+0x6a` | `+0x6f` | `RegisterClassExWOWW` (rva `0x1c99c`) | [정적] |
| `user32!RegisterClassExW` | `+0x1a` | `+0x1f` | `RegisterClassExWOWW` (`cbSize ≠ 0x50` 이면 `+0x2a SetLastError(0x57)`) | [정적] |
| `user32!RegisterClassExWOWW` | `+0xb8` | `+0xbd` | `RegisterDefaultClass` | [정적] |
| 〃 | `+0x143` | `+0x148` | `InitClsMenuNameW` (`CLSMENUNAME` 채움) | [정적] |
| 〃 | `+0x1b2` | `+0x1b7` | `ClassNameToVersion` | [정적] |
| 〃 | **`+0x236`** | **`+0x23d`** | **`NtUserRegisterClassExWOW`** (win32u `0x10a2`) | [정적] |
| 〃 | `+0x255` | `+0x25a` | `RtlGetExpWinVer` (돌아온 뒤 — 이미지 머리만) | [정적] |
| 〃 | `+0x32b` | `+0x332` | `RtlSetLastWin32Error(0x57)` (커널 부르기 **전** 거르는 경우만) | [정적] |
| `user32!UnregisterClassW` (rva `0x1cf30`) | `+0x87` | `+0x8c` | `ClassNameToVersion` | [정적] |
| 〃 | **`+0xd0`** | **`+0xd7`** | **`NtUserUnregisterClass`** (win32u `0x10ad`) | [정적] |
| 〃 | `+0xfb` · `+0x11a` | `+0x102` · `+0x121` | `RtlFreeHeap` ×2 (돌려준 값 1 이고 칸이 힙 포인터일 때만) | [정적] |
| `user32!ClassNameToVersion` (rva `0x1d0e0`) | `+0x131` | `+0x138` | `RtlFindActivationContextSectionString(…, 3, …)` | [정적] |
| 〃 | `+0x226` | `+0x22d` | `NtUserGetAtomName` (win32u `0x109c`, 아톰일 때만) | [정적] |
| `user32!RegisterClipboardFormatW` (= `RegisterWindowMessageW`, rva `0x4ce60`) | `+0x35` | `+0x3c` | `NtUserRegisterWindowMessage` (win32u `0x1032`) | [정적] |
| `user32!CreateWindowExW` (rva `0x1c110`) | `+0x7d` | `+0x82` | `CreateWindowInternal` (rva `0x1c1a4`) | [정적] |
| `user32!CreateWindowInternal` | `+0x1de` | `+0x1e3` | `VerNtUserCreateWindowEx` (rva `0x1c5c4`) | [정적] |
| `user32!VerNtUserCreateWindowEx` | `+0x177` | `+0x17c` | `ClassNameToVersion` | [정적] |
| 〃 | `+0x22f` | `+0x236` | `NtUserCreateWindowEx` (win32u `0x106f`, 인자 17개) | [정적] |
| 〃 | `+0x263` · `+0x288` · `+0x2d2` | `+0x26a` · `+0x28d` · `+0x2d7` | `GetLastError` · `RegisterDefaultClass` · `VersionRegisterClass` (오류 `0x57f` 일 때 다시 시도) | [정적] |

### 1. user32 등록 길 — `NtUserRegisterClassExWOW` 인자 7개 [정적]

- 도우미 `RegisterClassExWOWW` (PDB 시그니처 `(WNDCLASSEXW*, WOWCLS*, WORD, DWORD, int)`). `RegisterClassW` · `RegisterClassExW` 둘 다 WORD 0 · DWORD `0x80` · int 1 을 넘긴다 (`RegisterClassW +0x22 XOR R8D` · `+0x39 MOV R9D,0x80` · `+0x54 [RSP+0x20]=1`). WOWCLS 인자는 안 놓고 안 쓴다.
- int 1 = "기본 클래스 먼저 등록" 켜기: `+0x9b` 에서 보고 `+0xa4` 의 `gdwRegisteredClassesMask` · `gdwRegisteredClasses` 를 견준 뒤 `+0xb8 RegisterDefaultClass`. 이 함수는 잠금 (`gcsClassRegister`) 안에서 기본 클래스 17개 이름과 견줄 뿐이고, `OleMainThreadWndClass` 는 그 이름이 아니라 커널을 안 부르고 1 을 돌려준다 (결과서). `gpsi` (`+0x364` 아톰 표) 를 읽는 줄은 **`RegisterDefaultClass +0x21e`** — 이름이 정수 아톰일 때만 탄다 (아래 "flow-writer 대조").

| # | 놓는 줄 | 뜻 | 입력/출력 | 돌아온 뒤 읽나 |
|---|---|---|---|---|
| 1 | `+0x228 LEA RCX,[RBP-0x40]` | `WNDCLASSEXW` 복사본 0x50 B (user32 스택) | 입력 | 안 읽음 |
| 2 | `+0x21a RDX = [RSP+0x70]` → USTR `RSP+0x60` | 준 클래스 이름 (문자열이면 `RtlInitUnicodeString`, 아톰이면 `Length 0 · Buffer = 아톰`) | 입력 | 안 읽음 |
| 3 | `+0x215 R8 = [RSP+0x50]` → USTR `RSP+0x40` | 버전 이름 (`ClassNameToVersion` 결과 — 매니페스트 바꿔치기 없으면 2 와 같은 글자) | 입력 | 안 읽음 |
| 4 | `+0x21f LEA R9,[RBP-0x80]` | `CLSMENUNAME` 24 B {ANSI 메뉴 이름, 유니코드 메뉴 이름, `UNICODE_STRING*`} — `InitClsMenuNameW` 가 채움 | **입력** (커널이 쓴다는 줄 없음) | 실패 때만 앞 두 칸 해제 |
| 5 | `+0x231 [RSP+0x20] word = SI` | WORD (0) | 입력 | — |
| 6 | `+0x22c [RSP+0x28] dword = R14D` | 플래그 `0x80`, 모듈 `ExpWinVer > 0x30a` 면 `| 0x40` → 보통 `0xC0` (`+0x153 ~ +0x170`). 이름 `[공백]` | 입력 | — |
| 7 | `+0x223 [RSP+0x30] qword = R12 (0)` | 0 (WOW 칸 `[추측]`) | 입력 | — |

- **돌아온 값**: `+0x242 MOVZX EDI,AX` (16비트만) → `+0x255 RtlGetExpWinVer` (모듈 머리의 Subsystem 버전 — `< 0x30a` 옛 모듈이면 0 / 1 로 줄임, combase 는 해당 없음) → `+0x265 TEST RDI,RDI` → 0 아니면 ATOM 돌려줌, 0 이면 `+0x268 JZ` → 메뉴 이름 힙 해제 → 0.
- **user32 가 직접 쓰는 오류는 `0x57` 하나** (`+0x326` · `+0x32b`) — 커널 부르기 **전** 에 거르는 다섯 경우 (기본 클래스 등록 실패 · extra 음수 · user32 자신의 클래스 · 안 쓰는 스타일 비트 · 잘못된 브러시). 커널이 0 을 주면 user32 는 오류값을 안 건드린다.
- **성공 뒤 더 읽는 칸 없음**: `RegisterClassExWOWW` 가 읽는 전역은 `__security_cookie` · `gdwRegisteredClasses(Mask)` · `hmodUser` · `pUserHeap` 뿐 (`.asm` 전역 주석). `gSharedInfo` · `gpsi` 줄 없음.

### 2. user32 지움 길 — `NtUserUnregisterClass` 인자 3개 [정적]

| # | 놓는 줄 | 뜻 | 입력/출력 |
|---|---|---|---|
| 1 | `+0xc3 RCX = [RSP+0x40]` → USTR `RSP+0x30` | 지울 클래스 이름 (`ClassNameToVersion` 결과) | 입력 |
| 2 | `+0xcd RDX = RBX` | `hInstance` (combase 는 `g_hinst`) | 입력 |
| 3 | `+0xc8 LEA R8,[RSP+0x58]` | **24 B 출력** (`RSP+0x58 ~ +0x6f`) — 부르기 전 0 으로 채움 (`+0x4e` · `+0x56` · `+0x7d`) | **출력** |

- 돌아온 뒤: `+0xdc MOV EBX,EAX` · `+0xde MOV EAX,1` · `+0xe3 CMP RBX,RAX` — **정확히 1** 일 때만 `[RSP+0x58]` · `[RSP+0x60]` 이 힙 포인터 (`& 0xffffffffffff0000 ≠ 0`) 면 `RtlFreeHeap(pUserHeap, …)` (`+0xed` · `+0xf0` · `+0xfb`, `+0x10c` · `+0x10f` · `+0x11a`). `[RSP+0x68]` 은 안 읽음.
- 0 칸이면 해제 둘 다 건너뜀. 1 이 아니면 (`+0xe6 JNZ`) 아무것도 안 함, `SetLastError` 없음. 반환은 `+0x13c MOV EAX,EBX` — **커널 값 그대로** (BOOL 로 안 고침).
- 그 밖 (결과서): 이름 풀기 실패면 커널 안 부르고 0 (`+0x8f`). `g_systemCallFilterId == 5` 인 프로세스는 커널을 안 부르고 1 (`+0x28` · `+0x32`, 뜻 `[공백]`).

### 3. combase STA 초기화 — 등록 → 지움 → 등록 → 창 만들기 [정적]

1. `CoInitializeEx +0x33 TEST DIL,0xa` → STA 이면 `+0xa9` 의 "API 세트가 있나" (rva `0x179074`) — 없으면 `+0xb2 MOV EBX,0x80004001` (E_NOTIMPL) 로 실패. 결과는 캐시 칸에 (1 = 있음, 2 = 없음).
2. `ThreadFirstInitialize`: STA 갈래 (`+0x4d` · `+0x51`) → `+0x22b HeapAlloc 0x1b0` (실패 → `0x8007000e`) → `CComApartment` · `FinalConstruct` · `CreateAndFreezeDefaultObjectContext`.
3. 클래식 STA (`+0x278 TEST R14D,0x60000000` 이 0) 면 `+0x281` 에서 `g_cClassicSTAInits` 를 올리고, **1 이 되면** (`+0x28f CMP EAX,EDI` · `+0x291 JZ`) `+0x304 RegisterOleWndClass`.
4. 결과 HRESULT 가 성공 (`+0x30d JNS`) 이면 `+0x293` 에서 `gdwMainThreadId` 를 보고 0 이면 (`+0x29a JZ`) `+0x2be InitMainThreadWnd`.
5. 실패: 등록 실패 → `+0x30f DEC g_cClassicSTAInits` → 정리. 창 만들기 실패 (`+0x2c7 JNS` 안 탐) → `+0x2c9` 내리고 0 이면 `+0x2d2 UnRegisterOleWndClass` → 정리 → 그 HRESULT 를 `ThreadFirstInitialize` 가 돌려주고 `_CoInitializeEx` → `CoInitializeEx` 반환 (결과서 `+0x3c2` · `+0x4f9`).

| 차례 | 부르는 것 | 실패 때 | 돌려주는 값 | 줄 |
|---|---|---|---|---|
| ① | `RegisterClassW(&WNDCLASSW)` — style 0 · `lpfnWndProc = OleMainThreadWndProc` (rva `0x11f200`) · `cbWndExtra 0x14` · `hInstance = g_hinst` · `hbrBackground = 2` · 메뉴 0 · 이름 `L"OleMainThreadWndClass"` | AX = 0 → ② (이유 안 봄) | `+0x6d MOVZX EAX,AX` → `gOleWindowClass` (`0x180338870`) | `+0x61` · `+0x77 TEST RAX,RAX` · `+0x7a JZ +0x1397b4` |
| ② | `UnregisterClassW(L"OleMainThreadWndClass", g_hinst)` | **결과 검사 없음** | — | `+0x1397be` |
| ③ | `RegisterClassW(&같은 구조체)` | AX = 0 → `GetLastError` | 0 아니면 `+0x1397e8 JNZ +0x80` → **S_OK** | `+0x1397cf` |
| ③ 실패 | `GetLastError()` → HRESULT | — | 0 → **`0x8007000e`** · 양수 → `0x80070000 \| (e & 0xffff)` · 음수 → 그대로 | `+0x1397ee` · `+0x1397fa` · `+0x139803 OR` · `+0x13980b MOV EAX,0x8007000e` · `+0x139810 CMOVNS` |
| ④ | `CreateWindowExW(0, gOleWindowClass, L"OleMainThreadWndName", 0x88000000, 0x80000000 ×4, (HWND)-3, 0, g_hinst, 0)` | HWND 0 → `GetLastError` (같은 규칙) | `ghwndOleMainThread` (`0x180338878`) 에 저장, `gdwMainThreadId = GetCurrentThreadId()` → S_OK | `InitMainThreadWnd +0x1b R9D=0x88000000` · `+0x21 RDX=[gOleWindowClass]` · `+0x38 [R11-0x28]=-3` · `+0x50` · `+0x66 JZ` |

- 값 풀이 (공개 상수): `0x88000000` = `WS_POPUP | WS_DISABLED` (`WS_VISIBLE` 없음), `0x80000000` = `CW_USEDEFAULT`, `(HWND)-3` = `HWND_MESSAGE` → **메시지 전용 창**.
- 문자열 (`.rdata`, 결과서): `OleMainThreadWndClass` `0x1802cd830` · `OleMainThreadWndName` `0x1802cd928` · (STA 스레드 창, 이 길 아님) `OLEChannelWnd` `0x1802ccf48`.
- 창 함수 `OleMainThreadWndProc`: `WM_CLOSE` · `WM_DESTROY` 는 부수는 중이 아니면 막고, `0x405` 는 따로, **나머지는 전부 `DefWindowProcW`** — 만드는 동안의 메시지에 combase 가 할 일은 없다 (결과서).
- **지움 결과를 combase 는 안 본다**: ② 는 검사 자체가 없고, `UnRegisterOleWndClass` 는 0 이면 `GetLastError` 만 읽고 (추적용) 어느 쪽이든 `+0x22 AND [gOleWindowClass],0`.
- **`GetOrCreateSTAWindow`** (rva `0x1229b4`, 같은 클래스 · 창 이름 `OLEChannelWnd`) 는 `StartServer` 가 부르는 것 — `CoInitializeEx` 길 아님 (결과서).
- **`OleInitialize`** 는 combase `_index.tsv` 에 없다 (ole32 쪽, 이 요청 밖).

| 전역 (combase) | 주소 | 뜻 |
|---|---|---|
| `g_hinst` | `0x180337fb0` | combase 모듈 핸들 (요청 쪽 로그의 `0x7ffffcf20000`) |
| `g_cClassicSTAInits` | `0x180338008` | 클래식 STA 초기화 횟수 |
| `gdwMainThreadId` | `0x180338864` | 숨은 창을 만든 스레드 ID |
| `gfDestroyingMainWindow` | `0x180338868` | 숨은 창 부수는 중 |
| `gOleWindowClass` | `0x180338870` | 등록 ATOM |
| `ghwndOleMainThread` | `0x180338878` | 숨은 창 핸들 |

### 4. `RegisterWindowMessageW` · `NtUserGetAtomName` · `NtUserCreateWindowEx` (덧붙임) [정적]

- **`RegisterWindowMessageW`**: `RtlInitUnicodeString` (`+0x24`) → `NtUserRegisterWindowMessage` (`+0x35`) → UINT 그대로 (오류값 안 씀). user32 안에서 이 syscall 을 부르는 곳은 `RegisterClipboardFormatW +0x35` · `RegisterClipboardFormatA +0xf3` 둘. combase 는 `CLIPFORMAT_UserUnmarshal +0xad` · `CLIPFORMAT_UserUnmarshal64 +0x1f7` 둘뿐 — STA 초기화 · 등록 길과 무관. → 요청 쪽 seq 9941 의 0 은 이 막힘의 원인이 아니다. 누가 불렀는지는 `[공백]`.
- **`NtUserGetAtomName`** (`0x109c`): `ClassNameToVersion +0x226` — 이름이 아톰일 때만 (`+0x98 TEST RBX,-0x10000` · `+0x9f JZ`). 인자 2개 (`+0x21f MOVZX ECX,BX` 아톰, RDX = USTR — `+0x21b MaximumLength 0x200`, 버퍼는 `pUserHeap` 에서 0x200 B, 0 으로 안 채움). 반환 = **글자 수** → `+0x232 ADD AX,AX` · `+0x235` 로 user32 가 `Length` 를 직접 씀. 0 이면 `ClassNameToVersion` NULL → `VerNtUserCreateWindowEx +0x17f` · `+0x182 JZ` → 커널 `NtUserCreateWindowEx` 를 안 부르고 NULL. 받은 이름은 `RtlFindActivationContextSectionString(…, 3 = 창 클래스 바꿔치기, …)` 의 열쇠 — NOT_FOUND 계열 (`0xC0150001` · `0xC0150008`) 이면 원래 아톰 그대로.
- **`NtUserCreateWindowEx`** (사슬만): `CreateWindowExW +0x7d` → `CreateWindowInternal +0x1de` → `VerNtUserCreateWindowEx +0x22f` (인자 17개, `+0x1cb ~ +0x22b`). 둘째 = 준 클래스 (`+0x1e1 MOV RDX,RDI`), 셋째 = `ClassNameToVersion` 결과 (`+0x17c MOV R14,RAX` → `+0x1c1 MOV R8,R14`) — 바꿔치기가 없으면 같은 아톰 둘. 0 을 받으면 `GetLastError` 가 `0x57f` (`+0x26f`) 일 때만 기본 클래스 / 버전 클래스로 다시 시도, 그 밖 (오류 0 포함) 은 NULL → combase `InitMainThreadWnd +0x66` ④ 실패. 받은 핸들 쪽 갈래 (`CreateWindowInternal +0x1e3 ~ +0x234`, `ValidateHwnd`) 는 `[정적, 대충 읽음]` — 다음 요청.

### 호출 관계

```
combase!CoInitializeEx (0x309e0)                                                    [정적]
 ├─ +0xae   (API 세트 있나, 0x179074) → +0x2a ApiSetQueryApiSetPresence(…ntuser-window-ext-l1-1-0)   없으면 +0xb2 0x80004001
 └─ +0x49   _CoInitializeEx
            └─ +0x354  ThreadFirstInitialize (0x31518)
                        ├─ +0x232  HeapAlloc(0x1b0)               실패 → 0x8007000e
                        ├─ +0x309  RegisterOleWndClass (0x12e52c)   ← g_cClassicSTAInits 가 1 이 됐을 때
                        │           ├─ ① +0x68       RegisterClassW ⇢ (지연 가져오기) user32!RegisterClassW
                        │           │                 └─ +0x6f  RegisterClassExWOWW (0x1c99c)
                        │           │                      ├─ +0xbd  RegisterDefaultClass   (17개 이름 견줌, 커널 안 부름)
                        │           │                      ├─ +0x148 InitClsMenuNameW · +0x1b7 ClassNameToVersion
                        │           │                      └─ +0x23d NtUserRegisterClassExWOW   ── SYSCALL [win32u 0x10a2] 인자 7, AX 만 봄
                        │           ├─ ② +0x1397c5   UnregisterClassW   (①이 0 일 때, 결과 안 봄)
                        │           │                 └─ +0xd7  NtUserUnregisterClass       ── SYSCALL [win32u 0x10ad] 셋째 = 24 B 출력, 1 일 때만 해제
                        │           ├─ ③ +0x1397d6   RegisterClassW     (같은 길)
                        │           └─    +0x1397f5   GetLastError → 0 이면 0x8007000e
                        ├─ +0x2c3  InitMainThreadWnd (0x12f250)     ← 등록 성공 · gdwMainThreadId == 0
                        │           └─ ④ +0x57  CreateWindowExW(…, 0x88000000, …, HWND_MESSAGE, …)
                        │                        └─ +0x82 CreateWindowInternal → +0x1e3 VerNtUserCreateWindowEx
                        │                             ├─ +0x17c ClassNameToVersion → (아톰이면) +0x22d NtUserGetAtomName [0x109c]
                        │                             └─ +0x236 NtUserCreateWindowEx  ── SYSCALL [win32u 0x106f] 인자 17
                        └─ +0x2d7  UnRegisterOleWndClass             ← ④ 실패 뒤 g_cClassicSTAInits 가 0 이면
                                    └─ +0x19 UnregisterClassW
```

- (나무 오프셋은 ret 자리. `⇢` 는 지연 가져오기 발판 `DelayLoad_RegisterClassW` → 공용 꼬리 — 그 안은 안 읽음. 콜스택으로 본 적 없음.)

## 확인된 것

- `[정적]` combase 의 창 클래스 등록은 `ThreadFirstInitialize +0x304 RegisterOleWndClass` 하나 — `RegisterClassW` 가져오기 칸 `0x3635c8` 을 읽는 곳이 이 함수뿐 (`refs`, 결과서). 이름이 비슷한 `NdrpRegisterClass` 류는 창 클래스와 무관.
- `[정적]` "등록 → 지움 → 등록" 조건 = 첫 `RegisterClassW` 가 0 (`RegisterOleWndClass +0x77` · `+0x7a`). 지움 결과 · 오류값은 안 본다.
- `[정적]` user32 등록 길은 돌아온 값 AX 하나만 쓴다 (`RegisterClassExWOWW +0x242` · `+0x265`). 출력 인자 없음, 성공 뒤 `gSharedInfo` · `gpsi` · 클래스 캐시 안 읽음. 0 이면 user32 는 오류값을 안 쓴다.
- `[정적]` 둘째 등록 실패 HRESULT = `GetLastError()` 0 → `0x8007000e` (`RegisterOleWndClass +0x13980b` · `+0x139810`). `CoInitializeEx` 까지 그대로 간다. 요청 쪽 로그 (오류값 안 씀 → `OutOfMemoryException`) 와 맞는다 — CLR 이 이 값을 메모리 부족 예외로 옮기는 줄은 이 레포에 clr 이 없어 `[공백]` (`HRESULT_FROM_WIN32(14)` = E_OUTOFMEMORY 를 메모리 부족으로 읽는 것은 흔한 규칙 `[추측: 지식]`).
- `[정적]` `NtUserUnregisterClass` 셋째 인자는 user32 가 0 으로 채운 24 B 출력 — 1 일 때만 앞 두 칸을 힙 해제 (`UnregisterClassW +0xde` · `+0xe3` · `+0xe8 ~ +0x121`). 반환은 커널 값 그대로 (`+0x13c`).
- `[정적]` 성공 뒤 바로 다음 = `InitMainThreadWnd +0x50 CreateWindowExW` — 메시지 전용 창 (`HWND_MESSAGE`), `gdwMainThreadId == 0` 일 때만. 실패는 `CoInitializeEx` 를 실패 HRESULT 로 끝낸다 (combase 안에서 프로세스를 죽이진 않음).
- `[정적]` combase STA 초기화 길에는 `RegisterWindowMessageW` 가 없다.

### flow-writer 대조 (결과서와 다른 곳 — 뜻은 같음)

- 결과서 1-1 의 "`gpsi` 를 읽는 칸 (`+0x21e MOV RAX,[gpsi]`)" 은 **`RegisterDefaultClass +0x21e`** 줄이다 (`RegisterClassExWOWW.asm` 에는 `gpsi` 줄이 없다). 이름이 정수 아톰일 때 (`+0x6a RtlGetIntegerAtom` → `+0x76 MOVZX ESI,AL` → `+0xc3 TEST ESI` · `+0xc5 JNZ +0x218`) `gpsi + 0x364` 의 아톰 표와 견준다. combase 는 문자열 이름이라 안 탄다.
- 결과서 2-2 의 "`CMP RBX,1`" 은 `.asm` 에서 `+0xde MOV EAX,0x1` · `+0xe3 CMP RBX,RAX` 두 줄이다.

## 요청 쪽 로그와 코드가 다른 곳 (요청 쪽 에뮬레이터 로그 — 우리 `[관찰]` 아님)

- 요청 쪽은 "`NtUserUnregisterClass` 를 부르는 쪽은 combase `[추측]`" 이라 했다 — **맞다**: 둘째 인자 `0x7ffffcf20000` = `g_hinst`, 부르는 줄 `RegisterOleWndClass +0x1397be` (첫 등록 0 뒤) 또는 `UnRegisterOleWndClass +0x12` (창 만들기 실패 뒤 — 결과서 3-4 의 3, 새 로그 해석).
- 요청 쪽은 앞의 `NtUserRegisterWindowMessage` 를 같은 길로 봤다 — **아니다**: combase STA 길도, user32 등록 길도 이 syscall 을 안 부른다. 누가 불렀는지는 `[공백]`.

## PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정)

| 줄 (결과서가 적은 자리) | 판정 | 까닭 |
|---|---|---|
| `nt_user_register_class_ex_wow.cpp` — 늘 `0xC000` | **맞다** | user32 · combase 다 AX 가 0 이냐만 본다. 16비트 안. 같은 값 되풀이도 무해 |
| 인자 안 읽음 · 메모리 안 씀 | **맞다** | 출력 인자 없음. 인자 4 (`CLSMENUNAME`) 는 user32 가 채운 입력 |
| 헤더 주석 "WNDCLASSEXW 위치 근거 없음" | **이제 근거 있음** | 인자 1 = WNDCLASSEXW 복사 0x50 B, 인자 2 = 이름 `UNICODE_STRING*`. 이름이 필요해지면 인자 2 `Buffer` |
| 헤더 주석 "부르는 쪽 `RegisterClassExW`" | **일부 틀림** | combase 는 `RegisterClassW` (같은 도우미 · 같은 syscall) |
| `layer.cpp:743` 인자 수 0 | **표기만 다름** | 진짜는 7개 (5 = WORD, 6 = DWORD). stub 이 안 읽으니 동작엔 무해 |
| `NtUserUnregisterClass` 기본 대답 0 | **안전하다** | combase 가 결과를 안 봄. 1 을 돌려줄 때도 24 B 출력은 안 써도 됨 (0 이 안전, 포인터를 쓰면 user32 가 해제하려 듦) |
| `NtUserRegisterWindowMessage` 기본 대답 0 | **지금 막힘과 무관** | STA 길이 안 부름 |
| 실패를 흉내 낼 때 | **주의** | 등록을 0 으로 돌리려면 TEB `LastErrorValue` (`+0x68`) 를 써 줘야 한다 — 안 쓰면 `0x8007000e` |
| `NtUserGetAtomName` (`0xc000` 이면 1) | **동작은 하나, 뜻이 다름** | 인자 2, 반환 = 글자 수. 0 아니면 창 만들기로 감 (이름 안 채워도 NOT_FOUND → 아톰 그대로). 더 맞게: `[USTR+8]` 버퍼에 UTF-16 이름 + 글자 수 (끝 NUL 제외 `[추측]`) |

- 한 줄로: **`NtUserRegisterClassExWOW` stub 은 0 아닌 ATOM 하나, `NtUserUnregisterClass` 는 1 (또는 지금처럼 0) — 출력 칸 · 공유 메모리는 안 써 줘도 된다.** 단 돌려준 ATOM 은 combase 가 `gOleWindowClass` 에 담아 `CreateWindowExW` 클래스로 그대로 넘기므로 (`InitMainThreadWnd +0x21`) 그다음 `NtUserCreateWindowEx` stub 이 같은 값을 받는다.

## 아직 확인 필요

동적 (결과서 "동적으로 확인할 것" 1 ~ 6 — 대기열 Q242. test.exe 가 아니라 **STA COM 을 여는 프로그램** (`CoInitializeEx(…, COINIT_APARTMENTTHREADED)` 를 부르는 작은 exe 등) — 고르기는 사용자 결정. 유저 멈춤점, 프로세스 조건, ret 자리):

1. 진짜 ATOM 값과 `CLSMENUNAME` 이 정말 입력인지 — `user32!RegisterClassExWOWW+0x23d` 에서 `ax`, 부르기 전 · 후 `dq @rbp-0x80 L3` 비교. 조건: `poi(@rsp+0x70)` 이름이 `OleMainThreadWndClass`.
2. `NtUserUnregisterClass` 출력 24 B 가 진짜 커널에서 0 으로 남는지 (메뉴 이름 없는 클래스) — `user32!UnregisterClassW+0xd7` 에서 `eax` · `dq @rsp+0x58 L3`.
3. `NtUserGetAtomName` 의 진짜 반환 · 버퍼 — `user32!ClassNameToVersion+0x22d` 에서 `ax` 와 `du poi(@rbp-0x59)` (21글자인지, 끝 NUL 제외인지).
4. STA 길 — `combase!ThreadFirstInitialize+0x309` `eax` (등록 HRESULT), `combase!InitMainThreadWnd+0x57` `rax` (숨은 창 핸들), 실패 때만 `combase!RegisterOleWndClass+0x1397f5` `eax`. 전역 `combase+0x338870` · `combase+0x338864`. (뒷 범위 `+0x1397..` 가 WinDbg 에서 같은 이름으로 보일지는 `[추측]` — 결과서.)
5. `combase` 의 "API 세트 있나" (`0x179074`) `+0x2a` 뒤 `eax` · `[rsp+0x30]` — 진짜 Windows 에서 참인지.
6. 요청 쪽 seq 9941 (`NtUserRegisterWindowMessage`) 의 호출 스택 — 호출자 `[공백]`.

정적 (결과서 "다음 요청 후보" — 대기열 Q243):

1. **`NtUserCreateWindowEx` 본격** (원 요청 물음 5) — 인자 17개의 뜻 · 출력, 돌아온 핸들로 user32 가 syscall 없이 읽는 칸 (`gSharedInfo` · `ValidateHwnd` · `MDICompleteChildCreation` 갈래), `InitMainThreadWnd` 가 `CoInitializeEx` 로 돌아갈 때까지 더 부르는 user32 함수. 뽑을 것: `VerNtUserCreateWindowEx` · `CreateWindowInternal` 통째 + `ValidateHwnd`. (run86 정적 ④ 의 남은 몫과 겹침.)
2. `UninitMainThreadWnd` (`wCoUninitialize` · `CleanupRemoting`) 의 `IsWindow` (`+0x25`) · `DestroyWindow` (`+0x46`) 길 — `CoUninitialize` 때 `NtUserDestroyWindow` (`0x1095`) stub 답.
3. seq 9941 `RegisterWindowMessageW` 호출자 — 정적으로는 모름, 동적 6 으로.
- (flow-writer 보탬) 4. combase 지연 가져오기 공용 꼬리 (`__tailMerge_…window_ext_l1_1_0_dll` → `__delayLoadHelper2` 류) 와 요청 이름 `l1-1-0` ↔ 스키마 `l1-1-1` 의 버전 허용 (ntdll API 세트 풀이 쪽) — 지금은 `[추측]`. 낮음.

## 디버거 메모 / 도구 메모

- 새 함정 없음 (동적 안 함).
- 결과서: Ghidra `.c` 가 틀어 보인 곳 넷 (`RegisterOleWndClass.c` 의 16바이트 상수 · `RegisterClassExWOWW.c` 지역 변수 이름 · `ClassNameToVersion.c` 의 인자 없는 `NtUserGetAtomName()` · `RegisterOleWndClass` 본체가 두 범위로 나뉨) 은 `.asm` 으로 읽었다. 기록만, 하네스 제안 아님.

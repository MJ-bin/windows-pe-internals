# 128 user32-regclass — 정적 분석 결과

- 날짜: 2026-11-22
- 요청서: `_workspace/sast/128_user32-regclass.md` (새 Q240, PE_emulator 요청 `298_internals-user32-regclass.md` 의 물음 1~3)
- 판정: **분석성공.** 요청서 물음 1·2·3 모두 줄 근거와 함께 답했다. 덧붙임으로 PE_emulator 원 요청의 물음 4 (`NtUserGetAtomName`) 는 이미 뽑은 줄만으로 닫았고, 물음 5 (`NtUserCreateWindowEx`) 는 사슬·진입 줄만 적었다 (본격은 다음 요청).
- 스크립트 문제: **없음.** (Ghidra 디컴파일 `.c` 가 틀어 보인 곳 둘은 아래 "읽을 때 주의" 에 적음 — 그 자리는 `.asm` 으로 읽었다.)
- 서버 메모리: combase analyze 전 `free -g` = total 46 / used 14 / **available 30 GB**. 이때 다른 decompiler (sast/129 몫) 의 Ghidra JVM 이 8G 로 하나 돌고 있어서 건드리지 않고, 나는 `MAXMEM=10G` 로 **한 번에 하나** 만 돌렸다 (analyze 는 22:33~22:35, 약 2분 반). 끝난 뒤 export·refs 는 3~6G 로 한 번에 하나씩.

## 모듈 / 바이너리 / PDB

| 모듈 | 바이너리 (`ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/`) | PDB (`ms/sym/`) | GUID+Age (바이너리 CodeView = `.info.txt` = PDB 파일) |
|---|---|---|---|
| combase | `combase.dll` | `combase.dll.pdb` | **`DA1DC0C4F35CEE58F672CBB20460D11D1`** (바이너리에서 다시 읽음 `fetch_pdb.py` · `llvm-pdbutil dump --summary` 로 PDB 안 GUID `{DA1DC0C4-F35C-EE58-F672-CBB20460D11D}` 확인, PDB 쪽 Age 4 ≥ 이미지 Age 1) |
| user32 | `user32.dll` | `user32.dll.pdb` | `CDB947D1B96B87977915DE04CF54279C1` (이미 analyze 됨 — 다시 안 함) |
| win32u | `win32u.dll` | (이미 analyze 됨) | 스텁만 export (depth 0) |

- combase analyze 증거 셋: `ms/ghidra/combase.gpr` · `ms/decomp/combase/_index.tsv` (16,607 함수, `FUN_` 이름 632) · `ms/ghidra/combase.analyze.log` 의 `SetPdb: combase.dll <- …combase.dll.pdb` 줄.
- 별칭 표: `ms/sym/combase.dll.aliases.tsv` (별칭 무리 1,118곳).

## 뽑은 것 (이번에 새로 남긴 `.asm`·`.c`)

- user32 (`ms/disassemble/user32/`, `ms/decomp/user32/`): `RegisterClassW` `RegisterClassExW` `RegisterClassA` `RegisterClassExA` `RegisterClassExWOWW` `RegisterClassExWOWA` `UnregisterClassW` `UnregisterClassA` `VersionRegisterClass` (+ depth 1 로 딸려 온 `InitClsMenuNameW` `ClassNameToVersion` `RegisterDefaultClass` `RtlGetExpWinVer` …) 17개. 이어서 depth 0 으로 `RegisterClipboardFormatW` (= `RegisterWindowMessageW`, 아래), `CreateWindowExW` `CreateWindowInternal` `VerNtUserCreateWindowEx` (사슬 이름 확인용).
- win32u (`ms/disassemble/win32u/`): `NtUserRegisterClassExWOW` `NtUserUnregisterClass` `NtUserRegisterWindowMessage` `NtUserCreateWindowEx` 스텁 (depth 0).
- combase (`ms/disassemble/combase/`, `ms/decomp/combase/`): `RegisterOleWndClass` `UnRegisterOleWndClass` `InitMainThreadWnd` `UninitMainThreadWnd` `GetOrCreateSTAWindow` `OleMainThreadWndProc` `ThreadWndProc` `ThreadFirstInitialize` `CoInitializeEx` `_CoInitializeEx` `IsCreateWindowExWPresent` (전부 depth 0).
- 누가 읽나 (`refs`): combase 가져오기 칸 `RegisterClassW` (rva `0x3635c8`) · `UnregisterClassW` (`0x3635d0`) · `CreateWindowExW` (`0x363630`) · `RegisterClipboardFormatW` (`0x363540`) → `ms/disassemble/combase/_refs/`. user32 `0x4ce60` (`RegisterWindowMessageW`) · `0xb5eb8` (`NtUserRegisterWindowMessage` 가져오기 칸) → `ms/disassemble/user32/_refs/`.
- 일회용 읽기 (스크래치패드, 커밋 안 함): `apisetschema.dll` 의 `.apiset` 구역 읽기, combase `.rdata` UTF-16 문자열 읽기.

## 버전 확인

- 처음 분석한 모듈 combase 는 "알려진 사실" 표에 없다. 대신 **에뮬레이터 로그의 스택 주소 여섯 개를 우리 `.asm` 의 스택 칸 셈과 맞춰 봤다 — 전부 일치.**
  - 등록: 로그 `NtUserRegisterClassExWOW(0x7fbff150, 0x7fbff0f0, 0x7fbff0d0, 0x7fbff110, …)`. 이름 USTR = `RSP+0x40` (`RegisterClassExWOWW +0x1ee`) 을 `0x7fbff0d0` 로 놓으면 `RSP = 0x7fbff090`, `RBP = RSP+0x100` (`+0xb` `LEA RBP,[RSP-0x220]` 뒤 `+0x13` `SUB RSP,0x320`) → WNDCLASSEXW 복사 `RBP-0x40` = `0x7fbff150` ✓ · CLSMENUNAME `RBP-0x80` = `0x7fbff110` ✓ · 클래스 이름 USTR `RSP+0x60` = `0x7fbff0f0` ✓.
  - 지움: 로그 `NtUserUnregisterClass(0x7fbff200, 0x7ffffcf20000, 0x7fbff228)`. `UnregisterClassW` 가 첫째 인자를 `RSP+0x30` (`+0x9c`), 셋째를 `RSP+0x58` (`+0xc8`) 로 만든다 → 차이 `0x28` ✓ (`0x7fbff228 − 0x7fbff200`).
  - 에뮬레이터는 진짜 user32 코드를 Unicorn 에서 돌린 것이므로, 이 일치는 "스크립트의 오프셋 셈과 바이너리 둘 다 믿어도 된다" 는 근거다 (진짜 Windows 관찰은 아님).
- `.asm` 오프셋 재확인: 결과서의 `+0x` 는 `.asm` 줄에서 다시 찾았고, 호출은 `_calls.tsv` 의 `call_off/ret_off` 와 맞췄다. 점프 목적지는 "목적지 − 함수 시작" 으로 셌다 (예: `RegisterOleWndClass +0x7a JZ 0x180267ce0` → `0x180267ce0 − 0x18012e52c = +0x1397b4`).

---

## 먼저 읽는 한 줄 답

| # | 한 줄 답 |
|---|---|
| 1 | **예 — stub 은 0 아닌 ATOM (16비트) 하나만 돌려주면 된다.** user32 는 돌아온 값에서 **AX 만** 쓰고 (`RegisterClassExWOWW +0x242 MOVZX EDI,AX`, `+0x265 TEST RDI,RDI`), 출력 인자·`gSharedInfo`·`gpsi` 는 이 길에서 안 읽는다. 0 이면 user32 가 오류값을 따로 안 쓴다 (커널이 쓴 TEB 값 그대로). 단, 돌려준 ATOM 은 combase 가 `gOleWindowClass` 에 담아 `CreateWindowExW` 클래스 자리에 그대로 넘기므로 (`InitMainThreadWnd +0x21`) 같은 값이어야 한다. |
| 2 | **`NtUserUnregisterClass` stub 은 TRUE(1) 만 돌려주고 셋째 인자 (24바이트 출력) 는 건드리지 않으면 안전하다.** user32 가 그 칸을 미리 0 으로 채워 두고, 돌아온 값이 **정확히 1** 일 때만 칸의 앞 두 칸을 힙 포인터로 보고 `RtlFreeHeap` 한다 (`UnregisterClassW +0xe3 CMP RBX,1`, `+0xe8~+0x121`). 0 으로 두면 해제는 건너뛴다. 거짓(0)을 돌려줘도 user32 는 오류값을 안 쓰고 그 값 그대로 돌려준다. **칸에 0 이 아닌 값을 쓰면 오히려 위험** (힙 해제 호출). combase 는 지움 결과를 아예 안 본다. |
| 3a | **차례 맞다.** `RegisterOleWndClass`: `RegisterClassW` (+0x61/+0x68) 가 0 이면 (`+0x77 TEST RAX,RAX`, `+0x7a JZ +0x1397b4`) → **`UnregisterClassW("OleMainThreadWndClass", g_hinst)`** (+0x1397be/+0x1397c5, 결과 검사 없음) → `RegisterClassW` 한 번 더 (+0x1397cf/+0x1397d6). 첫 등록이 0 이기만 하면 이유와 상관없이 지우고 다시 한다 (오류값을 안 봄). |
| 3b | **둘째도 0 이면 `GetLastError()` 로 정한다: 0 이면 `0x8007000e` (E_OUTOFMEMORY)**, 0 보다 크면 `0x80070000 \| (오류값 & 0xffff)`, 이미 음수면 그대로 (+0x1397ee ~ +0x139810). 우리 로그 (오류값 안 씀 → 0 → `0x8007000e`) 와 맞는다. CLR 이 그 값을 `OutOfMemoryException` 으로 읽는 줄은 이 레포에 clr 이 없어 `[공백]`. |
| 3c | **성공하면 바로 다음 = `InitMainThreadWnd` 의 `CreateWindowExW(0, gOleWindowClass(ATOM), L"OleMainThreadWndName", 0x88000000, 0x80000000 ×4, (HWND)-3 = HWND_MESSAGE, 0, g_hinst, 0)`** — **메시지 전용 창** (스타일 = `WS_POPUP \| WS_DISABLED`, `WS_VISIBLE` 없음). 단 `gdwMainThreadId == 0` 일 때만 (프로세스의 첫 STA). **실패는 `CoInitializeEx` 를 실패시킨다 (치명 = CoInitializeEx 가 실패 HRESULT 를 돌려줌, combase 안에서 프로세스를 죽이진 않음).** |
| 3d | **combase 의 STA 초기화 길에는 `RegisterWindowMessageW` 가 없다.** combase 가 부르는 것은 `RegisterClipboardFormatW` (같은 user32 함수) 이고, 부르는 곳은 `CLIPFORMAT_UserUnmarshal` (+0xad) · `CLIPFORMAT_UserUnmarshal64` (+0x1f7) 둘뿐 — STA 초기화와 무관. 로그 seq 9941 의 호출자는 이 정적 자료로는 모름 `[공백]`. |
| 3e | 클래스 이름 `OleMainThreadWndClass` (`.rdata` `0x1802cd830`), 숨은 창 이름 `OleMainThreadWndName` (`0x1802cd928`). |
| 우리 | **맞다 / 일부 틀림**: `NtUserRegisterClassExWOW` = `0xC000` 하나 (출력 안 씀, 인자 안 읽음) **맞다.** 주석의 "부르는 쪽 RegisterClassExW" 는 **틀림 — combase 는 `RegisterClassW` 를 부른다** (같은 syscall 로 감). `NtUserUnregisterClass` 기본 대답 0 도 **안전하다** (아래 표). |

---

## 물음 1 — user32 등록 길

### 1-1. 사슬 (W 판; A 판은 이름만)

```
combase!RegisterOleWndClass +0x61 → [가져오기 칸 0x1803635c8] user32!RegisterClassW
user32!RegisterClassW      +0x6a/+0x6f → RegisterClassExWOWW(&WNDCLASSEXW 복사, 0, 0, 0x80, 1)
user32!RegisterClassExW    +0x1a/+0x1f → RegisterClassExWOWW(원래 구조체, 0, 0, 0x80, 1)   (cbSize ≠ 0x50 이면 +0x2a 에서 SetLastError(0x57), 0)
user32!RegisterClassExWOWW +0x236/+0x23d → [가져오기 칸 0x1800b5e98] win32u!NtUserRegisterClassExWOW   (syscall 0x10a2)
```

- 도우미 이름 = **`RegisterClassExWOWW`** (rva `0x1c99c`). PDB 이름 `?RegisterClassExWOWW@@YAGPEAUtagWNDCLASSEXW@@PEAUtagWOWCLS@@GKH@Z` — (WNDCLASSEXW*, WOWCLS*, WORD, DWORD, int). `RegisterClassW`/`RegisterClassExW` 가 넘기는 값은 **WOWCLS\* = 새로 채우지 않음 (두 함수 다 RDX 를 안 놓고, 도우미 안에서도 이 인자를 안 씀), WORD = 0, DWORD = `0x80`, int = 1**. (`RegisterClassW +0x22 XOR R8D,R8D` · `+0x39 MOV R9D,0x80` · `+0x54 MOV [RSP+0x20],1`; `RegisterClassExW +0x9 XOR R8D,R8D` · `+0x14 MOV R9D,0x80` · `+0xc MOV [RSP+0x20],1`).
- 마지막 인자 int = 1 은 "기본 클래스 이름이면 먼저 등록 시도" 켜는 값: `RegisterClassExWOWW +0x9b CMP [RBP+0x280],R12D` → `+0xa4~+0xb8` `gdwRegisteredClassesMask`·`gdwRegisteredClasses` 견주고 `RegisterDefaultClass` (ret `+0xbd`). **`OleMainThreadWndClass` 는 기본 클래스 17개 이름이 아니라서 `RegisterDefaultClass` 는 잠금 (`gcsClassRegister`) 안에서 이름 17개 견주고 1 을 돌려줄 뿐, 커널을 안 부른다** (`RegisterDefaultClass.c` 의 루프; `RtlGetIntegerAtom` 으로 "#숫자" 이름인지 먼저 봄). `gpsi` 를 읽는 칸 (`+0x21e MOV RAX,[gpsi]`) 은 **클래스 이름이 정수 아톰일 때만** 탄다.
- 이 인자 모양이 맞다는 근거: 위 "버전 확인" 의 스택 주소 여섯 개.

### 1-2. 표 — `NtUserRegisterClassExWOW` (win32u `0x10a2`, 인자 **7개**)

호출 자리: `RegisterClassExWOWW +0x236` (call) / `+0x23d` (ret). A 판 `RegisterClassExWOWA +0x21a / +0x221` 도 같은 syscall. 인자를 놓는 줄은 `+0x215 ~ +0x236`.

| # | 놓는 자리 (`.asm` 줄) | 뜻 | 입력/출력 | 돌아온 뒤 읽는 칸 |
|---|---|---|---|---|
| 1 | RCX = `LEA RCX,[RBP-0x40]` (+0x228) | **WNDCLASSEXW 복사본** (0x50 B, user32 스택). 호출한 쪽 구조체를 `+0x150~+0x18f` 에서 `MOVAPS` 로 복사 (`cbSize·style·lpfnWndProc·cbClsExtra·cbWndExtra·hInstance·hIcon·hCursor·hbrBackground·lpszMenuName·lpszClassName·hIconSm`) | 입력 | 안 읽음 |
| 2 | RDX = `[RSP+0x70]` (+0x21a) → `UNICODE_STRING` at `RSP+0x60` | **준 클래스 이름** (`lpszClassName` 이 문자열이면 `RtlInitUnicodeString` (+0x1e2), 아톰 (상위 48비트 0) 이면 `Length 0·MaxLen 0·Buffer = 아톰값` (+0x2fd~+0x302)) | 입력 | 안 읽음 |
| 3 | R8 = `[RSP+0x50]` (+0x215) → `UNICODE_STRING` at `RSP+0x40` | **버전 이름** — `ClassNameToVersion` 결과 (활성화 컨텍스트에 클래스 바꿔치기가 없으면 준 이름 그대로 = 2번과 같은 글자) | 입력 | 안 읽음 |
| 4 | R9 = `LEA R9,[RBP-0x80]` (+0x21f) | **`CLSMENUNAME` 24 B** = {ANSI 메뉴 이름 포인터, 유니코드 메뉴 이름 포인터, `UNICODE_STRING*`} (`InitClsMenuNameW` 가 채움, +0x143/+0x148) | **입력** (user32 가 채워 줌). 커널이 쓴다는 줄 없음 | **실패 때만** 앞 두 칸을 읽어 힙 포인터면 해제 (+0x2ab~+0x2d6, +0x3a4~+0x3b9) |
| 5 | `[RSP+0x20]` word = SI (+0x231) | **WORD** — `RegisterClassExWOWW` 의 셋째 인자. `RegisterClassW/ExW` 는 0 | 입력 | — |
| 6 | `[RSP+0x28]` dword = R14D (+0x22c) | **플래그** — 넷째 인자 `0x80` 에, 모듈 `ExpWinVer > 0x30a` 이면 `0x40` 을 더함 → **보통 `0xC0`** (+0x153 `MOV R14D,R15D`, +0x15a `OR R14D,0x40`, +0x15e `CMP EDI,0x30a`, +0x170 `CMOVBE`). 이름은 `[공백]` | 입력 | — |
| 7 | `[RSP+0x30]` qword = R12 = **0** (+0x223) | 0 (WOW 쪽 칸 `[추측]` — 이 빌드에선 늘 0) | 입력 | — |

- "커널이 쓰는 출력" = **없음** (정적으로 본 한). 반환값 하나가 전부.
- 이름: 인자 4 는 `CLSMENUNAME` (`tagCLSMENUNAME`) — 이름은 PDB 이름 `InitClsMenuNameW@@YAHPEAUtagCLSMENUNAME@@…`. LARGE_STRING 은 이 길에 안 나온다 (`NtUserCreateWindowEx` 쪽에서 나옴).

### 1-3. 돌아온 값으로 하는 일

| 갈래 | 줄 | 하는 일 |
|---|---|---|
| 돌아온 값 → `MOVZX EDI,AX` | `RegisterClassExWOWW +0x242` | **AX (16비트) 만** 쓴다. 상위 비트 무시. |
| 호출한 모듈의 `hInstance` → `RtlGetExpWinVer` | `+0x245 ~ +0x25a` | 이미지 머리 `RtlImageNtHeader` 에서 `MajorSubsystemVersion·MinorSubsystemVersion` 읽음 (`RtlGetExpWinVer +0x23 ~ +0x41`). **`< 0x30a` 인 옛 모듈이면 `EDI = (EDI != 0)`** (+0x393~+0x39f) → 1 을 돌려줌. 새 모듈 (combase) 은 AX 그대로. |
| AX ≠ 0 | `+0x265 TEST RDI,RDI` → `+0x26a MOVZX EAX,DI` → `RET` (+0x28d) | **성공, ATOM 돌려줌.** 더 읽는 칸 없음. |
| AX = 0 | `+0x268 JZ +0x2ab` | `CLSMENUNAME[0]`·`[1]` 이 힙 포인터 (상위 48비트 ≠ 0) 이면 `RtlFreeHeap(pUserHeap, 0, …)` (+0x2ca/+0x2d1, +0x3ad/+0x3b4) → 0 돌려줌. **user32 가 `SetLastError` 를 안 씀** — 커널이 쓴 `TEB.LastErrorValue` 그대로. |
| user32 가 직접 쓰는 오류 | `+0x326 MOV ECX,0x57` / `+0x32b CALL RtlSetLastWin32Error` (ret `+0x332`) | **`0x57` (ERROR_INVALID_PARAMETER)** — 커널을 부르기 **전에** 거르는 경우만: ① `RegisterDefaultClass` 가 0 (+0xbf), ② `cbClsExtra`·`cbWndExtra` 음수 (+0xce~+0xdc `CMP`·`JL`), ③ `hInstance == hmodUser` 이고 `TEB.Win32ClientInfo+0x10 ≥ 0x400` (+0xe2~+0x320), ④ 클래스 스타일에 안 쓰는 비트 (`& 0xf7fc0114`) 이고 `ExpWinVer > 0x30a` (+0x11e~+0x363), ⑤ `hbrBackground > 0x1f` 인데 `GdiValidateHandle` 실패 이고 `ExpWinVer > 0x300` (+0x2d8~+0x2ec). |
| 말없이 0 | `+0x14a JZ +0x337` (메뉴 이름 복사 실패) · `+0x1bd JZ +0x378` (`ClassNameToVersion` 이 NULL) | 오류값 없이 0 (메뉴 문자열은 해제). |

- **성공 뒤 user32 가 더 읽는 것** (물음 핵심): `RegisterClassExWOWW` 본문에서 **`gSharedInfo`·`SHAREDINFO`·`gpsi`·클래스 캐시를 읽는 줄은 없다.** 전역은 `hmodUser` · `pUserHeap` · `gdwRegisteredClasses(Mask)` · `__security_cookie` 뿐 (`.asm` 의 전역 주석). TEB 칸 읽기는 `hInstance == hmodUser` 일 때의 `TEB+0x810` 하나 (combase 는 해당 없음). 그 밖에 읽는 것은 **모듈 이미지 머리** (`RtlImageNtHeader`) 뿐 — 에뮬레이터 가상 메모리에 올린 진짜 이미지라 stub 과 무관.
- 참고 (user32 초기화가 켜는 칸, `[정적]`): `_UserClientDllInitialize +0x57b` 에서 `gdwRegisteredClassesMask |= 0x1ffff` (기본 클래스 17개), `ClientThreadSetup +0x422/+0x429` 에서 `[RBX+0xd0] = &gdwRegisteredClasses` (RBX 는 `+0xc3 ADD RBX,0x800` 으로 만든 `TEB+0x800` = `Win32ClientInfo` 로 읽힘 — 커널이 user32 의 이 비트 칸을 볼 수 있게 주소를 건네는 자리 `[추측]`). 이번 길과는 직접 관계 없음.

### 1-4. 이름 참고

- `RegisterWindowMessageW` 는 user32 에서 `RegisterClipboardFormatW` 와 **같은 함수** (rva `0x4ce60`, 서수 2245·2270) — Ghidra 이름은 `RegisterClipboardFormatW`. 본문: `RtlInitUnicodeString(&USTR, 이름)` (+0x24) → `NtUserRegisterWindowMessage(&USTR)` (+0x35/+0x3c, syscall `0x1032`), 반환 UINT 를 그대로 돌려줌 (오류값 안 씀). user32 안에서 `NtUserRegisterWindowMessage` 를 부르는 곳은 `RegisterClipboardFormatW +0x35` 와 `RegisterClipboardFormatA +0xf3` 둘뿐 (`refs` 0xb5eb8).

---

## 물음 2 — user32 지움 길

사슬: `combase!UnRegisterOleWndClass +0x12/+0x19` (또는 `RegisterOleWndClass +0x1397be`) → `user32!UnregisterClassW` (rva `0x1cf30`, `(lpClassName, hInstance)`) → `win32u!NtUserUnregisterClass` (syscall `0x10ad`). A 판 `UnregisterClassA` 는 이름만.

### 2-1. 표 — `NtUserUnregisterClass` (인자 **3개**)

호출 `UnregisterClassW +0xd0` (call) / `+0xd7` (ret).

| # | 놓는 자리 | 뜻 | 입력/출력 | 돌아온 뒤 읽는 칸 |
|---|---|---|---|---|
| 1 | RCX = `[RSP+0x40]` (+0xc3) → `UNICODE_STRING` at `RSP+0x30` | 지울 클래스 이름 (문자열이면 `RtlInitUnicodeString` +0xb7, 아톰이면 `Length 0·Buffer=아톰` +0x193~+0x197). `ClassNameToVersion` 결과 (원 이름 그대로가 보통) | 입력 | — |
| 2 | RDX = RBX = `UnregisterClassW` 둘째 인자 `hInstance` (+0x2f, +0xcd) | 클래스를 만든 모듈 핸들. combase 는 `g_hinst` (로그의 `0x7ffffcf20000`) | 입력 | — |
| 3 | R8 = `LEA R8,[RSP+0x58]` (+0xc8) | **출력 `CLSMENUNAME` 24 B** (`RSP+0x58 ~ +0x6f`). user32 가 부르기 전에 **0 으로 채움** (`+0x4e XORPS` · `+0x56 MOV [RSP+0x68],0` · `+0x7d MOVUPS [RSP+0x58],0`) | **출력** | 돌려받은 값이 1 일 때 앞 두 칸 |

### 2-2. 돌아온 뒤

| 갈래 | 줄 | 하는 일 |
|---|---|---|
| `MOV EBX,EAX` → `CMP RBX,1` | +0xdc, +0xe3 | 돌아온 값이 **정확히 1** 인지만 본다. |
| 1 일 때 | +0xe8~+0x102 | `[RSP+0x58]` (ANSI 메뉴 이름) 이 힙 포인터 (`& 0xffffffffffff0000 ≠ 0`) 이면 `RtlFreeHeap(pUserHeap,0,그 값)` (call +0xfb / ret +0x102) |
|  | +0x107~+0x121 | `[RSP+0x60]` (유니코드 메뉴 이름) 도 같게 (call +0x11a / ret +0x121). `[RSP+0x68]` 은 안 읽음. |
| **0 으로 두면** (stub 이 안 써 줌) | +0xed `TEST RSI,R8` → +0xf0 `JZ +0x107` · +0x10c `TEST RSI,R8` → +0x10f `JZ +0x126` | 해제 호출 **둘 다 건너뜀.** 안전. |
| 1 이 아닐 때 (거짓 포함) | +0xe6 `JNZ +0x126` | 해제 건너뜀. **`SetLastError` 안 씀.** |
| 그 뒤 | +0x126~+0x137 | `ClassNameToVersion` 이 활성화 컨텍스트를 돌려줬으면 (`[RSP+0x50] ≠ 0`) `RtlReleaseActivationContext` (call +0x130 / ret +0x137) |
| 반환 | +0x13c `MOV EAX,EBX` → `RET` (+0x15f) | **커널이 준 값을 그대로** (BOOL 로 정리 안 함). |
| 이름 풀기 실패 | +0x8c `TEST RAX,RAX` → +0x8f `JZ +0x161` | 커널 안 부르고 0 반환 (`SetLastError` 없음). |
| 시스템 콜 필터 | +0x28 `CMP g_systemCallFilterId,5` → +0x32 `JZ +0x166` | 필터 번호가 5 인 프로세스는 커널을 안 부르고 `TEB+0x89a` (16비트 세는 칸) 를 올린 뒤 1 반환. 보통 해당 없음 (뜻 `[공백]`). |

- **stub 에게:** 돌려줄 값은 `1` (TRUE) 이 가장 자연스럽다. 출력 24 B 는 **쓰지 않는다** (0 그대로). 메뉴 이름이 있는 클래스를 흉내 내서 포인터를 쓰면 user32 가 그 포인터를 `pUserHeap` 에서 해제하려 든다 — 쓰지 않는 게 안전.
- **combase 쪽 (지움 결과를 보나):** `UnRegisterOleWndClass +0x1e TEST EAX,EAX` 가 0 이면 `GetLastError` 만 읽고 (추적용) 어느 갈래든 `+0x22 AND [gOleWindowClass],0` 으로 간다 — **결과가 0 이든 1 이든 같은 길.** `RegisterOleWndClass` 안의 첫 지움 (+0x1397be) 은 결과 검사 자체가 없다. 그래서 지금 `NtUserUnregisterClass` 기본 대답 0 은 combase 쪽에서 아무 문제 없다.

---

## 물음 3 — combase 쪽

### 3-1. 함수 이름 (PDB 이름으로 확인, 지어낸 것 없음)

| PDB 이름 | rva | 맡은 일 |
|---|---|---|
| `RegisterOleWndClass` | `0x12e52c` | 창 클래스 등록 (물음 3 의 그 함수). 부르는 곳 `ThreadFirstInitialize` 하나 |
| `UnRegisterOleWndClass` | `0x13796c` | 지움. 부르는 곳 `wCoUninitialize` · `ThreadFirstInitialize` |
| `InitMainThreadWnd` | `0x12f250` | 숨은 창 (`ghwndOleMainThread`) 만들기. 부르는 곳 `ThreadFirstInitialize` |
| `UninitMainThreadWnd` | `0x108824` | 그 창 `DestroyWindow` (`wCoUninitialize`·`CleanupRemoting` 이 부름) |
| `GetOrCreateSTAWindow` | `0x1229b4` | STA 스레드별 창 (같은 클래스, 이름 `OLEChannelWnd`). 부르는 곳 `StartServer` — **`CoInitializeEx` 길 아님** |
| `ThreadFirstInitialize` | `0x31518` | STA/MTA 첫 초기화 (`_CoInitializeEx +0x34f/+0x354` 가 부름) |
| `OleMainThreadWndProc` | `0x11f200` | 그 클래스의 창 함수 |
| `IsCreateWindowExWPresent` | `0x179074` | API 세트 있나 검사 (같은 주소 별칭 `IsIsGUIThreadPresent`·`IsInSendMessageExPresent`) |

- `OleInitialize` 는 combase `_index.tsv` 에 없다 (ole32 쪽 — 이 요청 밖). `NdrpRegisterClass`·`NdrpUnregisterClass` 는 이름만 비슷하고 창 클래스와 무관 (combase 에서 `RegisterClassW` 가져오기 칸을 읽는 곳은 `RegisterOleWndClass` 하나, `refs` 0x3635c8).
- combase 는 창 함수를 **지연 로딩** 으로 가져온다: `ext-ms-win-rtcore-ntuser-window-ext-l1-1-0.dll` (`RegisterClassW`·`UnregisterClassW`·`CreateWindowExW`·`DestroyWindow`·`SendMessageW` 등, `llvm-readobj --coff-imports` 의 `DelayImport` 칸). 이 API 세트 이름은 `apisetschema.dll` 안에서 **`ext-ms-win-rtcore-ntuser-window-ext-l1-1-1 → user32.dll`** (같은 계열 `ext-ms-win-ntuser-misc-l1-1-0`·`ext-ms-win-rtcore-ntuser-synch-ext-l1-1-0` 도 `user32.dll`). 첫 호출 때 `DelayLoad_RegisterClassW` (rva `0x1792a9`, 12바이트) 가 `LEA RAX,[0x1803635c8]` (+0x0) 한 뒤 `JMP __tailMerge_ext_ms_win_rtcore_ntuser_window_ext_l1_1_0_dll` (+0x7, rva `0x1790c2`, 이 DLL 이름 하나에 모든 발판이 모이는 공용 꼬리) 로 간다 — 그 안의 로딩 코드는 이번에 안 읽음 `[공백]`. 로그가 `RegisterClassExWOW` 에 닿은 것이 이 길이 풀렸다는 증거 (요청 `l1-1-0` ↔ 스키마 `l1-1-1` 의 버전 허용은 ntdll 쪽 코드를 안 봐서 `[추측]`).

### 3-2. 위 사슬 — `CoInitializeEx` 아래에서 어떻게 여기로 오나

`CoInitializeEx` (rva `0x309e0`) → `_CoInitializeEx` (`+0x44/+0x49`) → `ThreadFirstInitialize` (`+0x34f/+0x354`).

1. **STA 인지·user32 창 함수가 있는지 먼저 본다**: `CoInitializeEx +0x33 TEST DIL,0xa` → `+0x37 JNZ +0xa9` → `IsCreateWindowExWPresent` (`+0xa9/+0xae`) → 0 이면 `+0xb2 MOV EBX,0x80004001` (E_NOTIMPL) 로 실패 갈래. `IsCreateWindowExWPresent` 는 `ApiSetQueryApiSetPresence(L"ext-ms-win-rtcore-ntuser-window-ext-l1-1-0")` (`+0x25/+0x2a`) 를 한 번 부르고 결과를 `ApiSetExists_…` 칸 (1 = 있음, 2 = 없음) 에 캐시. (이 검사는 STA 초기화에서 `RegisterClassW` 에 닿기 **전** 에 통과해야 한다.)
2. `ThreadFirstInitialize`: `+0x4d TEST R14B,0x2` → `+0x51 JNZ +0x1ae` (STA 갈래) → `HeapAlloc(g_hHeap, 0, 0x1b0)` (call `+0x22b` / ret `+0x232`, 실패하면 `+0x23a JZ +0x2b9` → EBX = `0x8007000e`) → `CComApartment` (`+0x242/+0x247`) → `FinalConstruct` (`+0x255/+0x25a`) → `CreateAndFreezeDefaultObjectContext` (`+0x263/+0x268`).
3. **클래식 STA 면** (`+0x278 TEST R14D,0x60000000` 이 0): `g_cClassicSTAInits` (`0x180338008`) 를 1 올리고 (`+0x281~+0x289`), **올린 값이 1 이면 (프로세스의 첫 클래식 STA)** `RegisterOleWndClass` (call `+0x304` / ret `+0x309`, `+0x28f CMP EAX,EDI` → `+0x291 JZ +0x304`).
4. 등록이 성공 (`+0x30d JNS +0x293`) 하면 `gdwMainThreadId` (`0x180338864`) 가 0 일 때만 (`+0x293 CMP …,0` → `+0x29a JZ +0x2be`) `InitMainThreadWnd` (`+0x2be/+0x2c3`). 성공하면 `+0x29c` 에서 `g_cSTAInits` 올리고 끝맺음.
5. **실패 갈래:** 등록 실패 (`+0x30d JNS` 안 탐) → `+0x30f DEC g_cClassicSTAInits` → `+0x315 JMP +0x2d7` (정리). 창 만들기 실패 (`+0x2c7 JNS` 안 탐) → `+0x2c9 ADD g_cClassicSTAInits,-1` → 0 이 되면 `+0x2d2 CALL UnRegisterOleWndClass` (ret `+0x2d7`) → `+0x2d7` 정리 (Release 둘, `FUN_180256bd6` 조각에서 `DecrementProcessInitializeCount`) → **EBX (실패 HRESULT) 를 `ThreadFirstInitialize` 가 그대로 돌려줌** (`+0x34e MOV EAX,EBX` 로 가는 `FUN_180256bd6 +0x6b JMP`). 이어 `_CoInitializeEx +0x3c2 TEST ESI,ESI` → `+0x3c4 JS +0x92a` → … → `+0x4f9 MOV EAX,ESI` 로 `CoInitializeEx` 가 **그 HRESULT 를 돌려준다** (`IInitializeSpy` 가 끼면 `+0x4ba MOV ESI,EAX` 가 바꿀 수 있으나 보통 없음).

### 3-3. 표 — 등록 → 지움 → 등록 → 창 만들기 (`차례 · 부르는 것 · 실패 때 · 돌려주는 값 · 줄`)

| 차례 | 부르는 것 (인자) | 실패 때 | 돌려주는 값 | 줄 |
|---|---|---|---|---|
| ① | `RegisterClassW(&WNDCLASSW)` — `style 0` · `lpfnWndProc = OleMainThreadWndProc` (rva `0x11f200`) · `cbClsExtra 0` · `cbWndExtra 0x14` · `hInstance = g_hinst` · `hIcon/hCursor 0` · `hbrBackground = (HBRUSH)2` · `lpszMenuName 0` · `lpszClassName = L"OleMainThreadWndClass"` | AX = 0 → `gOleWindowClass = 0` → ②로 (이유 안 봄) | AX 를 `MOVZX EAX,AX` 로 넓혀 `gOleWindowClass` (`0x180338870`) 에 저장 (성공이면 0 아닌 ATOM) | `RegisterOleWndClass +0x61/+0x68` ~ `+0x7a` (구조체는 `+0xf~+0x5d` 에서 채움; `hbrBackground` 와 `lpszMenuName` 은 `+0xf MOVDQA XMM1,[0x1802cd860]` = 16바이트 상수 `…02` → `hbr = 2`, `menu = 0`) |
| ② | `UnregisterClassW(L"OleMainThreadWndClass", g_hinst)` | **결과 검사 없음** | — | `+0x1397b4 ~ +0x1397c5` (`JZ` 도착 자리 `+0x1397b4` = `0x180267ce0`) |
| ③ | `RegisterClassW(&같은 구조체)` | AX = 0 → `GetLastError` 갈래 | `gOleWindowClass` 에 저장, 0 아니면 `+0x80` 으로 돌아가 **`S_OK (0)`** | `+0x1397cf/+0x1397d6` ~ `+0x1397e8 JNZ +0x80` |
| ③ 실패 | `GetLastError()` → HRESULT | — | 0 → **`0x8007000e`** · 양수 → `0x80070000 \| (e & 0xffff)` · 음수 → 그대로 | `+0x1397ee/+0x1397f5` · `+0x1397fa MOV EBX,EAX` · `+0x1397fc/+0x1397fe TEST·JLE` · `+0x139800 MOVZX` · `+0x139803 OR 0x80070000` · `+0x139809 TEST EBX,EBX` · `+0x13980b MOV EAX,0x8007000e` · `+0x139810 CMOVNS` |
| ④ | `InitMainThreadWnd`: `CreateWindowExW(0, gOleWindowClass, L"OleMainThreadWndName", 0x88000000, 0x80000000, 0x80000000, 0x80000000, 0x80000000, (HWND)-3, 0, g_hinst, 0)` | HWND = 0 → ④ 실패 | HWND 를 `ghwndOleMainThread` (`0x180338878`) 에, 성공이면 `gdwMainThreadId = GetCurrentThreadId()` 후 **`S_OK`** | `InitMainThreadWnd +0x50/+0x57` (인자는 `+0x8 ~ +0x4c`: `+0x1b R9D=0x88000000` · `+0x21 RDX=[gOleWindowClass]` · `+0x38 [R11-0x28]=-3` = `[RSP+0x40]` 부모 · `+0x40~+0x4c` 위치·크기 `0x80000000` 넷 · `[R11-0x18]=g_hinst` = `[RSP+0x50]`), `+0x5c/+0x63/+0x66 JZ +0x85`, `+0x68/+0x6f`, `+0x74` |
| ④ 실패 | `GetLastError()` → HRESULT | 같은 규칙 | 0 → `0x8007000e`, 양수 → `0x80070000 \| …`, 음수 → 그대로 | `+0x85/+0x8c` · `+0x91 MOV EBX,EAX` · `+0x95 JLE` · `+0x9b MOVZX` · `+0x9e OR` · 조각 `FUN_180267d96 +0x49 TEST EBX,EBX` · `+0x4b JS` · `+0x51 MOV EBX,0x8007000e` (= `InitMainThreadWnd +0x138b97`) |

- 값 풀이: `0x88000000` = `WS_POPUP (0x80000000) \| WS_DISABLED (0x08000000)`, `0x80000000` = `CW_USEDEFAULT`, `(HWND)-3` = `HWND_MESSAGE` (공개 상수; 이 빌드의 `.asm` `+0x38 MOV [R11-0x28],-0x3`).
- `RegisterOleWndClass` 결과는 **HRESULT** (`DWORD`) 다. 호출 쪽 `ThreadFirstInitialize +0x309 MOV EBX,EAX` · `+0x30b TEST` · `+0x30d JNS`. 둘째 등록 실패 때 `0x8007000e` 면 `CoInitializeEx` 가 `E_OUTOFMEMORY` 를 돌려준다.
- 추적 호출 (`ComTraceMessageT`, `IsWppLevelEnabled`) 은 기록용 (`gfEnableTracing`·TraceLogging 이 켜졌을 때만) — 결과에 영향 없음.
- **창 함수 `OleMainThreadWndProc`** (`ms/disassemble/combase/OleMainThreadWndProc.asm`): 메시지 `0x10 (WM_CLOSE)`·`0x2 (WM_DESTROY)` 는 `gfDestroyingMainWindow` 가 0 이면 막고 (추적 후 0 반환, +0x37~+0x6c), `0x405` 는 `GetSingleThreadedHost(lParam)` 후 1, **나머지 전부 `DefWindowProcW`** (+0x1f). 만들기 메시지 (`WM_NCCREATE`·`WM_CREATE` 류) 에서 combase 가 꼭 할 일은 없다.

### 3-4. 질문별 답 (요청서 순서)

1. **"등록 → 지움 → 등록" 차례:** 맞다. 조건 = 첫 `RegisterClassW` 가 0 일 때 (`RegisterOleWndClass +0x77 TEST RAX,RAX` / `+0x7a JZ`). 지움 결과·`GetLastError` 는 안 본다.
2. **둘째 등록도 실패하면:** `GetLastError()` 가 0 이면 **`0x8007000e`** (`E_OUTOFMEMORY` = `HRESULT_FROM_WIN32(14)`), 아니면 `0x8007xxxx`. 이 값이 `RegisterOleWndClass` → `ThreadFirstInitialize` → `_CoInitializeEx` → `CoInitializeEx` 반환까지 그대로 간다 (3-2 의 5). 그 뒤 CLR 이 `OutOfMemoryException` 으로 읽는지는 clr 이 이 레포에 없어 `[공백]` — 에뮬레이터 로그 (`OutOfMemoryException`) 가 그렇다는 정황 (HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY) = E_OUTOFMEMORY 를 메모리 부족으로 옮기는 것은 공개된 흔한 규칙이지만 이 빌드 확인 아님 `[추측]`).
3. **성공하면 바로 다음:** `ThreadFirstInitialize` 가 `gdwMainThreadId == 0` 이면 `InitMainThreadWnd` → `CreateWindowExW` (표 ④). 클래스 = 방금 받은 ATOM, 부모 = `HWND_MESSAGE (-3)`, 스타일 `0x88000000`. **실패는 `CoInitializeEx` 를 실패시킴** (치명 = 호출자에게 실패 HRESULT; 그 뒤는 호출자 몫).
   - 에뮬레이터 새 로그 해석 (AE01 뒤, `NtUserCreateWindowEx = 0` 다음에 `NtUserUnregisterClass`): `[정적]` 줄과 맞는다 — `InitMainThreadWnd` 실패 → `ThreadFirstInitialize +0x2c9~+0x2d2` 가 `g_cClassicSTAInits` 를 내려 0 이면 `UnRegisterOleWndClass` (`UnregisterClassW("OleMainThreadWndClass", g_hinst)`) 를 부른다. 이때도 `GetLastError() == 0` 이면 `0x8007000e` → `OutOfMemoryException`. (로그 쪽 사실은 요청서가 준 것, 우리는 줄만 대조.)
4. **앞의 `RegisterWindowMessageW` 가 0 이면 치명인가:** **combase 의 이 길에는 그 호출이 없다.** combase 의 `RegisterClipboardFormatW` 가져오기 칸 (`0x363540`) 을 읽는 곳은 `CLIPFORMAT_UserUnmarshal +0xad` 와 `CLIPFORMAT_UserUnmarshal64 +0x1f7` 둘 (`refs`, call 명령 위치) — 마샬링 쪽. `ThreadFirstInitialize`·`RegisterOleWndClass`·`InitMainThreadWnd` 의 `_calls.tsv` 에 없음. 그리고 user32 안에서 `NtUserRegisterWindowMessage` 를 부르는 곳도 `RegisterClipboardFormatA/W` 뿐이라 **등록 길 (`RegisterClassExWOWW`) 은 안 부른다.** 로그 seq 9941 의 호출자 = `[공백]` (clr 등 다른 모듈일 수 있으나 이름은 안 지어냄).
5. **클래스 이름 문자열:** `OleMainThreadWndClass` (`.rdata` `0x1802cd830`, UTF-16, `RegisterOleWndClass +0x20 LEA RSI` · `UnRegisterOleWndClass +0xb LEA RCX`), 창 이름 `OleMainThreadWndName` (`0x1802cd928`, `InitMainThreadWnd +0xd`), STA 스레드 창 이름 `OLEChannelWnd` (`0x1802ccf48`, `GetOrCreateSTAWindow +0x44`).

### 3-5. 전역 칸 (combase)

| 이름 | 주소 (rva) | 뜻 |
|---|---|---|
| `g_hinst` | `0x180337fb0` (`0x337fb0`) | combase 모듈 핸들 (로그의 `0x7ffffcf20000`) |
| `g_cClassicSTAInits` | `0x180338008` | 클래식 STA 초기화 횟수 (1 이 되는 순간 `RegisterOleWndClass`) |
| `gOleWindowClass` | `0x180338870` | 등록 ATOM (qword) |
| `ghwndOleMainThread` | `0x180338878` | 숨은 창 핸들 |
| `gdwMainThreadId` | `0x180338864` | 숨은 창을 만든 스레드 ID (0 이면 아직 없음) |
| `gfDestroyingMainWindow` | `0x180338868` | `UninitMainThreadWnd` 가 부수는 중 (`+0x3c` 에서 1) |

---

## 지금 우리 (PE_emulator) 맞나·틀리나

쓰는 곳: `ext/syscall/syscall_layer_cpp/` — `src/gui/nt_user_register_class_ex_wow.cpp` · `src/gui/nt_user_get_atom_name.cpp` · `src/layer.cpp:743 ~ :745` · `include/syscall_layer/layer.hpp:1066, 1068` · `tests/user32_stub.cpp`.

| 줄 | 지금 우리 | 판정 | 근거 |
|---|---|---|---|
| `NtUserRegisterClassExWOW` 늘 `0xC000` (`nt_user_register_class_ex_wow.cpp`) | 0 아닌 ATOM 하나 | **맞다** | user32 도 combase 도 AX 만, 0 이냐만 본다 (`RegisterClassExWOWW +0x242·+0x265`, `RegisterOleWndClass +0x6d·+0x77`). 16비트 안 (`0xC000`). 몇 번 불러도 같은 값도 무해 (둘째 등록은 지운 뒤 한 번뿐). |
| 인자 안 읽음 · 메모리 안 씀 · `outputs.empty()` | | **맞다** | 출력 인자 없음 (표 1-2). 인자 4 (`CLSMENUNAME`) 는 user32 가 채운 **입력** 이고 성공 뒤 안 읽음. `gSharedInfo`·`gpsi` 는 이 길에서 안 읽음. |
| 헤더 주석 "`(WNDCLASSEXW 위치 근거 없음)`" | 읽지 않는 이유 | **이제 근거 있음** | 인자 1 = RCX = user32 스택의 `WNDCLASSEXW` 복사본 (0x50 B), 인자 2 = `UNICODE_STRING*` (클래스 이름, `+0x0 Length`·`+0x2 MaxLen`·`+0x8 Buffer`, 아톰이면 `Length 0·Buffer=아톰`). **이름을 기억해야 할 일이 생기면** (예: `NtUserGetAtomName` 이 진짜 이름을 채우려면) 인자 2 의 `Buffer` 를 읽으면 된다. 지금 동작엔 필요 없음. |
| 헤더 주석 "부르는 쪽: user32 `RegisterClassExW` → …" | | **일부 틀림** | combase 가 부르는 것은 **`RegisterClassW`** (`RegisterOleWndClass +0x61`). `RegisterClassExW` 는 이 길에 안 나옴. 둘 다 `RegisterClassExWOWW` 를 거쳐 같은 syscall 로 간다. |
| `layer.cpp:743` `{…, 0, 0}` (인자 수 0) | | **표기만 다름** | 진짜 인자는 **7개** (표 1-2). stub 이 안 읽으니 동작엔 무해. 나중에 인자를 기록하려면 7 (u32 칸 마스크: 인자 5 는 WORD, 인자 6 은 DWORD). |
| `NtUserUnregisterClass` 기본 대답 0 | | **안전하다** | combase 는 결과를 안 봄 (`UnRegisterOleWndClass +0x22`, `RegisterOleWndClass` 첫 지움). user32 는 0 이면 아무것도 안 함. 참(1) 을 돌려줄 때도 출력 24 B 는 안 써 줘도 된다 (user32 가 0 으로 채워 둠). |
| `NtUserRegisterWindowMessage` 기본 대답 0 | | **지금 막힘과 무관** | combase STA 길이 안 부름 (3-4 의 4). user32 는 반환 UINT 를 그대로 돌려줌 (오류값 안 씀). 부르는 쪽 `[공백]`. |
| 실패를 흉내 낼 일이 생기면 | | **주의** | 등록을 0 으로 돌리려면 **TEB `LastErrorValue` (+0x68) 에 오류값을 써 줘야** 한다 — user32 는 안 씀. 안 쓰면 combase 가 `GetLastError() == 0` 으로 읽고 `0x8007000e` 를 만든다 (지금 로그의 `OutOfMemoryException`). |
| `NtUserGetAtomName` `a[0] & 0xffff == 0xc000` 이면 1 | | **동작은 하나, 뜻이 다름** (덧붙임 4 참고) | 인자 2개, 반환은 **글자 수**. |

---

## 덧붙임 — PE_emulator 원 요청의 물음 4·5 (이번 요청서 밖, 이미 뽑은 줄에서 읽은 것)

PE_emulator 원 요청 파일 (`298_internals-user32-regclass.md`, 2026-11-22 고침) 이 물음 4 (`NtUserGetAtomName`), 5 (`NtUserCreateWindowEx`) 를 더했다. 4 는 이번에 뽑은 `ClassNameToVersion` 안이라 같이 닫는다. 5 는 아래 사슬만 적고 본격은 다음 요청으로 둔다.

### 4. `NtUserGetAtomName` (syscall `0x109c`) — `[정적]`

- **(가) 부르는 곳·조건:** `user32!ClassNameToVersion` (rva `0x1d0e0`) 의 `+0x226` (call) / `+0x22d` (ret) — 클래스 이름 인자가 **아톰일 때만** (`+0x98 TEST RBX,-0x10000` → `+0x9f JZ +0x217`). 같은 호출이 `VersionRegisterClass +0xf4` 에도 있다. `ClassNameToVersion` 을 부르는 곳: `VerNtUserCreateWindowEx +0x177` (창 만들기 길) · `UnregisterClassW/A` · `RegisterClassExWOWW/A` · `GetClassInfoExW/A`. (`UnregisterClassW` 에 combase 가 **문자열** 을 넘기면 이 호출은 안 탄다.)
- **(나) 인자 2개:** RCX = 아톰 (`+0x21f MOVZX ECX,BX`), RDX = `UNICODE_STRING*` (`+0x217 LEA RDX,[RBP-0x61]`). 부르기 전 user32 가 채우는 칸: `MaximumLength = 0x200` (`+0x21b MOV [RBP-0x5f],DI`), `Buffer = RtlAllocateHeap(pUserHeap, 0, 0x200)` 로 받은 블록 (`+0x222 MOV [RBP-0x59],RSI`, 이 블록은 **0 으로 채우지 않은** 상태 — 플래그 0, `+0x72 XOR EDX,EDX`). 커널이 쓰는 칸 = 버퍼의 글자.
- **(다) 반환:** **글자 수 (WCHAR 개수)** 다 — `+0x232 ADD AX,AX` 로 2배 해서 `UNICODE_STRING.Length` (바이트) 에 **user32 가 직접** 쓴다 (`+0x235 MOV [RBP-0x61],AX`). 0 이면 (`+0x239 JNZ` 안 탐) `+0x23f XOR ECX,ECX` → `+0x244 JMP +0x147` → **`ClassNameToVersion` 이 NULL** → `VerNtUserCreateWindowEx +0x17f/+0x182 JZ +0x323` → `XOR EAX,EAX` 로 **커널 `NtUserCreateWindowEx` 를 안 부르고** NULL (`SetLastError` 없음) — AE01 시도 1 (0 을 줬더니 창 만들기를 안 부름) 과 맞는다.
- **(라) 받은 이름의 쓰임:** 활성화 컨텍스트 찾기의 열쇠 — `RtlFindActivationContextSectionString(1, 0, 3, &이름USTR, &데이터 (cbSize 0x70))` (`+0x131/+0x138`; 구역 3 = 창 클래스 바꿔치기). 결과: `0xC0150001` (STATUS_SXS_SECTION_NOT_FOUND) · `0xC0150008` (STATUS_SXS_KEY_NOT_FOUND) → **원래 아톰을 그대로 돌려줌** (`+0x13d~+0x144 MOV R14,RBX`, `+0x19c~+0x1a1`); 그 밖 실패 → NULL (`+0x1a5 JS +0x23f`); 성공하고 데이터 형식이 1 이면 매니페스트가 가리키는 이름으로 바뀜 (`+0x1ab~+0x1fe`). 그래서 **이름이 달라도 창 만들기에 넘기는 인자는 안 바뀐다** (찾기에서 NOT_FOUND 계열이 나오는 한).
- **한 줄 답: stub 은 진짜 클래스 이름을 채우지 않아도 된다 — 0 이 아닌 값만 돌려주면 된다.** 이름을 안 채우고 `1` 을 주면 `Length = 2` 인 한 글자 (버퍼 첫 `WCHAR` 쓰레기) 로 찾기가 돌고 NOT_FOUND 로 끝나 아톰이 그대로 간다 (우리 로그 13/13 과 맞음). 더 맞게 하려면 **UTF-16 이름을 `[USTR+8]` 버퍼에 쓰고 글자 수 (끝 NUL 제외 `[추측]`) 를 돌려준다**. 반환 0 은 창 만들기 중단. `UNICODE_STRING.Length` 는 user32 가 덮어쓰므로 stub 이 안 써도 된다.
- 인자 수: **2** (`layer.cpp:745` `{…, 1, 0x01}` 은 읽는 인자 1개 표기라 동작엔 맞음, 첫 인자 `& 0xffff` 는 `MOVZX ECX,BX` 와 같은 꼴).

### 5. `NtUserCreateWindowEx` — 사슬·진입만 (본격은 다음 요청)

- 사슬 `[정적]`: `CreateWindowExW` (rva `0x1c110`, `+0x7d/+0x82`) → `CreateWindowInternal` (rva `0x1c1a4`, `+0x1de/+0x1e3`) → `VerNtUserCreateWindowEx` (rva `0x1c5c4`; PDB 시그니처 `HWND(DWORD, LARGE_STRING*, LARGE_STRING*, DWORD, int×4, HWND, HMENU, void*, void*, ZBID, DWORD, DWORD)`) → `NtUserCreateWindowEx` (`+0x22f/+0x236`, win32u `0x106f`). **스택 칸 13개 + 레지스터 4개 = 인자 17개** (`+0x1cb ~ +0x22b`, 가장 높은 칸 `[RSP+0x80]`).
- **둘째·셋째에 같은 아톰이 가는 까닭** (로그 `NtUserCreateWindowEx(0x0, 0xc000, 0xc000, …)`): 둘째 = 준 클래스 (`+0x1e1 MOV RDX,RDI`), 셋째 = `ClassNameToVersion` 결과 (`+0x17c MOV R14,RAX` → `+0x1c1 MOV R8,R14`) — 매니페스트에 바꿔치기가 없으면 같은 아톰이 그대로 나온다. 결과가 포인터 (문자열) 이면 `LARGE_STRING` (`RBP-0x38`) 로 감싼다 (`+0x188~+0x1af`). `[정적, 대충 읽음 — 확정은 다음 요청]`
- **창 만들기 0 일 때:** `VerNtUserCreateWindowEx +0x263/+0x26a GetLastError` → `+0x26f CMP EAX,0x57f` (1407 = `ERROR_CANNOT_FIND_WND_CLASS`) 이면 기본 클래스 등록 (`RegisterDefaultClass`, `+0x288/+0x28d`) 이나 `VersionRegisterClass` (`+0x2d2/+0x2d7`) 로 **다시 시도**, 아니면 그냥 NULL. **오류값이 0 이면 다시 시도 없이 NULL.** 그 NULL 이 combase `InitMainThreadWnd +0x66 JZ +0x85` 로 가서 위 3-3 의 ④ 실패.
- **받은 핸들:** `CreateWindowInternal +0x1e3 TEST RAX,RAX` → `+0x1ec JNZ +0x279` → `+0x27c JZ +0x1f2` (`R13D == 0`) → `+0x1f6 JZ +0x211` (`[RBP-0x48] == 0`) → `+0x211 MOV RAX,RBX` → `RET` (`+0x234`): 이 갈래는 받은 값을 그대로 돌려주는 것으로 읽힘 (`R13D`·`[RBP-0x48]` 뜻 미확인 — MDI 아닌 갈래로 `[추측]`); `ValidateHwnd` (`+0x25d/+0x262`) 는 **부모 핸들** (`[RBP+0xa0]`) 을 검사하는 갈래. 확정은 다음 요청.
- **만드는 동안 콜백:** `OleMainThreadWndProc` 가 만들기 메시지를 `DefWindowProcW` 로 넘긴다 (위 3-3) → **combase 쪽 요구는 없음.** 커널이 창 함수를 불러 주는지·결과를 user32 가 읽는지는 win32kfull·`KeUserModeCallback` 쪽이라 이번 범위 밖.

---

## 읽을 때 주의 (스크립트 문제 아님)

- `ms/decomp/combase/RegisterOleWndClass.c` 는 `hbrBackground` 를 `___xmm_…02`, `lpszMenuName` 을 `_UNK_1802cd868` 로 그렸다. `.asm` (`+0xf MOVDQA XMM1,[0x1802cd860]` · `+0x57 MOVDQA [RSP+0x60],XMM1`) 으로 보면 16바이트 상수의 낮은 8바이트 = 2 (`hbr`), 높은 8바이트 = 0 (`menu`) 이다.
- `ms/decomp/user32/RegisterClassExWOWW.c` 는 세 번째·네 번째 지역 변수 이름이 엉켰다 (`puVar8` 등). 인자 자리는 `.asm` `+0x215 ~ +0x236` 으로 다시 셌다.
- `ms/decomp/user32/ClassNameToVersion.c` 의 `NtUserGetAtomName()` 호출은 인자 없이 그려졌다. `.asm` `+0x21f ~ +0x226` 에서 RCX·RDX 두 개를 놓는다.
- `RegisterOleWndClass` 는 함수 본체가 두 범위로 나뉘어 (`+0x0 ~ +0x92` 와 `+0x1397b4 ~ +0x139865`) 뒷부분 오프셋이 크다. 뒷 범위가 WinDbg 에서 같은 `RegisterOleWndClass+0x1397..` 로 보일지는 확인 전 `[추측]`.

---

## 동적으로 확인할 것

1. **진짜 ATOM 값과 `CLSMENUNAME` 이 정말 입력인지.** `user32!RegisterClassExWOWW+0x23d` (call 뒤) 에서 `ax`, 그리고 `dq @rbp-0x80 L3` 를 부르기 전·후에 비교. (정적으로는 커널이 안 쓴다고 읽음.) 조건: combase 가 부른 것만 — `poi(@rsp+0x70)` 의 이름이 `OleMainThreadWndClass`.
2. **`NtUserUnregisterClass` 출력 24 B 가 진짜 커널에서 0 으로 남는지** (메뉴 이름 없는 클래스). `user32!UnregisterClassW+0xd7` 에서 `eax`·`dq @rsp+0x58 L3`.
3. **`NtUserGetAtomName` 의 진짜 반환·버퍼.** `user32!ClassNameToVersion+0x22d` 에서 `ax` 와 `du poi(@rbp-0x59) L?` (이름이 `OleMainThreadWndClass` 21글자 인지, 끝 NUL 제외인지).
4. **STA 길 확인:** `combase!ThreadFirstInitialize+0x309` (`eax` = 등록 결과 HRESULT), `combase!InitMainThreadWnd+0x57` (`rax` = 숨은 창 핸들), `combase!RegisterOleWndClass+0x1397f5` (실패 때만, `eax` = `GetLastError`). 전역 `combase+0x338870` (`gOleWindowClass`), `combase+0x338864` (`gdwMainThreadId`).
5. 진짜 Windows 에서 `IsCreateWindowExWPresent` → `ApiSetQueryApiSetPresence(…window-ext-l1-1-0)` 가 참인지 (`combase!IsCreateWindowExWPresent+0x2a` 뒤 `eax`·`[rsp+0x30]`). 우리 쪽은 로그가 이 문을 지났으니 통과 중.
6. 로그 seq 9941 (`NtUserRegisterWindowMessage`) 의 호출 스택 — 호출자를 모름 `[공백]`.

## 다음 요청 후보

- **`NtUserCreateWindowEx` 본격** (PE_emulator 원 요청 물음 5 (가)(나)(라)): 인자 17개 각각의 뜻·출력, 돌아온 핸들로 user32 가 syscall 없이 읽는 칸 (`gSharedInfo`·`ValidateHwnd`·`MDICompleteChildCreation` 갈래), 창 만든 뒤 `InitMainThreadWnd` 가 `CoInitializeEx` 로 돌아갈 때까지 더 부르는 user32 함수 (`+0x68 GetCurrentThreadId` 뿐으로 보이나 `DestroyWindow`·`UninitMainThreadWnd` 의 끝 쪽 확인). 뽑을 것: `VerNtUserCreateWindowEx`·`CreateWindowInternal` 통째 읽기 + win32u `NtUserCreateWindowEx` (이미 있음) + `ValidateHwnd`.
- `UninitMainThreadWnd` (`wCoUninitialize`·`CleanupRemoting`) 가 끝에 `IsWindow` (`+0x25/+0x2c`)·`DestroyWindow` (`+0x46/+0x4d`) 를 부르는 길 — 창이 만들어진 뒤 `CoUninitialize` 에서 쓰는 syscall (`NtUserDestroyWindow` `0x1095`) 의 stub 답.
- `RegisterWindowMessageW` 의 로그 seq 9941 호출자 (정적으로는 모름) — 동적 요청으로.

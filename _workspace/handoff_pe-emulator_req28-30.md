# PE_emulator 에 넘길 요약 — 요청 28 · 29 · 30 의 답

- 쓴 날: 2026-11-13 (windows-pe-internals 오케스트레이터)
- 이 파일은 **길잡이**다. 근거·오프셋·표는 run 문서 맨 위 "PE_emulator 쪽에서 쓸 것" 절에 있다 (아래 경로).
- 표시: `[정적]` 디컴파일 코드 · `[관찰]` 디버거/사진 · `[추측]` · `[공백]`. 이 파일의 "지금 우리" = PE_emulator.
- 요청 26 · 27 의 답은 앞 묶음 (run81 · run82 · run83 · run84) 에 있다. 이 파일은 28 ~ 30 만.

| 요청 | 답한 run (최종 근거) | 정적 결과서 |
|---|---|---|
| 28 user32 · gdi32 최소 길 | `runs/03_sprint/run85-user32-init.md`, **`run86-gdi32-init.md` (28 전체 결론 · 최소 조건 표 A · B)** | `_workspace/sast/82_*`, `83_*` |
| 29 SxS 활성화 문맥 | `run87-sxs-create.md`, **`run88-sxs-redirect.md` (29 전체 결론 · 칸 표 · 최소 블롭 B 바이트 표)** | `_workspace/sast/84_*`, `85_*` |
| 30 .NET 시작 길 | **`run89-dotnet-start.md` (0 ~ 10 단계 표 · 덤프에 더할 파일 · 할 만한가)** | `_workspace/sast/86_*` |

---

## 요청 28 — user32 · gdi32 초기화 최소 길

### 답 (한 줄씩)

- **지금 막힌 `NtOpenSection(\Sessions\N\Windows\SharedSection)` `0xc000003a` 는 정상 길이 아니다** `[정적]`. 그 syscall 은 user32 안에 없고 ntdll `CsrpConnectToServer +0x1b6` 에만 있다 — **`CsrPortHandle == 0` (이 프로세스의 첫 CSR 연결) 일 때만** 탄다. 정상이면 kernelbase 가 먼저 BASESRV 로 연결해 두어서 (run68 `CsrpConnectToServer +0x2df NtConnectPort` 출력 칸), user32 의 둘째 연결 (서버 3 USERSRV) 은 `NtAlpcSendWaitReceivePort` 한 번이다. → 엔진에서 **그때 `CsrPortHandle` 이 왜 0 이었나** 를 먼저 보면 된다 (run85).
- **user32 `DllMain` 은 무조건 부르는 win32k syscall 이 0 개** `[정적]`. `NtUserProcessConnect` · `NtUserInitializeClientPfnArrays` 는 `gfServerProcess` (csrss 같은 서버) 일 때만.
- **gdi32 진입점은 거의 일을 안 하고, 진짜 GDI 초기화는 user32 `+0x996 GdiDllInitialize` 한 번** `[정적]` → gdi32full `GdiDllInitialize` → CRT → gdi32full `DllMain` → **`+0x2e NtGdiInit2` (win32u `0x12ee`)**. "로더가 gdi32full `DllMain` 을 부른다" 로 흉내 내면 안 맞는다.
- **무조건 나가는 첫 win32k syscall = `NtGdiInit2`** — 단 매니페스트 `uiAccess` 가 없고 (user32 `+0x5d2 NtUserSetProcessUIAccessZorder` 안 탐) 서버 프로세스가 아닐 때 `[정적]`. 커널은 이 안에서 스레드를 GUI 로 바꾼다.
- **데이터는 서로 다른 두 길** `[정적]`: user32 쪽은 csrss 답 0x248B 의 `+8` 부터 → `gSharedInfo`, 첫 8B → `psi` (읽기 가능 `0x1b68`B, 0 으로 채워도 됨). gdi32 쪽은 **PEB `+0xf8` (`GdiSharedHandleTable`) 이 가리키는 GDI 공유 메모리** (최소 `0x180150`B 읽기 가능, `+0x180140` DC 브러시 · `+0x180148` DC 펜 은 0 아닌 값, `+0x180094` 는 0). 두 메모리는 다르다 — `SharedSection` 을 채워도 GDI 쪽은 안 풀린다.

### 진입점까지 가는 최소 조건 (창 안 만드는 프로세스)

- 엔진이 꼭 맞춰야 할 바깥 답은 **다섯 자리** (run86 표 A 의 A1 · A2 · A3 · A7 · A11). 나머지는 실패해도 `DllMain` FALSE 가 아니다.
  - A1 `NtQuerySystemInformation(0)` 성공 (user32)
  - A2 `.mrdata` 열고 닫기 `NtProtectVirtualMemory` ×2 성공 (ntdll `RtlInitializeNtUserPfn`, 프로세스당 한 번만 성공)
  - A3 csrss USERSRV 연결 답 (`NtAlpcSendWaitReceivePort`, 그때 `CsrPortHandle ≠ 0`)
  - A7 GDI 힙 (`HeapCreate` → `NtAllocateVirtualMemory`) 성공
  - A11 **`NtGdiInit2` 가 0 아닌 64비트 값**
- **`NtGdiInit2` 에서 엔진이 할 최소 셋**: ① 0 아닌 값 반환 ② PEB `+0xf8` 에 위 GDI 공유 메모리 연결 ③ (선택) `KernelCallbackTable[84]` (`__ClientThreadSetup`) 콜백 — 안 해도 진입점까지는 갈 것으로 읽힘 `[추측]`.
- user32 는 `GdiDllInitialize` 반환이 **음수일 때만** FALSE (`0xc0000017` 힙 · `0xc0000001` gdi32full `DllMain` FALSE) → 로더 `0xc0000142`.
- 데이터 칸 전부: run86 표 B.

### PE_emulator 쪽 반영 후보 (차례 제안)

1. user32 `DllMain` 때 ntdll `CsrPortHandle` 이 0 인 까닭 찾기 (kernelbase 의 첫 BASESRV 연결이 엔진에서 어떻게 끝났나 — run68). 이게 풀리면 `NtOpenSection(SharedSection)` 이 아예 안 나간다.
2. USERSRV 연결 답 (`NtAlpcSendWaitReceivePort`) 에 0x248B 답 — `psi` 는 0 채운 `0x1b68`B 로 시작 (run85 데이터 칸 표).
3. `NtGdiInit2` (win32u `0x12ee`): 0 아닌 값 + PEB `+0xf8` 에 GDI 공유 메모리 (`0x180150`B, 스톡 핸들 두 칸 0 아님).
4. 지연 가져오기를 처음 풀 때마다 `NtProtectVirtualMemory` ×2, API 세트 세 이름이 `gdi32full.dll` 로 풀려야 함.
5. win32u syscall 이름표: `ms/disassemble/win32u/_syscalls.tsv` (이 레포 서버에 있음, 커밋은 안 됨).

### 정할 것 (PE_emulator · 사용자)

- **win32k 안쪽** (`win32kbase!GdiProcessCallout` — PEB `+0xf8` 칸을 실제로 채우는 쪽, win32kfull `NtGdiInit2` 구현, `SHAREDINFO` · `SERVERINFO` 칸 이름) 을 다음 요청으로 낼지. 요청 28 원문 "win32k 안쪽 깊이 안 넣음" 의 예외라 우리 대기열 Q161 ① · Q163 ① ② 에 "결정 대기" 로 둠.
- 창 만들기 · 메시지 루프 · GDI 그리기는 원문 "이번에 넣지 않는 것" → 새 요청 때 (Q163 ④ ⑤).

---

## 요청 29 — SxS 활성화 문맥

### 답 (한 줄씩)

- **블롭을 넣는 건 csrss, 읽는 건 ntdll 로더** `[정적]`.
  - `PEB+0x308` `SystemDefaultActivationContextData` ← 시스템 기본 문맥. **매니페스트 없는 exe 에도 늘** (test.exe 사진 `25996d00000`, 읽기 전용 공유 보기 4쪽 `[관찰]` run44).
  - `PEB+0x2f8` `ActivationContextData` ← 앱 문맥. **exe 매니페스트가 있을 때만** (test.exe 는 null `[관찰]`).
  - `PEB+0x2d8` `pShimData` ← 부모가 씀 (앱 호환, SxS 와 다른 이야기).
- **로더 차례**: TEB `+0x2c8` (스레드 문맥 스택) → `PEB+0x2f8` → `PEB+0x308`. 셋 다 없으면 `0xC0150008` (리다이렉트 없음) `[정적]`.
- **리다이렉트가 KnownDll 을 이긴다** `[정적]`: 리다이렉트가 먼저 돌고, 전체 경로가 나오면 KnownDll 은 "폴더가 System32 인가" 만 보고 거절 (`0xc0000135`) → 디스크 파일 길 (`NtOpenFile` · `NtCreateSection`).
- **블롭 안엔 키만** (조립 폴더 이름 + 파일 이름) — 루트 `C:\WINDOWS\WinSxS\` 는 ntdll 이 조립. 위치 칸이 전부 오프셋이라 **어느 주소에 붙여도 읽힌다**.
- 해시: 알고리즘 1 (X65599, 대문자로 바꾼 뒤), `COMCTL32.DLL` = `0xf1c4bc4f`. 구역 머리 `HashAlgorithm = 0xFFFFFFFF` 면 해시 안 쓰고 문자열만 비교.

### 지금 우리 판정

- "문맥 없음 → 리다이렉트 없음" **결과 자체는 맞다** (셋 다 없으면 진짜도 `0xC0150008`).
- 그러나 **"문맥 없음 → System32 KnownDll COMCTL32 (v5.82)" 는 작게 틀림**: 진짜는 매니페스트 없는 exe 에도 시스템 기본 문맥이 있어서 `C:\WINDOWS\WinSxS\amd64_microsoft.windows.common-controls_6595b64144ccf1df_5.82.26100.8328_none_87eede957a2ccddd\comctl32.dll` 로 간다. **파일 바이트는 System32 것과 같다** → 서수 #345 · #381 없음 문제는 그대로. 다른 것은 `FullDllName` · 연 syscall.
- **`PEB+0x308` 이 비어 있는 것 자체가 진짜와 다르다** (run87).
- notepad 처럼 v6 를 쓰는 exe 는 **앱 블롭 (`+0x2f8`) 이 없으면 v6 로 갈 길이 없다** — 원문 `0xc0000138` 벽은 이걸 넣어야 풀린다 `[추측: 서수가 v6 에만 있다는 원문 관찰에 기댐]`.

### PE_emulator 쪽 반영 후보 (차례 제안)

1. **TEB `+0x2c8` 을 널 아닌 빈 스택으로** — 로더가 널 검사 없이 따라간다 (`RtlFindActivationContextSectionString +0x141 ~ +0x148`). 진짜는 TEB `+0x290` (`_ActivationStack`).
2. 리다이렉트 → KnownDll **차례 지키기** ("KnownDll 먼저" 면 블롭을 넣어도 v6 로 안 감).
3. comctl32 v6 가 필요하면 **최소 블롭 B** (`0x1dc`B, 전체 경로 한 줄) 를 읽기 전용으로 붙이고 `PEB+0x2f8` 에 주소. 바이트 표는 run88 맨 위 (표만 보고 다시 만들 수 있게 적음). 서버에 만들어 둔 파일: `ms/disassemble/ntdll/_sast85/min_B.bin` (· `min_A.bin`, 커밋 안 됨). 그다음 단계로 A (윈도우 방식, 키만).
4. 시스템 기본 (`+0x308`) 까지 맞추려면 진짜 블롭 (`0x3084`B) 을 떠 와 그대로 붙이기 (손으로 만들기보다 쌈). 떠 오는 건 우리 쪽 Q104 (VM) 또는 Q165 ⑤ (사진 덤프 오프라인 페이지 걷기) 몫.
5. v6 파일이 엔진 파일 시스템에 있어야 함: `…/WinSxS/amd64_microsoft.windows.common-controls_6595b64144ccf1df_6.0.26100.9278_none_3e0d1ba8e3303201/comctl32.dll`.
6. 확인거리 (엔진 쪽): `NtCreateUserProcess` 흉내가 `PS_CREATE_INFO` 에 exe 매니페스트 주소 · 크기를 주는지, 앱 블롭을 넣을 때 `ProcessParameters.Flags` 비트 13 (`0x2000`, 매니페스트 있음) 도 켜는지 (진짜에선 이 비트가 "앱 문맥을 만들까" 를 정함, run87).

---

## 요청 30 — .NET exe 시작 길

### 답 (한 줄씩)

- ntdll 이 PE 머리에서 CLR 헤더 (디렉터리 14) 를 보면 `UseCOR` 를 세우고, **`LdrpCorInitialize` 로 `MSCOREE.DLL` 을 직접 올린 뒤 `LdrpInitializeProcess +0x18b4` 에서 첫 스레드 시작 문맥 `Rcx` 를 `_CorExeMain` 으로 덮는다** `[정적]`.
- IL-only 면 exe 의 import 걷기를 통째로 건너뛴다 (`+0x1a73/+0x1a76` 갈림, `+0x1a87 JMP`) `[정적]`. x64 IL-only exe 는 `AddressOfEntryPoint = 0` · import 없음.
- AnyCPU (PE32 IL-only) 는 `LdrpCorFixupImage` 가 메모리 속 머리를 PE32 → PE32+ 로 고친다 (`NtProtectVirtualMemory` 두 번). 이 함수는 디렉터리 14 결과를 NULL 검사 없이 읽는다 `[정적]`.
- test.exe 는 이 갈래를 안 탄다 (`UseCOR` 0 `[관찰]` run44). KUSD `ImageNumberLow/High` = `0x8664` · `ComPlusPackage` 1 `[관찰]` run44.
- mscoree `InitShimImpl` 은 환경변수 `COMPlus_*` → 레지스트리 `HKLM\Software\Microsoft\.NETFramework` (`InstallRoot`, `Policy\…`) → `<InstallRoot>\<버전>\mscoreei.dll` 을 찾는다. **없으면 (지금 덤프)** 상태 3 → 옛 길 `GetInstallation` 실패 → `ExitProcess(HRESULT)` (어떤 값인지 `[공백]`).

### 할 만한가 (run89 그대로)

- **1단계 (ntdll CLR 갈래 + mscoree `InitShimImpl` 까지) 는 할 만하다** — `LdrpInitializeProcess` 안 갈래 자리 열 + 작은 함수 넷 (`LdrpCorFixupImage` 388B · `LdrpCorValidateImage` 37B · `LdrpCorInitialize` · `LdrpCorProcessImports` 97B). mscoree 는 덤프에 있고 진짜 코드로 돈다 — 엔진은 레지스트리 · 파일 묻기에 대답만. 1단계만으로 .NET exe 가 "런타임 못 찾음" 으로 **정상적으로 끝나는 데까지** 간다.
- **2단계 (mscoreei → clr) 는 지금 아니다** — 덤프에 .NET Framework 폴더가 없고, 정확한 파일 목록은 VM 에서만 정해짐 (우리 Q167), 대상 샘플은 2% (310개).

### PE_emulator 쪽 반영 후보

1. (1단계를 한다면) run89 0 ~ 8 단계 표대로 ntdll 로더 흉내에 CLR 갈래 넣기 — `UseCOR` 판정 → `LdrpCorFixupImage` (PE32 만) → mscoree 올리기 → `LdrpCorValidateImage` (IL-only + TLS 면 `0xc000007b`) → `Rcx` 덮기 → import 건너뛰기.
2. mscoree 가 묻는 레지스트리 키에 "없음" 답을 바르게 (상태 3 길로 정상 종료하게).
3. 덤프에 더할 파일 (2단계 때): `Framework64\v4.0.30319\mscoreei.dll` · `clr.dll` · `clrjit.dll` · `mscorlib.dll` 등 + 레지스트리 값 — 목록 확정은 우리 쪽 Q167 (VM 에서 `ScriptRunner.exe` · `tzsync.exe`).

### 정할 것

- 1단계를 지금 넣을지 (PE_emulator 우선순위).

---

## 우리 쪽 (windows-pe-internals) 에 남은 것 — 요청 28 ~ 30 관련

| 대기열 | 무엇 | 종류 | 막힘 |
|---|---|---|---|
| Q161 · Q163 | user32 · gdi32 남은 정적 (① win32k 쪽은 결정 대기) | 정적 | win32k 쪽은 사용자 · PE_emulator 결정 |
| Q162 | user32 · gdi32 · comctl32 를 올리는 프로세스 동적 (notepad 류 / 새 GUI 샘플) | 동적 | 사용자 결정 (무엇으로 볼지) |
| Q164 · Q165 | SxS 남은 정적 (sxs 블롭 쓰는 쪽 · 정책 · ESI 끝까지 등), Q165 ⑤ 사진 덤프 오프라인 페이지 걷기로 test.exe 시스템 기본 블롭 몸 대조 | 정적 | 없음 |
| Q83 · Q104 | csrss 가 PEB 에 쓰는 순간 · test.exe 로 되는 SxS 확인 | 동적 (VM) | Q104 VM 단계 |
| Q166 | .NET 남은 정적 (커널 기계 고르기 · mscoree 옛 길 HRESULT 등) | 정적 | 없음 |
| Q167 | .NET exe 동적 (`ScriptRunner.exe` · `tzsync.exe`) | 동적 (VM) | 사용자 결정 |

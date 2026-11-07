# 2026-11-07 run71 — 이름 있는 객체 폴더 (Q136, PE_emulator 요청 22): `BasepGetNamedObjectDirectoryForToken +0x2b7` 은 csrss 공유 섹션 블록 **`BaseStaticServerData` (`0xa00`B) 의 `+0x20` 폴더 이름**을 읽는다 — 세션 0 갈래도 세션 N 갈래도 같은 줄. 블록은 csrss `basesrv!ServerDllInitialization` 이 세션 시작 때 짓는다. 진짜 프로세스에선 폴더 핸들·`DBWinMutex` 핸들이 **은퇴 전에** 이미 생긴다 (run44 사진)

## PE_emulator 쪽에서 쓸 것

### 한 줄 결론 (넷)

1. **멈춘 까닭**: `BasepGetNamedObjectDirectoryForToken +0x2b0` 이 전역 `BaseStaticServerData` (kernelbase rva `0x3aedb8`) 를 읽고, `+0x2b7` 이 그 블록 `+0x20` 에서 16바이트 (`UNICODE_STRING`) 를 읽는다. 전역이 0 이라 멈췄다 `[정적]`.
   - 토큰 대답이 세션 0 이든 세션 1 이든 **이 줄은 똑같이 탄다.** 두 갈래는 "지역 버퍼에 만드는 이름" 만 다르고, 그 이름은 둘째 인자 `EDX ≠ 0` 일 때만 쓴다. `BaseGetNamedObjectDirectory` 는 `EDX = 0` 으로 부른다 (`+0x9c XOR EDX,EDX`) `[정적]`.
2. **블록을 지어 넣으면 지나간다.** 아래 "최소 칸 목록" 이면 된다 `[정적]`.
3. **`+0x9c8` 은 반드시 `0xffffffff`.** 0 이면 `IsTimeZoneRedirectionEnabled` (`[+0x9c8] != -1`) 가 "시간대 리다이렉트 켜짐" 으로 읽는다. 서버도 처음 값을 `0xffffffff` 로 쓴다 (`ServerDllInitialization +0x3a3`) `[정적]`.
4. **블록을 지은 뒤엔 syscall 이 실패로 답해도 `DbgPrint` 길로 끝난다** `[정적]`.
   - `NtOpenDirectoryObject` 실패 → `BaseGetNamedObjectDirectory` 가 그 상태를 올림 → `CreateDBWinMutex` 가 `BaseSetLastNTError` 하고 0 → `OutputDebugStringA` 가 실패 깃발 (`[0x1803ad960] = 1`) 을 켜고 `+0xe5` 에서 `DbgPrint` 길로.
   - 성공시키려면 폴더 핸들 하나 + `NtCreateMutant` 성공이면 된다.

### 최소 칸 목록 — 1단계 (필수, `OutputDebugStringA` 를 크래시 없이 지나기)

| 무엇 | 값 꼴 | 정적으로 정해지나 |
|---|---|---|
| PEB `+0x88` (R, 내 뷰) · `+0x380` (C, csrss 쪽 뷰) | **R == C 로 두면 옮김이 0** (권장) | 꼴만. 값은 에뮬레이터 마음 |
| PEB `+0x98` (T) | 표 (6칸 × 8B = `0x30`B) 의 R 기준 주소 | 꼴 |
| 표 `T[1]` (= `[T+8]`) | S = 블록의 C 기준 주소 (R == C 면 블록 주소 그대로). basesrv 는 서버 번호 1 | 꼴 |
| 블록 `0xa00`B | `S − C + R` 자리, 읽을 수 있는 메모리, 안 쓴 칸은 0 | 크기 정해짐 |
| 블록 `+0x9e8` (8B) | **S** (블록의 csrss 쪽 자기 주소) | **정해짐** |
| 블록 `+0x20` (16B) | `{Length 0x38, MaximumLength 0x3a, Buffer → L"\Sessions\1\BaseNamedObjects"}` — `Buffer` 는 C 기준 주소 (R == C 면 그냥 주소), 글은 끝 NUL 포함 `0x3a`B | 꼴 정해짐. 세션 번호는 에뮬레이터가 정함 |

- 옮김 식 (`+0x2d3 ~ +0x343`): `Buffer += 블록지역주소 − [블록+0x9e8]`. R == C 이고 `[+0x9e8]` = S 면 0 이라 `Buffer` 를 그대로 쓴다.
- `BaseStaticServerData` 자체는 kernelbase 시작 함수가 `[PEB+0x98]+8 의 값 − PEB+0x380 + PEB+0x88` 로 셈한다 (`_KernelBaseBaseDllInitialize +0x2fb ~ +0x32b`, run68). PE_emulator 가 시작 함수를 흉내 낸다면 이 전역에 블록 지역 주소를 바로 넣어도 같다.
- (참고) `+0x28 Buffer` 가 0 이면 `+0x2ca TEST` · `+0x2cd JZ` 로 옮김을 건너뛰고 빈 이름으로 syscall 로 간다 → 0 만 든 블록도 이 길에선 크래시는 안 난다. 진짜와 같게 하려면 위 표대로 `+0x20`·`+0x9e8` 을 채운다.

### 권장 칸 — 2단계 (초기화·이웃 함수가 안전하게 읽게)

| 칸 | 값 | 이유 |
|---|---|---|
| `+0x9c8` | **`0xffffffff`** | 위 결론 3 |
| `+0x9d0` | 0 | 시간대 바뀜 번호 |
| `+0x9cc` | 0 또는 1 (VM 값은 동적) | `QueryDosDeviceW`·`DefineDosDeviceW`·kernel32 `DosPathToSessionPathW` 가 읽음 |
| `+0x38` | 0 가능 | `BaseRCNumber` 로 옮겨짐 (`+0x332`) |
| `+0x00` · `+0x10` | Windows·System 디렉터리 `UNICODE_STRING`. `Buffer` 0 이면 시작 함수가 건너뛰지만 `GetWindowsDirectoryW` 류가 빈 값을 줌. 채운다면 `C:\WINDOWS` · `C:\WINDOWS\system32` (VM 실제 값은 동적) | |
| `+0x3a` | 0 (빈 글) | kernel32 가 `wcslen` 으로 읽어 `BaseCSDVersion` |
| `+0x7f8` | 16B 0 | kernel32 `_BaseDllInitialize +0x1a4` 가 읽음 |
| `+0x170` | 0 | kernel32 가 `JZ` 로 건너뜀 |
| `+0x178 ~ +0x7f3` | 0 | 초기화 길은 안 읽음 (아래 "정정"). 나중 NLS 함수가 읽음 |
| `+0x9d8` · `+0x9f0` | `\Sessions\1\AppContainerNamedObjects` · `\Sessions\1\BaseNamedObjects` | 다른 토큰 갈래용 (test.exe 는 안 탄다 `[추측]`) |
| 나머지 | 0 | |

### 다른 길 하나 — 폴더 핸들 캐시

- `BaseGetNamedObjectDirectory` 는 전역 **`BaseNamedObjectDirectory`** (kernelbase rva `0x3af0e0`) 가 0 이 아니면 `+0x16` 에서 읽어 `+0x22` 에서 돌려주고 `+0x25` 에서 0 을 반환한다 — **syscall 도 블록 읽기도 없다** `[정적]`.
- 진짜 프로세스는 이 캐시가 `main` 보다 훨씬 앞 (kernel32 준비 구간) 에 이미 찼을 것이다 (아래 "진짜 프로세스에선 언제"). 그래서 PE_emulator 가 이 칸에 가짜 폴더 핸들을 넣어 두면 `main` 의 `OutputDebugStringA` 는 블록 없이도 이 함수를 지난다. 단 블록을 읽는 다른 함수 (시간대·DOS 장치·`CreateProcessInternalW` 등 — 아래 칸 표) 는 그대로 남는다.

### syscall 표 — `BaseGetNamedObjectDirectory` 한 번 (캐시가 비었을 때)

| 순서 | syscall | 인자 요약 | 조건 | 자리 (call / ret) |
|---|---|---|---|---|
| 1 | `NtOpenProcessToken` | (-1, 8 = QUERY, &h) | 보통 길 (흉내 안 냄) | `BaseGetNamedObjectDirectory +0x80 / +0x87` |
| 2 | `NtQueryInformationToken` | (h, `0xc`, 4B) | | `BasepGetNamedObjectDirectoryForToken +0xef / +0xf6` |
| 3 | `NtQueryInformationToken` | (h, `0x1d`, 4B) | | `+0x11d / +0x124` |
| 4 | `NtQueryInformationToken` | (h, `0x2a`, 4B) | | `+0x14c / +0x153` |
| 5 | `NtOpenDirectoryObject` | (&dir, `0xf`, OA{이름 = 블록 `+0x20`, Attributes `0x40`}) | 세션 0·세션 N 공통 | `+0x37f / +0x386` |
| 6 | `NtQueryInformationToken` | (h, `0x2c`, `0x120`B) | 셋째 인자 `R8D = 1` (이 호출자는 늘 1) | `+0x3b8 / +0x3bf` |
| 7 | `NtClose` | (h) | | `BaseGetNamedObjectDirectory +0xf4 / +0xfb` |

- 그 뒤 `CreateDBWinMutex +0x3c8 / +0x3cf` **`NtCreateMutant(&h, 0x2120001, OA{Root = 폴더, 이름 DBWinMutex, Attributes 0x80, SD}, FALSE)`** 하나.
- 토큰 클래스 이름 (`TokenSessionId` 등) 은 PDB 에 열거 타입이 없어 공개 번호표를 빌린 `[추측]` (결과서 1-B).
- 스레드가 흉내 내는 중이면 (`TEB+0x179c IsImpersonating ≠ 0`) `NtOpenThreadToken` + `NtSetInformationThread` ×2 + `NtClose` 가 더 붙는다.

### 진짜 프로세스에선 언제 생기나 (run44 사진 `[관찰]`, 새 실행 없음)

- **P1a ~ P1b** (`LdrpInitializeProcess +0x199d` kernel32 올리기 ~ `+0x1bb6` 그래프 켜기 앞): test.exe 핸들 표에 새로 생김 (`ms/logs/04_baggage-read_P1b.txt` 1481 ~ 1491줄 근처, P1a 목록엔 없음)
  - `0x80` **Directory `BaseNamedObjects`** — `GrantedAccess 0xf` (= `NtOpenDirectoryObject` 접근 `0xf` 와 같음). 객체 `ffffb689aac92670`, 부모 폴더 객체 `ffffb689aac922b0` (이름은 로그에 없음 — `\Sessions\1` 일 것 `[추측]`).
  - `0x7c` **Mutant `SM0:7632:304:WilStaging_02`** (`0x1f0001`) · `0x84`·`0x88` **Semaphore `…WilStaging_02_p0` · `…_p0h`** — 이름의 `7632` = test.exe PID `0x1dd0` (run44). 이 셋은 그 폴더 안에 있다 (P1b 폴더 목록).
  - 누가 열었나는 `[공백]`. 이름 (`Wil…`) 으로 보아 kernelbase 안 WIL 기능 단계 코드가 이름 있는 뮤텍스를 만들며 폴더를 연 것으로 보인다 `[추측]`. 캐시 `BaseNamedObjectDirectory` 를 쓰는 줄은 `BaseGetNamedObjectDirectory +0xbd` 말고도 셋 더 있다 — `BaseFormatObjectAttributes +0x13d` · `OpenMutexW +0x17c` · `OpenEventW +0x17c` (`_refs/0x3af0e0.tsv`).
- **P1b ~ P2** (그래프 켜기 = dll2 → dll1 TLS·DllMain ~ `NtContinue` 입구): 새 핸들 `0xbc` **Mutant `DBWinMutex`** (`0x120001`, `HandleCount 7` — 이미 있던 객체. P1b 폴더 목록에 이미 있음) (`_P2.txt` 2147 ~ 2151줄).
  - `0x120001` = `NtCreateMutant` 의 `0x2120001` 에서 `MAXIMUM_ALLOWED` 가 풀린 값 (READ_CONTROL · SYNCHRONIZE · QUERY_STATE). DACL 의 "모두 `0x120001`" 과 같다 `[정적 + 관찰]`.
  - 이 구간에 `OutputDebugStringA` 를 처음 부르는 건 sample 코드다: dll2 TLS 콜백 `Dll2Tls` → `Log` (`[소스]` `dll2.c:7`, `log.h:37`), 그다음 `dll2!DllMain +0x25 / +0x2a → Log` (`[정적]` dll2 `.asm`, DllMain 도달은 run6·run7 `[관찰]`).
- **진입점 뒤 (`main` 첫 `Log`)**: 새로 생기는 짐이 없다 `[정적 추론]`. 뮤텍스 전역 `[0x1803ad968]` 이 이미 차 있어 `CreateDBWinMutex` 를 다시 안 부르고 (`+0xac ~ +0xb5`), 바로 기다림 → `OpenFileMappingW("DBWIN_BUFFER")` 실패 → `DbgPrint` 길.
- 그래서 **PE_emulator 가 은퇴 뒤 `main` 에서 처음 닿은 건 진짜와 다른 순서**다. 진짜는 (1) kernel32 준비 구간의 WIL 이름 객체 길, (2) dll2 TLS·DllMain 의 `Log` 가 먼저 간다. PE_emulator 에서 이 둘이 안 탄 까닭은 PE_emulator 쪽 확인 거리다 (우리 쪽은 모름).
- 사진은 kernelbase `.data` 를 안 읽었다 (run67·run68). 그래서 `0x80` 이 캐시 `BaseNamedObjectDirectory` 의 값이라는 것은 `[추측]` 이다.

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: kernelbase · kernel32 · basesrv · csrsrv · ntdll — 기준 Ghidra 프로젝트 그대로, 다시 analyze 안 함 (결과서). PDB GUID 는 flow.md 기준과 같다.
- 버전 확인 (결과서): PE_emulator 가 잰 사슬 `OutputDebugStringA` ret `+0xc2` → `CreateDBWinMutex` ret `+0x371` → `BaseGetNamedObjectDirectory` ret `+0xb0` → `BasepGetNamedObjectDirectoryForToken +0x2b0 MOV RAX,[0x1803aedb8]` · `+0x2b7 MOVUPS XMM0,[RAX+0x20]` 가 이 바이너리 asm 과 일치. test.exe 쪽 `main +0x12 / +0x17 → Log` · `Log +0x1e0 / +0x1e6 → [IAT] OutputDebugStringA` 도 우리 `ms/disassemble/test/` 와 같다 (flow-writer).
- 값 대조: run44 덤프 읽기 로그의 핸들 목록·폴더 목록 (`ms/logs/04_baggage-read_P1a.txt` · `_P1b.txt` · `_P2.txt`) 과 PEB CSR 칸 (run68 이 이미 옮긴 값) 을 flow-writer 가 다시 읽었다. 새 동적 실행은 없다.

## 목적

- PE_emulator 은퇴 뒤 test.exe 가 `main` 첫 `Log` 의 `OutputDebugStringA` 안에서 `BaseStaticServerData = 0` 으로 멈췄다. csrss 가 없으니 그 블록을 직접 지어야 한다.
- 알고 싶은 것: (1) kernelbase 세 함수 (`CreateDBWinMutex` · `BaseGetNamedObjectDirectory` · `BasepGetNamedObjectDirectoryForToken`) 의 인자·전역·syscall·갈래, (2) 블록의 칸 표와 누가 언제 채우나, PE_emulator 가 지을 최소 칸.

## 방법

- 정적: `_workspace/sast/66_named-obj-dir.md`
  - kernelbase `^CreateDBWinMutex$` · `^BaseGetNamedObjectDirectory$` · `^BasepGetNamedObjectDirectoryForToken$` depth 1 (callee 8개 포함 11개), `OutputDebugStringA` · `OutputDebugStringW` depth 0, `BasepAdjustObjectAttributesForPrivateNamespace`, NLS 넷 (`NlsProcessInitialize` 등), 시간대·DOS 장치 읽는 쪽 18개 (depth 0).
  - kernel32 읽는 쪽 14개, basesrv 쓰는 쪽 12개 (`ServerDllInitialization` 등), csrsrv `CsrSrvCreateSharedSection` · `CsrServerDllInitialization` · `CsrApiHandleConnectionRequest` (depth 0).
  - 새 `_refs`: kernelbase `0x3aedb8` (`BaseStaticServerData`) · `0x3ad958` · `0x3af0e0` (`BaseNamedObjectDirectory`) · `gpServerNlsUserInfo`, kernel32 `0xc2410`, basesrv `BaseSrvpStaticServerData`, csrsrv `CsrSrvSharedStaticServerData` · `CsrSrvSharedSectionBase`.
  - 보조 파일: `ms/disassemble/kernelbase/OutputDebugStringA.handler.objdump.txt` — Ghidra export 가 못 읽은 `__except` 착지 몸통 (`+0x81 ~ +0x312`) 을 objdump 로 뽑아 오프셋을 붙인 것 (아래 "도구 메모").
- 결과서: `_workspace/sast/66_named-obj-dir_result.md` (**분석성공**).
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `BaseGetNamedObjectDirectory +0x9c XOR EDX,EDX` (EDX = 0) · `+0xa3 LEA R9D,[RDX+0xf]` · `+0xa7 LEA R8D,[RDX+1]` · `+0xab / +0xb0` `BasepGetNamedObjectDirectoryForToken`; `BasepGetNamedObjectDirectoryForToken +0x2a8 TEST EDI,EDI` · `+0x2aa JNZ` · `+0x2b0 MOV RAX,[BaseStaticServerData]` · `+0x2b7 MOVUPS XMM0,[RAX+0x20]`; `BaseNlsDllInitialize +0x9 LEA RAX,[R8+0x178]` · `+0x10 MOV [gpServerNlsUserInfo],RAX`.
- flow-writer 가 `.asm` 으로 한 번 더 본 줄:
  - kernelbase `CreateDBWinMutex +0x342` · `+0x36c / +0x371` · `+0x391` · `+0x3c8 / +0x3cf`; `BaseGetNamedObjectDirectory +0x16` · `+0x63` · `+0x80` · `+0xbd` · `+0xf4`; `BasepGetNamedObjectDirectoryForToken +0x180` · `+0x190` · `+0x192` · `+0x324` · `+0x343` · `+0x37f / +0x386` · `+0x3b8 / +0x3bf`; `_KernelBaseBaseDllInitialize +0x2fb` · `+0x32b` · `+0x332`; `entry +0xa3` · `+0xa5` · `+0xa7` · `+0xb3`; `BasepAdjustObjectAttributesForPrivateNamespace +0x6` · `+0x10`; objdump `OutputDebugStringA +0xbd / +0xc2` · `+0x274 / +0x279`.
  - basesrv `ServerDllInitialization +0x365` · `+0x372` · `+0x39c` · `+0x3a3` · `+0x3ad` · `+0x4a5` · `+0x4fb` · `+0xa4f / +0xa56`; csrsrv `CsrLoadServerDll +0x25f` · `+0x37b / +0x380` · `+0x3da` · `+0x3ee`, `CsrSrvCreateSharedSection +0x1b8 / +0x1bf` · `+0x22c / +0x233` · `+0x267 / +0x26e` · `+0x2b4 / +0x2bb`, `CsrApiHandleConnectionRequest +0x9c` · `+0xd0`.
  - test `main +0x12 / +0x17`, `Log +0x1e0 / +0x1e6`; dll2 `DllMain +0x25 / +0x2a`.
  - `_refs`: kernelbase `0x3af0e0.tsv` (캐시 쓰는 줄 넷), `0x3ad958.tsv`, `gpServerNlsUserInfo.tsv` (읽는 줄 23 · 쓰는 줄 1), `0x3aedb8.tsv` (줄 40).
- 로그: 없음 (정적만). 값 대조만 run44 덤프 읽기 로그.

## 결과

### 정적 — 호출 지점 (`_calls.tsv`·`.asm`·objdump)

test.exe 첫 스레드 — 진입점 뒤 (`main`) 와 그 전 (dll2 TLS·DllMain):

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `test!main` | `+0x12` | `+0x17` | `test!Log` (sample 코드 — 지도엔 안 그림) | 늘 | [정적] |
| `test!Log` | `+0x1e0` | `+0x1e6` | `[IAT] OutputDebugStringA` (실제 대상 `KERNELBASE!OutputDebugStringA` — kernel32 export 를 거치는지는 안 봄) | 늘 | [정적] |
| `dll2!DllMain` | `+0x25` | `+0x2a` | `dll2!Log` (본문 안 뽑음 — `log.h` 같은 코드 `[소스]`) | 늘 | [정적] |
| `dll2!Dll2Tls` | ? | ? | `Log` (`dll2.c:7`) | 이유 1 | [소스] |

KERNELBASE:

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `KERNELBASE!OutputDebugStringA` | `+0x77` | `+0x7c` | `RaiseException(0x40010006 DBG_PRINTEXCEPTION_C, 0, 2, 인자)` | 늘. `__try` 안 | [정적] |
| `KERNELBASE!OutputDebugStringA` (착지 몸통) | `+0xbd` | `+0xc2` | `KERNELBASE!CreateDBWinMutex` | 예외를 아무도 안 받았을 때 + 뮤텍스 전역 `[0x1803ad968]` · 실패 깃발 `[0x1803ad960]` 둘 다 0 | [정적: objdump] |
| `KERNELBASE!OutputDebugStringA` | `+0xfd` | `+0x102` | `WaitForSingleObjectEx(뮤텍스, 10000, 0)` | 뮤텍스 있음 | [정적: objdump] |
| `KERNELBASE!OutputDebugStringA` | `+0x119` | `+0x11e` | `OpenFileMappingW(2, 0, L"DBWIN_BUFFER")` | 기다림 성공 | [정적: objdump] |
| `KERNELBASE!OutputDebugStringA` | `+0x274` | `+0x279` | `DbgPrint("%s", …)` (kernelbase 안 중개 `0x180126d60` 거침) | DBWIN 듣는 쪽 없음 / 뮤텍스 없음 | [정적: objdump] |
| `KERNELBASE!CreateDBWinMutex` | `+0x36c` | `+0x371` | `KERNELBASE!BaseGetNamedObjectDirectory(&dir)` | 늘. 실패면 `+0x373 JS` → `+0x4b0` `BaseSetLastNTError`, 0 반환 | [정적] |
| `KERNELBASE!CreateDBWinMutex` | `+0x3b3` | `+0x3b8` | (CFG 간접) `pfnAdjustObjectAttributesForPrivateNamespace` = `BasepAdjustObjectAttributesForPrivateNamespace` | 칸 ≠ 0 (`+0x391` 읽기). 불려도 `BaseUsePrivateNamespaces == 0` 이면 `+0x10 JZ` 로 바로 돌아옴 | [정적] |
| `KERNELBASE!CreateDBWinMutex` | `+0x3c8` | `+0x3cf` | `ntdll!NtCreateMutant(&h, 0x2120001, &OA, FALSE)` | 늘 (폴더가 있을 때) | [정적] |
| `KERNELBASE!BaseGetNamedObjectDirectory` | `+0x80` | `+0x87` | `ntdll!NtOpenProcessToken(-1, 8, &h)` | 캐시 0 + 흉내 안 냄 | [정적] |
| `KERNELBASE!BaseGetNamedObjectDirectory` | `+0xab` | `+0xb0` | `KERNELBASE!BasepGetNamedObjectDirectoryForToken(h, 0, 1, 0xf, &dir)` | 위와 같음 | [정적] |
| `KERNELBASE!BaseGetNamedObjectDirectory` | `+0xf4` | `+0xfb` | `ntdll!NtClose(h)` | 늘 | [정적] |
| `KERNELBASE!BasepGetNamedObjectDirectoryForToken` | `+0xef` · `+0x11d` · `+0x14c` | `+0xf6` · `+0x124` · `+0x153` | `ntdll!NtQueryInformationToken` 클래스 `0xc` · `0x1d` · `0x2a` | 늘 | [정적] |
| `KERNELBASE!BasepGetNamedObjectDirectoryForToken` | `+0x180` | `+0x187` | `ntdll!RtlGetCurrentServiceSessionId` | AppContainer 아님 | [정적] |
| `KERNELBASE!BasepGetNamedObjectDirectoryForToken` | `+0x40f` | `+0x414` | `RtlStringCchPrintfW(…, L"%ws\%ld\BaseNamedObjects", L"\Sessions", 세션)` | 세션 ≠ 서비스 세션 (`+0x192 JNZ`) | [정적] |
| `KERNELBASE!BasepGetNamedObjectDirectoryForToken` | `+0x37f` | `+0x386` | `ntdll!NtOpenDirectoryObject(&dir, 0xf, &OA)` — 이름 = 블록 `+0x20` | `EDX == 0` (`+0x2aa JNZ` 안 탐) | [정적] |
| `KERNELBASE!BasepGetNamedObjectDirectoryForToken` | `+0x42a` | `+0x431` | `NtOpenDirectoryObject(…, 2, …)` (같은 이름, TRAVERSE) → 되면 `Restricted` 하위 폴더 (`+0x6dd / +0x6e4`) | 위가 실패 (`+0x38f JS`) | [정적] |
| `KERNELBASE!BasepGetNamedObjectDirectoryForToken` | `+0x3b8` | `+0x3bf` | `NtQueryInformationToken(h, 0x2c, +0x260, 0x120)` | `R8D ≠ 0` (`+0x395 TEST ESI,ESI`) | [정적] |

KERNELBASE 시작 함수 (② 초기화, 이미 run68 — 이번에 뜻이 정해진 줄만):

| 자리 | 하는 일 | 표시 |
|---|---|---|
| `KERNELBASE!entry +0xa7 · +0xb3` (`+0xa3 CMP AL,1` · `+0xa5 JNZ`) | `pfnAdjustObjectAttributesForPrivateNamespace` (rva `0x3ad958`) ← `0x1800b92d0` (= `BasepAdjustObjectAttributesForPrivateNamespace`) | [정적] |
| `_KernelBaseBaseDllInitialize +0x32b` | `BaseStaticServerData` ← 블록 지역 주소 (run68) | [정적] |
| `BaseNlsDllInitialize +0x9 · +0x10` | `gpServerNlsUserInfo` ← **주소** `블록 + 0x178` (`LEA` — 안 읽음) | [정적] |

csrss (다른 프로세스, 세션 시작 때 — 흐름도 범위 앞):

| caller | call | ret | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `csrsrv!CsrSrvCreateSharedSection` | `+0x1b8` | `+0x1bf` | `NtCreateSection` (`\Sessions\N\Windows\SharedSection`) | | [정적] |
| `csrsrv!CsrSrvCreateSharedSection` | `+0x22c` | `+0x233` | `NtMapViewOfSection` (csrss 에 붙임) | | [정적] |
| `csrsrv!CsrSrvCreateSharedSection` | `+0x267` | `+0x26e` | `RtlCreateHeap(0x7008, …)` (공유 섹션 안 힙) | | [정적] |
| `csrsrv!CsrSrvCreateSharedSection` | `+0x2b4` | `+0x2bb` | `RtlAllocateHeap(…, 0x30)` → 6칸 표 `CsrSrvSharedStaticServerData` | | [정적] |
| `csrsrv!CsrLoadServerDll` | `+0x37b` | `+0x380` | (CFG 간접) 서버 DLL 초기화 함수 — basesrv 를 올릴 때 `basesrv!ServerDllInitialization` | 서버 DLL 마다 | [정적] |
| `basesrv!ServerDllInitialization` | `+0x372` | `+0x379` | `RtlAllocateHeap(BaseSrvSharedHeap, …, 0xa00)` (`+0x365 R8D=0xa00`) → **블록** | | [정적] |
| `basesrv!ServerDllInitialization` | `+0xa4f` | `+0xa56` | `NtCreateDirectoryObject` (4곳 중 하나 — 어느 호출이 어느 폴더인지는 안 가림) | | [정적] |

- `CsrLoadServerDll +0x37b` 의 `.asm` 주석은 `indirect:cfg:CsrServerDllInitialization` (csrsrv 자기 것 — 서버 번호 0) 로 풀려 있다. 같은 자리가 basesrv 때는 basesrv 의 초기화 함수를 부른다는 건 결과서 2-D (서버 DLL 객체 `+0x60` 칸 쓰임) 근거다.

### 호출 관계

```
(csrss — 세션 시작 때, 다른 프로세스)
csrsrv!CsrSrvCreateSharedSection
 ├─ +0x1bf  NtCreateSection (\Sessions\N\Windows\SharedSection)
 ├─ +0x233  NtMapViewOfSection (csrss 쪽 뷰 = C)
 ├─ +0x26e  RtlCreateHeap(0x7008)                       공유 섹션 안 힙
 └─ +0x2bb  RtlAllocateHeap(0x30)                       표 CsrSrvSharedStaticServerData (6칸)
csrsrv!CsrLoadServerDll                                  basesrv = 서버 번호 1
 │  +0x25f  서버 객체 +0x60 ← 공유 힙
 ├─ +0x380  (CFG 간접) basesrv!ServerDllInitialization
 │           +0x379 RtlAllocateHeap(BaseSrvSharedHeap, 0xa00) → 블록 S
 │           +0x39c [S+0x9e8] ← S (자기 주소) · +0x3a3 [S+0x9c8] ← 0xffffffff · +0x3ad [S+0x9d0] ← 0
 │           +0x4a5 · +0x4fb [S+0x20] ← \Sessions\N\BaseNamedObjects (Buffer 는 힙 사본)
 │           +0xa56 NtCreateDirectoryObject (4곳 중 하나)
 │           +0x38e 서버 객체 +0x60 ← S
 └─ +0x3da ~ +0x3ee  표[1] ← [서버 객체 +0x60] = S
csrsrv!CsrApiHandleConnectionRequest                     test.exe 의 NtConnectPort 때
    +0x9c ~ +0xd0  응답 +0x28 CsrSrvSharedSectionBase (C) · +0x30 표 주소 · +0x38 csrss PID

(test.exe 첫 스레드 — ② 초기화, run68)
ntdll!CsrpConnectToServer  +0x36d PEB+0x380 ← C · +0x379 PEB+0x88 ← R · +0x38d PEB+0x98 ← 표 − C + R
KERNELBASE!entry +0xb3  pfnAdjustObjectAttributesForPrivateNamespace ← BasepAdjust…
KERNELBASE!_KernelBaseBaseDllInitialize  +0x32b BaseStaticServerData ← [[PEB+0x98]+8] − C + R
 └─ +0x4e6  BaseNlsDllInitialize   +0x9/+0x10 gpServerNlsUserInfo ← 주소 BSSD+0x178 (안 읽음)

(test.exe 첫 스레드 — 첫 OutputDebugStringA: 진짜는 dll2 TLS 콜백, PE_emulator 는 main)
dll2!Dll2Tls → Log  /  test!main +0x17 → Log +0x1e6
 └─ KERNELBASE!OutputDebugStringA
      ├─ (1) +0x7c   RaiseException(0x40010006)          디버거가 받으면 아래 안 탐
      │      ── __except 착지 몸통 +0x81 ~ ──
      ├─ (2) +0xc2   CreateDBWinMutex                     전역 둘 0 일 때 (처음 한 번)
      │      ├─ (1) +0x371  BaseGetNamedObjectDirectory   캐시 0x3af0e0 이 0 일 때만 아래
      │      │      ├─ (1) +0x87   NtOpenProcessToken
      │      │      ├─ (2) +0xb0   BasepGetNamedObjectDirectoryForToken(h, 0, 1, 0xf)
      │      │      │      ├─ +0xf6 · +0x124 · +0x153  NtQueryInformationToken 0xc · 0x1d · 0x2a
      │      │      │      ├─ +0x187  RtlGetCurrentServiceSessionId   (세션 N 이면 +0x414 RtlStringCchPrintfW — 쓰이지 않는 지역 이름)
      │      │      │      │   +0x2b0 BaseStaticServerData · +0x2b7 [블록+0x20] · +0x324 [블록+0x9e8] 로 Buffer 옮김
      │      │      │      ├─ +0x386  NtOpenDirectoryObject(0xf, 이름 = 블록+0x20)
      │      │      │      └─ +0x3bf  NtQueryInformationToken 0x2c (BNO 격리)
      │      │      │   +0xbd CMPXCHG 캐시 ← 폴더 핸들
      │      │      └─ (3) +0xfb   NtClose(토큰)
      │      ├─ (2) +0x3b8  (CFG 간접) BasepAdjustObjectAttributesForPrivateNamespace   BaseUsePrivateNamespaces 0 → 바로 돌아옴
      │      └─ (3) +0x3cf  NtCreateMutant(0x2120001, Root = 폴더, "DBWinMutex", SD)
      │   +0xd5 CMPXCHG 0x3ad968 ← 뮤텍스 (0 이면 +0xc8 0x3ad960 ← 1)
      ├─ (3) +0x102  WaitForSingleObjectEx(뮤텍스, 10000)
      ├─ (4) +0x11e  OpenFileMappingW("DBWIN_BUFFER")        듣는 쪽 없으면 실패 → ReleaseMutex
      └─ (5) +0x279  DbgPrint("%s")                          DBWIN 길이 아니면
```

- 이 트리의 (n) 은 이 절에서만 쓴 번호다 (흐름도 그림 번호와 다름).

### 질문별 요약 (근거 줄은 결과서 해당 절)

| 질문 | 답 | 닫힘? |
|---|---|---|
| 1-A `BaseGetNamedObjectDirectory` | 캐시 `BaseNamedObjectDirectory` 하나. 캐시 있으면 syscall 0. 없으면 토큰 열기 → 아래 함수 → 캐시 `CMPXCHG` (먼저 넣은 쪽이 이김) → 토큰 닫기 | 정적으로 닫힘 |
| 1-B `BasepGetNamedObjectDirectoryForToken` | 갈래: 개인 이름공간 / AppContainer / 보통 (세션 0 · 세션 N). **보통 갈래 둘 다 `+0x2b0` → 블록 `+0x20`** 으로 폴더를 엶. 다른 갈래는 `+0x9d8` (AppContainer) · `+0x9f0` (개인 이름공간) | 정적으로 닫힘. 탄 갈래는 동적 |
| 1-C `CreateDBWinMutex` | SID 4 · DACL 3 (모두 `0x120001`, SYSTEM·관리자 `0x1f0001`) · SACL 낮은 무결성 쓰기 금지 → `NtCreateMutant` `DBWinMutex` | 정적으로 닫힘 |
| 1-D `OutputDebugStringA` 뒤 | `RaiseException(0x40010006)` → `__except` 몸통 → 뮤텍스 (처음만) → 기다림 → `DBWIN_BUFFER` 열기 → 실패면 `DbgPrint` | 정적으로 닫힘 (몸통은 objdump). 탄 길은 동적 |
| 2-A 타입 이름 | PDB 에 없음 (kernelbase·kernel32·basesrv·csrsrv 공개 PDB 구조체 0개) | 닫힘 (없음) |
| 2-B 칸 표 | `0xa00`B, 칸 23 (아래 "확인된 것" 표) | 자리·크기 정적으로 닫힘. 라벨은 `[추측]`, 값은 동적 |
| 2-C `+0x20` · 옮김 | `UNICODE_STRING`, `Buffer` 도 csrss 주소 — `블록지역 − [블록+0x9e8]` 로 옮김 | 정적으로 닫힘 |
| 2-D 누가 언제 | csrss 세션 시작 (`CsrSrvCreateSharedSection` → `CsrLoadServerDll` → `ServerDllInitialization`), 연결 응답 (`CsrApiHandleConnectionRequest`), 내 뷰 (`CsrpConnectToServer`), 전역 (`_KernelBaseBaseDllInitialize +0x32b`) | 정적으로 닫힘 |
| 2-E 최소 칸 | 위 "최소 칸 목록" | 정적으로 닫힘 |

## 확인된 것

- `[정적]` **블록 칸 표** (`0xa00`B = 2560B, 결과서 2-B 를 줄임. 라벨은 코드 쓰임·옛 공개 이름을 빌린 `[추측]`, 자리·크기·쓰는 줄은 `[정적]`):

| 오프셋 | 크기 | 라벨 `[추측]` | 서버가 씀 (`basesrv!ServerDllInitialization` 등) | test.exe 쪽 읽는 곳 — 언제 |
|---|---|---|---|---|
| `+0x000` | 16 | WindowsDirectory (`Buffer` +0x8) | `+0x3ed` · `+0x438` (`%SystemRoot%` 확장) | kernelbase `_KernelBaseBaseDllInitialize +0x33e` (② 초기화) → `KernelBaseGlobalData+0x8` |
| `+0x010` | 16 | WindowsSystemDirectory (`Buffer` +0x18) | `+0x443` · `+0x48f` | `+0x3da` (②) → `KernelBaseGlobalData+0x18` |
| **`+0x020`** | 16 | **NamedObjectDirectory** (`Buffer` +0x28) | `+0x4a5` · `+0x4b1` · `+0x4fb`. 서비스 세션 `\BaseNamedObjects`, 아니면 `\Sessions\N\BaseNamedObjects` | **`BasepGetNamedObjectDirectoryForToken +0x2b7`** (폴더를 처음 열 때) |
| `+0x030` | 6 | (옛 버전 칸 자리) | 쓰는 줄 못 찾음 | 못 찾음 |
| `+0x036` | 2 | CSDNumber | `+0x600` · `+0x666` | 못 찾음 |
| `+0x038` | 2 | RCNumber | `+0x671` | kernelbase `+0x332` (②) → `BaseRCNumber` |
| `+0x03a` | `0x100` | CSDVersion (`WCHAR[128]`) | `+0x604` · `+0x6c1` | kernel32 `_BaseDllInitialize +0x190` (②) → `BaseCSDVersion` |
| `+0x140` | `0x30` | TimeOfDay | `NtQuerySystemInformation(3, …)` (`+0x392` · `+0x3bc`) | 못 찾음 |
| `+0x170` | 8 | IniFileMapping (서버 포인터) | `BaseSrvInitializeIniFileMappings +0xd4` | kernel32 `_Kernel32LegacyLib_BaseDllInitialize +0x6f` (②) |
| `+0x178` | `0x67c` | NlsUserInfo | **초기화에선 안 채움** (0). `BaseSrvNlsLogon`·`NlsUpdateCacheInfo` 가 나중에 | `BaseNlsDllInitialize +0x9` 는 **주소만** (②). 안을 읽는 줄 23곳 (함수 13개 — `GetCurrentNlsCache`·`CompareStringW` …) 은 나중 |
| `+0x7f4` · `+0x7f5` | 1 · 1 | DefaultSeparateVDM · IsWowTaskReady | `+0x742` 등 · `+0x85a` 등 | `CreateProcessInternalW +0x73e` · kernel32 `GetNextVDMCommand +0x89` (나중) |
| `+0x7f8` | 16 | WindowsSys32x86Directory (`Buffer` +0x800) | `+0x49a` · `+0x493` (0) | kernel32 `_BaseDllInitialize +0x1a4` (②) |
| `+0x808` | 1 | fTermsrvAppInstallMode | `+0x5d6` 0 | kernel32 `TermsrvAppInstallMode +0x9` 등 (나중) |
| `+0x80c` | `0x1b0` | 터미널 서비스 클라이언트 시간대 | `BaseSrvSetTermsrvClientTimeZone +0xb4 ~` | `GetClientDynamicTimeZoneInformation +0x2e` 등 (나중) |
| `+0x9bc` | 12 | 클라이언트 시간 차 (`KSYSTEM_TIME`) | 같은 함수 `+0x12c` · `+0x137` | `GetLocalTime +0x61` 등 (나중) |
| **`+0x9c8`** | 4 | TermsrvClientTimeZoneId | **`+0x3a3` ← `0xffffffff`** | `IsTimeZoneRedirectionEnabled +0x7` 외 15곳 (나중) |
| `+0x9cc` | 1 | LUIDDeviceMapsEnabled | `NtQueryInformationProcess(-1, 0x1c)` (`+0xc07`) 결과 ≠ 0 이면 1 (`+0xc25`) | `QueryDosDeviceW +0x3ec` 등 (나중) |
| `+0x9d0` | 4 | 시간대 바뀜 번호 | `+0x3ad` 0 | `GetClientDynamicTimeZoneInformation +0xf` (나중) |
| `+0x9d8` | 16 | AppContainer 폴더 이름 (`Buffer` +0x9e0) | `+0x503` · `+0x565` | `BasepGetNamedObjectDirectoryForToken +0x4f5` (AppContainer 갈래) |
| **`+0x9e8`** | 8 | **자기 주소 (csrss 쪽)** | **`+0x39c MOV [RAX+0x9e8],RAX`** | 옮김 식 전부 — kernelbase `+0x3b4` (②) · `+0x324` (폴더) · kernel32 `+0x7b` (②) 등 |
| `+0x9f0` | 16 | 세션별 BaseNamedObjects (`Buffer` +0x9f8) | `+0x570` · `+0x5c8` | `+0x842` (개인 이름공간 갈래) |

  - "못 찾음" 은 이번 정적 범위 (kernelbase·kernel32·basesrv 의 요청 함수들) 에서 안 보였다는 뜻이다. advapi32·user32 등 다른 모듈은 안 봤다.
  - 공유 섹션 힙은 `RtlCreateHeap(0x7008, …)` 로 만든다. 플래그 `8` 이 0 채움 비트라 안 쓴 칸은 0 이라는 건 결과서 `[추측]`.
- `[정적]` **블록이 놓이는 사슬**: csrss 세션 시작 → `CsrSrvCreateSharedSection` (섹션·뷰 C·힙·표) → `CsrLoadServerDll` (basesrv = 번호 1) → `ServerDllInitialization` (블록 S, 칸 채움) → 표 `[1] = S`. test.exe 가 `NtConnectPort` 하면 `CsrApiHandleConnectionRequest` 가 응답 `+0x28 = C` · `+0x30 = 표` · `+0x38 = csrss PID` 를 주고, ntdll `CsrpConnectToServer` 가 내 뷰 R 로 PEB 세 칸을 쓴다 (run68). 블록 값은 test.exe 가 오기 전에 이미 다 있다. test.exe 쪽에서 블록에 쓰는 줄은 이번 범위에 없다 (읽기 전용 뷰라서 `[추측]`).
- `[정적]` **`BaseStaticServerData` 를 읽는 kernelbase 줄 40** (`_refs/0x3aedb8.tsv`): 시작 함수 2 · `BasepGetNamedObjectDirectoryForToken` 3 · 나머지는 시간대·시각 (`GetLocalTime`·`SetLocalTime`·`FileTimeToLocalFileTime`·`GetTimeZoneInformation` 등) · DOS 장치 (`QueryDosDeviceW`·`DefineDosDeviceW`) · `CreateProcessInternalW`. 초기화 길 (②) 에서 읽는 건 시작 함수 둘뿐이다 — 폴더 함수는 이름 있는 객체를 처음 만들거나 열 때.
- `[정적]` **캐시 `BaseNamedObjectDirectory` (rva `0x3af0e0`) 를 쓰는 줄 넷** (`_refs/0x3af0e0.tsv`, 모두 `CMPXCHG.LOCK`): `BaseGetNamedObjectDirectory +0xbd` · `BaseFormatObjectAttributes +0x13d` · `OpenMutexW +0x17c` · `OpenEventW +0x17c`. 뒤 셋은 결과서 범위 밖이라 그 안에서 폴더를 어떻게 여는지는 안 봤다.
- `[정적]` **`pfnAdjustObjectAttributesForPrivateNamespace`** (rva `0x3ad958`): 쓰는 줄은 kernelbase `entry +0xb3` 하나 (`_refs`), 값 `0x1800b92d0` = `BasepAdjustObjectAttributesForPrivateNamespace`. 읽는 곳은 `CreateDBWinMutex +0x391` 말고도 `BaseFormatObjectAttributes +0x96` · `CreateMutexExW +0xb1` · `OpenMutexW +0x91` · `CreateEventExW +0xed` · `CreateEventExA +0x128` · `OpenSemaphoreW +0x84` · `TryGetValueInternal +0x156`. 그 함수는 `BaseUsePrivateNamespaces` (rva `0x3a9f30`) 바이트가 0 이면 `+0x10 JZ` 로 바로 돌아온다. 그 바이트를 쓰는 줄은 결과서에 안 나왔다 — 0 이라는 건 `[추측]`.
- `[정적]` **`OutputDebugStringW`** (rva `0xc3cd0`) 는 ANSI 로 바꾼 뒤 `RaiseException(0x4001000a)` (`+0xbb / +0xc0`), 착지 `+0xc2` 에서 `OutputDebugStringA` (`+0xc7 / +0xcc`, objdump) — W 는 A 를 거쳐 같은 길.
- `[정적]` **전역 셋** (kernelbase `.data`): `[0x1803ad968]` DBWin 뮤텍스 핸들 (`+0xd5 CMPXCHG.LOCK`, 졌으면 `+0xe0 CloseHandle`) · `[0x1803ad960]` 만들기 실패 깃발 (`+0xc8 ~ +0xca`, 한 번 실패하면 다시 안 만듦) · `BaseNamedObjectDirectory`. 결과서는 앞 둘에 PDB 이름을 안 붙였다.
- `[정적]` `CreateDBWinMutex` 안 `GlobalAlloc` ×2 (SD·ACL 버퍼) 는 끝에 `GlobalFree` ×2 로 푼다 (`+0x456 / +0x45b` · `+0x49f / +0x4a4`) → 남는 힙 블록 없음. SID 4개도 `RtlFreeSid`.
- `[정적]` **TEB `LastErrorValue` (+0x68)**: `OutputDebugStringA +0x9e` 가 `gs:[0x68]` 을 저장 → `CreateDBWinMutex +0x3e9 / +0x3f0` `RtlSetLastWin32Error(ECX)` — `NtCreateMutant` 가 `0x40000000` (이미 있음) 이면 `+0x3e1 JZ` → `+0x4a6 MOV ECX,0xb7` → `+0x4ab JMP` 로 같은 호출, 아니면 `+0x3e7 XOR ECX,ECX` → `OutputDebugStringA +0x306 / +0x30d` `RtlSetLastWin32Error(저장값)` 로 되돌림 (objdump). 사진 `끝까지 0` (짐 지도 3.3) 과 맞다.

## 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

flow-writer 가 덤프 읽기 로그를 다시 읽었다.

- **PEB CSR 칸** (run68 이 이미 옮긴 값, P1b): `+0x88` R = `0x7ff4ecf70000` · `+0x98` T = `0x7ff4ecf70750` · `+0x380` C = `0x7ff5575f0000`. 그래서 **표는 공유 뷰 시작 + `0x750`** 에 있다. R ≠ C (진짜는 옮김이 0 이 아니다). 블록 S 자리와 블록 안 값은 사진에 없다 (kernelbase `.data`·공유 뷰 안을 안 읽음).
- **핸들 표** (`!handle` 구획):
  - P1a 목록 (`0x4 ~ 0x70`) 에는 이름 있는 객체 폴더가 **없다**.
  - P1b 목록에 새로: `0x74` ALPC Port (csrss 연결, run68) · `0x78` EtwRegistration · **`0x7c` Mutant `SM0:7632:304:WilStaging_02`** · **`0x80` Directory `BaseNamedObjects` (객체 `ffffb689aac92670`, `GrantedAccess 0xf`, 부모 폴더 객체 `ffffb689aac922b0` — 이름은 로그에 없음, `\Sessions\1` 일 것 `[추측]`)** · `0x84`·`0x88` Semaphore (`…_p0`·`…_p0h`) (`_P1b.txt` 1481 ~ 1491줄 근처).
  - P2 목록에 새로: **`0xbc` Mutant `DBWinMutex`** (`GrantedAccess 0x120001`, `HandleCount 7`, 부모 폴더 = 위 `0x80` 과 같은 `ffffb689aac92670`) (`_P2.txt` 2147 ~ 2151줄). P1b 끝은 `0xb0`.
  - 폴더 목록 (`0x80` 구획 안): `DBWinMutex` 객체 `ffff8285969fdda0` 는 P1b 에도 있다 → test.exe 가 만든 게 아니라 **이미 있던 것을 연 것** (`NtCreateMutant` 가 `0x40000000` 을 돌려주는 갈래 → `RtlSetLastWin32Error(0xb7)`).
  - 토큰 `TS Session ID: 0x1` (`_P2.txt` P2 05b 구획) — 세션 N 갈래 (`+0x3f4`) 를 탈 조건과 맞다.
- 정리 `[관찰 + 정적]`: 폴더 핸들은 **P1a ~ P1b** (kernel32 준비 구간), `DBWinMutex` 핸들은 **P1b ~ P2** (DLL 초기화 구간 — dll2 TLS·DllMain 의 `Log` `[소스]`) 에 생긴다. 둘 다 **은퇴 전**이다.
  - `0x80` 이 캐시 `BaseNamedObjectDirectory` 의 값인지는 kernelbase `.data` 가 사진에 없어 `[추측]`.
  - 핸들 번호가 `0x7c` (뮤텍스) < `0x80` (폴더) 인 것은 "토큰 핸들이 `0x7c` 를 먼저 받았다가 닫히고, 그 자리를 뮤텍스가 다시 받았다" 로 읽으면 `BaseGetNamedObjectDirectory` 순서 (토큰 열기 → 폴더 열기 → 토큰 닫기 → 객체 만들기) 와 맞는다 `[추측]`.
- **디버거 영향**: run44 는 커널 디버거가 붙은 실행인데도 `DBWinMutex` 핸들이 생겼다 → `RaiseException(0x40010006)` 을 디버거가 받아 삼키지 않고 `__except` 몸통 (`CreateDBWinMutex`) 까지 갔다 `[관찰 간접]`. 유저 디버거 (cdb) 가 붙으면 디버거가 그 예외를 받아 몸통을 안 탈 수 있다 (결과서 1-D) — 그때는 이 셋이 안 생긴다 `[추측]`.

## 결과서와 같음 / 다르게 적은 곳 · 보탠 것

- 같음: 질문 1 · 2 의 결론, 칸 표, 최소 칸 목록, syscall 표 — flow-writer 가 본 `.asm` 줄이 결과서와 같다.
- (보탬) 결과서 "동적 2 탄 갈래 (세션 번호)" 의 세션 번호는 run44 토큰 구획에 이미 있다 (`TS Session ID 0x1`). AppContainer·개인 이름공간·BNO 격리 값은 사진에 없다.
- (보탬) 결과서 "동적 3 `NtOpenDirectoryObject` 결과·`NtCreateMutant` 상태" 는 핸들 목록으로 간접 답이 있다: 폴더 핸들 `0x80` (접근 `0xf`) 가 있고, `DBWinMutex` 는 이미 있던 객체라 상태 `0x40000000` 쪽 `[추측]`.
- (보탬) **진짜 프로세스는 `main` 보다 훨씬 앞에서 이 길을 탄다** — 결과서는 `main` 기준으로만 적었다 (위 "진짜 프로세스에선 언제").
- (보탬) 캐시를 쓰는 다른 줄 셋 · `pfnAdjust…` 읽는 곳 · `BaseStaticServerData` 읽는 줄 40 은 flow-writer 가 `_refs` 에서 셌다.
- (다르게) 요청서 sast/66 "이미 아는 것" 의 "`BaseNlsDllInitialize` 가 `+0x178` 을 읽음" → **주소만 만듦** (아래 "정정").

## 정정

- **run68** 의 `gpServerNlsUserInfo ← BaseStaticServerData + 0x178` 은 **`+0x178` 칸을 읽은 값이 아니라 그 칸의 주소**다 (`BaseNlsDllInitialize +0x9 LEA RAX,[R8+0x178]` → `+0x10 MOV [gpServerNlsUserInfo],RAX`). `BaseNlsDllInitialize` 와 `NlsProcessInitialize` 는 그 영역 안을 읽지 않는다. 서버도 초기화 때 그 영역을 안 채운다 (0).
  - 단서: `NlsProcessInitialize +0x5f / +0x64 MakeLocHashNode(0x7f)` 아래 (run68 `[공백]`) 가 `GetCurrentNlsCache` 까지 가면 그 함수는 `gpServerNlsUserInfo` 로 읽는다 (`_refs` `+0x76` 등). 그 길을 타는지는 run68 공백 그대로다.
  - run68 문서 맨 위에 정정 한 줄을 넣었다. flow.md 2.5 트리·그림 2 `bn1` 노드, 짐 지도 7.6.8 NLS 줄도 고쳤다. 요청서·결과서는 안 고친다.

## 아직 확인 필요

동적 몫은 모두 **Q104 VM 단계**에 얹는다. kernelbase 멈춤점은 test.exe 일 때만 걸리게 조건을 붙인다.

1. **블록 실제 값** — `KERNELBASE!_KernelBaseBaseDllInitialize+0x332` (rva `0x4e9f2`, `+0x32b` 저장 직후): `r r10` (블록 지역 주소) · `db r10 L0xa00` · `dq @$peb+0x88 L1` · `dq @$peb+0x98 L1` · `dq @$peb+0x380 L1` · 표 `dq poi(@$peb+0x98) L6`. 특히 `+0x0 ~ +0x40` · `+0x9c8` · `+0x9cc` · `+0x9d0` · `+0x9d8` · `+0x9e8` · `+0x9f0`.
2. **폴더를 처음 연 곳** (P1a ~ P1b): `bp ntdll!NtOpenDirectoryObject` (test.exe 조건) + `k` — `BaseGetNamedObjectDirectory` 인지, `BaseFormatObjectAttributes`·WIL 쪽인지. 같은 자리에서 `du poi(poi(@r8+0x10)+8)` (이름) 과 돌아온 뒤 `dq kernelbase+0x3af0e0 L1` (캐시 = `0x80` ?).
3. **탄 갈래** — `KERNELBASE!BasepGetNamedObjectDirectoryForToken+0x2b0` (rva `0x7ac00`): `dd @rsp+0x60 L1` (세션) · `dd @rsp+0x44 L1` (개인 이름공간) · `dd @rbp-0x58 L1` (AppContainer) · `db poi(KERNELBASE+0x3aedb8)+0x20 L0x10`. `+0x386` (rva `0x7acd6`) `r eax` · `dq @rsp+0x68 L1`. `+0x3bf` (rva `0x7ad0f`) `r eax` · `db @rbp+0x260 L0x10` (`+0x268` = 격리). 위 2 가 이 함수를 거치지 않으면 여기는 안 선다.
4. **첫 `OutputDebugStringA`** — `KERNELBASE!OutputDebugStringA+0xc2` (rva `0xc3eb2`) `r rax` (뮤텍스) + `k` (dll2 TLS 인지 DllMain 인지), `CreateDBWinMutex+0x3cf` `r eax` (`0x40000000` 기대). `+0x11e` `r rax` (`OpenFileMappingW` 0 기대) · `+0x1a4` 에서 `r rbx, rsi`.
5. `TEB+0x179c IsImpersonating` = 0 인지 (`BaseGetNamedObjectDirectory+0x63`).

- 다음 바퀴 후보 (정적):
  - P1a ~ P1b 에 폴더를 연 길 — WIL 이름 객체 (`SM0:<PID>:304:WilStaging_02` 뮤텍스 · 세마포어 둘) 를 만드는 kernelbase 함수와 그 안에서 캐시를 쓰는 길 (`BaseFormatObjectAttributes +0x13d` 후보). kernelbase `entry +0xba WilInitialize` 근처가 후보 `[추측: 이름]`.
  - `BaseFormatObjectAttributes` · `OpenMutexW` · `OpenEventW` 가 캐시가 비었을 때 폴더를 어떻게 여는지 (`BaseGetNamedObjectDirectory` 를 부르나, 블록 `+0x20` 을 읽나).
  - csrss `ServerDllInitialization` 의 `NtCreateDirectoryObject` 4곳이 각각 어느 폴더 (`\Sessions\N\BaseNamedObjects` · `AppContainerNamedObjects` 등).
  - `BaseUsePrivateNamespaces` (rva `0x3a9f30`) 를 쓰는 줄.
  - kernel32 `OutputDebugStringA` export 가 forwarder 인지 (run30 Q60 `LoadLibraryExW` 와 같은 꼴).
  - run68 공백 ① `MakeLocHashNode(0x7f)` 아래 — `gpServerNlsUserInfo` 를 읽는 `GetCurrentNlsCache` 까지 가는지 (위 "정정" 단서).

## 도구 메모

- (결과서, 하네스는 안 고침 — 오케스트레이터가 사용자에게 제안) **`ExportFunctions` 가 `__except` 착지 몸통을 못 읽는다.**
  - 증상: `OutputDebugStringA.asm` 맨 끝 "읽지 않은 틈 `+0x81 ~ +0x312`", `OutputDebugStringW` 는 `+0xc2 ~ +0xcf`. 그래서 `_calls.tsv` 에 `OutputDebugStringA +0xbd → CreateDBWinMutex` 줄이 **없다.**
  - 원인 (결과서 추정): 착지 주소는 점프가 아니라 `.pdata` UNWIND_INFO 의 핸들러 데이터 (C_SCOPE_TABLE `JumpTarget`) 에만 있다. 스크립트는 "점프로 닿는 곳" 만 따라간다.
  - 제안: `ExportFunctions` 가 함수의 UNWIND_INFO 를 읽어 `JumpTarget` 을 추가 진입점으로 삼는다 ("주인 없는 덩어리" 처럼). 이번에는 objdump 로 대신했다 (`OutputDebugStringA.handler.objdump.txt`, 오프셋 = 절대 주소 − `0x1800c3df0`).
  - flow.md 기준에 이미 "예외 착지점은 Ghidra 가 코드로 안 따라간다 (run27, run28)" 가 있다 — 같은 문제의 export 쪽 증상이다.
- windbg-kd-runbook pitfalls 에 추가 제안: "`OutputDebugString` 은 `RaiseException(0x40010006)` 으로 시작한다. **유저 디버거 (cdb) 가 붙으면** 디버거가 예외를 받아 `__except` 몸통 (`DBWinMutex`·DBWIN) 을 안 탈 수 있다 — 이름 있는 객체 짐을 볼 때는 커널 디버거만 붙인 실행으로 볼 것 (run44 는 커널 디버거만 붙인 실행에서 `DBWinMutex` 핸들이 생겼다)."

# 66 named-obj-dir — 정적 분석 결과

- 요청서: `_workspace/sast/66_named-obj-dir.md` (Q136, PE_emulator 요청 22, 급함 높음)
- 모듈 / 바이너리: kernelbase · kernel32 · basesrv · csrsrv · ntdll (모두 `ms/bin/.../System32/`, 이미 analyze 됨 — 다시 analyze 안 함). kernelbase 는 `KernelBase.dll`.
- 새로 뽑은 함수 (export, `ms/decomp/`·`ms/disassemble/`):
  - kernelbase 3개+callee 8개 = 11개 (`CreateDBWinMutex`·`BaseGetNamedObjectDirectory`·`BasepGetNamedObjectDirectoryForToken` depth 1) — `GlobalAlloc`·`GlobalFree`·`memset`·`RtlStringCchPrintfW`·`BaseSetLastNTError`·`__security_check_cookie`·`FUN_1801a2102` (조각) 포함
  - kernelbase `OutputDebugStringA`·`OutputDebugStringW` (depth 0), `BasepAdjustObjectAttributesForPrivateNamespace`, `NlsProcessInitialize`·`NlsInitIsInteractiveUserProcess`·`NlsIsInteractiveUserProcess`·`NlsGetCacheUpdateCount`, 시간대·DOS 장치 읽는 쪽 18개 (`GetLocalTime` 외, depth 0)
  - kernel32 읽는 쪽 14개 (`BaseDllReadWriteIniFileOnDisk` 외, depth 0), basesrv 쓰는 쪽 12개 (`NlsUpdateCacheInfo`·`BaseSrvSetTermsrvClientTimeZone` 외, depth 0), csrsrv `CsrSrvCreateSharedSection`·`CsrServerDllInitialization`·`CsrApiHandleConnectionRequest` (depth 0)
  - refs: kernelbase `0x3aedb8`(BaseStaticServerData)·`0x3ad958`(0x18B)·`0x3af0e0`·`gpServerNlsUserInfo`, kernel32 `0xc2410`, basesrv `BaseSrvpStaticServerData`, csrsrv `CsrSrvSharedStaticServerData`·`CsrSrvSharedSectionBase` → 각 모듈 `_refs/`
  - 보조 파일 하나: `ms/disassemble/kernelbase/OutputDebugStringA.handler.objdump.txt` (아래 "스크립트 문제" 참고 — Ghidra export 가 못 읽은 except 착지 몸통을 objdump 로 직접 뽑아 오프셋을 붙인 것)
- 버전 확인: PE_emulator 가 잰 사슬이 이 바이너리 asm 과 전부 맞는다.
  - `OutputDebugStringA` call `+0xbd` / ret `+0xc2` → `CreateDBWinMutex` · `CreateDBWinMutex` ret `+0x371` → `BaseGetNamedObjectDirectory` · ret `+0xb0` → `BasepGetNamedObjectDirectoryForToken` · `+0x2b0 MOV RAX,[0x1803aedb8]` · `+0x2b7 MOVUPS XMM0,[RAX+0x20]` (kernelbase rva `0x3aedb8` 맞음). **일치.**
- 표시: `[정적]` 코드에 있는 것 · `[추측]` 이름 붙이기·토큰 값 가정 · `[동적 필요]`. 오프셋은 `.asm` 줄과 다시 대조했다 (아래 "대조" 참고).

## 스크립트 문제 (고치지 않음, 제안만)

- **증상**: `OutputDebugStringA` (그리고 `OutputDebugStringW`) 의 `__except` 착지 몸통을 export 가 못 읽는다.
  - `.asm` 맨 끝 "읽지 않은 틈 `+0x81~+0x312`" (A), "`+0xc2~+0xcf`" (W) 로만 나오고 `_calls.tsv` 에 `OutputDebugStringA +0xbd → CreateDBWinMutex` 줄이 **없다.** (요청서의 사슬 `OutputDebugStringA (ret +0xc2) → CreateDBWinMutex` 가 `_calls.tsv` 에서는 안 보이는 까닭.)
  - W 의 착지 몸통 `+0xc2` 에는 `OutputDebugStringA` 부르기 (call `+0xc7` / ret `+0xcc`) 가 있다 (objdump 로 확인).
- **원인 추정**: 착지 주소는 점프가 아니라 `.pdata` unwind 정보 안 C_SCOPE_TABLE 의 `JumpTarget` 에만 있다. 스크립트는 "점프로 닿는 곳" 만 따라간다.
- **고칠 방법 제안**: `ExportFunctions` 가 함수의 UNWIND_INFO(핸들러 데이터)를 읽어 `JumpTarget` 을 추가 진입점으로 삼아 "주인 없는 덩어리" 처럼 읽는다. 이번에는 objdump 로 대신했다.
- 그 밖의 스크립트 문제는 없었다.

## 한눈에 (핵심만)

- **PE_emulator 가 멈춘 까닭**: `BasepGetNamedObjectDirectoryForToken +0x2b0` 이 `BaseStaticServerData` (전역) 를 읽고 `+0x2b7` 이 그 블록의 `+0x20` 에서 16바이트 (`UNICODE_STRING`) 를 읽는다. 그 전역이 0 이라 멈춘다. `[정적]`
- **`+0x20` 칸** = "이름 있는 객체 폴더 이름" `UNICODE_STRING` (csrss 가 `\Sessions\N\BaseNamedObjects` 를 써 줌, 세션 0 이면 `\BaseNamedObjects`). `Buffer`(+0x28) 는 csrss 쪽 주소라서 kernelbase 가 읽으며 `블록 지역 주소 − [블록+0x9e8]` 을 더해 옮긴다. `[블록+0x9e8]` = **블록의 csrss 쪽 자기 주소** (basesrv 가 `+0x39c` 에서 자기 주소를 써 둠). `[정적]`
- **세션 0 갈래도 test.exe(세션 N) 갈래도 `+0x20` 을 읽는다.** 두 갈래가 다른 것은 "지역 버퍼에 만드는 이름" 뿐인데 그 이름은 인자 `EDX≠0` 일 때만 쓰이고, `BaseGetNamedObjectDirectory` 는 `EDX=0` 으로 부른다. `[정적]`
- **블록은 크기 `0xa00`B**, csrss `basesrv!ServerDllInitialization` 가 공유 섹션 힙에 만든다. PDB 에는 이 타입이 없다 (4개 DLL PDB 모두 구조체 타입 0개 — 공개 PDB). 칸 표는 쓰는 줄·읽는 줄로 만들었다.
- **PE_emulator 가 `OutputDebugStringA` 를 지나려면** 아래 "최소 칸 목록" 1단계(PEB 셋 + 표 + 블록 + `+0x20`·`+0x9e8`) 면 된다. 그 뒤 syscall(토큰·`NtOpenDirectoryObject`·`NtCreateMutant`) 이 **실패 상태로 답해도** 코드가 `DbgPrint` 길로 끝낸다.
- **`+0x9c8` 은 반드시 `0xffffffff`.** 0 으로 두면 "시간대 리다이렉트 켜짐" 으로 읽힌다 (`IsTimeZoneRedirectionEnabled` = `[+0x9c8] != -1`).
- 정정: run68 이 "`BaseNlsDllInitialize` 가 `+0x178` 을 **읽음**" 이라 적었지만 코드는 **포인터만 만든다** (`LEA RAX,[R8+0x178]` 후 `gpServerNlsUserInfo` 에 저장). 초기화 길에서 그 영역 안을 읽지 않는다 (`NlsProcessInitialize` 도 안 읽음).

---

## 질문별 답 [정적]

### 1. kernelbase 셋 (+ `OutputDebugString*`)

#### 1-A. `BaseGetNamedObjectDirectory` (rva `0x7a790`, 440B)

- 인자: `RCX` = 폴더 핸들을 돌려줄 칸 (`HANDLE*`). `NULL` 이면 `+0x2f` 에서 `0xc000000d` 반환 (`+0x14 JZ 0x18007a7bf`).
- 쓰는 전역: **`BaseNamedObjectDirectory`** (kernelbase rva `0x3af0e0`, 폴더 핸들 캐시).
  - 읽기 `+0x16`: 이미 있으면 `+0x22` 에서 `*RCX ← 캐시`, `+0x25` 에서 0 반환. (첫 호출 뒤에는 syscall 이 하나도 없다.)
  - 쓰기 `+0xbd` `LOCK CMPXCHG [0x1803af0e0],RCX` : 먼저 넣은 쪽이 이긴다. 졌으면 `+0x10a/+0x111` 의 `NtClose` 가 내 핸들을 닫는다. 이긴 쪽은 `+0xc8` 에서 지역 칸을 0 으로 해 닫지 않는다.
- 갈래:
  - `+0x63 CMP [TEB+0x179c],EBP` (`_TEB+0x179c IsImpersonating`, PDB 칸 이름) ≠ 0 이면 `+0x69 JNZ 0x18007a8b2` (= `+0x122`) → **흉내 내는 스레드 갈래**: `NtOpenThreadToken(-2, 4, 1, &t)` (`+0x136/+0x13d`) → `NtSetInformationThread(-2, 5, &NULL, 8)` 로 흉내 풀기 (`+0x15e/+0x165`) → `+0x179 JMP 0x18007a7ff` (= `+0x6f`) 로 돌아가 아래 보통 길 → 끝에 `NtSetInformationThread(-2, 5, &t, 8)` 로 되돌림 (`+0x198/+0x19f`) + `NtClose(t)` (`+0x1a9/+0x1b0`).
  - **보통 길 (test.exe 는 이쪽 `[추측]`: 흉내 안 냄)**: `NtOpenProcessToken(-1, 8, &h)` (`+0x80/+0x87`; `+0x74 MOV EDX,0x8`) → `BasepGetNamedObjectDirectoryForToken(h, 0, 1, 0xf, &dir)` (`+0xab/+0xb0`; `+0x9e` 출력 칸을 `[RSP+0x20]` 에, `+0xa3 R9D=0xf`, `+0xa7 R8D=1`, `EDX=0`) → 성공이면 캐시에 넣고 → `NtClose(h)` (`+0xf4/+0xfb`).
- 반환: `NTSTATUS` (EDI). 실패면 `JS 0x18007a867` 로 정리하고 그 상태를 올린다.

#### 1-B. `BasepGetNamedObjectDirectoryForToken` (rva `0x7a950`, 2677B)

- 인자 (호출한 쪽 `BaseGetNamedObjectDirectory +0x9e ~ +0xab` 으로 확인): `RCX`=토큰 핸들 · `EDX`(=`EDI`)=**0** · `R8D`(=`ESI`)=**1** · `R9D`=`0xf` (안 쓰임: 이 함수 어디에도 안 읽는다) · `[RSP+0x20]`(=함수 안 `[RBP-0x20]`)=출력 폴더 핸들 칸.
  - `EDX≠0` 이면 지역 버퍼에 만든 이름을 쓴다 (`+0x2aa JNZ 0x18007afc5` → `RtlInitUnicodeString(…, 지역 버퍼)`). `EDX=0` 이면 **`BaseStaticServerData` 의 문자열**을 쓴다. 이름은 코드 쓰임으로 붙인 것 `[추측]`.
  - `R8D≠0` 이면 끝에 BNO 격리 검사 (`+0x395 TEST ESI,ESI`, `+0x397 JZ 0x18007ad27`).
- **토큰 syscall 셋** (모두 `NtQueryInformationToken`, 출력 4B): 클래스 이름은 PDB 에 열거 타입이 없어서(ntoskrnl PDB 타입 덤프에도 없음) 공개 번호표를 빌렸다 `[추측]`. ntoskrnl PDB `_TOKEN` 에 `SessionId`(+0x78)·`BnoIsolationHandlesEntry`(+0x480)·`LowboxNumberEntry`(+0x438) 가 있어 12·44·31 쪽은 맞는 결이다.

| 호출 | 클래스 | 이름 `[추측]` | 결과 칸 | 뜻 |
|---|---|---|---|---|
| `+0xef/+0xf6` (`+0xd7 EDX=0xc`) | `0xc` | TokenSessionId | `[RSP+0x60]` | 세션 번호 |
| `+0x11d/+0x124` (`+0x116 EDX=0x1d`) | `0x1d` | TokenIsAppContainer | `[RBP-0x58]` | AppContainer 토큰? |
| `+0x14c/+0x153` (`+0x145 EDX=0x2a`) | `0x2a` | TokenPrivateNameSpace | `[RSP+0x44]` | 개인 이름공간 토큰? |
| `+0x3b8/+0x3bf` (`+0x3b0 EDX=0x2c`) | `0x2c` | TokenBnoIsolation | `[RBP+0x260]` (0x120B, `+0x268` 바이트 = 켜짐) | BNO 격리 (`R8D≠0` 일 때만) |
| `+0x4a2/+0x4a9` (`+0x48b R14D=0x1f`) | `0x1f` | TokenAppContainerSid | 힙 | AppContainer 갈래에서만 |
| `+0x127603/+0x12760a` (조각, `EDX=1`) | `1` | TokenUser | `[RBP]` | 개인 이름공간 갈래에서만 |

- **갈래 (코드 순서, 점프 목적지는 "목적지 − 함수 시작")**:
  1. 세 질문 뒤 `+0x162 MOV EAX,[RSP+0x44]` / `+0x168 JNZ 0x1801a1f38` (= `+0x1275e8`): **개인 이름공간 토큰** → 조각 (TokenUser → `RtlConvertSidToUnicodeString`) 을 거쳐 `+0x16e` 로 돌아옴 (조각 끝 `+0x12763e JMP 0x18007aabe`).
  2. `+0x16e CMP [RBP-0x58],R15D` / `+0x172 JNZ 0x18007ad98` (= `+0x448`): **AppContainer 갈래** (아래 3 번 줄).
  3. 보통: `+0x180` `RtlGetCurrentServiceSessionId` (ret `+0x187`) 와 토큰 세션 번호 (`+0x190 CMP ECX,EAX`) 를 비교.
     - **같다 (세션 0 갈래)**: `+0x198 LEA R8,[0x1802a4020]` (= `L"\BaseNamedObjects"`) 을 지역 버퍼에 복사 (`+0x1ee TEST EBX,EBX` / `+0x1f0 JNS 0x18007abed` = `+0x29d`).
     - **다르다 (세션 N 갈래)**: `+0x192 JNZ 0x18007ad44` (= `+0x3f4`) → `RtlStringCchPrintfW(버퍼, 0x100, L"%ws\%ld\BaseNamedObjects", L"\Sessions", 세션번호)` (`+0x3f8 R9=0x1802a3b68`, `+0x408 R8=0x1802a4048`, call `+0x40f` / ret `+0x414`) → `+0x416 JMP 0x18007ab3e` (= `+0x1ee`).
  4. `+0x29d` 에서 합류: `CMP [RSP+0x44]` ≠ 0 이면 `+0x2a2 JNZ 0x18007b056` (= `+0x706`, 개인 이름공간 + `EDX=0` 이면 `+0x83b` 이 `BaseStaticServerData+0x9f0` 을 읽음). 아니면 `+0x2a8 TEST EDI,EDI` / `+0x2aa JNZ 0x18007afc5`.
  5. **`EDX=0` 이면 `+0x2b0` ~ 이름 읽기** (아래 표).
- **PE_emulator 가 지금 탄 갈래** = 토큰 syscall 이 전부 0 → 세션 0 == 서비스 세션 0 → `+0x198` 갈래 → `+0x29d` → **`+0x2b0`**. 맞다 `[정적]`.
- **test.exe(세션 N) 갈래** `[추측: 일반 사용자 토큰 = AppContainer 0, 개인 이름공간 0]`: 세션 N ≠ 0 → `+0x3f4` 갈래 → `+0x29d` → **같은 `+0x2b0`**. 즉 "지역 버퍼에 무슨 이름이 들어가나" 만 다르고, **열리는 폴더 이름은 두 갈래 모두 `BaseStaticServerData+0x20` 의 문자열**이다.

##### `+0x2b0` 이후 (보통 갈래, 읽는 칸)

| 오프셋 | 줄 | 읽는 칸 / 하는 일 |
|---|---|---|
| `+0x2b0` | `MOV RAX,[0x1803aedb8]` | `BaseStaticServerData` (전역) |
| `+0x2b7` | `MOVUPS XMM0,[RAX+0x20]` | 블록 `+0x20` 16B = `UNICODE_STRING{Length +0x20, MaximumLength +0x22, Buffer +0x28}` 을 `[RSP+0x50]` 에 복사 |
| `+0x2ca/+0x2cd` | `TEST RAX ; JZ 0x18007b15e` | `Buffer`(+0x28) == 0 이면 `+0x80e` 에서 `[RSP+0x58]=0` 하고 `+0x348` 로 (옮김 건너뜀. 그 채로 이름 `Buffer=NULL` 로 syscall 로 간다) |
| `+0x2d3 ~ +0x343` | `[PEB+0x98]`·`[그+8]`·`[PEB+0x380]`·`[PEB+0x88]`·`[블록+0x9e8]` | `Buffer += (S − [블록지역+0x9e8]) − C + R` 로 옮김. S=`[[PEB+0x98]+8]`(csrss 쪽 블록 주소), C=`PEB+0x380`, R=`PEB+0x88`, 블록 지역 주소 = `S − C + R`. 곧 `Buffer += 블록지역주소 − [블록+0x9e8]` (`+0x324 SUB R9,[R8+RDX+0x9e8]`, `+0x343 ADD [RSP+0x58],R9`) |
| `+0x348 ~ +0x37f` | `R14D=0x30 ; [RBP-0x80]=0 ; R15D=0x40 ; EDX=0xf` | `OBJECT_ATTRIBUTES{Length 0x30, RootDirectory 0, ObjectName=&[RSP+0x50], Attributes 0x40 (대소문자 무시)}` 만들고 **`NtOpenDirectoryObject(&[RSP+0x68], 0xf, &OA)`** (call `+0x37f` / ret `+0x386`). 실패면 `+0x38f JS 0x18007ad6b` (= `+0x41b`): 같은 이름을 접근권한 2 (TRAVERSE) 로 한 번 더 열고 (`+0x42a/+0x431`), 되면 `+0x6a9` 갈래: 그 폴더 아래 하위 폴더 **`Restricted`** (상수 `UNICODE_STRING` kernelbase rva `0x1ab0c8` → `L"Restricted"` `0x2a40e0`) 를 `0xf` 로 열고 (`+0x6dd/+0x6e4`) 부모 핸들 `NtClose` (`+0x6f0/+0x6f7`) — 제한된 토큰 쪽 길 `[추측]`. 성공하면 `+0x395` 로 합류 |
| `+0x395 ~ +0x3d1` | `ESI≠0` | **`NtQueryInformationToken(h, 0x2c, +0x260, 0x120)`** (`+0x3b8/+0x3bf`); `[RBP+0x268]` 바이트 == 0 이면 격리 없음 |
| `+0x3d7 ~ +0x3eb` | `MOV RAX,[RSP+0x68]` … `MOV [RCX],RAX` | 격리 없음 → 연 폴더 핸들을 출력 칸에 쓰고 `+0x3eb JMP 0x18007ab46` 로 정리 (**여기서 끝: test.exe 보통 길**) |
| `+0x3d1 JNZ 0x18007b0d1` (= `+0x781`) | 격리 켜짐 | `RtlInitUnicodeString(&[RSP+0x50], [RBP+0x260])` (`+0x78d/+0x794`) → 방금 연 폴더를 루트로 하위 폴더 `NtOpenDirectoryObject(0xf)` (`+0x7cb/+0x7d2`) → 원래 폴더 `NtClose` (`+0x7e9/+0x7f0`) → 하위 폴더 핸들을 결과로 |

- 정리부 `+0x1f6 ~ +0x26a`: `RtlFreeUnicodeString` (`+0x200/+0x207`), 남은 폴더 핸들 `NtClose` (`+0x214 JNZ`→`+0x875`; `+0x224/+0x22b`), `RtlFreeSid`(`+0x88d`), `RtlFreeHeap`(`+0x254/+0x25b`) 후 반환.
- **AppContainer 갈래** (`+0x448 ~`, 이름·조건만): `RtlLengthRequiredSid(0xc)` (`+0x44d`) → `RtlAllocateHeap` (`+0x46e`) → `NtQueryInformationToken(0x1f)` (`+0x4a2`) → `RtlStringCchPrintfW(…L"%ws\%ld\AppContainerNamedObjects")` (`+0x4d7`, 포맷 `0x1802a4080`) → `EDX=0` 이면 **`BaseStaticServerData+0x9d8` 16B** 읽고 같은 식으로 `Buffer` 옮김 (`+0x4ee ~ +0x584`) → `NtOpenDirectoryObject(0x2)` (`+0x5bf`) → `RtlGetAppContainerSidType` (`+0x5e8`) → SID 문자열 (`+0x613`) → `NtOpenDirectoryObject(0xf)` (`+0x65a`) 등. 부모 AppContainer 는 `RtlGetAppContainerParent` (`+0x820`, 조각 `FUN_1801a2102`). test.exe 는 안 탄다 `[추측]`.
- **개인 이름공간 갈래** (조각 `+0x1275e8`, 이름·조건만): `TokenUser` → SID 문자열 → `RtlSubAuthoritySid` ×4 → `RtlStringCchPrintfW(…L"%s\%u-%u-%u-%u")` (조각 `+0xa4`, 포맷 `0x1802e4600`) → `NtOpenDirectoryObject(0xf)` (조각 `+0xfb`). 이어 `EDX=0` 이면 **`BaseStaticServerData+0x9f0` 16B** 읽고 옮김 (`+0x83b ~ +0x861`, 조각 `+0x127643 ~ +0x1276b8`). test.exe 는 안 탄다 `[추측]`.

##### syscall 표 — `BaseGetNamedObjectDirectory` 한 번 (캐시 없을 때)

| 순서 | syscall | 인자 요약 | 조건 | 위치 (call/ret) |
|---|---|---|---|---|
| 1 | `NtOpenProcessToken` | (-1, 8 = QUERY, &h) | 보통 길 | BGNOD `+0x80/+0x87` |
| 2 | `NtQueryInformationToken` | (h, 0xc, 4B) | | BGNODFT `+0xef/+0xf6` |
| 3 | `NtQueryInformationToken` | (h, 0x1d, 4B) | | `+0x11d/+0x124` |
| 4 | `NtQueryInformationToken` | (h, 0x2a, 4B) | | `+0x14c/+0x153` |
| 5 | `NtOpenDirectoryObject` | (&dir, 0xf, OA{이름=블록+0x20, 0x40}) | 보통 갈래 (**test.exe·세션 0 갈래 공통**) | `+0x37f/+0x386` |
| 6 | `NtQueryInformationToken` | (h, 0x2c, 0x120B) | `R8D=1` (이 호출자는 항상 1) | `+0x3b8/+0x3bf` |
| 7 | `NtClose` | (h) | | BGNOD `+0xf4/+0xfb` |

- BGNOD = `BaseGetNamedObjectDirectory`, BGNODFT = `BasepGetNamedObjectDirectoryForToken`. 합 **7개** (+ 사용자 공간 `RtlGetCurrentServiceSessionId`·`RtlStringCchPrintfW`·`memset`).
- 흉내 내는 스레드 (`IsImpersonating≠0`) 면 `NtOpenThreadToken` + `NtSetInformationThread` ×2 + `NtClose` 가 더 붙는다.
- **세션 0 갈래와 test.exe 갈래의 syscall 차이는 없다** (토큰 질문의 *대답* 이 갈래를 가를 뿐). 지역 버퍼 이름만 다르다.

#### 1-C. `CreateDBWinMutex` (rva `0x79260`, 1211B)

- 인자 없음. 반환 `RAX` = 뮤텍스 핸들 (실패 0).
- 만드는 것: 이름 **`DBWinMutex`** (`+0x342 LEA RDX,[0x1802a4008]` 유니코드 문자열 확인). 아래 순서.
  1. SID 4개 `RtlAllocateAndInitializeSid` (`+0x9e/+0xa5`, `+0xe4/+0xeb`, `+0x126/+0x12d`, `+0x16b/+0x172`): S-1-5-18 (SYSTEM), S-1-5-32-544 (관리자), S-1-1-0 (모두), S-1-16-4096 (낮은 무결성).
  2. `RtlCreateSecurityDescriptor` (`+0x186/+0x18d`) · `GlobalAlloc` ×2 (`+0x1e6`, `+0x2be`) · `RtlCreateAcl` ×2.
  3. **DACL** (`RtlAddAccessAllowedAce` ×3 `+0x226`·`+0x24b`·`+0x270`): 모두 `0x120001` (읽기), SYSTEM `0x1f0001`, 관리자 `0x1f0001` (전체). **SACL** `RtlAddMandatoryAce` (`+0x308/+0x30f`: 개정 2, 플래그 0, SID=낮은 무결성, 종류 `0x11`, 정책 1 = 쓰기 금지).
  4. 이름 `RtlInitUnicodeString` (`+0x358/+0x35f`) → **`BaseGetNamedObjectDirectory(&dir)`** (call `+0x36c` / ret `+0x371`) → 실패면 `+0x373 JS 0x180079710` (= `+0x4b0`) → `BaseSetLastNTError` (`+0x4b2/+0x4b7`), 반환 0.
  5. `OBJECT_ATTRIBUTES` (`+0x398 Length 0x30`, `+0x39f Attributes 0x80` = 있으면 열기, `Root=dir`, `Name`, `SD`). `+0x391` 이 전역 `pfnAdjustObjectAttributesForPrivateNamespace` (kernelbase rva `0x3ad958`) 를 읽고 **0 이 아니면** CFG 간접 호출 (`+0x3b3/+0x3b8`).
     - 그 값은 DllMain `entry +0xa7/+0xb3` 이 `0x1800b92d0` (= `BasepAdjustObjectAttributesForPrivateNamespace`) 로 써 둔다 (`reason == 1`, `+0xa3 CMP AL,1`).
     - 그 함수 `+0x6 CMP byte [0x1803a9f30],0` (`BaseUsePrivateNamespaces`) 가 0 이면 `+0x10 JZ` 로 바로 돌아온다. 기본값은 코드에서 안 쓰여 0 `[추측: 개인 이름공간 안 만들면 0]` → test.exe 는 실제로 아무 일도 안 한다.
  6. **`NtCreateMutant(&h, 0x2120001, &OA, FALSE)`** (call `+0x3c8` / ret `+0x3cf`): 접근 = MAXIMUM_ALLOWED|SYNCHRONIZE|READ_CONTROL|QUERY_STATE.
  7. 실패(`+0x3d6 JS`)면 `+0x4b0` 로. `+0x3dc CMP EAX,0x40000000` (= 이미 있음) 이면 `+0x4a6 MOV ECX,0xb7` → `RtlSetLastWin32Error(0xb7)`, 아니면 `RtlSetLastWin32Error(0)` (`+0x3e9/+0x3f0`).
  8. 정리 `RtlFreeSid` ×4, `GlobalFree` ×2 (`+0x456/+0x45b`, `+0x49f/+0x4a4`), 쿠키 검사 (`+0x465/+0x46a`) 후 반환.
- syscall 합: `NtCreateMutant` 1개 + 1-A/1-B 의 7개. (`GlobalAlloc` 은 힙, 커널 호출 없음.)

#### 1-D. `OutputDebugStringA` / `W` 와 그 뒤 (이름만)

- `OutputDebugStringA` (rva `0xc3df0`): `strlen`(`+0x45/+0x4c`) 뒤 **`RaiseException(0x40010006 = DBG_PRINTEXCEPTION_C, 0, 2, 인자)`** (`+0x6e MOV ECX,0x40010006`, call `+0x77` / ret `+0x7c`). 본문이 `__try { RaiseException } __except(code == 0x40010006) { … }` 다 (`$filt$0` 이 `CMP ECX,0x40010006` — `.asm` 의 예외 정리 함수). **디버거가 이 예외를 받아 처리하면 `except` 는 안 탄다.** 아무도 못 받으면 아래 착지 몸통 (`+0x81 ~`) 을 탄다. 이 몸통은 Ghidra export 가 못 읽어서 `OutputDebugStringA.handler.objdump.txt` 로 읽었다.
- **착지 몸통 (OutputDebugStringA `+0x81 ~ +0x312`)** — 디버거 없음, DBWIN 듣는 프로그램(DebugView 등) 없음 `[동적 필요]` 가정:

| 오프셋 | 하는 일 |
|---|---|
| `+0x9e` | `r13d = TEB+0x68` (LastError 저장) |
| `+0xac ~ +0xb5` | 전역 `[0x1803ad968]` (DBWin 뮤텍스 핸들) == 0 이고 `[0x1803ad960]` (만들기 실패 깃발) == 0 이면 아래로 |
| **`+0xbd` / ret `+0xc2`** | **`CreateDBWinMutex()`** (요청서 사슬과 일치) |
| `+0xc8 ~ +0xca` | 반환 0 이면 실패 깃발 `[0x1803ad960] = 1` (다시는 안 만든다) |
| `+0xd5` | 아니면 `LOCK CMPXCHG [0x1803ad968],RCX` 로 전역에 넣고, 졌으면 `CloseHandle` (`+0xe0`) |
| `+0xe5 ~ +0xef` | 전역 핸들이 0 이면 `+0x1a4` 로 (아래 DbgPrint 길) |
| `+0xfd` / ret `+0x102` | **`WaitForSingleObjectEx(뮤텍스, 10000, 0)`**; `+0x102 TEST EAX,0xffffff7f` / `+0x107 JNE +0x1a4` : 0 이나 0x80 이 아니면 DbgPrint 길 |
| `+0x119` / ret `+0x11e` | `OpenFileMappingW(2, 0, L"DBWIN_BUFFER")` (rdi). 0 이면 `+0x129 JE` → `+0x192` |
| `+0x147` | `MapViewOfFileExNuma(rdi, 6, 0, 0, 0, 0, -1)` (r14). 0 이면 `+0x192` |
| `+0x167` / `+0x185` | `OpenEventA(SYNCHRONIZE, 0, "DBWIN_BUFFER_READY")` (rsi) / `OpenEventA(MODIFY_STATE 2, 0, "DBWIN_DATA_READY")` (rbx) |
| `+0x192 ~ +0x19e` | rbx == 0 (하나라도 못 열었다) 이면 `ReleaseMutex(전역)` (`+0x19e`) |
| `+0x1a4 ~` | 글자 수만큼 반복. rbx != 0 이고 `WaitForSingleObjectEx(rsi, 10000)` (`+0x1e1`) == 0 이면 **DBWIN 길**: `[r14] = TEB.ClientId.UniqueProcess` (`+0x1ea ~ +0x1f6`), 최대 `0xffb` 글자 `memcpy` (`+0x21a`), `SetEvent(rbx)` (`+0x22b`). 아니면 **`DbgPrint("%s", 최대 0x1ff 글자)`** 길 (`+0x232 ~ +0x274`, `+0x274` 가 `DbgPrint` call) |
| `+0x299 → +0x2bb` | 정리: `UnmapViewOfFile2(-1, r14, 0)` (`+0x2ca`), rbx 있으면 `ReleaseMutex(전역)` (`+0x2db`) + `CloseHandle(rbx)`, `CloseHandle(rsi)`, `CloseHandle(rdi)`, `RtlSetLastWin32Error(저장값)` (`+0x306`) |
| `+0x29b` | 안쪽 `__try` 가 터지면 (`$filt$1` : 스택 넘침 `0xc00000fd` 만 아님) `DbgPrint("OutputDebugString faulted during output")` |

- **VM 에서 DebugView 없이 test.exe 를 돌리면** (정적 추론 `[동적 필요]`) 순서: `RaiseException` → (except) → `CreateDBWinMutex`(최초 1번) → `WaitForSingleObjectEx`(뮤텍스, 바로 획득) → `OpenFileMappingW` **실패** → `ReleaseMutex` → `DbgPrint("%s")` → ntdll `DbgPrint` → `vDbgPrintExWithPrefixInternalHelper` (이름만, 범위 밖). 두 번째 `OutputDebugStringA` 부터는 뮤텍스를 다시 안 만들고 `+0xe5` 에서 전역을 바로 쓴다.
- `OutputDebugStringW` (rva `0xc3cd0`): ANSI 로 바꾼 뒤 **`RaiseException(0x4001000a)`** (`+0xb2`, call `+0xbb` / ret `+0xc0`). except 착지 `+0xc2` 에서 `OutputDebugStringA(ANSI 버퍼)` 를 부른다 (`+0xc7/+0xcc`, objdump 확인). 즉 W 는 A 를 거쳐 같은 길로 간다.

### 2. `BaseStaticServerData` 블록

#### 2-A. 타입 이름

- **PDB 에 없다.** kernelbase·kernel32·basesrv·csrsrv PDB 모두 `pdb_struct.py --list <바이너리> '.'` 가 0개 (공개 PDB 라 타입이 없다). ntdll PDB 는 있지만 이 이름이 없다. 칸 표는 쓰는 줄 (basesrv)·읽는 줄 (kernelbase·kernel32) 로 만들었다. 아래 이름은 코드 쓰임으로 붙인 라벨이고, 일부는 옛 공개 구조체 이름을 빌렸다 `[추측]`. 칸의 자리·크기·값은 `[정적]`.

#### 2-B. 칸 표 (크기 `0xa00` = 2560B)

basesrv `ServerDllInitialization` (`+0x365 R8D=0xa00`, `+0x372 RtlAllocateHeap(BaseSrvSharedHeap)`) 가 만든다. 공유 섹션 힙을 `RtlCreateHeap(0x7008, …)` (csrsrv `CsrSrvCreateSharedSection +0x267/+0x26e` 부근, 플래그에 `8` = 0 채움 비트 `[추측]`) 로 만들어서, 안 쓴 칸은 0 이다.

| 오프셋 | 크기 | 라벨 `[추측]` | 서버가 채움 (basesrv `ServerDllInitialization`) | 읽는 곳 (+ 오프셋) |
|---|---|---|---|---|
| `+0x000` | 16 | WindowsDirectory (`UNICODE_STRING`: Len +0, Max +2, **Buffer +8**) | `+0x3ed` 복사 → `+0x438` Buffer 를 힙 복사본으로 교체. 값은 `%SystemRoot%` 확장 (`+0x1e1`) | kernelbase `_KernelBaseBaseDllInitialize +0x33e` (→ `KernelBaseGlobalData+0x8`; Buffer 가 0 아니면 `+0x3d3` 에서 옮김; `+0x35d JZ 0x18004ed64` 로 NULL 이면 건너뜀). 그 사본은 `GetWindowsDirectoryW`·`GetSystemWindowsDirectoryW`·`GetTempPathW` 가 읽음 |
| `+0x010` | 16 | WindowsSystemDirectory (Buffer +0x18) | `+0x443` → `+0x48f` (`\system32` 덧붙임 `+0x246`) | kernelbase `+0x3da` (→ `KernelBaseGlobalData+0x18`; 옮김 `+0x469`). `GetSystemDirectoryW` 가 읽음 |
| **`+0x020`** | 16 | **NamedObjectDirectory** (Len +0x20, Max +0x22, **Buffer +0x28**) | `+0x4a5` 복사, `+0x4b1` Max = Len+2, `+0x4fb` Buffer 교체. 내용: 서비스 세션이면 `\BaseNamedObjects` (`+0x274 ~ +0x28e` `wcscpy_s`), 아니면 `\Sessions\N\BaseNamedObjects` (`+0x2a3 ~ +0x2b5` `swprintf_s`) | **kernelbase `BasepGetNamedObjectDirectoryForToken +0x2b7`** (+ `Buffer` 옮김 `+0x2d3 ~ +0x343`) |
| `+0x030` | 6 | (옛 Major/Minor/Build 자리 `[추측]`) | 이 빌드 코드에서 쓰는 줄을 못 찾음 | 읽는 줄도 못 찾음 |
| `+0x036` | 2 | CSDNumber (word) | `+0x600` 0 으로, 이어 레지스트리 값 `+0x666` | 못 찾음 |
| `+0x038` | 2 | RCNumber (word) | `+0x671` | **kernelbase `+0x332`** (→ `BaseRCNumber` rva `0x3aedc0`) |
| `+0x03a` | 0x100 | CSDVersion (`WCHAR[128]`) | `+0x604` 0, `+0x6c1` `wcsncpy_s(…, 0x80)` | kernel32 `_BaseDllInitialize +0x190` (`LEA RDX,[RAX+0x3a]` → `BaseCSDVersion`), `+0x275` `RtlInitUnicodeString(PEB+0x2e8, …)` (PEB 의 `CSDVersion` 이 비어 있을 때) |
| `+0x13a` | 6 | (맞춤 빈칸) | | |
| `+0x140` | 0x30 | TimeOfDay (`SYSTEM_TIMEOFDAY_INFORMATION`) | `NtQuerySystemInformation(3, 블록+0x140, 0x30)` (`+0x392`, `+0x3bc`) | 못 찾음 (이 정적 범위의 kernelbase·kernel32 에서) |
| `+0x170` | 8 | IniFileMapping (서버 쪽 포인터) | `BaseSrvInitializeIniFileMappings +0xd4` | kernel32 `_Kernel32LegacyLib_BaseDllInitialize +0x6f` (0 이면 `+0x79 JZ`, 아니면 `− [블록+0x9e8] + 블록` 으로 옮김 `+0x7b ~ +0x88` → `BaseDllIniFileMapping`). ini 함수들 (`BaseDllFindAppNameMapping +0x33` 등) 은 그 안의 서버 주소를 `[블록+0x9e8]` 로 옮기는 쪽 |
| `+0x178` | 0x67c | NlsUserInfo (`pNlsRegUserInfo` 가 가리킴) | **초기화에서는 안 채움 (0)**; `pNlsRegUserInfo ← 블록+0x178` (`+0x11b4/+0x11bb`). 채우는 곳: csrss `BaseSrvNlsLogon`·`NlsUpdateCacheInfo`, 복사해 주는 곳 `BaseSrvNlsGetUserInfo` (`0x67c` 확인) | kernelbase `BaseNlsDllInitialize +0x9/+0x10` (**포인터만**, 안 읽음). 나중에 읽는 줄 23곳(함수 13개)이 `gpServerNlsUserInfo` 로 읽음 (`CheckSpecialLocales`·`GetCurrentNlsCache`·`GetLocaleInfoHelper`·`CompareStringW` …). 예: `NlsGetCacheUpdateCount +0x7` 은 `[+0x678]` (= 블록 `+0x7f0`), `NlsIsInteractiveUserProcess +0xd5/+0xe1` 은 `[+0x62c]`·`[+0x630]` (= 블록 `+0x7a4`·`+0x7a8`) |
| `+0x7f4` | 1 | DefaultSeparateVDM | `+0x742` 0, 레지스트리에 따라 `+0x7d6`·`+0x826` | kernelbase `CreateProcessInternalW +0x73e` |
| `+0x7f5` | 1 | IsWowTaskReady | `+0x85a` 0, `BaseSrvCheckWOW +0x1e0`·`BaseSrvGetNextVDMCommand +0x28c` | kernel32 `GetNextVDMCommand +0x89` |
| `+0x7f8` | 16 | WindowsSys32x86Directory (Buffer +0x800) | `+0x49a` Len 0, `+0x493` Buffer 0 | kernel32 `_BaseDllInitialize +0x1a4` (Buffer 가 0 아니면 `+0x1bf JNZ` 옮김) |
| `+0x808` | 1 | fTermsrvAppInstallMode | `+0x5d6` 0, `BaseSrvSetTermsrvAppInstallMode +0x4f` | kernel32 `TermsrvAppInstallMode +0x9`, `BaseDllReadWriteIniFileOnDisk +0x180`, `TermsrvSyncUserIniFile +0xc5` |
| `+0x80c` | 0x1b0 | tziTermsrvClientTimeZone (`DYNAMIC_TIME_ZONE_INFORMATION` 모양: 0x80B ×3 + 0x30 복사로 확인) | `BaseSrvSetTermsrvClientTimeZone +0xb4 ~` | kernelbase `GetClientDynamicTimeZoneInformation +0x2e`, `GetClientTimeZoneInformation +0x25` |
| `+0x9bc` | 12 | ktTermsrvClientBias (`KSYSTEM_TIME`: +0x9bc, +0x9c0, +0x9c4) | `BaseSrvSetTermsrvClientTimeZone +0x12c`(8B)·`+0x137`(4B) | `GetLocalTime +0x61/+0x6b/+0x75`, `FileTimeToLocalFileTime +0x73 ~`, `LocalFileTimeToFileTime +0x31 ~` |
| **`+0x9c8`** | 4 | TermsrvClientTimeZoneId | **초기값 `0xffffffff`** (`+0x3a3`), `BaseSrvSetTermsrvClientTimeZone +0x140` | `IsTimeZoneRedirectionEnabled +0x7` (`CMP ECX,-1`) 외 시간대·`GetLocalTime`·`SetLocalTime` 등 함수 15개 |
| `+0x9cc` | 1 | LUIDDeviceMapsEnabled | `NtQueryInformationProcess(-1, 0x1c, …)` (`+0xc07`) 결과 ≠0 이면 1 (`+0xc25`) | kernelbase `QueryDosDeviceW +0x3ec`·`+0x731`, `DefineDosDeviceW +0x44`; kernel32 `DosPathToSessionPathW +0x28`·`+0xe1` |
| `+0x9d0` | 4 | TermsrvClientTimeZoneChangeNum | 초기값 0 (`+0x3ad`) | `GetClientDynamicTimeZoneInformation +0xf/+0x9d` (바뀌는 동안 다시 읽는 짝) |
| `+0x9d8` | 16 | AppContainer 이름있는 객체 경로 (Buffer +0x9e0) = `\Sessions\N\AppContainerNamedObjects` | `+0x503` → `+0x565` | `BasepGetNamedObjectDirectoryForToken +0x4f5` (AppContainer 갈래) |
| **`+0x9e8`** | 8 | **자기 주소 (csrss 쪽 블록 주소)** | `+0x39c MOV [RAX+0x9e8],RAX` | 옮김 식 전부: kernelbase `+0x3b4`(init)·`BGNODFT +0x324`·`+0x565`·조각 `+0x127694`, kernel32 `+0x7b`·ini 함수들 |
| `+0x9f0` | 16 | 세션별 BaseNamedObjects 경로 (Buffer +0x9f8) = 언제나 `\Sessions\N\BaseNamedObjects` | `+0x570` → `+0x5c8` | `BasepGetNamedObjectDirectoryForToken +0x842` (개인 이름공간 갈래) |
| `+0xa00` | | 끝 | | |

- 코드로 확인 못 한 칸: 위 표의 "못 찾음" 은 정적 범위(kernelbase·kernel32·basesrv 의 이 요청 함수들)에서 안 보였다는 뜻이다. 다른 모듈(advapi32·user32 등)이 읽는지는 이번에 안 봤다.

#### 2-C. `+0x20` 칸 값 꼴 · 옮김

- 값 꼴 `[정적]`: `UNICODE_STRING` 한 개 (16B). 세션 N → `\Sessions\N\BaseNamedObjects` (N=1 이면 28글자 = Length `0x38`, Max `0x3a`), 서비스 세션 0 이면 `\BaseNamedObjects` (17글자, Length `0x22`, Max `0x24`).
- `Buffer`(+0x28) 도 **csrss 주소**다. 예, 옮겨야 한다. kernelbase 가 읽으며 `Buffer += 블록지역주소 − [블록+0x9e8]` (위 표 `+0x2d3 ~ +0x343`).
  - 실제 프로세스에서는 `[블록+0x9e8]` = S (csrss 쪽 블록 주소) 라서 그 식은 `Buffer + (R − C)` 와 같다 (R=내 뷰 시작, C=csrss 뷰 시작). **에뮬레이터에서 `PEB+0x88` == `PEB+0x380` 으로 두고 `[블록+0x9e8]` = S 로 쓰면 옮김이 0** 이라 `Buffer` 를 지역 주소 그대로 써도 된다.

#### 2-D. 누가 언제 채우나

1. csrss 가 세션을 시작할 때 `csrsrv!CsrSrvCreateSharedSection` 이 공유 섹션 (`\Sessions\N\Windows\SharedSection`) 을 만들어 csrss 에 펼치고 그 안에 힙을 만든다 (`+0x1b8` `NtCreateSection`, `+0x22c` `NtMapViewOfSection`, `+0x267` `RtlCreateHeap`, `+0x2b4` 표 `RtlAllocateHeap(…, 0x30)` = 6칸 표 `CsrSrvSharedStaticServerData`).
2. `csrsrv!CsrLoadServerDll` (`basesrv` 를 번호 1 로 올릴 때): 서버 DLL 객체 `+0x60` 칸에 공유 힙을 먼저 넣고 (`+0x25f LEA R13,[RSI+0x60]`) **`basesrv!ServerDllInitialization` 를 간접 호출** (`+0x37b` / ret `+0x380`). 초기화가 `+0x38e MOV [RBX+0x60],RAX` 로 그 칸을 블록 주소로 바꾼다. 돌아오면 `+0x3da ~ +0x3ee` 가 `표[서버번호] = [서버객체+0x60]` (공유 힙과 다르면). basesrv 번호 = 1 (kernelbase 가 `[표+8]` 을 읽는다).
3. test.exe 가 `NtConnectPort` 할 때 csrss 의 `CsrApiHandleConnectionRequest` 가 응답 `+0x28` 에 `CsrSrvSharedSectionBase`, `+0x30` 에 `CsrSrvSharedStaticServerData` (표 주소), `+0x38` 에 csrss 프로세스 번호를 써 준다 (`+0x9c ~ +0xd0`). ntdll `CsrpConnectToServer` 가 `NtMapViewOfSection` 한 내 뷰 R 을 쓰며 `PEB+0x380 ← C`(csrss 뷰), `PEB+0x88 ← R`, `PEB+0x98 ← 표주소 − C + R` (run68 `+0x36d/+0x379/+0x38d`).
4. kernelbase `_KernelBaseBaseDllInitialize` 가 `BaseStaticServerData = [PEB+0x98]+8 의 값 − PEB+0x380 + PEB+0x88` (`+0x2fb ~ +0x32b`, 쓰기 `+0x32b`) = `S − C + R`. kernel32 도 `_BaseDllInitialize +0x189`·`_Kernel32LegacyLib_BaseDllInitialize +0x5c` 에서 자기 전역에 같은 식.
- 곧 **블록은 csrss(`basesrv`) 가 만들어 공유 섹션 힙에 두고, 클라이언트는 읽기 전용 뷰로 본다.** 값 채움은 test.exe 가 오기 전에 끝난다 (세션 시작 때). test.exe 쪽에서 블록을 쓰는 코드는 이번 범위에서 없다 (읽기 전용 뷰라서 `[추측]`).

#### 2-E. PE_emulator 가 지을 **최소 칸 목록**

단계 1 — `OutputDebugStringA` 를 크래시 없이 지나기 (필수):

| 무엇 | 값 꼴 | 정적으로 정해지나 |
|---|---|---|
| `PEB+0x88` (R), `PEB+0x380` (C) | 아무 같은 값이면 옮김 0 (권장: R == C) | 꼴 정해짐, 값은 에뮬레이터 마음 |
| `PEB+0x98` (T) | 표 (6칸 × 8B = 0x30B) 의 R 기준 주소 | 꼴 정해짐 |
| `T[1]` (= `[T+8]`) | S = 블록의 C 기준 주소 (R==C 면 블록 주소 그대로) | 꼴 정해짐 |
| 블록 `0xa00`B | `S − C + R` 위치, 읽을 수 있는 메모리 | 크기 정해짐 |
| 블록 `+0x9e8` (8B) | **S** (자기 주소) | **정해짐** |
| 블록 `+0x20` (16B) | Length `0x38`, Max `0x3a`, Buffer (C 기준 주소, ≠0) → `\Sessions\1\BaseNamedObjects` 유니코드 (끝 NUL 포함 `0x3a`B) | 꼴 정해짐, 세션 번호는 동적 (에뮬레이터 정함) |

- 이 단계 뒤 syscall 이 어떻게 답해도 통과한다: `NtOpenDirectoryObject` 실패 → `BaseGetNamedObjectDirectory` 가 상태를 올림 → `CreateDBWinMutex` 가 `BaseSetLastNTError` 하고 0 → `OutputDebugStringA` 가 실패 깃발 `[0x1803ad960]=1` 하고 `DbgPrint` 길 (`+0xe5 JE +0x1a4`). 성공하게 하려면 폴더 핸들 하나 + `NtCreateMutant` 성공이면 된다.

단계 2 — 초기화·이웃 함수가 안전하게 읽을 칸 (권장):

| 칸 | 값 | 이유 |
|---|---|---|
| `+0x9c8` | **`0xffffffff`** | 0 이면 시간대 리다이렉트 "켜짐" 으로 읽힘 |
| `+0x9d0` | 0 | |
| `+0x9cc` | 0 또는 1 (동적: VM 값) | `QueryDosDeviceW` 등이 읽음 |
| `+0x38` | 0 가능 (`BaseRCNumber`) | |
| `+0x00`·`+0x10` | Length/Max/Buffer. Buffer 0 이면 init 이 건너뛰지만 `GetWindowsDirectoryW` 류가 빈 값을 돌려줌. 필요하면 `C:\WINDOWS`·`C:\WINDOWS\system32` (동적: VM 실제 값) | |
| `+0x3a` | NUL 로 시작 (0) | kernel32 가 `wcslen` 으로 읽음 |
| `+0x7f8` | 16B 0 | kernel32 `+0x1a4` 가 읽음 |
| `+0x170` | 0 | kernel32 가 `JZ` 로 건너뜀 |
| `+0x178` ~ `+0x7f4` | 0 (초기화 길은 안 읽음) | 나중 NLS 함수가 읽음 |
| `+0x9d8`·`+0x9f0` | `\Sessions\1\AppContainerNamedObjects`·`\Sessions\1\BaseNamedObjects` | 다른 토큰 갈래용 (test.exe 는 안 탄다 `[추측]`) |
| 나머지 | 0 | |

---

## 동적분석으로 확인해야 할 것 (Q104 VM 단계에 얹을 것)

정적으로 닫힌 것: 위 모든 칸 자리·읽는 곳·갈래 조건·syscall 목록. **동적으로 닫아야 하는 것**:

1. **블록 `+0x0 ~ +0x40` 실제 값** (WindowsDirectory·SystemDirectory·`+0x20` 문자열·RCNumber), `+0x9c8`·`+0x9cc`·`+0x9d0`·`+0x9d8`·`+0x9e8`·`+0x9f0`, 표 `T[0..5]`.
2. **탄 갈래**: 세션 번호, `AppContainer` 값, 개인 이름공간 값, BNO 격리 켜짐 여부 (`[RBP+0x268]`).
3. `NtOpenDirectoryObject` 결과 (상태·핸들) 와 `NtCreateMutant` 상태 (`0` / `0x40000000`), `OpenFileMappingW` 실패 여부 (DBWIN 듣는 프로그램 있나).
4. test.exe 가 정말 `IsImpersonating == 0` 인가.

**추천 멈춤점** (모두 test.exe 만 멈추게 조건; 오프셋은 모듈 시작 + RVA):

| 멈춤 | 읽을 칸 |
|---|---|
| `KERNELBASE!_KernelBaseBaseDllInitialize+0x332` (rva `0x4e9f2`) — 블록 포인터 저장(`+0x32b`) 직후 | `r10` = 블록 지역 주소: `db r10 L0xa00`; `dq @$peb+0x88 L1`·`dq @$peb+0x98 L1`·`dq @$peb+0x380 L1`; 표 `dq poi(@$peb+0x98) L6` |
| `KERNELBASE!BasepGetNamedObjectDirectoryForToken+0x2b0` (rva `0x7ac00`) | `dd @rsp+0x60 L1` (세션 번호), `dd @rsp+0x44 L1` (개인 이름공간), `dd @rbp-0x58 L1` (AppContainer), `db poi(KERNELBASE+0x3aedb8)+0x20 L0x10` |
| 같은 함수 `+0x386` 직후 (rva `0x7acd6`) | `r eax` (상태), `dq @rsp+0x68 L1` (폴더 핸들) |
| 같은 함수 `+0x3bf` 직후 (rva `0x7ad0f`) | `r eax`, `db @rbp+0x260 L0x10` (`+0x268` 바이트 = 격리) |
| `KERNELBASE!OutputDebugStringA+0xc2` (rva `0xc3eb2`) | `r rax` (뮤텍스 핸들, 0 이면 실패 깃발 쪽) |
| `OutputDebugStringA+0x11e` · `+0x1a4` | `r rax`(`OpenFileMappingW`) · rbx/rsi 가 0 인지 |

## 판정

**분석성공.** 요청 질문 전부 코드 줄로 답했고, 블록 칸 표·최소 칸 목록·syscall 표를 냈다. 정적 한계 두 가지는 위 "동적" 절에 넘겼다 (실제 값·탄 갈래). `ExportFunctions` 가 except 착지 몸통을 못 읽는 문제는 objdump 로 보완했고 제안은 맨 위 "스크립트 문제" 절.

## 짐 지도 반영거리

- 새 짐 (모두 **유저 = 에뮬레이터 가상 주소 공간**, 놓는 법 = 읽기 전용 공유 섹션 뷰 안에 이미 있음):
  1. 공유 섹션 안 `BaseStaticServerData` 블록 `0xa00`B (만드는 곳 csrss `basesrv!ServerDllInitialization`, 놓는 사슬 `csrsrv!CsrLoadServerDll`). 칸 표는 위 2-B.
  2. 표 `CsrSrvSharedStaticServerData` 6칸 (`T[1]` 만 쓰임), `PEB+0x88`·`+0x98`·`+0x380` (이미 run68 에 있음 — 블록과 이어 붙이기).
- 새 짐 (kernelbase `.data`, **유저**): `BaseNamedObjectDirectory` (rva `0x3af0e0`, 폴더 핸들 캐시, 8B), DBWin 전역 둘 (`0x3ad968` 뮤텍스 핸들 · `0x3ad960` 실패 깃발 1B), `pfnAdjustObjectAttributesForPrivateNamespace` (`0x3ad958`, DllMain `entry +0xb3` 이 `0x1800b92d0` 로 씀), `gpServerNlsUserInfo` (`0x3afc88`, `BaseNlsDllInitialize +0x10`), `KernelBaseGlobalData+0x8 ~ +0x27` (Windows·System 디렉터리 `UNICODE_STRING` 사본 2개 + Buffer 옮김), `BaseRCNumber`.
- 새 짐 (**커널 = 에뮬레이터 바깥, syscall 로만**): 이름 있는 객체 폴더 `\Sessions\N\BaseNamedObjects` (csrss 가 세션 시작 때 `NtCreateDirectoryObject` 로 만듦 — `ServerDllInitialization +0xa4f/+0xa56` 등 4곳, 어느 호출이 어느 폴더인지는 안 가림), 토큰 질문 4종의 대답, 첫 `OutputDebugString` 때 만들어지는 `DBWinMutex` 뮤텍스 (이름 있는 객체 폴더 아래, 만드는 곳 `NtCreateMutant` `CreateDBWinMutex +0x3c8`).
- 경로 (지도 기준 함수 사슬): `main → Log → OutputDebugStringA(+0xbd) → CreateDBWinMutex(+0x36c) → BaseGetNamedObjectDirectory(+0xab) → BasepGetNamedObjectDirectoryForToken(+0x2b0)`. csrss 쪽 줄기: `CsrLoadServerDll(+0x37b) → ServerDllInitialization(+0x372 힙, +0x39c 자기주소)`.

## 대조 (결과서 쓰기 전)

- 위에 쓴 `+0x` 중 `.asm` 로 있는 것 (`BasepGetNamedObjectDirectoryForToken`·`BaseGetNamedObjectDirectory`·`CreateDBWinMutex`·`_KernelBaseBaseDllInitialize`·`BaseNlsDllInitialize`·`ServerDllInitialization`·`BaseSrvInitializeIniFileMappings`·kernel32 두 함수·`CsrLoadServerDll`·`CsrApiHandleConnectionRequest`·`NlsGetCacheUpdateCount`·`OutputDebugStringW/A` 앞부분·`BasepAdjust…`·`entry`·`RtlGetCurrentServiceSessionId`) 180줄을 스크립트로 `.asm` 줄과 하나하나 대조했다 (오프셋 + 명령 조각; 불일치 0, 의도적 더미 1 제외).
- `OutputDebugStringA +0x81 ~ +0x312` 는 `.asm` 에 없어서 objdump (`ms/disassemble/kernelbase/OutputDebugStringA.handler.objdump.txt`) 의 절대 주소에서 `절대 주소 − 0x1800c3df0` 로 셌다 (예: call `0x1800c3ead` → `+0xbd`, ret `0x1800c3eb2` → `+0xc2`).
- 점프 목적지는 "목적지 − 함수 시작" (예: `0x18007b056 − 0x18007a950 = 0x706`, `0x1801a1f38 − 0x18007a950 = 0x1275e8`).

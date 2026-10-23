# 21 map-dll-file — 정적 분석 결과

- 요청서: `_workspace/sast/21_map-dll-file.md` (Q14, 02_sprint, 예정 run26)
- 판정: **분석성공** (질문 1~5 전부 답함. 몇 군데는 호출된 함수 안쪽이라 `[추측]`/동적으로 넘김)
- 날짜: 2026-10-23
- 모듈: ntdll 26100.9457. 이미 분석된 모듈이라 analyze 는 안 했다 (`.gpr`, `_index.tsv`, `SetPdb:` 줄 있음). 지도 `_map/` 에서 위치만 확인.
- 한 일: `run_ghidra.sh export ntdll.dll '^(LdrpMapDllSearchPath|LdrpMapDllNtFileName|LdrpMapDllWithSectionHandle)$' 1` (대상 3 + depth 1 = 48개 함수). callee 본문은 안 읽었다.
- 스크립트 문제: 없음.
- 버전 대조: `_calls.tsv` 의 `LdrpLoadDll` call `+0xfb` / ret `+0x100` → `LdrpLoadDllInternal` (SKILL 알려진 사실과 **일치**). 세 함수의 `_calls.tsv` 줄(23 / 21 / 22개)이 `.asm` 의 `CALL ... ret=` 줄과 **전부 같음**을 스크립트로 대조함.
- 이름 확인: 세 함수 모두 `_index.tsv` 에 PDB 이름 그대로 있음 (rva `0x8a50` / `0xd210` / `0x9e50`, 크기 1102 / 1261 / 967).

## 파일 (레포 밖, 커밋 안 함)

- `ms/disassemble/ntdll/LdrpMapDllSearchPath.asm`, `LdrpMapDllNtFileName.asm`, `LdrpMapDllWithSectionHandle.asm`
- `ms/decomp/ntdll/` 같은 이름 `.c` (이번엔 안 읽음)
- `ms/disassemble/ntdll/_calls.tsv` (call 오프셋 / ret 오프셋 둘 다 있음)
- 칸 이름은 `llvm-pdbutil-14 dump -types ms/sym/ntdll.dll.pdb` 로 확인 (스크래치 파일).

## 먼저 알아 둘 칸

PDB 로 확인한 `[정적]`: `_LDR_DATA_TABLE_ENTRY` (`E`)
`+0x30 DllBase`, `+0x40 SizeOfImage`, `+0x48 FullDllName`, `+0x58 BaseDllName`, `+0x68 Flags` (비트 0 `PackagedBinary`, 2 `ImageDll`, 24 `CorILOnly`), `+0x80 TimeDateStamp`, `+0x98 DdagNode`, `+0xb0 LoadContext`, `+0xb8 ParentDllBase`, `+0x108 BaseNameHashValue`, `+0x10c LoadReason` (9 = `PatchImage`), `+0x118 DependentLoadFlags`, `+0x120 CheckSum`.
`_LDR_DDAG_STATE` 2 `Mapped`, 9 `ReadyToRun`. PEB `+3` 비트 `0x10` = `IsPackagedProcess`, PEB `+0xbc` = `NtGlobalFlag`.

`_LDRP_LOAD_CONTEXT` (`ctx`) 는 PDB 에 칸이 없다. 오프셋 + 이번에 코드에서 읽은 역할 (sast/17·18 표에 더함):

| 칸 | 역할 | 근거 |
|---|---|---|
| `+0x00` | 이름 `UNICODE_STRING` | sast/17 |
| `+0x10` | **DLL 검색 경로 구조체 포인터.** `LdrpSearchPath` 의 둘째 인자 | `SearchPath +0xe1 → +0x10e`. 뿌리는 exe 문맥: `LdrpInitializeProcess +0x1313 LdrpInitializeDllPath(0,0,&경로)` 로 만든 것을 `+0x1320` 에서 exe 문맥 `+0x10` 에 넣음 |
| `+0x18` | (이번에 발견) 섹션 핸들 보관 칸 — `ctx+0xb0 ≠ 0` 이고 성공일 때만 | `NtFileName +0x43f` |
| `+0x20` | 플래그. 이번에 읽은 비트: `0x8` (`LdrpSearchPath` 에 넘김), `0x20` (맨 이름만 비교), `0x100` (켜져 있으면 `CodeAuthz` 검사 건너뜀), `0x800` (WOW64 일 때 섹션 속성에 영향), `0x10000` (shim 이 이름을 바꿨을 때 `SearchPath` 가 켬), `0x800000` (켜져 있으면 명부에 안 넣음; sast/18 에서 enclave 갈래로 읽은 그 비트 `[추측]`), `0x1000000` (켜져 있으면 서명 수준 설정을 부름) | 각 줄은 아래 |
| `+0x28` | 결과 `NTSTATUS` 를 쓸 곳을 가리키는 포인터 | `WithSectionHandle +0x1c8` 가 읽음 |
| `+0x30` | 부모 항목 (`E*`). `+0x118`·`+0x50`(부모 `FullDllName.Buffer`)·`+0x30`(`DllBase`)를 읽음 | `SearchPath +0x71~+0xc1`, `WithSectionHandle +0x286` |
| `+0x38` | 이 문맥의 모듈 항목 (`E*`) | 세 함수 입구 |
| `+0xa8` | 맵 크기 `[추측]`. `WithSectionHandle` 이 `RtlImageNtHeaderEx` 의 크기 인자로 읽음 (`+0x74`, call `+0xa1`) | |
| `+0xb0` | **뜻 모름.** `≠ 0` 이면 `SearchPath` 가 이름 겹침 검사를 건너뛰고(`+0x158`), `NtFileName` 이 핸들을 닫지 않고 문맥에 보관함 | 아래 2·1 |
| `+0xb8` | **파일 핸들 보관 칸** (`-1` 로 시작: sast/17). `ctx+0xb0 ≠ 0` 이고 성공일 때 `NtFileName +0x434` 가 씀 (`+0x18` 은 `+0x43f`) | |

## 질문별 답 [정적]

### 1. `LdrpMapDllSearchPath` (rva `0x8a50`, `.asm` 257줄)

**인자**: `RCX = ctx`. 반환 = `NTSTATUS`. 입구에서 `E = ctx+0x38` (`RSI`), 부모 `= ctx+0x30`.

**순서** (call / ret):

1. `+0x52`/`+0x57`, `+0x68`/`+0x6d` `memset` 둘 — 경로 구조체 칸(`rbp+0xb0`, 0x80바이트)과 지역 문자열 버퍼(0x100바이트) 비움. 로컬 `UNICODE_STRING` 넷을 만든다: `A` (`rbp-0x60`, 최대 0x100바이트, 안쪽 버퍼 달림), `B` (`rbp-0x70`), `C` (`rsp+0x58`), `D` (`rsp+0x70`).
2. **경로 구조체 고르기** (`+0xb0~+0xdb`, 호출 없음 → 필요하면 `+0x330`/`+0x335 LdrpInitializeDllPath` 로 갔다 `+0xe5` 로 돌아옴):
   - 부모가 있고 **부모 `DependentLoadFlags(+0x118)` 가 마스크 `0x7b00`** (`LdrpPolicyBits & 4` 면 `0x7f00`) **와 겹치면** (`+0xd8 TEST`, `+0xdb JNZ +0x317`): `LdrpInitializeDllPath(부모 FullDllName.Buffer, (DependentLoadFlags & 마스크) | 1, &새경로(rbp+0xb0))` 를 부르고 `R14 = 새경로`. (`+0x317~+0x340`)
   - 아니면 **`R14 = ctx+0x10`**, 곧 프로세스 시작 때 만든 경로 (위 표).
   - sample 은 `build.bat` 에 `/DEPENDENTLOADFLAG` 가 없어서 `DependentLoadFlags` 가 0 일 것이다 `[추측: 링크 옵션 근거]` → test.exe·dll1·dll2 는 `ctx+0x10` 을 그대로 쓸 것 `[추측]`. 동적 확인: `+0xd8` 직전 `[부모+0x118]`.
3. **`+0x12c` / `+0x131 LdrpSearchPath`** — 인자: `RCX=ctx`, `RDX=R14` (경로 구조체), `R8B=(ctx.flags>>3)&1`, `R9=&B`, 스택 다섯: `&A`, `&D`, `&C`, `&바이트(rsp+0x50)`, `&dword(rsp+0x54)`. 결과 받는 곳은 호출 뒤 쓰임으로 읽었다:
   - `A` → 곧바로 `LdrpMapDllNtFileName` 의 둘째 인자 (이름이 "NtFileName", 그래서 NT 경로 꼴로 보임 `[추측: 이름]`)
   - `C` → `E.FullDllName`, `D` → `E.BaseDllName` (`+0x22d~+0x24e` 에서 복사하고 `C` 는 0 으로 비움). `D` 는 `+0x1b4` 에서 해시해 `E.BaseNameHashValue(+0x108)` 에 씀.
   - 바이트 `≠ 0` 이면 `E.Flags |= 1` (`PackagedBinary`) (`+0x131~+0x13a`). dword 는 ETW 로그 인자로만 감 (`+0x28c`).
   - `B` 는 이 함수 안에서 다시 안 읽는다.
   - 이름 비교용 DLL 이름은 인자로 안 넘기고 `ctx+0x00` 안에 있다 `[추측: sast/17 표 + 호출 인자에 이름 포인터가 없음]`.
4. 결과 `EAX` 분기 (`+0x13e~+0x158`): `0xc0000135` (DLL_NOT_FOUND) 이고 **이미 재시도 중(`R12≠0`)이면 `0xc000007b` 로 끝** (`+0x445`). 그냥 `0xc0000135` 면 계속(`+0x158`). 다른 음수는 끝(`+0x269`). 0 이상이면 계속.
5. **`ctx+0xb0 ≠ 0`** (`+0x158` 비교, `+0x166 JNZ +0x22d`) 이면 6·7번을 건너뛰고 바로 8번.
6. **shim 확인** (`+0x16c`): `g_ShimsEnabled` (`ntdll+0x1d23cc`) 또는 `g_ShimsLoading` (`+0x1d23cf`) 이 `≠ 0` 이면 `+0x37e` 로:
   - **`+0x3b4` / `+0x3b9` 간접 호출 (`cfg:?`) 의 출처 `[정적]`**: 전역 `g_pfnSE_LdrResolveDllName` (`ntdll+0x1e70e8`) 를 `KUSER_SHARED_DATA+0x330`(쿠키)으로 풀어서(ROR + XOR, `+0x37e~+0x3ad`) CFG 디스패치 (`0x180172020`) 로 부른다. 인자: `RCX=&E'(rbp-0x80)`, `RDX=&바이트(rsp+0x51)`, `R8= SearchPath 가 성공이면 &C, 실패(음수)면 ctx`.
   - 바이트가 `0` 이면 아무것도 안 하고 `+0x195` 로. `≠ 0` 이면 `ctx.flags |= 0x10000`, `C` 의 버퍼를 해제하고 **`+0x40d` / `+0x412 LdrpResolveDllName(&E', &A, &D, &C, ctx.flags)`** — 조건은 "(Shims 둘 중 하나가 켜져 있음) 그리고 shim 호출이 바이트 ≠ 0" 둘 다. 결과는 `R15D` 에 넣고 `+0x195`.
   - 둘 다 꺼져 있으면 (이 경우 `+0x182`/`+0x18f` 에서 그냥 지나감) 이 두 호출은 안 탄다. test.exe 가 꺼져 있을지는 값 읽기로 `[동적]`.
7. `+0x195`: `EDI = R15D` (SearchPath 상태). **음수면 끝(`+0x269`)**. 곧, 파일을 못 찾은 상태(`0xc0000135`)로 여기까지 오면 shim 이 살리지 않는 한 `NtFileName` 은 부르지도 않고 오류로 돌아간다.
8. **이미 올라온 DLL 인가 다시 봄** (`+0x1a1~+0x226`):
   - `+0x1af`·`+0x1b4`/`+0x1b9 LdrpHashUnicodeString(&D)` → `E+0x108`.
   - **`+0x1d4` / `+0x1d9 RtlAcquireSRWLockExclusive(LdrpModuleDatatableLock)`** (`ntdll+0x1d2458`).
   - **`+0x1f8` / `+0x1fd LdrpFindLoadedDllByNameLockHeld(&D, (flags&0x20 ? 0 : &C), flags, &찾은항목(rsp+0x68), 해시)`**.
   - 결과 `0xc0000135` 이고 `ctx.flags & 8` 이면 (`+0x206~+0x20a`, `JNZ +0x341`) **`+0x355` / `+0x35a` 다시 한 번**: 이름 칸을 비우고(`RCX=0`) `C`(전체 경로) 로만 찾음. 찾으면 찾은 항목 `Flags |= 1` 하고 락을 풀고(`+0x374`/`+0x379`) `+0x269`.
   - `+0x217` / `+0x21c RtlReleaseSRWLockExclusive`. 결과가 `0xc0000135` 가 **아니면 `+0x269`** (찾았거나 다른 오류).
   - 해석: 이 락 검사는 "경로로 풀린 이름이 이미 명부에 있나" 를 본다 `[정적: 코드]`. **그 사이 다른 스레드가 같은 DLL 을 먼저 올렸는가** 라는 이유는 `[추측]` (이 검사가 있는 자리가 락 안이라 가능할 뿐 코드에 이유는 안 쓰여 있음).
9. 못 찾았으면 (`0xc0000135` 그대로): `+0x22d` 에서 `LdrpFreeUnicodeString(&E.FullDllName)` (`+0x231`/`+0x236`) 하고 `C`→`E.FullDllName`, `D`→`E.BaseDllName` 로 넘긴 뒤 **`+0x257` / `+0x25c LdrpMapDllNtFileName(ctx, &A)`**. (`ctx+0xb0 ≠ 0` 이면 이 줄에서 시작: 위 5번.)
10. 결과 `EDI`. **`0x4000000e` (이미지 기계 종류 불일치) 이면 `+0x41a`**: `A` 를 비우고 `+0xe5` 로 돌아가 **`LdrpSearchPath` 부터 다시** 한다. 이번엔 `R12=1` 이라 못 찾으면 `0xc000007b` 로 끝난다 (`+0x14f`). 다음 후보를 어떻게 고르는지는 `SearchPath` 안쪽 `[추측]`. 그 밖의 값은 `+0x269`.
11. **끝맺음 `+0x269`**: 찾은 항목(`[rsp+0x68]`) `≠ 0` 이면 **`+0x30d` / `+0x312 LdrpLoadContextReplaceModule(ctx)`** (찾은 항목은 `RDX`, 이미 있는 모듈로 이 문맥의 항목을 바꿈 `[추측: 이름]`) — 이때 매핑은 안 한다 (결과 `EDI` 는 0 이상 그대로). 없으면 `LdrpIsSecurityEtwLoggingEnabled` (`+0x277`/`+0x27c`) 가 참일 때만 `LdrpLogEtwDllSearchResults` (`+0x287`/`+0x28c`). 이어 버퍼 해제 3곳(`+0x299`, `+0x2bd`, `+0x3d5`/`+0x427` 도), **`+0x2d2` / `+0x2d7 LdrpReleaseDllPath(&rbp+0xb0)`** (항상), 쿠키 검사(`+0x2e3`/`+0x2e8`), 반환.

**Q5 (exe 폴더)**: 이 함수 안에는 "exe 폴더" 를 직접 넣는 줄이 **없다**. 검색 폴더 목록은 (a) 프로세스 시작 때 만든 `ctx+0x10` 또는 (b) 2번의 `LdrpInitializeDllPath` 가 만든 것이고, 실제 폴더를 도는 일은 `LdrpSearchPath` 몫이다. → "exe 폴더가 들어가는 자리" 는 이 세 함수 밖 (`LdrpInitializeDllPath`, `LdrpSearchPath`).

### 2. `LdrpMapDllNtFileName` (rva `0xd210`, `.asm` 280줄)

**인자**: `RCX = ctx`, `RDX = NT 파일 이름 UNICODE_STRING*` (`SearchPath` 의 `A`). 입구: `R14 = E(ctx+0x38)`, `R15 = ctx+0xb0`.

- **`+0x46` / `+0x4b LdrpCheckForRetryLoading(ctx, 0)`**: 결과 `AL ≠ 0` 이면 **`0xc000022d` 로 바로 돌아간다** (`+0x4d JNZ +0x22e`, 파일 안 엶). 그 안의 조건은 callee 라 안 읽음 `[추측: 이름]`. 반환 `0xc000022d` 의 이름은 모른다 (이 레포에서 처음).
- `+0x71` / `+0x76 LdrpLogDllState(E.DllBase, &E.FullDllName, 0x14a5)`.
- `OBJECT_ATTRIBUTES` (`rbp-9`): `Length=0x30`, `RootDirectory=0`, `ObjectName=RDX`, `Attributes = LdrpUseImpersonatedDeviceMap(ntdll+0x1d29a0) 가 0 이면 0x840, 아니면 0x40` (`+0x76~+0x9b`; `0x40` = `OBJ_CASE_INSENSITIVE`, `0x800` 비트는 impersonated 장치 맵 무시 `[추측: 이름]`).
- **`+0x109` / `+0x10e NtSystemDebugControl`**: 조건 = `PEB.NtGlobalFlag(+0xbc) & 0x40000` (`+0xde`~`+0xe8`, 해당 비트는 gflags 의 `KDEBUG_SYMBOL_LOAD` 로 알려진 값 `[추측: SDK 상수 기억]`). 인자 `Command=0x26`, 입력 = 이름 `UNICODE_STRING` 16바이트 복사본. → test.exe 길에선 `NtGlobalFlag` 에 따라 다르다 (기본 0 이면 **안 탄다**). 동적: `dd @$peb+0xbc L1`.
- ETW 잡일: `RtlGetCurrentServiceSessionId` 4번, `LdrpLogEtwEvent` 2번 (`+0x34e`, `+0x3aa`) — 읽지 않음.
- **`+0x132` / `+0x137 NtOpenFile`** `[정적]`: `DesiredAccess = 0x100021` (`FILE_READ_DATA | FILE_EXECUTE | SYNCHRONIZE`), `ShareAccess = 5` (`READ | DELETE`), `OpenOptions = 0x60` (`FILE_NON_DIRECTORY_FILE | FILE_SYNCHRONOUS_IO_NONALERT`), 위 `OBJECT_ATTRIBUTES`, 핸들은 `rbp-0x49`. (값은 SDK 상수와 맞춘 것 `[추측: 이름]`, 숫자는 `.asm` 그대로.)
  - 실패(`+0x139 JS +0x2cc`): `0xc0000034` 또는 `0xc000003a` (이름/경로 없음) → **`0xc0000135` 로 바꿔 돌려줌** (`+0x2d7`). `0xc0000022` (접근 거부) 이고 아직 재시도 안 했으면 `LdrpCheckComponentOnDemandEtwEvent` (`+0x4d7`/`+0x4dc`) 가 참일 때 **한 번만 다시 `NtOpenFile`** (`+0x4ea → +0x111`). 그 밖은 상태 그대로.
- `+0x141`: `LdrpAuditIntegrityContinuity` (`ntdll+0x1d2918`) `≠ 0` 이면 **`+0x407` / `+0x40c LdrpValidateIntegrityContinuity(ctx, 파일핸들)`**. 실패해도 `LdrpEnforceIntegrityContinuity` (`+0x1d2900`) 가 0 이면 계속, `≠ 0` 이면 파일 핸들만 닫고 실패 반환. 평소엔 둘 다 꺼진 정책 값일 것 `[추측]` → 동적으로 두 바이트 읽기.
- `+0x14e`: `R13D = 0x1000000` (**`SEC_IMAGE`**). 그 뒤:
  - `ctx.flags & 0x1000000` 이면 (`+0x154`, `JNZ +0x448`): `PEB+3 & 0x10` (**`IsPackagedProcess`**) 이 꺼져 있을 때만 **`+0x47e` / `+0x483 LdrpSetModuleSigningLevel(파일핸들, E, &…, 8, &…)`**, 실패면 파일 핸들 닫고 반환.
  - `UseWOW64 (ntdll+0x1d2868) ≠ 0` (WOW64 프로세스) 이면 `ctx.flags & 0x800` 이 꺼진 경우 `R13D = 0x1100000` (`+0x4a0`). test.exe(64비트) 에선 이 갈래 없음 `[정적 조건 + 추측: 64비트]`.
- **`+0x18f` / `+0x194 NtCreateSection`** `[정적]`: `DesiredAccess = 0xd` (`SECTION_QUERY | MAP_READ | MAP_EXECUTE`), `ObjectAttributes = 0`, `MaximumSize = 0`, `SectionPageProtection = 0x10` (`PAGE_EXECUTE`), `AllocationAttributes = R13D` = **`0x1000000` (`SEC_IMAGE`)**, 파일 핸들 = 방금 연 것. 결과 섹션 핸들 = `rbp-0x41`.
  - 실패(음수) 분기 (`+0x19f~+0x1d0`): `0xc000047e`/`0xc000047f`/`0xc0000462` → `+0x494` / `+0x499 LdrAppxHandleIntegrityFailure(상태)` (상태를 그 반환으로 바꿈). `0xc0000017`, `0xc000009a`, `0xc000012d` (자원 부족류) → 아래 로그만. **그 밖의 모든 실패 (예: 이미지 아님) → `+0x201` / `+0x206 NtRaiseHardError(0xc000007b, 매개변수 2개, 마스크 1, {&E.FullDllName, 상태}, 응답 옵션 1)`** 를 하고, `LdrInitState ≠ 3` 이면 `LdrpFatalHardErrorCount++`. 이어 `LdrpLogError(상태, 0x1485, 0, &FullDllName)` (`+0x227`/`+0x22c`) 후 파일 핸들 닫고 반환.
  - → **test.exe 의 정상 DLL 길에선 `NtRaiseHardError`·`LdrAppxHandleIntegrityFailure` 는 `NtCreateSection` 이 실패해야만 탄다 (정상이면 안 탐).**
- 성공 (`+0x244`): ETW 잡일(`+0x244`/`+0x249`, `+0x358`/`+0x35d`).
- **`+0x277` (`TEST ctx.flags,0x100`)**: `UseWOW64 = 0` 이고 `ctx.flags & 0x100 = 0` 이면 **`+0x2e5` / `+0x2ea LdrpCodeAuthzCheckDllAllowed(이름, 파일핸들)`**. 반환이 0 이상이거나 `0xc0000225` 면 통과, 그 밖의 오류면 섹션·파일 핸들 닫고 그 오류를 돌려줌. (비트 `0x100` 의 뜻은 모름.)
- **`+0x287` / `+0x28c LdrpMapDllWithSectionHandle(ctx, 섹션핸들)`** (`RCX=ctx`, `RDX=[rbp-0x41]`). 결과 `EDI`.
- 핸들 닫기 (`+0x28e`):
  - **`ctx+0xb0 == 0` (평소)**: `+0x29b` / `+0x2a0 NtClose(섹션핸들)` → `+0x2a4` / `+0x2a9 NtClose(파일핸들)` → 반환. **둘 다 닫는다** (뷰는 이미 `NtMapViewOfSection` 으로 붙었으므로 핸들을 닫아도 매핑은 남는다 — 이건 일반 지식, 이 코드의 근거 아님).
  - `ctx+0xb0 ≠ 0`: 성공이면 **안 닫고** `ctx+0xb8 = 파일핸들`, `ctx+0x18 = 섹션핸들` 로 넘기고 반환 (`+0x428~+0x443`). 실패면 둘 다 닫음.
- **test.exe 길에서 안 탈 것으로 보이는 것** (정적 조건 정리): `NtSystemDebugControl` (`NtGlobalFlag` 0x40000 없으면), `LdrpValidateIntegrityContinuity` (`Audit` 바이트 0 이면), `LdrpSetModuleSigningLevel` (`ctx.flags` 비트 24 없으면), `NtRaiseHardError`·`LdrAppxHandleIntegrityFailure` (`NtCreateSection` 성공이면). **탈 것으로 보이는 것**: `LdrpCheckForRetryLoading` (호출은 항상, 결과는 모름), `NtOpenFile`, `NtCreateSection`, `LdrpCodeAuthzCheckDllAllowed` (`ctx.flags & 0x100` 이 0 이면 `[동적]`).

### 3. `LdrpMapDllWithSectionHandle` (rva `0x9e50`, `.asm` 214줄)

**인자**: `RCX = ctx` (`RDI`), `RDX = 섹션 핸들` (`RBX`). 반환 `NTSTATUS`.

**일의 코드 순서** (주소 순서와 다르다: `+0x1c0` 블록이 위에 있어도 실제로는 `+0x2c7` 에서 점프해 들어옴):

1. `+0x3e` / `+0x43` `memset(rsp+0x50, 0, 0x218)` (패치 갈래용 버퍼).
2. **`+0x49` / `+0x4e LdrpMinimalMapModule(ctx, 섹션핸들)`** — 여기서 메모리에 붙는다 (지도: 안에 `+0x179 NtMapViewOfSection`, 이번엔 안 읽음). 결과 `EBX`:
   - `0x4000000e` 또는 음수 → 그대로 반환 (`+0x55`, `+0x5d` → `+0x36`). (`SearchPath` 가 `0x4000000e` 를 받아 다시 찾는 이유가 여기서 온다.)
   - 그 밖의 0 이상 값은 **`EBP` 에 보관해 `LdrpCompleteMapModule` 3번째 인자로 그대로 넘긴다** (`+0x92`, `+0x244`). 값 종류 (예: `STATUS_IMAGE_NOT_AT_BASE` 같은 성공-경고) 는 `LdrpMinimalMapModule` 안쪽 `[추측]`.
   - 결과로 받는 칸 `[정적: 읽는 쪽]`: `E.DllBase(+0x30)` 와 `ctx+0xa8` 를 바로 아래에서 읽는다 (`+0x9d`, `+0x74`). 이 두 칸을 `LdrpMinimalMapModule` 이 채운다는 것은 `[추측]`.
3. `+0x63`~`+0x6e`: `E.LoadReason == 9` (패치 이미지) 일 때만 **`+0x30e` / `+0x313 NtQueryVirtualMemory(-1, DllBase, 클래스 2, 버퍼, 0x218, …)`** → `+0x32c` / `+0x331 LdrpResolvePatchDllName` → `+0x343` / `+0x348 LdrpHashUnicodeString(BaseDllName)` → `E+0x108` 갱신. **dll1·dll2 (`LoadReason` 0, 정적 import) 는 안 탄다.** (정적 import 의 `LoadReason` 이 9 가 아닌 것은 `[동적]` 으로 `E+0x10c` 를 읽어 확인.)
4. `+0xa1` / `+0xa6 RtlImageNtHeaderEx(0, DllBase, ctx+0xa8, &NT헤더(rsp+0x30))`. 음수면 반환.
5. **`ctx.flags & 0x800000` 이 켜져 있으면** (`+0xb0`, `JNZ +0x205`): 락·명부 넣기를 건너뛰고 `E.TimeDateStamp(+0x80)`, `E.CheckSum(+0x120)`, `E.SizeOfImage(+0x40)` 을 NT 헤더 값에서 채운 뒤 (`+0x205~+0x222`) 7번으로. (`EBP = 0`.)
6. **명부에 넣기 — 락 안** (`ctx.flags & 0x800000 == 0`):
   - `+0xd4` / `+0xd9 RtlAcquireSRWLockExclusive(LdrpModuleDatatableLock)`.
   - `+0xfd` / `+0x102 LdrpFindLoadedDllByNameLockHeld(&E.BaseDllName, (flags&0x20 ? 0 : &E.FullDllName), flags, &찾은항목(rsp+0x38), E.해시)`.
   - 결과가 `0xc0000135` (없음) 일 때만 `+0x12f` / `+0x134 LdrpFindLoadedDllByMappingLockHeld(DllBase, NT헤더, {TimeDateStamp, SizeOfImage}, &찾은항목)` (헤더의 `+8`, `+0x50` 칸).
   - **찾은 항목 `R14 == 0` 이면** (`+0x139 JNZ +0x1a1` 를 건너뜀): **`+0x141` / `+0x146 LdrpInsertDataTableEntry(E)`** → **`+0x14c` / `+0x151 LdrpInsertModuleToIndexLockHeld(E, NT헤더)`**.
   - `+0x158` / `+0x15d RtlReleaseSRWLockExclusive`.
7. **찾은 항목이 있었으면 (`R14 ≠ 0`)** (`+0x165 TEST`, 그 외는 8번):
   - `E.LoadReason != 9` 이면 → `+0x3be` / `+0x3c3 LdrpLoadContextReplaceModule(ctx, 찾은항목)` 하고 **끝** (반환 `EBX` = 0). 이 경우 이후 `CompleteMapModule`·`ProcessMappedModule`·`MapAndSnap` 모두 **안 부른다**.
   - `E.LoadReason == 9` 이고 찾은 항목도 `9` → 위와 같이 `ReplaceModule`.
   - `E.LoadReason == 9` 인데 찾은 항목은 `9` 가 아니면 → 오류 `0xc00004c0`: `+0x1b1` / `+0x1b6 LdrpLogEtwHotPatchStatus`, `+0x1b9` / `+0x1be LdrpDereferenceModule(찾은항목)`, 반환.
8. **새 모듈이면 마무리 (코드 순서)**:
   - `+0x225`: 현재 스레드 ID (`TEB+0x48`) 가 `LdrpDllNotificationLock` 의 소유 스레드 (`+0x10`) 와 같으면 **`0xc0000225` 로 끝** (`+0x239 JZ +0x357`). 곧 DLL 알림 콜백 안에서 새 DLL 올리기를 막는 것으로 보임 `[추측]`.
   - **`+0x24a` / `+0x24f LdrpCompleteMapModule(ctx, NT헤더, EBP=MinimalMap 결과)`**. 음수면 반환.
   - **`+0x267` / `+0x26c LdrpProcessMappedModule(E, ctx.flags, 1)`**. 음수면 반환. (그 안에서 `LdrpSignalModuleMapped`: sast/19. 이번엔 안 읽음.)
   - `+0x27d` / `+0x282 LdrpLogNewDllLoad(ctx+0x30(부모), E)`.
   - 부모가 있으면 `E.ParentDllBase(+0xb8) = 부모.DllBase` (`+0x286~+0x28f`).
   - **`+0x2a5~+0x2c7`**: `ctx.flags & 0x800000 == 0` **그리고** `E.Flags & 4 (ImageDll) == 0` 인 **두 조건이 다 참이면** `LdrpMapAndSnapDependency` 를 **건너뛴다** (패치 이미지의 부모가 exe 인 경우만 예외로 계속). `LdrpLogDllState(…, 0x14ae)` 하고 `E.DdagNode.State = 9 (ReadyToRun)` 로 쓰고 0 을 반환 (`+0x387~+0x3b3`). 곧 "이미지가 아닌 모듈은 import 를 안 펼치고 바로 ReadyToRun" `[정적: 코드; 의미는 추측]`.
   - 그다음 `E.Flags` 비트 24 (**`CorILOnly`**) 가 **꺼져 있으면** **`+0x1c3` / `+0x1c8 LdrpMapAndSnapDependency(ctx)`** 하고 결과는 `*(ctx+0x28)` 을 읽어 반환 (`+0x1c8~+0x1cc`). **켜져 있으면** `+0x2d0` / `+0x2d5 LdrpCorProcessImports(E)` 를 부르고 그 결과를 반환 (MapAndSnap 안 함).

**요점 답**:
- **dll1·dll2 가 PEB.Ldr 명부에 들어가는 순간이 여기인가: 예 `[정적]`** — 정확히 `+0x141` / `+0x146 LdrpInsertDataTableEntry` (같은 이름·같은 매핑의 모듈이 이미 없고 `ctx.flags & 0x800000` 이 꺼진 새 DLL 일 때). 근거: 앞선 결과 sast/13 (`refs PebLdr`) 에서 PEB.Ldr 의 적재/메모리 순서 목록(`+0x10`/`+0x20`)에 쓰는 함수는 `LdrpInsertDataTableEntry` 뿐이었다. 색인은 바로 뒤 `+0x14c` / `+0x151`.
- **순서는 "자기 import 를 먼저 펼치고 마무리"가 아니라 반대다 `[정적: 코드 순서]`**: `LdrpCompleteMapModule` (`+0x24a`) → `LdrpProcessMappedModule` (`+0x267`, 안에서 `State=2 Mapped` + `LdrpSignalModuleMapped`) → `LdrpLogNewDllLoad` → `ParentDllBase` → **그다음** `LdrpMapAndSnapDependency` (`+0x1c3`, import 펼치기·재귀). 주소는 `+0x1c3` 가 `+0x24a` 보다 앞이지만 실제로는 `+0x2c7 JNC +0x1c0` 점프로 가장 뒤에 탄다.
- **`LdrpMapAndSnapDependency` 를 부르는 조건**: (`ctx.flags & 0x800000` 이 켜짐 **또는** `E.Flags & 4` 가 켜짐 **또는** 위 패치 예외) **그리고** `E.Flags` 비트 24 (`CorILOnly`) 꺼짐. 그리고 새 모듈일 때만 (7번에서 이미 있는 모듈이면 여기에 안 옴).
- **재배치는 어느 호출 몫인가 `[추측]`**: 이 함수 안에 재배치를 하는 줄은 없다. 후보 둘: ① `LdrpCompleteMapModule` — `LdrpMinimalMapModule` 의 결과 코드 (`EBP`) 를 세 번째 인자로 받는다. 기준 주소가 바뀐 맵은 `NtMapViewOfSection` 이 성공-경고 코드를 돌려줄 것이고 (이 코드는 callee 라 안 읽음) 그 코드가 여기로 넘어간다. ② `LdrpProcessMappedModule` — 안에 `+0x24d LdrpIsExecutableRelocatedImage` 가 있다 (지도). 어느 쪽이 주소 고치기를 하는지는 이 요청서로는 못 정한다 → 다음 정적 후보.
- **`NtQueryVirtualMemory` / `LdrpResolvePatchDllName`**: `E.LoadReason == 9` 일 때만 (위 3번). **`LdrpLoadContextReplaceModule`**: 위 7번 (이미 같은 모듈이 있고 패치 아닌 일반 경우).

### 4. 한 바퀴 그림 잇기 (이어진 트리, 코드 순서)

```
LdrpProcessWork +0x6a  (State==0, LoadReason!=9, ctx.flags 비트 20·9 꺼짐)
└─ LdrpMapDllSearchPath(ctx)                                  [rva 0x8a50]
   ├─ (부모 DependentLoadFlags 겹치면) LdrpInitializeDllPath → 새 경로, 아니면 ctx+0x10
   ├─ +0x12c/+0x131 LdrpSearchPath → A(NT 경로) C(전체 이름) D(기본 이름)
   │     └─ 못 찾음(0xc0000135) 이고 shim 없음 → 오류로 끝
   ├─ [shim 켜짐일 때만] +0x3b4/+0x3b9 g_pfnSE_LdrResolveDllName → +0x40d/+0x412 LdrpResolveDllName
   ├─ 락(LdrpModuleDatatableLock) 안: +0x1f8/+0x1fd LdrpFindLoadedDllByNameLockHeld (이미 있나)
   │     └─ 찾음 → +0x30d/+0x312 LdrpLoadContextReplaceModule, 끝 (매핑 안 함)
   └─ 못 찾음 → +0x257/+0x25c LdrpMapDllNtFileName(ctx, A)     [rva 0xd210]
        ├─ +0x46/+0x4b LdrpCheckForRetryLoading → true 면 0xc000022d 로 끝
        ├─ +0x132/+0x137 NtOpenFile (0x100021, share 5, options 0x60)
        ├─ +0x18f/+0x194 NtCreateSection (0xd, PAGE_EXECUTE 0x10, SEC_IMAGE 0x1000000)
        ├─ +0x2e5/+0x2ea LdrpCodeAuthzCheckDllAllowed
        ├─ +0x287/+0x28c LdrpMapDllWithSectionHandle(ctx, 섹션)   [rva 0x9e50]
        │    ├─ +0x49/+0x4e LdrpMinimalMapModule     (→ NtMapViewOfSection; 메모리에 붙음)
        │    ├─ +0xa1/+0xa6 RtlImageNtHeaderEx
        │    ├─ 락 안: ByName(+0xfd/+0x102) → ByMapping(+0x12f/+0x134)
        │    │     ├─ 이미 있음 → +0x3be/+0x3c3 LdrpLoadContextReplaceModule, 끝
        │    │     └─ 없음 → +0x141/+0x146 LdrpInsertDataTableEntry  ← PEB.Ldr 에 들어감
        │    │              → +0x14c/+0x151 LdrpInsertModuleToIndexLockHeld
        │    ├─ +0x24a/+0x24f LdrpCompleteMapModule
        │    ├─ +0x267/+0x26c LdrpProcessMappedModule  (State=2, LdrpSignalModuleMapped)
        │    ├─ +0x27d/+0x282 LdrpLogNewDllLoad;  ParentDllBase 기록(+0x28f)
        │    └─ (CorILOnly 꺼짐) +0x1c3/+0x1c8 LdrpMapAndSnapDependency(ctx)  ← 재귀 (자기 import 펼치기)
        ├─ NtClose(섹션) +0x29b/+0x2a0 ; NtClose(파일) +0x2a4/+0x2a9   (ctx+0xb0==0 일 때)
        └─ (0x4000000e 면 SearchPath 로 돌아가 다시 찾음)
```

### 5. dll1·dll2 길 확인 (정적 조건까지)

- `LdrpProcessWork +0x6a` 에서 `LdrpMapDllSearchPath` 로 오는 조건은 이미 sast/19. 이번 세 함수 안에서 **KnownDll 갈림은 없다** (KnownDll 은 `LdrpLoadKnownDll +0xf1` 쪽에서 같은 `LdrpMapDllWithSectionHandle` 로 만난다: sast/16). dll1·dll2 는 test.exe 폴더에서 `LdrpSearchPath` 로 찾아 `NtOpenFile` 로 여는 길을 탈 것 `[추측]` (둘이 KnownDll 이 아닌 건 sast/18 `+0x1792` 확인 필요, 여기선 안 봄).
- exe 폴더가 들어가는 자리 = 이 세 함수 안엔 **없다** (1번 끝). "`LdrpSearchPath`/`LdrpInitializeDllPath` 몫".
- 이 길에서 dll1·dll2 가 걸러지는 갈림: ① `SearchPath` 4·7번 (못 찾음 → 오류), ② 락 안 이미-있음 검사 (8번) — 같은 이름이 이미 올라와 있으면 올리지 않는다, ③ `NtFileName` 의 정책 검사들 (안 탈 가능성 큼, 2번 표).

## `?` 칸 채움 목록

- run24 일감 한 바퀴 그림: `LdrpProcessWork +0x6a → LdrpMapDllSearchPath → ?` 의 **`?` = `LdrpMapDllNtFileName` (call `+0x257`/ret `+0x25c`)**, 그 안 `NtOpenFile` → `NtCreateSection` → `LdrpMapDllWithSectionHandle` (call `+0x287`/ret `+0x28c`). 매핑 함수 칸 = `LdrpMinimalMapModule` (call `+0x49`/ret `+0x4e`).
- run22 그림 `LdrpMapDllWithSectionHandle +0x1c8` 이전의 칸: `LdrpMinimalMapModule` → 명부 `LdrpInsertDataTableEntry` → 색인 → `LdrpCompleteMapModule` → `LdrpProcessMappedModule` → (그다음) `MapAndSnap`.
- 요청서 4번의 예상 순서 `… → LdrpMapAndSnapDependency 재귀 → LdrpCompleteMapModule → LdrpProcessMappedModule` 는 **순서가 반대** (코드대로: `CompleteMapModule` → `ProcessMappedModule` → `MapAndSnap`).
- run23 그림 (`LdrpLoadDependentModuleInternal +0x182c LdrpQueueWork` → 일감) 에서 일감이 `LdrpProcessWork` 를 지나 이 파일 길로 이어지는 칸 = 위 트리.

## 그림 2 `(정적)` 상자 후보

1. **"`LdrpMapDllWithSectionHandle`: 먼저 마무리(`CompleteMapModule` → `ProcessMappedModule`), 그다음 자기 import 펼치기(`MapAndSnapDependency`)"** — 요청서 예상과 반대이고 DLL 한 개의 처리 순서를 정한다.
2. **"`LdrpInsertDataTableEntry` = PEB.Ldr 명부에 들어가는 자리 (`WithSectionHandle +0x141`)"** — 이 레포의 핵심 질문 하나에 답.
3. **"`NtOpenFile`(읽기+실행) → `NtCreateSection`(`SEC_IMAGE`) → 뷰 매핑 → 핸들 둘 닫음"** — 파일이 메모리에 붙는 길의 대표 점.
4. (선택) **"이미 올라온 모듈은 `LdrpLoadContextReplaceModule` 로 바꿔치기, 새로 매핑 안 함"** — 두 군데(`SearchPath +0x30d`, `WithSectionHandle +0x3be`).

## 동적으로 확인할 것 (dast 요청 필요)

기존 Q20 (kernel32·kernelbase 올라오는 순간), Q23 (dll1·dll2 올라오는 순간), Q47 와 겹치는 것은 번호를 붙인다. 오프셋은 `ntdll + rva` (rva = 함수 rva + 오프셋; 아래는 모두 call 명령 위치이고 ret 은 +5).

| 확인 | 어디서 | 번호 |
|---|---|---|
| dll1·dll2 가 `LdrpMapDllSearchPath` 를 타는가, `ctx+0x10` 이 가리키는 경로 | `SearchPath` 입구 `ntdll+0x8a50`, `+0x12c` call → `ntdll+0x8b7c` (`rcx`=ctx) | 새 (Q23 에 붙임) |
| `NtOpenFile` 의 파일 이름 (`dll1.dll`, `dll2.dll` 의 NT 경로) | `NtFileName +0x132` → `ntdll+0xd342`, `NtFileName` 입구 `ntdll+0xd210` 에서 `rdx` 가 가리키는 `UNICODE_STRING` (`du poi(@rdx+8)`) | Q23 |
| `NtCreateSection` 속성 (`SEC_IMAGE 0x1000000`) | `ntdll+0xd39f`, 스택 `[rsp+0x28]` | 새 |
| `LdrpInsertDataTableEntry` 가 dll1·dll2 에서 불리는 순서 (명부에 들어가는 순간) | `WithSectionHandle +0x141` → `ntdll+0x9f91` | Q23 (dll 이름·주소와 `lm` 비교) |
| `CompleteMapModule` → `ProcessMappedModule` → `MapAndSnap` 순서 | `ntdll+0xa09a`, `+0xa0b7`, `+0xa013` (call 자리) 를 같은 DLL 에서 차례로 | 새 |
| `LdrpMinimalMapModule` 결과 코드 (`EBX`), 재배치 여부 | `ntdll+0x9e9e` (ret 자리) 에서 `eax` | 새 (dll 주소가 실행마다 바뀜 run8 과 연결) |
| 안 탈 갈림이 정말 안 타는가 | `NtSystemDebugControl` → `dd @$peb+0xbc L1` (`0x40000` 비트), `Audit`/`Enforce` 바이트 `ntdll+0x1d2918`, `+0x1d2900`, shim 바이트 `+0x1d23cc`, `+0x1d23cf` | 새 (값 읽기) |
| `ctx+0xb0` 값 (핸들을 닫는가) | `SearchPath +0x158` 직전 `poi(@rbx+0xb0)` (`ntdll+0x8ba8`) | 새 |

## 다음 정적 후보 (가치)

- **`LdrpMinimalMapModule`** (0xa220, 호출 13): 가치 높음 — 기준 주소·크기 칸을 누가 채우는지, 결과 코드 종류, `NtMapViewOfSection` 인자.
- **`LdrpCompleteMapModule`** (0xa5c0): 가치 높음 — 재배치 담당인지(위 `[추측]`)와 세 번째 인자의 쓰임.
- **`LdrpSearchPath`** (0x8eb0): 가치 중간 — exe 폴더가 들어가는 자리와 "찾은 파일이 여러 후보일 때" 순서. Q14 의 5번 마무리.
- `LdrpProcessMappedModule`: 이미 일부 (sast/19). 재배치 `LdrpIsExecutableRelocatedImage` 만 보면 됨.
- `LdrpInsertDataTableEntry`, `LdrpLoadContextReplaceModule`: 이름과 이미 쌓인 결과로 충분, 급하지 않음.

## `.asm` 다시 대조 (끝낸 뒤)

- 위에 쓴 call/ret 쌍 전부를 `_calls.tsv` 와 `.asm` 의 `CALL ... ret=` 줄에서 스크립트로 대조: 세 함수 모두 줄 수·오프셋·이름 일치.
- 점프 목적지는 `절대 주소 − 함수 시작` 으로 다시 계산해 본문에 쓴 값과 맞췄다: `SearchPath`: `+0x317` (`LdrpInitializeDllPath` 갈래), `+0x37e` (shim), `+0x41a` (`0x4000000e` 재시도), `+0x341` (이름 칸 비운 재검색), `+0x269` (끝맺음), `+0x445` (`0xc000007b`), `+0x30a` (`ReplaceModule`), `+0xe5` (되돌아가기). `NtFileName`: `+0x22e`, `+0x244`, `+0x2cc`, `+0x400`, `+0x448`, `+0x4a0`, `+0x2de`, `+0x492`, `+0x428`, `+0x111`. `WithSectionHandle`: `+0x205`, `+0x225`, `+0x3b8`, `+0x1c0`, `+0x361`, `+0x387`, `+0x2c2`, `+0x74`, `+0x357`.
- 대조하면서 고친 것: 요청서의 `+0x3b4` 를 call 로, `+0x3b9` 를 ret 로 구분. 요청서 "찾은 뒤 어디로" 의 `+0x30d` = `SearchPath` 의 `ReplaceModule` call 임을 확인. 요청서에 있던 "`WithSectionHandle` 에서 `CompleteMapModule` 이 `MapAndSnap` 뒤" 가정은 코드와 반대로 확인.
- 읽으며 놓쳤을 수 있는 곳: ETW·로그 잡일 (`NtFileName +0xa4~+0xcf`, `+0x304~+0x3af`) 은 읽지 않고 개수만 셈. 위 표의 정책 바이트 이름/뜻은 `.asm` 주석 이름 그대로.

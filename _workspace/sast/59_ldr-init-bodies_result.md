# 59 ldr-init-bodies — 정적 분석 결과

- 요청서: `_workspace/sast/59_ldr-init-bodies.md` (Q120b, 질문 1 ~ 13)
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`, pdb guid+age `C3093720177DA6851519DCFFE8DF53FE1` = VM 사진 `ms/logs/04_baggage-read_P1a.txt` 의 ntdll 과 같은 계열), ntoskrnl (pdb guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준 GUID 와 같음)
- analyze 다시 안 함 (둘 다 `.gpr` + `_index.tsv` + `SetPdb:` 있음). `pgrep -fa analyzeHeadless` 로 같은 프로젝트를 쓰는 작업이 없는 것을 매번 확인하고 export / refs 만 돌림 (잠금 충돌 없음, sast/60 은 안 만남).
- 새로 뽑은 것 (`ms/decomp/<모듈>/`, `ms/disassemble/<모듈>/`, 모두 depth 0 — 깊이 1 로 넓히면 힙·락 같은 공용 부품이 딸려와서 이름 지정으로 뽑음):
  - ntdll (요청서 대상 24개): `LdrpInitializePolicy` `LdrpIncludeAlternateForwarders` `LdrpCheckAppDirType` `RtlpInitCurrentDir` `RtlpCreateNewDirectoryReference` `RtlDetermineDosPathNameType_Ustr` `RtlInitializeProcessorFeaturesBitMap` `LdrpAllocateSchedulerSharedData` `LdrpInitializeTlsHeap` `LdrpGetNewTlsVector` `RtlSetBits` `RtlClearBits` `LdrpValidateEntrySection` `LdrpIsImageAddress` `RtlRbInsertNodeEx` `LdrpFindKnownDll` `LdrpLoadKnownDll` `NtOpenDirectoryObject` `NtOpenSymbolicLinkObject` `NtQuerySymbolicLinkObject` `RtlGetNtSystemRoot` `RtlInitUnicodeStringEx` `RtlAppendUnicodeToString` `RtlFreeAnsiString`.
  - ntdll (길 따라 더한 것, 이유는 괄호): `NtOpenSection` `NtQueryVolumeInformationFile` `NpQueryVirtualMemory` (syscall 번호), `RtlIsProcessorFeaturePresent` (비트맵을 읽는 쪽), `LdrpQueryIllegalCWDDevices` (질문 11 의 쓰는 곳), `LdrpCheckKnownDllFullPath` `LdrpAllocateUnicodeString` `LdrSetDllDirectory` (질문 1·9 의 하위, 이름만 보려고).
  - 이미 있던 `.asm` (다시 안 뽑고 읽기만): `LdrpInitializeProcess` `LdrpProcessMappedModule` `LdrpInitializeTls` `LdrControlFlowGuardEnforced` `LdrpAllocateTls` `LdrpAllocateTlsEntry` `LdrpAcquireSchedulerSharedDataSlot` `LdrpInsertModuleToIndexLockHeld` `LdrpInitializeNtdllDataTableEntry` `LdrpQueryCurrentPatch` `LdrpInitializeProcessHeap` `RtlDeleteBoundaryDescriptor` `NtOpenFile` `NtClose` `NtSetInformationProcess` `NtQuerySystemInformation`.
  - ntoskrnl: `NtQuerySystemInformation` `ExpQuerySystemInformation` (클래스 `0xfa` 갈래만 읽음) `KiSetFeatureBits` `PspSchedulerSharedDataRegionCreate` (질문 5 의 커널쪽 한 줄). 넓힌 이유: 질문 4 의 "클래스 `0xfa` 갈래 함수" 가 `NtQuerySystemInformation` 한 단계 아래 `ExpQuerySystemInformation` 안에 있다 (따로 함수가 없음). `NtSetInformationProcess.asm` 은 이미 있어서 읽기만.
  - 새 `_refs`: ntdll `0x1d3d70` `0x1d3d80` `0x1d23f4` `0x1d23f8` `0x1d2770` `0x1d2960` `0x1d26c8` `0x1d26e0` `0x1e7120`, ntoskrnl `KeProcessorFeaturesBuffer` `KeProcessorFeaturesBitMap` `0xfc7480`.
- 버전 확인 (알려진 사실, `_calls.tsv`): `LdrpInitializeProcess +0x1bb6/+0x1bbb → LdrpInitializeGraphRecurse` 일치, `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` 일치, 커널 GUID 일치. 요청서의 `LdrpInitializeProcess` 호출 자리 10곳 (`+0xdb8/+0xdbd` … `+0x18cb/+0x18d0`) 모두 `_calls.tsv` 와 일치. **일치.**
- 또 하나의 맞춰 보기 (VM 사진 `[관찰]`, `04_baggage-read_P1a.txt`): `ProcessParameters.CurrentDirectory` 가 `Length 0x84, Handle 0x58, Buffer = 힙 주소` 로 바뀐 것, `TEB+0x58` 벡터 슬롯 0 에 exe TLS 블록이 걸린 것이 아래 정적 줄과 그대로 맞는다 (질문 3·6). 두 바이너리 읽기 (스크립트 오프셋 · 파일) 둘 다 믿어도 된다.

## 스크립트 문제

- 없음. 한 가지 참고: `NtQuerySystemInformation.asm` 의 `; ALIASES(1): RtlGetNativeSystemInformation` 은 Ghidra 가 내보내기 이름까지 센 것이고 PDB 별칭 표 (`--lookup 0x161470`) 에는 `Zw`/`Nt` 짝만 있다. 문서에는 `NtQuerySystemInformation` 으로 쓴다.

## 한눈에 (먼저 읽을 것)

1. **test.exe 기본 길에서 묶음 A 의 syscall 은 모두 8번**이다: `\KnownDlls` 쪽 4 (`NtOpenDirectoryObject` · `NtOpenSymbolicLinkObject` · `NtQuerySymbolicLinkObject` · `NtClose`), 현재 디렉터리 2 (`NtOpenFile` · `NtQueryVolumeInformationFile`), 프로세서 기능 1 (`NtQuerySystemInformation` 클래스 `0xfa`), 스케줄러 공유 데이터 1 (`NtSetInformationProcess` 클래스 `0x70`).
2. **`LdrpInitializePolicy` 는 test.exe 에서 사실상 아무것도 안 한다.** 전역 네 칸을 0 으로 쓰고 PEB 비트 둘과 `DllPath.Length` 를 보고 돌아온다 (`LdrSetDllDirectory` 도 안 부름). 정책 전역은 `LdrpPolicyBits` (파일 값 `0x16`, 안 바뀜).
3. **`LdrpIncludeAlternateForwarders` 와 `LdrpCheckAppDirType` 은 test.exe 에서 아예 불리지 않는다** (불리는 조건: `ProcessParameters.Flags & 0x20000000`, `LdrpIllegalCWDDevices` 가 0 도 -1 도 아님). VM 사진 `Flags = 0x4001`, KUSD `CurDirDevicesSkippedForDlls = 0`.
4. **`RtlFreeUnicodeString` 이 `LdrpInitializeProcess +0x1563` 에서 불리는 것은 "현재 디렉터리가 비어 있어서 `C:\` 를 새로 만든" 갈래뿐이다.** 진짜 VM 은 `CurrentDirectory` 가 `C:\windows-pe-internals-main\...\sample\bin\` (0x84 바이트) 라서 이 자리가 건너뛰어진다. PE_emulator 가 이 free 를 봤다면 입력 `ProcessParameters` 의 현재 디렉터리가 비어 있었다는 뜻이다.
5. **클래스 `0xfa` 의 몸통은 `ExpQuerySystemInformation +0xa3d` 와 `+0xf4f ~ +0xf63`** 이고 하는 일은 `KeProcessorFeaturesBuffer` (16바이트) 복사 하나다. 이 버퍼를 쓰는 코드는 `KiSetFeatureBits +0x888` (0 으로 채움) 하나뿐이라 정적으로는 **16바이트 모두 0** 이다 (반증 가능 — 동적 한 번으로 닫는다).
6. **test.exe 의 `LdrpInitializeTls` 는 모듈 1개 (exe) 만 본다** (ntdll 항목은 `LoadReason == 9` 라 건너뜀). 그래서 `LdrpTlsBitmap` 은 9비트 (`1 + 8`), 안쪽 32비트 칸 `LdrpStaticTlsBitmapVector` 를 쓰고, `RtlSetBits(0, 1)` → `RtlClearBits(1, 8)`. TLS 힙은 새로 안 만들고 프로세스 힙을 그대로 쓴다. 사진 `TEB+0x58` 과 맞는다.
7. **`RtlRbInsertNodeEx` 의 첫 두 호출은 모두 회전 없는 갈래다.** 첫 노드 = ntdll (`LdrpInitializeNtdllDataTableEntry +0x111/+0x116`), 둘째 = exe (`LdrpInitializeProcess +0x14cd/+0x14d2`), 둘째는 ntdll 노드의 **왼쪽** 자식이 된다 (`LdrpMappingInfoIndex` 는 파일 값 `TimeDateStamp` 비교로 `[정적 계산]`, `LdrpModuleBaseAddressIndex` 는 `DllBase` 크기 비교라 `[추측]`).

## 질문별 답 [정적]

### 질문 1. `LdrpInitializePolicy` · `LdrpIncludeAlternateForwarders`

근거 파일: `ms/disassemble/ntdll/LdrpInitializePolicy.asm`, `LdrpIncludeAlternateForwarders.asm`, `LdrpInitializeProcess.asm` (+ 각 `.c`).

**`LdrpInitializePolicy` (rva `0xbd38c`, 1697B)** — 인자 없음, 반환값 안 씀 (void).

| 줄 | 하는 일 |
|---|---|
| `+0x13` `+0x1e` `+0x29` | `RCX = &PEB->ProcessParameters->DllPath` (`PEB+0x20` → `+0x50`) |
| `+0x22` `+0x2d` `+0x34` `+0x3b` | 전역 네 qword 를 0 으로: `LdrpAppPackagesPath` (`0x1d3070`, UNICODE_STRING 16B) · `LdrpOriginalAppPackagesPath` (`0x1d3080`, 16B) |
| `+0x42` `+0x46` `+0x48` | PEB `BitField` (`PEB+3`) 의 비트 4 `IsPackagedProcess` 가 켜졌으면 `+0x72` (정책 갈래) |
| `+0x4a` `+0x4c` | 비트 1 `IsProtectedProcess` 가 켜졌으면 `+0x6d`: `DllPath.Length = 0` 쓰고 끝 |
| `+0x4e` `+0x51` | `DllPath.Length == 0` 이면 `+0x58` 로 끝 |
| `+0x53/+0x58` | 아니면 `LdrSetDllDirectory(&DllPath)` 부르고 끝 |

- **test.exe 길**: 사진 P1a 에서 PEB `BitField = 0x4` (비트 4·1 모두 꺼짐), `DllPath = ""` (길이 0) `[관찰]` → `+0x4e` 에서 바로 끝. syscall 없음, 쓰는 칸은 위 전역 네 qword (이미 0 이라 값 안 바뀜).
- **패키지 앱 갈래 (`+0x72 ~`, test.exe 는 안 탐, 조건·이름만)**: `DllPath.Length != 0` 이면 `DllPath` 를 두 전역에 복사 (`+0xb0` `+0xbb`), `LdrpPolicyBits = 0x29` (`+0x7e`), `RtlQueryPackageClaims` 를 다섯 번 (`+0xc3` `+0x14a` `+0x1b9` `+0x228` `+0x2a2`) 불러 AppModelPolicy 값 다섯을 읽고 `LdrpPolicyBits` 의 비트를 켜고 끔 (`&= ~1` `+0x329`, `|= 2` `+0x31d`, `|= 4` `+0x26b`, `|= 0x40` `+0x305`), `LdrpDefaultDllDirectories = 0x1000` (`+0x110`), 마지막 정책이 `0x360001` 이면 `ProcessParameters.Flags` 비트 `0x1a` (`0x4000000`) 켬 (`+0x2eb ~ +0x2ef`). syscall 직접 호출은 없다 (`RtlQueryPackageClaims` 안은 이번 범위 밖).
- **정책 플래그 전역 이름**: `LdrpPolicyBits` (rva `0x1cd7fc`, 4B, ntdll 파일 값 `0x16` — `.data` 앞쪽이라 파일에 값이 있음). 읽는 곳: `LdrpInitializeProcess +0x1007`, `LdrLoadDll +0x106`, `LdrpGetDllPath +0x81`, `LdrSetDllDirectory +0x6` 등.
- **조건 닫힘**: **정적으로 닫힘** (PEB 비트 · `DllPath` 는 사진 값으로 확인). 동적 확인은 선택: `dd ntdll!LdrpPolicyBits L1` 이 `0x16` 그대로인지.

**`LdrpIncludeAlternateForwarders` (rva `0x1217c4`, 37B)** — 인자 없음, 반환 `AL` (BOOLEAN).
- `ProcessParameters.Flags (PP+8) & 0x4000000` 이 켜졌으면 TRUE (`+0xd` `+0x14` → `+0x23`), 아니면 `PEB+0x7c4` (`NtGlobalFlag2`) 의 비트 1 이 켜졌으면 TRUE (`+0x16` `+0x1d`), 아니면 FALSE (`+0x1f`). 쓰는 칸 없음, syscall 없음.
- **부르는 조건**: `LdrpInitializeProcess +0xfdb` 에서 `ProcessParameters.Flags & 0x20000000` 이 켜졌을 때만 (`+0xfe2 JZ → +0xfff`). 사진 `Flags = 0x4001` 이라 **test.exe 에서는 안 불린다**. 불렸을 때 결과는 `+0xfe9` 의 `R12B` 로 받아 `RtlpSystemDirs` 문자열에 `forwarders\alt;` (`+0x10e9`) 를 붙일지 정함.

### 질문 2. `LdrpCheckAppDirType` (rva `0x1191a0`, 353B)

근거: `ms/disassemble/ntdll/LdrpCheckAppDirType.asm`, `_refs/0x1d23f4.tsv`, `LdrpQueryIllegalCWDDevices.asm`.

- **인자**: `RCX` = 실행 파일 경로 UNICODE_STRING (`LdrpInitializeProcess +0xc53/+0xc58` 에서 `ProcessParameters.ImagePathName` (`PP+0x60`) 을 지역 `[RSP+0xf0]` 로 복사한 것, 호출 `+0x117e`). 반환값 없음.
- **하는 일** (줄):
  - `+0x8f/+0x94` `RtlpDosPathNameToRelativeNtPathName` 로 NT 경로 만들기.
  - `+0xe3/+0xe8` **`NtOpenFile`** (스텁 `0x33`): 접근 `0x100001` (`+0xb9`, FILE_READ_DATA | SYNCHRONIZE), 공유 `5` (`+0xdb`), 옵션 `0x60` (`+0xa8`, 동기 + 폴더 아님), 이름은 위 NT 경로, 속성 `0x40`.
  - `+0x106/+0x10b` **`NtQueryVolumeInformationFile`** (스텁 `0x49`): 클래스 `4` (`FileFsDeviceInformation`, `+0xfa`), 길이 8 (`+0xf1`). 출력 8바이트의 `+4` = Characteristics (`[RSP+0x54]`).
  - `+0x10f ~ +0x11b`: `LdrpIllegalCWDDevices & Characteristics` 가 0 이 아니면 `LdrpIllegalCWDDevices = 0` (쓰기 `+0x11b`). 즉 "exe 가 놓인 장치가 CWD 를 막을 장치 종류면 막는 표를 비운다".
  - `+0x12b/+0x130` `NtClose`, `+0x13c/+0x141` `RtlFreeUnicodeString` (NT 경로 문자열).
- **`LdrpIllegalCWDDevices` 의 정체 (질문 11)**: 장치 이름 목록이 아니라 **4바이트 마스크** (FILE_DEVICE 특성 비트). `LdrpQueryIllegalCWDDevices` (`+0x40` `+0x85` `+0x91`, 호출은 `LdrpInitializeExecutionOptions +0x733`, 그 호출은 `LdrpInitializeProcess +0x531`) 가 정함: IFEO 값 `CWDIllegalInDLLSearch` (문자열 `0x17aa90`) 이 있으면 그 값, 없으면 KUSD `+0x2d5` 비트 4~5 (`CurDirDevicesSkippedForDlls`) 에서: `0` → 마스크 `0`, `1` → `0x2000` (WebDAV), `2` → `0x10` (원격 장치), `3` → `-1` (전부). 이 전역은 ntdll 파일에 값이 없는 BSS 꼬리 (`.data` VirtualSize `0x8f58` > RawSize `0x2000`) 라 로드 때 0.
  - 읽는 곳: `LdrpLoadDependentModuleInternal +0x1649`, `LdrpGetFullPath +0x7c`, `LdrpPreprocessDllName +0x6cd`, `LdrpInitializeProcess +0x1169` (DLL 검색에서 CWD 를 뺄지 정하는 쪽).
- **부르는 조건**: `LdrpInitializeProcess +0x1169 ~ +0x1174` 가 `마스크 - 1 > 0xfffffffd` (= 마스크가 `0` 또는 `-1`) 이면 건너뜀 (`JA → +0x1183`). 사진 P1a 에서 KUSD `CurDirDevicesSkippedForDlls = 0y00` → 마스크 `0` (test.exe 에 IFEO 값이 없다는 가정) → **test.exe 에서 안 불린다 `[정적]` + `[관찰]` 일부**. 불릴 때만 syscall 은 `NtOpenFile` → `NtQueryVolumeInformationFile` → `NtClose` 한 벌.
- 닫힘: **정적으로 닫힘**. 동적 확인(선택): `dd ntdll!LdrpIllegalCWDDevices L1` = 0, `LdrpCheckAppDirType` 입구가 안 걸리는지.

### 질문 3. `RtlpInitCurrentDir` (rva `0x70e8`, 144B) · `RtlpCreateNewDirectoryReference` (rva `0x6ed0`, 528B)

근거: 두 `.asm`, `RtlDetermineDosPathNameType_Ustr.asm`, `_refs/0x1d23f8.tsv`, 사진 P1a 11번 절 (`PROCESS_PARAMETERS`).

**`RtlpInitCurrentDir(RCX = UNICODE_STRING* 경로)`** — 반환 NTSTATUS.
- 인자 `RCX`: `LdrpInitializeProcess +0xf26 ~ +0xf2b` 가 `ProcessParameters.CurrentDirectory.DosPath` (`PP+0x38`) 를 지역 `[RSP+0xb0]` 로 복사한 것 (호출 `+0x1517/+0x151c`). 복사본이라 `Buffer` 는 아직 `ProcessParameters` 덩어리 안 원본을 가리킨다.
  - 비어 있을 때 (`Buffer == NULL` 또는 `Length == 0` 또는 첫 글자 NUL, `+0xf44 ~ +0xf53`) 는 8바이트를 새로 할당해 `C:\` (시스템 루트 앞 3글자, `RtlGetNtSystemRoot` 에서 복사 `+0xf89 ~ +0xfa2`, `Length 6 / Max 8` `+0xfa7`) 로 채워 쓴다. 이때만 호출 뒤 `RtlFreeUnicodeString(&[RSP+0xb0])` (`+0x1563`) 가 불린다 (`[RSP+0x5c]` 가 0 으로 바뀌어서, `+0xf84`; 처음 값은 1 `+0x32f`). WOW64·보안 프로세스 (`+0xf0c`/`+0xf19`) 는 대신 시스템 루트 문자열을 넘긴다 (`+0xfb4`).
- 단계:
  1. `+0x1c/+0x21` `RtlDetermineDosPathNameType_Ustr` → 경로 종류 (1 UNC `\\x`, 2 `D:\`, 3 `D:x`, 4 `\x`, 5 상대, 6 `\\.\`, 7 `\\.`). `+0x24 ~ +0x8f` 가 종류 `1·2·6·7` 만 통과시키고 `0·3·4·5` 는 `0xc000000d` (`+0x79`) 로 돌려보낸다.
  2. `+0x32/+0x37` `RtlpCreateNewDirectoryReference(RCX = 경로, EDX = PP+0x3a (DosPath.MaximumLength, 사진 0x208), R8 = &결과)`. 실패 음수면 그대로 반환 (`+0x39`).
  3. 성공하면 **쓰는 칸 넷**: 전역 **`RtlpCurDirRef`** (rva `0x1d23f8`, 새 레코드 포인터) `+0x47`, `ProcessParameters.CurrentDirectory.Handle` (`PP+0x48`) ← 레코드 `+8` (`+0x52`), `DosPath.Buffer` (`PP+0x40`) ← 레코드 `+0x20` (`+0x5a`), `DosPath.Length` (`PP+0x38`) ← 레코드 `+0x18` (`+0x62`). 반환 0.
  - 사진 P1a (`+0x199d` 전) `[관찰]`: `DosPath.Length = 0x84` (66글자, `C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin\`, 끝 `\` 포함), `MaximumLength = 0x208`, `Handle = 0x58`, `Buffer = 0x25996f381c0` (프로세스 힙 안). **정적 줄과 일치.**

**`RtlpCreateNewDirectoryReference(RCX = 경로 ustr, EDX = 최대길이, R8 = 레코드 out)`** — 줄기 끝까지 (더 부르는 건 공용 부품과 syscall).

| 줄 | 하는 일 |
|---|---|
| `+0x62/+0x67` | `RtlpDosPathNameToRelativeNtPathName` 로 NT 경로 (힙 할당) |
| `+0x86` `+0x9d` | `R15D = KUSD+0x2dc` (`DismountCount`) 를 미리 읽어 둠 |
| `+0xbb/+0xc0` | **`NtOpenFile`** (스텁 `0x33`): 접근 `0x100020` (`+0x94`, SYNCHRONIZE \| FILE_TRAVERSE), 공유 `3` (`+0x7e`), 옵션 `0x21` (`+0x73`, 폴더 \| 동기), 속성 `0x40`, 핸들은 지역 (`[RBP+0x7f]`) |
| `+0xd5/+0xda` | `RtlFreeHeap(PEB.ProcessHeap, 0, NT경로.Buffer)` (열기 성공 여부와 무관하게 먼저 해제) |
| `+0xf7/+0xfc` | **`NtQueryVolumeInformationFile`** (스텁 `0x49`): 클래스 `4` (`FileFsDeviceInformation`), 길이 8, 출력 `[RBP-0x29]` |
| `+0x115/+0x11a` | `RtlAllocateHeap(PEB.ProcessHeap, 0, 0x30 + 최대길이)` — 사진 기준 `0x238` 바이트. 실패면 `0xc0000017` 로 핸들 닫고 끝 (`+0x122` `+0x12b`) |
| `+0x14f ~ +0x170` | 레코드 칸 채우기 (아래) |
| `+0x17b/+0x180` | `memcpy(레코드+0x30, 경로.Buffer, 경로.Length)`, 끝에 NUL (`+0x190`) |
| `+0x1a8 ~ +0x1b5` | 끝 글자가 `\` 이면 그대로 성공 (`*R8 = 레코드`, `+0x1b0`) |
| `+0x1ba ~ +0x1e0` | 아니면 `Length + 4 <= 최대길이` 일 때 `\` 와 NUL 을 붙이고 `Length += 2` 하여 성공. 모자라면 `NtClose` (`+0x1eb/+0x1f0`) + `RtlFreeHeap` (레코드, `+0x202/+0x207`) 하고 `0xc0000106` |

- **레코드 칸** (힙 블록 `0x30 + 최대길이`): `+0x0` 참조 수 = 1 (`+0x162`) · `+0x8` 디렉터리 핸들 (`+0x15a`) · `+0x10` KUSD `DismountCount` 복사본 (`+0x16c`) · `+0x18` word 길이 (`+0x198`) · `+0x1a` word 최대길이 (`+0x168`) · `+0x20` 버퍼 포인터 = 레코드 `+0x30` (`+0x15e`) · `+0x28` 장치 Characteristics (`FileFsDeviceInformation +4`, `+0x170`) · `+0x30 ~` 경로 문자열. (이름은 PDB 에 없음 `[추측]`; 이 레코드는 `RtlpReferenceCurrentDirectory`·`RtlSetCurrentDirectory_U` 도 `RtlpCurDirRef` 로 쓴다.)
- **syscall 이 여기서 나오나**: 예. **`NtOpenFile` 1번 + `NtQueryVolumeInformationFile` 1번** (기본 길). 실패 길에서만 `NtClose`.
- 닫힘: **정적으로 닫힘** (사진 값과 일치). 동적 확인(선택): 핸들 `0x58` 이 `RtlpCreateNewDirectoryReference +0xc0` 직후 `[RBP+0x7f]` 값인지.

### 질문 4. `RtlInitializeProcessorFeaturesBitMap` (rva `0x11f910`, 66B) 와 커널 클래스 `0xfa`

근거: `RtlInitializeProcessorFeaturesBitMap.asm`, `RtlIsProcessorFeaturePresent.asm`, 커널 `ExpQuerySystemInformation.asm` `KiSetFeatureBits.asm` `NtQuerySystemInformation.asm`, `_refs`.

**ntdll 쪽 (인자·크기·출력)**
- 호출: `LdrpInitializeProcess +0x1761/+0x1766`, **`LdrpIsSecureProcess == 0`** 일 때만 (`+0x1758 CMP` · `+0x175f JNZ → +0x1766`). 반환값은 호출한 쪽이 안 본다 (`+0x1766` 다음에 `EAX` 검사 없음).
- 본문: `NtQuerySystemInformation(RCX = 0xfa, RDX = &RtlProcessorFeaturesBuffer (rva 0x1d3d70), R8D = 0x10, R9 = &지역 ReturnLength (0 으로 시작))` (`+0x6` `+0xb` `+0x15` `+0x1a` `+0x20`, 호출 `+0x25/+0x2a`, 스텁 번호 `0x36`). 이어서 **비트맵 전역 `RtlProcessorFeaturesBitMap` (rva `0x1d3d80`, 16B = `_RTL_BITMAP`)**: `SizeOfBitMap = 0x80` (`+0x2a`, qword 로 씀), `Buffer = &RtlProcessorFeaturesBuffer` (`+0x35`). 반환은 syscall 상태가 `EAX` 에 남은 채 (`+0x41 RET`).
- 읽는 쪽: `RtlIsProcessorFeaturePresent(n)` — `n >= 0xc0` 거짓, `n < 0x40` 은 KUSD `ProcessorFeatures[n]` 바이트 (`+0xf` `+0x15`, `0x7ffe0274 + n`), 그 사이는 비트맵의 `n - 0x40` 번째 비트 (`+0x1b ~ +0x29`). ntdll 안에서 부르는 번호는 `0x26 ~ 0x2a` · `0x3c` (전부 0x40 미만) 라서 이 비트맵을 실제로 읽는 ntdll 코드는 test.exe 길에 없다 (밖에서 `IsProcessorFeaturePresent(>=0x40)` 부를 때만).

**ntoskrnl 쪽**
- `NtQuerySystemInformation` (rva `0xa06d20`) 은 `+0x24 ~ +0x92` 의 짧은 분류표를 거쳐 `+0x58/+0x5d` 에서 **`ExpQuerySystemInformation`** (rva `0xa06df8`, 3310B) 를 부른다. 클래스 `0xfa` 는 그 분류표의 특수 갈래에 안 걸려 (`+0x92 JNZ → +0x45`) 그대로 넘어간다. **클래스 `0xfa` 만을 위한 함수는 따로 없다.** 몸통은 `ExpQuerySystemInformation` 안:
  - 사용자 모드 호출이면 (`+0xe3 ~ +0xef`: `KTHREAD.PreviousMode` (`[스레드+0x232]`) `!= 0`) `+0x346` 의 검사 표를 거쳐 (`+0x361 ~ +0x39f`, `0xfa = 0xd2 + 0x28`) **`ProbeForWrite(버퍼, 길이, 정렬 4)`** (`+0x3b6/+0x3bb`) 와 ReturnLength 칸을 읽고 다시 쓰는 접근 검사 (`+0x3bb ~ +0x3d6`) 를 한 뒤 본 분류로 돌아간다 (`+0x3dd`).
  - 본 분류: `+0x851` 에서 `0xf7` 보다 크고, `+0x860 SUB ECX,0xfa` · `+0x866 JZ` → **`+0xa3d`**. (`0xfc` · `0xfd` 는 `Feature_...IsEnabled` 를 거치는 다른 갈래 `+0x872` 이고 `0xfa` 는 아님.)
  - `+0xa3d ~ +0xa5a`: 반환 길이 `[RSP+0x44] = 0x10`. 입력 길이 (`R14D`) `< 0x10` 이면 상태 `0xc0000004` (`STATUS_INFO_LENGTH_MISMATCH`, `+0xa4f`).
  - `+0xf4f ~ +0xf63`: **길이의 낮은 3비트가 0 이 아니면** (8의 배수가 아니면) 같은 `0xc0000004` (`+0xf4f TEST R14B,7` · `+0xf53 JNZ → +0xa4f`). 통과하면 `MOVUPS XMM0,[KeProcessorFeaturesBuffer]` · `MOVUPS [RDI],XMM0` (**16바이트 복사**, `+0xf59` `+0xf60`).
  - 끝 `+0x55b ~ +0x564 → +0x69e`: ReturnLength 포인터가 있으면 `0x10` 을 써 주고, 상태 0 을 돌려준다.
- **출력 칸 표** (16바이트 = 128비트 비트맵):

| 칸 | 크기 | 뜻 |
|---|---|---|
| `+0x0` ~ `+0xf` | 16B | 비트 `k` (0 ~ 127) = 프로세서 기능 번호 `0x40 + k` 가 있는지. ntdll 이 `RtlProcessorFeaturesBitMap` 으로 감싸 `RtlIsProcessorFeaturePresent` 에서 읽는다 |

- **nt 가 줄 값**: 원본 `KeProcessorFeaturesBuffer` (rva `0xfc7498`, 16B, `KeProcessorFeaturesBitMap` 은 `0xfc7598`/`+8`=`0xfc75a0`). 이 버퍼를 건드리는 코드는 `_refs` 로 **`KiSetFeatureBits +0x888`** (끝부분, `XORPS XMM0` 뒤 `MOVUPS [buf],XMM0` = **0 으로 채움**, 같은 함수 `+0x881` `+0x88f` 가 비트맵 머리를 만듦: 포인터 · `0x80`) 하나와 읽는 쪽 둘 (`ExpQuerySystemInformation +0xf59`, `RtlIsProcessorFeaturePresent` (nt) `+0x22`) 뿐이다. `KeProcessorFeaturesBitMap` 도 쓰는 곳이 위 둘뿐이다. → **정적으로는 16바이트가 모두 0** `[정적, 반증 가능]` — 비트를 켜는 코드를 `_refs` 가 못 찾았을 뿐이라 (레지스터 기준 쓰기는 `_refs` 가 못 본다) 동적으로 한 번 확인해야 완전히 닫힌다.
- 실패 상태값 요약: 길이 `< 0x10` 또는 8의 배수 아님 → `0xc0000004`; 사용자 포인터 틀림 → `ProbeForWrite` 의 예외; 그 외 0.
- 닫힘: ntdll 쪽 **정적으로 닫힘**. nt 쪽 "값" 은 **동적 필요 (작게)**: `dq nt!KeProcessorFeaturesBuffer L2` 와 `dq ntdll!RtlProcessorFeaturesBuffer L2` (ntdll `+0x2a` 직후).

### 질문 5. `LdrpAllocateSchedulerSharedData` (rva `0xf96b0`, 217B)

근거: `LdrpAllocateSchedulerSharedData.asm`, `LdrpAcquireSchedulerSharedDataSlot.asm`, `LdrpInitializeProcess.asm`, 커널 `NtSetInformationProcess.asm` (+ `PspSchedulerSharedDataRegionCreate.asm` 호출 목록).

- **부르는 조건** (`LdrpInitializeProcess` 안): `UseWOW64 == 0` 이면 (`+0x176d JZ → +0x1806`) → `+0x1806 LdrpInitializeCfgScpHelpers` → 목록 머리 초기화 (`+0x180b` `LdrpSchedulerSharedDataListHeadLock = 0`, `+0x1812 ~ +0x1820` `LdrpSchedulerSharedDataListHead` 의 Flink·Blink = 머리 자신) → `+0x1827/+0x182c` 이 함수. 반환값 안 봄. 이어서 `+0x1837/+0x183c LdrpAcquireSchedulerSharedDataSlot` (TEB 칸을 쓰는 쪽, 아래).
- **인자**: 없음. **반환**: NTSTATUS — 0 / `0xc00000bb` (`LdrpIsSecureProcess != 0`, `+0xc` `+0x13` `+0xbd`) / `NtSetInformationProcess` 실패 상태 그대로 / `0xc000009a` (힙 할당 실패, `+0xc4`).
- **하는 일**:
  - `+0x2c/+0x31` **`NtSetInformationProcess`** (스텁 `0x1c`): `RCX = -1` (현재 프로세스, `+0x24`), **`EDX = 0x70` (클래스 `0x70`, `+0x28 LEA EDX,[R9+0x68]`, R9=8)**, `R8 = &지역[RSP+0x30]` (0 으로 시작, **출력으로 쓰임**), `R9D = 8` (`+0x19`). 이 호출이 **핸들을 돌려준다**.
  - `+0x57/+0x5c` `RtlAllocateHeap(LdrpHeap, NtdllBaseTag + 0x40000 \| 8, 0x18)` (`+0x3b` `+0x41` `+0x47` `+0x4e` `+0x54`, 플래그 `0x8` = 0 으로 채움). 실패면 `0xc000009a` + `NtClose([RSP+0x30])` (`+0xd3/+0xd8`).
  - 노드 `0x18` 바이트: `+0x10` = 받은 핸들 (`+0x64` `+0x70`). `+0x74/+0x79` `RtlAcquireSRWLockExclusive(LdrpSchedulerSharedDataListHeadLock)`, 목록 머리 뒤에 끼워 넣기 (`+0x79 ~ +0xa6`: `Flink`/`Blink` · 머리 `Flink->Blink == &머리` 검사 `+0x87` 틀리면 `INT 0x29` `ECX=3`), `+0xad/+0xb2` `RtlReleaseSRWLockExclusive`. 반환 0.
- **커널 쪽 (클래스 `0x70`)**: `NtSetInformationProcess` 의 점프표 (`+0x25e ~ +0x284`, 표 `0x935800`/`0x935914`) 가 클래스 `0x70` 을 `+0xec4` 로 보낸다 (표 값을 파일에서 읽어 `0x140933874 = +0xec4` 로 확인). 길이가 정확히 8 이어야 하고 (`+0xec4 CMP R13D,8`), 프로세스 핸들 접근 `0x200` 으로 참조 (`+0xf02/+0xf07`), **`PspSchedulerSharedDataRegionCreate`** (`+0xf17/+0xf1c`) 가 객체 · 섹션 (`MmCreateSectionEx`) · 시스템 매핑 · 프로세스 매핑 (`MmMapViewOfSection`) · 핸들 삽입 (`ObInsertObjectEx`) 을 해서 핸들을 만들고, 성공하면 사용자 버퍼에 쓴다 (`+0x13ea ~ +0x13ef`). → run44 사진의 "2쪽짜리 매핑 섹션" 의 출처.
- **TEB `+0x1850` 을 쓰나**: **이 함수는 안 쓴다** (`GS:` 접근 자체가 없음). TEB `+0x1850` 의 PDB 이름은 **`_TEB.SchedulerSharedDataSlot`**. 쓰는 줄은 바로 다음 호출 **`LdrpAcquireSchedulerSharedDataSlot +0x92`** (`MOV [RDI+0x1850],RAX`): `NtSetInformationThread(RCX = -2, EDX = 0x39, R8 = &버퍼, R9D = 0x18)` (`+0x84/+0x89`) 에 위 노드의 핸들 (`[RBX+0x10]`, `+0x77`) 을 넣어 슬롯 주소를 돌려받아 쓴다 (커널 `NtSetInformationThread +0xc2c/+0xc31 → PspSchedulerSharedDataRegionSlotAllocate`, baggage.md 1344 줄과 같은 길). → **run44 의 `TEB+0x1850 ≠ 0` 은 `LdrpInitializeProcess +0x1837/+0x183c` 호출 직후 (`LdrpAcquireSchedulerSharedDataSlot +0x92`)** 에 생긴다. 건너뛰는 조건: 목록이 비었거나 (`+0x2d` `+0x30`) `PEB.NtGlobalFlag2` 비트 4 (`+0x40 TEST [RAX+0x7c4],0x10`).
- 닫힘: **정적으로 닫힘**. 동적 확인(선택): `LdrpAllocateSchedulerSharedData +0x31` 직후 `[RSP+0x30]` 핸들 값, `+0xb2` 직후 `dq ntdll!LdrpSchedulerSharedDataListHead L2`.

### 질문 6. TLS 묶음

근거: `LdrpInitializeTls.asm` (이미 있음), `LdrpInitializeTlsHeap.asm`, `LdrpGetNewTlsVector.asm`, `LdrpAllocateTls.asm`, `LdrpAllocateTlsEntry.asm`, `RtlSetBits.asm`, `RtlClearBits.asm`, `_refs/0x1d26c8.tsv` `0x1d26e0.tsv`, `sample/bin/test.exe` 의 TLS 디렉터리, 사진 P1a.

**`LdrpInitializeTls` (rva `0xabca8`) 의 흐름 (test.exe)**
1. `+0x12/+0x19` `LdrpActiveThreadCount != 0` 이면 0 돌려주고 끝 (첫 호출은 0).
2. `+0x1f/+0x24` `LdrpInitializeTlsHeap` — 음수면 그대로 반환.
3. `+0x36 ~ +0x10d` `PebLdr.InLoadOrderModuleList` (`0x1d2890`) 을 돈다. 각 항목: `LoadReason (+0x10c) == 9` 이면 건너뜀 (`+0x5d` `+0x64`) — **ntdll 항목** (`LdrpInitializeNtdllDataTableEntry +0x220` 이 `R14D = 9` 를 씀, `+0xe0`). `RtlImageNtHeaderEx(3, DllBase, 0, &nt)` (`+0x76/+0x7b`), `Magic == 0x20b` 아니면 건너뜀 (`+0x85`), `RtlImageDirectoryEntryToData(…, MappedAsImage=1, 디렉터리 9 = TLS)` (`+0x9c/+0xa1`) 가 NULL 이면 건너뜀, `LdrpLogInternal` (`+0xdc`), **`LdrpAllocateTlsEntry(RCX = TLS 디렉터리, RDX = 항목, R8 = &개수(지역 [RSP+0x60]), R9 = 0)`** (`+0xf5/+0xfa`), 성공하면 `LDR.TlsIndex (+0x6e) \|= 0xffff` (`+0x102`).
   - test.exe 길에서 이 시점 목록에 있는 건 **ntdll (건너뜀) 과 exe** 뿐이다. kernel32 · dll1 · dll2 는 한참 뒤 `LdrpInitializeProcess +0x1aa4 LdrpMapAndSnapDependency` 에서 올라오고 그쪽은 `LdrpHandleTlsData` 가 맡는다 (baggage.md 554 줄과 같은 말). → **개수 `N = 1`** `[정적]`. (`test.exe` TLS 디렉터리: 원본 데이터 1바이트, 0채움 0, 정렬 코드 `1` (=1바이트), 인덱스 변수 `0x140082ce0`, 콜백 목록 `0x14006e380`. `sample/bin/test.exe` 기준 — VM 파일이 같다는 가정.)
   - `LdrpAllocateTlsEntry` 안 (인자 확인에 필요한 만큼만): `RtlAllocateHeap(LdrpTlsHeap, NtdllBaseTag + 0xc0000, 0x48)` 으로 `LdrpTlsList` 항목을 만들고, TLS 디렉터리 `0x28` 바이트를 `+0x10` 에 복사, `*AddressOfIndex = 지금 개수` (R9 == 0 이라 `*R8` 을 쓰고 1 올림 `+0x9e ~ +0xaf`) → exe 의 `_tls_index` 변수가 0, `+0x40` 항목 인덱스 = 0, `LdrpTlsList` (rva `0x1cc938`) 꼬리에 연결 (`+0x103 ~ +0x10d`, 연결 검사 틀리면 `INT 0x29`).
4. `+0x10d ~ +0x15d` 목록 끝: `EDI = N`. `N == 0` 이면 `LdrpTlsBitmap` 을 0 으로 (`+0x167` `+0x16e`) 하고 `LdrpAllocateTls`. `N != 0` 이면 `ESI = N + 8` (`+0x115`, **여분 8비트**), `ESI > 0x20` 이면 힙에서 `((ESI+0x1f)>>5)*4` 바이트 벡터를 받음 (`+0x178 ~ +0x1a2`, 실패 `0xc0000017`), 아니면 안쪽 32비트 칸 **`LdrpStaticTlsBitmapVector`** (rva `0x1d26d4`) 를 씀 (`+0x11d`).
   - 쓰는 칸: `LdrpActualBitmapSize` (rva `0x1d26d8`, qword) = 1 (안쪽 칸 쓸 때, `+0x127`) 또는 dword 개수 (힙 벡터일 때), `LdrpTlsBitmap.SizeOfBitMap` (rva `0x1d26e0`) = `N + 8` (`+0x130`), `LdrpTlsBitmap.Buffer` (rva `0x1d26e8`) = 벡터 주소 (`+0x13d`).
   - **`RtlSetBits(&LdrpTlsBitmap, 시작 = 0, 개수 = N)`** (`+0x144/+0x149`, `EDX=0` `+0x12e`, `R8D=EDI` `+0x124`), **`RtlClearBits(&LdrpTlsBitmap, 시작 = N, 개수 = 8)`** (`+0x158/+0x15d`, `R8D=8` `+0x149`, `EDX=EDI` `+0x156`). 그 뒤 `LdrpAllocateTls()` 호출 (`+0x15d/+0x162`) 의 결과를 반환.
   - **test.exe 의 값**: `N = 1` → `SizeOfBitMap = 9`, `Buffer = &LdrpStaticTlsBitmapVector`, `LdrpActualBitmapSize = 1`, `RtlSetBits(0, 1)`, `RtlClearBits(1, 8)`. 결과 비트열: 바이트 0 = `0x01`, 나머지 0 (켜진 비트 = 0번 하나, "정적 TLS 로 쓰는 중"; 1 ~ 8번은 "TlsAlloc 식 동적 구역 여분").

**`RtlSetBits` (rva `0xa8460`, 158B) · `RtlClearBits` (rva `0xac290`, 157B)** — 인자 `RCX = 비트맵 머리`, `EDX = 시작 비트`, `R8D = 개수`, 반환 없음. 개수 0 이면 바로 돌아옴 (`+0x0` `+0x3`). 바이트 단위로 `Buffer + (시작 >> 3)` 에 마스크를 `OR` (`RtlSetBits`) / `AND` (`RtlClearBits`): 한 바이트 안에 다 들어가면 (`시작&7 + 개수 <= 8`) 한 번, 아니면 앞 반 바이트 · 가운데 `memset(0xff / 0)` · 뒤 반 바이트. 마스크 표 두 개를 **ntdll 이미지 안 `.rdata` 에서 읽는다** (`LEA RBP,[0x180000000]` 로 이미지 베이스를 잡고 `[RSI+RBP+rva]`): 표 `0x192548` = `00 01 03 07 0f 1f 3f 7f ff` (아래에서 `n` 비트), 표 `0x180558` = `ff fe fc f8 f0 e0 c0 80 00` (위에서 `8-n` 비트). `RtlClearBits(1, 8)` 은 `시작&7 + 8 = 9 > 8` 이라 둘로 갈라진다: 바이트 0 은 표 `0x192548[1] = 0x01` 로 AND (비트 1~7 지움, `+0x58 ~ +0x63`), 바이트 1 은 표 `0x180558[1] = 0xfe` 로 AND (비트 8 지움, `+0x94 ~ +0x9c`). syscall 없음. (PE_emulator 덫 후보: 이 두 표 읽기가 `이미지 베이스 + rva` 라서, 에뮬레이터에서 ntdll `.rdata` 가 그 자리에 올라와 있어야 한다.)

**`LdrpInitializeTlsHeap` (rva `0xabe6c`, 174B)** — 인자 없음, 반환 NTSTATUS. `+0x13 CMP [LdrpHeapPartitionName+8],0` (`+0x2a JNZ → +0x55`):
- **비어 있으면** (`LdrpHeapPartitionName.Buffer == NULL`): `LdrpTlsHeap` (rva `0x1d26c8`) = `PEB.ProcessHeap` (`+0x2c` `+0x35` `+0x39`), 반환 0. 새 힙 안 만듦, syscall 없음.
- 아니면 (`+0x55 ~`): `RtlpCreateHeap(ECX = 2, …)` (`+0x71/+0x76`, 실패 `0xc0000017`) 로 별도 힙을 만들고 `LdrpTlsHeap` 에 저장 (`+0x7d`), `RtlSetHeapInformation(힙, 클래스 7, "TLSHeap" 문자열 8B)` (`+0x9b/+0xa0`).
- test.exe: `ProcessParameters.HeapPartitionName` (`PP+0x420`) 이 사진 P1a 에서 두 qword 모두 0 `[관찰]` → `LdrpInitializeProcessHeap +0x11f` 가 그 값을 `LdrpHeapPartitionName` 으로 복사하므로 `Buffer == NULL` → **프로세스 힙을 그대로 TLS 힙으로 쓴다**.

**`LdrpGetNewTlsVector` (rva `0xa3e0c`, 100B)** — `ECX = 슬롯 수`, 반환 `RAX = 벡터` (0 = 실패). `RtlAllocateHeap(LdrpTlsHeap, NtdllBaseTag + 0xc0000, 0x10 + 슬롯 * 8)` (`+0x2c/+0x31`), 블록 맨 앞 dword = 슬롯 수 (`+0x41`), `memset(블록 + 0x10, 0, 슬롯 * 8)` (`+0x49/+0x4e`), 반환 = 블록 `+ 0x10`. 부르는 곳 `LdrpAllocateTls +0x4d/+0x52` (인자 = `LdrpTlsBitmap.SizeOfBitMap`, test.exe 는 9 → 힙 `0x58` 바이트). `LdrpAllocateTls` 는 그 벡터 슬롯 `[인덱스]` 에 항목마다 힙 블록 (정렬해서 앞에 원래 포인터를 숨김) 을 걸고 데이터를 복사한 뒤, **`TEB+0x58 = 벡터`** (`+0x1d9 ~ +0x1e2`) 와 `LdrpActiveThreadCount++` (`+0x1e5`) 를 한다. 사진 `[관찰]`: `TEB+0x58 = 0x25996f383e0`, 슬롯 0 = `0x25996f38440` (16 정렬) — 위와 맞음.

- 닫힘: **정적으로 닫힘** (사진 일치). 동적 확인(선택): `dd ntdll!LdrpTlsBitmap L1` = 9, `dq ntdll!LdrpTlsBitmap+8 L1` 이 `ntdll!LdrpStaticTlsBitmapVector`, `db ntdll!LdrpStaticTlsBitmapVector L2` = `01 00`.

### 질문 7. `LdrpValidateEntrySection` (rva `0xe9fe0`, 75B) · `LdrpIsImageAddress` (rva `0x15eaf8`, 91B)

- **`LdrpValidateEntrySection(RCX = LDR 항목)`** → `AL` BOOLEAN. `RtlImageNtHeaderEx(3, [RCX+0x30] (DllBase), 0, &nt)` (`+0x22/+0x27`), `EAX = nt.OptionalHeader.AddressOfEntryPoint` (`[RDX+0x28]`, `+0x2c`). 진입점이 0 이면 TRUE (`+0x31 JNZ` 안 탐 → `+0x33`), 항목의 `EntryPoint` (`+0x38`) 가 0 이어도 TRUE (`+0x3c` `+0x41 JZ → +0x33`), 진입점이 `SizeOfHeaders` (`[RDX+0x54]`) 이상이면 TRUE (`+0x43` `+0x46 JNC → +0x33`), **헤더 안을 가리키면 FALSE** (`+0x48`). syscall 없음, 쓰는 칸 없음. 부르는 곳 `LdrpProcessMappedModule +0x83/+0x88` (모듈마다 한 번. 그 앞에서 조건이 맞으면 `EntryPoint = DllBase + AddressOfEntryPoint` 를 `+0x78` 에 써 둠. FALSE 면 `+0x8a JZ → +0x22e` 에서 `0xc000007b`). ntdll (진입점 0) · exe (`0x1d40 >= 0x400`) 둘 다 TRUE.
- **`LdrpIsImageAddress(RCX = 주소, RDX = BOOL* 출력)`** → NTSTATUS. `NpQueryVirtualMemory(-1, 주소, 클래스 0, &MEMORY_BASIC_INFORMATION(0x30B 지역), 0x30, NULL)` (`+0x1f` `+0x18` `+0x2b` `+0xf` `+0x23` `+0x1b`, 호출 `+0x3a/+0x3f`). `NpQueryVirtualMemory` 는 `NtQueryVirtualMemory` 와 **같은 syscall 번호 `0x23` 인 두 번째 스텁** (rva `0x160940`; PDB 이름이 따로 있고 진짜 스텁은 rva `0x161210`). 성공하면 `*RDX = (Type == 0x1000000 /* MEM_IMAGE */)` (`+0x43` `+0x4b` `+0x4e`), 실패하면 출력을 안 쓰고 상태 반환 (`+0x41 JS → +0x50`).
  - 부르는 곳과 조건: `LdrpProcessMappedModule +0x27b/+0x280` 은 `RtlpInsertOrRemoveScpCfgFunctionTable` 이 **`0xc0000141`** 을 돌려줬을 때만 (`+0x203 CMP EAX,0xc0000141` · `+0x208 JZ → +0x26e`) — run63 의 test.exe 길은 0 / `0x117` 이라 안 탄다. `LdrpQueryCurrentPatch +0x9b/+0xa0` 은 `NtManageHotPatch` 가 `0xc0000018` 을 돌려줬을 때만 (`+0x5d` `+0x62`). → **test.exe 기본 길에서 `LdrpIsImageAddress` 는 안 불린다** `[정적]`.
- 닫힘: **정적으로 닫힘**.

### 질문 8. `RtlRbInsertNodeEx` (rva `0x387a0`, 1692B)

근거: `RtlRbInsertNodeEx.asm`, `LdrpInsertModuleToIndexLockHeld.asm`, PDB `_RTL_BALANCED_NODE` / `_RTL_RB_TREE`.

- **시그니처**: `RtlRbInsertNodeEx(RCX = _RTL_RB_TREE*, RDX = 부모 노드 (빈 트리면 NULL), R8B = 오른쪽이면 1 (왼쪽 0), R9 = 새 노드)`. 반환 없음.
- **칸 (PDB)**: `_RTL_BALANCED_NODE` (0x18B): `+0x0` `Left`, `+0x8` `Right`, `+0x10` `ParentValue` — 낮은 2비트 = 색 (`Red`, 비트 0) / 균형 값, 나머지 = 부모 포인터. `_RTL_RB_TREE` (0x10B): `+0x0` `Root`, `+0x8` `Min` (낮은 비트 0 = `Encoded` 표시).
- **공통 첫 줄** (`+0x9 ~ +0x24`): `새 노드.Left = 새 노드.Right = 0` (`+0xf` `+0x15`), `DIL = 트리.Min & 1` (Encoded). 이 트리 둘 (`LdrpMappingInfoIndex` `0x1d2430`, `LdrpModuleBaseAddressIndex` `0x1d2420`) 은 BSS 꼬리라 0 으로 시작 → Encoded 꺼짐 (포인터를 그대로 씀).
- **① 첫 노드 (빈 트리, `RDX == NULL`, `+0x24 TEST` · `+0x27 JNZ → +0x59` 안 탐)**: `Root = 새 노드` (`+0x39`), `Min = 새 노드` (`+0x47`; Encoded 트리면 `+0x41 JNZ → +0x42c` 가 `(새 노드 ^ 트리) \| 1` 로 씀), `새 노드.ParentValue = 0` (`+0x4b`: 부모 없음 · 검정) → 끝. 회전·색칠 없음.
- **② 둘째 노드 (부모 = 뿌리 하나)**:
  - `부모.Children[R8B] = 새 노드` (`+0x6c`), `새 노드.ParentValue = 부모 \| 1` (`+0x79` `+0x7d`: 부모 포인터 + **빨강**).
  - 오른쪽 (`R8B != 0`): `+0x84 JZ` 안 탐 → `+0x86` 에서 레지스터 저장 → `+0x9a` **부모의 색 비트 검사**: 뿌리는 검정 (`ParentValue` 낮은 비트 0) → `+0x9f TEST CL,1` · `+0xa2 JNZ` 안 탐 → `+0xa4 ~ +0xb8` 복원 후 끝. **회전 없음.**
  - 왼쪽 (`R8B == 0`, `+0x84 JZ → +0xba`): `RAX = 트리.Min` (`+0xba`), `Min & 1` 이 0 이면 (`+0xbe` `+0xc0`) `부모 == Min` 인지 (`+0xc9` `+0xcc`) → 같으면 **`Min = 새 노드`** (`+0xd6`), 그 뒤 위 오른쪽과 같은 `+0x86` → `+0x9a` 부모 색 검사 (검정) → 끝. **회전 없음.**
- **그 밖 (이름·조건만)**: 부모가 **빨강** (`+0x9f ~ +0xa2` 에서 `+0xdc` 로) 이면 위로 올라가며 (a) 삼촌 (부모의 형제) 이 빨강 → 부모·삼촌 검정 / 할아버지 빨강으로 다시 칠하고 (`+0x156 ~ +0x18d`) 할아버지로 올라가 반복 (`+0x1a6 JMP → +0x9a`), (b) 삼촌이 검정 또는 NULL → 한 번 또는 두 번 회전 (`+0x1ec ~ +0x317` 쪽의 부모 포인터 갈아끼우기: 자식 칸 `Left`/`Right` 교환, `ParentValue` 의 낮은 2비트 보존 `AND 3` + 새 부모 `OR`, 뿌리가 바뀌면 `트리.Root` 갱신), 불변식이 깨진 것을 보면 빠른 실패 `INT 0x29` `ECX = 0x1d` (`+0x311/+0x316`). 세부는 안 봄.
- **test.exe 길에서 불리는 순서** (둘 다 트리 둘에 각각 한 번씩, `LdrpInsertModuleToIndexLockHeld` 의 `+0x91/+0x96` (`LdrpMappingInfoIndex`, 노드 = 항목 `+0xe0`) 과 `+0xe7/+0xec` (`LdrpModuleBaseAddressIndex`, 노드 = 항목 `+0xc8`)):
  1. **ntdll**: `LdrpInitializeNtdllDataTableEntry +0x111/+0x116 → LdrpInsertModuleToIndex` → 두 트리 모두 빈 상태 → ① 갈래.
  2. **exe**: `LdrpInitializeProcess +0x14cd/+0x14d2 → LdrpInsertModuleToIndex` → 부모 = ntdll 노드. 방향은 `LdrpInsertModuleToIndexLockHeld` 의 키 비교가 정함: `LdrpMappingInfoIndex` 는 (`TimeDateStamp` → `SizeOfImage`) 순으로 새 값이 작으면 왼쪽 (`+0x56 ~ +0x69`), `LdrpModuleBaseAddressIndex` 는 `DllBase` 가 작으면 왼쪽 (`+0xc2 ~ +0xc9`). 파일 값: ntdll `TimeDateStamp 0x6bdf03ca` > exe `0x6ad22b72` (`sample/bin/test.exe` 기준) → 첫 트리에서 exe 는 **왼쪽** (`R8B = 0`). 둘째 트리는 `DllBase` 크기 비교인데 exe 베이스가 ntdll (`0x7ffa…`) 보다 낮을 거라 **왼쪽일 가능성이 높다 `[추측]`** (ASLR 이라 값은 실행마다 다름 → 동적 확인). 왼쪽이고 부모가 `Min` (= ntdll 노드) 이라 `Min = exe 노드` 로 바뀐다 (`+0xd6`).
- 닫힘: ①·② 갈래는 **정적으로 닫힘**. 둘째 트리의 방향 비트(`R8B`)는 **동적 필요 (작게)**: `RtlRbInsertNodeEx` 입구에서 `rcx, rdx, r8b, r9`.

### 질문 9. `LdrpFindKnownDll` (rva `0x9c70`, 469B) · `LdrpLoadKnownDll` (rva `0x9970`, 320B)

근거: 두 `.asm`, `LdrpAllocateUnicodeString.asm`, `_refs/0x1d2770.tsv` `0x1e7120.tsv`.

**`LdrpFindKnownDll(RCX = 찾을 이름 ustr*, RDX = 기본 이름 out, R8 = 전체 경로 out, R9 = 섹션 핸들 out)`** → NTSTATUS.
- `+0x64` `+0x93` 두 번 `LdrpLogInternal`. `+0x98 ~ +0xa2` **`LdrpKnownDllDirectoryHandle` (rva `0x1e7120`, `.mrdata`) 가 0 이면** 바로 `0xc0000135` (`+0xea`) — `\KnownDlls` 를 못 연 프로세스 (예: 보호 프로세스).
- `+0xd4/+0xd9` **`NtOpenSection`** (스텁 `0x37`): `RCX = 섹션 핸들 out`, **`EDX = 0xd`** (`SECTION_QUERY \| MAP_READ \| MAP_EXECUTE`), `R8 = OBJECT_ATTRIBUTES {길이 0x30, Root = LdrpKnownDllDirectoryHandle, 이름 = 첫 인자, 속성 0x40 (대소문자 무시)}` (`+0xa7 ~ +0xc6`).
- 실패하면 상태 `STATUS_OBJECT_NAME_NOT_FOUND (0xc0000034)` 는 `0xc0000135` (`STATUS_DLL_NOT_FOUND`) 로 바꿔 (`+0xe3` `+0xe8` `+0xea`) 반환 (`LdrpLogInternal` 두 번 `+0x118` `+0x146`). 그 밖 상태는 그대로.
- 성공하면 (`+0x169`): `LdrpAllocateUnicodeString(R8 out, LdrpKnownDllPath.Length + 2 + 이름.Length)` (`+0x16c ~ +0x180`; 안에서 `NtdllpAllocateStringRoutine` = 프로세스 힙 할당), `RtlAppendUnicodeStringToString(out, &LdrpKnownDllPath)` (`+0x190/+0x195`), `RtlAppendUnicodeToString(out, L"\")` (`+0x19f/+0x1a4`), 이름 붙이기 (`+0x1b2/+0x1b7`), `RtlInitUnicodeStringEx(RDX out, 전체 경로 안의 파일 이름 시작)` (`+0x1bd/+0x1c2`), 반환 0. 문자열 할당이 실패하면 `NtClose(섹션 핸들)` (`+0x1cc/+0x1d1`) 후 그 상태.
- 쓰는 전역: 없음 (읽기만: `LdrpKnownDllDirectoryHandle` `LdrpKnownDllPath`). 호출자 소유 칸 셋이 채워진다.

**`LdrpLoadKnownDll(RCX = 로드 문맥)`** → NTSTATUS. (`RDI = [RCX+0x38]` = 모듈 항목, 항목 `+0x48` `FullDllName` · `+0x58` `BaseDllName` · `+0x108` 해시 · `+0x30` `DllBase` 를 씀.)
- `+0x9` `+0x23 ~ +0x2d` 문맥 플래그 `[RCX+0x20]` 의 비트 9 가 켜졌으면 `LdrpCheckKnownDllFullPath(문맥, &지역 이름)` (`+0x32/+0x37`) 가 AL 로 가부 결정. 꺼졌으면 비트 5 (`0x20`) 가 켜지고 비트 3 (`8`) 이 꺼진 때만 가 (`+0x122 ~ +0x13d`). 불가면 `0xc0000135` (`+0x113`).
- `+0x5e/+0x63` `LdrpFindKnownDll(이름 = 비트 9 길이면 지역 이름, 아니면 문맥 첫 칸, &BaseDllName, &FullDllName, &섹션 핸들)`. 음수면 반환. 성공하면 `LdrpLogDllState` (`+0x78`), `LdrpHashUnicodeString(BaseDllName)` → 항목 `+0x108` (`+0x86/+0x8b`, `+0x92`), `LdrpModuleDatatableLock` 독점 잡고 (`+0x98`) `LdrpFindLoadedDllByNameLockHeld` (`+0xb7/+0xbc`) 로 이미 올라온 모듈 찾고 풀기 (`+0xc5`).
  - 이미 있으면 `LdrpLoadContextReplaceModule` (`+0x130/+0x135`), 없으면 `LdrpLogDllState` (`+0xdf`) 후 **`LdrpMapDllWithSectionHandle(문맥, 섹션 핸들)`** (`+0xec/+0xf1`). 어느 쪽이든 끝에 **`NtClose(섹션 핸들)`** (`+0xf8/+0xfd`).
- 부르는 곳 (요청서): `LdrpLoadDependentModuleInternal +0x178d`, `LdrpFindOrPrepareLoadingModule +0x5e0`.
- **`LdrpKnownDllPath` (rva `0x1d2770`, 16B `UNICODE_STRING`)** 은 `LdrpInitializeProcess +0xe64 ~ +0xe72` 가 쓴다 (질문 10). 읽는 쪽 `LdrpFindKnownDll +0x16c` `LdrpCheckKnownDllFullPath +0x4 +0x79 +0xa3`.
- test.exe 길에서 `NtOpenSection` 몇 번: 로더가 `LdrpLoadKnownDll` 로 보내는 모듈 수만큼 (KnownDlls 에 있는 kernel32 · KernelBase 등이 후보, dll1 · dll2 는 `\KnownDlls` 에 없어 `0xc0000034` → `0xc0000135` 로 돌아가 일반 경로로 간다 `[추측]`). 정확한 횟수·이름은 동적.
- 닫힘: 본문은 **정적으로 닫힘**. 호출 횟수·이름·핸들 값은 **동적 필요**.

### 질문 10. `\KnownDlls` 스텁 셋과 `LdrpInitializeProcess` 호출 자리

근거: 세 스텁 `.asm` (`MOV R10,RCX` / `MOV EAX,번호` / `SYSCALL` 24바이트 모양), `LdrpInitializeProcess.asm` +0xbc ~ +0xce, +0xd54 ~ +0xf0c, `.c` 140 줄 (`L"\\KnownDlls"`).

| 스텁 | rva | syscall 번호 | 부르는 자리 | 인자 | 결과를 쓰는 곳 |
|---|---|---|---|---|---|
| `NtOpenDirectoryObject` | `0x1618b0` | **`0x58`** (`+0x3`) | `LdrpInitializeProcess +0xdb8/+0xdbd` | `RCX = &LdrpKnownDllDirectoryHandle` (rva `0x1e7120`, `+0xdb1`) · `EDX = 3` (`DIRECTORY_QUERY \| TRAVERSE`, `+0xdae`) · `R8 = &OBJECT_ATTRIBUTES` (`+0xda6`): 길이 `0x30`, Root 0, 이름 **`\KnownDlls`** (`UNICODE_STRING` 길이 `0x14` / 최대 `0x16`, `+0xbc` `+0xce` 에서 만든 `[RSP+0x190]`), 속성 `0x40` | `LdrpKnownDllDirectoryHandle` (.mrdata 라서 호출 앞뒤로 `LdrProtectMrdata(0)` `+0xda1` / `(1)` `+0xdc4`) |
| `NtOpenSymbolicLinkObject` | `0x1634a0` | **`0x138`** | `+0xe47/+0xe4c` | `RCX = &링크 핸들 (지역 [RSP+0x110])` · `EDX = 1` (`SYMBOLIC_LINK_QUERY`) · `R8 = OBJECT_ATTRIBUTES`: Root = `LdrpKnownDllDirectoryHandle` (`+0xe00`), 이름 = **`KnownDllPath`** (`UNICODE_STRING` rva `0x174cf0`, 길이 `0x18`), 속성 `0x40` | 지역 핸들 |
| `NtQuerySymbolicLinkObject` | `0x163b00` | **`0x16b`** | `+0xe90/+0xe95` | `RCX = 링크 핸들` · `RDX = &LdrpKnownDllPath` · `R8 = &돌아온 길이 [RSP+0x88]` | `LdrpKnownDllPath` (아래) |

- **`LdrpKnownDllPath` 쓰는 순서**: 처음 버퍼 크기 `0x30` (`+0xd65 LEA EBX,[RAX+0x2e]`) 으로 `RtlAllocateHeap(PEB.ProcessHeap, 0, 크기)` (`+0xec0`), `Length = 0` · `MaximumLength = 크기` · `Buffer = 힙` (`+0xe64 +0xe6b +0xe72`) → `NtQuerySymbolicLinkObject` 가 `Length` 를 대상 길이로 채움. `0xc0000023` (`STATUS_BUFFER_TOO_SMALL`) 이면 힙 블록 해제 (`+0xeac`), 돌아온 길이로 다시 할당해 반복 (`+0xea2 JNZ`, `+0xeb1`). 성공하면 `NtClose(링크 핸들)` (`+0xf04/+0xf09`).
- **실패 길**: `NtOpenDirectoryObject` 가 음수면 `LdrpLogInternal` (줄 번호 `0x1993`) 후 오류 종료 (`+0xdf4 JMP → +0x564`), 링크 열기 실패는 `0x19aa` (`+0xe54 ~ +0xe62`), 질의 실패 (BUFFER_TOO_SMALL 외) 는 `0x19cd` (`+0xed4 ~ +0xef7`).
- **건너뛰는 조건**: `+0xd54 TEST [RDI+3],AL(2)` · `+0xd57 JNZ → +0xf0c` — `PEB.IsProtectedProcess` 가 켜지면 이 네 호출 (`LdrpKnownDllDirectoryHandle` 포함) 을 통째로 안 한다. test.exe 는 `BitField = 0x4` 라 한다.
- 사진 (P1a 1373 · 1386 줄): `\KnownDlls` 객체 디렉터리와 `KnownDllPath` 심볼릭 링크 존재 `[관찰]`. 링크 대상 문자열 (`C:\WINDOWS\System32` 로 짐작) 과 핸들 값은 사진에 없음 → 동적.
- 닫힘: 번호 · 인자 · 순서 **정적으로 닫힘**. 핸들 값 · 링크 대상 문자열은 **동적 필요 (작게)**.

### 질문 11. `LdrpIllegalCWDDevices` (rva `0x1d23f4`)

- 위 질문 2 에서 정리: **4바이트 마스크** (장치 특성 비트), 파일 값 0 (BSS 꼬리). 쓰는 곳 (`_refs/0x1d23f4.tsv`): `LdrpQueryIllegalCWDDevices +0x40` (`0` 또는 `0x10`), `+0x85` (`0x2000`), `+0x91` (`-1`) · `LdrpCheckAppDirType +0x11b` (`0`). 읽는 곳: `LdrpLoadDependentModuleInternal +0x1649`, `LdrpGetFullPath +0x7c`, `LdrpPreprocessDllName +0x6cd`, `LdrpInitializeProcess +0x1169`, `LdrpCheckAppDirType +0x10f`. 함수 범위 밖인 데이터라 PDB `--where` 는 `[none]` 이 맞다.
- **정적으로 닫힘**.

### 질문 12. 공용 부품 (한 줄씩)

| 함수 | 인자 · 쓰는 칸 · 반환 |
|---|---|
| `RtlGetNtSystemRoot` (`0x4cf20`, 53B) | 인자 없음. 반환 `PWSTR`: **`PEB.SharedData` (`PEB+0x90`) 가 NULL 이 아니고 그 `ServiceSessionId` (첫 dword) 가 0 이 아니면 `SharedData + 0x1e` (`_SILO_USER_SHARED_DATA.NtSystemRoot`, `+0x1c ~ +0x35`), 아니면 KUSD `NtSystemRoot` 주소 `0x7ffe0030`** (`+0x15`). 읽기만. 사진: `PEB+0x90 = 0` · KUSD `NtSystemRoot = "C:\WINDOWS"` `[관찰]` → `0x7ffe0030` |
| `RtlInitUnicodeStringEx` (`0xb7930`, 74B) | `RCX = out ustr`, `RDX = PCWSTR`. `out` 앞 8바이트 (`Length`·`MaximumLength`·패딩) 를 0 으로, `Buffer = RDX` 를 **먼저** 씀 (`+0x6` `+0x10`). `RDX == NULL` 이면 0 반환. `wcslen` (`+0x1c`) `> 0x7ffe` 면 `0xc0000106` (`Length` 는 0 인 채). 아니면 `Length = 2*len`, `MaximumLength = Length + 2`, 반환 0 |
| `RtlAppendUnicodeToString` (`0x4ce60`, 176B) | `RCX = 붙일 ustr`, `RDX = PCWSTR`. `RDX == NULL` 이면 0 (`+0x15`). `wcslen > 0x7ffe` 또는 `Length + 2*len > MaximumLength` 면 `0xc0000023` (`+0x9c`). 아니면 `memcpy(Buffer + Length, RDX, 2*len)` (`+0x60`), `Length += 2*len` (`+0x65`), 자리가 있으면 (`Length + 1 < MaximumLength`, `+0x6f ~ +0x73`) 끝에 NUL (`+0x7a`), 반환 0 |
| `RtlFreeUnicodeString` (PDB 이름 셋: `RtlFreeAnsiString` · `RtlFreeUnicodeString` · `RtlFreeUTF8String`, rva `0x4e200`, 35B) | `RCX = ustr*`. **`Buffer != NULL` 일 때만**: `RtlFreeHeap(PEB.ProcessHeap, 0, Buffer)` (`+0x12/+0x17` 의 호출 대상 rva `0x3d630` = PDB 이름 아홉 개 짝, 내용은 `PEB+0x30` 을 읽고 `RtlFreeHeap` 으로 꼬리 점프), 그리고 **ustr 16바이트를 모두 0 으로** (`+0x1a MOVUPS [RBX],XMM0` 직전 `+0x17 XORPS`). `Buffer == NULL` 이면 ustr 을 건드리지 않음 (`+0x10 JZ`) |

- **PE_emulator 실측 "덫 뒤 UNICODE_STRING 16B 가 0, 버퍼 첫 8B 바뀜" 과 코드**: **16B 가 0 이 되는 것은 코드와 맞다** (`+0x1a`). **버퍼 첫 8B 가 바뀌는 것은 `RtlFreeUnicodeString` 자신이 아니라 `RtlFreeHeap` 안의 일이다** (이 함수는 버퍼를 건드리지 않고 포인터만 넘김) → 정적으로 못 정함, 힙 관리자 몫 (run46). 그 8바이트짜리 버퍼는 질문 3 의 `C:\` 갈래 (`+0xf5e` `RtlAllocateHeap(힙, 0, 8)`) 에서 온 것이다.
- **닫힘**: 코드 줄은 **정적으로 닫힘**, "첫 8바이트" 는 **동적 필요 (선택)**: 그 버퍼 주소에 쓰기 감시점.

### 질문 13. `LdrControlFlowGuardEnforced` (rva `0xa6740`, 25B)

- 인자 없음, 반환 `EAX`. `LdrSystemDllInitBlock+0xb8` (qword, `CfgBitMap`) 이 0 이면 0 (`+0x0` `+0x8` `+0x17`). 아니면 `(~LdrSystemDllInitBlock+0x9c) & 1` (`+0xa` `+0x10` `+0x12`; `+0x9c` = `Flags`, 이름·뜻은 run32 의 init block 표). 쓰는 칸 · syscall 없음. test.exe 는 `+0xb8 = 0` (run63) → **0**. **정적으로 닫힘** (run63 / Q80 과 같은 결론).

## syscall 표 (이 의뢰의 함수들이 부르는 것)

| syscall (스텁 rva) | 번호 | 클래스 | 부르는 자리 (call/ret) | 조건 | test.exe 기본 길 횟수 |
|---|---|---|---|---|---|
| `NtOpenDirectoryObject` (`0x1618b0`) | `0x58` | — | `LdrpInitializeProcess +0xdb8/+0xdbd` | `PEB.IsProtectedProcess == 0` (`+0xd54/+0xd57`) | 1 |
| `NtOpenSymbolicLinkObject` (`0x1634a0`) | `0x138` | — | `LdrpInitializeProcess +0xe47/+0xe4c` | 위 + 디렉터리 열기 성공 | 1 |
| `NtQuerySymbolicLinkObject` (`0x163b00`) | `0x16b` | — | `LdrpInitializeProcess +0xe90/+0xe95` | 위 + 링크 열기 성공; `0xc0000023` 이면 다시 | 1 (대상 문자열이 `0x30` 바이트 이내일 때 `[추측]`) |
| `NtClose` (`0x160f90`) | `0xf` | — | `LdrpInitializeProcess +0xf04/+0xf09` (링크 핸들) | 위 | 1 |
| `NtOpenFile` (`0x161410`) | `0x33` | — | `RtlpCreateNewDirectoryReference +0xbb/+0xc0` (← `RtlpInitCurrentDir +0x32/+0x37` ← `LdrpInitializeProcess +0x1517/+0x151c`) | 경로 종류 `1·2·6·7` | 1 |
| `NtQueryVolumeInformationFile` (`0x1616d0`) | `0x49` | `4` (`FileFsDeviceInformation`) | `RtlpCreateNewDirectoryReference +0xf7/+0xfc` | `NtOpenFile` 성공 | 1 |
| `NtClose` | `0xf` | — | `RtlpCreateNewDirectoryReference +0x12b/+0x130` · `+0x1eb/+0x1f0` | 질의·길이 실패 길만 | 0 |
| `NtQuerySystemInformation` (`0x161470`) | `0x36` | **`0xfa`** | `RtlInitializeProcessorFeaturesBitMap +0x25/+0x2a` (← `LdrpInitializeProcess +0x1761/+0x1766`) | `LdrpIsSecureProcess == 0` | 1 |
| `NtSetInformationProcess` (`0x161130`) | `0x1c` | **`0x70`** | `LdrpAllocateSchedulerSharedData +0x2c/+0x31` (← `LdrpInitializeProcess +0x1827/+0x182c`) | `UseWOW64 == 0` (호출자, `+0x176d`), `LdrpIsSecureProcess == 0` | 1 |
| `NtClose` | `0xf` | — | `LdrpAllocateSchedulerSharedData +0xd3/+0xd8` | 힙 할당 실패 때만 | 0 |
| `NtOpenSection` (`0x161490`) | `0x37` | — | `LdrpFindKnownDll +0xd4/+0xd9` | `LdrpKnownDllDirectoryHandle != 0` | KnownDlls 로 보내는 모듈 수 (동적) |
| `NtClose` | `0xf` | — | `LdrpLoadKnownDll +0xf8/+0xfd` (섹션 핸들) · `LdrpFindKnownDll +0x1cc/+0x1d1` (문자열 할당 실패) | 성공 후 매번 / 실패만 | KnownDll 로 올릴 때마다 1 / 0 |
| `NpQueryVirtualMemory` (`0x160940`, `NtQueryVirtualMemory` 와 같은 번호) | `0x23` | `0` (`MemoryBasicInformation`), 길이 `0x30` | `LdrpIsImageAddress +0x3a/+0x3f` | 에러 길만 (질문 7) | 0 |
| `NtOpenFile` + `NtQueryVolumeInformationFile` + `NtClose` | `0x33` `0x49` `0xf` | `4` | `LdrpCheckAppDirType +0xe3/+0xe8` `+0x106/+0x10b` `+0x12b/+0x130` | `LdrpIllegalCWDDevices ∉ {0, -1}` | 0 |

(`LdrpInitializePolicy` · `LdrpIncludeAlternateForwarders` · `LdrpInitializeTls` 묶음 · `RtlRbInsertNodeEx` · `LdrpValidateEntrySection` · 공용 부품은 syscall 이 없다.)

## PE_emulator 용 작은 표 (함수 × 인자 · 쓰는 칸 · 하위 호출 · syscall · 반환)

| 함수 | 인자 | 쓰는 칸 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|
| `LdrpInitializePolicy` | 없음 | `LdrpAppPackagesPath`·`LdrpOriginalAppPackagesPath` 0 으로 (test.exe: 이미 0) | (test.exe) 없음. 조건부 `LdrSetDllDirectory`, `RtlQueryPackageClaims` ×5 | 없음 | void |
| `LdrpIncludeAlternateForwarders` | 없음 | 없음 | 없음 | 없음 | `AL` (PP.Flags `0x4000000` 또는 `PEB.NtGlobalFlag2` 비트 1) |
| `LdrpCheckAppDirType` | `RCX` = 이미지 경로 ustr | `LdrpIllegalCWDDevices` (조건부 0) | `RtlpDosPathNameToRelativeNtPathName` · `RtlFreeUnicodeString` | `NtOpenFile` · `NtQueryVolumeInformationFile`(4) · `NtClose` | void (test.exe 안 불림) |
| `RtlpInitCurrentDir` | `RCX` = 현재 디렉터리 ustr | `RtlpCurDirRef` · `PP.CurrentDirectory.{Handle,DosPath.Buffer,DosPath.Length}` | `RtlDetermineDosPathNameType_Ustr` · `RtlpCreateNewDirectoryReference` | (아래 함수가 냄) | NTSTATUS (`0` / `0xc000000d` / 아래 함수 상태) |
| `RtlpCreateNewDirectoryReference` | `RCX` 경로, `EDX` 최대길이, `R8` 레코드 out | 힙 레코드 `0x30 + 최대길이` | `RtlpDosPathNameToRelativeNtPathName` · `RtlFreeHeap` · `RtlAllocateHeap` · `memcpy` | `NtOpenFile`(`0x100020`,3,`0x21`) · `NtQueryVolumeInformationFile`(4) · (실패) `NtClose` | `0` / `0xc0000017` / `0xc0000106` / 하위 상태 |
| `RtlInitializeProcessorFeaturesBitMap` | 없음 | `RtlProcessorFeaturesBuffer`(16B, syscall 이 채움) · `RtlProcessorFeaturesBitMap{0x80, &버퍼}` | 없음 | `NtQuerySystemInformation(0xfa, 버퍼, 0x10, &ret)` | syscall 상태 (호출자는 안 봄) |
| `LdrpAllocateSchedulerSharedData` | 없음 | `LdrpSchedulerSharedDataListHead` (노드 `0x18B` 끼움) | `RtlAllocateHeap(LdrpHeap)` · `RtlAcquire/ReleaseSRWLockExclusive` | `NtSetInformationProcess(-1, 0x70, &핸들, 8)` · (실패) `NtClose` | `0` / `0xc00000bb` / `0xc000009a` / syscall 상태 |
| `LdrpInitializeTls` | 없음 | `LdrpTlsBitmap` · `LdrpStaticTlsBitmapVector` · `LdrpActualBitmapSize` · `LDR.TlsIndex`(0xffff) · (아래 호출이) `LdrpTlsList` · `TEB+0x58` · `LdrpActiveThreadCount` | `LdrpInitializeTlsHeap` · `RtlImageNtHeaderEx` · `RtlImageDirectoryEntryToData` · `LdrpLogInternal` · `LdrpAllocateTlsEntry` · `RtlSetBits` · `RtlClearBits` · `LdrpAllocateTls` · (N+8>32) `RtlAllocateHeap` | 없음 | `0` / `0xc0000017` / 하위 상태 |
| `LdrpInitializeTlsHeap` | 없음 | `LdrpTlsHeap` | (분기) `RtlpCreateHeap` · `RtlSetHeapInformation` | 없음 | `0` / `0xc0000017` / 하위 상태 |
| `LdrpGetNewTlsVector` | `ECX` 슬롯 수 | 힙 블록 (`+0` 슬롯 수, `+0x10 ~` 0 채움) | `RtlAllocateHeap(LdrpTlsHeap)` · `memset` | 없음 | `RAX` = 블록+0x10 / 0 |
| `RtlSetBits` / `RtlClearBits` | `RCX` 머리, `EDX` 시작, `R8D` 개수 | `Buffer` 안 비트 | `memset`(긴 구간) · ntdll `.rdata` 표 `0x192548`/`0x180558` 읽기 | 없음 | void |
| `LdrpValidateEntrySection` | `RCX` LDR 항목 | 없음 | `RtlImageNtHeaderEx` | 없음 | `AL` (진입점 0 / 항목 EP 0 / `>= SizeOfHeaders` 면 1) |
| `LdrpIsImageAddress` | `RCX` 주소, `RDX` BOOL* | `*RDX` (MEM_IMAGE 여부) | `NpQueryVirtualMemory` | `0x23` 클래스 0 | NTSTATUS |
| `RtlRbInsertNodeEx` | `RCX` 트리, `RDX` 부모, `R8B` 오른쪽?, `R9` 새 노드 | 새 노드 `Left`/`Right`/`ParentValue`, 부모 `Children[R8B]`, 트리 `Root`/`Min`, (회전 때) 이웃 노드 | 없음 (빠른 실패 `INT 0x29` ECX=0x1d) | 없음 | void |
| `LdrpFindKnownDll` | `RCX` 이름, `RDX` 기본 이름 out, `R8` 전체 경로 out, `R9` 섹션 핸들 out | 전체 경로 힙 문자열 | `LdrpLogInternal` ×2~4 · `LdrpAllocateUnicodeString` · `RtlAppendUnicodeStringToString` · `RtlAppendUnicodeToString` · `RtlInitUnicodeStringEx` | `NtOpenSection(0xd)` · (실패) `NtClose` | `0` / `0xc0000135` / `NtOpenSection` 상태 |
| `LdrpLoadKnownDll` | `RCX` 로드 문맥 | 항목 `+0x48` `+0x58` `+0x108` | `LdrpCheckKnownDllFullPath` · `LdrpFindKnownDll` · `LdrpHashUnicodeString` · `LdrpFindLoadedDllByNameLockHeld` · `LdrpLoadContextReplaceModule` · `LdrpMapDllWithSectionHandle` · 락 | `NtClose(섹션)` | NTSTATUS |
| `NtOpenDirectoryObject` · `NtOpenSymbolicLinkObject` · `NtQuerySymbolicLinkObject` | 질문 10 표 | — | — | `0x58` · `0x138` · `0x16b` | 커널 상태 |
| `RtlGetNtSystemRoot` · `RtlInitUnicodeStringEx` · `RtlAppendUnicodeToString` · `RtlFreeUnicodeString` | 질문 12 표 | 질문 12 표 | `wcslen` · `memcpy` · `RtlFreeHeap` | 없음 | 질문 12 표 |

## 클래스 `0xfa` 출력 칸 표 + nt 가 줄 값

| 항목 | 값 | 근거 |
|---|---|---|
| 입력 | 클래스 `0xfa`, 입력 버퍼 없음 (`ExpQuerySystemInformation` 에 `RDX = 0` · `R8D = 0` 으로 넘김) | `NtQuerySystemInformation +0x45 ~ +0x58` |
| 출력 버퍼 | 사용자 버퍼, 정렬 4 (`ProbeForWrite`), 길이 `>= 0x10` 이고 8의 배수 | `ExpQuerySystemInformation +0x3b6`, `+0xa45 ~ +0xa49`, `+0xf4f ~ +0xf53` |
| 출력 칸 | `+0x0 ~ +0xf` 128비트 비트맵 (비트 `k` ↔ 기능 번호 `0x40 + k`) | ntdll `RtlIsProcessorFeaturePresent +0x1b ~ +0x29` |
| 돌려주는 길이 | `0x10` | `ExpQuerySystemInformation +0xa3d` |
| 값의 출처 | `KeProcessorFeaturesBuffer` (rva `0xfc7498`) 16바이트 그대로 | `+0xf59 ~ +0xf60` |
| 정적으로 정해지는 값 | **모두 0** (쓰는 코드가 `KiSetFeatureBits +0x888` 의 0 채우기뿐) — 반증 가능 | `_refs/KeProcessorFeaturesBuffer.tsv` · `KeProcessorFeaturesBitMap.tsv` · `0xfc7480.tsv` |
| 상태값 | `0` · `0xc0000004` (길이 모자람·8의 배수 아님) · 포인터 예외 | 위 줄들 |

## 동적분석으로 확인해야 할 것 (Q104 VM 단계에 얹을 멈춤점 · 읽을 칸)

멈춤점은 모두 "test.exe 일 때만" 조건을 붙인다.

1. **`ntdll!LdrpInitializePolicy` 입구와 `+0x58`**: `LdrpPolicyBits` (`0x16` 유지), `LdrpAppPackagesPath` · `LdrpOriginalAppPackagesPath` (모두 0), `LdrSetDllDirectory` 가 안 걸리는지 (질문 1).
2. **`ntdll!LdrpInitializeProcess +0xfdb` 직전·`+0x1169`**: `ProcessParameters.Flags` (`0x4001`), `dd ntdll!LdrpIllegalCWDDevices L1` = 0 (`LdrpIncludeAlternateForwarders` · `LdrpCheckAppDirType` 입구에 멈춤점을 걸어 **안 걸리는 것**을 확인) (질문 1·2).
3. **`ntdll!RtlpInitCurrentDir` 입구·`+0x37`**: `RCX` 가 가리키는 ustr (`Length 0x84`), 돌아온 뒤 `PP+0x38 ~ +0x50`, `dq ntdll!RtlpCurDirRef L1`, 레코드 `+0x0 ~ +0x30` (질문 3). 사진과 이미 맞으니 선택.
4. **`ntdll!RtlInitializeProcessorFeaturesBitMap +0x2a` 직후**: `dq ntdll!RtlProcessorFeaturesBuffer L2` (0 이어야 함), `EAX` (상태 0); 커널 `nt!ExpQuerySystemInformation +0xf4f` 근처에서 `dq nt!KeProcessorFeaturesBuffer L2` (질문 4, **이것이 이 의뢰의 가장 중요한 동적 몫**).
5. **`ntdll!LdrpAllocateSchedulerSharedData +0x31` 직후 / `+0xb2` 직후**: `[RSP+0x30]` (핸들 값), `dq ntdll!LdrpSchedulerSharedDataListHead L2`, 노드 `+0x10`. 이어서 `LdrpAcquireSchedulerSharedDataSlot +0x92` 에서 `TEB+0x1850` 에 걸리는 값 (질문 5, run44 의 `≠ 0` 이 여기서 생기는지).
6. **`ntdll!LdrpInitializeTls +0x15d` 직전**: `dd ntdll!LdrpTlsBitmap L1` = 9, `db ntdll!LdrpStaticTlsBitmapVector L2` = `01 00`, `dq ntdll!LdrpActualBitmapSize L1` = 1, `dq ntdll!LdrpTlsHeap L1` = `PEB+0x30` 값 (질문 6).
7. **`ntdll!RtlRbInsertNodeEx` 입구 (4번 불림: ntdll×2 트리, exe×2 트리)**: `rcx` · `rdx` · `r8b` · `r9`, 돌아온 뒤 `Root`/`Min`; 둘째 트리 `r8b` 가 0 인지 (질문 8).
8. **`ntdll!LdrpFindKnownDll` 입구**: `du @rcx` 이름과 반환 상태; `ntdll!NtOpenSection` 입구 횟수 (질문 9). `ntdll!LdrpInitializeProcess +0xe95` 뒤 `du poi(ntdll!LdrpKnownDllPath+8)` 와 `dq ntdll!LdrpKnownDllDirectoryHandle L1` (질문 10).
9. **`RtlFreeUnicodeString` 뒤 첫 8바이트** (질문 12): `LdrpInitializeProcess +0xf5e` 가 안 타졌으면 (정적 결론) 이 free 자체가 안 일어난다. 타지는 입력으로 PE_emulator 와 비교할 때만 쓰기 감시점 (`ba w8`).

## 짐 지도 반영거리 (`runs/03_sprint/baggage.md`)

- **7.3 "쓴 자리 모르는 것" 목록 (`baggage.md` 1670 · 1671 줄) 에서 빠져 줄이 생기는 이름 14개**: `LdrpAppPackagesPath` `LdrpOriginalAppPackagesPath` (쓰는 곳 `LdrpInitializePolicy +0x22 ~ +0x3b`, test.exe 는 0 유지), `LdrpDefaultDllDirectories` (패키지 갈래만, test.exe 는 0 유지), `LdrpIllegalCWDDevices` (질문 2·11), `RtlpCurDirRef` (`RtlpInitCurrentDir +0x47`), `RtlProcessorFeaturesBuffer` · `RtlProcessorFeaturesBitMap` (`RtlInitializeProcessorFeaturesBitMap +0x2a +0x35`, 버퍼는 syscall 이 채움), `LdrpSchedulerSharedDataListHead` · `...ListHeadLock` (머리 초기화 `LdrpInitializeProcess +0x180b ~ +0x1820`, 노드 끼우기 `LdrpAllocateSchedulerSharedData +0x70 ~ +0xa6`), `LdrpTlsHeap` (`LdrpInitializeTlsHeap +0x39`), `LdrpTlsBitmap` (`LdrpInitializeTls +0x130 +0x13d` · 비트 `RtlSetBits`/`RtlClearBits`), `LdrpStaticTlsBitmapVector` (`+0x11d` 로 고르고 `RtlSetBits` 가 비트 0 씀), `LdrpActualBitmapSize` (`+0x127`), `LdrpActiveThreadCount` (`LdrpAllocateTls +0x1e5` `INC.LOCK`).
- 새 줄 (유저 공간, 단계 `②`, 경로 `LdrpInitializeProcess → …`):
  - 짐 "현재 디렉터리 레코드" — 힙 `0x238B` (`ProcessHeap`, 경로 `… +0x1517 RtlpInitCurrentDir → RtlpCreateNewDirectoryReference +0x115`) + 디렉터리 **핸들** (`PP+0x48` 과 레코드 `+0x8`, 사진 `0x58`) — 유저 공간 (핸들은 커널 객체 참조, 에뮬레이터 바깥) + `PP.CurrentDirectory.Buffer` 가 레코드 안을 가리킴.
  - 짐 "스케줄러 공유 데이터 노드 `0x18B`" (`LdrpHeap`) + 섹션 **핸들** (커널 객체, 커널 공간) — `LdrpAllocateSchedulerSharedData`.
  - 짐 "정적 TLS 벡터와 exe TLS 블록" (이미 baggage.md 390 줄이 `TEB+0x58` 로 다룸) 에 `LdrpTlsList` 항목 `0x48B` (`LdrpAllocateTlsEntry +0x35`) · 벡터 `0x58B` (`LdrpGetNewTlsVector +0x2c`) · TLS 블록 `0x11B` 를 크기와 함께.
  - 짐 "`LdrpKnownDllDirectoryHandle` · `LdrpKnownDllPath` + 힙 문자열 (`0x30B`)" — baggage.md 671 줄이 이미 있다: 크기 `0x30` 기본값과 재시도 길 (`+0xea2`) 만 추가.
  - 건너뛰는 갈래 정리 (test.exe 에서 안 쓰는 칸): `LdrpPolicyBits` 변경 · `LdrpDefaultDllDirectories` · `LdrpIllegalCWDDevices` 변경 — "안 바뀜" 로 적는다.
- 사진으로 닫을 것 (이미 있는 사진 P1a): `PP.CurrentDirectory` (`Length 0x84`, `Handle 0x58`), `TEB+0x58` 벡터 슬롯 0 — 이미 맞음.

## 판정

**분석성공** — 요청서 13개 질문 모두 [정적] 답과 줄 근거가 있고, 묶음 A 는 사진 P1a 와 대조까지 마쳤다. 동적으로 닫아야 남는 것은 (1) `KeProcessorFeaturesBuffer` 의 실제 값 (정적으로는 0), (2) `LdrpModuleBaseAddressIndex` 둘째 노드의 방향, (3) `NtOpenSection` 호출 횟수·이름, (4) 힙 해제 뒤 첫 8바이트 — 모두 Q104 에 얹을 작은 읽기들이다.

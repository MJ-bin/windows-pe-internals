# 119 run58-60-leftovers — 정적 분석 결과 (Q112 + Q113 + Q115, 질문 9개)

- 요청서: `_workspace/sast/119_run58-60-leftovers.md`
- 모듈 / 바이너리: ntdll (`ms/bin/.../System32/ntdll.dll`) · ntoskrnl (`.../ntoskrnl.exe`). 이름 찾기용으로 같은 폴더의 `nltest.exe`·`certca.dll`·`tdhres.dll` 은 **바이트만 읽음** (Ghidra 안 거침).
- PDB GUID: **둘 다 일치**
  - ntoskrnl `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`, 기준값과 같음)
  - ntdll `C3093720177DA6851519DCFFE8DF53FE1` (sast/53 · sast/111 과 같음)
- 분석: 둘 다 이미 돼 있음 (`.gpr` + `_index.tsv` + `SetPdb:` 줄). **다시 안 걸었다. 새 Ghidra 실행 0번, 새로 뽑은 함수 0개.** (돌고 있는 Ghidra 프로세스 없음 — 시작할 때 확인.)
  - 이미 있던 `.asm` 만 읽음. ntoskrnl: `PspAllocateProcess` · `PspApplyMitigationOptions` · `PspInheritMitigationOptions` · `PspHardenMitigationOptions` · `PspReadIFEOMitigationOptions` · `PspInitPhase0` · `PspBuildCreateProcessContext` · `PspPrepareSystemDllInitBlock` · `PspCaptureProcessParameters` · `PspCaptureUserProcessParameters` · `PspDeleteCreateProcessContext` · `PspDestroyProcessParameterOverrides` · `PspGetProcessParameterOverrides` · `PspCopyAndFixupParameters` · `PspCopyUserAttributes` · `PspProcessUserCopyAttribute` · `PspSetupUserProcessAddressSpace` · `RtlpQueryNlsSystemCodePages`. ntdll: `LdrpInitializeProcess` · `LdrProtectMrdata` 쪽 (`LdrpLocateMrdata` · `LdrpMakePermanentImageCommit` · `LdrEnsureMrdataHeapExists` · `LdrControlFlowGuardEnforced` · `LdrpInitializeCfgScpHelpers` · `RtlInitializeHistoryTable` · `LdrpInitializeExecutionOptions`) · `RtlSetUnhandledExceptionFilter` · `RtlpQueryNlsSystemCodePages` · `RtlpGetNtProductTypeFromRegistry`. kernelbase `CreateProcessInternalW` (곁눈질 한 줄).
  - 전수 검색은 스크래치 `ntdll.dis`·`nt.dis` (objdump 풀이, 커밋 안 함) 와 내가 만든 작은 도구 셋 (스크래치 `.../scratchpad/s119/`: `xref.py` = RIP 상대 참조 훑기, `ptrscan.py` = 이미지 안 8바이트/4바이트 값이 칸을 가리키는 곳, `rd.py` = rva 에서 글 읽기). 결과 파일은 안 남김.
- 버전 확인 (`_calls.tsv` `ret_off`, 스킬 "알려진 사실"): **일치**
  - `NtCreateUserProcess` call `+0xc31` / ret `+0xc36` → `PspAllocateProcess` · `PspInsertThread` `+0x802` / `+0x807` → `PspCallProcessNotifyRoutines`
  - `LdrpInitializeProcess` `+0x1bb6` / `+0x1bbb` → `LdrpInitializeGraphRecurse` · `_LdrpInitialize` `+0x167` / `+0x16c` → `LdrpInitializeProcess`
- 스크립트 문제: 없음.
- 오프셋 약속: `call +0x..` = CALL 이 있는 자리, `ret +0x..` = 다음 줄 (WinDbg `caller+0x..`), 그냥 `+0x..` = 그 명령이 시작하는 자리. 점프 목적지는 "목적지 주소 − 함수 시작" 으로 셈.

---

## 한눈에 — 질문 9개 한 줄씩

| # | 질문 | 한 줄 답 | 지금 우리 (PE_emulator) |
|---|---|---|---|
| 1 | `0xc000090b` 이름 | **`STATUS_BAD_DATA`** `[정적]` — PDB 에는 없지만 같은 빌드 `nltest.exe`·`certca.dll`·`tdhres.dll` 안 "값 → 이름" 표에 있다. ntdll 메시지 글 "Bad data." | 맞음 (우리 코드에 이 상수 없음) |
| 2 | ntoskrnl 사본 글 | **같다.** 키 `…\Control\Nls\CodePage`·값 `ACP`·`OEMCP` 글자 그대로. 호출 모양만 `Zw*`·`RtlInitUnicodeString` | 맞음 |
| 3 | `.rdata` 같은 글을 쓰는 다른 함수 | **없다.** 8개 글 모두 위 두 함수 (`RtlpGetNtProductTypeFromRegistry` · `RtlpQueryNlsSystemCodePages`) 안 `LEA` 한 곳씩뿐, 데이터 포인터 0곳 | 맞음 |
| 4 | 완화 옵션 셋째 칸 "2" 출처 | 비트 12~13 = **`PspAllocateProcess +0x1e03 ~ +0x1e15`** 가 **무조건** 2 로. 비트 24~25 = **`+0x1e1d ~ +0x1e30`** 가 `KeIsCoreIsolationMitigationPolicyEnforceable()` 이 거짓일 때 2 로. 비트 0~1 = `PspApplyMitigationOptions +0x510 ~ +0x523` (sast/54 그대로). 옵션 32~39 이름은 PDB `PS_MITIGATION_OPTION` 에 있다 | 다름 (알고 줄임) — 값 0, 읽는 줄이 지역 사본뿐이라 동작은 같음 |
| 5 | 만들기 정보 `+0x1d8` 을 읽는 함수 | **둘:** `PspCaptureProcessParameters +0x26` (→ `PspCaptureUserProcessParameters` 셋째 인자) · `PspDeleteCreateProcessContext +0x11a` (→ 지우기). 쓰는 곳은 `PspGetProcessParameterOverrides +0x4a` 하나 | 맞음 (줄임, 스텁 `ret 0` 가정 — 확인 필요) |
| 6 | 결과서 밖 전역 셋 | `RtlpUnhandledExceptionFilter` = 이미 run68 (sast/63) 이 3.8·2장에 셈. `LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity` = sast/111 1j 로 닫힘 (**0 을 씀**, 입력값). **2장에 센다: 아는 쪽 +2** | 맞음 |
| 7 | `PspCopyUserAttributes` → `+0x448` | **닫힘.** `PspCopyAndFixupParameters +0x3eb` → 목록을 "새 매개변수 블록 + 8바이트 올림한 (길이+환경 크기)" 자리에 쓰고 → `+0x3f8 ~ +0x402` 가 그 주소를 `+0x448` 에 쓴다. 기능 스위치가 꺼지면 이 줄을 안 탄다 | 맞음 (꺼짐 갈래) |
| 8 | `LdrpSuppressedExportOverrideListPtr` 읽는 곳 | **없다.** ntdll 안 전수에서 쓰기 1 (`LdrpInitializeProcess +0x49d`) 뿐. 읽기·포인터·이웃 칸 접근 모두 0 | 맞음 |
| 9 | 덤 다섯 | (a) 속성 `0x22` = **`PsAttributeHeapFeatures`** (PDB). (b) `LdrpMakePermanentImageCommit` = 쪽마다 "읽고 그대로 다시 쓰기" (아래). (c) `+0xdc4` `ECX` = **1** (닫기). (d) `LdrEnsureMrdataHeapExists` 는 CFG 가 켜져 있고 힙이 아직 없을 때만 쓴다 → test.exe 는 안 씀. (e) `.mrdata` 여닫는 곳 50자리 표 | 확인 필요 (e) |

---

## Q112 — run58 남은 것

### 1. 상태값 `0xc000090b` 의 이름 — **`STATUS_BAD_DATA`** `[정적]`

- 찾은 방법 (sast/100 이 쓴 길 + 한 가지 더):
  1. PDB 열거·공개 이름 (ntdll·ntoskrnl 둘, `llvm-pdbutil` 풀이 `types_*.pdb.txt`·`pubs_*.pdb.txt`): `-1073739509` (= `0xc000090b` 부호 있는 값) 도, `STATUS_BAD_*` 이름도 **0건**. 이 빌드 PDB 에는 NTSTATUS 이름이 없다 (sast/100 과 같음).
  2. ntdll 자기 메시지 표 (`RT_MESSAGETABLE`, 영어, 작은 파서): `0xc000090b` = **"Bad data."**. 이웃: `0xc000090a` "Bad key." · `0xc000090c` "Key does not exist." (ntoskrnl 의 메시지 표 538개에는 이 번호들이 없음).
  3. **같은 빌드의 다른 Windows 바이너리 안 "값 → 이름" 표** (이번에 새로 쓴 길): `System32` 의 `nltest.exe` · `certca.dll` · `tdhres.dll` 에 `{ DWORD 값; 빈칸 4바이트; 이름 포인터 }` 16바이트 칸 표가 들어 있다. 칸 하나씩 바이트로 읽음:

| 바이너리 | 칸 위치 (rva) | 값 | 이름 글 |
|---|---|---|---|
| `nltest.exe` | `0x68cd0` / `0x68ce0` / `0x68cf0` | `0xc000090a` / **`0xc000090b`** / `0xc000090c` | `STATUS_BAD_KEY` / **`STATUS_BAD_DATA`** / `STATUS_NO_KEY` |
| `certca.dll` | `0x557b0` / `0x557c0` / `0x557d0` | 같은 셋 | 같은 셋 |
| `tdhres.dll` | `0x72cd0` / `0x72ce0` / `0x72cf0` | 같은 셋 | 같은 셋 |

  - 세 바이너리가 서로 독립인데 값·이름이 다 같고, ntdll 메시지 글 ("Bad data." / "Bad key." / "Key does not exist.") 과도 뜻이 맞는다 → 이름 확정.
- 쓰이는 곳 (`mov reg,0xc000090b` 전수, 정적): ntdll 6곳 — `RtlpGetNtProductTypeFromRegistry +0x1b0` · `LdrAppxHandleIntegrityFailure +0x14c` · `LdrpAppxGetBinaryNameKeyInformation +0x63`·`+0xfb` · `LdrpAddRedirectedFunction +0x1b6` · `LdrpBuildImportRedirection +0x77`. ntoskrnl 9곳 (예: `RtlpGetNtProductTypeFromRegistry +0x173`, `SepVerifyDesktopAppxImage +0x18f`, `SshpSessionManagerOpenControlTrace +0x1be`·`+0x1c5`). 이 요청의 뜻: **제품 종류 값이 `REG_SZ` 가 아니거나 짧거나 `WinNt`·`LanmanNt`·`ServerNt` 셋 다 아니면 `STATUS_BAD_DATA`** (sast/53 질문 7 의 `+0x1b0 MOV EBX,0xc000090b` → `+0x1b5 JMP +0xdc`).
- **덤 (같은 표로 한 번에 닫힌 것):** sast/100 ④ 가 `[추측: 기억]` 으로 둔 **`0x40000036` = `STATUS_IMAGE_AT_DIFFERENT_BASE`** (`nltest.exe` 칸 rva `0x64770`), `0x40000003` = `STATUS_IMAGE_NOT_AT_BASE` (rva `0x64440`) 도 `[정적]` 으로 확정. (`0xc0000017` = `STATUS_NO_MEMORY` 도 같은 표에서 맞음 — 표 읽기가 맞다는 확인.)
- **지금 우리 (PE_emulator):** **맞음 / 이름만 보태면 됨.** 우리 `Src` 에 `0xc000090b` 상수는 없다 (진짜 ntdll 코드가 쓰는 값이고, test.exe 는 KUSD `ProductTypeIsValid` = 1 이라 이 길을 안 탐 — sast/53 7·run44 P1a). `Src/nt/AddressSpace.cpp:752 ~ 759` 의 `0x40000036` 주석에는 위 이름을 붙일 수 있다.

### 2. ntoskrnl 사본 `RtlpQueryNlsSystemCodePages` 의 글 — **ntdll 것과 같다** `[정적]`

- 함수: ntoskrnl rva `0x78af7c`, 452B (`_index.tsv` 에 같은 이름은 **하나뿐** — 이름이 겹친 다른 함수 아님). 부르는 곳 `ExInitializeNls +0x79` (ret `+0x7e`, `_calls.tsv`). 글은 `.rdata` 바이트로 직접 읽음:

| 쓰임 | ntoskrnl 줄 | 글 (UTF-16LE + NUL) | ntdll 쪽 (sast/53) |
|---|---|---|---|
| 키 | `+0x48 LEA RDX,[0x140afbe50]` → `+0x5f` `RtlInitUnicodeString` (ret `+0x64`) | `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` (63자) | 같은 글 (rva `0x180ad0`) |
| 값 1 | `+0xa3 LEA RDX,[0x140afbe40]` → `+0xb9` (ret `+0xbe`) | `ACP` | 같은 글 (`0x180b50`) |
| 값 2 | `+0x11c LEA RDX,[0x140afbe30]` → `+0x127` (ret `+0x12c`) | `OEMCP` | 같은 글 (`0x180b58`) |

- 나머지 모양도 같다 (다른 점은 호출 이름뿐):
  - 시작 때 두 출력 칸에 `0xfde9` (`+0x2b`·`+0x31`). `ObjectAttributes` 길이 `0x30` (`+0x68`), `Attributes = 0x240` (`+0x83`), 접근 `0x80000000` (`+0x7e`).
  - `ZwOpenKey` (call `+0x93`, ret `+0x98`) → `ZwQueryValueKey` ACP (call `+0xdf`, ret `+0xe4`; 클래스 2 = `+0xcf LEA R8D,[R14+2]`, 버퍼 `0x24` = `+0xd3`) → `RtlUnicodeStringToInteger` (call `+0x111`, ret `+0x116`, 진법 10) → `ZwQueryValueKey` OEMCP (call `+0x14f`, ret `+0x154`) → `RtlUnicodeStringToInteger` (call `+0x17e`, ret `+0x183`) → `ZwClose` (call `+0x189`, ret `+0x18e`).
  - **갈래도 같다:** ACP 형식이 `REG_SZ` 아님 → `+0xee CMP [RBP+0x1b],1` · `+0xf2 JNZ +0x11c` 로 **둘째 읽기로 넘어감** (ntdll `+0x121 → +0x171` 과 같음). OEMCP 형식이 아님 → `+0x15e JNZ +0x185` 로 `ZwClose` (EBX = 읽기 상태 0 이상이라 실패 아님, ntdll `+0x1cf → +0x219` 와 같음). 열기 실패 `+0x9a JS +0x192` (닫기 없이 `0xfde9` 되돌리기), 읽기·변환 실패 `+0x11a JS +0x185` 등 → 닫은 뒤 `+0x190 JNS +0x19e` 아니면 `+0x192`·`+0x198` 에서 둘 다 `0xfde9`. **늘 `+0x19e XOR EAX,EAX` = 0 반환.**
- 옆 사실 (묻지 않았지만 같은 표에서): ntoskrnl 에도 **`RtlpGetNtProductTypeFromRegistry` 사본이 있다** (rva `0xaa5c98`, 452B, 글 5개 `0xafc4d0 ~ 0xafc540` 이 ntdll 것과 같은 글, `+0x173 MOV EBX,0xc000090b`). 부르는 곳 `RtlGetNtProductType +0x51` · **`PspSiloInitializeUserSharedData +0x12a`** — KUSD `NtProductType` 를 채우는 쪽이 이 사본이라는 뜻 `[정적: 부르는 곳까지만]`.
- **지금 우리 (PE_emulator):** **맞음.** `Src/nt/Process.cpp:242 ~ 244` 가 ACP·OEMCP = 949 한 쌍을 커널 PEB (`+0x34c`·`+0x34e`) 와 silo 칸에 쓰는 설계인데, 커널 사본과 ntdll 사본이 같은 두 레지스트리 칸 (`ACP`·`OEMCP`) 을 읽으니 "두 쪽이 같은 값에서 나온다" 가 맞다 (`Registry.h:14 ~ 15` 의 `REG_SZ`·클래스 2 도 두 사본에 같음).

### 3. ntdll `.rdata` 같은 글 (`0x180ad0` · `0x180b50` · `0x180b58` · 제품 종류) 을 쓰는 다른 함수 — **없다** `[정적]`

- 훑은 것 (셋을 다 했음, 서로 덮어 확인):
  1. 코드: `ntdll.dis` (421,449줄) 의 RIP 상대 목적지가 글 범위 (`0x180ad0 ~ +0x80` · `0x180b50 ~ +8` · `0x180b58 ~ +0xc` · `0x17ab28 ~ 0x17ac04`) 안인 줄 **전부** → **8줄**, 모두 아래 두 함수:

| 함수 | 줄 (명령 시작) | 글 (rva) |
|---|---|---|
| `RtlpQueryNlsSystemCodePages` | `+0x3c` · `+0xc4` · `+0x171` | 키 `0x180ad0` · `ACP` `0x180b50` · `OEMCP` `0x180b58` |
| `RtlpGetNtProductTypeFromRegistry` | `+0x2d` · `+0x5d` · `+0x74` · `+0x7f` · `+0x8a` | 키 `0x17ab80` · `ProductType` `0x17ab28` · `LanmanNt` `0x17ab50` · `ServerNt` `0x17ab68` · `WinNt` `0x17ab40` |

  2. 데이터: ntdll 이미지 전체에서 8바이트 값 (8가지 정렬) 이 `이미지 시작 + 위 범위` 인 곳 **0**, 4바이트 값이 위 rva 범위인 곳 (4가지 정렬) **0**. (재배치 표·함수 포인터 표·`.mrdata` 어디에도 이 글 가리키는 칸 없음.)
  3. 글 복사본: 이미지 전체에서 같은 글자를 찾음 — `ProductType`·`WinNt`·`LanmanNt`·`ServerNt`·`OEMCP` 은 각 **1곳 (원본뿐)**. `ProductOptions` 도 1곳. `ACP` 는 UTF-16 로 9곳이지만 나머지 8곳은 `0x2306a4 ~ 0x2402ce` (다른 글 속 부분 문자열, `.rsrc`/메시지 쪽) 이라 이 글이 아님. `CodePage` 가 든 다른 글은 `activeCodePage` (`0x1809d0`) · `AlternateCodePage` (`0x197528`) — 다른 값 이름.
- 그래서 sast/53 이 "이 폴더에 없는 함수가 같은 글을 쓰는지는 안 봄" 이라 적은 빈칸이 닫힘: **ntdll 안에서 이 글 8개의 쓰는 자리는 위 8줄이 전부.** (링커가 글을 합쳐서 다른 함수가 같은 주소를 가리킬 수 있다는 걱정은 위 1번 훑기로 해소 — 합쳐진 흔적 없음.)
- **지금 우리 (PE_emulator):** **맞음.** 우리가 이 글을 따로 올릴 일이 없다 — 진짜 ntdll 함수의 상수이고 쓰는 곳이 둘뿐이다 (레지스트리 읽기 syscall 만 답하면 됨).

---

## Q113 — run59 남은 것

### 4. 시작 정보 블록 완화 옵션 셋째 칸 (`+0xb0`, 사진 `0x02002002`) 의 "2" — **출처는 `PspAllocateProcess` 안, 입력이 아니다** `[정적]`

#### 4-1. 옵션 번호 32 ~ 39 와 이름 (PDB `PS_MITIGATION_OPTION`, ntdll·ntoskrnl PDB 둘 다 같음)

옵션 번호 n → 표의 `n / 16` 번째 8바이트, 그 안 비트 `(n % 16) × 4` (4비트: 비트 0~1 = 상태, 비트 2 = 고정). 셋째 8바이트 = 옵션 32 ~ 39, 아래 32비트만 씀.

| n | PDB 이름 | 셋째 칸 비트 | `== 1` 일 때 (sast/54 표 + PDB 비트 이름) |
|---|---|---|---|
| 32 | `USER_CET_SET_CONTEXT_IP_VALIDATION` | 0~1 | 감사표 같은 칸이 1 이면 `MitigationFlags2 \|= 0x40000` (`AuditUserCetSetContextIpValidation`) |
| 33 | `BLOCK_NON_CET_BINARIES` | 4~5 | 1 → `0x200000` (`BlockNonCetBinaries`), 3 → `+0x400000` (`…NonEhcont`), 감사 → `0x800000` |
| 34 | `RESERVED_WAS_XFG` | 8~9 | 읽는 줄 없음 (이름 그대로 "예전에 XFG" 자리) |
| 35 | `POINTER_AUTH_USER_IP` | 12~13 | 1 → `MitigationFlags2 \|= 0x8000000` (`PointerAuthUserIp`). **0 이면 1 로 고쳐 쓰고 같은 효과** (`+0x56c JZ → +0xe65`, 쓰기 `+0xe65 ~ +0xe74`) |
| 36 | `CET_DYNAMIC_APIS_OUT_OF_PROC_ONLY` | 16~17 | 1 → `MitigationFlags2 \|= 0x40000000` (`+0x5a5 OR.LOCK [RDI+0x754],R14D`, `R14D = 0x40000000` = `+0x3da`; PDB 비트 순서로 `CetDynamicApisOutOfProcOnly` `[추측: 비트 순서 세기]`) |
| 37 | `REDIRECTION_TRUST` | 20~21 | 1 → `PspSetRedirectionTrustPolicy(process, 1)` (call `+0xe9d`, ret `+0xea2`) |
| 38 | `RESTRICT_CORE_SHARING` | 24~25 | 1 → `MitigationFlags3 \|= 1` (`+0xea7`, PDB `RestrictCoreSharing`) |
| 39 | `FSCTL_SYSTEM_CALL_DISABLE` | 28~29 | 1 → `MitigationFlags3 \|= 6` (PDB 비트 순서로 `DisallowFsctlSystemCalls` + `Audit…` `[추측: 비트 순서]`) |

- 값 이름 (`ON`/`OFF` 등) 은 PDB 에 없다. 코드가 가르는 값은 `== 1` (효과 켬) 과, 옵션 33 의 `== 3` (sast/54), 옵션 35 의 `== 0` 뿐이고 **`2` 는 어느 줄도 "효과 있음" 으로 안 본다** → "2 = 일부러 끔" 이라는 공개 헤더 이름 (`…_ALWAYS_OFF`) 은 `[추측: 기억]`.
- 앞 두 8바이트에 쓰이는 이름 (참고): 옵션 3 `HEAP_TERMINATE` = 비트 12~13, 10 `CONTROL_FLOW_GUARD` = 40~41 (sast/102 · sast/55 와 같음), 15 `IMAGE_LOAD_PREFER_SYSTEM32` = 60~61, 17 `LOADER_INTEGRITY_CONTINUITY` = 둘째 8바이트 비트 4~5, 19 `RESTRICT_SET_THREAD_CONTEXT` = 둘째 8바이트 비트 12~13, 31 `CET_USER_SHADOW_STACKS` = 둘째 8바이트 비트 60~61.

#### 4-2. "2" 가 처음 쓰이는 줄

표가 커널을 지나는 길 (`PspAllocateProcess`, 함수 시작 = ntoskrnl rva `0xa02568`):

1. `+0x1a8b` (ret `+0x1a90`) `PspReadIFEOMitigationOptions` → 지역 `[RSP+0x400]` 칸 둘 (24B). IFEO 에 없으면 0.
2. `+0x1ab2 ~ +0x1ac9` 시스템 전역 `PspSystemMitigationOptions` (24B) 를 `[RSP+0x2d0]` 에 복사. `+0x1aea` (ret `+0x1aef`) **`PspInheritMitigationOptions(A = 시스템 값, B = IFEO 값, 출력 = [RSP+0x400])`**. 옵션 40개를 4비트씩 훑어 "A 의 비트 2 가 켜졌거나 B 의 아래 2비트가 0 이면 A, 아니면 B" 로 고른다 (`PspInheritMitigationOptions +0x7c TEST DL,4` · `+0x81 TEST AL,3` · `+0x85 MOV AL,DL`).
   - **정정:** sast/54 질문 13 이 이 함수를 "부모 값" 이라 불렀는데, **A 는 부모 프로세스가 아니라 시스템 전역 표** (`PspSystemMitigationOptions`, rva `0xfc7140`) 다. 이 전역은 부팅 때 레지스트리 `…\Session Manager\Kernel` 값 **`MitigationOptions`** 에서 채워진다 (ntoskrnl INIT 표 rva `0xc825a0 ~ 0xc825b8`: 키 글 `Session Manager\Kernel` · 값 이름 글 `MitigationOptions` · 자료 칸 `PspSystemMitigationOptions` · 길이 칸 `PspSystemMitigationOptionsLength` — 이 표를 읽는 함수는 안 따라감 `[추측]`; 파일 처음 값은 24바이트 0, `PspInitPhase0 +0x108 ~ +0x19e` 가 길이 맞춤·검증).
3. `+0x1b79 ~ +0x1b82` 만들기 정보 `+0x8` 비트 16 (사용자가 옵션 속성을 줌) 이 켜져 있으면 `+0x1b84 / +0x1b9d` 에서 만들기 정보 `+0x150` (16B) · `+0x160` (8B) 을 B 로, 방금 만든 `[RSP+0x400]` 를 A 로 해서 `+0x1be0` (ret `+0x1be5`) `PspInheritMitigationOptions` 한 번 더. (`+0x150`·`+0x160` 은 `PspBuildCreateProcessContext +0xad8 ~ +0xb7b` 가 사용자 속성 `PsAttributeMitigationOptions` (16) 의 24바이트를 `PspValidateMitigationOptions` (call `+0xb55`, ret `+0xb5a`) 로 검사한 뒤 `+0xb68` · `+0xb71` 로 넣은 것. 속성 크기 `+0xae1 CMP R8,0x18` 이하. 사용자 쪽 입구: kernelbase `CreateProcessInternalW +0x22a3` (ret `+0x22aa`) `GetEmbeddedImageMitigationPolicy` 가 이미지에 박힌 정책이 있을 때만 (`+0x22b6 CMP [RSP+0x424],EDI` · `+0x22bd JZ +0x2375`) 24바이트 버퍼 (`[RSP+0xb50]`, 처음 0 — `+0x503`·`+0x50b`) 를 채워 `+0x2370` (ret `+0x2375`) 으로 속성 `0x20010` 에 얹는다. test.exe 는 박힌 정책이 없어 `[추측: 매니페스트·로드 구성 안 봄]` 입력 0. 호출자가 따로 주는 정책 속성은 안 봄. 어느 쪽이든 아래 6번이 비트 12~13 을 덮는다.)
4. `+0x1c82` (ret `+0x1c87`) `PspHardenMitigationOptions` — 앞 16바이트만 만지고 셋째 칸은 안 건드림 (`+0x132 ~ +0x136` 이 `XMM2` 셋째 칸을 그대로 되돌려 씀).
5. **`+0x1c87` `KeIsUserCetAllowed` (ret `+0x1c8c`)** — 거짓이면 `+0x1ca4 ~ +0x1cb2` 가 **둘째** 8바이트 비트 60~61 (옵션 31) 을 2 로 (`AND` `-0x1000000000000001` / `OR` `0x2000000000000000`). 사진 `+0xa8 = 0` 이라 이 길은 안 탄 것 (`KiUserCetAllowed` ≠ 0 — 사진과 맞음 `[정적 + 관찰]`).
6. **`+0x1e03 MOV RCX,[RSP+0x410]` → `+0x1e0b BTR RCX,0xc` → `+0x1e10 BTS RCX,0xd` → `+0x1e15 MOV [RSP+0x410],RCX`** — 셋째 8바이트 **비트 12 를 끄고 비트 13 을 켬 = 옵션 35 `POINTER_AUTH_USER_IP` := 2.** 앞에서 오는 길은 `+0x1deb JZ +0x1e03` 과 아래로 떨어지는 길뿐이고 이 구간으로 뛰어드는 다른 점프는 없다 (`PspAllocateProcess.asm`·`nt.dis` 에서 `0x140a0436b` 로 가는 점프 1개) → **무조건**. 사용자·IFEO·시스템 값이 뭐든 덮어쓴다.
7. **`+0x1e1d CALL KeIsCoreIsolationMitigationPolicyEnforceable` (ret `+0x1e22`) → `+0x1e24 JNZ +0x1e38`** — 거짓이면 `+0x1e26 BTR RCX,0x18` · `+0x1e2b BTS RCX,0x19` · `+0x1e30 MOV [RSP+0x410],RCX` = **비트 24~25 (옵션 38 `RESTRICT_CORE_SHARING`) := 2.** 이 함수는 11바이트 잎 함수 (`KiCoreIsolationEnforceable` (rva `0xfc6ed0`) ≠ 0 을 돌려줌, `RCX` 안 건드려서 `+0x1e26` 이 `RCX` 를 그대로 씀). 그 전역은 `KeOptimizeSpecCtrlSettings +0x48` 이 쓴다 (인자 0 일 때만): **`HvlHypervisorConnected` = 0 이면 1**, 하이퍼바이저가 붙어 있으면 `HvlpFlags` 비트 22 꺼짐 → 0, 비트 22 켜짐·비트 1 꺼짐 → 1, 둘 다 켜짐이면 `HvlpSchedulerType` 이 1·3·4 → 1, 그 밖 (2 포함) → 0 으로 갈림 `[정적: 갈래만]`. 사진이 2 이므로 이 VM 에서는 0 이었다는 뜻.
8. `+0x1eb3` (ret `+0x1eb8`) **`PspApplyMitigationOptions(…, R8 = &[RSP+0x400], …)`**. 위 두 "2" 는 효과가 없는 값이라 (`+0x56c JZ` 는 비트 12~13 이 0 일 때만 · `+0x572 CMP ECX,1` · `+0x5dd CMP ECX,1`) 그대로 남는다. **비트 0~1 의 2** 는 여기서 쓴다: `+0x500 TEST [RDI+0x754],0x4000` (프로세스 `CetUserShadowStacks` 꺼짐) → `+0x510 ~ +0x523` 이 비트 0 을 끄고 1 을 켬. 끝 `+0x5ef` · `+0x5f4` 가 표를 제자리에 되씀.
9. `+0x1f37 ~ +0x1f50` 표를 만들기 정보 `+0x150` · `+0x160` 에 되돌려 씀 → `PspPrepareSystemDllInitBlock +0x64 ~ +0x78` (`[RDX+0x150]`·`[RDX+0x160]` 읽기) → **`+0x12b MOVUPS [RBX+0xa0]` · `+0x138 MOVSD [RBX+0xb0]`** (시작 정보 블록 `+0xa0` 16B · `+0xb0` 8B).

- 그래서 `+0xb0 = 0x02002002` 는: 비트 0~1 = 2 (CET 켜진 프로세스가 아니라서), **비트 12~13 = 2 (커널이 늘 일부러 끔 — x64 에는 포인터 인증이 없음)**, **비트 24~25 = 2 (`KiCoreIsolationEnforceable` 이 0 이라서)**. sast/54 가 "`PspApplyMitigationOptions` 가 안 쓰니 입력 표가 갖고 온 값 `[공백]`" 이라 적은 것은 **틀림** — 입력 표가 아니라 호출한 쪽 `PspAllocateProcess` 가 그 자리에서 쓴다. 이 값은 test.exe 에 따라 달라지지 않는다 (사용자·IFEO 값을 덮어씀) — 달라지는 건 비트 24~25 (이 컴퓨터의 `KiCoreIsolationEnforceable`) 와 비트 0~1 (프로세스 CET 상태) 둘.
- 사진 쪽 대조 `[관찰]` (run44, 기존 문서): `+0xa0 = 0`, `+0xa8 = 0`, `+0xb0 = 0x02002002` — 위 길과 모순 없음 (둘째 칸 0 은 5번이 안 타서).
- **지금 우리 (PE_emulator):** **다름 — 알고 줄인 것.** `Src/nt/Process.cpp:767` 이 블록 `+0xa0` 24바이트를 0 으로 쓰고 `:844` 주석이 "사진 `0x02002002` 안 베낌" 이라 적음. ntdll 이 `+0xb0` 을 읽는 줄은 `LdrpInitializeProcess +0x4b3` · `+0xa09` 의 스택 사본뿐이라 (sast/54) **동작은 같다**. 값을 맞추려면 위 세 줄만 넣으면 된다: 비트 12~13 := 2 (무조건), 비트 24~25 := 2 (이 컴퓨터는 0 이라 사진처럼), 비트 0~1 := 2 (CET 끔). 그 값은 `PspPrepareSystemDllInitBlock` 의 `+0x12b ~ +0x138` 에서 블록으로 간다.

### 5. 만들기 정보 `+0x1d8` 을 읽는 함수 — **둘** `[정적]`

- 찾은 방법: ntoskrnl 전체 `nt.dis` (3,086,164줄) 에서 `[레지스터+0x1d8]` 모양 **211줄**을 훑어, 스택 (`RSP`·`RBP`) 기준·EPROCESS (`rbx+0x1d8` = `ActiveProcessLinks` 등)·다른 구조체를 이름으로 걸러냄. 남는 건 아래 셋 (쓰기 1 + 읽기 2). 호출로 안 닿는 접근 (`LEA rax,[reg+0x1d0]` 뒤 `[rax+8]` 꼴) 은 만들기 정보를 받는 함수 (`PspAllocateProcess` · `PspBuildCreateProcessContext` · `PspCaptureCreateInfo` · `PspSetupUserProcessAddressSpace` · `NtCreateUserProcess`) 에서 `0x1d0]`·`0x1d8]`·`0x378]` (`NtCreateUserProcess` 의 지역 만들기 정보 `RBP+0x1a0` 기준) 로 따로 확인 — 없음.

| 구분 | 함수 · 줄 | 하는 일 |
|---|---|---|
| 쓰기 (유일) | `PspGetProcessParameterOverrides +0x4a MOV [RBX+0x1d8],RAX` (`NtCreateUserProcess +0xab3` / ret `+0xab8`) | sast/54 12 그대로 (확장 import 스텁 `ret 0` 이면 0) |
| **읽기 1** | **`PspCaptureProcessParameters +0x26 MOV R8,[R8+0x1d8]`** (부르는 곳 `NtCreateUserProcess +0xac6` / ret `+0xacb`) | 첫 인자 `CL` ≠ 0 인 갈래 (`+0x15 TEST CL,CL` · `+0x17 JZ +0x58`) 에서 그 포인터를 **`PspCaptureUserProcessParameters` 의 셋째 인자 (`R8`)** 로 넘김 (call `+0x3a`, ret `+0x3f`). `CL = 0` 이면 이 줄을 안 읽음 (`+0x58` 이 `[R10]` 만 채움) |
| **읽기 2** | **`PspDeleteCreateProcessContext +0x11a MOV RCX,[RBX+0x1d8]`** | 바로 `+0x121` (ret `+0x126`) `PspDestroyProcessParameterOverrides` 로 지움. 그 함수는 `+0x4 TEST RCX,RCX` · `+0x7 JNZ` 로 **0 이면 아무것도 안 함** (`+0xf` 이후 import `PsDestroyProcessParameterOverrides` 를 부름). 부르는 곳: `NtCreateUserProcess +0x11d0` · `PspBuildCreateProcessContext +0x188e` · `NtCreateThreadEx +0x2d8` (ret 은 각 `+5`) |

- 그 포인터가 가리키는 칸의 뜻 (읽기 1 안에서, `PspCaptureUserProcessParameters` 의 `R15` = `R13` = 셋째 인자) — 이 **덤이 sast/104 의 해석을 바로잡는다:**
  - `[R15+0x0]` = 버전 (`PspGetProcessParameterOverrides` 가 `2` 만 받음, sast/54), `[R15+0x4]` = 어떤 칸을 덮을지 비트:
    - 비트 1 (`+0x7a1 TEST AL,2`) → `[R15+0x8]` UNICODE_STRING 16B 가 사용자 `RedirectionDllName` (`+0x410`) 캡처를 대신함 (`+0x7a9`)
    - 비트 2 (`+0x7bb TEST AL,4`, `+0x824`) → `[R15+0x18]` 16B 가 `HeapPartitionName` (`+0x420`) 이 되고 `[R15+0x38]` → `HeapMemoryTypeMask` (`+0x440`, `+0x84d`)
    - 비트 3 (`+0x80d TEST AL,8`) → `[R15+0x34]` → `DefaultThreadpoolThreadMaximum` (`+0x43c`, `+0x819`)
    - 비트 0 (`+0x13bcd4 TEST AL,1`, 조각) → 포인터 `[R15+0x28]` 에서 `[R15+0x30]` × 8바이트를 블록 뒤에 복사해 `DefaultThreadpoolCpuSetMasks` (`+0x430`) · `Count` (`+0x438`) 로 (크기 잡기는 `+0x7d6 ~ +0x7f5`)
  - 이 칸 이름은 모두 PDB `_RTL_USER_PROCESS_PARAMETERS` 의 것 (`+0x410 RedirectionDllName`, `+0x420 HeapPartitionName`, `+0x430/+0x438 DefaultThreadpoolCpuSetMasks/Count`, `+0x43c DefaultThreadpoolThreadMaximum`, `+0x440 HeapMemoryTypeMask`, `+0x448 AttributeList`).
  - **sast/104 정정 (이름만):** sast/104 질문 1(a) 의 "Environment 를 어디에 두나" 표가 `+0x420`·`+0x55e`·`+0x7d6` 줄을 "`PS_CREATE_INFO` 비트 0/2" 로 적었는데, 그 `R15` 는 **만들기 정보가 아니라 `+0x1d8` 의 덮어쓰기 구조** 다. 줄 오프셋·동작은 그대로 맞다.
- test.exe: `PspGetProcessParameterOverrides` 스텁이 `ret 0` 이면 `+0x1d8 = 0` → 읽기 1 은 `+0x22a TEST R15,R15 · +0x22d JNZ` · `+0x250 · +0x253` 이 안 뛰어 **사용자 값 캡처 갈래** (사진 `+0x430 ~ +0x448 = 0` 과 맞음, 기존 문서), 읽기 2 는 `+0x4 JNZ` 안 뜀. 슬롯이 정말 스텁으로 남는지는 정적으로 못 정함 (Q114 (4) — 동적).
- **지금 우리 (PE_emulator):** **맞음 (줄임).** `Src/nt/Process.cpp:1755` "안 함: `+0xab3` `PspGetProcessParameterOverrides`" = `+0x1d8` 을 0 으로 두는 것과 같은 결과. 확장이 실제로 붙어 0 이 아닌 포인터를 주면 위 비트 네 개가 `+0x410 ~ +0x440` 을 바꾸므로 그때는 **확인 필요** (Q114 (4)).

### 6. baggage 7.5 결과서 밖 전역 셋 — sast/111 · run68 근거를 옮김 (새로 안 봄, 줄 번호만 `.asm` 로 다시 맞춤)

| 전역 (rva) | 쓰는 줄 | 값 | 읽는 줄 (이름·자리만) | 근거 | 지금 우리 |
|---|---|---|---|---|---|
| `RtlpUnhandledExceptionFilter` (`0x1d28e8`) | 첫째: `LdrpInitializeProcess +0x12d XOR ECX,ECX` → call `+0x12f` (ret `+0x134`) `RtlEncodePointer` → **`+0x134 MOV [칸],RAX`**. 둘째: `RtlSetUnhandledExceptionFilter +0x4` (ret `+0x9`) `RtlEncodePointer` → `+0x9 MOV [칸],RAX` (KERNELBASE `_KernelBaseBaseDllInitialize +0x4ab/+0x4b2` 가 부름, run68) | 첫째 = 인코딩된 0, 둘째 = 인코딩된 `kernelbase!UnhandledExceptionFilter` | `RtlpNotOwnerCriticalSection +0xb1` · `RtlpPossibleDeadlock +0x38` · `TppExceptionFilter +0x9` · `LdrpFatalExceptionFilter +0x40` · `LdrpLogFatalUserCallbackException +0x37` · `RtlUserThreadStart$filt$0 +0x19` (`xref` 전수, 이번에 센 것) | sast/63 (Q129) 5번 · 본문 · 표 줄, baggage 7.5 취소선 (run68 이 3.8 로 옮김) | 맞음 |
| `LdrpEnforceIntegrityContinuity` (`0x1d2900`, 1B) | `LdrpInitializeProcess +0x4a4 MOVAPS XMM1,[LdrSystemDllInitBlock+0xa0]` → 둘째 8바이트 `>> 4 & 3` (`+0x4c4 ~ +0x4d6`) → `+0x4d9 CMP AL,BL` (`BL`=1) → **`+0x4db SETZ [칸]`** (값 1 이면 1) | 0 (사진 `끝까지 0`) — **입력값에서 계산, 0 을 씀** | `LdrpMapDllNtFileName +0x416` · `LdrpValidateIntegrityContinuity +0x90` | sast/111 1j · 표 (줄 177~178) · "정확" | 맞음 (sast/111 1k: `LdrInitTest.cpp:1544` 기대 0, 입력 비트 켠 시험 `:1578`) |
| `LdrpAuditIntegrityContinuity` (`0x1d2918`, 1B) | 같은 줄기: `+0x4f5 CMP AL,3` → `+0x4f7 MOV AL,R14B` · `+0x4fc MOV AL,BL` → **`+0x4fe MOV [칸],AL`** (값 1 또는 3 이면 1) | 0 (사진 `끝까지 0`) — **0 을 씀** | `LdrpMapDllNtFileName +0x141` | 같음 | 맞음 (`:1545` 기대 0, 켠 시험 `:1579` 1) |

- 줄 확인 (`.asm` 직접): `+0x12f` ret `+0x134` · `+0x134` 쓰기 (`LdrpInitializeProcess.asm` 줄 57~58), `RtlSetUnhandledExceptionFilter +0x4/+0x9` (`.asm` 줄 3~4). 나머지 두 줄은 sast/111 줄 그대로 (이번에 안 열어 봄).
- **2장에 셀지 (한 줄 판단):** **셈 — 셋 다 "쓴 자리 아는 것" 쪽.** `RtlpUnhandledExceptionFilter` 는 run68 이 이미 세었으니 다시 안 센다. `LdrpEnforce…` · `LdrpAudit…` 두 개만 새로 센다: **`끝까지 0` 쪽 ntdll 전역 "모름" 304 → 302, "아는 것" 189 → 191, `끝까지 0` 214 → 212** (baggage 7.3 목록 줄 1988 의 이름 둘을 빼고 3.8 에 "0 을 씀" 두 줄을 더함). 이 둘은 **직접 쓰는 줄이 있고 값이 0** 이라 "조건부 안 탐" 이 아니라 "0 을 씀" 으로 분류 (sast/111 의 분류와 같음). 지금 baggage 3.8 에는 두 이름의 줄이 아직 없음 (7.3 줄 1988 · 7.5 줄 2077 에만 있음, grep).

---

## Q115 — run60 남은 것

### 7. `PspCopyUserAttributes` 가 쓴 목록이 새 프로세스 `ProcessParameters +0x448` 에 이어지는 줄 — **닫힘** `[정적]`

사슬 (끝에서 끝까지, 새 프로세스 안 주소 기준):

| 순 | 어디서 | 줄 | 하는 일 |
|---|---|---|---|
| ① | `PspBuildCreateProcessContext` | `+0x1577` (ret `+0x157c`) `Feature_CreateProcessSegmentHeapEnablement…IsEnabledDeviceUsageNoInline` → `+0x157e JZ +0x17db` (스위치 꺼지면 건너뜀) · `+0x1584 TEST [RSP+0x138],0x80000` · `+0x1590 JZ +0x17db` · **`+0x159f` (ret `+0x15a4`) `PspProcessUserCopyAttribute`** | 속성값에 `0x80000` 비트가 있는 항목만 복사 (sast/55 2-2: 번호 `0x22` 하나뿐) |
| ② | `PspProcessUserCopyAttribute` (rva `0x5e2460`) | `+0x1f MOV R14D,[R8+0x250]` · `+0x2d`·`+0x3a` 로 **항목 32바이트를 만들기 정보 `+0x230 + N×0x20` 에 복사** · `+0x43 INC [R8+0x250]` (개수) · 값이 포인터인 항목 (`+0x4c BT EAX,0x12` 꺼짐) 은 `+0x82 / +0x89` 로 본문을 `만들기 정보 + 0x258 + [+0x254]` 에 복사하고 `+0x8f` 로 항목의 `Value` (`+0x10`) 를 그 주소로, `+0x9b ADD [RBX+0x254],…` (본문 총 길이 += 8로 올림한 크기) | 만들기 정보 쪽 **개수 `+0x250` (dword) · 본문 길이 `+0x254` (dword) · 항목 `+0x230` · 본문 `+0x258`**. 항목 칸이 `+0x230 ~ +0x24f` (32바이트) 라서 **한 개만 들어간다** (둘째부터는 `+0x250` 을 덮음) — 번호 `0x22` 말고 `0x80000` 비트 가진 속성이 없다는 표 (sast/55) 와 맞음 `[정적: 같은 속성을 두 번 주는 것을 막는 줄은 안 봄]` |
| ③ | `PspSetupUserProcessAddressSpace` | `+0x115 ~ +0x11f` 크기 = `Length + EnvironmentSize` · `+0x124` (ret `+0x129`) 같은 스위치 · `+0x12b JZ +0x157` · **`+0x130` (ret `+0x135`) `PspCalculateUserAttributeListSize(만들기 정보)`** · `+0x13a ~ +0x150` 크기 = **(앞 크기 8바이트 올림) + 목록 크기** | 새 프로세스에 잡을 한 덩어리 크기에 **목록 자리**를 얹음. 그 함수 (rva `0x782ff4`, 34B, objdump 로 읽음): `[+0x250] == 0` 이면 0, 아니면 **`[+0x254] + 8 + [+0x250] × 0x20`** |
| ④ | `PspCopyAndFixupParameters` (rva `0xae57e4`) | `+0x19d` (ret `+0x1a2`) 같은 스위치 → 켜짐이면 이 갈래 (`+0x1a4 JZ +0x416` 이 안 뜀). `+0x324` (ret `+0x329`) `RtlCopyToUser` 로 매개변수 블록 (`R14` = `Length + EnvironmentSize`) 을 새 프로세스 `RDI` (= 만들기 정보 `+0xe0`) 에 복사, `+0x347` (ret `+0x34c`) PEB `+0x20` 에 `RDI` 쓰기. 그 뒤 **`+0x3c7 CMP [R15+0x250],R13D` (R13D = 0) · `+0x3ce JBE +0x52f`** (개수 0 이면 건너뜀) → `+0x3d4 ~ +0x3e2` `RBX = RDI + 7 + (R14 − ((R14−1)&7))` = **`RDI + (R14 를 8바이트로 올림)`** → **`+0x3eb` (ret `+0x3f0`) `PspCopyUserAttributes(RCX = RBX, R8 = 만들기 정보)`** | 목록은 **새 프로세스 안에서 매개변수 블록 + 환경 바로 뒤 (8바이트 올림)** 에 놓인다 — ③ 이 그만큼 자리를 잡아 둔 것 |
| ⑤ | `PspCopyUserAttributes` (rva `0x783124`) | `+0x1d MOV EAX,[R8+0x250]` (개수 N) · `+0x2f ~ +0x38` 계산 · `+0x4d ~ +0x6f` 항목마다 `Value` (`+0x10`) 를 "만들기 정보 본문 주소 → 새 프로세스 안 주소" 로 고침 (비트 18 꺼진 항목만) · **`+0x84 / +0x88` (ret `+0x8d`) `RtlCopyToUser(목적 + 8, 만들기 정보 + 0x230, N×0x20)`** · **`+0x94 / +0x9e` (ret `+0xa3`) `RtlCopyToUser(목적 + 8 + N×0x20, 만들기 정보 + 0x258, [+0x254])`** (본문) · `+0xa3 SUB RSI,R14` → **`+0xac` (ret `+0xb1`) `RtlWriteULong64ToUser(목적, 8 + N×0x20)`** | 새 프로세스 안에 **`PS_ATTRIBUTE_LIST` 모양** (머리 8바이트 = 총 길이, 이어서 항목 `0x20`B × N, 그 뒤 본문) 을 만든다 |
| ⑥ | `PspCopyAndFixupParameters` | **`+0x3f8 LEA RCX,[RDI+0x448]` · `+0x3ff MOV RDX,RBX` · `+0x402` (ret `+0x407`) `RtlWriteULong64ToUser`** → `+0x407 JMP +0x52f` | **새 프로세스 `ProcessParameters +0x448` (PDB `AttributeList`) = ④ 의 `RBX` (목록 주소).** 이어짐 확정 |
| ⑦ | ntdll `LdrpHandleUserCopyAttributeList` (sast/55) | `+0x15` `[PEB+0x20]` → `+0x448` 읽음 | 읽는 쪽. test.exe 는 0 이라 `+0x23 JZ +0x15b` |

- **스위치 꺼짐 갈래:** ④ 에서 `+0x1a4 JZ +0x416` 이 뛰면 (사용자에 먼저 복사하고 거기서 고치는 갈래, `+0x41f` ret `+0x424`) **`+0x3c7 ~ +0x402` 를 안 지난다** — `+0x448` 은 사용자 값 그대로. ① 도 같은 스위치로 막혀 `+0x250` 이 0 이라 어느 쪽이든 test.exe 는 `+0x448 = 0`.
- 값이 쪽 안에서 맞물리는 것 (확인용): ② 의 항목 `Value` 가 본문 주소 → ⑤ 가 `R9 = (목적 + 8 + N×0x20) − 만들기 정보 − 0x258` 만큼 더해 새 프로세스 안 주소로 바꿈 (`+0x2f ~ +0x38` · `+0x6c ADD RAX,R9`). `[정적]` 계산, 실제 값은 이 길을 탈 프로세스가 있어야 보임.
- **sast/104 · sast/55 와의 이음:** sast/104 의 "`AttributeList (+0x448)` 별도 줄 (`+0x3c7` > 0 일 때만 `+0x3f8`)" 이 위 ④·⑥ 이고, `+0x430 / +0x438` (`DefaultThreadpoolCpuSetMasks` / `Count`) 은 **속성 목록이 아니라 위 5번의 덮어쓰기 구조 `[R15+0x28]/[R15+0x30]`** 에서 온다 (둘은 별개 줄기).
- **지금 우리 (PE_emulator):** **맞음 (test.exe 한정).** `Src/nt/Process.cpp:621` `PROCESS_SEGMENT_HEAP_ENABLED = 0` = 위 꺼짐 갈래 (`PspCalculateUserAttributeListSize` 를 안 만든다는 주석과 같음), `Src/ntdll/LdrInit.cpp:1131 ~ 1144` 가 `+0x448` 을 읽고 0 이면 그냥 지나감 (목록이 있을 때만 "pending" 로그). 켜짐 갈래를 만들 날이 오면 ③ 크기식 `(8로 올림) + [+0x254] + 8 + N×0x20` 과 ⑥ 의 `+0x448` 쓰기를 같이 넣으면 된다.

### 8. `LdrpSuppressedExportOverrideListPtr` (`.data` `0x1d2920`) 를 읽는 곳 — **ntdll 안에는 없다** `[정적]`

- 훑은 것 (전수):
  1. 코드: `ntdll.dis` 에서 RIP 상대 목적지가 `[0x1d2920, 0x1d2928)` 인 줄 **1개** = `LdrpInitializeProcess +0x49d MOV [칸],RAX` (쓰기). 읽는 줄 0. (`_refs/LdrpSuppressedExportOverrideListPtr.tsv` 와 같음.)
  2. 데이터: ntdll 이미지 안 8바이트·4바이트 값이 이 칸을 가리키는 곳 **0**.
  3. 이웃 칸 기준 접근 (`LdrpAuditIntegrityContinuity` 다음 칸이라 `lea [이웃]` 뒤 `+0x8` 로 읽는 꼴): 이웃 이름 (`0x1d28f0 ~ 0x1d2930`) 을 가리키는 줄 전수를 봤는데 `+8` 식 접근 없음 (모두 각자 이름의 칸 접근).
  4. 내보내기: ntdll 내보내기 이름 2516개에 `Suppress`·`Override` 가 든 것 없음 (다른 모듈이 이름으로 읽을 수 없다). ntoskrnl · securekernel 본문에도 `LdrpSuppressed`·`SuppressedExport` 글 없음 (커널이 이름으로 찾아 읽을 일도 없다).
- 쓰기 줄: `LdrpInitializeProcess +0x48d` (ret `+0x492`) `LdrControlFlowGuardEnforcedWithExportSuppression` → `+0x492 CMP EAX,EBX` (`EBX`=1) · `+0x494 JNZ +0x4a4` → (반환 1 일 때만) `+0x496 LEA RAX,[0x1801740f0]` (`LdrpSuppressedExportOverrideList`) · **`+0x49d MOV [0x1801d2920],RAX`** (sast/55 와 같음).
- 목록 자체 (참고, 값만): `0x1740f0` 의 첫 8바이트 = `0x18013ae40` = ntdll 함수 **`RtlMapSecurityErrorToNtStatus`** 시작 (`_index.tsv` rva `0x13ae40`) — 내보내기 억제를 "덮어써서 풀어 주는" 함수 한 개짜리 목록으로 보임 `[추측: 이름 뜻]`. 바로 뒤 `0x1740f8` 부터는 다른 변수 (`UNICODE_STRING` `\Registry\Machine\System\CurrentControlSet\Control\Session Manager\kernel`, `RtlQueryImageMitigationPolicy +0x4d4`·`RtlpOpenSystemSessionKey +0x24` 가 읽음) 라서 이 목록의 길이는 8바이트로 보임.
- 결론: **이 칸은 이 빌드 ntdll 안에서 "쓰기만 있는 칸" (정적).** 읽는 쪽이 있다면 코드가 아니라 (디버거 확장이 쓰는 심볼 같은) **밖에서 이름으로 읽는 것** 일 가능성 `[추측]`. test.exe 에서는 CFG 비트맵이 없어 (`+0xb8 = 0`) 쓰기도 안 탄다 → 끝까지 0 (사진과 맞음).
- **지금 우리 (PE_emulator):** **맞음.** 0 으로 두고 읽는 곳이 없으니 값이 뭐든 동작 영향이 없다.

### 9. 덤 — 싸게 된 것만 (길어지는 것은 "다음 요청 후보")

**(a) 속성 `0x22` 의 공개 이름 = `PsAttributeHeapFeatures`** `[정적]` — PDB 열거형 `_PS_ATTRIBUTE_NUM` (ntoskrnl·ntdll 둘 다) 에 `PsAttributeHeapFeatures = 34` (`0x22`). sast/55 2-2 의 "PDB 에 없다 `[추측]`" 은 **틀림** (sast/104 f-2 가 이미 찾았음). 이름이 기능 스위치 `CreateProcessSegmentHeapEnablement` · 읽는 쪽 `RtlpHpOptIntoSegmentHeap` 과 맞는다 → "프로세스 만들 때 힙 기능 요청 (세그먼트 힙 쓰기 0/1)" 으로 확정해도 좋다. 인접 이름: `PsAttributeChpe = 26`, `PsAttributeMitigationOptions = 16`, `PsAttributeMitigationAuditOptions = 27`, `PsAttributeSveVectorLength = 32`, `PsAttributeContainmentConfiguration = 35`.

**(b) `LdrpMakePermanentImageCommit` (rva `0xa3e78`, 56B) 의 목적** `[정적: 동작] / [추측: 이유]`
- 동작: `(시작, 크기)` 를 받아 쪽 수 = `((시작 & 0xfff) + 0xfff + 크기) >> 12` 만큼 (`+0x0 ~ +0x1f`), 쪽마다 **`+0x24 MOV RAX,[RCX]` → `+0x27 MOV [RCX],RAX`** (쪽 맨 앞 8바이트를 읽고 **같은 값을 다시 씀**), `+0x2a LEA RCX,[RCX+0x1000]` · `+0x31 SUB R8,1` · `+0x35 JNZ +0x24`. 값은 안 바뀐다.
- 부르는 곳 둘: `LdrpLocateMrdata +0x4d` (ret `+0x52`, 자기 `.mrdata` 구역 — 바로 다음 `+0x52`·`+0x59` 가 `LdrpMrdataSize`·`Base` 를 씀) · `LdrpCfgProcessLoadConfig +0x144` (ret `+0x149`).
- 이유 (코드엔 안 적혀 있음 `[추측]`): 이 시점 `.mrdata` 는 아직 **쓰기 가능** (카운터 `LdrpMrdataUnprotected` 파일 값 1, sast/55) 이라 이미지 파일 매핑 쪽을 한 번씩 **써서** 복사본 (copy-on-write) 을 미리 만들어 두는 일로 보인다 — 뒤에 `.mrdata` 를 읽기 전용으로 바꿨다 다시 쓰기로 바꿀 때 (`LdrpChangeMrdataProtection`) 쪽을 새로 못 얻어 실패하는 일을 막으려는 "영구 확보" (이름의 `Permanent … Commit`). 쓴 값이 같아서 보이는 변화는 없다.
- **지금 우리:** 진짜 ntdll 코드가 도니 Unicorn 안에서 같은 읽기·쓰기가 일어날 뿐 따로 할 일 없음. 쪽을 "처음 쓸 때" 장부가 바뀌는 구조면 영향이 있을 수 있어 **확인 필요**.

**(c) `LdrpInitializeProcess +0xdc4` 의 `ECX` = 1 (닫기)** `[정적]`
- `+0xd4f MOV EAX,2` → `+0xd65 LEA EBX,[RAX+0x2e]` = `0x30` (`OBJECT_ATTRIBUTES.Length`) → `+0xdc1 LEA ECX,[RBX-0x2f]` = **`1`** → `+0xdc4` (ret `+0xdc9`) `LdrProtectMrdata(1)` = 닫기 (0 이 열기, 0 아니면 닫기). 앞 `+0xd9f XOR ECX,ECX` · `+0xda1` 가 열기 (ret `+0xda6`), 사이 `+0xdae LEA EDX,[RBX-0x2d]` (= 3, 접근 `DIRECTORY_QUERY|TRAVERSE`) · `+0xdb8` (ret `+0xdbd`) `NtOpenDirectoryObject(&LdrpKnownDllDirectoryHandle (.mrdata 0x1e7120), 3, &oa)`. 즉 **KnownDlls 디렉터리 핸들을 `.mrdata` 칸에 쓰려고 연 짝** 이다.

**(d) `LdrEnsureMrdataHeapExists` (rva `0xa716c`) 가 `LdrpMrdataHeap` 를 쓰기까지 가는 조건** `[정적]`
1. **`+0xa` (ret `+0xf`) `LdrControlFlowGuardEnforced()` ≠ 0** — 아니면 `+0x11 JZ +0x1f` 로 바로 0 반환. 그 함수는 `+0x0 CMP [LdrSystemDllInitBlock+0xb8],0` · `+0x8 JZ +0x17` (비트맵 포인터 0 이면 0) · `+0xa ~ +0x12` `(~[+0x9c]) & 1` (`CfgOverride` 비트가 꺼져 있어야 1).
2. `+0x13 MOV RAX,[LdrpMrdataHeap]` · `+0x1d JZ +0x2d` — **이미 만들어져 있으면 (≠ 0) `+0x1f` 로 0 반환**.
3. 만들기: `+0x61` (ret `+0x66`) `NtAllocateVirtualMemory(-1, …, 크기 = LdrpAllocationGranularity, MEM_RESERVE 0x2000, PAGE_READWRITE 4)` → `+0x8c` (ret `+0x91`) `RtlpCreateHeap` → `+0xa5` (ret `+0xaa`) `RtlAllocateHeap(힙, HEAP_ZERO 4, 4바이트)` → `+0xbd` (ret `+0xc2`) `RtlProtectHeap(힙, 1)` — 어디서든 실패하면 `+0x138` 쪽에서 `NtFreeVirtualMemory` 로 되돌리고 `STATUS_NO_MEMORY` (`+0x15f AND EAX,0xc0000017`).
4. `+0xc4` (ret `+0xc9`) `LdrProtectMrdata(0)` 열기 → `+0xd0` (ret `+0xd5`) `LdrpMrdataLock` 독점 잡기 → `+0xd5 MOV RAX,[LdrpMrdataHeap]` · `+0xe6 JNZ +0x10a` (그새 다른 스레드가 만들었으면 풀고 우리 걸 버림 `+0x10a ~ +0x133`) → 아니면 **`+0xe8 MOV [LdrpMrdataHeapUnprotected],RDI` · `+0xef MOV [LdrpMrdataHeap],RBX`** → `+0xf6` 풀기 → `+0x100` (ret `+0x105`) `LdrProtectMrdata(1)` 닫기.
- 부르는 곳 (모두 초기화 줄기 밖, 지도에서 `RtlAddGrowableFunctionTable` 만 깊이 7): `RtlAddGrowableFunctionTable +0x40` (ret `+0x45`) · `RtlSetProtectedPolicy +0x29` (ret `+0x2e`) · `RtlpAddVectoredHandler +0x23` (ret `+0x28`).
- **test.exe: 1번에서 막힌다.** `[관찰]` run44 사진 `LdrSystemDllInitBlock +0xb8 = 0` (sast/55) → `LdrControlFlowGuardEnforced = 0` → 힙을 안 만들고 쓰기도 없음 → `LdrpMrdataHeap`·`HeapUnprotected` 끝까지 0 (사진과 맞음). **지금 우리:** 맞음.

**(e) `.mrdata` 를 여닫는 `LdrProtectMrdata` 호출 자리 전수 — 50자리 / 함수 21개** `[정적]` (인자는 호출 앞 `ECX` 만든 줄로 가름: `XOR ECX,ECX` = 열기 · `MOV ECX,1` 등 0 아님 = 닫기. 셈은 `call +0x..` = CALL 이 있는 자리)

- 규칙 (sast/55 와 같음): 카운터 `LdrpMrdataUnprotected` (파일 값 1) 가 **1 → 0 / 0 → 1 로 넘을 때만** `NtProtectVirtualMemory` (읽기 전용 / 읽기 쓰기). 초기화 안에서 카운터가 0 이 되는 첫 자리가 `LdrpInitializeProcess +0x488`.
- **초기화 줄기 안 (지도 `LdrInitializeThunk` 에 있음)** — `LdrpInitializeProcess` 몸통 순서 (호출 오프셋 순):

| 순 | 자리 | 열기 → 닫기 | 타는 코드가 있나 (test.exe, 정적 조건 기준 — 실행 여부 아님) | 안에서 쓰는 `.mrdata` 칸 |
|---|---|---|---|---|
| 1 | `LdrpInitializeProcess +0x415` → `RtlInsertInvertedFunctionTable` | `+0x4b` 열기 · `+0x67` 닫기 (함수 안) | 탐 | `LdrpInvertedFunctionTables` 칸 (sast/55) |
| 2 | `LdrpInitializeProcess +0x441` (열기) · `+0x470` → `RtlRemoveInvertedFunctionTable` (`RtlxRemove… +0x37` 열기 · `+0x48` 닫기) · `+0x47c` (닫기, ret `+0x481`) · `+0x481` `LdrpCaptureCriticalThunks` · **`+0x488` (닫기 → `NtProtectVirtualMemory`)** | 탐 | sast/55 6단계 |
| 3 | `LdrpInitializeProcess +0x531` → `LdrpInitializeExecutionOptions` | 안 `+0x373` 열기 · `+0x384` 닫기 | **안 탐** — IFEO 값 하나 (글 rva `0x18019c360`) 가 비트 0 일 때만 (`+0x36a TEST [RSP+0x40],1`) `RtlGuardAllowSuppressedCalls` (`0x1e7042`) = 1 을 씀 | `RtlGuardAllowSuppressedCalls` |
| 4 | `LdrpInitializeProcess +0xda1` (열기) · `+0xdb8` `NtOpenDirectoryObject` · `+0xdc4` (닫기, **`ECX` = 1**) | 탐 | `LdrpKnownDllDirectoryHandle` (`0x1e7120`) |
| 5 | `LdrpInitializeProcess +0x1277` → `RtlInitializeHistoryTable` | 안 `+0x17` 열기 · `+0xb4` **꼬리 점프** 로 닫기 (`JMP LdrProtectMrdata`) | 탐 (조건 없음) | `RtlpUnwindHistoryTable` (`0x1e7350 ~`: `+0x0` 개수, `+0x5` 바이트, `+0x10` 칸 … 을 씀) |
| 6 | `LdrpInitializeProcess +0x1713` → `AVrfInitializeVerifier` | 안 `+0x1b5`/`+0x1f1`, `+0x40d`/`+0x41b` | App Verifier 켠 프로세스만 `[추측: 조건 안 봄]` | |
| 7 | `LdrpInitializeProcess +0x17c1` → `LdrpLoadWow64` | 안 `+0xc3` / `+0x143` | WoW64 프로세스만 (sast/100: x64 test.exe 는 0) | |
| 8 | `LdrpInitializeProcess +0x1806` → `LdrpInitializeCfgScpHelpers` | 안 `+0x5a` 열기 · `+0x80` 닫기 (`LdrpGuardCheckIcallNoESFptr` · `LdrpGuardDispatchIcallNoESFptr` 를 씀) | **안 탐** — `+0x33` (ret `+0x38`) `LdrControlFlowGuardEnforced` 가 0 (test.exe `+0xb8 = 0`) 이면 `+0x3a JZ +0x85` | CFG 도우미 포인터 둘 |
| 9 | DLL 올릴 때마다 `RtlInsertInvertedFunctionTable` (`+0x4b` / `+0x67`) | 탐 (모듈 수만큼) | 표 칸 |

- **초기화 줄기 밖 (프로그램이 부를 때)**: `RtlAddFunctionTable` (`+0x146` / `+0x236`) · `RtlInstallFunctionTableCallback` (`+0x15e` / `+0x2c3`) · `RtlAddGrowableFunctionTable` (`+0x160` / `+0x29f`) · `RtlDeleteFunctionTable` (`+0xf` / `+0x93`·`+0xcb`) · `RtlDeleteGrowableFunctionTable` (`+0x5b` / `+0x173`) · `RtlpAddVectoredHandler` (`+0x189` / `+0x29b`) · `RtlpCallVectoredHandlers` (`+0x233` / `+0x26c`) · `RtlSetProtectedPolicy` (`+0x12e`·`+0x268` 열기 / `+0x17d` `LEA ECX,[RAX+1]`) · `LdrpGetShimEngineInterface` (`+0x48f` / `+0x560`) · `LdrEnsureMrdataHeapExists` (`+0xc4` / `+0x100`·`+0x114`) · `RtlInitializeNtUserPfn` (`+0x85` / `+0xd0`) · `RtlResetNtUserPfn` (`+0x23` / `+0x65`).
- **인자를 한 줄로 못 가른 자리 (직접 확인 필요):** `RtlpRemoveVectoredHandler +0x181` (앞 `MOV ECX,0xe` 는 `INT 0x29` 코드로 보임) · `+0x209` 닫기 · `RtlResetNtUserPfn +0x65` (`LEA ECX,[RDX-0x17]`) · `RtlAddFunctionTable +0x236` (`MOV ECX,R14D`) · `AVrfInitializeVerifier` 의 `EDI` · `LdrpGetShimEngineInterface +0x560` 의 `ESI`.
- 맨 앞 줄기 (1~5, 8·9 의 조건) 가 test.exe 초기화 몫이고, 나머지는 DLL 이 `RtlAddFunctionTable` 같은 것을 부를 때다 (정적으로는 "부를 수 있다").
- **지금 우리 (PE_emulator):** **확인 필요.** `Src/ntdll/LdrInit.cpp:1250 ~ 1252` 가 `LdrProtectMrdata` 를 C++ 로 짜 두었고 `:1402` 가 `+0x441` (열기) 을 부른다. 위 표의 4 (`+0xda1 / +0xdc4`) · 5 (`+0x1277`) 가 우리 몸통에 같은 순서로 있는지, `NtProtectVirtualMemory` 가 `.mrdata` 로 몇 번 오는지 (1·0 넘는 자리 = 위 2 의 `+0x488`, 4 · 5 의 열기/닫기 짝마다 2번씩) 는 `LdrInit.cpp` 에서 다시 맞춰 봐야 한다.

---

## baggage 줄 (flow-writer 몫, 있으면)

| 자리 | 바꿀 것 | 근거 |
|---|---|---|
| 3.9 `LdrSystemDllInitBlock` `+0xb0` (1 단계 ①) | 출처 문구: "입력 표가 갖고 온 값 `[공백]`" → **"`PspAllocateProcess +0x1e03 ~ +0x1e15` (비트 12~13 := 2, 무조건) · `+0x1e1d ~ +0x1e30` (비트 24~25 := 2, `KiCoreIsolationEnforceable` = 0 일 때) · `PspApplyMitigationOptions +0x510 ~ +0x523` (비트 0~1 := 2)"** | Q113 4 |
| 3.9 `+0xa0 ~ +0xb7` 줄 | "부모 값" 말 → "시스템 전역 `PspSystemMitigationOptions` (레지스트리 `Kernel\MitigationOptions`) · IFEO · 사용자 속성" 순서로 `PspInheritMitigationOptions` 두 번 | Q113 4-2 |
| 만들기 정보 `+0x1d8` 줄 (sast/54 표) | "읽는 쪽 `[공백]`" → **읽는 곳 둘: `PspCaptureProcessParameters +0x26`, `PspDeleteCreateProcessContext +0x11a`** | Q113 5 |
| 3.8 `.data` — 새 줄 둘 | `LdrpEnforceIntegrityContinuity` (`+0x4db`) · `LdrpAuditIntegrityContinuity` (`+0x4fe`), 둘 다 `끝까지 0`, "0 을 씀". 7.3 목록 · 7.5 줄 2077 에서 뺌. **ntdll 전역 아는 189 → 191, 모름 304 → 302** | Q113 6 · sast/111 1j |
| 3.2 매개변수 `+0x448` `AttributeList` | 쓰는 줄 ① ~ ⑥ 사슬 (`PspCopyAndFixupParameters +0x3eb / +0x3f8 ~ +0x402`) 한 줄, 켜짐 갈래만 (test.exe 는 0) | Q115 7 |
| 3.8 `LdrpSuppressedExportOverrideListPtr` | "읽는 쪽 `[공백]`" → "읽는 곳 없음 (ntdll 전수)" | Q115 8 |
| 3.8 `.data` `0x1ce268` 줄 (sast/55 2-1) | 속성 `0x22` = `PsAttributeHeapFeatures` | Q115 9 (a) |
| 이름 보강 | `0x40000036` = `STATUS_IMAGE_AT_DIFFERENT_BASE` (sast/100 ④ 의 `[추측: 기억]` → `[정적]`), `0xc000090b` = `STATUS_BAD_DATA` | Q112 1 |

---

## 동적분석으로 확인해야 할 것 (선택)

필수는 없다. 정적 답이 코드·바이트로 닫혔다.
- (Q113 4, 사진 `0x02002002` 가 위 세 줄과 맞는지) `nt!PspAllocateProcess +0x1e15` (= `nt+0xa04380`, 쓰기 직후) 와 `+0x1e30` 뒤 `dq @rsp+0x400 L3` 의 셋째 값 하위 32비트: `0x…2000` 꼴 (비트 13) 인지, `+0x1e22` 뒤 `al` (= `KiCoreIsolationEnforceable` 의 `dd nt+0xfc6ed0`) 이 0 인지. `nt!PspApplyMitigationOptions +0x5f4` 복귀 뒤 셋째 값 = `0x02002002`.
- (Q113 5, Q114 (4) 와 겹침) `dq nt!__imp_PsGetProcessParameterOverrides L1` 이 스텁 (`ret 0`, rva `0x4a10c0`) 인지 → `PspGetProcessParameterOverrides +0x4a` 에서 `RAX = 0` 이 쓰이는지.
- (Q113 4-2) 레지스트리 `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel` 값 `MitigationOptions` 존재 여부와 `dq nt!PspSystemMitigationOptions L3` (이 VM 의 시스템 값 — 사진의 앞 두 칸이 0 인 것과 맞는지).
- (Q115 9 (e)) test.exe 한 번에서 `NtProtectVirtualMemory` 가 `.mrdata` 로 몇 번 오는지 (조건 test.exe, 주소 = `ntdll+0x1e7000`, 크기 `0x35b8`, 새 보호 2 / 4): 위 표 2 의 `+0x488` 에서 1번, 4 · 5 짝에서 각 2번씩 + DLL 올릴 때 `RtlInsertInvertedFunctionTable` 짝.

추천 멈춤점 (`ntdll!`, 조건 test.exe): `LdrpInitializeProcess +0xda1` (`+0xda6` 복귀 뒤 `NtProtectVirtualMemory` 로 열렸는지), `+0xdc4`, `+0x1277` (`RtlInitializeHistoryTable` 입구). 커널 쪽은 위 첫 줄.

## 다음 요청 후보

1. `LdrpSuppressedExportOverrideList` 의 실제 길이·소비자: 목록 첫 칸이 `RtlMapSecurityErrorToNtStatus` 인 이유와, 이 칸을 읽는 쪽이 정말 없는지 (kernelbase·kernel32·ntdll 외 모듈 `.dis` 로 같은 모양 훑기). (작음)
2. `KeOptimizeSpecCtrlSettings` (`HvlHypervisorConnected` · `HvlpFlags` · `HvlpSchedulerType` 가 `KiCoreIsolationEnforceable` 을 정하는 식) — 에뮬레이터가 이 VM 의 비트 24~25 를 따로 정하려면 필요. (작음)
3. `PspSystemMitigationOptions` 를 레지스트리에서 채우는 함수 (INIT 표 `0xc825a0` 을 읽는 쪽) 와 그 값이 `0x02002002` 에 영향이 없다는 확인 (지금은 "전역 첫 두 칸 0" 만 사진으로 앎).
4. `.mrdata` 여닫기의 init 안 `NtProtectVirtualMemory` 수 (표 9 (e)) — 동적 한 번으로 끝나면 dast 에.
5. (PE_emulator 가 켜짐 갈래를 만들 때) `PspCopyAndFixupParameters` 의 속성 목록 줄기 (④ ~ ⑥) 와 `PspProcessUserCopyAttribute` 의 "한 개만 들어간다" 확인 (`PspBuildCreateProcessContext` 가 같은 속성을 두 번 받는 것을 막는지).

## 판정

**분석성공.** 요청서 질문 9개 (Q112 3 · Q113 3 · Q115 3) 에 전부 `.asm` 줄·바이트 근거와 함께 답했다. PDB GUID 일치, 버전 대조 일치, 새 Ghidra 실행 0번. 새로 알아낸 것 / 기존 문서를 고치는 것:
- `0xc000090b` = `STATUS_BAD_DATA`, `0x40000036` = `STATUS_IMAGE_AT_DIFFERENT_BASE` — 같은 빌드 다른 바이너리 표에서 `[정적]` 확정 (이름 찾는 새 길).
- sast/54 질문 13 정정: 완화 옵션 셋째 칸의 "2" 는 입력이 아니라 `PspAllocateProcess` 가 쓴다. sast/104 이름 정정: `PspCaptureUserProcessParameters` 의 `R15` 는 만들기 정보가 아니라 `+0x1d8` 덮어쓰기 구조. sast/55 정정: 속성 `0x22` 는 PDB 에 이름 있음 (`PsAttributeHeapFeatures`).
부분 답은 없음 — 단 Q115 9 (b) 의 "이유" 와 Q115 8 의 "읽는 쪽이 밖에 있을 가능성" 은 `[추측]`, (e) 일부 인자는 "직접 확인 필요" 로 표시.

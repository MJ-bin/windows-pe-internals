# 2026-11-16 (작성일) run123 — run58 · 59 · 60 남은 작은 정적 (Q112 + Q113 + Q115, 정적만): 시작 정보 블록 옵션 셋째 칸 (`+0xb0`, 사진 `0x02002002`) 의 비트 12~13 · 24~25 "2" 는 **입력이 아니라 `PspAllocateProcess` 가 그 자리에서 쓴다** (`+0x1e03 ~ +0x1e15` 무조건 · `+0x1e1d ~ +0x1e30` 코어 격리 정책이 안 될 때). 매개변수 `+0x448` (`AttributeList`) 이음 줄 ① ~ ⑥ 끝까지, `LdrpSuppressedExportOverrideListPtr` 를 읽는 곳은 ntdll 안에 **없다**, `0xc000090b` = `STATUS_BAD_DATA`

(글의 오프셋은 함수 안 줄 · call 자리. ret 는 아래 "정적 — 호출 지점" 표. 콜스택으로 본 적은 없다 — 정적만.)

> **지도 범위**: 이번 바퀴는 이미 있는 상자 **안쪽** 줄만 늘었다 — 2.1 `PspAllocateProcess` 완화 옵션 · `PspPrepareSystemDllInitBlock` · `PspCopyAndFixupParameters`, 2.4 `LdrpInitializeProcess` 묶음 3 뒤 (`.mrdata` · CFG) · 레지스트리 두 함수. 새 단계 · 새 caller 가 없어 **그림은 안 바꿨다.** 글 (flow.md 기준 · 2.1 · 2.4 · 2.6 · 4장 · 5장) 과 짐 지도 (기준 · 2장 메모 · 3.2 · 3.8 · 3.9 · 3.11 · 6장 · 7.4 · 7.6 · 7.19 · 7.20 · 7.36 · 새 7.53 · 8장) 를 고쳤다. **짐 칸 · 줄 · 2장 숫자 그대로** (아래 "Q113 (c) 셈 판단").

## 요약

| 대기열 | 물음 | 답 | 표시 |
|---|---|---|---|
| Q112 (1) | `0xc000090b` 이름 | **`STATUS_BAD_DATA`** — PDB 엔 없음, 같은 빌드 `nltest.exe` · `certca.dll` · `tdhres.dll` 안 "값 → 이름" 표 + ntdll 메시지 글 "Bad data." | [정적] |
| Q112 (2) | 커널 사본 `RtlpQueryNlsSystemCodePages` 글 | ntdll 것과 **같다** (키 `…\Control\Nls\CodePage`, 값 `ACP` · `OEMCP`). 다른 건 `Zw*` · `RtlInitUnicodeString` 호출 꼴뿐 | [정적] |
| Q112 (3) | 같은 `.rdata` 글을 쓰는 다른 ntdll 함수 | **없다** — 글 8개의 쓰는 자리는 두 함수 안 `LEA` 8줄이 전부, 데이터 포인터 0 | [정적] |
| Q113 (a) | 옵션 셋째 칸 "2" 출처 · 32 ~ 39 이름 | 비트 12~13 = `PspAllocateProcess +0x1e03 ~ +0x1e15` **무조건**, 비트 24~25 = `+0x1e1d ~ +0x1e30` (`KeIsCoreIsolationMitigationPolicyEnforceable` 거짓일 때), 비트 0~1 = `PspApplyMitigationOptions +0x510 ~ +0x523` (run59 그대로). 이름은 PDB `PS_MITIGATION_OPTION` (32 ~ 39 표 아래) | [정적] |
| Q113 (b) | 만들기 정보 `+0x1d8` 을 읽는 함수 | **둘** — `PspCaptureProcessParameters +0x26` · `PspDeleteCreateProcessContext +0x11a` | [정적] |
| Q113 (c) | 결과서 밖 전역 셋 2장 셈 | 근거만 옮김 (sast/111 · run68). **셈은 안 바꿈** — run114 가 이미 셌다 (아래) | [정적] |
| Q115 (a) | `PspCopyUserAttributes` → `+0x448` | **닫힘** — 사슬 ① ~ ⑥, 끝은 `PspCopyAndFixupParameters +0x3f8 ~ +0x402` | [정적] |
| Q115 (b) | `LdrpSuppressedExportOverrideListPtr` 읽는 곳 | **ntdll 안에 없다** (RIP 상대 · 데이터 포인터 · 이웃 칸 `+8` · 내보내기 모두 0) | [정적] |
| Q115 (c) | 덤 다섯 | 속성 `0x22` = **`PsAttributeHeapFeatures`** (PDB) · `LdrpMakePermanentImageCommit` = 쪽마다 "읽고 그대로 다시 쓰기" · `+0xdc4` `ECX` = **1** · `LdrEnsureMrdataHeapExists` 는 CFG 켜짐 + 힙 없음일 때만 · `.mrdata` 여닫는 자리 50 / 함수 21 표 | [정적] (이유 둘은 `[추측]`) |

- 덤: `0x40000036` = **`STATUS_IMAGE_AT_DIFFERENT_BASE`** · `0x40000003` = `STATUS_IMAGE_NOT_AT_BASE` 도 같은 표로 `[정적]` 확정 (run106 의 `[추측: 기억]` 닫힘).
- 이전 문서 정정 넷 (run50 · run59 · run60 · run104), 닫힘 셋 (run58 · run59 · run106 — 아래 "정정").

## 환경

- 26100.9457, **정적만** (VM 실행 없음, dast 없음).
- 03_sprint 여든한째 바퀴. plan **Q112 + Q113 + Q115** (run58 · 59 · 60 남은 작은 정적 — 사이 Q114 는 동적이라 이번 바퀴 아님). 사용자 결정 2026-11-15 "남은 정적 분석 전부" 의 차례. **짐 지도 바퀴.**
- 정적: ntoskrnl (`ntkrnlmp.pdb`) `C29EBFB06B78B3C020DCA66D99713F9E1` · ntdll `C3093720177DA6851519DCFFE8DF53FE1` (flow.md 기준과 같음, 결과서 대조).
- 버전 대조 (결과서): `NtCreateUserProcess +0xc31/+0xc36 → PspAllocateProcess` · `PspInsertThread +0x802/+0x807` · `LdrpInitializeProcess +0x1bb6/+0x1bbb` · `_LdrpInitialize +0x167/+0x16c` — 알려진 값과 같음.
- **새 Ghidra 실행 0번, 새로 뽑은 함수 0개.** 있던 `.asm` 만 읽음 + 전수는 스크래치 `ntdll.dis` · `nt.dis` (objdump). 이름 찾기용으로 `nltest.exe` · `certca.dll` · `tdhres.dll` 은 바이트만 읽음 (결과서).

## 목적

- run58 (sast/53) · run59 (sast/54) · run60 (sast/55) 가 남긴 작은 정적 셋을 한 바퀴로 닫는다:
  - Q112: 상태 이름 · 커널 사본 글 · 같은 글 쓰는 다른 함수.
  - Q113: 옵션 셋째 칸 "2" 출처 · 옵션 이름 · 만들기 정보 `+0x1d8` 읽는 쪽 · 결과서 밖 전역 셋 셈.
  - Q115: `+0x448` 이음 · `LdrpSuppressedExportOverrideListPtr` 읽는 곳 · 덤.

## 방법

- 정적: `_workspace/sast/119_run58-60-leftovers.md` — ntoskrnl · ntdll (kernelbase `CreateProcessInternalW` 곁눈질 한 줄).
- 결과서: `_workspace/sast/119_run58-60-leftovers_result.md` (**분석성공**, 스크립트 문제 없음, 동적 필수 없음).
- 결과서 도구: 스크래치 `s119/` (`xref.py` RIP 상대 참조 · `ptrscan.py` 이미지 안 포인터 · `rd.py` 글 읽기) — 레포 밖, 결과 파일 안 남김.
- flow-writer 가 `.asm` 으로 다시 본 줄 (결과서와 모두 같음):
  - ntoskrnl `PspAllocateProcess +0x1a8b · +0x1aea · +0x1be0 · +0x1c82 · +0x1c87 · +0x1deb · +0x1e03 ~ +0x1e30 · +0x1eb3 · +0x1f37 · +0x1f50`
  - `PspCaptureProcessParameters +0x15 · +0x17 · +0x26 · +0x3a · +0x58`, `PspDeleteCreateProcessContext +0x11a · +0x121`, `PspGetProcessParameterOverrides +0x4a`
  - `PspCopyAndFixupParameters +0x19d · +0x1a4 · +0x324 · +0x347 · +0x3c7 · +0x3ce · +0x3eb · +0x3f8 ~ +0x407`, `PspCopyUserAttributes +0x1d · +0x84 ~ +0xac`, `PspSetupUserProcessAddressSpace +0x115 · +0x124 · +0x12b · +0x130`, `PspBuildCreateProcessContext +0x1577 ~ +0x159f`
  - `RtlpQueryNlsSystemCodePages` (nt) `+0x48 · +0x5f · +0x93 · +0xa3 · +0xdf · +0x11c · +0x14f · +0x189 · +0x19e`
  - `PspApplyMitigationOptions +0x3da · +0x500 ~ +0x523 · +0x561 ~ +0x5a5 · +0x5d2 ~ +0x5f4 · +0x6b0 ~ +0x6ba` (마지막은 flow-writer 가 더 본 것 — 아래 "Q113 덤")
  - ntdll `LdrpInitializeProcess +0x12d ~ +0x134 · +0x415 ~ +0x49d · +0x531 · +0xd4f · +0xd65 · +0xd9f ~ +0xdc4 · +0x1277 · +0x1713 · +0x17c1 · +0x1806`, `LdrEnsureMrdataHeapExists +0xa ~ +0x1d · +0xc4 · +0xe8 · +0xef · +0x100`, `LdrpMakePermanentImageCommit +0x24 · +0x27`, `RtlpGetNtProductTypeFromRegistry +0x1b0 · +0x1b5`
- 사진 대조: run44 덤프 읽기 로그 8장의 `LdrSystemDllInitBlock +0xa0 ~ +0xb8` 줄 (아래 "도구 메모").

## 결과

비유 (가게 개업, 앞 run 들 이어서):

- 관리사무소 (커널) 가 새 가게 (프로세스) 의 **안전 규칙 카드** (완화 옵션 24바이트) 를 만든다. 칸마다 "켬 · 끔 · 모름" 을 적는데, 셋째 줄의 "끔" 두 개는 손님이 낸 서류에서 온 게 아니라 **사무소가 마지막에 도장으로 직접 찍는다** — 하나는 늘, 하나는 이 건물에 그 설비가 없을 때.
- 가게 서류철 (매개변수 블록) 맨 끝 칸 `+0x448` 은 "손님이 따로 맡긴 부탁 목록" 자리다. 사무소가 목록을 서류철 바로 뒤에 붙이고 그 자리를 칸에 적는 줄을 끝까지 찾았다. 이 기능 스위치가 꺼져 있어서 test.exe 가게엔 목록이 없다.

### 정적 — 호출 지점 (`_calls.tsv` · `.asm` `CALL … ret=` 줄 · 결과서)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!ExInitializeNls` | `+0x79` | `+0x7e` | `nt!RtlpQueryNlsSystemCodePages` (커널 사본) | [정적] |
| `nt!RtlpQueryNlsSystemCodePages` | `+0x5f` · `+0xb9` · `+0x127` | `+0x64` · `+0xbe` · `+0x12c` | `nt!RtlInitUnicodeString` (키 · `ACP` · `OEMCP`) | [정적] |
| `nt!RtlpQueryNlsSystemCodePages` | `+0x93` · `+0xdf` · `+0x14f` · `+0x189` | `+0x98` · `+0xe4` · `+0x154` · `+0x18e` | `nt!ZwOpenKey` · `ZwQueryValueKey` ×2 · `ZwClose` | [정적] |
| `nt!RtlpQueryNlsSystemCodePages` | `+0x111` · `+0x17e` | `+0x116` · `+0x183` | `nt!RtlUnicodeStringToInteger` (10진) | [정적] |
| `nt!RtlGetNtProductType` · `nt!PspSiloInitializeUserSharedData` | `+0x51` · `+0x12a` | — (결과서에 없음) | `nt!RtlpGetNtProductTypeFromRegistry` (커널 사본, 결과서 곁 사실) | [정적] |
| `nt!PspAllocateProcess` | `+0x1a8b` | `+0x1a90` | `nt!PspReadIFEOMitigationOptions` | [정적] |
| `nt!PspAllocateProcess` | `+0x1aea` · `+0x1be0` | `+0x1aef` · `+0x1be5` | `nt!PspInheritMitigationOptions` (시스템 × IFEO · 앞 결과 × 사용자 속성) | [정적] |
| `nt!PspAllocateProcess` | `+0x1c82` | `+0x1c87` | `nt!PspHardenMitigationOptions` (셋째 칸 안 건드림) | [정적] |
| `nt!PspAllocateProcess` | `+0x1c87` | `+0x1c8c` | `nt!KeIsUserCetAllowed` | [정적] |
| `nt!PspAllocateProcess` | `+0x1e1d` | `+0x1e22` | `nt!KeIsCoreIsolationMitigationPolicyEnforceable` (11B 잎) | [정적] |
| `nt!PspAllocateProcess` | `+0x1eb3` | `+0x1eb8` | `nt!PspApplyMitigationOptions` | [정적] |
| `nt!PspBuildCreateProcessContext` | `+0xb55` | `+0xb5a` | `nt!PspValidateMitigationOptions` (사용자 속성 16) | [정적] |
| `nt!PspApplyMitigationOptions` | `+0xe9d` | `+0xea2` | `nt!PspSetRedirectionTrustPolicy` (옵션 37 = 1 일 때) | [정적] |
| `KERNELBASE!CreateProcessInternalW` | `+0x22a3` | `+0x22aa` | `GetEmbeddedImageMitigationPolicy` (박힌 정책 있을 때만 속성 `0x20010`) | [정적] (결과서 곁눈질) |
| `nt!NtCreateUserProcess` | `+0xab3` | `+0xab8` | `nt!PspGetProcessParameterOverrides` (`+0x4a` 가 `+0x1d8` 쓰기) | [정적] (run59) |
| `nt!NtCreateUserProcess` | `+0xac6` | `+0xacb` | `nt!PspCaptureProcessParameters` (`+0x26` 이 `+0x1d8` 읽기) | [정적] |
| `nt!PspCaptureProcessParameters` | `+0x3a` | `+0x3f` | `nt!PspCaptureUserProcessParameters` (셋째 인자 = `+0x1d8` 값) | [정적] |
| `nt!PspDeleteCreateProcessContext` | `+0x121` | `+0x126` | `nt!PspDestroyProcessParameterOverrides` (`+0x11a` 가 `+0x1d8` 읽기, 0 이면 아무것도 안 함) | [정적] |
| `nt!NtCreateUserProcess` · `nt!PspBuildCreateProcessContext` · `nt!NtCreateThreadEx` | `+0x11d0` · `+0x188e` · `+0x2d8` | 각 `+5` | `nt!PspDeleteCreateProcessContext` | [정적] |
| `nt!PspBuildCreateProcessContext` | `+0x1577` | `+0x157c` | `Feature_CreateProcessSegmentHeapEnablement…IsEnabledDeviceUsageNoInline` | [정적] |
| `nt!PspBuildCreateProcessContext` | `+0x159f` | `+0x15a4` | `nt!PspProcessUserCopyAttribute` (`0x80000` 비트 항목만) | [정적] (run60) |
| `nt!PspSetupUserProcessAddressSpace` | `+0x124` · `+0x130` | `+0x129` · `+0x135` | 같은 스위치 · `nt!PspCalculateUserAttributeListSize` | [정적] |
| `nt!PspCopyAndFixupParameters` | `+0x19d` | `+0x1a2` | 같은 스위치 (꺼짐 → `+0x1a4 JZ +0x416`) | [정적] (run32) |
| `nt!PspCopyAndFixupParameters` | `+0x324` · `+0x347` | `+0x329` · `+0x34c` | `nt!RtlCopyToUser` · `nt!RtlWriteULong64ToUser` (블록 복사 · PEB `+0x20`) | [정적] (run32) |
| `nt!PspCopyAndFixupParameters` | `+0x3eb` · `+0x402` | `+0x3f0` · `+0x407` | `nt!PspCopyUserAttributes` · `nt!RtlWriteULong64ToUser` (`+0x448`) | [정적] (run60 · run104) |
| `nt!PspCopyUserAttributes` | `+0x88` · `+0x9e` · `+0xac` | `+0x8d` · `+0xa3` · `+0xb1` | `RtlCopyToUser` (항목) · `RtlCopyToUser` (본문) · `RtlWriteULong64ToUser` (머리 길이) | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x12f` | `+0x134` | `ntdll!RtlEncodePointer` (→ `RtlpUnhandledExceptionFilter`) | [정적] (run18 · run59) |
| `ntdll!LdrpInitializeProcess` | `+0x48d` | `+0x492` | `ntdll!LdrControlFlowGuardEnforcedWithExportSuppression` | [정적] (run60) |
| `ntdll!LdrpInitializeProcess` | `+0xda1` · `+0xdb8` · `+0xdc4` | `+0xda6` · `+0xdbd` · `+0xdc9` | `LdrProtectMrdata(0)` · `NtOpenDirectoryObject` · **`LdrProtectMrdata(1)`** | [정적] |
| `ntdll!LdrpLocateMrdata` · `ntdll!LdrpCfgProcessLoadConfig` | `+0x4d` · `+0x144` | `+0x52` · `+0x149` | `ntdll!LdrpMakePermanentImageCommit` | [정적] |
| `ntdll!LdrEnsureMrdataHeapExists` | `+0xa` | `+0xf` | `ntdll!LdrControlFlowGuardEnforced` (0 이면 `+0x11 JZ +0x1f`) | [정적] (run63) |
| `ntdll!LdrEnsureMrdataHeapExists` | `+0x61` · `+0x8c` · `+0xa5` · `+0xbd` | `+0x66` · `+0x91` · `+0xaa` · `+0xc2` | `NtAllocateVirtualMemory` · `RtlpCreateHeap` · `RtlAllocateHeap` · `RtlProtectHeap` | [정적] |
| `ntdll!LdrEnsureMrdataHeapExists` | `+0xc4` · `+0x100` | `+0xc9` · `+0x105` | `LdrProtectMrdata(0)` · `(1)` | [정적] |
| `ntdll!RtlAddGrowableFunctionTable` · `RtlSetProtectedPolicy` · `RtlpAddVectoredHandler` | `+0x40` · `+0x29` · `+0x23` | `+0x45` · `+0x2e` · `+0x28` | `ntdll!LdrEnsureMrdataHeapExists` | [정적] |

### Q112 — run58 남은 것

**(1) `0xc000090b` = `STATUS_BAD_DATA`** `[정적]`

- PDB (ntdll · ntoskrnl) 엔 NTSTATUS 이름이 없다 (sast/100 과 같음).
- ntdll 메시지 표: `0xc000090b` = "Bad data.", 이웃 `0xc000090a` "Bad key." · `0xc000090c` "Key does not exist.".
- 이름은 같은 빌드 System32 안 **"값 → 이름" 16바이트 칸 표** 셋에서 바이트로 읽음: `nltest.exe` (rva `0x68ce0`) · `certca.dll` (`0x557c0`) · `tdhres.dll` (`0x72ce0`). 셋이 서로 독립인데 값 · 이름이 같다 (이웃 `STATUS_BAD_KEY` · `STATUS_NO_KEY` 도).
- 이 요청의 뜻: 제품 종류 값이 `REG_SZ` 가 아니거나 짧거나 `WinNt` · `LanmanNt` · `ServerNt` 셋 다 아니면 `RtlpGetNtProductTypeFromRegistry +0x1b0 MOV EBX,0xc000090b` → `+0x1b5 JMP` (run58 7번).
- test.exe 는 KUSD `ProductTypeIsValid` = 1 이라 이 길을 안 탄다 (run58 · run44 `[관찰]`).
- 쓰는 곳 (`mov reg,0xc000090b` 전수, 결과서): ntdll 6곳 · ntoskrnl 9곳 (이름은 결과서).
- 덤: **`0x40000036` = `STATUS_IMAGE_AT_DIFFERENT_BASE`** (`nltest.exe` rva `0x64770`), `0x40000003` = `STATUS_IMAGE_NOT_AT_BASE` (`0x64440`). `0xc0000017` = `STATUS_NO_MEMORY` 로 표 읽기 검산.

**(2) 커널 사본 `RtlpQueryNlsSystemCodePages` (rva `0x78af7c`, 452B) — ntdll 것과 글이 같다** `[정적]`

| 쓰임 | 커널 줄 | 글 | ntdll (run58) |
|---|---|---|---|
| 키 | `+0x48 LEA RDX` → `+0x5f` | `\Registry\Machine\System\CurrentControlSet\Control\Nls\CodePage` | 같음 (`0x180ad0`) |
| 값 1 | `+0xa3 LEA RDX` → `+0xb9` | `ACP` | 같음 (`0x180b50`) |
| 값 2 | `+0x11c LEA RDX` → `+0x127` | `OEMCP` | 같음 (`0x180b58`) |

- 모양도 같다: 시작 때 두 칸 `0xfde9`, 속성 `0x240`, 접근 `0x80000000`, 클래스 2 · 버퍼 `0x24`, 10진 변환.
- 갈래도 같다: `ACP` 형식 틀림 → 둘째 읽기로 (`+0xf2 JNZ +0x11c`), `OEMCP` 형식 틀림 → 닫기 (`+0x15e JNZ +0x185`), 실패면 두 칸 `0xfde9`. **늘 0 반환** (`+0x19e XOR EAX,EAX`).
- 곁 사실 (결과서, 묻지 않음): ntoskrnl 에 `RtlpGetNtProductTypeFromRegistry` 사본도 있다 (rva `0xaa5c98`, 글 5개 ntdll 과 같음, `+0x173` 이 `0xc000090b`). 부르는 곳 `RtlGetNtProductType +0x51` · `PspSiloInitializeUserSharedData +0x12a`. 둘째는 **서버 silo 쪽 KUSD 사본** 길이다 `[정적: 부르는 곳까지만]` — 짐 지도 3.7 의 진짜 KUSD `NtProductType` 줄 (`MiMemoryLicense` 등) 과는 다른 길, test.exe 는 silo 가 아님 `[추측]`.

**(3) 같은 `.rdata` 글을 쓰는 다른 ntdll 함수 — 없다** `[정적]`

- 결과서가 셋을 다 훑음: ① 코드 — `ntdll.dis` RIP 상대 목적지가 글 범위 안인 줄 **8줄**, 모두 `RtlpQueryNlsSystemCodePages` (`+0x3c` · `+0xc4` · `+0x171`) · `RtlpGetNtProductTypeFromRegistry` (`+0x2d` · `+0x5d` · `+0x74` · `+0x7f` · `+0x8a`). ② 데이터 — 이 범위를 가리키는 8 · 4바이트 값 0. ③ 같은 글자 복사본 — `ProductType` · `WinNt` · `LanmanNt` · `ServerNt` · `OEMCP` · `ProductOptions` 각 1곳 (원본), `ACP` 의 나머지 8곳은 다른 글 속 부분 문자열.
- run58 이 "가진 `.asm` 중엔 두 함수뿐" 이라 한 빈칸이 ntdll 전체로 닫힘.

### Q113 — run59 남은 것

**(a) 옵션 셋째 칸 (`+0xb0`) 의 "2" — 출처는 `PspAllocateProcess` 안, 입력이 아니다** `[정적]`

옵션 번호 n → 표의 `n / 16` 번째 8바이트, 그 안 비트 `(n % 16) × 4` (비트 0~1 상태, 비트 2 고정). 셋째 8바이트 = 옵션 32 ~ 39 (PDB `PS_MITIGATION_OPTION`, ntdll · ntoskrnl 같음):

| n | PDB 이름 | 셋째 칸 비트 | `== 1` 일 때 (결과서) |
|---|---|---|---|
| 32 | `USER_CET_SET_CONTEXT_IP_VALIDATION` | 0~1 | (감사표) `MitigationFlags2 \|= 0x40000` |
| 33 | `BLOCK_NON_CET_BINARIES` | 4~5 | `0x200000` (3 이면 `+0x400000`) |
| 34 | `RESERVED_WAS_XFG` | 8~9 | 읽는 줄 없음 |
| 35 | `POINTER_AUTH_USER_IP` | 12~13 | `MitigationFlags2 \|= 0x8000000` (0 이면 1 로 고쳐 같은 효과 — `+0x56c JZ → +0xe65`) |
| 36 | `CET_DYNAMIC_APIS_OUT_OF_PROC_ONLY` | 16~17 | `MitigationFlags2 \|= 0x40000000` (`+0x5a5`) — 비트 이름은 `[추측: 비트 순서]` |
| 37 | `REDIRECTION_TRUST` | 20~21 | `PspSetRedirectionTrustPolicy(…, 1)` |
| 38 | `RESTRICT_CORE_SHARING` | 24~25 | `MitigationFlags3 \|= 1` |
| 39 | `FSCTL_SYSTEM_CALL_DISABLE` | 28~29 | `MitigationFlags3 \|= 6` (`[추측: 비트 순서]`) |

- **`2` 를 "효과 있음" 으로 보는 줄은 없다.** "2 = 일부러 끔" 이란 공개 이름 (`…_ALWAYS_OFF`) 은 `[추측: 기억]`.
- 앞 두 8바이트 이름 (참고): 3 `HEAP_TERMINATE` · 10 `CONTROL_FLOW_GUARD` · 15 `IMAGE_LOAD_PREFER_SYSTEM32` · 17 `LOADER_INTEGRITY_CONTINUITY` · 19 `RESTRICT_SET_THREAD_CONTEXT` · 31 `CET_USER_SHADOW_STACKS`.

표가 커널을 지나는 길 (`PspAllocateProcess`, 지역 `[RSP+0x400]` 24B):

1. `+0x1a8b` IFEO 읽기 → 없으면 0.
2. `+0x1ab2 ~ +0x1ac9` 시스템 전역 **`PspSystemMitigationOptions`** (rva `0xfc7140`, 24B) 사본 → `+0x1aea` **`PspInheritMitigationOptions(A = 시스템, B = IFEO)`**: 옵션마다 "A 비트 2 켜짐 또는 B 아래 2비트 0 이면 A, 아니면 B".
   - 전역은 부팅 때 레지스트리 `…\Session Manager\Kernel` 값 `MitigationOptions` 에서 채워진다 (INIT 표 rva `0xc825a0 ~ 0xc825b8` 에 키 글 · 값 이름 · 자료 칸 · 길이 칸). 그 표를 읽는 함수는 안 따라감 `[추측]`. 파일 처음 값 24B 0, `PspInitPhase0 +0x108 ~ +0x19e` 가 길이 맞춤 · 검증.
3. `+0x1b79 ~ +0x1b82` 만들기 정보 `+0x8` 비트 16 (사용자가 옵션 속성을 줌) 이면 `+0x1be0` 한 번 더 (A = 앞 결과, B = 만들기 정보 `+0x150` · `+0x160`). 그 둘은 `PspBuildCreateProcessContext +0xad8 ~ +0xb7b` 가 속성 `PsAttributeMitigationOptions` (16) 24B 를 `PspValidateMitigationOptions` 로 검사해 넣은 것. 사용자 쪽 입구는 kernelbase `CreateProcessInternalW +0x22a3` 의 박힌 정책 — test.exe 는 없어서 입력 0 `[추측: 매니페스트 · 로드 구성 안 봄]`.
4. `+0x1c82` 굳히기 — 셋째 칸 안 건드림 (`+0x132 ~ +0x136`).
5. `+0x1c87` `KeIsUserCetAllowed` 거짓이면 `+0x1ca4 ~ +0x1cb2` 가 **둘째** 칸 비트 60~61 (옵션 31) 을 2 로. 사진 `+0xa8 = 0` → 이 길 안 탐 `[정적 + 관찰]`.
6. **`+0x1e03 ~ +0x1e15`** — `[RSP+0x410]` (셋째 칸) `BTR 0xc` · `BTS 0xd` → **옵션 35 := 2. 무조건** (이 구간으로 오는 길은 `+0x1deb JZ` 와 아래로 떨어지기뿐 — flow-writer `.asm` 확인).
7. **`+0x1e1d ~ +0x1e30`** — `KeIsCoreIsolationMitigationPolicyEnforceable` (전역 `KiCoreIsolationEnforceable` (rva `0xfc6ed0`) ≠ 0) 이 거짓이면 `BTR 0x18` · `BTS 0x19` → **옵션 38 := 2.** 그 전역은 `KeOptimizeSpecCtrlSettings +0x48` 이 하이퍼바이저 연결 · `HvlpFlags` · `HvlpSchedulerType` 로 정한다 `[정적: 갈래만]`. 사진이 2 라서 이 VM 에선 0.
8. `+0x1eb3` `PspApplyMitigationOptions` — 위 두 "2" 는 효과 없는 값이라 그대로. **비트 0~1 의 2** 는 여기서 (`+0x500` 프로세스 `CetUserShadowStacks` 꺼짐 → `+0x510 ~ +0x523`). `+0x5ef` · `+0x5f4` 가 표를 제자리에 되씀.
9. `+0x1f37 ~ +0x1f50` 만들기 정보 `+0x150` · `+0x160` 에 되돌림 → `PspPrepareSystemDllInitBlock +0x64 ~ +0x78` 읽기 → `+0x12b` · `+0x138` 블록 `+0xa0` · `+0xb0` 쓰기 (run59).

- 그래서 사진 `0x02002002` = 비트 0~1 (CET 아님) · **12~13 (늘)** · **24~25 (이 컴퓨터 `KiCoreIsolationEnforceable` = 0)**. test.exe 에 따라 안 바뀌고, 바뀌는 건 비트 24~25 (컴퓨터) · 0~1 (프로세스 CET) 둘.
- 사진 대조 `[관찰]` run44 8장 같음: `+0xa0 = 0` · `+0xa8 = 0` · `+0xb0 = 0x02002002` · `+0xb8 = 0` (아래 "도구 메모" 줄). 위 길과 모순 없음.

**Q113 덤 — flow-writer 가 `.asm` 을 읽다 본 것** `[정적: flow-writer, 결과서 밖]`

- 짐 지도 3.11 `MitigationFlags2` 줄은 "조건 = 셋째 칸 비트 16~17 = 1" 이라 적었는데, 사진 셋째 칸 비트 16~17 은 **0** 이고 `MitigationFlags2` 는 `0x40000000` 이다 (K1 이전).
- `PspApplyMitigationOptions` 를 보면: `+0x593 ~ +0x597` 비트 16~17 → `+0x59a JZ` (0 이면) → **`+0x6b0 BT EBX,0x13` · `+0x6b4 JNC +0x5a5`** — EBX 비트 19 가 꺼져 있으면 **같은 `+0x5a5 OR.LOCK [RDI+0x754],R14D`** 로 간다. 1 이면 `+0x5a5` 바로, 2 · 3 이면 건너뜀 (`+0x5a3 JNZ`).
- 즉 "0 (안 정함) 이면 기본으로 켬" 갈래가 있다. EBX = `+0x8f MOV EBX,[RBP+0x60]` (스택 인자로 보임 — 뜻 안 봄 `[추측]`). 이 갈래로 사진과 맞는지는 EBX 비트 19 값을 봐야 한다 → 아래 "아직 확인 필요" 정적 ①.

**(b) 만들기 정보 `+0x1d8` 을 읽는 함수 — 둘** `[정적]`

- 결과서가 `nt.dis` 전체 `[레지스터+0x1d8]` 211줄을 걸러 남긴 셋: 쓰기 `PspGetProcessParameterOverrides +0x4a` (run59) + 읽기 둘.
  - **읽기 1** `PspCaptureProcessParameters +0x26 MOV R8,[R8+0x1d8]` — 첫 인자 `CL ≠ 0` 갈래 (`+0x15` · `+0x17 JZ +0x58`) 에서 `PspCaptureUserProcessParameters` 의 **셋째 인자** 로 넘김 (`+0x3a`).
  - **읽기 2** `PspDeleteCreateProcessContext +0x11a` → `+0x121` `PspDestroyProcessParameterOverrides` (`+0x4 TEST RCX,RCX` · `+0x7 JNZ` — 0 이면 아무것도 안 함).
- 그래서 run104 가 "셋째 인자 구조 (`R15`)" 라 부른 것의 **정체 = 이 `+0x1d8` 덮어쓰기 구조** (만들기 정보 아님, 속성도 아님):
  - `[+0x0]` 버전 (2 만 받음, run59), `[+0x4]` 덮을 칸 비트 — 비트 1 → `RedirectionDllName` (`+0x410`), 비트 2 → `HeapPartitionName` (`+0x420`) · `HeapMemoryTypeMask` (`+0x440`), 비트 3 → `DefaultThreadpoolThreadMaximum` (`+0x43c`), 비트 0 → `DefaultThreadpoolCpuSetMasks` / `Count` (`+0x430` / `+0x438`). 칸 이름은 PDB.
- test.exe: 확장 import 슬롯이 파일 값 (`ret 0` 스텁) 이면 `+0x1d8 = 0` → 사용자 값 캡처 갈래 (사진 `+0x430 ~ +0x448 = 0` 과 맞음), 지우기 안 함. 슬롯이 정말 스텁으로 남는지는 정적으로 못 정함 (Q114 (4) — 동적).

**(c) 결과서 밖 전역 셋 — 근거만, 셈 판단** `[정적]`

| 전역 | 쓰는 줄 | 값 (test.exe) | 읽는 줄 (결과서 `xref` 전수) |
|---|---|---|---|
| `RtlpUnhandledExceptionFilter` (`0x1d28e8`) | `LdrpInitializeProcess +0x134` (인코딩된 0) · `RtlSetUnhandledExceptionFilter +0x9` (KERNELBASE 가 부름) | 둘째 = 인코딩된 `kernelbase!UnhandledExceptionFilter` | `RtlpNotOwnerCriticalSection +0xb1` · `RtlpPossibleDeadlock +0x38` · `TppExceptionFilter +0x9` · `LdrpFatalExceptionFilter +0x40` · `LdrpLogFatalUserCallbackException +0x37` · `RtlUserThreadStart$filt$0 +0x19` |
| `LdrpEnforceIntegrityContinuity` (`0x1d2900`) | `+0x4db SETZ` (블록 `+0xa8` 비트 4~5 == 1) | **0 을 씀** | `LdrpMapDllNtFileName +0x416` · `LdrpValidateIntegrityContinuity +0x90` |
| `LdrpAuditIntegrityContinuity` (`0x1d2918`) | `+0x4fe MOV` (1 또는 3) | **0 을 씀** | `LdrpMapDllNtFileName +0x141` |

- **셈 판단 — 2장 숫자 안 바꿈.** 결과서는 "2장에 센다: 아는 쪽 +2 (189 → 191, 모름 304 → 302, `끝까지 0` 214 → 212), 지금 3.8 에 두 이름 줄이 없다" 라 적었다. 그런데 지금 짐 지도는:
  - 3.8 에 두 이름의 줄이 **이미 있다** (`LdrpEnforceIntegrityContinuity` · `LdrpAuditIntegrityContinuity` 한 줄 — "0 을 씀", 근거 `sast111 · run114`, "Q78e2 로 닫힘").
  - 7.3 에서 run114 가 이미 빼서 셌다 (`(해결됨 → run114) … 0 을 씀 2`, 모름 304 → 268).
  - 결과서의 304 · 189 · 214 는 run114 **전** 숫자다. 결과서가 본 "7.3 줄 1988 · 7.5 줄 2077" 도 옛 판 줄 번호.
  - `RtlpUnhandledExceptionFilter` 는 run68 이 셌다 (결과서와 같음).
  - 그래서 결과서 판단 ("셋 다 쓴 자리 아는 것") 과 **같은 결론이 이미 반영돼 있다** — 다시 +2 하면 두 번 센다. 이번엔 3.8 두 줄에 근거 (`sast119`) 만 보탰다.

### Q115 — run60 남은 것

**(a) `PspCopyUserAttributes` 목록 → 새 프로세스 `ProcessParameters +0x448` — 닫힘** `[정적]`

| 순 | 어디서 | 줄 | 하는 일 |
|---|---|---|---|
| ① | `PspBuildCreateProcessContext` | `+0x1577` 스위치 → `+0x157e JZ` · `+0x1584 TEST …,0x80000` · `+0x1590 JZ` · **`+0x159f` `PspProcessUserCopyAttribute`** | `0x80000` 비트 항목만 (번호 `0x22` 하나, run60) |
| ② | `PspProcessUserCopyAttribute` | `+0x1f` · `+0x2d` · `+0x3a` · `+0x43` · `+0x82 ~ +0x9b` | 만들기 정보 쪽에 **개수 `+0x250` · 본문 길이 `+0x254` · 항목 `+0x230` (32B) · 본문 `+0x258`**. 항목 칸이 32B 라 **한 개만** 들어간다 (둘째는 `+0x250` 을 덮음) — 같은 속성 두 번을 막는 줄은 안 봄 `[정적: 일부]` |
| ③ | `PspSetupUserProcessAddressSpace` | `+0x115 ~ +0x11f` 크기 = `Length + EnvironmentSize` · `+0x124` 스위치 · **`+0x130` `PspCalculateUserAttributeListSize`** · `+0x13a ~ +0x150` | 새 프로세스 덩어리에 목록 자리 = `(8 올림) + [+0x254] + 8 + [+0x250] × 0x20` (개수 0 이면 0) |
| ④ | `PspCopyAndFixupParameters` | `+0x19d` 스위치 켜짐 → `+0x324` 블록 복사 · `+0x347` PEB `+0x20` → **`+0x3c7 CMP [R15+0x250],0` · `+0x3ce JBE`** → `+0x3d4 ~ +0x3e2` `RBX = RDI + (R14 를 8 올림)` → **`+0x3eb` `PspCopyUserAttributes(RBX, 만들기 정보)`** | 목록은 **매개변수 블록 + 환경 바로 뒤** |
| ⑤ | `PspCopyUserAttributes` | `+0x1d` 개수 · `+0x4d ~ +0x6f` 항목 `Value` 주소 고침 · `+0x88` 항목 복사 · `+0x9e` 본문 복사 · `+0xac` 머리 8B = 총 길이 | 새 프로세스 안에 `PS_ATTRIBUTE_LIST` 모양 (머리 · 항목 `0x20` × N · 본문) |
| ⑥ | `PspCopyAndFixupParameters` | **`+0x3f8 LEA RCX,[RDI+0x448]` · `+0x3ff MOV RDX,RBX` · `+0x402` `RtlWriteULong64ToUser`** → `+0x407 JMP` | **`+0x448` (PDB `AttributeList`) = ④ 의 목록 주소** |
| ⑦ | ntdll `LdrpHandleUserCopyAttributeList` | `+0x15` → `+0x448` 읽기 · `+0x23 JZ +0x15b` | 읽는 쪽 (run60). test.exe 0 |

- **스위치 꺼짐 갈래**: ④ `+0x1a4 JZ +0x416` → `+0x3c7 ~ +0x402` 안 지남 (`+0x448` 은 사용자 값 그대로). ① 도 같은 스위치로 막혀 개수 0. 어느 쪽이든 test.exe `+0x448 = 0` (사진과 맞음, run44).
- run104 의 "`+0x430` / `+0x438` 은 속성 목록" 이 아니라 위 Q113 (b) 덮어쓰기 구조에서 온다 — **둘은 별개 줄기** (run104 문서 표기는 이미 "셋째 인자 구조").

**(b) `LdrpSuppressedExportOverrideListPtr` (`.data` `0x1d2920`) 를 읽는 곳 — ntdll 안에 없다** `[정적]`

- 결과서 전수: ① RIP 상대 목적지 `[0x1d2920, 0x1d2928)` 1줄 = `+0x49d` 쓰기 ② 이 칸을 가리키는 8 · 4바이트 값 0 ③ 이웃 칸 (`0x1d28f0 ~ 0x1d2930`) 기준 `+8` 접근 0 ④ 내보내기 2516개에 `Suppress` · `Override` 없음, ntoskrnl · securekernel 본문에도 그 글 없음.
- 쓰는 줄 (run60 그대로): `+0x48d` 확인 → `+0x492 CMP EAX,EBX` · `+0x494 JNZ +0x4a4` → (1 일 때만) `+0x496 LEA` · `+0x49d MOV`.
- 목록 값 (결과서): `0x1740f0` 첫 8B = ntdll `RtlMapSecurityErrorToNtStatus` 시작 — 한 칸짜리 목록으로 보임 `[추측: 이름 뜻]`. 바로 뒤 `0x1740f8` 은 다른 변수 (`…\Session Manager\kernel` 글).
- 결론: **이 빌드 ntdll 안에서 "쓰기만 있는 칸".** 밖에서 이름으로 읽을 가능성 `[추측]`. test.exe 는 쓰기도 안 탐 → 끝까지 0 (사진과 맞음).

**(c) 덤 다섯**

- **속성 `0x22` = `PsAttributeHeapFeatures`** `[정적]` — PDB `_PS_ATTRIBUTE_NUM` (`= 34`). 기능 스위치 `CreateProcessSegmentHeapEnablement` · 읽는 쪽 `RtlpHpOptIntoSegmentHeap` 과 맞는다 → `.data 0x1ce268` = "프로세스 만들 때 힙 기능 요청 (세그먼트 힙 0/1)". (run104 결과서 f-2 가 이미 PDB 이름을 봤지만 run60 줄은 "PDB 에 없음" 으로 남아 있었다 — 아래 정정.)
- **`LdrpMakePermanentImageCommit` (rva `0xa3e78`, 56B)** — 쪽마다 맨 앞 8B 를 **읽고 같은 값을 다시 씀** (`+0x24` · `+0x27`), 값은 안 바뀜 `[정적]`. 부르는 곳 `LdrpLocateMrdata +0x4d` (`.mrdata`) · `LdrpCfgProcessLoadConfig +0x144`. 목적은 "쓰기 가능할 때 copy-on-write 쪽을 미리 확보해, 뒤에 다시 열 때 실패하지 않게" 로 보인다 `[추측]` (코드에 이유 없음).
- **`LdrpInitializeProcess +0xdc4` 의 `ECX` = 1 (닫기)** `[정적]` — `+0xd4f MOV EAX,2` → `+0xd65 LEA EBX,[RAX+0x2e]` (= `0x30`) → `+0xdc1 LEA ECX,[RBX-0x2f]` = **1**. 앞 `+0xda1` 열기와 짝으로 `\KnownDlls` 핸들 (`.mrdata` `LdrpKnownDllDirectoryHandle`) 쓰기를 감쌈. run60 · flow.md 의 `[추측]` → `[정적]`.
- **`LdrEnsureMrdataHeapExists` 쓰기 조건** `[정적]` — ① CFG 켜짐 (`LdrControlFlowGuardEnforced` ≠ 0 — 블록 `+0xb8` ≠ 0 그리고 `CfgOverride` 꺼짐) ② 힙이 아직 없음 → 예약 · 힙 만들기 · 열기 · 락 · `+0xe8` `LdrpMrdataHeapUnprotected` · `+0xef` `LdrpMrdataHeap` · 닫기. **test.exe 는 ① 에서 막힘** (run44 `+0xb8 = 0`) → 둘 다 끝까지 0 (run63 결론과 같음, 줄만 보탬).
- **`.mrdata` 여닫는 `LdrProtectMrdata` 호출 자리 전수 — 50자리 / 함수 21개** `[정적]` (결과서 9 (e) 표). 초기화 줄기 안 (`LdrpInitializeProcess` 몸통 순):

| 순 | 자리 | test.exe 가 타나 (정적 조건) |
|---|---|---|
| 1 | `+0x415` `RtlInsertInvertedFunctionTable` (안 `+0x4b` 열기 · `+0x67` 닫기) | 탐 |
| 2 | `+0x441` 열기 · `+0x470` 빼기 (안에서 한 쌍) · `+0x47c` 닫기 · **`+0x488` 닫기 → `NtProtectVirtualMemory`** | 탐 |
| 3 | `+0x531` `LdrpInitializeExecutionOptions` (안 `+0x373` / `+0x384`) | 안 탐 — IFEO 값 비트 0 일 때만 `RtlGuardAllowSuppressedCalls` = 1 |
| 4 | `+0xda1` 열기 · `+0xdb8` `NtOpenDirectoryObject` · `+0xdc4` 닫기 | 탐 |
| 5 | `+0x1277` `RtlInitializeHistoryTable` (안 `+0x17` / 꼬리 `+0xb4`) | 탐 (조건 없음) |
| 6 | `+0x1713` `AVrfInitializeVerifier` | App Verifier 때만 `[추측: 조건 안 봄]` |
| 7 | `+0x17c1` `LdrpLoadWow64` | WoW64 때만 |
| 8 | `+0x1806` `LdrpInitializeCfgScpHelpers` (안 `+0x5a` / `+0x80`) | 안 탐 — CFG 꺼짐 |
| 9 | DLL 올릴 때마다 `RtlInsertInvertedFunctionTable` | 탐 (모듈 수만큼) |

- 줄기 밖 (프로그램이 부를 때) 12 함수와 "인자를 한 줄로 못 가른 자리" 여섯은 결과서 9 (e) 그대로 — 짐 지도 7.53.

### 호출 관계

```
(Q113 — 커널, 새 프로세스 만들 때)
NtCreateUserProcess
 ├─ +0xab8  PspGetProcessParameterOverrides      +0x4a  만들기 정보 +0x1d8 ← 확장 import (스텁이면 0)
 ├─ +0xacb  PspCaptureProcessParameters          +0x26  +0x1d8 읽기 → R8
 │        └─ +0x3f  PspCaptureUserProcessParameters    셋째 인자 = +0x1d8 덮어쓰기 구조 (+0x410 ~ +0x440)
 ├─ +0xc36  PspAllocateProcess
 │        ├─ +0x1a90  PspReadIFEOMitigationOptions
 │        ├─ +0x1aef  PspInheritMitigationOptions   A = PspSystemMitigationOptions · B = IFEO
 │        ├─ +0x1be5  PspInheritMitigationOptions   (사용자 속성 있을 때) A = 앞 결과 · B = 만들기 정보 +0x150/+0x160
 │        ├─ +0x1c87  PspHardenMitigationOptions
 │        ├─ +0x1c8c  KeIsUserCetAllowed            거짓 → 둘째 칸 비트 60~61 := 2 (test.exe 안 탐)
 │        │  +0x1e03 ~ +0x1e15  셋째 칸 비트 12~13 := 2   (무조건)
 │        ├─ +0x1e22  KeIsCoreIsolationMitigationPolicyEnforceable
 │        │  +0x1e26 ~ +0x1e30  거짓 → 비트 24~25 := 2   (이 VM)
 │        ├─ +0x1eb8  PspApplyMitigationOptions     +0x510 ~ +0x523 비트 0~1 := 2 · +0x5ef/+0x5f4 되씀
 │        │  +0x1f37 ~ +0x1f50  만들기 정보 +0x150/+0x160 ← 표
 │        └─ … PspSetupUserProcessAddressSpace → PspPrepareSystemDllInitBlock +0x12b/+0x138 → 블록 +0xa0/+0xb0 (run59)
 └─ +0x11d5 PspDeleteCreateProcessContext        +0x11a  +0x1d8 읽기 → +0x126 PspDestroyProcessParameterOverrides (0 이면 끝)

(Q115 — 커널, +0x448 이음. 스위치 켜짐 갈래만 — test.exe 는 꺼짐/개수 0)
PspBuildCreateProcessContext
 └─ +0x15a4  PspProcessUserCopyAttribute         만들기 정보 +0x230/+0x250/+0x254/+0x258
PspSetupUserProcessAddressSpace
 ├─ +0x135   PspCalculateUserAttributeListSize   덩어리 크기에 목록 자리
 └─ +0x1b9   PspCopyAndFixupParameters
          │  +0x3c7  개수 > 0 일 때만
          ├─ +0x3f0  PspCopyUserAttributes        블록 + 환경 뒤 (8 올림) 에 PS_ATTRIBUTE_LIST
          └─ +0x407  RtlWriteULong64ToUser        ProcessParameters +0x448 = 목록 주소

(Q115 덤 — ntdll)
LdrpInitializeProcess
 ├─ +0xda6  LdrProtectMrdata(0)
 ├─ +0xdbd  NtOpenDirectoryObject                  → .mrdata LdrpKnownDllDirectoryHandle
 └─ +0xdc9  LdrProtectMrdata(1)                    ECX = 0x30 − 0x2f = 1
```

- `+0x..` 는 ret 자리 (WinDbg 콜스택 기준). 호출 기호 없이 들여쓴 `+0x..` 는 그 함수 안 줄.

## 확인된 것

- `[정적]` `0xc000090b` = `STATUS_BAD_DATA`, `0x40000036` = `STATUS_IMAGE_AT_DIFFERENT_BASE`, `0x40000003` = `STATUS_IMAGE_NOT_AT_BASE` (같은 빌드 바이너리 셋의 이름 표 + ntdll 메시지 글).
- `[정적]` 커널 사본 `RtlpQueryNlsSystemCodePages` 는 ntdll 것과 같은 키 · 값 글, 같은 갈래, 늘 0 반환.
- `[정적]` ntdll 안에서 레지스트리 글 8개를 쓰는 자리는 두 함수 8줄이 전부.
- `[정적]` 옵션 셋째 칸 비트 12~13 의 2 = `PspAllocateProcess +0x1e03 ~ +0x1e15` (무조건), 비트 24~25 의 2 = `+0x1e1d ~ +0x1e30` (`KiCoreIsolationEnforceable` = 0 일 때). 옵션 32 ~ 39 이름은 PDB `PS_MITIGATION_OPTION`.
- `[정적]` `PspInheritMitigationOptions` 의 A 는 **시스템 전역** `PspSystemMitigationOptions` (부모 아님), B 는 IFEO · 사용자 속성.
- `[관찰]` run44 사진 8장 `LdrSystemDllInitBlock +0xa0 = 0` · `+0xa8 = 0` · `+0xb0 = 0x02002002` · `+0xb8 = 0` — 위 정적 길과 맞음.
- `[정적]` 만들기 정보 `+0x1d8` 을 읽는 곳 = `PspCaptureProcessParameters +0x26` · `PspDeleteCreateProcessContext +0x11a`. 그 값이 `PspCaptureUserProcessParameters` 셋째 인자 (덮어쓰기 구조).
- `[정적]` `+0x448` 사슬 ① ~ ⑥ (스위치 켜짐 · 개수 > 0 일 때만), test.exe 는 0.
- `[정적]` `LdrpSuppressedExportOverrideListPtr` 는 ntdll 안에서 쓰기만 있음.
- `[정적]` 속성 `0x22` = `PsAttributeHeapFeatures`. `+0xdc4` `ECX` = 1. `LdrEnsureMrdataHeapExists` 쓰기 조건 = CFG 켜짐 + 힙 없음.

## 정적과 동적이 다른 곳

- 없음 — 사진 `0x02002002` 와 정적 길이 맞다.
- 다만 짐 지도 3.11 `MitigationFlags2` 줄의 "조건 = 비트 16~17 = 1" 은 사진 (비트 16~17 = 0, `MitigationFlags2 = 0x40000000`) 과 안 맞아 보였다. flow-writer 가 `.asm` 에서 "0 이면 `+0x6b0` EBX 비트 19 꺼짐일 때 같은 쓰기" 갈래를 봤다 (위 "Q113 덤"). EBX 값은 안 봐서 사진과 맞는지는 `[공백]` → 4장 · 7.53.

## 정정

이전 문서 맨 위에 한 줄씩 넣었다 (결과서 파일은 안 고침):

- **run59** (sast/54 질문 13 · 12) — 정정: "비트 12~13 · 24~25 의 2 는 입력 표가 갖고 온 값 `[공백]`" → `PspAllocateProcess` 가 그 자리에서 씀. "부모 상속 (`PspInheritMitigationOptions`)" → A 는 시스템 전역. "옵션 번호 ↔ 이름 PDB 에 열거형 없음" → `PS_MITIGATION_OPTION` 있음 (run107 이 일부 먼저 씀). 닫힘: `+0x1d8` 읽는 함수 둘.
- **run50** — 정정: "완화 옵션 칸 값은 부모 · 시스템 기본 · 이미지 헤더에서만" 의 "부모" — `PspInheritMitigationOptions` 길에 부모 값은 없다 (시스템 전역 · IFEO · 사용자 속성).
- **run60** (sast/55 2-2) — 정정: "속성 번호 `0x22` 공개 이름 (PDB 에 없음)" → `PsAttributeHeapFeatures`. 닫힘: `+0x448` 사슬 · `LdrpSuppressedExportOverrideListPtr` 읽는 곳 없음 · `+0xdc4` `ECX` = 1 · `LdrpMakePermanentImageCommit` 동작 (목적 `[추측]`).
- **run104** (sast/104) — 정정: 다음 요청 후보 6 의 `[추측]` "셋째 인자는 속성 `0x20` 대 (`PsAttributeHeapFeatures` 류)" → 셋째 인자 = 만들기 정보 `+0x1d8` 덮어쓰기 구조 (`PspGetProcessParameterOverrides` 확장 import 가 줌), 속성 아님. (sast/104 결과서의 "`PS_CREATE_INFO` 비트 0/2" 이름도 같은 정정 — run104 문서는 이미 "셋째 인자" 로 적어 줄 · 동작은 그대로.)
- **run58** — 닫힘: 상태 이름 · 커널 사본 글 · 같은 글 쓰는 다른 함수 (Q112 셋).
- **run106** — 닫힘: `0x40000036` 이름 `[추측: 기억]` → `[정적]` `STATUS_IMAGE_AT_DIFFERENT_BASE`.
- 우리 문서 쪽 (flow.md · 짐 지도) 고친 곳은 아래 "바꾼 곳".

## 아직 확인 필요

동적 (모두 **선택** — 결과서 "필수 없음". 대기열 번호는 오케스트레이터가 붙임, 멈춤점은 test.exe 조건):

- ① `nt!PspAllocateProcess+0x1e1d` (= `nt+0xa04385`, 비트 12~13 쓰기 직후 — 결과서의 `nt+0xa04380` 은 `+0x1e15` 명령 (8바이트, `0xa0437d ~ 0xa04384`) 한가운데라 flow-writer 가 `.asm` 주소로 고침) · `+0x1e38` (`nt+0xa043a0`, 비트 24~25 쓰기 뒤 모이는 곳) 에서 `dq @rsp+0x400 L3` 셋째 값 — 비트 13 켜짐인지, `+0x1e22` 뒤 `al` (= `dd nt+0xfc6ed0`) 0 인지. `nt!PspApplyMitigationOptions` `+0x5f4` 뒤 셋째 값 = `0x02002002`.
- ② (Q114 (4) 와 같음) `dq nt!__imp_PsGetProcessParameterOverrides L1` 이 스텁 (`ret 0`, rva `0x4a10c0`) 인지.
- ③ 레지스트리 `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel` 값 `MitigationOptions` 있나 + `dq nt!PspSystemMitigationOptions L3` (사진 앞 두 칸 0 과 맞나).
- ④ test.exe 한 번에서 `NtProtectVirtualMemory` 가 `.mrdata` (`ntdll+0x1e7000`, `0x35b8`, 보호 2 / 4) 로 몇 번 오나 — 기대: `+0x488` 1 + 표 4 · 5 짝마다 2 + DLL 마다 넣기 짝.
- ⑤ (flow-writer 보탬) `nt!PspApplyMitigationOptions+0x6b0` 에 서는지 · `r ebx` 비트 19 — `MitigationFlags2 0x40000000` 이 "0 이면 기본 켬" 갈래에서 오는지.

정적 (결과서 "다음 요청 후보" + 보탬, 모두 작음):

- ① (flow-writer 보탬) `PspApplyMitigationOptions` EBX (`[RBP+0x60]`) 비트 19 의 뜻 · 출처 — 짐 지도 3.11 `MitigationFlags2` 줄 조건을 닫음.
- ② `LdrpSuppressedExportOverrideList` 의 실제 길이 · 소비자 (kernelbase · kernel32 등 다른 모듈 `.dis` 로 같은 꼴 훑기).
- ③ `KeOptimizeSpecCtrlSettings` — `KiCoreIsolationEnforceable` 를 정하는 식 (에뮬레이터가 비트 24~25 를 컴퓨터마다 정하려면).
- ④ `PspSystemMitigationOptions` 를 레지스트리에서 채우는 함수 (INIT 표 `0xc825a0` 을 읽는 쪽).
- ⑤ (켜짐 갈래를 만들 때) `+0x448` 사슬 ④ ~ ⑥ 과 "한 개만 들어간다" 확인 (같은 속성을 두 번 받는 것을 막는 줄).
- ⑥ (결과서 9 (e)) `LdrProtectMrdata` 인자를 한 줄로 못 가른 자리 여섯 (`RtlpRemoveVectoredHandler +0x181` · `+0x209`, `RtlResetNtUserPfn +0x65`, `RtlAddFunctionTable +0x236`, `AVrfInitializeVerifier` `EDI`, `LdrpGetShimEngineInterface +0x560` `ESI`).
- ⑦ (낮음, 곁 사실) ntoskrnl `RtlpGetNtProductTypeFromRegistry` 사본을 `PspSiloInitializeUserSharedData +0x12a` 가 부르는 길 — 서버 silo KUSD 사본 몫, test.exe 와 무관 `[추측]`.

PE_emulator 쪽 (결과서 "지금 우리 맞나" — `../PE_emulator` 코드는 flow-writer 가 안 읽음, 고칠지는 사용자 결정):

- Q112 셋 **맞음** (`0x40000036` 주석에 이름을 붙일 수 있음).
- Q113 (a) **다름 — 알고 줄인 것**: 블록 `+0xa0` 24B 를 0 으로 둠, ntdll 이 `+0xb0` 을 스택 사본으로만 옮겨 동작은 같음. 맞추려면 세 줄 (비트 12~13 · 24~25 · 0~1 := 2).
- Q113 (b) **맞음 (줄임)** — `PspGetProcessParameterOverrides` 안 함 = `+0x1d8` 0. 확장이 붙으면 확인 필요.
- Q115 (a) · (b) **맞음** (스위치 꺼짐 · 0). (c) `.mrdata` 여닫는 순서 · `NtProtectVirtualMemory` 수는 **확인 필요**.

## 바꾼 곳

- flow.md: 기준 (pdb 줄 · 근거 로그 줄 run123), 2.1 `NtCreateUserProcess` (`+0x1d8` 읽는 곳) · `PspAllocateProcess` 완화 옵션 · `PspPrepareSystemDllInitBlock` (`+0xb0` 출처 · 이름) · `PspCopyAndFixupParameters` (셋째 인자 정체 · 속성 `0x22` · `+0x448` 사슬), 2.4 레지스트리 두 함수 (커널 사본 · `STATUS_BAD_DATA`) · `.mrdata` (`+0xdc4` `[추측]` → `[정적]`, `LdrpMakePermanentImageCommit`, 50자리) · CFG 억제 (읽는 곳 없음) · `0x1ce268` 이름, 2.6 `0x40000036` 이름 셋 줄, 4장 run58 · run59 · run60 정적 줄 `(해결됨)` + 새 run123 줄 셋, 5장 근거 run 표 · 요청서 번호 표.
- 짐 지도: 기준 · 판 줄, 2장 메모 (숫자 그대로 · 셈 판단), 3.2 `AttributeList`, 3.8 `0x1ce268` · `RtlpUnhandledExceptionFilter` · 무결성 둘 · `LdrpSuppressedExportOverrideListPtr`, 3.9 머리 · `LdrpKnownDllDirectoryHandle` · `LdrSystemDllInitBlock`, 3.11 `MitigationFlags2`, 6장 레지스트리 줄, 7.4 · 7.6 만들기 정보, 7.19 · 7.20 · 7.36 ⑥ `(해결됨)`, 새 7.53, 8장.

## 도구 메모

- 결과서는 사진을 "run44, 기존 문서" 로만 인용했다 (로그 줄 번호 없음). flow-writer 가 로그로 맞춘 줄 — `LdrSystemDllInitBlock` = `0x7ffafb84a450` — `ms/logs/04_baggage-read_K1.txt:1531 ~ 1532` `dq nt!PsNtdllExports` 첫 칸, 블록 `+0x0` 크기 `0x128` (`:6178`), ntdll rva `0x1ea450` 와 맞음:
  - `+0xa0` · `+0xa8` = `…a4f0` 줄, `+0xb0` · `+0xb8` = `…a500` 줄 (`00000000'02002002 00000000'00000000`).
  - 줄 번호: K1 `:6188 · :6189` · K2 `:6196 · :6197` · K3 `:6201 · :6202` · P0 `:6203 · :6204` · P1 `:6202 · :6203` · P1a `:6594 · :6595` · P1b `:7712 · :7713` · P2 `:7724 · :7725`. 8장 모두 같은 값 — 칸 이름과 값 짝 어긋남 없음.
- 새 함정 없음.

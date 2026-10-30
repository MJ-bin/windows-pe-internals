# 50 todo-vm — 정적 분석 결과 (Q104, 거르기)

- 요청서: `_workspace/sast/50_todo-vm.md`
- 모듈 / 바이너리: ntoskrnl (`ms/bin/.../System32/ntoskrnl.exe`). PDB `ntkrnlmp.pdb` guid+age `C29EBFB06B78B3C020DCA66D99713F9E1` (`ms/sym/ntoskrnl.exe.pdb.info.txt`) = 기준값과 같음.
- 분석: 이미 돼 있어서 다시 안 걸었다 (`.gpr` + `_index.tsv` + analyze 로그 `SetPdb:` 줄 있음, Ghidra 프로세스 없음).
- **버전 확인: 일치.** `_calls.tsv`: `NtCreateUserProcess +0xc31 / ret +0xc36 → PspAllocateProcess`, `PspInsertThread +0x802 / ret +0x807 → PspCallProcessNotifyRoutines` (알려진 사실과 같음). 새로 뽑은 `.asm` 머리의 rva 도 `_index.tsv` 와 같음.
- **새로 뽑은 것 (모두 depth 0, 함수 7개)**: `PsBootPhaseComplete`, `KiEnableKvaShadowing`, `KiInitializeKernel`, `SepInitializeCodeIntegrity`, `PspBuildCreateProcessContext`, `Feature_CreateProcessSegmentHeapEnablement__private_IsEnabledFallback`, `Ordinal_25` → `ms/decomp/ntoskrnl/`, `ms/disassemble/ntoskrnl/`.
  - 새 `_refs` 5개 (`run_ghidra.sh refs`): `KiMxCsrMask`, `PsDefaultLoaderThreads`, `KiKvaShadow`, `g_kernelCallbacks`, `Feature_CreateProcessSegmentHeapEnablement__private_featureState` → `ms/disassemble/ntoskrnl/_refs/`.
  - 로그: `ms/ghidra/ntoskrnl.export.sast50.stdout`, `…sast50b.stdout`, `…sast50c.stdout`. `_calls.tsv` 는 이번 export 로 4,333 → 4,493줄.
- **읽기만 한 것 (다시 안 뽑음)**: `PspAllocateProcess`, `NtCreateUserProcess`, `PspSetContext`, `MiCreateImageFileMap`, `MiRelocateImage`, `MiSectionControlArea`, `MiMapProcessExecutable`, `PspWow64PickBestNtdll`, `PspSelectMachineForProcess`, `PspSetProcessAffinitySafe`, `MmCreatePeb`, `PspCopyAndFixupParameters`, `PspSetupUserProcessAddressSpace`, `PspInitializeThunkContext`, `PspGetSetContextInternal`, `KiInitializeContextThread`, `SwapContext`, `KxStartUserThread`, `KiKernelExit`, `kernelbase!CreateProcessInternalW` (상수 두 줄만).
- test.exe 자체의 PE 헤더도 읽었다 (`sample/bin/test.exe` = `ms/bin/test.exe`, `cmp` 같음). 임시 파이썬 (스크래치, 레포 밖) 로 `EntryRVA 0x1d40`, `ImageBase 0x140000000`, `Win32VersionValue 0`, LoadConfig 디렉터리 (rva `0x79090`, `Size 0x140`) 의 `ProcessAffinityMask 0` 을 직접 읽음.
- 판정: **분석성공** (17개 모두 A/B/C 로 갈랐다). A 2 · B만 8 · B+C 섞임 2 (#3·#14) · C만 5 (아래 표).

## 스크립트 문제

없다. `run_ghidra.sh export` 3번, `refs` 5번 모두 정상 종료. (참고, 스크립트 문제 아님: `refs KiKvaShadow 0x10` 은 바이트 수를 16 으로 줘서 이웃 플래그 칸 참조까지 215줄이 나왔다. 칸 하나만 보려면 바이트 수 `1`.)

---

## 맨 위 표 (17줄)

표시 규칙: `[정적] sast50` = 이번에 `.asm` 줄로 확인. 기존 문서는 `runN`/`sast/NN` 으로. 오프셋은 `.asm` 의 `+0x..` (WinDbg `함수+0x..` 와 같은 기준; call 줄이 아니라 ret 칸이 필요하면 `ret=` 로 따로 적음).

| # | 분류 | 한 줄 답 | 근거 | C 면 멈춤점·명령 |
|---|---|---|---|---|
| 1 | **A** | test.exe 는 **`+0x2095`** (보통 길). `+0x2113` (복제) 은 안 탄다 | `[정적]` run50 289 + sast/44 결과서 53·206·284. 이번에 `PspAllocateProcess.asm` `+0x2056 TEST BL,4`/`+0x2059 JNZ`, `+0x2c8 ~ +0x317`, `+0x4c`, `NtCreateUserProcess +0xc0c ~ +0xc13` 다시 읽음 (7번째 인자 섹션 = non-null) | — |
| 2 | **C** | 대상 = `SeCiCallbacks+0xc0` 칸에 CI(`ci.dll`) 가 부팅 때 넣은 함수. 인자 `RCX=이미지 파일 객체`, `RDX=0x218B 버퍼`. 타는 길: test.exe 에서 부르는 코드에 닿음 (포인터 ≠ 0 이면) | `[정적]` `PspAllocateProcess +0x1728 ~ +0x17c4`; `SepInitializeCodeIntegrity +0x118 / +0x12b` (`CiInitialize` 에 `&SeCiCallbacks` 넘김); `_refs/SeCiCallbacks.tsv` | **아무 때나** `dqs nt!SeCiCallbacks+0xc0 L3` (#2·#3·#14 한 번에) |
| 3 | **B** (타나) + **C** (대상) | **탄다.** 이 블록은 실패 전용이 아니라 **성공·실패 공용 꼬리** — 성공 길이 `+0x2833 JMP` 로 `+0x11cc` 에 들어와 `+0x1201 ~ +0x121a` 를 지난다. 부름 조건은 `SeCiCallbacks+0xc8` ≠ 0 하나 | `[정적]` sast50: `PspAllocateProcess +0x2833` (= `JMP 0x140a03734` = `+0x11cc`), `+0x1201 / +0x1208 / +0x120b / +0x1215`. 실패 길은 `+0x118e` 로 더 앞에서 합류 | #2 와 같은 `dqs` (`+0xc8`). 확인용 `bp nt!PspAllocateProcess+0x1215` → `r rax` |
| 4 | **B** | test.exe 때 `PspWow64PickBestNtdll` **안 탄다** | `[정적]` sast50: `PspAllocateProcess +0xfad TEST BL,0x40 / +0xfb0 JZ → +0x103b` 가 `+0xff6` 를 건너뜀. `0x40` 은 `+0x92a` (부모 WoW 정보 `[부모+0x310]≠0` 복제 갈래) 와 `+0xab3` (`PspSelectMachineForProcess` 출력 `+2` 바이트 ≠ 0) 에서만 켜짐. 이미지 `Machine 0x8664`·부모 x64 면 그 바이트는 0 (`PspSelectMachineForProcess.asm` `+0x54 ~ +0xd4`) | — |
| 5 | **B** | test.exe 때 `PspSetProcessAffinitySafe` **안 탄다** | `[정적]` sast50: `PspAllocateProcess +0x2323 CMP [RSP+0x210],0 / +0x232b JZ`. `[RSP+0x210]` = `MmCreatePeb` 출력 GROUP_AFFINITY 마스크 (`MmCreatePeb +0x80` 에서 0 으로 비움, `+0x34d ~ +0x356` 에서 **PE LoadConfig `ProcessAffinityMask`(+0x40)** 를 복사). test.exe 값 = **0** (PE 바이트 직접 읽음) | — |
| 6 | **B** | test.exe 때 `KeSecureProcess` **안 탄다** | `[정적]` sast50: `PspAllocateProcess +0x2728 BT EBX,0x10 / +0x272c JNC → +0x27b3`. 비트 16 은 `+0x3e7 BTS EBX,0x10` 하나가 켬 (`+0x3dc ~ +0x3e5`: 속성 모음 `[CreateInfo+8]` 의 비트 18). 비트 18 = 속성 `…0012` (보안 프로세스) — `kernelbase!CreateProcessInternalW +0xa0b` 가 생성 플래그 `0x400000` 일 때만 이 속성을 넣음 | — |
| 7 | **C** | `PspCopyAndFixupParameters` 가 켜짐(≠0) 이면 **`+0x34c` 쪽 (`RtlWriteULong64ToUser`)**, 꺼짐(=0) 이면 **`+0x52b` 쪽 (직접 쓰기)**. 두 쪽 다 `PEB+0x20` ← 새로 복사한 블록 주소 (값은 같고 쓰는 방식만 다름) | `[정적]` sast50: `PspCopyAndFixupParameters +0x19d` (ret `+0x1a2`) → `TEST EAX,EAX / +0x1a4 JZ → +0x416`(꺼짐: `+0x52b MOV [PEB+0x20]`) ; 켜짐: `+0x347` (ret `+0x34c`). `Feature_…IsEnabledDeviceUsageNoInline.asm`: 상태 칸 비트 4 가 켜져 있으면 결과 = 비트 0, 아니면 WIL 대체 판정 (`IsEnabledFallback`) | `dd nt!Feature_CreateProcessSegmentHeapEnablement__private_featureState L1` (아무 때나, 비트 4 켜짐이면 비트 0 이 답). 비트 4 꺼짐이면 `bp nt!PspCopyAndFixupParameters+0x1a2` → `r eax` (test.exe 만들 때) |
| 8 | **B** | **둘 다 더한다 (서로 다른 델타, 서로 다른 사본).** 조각 `+0x1b1279` = 섹션 만들 때 "새로 고른 기본 주소 − 헤더 기본 주소". `PspAllocateProcess +0x20ce` = 프로세스에 올린 뒤 "실제 올라간 주소 − 섹션에 기록된 기본 주소" (같으면 안 더함). 최종값은 어느 쪽이든 **`SectionBaseAddress + AddressOfEntryPoint`** | `[정적]` sast50: `MiRelocateImage` 조각 `+0x1b1269 / +0x1b126d / +0x1b1279`, `+0x32a ~ +0x33a`; `PspAllocateProcess +0x20a6 ~ +0x20ce`, `+0x98c`; `MiSectionControlArea.asm`; `MiMapProcessExecutable +0xc5` | — (선택: `bp nt!PspAllocateProcess+0x20c6` → `r rdx, rax` 같으면 두 번째는 0) |
| 9 | **A** | `PspSystemDlls[0]` = `PspNativeSystemDllData`, 그 `+0x20` = 실제 ntdll 베이스 (헤더 `ImageBase` 칸에 올라간 주소를 적음). 관찰 + 정적 사슬로 닫힘 | `[정적]` run48 188 (`+0x207054 MOV [RBX+0x20],RDX`) + `[정적]` `PspInitializeThunkContext +0x196 / +0x19d / +0x1c0` (`Rdx ← [PspSystemDlls[0]+0x20]`) + `[관찰]` run44 첫 트랩 프레임 `Rdx` = `7ffafb660000` | 선택: `dq nt!PspNativeSystemDllData+0x20 L1` (아무 때나) |
| 10 | **C** | 값 하나로 갈린다: `KiKvaShadow` 비트 0 = 1 → `KiKernelExit +0xa5`, = 0 → `KxStartUserThread +0x3e1` (조각). `+0x42c` 는 커널 모드 복귀용이라 test.exe 가 아님 | `[정적]` sast50: `KxStartUserThread +0xe1 / +0xe8` (`[RBP+0xf0]`=`SegCs` 비트 0), 조각 `+0x3be TEST [KiKvaShadow],1` → `+0x3c7 JMP KiKernelExit`; `KiKernelExit.asm` `+0xa5`; 켜는 곳 `KiEnableKvaShadowing +0x131` (`KiIsKvaShadowDisabled` 거짓 + `SpcQueryKvaLeakagePresent` ≠ 0 또는 모의) | **아무 때나** `db nt!KiKvaShadow L1` (비트 0). 보조 `dd nt!KiKvaShadowMode L1` |
| 11 | **B** | `0x27f`. 새 스레드의 저장 영역에 `0x27f` 를 박고 (`KiInitializeContextThread +0x1ec`) 첫 전환 때 `SwapContext` 가 복원한다 | `[정적]` sast50 (아래 11번) + `[관찰]` run44 맡긴 문맥 `0x27f` 와 맞음 | 선택: `bp ntdll!LdrInitializeThunk` → `r fpcw` |
| 12 | **C** (싼 것) | 정적 한계: 초기값 0, 쓰는 줄은 부팅 때 레지스트리 질의 하나 (`MaxLoaderThreads`). 이 VM 값은 `[관찰]` PEB 쪽 0 으로 0 에 가까우나 직접 읽은 적 없음 | `[정적]` sast50: `PsBootPhaseComplete +0x128 / +0x12f` (`R9=&PsDefaultLoaderThreads`, `RDX` = 문자열 `MaxLoaderThreads`), `PspSetupUserProcessAddressSpace +0xb8 ~ +0xc7`, `_refs/PsDefaultLoaderThreads.tsv` | **아무 때나** `dd nt!PsDefaultLoaderThreads L1` |
| 13 | **C** | 이름 = `g_kernelCallbacks+0xb0` (rva `0xfd8320`+`0xb0`). 표는 `Ordinal_25 +0x14` 가 `clipsp.sys` 의 `ClipSpInitialize` 에 넘겨 채움. 인자 `RCX` = 새 프로세스용 토큰, 반환 음수면 생성 실패 | `[정적]` sast50: `pdb_aliases.py --where` , `_refs/g_kernelCallbacks.tsv`, `Ordinal_25.asm`, `NtCreateUserProcess +0x59f ~ +0x5bb`, `+0x417` | **아무 때나** `dqs nt!g_kernelCallbacks+0xb0 L1` |
| 14 | **B** (이름·타나) + **C** (대상) | 이름 = **`SeCiCallbacks+0xd0`** (맞음). 공용 꼬리라 성공 때도 지나감. 인자 `RCX` = 이미지 파일 객체. 부름 조건 = 객체 ≠ 0 이고 포인터 ≠ 0 | `[정적]` sast50: `NtCreateUserProcess +0x108b JMP → +0x11a3`, `+0x11a3 / +0x11aa / +0x11af / +0x11b9 / +0x11bb`; `_refs/SeCiCallbacks.tsv` (`NtCreateUserProcess+0x11af READ +0xd0`) | #2 와 같은 `dqs` (`+0xd0`) |
| 15 | **B** | test.exe 는 **`PspCreateUserContext` 길 (`+0xd22`)**. `+0xd3e` 는 안 탄다 | `[정적]` sast50: `NtCreateUserProcess +0xce0` 의 섹션 참조 = `+0x912 MmCreateSpecialImageSection` → `+0x96d ObReferenceObjectByHandle` → `+0x978` 저장. 이미지 이름 속성(비트 5) 이 있으면 항상 이 길 (`+0x463 ~ +0x46b`); `kernelbase!CreateProcessInternalW +0x85d` 가 속성 `0x20005` 를 넣음 | — |
| 16 | **B** | **탄다** (`PspInitializeThunkContext +0x2d6` → `PspSetContextThreadInternal` → … → `PspSetContext`) . 입력 `MxCsr 0x1f80` → `& KiMxCsrMask` → **`0x1f80` 그대로** | `[정적]` sast50: `PspInitializeThunkContext +0x1a8 / +0x1f1 / +0x1f6 / +0x2be ~ +0x2d6`; `PspSetContext +0x1b8 ~ +0x1c4`, `+0x2a7 / +0x2aa / +0x2b0`; `KiInitializeKernel +0x214 ~ +0x22d` (마스크 = 칩 `MXCSR_MASK`, 0 이면 `0xffbf`) ; 파일 속 처음 값 `0xffbf` | 선택: `dd nt!KiMxCsrMask L1` |
| 17 | **B** | **`0xC000012F`** (`STATUS_INVALID_IMAGE_NOT_MZ`). 실패 길은 이 값을 `R14D` 에 넣고 정리한 뒤 `MOV EAX,R14D` 로 돌려준다 | `[정적]` sast50: `MiCreateImageFileMap +0x17f ~ +0x18b` (`CMP [RDX],0x5a4d / JNZ`), `+0x6bc MOV R14D,0xc000012f`, `+0x666 → +0x5f7 … → +0x46a / +0x473 MOV EAX,R14D / +0x495 RET` | — |

- 분류 요약: **A** = 1, 9 · **B** = 4, 5, 6, 8, 11, 15, 16, 17 · **B+C** = 3, 14 (타는지·이름은 B, 대상은 C) · **C** = 2, 7, 10, 12, 13.
- 이름 확인 필요: 없음 (`nt+0xfd83d0` = `g_kernelCallbacks+0xb0`, `nt+0xf04d30` = `SeCiCallbacks+0xd0`). 다만 `Ordinal_25` 는 PDB 공개 이름이 없는 내보내기라 Ghidra 라벨 그대로 쓴다 (`pdb_aliases.py --where ntoskrnl.exe Ordinal_25` → "PDB 공개 심볼에 없음").

---

## C 항목 묶음 (test.exe 한 번 실행에 다 보게)

### 1) 아무 때나 읽는 전역 (멈춤점 없이, 커널 디버거로 부팅 뒤 아무 때나)

부팅 뒤에 안 바뀌는 값들이다. 쓰는 곳이 부팅 코드 하나뿐이라서 `[정적]` (`_refs` 의 쓰기 줄). 한 번에 붙여 넣을 목록:

| 명령 | 답하는 # | 읽는 법 |
|---|---|---|
| `db nt!KiKvaShadow L1` | 10 | 비트 0 = 1 → 첫 IRETQ 가 `KiKernelExit+0xa5` (`nt+0xbcac25`). 0 → `KxStartUserThread+0x3e1` (`nt+0x6aff51`) |
| `dqs nt!SeCiCallbacks+0xc0 L3` | 2, 3, 14 | 세 칸이 `+0xc0` `+0xc8` `+0xd0`. 이름은 `.reload /f ci.dll` 뒤 `ln poi(nt!SeCiCallbacks+0xc0)` 식으로 |
| `dqs nt!g_kernelCallbacks+0xb0 L1` | 13 | 대상 주소 (`clipsp.sys` 안일 것 `[추측]`) |
| `dd nt!Feature_CreateProcessSegmentHeapEnablement__private_featureState L1` | 7 | 비트 4(`0x10`) 켜짐 → 비트 0 이 답. 꺼짐 → 아래 시간 순 (c) 필요 |
| `dd nt!PsDefaultLoaderThreads L1` | 12 | 0 이면 PEB 쪽 0 과 맞음 |
| (선택) `dd nt!KiMxCsrMask L1` | 16 | 보통 `0xffff` (또는 `0xffbf`). 둘 다 `0x1f80` 비트가 남음 |
| (선택) `dq nt!PspNativeSystemDllData+0x20 L1` | 9 | 한 번 직접 확인용 |

### 2) test.exe 만들 때 멈춤점 (시간 순, 전부 선택·확인용)

위 전역이 이미 대상을 알려 주니 **필수가 아니다.** "정말 그 줄에서 부르나/어느 쪽으로 가나" 를 한 번 눈으로 볼 때만. 커널 멈춤점은 모든 프로세스에 걸리므로 test.exe 를 만드는 스레드일 때만 멈추는 조건을 붙일 것 (dast-mentor 몫).

| 순서 | 멈춤점 | `nt+rva` | 읽을 것 | # |
|---|---|---|---|---|
| a | `nt!NtCreateUserProcess+0x5b2` | `nt+0xa018f2` | `r rax` (포인터 = `g_kernelCallbacks+0xb0` 값), `r rcx` (토큰) | 13 |
| b | `nt!PspAllocateProcess+0x17bf` | `nt+0xa03d27` | `r rax` (= `SeCiCallbacks+0xc0`), `r rcx` (파일 객체) | 2 |
| c | `nt!PspCopyAndFixupParameters+0x1a2` | `nt+0xae5986` | `r eax` (0 → `+0x52b` 직접 쓰기, ≠0 → `+0x34c` 쪽) | 7 |
| d | `nt!PspAllocateProcess+0x1215` | `nt+0xa0377d` | `r rax` (= `SeCiCallbacks+0xc8`) | 3 |
| e | `nt!NtCreateUserProcess+0x11bb` | `nt+0xa024fb` | `r rax` (= `SeCiCallbacks+0xd0`), `r rcx` (파일 객체) | 14 |
| f (선택) | `nt!PspAllocateProcess+0x20c6` | `nt+0xa0462e` | `r rdx, rax` (같으면 8번 두 번째 더하기 없음) | 8 |
| g (선택) | `ntdll!LdrInitializeThunk` (첫 명령) | — | `r fpcw` (기대 `0x27f`) | 11 |

- f 의 `nt+rva` = `PspAllocateProcess` rva `0xa02568` + `0x20c6`.
- 시간 순 근거: `PspSetupUserProcessAddressSpace` (→ `PspCopyAndFixupParameters`) 는 `PspAllocateProcess +0x2397` 에서 불리고 (`_calls.tsv`), 성공 꼬리 (d) 는 그 뒤 `+0x2833` 로 들어간다. (a) 는 `PspAllocateProcess` 부름 (`+0xc31`) 보다 앞, (e) 는 `NtCreateUserProcess` 의 마지막 꼬리.

---

## 항목별 자세히 (근거 줄)

경로 기준: `ms/disassemble/ntoskrnl/<함수>.asm`. 코드는 붙이지 않고 우리 말로 요약한다.

### 1 (A)

- `PspAllocateProcess` 의 지역 플래그 `EBX` (`[RSP+0x84]`) 는 `+0x12b` 에서 0 으로 시작한다.
- 비트 `0x4` 가 켜지는 길은 `+0x2c8 ~ +0x317` 뿐이다.
  - ProcessFlags (`[RSP+0x8b0]`, 9번째 인자) 비트 11 (`0x800`) **꺼짐** → `+0x2f7 TEST R12,R12`. `R12` = 7번째 인자 섹션 (`+0x4c MOV R12,[RSP+0x8a0]`). 섹션이 있으면 `JNZ → +0x319` 로 `EBX` 는 0 그대로.
  - 섹션이 0 일 때만 `+0x2fc ~ +0x30d`: 부모 `R14` (1번째 인자) ≠ 0 이면 `EBX = 3+1 = 4`.
  - (비트 11 이 켜져 있어도 `0x4` 가 서려면 비트 13 도 필요 — 최소 프로세스·복제 프로세스 플래그 `[추측: 공개 헤더 이름]`. 일반 CreateProcess 는 안 켠다고 본다.)
- 가름길: `+0x2056 TEST BL,0x4` → 켜졌으면 `+0x2059 JNZ → +0x20e2` → `+0x210e CALL` (ret `+0x2113`, 부모 `R12` 를 복제), 아니면 `+0x2090 CALL` (ret `+0x2095`, 둘째 인자 0, 셋째 `R8 = 섹션`).
- 7번째 인자: `NtCreateUserProcess +0xc0c MOV RAX,[RBP+0x270]` → `+0xc13 [RSP+0x30]` (#15 에서 non-null 임을 보임).

### 2·3·14 (SeCiCallbacks 세 칸, 13 은 다른 표)

- `SeCiCallbacks` = rva `0xf04c60`, 크기 `0x110` (`SepInitializeCodeIntegrity +0x52` 가 맨 앞 4바이트에 `0x110` 을 씀). `+0xc0 = 0xf04d20`, `+0xc8 = 0xf04d28`, `+0xd0 = 0xf04d30` (산술 + `_refs/SeCiCallbacks.tsv` 가 `PspAllocateProcess+0x17a7`, `+0x1201`, `NtCreateUserProcess+0x11af` 를 각각 `+0xc0`·`+0xc8`·`+0xd0` 읽기로 적음).
- 표를 채우는 쪽: `SepInitializeCodeIntegrity +0x112 ~ +0x12b` — `R9 = &SeCiCallbacks`, `R8D = 5`, `CALL [CiInitialize]` (가져온 함수, `ci.dll`). 이 표에 직접 쓰는 줄은 `+0x52` 한 곳뿐이라 나머지 칸은 `CiInitialize` 안에서 채워진다 `[추측: CiInitialize 안은 안 읽음]`.
- `PspAllocateProcess +0x17bf`: 부르기 전 `+0x1728 R15≠0` (CreateInfo 있음) · `+0x1731 BL&2==0` · `+0x179e RCX=[R15+0xb8]≠0` · `+0x17ae` 포인터 ≠ 0. 인자 `RCX` = 이미지 파일 객체, `RDX = &[RSP+0x5a0]` (`+0x2a7 ~ +0x2b5` 에서 0 으로 민 0x218 바이트 버퍼). 반환이 음수면 `+0x17c6 JS`. 이 버퍼는 뒤에 `PspInitializeProcessSecurity` 쪽으로 간다 (run50 256).
- `PspAllocateProcess +0x1215`: 같은 버퍼 `&[RSP+0x5a0]` 를 `RCX` 로 준다. 앞서 말했듯 성공 꼬리에 속한다.
  - 성공: `+0x2828 ~ +0x2833 MOV EDI,[RSP+0xc4] / JMP 0x140a03734` (= `+0x11cc`). `+0x11cc` 부터 풀 해제 (`ExFreePoolWithTag`), `Feature_OutsideInProtection…` 판정 뒤 `+0x1201` 로 온다.
  - 실패: 많은 `JS 0x140a036f6` (= `+0x118e`) 이 `+0x11a1` 이후로 합류.
  - run50 255 의 "실패·정리 블록 안 `[추측]`" 은 **성공도 지난다** 로 고친다.
- `NtCreateUserProcess +0x11bb`: 공용 꼬리 `+0x11a0` (오류 `JS 0x140a024e0` 이 모두 여기로) 와 성공 `+0x108b JMP 0x140a024e3` (= `+0x11a3`) 가 같은 곳으로 들어온다. `RCX = [RBP+0x258]` (이미지 파일 객체, `+0x797` 에서 `ObReferenceObjectByHandle` 결과 저장). 객체 ≠ 0 이고 `[SeCiCallbacks+0xd0]` ≠ 0 이면 부름.

### 4

- 가름길 `+0xfad TEST BL,0x40 / +0xfb0 JZ → +0x103b`. 안 뛰면 `+0xfbb ~ +0xff6` (풀 할당 후 `PspWow64PickBestNtdll`) 를 탄다.
- 비트 `0x40` 은 `+0x92a OR EBX,0x40` (**복제 갈래**: `+0x8ab TEST DIL,BL` 안, 부모 `[R12+0x310]` WoW 정보 ≠ 0) 과 `+0xab3 OR EBX,0x40` (`+0xa87` `PspSelectMachineForProcess` 가 출력 `[RSP+0xd0]` 의 `+2` 바이트를 1 로 준 경우) 두 곳.
- `PspSelectMachineForProcess.asm`: 이미지 `Machine` (`[CreateInfo+0x68]`) = `0x8664`, 요청 기계 인자 `[+0xa8]` = 0, 부모 기계 `PsGetProcessMachine(parent)` = `0x8664` 이면 `+0x8f JZ` 로 건너뛰어 `+0xc5 ~ +0xd4` 에서 `[출력] = 0x8664`, `[출력+2] = DIL = 0` 을 쓴다. 이 바이트가 1 이 되는 곳은 `+0x103` / `+0x10a886` (WoW 지원 기계 판정 갈래) 뿐이다.
- 그래서 test.exe (x64) + 부모 cmd.exe (x64) → `+0xab1 JZ` 로 `0x40` 안 켜짐. **전제**: 부모가 x64 `cmd.exe` `[소스: 요청서]`.
- `+0xff6` 가 불리면 `EAX = PspWow64PickBestNtdll` 반환이 `[풀+8]` 에 들어간다 (run10 184 와 같은 자리).

### 5

- `+0x22e9 TEST BL,0x4 / JNZ +0x230f 건너뜀` (복제는 PEB 안 만듦), 아니면 `+0x230f MmCreatePeb(R14, &[RSP+0x1a8], &Peb 칸, &[RSP+0x210])`.
- `+0x2323 CMP [RSP+0x210],RSI(0) / +0x232b JZ → +0x23cc`(= `+0x2364`) 가 `PspSetProcessAffinitySafe` (`+0x235f`) 를 건너뜀. `+0x2248 BT EBX,8 / JNC → +0x2364` 로 PEB 만들기 전체를 건너뛰는 길도 있다 (비트 8 은 `+0x21e9 BTS` 가 켜 주니 test.exe 는 켜짐).
- `[RSP+0x210]` 는 `MmCreatePeb` 의 4번째 인자 (`R9`) 가 가리키는 16바이트: `MmCreatePeb +0x80 MOVUPS [RBX],XMM0` 으로 0 으로 비우고 `+0xb1` 에서 그룹 번호(+8)만 채운다. 마스크(+0)를 쓰는 곳은 `+0x34d ~ +0x356` (LoadConfig `[RCX+0x40]` 복사) 뿐이고, 뒤 `+0x621 ~ +0x63f` 는 마스크가 그룹 밖이면 0 으로 만든다.
- `RCX` = `RtlImageDirectoryEntryToData(SectionBaseAddress, TRUE, 디렉터리 10 = LoadConfig)` (`MmCreatePeb +0x2fc ~ +0x311`).
- **test.exe 의 `LoadConfig.ProcessAffinityMask` = 0** (PE 헤더 직접 읽음, `Size 0x140`). → 마스크 0 → 호출 안 함.

### 6

- `+0x3d7 ~ +0x3f2`: `R15≠0` 이고 `[R15+0x8]` (속성 모음) 비트 `0x12` 가 켜졌을 때만 `BTS EBX,0x10`. 속성 모음의 비트 번호 = 속성 번호 (`PspBuildCreateProcessContext +0x1e6 ~ +0x205`: `1 << (속성 & 0xff)` 를 모음에 OR, 중복이면 `0xc000000d`). 같은 모음의 비트 5 가 이미지 이름 (#15), 비트 13 이 선호 노드 (`PspAllocateProcess +0x375 ~ +0x39b` 가 `KeNodeBlock[[R15+0xaa]]` 를 읽음).
- `+0x2728 BT EBX,0x10 / +0x272c JNC → +0x27b3` 이 `+0x2745 MmGetSectionStrongImageReference` 와 `+0x279b KeSecureProcess` 를 같이 건너뜀.
- `kernelbase!CreateProcessInternalW`: `+0xa0b TEST [RSP+0x108],0x400000 / JZ` 로 건너뛴 쪽에서 속성 `0x20012` (`+0xa0c`) 를 넣는다. `[RSP+0x108]` = 생성 플래그 `[추측: dwCreationFlags]`, `0x400000` = 공개 헤더의 보안 프로세스 생성 플래그 `[추측: 이름]`. cmd 가 test.exe 를 일반 플래그로 만들면 이 속성이 없어서 비트 18 이 안 켜진다 → 안 탄다. (속성 번호 18 = 보안 프로세스 이름은 공개 헤더 기준이라 `[추측]`; 비트 18 → `KeSecureProcess` 연결은 `[정적]`.)

### 7

- `+0x19d` (ret `+0x1a2`) `Feature_…IsEnabledDeviceUsageNoInline` → `TEST EAX,EAX / +0x1a4 JZ → +0x416`.
  - **꺼짐(0)**: `+0x41f` 로 사용자 쪽 `RDI` 에 통째 복사 (`RtlCopyMemory`) → 안쪽 포인터를 `_Dst` 기준으로 고침 (`+0x424 ~ +0x4fb`) → **`+0x52b MOV [PEB+0x20],RDI`** (PEB = `[[현재 스레드+0xb8]+0x2e0]`, `+0x514 ~ +0x524`).
  - **켜짐(≠0)**: 커널 사본(`RBX`)에서 고친 뒤 `RtlCopyToUser` (`+0x321`) → **`+0x347` `RtlWriteULong64ToUser(PEB+0x20, 새 블록)`** (ret `+0x34c`). 필요하면 이어서 `PspCopyUserAttributes` (`+0x3eb`).
- 판정 함수 (`Feature_CreateProcessSegmentHeapEnablement__private_IsEnabledDeviceUsageNoInline.asm`): `상태 = [rva 0xfc4110]`; `상태 & 0x10` 이면 `결과 = 상태 & 1`, 아니면 `IsEnabledFallback(상태, 3)` (`…IsEnabledFallback.asm`: 기술자 `rva 0xde430` 와 `wil_details_IsEnabledFallback`). 상태 칸은 `.data` 의 0 으로 시작 (파일에 값 없음) 하고 부팅 때 구성이 넣는다.
- 같은 판정 함수를 `PspSetupUserProcessAddressSpace +0x124`, `+0x1a6` 도 부른다 (`_calls.tsv`).
- "PEB `+0x40c LoaderThreads`" (#12) 와 헷갈리지 말 것: `+0x20` 은 PEB 의 **`ProcessParameters` 포인터**이고 `+0x40c` 는 그 블록 안 칸이다.

### 8

- (a) `MiRelocateImage` 조각 (`FUN_140b24658`, 본체 기준 `+0x1b1214 ~`):
  - `+0x1b1269 MOV [RCX+0x20],RAX` — `RCX = [RSP+0x90]` = 인자 세그먼트, `RAX = [RSP+0x58]` = `MiSelectImageBase` 가 낸 새 주소 → **세그먼트 `BasedAddress`(+0x20) 를 새 주소로 바꿈**.
  - `+0x1b126d MOV RAX,[R14+0x28]` — `R14` = `MiRelocateImage +0x1a9` 에서 새로 할당한 재배치 묶음, `+0x28` 은 `+0x32a ~ +0x33a` 에서 `새 주소 − [RSP+0x88](= 원래 세그먼트 BasedAddress)` 로 채움.
  - `+0x1b1271 MOV R15,[RSP+0xc0]` (`= [세그먼트+0x38]` = 이미지 정보) → **`+0x1b1279 ADD [R15],RAX`** (`TransferAddress += 델타`).
  - `+0x1b1357 OR [R15+0x33],4` (`ImageDynamicallyRelocated`).
  - 이 조각에 닿는 길: 본문 `+0x58a ~ +0x58e` 의 `JNZ → 조각 +0x2c` 또는 `+0x7ac JMP → 조각 +0x0`. 일찍 나가는 길(`[세그먼트+0xc] 비트 14`, 구조 검사 실패 등 `+0x95 / +0xd6 / +0x1cc ...`)에선 안 탐. 델타 값 자체는 무작위(ASLR)라 정적으로 숫자는 못 냄.
- (b) `PspAllocateProcess +0x20a6 ~ +0x20ce`: `MmInitializeProcessAddressSpace` (ret `+0x2095`) 뒤, `R15`(CreateInfo)≠0 이고 `BL&2==0` 일 때:
  - `RAX = MiSectionControlArea(섹션)` (`MiSectionControlArea.asm`: `[섹션+0x28]` 의 하위 비트를 떼어 컨트롤 에리어를 꺼냄), `RCX = [RAX]` (= `_CONTROL_AREA.Segment`), `RDX = [RCX+0x20]` (= `_SEGMENT.BasedAddress`), `RAX = [R14+0x2b0]` (= `_EPROCESS.SectionBaseAddress`, `MiMapProcessExecutable +0xc5` 가 `MmMapViewOfSectionEx` 결과로 씀).
  - `RDX == RAX` 면 `+0x20c9 JZ` 로 건너뜀. 다르면 **`[R15+0x38] += (실제 − BasedAddress)`** (`+0x20cb SUB`, `+0x20ce ADD`). `[R15+0x38]` = `+0x986 ~ +0x98c MmGetSectionInformation` 이 세그먼트의 이미지 정보를 복사한 CreateInfo 사본의 `TransferAddress`.
- 합쳐서: 사본 = 헤더값 + (a 델타) + (b 델타). (a) 가 `BasedAddress` 를 새 주소로 옮겼으므로 `헤더 + (새−헤더) + (실제−새) = 실제` → **`TransferAddress = SectionBaseAddress + AddressOfEntryPoint (0x1d40)`** 가 언제나 성립. (a) 를 안 탄 경우 (`BasedAddress` = 헤더값) 도 (b) 가 `실제 − 헤더` 를 더해서 같은 결과. 이 항등식은 코드 읽은 추론이다 (`[정적]` 추론, 두 사본이 같은 세그먼트 이미지 정보 `+0x0` 이라는 전제 = run31 105 · 사본 위치는 `+0x986 ~ +0x98c` 로 확인).
- PE_emulator 는 두 단계를 흉내 낼 필요 없이 `TransferAddress = 실제 올린 베이스 + 0x1d40` 으로 두면 된다. 두 번째(b)가 0 이 아닌 경우는 "`MmMapViewOfSectionEx` 가 섹션에 기록된 주소가 아닌 곳에 올릴 때" 뿐인데, 새 프로세스 빈 공간에서는 같은 주소일 것이다 `[추측: MmMapViewOfSectionEx 안은 안 읽음]`.

### 9

- 사슬: ① 부팅 때 `PspMapSystemDll` 이 `PspSystemDlls[0]` (= `PspNativeSystemDllData`) 의 `+0x20` 에 올라간 뷰의 헤더 `ImageBase` 를 씀 (run48 188 `[정적]`) ② `PspInitializeThunkContext +0x196 / +0x19d / +0x1c0` 가 그 `+0x20` 을 CONTEXT `Rdx` (`[RBP+0x158]`) 에 넣음 `[정적]` (이번에 `.asm` 다시 봄) ③ `[관찰]` run44 첫 트랩 프레임 `Rdx` = ntdll 실제 베이스. ①~③ 이 이어져 `+0x20` = 실제 베이스. 직접 `dq` 로 본 적은 없음 (선택 확인 한 줄).

### 10

- `KxStartUserThread +0xe1 TEST byte [RBP+0xf0],1` (`RBP+0xf0` = 트랩 프레임 `SegCs`, `_KTRAP_FRAME.SegCs` 는 `+0x170` 이고 `RBP` 가 프레임 − 0x80) 이 0 이면 `+0xe8 JZ → +0x3e3` 으로 가서 **`+0x42c IRETQ`** (커널 모드 복귀). 켜지면 (유저 모드) APC 검사 루프 (`+0x10c ~ +0x125`) 를 돈 뒤 `+0x229` 에서 조각으로 가서 `조각 +0x79 TEST [KiKvaShadow],1` (본문 기준 `+0x3be`).
- `KiKvaShadow & 1 == 0` → `조각 +0x87 ~ +0x9c` (`+0x3cc ~ +0x3e1`): `SWAPGS` 뒤 `IRETQ` (**`KxStartUserThread +0x3e1`**). `== 1` → `+0x3c7 JMP KiKernelExit` (꼬리 호출) → `KiKernelExit +0x83 MOV CR3` … `+0xa2 SWAPGS / +0xa5 IRETQ` (**`KiKernelExit +0xa5`**).
- `KiKvaShadow` 를 1 로 쓰는 줄은 `KiEnableKvaShadowing +0x131` 하나 (`_refs/KiKvaShadow.tsv` 의 `+0x0` 쓰기). 그 함수가 켜는 조건: `+0x13 KiIsKvaShadowDisabled` 거짓 → `+0x23 SpcQueryKvaLeakagePresent` ≠ 0 (CPU 가 이 누설에 약함) 이거나 `+0x2c SpcIsKvaLeakageSimulated`. 즉 **CPU·VM 설정에 달려서 정적으로 못 정함.**
- test.exe 길은 둘 중 하나. (`NtContinueEx` 쪽 갈래는 run29.)

### 11

- 값이 만들어지는 곳: `KiInitializeContextThread` (`PspAllocateThread` → `KeInitThread` 가 부름).
  - `+0x30 ~ +0x3f`: `RDI = (InitialStack − KiXSaveAreaLength) & −0x40` = 새 스레드의 저장 영역 (`_KTHREAD.StateSaveArea`, `+0x60`).
  - `+0xae MOV [RDI+0x18],0x1f80` (MXCSR), **`+0x1ec MOV word [RDI],0x27f`** (x87 제어 워드), `+0x1f1 MOV [R11+0x2c],0x1f80` (트랩 프레임 MxCsr).
  - `+0x1f9 ~ +0x206`: `KeFeatureBits & 0x800000` 이면 `[RDI+0x200] = 1` (XSAVE 머리의 "x87 사용 중" 비트) → 복원 때 `0x27f` 가 로드됨.
  - `+0x150 ~ +0x15f`: `KxContextToKframes` 에 넘기는 ContextFlags 를 `(원래 & 6) | 0x100001` 로 바꿔 **부동소수점 비트(`0x8`) 를 뺀다** → 맡긴 CONTEXT 의 `FltSave` 는 안 옮겨지고 위 기본값이 남는다.
- 복원: `SwapContext` 조각 (`FUN_1406b686f`, 본문 `+0x4cc ~ +0x4e8`) — `RCX = [새 스레드+0x60]`. `+0x4cc FNINIT` 는 `RBP` 비트 `0x29` (`KiFpuLeakage` 가 켠 것, `KiInitializeKernel +0x105 ~ +0x119`) 이고 마스크 비트 0 일 때만, 그 다음 `+0x4db FXRSTOR` / `+0x4e0 XRSTORS` / `+0x4e5 XRSTOR [RCX]`, `+0x4e8 LDMXCSR [RCX+0x18]`. FNINIT 뒤에 바로 저장본을 복원하니 최종 제어 워드는 저장본 `0x27f`.
- 그 뒤 유저 진입 직전까지 `FNINIT`/`FLDCW`/`XRSTOR` 류가 있나: 추출해 둔 커널 함수 `.asm` 전체를 grep 한 결과 `FNINIT`·`XRSTOR`·`FXRSTOR` 는 `SwapContext`(위), `KeContextToKframes`, `KxContextToKframes`, `PspGetSetContextSpecialApc` 에만 있고 `FLDCW` 는 하나도 없다. `KxStartUserThread`·`KiInitializeUserApc`·`KiKernelExit` 에는 없다. (커널 전체를 훑은 것이 아니라 **추출해 둔 함수만**이다.)
- `PspSetContext` 도 x87 제어 워드를 스레드에 쓰는 줄은 못 찾았다 (읽은 범위 `+0x0 ~ +0x4a0`; xmm·MxCsr 만). `+0x2c2 ~ +0x2c7 AND [CONTEXT.FltSave.ControlWord],0x1f3f` 는 받은 CONTEXT 사본을 고치는 줄이다.
- 결론 `0x27f` = run44 `[관찰]` 맡긴 문맥 `FltSave.ControlWord` `0x27f` (`PspInitializeThunkContext +0x1fc / +0x201` 가 쓴 값) 과 같다. 확인 한 줄이 필요하면 `ntdll!LdrInitializeThunk` 에서 `r fpcw`.

### 12

- 초기값 0 (`rva 0xfd9e24`, PE 파일 속 `PAGEDATA` 가 0). 쓰는 줄: `PsBootPhaseComplete +0x121 LEA R9,[PsDefaultLoaderThreads]` → `+0x12f RtlQueryImageFileExecutionOptions` (ret `+0x134`), 값 이름은 UTF-16 문자열 `MaxLoaderThreads` (`rva 0xafb170`). `RtlQueryImageFileExecutionOptions` 첫 인자·`R8` 은 이 함수가 안 정함 (부른 쪽 인자 그대로) 이라 어느 레지스트리 키인지는 이 읽기로 못 정함.
- 읽는 줄: `PspSetupUserProcessAddressSpace +0xb8 CMP [RBP+0x40c],0` → `JNZ` 아니면 `+0xc1 MOV EAX,PsDefaultLoaderThreads / +0xc7 MOV [RBP+0x40c],EAX`. 즉 레지스트리에 값이 없으면 0.
- 여기 `[RBP+0x40c]` 는 PEB 가 아니라 **`RTL_USER_PROCESS_PARAMETERS`(프로세스 인자 블록)의 `LoaderThreads`** 다 (run44 196 이 이미 "ProcessParameters.LoaderThreads" 로 적음). 요청서의 "PEB `+0x40c`" 는 줄임말.
- `[관찰]` run44 196: 그 칸 0 (K1 ~ P2). 커널 사본이 0 이었으면 전역 값이 들어갔을 테니 전역 0 이 거의 확실하나 직접 읽지 않았다 → 싼 한 줄.

### 13

- `NtCreateUserProcess +0x59f MOV RAX,[0x140fd83d0]` (`g_kernelCallbacks+0xb0`) → `+0x5a6 TEST / +0x5a9 JZ` (널이면 건너뜀, `ESI = 0`) → `+0x5ab MOV RCX,[RBP+0x240]` → `+0x5b2 CALL` (ret `+0x5b7`) → `+0x5b7 MOV ESI,EAX / +0x5bb JS → +0x11a0` (실패).
- `[RBP+0x240]` = 새 프로세스 토큰: `+0x417` `PspReferenceTokenForNewProcess(…, &[RBP+0x240])` 가 채움.
- `g_kernelCallbacks` = rva `0xfd8320`. 표를 넘기는 곳: `Ordinal_25.asm` `+0x14 LEA RDX,[g_kernelCallbacks]` → `+0x45 CALL [ClipSpInitialize]` (`clipsp.sys` 가져온 함수; 같은 함수가 `+0x0 ~ +0x3e` 에서 전역 4칸 `0x140fd8440 ~ 0x140fd8458` 에 함수 포인터·`SeExports` 도 적음). 그래서 표 칸(`+0xb0`) 대상은 `clipsp.sys` 안일 것이다 `[추측: ClipSpInitialize 안은 안 읽음]`.
- 이 표를 읽는 다른 이름 (`SPCallServerHandleUpdateLicense`, `SLQueryLicenseValueInternal`, `ExpGenuinePolicyPostProcess` 등) 이 소프트웨어 보호·라이선스 쪽이라 이 표는 그쪽 콜백 표로 보인다 `[추측]`.

### 15

- 가름길 `+0xce0 CMP [RBP+0x270],0 / +0xce7 JZ → +0xd24` (0 이면 `PspGetContextThreadInternal` ret `+0xd3e`). 아니면 `PspCreateUserContext` (ret `+0xd22`).
- `[RBP+0x270]` 를 쓰는 곳 (`.asm` 전체): `+0x978` (섹션 객체 저장), `+0xa36` (서명 수준이 달라 섹션을 닫고 0 으로 지움 → `+0xa43 JMP +0x912` 로 **다시 만들어** `+0x978` 이 또 씀), `+0xaec` (`ObReferenceObjectByHandle` 실패 → `JMP +0x11a0` 으로 오류 종료, `PspAllocateProcess` 안 감). 그래서 `PspAllocateProcess` 까지 간 길에서는 항상 non-null.
- 섹션 만들기를 건너뛰는 길은 하나 — `+0x463 ~ +0x46b` (`[RBP+0x1a8]` 속성 모음 비트 `0x20` = 이미지 이름이 없을 때 `JZ → +0xb09`): 호출자와 같은 프로세스를 부모로 복제하는 길 (`CMP R15,RBX`, `R12B`, `[RBP+0x60]` 검사). test.exe 는 이 길이 아니다:
  - 속성 모음 비트 번호 = 속성 번호 (#6).
  - `kernelbase!CreateProcessInternalW +0x85d MOV [RSP+0xfa8],0x20005` — 속성 번호 5 (이미지 이름) 를 항상 넣음 `[정적]`.
- 같은 `[RBP+0x270]` 가 `+0xc0c ~ +0xc13` 에서 7번째 인자로 `PspAllocateProcess` 에 가는 값 (#1).

### 16

- `PspSetContext +0x1b8 ~ +0x1c4`: `ContextFlags (ESI)` 가 `0x100008` (AMD64 + 부동소수점) 을 전부 가져야 `+0x1ca` 이후 FP 복사로 가고 `+0x2a7 MOV EAX,[CONTEXT+0x34] (MxCsr) / +0x2aa AND EAX,[KiMxCsrMask] / +0x2b0 MOV [트랩 프레임+0x2c],EAX`.
- 이 함수는 `PspGetSetContextInternal +0x3ea` 한 곳에서만 불린다 (`_calls.tsv`). 부르는 쪽: `PspGetSetContextSpecialApc +0x64` (스레드를 멈춘 뒤 도는 APC) ← `PspSetContextThreadInternal` ← **`PspInitializeThunkContext +0x2d6`** (`+0x2be MOV [RSP+0x20],2`, `+0x2cc LEA RDX,[RBP+0xd0]` 는 `+0x1a8 ~ +0x208` 로 만든 두 번째 CONTEXT: `ContextFlags 0x10000b` , `MxCsr 0x1f80`, `FltSave.ControlWord 0x27f`, `FltSave.MxCsr 0x1f80`).
  - 그래서 run44 가 본 P0 ~ P1 의 EFlags 바뀜 (`PspSetContext +0x5a`) 과 같은 호출이다. 입력 `MxCsr` = **`0x1f80` 상수**.
  - `PspGetSetContextInternal` 이 get 이 아니라 set 갈래(`+0x3ea`)로 가는 조건 (`+0x89 CMP [R14+0x58]`, `+0x3b4 CMP R9B,1` 근처) 은 자세히 안 따라갔다. 호출이 존재한다는 것까지만 `[정적]`.
- `KiMxCsrMask` 값: `KiInitializeKernel +0xe2 ~ +0xef` (0x200 바이트 버퍼 `memset` 후 `FXSAVE`), `+0x214 ~ +0x22d`: `EAX = 0xffbf; [RSP+0xac]` (= FXSAVE 영상의 `MXCSR_MASK`, +0x1c) 가 0 이 아니면 그 값 → `MOV [KiMxCsrMask],EAX`. 파일 속 처음 값도 `0xffbf` (`ALMOSTRO` 구역, 4바이트 `bf ff 00 00`).
- `0x1f80` = 비트 7 ~ 12 (여섯 예외 마스크). `0xffbf` 는 비트 6 (DAZ) 만 뺀 값이라 `0x1f80` 이 그대로 남는다. 칩이 보고하는 `MXCSR_MASK` 도 이 여섯 비트는 항상 쓰기 가능하다 (아키텍처 정의라 코드가 아닌 CPU 규격에서 온 근거; `[추측: SDM 기억]`) → `0x1f80 & 마스크 = 0x1f80`.
- `KxContextToKframes +0x30a` 의 `& KiMxCsrMask` 는 이 길에서 안 탄다 (sast/47 101) — `+0x150 ~ +0x15f` 에서 FP 비트가 빠지기 때문 (#11).

### 17

- `MiCreateImageFileMap.asm`: `+0x17b MOV RDX,[R13+0x18]` (헤더 페이지 주소), `+0x17f MOV EAX,0x5a4d` (`MZ`), `+0x188 CMP word [RDX],AX`, `+0x18b JNZ → +0x6bc`.
- `+0x6bc MOV R14D,0xc000012f` → `+0x6c2 JMP → +0x666 → +0x5b6 MOV RBX,R15 → +0x5f7` (아래 정리: `MiDeleteImageCreationMdls` 등) → `+0x46a TEST R12,R12` (`JNZ → +0x896` 파일 범위 해제 뒤 `+0x473`) → **`+0x473 MOV EAX,R14D` → `+0x495 RET`**. `R14` 는 피호출자 보존 레지스터라 정리 호출들이 안 바꿈 (정리 코드에 `R14` 쓰는 줄 없음).
- 이 갈래만 실패 이유 번호 전역 (`[0x140e2d838]`) 에 값을 **안 쓴다** (`+0x6b0`·`+0x6ca`·`+0x6dc` 같은 다른 실패 갈래는 `0x26`·`0x2a`·`0x2b` 등을 씀).
- 같은 함수의 다른 실패값: `+0x6c4 / +0x6d6` (`0xc0000130`, `MZ` 뒤 NT 헤더 오프셋 검사), `+0x700 / +0x715` (`0xc000007b`).
- 이름 `STATUS_INVALID_IMAGE_NOT_MZ` 는 공개 NTSTATUS 이름 기억이라 `[추측: 이름]`, 값 `0xC000012F` 는 `[정적]`.

### 요청서 밖에서 본 것

- #5 근처: `PspAllocateProcess +0x2248 BT EBX,8 / JNC → +0x2364` — 이 비트가 꺼지면 `PsQueryProcessAttributes`·`MmCreatePeb`·`PspSetProcessAffinitySafe` 를 모두 건너뛴다. 켜는 줄은 `+0x21e9 BTS EBX,8` (`+0x21c1 TEST R13B,BL` 와 `+0x21e4 TEST BL,2` 둘 다 0 일 때). test.exe 는 켜짐.
- `MmCreatePeb` 가 `PEB` 안 몇 칸의 값을 PE 헤더에서 읽는다: `+0x2c6 ~ +0x2db` (`RtlImageNtHeader`), `+0x333 ~ +0x3fa` (`Win32VersionValue`·서브시스템 버전·LoadConfig `CSDVersion` 등). #5 는 LoadConfig 한 칸만 봤다.

---

## 동적분석으로 확인해야 할 것 (dast 요청 필요: 있음)

- **dast 요청 필요: 위 "C 항목 묶음" 1)** — 커널 디버거로 붙은 아무 때나 전역 5 ~ 7줄. 멈춤점 없이도 #2·#3·#10·#13·#14 와 #7(조건부) · #12 가 닫힌다. test.exe 한 번 실행이 필요한 것은 #7 에서 상태 비트 4 가 꺼져 있을 때의 `r eax` 하나뿐이다 (그리고 선택 확인 f·g).
- 이번에 정적으로 못 낸 것: (1) CI/clipsp 가 등록한 함수의 정체와 그 안에서 하는 일 — 이 문서 범위 밖 (`CiInitialize`·`ClipSpInitialize` 는 가져온 함수, 안 읽음); (2) `MmMapViewOfSectionEx` 가 섹션에 기록된 주소와 같은 곳에 올리는지 (8번 두 번째 더하기가 0 인지; 최종값은 같다); (3) `PspGetSetContextInternal` 의 get/set 가름길 조건 (16번 "탄다" 의 마지막 고리).
- 정정할 문서 줄 (flow-writer 몫, 이 에이전트는 안 고침): run50 255 "실패·정리 블록 안 `[추측]`" → 성공·실패 공용 꼬리; run32/run44 의 "PEB `+0x40c`" → 프로세스 인자 블록 `+0x40c`; run16 87 "섹션 참조 0 이면 `+0xd3e`" 는 맞음 (test.exe 는 0 이 아니라 `+0xd22`).

## 판정

**분석성공.** 요청서의 17항목을 모두 A/B/C 로 가르고 근거 줄을 달았다. 정적으로 닫힌 것 10개 (A 2: #1·#9 / B 8: #4·#5·#6·#8·#11·#15·#16·#17), 절반만 닫힌 것 2개 (#3·#14: 타는지·이름은 정적, 대상은 동적), 동적으로 읽어야 하는 것 5개 (#2·#7·#10·#12·#13). 동적 값은 대부분 전역 한 줄 읽기. 버전 대조 일치, 스크립트 문제 없음.

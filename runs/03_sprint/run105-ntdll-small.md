# 2026-11-16 (작성일) run105 — ntdll 작은 정적 다섯 (Q66 + Q67, 정적만): **부모의 `CsrClientCallServer` 는 csrss 답을 기다린다** — `NtAlpcSendWaitReceivePort` 플래그 `0x20000`, 받는 칸 = 보낸 칸, 시간 제한 없음, 답 status = 메시지 `+0x34` `[정적]`. **`0x7ffe0308` = KUSD `SystemCall` (4B)**, 스텁은 bit 0 만 보고 사진 8장 모두 0 → `SYSCALL` 길 `[정적+관찰]`. **`LdrpInitializeProcess +0x1625` 가 끼우는 항목은 exe 도 ntdll 도 아닌 `AvrfpLoaderEntry` 의 복제** (App Verifier 만, test.exe 안 탐). **`LdrpNtdllHotPatchContext` 는 핫패치가 꺼져도 `LdrpInitializeInternal +0xa7` 이 스택 주소를 쓴다** — 사진 P1a·P1b `0x9b2fcff9d0` (메인 스레드 스택 안) 이 그 값이다 `[관찰]`. ntdll 예외 표 등록 `+0x415` 가 `InExceptionTable` 켜기 (`+0x119d` 안) 보다 먼저 `[정적]`.

(오프셋은 call 자리. ret 자리는 아래 "정적 — 호출 지점" 표.)

## 환경

- 26100.9457, **정적만** (VM 실행 없음). 값 대조는 run44 사진 덤프 읽기 로그를 다시 읽은 것뿐이다 (아래 "사진 로그 대조").
- 정적: **ntdll pdb `C3093720177DA6851519DCFFE8DF53FE1`** (기준과 같음). 다시 analyze 안 함, export 셋 (결과서 — `ms/ghidra/ntdll.export.sast99a·b·c.stdout`).
  - 버전 대조 (결과서): `LdrpInitializeProcess +0x1bb6 / ret +0x1bbb → LdrpInitializeGraphRecurse`, `LdrpInitializeInternal +0x55 / ret +0x5a → _LdrpInitialize` 그대로 (`_calls.tsv`).
- KUSD 칸 이름은 PDB 타입 (`pdb_struct.py ntdll.dll _KUSER_SHARED_DATA`, 크기 `0xa80`, ntoskrnl PDB 도 같은 칸). "커널 중 누가 `+0x308` 을 쓰나" 는 ntoskrnl Ghidra 프로젝트를 열지 않고 (다른 decompiler 가 쓰는 중) `objdump -d` 꼴 훑기로만 봤다 — **"이 방법으로는 안 보임"** 까지다.
- 결과서 메모: 요청서는 depth 0 셋 · depth 1 둘이었는데 한 정규식에 묶어 다섯 모두 depth 1 로 뽑았다 (이미 있던 파일 ~100개가 같은 스크립트로 다시 써짐, 내용 같음). 길을 따라가려고 depth 0 둘 (`LdrpInitializeApplicationVerifierPackage` · `AVrfInitializeVerifier`) 을 더 뽑았다.
- 다른 decompiler 가 같은 시간에 ntoskrnl (sast/112) · ntdll (sast/113) 을 돌리는 중이다. **sast/100 · 102 · 103 · 105 ~ 113 결과는 이 문서에 안 넣었다** (KUSD 다른 칸 = sast/110 몫).

## 목적

- **Q66** (run34 flow-writer "새 공백", 부모 쪽):
  - ① ntdll syscall 스텁 `+0x8 TEST byte [0x7ffe0308],1` 의 칸 이름 · 크기 · bit 0 뜻, 누가 쓰나, 사진 값.
  - ② `CsrClientCallServer` 가 csrss 답을 **기다리나** (보내고 받기인가 보내기만인가), 답 status 를 어디서 읽나, 실패를 돌려주나.
- **Q67** (sast/30 · run35 "새로 생긴 공백", ntdll 항목 주변):
  - (a) `LdrpInitializeProcess +0x415/+0x41a RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)` 가 맞나, `InExceptionTable` 미리 켜기와 어느 쪽이 먼저인가.
  - (b) `+0x1625 LdrpInsertDataTableEntry` 의 인자 `[RSP+0x60]` 정체.
  - (c) `LdrpLoadPatchedNtdll` 이 `[ctx+0]` 에 쓰는 것, `LdrpNtdllHotPatchContext` 와의 연결, test.exe 에서 타나.
- 앞 바퀴 짝: run34 (`runs/02_sprint/run34-parent-leftovers.md` — 스텁 모양 · `0x1001d`), run68 (`runs/03_sprint/run68-dll-init-rest.md` — `CsrpConnectToServer`), run18 · run35 (`runs/02_sprint/run18-proc-init-setup.md` · `run35-ntdll-entry.md` — `+0x41a` · `LdrpInsertDataTableEntry`), run60 (`runs/03_sprint/run60-func-table-mrdata.md` — 역함수 표 칸 0 · 1), run17 (`runs/02_sprint/run17-ldr-init-entry.md` — 로더 입구 · 핫패치 준비).

## 방법

- 정적: `_workspace/sast/99_ntdll-small.md` — ntdll
  - `^(LdrpLoadPatchedNtdll|CsrClientCallServer|LdrpInitializeProcess|RtlInsertInvertedFunctionTable|LdrpInsertDataTableEntry)$` depth 1 (121개) + depth 0 `LdrpInitializeApplicationVerifierPackage` (rva `0x116330`) · `AVrfInitializeVerifier` (rva `0x119de4`).
  - `_refs` 넷: `CsrClientProcess` · `CsrServerApiRoutine` · `CsrPortHandle` · `CsrPortMemoryRemoteDelta`.
  - 이미 있던 `.asm` 읽기만: kernelbase `CreateProcessInternalW`, csrsrv `CsrApiRequestThread`, ntdll `CsrClientConnectToServer` · `LdrpInitializeInternal` · `LdrpInitializeExecutionOptions` · `LdrpInitializeNtdllDataTableEntry` · `LdrpProcessMappedModule` · `LdrpInitializeHotPatching` · `LdrpQueryCurrentPatch` · `LdrpLoadCustomNtdllWithSectionHandle` · `NtCreateUserProcess` 외 스텁.
- 결과서: `_workspace/sast/99_ntdll-small_result.md` (**분석성공** — Q66-① 쓰는 커널 줄은 "안 보임" 까지, Q67-c test.exe 켜짐 여부는 동적으로 넘김).
- flow-writer 가 다시 본 것:
  - `.asm`: `CsrClientCallServer` (`+0x14` · `+0x1e` · `+0x48` · `+0x51` · `+0x69` · `+0x6f` · `+0x76` · `+0x81` · `+0xbb` · `+0xc2` · `+0xc7` · `+0xcc` · `+0xcf` · `+0xd4` · `+0xd7` · `+0xdc` · `+0xdf` · `+0xe4` · `+0xed` · `+0xf2` · `+0x12c` · `+0x12f` · `+0x131` · `+0x135` · `+0x147` · `+0x182` · `+0x1b8`), kernelbase `CreateProcessInternalW` (`+0x464f` · `+0x4655` · `+0x4663` · `+0x466b` · `+0x4672` · `+0x4677` · `+0x4680` · `+0x4682` · `+0x4687` · `+0x4695`), csrsrv `CsrApiRequestThread` (`+0x177 ~ +0x18f` · `+0x4e1` · `+0x4e6` · `+0x4fb` · `+0x651` · `+0x656` · `+0x6d9 ~ +0x6ed` · `+0x751` · `+0x790`), `CsrClientConnectToServer` (`+0x81` · `+0x8c` · `+0x8f`), `LdrpInitializeInternal` (`+0x5c` · `+0x5e` · `+0x98` · `+0x9f` · `+0xa4` · `+0xa7` · `+0xae` · `+0xb3` · `+0xb5` · `+0xbd` · `+0xc2`), `LdrpInitializeProcess` (`+0xe8` · `+0x415` · `+0x41a` · `+0x507` · `+0x50c` · `+0x531` · `+0x119d` · `+0x11a2` · `+0x1233` · `+0x123a` · `+0x1240` · `+0x124e` · `+0x1250` · `+0x1256` · `+0x126a` · `+0x1568` · `+0x156f` · `+0x1577` · `+0x1625` · `+0x18ef` · `+0x18f6` · `+0x18fa` · `+0x1916`), `AVrfInitializeVerifier` (`+0x40` · `+0x43` · `+0x2ac` · `+0x2bf` · `+0x3d9` · `+0x3f2` · `+0x3fd` · `+0x414`), `LdrpInitializeApplicationVerifierPackage` (`+0xc8` · `+0xd3` · `+0xe7` · `+0xf2`), `LdrpInitializeHotPatching` (`+0x18` · `+0x39` · `+0x3d` · `+0x40` · `+0x45` · `+0x4c` · `+0x53` · `+0x55`).
  - `_calls.tsv` (ntdll): `CsrClientCallServer` 2줄, `LdrpLoadPatchedNtdll` 6줄, `LdrpLoadCustomNtdllWithSectionHandle` 4줄, `LdrpQueryCurrentPatch` 3줄, `RtlInsertInvertedFunctionTable` 6줄, `LdrpInitializeInternal` 6줄, `LdrpInitializeHotPatching` 1줄, `LdrpInitializeApplicationVerifierPackage +0xf2/+0xf7 AVrfInitializeVerifier`, `LdrpInitializeExecutionOptions +0x75e/+0x763 LdrpInitializeApplicationVerifierPackage`, `AVrfInitializeVerifier +0x3f2/+0x3f7 AvrfMiniLoadDll`.
  - `_refs`: `LdrpIsHotPatchingEnabled.tsv` (6줄 — 쓰기 2 `LdrpInitializeHotPatching +0x55` · `LdrpInitializeProcess +0x587`), `LdrpNtdllHotPatchContext.tsv` (5줄 — 쓰기 2 `LdrpInitializeInternal +0xa7` · `+0x5e`, 읽기 3), `AvrfAppVerifierMode.tsv` (7줄 — 쓰기 1 `AVrfInitializeVerifier +0x414`).
  - `alias:` 없음 (이번에 문서에 적은 callee 는 모두 이름 하나). `AVrfInitializeVerifier` · `LdrpInitializeApplicationVerifierPackage` 의 `alias:LdrQueryImageFileKeyOption+1` 줄은 길 밖이라 안 적었다.

## 결과

비유 (관리사무소 = 커널, 관리실 = ntdll, 주민센터 = csrss):

- 부모가 주민센터에 전입 신고서를 낼 때 **창구 앞에서 답을 기다린다**. 신고서 한 장 (메시지 칸) 을 내밀고, 주민센터가 같은 종이의 `+0x34` 칸에 "처리 결과" 를 적어 돌려줄 때까지 안 돌아간다. 마감 시간도 없다.
- 관리실 전화기 (syscall 스텁) 옆 스위치 (`SystemCall` 칸) 는 이 VM 에선 늘 꺼져 있다 → 늘 "직통 전화 (`SYSCALL`)" 로 건다.
- 핫패치 수첩 칸 (`LdrpNtdllHotPatchContext`) 은 쓸 일이 없어도 **빈 메모지 주소를 늘 꽂아 둔다**. 메모지 (16B) 는 비어 있어서 읽는 쪽 셋이 다 지나간다.

### 정적 — 호출 지점 (`ms/disassemble/ntdll/_calls.tsv` · `.asm`)

| caller | call | ret | callee | 조건 · 뜻 | 표시 |
|---|---|---|---|---|---|
| `KERNELBASE!CreateProcessInternalW` | `+0x466b` | `+0x4672` | `ntdll!CsrClientCallServer` (IAT `[0x18029ad00]`) | `RCX` = 메시지 `[RSP+0xbe0]`, `R8D` = `0x1001d`, `R9D` = `0x218` | [정적] (run14 · run34 그대로) |
| `ntdll!CsrClientCallServer` | `+0xed` | `+0xf2` | `ntdll!NtAlpcSendWaitReceivePort` (syscall `0x8e`) | `CsrClientProcess ≠ 0` (클라이언트 길). **Flags `0x20000`, 보내는 칸 = 받는 칸, 시간 제한 0** | [정적] |
| `ntdll!CsrClientCallServer` | `+0x182` | `+0x187` | (CFG 간접) `CsrServerApiRoutine` | `CsrClientProcess == 0` (csrss 자신 · 네이티브) — test.exe 부모는 안 탐 | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x400` · `+0x415` | `+0x405` · `+0x41a` | `RtlImageNtHeaderEx` · **`RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)`** | 조건 없음 (이 구간), run18 · run60 그대로 | [정적] |
| `ntdll!RtlInsertInvertedFunctionTable` | `+0x29` · `+0x44` · `+0x4b` · `+0x5d` · `+0x67` · `+0x87` | `+0x2e` · `+0x49` · `+0x50` · `+0x62` · `+0x6c` · 꼬리 | `RtlImageDirectoryEntryToData(…, 1, 3, …)` · 락 · `LdrProtectMrdata(0)` · `RtlpInsertInvertedFunctionTableEntry` · `LdrProtectMrdata(1)` · `RtlReleaseSRWLockExclusive` | | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x119d` | `+0x11a2` | `LdrpInitializeNtdllDataTableEntry` (첫째) | 조건 없음. 안 `+0x93 OR [RBX+0x68],0x204` → `InExceptionTable` | [정적] (run35 그대로) |
| `ntdll!LdrpInitializeProcess` | `+0x531` | `+0x536` | `LdrpInitializeExecutionOptions` | 6번째 인자 = `&[RSP+0x60]` (`+0x507` · `+0x50c`) | [정적] |
| `ntdll!LdrpInitializeExecutionOptions` | `+0x75e` | `+0x763` | `LdrpInitializeApplicationVerifierPackage` | 그 칸 주소를 그대로 (`+0x3f` · `+0x73d` · `+0x74c`) | [정적] |
| `ntdll!LdrpInitializeApplicationVerifierPackage` | `+0xf2` | `+0xf7` | `AVrfInitializeVerifier` (4번째 인자 0, `+0xe7`) | 그 칸 주소를 그대로 (`+0xc8` · `+0xd3`) | [정적] |
| `ntdll!AVrfInitializeVerifier` | `+0x3f2` | `+0x3f7` | `AvrfMiniLoadDll(…, [RSP+0x20] = &AvrfpLoaderEntry)` | 4번째 인자 0 길 (`+0x43 JZ → +0x2ac`). 성공하면 `+0x3fd` 칸에 `&AvrfpLoaderEntry` | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x1577` · `+0x1625` | `+0x157c` · `+0x162a` | `LdrpAllocateModuleEntry(0)` · **`LdrpInsertDataTableEntry(RBX)`** | `AvrfAppVerifierMode ≠ 0` (`+0x1568` · `+0x156f JZ → +0x1753`) — test.exe 안 탐 | [정적] |
| `ntdll!LdrpInitializeInternal` | `+0x93` | `+0x98` | `LdrpInitializeHotPatching` | 처음 온 스레드 | [정적] (run17 그대로) |
| `ntdll!LdrpInitializeHotPatching` | `+0x40` | `+0x45` | `NtManageHotPatch(클래스 9, {1}, 8)` | `ProcessParameters.Flags & 0x2000000` 꺼짐 (`+0x18`) | [정적] |
| `ntdll!LdrpInitializeInternal` | `+0xbd` | `+0xc2` | `LdrpLoadPatchedNtdll(ntdll 시작, &ctx)` | `LdrpIsHotPatchingEnabled ≠ 0` (`+0x98` · `+0xb3 JZ → +0xc6`) | [정적] (run17 그대로) |
| `ntdll!LdrpLoadPatchedNtdll` | `+0x49` · `+0xc4` · `+0xe8` · `+0x12c` · `+0x138` · `+0x152` | `+0x4e` · `+0xc9` · `+0xed` · `+0x131` · `+0x13d` · `+0x157` | `LdrpQueryCurrentPatch` · `NtManageHotPatch` (클래스 `0xa`) · `LdrpLoadCustomNtdllWithSectionHandle` · `NtManageHotPatch` (클래스 7) · `NpClose` · `NpUnmapViewOfSection` | 둘째 ~ 넷째는 "패치 있음" 표시 1 일 때만 (`+0x5b` · `+0x5d JNZ → +0x8b`) | [정적] |
| `ntdll!LdrpQueryCurrentPatch` | `+0x58` · `+0x9b` | `+0x5d` · `+0xa0` | `NtManageHotPatch` (클래스 8, 크기 `0x30`) · `LdrpIsImageAddress` | | [정적] |
| `ntdll!LdrpLoadCustomNtdllWithSectionHandle` | `+0x49` · `+0x5a` · `+0x7a` · `+0x98` | `+0x4e` · `+0x5f` · `+0x7f` · `+0x9d` | `NpMapViewOfSection` (`BaseAddress` 출력 = `&ctx[0]`) · `LdrpProtectAndRelocateImage` · `RtlImageNtHeaderEx` · `NpUnmapViewOfSection` | | [정적] |
| `ntdll!LdrpInitializeProcess` | `+0x126a` | `+0x126f` | `LdrpInitializeNtdllDataTableEntry` (둘째, 패치 항목) | `[ctx+0] ≠ 0` (`+0x1250` · `+0x1256 JZ → +0x1277`) | [정적] (run35 그대로) |
| `ntdll!LdrpInitializeProcess` | `+0x1916` | `+0x191b` | `LdrpLogEtwHotPatchStatus` | `[ctx+0xc] ≠ 0` (`+0x18f6` · `+0x18fa JZ → +0x191d`) | [정적] |

### Q66-① `0x7ffe0308` = KUSD `SystemCall` (결과서 Q66 ①)

- **칸** `[정적]` (PDB `_KUSER_SHARED_DATA`, 두 PDB 같음): `+0x308 SystemCall`, `unsigned long` (4B). 비트 이름 없음. 이웃 `+0x300 QpcFrequency` (8B) · `+0x30c Reserved2` (4B).
- **bit 0 뜻** `[정적]`: ntdll 의 시스템 콜 스텁은 모두 `TEST byte [0x7ffe0308],1` 만 본다 — 꺼지면 `+0x12 SYSCALL`, 켜지면 `+0x10 JNZ → +0x15 INT 0x2e`. ntdll 전체에서 이 칸을 읽는 곳은 **494곳, 전부 24B 스텁** (run56 이 센 스텁 수와 같음). 스텁 밖에서 읽는 ntdll 코드는 없다. kernel32 · user32 는 0곳.
- **누가 쓰나**: **이 방법으로는 안 보임** `[정적, 한계 있음]`. ntoskrnl 전체 `objdump` 에서 KUSD 커널 주소 `0xfffff78000000XXX` 를 레지스터에 올리는 1026곳 뒤 150줄 안 `+0x308` 읽기 · 쓰기 0곳, 직접 주소 `0xfffff78000000308` 0곳, `0x7ffe0308` 0곳. 같은 방법이 `+0x300` · `+0x320` 은 잘 잡는다. 레지스터를 여러 번 옮기거나 포인터를 넘겨 남이 쓰는 길은 못 본다 — "쓰는 곳 없음" 이 아니다.
  - KUSD 쪽을 만드는 `MiInitializeSharedUserData` (rva `0xc5c878`) 는 할당 · PTE · 매핑만 하고 칸을 안 채운다 (결과서 `objdump`).
- **사진** `[관찰]` (run44): 커널 별칭 `fffff78000000000` 8장 모두 `+0x308 SystemCall : 0` (`ms/logs/04_baggage-read_supp_<K1 ~ P2>.txt:246`), 유저 주소 P1a · P1b · P2 도 0 (`04_baggage-read_P1a.txt:2954` · `_P1b.txt:4072` · `_P2.txt:4084`).
  - → **test.exe 의 syscall 은 모두 `SYSCALL` 길** `[정적+관찰]`. 이 칸이 디버거 때문에 바뀐다는 증거는 못 봤다.

### Q66-② `CsrClientCallServer` — 기다린다 (결과서 Q66 ②)

- 함수 `ntdll!CsrClientCallServer` rva `0xdbdd0`, 449B. 인자 (이름은 우리가 붙임): `RCX` 메시지 칸 (`RBX`), `RDX` 캡처 버퍼 (널 가능), `R8D` API 번호, `R9D` 데이터 길이.
- **`+0xed NtAlpcSendWaitReceivePort` 인자** `[정적]`:

| 인자 | 값 | 줄 |
|---|---|---|
| 포트 | `CsrPortHandle` (rva `0x1d4e20`) | `+0xbb` |
| Flags | **`0x20000`** | `+0x48 MOV R10D,0x20000` → `+0xdc MOV EDX,R10D` |
| 보내는 메시지 · 속성 | 메시지 칸 · 0 | `+0xd4 MOV R8,RBX` · `+0xcc XOR R9D,R9D` |
| **받는 메시지** | **같은 칸** | `+0xdf MOV [RSP+0x20],RBX` |
| 받는 크기 칸 | `[RSP+0x40]` = `0x3b8` | `+0xc2` · `+0xd7` · `+0xe4` |
| 받는 속성 · **시간 제한** | 0 · **0 (없음)** | `+0xc7` · `+0xcf` |

- 그래서 **"보내기만" 일 수 없다** — 받는 칸이 있고 시간 제한이 없으니 답이 와야 돌아온다 `[정적]`. `0x20000` 의 이름 (`ALPC_MSGFLG_SYNC_REQUEST`, 동기 요청) 은 공개 헤더에서 빌린 것 `[추측: 이름]`. 알림 가능 대기 비트 (`0x200000` 류) 는 값에 없다 `[추측: 비트 이름]`.
- **답 status = 메시지 `+0x34`** `[정적]`. 메시지 모양: `+0x0` 길이 둘 (데이터 `R9+0x18` · 전체 `R9+0x40`), `+0x28` 캡처 버퍼, `+0x30` API 번호 (`+0x51`), `+0x34` 반환 status.
  - 돌아오는 길: `+0xf2 MOV R8D,EAX` → `+0x12c TEST` → 음수가 아니면 `+0x12f JNS → +0x135 MOV EAX,[RBX+0x34]` → `+0x147 RET`. **ALPC 자체가 실패하면** `+0x131 MOV [RBX+0x34],R8D` 로 그 값을 칸에 넣고 같은 `+0x135` 로. 반환값은 늘 `+0x34` 안 값.
  - csrss 쪽 짝 (이름만, 결과서 + flow-writer `.asm`): `csrsrv!CsrApiRequestThread` 가 처리기를 부르고 (`+0x4e1` 또는 `+0x651`) `+0x656 MOV [RSP+0xe4],EAX` (메시지 `[RSP+0xb0]` + `0x34`) 로 쓴다. 모르는 번호는 `+0x751` · `+0x790` 이 `0xc00000af`. **두 쪽이 `+0x34` 로 맞는다.**
- **부모는 반환값 `EAX` 대신 `+0x34` 를 다시 읽는다** `[정적]`: `CreateProcessInternalW +0x4677 MOV ECX,[RSP+0xc14]` (= `0xbe0 + 0x34`) → `+0x4680 JNS`. 음수면 `+0x4682 BaseSetLastNTError` → `+0x4695 JMP → +0xb61` (실패 끝맺음). **csrss 알림이 실패하면 `CreateProcess` 가 실패로 끝난다.**
- **갈래 셋 (test.exe 부모는 안 탐, 코드에는 있음)** `[정적]`:
  - `+0x14 CMP [LdrpIsSecureProcess],0` → `+0x1e JNZ → +0x1b8` `0xc00000bb` (안 보냄).
  - `+0x6f CMP [CsrClientProcess],0` → `+0x76 JZ` → **같은 프로세스 안 `CsrServerApiRoutine` 간접 호출** (`+0x182`, `ClientId` 16B 를 `[msg+8]` 에). `CsrClientProcess` 는 `CsrClientConnectToServer +0x8f` 가 exe `Subsystem ≠ 1` 일 때 1 로 쓴다 (`+0x81`) → CUI · GUI 부모는 ALPC 길. 부모 = cmd.exe (CUI) `[소스: 실행 방법]` — 부모 헤더는 안 읽음.
  - PEB `+0x3` 비트 1 (`IsProtectedProcess`) 프로세스는 `+0x69 JNZ → +0x149` (서버 번호 2 이면 `0xc0000022` 등). 부모가 보호 프로세스일 일은 없다고 본다 `[추측]`.
- 캡처 버퍼가 있으면 `CsrPortMemoryRemoteDelta` (`+0x81`) 로 버퍼 안 포인터를 서버 주소로 바꿔 보내고 (`+0x8c ~ +0xb9`) 돌아와 되돌린다 (`+0xfa ~ +0x12a`).

### Q67-(a) ntdll 예외 표 — `+0x415` 가 먼저 (결과서 Q67 (a))

- **run18 줄 맞음** `[정적]`: `+0x2d MOV RSI,RDX` (ntdll 시작) → `+0x3f9 ~ +0x405 RtlImageNtHeaderEx(3, RSI, 0, …)` → `+0x40d MOV EBX,[RAX+0x50]` (`SizeOfImage`) → `+0x410 ~ +0x415 RtlInsertInvertedFunctionTable(RSI, EBX)` (ret `+0x41a`). 뒤 `+0x43a ~ +0x470` 칸 0 · 빼기는 run60 그대로.
- **순서** `[정적]` (이 구간은 갈래 없이 위에서 아래로):
  1. `+0x415` — 표에 ntdll 등록.
  2. `0xd88` 바이트 뒤 `+0x119d` (ret `+0x11a2`) `LdrpInitializeNtdllDataTableEntry` 안 `+0x93 OR [RBX+0x68],0x204` — 새로 켜지는 건 비트 9 `InExceptionTable` 하나 (비트 2 `ImageDll` 은 할당 때 이미, run35).
  3. 같은 함수 `+0x11f` (ret `+0x124`) `LdrpProcessMappedModule` 의 `+0x15e BT EDX,9` → `+0x16f JZ → +0x185` 로 **`+0x177 RtlInsertInvertedFunctionTable` 을 건너뜀**.
- **왜 미리 켜나 (코드가 보여 주는 만큼)**: 일반 DLL 은 `+0x15e ~ +0x177` 이 "비트 9 꺼짐일 때만" 등록하고 `+0x17f BTS EDX,9` 로 켠다. ntdll 은 `+0x415` 에서 손으로 등록했으니 **같은 이미지를 두 번 넣지 않으려고** 항목을 만들 때 켠다 `[정적]`. "왜 ntdll 만 일찍 손으로" 는 `[추측]` — 항목이 생기기 전에도 ntdll 안 예외 · 되감기가 표를 써야 해서로 보인다.

### Q67-(b) `+0x1625` 의 항목 = `AvrfpLoaderEntry` 복제 (결과서 Q67 (b))

- **정체** `[정적]`: ntdll `.data` 전역 **`AvrfpLoaderEntry`** (rva `0x1d4520`) 의 복제본. exe 항목도 ntdll 항목도 아니다.
- **`[RSP+0x60]` 을 채운 줄까지** `[정적]`:

```
LdrpInitializeProcess
 │  +0xe8   [RSP+0x60] = 0
 ├─ +0x536  LdrpInitializeExecutionOptions(…, 6번째 = &[RSP+0x60])          (+0x507 · +0x50c)
 │           └─ +0x763  LdrpInitializeApplicationVerifierPackage(…, 그 주소)  (+0x3f · +0x73d · +0x74c)
 │                       └─ +0xf7  AVrfInitializeVerifier(…, R9D = 0, 그 주소)   (+0xc8 · +0xd3 · +0xe7)
 │                                  +0x43 JZ → +0x2ac  RSI = 그 주소 · +0x2bf [RSI] = 0
 │                                  +0x3d9 R15 = &AvrfpLoaderEntry
 │                                  ├─ +0x3f7  AvrfMiniLoadDll(…, [RSP+0x20] = R15)
 │                                  │  +0x3fd  성공이면 [RSI] = &AvrfpLoaderEntry    ← 이 줄
 │                                  │  +0x414  AvrfAppVerifierMode = R14D (1 또는 2, 쓰는 곳 하나)
 │  +0x1568 AvrfAppVerifierMode == 0 → +0x156f JZ → +0x1753  (test.exe)
 ├─ +0x157c LdrpAllocateModuleEntry(0) → RBX
 │           +0x15b0 ~ +0x161e  [RSP+0x60] 에서 Flags · OriginalBase · FullDllName · BaseDllName · TimeDateStamp · CheckSum · DllBase 복사
 ├─ +0x162a LdrpInsertDataTableEntry(RBX)
 │           (+0x166b LdrpInsertModuleToIndex · +0x1679 LdrpProcessMappedModule · +0x168f EntryPoint 복사 · +0x16b0 State = 7)
```

- 즉 **"임시 항목 → 정식 항목으로 갈아끼우기"**. `AvrfMiniLoadDll` 이 loader 준비 전에 verifier 쪽 DLL 을 직접 붙이며 `AvrfpLoaderEntry` 를 채운다고 보인다 — 채우는 줄은 이번에 안 읽음 `[추측: 결과서 설명 + 복사하는 칸 모양]`. 붙이는 DLL 이름 (`verifier.dll`) 도 `[추측]`. `AVrfInitializeVerifier +0x18b` 가 `[AvrfpLoaderEntry+0x30]` 을 `DllBase` 로 읽는 것과 칸 모양은 맞다.
  - run19 의 짐작 "verifier 공급자 모듈?" 은 반만 맞다: 공급자를 올리는 건 4번째 인자 1 길 (`AVrfInitializeVerifier +0x49`, `LdrpInitializeProcess +0x1708` 의 둘째 호출) 이고, 복제되는 건 첫째 호출이 채운 임시 항목이다.
- **test.exe 는 안 탐** `[정적+관찰]`: `+0x1568 CMP [AvrfAppVerifierMode],R15D` (`R15D = 0`, `+0x11a2`) → `+0x156f JZ → +0x1753`. 사진 `AvrfAppVerifierMode` 0 (flow.md 2.4 `LdrpInitializeProcess` run44 "갈림 값" `[관찰]`), PEB `NtGlobalFlag = 0` (`04_baggage-read_P1a.txt:1770` · `_P2.txt:2480`), `IFEOKey (null)` (`_P2.txt:2452`), `AvrfpLoaderEntry` 사진 `끝까지 0*` (짐 지도 7.3).

### Q67-(c) 핫패치 문맥 — `[ctx+0]` = 패치 ntdll 시작 주소, 전역은 늘 쓴다 (결과서 Q67 (c))

- **ctx = `LdrpInitializeInternal` 지역 16B `[RSP+0x30]`, 전역 `LdrpNtdllHotPatchContext` (rva `0x1d2908`) 는 그 주소** `[정적]`: `+0x9f LEA RAX,[RSP+0x30]` · **`+0xa7 MOV [LdrpNtdllHotPatchContext],RAX`** · `+0xae MOVUPS [RSP+0x30],XMM0` (16B 0).
  - **`+0xa7` 은 핫패치 켜짐 · 꺼짐과 상관없다.** `+0x98 CMP [LdrpIsHotPatchingEnabled],0` 의 플래그를 `+0xa4 XORPS` · `+0xa7 MOV` · `+0xae MOVUPS` 가 안 바꿔서 `+0xb3 JZ → +0xc6` 이 그 비교를 쓴다.
  - `_LdrpInitialize` 에서 돌아오면 `+0x5e AND [LdrpNtdllHotPatchContext],0` (첫 스레드라 `+0x5c JNZ` 안 탐). 그래서 이 전역은 **초기화 도중에만** 스택 주소를 가진다.
- **ctx 칸** `[정적]`:

| 칸 | 크기 | 쓰는 줄 | 뜻 | 읽는 줄 |
|---|---|---|---|---|
| `+0x0` | 8B | `LdrpLoadPatchedNtdll +0x19` (0) · **`LdrpLoadCustomNtdllWithSectionHandle +0x49 NpMapViewOfSection` 의 `BaseAddress` 출력** (`+0x18 MOV R8,RDX`) · 실패 때 `LdrpLoadPatchedNtdll +0x157` · `…WithSectionHandle +0x9d` (0) | **패치 ntdll 이미지를 붙인 시작 주소** | `LdrpInitializeProcess +0x1250 ~ +0x1256` (0 이면 패치 항목 안 만듦), `LdrpInitializeNtdllDataTableEntry +0x1b7 ~ +0x1df` (`NtQueryVirtualMemory` 로 파일 이름) |
| `+0x8` | 4B | `LdrpInitializeInternal +0xc2` | `LdrpLoadPatchedNtdll` 반환 status | `+0x123a` (음수 + 켜짐이면 `+0x124e INT 0x29`, 코드 `0x45`) · `+0x1904` (ETW 기록) |
| `+0xc` | 1B | `LdrpLoadPatchedNtdll +0x57` | "패치 있음" 표시 (`LdrpQueryCurrentPatch` 결과) | `+0x18f6` (1 이면 `+0x1916 LdrpLogEtwHotPatchStatus`) |

- **`LdrpLoadPatchedNtdll` 순서** `[정적]` (켜짐일 때만 옴):
  1. `+0x19 [ctx+0] = 0` → `+0x49 LdrpQueryCurrentPatch` → 안 `+0x58 NtManageHotPatch(8, …, 0x30)`: `0xc0000023` → 표시 1, `0` · `0xc0000018` (`LdrpIsImageAddress` 뒤) → 0, 그 밖 → 0 + `0xc00004ac`.
  2. `+0x57` 표시를 `[ctx+0xc]` 에 → **표시 0 이면 `+0x5d` 에서 끝** (`[ctx+0]` 0 그대로).
  3. 표시 1 이면 `+0xc4 NtManageHotPatch(0xa, 0x28B)` (요청 `+0x18` = ntdll 시작 `& ~3`) → 섹션 핸들 → TEB `ArbitraryUserPointer` 저장 · 0 → `+0xe8 LdrpLoadCustomNtdllWithSectionHandle` (보호 `0x80`, `0x40000003` 이면 `LdrpProtectAndRelocateImage`, 헤더 틀리면 `0xc000007b`) → 복원 → `+0x12c NtManageHotPatch(7, 0x20B = {2, 3, -1, 원본 시작, 패치 시작})` ("이 패치를 적용" 알림으로 보임 `[추측: 뜻]`) → 끝맺음 `NpClose` · 실패면 `NpUnmapViewOfSection` 후 `[ctx] = 0`.
  - `Np*` = ntdll 자체 스텁 (`NpMapViewOfSection` = `NtMapViewOfSection` 과 같은 번호 `0x28`, `NpClose` `0xf`, `NpUnmapViewOfSection` `0x2a`).
- **켜짐 조건** `[정적]` (`LdrpIsHotPatchingEnabled` 쓰는 곳 둘, `_refs`):
  - `LdrpInitializeHotPatching +0x55 MOV [flag],BL` — `+0x18 TEST [ProcessParameters+8],0x2000000` 켜짐이면 `+0x53` 0. 아니면 `+0x40 NtManageHotPatch(9, {1}, 8)` 결과가 **`0xc00000bb` · `0xc0000002` 이면 0, 그 밖 (성공 포함) 이면 1** (`+0x45` · `+0x4c`).
  - `LdrpInitializeProcess +0x587` — `LdrpImageExpansionMitigation == 2` 일 때 0 (패치 로딩 **뒤**).
  - 켜져도 표시 1 (ntdll 에 패치가 있음) 이어야 `[ctx+0]` 이 채워진다.
  - (참고, 결과서 `objdump` — 확인 필요) 커널 `nt!NtManageHotPatch` (rva `0xa47e60`) 는 전역 dword (rva `0xe375f4`) bit 0 이 꺼져 있으면 모든 클래스에 `0xc00000bb`. 그 bit 는 부팅 때 `MiInitializeHotPatches +0x82` 가 켠다.

### 사진 로그 대조 (run44, 새로 돌린 것 아님 — flow-writer)

| 정적 | 사진 값 | 맞나 |
|---|---|---|
| 스텁은 `SystemCall` bit 0 꺼짐 → `SYSCALL` | KUSD `+0x308 SystemCall : 0` — 커널 별칭 8장 (`_supp_<K1 ~ P2>.txt:246`) · 유저 주소 P1a · P1b · P2 (`_P1a.txt:2954` · `_P1b.txt:4072` · `_P2.txt:4084`) | **맞음** — `SYSCALL` 길 |
| `LdrpNtdllHotPatchContext` = `+0xa7` 이 쓴 지역 칸 주소 (켜짐과 상관없이), `+0x5e` 가 비움 | `ntdll+0x1d2908` (= `7ffa`fb832908`, ntdll `7ffa`fb660000` — `_P1a.txt:59`) P1a · P1b **`0000009b`2fcff9d0`** → P2 **0** (`_P1a.txt:5130` · `_P1b.txt:6248` · `_P2.txt:6260`). 메인 스레드 TEB `StackBase 9b2fd00000` · `StackLimit 9b2fcfe000` (`_P1a.txt:3426`) 안 | **맞음** — 스택 주소 · 은퇴 전 비움 |
| 핫패치 꺼짐이어도 `+0xa7` 은 쓴다 | 같은 사진 `LdrpIsHotPatchingEnabled` (`ntdll+0x1d26f0`) P1a 0 (`_P1a.txt:5097`) | **맞음** — 꺼짐인데 전역은 스택 주소 |
| P1a (`LdrpInitializeProcess +0x199d`, run44) 는 `+0x587` 뒤 · `+0x1233` 뒤 | `LdrpImageExpansionMitigation` (`ntdll+0x1d2978`) 0 (`_P1a.txt:5137`, flow.md 2.4 `LdrpInitializeProcess` run44 "갈림 값" `[관찰]`) → `+0x587` 안 탐 | → **`LdrpIsHotPatchingEnabled` 0 은 `LdrpInitializeHotPatching +0x55` 가 쓴 값** → `+0xb3` 뜀 → **`LdrpLoadPatchedNtdll` 안 불림** `[정적+관찰]` |
| ctx 16B 는 0 (`+0xae`) | `9b2fcff9d0` 자리는 사진에 안 찍힘 (스택 덤프 `dq 9b2fcffa40 …` 뒤쪽만, `_P1a.txt:3230`) | 못 봄 — 동적 2 |
| 패치 없음 | PEB `CrossProcessFlags.ProcessImagesHotPatched = 0` (`_P2.txt:2461`), EPROCESS `MmHotPatchContext (null)` (`:298`), 모듈 6개 `HotPatchState 0` (`:3455` …) (결과서) | 맞음 |
| `AvrfAppVerifierMode` 0 → `+0x156f` 뜀 | 0 (`끝까지 0*`), `NtGlobalFlag 0` (`_P1a.txt:1770`) | 맞음 |

### 호출 관계

```
(부모 cmd.exe 스레드)
KERNELBASE!CreateProcessInternalW
 └─ +0x4672  ntdll!CsrClientCallServer(msg = [RSP+0xbe0], 0x1001d, 0x218)
              +0x14  LdrpIsSecureProcess ≠ 0 → 0xc00000bb
              +0x6f  CsrClientProcess == 0 → +0x187 (CFG) CsrServerApiRoutine   (csrss 자신 · 네이티브)
              └─ +0xf2  NtAlpcSendWaitReceivePort(CsrPortHandle, 0x20000, msg, 0, msg, &0x3b8, 0, 0)   답 올 때까지
                    ┄┄▶ csrsrv!CsrApiRequestThread … 처리기 (+0x4e6 / +0x656) → +0x656 [msg+0x34] = status
              +0x135 EAX = [msg+0x34]   (ALPC 실패면 +0x131 이 먼저 그 값을 넣음)
 +0x4677  [RSP+0xc14] (= msg+0x34) 음수 → BaseSetLastNTError → 실패 끝 (+0xb61)

(test.exe 첫 스레드)
LdrpInitializeInternal
 │  +0xa7  LdrpNtdllHotPatchContext = &ctx (지역 16B, 0)      ← 켜짐 · 꺼짐 상관없이
 ├─ (1) +0x98  LdrpInitializeHotPatching   +0x45 NtManageHotPatch(9) → +0x55 LdrpIsHotPatchingEnabled
 │  +0xb3  꺼짐 → 건너뜀                                       ← test.exe (사진 P1a 0)
 ├─ (켜짐) +0xc2  LdrpLoadPatchedNtdll(ntdll 시작, &ctx)
 │           ├─ +0x4e  LdrpQueryCurrentPatch → ctx+0xc (패치 있음)     없으면 끝
 │           ├─ +0xc9  NtManageHotPatch(0xa) → 섹션
 │           ├─ +0xed  LdrpLoadCustomNtdllWithSectionHandle
 │           │           └─ +0x4e  NpMapViewOfSection → ctx+0 = 패치 시작
 │           └─ +0x131 NtManageHotPatch(7)
 │  +0xc2  ctx+0x8 = status
 ├─ (3) +0x5a  _LdrpInitialize
 │              └─ +0x16c  LdrpInitializeProcess
 │                          ├─ +0x41a   RtlInsertInvertedFunctionTable(ntdll, SizeOfImage)   ← 먼저
 │                          ├─ +0x536   LdrpInitializeExecutionOptions → … → AVrfInitializeVerifier → AvrfMiniLoadDll
 │                          │            ([RSP+0x60] = &AvrfpLoaderEntry, App Verifier 만)
 │                          ├─ +0x11a2  LdrpInitializeNtdllDataTableEntry   +0x93 InExceptionTable 켬 → +0x177 등록 건너뜀
 │                          │  +0x1233 ~ +0x124e  ctx+8 음수 · 켜짐 → INT 0x29
 │                          ├─ +0x126f  LdrpInitializeNtdllDataTableEntry (ctx+0 ≠ 0 일 때만, 패치 항목)
 │                          │  +0x1568  AvrfAppVerifierMode == 0 → +0x1753     ← test.exe
 │                          ├─ +0x162a  LdrpInsertDataTableEntry (AvrfpLoaderEntry 복제 항목)
 │                          └─ +0x191b  LdrpLogEtwHotPatchStatus (ctx+0xc ≠ 0 일 때만)
 └─ +0x5e  LdrpNtdllHotPatchContext = 0
```

## 확인된 것

- `[정적]` **`0x7ffe0308` = KUSD `+0x308 SystemCall` (4B).** ntdll 스텁 494곳이 bit 0 만 본다 (0 → `SYSCALL`, 1 → `INT 0x2e`). `[관찰]` run44 8장 모두 0.
- `[정적]` **`CsrClientCallServer` 는 답을 기다린다** (`+0xed NtAlpcSendWaitReceivePort`, Flags `0x20000`, 받는 칸 = 보낸 칸, 시간 제한 없음). 답 status = 메시지 `+0x34` (csrss `CsrApiRequestThread +0x656` 이 씀, ALPC 실패면 `+0x131` 이 넣음). 부모 `CreateProcessInternalW +0x4677` 이 그 칸을 다시 읽어 음수면 실패로 끝낸다.
- `[정적]` `+0x415 RtlInsertInvertedFunctionTable(ntdll 시작, SizeOfImage)` 맞음, **`InExceptionTable` 켜기 (`+0x119d` 안 `+0x93`) 보다 먼저**. 미리 켜는 까닭 = `LdrpProcessMappedModule +0x177` 둘째 등록 막기.
- `[정적]` **`+0x1625` 항목 = `AvrfpLoaderEntry` 복제** (`AVrfInitializeVerifier +0x3fd` 가 `[RSP+0x60]` 에 그 주소를 씀, `+0x15b0 ~ +0x161e` 가 칸 복사). App Verifier 만, test.exe 는 `+0x156f` 에서 건너뜀.
- `[정적]` **`[ctx+0]` = 패치 ntdll 시작** (`LdrpLoadCustomNtdllWithSectionHandle +0x49 NpMapViewOfSection` 출력), `[ctx+8]` = status, `[ctx+0xc]` = 패치 있음. 전역 `LdrpNtdllHotPatchContext` 는 **켜짐과 상관없이** `+0xa7` 이 쓰고 `+0x5e` 가 비운다.
  - `[관찰]` run44 P1a · P1b `0x9b2fcff9d0` (메인 스레드 스택 안) → P2 0, 같은 사진 `LdrpIsHotPatchingEnabled` 0.
- `[정적+관찰]` **test.exe 에서 `LdrpLoadPatchedNtdll` 은 안 불렸다** — P1a 에 `LdrpIsHotPatchingEnabled` 0, P1a 앞에서 0 을 쓸 수 있는 둘 중 `+0x587` 은 안 탐 (`LdrpImageExpansionMitigation` 0) → `+0x55` 가 0 을 썼다 → `+0xb3` 뜀. 그러니 `[ctx+0]` 0 → `+0x126a` 패치 항목도 안 만듦.

## 결과서와 다르게 적은 곳 · 보탠 것

- **다르게 적은 곳 하나**: 결과서 Q67-c "사진 쪽 흔적" 의 **"`LdrpIsHotPatchingEnabled` 자체 값은 사진에 없다 (켜졌는데 패치가 없을 수도 있음)"** → 사진에 있다. run44 P1a `ntdll+0x1d26f0` = 0 (`04_baggage-read_P1a.txt:5097`, flow.md 4장 2.4 "로더 입구 실행 값" 줄 · 짐 지도 3.8 `끝까지 0*` 가 이미 적어 둠). 위 "확인된 것" 마지막 줄처럼 **켜짐 · 꺼짐은 꺼짐으로 정해진다** `[정적+관찰]`. 결과서 "test.exe 에서 타나: 정적으로 못 정한다" 는 사진까지 합치면 "안 탔다" 로 좁혀진다 (`NtManageHotPatch(9)` 의 실제 status 만 남음 — 동적 1).
- **보탠 것** (flow-writer `.asm` · 사진 로그):
  - `LdrpNtdllHotPatchContext` 사진 값 `0x9b2fcff9d0` 과 그게 메인 스레드 스택 안이라는 것 (TEB `StackBase` · `StackLimit`) — 결과서 동적 2 의 "비어 있지 않은 스택 주소여야" 는 사진으로 이미 답함. 남은 건 그 16B 내용뿐.
  - **"csrss 등록이 부모 `NtResumeThread` 보다 먼저 끝난다"** (flow.md 2.2 `[추측]`) 를 고리마다 나눔: ① 부모가 답을 기다린다 `[정적]` (이번) ② `+0x4672` 를 안 지나고는 `NtResumeThread` (`+0x5bd3`) 에 못 간다 — `CREATE_SECURE_PROCESS` 길 빼고 `[정적]` (run14 흐름 그래프) ③ `BaseSrvCreateProcess2 → CsrCreateProcess → CsrInsertProcess` 는 처리기 안 동기 호출 `[정적]` (run34) ④ csrss 가 처리기가 돌아온 **뒤** 답을 보낸다 — 처리기 뒤 `+0x656` 이 status 를 쓰고, 답 보내기 후보는 같은 함수 `+0x183 NtAlpcSendWaitReceivePort(CsrApiPort, 0x10000, R8 = R14, …, 받기 [RSP+0xb0])` · `+0x6ed` (`R8 = [RSP+0xb0]`) — `R14` 가 처리한 메시지인지는 안 따라감 `[추측: 줄 순서]`. 결과서가 "다음 요청 후보" 로 적은 "`+0x4672` 와 `NtResumeThread` 앞뒤" 는 ② 로 이미 답이 있다.
  - csrss 처리기 간접 호출은 결과서의 `+0x4e1` · `+0x651` 둘 중 **`+0x4e1` 길은 `+0x4e6 JMP → +0x4fb` 로 `+0x656` 을 안 지난다** (`.asm`). 그 길에서 status 를 누가 `+0x34` 에 쓰는지는 안 봄 `[공백]` — 결과서 "두 쪽이 `+0x34` 로 맞는다" 는 `+0x651` 길 기준.

## 정정

- 확정 사실을 고친 곳은 없다 → 이전 run 문서 맨 위 정정 줄은 안 넣었다. `[추측]` 으로 두었던 것들이 `[정적]` · `[정적+관찰]` 로 바뀌었을 뿐이다:
  - flow.md 2.2 "부모는 `CsrClientCallServer` 에서 답을 받을 때까지 기다린다 `[추측: 일반 지식]`" → `[정적]`.
  - flow.md 2.2 · 2.4 · 4.1 "`SystemCall` 칸으로 보임 `[추측]`" · "보통은 SYSCALL 쪽일 것 `[추측]`" → `[정적]` · `[관찰]`.
  - flow.md 2.4 "둘째 `+0x126a` … 핫패치가 꺼진 보통 프로세스는 안 탄다 `[추측]`" → `[정적]` (꺼짐이면 ctx 16B 가 0 그대로 → `+0x1256` 뜀), test.exe 는 `[정적+관찰]`.
  - flow.md 2.4 "AppVerifier 복제 항목 — 무엇을 복제하는지 (verifier 공급자 모듈?) `[추측]`" (run19) → `AvrfpLoaderEntry` 복제 `[정적]` (공급자 쪽은 아님 — 위 (b)).

## 이전 run 에서 닫은 것

- run34 flow-writer "새 공백" **Q66** (a) 칸 이름 · (b) 기다리나 — 닫힘. `INT 0x2e` 갈래 커널 입구 (`KiSystemService`) 는 그대로 남음 (run56 정적 남은 것).
- run35 · sast/30 "새로 생긴 공백" **Q67** (a) · (b) · (c) — 닫힘. (a) 는 run60 이 이미 "후보 둘 다" 로 닫았고 이번엔 순서만 더함.
- flow.md 4장 2.4 "로더 입구 실행 값" 줄의 "`LdrpIsHotPatchingEnabled` → `LdrpLoadPatchedNtdll` 이 불리나" — 안 불림 `[정적+관찰]`.
- 짐 지도 3.8 `AvrfAppVerifierMode` 줄 "(부르는 자리 확인은 Q78)" — 부르는 사슬 `LdrpInitializeProcess +0x531 → LdrpInitializeExecutionOptions +0x75e → LdrpInitializeApplicationVerifierPackage +0xf2 → AVrfInitializeVerifier +0x414`.

## 아직 확인 필요

### 동적 (`[VM]`, 결과서 "동적으로 확인할 것" — 사진으로 줄어든 만큼 고침)

1. **`NtManageHotPatch(9)` 실제 status** (Q43 (3) 과 같은 질문, 켜짐 · 꺼짐 자체는 사진으로 "꺼짐"): `ntdll+0x8bc19` (`LdrpInitializeHotPatching +0x45`) `eax` — `0xc00000bb` 인가 `0xc0000002` 인가, 아니면 `+0x18` 매개변수 비트 `0x2000000` 갈래였나 (사진 K1 `Flags 0x4001` 이라 비트 25 꺼짐 — 그럼 `NtManageHotPatch` 를 탔을 것 `[추측]`). 확인용.
2. **ctx 16B 내용** (결과서 동적 2 의 남은 반): `ntdll+0x8d637` (`LdrpInitializeProcess +0x1233`) 에서 `dq poi(ntdll+0x1d2908) L2` — 둘 다 0 이어야. 전역 값 자체는 사진이 이미 답함 (`0x9b2fcff9d0`).
3. **`CsrClientCallServer` 가 정말 기다리나 · 돌아온 `+0x34`** (부모 cmd.exe 쪽, 유저 멈춤점은 cmd.exe 프로세스 조건): `ntdll+0xdbebd` (`+0xed`, `r8d` 아님 — 이 자리 `r8` = 메시지, `[rbx+0x30] == 0x1001d` 일 때만) 와 `ntdll+0xdbec2` (`+0xf2`, `eax` = ALPC status, `dd @rbx+0x34 L1`). 두 지점 사이에 csrss 쪽 `basesrv!BaseSrvCreateProcess2` 가 서면 "등록 → 답" 순서까지 본다.
4. (선택, 읽기만) `dd nt+0xe375f4 L1` bit 0 (커널 핫패치 켜짐), `db ntdll+0x1d4df8 L1` (`CsrClientProcess` — 부모 cmd.exe 에서 1), `dd ntdll+0x1e7044 L1` (`AvrfAppVerifierMode` 0).

### 정적 (결과서 "다음 요청 후보" + 보탬)

1. (작게, sast/112 끝난 뒤) `nt!NtManageHotPatch` (rva `0xa47e60`) 클래스 8 · 9 갈래 반환값, `MiInitializeHotPatches` 가 켜는 `0xe375f4` bit 0 의 입력 둘 (`0xfc6ac8` bit 14 · `0xfc5260`).
2. (작게, 낮음) KUSD `+0x308` 을 쓰는 커널 코드 — 부팅 쪽 (`KiInitializeKernel` · `KiInitializeBootStructures` 류) 을 읽어 "없음" 을 굳힐지. 사진 8장 모두 0 이라 급하지 않음.
3. (작게) `AvrfMiniLoadDll` (rva `0xe094c`, `.asm` 은 이미 있음) 이 `AvrfpLoaderEntry` 를 채우는 줄 — App Verifier 만, test.exe 와 무관.
4. (보탬, 작게) csrsrv `CsrApiRequestThread` 답 보내는 줄 — `+0x183` 의 `R14` 가 처리한 메시지인가 (위 "보탠 것" ④), 그리고 `+0x4e1` 길에서 `+0x34` 를 쓰는 쪽. 이게 닫히면 flow.md 2.2 "csrss 등록이 `NtResumeThread` 보다 먼저" 가 `[정적]` 이 된다.

### PE_emulator 쪽 (결과서 "지금 우리 맞나" 요약 — `../PE_emulator` 코드는 flow-writer 가 안 읽음)

- Q66-① `SystemCall` 0 (`Src/nt/AddressSpace.cpp:505`, `uc_mem_map` 0 채움) — **맞음**.
- Q66-② 부모 · csrss 는 에뮬레이터에 없음 (`Src/main.cpp:268~269`, 의도) — **해당 없음**. 읽는 쪽 `CsrClientProcess` 값 (`BaseDllInit.cpp:1139~1140`) 은 이번 결과와 같음.
- Q67-(a) `RtlInsertInvertedFunctionTable` 을 `+0x415` 근거로 부름 (`Src/ntdll/LdrInit.cpp:1419`) — **맞음**.
- Q67-(b) `AvrfAppVerifierMode` 안 읽음 (`LdrInit.cpp:5845~5846`) — **맞음** (길 안 탐).
- Q67-(c) **작게 틀림 (동작은 같음)**: `Src/ntdll/LdrInit.cpp:7084~7092` 주석 "`LdrpNtdllHotPatchContext` 안 씀 … `+0x5a ~ +0x5e` 비우기도 안 함" — 진짜는 **켜짐과 상관없이 `LdrpInitializeInternal +0xa7` 이 스택 주소를 쓰고 `+0x5e` 가 비운다** (사진 P1a · P1b `0x9b2fcff9d0`). 읽는 쪽 셋은 16B 가 0 이라 건너뛰는 것과 결과가 같다. 다른 건 초기화 도중 전역 (rva `0x1d2908`) 값 (스택 주소 vs 0) 뿐 — 그 사이 메모리를 찍어 비교할 때만 보인다. 고칠지는 사용자 결정.

## flow.md · 짐 지도에 반영한 것

- flow.md: 기준 (ntdll 줄에 run105, 근거 로그 (run105) 한 줄), 그림 1 (`nd2` 글 · `nd1 ==> a0` 라벨, 제목 run105), 그림 2 (`p5` 글, 제목 run105), 2.2 (절 머리 · `CreateProcessInternalW` ② csrss 알림 · 스텁 절 `SystemCall` · csrss 쪽 절 "기다리나" 줄), 2.4 (절 머리 · `LdrpInitializeInternal` 핫패치 줄 · `LdrpInitializeProcess` 둘째 `+0x126a` 줄 · `+0x587` 줄 · AppVerifier 복제 항목 줄 · `LdrpInitializeNtdllDataTableEntry` `InExceptionTable` 줄 (예외 표 순서) · Q67 꼬리 줄 · `LdrpInsertDataTableEntry` caller 줄), 4장 (2.2 Q66 줄 · 2.4 Q67 줄 · 2.4 로더 입구 실행 값 줄 · 2.4 run19 이름 줄 해결됨 · run105 새 줄 셋 — 동적 · 정적 · PE_emulator), 4.1 (그림 1 ntdll ==> nt 줄 · 그림 1 노란 메모 줄 · 그림 2 (0) 프로세스 준비 줄), 5장 (run105 · sast/99).
- baggage.md: 기준 (run105), 판 (쉰다섯째판), 1장 그림 `.data` 숫자, 2장 표 `.data` · 전역 합 · 전역 셈 문장 · 2.1 전역 모름 문장, 3장 머리 메모, 3.7 KUSD 머리 (`SystemCall` 읽는 곳), 3.8 (머리 숫자 · 머리 메모 · `AvrfpLoaderEntry` 새 줄 · `LdrpIsHotPatchingEnabled` 첫 줄 조건 · `LdrpNtdllHotPatchContext` 조건 · 사진 값), 3.9 (`AvrfAppVerifierMode` 경로), 5장 ② 한 마디, 7.1 (아는 전역 끝까지 0 59 → 60), 7.2 KUSD `SystemCall` 실마리, 7.3 (`AvrfpLoaderEntry` 빠짐, 숫자), 7.4 (`AvrfpLoaderEntry` 짐작), 새 7.37, 8장.
  - 2장 숫자: ntdll 전역 아는 것 188 → **189** (`.data` 163 → 164, 정확 160 · 짐작 3 → 4), 모름 305 → **304** (`.data` 275 → 274, `끝까지 0` 237 → 236). `.data` ntdll 이 씀 161 → 162, 3구역 합 183 → 184. 구조체 숫자는 그대로 (KUSD `SystemCall` 은 쓰는 쪽을 몰라 모름에 남음).

## 디버거 메모 / 도구 메모

- **디버거가 붙어서 달라지는 점**: 이번에 읽은 줄 중 디버거 칸을 읽는 것은 없다 `[정적: 읽은 줄 안에서]`. `SystemCall` 이 디버거 때문에 바뀐다는 증거도 없다 (사진은 디버거 붙은 상태 — 값 0).
- 도구: KUSD 칸 "누가 쓰나" 는 Ghidra `refs` 가 레지스터 기준 주소를 못 잡아 결과서가 `objdump` 꼴 훑기로 했고 한계를 적었다 (run70 · run104 와 같은 방식). pitfalls 제안 없음.
- 결과서 실수 (depth 를 한 정규식에 묶음) 는 결과에 영향 없음. decompiler 쪽 메모로만 남긴다.

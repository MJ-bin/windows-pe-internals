# 2026-11-05 run65 — `LdrpInitializeProcess` 줄기 위 초기화 본문 (Q120b, PE_emulator 요청 11 의 7번): `\KnownDlls` 열기 · DLL 정책 · 현재 디렉터리 · CPU 기능 비트맵 · 스케줄러 공유 데이터 · TLS 준비, 그리고 모듈 올리기 길 부품·공용 부품. **test.exe 기본 길에서 이 묶음의 syscall 은 8번**이고, `RtlFreeUnicodeString` (`+0x1563`) 은 **현재 디렉터리가 빈 갈래에서만** 불린다

> **닫힘 · 작은 정정 (run130, sast/120 — Q124)**: 아래 "`LdrpInitializeExecutionOptions +0x733` (`LdrpQueryIllegalCWDDevices`) 이 test.exe 에서 실제로 닿는지는 안 봤다" → **닿는다** — IFEO 키 없음 표시 (`+0xad JNZ → +0x604`) 도 `+0x71c` 에서 합쳐져 `RCX = 0` 으로 `+0x733`. "마스크는 어느 쪽이든 0" 은 test.exe 에선 맞지만 이유는 **KUSD `CurDirDevicesSkippedForDlls = 0`** (이 VM 정책) 이다 — 코드가 0 을 보장하지 않음. TLS 블록 `0x11`B `[추측]` → `[정적]` (`LdrpAllocateTls +0xb7 · +0xec · +0xf0` = `End − Start` + 정렬 `0x10`, `SizeOfZeroFill` 안 읽음). 스케줄러 공유 데이터 섹션 크기 = 커널 상수 `0x2000` (`PspSchedulerSharedDataRegionCreate +0x127`). `LdrpPolicyBits` 자리는 run114 가 이미 옮김. 자세히는 `run130-run65-66-leftovers.md`.


- run 번호: 요청서 머리에는 "예정 run64" 라고 적혀 있지만, 같은 날 Q120c (sast/60) 가 먼저 끝나 run64 가 됐다. 이 문서는 `next_index.py next run` 값 **run65** 다.

## PE_emulator 쪽에서 쓸 것

### 갈림길 — 진짜 VM 과 다른 길을 탄 곳 (먼저 볼 것)

- **`LdrpInitializeProcess +0x1563` (ret `+0x1568`) 의 문자열 해제는 "현재 디렉터리가 비어 있을 때" 만 불린다** `[정적]`.
  - `+0xf44 ~ +0xf53`: `ProcessParameters.CurrentDirectory.DosPath` 사본의 `Buffer` 가 NULL 이거나, `Length` 가 0 이거나, 첫 글자가 NUL 이면 빈 것으로 본다.
  - 빈 갈래 (`+0xf55 ~ +0xfb2`): `RtlAllocateHeap(힙, 0, 8)` (call `+0xf5e`) 로 8바이트를 받아 시스템 루트 앞 3글자 `C:\` 를 채우고 (`RtlGetNtSystemRoot` call `+0xf89`, `Length 6 / Max 8` = `+0xfa7`), **표시 칸 `[RSP+0x5c]` 를 0 으로** 바꾼다 (`+0xf84`. 처음 값 1 은 `+0x32f`).
  - `+0x1554 CMP [RSP+0x5c],R15D` · `+0x1559 JNZ` → 표시가 0 일 때만 `+0x1563` 해제. 해제 대상은 위 8바이트 버퍼다.
  - **진짜 VM 은 이 갈래를 안 탄다**: 사진 P1a `CurrentDirectory.DosPath` = `Length 0x84`, `MaximumLength 0x208`, `Buffer 0x25996f381c0`, `Handle 0x58` `[관찰]` (`ms/logs/04_baggage-read_P1a.txt:2552 ~ 2553`).
  - 그래서 **PE_emulator 가 이 해제를 봤다면, 입력 `ProcessParameters` 의 현재 디렉터리가 비어 있었다**는 뜻이다. 진짜 VM 처럼 가려면 `CurrentDirectory.DosPath` 에 exe 폴더 (`C:\…\sample\bin\`, 끝 `\` 포함) 를 넣어 주면 된다.
- 해제 함수는 같은 주소 이름 3개 (rva `0x4e200`, 후보: `RtlFreeUnicodeString`·`RtlFreeAnsiString`·`RtlFreeUTF8String`). 하는 일:
  - `Buffer != NULL` 일 때만 `RtlFreeHeap(PEB.ProcessHeap, 0, Buffer)` 를 부르고 (`+0x12/+0x17`), 문자열 머리 16바이트를 모두 0 으로 (`+0x17 XORPS` · `+0x1a MOVUPS`).
  - PE_emulator 실측 "덫 뒤 16바이트가 0" 은 **코드와 맞다**.
  - "버퍼 첫 8바이트가 바뀜" 은 이 함수가 아니라 **`RtlFreeHeap` 안 (힙 관리자)** 의 일이다. 이 함수는 버퍼를 건드리지 않는다. 힙 해제 본문은 run46 범위라 정적으로 안 정했다.

### test.exe 기본 길 syscall 8번 (이 묶음)

| # | syscall (번호) | 클래스·인자 | 부르는 자리 (call / ret) | 결과를 쓰는 곳 |
|---|---|---|---|---|
| 1 | `NtOpenDirectoryObject` (`0x58`) | 접근 `3`, 이름 `\KnownDlls`, 속성 `0x40` | `LdrpInitializeProcess +0xdb8 / +0xdbd` | `.mrdata` `LdrpKnownDllDirectoryHandle` (rva `0x1e7120`) |
| 2 | `NtOpenSymbolicLinkObject` (`0x138`) | 접근 `1`, Root = 위 핸들, 이름 `KnownDllPath` | `+0xe47 / +0xe4c` | 지역 링크 핸들 |
| 3 | `NtQuerySymbolicLinkObject` (`0x16b`) | 링크 핸들, `&LdrpKnownDllPath` (처음 버퍼 `0x30`B) | `+0xe90 / +0xe95` | `.data` `LdrpKnownDllPath` (`0xc0000023` 이면 다시 할당해 반복) |
| 4 | `NtClose` (`0xf`) | 링크 핸들 | `+0xf04 / +0xf09` | — |
| 5 | `NtOpenFile` (`0x33`) | 접근 `0x100020`, 공유 `3`, 옵션 `0x21`, 속성 `0x40`, 이름 = 현재 디렉터리 NT 경로 | `RtlpCreateNewDirectoryReference +0xbb / +0xc0` (← `RtlpInitCurrentDir +0x32 / +0x37` ← `LdrpInitializeProcess +0x1517 / +0x151c`) | 레코드 `+0x8` → 매개변수 `CurrentDirectory.Handle` (사진 `0x58`) |
| 6 | `NtQueryVolumeInformationFile` (`0x49`) | 클래스 `4` (`FileFsDeviceInformation`), 길이 8 | `RtlpCreateNewDirectoryReference +0xf7 / +0xfc` | 레코드 `+0x28` (장치 Characteristics) |
| 7 | `NtQuerySystemInformation` (`0x36`) | 클래스 **`0xfa`**, 버퍼 `&RtlProcessorFeaturesBuffer`, 길이 `0x10` | `RtlInitializeProcessorFeaturesBitMap +0x25 / +0x2a` (← `LdrpInitializeProcess +0x1761 / +0x1766`) | `.data` `RtlProcessorFeaturesBuffer` (커널이 직접 씀) |
| 8 | `NtSetInformationProcess` (`0x1c`) | 핸들 `-1`, 클래스 **`0x70`**, 버퍼 `&지역` (출력), 길이 `8` | `LdrpAllocateSchedulerSharedData +0x2c / +0x31` (← `LdrpInitializeProcess +0x1827 / +0x182c`) | 지역 → 노드 `+0x10` (섹션 핸들) |

- 바로 뒤 `LdrpAcquireSchedulerSharedDataSlot` 의 `NtSetInformationThread(-2, 0x39, 버퍼, 0x18)` (`+0x84 / +0x89`) 는 이 묶음 밖 (run41) 이지만 같은 사슬이다 — 아래 "TEB `+0x1850`".
- 안 타는 syscall (test.exe 길): `LdrpCheckAppDirType` 의 `NtOpenFile`·`NtQueryVolumeInformationFile`·`NtClose` 한 벌, `RtlpCreateNewDirectoryReference` 실패 길 `NtClose`, `LdrpAllocateSchedulerSharedData` 실패 길 `NtClose`, `LdrpIsImageAddress` 의 `NpQueryVirtualMemory` (`0x23`, `NtQueryVirtualMemory` 와 같은 번호의 둘째 스텁).
- `LdrpFindKnownDll` 의 `NtOpenSection` (`0x37`, 접근 `0xd`) 과 `LdrpLoadKnownDll` 끝 `NtClose` 는 "KnownDll 로 올리는 모듈 수" 만큼 — 횟수는 동적.

### 함수마다 (인자 · 쓰는 칸 · 하위 호출 · syscall · 반환)

| 함수 | 인자 | 쓰는 칸 | 하위 호출 | syscall | 반환 |
|---|---|---|---|---|---|
| `LdrpInitializePolicy` (rva `0xbd38c`) | 없음 | `LdrpAppPackagesPath`·`LdrpOriginalAppPackagesPath` 를 0 으로 (test.exe 는 이미 0) | test.exe 없음 (조건부 `LdrSetDllDirectory`, 패키지 앱 `RtlQueryPackageClaims` ×5) | 없음 | 없음 |
| `LdrpIncludeAlternateForwarders` (`0x1217c4`) | 없음 | 없음 | 없음 | 없음 | `AL` — test.exe 는 **안 불림** |
| `LdrpCheckAppDirType` (`0x1191a0`) | `RCX` = exe 경로 문자열 | `LdrpIllegalCWDDevices` (조건부 0) | `RtlpDosPathNameToRelativeNtPathName` · 문자열 해제 | `NtOpenFile` · `NtQueryVolumeInformationFile(4)` · `NtClose` | 없음 — test.exe 는 **안 불림** |
| `RtlpInitCurrentDir` (`0x70e8`) | `RCX` = 현재 디렉터리 문자열 (사본) | `RtlpCurDirRef` · 매개변수 `CurrentDirectory.Handle`·`DosPath.Buffer`·`DosPath.Length` | `RtlDetermineDosPathNameType_Ustr` · `RtlpCreateNewDirectoryReference` | 아래 함수가 냄 | `0` / `0xc000000d` / 아래 함수 상태 |
| `RtlpCreateNewDirectoryReference` (`0x6ed0`) | `RCX` 경로, `EDX` 최대 길이, `R8` 레코드 out | 힙 레코드 `0x30 + 최대 길이` | NT 경로 만들기 · `RtlFreeHeap` · `RtlAllocateHeap` · 복사 | `NtOpenFile` · `NtQueryVolumeInformationFile(4)` · (실패) `NtClose` | `0` / `0xc0000017` / `0xc0000106` / 하위 상태 |
| `RtlInitializeProcessorFeaturesBitMap` (`0x11f910`) | 없음 | `RtlProcessorFeaturesBuffer` (syscall 이 채움) · `RtlProcessorFeaturesBitMap = {0x80, &버퍼}` | 없음 | `NtQuerySystemInformation(0xfa, 버퍼, 0x10, &ret)` | syscall 상태 (부른 쪽이 안 봄) |
| `LdrpAllocateSchedulerSharedData` (`0xf96b0`) | 없음 | `LdrpSchedulerSharedDataListHead` 에 노드 `0x18`B 끼움 | `RtlAllocateHeap(LdrpHeap, …, 0x18)` · SRW 락 잡기·풀기 | `NtSetInformationProcess(-1, 0x70, &핸들, 8)` · (실패) `NtClose` | `0` / `0xc00000bb` / `0xc000009a` / syscall 상태 (부른 쪽이 안 봄) |
| `LdrpInitializeTls` (`0xabca8`) | 없음 | `LdrpTlsBitmap` · `LdrpStaticTlsBitmapVector` · `LdrpActualBitmapSize` · exe 항목 `TlsIndex` (`0xffff`) · (아래 호출이) `LdrpTlsList` · TEB `+0x58` · `LdrpActiveThreadCount` | `LdrpInitializeTlsHeap` · `RtlImageNtHeaderEx` · `RtlImageDirectoryEntryToData` · `LdrpLogInternal` · `LdrpAllocateTlsEntry` · `RtlSetBits` · `RtlClearBits` · `LdrpAllocateTls` · (`N + 8 > 32` 일 때) `RtlAllocateHeap` | 없음 | `0` / `0xc0000017` / 하위 상태 |
| `LdrpInitializeTlsHeap` (`0xabe6c`) | 없음 | `LdrpTlsHeap` | (분기) `RtlpCreateHeap` · `RtlSetHeapInformation` | 없음 | `0` / `0xc0000017` |
| `LdrpGetNewTlsVector` (`0xa3e0c`) | `ECX` 칸 수 | 힙 블록 (`+0` 칸 수, `+0x10 ~` 0) | `RtlAllocateHeap(LdrpTlsHeap)` · `memset` | 없음 | `RAX` = 블록 `+0x10` / 0 |
| `RtlSetBits` / `RtlClearBits` (`0xa8460` / `0xac290`) | `RCX` 비트맵 머리, `EDX` 시작, `R8D` 개수 | 비트맵 `Buffer` 안 비트 | `memset` (긴 구간) | 없음 | 없음 |

- 나머지 (`LdrpValidateEntrySection`·`LdrpIsImageAddress`·`RtlRbInsertNodeEx`·`LdrpFindKnownDll`·`LdrpLoadKnownDll`·`\KnownDlls` 스텁 셋·공용 부품 넷) 의 같은 표는 결과서 `_workspace/sast/59_ldr-init-bodies_result.md` "PE_emulator 용 작은 표".

### 꼭 챙길 값

- **클래스 `0xfa` 출력** (16바이트 = 128비트 비트맵, 비트 `k` = 프로세서 기능 번호 `0x40 + k`): 커널 `KeProcessorFeaturesBuffer` (nt rva `0xfc7498`) 16바이트를 그대로 복사. 이 버퍼에 쓰는 커널 코드는 `KiSetFeatureBits +0x888` (0 으로 채움) 하나뿐이라 **정적으로는 16바이트 모두 0** `[정적, 반증 가능]`. 길이 `< 0x10` 이거나 8의 배수가 아니면 `0xc0000004`, 돌려주는 길이 `0x10`.
  - ntdll 쪽 사진: `RtlProcessorFeaturesBuffer` 는 사진 분류 `끝까지 0*` (P1a ~ P2 모두 0, 짐 지도 7.3) `[관찰]` — 정적 0 과 맞는다. 단 syscall 이 실패해도 0 이라 이것만으로 커널 값을 못 가린다 (아래 "아직 확인 필요" 1).
  - 이 비트맵을 읽는 ntdll 코드 (`RtlIsProcessorFeaturePresent(n)`, `0x40 <= n < 0xc0`) 는 test.exe 길에 없다. ntdll 이 부르는 번호는 모두 `0x40` 미만 (KUSD 쪽).
- **스케줄러 공유 데이터 (클래스 `0x70`)**: 커널 `NtSetInformationProcess +0xec4` 갈래 — 길이 정확히 8, 핸들 접근 `0x200` → `PspSchedulerSharedDataRegionCreate` (call `+0xf17`) 가 섹션 (`MmCreateSectionEx`) 을 만들어 **이 프로세스에 붙이고** (`MmMapViewOfSection`) 핸들 (`ObInsertObjectEx`) 을 돌려준다 (`+0x13ea ~ +0x13ef`). run44 사진의 "옛 매개변수 자리에 생긴 2쪽짜리 매핑 섹션" 이 이것이다.
- **TEB `+0x1850` (`_TEB.SchedulerSharedDataSlot`) 을 쓰는 곳은 `LdrpAllocateSchedulerSharedData` 가 아니다.** 바로 다음 호출 `LdrpInitializeProcess +0x1837 / +0x183c LdrpAcquireSchedulerSharedDataSlot` 의 **`+0x92 MOV [RDI+0x1850],RAX`** 다. 노드 `+0x10` 핸들 (`+0x77`) 을 넣어 `NtSetInformationThread(-2, 0x39, 버퍼, 0x18)` (`+0x84 / +0x89`) 로 칸 주소를 받는다. 건너뛰는 조건: 목록이 비었거나 (`+0x2d`/`+0x30`), `PEB.NtGlobalFlag2 & 0x10` (`+0x40`).
- **TLS (test.exe)**: `LdrpInitializeTls` 가 보는 모듈은 **exe 하나** (ntdll 항목은 `LoadReason == 9` 라 건너뜀, `+0x5d`/`+0x64`). 그래서 `N = 1` → `LdrpTlsBitmap = {SizeOfBitMap 9, Buffer &LdrpStaticTlsBitmapVector}`, `LdrpActualBitmapSize = 1`, `RtlSetBits(0, 1)` → `RtlClearBits(1, 8)` → 비트열 바이트 0 = `0x01`. TLS 힙 = 프로세스 힙 (`HeapPartitionName` 이 비어서), 벡터 = `LdrpGetNewTlsVector(9)` → 힙 `0x58`B (머리 `0x10` + 칸 9 × 8), 반환 = 블록 `+0x10` → TEB `+0x58`.
- **에뮬레이터 덫 후보**: `RtlSetBits`·`RtlClearBits` 는 마스크 표 둘을 **ntdll 이미지 안 `.rdata`** 에서 읽는다 (`LEA RBP,[0x180000000]` 로 이미지 시작을 잡고 `[RSI+RBP+rva]`): 표 `0x192548` = `00 01 03 07 0f 1f 3f 7f ff`, 표 `0x180558` = `ff fe fc f8 f0 e0 c0 80 00`. ntdll `.rdata` 가 제자리에 올라와 있어야 한다.

오프셋 읽는 법 (run60 ~ run64 와 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값.

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE1`, ntoskrnl pdb GUID `C29EBFB06B78B3C020DCA66D99713F9E1` (결과서 버전 대조 — `_calls.tsv` 로 알려진 사실 셋 확인, 요청서의 `LdrpInitializeProcess` 호출 자리 10곳 모두 일치)
- 값 대조: run44 사진 P1a 덤프 읽기 로그 (`ms/logs/04_baggage-read_P1a.txt` — 매개변수 `CurrentDirectory` 2552 ~ 2553줄, TEB `+0x58` 1979·3430 ~ 3433줄, TEB `+0x1850` 2108줄, KUSD `CurDirDevicesSkippedForDlls` 2927줄, `HeapPartitionName` 2543줄) 와 짐 지도 7.3 의 사진 분류. 새 동적 실행은 없다.

## 목적

- PE_emulator 가 진짜 ntdll 코드로 돌리다 덫에 걸렸거나 "공사 중" 빈 함수로 둔 초기화 함수 약 25개의 **인자 · 쓰는 칸 · 하위 호출 · syscall · 반환**.
- 묶음 A (`LdrpInitializeProcess` 줄기 위, 시간 순서) 를 자세히: 정책 → exe 폴더 종류 → 현재 디렉터리 → CPU 기능 비트맵 → 스케줄러 공유 데이터 → TLS. 묶음 B (모듈 올리기 길 부품) 는 보통, 묶음 C (공용 부품) 는 짧게.
- 커널 쪽: `NtQuerySystemInformation` 클래스 `0xfa` 갈래와 nt 가 줄 값.

## 방법

- 정적: `_workspace/sast/59_ldr-init-bodies.md` — ntdll 대상 24개 + 길 따라 8개 (syscall 번호 셋 `NtOpenSection`·`NtQueryVolumeInformationFile`·`NpQueryVirtualMemory`, 비트맵 읽는 쪽 `RtlIsProcessorFeaturePresent`, `LdrpQueryIllegalCWDDevices`, 이름만 볼 하위 셋), ntoskrnl `NtQuerySystemInformation`·`ExpQuerySystemInformation` (클래스 `0xfa` 갈래만)·`KiSetFeatureBits`·`PspSchedulerSharedDataRegionCreate`. 모두 depth 0 (공용 부품이 딸려오지 않게 이름 지정).
  - 이미 있던 `.asm` 16개는 다시 안 뽑고 읽기만 (`LdrpInitializeProcess`·`LdrpInitializeTls`·`LdrpAllocateTls`·`LdrpAcquireSchedulerSharedDataSlot`·`NtSetInformationProcess` 등).
  - 새 `_refs`: ntdll `0x1d3d70` `0x1d3d80` `0x1d23f4` `0x1d23f8` `0x1d2770` `0x1d2960` `0x1d26c8` `0x1d26e0` `0x1e7120`, ntoskrnl `KeProcessorFeaturesBuffer` `KeProcessorFeaturesBitMap` `0xfc7480`.
  - analyze 다시 안 함. 같은 때 sast/60 이 있었지만 `pgrep` 로 확인하고 export·refs 만 돌려 잠금 충돌 없음 (결과서).
- 결과서: `_workspace/sast/59_ldr-init-bodies_result.md` (**분석성공**).
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `RtlInitializeProcessorFeaturesBitMap +0xb LEA RBX,RtlProcessorFeaturesBuffer` · `+0x1a MOV R8D,0x10` · `+0x20 MOV ECX,0xfa` · `+0x25/+0x2a` · `+0x2a`/`+0x35` 비트맵 `{0x80, &버퍼}`, `LdrpAllocateSchedulerSharedData +0x19 MOV R9D,8` · `+0x28 LEA EDX,[R9+0x68]` (= `0x70`) · `+0x2c/+0x31`, `LdrpInitializeProcess +0x1827/+0x182c` · `+0x1837/+0x183c`, `LdrpAcquireSchedulerSharedDataSlot +0x84/+0x89` · `+0x92 MOV [RDI+0x1850],RAX`.
- flow-writer 가 `.asm` 으로 한 번 더 본 줄:
  - `LdrpInitializeProcess`: `+0xdb1 LEA RCX,[LdrpKnownDllDirectoryHandle]` · `+0xdb8/+0xdbd` · `+0xe47/+0xe4c` · `+0xe90/+0xe95` · `+0xf04/+0xf09`, `+0xf41 ~ +0xf53` (빈 디렉터리 검사) · `+0xf5e/+0xf63` · `+0xf84 MOV [RSP+0x5c],R15D` · `+0xf89/+0xf8e` · `+0xfa7 MOV [RSP+0xb0],0x80006`, `+0xfc5/+0xfca` · `+0xfdb TEST [RAX+0x8],0x20000000` · `+0xfe2 JZ` · `+0xfe4/+0xfe9`, `+0x1169 MOV EAX,[LdrpIllegalCWDDevices]` · `+0x1174 JA` · `+0x117e/+0x1183`, `+0x1517/+0x151c` · `+0x1522 JNS` · `+0x1554 CMP [RSP+0x5c],R15D` · `+0x1559 JNZ` · `+0x1563/+0x1568`, `+0x1758 CMP [LdrpIsSecureProcess],R15B` · `+0x175f JNZ` · `+0x1761/+0x1766`, `+0x176d JZ`, `+0x1806/+0x180b` · `+0x180b ~ +0x1820` (목록 머리), `+0x18cb/+0x18d0`. R15D 값: `+0x162 LEA R15D,[R14+1]` (1) 뒤 `+0x32f` 까지 R15 를 바꾸는 줄 없음, `+0xf09 XOR R15D,R15D` (0) 뒤 `+0xf84` 까지 없음.
  - `RtlpInitCurrentDir +0x1c/+0x21` · `+0x32/+0x37` · `+0x39 JNS` · `+0x47` · `+0x52` · `+0x5a` · `+0x62`.
  - `RtlpCreateNewDirectoryReference +0x73` · `+0x7e` · `+0x94` · `+0xbb/+0xc0` · `+0xd5/+0xda` · `+0xf7/+0xfc` · `+0x115/+0x11a` · `+0x15a` · `+0x15e` · `+0x162` · `+0x168` · `+0x16c` · `+0x170`.
  - `LdrpAllocateSchedulerSharedData +0xc` · `+0x13` · `+0x24` · `+0x57/+0x5c` · `+0x64` · `+0x70` · `+0x74/+0x79` · `+0xa6` · `+0xad/+0xb2` · `+0xd3/+0xd8`, `LdrpAcquireSchedulerSharedDataSlot +0x2d` · `+0x30` · `+0x40` · `+0x77`.
  - `LdrpInitializeTls +0x1f/+0x24` · `+0x10d` · `+0x115` · `+0x11d` · `+0x124` · `+0x127` · `+0x12e` · `+0x130` · `+0x13d` · `+0x144/+0x149` · `+0x156` · `+0x158/+0x15d` · `+0x15d/+0x162`, `LdrpInitializeTlsHeap +0x13` · `+0x2a` · `+0x39`, `LdrpAllocateTls +0x4d/+0x52` · `+0x1de` · `+0x1e2` · `+0x1e5`, `LdrpGetNewTlsVector +0x2c/+0x31` · `+0x41` · `+0x49/+0x4e`.
  - `LdrpInitializePolicy +0x22`·`+0x2d`·`+0x34`·`+0x3b` (두 전역의 앞·뒤 qword) · `+0x7e` · `+0xb0` · `+0xbb` · `+0x110` · `+0x26b` · `+0x305`.
  - ntoskrnl: `NtQuerySystemInformation +0x58/+0x5d`, `ExpQuerySystemInformation +0x3b6/+0x3bb` · `+0x851` · `+0x860` · `+0x866 JZ` (→ `+0xa3d`) · `+0xa3d` · `+0xa45` · `+0xa49 JNC` (→ `+0xf4f`) · `+0xa4f` · `+0xf4f` · `+0xf53` · `+0xf59` · `+0xf60`, `KiSetFeatureBits +0x881` · `+0x888` · `+0x88f`, `NtSetInformationProcess +0xec4` · `+0xf02/+0xf07` · `+0xf17/+0xf1c` · `+0x13ea` · `+0x13ef`, `PspSchedulerSharedDataRegionCreate +0x158/+0x15d` (`MmCreateSectionEx`) · `+0x24c/+0x251` (`MmMapViewOfSection`) · `+0x276/+0x27b` (`ObInsertObjectEx`).
- 로그: 없음 (정적만, 동적 필수 아님).

## 결과

### 정적 — 호출 지점 (`LdrpInitializeProcess` 안, `_calls.tsv`)

| call | ret (= 콜스택 오프셋) | callee | 조건 | test.exe | 표시 |
|---|---|---|---|---|---|
| `+0xdb8` | `+0xdbd` | `ntdll!NtOpenDirectoryObject` (`\KnownDlls`) | `PEB.IsProtectedProcess == 0` (`+0xd54/+0xd57`) | 탐 | [정적] |
| `+0xe47` | `+0xe4c` | `ntdll!NtOpenSymbolicLinkObject` (`KnownDllPath`) | 위 + 디렉터리 열기 성공 | 탐 | [정적] |
| `+0xe90` | `+0xe95` | `ntdll!NtQuerySymbolicLinkObject` | 위 + 링크 열기 성공, `0xc0000023` 이면 다시 | 탐 (1번 `[추측]` — 대상이 `0x30`B 이내일 때) | [정적] |
| `+0xf04` | `+0xf09` | `ntdll!NtClose` (링크 핸들) | 위 | 탐 | [정적] |
| `+0xfc5` | `+0xfca` | `ntdll!LdrpInitializePolicy` | 없음 | 탐 (거의 아무것도 안 함) | [정적] |
| `+0xfe4` | `+0xfe9` | `ntdll!LdrpIncludeAlternateForwarders` | `ProcessParameters.Flags & 0x20000000` (`+0xfdb/+0xfe2`) | **안 탐** (사진 `Flags = 0x4001`) | [정적] |
| `+0x117e` | `+0x1183` | `ntdll!LdrpCheckAppDirType` | `LdrpIllegalCWDDevices` 가 0 도 −1 도 아님 (`+0x1169 ~ +0x1174`) | **안 탐** (마스크 0) | [정적] |
| `+0x1517` | `+0x151c` | `ntdll!RtlpInitCurrentDir` | 없음 (실패해도 로그만, `+0x1522 JNS`) | 탐 | [정적] |
| `+0x1563` | `+0x1568` | 문자열 해제 (rva `0x4e200`, 같은 주소 이름 3개) | `[RSP+0x5c] == 0` (`+0x1554/+0x1559`) = 현재 디렉터리가 빈 갈래 | **안 탐** (진짜 VM) | [정적] |
| `+0x1761` | `+0x1766` | `ntdll!RtlInitializeProcessorFeaturesBitMap` | `LdrpIsSecureProcess == 0` (`+0x1758/+0x175f`) | 탐 | [정적] |
| `+0x1827` | `+0x182c` | `ntdll!LdrpAllocateSchedulerSharedData` | `UseWOW64 == 0` (`+0x176d JZ → +0x1806`) | 탐 | [정적] |
| `+0x1837` | `+0x183c` | `ntdll!LdrpAcquireSchedulerSharedDataSlot` | 위와 같음 (안 조건은 run41) | 탐 | [정적] |
| `+0x18cb` | `+0x18d0` | `ntdll!LdrpInitializeTls` | 네이티브 길 | 탐 | [정적] |

### 정적 — 안쪽 호출 지점 (대표)

| caller | call | ret | callee | 표시 |
|---|---|---|---|---|
| `ntdll!RtlpInitCurrentDir` | `+0x1c` | `+0x21` | `ntdll!RtlDetermineDosPathNameType_Ustr` (종류 1·2·6·7 만 통과, 나머지 `0xc000000d`) | [정적] |
| `ntdll!RtlpInitCurrentDir` | `+0x32` | `+0x37` | `ntdll!RtlpCreateNewDirectoryReference` | [정적] |
| `ntdll!RtlpCreateNewDirectoryReference` | `+0xbb` | `+0xc0` | `ntdll!NtOpenFile` (syscall `0x33`) | [정적] |
| `ntdll!RtlpCreateNewDirectoryReference` | `+0xd5` | `+0xda` | `ntdll!RtlFreeHeap` (NT 경로 버퍼, 열기 성공 여부와 무관) | [정적] |
| `ntdll!RtlpCreateNewDirectoryReference` | `+0xf7` | `+0xfc` | `ntdll!NtQueryVolumeInformationFile` (syscall `0x49`, 클래스 4) | [정적] |
| `ntdll!RtlpCreateNewDirectoryReference` | `+0x115` | `+0x11a` | `ntdll!RtlAllocateHeap` (`0x30 + 최대 길이`) | [정적] |
| `ntdll!RtlInitializeProcessorFeaturesBitMap` | `+0x25` | `+0x2a` | `ntdll!NtQuerySystemInformation` (syscall `0x36`, 클래스 `0xfa`. `.asm` 표시는 같은 주소 이름 `RtlGetNativeSystemInformation` 도 셈 — 결과서 "스크립트 문제") | [정적] |
| `nt!NtQuerySystemInformation` | `+0x58` | `+0x5d` | `nt!ExpQuerySystemInformation` (클래스 `0xfa` 만의 함수는 없음) | [정적] |
| `ntdll!LdrpAllocateSchedulerSharedData` | `+0x2c` | `+0x31` | `ntdll!NtSetInformationProcess` (syscall `0x1c`, 클래스 `0x70`) | [정적] |
| `nt!NtSetInformationProcess` | `+0xf17` | `+0xf1c` | `nt!PspSchedulerSharedDataRegionCreate` | [정적] |
| `ntdll!LdrpAllocateSchedulerSharedData` | `+0x57` | `+0x5c` | `ntdll!RtlAllocateHeap` (`LdrpHeap`, `0x18`) | [정적] |
| `ntdll!LdrpAcquireSchedulerSharedDataSlot` | `+0x84` | `+0x89` | `ntdll!NtSetInformationThread` (클래스 `0x39`, run41) | [정적] |
| `ntdll!LdrpInitializeTls` | `+0x1f` | `+0x24` | `ntdll!LdrpInitializeTlsHeap` | [정적] |
| `ntdll!LdrpInitializeTls` | `+0xf5` | `+0xfa` | `ntdll!LdrpAllocateTlsEntry` (exe 하나) | [정적] |
| `ntdll!LdrpInitializeTls` | `+0x144` | `+0x149` | `ntdll!RtlSetBits(&LdrpTlsBitmap, 0, N)` | [정적] |
| `ntdll!LdrpInitializeTls` | `+0x158` | `+0x15d` | `ntdll!RtlClearBits(&LdrpTlsBitmap, N, 8)` | [정적] |
| `ntdll!LdrpInitializeTls` | `+0x15d` | `+0x162` | `ntdll!LdrpAllocateTls` | [정적] |
| `ntdll!LdrpAllocateTls` | `+0x4d` | `+0x52` | `ntdll!LdrpGetNewTlsVector(LdrpTlsBitmap.SizeOfBitMap)` | [정적] |
| `ntdll!LdrpInsertModuleToIndexLockHeld` | `+0x91` · `+0xe7` | `+0x96` · `+0xec` | `ntdll!RtlRbInsertNodeEx` (트리 둘) | [정적] (run35 자리) |
| `ntdll!LdrpLoadKnownDll` | `+0x5e` | `+0x63` | `ntdll!LdrpFindKnownDll` | [정적] |
| `ntdll!LdrpFindKnownDll` | `+0xd4` | `+0xd9` | `ntdll!NtOpenSection` (syscall `0x37`, 접근 `0xd`) | [정적] |
| `ntdll!LdrpLoadKnownDll` | `+0xec` | `+0xf1` | `ntdll!LdrpMapDllWithSectionHandle` | [정적] (run21 자리) |
| `ntdll!LdrpLoadKnownDll` | `+0xf8` | `+0xfd` | `ntdll!NtClose` (섹션 핸들) | [정적] |
| `ntdll!LdrpProcessMappedModule` | `+0x83` | `+0x88` | `ntdll!LdrpValidateEntrySection` | [정적] |
| `ntdll!LdrpProcessMappedModule` | `+0x27b` | `+0x280` | `ntdll!LdrpIsImageAddress` (앞 결과가 `0xc0000141` 일 때만) | [정적] |

### 질문별 요약 (근거 줄은 결과서 해당 절)

**묶음 A — `LdrpInitializeProcess` 줄기 위**

1. **`LdrpInitializePolicy`** — 인자·반환 없음. `LdrpAppPackagesPath`·`LdrpOriginalAppPackagesPath` (각 `UNICODE_STRING` 16B, 앞·뒤 qword) 를 0 으로 (`+0x22`·`+0x2d`·`+0x34`·`+0x3b`). PEB `BitField` 비트 4 (`IsPackagedProcess`) 면 패키지 갈래 (`+0x72 ~`), 비트 1 (`IsProtectedProcess`) 면 `DllPath.Length = 0` 하고 끝, `DllPath.Length != 0` 이면 `LdrSetDllDirectory(&DllPath)`.
   - test.exe: 사진 `BitField = 0x4`, `DllPath = ""` `[관찰]` → `+0x4e` 에서 바로 끝. syscall 없음.
   - 패키지 갈래 (안 탐, 조건·이름만): `LdrpPolicyBits = 0x29` (`+0x7e`), `RtlQueryPackageClaims` 다섯 번, `LdrpPolicyBits` 비트 켜고 끄기 (`+0x26b` `|= 4` · `+0x305` `|= 0x40` · `+0x31d` `|= 2` · `+0x329` `&= ~1`), `LdrpDefaultDllDirectories = 0x1000` (`+0x110`), `ProcessParameters.Flags` 비트 26 (`0x4000000`, `+0x2eb ~ +0x2ef`).
   - 정책 전역 이름 = `LdrpPolicyBits` (rva `0x1cd7fc`, 파일 값 `0x16`, test.exe 에서 안 바뀜).
   - **`LdrpIncludeAlternateForwarders`** — `ProcessParameters.Flags & 0x4000000` 또는 `PEB.NtGlobalFlag2` 비트 1 이면 TRUE. test.exe 에서 **안 불림**. 불리면 결과가 `RtlpSystemDirs` 에 `forwarders\alt;` 를 붙일지 정한다 (`+0x10e9`).
2. **`LdrpCheckAppDirType`** — exe 경로를 열어 (`NtOpenFile`, 접근 `0x100001`·공유 5·옵션 `0x60`) 장치 특성 (`NtQueryVolumeInformationFile` 클래스 4) 을 보고, `LdrpIllegalCWDDevices & 특성` 이 0 이 아니면 `LdrpIllegalCWDDevices = 0` (`+0x11b`). test.exe 에서 **안 불림** (마스크 0).
   - `LdrpIllegalCWDDevices` 는 장치 이름 목록이 아니라 **4바이트 마스크** (질문 11). 정하는 곳은 `LdrpQueryIllegalCWDDevices` (`LdrpInitializeExecutionOptions +0x733` 이 부름): IFEO `CWDIllegalInDLLSearch` 값, 없으면 KUSD `+0x2d5` 비트 4~5 `CurDirDevicesSkippedForDlls` (`0` → 0, `1` → `0x2000`, `2` → `0x10`, `3` → −1). 사진 `CurDirDevicesSkippedForDlls = 0y00` `[관찰]`.
3. **`RtlpInitCurrentDir` · `RtlpCreateNewDirectoryReference`** — 인자 = 매개변수 `CurrentDirectory.DosPath` 사본 (`+0xf26 ~ +0xf2b`).
   - 경로 종류 검사 → `RtlpCreateNewDirectoryReference(경로, DosPath.MaximumLength, &레코드)` → 성공하면 쓰는 칸 넷: **`RtlpCurDirRef`** (rva `0x1d23f8`, `+0x47`) · `CurrentDirectory.Handle` ← 레코드 `+0x8` (`+0x52`) · `DosPath.Buffer` ← 레코드 `+0x20` (`+0x5a`) · `DosPath.Length` ← 레코드 `+0x18` (`+0x62`).
   - 레코드 (힙 `0x30 + 최대 길이`, 사진 기준 `0x238`B): `+0x0` 참조 수 1 · `+0x8` 디렉터리 핸들 · `+0x10` KUSD `DismountCount` 복사 · `+0x18` 길이 · `+0x1a` 최대 길이 · `+0x20` 버퍼 포인터 (= 레코드 `+0x30`) · `+0x28` 장치 특성 · `+0x30 ~` 경로 글자 (끝 `\` 보장). 칸 이름은 PDB 에 없다 `[추측: 뜻]`.
   - 사진 P1a `Length 0x84 · Max 0x208 · Buffer 힙 · Handle 0x58` `[관찰]` 과 맞는다.
4. **`RtlInitializeProcessorFeaturesBitMap`** — 위 "꼭 챙길 값". 비트맵 전역 이름 `RtlProcessorFeaturesBitMap` (rva `0x1d3d80`, `_RTL_BITMAP` 16B).
5. **`LdrpAllocateSchedulerSharedData`** — 위 "꼭 챙길 값". 부르기 전 `LdrpInitializeProcess` 가 `LdrpSchedulerSharedDataListHeadLock = 0` (`+0x180b`), `LdrpSchedulerSharedDataListHead` 빈 목록 (`+0x1812 ~ +0x1820`). 노드 `0x18`B (`LdrpHeap`, 0 으로 채움) `+0x10` = 핸들, 락 안에서 목록에 끼움 (`+0x79 ~ +0xa6`, 연결 검사 틀리면 `INT 0x29`).
6. **TLS 묶음** — 위 "꼭 챙길 값". `LdrpAllocateTlsEntry` 가 `LdrpTlsList` 항목 `0x48`B (`LdrpTlsHeap`) 를 만들고 TLS 디렉터리 `0x28`B 를 `+0x10` 에 복사, **exe 의 `_tls_index` 변수에 0** 을 쓰고 (`+0x9e ~ +0xaf`), 목록 꼬리에 잇는다. `LdrpInitializeTls +0x102` 가 exe 항목 `TlsIndex` 를 `0xffff` 로. `LdrpAllocateTls` 가 벡터 칸 0 에 exe TLS 블록을 걸고 TEB `+0x58` (`+0x1e2`) · `LdrpActiveThreadCount++` (`+0x1e5`).
   - `LdrpInitializeTlsHeap`: `LdrpHeapPartitionName.Buffer == NULL` 이면 `LdrpTlsHeap = PEB.ProcessHeap` (`+0x39`). 아니면 따로 힙 (`RtlpCreateHeap(2, …)`, 이름 `TLSHeap`).

**묶음 B — 모듈 올리기 길 (보통)**

7. **`LdrpValidateEntrySection`** — 진입점이 0 이거나, 항목 `EntryPoint` 가 0 이거나, 진입점이 `SizeOfHeaders` 이상이면 TRUE. **헤더 안을 가리키면 FALSE** → `LdrpProcessMappedModule +0x8a JZ` 로 `0xc000007b`. ntdll·exe 둘 다 TRUE. **`LdrpIsImageAddress`** — `NpQueryVirtualMemory(-1, 주소, 0, &MBI, 0x30)` 로 `Type == MEM_IMAGE` 인지. 부르는 두 곳 모두 에러 길이라 test.exe 길에선 **안 불림**.
8. **`RtlRbInsertNodeEx(트리, 부모, 오른쪽?, 새 노드)`** — 첫 노드 (빈 트리) = `Root = Min = 새 노드`, `ParentValue = 0` (검정). 둘째 노드 = 부모 자식 칸에 넣고 `ParentValue = 부모 | 1` (빨강), 부모가 검정이라 회전 없음. 왼쪽이고 부모가 `Min` 이면 `Min = 새 노드`.
   - test.exe 순서: ntdll (`LdrpInitializeNtdllDataTableEntry +0x116` 쪽) → 두 트리 모두 첫 노드 갈래. exe (`LdrpInitializeProcess +0x14d2` 쪽) → `LdrpMappingInfoIndex` 에서 **왼쪽** (파일 `TimeDateStamp` ntdll `0x6bdf03ca` > exe `0x6ad22b72`, `[정적 계산]`), `LdrpModuleBaseAddressIndex` 에서도 왼쪽일 것 `[추측]` (`DllBase` 크기 비교, ASLR 값은 실행마다 다름).
9. **`LdrpFindKnownDll` · `LdrpLoadKnownDll`** — `LdrpKnownDllDirectoryHandle == 0` 이면 바로 `0xc0000135`. 아니면 `NtOpenSection(접근 0xd, Root = \KnownDlls 핸들, 이름, 0x40)` → 없으면 `0xc0000034` 를 `0xc0000135` 로 바꿔 돌려줌 → 있으면 전체 경로 `LdrpKnownDllPath + "\" + 이름` (프로세스 힙) 과 기본 이름을 채움. `LdrpLoadKnownDll` 은 이미 올라왔으면 `LdrpLoadContextReplaceModule`, 아니면 `LdrpMapDllWithSectionHandle`, 끝에 늘 `NtClose(섹션)`. 쓰는 전역 없음.
10. **`\KnownDlls` 스텁 셋** — 번호 `0x58`·`0x138`·`0x16b`, 인자는 위 syscall 표. `LdrpKnownDllPath` 처음 버퍼 `0x30`B (`+0xd65`), `0xc0000023` 이면 해제하고 돌아온 길이로 다시 (`+0xea2 JNZ`). `PEB.IsProtectedProcess` 면 넷 다 건너뜀 (`+0xd54/+0xd57`). 앞선 run18 (`+0xdb8` 자리) 위에 인자·번호를 더한 것이다.
11. **`LdrpIllegalCWDDevices`** — 질문 2. 파일 값 0 (BSS 꼬리). 읽는 곳 다섯 (`LdrpLoadDependentModuleInternal +0x1649` · `LdrpGetFullPath +0x7c` · `LdrpPreprocessDllName +0x6cd` · `LdrpInitializeProcess +0x1169` · `LdrpCheckAppDirType +0x10f`).

**묶음 C — 공용 부품 (짧게)**

12. `RtlGetNtSystemRoot` = `PEB.SharedData` 의 silo 경로가 있으면 그것, 없으면 KUSD `NtSystemRoot` (`0x7ffe0030`) — 사진 `PEB+0x90 = 0` → KUSD. `RtlInitUnicodeStringEx` · `RtlAppendUnicodeToString` 은 길이 검사 (`0x7ffe` 글자, `0xc0000106` / `0xc0000023`) 와 복사. 문자열 해제는 위 "갈림길".
13. `LdrControlFlowGuardEnforced` — `LdrSystemDllInitBlock +0xb8` 이 0 이면 0, 아니면 `(~[+0x9c]) & 1`. test.exe 0 (run63 과 같음).

### 호출 관계

```
LdrpInitializeProcess (0x8c404)
 │  ⑨ 시스템 경로 · \KnownDlls · DLL 정책
 ├─ +0xdbd  NtOpenDirectoryObject      syscall 0x58 → .mrdata LdrpKnownDllDirectoryHandle
 ├─ +0xe4c  NtOpenSymbolicLinkObject   syscall 0x138 (KnownDllPath)
 ├─ +0xe95  NtQuerySymbolicLinkObject  syscall 0x16b → LdrpKnownDllPath (힙 0x30B, 모자라면 다시)
 ├─ +0xf09  NtClose
 │  +0xf41 ~ +0xfb2  현재 디렉터리가 비었으면 8B 힙에 "C:\" · [RSP+0x5c] = 0      (test.exe 안 탐)
 ├─ +0xfca  LdrpInitializePolicy        두 전역 0 · PEB 비트 둘 · DllPath 길이 0 → 끝
 ├─ (+0xfe9 LdrpIncludeAlternateForwarders — Flags 0x20000000 일 때만, 안 탐)
 │  ⑩
 ├─ (+0x1183 LdrpCheckAppDirType — 마스크가 0·−1 아닐 때만, 안 탐)
 │  뒤1
 ├─ +0x151c RtlpInitCurrentDir
 │        ├─ +0x21  RtlDetermineDosPathNameType_Ustr
 │        └─ +0x37  RtlpCreateNewDirectoryReference
 │                 ├─ +0xc0   NtOpenFile                     syscall 0x33
 │                 ├─ +0xda   RtlFreeHeap (NT 경로)
 │                 ├─ +0xfc   NtQueryVolumeInformationFile   syscall 0x49, 클래스 4
 │                 └─ +0x11a  RtlAllocateHeap (0x238B 레코드)
 │        +0x47 RtlpCurDirRef · +0x52 Handle · +0x5a Buffer · +0x62 Length
 ├─ (+0x1568 문자열 해제 — 빈 갈래만, 안 탐)
 │  뒤3
 ├─ +0x1766 RtlInitializeProcessorFeaturesBitMap
 │        └─ +0x2a  NtQuerySystemInformation(0xfa)  syscall 0x36
 │                  ══▶ nt NtQuerySystemInformation +0x5d ExpQuerySystemInformation
 │                        +0x866 → +0xa3d → +0xf4f ~ +0xf60  KeProcessorFeaturesBuffer 16B 복사
 │        +0x2a · +0x35 RtlProcessorFeaturesBitMap = {0x80, &버퍼}
 │  뒤4 (네이티브 길)
 ├─ +0x180b LdrpInitializeCfgScpHelpers       (run45, 그 뒤 +0x180b ~ +0x1820 목록 머리·락 0)
 ├─ +0x182c LdrpAllocateSchedulerSharedData
 │        ├─ +0x31  NtSetInformationProcess(-1, 0x70)  syscall 0x1c
 │        │         ══▶ nt NtSetInformationProcess +0xec4 → +0xf1c PspSchedulerSharedDataRegionCreate
 │        │               (섹션 · 이 프로세스에 매핑 · 핸들)
 │        ├─ +0x5c  RtlAllocateHeap(LdrpHeap, 0x18)  노드 +0x10 = 핸들
 │        └─ +0x79 / +0xb2  락 안에서 LdrpSchedulerSharedDataListHead 에 끼움
 ├─ +0x183c LdrpAcquireSchedulerSharedDataSlot (run41)
 │        └─ +0x89  NtSetInformationThread(-2, 0x39)  → +0x92 TEB+0x1850 SchedulerSharedDataSlot
 └─ +0x18d0 LdrpInitializeTls
          ├─ +0x24   LdrpInitializeTlsHeap            LdrpTlsHeap = PEB.ProcessHeap
          ├─ +0xfa   LdrpAllocateTlsEntry (exe)       LdrpTlsList 항목 0x48B · exe _tls_index = 0
          │  +0x102  exe TlsIndex = 0xffff
          │  +0x11d ~ +0x13d  LdrpActualBitmapSize 1 · LdrpTlsBitmap {9, &LdrpStaticTlsBitmapVector}
          ├─ +0x149  RtlSetBits(0, 1)
          ├─ +0x15d  RtlClearBits(1, 8)
          └─ +0x162  LdrpAllocateTls
                    ├─ +0x52  LdrpGetNewTlsVector(9)  힙 0x58B
                    │  +0x1e2 TEB+0x58 ThreadLocalStoragePointer
                    └─ +0x1e5 LdrpActiveThreadCount++
```

## 확인된 것

- `[정적]` test.exe 기본 길에서 묶음 A 의 syscall 은 **8번** (위 표). 그중 커널 짐을 새로 만드는 것: `\KnownDlls` 디렉터리 핸들 (`0x4c` `[관찰]` run44 P2 `!handle`), 현재 디렉터리 핸들 (`0x58` `[관찰]`), 스케줄러 공유 데이터 섹션·매핑·핸들 (`[관찰]` run44 2쪽 매핑 섹션 VAD).
- `[정적]` `LdrpInitializePolicy` 는 test.exe 에서 전역 둘을 0 으로 다시 쓰고 끝. `LdrpIncludeAlternateForwarders`·`LdrpCheckAppDirType`·`LdrpIsImageAddress` 는 test.exe 길에서 안 불린다 (조건 값은 사진 `[관찰]` `Flags 0x4001`·KUSD `0y00`, `LdrpIsImageAddress` 는 run63 반환값 0 / `0x117`).
- `[정적]` 현재 디렉터리 레코드를 만들고 매개변수 칸 셋 + `RtlpCurDirRef` 를 쓴다. `[관찰]` run44 P1a 칸 값과 맞는다.
- `[정적]` 클래스 `0xfa` = `ExpQuerySystemInformation +0xa3d` · `+0xf4f ~ +0xf60` (16바이트 복사 하나). 커널 버퍼에 쓰는 코드는 `KiSetFeatureBits +0x888` (0 채움) 뿐 → 정적 값 16바이트 0. ntdll 쪽 버퍼 사진 `끝까지 0*` `[관찰]` 과 맞음 (커널 값 확인은 아직).
- `[정적]` TEB `+0x1850` 은 `LdrpAcquireSchedulerSharedDataSlot +0x92` 가 쓴다 (`LdrpAllocateSchedulerSharedData` 는 `GS:` 를 안 씀). 그 슬롯이 가리키는 섹션은 `LdrpAllocateSchedulerSharedData` 의 클래스 `0x70` 이 만든 것. run44 `[관찰]` `TEB+0x1850 = 25996cb0000` = 2쪽 매핑 섹션 VAD 와 이어진다.
- `[정적]` test.exe 의 TLS 준비는 exe 하나만 보고 (`N = 1`), 비트맵 9비트 · 안쪽 32비트 칸 · TLS 힙 = 프로세스 힙. `[관찰]` run44 P1a `TEB+0x58 = 0x25996f383e0`, 그 칸 0 = `0x25996f38440` (16 정렬) 과 맞는다.
- `[정적]` `RtlRbInsertNodeEx` 첫 두 호출 (ntdll, exe) 은 회전 없는 갈래. 트리 둘은 BSS 라 `Encoded` 꺼짐 (포인터 그대로).
- `[정적]` `\KnownDlls` 스텁 번호 `0x58`·`0x138`·`0x16b`, `NtOpenSection` `0x37`, `NtOpenFile` `0x33`, `NtQueryVolumeInformationFile` `0x49`, `NtQuerySystemInformation` `0x36`, `NtSetInformationProcess` `0x1c`, `NtClose` `0xf`, `NpQueryVirtualMemory` `0x23`.

## 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

| 칸 | 사진 값 | 정적 줄 | 맞나 |
|---|---|---|---|
| 매개변수 `CurrentDirectory.DosPath` · `Handle` (P1a) | `Length 0x84` · `Max 0x208` · `Buffer 0x25996f381c0` (힙) · `Handle 0x58` | `RtlpInitCurrentDir +0x52 ~ +0x62`, 레코드 `0x30 + 0x208 = 0x238`B 의 `+0x30` 이 버퍼 | 맞음 |
| 매개변수 `Flags` | `0x4001` | `+0xfdb` 비트 `0x20000000` 꺼짐 → `LdrpIncludeAlternateForwarders` 안 탐 | 맞음 |
| PEB `BitField` · `DllPath` | `0x4` · `""` | `LdrpInitializePolicy +0x4e` 에서 끝 | 맞음 |
| KUSD `CurDirDevicesSkippedForDlls` (P1a 2927줄) | `0y00` | 마스크 0 → `LdrpCheckAppDirType` 안 탐 (IFEO 값 없음 가정) | 맞음 |
| 매개변수 `HeapPartitionName` (P1a 2543줄) | `""` | `LdrpTlsHeap = PEB.ProcessHeap` | 맞음 |
| TEB `+0x58` (P1a 1979·3432줄) | `0x25996f383e0`, 칸 0 = `0x25996f38440` | `LdrpAllocateTls +0x1e2`, 벡터 = 블록 `+0x10` | 맞음 |
| TEB `+0x1850` (P1a 2108줄) | `0x25996cb0000` | `LdrpAcquireSchedulerSharedDataSlot +0x92` (섹션 출처 클래스 `0x70`) | 맞음 |
| ntdll `.data` 사진 분류 (짐 지도 7.3) | `≤P1a`: `RtlpCurDirRef`·`LdrpTlsHeap`·`LdrpTlsBitmap`·`LdrpStaticTlsBitmapVector*`·`LdrpActualBitmapSize`·`LdrpActiveThreadCount`·`LdrpSchedulerSharedDataListHead`·`RtlProcessorFeaturesBitMap` / `끝까지 0*`: `RtlProcessorFeaturesBuffer`·`LdrpIllegalCWDDevices`·`LdrpSchedulerSharedDataListHeadLock`·`LdrpAppPackagesPath`·`LdrpOriginalAppPackagesPath`·`LdrpDefaultDllDirectories` | 값이 생기는 칸은 `≤P1a`, 0 을 쓰거나·잡고 풀거나·조건부 안 탐인 칸은 `끝까지 0` | 맞음 (14개 모두) |

## 결과서와 다르게 적은 곳 · 보탠 것

- **`RtlProcessorFeaturesBuffer` 사진 대조는 결과서에 없다** — flow-writer 가 짐 지도 7.3 분류 (`끝까지 0*`) 로 보탰다. 정적 결론 "16바이트 0" 과 맞지만, syscall 실패여도 0 이라 동적 1 을 대신하지는 못한다.
- 결과서 질문 1 의 "전역 네 qword" 는 `UNICODE_STRING` 두 개 (`LdrpAppPackagesPath` `0x1d3070` · `LdrpOriginalAppPackagesPath` `0x1d3080`) 의 앞·뒤 qword 다 (같은 말).
- 결과서 "짐 지도 반영거리" 의 **TLS 블록 `0x11`B** 는 본문에 셈 줄이 없다. 짐 지도에는 크기를 `[추측]` (결과서 값) 으로만 적었다.
- 결과서 반영거리의 7.3 에서 빠지는 이름은 14개다. **`LdrpPolicyBits`** 도 쓰는 줄 (`LdrpInitializePolicy +0x7e`·`+0x26b`·`+0x305` 등, 패키지 갈래) 이 나왔지만 결과서 목록에 없어 7.3 에 그대로 두었다 (처음 값 = 파일 `0x16`, test.exe 에서 안 바뀜). 옮길지는 다음 바퀴 판단 (아래).
- `LdrpQueryIllegalCWDDevices` 를 부르는 `LdrpInitializeExecutionOptions +0x733` 이 test.exe 에서 실제로 닿는지는 결과서가 안 봤다 (IFEO 키가 없어 IFEO 를 건너뛴다 — run46). 마스크는 어느 쪽이든 0 이다 (BSS 처음 값 0, 사진 `끝까지 0*`).
- 짐 지도 3.9 `LdrpKnownDllDirectoryHandle` 줄의 `[추측]` "출력 칸일 것" 은 이번 줄 (`+0xdb1 LEA RCX` → `+0xdb8` 첫 인자) 로 `[정적]` 이 됐다. 쓴 주인은 짐 지도 규칙대로 **nt (syscall, test.exe 가 부름)** 로 옮겼다. 같은 규칙으로 3.8 `LdrpKnownDllPath` 에 커널 쪽 둘째 줄 (`NtQuerySymbolicLinkObject` 가 `Length`·글자를 씀) 을 보탰다 — 결과서 반영거리엔 "크기·재시도만 추가" 로 적혀 있던 것.

## 정정

- 없음. 앞선 run 의 말과 어긋나는 곳은 없었다 (run18 `\KnownDlls` 자리, run19 `+0x151c` "실패해도 로그만", run41 `+0x1850` 쓰는 곳, run28 TLS 길, run35 트리 둘 모두 그대로 맞음).

## 아직 확인 필요

동적 몫은 모두 **Q104 VM 단계**에 얹는다. 멈춤점은 test.exe 일 때만 걸리게 조건을 붙인다.

1. **(가장 중요) 클래스 `0xfa` 의 진짜 값** — `nt!ExpQuerySystemInformation +0xf4f` 근처 (test.exe 일 때) 에서 `dq nt!KeProcessorFeaturesBuffer L2`, 그리고 ntdll `RtlInitializeProcessorFeaturesBitMap +0x2a` 직후 `r eax` (상태 0) · `dq ntdll!RtlProcessorFeaturesBuffer L2`. 정적은 "모두 0" 인데, 비트를 켜는 줄이 레지스터 기준 쓰기라 `_refs` 가 놓쳤을 수 있다.
2. `LdrpModuleBaseAddressIndex` 둘째 노드 (exe) 의 방향 — `ntdll!RtlRbInsertNodeEx` 입구 4번 (ntdll×2, exe×2) 에서 `r rcx, rdx, r8b, r9`, 둘째 트리 `r8b` 가 0 인가.
3. `NtOpenSection` 횟수·이름 — `ntdll!LdrpFindKnownDll` 입구 `du @rcx` · 반환 상태, `ntdll!NtOpenSection` 입구 수. 덤: `LdrpInitializeProcess +0xe95` 뒤 `du poi(ntdll!LdrpKnownDllPath+8)` (링크 대상 문자열) · `dq ntdll!LdrpKnownDllDirectoryHandle L1`.
4. 힙 해제 뒤 첫 8바이트 — 진짜 VM 은 `+0x1563` 을 안 타므로, PE_emulator 와 같은 입력 (빈 현재 디렉터리) 을 줄 때만 그 버퍼에 `ba w8`.
- 확인용 (정적 + 사진으로 이미 맞음, 선택): `LdrpInitializePolicy +0x58` 에서 `dd ntdll!LdrpPolicyBits L1` = `0x16` · `LdrpIncludeAlternateForwarders`·`LdrpCheckAppDirType` 입구가 **안 걸리는지** · `RtlpInitCurrentDir +0x37` 뒤 `dq ntdll!RtlpCurDirRef L1` (계산상 `0x25996f38190` 쯤 `[추측]` — 버퍼 − `0x30`) · `LdrpAllocateSchedulerSharedData +0x31` 직후 `[RSP+0x30]` 핸들 · `+0xb2` 직후 `dq ntdll!LdrpSchedulerSharedDataListHead L2` · `LdrpInitializeTls +0x15d` 직전 `dd ntdll!LdrpTlsBitmap L1` = 9 · `db ntdll!LdrpStaticTlsBitmapVector L2` = `01 00` · `dq ntdll!LdrpTlsHeap L1` = `PEB+0x30`.
- 다음 바퀴 후보 (정적):
  - `LdrpPolicyBits` 를 짐 지도 7.3 에서 3.8 로 옮길지 (쓰는 줄은 패키지 갈래뿐, 처음 값은 파일).
  - `LdrpInitializeExecutionOptions +0x733` (`LdrpQueryIllegalCWDDevices`) 이 IFEO 키가 없을 때도 닿는지.
  - exe TLS 블록 크기 (`LdrpAllocateTls` 안 셈 — 결과서 `0x11`B 의 근거 줄).

## 도구 메모

- 새 함정 없음. `.asm` 의 `alias:RtlFreeAnsiString+2` · `alias:NtQuerySystemInformation+1` 꼴은 같은 주소 이름이 더 있다는 표시이고, 글에는 PDB 이름과 "같은 주소 이름 N개" 로 적었다 (flow-doc 규칙).

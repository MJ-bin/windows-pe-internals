# 59 ldr-init-bodies — 정적 분석 요청

- 날짜: 2026-11-05
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run64
- 대기열: **Q120b** (PE_emulator 요청 11 의 7번 — "함께 쌓인 것", PE_emulator 59 ~ 75). 사용자 결정 2026-11-05: Q120b·Q120c 둘 다 진행
- 관련: run63 (sast/58, Q120a — 요청 11 의 1 ~ 6), run44 사진 (TEB `+0x1850` ≠ 0), run45 (`LdrpInitializeProcess` 묶음 일 목록), run46 (힙), Q80 (`LdrControlFlowGuardEnforced` 묶음)
- 원문 (읽기만): `../PE_emulator/_workspace/orchestrator/64_ntdll-entry.md`, `../PE_emulator/_workspace/00_planner/65_ntdll-init-order.md`
- **같이 도는 작업**: 같은 때 sast/60 (Q120c, ntoskrnl) decompiler 가 돌 수 있다. Ghidra 프로젝트를 열기 전에 `pgrep -fa analyzeHeadless` 로 같은 모듈 프로젝트를 쓰는 작업이 있는지 보고, 있으면 끝난 뒤에 돌린다 (프로젝트 잠금).

## 무엇을 위해

- PE_emulator 는 아래 함수들을 진짜 ntdll 코드로 돌리다 덫에 걸렸거나, "공사 중" 빈 함수로 두었다. 함수마다 **인자 · 쓰는 칸 (전역·구조체 칸, 이름은 PDB 로) · 하위 호출 · syscall (이름·클래스·인자) · 반환** 이 필요하다.
- 함수가 많다 (본문 약 25). 공용 부품은 짧게, `LdrpInitializeProcess` 줄기 위의 것 (묶음 A) 을 먼저·자세히.

## 알고 싶은 것

### 묶음 A — `LdrpInitializeProcess` 줄기 위 (시간 순서, 자세히)

1. **`LdrpInitializePolicy`** (rva `0xbd38c`, 1697B, call `LdrpInitializeProcess +0xfc5/+0xfca`) — syscall·쓰는 전역 (정책 플래그 전역 이름). 크다 → test.exe (정책 설정 없음 기본) 에서 타는 갈래 중심, 나머지는 조건·이름만
   - 같이: **`LdrpIncludeAlternateForwarders`** (rva `0x1217c4`, 37B, call `+0xfe4/+0xfe9`) 본문
2. **`LdrpCheckAppDirType`** (rva `0x1191a0`, 353B, call `+0x117e/+0x1183`) — 무엇을 읽어 무엇을 쓰나, syscall
3. **`RtlpInitCurrentDir`** (rva `0x70e8`, 144B, call `+0x1517/+0x151c`) — 하위 `RtlDetermineDosPathNameType_Ustr` `+0x1c` · `RtlpCreateNewDirectoryReference` `+0x32` (이건 줄기 끝까지 — syscall 이 여기서 나오나, 예: 디렉터리 핸들 열기), 쓰는 상태 (PEB `ProcessParameters.CurrentDirectory`?, 현재 디렉터리 참조 전역)
4. **`RtlInitializeProcessorFeaturesBitMap`** (rva `0x11f910`, 66B, call `+0x1761/+0x1766`) — `+0x25/+0x2a NtQuerySystemInformation` 인자 (PE_emulator 실측 클래스 `0xfa`, 버퍼 `RtlProcessorFeaturesBuffer` rva `0x1d3d70`) · 크기 · 출력을 어떻게 쓰나 (비트맵 전역 이름)
   - **ntoskrnl 쪽**: `NtQuerySystemInformation` → 클래스 `0xfa` 갈래 함수 이름 → 출력 모양 (칸 오프셋·크기·뜻), nt 가 줄 진짜 값 (정적으로 정해지면 — 예: `KeFeatureBits`·`SharedUserData.ProcessorFeatures` 등 어디서 복사하나), 크기 검사·실패 상태값
5. **`LdrpAllocateSchedulerSharedData`** (rva `0xf96b0`, 217B, call `+0x1827/+0x182c`) — `+0x2c/+0x31 NtSetInformationProcess` 클래스·인자, `+0x57 RtlAllocateHeap` 크기, `+0xd3/+0xd8 NtClose` (무슨 핸들), 쓰는 칸 — **TEB `+0x1850` 을 쓰나** (PDB 칸 이름), 아니면 누가 쓰나. run44 사진 `[관찰]` ≠ 0 과 이어지게
6. **`LdrpInitializeTls`** (rva `0xabca8`, 443B, `.asm` 있음, call `+0x18cb/+0x18d0`) 덫 안 — `LdrpInitializeTlsHeap` (rva `0xabe6c`, 174B, `+0x1f`) · `LdrpGetNewTlsVector` (rva `0xa3e0c`, 100B, `LdrpAllocateTls +0x4d` 경유) · `RtlSetBits` (rva `0xa8460`, 158B, `+0x144`) · `RtlClearBits` (rva `0xac290`, 157B, `+0x158`) 본문
   - test.exe (exe TLS 콜백 있음 — sample) 에서 각각 어떤 인자로 불리나 (비트맵 전역 이름·크기, 몇 번째 비트), syscall 이 있나, TLS 힙 (`LdrpTlsHeap`?) 을 어떻게 만드나

### 묶음 B — 모듈 올리기 길 (보통)

7. **`LdrpValidateEntrySection`** (rva `0xe9fe0`, 75B, `LdrpProcessMappedModule +0x83/+0x88`) · **`LdrpIsImageAddress`** (rva `0x15eaf8`, 91B, `LdrpProcessMappedModule +0x27b/+0x280`, `LdrpQueryCurrentPatch +0x9b`) — 무엇을 검사하나, syscall (예: `NtQueryVirtualMemory` 클래스), 반환
8. **`RtlRbInsertNodeEx`** (rva `0x387a0`, 1692B) — PE_emulator 69 `LdrpInsertModuleToIndex` 덫 안. 트리 칸 근거: `_RTL_BALANCED_NODE` 칸 (자식 둘·부모+색 비트) 을 어떻게 쓰나, 트리 머리 (`_RTL_RB_TREE` Root·Min) 를 어떻게 바꾸나. 1692B — 첫 노드 넣기 / 둘째 노드 넣기 (회전 없는 경우) 갈래만 자세히, 회전은 이름·조건만
9. **`LdrpFindKnownDll`** (rva `0x9c70`, 469B) · **`LdrpLoadKnownDll`** (rva `0x9970`, 320B, `LdrpLoadDependentModuleInternal +0x178d`·`LdrpFindOrPrepareLoadingModule +0x5e0`) — syscall (`NtOpenSection` 등)·쓰는 전역 (KnownDlls 디렉터리 핸들, `LdrpKnownDllPath` 등)
10. **`\KnownDlls` 스텁 셋** — `NtOpenDirectoryObject` (rva `0x1618b0`) · `NtOpenSymbolicLinkObject` (`0x1634a0`) · `NtQuerySymbolicLinkObject` (`0x163b00`) disassemble: syscall 번호 (`MOV EAX,…`). 부르는 자리 `LdrpInitializeProcess +0xdb8/+0xdbd` · `+0xe47/+0xe4c` · `+0xe90/+0xe95` 의 인자 (객체 이름 문자열, 접근 마스크) 와 결과를 쓰는 전역
11. **`LdrpIllegalCWDDevices`** (rva `0x1d23f4`, 데이터, Ghidra 함수 범위 밖) — 쓰는 곳·읽는 곳 (`_refs`), 내용 (파일 값 — 장치 이름 목록?)

### 묶음 C — 공용 부품 (짧게: 인자·쓰는 칸·반환 한 줄)

12. `RtlGetNtSystemRoot` (`0x4cf20`, 53B) — 무엇을 돌려주나 (KUSD `NtSystemRoot`?) · `RtlInitUnicodeStringEx` (`0xb7930`, 74B) · `RtlAppendUnicodeToString` (`0x4ce60`, 176B) · `RtlFreeUnicodeString` (PDB rva `0x4e200`, `_index.tsv` 이름 `RtlFreeAnsiString` — 같은 주소 `RtlFreeUTF8String` 도, ICF) — PE_emulator 실측 "덫 뒤 UNICODE_STRING 16B 가 0, 버퍼 첫 8B 바뀜" 이 코드와 맞나 (힙 해제 `RtlFreeHeap` 이 버퍼 첫 8B 를 바꾸는 것인가)
13. `LdrControlFlowGuardEnforced` (`0xa6740`, 25B, `.asm` 있음) — 반환값이 무엇으로 정해지나 (run63: `LdrSystemDllInitBlock+0xb8`). Q80 과 겹침 — 본문만 짧게

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpInitializePolicy$`, `^LdrpIncludeAlternateForwarders$`, `^LdrpCheckAppDirType$` | 1 | 1·2 |
| ntdll | `^RtlpInitCurrentDir$`, `^RtlpCreateNewDirectoryReference$` | 1 | 3 |
| ntdll | `^RtlInitializeProcessorFeaturesBitMap$` | 1 | 4 |
| ntoskrnl | `^NtQuerySystemInformation$` + 클래스 `0xfa` 갈래 함수 (지도·`_calls.tsv` 로 찾아 그것만) | 0 | 4 |
| ntdll | `^LdrpAllocateSchedulerSharedData$` | 1 | 5 |
| ntdll | `^LdrpInitializeTlsHeap$`, `^LdrpGetNewTlsVector$`, `^LdrpAllocateTls$`, `^RtlSetBits$`, `^RtlClearBits$` | 1 | 6 (`LdrpInitializeTls.asm` 있음) |
| ntdll | `^LdrpValidateEntrySection$`, `^LdrpIsImageAddress$` | 1 | 7 |
| ntdll | `^RtlRbInsertNodeEx$` | 0 | 8 |
| ntdll | `^LdrpFindKnownDll$`, `^LdrpLoadKnownDll$` | 1 | 9 |
| ntdll | `^NtOpenDirectoryObject$`, `^NtOpenSymbolicLinkObject$`, `^NtQuerySymbolicLinkObject$` | 0 | 10 |
| ntdll | (refs) `LdrpIllegalCWDDevices` `0x1d23f4`, `RtlProcessorFeaturesBuffer` `0x1d3d70` | — | 11·4 |
| ntdll | `^RtlGetNtSystemRoot$`, `^RtlInitUnicodeStringEx$`, `^RtlAppendUnicodeToString$`, `^RtlFreeAnsiString$` | 0 | 12 |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where ntdll.dll <이름>` (ntoskrnl 은 `ntoskrnl.exe`).
- `LdrpInitializeProcess.asm`·`LdrpProcessMappedModule.asm`·`LdrpInitializeTls.asm`·`LdrControlFlowGuardEnforced.asm` 은 이미 있음 — 다시 뽑지 않아도 됨.
- 공용 부품 (`RtlAllocateHeap`·`RtlFreeHeap`·락·`memset`·`memcpy`) 은 이름만.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpInitializeProcess` 안 부르는 자리 (call/ret): `+0xdb8/+0xdbd NtOpenDirectoryObject` · `+0xe47/+0xe4c NtOpenSymbolicLinkObject` · `+0xe90/+0xe95 NtQuerySymbolicLinkObject` · `+0xfc5/+0xfca LdrpInitializePolicy` · `+0xfe4/+0xfe9 LdrpIncludeAlternateForwarders` · `+0x117e/+0x1183 LdrpCheckAppDirType` · `+0x1517/+0x151c RtlpInitCurrentDir` · `+0x1761/+0x1766 RtlInitializeProcessorFeaturesBitMap` · `+0x1827/+0x182c LdrpAllocateSchedulerSharedData` · `+0x18cb/+0x18d0 LdrpInitializeTls`
- 깊이/caller: `LdrpInitializePolicy` 5/1 · `LdrpCheckAppDirType` 5/1 · `RtlpInitCurrentDir` 5/1 · `RtlInitializeProcessorFeaturesBitMap` 5/1 · `LdrpAllocateSchedulerSharedData` 5/3 · `LdrpInitializeTlsHeap` 6/1 · `LdrpGetNewTlsVector` 6/2 (`LdrpAllocateTls +0x4d`, `LdrpHandleTlsData +0x449`) · `RtlSetBits` 5/5 · `RtlClearBits` 6/4 · `LdrpIsImageAddress` 5/2 · `LdrpValidateEntrySection` 6/1 · `RtlRbInsertNodeEx` 7/14 · `LdrpFindKnownDll` 8/1 · `LdrpLoadKnownDll` 7/2
- `RtlpInitCurrentDir` callee: `+0x1c RtlDetermineDosPathNameType_Ustr` · `+0x32 RtlpCreateNewDirectoryReference`
- `RtlInitializeProcessorFeaturesBitMap` callee: `+0x25/+0x2a NtQuerySystemInformation`
- `LdrpAllocateSchedulerSharedData` callee: `+0x2c NtSetInformationProcess` · `+0x57 RtlAllocateHeap` · `+0x74`/`+0xad` 락 · `+0xd3 NtClose`
- `LdrpInitializeTls` callee: `+0x1f LdrpInitializeTlsHeap` · `+0x76 RtlImageNtHeaderEx` · `+0x9c RtlImageDirectoryEntryToData` · `+0xdc LdrpLogInternal` · `+0xf5 LdrpAllocateTlsEntry` · `+0x144 RtlSetBits` · `+0x158 RtlClearBits` · `+0x15d LdrpAllocateTls` · `+0x19d RtlAllocateHeap`
- `LdrpLoadKnownDll +0x5e LdrpFindKnownDll`
- PDB: `RtlFreeUnicodeString` rva `0x4e200` = `_index.tsv` `RtlFreeAnsiString` (같은 주소 `RtlFreeUTF8String`). `LdrpIllegalCWDDevices` rva `0x1d23f4`, `RtlProcessorFeaturesBuffer` rva `0x1d3d70` — 둘 다 데이터 (`--where`: `[none]`)

## 이미 아는 것

- `[관찰]` run44: TEB `+0x1850` ≠ 0 (PE_emulator 74 가 적음 — 칸 이름·P 지점은 사진 로그로 확인해 주면 좋음)
- `[관찰]` PE_emulator (진짜 ntdll 코드를 Unicorn 에서 실행 — 26100.9457 VM 관찰 아님): `RtlInitializeProcessorFeaturesBitMap` 안 `NtQuerySystemInformation(0xfa, &RtlProcessorFeaturesBuffer, …)` · `RtlFreeUnicodeString` 뒤 UNICODE_STRING 16B 0·버퍼 첫 8B 바뀜
- `[정적]` run63: `LdrControlFlowGuardEnforced` 는 `LdrSystemDllInitBlock+0xb8` 로 정해짐, test.exe 에서 0
- 앞선 run 문서에 이미 본문이 있는 함수가 있으면 (예: KnownDlls 열기 — run 문서·flow.md 를 `grep`) 다시 길게 쓰지 말고 그 run 을 가리켜 줘

## 이번에 안 하는 것

- Q120c (ApiSet 위상 정정 — sast/60)
- `LdrpAllocateTlsEntry`·`LdrpAllocateTls` 깊은 본문 (질문 6 의 인자 확인에 필요한 만큼만)
- `RtlRbInsertNodeEx` 회전·색 다시 칠하기 세부
- 힙 관리자 본문 (run46 몫)

## 받고 싶은 것

- 질문별 답 + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋 (`reference.md` "오프셋 적는 법"). 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- **syscall 표**: 이름 · 번호 (스텁) · 클래스 · 부르는 자리 (함수+call/ret) · 조건 · test.exe 기본 길에서 횟수
- PE_emulator 가 그대로 쓸 **작은 표**: 함수 × (인자 · 쓰는 칸 · 하위 호출 · syscall · 반환)
- 클래스 `0xfa` 출력 칸 표 + nt 가 줄 값 (정적으로 정해지는 것만)
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 동적 몫은 Q104 VM 단계에 얹을 멈춤점·읽을 칸으로
- 짐 지도 반영거리 (baggage 칸·전역 줄)

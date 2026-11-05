# 61 apphelp-cache — 정적 분석 요청

- 날짜: 2026-11-05
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run66
- 대기열: **Q125** (PE_emulator 요청 13 의 8번 — 원문 "급함 높음, PE_emulator B (kernel32 올리기) 가 여기서 막힘". 사용자 결정 2026-11-05: 맨 앞, Q104 VM 단계 앞)
- 관련: run21 (`LdrpPrepareModuleForExecution +0x76/+0x7b LdrpNotifyLoadOfGraph`), run27 (`LdrpNotifyLoadOfGraph +0x19 State = 7`, `+0x29` 실패 시 6), run36 (`LdrpSnapModule +0xe29`·`+0xe44 CompatCachepLookupCdb` — 못 찾음 오류 처리 안에서만), run59 사진 (`g_pfnApphelpCheckModuleProc`·`g_pfnSE_*` 끝까지 0 — shim 엔진 안 올라옴), sast/51 (syscall 인자 표 — `0x4c` 인자 4, 기계 추출)
- 원문 (읽기만): `../PE_emulator/_workspace/00_planner/88_kernel32-load.md` (198 ~ 283줄 쯤 "`NtApphelpCacheControl` — 근거 찾기", "U1")

## 무슨 일이 있었나

- PE_emulator 가 kernel32 를 `LdrLoadDll(0x4001, 0, "KERNEL32.DLL")` 로 올렸더니, kernel32·KernelBase 를 다 올린 뒤 이 길에서 약속 밖 syscall 로 멈췄다 (PE_emulator 88 실측 — 우리 기본 대답 아래라 진짜와 다를 수 있음):
  - `LdrpPrepareModuleForExecution +0x76` → `LdrpNotifyLoadOfGraph +0x61` (재귀) / `+0x20` → `LdrpSendPostSnapNotifications +0xbb` (ret) → `LdrpCheckModule +0x25` (ret) → `CompatCachepLookupCdb +0xb9` (ret) → **`NtApphelpCacheControl(0x6, …)`** (syscall 번호 `0x4c`). 출력 버퍼 `RDX` 는 스택
- PE_emulator 는 근거 없이 이 syscall 을 약속에 넣지 않는다 (81 R1 규칙). 근거가 될 asm 이 internals 에 없다 (`.c`·`.asm` 모두 없음, 색인 이름·크기만).
- PE_emulator planner: 진짜도 kernelbase·kernel32 노드마다 `LdrpSendPostSnapNotifications` 에 **들어간다** (kernel32 State 5). exe import 길 (`LdrpInitializeProcess +0x1aed` 쪽) 도 같은 벽.
- 걱정 (원문 U1): 기본 대답 (성공, 출력 안 씀) 을 주면 `CompatCachepLookupCdb` 가 안 쓴 출력 버퍼 (스택 쓰레기) 를 읽고 엉뚱한 갈래 (shim 등) 로 갈 수 있다.

## 알고 싶은 것

1. **`LdrpSendPostSnapNotifications` (ntdll rva `0xc240`, 321B)** — 인자 (노드), `+0xb6/+0xbb LdrpCheckModule` 을 부르는 **조건** (노드 칸·전역·PEB 비트 — 예: `g_pfnApphelpCheckModuleProc`, `ShimData`, 노드 `Flags`). 건너뛰는 갈래가 있나. `+0xa3 SbUpdateSwitchContextBasedOnDll` · `+0xc1 LdrpSendDllNotifications` · `+0x122` 간접(CFG) · `+0x130 AVrfDllLoadNotification` 의 조건도 한 줄씩 (test.exe 에서 타나)
2. **`LdrpCheckModule` (rva `0xcf50`, 339B)** — 인자, `+0x20/+0x25 CompatCachepLookupCdb` 가 거의 첫 일인데 그 앞에 조건이 있나. `CompatCachepLookupCdb` 의 반환·출력을 어떻게 읽어 `+0x7e RtlAllocateHeap` · `RtlCopyMemory` ×3 · `+0x14a RtlFreeHeap` 갈래를 고르나. 무엇을 만들어 어디에 놓나 (전역·노드 칸)
3. **`CompatCachepLookupCdb` (rva `0xc144`, 244B)** — 인자 (이름 문자열? `+0x3d wcsrchr` 로 파일 이름만 떼나), `+0x29 memset` 으로 비우는 버퍼 (크기·자리 — **출력 버퍼를 syscall 전에 비우나**: U1 걱정과 직결), `+0xb4/+0xb9 NtApphelpCacheControl` 인자 (클래스 `0x6`, 버퍼 꼴·크기 — 인자 수 2 인가 4 인가), 호출 전 조건 (건너뛰는 갈래), 돌아온 상태·출력을 어떻게 읽어 무엇을 돌려주나
   - caller 다섯 (`LdrpCheckModule +0x20`, `LdrpSnapModule +0xe29`·`+0xe44`, `LdrpLogDeprecatedDllEtwEvent +0x5c`, `LdrpCheckComponentOnDemandEtwEvent +0x56`) 이 넘기는 인자 차이 (이름만 짧게)
4. **ntoskrnl `NtApphelpCacheControl` (rva `0x9f46c0`, 437B)** — 클래스 `0x6` 갈래: 이름 (PDB 열거 `_APPHELPCACHESERVICECLASS` 등 있으면), 입력 버퍼 꼴 (칸 오프셋·크기·뜻), 크기 검사, 하위 호출 (`Ahc*` 함수 — 지도·`_calls.tsv` 로 찾아 그 갈래만 depth 1), **캐시가 비었거나 못 찾을 때 돌려주는 상태값**, 출력 칸에 쓰는 것. test.exe 프로세스에서 kernel32·kernelbase 이름으로 물을 때 정적으로 정해지는 대답 (예: 캐시가 부팅 때 채워지는지 — 그렇다면 "동적 필요")
   - 다른 클래스는 번호·이름 표만
5. **노드마다 몇 번** — test.exe 초기화 (exe import 길 + kernel32 올리기) 에서 이 길이 노드마다 한 번인가, 몇 노드가 타나 (ntdll·exe·kernel32·kernelbase·sample dll1·dll2 — `LdrpNotifyLoadOfGraph` 재귀 `+0x61` 조건, `State` 값 조건). 정적으로 정해지는 만큼
6. **PE_emulator 에 줄 한 줄 결론**: 기본 대답 (성공·출력 안 씀) 이 안전한가 (출력 버퍼를 미리 비우나, 상태 실패를 어떻게 다루나), 아니면 진짜가 줄 상태값 (예: `STATUS_NOT_FOUND` 류) 을 줘야 하나

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^LdrpSendPostSnapNotifications$` | 1 | 1 |
| ntdll | `^LdrpCheckModule$` | 1 | 2 |
| ntdll | `^CompatCachepLookupCdb$` | 1 | 3 |
| ntdll | `^NtApphelpCacheControl$` (스텁) | 0 | 3 — syscall 번호 `0x4c` 확인 |
| ntoskrnl | `^NtApphelpCacheControl$` + 클래스 `0x6` 갈래 하위 (지도로 찾아 그것만) | 1 | 4 |
| ntdll | `^LdrpNotifyLoadOfGraph$`, `^LdrpPrepareModuleForExecution$` | 0 | 5 (`.asm` 있음 — 다시 뽑지 않음) |
| ntdll | (refs) 질문 1·2 에서 나오는 전역 (`g_pfnApphelpCheckModuleProc` 등) | — | 1·2 쓰는 곳·읽는 곳 |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where ntdll.dll <이름>` (ntoskrnl 은 `ntoskrnl.exe`).
- 공용 부품 (`RtlEnterCriticalSection`·`RtlAllocateHeap`·`RtlFreeHeap`·`RtlCopyMemory`·`memset`·`wcsrchr`·`wcslen`) 은 이름만. `SbUpdateSwitchContextBasedOnDll`·`LdrpSendDllNotifications`·`AVrfDllLoadNotification` 은 조건 한 줄·이름만.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- 깊이/caller: `LdrpPrepareModuleForExecution` 5/2 · `LdrpNotifyLoadOfGraph` 6/2 · `LdrpSendPostSnapNotifications` 7/1 · `CompatCachepLookupCdb` 7/4 · `LdrpCheckModule` 8/1
- 길: `LdrpPrepareModuleForExecution +0x76/+0x7b` → `LdrpNotifyLoadOfGraph +0x20/+0x25 LdrpSendPostSnapNotifications` (`+0x61/+0x66` 자기 재귀) → `+0xb6/+0xbb LdrpCheckModule` → `+0x20/+0x25 CompatCachepLookupCdb` → `+0xb4/+0xb9 NtApphelpCacheControl`
- `LdrpSendPostSnapNotifications` callee: `+0x4a RtlEnterCriticalSection` · `+0xa3 SbUpdateSwitchContextBasedOnDll` · `+0xb6 LdrpCheckModule` · `+0xc1 LdrpSendDllNotifications` · `+0xd8 LdrpLogDllState` · `+0xf2 RtlLeaveCriticalSection` · `+0x122` 간접(CFG) · `+0x130 AVrfDllLoadNotification`
- `LdrpCheckModule` callee: `+0x20 CompatCachepLookupCdb` · `+0x7e RtlAllocateHeap` · `+0xab`·`+0x112`·`+0x12f RtlCopyMemory` (별칭 셋 더 — 이름 믿지 말 것) · `+0xe5 LdrpLogInternal` · `+0x14a RtlFreeHeap`
- `CompatCachepLookupCdb` callee: `+0x29 memset` · `+0x3d wcsrchr` · `+0x6c wcslen` · `+0xb4 NtApphelpCacheControl`
- ntoskrnl `_index.tsv`: `NtApphelpCacheControl` `0x9f46c0` 437B (`.asm` 없음), `AhcCacheQueryHwId` `0x837f84` 449B (관련 있는지 모름)

## 이미 아는 것

- `[정적]` run36: `LdrpSnapModule` 의 `CompatCachepLookupCdb` 두 자리는 못 찾음 오류 처리 안에서만 (test.exe 기본 길에선 안 탐)
- `[정적]` run27: `LdrpNotifyLoadOfGraph +0x19` 가 노드 `State = 7`, 실패 시 `+0x29` 에서 6
- `[관찰]` run59 사진: `g_pfnApphelpCheckModuleProc`·`g_pfnSE_*` 끝까지 0
- `[관찰]` PE_emulator 88 (진짜 ntdll 코드를 Unicorn 에서 실행 — 26100.9457 VM 관찰 아님): 위 길에서 `NtApphelpCacheControl(0x6, …)`, 출력 `RDX` = 스택
- runs 전체에 `NtApphelpCacheControl` 은 한 줄도 없음 (VM 에서 본 적 없음)

## 이번에 안 하는 것

- Q126 (요청 13 의 1 ~ 7 — 커널 이미지 올리기 syscall 본문)
- shim 엔진 (`LdrpLoadShimEngine` 등) 본문 — 이름·조건만
- `NtApphelpCacheControl` 의 클래스 `0x6` 밖 갈래 본문

## 받고 싶은 것

- 질문별 답 (예/아니오 또는 이름) + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋. 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- **syscall 표**: `NtApphelpCacheControl` 번호·클래스·인자 수·인자 (입력·출력 버퍼 꼴)·조건·노드마다 횟수
- 클래스 `0x6` 입력·출력 칸 표 + 실패 상태값 표
- 6번 한 줄 결론 (PE_emulator 가 그대로 쓸 수 있게)
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 동적 몫은 Q104 VM 단계에 얹을 멈춤점·읽을 칸으로 (예: `ntdll!CompatCachepLookupCdb+0xb9` 에서 `r eax`·출력 버퍼, 히트 횟수)

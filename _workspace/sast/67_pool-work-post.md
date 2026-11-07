# 67 pool-work-post — 정적 분석 요청

- 날짜: 2026-11-07
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run72
- 대기열: **Q137** (PE_emulator 요청 23, 원문 급함 보통. 사용자 결정 2026-11-07: "현재 분석 끝나면 이 분석도" — Q136 다음, Q104 VM 단계 앞)
- 관련: run24 (일감 큐 `LdrpQueueWork`·`LdrpDrainWorkQueue`·`LdrpProcessWork`, 재시도 길 빈 곳), run38 (`LdrpMinimalMapModule +0x174 NtMapViewOfSection`, `0x40000003`·`0x40000036` → `ctx+0xb0 == 0 · LdrpMapAndSnapWork ≠ 0` 일 때만 `+0x2ae LdrpCheckForRetryLoading`, `LdrpCompleteMapModule +0x31b LdrpRelocateImage` 는 `0x40000003` 만), run69 (sast/64 — 로더 풀 syscall 14, `LdrpMapAndSnapWork` 일감 `0xf0`B, 핸들 0 은 기본 대답 탓), Q134 (e) (`LdrpWorkCallback`), Q20·Q23 (동적 — kernel32·dll1·dll2 올라오는 순간)
- 원문 (읽기만): `../PE_emulator/_workspace/00_planner/121_pool-notes.md` 0절·R1
- **같이 도는 작업**: sast/66 (Q136, kernelbase 위주) decompiler 가 돌 수 있다. Ghidra 를 열기 전 `pgrep -fa 'ghidra.app.util.headless'` 로 같은 모듈 (ntdll) 프로젝트를 쓰는 작업이 있는지 보고, 있으면 끝난 뒤에 (프로젝트 잠금)

## 무슨 일이 있었나

- PE_emulator 는 풀 syscall 을 기본 대답 (`STATUS_SUCCESS`, 출력 안 씀) 으로 두고, 일꾼 스레드 없이 첫 스레드가 `LdrpDrainWorkQueue(1)` 로 큐를 비운다 (run24).
- 진짜 `LdrpEnableParallelLoading` 덫 (`LdrpInitializeProcess +0x1a42`) 을 켜면 `LdrpMapAndSnapWork ≠ 0` → `LdrpQueueWork +0x7d TpPostWork` → `TpPostWork +0x3c` 꼬리 `TppWorkPost` 길이 열린다. 이 길의 syscall 약속과 기다림이 있는지 알아야 덫을 켤 수 있다.

## 알고 싶은 것

1. **`TppWorkPost`** (ntdll rva `0x39430`, 1346B)
   - 인자 (일감), 타는 갈래 — 특히 **일꾼 0 · 풀 핸들 0 (PE_emulator 기본 대답)** 일 때, 그리고 진짜 (핸들 ≠ 0, 일꾼 아직 0 — 처음 넣기) 일 때 각각
   - syscall 표 (지도: `+0x127 NtAlertThreadByThreadId` · `+0x224 NtTraceEvent` · `+0x4f3 NtReleaseWorkerFactoryWorker` 외 — 이름·조건·횟수). **`NtReleaseWorkerFactoryWorker` 가 처음 넣기에서 나오나** (커널이 일꾼을 새로 만드는 계기 `[추측]`)
   - 기다림 (`NtWaitFor*`·`NtRemoveIoCompletion`·`NtWaitForWorkViaWorkerFactory` 류) 이 이 함수 안에 있나 — 없으면 "넣기만 하고 돌아옴"
   - `+0x3ca` 간접(CFG) 호출 대상 후보, 쓰는 칸 (풀 큐·일감 상태)
   - 1346B — 처음 넣기 갈래 자세히, 나머지는 조건·이름만
2. **일감 ≠ 0 일 때 재시도 길** (run24 빈 곳) — syscall 표:
   - `LdrpMinimalMapModule +0x296/+0x2a9` 쯤 `LdrpCheckForRetryLoading` (rva `0xdf18`, `.asm` 있음 — run38 은 `+0x2ae` ret) 이 무엇을 보고 `0xc000022d` (`STATUS_RETRY`?) 를 돌려주나 (`RtlRbInsertNodeEx` 로 넣는 트리 이름)
   - 그 뒤 `NtUnmapViewOfSection` (자리 call/ret) → `LdrpDrainWorkQueue +0xeb ~ +0x134` (`.asm` 있음) 가 무엇을 하나 → `LdrpProcessWork +0x149/+0x14e LdrpMapDllRetry` (rva `0xebefc`, `.asm` 있음 — `+0x81 LdrpFindExistingModule` · `+0x9a LdrpGetNtPathFromDosPath` · `+0xad LdrpMapDllNtFileName` · `+0xf2 LdrpLoadContextReplaceModule`) 의 syscall (다시 열기·다시 맵)
   - test.exe 에서 이 길을 타나 — 조건 (`0x40000003`·`0x40000036`, `ctx+0xb0 == 0`, `LdrpMapAndSnapWork ≠ 0`) 중 무엇이 정해지고 무엇이 실행 때 정해지나
3. **dll1·dll2 이미지 맵 결과** — `STATUS_SUCCESS` (0) 인가 `STATUS_IMAGE_NOT_AT_BASE` (`0x40000003`) 인가. 정적 근거: sample `dll1.dll`·`dll2.dll` 헤더 (`ImageBase`·`DllCharacteristics` 의 `DYNAMIC_BASE`·재배치 디렉터리 유무 — 파일은 `sample/bin` 쪽, `ms/bin` 에 사본이 있으면 거기), 커널 `MiMapViewOfImageSection` 의 `0x40000003` 조건 (run38·Q126 (6) 과 이어지면 이름만), ASLR 이 켜진 DLL 은 보통 `0`·꺼진 DLL 이 자리 충돌이면 `0x40000003` `[추측]` — 정적으로 닫히면 닫고, 아니면 "동적 필요 (Q104 — Q20·Q23 과 같이)"
4. (선택) **`LdrpWorkCallback`** (rva `0xe9ef0`, 218B, `.asm` 있음) — 일꾼이 일감을 받으면 하는 일 (줄 요약·syscall 유무). Q134 (e) 와 같음

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^TppWorkPost$` | 1 | 1 |
| ntdll | `^TpPostWork$`, `^LdrpQueueWork$`, `^LdrpCheckForRetryLoading$`, `^LdrpDrainWorkQueue$`, `^LdrpProcessWork$`, `^LdrpMapDllRetry$`, `^LdrpMinimalMapModule$`, `^LdrpWorkCallback$` | 0 | 2·4 (`.asm` 있음 — 다시 뽑지 않음) |
| ntdll | `^LdrpMapDllNtFileName$` (재시도 길 syscall 확인에 필요한 만큼) | 0 | 2 |
| (파일) | sample `dll1.dll`·`dll2.dll`·`test.exe` PE 헤더 | — | 3 |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where ntdll.dll <이름>`.
- 공용 부품 (락·`RtlFreeHeap`·`memset`·`RtlRbInsertNodeEx` (run65)·`RtlCompareUnicodeStrings`) 은 이름만.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpQueueWork` (`0xe244c`, 136B): `+0x19 RtlEnterCriticalSection` · `+0x54 RtlLeaveCriticalSection` · `+0x7d/+0x82 TpPostWork`
- `TpPostWork` (`0xe24e0`, 65B): `+0x37` 꼬리 `TppRaiseInvalidParameter` · `+0x3c` 꼬리 `TppWorkPost`
- `TppWorkPost` (`0x39430`, 1346B, `.asm` 없음): `+0x71`·`+0x10d`·`+0x403`·`+0x43b RtlReleaseSRWLockExclusive` · `+0x127/+0x12c NtAlertThreadByThreadId` · `+0x224/+0x229 NtTraceEvent` · `+0x304 TppCleanupGroupMemberDestroy` · `+0x325 RtlFreeHeap` · `+0x3ca` 간접(CFG) · `+0x3d7 RtlpAcquireSRWLockExclusiveContended` · `+0x42a RtlAcquireSRWLockExclusive` · `+0x432 TppAdjustRunningThreadGoalWithLock` · `+0x4f3/+0x4f8 NtReleaseWorkerFactoryWorker` · `+0x50a TppFreeWait` · `+0x514 TppAlpcpFree`
- `LdrpCheckForRetryLoading` (`0xdf18`, 494B): `+0x49 RtlEnterCriticalSection` · `+0xa9 RtlCompareUnicodeStrings` · `+0xfb LdrpCompareModuleName` · `+0x14e RtlRbInsertNodeEx` · `+0x190 RtlLeaveCriticalSection`
- `LdrpMapDllRetry` (`0xebefc`, 248B): `+0x40 memset` · `+0x81 LdrpFindExistingModule` · `+0x9a LdrpGetNtPathFromDosPath` · `+0xad LdrpMapDllNtFileName` · `+0xc3 RtlDeleteBoundaryDescriptor` · `+0xf2 LdrpLoadContextReplaceModule`

## 이미 아는 것

- `[정적]` run24: 일감 큐 길, `LdrpProcessWork +0x149/+0x14e LdrpMapDllRetry` (`ctx+0x20` 비트 20)
- `[정적]` run38: 재시도 진입 조건 (위 "관련")
- `[정적]` run69: `LdrpMapAndSnapWork` = `TpAllocWork` 결과 (콜백 `LdrpWorkCallback`), 로더 풀 syscall 14
- `[관찰]` run44 사진: `LdrpMapAndSnapWork` P1b ≠ 0 (run69)

## 이번에 안 하는 것

- 커널 쪽 일꾼 공장 (`NtReleaseWorkerFactoryWorker`·`NtWaitForWorkViaWorkerFactory`) 본문 — Q134 (b) 몫
- 일꾼 스레드의 `TppWorkerThread` 전체 (이름만)

## 받고 싶은 것

- 질문별 답 + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋. 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- **syscall 표** 둘: `TppWorkPost` (기본 대답 갈래 / 진짜 처음 넣기 갈래), 재시도 길
- PE_emulator 한 줄: 덫을 켰을 때 `TppWorkPost` 에 무엇을 약속하면 되나 (기다림이 없으면 "넣기만"), 재시도 길을 타는지
- 3번 결론 (정적으로 닫힘 / 동적 필요)
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 동적 몫은 Q104 VM 단계에 얹을 멈춤점·읽을 칸으로
- 짐 지도 반영거리

# 64 loader-pool — 정적 분석 요청

- 날짜: 2026-11-07
- 스프린트: 03_sprint (짐 지도 바퀴) / 예정 run69
- 대기열: **Q132** (PE_emulator 요청 12 — 원문 "요청 11 묶음 7 과 같은 급 (정적, 작음)", 오케스트레이터 기본값 Q104 VM 단계 앞)
- 관련: run19 (`LdrpInitializeProcess +0x1a42/+0x1a47 LdrpEnableParallelLoading(ECX = ProcessParameters.LoaderThreads)` — 스레드 풀을 만드는 첫 자리), run62 (sast/57 — 기본 스레드 풀 `TpAllocPoolInternal`·`NtSetInformationWorkerFactory` 클래스 `0xd`, Q119 (a) `TpAllocPoolInternal +0x316` 갈래), run46 (힙)
- 원문 (읽기만): `../PE_emulator/_workspace/00_planner/81_parallel-loading.md`, `../PE_emulator/_workspace/orchestrator/81_parallel-loading.md`

## 무슨 일이 있었나

- PE_emulator 81 이 진짜 `LdrpEnableParallelLoading` (`LdrpInitializeProcess +0x1a3c ~ +0x1a47`) 을 Unicorn 안 덫으로 부른다. 덫 안 syscall 약속을 asm 으로 세는데, 아래 셋은 internals 에 본문 asm 이 없어 약속을 닫을 수 없다.
- PE_emulator 81 시도 1 실측 (우리 기본 대답 아래 — 진짜와 다를 수 있음): 덫 안 syscall 14번째가 `NtSetInformationWorkerFactory(핸들 0, 클래스 0x2, &buf, 8)`, 돌아갈 주소 `ntdll+0xe655e` = `TpSetPoolWorkerThreadIdleTimeout +0x3e`. 앞 13 개는 asm 으로 센 표와 같음. **핸들 0** 이 눈에 띈다 — 앞 `TpAllocPoolInternal` 이 워커 팩토리 핸들을 못 받았거나 (우리 대답), 진짜도 이 길에서 핸들이 없는 경우가 있는지
- `TpAllocWork` 는 거기까지 못 감

## 알고 싶은 것

1. **`TpSetPoolWorkerThreadIdleTimeout`** (ntdll rva `0xe6520`, 79B, 부르는 곳 `LdrpEnableParallelLoading +0x61/+0x66`)
   - 인자 (풀, 시간 값 — `LdrpEnableParallelLoading` 이 넘기는 값), `+0x39/+0x3e NtSetInformationWorkerFactory` 인자: 핸들은 **풀의 어느 칸**에서 읽나 (칸 오프셋), 클래스 `0x2` 이름 (`WorkerFactoryThreadMinimum`? — PDB 열거 `_WORKERFACTORYINFOCLASS` 가 있으면 이름), 버퍼 8B 내용, 그 밖에 풀에 쓰는 칸
   - `+0x44/+0x49 TppRaiseInvalidParameter` 로 가는 조건
   - syscall 실패를 어떻게 다루나 (반환 상태를 돌려주나, 무시하나) — PE_emulator 가 이 syscall 에 무엇을 돌려줘야 하나
   - 같이: `LdrpEnableParallelLoading` 안 `+0x44/+0x49 TpAllocPoolInternal` 이 만든 풀에서 워커 팩토리 핸들 칸이 언제 채워지나 (run62 `TpAllocPoolInternal` 결과와 이어서 — 로더 전용 풀은 기본 풀과 다른 인자로 만드나, 그때 `NtCreateWorkerFactory` 가 나오나). **핸들 0 이 진짜로 가능한 길인지**
2. **`TpAllocWork`** (ntdll rva `0x84900`, 377B, 부르는 곳 `LdrpEnableParallelLoading +0xdd/+0xe2`)
   - 인자 (콜백 = 무엇 — `LdrpWorkCallback`?, 문맥, 환경), `+0x90/+0x95 RtlAllocateHeap` 힙·크기·플래그, `+0xd5/+0xda TppWorkInitialize` 가 채우는 칸 (이름만 + 크기), `+0x104 TppCleanupGroupAddMember` 조건, syscall 이 있나 (`TppWorkInitialize` 아래까지 줄기 한 단계)
   - 결과를 어느 전역에 넣나 (`LdrpWorkQueue`·`LdrpLoaderWork`? — `LdrpEnableParallelLoading` 쪽에서)
3. **`RtlpHpGCTimerEnable`** (ntdll rva `0x11f5dc`, 337B, 부르는 곳 `_LdrpInitialize +0x27d/+0x282`, `LdrpThreadPool ≠ 0` 일 때)
   - 조건 (`_LdrpInitialize` 쪽 갈래 줄), `+0xa3/+0xa8 RtlAllocateHeap` 크기, `+0xea/+0xef TppInitializeTimer` (줄기 한 단계 — syscall: `NtCreateTimer2`·`NtSetTimer2`·`NtAssociateWaitCompletionPacket` 류가 나오나), 쓰는 전역 (힙 GC 타이머 전역 이름), 실패 갈래 (`+0x121 TpReleaseTimer`, `+0x12c TppRaiseInvalidParameter`)
   - test.exe 에서 타나 (`LdrpThreadPool` 은 `LdrpEnableParallelLoading` 이 채우나)
4. **`LdrpEnableParallelLoading` 덫 안 syscall 전체 표** (`.asm` 있음) — PE_emulator 가 센 앞 13 개와 이번 셋을 합쳐 순서대로 (`+0x1c LdrpDetectDetour` · `+0x29 RtlGetSuiteMask` · `+0x44 TpAllocPoolInternal` · `+0x61 TpSetPoolWorkerThreadIdleTimeout` · `+0x7a TpSetPoolMaxThreads` · `+0xdd TpAllocWork`). `TpSetPoolMaxThreads` 도 본문이 없으면 같이 (작으면)

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^TpSetPoolWorkerThreadIdleTimeout$`, `^TpSetPoolMaxThreads$` | 1 | 1·4 |
| ntdll | `^TpAllocWork$`, `^TppWorkInitialize$` | 1 | 2 |
| ntdll | `^RtlpHpGCTimerEnable$`, `^TppInitializeTimer$` | 1 | 3 |
| ntdll | `^LdrpEnableParallelLoading$`, `^TpAllocPoolInternal$`, `^_LdrpInitialize$` | 0 | 1·3·4 (`.asm` 있으면 다시 뽑지 않음) |

- 이름은 `_index.tsv` 로, 없으면 `pdb_aliases.py --where ntdll.dll <이름>`.
- 공용 부품 (`RtlAllocateHeap`·`RtlFreeHeap`·락·`TppRaiseInvalidParameter`·`TppCleanupGroupAddMember`) 은 이름만.

## 지도에서 본 것 (`ms/disassemble/ntdll/_map/LdrInitializeThunk_*`)

- `LdrpInitializeProcess +0x1a42/+0x1a47 LdrpEnableParallelLoading` (rva `0xe572c`, 265B, `.asm` 있음)
- `LdrpEnableParallelLoading` callee: `+0x1c/+0x21 LdrpDetectDetour` · `+0x29/+0x2e RtlGetSuiteMask` · `+0x44/+0x49 TpAllocPoolInternal` · `+0x61/+0x66 TpSetPoolWorkerThreadIdleTimeout` · `+0x7a/+0x7f TpSetPoolMaxThreads` · `+0xdd/+0xe2 TpAllocWork`
- `TpSetPoolWorkerThreadIdleTimeout` (`0xe6520`, 79B): `+0x39/+0x3e NtSetInformationWorkerFactory` · `+0x44/+0x49 TppRaiseInvalidParameter`
- `TpAllocWork` (`0x84900`, 377B): `+0x90/+0x95 RtlAllocateHeap` · `+0xd5/+0xda TppWorkInitialize` · `+0x104/+0x109 TppCleanupGroupAddMember` · `+0x137 TppRaiseInvalidParameter` · `+0x169 RtlFreeHeap` · SEH 끝 블록 `RtlFreeHeap`
- `RtlpHpGCTimerEnable` (`0x11f5dc`, 337B): `+0xa3/+0xa8 RtlAllocateHeap` · `+0xea/+0xef TppInitializeTimer` · `+0x121/+0x126 TpReleaseTimer` · `+0x12c/+0x131 TppRaiseInvalidParameter`
- `_LdrpInitialize +0x27d/+0x282 RtlpHpGCTimerEnable`

## 이미 아는 것

- `[정적]` run19: `LdrpEnableParallelLoading(ECX = ProcessParameters.LoaderThreads)`, 스레드 풀을 만드는 첫 자리
- `[정적]` run62: 기본 스레드 풀 (`LdrpEnableUMGLTracingStateSync` 길) 의 `TpAllocPoolInternal` 본문·syscall
- `[관찰]` PE_emulator 81 (진짜 ntdll 코드를 Unicorn 에서 실행 — 26100.9457 VM 관찰 아님): 위 "무슨 일이 있었나"

## 이번에 안 하는 것

- 로더 일감 콜백 (`LdrpWorkCallback`) 본문, 워커 스레드 쪽
- 커널 `NtSetInformationWorkerFactory`·`NtCreateWorkerFactory` 본문 (클래스 이름·버퍼 꼴만 필요하면 PDB 로)

## 받고 싶은 것

- 질문별 답 + 근거 `+0x` 줄
  - **오프셋 적는 법**: 부르기는 call/ret 둘 다, 점프 목적지는 **목적지 절대 주소 − 함수 시작 주소**, 명령 하나는 그 명령 줄의 오프셋. 결과서 쓰기 전에 `.asm` 줄과 다시 대조
- **syscall 표** (질문 4): 이름 · 클래스 · 부르는 자리 (함수+call/ret) · 조건 · test.exe 기본 길에서 횟수
- PE_emulator 가 그대로 쓸 **작은 표**: 함수 × (인자 · 쓰는 칸 · 하위 호출 · syscall · 반환)
- 1번 "핸들 0" 한 줄 결론
- 질문마다 **"정적으로 닫힘 / 동적 필요"**. 동적 몫은 Q104 VM 단계에 얹을 멈춤점·읽을 칸으로
- 짐 지도 반영거리 (풀·일감·타이머 전역, 힙 블록)

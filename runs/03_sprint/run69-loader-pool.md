# 2026-11-07 run69 — 로더 전용 스레드 풀 (Q132, PE_emulator 요청 12): **풀 핸들 0 은 진짜 길에선 못 나온다** (만들기가 실패하면 풀째 버려서 여기 안 옴), `TpSetPoolWorkerThreadIdleTimeout` 은 asm 순서로 덫 안 **13번째** syscall, 은퇴 앞 `_LdrpInitialize +0x27d RtlpHpGCTimerEnable` 은 **힙 GC 타이머를 만들기만** 하고 syscall 1번 (클래스 3)

## PE_emulator 쪽에서 쓸 것

### 한 줄 결론 (넷)

1. **"핸들 0" 은 진짜 코드 길에선 못 나온다** `[정적]`.
   - 핸들 칸 = 풀 `+0x38` (PDB `_TP_POOL.WorkerFactory`). `TpAllocPoolInternal +0x303/+0x308 NtCreateWorkerFactory` 가 이 칸에 직접 쓴다 (`+0x2cb LEA RSI,[RDI+0x38]` → `+0x300 MOV RCX,RSI`).
   - 이 syscall 이 음수면 `+0x310 JS` → `+0x465` → 풀을 통째로 버리고 음수를 돌려준다. 출력 칸 `LdrpThreadPool` 은 `+0x28` 에서 0 으로 지운 뒤, 성공이 확정된 `+0x4be` 에서만 풀 주소를 받는다.
   - 그러면 `LdrpEnableParallelLoading +0x4d JS` 가 `+0xe4` 로 뛰어서 `TpSetPoolWorkerThreadIdleTimeout` 에 아예 안 온다. 즉 **여기 닿는 풀은 핸들이 방금 진짜로 만들어진 풀뿐**이다.
   - PE_emulator 의 0 은 **기본 대답 탓**이다: `NtCreateWorkerFactory` 에 "성공" 만 주고 핸들 칸을 안 써 줘서, 힙을 0 으로 채워 받은 풀 (`HEAP_ZERO_MEMORY`) 의 `+0x38` 이 0 그대로 남았다.
2. **PE_emulator 가 줄 대답** (결과서 1-7):
   - `TpAllocPoolInternal` 안 **`NtCreateWorkerFactory` 에는 핸들 칸 (`RCX` = `&풀+0x38`) 에 0 아닌 값을 써 줄 것.**
   - **`TpAllocPoolInternal +0x443/+0x448` 의 `NtSetInformationWorkerFactory` (클래스 `0xd`) 에는 0 이상 상태를 줄 것** — 이 하나만 상태를 본다 (`+0x44e TEST` · `+0x450 JS +0x465` → 풀 폐기 → `LdrpThreadPool` = 0 → 그 뒤 길 전체가 달라짐).
   - 클래스 2 (`IdleTimeout`) · 5 (`MaxThreads`) · 3 (`TppInitializeTimer`) 는 상태를 아무도 안 본다 → 아무 값이나 (권고 `STATUS_SUCCESS`, 출력 쓰기 없음).
   - 그래서 핸들 0 으로 가도 이번 로더 길 자체는 안 깨진다. 핸들 칸을 채우는 건 "진짜와 같게" 하려는 것이다.
3. **asm 순서로는 `IdleTimeout` 이 13번째다** `[정적]` (아래 syscall 표). PE_emulator 가 14번째로 봤다면 **그 앞에 표 밖 syscall 하나가 끼었다**는 뜻이다.
   - 후보 1순위: 힙이 자랄 때의 `NtAllocateVirtualMemory` (공용 부품 `RtlAllocateHeap` 안 — 표 밖).
   - 후보 2: `TppPoolUpdateNodeRelation` 이 `NtQuerySystemInformationEx` 를 3번 부름 (`+0x56 CMP EAX,0xc0000004` 가 또 맞아서 `+0xbd JMP` → `+0x23` 로 되돌아감).
   - **PE_emulator 가 앞 13개 이름을 아래 표 1 ~ 12 와 대조하면 가려진다.**
4. **`RtlpHpGCTimerEnable` 은 `LdrpInitializeProcess` 가 끝난 뒤, `LdrpThreadPool ≠ 0` 일 때 한 번 탄다** (`_LdrpInitialize +0x215 ~ +0x21f` → `+0x27d/+0x282`, 그다음 `+0x24d/+0x252 LdrpInitializationComplete`).
   - 하는 일: 힙 GC 타이머 개체 (`_TP_TIMER` `0x168`B, 프로세스 힙) 를 **만들기만** 하고 전역 `ntdll+0x1ce270` 에 꽂는다. **켜지 않는다** (`NtSetTimer2` 류 없음).
   - syscall 은 1번: `TppInitializeTimer +0xcb/+0xd0 NtSetInformationWorkerFactory(풀+0x38, 클래스 3, &1, 4)` — 로더 풀 `BindingCount` 옛 값이 0 이라서.
   - `[관찰]` run44 사진이 받쳐 준다: `0x1ce270` 이 P1b 0 → **P2 `0x25996f43210`** (아래 "사진과 맞춰 본 것").

### syscall 표 — `LdrpEnableParallelLoading` 덫 안 (test.exe 기본 길, 결과서 4번)

기본 길 가정: `LoaderThreads` 0 · `RtlGetSuiteMask` bit 16 꺼짐 · `LdrpDetourExist` 0 · `ActiveProcessorCount` 2 · `TppHeapTag` 0 · `TppPoolpDefaultPoolCpuSetCount` 0 · KUSD `+0x386` 0 (뒤 넷은 sast/57 `[관찰]`).

| # | 부르는 자리 (함수 + call/ret) | syscall | 클래스·인자 | 조건 |
|---|---|---|---|---|
| 1 | `+0x1c/+0x21` → `LdrpDetectDetour +0x9c/+0xa1` | `NtQueryInformationThread` | 핸들 `-2`, 클래스 `0x2a`, 버퍼 4B, 길이 4 | `LdrpDetourExist` 0 (`+0x8` · `+0x12 JNZ`) |
| 2 ~ 3 | `+0x44/+0x49` → `TpAllocPoolInternal +0x240/+0x245` → `TppPoolUpdateNodeRelation +0x4b/+0x50` | `NtQuerySystemInformationEx` | 클래스 `0x6b`, 입력 값 6 | 첫 대답 `0xc0000004` 면 한 번 더 |
| 4 | 〃 `+0x292/+0x297` | `NtCreateIoCompletion` | 출력 `&풀+0x40`, 접근 `0x1f0003`, 동시 수 `max(CPU+1, 4)` = 4 | 늘 |
| 5 | 〃 `+0x303/+0x308` | `NtCreateWorkerFactory` | 출력 `&풀+0x38`, 접근 `0xf00ff`, 포트 `[풀+0x40]`, 프로세스 `-1`, 시작 `0x18001a2e0` (`TppWorkerThread`), 매개변수 풀, 최대 `max(CPU×4, 0x200)` = `0x200`, 스택 = exe 헤더 예약/확정 | 늘 |
| 6 | 〃 `+0x443/+0x448` | `NtSetInformationWorkerFactory` | **클래스 `0xd`**, 4B, 값 1 | 둘째 인자 bit0 = 1 — **로더 풀만** |
| 7 ~ 12 | 〃 `+0x38e/+0x393` → `TppInitializeTimerQueue` → `TppInitializeTimerSubQueue` | `NtCreateTimer2` → `NtCreateWaitCompletionPacket` → `NtAssociateWaitCompletionPacket` | 큐 둘 (절대·상대) 각각 | 늘 |
| **13** | `+0x61/+0x66` → `TpSetPoolWorkerThreadIdleTimeout +0x39/+0x3e` | `NtSetInformationWorkerFactory` | **클래스 2**, 8B, −300000000 | 오류 갈래 아님 |
| 14 | `+0x7a/+0x7f` → `TpSetPoolMaxThreads +0x3b/+0x40` | `NtSetInformationWorkerFactory` | **클래스 5**, 4B, 값 3 | 오류 갈래 아님 |
| — | `+0xdd/+0xe2` → `TpAllocWork` (`TppWorkInitialize` 까지) | 없음 | — | — |
| (15) | **덫 밖** `_LdrpInitialize +0x27d/+0x282` → `RtlpHpGCTimerEnable +0xea/+0xef` → `TppInitializeTimer +0xcb/+0xd0` | `NtSetInformationWorkerFactory` | **클래스 3**, 4B, 값 1 | 풀 `BindingCount` 옛 값 0 |

- 덫 안 합계 **14** (+ 표 밖 힙 확장 `NtAllocateVirtualMemory` 몇 번, 동적).
- 커널 `NtSetInformationWorkerFactory` 가 요구하는 버퍼 길이 `[정적]` (`ms/decomp/ntoskrnl/NtSetInformationWorkerFactory.c`): 클래스 2 = 8 · 3·4·5·8·9·`0xb`·`0xc`·`0xd`·`0xe` = 4 · `0xa` = `0x10` · `0xf` = 가변 (최대 `0x100`). 위 호출 넷의 길이는 모두 맞다.
- 클래스 이름은 **PDB 에 없다** (ntdll·ntoskrnl 둘 다 `_WORKERFACTORYINFOCLASS` 열거 없음). 함수 이름으로 2 = 유휴 시간 제한, 5 = 최대 스레드 `[추측]`. 클래스 `0xd` 는 값 bit0 를 커널 일꾼 공장 `+0x198` 의 bit 11 (`0x800`) 에 복사한다 `[정적]` — 그 bit 의 뜻은 `[공백]`.

### 작은 표 (결과서 표를 줄임, rva 는 ntdll 기준)

| 함수 (rva) | 인자 | 쓰는 칸 | syscall | 반환 |
|---|---|---|---|---|
| `TpSetPoolWorkerThreadIdleTimeout` (`0xe6520`, 79B) | `RCX` 풀, `RDX` INT64 (< 0) | 없음 (스택 8B 버퍼만) | `NtSetInformationWorkerFactory(풀+0x38, 2, &값, 8)` ×1 | syscall 상태 그대로 (오류 갈래면 `0xc000000d`). 부르는 쪽은 안 봄 |
| `TpSetPoolMaxThreads` (`0xe6490`) | `RCX` 풀, `EDX` 개수 (≥ 0) | 없음 | `(풀+0x38, 5, &개수, 4)` ×1 | 없음 |
| `TpAllocWork` (`0x84900`, 377B) | `RCX` 출력, `RDX` 콜백, `R8` 문맥, `R9` 환경 V3 | 일감 `0xf0`B 힙 블록, 풀 `Refcount`(`+0x0`)·`PoolObjectList`(`+0x50`), `*출력` | 없음 | 0 / `0xc0000017` / `0xc000000d` / 하위 음수 |
| `RtlpHpGCTimerEnable` (`0x11f5dc`, 337B) | `RDX` 풀 (`RCX` 안 씀) | 타이머 `0x168`B 힙 블록, 풀 `Refcount`·`PoolObjectList`·`BindingCount`(`+0x1b0`), 전역 `0x1ce270` | `(풀+0x38, 3, &1, 4)` ×1 | 0 / `0xff` / `0xc0000017` / `0xc000000d` / 음수. 부르는 쪽은 안 봄 |
| `TpAllocPoolInternal` (`0xe5a00`) | `RCX` 출력 (`&LdrpThreadPool`), `EDX` 1 (로더) / 0 (기본) | 풀 `0x1d8`B (`+0x38`·`+0x40` 핸들, `+0x17a PoolFlags` = 1 (로더만), `+0x1a8` = `ActiveProcessorCount`, `+0x1ac` = `0xfffffffe`, `+0x180 PoolLinks` → `TppPoolpList`), 노드·큐·NUMA 배열 블록 3, `*출력` | 표 2 ~ 12 (11) | 0 이상 / 음수 (`*출력` = 0) |
| `LdrpEnableParallelLoading` (`0xe572c`, 265B) | `ECX` `LoaderThreads` | `LdrpThreadPool`·`LdrpMapAndSnapWork` (주소 넘김) | 표 1 ~ 14 | `TpAllocWork` 결과 (불렸을 때), 아니면 `TpAllocPoolInternal` 결과. `LdrpInitializeProcess` 는 안 봄 |

오프셋 읽는 법 (run60 ~ run68 과 같음):

- `call +0x..` = CALL 명령 자리, `ret +0x..` = 그 다음 줄 (WinDbg 콜스택의 `caller+0x..`), 그냥 `+0x..` = 그 명령 자리.
- 점프 목적지는 `.asm` 의 점프 주소에서 **함수 시작 주소를 뺀** 값.

## 환경

- 26100.9457, 정적만 (VM 안 씀)
- 정적: ntdll pdb GUID `C3093720177DA6851519DCFFE8DF53FE1` (`ms/sym/ntdll.dll.pdb.info.txt`). 새로 analyze 안 함. 커널은 기존 `.gpr` 에서 `NtSetInformationWorkerFactory` 하나만 뽑음 (기준 GUID `C29EBFB06B78B3C020DCA66D99713F9E1`, sast/63 에서 확인).
- 버전 확인 (결과서): `_LdrpInitialize +0x167/+0x16c` · `LdrpInitializeProcess +0x1bb6/+0x1bbb` · `LdrInitializeThunk +0x9/+0xe` 일치, 새로 뽑은 함수 크기도 요청서와 같음 (79·377·337B).
- 값 대조: run44 덤프 읽기 로그 셋 (`ms/logs/04_baggage-read_P1a.txt`·`_P1b.txt`·`_P2.txt`) 의 ntdll `.data` 줄을 flow-writer 가 다시 읽었다. 새 동적 실행은 없다.

## 목적

- PE_emulator 81 이 진짜 `LdrpEnableParallelLoading` 을 Unicorn 덫 안에서 돌리다가 덫 안 14번째 syscall 이 `NtSetInformationWorkerFactory(핸들 0, 클래스 2, &buf, 8)` (돌아갈 곳 `ntdll+0xe655e` = `TpSetPoolWorkerThreadIdleTimeout +0x3e`) 인 것을 봤다. 핸들 0 이 진짜로 가능한 길인지 모른다.
- 알고 싶은 것: (1) `TpSetPoolWorkerThreadIdleTimeout` 인자·핸들 칸·클래스·실패 처리, 로더 풀의 핸들 칸이 언제 채워지나 (2) `TpAllocWork` 인자·할당·결과 전역 (3) `RtlpHpGCTimerEnable` 조건·할당·syscall·전역 (4) 덫 안 syscall 전체 표.

## 방법

- 정적: `_workspace/sast/64_loader-pool.md` — ntdll `TpSetPoolWorkerThreadIdleTimeout`·`TpSetPoolMaxThreads`·`TpAllocWork`·`TppWorkInitialize`·`RtlpHpGCTimerEnable`·`TppInitializeTimer` depth 1, `LdrpEnableParallelLoading`·`TpAllocPoolInternal`·`_LdrpInitialize` 는 있던 `.asm` 그대로.
  - 결과서가 넓힌 것: 커널 `NtSetInformationWorkerFactory` (depth 0, 클래스별 버퍼 길이만), ntdll `RtlpHpGCTimerSchedule`·`RtlpHpGCTimerCallback` (depth 0, 합쳐 163B — GC 타이머 전역을 쓰고 읽는 곳), `_refs` 넷 (`LdrpThreadPool`·`LdrpMapAndSnapWork`·`0x1ce270`·`0x1ce278`·`0x1ce280`·`TpAllocPoolInternal`).
- 결과서: `_workspace/sast/64_loader-pool_result.md` (**분석성공**).
- 오케스트레이터가 `.asm` 으로 한 번 더 본 줄: `TpSetPoolWorkerThreadIdleTimeout +0x9/+0xc JZ +0x44` · `+0xe/+0x11 JNS +0x44` · `+0x13 ~ +0x24` · `+0x26 MOV RCX,[RCX+0x38]` · `+0x2f MOV R9D,8` · `+0x35 LEA EDX,[R9-6]` · `+0x39/+0x3e`; `LdrpEnableParallelLoading +0x38 MOV EDX,1` · `+0x3d` · `+0x44/+0x49` · `+0x4d JS +0xe4` · `+0x5a MOV RDX,-0x11e1a300` · `+0x61/+0x66`.
- flow-writer 가 `.asm` 으로 한 번 더 본 줄:
  - `LdrpEnableParallelLoading` 전체 (265B) — `+0x21 ~ +0x35` (스레드 수), `+0x66 ~ +0x70` (최대 스레드 = `EBX > 1` 이면 `EBX − 1`, 아니면 1), `+0x7f ~ +0x8b`, `+0x8d ~ +0xd6` (환경), `+0xf9 ~ +0x105`.
  - `TpSetPoolWorkerThreadIdleTimeout` 전체 (79B), `TpSetPoolMaxThreads +0x0 ~ +0x86`.
  - `TpAllocPoolInternal +0x16 ~ +0x32` · `+0x228`·`+0x237` · `+0x240/+0x245` · `+0x263`·`+0x273` · `+0x292/+0x297`·`+0x29f` · `+0x2cb` · `+0x300` · `+0x303/+0x308 ~ +0x31e` · `+0x324 ~ +0x3a4` · `+0x3a8 ~ +0x3ee` (`TppPoolpList` 꼬리 잇기) · `+0x3f3 ~ +0x42c` · `+0x42e ~ +0x460` · `+0x465 ~ +0x4be`.
  - `_LdrpInitialize +0x167/+0x16c` · `+0x172`·`+0x174` · `+0x1e3` · `+0x205` · `+0x20f` · `+0x215` · `+0x21c` · `+0x21f` · `+0x221 ~ +0x24d/+0x252` · `+0x27d/+0x282`.
  - `RtlpHpGCTimerEnable +0x24 ~ +0x35` · `+0x52 ~ +0x7b` · `+0x8a`·`+0x90`·`+0x9c`·`+0xa3/+0xa8`·`+0xb0` · `+0xb7`·`+0xbf` · `+0xd4` · `+0xea/+0xef ~ +0x136`.
  - `TppInitializeTimer +0x4b ~ +0x64` · `+0xac ~ +0xb0` · `+0xc7` · `+0xcb/+0xd0`. `TpAllocWork +0x6e`·`+0x7a`·`+0x7d ~ +0x95` · `+0xa3 ~ +0xaa` · `+0xd5/+0xda` · `+0xe4` · `+0xfb ~ +0x109` · `+0x118`.
  - `RtlpHpGCTimerSchedule +0x6`·`+0x14`·`+0x1b`·`+0x31`·`+0x42`·`+0x48`·`+0x52/+0x57`, `RtlpHpGCTimerCallback +0x29`.
  - `LdrpDetectDetour +0x0 ~ +0xba` (아래 "보탠 것"), `LdrpLoadDllInternal +0x1a1 ~ +0x1bb/+0x1c0`, `TppPoolpReferenceGlobalPool +0xbb`·`+0xc2/+0xc7`.
- 로그: 없음 (정적만). 값 대조만 run44 덤프 읽기 로그.

## 결과

### 정적 — 호출 지점 (`_calls.tsv`·`.asm`, 길 순서)

| caller | call | ret (= 콜스택 오프셋) | callee | 조건 | 표시 |
|---|---|---|---|---|---|
| `ntdll!LdrpInitializeProcess` | `+0x1a42` | `+0x1a47` | `ntdll!LdrpEnableParallelLoading(ECX = LoaderThreads)` | 늘 (run19) | [정적] |
| `ntdll!LdrpEnableParallelLoading` | `+0x1c` | `+0x21` | `ntdll!LdrpDetectDetour` | 늘 | [정적] |
| `ntdll!LdrpEnableParallelLoading` | `+0x29` | `+0x2e` | `ntdll!RtlGetSuiteMask` | `LoaderThreads` = 0 (`+0x23 JNZ +0xf9` 안 탐) | [정적] |
| `ntdll!LdrpEnableParallelLoading` | `+0x44` | `+0x49` | `ntdll!TpAllocPoolInternal(&LdrpThreadPool, 1)` | 늘 | [정적] |
| `ntdll!TpAllocPoolInternal` | `+0x303` | `+0x308` | `ntdll!NtCreateWorkerFactory(&풀+0x38, …)` | 늘 (실패면 `+0x310 JS +0x465`) | [정적] |
| `ntdll!TpAllocPoolInternal` | `+0x443` | `+0x448` | `ntdll!NtSetInformationWorkerFactory(핸들, 0xd, &1, 4)` | `[RSP+0xb8]` (= 둘째 인자 `& 1`) ≠ 0 (`+0x316` · `+0x31e JNZ +0x42e`) — **로더 풀만** | [정적] |
| `ntdll!LdrpEnableParallelLoading` | `+0x61` | `+0x66` | `ntdll!TpSetPoolWorkerThreadIdleTimeout(풀, −300000000)` | `+0x4d JS +0xe4` 안 탐 | [정적] |
| `ntdll!TpSetPoolWorkerThreadIdleTimeout` | `+0x39` | `+0x3e` | `ntdll!NtSetInformationWorkerFactory(풀+0x38, 2, &값, 8)` | 풀 ≠ 0 · 값 < 0 · 종료 중 아님 | [정적] |
| `ntdll!TpSetPoolWorkerThreadIdleTimeout` | `+0x44` | `+0x49` | `ntdll!TppRaiseInvalidParameter` | `+0xc` / `+0x11` / `+0x24` 중 하나 — 로더 값으로는 안 탐 | [정적] |
| `ntdll!LdrpEnableParallelLoading` | `+0x7a` | `+0x7f` | `ntdll!TpSetPoolMaxThreads(풀, 3)` | 위와 같음 | [정적] |
| `ntdll!TpSetPoolMaxThreads` | `+0x3b` | `+0x40` | `ntdll!NtSetInformationWorkerFactory(풀+0x38, 5, &개수, 4)` | 풀 ≠ 0 · 개수 ≥ 0 · 종료 중 아님 | [정적] |
| `ntdll!LdrpEnableParallelLoading` | `+0xdd` | `+0xe2` | `ntdll!TpAllocWork(&LdrpMapAndSnapWork, LdrpWorkCallback, 0, &환경)` | 스레드 수 > 1 (`+0x82 JBE`) · `LdrpDetourExist` 0 (`+0x8b JNZ`) | [정적] |
| `ntdll!TpAllocWork` | `+0x90` | `+0x95` | `ntdll!RtlAllocateHeap(ProcessHeap, TppHeapTag + 0x200000 \| 8, 0xf0)` | 늘 | [정적] |
| `ntdll!TpAllocWork` | `+0xd5` | `+0xda` | `ntdll!TppWorkInitialize` | 할당 성공 | [정적] |
| `ntdll!TpAllocWork` | `+0x104` | `+0x109` | `ntdll!TppCleanupGroupAddMember` | `CleanupGroup` ≠ 0 — 로더 환경은 0, 안 탐 | [정적] |
| `ntdll!_LdrpInitialize` | `+0x27d` | `+0x282` | `ntdll!RtlpHpGCTimerEnable(RDX = LdrpThreadPool)` | `LdrpThreadPool` ≠ 0 (`+0x215` · `+0x21c` · `+0x21f JNZ`) — `LdrpInitializeProcess` 뒤 | [정적] · [관찰] 사진 칸 값 |
| `ntdll!RtlpHpGCTimerEnable` | `+0xa3` | `+0xa8` | `ntdll!RtlAllocateHeap(ProcessHeap, TppHeapTag + 0x100000 \| 8, 0x168)` | 타이머 칸 `0x1ce270` = 0 · 종료 중 아님 | [정적] |
| `ntdll!RtlpHpGCTimerEnable` | `+0xea` | `+0xef` | `ntdll!TppInitializeTimer(타이머, 0, &0x1ce270, &환경, …)` | 할당 성공 | [정적] |
| `ntdll!TppInitializeTimer` | `+0xcb` | `+0xd0` | `ntdll!NtSetInformationWorkerFactory(풀+0x38, 3, &1, 4)` | 풀 `BindingCount` 옛 값 0 (`+0x57 XADD` · `+0x64` · `+0xae`) | [정적] |
| `ntdll!RtlpHpGCTimerEnable` | `+0x121` | `+0x126` | `ntdll!TpReleaseTimer` | `CMPXCHG` 에서 졌을 때만 (다른 스레드가 먼저) | [정적] |
| `ntdll!RtlpHpGCTimerSchedule` | `+0x52` | `+0x57` | `ntdll!TpSetTimerEx(타이머, &0x1ce278, 0, 1000)` | 타이머 ≠ 0 · 종료 중 아님 · 깃발 `0x1ce280` 0 → 1 을 이김 — **언제 불리는지는 동적** | [정적] |

### 호출 관계

```
_LdrpInitialize
 ├─ (1) +0x16c  LdrpInitializeProcess
 │       └─ +0x1a47 LdrpEnableParallelLoading (ECX = LoaderThreads = 0)
 │            ├─ (1) +0x21  LdrpDetectDetour            NtQueryInformationThread(-2, 0x2a)        [#1]
 │            ├─ (2) +0x2e  RtlGetSuiteMask            bit 16 꺼짐 → 스레드 수 4
 │            ├─ (3) +0x49  TpAllocPoolInternal(&LdrpThreadPool, 1)                               [#2 ~ #12]
 │            │       ├─ +0x245 TppPoolUpdateNodeRelation   NtQuerySystemInformationEx(0x6b) ×2
 │            │       ├─ +0x297 NtCreateIoCompletion        → 풀 +0x40
 │            │       ├─ +0x308 NtCreateWorkerFactory       → 풀 +0x38   (실패면 풀 폐기, *출력 = 0)
 │            │       ├─ +0x448 NtSetInformationWorkerFactory(0xd, 1) → +0x459 풀 +0x17a = 1   ← 로더 풀만
 │            │       ├─ +0x393 TppInitializeTimerQueue     NtCreateTimer2 · …WaitCompletionPacket ×2 큐
 │            │       ├─ +0x3a8 ~ +0x3ee  TppPoolpList 꼬리에 풀 +0x180 잇기 (TppPoolpListLock)
 │            │       └─ +0x4be  LdrpThreadPool ← 풀
 │            ├─ (4) +0x66  TpSetPoolWorkerThreadIdleTimeout(풀, −30초)                            [#13]
 │            │       └─ +0x3e  NtSetInformationWorkerFactory(풀+0x38, 2, 8B)
 │            ├─ (5) +0x7f  TpSetPoolMaxThreads(풀, 3)                                             [#14]
 │            │       └─ +0x40  NtSetInformationWorkerFactory(풀+0x38, 5, 4B)
 │            └─ (6) +0xe2  TpAllocWork(&LdrpMapAndSnapWork, LdrpWorkCallback, 0, &환경 V3)        (syscall 없음)
 │                    ├─ +0x95  RtlAllocateHeap 0xf0
 │                    └─ +0xda  TppWorkInitialize → TppCleanupGroupMemberInitialize (풀 Refcount +1 · PoolObjectList)
 ├─ (2) +0x282  RtlpHpGCTimerEnable(RDX = LdrpThreadPool)       ← LdrpThreadPool ≠ 0 일 때
 │       ├─ +0xa8  RtlAllocateHeap 0x168 (_TP_TIMER)
 │       ├─ +0xef  TppInitializeTimer   +0x57 BindingCount 0 → 1 → +0xd0 NtSetInformationWorkerFactory(3, 1)   [(15)]
 │       └─ +0x109 CMPXCHG 0x1ce270 ← 타이머      (콜백 칸 +0x50 = RtlpHpGCTimerCallback)
 └─ (3) +0x252  LdrpInitializationComplete
```

- 라벨은 ret 자리. `(1) (2) (3)` 은 그린 것끼리의 코드 흐름 순서 (사이에 안 그린 호출이 많다). `_LdrpInitialize` 의 `+0x282` 는 주소로는 `+0x252` 보다 뒤지만 `+0x21f JNZ +0x27d` → `+0x282 JMP +0x221` 로 돌아와서 실행은 그 앞이다.
- `[#n]` 은 위 syscall 표 번호.

### 질문별 요약 (근거 줄은 결과서 해당 절)

| # | 질문 | 답 | 닫힘 |
|---|---|---|---|
| 1 | `TpSetPoolWorkerThreadIdleTimeout` 인자·핸들 칸·클래스·실패 | 풀 · INT64 −300000000 (상대 30초 `[추측: NT 관례]`, 상수). 핸들 = 풀 `+0x38`. 클래스 2, 8B, 버퍼 = 그 값 하나. 풀에 쓰는 칸 없음. 오류 갈래 = 풀 0 / 값 ≥ 0 / `ShutdownInProgress`. 상태는 그대로 돌려주고 부르는 쪽은 안 봄. **로더 풀은 `EDX = 1` 로 만들어 `+0x443` 클래스 `0xd` 를 더 부른다. 핸들 0 은 진짜 길에선 불가** | **정적으로 닫힘** |
| 2 | `TpAllocWork` | 콜백 `LdrpWorkCallback` (`0xe9ef0`), 문맥 0, 환경 V3 (풀 = `LdrpThreadPool`, 우선순위 1, 나머지 0). 힙 `0xf0` = `_TP_WORK`. syscall 없음. 결과 → **`LdrpMapAndSnapWork`** (요청서 짐작 `LdrpWorkQueue`·`LdrpLoaderWork` 아님) | **정적으로 닫힘** |
| 3 | `RtlpHpGCTimerEnable` | `LdrpThreadPool ≠ 0` 일 때 `_LdrpInitialize +0x27d`. 이미 타이머가 있으면 `0xff`. 힙 `0x168` = `_TP_TIMER`, 우선순위 2, 문맥 = 전역 칸 `0x1ce270` 주소. syscall 1 (클래스 3). 만들기만, 켜기는 `RtlpHpGCTimerSchedule → TpSetTimerEx` (힙 코드가 부름) | 조건·전역·syscall **정적으로 닫힘**, 켜는 시점은 **동적** |
| 4 | 덫 안 syscall 전체 표 | 위 표. 합계 14, `IdleTimeout` 13번째 | **정적으로 닫힘** (표 밖 힙 확장 횟수만 ±) |

## 확인된 것

- `[정적]` **로더 풀과 기본 풀은 같은 `TpAllocPoolInternal` 을 둘째 인자만 달리해 만든다.** 기본 풀 = `TppPoolpReferenceGlobalPool +0xbb XOR EDX,EDX` → `+0xc2/+0xc7` (0), 로더 풀 = `LdrpEnableParallelLoading +0x38 MOV EDX,1` → `+0x44/+0x49` (1). 인자는 `+0x2f AND EDX,R12D(=1)` → `+0x32` 로 `[RSP+0xb8]` 에 남고, `+0x316 CMP` · `+0x31e JNZ +0x42e` 에서 갈래를 정한다.
  - 1 이면 `+0x42e ~ +0x448` 클래스 `0xd` (4B, 값 = 그 인자 1) → 성공이면 `+0x452 ~ +0x459` 풀 `+0x17a` (`PoolFlags`) = 1 → `+0x460 JMP +0x324` 로 본 줄기에 합류.
  - 그래서 **run62 의 `[공백]` (`+0x443` 갈래를 test.exe 가 타나) 은 닫혔다**: 기본 풀 (`+0xbe2` 아래) 은 안 탄다 → run62 의 `+0xb83 ~ +0xc2d` syscall 30 은 그대로. 로더 풀만 탄다.
- `[정적]` **로더 풀도 일꾼 공장을 만든다** (`NtCreateWorkerFactory` 는 기본 풀과 같은 `+0x303/+0x308`). 이 프로세스엔 일꾼 공장이 **둘** (기본 풀 · 로더 풀) 이다. 로더 풀 쪽 최대 스레드는 처음 `0x200` → `TpSetPoolMaxThreads` 로 3.
- `[정적]` **풀은 만들 때 전역 풀 목록 `TppPoolpList` 꼬리에 걸린다** (`+0x3a8 RtlAcquireSRWLockExclusive(TppPoolpListLock)` → `+0x3b5 LEA RAX,[RDI+0x180]` → `+0x3ca CMP` 고리 검사 (`+0x3d4 INT 0x29`) → `+0x3d6 ~ +0x3e0` 잇기 → `+0x3ee` 풀기). `+0x39b JS` (타이머 큐 실패) 가 아니면 늘 탄다 — 기본 풀·로더 풀 둘 다.
- `[정적]` **`TpSetPoolWorkerThreadIdleTimeout`·`TpSetPoolMaxThreads` 는 풀에 아무것도 안 쓴다.** 값은 커널 일꾼 공장 안에만 있다. 유저 풀 개체엔 사본이 없다.
- `[정적]` **`TpAllocWork` 는 `BindingCount` (`+0x1b0`) 를 안 올린다. 타이머 (`TppInitializeTimer +0x57`) 만 올린다.** 그래서 로더 풀의 클래스 3 syscall 은 은퇴 앞 GC 타이머 때 한 번이다.
- `[정적]` **최대 스레드 계산** (flow-writer `.asm`): `+0x66 EDX = 1` · `+0x6b ECX = EBX − 1` · `+0x6e CMP EBX,EDX` · `+0x70 CMOVA` → `EBX > 1` 이면 `EBX − 1`, 아니면 1. test.exe (`EBX = 4`) → 3.
- `[정적]` **GC 타이머 전역 세 칸** (PDB 이름 없음, `.data` `0x1ce270`·`0x1ce278`·`0x1ce280`):
  - `0x1ce270` 타이머 포인터 — `RtlInitializeHeapManager +0x7d` 가 0 으로, **`RtlpHpGCTimerEnable +0x109 CMPXCHG.LOCK`** 가 0 → 타이머. 복제 길 `RtlUnlockHeapManagerForCloning +0x15` 가 비움 (`—`).
  - `0x1ce278` 기한 — `RtlInitializeHeapManager +0x96` (run46). `RtlpHpGCTimerSchedule +0x48` 이 `TpSetTimerEx` 에 주소로 넘김.
  - `0x1ce280` "예약됨" 깃발 — `RtlInitializeHeapManager +0x85` 가 0, `RtlpHpGCTimerSchedule +0x31 CMPXCHG.LOCK` 0 → 1, 타이머 콜백 `RtlpHpGCTimerCallback +0x29` 가 0 (일꾼 스레드).

## 사진과 맞춰 본 것 (run44 `[관찰]`, 새 실행 없음)

flow-writer 가 ntdll `.data` 줄을 다시 읽었다 (ntdll 바탕 `0x7ffafb660000`).

- **`LdrpThreadPool`** (`0x1d25c0`): P1a 0 → **P1b `0x25996f387d0`** → P2 같음 (`_P1a.txt:5078`, `_P1b.txt:6196`, `_P2.txt:6208`). 풀 만들기가 성공했다 → 1번 결론대로 핸들 칸은 0 이 아니었을 것 `[추측: 핸들 값 자체는 사진에 없음 — 풀이 힙 안이라 안 읽음]`.
- **`LdrpMapAndSnapWork`** (`0x1d2640`): P1a 0 → **P1b `0x25996f38450`** (`_P1a.txt:5086`, `_P1b.txt:6204`, `_P2.txt:6216`). `TpAllocWork` 갈래를 탔다 (스레드 수 > 1 · Detour 없음과 맞음).
- **`LdrpDetourExist`** (`0x1d2610`): P1a·P1b·P2 모두 0 (`_P1a.txt:5083`, `_P1b.txt:6201`, `_P2.txt:6213`).
- **`TppPoolpList`** (`0x1cc728`, `LIST_ENTRY`): `Flink` = `0x25996f31c00` (P1a ~ P2 같음) = 기본 풀 `TppPoolpGlobalPool` `0x25996f31a80` (`0x1d2298`, `_P1a.txt:5027`) + `0x180`. `Blink` 는 P1a `0x25996f31c00` → **P1b `0x25996f38950`** = 로더 풀 `0x25996f387d0` + `0x180` (`_P1a.txt:3564 ~ 3565`, `_P1b.txt:4682 ~ 4683`, `_P2.txt:4694 ~ 4695`). **"`+0x180 PoolLinks` 를 목록 꼬리에 잇는다" 가 값으로 맞는다.**
- **`TppPoolpListLock`** (`0x1d2288`): P1a·P1b·P2 0 (`_P1a.txt:5026`, `_P1b.txt:6144`, `_P2.txt:6156`) — 잡고 풀어 0 으로 돌아온 것과 맞음.
- **GC 타이머** `0x1ce270` 줄: P1a·P1b `0` → **P2 `0x25996f43210`**, `0x1ce278` = `ffffffff ff676980` (−10,000,000, P1a ~ P2 같음), `0x1ce280` = 0 (P1a ~ P2) (`_P1a.txt:4001 ~ 4002`, `_P1b.txt:5119 ~ 5120`, `_P2.txt:5131 ~ 5132`).
  - 타이머 포인터가 **P1b ~ P2 사이**에 생겼다 = `+0x1bb6` 그래프 켜기 뒤, `_LdrpInitialize +0x27d` 와 같은 구간. `RtlpHpGCTimerEnable` 이 test.exe 에서 탔다는 받침이다.
  - 깃발 `0x1ce280` 은 P2 (`NtContinue` 입구) 에서 0. 은퇴 순간 "예약됨" 이 아니다. 예약했다가 콜백이 지웠을 수도 있어 "`TpSetTimerEx` 가 은퇴 전에 안 탔다" 고는 못 한다 `[추측]` → 동적 4번.

## 결과서와 같음 / 다르게 적은 곳 · 보탠 것

- 같음: 1 ~ 4번 결론, syscall 표 순서·클래스, `TpAllocPoolInternal` 갈래, GC 타이머 세 칸 — `.asm` 줄이 결과서와 모두 같다.
- (보탬) **`LdrpDetectDetour` 는 `+0x1a47` 이 처음 부르는 곳이 아니다.** 더 앞 kernel32 준비 길 `LdrpLoadDllInternal +0x1bb/+0x1c0` 에서도 부른다 (`_calls.tsv`, 들어가는 조건 `+0x1a1 ~ +0x1b5` 는 안 봄). 결과서 표 1번 "첫 호출은 0" 은 "`+0x1a47` 때 `LdrpDetourExist` 가 아직 0" 으로 읽어야 한다 — 사진 끝까지 0 과 맞음. 덫 안 표 숫자는 그대로.
- (보탬) `LdrpDetectDetour` 가 `LdrpDetourExist` 를 1 로 만드는 길은 **둘**이다 (짐 지도 3.8 의 "detour 를 찾으면 `[추측]`" 을 코드로): ① `+0x18 ~ +0x4e` 에서 `LdrpThunkSignature` 16B × 5 를 `LdrpCriticalLoaderFunctions` 표의 함수 머리와 비교해 다르면 `+0x75 LdrpLogInternal` → `+0x7a` ② `+0x9c NtQueryInformationThread(-2, 0x2a)` 의 4B 답이 1 이면 `+0xac`. 둘 다 `+0x8 · +0x12` 에서 이미 1 이면 건너뜀.
- (보탬) 최대 스레드 계산 (`+0x66 ~ +0x70`) 과 사진 대조 (위 절) 는 flow-writer.
- 결과서의 "`TpAllocPool` 공개 함수가 `JMP` 로 넘긴다 (둘째 인자 `[공백]`)" 는 그대로 공백.

## 정정

- **run62 (`runs/03_sprint/run62-tp-etw-init.md` "결과서와 다르게")** 의 `[공백]` "`TpAllocPoolInternal +0x316` 의 `[RSP+0xb8]` 출처 — test.exe 가 `+0x443` 갈래를 타나 (타면 구간 31)" → **닫힘: 기본 풀 길은 안 탄다.** `[RSP+0xb8]` = 둘째 인자 `& 1`, 기본 풀은 0 (`TppPoolpReferenceGlobalPool +0xbb XOR EDX,EDX`). 구간 30 그대로. run62 문서 맨 위에 한 줄 넣었다.
- 요청서 2번의 결과 전역 짐작 (`LdrpWorkQueue`·`LdrpLoaderWork`) 은 틀림 → `LdrpMapAndSnapWork` (run24 에 이미 있던 이름). 요청서는 고치지 않는다.
- PE_emulator 원문의 "덫 안 14번째 = `IdleTimeout`" 은 asm 순서로 **13번째** — 정정이라기보다 "표 밖 하나가 끼었다" 는 신호 (결론 3).

## 아직 확인 필요

동적 몫은 모두 **Q104 VM 단계**에 얹는다. ntdll 멈춤점은 test.exe 일 때만 걸리게 조건을 붙인다 (결과서 "dast 요청 필요" 1 ~ 5).

1. **핸들이 진짜로 0 이 아닌지 + 풀 칸 사진**: `ntdll!LdrpInitializeProcess+0x1a47` (= `ntdll+0x8de4b`) 에서 `dt ntdll!_TP_POOL poi(ntdll!LdrpThreadPool)` — `+0x38`·`+0x40` ≠ 0, `+0x17a` = 1, `+0x1a8` = 2, `+0x1ac` = `0xfffffffe`, `+0x1b0` = 0. `dt ntdll!_TP_WORK poi(ntdll!LdrpMapAndSnapWork)` — `+0x50` = `ntdll+0xe9ef0`, `+0x90` = 위 풀, `+0xc0` = 1, `+0xe8` = 1.
2. **syscall 순서·개수**: `ntdll!NtSetInformationWorkerFactory` (`ntdll+0x1643c0`) 입구에서 `rcx`·`edx`·`r9d` — 기대 (`h`, `0xd`, 4) → (`h`, 2, 8) → (`h`, 5, 4) → `_LdrpInitialize+0x27d` 뒤 (`h`, 3, 4), 같은 `h`. `NtCreateWorkerFactory` (`ntdll+0x162840`) 와 비교. 가능하면 이 구간 표 밖 `NtAllocateVirtualMemory` 개수.
3. **입력 값**: `LoaderThreads` (`dd poi(@$peb+0x20)+0x40c L1`) · SuiteMask bit 16 (`dd 0x7ffe02d0 L1`) · `dd 0x7ffe03c0 L1` · `db 0x7ffe0386 L1` · `dd ntdll!TppHeapTag L1` · `dd ntdll!TppPoolpDefaultPoolCpuSetCount L1`.
4. **GC 타이머 켜기**: `dq ntdll+0x1ce270 L3` 를 은퇴 직전·직후에, `ntdll!RtlpHpGCTimerSchedule` (`ntdll+0x11d308`) · `ntdll!TpSetTimerEx` (`ntdll+0x7eb60`) 입구가 은퇴 전에 서는지. (P2 사진 `+0x10` 깃발 0 은 위 절.)
5. 커널 일꾼 공장 값 (유휴 −30초 · 최대 3 · 바인딩 1 · 플래그 bit 11) — `!object`·`dt nt!_…` 로 읽을 타입 이름이 PDB 에 있는지부터 `[공백]`.

추천 멈춤점: `ntdll+0x8de4b` (사진) · `ntdll!NtSetInformationWorkerFactory` 입구 · `ntdll+0xe6559` (`IdleTimeout +0x39`, `rcx` = 핸들) · `ntdll+0xe5d08` (`TpAllocPoolInternal +0x308`, `poi(@rdi+0x38)`) · `ntdll+0x8c209` (`_LdrpInitialize +0x27d`) · `ntdll!RtlpHpGCTimerSchedule` 입구.

- 다음 바퀴 후보 (정적):
  - `TpSetTimerEx` (rva `0x7eb60`, 1157B) 아래 syscall — GC 타이머를 켤 때 무엇이 더 생기나.
  - 커널 `NtSetInformationWorkerFactory` 클래스 2·3·5·`0xd` 갈래 (Ghidra 가 점프표를 못 풀었음) 와 일꾼 공장 `+0x198` bit 11 의 뜻.
  - 공개 `TpAllocPool +0x27` 의 `JMP` 가 넘기는 둘째 인자.
  - `LdrpLoadDllInternal +0x1a1 ~ +0x1b5` — kernel32 준비 때 `LdrpDetectDetour` 를 부르는 조건 (부르면 `NtQueryInformationThread(0x2a)` 가 덫 밖에서 한 번 더).

## 도구 메모

- ntdll `.data` 사진 줄은 주소 열이 `00007ffa`fb8XXXXX` 꼴이라, rva 에 바탕 `0x7ffafb660000` 을 더해 grep 하면 바로 찾는다 (run66 이 쓴 방법과 같음).
- 새 함정 (디버거) 없음.

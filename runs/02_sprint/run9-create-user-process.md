# 2026-10-19 run9 — `NtCreateUserProcess` 가 직접 부르는 함수 (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: ntoskrnl (`ntoskrnl.exe`, 26100.9457), pdb `ntkrnlmp.pdb` GUID `C29EBFB06B78B3C020DCA66D99713F9E1`
  - 01_sprint run3 의 커널 PDB GUID 와 같다 → 같은 커널을 본 것.
- 뽑은 함수 2개: `NtCreateUserProcess`, `PspInsertThread` (depth 0)

## 목적

- `NtCreateUserProcess` 가 **직접 부르는 함수 전부**를 코드 순서대로 본다 (대기열 Q1).
- 01_sprint run3 에서 관찰한 4개 오프셋이 코드에서도 같은지 맞춰 본다.
- flow.md 2.2 `[공백]` 중 "첫 스레드를 멈춘 채 두나 / 풀어 주나" 를 코드로 볼 수 있는 데까지 본다.

## 방법

- 정적: `_workspace/sast/03_create-user-process.md` — ntoskrnl, `^(NtCreateUserProcess|PspInsertThread)$` depth 0
- 결과서: `_workspace/sast/03_create-user-process_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨):
  - `ms/disassemble/ntoskrnl/_calls.tsv`
  - `ms/disassemble/ntoskrnl/NtCreateUserProcess.asm`, `PspInsertThread.asm`
  - `ms/decomp/ntoskrnl/NtCreateUserProcess.c`, `PspInsertThread.c`
- 오프셋 읽는 법
  - `call` = call 명령이 있는 자리
  - `ret` = call 다음 명령 자리 = **WinDbg 콜스택에 찍히는 오프셋**. flow.md 그림 라벨은 이걸 쓴다.

## 결과

### 정적 — `NtCreateUserProcess` 의 직접 호출

- 직접 호출 **83줄**. 꼬리호출 0, 함수 조각으로 가는 점프 0.
  - 이 중 2곳(`+0x5b2`, `+0x11bb`)은 함수 포인터를 거쳐 부르는 **간접 호출**이다. 대상은 실행해 봐야 안다.
- 관찰 4개를 경계로 나누면:

| 구간 | 범위 | 호출 수 | 하는 일 (요약) |
|---|---|---|---|
| (가) | `PspAllocateProcess` 앞 | 47 | 재료 모으기: 인자 검사, exe 파일 열기, 이미지 섹션 만들기, 토큰·권한·서명 검사 |
| (나) | `PspAllocateProcess` ~ `PspAllocateThread` | 10 | 첫 스레드 준비: 시작 문맥, 스레드 플래그 바꾸기 |
| (다) | `PspAllocateThread` ~ `PspInsertProcess` | 5 | 보호 프로세스 검사 등 |
| (라) | `PspInsertProcess` ~ `PspInsertThread` | **0** | 호출 없이 인자만 채움 |
| (마) | `PspInsertThread` 뒤 | 17 | 부모 쪽 마무리: 프로세스 핸들, 결과 기록, 정리 |
| 경계 | 관찰 4개 | 4 | |

- 아래 표는 흐름도에 넣을 **대표 지점(12개)** 만. 나머지 71개는 검사·잠금·해제 같은 잡일이다 (전체 목록은 결과서).

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!NtCreateUserProcess` | `+0x295` | `+0x29a` | `nt!PspBuildCreateProcessContext` | [정적] |
| `nt!NtCreateUserProcess` | `+0x6c6` | `+0x6cb` | `nt!IopCreateFile` | [정적] |
| `nt!NtCreateUserProcess` | `+0x937` | `+0x93c` | `nt!MmCreateSpecialImageSection` | [정적] |
| `nt!NtCreateUserProcess` | `+0xc31` | `+0xc36` | `nt!PspAllocateProcess` | [26100.9457 확인] + [정적] |
| `nt!NtCreateUserProcess` | `+0xd1d` | `+0xd22` | `nt!PspCreateUserContext` | [정적] |
| `nt!NtCreateUserProcess` | `+0xdce` | `+0xdd3` | `nt!PspMapThreadCreationFlags` | [정적] |
| `nt!NtCreateUserProcess` | `+0xe57` | `+0xe5c` | `nt!PspAllocateThread` | [26100.9457 확인] + [정적] |
| `nt!NtCreateUserProcess` | `+0xe8f` | `+0xe94` | `nt!PsTestProtectedProcessIncompatibility` | [정적] |
| `nt!NtCreateUserProcess` | `+0xfbd` | `+0xfc2` | `nt!PspInsertProcess` | [26100.9457 확인] + [정적] |
| `nt!NtCreateUserProcess` | `+0x1015` | `+0x101a` | `nt!PspInsertThread` | [26100.9457 확인] + [정적] |
| `nt!NtCreateUserProcess` | `+0x10a2` | `+0x10a7` | `nt!PspCreateObjectHandle` | [정적] |
| `nt!NtCreateUserProcess` | `+0x1124` | `+0x1129` | `nt!PspUpdateCreateInfo` | [정적] |

- 대표로 안 넣었지만 같이 볼 만한 줄 [정적]:
  - `+0x311` (ret `+0x316`) `PspCaptureCreateInfo`, `+0xac6` (ret `+0xacb`) `PspCaptureProcessParameters`: 유저가 준 값을 커널로 복사
  - `+0xd39` (ret `+0xd3e`) `PspGetContextThreadInternal`: `PspCreateUserContext` 의 **반대쪽 길**
  - `+0x75a` (ret `+0x75f`) `PspUpdateCreateInfo`: 실패 길에서 부르는 자리 (값 1 또는 2)

### 정적 — `PspInsertThread` 안 (2.2 관련)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `nt!PspInsertThread` | `+0x2e0` | `+0x2e5` | `nt!KeSuspendThread` | [정적] (조건부) |
| `nt!PspInsertThread` | `+0x4f5` | `+0x4fa` | `nt!PspCallThreadNotifyRoutines` | [정적] |
| `nt!PspInsertThread` | `+0x520` | `+0x525` | `nt!PspCreateObjectHandle` | [정적] |
| `nt!PspInsertThread` | `+0x5a1` | `+0x5a6` | `nt!KeReadyThread` | [정적] |
| `nt!PspInsertThread` | `+0x5dd` | `+0x5e2` | `nt!KeStartThread` | [정적] |
| `nt!PspInsertThread` | `+0x802` | `+0x807` | `nt!PspCallProcessNotifyRoutines` | [26100.9457 확인] + [정적] |
| `nt!PspInsertThread` | `+0x9fc` | `+0xa01` | `nt!KeForceResumeThread` | [정적] (조건부) |

### 호출 관계

```
NtCreateUserProcess                              (번호 = 코드 순서. 관찰은 4개만)
 ├─ (1)  +0x29a   PspBuildCreateProcessContext   [정적]
 ├─ (2)  +0x6cb   IopCreateFile                  [정적]
 ├─ (3)  +0x93c   MmCreateSpecialImageSection    [정적]
 ├─ (4)  +0xc36   PspAllocateProcess             [관찰]+[정적]
 ├─ (5)  +0xd22   PspCreateUserContext           [정적]
 ├─ (6)  +0xdd3   PspMapThreadCreationFlags      [정적]
 ├─ (7)  +0xe5c   PspAllocateThread              [관찰]+[정적]
 ├─ (8)  +0xe94   PsTestProtectedProcessIncompatibility  [정적]
 ├─ (9)  +0xfc2   PspInsertProcess               [관찰]+[정적]
 ├─ (10) +0x101a  PspInsertThread                [관찰]+[정적]
 │        ├─ (1) +0x2e5  KeSuspendThread         [정적] ThreadFlags bit0 일 때만
 │        ├─ (2) +0x807  PspCallProcessNotifyRoutines  [관찰]+[정적]
 │        └─ (3) +0x5a6  KeReadyThread           [정적]
 ├─ (11) +0x10a7  PspCreateObjectHandle          [정적] 프로세스 핸들
 └─ (12) +0x1129  PspUpdateCreateInfo            [정적] 성공 길 끝
```

- `PspInsertThread` 안에서 `(2) +0x807` 이 `(3) +0x5a6` 보다 오프셋이 큰데 먼저인 이유 [정적]:
  - `+0x4b8` 의 `JNZ` 가 함수 뒤쪽 덩어리(`+0x7bb` ~)로 건너가 `+0x802` 에서 알림을 부르고, `+0x809` 의 `JMP` 로 `+0x4be` 로 돌아온다.
  - 그다음 `+0x5a1` 에서 `KeReadyThread`. 그래서 코드 흐름 순서는 Suspend → 알림 → Ready.

## 확인된 것

- **관찰과 코드가 맞는다.** [관찰] run3 + [정적] run9
  - `NtCreateUserProcess` `+0xc36` / `+0xe5c` / `+0xfc2` / `+0x101a` → `PspAllocateProcess` / `PspAllocateThread` / `PspInsertProcess` / `PspInsertThread`: 4개 모두 `ret` 오프셋까지 같다.
  - `PspInsertThread` `+0x807` → `PspCallProcessNotifyRoutines` 도 같다.
  - 네 함수는 `NtCreateUserProcess` 안에서 **한 번씩만** 불린다. 코드 순서도 관찰 순서와 같다.
- **옛 방식 `PspCreateProcess` 는 직접 부르는 코드가 없다.** [정적] run3 관찰("안 불림")과 맞는다.
- **`PspInsertProcess` 와 `PspInsertThread` 사이엔 호출이 0개.** [정적]
  - `PspInsertProcess` 가 실패를 돌려줘도 `PspInsertThread` 를 먼저 부르고, 실패 검사는 그 뒤 `+0x1025` 에서 한다.
- **points.md 의 `[확인 필요]` 이름 4개는 `NtCreateUserProcess` 가 직접 부르지 않는다.** [정적]
  - `MiCreateImageFileMap`, `MmInitializeProcessAddressSpace`, `PspMapSystemDll`, `MmCreateTeb` — 이 빌드에 함수로는 있다. 더 깊은 곳(Q2 이후)에서 찾을 일.
- **첫 스레드를 풀어 주는 호출은 `NtCreateUserProcess` 안에 없다.** [정적]
  - `+0x101a` 뒤 17개 호출 중 Resume·Ready·Start 같은 이름이 없다.
  - 입구에서 `ThreadFlags`(8번째 인자)에 bit0 말고 다른 비트가 있으면 바로 `0xc000000d` 로 돌아간다 (`+0x1bf` TEST → `+0x1ce` JZ → `+0x11f9`). 즉 허용되는 비트는 **bit0 하나뿐**.
  - `+0xdd3` `PspMapThreadCreationFlags` 가 그 값을 커널 내부 플래그로 바꿔 `PspInsertThread` 에 넘긴다.
  - `PspInsertThread` 안에서 그 플래그의 bit0 가 켜져 있으면 `+0x2e0` 에서 `KeSuspendThread` 를 부르고, 꺼져 있으면 `+0x2db` `JZ` 로 건너뛴다.
  - 함수 끝 쪽 `+0x5a1` 에서 `KeReadyThread` 를 부른다. 멈춤 여부와 상관없이 지나는 자리다.
  - 즉 **멈춰 둘지는 런타임 값(ThreadFlags bit0)이 정한다.** 코드만으로는 test.exe 때 어느 쪽인지 못 정한다.
- `PspInsertThread` 가 `+0x520` 에서 **스레드 핸들**을, `NtCreateUserProcess` 가 `+0x10a2` 에서 **프로세스 핸들**을 만든다. [정적]
- 성공 길 끝 `+0x1124` 에서 `PspUpdateCreateInfo` 를 값 6(`+0x111f` `MOV ECX,0x6`)으로 부른다. [정적]
  - 6 = "성공" 이라는 건 알려진 값이고 이 빌드에서 이름으로 확인하진 않았다. [추측]

## 아직 확인 필요

- 정적으로 더 볼 것 (작고 싸다)
  - `PspMapThreadCreationFlags` 본문 (80바이트): ThreadFlags bit0 가 내부 플래그 bit0 로 그대로 가는지
  - `PspCreateUserContext` 본문 (91바이트): 첫 스레드의 유저 시작 위치를 정하는 곳일 수 있다 [추측] → Q7 과 이어짐
  - `MmCreateSpecialImageSection` callee: `MiCreateImageFileMap` 과 이어지는지
- 동적으로 볼 것 (커널 멈춤점은 부모 cmd.exe 로 `/p` 필터)
  1. `ThreadFlags` 실제 값: `nt!NtCreateUserProcess` 입구에서 8번째 인자 (계산상 `poi(@rsp+0x40)`, 디버거로는 아직 안 맞춰 봄)
  2. `PspInsertThread` 입구에서 4번째 인자가 가리키는 값의 bit0 (`poi(@r9) & 1`), `+0x2e0` `KeSuspendThread` 를 탔는지, `+0x5a1` `KeReadyThread` 때 콜스택
  3. `+0xd1d` `PspCreateUserContext` 길인지 `+0xd39` `PspGetContextThreadInternal` 길인지 (일반 exe 실행이면 앞쪽일 것 [추측])
  4. 간접 호출 2곳의 실제 대상: `+0x5b2` (전역 포인터 `nt+0xfd83d0`), `+0x11bb` (전역 포인터 `nt+0xf04d30`)
  5. 서명 수준이 안 맞아 `MmCreateSpecialImageSection` 이 한 번 더 불리는지 (`+0xa08` ~ `+0xa2a` 뒤 재시도 루프). test.exe 면 한 번일 것 [추측]
  6. 성공 길 끝(`+0x1124` `PspUpdateCreateInfo`, `ECX=6`) 도달과 반환값
- 첫 스레드를 **누가 풀어 주나**: `NtCreateUserProcess` 밖이다. 부모 유저 쪽(`KERNELBASE!CreateProcessInternalW` 등, 이름 확인 필요)이 따로 부르는지 → 대기열 Q5

# 2026-10-22 run14 — 부모 쪽 KERNELBASE 가 첫 스레드를 "늘 멈춘 채로 만들고, 할 일 뒤에 직접 풀어 주는" 자리: `CreateProcessInternalW` +0x30ee `NtCreateUserProcess` → +0x4672 `CsrClientCallServer` → +0x5bd3 `NtResumeThread` (정적)

## 환경

- 정적만 한 바퀴다. 디버거는 안 붙였다 (dast 없음).
- 정적: KernelBase (`KernelBase.dll`, 26100.9457), pdb `kernelbase.pdb` GUID `72CCE506944DE0267C172AD64287F9BD1` (`ms/sym/KernelBase.dll.pdb.info.txt`)
  - KernelBase 는 이번에 처음 본 모듈이다. analyze 는 sast/08(run13) 때 걸어 둔 것을 그대로 썼다 (함수 6448개).
  - 버전 대조: "알려진 사실" 표에 KernelBase 줄이 없어서 표 대조는 못 했다. 대신 함수 위치·크기가 `_index.tsv` 와 같은 것, call 길이가 맞는 것 등 안쪽 일관성만 봤다 (결과서).
- 뽑은 함수 (모두 depth 0)
  - 요청서 3개: `CreateProcessInternalW` (0x7d6e0, **28087바이트**), `CreateProcessW` (0x7d5f0, 107바이트), `ResumeThread` (0xf1f00, 51바이트)
  - 넓힌 것: `CreateProcessInternalW` 의 SEH 정리 함수 3개 (`$fin$0`, `$filt$1`, `$fin$2`). "풀어 주는 호출이 한 곳뿐" 을 말하려면 같은 함수에 딸린 정리 코드까지 봐야 해서다. 여기엔 핸들 닫기·메모리 해제·프로세스 끝내기 같은 **정리 호출뿐**이었다.

## 목적

- 대기열 **Q5b**. flow.md 2.2 의 `[공백]` "부모 유저 쪽이 `NtCreateUserProcess` 뒤에 첫 스레드를 정말 풀어 주나" 를 채운다.
  1. `CreateProcessInternalW` 가 `NtCreateUserProcess` 를 부르는 자리 (몇 곳).
  2. 그 뒤 풀어 주는 호출이 있나, `NtResumeThread` / `NtAlertResumeThread` 중 무엇인가, 언제 건너뛰나 (`CREATE_SUSPENDED`).
  3. 커널에 넘기는 `ThreadFlags`(8번째 인자) bit0 를 켜나 (켜면 커널 `PspInsertThread +0x2e5` 에서 `KeSuspendThread`, run12).
  4. `CreateProcessW` 에서 와도 같은가.

## 방법

- 정적: `_workspace/sast/09_create-process-internal.md` — kernelbase, `^(CreateProcessInternalW|CreateProcessW|ResumeThread)$` depth 0
- 지도: `ms/disassemble/kernelbase/_map/CreateProcessInternalW_{nodes,edges}.tsv`, `_summary.txt`
- 결과서: `_workspace/sast/09_create-process-internal_result.md` (**분석성공**)
- 근거 파일 (커밋 안 됨)
  - `ms/disassemble/kernelbase/_calls.tsv`, `ms/disassemble/kernelbase/{CreateProcessInternalW,CreateProcessW,ResumeThread}.asm`
- 오프셋 읽는 법 (run9 ~ 13 과 같음)
  - `call` = call 명령이 있는 자리, `ret` = call 다음 명령 자리 = **WinDbg 콜스택에 찍히는 오프셋**. flow.md 그림 라벨은 `ret` 을 쓴다.
  - 이 함수의 ntdll 호출은 `CALL qword ptr [가져온 함수 칸]` 7바이트라서 `ret` = `call` + 7 이다 (예: `+0x30e7` → `+0x30ee`).
- 레지스터 값("`ESI` 는 늘 1", "`R15B` 는 4", "이 호출을 지나야만 저기 닿는다")은 결과서가 `.asm` 의 점프를 따라 흐름 그래프를 만들어 확인했다 (도달 정의·지배 관계). 다시 보려면 `.asm` 의 `+0x6cb`, `+0x4532`, `+0x5bb2`, `+0x5bb8` 줄.
- 이름은 import 이름 그대로 `.asm` 에 찍혀 있다 (`NtCreateUserProcess`, `NtResumeThread`, `CsrClientCallServer` 는 `_index.tsv` 에 없고 지도에서 `external`). 세 개 다 **ntdll.dll 에서 가져온다** `[정적]` — KernelBase import 표(`llvm-readobj --coff-imports`)로 확인 (오케스트레이터가 이번 바퀴에 읽음, 결과서 밖 근거).

## 결과

### 정적 — 호출 지점 (`ms/disassemble/kernelbase/_calls.tsv`)

| caller | call | ret (= 콜스택 오프셋) | callee | 표시 |
|---|---|---|---|---|
| `KERNELBASE!CreateProcessW` | `+0x61` | **`+0x66`** | `KERNELBASE!CreateProcessInternalW` (유일한 호출) | [정적] |
| `KERNELBASE!CreateProcessInternalW` | `+0x30e7` | **`+0x30ee`** | `NtCreateUserProcess` (import, **이 함수 안에 한 곳**) | [정적] |
| `KERNELBASE!CreateProcessInternalW` | `+0x466b` | **`+0x4672`** | `CsrClientCallServer` (import, csrss 에 알림) | [정적] |
| `KERNELBASE!CreateProcessInternalW` | `+0x5bcc` | **`+0x5bd3`** | `NtResumeThread` (import, **한 곳**, `CREATE_SUSPENDED` 가 없을 때만) | [정적] |
| `KERNELBASE!ResumeThread` | `+0xe` | `+0x15` | `NtResumeThread` (같은 import 칸 `0x18029b9a8`) — 위 길에는 안 낌 | [정적] |

- `NtAlertResumeThread` 는 `CreateProcessInternalW`·`ResumeThread` 어디에도 없다. 지도 전체에도 없다.
- `CreateProcessInternalW` 는 KERNELBASE 의 `ResumeThread` 함수를 **부르지 않는다.** 두 함수가 같은 import 칸으로 각자 `NtResumeThread` 를 부른다.
- `CreateProcessInternalW` 자신에는 함수 포인터 간접 호출이 없다. 직접 호출 자리 357곳 (`_calls.tsv`), 거의 다 잡일 (메모리 해제, 핸들 닫기, 에러 번호, ETW, 기능 켜짐 검사).

### 정적 — 커널에 넘기는 값과 풀어 주기 조건 (`CreateProcessInternalW.asm`)

| 자리 | 명령 | 뜻 | 표시 |
|---|---|---|---|
| `+0x699` → `+0x6cb` | `XOR EAX,EAX` → `LEA ESI,[RAX+1]` | `ESI = 1`. 그 뒤 함수 끝 `POP RSI` 전까지 `ESI` 를 바꾸는 줄이 없다 | [정적] |
| `+0x30af` | `MOV [RSP+0x38],ESI` | `NtCreateUserProcess` **8번째 인자(ThreadFlags) = 1**. 조건 없음 | [정적] |
| `+0x47` → `+0xc7` | `MOV R14D,[RSP+0x2480]` → `MOV [RSP+0x108],R14D` | 7번째 인자 `dwCreationFlags` 를 `[RSP+0x108]` 에 사본으로 둔다 | [정적] |
| `+0x7f0` ~ `+0x800` | `MOV R15D,4` / `TEST [RSP+0x108],R15B` / `JNZ` / `BTS [RSP+0x110],9` | 4 가 **꺼져 있으면** ProcessFlags(`[RSP+0x110]`, 커널에 넘길 값) bit9(`0x200`)를 켠다. 커널에서 뜻은 안 봄 | [정적] |
| `+0x5bb2` ~ `+0x5bc0` | `MOV R15D,4` / `TEST [RSP+0x108],R15B` / `JNZ +0x5be9` | 사본에 4 가 **켜져 있으면** `NtResumeThread` 를 건너뛴다 | [정적] |
| `+0x5bc2` | `XOR EDX,EDX` | `NtResumeThread` 2번째 인자 = 0 (이전 멈춤 횟수는 안 받음). 1번째 인자 = `[RSP+0x1b8]` = `NtCreateUserProcess` 가 돌려준 **스레드 핸들** | [정적] |
| `+0x5bd8` ~ `+0x5be3` | `MOV EBX,EAX` / `TEST EAX,EAX` / `JS` | `NtResumeThread` 가 실패하면 실패 길(`BaseSetLastNTError`) | [정적] |

- 4 = `CREATE_SUSPENDED` 라는 건 Win32 공개 상수 값이다. `[RSP+0x108]` 가 CreateProcess 의 `dwCreationFlags` 라는 건 같은 사본을 `+0x809 TEST …,0x3` 으로 보고 `DbgUiConnectToDbg`(`+0x81a`) 를 부르는 것(디버그 플래그 1·2)과도 맞는다.
- `NtResumeThread` 를 건너뛰는 다른 길 둘 (test.exe 와는 관계 없어 보임 `[추측]`):
  - `+0x5b5b OR [RSP+0x108],R15D` — 패키지(AppX) 앱 마무리 함수들(`CompletePackagedProcessCreationEx` `+0x5ac4`, `CompleteAppExecutionAliasProcessCreationEx` `+0x5b3d`, `BasepFinishPackageActivationForSxS` `+0x5ba8`)이 채우는 칸 `[RSP+0x284]` 가 켜지면 사본에 4 를 켠다. "풀어 주기는 패키지 쪽이 한다" 는 신호로 보인다 `[추측]`.
  - `+0x394d JNZ +0x5be9` — `BasepProcessInvalidImage`(`+0x392d`, 정상 PE 가 아닌 이미지 처리) 뒤 `[RSP+0x298] != 0` 이면. 16비트/VDM 쪽 길로 보인다 `[추측]`.
- `CREATE_SECURE_PROCESS`(0x400000) 가 켜져 있으면 `+0x4172` / `+0x417d JNZ` 로 가운데 일(아래 ①~③) 거의 전부를 건너뛰고 `+0x5bb2` 의 4 검사로 간다. 4 검사는 그대로 한다.

### 정적 — `NtCreateUserProcess` 와 `NtResumeThread` 사이의 큰 일

순서 ① → ② → ③ 은 결과서가 흐름 그래프로 확인했다: ② 의 `CsrClientCallServer` 를 지나지 않고는 ③ 의 어느 호출에도, `NtResumeThread` 에도 못 간다 (`CREATE_SECURE_PROCESS` 길 빼고). ① 은 ② 앞에만 있다. 각 묶음 안의 순서는 코드 배치 순서일 뿐이다. **전부 조건부**다 (대부분 "옵션 import 가 있는지" 검사나 플래그 뒤).

| # | 하는 일 | 대표 호출 (ret) | 역할 |
|---|---|---|---|
| ① | 새 프로세스 검사 | `BasepCheckWebBladeHashes` (`+0x3f91`), `BasepIsProcessAllowed` (`+0x3fd1`), `BasepCheckWinSaferRestrictions` (`+0x40c4`) | 정책·Safer 규칙으로 걸러 보기 (이름으로 본 역할 `[추측]`) |
| ② | **csrss 에 알림** | `BasepConstructSxsCreateProcessMessage` (`+0x445f`), `BasepCsrCaptureSxsMessage` (`+0x4640`), **`CsrClientCallServer` (`+0x4672`)** | 윈도우 서브시스템 서버(csrss)에 "프로세스 만들었다" 메시지. ApiNumber `0x1001d` (`+0x4655`), 길이 `0x218` (`+0x464f`). `CREATE_SECURE_PROCESS` 가 아니면 **모든 길이 지난다** |
| ③ | 상승·잡·앱 호환·패키지 후처리 | `BaseCheckElevation` (`+0x47c1`, `+0x48d5`), `BaseMarkProcessTokenForInstallers` (`+0x4828`), `AssignProcessToJobList` (`+0x55f1`), `BasepGetAppCompatData` (`+0x56c2`, `+0x5959`), `NtAllocateVirtualMemory` (`+0x5755`) + `NtWriteVirtualMemory` (`+0x5795`, `+0x57d7`, `+0x5831`), `BaseElevationPostProcessing` (`+0x588c`), `BasepInitAppCompatData` (`+0x597d`), `BasepPostSuccessAppXExtension` (`+0x59cd`), 패키지 마무리 3개 (위) | 관리자 상승 필요 여부, 잡 연결, 앱 호환 데이터를 자식 메모리에 써 넣기, 패키지 앱 마무리 |

- `NtWriteVirtualMemory` 두 번째 쓰기는 자식 PEB 주소(`[RSP+0x1a0]`) `+0x2d8` 자리에 주소를 쓴다. x64 PEB 의 `pShimData` 자리로 알려진 오프셋이라 그것으로 보인다 `[추측]`.
- 사이의 나머지 잡일: `NtResumeThread` 에 닿을 수 있는 call 자리는 76곳. 옵션 import 있는지 검사 12곳, ETW/로그 12곳, 기능 켜짐 검사 7곳, 정리, 값 읽기(`NtQueryInformationProcess` `+0x41b4` 등).
  - 옵션 import 검사 함수는 링커가 여러 함수를 한 주소로 합친 것이라 Ghidra 가 고른 이름을 믿을 수 없다 (RVA `0x12a02c`, 같은 주소 이름 21개 — 전부는 `pdb_aliases.py --lookup KernelBase.dll 0x12a02c`). 그래서 이름 대신 하는 일로만 적는다: "어떤 import 가 있는지 알려 주는 검사" `[추측]`.
- `NtCreateUserProcess` 앞의 큰 일 하나만: `BasepCreateProcessParameters` (`+0x2de4`, 1411바이트) — 이름대로면 커널에 넘길 프로세스 매개변수(`NtCreateUserProcess` 9번째 인자, `+0x30e7` 앞 `[RSP+0x40]` = R14)를 만드는 곳 `[추측]`.

### 호출 관계

```
KERNELBASE!CreateProcessW                        (부모가 이 입구로 오는지는 [추측])
 └─ +0x66  CreateProcessInternalW                [정적] dwCreationFlags 를 바꾸지 않고 넘김
            │  +0xc7   dwCreationFlags 사본 → [RSP+0x108]
            │  +0x6cb  ESI = 1 (함수 끝까지 그대로)
            │  +0x800  사본 & 4 == 0 이면 ProcessFlags |= 0x200
            ├─ (1) +0x30ee  NtCreateUserProcess   [정적] 한 곳. ThreadFlags(8번째) = 1 (+0x30af)
            │               ══▶ 커널 nt!NtCreateUserProcess (run9~12)
            │                    … PspInsertThread +0x2e5 KeSuspendThread (flags bit0)  → 멈춰 둠
            │    ① 검사 (+0x3f91, +0x3fd1, +0x40c4, 조건부)
            ├─ (2) +0x4672  CsrClientCallServer   [정적] csrss 알림 (보안 프로세스가 아니면 늘 지남)
            │    ③ 상승·잡·앱 호환·패키지 (+0x47c1 … +0x5ba8, 조건부)
            │  +0x5bb8  사본 & 4 (CREATE_SUSPENDED) ?  켜짐 → +0x5bc0 JNZ 로 건너뜀
            └─ (3) +0x5bd3  NtResumeThread(스레드 핸들, NULL)  [정적] 한 곳
                            ══▶ 커널 nt!NtResumeThread +0x9b PsMultiResumeThread … (run13) → 풀어 줌
```

- 번호 `(1)(2)(3)` 은 흐름 그래프로 본 순서다. 실행 때 그 길로 갔는지는 안 봤다.
- `NtCreateUserProcess` 는 자리가 한 곳이지만 **한 번만 불린다는 뜻은 아니다.** 실패 길(`+0x3158`)에서 다시 `+0x30e7` 로 돌아오는 길이 있다 (앱 실행 별칭, 16비트 실행 파일 같은 재시도로 보임 `[추측]`).

## 확인된 것

- **`CreateProcessInternalW` 는 `NtCreateUserProcess` 를 한 곳(`+0x30ee`)에서, `NtResumeThread` 를 한 곳(`+0x5bd3`)에서 부른다.** [정적] (`_calls.tsv`, 지도, SEH 정리 함수 3개까지 확인)
- **커널에 넘기는 `ThreadFlags` 는 늘 1 이다.** [정적] `+0x6cb` 에서 `ESI = 1` → `+0x30af` 8번째 인자. `dwCreationFlags` 를 읽어서 바꾸는 줄이 없다. 호출자가 `CREATE_SUSPENDED` 를 줬든 안 줬든 커널엔 똑같이 1 이 간다.
  - 그러면 커널 쪽은 `PspMapThreadCreationFlags`(bit0 → bit0, run12) → `PspInsertThread +0x2e5 KeSuspendThread`(flags bit0 일 때만, run9·run12) 로 **첫 스레드를 늘 멈춰 둔다** (run13 의 멈춤 APC + `SuspendEvent` 0).
- **풀어 주기는 `NtResumeThread` 이고, `dwCreationFlags` 에 `CREATE_SUSPENDED`(4) 가 없을 때만 부른다.** [정적] `+0x5bb8 TEST` / `+0x5bc0 JNZ`. 대상은 방금 받은 스레드 핸들.
  - `NtAlertResumeThread` 는 안 쓴다. 그래서 커널에선 run13 의 `NtResumeThread +0x9b` → `PsMultiResumeThread` 길이고, `PsMultiResumeThread` 가 `ProcessFirstResume` 표시도 켜는 쪽이다 (run13 `[정적]`).
- **두 시스템 콜 사이에 부모가 하는 일: 검사 → csrss 알림(`CsrClientCallServer` `+0x4672`) → 상승·잡·앱 호환·패키지 후처리.** [정적] csrss 알림은 보안 프로세스가 아니면 반드시 지난다. 나머지는 조건부.
- **그래서 2.2 의 모양은 "늘 멈춘 채로 만들기 → 부모가 할 일(csrss 알림 등) → 부모가 직접 풀어 주기" 다.** [정적] 멈춰 두는 이유를 짐작하면, 자식이 달리기 전에 csrss 등록·앱 호환 데이터 써 넣기 같은 준비를 끝내기 위해서로 보인다 `[추측]`.
- **`CreateProcessW` 는 `CreateProcessInternalW` 를 부르는 얇은 껍데기다.** [정적] 1번째 인자(토큰) = 0, 마지막 인자 = 0, 나머지는 한 칸씩 밀어 그대로. `dwCreationFlags` 도 바꾸지 않는다 (`+0x3c` → `+0x43`). 그러니 어느 입구로 왔든 위 세 자리·ThreadFlags=1 은 같고, 달라지는 건 `dwCreationFlags` 값뿐이다.
  - `CreateProcessInternalW` 를 부르는 곳은 지도 기준 5곳. `CreateProcessW` 말고 4곳은 이번에 안 봤다.
- run3 `[관찰]` "cmd.exe 스레드가 `KiSystemServiceCopyEnd+0x28` 에서 `nt!NtCreateUserProcess` 로 들어옴" 과 어긋나는 건 없다. 다만 그때 유저 쪽 콜스택은 안 찍었으니 `CreateProcessInternalW+0x30ee` 에서 왔다는 건 아직 `[추측]` 이다.

## 아직 확인 필요

- 정적으로 못 정한 것 → 동적 (멈춤점은 유저 모드 함수라 cmd.exe 프로세스 문맥에서 거는 법을 dast 가 정한다):
  1. cmd.exe 가 실제로 넘기는 `dwCreationFlags` 에 `0x4` 가 있나. 없으면 풀어 주기가 불린다. `[추측]` 보통 없다.
  2. `NtCreateUserProcess` 가 한 번만 불리나 (재시도 길이 코드에 있음).
  3. `NtCreateUserProcess` 8번째 인자가 실제로 1 인가 → 커널 `nt!NtCreateUserProcess` 입구 `poi(@rsp+0x40)` 와 같은 질문 (Q19).
  4. 풀어 주는 호출이 정말 `KERNELBASE!CreateProcessInternalW+0x5bd3` 에서 오나: `nt!NtResumeThread` 입구 (cmd.exe 일 때만) 에서 `k` 로 유저 쪽까지.
  5. 옵션 import 검사 12곳이 참을 돌려줘서 ①~③ 중 무엇이 실제로 도나 (특히 `CsrClientCallServer`, `BasepPostSuccessAppXExtension`).
  6. 부모가 `kernel32!CreateProcessW` → `KERNELBASE!CreateProcessW` 로 오나 (kernel32 쪽은 안 봄 `[공백]`).
- 추천 멈춤점 (후보)
  - `KERNELBASE!CreateProcessInternalW+0x30e7` (call) 과 `+0x30ee` (ret): 8번째 인자 `dd @rsp+0x38 L1`, 몇 번 오나.
  - `nt!NtResumeThread` 입구 (cmd.exe 일 때만): 유저 복귀 주소가 `KERNELBASE!CreateProcessInternalW+0x5bd3` 인지, 대상 핸들 = 방금 만든 스레드인지.
  - 선택: `KERNELBASE!CreateProcessInternalW+0x466b` (`CsrClientCallServer`) 가 `NtResumeThread` 앞에 오는지.
  - 선택: `+0x5bb8` 에서 `dd @rsp+0x108 L1` (cmd.exe 의 `dwCreationFlags` 사본).
- 정적으로 더 볼 것
  - ProcessFlags bit9(`0x200`, `CREATE_SUSPENDED` 가 없을 때 켬, `+0x800`) 를 커널 `NtCreateUserProcess` 가 어떻게 쓰나.
  - csrss 쪽에서 ApiNumber `0x1001d` 가 무엇인가. (`CsrClientCallServer` 가 ntdll 것인지는 import 표로 확인됨 `[정적]`)
  - ntdll `NtCreateUserProcess`·`NtResumeThread` 스텁이 바로 시스템 콜을 하는지 (`[추측]` 그렇다).

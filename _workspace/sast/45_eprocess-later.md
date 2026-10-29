# 45 eprocess-later — 정적 분석 요청

- 날짜: 2026-10-30
- 스프린트: 03_sprint (짐 지도 바퀴, 예정 run51)
- 대기열: **Q78c2b** — `_EPROCESS` 의 둘째 조각 (Q78c2 나눔, 2026-10-30). 앞 조각 Q78c2a (sast/44, run50) 가 만들 때 212칸 중 211 을 닫음.
- 대상 빌드: Windows 11 24H2 26100.9457 x64 ntoskrnl.exe (PDB GUID 확인)
- 관련:
  - `runs/03_sprint/baggage.md` 3.11 `_EPROCESS` — 머리 **"Q78c2b 에서 볼 것"** 메모 (sast/44 가 지나가며 본 줄 후보, 이번에 확인해서 옮김), `+0x2d8 OwnerProcessId` 줄 (`PspAllocateProcess +0x710`, 사진 `P1a~P1b` 에 바뀜), 7.2 `_EPROCESS` 남은 모름 30, 7.8 (`OwnerProcessId` `0x19a8` → `0x729`, 매개변수 `ConsoleHandle` 4 → `0x60` 같은 구간)
  - plan Q65 (Q78 로 합침, 2026-10-25): `PspAllocateProcess +0x710` 부모 PID `|2` (비트 9 는 sast/29 로 풀림), **`NtSetInformationProcess +0x1ecd` 의 다른 쓰기**, **`ObCloseHandleTableEntry +0x7a9` 간접 호출 앞 인자 채움** (`PspProcessClose` 인자 이름)
  - `runs/03_sprint/run46-process-heap.md` (`RtlpGetModifiedProcessCookie` → `NtQueryInformationProcess(0x24)` — `Cookie` 칸 후보), `run49`·`run50` (양식)
  - 이전 요청 sast/44 (Q78c2a)

## 범위

- `_EPROCESS` 남은 모름 **30칸** (baggage 7.2):
  - `K1 이전` 1: `SharedCommitCharge` (줄 후보 `MiInsertSharedCommitNode +0x33e` / `MiRemoveSharedCommitNode +0x173`, 동적 E5 — 정적으로 더 좁혀지면 좁히고, 아니면 그대로 둠)
  - `K2~K3` 3: `Flags.ProcessInserted`, `ActiveThreadsHighWatermark`*, `Flags4`
  - `K3~P0` 11: `Flags2.EnableWriteVmLogging`, `ExceptionPortData`, `ExceptionPortValue`, `Flags3`*, `Flags3.EnableProcessSuspendResumeLogging`, `Flags3.EnableThreadSuspendResumeLogging`, `Flags3.EnableProcessRemoteExecProtectVmLogging`, `Flags3.EnableProcessLocalExecProtectVmLogging`, `WakeChannel`, `WakeInfo`, `BamPpmPolicy`
  - `P0~P1` 9: `Flags2.PrimaryTokenFrozen`, `Flags.CreateReported`, `Flags.LaunchPrefetched`, `Flags.ImageNotifyDone`, `ImagePathHash`, `PrefetchTrace`*, `ModifiedPageCount`*, `Flags3.AddressPolicyFrozen`, `Flags3.SecurityFreezeComplete`
  - `P1~P1a` 5: `Cookie`, `DeviceMap`, `EtwDataSource`, `InvertedFunctionTable`, `WnfContext`
  - `P1a~P1b` 1: `Win32Process`
- 더해서: **`OwnerProcessId` 둘째 쓰기** (`P1a~P1b` `0x19a8` → `0x729`) 와 옛 Q65 남은 두 가지.
- 이 구간들은 **만들기 길 밖**이다. 쓰는 쪽 후보: `PspInsertProcess`·`PspInsertThread` (K2~K3), 부모 쪽 `NtCreateUserProcess` 뒤 (KERNELBASE 가 부르는 `NtSetInformationProcess` 등, K3~P0), 프로세스·이미지 알림·프리페처 (P0~P1), 첫 스레드 시작 (`PspUserThreadStartup → PspDisablePrimaryTokenExchange`, P0~P1), **ntdll 이 부르는 시스템 콜 안에서 커널이 처음 만드는 칸** (`NtQueryInformationProcess` 쿠키, 장치 맵, WNF, ETW — P1~P1a), win32k·csrss·콘솔 쪽 (P1a~P1b).
- 칸마다 **누가 부르는 시스템 콜 / 함수 안에서** 쓰는지까지 적는다 (놓는 법: 직접 / syscall — ntdll 이 부른 syscall 안 커널 쓰기 / 다른 프로세스 (부모·csrss) 가 부른 syscall). 부르는 쪽이 사용자 모드면 그 syscall 이름과 정보 클래스까지, 사용자 모드 호출자 이름은 이름만 (`[추측]` 이면 표시).
- 칸 이름·오프셋은 `pdb_struct.py ntoskrnl.exe _EPROCESS` (비트 칸은 sast/38 `_baggage/struct__*.tsv`).
- **범위 밖**: 이미 닫힌 칸, 사용자 모드 쪽 본문 (KERNELBASE·csrss·win32k 내부 — 이름만), 종료 길.

## 알고 싶은 것

1. **"Q78c2b 에서 볼 것" 메모 확인** — `Flags.ProcessInserted` `PspInsertProcess +0x202`, `ActiveThreadsHighWatermark` `PspInsertThread +0x678`, `Flags4` `PspInsertThread +0x67f`, `PrimaryTokenFrozen` 두 줄 (`PspDisablePrimaryTokenExchange +0x22` / `PspInsertThread +0x77b` — 둘째 줄의 조건), `Flags3.AddressPolicyFrozen`·`SecurityFreezeComplete` (`PspDisablePrimaryTokenExchange +0xbb · +0xa4 · +0x111`), `BamPpmPolicy` `PspSetProcessPpmPolicy +0xa5`. 칸마다 예/아니오 + 줄.
2. **K3~P0 11칸** — `ExceptionPortData`·`ExceptionPortValue` (예외 포트 — 누가 어떤 syscall·정보 클래스로), `Flags2.EnableWriteVmLogging`·`Flags3.Enable…Logging` 넷 (ETW·감사 설정 쪽?), `WakeChannel`·`WakeInfo`. 후보: `NtSetInformationProcess` (`0x9329b0`, 19144B — 해당 정보 클래스 갈래만), 프로세스 알림 콜백. test.exe 실행에서 이 구간을 지나는 주체 (부모 cmd 의 KERNELBASE / csrss / 드라이버) 를 가를 수 있으면 가른다.
3. **P0~P1 9칸** — `Flags.CreateReported`·`ImageNotifyDone`·`LaunchPrefetched`, `ImagePathHash`, `PrefetchTrace`, `ModifiedPageCount` 쓰는 줄 (첫 스레드 시작 `PspUserThreadStartup` 쪽, 이미지 알림 `PsCallImageNotifyRoutines` 류, 프리페처 `Pf*`/`Cc*` 류 — 이름은 `_index.tsv` 로 확인).
4. **P1~P1a 5칸** — ntdll 초기화 중 시스템 콜이 커널 안에서 처음 만드는 칸:
   - `Cookie`: `NtQueryInformationProcess` 정보 클래스 `0x24` 갈래가 쿠키가 0 이면 만들어 쓰는 줄 (run46 의 `RtlpGetModifiedProcessCookie` 가 부름 — 클래스 이름 `ProcessCookie` 는 run46 에서 `[추측]` 이었음, PDB 열거형에 있으면 확인).
   - `DeviceMap`·`WnfContext`·`EtwDataSource`·`InvertedFunctionTable`: 어느 syscall 안에서 처음 쓰나 (게으른 만들기인지), ntdll 쪽 부르는 자리 이름 (flow.md·baggage 로 알 수 있으면).
5. **P1a~P1b** — `Win32Process` (win32k 쪽 `PsSetProcessWin32Process` `0xa62cd0` 를 누가 부르나 — 이름만), **`OwnerProcessId` 둘째 쓰기** (`0x19a8` → `0x729` — 어느 syscall·정보 클래스, 값 `0x729` 의 뜻 (콘솔 호스트 PID?) `[추측]` 이면 표시. 옛 Q65 의 `NtSetInformationProcess +0x1ecd` 가 이 쓰기인가 예/아니오).
6. **옛 Q65 남은 것** — `NtSetInformationProcess +0x1ecd` 의 쓰기 (칸·조건), `ObCloseHandleTableEntry +0x7a9` 간접 호출 앞 인자 채움 (`PspProcessClose` 인자 이름 — 이 바퀴에서 짐에 영향 있으면, 없으면 짧게 답만).
7. **줄기와 경로** — 칸마다 baggage.md 3장 양식 (단계 / 무엇을 / 놓는 법 / 쓴 주인 / 경로 / 근거). 단계는 `①`(커널 단계) / `②`(ntdll 단계 중 syscall) 를 사진 구간과 맞춘다. 경로 머리는 syscall 이면 `NtXxx (정보 클래스 N)` 부터.

## 대상

rva 는 `ms/decomp/ntoskrnl/_index.tsv` 기준 (2026-10-30 확인). **이미 `.c` 가 있는 함수는 다시 뽑지 않는다.**

| 모듈 | 함수 | rva | depth | 이유 |
|---|---|---|---|---|
| ntoskrnl | `PspInsertProcess` · `PspInsertThread` · `PspUserThreadStartup` · `PspDisablePrimaryTokenExchange` · `PspSetProcessPpmPolicy` · `PspCallProcessNotifyRoutines` · `NtQueryInformationProcess` · `ObCloseHandleTableEntry` · `PspProcessClose` | `.c` 있음 | 0 | 읽기만 |
| ntoskrnl | `NtSetInformationProcess` | `0x9329b0` (19144B) | 0 | 해당 정보 클래스 갈래만 (예외 포트, 소유자, 로깅 비트, wake) |
| ntoskrnl | `PsSetProcessWin32Process` `0xa62cd0` · `ExpWnfCreateProcessContext` `0xa50e24` | | 0 | `Win32Process`·`WnfContext` |
| ntoskrnl | (필요하면) 장치 맵 (`Ob*DeviceMap*`), ETW 데이터 소스, 역함수표, 이미지 알림·프리페처, 쿠키 만드는 도우미 | `_index.tsv` / `pdb_aliases.py --where` / `run_ghidra.sh refs ntoskrnl.exe <칸 오프셋 쓰는 함수>` 로 | 0 | 2~5번 답에 따라 |

- 큰 함수 (`NtSetInformationProcess`, `NtQueryInformationProcess`) 는 `_EPROCESS` 칸 쓰기 줄과 그 갈래 조건만.
- objdump 로만 본 근거는 sast/43·44 처럼 함수+오프셋으로 적고 "`.asm` 없음" 표시, 이름은 `_index.tsv` 로 다시 확인.

## 이미 아는 것

- `[정적]` run50 (sast/44): 만들 때 212칸 중 211 닫힘. 뒤 구간 줄 후보는 3.11 메모 ("Q78c2b 에서 볼 것").
- `[정적]` run46: ntdll `RtlpGetModifiedProcessCookie +0x27` 이 `NtQueryInformationProcess(0x24, 4바이트)` 를 부름.
- `[관찰]` run44: 위 30칸의 처음 값 구간 (7.2), `OwnerProcessId` `P1a~P1b` `0x19a8` → `0x729`, 매개변수 `ConsoleHandle` 4 → `0x60` (같은 구간, 매개변수는 유저 쪽 — Q78f 몫이지만 같은 원인이면 한 줄로 이어 줌).

## 받고 싶은 것 (결과서 `_workspace/sast/45_eprocess-later_result.md`)

- 맨 앞 요약 표: "30칸 (+`OwnerProcessId` 둘째 쓰기) → 아는 칸 (nt 직접 / syscall 안) / 다른 프로세스·드라이버가 부른 syscall / 여전히 모름" 숫자, 구간별.
- 질문 1~7 답 + 근거 (`.asm` 의 +0x 줄). 태그 `[정적]` / `[정적] PDB` / `[추측]` / `[공백]`.
- 칸별 "쓰는 곳" 표 (baggage.md 3.11 양식).
- 동적 질문 목록 (번호, 추천 멈춤점 — syscall 이면 입구 + `test.exe` 조건 + 정보 클래스 조건, 찍을 것, 사진 덤프로 되는지 — 덤프면 Q79 로).
- 코드는 길게 붙이지 않고 우리 말로.

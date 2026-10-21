# 08 resume-thread — 정적 분석 요청

- 날짜: 2026-10-21
- 스프린트: 02_sprint
- 대기열: **Q5a** — 2.2 첫 스레드를 풀어 주는 nt 쪽 + 멈춰 두는 쪽이 하는 일 (`_workspace/plan.md`. Q5 를 Q5a/Q5b 로 나눔)
- 관련: `runs/02_sprint/flow.md` 2.2 (`[정적]` 일부 + `[공백]`), run9·run12 / 이전 요청 sast/07

## 먼저 할 준비 (Q5b 용, 뒤에서)

다음 항목 Q5b 는 KERNELBASE 를 본다. 처음 분석하는 모듈이라, **이번 바퀴를 시작할 때 뒤에서 걸어 둔다** (기다리지 않는다):
```bash
python3 .claude/skills/ghidra-headless-export/scripts/fetch_pdb.py KernelBase.dll
python3 .claude/skills/ghidra-headless-export/scripts/pdb_aliases.py KernelBase.dll
mkdir -p ms/ghidra
setsid nohup bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh analyze KernelBase.dll \
  > ms/ghidra/kernelbase.analyze.stdout 2>&1 < /dev/null &
```
- PID 와 로그 위치를 결과서 맨 위에 적는다. 띄운 직후 로그에서 `SetPdb:` 줄과 `osgi.ee`/`SCRIPT ERROR` 없음만 확인한다.
- 끝나는 걸 기다리지 않는다. 끝났는지는 다음 바퀴(Q5b)에서 본다. 지도는 Q5b 때 만든다.
- 파일 이름 대소문자: `ms/bin/.../System32/KernelBase.dll` 이다. 모듈 이름은 스크립트가 소문자 `kernelbase` 로 만든다.

## 지도·이름에서 본 것

- `_index.tsv` 에 모두 있음 (rva, 크기): `NtResumeThread` 0xa2d600 (190), `PsResumeThread` 0xa52c50 (20), `KeResumeThread` 0x2c9b34 (205), `KeSuspendThread` 0x48ec74 (222), `KiSuspendThread` 0x265dc4 (849). 그 밖에 `NtAlertResumeThread`, `KeAlertResumeThread`, `KeForceResumeThread`, `PsSuspendThread`, `KiInsertQueueApc` 도 있다.
- 지도(`_map/NtCreateUserProcess_*`)에서 `KeSuspendThread` 는 깊이 2, caller 2곳. 직접 호출: `KiAcquireKobjectLockSafe`, **`KiSuspendThread`**, `KiExitDispatcher`, IRQL 올리고 내리기.
- `NtResumeThread` 는 `NtCreateUserProcess` 지도에 없다 (다른 시스템 콜이라 당연).

## 알고 싶은 것

1. **풀어 주는 사슬**: `NtResumeThread` → … → `KeResumeThread` 까지 호출 줄 (ret_off). `PsResumeThread`(20바이트)가 끼는지, 꼬리호출인지.
2. **`KeResumeThread` 가 하는 일**: 멈춤 횟수(suspend count)를 줄이고, 0 이 되면 무엇을 해서 스레드를 다시 달리게 하나 (어떤 객체를 신호하나·깨우나). 코드로 보이는 만큼, 필드는 오프셋 `[정적]` + 이름 `[추측]`(PDB 타입으로 확인되면 `[정적]`).
3. **멈춰 두는 쪽이 하는 일**: `KeSuspendThread` / `KiSuspendThread` 는
   - 멈춤 횟수를 올리기만 하나, 아니면 "멈춤 APC" 같은 것을 스레드에 걸어 두나 (`KiInsertQueueApc` 를 부르나)?
   - 그렇다면 **멈춘 첫 스레드가 실제로 서는 곳**은 어디인가 — 스레드가 처음 달리기 시작한 직후(아직 유저로 넘어가기 전) 그 APC 를 처리하면서 기다리는 것인지. 코드로 보이는 만큼 쓰고, 나머지는 `[추측]` + 동적 확인 거리.
   - run12: `PspInsertThread` 는 멈춘 스레드도 `KeReadyThread`(+0x5a6) 로 대기 줄에 세운다. 이것과 위 답이 어떻게 맞물리나 한 문단.
4. `NtAlertResumeThread` 와 `NtResumeThread` 의 차이 한 줄 (Q5b 에서 KERNELBASE 가 어느 쪽을 부르는지 볼 때 필요).
5. 표기 규칙(tail, indirect-cfg, alias, frag, 주인 없는 덩어리)은 reference.md 대로.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntoskrnl | `^(NtResumeThread\|PsResumeThread\|KeResumeThread\|KeSuspendThread\|KiSuspendThread\|NtAlertResumeThread)$` | 0 | Q5a. 작은 함수들이라 6개 |
| kernelbase | — | — | 이번엔 analyze 만 뒤에서 걸어 둔다 (위 "먼저 할 준비") |

- depth 0. `KiInsertQueueApc` 등 더 필요하면 지도·`_index.tsv` 로 찾고 넓힌 이유를 쓴다.

## 이미 아는 것

- `[정적]` (run9, run12) `PspInsertThread` 는 flags bit0 가 켜져 있으면 `+0x2e5 KeSuspendThread` 로 멈춰 두고, 멈춤 여부와 상관없이 `+0x5a6 KeReadyThread` 로 대기 줄에 세운다. `NtCreateUserProcess` 안에는 첫 스레드를 풀어 주는 호출이 없다.
- `[추측]` (flow.md 2.2) 부모(cmd.exe) 쪽 유저 코드(KERNELBASE)가 `NtCreateUserProcess` 가 돌아온 뒤 스레드를 풀어 준다 — Q5b 에서 본다.
- `[관찰]` (01_sprint) test.exe 첫 스레드는 결국 `PspUserThreadStartup` → `LdrInitializeThunk` 로 달렸다.

## 받고 싶은 것

- 질문 1~4 답 + 근거 (`.asm` 의 `+0x` 줄, `.c` 요약). 대표 지점은 ret_off, call_off 는 괄호.
- KERNELBASE analyze 상태 (PID, 로그, SetPdb 줄).
- 동적으로 확인할 것 + 추천 멈춤점.
- 결과서: `_workspace/sast/08_resume-thread_result.md`

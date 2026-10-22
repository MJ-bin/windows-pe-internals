# 09 create-process-internal — 정적 분석 요청

- 날짜: 2026-10-22
- 스프린트: 02_sprint
- 대기열: **Q5b** — 2.2 부모 쪽에서 첫 스레드를 풀어 주는 곳 (`_workspace/plan.md`)
- 관련: `runs/02_sprint/flow.md` 2.2 (nt 쪽 사슬은 run13 `[정적]`, 부모 유저 쪽은 `[공백]`), 그림 1 의 점선 `[KERNELBASE 추정]` 노드 / 이전 요청 sast/08 (run13)

## 먼저 할 것: KERNELBASE 지도

- KERNELBASE 는 analyze 가 끝나 있다 (sast/08 때: `ms/ghidra/kernelbase.gpr`, `ms/decomp/kernelbase/_index.tsv` 6448개, 로그 `SetPdb:` 있음). **다시 analyze 하지 않는다.**
- 지도는 아직 없다. 먼저 만든다:
  ```bash
  bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh map KernelBase.dll CreateProcessInternalW
  ```
  → `ms/disassemble/kernelbase/_map/CreateProcessInternalW_*`. 결과서 "지도에서 본 것"에 크기·깊이 2 안쪽 눈에 띄는 것을 적는다.
- 이름 확인은 `_index.tsv`, 없으면 `pdb_aliases.py --where KernelBase.dll <이름>` (묶음 N2).

## 오케스트레이터가 이미 본 것 (`_index.tsv`)

- `CreateProcessW` 0x7d5f0 (107바이트), `CreateProcessInternalW` 0x7d6e0 (**28087바이트**), `CreateProcessA`, `CreateProcessInternalA`, `CreateProcessAsUserW`, `ResumeThread` 0xf1f00 (51바이트), `BasepCreateProcessParameters` 등.
- `NtCreateUserProcess`, `NtResumeThread`, `NtAlertResumeThread` 는 `_index.tsv` 에 없다 — ntdll 에서 가져다 쓰는 함수(import)라서로 보인다 `[추측]`. 장부·지도에선 `external` / import 이름으로 나올 것.

## 알고 싶은 것

1. **`CreateProcessInternalW` 가 `NtCreateUserProcess` 를 부르는 자리** (call_off·ret_off, import 이름 그대로). 한 곳인지 여러 곳인지.
2. **그 뒤에 첫 스레드를 풀어 주는 호출이 있나** (예/아니오 + 자리):
   - `NtResumeThread` 인가 `NtAlertResumeThread` 인가 (run13: 둘은 다른 길 — `NtResumeThread` 만 `PsMultiResumeThread` 를 거침), 아니면 `ResumeThread` 같은 KERNELBASE 함수를 거치나.
   - **언제 부르고 언제 안 부르나**: 호출자가 준 생성 플래그 중 "멈춘 채로 만들어 달라"(`CREATE_SUSPENDED`, 값 4 로 알려짐 `[추측]`) 를 보고 건너뛰는지. 그 플래그를 검사하는 명령 줄을 근거로.
   - `NtCreateUserProcess` 와 풀어 주기 **사이**에 하는 큰 일을 하는 일 단위로 1~3개 (예: CSR/SxS 알림, 앱 호환성 등 — 이름과 ret_off, 역할 한 줄. 잡일은 개수만).
3. **커널에 넘기는 ThreadFlags 의 bit0**: `CreateProcessInternalW` 가 `NtCreateUserProcess` 에 넘기는 스레드 플래그(인자 순서상 8번째, `ThreadFlags`)에 bit0(멈춘 채 만들기)를 켜는가? 항상 켜나, 조건부인가 (run9·run12: 이 bit0 가 켜지면 커널 `PspInsertThread +0x2e5` 에서 `KeSuspendThread`). 코드로 보이는 만큼.
   - 이게 "항상 켬 + 나중에 직접 풀어 줌" 모양이면 2.2 의 그림이 완성된다. 아니면 그렇다고.
4. 위 1~3 이 `CreateProcessW` → `CreateProcessInternalW` 길에서 그대로인지 (`CreateProcessW` 107바이트가 무엇을 넘기나 한 줄).
5. 표기 규칙(tail/frag, indirect-cfg, alias, 조각·덩어리·떨어진 범위, `cfg:stack[…]`)은 reference.md 대로. `.c` 는 reference "`.c` 읽을 때 주의"대로 — 흐름·플래그 검사는 `.asm` 으로.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| kernelbase | `^(CreateProcessInternalW\|CreateProcessW\|ResumeThread)$` | 0 | Q5b |

- depth 0. `CreateProcessInternalW` 가 너무 크면 장부(`_calls.tsv`)와 `.asm` 으로 질문에 필요한 구간만 읽는다. 다른 함수가 꼭 필요하면 지도로 찾고 넓힌 이유를 쓴다.

## 이미 아는 것

- `[정적]` (run13) 커널 쪽 풀어 주기 사슬: `NtResumeThread +0x9b PsMultiResumeThread → +0x25 KeResumeThread → +0x87 KiResumeThread → +0xab KiTryUnwaitThread`. `NtAlertResumeThread` 는 `KeAlertResumeThread` 길 (PsMultiResumeThread 안 거침).
- `[정적]` (run9, run12) `NtCreateUserProcess` 안엔 풀어 주는 호출이 없다. ThreadFlags → `PspMapThreadCreationFlags`(+0xdd3) → `PspInsertThread` 4번째 인자 bit0 → `KeSuspendThread`.
- `[관찰]` (01_sprint run3) 부모는 `cmd.exe`, 커널에 `KiSystemServiceCopyEnd+0x28` 로 들어와 `NtCreateUserProcess` 를 불렀다. 유저 쪽 콜스택은 찍지 않았다.
- `[추측]` cmd.exe 는 보통 `CREATE_SUSPENDED` 없이 `CreateProcessW` 를 부른다 — cmd.exe 자체는 이번 범위 밖 (분석 안 함).

## 받고 싶은 것

- 지도에서 본 것 (크기, 깊이, 눈에 띄는 external·import).
- 질문 1~5 답 + 근거 (`.asm` 의 `+0x` 줄). 대표 지점은 ret_off, call_off 는 괄호.
- 동적으로 확인할 것 + 추천 멈춤점 (유저 모드 함수라 커널 디버거에서 cmd.exe 프로세스 문맥으로 거는 법 주의 — dast 몫이니 후보만).
- 결과서: `_workspace/sast/09_create-process-internal_result.md`

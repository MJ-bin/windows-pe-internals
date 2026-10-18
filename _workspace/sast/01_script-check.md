# 01 script-check — 정적 분석 요청 (스크립트 시험)

- 날짜: 2026-10-19
- 스프린트: 02_sprint
- 관련: 대기열 항목 아님 (Q1 전 준비). `_workspace/plan.md` 결정 기록 2026-10-19
- 목적: 하네스의 Ghidra 스크립트(`.claude/skills/ghidra-headless-export/scripts/`)가 **진짜 Ghidra 11.3.2에서 처음** 도는지, 오프셋이 01_sprint 디버거 관찰과 맞는지 본다.

## 꼭 지킬 것 (이번 요청만의 규칙 — 에이전트 기본 에러 표보다 우선)

1. **`.claude/` 아래 스크립트를 절대 고치지 않는다.** 오타 수준이라도 고치지 않는다.
   - 문제가 보이면 그 자리에서 멈추고: 어느 스크립트 몇째 줄, 에러 메시지(로그 끝 30줄), 원인 추정, 고친다면 어떻게 고칠지(제안만)를 결과서에 쓴다. 사용자가 직접 고친다.
   - 시험용 임시 사본을 만들어 돌려 보는 것도 하지 않는다 (사용자가 고친 뒤 다시 돌린다).
2. **ntoskrnl analyze를 맨 먼저 뒤에서 걸어 둔다.** 이 에이전트가 끝나도 계속 돌아야 하므로 터미널에서 떼어서 띄운다. 예:
   ```bash
   setsid nohup bash .claude/skills/ghidra-headless-export/scripts/run_ghidra.sh analyze ntoskrnl.exe \
     > ms/ghidra/ntoskrnl.analyze.stdout 2>&1 < /dev/null &
   ```
   - PID를 결과서에 적는다. 끝날 때까지 기다리지 않는다.
   - 띄운 뒤 1~2분 안에 stdout/로그를 한 번 보고, 바로 죽었는지(java·스크립트 컴파일 에러)만 확인한다. 바로 죽었으면 원인을 보고하고 ntdll 시험은 그대로 이어간다.
3. ntdll analyze는 그동안 **앞에서**(끝날 때까지 기다리며) 돌린다. 서버 메모리 사용 가능 37G라 둘 다 `MAXMEM` 기본 8G로 충분하다.

## 환경 (오케스트레이터가 확인함)

- Java: Oracle JDK **25.0.2** (Ghidra 11.3.2는 최소 21, 최대 제한 없음. 25에서 도는지는 이번이 처음) — 다른 쓸 만한 JDK 없음
- GHIDRA_HOME: 환경변수 없음. 스크립트 기본값 `~/tools/ghidra_11.3.2_PUBLIC_20250415/ghidra_11.3.2_PUBLIC` 에 있음
- PDB: 이미 받음 (`ms/sym/ntoskrnl.exe.pdb`, `ms/sym/ntdll.dll.pdb`). 커널 GUID `C29EBFB06B78B3C020DCA66D99713F9E1` = 기준값과 같음. 다시 받지 않아도 된다.

## 알고 싶은 것

1. 스크립트 4개(`run_ghidra.sh`, `SetPdb.java`, `ExportIndex.java`, `ExportFunctions.java`)가 에러 없이 도는가? (예/아니오, 스크립트별로)
   - JDK 25 관련 경고·에러가 있으면 그대로 적는다.
2. PDB가 실제로 붙었는가? (`_index.tsv`에 `LdrpInitializeProcess` 같은 이름이 있고 `FUN_` 이 드문가. 전체 함수 수와 `FUN_` 수)
3. 아래 "알려진 사실" 표가 `_calls.tsv` 의 `ret_off`와 맞는가? 줄마다 일치 / 불일치.
   - 01_sprint flow.md 그림 라벨 중 일부는 call 위치인지 ret 위치인지 섞여 있을 수 있다. 그러니 줄마다 **call_off와 ret_off 둘 다** 적고, 관찰 값이 어느 쪽과 같은지 적는다.
4. export 결과물 형식이 reference.md와 같은가? (`.c` 머리 주석, `.asm` 의 `+0x…`/`ret=`, `_calls.tsv` 4열, 간접 호출 `indirect:` 표기, `_index.tsv` 열)
5. 불일치나 이상한 점이 있으면 원인이 **스크립트**인지 **바이너리**인지. asm 파일로 직접 손계산해서 가린다 (함수 시작 주소 + 오프셋 = 명령 주소).
6. ntoskrnl analyze가 아직 살아 있는가, 로그 위치는 어디인가.

## 대상

| 모듈 | 함수 이름 또는 정규식 | depth | 이유 |
|---|---|---|---|
| ntdll | `^(LdrInitializeThunk\|LdrpInitializeInternal\|LdrpInitialize\|LdrpInitializeProcess\|LdrpInitializeNode\|LdrpLoadDll)$` | 0 | 알려진 사실 대조. depth 0으로 충분 (`_calls.tsv`는 caller 기준) |

- depth 1은 이번엔 안 해도 된다. 단, depth 1 동작도 시험하고 싶으면 `^LdrInitializeThunk$` 하나만 depth 1로 (callee가 적어서 파일이 몇 개 안 나온다).
- 이 함수들은 대기열 Q8·Q9에서 다시 쓰이니 뽑은 파일은 지우지 않는다.

## 이미 아는 것 (01_sprint 디버거 콜스택 `[관찰]`)

| caller | 관찰 오프셋 | callee | 비고 |
|---|---|---|---|
| `LdrInitializeThunk` | `+0xe` | `LdrpInitializeInternal` | 콜스택 |
| `LdrpInitializeInternal` | `+0x5a` | `LdrpInitialize` | 콜스택 |
| `LdrpInitialize` | `+0x16c` | `LdrpInitializeProcess` | 콜스택 |
| `LdrpInitializeProcess` | `+0x1bbb` (ret) / `+0x1bb6` (call, BEFORE-GRAPH 멈춤점) | `LdrpInitializeGraphRecurse` | 핵심 대조 항목 |
| `LdrpInitializeProcess` | `+0x19a2` | `LdrpInitializeKernel32Functions` | ret인지 call인지 확인 |
| `LdrpInitializeProcess` | `+0x1c99` | `LdrpCallTlsInitializers` | ret인지 call인지 확인 |
| `LdrpInitializeNode` | `+0x16b` / `+0x19c` | `LdrpCallTlsInitializers` / `LdrpCallInitRoutine` | 콜스택 |
| `LdrpLoadDll` | `+0x100` | `LdrpLoadDllInternal` | 돌아올 주소 − 함수 시작 주소로 계산한 값 |

## 받고 싶은 것

- 질문 1~6 답 (결과서 `_workspace/sast/01_script-check_result.md`, reference.md 양식)
- 스크립트 문제가 있으면 맨 위에 "스크립트 문제" 절: 파일·줄·증상·원인 추정·고칠 방법 제안
- 판정: 분석성공(다 맞음) / 부분성공 / 실패
- 동적 확인 거리는 없어도 된다 (이번은 도구 시험)

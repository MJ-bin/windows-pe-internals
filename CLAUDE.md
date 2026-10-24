# windows-pe-internals

## 하려는 것

test.exe를 실행하면 Windows가 **커널에서 한 일 → 유저(ntdll)에서 한 일**을 어떤 순서로 하는지
처음부터 끝까지 쭉 따라가서, **한 장의 흐름도**로 그린다.
이 지도는 `../PE_emulator`(진짜 nt·ntdll과 같은 이름·호출관계로 짓는 에뮬레이터)의 바탕이다.

- 흐름도 범위: 프로세스 생성 → exe 진입점(PE `AddressOfEntryPoint`)까지. (새 스레드·종료는 그다음)
  - 진입점 뒤(CRT 등 컴파일러가 넣은 코드)는 OS가 아니라서 지도엔 점선 상자 하나 + `main`만 둔다.
  - 그 안에서 ntdll을 다시 부르는 것(`LoadLibraryExW` 등)만 화살표로 남긴다.
- 지금 목표 (03_sprint~): **짐 지도.** 초기화 동안(프로세스 생성 → `NtContinue` 은퇴) nt·ntdll이 세팅하는 짐을 빠짐없이 센다.
  - 짐마다 적는 것: 공간(유저 = 에뮬레이터 가상 주소 공간 / 커널 = 에뮬레이터 바깥, nt 엔진·syscall로만 닿음), 단계, 놓는 법, 경로(지도 기준 함수 사슬).
  - 어디까지 구현할지는 목록이 다 된 뒤 사용자와 정한다. 그 전엔 "구현할지"로 거르지 않는다.
- 자세함:
  - 흐름도(`flow.md`): **단계마다 대표 지점만** (수십 개). 명령 하나하나를 다 따라가는 건 안 한다.
  - 짐 지도(`baggage.md`): 짐을 정하는 **줄기 위에서는 끝까지** (더 부르는 게 없는 함수, 또는 syscall). 줄기 밖 callee는 이름만. 공용 부품(복사·할당·문자열·락 등)은 한 번만 보고 이름으로 가리킨다.
- 순서: **정적분석(디컴파일) 먼저**, 정적으로 못 본 것만 **동적분석(커널 디버거)**.
  - 짐이 빠짐없는지는 상태 사진(멈춘 순간의 덤프)으로 확인한다. 정적으로는 포인터로 쓰는 칸·간접 호출을 놓칠 수 있다.
- 분석 순서: 시간 순서로 지도를 넓힌다 (**nt → ntdll → 빈 곳 채우기 → 짐 지도**). 할 일은 `_workspace/plan.md` 대기열에 줄 세우고 **먼저 들어온 것부터** 한다. 새로 생긴 문제는 맨 뒤에 붙이고, 애매하면 사용자와 정한다.

## 방법

- **정적분석**: 서버의 Ghidra headless로 `ms/bin`의 MS 바이너리를 PDB와 함께 디컴파일·디스어셈블한다.
  "코드에 이렇게 쓰여 있다"를 본다.
- **동적분석**: 커널 디버거 하나로 커널과 유저를 같이 본다.
  - 유저 디버거(cdb)는 test.exe 안만 보인다. 커널 디버거는 커널 함수도, test.exe 안의 ntdll 함수도 멈출 수 있다.
  - 대표 지점 = 대부분 **함수 입구**, 몇 개만 **값이 바뀌는 순간**. 목록은 `points.md`.
  - 커널 멈춤점은 모든 프로세스에 걸린다 → test.exe일 때만 멈추게 조건을 붙인다.

## 환경

- 서버: Ubuntu 22.04. 사용자가 ssh로 붙어 Claude Code를 돌린다.
  - Ghidra 11.3.2: `~/tools/ghidra_11.3.2_PUBLIC_20250415/ghidra_11.3.2_PUBLIC`
  - MS 바이너리: `ms/bin/DLL - windows11 Education 24H2 26100.9457/win11-24H2-26100.9457/System32/` (경로에 띄어쓰기)
  - msdl.microsoft.com 에 접속된다 (PDB 받기).
- VM 두 대 (VMware). Claude는 VM에 못 들어간다. 사용자가 직접 만진다.
  - 타깃 VM: Windows 11 24H2, OS 빌드 26100.9457, Secure Boot 꺼짐. test.exe를 여기서 돌린다.
  - 디버거 VM: WinDbg. KDNET으로 타깃 VM에 붙는다.
- 심볼: `srv*C:\symbols*https://msdl.microsoft.com/download/symbols`
- 테스트 프로그램: `sample/` (exe TLS 콜백, dll1 → dll2 import, CreateThread 1개, main return).

## 폴더

| 폴더 | 내용 | 커밋 |
|---|---|---|
| `runs/NN_sprint/` | 최종 산출물: `runN-키워드.md` (run 문서), `flow.md` (흐름도), `baggage.md` (짐 지도, 03_sprint~), `runN-키워드-해설.md` (쉬운 말 해설서) | O |
| `_workspace/sast/` | 정적 요청서 `NN_키워드.md`, 결과서 `NN_키워드_result.md` | O |
| `_workspace/plan.md` | 분석 진행표 (대기열, 결정 기록) | O |
| `_workspace/dast/` | 동적 요청서 `NN_키워드.md`, 실행 안내 `NN_키워드_runbook.md` | O |
| `ms/logs/` | 디버거 로그 `NN_키워드.txt`, 판정 `NN_키워드_result.md` | O |
| `ms/dump/` | 상태 사진 덤프 원본 (짐 지도) | **X** (폴더만) |
| `ms/bin/`, `ms/sym/`, `ms/ghidra/` | MS 바이너리, PDB, Ghidra 프로젝트 | **X** |
| `ms/decomp/`, `ms/disassemble/` | MS 코드의 디컴파일·디스어셈블 결과 | **X** (폴더만) |
| `.claude/agents/`, `.claude/skills/` | 하네스 (에이전트, 스킬) | O |

## 규칙 (하네스)

- 최종 산출물은 `runs/NN_sprint/` 의 run 문서, `flow.md`, `baggage.md`.
- 중간 산출물은 `_workspace/` (요청서·결과서)와 `ms/logs/` (로그·판정)에 둔다.
- 에이전트는 `.claude/agents/`, 스킬은 `.claude/skills/` 에 정의한다.
- 세션은 `orchestrator` 에이전트로 시작한다 (`.claude/settings.json` 의 `"agent"`).
  - 누가 언제 무엇을 하는지는 `pe-flow-cycle` 스킬의 Workflow.
  - decompiler(정적) · dast-mentor(동적 안내·판정) · flow-writer(문서) · explainer(해설서) · committer(커밋)
- 에이전트끼리는 **파일로** 주고받는다. 요청서만 읽고 일할 수 있게 쓴다.
- 번호: sast·dast는 따로 `NN` (두 자리), `ms/logs`는 dast 번호를 따른다. run 번호는 스프린트가 바뀌어도 이어진다. 번호·지금 스프린트는 `next_index.py`로 계산한다 (여기에 적지 않는다).
- 키워드: 영어 소문자·숫자·하이픈 (WinDbg 명령에 들어가므로 한글 금지).
- **이 파일에는 잘 안 바뀌는 규칙만 둔다.** 바퀴마다 바뀌는 것은 다른 곳에 쓴다: 새 발견은 `runs/NN_sprint/flow.md`·run 문서, 진행 상황·다음 할 일은 `_workspace/plan.md`.
- 하네스(이 파일, `.claude/`)는 **오케스트레이터만, 사용자 검토 뒤에** 고친다.
  - 순서: 바꿀 파일·이유·바뀌는 줄을 사용자에게 보여줌 → 사용자 OK → 고침 → **하네스 파일만 담은 커밋 하나** (`fix|feat|docs(harness)`) → 커밋 해시를 `_workspace/plan.md` 결정 기록에 남김.
  - 되돌리기는 그 커밋을 `git revert` 한다 (사용자 허락 뒤).
  - 다른 에이전트는 고치지 않고 제안만 한다. `.claude/settings.json` 의 권한(permissions)은 사용자가 직접 고친다.

## 지킬 것

- **추측과 확인을 섞지 않는다.**
  - 커널 함수 이름(`nt!Psp*`, `nt!Mm*` 등)과 구조체 오프셋은 Windows 버전마다 다르다.
  - 이름을 쓰기 전에 `_index.tsv`(정적)나 VM의 `x nt!Psp*`(동적)로 먼저 확인한다. `_index.tsv` 에 없으면 바로 "없다"고 하지 말고 `pdb_aliases.py --where <모듈> <이름>` 으로 PDB 이름을 한 번 더 찾는다 (Ghidra 가 같은 함수를 다른 이름으로 잡을 수 있다).
  - 표시: `[관찰]` 디버거로 본 것 · `[정적]` 디컴파일 코드에 있는 것 · `[소스]` sample 소스 · `[추측]` · `[공백]`
  - run 문서 표에서는 `[26100.9457 확인]` / `[정적]` / `[확인 필요]`.
- 디버거가 붙어서 달라지는 점이 보이면 따로 적는다.
- MS 바이너리·심볼·메모리 덤프·디컴파일 결과는 커밋하지 않는다 (`.gitignore`). 문서에는 코드를 길게 붙이지 않고 우리 말로 요약한다.
- 커밋: Conventional Commits, 한국어 요약. **Claude를 공동 작성자로 넣지 않는다. push는 사용자가 직접 한다.**

## 말하는 방식

- 중학생도 알아듣는 쉬운 말. 짧은 문장, 글머리 기호, 넉넉한 줄바꿈.
- 피할 수 없는 전문 용어엔 일상 비유를 붙인다. 없는 단어를 지어내지 않는다.
- 한 번에 개념 하나. 여러 개념을 한꺼번에 섞지 않는다.
- 모르면 모른다고 한다.
- 톤: 딱딱한 말투 대신, 눈치 빠른 동료처럼 편하게.

## 참고 (예전 레포)

- https://github.com/MJ-bin/PELdr — 같은 test.exe로 유저 쪽을 먼저 확인한 기록.
  - `docs/PE_execution_process.md`: 예상 흐름 25단계와 근거. 여기서 단계 번호를 빌려 쓴다.
  - `CLAUDE.md`의 "내가 정리한 PE 실행 흐름": 26100.9457에서 확인된 ntdll 함수 이름들.
- 참고만 한다. 거기 도구(cdb 기록기, 에뮬레이터)는 가져오지 않는다.
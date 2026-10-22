---
name: windbg-kd-runbook
description: WinDbg 커널 디버거(KDNET)로 타깃 VM의 test.exe를 따라가는 실행 안내(runbook)를 만들고, 사용자가 붙여넣은 디버거 로그를 사용자실수/예기치못한상황발생/분석성공 중 하나로 판정하는 절차. 멈춤점(bp, ba) 거는 법, /p 필터, .logopen, BEFORE-GRAPH 기준점, 01_sprint에서 겪은 함정 목록이 들어 있다. "runbook 만들어줘", "디버거 명령 알려줘", "VM에서 뭐 해야 해", "멈춤점 어떻게 걸어", "g 한 번 더?", "로그 붙였어", "판정해줘", "왜 안 걸려", "Syntax error" 같은 말이 나오면 쓴다.
allowed-tools: Read Write Edit Glob Grep
---

# windbg-kd-runbook Skill

사람이 VM 두 대를 직접 만지는 일을 **틀리지 않게** 안내하고, 결과 로그를 판정한다.
Claude는 VM에 못 들어간다. 그래서 안내가 곧 실험 설계다.

## 고정 환경 (01_sprint에서 확인)

| 항목 | 값 |
|---|---|
| 타깃 VM | Windows 11 24H2 **26100.9457**, 192.168.100.133, NIC e1000e, Secure Boot·BitLocker·자동 업데이트 꺼짐 |
| 디버거 VM | WinDbg, KDNET 192.168.100.132 port 50000 |
| 커널 pdb | ntkrnlmp.pdb `C29EBFB06B78B3C020DCA66D99713F9E1` |
| 심볼 | `srv*C:\symbols*https://msdl.microsoft.com/download/symbols` + `.sympath+ C:\windows-pe-internals\sample\bin` (test/dll pdb) |
| test.exe | 디버거 VM에서 빌드 → 타깃에 복사 (방법 B, pdb 짝 맞음). 타깃 실행 위치: `C:\windows-pe-internals-main\windows-pe-internals-main\sample\bin\` |
| 기준점 | cmd의 `nt!PspCallProcessNotifyRoutines` (test.exe EPROCESS 얻기), `ntdll!LdrpInitializeProcess+0x1bb6` (BEFORE-GRAPH: dll1·dll2가 이미 올라와 있음), `test!main` (끝) |

## Workflow — 준비 모드 (runbook 만들기)

1. 동적 요청서 `_workspace/dast/NN_키워드.md` 를 읽는다. 관련 정적 결과서가 적혀 있으면 그것도 읽는다.
2. 멈춤점 이름이 **이 빌드에 있는지** 근거를 찾는다: `_index.tsv`(없으면 `pdb_aliases.py --where <모듈> <이름>` — WinDbg 는 PDB 이름을 쓴다), run 문서, flow.md. 근거가 없으면 runbook 첫 단계에 `x` 명령으로 확인하는 단계를 넣는다.
   - 함수 입구에 거는 멈춤점은 **별칭 무리인지**도 본다: `python3 .claude/skills/ghidra-headless-export/scripts/pdb_aliases.py --lookup <모듈> <이름>`. 무리면 [pitfalls.md](pitfalls.md) P19 대로 건다.
3. [runbook-template.md](runbook-template.md) 의 표준 뼈대에 이번 멈춤점을 끼워 넣는다.
   - 멈춤점마다: **멈출지 / 기록만**, `/p` 필터 방법, 찍을 것(`k`, `r`, `ln`)을 정한다.
   - 지나가는 곳은 기록만: `".echo ==== NAME; ? @$thread; k 6; g"`
4. [pitfalls.md](pitfalls.md) 에서 이번 계획에 해당하는 함정을 골라 runbook "자주 하는 실수"에 넣는다.
5. 빈 로그 파일 `ms/logs/NN_키워드.txt` 를 만든다 (내용 없이. 빈 내용이 안 써지면 줄바꿈 하나).
6. `_workspace/dast/NN_키워드_runbook.md` 를 쓴다.
7. 자체 점검: 명령에 한글 없음 / `.logopen` 앞에 Break / 커널 멈춤점마다 `/p` 또는 `.if (@$proc == ...)` / 공용 출구에 끝 표시 없음 / 고정 DLL 주소 없음 / 끝은 `test!main` / 마지막에 `bc *` → `.logclose`.

## Workflow — 질문 답하기 (실행 중)

1. 사용자가 붙인 출력에서 **지금 몇 단계인지**부터 찾는다.
2. 다음 명령에 넣을 값(EPROCESS, 주소)을 출력에서 찾아 **채운 명령**을 준다.
3. 계획을 바꿔야 하면 runbook 파일도 같이 고치고 "바뀐 곳"을 말한다.
4. 모르면 모른다고 한다. 추측이면 추측이라고 붙인다.

## Workflow — 판정 모드

1. `ms/logs/NN_키워드.txt` 를 읽는다. 비어 있으면(공백·줄바꿈뿐이어도) 판정하지 않고 "로그가 비어 있음"으로 돌려준다.
2. 요청서의 **성공 기준**과 runbook의 단계를 로그와 하나씩 대조한다.
   - 로그의 함수 이름(콜스택, `ln`)이 별칭 무리면(`pdb_aliases.py --lookup`), 그 이름을 그대로 믿지 말고 판정 문단에 "별칭 무리, 이름 N개"라고 적는다 (P19).
3. [judge.md](judge.md) 기준으로 판정 하나를 고른다: **사용자실수 / 예기치못한상황발생 / 분석성공**.
4. `ms/logs/NN_키워드_result.md` 에 **한 문단**으로 쓴다 (무엇을 / 어떻게 / 결과).
5. 재시도가 필요하면 고칠 점을 오케스트레이터에게 돌려주는 메시지에 적는다 (결과 파일엔 한 문단만).

## 급할 때 (바로 답할 것)

- **호스트가 멈춘 것 같다**: 디버거가 타깃을 세워 둔(Break) 상태에서 타깃 VM 화면을 클릭한 것. `Ctrl+Alt`로 VMware 입력을 풀고, 디버거 VM에서 `g`.
- **타깃이 안 움직인다**: 디버거 VM WinDbg 창에서 `g`. 조건 멈춤점이 `.else {gc}` 없이 걸려 있는지 `bl`.
- **VM 잠시 멈추기**: 타깃 먼저 일시정지, 다시 켤 땐 디버거 먼저.

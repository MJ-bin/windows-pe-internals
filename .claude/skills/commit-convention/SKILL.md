---
name: commit-convention
description: git diff를 읽고 바뀐 내용을 알 수 있는 키워드(run 번호, 함수 이름, 절 번호)가 담긴 Conventional Commits 메시지를 한국어로 써서 커밋하는 절차. 커밋하면 안 되는 파일(MS 바이너리·심볼·디컴파일 결과) 거르기, 공동 작성자 줄 넣지 않기, push 하지 않기를 포함한다. "커밋해줘", "커밋 메시지 써줘", "변경사항 정리해서 커밋", "git commit" 같은 말이 나오면 쓴다.
allowed-tools: Bash Read Write Grep Glob
---

# commit-convention Skill

변경을 **목적별로 묶어** 커밋한다. 메시지만 읽어도 무엇이 바뀌었는지 알 수 있게 키워드를 넣는다.
push는 하지 않는다 (사용자가 직접 한다).

## 메시지 형식

```
<type>(<scope>): <한국어 요약, 키워드 포함, 72자 안쪽>

- <파일 또는 묶음>: <무엇이 바뀌었나, 함수 이름·오프셋·절 번호>
- ...
```

| type | 쓰는 때 (이 레포 기준) |
|---|---|
| `docs` | run 문서 추가, flow.md에 새 사실 추가 |
| `fix` | 틀렸던 내용 고침 (flow.md 정정, run 정정, 머메이드 그림 고침), 스크립트 버그 수정 |
| `feat` | 하네스에 새 기능 (새 스크립트, 새 에이전트·스킬) |
| `refactor` | 내용은 같고 구조만 바꿈 |
| `chore` | 요청서·결과서(`_workspace`), `ms/logs`, `.gitignore`, `.gitkeep` |
| `init` | 레포·구조 처음 만들 때 (이 레포에서 이미 쓰던 type) |

| scope | 대상 |
|---|---|
| `02_sprint` 등 | `runs/NN_sprint/` |
| `harness` | `.claude/`, `CLAUDE.md` |
| `workspace` | `_workspace/` |
| `logs` | `ms/logs/` |
| `sample` | `sample/` |

예 (이 레포에서 이미 쓴 모양):
```
docs(01_sprint): nt!KiSystemServiceCopyEnd -> child!main 실행 과정 분석 추가
fix: 머메이드 다이어그램 수정(caller, callee 관계 표현)
```

새 예:
```
docs(02_sprint): run9 kernel32 매핑 시점 추가 (LdrpLoadDllInternal)

- run9-mapview.md: 정적(sast/01) + 동적(dast/02) 결과
- flow.md 2.5: [공백] → [관찰] kernel32 매핑 caller
- flow.md 4.1: "(1) kernel32 준비" 줄 해결
```

## Workflow

1. **상태 보기**
   ```bash
   git status --porcelain
   git diff --stat
   git diff
   ```
   - 새 파일(`??`)은 내용을 `Read`로 확인한다.
2. **묶기**: 목적이 다른 변경은 커밋을 나눈다. 순서: `chore`(요청서·로그) → `docs`/`fix`(runs) → `feat`/`fix`(harness).
   - 한 바퀴(요청서 + 로그 + run 문서 + 해설서 + flow.md)는 보통 2개: `chore(workspace|logs)` 1개 + `docs(NN_sprint)` 1개. 해설서 `runN-키워드-해설.md` 는 run 문서와 같은 `docs(NN_sprint)` 커밋에 넣는다.
   - **하네스(`.claude/`, `CLAUDE.md`) 변경은 다른 파일과 절대 같은 커밋에 넣지 않는다.** 오케스트레이터가 "묶음"을 여러 개 주면 묶음 하나 = 커밋 하나 (나중에 `git revert` 로 하나씩 되돌릴 수 있게).
3. **스테이징**: 파일을 **경로로 하나씩** `git add <경로>`. `git add -A`, `git add .` 금지.
4. **파일 검사**:
   ```bash
   bash .claude/skills/commit-convention/scripts/check_commit.sh staged
   ```
   - `[X]`가 나온 파일은 `git restore --staged <경로>` 로 빼고, 뺀 이유를 보고에 적는다. 사용자가 직접 허락한 경우에만 `ALLOW_DECOMP=1` 로 다시 검사.
5. **메시지 쓰기**: diff에서 키워드를 뽑는다 — run 번호, 새로 확인한 함수 이름, 바뀐 절 번호, 태그 변화(`[추측]→[관찰]`). 메시지를 `Write`로 `_workspace/.commit_msg` 에 쓴다 (`.git/` 안은 보호 경로라 매번 허락을 묻는다. 이 파일은 `.gitignore` 대상).
6. **메시지 검사**:
   ```bash
   bash .claude/skills/commit-convention/scripts/check_commit.sh msg _workspace/.commit_msg
   ```
7. **커밋**: `git commit -F _workspace/.commit_msg` → `git log -1 --format='%H%n%B'` 로 공동 작성자 줄이 없는지 다시 본다.
8. 묶음이 더 있으면 3번부터. 끝나면 `git status --porcelain` 이 비었는지(또는 남긴 파일이 무엇인지) 확인.

## 하지 않는 것

- `git push`, `git commit --amend`, `git rebase`, `git reset --hard` — 하지 않는다. (push는 설정에서도 막혀 있다)
- `Co-Authored-By`, `Generated with`, `Claude-Session` 같은 줄 — 넣지 않는다.
- 커밋 훅 건너뛰기(`--no-verify`) — 하지 않는다.

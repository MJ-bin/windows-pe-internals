#!/usr/bin/env python3
"""요청서 번호(NN), run 번호, 지금 스프린트를 계산한다. 사람마다 세는 법이 달라지지 않게 하려고 만든 도구.

사용법 (레포 루트에서):
  python3 .claude/skills/pe-flow-cycle/scripts/next_index.py next sast     # 다음 정적 요청 번호  예) 03
  python3 .claude/skills/pe-flow-cycle/scripts/next_index.py next dast     # 다음 동적 요청 번호  예) 02
  python3 .claude/skills/pe-flow-cycle/scripts/next_index.py latest sast   # 가장 최근 정적 요청서 경로
  python3 .claude/skills/pe-flow-cycle/scripts/next_index.py latest dast   # 가장 최근 동적 요청서 경로
  python3 .claude/skills/pe-flow-cycle/scripts/next_index.py next run      # 다음 run 번호 (모든 스프린트 통틀어) 예) 9
  python3 .claude/skills/pe-flow-cycle/scripts/next_index.py sprint        # 지금 스프린트 폴더  예) runs/02_sprint
  python3 .claude/skills/pe-flow-cycle/scripts/next_index.py keyword <kw>  # 키워드가 규칙에 맞는지 (영어 소문자·숫자·하이픈)

규칙
  - 요청서 이름: NN_키워드.md  (NN = 두 자리 이상 숫자, 키워드 = 영어 소문자/숫자/하이픈)
  - 같은 폴더의 NN_키워드_result.md, NN_키워드_runbook.md 는 "요청서"로 세지 않는다.
  - sast 와 dast 번호는 따로 센다. ms/logs 의 번호는 dast 번호를 그대로 쓴다.
  - run 번호는 runs/*/runN-*.md 중 가장 큰 N + 1 (스프린트가 바뀌어도 이어진다).
  - 지금 스프린트 = runs/NN_sprint 중 번호가 가장 큰 폴더.
종료 코드: 찾을 게 없거나 규칙 위반이면 1
"""
import os
import re
import subprocess
import sys

REQ = re.compile(r'^(\d{2,})_([a-z0-9][a-z0-9-]*)\.md$')
SUFFIX = ('_result', '_runbook')
KW = re.compile(r'^[a-z0-9][a-z0-9-]{1,39}$')
RUN = re.compile(r'^run(\d+)-[^/]+\.md$')
SPRINT = re.compile(r'^(\d{2,})_sprint$')


def root():
    try:
        return subprocess.check_output(['git', 'rev-parse', '--show-toplevel'], text=True,
                                       stderr=subprocess.DEVNULL).strip()
    except Exception:
        return os.getcwd()


def requests(kind):
    d = os.path.join(root(), '_workspace', kind)
    out = []
    if os.path.isdir(d):
        for f in os.listdir(d):
            m = REQ.match(f)
            if m and not m.group(2).endswith(SUFFIX) and not any(f.endswith(s + '.md') for s in SUFFIX):
                out.append((int(m.group(1)), f))
    return sorted(out)


def runs():
    base = os.path.join(root(), 'runs')
    nums = []
    if os.path.isdir(base):
        for sp in os.listdir(base):
            p = os.path.join(base, sp)
            if os.path.isdir(p):
                for f in os.listdir(p):
                    m = RUN.match(f)
                    if m:
                        nums.append(int(m.group(1)))
    return nums


def sprints():
    base = os.path.join(root(), 'runs')
    out = []
    if os.path.isdir(base):
        for sp in os.listdir(base):
            m = SPRINT.match(sp)
            if m and os.path.isdir(os.path.join(base, sp)):
                out.append((int(m.group(1)), sp))
    return sorted(out)


def main(a):
    if not a:
        print(__doc__)
        return 1
    if a[0] == 'next' and len(a) == 2 and a[1] in ('sast', 'dast'):
        r = requests(a[1])
        print('%02d' % ((r[-1][0] + 1) if r else 1))
        return 0
    if a[0] == 'latest' and len(a) == 2 and a[1] in ('sast', 'dast'):
        r = requests(a[1])
        if not r:
            print('[!] _workspace/%s 에 요청서가 없음' % a[1], file=sys.stderr)
            return 1
        print(os.path.join('_workspace', a[1], r[-1][1]))
        return 0
    if a[0] == 'next' and len(a) == 2 and a[1] == 'run':
        n = runs()
        print((max(n) + 1) if n else 1)
        return 0
    if a[0] == 'sprint':
        s = sprints()
        if not s:
            print('[!] runs/NN_sprint 폴더가 없음', file=sys.stderr)
            return 1
        print(os.path.join('runs', s[-1][1]))
        return 0
    if a[0] == 'keyword' and len(a) == 2:
        ok = bool(KW.match(a[1])) and not a[1].endswith(('-result', '-runbook'))
        print('OK' if ok else '[!] 키워드는 영어 소문자·숫자·하이픈 2~40자 (예: mapview, graph-build). 한글·밑줄(_) 금지')
        return 0 if ok else 1
    print(__doc__)
    return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))

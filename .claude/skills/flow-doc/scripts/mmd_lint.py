#!/usr/bin/env python3
"""flow.md 안의 mermaid 블록이 우리 규칙을 지키는지 검사한다 (문법 검사는 check_mermaid.sh 가 따로 함).

사용법: python3 mmd_lint.py runs/02_sprint/flow.md [--extract-dir DIR]
  --extract-dir 를 주면 블록을 DIR/block_N.mmd 로 저장한다 (문법 검사용).

검사 규칙 (01_sprint 시행착오에서 나옴):
  R1  첫 줄은 'flowchart LR'           (TD + 상자 안 상자 = 머메이드 12에서 LR 무시됨)
  R2  모든 subgraph 바로 다음 줄은 'direction LR'   (머메이드 11은 상자 안을 기본값 TB로 그림)
  R3  호출 화살표(-->)에는 라벨이 있어야 함: "+0x.." 또는 "?" (앞에 "(n) " 가능)
  R4  한 블록 안의 함수 상자들은 모두 화살표로 이어져야 함 (끊어진 덩어리는 위아래 순서가 뒤집힘)
  R5  같은 caller가 여러 callee를 부르면 모두 "(n)" 번호가 있어야 함
  R6  'flowchart TD' / 'graph TD' 금지
종료 코드: 위반이 있으면 1
"""
import os
import re
import sys

EDGE = re.compile(r'^\s*([A-Za-z0-9_]+)\s*(-->|==>|-\.->|-\.-|~~~)\s*(?:\|([^|]*)\|\s*)?([A-Za-z0-9_]+)\s*$')
NODE = re.compile(r'^\s*([A-Za-z0-9_]+)\s*[\[\(\{]')
SUB = re.compile(r'^\s*subgraph\s+([A-Za-z0-9_]+)')
LABEL_OK = re.compile(r'^"?\s*(\(\d+\)\s*)?(\+0x[0-9a-fA-F]+|\?)\s*"?$')


def lint_block(idx, lines):
    errs = []
    body = [l for l in lines if l.strip() and not l.strip().startswith('%%')]
    if not body or body[0].strip() != 'flowchart LR':
        errs.append('R1 첫 줄이 "flowchart LR" 이 아님: %r' % (body[0].strip() if body else ''))
    for i, l in enumerate(body):
        if re.match(r'^\s*(flowchart|graph)\s+(TD|TB)', l):
            errs.append('R6 TD/TB 방향 사용: %s' % l.strip())
        if SUB.match(l):
            nxt = body[i + 1].strip() if i + 1 < len(body) else ''
            if nxt != 'direction LR':
                errs.append('R2 subgraph 다음 줄이 direction LR 아님: %s' % l.strip())

    subgraphs = {m.group(1) for l in body for m in [SUB.match(l)] if m}
    nodes = set()
    for l in body:
        if SUB.match(l) or l.strip().startswith(('classDef', 'class ', 'direction', 'flowchart', 'end')):
            continue
        m = NODE.match(l)
        if m and m.group(1) not in subgraphs:
            nodes.add(m.group(1))

    adj = {n: set() for n in nodes}
    calls_from = {}
    for l in body:
        m = EDGE.match(l)
        if not m:
            continue
        a, kind, label, b = m.group(1), m.group(2), (m.group(3) or '').strip(), m.group(4)
        if a in subgraphs or b in subgraphs:
            continue
        adj.setdefault(a, set()).add(b)
        adj.setdefault(b, set()).add(a)
        if kind == '-->':
            if not label:
                errs.append('R3 라벨 없는 호출 화살표: %s' % l.strip())
            elif not LABEL_OK.match(label):
                errs.append('R3 라벨 형식이 "+0x..", "?", "(n) ..." 이 아님: %s' % l.strip())
            calls_from.setdefault(a, []).append(label)

    for caller, labels in calls_from.items():
        if len(labels) > 1 and not all(re.match(r'^"?\s*\(\d+\)', x) for x in labels):
            errs.append('R5 %s 가 여러 함수를 부르는데 (n) 번호가 빠짐: %s' % (caller, labels))

    # R4 연결 덩어리 수
    seen, comps = set(), 0
    for n in adj:
        if n in seen:
            continue
        comps += 1
        stack = [n]
        while stack:
            x = stack.pop()
            if x in seen:
                continue
            seen.add(x)
            stack.extend(adj.get(x, ()))
    if comps > 1:
        errs.append('R4 화살표로 이어지지 않은 덩어리가 %d개 (스레드가 다르면 그림을 나눌 것)' % comps)
    return errs


def main(argv):
    if not argv:
        print(__doc__)
        return 2
    path = argv[0]
    extract = None
    if '--extract-dir' in argv:
        extract = argv[argv.index('--extract-dir') + 1]
        os.makedirs(extract, exist_ok=True)
    text = open(path, encoding='utf-8').read()
    blocks = re.findall(r'```mermaid\n(.*?)```', text, re.S)
    if not blocks:
        print('[!] mermaid 블록 없음')
        return 1
    bad = 0
    for i, b in enumerate(blocks):
        if extract:
            with open(os.path.join(extract, 'block_%d.mmd' % i), 'w', encoding='utf-8') as w:
                w.write(b)
        errs = lint_block(i, b.split('\n'))
        print('block %d: %s' % (i, 'OK' if not errs else '%d 위반' % len(errs)))
        for e in errs:
            print('   - ' + e)
        bad += len(errs)
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))

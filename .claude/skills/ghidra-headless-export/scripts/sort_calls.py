#!/usr/bin/env python3
"""_calls.tsv 를 "caller 이름 → 오프셋 숫자" 순으로 정렬하고 같은 줄을 합친다 (제자리에 쓴다).

    python3 sort_calls.py ms/disassemble/<모듈>/_calls.tsv

- 예전엔 `sort -u` (글자 순)라 `+0x1060` 이 `+0x126` 앞에 왔다 (묶음 N1, 2026-10-22).
- 오프셋 순서: 본체 `+0x..`/`-0x..` (숫자) → 조각 `frag:<이름>+0x..`·예외 정리 함수 `seh:<이름>+0x..` (이름별, 숫자).
- 열: caller  call_off  ret_off  callee  (4열이 아닌 줄은 맨 뒤에 그대로)
"""
import re
import sys

OFF = re.compile(r"^(?:(?:frag|seh):(?P<frag>[^+-]+))?(?P<sign>[+-])0x(?P<hex>[0-9a-fA-F]+)$")  # frag:·seh: (묶음 O2)


def key(line):
    cols = line.split("\t")
    if len(cols) != 4:
        return (1, line)
    m = OFF.match(cols[1])
    if not m:
        return (0, cols[0], 2, "", 0, line)
    v = int(m.group("hex"), 16) * (-1 if m.group("sign") == "-" else 1)
    return (0, cols[0], 1 if m.group("frag") else 0, m.group("frag") or "", v, line)


def main(path):
    with open(path, encoding="utf-8") as f:
        lines = {l.rstrip("\n") for l in f if l.strip()}
    with open(path, "w", encoding="utf-8") as f:
        for l in sorted(lines, key=key):
            f.write(l + "\n")


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))

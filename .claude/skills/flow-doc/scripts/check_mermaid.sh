#!/usr/bin/env bash
# flow.md 의 mermaid 블록 검사: (1) 우리 규칙 lint  (2) mermaid 문법 parse
# 사용법: check_mermaid.sh runs/02_sprint/flow.md
# - parse 에는 node + npm 이 필요. 처음 한 번 ~/.cache/pe-flow-mermaid 에 mermaid, jsdom 을 설치한다.
# - node 가 없으면 lint 만 하고 parse 는 SKIP 으로 알린다.
set -uo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MD="${1:?flow.md 경로가 필요함}"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "== lint =="
python3 "$HERE/mmd_lint.py" "$MD" --extract-dir "$TMP"
LINT=$?

echo "== parse =="
if ! command -v node >/dev/null 2>&1 || ! command -v npm >/dev/null 2>&1; then
	echo "SKIP: node/npm 없음 (문법 검사 못 함)"
	exit $LINT
fi
CACHE="$HOME/.cache/pe-flow-mermaid"
if [ ! -d "$CACHE/node_modules/mermaid" ] || [ ! -d "$CACHE/node_modules/jsdom" ]; then
	mkdir -p "$CACHE" && (cd "$CACHE" && npm init -y >/dev/null 2>&1 && npm install mermaid jsdom >/dev/null 2>&1) \
		|| { echo "SKIP: mermaid/jsdom 설치 실패"; exit $LINT; }
fi
cp "$HERE/check_mermaid.mjs" "$CACHE/check_mermaid.mjs"
(cd "$CACHE" && node check_mermaid.mjs "$TMP"/block_*.mmd)
PARSE=$?
[ $LINT -eq 0 ] && [ $PARSE -eq 0 ] && echo "== 결과: 통과 ==" || echo "== 결과: 실패 (위 내용 확인) =="
[ $LINT -eq 0 ] && [ $PARSE -eq 0 ]

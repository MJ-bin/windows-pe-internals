#!/usr/bin/env bash
# 커밋 전 검사 두 가지.
#   check_commit.sh staged          스테이징된 파일 중 커밋하면 안 되는 것 찾기
#   check_commit.sh msg <파일>       커밋 메시지 형식 + 금지 줄(Co-Authored-By 등) 찾기
# 종료 코드: 문제가 있으면 1
#
# 환경변수 ALLOW_DECOMP=1 : 사용자가 ms/decomp, ms/disassemble 내용 커밋을 직접 허락했을 때만.
set -uo pipefail

MAX_BYTES=$((5 * 1024 * 1024))

staged() {
	local bad=0 f size
	while IFS= read -r f; do
		[ -n "$f" ] || continue
		case "$f" in
		ms/bin/* | ms/sym/* | ms/ghidra/*)
			echo "[X] MS 바이너리·심볼·Ghidra 프로젝트: $f"; bad=1; continue ;;
		_workspace/.commit_msg)
			echo "[X] 커밋 메시지 임시 파일: $f"; bad=1; continue ;;
		ms/decomp/.gitkeep | ms/disassemble/.gitkeep) ;;
		ms/decomp/* | ms/disassemble/*)
			if [ "${ALLOW_DECOMP:-0}" != "1" ]; then
				echo "[X] MS 코드 디컴파일·디스어셈블 결과 (사용자 허락 없음): $f"; bad=1; continue
			fi ;;
		esac
		case "${f,,}" in
		*.pdb | *.exe | *.dll | *.sys | *.dmp | *.run | *.gpr | *.zip)
			echo "[X] 바이너리·덤프·압축 파일: $f"; bad=1; continue ;;
		esac
		size="$(git cat-file -s ":$f" 2>/dev/null || echo 0)"
		if [ "$size" -gt "$MAX_BYTES" ]; then
			echo "[X] 5MB 넘는 파일: $f ($size bytes)"; bad=1
		fi
	done < <(git diff --cached --name-only --diff-filter=ACMR)
	[ $bad -eq 0 ] && echo "[OK] 스테이징된 파일 문제 없음"
	return $bad
}

msg() {
	local file="${1:?메시지 파일 경로가 필요함}" bad=0 head
	head="$(head -n 1 "$file")"
	if ! printf '%s' "$head" | grep -Eq '^(feat|fix|docs|chore|refactor|test|init)(\([a-z0-9_-]+\))?!?: .+'; then
		echo "[X] 첫 줄이 'type(scope): 요약' 형식이 아님: $head"; bad=1
	fi
	if [ "$(printf '%s' "$head" | wc -m)" -gt 72 ]; then
		echo "[X] 첫 줄이 72자를 넘음"; bad=1
	fi
	if [ "$(sed -n '2p' "$file")" != "" ] && [ "$(wc -l < "$file")" -ge 2 ]; then
		echo "[X] 둘째 줄은 비워야 함"; bad=1
	fi
	if grep -Eiq '^(co-authored-by|signed-off-by|claude-session):|generated with|noreply@anthropic\.com' "$file"; then
		echo "[X] 공동 작성자·생성 도구 표시 줄이 있음 (넣지 않기로 함)"; bad=1
	fi
	[ $bad -eq 0 ] && echo "[OK] 메시지 형식 문제 없음"
	return $bad
}

case "${1:-}" in
staged) staged ;;
msg) shift; msg "$@" ;;
*) echo "사용법: check_commit.sh staged | msg <파일>"; exit 2 ;;
esac

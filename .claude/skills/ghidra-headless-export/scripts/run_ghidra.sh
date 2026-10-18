#!/usr/bin/env bash
# Ghidra headless 실행기 (레포 루트 어디서 불러도 됨)
#
#   run_ghidra.sh analyze <바이너리이름>                    예) run_ghidra.sh analyze ntdll.dll
#       - ms/bin 아래에서 바이너리를 찾아 Ghidra 프로젝트(ms/ghidra/<모듈>)로 가져오고
#         PDB(ms/sym/<바이너리이름>.pdb)를 붙여 자동 분석한 뒤 함수 목록(_index.tsv)을 뽑는다.
#       - 모듈마다 한 번만 하면 된다. (ntoskrnl은 오래 걸린다)
#
#   run_ghidra.sh export <바이너리이름> '<정규식>' [depth]    예) run_ghidra.sh export ntdll.dll '^Ldrp(LoadDll|MapDll)' 1
#       - 이미 분석된 프로젝트에서 이름이 맞는 함수(+depth 단계 callee)를 디컴파일/디스어셈블한다.
#
# 환경변수:
#   GHIDRA_HOME  기본값: ~/tools/ghidra_11.3.2_PUBLIC_20250415/ghidra_11.3.2_PUBLIC
#   MAXMEM       기본값: 8G   (analyzeHeadless 기본 2G로는 ntoskrnl 분석이 모자란다)
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
GHIDRA_HOME="${GHIDRA_HOME:-$HOME/tools/ghidra_11.3.2_PUBLIC_20250415/ghidra_11.3.2_PUBLIC}"
MAXMEM="${MAXMEM:-8G}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cmd="${1:?사용법: run_ghidra.sh analyze|export <바이너리이름> ...}"
bin="${2:?바이너리 이름이 필요함 (예: ntdll.dll)}"
module="$(printf '%s' "${bin%.*}" | tr '[:upper:]' '[:lower:]')"

[ -x "$GHIDRA_HOME/support/launch.sh" ] || { echo "[!] GHIDRA_HOME 이 이상함: $GHIDRA_HOME" >&2; exit 2; }

find_bin() {
	if [ -f "$ROOT/ms/bin/$bin" ]; then printf '%s\n' "$ROOT/ms/bin/$bin"; return; fi
	# System32 우선, 없으면 아무 곳
	local hit
	hit="$(find "$ROOT/ms/bin" -type f -ipath "*/System32/$bin" -print -quit)"
	[ -n "$hit" ] || hit="$(find "$ROOT/ms/bin" -type f -iname "$bin" -print -quit)"
	printf '%s\n' "$hit"
}
BIN_PATH="$(find_bin)"
[ -n "$BIN_PATH" ] || { echo "[!] ms/bin 아래에서 $bin 을 못 찾음" >&2; exit 2; }

PROJ_DIR="$ROOT/ms/ghidra"
mkdir -p "$PROJ_DIR" "$ROOT/ms/decomp/$module" "$ROOT/ms/disassemble/$module"

headless() {
	# analyzeHeadless 와 같지만 MAXMEM 을 바꿀 수 있게 launch.sh 를 직접 부른다.
	"$GHIDRA_HOME/support/launch.sh" fg jdk Ghidra-Headless "$MAXMEM" \
		"-XX:ParallelGCThreads=2 -XX:CICompilerCount=2 -Djava.awt.headless=true" \
		ghidra.app.util.headless.AnalyzeHeadless "$@"
}

case "$cmd" in
analyze)
	PDB="$ROOT/ms/sym/$bin.pdb"
	[ -f "$PDB" ] || { echo "[!] PDB 없음: $PDB  (먼저 fetch_pdb.py $bin)" >&2; exit 3; }
	echo "[+] analyze $BIN_PATH  (pdb=$PDB, MAXMEM=$MAXMEM)"
	headless "$PROJ_DIR" "$module" \
		-import "$BIN_PATH" -overwrite \
		-scriptPath "$HERE" \
		-preScript SetPdb.java "$PDB" \
		-postScript ExportIndex.java "$ROOT/ms" "$module" \
		-analysisTimeoutPerFile 21600 \
		-log "$PROJ_DIR/$module.analyze.log"
	;;
export)
	regex="${3:?정규식이 필요함}"
	depth="${4:-0}"
	echo "[+] export $module  regex=$regex depth=$depth"
	headless "$PROJ_DIR" "$module" \
		-process "$(basename "$BIN_PATH")" -noanalysis -readOnly \
		-scriptPath "$HERE" \
		-postScript ExportFunctions.java "$ROOT/ms" "$module" "$regex" "$depth" \
		-log "$PROJ_DIR/$module.export.log"
	CALLS="$ROOT/ms/disassemble/$module/_calls.tsv"
	[ -f "$CALLS" ] && sort -u -o "$CALLS" "$CALLS"
	;;
*)
	echo "[!] 알 수 없는 명령: $cmd (analyze|export)" >&2; exit 1 ;;
esac
echo "[+] 끝. 로그: $PROJ_DIR/$module.$cmd.log"

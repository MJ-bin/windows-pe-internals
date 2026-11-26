#!/usr/bin/env bash
# Ghidra headless 실행기 (레포 루트 어디서 불러도 됨)
#
#   run_ghidra.sh analyze <바이너리이름>                    예) run_ghidra.sh analyze ntdll.dll
#       - ms/bin 아래에서 바이너리를 찾아 Ghidra 프로젝트(ms/ghidra/<모듈>)로 가져오고
#         PDB(ms/sym/<바이너리이름>.pdb)를 붙여 자동 분석한 뒤 함수 목록(_index.tsv)을 뽑는다.
#       - 모듈마다 한 번만 하면 된다. (ntoskrnl은 오래 걸린다)
#
#   <바이너리이름> 자리 (analyze·export·map·refs 공통): 이름이 같은 파일이 둘 이상일 때 (System32 와 SysWOW64)
#       <이름>=<경로>  예) run_ghidra.sh analyze ntdll32.dll=SysWOW64/ntdll.dll
#       - <이름> 이 모듈 이름·PDB 이름·Ghidra 프로그램 이름을 정한다 (ms/sym/ntdll32.dll.pdb, ms/ghidra/ntdll32, ...).
#       - <경로> = 실제 파일. 절대경로, ms/bin 기준 상대경로, 또는 ms/bin 아래 경로 끝부분 (SysWOW64/ntdll.dll).
#       - '=' 가 없으면 예전과 똑같다 (이름으로 System32 먼저 찾음).
#
#   run_ghidra.sh export <바이너리이름> '<정규식>' [depth]    예) run_ghidra.sh export ntdll.dll '^Ldrp(LoadDll|MapDll)' 1
#   run_ghidra.sh export <바이너리이름> 'rva:0x18b9c0,0x12f318' [depth]   (이름이 겹칠 때: 함수 시작 rva 로 고름, 16진)
#       - 이미 분석된 프로젝트에서 이름이 맞는 함수(+depth 단계 callee)를 디컴파일/디스어셈블한다.
#       - 파일 이름: 보통은 <함수>.c/.asm. 프로그램 안에 같은 이름(글자 바꾼 뒤)이 둘 이상이거나 이름에 ( 나 < 가 있으면
#         늘 <함수>__<rva>.c/.asm. 이름이 200자를 넘으면 앞 120자 + __ + 해시 8자리.
#
#   run_ghidra.sh map <바이너리이름> <뿌리 함수> [최대깊이=10] [공용도구기준=20]   예) run_ghidra.sh map ntoskrnl.exe NtCreateUserProcess
#       - 디컴파일 없이 뿌리에서 호출 관계만 따라가 "지도"(색인)를 만든다 (CallGraphMap.java, 몇 초).
#       - 출력: ms/disassemble/<모듈>/_map/<뿌리>_{edges.tsv,nodes.tsv,summary.txt}
#
#   run_ghidra.sh refs <바이너리이름> <이름|0xRVA> [바이트수]   예) run_ghidra.sh refs ntoskrnl.exe PsNtdllExports 0x98
#       - 그 주소(전역 변수·함수)를 읽고 쓰는 곳, 그 주소를 담은 포인터 값을 찾는다 (RefsTo.java, 묶음 O2).
#       - 출력: ms/disassemble/<모듈>/_refs/<이름>.tsv
#
# 환경변수:
#   GHIDRA_HOME  기본값: ~/tools/ghidra_11.3.2_PUBLIC_20250415/ghidra_11.3.2_PUBLIC
#   MAXMEM       기본값: 8G   (analyzeHeadless 기본 2G로는 ntoskrnl 분석이 모자란다)
#
# 종료 코드: 0 성공 / 2 경로·바이너리 / 3 PDB 없음 / 4 로그에 SCRIPT ERROR (안쪽 스크립트 실패)
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
GHIDRA_HOME="${GHIDRA_HOME:-$HOME/tools/ghidra_11.3.2_PUBLIC_20250415/ghidra_11.3.2_PUBLIC}"
MAXMEM="${MAXMEM:-8G}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cmd="${1:?사용법: run_ghidra.sh analyze|export|map|refs <바이너리이름>[=<경로>] ...}"
arg2="${2:?바이너리 이름이 필요함 (예: ntdll.dll, 또는 ntdll32.dll=SysWOW64/ntdll.dll)}"
# <이름>=<경로> 꼴이면 이름만 bin 으로 쓰고 (모듈·PDB·프로그램 이름), 경로는 파일 찾기에만 쓴다.
bin="${arg2%%=*}"
bin_loc=""
if [ "$arg2" != "$bin" ]; then
	bin_loc="${arg2#*=}"
	{ [ -n "$bin" ] && [ -n "$bin_loc" ]; } || { echo "[!] <이름>=<경로> 꼴이 이상함: $arg2" >&2; exit 2; }
fi
module="$(printf '%s' "${bin%.*}" | tr '[:upper:]' '[:lower:]')"

[ -x "$GHIDRA_HOME/support/launch.sh" ] || { echo "[!] GHIDRA_HOME 이 이상함: $GHIDRA_HOME" >&2; exit 2; }

find_bin() {
	if [ -n "$bin_loc" ]; then
		# <이름>=<경로>: 절대경로 → ms/bin 기준 상대경로 → ms/bin 아래 경로 끝부분 (대소문자 무시). 끝부분이 여러 파일에 맞으면 고르지 않고 멈춘다.
		if [ -f "$bin_loc" ]; then printf '%s\n' "$bin_loc"; return; fi
		if [ -f "$ROOT/ms/bin/$bin_loc" ]; then printf '%s\n' "$ROOT/ms/bin/$bin_loc"; return; fi
		local hits n
		hits="$(find "$ROOT/ms/bin" -type f -ipath "*/$bin_loc" -print 2>/dev/null || true)"
		n="$(printf '%s' "$hits" | grep -c . || true)"
		if [ "$n" -gt 1 ]; then
			echo "[!] 경로 끝부분 '$bin_loc' 이 파일 ${n}개에 맞음 — 더 길게 (예: System32/$bin_loc):" >&2
			printf '%s\n' "$hits" | head -5 >&2
			return 2
		fi
		printf '%s\n' "$hits"
		return
	fi
	if [ -f "$ROOT/ms/bin/$bin" ]; then printf '%s\n' "$ROOT/ms/bin/$bin"; return; fi
	# System32 우선, 없으면 아무 곳
	local hit
	hit="$(find "$ROOT/ms/bin" -type f -ipath "*/System32/$bin" -print -quit)"
	[ -n "$hit" ] || hit="$(find "$ROOT/ms/bin" -type f -iname "$bin" -print -quit)"
	printf '%s\n' "$hit"
}
BIN_PATH="$(find_bin)"
[ -n "$BIN_PATH" ] || { echo "[!] ms/bin 아래에서 ${bin_loc:-$bin} 을 못 찾음 (System32 와 SysWOW64 처럼 이름이 겹치면 <이름>=<경로> 꼴: ntdll32.dll=SysWOW64/ntdll.dll)" >&2; exit 2; }
# Ghidra 프로젝트 안 프로그램 이름: <이름>=<경로> 꼴이면 <이름> (analyze 가 그 이름으로 가져옴), 아니면 파일 이름 (예전과 같음)
PROC="$(basename "$BIN_PATH")"
if [ -n "$bin_loc" ]; then PROC="$bin"; fi

PROJ_DIR="$ROOT/ms/ghidra"
mkdir -p "$PROJ_DIR" "$ROOT/ms/decomp/$module" "$ROOT/ms/disassemble/$module"

headless() {
	# analyzeHeadless 와 같지만 MAXMEM 을 바꿀 수 있게 launch.sh 를 직접 부른다.
	"$GHIDRA_HOME/support/launch.sh" fg jdk Ghidra-Headless "$MAXMEM" \
		"-XX:ParallelGCThreads=2 -XX:CICompilerCount=2 -Djava.awt.headless=true" \
		ghidra.app.util.headless.AnalyzeHeadless "$@"
}

# 이번 실행에서 새로 생긴 로그 줄만 검사하려고, 시작 전 줄 수를 기억한다.
LOG="$PROJ_DIR/$module.$cmd.log"
LOG_START=0
[ -f "$LOG" ] && LOG_START="$(wc -l < "$LOG")"

case "$cmd" in
analyze)
	PDB="$ROOT/ms/sym/$bin.pdb"
	[ -f "$PDB" ] || { echo "[!] PDB 없음: $PDB  (먼저 fetch_pdb.py $bin)" >&2; exit 3; }
	IMPORT_PATH="$BIN_PATH"
	if [ -n "$bin_loc" ]; then
		# Ghidra 프로그램 이름 = 가져오는 파일 이름. <이름> 으로 맞추려고 이름 붙인 링크(하드링크, 안 되면 복사)를 ms/ghidra/_stage/ 에 둔다.
		mkdir -p "$PROJ_DIR/_stage"
		ln -f "$BIN_PATH" "$PROJ_DIR/_stage/$bin" 2>/dev/null || cp -f "$BIN_PATH" "$PROJ_DIR/_stage/$bin"
		IMPORT_PATH="$PROJ_DIR/_stage/$bin"
	fi
	echo "[+] analyze $BIN_PATH  (pdb=$PDB, MAXMEM=$MAXMEM)"
	headless "$PROJ_DIR" "$module" \
		-import "$IMPORT_PATH" -overwrite \
		-scriptPath "$HERE" \
		-preScript SetPdb.java "$PDB" \
		-postScript ExportIndex.java "$ROOT/ms" "$module" \
		-analysisTimeoutPerFile 21600 \
		-log "$LOG"
	;;
export)
	regex="${3:?정규식이 필요함}"
	depth="${4:-0}"
	echo "[+] export $module  regex=$regex depth=$depth"
	headless "$PROJ_DIR" "$module" \
		-process "$PROC" -noanalysis -readOnly \
		-scriptPath "$HERE" \
		-postScript ExportFunctions.java "$ROOT/ms" "$module" "$regex" "$depth" \
		-log "$LOG"
	CALLS="$ROOT/ms/disassemble/$module/_calls.tsv"
	[ -f "$CALLS" ] && python3 "$HERE/sort_calls.py" "$CALLS"   # caller → 오프셋 숫자 순 (묶음 N1)
	;;
map)
	root="${3:?뿌리 함수 이름이 필요함 (예: NtCreateUserProcess)}"
	depth="${4:-10}"
	th="${5:-20}"
	OUT="$ROOT/ms/disassemble/$module/_map"
	mkdir -p "$OUT"
	echo "[+] map $module  root=$root depth=$depth helper>=$th"
	headless "$PROJ_DIR" "$module" \
		-process "$PROC" -noanalysis -readOnly \
		-scriptPath "$HERE" \
		-postScript CallGraphMap.java "$OUT" "$root" "$depth" "$th" \
		-log "$LOG"
	echo "[+] 지도: $OUT/${root}_edges.tsv, ${root}_nodes.tsv, ${root}_summary.txt"
	;;
refs)
	key="${3:?이름이나 0xRVA 가 필요함 (예: PsNtdllExports)}"
	OUT="$ROOT/ms/disassemble/$module/_refs"
	mkdir -p "$OUT"
	echo "[+] refs $module  $key ${4:-}"
	headless "$PROJ_DIR" "$module" \
		-process "$PROC" -noanalysis -readOnly \
		-scriptPath "$HERE" \
		-postScript RefsTo.java "$OUT" "$key" ${4:+"$4"} \
		-log "$LOG"
	;;
*)
	echo "[!] 알 수 없는 명령: $cmd (analyze|export|map|refs)" >&2; exit 1 ;;
esac
# Ghidra 는 안쪽 스크립트(SetPdb 등)가 죽어도 정상 종료한다. 로그에서 직접 찾아 실패로 알린다.
# (로그를 덮어쓰면 LOG_START 보다 짧아진다 → 그땐 처음부터 본다)
if [ -f "$LOG" ]; then
	[ "$(wc -l < "$LOG")" -lt "$LOG_START" ] && LOG_START=0
	errs="$(tail -n +"$((LOG_START + 1))" "$LOG" | grep -F "SCRIPT ERROR" || true)"
	if [ -n "$errs" ]; then
		echo "[!] 로그에 SCRIPT ERROR 가 있음 — Ghidra 는 끝났지만 결과를 믿으면 안 됨:" >&2
		printf '%s\n' "$errs" | head -5 >&2
		echo "[!] 로그: $LOG" >&2
		exit 4
	fi
fi
echo "[+] 끝. 로그: $LOG"

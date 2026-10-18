// 모듈의 모든 함수 목록을 TSV로 뽑는다 (가볍다. 분석 직후 1번 실행).
// 인자: <ms 디렉토리 절대경로> <모듈 이름(소문자, 예: ntdll)>
// 출력: <ms>/decomp/<모듈>/_index.tsv
//   열: name  entry  rva  size  thunk
//@category PE-Internals
import java.io.File;
import java.io.PrintWriter;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;

public class ExportIndex extends GhidraScript {
	@Override
	protected void run() throws Exception {
		String[] args = getScriptArgs();
		if (args.length < 2) {
			printerr("ExportIndex: <msDir> <module> 인자가 필요함");
			return;
		}
		File dir = new File(new File(args[0], "decomp"), args[1]);
		dir.mkdirs();
		long base = currentProgram.getImageBase().getOffset();
		int n = 0;
		try (PrintWriter w = new PrintWriter(new File(dir, "_index.tsv"), "UTF-8")) {
			w.println("# name\tentry\trva\tsize\tthunk");
			FunctionIterator it = currentProgram.getFunctionManager().getFunctions(true);
			for (Function f : it) {
				if (monitor.isCancelled()) {
					break;
				}
				long entry = f.getEntryPoint().getOffset();
				w.printf("%s\t%s\t0x%x\t%d\t%s%n", f.getName(), f.getEntryPoint(), entry - base,
					f.getBody().getNumAddresses(), f.isThunk() ? "thunk" : "");
				n++;
			}
		}
		println("ExportIndex: " + n + " functions -> " + dir);
	}
}

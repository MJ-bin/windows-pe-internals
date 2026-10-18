// headless 분석 전에(pre-script) 이 프로그램에 쓸 PDB 파일을 지정한다.
// 인자: <pdb 파일 경로>
// Ghidra 11.3.2 의 PdbUniversalAnalyzer.setPdbFileOption(Program, File) 사용.
//@category PE-Internals
import java.io.File;

import ghidra.app.plugin.core.analysis.PdbUniversalAnalyzer;
import ghidra.app.script.GhidraScript;

public class SetPdb extends GhidraScript {
	@Override
	protected void run() throws Exception {
		String[] args = getScriptArgs();
		if (args.length < 1) {
			printerr("SetPdb: PDB 경로 인자가 필요함");
			return;
		}
		File pdb = new File(args[0]);
		if (!pdb.isFile()) {
			printerr("SetPdb: PDB 파일 없음: " + pdb);
			return;
		}
		PdbUniversalAnalyzer.setPdbFileOption(currentProgram, pdb);
		println("SetPdb: " + currentProgram.getName() + " <- " + pdb);
	}
}

// 이름이 정규식에 맞는 함수들(+ 그 함수가 부르는 함수를 depth 단계까지)을
// 디컴파일 C, 디스어셈블, 호출 지점 표로 뽑는다.
//
// 인자: <ms 디렉토리 절대경로> <모듈 이름> <함수이름 정규식> [depth=0]
// 출력:
//   <ms>/decomp/<모듈>/<함수>.c
//   <ms>/disassemble/<모듈>/<함수>.asm
//   <ms>/disassemble/<모듈>/_calls.tsv   (누적. 래퍼 스크립트가 sort -u 로 중복 제거)
//      열: caller  call_off  ret_off  callee
//      ret_off = call 다음 명령의 오프셋 = WinDbg 콜스택의 "caller+0x..." 와 같은 값
//      ret_off = tail  : JMP 꼬리호출. Ghidra 분석기 "Shared Return Calls" 가 함수로 가는 JMP 를
//                        호출로 바꿔 둔 것. 돌아올 자리가 없어 콜스택에 caller 가 안 남는다.
//      INT3 / INT n    : x86 사양(ia.sinc)이 swi(n) 로의 간접 call 로 적어서 Ghidra 는 호출로 보지만,
//                        함수 호출이 아니라 함정/예외라 표에 넣지 않는다 (.asm 에 "; TRAP" 만).
//      callee = indirect:cfg:<출처> : CFG 검사 함수(_guard_dispatch_icall 등)를 거친 간접 호출.
//                        진짜 대상은 RAX 안이라 CALL 앞에서 RAX 를 채운 메모리 자리의 이름(없으면 @rva)을 적는다.
//@category PE-Internals
import java.io.File;
import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.HashMap;
import java.util.LinkedHashSet;
import java.util.Map;
import java.util.Set;
import java.util.regex.Pattern;

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.lang.Register;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.SourceType;
import ghidra.program.model.symbol.Symbol;

public class ExportFunctions extends GhidraScript {

	private static String off(long v) {
		return (v < 0 ? "-0x" : "+0x") + Long.toHexString(Math.abs(v));
	}

	private String calleeName(Instruction ins, FunctionManager fm) {
		Address[] flows = ins.getFlows();
		if (flows != null && flows.length > 0) {
			Function cf = fm.getFunctionAt(flows[0]);
			if (cf != null) {
				return cf.getName();
			}
			Symbol s = currentProgram.getSymbolTable().getPrimarySymbol(flows[0]);
			return s != null ? s.getName() : flows[0].toString();
		}
		// 간접 호출: call [rip+X] 같은 것. 가리키는 포인터 자리의 이름을 적는다.
		for (Reference ref : ins.getReferencesFrom()) {
			Address to = ref.getToAddress();
			Function cf = fm.getFunctionAt(to);
			if (cf != null) {
				return cf.getName();
			}
			Symbol s = currentProgram.getSymbolTable().getPrimarySymbol(to);
			if (s != null) {
				return "indirect:" + s.getName();
			}
		}
		return "indirect:?";
	}

	private static boolean isCfgDispatch(String n) {
		return n != null && (n.contains("guard_dispatch_icall") || n.contains("guard_xfg_dispatch_icall"));
	}

	// CFG 간접 호출: CALL 앞을 최대 10개 거슬러 올라가 RAX 를 채운 명령을 찾고,
	// 그 명령이 읽은 메모리 자리(함수 포인터 변수)의 이름을 돌려준다.
	private String cfgTarget(Instruction call, Function owner) {
		long base = currentProgram.getImageBase().getOffset();
		Instruction p = call;
		for (int i = 0; i < 10; i++) {
			p = currentProgram.getListing().getInstructionBefore(p.getAddress());
			if (p == null || !owner.getBody().contains(p.getAddress()) || p.getFlowType().isCall()) {
				break;
			}
			boolean writesRax = false;
			for (Object r : p.getResultObjects()) {
				if (r instanceof Register && "RAX".equals(((Register) r).getBaseRegister().getName())) {
					writesRax = true;
					break;
				}
			}
			if (!writesRax) {
				continue;
			}
			for (Reference ref : p.getReferencesFrom()) {
				if (ref.getReferenceType().isData()) {
					Address to = ref.getToAddress();
					Symbol s = currentProgram.getSymbolTable().getPrimarySymbol(to);
					if (s != null && s.getSource() != SourceType.DEFAULT) {
						return "indirect:cfg:" + s.getName();
					}
					return "indirect:cfg:@0x" + Long.toHexString(to.getOffset() - base);
				}
			}
			return "indirect:cfg:?";
		}
		return "indirect:cfg:?";
	}

	@Override
	protected void run() throws Exception {
		String[] args = getScriptArgs();
		if (args.length < 3) {
			printerr("ExportFunctions: <msDir> <module> <regex> [depth] 인자가 필요함");
			return;
		}
		File msDir = new File(args[0]);
		String module = args[1];
		Pattern pat = Pattern.compile(args[2]);
		int depth = args.length > 3 ? Integer.parseInt(args[3]) : 0;

		File decDir = new File(new File(msDir, "decomp"), module);
		File disDir = new File(new File(msDir, "disassemble"), module);
		decDir.mkdirs();
		disDir.mkdirs();

		FunctionManager fm = currentProgram.getFunctionManager();
		long base = currentProgram.getImageBase().getOffset();

		// 1) 이름으로 고르기
		LinkedHashSet<Function> targets = new LinkedHashSet<>();
		FunctionIterator all = fm.getFunctions(true);
		for (Function f : all) {
			if (pat.matcher(f.getName()).find()) {
				targets.add(f);
			}
		}
		println("ExportFunctions: 이름으로 고른 함수 " + targets.size() + "개");

		// 2) 부르는 함수 depth 단계까지 넓히기
		Set<Function> frontier = new LinkedHashSet<>(targets);
		for (int d = 0; d < depth; d++) {
			Set<Function> next = new LinkedHashSet<>();
			for (Function f : frontier) {
				for (Function c : f.getCalledFunctions(monitor)) {
					if (!c.isExternal() && targets.add(c)) {
						next.add(c);
					}
				}
			}
			frontier = next;
		}
		println("ExportFunctions: depth " + depth + " 적용 후 " + targets.size() + "개");

		DecompInterface di = new DecompInterface();
		di.openProgram(currentProgram);
		Map<String, Integer> used = new HashMap<>();

		try (PrintWriter calls = new PrintWriter(new FileWriter(new File(disDir, "_calls.tsv"), true))) {
			for (Function f : targets) {
				if (monitor.isCancelled()) {
					break;
				}
				if (f.isExternal()) {
					continue;
				}
				String name = f.getName();
				String safe = name.replaceAll("[^A-Za-z0-9_.@$-]", "_");
				int k = used.merge(safe, 1, Integer::sum);
				if (k > 1) {
					safe = safe + "__" + Long.toHexString(f.getEntryPoint().getOffset() - base);
				}
				long entry = f.getEntryPoint().getOffset();
				String head = String.format("%s!%s  entry=%s  rva=0x%x", module, name, f.getEntryPoint(),
					entry - base);

				StringBuilder callers = new StringBuilder();
				for (Function c : f.getCallingFunctions(monitor)) {
					callers.append(c.getName()).append(' ');
				}
				StringBuilder callees = new StringBuilder();
				for (Function c : f.getCalledFunctions(monitor)) {
					callees.append(c.getName()).append(' ');
				}

				// 디컴파일
				DecompileResults r = di.decompileFunction(f, 180, monitor);
				try (PrintWriter w = new PrintWriter(new File(decDir, safe + ".c"), "UTF-8")) {
					w.println("// " + head);
					w.println("// callers: " + callers.toString().trim());
					w.println("// callees: " + callees.toString().trim());
					if (r != null && r.decompileCompleted() && r.getDecompiledFunction() != null) {
						w.print(r.getDecompiledFunction().getC());
					}
					else {
						w.println("// decompile failed: " + (r == null ? "null" : r.getErrorMessage()));
					}
				}

				// 디스어셈블 + 호출 지점
				try (PrintWriter w = new PrintWriter(new File(disDir, safe + ".asm"), "UTF-8")) {
					w.println("; " + head);
					InstructionIterator it = currentProgram.getListing().getInstructions(f.getBody(), true);
					while (it.hasNext()) {
						Instruction ins = it.next();
						long o = ins.getAddress().getOffset() - entry;
						StringBuilder line = new StringBuilder(
							String.format("%-9s %s  %s", off(o), ins.getAddress(), ins.toString()));
						if (ins.getFlowType().isCall()) {
							String mn = ins.getMnemonicString().toUpperCase();
							if (mn.startsWith("INT")) {
								line.append("    ; TRAP");
							}
							else if (mn.startsWith("J")) {
								String tgt = calleeName(ins, fm);
								line.append("    ; TAILJMP -> ").append(tgt);
								calls.printf("%s\t%s\t%s\t%s%n", name, off(o), "tail", tgt);
							}
							else {
								long ret = ins.getAddress().getOffset() + ins.getLength() - entry;
								String tgt = calleeName(ins, fm);
								if (isCfgDispatch(tgt)) {
									tgt = cfgTarget(ins, f);
								}
								line.append("    ; CALL ret=").append(off(ret)).append(" -> ").append(tgt);
								calls.printf("%s\t%s\t%s\t%s%n", name, off(o), off(ret), tgt);
							}
						}
						w.println(line);
					}
				}
			}
		}
		di.dispose();
		println("ExportFunctions: 완료 -> " + decDir + " , " + disDir);
	}
}

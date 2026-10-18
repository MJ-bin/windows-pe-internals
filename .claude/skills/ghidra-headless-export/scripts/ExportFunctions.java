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
//      callee = alias:<이름>+N : 대상 주소에 PDB 이름이 N개 더 있다 (링커가 같은 코드를 합친 ICF 등).
//                        <이름>은 Ghidra 가 고른 하나일 뿐이라 믿지 않는다. .asm 에 "; ALIASES" 로 5개까지.
//                        Ordinal_<숫자>, $thunk$, Nt/Zw 짝은 세지 않는다 (harmlessAlias).
//      call_off/ret_off = frag:<FUN_이름>+0x.. : 함수 밖으로 떨어져 나간 조각(컴파일러가 잘 안 쓰는 길을 멀리 둔 것)
//                        안의 호출. 함수가 JMP/Jcc 로 넘어가는 이름 없는(FUN_) 함수를 조각으로 보고 따라간다.
//                        오프셋은 조각 시작 기준 (WinDbg 가 이 주소를 어떻게 보여 줄지는 확인 전이라 caller+0x.. 로 적지 않는다).
//                        .asm 맨 아래에 "; --- 조각 ..." 머리와 함께 붙는다.
//@category PE-Internals
import java.io.File;
import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashSet;
import java.util.List;
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

	private String aliasNote() {
		if (lastAliases.isEmpty()) {
			return "";
		}
		List<String> head = lastAliases.subList(0, Math.min(5, lastAliases.size()));
		return "    ; ALIASES(" + lastAliases.size() + "): " + String.join(", ", head)
			+ (lastAliases.size() > 5 ? ", ..." : "");
	}

	private static String off(long v) {
		return (v < 0 ? "-0x" : "+0x") + Long.toHexString(Math.abs(v));
	}

	// 마지막 calleeName() 이 찾은 별칭 목록 (.asm 주석용)
	private List<String> lastAliases = new ArrayList<>();

	// 별칭으로 세지 않는 이름: 이름이 틀릴 걱정이 없는 것들.
	//   Ordinal_<숫자> : 내보내기 번호 라벨 (내보내는 함수마다 붙는다)
	//   ...$thunk$...  : 같은 함수로 가는 썽크 표시
	//   Nt<X> <-> Zw<X> : 같은 시스템 콜 함수의 두 이름
	private static boolean harmlessAlias(String primary, String other) {
		if (other.matches("Ordinal_[0-9]+") || other.contains("$thunk$")) {
			return true;
		}
		if (primary.length() > 2 && other.length() > 2 && primary.substring(2).equals(other.substring(2))) {
			String a = primary.substring(0, 2), b = other.substring(0, 2);
			return (a.equals("Nt") && b.equals("Zw")) || (a.equals("Zw") && b.equals("Nt"));
		}
		return false;
	}

	// 같은 주소에 사용자/PDB 가 붙인 다른 이름이 있으면 alias:<이름>+N 으로 표시한다.
	private String withAliases(Address a, String primary) {
		List<String> others = new ArrayList<>();
		for (Symbol s : currentProgram.getSymbolTable().getSymbols(a)) {
			String nm = s.getName();
			if (s.getSource() == SourceType.DEFAULT || nm.equals(primary) || others.contains(nm)
				|| harmlessAlias(primary, nm)) {
				continue;
			}
			others.add(nm);
		}
		lastAliases = others;
		return others.isEmpty() ? primary : "alias:" + primary + "+" + others.size();
	}

	private String calleeName(Instruction ins, FunctionManager fm) {
		lastAliases = new ArrayList<>();
		Address[] flows = ins.getFlows();
		if (flows != null && flows.length > 0) {
			Function cf = fm.getFunctionAt(flows[0]);
			if (cf != null) {
				return withAliases(flows[0], cf.getName());
			}
			Symbol s = currentProgram.getSymbolTable().getPrimarySymbol(flows[0]);
			return s != null ? s.getName() : flows[0].toString();
		}
		// 간접 호출: call [rip+X] 같은 것. 가리키는 포인터 자리의 이름을 적는다.
		for (Reference ref : ins.getReferencesFrom()) {
			Address to = ref.getToAddress();
			Function cf = fm.getFunctionAt(to);
			if (cf != null) {
				return withAliases(to, cf.getName());
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

	// body 의 명령을 차례로 .asm 에 쓰고, 호출은 _calls.tsv 에 caller=ownerName 으로 적는다.
	// prefix 가 "" 이면 본체 (+0x..), 아니면 조각 ("frag:FUN_..+0x..").
	// owner 밖·body 밖으로 가는 점프의 목적지가 이름 없는(FUN_) 함수면 frags 에 모은다.
	private void walkBody(String ownerName, Function owner, Function body, String prefix, PrintWriter w,
			PrintWriter calls, FunctionManager fm, Set<Function> frags) {
		long start = body.getEntryPoint().getOffset();
		InstructionIterator it = currentProgram.getListing().getInstructions(body.getBody(), true);
		while (it.hasNext()) {
			Instruction ins = it.next();
			long o = ins.getAddress().getOffset() - start;
			String at = prefix + off(o);
			StringBuilder line = new StringBuilder(
				String.format("%-9s %s  %s", at, ins.getAddress(), ins.toString()));
			String mn = ins.getMnemonicString().toUpperCase();
			if (mn.startsWith("J")) {
				for (Address fl : ins.getFlows()) {
					if (owner.getBody().contains(fl) || body.getBody().contains(fl)) {
						continue;
					}
					Function g = fm.getFunctionContaining(fl);
					if (g != null && g != owner && !g.isExternal() && g.getName().startsWith("FUN_")) {
						frags.add(g);
					}
				}
			}
			if (ins.getFlowType().isCall()) {
				if (mn.startsWith("INT")) {
					line.append("    ; TRAP");
				}
				else if (mn.startsWith("J")) {
					String tgt = calleeName(ins, fm);
					line.append("    ; TAILJMP -> ").append(tgt).append(aliasNote());
					calls.printf("%s\t%s\t%s\t%s%n", ownerName, at, "tail", tgt);
				}
				else {
					String ret = prefix + off(ins.getAddress().getOffset() + ins.getLength() - start);
					String tgt = calleeName(ins, fm);
					if (isCfgDispatch(tgt)) {
						tgt = cfgTarget(ins, body);
						lastAliases = new ArrayList<>();
					}
					line.append("    ; CALL ret=").append(ret).append(" -> ").append(tgt).append(aliasNote());
					calls.printf("%s\t%s\t%s\t%s%n", ownerName, at, ret, tgt);
				}
			}
			w.println(line);
		}
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
					Set<Function> frags = new LinkedHashSet<>();
					walkBody(name, f, f, "", w, calls, fm, frags);
					// 떨어진 조각들 (조각이 또 다른 조각으로 가면 그것도, 같은 조각은 한 번만, 최대 20개)
					List<Function> todo = new ArrayList<>(frags);
					Set<Function> seen = new HashSet<>(frags);
					for (int i = 0; i < todo.size() && i < 20; i++) {
						Function g = todo.get(i);
						w.println();
						w.println(String.format("; --- 조각 %s  entry=%s  rva=0x%x  (%s 에서 점프로 옴. 오프셋은 조각 시작 기준) ---",
							g.getName(), g.getEntryPoint(), g.getEntryPoint().getOffset() - base, name));
						Set<Function> more = new LinkedHashSet<>();
						walkBody(name, f, g, "frag:" + g.getName(), w, calls, fm, more);
						for (Function m : more) {
							if (seen.add(m)) {
								todo.add(m);
							}
						}
					}
				}
			}
		}
		di.dispose();
		println("ExportFunctions: 완료 -> " + decDir + " , " + disDir);
	}
}

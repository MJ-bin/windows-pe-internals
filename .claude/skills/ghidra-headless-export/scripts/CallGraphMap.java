// 뿌리 함수에서 정적 호출 관계를 따라가며 "지도"(색인)를 만든다. sast/05 실험(2026-10-21)에서 하네스로 옮김.
// 디컴파일은 하지 않는다. 명령을 읽어 호출만 모은다. 읽을 목록이 아니라 "이 함수는 어디쯤, 누가 부르나"를 찾는 색인이다.
// 실행: run_ghidra.sh map <바이너리> <뿌리 함수> [최대 깊이=10] [공용 도구 기준=20]
//
// 인자: <출력 폴더> <뿌리 함수 이름> <최대 깊이> [공용 도구 기준 caller 수=20]
// 출력 (<출력 폴더>/<뿌리>_ 로 시작):
//   _edges.tsv   : 펼친 지도(공용 도구에서 멈춤)의 호출 줄
//                  depth  caller  call_off  ret_off  kind  callee  note
//   _nodes.tsv   : 펼친 지도의 함수(또는 끊긴 끝)마다 한 줄
//                  name  rva  size  depth  callers_total  status  reason
//   _summary.txt : 깊이별 개수 — (A) 거르지 않은 전체, (B) 공용 도구에서 멈춘 지도 (기준 10/20/50)
//
// 끊김(더 안 따라가는 곳)과 이유:
//   indirect      간접 호출: 대상이 실행 때 정해짐 (레지스터/메모리 안의 주소)
//   indirect-cfg  CFG 검사를 거친 간접 호출 (_guard_dispatch_icall*): 진짜 대상은 RAX. 출처 변수 이름(있으면)
//   cfg-check     CFG 검사만 하는 호출 (_guard_check_icall*): 실제 호출은 그 뒤의 간접 호출
//   external      다른 모듈로 나감 (import)
//   syscall       함수 안에 SYSCALL 명령: 커널로 넘어감 (ntdll Nt*/Zw* 등)
//   helper        공용 도구: 프로그램 전체에서 caller 가 기준 이상 → 여기서 안 펼침 (지도 크기를 줄이려는 선택, 진짜 끊김 아님)
//   depth         최대 깊이에 닿음
//   (seen)        이미 지도에 있는 함수 — 줄만 남기고 다시 펼치지 않음
// 그대로 따라가는 것: 직접 CALL, 꼬리호출(JMP → 이름 있는 함수, kind=tail), 떨어진 조각(JMP/Jcc → FUN_, 본래 함수에 합침),
//   주인 없는 덩어리(JMP/Jcc → 어느 함수에도 안 속한 코드, 본래 함수에 합침, 오프셋은 본래 함수 기준 — sast/06)
// 표시만: alias (대상 주소에 다른 PDB 이름이 N개 — 이름을 믿지 말 것)
//@category PE-Internals
import java.io.File;
import java.io.PrintWriter;
import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Deque;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.address.AddressSet;
import ghidra.program.model.address.AddressSetView;
import ghidra.program.model.lang.Register;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.SourceType;
import ghidra.program.model.symbol.Symbol;

public class CallGraphMap extends GhidraScript {

	static class Edge {
		String callOff, retOff, kind, label, note = "";
		Function target; // null 이면 끊긴 끝
	}

	private FunctionManager fm;
	private long base;
	private final Map<Function, List<Edge>> cache = new HashMap<>();
	private final Map<Function, Boolean> syscallCache = new HashMap<>();
	private final Map<Function, Integer> callerCache = new HashMap<>();

	private static String off(long v) {
		return (v < 0 ? "-0x" : "+0x") + Long.toHexString(Math.abs(v));
	}

	private int aliasCount(Address a, String primary) {
		Set<String> others = new HashSet<>();
		for (Symbol s : currentProgram.getSymbolTable().getSymbols(a)) {
			String nm = s.getName();
			if (s.getSource() == SourceType.DEFAULT || nm.equals(primary) || nm.matches("Ordinal_[0-9]+")
				|| nm.contains("$thunk$")) {
				continue;
			}
			if (primary.length() > 2 && nm.length() > 2 && primary.substring(2).equals(nm.substring(2))
				&& (primary.startsWith("Nt") || primary.startsWith("Zw"))
				&& (nm.startsWith("Nt") || nm.startsWith("Zw"))) {
				continue;
			}
			others.add(nm);
		}
		return others.size();
	}

	private String cfgSource(Instruction call, AddressSetView region) {
		Instruction p = call;
		for (int i = 0; i < 10; i++) {
			p = currentProgram.getListing().getInstructionBefore(p.getAddress());
			if (p == null || !region.contains(p.getAddress()) || p.getFlowType().isCall()) {
				break;
			}
			boolean rax = false;
			for (Object r : p.getResultObjects()) {
				if (r instanceof Register && "RAX".equals(((Register) r).getBaseRegister().getName())) {
					rax = true;
				}
			}
			if (!rax) {
				continue;
			}
			for (Reference ref : p.getReferencesFrom()) {
				if (ref.getReferenceType().isData()) {
					Address to = ref.getToAddress();
					Symbol s = currentProgram.getSymbolTable().getPrimarySymbol(to);
					return (s != null && s.getSource() != SourceType.DEFAULT) ? s.getName()
							: "@0x" + Long.toHexString(to.getOffset() - base);
				}
			}
			return "?";
		}
		return "?";
	}

	// CFG 문: 이름으로 가린다. getFlows() 가 차 있어도(직접 CALL 모양) 먼저 본다 (sast/05 스크립트 문제 1)
	private boolean cfgGate(Instruction ins, AddressSetView body, String name, Edge e) {
		if (name.contains("guard_dispatch_icall") || name.contains("guard_xfg_dispatch_icall")) {
			e.kind = "indirect-cfg";
			e.label = "cfg:" + cfgSource(ins, body);
			e.note = "CFG 검사 뒤 RAX 의 주소로 감 (대상은 실행 때)";
			return true;
		}
		if (name.contains("guard_check_icall")) {
			e.kind = "cfg-check";
			e.label = name;
			e.note = "CFG 검사만 (실제 호출은 뒤의 간접 호출)";
			return true;
		}
		return false;
	}

	// 호출 대상: Function 이 있으면 그것, 없으면 끊긴 끝의 라벨을 e 에 채운다
	private void resolve(Instruction ins, AddressSetView body, Edge e) {
		Address[] flows = ins.getFlows();
		if (flows != null && flows.length > 0) {
			Function g = fm.getFunctionAt(flows[0]);
			if (g == null) {
				g = fm.getFunctionContaining(flows[0]);
			}
			if (g != null && cfgGate(ins, body, g.getName(), e)) {
				return;
			}
			if (g != null) {
				if (g.isExternal() || (g.isThunk() && g.getThunkedFunction(true) != null
					&& g.getThunkedFunction(true).isExternal())) {
					e.kind = "external";
					e.label = g.getName();
					e.note = "다른 모듈로 나감 (import)";
					return;
				}
				e.target = g;
				e.label = g.getName();
				return;
			}
			e.kind = "indirect";
			e.label = flows[0].toString();
			e.note = "목적지에 함수 없음";
			return;
		}
		for (Reference ref : ins.getReferencesFrom()) {
			Address to = ref.getToAddress();
			Symbol s = currentProgram.getSymbolTable().getPrimarySymbol(to);
			if (to.isExternalAddress() || (s != null && (s.isExternal() || s.getName().startsWith("__imp_")))) {
				e.kind = "external";
				e.label = s != null ? s.getName() : to.toString();
				e.note = "다른 모듈로 나감 (import)";
				return;
			}
			if (s != null && cfgGate(ins, body, s.getName(), e)) {
				return;
			}
			Function g = fm.getFunctionAt(to);
			if (g != null) {
				if (cfgGate(ins, body, g.getName(), e)) {
					return;
				}
				e.target = g;
				e.label = g.getName();
				return;
			}
			if (s != null) {
				e.kind = "indirect";
				e.label = "ptr:" + s.getName();
				e.note = "함수 포인터 변수를 거침 (대상은 실행 때)";
				return;
			}
		}
		e.kind = "indirect";
		e.label = "?";
		e.note = "레지스터 안의 주소로 감 (대상은 실행 때)";
	}

	private boolean hasSyscall(Function f) {
		return syscallCache.computeIfAbsent(f, k -> {
			InstructionIterator it = currentProgram.getListing().getInstructions(k.getBody(), true);
			while (it.hasNext()) {
				if (it.next().getMnemonicString().equalsIgnoreCase("SYSCALL")) {
					return true;
				}
			}
			return false;
		});
	}

	private int callers(Function f) {
		return callerCache.computeIfAbsent(f, k -> k.getCallingFunctions(monitor).size());
	}

	// 어느 함수에도 안 속한 코드 덩어리를 start 에서부터 흐름(다음 명령, 점프)을 따라 모은다
	private AddressSet collectOrphan(Address start, AddressSetView done) {
		AddressSet set = new AddressSet();
		Deque<Address> q = new ArrayDeque<>();
		q.add(start);
		while (!q.isEmpty() && set.getNumAddresses() < 20000) {
			Address a = q.poll();
			if (a == null || set.contains(a) || done.contains(a) || fm.getFunctionContaining(a) != null) {
				continue;
			}
			Instruction ins = currentProgram.getListing().getInstructionAt(a);
			if (ins == null) {
				continue;
			}
			set.add(ins.getMinAddress(), ins.getMaxAddress());
			if (ins.getFallThrough() != null) {
				q.add(ins.getFallThrough());
			}
			if (ins.getMnemonicString().toUpperCase().startsWith("J")) {
				for (Address fl : ins.getFlows()) {
					q.add(fl);
				}
			}
		}
		return set;
	}

	// f 본체 + 떨어진 조각 + 주인 없는 덩어리에서 호출 줄을 모은다 (한 번만 계산)
	private List<Edge> edges(Function f) {
		List<Edge> got = cache.get(f);
		if (got != null) {
			return got;
		}
		List<Edge> out = new ArrayList<>();
		// 읽을 곳: {영역, 오프셋 기준 주소, 접두어}
		List<Object[]> regions = new ArrayList<>();
		regions.add(new Object[] { f.getBody(), f.getEntryPoint().getOffset(), "" });
		Set<Function> seenFrag = new HashSet<>();
		AddressSet orphanDone = new AddressSet();
		for (int ri = 0; ri < regions.size() && ri < 60; ri++) {
			AddressSetView region = (AddressSetView) regions.get(ri)[0];
			long start = (Long) regions.get(ri)[1];
			String prefix = (String) regions.get(ri)[2];
			InstructionIterator it = currentProgram.getListing().getInstructions(region, true);
			while (it.hasNext()) {
				Instruction ins = it.next();
				String mn = ins.getMnemonicString().toUpperCase();
				long o = ins.getAddress().getOffset() - start;
				boolean inner = false; // 조각·덩어리로 가는 점프 = 함수 안 이동, 호출 아님
				if (mn.startsWith("J")) {
					for (Address fl : ins.getFlows()) {
						if (f.getBody().contains(fl) || region.contains(fl)) {
							continue;
						}
						Function g = fm.getFunctionContaining(fl);
						if (g == null) {
							if (!orphanDone.contains(fl) && currentProgram.getListing().getInstructionAt(fl) != null) {
								AddressSet chunk = collectOrphan(fl, orphanDone);
								if (!chunk.isEmpty()) {
									orphanDone.add(chunk);
									regions.add(new Object[] { chunk, f.getEntryPoint().getOffset(), "" });
								}
							}
							inner = true;
						}
						else if (g != f && !g.isExternal() && g.getName().startsWith("FUN_")) {
							inner = true;
							if (seenFrag.add(g) && seenFrag.size() <= 20) {
								regions.add(new Object[] { g.getBody(), g.getEntryPoint().getOffset(), "frag:" + g.getName() });
							}
						}
					}
				}
				if (!ins.getFlowType().isCall() || mn.startsWith("INT") || inner) {
					continue;
				}
				Edge e = new Edge();
				e.callOff = prefix + off(o);
				e.kind = mn.startsWith("J") ? "tail" : "call";
				e.retOff = mn.startsWith("J") ? "tail" : prefix + off(ins.getAddress().getOffset() + ins.getLength() - start);
				resolve(ins, region, e);
				if (e.target != null) {
					int n = aliasCount(e.target.getEntryPoint(), e.target.getName());
					if (n > 0) {
						e.note = "alias: 같은 주소 이름 " + n + "개 더 (이름을 믿지 말 것)";
					}
				}
				out.add(e);
			}
		}
		cache.put(f, out);
		return out;
	}

	// 깊이별 새 함수 수. threshold<0 이면 거르지 않음
	private List<Integer> layers(Function root, int maxDepth, int threshold) {
		List<Integer> counts = new ArrayList<>();
		Set<Function> seen = new HashSet<>();
		List<Function> frontier = new ArrayList<>();
		frontier.add(root);
		seen.add(root);
		counts.add(1);
		for (int d = 1; d <= maxDepth && !frontier.isEmpty(); d++) {
			List<Function> next = new ArrayList<>();
			for (Function f : frontier) {
				if (monitor.isCancelled()) {
					return counts;
				}
				if (f != root && (hasSyscall(f) || (threshold >= 0 && callers(f) >= threshold))) {
					continue; // 펼치지 않음
				}
				for (Edge e : edges(f)) {
					if (e.target != null && seen.add(e.target)) {
						next.add(e.target);
					}
				}
			}
			counts.add(next.size());
			frontier = next;
		}
		return counts;
	}

	@Override
	protected void run() throws Exception {
		String[] a = getScriptArgs();
		if (a.length < 3) {
			printerr("CallGraphMap: <출력 폴더> <뿌리 함수> <최대 깊이> [공용 도구 기준=20]");
			return;
		}
		File outDir = new File(a[0]);
		outDir.mkdirs();
		String rootName = a[1];
		int maxDepth = Integer.parseInt(a[2]);
		int threshold = a.length > 3 ? Integer.parseInt(a[3]) : 20;
		fm = currentProgram.getFunctionManager();
		base = currentProgram.getImageBase().getOffset();

		Function root = null;
		for (Function f : fm.getFunctions(true)) {
			if (f.getName().equals(rootName)) {
				root = f;
				break;
			}
		}
		if (root == null) {
			printerr("CallGraphMap: 뿌리 함수를 못 찾음: " + rootName);
			return;
		}
		long t0 = System.currentTimeMillis();

		// (B) 공용 도구에서 멈춘 지도 — 파일로 남김
		Map<Function, Integer> depthOf = new LinkedHashMap<>();
		Map<Function, String> status = new HashMap<>();
		Map<String, String[]> leaves = new LinkedHashMap<>(); // 끊긴 끝: label -> {depth, kind, note}
		Map<String, Integer> leafSites = new LinkedHashMap<>(); // 끊긴 끝 종류별 호출 자리 수 (sast/05 스크립트 문제 2)
		depthOf.put(root, 0);
		List<Function> frontier = new ArrayList<>();
		frontier.add(root);
		try (PrintWriter ew = new PrintWriter(new File(outDir, rootName + "_edges.tsv"), "UTF-8")) {
			ew.println("# depth\tcaller\tcall_off\tret_off\tkind\tcallee\tnote");
			for (int d = 0; d < maxDepth && !frontier.isEmpty(); d++) {
				List<Function> next = new ArrayList<>();
				for (Function f : frontier) {
					if (monitor.isCancelled()) {
						break;
					}
					if (f != root && hasSyscall(f)) {
						status.put(f, "syscall\t함수 안에 SYSCALL: 커널로 넘어감");
						continue;
					}
					if (f != root && callers(f) >= threshold) {
						status.put(f, "helper\t공용 도구: caller " + callers(f) + "곳 (기준 " + threshold + ") → 안 펼침");
						continue;
					}
					status.put(f, "expanded\t");
					for (Edge e : edges(f)) {
						String note = e.note;
						if (e.target != null) {
							if (!depthOf.containsKey(e.target)) {
								depthOf.put(e.target, d + 1);
								next.add(e.target);
							}
							else {
								note = (note.isEmpty() ? "" : note + "; ") + "(seen)";
							}
						}
						else {
							leaves.putIfAbsent(e.kind + ":" + e.label, new String[] { String.valueOf(d + 1), e.kind, e.note });
							leafSites.merge(e.kind, 1, Integer::sum);
						}
						ew.printf("%d\t%s\t%s\t%s\t%s\t%s\t%s%n", d + 1, f.getName(), e.callOff, e.retOff, e.kind,
							e.label, note);
					}
				}
				frontier = next;
			}
			for (Function f : frontier) {
				status.putIfAbsent(f, "depth\t최대 깊이 " + maxDepth + " 에 닿음");
			}
		}
		try (PrintWriter nw = new PrintWriter(new File(outDir, rootName + "_nodes.tsv"), "UTF-8")) {
			nw.println("# name\trva\tsize\tdepth\tcallers_total\tstatus\treason");
			for (Map.Entry<Function, Integer> en : depthOf.entrySet()) {
				Function f = en.getKey();
				String st = status.getOrDefault(f, "depth\t최대 깊이 " + maxDepth + " 에 닿음");
				nw.printf("%s\t0x%x\t%d\t%d\t%d\t%s%n", f.getName(), f.getEntryPoint().getOffset() - base,
					f.getBody().getNumAddresses(), en.getValue(), callers(f), st);
			}
			for (Map.Entry<String, String[]> en : leaves.entrySet()) {
				String[] v = en.getValue();
				nw.printf("%s\t-\t-\t%s\t-\t%s\t%s%n", en.getKey(), v[0], v[1], v[2]);
			}
		}

		// 요약
		Map<String, Integer> statusCount = new LinkedHashMap<>();
		for (String st : status.values()) {
			statusCount.merge(st.split("\t")[0], 1, Integer::sum);
		}
		Map<String, Integer> leafCount = new LinkedHashMap<>();
		for (String[] v : leaves.values()) {
			leafCount.merge(v[1], 1, Integer::sum);
		}
		try (PrintWriter sw = new PrintWriter(new File(outDir, rootName + "_summary.txt"), "UTF-8")) {
			sw.println("root=" + rootName + " maxDepth=" + maxDepth + " helperThreshold=" + threshold);
			sw.println("(A) 거르지 않은 전체 — 깊이별 새 함수 수: " + layers(root, maxDepth, -1));
			for (int th : new int[] { 10, 20, 50 }) {
				sw.println("(B) 공용 도구 기준 caller>=" + th + " 에서 멈춤 — 깊이별 새 함수 수: " + layers(root, maxDepth, th));
			}
			sw.println("파일로 남긴 지도(기준 " + threshold + "): 함수 " + depthOf.size() + "개, 상태별 " + statusCount);
			sw.println("끊긴 끝(서로 다른 것): " + leaves.size() + "개, 종류별 " + leafCount);
			sw.println("끊긴 끝(호출 자리 수): 종류별 " + leafSites);
			sw.println("걸린 시간: " + (System.currentTimeMillis() - t0) / 1000 + "초");
		}
		println("CallGraphMap: 끝 -> " + outDir + "/" + rootName + "_{edges,nodes}.tsv, _summary.txt");
	}
}

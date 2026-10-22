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
//      ret_off = frag  : 자기 조각(FUN_)으로 가는 JMP. 호출이 아니라 함수 안 이동 (.asm 에 "; FRAGJMP"). 묶음 N1.
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
//      call_off/ret_off = seh:<이름>+0x.. : 예외 정리 함수(<함수>$fin$N, <함수>$filt$N) 안의 호출. 호출로 안 닿고
//                        예외 때 운영체제가 부르는 형제 함수라 자동으로 같이 읽는다 (.asm "; --- 예외 정리 함수 ..."). 묶음 O2.
//      .asm 만 (묶음 O1a): 조각 줄 끝 "(= <함수> +0x..)" 환산. 장부: cfg 출처를 이름으로 못 찾으면 cfg:[레지스터+오프셋]
//      .asm 만 (묶음 O1b): 전역 변수 읽기·쓰기 줄 끝 "; <PDB 이름>+0x.." (메모리 피연산자, PDB 이름만, 앞쪽 0x100 안, GS:/FS: 제외),
//                        맨 끝 "; --- 읽지 않은 틈" 목록 (점프로 안 닿은 함수 안 코드)
//      FAKECALL        : 목적지 첫 명령이 ADD RSP,8 인 CALL — 돌아올 주소를 바로 버림. 함수 호출 아님 [추측].
//                        장부에 안 넣는다 (.asm 에 "; FAKECALL" 만). 묶음 O1b.
//      주인 없는 덩어리 : 함수가 JMP/Jcc 로 넘어가는 곳이 **어느 함수에도 안 속한 코드**면 (Ghidra 가 함수 범위에 못 넣은 것)
//                        거기서부터 흐름(다음 명령, 점프)을 따라가 그 함수 몫으로 읽는다. 오프셋은 본래 함수 시작 기준(+0x..),
//                        .asm 에 "; --- 주인 없는 덩어리 ..." 머리와 함께 붙는다 (sast/06, 2026-10-21).
//@category PE-Internals
import java.io.File;
import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Deque;
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
import ghidra.program.model.address.AddressRange;
import ghidra.program.model.address.AddressSet;
import ghidra.program.model.address.AddressSetView;
import ghidra.program.model.lang.Register;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.SourceType;
import ghidra.program.model.symbol.Symbol;
import ghidra.program.model.symbol.SymbolIterator;

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

	// ⑫ 전역 변수 자리에 가장 가까운 앞쪽 PDB 이름(IMPORTED) (+칸). 0x100 이상 떨어지면 다른 변수일 수 있어 안 붙인다 (묶음 O1b)
	private String globalName(Address to) {
		if (!to.isMemoryAddress() || to.getOffset() < 0x10000
				|| currentProgram.getFunctionManager().getFunctionContaining(to) != null) {
			return null;
		}
		SymbolIterator it = currentProgram.getSymbolTable().getSymbolIterator(to, false);
		while (it.hasNext()) {
			Symbol s = it.next();
			long d = to.getOffset() - s.getAddress().getOffset();
			if (d < 0) {
				continue;
			}
			if (d >= 0x100) {
				return null;
			}
			String nm = s.getName();
			if (s.getSource() == SourceType.IMPORTED && nm.startsWith("??_C@")) {
				return null; // 문자열 상수(PDB 이름 ??_C@…) — 변수가 아니고 못 읽음. 더 앞 이름으로 넘어가지 않는다
			}
			if (s.getSource() == SourceType.IMPORTED && !nm.startsWith("Ordinal_") && !nm.startsWith("s_")
					&& !nm.startsWith("u_") && !nm.startsWith("caseD_") && !nm.startsWith("switchD_") && !nm.startsWith("LAB_")) {
				return d == 0 ? nm : nm + "+0x" + Long.toHexString(d);
			}
		}
		return null;
	}

	private String dataNote(Instruction ins) {
		StringBuilder b = new StringBuilder();
		for (int i = 0; i < ins.getNumOperands(); i++) {
			if (ins.getDefaultOperandRepresentation(i).contains("S:[")) {
				return ""; // GS:/FS: 세그먼트 칸이 있는 명령 — 커널 GS 는 TEB 가 아니다. 칸 이름은 PDB 타입으로
			}
		}
		for (Reference r : ins.getReferencesFrom()) {
			if (!r.getReferenceType().isData() && !r.getReferenceType().isRead() && !r.getReferenceType().isWrite()) {
				continue;
			}
			int oi = r.getOperandIndex();
			if (oi < 0 || oi >= ins.getNumOperands() || !ins.getDefaultOperandRepresentation(oi).contains("[")) {
				continue; // 메모리 피연산자([..])에서 나온 참조만. 레지스터 값 따라가기 참조는 안 적는다
			}
			String n = globalName(r.getToAddress());
			if (n != null && b.indexOf(n) < 0) {
				b.append(b.length() == 0 ? "    ; " : ", ").append(n);
			}
		}
		return b.toString();
	}

	// ⑥ 가짜 호출: 목적지 첫 명령이 ADD RSP,8 (48 83 C4 08) — 돌아올 주소를 바로 버림 (묶음 O1b)
	private boolean isFakeCall(Instruction ins) {
		Address[] fl = ins.getFlows();
		if (fl == null || fl.length != 1 || !fl[0].isMemoryAddress()) {
			return false;
		}
		byte[] b = new byte[4];
		try {
			currentProgram.getMemory().getBytes(fl[0], b);
		}
		catch (Exception e) {
			return false;
		}
		return b[0] == 0x48 && (b[1] & 0xff) == 0x83 && (b[2] & 0xff) == 0xC4 && b[3] == 0x08;
	}

	// ⑤ 채움 바이트뿐인가: INT3(CC), NOP(90), 00, 여러 바이트 NOP(66 90, [66..] [2E] 0F 1F /0 + 0 들) (묶음 O1b)
	private boolean isPadding(Address a, long n) {
		byte[] b = new byte[(int) n];
		try {
			currentProgram.getMemory().getBytes(a, b);
		}
		catch (Exception e) {
			return false;
		}
		int i = 0;
		while (i < b.length) {
			int x = b[i] & 0xff;
			if (x == 0xCC || x == 0x90 || x == 0x00) {
				i++;
				continue;
			}
			int j = i;
			while (j < b.length && ((b[j] & 0xff) == 0x66 || (b[j] & 0xff) == 0x2E)) {
				j++;
			}
			if (j > i && j < b.length && (b[j] & 0xff) == 0x90) {
				i = j + 1;
				continue;
			}
			if (j + 2 >= b.length || (b[j] & 0xff) != 0x0F || (b[j + 1] & 0xff) != 0x1F) {
				return false;
			}
			int extra;
			switch (b[j + 2] & 0xff) {
				case 0x00: extra = 0; break;
				case 0x40: extra = 1; break;
				case 0x44: extra = 2; break;
				case 0x80: extra = 4; break;
				case 0x84: extra = 5; break;
				default: return false;
			}
			int end = j + 3 + extra;
			if (end > b.length) {
				return false;
			}
			for (int k = j + 3; k < end; k++) {
				if (b[k] != 0) {
					return false;
				}
			}
			i = end;
		}
		return true;
	}

	// ⑤ RSB 채우기 사다리: ADD RSP,8; CALL rel32 (48 83 C4 08 E8 ..) 9바이트 쌍이 3번 이상만으로 된 구간이면 칸 수, 아니면 0
	private int rsbLadder(Address a, long n) {
		if (n % 9 != 0 || n / 9 < 3) {
			return 0;
		}
		byte[] b = new byte[(int) n];
		try {
			currentProgram.getMemory().getBytes(a, b);
		}
		catch (Exception e) {
			return 0;
		}
		for (int i = 0; i < b.length; i += 9) {
			if (b[i] != 0x48 || (b[i + 1] & 0xff) != 0x83 || (b[i + 2] & 0xff) != 0xC4 || b[i + 3] != 0x08
					|| (b[i + 4] & 0xff) != 0xE8) {
				return 0;
			}
		}
		return (int) (n / 9);
	}

	// ⑤ 읽지 않은 틈: .asm 에 나온 곳(본체·조각·덩어리·예외 정리 함수) 범위들 사이(0x1000 이하)에서 안 나온 곳.
	// 다 읽은 뒤 한 번에 계산한다. 이름 있는 다른 함수 자리와 채움 바이트는 뺀다 (묶음 O1b)
	private void printGaps(PrintWriter w, Function f, AddressSetView printed, long start, FunctionManager fm) {
		AddressSet holes = new AddressSet();
		Address prevEnd = null;
		for (AddressRange rng : printed.getAddressRanges()) {
			if (prevEnd != null) {
				long d = rng.getMinAddress().subtract(prevEnd);
				if (d > 1 && d <= 0x1000) {
					holes.addRange(prevEnd.add(1), rng.getMinAddress().subtract(1));
				}
			}
			prevEnd = rng.getMaxAddress();
		}
		holes = holes.subtract(printed);
		if (holes.isEmpty()) {
			return;
		}
		java.util.Iterator<Function> gi = fm.getFunctionsOverlapping(holes);
		AddressSet named = new AddressSet();
		while (gi.hasNext()) {
			Function g = gi.next();
			if (g != f && !g.getName().startsWith("FUN_")) {
				named.add(g.getBody());
			}
		}
		holes = holes.subtract(named);
		List<String> out = new ArrayList<>();
		for (AddressRange r : holes.getAddressRanges()) {
			if (isPadding(r.getMinAddress(), r.getLength())) {
				continue;
			}
			int rungs = rsbLadder(r.getMinAddress(), r.getLength());
			out.add(String.format(";   %s~%s  (%d바이트)%s", off(r.getMinAddress().getOffset() - start),
				off(r.getMaxAddress().getOffset() - start), r.getLength(),
				rungs > 0 ? "  RSB 채우기 사다리 " + rungs + "칸 (ADD RSP,8; CALL 반복 — 읽을 것 없음 [추측])" : ""));
		}
		if (!out.isEmpty()) {
			w.println();
			w.println("; --- 읽지 않은 틈 (점프로 안 닿음 — 예외 착지점, Ghidra 가 못 읽는 명령(HRESET 등) 등. 원본 바이트로 볼 것. 오프셋은 본래 함수 시작 기준) ---");
			for (String s : out) {
				w.println(s);
			}
		}
	}

	private static boolean isCfgDispatch(String n) {
		return n != null && (n.contains("guard_dispatch_icall") || n.contains("guard_xfg_dispatch_icall"));
	}

	// CFG 간접 호출: CALL 앞을 최대 10개 거슬러 올라가 RAX 를 채운 명령을 찾고,
	// 그 명령이 읽은 메모리 자리(함수 포인터 변수)의 이름을 돌려준다.
	private String cfgTarget(Instruction call, AddressSetView region) {
		long base = currentProgram.getImageBase().getOffset();
		Instruction p = call;
		for (int i = 0; i < 10; i++) {
			p = currentProgram.getListing().getInstructionBefore(p.getAddress());
			// 다음 줄로 넘어오지 않는 명령(JMP·RET 등) 앞에서 멈춘다 — 다른 갈래의 값을 집지 않게 (묶음 O1a)
			if (p == null || !region.contains(p.getAddress()) || p.getFlowType().isCall() || !p.hasFallthrough()) {
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
				if (ref.getReferenceType().isData() && !p.getDefaultOperandRepresentation(p.getNumOperands() - 1).contains("+ R")) {
					Address to = ref.getToAddress();
					Symbol s = currentProgram.getSymbolTable().getPrimarySymbol(to);
					if (to.isStackAddress()) { // 함수 주소를 지역 변수(스택 칸)에서 꺼냄 (묶음 N1)
						return "indirect:cfg:stack[" + off(to.getOffset()) + "]";
					}
					if (to.isExternalAddress()) {
						return "indirect:cfg:import:" + (s != null ? s.getName() : "?");
					}
					if (s != null && s.getSource() != SourceType.DEFAULT) {
						return "indirect:cfg:" + s.getName();
					}
					return "indirect:cfg:@0x" + Long.toHexString(to.getOffset() - base);
				}
			}
			// ⑭ 데이터 이름이 없으면 RAX 를 채운 원천(레지스터+오프셋)을 적는다 — "MOV RAX, [메모리]" 꼴만 (묶음 O1a)
			if (p.getNumOperands() >= 2 && p.getMnemonicString().equalsIgnoreCase("MOV")) {
				String src = p.getDefaultOperandRepresentation(1).replace("qword ptr ", "").replace(" ", "");
				if (src.startsWith("[")) {
					return "indirect:cfg:" + src;
				}
			}
			return "indirect:cfg:?";
		}
		return "indirect:cfg:?";
	}

	// 어느 함수에도 안 속한 코드 덩어리를 start 에서부터 흐름(다음 명령, 점프)을 따라 모은다.
	// 함수 안 주소, 이미 모은 주소, 명령이 없는 곳에서 멈춘다.
	private AddressSet collectOrphan(Address start, AddressSetView done) {
		AddressSet set = new AddressSet();
		Deque<Address> q = new ArrayDeque<>();
		q.add(start);
		FunctionManager fm = currentProgram.getFunctionManager();
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

	// region 의 명령을 차례로 .asm 에 쓰고, 호출은 _calls.tsv 에 caller=ownerName 으로 적는다.
	// start = 오프셋 기준 주소, prefix 가 "" 이면 +0x.., 조각이면 "frag:FUN_..+0x..".
	// owner·region 밖으로 가는 점프의 목적지가 이름 없는(FUN_) 함수면 frags, 어느 함수에도 안 속하면 orphans 에 모은다.
	private void walkBody(String ownerName, Function owner, AddressSetView region, long start, String prefix,
			PrintWriter w, PrintWriter calls, FunctionManager fm, Set<Function> frags, Set<Address> orphans) {
		Address prevEnd = null;
		for (AddressRange rng : region.getAddressRanges()) {
		// 같은 함수(또는 조각·덩어리)가 멀리 떨어진 범위로 나뉘면 머리 표시 (묶음 N1, 예: KiSchedulerApc +0x4106e4)
		if (prevEnd != null && rng.getMinAddress().subtract(prevEnd) > 0x1000) {
			w.println(String.format("; --- 같은 함수의 떨어진 범위 %s~%s  (Ghidra 가 이 본체로 잡음. 오프셋은 같은 기준) ---",
				rng.getMinAddress(), rng.getMaxAddress()));
		}
		prevEnd = rng.getMaxAddress();
		InstructionIterator it = currentProgram.getListing().getInstructions(new AddressSet(rng), true);
		while (it.hasNext()) {
			Instruction ins = it.next();
			long o = ins.getAddress().getOffset() - start;
			String at = prefix + off(o);
			StringBuilder line = new StringBuilder(
				String.format("%-9s %s  %s", at, ins.getAddress(), ins.toString()));
			String mn = ins.getMnemonicString().toUpperCase();
			boolean toFrag = false;
			String dn = ins.getFlowType().isCall() ? "" : dataNote(ins); // ⑫
			if (mn.startsWith("J")) {
				for (Address fl : ins.getFlows()) {
					if (owner.getBody().contains(fl) || region.contains(fl)) {
						continue;
					}
					Function g = fm.getFunctionContaining(fl);
					if (g == null) {
						if (currentProgram.getListing().getInstructionAt(fl) != null) {
							orphans.add(fl);
						}
					}
					else if (g != owner && !g.isExternal() && g.getName().startsWith("FUN_")) {
						frags.add(g);
						toFrag = true;
					}
				}
			}
			if (ins.getFlowType().isCall()) {
				if (mn.startsWith("INT")) {
					line.append("    ; TRAP");
				}
				else if (!mn.startsWith("J") && isFakeCall(ins)) {
					// ⑥ 장부에 안 넣는다 (묶음 O1b)
					line.append("    ; FAKECALL (목적지 첫 명령 ADD RSP,8 — 돌아올 주소를 바로 버림. 함수 호출 아님 [추측])");
				}
				else if (mn.startsWith("J") && toFrag) {
					String tgt = calleeName(ins, fm);
					line.append("    ; FRAGJMP -> ").append(tgt);
					calls.printf("%s\t%s\t%s\t%s%n", ownerName, at, "frag", tgt);
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
						tgt = cfgTarget(ins, region);
						lastAliases = new ArrayList<>();
					}
					line.append("    ; CALL ret=").append(ret).append(" -> ").append(tgt).append(aliasNote());
					calls.printf("%s\t%s\t%s\t%s%n", ownerName, at, ret, tgt);
				}
			}
			line.append(dn);
			if (prefix.startsWith("frag:")) {
				// ⑦ 조각 오프셋을 본래 함수 기준으로도 (묶음 O1a)
				line.append("    (= ").append(ownerName).append(" ").append(off(ins.getAddress().getOffset() - owner.getEntryPoint().getOffset())).append(")");
			}
			w.println(line);
		}
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

		// 3) 예외 정리 형제 함수 (<이름>$fin$N, <이름>$filt$N) 모으기 (묶음 O2, sast/09)
		Map<String, List<Function>> seh = new HashMap<>();
		for (Function g : fm.getFunctions(true)) {
			String n = g.getName();
			int k = n.indexOf("$fin$");
			if (k < 0) {
				k = n.indexOf("$filt$");
			}
			if (k > 0) {
				seh.computeIfAbsent(n.substring(0, k), x -> new ArrayList<>()).add(g);
			}
		}

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
					Set<Address> orphans = new LinkedHashSet<>();
					walkBody(name, f, f.getBody(), entry, "", w, calls, fm, frags, orphans);
					// 떨어진 조각(최대 20개)과 주인 없는 덩어리를 새로 안 나올 때까지 따라간다
					List<Function> todo = new ArrayList<>();
					Set<Function> seen = new HashSet<>();
					AddressSet orphanDone = new AddressSet();
					int fi = 0;
					boolean progress = true;
					while (progress) {
						progress = false;
						for (Function g : new ArrayList<>(frags)) {
							if (seen.add(g)) {
								todo.add(g);
							}
						}
						while (fi < todo.size() && fi < 20) {
							Function g = todo.get(fi++);
							w.println();
							w.println(String.format("; --- 조각 %s  entry=%s  rva=0x%x  (%s 에서 점프로 옴. 오프셋은 조각 시작 기준) ---",
								g.getName(), g.getEntryPoint(), g.getEntryPoint().getOffset() - base, name));
							walkBody(name, f, g.getBody(), g.getEntryPoint().getOffset(), "frag:" + g.getName(), w, calls,
								fm, frags, orphans);
							progress = true;
						}
						for (Address oa : new ArrayList<>(orphans)) {
							if (orphanDone.contains(oa)) {
								continue;
							}
							AddressSet chunk = collectOrphan(oa, orphanDone);
							if (chunk.isEmpty()) {
								continue;
							}
							orphanDone.add(chunk);
							w.println();
							w.println(String.format("; --- 주인 없는 덩어리 %s~%s  (어느 함수에도 안 속함. %s 에서 점프로 옴. 오프셋은 %s 시작 기준) ---",
								chunk.getMinAddress(), chunk.getMaxAddress(), name, name));
							walkBody(name, f, chunk, entry, "", w, calls, fm, frags, orphans);
							progress = true;
						}
					}
					for (Function g : seh.getOrDefault(name, new ArrayList<>())) {
						w.println();
						w.println(String.format("; --- 예외 정리 함수 %s  entry=%s  rva=0x%x  (예외 때 운영체제가 부름 — 호출로 안 닿음. 오프셋은 이 함수 시작 기준) ---",
							g.getName(), g.getEntryPoint(), g.getEntryPoint().getOffset() - base));
						walkBody(name, f, g.getBody(), g.getEntryPoint().getOffset(), "seh:" + g.getName(), w, calls, fm,
							new LinkedHashSet<>(), new LinkedHashSet<>());
					}
					// ⑤ .asm 에 나온 곳을 다 모아 틈을 계산 (묶음 O1b)
					AddressSet printed = new AddressSet(f.getBody());
					for (int q = 0; q < fi; q++) {
						printed.add(todo.get(q).getBody());
					}
					printed.add(orphanDone);
					for (Function g : seh.getOrDefault(name, new ArrayList<>())) {
						printed.add(g.getBody());
					}
					printGaps(w, f, printed, entry, fm);
				}
			}
		}
		di.dispose();
		println("ExportFunctions: 완료 -> " + decDir + " , " + disDir);
	}
}

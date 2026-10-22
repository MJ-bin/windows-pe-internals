// "이 주소(전역 변수·함수)를 누가 읽고 쓰나"를 찾는다 (묶음 O2, sast/11).
// 실행: run_ghidra.sh refs <바이너리> <이름 | 0xRVA> [바이트 수]
//
// 인자: <출력 폴더> <이름 | 0xRVA> [바이트 수]
//   - 이름은 Ghidra 심볼 이름(전역 변수·함수·이름표). 없으면 0xRVA 로 준다 (pdb_aliases.py --where 로 RVA 를 찾는다).
//   - 바이트 수: 안 주면 그 주소의 Ghidra 데이터 길이, 함수면 1(입구), 둘 다 아니면 8.
// 출력: <출력 폴더>/<이름>.tsv 와 화면
//   # kind  from_rva  where  ref_type  target  text
//   kind = ref  : Ghidra 참조(코드에서 읽기·쓰기·부르기, 데이터 포인터 등). where = 함수+0x.. (함수 밖이면 가장 가까운 심볼+0x..)
//   kind = ptr  : 메모리 안 8바이트 값이 이 범위를 가리키는 곳 (Ghidra 가 참조로 안 만든 포인터 표 등, 8바이트 정렬만)
//   target = 범위 안 어느 칸(+0x..)을 가리키나
// 한계: 레지스터로 계산해서 만드는 주소(예: base+offset 를 실행 중에 더함)는 못 찾는다. 그건 동적으로.
//@category PE-Internals
import java.io.File;
import java.io.PrintWriter;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.mem.MemoryBlock;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.Symbol;
import ghidra.program.model.symbol.SymbolIterator;

public class RefsTo extends GhidraScript {

	private long base;

	private String where(Address a) {
		Function f = currentProgram.getFunctionManager().getFunctionContaining(a);
		if (f != null) {
			return f.getName() + "+0x" + Long.toHexString(a.getOffset() - f.getEntryPoint().getOffset());
		}
		Symbol s = null;
		Address p = a;
		for (int i = 0; i < 4096 && p != null; i++) {
			s = currentProgram.getSymbolTable().getPrimarySymbol(p);
			if (s != null) {
				break;
			}
			p = p.previous();
		}
		if (s != null) {
			return s.getName() + "+0x" + Long.toHexString(a.getOffset() - s.getAddress().getOffset()) + " (함수 밖)";
		}
		return "(함수 밖)";
	}

	@Override
	protected void run() throws Exception {
		String[] a = getScriptArgs();
		if (a.length < 2) {
			printerr("RefsTo: <출력 폴더> <이름 | 0xRVA> [바이트 수]");
			return;
		}
		File outDir = new File(a[0]);
		outDir.mkdirs();
		String key = a[1];
		base = currentProgram.getImageBase().getOffset();

		Address start = null;
		String label = key;
		if (key.toLowerCase().startsWith("0x")) {
			start = toAddr(base + Long.parseLong(key.substring(2), 16));
		}
		else {
			SymbolIterator it = currentProgram.getSymbolTable().getSymbols(key);
			if (it.hasNext()) {
				start = it.next().getAddress();
			}
		}
		if (start == null) {
			printerr("RefsTo: 못 찾음: " + key + " (Ghidra 심볼이 아님 — pdb_aliases.py --where 로 RVA 를 찾아 0xRVA 로 다시)");
			return;
		}
		long len;
		if (a.length > 2) {
			len = Long.decode(a[2]);
		}
		else {
			Data d = getDataAt(start);
			Function f = getFunctionAt(start);
			len = (d != null && d.getLength() > 0) ? d.getLength() : (f != null ? 1 : 8);
		}
		long lo = start.getOffset();
		long hi = lo + len; // [lo, hi)
		String safe = label.replaceAll("[^A-Za-z0-9_.@$-]", "_");

		try (PrintWriter w = new PrintWriter(new File(outDir, safe + ".tsv"), "UTF-8")) {
			String head = String.format("# %s  rva=0x%x  len=0x%x", label, lo - base, len);
			w.println(head);
			w.println("# kind\tfrom_rva\twhere\tref_type\ttarget\ttext");
			println(head);
			int nref = 0, nptr = 0;
			// (A) Ghidra 참조
			for (long x = lo; x < hi; x++) {
				Address t = toAddr(x);
				for (Reference r : getReferencesTo(t)) {
					Address from = r.getFromAddress();
					Instruction ins = getInstructionAt(from);
					String line = String.format("ref\t0x%x\t%s\t%s\t+0x%x\t%s", from.getOffset() - base, where(from),
						r.getReferenceType(), x - lo, ins != null ? ins.toString() : "");
					w.println(line);
					println(line);
					nref++;
				}
			}
			// (B) 메모리 안 포인터 값 (8바이트 정렬)
			for (MemoryBlock b : currentProgram.getMemory().getBlocks()) {
				if (!b.isInitialized() || b.getSize() < 8) {
					continue;
				}
				byte[] buf = new byte[(int) Math.min(b.getSize(), Integer.MAX_VALUE - 8)];
				b.getBytes(b.getStart(), buf);
				long bs = b.getStart().getOffset();
				int first = (int) ((8 - (bs % 8)) % 8);
				for (int i = first; i + 8 <= buf.length; i += 8) {
					long v = 0;
					for (int k = 7; k >= 0; k--) {
						v = (v << 8) | (buf[i + k] & 0xff);
					}
					if (v >= lo && v < hi) {
						Address at = toAddr(bs + i);
						String line = String.format("ptr\t0x%x\t%s\t(8바이트 값)\t+0x%x\t", at.getOffset() - base, where(at),
							v - lo);
						w.println(line);
						println(line);
						nptr++;
					}
				}
				if (monitor.isCancelled()) {
					break;
				}
			}
			String tail = String.format("# 참조 %d줄, 포인터 값 %d곳", nref, nptr);
			w.println(tail);
			println("RefsTo: " + tail + " -> " + new File(outDir, safe + ".tsv"));
		}
	}
}

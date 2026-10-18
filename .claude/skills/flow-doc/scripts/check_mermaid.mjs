// mermaid 문법 검사 (그림을 그리지는 않고 파싱만). check_mermaid.sh 가 부른다.
// 사용법: node check_mermaid.mjs <block_0.mmd> [block_1.mmd ...]
import { JSDOM } from 'jsdom';
import fs from 'fs';

const dom = new JSDOM('<!doctype html><html><body></body></html>');
globalThis.window = dom.window;
globalThis.document = dom.window.document;
globalThis.DOMParser = dom.window.DOMParser;
globalThis.Element = dom.window.Element;
const { default: mermaid } = await import('mermaid');

let bad = 0;
for (const f of process.argv.slice(2)) {
  try {
    await mermaid.parse(fs.readFileSync(f, 'utf8'));
    console.log(`${f}: parse OK`);
  } catch (e) {
    bad++;
    console.log(`${f}: PARSE ERROR ${e.message}`);
  }
}
process.exit(bad ? 1 : 0);

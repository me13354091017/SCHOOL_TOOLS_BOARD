/**
 * build.mjs — 生成可发布的 index.html 与离线单文件版
 *   node build.mjs
 * 产物：
 *   index.html                      （多文件版，GitHub Pages 直接使用）
 *   dist/inkboard-standalone.html   （单文件版，可拷进 U 盘双击打开）
 */
import fs from 'node:fs';
import path from 'node:path';

const root = path.dirname(new URL(import.meta.url).pathname);
const read = (p) => fs.readFileSync(path.join(root, p), 'utf8');
const tpl = read('src/template.html');

// 1) 多文件版：模板原样输出
fs.writeFileSync(path.join(root, 'index.html'), tpl);

// 2) 单文件版：内联 CSS / JS（不含 pdf.js，PDF 导入需在线版）
const css = read('src/style.css') + '\n' + read('src/style-ui-scale.css');
const jsFiles = ['00-util.js', '10-geometry.js', '20-model.js', '30-render.js', '40-input.js', '50-tools.js', '60-ui.js', '65-ui-tools.js', '70-main.js'];
const js = jsFiles.map((f) => `/* ===== ${f} ===== */\n` + read('src/' + f)).join('\n');

let single = tpl
  .replace('<link rel="stylesheet" href="src/style.css" />', `<style>\n${css}\n</style>`)
  .replace('<link rel="stylesheet" href="src/style-ui-scale.css" />', '')
  .replace(/<link rel="manifest"[^>]*\/>/, '')
  .replace(/<link rel="icon"[^>]*\/>/, '')
  .replace(/<script src="vendor\/pdf\.min\.js"><\/script>\n/, '')
  .replace(/<script src="vendor\/pdf\.worker\.min\.js"><\/script>\n/, '')
  .replace(/<script src="src\/[^"]*"><\/script>\n?/g, '');
single = single.replace('</body>', `<script>\n(function(){\n${js}\n})();\n</script>\n</body>`);

fs.mkdirSync(path.join(root, 'dist'), { recursive: true });
fs.writeFileSync(path.join(root, 'dist/inkboard-standalone.html'), single);
console.log('✓ index.html 已生成（多文件版）');
console.log('✓ dist/inkboard-standalone.html 已生成（单文件版，' + (single.length / 1024).toFixed(0) + ' KB）');

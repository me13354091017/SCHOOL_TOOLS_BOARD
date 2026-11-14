/**
 * build.mjs — 生成可发布的 index.html 与离线单文件版
 *   node build.mjs
 * 产物：
 *   index.html                      （多文件版，GitHub Pages 直接使用）
 *   dist/stb-standalone.html   （单文件版，可拷进 U 盘双击打开）
 *
 * 品牌补丁：把模板里的旧名「墨板 InkBoard」统一换成「School Tools Board · 课堂白板」
 * 并换成 STB 图标，避免模板与产物不一致。（幂等，重复执行安全）
 */
import fs from 'node:fs';
import path from 'node:path';

const root = path.dirname(new URL(import.meta.url).pathname);
const read = (p) => fs.readFileSync(path.join(root, p), 'utf8');

const BRAND_TITLE = 'School Tools Board · 课堂白板';
const BRAND_DESC = 'School Tools Board（STB）课堂白板 — SCHOOL_TOOLS 旗下的开源白板软件，面向课堂触控一体机：时间笔锋、图形识别、多页板书、课堂工具';
const BRAND_HTML = '<div class="brand"><img class="brand-mark" src="icons/icon.svg" alt="STB" />'
  + '<span class="brand-name">School Tools Board <i>课堂白板</i></span></div>';

function rebrand(tpl) {
  return tpl
    .replace(/<title>[\s\S]*?<\/title>/, `<title>${BRAND_TITLE}</title>`)
    .replace(/<meta name="description" content="[\s\S]*?" \/>/, `<meta name="description" content="${BRAND_DESC}" />`)
    .replace(/<div class="brand">[\s\S]*?<\/div>/, BRAND_HTML);
}

const tpl = rebrand(read('src/template.html'));

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

// 单文件版：把 STB 图标内联为 data URI，单独拷走也不丢图标
const iconData = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(read('icons/icon.svg'));
single = single.replace(/<link rel="(?:icon|apple-touch-icon)"[^>]*\/>/g, '')
               .replace('src="icons/icon.svg"', `src="${iconData}"`);

fs.mkdirSync(path.join(root, 'dist'), { recursive: true });
fs.writeFileSync(path.join(root, 'dist/stb-standalone.html'), single);
console.log('✓ index.html 已生成（多文件版）');
console.log('✓ dist/stb-standalone.html 已生成（单文件版，' + (single.length / 1024).toFixed(0) + ' KB）');

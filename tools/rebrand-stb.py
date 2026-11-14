#!/usr/bin/env python3
"""
rebrand-stb.py — 一次性的品牌迁移脚本：把「墨板 InkBoard」统一为
「School Tools Board（STB）课堂白板」，并换上 STB 图标 / 新的产物文件名。

设计为幂等：目标串已是新值则跳过，缺失则报错退出（避免静默半迁移）。
"""
import json
import os
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
NEW_TITLE = "School Tools Board · 课堂白板"
fails = []
skips = []


def rep(path, old, new, required=True):
    p = ROOT / path
    s = p.read_text(encoding="utf-8")
    if old in s:
        p.write_text(s.replace(old, new), encoding="utf-8")
        print(f"[edit] {path}: {old[:40]!r} -> {new[:40]!r}")
        return True
    if new in s:
        skips.append(f"{path}: 已是新值")
        return True
    if required:
        fails.append(f"{path}: 既没有旧值也没有新值: {old[:60]!r}")
    return False


def insert_after(path, anchor, payload):
    p = ROOT / path
    s = p.read_text(encoding="utf-8")
    if payload.strip().splitlines()[0] in s:
        skips.append(f"{path}: 已插入")
        return True
    if anchor not in s:
        fails.append(f"{path}: 找不到锚点 {anchor[:50]!r}")
        return False
    p.write_text(s.replace(anchor, anchor + payload, 1), encoding="utf-8")
    print(f"[insert] {path}")
    return True


def insert_before(path, anchor, payload):
    p = ROOT / path
    s = p.read_text(encoding="utf-8")
    if payload.strip().splitlines()[0] in s:
        skips.append(f"{path}: 已插入")
        return True
    if anchor not in s:
        fails.append(f"{path}: 找不到锚点 {anchor[:50]!r}")
        return False
    p.write_text(s.replace(anchor, payload + anchor, 1), encoding="utf-8")
    print(f"[insert] {path}")
    return True


# ---------- 1. Web 前端 ----------
rep("src/template.html",
    '<meta name="description" content="墨板 InkBoard — 面向课堂触控一体机的开源白板软件：时间笔锋、图形识别、多页板书、课堂工具" />',
    '<meta name="description" content="School Tools Board（STB）课堂白板 — SCHOOL_TOOLS 旗下的开源白板软件，面向课堂触控一体机：时间笔锋、图形识别、多页板书、课堂工具" />')
rep("src/template.html", "<title>墨板 InkBoard · 课堂白板</title>", f"<title>{NEW_TITLE}</title>")
rep("src/template.html",
    '<div class="brand"><span class="brand-mark">墨</span><span class="brand-name">墨板 <i>InkBoard</i></span></div>',
    '<div class="brand"><img class="brand-mark" src="icons/icon.svg" alt="STB" /><span class="brand-name">School Tools Board <i>课堂白板</i></span></div>')

rep("src/style.css", """.brand-mark {
  width: 34px; height: 34px; border-radius: 10px; display: grid; place-items: center;
  background: linear-gradient(135deg, #12b184, #0a7f5e); color: #fff; font-weight: 800; font-size: 19px;
  box-shadow: 0 4px 12px rgba(14, 159, 118, 0.32);
}""", """.brand-mark {
  width: 34px; height: 34px; border-radius: 10px; display: block; object-fit: contain;
  box-shadow: 0 4px 12px rgba(242, 138, 69, 0.30);
}""")

rep("build.mjs", "dist/inkboard-standalone.html", "dist/stb-standalone.html", required=False)
insert_after("build.mjs",
             "single = single.replace('</body>', `<script>\\n(function(){\\n${js}\\n})();\\n</script>\\n</body>`);",
             """

// 单文件版：把 STB 图标内联成 data URI，单独拷走也不丢图标
const iconData = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(read('icons/icon.svg'));
single = single.replace(new RegExp('<link rel="(?:icon|apple-touch-icon)"[^>]*/>', 'g'), '')
               .replace('src="icons/icon.svg"', `src="${iconData}"`);""")

rep("sw.js", "const VERSION = 'inkboard-v1';", "const VERSION = 'stb-v2';", required=False)

# manifest.json 整体重写
mp = ROOT / "manifest.json"
m = json.loads(mp.read_text(encoding="utf-8"))
m["name"] = "School Tools Board · 课堂白板"
m["short_name"] = "STB"
m["description"] = "SCHOOL_TOOLS 旗下的课堂白板：时间笔锋、图形识别、多页板书、课堂工具，面向触控一体机"
m["icons"] = [{"src": "icons/icon.svg", "sizes": "any", "type": "image/svg+xml"}]
mp.write_text(json.dumps(m, ensure_ascii=False, indent=2), encoding="utf-8")
print("[edit] manifest.json")

# ---------- 2. WPF ----------
rep("wpf/InkBoard.Wpf.csproj", "<AssemblyName>InkBoard</AssemblyName>",
    "<AssemblyName>SchoolToolsBoard</AssemblyName>")
# 应用图标 + 资源（缺 ico 时构建仍可用；CI 会先生成 Assets/app.ico）
cs = ROOT / "wpf/InkBoard.Wpf.csproj"
s = cs.read_text(encoding="utf-8")
if "ApplicationIcon" in s:
    skips.append("wpf/InkBoard.Wpf.csproj: 已有 ApplicationIcon")
else:
    s = s.replace("<RootNamespace>InkBoard</RootNamespace>",
                  "<RootNamespace>InkBoard</RootNamespace>\\n    <ApplicationIcon Condition=\"Exists('Assets/app.ico')\">Assets/app.ico</ApplicationIcon>", 1)
if "Assets/app.ico" not in s:
    s = s.replace("</Project>", """  <ItemGroup>
    <None Remove="Assets/**" />
    <Resource Include="Assets/app.ico" Condition="Exists('Assets/app.ico')" />
  </ItemGroup>
</Project>""")
cs.write_text(s, encoding="utf-8")
print("[edit] wpf/InkBoard.Wpf.csproj")

rep("wpf/MainWindow.xaml", 'Title="墨板 InkBoard · 课堂白板"', f'Title="{NEW_TITLE}"')
rep("wpf/MainWindow.xaml.cs", 'Title = string.Format("墨板 InkBoard · 第 {0}/{1} 页"',
    'Title = string.Format("School Tools Board · 第 {0}/{1} 页"')
insert_after("wpf/MainWindow.xaml.cs", "            InitializeComponent();\\n", """
            // 应用图标（由 icons/icon.svg 生成的 Assets/app.ico；没生成时静默跳过）
            try { Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/app.ico")); } catch { }
""")

rep("wpf/MainWindow.Files.cs", 'Filter = "墨板工程 (*.inkboard.json)|*.json|所有文件|*.*"',
    'Filter = "白板工程 (*.json)|*.json|所有文件|*.*"')
rep("wpf/MainWindow.Files.cs", 'Filter = "墨板工程 (*.json)|*.json"', 'Filter = "白板工程 (*.json)|*.json"')
rep("wpf/MainWindow.Files.cs", 'dlg.PrintVisual(dv, "墨板 InkBoard");', 'dlg.PrintVisual(dv, "School Tools Board");')
rep("wpf/MainWindow.Files.cs", 'Title = "墨板 InkBoard · " + s', 'Title = "School Tools Board · " + s')

# ---------- 3. 工作流：产物名 + 图标生成 ----------
rep(".github/workflows/pages.yml", "if [ -f pub-wpf/InkBoard.exe ]; then", "if [ -f pub-wpf/SchoolToolsBoard.exe ]; then")
rep(".github/workflows/pages.yml", "cp pub-wpf/InkBoard.exe _site/download/inkboard-wpf.exe",
    "cp pub-wpf/SchoolToolsBoard.exe _site/download/school-tools-board.exe")
rep(".github/workflows/pages.yml", "ls -lh _site/download/inkboard-wpf.exe", "ls -lh _site/download/school-tools-board.exe")

rep(".github/workflows/release.yml", 'cp pub-wpf/InkBoard.exe "rel/InkBoard-WPF-${VERSION}.exe"',
    'cp pub-wpf/SchoolToolsBoard.exe "rel/SchoolToolsBoard-WPF-${VERSION}.exe"')
rep(".github/workflows/release.yml", 'cp dist/inkboard-standalone.html "rel/InkBoard-Web-${VERSION}.html"',
    'cp dist/stb-standalone.html "rel/SchoolToolsBoard-Web-${VERSION}.html"')
rep(".github/workflows/release.yml", '--title "墨板 InkBoard v${VERSION}"',
    '--title "School Tools Board（STB）课堂白板 v${VERSION}"')

rep(".github/workflows/wpf.yml", "name: InkBoard-wpf", "name: SchoolToolsBoard-wpf")

ICON_STEP = """      - name: Generate app icon
        run: |
          mkdir -p wpf/Assets
          convert -background none icons/icon.svg \\\\
            -define icon:auto-resize=256,128,64,48,32,16 wpf/Assets/app.ico

"""
for f in (".github/workflows/pages.yml", ".github/workflows/release.yml"):
    s = (ROOT / f).read_text(encoding="utf-8")
    if "Generate app icon" not in s and "      - name: Publish WPF" in s:
        insert_before(f, "      - name: Publish WPF", ICON_STEP)

# ---------- 4. 删除已被替代的产物 ----------
for dead in ("dist/inkboard-standalone.html", "icons/icon-192.png", "icons/icon-512.png"):
    p = ROOT / dead
    if p.exists():
        p.unlink()
        print(f"[rm] {dead}")

print("\\n跳过:", len(skips))
if fails:
    print("\\n失败:")
    for f in fails:
        print("  -", f)
    sys.exit(1)
print("迁移完成")

# School Tools Board · 课堂白板（STB）

面向**课堂触控一体机**的开源白板软件，同一套设计出三个版本：**Web 版**（零依赖、打开浏览器就能上课）、**自研 WPF 桌面版**（Windows 原生、绿色免安装）、**ICC-CE 底座桌面版**（PPT 放映批注、多页白板 + 时间机器、笔压手势）。

> 本应用是 [**SCHOOL_TOOLS**](https://me13354091017.github.io/school_tools/)（一个足以改变全校的教学工具索引）旗下的应用，图标沿用 SCHOOL_TOOLS 的「玻璃板叠层 + 暖色底条」视觉，字母标记为 **STB**。
>
> 血缘：核心笔迹工程思路源于作者上一个项目 [SCHOOL_TOOLS_PDF](https://github.com/me13354091017/SCHOOL_TOOLS_PDF)（WPF / .NET 10 / InkCanvasForPDF 派生的 PDF 墨迹白板）。本项目把那套「**时间笔锋 + 手绘转标准图形 + 函数/坐标系图形**」的墨迹经验分别搬到浏览器与 WPF，几何算法全部自研重写。

## 在线试用

- 在线版： https://me13354091017.github.io/SCHOOL_TOOLS_BOARD/
- 离线单文件版：`dist/stb-standalone.html`（约 170 KB，双击即用，断网也能上课；由 CI 生成，图标已内联）
- 母站： https://me13354091017.github.io/school_tools/

## 下载

| 版本 | 链接 |
|---|---|
| **桌面版 · ICC-CE 底座（PPT 批注 / 时间机器）** | <https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/latest/download/SchoolToolsBoard-STB-Desktop-1.1.1.zip> |
| WPF 桌面版（68 MB 单文件，免装 .NET） | <https://me13354091017.github.io/SCHOOL_TOOLS_BOARD/download/school-tools-board.exe> |
| 全部版本与历史 | <https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases> |

## 桌面版 · ICC-CE 底座（推荐上课用）

在 v1.1.1 并入开源项目 [InkCanvasForClass Community Edition（ICC-CE）](https://github.com/InkCanvasForClass/community) 的源码，fork 成 STB 桌面版。它是教室里最「抗用」的一套：

| 能力 | 说明 |
|---|---|
| PPT 演示批注 | PowerPoint 放映中用笔写，按幻灯片逐页保存墨迹，翻页墨迹跟着走 |
| 多页白板 + 时间机器 | 上百页滑页、无限步撤销/重做、页缩略图 |
| 手势与笔压 | 双指缩放/旋转/平移，笔压参与笔宽，手掌防误触 |
| 形状与学科工具 | 直线/矩形/椭圆/箭头/抛物线、坐标系与函数图像、计时器、聚光灯 |

- 源码：<https://github.com/me13354091017/community>（默认分支 `net10`，GPL-3.0）
- 品牌化：不改上游源码，CI 构建前打补丁（程序名 → `SchoolToolsBoard`、标题 → 「School Tools Board · 课堂白板」、图标用 `stb/icon.svg` 现场生成），跟上游升级永不冲突
- 产物：`SchoolToolsBoard-STB-Desktop-1.1.1.zip`，解压运行 `SchoolToolsBoard.exe`（Windows 10 1809+ / 11 x64）

## 桌面版（WPF）

- 源码：`wpf/`，原生 WPF（.NET 10），**不依赖 WebView2、不依赖第三方 PDF 库**（PDF 用系统自带 `Windows.Data.Pdf` 渲染）
- 产物程序名 `SchoolToolsBoard.exe`，窗口标题「School Tools Board · 课堂白板」，带 STB 应用图标
- 编译产物：仓库 **Actions → WPF Build → 最新一次运行 → Artifacts → `SchoolToolsBoard-wpf`**（需登录 GitHub 下载）
- 本地构建：`dotnet build wpf/InkBoard.Wpf.csproj -c Release`（需 .NET SDK 10 + Windows 10 1809 以上）
- 能力：时间笔锋、荧光笔、按点切割橡皮、手绘转标准图形、7 种图形 + 坐标系 + 函数图像、文字、选择移动、激光笔、全屏授课、计时器、聚光灯、遮罩揭幕、多页管理、5 种背景、PDF 逐页导入、导出 PNG / 打印 PDF、`.json` 工程读写
- 详情与已知限制见 [`wpf/README.md`](wpf/README.md)

## 为什么默认走 Web

| 维度 | WPF（上游项目） | School Tools Board（Web 版） |
|---|---|---|
| 部署 | 需安装 .exe、.NET 运行时、驱动 | 打开浏览器即可，可 PWA 离线 |
| 平台 | Windows only | Windows / 鸿蒙 / Android 一体机通吃 |
| 分发 | Inno Setup 安装包 | GitHub Pages / U 盘拷一个 HTML |
| 升级 | 重新安装 | 刷新页面 |
| 手写笔 | 需 WinRT 墨水栈 | Pointer Events + coalesced 事件，直取高频采样 |

一体机的痛点不是“功能少”，而是“装不上、跑不动、一升级就崩”。所以默认版把“有没有”交给浏览器，把“卡不卡”交给分层画布与脏区重绘；需要纯原生手感或必须离线装机时，再用桌面版。

## 核心能力

### 书写
- **时间笔锋**（向希沃系笔迹学习）：抬笔前笔尖继续缓慢生长至目标粗细，抬笔即停，自然形成锋颖；宽度同时受速度与压感调制。
- 双层画布：`#layer-page` 静态层（增量重绘）+ `#layer-live` 动态层（实时笔迹），抬笔才落层。
- 4px 重采样 + 中心线平滑 + 法线轮廓 + 急转角收窄，`Path2D` 二次曲线填充，60 FPS 下笔迹不断尾。
- 荧光笔：等宽平涂，`alpha 0.34`，可叠色。
- 手掌防误触：刚抬笔 600ms 内的触摸点忽略。

### 图形与数学
- 8 种图形：直线 / 箭头 / 矩形 / 圆 / 三角形 / 多边形 / 平面直角坐标系 / 函数图像。
- **手绘自动转标准图形**：直线度 > 0.86 → 直线；闭合笔画按 RDP 顶点数分类（3 → 三角形、4 → 矩形、圆度 CV < 0.16 → 圆、5~8 → 多边形）。
- **自研表达式解析器**（不用 `eval`）：`sin cos tan sqrt abs log ln exp floor ceil round sign`、`^`、`pi e x`。
- 坐标系与函数图像共用「bbox 内 ±5 单位、unit = min(w,h)/10」的换算，两者天然对齐。

### 页面与板书
- 多页板书 + 左侧缩略图栏（新建 / 复制 / 删除 / 换背景）。
- 5 种背景：纯白、方格、点阵、横线、黑板。
- 页面比例 16:9 / 4:3 一键切，内容自动等比缩放不跑出页面。
- 撤销 / 重做 80 步，自动保存到 IndexedDB（关掉再开还在）。

### 课堂工具（9 项）
计时器（圆环进度 + 到点提示音）· 随机点名（名单本地保存）· 聚光灯（跟随指针、滚轮改光斑）· 遮罩揭幕（下拉拉手逐层露出）· 放大镜（双层合采、真 1.9×）· 板中板（草稿贴回白板）· 导入 PDF（最多 200 页，每页转白板页）· 页面背景 · 快捷键说明。

### 导出
单页 PNG · 全部页面 PNG · 打印 / PDF · `.inkboard` 工程文件（JSON）· 单页 SVG 矢量。

### 大屏适配
`--u` CSS 变量由视口自动计算（1920×1080 = 1，4K = 2），全部 UI 尺寸、间距、圆角、字号跟着缩放；DPR 面积超过 2.6M 像素时自动降到 1.25 封顶。右上角性能面板可实时看 FPS。

## 快捷键

| 键 | 功能 | 键 | 功能 |
|---|---|---|---|
| `P` | 钢笔 | `M` | 荧光笔 |
| `E` | 橡皮 | `S` | 图形 |
| `T` | 文本 | `V` | 选择 |
| `L` | 激光笔 | `H` | 漫游 |
| `空格`（按住） | 临时漫游 | `双指` | 缩放 / 平移 |
| `Ctrl+Z` / `Ctrl+Y` | 撤销 / 重做 | `Ctrl+D` | 复制选中 |
| `Ctrl+A` | 全选 | `Del` | 删除选中 |
| `Ctrl+S` | 导出工程文件 | `←` `→` | 翻页 |
| `F` | 全屏授课 | `Esc` | 收回工具 / 关弹窗 |

## 构建与本地运行

Web 版无需任何依赖（不需要 npm install）：

```bash
node build.mjs                  # 生成 index.html（多文件版）+ dist/stb-standalone.html（单文件版）
python3 -m http.server 8123     # 本地预览：http://localhost:8123
```

`build.mjs` 只做一件事：把 `src/template.html` 里的 `src/*.js` 与两个 CSS 内联成一个 HTML（单文件版），同时写一份保留外链的多文件版；单文件版会把 STB 图标内联成 data URI。

WPF 版：

```bash
dotnet build wpf/InkBoard.Wpf.csproj -c Release
```

在 Linux / macOS 上交叉编译检查（不产 exe，仅验证代码可编译）：

```bash
dotnet build wpf/InkBoard.Wpf.csproj -c Release -p:EnableWindowsTargeting=true
```

## 目录结构

```
SCHOOL_TOOLS_BOARD/
├─ build.mjs                 # 零依赖构建脚本
├─ VERSION                   # 版本号，改动它会触发 Release 流水线
├─ RELEASE_NOTES.md          # 当前版本的发布说明（Release 正文）
├─ manifest.json / sw.js     # PWA：可安装、可离线
├─ icons/                    # STB 图标（icon.svg 矢量 + 192/512/180/32 PNG）
├─ src/                      # Web 版前端（template.html / style*.css / 00~70*.js）
├─ dist/stb-standalone.html  # CI 生成的离线单文件版
├─ wpf/                      # WPF 桌面版（.NET 10）
│  ├─ InkBoard.Wpf.csproj    # 程序集名 SchoolToolsBoard
│  ├─ Assets/app.ico         # 应用图标（由 icons/icon.svg 生成）
│  ├─ App.xaml / App.xaml.cs      # 主题与资源（绿色 #0e9f76）
│  ├─ MainWindow.xaml(.cs)        # 主窗口、工具条、页栏
│  ├─ MainWindow.Files.cs         # 打开/保存/导出/PDF 导入
│  ├─ MainWindow.Classroom.cs     # 全屏授课、计时器、聚光灯、遮罩
│  ├─ MainWindow.Keys.cs          # 快捷键、翻页、撤销重做
│  ├─ MainWindow.Pages.cs         # 页面增删复制
│  ├─ MainWindow.Text.cs          # 文字输入对话框
│  └─ Core/                       # 模型、笔迹几何、橡皮切割、图形识别、双图层画板、绘制导出、PDF
└─ .github/workflows/
   ├─ pages.yml                # 构建 Web + 交叉发布 WPF 并部署 GitHub Pages
   ├─ wpf.yml                  # WPF 在 windows-latest 上编译并上传产物
   └─ release.yml              # VERSION 变更即打 tag、建 Release、挂附件（含 ICC-CE 底座包）
```

## 在课堂一体机上用

1. Web 版：用一体机浏览器打开在线版（或把 `stb-standalone.html` 拷到 U 盘里双击）；
2. ICC-CE 底座版（推荐上课用）：下载 `SchoolToolsBoard-STB-Desktop-1.1.1.zip` 解压，运行里面的 `SchoolToolsBoard.exe`（需要 Windows 10 1809 以上 + .NET 10 Desktop Runtime；PPT 批注需本机装 PowerPoint）；
3. 自研 WPF 版：下载 <https://me13354091017.github.io/SCHOOL_TOOLS_BOARD/download/school-tools-board.exe> 双击运行（免装 .NET；也可在 Actions 产物里取 `SchoolToolsBoard-wpf`）；
4. 点「全屏授课」进入沉浸模式（工具条变淡，顶栏自动隐藏）；
5. 想离线常驻：浏览器菜单「添加到主屏幕」即可安装为 PWA。

## 与希沃白板的差距（实话实说）

已覆盖：笔迹、荧光笔、橡皮、图形/函数、多页板书、课堂工具（计时/点名/聚光灯/遮罩/放大镜/板中板/PDF 讲评）、导出；ICC-CE 底座版另有 PPT 放映按页批注、无限步时间机器、笔压手势、坐标系与函数图像等学科工具。

**尚未覆盖**：云账号与课件库、协同备课、微课录制、课堂活动（分组竞赛/抢答器）、截图提问、与教材资源打通。这些都需要后端与内容生态，不是单仓库能完成的。

## 许可

GPL-3.0-or-later，与上游 `SCHOOL_TOOLS_PDF` 保持一致。完整许可原文：https://www.gnu.org/licenses/gpl-3.0.txt

> 你可以自由使用、修改、再发布；修改后的作品需同样以 GPL-3.0 开源。

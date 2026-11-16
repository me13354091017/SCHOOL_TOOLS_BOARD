# SchoolToolsBoard（STB）· 课堂白板

面向**课堂触控一体机**的开源白板软件。**主应用程序 = 桌面版**，自 v1.0.0.0 起以开源项目 [Ink Canvas Plus（IC+）](https://github.com/clover-tree/Ink-Canvas-Plus) 为底座重构；早期 v1.1.x 的 ICC-CE 版本已归档保留；另有配套的 **Web 版**（零依赖、打开浏览器就能上课）。

> 本应用是 [**SCHOOL_TOOLS**](https://me13354091017.github.io/school_tools/)（一个足以改变全校的教学工具索引）旗下的应用，图标沿用 SCHOOL_TOOLS 的「玻璃板叠层 + 暖色底条」视觉，字母标记为 **STB**。
>
> 血缘：Web 版核心笔迹工程思路源于作者上一个项目 [SCHOOL_TOOLS_PDF](https://github.com/me13354091017/SCHOOL_TOOLS_PDF)（WPF / PDF 墨迹白板）。

## 下载

| 版本 | 说明 | 链接 |
|---|---|---|
| **桌面版（主应用，IC+ 底座）** | 当前主应用，v1.0.0.0 起 | <https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/latest> |
| **Windows 安装包（推荐）** | Inno Setup 向导：可选目录、建快捷方式、带卸载项 | <https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/latest> |
| **Web 离线单文件** | `dist/stb-standalone.html`，约 170 KB、双击即用、断网也能上课 | <https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/latest> |
| ICC-CE 底座旧版（归档） | v1.1.5，含板中板 | <https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/tag/v1.1.5> |
| 全部版本 | | <https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases> |

- 在线版：<https://me13354091017.github.io/SCHOOL_TOOLS_BOARD />
- 母站：<https://me13354091017.github.io/school_tools />

## 主应用 · 桌面版（IC+ 底座）

底座为 `clover-tree/Ink-Canvas-Plus`（GPL-3.0，WPF / .NET Framework 4.7.2）。**不改上游源码**，全部改动在 CI 构建前打补丁，所以跟上游升级不会冲突。

| 能力 | 说明 |
|---|---|
| PPT 演示批注 | PowerPoint 放映中直接书写，逐页保存墨迹，翻页墨迹跟着走（IC+ 自带 Interop 实现，不需要加载项） |
| 多页板书 + 时间机器 | 滑页板书、无限步撤销/重做 |
| 手势与笔压 | 双指缩放/旋转/平移，笔压参与笔宽，手掌防误触 |
| 形状与学科工具 | 直线/矩形/椭圆/箭头/抛物线、坐标系与函数图、计时器、随机点名、遮罩、聚光灯、希沃图标包 |
| **STB 视觉主题** | `STBLight`（象牙白）/ `STBDark`（石墨）两套浮动栏配色，启动默认合并 STB 浅色，`Settings.Appearance.Theme` 默认 18，主题切换表新增第 7 项 |
| **黑板界面** | 黑板底 `#101A16`、白板底 `#F7F8F6`，全量替换取色常量与覆盖层 |
| 品牌与图标 | 程序名 `SchoolToolsBoard`、窗口标题「School Tools Board 课堂白板」、公司 `SCHOOL_TOOLS`、图标用母站 STB 图标现场生成 |

**跟 ICC-CE 版相比少的东西（实话实说）**：IC+ 没有板中板（MiniWhiteboard），所以 v1.0.0.0 **不提供**板中板；旧版 v1.1.5 仍可下载，需要板中板请用那个版本。旧版设置（`%APPDATA%\Ink Canvas`）不会自动迁移。

- 上游：<https://github.com/clover-tree/Ink-Canvas-Plus>（GPL-3.0，维护者 Clover Yan，复刻自 WXRIW/Ink Canvas）
- 补丁：`stb-plus/apply-stb-plus-patches.ps1`（品牌/版本、窗口标题、默认主题、`case 6 → STB`、全局配色、主题字典、安装包脚本）
- 流水线：`.github/workflows/release-plus.yml`（检出 IC+ → nuget restore → 打补丁 → 生成图标 → msbuild → zip → Inno Setup → 建 Release）
- 产物：`SchoolToolsBoard-Setup-<版本>.exe`、`SchoolToolsBoard-Plus-Desktop-<版本>.zip`（解压运行 `SchoolToolsBoard.exe`）

## 配套 · Web 版

| 维度 | 桌面版（主应用） | Web 版（配套） |
|---|---|---|
| 部署 | 下载 zip 解压即用 | 打开浏览器即可，可 PWA 离线 |
| 平台 | Windows only | Windows / 鸿蒙 / Android 一体机通吃 |
| 分发 | GitHub Release | GitHub Pages / U 盘拷一个 HTML |
| 升级 | 覆盖文件 | 刷新页面 |
| PPT 批注 | 支持 | 不支持（改为导入 PDF 讲评） |

一体机的痛点不是「功能少」，而是「装不上、跑不动、一升级就崩」。所以主应用直接拿成熟的桌面内核来用；现场装不了软件、只能用浏览器时，退回 Web 版。

### Web 版能力要点

- **书写**：时间笔锋（抬笔前笔尖继续生长至目标粗细）、双层画布增量重绘、4px 重采样 + 法线轮廓、荧光笔、手掌防误触。
- **图形与数学**：8 种图形，手绘自动转标准图形（直线/三角形/矩形/圆/多边形）；自研表达式解析器与坐标系/函数图像共用换算。
- **页面**：多页板书 + 缩略图栏、5 种背景、16:9 / 4:3 切换、80 步撤销、IndexedDB 自动保存。
- **课堂工具（9 项）**：计时器、随机点名、聚光灯、遮罩揭幕、放大镜、板中板、导入 PDF、页面背景、快捷键说明。
- **导出**：单页/全部 PNG、打印/PDF、`.inkboard` 工程、单页 SVG。

### 快捷键（Web 版）

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

Web 版无需依赖（不需要 npm install）：

```bash
node build.mjs                  # index.html + dist/stb-standalone.html
python3 -m http.server 8123     # 本地预览：http://localhost:8123
```

桌面版（IC+ 底座）也是 CI 编，本地手动编需要 Windows + MSBuild（net472）：

```bash
git clone https://github.com/clover-tree/Ink-Canvas-Plus.git
cd Ink-Canvas-Plus && nuget restore && msbuild "Ink Canvas.sln" -target:InkCanvasPlus -p:Configuration=Release -p:Platform="Any CPU"
```

正常情况下不用手编：改 `VERSION_PLUS` 就触发 `.github/workflows/release-plus.yml`；旧 ICC-CE 线仍由改 `VERSION` 触发 `release.yml`。

## 目录结构

```
SCHOOL_TOOLS_BOARD/
├─ build.mjs                 # 零依赖构建脚本（Web 版）
├─ VERSION_PLUS              # IC+ 主应用版本号（1.0.0.0），改动即触发 release-plus.yml
├─ VERSION                   # ICC-CE 旧版版本号（v1.1.x）
├─ RELEASE_NOTES_PLUS.md     # 当前主应用发布说明
├─ stb-plus/                 # IC+ 底座补丁与 STB 主题字典
│  ├─ apply-stb-plus-patches.ps1
│  └─ Styles/{STBLight,STBDark}.xaml
├─ icons/ · src/ · manifest.json · sw.js   # Web 版与 STB 图标
└─ .github/workflows/
   ├─ pages.yml               # Web 版构建并部署 Pages
   ├─ release-plus.yml        # 主应用（IC+ 底座）发布
   └─ release.yml             # ICC-CE 旧版发布（归档线）
```

## 在课堂一体机上用

1. 下载 Release 里的 `SchoolToolsBoard-Setup-<版本>.exe` 双击安装（免安装取 `SchoolToolsBoard-Plus-Desktop-<版本>.zip`，解压后运行 `SchoolToolsBoard.exe`）。Windows 10 1903+；PPT 批注需本机装 PowerPoint。
2. 装不了软件时：用一体机浏览器打开在线版，或把 `stb-standalone.html` 拷到 U 盘里双击。
3. 点「全屏授课」进入沉浸模式。
4. 想离线常驻：浏览器菜单「添加到主屏幕」即可安装为 PWA。

## 与希沃白板的差距（实话实说）

已覆盖：笔迹、荧光笔、橡皮、图形/函数、多页板书、时间机器、PPT 放映逐页批注、课堂工具（计时/点名/聚光灯/遮罩/放大镜/PDF 讲评）与一套自有的 STB 明暗双主题。

**尚未覆盖**：**板中板**（IC+ 底座无此窗口，旧版 v1.1.5 仍有）、云账号与课件库、协同备课、微课录制、课堂活动（分组竞赛/抢答器）、截图提问、与教材资源打通。

## 许可

GPL-3.0-or-later，与上游 IC+ / ICC-CE 保持一致。完整许可原文：https://www.gnu.org/licenses/gpl-3.0.txt
> 你可以自由使用、修改、再发布；修改后的作品需同样以 GPL-3.0 开源。

# School Tools Board（STB）课堂白板 v1.1.1

面向课堂触控一体机的开源白板，**同一仓库三个版本**：Web 版（零依赖）、自研 WPF 桌面版、**基于 ICC-CE 的桌面版**（PPT 批注 + 多页白板 + 时间机器）。GPL-3.0。本应用是 [SCHOOL_TOOLS](https://me13354091017.github.io/school_tools/) 旗下应用。

## 下载

| 文件 | 说明 |
|---|---|
| `SchoolToolsBoard-STB-Desktop-1.1.1.zip` | **新增**：基于 ICC-CE 的桌面版，解压后运行 `SchoolToolsBoard.exe`（Windows 10 1809+ / 11 x64） |
| `SchoolToolsBoard-WPF-1.1.1.exe` | 自研 WPF 桌面版，自包含单文件，双击即用，无需装 .NET |
| `SchoolToolsBoard-Web-1.1.1.html` | Web 离线单文件版，双击用浏览器打开，断网可用（图标已内联） |
| `LICENSE.txt` | GPL-3.0 完整原文 |

在线试用：<https://me13354091017.github.io/SCHOOL_TOOLS_BOARD/>

## v1.1.1 相对 v1.1.0 的变更

### 新增：以 ICC-CE 为底座的桌面版

第一版桌面版是自研 WPF，功能覆盖到「够用」，但 PPT 放映批注、无限步时间机器、笔压曲线这些教室刚需，靠自研追平需要很久。v1.1.1 起并入开源项目 **InkCanvasForClass Community Edition（ICC-CE, GPL-3.0）** 的源码底座，fork 成 STB 桌面版：

- **PPT 演示批注**：PowerPoint 放映中用笔写，按幻灯片逐页保存墨迹，翻页墨迹跟着走
- **多页白板 + 时间机器**：上百页滑页、无限步撤销/重做、页缩略图
- **手势与笔压**：双指缩放/旋转/平移，笔压参与笔宽，手掌防误触
- **形状与学科工具**：直线/矩形/椭圆/箭头/抛物线、坐标系与函数图像、计时器、聚光灯

品牌化方式：**不改上游源码**，构建时在 CI 里打补丁（程序名 `InkCanvasForClass` → `SchoolToolsBoard`、窗口标题 → 「School Tools Board · 课堂白板」、图标用 STB 矢量现场生成 ico），所以 `git pull` 跟上游升级永不冲突。上游作者 CJKmkp 与三代贡献者（WXRIW/Ink-Canvas → ChangSakura/Ink-Canvas → InkCanvasForClass → ICC-CE）的版权与 GPL-3.0 声明完整保留。

### 其他

- 版本号 `1.1.0` → `1.1.1`
- 发布资产统一 ASCII 文件名，避免中文名下载 404
- ICC-CE 桌面版源码固定在 fork：<https://github.com/me13354091017/community>（默认分支 `net10`）

## 功能（Web / WPF 版）

### 墨迹
- 时间笔锋：4px 重采样 + 速度定宽 + 起收笔包络 + 法线轮廓，抬笔即定形
- 双层画布：静态层增量重绘 + 动态层实时笔迹，笔迹不断尾
- 荧光笔：等宽平涂半透明，可叠色

### 工具
- 橡皮：按点切割，只擦掉一段，笔画分段保留
- 图形：直线、箭头、矩形、圆、三角形、多边形、平面直角坐标系、函数图像
- 手绘转标准图形：抬笔自动纠正为直线 / 三角 / 矩形 / 圆
- 自研表达式解析（不用 eval）：`sin cos tan sqrt abs log ln exp`、`^`、`pi e x`
- 文字、选择移动、框选、复制、激光笔、漫游

### 课堂
- 全屏授课、计时器、聚光灯、遮罩揭幕、放大镜、板中板
- 多页板书、5 种背景、16:9 与 4:3
- 导入 PDF（最多 200 页）、导出 PNG / PDF / SVG / `.json` 工程文件
- PWA 可安装、可离线；4K 大屏 UI 自动放大

## 许可

GPL-3.0-or-later，与上游 [SCHOOL_TOOLS_PDF](https://github.com/me13354091017/SCHOOL_TOOLS_PDF) 及 ICC-CE 一致。

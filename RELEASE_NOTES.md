# School Tools Board（STB）课堂白板 v1.1.0

面向课堂触控一体机的开源白板，Web 版 + WPF 桌面版，GPL-3.0。本应用是 [SCHOOL_TOOLS](https://me13354091017.github.io/school_tools/) 旗下应用。

## 下载

| 文件 | 说明 |
|---|---|
| `SchoolToolsBoard-WPF-1.1.0.exe` | Windows 桌面版，自包含单文件（约 68 MB），双击即用，无需安装 .NET，需要 Windows 10 1809 以上 |
| `SchoolToolsBoard-Web-1.1.0.html` | Web 离线单文件版，双击用浏览器打开，断网可用（图标已内联） |
| `LICENSE.txt` | GPL-3.0 完整原文（35 KB） |

在线试用：<https://me13354091017.github.io/SCHOOL_TOOLS_BOARD/>

## v1.1.0 相对 v1.0.1 的变更

- **改名**：产品名改为「School Tools Board · 课堂白板」（STB），与仓库名 `SCHOOL_TOOLS_BOARD` 对齐，不再叫「墨板 InkBoard」
- **图标**：改用 SCHOOL_TOOLS 母站的图标语言（玻璃板叠层 + 暖色底条），字母标记 **STB**；字母已转成矢量路径，任何环境渲染一致；WPF 版带 `app.ico` 应用图标（CI 自动生成）
- **归属**：README 与站点说明标注为 SCHOOL_TOOLS 旗下应用
- **程序名**：WPF 产物由 `InkBoard.exe` 改为 `SchoolToolsBoard.exe`，窗口标题「School Tools Board · 课堂白板」
- **文件名**：站点下载改为 `download/school-tools-board.exe`，离线单文件版改为 `dist/stb-standalone.html`
- **离线版**：单文件版把图标内联为 data URI，单独拷走不再丢图标

## 功能

### 墨迹
- 时间笔锋：4px 重采样 + 速度定宽 + 起收笔包络 + 法线轮廓，抬笔即定形
- 双层画布：静态层增量重绘 + 动态层实时笔迹，笔迹不断尾
- 荧光笔：等宽平涂半透明，可叠色

### 工具
- 橡皮：**按点切割**，只擦掉一段，笔画分段保留（Web / WPF 双端一致）
- 图形：直线、箭头、矩形、圆、三角形、多边形、平面直角坐标系、函数图像
- **手绘转标准图形**：抬笔自动纠正为直线 / 三角 / 矩形 / 圆
- 自研表达式解析（不用 eval）：`sin cos tan sqrt abs log ln exp`、`^`、`pi e x`
- 文字、选择移动、框选、复制、激光笔、漫游

### 课堂
- 全屏授课、计时器、聚光灯（滚轮调半径）、遮罩揭幕、放大镜、板中板
- 多页板书、5 种背景（空白 / 方格 / 点阵 / 横线 / 黑板）、16:9 与 4:3
- 导入 PDF（最多 200 页，每页转白板页）、导出 PNG / PDF / SVG / `.json` 工程文件
- PWA 可安装、可离线；4K 大屏 UI 自动放大

## 许可

GPL-3.0-or-later，与上游 [SCHOOL_TOOLS_PDF](https://github.com/me13354091017/SCHOOL_TOOLS_PDF) 一致。

# 墨板 InkBoard · WPF 桌面版

原生 WPF（.NET 10）实现，**不依赖 WebView2、不依赖第三方 PDF 库**，直接跑在 Windows 课堂触控一体机上。
同一套产品思想与数据模型与 Web 版（仓库根目录）一致：从 `me13354091017/SCHOOL_TOOLS_PDF`（InkCanvasForPDF 衍生）的墨迹白板思路延续而来，几何算法全部自研。

## 与 Web 版的关系

| | Web 版 | WPF 桌面版 |
|---|---|---|
| 运行环境 | 浏览器（Chromium / Edge / Chrome） | Windows 10 1809+ / Windows 11 |
| 笔迹渲染 | Canvas 2D | `DrawingVisual` + `StreamGeometry`（WPF 抗锯齿） |
| 触控 | Pointer Events | WPF `Stylus` / `Touch` / `Manipulation` |
| PDF 底图 | pdf.js | 系统自带 `Windows.Data.Pdf` |
| 离线 | PWA 缓存 | 绿色单文件，直接运行 |
| 压感 | 支持触摸压感 | 支持笔压（`StylusPoint.PressureFactor`） |

## 功能

- 笔迹：笔 / 荧光笔；4px 重采样 + 速度定宽 + 起收笔包络 + 法线轮廓（时间笔锋）
- 橡皮：按点切割，只擦掉经过的一段，笔迹分成几段保留
- **手绘转标准图形**：抬笔即判断——直线度 > 0.99 → 直线，闭合笔迹按简化顶点数分类（3 → 三角形、4 → 矩形、圆度 > 0.82 → 圆/椭圆），工具条「智能转图形」可开关
- 图形：直线、箭头、矩形、圆、三角形、坐标系（自动刻度/箭头/O·x·y 标注）、函数图像（自研表达式解析，支持 `sin/cos/tan/sqrt/abs/ln/log/exp`、`pi/e`、`^`）
- 文字：多行文本框，可再次编辑
- 选择：框选、拖动、Delete 删除、Ctrl+D 复制
- 课堂工具：全屏授课、计时器、聚光灯（滚轮调半径）、遮罩揭幕（拖拽/双击关闭）、激光笔
- 页面：多页、新建/复制/删除、5 种背景（空白 / 网格 / 点阵 / 横线 / 黑板）、16:9 与 4:3
- 导入导出：`.json` 工程读写、拖拽打开、PDF 逐页导入为白板底图、导出 PNG、打印为 PDF
- 性能：静态层缓存 + 实时层，FPS 面板（工具条「性能」）

## 构建

需要 **.NET SDK 10** 与 Windows 10 1809 以上（`Windows.Data.Pdf` 所需）。

```powershell
dotnet build wpf/InkBoard.Wpf.csproj -c Release
dotnet run  --project wpf/InkBoard.Wpf.csproj
```

输出：`wpf/bin/Release/net10.0-windows10.0.19041.0/InkBoard.exe`

自包含单文件发布（用户无需装 .NET，体积约 71 MB）：

```powershell
dotnet publish wpf/InkBoard.Wpf.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -o pub-wpf
```

CI：`.github/workflows/wpf.yml` 在 `windows-latest` 上编译并上传产物（Actions → Artifacts）。在线版站点也直接提供下载：<https://me13354091017.github.io/SCHOOL_TOOLS_BOARD/download/inkboard-wpf.exe>

## 已知限制

- PDF 底图不参与工程序列化（`.json` 只存笔迹与图形）
- 页面比例是全局设置，单页不可单独设定
- 已在 .NET SDK 10 下交叉编译验证（0 error / 0 warning），实际笔感需在 Windows 一体机上跑一遍确认

## 许可

GPL-3.0，与上游 `SCHOOL_TOOLS_PDF` 一致。见仓库根目录 `LICENSE`。

# SchoolToolsBoard 1.0.1.37

PPT 放映识别换成 GitHub 上验证过的实现。

## 识别：三重判据（QTimer 的 is_ppt_slideshow_active + RimePPT 的放映窗观察）
- 类名层：`screenClass`（PowerPoint）、`paneClassDC`、`MSO_SubWindow_Class`、`kslideshow` / `wps_slideshow` / `wpp_slideshow_class`（WPS 各版本）
- 进程层：窗口属于 `POWERPNT` / `PPTVW` / `WPP` / `WPS`，且标题带「幻灯片放映 / Slide Show」，或者是真全屏
- 全屏层：真全屏 = 窗口矩形与所在显示器矩形相差 ≤6px，**且排除最大化窗口**（编辑窗最大化不再算放映）
- 进程名读不到（权限受限）时，要求「标题带放映关键词 + 真全屏」

## 反应速度
- 常驻检查 0.8 秒一拍 → 0.5 秒一拍；按 F5 基本一秒内进批注

## 进入批注
- 有 COM 放映窗时走上游 `PptApplication_SlideShowBegin`（原生工具栏 / 翻页 / 结束放映）
- 拿不到 COM（WPS / 权限隔离 / 保护视图）时退化为轻量批注

## 异常复位（沿用 1.0.1.36）
- 进程全部退出（任务管理器结束 / 崩溃 / 先关文稿再退出）→ 收回批注界面、清 COM、重启上游定时器
- 点「结束放映」保留 5 秒静默期


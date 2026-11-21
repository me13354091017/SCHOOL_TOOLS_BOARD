# SchoolToolsBoard 1.0.1.38

PPT 的「功能」全部回到 Ink Canvas Plus 原版，只保留新的放映识别。

## 回到原版（与上游逐字一致）
- 结束放映：`BtnPPTSlideShowEnd_Click` 恢复上游原文，去掉 5 秒静默期与 Esc 兜底，只调 `pptApplication.SlideShowWindows[1].View.Exit()`
- 上一页 / 下一页：去掉 1.0.1.34 的 `pptApplication == null || StbPptComShowCount() < 1` 预检守卫
- 放映结束：去掉 1.0.1.36 塞进 `PptApplication_SlideShowEnd` 的 `_stbPptLight = false` + `StbPptExitLight()`
- 常驻探测：去掉 `StbPptInUserEndHold()` 判断，识别不再受「结束放映静默期」影响

## 保留（属于识别）
- `MainWindow.PptDetect.cs`：三重判据（放映专用类名 / 放映进程 + 标题 / 真全屏且非最大化）
- 常驻检查 0.5 秒一拍；COM 拿不到时轻量批注；进程消失后复位并重建 COM

## 取舍
- 结束放映按钮回到原版后依赖 PowerPoint 的 COM：WPS 轻量批注里点它不会结束放映，按 Esc 即可，识别会在放映窗消失后自动退出批注


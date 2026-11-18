# SchoolToolsBoard (STB) v1.0.1.13

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 新增：橡皮自动回笔
擦掉笔迹后自动切回书写，不用再手动点橡皮关闭。覆盖三处：
- 主窗口黑板 / 白板（`inkCanvas`）
- PPT / 批注模式（同一个画布，同样生效）
- 板中板（`MiniInkCanvas`），回笔时同时收起橡皮大小条

只在橡皮模式下生效：判断依据是画布 `EditingMode` 处于 `EraseByPoint` / `EraseByStroke`，并监听 `Strokes.StrokesChanged` 中 `Removed.Count > 0`（点擦拆分笔迹也算），切换任务走 `DispatcherPriority.Input`，不阻塞当前擦除手势。

## 修复：板中板橡皮条一点就没
原因在 `StbRootGrid_PreviewMouseLeftButtonDown`：它无条件 `CloseMiniPopups()`，然后把这次点击当作“点画布”处理。橡皮条 1.0.1.12 改成窗内面板之后，点条内部（包括拖滑杆）都会命中这段逻辑，于是面板刚展开就收起，看起来像“一拖就消失”。

现在先判断点击是否落在 `InlineEraserPanel` 内部：在内则保留面板并直接返回（也不参与四边四角缩放判定），在外仍然按原逻辑收起。

## 保留
- 橡皮条窗内嵌、直径松手生效
- 拖拽角缩放 40ms 节流
- 快捷设置面板新排版、另存为独立图标、透明底图标

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

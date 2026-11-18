# SchoolToolsBoard (STB) v1.0.1.14

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 修：橡皮一碰墨迹就变书写

上一版把“自动回笔”直接挂在 `StrokesChanged` 上，只要有一次笔迹被删除就立刻切回 `Ink`。
问题在于 `EraseByStroke`（整笔擦）是**按下即删**：手指落下的瞬间笔迹已被移除，于是模式立刻变回书写，后续拖动画出新笔迹——“橡皮碰到墨迹就变成书写”就是这么来的。

现在改成两段式：
1. `StrokesChanged` 只置一个 `_stbErasedInGesture` 标记（“这一笔擦到东西了”）；
2. 在 `PreviewStylusUp` / `PreviewMouseUp`（用 `AddHandler(..., true)`，即使被内部占用也能收到）里才把标记转成真正的回笔。

这样擦除手势全程都是橡皮，抬手后才回笔，与希沃手感一致。主窗口与板中板各一套，逻辑一致。

## 保留
- 板中板橡皮条窗内嵌，点条内不再被“点画布收起”误杀
- 拖拽角缩放 40ms 节流
- 快捷设置面板新排版、另存为独立图标、透明底图标

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

# SchoolToolsBoard (STB) v1.0.1.14

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 橡皮回笔：从“碰到就回”改成“抬笔才回”
上一版的做法是：`Strokes.StrokesChanged` 里 `Removed.Count > 0` 就直接切回 `Ink`。问题是橡皮一碰到第一笔，画布当场变回书写，你按着不放往下擦，后面全是画线——所以提笔之前就被打断了。

现在改为两段式：
- **擦的过程中**：`StrokesChanged` 只置 `_stbErasePending = true`，`EditingMode` 保持 `EraseByPoint` / `EraseByStroke`，一次按住可以连续擦任意多笔。
- **抬笔时**：`PreviewStylusUp`、`PreviewMouseLeftButtonUp`、`PreviewTouchUp`、`LostMouseCapture` 四个出口都挂同一个 flush；有挂起才真正切回 `Ink`，切完清标记。

挂起标记一次手势只置一次、flush 一次就清，不会重复触发；切换仍放在 `DispatcherPriority.Input`，不抢当前输入队列。板中板的 `MiniInkCanvas` 同一套逻辑，回笔时顺便把橡皮大小条收起。

## 新增开关：橡皮用完自动回到书写
位置：设置 → 「墨迹书写」分类，第三张卡下面。默认 **开**。
- 关闭后擦完仍停在橡皮，适合要连续擦一大片再统一写的场景
- 开关值写 `%AppData%\SchoolToolsBoard\prefs.txt` 的 `autoPenBack`，不动上游 Settings JSON，避免反序列化风险
- 黑板 / 白板、PPT 批注、板中板读同一个键，改一处全生效

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

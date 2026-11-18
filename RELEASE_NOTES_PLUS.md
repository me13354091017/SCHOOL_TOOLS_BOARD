# SchoolToolsBoard (STB) v1.0.1.5

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 修复

### 1. 「画笔颜色 · 更多」调色盘能正常用了
- 弹层原来靠 XAML 里的 `ElementName` 绑定找按钮，弹层自带独立名字域，绑定失效后就「跟着鼠标乱跑」，会压住浮动栏和画布。
- 改为打开时由代码直接指定弹出锚点（贴在调色板图标正上方，水平居中），不再依赖绑定。
- 选色改用弹层的 `PreviewMouseLeftButtonDown`：`StaysOpen=false` 的弹层会在点击瞬间先关闭，`MouseUp` 根本收不到，这是「点了没反应」的根因。
- 点色块即时生效：直接改 `DefaultDrawingAttributes.Color`，并复用上游 `ColorSwitchCheck()` 收尾（笔迹模式、选中笔迹同步着色、快捷键色点状态重置）。

### 2. 用橡皮不再有蓝色圈
- 黑板、批注、板中板三处橡皮的「跟随笔尖蓝色圆框」全部去掉，光标恢复常规笔形。

### 3. 设置页新增「橡皮擦大小」
- 位置：设置 → 墨迹书写 → 笔迹宽度下方。
- 形态：`－` / `＋` 两个按钮 + 中间实时显示当前直径（共 7 档：0.5× / 0.8× / 1.0× / 1.6× / 2.0× / 2.5× / 3.6×，即上游 `Settings.Canvas.EraserSize` 的 -2…4）。
- 改完立刻生效（同步刷新 `inkCanvas.EraserShape`），无需重启；同时写入配置文件。

## 安装说明
- 覆盖安装即可，安装目录沿用 `Program Files\SCHOOL_TOOLS\SchoolToolsBoard`。
- 便携版解压即用。

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

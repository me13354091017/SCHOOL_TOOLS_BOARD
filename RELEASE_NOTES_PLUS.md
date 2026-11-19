# SchoolToolsBoard (STB) v1.0.1.21

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 「打开」在哪
1. 快捷设置面板 → 「墨迹」一行第三项：图标 `▱`，下面写着「打开」；
2. 右键浮动栏最左侧笑脸 → 菜单里的「打开」；
3. 组件 · 浮动栏 → 工具库里的「打开」，加到自己的浮动栏即可常驻。

## 行为
- 弹 `OpenFileDialog`（过滤 `*.icstk`），默认目录仍是 `Documents\Ink Canvas Strokes\User Saved`；
- 选中后 `ClearStrokes(true)` 清空当前画布，再 `inkCanvas.Strokes.Add(...)` 载入，并弹通知「已打开 <路径>」。

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

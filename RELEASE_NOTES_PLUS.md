# SchoolToolsBoard (STB) v1.0.1.17

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 旧的「组件」窗口下线
之前点浮动栏上的「组件」会打开主界面组件窗（当前组件 + 组件库那两栏）。既然浮动栏已经接入同一套组件编辑方式，这个窗口就不再需要：

- `SymbolIconComponents_MouseUp` 与 `BtnComponents_Click` 改为 `StbFloatBarWindow.ShowFor(this);`
- 浮动栏与左侧竖栏两处入口文字由「组件」改为「浮动栏」
- 旧窗口类仍保留在源码里（不删，避免牵连其他引用），但界面上已经没有任何入口能打开它

## 「保存到其它」在哪里
两处都能用：
1. 浮动栏快捷设置面板 → 「墨迹」那一行第二个图标（两个文档叠起来、带向右箭头的 Copy 图标），就在「保存」右边；
2. 本次新增：组件 · 浮动栏 → 工具库里的「保存到其它」，加到自定义浮动栏后点一下就会弹另存对话框。

实现上把原来的另存逻辑抽成 `internal void StbSaveAsNow()`：浮动栏图标保留 `lastBorderMouseDownObject` 校验，组件条目直接调用，不用伪造 MouseDown 状态。

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

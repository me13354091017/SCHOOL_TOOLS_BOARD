# SchoolToolsBoard (STB) v1.0.1.12

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 本版重点：板中板橡皮条不再闪断 / 卡死

现象：点开板中板的「橡皮」，拖动橡皮大小滑杆时，板中板会闪一下甚至整块消失，鼠标拖动严重卡顿。

原因：板中板本身是 `AllowsTransparency="True"` 的分层窗口，而橡皮大小条当时是一个 `Popup`——`Popup` 在 WPF 里又是一个独立的分层窗口。拖动滑杆时，弹出层和板中板本体每动一下都要各自重绘、重新合成，叠加起来就是闪断 + 卡死。

改法：
1. 橡皮大小条从 `Popup` 改成板中板窗口内部的覆盖面板（`InlineEraserPanel`），点「橡皮」在拖拽条上方展开，再点一次收起；
2. 橡皮直径只在**松手后**应用，拖动过程只更新“直径 N 像素”文字；
3. `EllipseStylusShape` 按尺寸缓存，同一尺寸不再重复 new。

## 保留
- 拖拽角缩放 40ms 节流（松手补最终尺寸）
- PPT / 批注模式橡皮松手生效
- 快捷设置面板新排版、另存为独立图标、透明底图标
- 板中板拖动条 / 拖拽角 / 最大化，三种模式与组件系统

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

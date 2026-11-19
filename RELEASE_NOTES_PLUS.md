# SchoolToolsBoard (STB) v1.0.1.15

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 浮动栏接入同一套组件系统
之前组件系统只管主界面上的信息组件（时钟 / 倒计时 / 课程表…）。这版把浮动栏也做成“组件”：从工具库往浮动栏里放条目，像编辑主界面组件一样增删排序。

### 入口
组件窗（浮动栏「组件」图标 / 左侧竖栏「组件」）→ 底部 **编辑浮动栏**。
窗口顶部：`使用自定义浮动栏` 开关 + `图标大小` 滑杆（12–30）+ `图标下方显示文字`。

### 工作方式
- 关闭接管：浮动栏完全保持原样，内置工具组不动。
- 开启接管且至少一条：隐藏内置两组工具（`StackPanelCanvacMain`、`StackPanelCanvasControls`），用你自己排的条目组成浮动栏；每条都是 图标（默认 18px）+ 可选文字，点一下就执行对应动作。

### 工具库（10 项）
板中板、组件、设置、画笔、荧光笔、橡皮、选择、清屏、全屏、退出。
画笔/荧光笔/橡皮/选择直接改 `inkCanvas.EditingMode`（荧光笔同时给 12px 半透明黄色矩形笔头）；清屏清空当前画布笔迹；全屏在最大化与还原间切换。

### 配置
`%AppData%\SchoolToolsBoard\floatbar.txt`：`enabled` / `size` / `text` / `items`，即时生效，重启保留；「恢复默认」一键清空并关闭接管。

## 保留
- 橡皮抬笔后才回笔 + 设置页开关
- 板中板橡皮条、拖拽角缩放、组件系统、整页设置页

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

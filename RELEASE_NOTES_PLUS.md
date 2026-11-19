# SchoolToolsBoard (STB) v1.0.1.23

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 笑脸菜单：改为连点三次
1. 浮动栏最左側的笑脸按钮，**连续点三下**（间隔 < 800ms）弹出菜单；
2. 菜单顶部是应用图标 + 名字，点一下进「关于应用」；
3. 下面依次：组件编辑 / 设置 / 保存 / 另存到 / 打开 / 重启 / 退出。

## 插入函数：本轮修复点
- 窗口 `Topmost=True` + `ShowInTaskbar=False`，CenterScreen；不再被全屏 Topmost 白板遮住；
- `StbFuncPlot` 增加 `MeasureOverride`，横向列宽自适应，画板不再为 0 尺寸；
- 空状态在画板中央提示写法；错误表达式行红框 + 右侧红字提示条；
- 「插入函数」：先校验 `Curves` 非空；`Plot` 尺寸 < 120 时改用离屏 `StbFuncPlot` 测甲再 `RenderTargetBitmap`，保证插入的不是空白；
- `StbInsertFunctionImage` 改为挂到 `Main_Grid`，`Content as Grid` 作兜底；
- `SymbolIconFunc_MouseUp` 去掉 `lastBorderMouseDownObject` 守卫，只看开关。

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD

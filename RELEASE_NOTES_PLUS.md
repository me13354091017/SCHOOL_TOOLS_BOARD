# SchoolToolsBoard (STB) v1.0.1.20

> 底座：Ink Canvas Plus（clover-tree/Ink-Canvas-Plus，GPL-3.0）｜发行者：周之尧

## 浮动栏笑脸右键菜单
- 右键浮动栏最左侧的笑脸（`BorderFloatingBarEmoji`），弹出卡片；左键仍然是展开/收起工具组，互不影响；
- 卡片头部：应用图标 + `SchoolToolsBoard` + `课堂白板 v1.0.1.20`，点一下直接跳到设置页的「关于应用」；
- 菜单项：组件编辑（浮动栏编辑窗）、设置、保存（默认目录）、另存到、打开（*.icstk）、重启、退出；
- 可在 设置 → 高级 → 「浮动栏笑脸菜单」关掉。

## 插入函数
- 窗口左侧 6 类：一次 `y=x`、二次 `y=x^2`、幂 `y=x^3`、指数 `y=2^x`、对数 `y=log2(x)`、三角 `y=sin(x)`；
- 右侧表达式列表最多 6 条，颜色自动分配（黑/红/蓝/绿/橙/紫），实时重画；
- 解析支持 `x`、`pi`、`e`、`+ - * / ^`、括号，以及 `sin cos tan ln lg log2 sqrt abs`；
- 「插入函数」把当前坐标图按 2x 渲染成位图，作为白板上一张可拖动卡片（右键删除）。
- 入口：浮动栏 `f(x)` 按钮、PPT 批注调色盘「插入函数」按钮、浮动栏工具库条目、设置 → 高级 →「打开函数画板」。

## 设置页新增（高级）
- 浮动栏笑脸菜单开关；
- 浮动栏显示「插入函数」开关；
- 打开函数画板；
- 打开数据目录（`%AppData%\SchoolToolsBoard`）；
- 重启 / 退出。

## 开源
- 上游：https://github.com/clover-tree/Ink-Canvas-Plus （GPL-3.0）
- 本项目：https://github.com/me13354091017/SCHOOL_TOOLS_BOARD
